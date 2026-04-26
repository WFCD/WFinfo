# OCR Test Framework Usage Example

## Quick Start

1. **Create test images** and place them in `tests/test_images/`
   - `english_reward_basic.png`
   - `korean_fissure.png` 
   - `japanese_snapit.png`
   - etc.

2. **Run tests** using the batch script:
   ```batch
   cd tests
   run_tests.bat test_images\
   ```

3. **Or run manually**:
   ```bash
   WFInfo.exe map.json test_images/ results.json
   ```

## Expected Output

The test framework will generate a comprehensive JSON report with:

```json
{
  "TestSuiteName": "map",
  "TotalTests": 5,
  "PassedTests": 4,
  "FailedTests": 1,
  "OverallAccuracy": 85.5,
  "LanguageAccuracy": {
    "english": 90.0,
    "korean": 80.0
  },
  "TestResults": [...]
}
```

## Integration Notes

The test framework uses:
- **Real OCR engines** with language-specific algorithms
- **Actual Levenshtein distance** implementations for each language
- **Proper character normalization** for international text
- **Theme detection** and scaling simulation
- **Comprehensive validation** and error reporting

This provides automated regression testing for all supported languages (English, Korean, Chinese Simplified/Traditional, French, Ukrainian, Italian, German, Spanish, Portuguese, Polish, Russian) across different UI themes, resolutions, and game scenarios. Note: Thai, Japanese, and Turkish are supported in the main application but excluded from automated testing.
