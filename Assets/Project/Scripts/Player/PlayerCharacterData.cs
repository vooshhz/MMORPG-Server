using UnityEngine;
using Mirror;

public class PlayerCharacterData : NetworkBehaviour
{
    [Header("Character ID")]
    [SyncVar]
    public string characterId; // Synced unique identifier for this character
    
    [Header("Equipment")]
    [SyncVar(hook = nameof(OnHeadItemChanged))]
    public int headItemNumber; // Head equipment item code with change callback
    [SyncVar(hook = nameof(OnBodyItemChanged))]
    public int bodyItemNumber; // Body/skin type with change callback
    [SyncVar(hook = nameof(OnHairItemChanged))]
    public int hairItemNumber; // Hair style with change callback
    [SyncVar(hook = nameof(OnTorsoItemChanged))]
    public int torsoItemNumber; // Torso/shirt item with change callback
    [SyncVar(hook = nameof(OnLegsItemChanged))]
    public int legsItemNumber; // Legs/pants item with change callback
    
    [Header("Info")]
    [SyncVar]
    public string characterName; // Player-chosen character name
    [SyncVar]
    public string characterClass; // Character class (Warrior, Magician, etc.)
    [SyncVar]
    public int experience; // Character experience points
    [SyncVar]
    public int level; // Character level
    
    [Header("Location")]
    [SyncVar]
    public string sceneName; // Current scene the character is in
    public float x; // X position coordinate
    public float y; // Y position coordinate
    public float z; // Z position coordinate
    
    // Reference to the character animator
    private CharacterAnimator characterAnimator; // Handles sprite animation and visual appearance
    
    // Called when the component initializes
    private void Awake()
    {
        characterAnimator = GetComponentInChildren<CharacterAnimator>(); // Find animator component on children
    }
    
    // Hook called when head item changes
    void OnHeadItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.headItemNumber = newValue; // Update head sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Hook called when body item changes
    void OnBodyItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.bodyItemNumber = newValue; // Update body sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Hook called when hair item changes
    void OnHairItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.hairItemNumber = newValue; // Update hair sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Hook called when torso item changes
    void OnTorsoItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.torsoItemNumber = newValue; // Update torso sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Hook called when legs item changes
    void OnLegsItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.legsItemNumber = newValue; // Update legs sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Helper method to update all visual elements at once
    public void ApplyEquipmentToAnimator()
    {
        if (characterAnimator != null) // Check if animator exists
        {
            characterAnimator.headItemNumber = headItemNumber; // Set head sprite
            characterAnimator.bodyItemNumber = bodyItemNumber; // Set body sprite
            characterAnimator.hairItemNumber = hairItemNumber; // Set hair sprite
            characterAnimator.torsoItemNumber = torsoItemNumber; // Set torso sprite
            characterAnimator.legsItemNumber = legsItemNumber; // Set legs sprite
            characterAnimator.RefreshCurrentFrame(); // Refresh visual appearance
        }
    }
    
    // Called when this object appears on a client
    public override void OnStartClient()
    {
        base.OnStartClient(); // Call parent implementation
        
        ApplyEquipmentToAnimator(); // Apply all equipment visuals
    }
}