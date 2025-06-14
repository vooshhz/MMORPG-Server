using System.Collections.Generic;
using UnityEngine;

public class UIInventoryBar : MonoBehaviour
{
    [SerializeField] private Sprite blank16x16sprite = null;
    [SerializeField] private UIInventorySlot[] inventorySlot = null;
    public GameObject inventoryBarDraggedItem;
    [HideInInspector] public GameObject inventoryTextBoxGameobject;
    
    private void Awake() 
    {
        // Remove position switching functionality
    }

    public void InventoryUpdated(InventoryLocation inventoryLocation, List<InventoryItem> inventoryList)
    {
        if (inventoryLocation == InventoryLocation.player)
        {
            ClearInventorySlots();

            if(inventorySlot.Length > 0 && inventoryList.Count > 0)
            {
                // loop through inventory slots and update with corresponding inventory list item
                for (int i = 0; i < inventorySlot.Length; i++)
                {
                    if(i < inventoryList.Count)
                    {
                        int itemCode = inventoryList[i].itemCode;
                        
                        // Get item details from client inventory manager
                        ItemDetails itemDetails = ClientInventoryManager.Instance.GetItemDetails(itemCode);

                        if (itemDetails != null)
                        {
                            // add images and details to inventory item slot
                            inventorySlot[i].inventorySlotImage.sprite = itemDetails.itemSprite;
                            inventorySlot[i].textMeshProUGUI.text = inventoryList[i].itemQuantity.ToString();
                            inventorySlot[i].itemDetails = itemDetails;
                            inventorySlot[i].itemQuantity = inventoryList[i].itemQuantity;
                        }
                    }
                    else
                    {
                        break;
                    }                                        
                }
            }
        }
    }

    private void ClearInventorySlots()
    {
        if (inventorySlot.Length > 0)
        {
            // loop through inventory slots and update with blank sprite
            for(int i = 0; i < inventorySlot.Length; i++)
            {
                inventorySlot[i].inventorySlotImage.sprite = blank16x16sprite;
                inventorySlot[i].textMeshProUGUI.text = "";
                inventorySlot[i].itemDetails = null;
                inventorySlot[i].itemQuantity = 0;
            }
        }
    }
}