using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WFInfo.Tests;
using WFInfo.Settings;
using WFInfo.Services.WindowInfo;
using WFInfo.Services.Screenshot;
using WFInfo.Services.HDRDetection;

namespace WFInfo.Tests
{
    public class TestProgram
    {
        public static void Main(string[] args)
        {
            RunTests(args).Wait();
        }

        public static async Task RunTests(string[] args)
        {
            try
            {
                Console.WriteLine("WFInfo OCR Test Runner");
                Console.WriteLine("=======================");

                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: WFInfo.exe <testMap.json> <testImagesDirectory> [outputFile.json]");
                    Console.WriteLine("");
                    Console.WriteLine("Example:");
                    Console.WriteLine("  WFInfo.exe map.json tests/ results.json");
                    Console.WriteLine("");
                    Console.WriteLine("Test map format:");
                    Console.WriteLine("{");
                    Console.WriteLine("  \"scenarios\": [");
                    Console.WriteLine("    \"data/test1\",");
                    Console.WriteLine("    \"data/test2\"");
                    Console.WriteLine("  ]");
                    Console.WriteLine("}");
                    return;
                }

                string testMapPath = args[0];
                string testImagesDir = args[1];
                string outputPath = args.Length > 2 ? args[2] : $"test_results_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                Console.WriteLine($"Loading test map: {testMapPath}");
                Console.WriteLine($"Test images directory: {testImagesDir}");
                Console.WriteLine($"Output file: {outputPath}");
                Console.WriteLine("");

                // Validate inputs
                if (!File.Exists(testMapPath))
                {
                    Console.WriteLine($"ERROR: Test map file not found: {testMapPath}");
                    return;
                }

                if (!Directory.Exists(testImagesDir))
                {
                    Console.WriteLine($"ERROR: Test images directory not found: {testImagesDir}");
                    return;
                }

                // Initialize services (simplified for testing)
                var dataService = new TestDataService();
                var tesseractService = new TestTesseractService();
                var windowService = new TestWindowInfoService();
                var screenshotService = new TestScreenshotService();
                var hdrDetector = new TestHDRDetectorService();

                // Create test runner
                var testRunner = new OCRTestRunner(dataService, tesseractService, 
                    windowService, screenshotService, hdrDetector);

                // Run test suite
                var results = testRunner.RunTestSuite(testMapPath, testImagesDir);

                // Save results
                testRunner.SaveResults(results, outputPath);

                // Print summary
                PrintSummary(results);

                Console.WriteLine("");
                Console.WriteLine("Test completed successfully!");
                Console.WriteLine($"Results saved to: {outputPath}");

                // Set exit code based on results
                Environment.ExitCode = results.FailedTests > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.ExitCode = 2;
            }
        }

        private static void PrintSummary(TestSuiteResult results)
        {
            Console.WriteLine("");
            Console.WriteLine("TEST RESULTS SUMMARY");
            Console.WriteLine("==================");
            Console.WriteLine($"Test Suite: {results.TestSuiteName}");
            Console.WriteLine($"Total Tests: {results.TotalTests}");
            Console.WriteLine($"Passed: {results.PassedTests}");
            Console.WriteLine($"Failed: {results.FailedTests}");
            Console.WriteLine($"Pass Rate: {results.PassRate:F1}%");
            Console.WriteLine($"Overall Accuracy: {results.OverallAccuracy:F2}%");
            Console.WriteLine($"Duration: {(results.EndTime - results.StartTime).TotalMinutes:F1} minutes");

            Console.WriteLine("");
            Console.WriteLine("Category Coverage:");
            foreach (var category in results.CategoryCoverage)
            {
                Console.WriteLine($"  {category.Key}: {category.Value.PassedTests}/{category.Value.TotalTests} ({category.Value.PassRate:F1}% pass rate, {category.Value.AverageAccuracy:F2}% avg accuracy)");
            }

            Console.WriteLine("");
            Console.WriteLine("Language Coverage:");
            foreach (var lang in results.LanguageCoverage)
            {
                Console.WriteLine($"  {lang.Key}: {lang.Value.PassedTests}/{lang.Value.TotalTests} ({lang.Value.PassRate:F1}% pass rate, {lang.Value.AverageAccuracy:F2}% avg accuracy, {lang.Value.AverageProcessingTime:F0}ms avg time)");
            }

            Console.WriteLine("");
            Console.WriteLine("Language Accuracy:");
            foreach (var lang in results.LanguageAccuracy)
            {
                Console.WriteLine($"  {lang.Key}: {lang.Value:F2}%");
            }

            Console.WriteLine("");
            Console.WriteLine("Failed Tests:");
            var failedTests = new System.Collections.Generic.List<TestResult>();
            foreach (var test in results.TestResults)
            {
                if (!test.Success)
                    failedTests.Add(test);
            }
            
            foreach (var failed in failedTests)
            {
                Console.WriteLine($"  {failed.TestCaseName}: {failed.ErrorMessage}");
                if (failed.MissingParts.Count > 0)
                    Console.WriteLine($"    Missing: {string.Join(", ", failed.MissingParts)}");
                if (failed.ExtraParts.Count > 0)
                    Console.WriteLine($"    Extra: {string.Join(", ", failed.ExtraParts)}");
            }
        }
    }

    // Mock services for testing (these would be replaced with real implementations)
    public class TestDataService : IDataService
    {
        public string GetPartName(string name, out int low, bool suppressLogging, out bool multipleLowest)
        {
            // Mock implementation - in real usage this would use the actual Data class
            low = name == "Volt Prime Blueprint" ? 0 : 5;
            multipleLowest = false;
            return name == "Volt Prime Blueprint" ? "Volt Prime Blueprint" : "Unknown Part";
        }
    }

    public class TestTesseractService : ITesseractService
    {
        public Tesseract.TesseractEngine FirstEngine => throw new NotImplementedException("Mock service");
        public Tesseract.TesseractEngine SecondEngine => throw new NotImplementedException("Mock service");
        public Tesseract.TesseractEngine[] Engines => throw new NotImplementedException("Mock service");

        public void Init() { }
        public void ReloadEngines() { }
    }

    public class TestWindowInfoService : IWindowInfoService
    {
        public System.Drawing.Rectangle Window => new System.Drawing.Rectangle(0, 0, 1920, 1080);
        public System.Drawing.Point Center => new System.Drawing.Point(960, 540);
        public double ScreenScaling => 1.0;
        public double DpiScaling => 1.0;
        public System.Windows.Forms.Screen Screen => throw new NotImplementedException("Mock service");
        public void UpdateWindow() { }
        public void UseImage(System.Drawing.Bitmap image) { }
    }

    public class TestScreenshotService : IScreenshotService
    {
        public System.Threading.Tasks.Task<System.Collections.Generic.List<System.Drawing.Bitmap>> CaptureScreenshot() => 
            System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<System.Drawing.Bitmap>());
        
        public bool IsAvailable => true;
    }

    public class TestHDRDetectorService : IHDRDetectorService
    {
        public bool IsHDR => false;
    }
}
