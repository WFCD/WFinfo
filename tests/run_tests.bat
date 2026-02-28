@echo off
setlocal enabledelayedexpansion

echo WFInfo OCR Test Runner
echo ========================
echo.

REM Get script directory (always ends with \)
set "SCRIPT_DIR=%~dp0"

REM Locate WFInfo.exe - try Release first, then Debug
set "EXE="
if exist "%SCRIPT_DIR%..\bin\Release\net48\WFInfo.exe" (
    set "EXE=%SCRIPT_DIR%..\bin\Release\net48\WFInfo.exe"
) else if exist "%SCRIPT_DIR%..\bin\Debug\net48\WFInfo.exe" (
    set "EXE=%SCRIPT_DIR%..\bin\Debug\net48\WFInfo.exe"
) else if exist "%SCRIPT_DIR%..\WFInfo\bin\Release\net48\WFInfo.exe" (
    set "EXE=%SCRIPT_DIR%..\WFInfo\bin\Release\net48\WFInfo.exe"
) else if exist "%SCRIPT_DIR%..\WFInfo\bin\Debug\net48\WFInfo.exe" (
    set "EXE=%SCRIPT_DIR%..\WFInfo\bin\Debug\net48\WFInfo.exe"
)

if "%EXE%"=="" (
    echo ERROR: WFInfo.exe not found. Build the project first.
    echo Looked in:
    echo   %SCRIPT_DIR%..\bin\Release\net48\WFInfo.exe
    echo   %SCRIPT_DIR%..\bin\Debug\net48\WFInfo.exe
    echo   %SCRIPT_DIR%..\WFInfo\bin\Release\net48\WFInfo.exe
    echo   %SCRIPT_DIR%..\WFInfo\bin\Debug\net48\WFInfo.exe
    exit /b 2
)

REM Verify map.json exists
if not exist "%SCRIPT_DIR%map.json" (
    echo ERROR: map.json not found in %SCRIPT_DIR%
    exit /b 2
)

REM Generate timestamp for output file
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "TIMESTAMP=%%I"
set "TIMESTAMP=%TIMESTAMP:~0,8%_%TIMESTAMP:~8,6%"

REM Parse arguments
set "OUTPUT_FILE=%~1"
if "%OUTPUT_FILE%"=="" (
    set "OUTPUT_FILE=%SCRIPT_DIR%test_results_%TIMESTAMP%.json"
)

echo Executable: %EXE%
echo Test Map:   %SCRIPT_DIR%map.json
echo Output:     %OUTPUT_FILE%
echo.

REM Run tests via WFInfo.exe --test map.json output.json
"%EXE%" --test "%SCRIPT_DIR%map.json" "%OUTPUT_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if %EXIT_CODE% EQU 0 (
    echo All tests passed!
) else if %EXIT_CODE% EQU 1 (
    echo Some tests failed. Check results for details.
) else (
    echo Test execution encountered an error.
)

echo Results saved to: %OUTPUT_FILE%
exit /b %EXIT_CODE%
