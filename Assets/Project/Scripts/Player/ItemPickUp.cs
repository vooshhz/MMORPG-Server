using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemPickUp : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"[ItemPickUp] ItemPickUp script started on GameObject: {gameObject.name}");
        Debug.Log($"[ItemPickUp] Is Server: {NetworkServer.active}, Is Client: {NetworkClient.active}");
        
        // Check if this item has the required components
        Item itemComponent = GetComponent<Item>();
        if (itemComponent != null)
        {
            Debug.Log($"[ItemPickUp] Item component found. ItemCode: {itemComponent.ItemCode}");
        }
        else
        {
            Debug.LogError($"[ItemPickUp] No Item component found on {gameObject.name}!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[ItemPickUp] Trigger collision detected!");
        Debug.Log($"[ItemPickUp] Colliding object: {collision.gameObject.name}");
        Debug.Log($"[ItemPickUp] Collision on Server: {NetworkServer.active}, Client: {NetworkClient.active}");
        Debug.Log($"[ItemPickUp] This GameObject: {gameObject.name}");
        
        // Check if the colliding object is a player
        NetworkIdentity playerIdentity = collision.GetComponent<NetworkIdentity>();
        
        if (playerIdentity != null)
        {
            Debug.Log($"[ItemPickUp] Player NetworkIdentity found!");
            Debug.Log($"[ItemPickUp] Player netId: {playerIdentity.netId}");
            Debug.Log($"[ItemPickUp] Player connectionToClient: {playerIdentity.connectionToClient?.connectionId}");
            Debug.Log($"[ItemPickUp] Player isServer: {playerIdentity.isServer}");
            Debug.Log($"[ItemPickUp] Player isClient: {playerIdentity.isClient}");
            
            Item item = GetComponent<Item>();
            
            if (item != null)
            {
                Debug.Log($"[ItemPickUp] Item component found on pickup object");
                Debug.Log($"[ItemPickUp] Item Code: {item.ItemCode}");
                
                // Check if ServerInventoryManager exists
                if (ServerInventoryManager.Instance == null)
                {
                    Debug.LogError($"[ItemPickUp] ServerInventoryManager.Instance is NULL!");
                    return;
                }
                
                Debug.Log($"[ItemPickUp] ServerInventoryManager instance found");
                
                // Get item details
                ItemDetails itemDetails = ServerInventoryManager.Instance.GetItemDetails(item.ItemCode);
                
                if (itemDetails != null)
                {
                    Debug.Log($"[ItemPickUp] Item details found for itemCode {item.ItemCode}");
                    Debug.Log($"[ItemPickUp] Item name: {itemDetails.itemDescription}");
                    Debug.Log($"[ItemPickUp] Can be picked up: {itemDetails.canBePickedUp}");
                    
                    // if item can be picked up
                    if (itemDetails.canBePickedUp == true)
                    {
                        Debug.Log($"[ItemPickUp] Item {item.ItemCode} is pickable. Attempting to add to inventory...");
                        
                        // Add item to player's inventory on server
                        ServerInventoryManager.Instance.AddItemToPlayerInventory(playerIdentity, item.ItemCode);
                        
                        Debug.Log($"[ItemPickUp] Item pickup method called. Destroying item GameObject...");
                        
                        // Destroy the item after pickup
                        Destroy(gameObject);
                        
                        Debug.Log($"[ItemPickUp] Item {gameObject.name} destroyed successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemPickUp] Item {item.ItemCode} cannot be picked up (canBePickedUp = false)");
                    }
                }
                else
                {
                    Debug.LogError($"[ItemPickUp] No item details found for itemCode: {item.ItemCode}");
                }
            }
            else
            {
                Debug.LogError($"[ItemPickUp] No Item component found on {gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[ItemPickUp] Colliding object {collision.gameObject.name} does not have NetworkIdentity");
            Debug.Log($"[ItemPickUp] Checking what components it has:");
            
            Component[] components = collision.GetComponents<Component>();
            foreach (Component comp in components)
            {
                Debug.Log($"[ItemPickUp] - Component: {comp.GetType().Name}");
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log($"[ItemPickUp] Trigger exit detected with: {collision.gameObject.name}");
    }
}