// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.SqlServer.TypeHandlers;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.SqlServer.Tests.Unit;

public sealed class JsonTypeHandlerTests
{
    private sealed record Metadata(string Role, int Level);

    [Fact]
    public void SetValue_WithNonNullValue_SerializesToJsonStringAndSetsDbType()
    {
        var handler = new JsonTypeHandler<Metadata>();
        var parameter = Substitute.For<IDbDataParameter>();
        var metadata = new Metadata("Admin", 5);

        handler.SetValue(parameter, metadata);

        parameter.Received(1).Value = "{\"role\":\"Admin\",\"level\":5}";
        parameter.Received(1).DbType = DbType.String;
    }

    [Fact]
    public void SetValue_WithNullValue_SetsDBNullAndDbTypeString()
    {
        var handler = new JsonTypeHandler<Metadata>();
        var parameter = Substitute.For<IDbDataParameter>();

        handler.SetValue(parameter, null);

        parameter.Received(1).Value = DBNull.Value;
        parameter.Received(1).DbType = DbType.String;
    }

    [Fact]
    public void Parse_WithDBNull_ReturnsNull()
    {
        var handler = new JsonTypeHandler<Metadata>();
        var result = handler.Parse(DBNull.Value);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNull_ReturnsNull()
    {
        var handler = new JsonTypeHandler<Metadata>();
        var result = handler.Parse(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithValidJsonString_DeserializesCorrectly()
    {
        var handler = new JsonTypeHandler<Metadata>();
        var json = "{\"ROLE\":\"Manager\",\"level\":3}";

        var result = handler.Parse(json);

        result.Should().NotBeNull();
        result!.Role.Should().Be("Manager");
        result.Level.Should().Be(3);
    }

    [Fact]
    public void RegisterJsonHandler_RegistersHandlerInSqlMapperWithoutException()
    {
        var act = () => SqlServerTypeHandlerRegistrar.RegisterJsonHandler<Metadata>();
        act.Should().NotThrow();
    }
}
