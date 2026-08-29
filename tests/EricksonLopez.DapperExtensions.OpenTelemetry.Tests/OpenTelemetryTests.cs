// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EricksonLopez.DapperExtensions.OpenTelemetry.Tests;

public class OpenTelemetryTests
{
#nullable disable
    private sealed class NpgsqlCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "pg_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class SqlConnectionCustom : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "sql_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class MySqlCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "mysql_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class MariaDbCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "maria_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class SqliteCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "sqlite_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class OracleCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "oracle_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class GenericCustomConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 30;
        public string Database => "generic_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Open() { }
        public void Dispose() { }
    }
#nullable restore

    private sealed record MetricRecord(string InstrumentName, object Value, Dictionary<string, object?> Tags);

    private static (MeterListener Listener, List<MetricRecord> Records) CreateMeterListener()
    {
        var records = new List<MetricRecord>();
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == DapperDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            records.Add(new MetricRecord(instrument.Name, measurement, dict));
        });

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            records.Add(new MetricRecord(instrument.Name, measurement, dict));
        });

        listener.Start();
        return (listener, records);
    }

    [Fact]
    public void DapperDiagnostics_PropertiesAndConstants()
    {
        DapperDiagnostics.SourceName.Should().Be("EricksonLopez.DapperExtensions");
        DapperDiagnostics.Version.Should().Be("2.0.0");

        DapperDiagnostics.ActivitySource.Should().NotBeNull();
        DapperDiagnostics.ActivitySource.Name.Should().Be("EricksonLopez.DapperExtensions");
        DapperDiagnostics.ActivitySource.Version.Should().Be("2.0.0");

        DapperDiagnostics.Meter.Should().NotBeNull();
        DapperDiagnostics.Meter.Name.Should().Be("EricksonLopez.DapperExtensions");
        DapperDiagnostics.Meter.Version.Should().Be("2.0.0");

        DapperDiagnostics.CommandDurationHistogram.Should().NotBeNull();
        DapperDiagnostics.CommandDurationHistogram.Name.Should().Be("db.client.commands.duration");
        DapperDiagnostics.CommandDurationHistogram.Unit.Should().Be("ms");
        DapperDiagnostics.CommandDurationHistogram.Description.Should().Be("Duration of database command executions in milliseconds");

        DapperDiagnostics.CommandExecutionsCounter.Should().NotBeNull();
        DapperDiagnostics.CommandExecutionsCounter.Name.Should().Be("db.client.commands.count");
        DapperDiagnostics.CommandExecutionsCounter.Unit.Should().Be("{command}");
        DapperDiagnostics.CommandExecutionsCounter.Description.Should().Be("Total count of executed database commands");

        DapperDiagnostics.BulkRowsCounter.Should().NotBeNull();
        DapperDiagnostics.BulkRowsCounter.Name.Should().Be("db.client.bulk.rows");
        DapperDiagnostics.BulkRowsCounter.Unit.Should().Be("{row}");
        DapperDiagnostics.BulkRowsCounter.Description.Should().Be("Total rows affected by bulk operations");

        DapperDiagnostics.ResilienceRetriesCounter.Should().NotBeNull();
        DapperDiagnostics.ResilienceRetriesCounter.Name.Should().Be("db.client.resilience.retries");
        DapperDiagnostics.ResilienceRetriesCounter.Unit.Should().Be("{retry}");
        DapperDiagnostics.ResilienceRetriesCounter.Description.Should().Be("Total resilience retries executed for database operations");

        DapperDiagnostics.TagDbSystem.Should().Be("db.system");
        DapperDiagnostics.TagDbName.Should().Be("db.name");
        DapperDiagnostics.TagDbStatement.Should().Be("db.statement");
        DapperDiagnostics.TagDbOperation.Should().Be("db.operation");
        DapperDiagnostics.TagDbRowsAffected.Should().Be("db.rows_affected");
        DapperDiagnostics.TagServerAddress.Should().Be("server.address");
        DapperDiagnostics.TagErrorType.Should().Be("error.type");
    }

    [Fact]
    public void DapperOpenTelemetryOptions_Defaults_And_Setters()
    {
        var options = new DapperOpenTelemetryOptions();

        options.CaptureSqlStatements.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.MaxStatementLength.Should().Be(4096);

        options.CaptureSqlStatements = false;
        options.EnableMetrics = false;
        options.MaxStatementLength = 2048;

        options.CaptureSqlStatements.Should().BeFalse();
        options.EnableMetrics.Should().BeFalse();
        options.MaxStatementLength.Should().Be(2048);
    }

    [Fact]
    public void AddDapperOpenTelemetry_ServiceCollectionExtensions()
    {
        IServiceCollection nullServices = null!;
        var act = () => nullServices.AddDapperOpenTelemetry();
        act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("services");

        var services = new ServiceCollection();
        services.AddDapperOpenTelemetry();

        var provider = services.BuildServiceProvider();
        var defaultOptions = provider.GetRequiredService<DapperOpenTelemetryOptions>();
        defaultOptions.Should().NotBeNull();
        defaultOptions.CaptureSqlStatements.Should().BeTrue();
        defaultOptions.EnableMetrics.Should().BeTrue();
        defaultOptions.MaxStatementLength.Should().Be(4096);

        var customServices = new ServiceCollection();
        customServices.AddDapperOpenTelemetry(opt =>
        {
            opt.CaptureSqlStatements = false;
            opt.MaxStatementLength = 1024;
            opt.EnableMetrics = false;
        });

        var customProvider = customServices.BuildServiceProvider();
        var customOptions = customProvider.GetRequiredService<DapperOpenTelemetryOptions>();
        customOptions.Should().NotBeNull();
        customOptions.CaptureSqlStatements.Should().BeFalse();
        customOptions.EnableMetrics.Should().BeFalse();
        customOptions.MaxStatementLength.Should().Be(1024);
    }

    [Fact]
    public async Task ResolveDbSystem_CoversAllDialects()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = a => activities.Add(a)
        };
        ActivitySource.AddActivityListener(listener);

        await new NpgsqlCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new SqlConnectionCustom().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new MySqlCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new MariaDbCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new SqliteCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new OracleCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));
        await new GenericCustomConnection().TraceBulkOperationAsync("Test", "t", ct => Task.FromResult(1));

        activities.Should().HaveCount(7);
        activities[0].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("postgresql");
        activities[1].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("mssql");
        activities[2].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("mysql");
        activities[3].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("mariadb");
        activities[4].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("sqlite");
        activities[5].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("oracle");
        activities[6].GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("other_sql");
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_Guards()
    {
        IDbConnection nullConn = null!;
        using var realConn = new SqliteConnection("Data Source=:memory:");

        var act1 = () => nullConn.ExecuteWithTelemetryAsync("SELECT 1;");
        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var act2 = () => realConn.ExecuteWithTelemetryAsync(null!);
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sql");

        var act3 = () => realConn.ExecuteWithTelemetryAsync("   ");
        await act3.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_Success_WithActivityAndMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => activities.Add(a)
            };
            ActivitySource.AddActivityListener(listener);

            await connection.ExecuteWithTelemetryAsync("CREATE TABLE items (id INT); INSERT INTO items VALUES (1);");
            var rows = await connection.ExecuteWithTelemetryAsync("UPDATE items SET id = 2 WHERE id = 1;");
            rows.Should().Be(1);

            activities.Should().HaveCount(2);
            var activity = activities[1];
            activity.OperationName.Should().Be("Execute main");
            activity.Kind.Should().Be(ActivityKind.Client);
            activity.Status.Should().Be(ActivityStatusCode.Ok);
            activity.GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("sqlite");
            activity.GetTagItem(DapperDiagnostics.TagDbOperation).Should().Be("Execute");
            activity.GetTagItem(DapperDiagnostics.TagDbName).Should().Be("main");
            activity.GetTagItem(DapperDiagnostics.TagDbStatement).Should().Be("UPDATE items SET id = 2 WHERE id = 1;");
            activity.GetTagItem(DapperDiagnostics.TagDbRowsAffected).Should().Be(1);

            var countMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.count");
            countMetric.Should().NotBeNull();
            countMetric!.Value.Should().Be(1L);
            countMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Execute");
            countMetric.Tags["status"].Should().Be("ok");

            var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration");
            durationMetric.Should().NotBeNull();
            durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Execute");
        }
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_Failure_RecordsErrorActivityAndMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => activities.Add(a)
            };
            ActivitySource.AddActivityListener(listener);

            var act = () => connection.ExecuteWithTelemetryAsync("SYNTAX ERROR INVALID SQL;");
            var exception = await act.Should().ThrowAsync<SqliteException>();

            activities.Should().ContainSingle();
            var activity = activities[0];
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be(exception.Which.Message);
            activity.GetTagItem(DapperDiagnostics.TagErrorType).Should().Be(typeof(SqliteException).FullName);
            activity.Events.Should().Contain(e => e.Name == "exception");

            var countMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.count");
            countMetric.Should().NotBeNull();
            countMetric!.Value.Should().Be(1L);
            countMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Execute");
            countMetric.Tags["status"].Should().Be("error");

            var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration");
            durationMetric.Should().NotBeNull();
            durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Execute");
        }
    }

    [Fact]
    public async Task QueryWithTelemetryAsync_Guards()
    {
        IDbConnection nullConn = null!;
        using var realConn = new SqliteConnection("Data Source=:memory:");

        var act1 = () => nullConn.QueryWithTelemetryAsync<int>("SELECT 1;");
        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var act2 = () => realConn.QueryWithTelemetryAsync<int>(null!);
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sql");

        var act3 = () => realConn.QueryWithTelemetryAsync<int>("   ");
        await act3.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task QueryWithTelemetryAsync_Success_WithActivityAndMetrics()
    {
        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            // 1. Without Activity listener
            using (var connection1 = new SqliteConnection("Data Source=:memory:"))
            {
                await connection1.OpenAsync();
                var noListenerResult = (await connection1.QueryWithTelemetryAsync<int>("SELECT 42;")).ToList();
                noListenerResult.Should().Equal(42);
            }

            // 2. With Activity listener
            using (var connection2 = new SqliteConnection("Data Source=:memory:"))
            {
                await connection2.OpenAsync();
                var activities = new List<Activity>();
                using var listener = new ActivityListener
                {
                    ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    ActivityStarted = a => activities.Add(a)
                };
                ActivitySource.AddActivityListener(listener);

                var result = (await connection2.QueryWithTelemetryAsync<int>("SELECT 100;")).ToList();
                result.Should().Equal(100);

                activities.Should().ContainSingle();
                var activity = activities[0];
                activity.OperationName.Should().Be("Query main");
                activity.Kind.Should().Be(ActivityKind.Client);
                activity.Status.Should().Be(ActivityStatusCode.Ok);
                activity.GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("sqlite");
                activity.GetTagItem(DapperDiagnostics.TagDbOperation).Should().Be("Query");
                activity.GetTagItem(DapperDiagnostics.TagDbName).Should().Be("main");
                activity.GetTagItem(DapperDiagnostics.TagDbStatement).Should().Be("SELECT 100;");
                activity.GetTagItem(DapperDiagnostics.TagDbRowsAffected).Should().BeNull();

                var countMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.count");
                countMetric.Should().NotBeNull();
                countMetric!.Value.Should().Be(1L);
                countMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Query");
                countMetric.Tags["status"].Should().Be("ok");

                var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration");
                durationMetric.Should().NotBeNull();
                durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Query");
            }
        }
    }

    [Fact]
    public async Task QueryWithTelemetryAsync_Failure_RecordsErrorActivityAndMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => activities.Add(a)
            };
            ActivitySource.AddActivityListener(listener);

            var act = () => connection.QueryWithTelemetryAsync<int>("INVALID QUERY;");
            var exception = await act.Should().ThrowAsync<SqliteException>();

            activities.Should().ContainSingle();
            var activity = activities[0];
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be(exception.Which.Message);
            activity.GetTagItem(DapperDiagnostics.TagErrorType).Should().Be(typeof(SqliteException).FullName);
            activity.Events.Should().Contain(e => e.Name == "exception");

            var countMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.count");
            countMetric.Should().NotBeNull();
            countMetric!.Value.Should().Be(1L);
            countMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Query");
            countMetric.Tags["status"].Should().Be("error");

            var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration");
            durationMetric.Should().NotBeNull();
            durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("Query");
        }
    }

    [Fact]
    public async Task TraceBulkOperationAsync_Guards()
    {
        IDbConnection nullConn = null!;
        using var realConn = new SqliteConnection("Data Source=:memory:");

        var act1 = () => nullConn.TraceBulkOperationAsync("Insert", "users", ct => Task.FromResult(1));
        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");

        var act2 = () => realConn.TraceBulkOperationAsync("Insert", "users", null!);
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("bulkAction");
    }

    [Fact]
    public async Task TraceBulkOperationAsync_Success_WithActivityAndMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            // 1. Without Activity listener
            var noListenerRows = await connection.TraceBulkOperationAsync("Merge", "orders", ct => Task.FromResult(77));
            noListenerRows.Should().Be(77);

            // 2. With Activity listener
            var activities = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => activities.Add(a)
            };
            ActivitySource.AddActivityListener(listener);

            var rows = await connection.TraceBulkOperationAsync("BulkInsert", "products", ct => Task.FromResult(42));
            rows.Should().Be(42);

            activities.Should().ContainSingle();
            var activity = activities[0];
            activity.OperationName.Should().Be("Bulk BulkInsert products");
            activity.Kind.Should().Be(ActivityKind.Client);
            activity.Status.Should().Be(ActivityStatusCode.Ok);
            activity.GetTagItem(DapperDiagnostics.TagDbSystem).Should().Be("sqlite");
            activity.GetTagItem(DapperDiagnostics.TagDbOperation).Should().Be("BulkInsert");
            activity.GetTagItem(DapperDiagnostics.TagDbName).Should().Be("main");
            activity.GetTagItem("db.table").Should().Be("products");
            activity.GetTagItem(DapperDiagnostics.TagDbRowsAffected).Should().Be(42);

            var bulkMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.bulk.rows" && (string?)r.Tags.GetValueOrDefault(DapperDiagnostics.TagDbOperation) == "BulkInsert");
            bulkMetric.Should().NotBeNull();
            bulkMetric!.Value.Should().Be(42L);
            bulkMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("BulkInsert");

            var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration" && (string?)r.Tags.GetValueOrDefault(DapperDiagnostics.TagDbOperation) == "BulkInsert");
            durationMetric.Should().NotBeNull();
            durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("BulkInsert");
        }
    }

    [Fact]
    public async Task TraceBulkOperationAsync_Failure_RecordsErrorActivityAndMetrics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var (meterListener, metricRecords) = CreateMeterListener();
        using (meterListener)
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DapperDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => activities.Add(a)
            };
            ActivitySource.AddActivityListener(listener);

            var act = () => connection.TraceBulkOperationAsync("BulkInsert", "products", ct => throw new InvalidOperationException("Bulk timeout"));
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();

            activities.Should().ContainSingle();
            var activity = activities[0];
            activity.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("Bulk timeout");
            activity.GetTagItem(DapperDiagnostics.TagErrorType).Should().Be(typeof(InvalidOperationException).FullName);
            activity.Events.Should().Contain(e => e.Name == "exception");

            var countMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.count");
            countMetric.Should().NotBeNull();
            countMetric!.Value.Should().Be(1L);
            countMetric.Tags[DapperDiagnostics.TagDbOperation].Should().Be("BulkInsert");
            countMetric.Tags["status"].Should().Be("error");

            var durationMetric = metricRecords.FirstOrDefault(r => r.InstrumentName == "db.client.commands.duration");
            durationMetric.Should().NotBeNull();
            durationMetric!.Tags[DapperDiagnostics.TagDbOperation].Should().Be("BulkInsert");
        }
    }

    [Fact]
    public void AddDapperOpenTelemetry_Guards_And_Registration()
    {
        IServiceCollection nullServices = null!;
        var act = () => nullServices.AddDapperOpenTelemetry();
        var ex = act.Should().Throw<ArgumentNullException>().WithParameterName("services").Which;
        ex.TargetSite?.DeclaringType?.Name.Should().Be(nameof(DapperOpenTelemetryServiceCollectionExtensions));

        var services = new ServiceCollection();
        services.AddDapperOpenTelemetry(options =>
        {
            options.CaptureSqlStatements = false;
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<DapperOpenTelemetryOptions>();
        options.CaptureSqlStatements.Should().BeFalse();
    }
}
