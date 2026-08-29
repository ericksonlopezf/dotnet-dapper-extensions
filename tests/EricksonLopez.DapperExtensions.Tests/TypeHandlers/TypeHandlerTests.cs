// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.DapperExtensions.TypeHandlers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.DapperExtensions.Tests.TypeHandlers;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum PaymentMode
{
    CreditCard,
    DebitCard,
    WireTransfer
}

public sealed class TypeHandlerTests
{
    private sealed class OrderEntity
    {
        public int Id { get; set; }
        public DateOnly OrderDate { get; set; }
        public TimeOnly OrderTime { get; set; }
        public OrderStatus Status { get; set; }
    }

    private sealed class DateEntity
    {
        public int Id { get; set; }
        public DateOnly TheDate { get; set; }
        public TimeOnly TheTime { get; set; }
    }

    private sealed class PaymentEntity
    {
        public int Id { get; set; }
        public PaymentMode Mode { get; set; }
    }

    [Fact]
    public void DateOnlyTypeHandler_Properties_And_SetValue()
    {
        DateOnlyTypeHandler.Default.Should().NotBeNull();
        var handler = DateOnlyTypeHandler.Default;

        var actNullParam = () => handler.SetValue(null!, new DateOnly(2026, 8, 19));
        actNullParam.Should().ThrowExactly<ArgumentNullException>().WithParameterName("parameter");

        using var command = new SqliteCommand();
        var parameter = command.CreateParameter();

        var date = new DateOnly(2026, 8, 19);
        handler.SetValue(parameter, date);

        parameter.DbType.Should().Be(DbType.Date);
        parameter.Value.Should().Be(new DateTime(2026, 8, 19, 0, 0, 0));
    }

    [Fact]
    public void DateOnlyTypeHandler_Parse_HandlesAllSupportedBranches()
    {
        var handler = DateOnlyTypeHandler.Default;

        // DateTime branch
        var fromDateTime = handler.Parse(new DateTime(2026, 8, 19, 14, 30, 0));
        fromDateTime.Should().Be(new DateOnly(2026, 8, 19));

        // String TryParse branch
        var fromString = handler.Parse("2026-08-19");
        fromString.Should().Be(new DateOnly(2026, 8, 19));

        // DateTimeOffset branch
        var dto = new DateTimeOffset(new DateTime(2026, 8, 19, 10, 0, 0), TimeSpan.FromHours(-4));
        var fromDto = handler.Parse(dto);
        fromDto.Should().Be(new DateOnly(2026, 8, 19));

        // Fallback branch via Convert.ToDateTime (e.g., standard format string or convertible)
        var fromFallback = handler.Parse("08/19/2026");
        fromFallback.Should().Be(new DateOnly(2026, 8, 19));

        var actInvalid = () => handler.Parse(new object());
        actInvalid.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void TimeOnlyTypeHandler_Properties_And_SetValue()
    {
        TimeOnlyTypeHandler.Default.Should().NotBeNull();
        var handler = TimeOnlyTypeHandler.Default;

        var actNullParam = () => handler.SetValue(null!, new TimeOnly(15, 30, 45));
        actNullParam.Should().ThrowExactly<ArgumentNullException>().WithParameterName("parameter");

        using var command = new SqliteCommand();
        var parameter = command.CreateParameter();

        var time = new TimeOnly(15, 30, 45);
        handler.SetValue(parameter, time);

        parameter.DbType.Should().Be(DbType.Time);
        parameter.Value.Should().Be(new TimeSpan(15, 30, 45));
    }

    [Fact]
    public void TimeOnlyTypeHandler_Parse_HandlesAllSupportedBranches()
    {
        var handler = TimeOnlyTypeHandler.Default;

        // TimeSpan branch
        var fromTimeSpan = handler.Parse(new TimeSpan(15, 30, 45));
        fromTimeSpan.Should().Be(new TimeOnly(15, 30, 45));

        // DateTime branch
        var fromDateTime = handler.Parse(new DateTime(2026, 8, 19, 15, 30, 45));
        fromDateTime.Should().Be(new TimeOnly(15, 30, 45));

        // String TryParse branch
        var fromString = handler.Parse("15:30:45");
        fromString.Should().Be(new TimeOnly(15, 30, 45));

        // Fallback cast branch
        var actInvalidString = () => handler.Parse("not-a-time");
        actInvalidString.Should().Throw<InvalidCastException>();

        var actInvalidObject = () => handler.Parse(12345);
        actInvalidObject.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void StringEnumTypeHandler_Properties_And_SetValue()
    {
        StringEnumTypeHandler<OrderStatus>.Default.Should().NotBeNull();
        var handler = StringEnumTypeHandler<OrderStatus>.Default;

        var actNullParam = () => handler.SetValue(null!, OrderStatus.Processing);
        actNullParam.Should().ThrowExactly<ArgumentNullException>().WithParameterName("parameter");

        using var command = new SqliteCommand();
        var parameter = command.CreateParameter();

        handler.SetValue(parameter, OrderStatus.Processing);
        parameter.DbType.Should().Be(DbType.String);
        parameter.Value.Should().Be("Processing");

        handler.SetValue(parameter, OrderStatus.Cancelled);
        parameter.Value.Should().Be("Cancelled");
    }

    [Fact]
    public void StringEnumTypeHandler_Parse_HandlesAllSupportedBranches()
    {
        var handler = StringEnumTypeHandler<OrderStatus>.Default;

        // null and DBNull branch
        handler.Parse(null!).Should().Be(OrderStatus.Pending);
        handler.Parse(DBNull.Value).Should().Be(OrderStatus.Pending);

        // Direct enum branch
        handler.Parse(OrderStatus.Completed).Should().Be(OrderStatus.Completed);

        // Null or whitespace strings branch
        handler.Parse(string.Empty).Should().Be(OrderStatus.Pending);
        handler.Parse("   ").Should().Be(OrderStatus.Pending);

        // String TryParse branch (case-insensitive)
        handler.Parse("Completed").Should().Be(OrderStatus.Completed);
        handler.Parse("completed").Should().Be(OrderStatus.Completed);
        handler.Parse("PROCESSING").Should().Be(OrderStatus.Processing);
        handler.Parse("Cancelled").Should().Be(OrderStatus.Cancelled);

        // Enum.ToObject fallback branch (numeric integers, bytes, invalid enum name string fallback)
        handler.Parse(0).Should().Be(OrderStatus.Pending);
        handler.Parse((byte)1).Should().Be(OrderStatus.Processing);
        handler.Parse(2).Should().Be(OrderStatus.Completed);
        handler.Parse((long)3).Should().Be(OrderStatus.Cancelled);

        var actInvalidName = () => handler.Parse("NonExistentStatus");
        actInvalidName.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task DapperTypeHandlerRegistrar_RegisterStandardHandlers_RegistersDateOnlyAndTimeOnlyInSqlMapper()
    {
        DapperTypeHandlerRegistrar.RegisterStandardHandlers();

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE DateTable (
                Id INTEGER PRIMARY KEY,
                TheDate TEXT NOT NULL,
                TheTime TEXT NOT NULL
            );");

        var dateEntity = new DateEntity
        {
            Id = 1,
            TheDate = new DateOnly(2026, 8, 19),
            TheTime = new TimeOnly(11, 22, 33)
        };

        await connection.ExecuteAsync(
            "INSERT INTO DateTable (Id, TheDate, TheTime) VALUES (@Id, @TheDate, @TheTime);",
            dateEntity);

        var readBack = await connection.QuerySingleAsync<DateEntity>(
            "SELECT Id, TheDate, TheTime FROM DateTable WHERE Id = 1;");

        readBack.TheDate.Should().Be(new DateOnly(2026, 8, 19));
        readBack.TheTime.Should().Be(new TimeOnly(11, 22, 33));
    }

    [Fact]
    public async Task DapperTypeHandlerRegistrar_RegisterStringEnumHandler_RegistersStringEnumInSqlMapper()
    {
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<PaymentMode>();

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE Payments (Id INTEGER PRIMARY KEY, Mode TEXT NOT NULL);");
        await connection.ExecuteAsync("INSERT INTO Payments (Id, Mode) VALUES (10, 'WireTransfer'), (11, 'creditcard');");

        var retrieved1 = await connection.QuerySingleAsync<PaymentEntity>("SELECT Id, Mode FROM Payments WHERE Id = 10;");
        retrieved1.Id.Should().Be(10);
        retrieved1.Mode.Should().Be(PaymentMode.WireTransfer);

        var retrieved2 = await connection.QuerySingleAsync<PaymentEntity>("SELECT Id, Mode FROM Payments WHERE Id = 11;");
        retrieved2.Id.Should().Be(11);
        retrieved2.Mode.Should().Be(PaymentMode.CreditCard);
    }

    public enum DeliveryStatus
    {
        Standard = 1,
        Express = 2
    }

    private sealed class DeliveryEntity
    {
        public int Id { get; set; }
        public DeliveryStatus Status { get; set; }
    }

    [Fact]
    public async Task DapperTypeHandlerRegistrar_RegisterStringEnumHandler_RegistersAndHandlesCaseInsensitiveMapping()
    {
        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<DeliveryStatus>();

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync("CREATE TABLE Deliveries (Id INTEGER PRIMARY KEY, Status TEXT NOT NULL);");
        await connection.ExecuteAsync("INSERT INTO Deliveries (Id, Status) VALUES (1, 'express');");

        var delivery = await connection.QuerySingleAsync<DeliveryEntity>("SELECT Id, Status FROM Deliveries WHERE Id = 1;");
        delivery.Status.Should().Be(DeliveryStatus.Express);
    }
}
