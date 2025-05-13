using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private GameScene targetScene;
    [SerializeField] private Vector2 spawnPosition;

    // Track which players have entered with timestamps
    private Dictionary<string, float> processedCharacters = new Dictionary<string, float>();
    private const float REUSE_COOLDOWN = 2.0f; // seconds before portal can be reused
    
    private void Update()
    {
        // Clean up expired entries
        float currentTime = Time.time;
        List<string> expiredEntries = new List<string>();
        
        foreach (var entry in processedCharacters)
        {
            if (currentTime - entry.Value >= REUSE_COOLDOWN)
                expiredEntries.Add(entry.Key);
        }
        
        foreach (var key in expiredEntries)
            processedCharacters.Remove(key);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkServer.active) return;
        
        PlayerNetworkController player = other.GetComponent<PlayerNetworkController>();
        if (player == null) return;
        
        string characterId = player.characterId;
        float currentTime = Time.time;
        
        // Check if character is on cooldown
        if (processedCharacters.TryGetValue(characterId, out float lastUseTime))
        {
            if (currentTime - lastUseTime < REUSE_COOLDOWN)
                return; // Still on cooldown
        }
        
        // Update use time
        processedCharacters[characterId] = currentTime;
        
        // Handle portal transition
        ServerPortalManager.Instance.HandlePortalTransition(
            player.connectionToClient, characterId, targetScene, spawnPosition);
    }
}
