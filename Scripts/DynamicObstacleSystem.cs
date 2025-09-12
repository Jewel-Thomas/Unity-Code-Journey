// Unity Design Pattern Example: DynamicObstacleSystem
// This script demonstrates the DynamicObstacleSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DynamicObstacleSystem' design pattern in Unity provides a centralized and dynamic way to manage obstacles within a game environment. This is particularly useful for AI pathfinding, player interaction, and game mechanics where obstacles can appear, disappear, move, or change their blocking state at runtime.

Instead of disparate systems independently checking for obstacles, a central `DynamicObstacleSystem` acts as a registry. Obstacles register themselves upon creation/activation and deregister upon destruction/deactivation. Other game systems (like AI) can then query this central system for up-to-date obstacle information.

This example will demonstrate:
1.  **`DynamicObstacleSystem`**: The singleton manager responsible for registering, deregistering, and providing query capabilities for dynamic obstacles.
2.  **`DynamicObstacle`**: A component attached to any GameObject that acts as a dynamic obstacle. It automatically registers and deregisters itself with the system and can change its blocking state. It integrates with Unity's `NavMeshObstacle` for AI pathfinding.
3.  **`AICharacter`**: An example consumer script that uses the `NavMeshAgent` to navigate and actively queries the `DynamicObstacleSystem` to react to obstacles.

---

### **1. `DynamicObstacleSystem.cs`**
This script manages all active dynamic obstacles in the scene.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq extensions like Where()

/// <summary>
/// The central manager for all dynamic obstacles in the scene.
/// This is a singleton, ensuring only one instance exists and provides global access.
/// It allows obstacles to register/deregister and provides methods for other systems (e.g., AI)
/// to query obstacle information dynamically.
/// </summary>
public class DynamicObstacleSystem : MonoBehaviour
{
    // Singleton instance for easy global access
    public static DynamicObstacleSystem Instance { get; private set; }

    // A HashSet is used for efficient adding, removing, and checking existence of obstacles.
    // It ensures that each obstacle is registered only once.
    private HashSet<DynamicObstacle> _activeObstacles = new HashSet<DynamicObstacle>();

    [Header("System Debugging")]
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Color _debugObstacleColor = new Color(1, 0.5f, 0, 0.5f); // Orange, semi-transparent

    private void Awake()
    {
        // Singleton initialization
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep system alive across scenes if needed
        }
        else
        {
            // If another instance already exists, destroy this one to enforce singleton
            Debug.LogWarning("DynamicObstacleSystem: Another instance found, destroying this one.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clean up singleton reference if this is the active instance being destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a DynamicObstacle with the system.
    /// This should be called by the DynamicObstacle itself when it becomes active.
    /// </summary>
    /// <param name="obstacle">The DynamicObstacle to register.</param>
    public void RegisterObstacle(DynamicObstacle obstacle)
    {
        if (obstacle == null)
        {
            Debug.LogWarning("DynamicObstacleSystem: Attempted to register a null obstacle.");
            return;
        }
        if (_activeObstacles.Add(obstacle)) // Add() returns true if the element was added (i.e., not already present)
        {
            Debug.Log($"DynamicObstacleSystem: Registered obstacle '{obstacle.name}'. Total: {_activeObstacles.Count}");
        }
        else
        {
            Debug.LogWarning($"DynamicObstacleSystem: Obstacle '{obstacle.name}' was already registered.");
        }
    }

    /// <summary>
    /// Deregisters a DynamicObstacle from the system.
    /// This should be called by the DynamicObstacle itself when it becomes inactive or destroyed.
    /// </summary>
    /// <param name="obstacle">The DynamicObstacle to deregister.</param>
    public void DeregisterObstacle(DynamicObstacle obstacle)
    {
        if (obstacle == null)
        {
            Debug.LogWarning("DynamicObstacleSystem: Attempted to deregister a null obstacle.");
            return;
        }
        if (_activeObstacles.Remove(obstacle)) // Remove() returns true if the element was removed
        {
            Debug.Log($"DynamicObstacleSystem: Deregistered obstacle '{obstacle.name}'. Total: {_activeObstacles.Count}");
        }
        else
        {
            Debug.LogWarning($"DynamicObstacleSystem: Obstacle '{obstacle.name}' was not found in the system.");
        }
    }

    /// <summary>
    /// Returns a read-only collection of all currently registered active obstacles.
    /// </summary>
    /// <returns>An IEnumerable of DynamicObstacle.</returns>
    public IEnumerable<DynamicObstacle> GetAllActiveObstacles()
    {
        // Return a copy to prevent external modification of the internal collection
        return _activeObstacles.ToList();
    }

    /// <summary>
    /// Queries for active and blocking obstacles within a specified radius around a position.
    /// </summary>
    /// <param name="center">The center point for the radius check.</param>
    /// <param name="radius">The radius to search within.</param>
    /// <returns>A list of DynamicObstacle objects found within the radius that are currently blocking.</returns>
    public List<DynamicObstacle> GetBlockingObstaclesInRadius(Vector3 center, float radius)
    {
        // Using LINQ to filter obstacles. This creates a new list.
        return _activeObstacles
            .Where(obs => obs != null && obs.IsActiveAndBlocking && Vector3.Distance(center, obs.transform.position) <= radius)
            .ToList();
    }

    /// <summary>
    /// Queries for the closest active and blocking obstacle to a given position.
    /// </summary>
    /// <param name="position">The reference position.</param>
    /// <param name="maxDistance">The maximum distance to search for an obstacle.</param>
    /// <returns>The closest blocking DynamicObstacle, or null if none found within maxDistance.</returns>
    public DynamicObstacle GetClosestBlockingObstacle(Vector3 position, float maxDistance)
    {
        DynamicObstacle closestObstacle = null;
        float minDistanceSq = maxDistance * maxDistance; // Use squared distance for performance

        foreach (var obstacle in _activeObstacles)
        {
            if (obstacle == null || !obstacle.IsActiveAndBlocking)
                continue;

            float distSq = (obstacle.transform.position - position).sqrMagnitude;
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                closestObstacle = obstacle;
            }
        }
        return closestObstacle;
    }

    // You can add more advanced query methods here, e.g.:
    // - GetObstaclesInFrustum(Camera camera)
    // - GetObstaclesAlongRay(Ray ray)
    // - GetObstaclesOfType<T>() where T : DynamicObstacle (if you have subclasses)

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos || _activeObstacles == null)
            return;

        Gizmos.color = _debugObstacleColor;
        foreach (var obstacle in _activeObstacles)
        {
            if (obstacle != null)
            {
                // Draw a sphere to represent the obstacle's general area
                Gizmos.DrawSphere(obstacle.transform.position, 0.5f);
            }
        }
    }
}
```

---

### **2. `DynamicObstacle.cs`**
This script marks a GameObject as a dynamic obstacle and manages its state. It automatically integrates with Unity's `NavMeshObstacle` component.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshObstacle

/// <summary>
/// Component to mark a GameObject as a dynamic obstacle.
/// It automatically registers and deregisters itself with the DynamicObstacleSystem.
/// It also manages the state of its associated Collider and NavMeshObstacle component,
/// allowing it to dynamically block or unblock paths for AI.
/// </summary>
[RequireComponent(typeof(Collider))] // Obstacles typically need a collider to define their bounds
[RequireComponent(typeof(NavMeshObstacle))] // Essential for dynamic AI pathfinding avoidance
public class DynamicObstacle : MonoBehaviour
{
    // Public property to check if the obstacle is currently active and blocking.
    // Other systems can use this to determine if they should react to this obstacle.
    public bool IsActiveAndBlocking { get; private set; }

    // Reference to the collider component, retrieved on Awake.
    public Collider ObstacleCollider { get; private set; }

    // Reference to the NavMeshObstacle component, retrieved on Awake.
    private NavMeshObstacle _navMeshObstacle;

    [Header("Obstacle Settings")]
    [Tooltip("Initial blocking state of the obstacle.")]
    [SerializeField] private bool _initialBlockingState = true;
    [Tooltip("If true, the NavMeshObstacle will automatically bake its bounds into the NavMesh.")]
    [SerializeField] private bool _autoBakeNavMeshObstacle = true;
    [Tooltip("Optional: Visualizes the obstacle's effect in the Editor.")]
    [SerializeField] private Color _gizmoColor = new Color(1, 0, 0, 0.3f); // Red, semi-transparent

    private void Awake()
    {
        // Get references to required components
        ObstacleCollider = GetComponent<Collider>();
        _navMeshObstacle = GetComponent<NavMeshObstacle>();

        // Ensure NavMeshObstacle is properly configured
        if (_navMeshObstacle != null)
        {
            _navMeshObstacle.carving = _autoBakeNavMeshObstacle; // Enable carving for dynamic avoidance
        }
        else
        {
            Debug.LogError($"DynamicObstacle on '{name}': NavMeshObstacle component missing, but is required!", this);
        }
    }

    private void OnEnable()
    {
        // Register this obstacle with the central system when it becomes active.
        if (DynamicObstacleSystem.Instance != null)
        {
            DynamicObstacleSystem.Instance.RegisterObstacle(this);
        }
        else
        {
            Debug.LogError($"DynamicObstacle on '{name}': DynamicObstacleSystem instance not found! Obstacle cannot be registered.");
        }

        // Apply initial blocking state
        SetBlockingState(_initialBlockingState);
    }

    private void OnDisable()
    {
        // Deregister this obstacle from the central system when it becomes inactive or is destroyed.
        if (DynamicObstacleSystem.Instance != null)
        {
            DynamicObstacleSystem.Instance.DeregisterObstacle(this);
        }

        // Also ensure NavMeshObstacle and Collider are disabled if the obstacle becomes inactive.
        if (ObstacleCollider != null) ObstacleCollider.enabled = false;
        if (_navMeshObstacle != null) _navMeshObstacle.enabled = false;
    }

    /// <summary>
    /// Changes the blocking state of this obstacle.
    /// This will enable/disable its collider and the NavMeshObstacle component,
    /// affecting both physics and AI pathfinding.
    /// </summary>
    /// <param name="isBlocking">True to make the obstacle block, false to unblock.</param>
    public void SetBlockingState(bool isBlocking)
    {
        IsActiveAndBlocking = isBlocking;

        // Enable/disable the collider based on blocking state
        if (ObstacleCollider != null)
        {
            ObstacleCollider.enabled = isBlocking;
        }

        // Enable/disable the NavMeshObstacle based on blocking state
        // When enabled and carving, AI NavMeshAgents will avoid it.
        if (_navMeshObstacle != null)
        {
            _navMeshObstacle.enabled = isBlocking;
            // Force a NavMesh rebuild around this obstacle if carving is enabled and its state changes.
            // This is often handled automatically by NavMeshObstacle, but a manual call might be needed
            // for specific scenarios or if the obstacle moves.
            // NavMesh.SetNavMeshCallbacks(null, null, UpdateNavMesh); // Advanced usage, usually not needed.
        }

        Debug.Log($"DynamicObstacle '{name}' blocking state set to: {isBlocking}");
    }

    private void OnDrawGizmos()
    {
        // Visualize the obstacle's state in the editor
        if (IsActiveAndBlocking)
        {
            Gizmos.color = _gizmoColor;
            // Use bounds of the collider to draw a more accurate representation
            if (ObstacleCollider != null)
            {
                Gizmos.DrawCube(ObstacleCollider.bounds.center, ObstacleCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawCube(transform.position, Vector3.one); // Fallback
            }
        }
        else
        {
            // Optionally, draw a different color if not blocking
            Gizmos.color = new Color(0, 1, 0, 0.1f); // Green, very transparent
            if (ObstacleCollider != null)
            {
                Gizmos.DrawWireCube(ObstacleCollider.bounds.center, ObstacleCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one); // Fallback
            }
        }
    }
}
```

---

### **3. `AICharacter.cs`**
This script demonstrates how an AI agent can interact with the `DynamicObstacleSystem`.

```csharp
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// An example AI character that uses a NavMeshAgent for movement
/// and queries the DynamicObstacleSystem to react to dynamic obstacles.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class AICharacter : MonoBehaviour
{
    private NavMeshAgent _agent;

    [Header("AI Settings")]
    [SerializeField] private float _patrolRadius = 20f;
    [SerializeField] private float _obstacleDetectionRadius = 10f;
    [SerializeField] private LayerMask _groundLayer; // For random point on NavMesh
    [SerializeField] private float _pathRecalculateInterval = 1.0f; // How often AI checks for new path/obstacles
    [SerializeField] private float _minObstacleDistanceForReaction = 3f; // Min dist to an obstacle to react

    private Vector3 _currentDestination;
    private float _nextPathRecalculateTime;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_groundLayer.value == 0) // Default to everything if not set
        {
            _groundLayer = LayerMask.GetMask("Default"); // Ensure default layer is used if nothing specified
        }
    }

    void Start()
    {
        SetNewRandomDestination();
        _nextPathRecalculateTime = Time.time + Random.Range(0f, _pathRecalculateInterval);
    }

    void Update()
    {
        // Periodically check for new path or obstacles
        if (Time.time >= _nextPathRecalculateTime)
        {
            CheckForObstaclesAndRecalculatePath();
            _nextPathRecalculateTime = Time.time + _pathRecalculateInterval;
        }

        // If arrived at destination, set a new one
        if (!_agent.pathPending && _agent.remainingDistance < _agent.stoppingDistance)
        {
            SetNewRandomDestination();
        }
    }

    /// <summary>
    /// Sets a new random destination for the AI character within its patrol radius.
    /// </summary>
    private void SetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _patrolRadius;
        randomDirection += transform.position; // Offset from current position

        NavMeshHit hit;
        // Sample a random point on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, _patrolRadius, NavMesh.AllAreas))
        {
            _currentDestination = hit.position;
            _agent.SetDestination(_currentDestination);
            Debug.Log($"AI '{name}' setting new destination: {_currentDestination}");
        }
        else
        {
            Debug.LogWarning($"AI '{name}' could not find a valid random point on NavMesh.");
        }
    }

    /// <summary>
    /// Queries the DynamicObstacleSystem for nearby obstacles and reacts accordingly.
    /// This is where the integration with the pattern happens.
    /// </summary>
    private void CheckForObstaclesAndRecalculatePath()
    {
        if (DynamicObstacleSystem.Instance == null)
        {
            Debug.LogWarning("AICharacter: DynamicObstacleSystem not found, cannot check for obstacles.");
            return;
        }

        // Query the system for blocking obstacles in the detection radius
        List<DynamicObstacle> nearbyObstacles = DynamicObstacleSystem.Instance.GetBlockingObstaclesInRadius(transform.position, _obstacleDetectionRadius);

        if (nearbyObstacles.Any()) // If any obstacles are found
        {
            DynamicObstacle closestObstacle = null;
            float minDistance = float.MaxValue;

            // Find the closest blocking obstacle
            foreach (var obstacle in nearbyObstacles)
            {
                if (obstacle == null || !obstacle.IsActiveAndBlocking) continue;

                float dist = Vector3.Distance(transform.position, obstacle.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestObstacle = obstacle;
                }
            }

            if (closestObstacle != null && minDistance < _minObstacleDistanceForReaction)
            {
                Debug.Log($"AI '{name}' detected blocking obstacle '{closestObstacle.name}' at distance {minDistance:F2}. Recalculating path.");
                // Simply setting destination again allows NavMeshAgent to automatically find a path around
                // the active NavMeshObstacle components managed by the DynamicObstacle.
                _agent.SetDestination(_currentDestination);
            }
        }
        else
        {
            // No obstacles detected or they are too far, continue to current destination
            if (_agent.destination != _currentDestination)
            {
                 _agent.SetDestination(_currentDestination);
                 Debug.Log($"AI '{name}' cleared of obstacles, continuing to original destination.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the AI's detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _obstacleDetectionRadius);

        // Visualize current destination
        if (_agent != null && _agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, _agent.path.corners.Length > 0 ? _agent.path.corners[0] : transform.position);
            for (int i = 0; i < _agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(_agent.path.corners[i], _agent.path.corners[i + 1]);
            }
        }
    }
}
```

---

### **How to Implement and Test in Unity:**

1.  **Create an Empty GameObject for the System:**
    *   Right-click in the Hierarchy -> Create Empty. Name it `DynamicObstacleSystemManager`.
    *   Attach the `DynamicObstacleSystem.cs` script to it.

2.  **Prepare the Environment:**
    *   Create a simple 3D scene with a `Plane` or `Terrain` to act as ground.
    *   **Bake a NavMesh:**
        *   Go to `Window > AI > Navigation`.
        *   In the `Bake` tab, ensure your ground object is marked as `Navigation Static` in the Inspector.
        *   Click `Bake`. This will create the base NavMesh for your AI to walk on.

3.  **Create Dynamic Obstacles:**
    *   Create a 3D object (e.g., `Cube`, `Cylinder`). Name it `DynamicObstacle_1`.
    *   Position it on the NavMesh.
    *   Ensure it has a `Collider` (e.g., `Box Collider` for a Cube).
    *   Attach the `DynamicObstacle.cs` script to it.
    *   In the Inspector for `DynamicObstacle_1`:
        *   `Initial Blocking State`: `true` (so it starts as a blocker).
        *   `Auto Bake Nav Mesh Obstacle`: `true`.
    *   Duplicate this object (`Ctrl+D`) to create `DynamicObstacle_2`, `DynamicObstacle_3`, etc., and place them around your scene.

4.  **Create an AI Character:**
    *   Create another 3D object (e.g., `Capsule`). Name it `AI_Agent_1`.
    *   Position it on the NavMesh.
    *   Attach a `NavMeshAgent` component to it. Configure its `Speed`, `Angular Speed`, `Acceleration`, and `Radius` as desired.
    *   Attach the `AICharacter.cs` script to it.
    *   In the Inspector for `AI_Agent_1`:
        *   Set `Patrol Radius` to something reasonable (e.g., `20`).
        *   Set `Obstacle Detection Radius` (e.g., `10`).
        *   **Crucially, set `Ground Layer`**: Create a new Layer (e.g., "Ground") in `Tags & Layers` and assign your ground object (Plane/Terrain) to it. Then select this layer in the `AICharacter`'s Inspector. This is used for `NavMesh.SamplePosition`.
    *   Duplicate the AI character if you want multiple agents.

5.  **Run the Scene:**
    *   Press Play.
    *   You should see the AI agents moving around.
    *   The `DynamicObstacleSystemManager` will show debug Gizmos of registered obstacles.
    *   **Test Dynamic Behavior:**
        *   Select one of your `DynamicObstacle` GameObjects during runtime.
        *   In its Inspector, uncheck `Is Active And Blocking` (or use the `SetBlockingState(false)` method via a custom script/button). You should see the AI agents adjust their paths, potentially moving through the now-unblocked area.
        *   Re-check `Is Active And Blocking` to see them avoid it again.
        *   You can also disable/enable the entire `DynamicObstacle` GameObject to see it deregister/register with the system.

This setup demonstrates a practical application of the DynamicObstacleSystem pattern, making your game environments more reactive and intelligent.