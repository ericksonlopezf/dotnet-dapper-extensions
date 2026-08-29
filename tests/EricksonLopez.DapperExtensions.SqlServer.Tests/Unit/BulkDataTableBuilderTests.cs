// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.SqlServer.Bulk;
using Xunit;

namespace EricksonLopez.DapperExtensions.SqlServer.Tests.Unit;

public sealed class BulkDataTableBuilderTests
{
    private sealed record Product(Guid Id, string Name, decimal Price, bool IsActive, int? Stock);

    // ─── Argument Validation Tests ────────────────────────────────────────────

    [Fact]
    public void From_WhenItemsNull_ThrowsArgumentNullException()
    {
        IEnumerable<Product> items = null!;
        var act = () => BulkDataTableBuilder.From(items);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Column_WhenColumnNameNullOrWhiteSpace_ThrowsArgumentException(string? invalidColumnName)
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true, 10) };
        var builder = BulkDataTableBuilder.From(products);

        var act = () => builder.Column(invalidColumnName!, p => p.Name);
        act.Should().Throw<ArgumentException>().WithParameterName("columnName");
    }

    [Fact]
    public void Column_WhenSelectorNull_ThrowsArgumentNullException()
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true, 10) };
        var builder = BulkDataTableBuilder.From(products);

        Func<Product, string> nullSelector = null!;
        var act = () => builder.Column("Name", nullSelector);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void Build_WithoutColumns_ThrowsInvalidOperationException()
    {
        var products = new[] { new Product(Guid.NewGuid(), "A", 1m, true, 10) };

        var act = () => BulkDataTableBuilder.From(products).Build();
        act.Should().Throw<InvalidOperationException>().WithMessage("*column*");
    }

    // ─── Building and Mapping Tests ───────────────────────────────────────────

    [Fact]
    public void Build_WithItems_ReturnsPopulatedDataTable()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var products = new[]
        {
            new Product(id1, "Widget", 9.99m, true, 100),
            new Product(id2, "Gadget", 19.99m, false, null)
        };

        var table = BulkDataTableBuilder.From(products)
            .Column("Id", p => p.Id)
            .Column("Name", p => p.Name)
            .Column("Price", p => p.Price)
            .Column("IsActive", p => p.IsActive)
            .Column("Stock", p => p.Stock)
            .Build();

        table.Columns.Count.Should().Be(5);
        table.Rows.Count.Should().Be(2);

        table.Columns["Id"]!.DataType.Should().Be<Guid>();
        table.Columns["Name"]!.DataType.Should().Be<string>();
        table.Columns["Price"]!.DataType.Should().Be<decimal>();
        table.Columns["IsActive"]!.DataType.Should().Be<bool>();
        table.Columns["Stock"]!.DataType.Should().Be<int>();

        table.Rows[0]["Id"].Should().Be(id1);
        table.Rows[0]["Name"].Should().Be("Widget");
        table.Rows[0]["Price"].Should().Be(9.99m);
        table.Rows[0]["IsActive"].Should().Be(true);
        table.Rows[0]["Stock"].Should().Be(100);

        table.Rows[1]["Id"].Should().Be(id2);
        table.Rows[1]["Name"].Should().Be("Gadget");
        table.Rows[1]["Price"].Should().Be(19.99m);
        table.Rows[1]["IsActive"].Should().Be(false);
        table.Rows[1]["Stock"].Should().Be(DBNull.Value);
    }

    [Fact]
    public void Build_WithEmptyCollection_ReturnsEmptyDataTableWithColumns()
    {
        var products = Array.Empty<Product>();

        var table = BulkDataTableBuilder.From(products)
            .Column("Id", p => p.Id)
            .Column("Name", p => p.Name)
            .Build();

        table.Rows.Count.Should().Be(0);
        table.Columns.Count.Should().Be(2);
        table.Columns["Id"]!.DataType.Should().Be<Guid>();
        table.Columns["Name"]!.DataType.Should().Be<string>();
    }

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var products = new[]
        {
            new Product(Guid.NewGuid(), "A", 1m, true, 10),
            new Product(Guid.NewGuid(), "B", 2m, false, 20),
            new Product(Guid.NewGuid(), "C", 3m, true, null)
        };

        var builder = BulkDataTableBuilder.From(products)
            .Column("Id", p => p.Id);

        builder.Count.Should().Be(3);

        var emptyBuilder = BulkDataTableBuilder.From(Array.Empty<Product>());
        emptyBuilder.Count.Should().Be(0);
    }

    [Fact]
    public void Column_WithNullableType_HandlesNullAndNonNullValuesGracefully()
    {
        var products = new[]
        {
            new Product(Guid.NewGuid(), "A", 1m, true, null),
            new Product(Guid.NewGuid(), "B", 2m, false, 42)
        };

        var table = BulkDataTableBuilder.From(products)
            .Column<bool?>("flag", p => p.IsActive)
            .Column<string?>("description", p => p.Stock.HasValue ? p.Name : null)
            .Column<int?>("stock", p => p.Stock)
            .Build();

        table.Columns["flag"]!.DataType.Should().Be<bool>();
        table.Columns["stock"]!.DataType.Should().Be<int>();
        table.Columns["description"]!.DataType.Should().Be<string>();

        table.Rows[0]["flag"].Should().Be(true);
        table.Rows[0]["description"].Should().Be(DBNull.Value);
        table.Rows[0]["stock"].Should().Be(DBNull.Value);

        table.Rows[1]["flag"].Should().Be(false);
        table.Rows[1]["description"].Should().Be("B");
        table.Rows[1]["stock"].Should().Be(42);
    }
}
