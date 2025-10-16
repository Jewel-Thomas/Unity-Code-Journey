// Unity Design Pattern Example: RailShooterSystem
// This script demonstrates the RailShooterSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Rail Shooter System is a design pattern used in games where the player character or camera automatically moves along a predefined path ("rail"), while the player's interaction is primarily limited to aiming, shooting, and sometimes limited lateral/vertical movement within a constrained area relative to the rail.

This pattern is great for creating cinematic sequences, guiding players through levels, or in games where narrative progression is tied to environmental movement.

## RailShooterSystem Design Pattern in Unity

This example demonstrates a complete `RailShooterSystem` in Unity.

**Key Components & Concepts:**

1.  **Path Definition:** A series of `Transform` waypoints defining the rail.
2.  **Rail Follower:** An object (typically the camera or a parent of the player character) that automatically moves and rotates along the predefined path.
3.  **Player Constrained Movement:** The player's actual avatar or aiming reticle can move within a limited local space *relative to the Rail Follower's position* on the path. This allows for aiming and dodging without affecting the forward progress.
4.  **Shooting/Interaction:** Player input for actions like firing projectiles, triggering events, etc., based on their constrained position.
5.  **Path Progression:** The system manages advancing from one waypoint to the next, handling speeds, and detecting path completion.

---

### `RailShooterSystem.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Used for List<Transform> if preferred over array

/// <summary>
///     The core script for the Rail Shooter System design pattern.
///     Manages the automatic movement of a 'Rail Follower' along a predefined path,
///     and allows for constrained player movement and shooting relative to the follower.
/// </summary>
/// <remarks>
///     **How to Use:**
///     1.  Create an empty GameObject in your scene, name it "RailShooterManager", and attach this script.
///     2.  Create an empty GameObject to act as the 'Rail Follower' (e.g., "CameraRig").
///         This object will move along the rail. Your actual Main Camera should be a child of this object.
///         Assign "CameraRig" to the 'Rail Follower' field in the inspector.
///     3.  Create an empty GameObject to act as the 'Player Character' or 'Player View' (e.g., "PlayerView").
///         Make this a child of your 'Rail Follower' ("CameraRig"). This is the object whose local position
///         will be adjusted by player input for constrained movement.
///         Assign "PlayerView" to the 'Player Character' field in the inspector.
///     4.  Create an empty GameObject named "FirePoint" as a child of your 'Player Character' ("PlayerView").
///         Assign "FirePoint" to the 'Fire Point' field in the inspector.
///     5.  Create several empty GameObjects (e.g., "Waypoint_01", "Waypoint_02", etc.)
///         and place them sequentially in your scene to define the path.
///         Drag these Waypoint GameObjects into the 'Path Waypoints' array in the inspector.
///     6.  Create a simple Prefab for your projectile (e.g., a Sphere with a Rigidbody and a simple script
///         to move forward and destroy itself after a few seconds).
///         Assign this Prefab to the 'Projectile Prefab' field.
///     7.  Adjust speed, offsets, and other settings in the inspector.
///     8.  Ensure your Input Manager has "Horizontal" and "Vertical" axes set up for player movement,
///         and "Fire1" for shooting (usually Mouse 0 or Left Ctrl).
/// </remarks>
public class RailShooterSystem : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("The objects defining the path waypoints the railFollower will move along.")]
    [SerializeField] private Transform[] pathWaypoints;
    [Tooltip("The speed at which the railFollower moves along the path.")]
    [SerializeField] private float movementSpeed = 5f;
    [Tooltip("The speed at which the railFollower rotates to look at the next waypoint.")]
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("How close the railFollower needs to be to a waypoint to consider it 'reached'.")]
    [SerializeField] private float waypointReachThreshold = 0.1f;

    [Header("Rail Follower & Player Movement")]
    [Tooltip("The Transform that automatically moves along the rail (e.g., your Camera Rig).")]
    [SerializeField] private Transform railFollower;
    [Tooltip("The Transform representing the player's view or character model, child of Rail Follower, that moves with constrained input.")]
    [SerializeField] private Transform playerCharacter;
    [Tooltip("Speed for player's lateral (left/right) movement relative to the rail.")]
    [SerializeField] private float lateralMoveSpeed = 3f;
    [Tooltip("Speed for player's vertical (up/down) movement relative to the rail.")]
    [SerializeField] private float verticalMoveSpeed = 2f;
    [Tooltip("Maximum allowed lateral offset (left/right) from the center of the rail.")]
    [SerializeField] private float maxLateralOffset = 3f;
    [Tooltip("Maximum allowed vertical offset (up/down) from the center of the rail.")]
    [SerializeField] private float maxVerticalOffset = 2f;

    [Header("Shooting Settings")]
    [Tooltip("The prefab for the projectile fired by the player.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The Transform from which projectiles are fired (child of Player Character).")]
    [SerializeField] private Transform firePoint;
    [Tooltip("The rate (in seconds) at which the player can fire projectiles.")]
    [SerializeField] private float fireRate = 0.5f;

    // Internal state variables
    private int currentWaypointIndex = 0;
    private float nextFireTime;
    private bool pathCompleted = false;

    // Current offsets for player character relative to the rail follower's local position
    private float currentLateralOffset;
    private float currentVerticalOffset;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Performs initial setup and validation.
    /// </summary>
    private void Awake()
    {
        // Validate essential references
        if (railFollower == null)
        {
            Debug.LogError("Rail Follower Transform is not assigned! Please assign the object that follows the rail.", this);
            enabled = false; // Disable script if critical component is missing
            return;
        }
        if (playerCharacter == null)
        {
            Debug.LogError("Player Character Transform is not assigned! Please assign the object that moves with constrained input.", this);
            enabled = false;
            return;
        }
        if (firePoint == null)
        {
            Debug.LogWarning("Fire Point Transform is not assigned! Shooting might not work correctly.", this);
        }
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile Prefab is not assigned! Shooting might not work correctly.", this);
        }
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogError("No Path Waypoints assigned! The rail follower has nowhere to go.", this);
            enabled = false;
            return;
        }

        // Initialize player character's local offsets to its current position relative to railFollower
        Vector3 localPos = playerCharacter.localPosition;
        currentLateralOffset = localPos.x;
        currentVerticalOffset = localPos.y;

        Debug.Log("RailShooterSystem initialized. Current Waypoint: " + currentWaypointIndex);
    }

    /// <summary>
    /// Update is called once per frame.
    /// Handles path movement, player input, and shooting.
    /// </summary>
    private void Update()
    {
        if (pathCompleted)
        {
            // Debug.Log("Path completed. System idle.");
            return; // Stop processing once the path is finished
        }

        MoveRailFollowerAlongPath();
        HandlePlayerConstrainedMovement();
        HandleShootingInput();
    }

    /// <summary>
    /// Moves the railFollower towards the current target waypoint and rotates it.
    /// Advances to the next waypoint when the current one is reached.
    /// </summary>
    private void MoveRailFollowerAlongPath()
    {
        if (currentWaypointIndex >= pathWaypoints.Length)
        {
            PathCompleted();
            return;
        }

        Transform targetWaypoint = pathWaypoints[currentWaypointIndex];

        // Move the railFollower towards the target waypoint
        railFollower.position = Vector3.MoveTowards(railFollower.position, targetWaypoint.position, movementSpeed * Time.deltaTime);

        // Rotate the railFollower to look at the target waypoint
        Vector3 directionToTarget = targetWaypoint.position - railFollower.position;
        if (directionToTarget != Vector3.zero) // Avoid looking at (0,0,0) if target is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            railFollower.rotation = Quaternion.Slerp(railFollower.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Check if the railFollower has reached the current waypoint
        if (Vector3.Distance(railFollower.position, targetWaypoint.position) < waypointReachThreshold)
        {
            currentWaypointIndex++;
            Debug.Log("Reached Waypoint " + (currentWaypointIndex) + ". Moving to next.");

            // If this was the last waypoint, the path is complete
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                PathCompleted();
            }
        }
    }

    /// <summary>
    /// Handles player input for lateral and vertical movement,
    /// constraining the playerCharacter's local position.
    /// </summary>
    private void HandlePlayerConstrainedMovement()
    {
        // Get input for lateral and vertical movement
        float hInput = Input.GetAxis("Horizontal");
        float vInput = Input.GetAxis("Vertical");

        // Calculate desired offsets based on input and speed
        currentLateralOffset += hInput * lateralMoveSpeed * Time.deltaTime;
        currentVerticalOffset += vInput * verticalMoveSpeed * Time.deltaTime;

        // Clamp offsets within defined limits
        currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralOffset, maxLateralOffset);
        currentVerticalOffset = Mathf.Clamp(currentVerticalOffset, -maxVerticalOffset, maxVerticalOffset);

        // Apply the constrained local position to the player character
        // We set the Z-position to its initial value to ensure it doesn't move forward/backward
        playerCharacter.localPosition = new Vector3(currentLateralOffset, currentVerticalOffset, playerCharacter.localPosition.z);
    }

    /// <summary>
    /// Handles player input for shooting, respecting the fire rate.
    /// </summary>
    private void HandleShootingInput()
    {
        if (firePoint == null || projectilePrefab == null)
        {
            return; // Cannot shoot if fire point or projectile is missing
        }

        // Check for fire input and fire rate
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            // Instantiate projectile at fire point's position and rotation
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            nextFireTime = Time.time + fireRate; // Set next allowed fire time
            // Debug.Log("Player Fired!");
        }
    }

    /// <summary>
    /// Called when the railFollower has traversed all waypoints.
    /// </summary>
    private void PathCompleted()
    {
        pathCompleted = true;
        Debug.Log("--- Rail Shooter Path Completed! ---");
        // Here you can trigger end-of-level events, load new scenes, show victory screens, etc.
        // For this example, we'll just stop movement.
        enabled = false; // Disable the script to stop further updates
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize the path and settings.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            return;
        }

        // Draw path lines
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pathWaypoints.Length; i++)
        {
            if (pathWaypoints[i] != null)
            {
                // Draw a sphere for each waypoint
                Gizmos.DrawWireSphere(pathWaypoints[i].position, waypointReachThreshold);

                // Draw a line connecting waypoints
                if (i > 0 && pathWaypoints[i - 1] != null)
                {
                    Gizmos.DrawLine(pathWaypoints[i - 1].position, pathWaypoints[i].position);
                }
            }
        }

        // Draw current path progression
        if (Application.isPlaying && railFollower != null && currentWaypointIndex < pathWaypoints.Length)
        {
            Gizmos.color = Color.green;
            // Draw a line from current railFollower position to the next waypoint
            Gizmos.DrawLine(railFollower.position, pathWaypoints[currentWaypointIndex].position);
            // Draw a sphere for the current railFollower position
            Gizmos.DrawSphere(railFollower.position, 0.5f);
        }

        // Draw player character's constrained movement limits
        if (playerCharacter != null && railFollower != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = railFollower.position + railFollower.rotation * playerCharacter.localPosition; // Get world position of playerCharacter
            Vector3 extents = new Vector3(maxLateralOffset, maxVerticalOffset, 0); // Only X and Y for local offsets

            // Draw a wire cube representing the local movement boundaries
            // We need to transform the local offsets to world space relative to the railFollower's orientation
            // Calculate the 4 corners of the local bounds in world space
            Vector3 topLeft = railFollower.position + railFollower.right * -maxLateralOffset + railFollower.up * maxVerticalOffset + railFollower.forward * playerCharacter.localPosition.z;
            Vector3 topRight = railFollower.position + railFollower.right * maxLateralOffset + railFollower.up * maxVerticalOffset + railFollower.forward * playerCharacter.localPosition.z;
            Vector3 bottomLeft = railFollower.position + railFollower.right * -maxLateralOffset + railFollower.up * -maxVerticalOffset + railFollower.forward * playerCharacter.localPosition.z;
            Vector3 bottomRight = railFollower.position + railFollower.right * maxLateralOffset + railFollower.up * -maxVerticalOffset + railFollower.forward * playerCharacter.localPosition.z;
            
            // Draw lines to form the rectangle
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
    }
}
```