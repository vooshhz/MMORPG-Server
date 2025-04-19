using UnityEngine;
using Mirror;
using System;
using System.Collections;

public class GameWorldManager : MonoBehaviour
{
    public static GameWorldManager Instance { get; private set; }
    
    private NetworkSceneManager networkSceneManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Only get reference to NetworkSceneManager since that's client-side
        networkSceneManager = NetworkSceneManager.Instance;
        
        if (networkSceneManager == null)
            Debug.LogError("NetworkSceneManager not found");
    }
    
    // Primary method for changing scenes with player persistence
    public void ChangeGameScene(SceneName targetScene, string characterId, Vector3? overridePosition = null)
    {
        StartCoroutine(ChangeSceneAndSpawnPlayer(targetScene, characterId, overridePosition));
    }
    
    private IEnumerator ChangeSceneAndSpawnPlayer(SceneName targetScene, string characterId, Vector3? overridePosition)
    {
         // When saving player state before scene change, include the override position
        if (NetworkClient.localPlayer != null)
        {
            // Save player's current position/state before scene change
            SavePlayerState(characterId, overridePosition);
        }
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("Cannot change scene: Not connected to server");
            yield break;
        }
        
        // Step 1: Save current player position if needed
        if (NetworkClient.localPlayer != null)
        {
            // Save player's current position/state before scene change
            SavePlayerState(characterId);
        }
        
        // Step 2: Request scene change through NetworkSceneManager
        networkSceneManager.RequestSceneChange(targetScene);
        
        // After scene change, the SpawnPlayerRequestMessage will be sent by NetworkSceneManager
        // when the SceneChangeApprovedMessage is received with spawnAfterChange=true
    }
    
    private void SavePlayerState(string characterId, Vector3? overridePosition = null)
    {
        if (NetworkClient.localPlayer != null)
        {
            // Use override position if provided, otherwise use current position
            Vector3 position = overridePosition ?? NetworkClient.localPlayer.transform.position;
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // Send message to server with position (potentially overridden)
            NetworkClient.Send(new SavePlayerStateMessage
            {
                characterId = characterId,
                position = position,
                sceneName = currentScene
            });
        }
    }
}