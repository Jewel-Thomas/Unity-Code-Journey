// Unity Design Pattern Example: OmniDirectionalMovement
// This script demonstrates the OmniDirectionalMovement pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical example of the 'OmniDirectionalMovement' design pattern. This pattern focuses on encapsulating the logic for allowing a character or object to move freely in any direction, typically based on player input, relative to a reference frame (like the camera or world space).

The example uses Unity's `CharacterController` component, which is a common and robust choice for player movement as it handles collisions effectively without relying directly on the physics engine's `Rigidbody` forces, giving more direct control.

```csharp
using UnityEngine;
using System.Collections; // Included for standard Unity script structure, though not strictly needed for this specific logic.

/// <summary>
///     [DESIGN PATTERN] OmniDirectionalMovement
///     
///     This script implements the OmniDirectionalMovement pattern in Unity.
///     It allows a GameObject to move freely in any direction based on player input,
///     typically relative to the camera's forward direction (for 3D games)
///     or world space (for top-down 2D/3D games).
///     
///     The OmniDirectionalMovement pattern is about abstracting and encapsulating
///     the core mechanics of flexible movement. It typically involves:
///     
///     1.  **Input Interpretation:** Translating raw player input (e.g., WASD keys, joystick axes)
///         into a conceptual desired movement direction.
///     2.  **Direction Calculation:** Computing a normalized vector that represents the
///         final movement direction in the game's world space, often relative to the camera's
///         orientation for intuitive player control.
///     3.  **Movement Application:** Using a movement mechanism (like `CharacterController.Move`,
///         `Rigidbody.velocity`, or direct `Transform.Translate`) to apply this calculated
///         direction and a defined speed to the GameObject's position.
///     4.  **Orientation Adjustment:** Optionally, rotating the GameObject to visually
///         face its movement direction or a specific target, enhancing the feeling of control.
///     5.  **Auxiliary Mechanics:** Handling other movement-related aspects like gravity,
///         jumping, or collision detection within this encapsulated component.
///     
///     This specific implementation uses Unity's `CharacterController` component, which is ideal
///     for player-controlled characters as it handles collisions without using the physics engine
///     directly, providing fine-grained control over movement.
/// </summary>
[RequireComponent(typeof(CharacterController))] // Ensures a CharacterController is present on the GameObject.
public class OmniDirectionalMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the character moves horizontally.")]
    [SerializeField] private float moveSpeed = 6.0f;
    [Tooltip("The speed at which the character rotates to face its movement direction.")]
    [SerializeField] private float rotationSpeed = 720.0f; // Degrees per second

    [Header("Gravity Settings")]
    [Tooltip("Should gravity be applied to the character?")]
    [SerializeField] private bool applyGravity = true;
    [Tooltip("Strength of gravity pulling the character down.")]
    [SerializeField] private float gravityStrength = -9.81f; // Standard Earth gravity
    [Tooltip("The maximum downward velocity the character can reach due to gravity.")]
    [SerializeField] private float terminalFallSpeed = -20.0f; // Max downward velocity

    private CharacterController _characterController;
    private Vector3 _currentHorizontalMoveDirection; // Stores the calculated horizontal movement direction
    private Vector3 _verticalVelocity;               // Stores and applies gravity and vertical movement

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used to get references to other components on the GameObject.
    /// </summary>
    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            Debug.LogError("OmniDirectionalMover requires a CharacterController component, but none was found!", this);
            enabled = false; // Disable the script if the required component is missing.
        }
    }

    /// <summary>
    /// Update is called once per frame. This is where input is gathered,
    /// movement direction is calculated, and the character is moved and rotated.
    /// Using Update for CharacterController.Move is standard practice as it's not physics-based.
    /// </summary>
    void Update()
    {
        // 1. Input Interpretation & 2. Direction Calculation
        HandleMovementInput();

        // 5. Auxiliary Mechanics: Apply gravity
        ApplyGravity();

        // 3. Movement Application
        MoveCharacter();

        // 4. Orientation Adjustment
        RotateCharacter();
    }

    /// <summary>
    /// Gathers player input for horizontal and vertical movement and
    /// calculates the desired movement direction in world space, relative to the camera.
    /// This is crucial for typical third-person or over-the-shoulder camera controls.
    /// </summary>
    private void HandleMovementInput()
    {
        // Get raw input axes.
        // Input.GetAxis("Horizontal") maps to A/D keys, Left/Right Arrow keys, or gamepad left stick X-axis.
        // Input.GetAxis("Vertical") maps to W/S keys, Up/Down Arrow keys, or gamepad left stick Y-axis.
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Create a 2D vector from input. Normalize if magnitude > 1 to prevent faster diagonal movement.
        Vector2 inputVector = new Vector2(horizontalInput, verticalInput);
        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }

        // Determine the camera's forward and right vectors.
        // Ensure camera's forward is purely horizontal for character movement (ignore Y-component).
        // This prevents the character from tilting up/down or moving faster when the camera looks up/down.
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize(); // Normalize after flattening to maintain direction.

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize(); // Normalize after flattening to maintain direction.

        // Calculate the desired horizontal movement direction based on camera orientation and input.
        _currentHorizontalMoveDirection = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;

        // If there's no input, set movement direction to zero to ensure the character stops moving horizontally.
        if (inputVector.magnitude == 0)
        {
            _currentHorizontalMoveDirection = Vector3.zero;
        }
    }

    /// <summary>
    /// Applies gravity to the character's vertical velocity if enabled and not grounded.
    /// Resets vertical velocity to a small negative value if grounded to ensure continuous
    /// ground detection and proper handling of slopes.
    /// </summary>
    private void ApplyGravity()
    {
        if (applyGravity)
        {
            if (_characterController.isGrounded)
            {
                // If grounded, push down slightly to keep character "stuck" to the ground
                // and correctly detect slopes.
                _verticalVelocity.y = -0.5f; 
            }
            else
            {
                // Apply gravity, clamping at terminalFallSpeed to prevent infinite acceleration.
                _verticalVelocity.y = Mathf.Max(_verticalVelocity.y + gravityStrength * Time.deltaTime, terminalFallSpeed);
            }
        }
        else
        {
            // If gravity is disabled, ensure vertical velocity is zero to prevent accidental vertical movement.
            _verticalVelocity.y = 0f;
        }
    }

    /// <summary>
    /// Moves the character using the CharacterController based on the calculated
    /// horizontal movement direction and vertical velocity (gravity).
    /// </summary>
    private void MoveCharacter()
    {
        // Combine horizontal movement and vertical velocity (gravity/jump).
        Vector3 motion = _currentHorizontalMoveDirection * moveSpeed;
        motion.y = _verticalVelocity.y;

        // Apply the calculated motion using CharacterController.Move.
        // Time.deltaTime is crucial for frame-rate independent movement.
        _characterController.Move(motion * Time.deltaTime);
    }

    /// <summary>
    /// Rotates the character to smoothly face the direction it is currently moving.
    /// This provides visual feedback for the omnidirectional movement.
    /// </summary>
    private void RotateCharacter()
    {
        // Only rotate if there's actual movement input to avoid jitter when standing still.
        // Using a small threshold (0.1f) handles cases where input might be very small but not exactly zero.
        if (_currentHorizontalMoveDirection.magnitude > 0.1f)
        {
            // Calculate the target rotation based on the current horizontal movement direction.
            // Quaternion.LookRotation creates a rotation that looks along the forward direction.
            Quaternion targetRotation = Quaternion.LookRotation(_currentHorizontalMoveDirection);

            // Smoothly interpolate between the current rotation and the target rotation.
            // Quaternion.RotateTowards provides constant angular speed.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // --- Example Usage in Unity Editor ---
    /*
    * To implement and test this OmniDirectionalMovement pattern in your Unity project:
    * 
    * 1.  **Create a New Scene:** Start with a fresh scene to avoid conflicts.
    * 2.  **Create a Player GameObject:**
    *     -   In the Hierarchy window, right-click -> 3D Object -> Capsule (or Cube/Sphere).
    *     -   Rename it to "Player".
    * 3.  **Add Character Controller Component:**
    *     -   Select the "Player" GameObject.
    *     -   In the Inspector, click "Add Component" and search for "Character Controller". Add it.
    *     -   Adjust its 'Center' (e.g., Y=1 for a default Capsule of height 2) and 'Radius' if needed
    *         to match your character's model size and ensure its bottom is slightly below the pivot.
    * 4.  **Create and Attach the Script:**
    *     -   Create a new C# script named "OmniDirectionalMover" (matching the class name).
    *     -   Copy and paste the entire code above into this new script.
    *     -   Drag and drop the "OmniDirectionalMover" script onto your "Player" GameObject in the Hierarchy.
    * 5.  **Configure Script Properties (in Inspector):**
    *     -   **Move Speed:** Set this to a value like `5` or `8` to control how fast the character moves.
    *     -   **Rotation Speed:** A value like `500` or `720` will provide smooth turning.
    *     -   **Apply Gravity:** Keep this checked (`true`) if you want the character to fall when not grounded.
    * 6.  **Ensure a Main Camera Exists:**
    *     -   Make sure your scene has a `Camera` GameObject tagged as "MainCamera".
    *     -   Position the camera behind and slightly above your "Player" for a typical third-person view.
    *     -   For a more complete setup, you would add a separate script to the camera to make it follow the player.
    * 7.  **Add a Ground Plane:**
    *     -   Right-click in Hierarchy -> 3D Object -> Plane. This provides a surface for the character to walk on and for gravity to work.
    * 8.  **Play the Scene:**
    *     -   Run the game. Use the **WASD** or **Arrow keys** to move your character.
    *     -   Observe how the character moves freely in any direction relative to the camera and rotates to face its movement.
    * 
    * This script demonstrates how to create a reusable component that encapsulates the
    * omnidirectional movement logic, making it easily configurable and applicable to various characters.
    */
}
```