using Mirror;
using UnityEngine;
using System;

namespace MyGame.Server
{
   public class FirebaseAuthenticator : NetworkAuthenticator
    {
        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<AuthenticationRequestMessage>(OnAuthRequest, false);
        }

        public override void OnServerAuthenticate(NetworkConnectionToClient conn)
        {
            // Let OnAuthRequest handle it
        }

        void OnAuthRequest(NetworkConnectionToClient conn, AuthenticationRequestMessage msg)
        {
            Debug.Log("🛂 Server received authentication request.");

            string idToken = msg.token;

            Debug.Log("🔍 Verifying Firebase token on server...");

            // Call the token verifier with proper namespace if needed
            string uid = null;
            bool isValid = VerifyToken(idToken, out uid);

            Debug.Log($"🔎 Verification result: {isValid}, UID: {uid}");

            // Create the authentication response message
            var authResponse = new AuthenticationResponseMessage
            {
                success = isValid,
                userId = uid  // Include the user ID in the response
            };

            // Send the response back to the client
            conn.Send(authResponse);

            if (isValid)
            {
                Debug.Log($"✅ Firebase token verified for UID: {uid}");
                conn.authenticationData = uid;
                ServerAccept(conn);
            }
            else
            {
                Debug.LogWarning("❌ Firebase token verification FAILED.");
                ServerReject(conn);
            }
        }
        
        // Helper method to call FirebaseTokenVerifier
        private bool VerifyToken(string idToken, out string uid)
        {
            // Use the FirebaseTokenVerifier in the same namespace
            return FirebaseTokenVerifier.Verify(idToken, out uid);
        }
    }
}