using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

[System.Serializable]
public struct HouseData : IEquatable<HouseData>, INetworkSerializable
{
    public string title;
    public string description;
    public int cost;
    public int redCost;
    public int blackCost;

    public HouseData(string title, string description, int cost, int redCost, int blackCost)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.cost = cost;
        this.redCost = redCost;
        this.blackCost = blackCost;
    }

    public bool Equals(HouseData other)
    {
        return title == other.title && 
               description == other.description && 
               cost == other.cost &&
               redCost == other.redCost &&
               blackCost == other.blackCost;
    }

    public override bool Equals(object obj)
    {
        return obj is HouseData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, cost, redCost, blackCost);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        string titleValue = title ?? "";
        string descriptionValue = description ?? "";
        serializer.SerializeValue(ref titleValue);
        serializer.SerializeValue(ref descriptionValue);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref redCost);
        serializer.SerializeValue(ref blackCost);
        title = titleValue;
        description = descriptionValue;
    }
}

// Network-compatible version using FixedString
[System.Serializable]
public struct NetworkHouseData : IEquatable<NetworkHouseData>, INetworkSerializable
{
    public FixedString64Bytes title;
    public FixedString128Bytes description;
    public int cost;
    public int redCost;
    public int blackCost;

    public NetworkHouseData(string title, string description, int cost, int redCost, int blackCost)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.cost = cost;
        this.redCost = redCost;
        this.blackCost = blackCost;
    }

    public NetworkHouseData(HouseData houseData)
    {
        this.title = houseData.title ?? "";
        this.description = houseData.description ?? "";
        this.cost = houseData.cost;
        this.redCost = houseData.redCost;
        this.blackCost = houseData.blackCost;
    }

    public HouseData ToHouseData()
    {
        return new HouseData(title.ToString(), description.ToString(), cost, redCost, blackCost);
    }

    public bool Equals(NetworkHouseData other)
    {
        return title.Equals(other.title) && 
               description.Equals(other.description) && 
               cost == other.cost &&
               redCost == other.redCost &&
               blackCost == other.blackCost;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkHouseData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, cost, redCost, blackCost);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref description);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref redCost);
        serializer.SerializeValue(ref blackCost);
    }
}

// Simple data class for house information (not a MonoBehaviour)
[System.Serializable]
public class House
{
    public string title;
    public string description;
    public Sprite icon;
    public int cost;
    public int redCost;
    public int blackCost;

    public House(string title, string description, int cost, int redCost, int blackCost)
    {
        this.title = title;
        this.description = description;
        this.cost = cost;
        this.redCost = redCost;
        this.blackCost = blackCost;
    }

    public void Initialize(string title, string description, Sprite icon, int cost, int redCost, int blackCost)
    {
        this.title = title;
        this.description = description;
        this.icon = icon;
        this.cost = cost;
        this.redCost = redCost;
        this.blackCost = blackCost;
    }

    public void Execute(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;
        // Removed houses property reference as requested
    }

    public HouseData ToHouseData()
    {
        return new HouseData(title, description, cost, redCost, blackCost);
    }
}