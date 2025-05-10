using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    public Vector3 PlayerSpawnPosition { get; private set; }

    
    [SerializeField] private bool debugMode = false;
    
    // Event that fires when scene loading is complete
    public event Action<string> OnSceneLoadComplete;
    
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
            Debug.Log("GameSceneManager initialized");
    }
    
    private void OnEnable()
    {
        // Register to receive scene transition responses from server
        NetworkClient.RegisterHandler<GameSceneTransitionResponseMessage>(HandleSceneTransitionResponse);
    }
    
    private void OnDisable()
    {
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<GameSceneTransitionResponseMessage>();
    }
    
    // Method for client to request scene transition
    public void RequestSceneTransition(GameScene targetScene)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogWarning("Cannot request scene transition: Not connected to server");
            return;
        }
        
        if (debugMode)
            Debug.Log($"Requesting transition to {targetScene}");
        
        // Get the selected character ID
        string characterId = ClientPlayerDataManager.Instance.SelectedCharacterId;
        
        NetworkClient.Send(new GameSceneTransitionRequestMessage
        {
            targetScene = targetScene.ToString(),
            characterId = characterId
        });
    }
    
    // Handle scene transition response from server
// Handle scene transition response from server
    public void HandleSceneTransitionResponse(GameSceneTransitionResponseMessage msg)
    {
        if (debugMode)
            Debug.Log($"Received scene transition response: Approved={msg.approved}, Scene={msg.sceneName}");
        
        if (msg.approved)
        {
            // Try to parse as a GameScene enum
            if (Enum.TryParse<GameScene>(msg.sceneName, out GameScene targetScene))
            {
                if (debugMode)
                    Debug.Log($"Loading game scene: {targetScene}");
                
                // Store the spawn position for later use when spawning the player
                PlayerSpawnPosition = msg.spawnPosition;
                
                // Load the scene
                SceneManager.LoadScene(targetScene.ToString());
            }
            else
            {
                Debug.LogError($"Invalid scene name received: {msg.sceneName}");
            }
        }
        else
        {
            Debug.LogWarning($"Scene transition denied: {msg.message}");
        }
    }
    // Load a game scene
    public void LoadGameScene(GameScene targetScene)
    {
        if (debugMode)
            Debug.Log($"Loading game scene: {targetScene}");
        
        // Load the scene
        SceneManager.LoadScene(targetScene.ToString());
        
        // Notify subscribers that scene loading is complete
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unregister to prevent multiple calls
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (debugMode)
            Debug.Log($"Scene loaded: {scene.name}");
        
        // Notify subscribers
        OnSceneLoadComplete?.Invoke(scene.name);
    }
}