#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to help migrate from old AR tracking scripts to enhanced versions.
/// Access via: Tools > AR Tracking > Migration Helper
/// </summary>
public class ARTrackingMigrationHelper : EditorWindow
{
    private GameObject arSessionOrigin;
    private GameObject videoPrefab;
    private bool migrateManager = true;
    private bool migrateVideoControl = true;
    private bool migrateButton = true;
    private bool keepOldComponents = true;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/AR Tracking/Migration Helper")]
    public static void ShowWindow()
    {
        var window = GetWindow<ARTrackingMigrationHelper>("AR Migration Helper");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("AR Tracking Migration Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool helps you migrate from the original AR tracking scripts to the enhanced versions. " +
            "It will copy settings and references from old components to new ones.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Step 1: Select AR Session Origin
        GUILayout.Label("Step 1: Select AR Session Origin", EditorStyles.boldLabel);
        arSessionOrigin = (GameObject)EditorGUILayout.ObjectField(
            "AR Session Origin",
            arSessionOrigin,
            typeof(GameObject),
            true
        );
        
        GUILayout.Space(10);
        
        // Step 2: Select Video Prefab
        GUILayout.Label("Step 2: Select Video Prefab", EditorStyles.boldLabel);
        videoPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Video Prefab",
            videoPrefab,
            typeof(GameObject),
            false
        );
        
        GUILayout.Space(10);
        
        // Step 3: Migration Options
        GUILayout.Label("Step 3: Migration Options", EditorStyles.boldLabel);
        migrateManager = EditorGUILayout.Toggle("Migrate Manager", migrateManager);
        migrateVideoControl = EditorGUILayout.Toggle("Migrate Video Control", migrateVideoControl);
        migrateButton = EditorGUILayout.Toggle("Migrate Button", migrateButton);
        keepOldComponents = EditorGUILayout.Toggle("Keep Old Components (Disabled)", keepOldComponents);
        
        GUILayout.Space(20);
        
        // Migration buttons
        GUI.enabled = arSessionOrigin != null;
        if (GUILayout.Button("Migrate AR Session Origin", GUILayout.Height(30)))
        {
            MigrateARSessionOrigin();
        }
        
        GUI.enabled = videoPrefab != null;
        if (GUILayout.Button("Migrate Video Prefab", GUILayout.Height(30)))
        {
            MigrateVideoPrefab();
        }
        
        GUI.enabled = arSessionOrigin != null && videoPrefab != null;
        if (GUILayout.Button("Migrate Everything", GUILayout.Height(40)))
        {
            MigrateEverything();
        }
        
        GUI.enabled = true;
        
        GUILayout.Space(20);
        
        // Validation
        if (GUILayout.Button("Validate Setup", GUILayout.Height(30)))
        {
            ValidateSetup();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tip: Always backup your project before migration!",
            MessageType.Warning
        );
        
        EditorGUILayout.EndScrollView();
    }
    
    private void MigrateARSessionOrigin()
    {
        if (arSessionOrigin == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an AR Session Origin!", "OK");
            return;
        }
        
        if (!migrateManager)
        {
            EditorUtility.DisplayDialog("Info", "Manager migration is disabled in options.", "OK");
            return;
        }
        
        Undo.RecordObject(arSessionOrigin, "Migrate AR Session Origin");
        
        var oldManager = arSessionOrigin.GetComponent<MultiImageVideoManager>();
        if (oldManager == null)
        {
            EditorUtility.DisplayDialog("Info", "No MultiImageVideoManager found on this object.", "OK");
            return;
        }
        
        // Add enhanced manager
        var newManager = arSessionOrigin.GetComponent<ImageVideoManager>();
        if (newManager == null)
        {
            newManager = arSessionOrigin.AddComponent<ImageVideoManager>();
        }
        
        // Copy settings
        newManager.imageVideoMappings.Clear();
        foreach (var oldMapping in oldManager.imageVideoMappings)
        {
            var newMapping = new ImageVideoManager.ImageVideoMapping
            {
                imageName = oldMapping.imageName,
                videoPrefab = oldMapping.videoPrefab,
                videoSource = oldMapping.videoSource
            };
            newManager.imageVideoMappings.Add(newMapping);
        }
        
        newManager.defaultVideoPrefab = oldManager.defaultVideoPrefab;
        newManager.destroyOnImageLost = oldManager.destroyOnImageLost;
        newManager.disableOnImageLost = oldManager.disableOnImageLost;
        
        // Disable old manager
        if (keepOldComponents)
        {
            oldManager.enabled = false;
        }
        else
        {
            DestroyImmediate(oldManager);
        }
        
        EditorUtility.SetDirty(arSessionOrigin);
        Debug.Log("[Migration] Successfully migrated AR Session Origin!");
        EditorUtility.DisplayDialog("Success", "AR Session Origin migrated successfully!", "OK");
    }
    
    private void MigrateVideoPrefab()
    {
        if (videoPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a Video Prefab!", "OK");
            return;
        }
        
        string prefabPath = AssetDatabase.GetAssetPath(videoPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "Selected object is not a prefab!", "OK");
            return;
        }
        
        // Open prefab for editing
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        
        bool migrated = false;
        
        // Migrate Video Control
        if (migrateVideoControl)
        {
            var oldControl = prefabContents.GetComponent<VideoAnimControl>();
            if (oldControl != null)
            {
                var newControl = prefabContents.GetComponent<VideoController>();
                if (newControl == null)
                {
                    newControl = prefabContents.AddComponent<VideoController>();
                }
                
                // Copy settings
                newControl.videoPlayer = oldControl.videoPlayer;
                newControl.videoPlane = oldControl.videoPlane;
                newControl.trackedImage = oldControl.trackedImage;
                newControl.videoHeightOffset = oldControl.videoHeightOffset;
                newControl.videoScaleMultiplier = oldControl.videoScaleMultiplier;
                newControl.audioFadeTime = oldControl.audioFadeTime;
                
                if (keepOldComponents)
                {
                    oldControl.enabled = false;
                }
                else
                {
                    DestroyImmediate(oldControl);
                }
                
                migrated = true;
                Debug.Log("[Migration] Migrated VideoAnimControl");
            }
        }
        
        // Migrate Button
        if (migrateButton)
        {
            var oldButton = prefabContents.GetComponentInChildren<ArButton>();
            if (oldButton != null)
            {
                var buttonObj = oldButton.gameObject;
                var newButton = buttonObj.GetComponent<ARButton>();
                if (newButton == null)
                {
                    newButton = buttonObj.AddComponent<ARButton>();
                }
                
                // Copy settings
                newButton.videoPlayer = oldButton.videoPlayer;
                
                if (keepOldComponents)
                {
                    oldButton.enabled = false;
                }
                else
                {
                    DestroyImmediate(oldButton);
                }
                
                migrated = true;
                Debug.Log("[Migration] Migrated ArButton");
            }
        }
        
        if (migrated)
        {
            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[Migration] Successfully migrated Video Prefab!");
            EditorUtility.DisplayDialog("Success", "Video Prefab migrated successfully!", "OK");
        }
        else
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
            EditorUtility.DisplayDialog("Info", "No components to migrate in this prefab.", "OK");
        }
    }
    
    private void MigrateEverything()
    {
        if (EditorUtility.DisplayDialog(
            "Migrate Everything",
            "This will migrate both the AR Session Origin and Video Prefab. Continue?",
            "Yes", "Cancel"))
        {
            MigrateARSessionOrigin();
            MigrateVideoPrefab();
            
            Debug.Log("[Migration] Full migration complete!");
            EditorUtility.DisplayDialog("Success", "Full migration complete! Please test your setup.", "OK");
        }
    }
    
    private void ValidateSetup()
    {
        if (arSessionOrigin == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an AR Session Origin first!", "OK");
            return;
        }
        
        var validator = arSessionOrigin.GetComponent<ARTrackingValidator>();
        if (validator == null)
        {
            validator = arSessionOrigin.AddComponent<ARTrackingValidator>();
        }
        
        validator.ValidateSetup();
        
        EditorUtility.DisplayDialog(
            "Validation Complete",
            "Check the Console for validation results.",
            "OK"
        );
    }
}
#endif
