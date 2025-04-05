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
}
