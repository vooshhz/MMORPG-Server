using UnityEngine;
using Mirror;
public class Portal : MonoBehaviour
{
    [SerializeField] private SceneName targetScene;
    [SerializeField] private float destinationX;
    [SerializeField] private float destinationY;
    
    private bool isActive = true;
    private float cooldownTime = 3f;
    private float lastUseTime = -3f;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (!player || !player.isLocalPlayer || !isActive || Time.time - lastUseTime < cooldownTime)
            return;
            
        lastUseTime = Time.time;
        UsePortal(collision.gameObject);
    }
    
    private void UsePortal(GameObject player)
    {
        Debug.Log($"Portal used: Transitioning to {targetScene}");
        
        NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
        if (!identity || !identity.isLocalPlayer)
            return;
        
        PlayerNetworkController playerController = player.GetComponent<PlayerNetworkController>();
        if (!playerController)
            return;
            
        string characterId = playerController.characterId;
        if (string.IsNullOrEmpty(characterId))
            return;
        
        Vector3 destination = new Vector3(destinationX, destinationY, 0f);
        
        // Simple scene change request without saving position
        NetworkSceneManager.Instance.RequestSceneChange(targetScene);
    }
}