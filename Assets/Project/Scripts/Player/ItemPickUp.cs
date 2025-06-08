using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemPickUp : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!NetworkServer.active) return; // Only run on server
        
        Item item = collision.GetComponent<Item>();

        if(item != null)
        {            
            // Get item details
            ItemDetails itemDetails = ServerInventoryManager.Instance.GetItemDetails(item.ItemCode);

            // if item can be picked up
            if(itemDetails != null && itemDetails.canBePickedUp == true)
            {             
                // Get the player who picked up the item
                NetworkIdentity playerIdentity = GetComponent<NetworkIdentity>();
                
                if (playerIdentity != null)
                {
                    // Save inventory to Firebase
                    ServerInventoryManager.Instance.AddItemToPlayerInventory(playerIdentity, item.ItemCode);
                    
                    // Destroy the item
                   // Destroy(collision.gameObject);
                }
                else
                {
                    Debug.LogError("[ItemPickUp] Could not find NetworkIdentity on player!");
                }
            }
            else
            {
                Debug.Log($"[ItemPickUp] Item {item.ItemCode} cannot be picked up or item details not found");
            }
        }
    }
}