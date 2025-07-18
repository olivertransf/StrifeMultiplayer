using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class MoneyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI player1MoneyText;
    [SerializeField] private TextMeshProUGUI player2MoneyText;
    
    [Header("Settings")]
    [SerializeField] private string moneyFormat = "Money: ${0}";
    [SerializeField] private float refreshInterval = 0.5f; // Refresh every 0.5 seconds
    
    private Dictionary<ulong, TextMeshProUGUI> playerMoneyTexts = new Dictionary<ulong, TextMeshProUGUI>();
    private Dictionary<ulong, PlayerInventory> playerInventories = new Dictionary<ulong, PlayerInventory>();
    private float lastRefreshTime;
    private bool isGameStarted = false;
    
    void Start()
    {
        // Initialize UI with default values
        UpdateAllMoneyDisplays();
    }
    
    void Update()
    {
        // Only refresh if the game is actually running
        if (isGameStarted && Time.time - lastRefreshTime > refreshInterval)
        {
            RefreshDisplays();
            lastRefreshTime = Time.time;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        // Unsubscribe from inventory events
        foreach (var inventory in playerInventories.Values)
        {
            if (inventory != null)
            {
                inventory.OnMoneyChanged -= OnMoneyChanged;
            }
        }
    }
    
    // Call this when the game starts (from your NetworkManager or game start logic)
    public void OnGameStarted()
    {
        Debug.Log("Game started - setting up money UI");
        isGameStarted = true;
        
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        // Initial setup with a delay to ensure players are spawned
        Invoke(nameof(SetupPlayerMoneyDisplays), 1.0f);
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        // Wait a bit to ensure players are spawned
        Invoke(nameof(SetupPlayerMoneyDisplays), 0.5f);
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
        // Clean up disconnected player
        if (playerInventories.ContainsKey(clientId))
        {
            if (playerInventories[clientId] != null)
            {
                playerInventories[clientId].OnMoneyChanged -= OnMoneyChanged;
            }
            playerInventories.Remove(clientId);
            playerMoneyTexts.Remove(clientId);
        }
        
        UpdateAllMoneyDisplays();
    }
    
    private void SetupPlayerMoneyDisplays()
    {
        if (!isGameStarted) return;
        
        Debug.Log("Setting up player money displays...");
        
        // Find all players in the scene
        PlayerInventory[] inventories = FindObjectsOfType<PlayerInventory>();
        Debug.Log($"Found {inventories.Length} PlayerInventory components");
        
        foreach (PlayerInventory inventory in inventories)
        {
            if (inventory != null && inventory.NetworkObject != null)
            {
                ulong clientId = inventory.NetworkObject.OwnerClientId;
                Debug.Log($"Processing inventory for client {clientId}");
                
                // Assign text field based on player number
                TextMeshProUGUI moneyText = GetPlayerMoneyText(clientId);
                
                if (moneyText != null)
                {
                    // Store references
                    playerMoneyTexts[clientId] = moneyText;
                    playerInventories[clientId] = inventory;
                    
                    // Subscribe to money changes
                    inventory.OnMoneyChanged += OnMoneyChanged;
                    
                    // Update initial display
                    UpdateMoneyDisplay(clientId, inventory.GetMoney());
                    
                    Debug.Log($"Setup money display for player {clientId} with {inventory.GetMoney()} money");
                }
                else
                {
                    Debug.LogWarning($"No text field found for client {clientId}");
                }
            }
        }
        
        // If we're a client and haven't found any inventories yet, try again later
        if (NetworkManager.Singleton.IsClient && inventories.Length == 0)
        {
            Invoke(nameof(SetupPlayerMoneyDisplays), 1.0f);
        }
    }
    
    private TextMeshProUGUI GetPlayerMoneyText(ulong clientId)
    {
        // Simple mapping: Player 1 (client 0) gets player1MoneyText, Player 2 (client 1) gets player2MoneyText
        if (clientId == 0)
        {
            return player1MoneyText;
        }
        else if (clientId == 1)
        {
            return player2MoneyText;
        }
        
        return null;
    }
    
    private void OnMoneyChanged(int newAmount)
    {
        Debug.Log($"Money changed to: {newAmount}");
        
        // Find which player's money changed
        foreach (var kvp in playerInventories)
        {
            if (kvp.Value != null && kvp.Value.GetMoney() == newAmount)
            {
                UpdateMoneyDisplay(kvp.Key, newAmount);
                Debug.Log($"Updated display for player {kvp.Key} to {newAmount}");
                break;
            }
        }
    }
    
    private void UpdateMoneyDisplay(ulong clientId, int amount)
    {
        if (playerMoneyTexts.ContainsKey(clientId) && playerMoneyTexts[clientId] != null)
        {
            playerMoneyTexts[clientId].text = string.Format(moneyFormat, amount);
            Debug.Log($"Updated UI for client {clientId}: {amount}");
        }
    }
    
    private void UpdateAllMoneyDisplays()
    {
        // Update all known players
        foreach (var kvp in playerInventories)
        {
            if (kvp.Value != null)
            {
                UpdateMoneyDisplay(kvp.Key, kvp.Value.GetMoney());
            }
        }
        
        // Clear unused text fields
        if (player1MoneyText != null && !playerMoneyTexts.ContainsValue(player1MoneyText))
        {
            player1MoneyText.text = string.Format(moneyFormat, 0);
        }
        
        if (player2MoneyText != null && !playerMoneyTexts.ContainsValue(player2MoneyText))
        {
            player2MoneyText.text = string.Format(moneyFormat, 0);
        }
    }
    
    // Public method to manually refresh displays (useful for debugging)
    public void RefreshDisplays()
    {
        if (isGameStarted)
        {
            SetupPlayerMoneyDisplays();
        }
    }
} 