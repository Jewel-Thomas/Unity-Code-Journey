// Unity Design Pattern Example: NavMeshObstacleSystem
// This script demonstrates the NavMeshObstacleSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "NavMeshObstacleSystem" isn't a traditional Gang of Four design pattern but rather a practical **system-level approach** or **usage pattern** for effectively leveraging Unity's built-in `NavMeshObstacle` component. It addresses the common game development problem of dynamically changing environments that AI agents need to navigate.

The core idea is to:
1.  **Identify Dynamic Obstacles:** Mark objects that can move, appear, disappear, or change their blocking state at runtime.
2.  **Attach `NavMeshObstacle`:** Equip these objects with Unity's `NavMeshObstacle` component. This component tells the NavMesh system to dynamically carve out or block areas around the object without requiring a full NavMesh bake.
3.  **Encapsulate Logic:** Create a dedicated C# script (e.g., `DynamicObstacleController`) for each type of dynamic obstacle. This script manages the `NavMeshObstacle` component's properties (like `carve`, `enabled`, `size`, `center`) based on the object's specific game logic (e.g., movement, interaction, state changes).
4.  **Reactive AI:** `NavMeshAgent` components on AI characters automatically recognize and avoid these dynamic obstacles, recalculating their paths as needed.

This pattern promotes modularity, keeps AI logic clean, and allows designers to easily configure various dynamic obstacle behaviors.

---

### Unity Setup Instructions

To use this example, follow these steps in a new or existing Unity project:

1.  **Create a New Scene:** Go to `File > New Scene`.
2.  **Create Ground:** Create a 3D object like a `Plane` (e.g., scale X:10, Z:10) to serve as your walkable ground.
3.  **Create Walls/Static Obstacles:** Add some `Cube` objects around the scene to create static barriers. This helps define paths for the AI agents.
4.  **Bake NavMesh:**
    *   Select your `Plane` and any static `Cube` walls. In the Inspector, enable `Static` and ensure `Navigation Static` is checked (under `Object` tab in the `Navigation` window).
    *   Open the `Navigation` window: `Window > AI > Navigation`.
    *   Go to the `Bake` tab. Adjust settings if needed (default usually works for a demo).
    *   Click the `Bake` button. You should see a blue overlay on your walkable areas.
5.  **Create AI Agent (Capsule):**
    *   Create a `Capsule` GameObject (`GameObject > 3D Object > Capsule`).
    *   Add a `NavMeshAgent` component to it (`Add Component > NavMeshAgent`).
    *   Create a new C# script named `AI_AgentController.cs` and paste the provided code into it.
    *   Attach `AI_AgentController.cs` to the `Capsule`.
    *   **Create Target Points:** Create some empty `GameObject`s in your scene (e.g., named "Waypoint1", "Waypoint2", etc.). These will be the destinations for your AI agent. Drag these empty GameObjects into the `Target Points` array in the `AI_AgentController` component's Inspector.
6.  **Create Dynamic Obstacle (Moving Cube):**
    *   Create a `Cube` GameObject.
    *   **Add `NavMeshObstacle` component:** (`Add Component > NavMeshObstacle`). Ensure `Carve` is checked in the Inspector.
    *   Create a new C# script named `DynamicObstacleController.cs` and paste the provided code into it.
    *   Attach `DynamicObstacleController.cs` to the `Cube`.
    *   In the Inspector for `DynamicObstacleController`:
        *   Set `Obstacle Mode` to `Moving Obstacle`.
        *   **Create Path Points:** Create some empty `GameObject`s (e.g., "MovePointA", "MovePointB"). Drag these into the `Path Points` array.
        *   Adjust `Move Speed` and `Wait Time` as desired.
7.  **Create Interactive Obstacle (Door/Toggleable Cube):**
    *   Create another `Cube` GameObject. Position it where a door or barrier would be.
    *   **Add `NavMeshObstacle` component:** (`Add Component > NavMeshObstacle`). Ensure `Carve` is checked.
    *   Attach `DynamicObstacleController.cs` to this Cube.
    *   In the Inspector for `DynamicObstacleController`:
        *   Set `Obstacle Mode` to `Interactive Door` or `Simple Toggle`.
        *   If `Interactive Door`: Adjust `Open Rotation Euler`, `Door Speed`, and `Toggle Key` (e.g., `E`).
        *   If `Simple Toggle`: Adjust `Toggle Obstacle Key` (e.g., `T`).
        *   **Add Materials:** Create two simple materials (e.g., red for active, green for inactive) and assign them to `Active Material` and `Inactive Material` properties if you're using `Simple Toggle` mode.

Now, run the scene. The AI agent will navigate, avoiding the moving cube. If you press the configured key for the door/toggleable cube, the obstacle will change its state, and the AI agent will dynamically recalculate its path to account for the change!

---

### 1. `DynamicObstacleController.cs` (The Obstacle Manager)

This script embodies the "NavMeshObstacleSystem" usage pattern by encapsulating the logic for various types of dynamic obstacles. Each instance of this script manages its own `NavMeshObstacle` component.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshObstacle component
using System.Collections; // Required for Coroutines

/// <summary>
/// DynamicObstacleController: A core component of the NavMeshObstacleSystem pattern.
/// This script manages a NavMeshObstacle component on its GameObject, allowing it
/// to dynamically affect NavMesh agent paths without re-baking the NavMesh.
///
/// The 'NavMeshObstacleSystem' pattern is about effectively using Unity's NavMeshObstacle
/// component for dynamic avoidance. This script provides different 'modes' to demonstrate:
/// 1.  Moving Obstacles: An object that continuously moves, carving a dynamic hole.
/// 2.  Interactive Doors: An object that can be opened/closed, enabling/disabling its
///     obstruction on the NavMesh.
/// 3.  Simple Toggles: An object whose obstruction can be toggled on/off.
///
/// By encapsulating the obstacle's specific logic (movement, interaction, visual state)
/// with the NavMeshObstacle component management, we create a modular and scalable system.
/// AI agents with NavMeshAgent components will automatically react to these changes.
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))] // Ensures the GameObject has a NavMeshObstacle
public class DynamicObstacleController : MonoBehaviour
{
    // --- Public Enums and Inspector Variables ---

    /// <summary>
    /// Defines the behavior mode of this dynamic obstacle.
    /// </summary>
    public enum ObstacleMode
    {
        MovingObstacle,
        InteractiveDoor,
        SimpleToggle
    }

    [Header("General Obstacle Settings")]
    [Tooltip("The mode of this dynamic obstacle. Determines its behavior and how it affects the NavMesh.")]
    public ObstacleMode obstacleMode = ObstacleMode.MovingObstacle;

    [Tooltip("Should this obstacle carve a dynamic hole in the NavMesh? Essential for dynamic avoidance. " +
             "If unchecked, the obstacle will only stop agents that collide with it, not affect pathfinding.")]
    public bool carveNavMesh = true;

    // --- Private References and State Variables ---
    private NavMeshObstacle _navMeshObstacle;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // --- Moving Obstacle Specific Settings ---
    [Header("Moving Obstacle Settings (if mode is MovingObstacle)")]
    [Tooltip("The path points for the moving obstacle to cycle through.")]
    public Transform[] pathPoints;
    [Tooltip("Speed at which the obstacle moves between path points.")]
    public float moveSpeed = 2f;
    [Tooltip("Time to wait at each path point before moving to the next.")]
    public float waitTime = 1f;

    private int _currentPathIndex = 0;
    private bool _isMoving = true;

    // --- Interactive Door Specific Settings ---
    [Header("Interactive Door Settings (if mode is InteractiveDoor)")]
    [Tooltip("The target Euler angles (rotation) when the door is in its open state.")]
    public Vector3 openRotationEuler = new Vector3(0, 90, 0); // Example: Rotate around Y by 90 degrees
    [Tooltip("Speed at which the door opens and closes.")]
    public float doorSpeed = 2f;
    [Tooltip("The keyboard key to press to toggle the door's open/closed state.")]
    public KeyCode toggleKey = KeyCode.E;

    private bool _isOpen = false;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _doorAnimationCoroutine;

    // --- Simple Toggle Specific Settings ---
    [Header("Simple Toggle Settings (if mode is SimpleToggle)")]
    [Tooltip("The keyboard key to press to toggle the obstacle's active/inactive state.")]
    public KeyCode toggleObstacleKey = KeyCode.T;
    [Tooltip("Material to apply when the obstacle is active (carving).")]
    public Material activeMaterial;
    [Tooltip("Material to apply when the obstacle is inactive (not carving).")]
    public Material inactiveMaterial;

    private Renderer _renderer;

    // --- MonoBehaviour Lifecycle Methods ---

    void Awake()
    {
        // Get the NavMeshObstacle component. [RequireComponent] ensures it's present.
        _navMeshObstacle = GetComponent<NavMeshObstacle>();

        // Apply general obstacle settings
        _navMeshObstacle.carve = carveNavMesh;
        // For moving obstacles, 'carveOnlyStationary' should typically be false,
        // so the obstacle constantly updates its carve effect as it moves.
        // For static obstacles that toggle, it can be true for a minor optimization.
        _navMeshObstacle.carveOnlyStationary = !carveNavMesh; // For moving obstacles, we want it to carve when moving.
                                                              // For stationary (door, simple toggle), this will be true if carving is enabled.
                                                              // We will toggle `_navMeshObstacle.enabled` for door/toggle,
                                                              // which effectively makes carveOnlyStationary irrelevant when disabled.

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // Initialize mode-specific behaviors
        InitializeObstacleMode();
    }

    void Update()
    {
        // Handle input for interactive obstacle modes
        HandleModeSpecificUpdate();
    }

    void OnDrawGizmos()
    {
        // Visualize path points for MovingObstacle mode in the editor
        DrawPathGizmos();
    }

    // --- Initialization and Mode Handling ---

    /// <summary>
    /// Initializes the obstacle based on its selected mode.
    /// </summary>
    private void InitializeObstacleMode()
    {
        switch (obstacleMode)
        {
            case ObstacleMode.MovingObstacle:
                if (pathPoints == null || pathPoints.Length < 2)
                {
                    Debug.LogError("MovingObstacle mode requires at least 2 path points assigned to " + gameObject.name + ". Disabling movement.");
                    enabled = false; // Disable script if not properly configured
                    return;
                }
                // Start the movement routine for moving obstacles
                StartCoroutine(MoveObstacleRoutine());
                break;

            case ObstacleMode.InteractiveDoor:
                _closedRotation = transform.rotation; // Store initial rotation as closed
                _openRotation = Quaternion.Euler(openRotationEuler); // Calculate open rotation
                // Door starts closed (active obstacle)
                _navMeshObstacle.enabled = true;
                break;

            case ObstacleMode.SimpleToggle:
                _renderer = GetComponent<Renderer>();
                if (_renderer == null)
                {
                    Debug.LogError("SimpleToggle mode requires a Renderer component for visual feedback on " + gameObject.name + ". Disabling toggle.");
                    enabled = false;
                    return;
                }
                // Obstacle starts active (carving)
                _navMeshObstacle.enabled = true;
                UpdateVisualState(_navMeshObstacle.enabled);
                break;
        }
    }

    /// <summary>
    /// Handles frame-by-frame logic specific to the obstacle's mode (e.g., input).
    /// </summary>
    private void HandleModeSpecificUpdate()
    {
        switch (obstacleMode)
        {
            case ObstacleMode.InteractiveDoor:
                HandleDoorToggleInput();
                break;
            case ObstacleMode.SimpleToggle:
                HandleSimpleToggleInput();
                break;
            // MovingObstacle handles its logic in a Coroutine
        }
    }

    // --- Moving Obstacle Logic ---

    /// <summary>
    /// Coroutine that continuously moves the obstacle along its defined path points.
    /// </summary>
    private IEnumerator MoveObstacleRoutine()
    {
        while (true) // Loop indefinitely
        {
            // If waiting at a point, pause movement
            if (!_isMoving)
            {
                yield return new WaitForSeconds(waitTime);
                _isMoving = true; // Resume movement
            }

            // Move towards the current target path point
            Vector3 targetPosition = pathPoints[_currentPathIndex].position;
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null; // Wait for the next frame
            }

            // Reached the current point, prepare for the next
            _isMoving = false; // Pause movement
            _currentPathIndex = (_currentPathIndex + 1) % pathPoints.Length; // Cycle to the next point
        }
    }

    // --- Interactive Door Logic ---

    /// <summary>
    /// Checks for input to toggle the door's state (open/closed).
    /// </summary>
    private void HandleDoorToggleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _isOpen = !_isOpen; // Toggle door state
            
            // Stop any ongoing animation and start a new one
            if (_doorAnimationCoroutine != null)
            {
                StopCoroutine(_doorAnimationCoroutine);
            }
            _doorAnimationCoroutine = StartCoroutine(AnimateDoorRoutine(_isOpen ? _openRotation : _closedRotation));
        }
    }

    /// <summary>
    /// Coroutine to animate the door's rotation and toggle its NavMeshObstacle state.
    /// </summary>
    /// <param name="targetRotation">The target rotation for the door.</param>
    private IEnumerator AnimateDoorRoutine(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;
        // Estimate duration based on the angle to rotate and a nominal speed factor
        float duration = Quaternion.Angle(startRotation, targetRotation) / (doorSpeed * 100f + 0.01f); // Avoid division by zero

        // Key decision point: When to enable/disable the NavMeshObstacle?
        // - When opening: Disable the obstacle *as it starts to open*. Agents can then path through.
        // - When closing: Enable the obstacle *after it finishes closing*. Agents will avoid it.
        if (_isOpen)
        {
            // Door is opening, disable the obstacle to allow agents to path through
            _navMeshObstacle.enabled = false;
            Debug.Log($"Door {gameObject.name} is opening. NavMeshObstacle disabled.");
        }

        // Animate the door rotation
        while (elapsedTime < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation; // Ensure the final rotation is exact

        if (!_isOpen)
        {
            // Door has finished closing, enable the obstacle to block agent paths
            _navMeshObstacle.enabled = true;
            Debug.Log($"Door {gameObject.name} is closed. NavMeshObstacle enabled.");
        }
    }

    // --- Simple Toggle Logic ---

    /// <summary>
    /// Checks for input to toggle the simple obstacle's active state.
    /// </summary>
    private void HandleSimpleToggleInput()
    {
        if (Input.GetKeyDown(toggleObstacleKey))
        {
            _navMeshObstacle.enabled = !_navMeshObstacle.enabled; // Toggle component enabled state
            UpdateVisualState(_navMeshObstacle.enabled); // Update visual feedback
            Debug.Log($"Simple obstacle {gameObject.name} toggled. NavMeshObstacle enabled: {_navMeshObstacle.enabled}");
        }
    }

    /// <summary>
    /// Updates the material of the obstacle to visually indicate its active state.
    /// </summary>
    /// <param name="isActive">True if the obstacle is currently active (carving), false otherwise.</param>
    private void UpdateVisualState(bool isActive)
    {
        if (_renderer != null)
        {
            _renderer.material = isActive ? activeMaterial : inactiveMaterial;
        }
    }

    // --- Gizmos for Editor Visualization ---

    /// <summary>
    /// Draws visual helpers in the editor for specific obstacle modes (e.g., path points).
    /// </summary>
    private void DrawPathGizmos()
    {
        if (obstacleMode == ObstacleMode.MovingObstacle && pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            // Draw spheres at each path point and lines connecting them
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] != null)
                {
                    Gizmos.DrawSphere(pathPoints[i].position, 0.2f);
                    if (i < pathPoints.Length - 1 && pathPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                    }
                    else if (pathPoints.Length > 1 && pathPoints[0] != null)
                    {
                        // Draw line back to the first point to complete the loop
                        Gizmos.DrawLine(pathPoints[i].position, pathPoints[0].position);
                    }
                }
            }
        }
    }
}
```

---

### 2. `AI_AgentController.cs` (The Reactive AI)

This simple script controls a `NavMeshAgent` and demonstrates how agents automatically react to the dynamic obstacles managed by the `DynamicObstacleController`. No special code is needed here to "know" about the obstacles; the `NavMeshAgent` handles it automatically.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent component
using System.Collections; // Not strictly needed here, but good practice for AI scripts

/// <summary>
/// AI_AgentController: A simple script to control a NavMeshAgent.
/// This demonstrates how AI agents automatically react to dynamic NavMeshObstacles
/// managed by the DynamicObstacleSystem pattern. Agents will dynamically recalculate
/// their paths to avoid obstacles that are carving the NavMesh or have their
/// NavMeshObstacle component enabled.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))] // Ensures the GameObject has a NavMeshAgent
public class AI_AgentController : MonoBehaviour
{
    [Header("Agent Settings")]
    [Tooltip("The NavMeshAgent component attached to this GameObject.")]
    private NavMeshAgent _agent;

    [Tooltip("An array of target points the agent will try to reach in sequence.")]
    public Transform[] targetPoints;

    [Tooltip("Minimum time (in seconds) before the agent considers recalculating its path to the next target.")]
    public float minPathRecalculateInterval = 1f;
    [Tooltip("Maximum time (in seconds) before the agent considers recalculating its path to the next target.")]
    public float maxPathRecalculateInterval = 3f;

    // --- Private State Variables ---
    private int _currentWaypointIndex = -1;
    private float _nextPathRecalculateTime;

    // --- MonoBehaviour Lifecycle Methods ---

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name + ". Please add one.");
            enabled = false; // Disable this script if no agent
            return;
        }

        if (targetPoints == null || targetPoints.Length == 0)
        {
            Debug.LogWarning("No target points assigned for AI_AgentController on " + gameObject.name + ". Agent will be idle.");
            enabled = false; // Disable if no targets
            return;
        }

        // Set initial recalculation time and destination
        _nextPathRecalculateTime = Time.time + Random.Range(minPathRecalculateInterval, maxPathRecalculateInterval);
        SetNextDestination();
    }

    void Update()
    {
        // Avoid setting new destinations while the current path is being calculated
        if (_agent.pathPending) return;

        // Check if agent has reached its current destination OR if it's time to recalculate path
        // _agent.remainingDistance is reliable after the agent has a path
        if ((_agent.remainingDistance <= _agent.stoppingDistance && !_agent.hasPath) || Time.time >= _nextPathRecalculateTime)
        {
            SetNextDestination();
            // Schedule the next potential path recalculation
            _nextPathRecalculateTime = Time.time + Random.Range(minPathRecalculateInterval, maxPathRecalculateInterval);
        }
    }

    void OnDrawGizmos()
    {
        // Draw gizmos for target points in the editor
        DrawTargetPointGizmos();
    }

    // --- Agent Movement Logic ---

    /// <summary>
    /// Sets the next destination for the NavMeshAgent, cycling through the target points.
    /// </summary>
    private void SetNextDestination()
    {
        if (targetPoints == null || targetPoints.Length == 0) return;

        // Move to the next waypoint in the array, looping back to the start
        _currentWaypointIndex = (_currentWaypointIndex + 1) % targetPoints.Length;
        if (targetPoints[_currentWaypointIndex] != null)
        {
            _agent.SetDestination(targetPoints[_currentWaypointIndex].position);
            Debug.Log($"{gameObject.name} moving to target: {targetPoints[_currentWaypointIndex].name}");
        }
    }

    // --- Gizmos for Editor Visualization ---

    /// <summary>
    /// Draws spheres at each target point and lines connecting them in the editor.
    /// </summary>
    private void DrawTargetPointGizmos()
    {
        if (targetPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < targetPoints.Length; i++)
            {
                if (targetPoints[i] != null)
                {
                    Gizmos.DrawSphere(targetPoints[i].position, 0.1f); // Draw a small sphere at each point
                    if (i < targetPoints.Length - 1 && targetPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(targetPoints[i].position, targetPoints[i + 1].position); // Connect points with lines
                    }
                }
            }
        }
    }
}
```