using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// /// Handles network communication for player data between client and server
public class PlayerNetworkController : NetworkBehaviour
{
    // Reference to the client data manager singleton
    private ClientPlayerDataManager clientDataManager;
    
    // Character ID for this player instance
    public string characterId { get; private set; }

    private void Start()
    {
        // Only initialize for the local player
        if (isLocalPlayer)
        {
            // Get reference to the client data manager singleton
            clientDataManager = ClientPlayerDataManager.Instance;
        }
    }
    
    /// <summary>
    /// Sets the character ID for this player instance
    /// </summary>
    public void SetCharacterId(string id)
    {
        // Store the character ID
        characterId = id;
    }
    
    /// <summary>
    /// Client command to request all characters for the current user
    /// </summary>
    [Command]
    public void CmdRequestAllCharacterData()
    {
        // Forward request to the server player data manager
        ServerPlayerDataManager.Instance.HandleAllCharacterDataRequest(connectionToClient);
    }
    
    /// <summary>
    /// Client command to request detailed data for a specific character
    /// </summary>
    [Command]
    public void CmdRequestCharacterData(string characterId)
    {
        // Forward request to the server player data manager
        ServerPlayerDataManager.Instance.HandleCharacterDataRequest(connectionToClient, characterId);
    }
    
    /// <summary>
    /// Server to clients: Receive basic character info for all characters
    /// </summary>
    [ClientRpc]
    public void RpcReceiveCharacterInfos(ClientPlayerDataManager.CharacterInfo[] characters)
    {
        // Only process on the local player
        if (!isLocalPlayer) return;
        
        // Convert array to list and pass to client data manager
        clientDataManager.ReceiveCharacterInfos(new List<ClientPlayerDataManager.CharacterInfo>(characters));
    }
    
    /// <summary>
    /// Server to specific client: Receive equipment data for a character
    /// </summary>
    [TargetRpc]
    public void TargetReceiveEquipmentData(NetworkConnection target, string characterId, 
        int head, int body, int hair, int torso, int legs)
    {
        // Create equipment data object
        var equipment = new ClientPlayerDataManager.EquipmentData
        {
            head = head,
            body = body,
            hair = hair,
            torso = torso,
            legs = legs
        };
        
        // Pass to client data manager
        clientDataManager.ReceiveEquipmentData(characterId, equipment);
    }
    
    /// <summary>
    /// Server to specific client: Receive inventory data for a character
    /// </summary>
    [TargetRpc]
    public void TargetReceiveInventoryData(NetworkConnection target, string characterId, 
        InventoryItem[] items)
    {
        // Convert array to list and pass to client data manager
        clientDataManager.ReceiveInventoryData(characterId, new List<InventoryItem>(items));
    }

    /// <summary>
    /// Server to specific client: Receive location data for a character
    /// </summary>
    [TargetRpc]
    public void TargetReceiveLocationData(NetworkConnection target, string characterId, 
        string sceneName, Vector3 position)
    {
        // Only process on the local player
        if (!isLocalPlayer) return;
        
        // Create location data object
        var locationData = new ClientPlayerDataManager.LocationData
        {
            sceneName = sceneName,
            position = position
        };
        
        // Pass to client data manager
        clientDataManager.ReceiveLocationData(characterId, locationData);
    }
}