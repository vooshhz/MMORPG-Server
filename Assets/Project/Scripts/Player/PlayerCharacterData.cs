using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

public class PlayerCharacterDataComponent : MonoBehaviour
{
    [Header("Sync Info")]
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
}