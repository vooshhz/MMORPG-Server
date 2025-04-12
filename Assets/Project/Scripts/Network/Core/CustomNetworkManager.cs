using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;

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

    public struct SpawnPlayerRequestMessage : NetworkMessage
    {
        public string characterId;
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
        ClientPlayerDataManager.Instance.ReceiveCharacterPreviewData(msg.characters, msg.equipmentData);
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
            // Handle scene loading
            ServerChangeScene(sceneName);
            
            // Wait a moment for scene to load
            yield return new WaitForSeconds(1.0f);
        }
        
        // Get position data
        Vector3 position = locationData.position;
        
        // Spawn the player
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.SpawnPlayerCharacter(conn, characterId, position);
        }
        
        Debug.Log($"Player spawned for character {characterId} at position {position} in scene {sceneName}");
    }
}
