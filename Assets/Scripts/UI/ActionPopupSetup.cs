using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionPopupSetup : MonoBehaviour
{
    [Header("Prefab Setup")]
    public bool createPopupPrefab = false;
    
    void Start()
    {
        if (createPopupPrefab)
        {
            CreateActionPopupPrefab();
        }
    }
    
    [ContextMenu("Create Action Popup Prefab")]
    public void CreateActionPopupPrefab()
    {
        // Create the main popup GameObject
        GameObject popup = new GameObject("ActionPopup");
        popup.AddComponent<ActionPopup>();
        
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
        titleText.text = "Action Title";
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
        descText.text = "Action description goes here...";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = Color.black;
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.3f);
        descRect.anchorMax = new Vector2(1, 0.8f);
        descRect.sizeDelta = Vector2.zero;
        descRect.offsetMin = new Vector2(20, 10);
        descRect.offsetMax = new Vector2(-20, -10);
        
        // Create icon
        GameObject iconObj = CreateUIElement("Icon", popupPanel);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.gray;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.3f, 0.4f);
        iconRect.anchorMax = new Vector2(0.7f, 0.7f);
        iconRect.sizeDelta = Vector2.zero;
        

        
        // Create close button
        GameObject closeBtn = CreateUIElement("CloseButton", popupPanel);
        Button closeButton = closeBtn.AddComponent<Button>();
        Image closeImage = closeBtn.AddComponent<Image>();
        closeImage.color = Color.red;
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.3f, 0.1f);
        closeRect.anchorMax = new Vector2(0.7f, 0.25f);
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
        
        // Set up ActionPopup component references
        ActionPopup actionPopup = popup.GetComponent<ActionPopup>();
        actionPopup.backgroundImage = bgImage;
        actionPopup.iconImage = iconImage;
        actionPopup.titleText = titleText;
        actionPopup.descriptionText = descText;
        actionPopup.closeButton = closeButton;
        
        Debug.Log("ActionPopup prefab created! You can now drag this to your Prefabs folder and assign it to ActionManager.");
    }
    
    private GameObject CreateUIElement(string name, GameObject parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
} 