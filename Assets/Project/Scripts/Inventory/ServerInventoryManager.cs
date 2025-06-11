using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Mirror;
using Firebase.Database;

public class ServerInventoryManager : MonoBehaviour
{
    public static ServerInventoryManager Instance { get; private set; }
    private Dictionary<int, ItemDetails> itemDetailsDictionary;

    [SerializeField] private SO_ItemList itemList = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Create item details dictionary
        CreateItemDetailsDictionary();
    }



    private void CreateItemDetailsDictionary()
    {
        itemDetailsDictionary = new Dictionary<int, ItemDetails>();

        foreach (ItemDetails itemDetails in itemList.itemDetails)
        {
            itemDetailsDictionary.Add(itemDetails.itemCode, itemDetails);
        }
    }

    public ItemDetails GetItemDetails(int itemCode)
    {
        ItemDetails itemDetails;

        if (itemDetailsDictionary.TryGetValue(itemCode, out itemDetails))
        {
            return itemDetails;
        }
        else
        {
            return null;
        }

    }

  [Server]
    public async void AddItemToPlayerInventory(PlayerCharacterData playerData, int itemCode)
    {
        Debug.Log($"[AddItemToPlayerInventory] Starting method with itemCode: {itemCode}");
        
        if (playerData == null)
        {
            Debug.LogError("[AddItemToPlayerInventory] PlayerCharacterData is null!");
            return;
        }
        
        Debug.Log($"[AddItemToPlayerInventory] PlayerData found - Character: {playerData.characterName}, Class: {playerData.characterClass}");
        
        // Get userId and characterId from PlayerCharacterData
        string userId = playerData.userId;
        string characterId = playerData.characterId;
        
        Debug.Log($"[AddItemToPlayerInventory] UserId: {userId}, CharacterId: {characterId}");
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(characterId))
        {
            Debug.LogError($"[AddItemToPlayerInventory] Missing data - UserId: '{userId}', CharacterId: '{characterId}'");
            return;
        }
        
        // Validate item code
        if (itemCode <= 0)
        {
            Debug.LogError($"[AddItemToPlayerInventory] Invalid item code: {itemCode}");
            return;
        }
        
        // Check if item exists in our item details dictionary
        ItemDetails itemDetails = GetItemDetails(itemCode);
        if (itemDetails == null)
        {
            Debug.LogError($"[AddItemToPlayerInventory] Item code {itemCode} not found in item details dictionary!");
            return;
        }
        
        Debug.Log($"[AddItemToPlayerInventory] Item details found - Name: {itemDetails.itemDescription}, Type: {itemDetails.itemType}");
        
        try
        {
            Debug.Log($"[AddItemToPlayerInventory] Starting Firebase operations...");
            
            // Get current inventory array from Firebase
            DatabaseReference inventoryRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/characters/{characterId}/inventory/items");
            
            Debug.Log($"[AddItemToPlayerInventory] Firebase reference created for path: users/{userId}/characters/{characterId}/inventory/items");
            
            DataSnapshot snapshot = await inventoryRef.GetValueAsync();
            
            Debug.Log($"[AddItemToPlayerInventory] Firebase snapshot received. Exists: {snapshot.Exists}");
            
            if (snapshot.Exists)
            {
                Debug.Log($"[AddItemToPlayerInventory] Raw JSON data: {snapshot.GetRawJsonValue()}");
                
                // Convert to list for easier manipulation
                var inventoryList = JsonConvert.DeserializeObject<List<InventoryItem>>(snapshot.GetRawJsonValue());
                
                if (inventoryList == null)
                {
                    Debug.LogError("[AddItemToPlayerInventory] Failed to deserialize inventory list!");
                    return;
                }
                
                Debug.Log($"[AddItemToPlayerInventory] Inventory list deserialized. Count: {inventoryList.Count}");
                
                // Find if item already exists
                var existingItem = inventoryList.FirstOrDefault(x => x.itemCode == itemCode);
                
                if (existingItem != null)
                {
                    Debug.Log($"[AddItemToPlayerInventory] Item {itemCode} already exists with quantity {existingItem.itemQuantity}. Incrementing...");
                    // Item exists, increment quantity
                    existingItem.itemQuantity++;
                    Debug.Log($"[AddItemToPlayerInventory] Item quantity increased to {existingItem.itemQuantity}");
                }
                else
                {
                    Debug.Log($"[AddItemToPlayerInventory] Item {itemCode} not found. Looking for empty slot...");
                    // Find first empty slot (itemCode = 0)
                    var emptySlot = inventoryList.FirstOrDefault(x => x.itemCode == 0);
                    if (emptySlot != null)
                    {
                        Debug.Log($"[AddItemToPlayerInventory] Empty slot found at position. Adding item {itemCode}");
                        emptySlot.itemCode = itemCode;
                        emptySlot.itemQuantity = 1;
                        Debug.Log($"[AddItemToPlayerInventory] Item {itemCode} added to empty slot with quantity 1");
                    }
                    else
                    {
                        Debug.LogWarning($"[AddItemToPlayerInventory] No empty slots available in inventory! Inventory full.");
                        return;
                    }
                }
                
                Debug.Log($"[AddItemToPlayerInventory] Saving updated inventory to Firebase...");
                // Save back to Firebase
                await inventoryRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(inventoryList));
                Debug.Log($"[AddItemToPlayerInventory] Inventory successfully saved to Firebase!");
            }
            else
            {
                Debug.LogError($"[AddItemToPlayerInventory] Firebase snapshot does not exist for path: users/{userId}/characters/{characterId}/inventory/items");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AddItemToPlayerInventory] Failed to add item to Firebase: {e.Message}");
            Debug.LogError($"[AddItemToPlayerInventory] Stack trace: {e.StackTrace}");
        }
        
        Debug.Log($"[AddItemToPlayerInventory] Method completed for item {itemCode}");
    }
}

