using UnityEngine;

[CreateAssetMenu(fileName = "BagList", menuName = "Items/Bag List")]
public class SO_BagData : ScriptableObject
{
    [System.Serializable]
    public class BagData
    {
        public int bagId;
        public string bagName;
        public int maxSlots;
    }
    
    public BagData[] bags;
    
    public BagData GetBagById(int bagId)
    {
        return System.Array.Find(bags, bag => bag.bagId == bagId);
    }
}
