using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using WFInfo.Settings;
using WFInfo.Services.HDRDetection;
using WFInfo.Services.WindowInfo;

namespace WFInfo.Tests
{
    /// <summary>
    /// OCR regression test runner that calls real WFInfo OCR methods directly.
    /// Requires OCR.InitForTest and Main.dataBase to be initialized before use.
    /// </summary>
    public class OCRTestRunner
    {
        private readonly IWindowInfoService _windowService;
        private string _currentLocale;
        private bool _currentHDR;

        public OCRTestRunner(IWindowInfoService windowService)
        {
            _windowService = windowService;
        }

        public TestSuiteResult RunTestSuite(string testMapPath)
        {
            var result = new TestSuiteResult
            {
                TestSuiteName = Path.GetFileNameWithoutExtension(testMapPath),
                StartTime = DateTime.UtcNow
            };

            try
            {
                var testMapJson = File.ReadAllText(testMapPath);
                var testMap = JsonConvert.DeserializeObject<TestMap>(testMapJson);
                string testMapDir = Path.GetDirectoryName(Path.GetFullPath(testMapPath));

                Main.AddLog($"Starting test suite: {result.TestSuiteName} with {testMap.Scenarios.Count} scenario(s)");

                foreach (var scenario in testMap.Scenarios)
                {
                    var testResult = RunSingleTest(scenario, testMapDir);
                    result.TestResults.Add(testResult);
                }

                CalculateStatistics(result);
                result.EndTime = DateTime.UtcNow;

                Main.AddLog($"Test suite completed: {result.PassedTests}/{result.TotalTests} passed ({result.PassRate:F1}%), accuracy {result.OverallAccuracy:F1}%");
            }
            catch (Exception ex)
            {
                Main.AddLog($"Test suite failed: {ex.Message}\n{ex.StackTrace}");
                result.EndTime = DateTime.UtcNow;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private TestResult RunSingleTest(string scenarioPath, string testMapDir)
        {
            var stopwatch = Stopwatch.StartNew();

            // Resolve paths relative to the map.json directory with traversal protection
            string baseDir = Path.GetFullPath(testMapDir);
            string jsonFull = Path.GetFullPath(Path.Combine(baseDir, scenarioPath + ".json"));
            string imageFull = Path.GetFullPath(Path.Combine(baseDir, scenarioPath + ".png"));
            
            // Verify paths don't escape the base directory (case-insensitive on Windows)
            if (!jsonFull.Equals(baseDir, StringComparison.OrdinalIgnoreCase) && 
                !jsonFull.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Path traversal detected for JSON file: {scenarioPath}");
            }
            
            if (!imageFull.Equals(baseDir, StringComparison.OrdinalIgnoreCase) && 
                !imageFull.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Path traversal detected for image file: {scenarioPath}");
            }
            
            string jsonPath = jsonFull;
            string imagePath = imageFull;
            string testName = Path.GetFileName(scenarioPath);

            var result = new TestResult
            {
                TestCaseName = testName,
                ImagePath = imagePath
            };

            try
            {
                // Validate files exist
                if (!File.Exists(jsonPath))
                {
                    result.ErrorMessage = $"JSON not found: {jsonPath}";
                    result.Success = false;
                    stopwatch.Stop();
                    result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                if (!File.Exists(imagePath))
                {
                    result.ErrorMessage = $"PNG not found: {imagePath}";
                    result.Success = false;
                    stopwatch.Stop();
                    result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                // Load spec
                var testCase = JsonConvert.DeserializeObject<TestCase>(File.ReadAllText(jsonPath));
                result.Language = testCase.Language ?? "unknown";
                result.Theme = testCase.Theme ?? "auto";
                result.Category = testCase.Category ?? "reward";
                result.ExpectedParts = testCase.Parts?.Values.ToList() ?? new List<string>();

                Main.AddLog($"Running: {testName} [{result.Language}/{result.Category}/{result.Theme}] expecting {result.ExpectedParts.Count} part(s)");

                // Configure settings for this test
                ApplyTestSettings(testCase);

                // Run real OCR pipeline
                using (var bitmap = new Bitmap(imagePath))
                {
                    List<string> ocrResults;
                    switch (result.Category.ToLower())
                    {
                        case "snapit":
                            ocrResults = OCR.ProcessSnapItForTest(bitmap, _windowService);
                            break;
                        case "reward":
                        default:
                            ocrResults = OCR.ProcessRewardScreenForTest(bitmap, _windowService);
                            break;
                    }

                    result.ActualParts = ocrResults;
                }

                // Compare expected vs actual
                CompareResults(result);

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                string status = result.Success ? "PASS" : "FAIL";
                Main.AddLog($"  {status}: {testName} ({result.AccuracyScore:F0}% accuracy, {result.ProcessingTimeMs}ms) actual=[{string.Join(", ", result.ActualParts)}]");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.ErrorMessage = ex.Message;
                result.Success = false;
                Main.AddLog($"  ERROR: {testName}: {ex.Message}");
            }

            return result;
        }

        private void ApplyTestSettings(TestCase testCase)
        {
            var settings = ApplicationSettings.GlobalSettings;

            // Map language name to locale code
            string newLocale = MapLanguageToLocale(testCase.Language);
            bool localeChanged = newLocale != _currentLocale;
            bool hdrChanged = testCase.HDR != _currentHDR;
            settings.Locale = newLocale;
            _currentLocale = newLocale;
            _currentHDR = testCase.HDR;

            // Map theme name to enum
            settings.ThemeSelection = MapThemeToEnum(testCase.Theme);

            // Apply scaling
            if (testCase.Scaling > 0)
                OCR.uiScaling = testCase.Scaling / 100.0;

            // Reload engines if language changed (different tessdata) or HDR setting changed
            if (localeChanged || hdrChanged)
            {
                string reason = localeChanged ? $"Locale changed to '{newLocale}'" : $"HDR changed to '{testCase.HDR}'";
                Main.AddLog($"  {reason}, reinitializing OCR engines...");
                OCR.InitForTest(
                    new TesseractService(),
                    ApplicationSettings.GlobalReadonlySettings,
                    _windowService,
                    new HeadlessHDRDetector(testCase.HDR));

                // Also re-update Data so Levenshtein uses the right locale for matching (only when locale changes)
                if (localeChanged)
                {
                    Main.dataBase.ReloadItems().GetAwaiter().GetResult();
                }
            }
        }

        private static string MapLanguageToLocale(string language)
        {
            if (string.IsNullOrEmpty(language)) return "en";
            switch (language.ToLower())
            {
                case "english": return "en";
                case "korean": return "ko";
                case "japanese": return "ja";
                case "simplified chinese": return "zh-hans";
                case "traditional chinese": return "zh-hant";
                case "thai": return "th";
                case "french": return "fr";
                case "ukrainian": return "uk";
                case "italian": return "it";
                case "german": return "de";
                case "spanish": return "es";
                case "portuguese": return "pt";
                case "polish": return "pl";
                case "turkish": return "tr";
                case "russian": return "ru";
                default: return "en";
            }
        }

        private static WFtheme MapThemeToEnum(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return WFtheme.AUTO;
            switch (theme.ToLower())
            {
                case "orokin": return WFtheme.OROKIN;
                case "tenno": return WFtheme.TENNO;
                case "grineer": return WFtheme.GRINEER;
                case "corpus": return WFtheme.CORPUS;
                case "infested": return WFtheme.NIDUS;
                case "lotus": return WFtheme.LOTUS;
                case "fortuna": return WFtheme.FORTUNA;
                case "baruuk": return WFtheme.BARUUK;
                case "equinox": return WFtheme.EQUINOX;
                case "dark lotus": case "dark_lotus": return WFtheme.DARK_LOTUS;
                case "zephyr": return WFtheme.ZEPHYR;
                case "high contrast": case "high_contrast": return WFtheme.HIGH_CONTRAST;
                case "legacy": return WFtheme.LEGACY;
                default: return WFtheme.AUTO;
            }
        }

        private static void CompareResults(TestResult result)
        {
            // Count occurrences for multiset comparison
            var expectedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var actualCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var exp in result.ExpectedParts)
            {
                expectedCounts[exp] = expectedCounts.TryGetValue(exp, out int count) ? count + 1 : 1;
            }
            
            foreach (var act in result.ActualParts)
            {
                actualCounts[act] = actualCounts.TryGetValue(act, out int count) ? count + 1 : 1;
            }

            // Find missing parts (expected count > actual count)
            foreach (var kvp in expectedCounts)
            {
                int expectedCount = kvp.Value;
                int actualCount = actualCounts.TryGetValue(kvp.Key, out int count) ? count : 0;
                
                if (actualCount < expectedCount)
                {
                    for (int i = 0; i < expectedCount - actualCount; i++)
                    {
                        result.MissingParts.Add(kvp.Key);
                    }
                }
            }

            // Find extra parts (actual count > expected count)
            foreach (var kvp in actualCounts)
            {
                int actualCount = kvp.Value;
                int expectedCount = expectedCounts.TryGetValue(kvp.Key, out int count) ? count : 0;
                
                if (actualCount > expectedCount)
                {
                    for (int i = 0; i < actualCount - expectedCount; i++)
                    {
                        result.ExtraParts.Add(kvp.Key);
                    }
                }
            }

            // Calculate accuracy based on matched items
            int totalExpected = result.ExpectedParts.Count;
            int matched = 0;
            foreach (var kvp in expectedCounts)
            {
                int expectedCount = kvp.Value;
                int actualCount = actualCounts.TryGetValue(kvp.Key, out int count) ? count : 0;
                matched += Math.Min(expectedCount, actualCount);
            }
            
            result.AccuracyScore = totalExpected > 0 ? (double)matched / totalExpected * 100.0 : 0;
            result.Success = result.MissingParts.Count == 0 && result.ExtraParts.Count == 0 && string.IsNullOrEmpty(result.ErrorMessage);
        }

        private static void CalculateStatistics(TestSuiteResult suite)
        {
            suite.TotalTests = suite.TestResults.Count;
            suite.PassedTests = suite.TestResults.Count(t => t.Success);
            suite.FailedTests = suite.TestResults.Count(t => !t.Success && t.ErrorMessage == null);
            suite.ErrorTests = suite.TestResults.Count(t => t.ErrorMessage != null && !t.Success);
            suite.OverallAccuracy = suite.TestResults.Count > 0 ? suite.TestResults.Average(t => t.AccuracyScore) : 0;
            suite.PassRate = suite.TotalTests > 0 ? (double)suite.PassedTests / suite.TotalTests * 100 : 0;

            // Category coverage
            foreach (var group in suite.TestResults.GroupBy(t => t.Category ?? "unknown"))
            {
                suite.CategoryCoverage[group.Key] = BuildCoverage(group);
            }

            // Language coverage
            foreach (var group in suite.TestResults.GroupBy(t => t.Language ?? "unknown"))
            {
                suite.LanguageCoverage[group.Key] = BuildCoverage(group);
            }

            // Overall coverage
            suite.OverallCoverage = new TestCoverage
            {
                TotalTests = suite.TotalTests,
                PassedTests = suite.PassedTests,
                FailedTests = suite.FailedTests,
                PassRate = suite.PassRate,
                AverageAccuracy = suite.OverallAccuracy,
                AverageProcessingTime = suite.TestResults.Count > 0 ? suite.TestResults.Average(t => t.ProcessingTimeMs) : 0
            };
        }

        private static TestCoverage BuildCoverage(IGrouping<string, TestResult> group)
        {
            return new TestCoverage
            {
                TotalTests = group.Count(),
                PassedTests = group.Count(t => t.Success),
                FailedTests = group.Count(t => !t.Success),
                PassRate = group.Count() > 0 ? (double)group.Count(t => t.Success) / group.Count() * 100 : 0,
                AverageAccuracy = group.Average(t => t.AccuracyScore),
                AverageProcessingTime = group.Average(t => t.ProcessingTimeMs)
            };
        }

        public static void SaveResults(TestSuiteResult results, string outputPath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(results, Formatting.Indented);
                File.WriteAllText(outputPath, json);
                Main.AddLog($"Test results saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                Main.AddLog($"Failed to save results: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Headless HDR detector that returns a fixed value for testing.
    /// </summary>
    internal class HeadlessHDRDetector : IHDRDetectorService
    {
        public bool IsHDR { get; }
        public HeadlessHDRDetector(bool isHdr) { IsHDR = isHdr; }
    }
}
