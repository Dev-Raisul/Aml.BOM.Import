# Sage Settings Folder Browser Implementation

## ? IMPLEMENTATION COMPLETE

**Feature**: Folder browser for Sage Home Directory path selection  
**Build Status**: ? **SUCCESS**  
**Date**: 2024

---

## ?? Overview

The Settings UI has been updated to replace the "Server URL" text field with a "Sage Home Directory" field that includes a folder browser button. This makes it much easier for users to select the correct Sage 100 installation path.

---

## ?? What Was Changed

### 1. Settings UI (XAML)

**File**: `Aml.BOM.Import.UI\Views\SettingsView.xaml`

#### Changes Made:

**Before:**
```xaml
<TextBlock Text="Server URL:" Margin="0,0,0,5"/>
<TextBox Text="{Binding SageServerUrl, UpdateSourceTrigger=PropertyChanged}" 
        Margin="0,0,0,10"/>
```

**After:**
```xaml
<TextBlock Text="Sage Home Directory:" Margin="0,0,0,5"/>
<TextBlock Text="(e.g., C:\Sage\Sage100Standard\MAS90\Home)" 
          FontSize="10" 
          Foreground="Gray" 
          Margin="0,0,0,5"/>
<DockPanel Margin="0,0,0,10">
    <Button Content="Browse..." 
           DockPanel.Dock="Right" 
           Margin="5,0,0,0"
           Command="{Binding BrowseSagePathCommand}"
           Style="{StaticResource SecondaryButtonStyle}"/>
    <TextBox Text="{Binding SagePath, UpdateSourceTrigger=PropertyChanged}"/>
</DockPanel>
```

**Features:**
- ? Clear label: "Sage Home Directory"
- ? Example path shown in gray text
- ? Text box for manual entry
- ? Browse button for folder selection
- ? Proper layout with DockPanel

---

### 2. Settings ViewModel

**File**: `Aml.BOM.Import.UI\ViewModels\SettingsViewModel.cs`

#### New Using Statements:
```csharp
using System.IO;
using System.Windows.Forms;
```

#### New Commands:

##### Command 1: BrowseSagePathCommand

```csharp
[RelayCommand]
private void BrowseSagePath()
{
    using var dialog = new FolderBrowserDialog
    {
        Description = "Select Sage 100 Home Directory (e.g., C:\\Sage\\Sage100Standard\\MAS90\\Home)",
        UseDescriptionForTitle = true,
        ShowNewFolderButton = false
    };

    // Set initial directory intelligently
    if (!string.IsNullOrWhiteSpace(SagePath) && Directory.Exists(SagePath))
    {
        dialog.InitialDirectory = SagePath;
    }
    else if (Directory.Exists(@"C:\Sage"))
    {
        dialog.InitialDirectory = @"C:\Sage";
    }

    if (dialog.ShowDialog() == DialogResult.OK)
    {
        SagePath = dialog.SelectedPath;
        
        // Validate the selected path
        if (!ValidateSagePath(SagePath))
        {
            StatusMessage = "? Warning: Selected directory may not be a valid Sage 100 Home directory.";
        }
        else
        {
            StatusMessage = "? Sage path selected successfully!";
        }
    }
}
```

**Features:**
- ? Shows helpful description
- ? Starts in current path or C:\Sage
- ? Validates selected path
- ? Provides user feedback

##### Command 2: BrowseReportOutputDirectoryCommand

```csharp
[RelayCommand]
private void BrowseReportOutputDirectory()
{
    using var dialog = new FolderBrowserDialog
    {
        Description = "Select Report Output Directory",
        UseDescriptionForTitle = true,
        ShowNewFolderButton = true  // Allow creating new folders
    };

    if (!string.IsNullOrWhiteSpace(ReportOutputDirectory) && Directory.Exists(ReportOutputDirectory))
    {
        dialog.InitialDirectory = ReportOutputDirectory;
    }

    if (dialog.ShowDialog() == DialogResult.OK)
    {
        ReportOutputDirectory = dialog.SelectedPath;
        StatusMessage = "? Report directory selected successfully!";
    }
}
```

**Features:**
- ? Allows creating new folders
- ? Starts in current path if set
- ? Simple folder selection

##### Helper Method: ValidateSagePath

```csharp
private bool ValidateSagePath(string path)
{
    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        return false;

    // Check for common Sage files/folders
    var sageIndicators = new[]
    {
        "pvx.exe",
        "pvxwin32.exe",
        "pvx.ini",
        "SOA",
        "MAS90.exe"
    };

    foreach (var indicator in sageIndicators)
    {
        var fullPath = Path.Combine(path, indicator);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            return true;
        }
    }

    // Check if path contains typical Sage folder names
    var pathLower = path.ToLower();
    if (pathLower.Contains("sage") && pathLower.Contains("mas90"))
    {
        return true;
    }

    return false;
}
```

**Validation Checks:**
- ? Directory exists
- ? Contains ProvideX executables (pvx.exe, pvxwin32.exe)
- ? Contains Sage configuration files (pvx.ini)
- ? Contains Sage folders (SOA)
- ? Contains Sage executables (MAS90.exe)
- ? Path contains "sage" and "mas90"

---

## ?? User Experience

### Before:
```
Server URL: [_________________________]
```
User had to:
- Manually type the full path
- Remember the exact path
- Risk typos

### After:
```
Sage Home Directory:
(e.g., C:\Sage\Sage100Standard\MAS90\Home)
[C:\Sage\Sage100Standard\MAS90\Home___] [Browse...]
```
User can now:
- Click "Browse..." button
- Navigate visually to Sage folder
- Get path validation
- See helpful example

---

## ?? Folder Browser Features

### Sage Path Browser

| Feature | Description |
|---------|-------------|
| **Description** | "Select Sage 100 Home Directory (e.g., C:\Sage\Sage100Standard\MAS90\Home)" |
| **Initial Directory** | Current SagePath or C:\Sage (if exists) |
| **Create Folder** | No (should select existing Sage installation) |
| **Validation** | Yes - checks for Sage files/folders |
| **Feedback** | ? Success or ? Warning message |

### Report Output Directory Browser

| Feature | Description |
|---------|-------------|
| **Description** | "Select Report Output Directory" |
| **Initial Directory** | Current ReportOutputDirectory |
| **Create Folder** | Yes (user can create new folders) |
| **Validation** | No (any valid folder path) |
| **Feedback** | ? Success message |

---

## ?? Validation Logic

### What ValidateSagePath Checks:

#### File Checks:
```csharp
"pvx.exe"       ? ProvideX runtime executable
"pvxwin32.exe"  ? ProvideX Windows executable
"pvx.ini"       ? ProvideX configuration
"MAS90.exe"     ? Sage 100 executable
```

#### Folder Checks:
```csharp
"SOA"           ? Service-Oriented Architecture folder
```

#### Path Pattern Checks:
```csharp
Contains "sage" AND "mas90" ? Likely a Sage path
```

### Validation Flow:

```
User selects folder
    ?
Check if directory exists
    ?
Check for Sage indicator files
    ?
Check for Sage indicator folders
    ?
Check path contains "sage" + "mas90"
    ?
If ANY check passes ? Valid ?
If ALL checks fail ? Warning ?
```

---

## ?? Usage Examples

### Example 1: First Time Setup

**User Action:**
1. Open application
2. Go to Settings
3. See empty "Sage Home Directory" field
4. Click "Browse..."

**System Response:**
1. Opens folder browser at `C:\Sage` (if exists)
2. User navigates to: `C:\Sage\Sage100Standard\MAS90\Home`
3. User clicks "Select Folder"

**Result:**
- SagePath = `C:\Sage\Sage100Standard\MAS90\Home`
- StatusMessage = "? Sage path selected successfully!"
- Path validated and confirmed

### Example 2: Changing Existing Path

**User Action:**
1. Current path: `C:\Sage\Sagev2023\MAS90\Home`
2. Click "Browse..."

**System Response:**
1. Opens folder browser at current path
2. User changes to: `C:\Sage\Sage100Standard\MAS90\Home`

**Result:**
- SagePath updated to new location
- Validation runs on new path

### Example 3: Invalid Path Selected

**User Action:**
1. Click "Browse..."
2. Select: `C:\Windows\System32`

**System Response:**
1. Path set to `C:\Windows\System32`
2. Validation fails (no Sage indicators)

**Result:**
- StatusMessage = "? Warning: Selected directory may not be a valid Sage 100 Home directory."
- Path still saved (user may know better)
- Integration will fail if path is wrong

---

## ?? Common Sage Paths

### Typical Installation Paths:

```
C:\Sage\Sage100Standard\MAS90\Home
C:\Sage\Sage100Advanced\MAS90\Home
C:\Sage\Sage100Premium\MAS90\Home
C:\Sage\Sagev2023\MAS90\Home
C:\Sage\Sagev2024\MAS90\Home
C:\MAS90\Home
```

### Path Structure:

```
C:\
??? Sage\
    ??? Sage100Standard\  (or version folder)
        ??? MAS90\
            ??? Home\     ? Select this folder!
                ??? pvx.exe
                ??? pvxwin32.exe
                ??? pvx.ini
                ??? MAS90.exe
                ??? SOA\
```

---

## ?? Technical Details

### Why FolderBrowserDialog?

**Options Considered:**

| Option | Why Not Used |
|--------|--------------|
| **OpenFileDialog** | For selecting files, not folders |
| **SaveFileDialog** | For saving files, not selecting folders |
| **WPF FolderPicker** | Not available in .NET 8 WPF |
| **Windows.Forms.FolderBrowserDialog** | ? **Used** - Available via UseWindowsForms |

### Why UseWindowsForms Works:

Because `Aml.BOM.Import.Infrastructure.csproj` has:
```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

This gives us access to:
- `System.Windows.Forms.FolderBrowserDialog`
- `System.Windows.Forms.DialogResult`

### Thread Safety:

```csharp
[RelayCommand]
private void BrowseSagePath()  // ? Synchronous, runs on UI thread
{
    using var dialog = new FolderBrowserDialog();
    dialog.ShowDialog();  // ? Modal dialog, blocks UI thread (correct)
}
```

**Why synchronous?**
- Folder dialogs must run on UI thread
- Modal by nature (blocks until user responds)
- No async needed

---

## ? Testing Checklist

### UI Tests:

- [ ] Browse button appears next to Sage Path field
- [ ] Browse button appears next to Report Output Directory field
- [ ] Example text shows correct path format
- [ ] Fields still allow manual text entry
- [ ] Browse buttons have correct styling

### Functional Tests:

#### Sage Path Browser:
- [ ] Click Browse opens folder dialog
- [ ] Dialog description shows helpful text
- [ ] Dialog starts at current path (if valid)
- [ ] Dialog starts at C:\Sage (if current path empty)
- [ ] Selecting valid Sage path shows success message
- [ ] Selecting non-Sage path shows warning message
- [ ] Cancel button closes dialog without changing path
- [ ] Selected path appears in text box

#### Report Output Directory Browser:
- [ ] Click Browse opens folder dialog
- [ ] Dialog allows creating new folders
- [ ] Dialog starts at current path (if valid)
- [ ] Selecting folder shows success message
- [ ] Cancel button closes dialog without changing path
- [ ] Selected path appears in text box

### Validation Tests:

- [ ] Valid Sage path passes validation
  - C:\Sage\Sage100Standard\MAS90\Home ?
- [ ] Invalid path shows warning
  - C:\Windows\System32 ?
- [ ] Empty path handled gracefully
- [ ] Non-existent path handled gracefully

### Integration Tests:

- [ ] Save settings with browsed Sage path
- [ ] Settings persist after restart
- [ ] Sage integration uses browsed path
- [ ] COM session initializes with browsed path

---

## ?? Related Documentation

- **SAGE_INTEGRATION_IMPLEMENTATION_SUMMARY.md** - Full Sage integration details
- **SAGE_INTEGRATION_QUICK_REFERENCE.md** - Quick usage guide
- **AppSettings.cs** - SagePath property definition
- **SageSessionService.cs** - Uses SagePath for Init()

---

## ?? User Guide

### How to Configure Sage Path:

#### Method 1: Using Browse Button (Recommended)

1. Open application
2. Click **Settings** in navigation
3. Find "Sage Home Directory" section
4. Click **Browse...** button
5. Navigate to your Sage 100 installation
6. Select the **Home** folder (usually `C:\Sage\Sage100Standard\MAS90\Home`)
7. Click **Select Folder**
8. Verify success message appears
9. Click **Save Settings**

#### Method 2: Manual Entry

1. Open application
2. Click **Settings** in navigation
3. Find "Sage Home Directory" field
4. Type the full path: `C:\Sage\Sage100Standard\MAS90\Home`
5. Click **Save Settings**

### How to Find Your Sage Path:

1. **Check Sage Shortcut:**
   - Right-click Sage 100 desktop icon
   - Select "Properties"
   - Look at "Target" path
   - Note the folder (usually ends with `\Home`)

2. **Check Default Locations:**
   - `C:\Sage\Sage100Standard\MAS90\Home`
   - `C:\Sage\Sagev2023\MAS90\Home`
   - `C:\MAS90\Home`

3. **Use Browse Button:**
   - Click Browse
   - Navigate to C:\Sage
   - Look for Sage100 folders
   - Find MAS90\Home subfolder

---

## ?? Troubleshooting

### Issue: Browse button not working

**Possible Causes:**
- UseWindowsForms not enabled
- .NET version mismatch

**Solution:**
- Verify `UseWindowsForms=true` in .csproj
- Verify targeting `net8.0-windows`

### Issue: Validation warning on valid path

**Possible Causes:**
- Sage installed in non-standard location
- Older Sage version with different files

**Solution:**
- Ignore warning if you know path is correct
- Warning doesn't prevent saving
- Integration will verify path at runtime

### Issue: Can't find Sage folder

**Possible Causes:**
- Sage not installed
- Installed in custom location
- Network installation

**Solution:**
- Check if Sage 100 is installed
- Search computer for "pvx.exe"
- Ask system administrator for path

---

## ?? Summary

### What Changed:

**Before:**
```
Server URL: [_____________________________]
  ? User had to type full path manually
```

**After:**
```
Sage Home Directory:
(e.g., C:\Sage\Sage100Standard\MAS90\Home)
[C:\Sage\...\MAS90\Home_____________] [Browse...]
  ? User can browse visually
  ? Path gets validated
  ? Clear feedback provided
```

### Benefits:

? **Easier Configuration** - Visual folder selection  
? **Less Errors** - No typos in manual entry  
? **Better UX** - Clear example and validation  
? **Smart Defaults** - Starts in C:\Sage or current path  
? **Validation** - Warns if path doesn't look like Sage  
? **Professional** - Standard Windows folder dialog  

### Files Modified:

1. ? `SettingsView.xaml` - Added Browse buttons and example text
2. ? `SettingsViewModel.cs` - Added folder browser commands
3. ? Build successful - No errors

---

**Status**: ? **Complete and Ready for Use**  
**User Experience**: ? **Significantly Improved**  
**Documentation**: ? **Complete**

?? **Sage Settings folder browser successfully implemented!** ??
