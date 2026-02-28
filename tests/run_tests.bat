@echo off
setlocal enabledelayedexpansion

echo WFInfo OCR Test Runner
echo ========================
echo.

REM Get script directory for absolute path resolution
set "SCRIPT_DIR=%~dp0"

REM Check if map.json exists in script directory
if not exist "%SCRIPT_DIR%map.json" (
    echo ERROR: map.json not found in script directory: %SCRIPT_DIR%
    echo.
    echo Usage: run_tests.bat [test_data_directory]
    echo.
    echo Example: run_tests.bat data\
    exit /b 2
)

REM Set test images directory
set "TEST_IMAGES_DIR=%~1"
if "%TEST_IMAGES_DIR%"=="" set "TEST_IMAGES_DIR=%SCRIPT_DIR%data"

REM Check if TEST_IMAGES_DIR is relative and prefix with script directory
echo "%TEST_IMAGES_DIR%" | findstr /r "^\".:\\.*" >nul
if %errorlevel% neq 0 (
    REM Relative path detected, prefix with script directory
    set "TEST_IMAGES_DIR=%SCRIPT_DIR%%TEST_IMAGES_DIR%"
)

REM Check if test images directory exists
if not exist "%TEST_IMAGES_DIR%" (
    echo ERROR: Test images directory not found: "%TEST_IMAGES_DIR%"
    exit /b 3
)

REM Run the test
echo Running OCR tests...
echo Map: map.json
echo Images: %TEST_IMAGES_DIR%

REM Generate locale-safe timestamp with fallback
set "TIMESTAMP="
for /f "usebackq delims=" %%T in (`powershell -NoProfile -Command "Get-Date -Format 'yyyyMMdd_HHmmss'" 2^>nul`) do set TIMESTAMP=%%T

REM Check if PowerShell command failed and provide fallback
if "%TIMESTAMP%"=="" (
    REM Fallback using DATE and TIME environment variables
    set "TIMESTAMP=%DATE:~-4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
    REM Remove spaces that might be in TIME
    set "TIMESTAMP=%TIMESTAMP: =0%"
)
echo Output: test_results_%TIMESTAMP%.json
echo.

REM Run test executable (using main WFInfo executable)
"%SCRIPT_DIR%..\bin\Release\net48\WFInfo.exe" "%SCRIPT_DIR%map.json" "%TEST_IMAGES_DIR%" "test_results_%TIMESTAMP%.json"

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
REM Only pause in interactive environments (not CI)
if "%CI%"=="" if "%GITHUB_ACTIONS%"=="" pause
