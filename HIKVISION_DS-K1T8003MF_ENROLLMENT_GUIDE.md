# 📱 Hikvision DS-K1T8003MF Fingerprint Enrollment Guide

## Device Information
- **Model**: DS-K1T8003MF  
- **Type**: Fingerprint Access Control Terminal
- **Manufacturer**: Hangzhou Hikvision Digital Technology Co., Ltd.
- **Power**: 12V⎓1A
- **Sensor**: Red fingerprint sensor on right side

---

## ⚠️ IMPORTANT: Local Enrollment Required

**Your DS-K1T8003MF model requires MANUAL enrollment using the device screen.**

❌ Remote API enrollment (`/AccessControl/CaptureFingerPrint`) is **NOT supported** on this model  
✅ You must enroll fingerprints **directly on the device**

---

## 🔧 Enrollment Process (Step-by-Step)

### 1. **Start Enrollment in Software**
- Open the Gym Management System
- Go to **Biometric** page
- Click **Enroll** next to your device
- Select the member
- Click **START ENROLLMENT**

### 2. **Follow On-Screen Instructions**
The system will show you:
- Member ID (e.g., 76722019)
- Member Name
- Step-by-step device instructions

### 3. **On the Hikvision Device** (PHYSICAL DEVICE)

#### Method A: Direct Enrollment (Easiest)
1. Press **ESC** button on device keypad
2. Navigate to **User** or **User Management** (use ▲▼ arrows)
3. Press **OK** / **Enter**
4. Select **Add User** or **New User**
5. Enter **User ID**: The member ID shown in software (e.g., `76722019`)
6. Select **FP** (Fingerprint) enrollment
7. Place finger on **RED sensor** 3 times:
   - Device will beep after each successful scan
   - Keep finger flat and steady
   - Use same finger position each time
8. Device shows **"Success"** or beeps twice
9. Press **ESC** to exit menu

#### Method B: Admin Menu (Alternative)
1. On device, enter **Admin Password** (default: `12345` or set during device setup)
2. Go to **System** → **User Management**
3. Select **Enroll User**
4. Enter **Employee No**: The member ID (e.g., `76722019`)
5. Choose **Fingerprint**
6. Scan finger 3 times on RED sensor
7. Confirm and exit

### 4. **System Verification**
After enrolling on device:
- Software will automatically detect the new user (polling every 3 seconds for up to 2 minutes)
- If auto-detection fails, you'll see a **Manual Confirmation** dialog
- Click **YES** if you saw "Success" on the device
- System marks enrollment as complete

---

## 🎯 Fingerprint Scanning Tips

### ✅ DO:
- **Clean your finger** before scanning (no moisture/oil)
- **Place finger FLAT** on the red sensor
- **Press firmly** but gently
- **Use same angle** for all 3 scans
- **Wait for beep** before lifting finger
- **Use the SAME finger** for all 3 scans

### ❌ DON'T:
- Don't move finger during scan
- Don't use wet/dirty fingers
- Don't change finger angle between scans
- Don't lift too quickly
- Don't use different fingers

---

## 🔍 Troubleshooting

### Issue: "Device not responding"
**Solution:**
1. Check IP address: `192.168.1.100` (your device)
2. Ensure device is on same network as computer
3. Test connection in Biometric page
4. Check port: Use port `80` (standard)

### Issue: "Can't enter device menu"
**Solution:**
1. Press **ESC** button (top row, left side)
2. If password required, enter: `12345` (default admin password)
3. If forgotten, reset device (check manual)

### Issue: "Fingerprint scan fails"
**Solution:**
1. **Clean the RED sensor** with soft cloth
2. **Dry your finger** completely
3. Try **different finger** (thumb or index usually best)
4. **Check sensor** for scratches/damage
5. **Update firmware** if available

### Issue: "System says timeout"
**Solution:**
1. You'll see **Manual Confirmation** dialog
2. Check device screen - did it show "Success"?
3. If YES on device → Click **YES** in dialog
4. System marks enrollment complete
5. Test by having member scan finger on device

### Issue: "Member not found in device after enrollment"
**Possible Causes:**
1. **Wrong User ID** entered on device
   - Must match Member ID from software exactly
2. **Device didn't save** - check device user list:
   - Device Menu → User Management → User List
3. **Different user ID** - device assigned auto ID
   - Check what ID the device used

---

## 🔌 Device Configuration

### Network Settings (if needed to reconfigure)
1. Press **ESC** → **System** → **Network**
2. Set **IP Address**: `192.168.1.100` (or your network)
3. Set **Gateway**: `192.168.1.1` (your router)
4. Set **Subnet Mask**: `255.255.255.0`
5. Set **Port**: `80` (for ISAPI)
6. Enable **ISAPI**
7. Save and restart device

### User Management Settings
1. **Max Users**: 3000 (default)
2. **FP Template**: Standard
3. **Matching Threshold**: Medium (recommended)
4. **Duress Alarm**: Disabled (unless needed)

### Access Control Settings
1. **Verify Mode**: FP (Fingerprint only) or FP+Card
2. **Door Unlock Time**: 5 seconds
3. **Authentication Timeout**: 10 seconds

---

## 📊 Verification After Enrollment

### Check on Device:
1. Device Menu → **User** → **User List**
2. Look for your Member ID (e.g., `76722019`)
3. Should show **FP: 1** (fingerprint enrolled)

### Check in Software:
1. Go to Biometric page
2. Device should show in list
3. Member should be able to scan finger on device
4. Attendance should record automatically when scanned

### Test Attendance:
1. Have member place finger on RED sensor
2. Device beeps and shows name
3. Check Attendance page in software
4. Should see new attendance record with Type = "Biometric"

---

## 🔐 Device Default Credentials

**Default Admin:**
- Username: `admin`
- Password: `12345`

**⚠️ IMPORTANT**: Change default password for security!

**To change password:**
1. Device Menu → System → **Password**
2. Enter current password: `12345`
3. Enter new password (6-digit recommended)
4. Confirm new password
5. Update in software device settings

---

## 📞 Quick Reference

| Action | Button/Menu |
|--------|-------------|
| Enter Menu | **ESC** |
| Navigate | **▲▼** arrows |
| Select | **OK** or **►** |
| Back | **ESC** or **◄** |
| Number Entry | Keypad **0-9** |
| Enroll User | Menu → User → Add User |
| View Users | Menu → User → User List |
| Network Settings | Menu → System → Network |
| Factory Reset | Menu → System → Reset |

---

## ✅ Success Indicators

**On Device:**
- ✅ Beep after each finger scan
- ✅ "Success" or "OK" message on screen
- ✅ Green LED flash
- ✅ User appears in device user list

**In Software:**
- ✅ "Fingerprint enrolled successfully" message
- ✅ Can close enrollment dialog
- ✅ Member can scan finger for attendance
- ✅ Attendance records show "Biometric" type

---

## 🆘 Support

### If Still Having Issues:
1. **Check device firmware version**
   - Menu → System → Device Info
   - Update if available from Hikvision website

2. **Check ISAPI enabled**
   - Menu → Network → ISAPI: ON

3. **Verify network connectivity**
   - Ping device: `ping 192.168.1.100`
   - Access web interface: `http://192.168.1.100`

4. **Check device manual**
   - Model-specific instructions
   - Available from Hikvision support

5. **Contact support**
   - Hikvision Technical Support
   - Provide device serial number: `FZ0813S95`

---

## 📌 Quick Checklist

Before enrollment:
- [ ] Device powered on
- [ ] Network connected (green LED)
- [ ] IP address correct: `192.168.1.100`
- [ ] Software can connect to device
- [ ] Finger clean and dry

During enrollment:
- [ ] Correct Member ID entered on device
- [ ] Same finger used for all 3 scans
- [ ] Device beeped after each scan
- [ ] "Success" message shown on device
- [ ] ESC pressed to exit menu

After enrollment:
- [ ] User visible in device user list
- [ ] Software detected enrollment OR manual confirmation clicked
- [ ] Test scan works on device
- [ ] Attendance records correctly

---

**Model**: DS-K1T8003MF  
**Your Device IP**: 192.168.1.100:80  
**Enrollment Mode**: Local (Device Screen)  
**Detection**: Automatic polling + Manual confirmation fallback

**Last Updated**: January 2026
