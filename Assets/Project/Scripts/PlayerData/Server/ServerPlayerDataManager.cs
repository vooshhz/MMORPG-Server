using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using Firebase;

public class ServerPlayerDataManager : MonoBehaviour
{
    public static ServerPlayerDataManager Instance { get; private set; }
    
    private DatabaseReference dbReference;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

                try {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("Database reference initialized successfully");
        } catch (Exception ex) {
            Debug.LogError($"Failed to initialize Firebase Database: {ex.Message}");
        }
    }
    
    // Handle request for all character data for a user
    public void HandleAllCharacterDataRequest(NetworkConnectionToClient conn)
    {
        // Get Firebase UID from connection's authentication data
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth");
            return;
        }
        
        StartCoroutine(FetchAllCharacterData(conn, userId));
    }
    
    private IEnumerator FetchAllCharacterData(NetworkConnectionToClient conn, string userId)
    {
        
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters")
            .GetValueAsync();
        
        yield return new WaitUntil(() => characterListTask.IsCompleted);
        
        if (characterListTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}");
            yield break;
        }
        
        DataSnapshot snapshot = characterListTask.Result;
        var characterInfos = new List<ClientPlayerDataManager.CharacterInfo>();
        
        foreach (DataSnapshot characterSnapshot in snapshot.Children)
        {
            string charId = characterSnapshot.Key;
            DataSnapshot infoData = characterSnapshot.Child("info");
            
            var charInfo = new ClientPlayerDataManager.CharacterInfo
            {
                id = charId,
                characterName = infoData.Child("characterName").Value?.ToString(),
                characterClass = infoData.Child("characterClass").Value?.ToString(),
                level = Convert.ToInt32(infoData.Child("level").Value),
                experience = Convert.ToInt32(infoData.Child("experience").Value)
            };
            
            characterInfos.Add(charInfo);
        }
        
        // Send data back to client
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .RpcReceiveCharacterInfos(characterInfos.ToArray());
        }
    }
    
    // Handle request for specific character data
    public void HandleCharacterDataRequest(NetworkConnectionToClient conn, string characterId)
    {
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth");
            return;
        }
        
        StartCoroutine(FetchCharacterEquipment(conn, userId, characterId));
        StartCoroutine(FetchCharacterInventory(conn, userId, characterId));
        StartCoroutine(FetchCharacterLocation(conn, userId, characterId));
    }
    
    private IEnumerator FetchCharacterEquipment(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var equipmentTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("equipment").GetValueAsync();
        
        yield return new WaitUntil(() => equipmentTask.IsCompleted);
        
        if (equipmentTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch equipment data: {equipmentTask.Exception}");
            yield break;
        }
        
        DataSnapshot snapshot = equipmentTask.Result;
        
        int head = Convert.ToInt32(snapshot.Child("head").Value);
        int body = Convert.ToInt32(snapshot.Child("body").Value);
        int hair = Convert.ToInt32(snapshot.Child("hair").Value);
        int torso = Convert.ToInt32(snapshot.Child("torso").Value);
        int legs = Convert.ToInt32(snapshot.Child("legs").Value);
        
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveEquipmentData(conn, characterId, head, body, hair, torso, legs);
        }
    }
    
    private IEnumerator FetchCharacterInventory(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var inventoryTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("inventory").Child("items").GetValueAsync();
        
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch inventory data: {inventoryTask.Exception}");
            yield break;
        }
        
        DataSnapshot snapshot = inventoryTask.Result;
        var items = new List<ClientPlayerDataManager.InventoryItem>();
        
        foreach (DataSnapshot itemData in snapshot.Children)
        {
            var item = new ClientPlayerDataManager.InventoryItem
            {
                itemCode = Convert.ToInt32(itemData.Child("itemCode").Value),
                quantity = Convert.ToInt32(itemData.Child("itemQuantity").Value)
            };
            
            items.Add(item);
        }
        
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveInventoryData(conn, characterId, items.ToArray());
        }
    }
    
    private IEnumerator FetchCharacterLocation(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var locationTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("location").GetValueAsync();
        
        yield return new WaitUntil(() => locationTask.IsCompleted);
        
        if (locationTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch location data: {locationTask.Exception}");
            yield break;
        }
        
        DataSnapshot snapshot = locationTask.Result;
        
        string sceneName = snapshot.Child("sceneName").Value?.ToString();
        float x = Convert.ToSingle(snapshot.Child("x").Value);
        float y = Convert.ToSingle(snapshot.Child("y").Value);
        float z = Convert.ToSingle(snapshot.Child("z").Value);
        
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveLocationData(conn, characterId, sceneName, new Vector3(x, y, z));
        }
    }
    
    // Save data methods
    public void SaveCharacterPosition(string userId, string characterId, Vector3 position, string sceneName)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            ["x"] = position.x,
            ["y"] = position.y,
            ["z"] = position.z,
            ["sceneName"] = sceneName
        };
        
        string path = $"users/{userId}/characters/{characterId}/location";
        dbReference.Child(path).UpdateChildrenAsync(updates);
    }
    
    public void HandleCharacterPreviewRequest(NetworkConnectionToClient conn, string userId)
    {
        StartCoroutine(FetchCharacterPreviewData(conn, userId));
    }

    private IEnumerator FetchCharacterPreviewData(NetworkConnectionToClient conn, string userId)
    {
        Debug.Log($"FetchCharacterPreviewData started with userId: {userId}");
        Debug.Log($"dbReference is null? {dbReference == null}");
        
        if (dbReference == null)
        {
            Debug.LogError("Database reference is null! Firebase Database not initialized");
            
            try {
                Debug.Log("Attempting to initialize Firebase Database again...");
                FirebaseApp app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    Debug.LogError("FirebaseApp.DefaultInstance is null!");
                    yield break;
                }
                
                app.Options.DatabaseUrl = new Uri("https://willowfable3-default-rtdb.firebaseio.com/");
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Successfully re-initialized Firebase Database");
            } catch (Exception ex) {
                Debug.LogError($"Failed to initialize Firebase Database: {ex.Message}");
                yield break;
            }
        }
        
        // Fetch basic character data
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync();
        yield return new WaitUntil(() => characterListTask.IsCompleted);
        
        if (characterListTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}");
            yield break;
        }
        
        DataSnapshot snapshot = characterListTask.Result;
        var characterInfos = new List<ClientPlayerDataManager.CharacterInfo>();
        var equipmentPairs = new List<CharacterEquipmentPair>();
        
        // Process each character
        foreach (DataSnapshot characterSnapshot in snapshot.Children)
        {
            string charId = characterSnapshot.Key;
            
            // Get character info
            DataSnapshot infoData = characterSnapshot.Child("info");
            var charInfo = new ClientPlayerDataManager.CharacterInfo
            {
                id = charId,
                characterName = infoData.Child("characterName").Value?.ToString(),
                characterClass = infoData.Child("characterClass").Value?.ToString(),
                level = Convert.ToInt32(infoData.Child("level").Value),
                experience = Convert.ToInt32(infoData.Child("experience").Value)
            };
            characterInfos.Add(charInfo);
            
            // Get equipment data
            DataSnapshot equipData = characterSnapshot.Child("equipment");
            var equipment = new ClientPlayerDataManager.EquipmentData
            {
                head = Convert.ToInt32(equipData.Child("head").Value),
                body = Convert.ToInt32(equipData.Child("body").Value),
                hair = Convert.ToInt32(equipData.Child("hair").Value),
                torso = Convert.ToInt32(equipData.Child("torso").Value),
                legs = Convert.ToInt32(equipData.Child("legs").Value)
            };
            
            equipmentPairs.Add(new CharacterEquipmentPair
            {
                characterId = charId,
                equipment = equipment
            });
        }
        
        // Send response
        var response = new CharacterPreviewResponseMessage
        {
            characters = characterInfos.ToArray(),
            equipmentData = equipmentPairs.ToArray()
        };
        
        conn.Send(response);
    }
}