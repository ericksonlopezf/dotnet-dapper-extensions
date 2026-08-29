// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Sqlite.TypeHandlers;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.Sqlite.Tests.Unit;

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
        var json = "{\"TAG\":\"sqlite_db\",\"VERSION\":42,\"ISACTIVE\":true}";

        var result = _handler.Parse(json);

        result.Should().NotBeNull();
        result!.Tag.Should().Be("sqlite_db");
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

    [Fact]
    public void RegisterJsonHandler_RegistersHandlerInSqlMapper()
    {
        var act = () => SqliteTypeHandlerRegistrar.RegisterJsonHandler<Metadata>();
        act.Should().NotThrow();
    }
}
