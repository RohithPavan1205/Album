# 🚀 Quick Start Guide - Live Album

## ⚡ 5-Minute Setup

### **Unity AR App**

```bash
# 1. Open Project
Open Unity Hub → Add Project → Select Live-Album-main

# 2. Verify Setup
- Open AR Session Origin in Hierarchy
- Add ARTrackingValidator component
- Right-click → "Validate AR Setup"
- Check console for ✓ marks

# 3. Test in Editor
- Press Play (F5)
- Press 'D' to toggle debug overlay
- Check FPS and stats

# 4. Build for Android
File → Build Settings → Android → Build
- Wait 5-10 minutes
- Install APK on device

# 5. Test on Device
- Point camera at printed AR image
- Video should appear instantly
- Tap button to play/pause
```

---

### **Django Backend**

```bash
# 1. Start Server
cd backend_server
source venv/bin/activate  # or venv\Scripts\activate on Windows
python manage.py runserver

# 2. Access Admin
Open: http://127.0.0.1:8000/admin/
Login: admin / your_password

# 3. Create Client
Navigation → Clients → Add Client
- Client ID: TEST001
- Client Name: Test Event
- Save

# 4. Upload Assets
Navigation → Batch Upload
- Select client: TEST001
- Drag & drop 3 images
- Drag & drop 1 video
- Click "Upload Assets"

# 5. Verify
Check Media Assets list
- Should see uploaded files
- Firebase URLs present
```

---

## 📱 Testing on Device

### **Android**
```bash
# Install APK
adb install path/to/LiveAlbum.apk

# Or manually:
# 1. Copy APK to device
# 2. Open file manager
# 3. Tap APK to install
# 4. Grant permissions
```

### **Test AR**
1. Launch app
2. Grant camera permission
3. Point at printed image
4. **Expected:** Video appears instantly
5. Tap button to control playback

---

## 🔧 Quick Troubleshooting

### **Unity Won't Build**
```
Solution: File → Build Settings → Switch Platform
```

### **Backend Won't Start**
```bash
# Install dependencies
pip install -r requirements.txt

# Run migrations
python manage.py migrate
```

### **AR Not Detecting Images**
```
- Ensure good lighting
- Use high-contrast images
- Print images at correct size
- Check AR Foundation is installed
```

### **Upload Fails**
```
- Check Firebase credentials
- Verify internet connection
- Check file size (< 50MB recommended)
```

---

## 📊 Performance Targets

| Component | Metric | Target |
|-----------|--------|--------|
| **Unity** | FPS | 50+ |
| **Unity** | Memory | < 400MB |
| **Unity** | Load Time | < 1s |
| **Backend** | Page Load | < 2s |
| **Backend** | Upload | Depends on size |

---

## 🎯 Key Features to Test

### **Unity**
- [x] AR image detection
- [x] Video playback
- [x] Button interaction
- [x] Haptic feedback
- [x] Performance (FPS)

### **Backend**
- [x] Dashboard statistics
- [x] Batch upload
- [x] Drag & drop
- [x] File previews
- [x] Responsive design

---

## 📚 Documentation Quick Links

- **Full Testing Guide:** `TESTING_GUIDE.md`
- **Setup Instructions:** `SETUP_CHECKLIST.md`
- **AR Enhancements:** `AR_TRACKING_ENHANCEMENTS.md`
- **Backend UI:** `BACKEND_UI_IMPROVEMENTS.md`
- **Complete Overview:** `PROJECT_SUMMARY.md`
- **Navigation:** `INDEX.md`

---

## 💡 Pro Tips

1. **Use Debug Overlay** - Press 'D' in Unity to see FPS and stats
2. **Test on Real Device** - AR doesn't work in Unity editor
3. **Print Good Images** - High contrast, well-lit photos work best
4. **Check Console** - Look for errors in Unity console and browser DevTools
5. **Read Docs** - Comprehensive guides available for everything

---

## ✅ Success Checklist

**Unity:**
- [ ] Project opens without errors
- [ ] APK builds successfully
- [ ] App runs on device
- [ ] AR tracking works
- [ ] Performance is smooth

**Backend:**
- [ ] Server starts
- [ ] Admin login works
- [ ] Dashboard displays
- [ ] Upload works
- [ ] Assets appear

**Integration:**
- [ ] Unity loads backend assets
- [ ] End-to-end flow works

---

## 🆘 Need Help?

1. Check `TESTING_GUIDE.md` for detailed instructions
2. Review troubleshooting sections
3. Enable debug mode for more info
4. Check console logs

---

## 🎉 You're Ready!

**Next Steps:**
1. Follow Unity setup above
2. Follow Backend setup above
3. Test on device
4. Enjoy your AR experience!

---

**Made with ❤️ for Live Album**

*Everything you need to get started quickly!*
