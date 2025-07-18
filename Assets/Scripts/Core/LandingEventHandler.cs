using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;

public class LandingEventHandler
{
    /// <summary>
    /// Handle landing on a tile and give basic reward
    /// </summary>
    public static void HandleLanding(PlayerMovement playerMovement, Tilemap tilemap)
    {
        if (playerMovement == null) return;
        
        // Get the player's inventory
        PlayerInventory inventory = playerMovement.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("PlayerInventory component not found on player!");
            return;
        }
        
        // Give basic reward for landing
        inventory.AddMoney(10);
        Debug.Log("Player landed and earned 10 money!");
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