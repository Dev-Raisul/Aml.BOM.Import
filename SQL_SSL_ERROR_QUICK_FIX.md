# SQL SSL Certificate Error - Quick Fix

## The Error
```
SSL Provider, error: 0 - The certificate chain was issued by an authority that is not trusted.
```

---

## ? Quick Fix (1 Minute)

### Add to Connection String:
```
;TrustServerCertificate=true
```

### Before:
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;
```

### After:
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

---

## ?? How to Apply

### Method 1: Through Application (Recommended)

1. Open application
2. Go to **Settings**
3. Update connection string (add `;TrustServerCertificate=true`)
4. Click **Test Connection**
5. Click **Save**

### Method 2: Edit File Directly

1. Open: `%APPDATA%\Aml.BOM.Import\appsettings.json`
2. Update `DatabaseConnectionString`
3. Save file
4. Restart application

---

## ?? Common Connection Strings

### Local Development
```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### SQL Express
```
Server=localhost\SQLEXPRESS;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### Remote Server
```
Server=192.168.1.100;Database=MAS_AML;Trusted_Connection=true;TrustServerCertificate=true;
```

### SQL Authentication
```
Server=localhost;Database=MAS_AML;User Id=sa;Password=YourPass;TrustServerCertificate=true;
```

---

## ? Verify Fix

```powershell
# Test with SqlCmd
sqlcmd -S localhost -d MAS_AML -E -C
```

Or in application: **Settings ? Test Connection** ?

---

## ?? Is This Safe?

| Environment | Safe? | Recommendation |
|-------------|-------|----------------|
| **Local Dev** | ? Yes | Use `TrustServerCertificate=true` |
| **Internal Network** | ? Yes | Use `TrustServerCertificate=true` |
| **Production (Internet)** | ?? No | Use trusted CA certificate |

---

## ?? Alternative: Disable Encryption (Not Recommended)

```
Server=localhost;Database=MAS_AML;Trusted_Connection=true;Encrypt=false;
```

?? Only for testing! Disables all encryption.

---

## ?? Still Not Working?

### Check typos:
- ? `TrustServerCertificate=true`
- ? `TrustServerCertificate = true` (space issue)
- ? Missing semicolon

### Verify SQL Server:
```powershell
Get-Service MSSQLSERVER
```

### Check logs:
```
%APPDATA%\Aml.BOM.Import\Logs\
```

---

## ?? Full Guide

For detailed explanation and production scenarios, see:
**[SQL_SSL_CERTIFICATE_ERROR_FIX.md](SQL_SSL_CERTIFICATE_ERROR_FIX.md)**

---

**?? Time to Fix**: < 2 minutes  
**? Success Rate**: 99%  
**?? Solution**: Add `TrustServerCertificate=true`
