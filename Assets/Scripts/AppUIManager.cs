using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Manages the application flow: Splash -> Login -> Loading -> AR.
/// Handles UI transitions and connects with RuntimeWeddingLoader.
/// </summary>
public class AppUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject splashPanel;
    public GameObject loginPanel;
    public GameObject loadingPanel;
    public GameObject arHudPanel;
    public GameObject errorPanel;

    [Header("UI Elements")]
    public TMP_InputField clientIdInput;
    public Button loginButton;
    public Button retryButton;
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI errorText;

    [Header("Dependencies")]
    public RuntimeWeddingLoader weddingLoader;

    private const string PREF_CLIENT_ID = "CLIENT_ID";
    private float minSplashTime = 2.0f;

    void Start()
    {
        if (weddingLoader == null) weddingLoader = FindObjectOfType<RuntimeWeddingLoader>();

        // Safety Checks
        if (splashPanel == null || loginPanel == null || loadingPanel == null)
        {
            Debug.LogError("[AppUIManager] CRITICAL: UI Panels are not assigned in the Inspector! Please assign Splash, Login, and Loading panels.");
            return;
        }

        // Subscribe to events
        weddingLoader.OnProgressUpdated += UpdateProgress;
        weddingLoader.OnAssetsLoaded += OnAssetsReady;
        weddingLoader.OnError += OnError;
        weddingLoader.OnRetryPossible += OnRetryPossible;

        // UI Setup
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);

        // Start Flow
        StartCoroutine(StartAppFlow());
    }

    private IEnumerator StartAppFlow()
    {
        // 1. Show Splash
        ShowPanel(splashPanel);
        yield return new WaitForSeconds(minSplashTime);

        // 2. Check Login
        string savedId = PlayerPrefs.GetString(PREF_CLIENT_ID, "");
        if (string.IsNullOrEmpty(savedId))
        {
            ShowPanel(loginPanel);
        }
        else
        {
            StartLoading(savedId);
        }
    }

    public void OnLoginClicked()
    {
        string inputId = clientIdInput.text.Trim();
        if (!string.IsNullOrEmpty(inputId))
        {
            PlayerPrefs.SetString(PREF_CLIENT_ID, inputId);
            PlayerPrefs.Save();
            StartLoading(inputId);
        }
    }

    private void StartLoading(string clientId)
    {
        ShowPanel(loadingPanel);
        statusText.text = "Initializing...";
        progressBar.value = 0;
        
        // Use the new explicit load method
        weddingLoader.LoadForClient(clientId);
    }

    private void OnRetryClicked()
    {
        ShowPanel(loadingPanel);
        weddingLoader.Retry();
    }

    private void UpdateProgress(float progress)
    {
        progressBar.value = progress;
        statusText.text = $"Downloading memories... {(int)(progress * 100)}%";
    }

    private void OnAssetsReady(UnityEngine.XR.ARSubsystems.MutableRuntimeReferenceImageLibrary lib, string path)
    {
        statusText.text = "Ready!";
        StartCoroutine(TransitionToAR());
    }

    private IEnumerator TransitionToAR()
    {
        yield return new WaitForSeconds(0.5f);
        // Fade out loading? For now just switch.
        ShowPanel(arHudPanel);
    }

    private void OnError(string msg)
    {
        Debug.LogError($"[AppUIManager] Error: {msg}");
        // Only show error panel if we aren't in a retry-possible state handled by loader
        // But Loader fires OnError for everything. 
        // We will update text, but wait for RetryPossible to enable button.
        errorText.text = msg;
        ShowPanel(errorPanel);
        retryButton.interactable = false; // Wait for signal
    }

    private void OnRetryPossible()
    {
        retryButton.interactable = true;
    }

    public void OnLogout()
    {
        PlayerPrefs.DeleteKey(PREF_CLIENT_ID);
        ShowPanel(loginPanel);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if (splashPanel) splashPanel.SetActive(false);
        if (loginPanel) loginPanel.SetActive(false);
        if (loadingPanel) loadingPanel.SetActive(false);
        if (arHudPanel) arHudPanel.SetActive(false);
        if (errorPanel) errorPanel.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
    }
}
