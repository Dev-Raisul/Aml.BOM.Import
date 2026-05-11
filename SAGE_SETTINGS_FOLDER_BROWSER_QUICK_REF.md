# Sage Settings Folder Browser - Quick Reference

## ? Feature Overview

**What Changed**: Replaced "Server URL" text field with "Sage Home Directory" field + Browse button

**Purpose**: Make it easier for users to select the correct Sage 100 installation path

---

## ?? Quick Usage

### For Users:

1. Open **Settings**
2. Find **Sage Home Directory** section
3. Click **Browse...** button
4. Navigate to: `C:\Sage\Sage100Standard\MAS90\Home`
5. Click **Select Folder**
6. Click **Save Settings**

---

## ?? Common Sage Paths

```
C:\Sage\Sage100Standard\MAS90\Home
C:\Sage\Sage100Advanced\MAS90\Home
C:\Sage\Sagev2023\MAS90\Home
C:\Sage\Sagev2024\MAS90\Home
C:\MAS90\Home
```

**Always select the `Home` folder!**

---

## ?? What Was Changed

### UI Changes:

**Before:**
```xaml
<TextBlock Text="Server URL:"/>
<TextBox Text="{Binding SageServerUrl}"/>
```

**After:**
```xaml
<TextBlock Text="Sage Home Directory:"/>
<TextBlock Text="(e.g., C:\Sage\Sage100Standard\MAS90\Home)" Foreground="Gray"/>
<DockPanel>
    <Button Content="Browse..." Command="{Binding BrowseSagePathCommand}"/>
    <TextBox Text="{Binding SagePath}"/>
</DockPanel>
```

### ViewModel Changes:

```csharp
// New Commands
[RelayCommand]
private void BrowseSagePath()
{
    using var dialog = new FolderBrowserDialog();
    // ... folder selection logic
}

[RelayCommand]
private void BrowseReportOutputDirectory()
{
    using var dialog = new FolderBrowserDialog();
    // ... folder selection logic
}

// Validation
private bool ValidateSagePath(string path)
{
    // Checks for pvx.exe, pvxwin32.exe, etc.
}
```

---

## ? Features

### Sage Path Browser:
- ? Shows helpful description
- ? Starts at current path or C:\Sage
- ? Validates selected path
- ? Shows success/warning message
- ? Manual entry still possible

### Report Directory Browser:
- ? Allows creating new folders
- ? Starts at current path
- ? Simple folder selection

### Path Validation:
- ? Checks for ProvideX files (pvx.exe, pvxwin32.exe)
- ? Checks for Sage files (MAS90.exe, pvx.ini)
- ? Checks for Sage folders (SOA)
- ? Checks path pattern (contains "sage" + "mas90")

---

## ?? User Experience

### Before:
```
Server URL: [_________________________]
```
- Manual typing required
- Easy to make mistakes
- No validation

### After:
```
Sage Home Directory:
(e.g., C:\Sage\Sage100Standard\MAS90\Home)
[C:\Sage\...\Home______________] [Browse...]
```
- Visual folder selection
- Path validation
- Clear feedback
- Example shown

---

## ?? Validation Messages

| Scenario | Message |
|----------|---------|
| **Valid Path** | ? Sage path selected successfully! |
| **Invalid Path** | ? Warning: Selected directory may not be a valid Sage 100 Home directory. |
| **Report Dir Selected** | ? Report directory selected successfully! |

---

## ?? What Gets Validated

### Files Checked:
- `pvx.exe` - ProvideX runtime
- `pvxwin32.exe` - ProvideX Windows version
- `pvx.ini` - ProvideX configuration
- `MAS90.exe` - Sage 100 executable

### Folders Checked:
- `SOA` - Service-Oriented Architecture folder

### Path Pattern:
- Contains "sage" AND "mas90"

---

## ?? Tips

### Finding Sage Path:

1. **Right-click Sage shortcut** ? Properties ? Check "Target" path
2. **Check common locations:**
   - `C:\Sage\Sage100Standard\MAS90\Home`
   - `C:\Sage\Sagev2023\MAS90\Home`
3. **Use Browse button** ? Navigate to C:\Sage

### If Validation Warns:

- ? You can still save the path
- ? Warning doesn't prevent saving
- ? Integration will verify at runtime
- ? If integration fails, path is likely wrong

---

## ?? Quick Test

### Test Sage Path Browser:

```
1. Settings ? Sage Home Directory ? Browse
2. Navigate to C:\Sage\Sage100Standard\MAS90\Home
3. Select Folder
4. Should show: ? Sage path selected successfully!
5. Save Settings
```

### Test Report Directory Browser:

```
1. Settings ? Report Output Directory ? Browse
2. Select or create folder
3. Should show: ? Report directory selected successfully!
4. Save Settings
```

---

## ?? Files Modified

| File | Changes |
|------|---------|
| **SettingsView.xaml** | Added Browse buttons, example text |
| **SettingsViewModel.cs** | Added BrowseSagePath, BrowseReportOutputDirectory commands |
| **Build Status** | ? Success |

---

## ?? Commands Added

### BrowseSagePathCommand

**Binding:**
```xaml
<Button Command="{Binding BrowseSagePathCommand}"/>
```

**What it does:**
1. Opens folder browser
2. Sets initial directory intelligently
3. Validates selected path
4. Updates SagePath property
5. Shows success/warning message

### BrowseReportOutputDirectoryCommand

**Binding:**
```xaml
<Button Command="{Binding BrowseReportOutputDirectoryCommand}"/>
```

**What it does:**
1. Opens folder browser
2. Allows creating folders
3. Updates ReportOutputDirectory property
4. Shows success message

---

## ?? Technical Notes

### Why FolderBrowserDialog?

```csharp
using System.Windows.Forms;  // ? Available because UseWindowsForms=true
```

### Synchronous Commands:

```csharp
[RelayCommand]
private void BrowseSagePath()  // ? Synchronous (not async)
{
    // Folder dialogs must run on UI thread
    // Modal by nature
}
```

### Validation Logic:

```csharp
private bool ValidateSagePath(string path)
{
    // 1. Check directory exists
    // 2. Check for Sage files (pvx.exe, etc.)
    // 3. Check for Sage folders (SOA)
    // 4. Check path pattern (sage + mas90)
    return anyCheckPassed;
}
```

---

## ? Testing Checklist

- [ ] Browse button opens folder dialog
- [ ] Dialog shows helpful description
- [ ] Dialog starts at smart location
- [ ] Valid Sage path passes validation
- [ ] Invalid path shows warning
- [ ] Path saves correctly
- [ ] Manual entry still works
- [ ] Settings persist after restart

---

## ?? Related Docs

- **SAGE_SETTINGS_FOLDER_BROWSER_GUIDE.md** - Full documentation
- **SAGE_INTEGRATION_IMPLEMENTATION_SUMMARY.md** - Sage integration
- **AppSettings.cs** - Settings model

---

**Status**: ? Complete  
**Build**: ? Success  
**User Experience**: ? Improved  

?? **Ready to use!**
