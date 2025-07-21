using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

[System.Serializable]
public struct BabyData : IEquatable<BabyData>, INetworkSerializable
{
    public string title;
    public string description;
    public bool isMale;
    public int age; // Age in months

    public BabyData(string title, string description, bool isMale, int age = 0)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.isMale = isMale;
        this.age = age;
    }

    public bool Equals(BabyData other)
    {
        return title == other.title && 
               description == other.description && 
               isMale == other.isMale &&
               age == other.age;
    }

    public override bool Equals(object obj)
    {
        return obj is BabyData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, isMale, age);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        string titleValue = title ?? "";
        string descriptionValue = description ?? "";
        serializer.SerializeValue(ref titleValue);
        serializer.SerializeValue(ref descriptionValue);
        serializer.SerializeValue(ref isMale);
        serializer.SerializeValue(ref age);
        title = titleValue;
        description = descriptionValue;
    }
}

// Network-compatible version using FixedString
[System.Serializable]
public struct NetworkBabyData : IEquatable<NetworkBabyData>, INetworkSerializable
{
    public FixedString64Bytes title;
    public FixedString128Bytes description;
    public bool isMale;
    public int age;

    public NetworkBabyData(string title, string description, bool isMale, int age = 0)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.isMale = isMale;
        this.age = age;
    }

    public NetworkBabyData(BabyData babyData)
    {
        this.title = babyData.title ?? "";
        this.description = babyData.description ?? "";
        this.isMale = babyData.isMale;
        this.age = babyData.age;
    }

    public BabyData ToBabyData()
    {
        return new BabyData(title.ToString(), description.ToString(), isMale, age);
    }

    public bool Equals(NetworkBabyData other)
    {
        return title.Equals(other.title) && 
               description.Equals(other.description) && 
               isMale == other.isMale &&
               age == other.age;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkBabyData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, isMale, age);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref description);
        serializer.SerializeValue(ref isMale);
        serializer.SerializeValue(ref age);
    }
}

// Simple data class for baby information (not a MonoBehaviour)
[System.Serializable]
public class Baby
{
    public string title;
    public string description;
    public Sprite icon;
    public bool isMale;
    public int age;

    public Baby(string title, string description, bool isMale, int age = 0)
    {
        this.title = title;
        this.description = description;
        this.isMale = isMale;
        this.age = age;
    }

    public void Initialize(string title, string description, Sprite icon, bool isMale, int age = 0)
    {
        this.title = title;
        this.description = description;
        this.icon = icon;
        this.isMale = isMale;
        this.age = age;
    }

    public BabyData ToBabyData()
    {
        return new BabyData(title, description, isMale, age);
    }
} 