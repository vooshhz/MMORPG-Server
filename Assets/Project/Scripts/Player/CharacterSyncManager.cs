using UnityEngine;
using Mirror;
using System;
using System.Collections;

public class CharacterSyncManager : NetworkBehaviour
{
    // Events for different stages of the sync process
    public static event Action OnSyncStarted;
    public static event Action OnEquipmentSynced;
    public static event Action OnInventorySynced;
    public static event Action OnStatsSynced;
    public static event Action OnSyncCompleted;
    
    // Called when the player object is spawned
    public override void OnStartClient()
    {
        base.OnStartClient();
    }
    
    private IEnumerator SyncCharacterData()
    {
        OnSyncStarted?.Invoke();
        
        // Get references
        var playerController = GetComponent<PlayerNetworkController>();
        var characterData = GetComponentInChildren<PlayerCharacterData>();
        
        if (characterData == null)
        {
            Debug.LogError("PlayerCharacterData component not found!");
            yield break;
        }
        
        string characterId = playerController.characterId;
        characterData.characterId = characterId;
        Debug.Log($"[Client] 🧠 SyncCharacterData started. characterId = {playerController.characterId}");
        yield return new WaitUntil(() => !string.IsNullOrEmpty(playerController.characterId));

        // Request data
        CmdRequestCharacterData(characterId);
        
        // Wait and store equipment data
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasEquipmentData);
        var equipment = ClientPlayerDataManager.Instance.GetEquipment(characterId);
        characterData.headItemNumber = equipment.head;
        characterData.bodyItemNumber = equipment.body;
        characterData.hairItemNumber = equipment.hair;
        characterData.torsoItemNumber = equipment.torso;
        characterData.legsItemNumber = equipment.legs;
        OnEquipmentSynced?.Invoke();
        
        // Wait and store inventory data
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasInventoryData);
        var inventoryItems = ClientPlayerDataManager.Instance.GetInventory(characterId);
        characterData.inventoryItems.Clear();
        foreach (var item in inventoryItems)
        {
            characterData.inventoryItems.Add(new PlayerCharacterData.InventoryItem 
            { 
                itemCode = item.itemCode, 
                quantity = item.quantity 
            });
        }
        OnInventorySynced?.Invoke();
        
        // Wait and store character info
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasCharacterData);
        var charInfo = ClientPlayerDataManager.Instance.GetCharacterInfo(characterId);
        if (charInfo != null)
        {
            characterData.characterName = charInfo.characterName;
            characterData.characterClass = charInfo.characterClass;
            characterData.level = charInfo.level;
            characterData.experience = charInfo.experience;
        }
        
        // Store location data if available
        var location = ClientPlayerDataManager.Instance.GetLocation(characterId);
        if (location != null)
        {
            characterData.currentSceneName = location.sceneName;
            characterData.position = location.position;
        }
        
        // Mark as fully loaded
        characterData.isFullyLoaded = true;
        
        // Update sync time and apply data to components
        characterData.UpdateSyncTime();
        characterData.ApplyEquipmentToCharacter();
        characterData.ApplyCharacterInfo();
        characterData.ApplyInventory();
        
        OnStatsSynced?.Invoke();
        OnSyncCompleted?.Invoke();
        
        Debug.Log($"Character sync completed for {characterData.characterName} (ID: {characterId})");
    }

    [Command]
    private void CmdRequestCharacterData(string characterId)
    {
        // This will be handled by PlayerNetworkController.CmdRequestCharacterData
        // but we're ensuring it gets called in the right sequence
        var networkController = GetComponent<PlayerNetworkController>();
        networkController.CmdRequestCharacterData(characterId);
    }
}