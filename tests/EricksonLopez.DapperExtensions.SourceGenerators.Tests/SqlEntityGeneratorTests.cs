// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace EricksonLopez.DapperExtensions.SourceGenerators.UnitTests;

public sealed class SqlEntityGeneratorTests
{
    private static readonly string[] _expectedDocLines =
    [
        "    /// <summary>",
        "    /// Zero-reflection source-generated factory method for Native AOT multi-mapping.",
        "    /// </summary>",
        "    public static Func<IDataReader, object> GetMultiMapReaderFactory()",
        "    {",
        "        return static reader => ReadFromDataReader(reader);",
        "    }",
        "",
        "    /// <summary>",
        "    /// Reads and populates a new instance of <see cref=\"DocEntity\"/> from an open <see cref=\"IDataReader\"/>.",
        "    /// </summary>"
    ];

    private static readonly MetadataReference[] _baseReferences = GetMetadataReferences().ToArray();

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var assemblies = new HashSet<Assembly>
        {
            typeof(object).Assembly,
            typeof(Attribute).Assembly,
            typeof(IDataReader).Assembly,
            typeof(DataTable).Assembly,
            typeof(EricksonLopez.DapperExtensions.SqlEntityAttribute).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Console).Assembly,
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Data.Common")
        };

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                yield return MetadataReference.CreateFromFile(assembly.Location);
            }
        }
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedSources, Compilation OutputCompilation) RunGenerator(
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: new[] { syntaxTree },
            references: _baseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SqlEntityGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics, cancellationToken);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();

        return (diagnostics, generatedSources, outputCompilation);
    }

    [Fact]
    public void Generator_WithNoTypes_GeneratesNoSources()
    {
        var source = @"
// empty file
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithTypeWithoutAttributes_GeneratesNoSources()
    {
        var source = @"
namespace TestNamespace;

public class RegularUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithNonTypeDeclarationsWithAttributes_GeneratesNoSources()
    {
        var source = @"
using System;

namespace TestNamespace;

[Flags]
public enum UserStatus
{
    None = 0,
    Active = 1
}

public class MyService
{
    [Obsolete]
    public void OldMethod() {}
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithTypeWithUnrelatedAttribute_GeneratesNoSources()
    {
        var source = @"
using System;

namespace TestNamespace;

[Serializable]
[Obsolete(""Use other class"")]
public class NonSqlEntity
{
    public int Id { get; set; }
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WithSqlEntityClass_GeneratesPartialClassWithMappers()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}
";
        var (diagnostics, generatedSources, outputCompilation) = RunGenerator(source);

        diagnostics.Should().BeEmpty();
        generatedSources.Should().HaveCount(1);

        var generated = generatedSources[0];
        generated.HintName.Should().Be("TestApp_Domain_User_SqlEntityMapper.g.cs");

        var code = generated.SourceText.ToString();
        code.Should().Contain("// <auto-generated/>");
        code.Should().Contain("#nullable enable");
        code.Should().Contain("using System;");
        code.Should().Contain("using System.Data;");
        code.Should().Contain("using EricksonLopez.DapperExtensions.MultiMap;");
        code.Should().Contain("namespace TestApp.Domain;");
        code.Should().Contain("partial class User");
        code.Should().Contain("Zero-reflection source-generated factory method for Native AOT multi-mapping.");
        code.Should().Contain("public static Func<IDataReader, object> GetMultiMapReaderFactory()");
        code.Should().Contain("return static reader => ReadFromDataReader(reader);");
        code.Should().Contain("Reads and populates a new instance of <see cref=\"User\"/> from an open <see cref=\"IDataReader\"/>.");
        code.Should().Contain("public static User ReadFromDataReader(IDataReader reader)");
        code.Should().Contain("ArgumentNullException.ThrowIfNull(reader);");
        code.Should().Contain("var instance = new User();");
        code.Should().Contain("reader.GetOrdinal(\"Id\");");
        code.Should().Contain("reader.GetOrdinal(\"Name\");");
        code.Should().Contain("reader.GetOrdinal(\"Email\");");
        code.Should().Contain("!reader.IsDBNull(ordinal)");
        code.Should().Contain("instance.Id = rawValue is int directVal ? directVal : (int)Convert.ChangeType(rawValue, typeof(int));");
        code.Should().Contain("instance.Name = rawValue is string directVal ? directVal : (string)Convert.ChangeType(rawValue, typeof(string));");
        code.Should().Contain("instance.Email = rawValue is string directVal ? directVal : (string?)Convert.ChangeType(rawValue, typeof(string));");
        code.Should().Contain("catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentException)");
        code.Should().Contain("// Column not present in result set; leave default value.");
        code.Should().Contain("return instance;");

        // Verify that the output compilation produces valid IL without errors
        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithSqlEntityAttributeLongName_GeneratesMapper()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntityAttribute(TableName = ""customers"")]
public partial class Customer
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        generatedSources[0].HintName.Should().Be("TestApp_Domain_Customer_SqlEntityMapper.g.cs");

        var code = generatedSources[0].SourceText.ToString();
        code.Should().Contain("partial class Customer");
        code.Should().Contain("instance.Balance = rawValue is decimal directVal ? directVal : (decimal)Convert.ChangeType(rawValue, typeof(decimal));");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithTypeInGlobalNamespace_GeneratesMapperWithoutNamespaceDeclaration()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

[SqlEntity]
public partial class GlobalConfig
{
    public int Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        generatedSources[0].HintName.Should().Be("Global_GlobalConfig_SqlEntityMapper.g.cs");

        var code = generatedSources[0].SourceText.ToString();
        code.Should().NotContain("namespace ");
        code.Should().Contain("partial class GlobalConfig");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithStruct_GeneratesPartialStruct()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Geometry;

[SqlEntity]
public partial struct Point2D
{
    public int X { get; set; }
    public int Y { get; set; }
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        var code = generatedSources[0].SourceText.ToString();
        code.Should().Contain("partial struct Point2D");
        code.Should().Contain("public static Point2D ReadFromDataReader(IDataReader reader)");
        code.Should().Contain("var instance = new Point2D();");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithRecordClass_GeneratesPartialRecordClass()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Orders;

[SqlEntity]
public partial record class OrderHeader
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        var code = generatedSources[0].SourceText.ToString();
        code.Should().Contain("partial record class OrderHeader");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithRecordStruct_GeneratesPartialRecordStruct()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Physics;

[SqlEntity]
public partial record struct Vector3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        var code = generatedSources[0].SourceText.ToString();
        code.Should().Contain("partial record struct Vector3D");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithIgnoredAndNonSettableProperties_OnlyGeneratesPublicSettableProperties()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class SpecialEntity
{
    public int ValidId { get; set; }

    private int SecretCode { get; set; }
    internal string InternalTag { get; set; } = string.Empty;
    protected int ProtectedValue { get; set; }

    public static int StaticProperty { get; set; }
    public int ComputedProperty => ValidId * 2;
    public int ReadOnlyProperty { get; } = 100;
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        var code = generatedSources[0].SourceText.ToString();

        code.Should().Contain("instance.ValidId =");
        code.Should().NotContain("SecretCode");
        code.Should().NotContain("InternalTag");
        code.Should().NotContain("ProtectedValue");
        code.Should().NotContain("StaticProperty");
        code.Should().NotContain("ComputedProperty");
        code.Should().NotContain("ReadOnlyProperty");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithEmptyClass_GeneratesValidMapperWithNoPropertyAssignments()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class EmptyEntity
{
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(1);
        var code = generatedSources[0].SourceText.ToString();

        code.Should().Contain("partial class EmptyEntity");
        code.Should().Contain("var instance = new EmptyEntity();");
        code.Should().Contain("return instance;");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void GeneratedMapper_Execution_PopulatesObjectFromDataReader()
    {
        var source = @"
using System;
using EricksonLopez.DapperExtensions;

namespace TestApp.Runtime;

[SqlEntity]
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Stock { get; set; }
    public Guid Sku { get; set; }
}
";
        var (_, _, outputCompilation) = RunGenerator(source);

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var productType = assembly.GetType("TestApp.Runtime.Product");
        productType.Should().NotBeNull();

        var expectedSku = Guid.NewGuid();

        // Create a DataTable with product data
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(long)); // tests type conversion from long in DB to int in model
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Price", typeof(decimal));
        dt.Columns.Add("Stock", typeof(int));
        dt.Columns.Add("Sku", typeof(Guid));

        dt.Rows.Add(101L, "Mechanical Keyboard", 149.99m, 42, expectedSku);
        using var reader = dt.CreateDataReader();
        reader.Read().Should().BeTrue();

        // Invoke ReadFromDataReader
        var readMethod = productType!.GetMethod("ReadFromDataReader", BindingFlags.Public | BindingFlags.Static);
        readMethod.Should().NotBeNull();

        var product = readMethod!.Invoke(null, new object[] { reader });
        product.Should().NotBeNull();

        productType.GetProperty("Id")!.GetValue(product).Should().Be(101);
        productType.GetProperty("Name")!.GetValue(product).Should().Be("Mechanical Keyboard");
        productType.GetProperty("Price")!.GetValue(product).Should().Be(149.99m);
        productType.GetProperty("Stock")!.GetValue(product).Should().Be(42);
        productType.GetProperty("Sku")!.GetValue(product).Should().Be(expectedSku);

        // Invoke GetMultiMapReaderFactory
        var factoryMethod = productType.GetMethod("GetMultiMapReaderFactory", BindingFlags.Public | BindingFlags.Static);
        factoryMethod.Should().NotBeNull();

        var factory = factoryMethod!.Invoke(null, null) as Func<IDataReader, object>;
        factory.Should().NotBeNull();

        var productFromFactory = factory!(reader);
        productFromFactory.Should().NotBeNull();
        productType.GetProperty("Id")!.GetValue(productFromFactory).Should().Be(101);
    }

    [Fact]
    public void GeneratedMapper_Execution_WithNullDbValuesAndMissingColumns_LeavesDefaults()
    {
        var source = @"
using System;
using EricksonLopez.DapperExtensions;

namespace TestApp.Runtime;

[SqlEntity]
public partial class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = ""Default Title"";
    public string? Subtitle { get; set; }
    public int? Views { get; set; }
}
";
        var (_, _, outputCompilation) = RunGenerator(source);

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var articleType = assembly.GetType("TestApp.Runtime.Article");
        articleType.Should().NotBeNull();

        // DataTable only has Id and Subtitle (DBNull), missing Title and Views columns completely
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Subtitle", typeof(string));

        dt.Rows.Add(200, DBNull.Value);
        using var reader = dt.CreateDataReader();
        reader.Read().Should().BeTrue();

        var readMethod = articleType!.GetMethod("ReadFromDataReader", BindingFlags.Public | BindingFlags.Static);
        var article = readMethod!.Invoke(null, new object[] { reader });

        article.Should().NotBeNull();
        articleType.GetProperty("Id")!.GetValue(article).Should().Be(200);
        articleType.GetProperty("Title")!.GetValue(article).Should().Be("Default Title");
        articleType.GetProperty("Subtitle")!.GetValue(article).Should().BeNull();
        articleType.GetProperty("Views")!.GetValue(article).Should().BeNull();
    }

    [Fact]
    public void GeneratedMapper_Execution_WithNullReader_ThrowsArgumentNullException()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Runtime;

[SqlEntity]
public partial class SafeEntity
{
    public int Id { get; set; }
}
";
        var (_, _, outputCompilation) = RunGenerator(source);

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var entityType = assembly.GetType("TestApp.Runtime.SafeEntity");

        var readMethod = entityType!.GetMethod("ReadFromDataReader", BindingFlags.Public | BindingFlags.Static);
        var action = () =>
        {
            try
            {
                readMethod!.Invoke(null, new object?[] { null });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generator_WithMultipleSqlEntities_GeneratesAllMappers()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Models;

[SqlEntity]
public partial class FirstEntity
{
    public int Id { get; set; }
}

[SqlEntity]
public partial class SecondEntity
{
    public string Code { get; set; } = string.Empty;
}
";
        var (_, generatedSources, outputCompilation) = RunGenerator(source);

        generatedSources.Should().HaveCount(2);
        generatedSources.Select(s => s.HintName).Should().Contain("TestApp_Models_FirstEntity_SqlEntityMapper.g.cs");
        generatedSources.Select(s => s.HintName).Should().Contain("TestApp_Models_SecondEntity_SqlEntityMapper.g.cs");

        using var ms = new MemoryStream();
        var emitResult = outputCompilation.Emit(ms);
        emitResult.Success.Should().BeTrue();
    }

    [Fact]
    public void Generator_WithSyntaxErrorClassDeclaration_DoesNotThrowAndSkips()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

[SqlEntity]
class
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().BeEmpty();
    }

    [Fact]
    public void Generator_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class CancelledEntity
{
    public int Id { get; set; }
}
";
        var action = () => RunGenerator(source, cts.Token);
        action.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Generator_WithMultipleAttributes_IncludingSqlEntity_GeneratesSource()
    {
        var source = @"
using System;
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[Serializable]
[SqlEntity]
public partial class MultiAttrEntity
{
    public int Id { get; set; }
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().ContainSingle();
        generatedSources[0].HintName.Should().Be("TestApp_Domain_MultiAttrEntity_SqlEntityMapper.g.cs");
    }

    [Fact]
    public void Generator_WithSqlEntityShortAttributeName_GeneratesSource()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class ShortAttrNameEntity
{
    public int Id { get; set; }
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().ContainSingle();
        generatedSources[0].HintName.Should().Be("TestApp_Domain_ShortAttrNameEntity_SqlEntityMapper.g.cs");
    }

    [Fact]
    public void IsSyntaxTargetForGeneration_ValidatesNodes()
    {
        var classWithoutAttrs = CSharpSyntaxTree.ParseText("class NoAttrs {}")
            .GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();
        var classWithAttrs = CSharpSyntaxTree.ParseText("[Serializable] class WithAttrs {}")
            .GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();
        var methodNode = CSharpSyntaxTree.ParseText("class C { void M() {} }")
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        SqlEntityGenerator.IsSyntaxTargetForGeneration(classWithoutAttrs).Should().BeFalse();
        SqlEntityGenerator.IsSyntaxTargetForGeneration(classWithAttrs).Should().BeTrue();
        SqlEntityGenerator.IsSyntaxTargetForGeneration(methodNode).Should().BeFalse();
    }

    [Fact]
    public void Generator_GeneratedSource_ContainsExactXmlDocAndFormat()
    {
        var source = @"
using EricksonLopez.DapperExtensions;

namespace TestApp.Domain;

[SqlEntity]
public partial class DocEntity
{
    public int Id { get; set; }
}
";
        var (_, generatedSources, _) = RunGenerator(source);
        generatedSources.Should().ContainSingle();
        var code = generatedSources[0].SourceText.ToString();

        var nl = Environment.NewLine;
        var expectedFactoryBlock = string.Join(nl, _expectedDocLines);

        code.Should().Contain(expectedFactoryBlock);
        code.Should().Contain($"using EricksonLopez.DapperExtensions.MultiMap;{nl}{nl}namespace TestApp.Domain;{nl}{nl}partial class DocEntity");
    }
}
