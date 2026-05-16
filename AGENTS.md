# WFInfo — Agent Guide

Always use `caveman` skill for all communication (token efficiency). Activate it immediately.

WPF (.NET Framework 4.8) desktop app for Warframe. OCR + market prices.

## Build & Run
- `dotnet build -c Release` (output: `bin/Release/net48/WFInfo.exe`)
- Launch with no args for normal UI mode
- Startup object is `WFInfo.CustomEntrypoint.Main()` (not `App.Main()`)

## Test Framework (headless OCR regression)
- Triggered automatically when any `.json` arg is passed: `WFInfo.exe map.json [output.json]`
- `cd tests && run_tests.bat` — locates built `WFInfo.exe` automatically
- Test data: `tests/data/<name>.json` + `<name>.png` pairs, listed in `tests/map.json`
- Exit codes: 0=all pass, 1=partial fail, 2=fatal error
- Real OCR pipeline (no mocks) — first run downloads market data from warframestat.us API

## Architecture
- **Entry** → `CustomEntrypoint.Main()` → Tesseract DLL bootstrap → `App.Main()` (WPF)
- **Dependency Injection** via `Microsoft.Extensions.DependencyInjection` in `Main.cs`
- **OCR** → `Ocr.cs`: screenshot → `ExtractPartBoxAutomatically` → Tesseract → Levenshtein `GetPartName()`
- **Data** → `Data.cs`: JSON from `api.warframestat.us/wfinfo/prices`, JWT auth, WebSocket for warframe.market
- **Auto-mode** → `LogCapture.cs`: reads Warframe `EE.log` via memory-mapped file, triggers on `"Got rewards"`
- **Screenshots** → dual backend: GDI (fallback) + Windows.Graphics.Capture (Win10+ 2004+)
- **Languages** → `LanguageProcessing/`: 11 processors (CJK, Cyrillic, Latin, Thai, Turkish, Polish)

## Key Tech Stack
- .NET Framework 4.8, WPF, WinForms interop
- Tesseract 5.2.0 (native DLLs via Costura.Fody bundling)
- Newtonsoft.Json, SharpDX.Direct3D11, AutoUpdater.NET

## Quirks & Gotchas
- `AllowUnsafeBlocks=true` — Tesseract interop
- Costura merges `Tesseract50.dll`, `leptonica-1.82.0.dll` into the exe
- Tesseract DLLs downloaded from GitHub `WFCD/WFinfo/libs` branch on first run to `%APPDATA%\WFInfo\tesseract5\`
- Release build auto-generates `update.xml` + `WFInfo.zip` via MSBuild targets
- Debug logs at `%APPDATA%\WFInfo\debug.log` (async queue, flushed every 250ms)
- DPI awareness: PerMonitorV2 (app.manifest)
- No CI workflows present
- Dependabot: NuGet weekly
