using UnityEngine;
using Mirror;
using System;
using kcp2k;
using System.Collections;
using UnityEngine.SceneManagement;

// Handles server initialization, configuration and startup
public class ServerBootstrapper : MonoBehaviour
{
    // Server configuration
    [SerializeField] private NetworkManager networkManager;  // Reference to Mirror's NetworkManager
    [SerializeField] private int serverPort = 7777;          // Default server port
    [SerializeField] private bool autoStartServer = true;    // Whether to start the server automatically
    [SerializeField] private bool preloadGameScenes = true;  // Whether to preload game scenes on startup

    // Initializes and starts the server
    private void Start()
    {
        Debug.Log("Server Bootstrapper starting...");

#if DEDICATED_SERVER
        // Find NetworkManager if not set in inspector
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (networkManager != null)
        {
            ConfigureTransport();                            // Configure transport with port settings
            ParseCommandLineArgs();                          // Check for command line overrides

            // Start server automatically if configured
            if (autoStartServer)
            {
                Debug.Log("Auto-starting server...");
                networkManager.StartServer();                // Start the Mirror server
                Debug.Log("Server started!");

                if (preloadGameScenes)
                {
                    StartCoroutine(PreloadGameScenes());     // Preload game scenes if enabled
                }
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Cannot start server.");
        }
#endif
    }

    // Sets up the network transport with the correct port
    private void ConfigureTransport()
    {
        Transport transport = Transport.active;
        if (transport != null && transport is TelepathyTransport telepathyTransport)
        {
            telepathyTransport.port = (ushort)serverPort;    // Configure Telepathy port
            Debug.Log($"Telepathy transport configured on port: {serverPort}");
        }
        else if (transport != null && transport is KcpTransport kcpTransport)
        {
            kcpTransport.Port = (ushort)serverPort;          // Configure KCP port
            Debug.Log($"KCP transport configured on port: {serverPort}");
        }
        else
        {
            Debug.LogWarning("Unknown transport type or no transport active.");
        }
    }

    // Loads all game scenes additively to improve scene switching
    private IEnumerator PreloadGameScenes()
    {
        Debug.Log("Starting to preload all game scenes...");

        string[] gameScenes = Enum.GetNames(typeof(GameScene));  // Get scene names from enum

        foreach (string sceneName in gameScenes)
        {
            Debug.Log($"Loading scene: {sceneName}");

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);  // Load scene
            yield return loadOp;                             // Wait for scene load to complete

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

            yield return null;                               // Wait one frame before next scene
        }

        Debug.Log($"Finished loading all game scenes. Total loaded: {gameScenes.Length}");
    }

    // Processes command line arguments (-port, -noAutoStart, -noPreload)
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
                        serverPort = port;                   // Override default port
                        Debug.Log($"Setting server port from command line: {port}");
                    }
                    break;

                case "-noAutoStart":
                    autoStartServer = false;                 // Disable auto-starting
                    Debug.Log("Auto-start disabled from command line");
                    break;

                case "-noPreload":
                    preloadGameScenes = false;               // Disable scene preloading
                    Debug.Log("Scene preloading disabled from command line");
                    break;

                    // Add more command-line arguments as needed
            }
        }
    }
}