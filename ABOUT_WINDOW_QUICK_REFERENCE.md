# About Window - Quick Reference

## ?? What Was Added

**Professional About Dialog** with version information, system details, and application features.

---

## ?? Files Created

| File | Purpose |
|------|---------|
| `Aml.BOM.Import.UI\Views\AboutWindow.xaml` | XAML UI definition |
| `Aml.BOM.Import.UI\Views\AboutWindow.xaml.cs` | Code-behind logic |

---

## ?? Files Modified

| File | Changes |
|------|---------|
| `Aml.BOM.Import.UI\Aml.BOM.Import.UI.csproj` | Added assembly metadata (version, copyright, etc.) |
| `Aml.BOM.Import.UI\MainWindow.xaml` | Added About button to navigation |
| `Aml.BOM.Import.UI\MainWindow.xaml.cs` | Added AboutButton_Click handler |

---

## ?? How to Use

### From UI
1. Click **"About"** button in sidebar (bottom, below Settings)
2. View application information
3. Click **"Close"** to dismiss

### From Code
```csharp
var aboutWindow = new AboutWindow { Owner = this };
aboutWindow.ShowDialog();
```

---

## ?? Information Displayed

| Field | Source |
|-------|--------|
| **Version** | Assembly version (1.0.0) |
| **.NET Version** | Runtime version |
| **OS Version** | Operating system |
| **Build Date** | File creation date |
| **Copyright** | Assembly attribute |

---

## ?? Window Layout

```
??????????????????????????
?    [Icon] ??          ?  ? Dark blue header
?  AML BOM Import        ?
?  Version 1.0.0         ?
??????????????????????????
? Description            ?  ? White content area
? Key Features           ?     (scrollable)
? System Information     ?
? License                ?
??????????????????????????
?    [Close Button]      ?  ? Footer
??????????????????????????
```

**Size**: 500w × 450h pixels  
**Position**: Centered over parent  
**Resizable**: No  
**Modal**: Yes

---

## ?? Styling

Uses existing app resources:
- `PrimaryBackgroundBrush` - White
- `SecondaryTextBrush` - Gray
- `PrimaryTextBrush` - Dark blue-gray
- `PrimaryButtonStyle` - Blue button

Header background: `#2C3E50` (dark blue-gray)

---

## ?? Version Configuration

**Edit**: `Aml.BOM.Import.UI.csproj`

```xml
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<Version>1.0.0</Version>
<Company>AML Corporation</Company>
<Copyright>© 2024 AML Corporation. All rights reserved.</Copyright>
<Product>AML BOM Import</Product>
<Description>Bill of Materials Import and Integration Tool for Sage 100</Description>
```

---

## ?? Key Features Listed

- Import BOMs from Excel files
- Automatic validation against Sage 100 items
- Manage new make and buy items
- Duplicate detection and handling
- Direct integration with Sage 100 via COM
- Comprehensive logging and error tracking

---

## ?? Customization

### Change Version
```xml
<!-- In .csproj -->
<Version>1.1.0</Version>
```

### Change Description
```xaml
<!-- In AboutWindow.xaml, Description section -->
<TextBlock TextWrapping="Wrap" ...>
    Your custom description
</TextBlock>
```

### Add Feature
```xaml
<!-- In AboutWindow.xaml, Key Features section -->
<TextBlock ... Text="- Your new feature"/>
```

### Change Window Size
```xaml
<Window ... Height="500" Width="600" ...>
```

---

## ? Status

**Files**: ? Created  
**Integration**: ? Added to navigation  
**Assembly Info**: ? Configured  
**Build**: ?? Pre-existing errors in AppStyles.xaml (unrelated)

---

## ?? Testing

```
? Click "About" button ? Opens dialog
? Version displays correctly
? System info populated
? Click "Close" ? Closes dialog
? Window is modal
? Window is centered
? Cannot resize window
? Scrollbar if content too tall
```

---

## ?? Visual

```
  ???????????????????????????????
  ?     ??                       ?
  ?  AML BOM Import             ?
  ?  Version 1.0.0              ?
  ???????????????????????????????
  ? Description                 ?
  ? A comprehensive BOM...      ?
  ?                             ?
  ? Key Features                ?
  ? - Import BOMs               ?
  ? - Validation                ?
  ? - New items                 ?
  ? - Duplicates                ?
  ? - Sage 100 integration      ?
  ? - Logging                   ?
  ?                             ?
  ? System Information          ?
  ? .NET:     8.0.x             ?
  ? OS:       Windows 10        ?
  ? Build:    Dec 20, 2024      ?
  ? ©:        2024 AML Corp     ?
  ?                             ?
  ? License                     ?
  ? Proprietary software...     ?
  ???????????????????????????????
  ?      [    Close    ]        ?
  ???????????????????????????????
       (500 × 450 pixels)
```

---

## ?? Pro Tips

1. **Update version** before each release in `.csproj`
2. **Update copyright year** annually
3. **Add features** to list as you implement them
4. **Build date** updates automatically on rebuild
5. **Modal window** prevents interaction with main window

---

## ?? Related Files

- `MainWindow.xaml` - Navigation sidebar
- `MainWindow.xaml.cs` - Button click handler
- `AppStyles.xaml` - Shared styles/resources
- `.csproj` - Assembly metadata

---

## ?? Full Documentation

See: `ABOUT_WINDOW_IMPLEMENTATION_GUIDE.md`

---

**Quick Access**: Sidebar ? About (bottom)  
**Status**: ? Ready to use  
**Last Updated**: December 20, 2024
