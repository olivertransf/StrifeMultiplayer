using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class ActionManager : NetworkBehaviour
{
    [Header("Action Popup")]
    public GameObject actionPopupPrefab;
    
    private static ActionManager instance;
    public static ActionManager Instance { get { return instance; } }
    
    // Callback for when actions are received
    public static event Action<ActionData> OnActionReceived;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public ActionData GetRandomAction()
    {
        Debug.Log($"ActionManager: GetRandomAction called (IsServer: {IsServer}, IsClient: {IsClient})");
        
        // Only server should have access to GameInitializer
        if (IsServer && GameInitializer.Instance != null)
        {
            ActionData action = GameInitializer.Instance.GetRandomAction();
            Debug.Log($"ActionManager: Server returning action: {action?.title}");
            return action;
        }
        
        // Clients should request actions from server
        if (IsClient && !IsServer)
        {
            Debug.Log("ActionManager: Client requesting action from server");
            RequestRandomActionServerRpc();
            return null; // Will be received via ClientRpc
        }
        
        Debug.LogWarning("ActionManager: No GameInitializer found! No actions available.");
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRandomActionServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ActionData randomAction = null;
        
        if (GameInitializer.Instance != null)
        {
            randomAction = GameInitializer.Instance.GetRandomAction();
        }
        
        // Send the action back to the requesting client
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        SendRandomActionClientRpc(randomAction, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } });
    }

    [ClientRpc]
    private void SendRandomActionClientRpc(ActionData actionData, ClientRpcParams clientRpcParams = default)
    {
        // Handle the received action data
        if (actionData != null)
        {
            Debug.Log($"Received random action: {actionData.title}");
            // Trigger the event so other systems can handle the action
            OnActionReceived?.Invoke(actionData);
        }
    }
    
    public void ShowActionPopup(ActionData actionData, PlayerInventory targetPlayer)
    {
        Debug.Log($"ShowActionPopup called - Action: {actionData?.title}, TargetPlayer: {targetPlayer?.name}");
        
        // Use the network-synchronized method to show popup to all players
        if (targetPlayer != null && targetPlayer.NetworkObject != null)
        {
            string playerName = GetPlayerName(targetPlayer.NetworkObject.OwnerClientId);
            Debug.Log($"Showing action popup to all players for {playerName} (Client ID: {targetPlayer.NetworkObject.OwnerClientId})");
            ShowActionPopupToAllPlayersClientRpc(actionData, targetPlayer.NetworkObject.OwnerClientId, playerName);
        }
        else
        {
            Debug.LogError("Target player or NetworkObject is null!");
        }
    }
    
    /// <summary>
    /// Get player name based on client ID
    /// </summary>
    private string GetPlayerName(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return "Unknown Player";
        
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            return NetworkManager.Singleton.IsHost ? "Player 1" : "Player 2";
        }
        else
        {
            return NetworkManager.Singleton.IsHost ? "Player 2" : "Player 1";
        }
    }
    
    // New method to show action popup to all players
    [ClientRpc]
    public void ShowActionPopupToAllPlayersClientRpc(ActionData actionData, ulong targetPlayerClientId, string targetPlayerName)
    {
        Debug.Log($"ShowActionPopupToAllPlayersClientRpc received - Action: {actionData?.title}, TargetPlayer: {targetPlayerName}, Client ID: {targetPlayerClientId}");
        
        if (actionPopupPrefab == null)
        {
            Debug.LogError("Action popup prefab not assigned!");
            return;
        }
        
        // Find the target player's inventory
        PlayerInventory targetPlayer = null;
        PlayerInventory[] allInventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        Debug.Log($"ActionManager: Found {allInventories.Length} PlayerInventory objects");
        
        foreach (PlayerInventory inventory in allInventories)
        {
            if (inventory.NetworkObject != null)
            {
                Debug.Log($"ActionManager: Checking inventory - OwnerClientId: {inventory.NetworkObject.OwnerClientId}, Target: {targetPlayerClientId}");
                if (inventory.NetworkObject.OwnerClientId == targetPlayerClientId)
                {
                    targetPlayer = inventory;
                    Debug.Log($"ActionManager: Found target player inventory: {inventory.name}");
                    break;
                }
            }
            else
            {
                Debug.LogWarning($"ActionManager: PlayerInventory {inventory.name} has no NetworkObject!");
            }
        }
        
        if (targetPlayer == null)
        {
            Debug.LogError($"Could not find PlayerInventory for client {targetPlayerClientId}");
            return;
        }
        
        // Create the popup
        GameObject popup = Instantiate(actionPopupPrefab);
        ActionPopup actionPopup = popup.GetComponent<ActionPopup>();
        
        if (actionPopup != null)
        {
            actionPopup.InitializeForAllPlayers(actionData, targetPlayer, targetPlayerName);
        }
        else
        {
            Debug.LogError("ActionPopup component not found on prefab!");
        }
    }
    
    public void ExecuteAction(ActionData actionData, PlayerInventory targetPlayer)
    {
        if (actionData == null || targetPlayer == null) return;
        
        // Create a temporary action object to execute
        GameObject tempAction = new GameObject("TempAction");
        Action action = tempAction.AddComponent<Action>();
        action.Initialize(actionData);
        action.Execute(targetPlayer);
        
        // Clean up
        Destroy(tempAction);
    }
} 