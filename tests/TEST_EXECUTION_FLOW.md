# Test Execution Flow - How WFInfo.exe Redirects to Tests

## 🎯 Command-Line Detection Logic

The test framework is integrated into the main WFInfo executable through **command-line argument detection** in `CustomEntrypoint.cs`.

## 🔄 Execution Flow

### **Normal UI Mode (Default):**
```bash
WFInfo.exe
# → Launches normal WFInfo UI application
# → App.Main() is called
```

### **Test Execution Mode (When arguments detected):**
```bash
WFInfo.exe map.json data/ results.json
# → Detects test arguments
# → Redirects to TestProgram.RunTests()
# → Runs OCR test framework
```

## 📋 Detection Logic

**Location:** `CustomEntrypoint.cs` lines 86-107

```csharp
// Check for test execution arguments
string[] args = Environment.GetCommandLineArgs();
if (args.Length >= 4 && (args[1].EndsWith(".json") || args[1].Contains("map")))
{
    // Test execution mode detected!
    Console.WriteLine("WFInfo OCR Test Runner");
    Console.WriteLine("=======================");
    
    // Redirect to test framework
    TestProgram.RunTests(args).Wait();
    return; // Skip UI launch
}

// Normal UI mode continues...
App.Main(); // Launch WFInfo UI
```

## 🎯 Argument Pattern Matching

**Test Mode Detection:**
- **Minimum args:** 4+ arguments
- **Key indicator:** Second argument ends with `.json` OR contains `"map"`
- **Example patterns:**
  - `WFInfo.exe map.json data/ results.json` ✅
  - `WFInfo.exe tests/map.json images/ output.json` ✅
  - `WFInfo.exe config.json` ❌ (not enough args)
  - `WFInfo.exe --help` ❌ (doesn't match pattern)

## 🚀 Test Program Integration

**TestProgram.cs** provides two entry points:

```csharp
public static void Main(string[] args)
{
    RunTests(args).Wait(); // Direct call
}

public static async Task RunTests(string[] args)
{
    // Test execution logic
    // External data loading
    // OCR processing
    // Coverage metrics
    // JSON reporting
}
```

## 📁 Complete Execution Chain

```
1. User runs: WFInfo.exe map.json data/ results.json
2. CustomEntrypoint.Main() detects test arguments
3. Redirects to TestProgram.RunTests(args)
4. TestProgram loads external test data
5. OCR processing with comprehensive metrics
6. Results saved to JSON file
7. Console output with coverage analysis
8. Exit with appropriate code (0=success, 1=partial failure, etc.)
```

## 🔧 Build & Run Instructions

### **Build:**
```bash
cd <path_to_code_folder>\WFinfo
dotnet build --configuration Release
# Executable: bin\Release\net48\WFInfo.exe
```

### **Run Tests:**
```bash
cd <path_to_code_folder>\WFinfo\tests
..\bin\Release\net48\WFInfo.exe map.json data/ results.json
```

### **Run UI:**
```bash
cd <path_to_code_folder>\WFinfo
bin\Release\net48\WFInfo.exe
# (no arguments = normal UI mode)
```

## 🎯 Key Benefits

### **Single Executable:**
- **No separate test binary needed**
- **Same executable** for UI and testing
- **Simplified deployment** and distribution

### **Smart Detection:**
- **Automatic mode selection** based on arguments
- **No configuration files** needed for mode switching
- **Backward compatible** with existing workflows

### **Integrated Testing:**
- **Full access** to WFInfo internals
- **Same OCR engines** as production
- **Identical behavior** to real application

### **CI/CD Ready:**
- **Command-line interface** perfect for automation
- **JSON output** for result processing
- **Exit codes** for build status integration

## 📊 Test Framework Features

When running in test mode, WFInfo.exe provides:

- **External Data Loading:** `{scenario}.json` + `{scenario}.png` pairs
- **Multi-Language Support:** All 15 supported languages
- **Coverage Metrics:** Pass rates, accuracy, processing times
- **Theme Testing:** All WFInfo themes supported
- **HDR Support:** Test with/without HDR
- **Filter Testing:** Accessibility filter validation
- **Comprehensive Reporting:** JSON output with detailed metrics

## 🚀 Result

The test framework is **fully integrated** into WFInfo.exe with **smart command-line detection** - providing a **unified solution** for both UI application and automated testing! 🎯
