using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WFInfo.Settings;
using WFInfo.Services.WarframeProcess;
using WFInfo.Services.WindowInfo;

namespace WFInfo.Tests
{
    /// <summary>
    /// Headless entry point for OCR regression tests.
    /// Initializes real WFInfo services (Tesseract, Data, WindowInfo) without WPF UI.
    /// </summary>
    public static class TestProgram
    {
        public static async Task RunTests(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            string testMapPath = args[0];
            string outputPath = args.Length > 1 ? args[1] : $"test_results_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            Console.WriteLine($"Map:    {Path.GetFullPath(testMapPath)}");
            Console.WriteLine($"Output: {Path.GetFullPath(outputPath)}");
            Console.WriteLine();

            if (!File.Exists(testMapPath))
            {
                Console.Error.WriteLine($"ERROR: map file not found: {testMapPath}");
                Environment.ExitCode = 2;
                return;
            }

            try
            {
                // --- Initialize real services headlessly ---
                Console.WriteLine("Initializing services...");

                var settings = ApplicationSettings.GlobalSettings;
                settings.Debug = true; // Enable debug mode so window info works without a game process

                var processFinder = new HeadlessProcessFinder();
                var windowService = new Win32WindowInfoService(processFinder, ApplicationSettings.GlobalReadonlySettings);

                // Initialize Data (downloads/loads market data, name data, etc.)
                Main.dataBase = new Data(ApplicationSettings.GlobalReadonlySettings, processFinder, windowService);
                Console.WriteLine("Updating databases (this may take a moment on first run)...");
                await Main.dataBase.Update();
                Console.WriteLine("Databases ready.");

                // Initialize OCR with real TesseractService
                OCR.InitForTest(
                    new TesseractService(),
                    ApplicationSettings.GlobalReadonlySettings,
                    windowService,
                    new HeadlessHDRDetector(false));
                Console.WriteLine("OCR engine ready.");
                Console.WriteLine();

                // --- Run tests ---
                var runner = new OCRTestRunner(windowService);
                var results = runner.RunTestSuite(testMapPath);

                // --- Save & report ---
                OCRTestRunner.SaveResults(results, outputPath);
                PrintSummary(results);

                Console.WriteLine();
                Console.WriteLine($"Results saved to: {Path.GetFullPath(outputPath)}");

                // Exit code: 0 = all pass, 1 = some fail, 2 = error
                if (!string.IsNullOrEmpty(results.ErrorMessage))
                    Environment.ExitCode = 2;
                else if (results.FailedTests > 0 || results.ErrorTests > 0)
                    Environment.ExitCode = 1;
                else
                    Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FATAL: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                Environment.ExitCode = 2;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: WFInfo.exe <map.json> [output.json]");
            Console.WriteLine();
            Console.WriteLine("  map.json    - Test map file listing scenario paths");
            Console.WriteLine("  output.json - (optional) Output results file");
            Console.WriteLine();
            Console.WriteLine("Each scenario is a pair of files relative to map.json:");
            Console.WriteLine("  data/test1.json  - Test spec (language, theme, expected parts, ...)");
            Console.WriteLine("  data/test1.png   - Screenshot to OCR");
            Console.WriteLine();
            Console.WriteLine("Example map.json:");
            Console.WriteLine("  { \"scenarios\": [\"data/test1\", \"data/test2\"] }");
        }

        private static void PrintSummary(TestSuiteResult results)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("  TEST RESULTS SUMMARY");
            Console.WriteLine("========================================");
            Console.WriteLine($"  Suite:    {results.TestSuiteName}");
            Console.WriteLine($"  Total:    {results.TotalTests}");
            Console.WriteLine($"  Passed:   {results.PassedTests}");
            Console.WriteLine($"  Failed:   {results.FailedTests}");
            if (results.ErrorTests > 0)
                Console.WriteLine($"  Errors:   {results.ErrorTests}");
            Console.WriteLine($"  Pass Rate: {results.PassRate:F1}%");
            Console.WriteLine($"  Accuracy:  {results.OverallAccuracy:F1}%");
            Console.WriteLine($"  Duration:  {(results.EndTime - results.StartTime).TotalSeconds:F1}s");

            if (results.LanguageCoverage.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("  By Language:");
                foreach (var kv in results.LanguageCoverage)
                {
                    var c = kv.Value;
                    Console.WriteLine($"    {kv.Key,-20} {c.PassedTests}/{c.TotalTests} pass  {c.AverageAccuracy:F0}% acc  {c.AverageProcessingTime:F0}ms avg");
                }
            }

            if (results.CategoryCoverage.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("  By Category:");
                foreach (var kv in results.CategoryCoverage)
                {
                    var c = kv.Value;
                    Console.WriteLine($"    {kv.Key,-20} {c.PassedTests}/{c.TotalTests} pass  {c.AverageAccuracy:F0}% acc  {c.AverageProcessingTime:F0}ms avg");
                }
            }

            // Print failed/error test details
            var problems = results.TestResults.FindAll(t => !t.Success);
            if (problems.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("  Failed/Error Details:");
                foreach (var t in problems)
                {
                    if (!string.IsNullOrEmpty(t.ErrorMessage))
                    {
                        Console.WriteLine($"    ERROR {t.TestCaseName}: {t.ErrorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"    FAIL  {t.TestCaseName} ({t.AccuracyScore:F0}% accuracy)");
                        if (t.MissingParts.Count > 0)
                            Console.WriteLine($"          Missing: {string.Join(", ", t.MissingParts)}");
                        if (t.ExtraParts.Count > 0)
                            Console.WriteLine($"          Extra:   {string.Join(", ", t.ExtraParts)}");
                        if (t.ActualParts.Count > 0)
                            Console.WriteLine($"          Got:     {string.Join(", ", t.ActualParts)}");
                    }
                }
            }

            Console.WriteLine("========================================");
        }
    }

    /// <summary>
    /// Headless process finder that reports no running game process.
    /// </summary>
    internal class HeadlessProcessFinder : IProcessFinder
    {
        public Process Warframe => null;
        public HandleRef HandleRef => default;
        public bool IsRunning => false;
        public bool GameIsStreamed => false;
        public event ProcessChangedArgs OnProcessChanged { add { } remove { } }
    }
}
