// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.DependencyInjection;
using EricksonLopez.DapperExtensions.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.DapperExtensions.DependencyInjection.Tests;

public class ServiceCollectionExtensionsTests
{
    private static bool HasRegisteredTypeHandler<T>()
    {
        var field = typeof(SqlMapper).GetField("typeHandlers", BindingFlags.Static | BindingFlags.NonPublic);
        if (field?.GetValue(null) is IDictionary dict)
        {
            return dict.Contains(typeof(T));
        }
        return false;
    }

    [Fact]
    public void DapperExtensionsOptions_DefaultValues_AreTrue()
    {
        var options = new DapperExtensionsOptions();

        options.RegisterStandardTypeHandlers.Should().BeTrue();
        options.RegisterTransientErrorDetectors.Should().BeTrue();

        options.RegisterStandardTypeHandlers = false;
        options.RegisterTransientErrorDetectors = false;

        options.RegisterStandardTypeHandlers.Should().BeFalse();
        options.RegisterTransientErrorDetectors.Should().BeFalse();
    }

    [Fact]
    public void AddDapperExtensions_WithNullServices_ThrowsArgumentNullException_WithCorrectParamName()
    {
        IServiceCollection services = null!;
        var act1 = () => services.AddDapperExtensions();
        act1.Should().Throw<ArgumentNullException>().WithParameterName("services");

        var act2 = () => services.AddDapperExtensions(opts =>
        {
            opts.RegisterStandardTypeHandlers = false;
            opts.RegisterTransientErrorDetectors = false;
        });
        act2.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddDapperTypeHandlers_WithNullServices_ThrowsArgumentNullException_WithCorrectParamName()
    {
        IServiceCollection services = null!;
        var act = () => services.AddDapperTypeHandlers();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddDapperTransientErrorDetectors_WithNullServices_ThrowsArgumentNullException_WithCorrectParamName()
    {
        IServiceCollection services = null!;
        var act = () => services.AddDapperTransientErrorDetectors();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddDapperExtensions_WithDefaultOptions_RegistersDetectorsAndHandlersAndReturnsSameCollection()
    {
        SqlMapper.ResetTypeHandlers();

        var services = new ServiceCollection();
        var result = services.AddDapperExtensions();

        result.Should().BeSameAs(services);

        HasRegisteredTypeHandler<DateOnly>().Should().BeTrue();
        HasRegisteredTypeHandler<TimeOnly>().Should().BeTrue();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<SqlServerTransientErrorDetector>()
            .Should().BeSameAs(SqlServerTransientErrorDetector.Default);
        provider.GetRequiredService<PostgreSqlTransientErrorDetector>()
            .Should().BeSameAs(PostgreSqlTransientErrorDetector.Default);
        provider.GetRequiredService<MySqlTransientErrorDetector>()
            .Should().BeSameAs(MySqlTransientErrorDetector.Default);
        provider.GetRequiredService<SqliteTransientErrorDetector>()
            .Should().BeSameAs(SqliteTransientErrorDetector.Default);
        provider.GetRequiredService<OracleTransientErrorDetector>()
            .Should().BeSameAs(OracleTransientErrorDetector.Default);
    }

    [Fact]
    public void AddDapperExtensions_WithConfigureAction_DisablingAll_DoesNotRegisterAnything()
    {
        SqlMapper.ResetTypeHandlers();

        var services = new ServiceCollection();
        var result = services.AddDapperExtensions(options =>
        {
            options.RegisterStandardTypeHandlers = false;
            options.RegisterTransientErrorDetectors = false;
        });

        result.Should().BeSameAs(services);
        services.Should().BeEmpty();

        HasRegisteredTypeHandler<DateOnly>().Should().BeFalse();
        HasRegisteredTypeHandler<TimeOnly>().Should().BeFalse();

        var provider = services.BuildServiceProvider();

        provider.GetService<SqlServerTransientErrorDetector>().Should().BeNull();
        provider.GetService<PostgreSqlTransientErrorDetector>().Should().BeNull();
        provider.GetService<MySqlTransientErrorDetector>().Should().BeNull();
        provider.GetService<SqliteTransientErrorDetector>().Should().BeNull();
        provider.GetService<OracleTransientErrorDetector>().Should().BeNull();
    }

    [Fact]
    public void AddDapperExtensions_WithOnlyStandardTypeHandlersEnabled_RegistersHandlersOnly()
    {
        SqlMapper.ResetTypeHandlers();

        var services = new ServiceCollection();
        var result = services.AddDapperExtensions(options =>
        {
            options.RegisterStandardTypeHandlers = true;
            options.RegisterTransientErrorDetectors = false;
        });

        result.Should().BeSameAs(services);
        services.Should().BeEmpty();

        HasRegisteredTypeHandler<DateOnly>().Should().BeTrue();
        HasRegisteredTypeHandler<TimeOnly>().Should().BeTrue();

        var provider = services.BuildServiceProvider();
        provider.GetService<SqlServerTransientErrorDetector>().Should().BeNull();
    }

    [Fact]
    public void AddDapperExtensions_WithOnlyTransientErrorDetectorsEnabled_RegistersDetectorsOnly()
    {
        SqlMapper.ResetTypeHandlers();

        var services = new ServiceCollection();
        var result = services.AddDapperExtensions(options =>
        {
            options.RegisterStandardTypeHandlers = false;
            options.RegisterTransientErrorDetectors = true;
        });

        result.Should().BeSameAs(services);
        services.Should().HaveCount(5);

        HasRegisteredTypeHandler<DateOnly>().Should().BeFalse();
        HasRegisteredTypeHandler<TimeOnly>().Should().BeFalse();

        var provider = services.BuildServiceProvider();
        provider.GetService<SqlServerTransientErrorDetector>().Should().NotBeNull();
    }

    [Fact]
    public void AddDapperTypeHandlers_RegistersStandardHandlersAndReturnsSameCollection()
    {
        SqlMapper.ResetTypeHandlers();

        var services = new ServiceCollection();
        var result = services.AddDapperTypeHandlers();

        result.Should().BeSameAs(services);

        HasRegisteredTypeHandler<DateOnly>().Should().BeTrue();
        HasRegisteredTypeHandler<TimeOnly>().Should().BeTrue();
    }

    [Fact]
    public void AddDapperTransientErrorDetectors_RegistersAllDescriptorsAsSingletonsAndResolvesDefaults()
    {
        var services = new ServiceCollection();
        var result = services.AddDapperTransientErrorDetectors();

        result.Should().BeSameAs(services);
        services.Should().HaveCount(5);

        services.All(sd => sd.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<SqlServerTransientErrorDetector>()
            .Should().BeSameAs(SqlServerTransientErrorDetector.Default);
        provider.GetRequiredService<PostgreSqlTransientErrorDetector>()
            .Should().BeSameAs(PostgreSqlTransientErrorDetector.Default);
        provider.GetRequiredService<MySqlTransientErrorDetector>()
            .Should().BeSameAs(MySqlTransientErrorDetector.Default);
        provider.GetRequiredService<SqliteTransientErrorDetector>()
            .Should().BeSameAs(SqliteTransientErrorDetector.Default);
        provider.GetRequiredService<OracleTransientErrorDetector>()
            .Should().BeSameAs(OracleTransientErrorDetector.Default);
    }
}
