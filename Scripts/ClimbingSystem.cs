// Unity Design Pattern Example: ClimbingSystem
// This script demonstrates the ClimbingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'ClimbingSystem' pattern in Unity, which encapsulates all logic related to detecting climbable surfaces, initiating/terminating climbing, handling movement while climbing, and integrating with Unity's physics and animation systems.

The core idea is to create a self-contained module that manages the character's climbing behavior, separating it from general movement or other character abilities. This makes the system modular, easier to maintain, and extensible.

```csharp
using UnityEngine;
using System.Collections; // Included for completeness, though not strictly used in this iteration

/// <summary>
/// The ClimbingSystem design pattern provides a robust and modular way to implement
/// character climbing mechanics in Unity. It encapsulates all logic related to:
/// 1.  **Detection:** Identifying climbable surfaces in the environment.
/// 2.  **Activation/Deactivation:** Managing the transition into and out of the climbing state.
/// 3.  **Movement:** Handling character movement while attached to a wall.
/// 4.  **Physics Interaction:** Modifying physics behavior (e.g., disabling gravity, making Rigidbody kinematic)
///     during climbing to ensure stable movement.
/// 5.  **Orientation:** Keeping the character correctly oriented and "stuck" to the climbing surface.
/// 6.  **Animation Integration:** Triggering appropriate climbing animations based on state and input.
///
/// This pattern separates climbing logic from general character movement,
/// making the system easier to understand, extend, and debug.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))] // Assuming a standard character setup
public class ClimbingSystem : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Reference to the character's Rigidbody. Will be set to kinematic during climbing.")]
    [SerializeField] private Rigidbody _rigidbody;
    [Tooltip("Reference to the character's Animator component (optional, but recommended for visuals).")]
    [SerializeField] private Animator _animator;

    [Header("Climbing Settings")]
    [Tooltip("Speed at which the character moves while climbing.")]
    [SerializeField] private float _climbSpeed = 3f;
    [Tooltip("Max distance to raycast forward to detect climbable walls when not climbing.")]
    [SerializeField] private float _wallDetectionDistance = 1f;
    [Tooltip("Distance from the wall the character will maintain while climbing. This is its 'perched' distance.")]
    [SerializeField] private float _wallStickOffset = 0.5f;
    [Tooltip("Speed at which the character rotates to align with the wall normal.")]
    [SerializeField] private float _wallRotationSpeed = 10f;
    [Tooltip("Force applied when the character jumps off the wall or stops climbing.")]
    [SerializeField] private float _jumpOffWallForce = 10f;
    [Tooltip("LayerMask containing all layers that are considered climbable.")]
    [SerializeField] private LayerMask _climbableLayer;

    [Header("Input Settings")]
    [Tooltip("Key to press to initiate climbing or stop climbing.")]
    [SerializeField] private KeyCode _climbToggleKey = KeyCode.E;
    [Tooltip("Key to press to jump off the wall while climbing.")]
    [SerializeField] private KeyCode _jumpOffWallKey = KeyCode.Space;

    [Header("Debug")]
    [Tooltip("Current climbing state of the character.")]
    [SerializeField] private bool _isClimbing = false;
    // Public getter for external systems to query climbing state
    public bool IsClimbing => _isClimbing;

    // Internal state variables for climbing
    private Vector3 _currentClimbNormal;      // The normal vector of the surface currently being climbed
    private Vector3 _climbSurfaceInitialHit;  // The point on the surface initially hit to start climbing
    private bool _canGrabWall = false;        // True if a climbable wall is currently detected in front

    // Cached component references
    private CapsuleCollider _capsuleCollider;

    // Animator parameter hashes for performance (avoiding string comparisons every frame)
    private readonly int IsClimbingAnimHash = Animator.StringToHash("IsClimbing");
    private readonly int ClimbXAnimHash = Animator.StringToHash("ClimbX");
    private readonly int ClimbYAnimHash = Animator.StringToHash("ClimbY");

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used to get component references and initialize internal state.
    /// </summary>
    void Awake()
    {
        // Get Rigidbody reference if not already assigned
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError("ClimbingSystem requires a Rigidbody component on the GameObject.", this);
                enabled = false; // Disable script if essential component is missing
                return;
            }
        }
        // It's good practice to freeze rotation for character rigidbodies to prevent physics-induced tumbling.
        _rigidbody.freezeRotation = true;

        // Get CapsuleCollider reference
        _capsuleCollider = GetComponent<CapsuleCollider>();
        if (_capsuleCollider == null)
        {
            Debug.LogError("ClimbingSystem requires a CapsuleCollider component on the GameObject.", this);
            enabled = false;
            return;
        }

        // Animator is optional, but if not found, climbing animations won't play.
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning("ClimbingSystem did not find an Animator component. Climbing animations will not play.", this);
            }
        }
    }

    /// <summary>
    /// Update is called once per frame. Handles input and wall detection when not climbing.
    /// Handles climbing movement and continuous wall realignment when climbing.
    /// </summary>
    void Update()
    {
        // 1. Wall Detection (only when not climbing)
        // This raycast checks if there's a climbable wall directly in front of the character.
        if (!_isClimbing)
        {
            DetectClimbableWall();
        }

        // 2. Input Handling for starting/stopping climb
        HandleInput();

        // 3. Climbing Movement and Re-alignment (only when climbing)
        // This logic keeps the character moving on the wall and continuously attached.
        if (_isClimbing)
        {
            HandleClimbingMovement();
            CheckAndRealignToWall(); // Ensure character stays on wall and updates its orientation
        }
    }

    /// <summary>
    /// Detects if a climbable wall is in front of the character using a raycast.
    /// Updates `_canGrabWall` and stores wall hit info if a wall is found.
    /// This is typically called when the character is NOT climbing.
    /// </summary>
    private void DetectClimbableWall()
    {
        // Raycast origin is centered on the character's height to check for walls around the middle.
        Vector3 rayOrigin = transform.position + Vector3.up * _capsuleCollider.height * 0.5f;
        RaycastHit hit;

        // Perform the raycast forward. Only hits objects on the `_climbableLayer`.
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, _wallDetectionDistance, _climbableLayer))
        {
            _canGrabWall = true;
            _climbSurfaceInitialHit = hit.point;
            _currentClimbNormal = hit.normal;
            // Debug.DrawRay(rayOrigin, transform.forward * _wallDetectionDistance, Color.green); // Debug visualization
        }
        else
        {
            _canGrabWall = false;
            // Debug.DrawRay(rayOrigin, transform.forward * _wallDetectionDistance, Color.red); // Debug visualization
        }
    }

    /// <summary>
    /// Handles player input for climbing actions (toggle climb, jump off wall).
    /// </summary>
    private void HandleInput()
    {
        // Toggle climb state based on a key press
        if (Input.GetKeyDown(_climbToggleKey))
        {
            if (_isClimbing)
            {
                StopClimbing(); // If climbing, stop
            }
            else if (_canGrabWall) // If not climbing and a wall is detected, start
            {
                StartClimbing();
            }
        }

        // Jump off wall while climbing
        if (_isClimbing && Input.GetKeyDown(_jumpOffWallKey))
        {
            StopClimbing(true); // Stop climbing and apply jump force
        }
    }

    /// <summary>
    /// Initiates the climbing state.
    /// Modifies Rigidbody physics, positions and rotates the character to the wall.
    /// </summary>
    private void StartClimbing()
    {
        _isClimbing = true;

        // Disable physics for manual control while climbing.
        // Setting Rigidbody to kinematic allows direct transform manipulation without fighting physics.
        _rigidbody.isKinematic = true;
        _rigidbody.velocity = Vector3.zero;        // Stop any previous movement
        _rigidbody.angularVelocity = Vector3.zero; // Stop any previous rotation

        // Position character correctly relative to the wall at the initial hit point.
        // Move character to be `_wallStickOffset` away from the wall along its normal.
        transform.position = _climbSurfaceInitialHit + _currentClimbNormal * _wallStickOffset;

        // Rotate character to face away from the wall (its back to the wall).
        transform.rotation = Quaternion.LookRotation(-_currentClimbNormal, Vector3.up);

        // Inform the Animator to play climbing animations.
        _animator?.SetBool(IsClimbingAnimHash, true);

        Debug.Log("Started climbing!", this);
    }

    /// <summary>
    /// Terminates the climbing state.
    /// Restores Rigidbody physics and optionally applies a jump-off force.
    /// </summary>
    /// <param name="jumpOff">If true, applies an outward and upward force when stopping.</param>
    private void StopClimbing(bool jumpOff = false)
    {
        _isClimbing = false;

        // Re-enable physics.
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true; // Ensure gravity is active again after climbing.

        // Apply force if jumping off the wall.
        if (jumpOff)
        {
            // Push off the wall slightly outwards and upwards.
            // `transform.forward` (character's forward) is away from the wall during climbing.
            Vector3 jumpDirection = (transform.forward + Vector3.up).normalized;
            _rigidbody.AddForce(jumpDirection * _jumpOffWallForce, ForceMode.Impulse);
        }
        else
        {
            // If just letting go, apply a small push away from the wall to prevent immediate re-grab.
            _rigidbody.AddForce(transform.forward * _jumpOffWallForce * 0.2f, ForceMode.Impulse);
        }

        // Inform the Animator to stop climbing animations and reset parameters.
        _animator?.SetBool(IsClimbingAnimHash, false);
        _animator?.SetFloat(ClimbXAnimHash, 0f);
        _animator?.SetFloat(ClimbYAnimHash, 0f);

        Debug.Log("Stopped climbing!", this);
    }

    /// <summary>
    /// Handles character movement while in the climbing state based on player input.
    /// </summary>
    private void HandleClimbingMovement()
    {
        // Get input for horizontal and vertical movement (e.g., A/D or Left/Right, W/S or Up/Down).
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement directions relative to the wall's orientation.
        // `climbRight` is perpendicular to the wall normal and global up.
        // `climbUp` is perpendicular to `climbRight` and the wall normal.
        Vector3 climbRight = Vector3.Cross(_currentClimbNormal, Vector3.up).normalized;
        Vector3 climbUp = Vector3.Cross(climbRight, _currentClimbNormal).normalized;

        // Combine inputs to get the desired movement direction along the wall.
        Vector3 desiredMoveDirection = (climbRight * horizontalInput + climbUp * verticalInput).normalized;

        // Apply movement to the character's position directly since Rigidbody is kinematic.
        transform.position += desiredMoveDirection * _climbSpeed * Time.deltaTime;

        // Update animation parameters for blend tree (e.g., walk up, walk right).
        // Using damping (last two parameters) for smoother animation transitions.
        _animator?.SetFloat(ClimbXAnimHash, horizontalInput, 0.1f, Time.deltaTime);
        _animator?.SetFloat(ClimbYAnimHash, verticalInput, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// Constantly checks if the character is still on a climbable wall and realigns
    /// its position and rotation to the wall normal. If no wall is detected, stops climbing.
    /// This is crucial for smoothly navigating curved surfaces or changing wall normals.
    /// </summary>
    private void CheckAndRealignToWall()
    {
        RaycastHit hit;
        // Raycast backwards from character's current position to re-find the wall it's attached to.
        // The ray origin is slightly in front of the character's visual center.
        Vector3 rayOrigin = transform.position + transform.forward * _wallStickOffset * 0.5f;
        // The ray length covers the `_wallStickOffset` and a bit more to ensure detection.
        float rayLength = _wallStickOffset * 1.5f;

        // Debug.DrawRay(rayOrigin, -transform.forward * rayLength, Color.magenta); // Debug visualization

        if (Physics.Raycast(rayOrigin, -transform.forward, out hit, rayLength, _climbableLayer))
        {
            // Smoothly update `_currentClimbNormal` if the wall's normal changes (e.g., on a curved surface).
            if (Vector3.Angle(_currentClimbNormal, hit.normal) > 5f) // Threshold to avoid micro-adjustments
            {
                _currentClimbNormal = Vector3.Slerp(_currentClimbNormal, hit.normal, Time.deltaTime * _wallRotationSpeed * 0.5f);
            }

            // Smoothly move the character to maintain the `_wallStickOffset` from the new hit point.
            Vector3 targetPosition = hit.point + _currentClimbNormal * _wallStickOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _wallRotationSpeed);

            // Smoothly rotate the character to face away from the updated wall normal.
            Quaternion targetRotation = Quaternion.LookRotation(-_currentClimbNormal, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _wallRotationSpeed);
        }
        else
        {
            // No climbable wall detected behind the character; it has moved off the surface.
            Debug.Log("Lost wall attachment, stopping climb.");
            StopClimbing();
        }
    }

    /// <summary>
    /// Draws gizmos in the editor for debugging purposes (e.g., wall detection ray, current normal).
    /// </summary>
    void OnDrawGizmos()
    {
        if (_capsuleCollider == null) return;

        // Draw the wall detection ray when not climbing
        Vector3 rayOrigin = transform.position + Vector3.up * _capsuleCollider.height * 0.5f;
        Gizmos.color = _canGrabWall && !_isClimbing ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * _wallDetectionDistance);

        // Draw climb normal and local axes if currently climbing
        if (_isClimbing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _currentClimbNormal * 1f); // Wall normal (outwards)
            Gizmos.color = Color.cyan;
            // Draw climb 'right' direction
            Gizmos.DrawRay(transform.position, Vector3.Cross(_currentClimbNormal, Vector3.up).normalized * 0.5f);
            // Draw climb 'up' direction
            Gizmos.DrawRay(transform.position, Vector3.Cross(Vector3.Cross(_currentClimbNormal, Vector3.up).normalized, _currentClimbNormal).normalized * 0.5f);
        }
    }
}

/*
/// EXAMPLE USAGE AND SETUP IN UNITY:
/// ---------------------------------
///
/// To use this `ClimbingSystem` script effectively, follow these setup steps in your Unity project:
///
/// 1.  **Create a 3D Character (Player GameObject):**
///     a.  Create an empty GameObject in your scene and name it, for example, "Player".
///     b.  **Add `CapsuleCollider`:** To the "Player" GameObject, add a `CapsuleCollider` component.
///         -   Adjust its `Center` and `Radius`/`Height` to fit your character's model (e.g., Center Y=1, Radius=0.3, Height=2).
///     c.  **Add `Rigidbody`:** To the "Player" GameObject, add a `Rigidbody` component.
///         -   Set `Mass` to 1.
///         -   Set `Drag` to 0, `Angular Drag` to 0.05.
///         -   **IMPORTANT:** In the Rigidbody component, under `Constraints`, check `Freeze Rotation` for X, Y, and Z axes. This prevents unwanted physics-based rotations that would conflict with the script's controlled rotation, especially during climbing. The script itself handles `freezeRotation = true` in `Awake`, but setting it here avoids initial issues. You can uncheck `Use Gravity` if you have a separate character controller that manages it, or leave it checked and this script will toggle it.
///     d.  **Add `Animator` (Optional but Recommended):** If you have character animations, add an `Animator` component. Assign an Avatar and an Animator Controller. (See Animator Setup below).
///     e.  **Add Visual Mesh:** Add a child GameObject (e.g., a 3D Capsule, or import your character model) to "Player" to give it a visual representation. Position it correctly relative to the `CapsuleCollider`.
///     f.  **Attach `ClimbingSystem` Script:** Drag and drop this `ClimbingSystem.cs` script onto your "Player" GameObject.
///
/// 2.  **Create Climbable Surfaces (Walls):**
///     a.  Create any 3D GameObject you want to be climbable (e.g., a Cube, a custom 3D model). Scale and position it as a wall.
///     b.  **Create a Custom Layer:**
///         -   In the Unity Editor, with the wall GameObject selected, go to the Inspector.
///         -   Below the "Tag" dropdown, click the "Layer" dropdown.
///         -   Select "Add Layer...". Choose an empty User Layer (e.g., User Layer 8).
///         -   Type a name for the layer, such as "Climbable".
///         -   Go back to your wall GameObject, click the "Layer" dropdown again, and select your newly created "Climbable" layer.
///     c.  **Ensure Collider:** Make sure your wall GameObject has a `Collider` component (e.g., `BoxCollider`, `MeshCollider`).
///
/// 3.  **Configure the `ClimbingSystem` Script in the Inspector:**
///     a.  Select your "Player" GameObject.
///     b.  In the `ClimbingSystem` component in the Inspector:
///         -   **Components:**
///             -   `_rigidbody`: Drag your Player's Rigidbody here (should auto-assign if on same GameObject).
///             -   `_animator`: Drag your Player's Animator here (if present and not auto-assigned).
///         -   **Climbing Settings:**
///             -   `_climbSpeed`: Adjust to control how fast the character moves on the wall (e.g., 2-5).
///             -   `_wallDetectionDistance`: How far in front of the character the script looks for climbable walls (e.g., 0.6 - 1.2, depending on character size).
///             -   `_wallStickOffset`: The distance the character will maintain from the wall while climbing (e.g., 0.5). This creates a slight gap.
///             -   `_wallRotationSpeed`: How quickly the character rotates to align its back with the wall (e.g., 10-20 for smooth turns).
///             -   `_jumpOffWallForce`: The strength of the impulse when the character jumps off the wall (e.g., 10-20).
///             -   `_climbableLayer`: **Crucially, select your "Climbable" layer from the dropdown.** This tells the raycasts what to look for.
///         -   **Input Settings:**
///             -   `_climbToggleKey`: `E` (default) or choose another key to start/stop climbing.
///             -   `_jumpOffWallKey`: `Space` (default) or choose another key to jump off the wall.
///
/// 4.  **Animator Setup (Optional but Highly Recommended):**
///     If you want animations:
///     a.  In your Animator Controller (Window > Animation > Animator), create the following parameters:
///         -   A `bool` parameter: `IsClimbing`
///         -   Two `float` parameters: `ClimbX`, `ClimbY`
///     b.  Create an animation state for "Climbing" (e.g., an empty state or a Blend Tree for directional climbing).
///     c.  **Transition from your "Idle/Run" state to "Climbing":**
///         -   Set `Has Exit Time` to `false`.
///         -   Add a Condition: `IsClimbing` is `true`.
///     d.  **Transition from "Climbing" state back to "Idle/Run":**
///         -   Set `Has Exit Time` to `false`.
///         -   Add a Condition: `IsClimbing` is `false`.
///     e.  **For directional climbing:** Within the "Climbing" state, you can create a 2D Blend Tree.
///         -   Set its `Parameters` to `ClimbX` and `ClimbY`.
///         -   Add your climb up, down, left, right, and idle-climb animations to this blend tree, positioning them appropriately (e.g., up at (0,1), down at (0,-1), etc.).
///
/// 5.  **Camera Setup (Crucial for Player Experience):**
///     -   For a third-person character, the camera usually follows the player. Ensure your camera script
///         doesn't interfere with the character's rotation during climbing, or has logic to adapt.
///         A simple Cinemachine Free Look camera often handles this well by following the character's transform.
///
/// 6.  **Test the Scene:**
///     -   Run the scene.
///     -   Move your character towards a wall assigned to the "Climbable" layer.
///     -   Press `E` (or your chosen toggle key). The character should snap to the wall and enter climbing mode.
///     -   Use `W, A, S, D` (or arrow keys) to move around on the wall.
///     -   Press `Space` (or your chosen jump key) to jump off the wall with an impulse.
///     -   Press `E` again to stop climbing without jumping off.
*/
```