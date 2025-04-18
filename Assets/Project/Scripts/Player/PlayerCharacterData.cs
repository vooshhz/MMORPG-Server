using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

public class PlayerCharacterData : MonoBehaviour
{
    [Header("Sync Info")]
    [SerializeField] private string lastSyncedTimeString = "Not synced yet";
    public DateTime lastSyncedTime;
    
    [Header("Character Info")]
    public string characterId;
    public string characterName;
    public string characterClass;
    public int level;
    public int experience;
    
    [Header("Equipment")]
    public int headItemNumber;
    public int bodyItemNumber;
    public int hairItemNumber;
    public int torsoItemNumber;
    public int legsItemNumber;
    
    // Inventory item class definition
    [System.Serializable]
    public class InventoryItem
    {
        public int itemCode;
        public int quantity;
    }
    
    [Header("Inventory")]
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    
    [Header("Stats")]
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    public int strength;
    public int intelligence;
    public int dexterity;
    
    [Header("Location")]
    public string currentSceneName;
    public Vector3 position;
    
    [Header("State Tracking")]
    public bool isFullyLoaded;

    // Reference to the character animator
    [Header("Visual References")]
    [SerializeField] private CharacterAnimator characterAnimator;

    private void Awake()
    {
        // Find the CharacterAnimator if not assigned in inspector
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<CharacterAnimator>();
        }
    }

    // Updates the last synced time and string representation
    public void UpdateSyncTime()
    {
        lastSyncedTime = DateTime.Now;
        lastSyncedTimeString = lastSyncedTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Applies equipment data to the CharacterAnimator
    public void ApplyEquipmentToCharacter()
    {
        if (characterAnimator == null)
        {
            // Try to find it if missing
            characterAnimator = GetComponentInChildren<CharacterAnimator>(true);
            
            if (characterAnimator == null)
            {
                Debug.LogError($"CharacterAnimator not found for {characterName}. Cannot apply equipment.");
                return;
            }
        }

        // Apply equipment item numbers to animator
        characterAnimator.headItemNumber = headItemNumber;
        characterAnimator.bodyItemNumber = bodyItemNumber;
        characterAnimator.hairItemNumber = hairItemNumber;
        characterAnimator.torsoItemNumber = torsoItemNumber;
        characterAnimator.legsItemNumber = legsItemNumber;

        // Refresh the character visual - ensure this calls the right method
        characterAnimator.RefreshCurrentFrame();
        
        Debug.Log($"Equipment applied to animator: Head={headItemNumber}, Body={bodyItemNumber}, Hair={hairItemNumber}");
    }

    // Method to apply character info data
    public void ApplyCharacterInfo()
    {
        // Set any visual elements related to character info
        // For example, name tags, level indicators, etc.
        Debug.Log($"Character info applied: {characterName}, Level {level} {characterClass}");
    }

    // Method to apply inventory data
    public void ApplyInventory()
    {
        // Update any inventory UI or relevant components
        Debug.Log($"Inventory applied with {inventoryItems.Count} items");
    }
}