// Unity Design Pattern Example: WaypointSystem
// This script demonstrates the WaypointSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Waypoint System design pattern is a fundamental concept in game development, especially for AI pathfinding, cinematic sequences, or guided tours. It involves defining a series of points (waypoints) in the game world that an entity (like an AI character, camera, or player) can follow.

This C# Unity example provides a robust and practical implementation of the Waypoint System.

---

### WaypointSystem Design Pattern Explained

1.  **Waypoint:** A specific point in the game world. In Unity, this is most often represented by a `Transform` component (attached to an empty GameObject) because `Transform` already provides position, rotation, and hierarchical parent/child relationships.
2.  **Waypoint System (Manager):** A central component that manages a collection of waypoints. It defines the path and the rules for traversing it (e.g., loop, ping-pong, once). It provides methods for agents to query for the next waypoint in the sequence.
3.  **Agent:** The entity that traverses the waypoints (e.g., an AI character, a moving platform, a camera). The agent queries the Waypoint System for its next target waypoint and moves towards it. The agent manages its own state (current waypoint index, speed, etc.).

**Benefits of the Waypoint System Pattern:**

*   **Ease of Design:** Paths can be easily laid out and modified directly in the Unity editor by moving empty GameObjects.
*   **Performance:** Simpler than complex pathfinding algorithms (like A\* or NavMesh) for predefined paths, leading to better performance.
*   **Predictability:** Ensures agents follow a specific, intended route.
*   **Flexibility:** Different traversal modes (loop, ping-pong, once) can be implemented.

---

### Complete C# Unity WaypointSystem Example

This script, `WaypointSystem.cs`, can be dropped directly into your Unity project.

**How to Use in Unity:**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty). Rename it to something like `Path_PatrolRoute1`.
2.  **Attach Script:** Drag and drop the `WaypointSystem.cs` script onto this `Path_PatrolRoute1` GameObject.
3.  **Create Waypoints:**
    *   Make `Path_PatrolRoute1` your selected GameObject.
    *   Right-click on `Path_PatrolRoute1` in the Hierarchy -> Create Empty. Rename this new empty GameObject to `Waypoint_0`.
    *   Duplicate `Waypoint_0` (Ctrl+D or Cmd+D) multiple times, renaming them `Waypoint_1`, `Waypoint_2`, etc. These are your actual waypoints.
    *   Position these `Waypoint_X` GameObjects in your scene to define the path you want an agent to follow.
4.  **Assign Waypoints:**
    *   Select `Path_PatrolRoute1` again. In its Inspector, you'll see the `WaypointSystem` component.
    *   Set the `Waypoints` array size to the number of waypoints you created (e.g., 3 if you have Waypoint_0, Waypoint_1, Waypoint_2).
    *   Drag your `Waypoint_0`, `Waypoint_1`, `Waypoint_2` GameObjects from the Hierarchy into the respective `Element 0`, `Element 1`, `Element 2` slots of the `Waypoints` array in the Inspector. Ensure they are in the correct order.
5.  **Configure Traversal:** Choose the `Traversal Mode` (Loop, Ping Pong, Once) from the dropdown in the Inspector.
6.  **Visualize:** The path will be drawn in the editor using Gizmos, making it easy to see and adjust.
7.  **Agent Setup (Example):** See the `AI_Agent` example script in the comments below for how an AI character would use this `WaypointSystem`. You would create a separate C# script for your agent (e.g., `AI_Agent.cs`), attach it to your AI character, and drag your `Path_PatrolRoute1` GameObject into its `Target Waypoint System` slot.

---

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator in example usage comments
using System.Collections.Generic; // Required for List if you were to use it, but array is fine here.

// Define the different ways an agent can traverse the path.
public enum WaypointTraversalMode
{
    Loop,       // After reaching the last waypoint, goes back to the first.
    PingPong,   // After reaching the last waypoint, goes backward to the first, then forward again, and so on.
    Once        // After reaching the last waypoint, stops.
}

/// <summary>
///     The WaypointSystem is a design pattern used to define and manage a path
///     made of distinct points (waypoints) that an agent (e.g., AI character,
///     moving platform, camera) can follow.
///
///     This script should be attached to an empty GameObject in the scene,
///     and its 'Waypoints' array should be populated with the Transforms
///     of child GameObjects representing the individual waypoints.
/// </summary>
/// <remarks>
///     **How it works:**
///     1.  **Waypoints:** A collection of `Transform` components act as the waypoints.
///         These are typically empty child GameObjects positioned in the scene.
///     2.  **Traversal Mode:** Defines the rules for moving through the waypoints
///         (Loop, PingPong, Once).
///     3.  **Agent Interaction:** Provides public methods for an 'agent' (another script)
///         to query for a specific waypoint's `Transform` or to calculate the next
///         waypoint in the sequence based on the current index and traversal mode.
///     4.  **Editor Visualization:** Uses `OnDrawGizmos()` to clearly display the
///         path and waypoints in the Unity editor for easy setup and debugging.
/// </remarks>
public class WaypointSystem : MonoBehaviour
{
    [Header("Waypoint Configuration")]
    [Tooltip("The ordered list of Transform components that define the path's waypoints.\n" +
             "It's recommended to make these child GameObjects for easier organization.")]
    [SerializeField]
    private Transform[] waypoints; // Array of Transforms serves as our waypoints.

    [Tooltip("Determines how agents should traverse the waypoints.")]
    [SerializeField]
    private WaypointTraversalMode traversalMode = WaypointTraversalMode.Loop;

    [Header("Editor Visualization")]
    [Tooltip("Color of the path lines drawn between waypoints in the editor.")]
    [SerializeField]
    private Color pathColor = Color.blue;

    [Tooltip("Color of the spheres drawn at each waypoint position in the editor.")]
    [SerializeField]
    private Color waypointColor = Color.cyan;

    [Tooltip("Radius of the spheres drawn for waypoints in the editor.")]
    [SerializeField]
    private float waypointGizmoRadius = 0.3f;

    /// <summary>
    /// Gets the total number of waypoints currently configured in this system.
    /// </summary>
    public int TotalWaypoints => waypoints.Length;

    // --- Public Methods for Agent Interaction ---

    /// <summary>
    /// Retrieves the Transform component of a specific waypoint by its index.
    /// Agents use this to get the target position to move towards.
    /// </summary>
    /// <param name="index">The zero-based index of the desired waypoint.</param>
    /// <returns>The <see cref="Transform"/> component of the waypoint at the specified index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown if the provided index is out of the valid range [0, TotalWaypoints - 1].
    /// </exception>
    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Length)
        {
            throw new System.ArgumentOutOfRangeException(
                $"Index {index} is out of bounds for WaypointSystem '{name}' " +
                $"which has {waypoints.Length} waypoints.");
        }
        return waypoints[index];
    }

    /// <summary>
    /// Calculates the next waypoint index an agent should target, based on the
    /// current index and the WaypointSystem's <see cref="WaypointTraversalMode"/>.
    /// </summary>
    /// <param name="currentWaypointIndex">
    /// The current waypoint index the agent has just reached or is moving towards.
    /// </param>
    /// <param name="pathDirection">
    /// A reference to an integer that represents the agent's current direction
    /// along the path (1 for forward, -1 for backward). This parameter is
    /// modified by the WaypointSystem for <see cref="WaypointTraversalMode.PingPong"/> mode.
    /// </param>
    /// <returns>
    /// The index of the next waypoint. Returns -1 if the path has ended
    /// (e.g., in 'Once' mode when the last waypoint is reached).
    /// </returns>
    public int GetNextWaypointIndex(int currentWaypointIndex, ref int pathDirection)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"WaypointSystem '{name}' has no waypoints configured.");
            return -1; // No waypoints to traverse.
        }

        switch (traversalMode)
        {
            case WaypointTraversalMode.Loop:
                // Move to the next waypoint, wrapping around to the first if at the end.
                return (currentWaypointIndex + 1) % waypoints.Length;

            case WaypointTraversalMode.PingPong:
                int nextIndex = currentWaypointIndex + pathDirection;

                // Check if we need to reverse direction (hit an end of the path)
                if (nextIndex >= waypoints.Length || nextIndex < 0)
                {
                    pathDirection *= -1; // Reverse the direction
                    // Recalculate the next index with the new direction.
                    // If we were at the last waypoint (N-1) and pathDirection was 1,
                    // nextIndex would be N. After reversing direction to -1,
                    // the new nextIndex will be (N-1) + (-1) = N-2. This is correct.
                    // Similarly, if we were at 0 and pathDirection was -1,
                    // nextIndex would be -1. After reversing to 1,
                    // new nextIndex will be 0 + 1 = 1. Correct.
                    nextIndex = currentWaypointIndex + pathDirection;
                }
                return nextIndex;

            case WaypointTraversalMode.Once:
                // If not at the last waypoint, move to the next.
                // If at the last waypoint, return -1 to signify the end of the path.
                return (currentWaypointIndex < waypoints.Length - 1) ? (currentWaypointIndex + 1) : -1;

            default:
                Debug.LogError($"WaypointSystem: Unhandled traversal mode: {traversalMode}. Returning -1.");
                return -1; // Should not happen with valid enum values.
        }
    }

    // --- Editor-Specific Visualization (Gizmos) ---

    /// <summary>
    /// Draws visual debugging aids in the Unity editor (scene view).
    /// This method is called automatically by Unity to draw Gizmos.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Don't draw if no waypoints are set up.
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        // Set the color for path lines.
        Gizmos.color = pathColor;

        // Draw lines connecting the waypoints to visualize the path.
        for (int i = 0; i < waypoints.Length; i++)
        {
            // Skip if a waypoint Transform is null (e.g., deleted object).
            if (waypoints[i] == null) continue;

            // Draw a line from the current waypoint to the next.
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
            // If in Loop mode, draw a line from the last waypoint back to the first.
            else if (traversalMode == WaypointTraversalMode.Loop && waypoints.Length > 1 && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }

        // Set the color for waypoint spheres.
        Gizmos.color = waypointColor;

        // Draw a sphere at each waypoint's position.
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, waypointGizmoRadius);
            }
        }
    }
}

/*
/// <summary>
///     *** EXAMPLE USAGE: AI Agent Script (This would typically be a separate C# file) ***
/// </summary>
/// <remarks>
///     This script demonstrates how an AI agent (e.g., a character, a moving platform)
///     would interact with the `WaypointSystem`.
///     
///     To use this example:
///     1.  Create a new C# script named `AI_Agent.cs`.
///     2.  Copy and paste the code below into `AI_Agent.cs`.
///     3.  Attach `AI_Agent.cs` to your AI character GameObject.
///     4.  In the Inspector for the `AI_Agent` component, drag your `WaypointSystem`
///         GameObject (e.g., `Path_PatrolRoute1`) into the `Target Waypoint System` slot.
///     5.  Run the scene! Your AI agent should start moving along the path.
/// </remarks>
*/
/*
using UnityEngine;
using System.Collections; // Required for Coroutines

public class AI_Agent : MonoBehaviour
{
    [Header("Agent Settings")]
    [Tooltip("The WaypointSystem this agent should follow.")]
    [SerializeField]
    private WaypointSystem targetWaypointSystem;

    [Tooltip("Speed at which the agent moves.")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Tooltip("Distance threshold to consider a waypoint 'reached'.")]
    [SerializeField]
    private float arrivalThreshold = 0.5f;

    [Tooltip("Time to pause at each waypoint (optional).")]
    [SerializeField]
    private float pauseTimeAtWaypoint = 0f;

    // Internal state variables for the agent's path following.
    private int currentWaypointIndex = 0;
    private int pathDirection = 1; // 1 for forward, -1 for backward (used by PingPong mode).
    private Transform currentTargetWaypoint;
    private bool isMoving = false;

    void Start()
    {
        // Basic validation for WaypointSystem.
        if (targetWaypointSystem == null)
        {
            Debug.LogError($"AI_Agent '{name}': No WaypointSystem assigned! Disabling agent movement.");
            enabled = false; // Disable this script if no system is assigned.
            return;
        }

        if (targetWaypointSystem.TotalWaypoints == 0)
        {
            Debug.LogWarning($"AI_Agent '{name}': Assigned WaypointSystem '{targetWaypointSystem.name}' has no waypoints. Agent will stand still.");
            enabled = false;
            return;
        }

        // Initialize the agent's current target to the first waypoint.
        currentWaypointIndex = 0;
        currentTargetWaypoint = targetWaypointSystem.GetWaypoint(currentWaypointIndex);
        
        // Start the movement coroutine.
        StartMovement();
    }

    /// <summary>
    /// Initiates the agent's movement along the path.
    /// </summary>
    public void StartMovement()
    {
        if (!isMoving)
        {
            isMoving = true;
            StartCoroutine(MoveAlongPath());
        }
    }

    /// <summary>
    /// Halts the agent's movement along the path.
    /// </summary>
    public void StopMovement()
    {
        if (isMoving)
        {
            isMoving = false;
            StopAllCoroutines(); // Stop the movement coroutine.
        }
    }

    /// <summary>
    /// Coroutine that continuously moves the agent towards its current target waypoint
    /// and updates the target when a waypoint is reached.
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (isMoving)
        {
            // If for some reason the target waypoint becomes null (e.g., deleted in editor), stop.
            if (currentTargetWaypoint == null)
            {
                Debug.LogWarning($"AI_Agent '{name}': Current target waypoint is null. Stopping movement.");
                StopMovement();
                yield break;
            }

            // Calculate the direction to the target waypoint.
            Vector3 directionToTarget = currentTargetWaypoint.position - transform.position;

            // Move the agent towards the target waypoint.
            transform.position = Vector3.MoveTowards(transform.position, currentTargetWaypoint.position, moveSpeed * Time.deltaTime);

            // Optional: Rotate the agent to face the direction of movement.
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime * 5f); // Rotate faster than move.
            }

            // Check if the agent has reached the current waypoint within the threshold.
            if (Vector3.Distance(transform.position, currentTargetWaypoint.position) < arrivalThreshold)
            {
                // Pause for a moment if pauseTimeAtWaypoint is set.
                if (pauseTimeAtWaypoint > 0)
                {
                    yield return new WaitForSeconds(pauseTimeAtWaypoint);
                }

                // Request the next waypoint index from the WaypointSystem.
                // The 'pathDirection' is passed by reference and might be updated by the WaypointSystem
                // if PingPong mode is active.
                int nextIndex = targetWaypointSystem.GetNextWaypointIndex(currentWaypointIndex, ref pathDirection);

                if (nextIndex == -1)
                {
                    // The path has ended (e.g., in 'Once' mode when the last waypoint is reached).
                    Debug.Log($"AI_Agent '{name}': Reached end of path on WaypointSystem '{targetWaypointSystem.name}'. Stopping.");
                    StopMovement();
                    yield break; // Exit the coroutine.
                }
                else
                {
                    // Update to the next waypoint.
                    currentWaypointIndex = nextIndex;
                    currentTargetWaypoint = targetWaypointSystem.GetWaypoint(currentWaypointIndex);
                }
            }
            yield return null; // Wait for the next frame before continuing the loop.
        }
    }

    /// <summary>
    /// Draws visual aids in the Unity editor to show the agent and its current target.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw a small red sphere at the agent's position.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.4f);

        // If the agent has a current target waypoint, draw a line to it and a sphere there.
        if (currentTargetWaypoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTargetWaypoint.position);
            Gizmos.DrawSphere(currentTargetWaypoint.position, 0.2f);
        }
    }
}
*/
```