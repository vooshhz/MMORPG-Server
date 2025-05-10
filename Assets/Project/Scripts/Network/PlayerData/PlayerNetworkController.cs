using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerNetworkController : NetworkBehaviour
{
    private ClientPlayerDataManager clientDataManager;
    public string characterId { get; private set; }

    private void Start()
    {
        if (isLocalPlayer)
        {
            clientDataManager = ClientPlayerDataManager.Instance;
        }
    }
    public void SetCharacterId(string id)
    {
        characterId = id;
    }
    [Command]
    public void CmdRequestAllCharacterData()
    {
        // Server will handle this
        ServerPlayerDataManager.Instance.HandleAllCharacterDataRequest(connectionToClient);
    }
    
    [Command]
    public void CmdRequestCharacterData(string characterId)
    {
        // Server will handle this
        ServerPlayerDataManager.Instance.HandleCharacterDataRequest(connectionToClient, characterId);
    }
    
    // Client-side methods to receive data from server
    [ClientRpc]
    public void RpcReceiveCharacterInfos(ClientPlayerDataManager.CharacterInfo[] characters)
    {
        if (!isLocalPlayer) return;
        clientDataManager.ReceiveCharacterInfos(new List<ClientPlayerDataManager.CharacterInfo>(characters));
    }
    
    [TargetRpc]
    public void TargetReceiveEquipmentData(NetworkConnection target, string characterId, 
        int head, int body, int hair, int torso, int legs)
    {
        var equipment = new ClientPlayerDataManager.EquipmentData
        {
            head = head,
            body = body,
            hair = hair,
            torso = torso,
            legs = legs
        };
        
        clientDataManager.ReceiveEquipmentData(characterId, equipment);
    }
    
    [TargetRpc]
    public void TargetReceiveInventoryData(NetworkConnection target, string characterId, 
        ClientPlayerDataManager.InventoryItem[] items)
    {
        if (!isLocalPlayer) return;
        clientDataManager.ReceiveInventoryData(characterId, new List<ClientPlayerDataManager.InventoryItem>(items));
    }

    [TargetRpc]
    public void TargetReceiveLocationData(NetworkConnection target, string characterId, 
        string sceneName, Vector3 position)
    {
        if (!isLocalPlayer) return;
        var locationData = new ClientPlayerDataManager.LocationData
        {
            sceneName = sceneName,
            position = position
        };
        clientDataManager.ReceiveLocationData(characterId, locationData);
}

}