@echo off
setlocal enabledelayedexpansion

echo WFInfo OCR Test Runner
echo ========================
echo.

REM Check if map.json exists
if not exist "map.json" (
    echo ERROR: map.json not found in current directory
    echo.
    echo Usage: run_tests.bat [test_data_directory]
    echo.
    echo Example: run_tests.bat data\
    exit /b 2
)

REM Set test images directory
set "TEST_IMAGES_DIR=%~1"
if "%TEST_IMAGES_DIR%"=="" set "TEST_IMAGES_DIR=data"

REM Check if test images directory exists
if not exist "%TEST_IMAGES_DIR%" (
    echo ERROR: Test images directory not found: "%TEST_IMAGES_DIR%"
    exit /b 3
)

REM Run the test
echo Running OCR tests...
echo Map: map.json
echo Images: %TEST_IMAGES_DIR%

REM Generate locale-safe timestamp
for /f "usebackq delims=" %%T in (`powershell -NoProfile -Command "Get-Date -Format 'yyyyMMdd_HHmmss'"`) do set TIMESTAMP=%%T
echo Output: test_results_%TIMESTAMP%.json
echo.

REM Run test executable (using main WFInfo executable)
..\bin\Release\net48\WFInfo.exe map.json "%TEST_IMAGES_DIR%" "test_results_%TIMESTAMP%.json"

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
echo Test completed. Check the JSON results file for detailed information.
pause
