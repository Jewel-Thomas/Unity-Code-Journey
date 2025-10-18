// Unity Design Pattern Example: RopeClimbingSystem
// This script demonstrates the RopeClimbingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a "Rope Climbing System" design pattern in Unity, focusing on separating concerns between the climbable object (the rope) and the climbing character.

**Rope Climbing System Pattern Core Idea:**

The pattern involves two main components that interact:

1.  **`ClimbableRope` (The "Rope" component):**
    *   Defines an object in the scene as a climbable surface.
    *   Holds properties specific to *this* rope (e.g., its climbable limits, speed multiplier).
    *   Acts as a data provider and marker for climbable areas.

2.  **`CharacterRopeClimber` (The "Climber" component):**
    *   Attached to the player character.
    *   Handles detection of `ClimbableRope` objects.
    *   Manages the character's *state* (climbing or not climbing).
    *   Controls character movement, input, and physics behavior *while climbing*.
    *   Interacts with other character components (e.g., `CharacterController`, other movement scripts).

This separation allows you to easily:
*   Add new types of climbable objects (ladders, vines) by creating similar `Climbable` components.
*   Change climbing mechanics without modifying the rope objects.
*   Have multiple characters with different climbing behaviors.

---

## 1. `ClimbableRope.cs` Script

This script identifies a GameObject as a climbable rope and defines its properties like climbable height limits and a specific climb speed multiplier.

```csharp
using UnityEngine;
using System.Collections; // Not strictly required for this specific script, but often useful.

/// <summary>
/// Represents a climbable rope in the game world.
/// This component marks a GameObject as a rope that a character can interact with and climb.
/// It defines the physical attributes and limits of the climbable area.
/// </summary>
[RequireComponent(typeof(Collider))] // A collider is essential for detecting interaction with the rope.
public class ClimbableRope : MonoBehaviour
{
    [Header("Rope Settings")]
    [Tooltip("Defines the top-most Y position (world Y) a character can climb on this rope.")]
    public float topLimit;
    [Tooltip("Defines the bottom-most Y position (world Y) a character can climb on this rope.")]
    public float bottomLimit;
    [Tooltip("A multiplier for the base climbing speed when a character is on this specific rope.")]
    [Range(0.1f, 5.0f)]
    public float climbSpeedMultiplier = 1.0f;

    // The collider associated with this rope. It must be a trigger for detection.
    private Collider _ropeCollider;

    void Awake()
    {
        _ropeCollider = GetComponent<Collider>();
        if (_ropeCollider == null)
        {
            Debug.LogError("ClimbableRope requires a Collider component!", this);
            enabled = false; // Disable script if no collider is found.
            return;
        }

        // Ensure the collider is a trigger so players can pass through it and detect entry/exit events.
        _ropeCollider.isTrigger = true;

        // --- Auto-detect limits if not explicitly set in the editor ---
        // This is a convenience feature, assuming a generally vertical rope.
        // For complex rope shapes (e.g., splines), you would need more advanced limit calculation.
        if (topLimit == 0 && bottomLimit == 0 && _ropeCollider is BoxCollider)
        {
            // Use the collider's bounds to define the limits.
            // This calculation adjusts for the rope's world position.
            topLimit = transform.position.y + _ropeCollider.bounds.extents.y;
            bottomLimit = transform.position.y - _ropeCollider.bounds.extents.y;
            
            // Add a small buffer to prevent player from clipping through ends
            topLimit -= 0.1f; 
            bottomLimit += 0.1f;
        }

        // Ensure limits are ordered correctly (bottom should be less than top).
        if (bottomLimit > topLimit)
        {
            float temp = topLimit;
            topLimit = bottomLimit;
            bottomLimit = temp;
        }

        Debug.Log($"Rope '{gameObject.name}' initialized. Top: {topLimit}, Bottom: {bottomLimit}");
    }

    /// <summary>
    /// Returns the horizontal (X, Z) center position of the rope in world coordinates.
    /// This is used by the climber to snap its XZ position to the rope.
    /// </summary>
    /// <returns>A Vector3 representing the XZ center of the rope.</returns>
    public Vector3 GetRopeXZCenter()
    {
        // For a simple vertical rope, the transform's X and Z are usually sufficient.
        // For more complex colliders or rotations, _ropeCollider.bounds.center might be more accurate.
        return new Vector3(transform.position.x, 0, transform.position.z);
    }

    /// <summary>
    /// Clamps a given Y position within the rope's defined vertical climbing limits.
    /// Ensures the character stays within the climbable range of the rope.
    /// </summary>
    /// <param name="yPosition">The current or desired Y position of the character.</param>
    /// <returns>The clamped Y position.</returns>
    public float ClampClimbYPosition(float yPosition)
    {
        return Mathf.Clamp(yPosition, bottomLimit, topLimit);
    }

    // Visualizes the climbable limits in the Unity editor.
    void OnDrawGizmos()
    {
        if (_ropeCollider == null) _ropeCollider = GetComponent<Collider>();
        if (_ropeCollider == null) return;

        // Draw green spheres at the top and bottom limits and a line connecting them.
        Gizmos.color = Color.green;
        Vector3 topPos = new Vector3(transform.position.x, topLimit, transform.position.z);
        Vector3 bottomPos = new Vector3(transform.position.x, bottomLimit, transform.position.z);
        Gizmos.DrawWireSphere(topPos, 0.2f);
        Gizmos.DrawLine(topPos, bottomPos);
        Gizmos.DrawWireSphere(bottomPos, 0.2f);

        // Draw the rope collider bounds in cyan.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_ropeCollider.bounds.center, _ropeCollider.bounds.size);
    }
}
```

---

## 2. `CharacterRopeClimber.cs` Script

This script is attached to your player character. It handles detecting `ClimbableRope` components, entering and exiting climbing mode, and processing input for movement while climbing.

```csharp
using UnityEngine;
using System.Collections; // Not strictly required for this specific script, but often useful.

/// <summary>
/// The CharacterRopeClimber component enables a character to detect and climb objects
/// marked with the ClimbableRope component. It manages the climbing state, input,
/// and character movement while attached to a rope.
/// </summary>
[RequireComponent(typeof(CharacterController))] // Essential for player movement and collision handling.
public class CharacterRopeClimber : MonoBehaviour
{
    [Header("Climber Settings")]
    [Tooltip("The base speed at which the character climbs up and down the rope.")]
    public float baseClimbSpeed = 3.0f;
    [Tooltip("The vertical force applied when jumping off a rope.")]
    public float climbJumpForce = 7.0f;
    [Tooltip("The horizontal force applied when jumping off a rope, pushing away from the rope's center.")]
    public float climbJumpHorizontalForce = 3.0f;
    [Tooltip("The distance the character moves away from the rope when simply exiting climb mode (e.g., pressing interact key again).")]
    public float climbExitPushOffDistance = 0.5f;
    [Tooltip("The input key to interact with a rope (e.g., press 'E' to grab/release).")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("The input key to jump off the rope (e.g., press 'Space').")]
    public KeyCode jumpKey = KeyCode.Space;

    // --- Component References ---
    private CharacterController _characterController;
    // If your character has a separate script for ground movement, you'll need to disable/enable it.
    // Uncomment the line below and assign it in Awake or the Inspector.
    // private PlayerMovement _playerMovement; 
    private Rigidbody _rigidbody; // Used if the player character has a Rigidbody component.

    // --- Climbing State Variables ---
    private bool _isClimbing = false; // Is the character currently in climbing mode?
    private ClimbableRope _currentRope = null; // The specific rope the character is currently climbing.
    private ClimbableRope _potentialRope = null; // A rope the character is currently overlapping but not yet climbing.

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            Debug.LogError("CharacterRopeClimber requires a CharacterController component!", this);
            enabled = false;
            return;
        }

        // --- Optional: Get other character components ---
        // If you have a custom PlayerMovement script, uncomment and get its reference.
        // Example: _playerMovement = GetComponent<PlayerMovement>();

        _rigidbody = GetComponent<Rigidbody>();
        // If using a Rigidbody, it's generally best to make the CharacterController handle movement
        // and disable the Rigidbody's physics simulation when climbing.
    }

    void Update()
    {
        // --- Rope Detection and Interaction Input ---
        // If the character is overlapping a potential rope AND presses the interact key.
        if (_potentialRope != null && Input.GetKeyDown(interactKey))
        {
            if (!_isClimbing)
            {
                // If not currently climbing, enter climbing mode on the potential rope.
                EnterClimbingMode(_potentialRope);
            }
            else if (_currentRope == _potentialRope) // If already climbing this specific rope.
            {
                // Pressing interact again can also exit climbing mode.
                ExitClimbingMode(Vector3.zero); // Exit without applying a jump force.
            }
        }

        // --- Climbing Movement and Actions (only when actually climbing) ---
        if (_isClimbing)
        {
            HandleClimbingInput();
        }
    }

    /// <summary>
    /// Handles player input (vertical movement, jumping) and applies movement
    /// specifically for climbing. This is called only when _isClimbing is true.
    /// </summary>
    private void HandleClimbingInput()
    {
        // Get vertical input (e.g., W/S or Up/Down arrows).
        float verticalInput = Input.GetAxis("Vertical");
        // Calculate the actual climb speed, factoring in the rope's multiplier.
        float currentClimbSpeed = baseClimbSpeed * _currentRope.climbSpeedMultiplier;

        // Calculate the desired vertical movement based on input and speed.
        Vector3 desiredVerticalMove = Vector3.up * verticalInput * currentClimbSpeed * Time.deltaTime;

        // Get the character's current position.
        Vector3 currentPosition = transform.position;
        // Calculate the desired Y position after applying movement.
        float desiredY = currentPosition.y + desiredVerticalMove.y;

        // Clamp the desired Y position within the current rope's defined limits.
        float clampedY = _currentRope.ClampClimbYPosition(desiredY);

        // Calculate the actual Y movement that will occur after clamping.
        float actualYMovement = clampedY - currentPosition.y;

        // Get the horizontal (X, Z) center of the rope to snap the character to.
        Vector3 ropeCenterXZ = _currentRope.GetRopeXZCenter();
        
        // Construct the snapped position, using the rope's XZ and the clamped Y.
        Vector3 snappedPosition = new Vector3(ropeCenterXZ.x, clampedY, ropeCenterXZ.z);

        // Calculate the final movement vector required to reach the snapped position.
        // We use the 'actualYMovement' for the Y component to respect clamping.
        Vector3 finalMovement = snappedPosition - currentPosition;
        finalMovement.y = actualYMovement; 

        // Apply the movement using the CharacterController.
        // This ensures collision detection even when climbing.
        _characterController.Move(finalMovement);


        // --- Check for jump input to exit climbing mode with a jump ---
        if (Input.GetKeyDown(jumpKey))
        {
            // Calculate a horizontal push direction away from the rope's center.
            // This makes the jump feel more natural, pushing the player away from the surface.
            Vector3 pushDirection = (transform.position - _currentRope.GetRopeXZCenter()).normalized;
            pushDirection.y = 0; // Only horizontal push.
            if (pushDirection == Vector3.zero) pushDirection = transform.forward; // Fallback if player is exactly at rope center.

            // Combine vertical jump force and horizontal push.
            Vector3 jumpVelocity = (Vector3.up * climbJumpForce) + (pushDirection * climbJumpHorizontalForce);
            ExitClimbingMode(jumpVelocity); // Exit climbing, applying the jump velocity.
        }
    }

    /// <summary>
    /// Initiates climbing mode, attaching the character to the specified rope.
    /// This method disables normal character movement and gravity, and snaps the character to the rope.
    /// </summary>
    /// <param name="rope">The ClimbableRope component to attach to.</param>
    private void EnterClimbingMode(ClimbableRope rope)
    {
        Debug.Log($"Entering climbing mode on '{rope.name}'.");
        _isClimbing = true;
        _currentRope = rope;

        // --- Disable regular player movement (if applicable) ---
        // If you have a custom PlayerMovement script (e.g., for ground movement, jumping, gravity),
        // disable it here so this script can take full control.
        // Example: if (_playerMovement != null) _playerMovement.enabled = false;
        
        // --- Disable Rigidbody physics if present ---
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true; // Make Rigidbody non-physical to prevent gravity/collisions from interfering.
            _rigidbody.velocity = Vector3.zero; // Stop any current velocity.
            _rigidbody.angularVelocity = Vector3.zero; // Stop any current rotation.
        }

        // --- Snap character to the rope's position ---
        // Snap player's XZ position to the rope's horizontal center.
        Vector3 ropeCenterXZ = _currentRope.GetRopeXZCenter();
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(ropeCenterXZ.x, currentPos.y, ropeCenterXZ.z);

        // Clamp the initial Y position to ensure the character starts within the rope's limits.
        float clampedY = _currentRope.ClampClimbYPosition(transform.position.y);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    /// <summary>
    /// Exits climbing mode, restoring normal player movement and physics.
    /// An optional exit velocity can be applied (e.g., for a jump).
    /// </summary>
    /// <param name="exitVelocity">The velocity to apply to the character upon exiting (e.g., a jump force).</param>
    private void ExitClimbingMode(Vector3 exitVelocity)
    {
        Debug.Log("Exiting climbing mode.");
        _isClimbing = false;
        _currentRope = null;

        // --- Re-enable regular player movement (if applicable) ---
        // Example: if (_playerMovement != null) _playerMovement.enabled = true;

        // --- Restore Rigidbody physics and apply exit velocity ---
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false; // Re-enable physics simulation for the Rigidbody.
            _rigidbody.velocity = exitVelocity; // Apply any exit velocity (e.g., jump force).
            
            // If no specific jump velocity was given, apply a small push away from the rope.
            if (exitVelocity == Vector3.zero && _potentialRope != null) 
            {
                Vector3 pushDirection = (transform.position - _potentialRope.GetRopeXZCenter()).normalized;
                pushDirection.y = 0; // Only horizontal push.
                if (pushDirection == Vector3.zero) pushDirection = transform.forward; // Fallback if player is exactly at rope center.
                _rigidbody.velocity += pushDirection * (climbExitPushOffDistance * 10f); // Apply a scaled push force.
            }
        }
        else // If using CharacterController without Rigidbody, manually apply 'push' for exiting.
        {
            // Apply a small push away from the rope to prevent immediate re-entry or getting stuck.
            // This is done via CharacterController.Move, which also handles collisions.
            if (_potentialRope != null)
            {
                Vector3 pushDirection = (transform.position - _potentialRope.GetRopeXZCenter()).normalized;
                pushDirection.y = 0;
                if (pushDirection == Vector3.zero) pushDirection = transform.forward; // Fallback
                // Apply the push and the exitVelocity (scaled by Time.deltaTime for CharacterController.Move).
                _characterController.Move((pushDirection * climbExitPushOffDistance) + (exitVelocity * Time.deltaTime)); 
            }
            else // If no potential rope (e.g. forced exit), just apply jump velocity
            {
                _characterController.Move(exitVelocity * Time.deltaTime);
            }
        }

        // The _potentialRope might still be set if the player exited while still overlapping.
        // This is fine, as _isClimbing prevents re-entry until interactKey is pressed again.
    }

    // --- Trigger Detection for potential ropes ---
    // These methods detect when the character's collider enters or exits a trigger collider.
    // The ClimbableRope component's collider must be set to 'Is Trigger' for these to work.

    void OnTriggerEnter(Collider other)
    {
        // Attempt to get a ClimbableRope component from the object whose trigger was entered.
        ClimbableRope rope = other.GetComponent<ClimbableRope>();
        if (rope != null)
        {
            _potentialRope = rope; // Store it as a potential rope to climb.
            Debug.Log($"Potential rope '{rope.name}' detected. Press '{interactKey}' to climb.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the collider being exited belongs to the potential or current rope.
        ClimbableRope rope = other.GetComponent<ClimbableRope>();
        if (rope != null && _potentialRope == rope)
        {
            _potentialRope = null; // No longer overlapping this rope.
            Debug.Log($"Exited potential rope area: '{rope.name}'.");

            // If the character was actively climbing this rope and has left its trigger area,
            // they should automatically exit climbing mode.
            if (_isClimbing && _currentRope == rope)
            {
                ExitClimbingMode(Vector3.zero); // Exit without a jump force.
            }
        }
    }

    /// <summary>
    /// Public getter to check if the character is currently climbing.
    /// </summary>
    public bool IsClimbing => _isClimbing;
}
```

---

## Example Usage in Unity Project:

To use this Rope Climbing System in your Unity project, follow these steps:

### 1. Setup Your Player Character:

*   **Create a Player GameObject:** (e.g., a Capsule or a custom character model).
*   **Add a `CharacterController` component:** This is essential for the `CharacterRopeClimber` script. Configure its `Radius`, `Height`, and `Center` to fit your character.
*   **Add the `CharacterRopeClimber.cs` script** to your Player GameObject.
*   **Configure `CharacterRopeClimber` properties:**
    *   Adjust `Base Climb Speed`, `Climb Jump Force`, `Climb Jump Horizontal Force`, and `Climb Exit Push Off Distance` to your liking.
    *   Set `Interact Key` (e.g., `E`) and `Jump Key` (e.g., `Space`).
*   **Player Movement Script (Optional but Recommended):**
    *   If you have a separate script for your player's ground movement (e.g., `PlayerMovement.cs` that uses `CharacterController.Move()`), you'll need to **disable this script** when `CharacterRopeClimber` takes over and **re-enable it** when climbing stops.
    *   To do this, in `CharacterRopeClimber.cs`, uncomment the `_playerMovement` field and the lines related to `_playerMovement.enabled = false/true;`. You might also need to drag your `PlayerMovement` script into a public field in `CharacterRopeClimber` if it's not on the same GameObject or if you prefer manual assignment.
    *   *Alternatively, if your ground movement script is simple or you don't have one, the `CharacterRopeClimber` will directly control the `CharacterController` while climbing, effectively overriding any other movement attempts.*
*   **Rigidbody (Optional):** If your player has a `Rigidbody` component, the `CharacterRopeClimber` will automatically set `isKinematic` to `true` while climbing and restore it upon exiting, preventing physics conflicts. Ensure your `Rigidbody` is configured correctly (e.g., freeze rotations if it's a 3D character).

### 2. Setup Your Rope GameObject(s):

*   **Create a Rope GameObject:** (e.g., a simple Cylinder, a Cube, or a custom rope model).
*   **Add a `Collider` component:** This is crucial for detection. A `BoxCollider` or `CapsuleCollider` is common. Make sure its dimensions roughly match your rope visual.
*   **Mark the Collider as a Trigger:** In the Inspector, check the `Is Trigger` checkbox on the collider component.
*   **Add the `ClimbableRope.cs` script** to your Rope GameObject.
*   **Configure `ClimbableRope` properties:**
    *   **`Top Limit` and `Bottom Limit`:** Set these to define the exact world Y-coordinates where the player can stop climbing up or down. If left at `0`, the script will attempt to auto-calculate them based on the collider bounds (useful for vertical ropes).
    *   **`Climb Speed Multiplier`:** Adjust if this specific rope should be climbed faster or slower than the character's base climb speed.
*   **Visual Representation:** Add a `Mesh Renderer` and a `Material` to your rope so you can see it in the scene.

### 3. Test and Adjust:

1.  Place your Player GameObject near a Rope GameObject.
2.  Run the scene.
3.  Move your character into the `ClimbableRope`'s trigger area.
4.  Press the `Interact Key` (default `E`). Your character should snap to the rope and enter climbing mode.
5.  Use `Vertical` input (W/S or Up/Down arrows) to climb up and down.
6.  Press the `Jump Key` (default `Space`) to jump off the rope.
7.  Press the `Interact Key` again to simply let go of the rope.

Adjust the speed, force, and limit values in the Inspectors of both `CharacterRopeClimber` and `ClimbableRope` to fine-tune the climbing experience.