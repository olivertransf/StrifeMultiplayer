using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

public class NetworkGameManager : MonoBehaviour
{
    [Header("Player Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Color[] playerColors = { Color.red, Color.blue };
    

    
    [Header("UI References")]
    [SerializeField] private GameObject hostButton;
    [SerializeField] private GameObject clientButton;
    [SerializeField] private GameObject disconnectButton;
    [SerializeField] private TMPro.TextMeshProUGUI joinCodeText;
    [SerializeField] private TMPro.TMP_InputField joinCodeInput;
    
    [Header("Relay Settings")]
    [SerializeField] private int maxPlayers = 2;
    
    private Unity.Netcode.NetworkManager networkManager;
    private int currentSpawnIndex = 0;
    private HashSet<ulong> spawnedClients = new HashSet<ulong>();
    private Dictionary<ulong, NetworkObject> activePlayers = new Dictionary<ulong, NetworkObject>();
    private string joinCode;
    
    async void Start()
    {
        networkManager = GetComponent<Unity.Netcode.NetworkManager>();
        
        // Subscribe to network events
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.OnServerStarted += OnServerStarted;
        
        // Initialize Unity Services
        await InitializeUnityServices();
        
        // Show/hide UI based on connection state
        UpdateUI();
    }
    
    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            
            // Clear session token in editor to ensure fresh authentication
            #if UNITY_EDITOR
            AuthenticationService.Instance.ClearSessionToken();
            #endif
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnServerStarted -= OnServerStarted;
        }
    }
    
    // UI Button Methods
    public async void StartHost()
    {
        // Force complete shutdown and wait
        if (networkManager.IsClient || networkManager.IsHost)
        {
            networkManager.Shutdown();
            
            // Wait for shutdown to complete
            await System.Threading.Tasks.Task.Delay(1000);
        }
        
        // Reset transport completely
        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.Shutdown();
            await System.Threading.Tasks.Task.Delay(500);
        }
        
        await CreateRelay();
    }
    
    public async void StartClient()
    {
        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogError("Please enter a join code!");
            return;
        }
        
        // Force complete shutdown and wait
        if (networkManager.IsClient || networkManager.IsHost)
        {
            networkManager.Shutdown();
            
            // Wait for shutdown to complete
            await System.Threading.Tasks.Task.Delay(1000);
        }
        
        // Reset transport completely
        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.Shutdown();
            await System.Threading.Tasks.Task.Delay(500);
        }
        
        string codeToJoin = joinCodeInput.text.Trim();
        
        await JoinRelay(codeToJoin);
    }
    
    public void Disconnect()
    {
        if (networkManager.IsHost)
        {
            networkManager.Shutdown();
        }
        else if (networkManager.IsClient)
        {
            networkManager.Shutdown();
        }
        UpdateUI();
    }
    
    // Relay Methods
    private async Task CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // Configure NetworkManager to use Relay (Host)
            var transport = networkManager.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            // Start the host
            networkManager.StartHost();
            
            // Display join code
            if (joinCodeText != null)
            {
                joinCodeText.text = $"Join Code: {joinCode}";
            }
            
            // Try to copy to clipboard if possible
            try
            {
                GUIUtility.systemCopyBuffer = joinCode;
            }
            catch
            {
                // Could not copy to clipboard
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create relay: {e.Message}");
        }
    }
    
    private async Task JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            // Configure NetworkManager to use Relay (Client)
            var transport = networkManager.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );
            
            try
            {
                networkManager.StartClient();
                StartCoroutine(MonitorClientConnection());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Client: Exception during StartClient: {e.Message}");
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client: Failed to join relay: {e.Message}");
        }
    }
    
    // Network Event Handlers
    private void OnServerStarted()
    {
        Debug.Log("NetworkManager: OnServerStarted called");
        spawnedClients.Clear(); // Clear spawn tracking when server starts
        activePlayers.Clear(); // Clear active players tracking
        
        // SpinManager is now placed in the scene as a singleton, no need to spawn it dynamically
        Debug.Log("NetworkManager: SpinManager should be in scene as singleton");
        
        // Notify MoneyUI that the game has started
        NotifyGameStarted();
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // If this is the server, spawn a player for the connected client
        if (networkManager.IsServer)
        {
            // Check if we already spawned for this client
            if (!spawnedClients.Contains(clientId))
            {
                // Additional check: make sure client doesn't already have a player object
                if (!networkManager.ConnectedClients[clientId].PlayerObject)
                {
                    SpawnPlayer(clientId);
                    spawnedClients.Add(clientId);
                }
            }
        }
        
        // Notify MoneyUI that the game has started (for clients)
        NotifyGameStarted();
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        spawnedClients.Remove(clientId); // Remove from tracking
        activePlayers.Remove(clientId); // Remove from active players
        UpdateUI();
    }
    
    private void OnClientConnectionFailed(ulong clientId, int errorCode)
    {
        Debug.LogError($"Client connection failed! Client ID: {clientId}, Error Code: {errorCode}");
    }
    
    private System.Collections.IEnumerator MonitorClientConnection()
    {
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            if (networkManager.IsConnectedClient)
            {
                yield break;
            }
        }
        
        Debug.LogError("Client: Connection timeout - failed to connect to host within 10 seconds");
    }
    
    // Spawn a player for the given client
    private void SpawnPlayer(ulong clientId)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points configured!");
            return;
        }
        
        // Check if player already exists for this client
        if (activePlayers.ContainsKey(clientId))
        {
            if (activePlayers[clientId] != null)
            {
                activePlayers[clientId].Despawn();
            }
            activePlayers.Remove(clientId);
        }
        
        // Get spawn position
        Vector3 spawnPosition = spawnPoints[currentSpawnIndex % spawnPoints.Length].position;
        currentSpawnIndex++;
        
        // Spawn the player
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        
        if (networkObject == null)
        {
            Debug.LogError("Player prefab doesn't have a NetworkObject component!");
            Destroy(playerInstance);
            return;
        }
        
        // Set ownership to the client
        networkObject.SpawnAsPlayerObject(clientId);
        
        // Track the active player
        activePlayers[clientId] = networkObject;
        
        // Set player color based on client ID using RPC for network sync
        SetPlayerColorServerRpc(clientId, (int)clientId);
    }
    
    // Server RPC to set player color across the network
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerColorServerRpc(ulong clientId, int playerNumber)
    {
        // Find the player object for this client
        if (networkManager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                SetPlayerColor(client.PlayerObject.gameObject, playerNumber);
                // Notify all clients about the color change
                SetPlayerColorClientRpc(clientId, playerNumber);
            }
        }
    }
    
    // Client RPC to sync color changes
    [ClientRpc]
    private void SetPlayerColorClientRpc(ulong clientId, int playerNumber)
    {
        if (networkManager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                SetPlayerColor(client.PlayerObject.gameObject, playerNumber);
            }
        }
    }
    
    // Set player color based on player number
    private void SetPlayerColor(GameObject player, int playerNumber)
    {
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && playerNumber < playerColors.Length)
        {
            spriteRenderer.color = playerColors[playerNumber];
        }
    }
    
    // Update UI visibility based on connection state
    private void UpdateUI()
    {
        bool isConnected = networkManager.IsClient || networkManager.IsHost;
        
        if (hostButton != null) hostButton.SetActive(!isConnected);
        if (clientButton != null) clientButton.SetActive(!isConnected);
        if (disconnectButton != null) disconnectButton.SetActive(isConnected);
        
        // Show/hide join code input
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(!isConnected);
    }
    
    // Get player number (1 or 2) for the local player
    public static int GetLocalPlayerNumber()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return 0;
        
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            return 1; // Host is always player 1
        }
        else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
        {
            return 2; // Client is player 2
        }
        
        return 0;
    }
    
    private void NotifyGameStarted()
    {
        // Find all MoneyUI components and notify them that the game has started
        MoneyUI[] moneyUIs = FindObjectsOfType<MoneyUI>();
        foreach (MoneyUI moneyUI in moneyUIs)
        {
            if (moneyUI != null)
            {
                moneyUI.OnGameStarted();
            }
        }
    }
} 