using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace MyGame.Client
{
    public class FirebaseAuthenticator : NetworkAuthenticator
    {
        public string idToken;
        
        // Optional: Reference to a UI text element to show the authentication status
        public TMP_Text authStatusText;
        
        // Optional: Unity Event that gets triggered when authentication is successful
        public UnityEvent<string> onAuthenticationSuccess;

        public override void OnStartClient()
        {
            // Register handler to receive auth messages
            NetworkClient.RegisterHandler<AuthenticationResponseMessage>(OnAuthResponse, false);
        }

        public override void OnClientAuthenticate()
        {
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("❌ ID Token is null or empty! Cannot authenticate.");
                if (authStatusText != null)
                    authStatusText.text = "Authentication failed: No token available";
                NetworkClient.Disconnect();
                return;
            }

            Debug.Log("🟡 Sending token to server...");
            if (authStatusText != null)
                authStatusText.text = "Sending authentication token to server...";

            var msg = new AuthenticationRequestMessage
            {
                token = idToken
            };

            NetworkClient.Send(msg);
        }

        void OnAuthResponse(AuthenticationResponseMessage msg)
        {
            if (msg.success)
            {
                string userId = msg.userId;
                Debug.Log($"✅ Authentication verified by server for user: {userId}");
                
                if (authStatusText != null)
                    authStatusText.text = $"Authentication successful! User ID: {userId}";
                
                // Trigger the success event with the user ID
                onAuthenticationSuccess?.Invoke(userId);
                
                ClientAccept();
            }
            else
            {
                Debug.LogError("❌ Server rejected authentication");
                
                if (authStatusText != null)
                    authStatusText.text = "Authentication failed: Server rejected token";
                
                ClientReject();
            }
        }
    }
}