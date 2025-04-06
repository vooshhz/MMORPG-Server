using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "NewEquipmentData", menuName = "Equipment/Equipment Data")]
public class SO_EquipmentData : ScriptableObject
{
    [System.Serializable]
    public class EquipmentItem
    {
        public enum ItemType
        {
            body,
            torso,
            legs,
            head,
            hair,
        }

        public ItemType itemType;           // The type of the item (body, torso, legs)
        public int itemNumber;              // Unique identifier for the item
        public string itemName;             // The name of the item
        public Sprite[] slicedSpritesArray; // Array of sprites for this item
    }

    [Header("Equipment Items")]
    public EquipmentItem[] equipmentItems; // Array of all equipment items

    // This method is called in the editor whenever values change
    private void OnValidate()
    {
        if (equipmentItems == null || equipmentItems.Length == 0)
            return;

        // Group items by type
        var itemsByType = equipmentItems.GroupBy(item => item.itemType);
        
        foreach (var group in itemsByType)
        {
            // Get prefix based on item type
            int prefix = GetPrefixForItemType(group.Key);
            
            // Get existing items of this type
            var itemsOfType = group.ToArray();
            
            // Sort by existing item numbers to find the highest one
            var existingNumbers = itemsOfType
                .Where(item => item.itemNumber >= prefix * 10000 && item.itemNumber < (prefix + 1) * 10000)
                .Select(item => item.itemNumber)
                .OrderBy(num => num)
                .ToList();
            
            // Assign numbers to items that don't have a valid number yet
            foreach (var item in itemsOfType)
            {
                // Check if this item needs a new number
                bool needsNewNumber = item.itemNumber < prefix * 10000 || 
                                     item.itemNumber >= (prefix + 1) * 10000;
                
                if (needsNewNumber)
                {
                    // Find the next available number
                    int nextNumber = prefix * 10000 + 1; // Start with x0001
                    
                    while (existingNumbers.Contains(nextNumber))
                    {
                        nextNumber++;
                    }
                    
                    // Assign the new number and add it to our tracking list
                    item.itemNumber = nextNumber;
                    existingNumbers.Add(nextNumber);
                }
            }
        }
    }
    
    // Helper method to get the prefix digit based on item type
    private int GetPrefixForItemType(EquipmentItem.ItemType itemType)
    {
        switch (itemType)
        {
            case EquipmentItem.ItemType.body:
                return 1;
            case EquipmentItem.ItemType.head:
                return 2;
            case EquipmentItem.ItemType.hair:
                return 3;
            case EquipmentItem.ItemType.torso:
                return 4;
            case EquipmentItem.ItemType.legs:
                return 5;
            default:
                return 9; // Fallback
        }
    }
}