using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace MyGame.Server
{
   public class FirebaseAuthenticator : NetworkAuthenticator
    {
        // Define a timespan for authentication timeout (e.g., 15 seconds)
    private readonly Dictionary<NetworkConnectionToClient, float> pendingAuthentications = new Dictionary<NetworkConnectionToClient, float>();
    private const float AUTH_TIMEOUT_SECONDS = 15f;
        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<AuthenticationRequestMessage>(OnAuthRequest, false);
                // Add update callback for timeout handling
            if (NetworkServer.active)
            {
                StartCoroutine(CheckAuthenticationTimeouts());
            }
        }

        private IEnumerator CheckAuthenticationTimeouts()
        {
            while (NetworkServer.active)
            {
                foreach (var conn in pendingAuthentications.Keys.ToList())
                {
                    pendingAuthentications[conn] += Time.deltaTime;
                    
                    if (pendingAuthentications[conn] >= AUTH_TIMEOUT_SECONDS)
                    {
                        Debug.LogWarning($"Authentication timed out for connection {conn.connectionId}");
                        pendingAuthentications.Remove(conn);
                        ServerReject(conn);
                    }
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        public override void OnServerAuthenticate(NetworkConnectionToClient conn)
        {
            // Let OnAuthRequest handle it
        }

        void OnAuthRequest(NetworkConnectionToClient conn, AuthenticationRequestMessage msg)
        {
            // Add to pending authentications
            pendingAuthentications[conn] = 0f;

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
            
            pendingAuthentications.Remove(conn);
        }
        
        // Helper method to call FirebaseTokenVerifier
        private bool VerifyToken(string idToken, out string uid)
        {
            // Use the FirebaseTokenVerifier in the same namespace
            return FirebaseTokenVerifier.Verify(idToken, out uid);
        }
    }
}