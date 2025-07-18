using UnityEngine;
using TMPro;
using Unity.Netcode;

public class SpinManager : NetworkBehaviour
{
    public TMP_Text text;
    
    // Singleton pattern
    public static SpinManager Instance { get; private set; }
    
    // Public getter for the final spin result
    public int GetFinalNumber()
    {
        int result = finalNumber.Value;
        
        // Validate the result is within expected range
        if (result < 1 || result > 10)
        {
            Debug.LogError($"SpinManager: Invalid finalNumber.Value: {result}! Expected 1-10. This might indicate a network sync issue.");
        }
        
        Debug.Log($"SpinManager[{GetInstanceID()}]: GetFinalNumber() called, returning: {result} (IsSpinComplete: {spinComplete.Value}, IsServer: {IsServer}, IsClient: {IsClient})");
        return result;
    }
    
    // Public getter to check if spin is complete
    public bool IsSpinComplete()
    {
        return spinComplete.Value;
    }
    
    // Public method to set the text field
    public void SetTextField(TMP_Text textField)
    {
        text = textField;
        Debug.Log($"SpinManager[{GetInstanceID()}]: Text field assigned {(text != null ? "successfully" : "failed")} to text field: {(textField != null ? textField.name : "null")}");
    }
    
    public float spinSpeed = 10f;
    public float spinDuration = 1f; // How long to spin before landing
    
    // Network variables to sync spin state
    private NetworkVariable<bool> isSpinning = new NetworkVariable<bool>(false);
    private NetworkVariable<int> finalNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> spinComplete = new NetworkVariable<bool>(false);
    private NetworkVariable<float> spinStartTime = new NetworkVariable<float>(0f);
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"SpinManager[{GetInstanceID()}]: Set as singleton instance");
        }
        else
        {
            Debug.LogWarning($"SpinManager[{GetInstanceID()}]: Another instance already exists ({Instance.GetInstanceID()}), destroying this one");
            Destroy(gameObject);
            return;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"SpinManager[{GetInstanceID()}]: OnNetworkSpawn called (IsServer: {IsServer}, IsClient: {IsClient})");
        
        // If this is not the singleton instance, disable it
        if (Instance != this)
        {
            Debug.LogWarning($"SpinManager[{GetInstanceID()}]: Not the singleton instance, disabling");
            enabled = false;
            return;
        }
        
        // Both server and client should show the spinning animation
        // The server controls when to stop, but clients can see the spinning
        Debug.Log($"SpinManager[{GetInstanceID()}]: Network spawn complete - {(IsServer ? "Server" : "Client")} instance ready");
        
        // Try to find text field if not assigned
        if (text == null)
        {
            text = FindFirstObjectByType<TMP_Text>();
            if (text != null)
            {
                Debug.Log($"SpinManager[{GetInstanceID()}]: Found text field automatically: {text.name}");
            }
            else
            {
                Debug.LogWarning($"SpinManager[{GetInstanceID()}]: No text field found! Please assign a TMP_Text component.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSpinning.Value)
        {
            // Calculate elapsed time since spin started (synchronized across network)
            float elapsedTime = Time.time - spinStartTime.Value;
            
            // Calculate progress (0 to 1)
            float progress = elapsedTime / spinDuration;
            
            // Slow down the cycling as we approach the end
            float currentSpeed = spinSpeed * (1f - progress * 0.8f);
            
            // Cycle through numbers 1-10 (ensure positive result)
            float timeValue = Time.time * currentSpeed;
            int currentNumber = Mathf.FloorToInt(Mathf.Abs(timeValue) % 10) + 1;
            
            // Show the spinning animation
            if (text != null)
            {
                text.text = currentNumber.ToString();
                // Debug: Only log occasionally to avoid spam
                if (Time.frameCount % 100 == 0) // Log every 100 frames (about once per second)
                {
                    Debug.Log($"SpinManager[{GetInstanceID()}]: Update: Cycling number = {currentNumber}, text.text = '{text.text}' (timeValue: {timeValue}, IsServer: {IsServer})");
                }
            }
            
            // Only the server controls when the spin stops and sets the final number
            if (IsServer && elapsedTime >= spinDuration)
            {
                StopSpinServerRpc();
            }
        }
        else if (spinComplete.Value && text != null)
        {
            // When spin is complete, show the final number from the server
            text.text = finalNumber.Value.ToString();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void StartSpinServerRpc()
    {
        // Prevent multiple spins from starting simultaneously
        if (isSpinning.Value)
        {
            Debug.LogWarning("Server: Spin already in progress, ignoring new spin request");
            return;
        }
        
        isSpinning.Value = true;
        spinComplete.Value = false;
        int newNumber = Random.Range(1, 11); // Random number 1-10
        finalNumber.Value = newNumber;
        spinStartTime.Value = Time.time;
        
        Debug.Log($"Server: Starting spin, generated number: {newNumber}, finalNumber.Value: {finalNumber.Value} at time {spinStartTime.Value}");
        Debug.Log($"Server: isSpinning.Value = {isSpinning.Value}, text field = {(text != null ? "assigned" : "null")}");
    }
    
    [ServerRpc(RequireOwnership = false)]
    void StopSpinServerRpc()
    {
        Debug.Log($"Server: StopSpinServerRpc called, setting isSpinning.Value = false");
        isSpinning.Value = false;
        spinComplete.Value = true;
        
        if (text != null)
        {
            text.text = finalNumber.Value.ToString();
            Debug.Log($"SpinManager[{GetInstanceID()}]: Server: Set text to '{text.text}' for finalNumber.Value={finalNumber.Value}");
        }
        
        Debug.Log($"Server: Spin complete, final number is {finalNumber.Value}, spinComplete.Value = {spinComplete.Value}");
        
        // Notify all clients of the final result
        Debug.Log($"Server: Sending finalResult={finalNumber.Value} to clients via ClientRpc");
        NotifySpinCompleteClientRpc(finalNumber.Value);
    }
    
    [ClientRpc]
    void NotifySpinCompleteClientRpc(int finalResult)
    {
        Debug.Log($"Client: Spin complete notification received, final number is {finalResult} (raw value: {finalResult})");
        
        if (text != null)
        {
            text.text = finalResult.ToString();
            Debug.Log($"SpinManager[{GetInstanceID()}]: Client: Set text to '{text.text}' for finalResult={finalResult}");
        }
        else
        {
            Debug.LogWarning($"SpinManager[{GetInstanceID()}]: Client: Text field is null, cannot display final result");
        }
    }
    
    // Public method to trigger a new spin
    public void Spin()
    {
        // Only allow spinning if we're the server or if we have a valid network connection
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            StartSpinServerRpc();
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // Client requests spin from server
            StartSpinServerRpc();
        }
        else
        {
            // Fallback for local testing or when not networked
            Debug.Log("SpinManager: No network connection, using local spin");
            StartLocalSpin();
        }
    }
    
    // Local fallback method for when not networked
    private void StartLocalSpin()
    {
        isSpinning.Value = true;
        spinComplete.Value = false;
        finalNumber.Value = Random.Range(1, 11);
        spinStartTime.Value = Time.time;
        
        Debug.Log($"Local: Starting spin, final number will be {finalNumber.Value}");
    }
}