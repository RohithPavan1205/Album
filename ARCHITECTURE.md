# Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AR Session Origin                         │
│  ┌────────────────────────────────────────────────────┐     │
│  │         MultiImageVideoManager                      │     │
│  │  • Manages all tracked images                       │     │
│  │  • Maps images to video content                     │     │
│  │  • Instantiates prefabs dynamically                 │     │
│  └────────────────────────────────────────────────────┘     │
│  ┌────────────────────────────────────────────────────┐     │
│  │         ARTrackedImageManager                       │     │
│  │  • Detects tracked images                           │     │
│  │  • Provides tracking state                          │     │
│  │  • References XR Image Library                      │     │
│  └────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Instantiates per tracked image
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    ParentPrefab Instance                     │
│                   (One per wedding photo)                    │
│  ┌────────────────────────────────────────────────────┐     │
│  │         VideoAnimControl                            │     │
│  │  • Scales video to image dimensions                 │     │
│  │  • Monitors video playback                          │     │
│  │  • Handles end animations                           │     │
│  │  • Updates on image size changes                    │     │
│  └────────────────────────────────────────────────────┘     │
│                            │                                 │
│         ┌──────────────────┴──────────────────┐             │
│         ▼                                      ▼             │
│  ┌─────────────┐                      ┌─────────────┐       │
│  │ Video Plane │                      │ Play Button │       │
│  │  • Displays │                      │  ┌────────┐ │       │
│  │    video    │                      │  │ArButton│ │       │
│  │  • Auto-    │                      │  │ • Touch│ │       │
│  │    scaled   │                      │  │ • Mouse│ │       │
│  │             │                      │  │ • Pause│ │       │
│  │ ┌─────────┐ │                      │  └────────┘ │       │
│  │ │VideoPlay│ │                      │             │       │
│  │ │   er    │ │                      │             │       │
│  │ └─────────┘ │                      │             │       │
│  └─────────────┘                      └─────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow

### 1. Image Detection Flow
```
User points camera at wedding photo
            ↓
ARTrackedImageManager detects image
            ↓
MultiImageVideoManager receives event
            ↓
Looks up image name in mappings
            ↓
Instantiates ParentPrefab
            ↓
Sets video source for this image
            ↓
VideoAnimControl scales video to image size
```

### 2. Video Scaling Flow
```
VideoAnimControl.Start()
            ↓
Gets ARTrackedImage reference
            ↓
Reads referenceImage.size (physical dimensions)
            ↓
Calculates scale: size * multiplier * 0.1
            ↓
Applies scale to video plane
            ↓
Positions video at heightOffset above image
            ↓
Monitors for size changes in Update()
```

### 3. Button Interaction Flow
```
User taps screen
            ↓
ArButton.HandleInput() detects touch
            ↓
Raycasts from touch position
            ↓
Checks if button was hit
            ↓
Toggles isPlaying state
            ↓
Calls VideoPlayer.Play() or Pause()
            ↓
Plays animation (btn_Play or btn_Pause)
```

## Component Relationships

```
MultiImageVideoManager
    ├── Manages → ARTrackedImageManager
    ├── Creates → ParentPrefab instances
    └── Configures → VideoAnimControl

VideoAnimControl
    ├── Reads from → ARTrackedImage
    ├── Controls → VideoPlayer
    └── Scales → Video Plane

ArButton
    ├── Controls → VideoPlayer
    └── Updates → Animator
```

## File Structure

```
Live-Album-main/
├── Assets/
│   ├── Scripts/
│   │   ├── ArButton.cs                    [Refactored]
│   │   ├── VideoAnimControl.cs            [Refactored]
│   │   └── MultiImageVideoManager.cs      [New]
│   ├── ParentPrefab.prefab
│   ├── ReferenceImageLibrary.asset
│   └── Scenes/
├── REFACTORING_GUIDE.md                   [New]
├── QUICK_SETUP.md                         [New]
├── ARCHITECTURE.md                        [This file]
└── README.md
```

## Key Design Decisions

### 1. Separation of Concerns
- **MultiImageVideoManager**: Handles multiple images and lifecycle
- **VideoAnimControl**: Handles single video scaling and playback
- **ArButton**: Handles user interaction only

### 2. Automatic Scaling
- Uses physical dimensions from XR Reference Image Library
- Scales in real-time if image size changes
- Configurable multiplier for fine-tuning

### 3. Dynamic Instantiation
- Prefabs created on-demand when images detected
- Reduces memory usage
- Supports unlimited wedding photos

### 4. Flexible Video Sources
- Supports streaming URLs
- Supports local VideoClip assets
- Configurable per image

## Scaling Mathematics

### Unity Plane Default Size
- Unity planes are 10x10 units by default
- To match real-world size, we scale by 0.1

### Calculation
```
Given:
  Physical image size: W x H meters
  Video scale multiplier: M
  Unity plane default: 10 units

Scale:
  scaleX = W * M * 0.1
  scaleZ = H * M * 0.1

Example:
  Image: 0.15m x 0.10m
  Multiplier: 1.0
  
  scaleX = 0.15 * 1.0 * 0.1 = 0.015
  scaleZ = 0.10 * 1.0 * 0.1 = 0.010
  
  Result: Video is exactly 15cm x 10cm in AR
```

## Event Flow Diagram

```
┌──────────────┐
│ AR Camera    │
│ detects      │
│ image        │
└──────┬───────┘
       │
       ▼
┌──────────────────────┐
│ ARTrackedImageManager│
│ fires event          │
└──────┬───────────────┘
       │
       ▼
┌────────────────────────────┐
│ MultiImageVideoManager     │
│ • added event              │
│ • updated event            │
│ • removed event            │
└──────┬─────────────────────┘
       │
       ├─────────────────┬─────────────────┐
       ▼                 ▼                 ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│ Instantiate │   │   Update    │   │  Disable/   │
│   Prefab    │   │  Transform  │   │  Destroy    │
└──────┬──────┘   └─────────────┘   └─────────────┘
       │
       ▼
┌─────────────────────┐
│ VideoAnimControl    │
│ • SetTrackedImage() │
│ • ScaleVideo()      │
└─────────────────────┘
```

## Performance Considerations

### Memory Management
- Prefabs instantiated on-demand
- Option to disable vs destroy on image lost
- Video streaming reduces app size

### Update Loop Optimization
- Only active videos check for end state
- Scale updates only when size changes
- Raycasting only on input events

### Recommended Limits
- **Max tracked images**: 10 simultaneous
- **Video resolution**: 1080p max for mobile
- **Video bitrate**: 5 Mbps max for streaming

## Extension Points

### Adding New Features

1. **Custom Animations**
   - Extend VideoAnimControl
   - Add new animation triggers

2. **Analytics**
   - Subscribe to MultiImageVideoManager events
   - Track image detection and video views

3. **Social Sharing**
   - Add screenshot capture in ArButton
   - Integrate sharing SDK

4. **Gesture Controls**
   - Extend ArButton for pinch/swipe
   - Add volume/seek controls

## Testing Strategy

### Unit Testing
- Test video scaling calculations
- Test image name matching
- Test state transitions

### Integration Testing
- Test multiple images simultaneously
- Test image lost/found scenarios
- Test video source switching

### Device Testing
- Test on various iOS devices
- Test different image sizes
- Test network conditions for streaming

---

**This architecture supports:**
- ✅ Multiple wedding photos
- ✅ Exact physical dimension scaling
- ✅ Dynamic content management
- ✅ Extensible design
- ✅ Performance optimization
