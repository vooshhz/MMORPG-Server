using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button createButton;
    
    [Header("Class Selection")]
    [SerializeField] private Button warriorButton;
    [SerializeField] private Button magicianButton;
    [SerializeField] private Button luminaryButton;
    [SerializeField] private Button hunterButton;
    private string selectedClass = "Warrior"; // Default class
    
    [Header("Customization Controls")]
    [SerializeField] private Button bodyLeftButton;
    [SerializeField] private Button bodyRightButton;
    [SerializeField] private Button hairLeftButton;
    [SerializeField] private Button hairRightButton;
    [SerializeField] private Button torsoLeftButton;
    [SerializeField] private Button torsoRightButton;
    [SerializeField] private Button legsLeftButton;
    [SerializeField] private Button legsRightButton;
    
    [Header("Character Preview")]
    [SerializeField] private CharacterAnimator characterPreview;
    
    // Available options from server
    private string[] availableClasses;
    private int[] bodyOptions;
    private int[] hairOptions;
    private int[] headOptions;
    private int[] torsoOptions;
    private int[] legsOptions;
    
    // Current selection indices
    private int currentBodyIndex = 0;
    private int currentHairIndex = 0;
    private int currentHeadIndex = 0;
    private int currentTorsoIndex = 0;
    private int currentLegsIndex = 0;
    
    // Creation state
    private bool hasReceivedOptions = false;
    private bool isCreatingCharacter = false;
    private bool atCharacterLimit = false;
    private bool isSubmitting = false;


    
    private void Start()
    {
        // Set up button event listeners
        warriorButton.onClick.AddListener(() => SelectClass("Warrior"));
        magicianButton.onClick.AddListener(() => SelectClass("Magician"));
        luminaryButton.onClick.AddListener(() => SelectClass("Luminary"));
        hunterButton.onClick.AddListener(() => SelectClass("Hunter"));
        
        bodyLeftButton.onClick.AddListener(() => {
            // Cycle body option
            CycleOption(ref currentBodyIndex, bodyOptions, value => characterPreview.bodyItemNumber = value);
            // Also update head option
            if (headOptions != null && headOptions.Length > 0 && currentBodyIndex < headOptions.Length) {
                characterPreview.headItemNumber = headOptions[currentBodyIndex];
            }
        });
        
        bodyRightButton.onClick.AddListener(() => {
            // Cycle body option
            CycleOption(ref currentBodyIndex, bodyOptions, value => characterPreview.bodyItemNumber = value, true);
            // Also update head option
            if (headOptions != null && headOptions.Length > 0 && currentBodyIndex < headOptions.Length) {
                characterPreview.headItemNumber = headOptions[currentBodyIndex];
            }
        });
        
        hairLeftButton.onClick.AddListener(() => CycleOption(ref currentHairIndex, hairOptions, value => characterPreview.hairItemNumber = value));
        hairRightButton.onClick.AddListener(() => CycleOption(ref currentHairIndex, hairOptions, value => characterPreview.hairItemNumber = value, true));
        
        torsoLeftButton.onClick.AddListener(() => CycleOption(ref currentTorsoIndex, torsoOptions, value => characterPreview.torsoItemNumber = value));
        torsoRightButton.onClick.AddListener(() => CycleOption(ref currentTorsoIndex, torsoOptions, value => characterPreview.torsoItemNumber = value, true));
        
        legsLeftButton.onClick.AddListener(() => CycleOption(ref currentLegsIndex, legsOptions, value => characterPreview.legsItemNumber = value));
        legsRightButton.onClick.AddListener(() => CycleOption(ref currentLegsIndex, legsOptions, value => characterPreview.legsItemNumber = value, true));
        
        createButton.onClick.AddListener(CreateCharacter);
        
        // Register network handlers
        NetworkClient.RegisterHandler<CharacterCreationOptionsMessage>(OnReceiveCreationOptions);
        NetworkClient.RegisterHandler<CreateCharacterResponseMessage>(OnCreateCharacterResponse);
        
        // Request available options from server
        RequestCreationOptions();
        
        // Disable controls until options are received
        SetControlsInteractable(false);
    }
    private void RequestCreationOptions()
    {
        if (NetworkClient.isConnected)
        {
            NetworkClient.Send(new RequestCharacterCreationOptionsMessage());
            Debug.Log("Requesting character creation options from server...");
        }
        else
        {
            Debug.LogError("Not connected to server. Cannot request character creation options.");
        }
    }
    
    private void OnReceiveCreationOptions(CharacterCreationOptionsMessage msg)
    {
        Debug.Log("Received character creation options from server");
        
        // Store options
        availableClasses = msg.availableClasses;
        bodyOptions = msg.bodyOptions;
        hairOptions = msg.hairOptions;
        headOptions = msg.headOptions;
        torsoOptions = msg.torsoOptions;
        legsOptions = msg.legsOptions;
        atCharacterLimit = msg.atCharacterLimit;  // Store the character limit flag
        
        // Set default values on character preview
        if (bodyOptions.Length > 0) characterPreview.bodyItemNumber = bodyOptions[0];
        if (hairOptions.Length > 0) characterPreview.hairItemNumber = hairOptions[0];
        if (headOptions.Length > 0) characterPreview.headItemNumber = headOptions[0];
        if (torsoOptions.Length > 0) characterPreview.torsoItemNumber = torsoOptions[0];
        if (legsOptions.Length > 0) characterPreview.legsItemNumber = legsOptions[0];
        
        // Refresh the character preview
        characterPreview.RefreshCurrentFrame();
        
        // Enable controls
        hasReceivedOptions = true;
        SetControlsInteractable(true);
        
        // But disable create button if at character limit
        if (atCharacterLimit)
        {
            createButton.interactable = false;
            // Optionally add a tooltip or text explaining why button is disabled
            if (createButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Character Limit Reached";
        }
        
        // Select default class
        SelectClass("Warrior");
    }
    
    private void CycleOption<T>(ref int currentIndex, T[] options, System.Action<T> applyValue, bool forward = false)
    {
        if (options == null || options.Length == 0)
            return;
            
        if (forward)
        {
            currentIndex = (currentIndex + 1) % options.Length;
        }
        else
        {
            currentIndex = (currentIndex - 1 + options.Length) % options.Length;
        }
        
        applyValue(options[currentIndex]);
        characterPreview.RefreshCurrentFrame();
    }
    
    private void SelectClass(string className)
    {
        selectedClass = className;
        
        // Highlight the selected button
        HighlightSelectedClassButton(className);
        
        // Apply default equipment for this class
        // In a full implementation, you might get default equipment for classes from the server
        // For now we'll just use current equipment
    }
    
    private void HighlightSelectedClassButton(string className)
    {
        // Reset all buttons
        ColorBlock colors;
        
        colors = warriorButton.colors;
        colors.normalColor = Color.white;
        warriorButton.colors = colors;
        
        colors = magicianButton.colors;
        colors.normalColor = Color.white;
        magicianButton.colors = colors;
        
        colors = luminaryButton.colors;
        colors.normalColor = Color.white;
        luminaryButton.colors = colors;
        
        colors = hunterButton.colors;
        colors.normalColor = Color.white;
        hunterButton.colors = colors;
        
        // Highlight selected button
        Button selectedButton = null;
        
        switch (className)
        {
            case "Warrior": selectedButton = warriorButton; break;
            case "Magician": selectedButton = magicianButton; break;
            case "Luminary": selectedButton = luminaryButton; break;
            case "Hunter": selectedButton = hunterButton; break;
        }
        
        if (selectedButton != null)
        {
            colors = selectedButton.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 1f); // Light blue highlight
            selectedButton.colors = colors;
        }
    }
    
    private void CreateCharacter()
    {
        if (!hasReceivedOptions || isCreatingCharacter)
            return;
        
        // Set the flag to prevent double-submission
        isSubmitting = true;
        // Disable the button
        createButton.interactable = false;
        createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Creating...";
        // Validate name
        string characterName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(characterName) || characterName.Length < 3 || characterName.Length > 16)
        {
            // Show error (in a real implementation, you'd have a UI element for this)
            Debug.LogError("Invalid character name (must be 3-16 characters)");
            return;
        }
        
        // Create request message
        var msg = new CreateCharacterRequestMessage
        {
            characterName = characterName,
            characterClass = selectedClass,
            bodyItem = characterPreview.bodyItemNumber,
            headItem = characterPreview.headItemNumber,
            hairItem = characterPreview.hairItemNumber,
            torsoItem = characterPreview.torsoItemNumber,
            legsItem = characterPreview.legsItemNumber
        };
        
        // Send to server
        NetworkClient.Send(msg);
        isCreatingCharacter = true;
        
        // Disable controls during creation
        SetControlsInteractable(false);
        createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Creating...";
    }
    
    private void OnCreateCharacterResponse(CreateCharacterResponseMessage msg)
    {
        isCreatingCharacter = false;
        // Reset the flag
        isSubmitting = false;
        
        if (msg.success)
        {
            Debug.Log($"Character created successfully! ID: {msg.characterId}");
            
            // Return to character selection screen
            if (LobbySceneManager.Instance != null)
            {
                LobbySceneManager.Instance.RequestSceneTransition(LobbyScene.CharacterSelectionScene);
            } 
        }
        else
        {
            Debug.LogError($"Character creation failed: {msg.message}");
            
            // Re-enable controls
            SetControlsInteractable(true);
            createButton.interactable = true;
            createButton.GetComponentInChildren<TextMeshProUGUI>().text = "Create";
            
            // Show error message (in a real implementation, you'd have a UI element for this)
        }
    }
    
    private void SetControlsInteractable(bool interactable)
    {
        warriorButton.interactable = interactable;
        magicianButton.interactable = interactable;
        luminaryButton.interactable = interactable;
        hunterButton.interactable = interactable;
        
        bodyLeftButton.interactable = interactable;
        bodyRightButton.interactable = interactable;
        hairLeftButton.interactable = interactable;
        hairRightButton.interactable = interactable;
        torsoLeftButton.interactable = interactable;
        torsoRightButton.interactable = interactable;
        legsLeftButton.interactable = interactable;
        legsRightButton.interactable = interactable;
        
        createButton.interactable = interactable;
        nameInput.interactable = interactable;
    }
}