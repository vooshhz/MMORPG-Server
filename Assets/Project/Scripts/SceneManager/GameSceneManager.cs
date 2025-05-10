using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    public Vector3 PlayerSpawnPosition { get; private set; }
    
    [SerializeField] private bool debugMode = false;
    
    // Add this field to store the expected scene name
    private string loadedSceneName;
    
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
    public void HandleSceneTransitionResponse(GameSceneTransitionResponseMessage msg)
    {
        if (debugMode)
            Debug.Log($"Received scene transition response: Approved={msg.approved}, Scene={msg.sceneName}");
        
        if (msg.approved)
        {
            // Store the scene name for confirmation check
            loadedSceneName = msg.sceneName;
            
            // Try to parse as a GameScene enum
            if (Enum.TryParse<GameScene>(msg.sceneName, out GameScene targetScene))
            {
                if (debugMode)
                    Debug.Log($"Loading game scene: {targetScene}");
                
                // Store the spawn position for later use when spawning the player
                PlayerSpawnPosition = msg.spawnPosition;
                
                // Register for scene loaded event
                SceneManager.sceneLoaded += OnSceneLoaded;
                
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
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unregister to prevent multiple calls
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (debugMode)
            Debug.Log($"Scene loaded: {scene.name}");
        
        // Confirm the loaded scene matches what was intended
        if (scene.name == loadedSceneName)
        {
            // Notify server we're ready for player spawning
            if (NetworkClient.isConnected)
            {
                // Send ready message with our character ID
                string characterId = ClientPlayerDataManager.Instance.SelectedCharacterId;
                NetworkClient.Send(new PlayerSceneReadyMessage { characterId = characterId });
                
                if (debugMode)
                    Debug.Log($"Sent ready message for character: {characterId}");
            }
        }
        else
        {
            Debug.LogError($"Scene mismatch: Loaded {scene.name} but expected {loadedSceneName}");
        }
        
        // Notify subscribers
        OnSceneLoadComplete?.Invoke(scene.name);
    }
}