using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playerInfoText;
    
    private NetworkGameManager networkManager;
    
    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkGameManager>();
        
        // Set up button listeners
        if (hostButton != null)
            hostButton.onClick.AddListener(() => networkManager.StartHost());
        
        if (clientButton != null)
            clientButton.onClick.AddListener(() => networkManager.StartClient());
        
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(() => networkManager.Disconnect());
        
        UpdateUI();
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return;
        
        bool isConnected = Unity.Netcode.NetworkManager.Singleton.IsClient || 
                          Unity.Netcode.NetworkManager.Singleton.IsHost;
        
        // Update button visibility
        if (hostButton != null) hostButton.gameObject.SetActive(!isConnected);
        if (clientButton != null) clientButton.gameObject.SetActive(!isConnected);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(isConnected);
        
        // Update status text
        if (statusText != null)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsHost)
            {
                statusText.text = "Status: Host (Player 1)";
            }
            else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
            {
                statusText.text = "Status: Client (Player 2)";
            }
            else
            {
                statusText.text = "Status: Disconnected";
            }
        }
        
        // Update player info
        if (playerInfoText != null)
        {
            int playerNumber = NetworkGameManager.GetLocalPlayerNumber();
            if (playerNumber > 0)
            {
                playerInfoText.text = $"You are Player {playerNumber}";
            }
            else
            {
                playerInfoText.text = "Not connected";
            }
        }
    }
} 