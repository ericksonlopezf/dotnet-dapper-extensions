// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.DapperExtensions;
using EricksonLopez.DapperExtensions.TypeHandlers;

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

    public static void Main()
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
