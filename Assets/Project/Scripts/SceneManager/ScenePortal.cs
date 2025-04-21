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
        
        // Make sure we have a valid characterId
        string characterId = player.characterId;
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = ClientPlayerDataManager.Instance.SelectedCharacterId;
            Debug.Log($"Using characterId from ClientPlayerDataManager: {characterId}");
        }
        
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogError("No valid characterId found for scene transition!");
            return;
        }
        
        // Request scene transition with the valid characterId
        NetworkClient.Send(new SceneChangeRequestMessage
        {
            sceneName = targetScene.ToString(),
            characterId = characterId
        });
        
        // You could add a loading indicator here
    }
}
