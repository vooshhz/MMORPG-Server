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
        Debug.Log($"[Client] 🧠 SyncCharacterData started. characterId = {characterId}");
        
        // Request data
        CmdRequestCharacterData(characterId);
        
        // Add timeout mechanism
        float timeoutSeconds = 10.0f;
        float elapsedTime = 0f;
        
        // Wait for equipment data with timeout
        while (!ClientPlayerDataManager.Instance.HasEquipmentData && elapsedTime < timeoutSeconds)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (ClientPlayerDataManager.Instance.HasEquipmentData)
        {
            var equipment = ClientPlayerDataManager.Instance.GetEquipment(characterId);
            if (equipment != null)
            {
                characterData.headItemNumber = equipment.head;
                characterData.bodyItemNumber = equipment.body;
                characterData.hairItemNumber = equipment.hair;
                characterData.torsoItemNumber = equipment.torso;
                characterData.legsItemNumber = equipment.legs;
                OnEquipmentSynced?.Invoke();
                Debug.Log($"Equipment synced: head={equipment.head}, body={equipment.body}, hair={equipment.hair}");
            }
        }
        
        // Reset timeout for character info
        elapsedTime = 0f;
        
        // Wait for character info with timeout
        while (!ClientPlayerDataManager.Instance.HasCharacterData && elapsedTime < timeoutSeconds)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (ClientPlayerDataManager.Instance.HasCharacterData)
        {
            var charInfo = ClientPlayerDataManager.Instance.GetCharacterInfo(characterId);
            if (charInfo != null)
            {
                characterData.characterName = charInfo.characterName;
                characterData.characterClass = charInfo.characterClass;
                characterData.level = charInfo.level;
                characterData.experience = charInfo.experience;
                Debug.Log($"Character info synced: {charInfo.characterName}, Level {charInfo.level} {charInfo.characterClass}");
            }
        }
        
        // Force-apply data to components
        characterData.isFullyLoaded = true;
        characterData.UpdateSyncTime();
        characterData.ApplyEquipmentToCharacter();
        characterData.ApplyCharacterInfo();
        
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