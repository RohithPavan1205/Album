using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// Debug UI overlay for monitoring AR tracking performance in real-time.
/// Attach this to a Canvas in your scene for live performance metrics.
/// </summary>
public class ARTrackingDebugger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the Enhanced Manager")]
    public ImageVideoManager trackingManager;
    
    [Header("UI Elements")]
    [Tooltip("Text component to display debug info")]
    public Text debugText;
    
    [Tooltip("Toggle to show/hide debug panel")]
    public KeyCode toggleKey = KeyCode.D;
    
    [Header("Settings")]
    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 2.0f)]
    public float updateInterval = 0.5f;
    
    [Tooltip("Show FPS counter")]
    public bool showFPS = true;
    
    [Tooltip("Show memory usage")]
    public bool showMemory = true;
    
    [Tooltip("Show tracking statistics")]
    public bool showTrackingStats = true;
    
    [Tooltip("Show individual image status")]
    public bool showImageDetails = true;
    
    // Private state
    private bool isVisible = true;
    private float lastUpdateTime = 0f;
    private float deltaTime = 0f;
    private CanvasGroup canvasGroup;
    
    void Awake()
    {
        // Auto-find components if not assigned
        if (trackingManager == null)
        {
            trackingManager = FindObjectOfType<ImageVideoManager>();
        }
        
        if (debugText == null)
        {
            debugText = GetComponentInChildren<Text>();
        }
        
        // Get or add canvas group for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void Update()
    {
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleVisibility();
        }
        
        // Update FPS calculation
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // Update debug text at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDebugText();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Updates the debug text with current statistics
    /// </summary>
    private void UpdateDebugText()
    {
        if (debugText == null || !isVisible) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== AR TRACKING DEBUG ===\n");
        
        // FPS
        if (showFPS)
        {
            float fps = 1.0f / deltaTime;
            Color fpsColor = GetFPSColor(fps);
            sb.AppendLine($"<b>FPS:</b> <color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{fps:F1}</color>");
        }
        
        // Memory
        if (showMemory)
        {
            long memoryMB = System.GC.GetTotalMemory(false) / 1048576;
            sb.AppendLine($"<b>Memory:</b> {memoryMB} MB");
        }
        
        sb.AppendLine();
        
        // Tracking statistics
        if (showTrackingStats && trackingManager != null)
        {
            string stats = trackingManager.GetTrackingStats();
            sb.AppendLine($"<b>Tracking:</b>\n{stats}");
            sb.AppendLine();
        }
        
        // Individual image details
        if (showImageDetails && trackingManager != null)
        {
            sb.AppendLine("<b>Images:</b>");
            
            foreach (var mapping in trackingManager.imageVideoMappings)
            {
                if (mapping.instantiatedPrefab != null && mapping.instantiatedPrefab.activeSelf)
                {
                    string qualityIcon = GetQualityIcon(mapping.trackingQuality);
                    Color qualityColor = GetQualityColor(mapping.trackingQuality);
                    
                    sb.AppendLine($"  {qualityIcon} <color=#{ColorUtility.ToHtmlStringRGB(qualityColor)}>{mapping.imageName}</color>");
                    
                    // Video status
                    var videoControl = mapping.instantiatedPrefab.GetComponent<VideoController>();
                    if (videoControl != null)
                    {
                        string videoStatus = videoControl.IsPlaying() ? "▶ Playing" : "⏸ Paused";
                        float progress = videoControl.GetPlaybackProgress() * 100f;
                        sb.AppendLine($"    {videoStatus} ({progress:F0}%)");
                    }
                }
            }
        }
        
        debugText.text = sb.ToString();
    }
    
    /// <summary>
    /// Gets color based on FPS performance
    /// </summary>
    private Color GetFPSColor(float fps)
    {
        if (fps >= 50f) return Color.green;
        if (fps >= 30f) return Color.yellow;
        return Color.red;
    }
    
    /// <summary>
    /// Gets color based on tracking quality
    /// </summary>
    private Color GetQualityColor(ImageVideoManager.TrackingQuality quality)
    {
        switch (quality)
        {
            case ImageVideoManager.TrackingQuality.Excellent:
                return Color.green;
            case ImageVideoManager.TrackingQuality.Good:
                return Color.cyan;
            case ImageVideoManager.TrackingQuality.Fair:
                return Color.yellow;
            case ImageVideoManager.TrackingQuality.Poor:
                return new Color(1f, 0.5f, 0f); // Orange
            default:
                return Color.gray;
        }
    }
    
    /// <summary>
    /// Gets icon based on tracking quality
    /// </summary>
    private string GetQualityIcon(ImageVideoManager.TrackingQuality quality)
    {
        switch (quality)
        {
            case ImageVideoManager.TrackingQuality.Excellent:
                return "●●●";
            case ImageVideoManager.TrackingQuality.Good:
                return "●●○";
            case ImageVideoManager.TrackingQuality.Fair:
                return "●○○";
            case ImageVideoManager.TrackingQuality.Poor:
                return "○○○";
            default:
                return "???";
        }
    }
    
    /// <summary>
    /// Toggles debug panel visibility
    /// </summary>
    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }
    
    /// <summary>
    /// Shows the debug panel
    /// </summary>
    public void Show()
    {
        isVisible = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// Hides the debug panel
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
