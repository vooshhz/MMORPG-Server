using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using System;
using Firebase;
using System.Linq;
using UnityEngine.SceneManagement;


public class ServerPlayerDataManager : MonoBehaviour
{
    /// Singleton and References
    public static ServerPlayerDataManager Instance { get; private set; } // Singleton instance
    private Dictionary<NetworkConnectionToClient, string> connectionCharacterIds = new Dictionary<NetworkConnectionToClient, string>(); // Maps connections to character IDs

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab; // Reference to player prefab for instantiation

    private Dictionary<NetworkConnectionToClient, PlayerSpawnData> SpawnDataByConnection = new Dictionary<NetworkConnectionToClient, PlayerSpawnData>(); // Stores spawn data by connection

    private Dictionary<NetworkConnectionToClient, ActivePlayerInfo> activePlayers = new Dictionary<NetworkConnectionToClient, ActivePlayerInfo>();

    private DatabaseReference dbReference; // Firebase database reference
    [SerializeField] private CharacterCreationOptionsData characterCreationOptions; // Character creation configuration

    [Header("Bag System")]
    [SerializeField] private SO_BagData bagList;

    /// Initialization
    private void Awake()
    {
        if (Instance != null && Instance != this) // Check if singleton exists
        {
            Destroy(gameObject); // Destroy duplicate
            return; // Exit early
        }

        Instance = this; // Set singleton instance
        DontDestroyOnLoad(gameObject); // Keep across scene changes

        try
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference; // Get database reference
            Debug.Log("Database reference initialized successfully"); // Log success
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize Firebase Database: {ex.Message}"); // Log error
        }
    }

    /// Character Data Retrieval
    public void HandleAllCharacterDataRequest(NetworkConnectionToClient conn)
    {
        string userId = conn.authenticationData as string; // Get user ID from auth data
        if (string.IsNullOrEmpty(userId)) // Verify authentication
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth"); // Log error
            return; // Exit if not authenticated
        }

        StartCoroutine(FetchAllCharacterData(conn, userId)); // Start data fetch coroutine
    }

    private IEnumerator FetchAllCharacterData(NetworkConnectionToClient conn, string userId)
    {
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync(); // Query character data

        yield return new WaitUntil(() => characterListTask.IsCompleted); // Wait for query to complete

        if (characterListTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}"); // Log error
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = characterListTask.Result; // Get query result
        var characterInfos = new List<ClientPlayerDataManager.CharacterInfo>(); // Create list for character info

        foreach (DataSnapshot characterSnapshot in snapshot.Children) // Process each character
        {
            string charId = characterSnapshot.Key; // Get character ID
            DataSnapshot infoData = characterSnapshot.Child("info"); // Get character info node

            var charInfo = new ClientPlayerDataManager.CharacterInfo // Create character info object
            {
                id = charId, // Set ID
                characterName = infoData.Child("characterName").Value?.ToString(), // Set name
                characterClass = infoData.Child("characterClass").Value?.ToString(), // Set class
                level = Convert.ToInt32(infoData.Child("level").Value), // Set level
                experience = Convert.ToInt32(infoData.Child("experience").Value) // Set experience
            };

            characterInfos.Add(charInfo); // Add to list
        }

        if (conn.identity != null) // Check if connection has identity
        {
            conn.identity.GetComponent<PlayerNetworkController>().RpcReceiveCharacterInfos(characterInfos.ToArray()); // Send to client
        }
    }

    public void HandleCharacterDataRequest(NetworkConnectionToClient conn, string characterId)
    {
        string userId = conn.authenticationData as string; // Get user ID from auth data
        if (string.IsNullOrEmpty(userId)) // Verify authentication
        {
            Debug.LogError($"Connection {conn.connectionId} requested character data without valid auth"); // Log error
            return; // Exit if not authenticated
        }

        StartCoroutine(FetchCharacterEquipment(conn, userId, characterId)); // Fetch equipment
        StartCoroutine(FetchCharacterInventory(conn, userId, characterId)); // Fetch inventory
        StartCoroutine(FetchCharacterLocation(conn, userId, characterId)); // Fetch location
    }

    private IEnumerator FetchCharacterEquipment(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var equipmentTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("equipment").GetValueAsync(); // Query equipment

        yield return new WaitUntil(() => equipmentTask.IsCompleted); // Wait for query to complete

        if (equipmentTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch equipment data: {equipmentTask.Exception}"); // Log error
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = equipmentTask.Result; // Get query result

        int head = Convert.ToInt32(snapshot.Child("head").Value); // Get head item
        int body = Convert.ToInt32(snapshot.Child("body").Value); // Get body item
        int hair = Convert.ToInt32(snapshot.Child("hair").Value); // Get hair item
        int torso = Convert.ToInt32(snapshot.Child("torso").Value); // Get torso item
        int legs = Convert.ToInt32(snapshot.Child("legs").Value); // Get legs item

        if (conn.identity != null) // Check if connection has identity
        {
            conn.identity.GetComponent<PlayerNetworkController>().TargetReceiveEquipmentData(conn, characterId, head, body, hair, torso, legs); // Send to client
        }
    }

    private IEnumerator FetchCharacterInventory(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var inventoryTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("inventory").Child("items").GetValueAsync(); // Query inventory

        yield return new WaitUntil(() => inventoryTask.IsCompleted); // Wait for query to complete

        if (inventoryTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch inventory data: {inventoryTask.Exception}"); // Log error
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = inventoryTask.Result; // Get query result
        var items = new List<InventoryItem>(); // Create list for items

        foreach (DataSnapshot itemData in snapshot.Children) // Process each item
        {
            var item = new InventoryItem
            {
                itemCode = Convert.ToInt32(itemData.Child("itemCode").Value),
                itemQuantity = Convert.ToInt32(itemData.Child("itemQuantity").Value)  // Note: itemQuantity not quantity
            };

            items.Add(item); // Add to list
        }

        if (conn.identity != null) // Check if connection has identity
        {
            conn.identity.GetComponent<PlayerNetworkController>().TargetReceiveInventoryData(conn, characterId, items.ToArray()); // Send to client
        }
    }

    private IEnumerator FetchCharacterLocation(NetworkConnectionToClient conn, string userId, string characterId)
    {
        var locationTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("location").GetValueAsync(); // Query location

        yield return new WaitUntil(() => locationTask.IsCompleted); // Wait for query to complete

        if (locationTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch location data: {locationTask.Exception}"); // Log error
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = locationTask.Result; // Get query result

        string sceneName = snapshot.Child("sceneName").Value?.ToString(); // Get scene name
        float x = Convert.ToSingle(snapshot.Child("x").Value); // Get X position
        float y = Convert.ToSingle(snapshot.Child("y").Value); // Get Y position
        float z = Convert.ToSingle(snapshot.Child("z").Value); // Get Z position

        if (conn.identity != null) // Check if connection has identity
        {
            conn.identity.GetComponent<PlayerNetworkController>().TargetReceiveLocationData(conn, characterId, sceneName, new Vector3(x, y, z)); // Send to client
        }
    }

    /// Character Preview Data
    public void HandleCharacterPreviewRequest(NetworkConnectionToClient conn, string userId)
    {
        if (dbReference == null) // Check if database reference is null
        {
            Debug.LogError("Database reference is null! Attempting to reinitialize..."); // Log error
            try
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference; // Try to reinitialize
                if (dbReference == null) // Check if still null
                {
                    Debug.LogError("Still couldn't initialize database reference. Disconnecting client."); // Log error
                    conn.Disconnect(); // Disconnect client
                    return; // Exit
                }
            }
            catch (Exception ex) // Handle exceptions
            {
                Debug.LogError($"Exception reinitializing database: {ex.Message}"); // Log error
                conn.Disconnect(); // Disconnect client
                return; // Exit
            }
        }

        StartCoroutine(FetchCharacterPreviewData(conn, userId)); // Start data fetch coroutine
    }

    private IEnumerator FetchCharacterPreviewData(NetworkConnectionToClient conn, string userId)
    {
        if (dbReference == null) // Check if database reference is null
        {
            Debug.LogError("Database reference is null! Firebase Database not initialized"); // Log error

            try
            {
                Debug.Log("Attempting to initialize Firebase Database again..."); // Log attempt
                FirebaseApp app = FirebaseApp.DefaultInstance; // Get Firebase app
                if (app == null) // Check if app is null
                {
                    Debug.LogError("FirebaseApp.DefaultInstance is null!"); // Log error
                    yield break; // Exit coroutine
                }

                app.Options.DatabaseUrl = new Uri("https://willowfable3-default-rtdb.firebaseio.com/"); // Set database URL
                dbReference = FirebaseDatabase.DefaultInstance.RootReference; // Get reference
                Debug.Log("Successfully re-initialized Firebase Database"); // Log success
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Firebase Database: {ex.Message}"); // Log error
                yield break; // Exit coroutine
            }
        }

        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync(); // Query characters
        yield return new WaitUntil(() => characterListTask.IsCompleted); // Wait for query to complete

        if (characterListTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch character data: {characterListTask.Exception}"); // Log error
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = characterListTask.Result; // Get query result
        var characterInfos = new List<ClientPlayerDataManager.CharacterInfo>(); // Create list for character info
        var equipmentPairs = new List<CharacterEquipmentPair>(); // Create list for equipment pairs
        var locationPairs = new List<CharacterLocationPair>(); // Create list for location pairs

        foreach (DataSnapshot characterSnapshot in snapshot.Children) // Process each character
        {
            string charId = characterSnapshot.Key; // Get character ID

            DataSnapshot infoData = characterSnapshot.Child("info"); // Get info node
            var charInfo = new ClientPlayerDataManager.CharacterInfo // Create character info object
            {
                id = charId, // Set ID
                characterName = infoData.Child("characterName").Value?.ToString(), // Set name
                characterClass = infoData.Child("characterClass").Value?.ToString(), // Set class
                level = Convert.ToInt32(infoData.Child("level").Value), // Set level
                experience = Convert.ToInt32(infoData.Child("experience").Value) // Set experience
            };
            characterInfos.Add(charInfo); // Add to list

            DataSnapshot equipData = characterSnapshot.Child("equipment"); // Get equipment node
            var equipment = new ClientPlayerDataManager.EquipmentData // Create equipment data object
            {
                head = Convert.ToInt32(equipData.Child("head").Value), // Set head item
                body = Convert.ToInt32(equipData.Child("body").Value), // Set body item
                hair = Convert.ToInt32(equipData.Child("hair").Value), // Set hair item
                torso = Convert.ToInt32(equipData.Child("torso").Value), // Set torso item
                legs = Convert.ToInt32(equipData.Child("legs").Value) // Set legs item
            };

            equipmentPairs.Add(new CharacterEquipmentPair // Create equipment pair
            {
                characterId = charId, // Set character ID
                equipment = equipment // Set equipment data
            });

            DataSnapshot locationData = characterSnapshot.Child("location"); // Get location node
            if (locationData.Exists) // Check if location exists
            {
                string sceneName = locationData.Child("sceneName").Value?.ToString(); // Get scene name
                float x = 0f, y = 0f, z = 0f; // Initialize position values

                if (locationData.Child("x").Exists) // Check if X exists
                    x = Convert.ToSingle(locationData.Child("x").Value); // Get X position
                if (locationData.Child("y").Exists) // Check if Y exists
                    y = Convert.ToSingle(locationData.Child("y").Value); // Get Y position
                if (locationData.Child("z").Exists) // Check if Z exists
                    z = Convert.ToSingle(locationData.Child("z").Value); // Get Z position

                locationPairs.Add(new CharacterLocationPair // Create location pair
                {
                    characterId = charId, // Set character ID
                    location = new ClientPlayerDataManager.LocationData // Create location data
                    {
                        sceneName = sceneName, // Set scene name
                        position = new Vector3(x, y, z) // Set position
                    }
                });

                Debug.Log($"Loaded location data for character {charId}: Scene={sceneName}, Pos=({x},{y},{z})"); // Log success
            }
            else
            {
                Debug.LogWarning($"No location data found for character {charId}"); // Log warning
            }
        }

        var response = new CharacterPreviewResponseMessage // Create response message
        {
            characters = characterInfos.ToArray(), // Set character info
            equipmentData = equipmentPairs.ToArray(), // Set equipment data
            locationData = locationPairs.ToArray() // Set location data
        };

        conn.Send(response); // Send response to client
        Debug.Log($"Sent character preview response with {characterInfos.Count} characters, {equipmentPairs.Count} equipment sets, and {locationPairs.Count} location data sets"); // Log success
    }

    /// Character Data Saving
    public void SaveCharacterPosition(string userId, string characterId, Vector3 position, string sceneName)
    {
        Dictionary<string, object> updates = new Dictionary<string, object> // Create updates dictionary
        {
            ["x"] = position.x, // Set X position
            ["y"] = position.y, // Set Y position
            ["z"] = position.z, // Set Z position
            ["sceneName"] = sceneName // Set scene name
        };

        string path = $"users/{userId}/characters/{characterId}/location"; // Create database path
        dbReference.Child(path).UpdateChildrenAsync(updates); // Update database
    }

    /// Character Creation
    public bool ValidateCharacterCreation(string className, int bodyItem, int headItem, int hairItem, int torsoItem, int legsItem)
    {
        bool validClass = false; // Initialize class validation flag
        foreach (var classOption in characterCreationOptions.availableClasses) // Check each class
        {
            if (classOption.className == className) // If class matches
            {
                validClass = true; // Set flag
                break; // Exit loop
            }
        }

        if (!validClass) // If class is invalid
            return false; // Return false

        // Check if equipment options are valid
        bool validBody = System.Array.IndexOf(characterCreationOptions.bodyOptions, bodyItem) >= 0; // Check body
        bool validHead = System.Array.IndexOf(characterCreationOptions.headOptions, headItem) >= 0; // Check head
        bool validHair = System.Array.IndexOf(characterCreationOptions.hairOptions, hairItem) >= 0; // Check hair
        bool validTorso = System.Array.IndexOf(characterCreationOptions.torsoOptions, torsoItem) >= 0; // Check torso
        bool validLegs = System.Array.IndexOf(characterCreationOptions.legsOptions, legsItem) >= 0; // Check legs

        return validClass && validBody && validHead && validHair && validTorso && validLegs; // Return combined validation result
    }

    public void SendCharacterCreationOptions(NetworkConnectionToClient conn)
    {
        var msg = new CharacterCreationOptionsMessage // Create message
        {
            availableClasses = characterCreationOptions.availableClasses.Select(c => c.className).ToArray(), // Set classes
            bodyOptions = characterCreationOptions.bodyOptions, // Set body options
            headOptions = characterCreationOptions.headOptions, // Set head options
            hairOptions = characterCreationOptions.hairOptions, // Set hair options
            torsoOptions = characterCreationOptions.torsoOptions, // Set torso options
            legsOptions = characterCreationOptions.legsOptions // Set legs options
        };

        conn.Send(msg); // Send to client
        Debug.Log($"Sent character creation options to client {conn.connectionId}"); // Log success
    }

    public void HandleCreateCharacterRequest(NetworkConnectionToClient conn, CreateCharacterRequestMessage msg)
    {
        string userId = conn.authenticationData as string; // Get user ID from auth data
        if (string.IsNullOrEmpty(userId)) // Verify authentication
        {
            Debug.LogError($"Connection {conn.connectionId} tried to create character without valid auth"); // Log error
            SendCreateCharacterResponse(conn, false, "Authentication error"); // Send error response
            return; // Exit
        }

        if (string.IsNullOrEmpty(msg.characterName) || msg.characterName.Length < 3 || msg.characterName.Length > 16) // Validate name
        {
            SendCreateCharacterResponse(conn, false, "Invalid character name (must be 3-16 characters)"); // Send error response
            return; // Exit
        }

        bool validClass = characterCreationOptions.availableClasses.Any(c => c.className == msg.characterClass); // Validate class
        if (!validClass) // If class is invalid
        {
            SendCreateCharacterResponse(conn, false, "Invalid character class"); // Send error response
            return; // Exit
        }

        // Validate equipment options
        bool validBody = characterCreationOptions.bodyOptions.Contains(msg.bodyItem); // Check body
        bool validHead = characterCreationOptions.headOptions.Contains(msg.headItem); // Check head
        bool validHair = characterCreationOptions.hairOptions.Contains(msg.hairItem); // Check hair
        bool validTorso = characterCreationOptions.torsoOptions.Contains(msg.torsoItem); // Check torso
        bool validLegs = characterCreationOptions.legsOptions.Contains(msg.legsItem); // Check legs

        if (!validBody || !validHead || !validHair || !validTorso || !validLegs) // If any equipment is invalid
        {
            SendCreateCharacterResponse(conn, false, "Invalid customization options"); // Send error response
            return; // Exit
        }

        StartCoroutine(CheckNameAvailability(conn, userId, msg)); // Start name check coroutine
    }

    private IEnumerator CreateCharacterInDatabase(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
        string characterId = System.Guid.NewGuid().ToString(); // Generate unique ID

        // Get bag data to determine slot count
        var bagData = characterCreationOptions.bagData.bags.FirstOrDefault(b => b.bagId == characterCreationOptions.defaultBagId);
        int maxSlots = bagData?.maxSlots ?? 16; // Get max slots from bag

        // Initialize inventory slots
        Dictionary<string, object> slotsData = new Dictionary<string, object>();
        for (int i = 0; i < maxSlots; i++)
        {
            slotsData[i.ToString()] = new Dictionary<string, object>
            {
                ["itemCode"] = 0,
                ["itemQuantity"] = 0
            };
        }

        Dictionary<string, object> characterData = new Dictionary<string, object> // Create character data
        {
            ["info"] = new Dictionary<string, object> // Create info object
            {
                ["characterName"] = msg.characterName, // Set name
                ["characterClass"] = msg.characterClass, // Set class
                ["level"] = 1, // Set level
                ["experience"] = 0 // Set experience
            },
            ["equipment"] = new Dictionary<string, object> // Create equipment object
            {
                ["head"] = msg.headItem, // Set head item
                ["body"] = msg.bodyItem, // Set body item
                ["hair"] = msg.hairItem, // Set hair item
                ["torso"] = msg.torsoItem, // Set torso item
                ["legs"] = msg.legsItem // Set legs item
            },
            ["inventory"] = CreateInventoryStructure(msg.characterClass),

            ["location"] = new Dictionary<string, object> // Create location object
            {
                ["sceneName"] = characterCreationOptions.startingSceneName.ToString(), // Set starting scene
                ["x"] = 0, // Set X position
                ["y"] = 0, // Set Y position
                ["z"] = 0 // Set Z position
            }
        };

        var dbTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).SetValueAsync(characterData); // Save to database

        yield return new WaitUntil(() => dbTask.IsCompleted); // Wait for database operation to complete

        if (dbTask.IsFaulted) // Check for database error
        {
            Debug.LogError($"Failed to create character: {dbTask.Exception}"); // Log error
            SendCreateCharacterResponse(conn, false, "Database error"); // Send error response
            yield break; // Exit coroutine
        }

        SendCreateCharacterResponse(conn, true, "Character created successfully", characterId); // Send success response
    }

    private Dictionary<string, object> CreateInventoryStructure(string characterClass)
    {
        int defaultBagId = characterCreationOptions.defaultBagId;
        var bagInfo = System.Array.Find(characterCreationOptions.bagData.bags, b => b.bagId == defaultBagId);
        int maxSlots = bagInfo?.maxSlots ?? 16;

        // Create items dictionary
        var items = new Dictionary<string, object>();
        for (int i = 0; i < maxSlots; i++)
        {
            items[i.ToString()] = new Dictionary<string, object>
            {
                ["itemCode"] = 0,
                ["itemQuantity"] = 0
            };
        }

        var inventory = new Dictionary<string, object>
        {
            ["bagId"] = defaultBagId.ToString(),
            ["slots"] = maxSlots.ToString(),
            ["items"] = items
        };

        return inventory;
    }
    private void SendCreateCharacterResponse(NetworkConnectionToClient conn, bool success, string message, string characterId = null)
    {
        var response = new CreateCharacterResponseMessage // Create response message
        {
            success = success, // Set success flag
            message = message, // Set message
            characterId = characterId // Set character ID
        };

        conn.Send(response); // Send to client
    }

    private IEnumerator CheckNameAvailability(NetworkConnectionToClient conn, string userId, CreateCharacterRequestMessage msg)
    {
        var nameQuery = dbReference.Child("users").OrderByChild("characters").GetValueAsync(); // Query users

        yield return new WaitUntil(() => nameQuery.IsCompleted); // Wait for query to complete

        if (nameQuery.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to check name availability: {nameQuery.Exception}"); // Log error
            SendCreateCharacterResponse(conn, false, "Database error while checking name"); // Send error response
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = nameQuery.Result; // Get query result
        bool nameExists = false; // Initialize name exists flag

        foreach (DataSnapshot userSnapshot in snapshot.Children) // Process each user
        {
            DataSnapshot charactersSnapshot = userSnapshot.Child("characters"); // Get characters node
            foreach (DataSnapshot characterSnapshot in charactersSnapshot.Children) // Process each character
            {
                string existingName = characterSnapshot.Child("info/characterName").Value?.ToString(); // Get character name
                if (existingName != null && existingName.Equals(msg.characterName, System.StringComparison.OrdinalIgnoreCase)) // If name matches
                {
                    nameExists = true; // Set flag
                    break; // Exit loop
                }
            }

            if (nameExists) break; // Exit loop if name exists
        }

        if (nameExists) // If name exists
        {
            SendCreateCharacterResponse(conn, false, "This character name is already taken"); // Send error response
            yield break; // Exit coroutine
        }

        StartCoroutine(CreateCharacterInDatabase(conn, userId, msg)); // Start character creation coroutine
    }

    public void CheckCharacterLimitAndSendOptions(NetworkConnectionToClient conn)
    {
        string userId = conn.authenticationData as string; // Get user ID from auth data
        if (string.IsNullOrEmpty(userId)) // Verify authentication
        {
            Debug.LogError($"Connection {conn.connectionId} requested character limit check without valid auth"); // Log error
            return; // Exit
        }

        StartCoroutine(CheckCharacterLimitCoroutine(conn, userId)); // Start character limit check coroutine
    }

    private IEnumerator CheckCharacterLimitCoroutine(NetworkConnectionToClient conn, string userId)
    {
        var characterListTask = dbReference.Child("users").Child(userId).Child("characters").GetValueAsync(); // Query characters
        yield return new WaitUntil(() => characterListTask.IsCompleted); // Wait for query to complete

        if (characterListTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch character count: {characterListTask.Exception}"); // Log error
            SendCharacterCreationOptions(conn); // Send options anyway
            yield break; // Exit coroutine
        }

        DataSnapshot snapshot = characterListTask.Result; // Get query result
        int characterCount = 0; // Initialize character count

        foreach (DataSnapshot characterSnapshot in snapshot.Children) // Count characters
        {
            characterCount++; // Increment count
        }

        var msg = new CharacterCreationOptionsMessage // Create message
        {
            availableClasses = characterCreationOptions.availableClasses.Select(c => c.className).ToArray(), // Set classes
            bodyOptions = characterCreationOptions.bodyOptions, // Set body options
            headOptions = characterCreationOptions.headOptions, // Set head options
            hairOptions = characterCreationOptions.hairOptions, // Set hair options
            torsoOptions = characterCreationOptions.torsoOptions, // Set torso options
            legsOptions = characterCreationOptions.legsOptions, // Set legs options
            atCharacterLimit = (characterCount >= 3) // Set character limit flag
        };

        conn.Send(msg); // Send to client
        Debug.Log($"Sent character creation options to client {conn.connectionId}. At character limit: {msg.atCharacterLimit}"); // Log success
    }

    /// Player Spawning
    public void HandlePlayerSpawnRequest(NetworkConnectionToClient conn, string characterId)
    {
        string userId = conn.authenticationData as string; // Get user ID from auth data
        if (string.IsNullOrEmpty(userId)) // Verify authentication
        {
            Debug.LogError($"Connection {conn.connectionId} requested spawn without valid auth"); // Log error
            return; // Exit
        }

        StartCoroutine(FetchCharacterSceneAndSpawn(conn, userId, characterId)); // Start spawn coroutine
    }

    private IEnumerator FetchCharacterSceneAndSpawn(NetworkConnectionToClient conn, string userId, string characterId)
    {
        connectionCharacterIds[conn] = characterId; // Store character ID for connection

        var locationTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("location").GetValueAsync(); // Query location

        yield return new WaitUntil(() => locationTask.IsCompleted); // Wait for query to complete

        if (locationTask.IsFaulted) // Check for query error
        {
            Debug.LogError($"Failed to fetch location data: {locationTask.Exception}"); // Log error
            SendGameSceneTransitionResponse(conn, true, GameScene.Farm_Scene.ToString(), Vector3.zero); // Use default location
            yield break; // Exit coroutine
        }

        var snapshot = locationTask.Result; // Get query result
        string sceneName = snapshot.Child("sceneName").Value?.ToString() ?? GameScene.Farm_Scene.ToString(); // Get scene name
        float x = Convert.ToSingle(snapshot.Child("x").Value ?? 0f); // Get X position
        float y = Convert.ToSingle(snapshot.Child("y").Value ?? 0f); // Get Y position
        float z = Convert.ToSingle(snapshot.Child("z").Value ?? 0f); // Get Z position

        Vector3 spawnPosition = new Vector3(x, y, z); // Create position vector

        Debug.Log($"Character {characterId} location data: Scene={sceneName}, Position={spawnPosition}"); // Log info

        SpawnDataByConnection[conn] = new PlayerSpawnData // Store spawn data
        {
            CharacterId = characterId, // Set character ID
            Position = spawnPosition, // Set position
            SceneName = sceneName // Set scene name
        };

        SendGameSceneTransitionResponse(conn, true, sceneName, spawnPosition); // Send transition response
    }

    private void SendGameSceneTransitionResponse(NetworkConnectionToClient conn, bool approved, string sceneName, Vector3 position, string message = "")
    {
        conn.Send(new GameSceneTransitionResponseMessage // Create and send message
        {
            approved = approved, // Set approved flag
            sceneName = sceneName, // Set scene name
            spawnPosition = position, // Set position
            message = message // Set message
        });
    }

    public void HandleGameSceneTransitionRequest(NetworkConnectionToClient conn, string characterId, string targetScene)
    {
        if (Enum.TryParse<GameScene>(targetScene, out GameScene targetGameScene)) // Parse scene name
        {
            // Additional validation could be added here

            SendGameSceneTransitionResponse(conn, true, targetGameScene.ToString(), Vector3.zero); // Send success response
        }
        else
        {
            SendGameSceneTransitionResponse(conn, false, targetScene, Vector3.zero, "Invalid scene name"); // Send error response
        }
    }

    public void SpawnPlayerForClient(NetworkConnectionToClient conn, string characterId, Vector3 position, bool isSceneTransition = false)
    {
        // Get the SpawnData to find the correct scene name
        if (!SpawnDataByConnection.TryGetValue(conn, out PlayerSpawnData spawnData))
        {
            Debug.LogError($"No spawn data found for connection {conn.connectionId}");
            return;
        }

        string sceneName = spawnData.SceneName;
        Scene targetScene = SceneManager.GetSceneByName(sceneName);

        if (!targetScene.isLoaded)
        {
            Debug.LogError($"Target scene {sceneName} is not loaded on server!");
            return;
        }

        // Create player instance in current active scene
        GameObject playerInstance = Instantiate(playerPrefab, position, Quaternion.identity);

        // Move the player to the correct scene
        SceneManager.MoveGameObjectToScene(playerInstance, targetScene);

        // Set the layer to match the scene name
        int layerIndex = LayerMask.NameToLayer(sceneName);
        if (layerIndex != -1)
        {
            // Set layer for the player and all children
            SetLayerRecursively(playerInstance, layerIndex);
            Debug.Log($"Player layer set to '{sceneName}' (layer index: {layerIndex})");
        }
        else
        {
            Debug.LogWarning($"Layer named '{sceneName}' not found. Using default layer instead.");
            // Leave on default layer (no change needed)
        }

        // Set up player identity
        PlayerNetworkController playerController = playerInstance.GetComponent<PlayerNetworkController>();
        if (playerController != null)
        {
            playerController.SetCharacterId(characterId);
        }

        // Get userId once at the beginning
        string userId = conn.authenticationData as string;

        // Set up character data
        PlayerCharacterData characterData = playerInstance.GetComponent<PlayerCharacterData>();
        if (characterData != null)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                InitializeCharacterData(userId, characterId, characterData);
            }
        }

        // Register with network
        NetworkServer.Spawn(playerInstance, conn);

        // Associate with connection
        var networkManager = NetworkManager.singleton as CustomNetworkManager;
        if (networkManager != null)
        {
            if (isSceneTransition)
                networkManager.ReplacePlayerForConnection(conn, playerInstance);
            else
                networkManager.AddPlayerForConnection(conn, playerInstance);
        }

        // Add to active players tracking (using the same userId variable)
        activePlayers[conn] = new ActivePlayerInfo
        {
            UserId = userId,
            CharacterId = characterId,
            CharacterName = "" // Will be populated when character data loads
        };

        LogAllConnectedPlayers();

        Scene playerServerScene = playerInstance.scene;
        string clientTargetScene = sceneName;

        Debug.Log($"Player spawned for character {characterId}:" +
            $"\nSERVER: In scene '{playerServerScene.name}' at position {position}" +
            $"\nCLIENT: Loading into scene '{clientTargetScene}'");
    }

    private void SetLayerRecursively(GameObject obj, int layerIndex)
    {
        // Set layer for this object
        obj.layer = layerIndex;

        // Set layer for all children
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layerIndex);
        }
    }

    private void InitializeCharacterData(string userId, string characterId, PlayerCharacterData characterData)
    {
        characterData.characterId = characterId;
        characterData.userId = userId; // Add this line to set the user ID

        // Get PlayerInventory component
        PlayerInventory playerInventory = characterData.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            // Start both coroutines
            StartCoroutine(FetchAndSetCharacterData(userId, characterId, characterData));
            StartCoroutine(FetchAndSetInventoryData(userId, characterId, playerInventory));
        }
        else
        {
            Debug.LogError($"PlayerInventory component not found on character {characterId}");
            // Still initialize character data even if inventory fails
            StartCoroutine(FetchAndSetCharacterData(userId, characterId, characterData));
        }
    }

    private IEnumerator FetchAndSetCharacterData(string userId, string characterId, PlayerCharacterData characterData)
    {
        var infoTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("info").GetValueAsync();
        yield return new WaitUntil(() => infoTask.IsCompleted);

        if (!infoTask.IsFaulted && infoTask.Result.Exists)
        {
            var infoData = infoTask.Result;
            characterData.characterName = infoData.Child("characterName").Value?.ToString();
            characterData.characterClass = infoData.Child("characterClass").Value?.ToString();
            characterData.level = Convert.ToInt32(infoData.Child("level").Value);
            characterData.experience = Convert.ToInt32(infoData.Child("experience").Value);
            
            // Update the active player's character name
            var conn = NetworkServer.connections.FirstOrDefault(c => 
                c.Value != null && 
                activePlayers.ContainsKey(c.Value) && 
                activePlayers[c.Value].CharacterId == characterId).Value;

            if (conn != null && activePlayers.ContainsKey(conn))
            {
                activePlayers[conn].CharacterName = characterData.characterName;
                Debug.Log($"Updated active player info - Name: {characterData.characterName}, CharacterId: {characterId}");
                
                LogAllConnectedPlayers();
            }
        }

        var equipTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("equipment").GetValueAsync();
        yield return new WaitUntil(() => equipTask.IsCompleted);

        if (!equipTask.IsFaulted && equipTask.Result.Exists)
        {
            var equipData = equipTask.Result;
            characterData.headItemNumber = Convert.ToInt32(equipData.Child("head").Value);
            characterData.bodyItemNumber = Convert.ToInt32(equipData.Child("body").Value);
            characterData.hairItemNumber = Convert.ToInt32(equipData.Child("hair").Value);
            characterData.torsoItemNumber = Convert.ToInt32(equipData.Child("torso").Value);
            characterData.legsItemNumber = Convert.ToInt32(equipData.Child("legs").Value);
        }

        Vector3 currentPosition = characterData.transform.position;
        characterData.sceneName = characterData.transform.position.ToString();
        characterData.x = currentPosition.x;
        characterData.y = currentPosition.y;
        characterData.z = currentPosition.z;
    }

    private IEnumerator FetchAndSetInventoryData(string userId, string characterId, PlayerInventory playerInventory)
    {
        Debug.Log($"[INVENTORY] Starting inventory fetch for character {characterId}");

        var inventoryTask = dbReference.Child("users").Child(userId).Child("characters").Child(characterId).Child("inventory").GetValueAsync();
        yield return new WaitUntil(() => inventoryTask.IsCompleted);

        Debug.Log($"[INVENTORY] Firebase task completed. Faulted: {inventoryTask.IsFaulted}, Exists: {inventoryTask.Result?.Exists}");

        if (!inventoryTask.IsFaulted && inventoryTask.Result.Exists)
        {
            var inventoryData = inventoryTask.Result;

            // Get bag ID from Firebase
            int bagId = Convert.ToInt32(inventoryData.Child("bagId").Value);
            Debug.Log($"[INVENTORY] Found bagId in Firebase: {bagId}");

            playerInventory.bagId = bagId.ToString();

            Debug.Log($"[INVENTORY] bagList is null: {bagList == null}");
            if (bagList != null)
            {
                Debug.Log($"[INVENTORY] bagList.bags count: {bagList.bags?.Length ?? 0}");
            }

            // Find matching bag in SO_BagList to get maxSlots
            foreach (var bag in bagList.bags)
            {
                Debug.Log($"[INVENTORY] Checking bag {bag.bagId} against {bagId}");
                if (bag.bagId == bagId)
                {
                    Debug.Log($"[INVENTORY] Found matching bag! maxSlots: {bag.maxSlots}");

                    // Set maxSlots from bag data
                    playerInventory.maxSlots = bag.maxSlots;

                    // Initialize inventory array with correct size
                    playerInventory.InventoryItems = new InventoryItem[bag.maxSlots];

                    // Initialize all slots as empty
                    for (int i = 0; i < bag.maxSlots; i++)
                    {
                        playerInventory.InventoryItems[i] = new InventoryItem
                        {
                            itemCode = 0,
                            itemQuantity = 0
                        };
                    }

                    Debug.Log($"[INVENTORY] Initialized {bag.maxSlots} inventory slots");

                    // Load actual items if they exist
                    var itemsData = inventoryData.Child("items");
                    if (itemsData.Exists)
                    {
                        Debug.Log($"[INVENTORY] Loading items from Firebase");
                        foreach (var itemSnapshot in itemsData.Children)
                        {
                            int slotIndex = int.Parse(itemSnapshot.Key);
                            if (slotIndex < bag.maxSlots)
                            {
                                playerInventory.InventoryItems[slotIndex] = new InventoryItem
                                {
                                    itemCode = Convert.ToInt32(itemSnapshot.Child("itemCode").Value),
                                    itemQuantity = Convert.ToInt32(itemSnapshot.Child("itemQuantity").Value)
                                };
                                Debug.Log($"[INVENTORY] Loaded item {itemSnapshot.Child("itemCode").Value} in slot {slotIndex}");
                            }
                        }
                    }
                    break;
                }
            }
        }
        else
        {
            Debug.LogError($"[INVENTORY] Failed to fetch inventory data. Faulted: {inventoryTask.IsFaulted}");
        }
    }



    /// Helper Classes and Methods
 
    public class ActivePlayerInfo
    {
        public string UserId { get; set; }
        public string CharacterId { get; set; }
        public string CharacterName { get; set; }
    }

    public void HandlePlayerDisconnection(NetworkConnectionToClient conn)
    {
        if (activePlayers.ContainsKey(conn))
        {
            Debug.Log($"Player {activePlayers[conn].CharacterName} disconnected");
            activePlayers.Remove(conn);
        }
        
        // Clean up other dictionaries
        connectionCharacterIds.Remove(conn);
        SpawnDataByConnection.Remove(conn);
    }

    public ActivePlayerInfo GetActivePlayerInfo(NetworkConnectionToClient conn)
    {
        return activePlayers.TryGetValue(conn, out var info) ? info : null;
    }
    public class PlayerSpawnData
    {
        public string CharacterId; // Character identifier
        public Vector3 Position; // Spawn position
        public string SceneName; // Scene name
    }

    public void LogAllConnectedPlayers()
    {
        Debug.Log("=== CONNECTED PLAYERS ===");
        int index = 1;
        foreach (var player in activePlayers)
        {
            var info = player.Value;
            Debug.Log($"{index}. UserId: {info.UserId}, CharacterId: {info.CharacterId}, CharacterName: {info.CharacterName}");
            index++;
        }
        Debug.Log($"Total Players Online: {activePlayers.Count}");
        Debug.Log("========================");
    }

    public bool TryGetSpawnData(NetworkConnectionToClient conn, out PlayerSpawnData spawnData)
    {
        return SpawnDataByConnection.TryGetValue(conn, out spawnData); // Get spawn data if exists
    }

    public void StoreSpawnData(NetworkConnectionToClient conn, string characterId, Vector3 position, string sceneName)
    {
        SpawnDataByConnection[conn] = new PlayerSpawnData // Store spawn data
        {
            CharacterId = characterId, // Set character ID
            Position = position, // Set position
            SceneName = sceneName // Set scene name
        };

        Debug.Log($"Stored spawn data for character {characterId}: Scene={sceneName}, Position={position}"); // Log success
    }

    string GetCharacterId(NetworkConnectionToClient conn)
    {
        if (connectionCharacterIds.TryGetValue(conn, out string characterId)) // Try get character ID
            return characterId; // Return ID if exists
        return null; // Return null if not found
    }

    public SO_BagData.BagData GetBagData(int bagId)
    {
        if (bagList == null)
        {
            Debug.LogError("Bag list not assigned in ServerPlayerDataManager!");
            return null;
        }

        return bagList.GetBagById(bagId);
    }

    public bool IsInventoryAtCapacity(int bagId, int currentItemCount)
    {
        var bagData = GetBagData(bagId);
        if (bagData == null) return true; // Assume at capacity if bag not found

        return currentItemCount >= bagData.maxSlots;
    }
    
    public string GetCharacterIdForConnection(NetworkConnectionToClient conn)
    {
        if (connectionCharacterIds.TryGetValue(conn, out string characterId))
        {
            return characterId;
        }
        
        Debug.LogWarning($"No character ID found for connection {conn.connectionId}");
        return null;
    }
}