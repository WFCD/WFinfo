# WFInfo OCR Test Framework

This test framework allows you to run comprehensive OCR tests programmatically without the UI, supporting all 15 languages with various themes, resolutions, and categories.

## Features

- **Multi-language Support**: Tests all supported languages (English, Korean, Japanese, Chinese Simplified/Traditional, French, Ukrainian, Italian, German, Spanish, Portuguese, Polish, Russian) - excludes Thai, Japanese, and Turkish from automated testing
- **Category Testing**: Reward screens (including fissure rewards), SnapIt inventory, Profile screens
- **Theme Testing**: All Warframe UI themes (Orokin, Tenno, Grineer, Corpus, etc.)
- **HDR Support**: Test both HDR and non-HDR scenarios
- **Custom Filters**: Support for colorblind filters and other visual modifications
- **Detailed Reporting**: Accuracy metrics, processing times, missing/extra parts detection

## Quick Start

### 1. Prepare Test Files
```text
tests/
├── map.json              # Test scenarios configuration
├── run_tests.bat          # Windows batch runner
├── test_images/           # Directory containing test images
│   ├── english_reward_basic.png
│   ├── korean_fissure.png
│   ├── japanese_snapit.png
│   └── ...
└── results/               # Generated test results
```

### 2. Create Test Scenarios

Edit `map.json` to define your test cases:

```json
{
  "scenarios": {
    "test_name": {
      "description": "Test description",
      "resolution": "1920x1080",
      "scaling": 100,
      "theme": "orokin",
      "language": "english",
      "parts": {
        "0": "Expected Part Name"
      },
      "category": "reward",
      "hdr": false,
      "filters": []
    }
  }
}
```

### 3. Run Tests

**Windows:**
```batch
cd tests
run_tests.bat test_images\
```

**Manual:**
```bash
WFInfo.Tests.exe map.json test_images/ results.json
```

## Test Categories

### Categories
- **`reward`**: Standard reward screen with 4 items (includes fissure rewards)
- **`inventory`**: Profile/inventory screen scanning
- **`snapit`**: Inventory screen scanning

**Note**: Fissure rewards are treated as a subtype of the `reward` category and should use `"category": "reward"` in map.json files.

### Languages
- **English** (`english`)
- **Korean** (`korean`) - 한국어
- **Japanese** (`japanese`) - 日本语
- **Simplified Chinese** (`simplified chinese`) - 简体中文
- **Traditional Chinese** (`traditional chinese`) - 繁體中文
- **French** (`french`) - Français
- **Ukrainian** (`ukrainian`) - Українська
- **Italian** (`italian`) - Italiano
- **German** (`german`) - Deutsch
- **Spanish** (`spanish`) - Español
- **Portuguese** (`portuguese`) - Português
- **Polish** (`polish`) - Polski
- **Russian** (`russian`) - Русский

**Note**: Thai and Turkish are supported in the main application but excluded from automated testing.

### Themes
- **Orokin** (`orokin`)
- **Tenno** (`tenno`)
- **Grineer** (`grineer`)
- **Corpus** (`corpus`)
- **Infested** (`infested`) - Maps to NIDUS
- **Lotus** (`lotus`)
- **Fortuna** (`fortuna`)
- **Baruuk** (`baruuk`)
- **Equinox** (`equinox`)
- **Dark Lotus** (`dark_lotus`)
- **Zephyr** (`zephyr`)
- **High Contrast** (`high_contrast`)
- **Legacy** (`legacy`)

## Results

The test framework generates comprehensive JSON reports with:

```json
{
  "TestSuiteName": "map",
  "TotalTests": 5,
  "PassedTests": 4,
  "FailedTests": 1,
  "PassRate": 80.0,
  "OverallAccuracy": 85.5,
  "LanguageAccuracy": {
    "english": 90.0,
    "korean": 80.0
  },
  "CategoryCoverage": {
    "reward": {
      "TotalTests": 3,
      "PassedTests": 2,
      "FailedTests": 1,
      "PassRate": 66.7,
      "AverageAccuracy": 88.3,
      "AverageProcessingTime": 1250.0
    },
    "inventory": {
      "TotalTests": 2,
      "PassedTests": 2,
      "FailedTests": 0,
      "PassRate": 100.0,
      "AverageAccuracy": 82.5,
      "AverageProcessingTime": 980.0
    }
  },
  "LanguageCoverage": {
    "english": {
      "TotalTests": 3,
      "PassedTests": 3,
      "FailedTests": 0,
      "PassRate": 100.0,
      "AverageAccuracy": 91.7,
      "AverageProcessingTime": 1100.0
    },
    "korean": {
      "TotalTests": 2,
      "PassedTests": 1,
      "FailedTests": 1,
      "PassRate": 50.0,
      "AverageAccuracy": 79.0,
      "AverageProcessingTime": 1400.0
    }
  },
  "OverallCoverage": {
    "TotalTests": 5,
    "PassedTests": 4,
    "FailedTests": 1,
    "PassRate": 80.0,
    "AverageAccuracy": 85.5,
    "AverageProcessingTime": 1220.0
  },
  "TestResults": [
    {
      "TestCaseName": "english_reward_basic",
      "Success": true,
      "AccuracyScore": 100.0,
      "ProcessingTimeMs": 1250,
      "ExpectedParts": [...],
      "ActualParts": [...],
      "MissingParts": [],
      "ExtraParts": []
    }
  ]
}
```

## Integration with WFInfo

The test framework uses the actual OCR engine and language-specific algorithms:

- **Levenshtein Distance**: Language-specific implementations for optimal matching
- **Character Normalization**: Diacritic handling for European languages, full-width conversion for Asian languages
- **Blueprint Removal**: Language-specific term removal (설계도, 設計図, 蓝图, Schéma, Bauplan, etc.)
- **Validation Logic**: Minimum character length validation per language

## Exit Codes

- **0**: Success - All tests passed
- **1**: Warning - Some tests failed
- **2**: Error - Test execution failed

## Advanced Usage

### Regression Testing
Create comprehensive test suites for regression testing:

```json
{
  "categories": {
    "reward": ["test1", "test2", "test3", "fissure_test1", "fissure_test2"],
    "inventory": ["inventory_test1", "inventory_test2"],
    "snapit": ["snapit_test1", "snapit_test2"]
  }
}
```

### Performance Testing
Monitor processing times and accuracy across different:
- Resolutions (1920x1080, 2560x1440, 3840x2160)
- Scaling factors (100%, 125%, 150%)
- HDR vs non-HDR
- Language complexity (Latin vs Cyrillic vs Asian scripts)

### CI/CD Integration
Perfect for automated testing pipelines:
- JSON output for easy parsing
- Exit codes for build status
- Detailed logging for debugging
- Batch scripts for Windows environments

## Troubleshooting

### Common Issues
1. **Missing Images**: Ensure all PNG files exist in test_images directory
2. **Language Not Supported**: Check language spelling in JSON matches supported locales
3. **Theme Detection Failures**: Verify theme names are valid WFtheme enum values
4. **OCR Engine Issues**: Ensure traineddata files are downloaded for test languages

### Debug Mode
Add `"debug": true` to test scenarios for verbose logging and intermediate image saving.

This framework provides comprehensive, automated testing of WFInfo's OCR capabilities across all supported languages and scenarios.
