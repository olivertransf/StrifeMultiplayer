using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class PathChoicePopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Image backgroundImage;
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI descriptionText;
    [SerializeField] public Transform choiceButtonContainer;
    [SerializeField] public GameObject choiceButtonPrefab;
    [SerializeField] public Button closeButton;
    
    [Header("Fallback Button Creation")]
    [SerializeField] private bool createButtonPrefabIfMissing = true;
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private List<PathConnection> availableConnections;
    private PlayerMovement requestingPlayer;
    
    // Static reference to ensure only one popup exists at a time
    private static PathChoicePopup currentPopup;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        rectTransform = GetComponent<RectTransform>();
        
        // Set up close button listener
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }
    
    void OnEnable()
    {
        // If there's already a popup, destroy it
        if (currentPopup != null && currentPopup != this)
        {
            Destroy(currentPopup.gameObject);
        }
        
        // Set this as the current popup
        currentPopup = this;
    }
    
    void OnDestroy()
    {
        // Clear the reference if this popup is being destroyed
        if (currentPopup == this)
        {
            currentPopup = null;
        }
    }
    
    public void Initialize(List<PathConnection> connections, PlayerMovement player)
    {
        availableConnections = connections;
        requestingPlayer = player;
        
        Debug.Log($"PathChoicePopup: Initializing with {connections.Count} connections for player {player.name}");
        
        UpdateUI();
        ShowPopup();
    }
    
    private void UpdateUI()
    {
        if (titleText != null)
            titleText.text = "Choose Your Path";
            
        if (descriptionText != null)
            descriptionText.text = "Select which path you'd like to take next:";
        
        CreateChoiceButtons();
    }
    
    private void CreateChoiceButtons()
    {
        if (choiceButtonContainer == null)
        {
            Debug.LogError("PathChoicePopup: choiceButtonContainer is null!");
            return;
        }
        
        // Create button prefab if it's missing
        if (choiceButtonPrefab == null && createButtonPrefabIfMissing)
        {
            choiceButtonPrefab = CreateChoiceButtonPrefab();
        }
        
        if (choiceButtonPrefab == null)
        {
            Debug.LogError("PathChoicePopup: choiceButtonPrefab is null and could not be created!");
            return;
        }
        
        Debug.Log($"PathChoicePopup: Creating {availableConnections.Count} choice buttons");
        
        // Clear existing buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each available connection
        for (int i = 0; i < availableConnections.Count; i++)
        {
            PathConnection connection = availableConnections[i];
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            
            // Make sure the button is active and visible
            buttonObj.SetActive(true);
            
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();
            
            // Explicitly enable all components
            if (button != null)
            {
                button.enabled = true;
                Debug.Log($"Button {i}: Button enabled = {button.enabled}");
                int index = i; // Capture the index for the lambda
                button.onClick.AddListener(() => OnPathChosen(index));
            }
            else
            {
                Debug.LogError("Button component not found on choice button prefab!");
            }
            
            if (buttonImage != null)
            {
                buttonImage.enabled = true;
                Debug.Log($"Button {i}: Image enabled = {buttonImage.enabled}");
            }
            else
            {
                Debug.LogError("Image component not found on choice button prefab!");
            }
            
            if (buttonText != null)
            {
                buttonText.enabled = true;
                Debug.Log($"Button {i}: Text enabled = {buttonText.enabled}");
                buttonText.text = $"Path {i + 1}: {connection.targetSegmentName}";
                buttonText.color = Color.white; // Ensure text is white
            }
            else
            {
                Debug.LogError("Button text component not found on choice button prefab!");
            }
            
            // Also enable any other UI components that might be disabled
            CanvasGroup buttonCanvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 1f;
                buttonCanvasGroup.interactable = true;
                buttonCanvasGroup.blocksRaycasts = true;
            }
        }
    }
    
    private GameObject CreateChoiceButtonPrefab()
    {
        Debug.Log("PathChoicePopup: Creating choice button prefab programmatically");
        
        // Create the button GameObject
        GameObject buttonPrefab = new GameObject("ChoiceButtonPrefab");
        buttonPrefab.AddComponent<RectTransform>();
        
        // Add Image component for button background
        Image buttonImage = buttonPrefab.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        buttonImage.enabled = true;
        
        // Add Button component
        Button button = buttonPrefab.AddComponent<Button>();
        button.enabled = true;
        
        // Set up RectTransform
        RectTransform buttonRect = buttonPrefab.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 50);
        
        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonPrefab.transform, false);
        textObj.AddComponent<RectTransform>();
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Path Choice";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enabled = true;
        
        // Set up text RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);
        
        // Set up button colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 1f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
        button.colors = colors;
        
        // Ensure the prefab is active
        buttonPrefab.SetActive(true);
        
        return buttonPrefab;
    }
    
    private void OnPathChosen(int choiceIndex)
    {
        if (choiceIndex >= 0 && choiceIndex < availableConnections.Count && requestingPlayer != null)
        {
            PathConnection chosenConnection = availableConnections[choiceIndex];
            
            // Notify the player about their choice
            requestingPlayer.OnPathChoiceMade(chosenConnection);
            
            // Hide the popup
            HidePopup();
        }
    }
    
    private void ShowPopup()
    {
        Debug.Log("PathChoicePopup: ShowPopup called");
        
        // Set initial state
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        
        // Animate in
        StartCoroutine(AnimatePopup(true));
    }
    
    private void HidePopup()
    {
        StartCoroutine(AnimatePopup(false));
    }
    
    private IEnumerator AnimatePopup(bool show)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        float startScale = rectTransform.localScale.x;
        float targetAlpha = show ? 1f : 0f;
        float targetScale = show ? 1f : 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = animationCurve.Evaluate(t);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            float scale = Mathf.Lerp(startScale, targetScale, curveValue);
            rectTransform.localScale = new Vector3(scale, scale, scale);
            
            yield return null;
        }
        
        // Set final values
        canvasGroup.alpha = targetAlpha;
        rectTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
        
        if (!show)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnCloseClicked()
    {
        // If player closes without choosing, default to first option
        if (availableConnections.Count > 0 && requestingPlayer != null)
        {
            OnPathChosen(0);
        }
        else
        {
            HidePopup();
        }
    }
} 