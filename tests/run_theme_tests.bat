@echo off
setlocal enabledelayedexpansion

echo WFInfo Theme Detection Test Runner
echo ====================================
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

REM Check for folder argument
if "%1"=="" (
    echo Usage: run_theme_tests.bat ^<folder_with_pngs^> [uiScale]
    echo.
    echo Runs WFInfo.exe --theme-debug on all PNGs in the folder.
    echo uiScale matches your in-game menu scale ^(e.g. 0.90 for 90%%^).
    echo Default: 1.0. Use the value your screenshots were captured at.
    echo Examples:
    echo   run_theme_tests.bat screenshots
    echo   run_theme_tests.bat screenshots 0.90
    echo.
    echo Each PNG filename must contain the expected theme name:
    echo   lunar_renewal.png  -^> LUNAR_RENEWAL
    echo   corpus_screen.png  -^> CORPUS
    echo   grineer_test.png  -^> GRINEER
    echo.
    echo Built-in themes: vitruvian, stalker, baruuk, corpus, fortuna,
    echo   grineer, lotus, nidus, orokin, tenno, high_contrast, legacy,
    echo   equinox, dark_lotus, zephyr, conquera, deadlock, lunar_renewal, pom_2
    exit /b 1
)

set "TEST_FOLDER=%~1"
set "UI_SCALE=%~2"

if not exist "%TEST_FOLDER%" (
    echo ERROR: Folder not found: %TEST_FOLDER%
    exit /b 2
)

echo Executable: %EXE%
echo Test Folder: %TEST_FOLDER%
if not "%UI_SCALE%"=="" echo UI Scale: %UI_SCALE%
echo.

REM Run theme detection tests
if not "%UI_SCALE%"=="" (
    "%EXE%" --theme-debug "%TEST_FOLDER%" "%UI_SCALE%"
) else (
    "%EXE%" --theme-debug "%TEST_FOLDER%"
)
set "EXIT_CODE=%ERRORLEVEL%"

echo.
if exist "%TEST_FOLDER%\theme_test_results.txt" (
    echo ===== RESULTS =====
    type "%TEST_FOLDER%\theme_test_results.txt"
    echo ===================
) else if exist "%CD%\theme_test_results.txt" (
    echo ===== RESULTS =====
    type "%CD%\theme_test_results.txt"
    echo ===================
)

echo.
if %EXIT_CODE% EQU 0 (
    echo All theme tests passed!
) else if %EXIT_CODE% EQU 1 (
    echo Some theme tests failed.
) else (
    echo Theme test execution encountered an error.
)

exit /b %EXIT_CODE%
