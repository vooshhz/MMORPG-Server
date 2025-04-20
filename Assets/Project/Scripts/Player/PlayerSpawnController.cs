using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerSpawnController : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnDelay = 0.5f;
    
    private static PlayerSpawnController _instance;
    public static PlayerSpawnController Instance { get { return _instance; } }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // public void StartPlayerSpawnProcess(string characterId, LobbyScene sceneToLoad)
    // {
    //     StartCoroutine(SpawnPlayerCoroutine(characterId, sceneToLoad));
    // }
    
    // private IEnumerator SpawnPlayerCoroutine(string characterId, LobbyScene sceneToLoad)
    // {
    //     // Trigger scene transition with fade
    //     if (SceneTransitionManager.Instance != null)
    //     {
    //         bool sceneLoadComplete = false;
    //         SceneTransitionManager.Instance.LoadScene(sceneToLoad, () => { sceneLoadComplete = true; });
            
    //         // Wait for scene load to complete
    //         yield return new WaitUntil(() => sceneLoadComplete);
    //     }
        
    //     // Additional delay to ensure scene is fully set up
    //     yield return new WaitForSeconds(spawnDelay);
        
    //     // Send request to server to spawn player
    //     NetworkClient.Send(new SpawnPlayerRequestMessage
    //     {
    //         characterId = characterId
    //     });
    // }
    
    // Call this from your NetworkManager when player is ready to be added to the scene
    public GameObject SpawnPlayerCharacter(NetworkConnectionToClient conn, string characterId, Vector3 position)
    {
        // Instantiate the player object
        GameObject playerObject = Instantiate(playerPrefab, position, Quaternion.identity);
        
        // Set up player with character data
        var playerNetworkController = playerObject.GetComponent<PlayerNetworkController>();
        
        // Spawn the player object on the network
        NetworkServer.AddPlayerForConnection(conn, playerObject);

        // Store the character ID in the player object (need to add this property to PlayerNetworkController)
        if (playerNetworkController != null)
        {
            // Server-side assignment (optional, if you use it server-side)
            playerNetworkController.SetCharacterId(characterId);

            // 🔥 Send characterId to the client via TargetRpc
            playerNetworkController.TargetInitializePlayer(conn, characterId);
        }
        
        return playerObject;
    }
}