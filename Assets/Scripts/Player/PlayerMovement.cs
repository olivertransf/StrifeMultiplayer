using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private float moveSpeed = 2f;
    
    // Network variables to sync position across clients
    private NetworkVariable<int> currentPathIndex = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false);
    
    // New segment-based variables
    private NetworkVariable<int> currentSegmentId = new NetworkVariable<int>(-1); // -1 means no segment
    private NetworkVariable<int> currentSegmentIndex = new NetworkVariable<int>(0);
    private NetworkVariable<bool> waitingForPathChoice = new NetworkVariable<bool>(false);
    
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
        
        // Initialize player at starting position
        InitializePlayerPosition();
        
        // Subscribe to turn changes to show path choice when game starts
        if (TurnManager.Instance != null)
        {
            StartCoroutine(WaitForGameStartAndShowChoice());
        }
    }
    
    private void InitializePlayerPosition()
    {
        if (boardManager != null)
        {
            // Don't show choice popup here - wait for game to start
            // Players will be positioned when they make their choice
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
        
        // Check if player can move at least one space
        if (!CanMoveAtLeastOneSpace())
        {
            Debug.Log("Player cannot move anymore!");
            CheckForGameEnd();
            return;
        }
        
        // Move to next position
        int newIndex = currentSegmentIndex.Value + 1;
        UpdateSegmentIndexServerRpc(newIndex);
        Vector3 targetCellPos = GetCurrentSegmentPosition(newIndex);
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
        SpinManager spinManager = FindFirstObjectByType<SpinManager>();
        if (spinManager == null) 
        {
            Debug.LogError("PlayerMovement: SpinManager not found!");
            yield break;
        }
        
        Debug.Log($"PlayerMovement: Waiting for spin to complete... (IsServer: {IsServer}, IsClient: {IsClient})");
        
        // Wait for spin to complete
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (!spinManager.IsSpinComplete() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
            
            if (elapsed % 1f < 0.1f) // Log every second
            {
                Debug.Log($"PlayerMovement: Still waiting for spin... ({elapsed:F1}s) - IsSpinComplete: {spinManager.IsSpinComplete()}");
            }
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("PlayerMovement: Spin timeout! Using fallback result.");
        }
        
        // Get the spin result
        int spinResult = GetSpinResult();
        Debug.Log($"PlayerMovement: Final spin result: {spinResult} (IsServer: {IsServer}, IsClient: {IsClient})");
        
        // Move the player
        MoveSpaces(spinResult);
    }
    
    /// <summary>
    /// Get the final spin result
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
        
        // Check if we landed on a stop segment and need to make a path choice
        CheckForPathChoice();
    }
    
    /// <summary>
    /// Smoothly moves the player to the target cell position and ends the turn
    /// </summary>
    private IEnumerator MoveToCellPositionAndEndTurn(Vector3 targetCellPos)
    {
        yield return StartCoroutine(MoveToCellPosition(targetCellPos));
        
        // Check if player landed on a Stop tile - if so, they get another turn
        if (IsOnStopTile())
        {
            yield return new WaitForSeconds(0.5f); // Small delay after movement
            HandleStopTileEffect();
        }
        else
        {
            // End the turn after movement completes
            if (TurnManager.Instance != null)
            {
                yield return new WaitForSeconds(0.5f); // Small delay after movement
                TurnManager.Instance.EndTurn();
            }
        }
    }
    
    /// <summary>
    /// Optional: Method to move multiple spaces (useful for dice rolls)
    /// </summary>
    public void MoveSpaces(int spaces)
    {
        Debug.Log($"PlayerMovement: MoveSpaces called with {spaces} spaces (IsServer: {IsServer}, IsClient: {IsClient})");
        
        // Check if game has ended
        if (TurnManager.Instance != null && TurnManager.Instance.IsGameEnded())
        {
            Debug.Log("Game has ended! Cannot move.");
            return;
        }
        
        if (isMoving.Value || boardManager == null || spaces <= 0) 
        {
            Debug.LogWarning($"PlayerMovement: Cannot move - isMoving: {isMoving.Value}, boardManager: {boardManager != null}, spaces: {spaces}");
            return;
        }
        
        // Check if player can move at least one space
        if (!CanMoveAtLeastOneSpace())
        {
            Debug.Log("Player cannot move anymore!");
            CheckForGameEnd();
            return;
        }
        
        Debug.Log($"PlayerMovement: Starting MoveMultipleSpaces with {spaces} spaces");
        StartCoroutine(MoveMultipleSpaces(spaces));
    }
    
    /// <summary>
    /// Moves the player multiple spaces with consistent timing
    /// </summary>
    private IEnumerator MoveMultipleSpaces(int spaces)
    {
        Debug.Log($"PlayerMovement: MoveMultipleSpaces started with {spaces} spaces (IsServer: {IsServer}, IsClient: {IsClient})");
        
        PathSegment currentSegment = GetCurrentSegment();
        if (currentSegment == null) 
        {
            Debug.LogError("PlayerMovement: Current segment is null!");
            yield break;
        }
        
        Debug.Log($"PlayerMovement: Current segment: {currentSegment.segmentName}, length: {currentSegment.GetPathLength()}, current index: {currentSegmentIndex.Value}");
        
        // Calculate how many spaces we can actually move within current segment
        int maxSpaces = Mathf.Min(spaces, currentSegment.GetPathLength() - 1 - currentSegmentIndex.Value);
        
        Debug.Log($"PlayerMovement: Path calculation - currentIndex={currentSegmentIndex.Value}, segmentLength={currentSegment.GetPathLength()}, spaces={spaces}, maxSpaces={maxSpaces}");
        
        if (maxSpaces <= 0)
        {
            Debug.LogWarning("PlayerMovement: Cannot move further on the current segment!");
            yield break;
        }
        
        // Create a smooth path through all target positions
        Vector3 startPosition = transform.position;
        Vector3[] targetPositions = new Vector3[maxSpaces];
        
        for (int i = 0; i < maxSpaces; i++)
        {
            Vector3 targetCellPos = currentSegment.GetPositionAtIndex(currentSegmentIndex.Value + i + 1);
            
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
        
        // Update current segment index with safety check
        int newIndex = currentSegmentIndex.Value + maxSpaces;
        if (newIndex >= currentSegment.GetPathLength())
        {
            newIndex = currentSegment.GetPathLength() - 1; // Clamp to last position
        }
        
        Debug.Log($"Moved {maxSpaces} spaces out of {spaces} requested. New index will be: {newIndex}");
        
        // Check for game ending conditions BEFORE updating the index
        if (newIndex >= currentSegment.GetPathLength() - 1 && currentSegment.isEndSegment)
        {
            Debug.Log("Player will reach the end of the path! Checking for game end...");
            CheckForGameEnd();
        }
        
        // Update position on server via RPC
        UpdateSegmentIndexServerRpc(newIndex);
        transform.position = targetPositions[targetPositions.Length - 1];
        
        // Log the color of the tile we landed on
        LogLandedTileColor();
        
        // Handle landing events and rewards
        LandingEventHandler.HandleLanding(this, tilemap);
        
        // Check for game ending conditions after movement (in case we landed on End tile)
        CheckForGameEnd();
        
        Debug.Log($"Player position: {currentSegmentIndex.Value}/{currentSegment.GetPathLength() - 1}");
        
        // Check if player landed on a Stop tile - if so, they get another turn
        if (IsOnStopTile())
        {
            yield return new WaitForSeconds(0.5f); // Small delay after movement
            HandleStopTileEffect();
        }
        else
        {
            // End the turn after movement completes (for MoveSpaces method)
            if (TurnManager.Instance != null && !TurnManager.Instance.IsGameEnded())
            {
                yield return new WaitForSeconds(0.5f); // Small delay after movement
                TurnManager.Instance.EndTurn();
            }
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
    /// Get the current segment the player is on
    /// </summary>
    private PathSegment GetCurrentSegment()
    {
        if (boardManager == null || currentSegmentId.Value < 0) return null;
        
        List<PathSegment> segments = boardManager.GetPathSegments();
        if (currentSegmentId.Value < segments.Count)
        {
            return segments[currentSegmentId.Value];
        }
        return null;
    }
    
    /// <summary>
    /// Get position at specific index in current segment
    /// </summary>
    private Vector3 GetCurrentSegmentPosition(int index)
    {
        PathSegment segment = GetCurrentSegment();
        if (segment != null)
        {
            return segment.GetPositionAtIndex(index);
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Set player position in world space
    /// </summary>
    private void SetPlayerPosition(Vector3 position)
    {
        if (tilemap != null)
        {
            Vector3Int cellPos = new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
            transform.position = tilemap.GetCellCenterWorld(cellPos);
        }
        else
        {
            transform.position = position;
        }
    }
    
    /// <summary>
    /// Wait for game to start and show path choice on player's turn
    /// </summary>
    private System.Collections.IEnumerator WaitForGameStartAndShowChoice()
    {
        // Wait for game to start
        while (TurnManager.Instance == null || !TurnManager.Instance.IsGameStarted())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Wait for it to be this player's turn
        while (!TurnManager.Instance.IsMyTurn())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Show path choice popup only on this player's turn
        if (boardManager != null)
        {
            int playerNumber = GetMyPlayerNumber();
            boardManager.ShowPathChoicePopup(playerNumber, this);
            SetWaitingForPathChoiceServerRpc(true);
        }
    }
    
    /// <summary>
    /// Check if player needs to make a path choice
    /// </summary>
    private void CheckForPathChoice()
    {
        // Check if the current tile is a "Stop" tile
        if (IsOnStopTile())
        {
            PathSegment currentSegment = GetCurrentSegment();
            if (currentSegment != null && currentSegment.HasConnections())
            {
                // Show path choice popup
                int playerNumber = GetMyPlayerNumber();
                boardManager.ShowPathChoicePopup(playerNumber, this);
                SetWaitingForPathChoiceServerRpc(true);
            }
        }
    }
    
    /// <summary>
    /// Check if the player is currently on a Stop tile
    /// </summary>
    public bool IsOnStopTile()
    {
        if (tilemap == null) return false;
        
        Vector3Int cellPosition = tilemap.WorldToCell(transform.position);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        if (tile != null)
        {
            bool isStopTile = tile.name.Contains("Stop");
            if (isStopTile)
            {
                Debug.Log($"Player {GetMyPlayerNumber()} landed on Stop tile: {tile.name}");
            }
            return isStopTile;
        }
        
        return false;
    }
    
    /// <summary>
    /// Handle the Stop tile effect - give player another turn
    /// </summary>
    private void HandleStopTileEffect()
    {
        Debug.Log($"Player {GetMyPlayerNumber()} gets another turn due to Stop tile!");
        
        // Add visual feedback - flash the player or show a message
        StartCoroutine(FlashPlayerForStopTile());
        
        // Show path choice popup immediately for another turn
        if (boardManager != null)
        {
            boardManager.ShowPathChoicePopup(GetMyPlayerNumber(), this);
        }
        else
        {
            Debug.LogWarning("BoardManager not found, cannot show path choice popup for Stop tile effect");
        }
    }
    
    /// <summary>
    /// Visual feedback when player gets another turn from Stop tile
    /// </summary>
    private System.Collections.IEnumerator FlashPlayerForStopTile()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        Color flashColor = Color.yellow;
        
        // Flash 3 times
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    /// <summary>
    /// Called when player makes a path choice
    /// </summary>
    public void OnPathChoiceMade(PathConnection chosenConnection)
    {
        if (boardManager == null) return;
        
        // Find the target segment by name
        List<PathSegment> segments = boardManager.GetPathSegments();
        int targetSegmentId = -1;
        PathSegment targetSegment = null;
        
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].segmentName == chosenConnection.targetSegmentName)
            {
                targetSegmentId = i;
                targetSegment = segments[i];
                break;
            }
        }
        
        if (targetSegment == null)
        {
            Debug.LogError($"Target segment '{chosenConnection.targetSegmentName}' not found!");
            return;
        }
        
        // Update player's path data
        PlayerPathData newPathData = new PlayerPathData(chosenConnection.targetSegmentName, chosenConnection.entryPointIndex);
        newPathData.pathHistory = new System.Collections.Generic.List<string>();
        
        // Add current segment to history
        if (currentSegmentId.Value >= 0 && currentSegmentId.Value < segments.Count)
        {
            newPathData.pathHistory.Add(segments[currentSegmentId.Value].segmentName);
        }
        
        // Update on server
        boardManager.SetPlayerPathServerRpc(GetMyPlayerNumber(), newPathData);
        
        // Update local variables
        SetSegmentDataServerRpc(targetSegmentId, chosenConnection.entryPointIndex);
        SetWaitingForPathChoiceServerRpc(false);
        
        // Move player to new position
        Vector3 newPosition = targetSegment.GetPositionAtIndex(chosenConnection.entryPointIndex);
        SetPlayerPosition(newPosition);
        
        Debug.Log($"Player chose path: {chosenConnection.targetSegmentName} at index {chosenConnection.entryPointIndex}");
    }
    
    /// <summary>
    /// Get the current position index on the path
    /// </summary>
    public int GetCurrentPathIndex()
    {
        return currentSegmentIndex.Value;
    }
    
    /// <summary>
    /// Check if player has reached the end of the current segment
    /// </summary>
    public bool HasReachedEnd()
    {
        PathSegment currentSegment = GetCurrentSegment();
        if (currentSegment == null) return false;
        return currentSegmentIndex.Value >= currentSegment.GetPathLength() - 1 && currentSegment.isEndSegment;
    }
    
    /// <summary>
    /// Set the current position on the path (useful for loading game state)
    /// </summary>
    public void SetCurrentPathIndex(int index)
    {
        PathSegment currentSegment = GetCurrentSegment();
        if (currentSegment == null) return;
        
        if (index >= 0 && index < currentSegment.GetPathLength())
        {
            // Use ServerRpc to update the index
            UpdateSegmentIndexServerRpc(index);
            Vector3 targetCellPos = currentSegment.GetPositionAtIndex(index);
            SetPlayerPosition(targetCellPos);
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
    /// ServerRpc to update the segment index (only server can modify NetworkVariables)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void UpdateSegmentIndexServerRpc(int newIndex)
    {
        currentSegmentIndex.Value = newIndex;
        Debug.Log($"Server: Updated player segment index to {newIndex}");
    }
    
    /// <summary>
    /// ServerRpc to set segment data
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SetSegmentDataServerRpc(int segmentId, int index)
    {
        currentSegmentId.Value = segmentId;
        currentSegmentIndex.Value = index;
        Debug.Log($"Server: Updated player segment to ID {segmentId} at index {index}");
    }
    
    /// <summary>
    /// ServerRpc to set waiting for path choice state
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SetWaitingForPathChoiceServerRpc(bool waiting)
    {
        waitingForPathChoice.Value = waiting;
    }
    
    /// <summary>
    /// Check for game ending conditions
    /// </summary>
    private void CheckForGameEnd()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.IsGameEnded()) return;
        
        // Check if this player has reached the end of the path
        if (HasReachedEnd())
        {
            Debug.Log("A player has reached the end of the path! Requesting server to determine winner...");
            RequestGameEndServerRpc();
            return;
        }
        
        // Check if this player landed on the End tile (regardless of position)
        if (IsOnEndTile())
        {
            Debug.Log("A player landed on the End tile! Requesting server to determine winner...");
            RequestGameEndServerRpc();
            return;
        }
        
        // Check if any player has gone bankrupt (negative money)
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerInventory inventory = allPlayers[i].GetComponent<PlayerInventory>();
            if (inventory != null && inventory.GetMoney() < 0)
            {
                // Find the other player (the winner)
                int winnerNumber = 0;
                for (int j = 0; j < allPlayers.Length; j++)
                {
                    if (j != i)
                    {
                        winnerNumber = allPlayers[j].GetMyPlayerNumber();
                        break;
                    }
                }
                Debug.Log($"Player {allPlayers[i].GetMyPlayerNumber()} went bankrupt! Player {winnerNumber} wins!");
                TurnManager.Instance.EndGame(winnerNumber);
                return;
            }
        }
    }
    
    /// <summary>
    /// Check if the player is currently on an End tile
    /// </summary>
    private bool IsOnEndTile()
    {
        if (tilemap == null) return false;
        
        Vector3Int cellPosition = tilemap.WorldToCell(transform.position);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        if (tile != null)
        {
            return tile.name.Contains("End");
        }
        
        return false;
    }
    
    /// <summary>
    /// ServerRpc to request game end
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestGameEndServerRpc()
    {
        if (TurnManager.Instance != null)
        {
            // Determine winner based on player numbers
            int winnerNumber = GetMyPlayerNumber();
            TurnManager.Instance.EndGame(winnerNumber);
        }
    }
    
    /// <summary>
    /// Get the current player number (1 for host, 2 for client)
    /// </summary>
    public int GetMyPlayerNumber()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsHost ? 1 : 2;
    }
    
    /// <summary>
    /// Check if this player can move at least one space
    /// </summary>
    public bool CanMoveAtLeastOneSpace()
    {
        PathSegment currentSegment = GetCurrentSegment();
        if (currentSegment == null) return false;
        return currentSegmentIndex.Value < currentSegment.GetPathLength() - 1;
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