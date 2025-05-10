using UnityEngine;
using Mirror;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] private GameScene portalDestination;
    [SerializeField] private Vector2 portalSpawnPoint;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if this is the local player
        PlayerNetworkController player = other.GetComponent<PlayerNetworkController>();
        if (player == null || !player.isLocalPlayer)
            return;
            
        Debug.Log($"**** PORTAL TRIGGERED with characterId: {player.characterId} to scene: {portalDestination} ****");
        
        // Remember current position for debugging
        Vector3 currentPosition = player.transform.position;
        Debug.Log($"Current player position: {currentPosition}");
        
        // Make sure we have a valid characterId
        string characterId = player.characterId;
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = ClientPlayerDataManager.Instance.SelectedCharacterId;
            Debug.Log($"Using characterId from ClientPlayerDataManager: {characterId}");
        }
        
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogError("No valid characterId found for scene transition!");
            return;
        }
        
        // Save player state before sending transition request
        if (NetworkClient.connection != null)
        {
            NetworkClient.Send(new SavePlayerStateMessage
            {
                characterId = characterId,
                position = portalSpawnPoint,
                sceneName = portalDestination.ToString()
            });

            Debug.Log($"Sent SavePlayerStateMessage before transition: pos={portalSpawnPoint}, scene={portalDestination}");
        }
        
        // Request scene transition with the valid characterId
        NetworkClient.Send(new SceneChangeRequestMessage
        {
            sceneName = portalDestination.ToString(),
            characterId = characterId
        });
    }
}