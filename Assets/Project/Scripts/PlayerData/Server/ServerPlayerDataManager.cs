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

    //=============================================
    // INITIALIZATION
    //=============================================
    
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize Firebase database connection
        try {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("Database reference initialized successfully");
        } catch (Exception ex) {
            Debug.LogError($"Failed to initialize Firebase Database: {ex.Message}");
        }
    }
    
    //=============================================
    // CHARACTER DATA RETRIEVAL
    //=============================================
    
    // Handle request for all character data for a user
    public void HandleAllCharacterDataRequest(NetworkConnectionToClient conn)
    {
        // Verify user is authenticated before providing data
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth");
            return;
        }
        
        StartCoroutine(FetchAllCharacterData(conn, userId));
    }
    
    // Fetch basic character data (names, classes, levels) for all characters
    private IEnumerator FetchAllCharacterData(NetworkConnectionToClient conn, string userId)
    {
        // Query Firebase for user's character data
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters")
            .GetValueAsync();
        
        yield return new WaitUntil(() => characterListTask.IsCompleted);
        
        if (characterListTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}");
            yield break;
        }
        
        // Process each character's basic info
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
    
    // Handle request for specific character's detailed data
    public void HandleCharacterDataRequest(NetworkConnectionToClient conn, string characterId)
    {
        // Verify user is authenticated
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth");
            return;
        }
        
        // Fetch the various types of character data
        StartCoroutine(FetchCharacterEquipment(conn, userId, characterId));
        StartCoroutine(FetchCharacterInventory(conn, userId, characterId));
        StartCoroutine(FetchCharacterLocation(conn, userId, characterId));
    }
    
    // Fetch a character's equipment data
    private IEnumerator FetchCharacterEquipment(NetworkConnectionToClient conn, string userId, string characterId)
    {
        // Query Firebase for character's equipment
        var equipmentTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("equipment").GetValueAsync();
        
        yield return new WaitUntil(() => equipmentTask.IsCompleted);
        
        if (equipmentTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch equipment data: {equipmentTask.Exception}");
            yield break;
        }
        
        // Extract equipment values
        DataSnapshot snapshot = equipmentTask.Result;
        
        int head = Convert.ToInt32(snapshot.Child("head").Value);
        int body = Convert.ToInt32(snapshot.Child("body").Value);
        int hair = Convert.ToInt32(snapshot.Child("hair").Value);
        int torso = Convert.ToInt32(snapshot.Child("torso").Value);
        int legs = Convert.ToInt32(snapshot.Child("legs").Value);
        
        // Send data to client
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveEquipmentData(conn, characterId, head, body, hair, torso, legs);
        }
    }
    
    // Fetch a character's inventory data
    private IEnumerator FetchCharacterInventory(NetworkConnectionToClient conn, string userId, string characterId)
    {
        // Query Firebase for character's inventory
        var inventoryTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("inventory").Child("items").GetValueAsync();
        
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch inventory data: {inventoryTask.Exception}");
            yield break;
        }
        
        // Process inventory items
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
        
        // Send data to client
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveInventoryData(conn, characterId, items.ToArray());
        }
    }
    
    // Fetch a character's location data
    private IEnumerator FetchCharacterLocation(NetworkConnectionToClient conn, string userId, string characterId)
    {
        // Query Firebase for character's location
        var locationTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("location").GetValueAsync();
        
        yield return new WaitUntil(() => locationTask.IsCompleted);
        
        if (locationTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch location data: {locationTask.Exception}");
            yield break;
        }
        
        // Extract location data
        DataSnapshot snapshot = locationTask.Result;
        
        string sceneName = snapshot.Child("sceneName").Value?.ToString();
        float x = Convert.ToSingle(snapshot.Child("x").Value);
        float y = Convert.ToSingle(snapshot.Child("y").Value);
        float z = Convert.ToSingle(snapshot.Child("z").Value);
        
        // Send data to client
        if (conn.identity != null)
        {
            conn.identity.GetComponent<PlayerNetworkController>()
                .TargetReceiveLocationData(conn, characterId, sceneName, new Vector3(x, y, z));
        }
    }
    
    //=============================================
    // CHARACTER PREVIEW DATA
    //=============================================
    
    // Handle request for character preview data (for character selection screen)
    public void HandleCharacterPreviewRequest(NetworkConnectionToClient conn, string userId)
    {
        // Ensure database connection is valid
        if (dbReference == null)
        {
            Debug.LogError("Database reference is null! Attempting to reinitialize...");
            try 
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                if (dbReference == null)
                {
                    Debug.LogError("Still couldn't initialize database reference. Disconnecting client.");
                    conn.Disconnect();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception reinitializing database: {ex.Message}");
                conn.Disconnect();
                return;
            }
        }
        
        StartCoroutine(FetchCharacterPreviewData(conn, userId));
    }

    // Fetch combined preview data for character selection screen
    private IEnumerator FetchCharacterPreviewData(NetworkConnectionToClient conn, string userId)
    {
        // Re-initialize database if needed
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
        
        // Fetch all character data in one go
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync();
        yield return new WaitUntil(() => characterListTask.IsCompleted);
        
        if (characterListTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}");
            yield break;
        }
        
        // Prepare data containers
        DataSnapshot snapshot = characterListTask.Result;
        var characterInfos = new List<ClientPlayerDataManager.CharacterInfo>();
        var equipmentPairs = new List<CharacterEquipmentPair>();
        var locationPairs = new List<CharacterLocationPair>();
        
        // Process each character's data
        foreach (DataSnapshot characterSnapshot in snapshot.Children)
        {
            string charId = characterSnapshot.Key;
            
            // Process character info
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
            
            // Process equipment data
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
            
            // Process location data
            DataSnapshot locationData = characterSnapshot.Child("location");
            if (locationData.Exists)
            {
                string sceneName = locationData.Child("sceneName").Value?.ToString();
                float x = 0f, y = 0f, z = 0f;
                
                // Safely extract location values
                if (locationData.Child("x").Exists)
                    x = Convert.ToSingle(locationData.Child("x").Value);
                if (locationData.Child("y").Exists)
                    y = Convert.ToSingle(locationData.Child("y").Value);
                if (locationData.Child("z").Exists)
                    z = Convert.ToSingle(locationData.Child("z").Value);
                
                locationPairs.Add(new CharacterLocationPair
                {
                    characterId = charId,
                    location = new ClientPlayerDataManager.LocationData
                    {
                        sceneName = sceneName,
                        position = new Vector3(x, y, z)
                    }
                });
                
                Debug.Log($"Loaded location data for character {charId}: Scene={sceneName}, Pos=({x},{y},{z})");
            }
            else
            {
                Debug.LogWarning($"No location data found for character {charId}");
            }
        }
        
        // Send combined preview data to client
        var response = new CharacterPreviewResponseMessage
        {
            characters = characterInfos.ToArray(),
            equipmentData = equipmentPairs.ToArray(),
            locationData = locationPairs.ToArray()
        };
        
        conn.Send(response);
        Debug.Log($"Sent character preview response with {characterInfos.Count} characters, {equipmentPairs.Count} equipment sets, and {locationPairs.Count} location data sets");
    }
    
    //=============================================
    // CHARACTER DATA SAVING
    //=============================================
    
    // Save character position to database
    public void SaveCharacterPosition(string userId, string characterId, Vector3 position, string sceneName)
    {
        // Update location data in Firebase
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
    
    //=============================================
    // CHARACTER CREATION
    //=============================================
    
    // Validate character creation parameters
    public bool ValidateCharacterCreation(string className, int bodyItem, int headItem, int hairItem, int torsoItem, int legsItem)
    {
        // Check if class is valid
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
        
        // Check if equipment options are valid
        bool validBody = System.Array.IndexOf(characterCreationOptions.bodyOptions, bodyItem) >= 0;
        bool validHead = System.Array.IndexOf(characterCreationOptions.headOptions, headItem) >= 0;
        bool validHair = System.Array.IndexOf(characterCreationOptions.hairOptions, hairItem) >= 0;
        bool validTorso = System.Array.IndexOf(characterCreationOptions.torsoOptions, torsoItem) >= 0;
        bool validLegs = System.Array.IndexOf(characterCreationOptions.legsOptions, legsItem) >= 0;
        
        return validClass && validBody && validHead && validHair && validTorso && validLegs;
    }

    // Send available character creation options to client
    public void SendCharacterCreationOptions(NetworkConnectionToClient conn)
    {
        // Convert scriptable object data to message format
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

    // Handle character creation request from client
    public void HandleCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequestMessage msg)
    {
        // Verify user is authenticated
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
        
        // Validate character class
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
        
        // Check if name is already taken
        StartCoroutine(CheckNameAvailability(conn, userId, msg));
    }

    // Create character in database after validations pass
    private IEnumerator CreateCharacterInDatabase(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
        // Generate unique character ID
        string characterId = System.Guid.NewGuid().ToString();
        
        // Create character data structure
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
        
        // Save to Firebase
        var dbTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).SetValueAsync(characterData);
        
        yield return new WaitUntil(() => dbTask.IsCompleted);
        
        if (dbTask.IsFaulted)
        {
            Debug.LogError($"Failed to create character: {dbTask.Exception}");
            SendCreateCharacterResponse(conn, false, "Database error");
            yield break;
        }
        
        // Send success response to client
        SendCreateCharacterResponse(conn, true, "Character created successfully", characterId);
    }

    // Send character creation response to client
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

    // Check if character name is already taken
    private IEnumerator CheckNameAvailability(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
        // Query all users to check for name uniqueness
        var nameQuery = dbReference.Child("users").OrderByChild("characters")
            .GetValueAsync();
        
        yield return new WaitUntil(() => nameQuery.IsCompleted);
        
        if (nameQuery.IsFaulted)
        {
            Debug.LogError($"Failed to check name availability: {nameQuery.Exception}");
            SendCreateCharacterResponse(conn, false, "Database error while checking name");
            yield break;
        }
        
        // Check each character for name collision
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

    // Check character limit and send creation options
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

    // Check if user is at character limit and send options
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
        
        // Count user's characters
        DataSnapshot snapshot = characterListTask.Result;
        int characterCount = 0;
        
        foreach (DataSnapshot characterSnapshot in snapshot.Children)
        {
            characterCount++;
        }
        
        // Create message with character limit info
        var msg = new CharacterCreationOptionsMessage
        {
            availableClasses = characterCreationOptions.availableClasses.Select(c => c.className).ToArray(),
            bodyOptions = characterCreationOptions.bodyOptions,
            headOptions = characterCreationOptions.headOptions,
            hairOptions = characterCreationOptions.hairOptions,
            torsoOptions = characterCreationOptions.torsoOptions,
            legsOptions = characterCreationOptions.legsOptions,
            atCharacterLimit = (characterCount >= 3)  // Max 3 characters per account
        };
        
        // Send to client
        conn.Send(msg);
        Debug.Log($"Sent character creation options to client {conn.connectionId}. At character limit: {msg.atCharacterLimit}");
    }



    // Handle player spawn request
    public void HandlePlayerSpawnRequest(NetworkConnectionToClient conn, string characterId)
    {
        // Verify user is authenticated
        string userId = conn.authenticationData as string;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"Connection {conn.connectionId} requested spawn without valid auth");
            return;
        }
        
        StartCoroutine(FetchCharacterSceneAndSpawn(conn, userId, characterId));
    }

    // Coroutine to fetch character scene data and initiate spawn
    private IEnumerator FetchCharacterSceneAndSpawn(NetworkConnectionToClient conn, string userId, string characterId)
    {
        // Create a task to fetch character location data
        var locationTask = dbReference.Child("users").Child(userId)
            .Child("characters").Child(characterId).Child("location").GetValueAsync();
        
        // Wait for the task to complete
        yield return new WaitUntil(() => locationTask.IsCompleted);
        
        if (locationTask.IsFaulted)
        {
            Debug.LogError($"Failed to fetch location data: {locationTask.Exception}");
            // Use default location as fallback
            SendGameSceneTransitionResponse(conn, true, GameScene.Farm_Scene.ToString(), Vector3.zero);
            yield break;
        }
        
        // Extract location data from Firebase response
        var snapshot = locationTask.Result;
        string sceneName = snapshot.Child("sceneName").Value?.ToString() ?? GameScene.Farm_Scene.ToString();
        float x = Convert.ToSingle(snapshot.Child("x").Value ?? 0f);
        float y = Convert.ToSingle(snapshot.Child("y").Value ?? 0f);
        float z = Convert.ToSingle(snapshot.Child("z").Value ?? 0f);
        
        Vector3 spawnPosition = new Vector3(x, y, z);
        
        Debug.Log($"Character {characterId} location data: Scene={sceneName}, Position={spawnPosition}");
        
        // Send response to client with scene to load
        SendGameSceneTransitionResponse(conn, true, sceneName, spawnPosition);
    }

    // Send game scene transition response to client
    private void SendGameSceneTransitionResponse(NetworkConnectionToClient conn, bool approved, string sceneName, Vector3 position, string message = "")
    {
        conn.Send(new GameSceneTransitionResponseMessage
        {
            approved = approved,
            sceneName = sceneName,
            spawnPosition = position,
            message = message
        });
    }

    // Handle game scene transition request
    public void HandleGameSceneTransitionRequest(NetworkConnectionToClient conn, string characterId, string targetScene)
    {
        // Verify the requested scene is valid
        if (Enum.TryParse<GameScene>(targetScene, out GameScene targetGameScene))
        {
            // You could add more validation here as needed
            
            // For now, approve the transition
            SendGameSceneTransitionResponse(conn, true, targetGameScene.ToString(), Vector3.zero);
        }
        else
        {
            // Invalid scene name
            SendGameSceneTransitionResponse(conn, false, targetScene, Vector3.zero, "Invalid scene name");
        }
    }

}