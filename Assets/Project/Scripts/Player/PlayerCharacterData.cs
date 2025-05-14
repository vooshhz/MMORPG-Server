using UnityEngine;

public class PlayerCharacterData : MonoBehaviour
{
    [Header("Character ID")]
    public string characterId;
    
    [Header("Equipment")]
    public int headItemNumber;
    public int bodyItemNumber;
    public int hairItemNumber;
    public int torsoItemNumber;
    public int legsItemNumber;
    
    [Header("Info")]
    public string characterName;
    public string characterClass;
    public int experience;
    public int level;
    
    [Header("Location")]
    public string sceneName;
    public float x;
    public float y;
    public float z;
}