// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.MariaDb.Bulk;
using Xunit;

namespace EricksonLopez.DapperExtensions.MariaDb.Tests.Unit;

public sealed class BulkBuilderTests
{
    private sealed record Product(Guid Id, string Name, decimal Price, bool IsActive);

    [Fact]
    public void Build_WithItems_ReturnsSqlAndParameters()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var products = new[]
        {
            new Product(id1, "Product A", 10.5m, true),
            new Product(id2, "Product B", 20.0m, false)
        };

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("is_active", p => p.IsActive)
            .Build();

        sql.Should().NotBeNullOrWhiteSpace();
        sql.Should().Be("INSERT INTO `products` (`id`, `name`, `price`, `is_active`) VALUES (@p0_0, @p0_1, @p0_2, @p0_3), (@p1_0, @p1_1, @p1_2, @p1_3)");

        parameters.Should().NotBeNull();
        parameters!.Get<Guid>("p0_0").Should().Be(id1);
        parameters.Get<string>("p0_1").Should().Be("Product A");
        parameters.Get<decimal>("p0_2").Should().Be(10.5m);
        parameters.Get<bool>("p0_3").Should().BeTrue();

        parameters.Get<Guid>("p1_0").Should().Be(id2);
        parameters.Get<string>("p1_1").Should().Be("Product B");
        parameters.Get<decimal>("p1_2").Should().Be(20.0m);
        parameters.Get<bool>("p1_3").Should().BeFalse();
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
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true) };

        var act = () => BulkBuilder.From(products)
            .Column("id", p => p.Id)
            .Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Table name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Table_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Table(invalidName!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Column_WithInvalidColumnName_ThrowsArgumentException(string? invalidColumn)
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column(invalidColumn!, p => p.Id);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Column_WithNullSelector_ThrowsArgumentNullException()
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column("id", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_WithoutColumns_ThrowsInvalidOperationException()
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true) };

        var act = () => BulkBuilder.From(products)
            .Table("products")
            .Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*column*");
    }

    [Fact]
    public void Build_SingleRow_ProducesCorrectSql()
    {
        var products = new[] { new Product(Guid.NewGuid(), "Widget", 9.99m, true) };

        var (sql, _) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Build();

        sql.Should().Be(
            "INSERT INTO `products` (`id`, `name`, `price`) VALUES (@p0_0, @p0_1, @p0_2)");
    }

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var products = new[]
        {
            new Product(Guid.NewGuid(), "A", 1m, true),
            new Product(Guid.NewGuid(), "B", 2m, false)
        };

        var builder = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id);

        builder.Count.Should().Be(2);
    }

    [Fact]
    public void From_WhenItemsNull_ThrowsArgumentNullException()
    {
        IEnumerable<Product> items = null!;
        var act = () => BulkBuilder.From(items);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }
}
