using UnityEngine;
using Mirror;

public class NetworkSpawnManager : NetworkBehaviour
{
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10, -10);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10, 10);
    
    public static NetworkSpawnManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        
        return new Vector3(x, y, 0);
    }
    
    // Call this when a new player joins to position them
   [Server]
    public void PositionPlayerAtSpawn(GameObject player)
    {
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        // Just set the position directly - NetworkTransform will handle the sync
        player.transform.position = spawnPos;
        
        // No need for explicit RpcTeleport in newer Mirror versions
        // The position will be synced automatically
    }
    }
