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
        NetworkClient.RegisterHandler<LobbySceneTransitionResponseMessage>(HandleSceneTransitionResponse);
    }
    
    private void OnDisable()
    {
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<LobbySceneTransitionResponseMessage>();
    }
    
    public void RequestSceneTransition(LobbyScene targetScene)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogWarning("Cannot request scene transition: Not connected to server");
            
            // For login scene, load directly without server approval
            if (targetScene == LobbyScene.LoginScene)
            {
                PerformSceneTransition(targetScene);
                return;
            }
            
            return;
        }
        
        if (debugMode)
            Debug.Log($"Requesting transition to {targetScene}");
        
        NetworkClient.Send(new LobbySceneTransitionRequestMessage
        {
            targetScene = targetScene.ToString()
        });
    }
    
    private void HandleSceneTransitionResponse(LobbySceneTransitionResponseMessage msg)
    {
        if (debugMode)
            Debug.Log($"Received scene transition response: Approved={msg.approved}, Scene={msg.sceneName}");
        
        if (msg.approved)
        {
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
    
    private void PerformSceneTransition(LobbyScene targetScene)
    {
        if (debugMode)
            Debug.Log($"Performing transition to {targetScene}");
        
        // Direct scene loading without fading
        SceneManager.LoadScene(targetScene.ToString());
    }
}