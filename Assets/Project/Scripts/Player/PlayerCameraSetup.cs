using UnityEngine;
using Cinemachine;

public class PlayerCameraSetup : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    private void Start()
    {
        // Find the local player object (the one with isLocalPlayer = true)
        PlayerNetworkController localPlayer = FindObjectOfType<PlayerNetworkController>();
        
        if (localPlayer != null && localPlayer.isLocalPlayer)
        {
            // Set the virtual camera to follow the player
            virtualCamera.Follow = localPlayer.transform;
        }
    }
}