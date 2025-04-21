using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SceneManagement;

public class SceneHierarchyExtractor : EditorWindow
{
    private string outputPath = "Assets/SceneHierarchy";
    private bool includeInactiveObjects = true;
    private bool includeTransformDetails = false;
    private string[] scenePaths;
    private bool[] sceneSelections;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Scene Hierarchy Extractor")]
    public static void ShowWindow()
    {
        GetWindow<SceneHierarchyExtractor>("Scene Extractor");
    }

    private void OnEnable()
    {
        RefreshSceneList();
    }

    private void RefreshSceneList()
    {
        // Get all scenes in the project
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        List<string> paths = new List<string>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            paths.Add(path);
        }
        
        scenePaths = paths.ToArray();
        sceneSelections = new bool[scenePaths.Length];
        
        // Default to all scenes selected
        for (int i = 0; i < sceneSelections.Length; i++)
        {
            sceneSelections[i] = true;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Extract Scene Hierarchy Info", EditorStyles.boldLabel);
        
        // Output path
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Directory:", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Directory", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to project relative path if inside the project
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                outputPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Options
        includeInactiveObjects = EditorGUILayout.Toggle("Include Inactive Objects", includeInactiveObjects);
        includeTransformDetails = EditorGUILayout.Toggle("Include Transform Details", includeTransformDetails);
        
        // Scene selection
        EditorGUILayout.Space();
        GUILayout.Label("Select Scenes to Extract:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Scene List"))
        {
            RefreshSceneList();
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (int i = 0; i < sceneSelections.Length; i++)
            {
                sceneSelections[i] = true;
            }
        }
        if (GUILayout.Button("Select None"))
        {
            for (int i = 0; i < sceneSelections.Length; i++)
            {
                sceneSelections[i] = false;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Scene list with checkboxes
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < scenePaths.Length; i++)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePaths[i]);
            sceneSelections[i] = EditorGUILayout.ToggleLeft($"{sceneName} ({scenePaths[i]})", sceneSelections[i]);
        }
        EditorGUILayout.EndScrollView();
        
        // Process button
        EditorGUILayout.Space();
        if (GUILayout.Button("Extract Selected Scenes"))
        {
            ExtractSelectedScenes();
        }
        
        // Headless option
        EditorGUILayout.Space();
        GUILayout.Label("Headless Extraction (via script):", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("To run this tool headless, call the static method:\nSceneHierarchyExtractor.ExtractScenesHeadless(outputPath, includeInactive, includeTransform, sceneFilter)", MessageType.Info);
    }
    
    private void ExtractSelectedScenes()
    {
        List<string> selectedScenes = new List<string>();
        for (int i = 0; i < scenePaths.Length; i++)
        {
            if (sceneSelections[i])
            {
                selectedScenes.Add(scenePaths[i]);
            }
        }
        
        if (selectedScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("No Scenes Selected", "Please select at least one scene to extract.", "OK");
            return;
        }
        
        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        // Remember the current scene
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        bool sceneWasModified = EditorSceneManager.GetActiveScene().isDirty;
        
        try
        {
            // Extract each scene
            foreach (string scenePath in selectedScenes)
            {
                ExtractSceneHierarchy(scenePath, outputPath, includeInactiveObjects, includeTransformDetails);
            }
            
            EditorUtility.DisplayDialog("Extraction Complete", 
                $"Successfully extracted {selectedScenes.Count} scene hierarchies to:\n{outputPath}", "OK");
                
            // Show in explorer/finder
            EditorUtility.RevealInFinder(outputPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during scene extraction: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"An error occurred during extraction: {e.Message}", "OK");
        }
        finally
        {
            // Restore the original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                if (!sceneWasModified)
                {
                    // Since the scene wasn't modified before, we're restoring that state
                    // First mark as clean by reloading without changes
                    EditorSceneManager.OpenScene(EditorSceneManager.GetActiveScene().path, OpenSceneMode.Single);
                }
            }
        }
    }
    
    // Static method for headless operation
    public static void ExtractScenesHeadless(string outputDir, bool includeInactive = true, 
                                            bool includeTransform = false, System.Func<string, bool> sceneFilter = null)
    {
        Debug.Log($"Starting headless scene hierarchy extraction to: {outputDir}");
        
        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        
        // Get all scenes in the project
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        List<string> scenePaths = new List<string>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (sceneFilter == null || sceneFilter(path))
            {
                scenePaths.Add(path);
            }
        }
        
        if (scenePaths.Count == 0)
        {
            Debug.LogWarning("No scenes found that match the filter criteria.");
            return;
        }
        
        // Remember the current scene
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        bool sceneWasModified = EditorSceneManager.GetActiveScene().isDirty;
        
        try
        {
            // Extract each scene
            foreach (string scenePath in scenePaths)
            {
                ExtractSceneHierarchy(scenePath, outputDir, includeInactive, includeTransform);
            }
            
            Debug.Log($"Successfully extracted {scenePaths.Count} scene hierarchies to: {outputDir}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during scene extraction: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            // Restore the original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                if (!sceneWasModified)
                {
                    // Since the scene wasn't modified before, we're restoring that state
                    // First mark as clean by reloading without changes
                    EditorSceneManager.OpenScene(EditorSceneManager.GetActiveScene().path, OpenSceneMode.Single);
                }
            }
        }
    }
    
    private static void ExtractSceneHierarchy(string scenePath, string outputDir, bool includeInactive, bool includeTransform)
    {
        Debug.Log($"Processing scene: {scenePath}");
        
        // Open the scene
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Get the scene name without extension
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string outputPath = Path.Combine(outputDir, $"{sceneName}_hierarchy.txt");
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Scene: {sceneName} ({scenePath})");
        sb.AppendLine("Extraction Time: " + System.DateTime.Now.ToString());
        sb.AppendLine("------------------------------------------------------");
        sb.AppendLine();
        
        // Extract all root GameObjects in the scene
        GameObject[] rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObj in rootObjects)
        {
            ProcessGameObject(rootObj, 0, sb, includeInactive, includeTransform);
        }
        
        // Write to file
        File.WriteAllText(outputPath, sb.ToString());
        Debug.Log($"Extracted hierarchy to: {outputPath}");
    }
    
    private static void ProcessGameObject(GameObject obj, int depth, StringBuilder sb, bool includeInactive, bool includeTransform)
    {
        // Skip inactive objects if specified
        if (!includeInactive && !obj.activeSelf)
            return;
        
        // Indent based on depth
        string indent = new string(' ', depth * 4);
        
        // Object details
        string activeState = obj.activeSelf ? "[Active]" : "[Inactive]";
        sb.AppendLine($"{indent}+ {obj.name} {activeState}");
        
        // Transform details
        if (includeTransform)
        {
            sb.AppendLine($"{indent}  Position: {obj.transform.localPosition}");
            sb.AppendLine($"{indent}  Rotation: {obj.transform.localEulerAngles}");
            sb.AppendLine($"{indent}  Scale: {obj.transform.localScale}");
        }
        
        // Component list
        Component[] components = obj.GetComponents<Component>();
        if (components.Length > 0)
        {
            sb.AppendLine($"{indent}  Components:");
            foreach (Component component in components)
            {
                // Skip Transform as it's on every GameObject
                if (component is Transform)
                    continue;
                
                if (component != null)
                {
                    string componentName = component.GetType().Name;
                    sb.AppendLine($"{indent}    - {componentName}");
                }
                else
                {
                    sb.AppendLine($"{indent}    - [Missing Script]");
                }
            }
        }
        
        // Process children
        foreach (Transform child in obj.transform)
        {
            ProcessGameObject(child.gameObject, depth + 1, sb, includeInactive, includeTransform);
        }
    }
}