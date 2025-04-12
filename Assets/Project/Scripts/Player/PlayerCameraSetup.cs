using UnityEngine;
using Cinemachine;

public class PlayerCameraSetup : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    private void Start()
    {
        // Subscribe to the character sync completed event
        CharacterSyncManager.OnSyncCompleted += SetupCamera;
    }
    
    private void OnDestroy()
    {
        CharacterSyncManager.OnSyncCompleted -= SetupCamera;
    }
    
    private void SetupCamera()
    {
        // Find the local player object
        PlayerNetworkController[] players = FindObjectsOfType<PlayerNetworkController>();
        foreach (var player in players)
        {
            if (player.isLocalPlayer)
            {
                // Set the virtual camera to follow the player
                virtualCamera.Follow = player.transform;
                Debug.Log("Camera attached to local player");
                break;
            }
        }
    }
}