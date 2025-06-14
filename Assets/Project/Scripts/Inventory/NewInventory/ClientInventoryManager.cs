using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ClientInventoryManager : MonoBehaviour
{
    public static ClientInventoryManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private SO_ItemList itemList;  
    
    private UIInventoryBar inventoryBar;
    private Dictionary<int, ItemDetails> itemDetailsDictionary;
    private List<InventoryItem> playerInventory = new List<InventoryItem>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateItemDetailsDictionary();
    }
    
    // Remove Start() method - no more searching needed
    
    private void CreateItemDetailsDictionary()
    {
        itemDetailsDictionary = new Dictionary<int, ItemDetails>();
        
        if (itemList != null)
        {
            foreach (ItemDetails itemDetails in itemList.itemDetails)
            {
                itemDetailsDictionary.Add(itemDetails.itemCode, itemDetails);
            }
        }
    }
    
    // New registration method
        public void RegisterInventoryBar(UIInventoryBar bar)
    {
    inventoryBar = bar;
    Debug.Log("UIInventoryBar registered with ClientInventoryManager");
    
    // Debug: Check if we have data
    if (ClientPlayerDataManager.Instance != null)
    {
        string selectedCharacterId = ClientPlayerDataManager.Instance.SelectedCharacterId;
        Debug.Log($"Selected character: {selectedCharacterId}");
        
        if (!string.IsNullOrEmpty(selectedCharacterId))
        {
            var inventoryData = ClientPlayerDataManager.Instance.GetInventory(selectedCharacterId);
            Debug.Log($"Inventory data count: {inventoryData?.Count ?? 0}");
            
            if (inventoryData != null && inventoryData.Count > 0)
            {
                // Convert from ClientPlayerDataManager.InventoryItem to your InventoryItem
                List<InventoryItem> convertedInventory = new List<InventoryItem>();
                foreach (var item in inventoryData)
                {
                    convertedInventory.Add(new InventoryItem 
                    { 
                        itemCode = item.itemCode, 
                        itemQuantity = item.itemQuantity
                    });
                }
                
                Debug.Log($"Converting {convertedInventory.Count} items for UI update");
                ReceiveInventoryData(convertedInventory);
            }
            else
            {
                Debug.Log("No inventory data available - UI will remain empty");
            }
        }
        else
        {
            Debug.Log("No character selected - cannot load inventory");
        }
    }
    else
    {
        Debug.Log("ClientPlayerDataManager not available");
    }
    }
    
    // New unregistration method
    public void UnregisterInventoryBar()
    {
        inventoryBar = null;
        Debug.Log("UIInventoryBar unregistered from ClientInventoryManager");
    }
    
    public void ReceiveInventoryData(List<InventoryItem> inventoryData)
    {
        playerInventory = inventoryData;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (inventoryBar != null)
        {
            inventoryBar.InventoryUpdated(InventoryLocation.player, playerInventory);
        }
    }
    
    public ItemDetails GetItemDetails(int itemCode)
    {
        ItemDetails itemDetails;
        
        if (itemDetailsDictionary.TryGetValue(itemCode, out itemDetails))
        {
            return itemDetails;
        }
        
        return null;
    }
}