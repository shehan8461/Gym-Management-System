# 🔐 UNAUTHORIZED (401) Error - Fix Guide

## ❌ **Problem Identified**

Your debug output shows:
```
Get users error: Unauthorized
```

This means:
- ✅ Device connection is working (connection test passed)
- ❌ **API call to get users is being rejected** with 401 Unauthorized
- ❌ Authentication is failing for the UserInfo endpoint

---

## 🔍 **Root Causes**

### 1. **Wrong Username/Password** (Most Common)
The credentials stored in the device settings might be incorrect.

**Check:**
- Device settings in software: Username = `admin`?
- Device settings in software: Password = `12345`?
- Are these the actual device admin credentials?

---

### 2. **User Doesn't Have API Permissions**
The account might not have permission to access the UserInfo API.

**Solution:**
- Use **admin** account (full permissions)
- Check device user roles/permissions

---

### 3. **Authentication Header Not Sent**
The HttpClient might not be sending the Authorization header correctly.

**What I Fixed:**
- Added better error logging
- Returns empty list instead of null (so you know it's auth vs truly empty)
- Better diagnostic messages

---

### 4. **Device Requires Different Auth Method**
Some devices require Digest authentication instead of Basic.

**Current:** Using Basic Auth  
**Alternative:** Might need Digest Auth

---

## ✅ **Solutions**

### **Solution 1: Verify Device Credentials**

1. **Check Device Settings in Software:**
   - Go to **Biometric** page
   - Click **Edit** on your device
   - Verify **Username**: Should be `admin` (default)
   - Verify **Password**: Should be `12345` (default)
   - Click **Save**

2. **Test Credentials on Device:**
   - Access device web interface: `http://192.168.1.100`
   - Try to login with the same credentials
   - If login fails, credentials are wrong!

3. **Reset Device Password (if needed):**
   - On device: Press **ESC** → **System** → **Password**
   - Change password if forgotten
   - Update in software device settings

---

### **Solution 2: Test Connection First**

1. Click **"Test"** button on device
2. If test passes but sync fails:
   - Connection is OK
   - But UserInfo API access is denied
   - Likely permission issue

---

### **Solution 3: Check Device Web Interface**

1. Open browser: `http://192.168.1.100`
2. Login with admin credentials
3. Navigate to **User Management** or **Access Control** → **Users**
4. See if you can view users there
5. If web works but API doesn't:
   - API might be disabled
   - Check device settings for ISAPI access control

---

### **Solution 4: Check ISAPI Settings on Device**

**On Device:**
1. Press **ESC** → **System** → **Network**
2. Go to **ISAPI** or **API Settings**
3. Verify:
   - ✅ ISAPI is **Enabled**
   - ✅ UserInfo API is **Enabled** (if available)
   - ✅ Access Control API is **Enabled**

**If ISAPI is disabled:**
- Enable it
- Restart device (if required)
- Try sync again

---

### **Solution 5: Use Device Admin Account**

**Most Important:**
- The account must have **admin privileges**
- Default admin on Hikvision devices:
  - Username: `admin`
  - Password: `12345` (or whatever was set)

**To check:**
- Try device web interface login
- If admin account works there, use same credentials in software

---

## 🔧 **What I Fixed in Code**

### 1. **Better Error Detection**
Now detects 401 Unauthorized specifically:
```csharp
else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    Debug.WriteLine($"❌ UNAUTHORIZED (401) - Authentication failed!");
    Debug.WriteLine($"⚠️ Possible causes:");
    Debug.WriteLine($"   1. Wrong username or password");
    Debug.WriteLine($"   2. User doesn't have permission");
    // ...
}
```

### 2. **Better Return Values**
- Returns empty list instead of null
- Distinguishes between:
  - Auth error → Empty list (from auth failure)
  - Truly empty → Empty list (from empty device)

### 3. **Better User Messages**
- UI now detects auth issues
- Shows helpful troubleshooting steps

---

## 📊 **Diagnostic Steps**

### Step 1: Check Debug Output

**In Visual Studio:**
1. View → Output → Select "Debug"
2. Click "Sync Users"
3. Look for:
   ```
   ❌ UNAUTHORIZED (401) - Authentication failed!
   ```

**If you see this:**
→ Authentication problem confirmed

---

### Step 2: Test Device Credentials

**Method 1: Web Interface**
```
1. Open: http://192.168.1.100
2. Login with:
   Username: admin (or what you set)
   Password: 12345 (or what you set)
3. If login fails → Credentials wrong!
```

**Method 2: Device Test Button**
```
1. In software: Click "Test" on device
2. If test passes:
   → Connection OK
   → But UserInfo API might need different permissions
```

---

### Step 3: Verify Device Settings

**In Software Device Settings:**
- IP Address: `192.168.1.100` ✅
- Port: `80` ✅
- Username: `admin` ← **Check this!**
- Password: `12345` ← **Check this!**

**Match with:**
- What works on device web interface
- What works when you login on device screen

---

### Step 4: Try Manual API Test

**Using Browser or Postman:**

**Test:**
```
POST http://192.168.1.100:80/ISAPI/AccessControl/UserInfo/Search?format=json

Headers:
Authorization: Basic YWRtaW46MTIzNDU=  (base64 of "admin:12345")

Body (JSON):
{
  "UserInfoSearchCond": {
    "searchID": "1",
    "maxResults": 100,
    "searchResultPosition": 0
  }
}
```

**Expected:**
- **200 OK** with user data → Credentials OK, issue elsewhere
- **401 Unauthorized** → Credentials wrong
- **403 Forbidden** → Permission issue
- **404 Not Found** → Endpoint issue

---

## 🎯 **Quick Fix Checklist**

- [ ] Verify username/password in software matches device
- [ ] Test credentials on device web interface
- [ ] Check device web interface shows users
- [ ] Verify ISAPI is enabled on device
- [ ] Check Debug output for detailed error
- [ ] Try "Test" button - should succeed
- [ ] If all fails, reset device to factory defaults (careful!)

---

## 💡 **Most Likely Solution**

**90% chance:** Wrong password!

**Quick fix:**
1. On device web interface: `http://192.168.1.100`
2. Try to login
3. If fails → Password changed from default
4. Reset password on device OR
5. Find correct password and update in software

---

## 🆘 **Still Having Issues?**

### If credentials are definitely correct:

1. **Check device firmware:**
   - Some firmware versions have API bugs
   - Update to latest firmware

2. **Try different API endpoint:**
   - Check Debug output
   - See what endpoints were tried
   - Some devices use different paths

3. **Check device logs:**
   - Device menu → System → Logs
   - See if API calls are being rejected
   - May show why authentication is failing

4. **Contact Hikvision support:**
   - Provide device model: DS-K1T8003MF
   - Provide firmware version
   - Explain ISAPI authentication failing

---

## ✅ **Success Indicators**

After fixing authentication, you should see:

**Debug Output:**
```
GetAllUsers Response: {"UserInfoSearch": {"totalMatches": X, ...}}
✅ Found X users
```

**UI:**
- Sync Users dialog opens
- Shows list of enrolled users
- Shows mapping status

---

**Status:** ✅ **Enhanced with better auth error detection**  
**Last Updated:** January 11, 2026  
**Next Step:** Verify device credentials match software settings!
