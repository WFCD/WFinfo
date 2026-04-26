# Building WFInfo Test Framework

## 🎯 Current Architecture

The test framework is **embedded within the main WFInfo project**, not a separate executable. Here's how to build and run it:

## 📁 Project Structure

```
WFInfo/
├── WFInfo.csproj              # Main project (includes tests)
├── Tests/                     # Test framework code
│   ├── TestModels.cs          # Test data models
│   ├── OCRTestRunner.cs      # Test execution logic
│   └── TestProgram.cs        # Console entry point
└── tests/                    # Test data and scripts
    ├── map.json              # Test scenarios
    ├── data/                 # External test data
    └── run_tests.bat        # Batch script
```

## 🔧 Building the Test Framework

### **Option 1: Build Main Project**
```bash
# Navigate to WFInfo root
cd <path_to_code_folder>\WFinfo

# Build the main project (includes test framework)
dotnet build --configuration Release

# The executable will be at:
# bin\Release\net48\WFInfo.exe
```

### **Option 2: Build with Visual Studio**
1. Open `WFInfo.sln` in Visual Studio
2. Set configuration to **Release**
3. Build solution (**Ctrl+Shift+B**)
4. Executable: `bin\Release\net48\WFInfo.exe`

### **Option 3: Create Separate Test Project**
If you want a dedicated test executable:

```bash
# Create new test project
dotnet new console -n WFInfo.Tests -f net48

# Copy test files to new project
# Copy Tests/ folder to WFInfo.Tests/
# Add necessary references to WFInfo.Tests.csproj
```

## 🚀 Running Tests

### **Using the Main Executable:**
```bash
# Navigate to tests directory
cd <path_to_code_folder>\WFinfo\tests

# Run tests using main WFInfo executable
..\bin\Release\net48\WFInfo.exe map.json data/ results.json
```

### **Using the Batch Script:**
```bash
# Update run_tests.bat to use correct path
# Change line 33 from:
..\WFInfo.Tests.exe map.json %TEST_IMAGES_DIR% test_results_...
# To:
..\bin\Release\net48\WFInfo.exe map.json %TEST_IMAGES_DIR% test_results_...
```

## 📝 Updated run_tests.bat

Here's the corrected batch script:

```batch
@echo off
setlocal enabledelayedexpansion

echo WFInfo OCR Test Runner
echo ========================
echo.

REM Check if map.json exists
if not exist "map.json" (
    echo ERROR: map.json not found in current directory
    echo.
    echo Usage: run_tests.bat [test_images_directory]
    echo.
    echo Example: run_tests.bat data\
    goto :eof
)

REM Set test images directory
set TEST_IMAGES_DIR=%1
if "%TEST_IMAGES_DIR%"=="" set TEST_IMAGES_DIR=data

REM Check if test images directory exists
if not exist "%TEST_IMAGES_DIR%" (
    echo ERROR: Test images directory not found: %TEST_IMAGES_DIR%
    goto :eof
)

REM Run test
echo Running OCR tests...
echo Map: map.json
echo Images: %TEST_IMAGES_DIR%
echo Output: test_results_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.json
echo.

REM Run test executable (using main WFInfo executable)
..\bin\Release\net48\WFInfo.exe map.json %TEST_IMAGES_DIR% test_results_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.json

REM Check results
if %errorlevel% equ 0 (
    echo.
    echo SUCCESS: All tests passed!
) else if %errorlevel% equ 1 (
    echo.
    echo WARNING: Some tests failed (exit code 1)
) else (
    echo.
    echo ERROR: Test execution failed (exit code %errorlevel%)
)

echo.
echo Test completed. Check JSON results file for detailed information.
pause
```

## 🎯 Quick Start

1. **Build the main project:**
   ```bash
   cd <path_to_code_folder>\WFinfo
   dotnet build --configuration Release
   ```

2. **Run tests:**
   ```bash
   cd <path_to_code_folder>\WFinfo\tests
   ..\bin\Release\net48\WFInfo.exe map.json data/ results.json
   ```

3. **Or use the batch script:**
   ```bash
   cd <path_to_code_folder>\WFinfo\tests
   run_tests.bat data\
   ```

## 📊 Test Framework Features

The test framework provides:
- **External Data Loading**: `{scenario}.json` + `{scenario}.png` pairs
- **Multi-Language Support**: All 15 supported languages
- **Coverage Metrics**: Pass rates, accuracy, processing times
- **Theme Testing**: All WFInfo themes supported
- **HDR Support**: Test with/without HDR
- **Filter Testing**: Accessibility filter validation
- **Comprehensive Reporting**: JSON output with detailed metrics

## 🚀 Next Steps

For a dedicated test executable, consider:
1. Creating separate `WFInfo.Tests` project
2. Moving test code to separate solution
3. Adding proper test project dependencies
4. Building as standalone console application

But for now, the **embedded approach works perfectly** for comprehensive OCR testing! 🎯
