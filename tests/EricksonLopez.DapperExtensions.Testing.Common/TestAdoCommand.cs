// Copyright © Erickson Lopez. MIT License.
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

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
