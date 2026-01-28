using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Manages multiple AR tracked images and their associated video content.
/// Automatically instantiates prefabs for each tracked wedding photo and manages their lifecycle.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class MultiImageVideoManager : MonoBehaviour
{
    [System.Serializable]
    public class ImageVideoMapping
    {
        [Tooltip("Name of the image in the Reference Image Library")]
        public string imageName;
        
        [Tooltip("Prefab to instantiate when this image is tracked")]
        public GameObject videoPrefab;
        
        [Tooltip("Video clip or URL for this specific wedding photo")]
        public string videoSource;
        
        [HideInInspector]
        public GameObject instantiatedPrefab;
    }
    
    [Header("Image-Video Mappings")]
    [Tooltip("Map each tracked image to its corresponding video content")]
    public List<ImageVideoMapping> imageVideoMappings = new List<ImageVideoMapping>();
    
    [Header("Default Prefab")]
    [Tooltip("Default prefab to use if no specific mapping is found")]
    public GameObject defaultVideoPrefab;
    
    [Header("Settings")]
    [Tooltip("Destroy prefabs when images are lost from tracking")]
    public bool destroyOnImageLost = false;
    
    [Tooltip("Disable prefabs when images are lost (instead of destroying)")]
    public bool disableOnImageLost = true;
    
    // Runtime State
    private bool isRuntimeMode = false;
    private string runtimeGlobalVideoPath;

    // Internal tracking
    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, ImageVideoMapping> mappingDictionary;
    private Dictionary<TrackingState, string> trackingStateMessages = new Dictionary<TrackingState, string>
    {
        { TrackingState.None, "Not tracking" },
        { TrackingState.Limited, "Limited tracking" },
        { TrackingState.Tracking, "Tracking" }
    };
    
    void Awake()
    {
        // Get the AR Tracked Image Manager
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        
        // Build dictionary for fast lookup
        BuildMappingDictionary();
        
        // Validate mappings
        ValidateMappings();
    }
    
    void OnEnable()
    {
        // Subscribe to tracked image events
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    
    /// <summary>
    /// Builds a dictionary for quick image name lookup
    /// </summary>
    private void BuildMappingDictionary()
    {
        mappingDictionary = new Dictionary<string, ImageVideoMapping>();
        
        foreach (var mapping in imageVideoMappings)
        {
            if (!string.IsNullOrEmpty(mapping.imageName))
            {
                if (!mappingDictionary.ContainsKey(mapping.imageName))
                {
                    mappingDictionary[mapping.imageName] = mapping;
                }
                else
                {
                    Debug.LogWarning($"[MultiImageVideoManager] Duplicate image name found: {mapping.imageName}. Using first occurrence.");
                }
            }
        }
        
        Debug.Log($"[MultiImageVideoManager] Loaded {mappingDictionary.Count} image-video mappings");
    }
    
    /// <summary>
    /// Validates that all mappings have required components
    /// </summary>
    private void ValidateMappings()
    {
        int invalidCount = 0;
        
        foreach (var mapping in imageVideoMappings)
        {
            if (string.IsNullOrEmpty(mapping.imageName))
            {
                Debug.LogWarning("[MultiImageVideoManager] Found mapping with empty image name");
                invalidCount++;
            }
            
            if (mapping.videoPrefab == null && defaultVideoPrefab == null)
            {
                Debug.LogWarning($"[MultiImageVideoManager] No prefab assigned for image '{mapping.imageName}' and no default prefab set");
                invalidCount++;
            }
        }
        
        if (invalidCount > 0)
        {
            Debug.LogWarning($"[MultiImageVideoManager] Found {invalidCount} invalid mappings. Please review configuration.");
        }
    }
    
    /// <summary>
    /// Called when tracked images are added, updated, or removed
    /// </summary>
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Handle newly detected images
        foreach (var trackedImage in eventArgs.added)
        {
            HandleTrackedImageAdded(trackedImage);
        }
        
        // Handle updated images (position/rotation changes)
        foreach (var trackedImage in eventArgs.updated)
        {
            HandleTrackedImageUpdated(trackedImage);
        }
        
        // Handle removed images
        foreach (var trackedImage in eventArgs.removed)
        {
            HandleTrackedImageRemoved(trackedImage);
        }
    }
    
    /// <summary>
    /// Handles a newly detected tracked image
    /// </summary>
    private void HandleTrackedImageAdded(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        
        Debug.Log($"[MultiImageVideoManager] Image detected: {imageName}");
        
        // Haptic Feedback
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        Debug.Log($"[MultiImageVideoManager] Image detected: {imageName}");
        
        // Get the mapping for this image
        ImageVideoMapping mapping = GetMappingForImage(imageName);
        
        // Runtime Callback: If we are in runtime mode and found no specific mapping, create one dynamically
        if (mapping == null && isRuntimeMode)
        {
            Debug.Log($"[MultiImageVideoManager] Runtime mode: creating dynamic mapping for {imageName}");
            mapping = new ImageVideoMapping
            {
                imageName = imageName,
                videoPrefab = defaultVideoPrefab,
                videoSource = runtimeGlobalVideoPath
            };
            
            // Allow this to be looked up later
            if (!mappingDictionary.ContainsKey(imageName))
            {
               mappingDictionary.Add(imageName, mapping);
            }
        }
        
        if (mapping != null)
        {
            // Instantiate the prefab if not already created
            if (mapping.instantiatedPrefab == null)
            {
                InstantiatePrefabForImage(trackedImage, mapping);
            }
            else
            {
                // Re-enable if it was disabled
                mapping.instantiatedPrefab.SetActive(true);
                UpdatePrefabTransform(trackedImage, mapping.instantiatedPrefab);
            }
        }
        else
        {
            Debug.LogWarning($"[MultiImageVideoManager] No mapping found for image: {imageName}");
        }
    }
    
    /// <summary>
    /// Handles updates to a tracked image (position, rotation, tracking state)
    /// </summary>
    private void HandleTrackedImageUpdated(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        ImageVideoMapping mapping = GetMappingForImage(imageName);
        
        if (mapping != null && mapping.instantiatedPrefab != null)
        {
            // Update transform
            UpdatePrefabTransform(trackedImage, mapping.instantiatedPrefab);
            
            // Handle tracking state changes
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                mapping.instantiatedPrefab.SetActive(true);
            }
            else if (trackedImage.trackingState == TrackingState.None || 
                     trackedImage.trackingState == TrackingState.Limited)
            {
                if (disableOnImageLost)
                {
                    mapping.instantiatedPrefab.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Handles a tracked image being removed from tracking
    /// </summary>
    private void HandleTrackedImageRemoved(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        Debug.Log($"[MultiImageVideoManager] Image lost: {imageName}");
        
        ImageVideoMapping mapping = GetMappingForImage(imageName);
        
        if (mapping != null && mapping.instantiatedPrefab != null)
        {
            if (destroyOnImageLost)
            {
                Destroy(mapping.instantiatedPrefab);
                mapping.instantiatedPrefab = null;
            }
            else if (disableOnImageLost)
            {
                mapping.instantiatedPrefab.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Gets the mapping for a given image name
    /// </summary>
    private ImageVideoMapping GetMappingForImage(string imageName)
    {
        if (mappingDictionary.TryGetValue(imageName, out ImageVideoMapping mapping))
        {
            return mapping;
        }
        return null;
    }
    
    /// <summary>
    /// Instantiates a prefab for the tracked image
    /// </summary>
    private void InstantiatePrefabForImage(ARTrackedImage trackedImage, ImageVideoMapping mapping)
    {
        // Determine which prefab to use
        GameObject prefabToInstantiate = mapping.videoPrefab != null ? mapping.videoPrefab : defaultVideoPrefab;
        
        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[MultiImageVideoManager] No prefab available for image: {mapping.imageName}");
            return;
        }
        
        // Instantiate the prefab
        GameObject instance = Instantiate(prefabToInstantiate, trackedImage.transform);
        mapping.instantiatedPrefab = instance;
        
        // Set the video source if specified
        if (!string.IsNullOrEmpty(mapping.videoSource))
        {
            SetVideoSource(instance, mapping.videoSource);
        }
        
        // Configure the VideoAnimControl component with tracked image reference
        VideoAnimControl videoControl = instance.GetComponent<VideoAnimControl>();
        if (videoControl != null)
        {
            videoControl.SetTrackedImage(trackedImage);
        }
        
        Debug.Log($"[MultiImageVideoManager] Instantiated prefab for image: {mapping.imageName}");
    }
    
    /// <summary>
    /// Updates the prefab's transform to match the tracked image
    /// </summary>
    private void UpdatePrefabTransform(ARTrackedImage trackedImage, GameObject prefab)
    {
        prefab.transform.position = trackedImage.transform.position;
        prefab.transform.rotation = trackedImage.transform.rotation;
    }
    
    /// <summary>
    /// Sets the video source (URL or file path) for the instantiated prefab
    /// </summary>
    private void SetVideoSource(GameObject instance, string videoSource)
    {
        UnityEngine.Video.VideoPlayer videoPlayer = instance.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
        
        if (videoPlayer != null)
        {
            // Check if it's a URL or local file
            // Note: VideoPlayer.url supports local file paths (e.g. "file:///..." or absolute paths)
            bool isPathOrUrl = videoSource.Contains("://") || videoSource.Contains("/") || videoSource.Contains("\\");

            if (isPathOrUrl)
            {
                videoPlayer.source = UnityEngine.Video.VideoSource.Url;
                videoPlayer.url = videoSource;
            }
            else
            {
                // Assume it's a VideoClip asset only if it doesn't look like a path
                videoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
                // Note: You'll need to load the VideoClip from Resources or assign it in the inspector
                Debug.LogWarning($"[MultiImageVideoManager] Local video file support requires VideoClip assignment: {videoSource}");
            }
        }
        else
        {
            Debug.LogWarning($"[MultiImageVideoManager] No VideoPlayer found in prefab for video source: {videoSource}");
        }
    }
    
    /// <summary>
    /// Public method to add a new image-video mapping at runtime
    /// </summary>
    public void AddImageVideoMapping(string imageName, GameObject prefab, string videoSource)
    {
        ImageVideoMapping newMapping = new ImageVideoMapping
        {
            imageName = imageName,
            videoPrefab = prefab,
            videoSource = videoSource
        };
        
        imageVideoMappings.Add(newMapping);
        mappingDictionary[imageName] = newMapping;
        
        Debug.Log($"[MultiImageVideoManager] Added new mapping for image: {imageName}");
    }
    
    /// <summary>
    /// Resets all instantiated prefabs
    /// </summary>
    public void ResetAllVideos()
    {
        foreach (var mapping in imageVideoMappings)
        {
            if (mapping.instantiatedPrefab != null)
            {
                VideoAnimControl videoControl = mapping.instantiatedPrefab.GetComponent<VideoAnimControl>();
                if (videoControl != null)
                {
                    videoControl.ResetVideo();
                }
            }
        }
    }

    /// <summary>
    /// Initializes the manager with a runtime-generated library and a global video path.
    /// This restarts the ARTrackedImageManager.
    /// </summary>
    /// <param name="library">The mutable runtime library to use.</param>
    /// <param name="globalVideoPath">The path/URL to the video to play for all images.</param>
    public void InitializeRuntime(MutableRuntimeReferenceImageLibrary library, string globalVideoPath)
    {
        if (trackedImageManager == null) trackedImageManager = GetComponent<ARTrackedImageManager>();

        Debug.Log($"[MultiImageVideoManager] Initializing Runtime with {(library != null ? library.count.ToString() : "NULL (Editor)")} images and video: {globalVideoPath}");

        isRuntimeMode = true;
        runtimeGlobalVideoPath = globalVideoPath;

        // 1. Assign the new library
        if (library != null)
        {
            trackedImageManager.referenceLibrary = library;
        }
        else
        {
             // Editor Mode: Just warn and proceed so we don't crash the loop
             Debug.LogWarning("[MultiImageVideoManager] Library is null (expected in Editor without XR Sim). Skipping assignment.");
        }
        
        // 2. Clear existing mappings to avoid conflicts, or keep them if you want hybrid behavior
        // For this specific request ("Instead of using preloaded..."), we prioritize the runtime flow.
        // We don't necessarily clear mappingDictionary because we might want to keep the 'runtime' mappings we create.
        
        // 3. Restart the subsystem to pick up the new library
        // Note: In some ARFoundation versions, you need to disable/enable the manager or the session.
        // Toggling the manager is the safest bit-compatible way across versions.
        trackedImageManager.enabled = false;
        trackedImageManager.enabled = true;

        Debug.Log("[MultiImageVideoManager] ARTrackedImageManager restarted with runtime library.");
    }
}
