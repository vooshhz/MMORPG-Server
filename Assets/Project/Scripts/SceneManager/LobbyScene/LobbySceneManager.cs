using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class LobbySceneManager : MonoBehaviour
{
    public static LobbySceneManager Instance { get; private set; } // Singleton instance for global access
    
    [SerializeField] private bool debugMode = false; // Toggle for debug logging
    
    private void Awake()
    {
        if (Instance != null && Instance != this) // Check if instance already exists
        {
            Destroy(gameObject); // Destroy duplicate
            return;
        }
        
        Instance = this; // Set this as the instance
        DontDestroyOnLoad(gameObject); // Persist across scene loads
        
        if (debugMode)
            Debug.Log("LobbySceneManager initialized"); // Debug log initialization
    }
    
    private void OnEnable()
    {
        NetworkClient.RegisterHandler<LobbySceneTransitionResponseMessage>(HandleSceneTransitionResponse); // Register network message handler
    }
    
    private void OnDisable()
    {
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<LobbySceneTransitionResponseMessage>(); // Unregister handler when disabled
    }
    
    public void RequestSceneTransition(LobbyScene targetScene)
    {
        if (!NetworkClient.isConnected) // Check network connection
        {
            Debug.LogWarning("Cannot request scene transition: Not connected to server"); // Warn about no connection
            
            // For login scene, load directly without server approval
            if (targetScene == LobbyScene.LoginScene) // Special case for login scene
            {
                PerformSceneTransition(targetScene); // Load login scene directly
                return;
            }
            
            return;
        }
        
        if (debugMode)
            Debug.Log($"Requesting transition to {targetScene}"); // Debug log request
        
        NetworkClient.Send(new LobbySceneTransitionRequestMessage // Send scene change request to server
        {
            targetScene = targetScene.ToString() // Convert enum to string
        });
    }
    
    private void HandleSceneTransitionResponse(LobbySceneTransitionResponseMessage msg)
    {
        if (debugMode)
            Debug.Log($"Received scene transition response: Approved={msg.approved}, Scene={msg.sceneName}"); // Debug log response
        
        if (msg.approved) // Check if server approved
        {
            if (Enum.TryParse<LobbyScene>(msg.sceneName, out LobbyScene targetScene)) // Parse scene name to enum
            {
                PerformSceneTransition(targetScene); // Load the approved scene
            }
            else
            {
                Debug.LogError($"Invalid scene name received: {msg.sceneName}"); // Error for invalid scene
            }
        }
        else
        {
            Debug.LogWarning($"Scene transition to {msg.sceneName} was denied by server: {msg.message}"); // Warn about denial
        }
    }
    
    private void PerformSceneTransition(LobbyScene targetScene)
    {
        if (debugMode)
            Debug.Log($"Performing transition to {targetScene}"); // Debug log transition
        
        // Direct scene loading without fading
        SceneManager.LoadScene(targetScene.ToString()); // Load the scene
    }
}