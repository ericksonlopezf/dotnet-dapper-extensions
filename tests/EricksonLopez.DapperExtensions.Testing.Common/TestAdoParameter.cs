// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

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

    public override bool Contains(string value) => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName)
    {
        var clean = parameterName.TrimStart('@', ':', '?');
        return _parameters.FindIndex(p => p.ParameterName.TrimStart('@', ':', '?').Equals(clean, StringComparison.OrdinalIgnoreCase));
    }

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
