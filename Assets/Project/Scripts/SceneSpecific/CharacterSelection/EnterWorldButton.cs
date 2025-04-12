using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EnterWorldButton : MonoBehaviour
{
    [SerializeField] private Button enterWorldButton;
    
    private ClientPlayerDataManager dataManager;
    
    private void Start()
    {
        dataManager = ClientPlayerDataManager.Instance;
        
        if (enterWorldButton == null)
            enterWorldButton = GetComponent<Button>();
            
        enterWorldButton.onClick.AddListener(OnEnterWorldClicked);
        
        // Initially disable button until character is selected
        enterWorldButton.interactable = !string.IsNullOrEmpty(dataManager.SelectedCharacterId);
        
        // Add this line to listen for character selection
        dataManager.OnCharacterSelected += OnCharacterSelected;
    }

    // Add this method
    private void OnCharacterSelected(string characterId)
    {
        enterWorldButton.interactable = !string.IsNullOrEmpty(characterId);
    }

    // Don't forget to unsubscribe when destroyed
    private void OnDestroy()
    {
        if (dataManager != null)
            dataManager.OnCharacterSelected -= OnCharacterSelected;
    }
    
    private void OnEnterWorldClicked()
    {
        if (string.IsNullOrEmpty(dataManager.SelectedCharacterId))
        {
            Debug.LogWarning("No character selected!");
            return;
        }
        
        Debug.Log($"Entering world with character: {dataManager.SelectedCharacterId}");
        
        // Get character's location data
        var locationData = dataManager.GetLocation(dataManager.SelectedCharacterId);
        
        if (locationData == null)
        {
            Debug.LogWarning("No location data available for selected character! Using default location.");
            // Create default location data as a fallback
            locationData = new ClientPlayerDataManager.LocationData
            {
                sceneName = SceneName.FarmScene.ToString(),
                position = Vector3.zero
            };
        }
        
        // Parse the scene name to SceneName enum
        if (System.Enum.TryParse(locationData.sceneName, out SceneName targetScene))
        {
            // Start scene transition with fade
            SceneTransitionManager.Instance?.FadeOut(() => {
                // Send spawn request after fade out
                NetworkClient.Send(new SpawnPlayerRequestMessage
                {
                    characterId = dataManager.SelectedCharacterId
                });
            });
        }
        else
        {
            Debug.LogError($"Invalid scene name in character data: {locationData.sceneName}");
        }
    }

   }