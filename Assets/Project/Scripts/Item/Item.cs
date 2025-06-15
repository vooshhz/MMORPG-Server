using UnityEngine;
using Mirror;

public class Item : NetworkBehaviour
{
    [ItemCodeDescription]
    [SerializeField][SyncVar] private int _itemCode;
    private SpriteRenderer spriteRenderer;
    

     public int ItemCode { get { return _itemCode; } private set { _itemCode = value; } }

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (isServer && ItemCode != 0)
            {
                Init(ItemCode);
            }
    }

   [Server]
    public void Init(int itemCodeParam)
    {   
        if (itemCodeParam != 0)
        {
            ItemCode = itemCodeParam;
            
            ItemDetails itemDetails = ServerInventoryManager.Instance.GetItemDetails(ItemCode);
            
            if (itemDetails != null)
            {
                // Set sprite on server
                spriteRenderer.sprite = itemDetails.itemSprite;
                
                // Sync sprite to all clients
                RpcSetSprite(itemDetails.itemCode);
                
                // If item type is reapable then add nudgeable component
                if(itemDetails.itemType == ItemType.Reapable_scenary)
                {
                    gameObject.AddComponent<ItemNudge>();
                }
            }
        }
    }

    [ClientRpc]
    private void RpcSetSprite(int itemCode)
    {
        // On clients, get item details from ClientInventoryManager
        if (ClientInventoryManager.Instance != null)
        {
            ItemDetails itemDetails = ClientInventoryManager.Instance.GetItemDetails(itemCode);
            if (itemDetails != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = itemDetails.itemSprite;
            }
        }
    }
}
