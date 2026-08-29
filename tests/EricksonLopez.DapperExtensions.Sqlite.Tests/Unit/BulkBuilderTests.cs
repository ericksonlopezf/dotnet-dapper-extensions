// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Sqlite.Bulk;
using Xunit;

namespace EricksonLopez.DapperExtensions.Sqlite.Tests.Unit;

public sealed class BulkBuilderTests
{
    private sealed record Product(int Id, string Name, decimal Price);

    [Fact]
    public void Build_WithItems_ReturnsSqlAndParameters()
    {
        var products = new[]
        {
            new Product(1, "Widget", 9.99m),
            new Product(2, "Gadget", 19.99m)
        };

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Build();

        sql.Should().Be("INSERT INTO \"products\" (\"id\", \"name\", \"price\") VALUES (@p0_0, @p0_1, @p0_2), (@p1_0, @p1_1, @p1_2)");

        parameters.Should().NotBeNull();
        parameters!.Get<int>("p0_0").Should().Be(1);
        parameters.Get<string>("p0_1").Should().Be("Widget");
        parameters.Get<decimal>("p0_2").Should().Be(9.99m);
        parameters.Get<int>("p1_0").Should().Be(2);
        parameters.Get<string>("p1_1").Should().Be("Gadget");
        parameters.Get<decimal>("p1_2").Should().Be(19.99m);
    }

    [Fact]
    public void Build_WithEmptyCollection_ReturnsNullSqlAndParameters()
    {
        var products = Array.Empty<Product>();

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Build();

        sql.Should().BeNull();
        parameters.Should().BeNull();
    }

    [Fact]
    public void Build_WithoutTable_ThrowsInvalidOperationException()
    {
        var products = new[] { new Product(1, "A", 1m) };

        var act = () => BulkBuilder.From(products)
            .Column("id", p => p.Id)
            .Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Table name*");
    }

    [Fact]
    public void Build_WithoutColumns_ThrowsInvalidOperationException()
    {
        var products = new[] { new Product(1, "A", 1m) };

        var act = () => BulkBuilder.From(products)
            .Table("products")
            .Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*column*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Table_WithInvalidName_ThrowsArgumentException(string? invalidTable)
    {
        var products = new[] { new Product(1, "A", 1m) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Table(invalidTable!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Column_WithInvalidName_ThrowsArgumentException(string? invalidColumn)
    {
        var products = new[] { new Product(1, "A", 1m) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column(invalidColumn!, p => p.Id);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Column_WithNullSelector_ThrowsArgumentNullException()
    {
        var products = new[] { new Product(1, "A", 1m) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column("id", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Build_SingleRow_ProducesCorrectSql()
    {
        var products = new[] { new Product(1, "Widget", 9.99m) };

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Build();

        sql.Should().Be(
            "INSERT INTO \"products\" (\"id\", \"name\", \"price\") VALUES (@p0_0, @p0_1, @p0_2)");
        parameters.Should().NotBeNull();
    }

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var products = new[]
        {
            new Product(1, "A", 1m),
            new Product(2, "B", 2m),
            new Product(3, "C", 3m)
        };

        var builder = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id);

        builder.Count.Should().Be(3);
    }

    [Fact]
    public void From_WhenItemsNull_ThrowsArgumentNullException()
    {
        System.Collections.Generic.IEnumerable<Product> items = null!;
        var act = () => BulkBuilder.From(items);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }
}
