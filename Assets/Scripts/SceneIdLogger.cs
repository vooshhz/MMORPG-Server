using UnityEngine;
using Mirror;

public class SceneIdLogger : MonoBehaviour
{
    void Start() 
    {
        NetworkIdentity netId = GetComponent<NetworkIdentity>();
        if (netId != null)
        {
            Debug.Log($"Object {gameObject.name} has sceneId: {netId.sceneId.ToString("X")}");
        }
    }
}