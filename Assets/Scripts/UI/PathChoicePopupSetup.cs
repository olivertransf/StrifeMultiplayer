using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathChoicePopupSetup : MonoBehaviour
{
    [Header("Prefab Setup")]
    public bool createPopupPrefab = false;
    
    void Start()
    {
        if (createPopupPrefab)
        {
            CreatePathChoicePopupPrefab();
        }
    }
    
    [ContextMenu("Create Path Choice Popup Prefab")]
    public void CreatePathChoicePopupPrefab()
    {
        // Create the main popup GameObject
        GameObject popup = new GameObject("PathChoicePopup");
        popup.AddComponent<PathChoicePopup>();
        
        // Add Canvas components
        Canvas canvas = popup.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // High priority
        
        popup.AddComponent<CanvasScaler>();
        popup.AddComponent<GraphicRaycaster>();
        
        // Add CanvasGroup for animations
        popup.AddComponent<CanvasGroup>();
        
        // Create background panel
        GameObject background = CreateUIElement("Background", popup);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Create main popup panel
        GameObject popupPanel = CreateUIElement("PopupPanel", popup);
        Image panelImage = popupPanel.AddComponent<Image>();
        panelImage.color = Color.white;
        RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.2f);
        panelRect.anchorMax = new Vector2(0.8f, 0.8f);
        panelRect.sizeDelta = Vector2.zero;
        
        // Create title
        GameObject titleObj = CreateUIElement("Title", popupPanel);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Choose Your Path";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.offsetMin = new Vector2(10, 10);
        titleRect.offsetMax = new Vector2(-10, -10);
        
        // Create description
        GameObject descObj = CreateUIElement("Description", popupPanel);
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Select which path you'd like to take next:";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = Color.black;
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.6f);
        descRect.anchorMax = new Vector2(1, 0.8f);
        descRect.sizeDelta = Vector2.zero;
        descRect.offsetMin = new Vector2(20, 10);
        descRect.offsetMax = new Vector2(-20, -10);
        
        // Create choice button container
        GameObject choiceContainer = CreateUIElement("ChoiceContainer", popupPanel);
        RectTransform choiceRect = choiceContainer.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(0, 0.2f);
        choiceRect.anchorMax = new Vector2(1, 0.6f);
        choiceRect.sizeDelta = Vector2.zero;
        choiceRect.offsetMin = new Vector2(20, 10);
        choiceRect.offsetMax = new Vector2(-20, -10);
        
        // Add VerticalLayoutGroup to choice container
        VerticalLayoutGroup layoutGroup = choiceContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        
        // Create choice button prefab (as a separate object, not child of container)
        GameObject choiceButtonPrefab = new GameObject("ChoiceButtonPrefab");
        choiceButtonPrefab.AddComponent<RectTransform>();
        
        // Add Image component for button background
        Image choiceImage = choiceButtonPrefab.AddComponent<Image>();
        choiceImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        choiceImage.enabled = true; // Ensure it's enabled
        
        // Add Button component
        Button choiceButton = choiceButtonPrefab.AddComponent<Button>();
        choiceButton.enabled = true; // Ensure it's enabled
        
        // Set up RectTransform
        RectTransform choiceButtonRect = choiceButtonPrefab.GetComponent<RectTransform>();
        choiceButtonRect.sizeDelta = new Vector2(0, 50);
        
        // Create button text
        GameObject buttonTextObj = CreateUIElement("Text", choiceButtonPrefab);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Path Choice";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enabled = true; // Ensure it's enabled
        
        // Set up text RectTransform
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.offsetMin = new Vector2(10, 5); // Add some padding
        buttonTextRect.offsetMax = new Vector2(-10, -5);
        
        // Set up button colors
        ColorBlock colors = choiceButton.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 1f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
        choiceButton.colors = colors;
        
        // Ensure the prefab is active
        choiceButtonPrefab.SetActive(true);
        
        // Create close button
        GameObject closeBtn = CreateUIElement("CloseButton", popupPanel);
        Button closeButton = closeBtn.AddComponent<Button>();
        Image closeImage = closeBtn.AddComponent<Image>();
        closeImage.color = Color.red;
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.3f, 0.05f);
        closeRect.anchorMax = new Vector2(0.7f, 0.15f);
        closeRect.sizeDelta = Vector2.zero;
        
        GameObject closeTextObj = CreateUIElement("Text", closeBtn);
        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "Close";
        closeText.fontSize = 18;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        
        // Set up PathChoicePopup component references
        PathChoicePopup pathChoicePopup = popup.GetComponent<PathChoicePopup>();
        pathChoicePopup.backgroundImage = bgImage;
        pathChoicePopup.titleText = titleText;
        pathChoicePopup.descriptionText = descText;
        pathChoicePopup.choiceButtonContainer = choiceContainer.transform;
        pathChoicePopup.choiceButtonPrefab = choiceButtonPrefab;
        pathChoicePopup.closeButton = closeButton;
        
        Debug.Log("PathChoicePopup prefab created! You can now drag this to your Prefabs folder and assign it to BoardManager.");
    }
    
    private GameObject CreateUIElement(string name, GameObject parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
} 