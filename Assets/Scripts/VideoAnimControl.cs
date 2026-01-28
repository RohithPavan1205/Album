using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Controls video playback and animations for AR tracked images.
/// Automatically scales video to match the physical dimensions of the tracked image.
/// Supports multiple tracked images with individual video content.
/// </summary>
public class VideoAnimControl : MonoBehaviour
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

    // State tracking
    private bool hasPlayedEndAnimation = false;
    private bool isInitialized = false;
    private Vector2 lastImageSize = Vector2.zero;
    private Coroutine audioFadeRoutine;
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        // Scale video to match tracked image on start
        // Scale video to match tracked image on start
        ScaleVideoToTrackedImage();
        
        // Start audio fade in
        if (isActiveAndEnabled)
        {
             audioFadeRoutine = StartCoroutine(FadeAudioIn());
        }
    }
    
    void Update()
    {
        // Check if video has ended
        CheckVideoEnd();
        
        // Update scale if tracked image size changes
        UpdateScaleIfNeeded();
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
            Debug.LogWarning($"[VideoAnimControl] No Animator found on {gameObject.name}");
        }
        
        // Try to find video player if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError($"[VideoAnimControl] VideoPlayer not found on {gameObject.name}");
                return;
            }
        }
        
        // Try to find video plane if not assigned
        if (videoPlane == null)
        {
            // Look for a child object named "VideoPlane" or "Plane"
            Transform plane = transform.Find("VideoPlane") ?? transform.Find("Plane");
            if (plane != null)
            {
                videoPlane = plane;
            }
            else
            {
                Debug.LogWarning($"[VideoAnimControl] VideoPlane not assigned. Auto-scaling disabled.");
            }
        }
        
        // Try to get tracked image from parent
        if (trackedImage == null)
        {
            trackedImage = GetComponentInParent<ARTrackedImage>();
            if (trackedImage == null)
            {
                Debug.LogWarning($"[VideoAnimControl] ARTrackedImage not found. Will attempt to find it later.");
            }
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Scales the video plane to match the physical dimensions of the tracked image
    /// </summary>
    public void ScaleVideoToTrackedImage()
    {
        if (videoPlane == null)
        {
            Debug.LogWarning("[VideoAnimControl] Cannot scale video - videoPlane is null");
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
            
            // Apply scale to video plane
            videoPlane.localScale = new Vector3(scaleX, 1f, scaleZ);
            
            // Position video slightly above the tracked image to prevent z-fighting
            Vector3 localPos = videoPlane.localPosition;
            localPos.y = videoHeightOffset;
            videoPlane.localPosition = localPos;
            
            Debug.Log($"[VideoAnimControl] Scaled video to match image: {imageSize.x}m x {imageSize.y}m");
        }
        else
        {
            Debug.LogWarning("[VideoAnimControl] Cannot scale video - tracked image reference not found");
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
            
            // Check if size has changed
            if (currentSize != lastImageSize)
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
        if (videoPlayer == null || !isInitialized)
            return;
        
        // Check if video has ended
        if (videoPlayer.frameCount > 0)
        {
            long currentFrame = videoPlayer.frame;
            long totalFrames = (long)videoPlayer.frameCount;
            
            // Trigger end animation when video is complete
            if (currentFrame >= totalFrames - 1 && !hasPlayedEndAnimation)
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
        Debug.Log("[VideoAnimControl] Video ended - playing fade out animation");
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
    /// Sets the video player reference (useful for dynamic instantiation)
    /// </summary>
    public void SetVideoPlayer(VideoPlayer vp)
    {
        videoPlayer = vp;
    }
    
    /// <summary>
    /// Sets the tracked image reference (useful for dynamic instantiation)
    /// </summary>
    public void SetTrackedImage(ARTrackedImage image)
    {
        trackedImage = image;
        ScaleVideoToTrackedImage();
    }

    private System.Collections.IEnumerator FadeAudioIn()
    {
        if (videoPlayer == null) yield break;

        float timer = 0f;
        SetVolume(0f);

        // Wait until playing?
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

    private void SetVolume(float vol)
    {
        if (videoPlayer == null) return;
        
        // Output mode might be Direct or AudioSource.
        // For Direct (typical on mobile):
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
}
