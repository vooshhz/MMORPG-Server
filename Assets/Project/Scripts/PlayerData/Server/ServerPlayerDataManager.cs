using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using System;
using Firebase;
using System.Linq;

public class ServerPlayerDataManager : MonoBehaviour
{
    public static ServerPlayerDataManager Instance { get; private set; }
    
    private DatabaseReference dbReference;
    [SerializeField] private CharacterCreationOptionsData characterCreationOptions;

    
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

    // Validate a character creation request
    public bool ValidateCharacterCreation(string className, int bodyItem, int headItem, int hairItem, int torsoItem, int legsItem)
    {
        // Validate class
        bool validClass = false;
        foreach (var classOption in characterCreationOptions.availableClasses)
        {
            if (classOption.className == className)
            {
                validClass = true;
                break;
            }
        }
        
        if (!validClass)
            return false;
        
        // Validate equipment options
        bool validBody = System.Array.IndexOf(characterCreationOptions.bodyOptions, bodyItem) >= 0;
        bool validHead = System.Array.IndexOf(characterCreationOptions.headOptions, headItem) >= 0;
        bool validHair = System.Array.IndexOf(characterCreationOptions.hairOptions, hairItem) >= 0;
        bool validTorso = System.Array.IndexOf(characterCreationOptions.torsoOptions, torsoItem) >= 0;
        bool validLegs = System.Array.IndexOf(characterCreationOptions.legsOptions, legsItem) >= 0;
        
        return validClass && validBody && validHead && validHair && validTorso && validLegs;
    }

    public void SendCharacterCreationOptions(NetworkConnectionToClient conn)
    {
        // Convert scriptable object data to message
        var msg = new CharacterCreationOptionsMessage
        {
            availableClasses = characterCreationOptions.availableClasses.Select(c => c.className).ToArray(),
            bodyOptions = characterCreationOptions.bodyOptions,
            headOptions = characterCreationOptions.headOptions,
            hairOptions = characterCreationOptions.hairOptions,
            torsoOptions = characterCreationOptions.torsoOptions,
            legsOptions = characterCreationOptions.legsOptions
        };
        
        // Send to client
        conn.Send(msg);
        Debug.Log($"Sent character creation options to client {conn.connectionId}");
    }

// Handle character creation request
    public void HandleCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequestMessage msg)
    {
        // Get user ID from connection's authentication data
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} tried to create character without valid auth");
            SendCreateCharacterResponse(conn, false, "Authentication error");
            return;
        }
        
        // Validate character name
        if (string.IsNullOrEmpty(msg.characterName) || msg.characterName.Length < 3 || msg.characterName.Length > 16)
        {
            SendCreateCharacterResponse(conn, false, "Invalid character name (must be 3-16 characters)");
            return;
        }


        
        // Validate class
        bool validClass = characterCreationOptions.availableClasses.Any(c => c.className == msg.characterClass);
        if (!validClass)
        {
            SendCreateCharacterResponse(conn, false, "Invalid character class");
            return;
        }
        
        // Validate equipment options
        bool validBody = characterCreationOptions.bodyOptions.Contains(msg.bodyItem);
        bool validHead = characterCreationOptions.headOptions.Contains(msg.headItem);
        bool validHair = characterCreationOptions.hairOptions.Contains(msg.hairItem);
        bool validTorso = characterCreationOptions.torsoOptions.Contains(msg.torsoItem);
        bool validLegs = characterCreationOptions.legsOptions.Contains(msg.legsItem);
        
        if (!validBody || !validHead || !validHair || !validTorso || !validLegs)
        {
            SendCreateCharacterResponse(conn, false, "Invalid customization options");
            return;
        }
        
        StartCoroutine(CheckNameAvailability(conn, userId, msg));
    }

    private IEnumerator CreateCharacterInDatabase(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
        // Generate a unique character ID
        string characterId = System.Guid.NewGuid().ToString();
        
        // Create the character data structure
        Dictionary<string, object> characterData = new Dictionary<string, object>
        {
            ["info"] = new Dictionary<string, object>
            {
                ["characterName"] = msg.characterName,
                ["characterClass"] = msg.characterClass,
                ["level"] = 1,
                ["experience"] = 0
            },
            ["equipment"] = new Dictionary<string, object>
            {
                ["head"] = msg.headItem,
                ["body"] = msg.bodyItem,
                ["hair"] = msg.hairItem,
                ["torso"] = msg.torsoItem,
                ["legs"] = msg.legsItem
            },
            ["inventory"] = new Dictionary<string, object>
            {
                ["items"] = new Dictionary<string, object>()
            },
            ["location"] = new Dictionary<string, object>
{
            ["sceneName"] = characterCreationOptions.startingSceneName.ToString(),
            ["x"] = 0,
            ["y"] = 0,
            ["z"] = 0
            }
        };
        
        // Add to Firebase database
        var dbTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).SetValueAsync(characterData);
        
        yield return new WaitUntil(() => dbTask.IsCompleted);
        
        if (dbTask.IsFaulted)
        {
            Debug.LogError($"Failed to create character: {dbTask.Exception}");
            SendCreateCharacterResponse(conn, false, "Database error");
            yield break;
        }
        
        // Success! Send response to client
        SendCreateCharacterResponse(conn, true, "Character created successfully", characterId);
    }

    private void SendCreateCharacterResponse(NetworkConnectionToClient conn, bool success, string message, string characterId = null)
    {
        var response = new CreateCharacterResponseMessage
        {
            success = success,
            message = message,
            characterId = characterId
        };
        
        conn.Send(response);
    }

    private IEnumerator CheckNameAvailability(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
    // First check if this name already exists in the database
    var nameQuery = dbReference.Child("users").OrderByChild("characters")
        .GetValueAsync();
    
    yield return new WaitUntil(() => nameQuery.IsCompleted);
    
    if (nameQuery.IsFaulted)
    {
        Debug.LogError($"Failed to check name availability: {nameQuery.Exception}");
        SendCreateCharacterResponse(conn, false, "Database error while checking name");
        yield break;
    }
    
    DataSnapshot snapshot = nameQuery.Result;
    bool nameExists = false;
    
    foreach (DataSnapshot userSnapshot in snapshot.Children)
    {
        DataSnapshot charactersSnapshot = userSnapshot.Child("characters");
        foreach (DataSnapshot characterSnapshot in charactersSnapshot.Children)
        {
            string existingName = characterSnapshot.Child("info/characterName").Value?.ToString();
            if (existingName != null && existingName.Equals(msg.characterName, System.StringComparison.OrdinalIgnoreCase))
            {
                nameExists = true;
                break;
            }
        }
        
        if (nameExists) break;
    }
    
    if (nameExists)
    {
        SendCreateCharacterResponse(conn, false, "This character name is already taken");
        yield break;
    }
    
    // Name is available, proceed with character creation
    StartCoroutine(CreateCharacterInDatabase(conn, userId, msg));
    }

    public void CheckCharacterLimitAndSendOptions(NetworkConnectionToClient conn)
{
    string userId = conn.authenticationData as string;
    if (string.IsNullOrEmpty(userId))
    {
        Debug.LogError($"Connection {conn.connectionId} requested character limit check without valid auth");
        return;
    }
    
    StartCoroutine(CheckCharacterLimitCoroutine(conn, userId));
}

    private IEnumerator CheckCharacterLimitCoroutine(NetworkConnectionToClient conn, string userId)
    {
        // Get character count from database
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync();
        yield return new WaitUntil(() => characterListTask.IsCompleted);
        
        if (characterListTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch character count: {characterListTask.Exception}");
            // Send options anyway, but don't restrict creation
            SendCharacterCreationOptions(conn);
            yield break;
        }
        
        DataSnapshot snapshot = characterListTask.Result;
        int characterCount = 0;
        
        // Count the characters
        foreach (DataSnapshot characterSnapshot in snapshot.Children)
        {
            characterCount++;
        }
        
        // Create and send the message with character limit info
        var msg = new CharacterCreationOptionsMessage
        {
            availableClasses = characterCreationOptions.availableClasses.Select(c => c.className).ToArray(),
            bodyOptions = characterCreationOptions.bodyOptions,
            headOptions = characterCreationOptions.headOptions,
            hairOptions = characterCreationOptions.hairOptions,
            torsoOptions = characterCreationOptions.torsoOptions,
            legsOptions = characterCreationOptions.legsOptions,
            atCharacterLimit = (characterCount >= 3)  // Add this new field
        };
        
        // Send to client
        conn.Send(msg);
        Debug.Log($"Sent character creation options to client {conn.connectionId}. At character limit: {msg.atCharacterLimit}");
        }

    public void GetCharacterLocation(NetworkConnectionToClient conn, string userId, string characterId, Action<ClientPlayerDataManager.LocationData> callback)
    {
        StartCoroutine(FetchCharacterLocationForSpawn(conn, userId, characterId, callback));
    }

    private IEnumerator FetchCharacterLocationForSpawn(NetworkConnectionToClient conn, string userId, string characterId, Action<ClientPlayerDataManager.LocationData> callback)
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
        
        var locationData = new ClientPlayerDataManager.LocationData
        {
            sceneName = sceneName,
            position = new Vector3(x, y, z)
        };
        
        callback(locationData);
    }
}