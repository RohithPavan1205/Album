using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Downloads wedding assets (images & video) at runtime based on the Client ID,
/// creates a runtime AR reference library, and handles local caching.
/// </summary>
public class RuntimeWeddingLoader : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Base URL of the backend API")]
    public string apiBaseUrl = "http://localhost:8000"; // Default, should be changed or set dynamically
    
    [Header("Debug")]
    public string overrideClientId = ""; // For testing without PlayerPrefs

    [Header("Dependencies")]
    [Tooltip("Reference to the MultiImageVideoManager to configure")]
    public MultiImageVideoManager videoManager;

    [Tooltip("Reference to the ARSession to enable/disable")]
    public ARSession arSession;

    // Events
    public event Action<float> OnProgressUpdated;
    public event Action<MutableRuntimeReferenceImageLibrary, string> OnAssetsLoaded; // Returns library + video path
    public event Action<string> OnError;
    public event Action OnRetryPossible;

    // State
    private string currentClientId;
    private string cachedVideoPath;
    private List<Texture2D> loadedTextures = new List<Texture2D>();
    private MutableRuntimeReferenceImageLibrary runtimeLibrary;
    private bool isOfflineMode = false;

    [Serializable]
    private class AssetResponse
    {
        public string client_id;
        public List<string> images;
        public string video; 
        public string error; 
    }

    private void Start()
    {
        if (videoManager == null)
        {
            videoManager = FindObjectOfType<MultiImageVideoManager>();
        }

        if (arSession == null) arSession = FindObjectOfType<ARSession>();
        
        // Don't disable ARSession immediately here, let AppUIManager handle the flow 
        // or disable it only if we are actually starting a load.
        
        // Check if we have a saved ID to auto-start, otherwise wait for UI
        currentClientId = PlayerPrefs.GetString("CLIENT_ID", "");
        
        if (!string.IsNullOrEmpty(overrideClientId))
        {
            currentClientId = overrideClientId;
        }

        if (!string.IsNullOrEmpty(currentClientId))
        {
            // Auto-start if we remember the user
            if (arSession != null) arSession.enabled = false;
            StartCoroutine(LoadAssetsRoutine());
        }
        else
        {
            Debug.Log("[RuntimeWeddingLoader] Initialization Complete. Waiting for user to enter Client ID in Logic Screen...");
        }
    }

    /// <summary>
    /// Call this from UI when user logs in
    /// </summary>
    /// <param name="newClientId">The client ID entered by user</param>
    public void LoadForClient(string newClientId)
    {
        currentClientId = newClientId;
        PlayerPrefs.SetString("CLIENT_ID", currentClientId);
        PlayerPrefs.Save();
        
        StopAllCoroutines();
        if (arSession != null) arSession.enabled = false;
        StartCoroutine(LoadAssetsRoutine());
    }

    public void Retry()
    {
        StopAllCoroutines();
        StartCoroutine(LoadAssetsRoutine());
    }

    private IEnumerator LoadAssetsRoutine()
    {
        OnProgressUpdated?.Invoke(0.1f);
        AssetResponse response = null;

        // 1. Fetch Asset List from Backend
        string apiUrl = $"{apiBaseUrl}/api/client/{Uri.EscapeDataString(currentClientId)}/assets";
        Debug.Log($"[RuntimeWeddingLoader] Fetching: {apiUrl}");

        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.timeout = 10; // 10 seconds timeout
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
             string json = request.downloadHandler.text;
             response = JsonUtility.FromJson<AssetResponse>(json);
             
             // Cache the manifest
             string manifestPath = Path.Combine(Application.persistentDataPath, "clients", currentClientId, "manifest.json");
             EnsureDirectory(manifestPath);
             File.WriteAllText(manifestPath, json);
             isOfflineMode = false;
        }
        else
        {
            Debug.LogWarning($"[RuntimeWeddingLoader] API Failed ({request.error}). Trying offline cache...");
            
            // Try Loading from Cache
            string manifestPath = Path.Combine(Application.persistentDataPath, "clients", currentClientId, "manifest.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    string json = File.ReadAllText(manifestPath);
                    response = JsonUtility.FromJson<AssetResponse>(json);
                    
                    if (response != null && CheckIfCachedFilesExist(response))
                    {
                        Debug.Log("[RuntimeWeddingLoader] Loaded from local cache.");
                        isOfflineMode = true;
                    }
                    else
                    {
                        HandleError("Cached data is incomplete. Please check connection.");
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    HandleError($"Corrupt cache: {e.Message}");
                    yield break;
                }
            }
            else
            {
                HandleError($"Connection failed and no cache found: {request.error}");
                OnRetryPossible?.Invoke();
                Debug.Log($"[RuntimeWeddingLoader] Offline Mode: {isOfflineMode}");
                yield break;
            }
        }

        if (response == null || !string.IsNullOrEmpty(response.error))
        {
            HandleError($"Server Error: {response?.error ?? "Invalid response"}");
            OnRetryPossible?.Invoke();
            yield break;
        }

        if (response.images == null || response.images.Count == 0)
        {
            HandleError("No images found for this client.");
            yield break;
        }

        Debug.Log($"[RuntimeWeddingLoader] Found {response.images.Count} images and 1 video.");
        OnProgressUpdated?.Invoke(0.2f);

        // 2. Prepare Directories
        string clientDir = Path.Combine(Application.persistentDataPath, "clients", currentClientId);
        string imagesDir = Path.Combine(clientDir, "images");
        string videosDir = Path.Combine(clientDir, "videos");

        if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);
        if (!Directory.Exists(videosDir)) Directory.CreateDirectory(videosDir);

        // 3. Download/Load Video
        if (!string.IsNullOrEmpty(response.video))
        {
            string videoFileName = GetFileNameFromUrl(response.video, "video.mp4");
            string localVideoPath = Path.Combine(videosDir, videoFileName);
            
            yield return DownloadFileIfNeeded(response.video, localVideoPath);
            cachedVideoPath = localVideoPath;
        }
        else
        {
            HandleError("No video URL provided.");
            yield break;
        }

        OnProgressUpdated?.Invoke(0.4f);

        // 4. Download/Load Images & Create Textures
        loadedTextures.Clear();
        float imageProgressStep = 0.4f / response.images.Count; // Allocate 40% of progress to images

        for (int i = 0; i < response.images.Count; i++)
        {
            string imageUrl = response.images[i];
            string imageFileName = GetFileNameFromUrl(imageUrl, $"image_{i}.jpg");
            string localImagePath = Path.Combine(imagesDir, imageFileName);

            yield return DownloadFileIfNeeded(imageUrl, localImagePath);
            
            // Load into Texture2D
            byte[] fileData = File.ReadAllBytes(localImagePath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(fileData))
            {
                tex.name = imageFileName; // Used as the AR Reference Image Name
                loadedTextures.Add(tex);
            }
            else
            {
                Debug.LogWarning($"[RuntimeWeddingLoader] Failed to load texture: {localImagePath}");
            }

            OnProgressUpdated?.Invoke(0.4f + ((i + 1) * imageProgressStep));
        }

        if (loadedTextures.Count == 0)
        {
            HandleError("Failed to load any valid textures.");
            yield break;
        }


        // 5. Build Runtime Reference Library
        yield return BuildReferenceLibrary();
        
        // 6. Finish
        OnProgressUpdated?.Invoke(1.0f);
        
        // Enable AR Session now that we are ready
        if (arSession != null) 
        {
            arSession.enabled = true;
            // Sometimes it's good to reset it
            arSession.Reset();
        }

        OnAssetsLoaded?.Invoke(runtimeLibrary, cachedVideoPath);
        
        if (videoManager != null)
        {
            videoManager.InitializeRuntime(runtimeLibrary, cachedVideoPath);
        }
    }

    private void EnsureDirectory(string filePath)
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
    
    private bool CheckIfCachedFilesExist(AssetResponse response)
    {
        string clientDir = Path.Combine(Application.persistentDataPath, "clients", currentClientId);
        
        // Check video
        if (!string.IsNullOrEmpty(response.video))
        {
             string videoName = GetFileNameFromUrl(response.video, "video.mp4");
             if (!File.Exists(Path.Combine(clientDir, "videos", videoName))) return false;
        }
        
        // Check images (check at least one exists?)
        foreach (var img in response.images)
        {
            string imgName = GetFileNameFromUrl(img, "img.jpg"); 
             // Note: GetFileNameFromUrl logic needs to be consistent!
             // It uses GetHashCode, which is stable for same string input in same runtime, 
             // but technically GetHashCode implementation is NOT guaranteed to be stable across versions/platforms.
             // Ideally we should use a stable hash like MD5 or just the filename if confident.
             // For now we assume it works or we should fix GetFileNameFromUrl to be stable.
            if (!File.Exists(Path.Combine(clientDir, "images", imgName))) return false; 
        }
        return true;
    }


    private IEnumerator DownloadFileIfNeeded(string url, string localPath)
    {
        if (File.Exists(localPath))
        {
            Debug.Log($"[RuntimeWeddingLoader] Found cached: {localPath}");
            yield break; 
        }

        Debug.Log($"[RuntimeWeddingLoader] Downloading: {url} -> {localPath}");
        using (UnityWebRequest uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerFile(localPath);
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[RuntimeWeddingLoader] Download failed: {uwr.error}");
            }
        }
    }

    private IEnumerator BuildReferenceLibrary()
    {
        Debug.Log("[RuntimeWeddingLoader] Building Runtime Reference Library...");
        
        // Find the AR Tracked Image Manager to create a library
        ARTrackedImageManager manager = FindObjectOfType<ARTrackedImageManager>();
        if (manager == null)
        {
            HandleError("ARTrackedImageManager not found in scene.");
            yield break;
        }

        // --- EDITOR BYPASS ---
        #if UNITY_EDITOR
        Debug.Log("[RuntimeWeddingLoader] Editor Mode: Skipping Runtime Library Creation (Not supported in Editor).");
        // Simulate progress for testing UI flow
        for (int i = 0; i < loadedTextures.Count; i++)
        {
            yield return new WaitForSeconds(0.1f); 
            OnProgressUpdated?.Invoke(0.8f + ((i+1) * 0.2f / loadedTextures.Count));
        }
        
        // We can't actually track images in Editor without XR Simulation, so we just return.
        // The AppUIManager will still transition to "Ready" state.
        yield break;
        #endif
        // ---------------------

        runtimeLibrary = manager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;
        if (runtimeLibrary == null)
        {
            HandleError("Platform does not support MutableRuntimeReferenceImageLibrary.");
            yield break;
        }

        float jobProgressStep = 0.2f / loadedTextures.Count; // Last 20%
        int addedCount = 0;

        foreach (var texture in loadedTextures)
        {
             // 0.2 meters width is typical for a printed photo
             if (texture == null) continue;

             UnityEngine.XR.ARSubsystems.AddReferenceImageJobState jobState = default;
             bool jobScheduled = false;

             try 
             {
                 // Note: ScheduleAddImageWithValidationJob returns a JobHandle
                 jobState = runtimeLibrary.ScheduleAddImageWithValidationJob(
                     texture,
                     texture.name,
                     0.2f // Physical width in meters
                 );
                 jobScheduled = true;
             }
             catch(Exception e)
             {
                 Debug.LogError($"[RuntimeWeddingLoader] Failed to schedule image add: {e.Message}");
             }

             if (jobScheduled)
             {
                 // Wait for the job to complete
                 while (!jobState.jobHandle.IsCompleted)
                 {
                     yield return null;
                 }
                 
                 addedCount++;
                 OnProgressUpdated?.Invoke(0.8f + (addedCount * jobProgressStep));
             }
        }

        Debug.Log($"[RuntimeWeddingLoader] Library ready with {runtimeLibrary.count} images.");
    }

    private string GetFileNameFromUrl(string url, string fallback)
    {
        try
        {
            // Simple extraction. For Firebase, usually has tokens, so might need cleaning.
            // But preserving uniqueness is key. 
            // Firebase URLs: .../o/full%2Fpath%2Fimage.jpg?alt=media...
            // Let's just create a hash or safe name if it looks complex, 
            // OR use the actual path if possible. 
            // For simplicity and safety, let's use a hash of the full URL + extension from fallback.
            
            // Clean logic:
            if (url.StartsWith("http") || url.Contains("/"))
            {
                 // Create a stable filename based on the string content, not GetHashCode which can vary
                 using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                 {
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(url);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    
                    // Convert to hex string
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }
                    string extension = Path.GetExtension(url);
                    // Remove query params from extension if any
                    if (extension.Contains("?")) extension = extension.Split('?')[0];
                    if (string.IsNullOrEmpty(extension)) extension = Path.GetExtension(fallback);
                    
                    return sb.ToString() + extension;
                 }
            }
            return fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void HandleError(string msg)
    {
        Debug.LogError($"[RuntimeWeddingLoader] {msg}");
        OnError?.Invoke(msg);
    }
}
