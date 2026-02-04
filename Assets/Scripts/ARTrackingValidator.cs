using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Utility script to help configure and validate AR tracking setup.
/// Attach to AR Session Origin and run in editor or on device.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARTrackingValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [Tooltip("Run validation on start")]
    public bool validateOnStart = true;
    
    [Tooltip("Show warnings in console")]
    public bool showWarnings = true;
    
    [Tooltip("Show info messages in console")]
    public bool showInfo = true;
    
    private ARTrackedImageManager imageManager;
    private ImageVideoManager videoManager;
    
    void Start()
    {
        if (validateOnStart)
        {
            ValidateSetup();
        }
    }
    
    /// <summary>
    /// Validates the entire AR tracking setup
    /// </summary>
    [ContextMenu("Validate AR Setup")]
    public void ValidateSetup()
    {
        LogInfo("=== AR TRACKING VALIDATION ===\n");
        
        ValidateARComponents();
        ValidateImageLibrary();
        ValidateVideoManager();
        ValidatePrefabs();
        ValidatePerformanceSettings();
        
        LogInfo("\n=== VALIDATION COMPLETE ===");
    }
    
    /// <summary>
    /// Validates AR Foundation components
    /// </summary>
    private void ValidateARComponents()
    {
        LogInfo("Checking AR Components...");
        
        // Check AR Session
        ARSession session = FindObjectOfType<ARSession>();
        if (session == null)
        {
            LogWarning("ARSession not found in scene!");
        }
        else
        {
            LogInfo("✓ ARSession found");
        }
        
        // Check AR Session Origin
        ARSessionOrigin origin = FindObjectOfType<ARSessionOrigin>();
        if (origin == null)
        {
            LogWarning("ARSessionOrigin not found in scene!");
        }
        else
        {
            LogInfo("✓ ARSessionOrigin found");
        }
        
        // Check AR Tracked Image Manager
        imageManager = GetComponent<ARTrackedImageManager>();
        if (imageManager == null)
        {
            LogWarning("ARTrackedImageManager not found!");
        }
        else
        {
            LogInfo("✓ ARTrackedImageManager found");
        }
    }
    
    /// <summary>
    /// Validates reference image library
    /// </summary>
    private void ValidateImageLibrary()
    {
        LogInfo("\nChecking Image Library...");
        
        if (imageManager == null) return;
        
        var library = imageManager.referenceLibrary;
        if (library == null)
        {
            LogWarning("No reference image library assigned!");
            return;
        }
        
        int imageCount = library.count;
        LogInfo($"✓ Library contains {imageCount} images");
        
        if (imageCount == 0)
        {
            LogWarning("Image library is empty!");
        }
        else if (imageCount > 10)
        {
            LogWarning($"Large library ({imageCount} images) may impact performance. Consider splitting.");
        }
        
        // Check individual images
        for (int i = 0; i < imageCount; i++)
        {
            var refImage = library[i];
            
            // Check size
            if (refImage.size.x <= 0 || refImage.size.y <= 0)
            {
                LogWarning($"Image '{refImage.name}' has invalid size: {refImage.size}");
            }
            
            // Check texture
            if (refImage.texture == null)
            {
                LogWarning($"Image '{refImage.name}' has no texture assigned!");
            }
            else
            {
                // Check texture resolution
                int width = refImage.texture.width;
                int height = refImage.texture.height;
                
                if (width < 256 || height < 256)
                {
                    LogWarning($"Image '{refImage.name}' resolution is low ({width}x{height}). Recommend 512x512 minimum.");
                }
                else if (width > 2048 || height > 2048)
                {
                    LogWarning($"Image '{refImage.name}' resolution is very high ({width}x{height}). May impact performance.");
                }
                else
                {
                    LogInfo($"  ✓ {refImage.name}: {width}x{height}, size: {refImage.size.x:F2}m x {refImage.size.y:F2}m");
                }
            }
        }
    }
    
    /// <summary>
    /// Validates video manager setup
    /// </summary>
    private void ValidateVideoManager()
    {
        LogInfo("\nChecking Video Manager...");
        
        videoManager = GetComponent<ImageVideoManager>();
        
        if (videoManager == null)
        {
            // Check for old manager
            var oldManager = GetComponent<MultiImageVideoManager>();
            if (oldManager != null)
            {
                LogWarning("Using old MultiImageVideoManager. Consider upgrading to ImageVideoManager.");
            }
            else
            {
                LogWarning("No video manager found!");
            }
            return;
        }
        
        LogInfo("✓ ImageVideoManager found");
        
        // Check mappings
        if (videoManager.imageVideoMappings == null || videoManager.imageVideoMappings.Count == 0)
        {
            LogWarning("No image-video mappings configured!");
        }
        else
        {
            LogInfo($"✓ {videoManager.imageVideoMappings.Count} image-video mappings configured");
            
            // Validate each mapping
            foreach (var mapping in videoManager.imageVideoMappings)
            {
                if (string.IsNullOrEmpty(mapping.imageName))
                {
                    LogWarning("Found mapping with empty image name!");
                }
                
                if (mapping.videoPrefab == null && videoManager.defaultVideoPrefab == null)
                {
                    LogWarning($"Mapping '{mapping.imageName}' has no prefab and no default prefab set!");
                }
                
                if (string.IsNullOrEmpty(mapping.videoSource))
                {
                    LogWarning($"Mapping '{mapping.imageName}' has no video source!");
                }
            }
        }
        
        // Check default prefab
        if (videoManager.defaultVideoPrefab == null)
        {
            LogWarning("No default video prefab assigned!");
        }
    }
    
    /// <summary>
    /// Validates prefab configuration
    /// </summary>
    private void ValidatePrefabs()
    {
        LogInfo("\nChecking Prefabs...");
        
        if (videoManager == null) return;
        
        GameObject prefabToCheck = videoManager.defaultVideoPrefab;
        if (prefabToCheck == null && videoManager.imageVideoMappings.Count > 0)
        {
            prefabToCheck = videoManager.imageVideoMappings[0].videoPrefab;
        }
        
        if (prefabToCheck == null)
        {
            LogWarning("No prefab to validate!");
            return;
        }
        
        // Check for required components
        var videoControl = prefabToCheck.GetComponent<VideoController>();
        if (videoControl == null)
        {
            var oldControl = prefabToCheck.GetComponent<VideoAnimControl>();
            if (oldControl != null)
            {
                LogWarning("Prefab uses old VideoAnimControl. Consider upgrading to VideoController.");
            }
            else
            {
                LogWarning("Prefab missing video control component!");
            }
        }
        else
        {
            LogInfo("✓ Prefab has VideoController");
            
            // Check video control configuration
            if (videoControl.videoPlayer == null)
            {
                LogWarning("VideoControl has no VideoPlayer assigned!");
            }
            
            if (videoControl.videoPlane == null)
            {
                LogWarning("VideoControl has no VideoPlane assigned!");
            }
        }
        
        // Check for video player
        var videoPlayer = prefabToCheck.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
        if (videoPlayer == null)
        {
            LogWarning("Prefab has no VideoPlayer component!");
        }
        else
        {
            LogInfo("✓ Prefab has VideoPlayer");
        }
        
        // Check for button
        var button = prefabToCheck.GetComponentInChildren<ARButton>();
        if (button == null)
        {
            var oldButton = prefabToCheck.GetComponentInChildren<ArButton>();
            if (oldButton != null)
            {
                LogWarning("Prefab uses old ArButton. Consider upgrading to ARButton.");
            }
        }
        else
        {
            LogInfo("✓ Prefab has ARButton");
            
            if (button.videoPlayer == null)
            {
                LogWarning("Button has no VideoPlayer reference!");
            }
        }
    }
    
    /// <summary>
    /// Validates performance settings
    /// </summary>
    private void ValidatePerformanceSettings()
    {
        LogInfo("\nChecking Performance Settings...");
        
        if (videoManager == null) return;
        
        // Check object pooling
        if (!videoManager.useObjectPooling)
        {
            LogWarning("Object pooling is disabled. Enable for better performance.");
        }
        else
        {
            LogInfo($"✓ Object pooling enabled (max: {videoManager.maxPoolSize})");
        }
        
        // Check grace period
        if (videoManager.lostImageGracePeriod < 0.2f)
        {
            LogWarning($"Grace period is very short ({videoManager.lostImageGracePeriod}s). May cause flickering.");
        }
        else if (videoManager.lostImageGracePeriod > 1.0f)
        {
            LogWarning($"Grace period is very long ({videoManager.lostImageGracePeriod}s). May feel unresponsive.");
        }
        else
        {
            LogInfo($"✓ Grace period: {videoManager.lostImageGracePeriod}s");
        }
        
        // Check video control settings
        GameObject prefab = videoManager.defaultVideoPrefab;
        if (prefab != null)
        {
            var videoControl = prefab.GetComponent<VideoController>();
            if (videoControl != null)
            {
                if (!videoControl.preloadVideo)
                {
                    LogInfo("Video preloading disabled. Videos will have loading delay.");
                }
                
                if (videoControl.scaleCheckInterval < 0.3f)
                {
                    LogWarning($"Scale check interval is very frequent ({videoControl.scaleCheckInterval}s). May impact performance.");
                }
                
                if (!videoControl.pauseWhenInvisible)
                {
                    LogInfo("Pause when invisible is disabled. May impact battery life.");
                }
            }
        }
    }
    
    /// <summary>
    /// Logs an info message
    /// </summary>
    private void LogInfo(string message)
    {
        if (showInfo)
        {
            Debug.Log($"[ARValidator] {message}");
        }
    }
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    private void LogWarning(string message)
    {
        if (showWarnings)
        {
            Debug.LogWarning($"[ARValidator] {message}");
        }
    }
    
    /// <summary>
    /// Gets a summary of the setup
    /// </summary>
    public string GetSetupSummary()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("AR TRACKING SETUP SUMMARY");
        sb.AppendLine("========================");
        
        // AR Components
        sb.AppendLine($"AR Session: {(FindObjectOfType<ARSession>() != null ? "✓" : "✗")}");
        sb.AppendLine($"AR Session Origin: {(FindObjectOfType<ARSessionOrigin>() != null ? "✓" : "✗")}");
        sb.AppendLine($"AR Image Manager: {(GetComponent<ARTrackedImageManager>() != null ? "✓" : "✗")}");
        
        // Image Library
        var manager = GetComponent<ARTrackedImageManager>();
        if (manager != null && manager.referenceLibrary != null)
        {
            sb.AppendLine($"Image Library: {manager.referenceLibrary.count} images");
        }
        
        // Video Manager
        var vidManager = GetComponent<ImageVideoManager>();
        if (vidManager != null)
        {
            sb.AppendLine($"Video Mappings: {vidManager.imageVideoMappings.Count}");
            sb.AppendLine($"Object Pooling: {(vidManager.useObjectPooling ? "Enabled" : "Disabled")}");
        }
        
        return sb.ToString();
    }
}
