using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using kcp2k;

public class ClientConnectionManager : MonoBehaviour
{
    public TMP_InputField addressInput;
    public TMP_InputField portInput;
    public Button connectButton;
    
    [SerializeField] private NetworkManager networkManager;
    
    void Start()
    {
        // Get reference to NetworkManager
        networkManager = GetComponent<NetworkManager>();
        
        // Set default values
        addressInput.text = "52.204.110.199";  // Replace with your AWS IP
        portInput.text = "7777";
        
        // Add button listener
        connectButton.onClick.AddListener(ConnectToServer);
    }
    
    void ConnectToServer()
    {
        string address = addressInput.text;
        int port = int.Parse(portInput.text);
        
        Debug.Log($"Connecting to server at {address}:{port}");
        
        // Set NetworkManager address
        networkManager.networkAddress = address;
        
        // Set port on the transport
        Transport transport = Transport.active;
        if (transport is KcpTransport kcpTransport)
        {
            kcpTransport.Port = (ushort)port;
        }
        
        // Connect to server
        networkManager.StartClient();
        
        // Optional: Disable UI after connection attempt
        addressInput.interactable = false;
        portInput.interactable = false;
        connectButton.interactable = false;
    }
    
    // Optional: Add a reconnect option if connection fails
    public void EnableReconnect()
    {
        addressInput.interactable = true;
        portInput.interactable = true;
        connectButton.interactable = true;
    }
}