using UnityEngine;
using Mirror;

public class Portal : MonoBehaviour
{
    [Header("Portal Destination")]
    [SerializeField] private SceneName targetScene;
    [SerializeField] private float destinationX;
    [SerializeField] private float destinationY;
    private float destinationZ = 0f;
    
    private bool isActive = true;
    private const float portalCooldown = 3f; // Prevent rapid portal use
    private float lastUseTime = -portalCooldown;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object is a player
        PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
        if (playerMovement == null || !playerMovement.isLocalPlayer)
            return;
            
        // Check cooldown
        if (Time.time - lastUseTime < portalCooldown)
            return;
        
        // Check if portal is active
        if (!isActive)
            return;
        
        // Update last use time to prevent rapid re-entry
        lastUseTime = Time.time;
        
        // Start the scene transition
        UsePortal(collision.gameObject);
    }
    
    private void UsePortal(GameObject player)
    {
        Debug.Log($"Portal used: Transitioning to {targetScene}");
        
        // Get the player's network identity component to ensure we're dealing with the local player
        NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
        if (identity == null || !identity.isLocalPlayer)
            return;
        
        // Get required components for transition
        PlayerNetworkController playerController = player.GetComponent<PlayerNetworkController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerNetworkController not found on player!");
            return;
        }
        
        string characterId = playerController.characterId;
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogError("Player has no character ID!");
            return;
        }
        
        // Create the destination position from our coordinates
        Vector3 destinationPosition = new Vector3(destinationX, destinationY, destinationZ);
        
        // Initiate scene change through GameWorldManager
        if (GameWorldManager.Instance != null)
        {
            GameWorldManager.Instance.ChangeGameScene(targetScene, characterId, destinationPosition);
        }
        else
        {
            Debug.LogError("GameWorldManager instance not found!");
        }
    }
    
    // Method to activate/deactivate the portal
    public void SetPortalActive(bool active)
    {
        isActive = active;
    }
}