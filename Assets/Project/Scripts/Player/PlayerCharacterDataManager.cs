using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerCharacterDataManager : NetworkBehaviour
{
    private PlayerNetworkController networkController;
    private PlayerCharacterData characterData;
    private ClientPlayerDataManager dataManager;
    private bool dataInitialized = false;

    void Awake()
    {
        networkController = GetComponent<PlayerNetworkController>();
        characterData = GetComponent<PlayerCharacterData>();
        
        if (characterData == null)
        {
            characterData = gameObject.AddComponent<PlayerCharacterData>();
        }
    }

    void Start()
    {
        if (isLocalPlayer)
        {
            dataManager = ClientPlayerDataManager.Instance;
            
            if (dataManager != null)
            {
                // Subscribe to data events
                dataManager.OnCharacterDataReceived += OnCharacterDataReceived;
                dataManager.OnEquipmentDataReceived += OnEquipmentDataReceived;
                dataManager.OnLocationDataReceived += OnLocationDataReceived;
                dataManager.OnInventoryDataReceived += OnInventoryDataReceived;
                
                // Set character ID from network controller
                if (!string.IsNullOrEmpty(networkController.characterId))
                {
                    characterData.characterId = networkController.characterId;
                    
                    // Request data from server
                    StartCoroutine(RequestCharacterData());
                }
            }
            else
            {
                Debug.LogError("ClientPlayerDataManager instance not found!");
            }
        }
    }

    private IEnumerator RequestCharacterData()
    {
        // Wait one frame to ensure everything is initialized
        yield return null;
        
        if (dataManager != null && !string.IsNullOrEmpty(characterData.characterId))
        {
            // Request character data from server
            networkController.CmdRequestCharacterData(characterData.characterId);
            
            // Wait for all data to be received (timeout after 5 seconds)
            float timeoutTimer = 0f;
            while (!dataInitialized && timeoutTimer < 5f)
            {
                timeoutTimer += Time.deltaTime;
                yield return null;
            }
            
            if (!dataInitialized)
            {
                Debug.LogWarning("Timed out waiting for character data");
            }
        }
    }

    // Call this method whenever character equipment changes in-game
    public void RefreshEquipmentData()
    {
        if (isLocalPlayer && !string.IsNullOrEmpty(characterData.characterId))
        {
            networkController.CmdRequestCharacterData(characterData.characterId);
        }
    }

    // Call this method to update position in the database
    public void UpdatePositionInDatabase(Vector3 position, string currentScene)
    {
        if (isLocalPlayer && !string.IsNullOrEmpty(characterData.characterId))
        {
            // Command to update position on server
            // This would be implemented in PlayerNetworkController
            //networkController.CmdUpdateCharacterPosition(characterData.characterId, position, currentScene);
        }
    }

    private void OnCharacterDataReceived()
    {
        if (!isLocalPlayer) return;
        
        // Get character info from data manager
        ClientPlayerDataManager.CharacterInfo info = dataManager.GetCharacterInfo(characterData.characterId);
        
        if (info != null)
        {
            // Populate character info
            characterData.characterName = info.characterName;
            characterData.characterClass = info.characterClass;
            characterData.level = info.level;
            // Note: experience is omitted as mentioned
            
            Debug.Log($"Character info loaded: {characterData.characterName}, Level {characterData.level} {characterData.characterClass}");
            CheckInitializationComplete();
        }
    }

    private void OnEquipmentDataReceived()
    {
        if (!isLocalPlayer) return;
        
        // Get equipment data from data manager
        ClientPlayerDataManager.EquipmentData equipment = dataManager.GetEquipment(characterData.characterId);
        
        if (equipment != null)
        {
            // Populate equipment data
            characterData.headItemNumber = equipment.head;
            characterData.bodyItemNumber = equipment.body;
            characterData.hairItemNumber = equipment.hair;
            characterData.torsoItemNumber = equipment.torso;
            characterData.legsItemNumber = equipment.legs;
            
            Debug.Log($"Equipment loaded for {characterData.characterName}");
            
            // Apply equipment to character animator if present
            CharacterAnimator animator = GetComponentInChildren<CharacterAnimator>();
            if (animator != null)
            {
                animator.headItemNumber = equipment.head;
                animator.bodyItemNumber = equipment.body;
                animator.hairItemNumber = equipment.hair;
                animator.torsoItemNumber = equipment.torso;
                animator.legsItemNumber = equipment.legs;
                animator.RefreshCurrentFrame();
            }
            
            CheckInitializationComplete();
        }
    }

    private void OnInventoryDataReceived()
    {
        if (!isLocalPlayer) return;
        
        // Get inventory data from data manager and update the character
        // Implementation depends on your inventory system
        Debug.Log($"Inventory updated for {characterData.characterName}");
    }

    private void OnLocationDataReceived()
    {
        if (!isLocalPlayer) return;
        
        // Get location data from data manager
        ClientPlayerDataManager.LocationData location = dataManager.GetLocation(characterData.characterId);
        
        if (location != null)
        {
            // Populate location data
            characterData.sceneName = location.sceneName;
            characterData.x = location.position.x;
            characterData.y = location.position.y;
            characterData.z = location.position.z;
            
            Debug.Log($"Location loaded for {characterData.characterName}: {characterData.sceneName} ({characterData.x}, {characterData.y}, {characterData.z})");
            CheckInitializationComplete();
        }
    }

    private void CheckInitializationComplete()
    {
        // Check if we have all the necessary data
        bool hasInfo = !string.IsNullOrEmpty(characterData.characterName) && !string.IsNullOrEmpty(characterData.characterClass);
        bool hasEquipment = characterData.bodyItemNumber > 0 && characterData.headItemNumber > 0;
        bool hasLocation = !string.IsNullOrEmpty(characterData.sceneName);
        
        if (hasInfo && hasEquipment && hasLocation)
        {
            dataInitialized = true;
            Debug.Log($"Character data initialization complete for {characterData.characterName}");
        }
    }

    void OnDestroy()
    {
        // Clean up event subscriptions
        if (dataManager != null)
        {
            dataManager.OnCharacterDataReceived -= OnCharacterDataReceived;
            dataManager.OnEquipmentDataReceived -= OnEquipmentDataReceived;
            dataManager.OnLocationDataReceived -= OnLocationDataReceived;
            dataManager.OnInventoryDataReceived -= OnInventoryDataReceived;
        }
    }
}