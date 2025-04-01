using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    // SyncVar makes this variable synchronized across the network
    [SyncVar] private Vector3 serverPosition;
    
    private void Update()
    {
        if (isLocalPlayer)
        {
            // Only process input for local player
            HandleMovement();
        }
        else
        {
            // For non-local players, smoothly move towards server position
            transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * 10f);
        }
    }
    
    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        
        if (movement != Vector3.zero)
        {
            // Instead of moving directly, send command to server
            CmdMove(transform.position + movement);
        }
    }
    
    // Commands run on the server, called by client
    [Command]
    private void CmdMove(Vector3 newPosition)
    {
        // Validate move on server (add any game-specific validation here)
        // For example, check if position is within bounds, not colliding, etc.
        bool isValidMove = true; // Replace with actual validation
        
        if (isValidMove)
        {
            // Update server position (authoritative)
            serverPosition = newPosition;
            
            // Also update transform immediately (smoother on server)
            transform.position = newPosition;
            
            // Inform all clients (including the one who sent this command)
            RpcUpdatePosition(newPosition);
        }
    }
    
    // RPC calls run on all clients
    [ClientRpc]
    private void RpcUpdatePosition(Vector3 newPosition)
    {
        // Update local position for this player on all clients
        // The if check prevents applying this to the local player who already moved
        if (!isLocalPlayer)
        {
            serverPosition = newPosition;
        }
    }
    
    // Called on the server when a client connects
    public override void OnStartServer()
    {
        base.OnStartServer();
        serverPosition = transform.position;
    }
    
    // Called on the client when this player object is created
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Any client-specific initialization
        if (isLocalPlayer)
        {
            Debug.Log("Local player spawned!");
            // E.g., activate camera, UI, etc.
        }
    }
}