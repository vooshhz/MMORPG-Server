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

public struct SpawnPlayerRequestMessage : NetworkMessage
{
    public string characterId;
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
    public CharacterLocationPair[] locationData;
}

public struct CharacterLocationPair : NetworkMessage
{
    public string characterId;
    public ClientPlayerDataManager.LocationData location;
}

public struct CharacterDetailRequestMessage : NetworkMessage
{
    public string characterId;
}

public struct RequestCharacterCreationOptionsMessage : NetworkMessage
{
    // Empty message, just a request
}

public struct CharacterCreationOptionsMessage : NetworkMessage
{
    public string[] availableClasses;
    public int[] bodyOptions;
    public int[] headOptions;
    public int[] hairOptions;
    public int[] torsoOptions;
    public int[] legsOptions;
    public bool atCharacterLimit; 
}

public struct CreateCharacterRequestMessage : NetworkMessage
{
    public string characterName;
    public string characterClass;
    public int headItem;
    public int bodyItem;
    public int hairItem;
    public int torsoItem;
    public int legsItem;
}

public struct CreateCharacterResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
    public string characterId;
}

public struct SceneChangeRequestMessage : NetworkMessage
{
    public string sceneName;
}

public struct SceneChangeApprovedMessage : NetworkMessage
{
    public string sceneName;
    public string characterId; // Add character ID for player spawning after scene change
    public bool spawnAfterChange; // Flag to indicate if player should be spawned after this scene change
}

// New message: Client confirms scene load is complete
public struct SceneChangeCompletedMessage : NetworkMessage
{
    public string sceneName;
    public string characterId; // Send back the character ID for player spawning
}