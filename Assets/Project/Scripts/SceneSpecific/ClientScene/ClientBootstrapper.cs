using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBootstrapper : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(WaitForManagersThenProceed());
    }

    private IEnumerator WaitForManagersThenProceed()
    {
        // Wait until singletons exist
        yield return new WaitUntil(() =>
            LobbySceneManager.Instance != null &&
            CustomNetworkManager.singleton != null
        );

        Debug.Log("✅ LobbySceneManager and CustomNetworkManager are loaded.");

        SceneManager.LoadScene("LoginScene");
    }
}
