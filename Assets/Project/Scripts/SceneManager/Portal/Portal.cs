using UnityEngine;
using Mirror;
using System.Collections;
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
    
    // Save player state with the destination position before scene change
    if (NetworkClient.isConnected)
    {
        // Send message to server with the destination position
        NetworkClient.Send(new SavePlayerStateMessage
        {
            characterId = characterId,
            position = destination, // Use destination, not current position
            sceneName = targetScene.ToString()
        });
        
        // Request scene change after saving state
        StartCoroutine(RequestSceneChangeAfterDelay(targetScene));
    }
}

private IEnumerator RequestSceneChangeAfterDelay(SceneName sceneToLoad)
{
    // Small delay to ensure SavePlayerState message is processed first
    yield return new WaitForSeconds(0.1f);
    NetworkSceneManager.Instance.RequestSceneChange(sceneToLoad);
}
}