using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class MoveButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button moveButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private TextMeshProUGUI spinNumberText; // Reference to the "Number" GameObject
    
    private TurnManager turnManager;
    
    void Start()
    {
        turnManager = TurnManager.Instance;
        
        if (moveButton != null)
        {
            moveButton.onClick.AddListener(OnMoveButtonClicked);
        }
        
        // Set initial text
        if (buttonText != null)
        {
            buttonText.text = "Move";
        }
        
        if (turnIndicatorText != null)
        {
            turnIndicatorText.text = "Waiting for game to start...";
            turnIndicatorText.color = Color.gray;
        }
        
        // Assign text field to SpinManager
        StartCoroutine(AssignSpinManagerText());
    }
    
    private System.Collections.IEnumerator AssignSpinManagerText()
    {
        // Wait for SpinManager singleton to be available
        SpinManager spinManager = null;
        int attempts = 0;
        const int maxAttempts = 20; // Try for 10 seconds (20 * 0.5s)
        
        while (spinManager == null && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.5f);
            spinManager = SpinManager.Instance;
            attempts++;
            
            if (spinManager == null)
            {
                Debug.Log($"MoveButtonUI: Attempt {attempts}/{maxAttempts} - SpinManager.Instance not available yet...");
            }
        }
        
        Debug.Log($"MoveButtonUI: SpinManager found = {(spinManager != null)}, spinNumberText assigned = {(spinNumberText != null)}, turnIndicatorText assigned = {(turnIndicatorText != null)}");
        
        if (spinManager != null && spinNumberText != null)
        {
            spinManager.SetTextField(spinNumberText);
            Debug.Log("MoveButtonUI: Assigned spin number text to SpinManager");
        }
        else if (spinManager != null && turnIndicatorText != null)
        {
            // Fallback to turn indicator if spin number text not assigned
            spinManager.SetTextField(turnIndicatorText);
            Debug.Log("MoveButtonUI: Assigned turn indicator text to SpinManager (fallback)");
        }
        else
        {
            Debug.LogWarning("MoveButtonUI: Could not assign text to SpinManager");
            if (spinManager == null)
            {
                Debug.LogWarning("MoveButtonUI: SpinManager.Instance is null - make sure SpinManager is in the scene with NetworkObject component");
            }
            if (spinNumberText == null)
            {
                Debug.LogWarning("MoveButtonUI: spinNumberText is null - please assign the 'Number' GameObject");
            }
        }
    }
    
    void Update()
    {
        UpdateButtonState();
    }
    
    void OnDestroy()
    {
        if (moveButton != null)
        {
            moveButton.onClick.RemoveListener(OnMoveButtonClicked);
        }
    }
    
    private void UpdateButtonState()
    {
        if (turnManager == null) return;
        
        bool isMyTurn = turnManager.IsMyTurn();
        bool isGameStarted = turnManager.IsGameStarted();
        
        // Update button interactability
        if (moveButton != null)
        {
            moveButton.interactable = isMyTurn && isGameStarted;
        }
        
        // Update turn indicator text
        if (turnIndicatorText != null)
        {
            if (!isGameStarted)
            {
                turnIndicatorText.text = "Waiting for game to start...";
                turnIndicatorText.color = Color.gray;
            }
            else if (isMyTurn)
            {
                turnIndicatorText.text = "Your Turn!";
                turnIndicatorText.color = Color.green;
            }
            else
            {
                int currentPlayer = turnManager.GetCurrentTurnPlayer();
                turnIndicatorText.text = $"Player {currentPlayer}'s Turn";
                turnIndicatorText.color = Color.red;
            }
        }
    }
    
    private void OnMoveButtonClicked()
    {
        if (turnManager == null || !turnManager.IsMyTurn()) return;
        
        // Find the local player and trigger movement
        var localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            var playerMovement = localPlayer.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // Trigger the spin and move
                playerMovement.SpinAndMove();
            }
        }
    }
    
    private GameObject FindLocalPlayer()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return null;
        
        var localClient = Unity.Netcode.NetworkManager.Singleton.LocalClient;
        if (localClient != null && localClient.PlayerObject != null)
        {
            return localClient.PlayerObject.gameObject;
        }
        
        return null;
    }
} 