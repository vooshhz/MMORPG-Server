using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class ClientPlayerDataManager : MonoBehaviour
{
    /// Singleton and Instance Variables
    private Dictionary<string, bool> characterDataCompleteness = new Dictionary<string, bool>(); // Tracks if character data is fully loaded
    public static ClientPlayerDataManager Instance { get; private set; } // Singleton instance

    /// Events
    public event Action OnNetworkIdentityReady; // Fired when network identity is ready
    public event Action OnCharacterDataReceived; // Fired when character info is received
    public event Action OnInventoryDataReceived; // Fired when inventory data is received
    public event Action OnEquipmentDataReceived; // Fired when equipment data is received
    public event Action OnLocationDataReceived; // Fired when location data is received
    public event Action<string> OnCharacterSelected; // Fired when a character is selected

    /// State Flags
    public bool HasCharacterData { get; private set; } // Whether any character data is loaded
    public bool HasInventoryData { get; private set; } // Whether inventory data is loaded
    public bool HasEquipmentData { get; private set; } // Whether equipment data is loaded
    public bool HasLocationData { get; private set; } // Whether location data is loaded
    public string SelectedCharacterId { get; private set; } // Currently selected character ID

    /// Data Storage
    private Dictionary<string, CharacterInfo> characterInfos = new Dictionary<string, CharacterInfo>(); // Character basic info by ID
    private Dictionary<string, EquipmentData> equipmentData = new Dictionary<string, EquipmentData>(); // Equipment data by character ID
    private Dictionary<string, List<InventoryItem>> inventoryData = new Dictionary<string, List<InventoryItem>>(); // Inventory data by character ID
    private Dictionary<string, LocationData> locationData = new Dictionary<string, LocationData>(); // Location data by character ID

    /// Data Classes
    [System.Serializable]
    public class CharacterInfo
    {
        public string id; // Unique character identifier
        public string characterName; // Character name
        public string characterClass; // Character class (Warrior, Mage, etc.)
        public int level; // Character level
        public int experience; // Character experience points
    }

    [System.Serializable]
    public class EquipmentData
    {
        public int head; // Head equipment item ID
        public int body; // Body equipment item ID
        public int hair; // Hair equipment item ID
        public int torso; // Torso equipment item ID
        public int legs; // Legs equipment item ID
    }

    [System.Serializable]
    public class InventoryItem
    {
        public int itemCode; // Item identifier code
        public int quantity; // Quantity of the item
    }

    [System.Serializable]
    public class LocationData
    {
        public string sceneName; // Scene the character is in
        public Vector3 position; // Position within the scene
    }

    /// Lifecycle Methods
    private void Awake()
    {
        if (Instance != null && Instance != this) // Check if instance already exists
        {
            Destroy(gameObject); // Destroy duplicate
            return; // Exit early
        }

        Instance = this; // Set singleton instance
        DontDestroyOnLoad(gameObject); // Persist across scene changes
    }

    /// Character Selection Methods
    public void SetSelectedCharacterId(string characterId)
    {
        SelectedCharacterId = characterId; // Store selected character ID
        Debug.Log($"[ClientPlayerDataManager] SelectedCharacterId set to: {characterId}"); // Log selection
        OnCharacterSelected?.Invoke(characterId); // Notify listeners
    }

    public bool IsCharacterDataComplete(string characterId)
    {
        return characterDataCompleteness.ContainsKey(characterId) && characterDataCompleteness[characterId]; // Check if character data is complete
    }

    // Legacy selection method for compatibility
    public void SelectCharacter(string characterId)
    {
        SetSelectedCharacterId(characterId); // Forward to new method
    }

    /// Network Request Methods
    public void RequestAllCharacterData(string userId)
    {
        if (NetworkClient.connection == null || !NetworkClient.connection.isReady) // Check network connection
        {
            Debug.LogError("NetworkClient not ready."); // Log error
            return; // Exit if not connected
        }

        ClearAllData(); // Clear existing data

        NetworkClient.Send(new CharacterPreviewRequestMessage // Send request message
        {
            userId = userId // Set user ID in message
        });
    }

    public void RequestCharacterData(string userId, string characterId)
    {
        if (NetworkClient.connection == null || !NetworkClient.connection.isReady) // Check network connection
        {
            Debug.LogError("NetworkClient not ready."); // Log error
            return; // Exit if not connected
        }

        NetworkClient.Send(new CharacterDetailRequestMessage // Send request message
        {
            characterId = characterId // Set character ID in message
        });
    }

    /// Data Management Methods
    public void ClearAllData()
    {
        characterInfos.Clear(); // Clear character info
        equipmentData.Clear(); // Clear equipment data
        inventoryData.Clear(); // Clear inventory data
        locationData.Clear(); // Clear location data

        HasCharacterData = false; // Reset character data flag
        HasInventoryData = false; // Reset inventory data flag
        HasEquipmentData = false; // Reset equipment data flag
        HasLocationData = false; // Reset location data flag
    }

    /// Data Receiving Methods
    public void ReceiveCharacterInfos(List<CharacterInfo> characters)
    {
        characterInfos.Clear(); // Clear existing character info
        foreach (var character in characters) // Process each character
        {
            characterInfos[character.id] = character; // Store character by ID
        }
        HasCharacterData = true; // Set character data flag
        OnCharacterDataReceived?.Invoke(); // Notify listeners
    }

    public void ReceiveEquipmentData(string characterId, EquipmentData equipment)
    {
        equipmentData[characterId] = equipment; // Store equipment data
        HasEquipmentData = true; // Set equipment data flag
        OnEquipmentDataReceived?.Invoke(); // Notify listeners
    }

    public void ReceiveInventoryData(string characterId, List<InventoryItem> items)
    {
        inventoryData[characterId] = items; // Store inventory data
        HasInventoryData = true; // Set inventory data flag
        OnInventoryDataReceived?.Invoke(); // Notify listeners
    }

    public void ReceiveLocationData(string characterId, LocationData location)
    {
        locationData[characterId] = location; // Store location data
        HasLocationData = true; // Set location data flag
        OnLocationDataReceived?.Invoke(); // Notify listeners
    }

    public void ReceiveCharacterPreviewData(CharacterInfo[] characters, CharacterEquipmentPair[] equipmentPairs, CharacterLocationPair[] locationPairs)
    {
        ReceiveCharacterInfos(new List<CharacterInfo>(characters)); // Process character info

        foreach (var pair in equipmentPairs) // Process equipment data
        {
            characterDataCompleteness[pair.characterId] = true; // Mark character data as complete
            ReceiveEquipmentData(pair.characterId, pair.equipment); // Store equipment data
        }

        if (locationPairs != null) // Check if location data exists
        {
            foreach (var pair in locationPairs) // Process location data
            {
                ReceiveLocationData(pair.characterId, pair.location); // Store location data
            }
        }
    }

    /// Data Access Methods
    public List<string> GetAllCharacterIds()
    {
        return new List<string>(characterInfos.Keys); // Return all character IDs
    }

    public CharacterInfo GetCharacterInfo(string characterId)
    {
        return characterInfos.TryGetValue(characterId, out var info) ? info : null; // Get character info safely
    }

    public EquipmentData GetEquipment(string characterId)
    {
        return equipmentData.TryGetValue(characterId, out var data) ? data : null; // Get equipment data safely
    }

    public List<InventoryItem> GetInventory(string characterId)
    {
        return inventoryData.TryGetValue(characterId, out var items) ? items : new List<InventoryItem>(); // Get inventory data safely
    }

    public LocationData GetLocation(string characterId)
    {
        return locationData.TryGetValue(characterId, out var location) ? location : null; // Get location data safely
    }
}