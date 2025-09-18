// Unity Design Pattern Example: LadderClimbSystem
// This script demonstrates the LadderClimbSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **'LadderClimbSystem' design pattern** in Unity. While not a classical design pattern like Singleton or Observer, it represents a common *system-level pattern* used in game development to structure a specific game mechanic.

The core idea is to break down the "ladder climbing" mechanic into distinct, collaborating components, separating concerns and managing the player's state effectively.

### LadderClimbSystem Pattern Breakdown:

1.  **`Ladder` Component:**
    *   **Role:** Defines what a ladder *is*. It holds the physical properties, climbable bounds, entry/exit points, and orientation for climbing.
    *   **Responsibility:** Provides data and helper methods to the player about itself (e.g., where to climb, where to exit, vertical limits). It doesn't control player movement directly.
    *   **Pattern Aspect:** Acts as the 'Context' or 'Data Source' for the climbing operation.

2.  **`PlayerClimbingController` Component:**
    *   **Role:** Defines how a player *climbs* a ladder. It manages the player's state (climbing vs. not climbing), detects ladders, handles input during climbing, and transitions between states.
    *   **Responsibility:** Detects `Ladder` components, initiates/terminates climbing, handles player movement and rotation while on a ladder, and temporarily overrides or disables the player's normal movement controls.
    *   **Pattern Aspect:** Implements a 'State' or 'Strategy' pattern where the player switches from a general locomotion state to a specialized climbing state. It's also the 'Client' that interacts with the `Ladder` objects.

### Benefits of this approach:

*   **Separation of Concerns:** The `Ladder` doesn't need to know *how* a player climbs, and the `PlayerClimbingController` doesn't need to know the specific geometry of every ladder. Each component has a clear, single responsibility.
*   **Modularity:** You can create many different `Ladder` prefabs with unique geometries and exit points, and the same `PlayerClimbingController` will interact with all of them.
*   **Extensibility:**
    *   You could easily extend `Ladder` to have different climbing speeds, types of ladders (e.g., broken, slippery).
    *   You could add animations to `PlayerClimbingController` without affecting the core logic.
    *   The system allows for easy integration with existing player controllers by enabling/disabling them.
*   **Maintainability:** Changes to climbing physics or ladder properties are localized to their respective scripts.

---

### `Ladder.cs` Script

This script defines a climbable ladder. Attach it to your ladder GameObject.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Although not directly used here, good practice for common Unity namespaces.

/// <summary>
/// Defines the behavior and properties of a climbable ladder in the game.
/// This component acts as the 'data source' and 'interaction point' for the climbing system.
/// It provides information about its climbable area, entry/exit points, and player alignment.
/// </summary>
[RequireComponent(typeof(Collider))] // Ladders need a collider to define their climbable area.
public class Ladder : MonoBehaviour
{
    [Tooltip("The transform representing the typical entry/bottom point of the ladder.")]
    public Transform entryPoint;
    [Tooltip("The transform representing the top exit point of the ladder.")]
    public Transform topExitPoint;
    [Tooltip("The transform representing the bottom exit point of the ladder (optional, defaults to entry if null).")]
    public Transform bottomExitPoint;
    [Tooltip("The local direction the player should face when climbing the ladder (e.g., usually Vector3.forward of the ladder object).")]
    public Vector3 ladderForwardDirection = Vector3.forward;
    [Tooltip("The distance the player is offset from the ladder's surface along its forward direction when climbing. Adjust to prevent clipping.")]
    public float climbDepthOffset = 0.2f;

    private Collider _ladderCollider; // The collider defining the ladder's volume.

    // Enum for clarity when requesting exit points.
    public enum LadderExitDirection { Top, Bottom }

    void Awake()
    {
        _ladderCollider = GetComponent<Collider>();
        if (_ladderCollider == null)
        {
            Debug.LogError("Ladder component requires a Collider!", this);
            enabled = false; // Disable if no collider found.
        }

        // It's good practice for ladder colliders to be triggers for player detection,
        // but PlayerClimbingController uses OverlapSphere, which works with non-triggers too.
        // If you rely on OnTriggerEnter/Exit on the player, this should be a trigger.
        // For this example, it's not strictly required, but setting it true is common.
        if (!_ladderCollider.isTrigger)
        {
            Debug.LogWarning($"Ladder collider on {gameObject.name} is not set as a trigger. This is fine for OverlapSphere detection but consider it for other interactions.", this);
        }

        // Set default exit points if they are not assigned in the Inspector.
        // This ensures the system always has valid points to work with.
        if (entryPoint == null) entryPoint = transform; 
        if (topExitPoint == null) topExitPoint = transform;
        if (bottomExitPoint == null) bottomExitPoint = entryPoint; // By default, bottom exit is same as entry.
    }

    /// <summary>
    /// Gets the world space target position for a climbing player, clamping their vertical movement
    /// within the ladder's bounds and aligning them horizontally.
    /// </summary>
    /// <param name="climberTransform">The transform of the climbing player.</param>
    /// <param name="verticalInput">The raw vertical input (e.g., from Input.GetAxis("Vertical")).</param>
    /// <param name="climbSpeed">The climbing speed.</param>
    /// <returns>The calculated target position for the climber.</returns>
    public Vector3 GetClimbTargetPosition(Transform climberTransform, float verticalInput, float climbSpeed)
    {
        // Calculate the desired vertical movement.
        Vector3 targetPosition = climberTransform.position + Vector3.up * verticalInput * climbSpeed * Time.fixedDeltaTime;

        // Clamp the vertical position within the ladder's collider bounds.
        float minClimbY = _ladderCollider.bounds.min.y;
        float maxClimbY = _ladderCollider.bounds.max.y;
        targetPosition.y = Mathf.Clamp(targetPosition.y, minClimbY, maxClimbY);

        // Snap the player's horizontal position (X and Z) to the closest point on the ladder's collider.
        // This keeps the player aligned with the ladder's surface.
        Vector3 ladderPlanePosition = _ladderCollider.ClosestPoint(climberTransform.position);
        targetPosition.x = ladderPlanePosition.x;
        targetPosition.z = ladderPlanePosition.z;

        // Apply the depth offset to push the player slightly away from the ladder's surface
        // to prevent clipping with the ladder's mesh.
        Vector3 worldForward = transform.TransformDirection(ladderForwardDirection.normalized);
        targetPosition += worldForward * climbDepthOffset;

        return targetPosition;
    }

    /// <summary>
    /// Gets the target rotation for the climber to face correctly while on the ladder.
    /// The player will face the ladder's defined forward direction.
    /// </summary>
    /// <returns>The target Quaternion rotation.</returns>
    public Quaternion GetClimbTargetRotation()
    {
        return Quaternion.LookRotation(transform.TransformDirection(ladderForwardDirection));
    }

    /// <summary>
    /// Gets the appropriate exit point transform based on the desired direction (Top or Bottom).
    /// </summary>
    /// <param name="direction">The desired exit direction (Top or Bottom).</param>
    /// <returns>The Transform of the specified exit point.</returns>
    public Transform GetExitPoint(LadderExitDirection direction)
    {
        return direction == LadderExitDirection.Top ? topExitPoint : bottomExitPoint;
    }

    /// <summary>
    /// Checks if the player is currently at the top extent of the ladder.
    /// Uses a small epsilon for floating point comparison robustness.
    /// </summary>
    /// <param name="playerY">The player's current Y position.</param>
    /// <returns>True if at top, false otherwise.</returns>
    public bool IsAtTop(float playerY)
    {
        return Mathf.Abs(playerY - _ladderCollider.bounds.max.y) < 0.1f;
    }

    /// <summary>
    /// Checks if the player is currently at the bottom extent of the ladder.
    /// Uses a small epsilon for floating point comparison robustness.
    /// </summary>
    /// <param name="playerY">The player's current Y position.</param>
    /// <returns>True if at bottom, false otherwise.</returns>
    public bool IsAtBottom(float playerY)
    {
        return Mathf.Abs(playerY - _ladderCollider.bounds.min.y) < 0.1f;
    }

    // Optional: Draw Gizmos for easier setup and visualization in the Unity editor.
    void OnDrawGizmos()
    {
        // Draw Entry Point
        if (entryPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(entryPoint.position, 0.2f);
            Gizmos.DrawLine(entryPoint.position, entryPoint.position + Vector3.up);
#if UNITY_EDITOR // Use preprocessor directives for editor-only code to avoid runtime overhead
            UnityEditor.Handles.Label(entryPoint.position + Vector3.up * 0.4f, "Entry");
#endif
        }
        // Draw Top Exit Point
        if (topExitPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(topExitPoint.position, 0.2f);
            Gizmos.DrawLine(topExitPoint.position, topExitPoint.position + Vector3.up);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(topExitPoint.position + Vector3.up * 0.4f, "Top Exit");
#endif
        }
        // Draw Bottom Exit Point (only if distinct from Entry Point)
        if (bottomExitPoint != null && bottomExitPoint != entryPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bottomExitPoint.position, 0.2f);
            Gizmos.DrawLine(bottomExitPoint.position, bottomExitPoint.position + Vector3.up);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(bottomExitPoint.position + Vector3.up * 0.4f, "Bottom Exit");
#endif
        }

        // Draw Ladder Forward Direction
        Gizmos.color = Color.yellow;
        Vector3 worldForward = transform.TransformDirection(ladderForwardDirection.normalized);
        Gizmos.DrawRay(transform.position, worldForward * 1.0f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + worldForward * 1.2f, "Ladder Forward");
#endif
    }
}
```

---

### `PlayerClimbingController.cs` Script

This script manages the player's climbing behavior. Attach it to your player GameObject.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .Where() and .ToArray()

/// <summary>
/// This script demonstrates the 'LadderClimbSystem' pattern in Unity.
/// It consists of two main components:
/// 1.  <see cref="Ladder"/>: Defines the climbable object, its bounds, and exit points.
/// 2.  <see cref="PlayerClimbingController"/>: Manages the player's interaction with ladders,
///     handling state transitions, climbing movement, and exiting.
///
/// **The LadderClimbSystem Pattern in action:**
/// -   **Encapsulation & Separation of Concerns:**
///     -   The `Ladder` script only cares about *being* a ladder (its geometry, exit points, orientation).
///     -   The `PlayerClimbingController` script only cares about *climbing* ladders (detection, state, movement logic).
///     -   This keeps each component focused on a single responsibility.
/// -   **State Management:** The player transitions into a distinct 'climbing' state,
///     where their normal movement (e.g., gravity, walking, jumping) is temporarily disabled
///     and replaced with ladder-specific physics and input handling.
/// -   **Interaction:** The `PlayerClimbingController` actively searches for and interacts with `Ladder` objects,
///     acting as the 'client' of the `Ladder` component.
/// -   **Extensibility:**
///     -   New ladder types could extend the `Ladder` class or implement an `ILadder` interface.
///     -   Different player behaviors could integrate with `PlayerClimbingController` by listening to climbing events.
///     -   The `PlayerClimbingController` is designed to be added alongside (and potentially control) a main `PlayerController` script.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player needs a Rigidbody for physics interactions and movement.
[RequireComponent(typeof(Collider))] // Player needs a Collider for detection.
public class PlayerClimbingController : MonoBehaviour
{
    [Header("Climbing Settings")]
    [Tooltip("The speed at which the player climbs up and down the ladder.")]
    public float climbSpeed = 2f;
    [Tooltip("The force applied when exiting the ladder, pushing the player slightly away.")]
    public float exitPushForce = 5f;
    [Tooltip("The radius around the player to detect nearby ladders for interaction.")]
    public float interactionDetectionRadius = 1f;
    [Tooltip("Layer mask to filter for Ladder objects during detection.")]
    public LayerMask ladderLayer;

    [Header("Input Settings")]
    [Tooltip("Input key to initiate climbing when near a ladder.")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Input key to exit climbing. Can be the same as interact key for toggle behavior or a separate key like Jump.")]
    public KeyCode exitClimbKey = KeyCode.Space; 

    // Internal state variables
    private Rigidbody _rigidbody;
    private Collider _playerCollider;
    private Ladder _currentLadder; // Reference to the ladder currently being climbed.
    private bool _isClimbing = false; // Flag to indicate if the player is in climbing state.

    // References to other player movement scripts that should be disabled/enabled.
    // This allows for flexible integration with different player controllers.
    // Assign these in the Inspector, or find them via GetComponent in Awake().
    [Tooltip("Reference to the player's main movement script (e.g., PlayerMovement, CharacterControllerMovement). This will be disabled during climbing.")]
    public MonoBehaviour playerMovementScript;
    [Tooltip("Reference to the player's main camera script (if it controls camera rotation based on player input). This will be disabled during climbing.")]
    public MonoBehaviour playerCameraScript;


    // Public property to check if the player is currently climbing.
    public bool IsClimbing => _isClimbing;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _playerCollider = GetComponent<Collider>();

        // Ensure Rigidbody is configured for physics-based movement.
        // We will toggle useGravity as needed. isKinematic should generally be false for physics movement.
        if (_rigidbody.isKinematic)
        {
            Debug.LogWarning("Player Rigidbody is kinematic by default. Ensure it's not always kinematic for normal movement. Setting to non-kinematic for interaction.", this);
            _rigidbody.isKinematic = false;
        }
    }

    void Update()
    {
        if (!_isClimbing)
        {
            // --- Detection & Initiation Phase (when not climbing) ---
            // Continuously detect nearby ladders.
            Ladder detectedLadder = DetectLadder();

            if (detectedLadder != null)
            {
                // Optional: Display a UI prompt like "Press E to Climb"
                Debug.Log($"<color=cyan>[{gameObject.name}] Press {interactKey} to climb: {detectedLadder.name}</color>");

                if (Input.GetKeyDown(interactKey))
                {
                    StartClimbing(detectedLadder);
                }
            }
        }
        else // _isClimbing is true, player is currently on a ladder
        {
            // --- Exiting Phase (when climbing) ---
            float verticalInput = Input.GetAxis("Vertical");

            // Check if player wants to exit via input or by reaching the ladder's end.
            bool attemptingExit = Input.GetKeyDown(exitClimbKey);
            bool reachedTop = _currentLadder.IsAtTop(transform.position.y) && verticalInput > 0;
            bool reachedBottom = _currentLadder.IsAtBottom(transform.position.y) && verticalInput < 0;

            if (attemptingExit || reachedTop || reachedBottom)
            {
                Ladder.LadderExitDirection exitDir;

                // Determine exit direction:
                if (reachedTop)
                {
                    exitDir = Ladder.LadderExitDirection.Top;
                }
                else if (reachedBottom)
                {
                    exitDir = Ladder.LadderExitDirection.Bottom;
                }
                else if (attemptingExit) // If only interact key pressed, decide based on current position
                {
                    // If interact key is pressed mid-ladder, exit towards the closer end.
                    float midY = (_currentLadder.GetExitPoint(Ladder.LadderExitDirection.Top).position.y + _currentLadder.GetExitPoint(Ladder.LadderExitDirection.Bottom).position.y) / 2f;
                    exitDir = transform.position.y > midY ? Ladder.LadderExitDirection.Top : Ladder.LadderExitDirection.Bottom;
                }
                else
                {
                    // This case should ideally not be reached if conditions are exhaustive.
                    // Default to bottom exit if logic falls through.
                    exitDir = Ladder.LadderExitDirection.Bottom; 
                }
                
                StopClimbing(exitDir);
            }
        }
    }

    void FixedUpdate()
    {
        if (_isClimbing && _currentLadder != null)
        {
            // --- Climbing Movement Phase (physics updates) ---
            float verticalInput = Input.GetAxis("Vertical");

            // Get the calculated target position and rotation from the Ladder component.
            Vector3 targetPosition = _currentLadder.GetClimbTargetPosition(transform, verticalInput, climbSpeed);
            Quaternion targetRotation = _currentLadder.GetClimbTargetRotation();

            // Move the Rigidbody to the target position. Using MovePosition for physics-based movement.
            _rigidbody.MovePosition(targetPosition);
            // Rotate the Rigidbody to align with the ladder's forward direction.
            _rigidbody.MoveRotation(targetRotation);
        }
    }

    /// <summary>
    /// Initiates the climbing state for the player.
    /// This involves disabling other movement, adjusting physics, and snapping to the ladder.
    /// </summary>
    /// <param name="ladderToClimb">The Ladder component the player is interacting with.</param>
    private void StartClimbing(Ladder ladderToClimb)
    {
        _isClimbing = true;
        _currentLadder = ladderToClimb;

        // --- State Transition Logic ---
        // 1. Disable normal player movement and camera controls to prevent conflicts.
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerCameraScript != null) playerCameraScript.enabled = false;

        // 2. Adjust Rigidbody properties for climbing.
        _rigidbody.useGravity = false; // Player is no longer affected by gravity.
        _rigidbody.velocity = Vector3.zero; // Stop any existing momentum.
        _rigidbody.angularVelocity = Vector3.zero; // Stop any existing rotation.
        _rigidbody.isKinematic = false; // Ensure it's not kinematic so MovePosition works.

        // 3. Snap player to the ladder's initial position and orientation.
        // We get the target position with 0 vertical input to snap to the current Y on the ladder, clamped.
        transform.position = _currentLadder.GetClimbTargetPosition(transform, 0, 0); 
        transform.rotation = _currentLadder.GetClimbTargetRotation();

        Debug.Log($"<color=green>[{gameObject.name}] Started climbing: {_currentLadder.name}</color>");
    }

    /// <summary>
    /// Terminates the climbing state, returning the player to normal movement.
    /// This re-enables other movement, restores physics, and applies an exit force.
    /// </summary>
    /// <param name="exitDirection">The direction from which the player is exiting the ladder (Top or Bottom).</param>
    private void StopClimbing(Ladder.LadderExitDirection exitDirection)
    {
        _isClimbing = false;

        // --- State Transition Logic ---
        // 1. Re-enable normal player movement and camera controls.
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerCameraScript != null) playerCameraScript.enabled = true;

        // 2. Restore Rigidbody properties.
        _rigidbody.useGravity = true; // Gravity is re-enabled.
        _rigidbody.isKinematic = false; // Ensure it's not kinematic for gravity and normal movement.

        // 3. Teleport player to the designated exit point.
        Transform exitPoint = _currentLadder.GetExitPoint(exitDirection);
        transform.position = exitPoint.position;

        // 4. Apply a push force to move the player off the ladder smoothly.
        Vector3 ladderForward = _currentLadder.GetClimbTargetRotation() * Vector3.forward;
        Vector3 pushForceVector = Vector3.zero;

        if (exitDirection == Ladder.LadderExitDirection.Top)
        {
            // Push player forward (relative to ladder) and slightly up when exiting top.
            pushForceVector = (ladderForward + Vector3.up).normalized * exitPushForce;
        }
        else // Ladder.LadderExitDirection.Bottom
        {
            // Push player backward (away from the ladder's forward) when exiting bottom.
            pushForceVector = (-ladderForward).normalized * exitPushForce;
        }
        _rigidbody.AddForce(pushForceVector, ForceMode.VelocityChange);
        
        _currentLadder = null; // Clear the ladder reference.
        Debug.Log($"<color=red>[{gameObject.name}] Stopped climbing. Exited {exitDirection}</color>");
    }

    /// <summary>
    /// Detects a nearby Ladder component using an OverlapSphere.
    /// This method is designed to find the closest valid ladder.
    /// </summary>
    /// <returns>The Ladder component if found within detection radius, otherwise null.</returns>
    private Ladder DetectLadder()
    {
        // Use OverlapSphere to find all colliders in the detection radius, filtered by `ladderLayer`.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionDetectionRadius, ladderLayer);

        // Filter out the player's own collider if it's mistakenly in the results (e.g., player's collider is on ladder layer).
        // This makes the system more robust against incorrect layer setups.
        hitColliders = hitColliders.Where(c => c.transform.root != transform.root).ToArray();

        Ladder closestLadder = null;
        float minDistance = float.MaxValue;

        // Iterate through detected colliders to find the closest 'Ladder' component.
        foreach (Collider hitCollider in hitColliders)
        {
            // Get the Ladder component from the hit collider's GameObject or its parents.
            // This handles cases where the actual Ladder component is on a parent GameObject.
            Ladder ladder = hitCollider.GetComponentInParent<Ladder>(); 
            if (ladder != null)
            {
                // Calculate distance to the ladder's entry point for proximity sorting.
                float distance = Vector3.Distance(transform.position, ladder.entryPoint.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLadder = ladder;
                }
            }
        }
        return closestLadder;
    }

    // Optional: Draw Gizmos for easier visualization of interaction radius in the Unity editor.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDetectionRadius);
    }
}
```

---

### Example Usage in Unity Project

Follow these steps to implement the LadderClimbSystem in your Unity scene:

**1. Create your Ladder GameObject:**

*   Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty). Name it **"Ladder_01"**.
*   **Add a `Box Collider` component** to "Ladder_01".
    *   Adjust its `Size` and `Center` to match your visual ladder model's climbable area. This collider defines the vertical limits for climbing.
    *   **Crucially:** Ensure **`Is Trigger` is `true`** on this Box Collider.
*   **Add the `Ladder.cs` script** to "Ladder_01".
*   **Create empty child GameObjects for the entry and exit points:**
    *   Right-click "Ladder_01" -> Create Empty. Name it **"Entry Point"**. Position it at the bottom-center of the ladder, slightly *in front* of the ladder's visual mesh. This is where the player will snap to initially.
    *   Right-click "Ladder_01" -> Create Empty. Name it **"Top Exit Point"**. Position it at the top-center of the ladder, slightly *above and in front* of the top rung.
    *   (Optional) Right-click "Ladder_01" -> Create Empty. Name it **"Bottom Exit Point"**. Position it at the bottom-center of the ladder, slightly *below and behind* the bottom rung. This gives a distinct exit location.
*   **Drag these child Transforms** from the Hierarchy into the `Ladder` script's `Entry Point`, `Top Exit Point`, and `Bottom Exit Point` fields in the Inspector.
*   **Adjust `Ladder Forward Direction`** if your ladder isn't facing along its own Z-axis. This vector defines which way the player will face while climbing.
*   **Important:** Create a new Layer named **"Ladder"** (Go to Layers dropdown -> Add Layer...). Assign "Ladder_01" (and its children, if any visual mesh is separate) to this **"Ladder"** layer.

**2. Set up your Player GameObject:**

*   Ensure your Player GameObject has:
    *   A **`Rigidbody`** component (required by `PlayerClimbingController`).
    *   A **`Collider`** component (e.g., `Capsule Collider`) (required by `PlayerClimbingController`).
    *   **Important:** Assign your Player to a distinct layer (e.g., **"Player"**) to prevent self-intersection in `OverlapSphere` detection.
*   **Add the `PlayerClimbingController.cs` script** to your Player GameObject.
*   In the `PlayerClimbingController` Inspector:
    *   Set **`Ladder Layer`** to the **"Ladder"** layer you created earlier.
    *   Adjust `Climb Speed`, `Exit Push Force`, `Interaction Detection Radius` as needed for your game.
    *   Set `Interact Key` (e.g., 'E') and `Exit Climb Key` (e.g., 'Space' or 'E' again for toggle).
    *   **Crucially:** Drag your player's main movement script (e.g., a script named 'PlayerMovement.cs', 'ThirdPersonController.cs', etc.) into the **`Player Movement Script`** field. This script will be disabled when climbing.
    *   (Optional) If your camera is controlled by player input (e.g., a 'PlayerLook.cs' or 'CameraController.cs' script), drag that script into the **`Player Camera Script`** field. This ensures climbing disables regular camera rotation.

**3. Test:**

*   Run the scene.
*   Move your player near the ladder. You should see a debug message in the console (and potentially a UI prompt if you add one) about pressing the interact key.
*   Press the interact key. The player should snap to the ladder and enter the climbing state.
*   Use the vertical input (W/S or Up/Down arrows) to climb up and down.
*   Press the exit climb key (e.g., Space) or move to the very top/bottom of the ladder and continue applying vertical input to exit.

This complete setup provides a robust and extensible climbing system, demonstrating a practical application of structuring a game mechanic as a collaborative system (the "LadderClimbSystem pattern").