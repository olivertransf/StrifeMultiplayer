using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Path Positions")]
    [SerializeField] private List<Vector3> pathPositions = new List<Vector3>();
    
    [Header("Tile Position Helper")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase targetTile;
    [SerializeField] private bool useWorldPositions = false; 
    
    [Header("Path Visualization")]
    [SerializeField] private bool showPathGizmo = true;
    [SerializeField] private Color pathColor = Color.green;

    // public void Update()
    // {
    //     // Only get tile position when mouse is clicked
    //     if (Mouse.current.leftButton.wasPressedThisFrame)
    //     {
    //         GetTilePositionFromMouse();
    //     }
    // }

    // Public getter to access path positions from other scripts
    public List<Vector3> GetPathPositions()
    {
        return pathPositions;
    }
    
    // Method to add a path position
    public void AddPathPosition(Vector3 position)
    {
        pathPositions.Add(position);
    }
    
    // Method to remove a path position by index
    public void RemovePathPosition(int index)
    {
        if (index >= 0 && index < pathPositions.Count)
        {
            pathPositions.RemoveAt(index);
        }
    }
    
    // Method to clear all path positions
    public void ClearPathPositions()
    {
        pathPositions.Clear();
    }
    
    // Extract all positions of a specific tile type
    public void ExtractTilePositions()
    {
        if (tilemap == null || targetTile == null)
        {
            Debug.LogWarning("Please assign both Tilemap and Target Tile in the inspector!");
            return;
        }
        
        pathPositions.Clear();
        
        // Get the bounds of the tilemap
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            if (tile == targetTile)
            {
                if (useWorldPositions)
                {
                    // Convert cell position to world position (center of tile)
                    Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
                    pathPositions.Add(worldPos);
                }
                else
                {
                    // Store cell position as Vector3
                    pathPositions.Add(new Vector3(pos.x, pos.y, pos.z));
                }
            }
        }
        
        string positionType = useWorldPositions ? "world" : "cell";
        Debug.Log($"Extracted {pathPositions.Count} {positionType} positions for the target tile");
    }
    
    // Get position from mouse click on tilemap
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
        
        if (useWorldPositions)
        {
            pathPositions.Add(worldPos);
            Debug.Log($"Added world position: {worldPos} at cell: {cellPos}");
        }
        else
        {
            pathPositions.Add(new Vector3(cellPos.x, cellPos.y, cellPos.z));
            Debug.Log($"Added cell position: {cellPos} (world: {worldPos})");
        }
    }

    // Draw path gizmo in Scene view
    void OnDrawGizmos()
    {
        if (!showPathGizmo || pathPositions == null || pathPositions.Count == 0)
            return;
            
        // Set gizmo color
        Gizmos.color = pathColor;
        
        // Draw number labels at each path position
        for (int i = 0; i < pathPositions.Count; i++)
        {
            Vector3 pos = pathPositions[i];
            
            // Convert cell position to world position if using cell positions
            if (!useWorldPositions && tilemap != null)
            {
                Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
                pos = tilemap.GetCellCenterWorld(cellPos);
            }
            
            // Draw number label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, i.ToString());
            #endif
        }
        
        // Draw lines connecting the path
        if (pathPositions.Count > 1)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < pathPositions.Count - 1; i++)
            {
                Vector3 start = pathPositions[i];
                Vector3 end = pathPositions[i + 1];
                
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