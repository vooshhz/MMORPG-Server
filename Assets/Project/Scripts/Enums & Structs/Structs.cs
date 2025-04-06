using Mirror;
using System.Collections.Generic;

public struct AuthenticationRequestMessage : NetworkMessage
{
    public string token;
}

public struct AuthenticationResponseMessage : NetworkMessage
{
    public bool success;
    public string userId; // Added user ID to the response
}

public struct CharacterPreviewRequestMessage : NetworkMessage
{
    public string userId;
}

// Use arrays instead of Dictionary
public struct CharacterEquipmentPair : NetworkMessage
{
    public string characterId;
    public ClientPlayerDataManager.EquipmentData equipment;
}

public struct CharacterPreviewResponseMessage : NetworkMessage
{
    public ClientPlayerDataManager.CharacterInfo[] characters;
    public CharacterEquipmentPair[] equipmentData;
}