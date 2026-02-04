# Quick Setup Checklist - Enhanced AR Tracking

## ✅ Pre-Implementation Checklist

### 1. Backup Current Project
- [ ] Create a backup of your entire project
- [ ] Export current scene as a package
- [ ] Commit current state to version control (if using Git)

### 2. Review Current Setup
- [ ] Note current `MultiImageVideoManager` settings
- [ ] Note current `VideoAnimControl` settings
- [ ] Note current `ArButton` settings
- [ ] Document any custom modifications

## 🚀 Implementation Steps

### Step 1: Add Enhanced Scripts to Project
The enhanced scripts are already in your `Assets/Scripts/` folder:
- [x] `EnhancedMultiImageVideoManager.cs`
- [x] `EnhancedVideoAnimControl.cs`
- [x] `EnhancedArButton.cs`

### Step 2: Update AR Session Origin
1. [ ] Open your main AR scene
2. [ ] Find the `AR Session Origin` GameObject
3. [ ] Add `EnhancedMultiImageVideoManager` component
4. [ ] Configure settings:
   ```
   Object Pooling: ✓ Enabled
   Max Pool Size: 5
   Lost Image Grace Period: 0.3s
   Debug Mode: ✓ Enabled (for testing)
   ```
5. [ ] Copy image-video mappings from old component
6. [ ] Disable (don't delete yet) old `MultiImageVideoManager`

### Step 3: Update ParentPrefab
1. [ ] Open `ParentPrefab` in prefab edit mode
2. [ ] Find the root GameObject with `VideoAnimControl`
3. [ ] Add `EnhancedVideoAnimControl` component
4. [ ] Configure settings:
   ```
   Preload Video: ✓ Enabled
   Scale Check Interval: 0.5s
   Pause When Invisible: ✓ Enabled
   ```
5. [ ] Copy references from old component:
   - Video Player
   - Video Plane
   - Tracked Image (if set)
6. [ ] Disable old `VideoAnimControl`

### Step 4: Update Play Button
1. [ ] Still in `ParentPrefab`, find the button GameObject
2. [ ] Add `EnhancedArButton` component
3. [ ] Configure settings:
   ```
   Debounce Time: 0.3s
   Enable Haptic Feedback: ✓ Enabled
   Pressed Scale: 0.9
   ```
4. [ ] Copy Video Player reference from old component
5. [ ] Disable old `ArButton`

### Step 5: Test in Editor
1. [ ] Save all changes
2. [ ] Enter Play Mode
3. [ ] Check console for initialization messages
4. [ ] Verify no errors appear

### Step 6: Build and Test on Device
1. [ ] Build for your target platform (iOS/Android)
2. [ ] Install on test device
3. [ ] Test with physical images
4. [ ] Monitor performance and tracking quality

### Step 7: Fine-Tune Settings
Based on testing results:

#### If tracking is unstable:
- [ ] Increase `lostImageGracePeriod` to 0.5s
- [ ] Lower `minimumTrackingQuality` to `Poor`

#### If performance is poor:
- [ ] Increase `scaleCheckInterval` to 1.0s
- [ ] Enable `pauseWhenInvisible`
- [ ] Reduce `maxPoolSize` if memory is limited

#### If videos load slowly:
- [ ] Ensure `preloadVideo` is enabled
- [ ] Check video file sizes (should be < 50MB)
- [ ] Consider lowering video resolution

### Step 8: Production Deployment
1. [ ] Disable debug mode:
   ```csharp
   enhancedManager.debugMode = false;
   enhancedManager.showTrackingQuality = false;
   ```
2. [ ] Remove old components completely
3. [ ] Clean up unused scripts
4. [ ] Final build and test

## 📊 Testing Checklist

### Functional Testing
- [ ] Single image tracking works
- [ ] Multiple images tracked simultaneously
- [ ] Video plays when image detected
- [ ] Video pauses when image lost
- [ ] Play/pause button works
- [ ] Button has visual feedback
- [ ] Haptic feedback works (on device)
- [ ] Video scales correctly to image size

### Performance Testing
- [ ] Frame rate is stable (30+ FPS)
- [ ] No memory leaks (check Profiler)
- [ ] Videos load quickly
- [ ] No stuttering during playback
- [ ] Smooth tracking (no jitter)

### Edge Case Testing
- [ ] Rapid image detection/loss
- [ ] Multiple rapid button presses
- [ ] Low light conditions
- [ ] Angled viewing
- [ ] Partial image occlusion
- [ ] App backgrounding/foregrounding

## 🐛 Common Issues & Quick Fixes

### Issue: "Component not found" errors
**Fix**: Ensure all references are assigned in Inspector

### Issue: Videos don't play
**Fix**: 
```csharp
// Check in EnhancedVideoAnimControl
preloadVideo = true;
```

### Issue: Button doesn't respond
**Fix**: 
```csharp
// Check button has a Collider component
// Increase maxRaycastDistance = 20f;
```

### Issue: Poor performance
**Fix**:
```csharp
// In EnhancedMultiImageVideoManager
useObjectPooling = true;

// In EnhancedVideoAnimControl
scaleCheckInterval = 1.0f;
pauseWhenInvisible = true;
```

## 📈 Performance Benchmarks

### Target Metrics (on mid-range device)
- Frame Rate: 30+ FPS
- Memory Usage: < 500MB
- Video Load Time: < 1 second
- Tracking Latency: < 100ms
- Button Response: < 50ms

### Monitoring Tools
```csharp
// Add this to a debug UI
void OnGUI()
{
    if (debugMode)
    {
        GUILayout.Label($"FPS: {1.0f / Time.deltaTime:F1}");
        GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / 1048576}MB");
        GUILayout.Label(enhancedManager.GetTrackingStats());
    }
}
```

## 🎯 Optimization Tips

### For Low-End Devices
1. Reduce video resolution to 720p
2. Set `scaleCheckInterval = 1.0f`
3. Set `maxPoolSize = 3`
4. Enable `pauseWhenInvisible = true`

### For High-End Devices
1. Use 1080p videos
2. Set `scaleCheckInterval = 0.2f`
3. Set `maxPoolSize = 10`
4. Enable all visual effects

### For Battery Life
1. Enable `pauseWhenInvisible = true`
2. Increase `lostImageGracePeriod = 0.5f`
3. Set `minimumTrackingQuality = Fair`

## 📝 Configuration Templates

### Wedding Album (3-5 photos)
```csharp
// EnhancedMultiImageVideoManager
useObjectPooling = true;
maxPoolSize = 5;
lostImageGracePeriod = 0.3f;
minimumTrackingQuality = Fair;

// EnhancedVideoAnimControl
preloadVideo = true;
scaleCheckInterval = 0.5f;
pauseWhenInvisible = true;
audioFadeTime = 2.0f;

// EnhancedArButton
debounceTime = 0.3f;
enableHapticFeedback = true;
```

### Large Event (10+ photos)
```csharp
// EnhancedMultiImageVideoManager
useObjectPooling = true;
maxPoolSize = 10;
lostImageGracePeriod = 0.2f;
minimumTrackingQuality = Good;

// EnhancedVideoAnimControl
preloadVideo = false; // Save memory
scaleCheckInterval = 1.0f;
pauseWhenInvisible = true;
audioFadeTime = 1.0f;

// EnhancedArButton
debounceTime = 0.5f;
enableHapticFeedback = true;
```

### Demo/Exhibition
```csharp
// EnhancedMultiImageVideoManager
useObjectPooling = true;
maxPoolSize = 3;
lostImageGracePeriod = 0.5f; // More forgiving
minimumTrackingQuality = Poor; // Keep playing

// EnhancedVideoAnimControl
preloadVideo = true;
scaleCheckInterval = 0.3f;
pauseWhenInvisible = false; // Always play
audioFadeTime = 1.0f;

// EnhancedArButton
debounceTime = 0.2f;
enableHapticFeedback = true;
```

## ✅ Final Verification

Before releasing to production:

- [ ] All tests passed
- [ ] Performance meets targets
- [ ] No console errors or warnings
- [ ] Debug mode disabled
- [ ] Old components removed
- [ ] Documentation updated
- [ ] Team trained on new features

---

**Estimated Setup Time**: 30-45 minutes
**Recommended Testing Time**: 2-3 hours
**Total Implementation Time**: 3-4 hours

Good luck! 🚀
