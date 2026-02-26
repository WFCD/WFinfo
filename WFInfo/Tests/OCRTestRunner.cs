using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using WFInfo.Settings;
using WFInfo.Services.WindowInfo;
using WFInfo.Services.Screenshot;
using WFInfo.Services.HDRDetection;

namespace WFInfo.Tests
{
    public class OCRTestRunner
    {
        private readonly IDataService _dataService;
        private readonly ITesseractService _tesseractService;
        private readonly IWindowInfoService _windowService;
        private readonly IScreenshotService _screenshotService;
        private readonly IHDRDetectorService _hdrDetector;

        public OCRTestRunner(IDataService dataService, ITesseractService tesseractService, 
            IWindowInfoService windowService, IScreenshotService screenshotService, 
            IHDRDetectorService hdrDetector)
        {
            _dataService = dataService;
            _tesseractService = tesseractService;
            _windowService = windowService;
            _screenshotService = screenshotService;
            _hdrDetector = hdrDetector;
        }

        public TestSuiteResult RunTestSuite(string testMapPath, string testImagesDirectory)
        {
            var result = new TestSuiteResult
            {
                TestSuiteName = Path.GetFileNameWithoutExtension(testMapPath),
                StartTime = DateTime.UtcNow,
                TestResults = new List<TestResult>(),
                LanguageAccuracy = new Dictionary<string, double>(),
                ThemeAccuracy = new Dictionary<string, double>(),
                CategoryAccuracy = new Dictionary<string, double>(),
                CategoryCoverage = new Dictionary<string, TestCoverage>(),
                LanguageCoverage = new Dictionary<string, TestCoverage>(),
                OverallCoverage = new TestCoverage()
            };

            try
            {
                // Load test map
                var testMapJson = File.ReadAllText(testMapPath);
                var testMap = JsonConvert.DeserializeObject<TestMap>(testMapJson);

                Main.AddLog($"Starting test suite: {result.TestSuiteName} with {testMap.Scenarios.Count} test cases");

                // Run each test scenario
                foreach (var scenario in testMap.Scenarios)
                {
                    var testResult = RunSingleTest(scenario, testImagesDirectory, Path.GetDirectoryName(testMapPath));
                    result.TestResults.Add(testResult);
                }

                // Calculate final statistics
                result.TotalTests = result.TestResults.Count;
                result.PassedTests = result.TestResults.Count(t => t.Success);
                result.FailedTests = result.TotalTests - result.PassedTests;
                result.OverallAccuracy = result.TestResults.Average(t => t.AccuracyScore);
                result.PassRate = result.TotalTests > 0 ? (double)result.PassedTests / result.TotalTests * 100 : 0;
                
                // Calculate coverage metrics
                CalculateCoverageMetrics(result);
                
                result.EndTime = DateTime.UtcNow;

                Main.AddLog($"Test suite completed: {result.PassedTests}/{result.TotalTests} passed, {result.PassRate:F1}% pass rate, {result.OverallAccuracy:F2}% overall accuracy");

                return result;
            }
            catch (Exception ex)
            {
                Main.AddLog($"Test suite failed: {ex.Message}");
                result.EndTime = DateTime.UtcNow;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private TestResult RunSingleTest(string scenarioPath, string testImagesDirectory, string testMapDirectory)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TestCaseName = Path.GetFileNameWithoutExtension(scenarioPath),
                ImagePath = Path.Combine(testImagesDirectory, Path.GetFileNameWithoutExtension(scenarioPath) + ".png"),
                ExpectedParts = new List<PartMatchResult>(),
                ActualParts = new List<PartMatchResult>(),
                MissingParts = new List<string>(),
                ExtraParts = new List<string>(),
                AccuracyScore = 0,
                ProcessingTimeMs = 0
            };

            try
            {
                Main.AddLog($"Running test: {result.TestCaseName}");

                // Load test data from external file
                var testDataPath = Path.Combine(testMapDirectory, scenarioPath + ".json");
                if (!File.Exists(testDataPath))
                {
                    result.ErrorMessage = $"Test data file not found: {testDataPath}";
                    result.Success = false;
                    return result;
                }

                var testDataJson = File.ReadAllText(testDataPath);
                var testCase = JsonConvert.DeserializeObject<TestCase>(testDataJson);

                // Load test image
                if (!File.Exists(result.ImagePath))
                {
                    result.ErrorMessage = $"Test image not found: {result.ImagePath}";
                    result.Success = false;
                    return result;
                }

                // Setup test environment
                SetupTestEnvironment(testCase);

                // Load test image
                using (var bitmap = new Bitmap(result.ImagePath))
                {
                    // Process image based on category
                    var ocrResults = ProcessImageByCategory(bitmap, testCase.Category);

                    // Build expected parts from test data
                    foreach (var expectedPart in testCase.Parts)
                    {
                        result.ExpectedParts.Add(new PartMatchResult
                        {
                            OriginalText = expectedPart.Value,
                            MatchedName = expectedPart.Value,
                            IsExactMatch = true,
                            Confidence = 1.0
                        });
                    }

                    // Compare results
                    CompareResults(result, ocrResults);
                }

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                Main.AddLog($"Test {result.TestCaseName} completed in {result.ProcessingTimeMs}ms - Success: {result.Success}, Accuracy: {result.AccuracyScore:F2}%");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.ErrorMessage = ex.Message;
                result.Success = false;
                Main.AddLog($"Test {result.TestCaseName} failed: {ex.Message}");
                return result;
            }
        }

        private void SetupTestEnvironment(TestCase testCase)
        {
            // Apply test settings
            var settings = ApplicationSettings.GlobalSettings;
            
            // Set language
            var langLower = testCase.Language.ToLower();
            switch (langLower)
            {
                case "english":
                    settings.Locale = "en";
                    break;
                case "korean":
                    settings.Locale = "ko";
                    break;
                case "japanese":
                    settings.Locale = "ja";
                    break;
                case "simplified chinese":
                    settings.Locale = "zh-hans";
                    break;
                case "traditional chinese":
                    settings.Locale = "zh-hant";
                    break;
                case "thai":
                    settings.Locale = "th";
                    break;
                case "french":
                    settings.Locale = "fr";
                    break;
                case "ukrainian":
                    settings.Locale = "uk";
                    break;
                case "italian":
                    settings.Locale = "it";
                    break;
                case "german":
                    settings.Locale = "de";
                    break;
                case "spanish":
                    settings.Locale = "es";
                    break;
                case "portuguese":
                    settings.Locale = "pt";
                    break;
                case "polish":
                    settings.Locale = "pl";
                    break;
                case "turkish":
                    settings.Locale = "tr";
                    break;
                case "russian":
                    settings.Locale = "ru";
                    break;
                default:
                    settings.Locale = "en";
                    break;
            }

            // Set theme
            var themeLower = testCase.Theme.ToLower();
            switch (themeLower)
            {
                case "orokin":
                    settings.ThemeSelection = WFtheme.OROKIN;
                    break;
                case "tenno":
                    settings.ThemeSelection = WFtheme.TENNO;
                    break;
                case "grineer":
                    settings.ThemeSelection = WFtheme.GRINEER;
                    break;
                case "corpus":
                    settings.ThemeSelection = WFtheme.CORPUS;
                    break;
                case "infested":
                    settings.ThemeSelection = WFtheme.NIDUS;
                    break;
                case "lotus":
                    settings.ThemeSelection = WFtheme.LOTUS;
                    break;
                case "fortuna":
                    settings.ThemeSelection = WFtheme.FORTUNA;
                    break;
                case "baruuk":
                    settings.ThemeSelection = WFtheme.BARUUK;
                    break;
                case "equinox":
                    settings.ThemeSelection = WFtheme.EQUINOX;
                    break;
                case "dark lotus":
                    settings.ThemeSelection = WFtheme.DARK_LOTUS;
                    break;
                case "zephyr":
                    settings.ThemeSelection = WFtheme.ZEPHYR;
                    break;
                case "high contrast":
                    settings.ThemeSelection = WFtheme.HIGH_CONTRAST;
                    break;
                case "legacy":
                    settings.ThemeSelection = WFtheme.LEGACY;
                    break;
                default:
                    settings.ThemeSelection = WFtheme.AUTO;
                    break;
            }

            // Set scaling
            OCR.uiScaling = testCase.Scaling / 100.0;

            // Reload OCR engines with new settings
            _tesseractService.ReloadEngines();
        }

        private List<PartMatchResult> ProcessImageByCategory(Bitmap image, string category)
        {
            var results = new List<PartMatchResult>();

            switch (category.ToLower())
            {
                case "reward":
                    return ProcessRewardScreen(image);
                
                case "inventory":
                    return ProcessInventoryScreen(image);
                
                case "snapit":
                    return ProcessSnapIt(image);
                
                default:
                    return ProcessRewardScreen(image); // Default to reward screen processing
            }
        }

        private List<PartMatchResult> ProcessRewardScreen(Bitmap image)
        {
            var results = new List<PartMatchResult>();
            
            try
            {
                // Simulate reward screen processing - basic OCR on the whole image
                // This is a simplified approach since we can't access the private ExtractPartBoxAutomatically method
                var ocrText = OCR.GetTextFromImage(image, _tesseractService.FirstEngine);
                
                if (!string.IsNullOrEmpty(ocrText) && ocrText.Replace(" ", "").Length > 6)
                {
                    var matchedName = _dataService.GetPartName(ocrText, out int distance, false, out bool multipleLowest);
                        
                    results.Add(new PartMatchResult
                    {
                        OriginalText = ocrText,
                        MatchedName = matchedName,
                        LevenshteinDistance = distance,
                        IsExactMatch = ocrText.Equals(matchedName, StringComparison.OrdinalIgnoreCase),
                        Confidence = CalculateConfidence(distance, ocrText.Length, matchedName.Length)
                    });
                }
            }
            catch (Exception ex)
            {
                Main.AddLog($"Reward screen processing failed: {ex.Message}");
            }

            return results;
        }

        private List<PartMatchResult> ProcessSnapIt(Bitmap image)
        {
            var results = new List<PartMatchResult>();
            
            try
            {
                // Use existing SnapIt logic - simulate process
                var filteredImage = OCR.ScaleUpAndFilter(image, WFtheme.AUTO, out _, out _);
                
                // Since FindAllParts is private, we'll simulate basic OCR on whole image
                var ocrText = OCR.GetTextFromImage(image, _tesseractService.FirstEngine);
                
                if (!string.IsNullOrEmpty(ocrText) && OCR.PartNameValid(ocrText))
                {
                    var matchedName = _dataService.GetPartName(ocrText, out int distance, false, out bool multipleLowest);
                    
                    results.Add(new PartMatchResult
                    {
                        OriginalText = ocrText,
                        MatchedName = matchedName,
                        LevenshteinDistance = distance,
                        IsExactMatch = ocrText.Equals(matchedName, StringComparison.OrdinalIgnoreCase),
                        Confidence = CalculateConfidence(distance, ocrText.Length, matchedName.Length)
                    });
                }
            }
            catch (Exception ex)
            {
                Main.AddLog($"SnapIt processing failed: {ex.Message}");
            }

            return results;
        }

        private List<PartMatchResult> ProcessInventoryScreen(Bitmap image)
        {
            var results = new List<PartMatchResult>();
            
            try
            {
                // Use inventory OCR logic
                var ocrText = OCR.GetTextFromImage(image, _tesseractService.FirstEngine);
                
                if (!string.IsNullOrEmpty(ocrText))
                {
                    var matchedName = _dataService.GetPartName(ocrText, out int distance, false, out bool multipleLowest);
                    
                    results.Add(new PartMatchResult
                    {
                        OriginalText = ocrText,
                        MatchedName = matchedName,
                        LevenshteinDistance = distance,
                        IsExactMatch = ocrText.Equals(matchedName, StringComparison.OrdinalIgnoreCase),
                        Confidence = CalculateConfidence(distance, ocrText.Length, matchedName.Length)
                    });
                }
            }
            catch (Exception ex)
            {
                Main.AddLog($"Inventory processing failed: {ex.Message}");
            }

            return results;
        }

        private void CompareResults(TestResult result, List<PartMatchResult> ocrResults)
        {
            result.ActualParts = ocrResults;

            // Find missing parts (expected but not found)
            foreach (var expected in result.ExpectedParts)
            {
                var found = result.ActualParts.FirstOrDefault(p => 
                    p.MatchedName.Equals(expected.MatchedName, StringComparison.OrdinalIgnoreCase));
                
                if (found == null)
                {
                    result.MissingParts.Add(expected.MatchedName);
                }
            }

            // Find extra parts (found but not expected)
            foreach (var actual in result.ActualParts)
            {
                var expected = result.ExpectedParts.FirstOrDefault(p => 
                    p.MatchedName.Equals(actual.MatchedName, StringComparison.OrdinalIgnoreCase));
                
                if (expected == null)
                {
                    result.ExtraParts.Add(actual.MatchedName);
                }
            }

            // Calculate accuracy
            var totalExpected = result.ExpectedParts.Count;
            var correctlyIdentified = totalExpected - result.MissingParts.Count;
            result.AccuracyScore = totalExpected > 0 ? (double)correctlyIdentified / totalExpected * 100 : 0;
            result.Success = result.AccuracyScore >= 50.0; // Consider 50%+ as passing
        }

        private double CalculateConfidence(int levenshteinDistance, int originalLength, int matchedLength)
        {
            if (originalLength == 0 || matchedLength == 0) return 0;
            
            var maxLength = Math.Max(originalLength, matchedLength);
            var similarity = (double)(maxLength - levenshteinDistance) / maxLength;
            return Math.Max(0, similarity);
        }

        private void CalculateCoverageMetrics(TestSuiteResult suiteResult)
        {
            suiteResult.CategoryCoverage = new Dictionary<string, TestCoverage>();
            suiteResult.LanguageCoverage = new Dictionary<string, TestCoverage>();

            // Calculate category coverage
            var categoryGroups = suiteResult.TestResults.GroupBy(t => GetTestCategory(t.TestCaseName));
            foreach (var group in categoryGroups)
            {
                var coverage = new TestCoverage
                {
                    TotalTests = group.Count(),
                    PassedTests = group.Count(t => t.Success),
                    FailedTests = group.Count(t => !t.Success),
                    PassRate = group.Count() > 0 ? (double)group.Count(t => t.Success) / group.Count() * 100 : 0,
                    AverageAccuracy = group.Average(t => t.AccuracyScore),
                    AverageProcessingTime = group.Average(t => t.ProcessingTimeMs)
                };
                suiteResult.CategoryCoverage[group.Key] = coverage;
            }

            // Calculate language coverage
            var languageGroups = suiteResult.TestResults.GroupBy(t => GetTestLanguage(t.TestCaseName));
            foreach (var group in languageGroups)
            {
                var coverage = new TestCoverage
                {
                    TotalTests = group.Count(),
                    PassedTests = group.Count(t => t.Success),
                    FailedTests = group.Count(t => !t.Success),
                    PassRate = group.Count() > 0 ? (double)group.Count(t => t.Success) / group.Count() * 100 : 0,
                    AverageAccuracy = group.Average(t => t.AccuracyScore),
                    AverageProcessingTime = group.Average(t => t.ProcessingTimeMs)
                };
                suiteResult.LanguageCoverage[group.Key] = coverage;
            }

            // Calculate overall coverage
            suiteResult.OverallCoverage = new TestCoverage
            {
                TotalTests = suiteResult.TotalTests,
                PassedTests = suiteResult.PassedTests,
                FailedTests = suiteResult.FailedTests,
                PassRate = suiteResult.PassRate,
                AverageAccuracy = suiteResult.OverallAccuracy,
                AverageProcessingTime = suiteResult.TestResults.Average(t => t.ProcessingTimeMs)
            };
        }

        private string GetTestCategory(string scenarioPath)
        {
            // Extract category from scenario path or use default
            var fileName = Path.GetFileNameWithoutExtension(scenarioPath).ToLower();
            if (fileName.Contains("reward") || fileName.Contains("fissure"))
                return "reward";
            else if (fileName.Contains("inventory") || fileName.Contains("profile"))
                return "inventory";
            else if (fileName.Contains("snapit"))
                return "snapit";
            else
                return "unknown";
        }

        private string GetTestLanguage(string scenarioPath)
        {
            // Extract language from scenario path
            var fileName = Path.GetFileNameWithoutExtension(scenarioPath).ToLower();
            if (fileName.Contains("english")) return "english";
            if (fileName.Contains("korean")) return "korean";
            if (fileName.Contains("japanese")) return "japanese";
            if (fileName.Contains("chinese")) return "chinese";
            if (fileName.Contains("thai")) return "thai";
            if (fileName.Contains("french")) return "french";
            if (fileName.Contains("ukrainian")) return "ukrainian";
            if (fileName.Contains("italian")) return "italian";
            if (fileName.Contains("german")) return "german";
            if (fileName.Contains("spanish")) return "spanish";
            if (fileName.Contains("portuguese")) return "portuguese";
            if (fileName.Contains("polish")) return "polish";
            if (fileName.Contains("turkish")) return "turkish";
            if (fileName.Contains("russian")) return "russian";
            return "unknown";
        }

        public void SaveResults(TestSuiteResult results, string outputPath)
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

    public interface IDataService
    {
        string GetPartName(string name, out int low, bool suppressLogging, out bool multipleLowest);
    }
}
