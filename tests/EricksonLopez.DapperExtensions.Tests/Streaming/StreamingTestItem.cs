// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.DapperExtensions.Tests.Streaming;

/// <summary>
/// Sample entity for verifying Dapper unbuffered streaming extensions.
/// </summary>
public sealed class StreamingTestItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
