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
        
        if (isLocalPlayer)
        {
            StartCoroutine(SyncCharacterData());
        }
    }
    
    private IEnumerator SyncCharacterData()
    {
        OnSyncStarted?.Invoke();
        
        // Request latest data from server
        var playerController = GetComponent<PlayerNetworkController>();
        string characterId = playerController.characterId;
        
        // Request equipment data
        CmdRequestCharacterData(characterId);
        
        // Wait for equipment to be synced
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasEquipmentData);
        
        OnEquipmentSynced?.Invoke();
        
        // Apply equipment to character model
        var characterAnimator = GetComponent<CharacterAnimator>();
        var equipment = ClientPlayerDataManager.Instance.GetEquipment(characterId);
        
        characterAnimator.headItemNumber = equipment.head;
        characterAnimator.bodyItemNumber = equipment.body;
        characterAnimator.hairItemNumber = equipment.hair;
        characterAnimator.torsoItemNumber = equipment.torso;
        characterAnimator.legsItemNumber = equipment.legs;
        characterAnimator.RefreshCurrentFrame();
        
        // Wait for inventory to be synced
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasInventoryData);
        
        OnInventorySynced?.Invoke();
        
        // Wait for stats to be synced
        yield return new WaitUntil(() => ClientPlayerDataManager.Instance.HasCharacterData);
        
        OnStatsSynced?.Invoke();
        
        // All sync completed
        OnSyncCompleted?.Invoke();
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