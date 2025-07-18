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
            Debug.Log($"Authentication successful. Player ID: {AuthenticationService.Instance.PlayerId}");
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
            Debug.Log("Force disconnecting before starting host...");
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
            Debug.Log("Force disconnecting before starting client...");
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
        Debug.Log($"Client: Input field contains: '{codeToJoin}'");
        Debug.Log($"Client: Input field length: {codeToJoin.Length}");
        
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
            Debug.Log("Host: Creating relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            Debug.Log($"Host: Allocation created with ID: {allocation.AllocationId}");
            
            Debug.Log("Host: Getting join code...");
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Host: Join code received: {joinCode}");
            
            // Configure NetworkManager to use Relay (Host)
            var transport = networkManager.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            Debug.Log($"Host: Connection data length: {allocation.ConnectionData.Length}");
            Debug.Log($"Host: Connection data (first 10 bytes): {BitConverter.ToString(allocation.ConnectionData.Take(10).ToArray())}");
            
            Debug.Log($"Host: Relay configured - IP: {allocation.RelayServer.IpV4}, Port: {allocation.RelayServer.Port}");
            Debug.Log($"Host: Allocation ID: {allocation.AllocationId}");
            Debug.Log($"Host: Key length: {allocation.Key.Length}, Connection data length: {allocation.ConnectionData.Length}");
            Debug.Log($"Host: IsHost flag set to: true");
            Debug.Log($"Host: Join code for this allocation: {joinCode}");
            Debug.Log($"Host: Allocation ID: {allocation.AllocationId}");
            Debug.Log($"Host: Key length: {allocation.Key.Length}, Connection data length: {allocation.ConnectionData.Length}");
            
            Debug.Log("Host: Relay server data configured, starting host...");
            
            // Start the host
            networkManager.StartHost();
            
            // Display join code
            if (joinCodeText != null)
            {
                joinCodeText.text = $"Join Code: {joinCode}";
            }
            
            Debug.Log($"=== HOST JOIN CODE === {joinCode} ===");
            Debug.Log($"Copy this exact code: {joinCode}");
            Debug.Log($"Code length: {joinCode.Length} characters");
            
            // Try to copy to clipboard if possible
            try
            {
                GUIUtility.systemCopyBuffer = joinCode;
                Debug.Log("Join code copied to clipboard!");
            }
            catch
            {
                Debug.Log("Could not copy to clipboard automatically");
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create relay: {e.Message}");
            Debug.LogError($"Exception details: {e}");
        }
    }
    
    private async Task JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Client: Attempting to join relay with code: '{joinCode}'");
            Debug.Log($"Client: Join code length: {joinCode.Length} characters");
            
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            Debug.Log($"Client: Successfully joined relay allocation");
            Debug.Log($"Client: Relay IP: {allocation.RelayServer.IpV4}, Port: {allocation.RelayServer.Port}");
            Debug.Log($"Client: Allocation ID: {allocation.AllocationId}");
            Debug.Log($"Client: Join code used: {joinCode}");
            
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
            
            Debug.Log($"Client: Connection data length: {allocation.ConnectionData.Length}");
            Debug.Log($"Client: Connection data (first 10 bytes): {BitConverter.ToString(allocation.ConnectionData.Take(10).ToArray())}");
            Debug.Log($"Client: Host connection data length: {allocation.HostConnectionData.Length}");
            Debug.Log($"Client: Host connection data (first 10 bytes): {BitConverter.ToString(allocation.HostConnectionData.Take(10).ToArray())}");
            
            Debug.Log($"Client: IsHost flag set to: false");
            
            Debug.Log("Client: Relay server data configured, starting client...");
            
            // Check network manager state before starting
            Debug.Log($"Client: NetworkManager state before StartClient - IsClient: {networkManager.IsClient}, IsHost: {networkManager.IsHost}, IsServer: {networkManager.IsServer}");
            Debug.Log($"Client: NetworkManager is listening: {networkManager.IsListening}");
            
            // Start the client
            Debug.Log("Client: About to call networkManager.StartClient()");
            try
            {
                networkManager.StartClient();
                Debug.Log("Client: networkManager.StartClient() completed");
                
                // Check state after StartClient
                Debug.Log($"Client: After StartClient - IsClient: {networkManager.IsClient}, IsHost: {networkManager.IsHost}, IsServer: {networkManager.IsServer}");
                Debug.Log($"Client: After StartClient - IsListening: {networkManager.IsListening}");
                Debug.Log($"Client: After StartClient - IsConnectedClient: {networkManager.IsConnectedClient}");
                
                // Start monitoring connection status
                StartCoroutine(MonitorClientConnection());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Client: Exception during StartClient: {e.Message}");
                Debug.LogError($"Client: Exception details: {e}");
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client: Failed to join relay: {e.Message}");
            Debug.LogError($"Client: Join code used: '{joinCode}'");
            Debug.LogError($"Client: Exception details: {e}");
        }
    }
    
    // Network Event Handlers
    private void OnServerStarted()
    {
        Debug.Log("Server started successfully!");
        spawnedClients.Clear(); // Clear spawn tracking when server starts
        activePlayers.Clear(); // Clear active players tracking
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected with ID: {clientId}");
        Debug.Log($"IsServer: {networkManager.IsServer}, IsHost: {networkManager.IsHost}, IsClient: {networkManager.IsClient}");
        
        // If this is the server, spawn a player for the connected client
        if (networkManager.IsServer)
        {
            Debug.Log($"Server spawning player for client {clientId}");
            // Check if we already spawned for this client
            if (!spawnedClients.Contains(clientId))
            {
                // Additional check: make sure client doesn't already have a player object
                if (!networkManager.ConnectedClients[clientId].PlayerObject)
                {
                    SpawnPlayer(clientId);
                    spawnedClients.Add(clientId);
                }
                else
                {
                    Debug.Log($"Client {clientId} already has a player object");
                }
            }
            else
            {
                Debug.Log($"Already spawned for client {clientId}");
            }
        }
        else
        {
            Debug.Log("This is not the server, so not spawning player");
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected with ID: {clientId}");
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
        Debug.Log("Client: Starting connection monitoring...");
        
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            Debug.Log($"Client: Connection check {elapsed}s - IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient}, ConnectedClients: {networkManager.ConnectedClients.Count}");
            
            if (networkManager.IsConnectedClient)
            {
                Debug.Log("Client: Successfully connected to host!");
                yield break;
            }
        }
        
        Debug.LogError("Client: Connection timeout - failed to connect to host within 10 seconds");
    }
    
    // Spawn a player for the given client
    private void SpawnPlayer(ulong clientId)
    {
        Debug.Log($"SpawnPlayer called for client {clientId}");
        
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
        
        Debug.Log($"Spawning player object for client {clientId} at position {spawnPosition}");
        
        // Set ownership to the client
        networkObject.SpawnAsPlayerObject(clientId);
        
        // Track the active player
        activePlayers[clientId] = networkObject;
        
        Debug.Log($"Player spawned successfully for client {clientId}");
        
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
} 