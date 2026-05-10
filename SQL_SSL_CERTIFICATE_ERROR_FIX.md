# SQL Server SSL Certificate Error - Complete Fix Guide

## The Error

```
A connection was successfully established with the server, but then an error occurred during the login process. 
(provider: SSL Provider, error: 0 - The certificate chain was issued by an authority that is not trusted.)
```

---

## Understanding the Issue

This error occurs because:
1. SQL Server is configured to use SSL/TLS encryption
2. The server is using a self-signed certificate or certificate from an untrusted CA
3. The client (your application) doesn't trust the certificate

---

## Solutions (Choose One)

### ? Solution 1: Trust Server Certificate (Recommended for Development)

**Best for**: Local development, internal networks, self-signed certificates

#### Step 1: Update Connection String

Add `TrustServerCertificate=true` or `TrustServerCertificate=True` to your connection string:

**Before:**
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;
```

**After:**
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

#### Step 2: Update Your Application Settings

Open your settings file and update the connection string:

**File Location**: `%APPDATA%\Aml.BOM.Import\appsettings.json`

```json
{
  "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;",
  "SageSettings": {
    "ServerUrl": "",
    "Username": "",
    "Password": "",
    "CompanyCode": ""
  },
  "ReportSettings": {
    "OutputDirectory": "",
    "AutoGenerateReports": false
  }
}
```

#### Step 3: Verify in Application

1. Open your BOM Import application
2. Go to **Settings** view
3. Enter/update your connection string:
   ```
   Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
   ```
4. Click **Test Connection**
5. You should see: "Connection successful!"

---

### ? Solution 2: Disable Encryption (Not Recommended for Production)

**Best for**: Quick testing only

Add `Encrypt=false` to disable SSL entirely:

```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;Encrypt=false;
```

?? **Warning**: This disables encryption. Don't use in production!

---

### ? Solution 3: Install Trusted Certificate (Recommended for Production)

**Best for**: Production environments, security-sensitive applications

#### Option A: Install Certificate on Client Machine

1. **Export certificate from SQL Server**:
   ```sql
   -- Run this on SQL Server
   SELECT * FROM sys.certificates;
   ```

2. **Import to Trusted Root Certification Authorities**:
   - Open `certmgr.msc` (Certificate Manager)
   - Right-click **Trusted Root Certification Authorities** ? **Certificates**
   - Click **Action** ? **All Tasks** ? **Import**
   - Select the certificate file
   - Complete the wizard

#### Option B: Use Certificate from Trusted CA

1. Purchase SSL certificate from trusted CA (DigiCert, Let's Encrypt, etc.)
2. Install on SQL Server
3. No client-side changes needed

---

### ? Solution 4: Configure SQL Server to Not Force Encryption

**Best for**: Controlling encryption at server level

#### Steps:

1. Open **SQL Server Configuration Manager**
2. Expand **SQL Server Network Configuration**
3. Right-click **Protocols for [Your Instance]**
4. Select **Properties**
5. Go to **Flags** tab
6. Set **Force Encryption** to **No**
7. Click **OK**
8. **Restart SQL Server service**

---

## Connection String Options Explained

### Complete Connection String Anatomy

```
Server=localhost;
Database=MAS_AML;
Trusted_Connection=true;
TrustServerCertificate=true;
Encrypt=true;
```

| Parameter | Values | Description |
|-----------|--------|-------------|
| **Server** | hostname/IP | SQL Server instance address |
| **Database** | database name | Target database |
| **Trusted_Connection** | true/false | Use Windows Authentication |
| **TrustServerCertificate** | true/false | Trust self-signed certificates |
| **Encrypt** | true/false | Enable SSL/TLS encryption |

### Common Combinations

#### 1. Development (Recommended)
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```
- ? Uses Windows Auth
- ? Encryption enabled
- ? Trusts self-signed cert

#### 2. Production with Trusted CA
```
Server=production-server;Database=MAS_AML;Trusted_Connection=true;Encrypt=true;
```
- ? Uses Windows Auth
- ? Encryption enabled
- ? Only trusts valid CA certs

#### 3. SQL Authentication
```
Server=localhost;Database=MAS_AML;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```
- ? Uses SQL Auth
- ? Encryption enabled
- ? Trusts self-signed cert

#### 4. No Encryption (Testing Only)
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;Encrypt=false;
```
- ?? No encryption
- ?? Not recommended

---

## Step-by-Step Fix for Your Application

### Method 1: Through Application UI

1. **Launch Application**
   ```
   Open Aml.BOM.Import.UI.exe
   ```

2. **Navigate to Settings**
   - Click on **Settings** in the navigation menu

3. **Update Connection String**
   - Current: `Server=localhost;Database=MAS_AML;Trusted_Connection=true;`
   - New: `Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;`

4. **Test Connection**
   - Click **Test Connection** button
   - Should show: "Connection successful!"

5. **Save Settings**
   - Click **Save Settings**

### Method 2: Edit Settings File Directly

1. **Locate Settings File**
   ```
   %APPDATA%\Aml.BOM.Import\appsettings.json
   ```
   Or:
   ```
   C:\Users\[YourUsername]\AppData\Roaming\Aml.BOM.Import\appsettings.json
   ```

2. **Open in Text Editor**
   ```
   notepad %APPDATA%\Aml.BOM.Import\appsettings.json
   ```

3. **Update Connection String**
   ```json
   {
     "DatabaseConnectionString": "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;"
   }
   ```

4. **Save File**

5. **Restart Application**

---

## Verification Steps

### Test 1: Command Line Test

```powershell
# Test connection using SqlCmd
sqlcmd -S localhost -d MAS_AML -E -C
```

The `-C` flag trusts the server certificate.

### Test 2: Application Test

```csharp
// This code tests the connection
var connectionString = "Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;";

using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
Console.WriteLine("? Connection successful!");
```

### Test 3: Settings View Test

1. Open application
2. Go to Settings
3. Click "Test Connection"
4. Should see success message

---

## Security Considerations

### Development Environment ?
```
TrustServerCertificate=true
```
- ? **Safe**: Self-signed certificates are acceptable
- ? **Convenient**: No certificate management needed

### Production Environment ??

**Option 1 (Recommended)**: Use trusted certificate
```
Server=prod;Database=MAS_AML;Trusted_Connection=true;Encrypt=true;
```
- ? Full encryption
- ? Certificate validation
- ? No `TrustServerCertificate=true` needed

**Option 2 (If necessary)**: Trust self-signed with documentation
```
Server=prod;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```
- ?? Document why this is necessary
- ?? Plan to move to trusted certificate
- ?? Ensure network is secure

---

## Troubleshooting

### Issue: Still Getting Certificate Error

**Solution 1**: Clear connection string cache
1. Delete: `%APPDATA%\Aml.BOM.Import\appsettings.json`
2. Restart application
3. Re-enter connection string with `TrustServerCertificate=true`

**Solution 2**: Check for typos
- Correct: `TrustServerCertificate=true`
- Wrong: `TrustServerCertificate=True` (case doesn't matter but be consistent)
- Wrong: `TrustServerCertificate = true` (spaces matter)

**Solution 3**: Verify SQL Server is running
```powershell
Get-Service | Where-Object {$_.Name -like "*SQL*"}
```

### Issue: "Login failed for user"

This is a different error (authentication, not SSL).

**Solutions**:
1. Verify Windows user has access to database
2. Or use SQL Authentication:
   ```
   Server=localhost;Database=MAS_AML;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
   ```

### Issue: "A network-related or instance-specific error"

This means SQL Server is not reachable.

**Solutions**:
1. Verify server name: `localhost` or `.\SQLEXPRESS` or actual server name
2. Check SQL Server service is running
3. Verify TCP/IP is enabled in SQL Server Configuration Manager

---

## Common Connection String Patterns

### Pattern 1: Local SQL Server (Express)
```
Server=localhost\SQLEXPRESS;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Pattern 2: Local SQL Server (Default Instance)
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Pattern 3: Remote SQL Server
```
Server=192.168.1.100;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Pattern 4: Named Instance
```
Server=SERVER\INSTANCE;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Pattern 5: SQL Authentication
```
Server=localhost;Database=MAS_AML;User Id=bomuser;Password=SecurePassword123;TrustServerCertificate=true;
```

---

## Quick Fix Summary

### For Immediate Resolution:

**Add this to your connection string:**
```
;TrustServerCertificate=true
```

**Full Example:**
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Where to Add:

1. **Settings View** in application
2. **appsettings.json** file at: `%APPDATA%\Aml.BOM.Import\appsettings.json`

### Test:

Open application ? Settings ? Test Connection ? Should succeed! ?

---

## Related Documentation

- [DATABASE_CONNECTION_GUIDE.md](DATABASE_CONNECTION_GUIDE.md) - Complete database setup
- [SQL_CONNECTION_IMPLEMENTATION_SUMMARY.md](SQL_CONNECTION_IMPLEMENTATION_SUMMARY.md) - Connection architecture
- [SETTINGS_FILE_LOCATION.md](SETTINGS_FILE_LOCATION.md) - Where settings are stored

---

## Need More Help?

If you're still experiencing issues:

1. Check application logs at: `%APPDATA%\Aml.BOM.Import\Logs\`
2. Verify SQL Server error logs
3. Test connection with SQL Server Management Studio (SSMS)
4. Ensure firewall allows SQL Server connections

---

**Status**: Common issue with simple fix  
**Solution**: Add `TrustServerCertificate=true` to connection string  
**Time to Fix**: < 2 minutes  
**Works for**: Development and internal networks
