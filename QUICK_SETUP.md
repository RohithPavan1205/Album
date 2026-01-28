# Quick Setup Checklist

## ✅ Setup Steps (5 minutes)

### 1. Add Manager Component
- [ ] Select **AR Session Origin** GameObject
- [ ] Add **MultiImageVideoManager** component
- [ ] Set **Default Video Prefab** to your ParentPrefab

### 2. Configure Image Mappings
For each wedding photo:
- [ ] Add entry to **Image Video Mappings** list
- [ ] Set **Image Name** (must match Reference Image Library)
- [ ] Set **Video Source** (URL or file path)

### 3. Update ParentPrefab
- [ ] Add **VideoAnimControl** component to root
- [ ] Assign **Video Player** reference
- [ ] Assign **Video Plane** reference
- [ ] Set **Video Height Offset** = 0.001
- [ ] Set **Video Scale Multiplier** = 1.0

### 4. Update Play/Pause Button
- [ ] Verify **ArButton** component exists
- [ ] Assign **Video Player** reference
- [ ] Ensure button has **Collider** component

### 5. Configure Reference Images
- [ ] Open **XR Reference Image Library**
- [ ] Add all wedding photos
- [ ] **Set physical size for each image** (in meters!)
- [ ] Name images to match mappings

### 6. Test
- [ ] Build to device
- [ ] Point at wedding photo
- [ ] Verify video scales correctly
- [ ] Test play/pause button

---

## 🎯 Critical Settings

### Physical Size (Most Important!)
```
In XR Reference Image Library:
- 10cm x 15cm photo = 0.10 x 0.15 meters
- 15cm x 20cm photo = 0.15 x 0.20 meters
```

### Video Scale Multiplier
```
1.0 = Exact match to image size
0.9 = Slightly smaller
1.1 = Slightly larger
```

### Video Height Offset
```
0.001 = Recommended (prevents z-fighting)
0.01 = Higher above image
```

---

## 🔧 Common Issues

| Problem | Solution |
|---------|----------|
| Video wrong size | Set physical size in Reference Image Library |
| Button not working | Add Collider to button GameObject |
| Video not playing | Assign VideoPlayer in ArButton |
| Multiple videos not working | Check image names match mappings |

---

## 📝 Example Configuration

```
MultiImageVideoManager Settings:
  Default Video Prefab: ParentPrefab
  Image Video Mappings:
    [0] Image Name: "WeddingPhoto1"
        Video Source: "https://example.com/video1.mp4"
    [1] Image Name: "WeddingPhoto2"
        Video Source: "https://example.com/video2.mp4"

VideoAnimControl Settings:
  Video Player: VideoPlayer (assigned)
  Video Plane: Plane (assigned)
  Video Height Offset: 0.001
  Video Scale Multiplier: 1.0

ArButton Settings:
  Video Player: VideoPlayer (assigned)
```

---

## 🚀 You're Ready!

See **REFACTORING_GUIDE.md** for detailed documentation.
