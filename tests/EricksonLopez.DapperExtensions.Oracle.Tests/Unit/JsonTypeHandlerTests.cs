// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.Oracle.TypeHandlers;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.Oracle.Tests.Unit;

public sealed class JsonTypeHandlerTests
{
    private sealed record Metadata(string Tag, int Version, bool IsActive = false);

    private readonly JsonTypeHandler<Metadata> _handler = new();

    [Fact]
    public void SetValue_WithNonNullValue_SerializesToJson()
    {
        var param = Substitute.For<IDbDataParameter>();
        var metadata = new Metadata("active", 2, true);

        _handler.SetValue(param, metadata);

        param.Value.Should().BeOfType<string>();
        var json = (string)param.Value!;
        json.Should().Contain("\"tag\":\"active\"");
        json.Should().Contain("\"version\":2");
        json.Should().Contain("\"isActive\":true");
        param.DbType.Should().Be(DbType.String);
    }

    [Fact]
    public void SetValue_WithNullValue_SetsDbNullValue()
    {
        var param = Substitute.For<IDbDataParameter>();
        _handler.SetValue(param, null);
        param.Value.Should().Be(DBNull.Value);
        param.DbType.Should().Be(DbType.String);
    }

    [Fact]
    public void Parse_WithValidJson_DeserializesCorrectly()
    {
        var json = "{\"tag\":\"active\",\"version\":3,\"isActive\":true}";
        var result = _handler.Parse(json);

        result.Should().NotBeNull();
        result!.Tag.Should().Be("active");
        result.Version.Should().Be(3);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithCaseMismatchedJsonString_DeserializesCorrectlyDueToCaseInsensitiveOptions()
    {
        // Using uppercase and mixed-case property names to verify PropertyNameCaseInsensitive = true
        var json = "{\"TAG\":\"oracle_schema\",\"VERSION\":42,\"ISACTIVE\":true}";

        var result = _handler.Parse(json);

        result.Should().NotBeNull();
        result!.Tag.Should().Be("oracle_schema");
        result.Version.Should().Be(42);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithDbNull_ReturnsDefault()
    {
        var result = _handler.Parse(DBNull.Value);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNull_ReturnsDefault()
    {
        var result = _handler.Parse(null!);
        result.Should().BeNull();
    }

    private sealed record RegisteredItem(string Tag, int Version);

    [Fact]
    public void RegisterJsonHandler_RegistersHandlerInSqlMapper()
    {
        OracleTypeHandlerRegistrar.RegisterJsonHandler<RegisteredItem>();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var item = new RegisteredItem("active", 2);
        var parameters = new DynamicParameters();
        parameters.Add("item", item);
        var json = connection.ExecuteScalar<string>("SELECT @item", parameters);
        json.Should().Contain("\"tag\":\"active\"");
    }
}
