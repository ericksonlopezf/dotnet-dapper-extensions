// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Testing.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.HealthChecks.Tests;

public sealed class HealthChecksTests
{
    private static readonly string[] _customTags = ["custom_tag"];
    private static readonly string[] _customPgTags = ["custom_pg"];
    private static readonly string[] _customSqlServerTags = ["custom_sqlserver"];
    private static readonly string[] _customOracleTags = ["custom_oracle"];
    private static readonly string[] _customMySqlTags = ["custom_mysql"];
    private static readonly string[] _customSqliteTags = ["custom_sqlite"];

    private static readonly string[] _defaultPgExpected = ["db", "postgresql", "sql"];
    private static readonly string[] _defaultSqlServerExpected = ["db", "sqlserver", "sql"];
    private static readonly string[] _defaultOracleExpected = ["db", "oracle", "sql"];
    private static readonly string[] _defaultMySqlExpected = ["db", "mysql", "sql"];
    private static readonly string[] _defaultSqliteExpected = ["db", "sqlite", "sql"];

#nullable disable
    private sealed class NonDbConnection : IDbConnection
    {
        public bool WasOpened { get; private set; }
        public bool WasDisposed { get; private set; }
        public string DatabaseValue { get; set; }

        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => DatabaseValue;
        public ConnectionState State { get; set; } = ConnectionState.Closed;

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() => State = ConnectionState.Closed;
        public IDbCommand CreateCommand()
        {
            var cmd = Substitute.For<IDbCommand>();
            var paramCol = Substitute.For<IDataParameterCollection>();
            cmd.Parameters.Returns(paramCol);
            cmd.ExecuteScalar().Returns(1);
            return cmd;
        }

        public void Open()
        {
            WasOpened = true;
            State = ConnectionState.Open;
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }
#nullable restore

    [Fact]
    public void DapperHealthCheckOptions_DefaultValues_And_Setters()
    {
        var options = new DapperHealthCheckOptions();

        options.CommandText.Should().Be("SELECT 1;");
        options.DegradedThreshold.Should().Be(TimeSpan.FromMilliseconds(500));
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));

        options.CommandText = "SELECT 42;";
        options.DegradedThreshold = TimeSpan.FromSeconds(1);
        options.Timeout = TimeSpan.FromSeconds(10);

        options.CommandText.Should().Be("SELECT 42;");
        options.DegradedThreshold.Should().Be(TimeSpan.FromSeconds(1));
        options.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DapperHealthCheck_Constructor_Guards()
    {
        Func<CancellationToken, Task<IDbConnection>> nullFactory = null!;
        var act = () => new DapperHealthCheck(nullFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");

        var check = new DapperHealthCheck(ct => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")));
        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenQuerySucceedsWithinThreshold()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(connection),
            new DapperHealthCheckOptions
            {
                CommandText = "SELECT 1;",
                DegradedThreshold = TimeSpan.FromSeconds(10)
            });

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().StartWith("Database probe succeeded in ");
        result.Description.Should().EndWith("ms.");
        result.Data.Should().ContainKey("latency_ms");
        result.Data.Should().ContainKey("database");
        result.Data["database"].Should().Be("main");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullDatabaseName_SetsEmptyStringInData()
    {
        var fakeConn = new TestAdoConnection { CustomDatabase = null! };
        fakeConn.SetState(ConnectionState.Open);
        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(fakeConn),
            new DapperHealthCheckOptions { DegradedThreshold = TimeSpan.FromSeconds(10) });

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["database"].Should().Be(string.Empty);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenLatencyExceedsThreshold()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(connection),
            new DapperHealthCheckOptions
            {
                CommandText = "SELECT 1;",
                DegradedThreshold = TimeSpan.Zero
            });

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().StartWith("Database response latency (");
        result.Description.Should().Contain("exceeded degraded threshold (0.0ms).");
        result.Data.Should().ContainKey("latency_ms");
        result.Data.Should().ContainKey("database");
    }

    [Fact]
    public async Task CheckHealthAsync_OpensClosedDbConnection_Asynchronously()
    {
        var fakeConn = new TestAdoConnection(initialState: ConnectionState.Closed);

        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(fakeConn),
            new DapperHealthCheckOptions());

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        fakeConn.WasOpenAsyncCalled.Should().BeTrue();
        fakeConn.WasDisposeAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_OpensClosedNonDbConnection_Synchronously()
    {
        var nonDbConn = new NonDbConnection { DatabaseValue = "NonDb", State = ConnectionState.Closed };

        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(nonDbConn),
            new DapperHealthCheckOptions());

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        nonDbConn.WasOpened.Should().BeTrue();
        nonDbConn.WasDisposed.Should().BeTrue();
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Async operations require use of a DbConnection");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionAlreadyOpen_DoesNotCallOpen()
    {
        var fakeConn = new TestAdoConnection(initialState: ConnectionState.Open);

        var check = new DapperHealthCheck(
            ct => Task.FromResult<IDbConnection>(fakeConn),
            new DapperHealthCheckOptions());

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        fakeConn.WasOpenAsyncCalled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenFactoryThrowsException()
    {
        var expectedEx = new InvalidOperationException("Connection pool exhausted");
        var check = new DapperHealthCheck(
            ct => Task.FromException<IDbConnection>(expectedEx),
            new DapperHealthCheckOptions());

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database health probe failed: Connection pool exhausted");
        result.Exception.Should().BeSameAs(expectedEx);
        result.Data.Should().ContainKey("latency_ms");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenTimedOutOrCancelled()
    {
        var check = new DapperHealthCheck(
            async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                return new SqliteConnection("Data Source=:memory:");
            },
            new DapperHealthCheckOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            });

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database health probe timed out after 0.05s.");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenLatencyEqualsDegradedThreshold_ReturnsDegraded()
    {
        var fakeTime = new FakeTimeProvider();
        var fakeConn = new TestAdoConnection(initialState: ConnectionState.Open);

        var check = new DapperHealthCheck(
            ct =>
            {
                fakeTime.Advance(TimeSpan.FromMilliseconds(500));
                return Task.FromResult<IDbConnection>(fakeConn);
            },
            new DapperHealthCheckOptions
            {
                DegradedThreshold = TimeSpan.FromMilliseconds(500)
            },
            fakeTime);

        var context = new HealthCheckContext();
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("exceeded degraded threshold");
    }

    [Fact]
    public void AddDapperHealthCheck_Guards()
    {
        IHealthChecksBuilder nullBuilder = null!;
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        var act1 = () => nullBuilder.AddDapperHealthCheck("test", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act1.Should().Throw<ArgumentNullException>().WithParameterName("builder");

        var act2 = () => builder.AddDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act2.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var act3 = () => builder.AddDapperHealthCheck("   ", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act3.Should().Throw<ArgumentException>().WithParameterName("name");

        var act4 = () => builder.AddDapperHealthCheck("test", null!);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    [Fact]
    public async Task AddDapperHealthCheck_RegistersAndExecutesHealthCheck()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddDapperHealthCheck(
            "custom_sql",
            (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")),
            configure: opts =>
            {
                opts.CommandText = "SELECT 99;";
                opts.DegradedThreshold = TimeSpan.FromSeconds(3);
            },
            failureStatus: HealthStatus.Degraded,
            tags: _customTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var registration = options.Registrations.Single(r => r.Name == "custom_sql");
        registration.FailureStatus.Should().Be(HealthStatus.Degraded);
        registration.Tags.Should().Contain("custom_tag");

        var healthCheck = registration.Factory(provider);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddPostgreSqlDapperHealthCheck_RegistersWithDefaultTags_AndAllowsOverrides()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddPostgreSqlDapperHealthCheck("pg_default", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        builder.AddPostgreSqlDapperHealthCheck(
            "pg_custom",
            (sp, ct) => Task.FromResult<IDbConnection>(null!),
            tags: _customPgTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var defaultReg = options.Registrations.Single(r => r.Name == "pg_default");
        defaultReg.Tags.Should().BeEquivalentTo(_defaultPgExpected);

        var customReg = options.Registrations.Single(r => r.Name == "pg_custom");
        customReg.Tags.Should().BeEquivalentTo(_customPgTags);
    }

    [Fact]
    public void AddSqlServerDapperHealthCheck_RegistersWithDefaultTags_AndAllowsOverrides()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddSqlServerDapperHealthCheck("sqlserver_default", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        builder.AddSqlServerDapperHealthCheck(
            "sqlserver_custom",
            (sp, ct) => Task.FromResult<IDbConnection>(null!),
            tags: _customSqlServerTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var defaultReg = options.Registrations.Single(r => r.Name == "sqlserver_default");
        defaultReg.Tags.Should().BeEquivalentTo(_defaultSqlServerExpected);

        var customReg = options.Registrations.Single(r => r.Name == "sqlserver_custom");
        customReg.Tags.Should().BeEquivalentTo(_customSqlServerTags);
    }

    [Fact]
    public async Task AddOracleDapperHealthCheck_RegistersWithDefaultQueryAndTags_AndAllowsOverrides()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        DapperHealthCheckOptions? defaultCapturedOptions = null;
        builder.AddOracleDapperHealthCheck(
            "oracle_default",
            (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")),
            configure: opt => defaultCapturedOptions = opt);

        DapperHealthCheckOptions? customCapturedOptions = null;
        builder.AddOracleDapperHealthCheck(
            "oracle_custom",
            (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")),
            configure: opt =>
            {
                customCapturedOptions = opt;
                opt.CommandText = "SELECT 1;";
            },
            tags: _customOracleTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var defaultReg = options.Registrations.Single(r => r.Name == "oracle_default");
        defaultReg.Tags.Should().BeEquivalentTo(_defaultOracleExpected);
        defaultCapturedOptions.Should().NotBeNull();
        defaultCapturedOptions!.CommandText.Should().Be("SELECT 1 FROM DUAL");

        var customReg = options.Registrations.Single(r => r.Name == "oracle_custom");
        customReg.Tags.Should().BeEquivalentTo(_customOracleTags);
        customCapturedOptions.Should().NotBeNull();
        customCapturedOptions!.CommandText.Should().Be("SELECT 1;");

        var check = customReg.Factory(provider);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AddMySqlDapperHealthCheck_RegistersWithDefaultTags_AndAllowsOverrides()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddMySqlDapperHealthCheck("mysql_default", (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")));
        builder.AddMySqlDapperHealthCheck(
            "mysql_custom",
            (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")),
            tags: _customMySqlTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var defaultReg = options.Registrations.Single(r => r.Name == "mysql_default");
        defaultReg.Tags.Should().BeEquivalentTo(_defaultMySqlExpected);

        var check = defaultReg.Factory(provider);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.Should().Be(HealthStatus.Healthy);

        var customReg = options.Registrations.Single(r => r.Name == "mysql_custom");
        customReg.Tags.Should().BeEquivalentTo(_customMySqlTags);
    }

    [Fact]
    public async Task AddSqliteDapperHealthCheck_RegistersWithDefaultTags_AndAllowsOverrides()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddSqliteDapperHealthCheck("sqlite_default", (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")));
        builder.AddSqliteDapperHealthCheck(
            "sqlite_custom",
            (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")),
            tags: _customSqliteTags);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var defaultReg = options.Registrations.Single(r => r.Name == "sqlite_default");
        defaultReg.Tags.Should().BeEquivalentTo(_defaultSqliteExpected);

        var check = defaultReg.Factory(provider);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.Should().Be(HealthStatus.Healthy);

        var customReg = options.Registrations.Single(r => r.Name == "sqlite_custom");
        customReg.Tags.Should().BeEquivalentTo(_customSqliteTags);
    }

    [Fact]
    public async Task AddPostgreSqlAndSqlServerDapperHealthCheck_FactoryExecution()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        builder.AddPostgreSqlDapperHealthCheck("pg_exec", (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")));
        builder.AddSqlServerDapperHealthCheck("sqlserver_exec", (sp, ct) => Task.FromResult<IDbConnection>(new SqliteConnection("Data Source=:memory:")));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var pgReg = options.Registrations.Single(r => r.Name == "pg_exec");
        var pgCheck = pgReg.Factory(provider);
        var pgResult = await pgCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        pgResult.Status.Should().Be(HealthStatus.Healthy);

        var sqlServerReg = options.Registrations.Single(r => r.Name == "sqlserver_exec");
        var sqlServerCheck = sqlServerReg.Factory(provider);
        var sqlServerResult = await sqlServerCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        sqlServerResult.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void BuilderExtensions_Guards()
    {
        IHealthChecksBuilder nullBuilder = null!;
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Func<IServiceProvider, CancellationToken, Task<IDbConnection>> nullFactory = null!;

        var act1 = () => nullBuilder.AddDapperHealthCheck("db", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act1.Should().Throw<ArgumentNullException>().WithParameterName("builder");

        var act2 = () => builder.AddDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act2.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var act3 = () => builder.AddDapperHealthCheck("   ", (sp, ct) => Task.FromResult<IDbConnection>(null!));
        act3.Should().Throw<ArgumentException>().WithParameterName("name");

        var act4 = () => builder.AddDapperHealthCheck("db", nullFactory);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");

        // Specific provider guards
        var actPg = () => builder.AddPostgreSqlDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        actPg.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var actSqlServer = () => builder.AddSqlServerDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        actSqlServer.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var actMySql = () => builder.AddMySqlDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        actMySql.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var actSqlite = () => builder.AddSqliteDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        actSqlite.Should().Throw<ArgumentNullException>().WithParameterName("name");

        var actOracle = () => builder.AddOracleDapperHealthCheck(null!, (sp, ct) => Task.FromResult<IDbConnection>(null!));
        actOracle.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }
}
