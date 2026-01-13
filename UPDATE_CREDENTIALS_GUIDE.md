# Update Device Credentials Guide

## 🔐 Issue: Unauthorized (401) Error

Your device is rejecting the password. The web interface 404 doesn't matter - **the API will work once credentials are correct**.

---

## ✅ Solution: Update Password in Software

### Step 1: Open Biometric Devices Page
1. Launch your Gym Management System
2. Go to **Biometric** tab/page

### Step 2: Edit Your Device
1. Find your Hikvision device in the list
2. Click **Edit** button (or double-click the device row)

### Step 3: Update Credentials
In the **Edit Device** dialog:
- **Username:** `admin` (should already be set)
- **Password:** Change to: **`metro@kandy1`** ⬅️ **THIS IS THE KEY!**
- **IP Address:** (keep as is)
- **Port:** (usually 80, keep as is)

### Step 4: Save
1. Click **Save** button
2. Device settings updated ✅

---

## 🧪 Test Connection

### Option A: Use Test Connection Button
1. In **Edit Device** dialog, click **TEST CONNECTION**
2. Should show: ✅ **Connection successful!**

### Option B: Use Sync Users Button (Better Test)
1. Close Edit dialog
2. On Biometric page, click **Sync Users** (purple button)
3. Should now work! ✅

---

## 🌐 About Web Interface 404

**The web interface 404 error is NOT a problem!**

Some Hikvision devices (like DS-K1T8003MF) don't have a traditional web interface. They only expose:
- ✅ **ISAPI API** (HTTP REST API) - This is what your software uses
- ❌ **Web Interface** - May not exist or be disabled

**What matters:**
- Your software connects via **ISAPI API** ✅
- The API works independently of web interface ✅
- Update password → API will authenticate ✅

---

## 📋 Common Web Interface Paths to Try (Optional)

If you want to try accessing web interface anyway, try these URLs in your browser:

```
http://[device-ip]:80/
http://[device-ip]:80/doc/page/login.asp
http://[device-ip]:8080/
http://[device-ip]:8000/
http://[device-ip]/ISAPI
```

**But remember:** Even if all of these give 404, the API will still work once you update the password!

---

## 🎯 What to Expect After Update

### Debug Output (should show):
```
🔐 Using credentials: Username=admin, Auth header added
GetAllUsers Response: {"UserInfoSearch": {"totalMatches": X, "UserInfo": [...]}}
✅ Found X users
```

### In Application:
- **Sync Users** button will open dialog
- Dialog shows all enrolled users from device
- Each user shows: Employee No, Name, Valid status, Mapped status

---

## ❓ Still Getting Unauthorized?

If you still get "Unauthorized" after updating password:

1. **Double-check password:** `metro@kandy1` (exact spelling, case-sensitive)
2. **Verify username:** `admin` (not `Admin` or `ADMIN`)
3. **Check IP/Port:** Make sure device IP and port are correct
4. **Try Test Connection:** Should work before trying Sync Users
5. **Check Debug Output:** Look for detailed error messages

---

## 📞 Next Steps

Once credentials are updated and Sync Users works:
1. ✅ All device users will be displayed
2. ✅ You'll see which users are mapped to gym members
3. ✅ You can export the list to CSV
4. ✅ Then we can proceed with next features (mapping, enrollment, etc.)

---

**Summary:** Update password to `metro@kandy1` in software settings → Test with Sync Users → Should work! 🎉
