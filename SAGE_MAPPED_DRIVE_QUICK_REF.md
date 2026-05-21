# Sage Mapped Drive - Quick Reference

## ? Implementation Complete

**Feature**: Enforce mapped drive for Sage Home directory  
**Build**: ? Success

---

## ?? The Requirement

**Sage integration REQUIRES a mapped drive letter.**

### ? Supported Paths

- `C:\Sage\MAS90\Home` (local drive)
- `Z:\Sage\MAS90\Home` (mapped network drive)

### ? NOT Supported Paths

- `\\server\share\Sage\MAS90\Home` (UNC/network path)
- `//server/share/Sage/MAS90/Home` (UNC with forward slashes)

---

## ?? Why Network Paths Fail

**COM Integration Issues:**
- ProvideX.Script cannot access `\\server\share` paths
- Authentication problems
- Session context failures

**Solution:** Map to drive letter (e.g., `Z:`)

---

## ?? User Experience

### Scenario 1: User Selects Network Path

```
User browses to: \\server\share\Sage
        ?
? WARNING DIALOG
        ?
Options:
  [YES] Show mapping instructions
  [NO] Continue anyway (with warning)
  [CANCEL] Try again
```

### Scenario 2: User Selects Mapped Drive

```
User browses to: Z:\Sage\MAS90\Home
        ?
? ACCEPTED
Status: "Sage path selected (Mapped Drive)!"
```

---

## ?? How to Map a Network Drive

**Quick Steps:**

```
1. Windows + E (File Explorer)
2. This PC ? Map network drive
3. Choose letter: Z:
4. Folder: \\server\share\Sage
5. ? Reconnect at sign-in
6. Click Finish
```

**Command Line:**

```cmd
net use Z: \\server\share\Sage /persistent:yes
```

---

## ?? Path Detection

| Path | Type | Accepted | Action |
|------|------|----------|--------|
| `C:\Sage` | Local | ? | Accept |
| `Z:\Sage` | Mapped | ? | Accept |
| `\\server\share` | UNC | ?? | Warn |

---

## ?? Save Validation

**If UNC path detected when saving:**

```
? WARNING: Network Path Detected

This may cause integration to FAIL!
Save anyway?

[Yes] [No]
```

- **Yes**: Saves with warning
- **No**: Cancels save

---

## ?? Status Messages

### ? Success

- `? Sage path selected successfully!`
- `? Sage path selected (Mapped Drive)!`
- `Settings saved successfully!`

### ?? Warnings

- `? WARNING: Using network path - Integration may fail!`
- `? Settings saved - WARNING: Network path may cause failures!`
- `? Please map the network drive and try again.`

---

## ?? Quick Test

**Test UNC Detection:**
1. Click Browse
2. Navigate to `\\server\share\Sage`
3. Should see warning dialog

**Test Mapped Drive:**
1. Map network to `Z:`
2. Browse to `Z:\Sage`
3. Should accept without warning

---

## ?? Detection Methods

```csharp
// Detects \\server\share
IsUncPath(path)

// Detects Z:\ (network drive)
IsMappedDrive(path)
```

---

## ?? Files Changed

- `SettingsViewModel.cs`
  - Added `IsUncPath()` method
  - Added `IsMappedDrive()` method
  - Updated `BrowseSagePath()` command
  - Updated `SaveSettings()` command

---

## ? Benefits

? Prevents integration failures  
? Clear user guidance  
? Multiple warning points  
? Step-by-step instructions  
? Proper error logging

---

**Status**: ? Complete  
**Protection**: ? Enforced  
**User Experience**: ? Improved
