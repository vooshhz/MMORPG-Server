using Mirror;
using UnityEngine;

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

            bool isValid = FirebaseTokenVerifier.Verify(idToken, out string uid);

            Debug.Log($"🔎 Verification result: {isValid}, UID: {uid}");

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



    }
}
