using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PathSegment
{
    [Header("Segment Info")]
    public string segmentName;
    public List<Vector3> pathPositions = new List<Vector3>();
    
    [Header("Segment Connections")]
    public List<PathConnection> connections = new List<PathConnection>();
    
    [Header("Segment Properties")]
    public bool isEndSegment = false; // If true, this is the final segment
    
    public PathSegment(string name)
    {
        segmentName = name;
    }
    
    public void AddPathPosition(Vector3 position)
    {
        pathPositions.Add(position);
    }
    
    public void AddConnection(string targetSegmentName, int entryPointIndex)
    {
        connections.Add(new PathConnection(targetSegmentName, entryPointIndex));
    }
    
    public List<PathConnection> GetAvailableConnections()
    {
        return connections;
    }
    
    public bool HasConnections()
    {
        return connections.Count > 0;
    }
    
    public Vector3 GetPositionAtIndex(int index)
    {
        if (index >= 0 && index < pathPositions.Count)
        {
            return pathPositions[index];
        }
        return Vector3.zero;
    }
    
    public int GetPathLength()
    {
        return pathPositions.Count;
    }
}

[System.Serializable]
public class PathConnection
{
    public string targetSegmentName;
    public int entryPointIndex; // Index in the target segment where player enters
    
    public PathConnection(string target, int entryIndex)
    {
        targetSegmentName = target;
        entryPointIndex = entryIndex;
    }
} 