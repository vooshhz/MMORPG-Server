using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
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
        NetworkServer.RegisterHandler<LobbySceneTransitionRequestMessage>(OnLobbySceneTransitionRequest);
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
    }

}