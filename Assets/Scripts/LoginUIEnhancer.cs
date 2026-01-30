using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Enhances Login UI visuals and auto-fixes common Input/EventSystem issues.
/// </summary>
public class LoginUIEnhancer : MonoBehaviour
{
    [Header("References")]
    public Image backgroundPanel;
    public TMP_InputField inputField;
    public Button loginButton;
    
    [Header("Style Settings")]
    public Color gradientTop = new Color(0.1f, 0.2f, 0.4f);
    public Color gradientBottom = new Color(0.05f, 0.1f, 0.2f);

    void Awake()
    {
        // CRITICAL: Fix Input System before anything else
        EnsureEventSystem();
        EnsureCanvasRaycaster();
    }

    void Start()
    {
        SetupInput();
        // Background styling removed as requested
    }

    void EnsureEventSystem()
    {
        EventSystem currentEs = EventSystem.current;
        if (currentEs == null)
        {
            currentEs = FindObjectOfType<EventSystem>();
            if (currentEs == null)
            {
                Debug.Log("[LoginUIEnhancer] Creating missing EventSystem...");
                GameObject go = new GameObject("EventSystem");
                currentEs = go.AddComponent<EventSystem>();
            }
        }
        
        // Ensure StandaloneInputModule is present
        if (currentEs.GetComponent<StandaloneInputModule>() == null)
        {
             // Try to detect if the conflicting New Input System module is present
             System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
             if (newModuleType != null && currentEs.GetComponent(newModuleType) != null)
             {
                 Debug.LogWarning("[LoginUIEnhancer] Found 'InputSystemUIInputModule'. If UI is not responsive, it might be due to missing Action Assets. Attempting to add StandaloneInputModule as fallback.");
             }
             
             Debug.Log("[LoginUIEnhancer] Adding StandaloneInputModule to EventSystem.");
             currentEs.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    void EnsureCanvasRaycaster()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.Log("[LoginUIEnhancer] Adding missing GraphicRaycaster to Canvas.");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    void SetupInput()
    {
        if (inputField != null)
        {
            // CRITICAL for Mobile Keyboards
            inputField.shouldHideMobileInput = false; 
            inputField.keyboardType = TouchScreenKeyboardType.Default;
            
            // Force Re-enable to reset state
            inputField.interactable = false;
            inputField.interactable = true;
            
            // Ensure proper events
            inputField.onSelect.AddListener((val) => {
                Debug.Log("[LoginUI] Input Selected: " + val);
            });
        }
        
        if (loginButton != null)
        {
             // Add a click listener for debugging
             loginButton.onClick.AddListener(() => Debug.Log("[LoginUI] Login Button Clicked"));
        }
    }
}
