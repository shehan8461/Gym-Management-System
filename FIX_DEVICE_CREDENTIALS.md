# Fix Device Authentication - Unauthorized Error

## Problem
Getting "Unauthorized" error when connecting to device:
```
Get users search error: Unauthorized - Unauthorized
subStatusCode: "badAuthorization"
errorCode: 1073741827 (0x40000003)
```

## Root Cause
Your device username/password stored in the database is **incorrect** or the device requires different authentication.

## Solution Steps

### Step 1: Verify Device Login via Web Browser

**Test device credentials manually:**

1. Open browser: `http://192.168.1.64` (your device IP)
2. You should see Hikvision login page
3. Try these common credentials:
   - Username: `admin` / Password: `admin`
   - Username: `admin` / Password: `12345`
   - Username: `admin` / Password: `admin123`
   - Username: `admin` / Password: (empty)

4. **If none work:**
   - Check device label for default password
   - Or use the password you set when initializing device

5. **Once logged in successfully**, note the exact username and password used

### Step 2: Update Credentials in Desktop App

**Method A: Via Biometric Page (GUI)**

1. Run your app: `dotnet run`
2. Login to app
3. Go to **Biometric** page (left menu)
4. Find your device in the list
5. Click **Edit** button (pencil icon)
6. Update:
   - **IP Address**: 192.168.1.64 (confirm this is correct)
   - **Port**: 80 (default)
   - **Username**: admin (or whatever worked in browser)
   - **Password**: (enter the correct password)
7. Click **Save**
8. Click **Test Connection** to verify

**Method B: Direct Database Update**

If you can't access the GUI, update database directly:

```sql
-- Connect to your PostgreSQL database
-- Update device credentials (replace with YOUR values)

UPDATE "BiometricDevices" 
SET 
    "Username" = 'admin',
    "Password" = 'your_actual_password',
    "IPAddress" = '192.168.1.64',
    "Port" = 80
WHERE "DeviceId" = 1;  -- Adjust DeviceId if different

-- Verify the update
SELECT "DeviceId", "DeviceName", "IPAddress", "Port", "Username", "Password" 
FROM "BiometricDevices";
```

### Step 3: Test Device Connection

**After updating credentials:**

1. In app, go to **Biometric** page
2. Click **Test Connection** on your device
3. Should see: "✅ Connected successfully"

**If still fails:**
- Verify IP address is reachable: `ping 192.168.1.64`
- Check device is powered on
- Ensure device and computer on same network

### Step 4: Retry User Operations

Once connection works:

1. Go to **Members** page
2. Select a member
3. Click **Enroll Fingerprint** (biometric icon)
4. Click **START ENROLLMENT**
5. Should see: "✅ User created! Look for Employee ID: X"

## Common Credential Issues

### Issue 1: Default Password Changed
**Device was activated with custom password**

**Solution:**
- Check device activation documentation
- Use Hikvision SADP tool to reset password
- Or factory reset device (last resort)

### Issue 2: Special Characters in Password
**Password contains special characters causing encoding issues**

**Solution:**
- Change device password to simple alphanumeric (via web interface)
- Update in desktop app
- Test again

### Issue 3: Case Sensitivity
**Username is case-sensitive on some devices**

**Solution:**
- Try: `admin`, `Admin`, `ADMIN`
- Try: `administrator`
- Verify exact case from device login

### Issue 4: Wrong Authentication Method
**Some devices use different authentication schemes**

Current code uses **HTTP Basic Authentication**. If device requires digest auth:

**Check HikvisionService.cs (around line 130):**
```csharp
var authValue = Convert.ToBase64String(
    Encoding.ASCII.GetBytes($"{username}:{password}"));
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Basic", authValue);
```

This should work for most Hikvision devices. If not, device might need:
- Digest authentication
- Token-based authentication
- Session cookies

## Device Reset Options

### Option 1: SADP Tool Password Reset

**Hikvision SADP (Search Active Devices Protocol) Tool:**

1. Download SADP from Hikvision website
2. Install and run SADP
3. It will discover your device on network
4. Select device
5. Click "Reset Password" or "Activate"
6. Follow wizard to set new password
7. Update password in desktop app

### Option 2: Factory Reset via Device

**Physical device reset:**

1. Go to device admin menu
2. Navigate to: **System → Maintenance**
3. Find: **Restore Default** or **Factory Reset**
4. Confirm reset (WARNING: Deletes all users!)
5. Device reboots with default credentials
6. Reconfigure network settings
7. Try default: admin/admin or admin/12345

### Option 3: Reset Button

**Hardware reset (if available):**

1. Locate reset button on device (usually inside or back)
2. Power on device
3. Press and hold reset for 10-15 seconds
4. Device resets to factory defaults
5. Reconfigure via web interface

## Verification Checklist

After fixing credentials, verify:

- [ ] Can login to device web interface: `http://192.168.1.64`
- [ ] Desktop app "Test Connection" succeeds
- [ ] Can create test user: Employee ID 9999
- [ ] "SHOW DEVICE USERS" returns user list
- [ ] Can enroll fingerprint on device
- [ ] Attendance scanning works

## Testing Commands

**Test device HTTP access:**

```powershell
# Test if device is reachable
ping 192.168.1.64

# Test HTTP connection
Invoke-WebRequest -Uri "http://192.168.1.64" -Method Get

# Test with credentials (replace with yours)
$user = "admin"
$pass = "your_password"
$base64 = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${user}:${pass}"))
$headers = @{ "Authorization" = "Basic $base64" }

Invoke-WebRequest -Uri "http://192.168.1.64/ISAPI/System/deviceInfo" `
    -Headers $headers `
    -Method Get
```

**If above command succeeds:**
- You have correct credentials
- Update them in desktop app database
- Should work immediately

**If above command fails with 401:**
- Credentials are wrong
- Try different username/password combinations

## Quick Reference

| Error | Meaning | Fix |
|-------|---------|-----|
| Unauthorized (401) | Wrong credentials | Update username/password |
| badAuthorization | Invalid auth header | Check credentials format |
| Timeout | Device unreachable | Check IP, network, power |
| Forbidden (403) | User lacks permissions | Use admin account |
| Not Found (404) | Wrong endpoint | Check device model/firmware |

## Default Credentials by Model

| Model | Default Username | Default Password |
|-------|-----------------|------------------|
| DS-K1T8003MF | admin | 12345 or admin |
| Most Hikvision | admin | 12345 |
| After activation | admin | (custom set during setup) |
| Factory reset | admin | 12345 or admin |

## Still Not Working?

1. **Use Hikvision SADP tool** to discover and configure device
2. **Check device documentation** for specific model defaults
3. **Contact IT/Network admin** if device is managed
4. **Factory reset device** as last resort (loses all data)

---

**Next Step After Fix:**
Once credentials work, go back and click **"SHOW DEVICE USERS"** - it should now list users instead of showing Unauthorized error.
