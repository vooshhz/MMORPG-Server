using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    // Track pending spawn requests that are waiting for scene transitions
    private Dictionary<string, PendingSpawnRequest> pendingSpawnRequests = new Dictionary<string, PendingSpawnRequest>();
    
    // Class to track pending spawn requests
    private class PendingSpawnRequest
    {
        public NetworkConnectionToClient connection;
        public string characterId;
        public string targetScene;
        public Vector3 spawnPosition;
        public float requestTime;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"Connection received. Waiting for login and character selection. ConnID: {conn.connectionId}");
        // Do nothing here. Player will be spawned manually later.
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[SERVER] Client connected: connectionId={conn.connectionId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started!");
        NetworkServer.RegisterHandler<CharacterPreviewRequestMessage>(OnCharacterPreviewRequest);
        NetworkServer.RegisterHandler<CharacterDetailRequestMessage>(OnCharacterDetailRequest);
        NetworkServer.RegisterHandler<RequestCharacterCreationOptionsMessage>(OnRequestCharacterCreationOptions);
        NetworkServer.RegisterHandler<CreateCharacterRequestMessage>(OnCreateCharacterRequest);     
        NetworkServer.RegisterHandler<SpawnPlayerRequestMessage>(OnSpawnPlayerRequest);   
        NetworkServer.RegisterHandler<SceneChangeCompletedMessage>(OnSceneChangeCompleted);
        NetworkServer.RegisterHandler<SavePlayerStateMessage>(OnSavePlayerState);
        NetworkServer.RegisterHandler<LobbySceneTransitionRequestMessage>(OnLobbySceneTransitionRequest);
        
        // Start routine to clean up stale pending spawn requests
        StartCoroutine(CleanupStalePendingRequests());
    }
    
    private void OnLobbySceneTransitionRequest(NetworkConnectionToClient conn, LobbySceneTransitionRequestMessage msg)
    {
        // Get the authenticated user ID from the connection
        string userId = conn.authenticationData as string;
        
        // Check if user is authenticated
        if (string.IsNullOrEmpty(userId))
        {
            // Allow transition to login scene even without auth
            if (msg.targetScene == LobbyScene.LoginScene.ToString())
            {
                SendLobbySceneTransitionResponse(conn, true, LobbyScene.LoginScene.ToString());
                return;
            }
            
            // Deny other transitions if not authenticated
            Debug.LogWarning($"Connection {conn.connectionId} requested scene transition without valid auth");
            SendLobbySceneTransitionResponse(conn, false, msg.targetScene, "Authentication required");
            return;
        }
        
        // Parse the requested scene name
        if (Enum.TryParse<LobbyScene>(msg.targetScene, out LobbyScene targetScene))
        {
            // Check if the transition is allowed
            if (IsLobbySceneTransitionAllowed(conn, targetScene))
            {
                // Approve the transition
                SendLobbySceneTransitionResponse(conn, true, targetScene.ToString());
            }
            else
            {
                // Deny the transition
                SendLobbySceneTransitionResponse(conn, false, targetScene.ToString(), "Transition not allowed");
            }
        }
        else
        {
            // Invalid scene name
            Debug.LogError($"Invalid lobby scene name requested: {msg.targetScene}");
            SendLobbySceneTransitionResponse(conn, false, msg.targetScene, "Invalid scene name");
        }
    }

    // Check if a scene transition is allowed
    private bool IsLobbySceneTransitionAllowed(NetworkConnectionToClient conn, LobbyScene targetScene)
    {
        // Add your validation logic here
        // For example, check if player has completed necessary steps
        
        // For now, allow all transitions for authenticated users
        return true;
    }

    // Send response to client
    private void SendLobbySceneTransitionResponse(NetworkConnectionToClient conn, bool approved, string sceneName, string message = "")
    {
        conn.Send(new LobbySceneTransitionResponseMessage
        {
            approved = approved,
            sceneName = sceneName,
            message = message
        });
    }
    // Clean up pending spawn requests that have been waiting too long (30 seconds)
    private IEnumerator CleanupStalePendingRequests()
    {
        while (true)
        {
            List<string> keysToRemove = new List<string>();
            
            foreach (var kvp in pendingSpawnRequests)
            {
                if (Time.time - kvp.Value.requestTime > 30f)
                {
                    keysToRemove.Add(kvp.Key);
                    Debug.LogWarning($"Removing stale spawn request for character {kvp.Value.characterId} that has been pending for over 30 seconds");
                }
            }
            
            foreach (var key in keysToRemove)
            {
                pendingSpawnRequests.Remove(key);
            }
            
            yield return new WaitForSeconds(10f);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Client disconnected: {conn.connectionId}");
        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server stopped!");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<CharacterPreviewResponseMessage>(OnCharacterPreviewResponse);
    }

    // Server-side handler
    private void OnCharacterPreviewRequest(NetworkConnectionToClient conn, CharacterPreviewRequestMessage msg)
    {
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId)) return;
        
        ServerPlayerDataManager.Instance.HandleCharacterPreviewRequest(conn, userId);
    }
    
    private void OnCharacterDetailRequest(NetworkConnectionToClient conn, CharacterDetailRequestMessage msg)
    {
        ServerPlayerDataManager.Instance.HandleCharacterDataRequest(conn, msg.characterId);
    }

    // Client-side handler
    private void OnCharacterPreviewResponse(CharacterPreviewResponseMessage msg)
    {
        // Route to ClientPlayerDataManager
        ClientPlayerDataManager.Instance.ReceiveCharacterPreviewData(msg.characters, msg.equipmentData, msg.locationData);
    }

    private void OnRequestCharacterCreationOptions(NetworkConnectionToClient conn, RequestCharacterCreationOptionsMessage msg)
    {
        ServerPlayerDataManager.Instance.CheckCharacterLimitAndSendOptions(conn);
    }

    private void OnCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequestMessage msg)
    {
        // Pass to ServerPlayerDataManager to handle
        ServerPlayerDataManager.Instance.HandleCreateCharacterRequest(conn, msg);
    }

    private void OnSpawnPlayerRequest(NetworkConnectionToClient conn, SpawnPlayerRequestMessage msg)
    {
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} tried to spawn player without valid auth");
            return;
        }
        
        string characterId = msg.characterId;
        
        // Get location data
        ServerPlayerDataManager.Instance.GetCharacterLocation(conn, userId, characterId, 
            (locationData) => HandlePlayerSpawnWithLocation(conn, characterId, locationData));
    }
    
    private void HandlePlayerSpawnWithLocation(NetworkConnectionToClient conn, string characterId, ClientPlayerDataManager.LocationData locationData)
    {
        // Get scene name as string
        string targetScene = locationData.sceneName;
        string currentScene = SceneManager.GetActiveScene().name;
        Vector3 position = locationData.position;
        
        // Create a unique key for this pending request
        string requestKey = conn.connectionId + "_" + characterId;
        
        // Check if we need to change scene
        if (targetScene != currentScene)
        {
            Debug.Log($"Need to change scene from {currentScene} to {targetScene} before spawning player {characterId}");
            
            // Store the spawn request for later
            pendingSpawnRequests[requestKey] = new PendingSpawnRequest
            {
                connection = conn,
                characterId = characterId,
                targetScene = targetScene,
                spawnPosition = position,
                requestTime = Time.time
            };
            
            // Tell client to change scene first, and that we'll spawn player after
            conn.Send(new SceneChangeApprovedMessage
            { 
                sceneName = targetScene,
                characterId = characterId,
                spawnAfterChange = true
            });
            
            // Server will wait for scene change confirmation before spawning player
        }
        else
        {
            // Same scene, spawn immediately
            SpawnPlayerInScene(conn, characterId, position);
        }
    }
    
    private void OnSceneChangeCompleted(NetworkConnectionToClient conn, SceneChangeCompletedMessage msg)
    {
        Debug.Log($"Client confirmed scene change to {msg.sceneName} is complete");
        
        // Check if there's a character to spawn
        if (!string.IsNullOrEmpty(msg.characterId))
        {
            // Create the request key
            string requestKey = conn.connectionId + "_" + msg.characterId;
            
            // Check if we have a pending spawn request
            if (pendingSpawnRequests.TryGetValue(requestKey, out PendingSpawnRequest request))
            {
                Debug.Log($"Found pending spawn request for character {msg.characterId}, spawning now");
                
                // Spawn the player
                SpawnPlayerInScene(conn, request.characterId, request.spawnPosition);
                
                // Remove from pending requests
                pendingSpawnRequests.Remove(requestKey);
            }
        }
    }
    
    private void SpawnPlayerInScene(NetworkConnectionToClient conn, string characterId, Vector3 position)
    {
        // Spawn the player
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.SpawnPlayerCharacter(conn, characterId, position);
            Debug.Log($"Player spawned for character {characterId} at position {position}");
        }
        else
        {
            Debug.LogError("PlayerSpawnController.Instance is null! Cannot spawn player.");
        }
    }
    
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Disconnected from server, cleaning up session...");
        
        // Sign out from Firebase
        if (Firebase.Auth.FirebaseAuth.DefaultInstance != null)
        {
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
        }
        
        // Clear client-side data
        if (ClientPlayerDataManager.Instance != null)
        {
            ClientPlayerDataManager.Instance.ClearAllData();
        }
        
        // Reset UI state if needed
        
        // Return to login scene
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(LobbyScene.LoginScene);
        }
    }

    private void OnSavePlayerState(NetworkConnectionToClient conn, SavePlayerStateMessage msg)
    {
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Connection tried to save player state without valid auth");
            return;
        }
        
        // Save player state to database
        ServerPlayerDataManager.Instance.SaveCharacterPosition(
            userId, 
            msg.characterId, 
            msg.position, 
            msg.sceneName
        );
    }
}