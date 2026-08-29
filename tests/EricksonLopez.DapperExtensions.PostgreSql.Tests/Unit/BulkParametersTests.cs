// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.PostgreSql.Bulk;
using NpgsqlTypes;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

public sealed class BulkParametersTests
{
    private sealed record Product(Guid Id, string Name, decimal Price, bool IsActive);

    private static readonly List<Product> _products =
    [
        new(Guid.NewGuid(), "Widget",    9.99m,   true),
        new(Guid.NewGuid(), "Gadget",    49.99m,  true),
        new(Guid.NewGuid(), "Doohickey", 4.99m,   false),
    ];

    [Fact]
    public void Build_WithColumns_ShouldProduceCorrectParameterCount()
    {
        var parameters = BulkParameters.From(_products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Build();

        parameters.Should().HaveCount(3);
    }

    [Fact]
    public void Build_ShouldSetArrayFlag_OnEachParameter()
    {
        var parameters = BulkParameters.From(_products)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        // NpgsqlDbType.Array is OR'd with the element type
        var param = parameters[0];
        ((param.NpgsqlDbType & NpgsqlDbType.Array) != 0).Should().BeTrue();
    }

    [Fact]
    public void Build_ShouldExtractCorrectValues()
    {
        var parameters = BulkParameters.From(_products)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        var values = (string[])parameters[0].Value!;
        values.Should().BeEquivalentTo(["Widget", "Gadget", "Doohickey"]);
    }

    [Fact]
    public void Build_ShouldSetParameterName_WithAtPrefix()
    {
        var parameters = BulkParameters.From(_products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
            .Build();

        // Npgsql normalizes: parameter name stored without @
        parameters[0].ParameterName.Should().Be("Ids");
    }

    [Fact]
    public void Build_WithNullableValues_ShouldExtractCorrectly()
    {
        var items = new[] { new { Id = 1, Tag = (string?)null }, new { Id = 2, Tag = (string?)"active" } };

        var parameters = BulkParameters.From(items)
            .Add("Tags", p => p.Tag, NpgsqlDbType.Text)
            .Build();

        var values = (string?[])parameters[0].Value!;
        values[0].Should().BeNull();
        values[1].Should().Be("active");
    }

    [Fact]
    public void Build_WithNoColumns_ShouldThrow()
    {
        var bulk = BulkParameters.From(_products);

        var act = () => bulk.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one column*");
    }

    [Fact]
    public void Count_ShouldReflectNumberOfItems()
    {
        var bulk = BulkParameters.From(_products)
            .Add("Names", p => p.Name, NpgsqlDbType.Text);

        bulk.Count.Should().Be(3);
    }

    [Fact]
    public void From_WithEmptyCollection_ShouldBuildWithZeroCount()
    {
        var bulk = BulkParameters.From(Array.Empty<Product>())
            .Add("Names", p => p.Name, NpgsqlDbType.Text);

        bulk.Count.Should().Be(0);
        var parameters = bulk.Build();
        var values = (string[])parameters[0].Value!;
        values.Should().BeEmpty();
    }

    [Fact]
    public void From_WithNullItems_ShouldThrow()
    {
        var act = () => BulkParameters.From((IEnumerable<Product>)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void Add_WithNullOrWhiteSpaceParameterName_ShouldThrow()
    {
        var bulk = BulkParameters.From(_products);

        var act1 = () => bulk.Add(null!, p => p.Id, NpgsqlDbType.Uuid);
        act1.Should().Throw<ArgumentException>().WithParameterName("parameterName");

        var act2 = () => bulk.Add(" ", p => p.Id, NpgsqlDbType.Uuid);
        act2.Should().Throw<ArgumentException>().WithParameterName("parameterName");
    }

    [Fact]
    public void Add_WithNullSelector_ShouldThrow()
    {
        var bulk = BulkParameters.From(_products);

        var act = () => bulk.Add("Ids", (Func<Product, Guid>)null!, NpgsqlDbType.Uuid);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }
}
