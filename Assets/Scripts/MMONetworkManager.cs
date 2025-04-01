using UnityEngine;
using Mirror;

public class MMONetworkManager : NetworkManager
{
    [SerializeField] private GameObject spawnManagerPrefab; // 👈 Corrected type

    private NetworkSpawnManager runtimeSpawnManager;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);

        if (runtimeSpawnManager != null)
            runtimeSpawnManager.PositionPlayerAtSpawn(player);

        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"Player spawned for connection ID: {conn.connectionId}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started!");

        GameObject spawnMgr = Instantiate(spawnManagerPrefab);
        NetworkServer.Spawn(spawnMgr);

        runtimeSpawnManager = spawnMgr.GetComponent<NetworkSpawnManager>();

        Debug.Log("SpawnManager instantiated and registered.");
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
