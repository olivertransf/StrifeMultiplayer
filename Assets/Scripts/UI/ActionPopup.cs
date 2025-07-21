using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionPopup : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI playerNameText; // New field to show which player's action
    public Button closeButton;
    
    [Header("Animation")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private ActionData currentAction;
    private PlayerInventory targetPlayer;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    
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
    
    public void Initialize(ActionData actionData, PlayerInventory player)
    {
        currentAction = actionData;
        targetPlayer = player;
        
        UpdateUI();
        ShowPopup();
        
        // Automatically execute the action when popup is shown
        ExecuteAction();
    }
    
    // New method for showing action to all players
    public void InitializeForAllPlayers(ActionData actionData, PlayerInventory player, string playerName)
    {
        Debug.Log($"ActionPopup.InitializeForAllPlayers called - Action: {actionData?.title}, Player: {playerName}");
        
        currentAction = actionData;
        targetPlayer = player;
        
        UpdateUIForAllPlayers(playerName);
        ShowPopup();
        
        // Automatically execute the action when popup is shown
        ExecuteAction();
    }
    
    private void UpdateUI()
    {
        if (currentAction == null) return;
        
        if (titleText != null)
            titleText.text = currentAction.title;
            
        if (descriptionText != null)
            descriptionText.text = currentAction.description;
            
        if (iconImage != null && currentAction.icon != null)
            iconImage.sprite = currentAction.icon;
    }
    
    private void UpdateUIForAllPlayers(string playerName)
    {
        if (currentAction == null) return;
        
        if (titleText != null)
            titleText.text = currentAction.title;
            
        if (descriptionText != null)
            descriptionText.text = currentAction.description;
            
        if (iconImage != null && currentAction.icon != null)
            iconImage.sprite = currentAction.icon;
            
        // Show which player's action this is
        if (playerNameText != null)
            playerNameText.text = $"{playerName}'s Action";
    }
    
    private void ShowPopup()
    {
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
    
    private System.Collections.IEnumerator AnimatePopup(bool show)
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
    
    private void ExecuteAction()
    {
        // Execute the action automatically when popup is shown
        if (currentAction != null && targetPlayer != null)
        {
            ActionManager.Instance.ExecuteAction(currentAction, targetPlayer);
        }
    }
    
    private void OnCloseClicked()
    {
        HidePopup();
    }
    
    // Note: Escape key functionality removed to avoid Input System conflicts
    // Users can close the popup using the Close button instead
} 