using UnityEngine;
using Mirror;
using Cinemachine;
using System.Collections;

public class PlayerCameraSetup : NetworkBehaviour
{
    private CinemachineVirtualCamera vcam;
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(AssignCameraAfterDelay());
    }
    
    private IEnumerator AssignCameraAfterDelay()
    {
        yield return null; // wait one frame to ensure camera is in scene
        
        if (vcam == null)
        {
            vcam = FindObjectOfType<CinemachineVirtualCamera>();
        }
        
        if (vcam != null)
        {
            vcam.Follow = transform;
            
            // Make sure confiner is enabled
            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.enabled = true;
                
                // Invalidate the cache to recalculate bounds
                confiner.InvalidateCache();
            }
        }
        else
        {
            Debug.LogWarning("Virtual Camera not found in scene.");
        }
    }
}