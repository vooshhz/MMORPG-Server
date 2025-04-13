using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

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
        NetworkServer.RegisterHandler<SpawnPlayerRequestMessage>(OnSpawnPlayerRequest);   

        NetworkServer.RegisterHandler<SceneChangeRequestMessage>(OnSceneChangeRequest);
        

    }

    private void OnSceneChangeRequest(NetworkConnectionToClient conn, SceneChangeRequestMessage msg)
    {
        // Verify user is authenticated
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} tried to change scene without valid auth");
            return;
        }
        
        // Verify the requested scene is valid 
        if (Enum.TryParse<SceneName>(msg.sceneName, out SceneName targetScene))
        {
            // Check if scene change is allowed
            bool isAllowed = IsSceneChangeAllowed(conn, targetScene);
            
            if (isAllowed)
            {
                // For character selection and similar scenes, change scene for this client only
                if (targetScene == SceneName.CharacterSelectionScene || 
                    targetScene == SceneName.CharacterCreationScene)
                {
                    conn.Send(new SceneChangeApprovedMessage { 
                        sceneName = targetScene.ToString() 
                    });
                }
                // For game world scenes, handle via server's scene management
                else
                {
                    ServerChangeScene(targetScene.ToString());
                }
            }
        }
        else
        {
            Debug.LogError($"Invalid scene name requested: {msg.sceneName}");
        }
    }
    private bool IsSceneChangeAllowed(NetworkConnectionToClient conn, SceneName targetScene)
    {
        // Add your validation logic here
        // For example, check if player has the right to access this scene
        return true; // For now, allow all scene changes
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
            (locationData) => StartCoroutine(HandlePlayerSpawn(conn, characterId, locationData)));
    }

 private IEnumerator HandlePlayerSpawn(NetworkConnectionToClient conn, string characterId, ClientPlayerDataManager.LocationData locationData)
    {
        // Get scene name as string
        string sceneName = locationData.sceneName;
        string currentScene = SceneManager.GetActiveScene().name;

        // Check if we need to change scene
        if (sceneName != currentScene)
        {
            Debug.Log($"Need to change scene from {currentScene} to {sceneName} before spawning player");
            
            // Tell client to change scene first
            conn.Send(new SceneChangeApprovedMessage { 
                sceneName = sceneName 
            });
            
            // Wait for scene change to complete (add a longer delay)
            yield return new WaitForSeconds(2.0f);
            
            // Check scene again - it may still not be correct on the server
            if (SceneManager.GetActiveScene().name != sceneName)
            {
                ServerChangeScene(sceneName);
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        // Double check we're in the right scene now
        Debug.Log($"Current scene before spawning: {SceneManager.GetActiveScene().name}, target scene: {sceneName}");
        
        // Get position data
        Vector3 position = locationData.position;
        
        // Spawn the player
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.SpawnPlayerCharacter(conn, characterId, position);
            Debug.Log($"Player spawned for character {characterId} at position {position} in scene {SceneManager.GetActiveScene().name}");
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
        SceneTransitionManager.Instance.LoadScene(SceneName.LoginScene);
    }
}
}
