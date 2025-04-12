using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

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
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Don't fade in for the initial scene (prevents black screen at start)
        if (scene.buildIndex == 0 && mode == UnityEngine.SceneManagement.LoadSceneMode.Single)
            return;
            
        // Fade in when a new scene is loaded
        transitionManager.FadeIn();
    }
    
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
            // Use Unity's scene management for client-only scenes
            transitionManager.LoadScene(targetScene);
        }
    }
}