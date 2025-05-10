using Mirror;
using System.Collections;
using UnityEngine;

//------------------------------------------------------------------------------
// AUTHENTICATION MESSAGES
//------------------------------------------------------------------------------

// Client to server: Send Firebase authentication token for verification
public struct AuthenticationRequestMessage : NetworkMessage
{
    public string token;
}

// Server to client: Response to authentication request
public struct AuthenticationResponseMessage : NetworkMessage
{
    public bool success;      // Whether authentication was successful
    public string userId;     // Firebase user ID for the authenticated user
}

//------------------------------------------------------------------------------
// CHARACTER SELECTION & PREVIEW MESSAGES
//------------------------------------------------------------------------------

// Client to server: Request character list for a user
public struct CharacterPreviewRequestMessage : NetworkMessage
{
    public string userId;     // User ID to fetch characters for
}

// Server to client: Response containing all character data for selection screen
public struct CharacterPreviewResponseMessage : NetworkMessage
{
    public ClientPlayerDataManager.CharacterInfo[] characters;     // Basic character info
    public CharacterEquipmentPair[] equipmentData;                 // Equipment for each character
    public CharacterLocationPair[] locationData;                   // Location data for each character
}

// Helper struct for character equipment mapping
public struct CharacterEquipmentPair : NetworkMessage
{
    public string characterId;
    public ClientPlayerDataManager.EquipmentData equipment;
}

// Helper struct for character location mapping
public struct CharacterLocationPair : NetworkMessage
{
    public string characterId;
    public ClientPlayerDataManager.LocationData location;
}

// Client to server: Request detailed data for a specific character
public struct CharacterDetailRequestMessage : NetworkMessage
{
    public string characterId;    // ID of character to fetch details for
}

//------------------------------------------------------------------------------
// CHARACTER CREATION MESSAGES
//------------------------------------------------------------------------------

// Client to server: Request available options for character creation
public struct RequestCharacterCreationOptionsMessage : NetworkMessage
{
    // Empty message, just a request
}

// Server to client: Available options for character creation
public struct CharacterCreationOptionsMessage : NetworkMessage
{
    public string[] availableClasses;     // List of character classes
    public int[] bodyOptions;             // Available body types/skins
    public int[] headOptions;             // Available head options
    public int[] hairOptions;             // Available hair styles
    public int[] torsoOptions;            // Available torso/clothing options
    public int[] legsOptions;             // Available leg/pants options
    public bool atCharacterLimit;         // Whether user has reached character limit
}

// Client to server: Request to create a new character
public struct CreateCharacterRequestMessage : NetworkMessage
{
    public string characterName;      // Name for the new character
    public string characterClass;     // Class of the new character
    public int headItem;              // Selected head item
    public int bodyItem;              // Selected body/skin type
    public int hairItem;              // Selected hair style
    public int torsoItem;             // Selected torso/clothing
    public int legsItem;              // Selected legs/pants
}

// Server to client: Response to character creation request
public struct CreateCharacterResponseMessage : NetworkMessage
{
    public bool success;          // Whether creation was successful
    public string message;        // Success/error message
    public string characterId;    // ID of newly created character (if successful)
}

//------------------------------------------------------------------------------
// SCENE MANAGEMENT MESSAGES
//------------------------------------------------------------------------------

// Client to server: Request for player spawning
public struct SpawnPlayerRequestMessage : NetworkMessage
{
    public string characterId;    // Character to spawn
}

// Client to server: Request to change game scene
public struct SceneChangeRequestMessage : NetworkMessage
{
    public string sceneName;      // Target scene name
    public string characterId;    // Character requesting the scene change
}

// Client to server: Request for lobby scene transition
public struct LobbySceneTransitionRequestMessage : NetworkMessage
{
    public string targetScene;    // Target lobby scene name
}

// Server to client: Response to lobby scene transition request
public struct LobbySceneTransitionResponseMessage : NetworkMessage
{
    public bool approved;         // Whether transition is allowed
    public string sceneName;      // Approved scene name
    public string message;        // Optional message (especially for denials)
}
