# Ready to Integrate vs Total Validated - Quick Reference

## The Two Counts

### 1. Ready to Integrate (ValidatedBomsCount)
**Purpose**: Records that can actually be integrated  
**Shows in**: Statistics panel (green)  
**Logic**: Only fully validated parents

### 2. Total Validated (TotalValidatedRecords)
**Purpose**: Overall validation progress  
**Shows in**: Validation label  
**Logic**: All records with Status='Validated'

## Visual

```
Statistics:
  Ready to Integrate: 30  ? ValidatedBomsCount

Label:
  Validation Status: 50 Validated / 100 Not Validated
                     ?
              TotalValidatedRecords
```

## Example

**Data**:
```
ASSY-001 ? PART-A (Validated) ?
ASSY-001 ? PART-B (NewBuyItem) ?
ASSY-002 ? PART-C (Validated) ?
ASSY-002 ? PART-D (Validated) ?
```

**Counts**:
```
Ready to Integrate: 2 (only ASSY-002 records)
Total Validated: 3 (PART-A, PART-C, PART-D)
Not Validated: TotalPending - 3
```

## Why Different?

**Ready to Integrate**: 
- Strict quality gate
- Only complete BOMs
- Prevents integration failures

**Total Validated**:
- Progress metric
- Shows work completed
- Motivates users

## Formula

```
Ready to Integrate ? Total Validated ? Total Pending

Not Validated = Total Pending - Total Validated
```

## Implementation

```csharp
// ViewModel
[ObservableProperty]
private int _validatedBomsCount;  // Ready to Integrate

[ObservableProperty]
private int _totalValidatedRecords;  // Total Validated

public int NotValidatedCount => TotalPendingBoms - TotalValidatedRecords;
```

```csharp
// Loading
ValidatedBomsCount = await repo.GetReadyToIntegrateRecordCountAsync();
TotalValidatedRecords = statusSummary["Validated"];
```

## XAML

**Statistics**:
```xml
<TextBlock Text="{Binding ValidatedBomsCount}"/>
```

**Label**:
```xml
<Run Text="{Binding TotalValidatedRecords}"/>
<Run Text=" / "/>
<Run Text="{Binding NotValidatedCount}"/>
```

## User Scenarios

### Progress But Not Ready
```
Ready: 10
Validated: 30 / 120 Not Validated

Message: "30 validated, but only 10 from complete BOMs"
```

### All Ready
```
Ready: 50
Validated: 50 / 0 Not Validated

Message: "All validated records ready!"
```

## Benefits

? **Progress Tracking** - See validation work completed  
? **Realistic Expectations** - Know what's truly ready  
? **Educational** - Understand completeness requirements  
? **Motivational** - Clear goals and progress  

## Summary

- **Two counts, two purposes**
- **Ready to Integrate**: Can we integrate? (strict)
- **Total Validated**: How far along? (progress)
- **Gap between them**: Work remaining

---

**Status**: ? Implemented  
**Build**: ? Successful
