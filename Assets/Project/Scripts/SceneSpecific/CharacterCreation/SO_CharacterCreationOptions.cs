using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCreationOptions", menuName = "Character/Character Creation Options")]
public class CharacterCreationOptionsData : ScriptableObject
{
   
    [System.Serializable]
    public class ClassOption
    {
        public string className;
        public string description;
        public int defaultHeadItem;
        public int defaultBodyItem;
        public int defaultHairItem;
        public int defaultTorsoItem;
        public int defaultLegsItem;

    }

    [Header("Starting Scene")]
    public LobbyScene startingSceneName = LobbyScene.CharacterSelectionScene;

    [Header("Starting Inventory")]
    public int defaultBagId = 1001; // Basic Backpack
    
    [Header("Available Classes")]
    public ClassOption[] availableClasses = new ClassOption[]
    {
        new ClassOption { 
            className = "Warrior", 
            description = "Strong melee fighter with high physical damage and defense.",
            defaultHeadItem = 20001,
            defaultBodyItem = 10001,
            defaultHairItem = 30001,
            defaultTorsoItem = 40001,
            defaultLegsItem = 50001
        },
        new ClassOption { 
            className = "Magician", 
            description = "Spellcaster with powerful magic attacks and support abilities.",
            defaultHeadItem = 20001,
            defaultBodyItem = 10001,
            defaultHairItem = 30002,
            defaultTorsoItem = 40002,
            defaultLegsItem = 50001
        },
        new ClassOption { 
            className = "Luminary", 
            description = "Blessed hero with balanced abilities and unique powers.",
            defaultHeadItem = 20002,
            defaultBodyItem = 10001,
            defaultHairItem = 30001,
            defaultTorsoItem = 40001,
            defaultLegsItem = 50002
        },
        new ClassOption { 
            className = "Hunter", 
            description = "Ranged specialist with high accuracy and tracking skills.",
            defaultHeadItem = 20001,
            defaultBodyItem = 10002,
            defaultHairItem = 30002,
            defaultTorsoItem = 40002,
            defaultLegsItem = 50002
        }
    };
    
    [Header("Body Options (Skin Colors)")]
    public int[] bodyOptions = new int[] { 10001, 10002 };
    
    [Header("Head Options")]
    public int[] headOptions = new int[] { 20001, 20002 };
    
    [Header("Hair Options")]
    public int[] hairOptions = new int[] { 30001, 30002 };
    
    [Header("Torso Options (Shirts/Armor)")]
    public int[] torsoOptions = new int[] { 40001, 40002 };
    
    [Header("Legs Options (Pants/Skirts)")]
    public int[] legsOptions = new int[] { 50001, 50002 };
}