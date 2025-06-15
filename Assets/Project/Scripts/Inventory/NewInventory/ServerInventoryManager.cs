using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Mirror;
using Firebase.Database;
using System.Threading.Tasks;


public class ServerInventoryManager : MonoBehaviour
{
    public static ServerInventoryManager Instance { get; private set; }
    private Dictionary<int, ItemDetails> itemDetailsDictionary;
    [SerializeField] private GameObject itemPrefab; // Add this field for spawning items

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
    public async Task AddItemToPlayerInventory(PlayerCharacterData playerData, int itemCode)
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

                // Force UI update to show the new item/quantity immediately
                await ForceClientInventoryUpdate(playerData.userId, playerData.characterId);
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

    [Server]
    public async Task DropItemFromInventory(PlayerCharacterData playerData, int itemCode, int slotNumber, Vector3 dropPosition)
    {
        Debug.Log($"[SERVER] Processing item drop - ItemCode: {itemCode}, Slot: {slotNumber}");

        // 1. Validate item exists in Firebase for this character
        bool itemExists = await ValidateItemInFirebase(playerData.userId, playerData.characterId, itemCode, slotNumber);

        if (!itemExists)
        {
            Debug.LogError($"Item validation failed for {playerData.characterName}");
            return;
        }

        // 2. Remove item from Firebase inventory
        await RemoveItemFromFirebase(playerData.userId, playerData.characterId, itemCode, slotNumber);

        // 3. Spawn item in world with same layer as player
        SpawnItemInWorld(itemCode, dropPosition, playerData.gameObject);

        // 4. Update client UI (this should trigger automatically through existing system)
        Debug.Log($"Item {itemCode} successfully dropped for {playerData.characterName}");

        // 5. Force UI update to reflect the change immediately
        await ForceClientInventoryUpdate(playerData.userId, playerData.characterId);
    }

    [Server]
    private void SpawnItemInWorld(int itemCode, Vector3 dropPosition, GameObject playerWhoDropped)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("Item prefab not assigned in ServerInventoryManager!");
            return;
        }

        // Spawn the item using NetworkServer
        GameObject droppedItem = Instantiate(itemPrefab, dropPosition, Quaternion.identity);

        // Disable BoxCollider2D during drop animation to prevent immediate pickup
        BoxCollider2D boxCollider = droppedItem.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Debug.Log($"Item {itemCode} collider disabled during drop animation");
        }

        // Set item to same layer as the player who dropped it
        if (playerWhoDropped != null)
        {
            droppedItem.layer = playerWhoDropped.layer;
            Debug.Log($"Item {itemCode} set to layer: {LayerMask.LayerToName(playerWhoDropped.layer)}");
        }

        // Initialize the item with the correct item code
        Item itemComponent = droppedItem.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.Init(itemCode);
        }

        // Spawn it on the network so all clients can see it
        NetworkServer.Spawn(droppedItem);

        Vector3 startPosition = dropPosition; // Player position
        Vector3 endPosition = dropPosition + new Vector3(0, 2f, 0); // 2 units above player
        StartCoroutine(AnimateItemUpDown(droppedItem, startPosition, endPosition));

        // Re-enable collider after animation completes (1.1 seconds)
        StartCoroutine(EnableColliderAfterDelay(droppedItem, boxCollider, 1.1f));

        Debug.Log($"Item {itemCode} spawned at position {dropPosition}");
    }

    [Server]
    private IEnumerator EnableColliderAfterDelay(GameObject item, BoxCollider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if item and collider still exist before re-enabling
        if (item != null && collider != null)
        {
            collider.enabled = true;
            Debug.Log($"Item collider re-enabled after {delay} seconds");
        }
    }


    [Server]
    private async Task<bool> ValidateItemInFirebase(string userId, string characterId, int itemCode, int slotNumber)
    {
        Debug.Log($"[VALIDATION] Checking item {itemCode} in slot {slotNumber} for character {characterId}");

        try
        {
            // Get inventory from Firebase using same pattern as AddItemToPlayerInventory
            DatabaseReference inventoryRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/characters/{characterId}/inventory/items");

            DataSnapshot snapshot = await inventoryRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"[VALIDATION] No inventory found for character {characterId}");
                await ForceClientInventoryUpdate(userId, characterId);
                return false;
            }

            // Deserialize inventory
            var inventoryList = JsonConvert.DeserializeObject<List<InventoryItem>>(snapshot.GetRawJsonValue());

            if (inventoryList == null || slotNumber >= inventoryList.Count || slotNumber < 0)
            {
                Debug.LogError($"[VALIDATION] Invalid slot number {slotNumber} for character {characterId}");
                await ForceClientInventoryUpdate(userId, characterId);
                return false;
            }

            // Check if slot contains expected item with quantity > 0
            var slotItem = inventoryList[slotNumber];
            bool isValid = (slotItem.itemCode == itemCode && slotItem.itemQuantity > 0);

            if (!isValid)
            {
                Debug.LogWarning($"[VALIDATION] MISMATCH! Expected item {itemCode} in slot {slotNumber}, found item {slotItem.itemCode} with quantity {slotItem.itemQuantity}");
                Debug.LogWarning($"[VALIDATION] Client inventory is out of sync - forcing immediate update");

                // Force immediate client UI update and end the request
                await ForceClientInventoryUpdate(userId, characterId);
                return false;
            }

            Debug.Log($"[VALIDATION] SUCCESS! Item {itemCode} found in slot {slotNumber} with quantity {slotItem.itemQuantity}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VALIDATION] Firebase error: {e.Message}");
            // On error, force update to be safe
            await ForceClientInventoryUpdate(userId, characterId);
            return false;
        }
    }

    [Server]
    private async Task ForceClientInventoryUpdate(string userId, string characterId)
    {
        Debug.Log($"[FORCE_UPDATE] Triggering immediate inventory update for character {characterId}");

        try
        {
            // Find the player's NetworkConnectionToClient
            PlayerCharacterData playerData = null;

            // Search through active players to find the one with matching characterId
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn?.identity?.gameObject?.GetComponent<PlayerCharacterData>() is PlayerCharacterData data &&
                    data.characterId == characterId)
                {
                    playerData = data;
                    break;
                }
            }

            if (playerData == null)
            {
                Debug.LogError($"[FORCE_UPDATE] Could not find active player with characterId {characterId}");
                return;
            }

            // Get fresh inventory data from Firebase
            DatabaseReference inventoryRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/characters/{characterId}/inventory/items");

            DataSnapshot snapshot = await inventoryRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"[FORCE_UPDATE] No inventory data found for character {characterId}");
                return;
            }

            var inventoryList = JsonConvert.DeserializeObject<List<InventoryItem>>(snapshot.GetRawJsonValue());

            if (inventoryList == null)
            {
                Debug.LogError($"[FORCE_UPDATE] Failed to deserialize inventory for character {characterId}");
                return;
            }

            // Send updated inventory to the client via PlayerNetworkController
            PlayerNetworkController networkController = playerData.GetComponent<PlayerNetworkController>();
            if (networkController != null)
            {
                networkController.TargetReceiveInventoryData(playerData.connectionToClient, characterId, inventoryList.ToArray());
                Debug.Log($"[FORCE_UPDATE] Sent corrected inventory data to client for character {characterId}");
            }
            else
            {
                Debug.LogError($"[FORCE_UPDATE] PlayerNetworkController not found for character {characterId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FORCE_UPDATE] Error forcing inventory update: {e.Message}");
        }
    }


    [Server]
    private async Task RemoveItemFromFirebase(string userId, string characterId, int itemCode, int slotNumber)
    {
        Debug.Log($"[FIREBASE] Removing item {itemCode} from slot {slotNumber} for character {characterId}");

        try
        {
            // Get inventory from Firebase using same pattern as validation
            DatabaseReference inventoryRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/characters/{characterId}/inventory/items");

            DataSnapshot snapshot = await inventoryRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"[FIREBASE] No inventory found for character {characterId}");
                return;
            }

            // Deserialize inventory
            var inventoryList = JsonConvert.DeserializeObject<List<InventoryItem>>(snapshot.GetRawJsonValue());

            if (inventoryList == null || slotNumber >= inventoryList.Count || slotNumber < 0)
            {
                Debug.LogError($"[FIREBASE] Invalid slot number {slotNumber} for character {characterId}");
                return;
            }

            // Get the item in the specified slot
            var slotItem = inventoryList[slotNumber];

            // Verify this is the correct item (extra safety check)
            if (slotItem.itemCode != itemCode)
            {
                Debug.LogError($"[FIREBASE] Item mismatch! Expected {itemCode}, found {slotItem.itemCode} in slot {slotNumber}");
                return;
            }

            // Verify we have quantity to remove
            if (slotItem.itemQuantity <= 0)
            {
                Debug.LogError($"[FIREBASE] Cannot remove item {itemCode} - quantity is already {slotItem.itemQuantity}");
                return;
            }

            // Decrease quantity by 1
            slotItem.itemQuantity--;
            Debug.Log($"[FIREBASE] Decreased item {itemCode} quantity to {slotItem.itemQuantity}");

            // If quantity reaches 0, clear the slot (set to empty)
            if (slotItem.itemQuantity == 0)
            {
                slotItem.itemCode = 0;
                slotItem.itemQuantity = 0;
                Debug.Log($"[FIREBASE] Slot {slotNumber} cleared - item stack fully consumed");
            }

            // Save updated inventory back to Firebase
            Debug.Log($"[FIREBASE] Saving updated inventory to Firebase...");
            await inventoryRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(inventoryList));
            Debug.Log($"[FIREBASE] Successfully removed item {itemCode} from slot {slotNumber}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FIREBASE] Failed to remove item from Firebase: {e.Message}");
            Debug.LogError($"[FIREBASE] Stack trace: {e.StackTrace}");
        }
    }

    private IEnumerator AnimateItemUpDown(GameObject item, Vector3 startPosition, Vector3 endPosition)
    {
        if (item == null) yield break;

        // Store transform reference
        Transform itemTransform = item.transform;

        // Animation parameters
        float upDuration = 0.5f;   // Time to move up
        float downDuration = 0.4f; // Time to move down (slightly faster)
        float totalDuration = upDuration + downDuration;

        // Reset initial rotation to have some random starting angle
        float startRotation = Random.Range(0f, 360f);
        itemTransform.rotation = Quaternion.Euler(0f, 0f, startRotation);

        // Define positions
        Vector3 originalPosition = startPosition;
        Vector3 topPosition = originalPosition + new Vector3(0, 4f, 0);
        Vector3 finalPosition = endPosition; // Final position where item should land

        // Calculate target rotation that will end exactly at 0 degrees
        // First, determine a consistent rotation speed (degrees per second)
        float rotationSpeed = 720f;

        // Calculate total possible rotation during the entire animation
        float totalPossibleRotation = rotationSpeed * totalDuration;

        // Calculate how many full rotations we'll complete plus the amount needed to end at 0
        // We need to adjust our rotation so that: startRotation + totalRotation = 0 (or multiple of 360)
        // Meaning totalRotation = N*360 - startRotation (where N is an integer)
        int numFullRotations = Mathf.FloorToInt(totalPossibleRotation / 360f);
        float targetRotation = (numFullRotations * 360f) + (360f - (startRotation % 360f));

        // If the target rotation is too large for our duration, reduce it by 360 degrees
        if (targetRotation > totalPossibleRotation)
        {
            targetRotation -= 360f;
        }

        // Now we have a rotation amount that fits within our time and ends at 0
        float rotationPerSecond = targetRotation / totalDuration;

        // Track elapsed time for the whole animation
        float totalElapsedTime = 0f;

        // MOVE UP PHASE
        float elapsedTime = 0f;

        while (elapsedTime < upDuration)
        {
            if (item == null) yield break;

            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            totalElapsedTime += deltaTime;

            float t = Mathf.Clamp01(elapsedTime / upDuration);

            // Ease out for slowing down at peak (for position only)
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

            // Calculate new position (moving up)
            Vector3 newPosition = Vector3.Lerp(originalPosition, topPosition, smoothT);
            itemTransform.position = newPosition;

            // Apply rotation based on elapsed time (consistent speed)
            float currentRotation = startRotation + (totalElapsedTime * rotationPerSecond);
            itemTransform.rotation = Quaternion.Euler(0f, 0f, currentRotation);

            yield return null;
        }

        // Ensure it reached the top position
        if (item == null) yield break;
        itemTransform.position = topPosition;

        // MOVE DOWN PHASE
        elapsedTime = 0f;
        while (elapsedTime < downDuration)
        {
            if (item == null) yield break;

            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            totalElapsedTime += deltaTime;

            float t = Mathf.Clamp01(elapsedTime / downDuration);

            // Ease in for acceleration due to "gravity" (for position only)
            float smoothT = 1 - Mathf.Cos(t * Mathf.PI * 0.5f);

            // Calculate new position (moving from top to final position)
            Vector3 newPosition = Vector3.Lerp(topPosition, finalPosition, smoothT);
            itemTransform.position = newPosition;

            // Apply rotation based on elapsed time (consistent speed)
            float currentRotation = startRotation + (totalElapsedTime * rotationPerSecond);
            itemTransform.rotation = Quaternion.Euler(0f, 0f, currentRotation);

            yield return null;
        }

        // Set final position and rotation exactly
        if (item != null)
        {
            // Force exact final position
            itemTransform.position = finalPosition;

            // Rotation should naturally be at or very close to 0, but set it exactly to be safe
            itemTransform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Log for verification
            Debug.Log($"Item finalized at position: {finalPosition}, rotation: {itemTransform.rotation.eulerAngles}");

            // Add ItemFloat AFTER animation completes (much simpler approach!)
            Item itemComponent = item.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.RpcAddItemFloat();
            }
        }
    }

    [Server]
    public async Task SwapInventoryItems(PlayerCharacterData playerData, int fromSlot, int toSlot, 
        int expectedFromItemCode, int expectedToItemCode)
    {
        string userId = playerData.userId;
        string characterId = playerData.characterId;
        
        Debug.Log($"[SWAP] Validating swap - From: slot {fromSlot} expecting item {expectedFromItemCode}, " +
                $"To: slot {toSlot} expecting item {expectedToItemCode}");
        
        try
        {
            // Get inventory from Firebase
            DatabaseReference inventoryRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{userId}/characters/{characterId}/inventory/items");
                
            DataSnapshot snapshot = await inventoryRef.GetValueAsync();
            
            if (!snapshot.Exists)
            {
                Debug.LogError($"[SWAP] No inventory found for character {characterId}");
                await ForceClientInventoryUpdate(userId, characterId);
                return;
            }
            
            var inventoryList = JsonConvert.DeserializeObject<List<InventoryItem>>(snapshot.GetRawJsonValue());
            
            // Validate slot numbers are in range
            if (fromSlot < 0 || fromSlot >= inventoryList.Count || 
                toSlot < 0 || toSlot >= inventoryList.Count)
            {
                Debug.LogError($"[SWAP] Invalid slot numbers: {fromSlot}, {toSlot}. Inventory size: {inventoryList.Count}");
                await ForceClientInventoryUpdate(userId, characterId);
                return;
            }
            
            // Check if swapping with same slot
            if (fromSlot == toSlot)
            {
                Debug.Log($"[SWAP] Same slot selected, no swap needed");
                return;
            }
            
            // Get the actual items from Firebase
            var fromItem = inventoryList[fromSlot];
            var toItem = inventoryList[toSlot];
            
            // Log what we found for debugging
            Debug.Log($"[SWAP] Firebase validation:");
            Debug.Log($"[SWAP] Slot {fromSlot}: ItemCode={fromItem.itemCode}, Quantity={fromItem.itemQuantity}");
            Debug.Log($"[SWAP] Slot {toSlot}: ItemCode={toItem.itemCode}, Quantity={toItem.itemQuantity}");
            
            // Validate the items match what client expects
            bool isValid = true;
            
            if (fromItem.itemCode != expectedFromItemCode)
            {
                Debug.LogError($"[SWAP] MISMATCH! Slot {fromSlot} has item {fromItem.itemCode}, " +
                            $"but client expects {expectedFromItemCode}");
                isValid = false;
            }
            
            if (toItem.itemCode != expectedToItemCode)
            {
                Debug.LogError($"[SWAP] MISMATCH! Slot {toSlot} has item {toItem.itemCode}, " +
                            $"but client expects {expectedToItemCode}");
                isValid = false;
            }
            
            // Additional validation: ensure at least one slot has an item (prevent swapping two empty slots)
            if (fromItem.itemCode == 0 && toItem.itemCode == 0)
            {
                Debug.LogWarning($"[SWAP] Both slots are empty, no swap needed");
                await ForceClientInventoryUpdate(userId, characterId);
                return;
            }
            
            if (!isValid)
            {
                Debug.LogError($"[SWAP] Client inventory out of sync! Forcing update...");
                await ForceClientInventoryUpdate(userId, characterId);
                return;
            }
            
            // If validation passes, perform the swap
            Debug.Log($"[SWAP] Validation passed, performing swap");
            
            // Perform the swap
            inventoryList[fromSlot] = toItem;
            inventoryList[toSlot] = fromItem;
            
            // Save back to Firebase
            Debug.Log($"[SWAP] Saving swapped inventory to Firebase...");
            await inventoryRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(inventoryList));
            
            // Force UI update to ensure sync
            await ForceClientInventoryUpdate(userId, characterId);
            
            Debug.Log($"[SWAP] Successfully swapped items between slots {fromSlot} and {toSlot}");
            Debug.Log($"[SWAP] Final state - Slot {fromSlot}: ItemCode={toItem.itemCode}, Slot {toSlot}: ItemCode={fromItem.itemCode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SWAP] Failed to swap items: {e.Message}");
            Debug.LogError($"[SWAP] Stack trace: {e.StackTrace}");
            
            // On any error, force update to resync
            await ForceClientInventoryUpdate(userId, characterId);
        }
    }
}

