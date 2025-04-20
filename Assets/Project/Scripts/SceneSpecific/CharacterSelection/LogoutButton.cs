using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Mirror;

public class LogoutButton : MonoBehaviour
{
    [SerializeField] private Button logoutButton;

    private void Start()
    {
        if (logoutButton == null)
            logoutButton = GetComponent<Button>();
            
        logoutButton.onClick.AddListener(OnLogoutClicked);
    }

    private void OnLogoutClicked()
    {
        // 1. Disconnect from the network server
        if (NetworkClient.isConnected)
        {
            NetworkClient.Disconnect();
        }
        
        // 2. Sign out from Firebase
        if (FirebaseAuth.DefaultInstance != null)
        {
            FirebaseAuth.DefaultInstance.SignOut();
        }
        
        // 3. Clear any stored player data
        if (ClientPlayerDataManager.Instance != null)
        {
            ClientPlayerDataManager.Instance.ClearAllData();
        }
        
        // 4. Return to login scene
        if (LobbySceneManager.Instance != null)
        {
            LobbySceneManager.Instance.RequestSceneTransition(LobbyScene.LoginScene);
            Debug.Log("Logged out, returning to login screen");
        }
        else
        {
            // Fallback if LobbySceneManager not available
            UnityEngine.SceneManagement.SceneManager.LoadScene(LobbyScene.LoginScene.ToString());
            Debug.Log("Logged out, returning to login screen (fallback)");
        }
    }
}