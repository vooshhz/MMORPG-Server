using Mirror;
using System.Collections;
using UnityEngine;

//==============================================================================
// AUTHENTICATION MESSAGES
//==============================================================================

public struct AuthenticationRequestMessage : NetworkMessage
{
    public string token;     // Firebase authentication token from client
}

public struct AuthenticationResponseMessage : NetworkMessage
{
    public bool success;     // Whether authentication succeeded
    public string userId;    // Firebase user ID for authenticated user
}

//==============================================================================
// CHARACTER SELECTION MESSAGES
//==============================================================================

public struct CharacterPreviewRequestMessage : NetworkMessage
{
    public string userId;    // Request character list for this user ID
}

public struct CharacterPreviewResponseMessage : NetworkMessage
{
    public ClientPlayerDataManager.CharacterInfo[] characters;     // Basic info for all characters
    public CharacterEquipmentPair[] equipmentData;                 // Equipment data for each character
    public CharacterLocationPair[] locationData;                   // Position data for each character
}

public struct CharacterEquipmentPair : NetworkMessage
{
    public string characterId;                            // Character identifier
    public ClientPlayerDataManager.EquipmentData equipment;  // Character's equipped items
}

public struct CharacterLocationPair : NetworkMessage
{
    public string characterId;                            // Character identifier
    public ClientPlayerDataManager.LocationData location;    // Character's position and scene
}

public struct CharacterDetailRequestMessage : NetworkMessage
{
    public string characterId;    // Request detailed data for specific character
}

//==============================================================================
// CHARACTER CREATION MESSAGES
//==============================================================================

public struct RequestCharacterCreationOptionsMessage : NetworkMessage
{
    // Empty request message for available character creation options
}

public struct CharacterCreationOptionsMessage : NetworkMessage
{
    public string[] availableClasses;  // Available character classes
    public int[] bodyOptions;          // Available body types/skins
    public int[] headOptions;          // Available head options
    public int[] hairOptions;          // Available hairstyles
    public int[] torsoOptions;         // Available clothing/armor options
    public int[] legsOptions;          // Available pants/leg options
    public bool atCharacterLimit;      // Whether user has reached max characters
}

public struct CreateCharacterRequestMessage : NetworkMessage
{
    public string characterName;   // New character's name
    public string characterClass;  // New character's class
    public int headItem;           // Selected head option
    public int bodyItem;           // Selected body/skin option
    public int hairItem;           // Selected hairstyle option
    public int torsoItem;          // Selected torso option
    public int legsItem;           // Selected leg option
}

public struct CreateCharacterResponseMessage : NetworkMessage
{
    public bool success;       // Whether creation succeeded
    public string message;     // Success/error message details
    public string characterId; // ID of new character (if successful)
}

//==============================================================================
// SCENE MANAGEMENT MESSAGES
//==============================================================================

public struct SpawnPlayerRequestMessage : NetworkMessage
{
    public string characterId;    // Character to spawn in world
}

public struct SceneChangeRequestMessage : NetworkMessage
{
    public string sceneName;     // Requested target scene
    public string characterId;   // Character requesting the change
}

public struct LobbySceneTransitionRequestMessage : NetworkMessage
{
    public string targetScene;    // Requested lobby scene
}

public struct LobbySceneTransitionResponseMessage : NetworkMessage
{
    public bool approved;      // Whether transition is allowed
    public string sceneName;   // Approved scene name
    public string message;     // Optional message (especially for denials)
}

public struct GameSceneTransitionRequestMessage : NetworkMessage
{
    public string targetScene;    // Requested game scene
    public string characterId;    // Character requesting the transition
}

public struct GameSceneTransitionResponseMessage : NetworkMessage
{
    public bool approved;         // Whether transition is allowed
    public string sceneName;      // Approved scene name
    public string message;        // Optional message (especially for denials)
    public Vector3 spawnPosition; // Where to spawn the player in new scene
}

public struct PlayerSceneReadyMessage : NetworkMessage
{
    public string characterId;    // Character that's ready in the new scene
}