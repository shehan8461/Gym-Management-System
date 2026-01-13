# Device User Not Found - Troubleshooting Guide

## Problem
After clicking "START ENROLLMENT" and seeing "User created successfully", you go to the physical device but cannot find the user/ID.

## Solution Steps

### Step 1: Verify User Creation
1. In the Fingerprint Enrollment dialog, click **"SHOW DEVICE USERS"** button
2. This will display ALL users currently stored on the device
3. Look for your Member ID in the "Employee ID" column

**What to look for:**
```
Employee ID | Name
------------|----------------
1           | John Doe
5           | Jane Smith
12          | Mike Wilson
```

### Step 2: Check Member ID
The Member ID in your database should match the Employee ID on the device **exactly**.

**To verify:**
1. Look at the member details in your app (Member ID is shown)
2. Look for this same number in the device user list
3. The "Employee ID" on device = "Member ID" in database

### Step 3: Navigate Device Menu
If the user IS on the device (confirmed by SHOW DEVICE USERS):

**On DS-K1T8003MF:**
1. Touch screen to wake up
2. Enter admin menu (may require admin password/card)
3. Go to: **User Management** or **Personnel Management**
4. Select: **Search by Employee ID**
5. Enter the Member ID number
6. Select the user
7. Choose: **Enroll Fingerprint** or **Add Fingerprint**
8. Follow on-screen prompts to scan finger 3 times

### Step 4: If User NOT in Device List

If SHOW DEVICE USERS doesn't show your member, the creation failed. Check:

#### Device Connection Issues
- Is the device IP address correct?
- Can you ping the device from your computer?
- Is the device on the same network?
- Are device credentials (username/password) correct?

**Test connection:**
```powershell
# Run in PowerShell
ping [DEVICE_IP_ADDRESS]
```

#### Device API Response
The desktop app might be getting an error from the device. Check for:
- Device memory full (too many users)
- Invalid characters in member name
- Duplicate Employee ID conflict

### Step 5: Manual Device Verification

**Check device directly:**
1. Go to device admin menu
2. Navigate to **User Management**
3. Select **User List** or **All Users**
4. Scroll through and verify total user count
5. This should match what SHOW DEVICE USERS displays

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| User not created | Connection timeout | Check network, retry creation |
| Can't find in device menu | Looking in wrong section | Use "Search by Employee ID" |
| Duplicate error | Member ID already used | Delete old user or use different member |
| Device full | Storage limit reached | Delete unused users from device |
| Name shows as "?" | Special characters | Edit member name to use only letters/numbers |

## Working Workflow

**Correct process:**
1. Click "SAVE & ENROLL FINGERPRINT" on member
2. Dialog opens, click "START ENROLLMENT"
3. See: "✅ User created! Look for Employee ID: 5"
4. Click "SHOW DEVICE USERS" - verify Employee ID 5 appears
5. Go to physical device
6. Menu → User Management → Search by ID → Enter "5"
7. Select user → Enroll Fingerprint → Scan finger 3x
8. Return to desktop app
9. Click "VERIFY ENROLLMENT" - should show success

## Debug Information

**What Employee ID means:**
- Desktop creates user on device with `EmployeeNo` = Member ID
- Device stores this as "Employee ID" 
- When member scans finger, device sends event with this ID
- Desktop app matches this ID to database Member ID for attendance

**Network Requirements:**
- Both computer and device must be on same LAN
- Device must have static IP or reserved DHCP
- No firewall blocking port (default: 80)
- Device firmware should be up to date

## Still Having Issues?

1. **Test with simple member:**
   - Create test member with simple name: "Test User"
   - Note the Member ID (e.g., 99)
   - Try enrolling - look for Employee ID 99

2. **Check device logs:**
   - Some Hikvision devices have operation logs
   - Check if user creation commands are being received

3. **Verify device model:**
   - Confirm it's DS-K1T8003MF
   - Check firmware version (may affect API compatibility)

4. **Try device web interface:**
   - Open browser: `http://[DEVICE_IP]`
   - Login with admin credentials
   - Go to Personnel Management
   - Manually add a test user with Employee ID 999
   - See if it appears in SHOW DEVICE USERS

## Support Information

If user IS created (appears in SHOW DEVICE USERS) but you still can't enroll:
- This is a device operation issue, not app issue
- Refer to Hikvision DS-K1T8003MF user manual
- Contact Hikvision support for device-specific enrollment procedures

If user is NOT created (doesn't appear in SHOW DEVICE USERS):
- This is an API communication issue
- Check network connectivity
- Verify device credentials
- Ensure ISAPI is enabled on device
