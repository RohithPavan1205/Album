using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Hides the content (MeshRenderer) until the VideoPlayer is actually ready and playing.
/// Prevents the static "placeholder" image from showing up before the video starts.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(MeshRenderer))]
public class HideBeforeVideo : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Hide immediately on awake
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Subscribe to events
        videoPlayer.prepareCompleted += OnPrepareCompleted;
        videoPlayer.started += OnStarted;
        videoPlayer.errorReceived += OnError;
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        // Video is ready, but might not be playing yet if "Play On Awake" is false
        // However, if we want to show the first frame (which should be ready now), we can enable it.
        // Usually, 'started' is safer for avoiding the blip, but let's see.
        
        // If PlayOnAwake is true, it will start immediately. 
        // We can wait for the first frame.
    }

    private void OnStarted(VideoPlayer source)
    {
        // The video claims to have started. 
        // Enable the renderer so we see the video content.
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogError($"[HideBeforeVideo] Video Error: {message}");
        // Optionally show the mesh anyway if it has a fallback image? 
        // For now, keep hidden or enable if you want the user to see the static image as error state.
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true; // Show static image if video fails
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.started -= OnStarted;
            videoPlayer.errorReceived -= OnError;
        }
    }
}
