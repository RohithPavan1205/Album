using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>
/// Handles play/pause button interactions for AR video playback.
/// Supports touch input for mobile AR experiences.
/// </summary>
public class ArButton : MonoBehaviour
{
    [Header("Video Control")]
    [Tooltip("Reference to the video player to control")]
    public VideoPlayer videoPlayer;
    
    [Header("Animation")]
    [Tooltip("Animator component for play/pause button animations")]
    private Animator animController;
    
    [Header("Events")]
    public UnityEvent onPlayPause = new UnityEvent();
    
    // State tracking
    private bool isPlaying = false;
    private Camera mainCamera;
    
    void Awake()
    {
        // Cache components
        animController = GetComponent<Animator>();
        mainCamera = Camera.main;
        
        // Validate references
        if (videoPlayer == null)
        {
            Debug.LogError($"[ArButton] VideoPlayer reference is missing on {gameObject.name}");
        }
        
        if (animController == null)
        {
            Debug.LogWarning($"[ArButton] No Animator found on {gameObject.name}. Animations will be disabled.");
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// Handles both mouse (editor) and touch (mobile) input
    /// </summary>
    private void HandleInput()
    {
        // Handle touch input for mobile devices
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
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
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }
        
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
        {
            TogglePlayPause();
        }
    }
    
    /// <summary>
    /// Toggles between play and pause states
    /// </summary>
    public void TogglePlayPause()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[ArButton] Cannot toggle play/pause - VideoPlayer reference is missing!");
            return;
        }
        
        isPlaying = !isPlaying;
        
        if (isPlaying)
        {
            videoPlayer.Play();
            PlayAnimation("btn_Pause");
        }
        else
        {
            videoPlayer.Pause();
            PlayAnimation("btn_Play");
        }
        
        // Invoke event for external listeners
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
    }
}
