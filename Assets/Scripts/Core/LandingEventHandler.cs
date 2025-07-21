using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections.Generic;

public class LandingEventHandler
{
    private static PlayerInventory pendingInventory = null;
    
    static LandingEventHandler()
    {
        // Subscribe to action received events
        ActionManager.OnActionReceived += OnActionReceived;
    }
    
    /// <summary>
    /// Handle landing on a tile and trigger appropriate events
    /// </summary>
    public static void HandleLanding(PlayerMovement playerMovement, Tilemap tilemap)
    {
        Debug.Log($"LandingEventHandler: HandleLanding called for player {playerMovement.GetMyPlayerNumber()} (IsServer: {Unity.Netcode.NetworkManager.Singleton?.IsServer}, IsClient: {Unity.Netcode.NetworkManager.Singleton?.IsClient})");
        
        if (playerMovement == null) return;
        
        // Get the player's inventory
        PlayerInventory inventory = playerMovement.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("PlayerInventory component not found on player!");
            return;
        }
        
        // Get the tile the player landed on
        TileBase landedTile = GetLandedTile(playerMovement, tilemap);
        if (landedTile == null)
        {
            Debug.LogWarning("No tile found at player position!");
            return;
        }
        
        string tileName = landedTile.name;
        Debug.Log($"LandingEventHandler: Player landed on tile: {tileName}");
        
        // Handle different tile types
        if (IsActionTile(tileName))
        {
            HandleActionTile(playerMovement, inventory);
        }
        else if (IsBoyTile(tileName))
        {
            HandleBoyTile(playerMovement, inventory);
        }
        else if (IsGirlTile(tileName))
        {
            HandleGirlTile(playerMovement, inventory);
        }
        else if (IsHouseTile(tileName))
        {
            HandleHouseTile(playerMovement, inventory);
        }
        else if (IsLoseTile(tileName))
        {
            HandleLoseTile(playerMovement, inventory);
        }
        else if (IsPaydayTile(tileName))
        {
            HandlePaydayTile(playerMovement, inventory);
        }
        else if (IsEndTile(tileName))
        {
            HandleEndTile(playerMovement, inventory);
        }
        else if (IsStopTile(tileName))
        {
            HandleStopTile(playerMovement, inventory);
        }
        else
        {
            Debug.Log($"LandingEventHandler: Unknown tile type: {tileName}");
        }
    }
    
    /// <summary>
    /// Get the tile at the player's current position
    /// </summary>
    private static TileBase GetLandedTile(PlayerMovement playerMovement, Tilemap tilemap)
    {
        if (tilemap == null) return null;
        
        Vector3Int cellPosition = tilemap.WorldToCell(playerMovement.transform.position);
        return tilemap.GetTile(cellPosition);
    }
    
    /// <summary>
    /// Handle Action tile - show random action popup
    /// </summary>
    private static void HandleActionTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Action tile");
        
        if (ActionManager.Instance != null)
        {
            ActionData randomAction = ActionManager.Instance.GetRandomAction();
            if (randomAction != null)
            {
                string playerName = GetPlayerName(playerMovement);
                ActionManager.Instance.ShowActionPopupToAllPlayersClientRpc(randomAction, inventory.NetworkObject.OwnerClientId, playerName);
            }
            else
            {
                // Fallback for clients waiting for action
                if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    pendingInventory = inventory;
                }
                else
                {
                    inventory.AddMoney(10);
                }
            }
        }
        else
        {
            inventory.AddMoney(10);
        }
    }
    
    /// <summary>
    /// Handle Boy tile - add a boy baby
    /// </summary>
    private static void HandleBoyTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Boy tile");
        
        if (GameInitializer.Instance != null)
        {
            Baby boyBaby = GameInitializer.Instance.GetRandomBabyByGender(true);
            if (boyBaby != null)
            {
                // Convert to BabyData before adding to inventory
                inventory.AddBaby(boyBaby.ToBabyData());
                
                // Show popup notification
                ActionData boyAction = new ActionData(
                    "New Baby Boy!", 
                    $"Congratulations! You now have a baby boy named {boyBaby.title}!", 
                    0, 1
                );
                
                string playerName = GetPlayerName(playerMovement);
                ActionManager.Instance?.ShowActionPopupToAllPlayersClientRpc(boyAction, inventory.NetworkObject.OwnerClientId, playerName);
            }
        }
    }
    
    /// <summary>
    /// Handle Girl tile - add a girl baby
    /// </summary>
    private static void HandleGirlTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Girl tile");
        
        if (GameInitializer.Instance != null)
        {
            Baby girlBaby = GameInitializer.Instance.GetRandomBabyByGender(false);
            if (girlBaby != null)
            {
                // Convert to BabyData before adding to inventory
                inventory.AddBaby(girlBaby.ToBabyData());
                
                // Show popup notification
                ActionData girlAction = new ActionData(
                    "New Baby Girl!", 
                    $"Congratulations! You now have a baby girl named {girlBaby.title}!", 
                    0, 1
                );
                
                string playerName = GetPlayerName(playerMovement);
                ActionManager.Instance?.ShowActionPopupToAllPlayersClientRpc(girlAction, inventory.NetworkObject.OwnerClientId, playerName);
            }
        }
    }
    
    /// <summary>
    /// Handle House tile - show house purchase options
    /// </summary>
    private static void HandleHouseTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling House tile");
        
        if (GameInitializer.Instance != null)
        {
            // Get two random houses for purchase options
            List<House> allHouses = GameInitializer.Instance.GetAllHouses();
            if (allHouses.Count >= 2)
            {
                // Get two random houses
                House house1 = allHouses[Random.Range(0, allHouses.Count)];
                House house2 = allHouses[Random.Range(0, allHouses.Count)];
                
                // Ensure we get two different houses
                while (house2.title == house1.title && allHouses.Count > 1)
                {
                    house2 = allHouses[Random.Range(0, allHouses.Count)];
                }
                
                // Show house purchase popup
                ShowHousePurchasePopup(playerMovement, inventory, house1, house2);
            }
        }
    }
    
    /// <summary>
    /// Handle Lose tile - lose money
    /// </summary>
    private static void HandleLoseTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Lose tile");
        
        int loseAmount = 1000;
        inventory.AddMoney(-loseAmount);
        
        // Show popup notification
        ActionData loseAction = new ActionData(
            "Lost Money!", 
            $"You lost ${loseAmount}!", 
            -loseAmount
        );
        
        string playerName = GetPlayerName(playerMovement);
        ActionManager.Instance?.ShowActionPopupToAllPlayersClientRpc(loseAction, inventory.NetworkObject.OwnerClientId, playerName);
    }
    
    /// <summary>
    /// Handle Payday tile - receive salary
    /// </summary>
    private static void HandlePaydayTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Payday tile");
        
        JobData currentJob = inventory.GetJob();
        int salary = currentJob.salary;
        
        inventory.AddMoney(salary);
        
        // Show popup notification
        ActionData paydayAction = new ActionData(
            "Payday!", 
            $"You received your salary of ${salary}!", 
            salary
        );
        
        string playerName = GetPlayerName(playerMovement);
        ActionManager.Instance?.ShowActionPopupToAllPlayersClientRpc(paydayAction, inventory.NetworkObject.OwnerClientId, playerName);
    }
    
    /// <summary>
    /// Handle End tile - game ending
    /// </summary>
    private static void HandleEndTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling End tile");
        
        ActionData endTileAction = new ActionData(
            "End Tile Reached!", 
            "You've reached the end of the path! The game will now end and the player with the most money will win!", 
            500
        );
        
        string playerName = GetPlayerName(playerMovement);
        ActionManager.Instance?.ShowActionPopupToAllPlayersClientRpc(endTileAction, inventory.NetworkObject.OwnerClientId, playerName);
    }
    
    /// <summary>
    /// Handle Stop tile - already handled in PlayerMovement, just log
    /// </summary>
    private static void HandleStopTile(PlayerMovement playerMovement, PlayerInventory inventory)
    {
        Debug.Log("LandingEventHandler: Handling Stop tile (turn continuation handled in PlayerMovement)");
    }
    
    /// <summary>
    /// Show house purchase popup with two options
    /// </summary>
    private static void ShowHousePurchasePopup(PlayerMovement playerMovement, PlayerInventory inventory, House house1, House house2)
    {
        // Try to load a prefab named "HouseChoicePopup" from a Resources folder.
        GameObject prefab = Resources.Load<GameObject>("HouseChoicePopup");
        GameObject popupObj;
        if (prefab != null)
        {
            popupObj = GameObject.Instantiate(prefab);
        }
        else
        {
            // If no prefab found, create a simple popup object programmatically
            popupObj = new GameObject("HouseChoicePopup");
        }
        
        HouseChoicePopup popup = popupObj.AddComponent<HouseChoicePopup>();
        popup.Initialize(house1, house2, inventory, playerMovement);
    }
    
    // Tile type checking methods
    private static bool IsActionTile(string tileName) => tileName.Contains("Action");
    private static bool IsBoyTile(string tileName) => tileName.Contains("Boy");
    private static bool IsGirlTile(string tileName) => tileName.Contains("Girl");
    private static bool IsHouseTile(string tileName) => tileName.Contains("House");
    private static bool IsLoseTile(string tileName) => tileName.Contains("Lose");
    private static bool IsPaydayTile(string tileName) => tileName.Contains("Payday");
    private static bool IsEndTile(string tileName) => tileName.Contains("End") || tileName.Contains("end");
    private static bool IsStopTile(string tileName) => tileName.Contains("Stop");
    
    /// <summary>
    /// Get the player name based on client ID
    /// </summary>
    private static string GetPlayerName(PlayerMovement playerMovement)
    {
        if (playerMovement.NetworkObject == null) return "Unknown Player";
        
        ulong clientId = playerMovement.NetworkObject.OwnerClientId;
        if (clientId == 0)
        {
            return "Player 1";
        }
        else if (clientId == 1)
        {
            return "Player 2";
        }
        else
        {
            return $"Player {clientId + 1}";
        }
    }
    
    private static void OnActionReceived(ActionData actionData)
    {
        if (actionData != null && pendingInventory != null)
        {
            ActionManager.Instance?.ShowActionPopup(actionData, pendingInventory);
            pendingInventory = null;
        }
    }
    
    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public static void AddMoney(PlayerInventory playerInventory, int amount)
    {
        if (playerInventory != null)
        {
            playerInventory.AddMoney(amount);
        }
    }
}