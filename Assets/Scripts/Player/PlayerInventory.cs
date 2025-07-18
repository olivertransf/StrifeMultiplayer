using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    
    [Header("Network Variables")]
    public NetworkVariable<int> money = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    // Events for UI updates
    public System.Action<int> OnMoneyChanged;
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Subscribe to money changes for local player
            money.OnValueChanged += OnMoneyValueChanged;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            // Unsubscribe from money changes
            money.OnValueChanged -= OnMoneyValueChanged;
        }
    }
    
    private void OnMoneyValueChanged(int previousValue, int newValue)
    {
        // Notify UI or other systems about money change
        OnMoneyChanged?.Invoke(newValue);
        Debug.Log($"Money changed from {previousValue} to {newValue}");
    }
    
    // Add money (only owner can call this)
    public void AddMoney(int amount)
    {
        if (!IsOwner) return;
        
        money.Value += amount;
    }
    
    // Remove money (only owner can call this)
    public bool RemoveMoney(int amount)
    {
        if (!IsOwner) return false;
        
        if (money.Value >= amount)
        {
            money.Value -= amount;
            return true;
        }
        return false;
    }
    
    // Get current money (anyone can read)
    public int GetMoney()
    {
        return money.Value;
    }
    
    // Check if player has enough money
    public bool HasEnoughMoney(int amount)
    {
        return money.Value >= amount;
    }
}