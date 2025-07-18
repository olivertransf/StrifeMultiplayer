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
        
        // If we're the host and now have both players, start the game
        if (IsHost && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            StartGameServerRpc();
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
        
        // Both players are connected, start the game
        StartGameServerRpc();
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
        if (newValue)
        {
            Debug.Log($"Game ended! Winner: Player {winningPlayer.Value}");
            UpdateTurnUI();
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
    
    private GameObject FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return null;
        
        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient != null && localClient.PlayerObject != null)
        {
            return localClient.PlayerObject.gameObject;
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
} 