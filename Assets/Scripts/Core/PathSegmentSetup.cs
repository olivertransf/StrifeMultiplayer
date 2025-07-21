using UnityEngine;
using System.Collections.Generic;

public class PathSegmentSetup : MonoBehaviour
{
    [Header("Segment Setup")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private bool createExampleSegments = false;
    
    [Header("Example Segments")]
    [SerializeField] private string[] segmentNames = { "Start", "BranchA", "BranchB", "Converge", "End" };
    
    void Start()
    {
        if (createExampleSegments)
        {
            CreateExamplePathSegments();
        }
    }
    
    [ContextMenu("Create Example Path Segments")]
    public void CreateExamplePathSegments()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("BoardManager not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Clear existing segments
        boardManager.ClearPathSegments();
        
        // Create Start segment
        PathSegment startSegment = new PathSegment("Start");
        startSegment.AddPathPosition(new Vector3(0, 0, 0));
        startSegment.AddPathPosition(new Vector3(1, 0, 0));
        startSegment.AddPathPosition(new Vector3(2, 0, 0));
        // This is where players choose (Stop tiles will be placed here)
        startSegment.AddConnection("BranchA", 0);
        startSegment.AddConnection("BranchB", 0);
        boardManager.AddPathSegment(startSegment);
        
        // Create Branch A
        PathSegment branchA = new PathSegment("BranchA");
        branchA.AddPathPosition(new Vector3(3, 1, 0));
        branchA.AddPathPosition(new Vector3(4, 1, 0));
        branchA.AddPathPosition(new Vector3(5, 1, 0));
        branchA.AddConnection("Converge", 0);
        boardManager.AddPathSegment(branchA);
        
        // Create Branch B
        PathSegment branchB = new PathSegment("BranchB");
        branchB.AddPathPosition(new Vector3(3, -1, 0));
        branchB.AddPathPosition(new Vector3(4, -1, 0));
        branchB.AddPathPosition(new Vector3(5, -1, 0));
        branchB.AddConnection("Converge", 0);
        boardManager.AddPathSegment(branchB);
        
        // Create Converge segment
        PathSegment convergeSegment = new PathSegment("Converge");
        convergeSegment.AddPathPosition(new Vector3(6, 0, 0));
        convergeSegment.AddPathPosition(new Vector3(7, 0, 0));
        convergeSegment.AddConnection("End", 0);
        boardManager.AddPathSegment(convergeSegment);
        
        // Create End segment
        PathSegment endSegment = new PathSegment("End");
        endSegment.AddPathPosition(new Vector3(8, 0, 0));
        endSegment.AddPathPosition(new Vector3(9, 0, 0));
        endSegment.isEndSegment = true;
        boardManager.AddPathSegment(endSegment);
        
        Debug.Log("Example path segments created! Players will start at 'Start', choose between 'BranchA' and 'BranchB', then converge at 'Converge' before reaching 'End'.");
    }
    
    [ContextMenu("Create Simple Linear Path")]
    public void CreateSimpleLinearPath()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("BoardManager not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Clear existing segments
        boardManager.ClearPathSegments();
        
        // Create a simple linear path
        PathSegment linearPath = new PathSegment("Linear");
        for (int i = 0; i < 10; i++)
        {
            linearPath.AddPathPosition(new Vector3(i, 0, 0));
        }
        linearPath.isEndSegment = true;
        boardManager.AddPathSegment(linearPath);
        
        Debug.Log("Simple linear path created with 10 positions!");
    }
    
    [ContextMenu("Create Two Branch Path")]
    public void CreateTwoBranchPath()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("BoardManager not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Clear existing segments
        boardManager.ClearPathSegments();
        
        // Create Start segment
        PathSegment startSegment = new PathSegment("Start");
        startSegment.AddPathPosition(new Vector3(0, 0, 0));
        startSegment.AddPathPosition(new Vector3(1, 0, 0));
        startSegment.AddPathPosition(new Vector3(2, 0, 0));
        // Stop tiles will be placed here for path choices
        startSegment.AddConnection("LeftPath", 0);
        startSegment.AddConnection("RightPath", 0);
        boardManager.AddPathSegment(startSegment);
        
        // Create Left Path
        PathSegment leftPath = new PathSegment("LeftPath");
        leftPath.AddPathPosition(new Vector3(3, 2, 0));
        leftPath.AddPathPosition(new Vector3(4, 2, 0));
        leftPath.AddPathPosition(new Vector3(5, 2, 0));
        leftPath.AddConnection("Final", 0);
        boardManager.AddPathSegment(leftPath);
        
        // Create Right Path
        PathSegment rightPath = new PathSegment("RightPath");
        rightPath.AddPathPosition(new Vector3(3, -2, 0));
        rightPath.AddPathPosition(new Vector3(4, -2, 0));
        rightPath.AddPathPosition(new Vector3(5, -2, 0));
        rightPath.AddConnection("Final", 0);
        boardManager.AddPathSegment(rightPath);
        
        // Create Final segment
        PathSegment finalSegment = new PathSegment("Final");
        finalSegment.AddPathPosition(new Vector3(6, 0, 0));
        finalSegment.AddPathPosition(new Vector3(7, 0, 0));
        finalSegment.isEndSegment = true;
        boardManager.AddPathSegment(finalSegment);
        
        Debug.Log("Two branch path created! Players can choose between 'LeftPath' and 'RightPath'.");
    }
    
    [ContextMenu("Create Career vs College Path")]
    public void CreateCareerVsCollegePath()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("BoardManager not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Clear existing segments
        boardManager.ClearPathSegments();
        
        // Create Start segment (where players begin and choose)
        PathSegment startSegment = new PathSegment("Start");
        startSegment.AddPathPosition(new Vector3(0, 0, 0)); // Starting position
        startSegment.AddConnection("Career", 0);
        startSegment.AddConnection("College", 0);
        boardManager.AddPathSegment(startSegment);
        
        // Create Career Path
        PathSegment careerPath = new PathSegment("Career");
        careerPath.AddPathPosition(new Vector3(1, 1, 0)); // Entry point
        careerPath.AddPathPosition(new Vector3(2, 1, 0));
        careerPath.AddPathPosition(new Vector3(3, 1, 0));
        careerPath.AddPathPosition(new Vector3(4, 1, 0));
        careerPath.AddPathPosition(new Vector3(5, 1, 0));
        careerPath.AddConnection("Converge", 0);
        boardManager.AddPathSegment(careerPath);
        
        // Create College Path
        PathSegment collegePath = new PathSegment("College");
        collegePath.AddPathPosition(new Vector3(1, -1, 0)); // Entry point
        collegePath.AddPathPosition(new Vector3(2, -1, 0));
        collegePath.AddPathPosition(new Vector3(3, -1, 0));
        collegePath.AddPathPosition(new Vector3(4, -1, 0));
        collegePath.AddPathPosition(new Vector3(5, -1, 0));
        collegePath.AddConnection("Converge", 0);
        boardManager.AddPathSegment(collegePath);
        
        // Create Converge segment (where paths meet again)
        PathSegment convergeSegment = new PathSegment("Converge");
        convergeSegment.AddPathPosition(new Vector3(6, 0, 0));
        convergeSegment.AddPathPosition(new Vector3(7, 0, 0));
        convergeSegment.AddPathPosition(new Vector3(8, 0, 0));
        convergeSegment.AddConnection("End", 0);
        boardManager.AddPathSegment(convergeSegment);
        
        // Create End segment
        PathSegment endSegment = new PathSegment("End");
        endSegment.AddPathPosition(new Vector3(9, 0, 0));
        endSegment.AddPathPosition(new Vector3(10, 0, 0));
        endSegment.isEndSegment = true;
        boardManager.AddPathSegment(endSegment);
        
        Debug.Log("Career vs College path created! Players will choose between Career and College paths at the start, then converge before reaching the end.");
    }
    
    [ContextMenu("Print Current Segments")]
    public void PrintCurrentSegments()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
        
        if (boardManager != null)
        {
            List<PathSegment> segments = boardManager.GetPathSegments();
            Debug.Log($"Current segments ({segments.Count}):");
            
            for (int i = 0; i < segments.Count; i++)
            {
                PathSegment segment = segments[i];
                Debug.Log($"  {i}: {segment.segmentName} - {segment.GetPathLength()} positions, End: {segment.isEndSegment}");
                
                foreach (PathConnection connection in segment.GetAvailableConnections())
                {
                    Debug.Log($"    -> {connection.targetSegmentName} (entry: {connection.entryPointIndex})");
                }
            }
        }
        else
        {
            Debug.LogError("BoardManager not found!");
        }
    }
} 