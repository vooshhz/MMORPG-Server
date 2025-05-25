using System.Collections.Generic;
using UnityEngine;


public class InventoryManager : MonoBehaviour // Need to check this part and figure out singleton without inherting
{
    public static bool IsInventorManagerReady { get; private set; }
    public UIInventoryBar inventoryBar;
    public static InventoryManager Instance { get; private set; }
    private Dictionary<int, ItemDetails> itemDetailsDictionary;
    public List<InventoryItem>[] inventoryLists;
    [HideInInspector] public int[] inventoryListCapacityIntArray; // the index of the array is the inventory list (from the InventoryLocation enum), 
    // and the value is the capacity of that inventory list

    [SerializeField] private SO_ItemList itemList = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Create item details dictionary
        CreateItemDetailsDictionary();

        // Create inventory lists
        CreateInventoryLists();
    }

    private void CreateInventoryLists()
    {
        inventoryLists = new List<InventoryItem>[(int)InventoryLocation.count];

        for (int i = 0; i < (int)InventoryLocation.count; i++)
        {
            inventoryLists[i] = new List<InventoryItem>();
        }

        // initialize inventory list capacity array
        inventoryListCapacityIntArray = new int[(int)InventoryLocation.count];

        //initialise player inventory list capacity
        inventoryListCapacityIntArray[(int)InventoryLocation.player] = Settings.playerInitialInventoryCapacity;
    }
    public void AddItem(InventoryLocation inventoryLocation, Item item, GameObject gameObjectToDelete)
    {
        AddItem(inventoryLocation, item);

        Destroy(gameObjectToDelete);
    }

    public void AddItem(InventoryLocation inventoryLocation, Item item)
    {
        int itemCode = item.ItemCode;

        List<InventoryItem> inventoryList = inventoryLists[(int)inventoryLocation];

        // Check if inventory already contains the item
        int itemPosition = FindItemInInventory(inventoryLocation, itemCode);

        if (itemPosition != -1)
        {
            AddItemAtPosition(inventoryList, itemCode, itemPosition);
        }
        else
        {
            AddItemAtPosition(inventoryList, itemCode);
        }

        inventoryBar.InventoryUpdated(inventoryLocation, inventoryList);
    }
    public void RemoveItem(InventoryLocation inventoryLocation, int itemCode)
    {
        List<InventoryItem> inventoryList = inventoryLists[(int)inventoryLocation];

        // Check if inventory already contains the item
        int itemPosition = FindItemInInventory(inventoryLocation, itemCode);

        if (itemPosition != -1)
        {
            RemoveItemAtPosition(inventoryList, itemCode, itemPosition);
        }
    }
    private void RemoveItemAtPosition(List<InventoryItem> inventoryList, int itemCode, int position)
    {
        InventoryItem inventoryItem = new InventoryItem();

        int quantity = inventoryList[position].itemQuantity - 1;

        if (quantity > 0)
        {
            inventoryItem.itemQuantity = quantity;
            inventoryItem.itemCode = itemCode;
            inventoryList[position] = inventoryItem;
        }
        else
        {
            inventoryList.RemoveAt(position);
        }
    }
    public int FindItemInInventory(InventoryLocation inventoryLocation, int itemCode)
    {
        List<InventoryItem> inventoryList = inventoryLists[(int)inventoryLocation];

        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (inventoryList[i].itemCode == itemCode)
            {
                return i;
            }
        }

        return -1;
    }
    private void AddItemAtPosition(List<InventoryItem> inventoryList, int itemCode)
    {
        InventoryItem inventoryItem = new InventoryItem();

        inventoryItem.itemCode = itemCode;
        inventoryItem.itemQuantity = 1;
        inventoryList.Add(inventoryItem);
    }
    private void AddItemAtPosition(List<InventoryItem> inventoryList, int itemCode, int position)
    {
        // Get existing item
        InventoryItem inventoryItem = inventoryList[position];

        // Update quantity while preserving the correct itemCode
        inventoryItem.itemQuantity = inventoryItem.itemQuantity + 1;

        // Put back in list
        inventoryList[position] = inventoryItem;
    }
    public void SwapInventoryItems(InventoryLocation inventoryLocation, int fromSlot, int toSlot)
    {
        // Get the inventory list for the specified location (player)
        List<InventoryItem> inventoryList = inventoryLists[(int)inventoryLocation];

        // Check if both slots are within valid range
        if (fromSlot < inventoryList.Count && (toSlot < inventoryList.Count || toSlot == inventoryList.Count))
        {
            // Case 1: Moving to an empty slot (at the end of the list)
            if (toSlot == inventoryList.Count)
            {
                // Create new item at destination
                InventoryItem item = inventoryList[fromSlot];
                inventoryList.Add(item);

                // Remove from original position
                inventoryList.RemoveAt(fromSlot);
            }
            // Case 2: Swapping two existing items
            else
            {
                // Store items
                InventoryItem fromItem = inventoryList[fromSlot];
                InventoryItem toItem = inventoryList[toSlot];

                // Swap the items
                inventoryList[fromSlot] = toItem;
                inventoryList[toSlot] = fromItem;
            }

            // Update UI
            inventoryBar.InventoryUpdated(inventoryLocation, inventoryList);

            // Persist changes to Firebase
            // SaveInventoryToFirebase();
        }
    }

    private void CreateItemDetailsDictionary()
    {
        itemDetailsDictionary = new Dictionary<int, ItemDetails>();

        foreach (ItemDetails itemDetails in itemList.itemDetails)
        {
            itemDetailsDictionary.Add(itemDetails.itemCode, itemDetails);
        }
    }

    public ItemDetails GetItemDetails(int itemCode)
    {
        ItemDetails itemDetails;

        if (itemDetailsDictionary.TryGetValue(itemCode, out itemDetails))
        {
            return itemDetails;
        }
        else
        {
            return null;
        }

    }

    public string GetItemTypeDescription(ItemType itemType)
    {
        string itemTypeDescription;
        switch (itemType)
        {
            case ItemType.Breaking_tool:
                itemTypeDescription = Settings.BreakingTool;
                break;

            case ItemType.Chopping_tool:
                itemTypeDescription = Settings.ChoppingTool;
                break;

            case ItemType.Hoeing_tool:
                itemTypeDescription = Settings.HoeingTool;
                break;

            case ItemType.Reaping_tool:
                itemTypeDescription = Settings.ReapingTool;
                break;

            case ItemType.Watering_tool:
                itemTypeDescription = Settings.WateringTool;
                break;

            case ItemType.Collecting_tool:
                itemTypeDescription = Settings.CollectingTool;
                break;

            default:
                itemTypeDescription = itemType.ToString();
                break;
        }

        return itemTypeDescription;
    }

    public void InitializeInventoryBar()
    {

        if (inventoryBar == null)
        {
            Debug.LogError("UIInventoryBar not found in the scene!");
        }
        else
        {
            Debug.Log("Inventory bar initialized successfully.");
        }
    }

    public void SetInventoryBar(UIInventoryBar bar)
    {
        inventoryBar = bar;
        Debug.Log("Inventory bar manually set.");
    }

}  
