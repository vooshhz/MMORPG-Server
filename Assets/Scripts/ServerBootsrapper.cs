using UnityEngine;
using Mirror;
using System;
using kcp2k;

public class ServerBootstrapper : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private int serverPort = 7777;
    [SerializeField] private bool autoStartServer = true;
    
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
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Cannot start server.");
        }
        #endif
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
                    
                // Add more command-line arguments as needed
            }
        }
    }
}