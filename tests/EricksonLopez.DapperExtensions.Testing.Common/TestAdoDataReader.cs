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
    [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Test double provides dynamic runtime type")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal) => GetValue(ordinal).GetType();
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);
}
