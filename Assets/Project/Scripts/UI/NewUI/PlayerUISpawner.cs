using UnityEngine;
using Mirror;

public class PlayerUISpawner : NetworkBehaviour
{
   [SerializeField]  GameObject uiPrefab;
   private static GameObject spawnedUI;
   
   public override void OnStartLocalPlayer()
   {
       if (uiPrefab != null && spawnedUI == null)
       {
           spawnedUI = Instantiate(uiPrefab);
           DontDestroyOnLoad(spawnedUI);
       }
   }
}