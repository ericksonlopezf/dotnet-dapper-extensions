// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Utility helper for formatting CLI output and level banners in Showcase.
/// </summary>
public static class ConsoleHelper
{
    public static void PrintHeader(int level, string title, string description)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  LEVEL {level} — {title.ToUpperInvariant()}");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {description}\n");
        Console.ResetColor();
    }

    public static void PrintStep(string stepName)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ➤ {stepName}");
        Console.ResetColor();
    }

    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    ✔ {message}");
        Console.ResetColor();
    }

    public static void PrintInfo(string label, object? value)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"    • {label}: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(value?.ToString() ?? "null");
        Console.ResetColor();
    }

    public static void PrintWarning(string warning)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"    ⚠ {warning}");
        Console.ResetColor();
    }

    public static void PrintSeparator()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ------------------------------------------------------------------------------");
        Console.ResetColor();
    }
}
