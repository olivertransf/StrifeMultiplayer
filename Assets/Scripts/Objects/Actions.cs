using UnityEngine;
using Unity.Netcode;
using System;

[System.Serializable]
public class ActionData : INetworkSerializable
{
    public string title;
    public string description;
    public Sprite icon;
    public int moneyChange = 0; // Default to 0 (no change)
    public int babyChange = 0; // Default to 0 (no change)
    
    // Default constructor required by Unity Netcode
    public ActionData()
    {
        title = "";
        description = "";
        moneyChange = 0;
        babyChange = 0;
    }
    
    // Constructor for easy creation
    public ActionData(string title, string description, int moneyChange = 0, int babyChange = 0)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.moneyChange = moneyChange;
        this.babyChange = babyChange;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref description);
        serializer.SerializeValue(ref moneyChange);
        serializer.SerializeValue(ref babyChange);
        // Note: Sprite cannot be serialized over network, so we skip it
    }
}

public class Action : MonoBehaviour
{
    [Header("Action Properties")]
    public ActionData actionData;
    
    [Header("UI References")]
    public UnityEngine.UI.Image iconImage;
    public TMPro.TextMeshProUGUI titleText;
    public TMPro.TextMeshProUGUI descriptionText;
    
    public void Initialize(ActionData data)
    {
        actionData = data;
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        if (actionData == null) return;
        
        if (titleText != null)
            titleText.text = actionData.title;
            
        if (descriptionText != null)
            descriptionText.text = actionData.description;
            
        if (iconImage != null && actionData.icon != null)
            iconImage.sprite = actionData.icon;
    }
    
    public void Execute(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;
        
        // Apply money changes
        if (actionData.moneyChange != 0)
        {
            if (actionData.moneyChange > 0)
                playerInventory.AddMoney(actionData.moneyChange);
            else
                playerInventory.RemoveMoney(Mathf.Abs(actionData.moneyChange));
        }
        
        // Apply baby changes
        if (actionData.babyChange != 0)
        {
            if (actionData.babyChange > 0)
                playerInventory.AddBabies(actionData.babyChange);
            else
                playerInventory.RemoveBabies(Mathf.Abs(actionData.babyChange));
        }
        
        // Action executed successfully
    }
} 