# 🧪 Testing Guide - Live Album AR Project

## 📋 Overview

This guide covers testing both the Unity AR client and Django backend to ensure everything works correctly.

---

## 🎮 Part 1: Testing Unity AR Client

### **Prerequisites**
- Unity 2021.3+ installed
- Android/iOS device with AR support
- USB cable for device connection
- Physical printed images for AR tracking

### **Step 1: Open Project in Unity**

```bash
# Navigate to project
cd /Users/rohithpavan/Desktop/Live-Album-main

# Open with Unity Hub or directly
# Unity Hub: Add project and open
```

**Expected Result:** Project opens without errors

---

### **Step 2: Verify Scripts**

1. **Check Scripts Folder:**
   - Open `Assets/Scripts/`
   - Verify these files exist:
     - ✅ `ARButton.cs`
     - ✅ `ImageVideoManager.cs`
     - ✅ `VideoController.cs`
     - ✅ `ARTrackingDebugger.cs`
     - ✅ `ARTrackingValidator.cs`
     - ✅ `RuntimeWeddingLoader.cs`
     - ✅ `HideBeforeVideo.cs`

2. **Check for Compilation Errors:**
   - Open Unity Console (Ctrl/Cmd + Shift + C)
   - Should show **0 errors**
   - Warnings are okay

**Expected Result:** All scripts present, no compilation errors

---

### **Step 3: Validate AR Setup (In Unity Editor)**

1. **Add Validator Component:**
   - Find `AR Session Origin` in Hierarchy
   - Add Component → `ARTrackingValidator`
   - Right-click component → **"Validate AR Setup"**

2. **Check Console Output:**
   ```
   [ARValidator] === AR TRACKING VALIDATION ===
   [ARValidator] ✓ ARSession found
   [ARValidator] ✓ ARSessionOrigin found
   [ARValidator] ✓ ARTrackedImageManager found
   [ARValidator] ✓ Library contains X images
   [ARValidator] === VALIDATION COMPLETE ===
   ```

**Expected Result:** All checkmarks, no warnings

---

### **Step 4: Test in Unity Editor (Play Mode)**

1. **Enter Play Mode:**
   - Press Play button (or F5)
   - Wait for scene to load

2. **Enable Debug Overlay:**
   - Press **'D'** key
   - Debug panel should appear showing:
     ```
     === AR TRACKING DEBUG ===
     FPS: 60.0
     Memory: XXX MB
     Tracking: Active: 0, Well-Tracked: 0
     ```

3. **Check Console:**
   - Should see initialization messages
   - No red errors

**Expected Result:** 
- Scene runs smoothly
- Debug overlay toggles with 'D' key
- FPS shows 50+
- No errors in console

---

### **Step 5: Build for Android**

1. **Configure Build Settings:**
   ```
   File → Build Settings
   - Platform: Android
   - Click "Switch Platform" if needed
   ```

2. **Player Settings:**
   ```
   Edit → Project Settings → Player
   
   ✅ Company Name: Your Company
   ✅ Product Name: Live Album
   ✅ Package Name: com.yourcompany.livealbum
   ✅ Minimum API Level: Android 7.0 (API 24)
   ✅ Target API Level: Automatic (highest installed)
   
   XR Settings:
   ✅ ARCore Supported: Checked
   ```

3. **Build APK:**
   ```
   File → Build Settings → Build
   - Choose save location
   - Wait for build to complete (5-10 minutes)
   ```

**Expected Result:** APK file created successfully

---

### **Step 6: Test on Android Device**

1. **Install APK:**
   ```bash
   # Connect device via USB
   # Enable USB debugging on device
   
   # Install APK
   adb install path/to/your.apk
   
   # Or drag APK to device and install manually
   ```

2. **Launch App:**
   - Open "Live Album" app on device
   - Grant camera permissions
   - Grant storage permissions

3. **Test AR Tracking:**
   - Point camera at a printed AR image
   - **Expected:** Video overlay appears instantly
   - **Expected:** Video plays smoothly
   - **Expected:** Tracking is stable (no flicker)

4. **Test Button Interaction:**
   - Tap the play/pause button
   - **Expected:** Button shrinks slightly (visual feedback)
   - **Expected:** Device vibrates (haptic feedback)
   - **Expected:** Video pauses/plays

5. **Test Performance:**
   - Enable debug overlay (if included in build)
   - **Expected:** FPS 30+ on mid-range devices
   - **Expected:** FPS 50+ on high-end devices
   - **Expected:** Smooth video playback

6. **Test Multiple Images:**
   - Switch between different AR images
   - **Expected:** Fast switching (< 1 second)
   - **Expected:** No memory leaks
   - **Expected:** Stable performance

**Expected Results:**
- ✅ App launches without crashes
- ✅ Camera permission granted
- ✅ AR images detected instantly
- ✅ Videos play smoothly
- ✅ Button responds to taps
- ✅ Haptic feedback works
- ✅ No lag or stuttering

---

### **Step 7: Test Edge Cases**

1. **Poor Lighting:**
   - Test in dim lighting
   - **Expected:** Tracking quality warning (if debug enabled)
   - **Expected:** Still functional

2. **Rapid Movement:**
   - Move camera quickly
   - **Expected:** Grace period prevents flicker
   - **Expected:** Video stays visible briefly

3. **Partial Occlusion:**
   - Cover part of AR image
   - **Expected:** Tracking continues if enough visible
   - **Expected:** Smooth recovery when fully visible

4. **App Backgrounding:**
   - Press home button
   - Return to app
   - **Expected:** App resumes correctly
   - **Expected:** AR tracking restarts

---

## 🌐 Part 2: Testing Django Backend

### **Prerequisites**
- Python 3.8+ installed
- Virtual environment activated
- Dependencies installed

### **Step 1: Start Development Server**

```bash
# Navigate to backend
cd /Users/rohithpavan/Desktop/Live-Album-main/backend_server

# Activate virtual environment (if not already)
source venv/bin/activate  # Mac/Linux
# or
venv\Scripts\activate  # Windows

# Install dependencies
pip install -r requirements.txt

# Run migrations
python manage.py migrate

# Create superuser (if not exists)
python manage.py createsuperuser
# Username: admin
# Email: admin@example.com
# Password: (your choice)

# Start server
python manage.py runserver
```

**Expected Output:**
```
System check identified no issues (0 silenced).
February 04, 2026 - 17:30:00
Django version 4.2.x, using settings 'config.settings'
Starting development server at http://127.0.0.1:8000/
Quit the server with CONTROL-C.
```

---

### **Step 2: Test Admin Login**

1. **Open Browser:**
   ```
   http://127.0.0.1:8000/admin/
   ```

2. **Login:**
   - Enter superuser credentials
   - Click "Log in"

**Expected Result:**
- ✅ Beautiful modern login page
- ✅ Successful login
- ✅ Redirects to dashboard

---

### **Step 3: Test Dashboard**

1. **Verify Dashboard Elements:**
   - **Statistics Cards:** Should show counts
     - Total Clients: X
     - Images Uploaded: X
     - Videos Uploaded: X
     - Total Assets: X
   
   - **Quick Actions:** 4 cards visible
     - Add New Client
     - Batch Upload
     - View All Assets
     - Download APK
   
   - **Recent Uploads:** List of recent assets (or empty state)
   
   - **AR Application Card:** App info and download button
   
   - **System Info:** Shows Django version, database, etc.

2. **Test Navigation:**
   - Click each navigation link:
     - Dashboard ✅
     - Clients ✅
     - Media Assets ✅
     - Batch Upload ✅
   
   - **Expected:** Smooth transitions, no errors

**Expected Result:**
- ✅ Modern, beautiful dashboard
- ✅ All statistics display correctly
- ✅ Quick actions work
- ✅ Navigation is smooth
- ✅ Responsive design (resize browser)

---

### **Step 4: Test Client Management**

1. **Add New Client:**
   ```
   Navigation → Clients → Add Client
   
   Client ID: TEST001
   Client Name: Test Wedding
   
   Click "Save"
   ```

2. **Verify Client:**
   - Should redirect to client list
   - New client appears in list
   - Statistics update (Total Clients +1)

**Expected Result:**
- ✅ Client created successfully
- ✅ Appears in list
- ✅ Dashboard stats updated

---

### **Step 5: Test Batch Upload (Main Feature)**

1. **Navigate to Batch Upload:**
   ```
   Navigation → Batch Upload
   or
   Dashboard → Quick Actions → Batch Upload
   ```

2. **Verify Upload Interface:**
   - **Expected:** Modern upload page with:
     - Client dropdown
     - Image upload area (drag & drop)
     - Video upload area (drag & drop)
     - Upload tips section

3. **Test Image Upload:**
   - **Method 1 - Drag & Drop:**
     - Drag 2-3 images to "Upload Images" area
     - **Expected:** Area highlights on hover
     - **Expected:** Image previews appear below
     - **Expected:** Image count updates
   
   - **Method 2 - Click to Browse:**
     - Click "Upload Images" area
     - Select multiple images
     - **Expected:** File dialog opens
     - **Expected:** Previews appear

4. **Test Video Upload:**
   - Drag or select 1 video file
   - **Expected:** Video preview appears
   - **Expected:** Video count updates

5. **Test File Removal:**
   - Click "×" button on any preview
   - **Expected:** File removed from preview
   - **Expected:** Count updates

6. **Submit Upload:**
   - Select client from dropdown
   - Ensure files are selected
   - Click "Upload Assets"
   - **Expected:** Progress indication
   - **Expected:** Success message
   - **Expected:** Redirect to asset list

7. **Verify Upload:**
   - Check Media Assets list
   - **Expected:** New assets appear
   - **Expected:** Correct client association
   - **Expected:** Firebase URLs present

**Expected Results:**
- ✅ Drag & drop works smoothly
- ✅ File previews display correctly
- ✅ Remove buttons work
- ✅ Upload completes successfully
- ✅ Files appear in Firebase
- ✅ Database records created

---

### **Step 6: Test Responsive Design**

1. **Resize Browser Window:**
   - Desktop (1920px+): Full layout
   - Tablet (768px-1024px): Adjusted grid
   - Mobile (< 768px): Single column

2. **Test on Mobile Device:**
   - Open admin on phone browser
   - **Expected:** Fully responsive
   - **Expected:** Touch-friendly buttons
   - **Expected:** Readable text

**Expected Result:**
- ✅ Layout adapts to screen size
- ✅ All features accessible on mobile
- ✅ No horizontal scrolling

---

### **Step 7: Test Error Handling**

1. **Upload Without Client:**
   - Try uploading without selecting client
   - **Expected:** Error message

2. **Upload Without Files:**
   - Select client but no files
   - **Expected:** Submit button disabled or error

3. **Invalid File Types:**
   - Try uploading .txt or .exe file
   - **Expected:** Validation error

**Expected Result:**
- ✅ Proper error messages
- ✅ No crashes
- ✅ User-friendly feedback

---

## 🔗 Part 3: Integration Testing

### **Test Full Workflow**

1. **Backend: Upload Assets**
   ```
   Admin → Batch Upload
   - Client: TEST001
   - Images: 3 photos
   - Video: 1 video
   - Upload
   ```

2. **Backend: Get API Response**
   ```bash
   # Test API endpoint
   curl http://127.0.0.1:8000/api/assets/TEST001/
   ```
   
   **Expected:** JSON with image and video URLs

3. **Unity: Configure Client ID**
   - In Unity, set client ID to "TEST001"
   - Build and install on device

4. **Device: Test AR Experience**
   - Point at printed images
   - **Expected:** Videos from backend appear
   - **Expected:** Smooth playback

**Expected Result:**
- ✅ Backend → Unity → Device flow works
- ✅ Assets load correctly
- ✅ End-to-end functionality verified

---

## 📊 Performance Testing

### **Unity Performance Metrics**

| Metric | Target | How to Check |
|--------|--------|--------------|
| FPS | 50+ | Debug overlay (Press 'D') |
| Memory | < 400MB | Debug overlay |
| Load Time | < 1s | Time from detection to playback |
| Tracking Latency | < 100ms | Visual observation |

### **Backend Performance Metrics**

| Metric | Target | How to Check |
|--------|--------|--------------|
| Page Load | < 2s | Browser DevTools → Network |
| Upload Speed | Depends on file size | Progress bar |
| Dashboard Load | < 1s | Browser DevTools |
| API Response | < 500ms | curl with timing |

---

## ✅ Testing Checklist

### **Unity AR Client**
- [ ] Project opens without errors
- [ ] All scripts compile successfully
- [ ] Validator shows no issues
- [ ] Play mode works in editor
- [ ] Debug overlay toggles with 'D'
- [ ] APK builds successfully
- [ ] App installs on device
- [ ] Camera permissions granted
- [ ] AR images detected instantly
- [ ] Videos play smoothly
- [ ] Button responds with feedback
- [ ] Haptic feedback works
- [ ] Performance is smooth (30+ FPS)
- [ ] No memory leaks
- [ ] App handles backgrounding

### **Django Backend**
- [ ] Server starts without errors
- [ ] Admin login works
- [ ] Dashboard displays correctly
- [ ] Statistics are accurate
- [ ] Navigation works
- [ ] Client creation works
- [ ] Batch upload page loads
- [ ] Drag & drop works
- [ ] File previews display
- [ ] Upload completes successfully
- [ ] Assets appear in database
- [ ] Firebase URLs are valid
- [ ] Responsive design works
- [ ] Mobile view is functional
- [ ] Error handling works

### **Integration**
- [ ] Backend API responds correctly
- [ ] Unity loads backend assets
- [ ] End-to-end flow works
- [ ] Performance meets targets

---

## 🐛 Common Issues & Solutions

### **Unity Issues**

**Issue:** Compilation errors
```
Solution: 
1. Check all scripts are properly renamed
2. Verify no old script references
3. Reimport scripts: Right-click → Reimport
```

**Issue:** AR not working in editor
```
Solution: AR only works on device, not in editor
Use AR Foundation Remote for editor testing
```

**Issue:** Low FPS on device
```
Solution:
1. Enable object pooling
2. Increase scaleCheckInterval
3. Enable pauseWhenInvisible
4. Lower video resolution
```

### **Backend Issues**

**Issue:** Static files not loading
```bash
Solution:
python manage.py collectstatic
```

**Issue:** Dashboard stats not showing
```
Solution: Check context processor is added to settings.py
```

**Issue:** Upload fails
```
Solution:
1. Check Firebase credentials
2. Verify FIREBASE_STORAGE_BUCKET in settings
3. Check file permissions
```

---

## 📝 Test Report Template

```markdown
# Test Report - Live Album

**Date:** YYYY-MM-DD
**Tester:** Your Name
**Environment:** Unity 2021.3.x / Django 4.2.x

## Unity AR Client
- [ ] Build: Success/Fail
- [ ] Installation: Success/Fail
- [ ] AR Tracking: Excellent/Good/Poor
- [ ] Performance: FPS: XX
- [ ] Issues Found: None / List issues

## Django Backend
- [ ] Server Start: Success/Fail
- [ ] Dashboard: Working/Issues
- [ ] Batch Upload: Working/Issues
- [ ] Performance: Fast/Slow
- [ ] Issues Found: None / List issues

## Integration
- [ ] End-to-End: Working/Issues
- [ ] Overall Rating: ⭐⭐⭐⭐⭐

## Notes:
[Add any additional observations]
```

---

## 🎯 Success Criteria

**Project is ready for production when:**
- ✅ All Unity tests pass
- ✅ All backend tests pass
- ✅ Integration tests pass
- ✅ Performance meets targets
- ✅ No critical bugs
- ✅ User experience is smooth

---

**Happy Testing! 🧪**

*If you encounter any issues, refer to the troubleshooting sections in the documentation.*
