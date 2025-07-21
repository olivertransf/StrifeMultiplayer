using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class ItemPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private Button closeButton;
    
    [Header("Animation")]
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Mouse mouse;
    
    // Static reference to ensure only one popup exists at a time
    private static ItemPopup currentPopup;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        rectTransform = GetComponent<RectTransform>();
        
        // Set up close button listener
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
            
        // Get reference to mouse input
        mouse = Mouse.current;
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
    
    public void InitializeHouse(HouseData house, string playerName)
    {
        if (titleText != null)
            titleText.text = house.title;
            
        if (descriptionText != null)
            descriptionText.text = house.description;
            
        if (playerNameText != null)
            playerNameText.text = $"{playerName}'s House";
            
        if (detailsText != null)
        {
            if (house.title.Contains("All Houses"))
            {
                detailsText.text = $"Total Houses: {house.cost}";
            }
            else
            {
                detailsText.text = $"Cost: ${house.cost}";
            }
        }
        
        // Note: Icon would need to be set up in the prefab or loaded dynamically
        // if (iconImage != null && house.icon != null)
        //     iconImage.sprite = house.icon;
        
        ShowPopup();
    }
    
    public void InitializeJob(JobData job, string playerName)
    {
        if (titleText != null)
            titleText.text = job.title;
            
        if (descriptionText != null)
            descriptionText.text = job.description;
            
        if (playerNameText != null)
            playerNameText.text = $"{playerName}'s Job";
            
        if (detailsText != null)
        {
            string details = $"Salary: ${job.salary}/month\nSpin Number: {job.rollNumber}";
            if (job.requiresEducation)
                details += "\nRequires Education";
            detailsText.text = details;
        }
        
        // Note: Icon would need to be set up in the prefab or loaded dynamically
        // if (iconImage != null && job.icon != null)
        //     iconImage.sprite = job.icon;
        
        ShowPopup();
    }
    
    public void InitializeBaby(BabyData baby, string playerName)
    {
        if (titleText != null)
            titleText.text = baby.title;
            
        if (descriptionText != null)
            descriptionText.text = baby.description;
            
        if (playerNameText != null)
            playerNameText.text = $"{playerName}'s Baby";
            
        if (detailsText != null)
        {
            if (baby.title.Contains("All Babies"))
            {
                detailsText.text = $"Total Babies: {baby.age}";
            }
            else
            {
                string gender = baby.isMale ? "Boy" : "Girl";
                string details = $"Gender: {gender}\nAge: {baby.age} months";
                detailsText.text = details;
            }
        }
        
        // Note: Icon would need to be set up in the prefab or loaded dynamically
        // if (iconImage != null && baby.icon != null)
        //     iconImage.sprite = baby.icon;
        
        ShowPopup();
    }
    
    private void ShowPopup()
    {
        // Show popup instantly - no animation, don't touch background
        // The prefab's background and all settings remain exactly as set in inspector
        canvasGroup.alpha = 1f; // Ensure popup is visible
    }
    
    private void HidePopup()
    {
        // Hide popup instantly - no animation
        Destroy(gameObject);
    }
    
    private void OnCloseClicked()
    {
        HidePopup();
    }
    
    // Close popup when clicking outside (optional)
    void Update()
    {
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            // Check if click is outside the popup
            Vector2 mousePosition = mouse.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition))
            {
                HidePopup();
            }
        }
    }
} 