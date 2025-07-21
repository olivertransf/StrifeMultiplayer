using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;

public class BoardManager : NetworkBehaviour
{
    [Header("Path Segments")]
    [SerializeField] private List<PathSegment> pathSegments = new List<PathSegment>();
    
    [Header("Path Choice UI")]
    [SerializeField] private GameObject pathChoicePopupPrefab;
    
    [Header("Tile Position Helper")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase targetTile;
    [SerializeField] private bool useWorldPositions = false; 
    
    [Header("Path Visualization")]
    [SerializeField] private bool showPathGizmo = true;
    [SerializeField] private Color pathColor = Color.green;
    
    // Network variables to track player paths
    private NetworkVariable<PlayerPathData> player1Path = new NetworkVariable<PlayerPathData>(new PlayerPathData("", -1));
    private NetworkVariable<PlayerPathData> player2Path = new NetworkVariable<PlayerPathData>(new PlayerPathData("", -1));

    // Public getter to access path segments from other scripts
    public List<PathSegment> GetPathSegments()
    {
        return pathSegments;
    }
    
    // Get a specific path segment by name
    public PathSegment GetPathSegment(string segmentName)
    {
        foreach (PathSegment segment in pathSegments)
        {
            if (segment.segmentName == segmentName)
            {
                return segment;
            }
        }
        return null;
    }
    
    // Get the first segment (starting segment)
    public PathSegment GetStartingSegment()
    {
        if (pathSegments.Count > 0)
        {
            return pathSegments[0];
        }
        return null;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Ensure NetworkVariables are properly initialized
        if (IsServer)
        {
            // Initialize with default values if they're empty
            if (string.IsNullOrEmpty(player1Path.Value.currentSegmentName))
            {
                player1Path.Value = new PlayerPathData("", -1);
            }
            if (string.IsNullOrEmpty(player2Path.Value.currentSegmentName))
            {
                player2Path.Value = new PlayerPathData("", -1);
            }
        }
    }
    
    // Method to add a path segment
    public void AddPathSegment(PathSegment segment)
    {
        pathSegments.Add(segment);
    }
    
    // Method to remove a path segment by index
    public void RemovePathSegment(int index)
    {
        if (index >= 0 && index < pathSegments.Count)
        {
            pathSegments.RemoveAt(index);
        }
    }
    
    // Method to clear all path segments
    public void ClearPathSegments()
    {
        pathSegments.Clear();
    }
    
    // Get player's current path data
    public PlayerPathData GetPlayerPath(int playerNumber)
    {
        if (playerNumber == 1)
        {
            return player1Path.Value;
        }
        else if (playerNumber == 2)
        {
            return player2Path.Value;
        }
        return new PlayerPathData();
    }
    
    // Get player's current segment ID
    public int GetPlayerSegmentId(int playerNumber)
    {
        PlayerPathData pathData = GetPlayerPath(playerNumber);
        if (string.IsNullOrEmpty(pathData.currentSegmentName)) return -1;
        
        // Find segment ID by name
        for (int i = 0; i < pathSegments.Count; i++)
        {
            if (pathSegments[i].segmentName == pathData.currentSegmentName)
            {
                return i;
            }
        }
        return -1;
    }
    
    // Set player's path data
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerPathServerRpc(int playerNumber, PlayerPathData pathData)
    {
        if (playerNumber == 1)
        {
            player1Path.Value = pathData;
        }
        else if (playerNumber == 2)
        {
            player2Path.Value = pathData;
        }
    }
    
    // Get current position for a player
    public Vector3 GetPlayerCurrentPosition(int playerNumber)
    {
        PlayerPathData pathData = GetPlayerPath(playerNumber);
        PathSegment currentSegment = GetPathSegment(pathData.currentSegmentName);
        
        if (currentSegment != null && pathData.currentSegmentIndex < currentSegment.GetPathLength())
        {
            return currentSegment.GetPositionAtIndex(pathData.currentSegmentIndex);
        }
        
        return Vector3.zero;
    }
    
    // Check if player is on a stop tile (this is now handled by PlayerMovement.IsOnStopTile())
    public bool IsPlayerOnStopTile(int playerNumber)
    {
        // This method is now deprecated - use PlayerMovement.IsOnStopTile() instead
        // Keeping for backward compatibility but it always returns false
        return false;
    }
    
    // Get available connections for a player
    public List<PathConnection> GetPlayerAvailableConnections(int playerNumber)
    {
        PlayerPathData pathData = GetPlayerPath(playerNumber);
        PathSegment currentSegment = GetPathSegment(pathData.currentSegmentName);
        
        if (currentSegment != null)
        {
            return currentSegment.GetAvailableConnections();
        }
        
        return new List<PathConnection>();
    }
    
    // Show path choice popup for a player
    public void ShowPathChoicePopup(int playerNumber, PlayerMovement player)
    {
        if (pathChoicePopupPrefab == null)
        {
            Debug.LogError("Path choice popup prefab not assigned!");
            return;
        }
        
        List<PathConnection> connections = GetPlayerAvailableConnections(playerNumber);
        
        // If no connections from current segment, show all segments as choices (for initial choice)
        if (connections.Count == 0)
        {
            connections = GetAllSegmentsAsConnections();
        }
        
        if (connections.Count == 0)
        {
            Debug.LogWarning($"No available choices for player {playerNumber}");
            return;
        }
        
        // Create the popup (it has its own Canvas component)
        GameObject popup = Instantiate(pathChoicePopupPrefab);
        
        PathChoicePopup pathChoicePopup = popup.GetComponent<PathChoicePopup>();
        
        if (pathChoicePopup != null)
        {
            pathChoicePopup.Initialize(connections, player);
        }
        else
        {
            Debug.LogError("PathChoicePopup component not found on prefab!");
        }
    }
    
    // Get all segments as connections (for initial path choice)
    private List<PathConnection> GetAllSegmentsAsConnections()
    {
        List<PathConnection> allConnections = new List<PathConnection>();
        
        foreach (PathSegment segment in pathSegments)
        {
            // Skip end segments as initial choices
            if (!segment.isEndSegment)
            {
                allConnections.Add(new PathConnection(segment.segmentName, 0));
            }
        }
        
        return allConnections;
    }
    
    // Extract all positions of a specific tile type (legacy method for backward compatibility)
    public void ExtractTilePositions()
    {
        if (tilemap == null || targetTile == null)
        {
            Debug.LogWarning("Please assign both Tilemap and Target Tile in the inspector!");
            return;
        }
        
        // Clear existing segments and create a default one
        pathSegments.Clear();
        PathSegment defaultSegment = new PathSegment("Default");
        
        // Get the bounds of the tilemap
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            if (tile == targetTile)
            {
                Vector3 position;
                if (useWorldPositions)
                {
                    // Convert cell position to world position (center of tile)
                    position = tilemap.GetCellCenterWorld(pos);
                }
                else
                {
                    // Store cell position as Vector3
                    position = new Vector3(pos.x, pos.y, pos.z);
                }
                defaultSegment.AddPathPosition(position);
            }
        }
        
        pathSegments.Add(defaultSegment);
        
        string positionType = useWorldPositions ? "world" : "cell";
        Debug.Log($"Extracted {defaultSegment.GetPathLength()} {positionType} positions for the target tile");
    }
    
    // Get position from mouse click on tilemap (legacy method for backward compatibility)
    public void GetTilePositionFromMouse()
    {
        if (tilemap == null)
        {
            Debug.LogWarning("Please assign Tilemap in the inspector!");
            return;
        }
        
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0; // Set to 0 for 2D
        
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
        
        Vector3 position;
        if (useWorldPositions)
        {
            position = worldPos;
        }
        else
        {
            position = new Vector3(cellPos.x, cellPos.y, cellPos.z);
        }
        
        // Add to the first segment (or create one if none exists)
        if (pathSegments.Count == 0)
        {
            pathSegments.Add(new PathSegment("Default"));
        }
        
        pathSegments[0].AddPathPosition(position);
        
        Debug.Log($"Added position: {position} to segment: {pathSegments[0].segmentName}");
    }

    // Draw path gizmo in Scene view
    void OnDrawGizmos()
    {
        if (!showPathGizmo || pathSegments == null || pathSegments.Count == 0)
            return;
            
        // Set gizmo color
        Gizmos.color = pathColor;
        
        // Draw each segment
        for (int segmentIndex = 0; segmentIndex < pathSegments.Count; segmentIndex++)
        {
            PathSegment segment = pathSegments[segmentIndex];
            
            // Draw number labels at each path position
            for (int i = 0; i < segment.pathPositions.Count; i++)
            {
                Vector3 pos = segment.pathPositions[i];
                
                // Convert cell position to world position if using cell positions
                if (!useWorldPositions && tilemap != null)
                {
                    Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
                    pos = tilemap.GetCellCenterWorld(cellPos);
                }
                
                // Draw number label with segment info
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, $"{segmentIndex}:{i}");
                #endif
            }
            
            // Draw lines connecting the path within this segment
            if (segment.pathPositions.Count > 1)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < segment.pathPositions.Count - 1; i++)
                {
                    Vector3 start = segment.pathPositions[i];
                    Vector3 end = segment.pathPositions[i + 1];
                    
                    // Convert cell positions to world positions if needed
                    if (!useWorldPositions && tilemap != null)
                    {
                        Vector3Int startCell = new Vector3Int(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.y), Mathf.RoundToInt(start.z));
                        Vector3Int endCell = new Vector3Int(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y), Mathf.RoundToInt(end.z));
                        start = tilemap.GetCellCenterWorld(startCell);
                        end = tilemap.GetCellCenterWorld(endCell);
                    }
                    
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}

// Data structure to track a player's path through segments
[System.Serializable]
public struct PlayerPathData : INetworkSerializable
{
    public string currentSegmentName;
    public int currentSegmentIndex;
    public List<string> pathHistory; // History of segments visited
    
    public PlayerPathData(string segmentName, int index)
    {
        currentSegmentName = segmentName;
        currentSegmentIndex = index;
        pathHistory = new List<string>();
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Handle null strings safely
        string segmentName = currentSegmentName ?? "";
        serializer.SerializeValue(ref segmentName);
        if (serializer.IsReader)
        {
            currentSegmentName = segmentName;
        }
        
        serializer.SerializeValue(ref currentSegmentIndex);
        
        // Handle List<string> serialization manually
        if (serializer.IsReader)
        {
            // Reading from network
            int count = 0;
            serializer.SerializeValue(ref count);
            pathHistory = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string item = "";
                serializer.SerializeValue(ref item);
                pathHistory.Add(item);
            }
        }
        else
        {
            // Writing to network
            int count = pathHistory?.Count ?? 0;
            serializer.SerializeValue(ref count);
            if (pathHistory != null)
            {
                for (int i = 0; i < pathHistory.Count; i++)
                {
                    string item = pathHistory[i] ?? "";
                    serializer.SerializeValue(ref item);
                }
            }
        }
    }
}