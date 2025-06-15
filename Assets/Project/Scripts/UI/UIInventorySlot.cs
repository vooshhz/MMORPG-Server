using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections;

public class UIInventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Canvas parentCanvas;
    private Transform parentItem;
    public Image inventorySlotHighlight;
    public Image inventorySlotImage;
    public TextMeshProUGUI textMeshProUGUI;
    private Transform playerTransform;

    // Track if this slot is currently selected
    private bool isSelected = false;

    // Reference to the currently selected slot (static so only one slot can be selected at a time)
    private GameObject selectedItemVisual;

    private static UIInventorySlot currentlySelectedSlot = null;

    [SerializeField] private UIInventoryBar inventoryBar = null;
    [HideInInspector] public ItemDetails itemDetails;
    [SerializeField] private GameObject itemPrefab = null;
    [HideInInspector] public int itemQuantity;
    [SerializeField] private GameObject inventoryTextBoxPrefab = null;
    [SerializeField] private int slotNumber = 0;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }
    private void Start()
    {
        StartCoroutine(TryFindParentItem());
        StartCoroutine(FindLocalPlayer());
    }

    private void Update()
    {
        // If this slot is selected and player clicks on the game world (not UI)
        if (isSelected && Input.GetMouseButtonDown(0))
        {
            // Check if the click was on a UI element
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                RequestItemDrop();
                UnselectItem();
            }
        }

        // Move the selected item visual to follow the mouse
        if (isSelected && selectedItemVisual != null)
        {
            selectedItemVisual.transform.position = Input.mousePosition;
        }
    }

    private IEnumerator TryFindParentItem(float timeout = 5f)
    {
        float timer = 0f;

        while (parentItem == null && timer < timeout)
        {
            GameObject found = GameObject.FindGameObjectWithTag(Tags.ItemsParentTransform);

            if (found != null)
            {
                parentItem = found.transform;
                Debug.Log("✅ Found ItemsParentTransform.");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (parentItem == null)
        {
            Debug.LogWarning("⚠️ Failed to find ItemsParentTransform within timeout.");
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // If there's already a selected slot and it's not this one, perform swap
        if (currentlySelectedSlot != null && currentlySelectedSlot != this)
        {
            // Get the slot numbers
            int fromSlot = currentlySelectedSlot.slotNumber;
            int toSlot = this.slotNumber;

            // Perform the swap in InventoryManager
            InventoryManager.Instance.SwapInventoryItems(InventoryLocation.player, fromSlot, toSlot);

            //Destroy inventory text box
            DestroyInventoryTextBox();

            // Unselect the current slot
            currentlySelectedSlot.UnselectItem();

            return;
        }
        // Toggle selected state
        if (!isSelected)
        {
            SelectItem();
        }
        else
        {
            UnselectItem();
        }
    }

    private void SelectItem()
    {
        if (itemDetails == null) return;

        // Set as currently selected
        isSelected = true;
        currentlySelectedSlot = this;

        // Highlight the slot
        inventorySlotHighlight.color = new Color(1f, 1f, 1f, 1f);

        // Create visual representation
        selectedItemVisual = Instantiate(inventoryBar.inventoryBarDraggedItem, inventoryBar.transform);

        // Set the visual's image
        Image selectedItemImage = selectedItemVisual.GetComponentInChildren<Image>();
        selectedItemImage.sprite = inventorySlotImage.sprite;

        // Set initial position to mouse
        selectedItemVisual.transform.position = Input.mousePosition;
    }

    private void UnselectItem()
    {
        isSelected = false;

        if (currentlySelectedSlot == this)
        {
            currentlySelectedSlot = null;
        }

        // Remove highlight
        inventorySlotHighlight.color = new Color(1f, 1f, 1f, 0f);

        // Destroy the visual
        if (selectedItemVisual != null)
        {
            Destroy(selectedItemVisual);
            selectedItemVisual = null;
        }
    }

    private IEnumerator FindLocalPlayer()
    {
        while (playerTransform == null)
        {
            if (NetworkClient.localPlayer != null)
            {
                playerTransform = NetworkClient.localPlayer.transform;
                Debug.Log("Local player transform found: " + playerTransform.position);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // private void DropSelectedItemAtPlayerPosition()
    // {
    //     if (GridPropertiesManager.Instance == null)
    //     {
    //         Debug.LogError("GridPropertiesManager.Instance is NULL");
    //         return;
    //     }

    //     if (GridPropertiesManager.Instance.grid == null)
    //     {
    //         Debug.LogError("Grid is NULL on GridPropertiesManager");
    //         return;
    //     }
    //     if (itemDetails == null || !itemDetails.canBeDropped) return;

    //     // Check if player transform exists
    //     if (playerTransform == null)
    //     {
    //         Debug.LogError("Player transform reference is missing!");
    //         return;
    //     }

    //     if (itemPrefab == null)
    //     {
    //         Debug.LogError("Item prefab is not assigned!");
    //         return;
    //     }

    //     // Capture the player's current position once at the beginning
    //     Vector3 playerPositionAtTimeOfDrop = playerTransform.position;

    //     // Calculate start and end positions
    //     Vector3 dropStartPosition = playerPositionAtTimeOfDrop;
    //     Vector3 dropEndPosition = playerPositionAtTimeOfDrop + new Vector3(0, 1.8f, 0);

    //     // If can drop item here
    //     Vector3Int gridPosition = GridPropertiesManager.Instance.grid.WorldToCell(dropEndPosition);
    //     GridPropertyDetails gridPropertyDetails = GridPropertiesManager.Instance.GetGridPropertyDetails(gridPosition.x, gridPosition.y);

    //     if(gridPropertyDetails !=null && gridPropertyDetails.canDropItem)
    //         {
    //             // Create the item WITHOUT a parent initially
    //             GameObject newItem = Instantiate(itemPrefab, dropStartPosition, Quaternion.identity);

    //             // Disable the BoxCollider2D during animation
    //             BoxCollider2D boxCollider = newItem.GetComponent<BoxCollider2D>();
    //             if (boxCollider != null)
    //             {
    //                 boxCollider.enabled = false;
    //             }

    //             // Set up the item using InitAfterDrop instead of Init
    //             Item itemComponent = newItem.GetComponent<Item>();
    //             if (itemComponent != null)
    //             {
    //                 itemComponent.InitAfterDrop(itemDetails.itemCode);

    //                 // Use InventoryManager to remove item from inventory
    //                 InventoryManager.Instance.RemoveItem(InventoryLocation.player, itemDetails.itemCode);
    //             }

    //             // Prevent any automatic physics before our animation
    //             Rigidbody2D rb2d = newItem.GetComponent<Rigidbody2D>();
    //             if (rb2d != null)
    //             {
    //                 rb2d.isKinematic = true;
    //                 rb2d.velocity = Vector2.zero;
    //             }

    //             // Now set parent after initial setup
    //             if (parentItem != null)
    //             {
    //                 newItem.transform.SetParent(parentItem, true); // worldPositionStays = true
    //             }

    //             // Start the up-down animation with both captured positions
    //             StartCoroutine(AnimateItemUpDown(newItem, dropStartPosition, dropEndPosition));
    //         }

    // }

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemQuantity != 0)
        {
            inventoryBar.inventoryTextBoxGameobject = Instantiate(inventoryTextBoxPrefab, transform.position, Quaternion.identity);
            inventoryBar.inventoryTextBoxGameobject.transform.SetParent(parentCanvas.transform, false);

            UIInventoryTextBox inventoryTextBox = inventoryBar.inventoryTextBoxGameobject.GetComponent<UIInventoryTextBox>();

            string itemTypeDescription = ClientInventoryManager.Instance.GetItemTypeDescription(itemDetails.itemType);

            inventoryTextBox.SetTextboxText(itemDetails.itemDescription, itemTypeDescription, "", itemDetails.itemLongDescription, "", "");

            inventoryBar.inventoryTextBoxGameobject.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            inventoryBar.inventoryTextBoxGameobject.transform.position = new Vector3(transform.position.x, transform.position.y + 50f, transform.position.z);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DestroyInventoryTextBox();
    }

    public void DestroyInventoryTextBox()
    {
        if (inventoryBar.inventoryTextBoxGameobject != null)
        {
            Destroy(inventoryBar.inventoryTextBoxGameobject);
        }
    }
    
    private void RequestItemDrop()
    {
        if (itemDetails == null || !itemDetails.canBeDropped) return;
        
        PlayerNetworkController playerController = FindLocalPlayerController();
        if (playerController != null)
        {
            playerController.CmdDropItem(itemDetails.itemCode, slotNumber);
        }
        else
        {
            Debug.LogError("Could not find PlayerNetworkController for item drop");
        }
    }

    private PlayerNetworkController FindLocalPlayerController()
    {
        if (NetworkClient.localPlayer != null)
        {
            return NetworkClient.localPlayer.GetComponent<PlayerNetworkController>();
        }
        return null;
    }
}
