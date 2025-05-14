using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    //==========================================================================
    // MIRROR LIFECYCLE OVERRIDES
    //==========================================================================

    public override void OnStartServer()
    {
        base.OnStartServer();                                               // Call base implementation
        Debug.Log("Server started!");                                       // Log server start
        
        // Register server-side message handlers
        NetworkServer.RegisterHandler<CharacterPreviewRequestMessage>(OnCharacterPreviewRequest);
        NetworkServer.RegisterHandler<CharacterDetailRequestMessage>(OnCharacterDetailRequest);
        NetworkServer.RegisterHandler<RequestCharacterCreationOptionsMessage>(OnRequestCharacterCreationOptions);
        NetworkServer.RegisterHandler<CreateCharacterRequestMessage>(OnCreateCharacterRequest);
        NetworkServer.RegisterHandler<LobbySceneTransitionRequestMessage>(OnLobbySceneTransitionRequest);
        NetworkServer.RegisterHandler<SpawnPlayerRequestMessage>(OnSpawnPlayerRequest);
        NetworkServer.RegisterHandler<GameSceneTransitionRequestMessage>(OnGameSceneTransitionRequest);
        NetworkServer.RegisterHandler<PlayerSceneReadyMessage>(OnPlayerSceneReady);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();                                              // Call base implementation
        
        // Register client-side message handlers
        NetworkClient.RegisterHandler<CharacterPreviewResponseMessage>(OnCharacterPreviewResponse);
        NetworkClient.RegisterHandler<GameSceneTransitionResponseMessage>(OnGameSceneTransitionResponse);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);                                        // Call base implementation
        Debug.Log($"[SERVER] Client connected: connectionId={conn.connectionId}"); // Log connection
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"Connection received. Waiting for login and character selection. ConnID: {conn.connectionId}");
        // Skip default player creation - will be spawned after authentication and character selection
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Client disconnected: {conn.connectionId}");           // Log disconnect
        base.OnServerDisconnect(conn);                                    // Call base implementation
    }

    public override void OnStopServer()
    {
        base.OnStopServer();                                             // Call base implementation
        Debug.Log("Server stopped!");                                    // Log server stop
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();                                       // Call base implementation
        Debug.Log("Disconnected from server, cleaning up session...");   // Log disconnect
        
        // Sign out from Firebase
        if (Firebase.Auth.FirebaseAuth.DefaultInstance != null)
        {
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();        // Sign out Firebase session
        }
        
        // Clear client-side data
        if (ClientPlayerDataManager.Instance != null)
        {
            ClientPlayerDataManager.Instance.ClearAllData();             // Clean up client data
        }
    }

    //==========================================================================
    // SERVER-SIDE MESSAGE HANDLERS
    //==========================================================================

    // Handle character selection preview request from client
    private void OnCharacterPreviewRequest(NetworkConnectionToClient conn, CharacterPreviewRequestMessage msg)
    {
        string userId = conn.authenticationData as string;               // Extract user ID
        if (string.IsNullOrEmpty(userId)) return;                        // Exit if not authenticated
        
        ServerPlayerDataManager.Instance.HandleCharacterPreviewRequest(conn, userId); // Forward to data manager
    }
    
    // Handle request for detailed character data
    private void OnCharacterDetailRequest(NetworkConnectionToClient conn, CharacterDetailRequestMessage msg)
    {
        ServerPlayerDataManager.Instance.HandleCharacterDataRequest(conn, msg.characterId); // Forward to data manager
    }

    // Handle request for character creation options
    private void OnRequestCharacterCreationOptions(NetworkConnectionToClient conn, RequestCharacterCreationOptionsMessage msg)
    {
        ServerPlayerDataManager.Instance.CheckCharacterLimitAndSendOptions(conn); // Check character limit and send options
    }

    // Handle request to create new character
    private void OnCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequestMessage msg)
    {
        ServerPlayerDataManager.Instance.HandleCreateCharacterRequest(conn, msg); // Forward to data manager
    }
    
    // Handle lobby scene transition request
    private void OnLobbySceneTransitionRequest(NetworkConnectionToClient conn, LobbySceneTransitionRequestMessage msg)
    {
        string userId = conn.authenticationData as string;              // Extract user ID
        
        // Check authentication status
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
        
        // Parse requested scene name
        if (Enum.TryParse<LobbyScene>(msg.targetScene, out LobbyScene targetScene))
        {
            // Check if transition is allowed
            if (IsLobbySceneTransitionAllowed(conn, targetScene))
            {
                SendLobbySceneTransitionResponse(conn, true, targetScene.ToString()); // Approve transition
            }
            else
            {
                SendLobbySceneTransitionResponse(conn, false, targetScene.ToString(), "Transition not allowed"); // Deny
            }
        }
        else
        {
            // Invalid scene name
            Debug.LogError($"Invalid lobby scene name requested: {msg.targetScene}");
            SendLobbySceneTransitionResponse(conn, false, msg.targetScene, "Invalid scene name");
        }
    }

    // Handle player spawn request
    private void OnSpawnPlayerRequest(NetworkConnectionToClient conn, SpawnPlayerRequestMessage msg)
    {
        ServerPlayerDataManager.Instance.HandlePlayerSpawnRequest(conn, msg.characterId); // Forward to data manager
    }

    // Handle game scene transition request
    private void OnGameSceneTransitionRequest(NetworkConnectionToClient conn, GameSceneTransitionRequestMessage msg)
    {
        string userId = conn.authenticationData as string;               // Extract user ID
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning($"Connection {conn.connectionId} requested scene transition without valid auth");
            return;                                                      // Reject if not authenticated
        }
        
        // Forward to data manager
        ServerPlayerDataManager.Instance.HandleGameSceneTransitionRequest(conn, msg.characterId, msg.targetScene);
    }
    
    // Handle player ready notification after scene load
    private void OnPlayerSceneReady(NetworkConnectionToClient conn, PlayerSceneReadyMessage msg)
    {
        string userId = conn.authenticationData as string;              // Extract user ID
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} sent ready message without valid auth");
            return;                                                     // Reject if not authenticated
        }
        
        // Get spawn data from server data manager
        if (ServerPlayerDataManager.Instance.TryGetSpawnData(conn, out var spawnData))
        {
            bool isSceneTransition = conn.identity != null;             // Check if this is a scene transition
            // Spawn player at correct position
            ServerPlayerDataManager.Instance.SpawnPlayerForClient(
                conn, msg.characterId, spawnData.Position, isSceneTransition);
        }
        else
        {
            Debug.LogError($"No spawn data found for connection {conn.connectionId}");
            // Fallback to default position
            ServerPlayerDataManager.Instance.SpawnPlayerForClient(conn, msg.characterId, Vector3.zero, false);
        }
    }

    //==========================================================================
    // CLIENT-SIDE MESSAGE HANDLERS
    //==========================================================================

    // Handle character preview response
    private void OnCharacterPreviewResponse(CharacterPreviewResponseMessage msg)
    {
        // Forward to client data manager
        ClientPlayerDataManager.Instance.ReceiveCharacterPreviewData(msg.characters, msg.equipmentData, msg.locationData);
    }

    // Handle game scene transition response
    private void OnGameSceneTransitionResponse(GameSceneTransitionResponseMessage msg)
    {
        Debug.Log($"Received scene transition response from server: {msg.sceneName}");
        
        // Forward to game scene manager if available
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.HandleSceneTransitionResponse(msg);
        }
        else
        {
            Debug.LogError("GameSceneManager not found! Cannot handle scene transition.");
        }
    }

    //==========================================================================
    // HELPER METHODS
    //==========================================================================

    // Check if a scene transition is allowed
    private bool IsLobbySceneTransitionAllowed(NetworkConnectionToClient conn, LobbyScene targetScene)
    {
        // Add validation logic here (e.g., check if player has completed necessary steps)
        return true;  // For now, allow all transitions for authenticated users
    }

    // Send lobby scene transition response
    private void SendLobbySceneTransitionResponse(NetworkConnectionToClient conn, bool approved, string sceneName, string message = "")
    {
        conn.Send(new LobbySceneTransitionResponseMessage
        {
            approved = approved,     // Whether transition is approved
            sceneName = sceneName,   // Scene to transition to
            message = message        // Optional message (especially for denials)
        });
    }

    // Add initial player for a connection
    public void AddPlayerForConnection(NetworkConnectionToClient conn, GameObject player)
    {
        NetworkServer.AddPlayerForConnection(conn, player);              // Register player with connection
        Debug.Log($"Initial player added for connection: {conn.connectionId}");
    }
    
    // Replace existing player for a connection (during scene transitions)
    public void ReplacePlayerForConnection(NetworkConnectionToClient conn, GameObject player)
    {
        var options = new ReplacePlayerOptions();                        // Default replacement options
        NetworkServer.ReplacePlayerForConnection(conn, player, options); // Replace player for connection
        Debug.Log($"Player replaced for connection: {conn.connectionId}");
    }
}