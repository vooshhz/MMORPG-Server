using UnityEngine;
using Mirror;
using Cinemachine;
using System.Collections;

public class PlayerInitializeUI : NetworkBehaviour
{
    private CinemachineVirtualCamera vcam;
    private Camera playerCamera;
    
    private void Awake()
    {
        // Get references to camera components on the player prefab
        vcam = GetComponentInChildren<CinemachineVirtualCamera>();
        playerCamera = GetComponentInChildren<Camera>();
    }
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        // Enable cameras only for local player
        if (playerCamera != null)
            playerCamera.enabled = true;
            
        if (vcam != null)
            vcam.enabled = true;
            
        StartCoroutine(SetupBoundsConfiner());
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Disable cameras for non-local players
        if (!isLocalPlayer)
        {
            if (playerCamera != null)
                playerCamera.enabled = false;
                
            if (vcam != null)
                vcam.enabled = false;
        }
    }
    
    private IEnumerator SetupBoundsConfiner()
    {
        yield return null; // Wait one frame to ensure scene is loaded
        
        // Find the bounds confiner in the current scene
        GameObject boundsConfiner = GameObject.FindGameObjectWithTag("BoundsConfiner");
        
        if (boundsConfiner != null && vcam != null)
        {
            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                var collider = boundsConfiner.GetComponent<PolygonCollider2D>();
                if (collider != null)
                {
                    confiner.m_BoundingShape2D = collider;
                    confiner.enabled = true;
                    confiner.InvalidateCache();
                    yield return null;
                    confiner.InvalidateCache();
                    Debug.Log($"Bounds confiner set for scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                }
                else
                {
                    Debug.LogWarning("BoundsConfiner found but missing PolygonCollider2D component");
                }
            }
        }
        else
        {
            Debug.LogWarning($"No BoundsConfiner found in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
    }
}