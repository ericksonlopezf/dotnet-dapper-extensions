// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers;
using Npgsql;
using NpgsqlTypes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.PostgreSql.Tests.Unit;

public sealed class JsonbTypeHandlerTests
{
    private sealed record TestData(int Id, string Name);

    [Fact]
    public void SetValue_WithNullValue_SetsDBNull()
    {
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = Substitute.For<IDbDataParameter>();

        handler.SetValue(parameter, null);

        parameter.Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void SetValue_WithValidObject_SetsJsonAndNpgsqlDbType()
    {
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new NpgsqlParameter();

        var data = new TestData(1, "TestName");
        handler.SetValue(parameter, data);

        parameter.Value.Should().Be("{\"id\":1,\"name\":\"TestName\"}");
        parameter.NpgsqlDbType.Should().Be(NpgsqlDbType.Jsonb);
    }

    [Fact]
    public void SetValue_WithNonNpgsqlParameter_SetsValue()
    {
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = Substitute.For<IDbDataParameter>();

        var data = new TestData(1, "TestName");
        handler.SetValue(parameter, data);

        parameter.Value.Should().Be("{\"id\":1,\"name\":\"TestName\"}");
    }

    [Fact]
    public void Parse_WithDBNull_ReturnsDefault()
    {
        var handler = new JsonbTypeHandler<TestData>();

        var result = handler.Parse(DBNull.Value);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNull_ReturnsDefault()
    {
        var handler = new JsonbTypeHandler<TestData>();

        var result = handler.Parse(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithValidJson_ReturnsDeserializedObject()
    {
        var handler = new JsonbTypeHandler<TestData>();
        var json = "{\"id\":42,\"name\":\"ParsedName\"}";

        var result = handler.Parse(json);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("ParsedName");
    }

    [Fact]
    public void Parse_WithWeirdCaseJson_ReturnsDeserializedObject_UsingCaseInsensitive()
    {
        var handler = new JsonbTypeHandler<TestData>();
        var json = "{\"iD\":42,\"nAmE\":\"ParsedName\"}";

        var result = handler.Parse(json);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("ParsedName");
    }

    [Fact]
    public void NpgsqlTypeHandlerRegistrar_RegisterJsonbHandler_ShouldNotThrow()
    {
        var act = () => NpgsqlTypeHandlerRegistrar.RegisterJsonbHandler<TestData>();
        act.Should().NotThrow();
    }
}



