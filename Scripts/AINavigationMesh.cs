// Unity Design Pattern Example: AINavigationMesh
// This script demonstrates the AINavigationMesh pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical example of the **AINavigationMesh design pattern**. It demonstrates how to create a custom, grid-based navigation mesh, implement a simple A* pathfinding algorithm, and have an AI agent utilize this system for movement.

The script is designed to be self-contained within a single `.cs` file, making it easy to drop into any Unity project.

```csharp
// AINavigationMeshPatternExample.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions if using, though not strictly in this simplified A*

// This script demonstrates the AINavigationMesh design pattern in Unity.
// It provides a custom, grid-based navigation mesh system and a simple A* pathfinding algorithm.
//
// To use this example:
// 1. Create an empty GameObject in your scene and name it "NavigationSystem".
// 2. Attach the 'AINavigationMeshSystem' script to it.
// 3. Configure the grid dimensions (Grid Width, Grid Height) and Cell Size in the Inspector.
// 4. In Project Settings -> Layers, add a new Layer called "Obstacle".
// 5. In the 'NavigationSystem' GameObject's Inspector, set the 'Obstacle Layer' to your new "Obstacle" layer.
// 6. Optionally, create a "Ground" layer for your ground plane/terrain and add it to the 'AIAgentController's raycast.
// 7. Place some 3D cubes or other GameObjects in your scene, set their Layer to "Obstacle", and position them on your grid.
// 8. With 'NavigationSystem' selected, click the 'Build Navigation Mesh' button in its Inspector.
//    You should see green (walkable) and red (obstacle) squares drawn in the Scene view.
// 9. Create another empty GameObject and name it "Agent".
// 10. Attach the 'AIAgentController' script to it.
// 11. Assign the "NavigationSystem" GameObject from your Hierarchy to the 'Navigation Mesh System' field of the 'AIAgentController'.
// 12. Run the scene. The agent will attempt to find a path to a random target within the mesh.
//     Click anywhere on the ground plane (or within the grid bounds) to set a new target for the agent.
//     The agent's path will be drawn in blue in the Scene view (if 'Draw Path Gizmos' is enabled).

// --- 1. AINavigationNode: Represents a single traversable unit (e.g., a tile/cell) in the navigation mesh. ---
// This class is the fundamental building block of our custom navigation mesh.
// It stores information about a specific location in the grid, including its walkability
// and properties used by pathfinding algorithms (like A*).
public class AINavigationNode
{
    public int GridX { get; private set; } // X coordinate in the grid
    public int GridY { get; private set; } // Y coordinate in the grid
    public bool IsWalkable { get; set; }   // True if an agent can walk on this node, false if it's an obstacle.

    // Pathfinding specific properties (used by A* algorithm)
    public float GCost { get; set; } // Cost from the start node to this node
    public float HCost { get; set; } // Heuristic cost (estimated cost) from this node to the target node
    public float FCost => GCost + HCost; // Total cost (GCost + HCost) - used for priority in A*

    public AINavigationNode Parent { get; set; } // The node that came before this node in the path

    public AINavigationNode(int x, int y, bool isWalkable)
    {
        GridX = x;
        GridY = y;
        IsWalkable = isWalkable;
    }

    public override string ToString()
    {
        return $"Node({GridX},{GridY}) Walkable: {IsWalkable}";
    }
}

// --- 2. AINavigationMeshSystem: The central class managing the grid data and providing mesh information. ---
// This class embodies the 'AINavigationMesh' design pattern. It's responsible for:
// - Defining the grid's dimensions and cell properties.
// - Building the mesh (detecting obstacles).
// - Providing methods to query the mesh (e.g., get a node from world position, get neighbors).
// - Visualizing the mesh in the editor.
[ExecuteAlways] // Allows the mesh to be built and visualized in the editor without running
public class AINavigationMeshSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Width of the navigation grid in number of cells.")]
    public int GridWidth = 50;
    [Tooltip("Height of the navigation grid in number of cells.")]
    public int GridHeight = 50;
    [Tooltip("Size of each grid cell in world units.")]
    public float CellSize = 1f;
    [Tooltip("Height offset from the origin for raycasting obstacles. Adjust if your ground is not at Y=0.")]
    public float ObstacleRaycastHeightOffset = 10f; // Start raycast from higher up
    [Tooltip("Distance the raycast travels downwards to detect obstacles.")]
    public float ObstacleDetectionDistance = 11f; // Covers 10 units down from offset

    [Header("Obstacle Detection")]
    [Tooltip("Layer masks that will be considered obstacles during mesh generation.")]
    public LayerMask ObstacleLayer;
    [Tooltip("The Y-position at which the navigation mesh cells are considered to exist. Adjust if your terrain is not at Y=0.")]
    public float NavMeshBaseY = 0f;

    private AINavigationNode[,] _grid; // The 2D array representing our navigation mesh
    private Vector3 _gridWorldOrigin;  // World position of the bottom-left corner of the grid

    public AINavigationNode[,] Grid => _grid; // Public accessor for the grid data

    /// <summary>
    /// Builds or rebuilds the navigation mesh based on current settings and obstacles in the scene.
    /// This method can be called from the Editor via the ContextMenu attribute.
    /// </summary>
    [ContextMenu("Build Navigation Mesh")]
    public void BuildNavigationMesh()
    {
        // Calculate the world origin of the grid. It's centered around the GameObject's position.
        _gridWorldOrigin = transform.position - new Vector3(GridWidth * CellSize / 2, 0, GridHeight * CellSize / 2);
        _gridWorldOrigin.y = NavMeshBaseY; // Ensure origin is at the specified Y level

        _grid = new AINavigationNode[GridWidth, GridHeight];

        // Iterate through each cell in the grid to determine its walkability
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // Calculate the world position of the center of the current cell
                Vector3 worldPoint = GridToWorld(x, y);

                // Check for obstacles using a raycast downwards.
                // We cast downwards from a height to ensure we hit obstacles on the ground.
                // Physics.Raycast(startPoint, direction, maxDistance, layerMask)
                bool isObstacle = Physics.Raycast(
                    worldPoint + Vector3.up * ObstacleRaycastHeightOffset, // Start high above the cell
                    Vector3.down,                                           // Cast downwards
                    ObstacleDetectionDistance,                              // Max distance to check
                    ObstacleLayer                                           // Only check for objects on the ObstacleLayer
                );
                
                _grid[x, y] = new AINavigationNode(x, y, !isObstacle);
            }
        }
        Debug.Log($"Navigation Mesh Built: {GridWidth}x{GridHeight} cells. Total nodes: {GridWidth * GridHeight}.");
    }

    /// <summary>
    /// Converts a world position to grid coordinates.
    /// </summary>
    /// <param name="worldPosition">The world space position.</param>
    /// <returns>A Vector2Int representing the grid (X, Y) coordinates.</returns>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        // Adjust world position relative to grid origin, and scale by cell size
        Vector3 relativePos = worldPosition - _gridWorldOrigin;

        // Calculate grid coordinates. FloorToInt ensures we get the correct cell index.
        int x = Mathf.FloorToInt(relativePos.x / CellSize);
        int y = Mathf.FloorToInt(relativePos.z / CellSize); // Using Z for Y in 3D grid

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates to the world position of the center of that cell.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <returns>A Vector3 representing the world position of the cell's center.</returns>
    public Vector3 GridToWorld(int x, int y)
    {
        // Calculate world position based on grid origin and cell size.
        // Add CellSize / 2 to center the point within the cell.
        float worldX = _gridWorldOrigin.x + x * CellSize + CellSize / 2;
        float worldZ = _gridWorldOrigin.z + y * CellSize + CellSize / 2; // Using Z for Y in 3D grid

        // The Y component is fixed to NavMeshBaseY for simplicity in this grid example.
        // For varying terrain, one would typically raycast downwards from this X/Z to get actual terrain height.
        return new Vector3(worldX, NavMeshBaseY, worldZ);
    }

    /// <summary>
    /// Retrieves the AINavigationNode at the given grid coordinates.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <returns>The AINavigationNode if within bounds, otherwise null.</returns>
    public AINavigationNode GetNode(int x, int y)
    {
        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
        {
            return _grid[x, y];
        }
        return null; // Out of bounds
    }

    /// <summary>
    /// Retrieves the AINavigationNode corresponding to a world position.
    /// </summary>
    /// <param name="worldPosition">The world space position.</param>
    /// <returns>The AINavigationNode at that position if within bounds, otherwise null.</returns>
    public AINavigationNode GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector2Int gridCoords = WorldToGrid(worldPosition);
        return GetNode(gridCoords.x, gridCoords.y);
    }

    /// <summary>
    /// Returns a list of all directly adjacent walkable nodes (up, down, left, right, and diagonals).
    /// </summary>
    /// <param name="node">The central node for which to find neighbors.</param>
    /// <returns>A list of walkable neighbor nodes.</returns>
    public List<AINavigationNode> GetNeighbors(AINavigationNode node)
    {
        List<AINavigationNode> neighbors = new List<AINavigationNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip the node itself

                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                // Check if the neighbor is within grid bounds
                if (checkX >= 0 && checkX < GridWidth && checkY >= 0 && checkY < GridHeight)
                {
                    AINavigationNode neighbor = _grid[checkX, checkY];
                    if (neighbor.IsWalkable)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
        }
        return neighbors;
    }

    // --- Visualization (Gizmos) ---
    // OnDrawGizmos is called in the editor to draw visual aids.
    private void OnDrawGizmos()
    {
        // If the mesh hasn't been built or the object is inactive, draw the grid bounds
        if (_grid == null || !gameObject.activeInHierarchy || !enabled)
        {
            Gizmos.color = Color.grey;
            Vector3 center = transform.position;
            center.y = NavMeshBaseY;
            Vector3 size = new Vector3(GridWidth * CellSize, 0.1f, GridHeight * CellSize);
            Gizmos.DrawWireCube(center, size);
            return;
        }

        // Draw each grid cell based on its walkability
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                AINavigationNode node = _grid[x, y];
                Vector3 worldPos = GridToWorld(x, y);

                // Set color: transparent green for walkable, transparent red for obstacles
                Gizmos.color = node.IsWalkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(worldPos, Vector3.one * (CellSize * 0.9f)); // Draw a slightly smaller cube than cell size for separation

                // Draw wireframe for cell boundaries
                Gizmos.color = new Color(0, 1, 1, 0.1f); // Transparent cyan
                Gizmos.DrawWireCube(worldPos, Vector3.one * CellSize); 
            }
        }
    }
}

// --- 3. Pathfinder: Implements the A* algorithm to find paths on the AINavigationMesh. ---
// This static class provides the pathfinding logic, keeping it decoupled from the mesh data itself.
// It takes an AINavigationMeshSystem instance to perform its calculations.
public static class Pathfinder
{
    /// <summary>
    /// Finds a path from a start world position to a target world position using the A* algorithm.
    /// </summary>
    /// <param name="navMesh">The AINavigationMeshSystem to use for pathfinding.</param>
    /// <param name="startWorldPos">The starting world position.</param>
    /// <param name="targetWorldPos">The target world position.</param>
    /// <returns>A list of Vector3 world points representing the path, or an empty list if no path is found.</returns>
    public static List<Vector3> FindPath(AINavigationMeshSystem navMesh, Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        if (navMesh == null || navMesh.Grid == null)
        {
            Debug.LogError("Pathfinder: Navigation Mesh System is null or not built.");
            return new List<Vector3>();
        }

        AINavigationNode startNode = navMesh.GetNodeFromWorldPoint(startWorldPos);
        AINavigationNode targetNode = navMesh.GetNodeFromWorldPoint(targetWorldPos);

        // Validate start and target nodes
        if (startNode == null || targetNode == null)
        {
            Debug.LogError("Pathfinder: Start or target node is outside the grid bounds.");
            return new List<Vector3>();
        }
        if (!startNode.IsWalkable)
        {
            Debug.LogWarning($"Pathfinder: Start node ({startNode.GridX},{startNode.GridY}) is not walkable. Cannot find path.");
            return new List<Vector3>();
        }
        if (!targetNode.IsWalkable)
        {
            Debug.LogWarning($"Pathfinder: Target node ({targetNode.GridX},{targetNode.GridY}) is not walkable. Cannot find path.");
            return new List<Vector3>();
        }

        // --- A* Algorithm Implementation ---

        // Open set: Nodes to be evaluated, sorted by FCost. Using a simple List and sorting/scanning for simplicity.
        // For high performance, a MinHeap/PriorityQueue would be used here.
        List<AINavigationNode> openSet = new List<AINavigationNode>();
        // Closed set: Nodes already evaluated. Using a HashSet for efficient lookups.
        HashSet<AINavigationNode> closedSet = new HashSet<AINavigationNode>();

        openSet.Add(startNode);

        // Reset costs and parent pointers for all nodes in the grid.
        // This is crucial if you reuse the grid for multiple pathfinding requests without recreating it.
        // A more efficient approach for very large grids might be to only reset relevant nodes or use a dictionary.
        foreach (AINavigationNode node in navMesh.Grid)
        {
            node.GCost = float.MaxValue;
            node.HCost = float.MaxValue;
            node.Parent = null;
        }

        startNode.GCost = 0;
        startNode.HCost = GetDistance(startNode, targetNode); // Heuristic: estimate distance to target

        while (openSet.Count > 0)
        {
            // Find the node with the lowest FCost in the open set
            AINavigationNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || // Prioritize lower FCost
                    (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost)) // Tie-break with lower HCost
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // If we reached the target, reconstruct and return the path
            if (currentNode == targetNode)
            {
                return ReconstructPath(navMesh, startNode, targetNode);
            }

            // Evaluate neighbors of the current node
            foreach (AINavigationNode neighbor in navMesh.GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue; // Skip already evaluated nodes
                }

                // Calculate the cost to move from the start node to this neighbor through the current node
                float newMovementCostToNeighbor = currentNode.GCost + GetDistance(currentNode, neighbor); 
                
                // If this new path to the neighbor is shorter than any previously found path
                if (newMovementCostToNeighbor < neighbor.GCost)
                {
                    neighbor.GCost = newMovementCostToNeighbor;         // Update GCost
                    neighbor.HCost = GetDistance(neighbor, targetNode); // Update HCost
                    neighbor.Parent = currentNode;                      // Set current node as parent

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor); // Add neighbor to open set if not already there
                    }
                }
            }
        }

        // If the open set is empty and the target was not reached, no path exists
        Debug.LogWarning("Pathfinder: No path found!");
        return new List<Vector3>();
    }

    /// <summary>
    /// Reconstructs the path from the target node back to the start node using parent pointers.
    /// </summary>
    private static List<Vector3> ReconstructPath(AINavigationMeshSystem navMesh, AINavigationNode startNode, AINavigationNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        AINavigationNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(navMesh.GridToWorld(currentNode.GridX, currentNode.GridY));
            currentNode = currentNode.Parent;
            if (currentNode == null) // Safety break, should not happen if path found
            {
                Debug.LogError("Path reconstruction failed: Parent node was null before reaching start node.");
                return new List<Vector3>();
            }
        }
        path.Add(navMesh.GridToWorld(startNode.GridX, startNode.GridY)); // Add start node itself
        path.Reverse(); // Reverse to get path from start to end
        return path;
    }

    /// <summary>
    /// Calculates the estimated distance between two nodes (heuristic function for A*).
    /// Uses Manhattan distance modified for diagonal movement cost (D * min(dx, dy) + D2 * (max(dx, dy) - min(dx, dy))).
    /// D = 10 (cost for straight movement), D2 = 14 (cost for diagonal movement, approx. sqrt(2)*D).
    /// </summary>
    private static float GetDistance(AINavigationNode nodeA, AINavigationNode nodeB)
    {
        int distX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        int distY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        // Standard diagonal distance heuristic
        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }
}

// --- 4. AIAgentController: An example AI agent that uses the custom navigation mesh. ---
// This MonoBehaviour demonstrates how an AI entity would interact with the AINavigationMeshSystem
// and Pathfinder to request and follow paths.
public class AIAgentController : MonoBehaviour
{
    [Header("Agent Settings")]
    [Tooltip("Reference to the AINavigationMeshSystem in the scene.")]
    public AINavigationMeshSystem NavigationMeshSystem;
    [Tooltip("Movement speed of the agent.")]
    public float MoveSpeed = 5f;
    [Tooltip("How close the agent needs to be to a waypoint to consider it reached.")]
    public float WaypointTolerance = 0.5f;

    [Header("Targeting")]
    [Tooltip("Layer mask for the ground/floor where the agent can receive new targets via mouse click.")]
    public LayerMask GroundLayer;

    [Header("Debug")]
    [Tooltip("Show the agent's current path in the editor.")]
    public bool DrawPathGizmos = true;

    private List<Vector3> _currentPath = new List<Vector3>();
    private int _currentPathIndex = 0;
    private Vector3 _targetWorldPosition;
    private bool _hasTarget = false;

    private void Start()
    {
        if (NavigationMeshSystem == null)
        {
            Debug.LogError("AIAgentController: Navigation Mesh System not assigned! Please assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Example: Find an initial random target when the game starts
        SetRandomTarget();
    }

    private void Update()
    {
        // Handle input to set a new target (for testing and user interaction)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // Raycast against both GroundLayer and ObstacleLayer to determine click target
            if (Physics.Raycast(ray, out hit, 100f, GroundLayer | NavigationMeshSystem.ObstacleLayer))
            {
                SetNewTarget(hit.point);
            }
        }

        if (_hasTarget && _currentPath != null && _currentPath.Count > 0)
        {
            MoveAlongPath();
        }
        else if (_hasTarget && (_currentPath == null || _currentPath.Count == 0))
        {
            // If we have a target but no path (e.g., path not found or cleared), try finding one again.
            // This might happen if the nav mesh was re-built or an obstacle moved.
            SetNewTarget(_targetWorldPosition); // Re-attempt pathfinding to the last known target
        }
        // If !hasTarget, the agent is idle.
    }

    /// <summary>
    /// Sets a new target for the agent and initiates pathfinding.
    /// </summary>
    /// <param name="targetPosition">The world position of the new target.</param>
    public void SetNewTarget(Vector3 targetPosition)
    {
        _targetWorldPosition = targetPosition;
        _hasTarget = true;
        
        // Request a new path from the Pathfinder using our NavigationMeshSystem.
        _currentPath = Pathfinder.FindPath(NavigationMeshSystem, transform.position, _targetWorldPosition);
        _currentPathIndex = 0; // Reset path index to start from the beginning

        if (_currentPath.Count == 0)
        {
            Debug.LogWarning($"Agent: Failed to find path to {_targetWorldPosition}. Target might be unreachable or off-mesh.");
            _hasTarget = false; // Clear target if no path found
        }
        else
        {
            Debug.Log($"Agent: Path found with {_currentPath.Count} waypoints.");
        }
    }

    /// <summary>
    /// Sets a random walkable target within the navigation mesh's bounds.
    /// </summary>
    private void SetRandomTarget()
    {
        if (NavigationMeshSystem == null || NavigationMeshSystem.Grid == null) return;

        AINavigationNode randomNode = null;
        int maxAttempts = 100; // Prevent infinite loops if no walkable nodes exist
        for (int i = 0; i < maxAttempts; i++)
        {
            int randX = Random.Range(0, NavigationMeshSystem.GridWidth);
            int randY = Random.Range(0, NavigationMeshSystem.GridHeight);
            AINavigationNode node = NavigationMeshSystem.GetNode(randX, randY);
            if (node != null && node.IsWalkable)
            {
                randomNode = node;
                break;
            }
        }

        if (randomNode != null)
        {
            SetNewTarget(NavigationMeshSystem.GridToWorld(randomNode.GridX, randomNode.GridY));
        }
        else
        {
            Debug.LogWarning("Agent: Could not find a random walkable node for target.");
        }
    }

    /// <summary>
    /// Moves the agent along the current path, waypoint by waypoint.
    /// </summary>
    private void MoveAlongPath()
    {
        if (_currentPathIndex >= _currentPath.Count)
        {
            // Reached the end of the path
            _hasTarget = false;
            _currentPath.Clear(); // Clear the path
            Debug.Log("Agent: Reached target.");
            SetRandomTarget(); // Optionally, set a new random target after reaching the current one
            return;
        }

        // Get the current waypoint the agent is moving towards
        Vector3 currentWaypoint = _currentPath[_currentPathIndex];
        // Ensure the agent stays on its current Y level unless specifically moving vertically
        // For this example, we assume flat movement, so we fix Y to agent's current Y.
        currentWaypoint.y = transform.position.y; 

        // Move the agent towards the current waypoint
        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, MoveSpeed * Time.deltaTime);

        // Check if agent is close enough to the current waypoint to consider it reached
        if (Vector3.Distance(transform.position, currentWaypoint) < WaypointTolerance)
        {
            _currentPathIndex++; // Move to the next waypoint in the path
        }
    }

    // --- Visualization (Gizmos) ---
    // OnDrawGizmos is called in the editor to draw visual aids for the agent's path.
    private void OnDrawGizmos()
    {
        if (!DrawPathGizmos || _currentPath == null || _currentPath.Count == 0) return;

        Gizmos.color = Color.blue; // Color for waypoints
        for (int i = 0; i < _currentPath.Count; i++)
        {
            Vector3 point = _currentPath[i];
            point.y += 0.1f; // Draw slightly above ground for visibility

            Gizmos.DrawSphere(point, 0.2f); // Draw waypoint as a sphere

            if (i > 0)
            {
                Vector3 prevPoint = _currentPath[i - 1];
                prevPoint.y += 0.1f;
                Gizmos.DrawLine(prevPoint, point); // Draw a line connecting waypoints
            }
        }

        // Draw a line from the agent's current position to the first waypoint in its path
        if (_currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            // Adjust Y slightly to make it visible above the agent's base
            Gizmos.DrawLine(transform.position, _currentPath[0] + Vector3.up * 0.1f);
        }
    }
}
```