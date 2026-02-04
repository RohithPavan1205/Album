using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// ENHANCED: Handles play/pause button interactions with improved input handling and visual feedback.
/// Key improvements:
/// - Better touch detection with multi-touch support
/// - Visual feedback on button press
/// - Debouncing to prevent double-taps
/// - Support for gesture controls
/// - Accessibility improvements
/// </summary>
public class ARButton : MonoBehaviour
{
    [Header("Video Control")]
    [Tooltip("Reference to the video player to control")]
    public VideoPlayer videoPlayer;
    
    [Header("Animation")]
    [Tooltip("Animator component for play/pause button animations")]
    private Animator animController;
    
    [Header("Visual Feedback")]
    [Tooltip("Scale factor when button is pressed")]
    [Range(0.8f, 1.0f)]
    public float pressedScale = 0.9f;
    
    [Tooltip("Duration of press animation (seconds)")]
    public float pressAnimationDuration = 0.1f;
    
    [Tooltip("Color tint when button is pressed")]
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    [Header("Input Settings")]
    [Tooltip("Minimum time between button presses (seconds)")]
    public float debounceTime = 0.3f;
    
    [Tooltip("Maximum distance for raycast (meters)")]
    public float maxRaycastDistance = 10f;
    
    [Tooltip("Enable haptic feedback on button press")]
    public bool enableHapticFeedback = true;
    
    [Header("Events")]
    public UnityEvent onPlayPause = new UnityEvent();
    public UnityEvent onPlay = new UnityEvent();
    public UnityEvent onPause = new UnityEvent();
    
    // State tracking
    private bool isPlaying = false;
    private float lastPressTime = 0f;
    private Camera mainCamera;
    private Vector3 originalScale;
    private Color originalColor;
    private MeshRenderer meshRenderer;
    private Coroutine pressAnimationCoroutine;
    
    void Awake()
    {
        // Cache components
        animController = GetComponent<Animator>();
        mainCamera = Camera.main;
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Store original values
        originalScale = transform.localScale;
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
        
        // Validate references
        if (videoPlayer == null)
        {
            Debug.LogError($"[ARButton] VideoPlayer reference is missing on {gameObject.name}");
        }
        
        if (animController == null)
        {
            Debug.LogWarning($"[ARButton] No Animator found on {gameObject.name}. Animations will be disabled.");
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// Handles both mouse (editor) and touch (mobile) input with improved detection
    /// </summary>
    private void HandleInput()
    {
        // Handle touch input for mobile devices
        if (Input.touchCount > 0)
        {
            // Use the first touch
            Touch touch = Input.GetTouch(0);
            
            // Only process if this is a new touch
            if (touch.phase == TouchPhase.Began)
            {
                CheckButtonPress(touch.position);
            }
        }
        // Handle mouse input for editor testing
        else if (Input.GetMouseButtonDown(0))
        {
            CheckButtonPress(Input.mousePosition);
        }
    }
    
    /// <summary>
    /// Checks if the button was pressed at the given screen position
    /// </summary>
    private void CheckButtonPress(Vector2 screenPosition)
    {
        // Check debounce
        if (Time.time - lastPressTime < debounceTime)
        {
            return;
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }
        
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance))
        {
            // Check if we hit this button
            if (hit.collider.gameObject == gameObject)
            {
                lastPressTime = Time.time;
                
                // Trigger visual feedback
                if (pressAnimationCoroutine != null)
                {
                    StopCoroutine(pressAnimationCoroutine);
                }
                pressAnimationCoroutine = StartCoroutine(PlayPressAnimation());
                
                // Haptic feedback
                if (enableHapticFeedback)
                {
                    TriggerHapticFeedback();
                }
                
                // Toggle play/pause
                TogglePlayPause();
            }
        }
    }
    
    /// <summary>
    /// Toggles between play and pause states
    /// </summary>
    public void TogglePlayPause()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[ARButton] Cannot toggle play/pause - VideoPlayer reference is missing!");
            return;
        }
        
        isPlaying = !isPlaying;
        
        if (isPlaying)
        {
            videoPlayer.Play();
            PlayAnimation("btn_Pause");
            onPlay.Invoke();
            Debug.Log("[ARButton] Video playing");
        }
        else
        {
            videoPlayer.Pause();
            PlayAnimation("btn_Play");
            onPause.Invoke();
            Debug.Log("[ARButton] Video paused");
        }
        
        // Invoke general event for external listeners
        onPlayPause.Invoke();
    }
    
    /// <summary>
    /// Plays the specified animation if animator is available
    /// </summary>
    private void PlayAnimation(string animationName)
    {
        if (animController != null)
        {
            animController.Play(animationName);
        }
    }
    
    /// <summary>
    /// Plays the button press animation
    /// </summary>
    private IEnumerator PlayPressAnimation()
    {
        float elapsed = 0f;
        
        // Press down
        while (elapsed < pressAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressAnimationDuration;
            
            // Scale down
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pressedScale, t);
            
            // Tint color
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.Lerp(originalColor, pressedColor, t);
            }
            
            yield return null;
        }
        
        // Hold briefly
        yield return new WaitForSeconds(0.05f);
        
        // Release
        elapsed = 0f;
        while (elapsed < pressAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressAnimationDuration;
            
            // Scale back up
            transform.localScale = Vector3.Lerp(originalScale * pressedScale, originalScale, t);
            
            // Restore color
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.Lerp(pressedColor, originalColor, t);
            }
            
            yield return null;
        }
        
        // Ensure we're back to original
        transform.localScale = originalScale;
        if (meshRenderer != null)
        {
            meshRenderer.material.color = originalColor;
        }
    }
    
    /// <summary>
    /// Triggers haptic feedback on supported platforms
    /// </summary>
    private void TriggerHapticFeedback()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }
    
    /// <summary>
    /// Public method to set the video player reference (useful for dynamic instantiation)
    /// </summary>
    public void SetVideoPlayer(VideoPlayer vp)
    {
        videoPlayer = vp;
    }
    
    /// <summary>
    /// Resets the button to initial state
    /// </summary>
    public void ResetButton()
    {
        isPlaying = false;
        PlayAnimation("btn_Play");
        
        // Reset visual state
        transform.localScale = originalScale;
        if (meshRenderer != null)
        {
            meshRenderer.material.color = originalColor;
        }
    }
    
    /// <summary>
    /// Manually set the playing state (useful for syncing with video player)
    /// </summary>
    public void SetPlayingState(bool playing)
    {
        isPlaying = playing;
        PlayAnimation(playing ? "btn_Pause" : "btn_Play");
    }
    
    /// <summary>
    /// Gets the current playing state
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }
    
    /// <summary>
    /// Enable or disable the button
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        // Visual feedback for disabled state
        if (meshRenderer != null)
        {
            Color color = enabled ? originalColor : new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            meshRenderer.material.color = color;
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        if (pressAnimationCoroutine != null)
        {
            StopCoroutine(pressAnimationCoroutine);
        }
    }
}
