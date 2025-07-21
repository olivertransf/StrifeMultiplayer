using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

[System.Serializable]
public struct JobData : IEquatable<JobData>, INetworkSerializable
{
    public string title;
    public string description;
    public int salary;
    public int rollNumber;
    public bool requiresEducation;

    public JobData(string title, string description, int salary, int rollNumber = 0, bool requiresEducation = false)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.salary = salary;
        this.rollNumber = rollNumber;
        this.requiresEducation = requiresEducation;
    }

    public bool Equals(JobData other)
    {
        return title == other.title && 
               description == other.description && 
               salary == other.salary && 
               rollNumber == other.rollNumber &&
               requiresEducation == other.requiresEducation;
    }

    public override bool Equals(object obj)
    {
        return obj is JobData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, salary, rollNumber, requiresEducation);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        string titleValue = title ?? "";
        string descriptionValue = description ?? "";
        serializer.SerializeValue(ref titleValue);
        serializer.SerializeValue(ref descriptionValue);
        serializer.SerializeValue(ref salary);
        serializer.SerializeValue(ref rollNumber);
        serializer.SerializeValue(ref requiresEducation);
        title = titleValue;
        description = descriptionValue;
    }
}

// Network-compatible version using FixedString
[System.Serializable]
public struct NetworkJobData : IEquatable<NetworkJobData>, INetworkSerializable
{
    public FixedString64Bytes title;
    public FixedString128Bytes description;
    public int salary;
    public int rollNumber;
    public bool requiresEducation;

    public NetworkJobData(string title, string description, int salary, int rollNumber = 0, bool requiresEducation = false)
    {
        this.title = title ?? "";
        this.description = description ?? "";
        this.salary = salary;
        this.rollNumber = rollNumber;
        this.requiresEducation = requiresEducation;
    }

    public NetworkJobData(JobData jobData)
    {
        this.title = jobData.title ?? "";
        this.description = jobData.description ?? "";
        this.salary = jobData.salary;
        this.rollNumber = jobData.rollNumber;
        this.requiresEducation = jobData.requiresEducation;
    }

    public JobData ToJobData()
    {
        return new JobData(title.ToString(), description.ToString(), salary, rollNumber, requiresEducation);
    }

    public bool Equals(NetworkJobData other)
    {
        return title.Equals(other.title) && 
               description.Equals(other.description) && 
               salary == other.salary && 
               rollNumber == other.rollNumber &&
               requiresEducation == other.requiresEducation;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkJobData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(title, description, salary, rollNumber, requiresEducation);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref description);
        serializer.SerializeValue(ref salary);
        serializer.SerializeValue(ref rollNumber);
        serializer.SerializeValue(ref requiresEducation);
    }
}

// Simple data class for job information (not a MonoBehaviour)
[System.Serializable]
public class Job
{
    public string title;
    public string description;
    public Sprite icon;
    public int rollNumber;
    public int salary;
    public bool requiresEducation;

    public Job(string title, string description, int salary, int rollNumber = 0, bool requiresEducation = false)
    {
        this.title = title;
        this.description = description;
        this.salary = salary;
        this.rollNumber = rollNumber;
        this.requiresEducation = requiresEducation;
    }

    public void Initialize(string title, string description, Sprite icon, int salary, int rollNumber = 0, bool requiresEducation = false)
    {
        this.title = title;
        this.description = description;
        this.icon = icon;
        this.salary = salary;
        this.rollNumber = rollNumber;
        this.requiresEducation = requiresEducation;
    }

    public void Execute(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return;
        playerInventory.isEmployed.Value = true;
    }

    public JobData ToJobData()
    {
        return new JobData(title, description, salary, rollNumber, requiresEducation);
    }
}