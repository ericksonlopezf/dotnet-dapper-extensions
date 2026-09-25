// Copyright © Erickson Lopez. MIT License.
using System.Data.Common;

namespace EricksonLopez.DapperExtensions.Testing.Common;

/// <summary>
/// Spy object to track ADO.NET execution metrics and state transitions, 
/// keeping the connection fake as a pure Humble Object.
/// </summary>
public class AdoExecutionSpy
{
    public string? LastCommandText { get; set; }
    public DbParameterCollection? LastParameters { get; set; }
    public int OpenCount { get; set; }
    public int OpenAsyncCount { get; set; }
    public int CloseCount { get; set; }
    public int DisposeAsyncCount { get; set; }
    public int BeginTransactionCount { get; set; }
    public int BeginTransactionAsyncCount { get; set; }
    public bool WasOpenAsyncCalled => OpenAsyncCount > 0;
    public bool WasDisposeAsyncCalled => DisposeAsyncCount > 0;
    public TestAdoTransaction? LastTransaction { get; set; }
}
