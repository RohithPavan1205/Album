#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SpatialTracking;

public class SceneSetupTool : MonoBehaviour
{
    [MenuItem("WeddingApp/Rebuild Scene")]
    public static void RebuildScene()
    {
        // 1. Create AR Session Origin if missing
        ARSessionOrigin origin = FindObjectOfType<ARSessionOrigin>();
        if (origin == null)
        {
            GameObject arOriginGo = new GameObject("AR Session Origin");
            origin = arOriginGo.AddComponent<ARSessionOrigin>();
            arOriginGo.AddComponent<ARPlaneManager>();
            arOriginGo.AddComponent<ARRaycastManager>();
            // Add Camera
            GameObject cameraGo = new GameObject("AR Camera");
            cameraGo.transform.SetParent(arOriginGo.transform);
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cameraGo.AddComponent<ARCameraManager>();
            cameraGo.AddComponent<ARCameraBackground>();
            cameraGo.AddComponent<TrackedPoseDriver>(); // Need to add UnityEngine.SpatialTracking namespace if 2019+
            
            origin.camera = cam;
        }

        // 2. Add Managers to Origin
        if (!origin.GetComponent<ARTrackedImageManager>())
        {
             var manager = origin.gameObject.AddComponent<ARTrackedImageManager>();
             manager.maxNumberOfMovingImages = 4;
        }
        
        if (!origin.GetComponent<MultiImageVideoManager>())
        {
            origin.gameObject.AddComponent<MultiImageVideoManager>();
        }

        // AUTO-RESTORE DEFAULT PREFAB
        MultiImageVideoManager videoManager = origin.GetComponent<MultiImageVideoManager>();
        if (videoManager.defaultVideoPrefab == null)
        {
            GameObject videoPrefab = new GameObject("DefaultVideoPrefab");
            videoPrefab.SetActive(false); // Hide it, it's a template
            
            // Add Video Player
            UnityEngine.Video.VideoPlayer vp = videoPrefab.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.source = UnityEngine.Video.VideoSource.Url;
            vp.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;

            // Add the Quad for display
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(videoPrefab.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0); // Flat on image
            quad.transform.localScale = new Vector3(1, 1, 1); // normalized
            
            // Link VideoPlayer to Quad
            vp.targetMaterialRenderer = quad.GetComponent<Renderer>();

            // Add Control Script
            if (videoPrefab.GetComponent<VideoAnimControl>() == null)
                videoPrefab.AddComponent<VideoAnimControl>();

            videoManager.defaultVideoPrefab = videoPrefab;
            Debug.Log("Restored Deleted Default Video Prefab!");
        }

        RuntimeWeddingLoader loader = origin.GetComponent<RuntimeWeddingLoader>();
        if (!loader)
        {
            loader = origin.gameObject.AddComponent<RuntimeWeddingLoader>();
        }
        loader.apiBaseUrl = "https://album-x1rn.onrender.com"; // AUTO-SET URL

        // 3. Create AR Session if missing
        if (FindObjectOfType<ARSession>() == null)
        {
            GameObject sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();
        }

        // 4. Create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("MainCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // 5. Create UI Panels
        GameObject splash = CreatePanel(canvas.transform, "SplashPanel", Color.white);
        CreateText(splash.transform, "Welcome to AR Wedding", 0);

        GameObject login = CreatePanel(canvas.transform, "LoginPanel", new Color(0.95f, 0.95f, 0.95f));
        CreateText(login.transform, "Enter Client ID:", 100);
        TMP_InputField input = CreateInput(login.transform, "IdInputField");
        Button loginBtn = CreateButton(login.transform, "LoginButton", "Login", -100);

        GameObject loading = CreatePanel(canvas.transform, "LoadingPanel", new Color(0,0,0,0.8f));
        loading.SetActive(false);
        CreateText(loading.transform, "Downloading Memories...", 50);
        Slider progress = CreateSlider(loading.transform, "ProgressBar");
        TextMeshProUGUI statusTxt = CreateText(loading.transform, "Status...", -50);

        GameObject hud = CreatePanel(canvas.transform, "ARHudPanel", Color.clear);
        hud.SetActive(false);
        CreateText(hud.transform, "Point camera at photo", -200);

        GameObject error = CreatePanel(canvas.transform, "ErrorPanel", new Color(0.5f, 0, 0, 0.9f));
        error.SetActive(false);
        TextMeshProUGUI errTxt = CreateText(error.transform, "Error Occurred", 50);
        Button retryBtn = CreateButton(error.transform, "RetryButton", "Retry", -100);

        // 6. Create AppManager and Link
        AppUIManager uiManager = FindObjectOfType<AppUIManager>();
        if (uiManager == null)
        {
            GameObject managerGo = new GameObject("AppManager");
            uiManager = managerGo.AddComponent<AppUIManager>();
        }

        uiManager.splashPanel = splash;
        uiManager.loginPanel = login;
        uiManager.loadingPanel = loading;
        uiManager.arHudPanel = hud;
        uiManager.errorPanel = error;
        
        uiManager.clientIdInput = input;
        uiManager.loginButton = loginBtn;
        uiManager.retryButton = retryBtn;
        uiManager.progressBar = progress;
        uiManager.statusText = statusTxt;
        uiManager.errorText = errTxt;
        
        uiManager.weddingLoader = loader;

        Debug.Log("Scene Rebuilt Successfully!");
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero; // Full stretch
        
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    static TextMeshProUGUI CreateText(Transform parent, string content, float yOffset)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.black;
        txt.fontSize = 40;
        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yOffset);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
        return txt;
    }

    static TMP_InputField CreateInput(Transform parent, string name)
    {
        // Simplified input creation - better to drag prefab in real world but this works for rescue
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = Color.white;
        
        TMP_InputField input = obj.AddComponent<TMP_InputField>();
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 80);
        
        GameObject textObj = new GameObject("Text Area");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 10);
        textRT.offsetMax = new Vector2(-10, -10);
        
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textObj.transform, false);
        TextMeshProUGUI pTxt = placeholder.AddComponent<TextMeshProUGUI>();
        pTxt.text = "Enter ID...";
        pTxt.color = Color.gray;
        
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textObj.transform, false);
        TextMeshProUGUI tTxt = text.AddComponent<TextMeshProUGUI>();
        tTxt.text = "";
        tTxt.color = Color.black;
        
        input.textViewport = textRT;
        input.placeholder = pTxt;
        input.textComponent = tTxt;
        
        return input;
    }

    static Button CreateButton(Transform parent, string name, string label, float yOffset)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);
        
        Button btn = obj.AddComponent<Button>();
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 80);
        rt.anchoredPosition = new Vector2(0, yOffset);
        
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 32;
        txt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return btn;
    }

    static Slider CreateSlider(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Slider slider = obj.AddComponent<Slider>();
        
        // Background
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 40);
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform fillRt = fillArea.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0.25f);
        fillRt.anchorMax = new Vector2(1, 0.75f);
        fillRt.offsetMin = new Vector2(5, 0); 
        fillRt.offsetMax = new Vector2(-5, 0);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.green;
        RectTransform fRect = fill.GetComponent<RectTransform>();
        fRect.sizeDelta = Vector2.zero;

        slider.fillRect = fRect;
        
        return slider;
    }
}
#endif
