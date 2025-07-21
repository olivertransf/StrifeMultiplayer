using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HouseChoicePopup : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button house1Button;
    public Button house2Button;
    public Button skipButton;

    private House house1;
    private House house2;
    private PlayerInventory targetInventory;
    private PlayerMovement requestingPlayer;

    /// <summary>
    /// Initialize the popup with two house options and the player inventory.
    /// </summary>
    public void Initialize(House h1, House h2, PlayerInventory inventory, PlayerMovement player)
    {
        house1 = h1;
        house2 = h2;
        targetInventory = inventory;
        requestingPlayer = player;

        SetupDefaultUI();
        Show();
    }

    private void SetupDefaultUI()
    {
        // If any of the references are missing (e.g. when instantiated programmatically)
        // we create a very basic UI so the popup is still functional.
        if (backgroundImage == null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.7f);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
        }

        if (titleText == null)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 24;
            RectTransform tRT = titleObj.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.1f, 0.8f);
            tRT.anchorMax = new Vector2(0.9f, 0.95f);
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        }
        titleText.text = "Choose a House";

        if (descriptionText == null)
        {
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(transform, false);
            descriptionText = descObj.AddComponent<TextMeshProUGUI>();
            descriptionText.alignment = TextAlignmentOptions.Center;
            descriptionText.fontSize = 18;
            RectTransform dRT = descObj.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0.1f, 0.7f);
            dRT.anchorMax = new Vector2(0.9f, 0.8f);
            dRT.offsetMin = dRT.offsetMax = Vector2.zero;
        }
        descriptionText.text = "Pick one of the houses below to purchase or skip.";

        // Container for buttons
        Transform container = transform;

        if (house1Button == null)
        {
            house1Button = CreateButton(container, "House1Button");
        }
        if (house2Button == null)
        {
            house2Button = CreateButton(container, "House2Button");
        }
        if (skipButton == null)
        {
            skipButton = CreateButton(container, "SkipButton");
        }

        // Position buttons vertically
        PositionButton(house1Button.GetComponent<RectTransform>(), 0.5f, 0.6f);
        PositionButton(house2Button.GetComponent<RectTransform>(), 0.35f, 0.45f);
        PositionButton(skipButton.GetComponent<RectTransform>(), 0.15f, 0.25f);

        // Set button texts
        SetButtonText(house1Button, $"{house1.title}\n${house1.cost}");
        SetButtonText(house2Button, $"{house2.title}\n${house2.cost}");
        SetButtonText(skipButton, "Skip");

        // Add listeners
        house1Button.onClick.AddListener(() => OnHouseChosen(house1));
        house2Button.onClick.AddListener(() => OnHouseChosen(house2));
        skipButton.onClick.AddListener(OnSkip);
    }

    private Button CreateButton(Transform parent, string name)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = true;
        return btn;
    }

    private void PositionButton(RectTransform rect, float anchorMinY, float anchorMaxY)
    {
        rect.anchorMin = new Vector2(0.2f, anchorMinY);
        rect.anchorMax = new Vector2(0.8f, anchorMaxY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void SetButtonText(Button button, string text)
    {
        TextMeshProUGUI txt = button.GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
        }
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.text = text;
        RectTransform tRT = txt.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
    }

    private void Show()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = 1f; // instantly visible
    }

    private void OnHouseChosen(House house)
    {
        if (targetInventory == null) { Close(); return; }

        if (targetInventory.HasEnoughMoney(house.cost))
        {
            targetInventory.RemoveMoney(house.cost);
            targetInventory.AddHouse(house);
        }
        else
        {
            // Not enough money; could add feedback here
            Debug.Log($"Not enough money to purchase {house.title}");
        }
        Close();
    }

    private void OnSkip()
    {
        Close();
    }

    private void Close()
    {
        Destroy(gameObject);
    }
} 