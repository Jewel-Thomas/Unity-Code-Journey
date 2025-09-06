// Unity Design Pattern Example: AIWaypointSystem
// This script demonstrates the AIWaypointSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AIWaypointSystem' design pattern in Unity separates the definition of a path (waypoints) from the logic of an AI agent that follows that path. This promotes modularity, reusability, and easier maintenance.

Here are two C# scripts that implement this pattern:

1.  **`WaypointPath.cs`**: This script defines and manages a series of waypoints, acting as the 'System' part of the pattern. It's responsible for the path's data and structure.
2.  **`AIWaypointAgent.cs`**: This script represents an AI agent that navigates the path provided by a `WaypointPath` instance. It acts as the 'Agent' part, focusing solely on movement and path consumption.

---

### 1. `WaypointPath.cs`

This script defines a path by holding a list of `Transform` references (your actual waypoints in the scene). It also handles visualization in the editor.

```csharp
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
// Required for drawing text labels in the editor (Gizmos.DrawString is deprecated)
using UnityEditor;
#endif

/// <summary>
/// AIWaypointSystem: WaypointPath
///
/// This script defines a path composed of a series of waypoints.
/// It acts as the 'System' part of the AIWaypointSystem pattern,
/// managing the path data and providing methods for agents to query it.
///
/// Design Pattern Explained:
/// The AIWaypointSystem pattern separates the path definition (WaypointPath)
/// from the AI agent's movement logic (AIWaypointAgent).
///
/// - WaypointPath: Manages a collection of ordered points (waypoints).
///   It's responsible for defining the geometry of the path, whether it loops,
///   and providing access to individual waypoints. It doesn't know or care
///   how an AI agent will use it. It's a data provider for the path.
///
/// - AIWaypointAgent: Consumes the path provided by WaypointPath.
///   It's responsible for navigating along the path, determining its next target
///   waypoint, and moving towards it. It doesn't know or care how the path
///   was defined, only that it can query it for waypoints.
///
/// This clear separation of concerns offers several benefits:
/// 1.  Reusability: Multiple AI agents can use the same `WaypointPath` instance,
///     e.g., a patrol route for several guards.
/// 2.  Flexibility: You can easily change path definitions (add/remove waypoints,
///     change looping behavior) without affecting the AI agent's movement logic.
///     Conversely, you can change how an AI agent moves without altering the path.
/// 3.  Maintainability: Clear responsibilities make the code easier to understand,
///     modify, and debug.
/// 4.  Testability: Easier to test path generation and AI movement independently.
///
/// How to Use:
/// 1.  Create an empty GameObject in your scene (e.g., "AI_Patrol_Path_1").
/// 2.  Attach this `WaypointPath` script to it.
/// 3.  To define waypoints:
///     a.  Create empty GameObjects as children of the "AI_Patrol_Path_1" GameObject
///         (e.g., "Waypoint_0", "Waypoint_1", "Waypoint_2"). Position them where you want.
///     b.  In the `WaypointPath` component's Inspector, click the three dots (...)
///         on the right of the component name and select "Populate Waypoints From Children".
///         This will automatically add all child Transforms to the `Waypoints` list.
///     c.  Alternatively, you can manually drag any `Transform` components (empty GameObjects,
///         other objects) from your scene hierarchy into the `Waypoints` list in the Inspector.
/// 4.  Adjust `Is Looping` if you want the path to repeat from the last to the first waypoint.
/// 5.  The path will be visualized with lines, spheres, and numbers in the editor,
///     making it easy to set up and visualize patrol routes.
/// </summary>
[ExecuteAlways] // Allows gizmos to be drawn even when not playing for better editing experience
public class WaypointPath : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("The list of waypoints that define this path. Drag Transforms here.")]
    public List<Transform> waypoints = new List<Transform>();

    [Tooltip("Should the AI agent loop back to the first waypoint after reaching the last?")]
    public bool isLooping = true;

    [Header("Editor Visualization")]
    [Tooltip("Color of the path lines drawn in the editor.")]
    public Color pathColor = Color.green;

    [Tooltip("Color of individual waypoint spheres in the editor.")]
    public Color waypointColor = Color.yellow;

    [Tooltip("Radius for individual waypoint gizmo spheres.")]
    [Range(0.1f, 2f)]
    public float waypointGizmoRadius = 0.3f;

    /// <summary>
    /// Returns the total number of waypoints in this path.
    /// </summary>
    public int WaypointCount => waypoints.Count;

    /// <summary>
    /// Gets a specific waypoint by its index.
    /// </summary>
    /// <param name="index">The zero-based index of the waypoint.</param>
    /// <returns>The Transform of the waypoint at the given index, or null if the index is out of range or no waypoints exist.</returns>
    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("WaypointPath: No waypoints defined in the path!", this);
            return null;
        }

        if (index < 0 || index >= waypoints.Count)
        {
            Debug.LogError($"WaypointPath: Requested index {index} is out of range. Path has {waypoints.Count} waypoints.", this);
            return null;
        }

        return waypoints[index];
    }

    /// <summary>
    /// Calculates the index of the next waypoint in the path.
    /// Handles looping if `isLooping` is true.
    /// </summary>
    /// <param name="currentIndex">The current waypoint index.</param>
    /// <returns>The index of the next waypoint. If not looping and the end is reached, it returns the current index.</returns>
    public int GetNextWaypointIndex(int currentIndex)
    {
        if (waypoints == null || waypoints.Count == 0) return -1; // No waypoints to get a next index from

        if (currentIndex < 0 || currentIndex >= waypoints.Count)
        {
            Debug.LogWarning($"WaypointPath: Current index {currentIndex} is out of range. Returning first waypoint index (0).", this);
            return 0; // Default to first waypoint if an invalid current index is provided
        }

        int nextIndex = currentIndex + 1;

        if (nextIndex >= waypoints.Count)
        {
            if (isLooping)
            {
                return 0; // Loop back to the first waypoint
            }
            else
            {
                return currentIndex; // Stay at the last waypoint if not looping
            }
        }

        return nextIndex;
    }

    /// <summary>
    /// Called by Unity to draw gizmos in the editor.
    /// This visualizes the waypoint path, showing lines between waypoints,
    /// spheres at each waypoint, and their respective indices.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        Gizmos.color = pathColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            // Draw lines between waypoints
            if (i < waypoints.Count - 1)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
            else if (isLooping && waypoints.Count > 1) // Draw line from last to first if looping
            {
                if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
                {
                    Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
                }
            }

            // Draw spheres for individual waypoints and label them
            if (waypoints[i] != null)
            {
                Gizmos.color = waypointColor;
                Gizmos.DrawSphere(waypoints[i].position, waypointGizmoRadius);

                // Draw waypoint number for easier identification in the editor
                #if UNITY_EDITOR
                Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"WP {i}");
                #endif
            }
        }
    }

    /// <summary>
    /// Editor-only method called when the script is first attached or reset in the Inspector.
    /// It attempts to automatically populate the waypoints list from direct child transforms.
    /// </summary>
    private void Reset()
    {
        PopulateWaypointsFromChildren();
    }

    /// <summary>
    /// Populates the waypoints list with all direct child Transforms of this GameObject.
    /// This method can be conveniently called from the Inspector's context menu.
    /// Ensure your individual waypoints are children of the `WaypointPath` GameObject.
    /// </summary>
    [ContextMenu("Populate Waypoints From Children")]
    public void PopulateWaypointsFromChildren()
    {
        waypoints.Clear(); // Clear existing waypoints before populating
        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }
        Debug.Log($"WaypointPath: Populated {waypoints.Count} waypoints from children of '{gameObject.name}'.", this);

        #if UNITY_EDITOR
        // Mark the object as dirty to ensure changes are saved in the editor
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}
```

---

### 2. `AIWaypointAgent.cs`

This script represents an AI agent that consumes the path defined by a `WaypointPath`. It handles movement, rotation, and advancing through the waypoints.

```csharp
using UnityEngine;
using System.Collections; // System.Collections is generally used for IEnumerator (coroutines), not strictly necessary here.

/// <summary>
/// AIWaypointSystem: AIWaypointAgent
///
/// This script defines an AI agent that navigates along a path defined by a `WaypointPath` script.
/// It acts as the 'Agent' part of the AIWaypointSystem design pattern.
///
/// Design Pattern Explained:
/// As explained in `WaypointPath.cs`, this script consumes the path information
/// without needing to know how the path itself is structured or generated.
/// It only interacts with the `WaypointPath` via its public interface
/// (`GetWaypoint`, `GetNextWaypointIndex`, `WaypointCount`, `isLooping`).
///
/// This separation makes the AI agent highly reusable. You can drop this script
/// onto any GameObject (e.g., an enemy, a civilian, a patrolling guard) and
/// simply assign a `WaypointPath` from the scene for it to follow.
///
/// The AIWaypointAgent focuses on:
/// -   Holding a reference to its target `WaypointPath`.
/// -   Managing its current position along the path (e.g., `currentWaypointIndex`).
/// -   Implementing the movement and rotation logic to reach the next waypoint.
/// -   Handling the transition to the next waypoint when the current one is reached.
/// -   Respecting the path's looping behavior.
///
/// How to Use:
/// 1.  Ensure you have a `WaypointPath` GameObject set up and configured in your scene
///     (see `WaypointPath.cs` for instructions).
/// 2.  Create an AI agent GameObject (e.g., a simple Cube, Capsule, or your character model).
///     This object will be the one moving along the path.
/// 3.  Attach this `AIWaypointAgent` script to the AI agent GameObject.
/// 4.  In the Inspector of the `AIWaypointAgent` component:
///     a.  Drag the `WaypointPath` GameObject (from step 1) into the `Target Path` field.
///     b.  Adjust `Move Speed`, `Rotation Speed`, and `Reach Distance` as needed
///         to control the agent's movement characteristics.
/// 5.  Run the Unity scene. The AI agent will automatically start moving along the
///     defined path.
/// </summary>
public class AIWaypointAgent : MonoBehaviour
{
    [Header("Path Following Settings")]
    [Tooltip("The WaypointPath script this AI agent will follow.")]
    public WaypointPath targetPath;

    [Tooltip("Movement speed of the AI agent in units per second.")]
    public float moveSpeed = 3f;

    [Tooltip("Rotation speed of the AI agent towards the next waypoint in degrees per second.")]
    public float rotationSpeed = 5f;

    [Tooltip("Distance threshold to consider a waypoint 'reached'. When the agent is closer than this, it moves to the next waypoint.")]
    public float reachDistance = 0.5f;

    // Internal state variables
    private int currentWaypointIndex = 0;
    private bool hasReachedEndOfPath = false; // True if not looping and reached the last waypoint

    /// <summary>
    /// Initializes the AI agent.
    /// Checks for a valid path and positions the agent at the start of the path.
    /// </summary>
    void Start()
    {
        if (targetPath == null)
        {
            Debug.LogError("AIWaypointAgent: Target Path is not assigned! This agent will not move.", this);
            enabled = false; // Disable the script if no path is assigned to prevent errors
            return;
        }

        if (targetPath.WaypointCount == 0)
        {
            Debug.LogWarning("AIWaypointAgent: Target Path has no waypoints defined! This agent will not move.", this);
            enabled = false; // Disable the script if the path has no waypoints
            return;
        }

        // Initialize agent position to the first waypoint
        Transform firstWaypoint = targetPath.GetWaypoint(currentWaypointIndex);
        if (firstWaypoint != null)
        {
            transform.position = firstWaypoint.position;
        }
        else
        {
            Debug.LogError($"AIWaypointAgent: First waypoint at index {currentWaypointIndex} is null. Agent cannot start.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Called once per frame. Handles the AI agent's continuous movement and rotation.
    /// </summary>
    void Update()
    {
        // If no path is assigned, or if the agent has completed a non-looping path, do nothing.
        if (targetPath == null || hasReachedEndOfPath)
        {
            return;
        }

        // Get the current target waypoint's transform from the WaypointPath
        Transform targetWaypoint = targetPath.GetWaypoint(currentWaypointIndex);

        // If for some reason the target waypoint is null (e.g., deleted during runtime), stop the agent.
        if (targetWaypoint == null)
        {
            Debug.LogError($"AIWaypointAgent: Current target waypoint at index {currentWaypointIndex} is null. Stopping agent.", this);
            hasReachedEndOfPath = true; // Effectively stop the agent
            return;
        }

        // --- 1. Move towards the target waypoint ---
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

        // --- 2. Rotate towards the target waypoint ---
        Vector3 directionToWaypoint = targetWaypoint.position - transform.position;
        // Only rotate if there's a significant direction to avoid Gimbal Lock/jittering when very close
        if (directionToWaypoint.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- 3. Check if the current waypoint has been reached ---
        if (Vector3.Distance(transform.position, targetWaypoint.position) < reachDistance)
        {
            // Get the index of the next waypoint in the path.
            // The WaypointPath script handles the logic for looping or stopping at the end.
            int nextIndex = targetPath.GetNextWaypointIndex(currentWaypointIndex);

            // If not looping and GetNextWaypointIndex returns the same index,
            // it means we've reached the final waypoint of a non-looping path.
            if (!targetPath.isLooping && nextIndex == currentWaypointIndex)
            {
                Debug.Log($"AIWaypointAgent: Reached end of non-looping path at waypoint {currentWaypointIndex} on '{targetPath.name}'.", this);
                hasReachedEndOfPath = true; // Mark as complete
                return; // Stop processing movement
            }

            // Move to the next waypoint
            currentWaypointIndex = nextIndex;

            // Optional: Add a small delay or animation trigger here before moving to the next waypoint
            // e.g., StartCoroutine(WaitAtWaypoint(delayTime));
        }
    }

    /// <summary>
    /// Draws a sphere at the agent's position in the editor for easy identification,
    /// and a line to its current target waypoint.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw a wire sphere around the agent to easily spot it when selected
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.75f);

        // Draw a line to the current target waypoint if a path and waypoint exist
        if (targetPath != null && currentWaypointIndex >= 0 && currentWaypointIndex < targetPath.WaypointCount)
        {
            Transform nextWaypoint = targetPath.GetWaypoint(currentWaypointIndex);
            if (nextWaypoint != null)
            {
                Gizmos.color = Color.red; // Line color to the current target
                Gizmos.DrawLine(transform.position, nextWaypoint.position);
                Gizmos.DrawSphere(nextWaypoint.position, 0.2f); // Mark the current target waypoint
            }
        }
    }
}
```

---

### How to Set Up in Unity:

1.  **Create Scripts**:
    *   Create a new C# script named `WaypointPath.cs` and copy the first code block into it.
    *   Create another new C# script named `AIWaypointAgent.cs` and copy the second code block into it.

2.  **Set Up the Waypoint Path**:
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> `Create Empty`). Name it something like "PatrolPath_Alpha".
    *   Attach the `WaypointPath.cs` script to "PatrolPath_Alpha".
    *   As children of "PatrolPath_Alpha", create several more empty GameObjects (e.g., "Waypoint_0", "Waypoint_1", "Waypoint_2"). Position these child GameObjects where you want your path points to be.
    *   Select "PatrolPath_Alpha" in the Hierarchy. In its Inspector, click the three dots (...) on the `WaypointPath` component and select "Populate Waypoints From Children". This will automatically add your child waypoints to the list.
    *   Adjust the `Is Looping` checkbox on "PatrolPath_Alpha" if you want the AI to continuously loop.
    *   You'll see green lines connecting your waypoints and yellow spheres at each point, visualizing your path in the Scene view.

3.  **Set Up the AI Agent**:
    *   Create another GameObject that will represent your AI agent (e.g., a 3D Cube, Capsule, or your character model). Name it "MyAI_Agent".
    *   Attach the `AIWaypointAgent.cs` script to "MyAI_Agent".
    *   Select "MyAI_Agent" in the Hierarchy. In its Inspector, drag your "PatrolPath_Alpha" GameObject (from step 2) into the `Target Path` field of the `AIWaypointAgent` component.
    *   Adjust the `Move Speed`, `Rotation Speed`, and `Reach Distance` values as desired.

4.  **Run the Scene**:
    *   Press Play in Unity. Your "MyAI_Agent" should now start moving along the path defined by "PatrolPath_Alpha", demonstrating the AIWaypointSystem pattern in action!

This example provides a robust and easy-to-use foundation for implementing AI navigation along predefined paths in your Unity projects, with clear separation of concerns as dictated by the design pattern.