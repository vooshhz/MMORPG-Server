using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnedCharacterTracker : MonoBehaviour
{
    public static SpawnedCharacterTracker Instance { get; private set; }

    private class ActiveCharacterData
    {
        public string userId;
        public string characterId;
        public string sceneName;
        public Vector3 position;
        public GameObject playerObject;
    }

    private Dictionary<string, ActiveCharacterData> activeCharactersById = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // prevent duplicate
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool CharacterExists(string characterId)
    {
        return activeCharactersById.ContainsKey(characterId);
    }

    public void RegisterSpawn(string userId, string characterId, string sceneName, Vector3 position, GameObject playerObject)
    {
        activeCharactersById[characterId] = new ActiveCharacterData
        {
            userId = userId,
            characterId = characterId,
            sceneName = sceneName,
            position = position,
            playerObject = playerObject
        };
    }

    public void RemoveCharacter(string characterId)
    {
        if (activeCharactersById.TryGetValue(characterId, out var data))
        {
            if (data.playerObject != null)
                NetworkServer.Destroy(data.playerObject);

            activeCharactersById.Remove(characterId);
        }
    }

    public void UpdateCharacterPosition(string characterId, Vector3 newPos)
    {
        if (activeCharactersById.TryGetValue(characterId, out var data))
        {
            data.position = newPos;
        }
    }

    public string GetCharacterScene(string characterId)
    {
        return activeCharactersById.TryGetValue(characterId, out var data) ? data.sceneName : null;
    }

    public GameObject GetPlayerObject(string characterId)
    {
        return activeCharactersById.TryGetValue(characterId, out var data) ? data.playerObject : null;
    }
}
