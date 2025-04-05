using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;

// This class manages user registration using Firebase Authentication
public class RegisterManager : MonoBehaviour
{
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;
    public TextMeshProUGUI messageText;
    public Button registerButton;
    private FirebaseAuth auth; // Firebase authentication reference


    private async void Start()
    {
        try
        {
            // Log dependency check
            Debug.Log("Checking Firebase dependencies...");
            
            // Check and fix Firebase dependencies asynchronously
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            
            // If dependencies are ready
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase dependencies are available.");
                // Get the default Firebase app instance
                FirebaseApp app = FirebaseApp.DefaultInstance;
                // Get the FirebaseAuth instance
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase Authentication initialized.");
                // Notify user Firebase is ready
                messageText.text = "Firebase initialized successfully!";
            }
            else
            {
                // Handle missing or broken dependencies
                Debug.LogError($"Firebase dependencies error: {dependencyStatus}");
                messageText.text = "Firebase initialization failed: " + dependencyStatus.ToString();
            }
        }
        catch (Exception e)
        {
            // Handle general exceptions
            Debug.LogError($"Error initializing Firebase: {e.Message}");
            messageText.text = "Firebase error: " + e.Message;
        }
    }

    // Called when the user presses the register button
    public void RegisterNewUser()
        {
            // Get email and password input values, trimmed
            string email = emailInputField.text.Trim();
            string password = passwordInputField.text.Trim();
            
            // Check for empty input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                messageText.text = "Email and Password cannot be empty.";
                return;
            }

            // Show progress message
            messageText.text = "Registering user...";

            // Start Firebase user creation
            auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                // Handle canceled registration
                if (task.IsCanceled)
                {
                    messageText.text = "Registration canceled.";
                    Debug.LogError("Registration canceled.");
                    return;
                }

                // Handle errors during registration
                if (task.IsFaulted)
                {
                    messageText.text = "Registration failed.";
                    Debug.LogError("Registration failed.");
                    
                    // Go through each exception in the task
                    foreach (var exception in task.Exception.Flatten().InnerExceptions)
                    {
                        // Try to cast to FirebaseException to get the error code
                        FirebaseException firebaseEx = exception as FirebaseException;
                        if (firebaseEx != null)
                        {
                            // Get the error code and show a meaningful message
                            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                            Debug.LogError($"Firebase Registration Error Code: {errorCode}");
                            messageText.text = $"Registration failed: {errorCode.ToString()} - {firebaseEx.Message}";
                        }
                        else
                        {
                            // Fallback message if not a FirebaseException
                            messageText.text = "Registration failed: " + exception.Message;
                        }
                    }
                    return;
                }

                // Registration successful, get the new user
                FirebaseUser newUser = task.Result.User;

                // Show success message
                messageText.text = "Registration successful! You can now log in.";
                Debug.Log($"Registration successful! User: {newUser.Email}, UID: {newUser.UserId}");

                // OPTIONAL: Sign out immediately to prevent auto-login effect
                auth.SignOut(); // <-- THIS LINE LOGS THEM OUT RIGHT AFTER REGISTRATION
            });
        }
    }
