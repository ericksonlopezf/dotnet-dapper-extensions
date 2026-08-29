// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions;
using Xunit;

namespace EricksonLopez.DapperExtensions.UnitTests;

public class SqlEntityAttributeTests
{
    [Fact]
    public void SqlEntityAttribute_Instantiation_DefaultTableNameIsNull()
    {
        var attribute = new SqlEntityAttribute();
        attribute.TableName.Should().BeNull();
    }

    [Theory]
    [InlineData("custom_users")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SqlEntityAttribute_TableName_CanBeSetAndRetrieved(string? tableName)
    {
        var attribute = new SqlEntityAttribute
        {
            TableName = tableName
        };
        attribute.TableName.Should().Be(tableName);
    }

    [Fact]
    public void SqlEntityAttribute_HasCorrectAttributeUsage()
    {
        var usage = typeof(SqlEntityAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Struct);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeFalse();
    }

    [SqlEntity(TableName = "test_class")]
    private sealed class DecoratedClass;

    [SqlEntity]
    private readonly struct DecoratedStruct;

    [Fact]
    public void SqlEntityAttribute_CanDecorateClassAndStruct()
    {
        var classAttr = typeof(DecoratedClass).GetCustomAttribute<SqlEntityAttribute>();
        classAttr.Should().NotBeNull();
        classAttr!.TableName.Should().Be("test_class");

        var structAttr = typeof(DecoratedStruct).GetCustomAttribute<SqlEntityAttribute>();
        structAttr.Should().NotBeNull();
        structAttr!.TableName.Should().BeNull();
    }
}

