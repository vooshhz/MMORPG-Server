using UnityEngine;
using Mirror;

public class ItemPickUp : NetworkBehaviour
{
    [Server]
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ItemPickUp] OnTriggerEnter2D triggered with: {other.name}");
        
        // Only process on server
        if (!NetworkServer.active)
        {
            Debug.Log("[ItemPickUp] Not running on server, ignoring collision");
            return;
        }
        
        // Get the Item component from the colliding object
        Item item = other.GetComponent<Item>();
        if (item == null)
        {
            Debug.Log($"[ItemPickUp] No Item component found on {other.name}");
            return;
        }
        
        Debug.Log($"[ItemPickUp] Item component found with ItemCode: {item.ItemCode}");
        
        // Check if item code is valid (not 0)
        if (item.ItemCode <= 0)
        {
            Debug.LogWarning($"[ItemPickUp] Invalid item code: {item.ItemCode}");
            return;
        }
        
        // Get PlayerCharacterData from this player
        PlayerCharacterData playerData = GetComponent<PlayerCharacterData>();
        if (playerData == null)
        {
            Debug.LogError("[ItemPickUp] PlayerCharacterData component not found on player!");
            return;
        }
        
        Debug.Log($"[ItemPickUp] PlayerCharacterData found - CharacterId: {playerData.characterId}, UserId: {playerData.userId}");
        
        // Check if we have valid character and user IDs
        if (string.IsNullOrEmpty(playerData.characterId) || string.IsNullOrEmpty(playerData.userId))
        {
            Debug.LogError($"[ItemPickUp] Missing player data - CharacterId: '{playerData.characterId}', UserId: '{playerData.userId}'");
            return;
        }
        
        // Check if ServerInventoryManager exists
        if (ServerInventoryManager.Instance == null)
        {
            Debug.LogError("[ItemPickUp] ServerInventoryManager.Instance is null!");
            return;
        }
        
        Debug.Log($"[ItemPickUp] About to add item {item.ItemCode} to player {playerData.characterName}'s inventory");
        
        // Add item to player's inventory
        ServerInventoryManager.Instance.AddItemToPlayerInventory(playerData, item.ItemCode);
        
        Debug.Log($"[ItemPickUp] Item pickup processing completed for item {item.ItemCode}");
    }
}