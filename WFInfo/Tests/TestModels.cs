using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WFInfo.Tests
{
    public class TestCase
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        [JsonProperty("scaling")]
        public int Scaling { get; set; }

        [JsonProperty("theme")]
        public string Theme { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("parts")]
        public Dictionary<string, string> Parts { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("hdr")]
        public bool HDR { get; set; }

        [JsonProperty("filters")]
        public List<string> Filters { get; set; }
    }

    public class TestMap
    {
        [JsonProperty("scenarios")]
        public List<string> Scenarios { get; set; }

        [JsonProperty("categories")]
        public Dictionary<string, List<string>> Categories { get; set; }
    }

    public class TestResult
    {
        public string TestCaseName { get; set; }
        public string ImagePath { get; set; }
        public bool Success { get; set; }
        public List<PartMatchResult> ExpectedParts { get; set; }
        public List<PartMatchResult> ActualParts { get; set; }
        public List<string> MissingParts { get; set; }
        public List<string> ExtraParts { get; set; }
        public double AccuracyScore { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PartMatchResult
    {
        public string OriginalText { get; set; }
        public string MatchedName { get; set; }
        public int LevenshteinDistance { get; set; }
        public bool IsExactMatch { get; set; }
        public double Confidence { get; set; }
    }

    public class TestSuiteResult
    {
        public string TestSuiteName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<TestResult> TestResults { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double OverallAccuracy { get; set; }
        public double PassRate { get; set; }
        public Dictionary<string, double> LanguageAccuracy { get; set; }
        public Dictionary<string, double> ThemeAccuracy { get; set; }
        public Dictionary<string, double> CategoryAccuracy { get; set; }
        public Dictionary<string, TestCoverage> CategoryCoverage { get; set; }
        public Dictionary<string, TestCoverage> LanguageCoverage { get; set; }
        public TestCoverage OverallCoverage { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TestCoverage
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double PassRate { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
    }

    public enum TestCategory
    {
        RewardScreen,
        SnapIt,
        Inventory,
        Profile,
        Fissure,
        All
    }
}
