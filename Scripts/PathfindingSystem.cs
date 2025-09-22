// Unity Design Pattern Example: PathfindingSystem
// This script demonstrates the PathfindingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the 'PathfindingSystem' design pattern. It provides a centralized, asynchronous service for pathfinding requests, making it practical and scalable for real-world Unity projects.

The example uses Unity's NavMesh for pathfinding, but the system is designed to be easily extensible for other algorithms (e.g., A* on a grid) using a Strategy pattern (explained in the comments).

---

### **1. PathfindingSystem.cs**

This script defines the core `PathfindingSystem` and the `PathRequest` structure. It acts as a central manager (Singleton) that processes pathfinding requests from a queue to prevent performance spikes.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMesh functionality
using System;       // Required for Action
using System.Collections.Generic; // Required for List and Queue
using System.Linq; // Required for .ToList()

/// <summary>
/// Represents a single pathfinding request made to the PathfindingSystem.
/// </summary>
public struct PathRequest
{
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public Action<Vector3[], bool> callback; // Callback for when the path is found (path, success)

    public PathRequest(Vector3 start, Vector3 target, Action<Vector3[], bool> onPathFound)
    {
        startPosition = start;
        targetPosition = target;
        callback = onPathFound;
    }
}

/// <summary>
/// This class implements the 'PathfindingSystem' design pattern.
/// It acts as a central service for all pathfinding requests in the game.
/// Using a Singleton pattern ensures there's one global access point.
/// It uses a queue to process path requests asynchronously (over multiple frames)
/// to prevent frame drops, especially if pathfinding algorithms were more complex.
/// </summary>
public class PathfindingSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global access point to the PathfindingSystem instance.
    public static PathfindingSystem Instance { get; private set; }

    // --- Request Queue ---
    // A queue to hold incoming pathfinding requests.
    // This allows path requests to be processed over time, rather than all at once,
    // which can prevent performance spikes (frame drops) when many agents request paths simultaneously.
    private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();

    // --- Processing State ---
    // Flag to ensure only one pathfinding request is actively being processed at a time.
    // This is especially useful if pathfinding involves coroutines or background threads.
    private bool isProcessingPath = false;

    [Tooltip("How many path requests to process per frame. Set to 0 for unlimited, 1 for one per frame.")]
    [SerializeField]
    private int requestsPerFrame = 1; 
    private int processedCountThisFrame = 0;


    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance of PathfindingSystem exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PathfindingSystem instances found! Destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make it persist across scene loads if pathfinding needs to be global.
            // DontDestroyOnLoad(gameObject); 
        }
    }

    private void Update()
    {
        // Reset the count of requests processed this frame.
        processedCountThisFrame = 0;

        // Process requests from the queue until it's empty or we hit the per-frame limit.
        while (pathRequestQueue.Count > 0 && 
               (requestsPerFrame == 0 || processedCountThisFrame < requestsPerFrame))
        {
            // If already processing, wait for the current one to finish.
            // This flag is mainly for more complex async operations (e.g., coroutines or threads)
            // but for synchronous NavMesh.CalculatePath, it ensures a single sequential processing.
            if (!isProcessingPath)
            {
                ProcessNextRequest();
                processedCountThisFrame++;
            }
            else
            {
                // If a path is currently being processed, break and try again next frame.
                // This scenario would typically happen if ProcessNextRequest used a Coroutine or Task.
                break; 
            }
        }
    }

    // --- Public API for Requesting Paths ---

    /// <summary>
    /// Enqueues a pathfinding request to be processed by the system.
    /// Other game objects (agents) will call this method to get a path.
    /// </summary>
    /// <param name="start">The starting position for the path.</param>
    /// <param name="target">The target position for the path.</param>
    /// <param name="callback">A callback function to invoke when the path is found.
    /// The callback receives a Vector3[] array representing the path corners and a boolean indicating success.</param>
    public void RequestPath(Vector3 start, Vector3 target, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(start, target, callback);
        pathRequestQueue.Enqueue(newRequest);
    }

    // --- Internal Path Processing Logic ---

    /// <summary>
    /// Dequeues and processes the next pathfinding request.
    /// This method is called repeatedly from `Update` to handle requests incrementally.
    /// </summary>
    private void ProcessNextRequest()
    {
        if (pathRequestQueue.Count > 0)
        {
            isProcessingPath = true; // Mark that we are starting processing
            PathRequest currentRequest = pathRequestQueue.Dequeue();

            // Perform the actual pathfinding. For this example, we use Unity's built-in NavMesh.
            // This is the point where you would typically delegate to a specific pathfinding algorithm
            // (e.g., A*, Dijkstra, or a custom one) if you were using a Strategy pattern.
            FindPathWithNavMesh(currentRequest.startPosition, currentRequest.targetPosition, currentRequest.callback);

            // For synchronous operations like NavMesh.CalculatePath, we can immediately
            // set isProcessingPath to false. If using Coroutines or Tasks for async processing,
            // this flag would be reset in the callback or upon completion of the async operation.
            isProcessingPath = false; 
        }
    }

    /// <summary>
    /// Finds a path using Unity's NavMesh system.
    /// This method is a concrete implementation of a pathfinding algorithm within the system.
    /// </summary>
    /// <param name="start">Start position for the path calculation.</param>
    /// <param name="target">Target position for the path calculation.</param>
    /// <param name="callback">The action to invoke when the path is found, providing the path corners and success status.</param>
    private void FindPathWithNavMesh(Vector3 start, Vector3 target, Action<Vector3[], bool> callback)
    {
        NavMeshPath navMeshPath = new NavMeshPath();
        bool pathFound = false;

        // It's good practice to sample the positions on the NavMesh first,
        // as NavMesh.CalculatePath expects points on the mesh.
        NavMeshHit startHit, targetHit;
        if (NavMesh.SamplePosition(start, out startHit, 1.0f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(target, out targetHit, 1.0f, NavMesh.AllAreas))
        {
            // Use NavMesh.CalculatePath to find the path. This is a synchronous operation.
            pathFound = NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, navMeshPath);
        }
        else
        {
            Debug.LogWarning($"PathfindingSystem: Start or target position not on NavMesh. Start: {start}, Target: {target}");
        }

        Vector3[] pathCorners = pathFound ? navMeshPath.corners : new Vector3[0];

        // Invoke the original callback on the main thread with the path results.
        // The callback pattern decouples the pathfinding logic from the agent's response.
        callback?.Invoke(pathCorners, pathFound && pathCorners.Length > 0);
    }


    /*
     * --- Design Pattern Explanation & Extension ---
     *
     * This 'PathfindingSystem' demonstrates several design principles and patterns:
     *
     * 1.  Singleton (`PathfindingSystem.Instance`):
     *     - **Purpose:** Ensures there is only one central point of access for pathfinding services throughout the game.
     *     - **Benefit:** Simplifies access for any agent in the game needing a path (e.g., `PathfindingSystem.Instance.RequestPath(...)`)
     *       without needing to pass references around or explicitly find the manager object.
     *
     * 2.  Command/Request Queue (`pathRequestQueue`):
     *     - **Purpose:** Decouples path requesting from path processing. Agents "command" the system to find a path by enqueuing a request.
     *     - **Benefit:** Allows multiple agents to request paths without blocking the main thread immediately. The system processes requests
     *       one by one (or a few per frame) from the queue in the `Update` loop. This "asynchronous simulation" spreads the computational load
     *       over multiple frames, preventing a single large pathfinding request from causing a noticeable stutter (frame drop).
     *
     * 3.  Service Locator (Implicit):
     *     - **Purpose:** Agents "locate" the `PathfindingSystem` service via its `Instance` property.
     *     - **Benefit:** This abstracts away the specifics of *how* the path is found. Agents only care *that* they can request a path
     *       and receive a result, not the internal implementation details.
     *
     * 4.  Callback Pattern (`Action<Vector3[], bool> callback`):
     *     - **Purpose:** Enables asynchronous results. When a path is found (or not found), the system "calls back" to the requesting agent
     *       with the path data.
     *     - **Benefit:** The agent doesn't need to actively poll or wait for the path to be ready. It provides a function to the system,
     *       which the system will invoke once the work is complete, allowing the agent to continue other tasks in the meantime.
     *
     * --- How to Extend (Implementing a Strategy Pattern for Different Algorithms) ---
     *
     * To support different pathfinding algorithms (e.g., A* for grid-based worlds, Dijkstra, etc.) beyond just NavMesh,
     * you would typically introduce a Strategy Pattern:
     *
     * A.  Create an interface for pathfinding algorithms:
     *     public interface IPathfindingAlgorithm
     *     {
     *         // Define a method that all pathfinding algorithms must implement.
     *         void FindPath(Vector3 start, Vector3 target, Action<Vector3[], bool> callback);
     *     }
     *
     * B.  Implement concrete strategies for each algorithm:
     *     public class NavMeshAlgorithm : MonoBehaviour, IPathfindingAlgorithm // Could be a MonoBehaviour for Inspector setup
     *     {
     *         public void FindPath(Vector3 start, Vector3 target, Action<Vector3[], bool> callback) {
     *             // ... NavMesh.CalculatePath logic ...
     *             callback?.Invoke(pathCorners, success);
     *         }
     *     }
     *
     *     public class AStarGridAlgorithm : MonoBehaviour, IPathfindingAlgorithm // Requires a grid data structure
     *     {
     *         [SerializeField] private GridManager grid; // Reference to your grid
     *         public void FindPath(Vector3 start, Vector3 target, Action<Vector3[], bool> callback) {
     *             // ... A* algorithm logic using the grid ...
     *             callback?.Invoke(pathNodes, success);
     *         }
     *     }
     *
     * C.  Modify PathfindingSystem to use the chosen strategy:
     *     public class PathfindingSystem : MonoBehaviour
     *     {
     *         public static PathfindingSystem Instance { get; private set; }
     *
     *         [SerializeField] private MonoBehaviour currentAlgorithmMB; // Serialize as MonoBehaviour
     *         private IPathfindingAlgorithm currentAlgorithm; // Runtime reference
     *
     *         private void Awake()
     *         {
     *             // ... Singleton setup ...
     *             currentAlgorithm = currentAlgorithmMB as IPathfindingAlgorithm;
     *             if (currentAlgorithm == null) {
     *                 Debug.LogError("Current Algorithm does not implement IPathfindingAlgorithm!");
     *             }
     *         }
     *
     *         private void ProcessNextRequest()
     *         {
     *             // ... dequeue request ...
     *             if (currentAlgorithm != null)
     *             {
     *                 currentAlgorithm.FindPath(currentRequest.startPosition, currentRequest.targetPosition, currentRequest.callback);
     *             }
     *             else
     *             {
     *                 Debug.LogError("No pathfinding algorithm assigned!");
     *                 currentRequest.callback?.Invoke(new Vector3[0], false);
     *             }
     *             // ... reset isProcessingPath ...
     *         }
     *     }
     *     This approach makes the `PathfindingSystem` flexible and extensible. You can swap out different pathfinding logic
     *     without modifying the core manager or the agents requesting paths.
     */
}
```

---

### **2. AgentController.cs**

This script demonstrates how an AI agent interacts with the `PathfindingSystem`. It requests a path to a target and then moves along the received path.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// This script demonstrates how an AI agent would interact with the PathfindingSystem.
/// It requests a path to a designated target and then moves along the received path.
/// </summary>
public class AgentController : MonoBehaviour
{
    [Header("Agent Settings")]
    [SerializeField] private Transform targetTransform; // The target the agent wants to reach
    [SerializeField] private float moveSpeed = 5f;      // Speed of the agent
    [SerializeField] private float pathUpdateInterval = 1.0f; // How often to request a new path (in seconds)
    [SerializeField] private float minDistanceToWaypoint = 0.5f; // How close to a waypoint before moving to the next

    private Vector3[] currentPath;      // The path received from the PathfindingSystem
    private int currentPathWaypointIndex; // Index of the current waypoint on the path
    private bool isMoving = false;      // Flag to indicate if the agent is currently moving
    private float lastPathUpdateTime;   // Time when the last path request was made

    private void Start()
    {
        // Initial path request when the agent starts.
        if (targetTransform != null)
        {
            RequestPathToTarget();
        }
        else
        {
            Debug.LogError("AgentController: Target Transform is not assigned! Please assign a target in the Inspector.");
        }
        lastPathUpdateTime = Time.time; // Initialize last update time
    }

    private void Update()
    {
        // Periodically request a new path to the target. This handles cases where
        // the target moves, or the environment changes (e.g., new obstacles).
        if (targetTransform != null && Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            RequestPathToTarget();
            lastPathUpdateTime = Time.time;
        }

        // Move the agent if a valid path exists and the agent is supposed to be moving.
        if (isMoving && currentPath != null && currentPathWaypointIndex < currentPath.Length)
        {
            MoveAlongPath();
        }
    }

    /// <summary>
    /// Requests a path from the PathfindingSystem to the designated target's position.
    /// This is the agent's interaction point with the PathfindingSystem.
    /// </summary>
    private void RequestPathToTarget()
    {
        // Ensure the PathfindingSystem is available in the scene.
        if (PathfindingSystem.Instance == null)
        {
            Debug.LogError("PathfindingSystem not found! Make sure an instance is in the scene.");
            isMoving = false; // Stop movement if no system to request from
            return;
        }

        if (targetTransform == null) return; // Cannot request path without a target

        // Call the PathfindingSystem's public API to enqueue a path request.
        // The `OnPathFound` method will be called by the system when the path is ready.
        PathfindingSystem.Instance.RequestPath(transform.position, targetTransform.position, OnPathFound);
        // Debug.Log($"Agent {name} requesting path from {transform.position} to {targetTransform.position}");
    }

    /// <summary>
    /// Callback method invoked by the PathfindingSystem when a path request is completed.
    /// This method processes the received path and prepares the agent for movement.
    /// </summary>
    /// <param name="path">The array of waypoints (Vector3) forming the path.</param>
    /// <param name="success">True if a valid path was found, false otherwise.</param>
    private void OnPathFound(Vector3[] path, bool success)
    {
        if (success)
        {
            currentPath = path;
            currentPathWaypointIndex = 0;
            isMoving = true; // Start moving along the new path
            // Debug.Log($"Agent {name} received path with {path.Length} waypoints. Path starts at {path[0]}");
        }
        else
        {
            currentPath = null;
            isMoving = false; // Stop moving if no path was found
            Debug.LogWarning($"Agent {name}: No path found to target!");
        }
    }

    /// <summary>
    /// Moves the agent along the current path, progressing through waypoints.
    /// </summary>
    private void MoveAlongPath()
    {
        // Ensure we have a valid path and a current waypoint to move towards.
        if (currentPath == null || currentPathWaypointIndex >= currentPath.Length)
        {
            isMoving = false; // Reached end of path or invalid path
            return;
        }

        Vector3 currentWaypoint = currentPath[currentPathWaypointIndex];
        Vector3 direction = (currentWaypoint - transform.position).normalized;

        // Move the agent towards the current waypoint.
        // We ensure movement is only on the XZ plane to avoid agents flying up/down
        // due to slight height differences in NavMesh waypoints.
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        transform.position += flatDirection * moveSpeed * Time.deltaTime;

        // Optionally, rotate the agent to face the direction of movement.
        if (flatDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDirection), Time.deltaTime * 10f);
        }

        // Check if the agent has reached the current waypoint.
        // We use Vector2.Distance for flat distance check to ignore Y-axis differences.
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currentWaypoint.x, currentWaypoint.z)) < minDistanceToWaypoint)
        {
            currentPathWaypointIndex++; // Move to the next waypoint
            if (currentPathWaypointIndex >= currentPath.Length)
            {
                // Reached the end of the path.
                isMoving = false;
                Debug.Log($"Agent {name} reached target!");
            }
        }
    }

    // --- Visualization for Debugging (Gizmos) ---
    private void OnDrawGizmos()
    {
        // Draw the path received from the PathfindingSystem.
        if (currentPath != null && currentPath.Length > 0)
        {
            Gizmos.color = Color.blue; // Path color
            for (int i = 0; i < currentPath.Length - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.DrawSphere(currentPath[i], 0.2f); // Draw spheres at waypoints
            }
            Gizmos.DrawSphere(currentPath[currentPath.Length - 1], 0.2f); // Draw last waypoint
        }

        // Draw the target position.
        if (targetTransform != null)
        {
            Gizmos.color = Color.red; // Target color
            Gizmos.DrawSphere(targetTransform.position, 0.5f);
            Gizmos.DrawWireCube(targetTransform.position, Vector3.one * 0.7f);
        }

        // Draw the agent's current position.
        Gizmos.color = Color.green; // Agent color
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
```

---

### **How to Set Up in Unity:**

Follow these steps to get the example working immediately in your Unity project:

1.  **Create a New Unity Project** (or open an existing one).

2.  **Create a 3D Scene:**
    *   Right-click in the Hierarchy window -> 3D Object -> Plane. Name it "Ground".
    *   Right-click in the Hierarchy window -> 3D Object -> Cube. Name it "Obstacle1". Place it on the ground and scale it (e.g., X=2, Y=2, Z=2). Duplicate it a few times (`Ctrl+D`) and arrange them to create some barriers for the agent.

3.  **Bake a NavMesh:**
    *   Select "Ground" and all "Obstacle" GameObjects.
    *   In the Inspector, go to the "Static" dropdown at the top right, and ensure **"Navigation Static"** is checked for all of them.
    *   Open the Navigation window: `Window > AI > Navigation`.
    *   Go to the **"Bake"** tab.
    *   Click the **"Bake"** button. You should see a blue overlay on your "Ground" plane, indicating the walkable areas. Ensure obstacles are excluded from the blue area.

4.  **Create the PathfindingSystem GameObject:**
    *   Create an empty GameObject in your Hierarchy: Right-click -> Create Empty.
    *   Rename it to "Pathfinding Manager".
    *   Drag and drop the `PathfindingSystem.cs` script (from above) onto this "Pathfinding Manager" GameObject in the Inspector.
    *   (Optional) You can adjust the `Requests Per Frame` value. `1` is generally a good default to spread the load.

5.  **Create Agent and Target GameObjects:**
    *   **Agent:**
        *   Create an empty GameObject: Right-click -> Create Empty. Rename it "Agent".
        *   Add a visual representation as a child: Right-click on "Agent" -> 3D Object -> Capsule. Set its Y position to `1` so it sits on the ground. You can scale it down if it's too big.
        *   Drag and drop the `AgentController.cs` script (from above) onto the "Agent" GameObject.
    *   **Target:**
        *   Create an empty GameObject: Right-click -> Create Empty. Rename it "Target".
        *   Add a visual representation as a child: Right-click on "Target" -> 3D Object -> Sphere. Set its Y position to `0.5` so it sits on the ground.

6.  **Configure the AgentController:**
    *   Select the "Agent" GameObject in the Hierarchy.
    *   In its Inspector, locate the `AgentController` component.
    *   Drag the "Target" GameObject from the Hierarchy into the **'Target Transform'** field of the `AgentController`.
    *   Adjust `Move Speed`, `Path Update Interval`, etc., as desired.

7.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   The "Agent" should start moving from its current position towards the "Target", navigating around the "Obstacle" cubes.
    *   If you enable **Gizmos** in the Scene view (top bar, click "Gizmos"), you will see the calculated path drawn in blue lines and the agent/target indicators.
    *   **During Play Mode:** Try moving the "Target" GameObject to a new location (even behind obstacles). The "Agent" should periodically request a new path and update its movement to the new target.

---

This setup provides a complete, functional, and educational example of the PathfindingSystem pattern in Unity development, ready for immediate use and further experimentation.