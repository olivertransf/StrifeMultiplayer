using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class BottomStatusBarUI : MonoBehaviour
{
    [Header("Player 1 UI")]
    [SerializeField] private TextMeshProUGUI player1MoneyText;
    [SerializeField] private Button player1BabiesButton;
    [SerializeField] private TextMeshProUGUI player1BabiesText;
    [SerializeField] private Button player1HousesButton;
    [SerializeField] private TextMeshProUGUI player1HousesText;
    [SerializeField] private Button player1JobButton;
    [SerializeField] private TextMeshProUGUI player1JobText;
    
    [Header("Player 2 UI")]
    [SerializeField] private TextMeshProUGUI player2MoneyText;
    [SerializeField] private Button player2BabiesButton;
    [SerializeField] private TextMeshProUGUI player2BabiesText;
    [SerializeField] private Button player2HousesButton;
    [SerializeField] private TextMeshProUGUI player2HousesText;
    [SerializeField] private Button player2JobButton;
    [SerializeField] private TextMeshProUGUI player2JobText;
    
    [Header("Item Popup")]
    [SerializeField] private GameObject itemPopupPrefab;
    [SerializeField] private Canvas targetCanvas; // Canvas to parent popups to
    
    private Dictionary<ulong, PlayerInventory> playerInventories = new Dictionary<ulong, PlayerInventory>();
    private Dictionary<ulong, TextMeshProUGUI> moneyTexts = new Dictionary<ulong, TextMeshProUGUI>();
    private Dictionary<ulong, Button> babiesButtons = new Dictionary<ulong, Button>();
    private Dictionary<ulong, TextMeshProUGUI> babiesTexts = new Dictionary<ulong, TextMeshProUGUI>();
    private Dictionary<ulong, Button> housesButtons = new Dictionary<ulong, Button>();
    private Dictionary<ulong, TextMeshProUGUI> housesTexts = new Dictionary<ulong, TextMeshProUGUI>();
    private Dictionary<ulong, Button> jobButtons = new Dictionary<ulong, Button>();
    private Dictionary<ulong, TextMeshProUGUI> jobTexts = new Dictionary<ulong, TextMeshProUGUI>();
    
    void Start()
    {
        SetupUI();
        SetupButtonListeners();
        
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        // Set initial default values
        SetDefaultValues();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        // Unsubscribe from events
        foreach (var inventory in playerInventories.Values)
        {
            if (inventory != null)
            {
                inventory.OnMoneyChanged -= OnMoneyChanged;
                inventory.OnBabiesChanged -= OnBabiesChanged;
                inventory.OnHousesChanged -= OnHousesChanged;
                inventory.OnJobChanged -= OnJobChanged;
            }
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // Wait a bit for players to spawn, then refresh
        Invoke(nameof(SetupPlayerInventories), 1.0f);
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        // Remove disconnected player
        if (playerInventories.ContainsKey(clientId))
        {
            if (playerInventories[clientId] != null)
            {
                playerInventories[clientId].OnMoneyChanged -= OnMoneyChanged;
                playerInventories[clientId].OnBabiesChanged -= OnBabiesChanged;
                playerInventories[clientId].OnHousesChanged -= OnHousesChanged;
                playerInventories[clientId].OnJobChanged -= OnJobChanged;
            }
            playerInventories.Remove(clientId);
        }
        
        // Reset display for disconnected player
        SetDefaultValues();
    }
    
    // Call this when the game starts
    public void OnGameStarted()
    {
        Invoke(nameof(SetupPlayerInventories), 1.0f);
    }
    
    private void SetDefaultValues()
    {
        // Set default values for Player 1
        if (player1MoneyText != null) player1MoneyText.text = "$0";
        if (player1BabiesText != null) player1BabiesText.text = "Babies: 0";
        if (player1HousesText != null) player1HousesText.text = "Houses: 0";
        if (player1JobText != null) player1JobText.text = "Unemployed";
        
        // Set default values for Player 2
        if (player2MoneyText != null) player2MoneyText.text = "$0";
        if (player2BabiesText != null) player2BabiesText.text = "Babies: 0";
        if (player2HousesText != null) player2HousesText.text = "Houses: 0";
        if (player2JobText != null) player2JobText.text = "Unemployed";
    }
    
    private void SetupUI()
    {
        // Set up Player 1 UI references
        moneyTexts[0] = player1MoneyText;
        babiesButtons[0] = player1BabiesButton;
        babiesTexts[0] = player1BabiesText;
        housesButtons[0] = player1HousesButton;
        housesTexts[0] = player1HousesText;
        jobButtons[0] = player1JobButton;
        jobTexts[0] = player1JobText;
        
        // Set up Player 2 UI references
        moneyTexts[1] = player2MoneyText;
        babiesButtons[1] = player2BabiesButton;
        babiesTexts[1] = player2BabiesText;
        housesButtons[1] = player2HousesButton;
        housesTexts[1] = player2HousesText;
        jobButtons[1] = player2JobButton;
        jobTexts[1] = player2JobText;
        
        // Find player inventories
        SetupPlayerInventories();
    }
    
    private void SetupButtonListeners()
    {
        // Player 1 buttons
        if (player1BabiesButton != null)
            player1BabiesButton.onClick.AddListener(() => ShowBabiesPopup(0));
        if (player1HousesButton != null)
            player1HousesButton.onClick.AddListener(() => ShowHousesPopup(0));
        if (player1JobButton != null)
            player1JobButton.onClick.AddListener(() => ShowJobPopup(0));
        
        // Player 2 buttons
        if (player2BabiesButton != null)
            player2BabiesButton.onClick.AddListener(() => ShowBabiesPopup(1));
        if (player2HousesButton != null)
            player2HousesButton.onClick.AddListener(() => ShowHousesPopup(1));
        if (player2JobButton != null)
            player2JobButton.onClick.AddListener(() => ShowJobPopup(1));
    }
    
    private void SetupPlayerInventories()
    {
        Debug.Log("BottomStatusBarUI: Setting up player inventories...");
        
        PlayerInventory[] inventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        Debug.Log($"BottomStatusBarUI: Found {inventories.Length} player inventories");
        
        foreach (PlayerInventory inventory in inventories)
        {
            if (inventory != null && inventory.NetworkObject != null)
            {
                ulong clientId = inventory.NetworkObject.OwnerClientId;
                Debug.Log($"BottomStatusBarUI: Found inventory for client {clientId}");
                
                if (clientId == 0 || clientId == 1) // Only handle Player 1 and 2
                {
                    playerInventories[clientId] = inventory;
                    
                    // Subscribe to events
                    inventory.OnMoneyChanged += OnMoneyChanged;
                    inventory.OnBabiesChanged += OnBabiesChanged;
                    inventory.OnHousesChanged += OnHousesChanged;
                    inventory.OnJobChanged += OnJobChanged;
                    
                    // Initial update
                    UpdatePlayerDisplay(clientId);
                    Debug.Log($"BottomStatusBarUI: Set up inventory for client {clientId}");
                }
            }
        }
        
        // If we're connected but haven't found inventories yet, try again later
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) && inventories.Length == 0)
        {
            Debug.Log("BottomStatusBarUI: No inventories found, will retry in 2 seconds");
            Invoke(nameof(SetupPlayerInventories), 2.0f);
        }
    }
    
    private void OnMoneyChanged(int newAmount)
    {
        // Find which player's money changed
        foreach (var kvp in playerInventories)
        {
            if (kvp.Value != null && kvp.Value.GetMoney() == newAmount)
            {
                UpdateMoneyDisplay(kvp.Key, newAmount);
                break;
            }
        }
    }
    
    private void OnBabiesChanged()
    {
        // Update all baby displays
        foreach (var kvp in playerInventories)
        {
            UpdateBabiesDisplay(kvp.Key);
        }
    }
    
    private void OnHousesChanged()
    {
        // Update all house displays
        foreach (var kvp in playerInventories)
        {
            UpdateHousesDisplay(kvp.Key);
        }
    }
    
    private void OnJobChanged()
    {
        // Update all job displays
        foreach (var kvp in playerInventories)
        {
            UpdateJobDisplay(kvp.Key);
        }
    }
    
    private void UpdatePlayerDisplay(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId)) return;
        
        PlayerInventory inventory = playerInventories[clientId];
        UpdateMoneyDisplay(clientId, inventory.GetMoney());
        UpdateBabiesDisplay(clientId);
        UpdateHousesDisplay(clientId);
        UpdateJobDisplay(clientId);
    }
    
    private void UpdateMoneyDisplay(ulong clientId, int amount)
    {
        if (moneyTexts.ContainsKey(clientId) && moneyTexts[clientId] != null)
        {
            moneyTexts[clientId].text = $"${amount:N0}";
        }
    }
    
    private void UpdateBabiesDisplay(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId)) return;
        
        int babyCount = playerInventories[clientId].GetTotalBabies();
        if (babiesTexts.ContainsKey(clientId) && babiesTexts[clientId] != null)
        {
            babiesTexts[clientId].text = $"Babies: {babyCount}";
        }
    }
    
    private void UpdateHousesDisplay(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId)) return;
        
        int houseCount = playerInventories[clientId].GetHouseCount();
        if (housesTexts.ContainsKey(clientId) && housesTexts[clientId] != null)
        {
            housesTexts[clientId].text = $"Houses: {houseCount}";
        }
    }
    
    private void UpdateJobDisplay(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId)) return;
        
        JobData job = playerInventories[clientId].GetJob();
        if (jobTexts.ContainsKey(clientId) && jobTexts[clientId] != null)
        {
            if (!string.IsNullOrEmpty(job.title))
            {
                jobTexts[clientId].text = $"{job.title}\n${job.salary:N0}/month (Spin: {job.rollNumber})";
            }
            else
            {
                jobTexts[clientId].text = "Unemployed";
            }
        }
    }
    
    private void ShowBabiesPopup(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId) || itemPopupPrefab == null) return;
        
        List<BabyData> babies = playerInventories[clientId].GetAllBabies();
        string playerName = clientId == 0 ? "Player 1" : "Player 2";
        
        // Find Canvas if not assigned
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        GameObject popup = Instantiate(itemPopupPrefab, canvas.transform);
        ItemPopup itemPopup = popup.GetComponent<ItemPopup>();
        if (itemPopup != null)
        {
            // Count boys and girls
            int boysCount = 0;
            int girlsCount = 0;
            foreach (BabyData baby in babies)
            {
                if (baby.isMale)
                    boysCount++;
                else
                    girlsCount++;
            }
            
            // Create a summary baby data for the popup
            BabyData summaryBaby = new BabyData(
                $"All Babies ({babies.Count})",
                $"Player has {boysCount} boys and {girlsCount} girls",
                babies.Count > 0 ? babies[0].isMale : true,
                0
            );
            itemPopup.InitializeBaby(summaryBaby, playerName);
        }
    }
    
    private void ShowHousesPopup(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId) || itemPopupPrefab == null) return;
        
        List<HouseData> houses = new List<HouseData>();
        for (int i = 0; i < playerInventories[clientId].GetHouseCount(); i++)
        {
            HouseData? house = playerInventories[clientId].GetHouse(i);
            if (house.HasValue)
            {
                houses.Add(house.Value);
            }
        }
        
        string playerName = clientId == 0 ? "Player 1" : "Player 2";
        
        // Find Canvas if not assigned
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        GameObject popup = Instantiate(itemPopupPrefab, canvas.transform);
        ItemPopup itemPopup = popup.GetComponent<ItemPopup>();
        if (itemPopup != null)
        {
            // Create a summary house data for the popup
            HouseData summaryHouse = new HouseData(
                $"All Houses ({houses.Count})",
                $"Player owns {houses.Count} houses",
                houses.Count > 0 ? houses[0].cost : 0,
                0, 0
            );
            itemPopup.InitializeHouse(summaryHouse, playerName);
        }
    }
    
    private void ShowJobPopup(ulong clientId)
    {
        if (!playerInventories.ContainsKey(clientId) || itemPopupPrefab == null) return;
        
        JobData job = playerInventories[clientId].GetJob();
        string playerName = clientId == 0 ? "Player 1" : "Player 2";
        
        // Find Canvas if not assigned
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        GameObject popup = Instantiate(itemPopupPrefab, canvas.transform);
        ItemPopup itemPopup = popup.GetComponent<ItemPopup>();
        if (itemPopup != null)
        {
            itemPopup.InitializeJob(job, playerName);
        }
    }
    
    // Public method to refresh displays (useful for debugging)
    public void RefreshDisplays()
    {
        foreach (var kvp in playerInventories)
        {
            UpdatePlayerDisplay(kvp.Key);
        }
    }
} 