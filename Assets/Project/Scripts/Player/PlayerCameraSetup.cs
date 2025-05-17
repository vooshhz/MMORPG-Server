using UnityEngine;
using Mirror;
using Cinemachine;
using System.Collections;

public class PlayerCameraSetup : NetworkBehaviour
{
    private CinemachineVirtualCamera vcam; // Reference to Cinemachine camera

    // Called when this player becomes the local player
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer(); // Call parent method
        StartCoroutine(AssignCameraAfterDelay()); // Start camera setup process
    }

    // Assigns camera with delay to ensure scene is ready
    private IEnumerator AssignCameraAfterDelay()
    {
        yield return null; // Wait one frame for scene initialization

        if (vcam == null)
        {
            vcam = FindObjectOfType<CinemachineVirtualCamera>(); // Find camera in scene
        }

        if (vcam != null)
        {
            vcam.Follow = transform; // Make camera follow this player
        }
        else
        {
            Debug.LogWarning("Virtual Camera not found in scene."); // Log warning if camera not found
        }
    }
}