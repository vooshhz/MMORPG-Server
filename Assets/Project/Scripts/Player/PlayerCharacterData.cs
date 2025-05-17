using UnityEngine;
using Mirror;

public class PlayerCharacterData : NetworkBehaviour
{
    [Header("Character ID")]
    [SyncVar]
    public string characterId;
    
    [Header("Equipment")]
    [SyncVar(hook = nameof(OnHeadItemChanged))]
    public int headItemNumber;
    [SyncVar(hook = nameof(OnBodyItemChanged))]
    public int bodyItemNumber;
    [SyncVar(hook = nameof(OnHairItemChanged))]
    public int hairItemNumber;
    [SyncVar(hook = nameof(OnTorsoItemChanged))]
    public int torsoItemNumber;
    [SyncVar(hook = nameof(OnLegsItemChanged))]
    public int legsItemNumber;
    
    [Header("Info")]
    [SyncVar]
    public string characterName;
    [SyncVar]
    public string characterClass;
    [SyncVar]
    public int experience;
    [SyncVar]
    public int level;
    
    [Header("Location")]
    [SyncVar]
    public string sceneName;
    public float x;
    public float y;
    public float z;
    
    // Reference to the character animator
    private CharacterAnimator characterAnimator;
    
    private void Awake()
    {
        // Find the CharacterAnimator (could be on this GameObject or a child)
        characterAnimator = GetComponentInChildren<CharacterAnimator>();
    }
    
    // Hooks for equipment changes to update visual appearance
    void OnHeadItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null)
        {
            characterAnimator.headItemNumber = newValue;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    void OnBodyItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null)
        {
            characterAnimator.bodyItemNumber = newValue;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    void OnHairItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null)
        {
            characterAnimator.hairItemNumber = newValue;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    void OnTorsoItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null)
        {
            characterAnimator.torsoItemNumber = newValue;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    void OnLegsItemChanged(int oldValue, int newValue)
    {
        if (characterAnimator != null)
        {
            characterAnimator.legsItemNumber = newValue;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    // Helper method to apply all equipment to the character animator
    public void ApplyEquipmentToAnimator()
    {
        if (characterAnimator != null)
        {
            characterAnimator.headItemNumber = headItemNumber;
            characterAnimator.bodyItemNumber = bodyItemNumber;
            characterAnimator.hairItemNumber = hairItemNumber;
            characterAnimator.torsoItemNumber = torsoItemNumber;
            characterAnimator.legsItemNumber = legsItemNumber;
            characterAnimator.RefreshCurrentFrame();
        }
    }
    
    // Called when this object becomes visible to a client
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Apply equipment to character model when it spawns on the client
        ApplyEquipmentToAnimator();
    }
}