using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;


public class ClientPlayerDataManager : MonoBehaviour
{
    private Dictionary<string, bool> characterDataCompleteness = new Dictionary<string, bool>();
    public event Action OnNetworkIdentityReady;
    // Singleton pattern
    public static ClientPlayerDataManager Instance { get; private set; }

    // Events
    public event Action OnCharacterDataReceived;
    public event Action OnInventoryDataReceived;
    public event Action OnEquipmentDataReceived;
    public event Action OnLocationDataReceived;
    
    // Data state tracking
    public bool HasCharacterData { get; private set; }
    public bool HasInventoryData { get; private set; }
    public bool HasEquipmentData { get; private set; }
    public bool HasLocationData { get; private set; }

    public bool IsCharacterDataComplete(string characterId)
    {
        return characterDataCompleteness.ContainsKey(characterId) && characterDataCompleteness[characterId];
    }
    
    // Character data
    [System.Serializable]
    public class CharacterInfo
    {
        public string id;
        public string characterName;
        public string characterClass;
        public int level;
        public int experience;
    }
    
    [System.Serializable]
    public class EquipmentData
    {
        public int head;
        public int body;
        public int hair;
        public int torso;
        public int legs;
    }
    
    [System.Serializable]
    public class InventoryItem
    {
        public int itemCode;
        public int quantity;
    }
    
    [System.Serializable]
    public class LocationData
    {
        public string sceneName;
        public Vector3 position;
    }
    
    // Data storage
    private Dictionary<string, CharacterInfo> characterInfos = new Dictionary<string, CharacterInfo>();
    private Dictionary<string, EquipmentData> equipmentData = new Dictionary<string, EquipmentData>();
    private Dictionary<string, List<InventoryItem>> inventoryData = new Dictionary<string, List<InventoryItem>>();
    private Dictionary<string, LocationData> locationData = new Dictionary<string, LocationData>();
    
    // Currently selected character
    public string SelectedCharacterId { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // Request methods
    public void RequestAllCharacterData(string userId)
    {
        if (NetworkClient.connection == null || !NetworkClient.connection.isReady)
        {
            Debug.LogError("NetworkClient not ready.");
            return;
        }

        // Clear stored data
        ClearAllData();

        NetworkClient.Send(new CharacterPreviewRequestMessage
        {
            userId = userId
        });
    }

    public void RequestCharacterData(string userId, string characterId)
    {
        if (NetworkClient.connection == null || !NetworkClient.connection.isReady)
        {
            Debug.LogError("NetworkClient not ready.");
            return;
        }

        NetworkClient.Send(new CharacterDetailRequestMessage
        {
            userId = userId,
            characterId = characterId
        });
    }

    // Clear methods
    public void ClearAllData()
    {
        characterInfos.Clear();
        equipmentData.Clear();
        inventoryData.Clear();
        locationData.Clear();
        
        HasCharacterData = false;
        HasInventoryData = false;
        HasEquipmentData = false;
        HasLocationData = false;
    }
    
    // Data receiver methods (called by NetworkBehavior)
    public void ReceiveCharacterInfos(List<CharacterInfo> characters)
    {
        characterInfos.Clear();
        foreach (var character in characters)
        {
            characterInfos[character.id] = character;
        }
        HasCharacterData = true;
        OnCharacterDataReceived?.Invoke();
    }
    
    public void ReceiveEquipmentData(string characterId, EquipmentData equipment)
    {
        equipmentData[characterId] = equipment;
        HasEquipmentData = true;
        OnEquipmentDataReceived?.Invoke();
    }
    
    public void ReceiveInventoryData(string characterId, List<InventoryItem> items)
    {
        inventoryData[characterId] = items;
        HasInventoryData = true;
        OnInventoryDataReceived?.Invoke();
    }
    
    public void ReceiveLocationData(string characterId, LocationData location)
    {
        locationData[characterId] = location;
        HasLocationData = true;
        OnLocationDataReceived?.Invoke();
    }
    
    // Data access methods
    public void SelectCharacter(string characterId)
    {
        SelectedCharacterId = characterId;
        OnCharacterSelected?.Invoke(characterId);
    }
    
    public List<string> GetAllCharacterIds()
    {
        return new List<string>(characterInfos.Keys);
    }
    
    public CharacterInfo GetCharacterInfo(string characterId)
    {
        return characterInfos.TryGetValue(characterId, out var info) ? info : null;
    }
    
    public EquipmentData GetEquipment(string characterId)
    {
        return equipmentData.TryGetValue(characterId, out var data) ? data : null;
    }
    
    public List<InventoryItem> GetInventory(string characterId)
    {
        return inventoryData.TryGetValue(characterId, out var items) ? items : new List<InventoryItem>();
    }
    
    public LocationData GetLocation(string characterId)
    {
        return locationData.TryGetValue(characterId, out var location) ? location : null;
    }
    public void ReceiveCharacterPreviewData(CharacterInfo[] characters, CharacterEquipmentPair[] equipmentPairs)
    {
        // This happens first - process character infos
        ReceiveCharacterInfos(new List<CharacterInfo>(characters));
        
        // Track equipment data completeness
        foreach (var pair in equipmentPairs)
        {
            // Mark this character as having complete data
            characterDataCompleteness[pair.characterId] = true;
            ReceiveEquipmentData(pair.characterId, pair.equipment);
        }
    }
    // In ClientPlayerDataManager.cs
    public event Action<string> OnCharacterSelected;
}