// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.Pagination;

public sealed class CursorPaginationTests
{
    private sealed record SampleEntity(long Id, string Name);

    [Fact]
    public void CursorPaginationParameters_CalculatesPageSizeCorrectly()
    {
        var parameters = new CursorPaginationParameters { First = 25 };
        parameters.GetPageSize(10).Should().Be(25);

        var defaultParams = CursorPaginationParameters.Default;
        defaultParams.GetPageSize(10).Should().Be(10);
    }

    [Fact]
    public void CursorPaginationParameters_CalculatesPageSizeAndCursorsCorrectly()
    {
        var parameters = new CursorPaginationParameters
        {
            First = 25,
            After = "cursor-10",
            Last = 15,
            Before = "cursor-5"
        };

        parameters.First.Should().Be(25);
        parameters.After.Should().Be("cursor-10");
        parameters.Last.Should().Be(15);
        parameters.Before.Should().Be("cursor-5");
        parameters.GetPageSize(10).Should().Be(25);

        var lastOnlyParams = new CursorPaginationParameters { Last = 30 };
        lastOnlyParams.GetPageSize(10).Should().Be(30);

        var defaultParams = CursorPaginationParameters.Default;
        defaultParams.GetPageSize(10).Should().Be(10);
        defaultParams.First.Should().Be(10);
        defaultParams.After.Should().BeNull();
    }
}
