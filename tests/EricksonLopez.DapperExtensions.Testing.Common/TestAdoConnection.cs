// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

/// <summary>
/// Reusable ADO.NET connection test double for testing extensions without a live database engine.
/// </summary>
public class TestAdoConnection : DbConnection, IAsyncDisposable
{
    private ConnectionState _state;
    private string _database;

    public TestAdoConnection(ConnectionState initialState = ConnectionState.Open, string database = "TestDb", string serverVersion = "1.0.0")
    {
        _state = initialState;
        _database = database;
        ServerVersion = serverVersion;
    }

    public AdoExecutionSpy Spy { get; } = new();

    public Func<string, DbParameterCollection, DbDataReader>? ReaderFactory { get; set; }
    public Func<string, DbParameterCollection, object?>? ScalarFactory { get; set; }
    public Func<string, DbParameterCollection, int>? NonQueryFactory { get; set; }
    public Func<DbConnection, IsolationLevel, TestAdoTransaction>? TransactionFactory { get; set; }

    public override string ConnectionString { get; set; } = "Data Source=TestFakeServer;Database=TestDb;";
    public override string Database => _database;
    public string CustomDatabase { get => _database; set => _database = value; }
    public string DataSourceValue { get; set; } = "TestDataSource";
    public override string DataSource => DataSourceValue;
    public override string ServerVersion { get; }
    public override ConnectionState State => _state;

    public void SetState(ConnectionState state) => _state = state;
    public void SetDatabase(string database) => _database = database;

    public override void ChangeDatabase(string databaseName) => _database = databaseName;

    public override void Close()
    {
        Spy.CloseCount++;
        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        Spy.OpenCount++;
        _state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        Spy.OpenAsyncCount++;
        Open();
        return Task.CompletedTask;
    }

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public new ValueTask DisposeAsync()
    {
        Spy.DisposeAsyncCount++;
        Close();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        Spy.BeginTransactionCount++;
        var tx = TransactionFactory != null ? TransactionFactory(this, isolationLevel) : new TestAdoTransaction(this, isolationLevel);
        Spy.LastTransaction = tx;
        return tx;
    }

    protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<DbTransaction>(cancellationToken);
        Spy.BeginTransactionAsyncCount++;
        var tx = BeginDbTransaction(isolationLevel);
        return ValueTask.FromResult(tx);
    }

    protected override DbCommand CreateDbCommand()
        => new TestAdoCommand(this);

    internal DbDataReader ExecuteReader(string commandText, DbParameterCollection parameters)
    {
        Spy.LastCommandText = commandText;
        Spy.LastParameters = parameters;
        return ReaderFactory != null
            ? ReaderFactory(commandText, parameters)
            : new TestAdoDataReader([]);
    }

    internal object? ExecuteScalar(string commandText, DbParameterCollection parameters)
    {
        if (State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open to execute command.");
        Spy.LastCommandText = commandText;
        Spy.LastParameters = parameters;
        return ScalarFactory != null ? ScalarFactory(commandText, parameters) : 1;
    }

    internal int ExecuteNonQuery(string commandText, DbParameterCollection parameters)
    {
        if (State != ConnectionState.Open)
            throw new InvalidOperationException("ExecuteNonQuery requires an open connection");
        Spy.LastCommandText = commandText;
        Spy.LastParameters = parameters;
        return NonQueryFactory != null ? NonQueryFactory(commandText, parameters) : 42;
    }
}
