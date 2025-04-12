using UnityEngine;
using Mirror;

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
}
