using UnityEngine;
using Unity.Netcode;

public class PlayerColorSync : NetworkBehaviour
{
    [SerializeField] private Color[] playerColors = { Color.red, Color.blue };
    
    // Network variable to sync player number across the network
    private NetworkVariable<int> playerNumber = new NetworkVariable<int>();
    private SpriteRenderer spriteRenderer;
    
    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (IsServer)
        {
            // Set player number based on client ID
            int clientNumber = (int)OwnerClientId;
            playerNumber.Value = clientNumber;
        }
        
        // Subscribe to player number changes
        playerNumber.OnValueChanged += OnPlayerNumberChanged;
        
        // Apply initial color
        ApplyColor();
    }
    
    public override void OnNetworkDespawn()
    {
        if (playerNumber != null)
        {
            playerNumber.OnValueChanged -= OnPlayerNumberChanged;
        }
    }
    
    private void OnPlayerNumberChanged(int previousValue, int newValue)
    {
        ApplyColor();
    }
    
    private void ApplyColor()
    {
        if (spriteRenderer == null) return;
        
        int colorIndex = playerNumber.Value;
        if (colorIndex >= 0 && colorIndex < playerColors.Length)
        {
            spriteRenderer.color = playerColors[colorIndex];
        }
    }
} 