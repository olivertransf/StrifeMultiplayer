using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private float moveSpeed = 2f;
    
    // Network variables to sync position across clients
    private NetworkVariable<int> currentPathIndex = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false);
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find BoardManager if not assigned
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
        
        // Find Tilemap if not assigned
        if (tilemap == null)
        {
            tilemap = FindFirstObjectByType<Tilemap>();
        }
        
        // Position player at starting position
        if (boardManager != null && boardManager.GetPathPositions().Count > 0)
        {
            Vector3 startPos = boardManager.GetPathPositions()[currentPathIndex.Value];
            // Convert to world position if using cell positions
            if (tilemap != null)
            {
                Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.y), Mathf.RoundToInt(startPos.z));
                transform.position = tilemap.GetCellCenterWorld(cellPos);
            }
            else
            {
                transform.position = startPos;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Public method to move one space forward - can be called from buttons
    /// </summary>
    public void MoveOneSpace()
    {
        // Check if game has ended
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameEnded())
        {
            Debug.Log("Game has ended! Cannot move.");
            return;
        }
        
        // Check if it's this player's turn
        if (TurnManager.Instance != null && !TurnManager.Instance.IsMyTurn())
        {
            Debug.Log("Not your turn!");
            return;
        }
        
        if (isMoving.Value || boardManager == null) return;
        
        var pathPositions = boardManager.GetPathPositions();
        if (pathPositions.Count == 0) return;
        
        // Check if player can move at least one space
        if (!CanMoveAtLeastOneSpace())
        {
            Debug.Log("Player cannot move anymore!");
            CheckForGameEnd();
            return;
        }
        
        // Check if we're at the last index
        if (currentPathIndex.Value >= pathPositions.Count - 1)
        {
            Debug.Log("Player has reached the end of the path!");
            CheckForGameEnd();
            return;
        }
        
        // Move to next position - use ServerRpc
        int newIndex = currentPathIndex.Value + 1;
        UpdatePathIndexServerRpc(newIndex);
        Vector3 targetCellPos = pathPositions[newIndex];
        StartCoroutine(MoveToCellPositionAndEndTurn(targetCellPos));
    }
    
    /// <summary>
    /// Spin and move based on the spin result
    /// </summary>
    public void SpinAndMove()
    {
        // Check if game has ended
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameEnded())
        {
            Debug.Log("Game has ended! Cannot move.");
            return;
        }
        
        // Check if it's this player's turn
        if (TurnManager.Instance != null && !TurnManager.Instance.IsMyTurn())
        {
            Debug.Log("Not your turn!");
            return;
        }
        
        if (isMoving.Value || boardManager == null) return;
        
        // Check if player can move at least one space
        if (!CanMoveAtLeastOneSpace())
        {
            Debug.Log("Player cannot move anymore!");
            CheckForGameEnd();
            return;
        }
        
        // Find SpinManager in the scene
        SpinManager spinManager = FindFirstObjectByType<SpinManager>();
        if (spinManager == null) return;
        
        // Start the spin
        Debug.Log($"PlayerMovement: Starting spin, my turn: {TurnManager.Instance.IsMyTurn()}, current turn: {TurnManager.Instance.GetCurrentTurnPlayer()}");
        spinManager.Spin();
        
        // Wait for spin to complete, then move
        StartCoroutine(WaitForSpinAndMove());
    }
    
    /// <summary>
    /// Waits for spin to complete, then moves the player
    /// </summary>
    private IEnumerator WaitForSpinAndMove()
    {
        // Find SpinManager in the scene
        SpinManager spinManager = FindFirstObjectByType<SpinManager>();
        if (spinManager == null) yield break;
        
        // Wait for spin duration plus a small buffer
        yield return new WaitForSeconds(spinManager.spinDuration + 0.1f);
        
        // Wait for spin to actually complete (for network synchronization)
        float waitTime = 0f;
        float maxWaitTime = 2f; // Maximum wait time to prevent infinite loop
        
        while (!spinManager.IsSpinComplete() && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (waitTime >= maxWaitTime)
        {
            Debug.LogWarning("SpinManager: Timeout waiting for spin to complete!");
        }
        
        // Get the final spin number (1-10)
        int spinResult = GetSpinResult();
        
        // Move the player based on spin result
        MoveSpaces(spinResult);
        
        Debug.Log($"Spin result: {spinResult}, moving {spinResult} spaces");
        
        // Note: Turn ending is handled in MoveMultipleSpaces() after movement completes
    }
    
    /// <summary>
    /// Get the current spin result from the SpinManager
    /// </summary>
    private int GetSpinResult()
    {
        SpinManager spinManager = FindFirstObjectByType<SpinManager>();
        if (spinManager != null)
        {
            int result = spinManager.GetFinalNumber();
            Debug.Log($"PlayerMovement: Getting spin result: {result} (IsSpinComplete: {spinManager.IsSpinComplete()}, IsServer: {IsServer}, IsClient: {IsClient})");
            
            // Validate the result
            if (result < 1 || result > 10)
            {
                Debug.LogError($"PlayerMovement: Invalid spin result: {result}! Expected 1-10.");
            }
            
            return result;
        }
        
        // Fallback to random number 1-10 if we can't get the actual result
        Debug.LogWarning("PlayerMovement: SpinManager not found, using fallback random number");
        return Random.Range(1, 11);
    }
    
    /// <summary>
    /// Smoothly moves the player to the target cell position
    /// </summary>
    private IEnumerator MoveToCellPosition(Vector3 targetCellPos)
    {
        isMoving.Value = true;
        Vector3 startPosition = transform.position;
        Vector3 targetWorldPos;
        
        // Convert cell position to world position
        if (tilemap != null)
        {
            Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(targetCellPos.x), Mathf.RoundToInt(targetCellPos.y), Mathf.RoundToInt(targetCellPos.z));
            targetWorldPos = tilemap.GetCellCenterWorld(cellPos);
        }
        else
        {
            targetWorldPos = targetCellPos;
        }
        
        float journey = 0f;
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPosition, targetWorldPos, journey);
            yield return null;
        }
        
        transform.position = targetWorldPos;
        isMoving.Value = false;
        
        // Log the color of the tile we landed on
        LogLandedTileColor();

        // Handle landing events and rewards
        LandingEventHandler.HandleLanding(this, tilemap);
    }
    
    /// <summary>
    /// Smoothly moves the player to the target cell position and ends the turn
    /// </summary>
    private IEnumerator MoveToCellPositionAndEndTurn(Vector3 targetCellPos)
    {
        yield return StartCoroutine(MoveToCellPosition(targetCellPos));
        
        // End the turn after movement completes
        if (TurnManager.Instance != null)
        {
            yield return new WaitForSeconds(0.5f); // Small delay after movement
            TurnManager.Instance.EndTurn();
        }
    }
    
    /// <summary>
    /// Optional: Method to move multiple spaces (useful for dice rolls)
    /// </summary>
    public void MoveSpaces(int spaces)
    {
        // Check if game has ended
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameEnded())
        {
            Debug.Log("Game has ended! Cannot move.");
            return;
        }
        
        if (isMoving.Value || boardManager == null || spaces <= 0) return;
        
        // Check if player can move at least one space
        if (!CanMoveAtLeastOneSpace())
        {
            Debug.Log("Player cannot move anymore!");
            CheckForGameEnd();
            return;
        }
        
        StartCoroutine(MoveMultipleSpaces(spaces));
    }
    
    /// <summary>
    /// Moves the player multiple spaces with consistent timing
    /// </summary>
    private IEnumerator MoveMultipleSpaces(int spaces)
    {
        var pathPositions = boardManager.GetPathPositions();
        
        // Calculate how many spaces we can actually move
        int maxSpaces = Mathf.Min(spaces, pathPositions.Count - 1 - currentPathIndex.Value);
        
        Debug.Log($"Path calculation: currentIndex={currentPathIndex.Value}, pathLength={pathPositions.Count}, spaces={spaces}, maxSpaces={maxSpaces}");
        
        if (maxSpaces <= 0)
        {
            Debug.Log("Player has reached the end of the path!");
            yield break;
        }
        
        // Create a smooth path through all target positions
        Vector3 startPosition = transform.position;
        Vector3[] targetPositions = new Vector3[maxSpaces];
        
        for (int i = 0; i < maxSpaces; i++)
        {
            Vector3 targetCellPos = pathPositions[currentPathIndex.Value + i + 1];
            
            // Convert cell position to world position
            if (tilemap != null)
            {
                Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(targetCellPos.x), Mathf.RoundToInt(targetCellPos.y), Mathf.RoundToInt(targetCellPos.z));
                targetPositions[i] = tilemap.GetCellCenterWorld(cellPos);
            }
            else
            {
                targetPositions[i] = targetCellPos;
            }
        }
        
        // Move through all positions smoothly
        float totalDistance = 0f;
        for (int i = 0; i < targetPositions.Length; i++)
        {
            Vector3 currentPos = (i == 0) ? startPosition : targetPositions[i - 1];
            totalDistance += Vector3.Distance(currentPos, targetPositions[i]);
        }
        
        float journey = 0f;
        float speed = totalDistance / (maxSpaces * (1f / moveSpeed)); 
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * speed;
            float clampedJourney = Mathf.Clamp01(journey);
            
            // Calculate position along the path
            Vector3 newPosition = CalculatePositionAlongPath(startPosition, targetPositions, clampedJourney);
            transform.position = newPosition;
            
            yield return null;
        }
        
        // Update current path index with safety check - use ServerRpc
        int newIndex = currentPathIndex.Value + maxSpaces;
        if (newIndex >= pathPositions.Count)
        {
            newIndex = pathPositions.Count - 1; // Clamp to last position
        }
        
        // Update position on server via RPC
        UpdatePathIndexServerRpc(newIndex);
        transform.position = targetPositions[targetPositions.Length - 1];
        
        Debug.Log($"Moved {maxSpaces} spaces out of {spaces} requested. New index: {currentPathIndex.Value}");
        
        // Log the color of the tile we landed on
        LogLandedTileColor();
        
        // Handle landing events and rewards
        LandingEventHandler.HandleLanding(this, tilemap);
        
        // Check if we reached the end
        if (currentPathIndex.Value >= pathPositions.Count - 1)
        {
            Debug.Log($"Player has reached the end of the path! Index: {currentPathIndex.Value}, Path length: {pathPositions.Count}");
            // Check for game ending conditions
            CheckForGameEnd();
        }
        else
        {
            Debug.Log($"Player position: {currentPathIndex.Value}/{pathPositions.Count - 1}");
        }
        
        // End the turn after movement completes (for MoveSpaces method)
        if (TurnManager.Instance != null && !TurnManager.Instance.IsGameEnded())
        {
            yield return new WaitForSeconds(0.5f); // Small delay after movement
            TurnManager.Instance.EndTurn();
        }
    }
    
    /// <summary>
    /// Calculate position along a path of waypoints
    /// </summary>
    private Vector3 CalculatePositionAlongPath(Vector3 start, Vector3[] waypoints, float t)
    {
        if (waypoints.Length == 0) return start;
        
        float totalLength = Vector3.Distance(start, waypoints[0]);
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            totalLength += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        
        float targetDistance = totalLength * t;
        float currentDistance = 0f;
        
        // Check if we're between start and first waypoint
        float segmentLength = Vector3.Distance(start, waypoints[0]);
        if (targetDistance <= segmentLength)
        {
            float segmentT = targetDistance / segmentLength;
            return Vector3.Lerp(start, waypoints[0], segmentT);
        }
        currentDistance += segmentLength;
        
        // Check intermediate waypoints
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            segmentLength = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            if (targetDistance <= currentDistance + segmentLength)
            {
                float segmentT = (targetDistance - currentDistance) / segmentLength;
                return Vector3.Lerp(waypoints[i], waypoints[i + 1], segmentT);
            }
            currentDistance += segmentLength;
        }
        
        // If we get here, we're at the end
        return waypoints[waypoints.Length - 1];
    }
    
    /// <summary>
    /// Get the current position index on the path
    /// </summary>
    public int GetCurrentPathIndex()
    {
        return currentPathIndex.Value;
    }
    
    /// <summary>
    /// Check if player has reached the end of the path
    /// </summary>
    public bool HasReachedEnd()
    {
        if (boardManager == null) return false;
        var pathPositions = boardManager.GetPathPositions();
        return currentPathIndex.Value >= pathPositions.Count - 1;
    }
    
    /// <summary>
    /// Set the current position on the path (useful for loading game state)
    /// </summary>
    public void SetCurrentPathIndex(int index)
    {
        if (boardManager == null) return;
        
        var pathPositions = boardManager.GetPathPositions();
        if (index >= 0 && index < pathPositions.Count)
        {
            // Use ServerRpc to update the index
            UpdatePathIndexServerRpc(index);
            Vector3 targetCellPos = pathPositions[index];
            
            // Convert to world position if using cell positions
            if (tilemap != null)
            {
                Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(targetCellPos.x), Mathf.RoundToInt(targetCellPos.y), Mathf.RoundToInt(targetCellPos.z));
                transform.position = tilemap.GetCellCenterWorld(cellPos);
            }
            else
            {
                transform.position = targetCellPos;
            }
        }
    }
    
    /// <summary>
    /// Get the current cell position the player is on
    /// </summary>
    public Vector3Int GetCurrentCellPosition()
    {
        if (tilemap != null)
        {
            return tilemap.WorldToCell(transform.position);
        }
        return Vector3Int.zero;
    }
    
    /// <summary>
    /// ServerRpc to update the path index (only server can modify NetworkVariables)
    /// </summary>
    [ServerRpc]
    private void UpdatePathIndexServerRpc(int newIndex)
    {
        currentPathIndex.Value = newIndex;
        Debug.Log($"Server: Updated player path index to {newIndex}");
    }
    
    /// <summary>
    /// Check for game ending conditions
    /// </summary>
    private void CheckForGameEnd()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.IsGameEnded()) return;
        
        // Check if this player has reached the end
        if (HasReachedEnd())
        {
            int myPlayerNumber = GetMyPlayerNumber();
            Debug.Log($"Player {myPlayerNumber} has reached the end! Game Over!");
            TurnManager.Instance.EndGame(myPlayerNumber);
            return;
        }
        
        // Check if the other player has reached the end
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player != this && player.HasReachedEnd())
            {
                int otherPlayerNumber = player.GetMyPlayerNumber();
                Debug.Log($"Other player {otherPlayerNumber} has reached the end! Game Over!");
                TurnManager.Instance.EndGame(otherPlayerNumber);
                return;
            }
        }
        
        // Check if both players can't move anymore (optional - for tie scenarios)
        bool canAnyPlayerMove = false;
        foreach (var player in allPlayers)
        {
            if (player.CanMoveAtLeastOneSpace())
            {
                canAnyPlayerMove = true;
                break;
            }
        }
        
        if (!canAnyPlayerMove)
        {
            Debug.Log("No player can move anymore! It's a tie!");
            TurnManager.Instance.EndGame(0); // 0 = tie
        }
    }
    
    /// <summary>
    /// Get the current player number (1 for host, 2 for client)
    /// </summary>
    private int GetMyPlayerNumber()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsHost ? 1 : 2;
    }
    
    /// <summary>
    /// Check if this player can move at least one space
    /// </summary>
    public bool CanMoveAtLeastOneSpace()
    {
        if (boardManager == null) return false;
        var pathPositions = boardManager.GetPathPositions();
        return currentPathIndex.Value < pathPositions.Count - 1;
    }
    
    /// <summary>
    /// Log the color of the tile the player landed on
    /// </summary>
    private void LogLandedTileColor()
    {
        if (tilemap == null) return;
        
        Vector3Int cellPosition = tilemap.WorldToCell(transform.position);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        if (tile != null)
        {
            string tileName = tile.name;
            Debug.Log($"Player landed on tile: {tileName}");
        }
        else
        {
            Debug.Log("Player landed on empty space");
        }
    }
    
}