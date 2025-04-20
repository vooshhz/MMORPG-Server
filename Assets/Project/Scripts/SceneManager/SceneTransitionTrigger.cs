using UnityEngine;
using Mirror;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Destination Settings")]
    [SerializeField] private LobbyScene destinationScene;
    [SerializeField] private float spawnX;
    [SerializeField] private float spawnY;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        Debug.Log($"Trigger entered by {collision.gameObject.name}");
        
        // Check for components on either this object or its parent
        GameObject playerObject = collision.gameObject;
        
        // Try to get PlayerMovement from this object or parent
        PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
        if (playerMovement == null && playerObject.transform.parent != null)
        {
            playerMovement = playerObject.transform.parent.GetComponent<PlayerMovement>();
            playerObject = playerObject.transform.parent.gameObject;
        }
        
        Debug.Log($"PlayerMovement found: {playerMovement != null}");
        
        if (playerMovement != null && playerMovement.isLocalPlayer)
        {
            Debug.Log("Is local player: true");
            
            // Get the player's character ID
            PlayerNetworkController networkController = playerObject.GetComponent<PlayerNetworkController>();
            Debug.Log($"NetworkController found: {networkController != null}");
            
            if (networkController != null)
            {
                isTransitioning = true;
                string characterId = networkController.characterId;
                Debug.Log($"Character ID: {characterId}");
                
                // Initiate scene transition via GameWorldManager
                Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);
                
                if (GameWorldManager.Instance != null)
                {
                    GameWorldManager.Instance.ChangeGameScene(destinationScene, characterId, spawnPosition);
                    Debug.Log($"Transitioning to {destinationScene} at position ({spawnX}, {spawnY})");
                }
                else
                {
                    Debug.LogError("GameWorldManager.Instance is null!");
                }
            }
        }
    }
}