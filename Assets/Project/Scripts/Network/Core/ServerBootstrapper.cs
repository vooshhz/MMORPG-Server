using UnityEngine;
using Mirror;
using System;
using kcp2k;
using System.Collections;
using UnityEngine.SceneManagement;

public class ServerBootstrapper : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private int serverPort = 7777;
    [SerializeField] private bool autoStartServer = true;
    [SerializeField] private bool preloadGameScenes = true;
    
    private void Start()
    {
        Debug.Log("Server Bootstrapper starting...");
        
        #if DEDICATED_SERVER
        // Configure server settings
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();
            
        if (networkManager != null)
        {
            // Configure network settings
            Transport transport = Transport.active;
            if (transport != null && transport is TelepathyTransport telepathyTransport)
            {
                telepathyTransport.port = (ushort)serverPort;
                Debug.Log($"Server configured on port: {serverPort}");
            }
            else if (transport != null && transport is KcpTransport kcpTransport)
            {
                kcpTransport.Port = (ushort)serverPort;
                Debug.Log($"Server configured on port: {serverPort}");
            }
            
            // Parse command-line arguments
            ParseCommandLineArgs();
            
            // Start server automatically if configured
            if (autoStartServer)
            {
                Debug.Log("Auto-starting server...");
                networkManager.StartServer();
                Debug.Log("Server started!");
                
                // Preload game scenes after server has started
                if (preloadGameScenes)
                {
                    StartCoroutine(PreloadGameScenes());
                }
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Cannot start server.");
        }
        #endif
    }
    
    private IEnumerator PreloadGameScenes()
    {
        Debug.Log("Starting to preload all game scenes...");
        
        // Get all game scene names from the enum
        string[] gameScenes = Enum.GetNames(typeof(GameScene));
        
        // Load each scene additively
        foreach (string sceneName in gameScenes)
        {
            Debug.Log($"Loading scene: {sceneName}");
            
            // Load the scene additively
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            // Wait for scene to load
            yield return loadOp;
            
            // Check if successfully loaded
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.isLoaded)
            {
                Debug.Log($"Successfully loaded scene: {sceneName}");
            }
            else
            {
                Debug.LogError($"Failed to load scene: {sceneName}");
            }
            
            // Small delay before next scene
            yield return null;
        }
        
        Debug.Log($"Finished loading all game scenes. Total loaded: {gameScenes.Length}");
    }
    
    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                    {
                        serverPort = port;
                        Debug.Log($"Setting server port from command line: {port}");
                    }
                    break;
                    
                case "-noAutoStart":
                    autoStartServer = false;
                    Debug.Log("Auto-start disabled from command line");
                    break;
                
                case "-noPreload":
                    preloadGameScenes = false;
                    Debug.Log("Scene preloading disabled from command line");
                    break;
                    
                // Add more command-line arguments as needed
            }
        }
    }
}