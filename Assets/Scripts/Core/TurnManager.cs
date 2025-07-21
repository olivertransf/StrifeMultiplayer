using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TurnManager : NetworkBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private float turnDelay = 1f; // Delay between turns
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI turnIndicatorText;
    
    // Network variables for turn management
    private NetworkVariable<int> currentTurnPlayer = new NetworkVariable<int>(1); // 1 = Host, 2 = Client
    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isGameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<int> winningPlayer = new NetworkVariable<int>(0); // 0 = no winner, 1 = Host, 2 = Client
    
    // Local variables
    private Dictionary<ulong, int> playerNumbers = new Dictionary<ulong, int>();
    private bool isMyTurn = false;
    
    public static TurnManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to network variable changes
        currentTurnPlayer.OnValueChanged += OnTurnChanged;
        isGameStarted.OnValueChanged += OnGameStartedChanged;
        isGameEnded.OnValueChanged += OnGameEndedChanged;
        winningPlayer.OnValueChanged += OnWinningPlayerChanged;
        
        // Subscribe to client connection events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        

        
        // Initialize player numbers
        InitializePlayerNumbers();
        
        // Start the game if we're the host and both players are connected
        if (IsHost)
        {
            StartCoroutine(WaitForBothPlayersAndStartGame());
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Unsubscribe from network variable changes
        currentTurnPlayer.OnValueChanged -= OnTurnChanged;
        isGameStarted.OnValueChanged -= OnGameStartedChanged;
        isGameEnded.OnValueChanged -= OnGameEndedChanged;
        winningPlayer.OnValueChanged -= OnWinningPlayerChanged;
        
        // Unsubscribe from client connection events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        

    }
    
    private void InitializePlayerNumbers()
    {
        playerNumbers.Clear();
        
        if (NetworkManager.Singleton != null)
        {
            // Host is always player 1
            if (NetworkManager.Singleton.IsHost)
            {
                playerNumbers[NetworkManager.Singleton.LocalClientId] = 1;
            }
            
            // Add all connected clients
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Key != NetworkManager.Singleton.LocalClientId)
                {
                    playerNumbers[client.Key] = 2; // Client is player 2
                }
            }
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected. Total players: {NetworkManager.Singleton.ConnectedClients.Count}");
        
        // Update player numbers when a new client connects
        InitializePlayerNumbers();
        
        // If we're the host and now have both players, wait a bit for spawning to complete
        if (IsHost && NetworkManager.Singleton.ConnectedClients.Count >= 2 && !isGameStarted.Value)
        {
            StartCoroutine(WaitForBothPlayersAndStartGame());
        }
        // If game is already started, initialize the new player's inventory
        else if (isGameStarted.Value)
        {
            InitializeNewPlayerInventory(clientId);
        }
    }
    
    /// <summary>
    /// Initialize inventory for a new player joining after game start
    /// </summary>
    private void InitializeNewPlayerInventory(ulong clientId)
    {
        if (GameInitializer.Instance == null) return;
        
        // Find the player object for this client
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                PlayerInventory inventory = client.PlayerObject.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    Debug.Log($"TurnManager: Initializing inventory for late-joining player {clientId}");
                    GameInitializer.Instance.InitializePlayerInventory(inventory);
                }
            }
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected. Total players: {NetworkManager.Singleton.ConnectedClients.Count}");
        
        // Update player numbers when a client disconnects
        InitializePlayerNumbers();
        
        // If we're the host and now have less than 2 players, stop the game
        if (IsHost && NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            StopGameServerRpc();
        }
    }
    
    private System.Collections.IEnumerator WaitForBothPlayersAndStartGame()
    {
        // Wait until we have at least 2 players (host + client)
        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            if (turnIndicatorText != null)
            {
                turnIndicatorText.text = $"Waiting for player 2 to join... ({NetworkManager.Singleton.ConnectedClients.Count}/2)";
                turnIndicatorText.color = Color.yellow;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        // Wait a bit more for player spawning to complete
        Debug.Log("TurnManager: Both players connected, waiting for spawning to complete...");
        yield return new WaitForSeconds(1f);
        
        // Check if both players have spawned
        int spawnedPlayers = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                spawnedPlayers++;
            }
        }
        
        Debug.Log($"TurnManager: {spawnedPlayers}/{NetworkManager.Singleton.ConnectedClients.Count} players have spawned");
        
        // Both players are connected and spawned, start the game
        if (!isGameStarted.Value)
        {
            StartGameServerRpc();
        }
        else
        {
            Debug.LogWarning("TurnManager: Game already started, skipping StartGameServerRpc");
        }
    }
    
    private void OnTurnChanged(int previousValue, int newValue)
    {
        UpdateTurnUI();
    }
    
    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            UpdateTurnUI();
        }
    }
    
    private void OnGameEndedChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"OnGameEndedChanged: {previousValue} -> {newValue}, Winner: {winningPlayer.Value}, IsServer: {IsServer}, IsClient: {IsClient}");
        
        if (newValue)
        {
            Debug.Log($"Game ended! Winner: Player {winningPlayer.Value}");
            UpdateTurnUI();
            
            // Show game end action popup
            ShowGameEndAction();
        }
    }
    
    private void OnWinningPlayerChanged(int previousValue, int newValue)
    {
        if (isGameEnded.Value)
        {
            Debug.Log($"Winner changed to Player {newValue}");
            UpdateTurnUI();
        }
    }
    
    private void UpdateTurnUI()
    {
        if (!isGameStarted.Value) 
        {
            // Game not started - show waiting message
            if (turnIndicatorText != null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.Count < 2)
                {
                    turnIndicatorText.text = $"Waiting for player 2 to join... ({NetworkManager.Singleton.ConnectedClients.Count}/2)";
                    turnIndicatorText.color = Color.yellow;
                }
                else
                {
                    turnIndicatorText.text = "Game not started";
                    turnIndicatorText.color = Color.gray;
                }
            }
            return;
        }
        
        if (isGameEnded.Value)
        {
            // Game ended - show winner
            if (turnIndicatorText != null)
            {
                if (winningPlayer.Value > 0)
                {
                    if (winningPlayer.Value == GetMyPlayerNumber())
                    {
                        turnIndicatorText.text = "You Won! 🎉";
                        turnIndicatorText.color = Color.green;
                    }
                    else
                    {
                        turnIndicatorText.text = $"Player {winningPlayer.Value} Won!";
                        turnIndicatorText.color = Color.red;
                    }
                }
                else
                {
                    turnIndicatorText.text = "Game Over - It's a Tie!";
                    turnIndicatorText.color = Color.yellow;
                }
            }
            return;
        }
        
        int myPlayerNumber = GetMyPlayerNumber();
        bool wasMyTurn = isMyTurn;
        isMyTurn = (currentTurnPlayer.Value == myPlayerNumber);
        
        // Debug log when turn changes
        if (wasMyTurn != isMyTurn)
        {
            Debug.Log($"TurnManager: Turn changed - Player {currentTurnPlayer.Value} (My turn: {isMyTurn}, My player number: {myPlayerNumber})");
        }
        
        // Update turn indicator text
        if (turnIndicatorText != null)
        {
            if (isMyTurn)
            {
                turnIndicatorText.text = "Your Turn!";
                turnIndicatorText.color = Color.green;
            }
            else
            {
                turnIndicatorText.text = $"Player {currentTurnPlayer.Value}'s Turn";
                turnIndicatorText.color = Color.red;
            }
        }
    }
    

    
    private System.Collections.IEnumerator EndTurnAfterDelay()
    {
        yield return new WaitForSeconds(turnDelay);
        EndTurnServerRpc();
    }
    
    private PlayerMovement FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return null;
        
        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient != null && localClient.PlayerObject != null)
        {
            return localClient.PlayerObject.GetComponent<PlayerMovement>();
        }
        
        return null;
    }
    
    private int GetMyPlayerNumber()
    {
        if (NetworkManager.Singleton == null) return 0;
        
        if (playerNumbers.TryGetValue(NetworkManager.Singleton.LocalClientId, out int playerNumber))
        {
            return playerNumber;
        }
        
        // Fallback
        return NetworkManager.Singleton.IsHost ? 1 : 2;
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        isGameStarted.Value = true;
        currentTurnPlayer.Value = 1; // Host starts first
        Debug.Log("Game started! Host goes first.");
        
        // Initialize all player inventories when the game starts
        InitializeAllPlayerInventories();
    }
    
    /// <summary>
    /// Initialize all player inventories when the game starts
    /// </summary>
    private void InitializeAllPlayerInventories()
    {
        if (GameInitializer.Instance == null)
        {
            Debug.LogWarning("TurnManager: GameInitializer not found, cannot initialize player inventories");
            return;
        }
        
        // Ensure GameInitializer is properly initialized on server
        if (!GameInitializer.Instance.isInitialized)
        {
            Debug.Log("TurnManager: Forcing GameInitializer initialization on server");
            GameInitializer.Instance.InitializeGame();
        }
        
        Debug.Log($"TurnManager: Starting inventory initialization for {NetworkManager.Singleton.ConnectedClients.Count} players");
        
        // Tell all clients to initialize their own inventories
        InitializePlayerInventoriesClientRpc();
    }
    
    /// <summary>
    /// ClientRpc to tell all clients to initialize their own inventories
    /// </summary>
    [ClientRpc]
    private void InitializePlayerInventoriesClientRpc()
    {
        Debug.Log("TurnManager: Initializing player inventories on client");
        
        // Find the local player object
        PlayerMovement localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            PlayerInventory inventory = localPlayer.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                int playerNumber = NetworkManager.Singleton.IsHost ? 1 : 2;
                Debug.Log($"TurnManager: Client initializing inventory for player {playerNumber}");
                
                // Ensure GameInitializer is properly initialized
                if (GameInitializer.Instance != null)
                {
                    // Force initialization if not already done
                    if (!GameInitializer.Instance.isInitialized)
                    {
                        GameInitializer.Instance.InitializeGame();
                    }
                    GameInitializer.Instance.InitializePlayerInventory(inventory);
                }
                else
                {
                    Debug.LogError("TurnManager: GameInitializer.Instance is null!");
                }
            }
            else
            {
                Debug.LogWarning($"TurnManager: PlayerInventory component not found on local player object");
            }
        }
        else
        {
            Debug.LogWarning($"TurnManager: Local player object not found");
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StopGameServerRpc()
    {
        isGameStarted.Value = false;
        Debug.Log("Game stopped - not enough players.");
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestEndTurnServerRpc()
    {
        Debug.Log($"TurnManager: Client requested turn end, switching from player {currentTurnPlayer.Value}");
        EndTurnServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc()
    {
        // Switch to next player's turn
        currentTurnPlayer.Value = (currentTurnPlayer.Value == 1) ? 2 : 1;
        Debug.Log($"TurnManager: Turn switched to player {currentTurnPlayer.Value}");
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestEndGameServerRpc(int winnerPlayerNumber)
    {
        Debug.Log($"TurnManager: Client requested game end with winner Player {winnerPlayerNumber}");
        EndGameServerRpc(winnerPlayerNumber);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc(int winnerPlayerNumber)
    {
        isGameEnded.Value = true;
        winningPlayer.Value = winnerPlayerNumber;
        Debug.Log($"TurnManager: Game ended! Winner: Player {winnerPlayerNumber}");
    }
    
    // Public method to check if it's the current player's turn
    public bool IsMyTurn()
    {
        return isMyTurn && isGameStarted.Value;
    }
    
    // Public method to get current turn player
    public int GetCurrentTurnPlayer()
    {
        return currentTurnPlayer.Value;
    }
    
    // Public method to check if game has started
    public bool IsGameStarted()
    {
        return isGameStarted.Value;
    }
    
    // Public method to end turn (called from PlayerMovement)
    public void EndTurn()
    {
        // Only the server should end turns, but clients can request it
        if (IsServer)
        {
            Debug.Log($"TurnManager: Ending turn, switching from player {currentTurnPlayer.Value}");
            EndTurnServerRpc();
        }
        else
        {
            // Client requests turn end from server
            RequestEndTurnServerRpc();
        }
    }
    
    // Public method to end the game (called from PlayerMovement)
    public void EndGame(int winnerPlayerNumber)
    {
        if (IsServer)
        {
            EndGameServerRpc(winnerPlayerNumber);
        }
        else
        {
            RequestEndGameServerRpc(winnerPlayerNumber);
        }
    }
    
    // Public method to check if game has ended
    public bool IsGameEnded()
    {
        return isGameEnded.Value;
    }
    
    // Public method to get the winning player
    public int GetWinningPlayer()
    {
        return winningPlayer.Value;
    }
    
    // Show the game end action popup
    private void ShowGameEndAction()
    {
        Debug.Log($"ShowGameEndAction called - Winner: {winningPlayer.Value}, IsServer: {IsServer}, IsClient: {IsClient}");
        
        if (GameInitializer.Instance != null && ActionManager.Instance != null)
        {
            // Get the game end action based on the winner
            ActionData gameEndAction = GameInitializer.Instance.GetGameEndAction(winningPlayer.Value);
            Debug.Log($"Created game end action: {gameEndAction?.title}");
            
            // Show the game end popup on all clients
            ShowGameEndPopupClientRpc(gameEndAction);
            Debug.Log("ShowGameEndPopupClientRpc called");
        }
        else
        {
            Debug.LogWarning($"Cannot create game end action - GameInitializer: {GameInitializer.Instance != null}, ActionManager: {ActionManager.Instance != null}");
        }
    }
    
    [ClientRpc]
    private void ShowGameEndPopupClientRpc(ActionData gameEndAction)
    {
        Debug.Log($"ClientRpc received: ShowGameEndPopupClientRpc - Winner: {winningPlayer.Value}, Action: {gameEndAction?.title}");
        
        // Find the local player to show the popup to
        PlayerMovement localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            PlayerInventory playerInventory = localPlayer.GetComponent<PlayerInventory>();
            if (playerInventory != null && ActionManager.Instance != null)
            {
                Debug.Log($"Showing game end popup to local player");
                // Show the game end action popup
                ActionManager.Instance.ShowActionPopup(gameEndAction, playerInventory);
            }
            else
            {
                Debug.LogWarning($"Cannot show popup - PlayerInventory: {playerInventory != null}, ActionManager: {ActionManager.Instance != null}");
            }
        }
        else
        {
            Debug.LogWarning("Local player not found for game end popup");
        }
    }
    
} 