using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : MonoBehaviour
{
    public static NetworkSceneManager Instance { get; private set; }
    
    [SerializeField] private SceneTransitionManager transitionManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Find transition manager if not set
        if (transitionManager == null)
            transitionManager = FindObjectOfType<SceneTransitionManager>();
    }
    
    private void OnEnable()
    {
        // Register for scene events through Unity's SceneManager instead
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Register for our custom scene change message
        NetworkClient.RegisterHandler<SceneChangeApprovedMessage>(OnSceneChangeApproved);
    }
    
    private void OnDisable()
    {
        // Unsubscribe from scene events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Called when a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Don't fade in for the initial scene
        if (scene.buildIndex == 0 && mode == LoadSceneMode.Single)
            return;
            
        // Fade in when a new scene is loaded
        transitionManager.FadeIn();
        
        // Now tell the server that the scene change is complete
        if (NetworkClient.isConnected && !string.IsNullOrEmpty(scene.name))
        {
            // Get scene enum value from scene name
            if (Enum.TryParse<SceneName>(scene.name, out SceneName sceneEnum))
            {
                string characterId = ""; 
                
                // IMPORTANT: Include the character ID in the response to server
                if (NetworkClient.localPlayer != null && 
                    NetworkClient.localPlayer.GetComponent<PlayerNetworkController>() != null)
                {
                    characterId = NetworkClient.localPlayer.GetComponent<PlayerNetworkController>().characterId;
                }
                // Also check pending scene change
                else if (_pendingSceneChange?.spawnAfterChange == true)
                {
                    characterId = _pendingSceneChange.Value.characterId;
                    _pendingSceneChange = null;
                }
                
                // Send confirmation to server
                NetworkClient.Send(new SceneChangeCompletedMessage
                {
                    sceneName = scene.name,
                    characterId = characterId
                });
                
                Debug.Log($"Notified server that scene change to {scene.name} is complete with characterId: {characterId}");
            }
        }
    }
    
    // Store pending scene change that will require player spawning
    private SceneChangeApprovedMessage? _pendingSceneChange;
    
    // Request scene change from server (use this for post-auth scenes)
    public void RequestSceneChange(SceneName targetScene)
    {
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("Cannot request scene change: Not connected to server");
            return;
        }
        
        // Fade out first
        transitionManager.FadeOut(() => {
            // After fade out, send request to server
            NetworkClient.Send(new SceneChangeRequestMessage {
                sceneName = targetScene.ToString()
            });
        });
    }
    
    // For pre-auth scenes that don't need server authorization
    public void ChangeSceneLocally(SceneName targetScene)
    {
        // Use your existing SceneTransitionManager for local scene changes
        transitionManager.LoadScene(targetScene);
    }
    
    // Handler for approved scene changes
    private void OnSceneChangeApproved(SceneChangeApprovedMessage msg)
    {
        if (System.Enum.TryParse<SceneName>(msg.sceneName, out SceneName targetScene))
        {
            // Store this as a pending scene change if player spawning is needed after
            if (msg.spawnAfterChange)
            {
                _pendingSceneChange = msg;
                Debug.Log($"Scene change approved with pending player spawn for character {msg.characterId}");
            }
            
            // Use Unity's scene management for client-only scenes
            transitionManager.LoadScene(targetScene);
        }
    }
}