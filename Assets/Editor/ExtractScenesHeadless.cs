using UnityEngine;
using UnityEditor;

public class ExtractScenesHeadless
{
    [MenuItem("Tools/Extract All Scenes Headless")]
    public static void ExtractAllScenes()
    {
        SceneHierarchyExtractor.ExtractScenesHeadless(
            "Assets/SceneHierarchy",  // Output directory
            true,                      // Include inactive objects
            true                       // Include transform details
        );
    }
    
    // Example with scene filtering
    [MenuItem("Tools/Extract Game Scenes Only")]
    public static void ExtractGameScenesOnly()
    {
        SceneHierarchyExtractor.ExtractScenesHeadless(
            "Assets/SceneHierarchy/GameScenes",
            true,
            false,
            (scenePath) => 
            {
                // Filter to only include game scenes
                return !scenePath.Contains("Editor") && 
                       !scenePath.Contains("Test") &&
                       !scenePath.Contains("Tutorial");
            }
        );
    }
    
    // You can also run this from command line using Unity's batch mode:
    // Unity.exe -batchmode -projectPath "C:/Path/To/Project" -executeMethod ExtractScenesHeadless.ExtractAllScenes -quit
    
    // For CI/CD pipelines and build servers:
    [MenuItem("Tools/CI Extract Scenes")]
    public static void CIExtractScenes()
    {
        string outputDir = "Build/SceneHierarchy";
        
        // For CI environments, you might want to use an environment variable or command line arg for the output path
        string ciOutputPath = System.Environment.GetEnvironmentVariable("SCENE_OUTPUT_PATH");
        if (!string.IsNullOrEmpty(ciOutputPath))
        {
            outputDir = ciOutputPath;
        }
        
        Debug.Log($"Starting CI scene extraction to {outputDir}");
        SceneHierarchyExtractor.ExtractScenesHeadless(outputDir, true, true);
        Debug.Log("CI scene extraction complete");
    }
}