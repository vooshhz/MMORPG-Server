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
    
    private void Start()
    {
        FindUIInventoryBar();
    }
    
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
    
    private void FindUIInventoryBar()
    {
        GameObject uiBarObject = GameObject.Find("UIInventoryBar");
        if (uiBarObject != null)
        {
            inventoryBar = uiBarObject.GetComponent<UIInventoryBar>();
            Debug.Log("Client Inventory Manager found UIInventoryBar");
        }
        else
        {
            Debug.LogWarning("UIInventoryBar not found, retrying...");
            Invoke(nameof(FindUIInventoryBar), 0.5f);
        }
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