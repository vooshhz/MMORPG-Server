using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Firebase.Database;
using System.Threading.Tasks;

public class ServerPortalManager : MonoBehaviour
{
    public static ServerPortalManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void HandlePortalTransition(NetworkConnectionToClient conn, string characterId, 
                                       GameScene targetScene, Vector2 position)
    {
        // Get userId from connection's authentication data
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Missing authentication data for portal transition");
            return;
        }
        
        // Update Firebase with new location
        UpdateCharacterLocationInDatabase(userId, characterId, targetScene, position)
            .ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to update character location: {task.Exception}");
                    return;
                }
                
                // Store spawn data for when client signals readiness
                ServerPlayerDataManager.Instance.StoreSpawnData(
                    conn, characterId, position, targetScene.ToString());
                
                // Trigger client to change scenes
                conn.Send(new GameSceneTransitionResponseMessage {
                    approved = true,
                    sceneName = targetScene.ToString(),
                    spawnPosition = position
                });
                
                // Clean up existing player object if any
                if (conn.identity != null)
                {
                    NetworkServer.Destroy(conn.identity.gameObject);
                }
            });
    }
    
    private Task UpdateCharacterLocationInDatabase(string userId, string characterId, 
                                                 GameScene targetScene, Vector2 position)
    {
        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        string path = $"users/{userId}/characters/{characterId}/location";
        
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            ["sceneName"] = targetScene.ToString(),
            ["x"] = position.x,
            ["y"] = position.y,
            ["z"] = 0
        };
        
        return dbRef.Child(path).UpdateChildrenAsync(updates);
    }
}
