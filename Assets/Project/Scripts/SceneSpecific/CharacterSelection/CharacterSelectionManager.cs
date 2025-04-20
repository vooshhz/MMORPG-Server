using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Character Panels")]
    [SerializeField] private GameObject characterPanel1;
    [SerializeField] private GameObject characterPanel2;
    [SerializeField] private GameObject characterPanel3;
    
    [Header("Character Info - Panel 1")]
    [SerializeField] private TMP_Text nameText1;
    [SerializeField] private TMP_Text classText1;
    [SerializeField] private TMP_Text levelText1;
    [SerializeField] private CharacterAnimator characterAnimator1;
    [SerializeField] private RawImage panelImage1;
    
    [Header("Character Info - Panel 2")]
    [SerializeField] private TMP_Text nameText2;
    [SerializeField] private TMP_Text classText2;
    [SerializeField] private TMP_Text levelText2;
    [SerializeField] private CharacterAnimator characterAnimator2;
    [SerializeField] private RawImage panelImage2;
    
    [Header("Character Info - Panel 3")]
    [SerializeField] private TMP_Text nameText3;
    [SerializeField] private TMP_Text classText3;
    [SerializeField] private TMP_Text levelText3;
    [SerializeField] private CharacterAnimator characterAnimator3;
    [SerializeField] private RawImage panelImage3;

    [Header("Create Character")]
    [SerializeField] private Button createCharacterButton;
    [SerializeField] private string characterCreationSceneName = "CharacterCreationScene";
    
    private ClientPlayerDataManager dataManager;
    private RawImage selectedPanelImage;
    private string selectedCharacterId;
    
    private void Awake()
    {
        Debug.Log("[CharacterSelectionManager] Awake called");
        
        // Hide all panels initially
        characterPanel1.SetActive(false);
        characterPanel2.SetActive(false);
        characterPanel3.SetActive(false);
        

    }
    
    private void Start()
    {
        Debug.Log("[CharacterSelectionManager] Start called");

        // After authentication is complete
        if (NetworkClient.isConnected)
        {
            NetworkClient.Send(new CharacterPreviewRequestMessage
            {
                userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId
            });

            if (createCharacterButton != null)
            {
                createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            }
        }

        
        // Get reference to data manager singleton
        dataManager = ClientPlayerDataManager.Instance;
        
        if (dataManager == null)
        {
            Debug.LogError("[CharacterSelectionManager] ClientPlayerDataManager not found! Make sure it exists in the scene.");
            return;
        }
        
        Debug.Log("[CharacterSelectionManager] ClientPlayerDataManager found: " + dataManager.name);
        
        // Set up event listeners for data reception
        dataManager.OnCharacterDataReceived += OnCharacterDataReceived;
        dataManager.OnEquipmentDataReceived += OnEquipmentDataReceived;
        
        // Request all character data from server
        Debug.Log("[CharacterSelectionManager] Requesting character data...");      

        // If using the createCharacterButton, update its interactability based on character count
        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            
            // Check if we already have reached the character limit
            if (dataManager != null && dataManager.GetAllCharacterIds().Count >= 3)
            {
                createCharacterButton.interactable = false;
                // Optionally update button text
                if (createCharacterButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                    createCharacterButton.GetComponentInChildren<TextMeshProUGUI>().text = "Character Limit Reached";
            }
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log("[CharacterSelectionManager] OnDestroy called");
        
        // Clean up event subscriptions
        if (dataManager != null)
        {
            dataManager.OnCharacterDataReceived -= OnCharacterDataReceived;
            dataManager.OnEquipmentDataReceived -= OnEquipmentDataReceived;
        }
    }
    
    private void OnCharacterDataReceived()
    {
        Debug.Log("[CharacterSelectionManager] OnCharacterDataReceived event triggered");
        UpdateCharacterPanels();

            // Update create character button based on character count
    if (createCharacterButton != null && dataManager != null)
    {
        bool atLimit = dataManager.GetAllCharacterIds().Count >= 3;
        createCharacterButton.interactable = !atLimit;
        
        // Optionally update button text
        if (atLimit && createCharacterButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            createCharacterButton.GetComponentInChildren<TextMeshProUGUI>().text = "Character Limit Reached";
    }
    }
    
    private void OnEquipmentDataReceived()
    {
        Debug.Log("[CharacterSelectionManager] OnEquipmentDataReceived event triggered");
        UpdateEquipment();
    }
    
    private void RequestCharacterData()
    {
        // Make sure we're connected
        if (NetworkClient.connection == null || !NetworkClient.connection.isReady)
        {
            Debug.LogWarning("[CharacterSelectionManager] Not connected to server. Cannot request character data.");
            return;
        }
        
        // Request all character data from server through the data manager
        Debug.Log("[CharacterSelectionManager] Requesting all character data from server...");
        dataManager.RequestAllCharacterData(FirebaseAuth.DefaultInstance.CurrentUser.UserId);
    }
    
    private void UpdateCharacterPanels()
    {
        if (!dataManager.HasCharacterData)
        {
            Debug.LogWarning("[CharacterSelectionManager] No character data available yet.");
            return;
        }
            
        List<string> characterIds = dataManager.GetAllCharacterIds();
        Debug.Log($"[CharacterSelectionManager] Updating panels with {characterIds.Count} characters");
        
        // Hide all panels first
        characterPanel1.SetActive(false);
        characterPanel2.SetActive(false);
        characterPanel3.SetActive(false);
        
        // Set up panels based on available characters (up to 3)
        for (int i = 0; i < Mathf.Min(characterIds.Count, 3); i++)
        {
            string charId = characterIds[i];
            ClientPlayerDataManager.CharacterInfo charInfo = dataManager.GetCharacterInfo(charId);
            
            if (charInfo == null)
            {
                Debug.LogWarning($"[CharacterSelectionManager] Character info is null for ID: {charId}");
                continue;
            }
                
            Debug.Log($"[CharacterSelectionManager] Setting up panel {i+1} for character: {charInfo.characterName} (ID: {charId})");
            // Set up the corresponding panel
            SetupCharacterPanel(i, charId, charInfo);
            
            // Request detailed data for this character
            Debug.Log($"[CharacterSelectionManager] Requesting detailed data for character: {charId}");
            dataManager.RequestCharacterData(FirebaseAuth.DefaultInstance.CurrentUser.UserId, charId);
        }
    }
    
    private void SetupCharacterPanel(int index, string charId, ClientPlayerDataManager.CharacterInfo charInfo)
    {
        GameObject panel = null;
        TMP_Text nameText = null;
        TMP_Text classText = null;
        TMP_Text levelText = null;
        RawImage panelImage = null;
        
        // Select the correct panel based on index
        switch (index)
        {
            case 0:
                panel = characterPanel1;
                nameText = nameText1;
                classText = classText1;
                levelText = levelText1;
                panelImage = panelImage1;
                break;
            case 1:
                panel = characterPanel2;
                nameText = nameText2;
                classText = classText2;
                levelText = levelText2;
                panelImage = panelImage2;
                break;
            case 2:
                panel = characterPanel3;
                nameText = nameText3;
                classText = classText3;
                levelText = levelText3;
                panelImage = panelImage3;
                break;
        }
        
        if (panel == null)
        {
            Debug.LogError($"[CharacterSelectionManager] Panel {index+1} references are null");
            return;
        }
            
        // Activate the panel and set text
        panel.SetActive(true);
        nameText.text = charInfo.characterName;
        classText.text = charInfo.characterClass;
        levelText.text = "Level " + charInfo.level;
        
        Debug.Log($"[CharacterSelectionManager] Panel {index+1} activated for {charInfo.characterName} (Level {charInfo.level} {charInfo.characterClass})");
        
        // Set up panel click listener
        Button panelButton = panel.GetComponent<Button>();
        if (panelButton != null)
        {
            panelButton.onClick.RemoveAllListeners();
            panelButton.onClick.AddListener(() => SelectCharacter(charId, panelImage));
        }
        
        // Store the character ID in the panel for reference
        panel.name = "CharacterPanel_" + charId;
    }
    
    private void UpdateEquipment()
    {
        Debug.Log("[CharacterSelectionManager] Updating equipment for character models");
        
        if (!dataManager.HasEquipmentData)
        {
            Debug.LogWarning("[CharacterSelectionManager] No equipment data available yet");
            return;
        }
        
        List<string> characterIds = dataManager.GetAllCharacterIds();
        
        // Update equipment for each character panel
        for (int i = 0; i < Mathf.Min(characterIds.Count, 3); i++)
        {
            string charId = characterIds[i];
            
            // Only process characters with complete data
            if (!dataManager.IsCharacterDataComplete(charId))
            {
                continue;
            }
            
            ClientPlayerDataManager.EquipmentData equipment = dataManager.GetEquipment(charId);
            
            if (equipment == null)
            {
                Debug.LogWarning($"[CharacterSelectionManager] No equipment data for character ID: {charId}");
                continue;
            }
                
            Debug.Log($"[CharacterSelectionManager] Applying equipment to character {i+1} (ID: {charId})");
            ApplyEquipmentToCharacter(i, equipment);
        }
    }
    
    private void ApplyEquipmentToCharacter(int index, ClientPlayerDataManager.EquipmentData equipment)
    {
        CharacterAnimator animator = null;
        
        // Select the correct animator based on index
        switch (index)
        {
            case 0:
                animator = characterAnimator1;
                break;
            case 1:
                animator = characterAnimator2;
                break;
            case 2:
                animator = characterAnimator3;
                break;
        }
        
        if (animator == null)
        {
            Debug.LogError($"[CharacterSelectionManager] CharacterAnimator {index+1} is null");
            return;
        }
            
        // Apply equipment item numbers to the animator
        animator.headItemNumber = equipment.head;
        animator.bodyItemNumber = equipment.body;
        animator.hairItemNumber = equipment.hair;
        animator.torsoItemNumber = equipment.torso;
        animator.legsItemNumber = equipment.legs;
        
        Debug.Log($"[CharacterSelectionManager] Applied equipment to character {index+1}: " +
                  $"Head={equipment.head}, Body={equipment.body}, Hair={equipment.hair}, " +
                  $"Torso={equipment.torso}, Legs={equipment.legs}");
        
        // Refresh the character visual
        animator.RefreshCurrentFrame();
    }
    
    private void SelectCharacter(string charId, RawImage panelImage)
    {
        Debug.Log($"[CharacterSelectionManager] Character selected: {charId}");
        
        // Store the selected character ID
        selectedCharacterId = charId;
        
        // Update panel highlighting
        if (selectedPanelImage != null)
        {
            selectedPanelImage.color = Color.white;
        }
        
        panelImage.color = Color.green;
        selectedPanelImage = panelImage;
        
        // If using the ClientPlayerDataManager for character selection
        dataManager.SelectCharacter(charId);
    }

    private void OnCreateCharacterClicked()
    {
        Debug.Log("[CharacterSelectionManager] Loading character creation scene...");
    
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(LobbyScene.CharacterCreationScene);
        }
    }
}