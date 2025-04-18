using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EnterWorldButton : MonoBehaviour
{
    [SerializeField] private Button enterWorldButton;
    [SerializeField] private GameObject loadingIndicator; // Optional loading indicator
    
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
        
        // Hide loading indicator initially
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
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
        
        // Disable button to prevent multiple clicks
        enterWorldButton.interactable = false;
        
        // Show loading indicator if available
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        
        // Send spawn request to server
        NetworkClient.Send(new SpawnPlayerRequestMessage
        {
            characterId = dataManager.SelectedCharacterId
        });
        
        // The server will handle scene change and player spawning
    }
}