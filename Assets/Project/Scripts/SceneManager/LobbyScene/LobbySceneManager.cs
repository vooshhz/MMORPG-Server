using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class LobbySceneManager : MonoBehaviour
{
    public static LobbySceneManager Instance { get; private set; }
    
    [SerializeField] private bool debugMode = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (debugMode)
            Debug.Log("LobbySceneManager initialized");
    }
    
    private void OnEnable()
    {
        // Register for network messages
        NetworkClient.RegisterHandler<LobbySceneTransitionResponseMessage>(HandleSceneTransitionResponse);
    }
    
    private void OnDisable()
    {
        // Unregister to prevent memory leaks
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<LobbySceneTransitionResponseMessage>();
    }
    
    // Request a scene transition from the server
    public void RequestSceneTransition(LobbyScene targetScene)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogWarning("Cannot request scene transition: Not connected to server");
            
            // For the login scene specifically, we can load it directly without server approval
            if (targetScene == LobbyScene.LoginScene)
            {
                PerformSceneTransition(targetScene);
                return;
            }
            
            return;
        }
        
        if (debugMode)
            Debug.Log($"Requesting transition to {targetScene}");
        
        // Send request to server
        NetworkClient.Send(new LobbySceneTransitionRequestMessage
        {
            targetScene = targetScene.ToString()
        });
    }
    
    // Handler for server response
    private void HandleSceneTransitionResponse(LobbySceneTransitionResponseMessage msg)
    {
        if (debugMode)
            Debug.Log($"Received scene transition response: Approved={msg.approved}, Scene={msg.sceneName}");
        
        if (msg.approved)
        {
            // Parse the scene name to enum
            if (Enum.TryParse<LobbyScene>(msg.sceneName, out LobbyScene targetScene))
            {
                PerformSceneTransition(targetScene);
            }
            else
            {
                Debug.LogError($"Invalid scene name received: {msg.sceneName}");
            }
        }
        else
        {
            Debug.LogWarning($"Scene transition to {msg.sceneName} was denied by server: {msg.message}");
        }
    }
    
    // Actually perform the scene transition
    private void PerformSceneTransition(LobbyScene targetScene)
    {
        if (debugMode)
            Debug.Log($"Performing transition to {targetScene}");
        
        // Load the scene
        SceneManager.LoadScene(targetScene.ToString());
    }
}