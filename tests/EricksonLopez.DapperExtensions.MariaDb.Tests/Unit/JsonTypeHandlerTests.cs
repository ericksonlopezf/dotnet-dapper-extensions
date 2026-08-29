// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.MariaDb.TypeHandlers;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.MariaDb.Tests.Unit;

public sealed class JsonTypeHandlerTests
{
    private sealed record UserProfile(string DisplayName, int Age, bool IsVerified);

    [Fact]
    public void SetValue_WithNull_SetsDBNullAndDbTypeString()
    {
        var handler = new JsonTypeHandler<UserProfile>();
        var parameter = Substitute.For<IDbDataParameter>();

        handler.SetValue(parameter, null);

        parameter.Value.Should().Be(DBNull.Value);
        parameter.DbType.Should().Be(DbType.String);
    }

    [Fact]
    public void SetValue_WithValidObject_SerializesToCamelCaseJson()
    {
        var handler = new JsonTypeHandler<UserProfile>();
        var parameter = Substitute.For<IDbDataParameter>();
        var profile = new UserProfile("Erickson", 30, true);

        handler.SetValue(parameter, profile);

        parameter.Value.Should().NotBeNull();
        var json = parameter.Value!.ToString();
        json.Should().Contain("\"displayName\":\"Erickson\"");
        json.Should().Contain("\"age\":30");
        json.Should().Contain("\"isVerified\":true");
        parameter.DbType.Should().Be(DbType.String);
    }

    [Theory]
    [InlineData(null)]
    public void Parse_WithNull_ReturnsDefault(object? nullValue)
    {
        var handler = new JsonTypeHandler<UserProfile>();
        var result = handler.Parse(nullValue!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithDBNull_ReturnsDefault()
    {
        var handler = new JsonTypeHandler<UserProfile>();
        var result = handler.Parse(DBNull.Value);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithCaseMismatchedJsonString_DeserializesCorrectlyDueToCaseInsensitiveOptions()
    {
        var handler = new JsonTypeHandler<UserProfile>();
        // Using uppercase and mixed-case property names to verify PropertyNameCaseInsensitive = true
        var json = "{\"DISPLAYNAME\":\"Bob\",\"Age\":45,\"ISVERIFIED\":true}";

        var result = handler.Parse(json);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Bob");
        result.Age.Should().Be(45);
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithValidJsonString_DeserializesCorrectly()
    {
        var handler = new JsonTypeHandler<UserProfile>();
        var json = "{\"displayName\":\"Alice\",\"age\":28,\"isVerified\":false}";

        var result = handler.Parse(json);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Alice");
        result.Age.Should().Be(28);
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void RegisterJsonHandler_RegistersHandlerInSqlMapper()
    {
        var act = () => MariaDbTypeHandlerRegistrar.RegisterJsonHandler<UserProfile>();
        act.Should().NotThrow();
    }
}
