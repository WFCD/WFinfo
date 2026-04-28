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
        public List<string> Scenarios { get; set; } = new List<string>();
    }

    public class TestResult
    {
        public string TestCaseName { get; set; }
        public string ImagePath { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public string Category { get; set; }
        public bool Success { get; set; }
        public List<string> ExpectedParts { get; set; } = new List<string>();
        public List<string> ActualParts { get; set; } = new List<string>();
        public List<string> MissingParts { get; set; } = new List<string>();
        public List<string> ExtraParts { get; set; } = new List<string>();
        public double AccuracyScore { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TestSuiteResult
    {
        public string TestSuiteName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int ErrorTests { get; set; }
        public double OverallAccuracy { get; set; }
        public double PassRate { get; set; }
        public Dictionary<string, TestCoverage> CategoryCoverage { get; set; } = new Dictionary<string, TestCoverage>();
        public Dictionary<string, TestCoverage> LanguageCoverage { get; set; } = new Dictionary<string, TestCoverage>();
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
}
