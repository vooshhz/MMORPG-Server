using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Inventory Data")]
    [SyncVar]
    public string bagId; // Bag identifier from Firebase

    public int maxSlots = 24; // Maximum inventory slots
    
    [SerializeField]
    private InventoryItem[] inventoryItems; // Array of inventory items
    
    // Property to access inventory items
    public InventoryItem[] InventoryItems 
    { 
        get => inventoryItems; 
        set => inventoryItems = value; 
    }
    
    private void Awake()
    {
        // Initialize inventory array
        inventoryItems = new InventoryItem[maxSlots];
    }
    
    // Called when this object appears on a client
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Initialize inventory if not already done
        if (inventoryItems == null || inventoryItems.Length != maxSlots)
        {
            inventoryItems = new InventoryItem[maxSlots];
        }
    }
}