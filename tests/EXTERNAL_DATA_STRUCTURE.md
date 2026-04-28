# External Test Data Structure

## 🎯 New Architecture

The test framework now uses **external data files** instead of embedded test scenarios, providing better organization and flexibility.

## 📁 File Structure

```
tests/
├── map.json              # Main test map (scenario references)
├── data/                  # Test data directory
│   ├── test1.json       # Test scenario 1 data
│   ├── test1.png        # Test scenario 1 image
│   ├── test2.json       # Test scenario 2 data
│   ├── test2.png        # Test scenario 2 image
│   ├── test3.json       # Test scenario 3 data
│   └── test3.png        # Test scenario 3 image
├── run_tests.bat          # Batch script
└── results/              # Generated test results
```

## 📋 map.json Structure

```json
{
  "scenarios": [
    "data/test1",
    "data/test2", 
    "data/test3"
  ]
}
```

**Benefits:**
- **Clean**: Main map only contains scenario references
- **Flexible**: Easy to add/remove tests
- **Organized**: Test data separated from configuration
- **Scalable**: Works with any number of test scenarios

## 📄 Individual Test Data Files

### **data/test1.json**
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

### **data/test2.json**
```json
{
  "description": "Korean fissure reward screen",
  "resolution": "1920x1080",
  "scaling": 125,
  "theme": "lotus",
  "language": "korean",
  "parts": {
    "0": "보 프라임 설계도"
  },
  "category": "reward",
  "hdr": false,
  "filters": []
}
```

### **data/test3.json**
```json
{
  "description": "Japanese inventory screen",
  "resolution": "2560x1440",
  "scaling": 150,
  "theme": "tenno",
  "language": "japanese",
  "parts": {
    "0": "Volt Prime 設計図",
    "1": "Saryn Prime 設計図"
  },
  "category": "inventory",
  "hdr": true,
  "filters": ["colorblind"]
}
```

## 🔄 Test Execution Flow

1. **Load map.json** → Get scenario paths
2. **For each scenario:**
   - Load `{scenario}.json` → Test configuration
   - Load `{scenario}.png` → Test image
   - Execute OCR with test settings
   - Compare results with expected parts
3. **Generate comprehensive report** → JSON with coverage metrics

## 🎯 Benefits

### **Organization**
- **Separation of Concerns**: Test data separate from test logic
- **Modularity**: Each test is self-contained
- **Maintainability**: Easy to update individual tests
- **Scalability**: Add tests without touching core framework

### **Flexibility**
- **Dynamic Loading**: Tests loaded at runtime from file system
- **Easy Updates**: Modify test data without code changes
- **Version Control**: Track changes to individual test scenarios
- **CI/CD Ready**: External data works well with pipelines

### **Coverage Analysis**
- **Path-based Classification**: Extract language/category from file paths
- **Comprehensive Metrics**: Pass rates, accuracy, processing times
- **Performance Tracking**: Per-language and per-category analysis

## 🚀 Usage

### **Adding New Tests:**
```bash
# 1. Create new test files
echo '{"description": "...", "language": "...", "parts": {...}}' > data/test4.json
# Add corresponding screenshot
cp screenshot.png data/test4.png

# 2. Update map.json
echo '["data/test1", "data/test2", "data/test3", "data/test4"]' > map.json
```

### **Running Tests:**
```bash
# Run all tests
WFInfo.Tests.exe map.json data/ results.json

# Run specific test
WFInfo.Tests.exe map.json data/ results.json --filter "data/test1"
```

This external data structure provides **maximum flexibility** while maintaining **clean organization** and **comprehensive coverage metrics**! 🚀
