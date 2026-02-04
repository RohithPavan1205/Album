using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// ENHANCED: Manages multiple AR tracked images with improved performance, stability, and memory management.
/// Key improvements:
/// - Object pooling for prefabs
/// - Tracking quality monitoring
/// - Performance optimizations
/// - Better state management
/// - Enhanced debugging tools
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageVideoManager : MonoBehaviour
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
        
        [HideInInspector]
        public TrackingQuality trackingQuality = TrackingQuality.Unknown;
        
        [HideInInspector]
        public float lastSeenTime;
    }
    
    public enum TrackingQuality
    {
        Unknown,
        Poor,
        Fair,
        Good,
        Excellent
    }
    
    [Header("Image-Video Mappings")]
    [Tooltip("Map each tracked image to its corresponding video content")]
    public List<ImageVideoMapping> imageVideoMappings = new List<ImageVideoMapping>();
    
    [Header("Default Prefab")]
    [Tooltip("Default prefab to use if no specific mapping is found")]
    public GameObject defaultVideoPrefab;
    
    [Header("Performance Settings")]
    [Tooltip("Use object pooling for better performance")]
    public bool useObjectPooling = true;
    
    [Tooltip("Maximum number of pooled objects per prefab type")]
    public int maxPoolSize = 5;
    
    [Tooltip("Minimum time (seconds) before re-instantiating a recently destroyed prefab")]
    public float prefabCooldownTime = 0.5f;
    
    [Header("Tracking Settings")]
    [Tooltip("Destroy prefabs when images are lost from tracking")]
    public bool destroyOnImageLost = false;
    
    [Tooltip("Disable prefabs when images are lost (instead of destroying)")]
    public bool disableOnImageLost = true;
    
    [Tooltip("Time (seconds) to wait before disabling a lost image")]
    public float lostImageGracePeriod = 0.3f;
    
    [Tooltip("Minimum tracking quality to keep video playing")]
    public TrackingQuality minimumTrackingQuality = TrackingQuality.Fair;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool debugMode = false;
    
    [Tooltip("Show tracking quality indicators")]
    public bool showTrackingQuality = true;
    
    // Events
    public event Action<string, TrackingQuality> OnTrackingQualityChanged;
    public event Action<string> OnImageDetected;
    public event Action<string> OnImageLost;
    
    // Runtime State
    private bool isRuntimeMode = false;
    private string runtimeGlobalVideoPath;

    // Internal tracking
    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, ImageVideoMapping> mappingDictionary;
    private Dictionary<GameObject, Queue<GameObject>> objectPools;
    private Dictionary<string, Coroutine> lostImageCoroutines;
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
        
        // Initialize collections
        mappingDictionary = new Dictionary<string, ImageVideoMapping>();
        objectPools = new Dictionary<GameObject, Queue<GameObject>>();
        lostImageCoroutines = new Dictionary<string, Coroutine>();
        
        // Build dictionary for fast lookup
        BuildMappingDictionary();
        
        // Validate mappings
        ValidateMappings();
        
        // Initialize object pools if enabled
        if (useObjectPooling)
        {
            InitializeObjectPools();
        }
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
        mappingDictionary.Clear();
        
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
                    Debug.LogWarning($"[EnhancedManager] Duplicate image name found: {mapping.imageName}. Using first occurrence.");
                }
            }
        }
        
        LogDebug($"Loaded {mappingDictionary.Count} image-video mappings");
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
                Debug.LogWarning("[EnhancedManager] Found mapping with empty image name");
                invalidCount++;
            }
            
            if (mapping.videoPrefab == null && defaultVideoPrefab == null)
            {
                Debug.LogWarning($"[EnhancedManager] No prefab assigned for image '{mapping.imageName}' and no default prefab set");
                invalidCount++;
            }
        }
        
        if (invalidCount > 0)
        {
            Debug.LogWarning($"[EnhancedManager] Found {invalidCount} invalid mappings. Please review configuration.");
        }
    }
    
    /// <summary>
    /// Initializes object pools for better performance
    /// </summary>
    private void InitializeObjectPools()
    {
        if (!useObjectPooling) return;
        
        HashSet<GameObject> uniquePrefabs = new HashSet<GameObject>();
        
        // Collect unique prefabs
        foreach (var mapping in imageVideoMappings)
        {
            if (mapping.videoPrefab != null)
            {
                uniquePrefabs.Add(mapping.videoPrefab);
            }
        }
        
        if (defaultVideoPrefab != null)
        {
            uniquePrefabs.Add(defaultVideoPrefab);
        }
        
        // Create pools
        foreach (var prefab in uniquePrefabs)
        {
            objectPools[prefab] = new Queue<GameObject>();
        }
        
        LogDebug($"Initialized object pools for {objectPools.Count} prefab types");
    }
    
    /// <summary>
    /// Gets a prefab from the pool or creates a new one
    /// </summary>
    private GameObject GetPrefabFromPool(GameObject prefab, Transform parent)
    {
        if (!useObjectPooling || !objectPools.ContainsKey(prefab))
        {
            return Instantiate(prefab, parent);
        }
        
        Queue<GameObject> pool = objectPools[prefab];
        
        // Try to get from pool
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.SetActive(true);
                return obj;
            }
        }
        
        // Pool is empty, create new
        return Instantiate(prefab, parent);
    }
    
    /// <summary>
    /// Returns a prefab to the pool
    /// </summary>
    private void ReturnPrefabToPool(GameObject prefab, GameObject instance)
    {
        if (!useObjectPooling || !objectPools.ContainsKey(prefab))
        {
            Destroy(instance);
            return;
        }
        
        Queue<GameObject> pool = objectPools[prefab];
        
        if (pool.Count < maxPoolSize)
        {
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            pool.Enqueue(instance);
        }
        else
        {
            Destroy(instance);
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
        
        // Handle updated images (position/rotation/quality changes)
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
        
        LogDebug($"Image detected: {imageName}");
        
        // Haptic Feedback
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
        
        // Fire event
        OnImageDetected?.Invoke(imageName);
        
        // Cancel any pending lost coroutine
        if (lostImageCoroutines.ContainsKey(imageName))
        {
            if (lostImageCoroutines[imageName] != null)
            {
                StopCoroutine(lostImageCoroutines[imageName]);
            }
            lostImageCoroutines.Remove(imageName);
        }
        
        // Get the mapping for this image
        ImageVideoMapping mapping = GetMappingForImage(imageName);
        
        // Runtime Callback: If we are in runtime mode and found no specific mapping, create one dynamically
        if (mapping == null && isRuntimeMode)
        {
            LogDebug($"Runtime mode: creating dynamic mapping for {imageName}");
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
            // Update last seen time
            mapping.lastSeenTime = Time.time;
            
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
            Debug.LogWarning($"[EnhancedManager] No mapping found for image: {imageName}");
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
            // Update last seen time
            mapping.lastSeenTime = Time.time;
            
            // Update transform
            UpdatePrefabTransform(trackedImage, mapping.instantiatedPrefab);
            
            // Evaluate tracking quality
            TrackingQuality quality = EvaluateTrackingQuality(trackedImage);
            
            if (quality != mapping.trackingQuality)
            {
                mapping.trackingQuality = quality;
                OnTrackingQualityChanged?.Invoke(imageName, quality);
                
                if (showTrackingQuality)
                {
                    LogDebug($"Image '{imageName}' tracking quality: {quality}");
                }
            }
            
            // Handle tracking state changes
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (!mapping.instantiatedPrefab.activeSelf)
                {
                    mapping.instantiatedPrefab.SetActive(true);
                }
                
                // Pause video if tracking quality is too poor
                if (quality < minimumTrackingQuality)
                {
                    PauseVideoForMapping(mapping);
                }
                else
                {
                    ResumeVideoForMapping(mapping);
                }
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                // Limited tracking - keep visible but maybe pause video
                if (quality < minimumTrackingQuality)
                {
                    PauseVideoForMapping(mapping);
                }
            }
            else if (trackedImage.trackingState == TrackingState.None)
            {
                // Start grace period before hiding
                if (disableOnImageLost && !lostImageCoroutines.ContainsKey(imageName))
                {
                    lostImageCoroutines[imageName] = StartCoroutine(HandleImageLostWithGracePeriod(imageName, mapping));
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
        LogDebug($"Image lost: {imageName}");
        
        OnImageLost?.Invoke(imageName);
        
        ImageVideoMapping mapping = GetMappingForImage(imageName);
        
        if (mapping != null && mapping.instantiatedPrefab != null)
        {
            if (destroyOnImageLost)
            {
                GameObject prefabType = mapping.videoPrefab != null ? mapping.videoPrefab : defaultVideoPrefab;
                ReturnPrefabToPool(prefabType, mapping.instantiatedPrefab);
                mapping.instantiatedPrefab = null;
            }
            else if (disableOnImageLost)
            {
                if (!lostImageCoroutines.ContainsKey(imageName))
                {
                    lostImageCoroutines[imageName] = StartCoroutine(HandleImageLostWithGracePeriod(imageName, mapping));
                }
            }
        }
    }
    
    /// <summary>
    /// Waits for grace period before disabling lost image
    /// </summary>
    private IEnumerator HandleImageLostWithGracePeriod(string imageName, ImageVideoMapping mapping)
    {
        yield return new WaitForSeconds(lostImageGracePeriod);
        
        if (mapping.instantiatedPrefab != null)
        {
            mapping.instantiatedPrefab.SetActive(false);
        }
        
        lostImageCoroutines.Remove(imageName);
    }
    
    /// <summary>
    /// Evaluates the tracking quality based on various factors
    /// </summary>
    private TrackingQuality EvaluateTrackingQuality(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.None)
            return TrackingQuality.Poor;
        
        if (trackedImage.trackingState == TrackingState.Limited)
            return TrackingQuality.Fair;
        
        // For tracking state, we can look at additional factors
        // Note: AR Foundation doesn't expose tracking confidence directly,
        // but we can infer from stability
        
        // Check if position is stable (not jittering)
        // This would require tracking position over time - simplified for now
        
        return TrackingQuality.Good;
    }
    
    /// <summary>
    /// Pauses video for a mapping
    /// </summary>
    private void PauseVideoForMapping(ImageVideoMapping mapping)
    {
        if (mapping.instantiatedPrefab != null)
        {
            var videoControl = mapping.instantiatedPrefab.GetComponent<VideoAnimControl>();
            if (videoControl != null && videoControl.videoPlayer != null)
            {
                if (videoControl.videoPlayer.isPlaying)
                {
                    videoControl.videoPlayer.Pause();
                    LogDebug($"Paused video for {mapping.imageName} due to poor tracking");
                }
            }
        }
    }
    
    /// <summary>
    /// Resumes video for a mapping
    /// </summary>
    private void ResumeVideoForMapping(ImageVideoMapping mapping)
    {
        if (mapping.instantiatedPrefab != null)
        {
            var videoControl = mapping.instantiatedPrefab.GetComponent<VideoAnimControl>();
            if (videoControl != null && videoControl.videoPlayer != null)
            {
                if (!videoControl.videoPlayer.isPlaying)
                {
                    videoControl.videoPlayer.Play();
                    LogDebug($"Resumed video for {mapping.imageName}");
                }
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
            Debug.LogError($"[EnhancedManager] No prefab available for image: {mapping.imageName}");
            return;
        }
        
        // Get instance from pool or create new
        GameObject instance = GetPrefabFromPool(prefabToInstantiate, trackedImage.transform);
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
        
        LogDebug($"Instantiated prefab for image: {mapping.imageName}");
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
            bool isPathOrUrl = videoSource.Contains("://") || videoSource.Contains("/") || videoSource.Contains("\\");

            if (isPathOrUrl)
            {
                videoPlayer.source = UnityEngine.Video.VideoSource.Url;
                videoPlayer.url = videoSource;
            }
            else
            {
                videoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
                Debug.LogWarning($"[EnhancedManager] Local video file support requires VideoClip assignment: {videoSource}");
            }
        }
        else
        {
            Debug.LogWarning($"[EnhancedManager] No VideoPlayer found in prefab for video source: {videoSource}");
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
        
        LogDebug($"Added new mapping for image: {imageName}");
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
    /// </summary>
    public void InitializeRuntime(MutableRuntimeReferenceImageLibrary library, string globalVideoPath)
    {
        if (trackedImageManager == null) trackedImageManager = GetComponent<ARTrackedImageManager>();

        LogDebug($"Initializing Runtime with {(library != null ? library.count.ToString() : "NULL (Editor)")} images and video: {globalVideoPath}");

        isRuntimeMode = true;
        runtimeGlobalVideoPath = globalVideoPath;

        // Assign the new library
        if (library != null)
        {
            trackedImageManager.referenceLibrary = library;
        }
        else
        {
             Debug.LogWarning("[EnhancedManager] Library is null (expected in Editor without XR Sim). Skipping assignment.");
        }
        
        // Restart the subsystem to pick up the new library
        trackedImageManager.enabled = false;
        trackedImageManager.enabled = true;

        LogDebug("ARTrackedImageManager restarted with runtime library.");
    }
    
    /// <summary>
    /// Cleans up all pooled objects
    /// </summary>
    public void ClearObjectPools()
    {
        foreach (var pool in objectPools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        
        objectPools.Clear();
        LogDebug("Cleared all object pools");
    }
    
    /// <summary>
    /// Gets tracking statistics for debugging
    /// </summary>
    public string GetTrackingStats()
    {
        int activeCount = 0;
        int trackedCount = 0;
        
        foreach (var mapping in imageVideoMappings)
        {
            if (mapping.instantiatedPrefab != null && mapping.instantiatedPrefab.activeSelf)
            {
                activeCount++;
                if (mapping.trackingQuality >= TrackingQuality.Good)
                {
                    trackedCount++;
                }
            }
        }
        
        return $"Active: {activeCount}, Well-Tracked: {trackedCount}, Total Mappings: {imageVideoMappings.Count}";
    }
    
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EnhancedManager] {message}");
        }
    }
    
    void OnDestroy()
    {
        ClearObjectPools();
    }
}
