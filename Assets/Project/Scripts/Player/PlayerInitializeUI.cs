using UnityEngine;
using Mirror;
using Cinemachine;
using System.Collections;

public class PlayerInitializeUI : NetworkBehaviour
{
    private CinemachineVirtualCamera vcam;
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(InitializeLocalPlayerComponents());
    }
    
    private IEnumerator InitializeLocalPlayerComponents()
    {
        yield return null; // wait one frame to ensure components are in scene
        
        // Initialize Virtual Camera
        if (vcam == null)
        {
            vcam = FindObjectOfType<CinemachineVirtualCamera>();
        }
        
        if (vcam != null)
        {
            vcam.Follow = transform;
            
            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.enabled = true;
                confiner.InvalidateCache();
            }
        }
        else
        {
            Debug.LogWarning("Virtual Camera not found in scene.");
        }

        // Initialize Inventory Bar
        if (InventoryManager.Instance != null)
        {
            UIInventoryBar inventoryBar = FindObjectOfType<UIInventoryBar>();
            
            if (inventoryBar != null)
            {
                // Directly set the inventory bar in the InventoryManager
                InventoryManager.Instance.SetInventoryBar(inventoryBar);
                Debug.Log("Inventory bar found and assigned.");
            }
            else
            {
                Debug.LogWarning("Inventory bar not found in scene.");
            }
        }
    }
}