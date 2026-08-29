// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Oracle.Bulk;
using Xunit;

namespace EricksonLopez.DapperExtensions.Oracle.Tests.Unit;

public sealed class BulkBuilderTests
{
    private sealed record Product(int Id, string Name, decimal Price, bool IsActive);

    [Fact]
    public void Build_WithItems_ReturnsInsertAllSqlAndParameters()
    {
        var products = new[]
        {
            new Product(1, "Widget", 9.99m, true),
            new Product(2, "Gadget", 19.99m, false)
        };

        var (sql, parameters) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Column("price", p => p.Price)
            .Column("is_active", p => p.IsActive)
            .Build();

        sql.Should().NotBeNullOrWhiteSpace();
        sql.Should().StartWith("INSERT ALL");
        sql.Should().Contain("INTO \"products\"");
        sql.Should().Contain("\"id\"");
        sql.Should().Contain("\"name\"");
        sql.Should().Contain("\"price\"");
        sql.Should().Contain("\"is_active\"");
        sql.Should().Contain(":p0_0");
        sql.Should().Contain(":p0_1");
        sql.Should().Contain(":p0_2");
        sql.Should().Contain(":p0_3");
        sql.Should().Contain(":p1_0");
        sql.Should().EndWith("SELECT 1 FROM DUAL");

        parameters.Should().NotBeNull();
        parameters!.Get<int>("p0_0").Should().Be(1);
        parameters.Get<string>("p0_1").Should().Be("Widget");
        parameters.Get<decimal>("p0_2").Should().Be(9.99m);
        parameters.Get<bool>("p0_3").Should().BeTrue();

        parameters.Get<int>("p1_0").Should().Be(2);
        parameters.Get<string>("p1_1").Should().Be("Gadget");
        parameters.Get<decimal>("p1_2").Should().Be(19.99m);
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
        var products = new[] { new Product(1, "A", 1m, true) };

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
        var products = new[] { new Product(1, "A", 1m, true) };
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
        var products = new[] { new Product(1, "A", 1m, true) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column(invalidColumn!, p => p.Id);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Column_WithNullSelector_ThrowsArgumentNullException()
    {
        var products = new[] { new Product(1, "A", 1m, true) };
        var builder = BulkBuilder.From(products);

        var act = () => builder.Column("id", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_WithoutColumns_ThrowsInvalidOperationException()
    {
        var products = new[] { new Product(1, "A", 1m, true) };

        var act = () => BulkBuilder.From(products)
            .Table("products")
            .Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*column*");
    }

    [Fact]
    public void Build_SingleRow_ProducesCorrectInsertAll()
    {
        var products = new[] { new Product(1, "Widget", 9.99m, true) };

        var (sql, _) = BulkBuilder.From(products)
            .Table("products")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Build();

        sql.Should().Contain("INTO \"products\" (\"id\", \"name\") VALUES (:p0_0, :p0_1)");
        var intoCount = sql!.Split("INTO").Length - 1;
        intoCount.Should().Be(1);
    }

    [Fact]
    public void Build_MultipleRows_ProducesMultipleIntoClauses()
    {
        var products = Enumerable.Range(1, 3)
            .Select(i => new Product(i, $"P{i}", i * 10m, true))
            .ToList();

        var (sql, _) = BulkBuilder.From(products)
            .Table("items")
            .Column("id", p => p.Id)
            .Column("name", p => p.Name)
            .Build();

        var intoCount = sql!.Split("INTO").Length - 1;
        intoCount.Should().Be(3);
    }

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var products = new[]
        {
            new Product(1, "A", 1m, true),
            new Product(2, "B", 2m, false)
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
