using Mirror;
using UnityEngine;

namespace MyGame.Client
{
    public class FirebaseAuthenticator : NetworkAuthenticator
    {
        public string idToken;

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
                NetworkClient.Disconnect();
                return;
            }

            Debug.Log("🟡 Sending token to server...");

            var msg = new AuthenticationRequestMessage
            {
                token = idToken
            };

            NetworkClient.Send(msg);
        }


        void OnAuthResponse(AuthenticationResponseMessage msg)
        {
            if (msg.success)
                ClientAccept();
            else
                ClientReject();
        }
    }
}

