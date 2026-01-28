# AR Wedding Album - Refactored Scripts Documentation

## Overview

The refactored scripts now support **multiple wedding photos** with individual video content, and videos automatically scale to match the **exact physical dimensions** of tracked images.

## What's New

### ✨ Key Improvements

1. **Multiple Image Support** - Track unlimited wedding photos, each with its own video
2. **Automatic Video Scaling** - Videos scale precisely to match physical image dimensions
3. **Better Architecture** - Cleaner, more maintainable code with proper error handling
4. **Touch Support** - Full support for mobile touch input
5. **Dynamic Management** - Runtime image-video mapping management

---

## Scripts Overview

### 1. ArButton.cs
**Purpose:** Handles play/pause button interactions for video playback

**Key Features:**
- Touch and mouse input support
- Automatic component validation
- Animation control for play/pause states
- Event system for external listeners

**Public Methods:**
- `TogglePlayPause()` - Toggle between play and pause
- `SetVideoPlayer(VideoPlayer vp)` - Set video player reference
- `ResetButton()` - Reset to initial state

---

### 2. VideoAnimControl.cs
**Purpose:** Controls video playback and automatic scaling to tracked images

**Key Features:**
- **Automatic scaling to physical image dimensions**
- Video end detection with fade animations
- Dynamic tracked image size updates
- Component auto-discovery

**Public Methods:**
- `ScaleVideoToTrackedImage()` - Manually trigger scaling update
- `TriggerEndAnimation()` - Play end animation
- `ResetVideo()` - Reset video to beginning
- `SetTrackedImage(ARTrackedImage image)` - Set tracked image reference

**Inspector Properties:**
- `videoHeightOffset` - Offset from image surface (default: 0.001m)
- `videoScaleMultiplier` - Scale multiplier (0.5 - 2.0, default: 1.0)

---

### 3. MultiImageVideoManager.cs (NEW!)
**Purpose:** Manages multiple tracked images and their video content

**Key Features:**
- Map multiple images to individual videos
- Automatic prefab instantiation per tracked image
- Lifecycle management (enable/disable/destroy)
- Runtime mapping additions

**Public Methods:**
- `AddImageVideoMapping(string imageName, GameObject prefab, string videoSource)` - Add mapping at runtime
- `ResetAllVideos()` - Reset all video players

---

## Setup Instructions

### Step 1: Update Your AR Session Origin

1. Select your **AR Session Origin** GameObject
2. Add the **MultiImageVideoManager** component
3. The **ARTrackedImageManager** component will be added automatically

### Step 2: Configure Image-Video Mappings

In the **MultiImageVideoManager** inspector:

1. Set the **Default Video Prefab** (your ParentPrefab)
2. Add entries to **Image Video Mappings** list:
   - **Image Name**: Must match the name in your XR Reference Image Library
   - **Video Prefab**: Prefab to instantiate (or leave empty to use default)
   - **Video Source**: URL or path to video file

Example configuration:
```
Image Video Mappings:
  [0]
    Image Name: "Wedding_Photo_1"
    Video Prefab: ParentPrefab
    Video Source: "https://example.com/wedding_video_1.mp4"
  [1]
    Image Name: "Wedding_Photo_2"
    Video Prefab: ParentPrefab
    Video Source: "https://example.com/wedding_video_2.mp4"
```

### Step 3: Update Your ParentPrefab

1. Open your **ParentPrefab**
2. Add/Update the **VideoAnimControl** component on the root GameObject:
   - Assign **Video Player** reference
   - Assign **Video Plane** (the quad/plane showing the video)
   - Set **Video Height Offset** (recommended: 0.001)
   - Set **Video Scale Multiplier** (1.0 = exact match)

3. Update the **ArButton** component on your button GameObject:
   - Assign **Video Player** reference
   - Ensure it has a **Collider** component for raycasting

### Step 4: Configure XR Reference Image Library

1. Open your **XR Reference Image Library** asset
2. Add all wedding photos you want to track
3. **IMPORTANT:** Set the **physical size** (in meters) for each image
   - Example: A 10cm x 15cm photo = 0.1m x 0.15m
   - This is crucial for accurate video scaling!

4. Name each image to match your mappings (e.g., "Wedding_Photo_1")

### Step 5: Test Your Setup

1. Build and run on your AR device
2. Point camera at a wedding photo
3. Video should appear scaled exactly to the photo size
4. Tap the play/pause button to control playback

---

## Video Scaling Explained

### How It Works

The `VideoAnimControl.ScaleVideoToTrackedImage()` method:

1. Gets the physical size from `ARTrackedImage.referenceImage.size`
2. Calculates scale based on Unity plane default size (10x10 units)
3. Applies scale: `scale = imageSize * multiplier * 0.1`
4. Positions video slightly above image to prevent z-fighting

### Example Calculation

For a wedding photo that's **0.15m x 0.10m** (15cm x 10cm):

```
Physical Size: 0.15m x 0.10m
Scale Multiplier: 1.0
Unity Plane Default: 10 units

Scale X = 0.15 * 1.0 * 0.1 = 0.015
Scale Z = 0.10 * 1.0 * 0.1 = 0.010

Result: Video plane scaled to 0.015 x 0.010 units
        = Exactly 15cm x 10cm in AR space
```

### Adjusting Scale

Use **Video Scale Multiplier** to fine-tune:
- `1.0` = Exact match to image size
- `0.9` = 90% of image size (smaller)
- `1.1` = 110% of image size (larger)

---

## Advanced Usage

### Adding Images at Runtime

```csharp
MultiImageVideoManager manager = GetComponent<MultiImageVideoManager>();
manager.AddImageVideoMapping(
    "NewWeddingPhoto",
    myPrefab,
    "https://example.com/video.mp4"
);
```

### Resetting All Videos

```csharp
MultiImageVideoManager manager = GetComponent<MultiImageVideoManager>();
manager.ResetAllVideos();
```

### Custom Video Scaling

```csharp
VideoAnimControl videoControl = GetComponent<VideoAnimControl>();
videoControl.videoScaleMultiplier = 1.2f; // 120% of image size
videoControl.ScaleVideoToTrackedImage();
```

---

## Troubleshooting

### Video Not Scaling Correctly

**Problem:** Video doesn't match image size

**Solutions:**
1. Verify physical size is set in XR Reference Image Library
2. Check that `videoPlane` is assigned in VideoAnimControl
3. Ensure plane is a child of the tracked image prefab
4. Adjust `videoScaleMultiplier` if needed

### Multiple Videos Not Working

**Problem:** Only one video plays

**Solutions:**
1. Verify each image has unique name in Reference Image Library
2. Check that image names match in MultiImageVideoManager mappings
3. Ensure each mapping has a prefab assigned (or default is set)
4. Check console for mapping validation warnings

### Button Not Responding

**Problem:** Play/pause button doesn't work

**Solutions:**
1. Verify button GameObject has a Collider component
2. Check that VideoPlayer reference is assigned in ArButton
3. Ensure Main Camera has the "MainCamera" tag
4. Test with mouse in Unity Editor first

### Video Position Issues

**Problem:** Video appears in wrong location

**Solutions:**
1. Ensure video plane is a child of the tracked image
2. Check `videoHeightOffset` value (try 0.001 to 0.01)
3. Verify prefab hierarchy matches expected structure
4. Check that tracked image transform is being updated

---

## Best Practices

### 1. Image Quality
- Use high-quality, high-contrast wedding photos
- Avoid reflective or glossy surfaces
- Ensure good lighting when tracking

### 2. Physical Size Accuracy
- Measure actual photo dimensions precisely
- Set exact measurements in Reference Image Library
- This ensures perfect video scaling

### 3. Video Optimization
- Use compressed video formats (H.264)
- Keep video resolution reasonable for mobile (1080p max)
- Consider using streaming URLs for large files

### 4. Performance
- Limit active tracked images (5-10 max recommended)
- Use `disableOnImageLost = true` to save resources
- Avoid very high-resolution videos on lower-end devices

### 5. User Experience
- Provide visual feedback when image is detected
- Add loading indicators for video streaming
- Include instructions for users on how to interact

---

## Migration from Old Scripts

If you're upgrading from the original scripts:

### Changes in ArButton.cs
- ✅ Touch input support added
- ✅ Better error handling
- ✅ Public method to set video player
- ⚠️ `pause` variable renamed to `isPlaying` (inverted logic)

### Changes in VideoAnimControl.cs
- ✅ Automatic scaling to tracked image size
- ✅ Component auto-discovery
- ✅ Public methods for external control
- ⚠️ Now requires ARTrackedImage reference
- ⚠️ `tocouAnim` renamed to `hasPlayedEndAnimation`

### New Requirements
- Add MultiImageVideoManager to AR Session Origin
- Set physical sizes in XR Reference Image Library
- Configure image-video mappings

---

## Example Scene Hierarchy

```
AR Session Origin
├── MultiImageVideoManager (component)
├── ARTrackedImageManager (component)
└── AR Camera

ParentPrefab (instantiated per tracked image)
├── VideoAnimControl (component)
├── Animator (component)
├── VideoPlane (Transform)
│   └── VideoPlayer (component)
└── PlayPauseButton
    ├── ArButton (component)
    ├── SphereCollider (component)
    └── Animator (component)
```

---

## Support

For issues or questions:
1. Check console logs for detailed error messages
2. Verify all component references are assigned
3. Test in Unity Editor with AR Foundation Remote
4. Review this documentation for configuration steps

---

## Future Enhancements

Potential improvements for the system:
- [ ] Video clip preloading for offline use
- [ ] Gesture controls (pinch to zoom, swipe to skip)
- [ ] Audio controls separate from video
- [ ] Multiple videos per image (playlist)
- [ ] Analytics tracking for video views
- [ ] Social sharing integration

---

**Version:** 2.0  
**Last Updated:** January 2026  
**Compatible with:** Unity 2021.2.18f or later, AR Foundation 4.x+
