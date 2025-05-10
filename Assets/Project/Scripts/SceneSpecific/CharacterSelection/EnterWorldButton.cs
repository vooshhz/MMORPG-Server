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
    

}