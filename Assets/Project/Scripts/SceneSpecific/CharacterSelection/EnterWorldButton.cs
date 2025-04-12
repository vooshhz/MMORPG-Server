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
            Debug.LogError("No location data available for selected character!");
            return;
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