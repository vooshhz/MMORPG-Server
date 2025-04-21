using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] private GameScene targetScene;
    [SerializeField] private Vector2 spawnPosition;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if this is the local player
        PlayerNetworkController player = other.GetComponent<PlayerNetworkController>();
        if (player == null || !player.isLocalPlayer)
            return;
            
        Debug.Log($"Player entered portal to {targetScene} at position {spawnPosition}");
        
        // Request scene transition
        NetworkClient.Send(new SceneChangeRequestMessage
        {
            sceneName = targetScene.ToString(),
            characterId = player.characterId
        });
        
        // You could add a loading indicator here
    }
}
