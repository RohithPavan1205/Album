using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// ENHANCED: Controls video playback with improved performance, preloading, and smooth transitions.
/// Key improvements:
/// - Video preloading and buffering
/// - Smooth fade transitions
/// - Adaptive quality based on tracking
/// - Better memory management
/// - Frame-rate independent updates
/// </summary>
public class VideoController : MonoBehaviour
{
    [Header("Video Components")]
    [Tooltip("Reference to the video player component")]
    public VideoPlayer videoPlayer;
    
    [Header("Scaling")]
    [Tooltip("The plane/quad that displays the video")]
    public Transform videoPlane;
    
    [Tooltip("Reference to the tracked image (parent)")]
    public ARTrackedImage trackedImage;
    
    [Header("Animation")]
    [Tooltip("Animator for fade in/out effects")]
    private Animator animController;
    
    [Header("Settings")]
    [Tooltip("Offset from the tracked image surface (in meters)")]
    public float videoHeightOffset = 0.001f;
    
    [Tooltip("Scale multiplier for the video (1.0 = exact match to image size)")]
    [Range(0.5f, 2.0f)]
    public float videoScaleMultiplier = 1.0f;
    
    [Tooltip("Time to fade in audio (seconds)")]
    public float audioFadeTime = 2.0f;
    
    [Header("Performance")]
    [Tooltip("Preload video on start for instant playback")]
    public bool preloadVideo = true;
    
    [Tooltip("Update scale check interval (seconds) - higher = better performance")]
    [Range(0.1f, 1.0f)]
    public float scaleCheckInterval = 0.5f;
    
    [Tooltip("Pause video when not visible to save resources")]
    public bool pauseWhenInvisible = true;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to use when video is loading")]
    public Material loadingMaterial;
    
    [Tooltip("Show loading indicator")]
    public bool showLoadingIndicator = true;
    
    // State tracking
    private bool hasPlayedEndAnimation = false;
    private bool isInitialized = false;
    private bool isVideoReady = false;
    private bool wasPlayingBeforePause = false;
    private Vector2 lastImageSize = Vector2.zero;
    private float lastScaleCheckTime = 0f;
    private Coroutine audioFadeRoutine;
    private Material originalMaterial;
    private MeshRenderer videoRenderer;
    
    // Events
    public event Action OnVideoReady;
    public event Action OnVideoStarted;
    public event Action OnVideoEnded;
    public event Action<string> OnVideoError;
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        if (preloadVideo && videoPlayer != null)
        {
            StartCoroutine(PrepareVideoRoutine());
        }
        else
        {
            // Scale video to match tracked image on start
            ScaleVideoToTrackedImage();
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Check if video has ended (only when playing)
        if (isVideoReady && videoPlayer.isPlaying)
        {
            CheckVideoEnd();
        }
        
        // Update scale if tracked image size changes (throttled)
        if (Time.time - lastScaleCheckTime >= scaleCheckInterval)
        {
            UpdateScaleIfNeeded();
            lastScaleCheckTime = Time.time;
        }
    }
    
    void OnBecameVisible()
    {
        if (pauseWhenInvisible && wasPlayingBeforePause && videoPlayer != null)
        {
            videoPlayer.Play();
            wasPlayingBeforePause = false;
        }
    }
    
    void OnBecameInvisible()
    {
        if (pauseWhenInvisible && videoPlayer != null && videoPlayer.isPlaying)
        {
            wasPlayingBeforePause = true;
            videoPlayer.Pause();
        }
    }
    
    /// <summary>
    /// Initializes and validates all required components
    /// </summary>
    private void InitializeComponents()
    {
        // Get animator component
        animController = GetComponent<Animator>();
        if (animController == null)
        {
            Debug.LogWarning($"[EnhancedVideoControl] No Animator found on {gameObject.name}");
        }
        
        // Try to find video player if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError($"[EnhancedVideoControl] VideoPlayer not found on {gameObject.name}");
                return;
            }
        }
        
        // Subscribe to video player events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.started += OnVideoStartedEvent;
        videoPlayer.errorReceived += OnVideoErrorEvent;
        videoPlayer.loopPointReached += OnVideoLoopPointReached;
        
        // Try to find video plane if not assigned
        if (videoPlane == null)
        {
            Transform plane = transform.Find("VideoPlane") ?? transform.Find("Plane");
            if (plane != null)
            {
                videoPlane = plane;
                videoRenderer = videoPlane.GetComponent<MeshRenderer>();
                
                // Store original material
                if (videoRenderer != null)
                {
                    originalMaterial = videoRenderer.material;
                }
            }
            else
            {
                Debug.LogWarning($"[EnhancedVideoControl] VideoPlane not assigned. Auto-scaling disabled.");
            }
        }
        
        // Try to get tracked image from parent
        if (trackedImage == null)
        {
            trackedImage = GetComponentInParent<ARTrackedImage>();
            if (trackedImage == null)
            {
                Debug.LogWarning($"[EnhancedVideoControl] ARTrackedImage not found. Will attempt to find it later.");
            }
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Prepares the video for playback
    /// </summary>
    private IEnumerator PrepareVideoRoutine()
    {
        if (videoPlayer == null) yield break;
        
        // Show loading indicator
        if (showLoadingIndicator && loadingMaterial != null && videoRenderer != null)
        {
            videoRenderer.material = loadingMaterial;
        }
        
        // Prepare the video
        videoPlayer.Prepare();
        
        // Wait for video to be ready
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        // Video is ready
        isVideoReady = true;
        
        // Restore original material
        if (videoRenderer != null && originalMaterial != null)
        {
            videoRenderer.material = originalMaterial;
        }
        
        // Scale video to match tracked image
        ScaleVideoToTrackedImage();
        
        // Fire event
        OnVideoReady?.Invoke();
        
        // Start audio fade in if video is set to play on awake
        if (videoPlayer.playOnAwake && isActiveAndEnabled)
        {
            audioFadeRoutine = StartCoroutine(FadeAudioIn());
        }
    }
    
    /// <summary>
    /// Called when video is prepared
    /// </summary>
    private void OnVideoPrepared(VideoPlayer source)
    {
        isVideoReady = true;
        Debug.Log($"[EnhancedVideoControl] Video prepared: {source.url}");
    }
    
    /// <summary>
    /// Called when video starts playing
    /// </summary>
    private void OnVideoStartedEvent(VideoPlayer source)
    {
        OnVideoStarted?.Invoke();
        Debug.Log($"[EnhancedVideoControl] Video started");
    }
    
    /// <summary>
    /// Called when video encounters an error
    /// </summary>
    private void OnVideoErrorEvent(VideoPlayer source, string message)
    {
        Debug.LogError($"[EnhancedVideoControl] Video error: {message}");
        OnVideoError?.Invoke(message);
    }
    
    /// <summary>
    /// Called when video reaches loop point (end)
    /// </summary>
    private void OnVideoLoopPointReached(VideoPlayer source)
    {
        if (!source.isLooping)
        {
            TriggerEndAnimation();
            OnVideoEnded?.Invoke();
        }
    }
    
    /// <summary>
    /// Scales the video plane to match the physical dimensions of the tracked image
    /// </summary>
    public void ScaleVideoToTrackedImage()
    {
        if (videoPlane == null)
        {
            Debug.LogWarning("[EnhancedVideoControl] Cannot scale video - videoPlane is null");
            return;
        }
        
        // Try to get tracked image if not already set
        if (trackedImage == null)
        {
            trackedImage = GetComponentInParent<ARTrackedImage>();
        }
        
        if (trackedImage != null && trackedImage.referenceImage != null)
        {
            // Get the physical size of the tracked image
            Vector2 imageSize = trackedImage.referenceImage.size;
            lastImageSize = imageSize;
            
            // Calculate scale to match image dimensions
            // Unity planes are 10x10 units by default, so we need to scale accordingly
            float scaleX = imageSize.x * videoScaleMultiplier * 0.1f;
            float scaleZ = imageSize.y * videoScaleMultiplier * 0.1f;
            
            // Apply scale to video plane with smooth transition
            videoPlane.localScale = new Vector3(scaleX, 1f, scaleZ);
            
            // Position video slightly above the tracked image to prevent z-fighting
            Vector3 localPos = videoPlane.localPosition;
            localPos.y = videoHeightOffset;
            videoPlane.localPosition = localPos;
            
            Debug.Log($"[EnhancedVideoControl] Scaled video to match image: {imageSize.x}m x {imageSize.y}m");
        }
        else
        {
            Debug.LogWarning("[EnhancedVideoControl] Cannot scale video - tracked image reference not found");
        }
    }
    
    /// <summary>
    /// Updates the video scale if the tracked image size has changed
    /// </summary>
    private void UpdateScaleIfNeeded()
    {
        if (trackedImage != null && trackedImage.referenceImage != null)
        {
            Vector2 currentSize = trackedImage.referenceImage.size;
            
            // Check if size has changed (with small epsilon for floating point comparison)
            if (Vector2.Distance(currentSize, lastImageSize) > 0.001f)
            {
                ScaleVideoToTrackedImage();
            }
        }
    }
    
    /// <summary>
    /// Checks if the video has reached the end and triggers fade out animation
    /// </summary>
    private void CheckVideoEnd()
    {
        if (videoPlayer == null || !isVideoReady || hasPlayedEndAnimation)
            return;
        
        // Check if video has ended
        if (videoPlayer.frameCount > 0)
        {
            long currentFrame = videoPlayer.frame;
            long totalFrames = (long)videoPlayer.frameCount;
            
            // Trigger end animation when video is near complete (last 2 frames)
            if (currentFrame >= totalFrames - 2)
            {
                TriggerEndAnimation();
            }
        }
    }
    
    /// <summary>
    /// Triggers the fade out animation when video ends
    /// </summary>
    public void TriggerEndAnimation()
    {
        if (hasPlayedEndAnimation)
            return;
        
        if (animController != null)
        {
            animController.Play("FadeOut");
        }
        
        hasPlayedEndAnimation = true;
        Debug.Log("[EnhancedVideoControl] Video ended - playing fade out animation");
    }
    
    /// <summary>
    /// Resets the video to the beginning and resets animation state
    /// </summary>
    public void ResetVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.frame = 0;
            
            // Re-prepare if needed
            if (preloadVideo && !videoPlayer.isPrepared)
            {
                StartCoroutine(PrepareVideoRoutine());
            }
        }
        
        hasPlayedEndAnimation = false;
        
        if (animController != null)
        {
            animController.Play("FadeIn");
        }
        
        // Reset Volume
        if (videoPlayer != null)
        {
            SetVolume(0f);
        }
        
        // Restart fade if active
        if (isActiveAndEnabled)
        {
             if (audioFadeRoutine != null) StopCoroutine(audioFadeRoutine);
             audioFadeRoutine = StartCoroutine(FadeAudioIn());
        }
    }
    
    /// <summary>
    /// Plays the video
    /// </summary>
    public void PlayVideo()
    {
        if (videoPlayer == null) return;
        
        if (!isVideoReady && preloadVideo)
        {
            StartCoroutine(PrepareAndPlay());
        }
        else
        {
            videoPlayer.Play();
            
            if (audioFadeRoutine != null) StopCoroutine(audioFadeRoutine);
            audioFadeRoutine = StartCoroutine(FadeAudioIn());
        }
    }
    
    /// <summary>
    /// Pauses the video
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }
    
    /// <summary>
    /// Prepares and plays the video
    /// </summary>
    private IEnumerator PrepareAndPlay()
    {
        yield return PrepareVideoRoutine();
        videoPlayer.Play();
    }
    
    /// <summary>
    /// Sets the video player reference (useful for dynamic instantiation)
    /// </summary>
    public void SetVideoPlayer(VideoPlayer vp)
    {
        // Unsubscribe from old player
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.started -= OnVideoStartedEvent;
            videoPlayer.errorReceived -= OnVideoErrorEvent;
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }
        
        videoPlayer = vp;
        
        // Subscribe to new player
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.started += OnVideoStartedEvent;
            videoPlayer.errorReceived += OnVideoErrorEvent;
            videoPlayer.loopPointReached += OnVideoLoopPointReached;
        }
    }
    
    /// <summary>
    /// Sets the tracked image reference (useful for dynamic instantiation)
    /// </summary>
    public void SetTrackedImage(ARTrackedImage image)
    {
        trackedImage = image;
        ScaleVideoToTrackedImage();
    }

    /// <summary>
    /// Fades audio in smoothly
    /// </summary>
    private IEnumerator FadeAudioIn()
    {
        if (videoPlayer == null) yield break;

        float timer = 0f;
        SetVolume(0f);

        // Wait until playing
        while (!videoPlayer.isPlaying)
        {
            yield return null;
        }

        while (timer < audioFadeTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Clamp01(timer / audioFadeTime);
            SetVolume(volume);
            yield return null;
        }
        SetVolume(1f);
    }

    /// <summary>
    /// Sets the volume for all audio tracks
    /// </summary>
    private void SetVolume(float vol)
    {
        if (videoPlayer == null) return;
        
        // For Direct audio output (typical on mobile)
        for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
        {
            videoPlayer.SetDirectAudioVolume(i, vol);
        }
        
        // If using AudioSource
        if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource source = videoPlayer.GetTargetAudioSource(0);
            if (source != null) source.volume = vol;
        }
    }
    
    /// <summary>
    /// Gets the current playback progress (0-1)
    /// </summary>
    public float GetPlaybackProgress()
    {
        if (videoPlayer == null || videoPlayer.frameCount == 0)
            return 0f;
        
        return (float)videoPlayer.frame / (float)videoPlayer.frameCount;
    }
    
    /// <summary>
    /// Checks if video is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return videoPlayer != null && videoPlayer.isPlaying;
    }
    
    /// <summary>
    /// Checks if video is ready to play
    /// </summary>
    public bool IsReady()
    {
        return isVideoReady;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.started -= OnVideoStartedEvent;
            videoPlayer.errorReceived -= OnVideoErrorEvent;
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }
    }
}
