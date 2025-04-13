using UnityEngine;
using Cinemachine;
using Mirror;

// Attach this to your player prefab
public class PlayerCameraSetup : NetworkBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    
    // This runs only on the local player when it spawns
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        Debug.Log("PlayerCameraSetup: Local player spawned, searching for camera");
        SetupCamera();
    }
    
    private void SetupCamera()
    {
        // Find the virtual camera in the scene
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        
        if (virtualCamera == null)
        {
            Debug.LogError("No CinemachineVirtualCamera found in scene!");
            return;
        }
        
        Debug.Log($"Found camera: {virtualCamera.name}, attaching to player: {gameObject.name}");
        
        // Set the camera to follow this player
        virtualCamera.Follow = transform;
        
        // Optionally set LookAt target as well
        virtualCamera.LookAt = transform;
        
        Debug.Log("Camera successfully attached to local player");
    }
    
    // This can be called when the player changes scenes if needed
    public void RefindCamera()
    {
        if (!isLocalPlayer) return;
        SetupCamera();
    }
}