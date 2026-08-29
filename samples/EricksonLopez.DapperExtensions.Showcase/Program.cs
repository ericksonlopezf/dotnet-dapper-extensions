// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level00_Conceptual;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level01_QuickStart;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level02_Configuration;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level03_RealWorldUseCases;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level04_AdvancedIntegration;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level05_BulkProcessing;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level06_ErrorHandlingAndResilience;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level07_ScalabilityAndPerformance;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level08_Customization;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level09_ObservabilityAndHealth;
using EricksonLopez.DapperExtensions.Showcase.Levels.Level10_EnterpriseArchitecture;

namespace EricksonLopez.DapperExtensions.Showcase;

/// <summary>
/// Main runner for the official EricksonLopez.DapperExtensions Showcase.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("""
================================================================================
   ERICKSONLOPEZ.DAPPEREXTENSIONS — SHOWCASE & OFFICIAL REFERENCE
   "Raw SQL, Managed Infrastructure" — .NET 8 / 9 / 10 & Native AOT
================================================================================
""");
        Console.ResetColor();

        int? targetLevel = null;
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--level" || args[i] == "-l") && i + 1 < args.Length && int.TryParse(args[i + 1], out var lvl))
            {
                targetLevel = lvl;
                break;
            }
        }

        try
        {
            if (targetLevel.HasValue)
            {
                await ExecuteLevelAsync(targetLevel.Value).ConfigureAwait(false);
            }
            else
            {
                // Run all levels sequentially
                for (int lvl = 0; lvl <= 10; lvl++)
                {
                    await ExecuteLevelAsync(lvl).ConfigureAwait(false);
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("""

================================================================================
  ✔ ALL SHOWCASE LEVELS EXECUTED SUCCESSFULLY
================================================================================
""");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[CRITICAL ERROR IN SHOWCASE]: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task ExecuteLevelAsync(int level)
    {
        switch (level)
        {
            case 0:
                await ConceptualOverview.RunAsync().ConfigureAwait(false);
                break;
            case 1:
                await QuickStartDemo.RunAsync().ConfigureAwait(false);
                break;
            case 2:
                await ConfigurationDemo.RunAsync().ConfigureAwait(false);
                break;
            case 3:
                await PaginationAndCrudDemo.RunAsync().ConfigureAwait(false);
                break;
            case 4:
                await UnitOfWorkAndMultiMapDemo.RunAsync().ConfigureAwait(false);
                break;
            case 5:
                await BulkOperationsDemo.RunAsync().ConfigureAwait(false);
                break;
            case 6:
                await ResilienceAndSavepointDemo.RunAsync().ConfigureAwait(false);
                break;
            case 7:
                await NativeAotAndPerformanceDemo.RunAsync().ConfigureAwait(false);
                break;
            case 8:
                await CustomDetectorAndHandlerDemo.RunAsync().ConfigureAwait(false);
                break;
            case 9:
                await OpenTelemetryAndHealthChecksDemo.RunAsync().ConfigureAwait(false);
                break;
            case 10:
                await EnterprisePatternsDemo.RunAsync().ConfigureAwait(false);
                break;
            default:
                Console.WriteLine($"Unknown level: {level}. Valid levels: 0 to 10.");
                break;
        }
    }
}
