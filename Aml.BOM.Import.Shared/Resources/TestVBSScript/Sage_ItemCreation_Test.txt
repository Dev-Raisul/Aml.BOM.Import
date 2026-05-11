' ============================================================
' Sage 100 - Create New Item Script
' ============================================================

Dim oPVX, oSS, oItem
Dim retVal, stepLog

stepLog = "===== Sage 100 New Item Creation Log =====" & vbCrLf & vbCrLf

' ------------------------------------------------------------
' STEP 1: Initialize ProvideX
' ------------------------------------------------------------
On Error Resume Next
Set oPVX = CreateObject("ProvideX.Script")
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
' STEP 3: Set User
' ------------------------------------------------------------
retVal = oSS.nSetUser("Raisul", "")
If retVal = 0 Then
    MsgBox "STEP 3 FAILED: nSetUser" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 3 - Set User"
    WScript.Quit
End If
stepLog = stepLog & "[STEP 3] Set User 'Raisul': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 3: Set User"

' ' ------------------------------------------------------------
' ' STEP 4: Logon FIRST (before SetDate and SetModule)
' ' ------------------------------------------------------------
' retVal = oSS.nLogon()
' If retVal = 0 Then
    ' MsgBox "STEP 4 FAILED: nLogon returned 0" & vbCrLf & _
           ' "Error: " & oSS.sLastErrorMsg & vbCrLf & vbCrLf & _
           ' "Check: Username/Password, Company Code, Sage is running.", _
           ' vbCritical, "Step 4 - Logon Failed"
    ' oSS.nCleanup()
    ' WScript.Quit
' End If
' stepLog = stepLog & "[STEP 4] nLogon: OK (retVal=" & retVal & ")" & vbCrLf
' MsgBox stepLog, vbInformation, "Step 5: Logon SUCCESS"

' ------------------------------------------------------------
' STEP 5: Set Company
' ------------------------------------------------------------
retVal = oSS.nSetCompany("RAI")
If retVal = 0 Then
    MsgBox "STEP 5 FAILED: nSetCompany" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 4 - Set Company"
    WScript.Quit
End If
stepLog = stepLog & "[STEP 5] Set Company 'RAI': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 5: Set Company"

' ------------------------------------------------------------
' STEP 6: Set Date using Year/Month/Day (no Format function)
' ------------------------------------------------------------
Dim sToday
sToday = Year(Date) & Right("0" & Month(Date), 2) & Right("0" & Day(Date), 2)

retVal = oSS.nSetDate("I/M", sToday)
If retVal = 0 Then
    MsgBox "STEP 6 FAILED: nSetDate" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 6 - Set Date"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 6] Set Date (" & sToday & "): OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 6: Set Date"

' ------------------------------------------------------------
' STEP 7: Set Module
' ------------------------------------------------------------
retVal = oSS.nSetModule("I/M")
If retVal = 0 Then
    MsgBox "STEP 7 FAILED: nSetModule" & vbCrLf & "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 7 - Set Module"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 7] Set Module 'I/M': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 7: Set Module"

' ------------------------------------------------------------
' STEP 8: Set Program Context
' ------------------------------------------------------------
Dim secObj
secObj = oSS.nSetProgram(oSS.nLookupTask("CI_ItemCode_ui"))
stepLog = stepLog & "[STEP 8] Set Program Context: OK" & vbCrLf
MsgBox stepLog, vbInformation, "Step 8: Program Context"

' ------------------------------------------------------------
' STEP 9: Create CI_ItemCode_bus Object
' ------------------------------------------------------------
Set oItem = oPVX.NewObject("CI_ItemCode_bus", oSS)
If oItem Is Nothing Then
    MsgBox "STEP 9 FAILED: Could not create CI_ItemCode_bus." & vbCrLf & _
           "Error: " & oSS.sLastErrorMsg, vbCritical, "Step 9 - Business Object"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 9] CI_ItemCode_bus Created: OK" & vbCrLf
MsgBox stepLog, vbInformation, "Step 9: Business Object"

' ============================================================
' ITEM DETAILS - Customize these
' ============================================================
Dim sItemCode, sItemDesc, sProductLine, sItemType
Dim dStandardCost, dSalesPrice

sItemCode     = "TESTITEM01"
sItemDesc     = "Test Item Created via VBScript"
sProductLine  = "0001"
sItemType     = "1"       ' 1=Regular
dStandardCost = "10.00"
dSalesPrice   = "19.99"

' ------------------------------------------------------------
' STEP 10: Set Key (Item Code)
' ------------------------------------------------------------
retVal = oItem.nSetKey(sItemCode)
If retVal = 0 Then
    MsgBox "STEP 10 FAILED: nSetKey" & vbCrLf & "Error: " & oItem.sLastErrorMsg, vbCritical, "Step 10 - Set Key"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 10] Set Key '" & sItemCode & "': OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 10: Item Key Set"

' ------------------------------------------------------------
' STEPS 11-15: Set Field Values
' ------------------------------------------------------------
retVal = oItem.nSetValue("ItemCodeDesc$", sItemDesc)
stepLog = stepLog & "[STEP 11] Set Description '" & sItemDesc & "': retVal=" & retVal & vbCrLf

retVal = oItem.nSetValue("ProductLine$", sProductLine)
stepLog = stepLog & "[STEP 12] Set Product Line '" & sProductLine & "': retVal=" & retVal & vbCrLf

MsgBox(oItem.sLastErrorMsg)

retVal = oItem.nSetValue("ItemType$", sItemType)
stepLog = stepLog & "[STEP 13] Set Item Type '" & sItemType & "': retVal=" & retVal & vbCrLf

retVal = oItem.nSetValue("StandardUnitCost", dStandardCost)
stepLog = stepLog & "[STEP 14] Set Std Cost '$" & dStandardCost & "': retVal=" & retVal & vbCrLf

retVal = oItem.nSetValue("StandardUnitPrice", dSalesPrice)
stepLog = stepLog & "[STEP 15] Set Sales Price '$" & dSalesPrice & "': retVal=" & retVal & vbCrLf

MsgBox stepLog, vbInformation, "Steps 11-15: Field Values Set"

' ------------------------------------------------------------
' STEP 16: Write / Save
' ------------------------------------------------------------
retVal = oItem.nWrite()
If retVal = 0 Then
    MsgBox "STEP 16 FAILED: Item NOT saved." & vbCrLf & _
           "Error: " & oItem.sLastErrorMsg, vbCritical, "Step 16 - Write Failed"
    oSS.nCleanup()
    WScript.Quit
End If
stepLog = stepLog & "[STEP 16] Item Written/Saved: OK (retVal=" & retVal & ")" & vbCrLf
MsgBox stepLog, vbInformation, "Step 16: Item Saved"

' ------------------------------------------------------------
' STEP 17: Cleanup
' ------------------------------------------------------------
oItem.DropObject()
Set oItem = Nothing
oSS.nCleanup()
oSS.DropObject()
Set oSS = Nothing
Set oPVX = Nothing

stepLog = stepLog & "[STEP 17] Session Cleaned Up & Objects Released: OK" & vbCrLf & vbCrLf
stepLog = stepLog & "===========================================" & vbCrLf
stepLog = stepLog & " SUCCESS! Item Created:" & vbCrLf
stepLog = stepLog & "  Code        : " & sItemCode & vbCrLf
stepLog = stepLog & "  Description : " & sItemDesc & vbCrLf
stepLog = stepLog & "  Product Line: " & sProductLine & vbCrLf
stepLog = stepLog & "  Date Used   : " & sToday & vbCrLf
stepLog = stepLog & "  Std Cost    : $" & dStandardCost & vbCrLf
stepLog = stepLog & "  Sales Price : $" & dSalesPrice & vbCrLf
stepLog = stepLog & "==========================================="

MsgBox stepLog, vbInformation, "COMPLETE - New Item Created in Sage 100"