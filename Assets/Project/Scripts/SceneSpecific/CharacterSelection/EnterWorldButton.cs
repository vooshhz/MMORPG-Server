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
        
        // Initially disable button until character is selected
        enterWorldButton.interactable = !string.IsNullOrEmpty(dataManager.SelectedCharacterId);
        
        // Add this line to listen for character selection
        dataManager.OnCharacterSelected += OnCharacterSelected;
        
        enterWorldButton.onClick.AddListener(OnEnterWorldClicked);

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
        // Get selected character ID
        string characterId = dataManager.SelectedCharacterId;
        
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogWarning("No character selected!");
            return;
        }
            
        // Send request to server
        Debug.Log($"Requesting to enter world with character: {characterId}");
        NetworkClient.Send(new SpawnPlayerRequestMessage
        {
            characterId = characterId
        });
    }

}