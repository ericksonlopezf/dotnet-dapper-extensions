// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

/// <summary>
/// Reusable test transaction double.
/// </summary>
public sealed class TestAdoTransaction : DbTransaction
{
    private readonly DbConnection _connection;
    private readonly IsolationLevel _isolationLevel;

    public TestAdoTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public override IsolationLevel IsolationLevel => _isolationLevel;
    protected override DbConnection DbConnection => _connection;

    public override void Commit() { }
    public override void Rollback() { }
}

/// <summary>
/// Reusable test exception simulating database exceptions with transient, SQLSTATE, and error code metadata.
/// </summary>
public sealed class TestDbException : DbException
{
    private readonly bool _isTransient;
    private readonly string? _sqlState;

    public TestDbException(string message, int errorCode = 0, bool isTransient = false, string? sqlState = null, Exception? innerException = null)
        : base(message, innerException)
    {
        HResult = errorCode;
        _isTransient = isTransient;
        _sqlState = sqlState;
    }

    public override bool IsTransient => _isTransient;
    public override string? SqlState => _sqlState;
    public override int ErrorCode => HResult;
}

/// <summary>
/// Reusable test TimeProvider for virtualizing time and executing Polly retries/delays instantly.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    private readonly System.Collections.Generic.List<SchedulingTimer> _timers = new();
    private readonly object _lock = new();

    public override DateTimeOffset GetUtcNow() => _utcNow;
    public override long GetTimestamp() => _utcNow.Ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public void Advance(TimeSpan duration)
    {
        lock (_lock)
        {
            _utcNow += duration;
            foreach (var timer in _timers.ToArray())
            {
                timer.OnTimeAdvanced(_utcNow);
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_lock)
        {
            var timer = new SchedulingTimer(this, callback, state, dueTime, period, _utcNow);
            _timers.Add(timer);
            if (dueTime > TimeSpan.Zero && dueTime <= TimeSpan.FromSeconds(15))
            {
                timer.Trigger();
            }
            return timer;
        }
    }

    internal void RemoveTimer(SchedulingTimer timer)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
        }
    }

    internal sealed class SchedulingTimer : ITimer
    {
        private readonly FakeTimeProvider _parent;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private DateTimeOffset _dueTimeUtc;
        private TimeSpan _period;
        private bool _triggered;

        public SchedulingTimer(FakeTimeProvider parent, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period, DateTimeOffset currentUtc)
        {
            _parent = parent;
            _callback = callback;
            _state = state;
            _dueTimeUtc = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : currentUtc + dueTime;
            _period = period;
        }

        public void Trigger()
        {
            if (!_triggered)
            {
                _triggered = true;
                ThreadPool.QueueUserWorkItem(_ => _callback(_state));
            }
        }

        public void OnTimeAdvanced(DateTimeOffset newUtc)
        {
            if (!_triggered && newUtc >= _dueTimeUtc)
            {
                Trigger();
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            _dueTimeUtc = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : _parent.GetUtcNow() + dueTime;
            _triggered = false;
            if (dueTime > TimeSpan.Zero && dueTime <= TimeSpan.FromSeconds(15))
            {
                Trigger();
            }
            return true;
        }

        public void Dispose()
        {
            _parent.RemoveTimer(this);
        }

        public ValueTask DisposeAsync()
        {
            _parent.RemoveTimer(this);
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Reusable ADO.NET parameter test double.
/// </summary>
public sealed class TestAdoParameter : DbParameter
{
    public override DbType DbType { get; set; } = DbType.String;
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; } = true;
    public override string ParameterName { get; set; } = string.Empty;
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

    public override void ResetDbType()
    {
        DbType = DbType.String;
    }
}

/// <summary>
/// Reusable ADO.NET parameter collection test double.
/// </summary>
public sealed class TestAdoParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = new();

    public override int Count => _parameters.Count;
    public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var item in values)
        {
            if (item is DbParameter p)
                _parameters.Add(p);
        }
    }

    public override void Clear() => _parameters.Clear();

    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);

    public override bool Contains(string value) => _parameters.Exists(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);

    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _parameters.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _parameters.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _parameters.RemoveAt(idx);
    }

    protected override DbParameter GetParameter(int index) => _parameters[index];

    protected override DbParameter GetParameter(string parameterName)
    {
        var idx = IndexOf(parameterName);
        if (idx < 0) throw new ArgumentOutOfRangeException(nameof(parameterName), $"Parameter '{parameterName}' not found.");
        return _parameters[idx];
    }

    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _parameters[idx] = value;
        else _parameters.Add(value);
    }
}

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

    public string? LastCommandText { get; private set; }
    public DbParameterCollection? LastParameters { get; private set; }
    public int OpenCount { get; private set; }
    public int OpenAsyncCount { get; private set; }
    public int CloseCount { get; private set; }
    public int DisposeAsyncCount { get; private set; }
    public bool WasOpenAsyncCalled => OpenAsyncCount > 0;
    public bool WasDisposeAsyncCalled => DisposeAsyncCount > 0;

    public Func<string, DbParameterCollection, DbDataReader>? ReaderFactory { get; set; }
    public Func<string, DbParameterCollection, object?>? ScalarFactory { get; set; }
    public Func<string, DbParameterCollection, int>? NonQueryFactory { get; set; }

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
        CloseCount++;
        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        OpenCount++;
        _state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        OpenAsyncCount++;
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
        DisposeAsyncCount++;
        Close();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => new TestAdoTransaction(this, isolationLevel);

    protected override DbCommand CreateDbCommand()
        => new TestAdoCommand(this);

    internal DbDataReader ExecuteReader(string commandText, DbParameterCollection parameters)
    {
        LastCommandText = commandText;
        LastParameters = parameters;
        return ReaderFactory != null
            ? ReaderFactory(commandText, parameters)
            : new TestAdoDataReader([]);
    }

    internal object? ExecuteScalar(string commandText, DbParameterCollection parameters)
    {
        if (State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open to execute command.");
        LastCommandText = commandText;
        LastParameters = parameters;
        return ScalarFactory != null ? ScalarFactory(commandText, parameters) : 1;
    }

    internal int ExecuteNonQuery(string commandText, DbParameterCollection parameters)
    {
        if (State != ConnectionState.Open)
            throw new InvalidOperationException("ExecuteNonQuery requires an open connection");
        LastCommandText = commandText;
        LastParameters = parameters;
        return NonQueryFactory != null ? NonQueryFactory(commandText, parameters) : 42;
    }
}

/// <summary>
/// Reusable ADO.NET command test double.
/// </summary>
public sealed class TestAdoCommand : DbCommand
{
    private readonly TestAdoConnection _connection;
    private readonly TestAdoParameterCollection _parameters = new();

    public TestAdoCommand(TestAdoConnection connection)
    {
        _connection = connection;
    }

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get => _connection; set { } }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery() => _connection.ExecuteNonQuery(CommandText, _parameters);

    public override object? ExecuteScalar() => _connection.ExecuteScalar(CommandText, _parameters);

    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new TestAdoParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => _connection.ExecuteReader(CommandText, _parameters);

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        => Task.FromResult(ExecuteNonQuery());

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        => Task.FromResult(ExecuteScalar());

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        => Task.FromResult(ExecuteDbDataReader(behavior));
}

/// <summary>
/// In-memory generic DbDataReader test double.
/// </summary>
public sealed class TestAdoDataReader : DbDataReader
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;
    private int _currentIndex = -1;
    private bool _isClosed;

    public TestAdoDataReader(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        _rows = rows ?? Array.Empty<IReadOnlyDictionary<string, object?>>();
    }

    private IReadOnlyDictionary<string, object?> CurrentRow
    {
        get
        {
            if (_currentIndex < 0 || _currentIndex >= _rows.Count)
                throw new InvalidOperationException("No current row available.");
            return _rows[_currentIndex];
        }
    }

    public override int FieldCount => _rows.Count > 0 ? _rows[0].Count : 0;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => _isClosed;
    public override int RecordsAffected => -1;
    public override int Depth => 0;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => CurrentRow[name] ?? DBNull.Value;

    public override bool Read()
    {
        if (_isClosed) return false;
        _currentIndex++;
        return _currentIndex < _rows.Count;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read());
    }

    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public override void Close() => _isClosed = true;

    public override string GetName(int ordinal)
    {
        var keys = new List<string>(CurrentRow.Keys);
        return keys[ordinal];
    }

    public override int GetOrdinal(string name)
    {
        var keys = new List<string>(CurrentRow.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (string.Equals(keys[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override object GetValue(int ordinal)
    {
        var name = GetName(ordinal);
        return CurrentRow[name] ?? DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;

    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal)
    {
        var val = GetValue(ordinal);
        return val is Guid g ? g : Guid.Parse(val.ToString()!);
    }
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => GetValue(ordinal)?.ToString() ?? string.Empty;
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Test double provides dynamic runtime type")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal) => GetValue(ordinal).GetType();
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);
}
