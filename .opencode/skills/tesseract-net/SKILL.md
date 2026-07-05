---
name: tesseract-net
description: >-
  Build, debug, and write code for projects using the Tesseract.NET wrapper (charlesw/tesseract v5.2.0).
  Trigger whenever the user: mentions tesseract OCR in a .NET/C# context; asks about TesseractEngine, Pix, Page, ResultIterator APIs;
  encounters DllNotFoundException or native interop errors with tesseract/leptonica; needs to build specific TFMs (netstandard2.0/net47/net48);
  writes OCR pipelines in C#; or works with P/Invoke/InteropDotNet patterns in a tesseract wrapper project.
  Also trigger when the user needs to troubleshoot tessdata paths, engine initialization, cross-platform DLL resolution,
  or packaging native binaries with a .NET library. Use this skill even if the user doesn't explicitly name "tesseract.NET" —
  any .NET OCR task with tesseract references or native interop loading tesseract50/leptonica-1.82.0 should trigger.
---

# Tesseract.NET Wrapper Skill

Guide for building, debugging, and developing with the Tesseract.NET wrapper (v5.2.0) wrapping tesseract C++ 5.2.0.

Tessdoc is available locally at `tessdoc/` — see `tessdoc/ImproveQuality.md`, `tessdoc/FAQ.md`, `tessdoc/Common-Errors-and-Resolutions.md`, `tessdoc/APIExample.md`, `tessdoc/Data-Files.md`, `tessdoc/Command-Line-Usage.md`, and `tessdoc/InputFormats.md` for detailed upstream documentation.

## Project Structure

```
repo-root/
├── src/
│   ├── Tesseract/                 # Main library (netstandard2.0; net47; net48)
│   │   ├── Interop/               # P/Invoke signatures (BaseApi.cs, LeptonicaApi.cs)
│   │   ├── Internal/InteropDotNet/ # Runtime DLL loading
│   │   ├── TesseractEngine.cs     # Main OCR engine
│   │   ├── Pix.cs                 # Leptonica image wrapper
│   │   ├── Page.cs                # OCR result page
│   │   ├── ResultIterator.cs      # Recognition results iterator
│   │   ├── x64/                   # x64 native DLLs
│   │   └── x86/                   # x86 native DLLs
│   ├── Tesseract.Drawing/         # System.Drawing interop (netstandard2.0)
│   ├── Tesseract.Tests/           # Test fixtures + test data
│   ├── Tesseract.NetCore31Tests/  # .NET Core 3.1 test project
│   ├── Tesseract.Net48Tests/      # .NET Framework 4.8 test project
│   └── Tesseract.sln
├── tesseract/                     # C++ tesseract 5.2.0 submodule
├── tessdoc/                       # Official tesseract documentation
└── docs/
```

## Building

```bash
# Build specific TFM
dotnet build src/Tesseract/Tesseract.csproj -f netstandard2.0
dotnet build src/Tesseract/Tesseract.csproj -f net47
dotnet build src/Tesseract/Tesseract.csproj -f net48

# Build all
dotnet build src/Tesseract.sln

# Build with config
dotnet build src/Tesseract/Tesseract.csproj -f netstandard2.0 -c Release
dotnet build src/Tesseract/Tesseract.csproj -f netstandard2.0 -c Debug

# Build test projects
dotnet build src/Tesseract.NetCore31Tests/Tesseract.NetCore31Tests.csproj
dotnet build src/Tesseract.Net48Tests/Tesseract.Net48Tests.csproj

# Pack NuGet
dotnet pack src/Tesseract/Tesseract.csproj -f netstandard2.0 -c Release
```

## Cross-Platform

### Linux
```bash
sudo apt-get install libleptonica-dev tesseract-ocr tesseract-ocr-eng
```
Post-build symlinks in test csproj create `$(OutDir)x64/libleptonica-1.82.0.so` and `libtesseract50.so`.

### Windows
Native DLLs bundled in `src/Tesseract/x64/` and `x86/`. Requires VS 2019 x86/x64 runtimes. `Tesseract.targets` handles copy-to-output.

### macOS
Post-build symlinks for `.dylib`. Requires `brew install tesseract leptonica`.

## API Usage Patterns

### Basic OCR (text from file)
```csharp
using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
using var img = Pix.LoadFromFile(testImagePath);
using var page = engine.Process(img);
string text = page.GetText();
float confidence = page.GetMeanConfidence();
```

### OCR from bytes
```csharp
byte[] fileBytes = File.ReadAllBytes(filename);
using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
using var img = Pix.LoadFromMemory(fileBytes);
using var page = engine.Process(img);
```

### OCR with restricted sub-rectangle
```csharp
// After SetImage in native API, call SetRectangle before Recognize.
// In .NET wrapper, use engine.Process(img, pageSegMode, rect)
var rect = new Rect(x: 30, y: 86, width: 590, height: 100);
using var page = engine.Process(img, rect);
```

### Searchable PDF
```csharp
using var renderer = PdfResultRenderer.CreatePdfRenderer("output.pdf", "./tessdata", false);
using (renderer.BeginDocument("title"))
{
    using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.TesseractAndLstm);
    using var img = Pix.LoadFromFile("input.jpg");
    using var page = engine.Process(img, "page-title");
    renderer.AddPage(page);
}
```

### ResultIterator (word-level)
```csharp
using var page = engine.Process(img);
using var iter = page.GetIterator();
iter.Begin();
do {
    string word = iter.GetText(PageIteratorLevel.Word);
    float conf = iter.GetConfidence(PageIteratorLevel.Word);
    iter.TryGetBoundingBox(PageIteratorLevel.Word, out Rect rect);
    var attrs = iter.GetWordFontAttributes();
} while (iter.Next(PageIteratorLevel.Word));
```

### ChoiceIterator (alternative symbol hypotheses)
```csharp
using var iter = page.GetIterator();
iter.Begin();
do {
    if (iter.GetText(PageIteratorLevel.Symbol) != null) {
        using var choices = iter.GetChoiceIterator();
        do {
            string alt = choices.GetText();
            float conf = choices.GetConfidence();
        } while (choices.Next());
    }
} while (iter.Next(PageIteratorLevel.Symbol));
```

### Pix image preprocessing
```csharp
// Binarization
var binary = img.BinarizeOtsuAdaptiveThreshold(sx: 0, sy: 0, smoothx: 0, smoothy: 0, scorefract: 0.1f);
var sauvola = img.BinarizeSauvola(whsize: 51, factor: 0.35f, addborder: false);

// Deskew
var deskewed = img.Deskew();
var deskewed2 = img.Deskew(new Scew { Angle = -2.5, Confidence = 90 });

// Remove lines (morphological pipeline)
var cleaned = img.RemoveLines();

// Despeckle
var cleaned2 = img.Despeckle(selStr: "o2", selSize: 2);

// Rotate
var rotated = img.Rotate(radians, RotationMethod.AreaMap, RotationFill.White, null, null);
var rotated90 = img.Rotate90(RotationFill.White);

// Convert RGB to grayscale
var gray = img.ConvertRGBToGray();

// Scale
var scaled = img.Scale(2.0f, 2.0f);

// Invert
var inverted = img.Invert();
```

### SetVariable configuration
```csharp
// Character whitelist (digits only)
engine.SetVariable("tessedit_char_whitelist", "0123456789");

// Page segmentation mode
engine.SetVariable("tessedit_pageseg_mode", "6"); // PSM_SINGLE_BLOCK

// Disable dictionary (for non-dictionary text like codes)
engine.SetVariable("load_system_dawg", "0");
engine.SetVariable("load_freq_dawg", "0");

// Debug output file
engine.SetDebugVariable("debug_file", "tesseract.log");

// Disable adaptive classifier (for consistent multi-image results)
engine.SetVariable("classify_enable_learning", "0");

// LSTM alternative symbol choices (requires LSTMs)
engine.SetVariable("lstm_choice_mode", "2");

// Disable image inversion (speed gain)
engine.SetVariable("tessedit_do_invert", "0");

// Thread control (OMP_THREAD_LIMIT env var, not a SetVariable)
```

### Page layout analysis (OSD)
```csharp
using var layout = page.AnalyseLayout();
layout.Begin();
do {
    PolyBlockType type = layout.BlockType;
    if (layout.TryGetBoundingBox(PageIteratorLevel.Block, out Rect bb))
        Console.WriteLine($"Block {type} at ({bb.X},{bb.Y}) {bb.Width}x{bb.Height}");
    var props = layout.GetProperties();
    // Orientation, WritingDirection, TextLineOrder, DeskewAngle
} while (layout.Next(PageIteratorLevel.Block));
```

### Multi-page TIFF
```csharp
Pix img = null;
int offset = 0;
while ((img = Pix.pixReadFromMultipageTiff(path, ref offset)) != null) {
    using (img) {
        using var page = engine.Process(img);
        string text = page.GetText();
    }
}
```

### Multiple languages
```csharp
// "+" separated language codes
using var engine = new TesseractEngine("./tessdata", "eng+deu", EngineMode.Default);
```

## Image Quality & OCR Accuracy

Based on `tessdoc/ImproveQuality.md`. See that file for full details.

### Critical factors
- **DPI ≥ 300** — rescale if needed. Optimal capital letter height suggested by Willus Dotkom.
- **Dark text on light background** — tesseract 4+ handles dark-on-light only. Invert if needed.
- **Binarization** — tesseract uses Otsu internally. Tesseract 5 adds Adaptive Otsu and Sauvola via `thresholding_*` config params. Can also preprocess with Leptonica in code (`BinarizeOtsuAdaptiveThreshold`, `BinarizeSauvola`).
- **Noise** — use `Despeckle()` or preprocess with ImageMagick/OpenCV.
- **Rotation/deskew** — even slight skew degrades line segmentation. Use `Deskew()` or preprocess.
- **Borders** — too little border → segmentation issues. Too much border → "empty page". Add ~10px white border if tightly cropped.
- **Alpha channel** — tesseract 4+ blends with white background. For problematic cases (subtitles), remove alpha beforehand.

### Page segmentation modes
PSM directly corresponds to `PageSegMode` enum in wrapper:
```
0  OSD only               → PageSegMode.OsdOnly
1  Auto + OSD             → PageSegMode.AutoOsd
2  Auto only, no OCR      → PageSegMode.AutoOnly
3  Fully auto (default)    → PageSegMode.Auto
4  Single column           → PageSegMode.SingleColumn
5  Single block vertical   → PageSegMode.SingleBlockVertText
6  Single block            → PageSegMode.SingleBlock
7  Single line             → PageSegMode.SingleLine
8  Single word             → PageSegMode.SingleWord
9  Circle word             → PageSegMode.CircleWord
10 Single char             → PageSegMode.SingleChar
11 Sparse text             → PageSegMode.SparseText
12 Sparse text + OSD       → PageSegMode.SparseTextOsd
13 Raw line                → PageSegMode.RawLine
```

### Dictionary & character control
- Disable dictionaries for non-dictionary text: `load_system_dawg=0`, `load_freq_dawg=0`
- Character whitelist: `tessedit_char_whitelist`
- User words: place `eng.user-words` in tessdata, set `user_words_suffix=user-words`
- User patterns: place `eng.user-patterns` in tessdata (see `tessdoc/APIExample-user_patterns.md`)
- Language model penalties: `language_model_penalty_non_dict_word` (default 0.15), `language_model_penalty_non_freq_dict_word` (default 0.1)

## Traineddata Files

Three variants available. The `tessdata` repo files contain both legacy + LSTM models. `tessdata_fast`/`tessdata_best` are LSTM-only.

| Repo | Speed | Accuracy | Legacy OEM | Retrainable |
|------|-------|----------|------------|-------------|
| tessdata | Fast+ | Good | Yes (OEM 0) | No |
| tessdata_fast | Fastest | Fair | No | No |
| tessdata_best | Slowest | Best | No | Yes |

- `eng.traineddata` → English
- `osd.traineddata` → orientation/script detection (always needed for OSD)
- `equ.traineddata` → math/equation detection
- Multi-language: `eng+deu`, `hin+eng`, or script-level `script/Devanagari`

### Supported input formats (via Leptonica)
PNG, JPEG, TIFF, JPEG 2000, GIF, WebP, BMP, PNM.
Unsupported: PDF (use OCRmyPDF to convert first), HEIC, AVIF, animated WebP/GIF.

## Enums Quick Reference

### EngineMode
```
TesseractOnly = 0    Legacy engine
LstmOnly = 1         LSTM neural net
TesseractAndLstm = 2 Both engines
Default = 3          LSTM-based (current default)
```

### PageSegMode
```
OsdOnly=0, AutoOsd=1, AutoOnly=2, Auto=3, SingleColumn=4,
SingleBlockVertText=5, SingleBlock=6, SingleLine=7, SingleWord=8,
CircleWord=9, SingleChar=10, SparseText=11, SparseTextOsd=12,
RawLine=13, Count=14
```

### PageIteratorLevel
```
Block=0, Para=1, TextLine=2, Word=3, Symbol=4
```

## Debugging

### DllNotFoundException
LibraryLoader searches platform subdirs (`x64/` or `x86/`) in order:
1. `TesseractEnviornment.CustomSearchPath`
2. Executing assembly directory
3. AppDomain base directory
4. `<AppDomain>/bin/`
5. Current working directory

**Fix:** Verify correct bitness DLLs in output. Check test projects link native DLLs with `CopyToOutputDirectory=PreserveNewest`.

### Engine init fails
- Missing `eng.traineddata` in datapath directory
- Process bitness mismatch with native DLLs
- `EngineMode.Default` needs LSTM-compatible traineddata; use `EngineMode.TesseractOnly` for legacy `tessdata` repo files
- Invalid config variable names in `SetVariable` or `initialOptions` — removes those from the `--print-parameters` list or revert them

### Locale assertion error
```
!strcmp(locale, "C"):Error:Assert failed:in file baseapi.cpp, line 192
```
The native library requires `"C"` locale. Restore locale after calling Tesseract API if your app changes it.

### "Cannot load leptonica DLL before tesseract"
Leptonica loads first — if it fails, tesseract never loads. Check leptonica DLL integrity and path.

### Dispose ordering
- `ResultIterator`/`PageIterator` invalidated when owning `Page` is disposed
- Only one `Page` active per engine at a time (enforced by `processCount`)
- Use separate `TesseractEngine` instances for parallel processing

### Inconsistent OCR on multiple images
Adaptive classifier accumulates data across calls. Either:
- Set `classify_enable_learning` to `0`
- Or use separate engine instances per image

### Pix.LoadFromFile returns null
Leptonica's `pixRead` fails silently for unsupported formats. Check format support list above.

### Speed optimization
- Use `tessdata_fast` models (integer, smaller network)
- Set `OMP_THREAD_LIMIT=1` env var to disable multithreading (helps on 2-core systems)
- Set `tessedit_do_invert=0` to skip auto-inversion check
- Use `EngineMode.LstmOnly` (no legacy engine overhead)

## Native Interop

### DLL name resolution
| Platform | tesseract | leptonica |
|----------|-----------|-----------|
| Windows  | tesseract50.dll | leptonica-1.82.0.dll |
| Linux    | libtesseract50.so | libleptonica-1.82.0.so |
| macOS    | libtesseract50.dylib | libleptonica-1.82.0.dylib |

LibraryLoader appends prefixes/suffixes automatically. Use base name in `[RuntimeDllImport]`.

### How interop works
1. `InteropRuntimeImplementer.CreateInstance<T>()` generates dynamic class via `System.Reflection.Emit`
2. Constructor calls `LibraryLoader.Instance.LoadLibrary()` → `dlopen`/`LoadLibrary`
3. Each method calls `GetProcAddress()` → `Marshal.GetDelegateForFunctionPointer()`
4. Avoids `[DllImport]` for cross-platform compatibility (Mono/.NET)

### Adding new native bindings
1. Add signature to `ITessApiSignatures` in `Interop/BaseApi.cs`:
   `[RuntimeDllImport(Constants.TesseractDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "TessFuncName")]`
2. Add managed wrapper to `TessApi` static class (handles UTF-8 string marshaling, IntPtr management, memory deallocation via `DeleteText`)

## Output formats available
The wrapper exposes these via `page.Get*Text()` methods and `ResultRenderer`:
- `GetText()` → plain UTF-8 text
- `GetHOCRText(pageNum)` → XHTML (hOCR spec)
- `GetAltoText(pageNum)` → ALTO XML
- `GetTsvText(pageNum)` → TSV (tab-separated values)
- `GetBoxText(pageNum)` → Bounding box data
- `GetLSTMBoxText(pageNum)` → LSTM-level boxes
- `GetWordStrBoxText(pageNum)` → Word string boxes
- `GetUNLVText()` → UNLV format
- `PdfResultRenderer` → searchable PDF
