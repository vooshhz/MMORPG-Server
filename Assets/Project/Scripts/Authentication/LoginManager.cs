using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using MyGame.Client;
using Mirror;
using kcp2k;
using UnityEngine.SceneManagement; // Add this for scene loading

// Login Manager handles Firebase login for users in Unity
public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TMP_Text messageText;
    private FirebaseAuth auth;
    private FirebaseUser user;
    [SerializeField] private string serverAddress = "52.204.110.199";
    [SerializeField] private ushort serverPort = 7777;
    [SerializeField] private FirebaseAuthenticator authenticator;

    private void Start()
    {   
        // Check and fix Firebase dependencies asynchronously
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            // If all dependencies are available
            if (task.Result == DependencyStatus.Available)
            {   
                // Get the default Firebase app instance
                FirebaseApp app = FirebaseApp.DefaultInstance;

                // Set the Realtime Database URL (not required for Auth, but may be for future DB use)
                app.Options.DatabaseUrl = new System.Uri("https://willowfable3-default-rtdb.firebaseio.com/");
                
                // Log Firebase project details to console (helpful for debugging)
                Debug.Log("Firebase Project ID: " + app.Options.ProjectId);
                Debug.Log("Firebase Database URL: " + app.Options.DatabaseUrl);
                Debug.Log("Firebase API Key: " + app.Options.ApiKey);
                Debug.Log("Firebase App ID: " + app.Options.AppId);

                // Assign the FirebaseAuth instance
                auth = FirebaseAuth.DefaultInstance;
                // Notify user Firebase initialized
                messageText.text = "Firebase Initialized";
                GameLogger.Log(GameLogger.LogCategory.Auth, "Authentication connected to Firebase.");
            }
            else
            {
                // Notify user that Firebase failed to initialize
                messageText.text = "Firebase failed to initialize: " + task.Result.ToString();
                Debug.LogError("Firebase failed to initialize: " + task.Result.ToString());
            }
        });

        // Assign the login button's onClick listener to trigger login
        loginButton.onClick.AddListener(LoginUser);
        
        // Get reference to the authenticator if not set in the inspector
        if (authenticator == null)
            authenticator = FindObjectOfType<FirebaseAuthenticator>();
            
        // If we have an authenticator, set up the UI connection for status messages
        if (authenticator != null)
        {
            authenticator.authStatusText = messageText;
            
            // Optional: Subscribe to the authentication success event
            authenticator.onAuthenticationSuccess.AddListener(OnServerAuthenticationSuccess);
        }
    }
    
    // Called when the server confirms successful authentication
    private void OnServerAuthenticationSuccess(string userId)
    {
        Debug.Log($"Server confirmed authentication for user ID: {userId}");
        
        // Load character selection scene
        messageText.text = "Loading character selection...";
        
        if (NetworkSceneManager.Instance != null)
        {
            NetworkSceneManager.Instance.RequestSceneChange(SceneName.CharacterSelectionScene);
        }        
    }

    // Called when login button is clicked
    public void LoginUser()
    {
        // Get email and password input
        string email = emailInput.text;
        string password = passwordInput.text;

        // Check if either field is empty
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageText.text = "Email and password cannot be empty.";
            return;
        }

        // Call Firebase to sign in using email and password
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            // Handle canceled login
            if (task.IsCanceled)
            {
                messageText.text = "Login canceled.";
                Debug.LogError("Login canceled.");
                return;
            }

            // Handle errors
            if (task.IsFaulted)
            {
                // Extract FirebaseException if present
                FirebaseException firebaseEx = task.Exception?.Flatten().InnerExceptions[0] as FirebaseException;
                // Get the AuthError code
                AuthError errorCode = firebaseEx != null ? (AuthError)firebaseEx.ErrorCode : AuthError.None;

                // Log error details
                Debug.LogError($"Login Failed: {firebaseEx?.Message}");
                Debug.LogError($"Firebase Error Code: {errorCode}");
                
                // Display error message based on the error code
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        messageText.text = "Email is required.";
                        break;
                    case AuthError.MissingPassword:
                        messageText.text = "Password is required.";
                        break;
                    case AuthError.InvalidEmail:
                        messageText.text = "Invalid email format.";
                        break;
                    case AuthError.UserNotFound:
                        messageText.text = "Account not found.";
                        break;
                    case AuthError.WrongPassword:
                        messageText.text = "Incorrect password.";
                        break;
                    default:
                        messageText.text = "Login failed: " + firebaseEx?.Message;
                        break;
                }

                // Special Unity Editor workaround for internal error
                if (Application.isEditor && firebaseEx != null && firebaseEx.Message.Contains("internal error"))
                {
                    messageText.text = "Account not found (Editor Workaround)";
                }

                return;
            }

            // If login is successful
            user = task.Result.User;
            messageText.text = "Login successful! Welcome back, " + user.Email;
            Debug.Log("Login successful: " + user.Email);

            user.TokenAsync(true).ContinueWithOnMainThread(tokenTask =>
            {
                if (tokenTask.IsCanceled || tokenTask.IsFaulted)
                {
                    Debug.LogError("Failed to get ID token: " + tokenTask.Exception);
                    messageText.text = "Failed to get authentication token.";
                    return;
                }

                string idToken = tokenTask.Result;
                Debug.Log("Got ID token: " + idToken);
                messageText.text = "Got authentication token, connecting to game server...";

                // Save the token to be picked up by FirebaseAuthenticator
                FirebaseAuthenticator authenticator = 
                    (FirebaseAuthenticator)NetworkManager.singleton.authenticator;

                authenticator.idToken = idToken;

                // Set the server info
                NetworkManager.singleton.networkAddress = serverAddress;
                if (Transport.active is KcpTransport kcp)
                {
                    kcp.Port = serverPort;
                }

                Debug.Log("[LoginManager] Starting client after Firebase login...");
                messageText.text = "Connecting to game server...";
                NetworkManager.singleton.StartClient();  // THIS kicks off Mirror connection
            });
        });
    }
}