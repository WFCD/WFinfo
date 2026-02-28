# WFInfo OCR Test Framework

Regression and accuracy testing for WFInfo's OCR pipeline. Runs **headlessly** from the command line using the **real** WFInfo OCR methods (no mocks or copied code).

## How It Works

1. The runner reads `map.json` which lists scenario paths.
2. Each scenario is a **PNG + JSON pair** (e.g. `data/test1.png` + `data/test1.json`).
3. The JSON spec defines language, theme, HDR, scaling, category, and expected part names.
4. WFInfo's real OCR pipeline processes the screenshot:
   - **Reward screens**: `ExtractPartBoxAutomatically` → `GetTextFromImage` → `GetPartName`
   - **SnapIt**: `ScaleUpAndFilter` → `FindAllParts` → `GetPartName`
5. Actual results are compared against expected parts; accuracy and pass/fail are reported.

## Directory Structure

```
tests/
├── map.json           # Lists scenarios to run
├── run_tests.bat      # One-click Windows runner
├── data/
│   ├── test1.json     # Test spec
│   ├── test1.png      # Corresponding screenshot
│   ├── test2.json
│   ├── test2.png
│   └── ...
```

## Quick Start

### 1. Build the project
```batch
dotnet build WFInfo.sln -c Release
```

### 2. Run tests
```batch
cd tests
run_tests.bat
```

Or manually:
```batch
WFInfo.exe --test map.json results.json
WFInfo.exe map.json results.json
```

If no output file is specified, results go to `test_results_<timestamp>.json`.

### 3. Check results
The runner prints a summary to stdout and writes detailed JSON to the output file.

## Test Spec Format (JSON)

Each test scenario JSON file:

```json
{
  "description": "Basic English reward screen with 4 items",
  "resolution": "1920x1080",
  "scaling": 100,
  "theme": "orokin",
  "language": "english",
  "parts": {
    "0": "Volt Prime Blueprint",
    "1": "Mag Prime Blueprint",
    "2": "Ash Prime Blueprint",
    "3": "Trinity Prime Blueprint"
  },
  "category": "reward",
  "hdr": false,
  "filters": []
}
```

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `description` | No | Human-readable description |
| `resolution` | No | Source resolution (informational) |
| `scaling` | Yes | UI scaling percentage (100 = 100%) |
| `theme` | Yes | UI theme name (see below) |
| `language` | Yes | Language name (see below) |
| `parts` | Yes | Map of index → expected part name (English) |
| `category` | Yes | `reward` or `snapit` |
| `hdr` | Yes | Whether the screenshot is HDR |
| `filters` | No | Optional filter tags (e.g. `colorblind`) |

## map.json Format

```json
{
  "scenarios": [
    "data/test1",
    "data/test2",
    "data/test3"
  ]
}
```

Each entry is a path (relative to `map.json`) without extension. The runner appends `.json` and `.png`.

## Supported Values

### Categories
- **`reward`** — Fissure reward screen (1-4 items)
- **`snapit`** — SnapIt inventory scanning

### Languages
`english`, `korean`, `japanese`, `simplified chinese`, `traditional chinese`, `thai`, `french`, `ukrainian`, `italian`, `german`, `spanish`, `portuguese`, `polish`, `turkish`, `russian`

### Themes
`orokin`, `tenno`, `grineer`, `corpus`, `infested`, `lotus`, `fortuna`, `baruuk`, `equinox`, `dark lotus` / `dark_lotus`, `zephyr`, `high contrast` / `high_contrast`, `legacy`, `auto`

## Output Format

```json
{
  "TestSuiteName": "map",
  "TotalTests": 3,
  "PassedTests": 2,
  "FailedTests": 1,
  "ErrorTests": 0,
  "PassRate": 66.7,
  "OverallAccuracy": 83.3,
  "CategoryCoverage": { ... },
  "LanguageCoverage": { ... },
  "OverallCoverage": { ... },
  "TestResults": [
    {
      "TestCaseName": "test1",
      "Language": "english",
      "Theme": "orokin",
      "Category": "reward",
      "Success": true,
      "AccuracyScore": 100.0,
      "ProcessingTimeMs": 1250,
      "ExpectedParts": ["Volt Prime Blueprint", ...],
      "ActualParts": ["Volt Prime Blueprint", ...],
      "MissingParts": [],
      "ExtraParts": []
    }
  ]
}
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All tests passed |
| 1 | Some tests failed |
| 2 | Fatal error (missing files, init failure, etc.) |

## Architecture

The test runner calls WFInfo's internal methods directly:

- `OCR.InitForTest()` — headless OCR initialization (real TesseractService, no sound/screenshot services)
- `OCR.ProcessRewardScreenForTest()` — full reward pipeline: extract part boxes → Tesseract OCR → Levenshtein matching
- `OCR.ProcessSnapItForTest()` — full SnapIt pipeline: theme detection → filter → find parts → matching
- `Data.GetPartName()` — real Levenshtein-based name matching against the market database
- `LanguageProcessorFactory` — real language-specific processing (CJK, Cyrillic, Latin, etc.)

Settings (locale, theme, scaling) are applied via `ApplicationSettings.GlobalSettings` before each test, and Tesseract engines are reloaded when the language changes.

## Adding New Tests

1. Take a screenshot in Warframe
2. Save as `tests/data/<name>.png`
3. Create `tests/data/<name>.json` with the spec (see format above)
4. Add `"data/<name>"` to `map.json` scenarios list
5. Run `run_tests.bat`

## Troubleshooting

- **"Databases not ready"** — First run downloads market data from the internet. Ensure connectivity.
- **"PNG not found"** — The `.png` must be next to the `.json` with the same base name.
- **Low accuracy** — Check that expected part names match WFInfo's English database names exactly.
- **Tesseract errors** — Ensure tessdata files are available in `%APPDATA%\WFInfo\tessdata\`.
- **Debug logs** — Check `%APPDATA%\WFInfo\debug.log` for detailed OCR pipeline logs.
