using Mirror;

public struct AuthenticationRequestMessage : NetworkMessage
{
    public string token;
}

public struct AuthenticationResponseMessage : NetworkMessage
{
    public bool success;
    public string userId; // Added user ID to the response
}