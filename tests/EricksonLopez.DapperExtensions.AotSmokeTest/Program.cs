// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions;
using EricksonLopez.DapperExtensions.Sqlite.Transactions;
using EricksonLopez.DapperExtensions.TypeHandlers;
using Microsoft.Data.Sqlite;

#pragma warning disable CA1050
#pragma warning disable CA1303

namespace EricksonLopez.DapperExtensions.AotSmokeTest;

internal static class Program
{
    private static int _passedTests;

    private static void Assert([DoesNotReturnIf(false)] bool condition, string testName)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName}");
            Console.ResetColor();
            Environment.Exit(1);
        }
        _passedTests++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[PASS] {testName}");
        Console.ResetColor();
    }

    public static async Task Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" EricksonLopez.DapperExtensions NativeAOT Suite ");
        Console.WriteLine("=================================================");

        // ── 1. SqlEntityAttribute Invariants ──────────────────────────────────────
        Console.WriteLine("\n--- 1. SqlEntityAttribute ---");

        var attr = new SqlEntityAttribute { TableName = "users_table" };
        Assert(attr.TableName == "users_table", "SqlEntityAttribute.TableName matches");

        // ── 2. Type Handlers Registration ─────────────────────────────────────────
        Console.WriteLine("\n--- 2. TypeHandler Registration ---");

        DapperTypeHandlerRegistrar.RegisterStandardHandlers();
        Assert(true, "RegisterStandardHandlers executes without exception");

        DapperTypeHandlerRegistrar.RegisterStringEnumHandler<TestStatus>();
        Assert(true, "RegisterStringEnumHandler executes without exception");
        Assert(SqlMapper.HasTypeHandler(typeof(TestStatus)), "SqlMapper.HasTypeHandler confirms TestStatus is registered");

        // ── 3. DateOnlyTypeHandler Hydration ──────────────────────────────────────
        Console.WriteLine("\n--- 3. DateOnlyTypeHandler Hydration ---");

        var parsedDateFromString = DateOnlyTypeHandler.Default.Parse("2026-09-03");
        Assert(parsedDateFromString == new DateOnly(2026, 9, 3), "DateOnlyTypeHandler parses ISO string correctly");

        var parsedDateFromDateTime = DateOnlyTypeHandler.Default.Parse(new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc));
        Assert(parsedDateFromDateTime == new DateOnly(2026, 9, 3), "DateOnlyTypeHandler parses DateTime correctly");

        // ── 4. TimeOnlyTypeHandler Hydration ──────────────────────────────────────
        Console.WriteLine("\n--- 4. TimeOnlyTypeHandler Hydration ---");

        var parsedTimeFromString = TimeOnlyTypeHandler.Default.Parse("14:30:15");
        Assert(parsedTimeFromString == new TimeOnly(14, 30, 15), "TimeOnlyTypeHandler parses string correctly");

        var parsedTimeFromTimeSpan = TimeOnlyTypeHandler.Default.Parse(new TimeSpan(14, 30, 15));
        Assert(parsedTimeFromTimeSpan == new TimeOnly(14, 30, 15), "TimeOnlyTypeHandler parses TimeSpan correctly");

        // ── 5. StringEnumTypeHandler Hydration ────────────────────────────────────
        Console.WriteLine("\n--- 5. StringEnumTypeHandler Hydration ---");

        var parsedEnumExact = StringEnumTypeHandler<TestStatus>.Default.Parse("Active");
        Assert(parsedEnumExact == TestStatus.Active, "StringEnumTypeHandler parses exact enum name correctly");

        var parsedEnumCaseInsensitive = StringEnumTypeHandler<TestStatus>.Default.Parse("inactive");
        Assert(parsedEnumCaseInsensitive == TestStatus.Inactive, "StringEnumTypeHandler parses case-insensitive enum correctly");

        bool threwOnInvalid = false;
        try
        {
            StringEnumTypeHandler<TestStatus>.Default.Parse("UnknownValue");
        }
        catch (ArgumentException)
        {
            threwOnInvalid = true;
        }
        Assert(threwOnInvalid, "StringEnumTypeHandler throws ArgumentException on invalid enum value");

        // ── 6. Sqlite In-Memory Transaction Under AOT ────────────────────────────
        Console.WriteLine("\n--- 6. Sqlite Transactions Under AOT ---");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE aot_items (id INT PRIMARY KEY, name TEXT NOT NULL);";
            cmd.ExecuteNonQuery();
        }

        await connection.ExecuteInTransactionAsync(async trx =>
        {
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = (SqliteTransaction)trx;
            insertCmd.CommandText = "INSERT INTO aot_items (id, name) VALUES (1, 'AOT Item');";
            await insertCmd.ExecuteNonQueryAsync();
        });

        using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM aot_items;";
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            Assert(count == 1, "ExecuteInTransactionAsync committed data correctly under AOT");
        }

        Console.WriteLine("\n=================================================");
        Console.WriteLine($" ALL {_passedTests} NATIVE AOT SUITE TESTS PASSED SUCCESSFULLY! ");
        Console.WriteLine("=== AOT Validator: OK ===");
        Console.WriteLine("=================================================");
    }
}

public enum TestStatus
{
    Active = 1,
    Inactive = 2
}
