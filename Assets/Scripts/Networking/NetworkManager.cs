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
        // Ensure proper cleanup when the component is destroyed
        CleanupNetworkManager();
    }
    
    void OnApplicationQuit()
    {
        // Ensure proper cleanup when the application quits
        CleanupNetworkManager();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // Clean up when the application is paused (mobile/editor)
        if (pauseStatus)
        {
            CleanupNetworkManager();
        }
    }
    
    private void CleanupNetworkManager()
    {
        if (networkManager != null)
        {
            // Unsubscribe from events
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnServerStarted -= OnServerStarted;
            
            // Force shutdown if still running
            if (networkManager.IsClient || networkManager.IsHost || networkManager.IsServer)
            {
                Debug.Log("NetworkManager: Force shutting down network connection during cleanup");
                networkManager.Shutdown();
            }
        }
        
        // Clear tracking collections
        spawnedClients.Clear();
        activePlayers.Clear();
        joinCode = null; // Clear join code during cleanup
    }
    
    private async System.Threading.Tasks.Task ForceNetworkReset()
    {
        Debug.Log("NetworkManager: Starting force network reset...");
        
        // Step 1: Force shutdown if running
        if (networkManager != null)
        {
            if (networkManager.IsClient || networkManager.IsHost || networkManager.IsServer)
            {
                Debug.Log("NetworkManager: Force shutting down existing connection");
                networkManager.Shutdown();
                
                // Wait for shutdown with timeout
                float timeout = 3f;
                float elapsed = 0f;
                while ((networkManager.IsClient || networkManager.IsHost || networkManager.IsServer) && elapsed < timeout)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    elapsed += 0.1f;
                }
            }
            
            // Step 2: Wait for shutdown to complete
            await System.Threading.Tasks.Task.Delay(500);
            
            // Step 3: Force component recreation if still not clean
            if (networkManager.IsClient || networkManager.IsHost || networkManager.IsServer)
            {
                Debug.LogError("NetworkManager: Shutdown failed, forcing component recreation");
                
                // Store the old component
                var oldNetworkManager = networkManager;
                
                // Create new component
                networkManager = gameObject.AddComponent<Unity.Netcode.NetworkManager>();
                
                // Copy important settings from old component
                if (oldNetworkManager != null)
                {
                    networkManager.NetworkConfig = oldNetworkManager.NetworkConfig;
                }
                
                // Destroy old component
                DestroyImmediate(oldNetworkManager);
                
                // Re-subscribe to events
                networkManager.OnClientConnectedCallback += OnClientConnected;
                networkManager.OnClientDisconnectCallback += OnClientDisconnected;
                networkManager.OnServerStarted += OnServerStarted;
                
                await System.Threading.Tasks.Task.Delay(1000); // Give time for recreation
            }
        }
        
        // Step 4: Clear all tracking data
        spawnedClients.Clear();
        activePlayers.Clear();
        currentSpawnIndex = 0;
        joinCode = null; // Clear join code during reset
        
        Debug.Log("NetworkManager: Force network reset completed");
    }
    
    // UI Button Methods
    public async void StartHost()
    {
        // Force complete reset before starting
        await ForceNetworkReset();
        
        await CreateRelay();
    }
    
    public async void StartClient()
    {
        if (joinCodeInput == null)
        {
            Debug.LogError("Client: Join code input field is not assigned!");
            return;
        }
        
        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogError("Please enter a join code!");
            return;
        }
        
        Debug.Log("Client: Starting client connection process...");
        DebugNetworkConfiguration();
        
        // Force complete reset before starting
        await ForceNetworkReset();
        
        string codeToJoin = joinCodeInput.text.Trim();
        Debug.Log($"Client: Using join code: '{codeToJoin}'");
        
        await JoinRelay(codeToJoin);
    }
    
    public void Disconnect()
    {
        Debug.Log("NetworkManager: Manual disconnect requested");
        
        if (networkManager != null)
        {
            if (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer)
            {
                networkManager.Shutdown();
            }
        }
        
        // Clear tracking collections
        spawnedClients.Clear();
        activePlayers.Clear();
        
        // Clear join code when disconnecting
        joinCode = null;
        
        UpdateUI();
    }
    
    // Manual reset button for debugging
    public async void ForceReset()
    {
        Debug.Log("NetworkManager: Manual force reset requested");
        await ForceNetworkReset();
        UpdateUI();
    }
    
    // Debug method to check NetworkManager configuration
    public void DebugNetworkConfiguration()
    {
        Debug.Log("=== NetworkManager Configuration Debug ===");
        Debug.Log($"NetworkManager component: {(networkManager != null ? "Found" : "Missing")}");
        
        if (networkManager != null)
        {
            Debug.Log($"NetworkConfig: {(networkManager.NetworkConfig != null ? "Present" : "Missing")}");
            Debug.Log($"IsClient: {networkManager.IsClient}");
            Debug.Log($"IsHost: {networkManager.IsHost}");
            Debug.Log($"IsServer: {networkManager.IsServer}");
            Debug.Log($"IsConnectedClient: {networkManager.IsConnectedClient}");
            
            var transport = networkManager.GetComponent<UnityTransport>();
            Debug.Log($"UnityTransport: {(transport != null ? "Found" : "Missing")}");
        }
        
        Debug.Log($"Player Prefab: {(playerPrefab != null ? "Assigned" : "Missing")}");
        Debug.Log($"Spawn Points: {spawnPoints.Length}");
        Debug.Log($"Max Players: {maxPlayers}");
        Debug.Log("=== End Debug ===");
    }
    
    // Relay Methods
    private async Task CreateRelay()
    {
        try
        {
            Debug.Log("Host: Creating relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            Debug.Log($"Host: Relay allocation created successfully");
            
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Host: Join code generated: {joinCode}");
            
            // Configure NetworkManager to use Relay (Host)
            var transport = networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("Host: UnityTransport component not found on NetworkManager!");
                return;
            }
            
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            Debug.Log("Host: Relay data configured, starting host...");
            
            // Start the host
            networkManager.StartHost();
            Debug.Log("Host: StartHost called successfully");
            
            // Display join code
            if (joinCodeText != null)
            {
                joinCodeText.text = $"Join Code: {joinCode}";
            }
            
            // Try to copy to clipboard if possible
            try
            {
                GUIUtility.systemCopyBuffer = joinCode;
                Debug.Log("Host: Join code copied to clipboard");
            }
            catch
            {
                Debug.LogWarning("Host: Could not copy join code to clipboard");
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Host: Failed to create relay: {e.Message}");
            Debug.LogError($"Host: Stack trace: {e.StackTrace}");
        }
    }
    
    private async Task JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Client: Attempting to join relay with code: {joinCode}");
            
            // Validate join code
            if (string.IsNullOrEmpty(joinCode) || joinCode.Length < 6)
            {
                Debug.LogError($"Client: Invalid join code: '{joinCode}'");
                return;
            }
            
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"Client: Successfully joined relay allocation");
            Debug.Log($"Client: Relay server - IP: {allocation.RelayServer.IpV4}, Port: {allocation.RelayServer.Port}");
            
            // Configure NetworkManager to use Relay (Client)
            var transport = networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("Client: UnityTransport component not found on NetworkManager!");
                return;
            }
            
            Debug.Log($"Client: Configuring UnityTransport with relay data...");
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );
            
            Debug.Log($"Client: Relay data configured successfully");
            Debug.Log($"Client: Starting client connection...");
            
            // Verify NetworkManager is ready
            if (networkManager.NetworkConfig == null)
            {
                Debug.LogError("Client: NetworkConfig is null - NetworkManager not properly configured!");
                return;
            }
            
            Debug.Log($"Client: NetworkConfig verified - Transport: {networkManager.NetworkConfig.NetworkTransport}");
            
            try
            {
                networkManager.StartClient();
                Debug.Log("Client: StartClient called successfully");
                Debug.Log($"Client: NetworkManager state after StartClient - IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient}");
                StartCoroutine(MonitorClientConnection());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Client: Exception during StartClient: {e.Message}");
                Debug.LogError($"Client: Stack trace: {e.StackTrace}");
            }
            
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client: Failed to join relay: {e.Message}");
            Debug.LogError($"Client: Stack trace: {e.StackTrace}");
            
            // Check if it's a specific Relay service error
            if (e.Message.Contains("404"))
            {
                Debug.LogError("Client: Join code not found or expired. Please check the code and try again.");
            }
            else if (e.Message.Contains("timeout"))
            {
                Debug.LogError("Client: Connection to Relay service timed out. Please check your internet connection.");
            }
        }
    }
    
    // Network Event Handlers
    private void OnServerStarted()
    {
        Debug.Log("NetworkManager: OnServerStarted called");
        spawnedClients.Clear(); // Clear spawn tracking when server starts
        activePlayers.Clear(); // Clear active players tracking
        
        // Spawn ActionManager if it exists in the scene
        ActionManager actionManager = FindFirstObjectByType<ActionManager>();
        if (actionManager != null && actionManager.GetComponent<NetworkObject>() != null)
        {
            NetworkObject actionManagerNetworkObject = actionManager.GetComponent<NetworkObject>();
            if (!actionManagerNetworkObject.IsSpawned)
            {
                actionManagerNetworkObject.Spawn();
                Debug.Log("NetworkManager: ActionManager spawned on network");
            }
        }
        else
        {
            Debug.LogWarning("NetworkManager: ActionManager not found or missing NetworkObject component");
        }
        
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
    

    
    private System.Collections.IEnumerator MonitorClientConnection()
    {
        float timeout = 15f; // Increased timeout to 15 seconds
        float elapsed = 0f;
        
        Debug.Log("Client: Starting connection monitoring...");
        Debug.Log($"Client: Initial state - IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient}");
        
        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            // Log current state every 2 seconds
            if (Mathf.FloorToInt(elapsed) % 2 == 0)
            {
                Debug.Log($"Client: Still connecting... ({elapsed:F1}s / {timeout}s) - IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient}");
            }
            
            if (networkManager.IsConnectedClient)
            {
                Debug.Log("Client: Successfully connected to host!");
                yield break;
            }
            
            // Check if client failed to start
            if (!networkManager.IsClient && elapsed > 2f)
            {
                Debug.LogError("Client: NetworkManager.IsClient is false - client may have failed to start");
                yield break;
            }
        }
        
        Debug.LogError($"Client: Connection timeout - failed to connect to host within {timeout} seconds");
        Debug.LogError($"Client: Final NetworkManager state - IsClient: {networkManager.IsClient}, IsConnectedClient: {networkManager.IsConnectedClient}");
        
        // Try to get more information about the connection state
        if (networkManager.NetworkConfig != null)
        {
            Debug.LogError($"Client: NetworkConfig - NetworkTransport: {networkManager.NetworkConfig.NetworkTransport}");
        }
        
        // Check transport state
        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            Debug.LogError($"Client: UnityTransport component found");
        }
        else
        {
            Debug.LogError($"Client: UnityTransport component missing");
        }
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
        bool isHost = networkManager.IsHost;
        
        // Show join code text only when host is running and has a join code
        if (joinCodeText != null)
        {
            joinCodeText.gameObject.SetActive(isHost && !string.IsNullOrEmpty(joinCode));
        }
        
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
        // Find all BottomStatusBarUI components and notify them that the game has started
        BottomStatusBarUI[] statusBars = FindObjectsByType<BottomStatusBarUI>(FindObjectsSortMode.None);
        foreach (BottomStatusBarUI statusBar in statusBars)
        {
            if (statusBar != null)
            {
                statusBar.OnGameStarted();
            }
        }
    }
} 