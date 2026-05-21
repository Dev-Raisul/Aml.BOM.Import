' ============================================================
' Sage 100 - Create BOM Script (BM_Bill_bus)
' Template style: matches your popup/log/step structure + credentials
' ============================================================

Dim oPVX, oSS, oBOM
Dim retVal, stepLog

stepLog = "===== Sage 100 BOM Creation Log =====" & vbCrLf & vbCrLf

' ------------------------------------------------------------
' STEP 1: Initialize ProvideX
' ------------------------------------------------------------
On Error Resume Next
Set oPVX = CreateObject("ProvideX.Script.1")
If Err.Number <> 0 Then
    MsgBox "STEP 1 FAILED: Could not create ProvideX.Script." & vbCrLf & _
           "Error: " & Err.Description, vbCritical, "Step 1 - Init Error"
    WScript.Quit
End If
On Error GoTo 0

oPVX.Init("C:\Sage\Sagev2023\MAS90\Home")
stepLog = stepLog & "[STEP 1] ProvideX Initialized: OK" & vbCrLf
MsgBox stepLog, vbInformation, "Step 1: ProvideX Init"

' ------------------------------------------------------------
' STEP 2: Create Session Object
' ------------------------------------------------------------
Set oSS = oPVX.NewObject("SY_Session")
If oSS Is Nothing Then
    MsgBox "STEP 2 FAILED: Could not create SY_Session.", vbCritical, "Step 2 - Session Error"
    WScript.Quit
End If
stepLog = stepLog & "[STEP 2] SY_Session Created: OK" & vbCrLf
MsgBox stepLog, vbInformation, "Step 2: Session Created"

' ------------------------------------------------------------
' STEP 3: Set User (use your credentials)
' ------------------------------------------------------------
retVal = oSS.nSetUser("Raisul", "")
If retVal = 0 Then
    MsgBox "STEP 3 FAILED: nSetUser" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 3 - Set User"
    WScript.Quit
End If
stepLog = stepLog & "[STEP 3] Set User 'Raisul': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 3: Set User"

' ------------------------------------------------------------
' STEP 4: Set Company
' ------------------------------------------------------------
retVal = oSS.nSetCompany("RAI")
If retVal = 0 Then
    MsgBox "STEP 4 FAILED: nSetCompany" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 4 - Set Company"
    WScript.Quit
End If
stepLog = stepLog & "[STEP 4] Set Company 'RAI': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 4: Set Company"

' ------------------------------------------------------------
' STEP 5: Set Date using Year/Month/Day (no Format function)
' ------------------------------------------------------------
Dim sToday
sToday = Year(Date) & Right("0" & Month(Date), 2) & Right("0" & Day(Date), 2)

retVal = oSS.nSetDate("B/M", sToday)
If retVal = 0 Then
    MsgBox "STEP 5 FAILED: nSetDate" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 5 - Set Date"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 5] Set Date (" & sToday & "): OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 5: Set Date"

' ------------------------------------------------------------
' STEP 6: Set Module
' ------------------------------------------------------------
retVal = oSS.nSetModule("B/M")
If retVal = 0 Then
    MsgBox "STEP 6 FAILED: nSetModule" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 6 - Set Module"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 6] Set Module 'B/M': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 6: Set Module"

' ------------------------------------------------------------
' STEP 7: Set Program Context (try BOM UI task)
' NOTE: Task name can vary; adjust if your lookup differs.
' ------------------------------------------------------------
Dim secObj, uiTask

uiTask = "BM_Bill_ui"   ' <-- If this fails, tell me what your BOM maintenance task is called in LookupTask.
secObj = oSS.nSetProgram(oSS.nLookupTask(uiTask))
stepLog = stepLog & "[STEP 7] Set Program Context (" & uiTask & "): retVal=" & secObj & vbCrLf
MsgBox stepLog, vbInformation, "Step 7: Program Context"

' ------------------------------------------------------------
' STEP 8: Create BM_Bill_bus Object
' ------------------------------------------------------------
Set oBOM = oPVX.NewObject("BM_Bill_bus", oSS)
If oBOM Is Nothing Then
    MsgBox "STEP 8 FAILED: Could not create BM_Bill_bus." & vbCrLf & _
           "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 8 - Business Object"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 8] BM_Bill_bus Created: OK" & vbCrLf
MsgBox stepLog, vbInformation, "Step 8: Business Object"

' ============================================================
' BOM DETAILS - Customize these
' ============================================================
Dim sParentItem, sRevision, sEffectiveDate, sBillDesc, sBillType
Dim sComponentItem, sQtyPer

sParentItem    = "TESTITEM01"
sRevision      = ""          ' optional
sEffectiveDate = sToday      ' use YYYYMMDD as you do elsewhere
sBillDesc 	   = "Test Description"
sBillType	   = "S"


sComponentItem = "ITEMA"
sQtyPer        = "2"

' ------------------------------------------------------------
' STEP 9: Set Key (BOM Header)
' IMPORTANT: Key signature varies by system (Parent Item / BOM No / Revision).
' If this step fails, we need your exact BM_Bill_bus key fields.
' ------------------------------------------------------------
retVal = oBOM.nSetKeyValue("BillNo$",sParentItem)
If retVal = 0 Then
    MsgBox "STEP 9a FAILED: nSetKeyValue BillNo (BOM Header)" & vbCrLf & _
           "Parent='" & sParentItem & "'" & vbCrLf & _
           "Error: " & oBOM.sLastErrorMsg, vbCritical, "Step 9a - nSetKeyValue Bill No"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 9a] Set BOM Key BillNo'" & sParentItem & "': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 9a: BOM Key Set"

retVal = oBOM.nSetKeyValue("Revision$","000")
If retVal = 0 Then
    MsgBox "STEP 9b FAILED: nSetKeyValue Revision (BOM Header)" & vbCrLf & _
           "Parent='" & sParentItem & "'" & vbCrLf & _
           "Error: " & oBOM.sLastErrorMsg, vbCritical, "Step 9a - nSetKeyValue Revision"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 9b] Set BOM Key Revision'" & "000" & "': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 9b: BOM Key Set"

retVal = oBOM.nSetKey()
If retVal = 0 Then
    MsgBox "STEP 9c FAILED: nSetKey (BOM Header)" & vbCrLf & _
           "Parent='" & sParentItem & "'" & vbCrLf & _
           "Error: " & oBOM.sLastErrorMsg, vbCritical, "Step 9c - nSetKey "
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 9c] Set BOM Key '" & "': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 9c: BOM Key Set"

' ------------------------------------------------------------
' STEP 10: Create New BOM (if not existing)
' Some BOs require nNew() before setting values.
' If nNew is not supported, it will simply fail and we continue.
' ------------------------------------------------------------
On Error Resume Next
retVal = oBOM.nNew()
On Error GoTo 0
stepLog = stepLog & "[STEP 10] nNew attempted: retVal=" & retVal & " (ignore if unsupported)" & vbCrLf
MsgBox stepLog, vbInformation, "Step 10: New BOM (attempted)"

' ------------------------------------------------------------
' STEP 11-13: Set BOM Header Values
' NOTE: Field names vary; these are common guesses.
' If any return 0, read MsgBox with sLastErrorMsg and tell me which field failed.
' ------------------------------------------------------------
retVal = oBOM.nSetValue("BillDesc1$", sBillDesc) ' sometimes BillNo$ is the parent
stepLog = stepLog & "[STEP 11] Set BillDesc1$='" & sBillDesc & "': retVal=" & retVal & vbCrLf

retVal = oBOM.nSetValue("BillType$", sBillType)
stepLog = stepLog & "[STEP 12] Set BillType$='" & sBillType & "': retVal=" & retVal & vbCrLf


MsgBox stepLog, vbInformation, "Steps 11-13: Header Values Set"

' ------------------------------------------------------------
' STEP 13: Get Lines object (oLines) from BM_Bill_bus
'
' Sage 100 BOs commonly expose a child "Lines" object.
' Depending on version, it can be:
'   - oBOM.oLines
'   - oBOM.Lines
'   - oBOM.GetLines() / oBOM.oBillLines, etc.
'
' This step tries the common properties. If your property name differs,
' tell me what it's called in Object Browser / BOI docs.
' ------------------------------------------------------------
Set oLines = Nothing
On Error Resume Next

Set oLines = oBOM.oLines
If oLines Is Nothing Then
    Err.Clear
    Set oLines = oBOM.Lines
End If

On Error GoTo 0

If oLines Is Nothing Then
    MsgBox "STEP 13 FAILED: Could not get Lines collection from BM_Bill_bus." & vbCrLf & _
           "BM_Bill_bus.sLastErrorMsg: " & oBOM.sLastErrorMsg & vbCrLf & vbCrLf & _
           "Action: confirm the child collection/property name (oLines/Lines/etc.).", _
           vbCritical, "Step 13 - Lines Object"
    oSS.nCleanup()
    WScript.Quit
End If

Dim added
added = 0

On Error Resume Next

Err.Clear
added = oLines.nAddLine()
If Err.Number <> 0 Or added = 0 Then
    Err.Clear
    added = oLines.nNew()
End If

If Err.Number <> 0 Or added = 0 Then
    Err.Clear
    added = oLines.nNewLine()
End If

On Error GoTo 0

If added = 0 Then
    MsgBox "STEP 14 FAILED: Could not add a new BOM line." & vbCrLf & _
           "Lines LastError: " & oLines.sLastErrorMsg, vbCritical, "Step 14 - Add Line"
    oSS.nCleanup()
    WScript.Quit
End If

stepLog = stepLog & "[STEP 14] New line added: OK (retVal=" & added & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 14: Line Added"


' ------------------------------------------------------------
' STEP 15-16: Set component values on the current line
' NOTE: Use your actual field names (you used QuantityPerBill$).
' ------------------------------------------------------------
retVal = oLines.nSetValue("ComponentItemCode$", sComponentItem)
stepLog = stepLog & "[STEP 15] Line ComponentItemCode$='" & sComponentItem & "': retVal=" & retVal & vbCrLf 
retVal = oLines.nSetValue("QuantityPerBill$", sQtyPer)
stepLog = stepLog & "[STEP 16] Line QuantityPerBill$='" & sQtyPer & "': retVal=" & retVal & vbCrLf 
MsgBox stepLog, vbInformation, "Steps 15-16: Line Values Set"

' ------------------------------------------------------------
' STEP 17: Write / Save BOM
' Usually you write the parent (oBOM.nWrite). Some setups require
' writing the line object too; we attempt both safely.
' ------------------------------------------------------------
On Error Resume Next
Call oLines.nWrite() ' ignore if unsupported
On Error GoTo 0

retVal = oBOM.nWrite()
If retVal = 0 Then
    MsgBox "STEP 17 FAILED: BOM NOT saved." & vbCrLf & _
           "Error: " & oBOM.sLastErrorMsg, vbCritical, "Step 17 - Write Failed"
    oSS.nCleanup()
    WScript.Quit
End If

stepLog = stepLog & "[STEP 17] BOM Written/Saved: OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 17: BOM Saved"

' ------------------------------------------------------------
' STEP 18: Cleanup
' ------------------------------------------------------------
On Error Resume Next
If Not (oLines Is Nothing) Then oLines.DropObject()
Set oLines = Nothing

oBOM.DropObject()
Set oBOM = Nothing

oSS.nCleanup()
oSS.DropObject()
Set oSS = Nothing

Set oPVX = Nothing
On Error GoTo 0

stepLog = stepLog & "[STEP 18] Session Cleaned Up & Objects Released: OK" & vbCrLf & vbCrLf
stepLog = stepLog & "===========================================" & vbCrLf
stepLog = stepLog & " SUCCESS! BOM Created/Updated:" & vbCrLf
stepLog = stepLog & "  Parent Item : " & sParentItem & vbCrLf
stepLog = stepLog & "  Component   : " & sComponentItem & vbCrLf
stepLog = stepLog & "  Qty Per     : " & sQtyPer & vbCrLf
stepLog = stepLog & "  Date Used   : " & sToday & vbCrLf
stepLog = stepLog & "==========================================="

MsgBox stepLog, vbInformation, "COMPLETE - BOM Created in Sage 100"