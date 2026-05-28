# Terminology Update: "COM" ? "Sage Business Logic"

## Overview

Updated user-facing terminology in popup messages to use "Sage Business Logic" instead of "COM integration" for better user understanding.

---

## What Was Changed

### File: `Aml.BOM.Import.UI\ViewModels\NewBomsViewModel.cs`

#### Before:
```csharp
var result = System.Windows.MessageBox.Show(
    $"Ready to integrate {validatedCount} validated BOM(s) into Sage 100.\n\n" +
    $"This will create Bill of Materials in Sage using COM integration.\n\n" +
    $"Continue?",
    "Integrate BOMs to Sage",
    System.Windows.MessageBoxButton.YesNo,
    System.Windows.MessageBoxImage.Question);
```

#### After:
```csharp
var result = System.Windows.MessageBox.Show(
    $"Ready to integrate {validatedCount} validated BOM(s) into Sage 100.\n\n" +
    $"This will create Bill of Materials in Sage using Sage Business Logic.\n\n" +
    $"Continue?",
    "Integrate BOMs to Sage",
    System.Windows.MessageBoxButton.YesNo,
    System.Windows.MessageBoxImage.Question);
```

---

## Rationale

**Why This Change?**

1. **User-Friendly**: "Sage Business Logic" is more meaningful to end users than technical "COM integration"
2. **Professional**: Aligns with Sage terminology
3. **Clear**: Indicates integration with Sage business rules and validation
4. **Consistent**: Better fits the application's business domain language

**Technical Note**: Internally, the code still uses COM interop (ProvideX.Script, Business Objects), but this is an implementation detail users don't need to know.

---

## Impact

**User-Facing Changes**:
- ? BOM Integration confirmation dialog
- ? More professional terminology
- ? Better user understanding

**Code-Level Changes**:
- ? Single line change in NewBomsViewModel.cs
- ? No functional changes
- ? Build successful

---

## Similar Terminology in Codebase

**Internal/Developer-Facing** (Keep as "COM"):
- Code comments: "Manages Sage 100 ProvideX session using COM interop"
- Log messages: "Creating ProvideX.Script COM object"
- Documentation files: Technical implementation details

**User-Facing** (Use "Sage Business Logic"):
- MessageBox dialogs
- Status messages visible to users
- User documentation

---

## Build Status

? **Build Successful** - No compilation errors  
? **Single File Changed** - Minimal impact  
? **No Breaking Changes** - Existing functionality preserved  

---

## Testing

**Manual Test**:
1. Open application
2. Navigate to New BOMs View
3. Click "Integrate BOMs" button
4. Verify dialog message reads: "...using Sage Business Logic..."

**Expected Result**: Dialog shows updated terminology

---

## Related Files

**Not Changed** (Internal/Technical):
- `SageSessionService.cs` - Comments remain technical
- `BomIntegrationService.cs` - Implementation unchanged
- Documentation markdown files - Technical details preserved

**Changed**:
- `NewBomsViewModel.cs` - User-facing message updated

---

## Summary

This minor terminology update improves the user experience by using business-appropriate language ("Sage Business Logic") instead of technical jargon ("COM integration") in user-facing dialogs, while maintaining accurate technical terminology in code and developer documentation.

---

**Status**: ? Complete  
**Build**: ? Successful  
**Impact**: Minimal (1 line)  
**Benefit**: Improved UX
