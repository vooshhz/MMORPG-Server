using Mirror;

public struct AuthenticationRequestMessage : NetworkMessage
{
    public string token;
}

public struct AuthenticationResponseMessage : NetworkMessage
{
    public bool success;
}
