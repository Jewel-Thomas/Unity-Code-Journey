// Unity Design Pattern Example: CoyoteTimeJumping
// This script demonstrates the CoyoteTimeJumping pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical example of the 'Coyote Time Jumping' design pattern, along with an optional 'Jump Buffer', to enhance player experience in platformer games.

**Coyote Time Jumping:** Allows the player to jump for a very brief period after walking off a ledge, making jumps feel more forgiving and responsive.
**Jump Buffer:** Allows the player to press the jump button slightly *before* landing, and the jump will execute automatically as soon as they touch the ground.

Both patterns reduce player frustration by making jump timing less precise.

```csharp
using UnityEngine;
using System.Collections; // Included for completeness, though not strictly required for this specific implementation.

/// <summary>
/// Implements the Coyote Time Jumping and Jump Buffer design patterns in Unity.
///
/// Coyote Time: Allows the player to jump for a short duration after leaving solid ground.
/// Jump Buffer: Stores a jump input for a short duration, allowing it to execute
///              immediately upon landing if pressed slightly before.
///
/// This script requires a Rigidbody2D component and is designed for 2D platformers.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures a Rigidbody2D is attached to the GameObject.
public class PlayerCoyoteTimeJump : MonoBehaviour
{
    // --- Movement Parameters ---
    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed of the player.")]
    [SerializeField] private float moveSpeed = 7f;

    [Tooltip("Vertical force applied to the player when initiating a jump.")]
    [SerializeField] private float jumpForce = 12f;

    // --- Coyote Time & Jump Buffer Settings ---
    [Header("Jump Forgiveness Settings")]
    [Tooltip("The duration (in seconds) that the player can still jump after walking off a platform.")]
    [SerializeField] private float coyoteTimeDuration = 0.15f; // Typical values: 0.05s to 0.2s.

    [Tooltip("The duration (in seconds) that a jump input will be 'buffered' " +
             "and stored, so it can execute immediately upon landing.")]
    [SerializeField] private float jumpBufferTime = 0.1f; // Typical values: 0.05s to 0.2s.

    // --- Ground Check Settings ---
    [Header("Ground Check Settings")]
    [Tooltip("Local offset from the player's transform.position for the ground check origin.")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.9f);

    [Tooltip("Radius of the Physics2D.OverlapCircle used to detect if the player is grounded.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("LayerMask to define which layers are considered 'ground' for the player.")]
    [SerializeField] private LayerMask groundLayer;

    // --- Private Internal State ---
    private Rigidbody2D _rigidbody;
    private float _moveInput; // Stores the horizontal input (-1 to 1).
    private bool _isGrounded; // True if the player is currently touching ground.

    // Coyote Time variables
    // This counter stores how much coyote time is remaining.
    // It's reset to 'coyoteTimeDuration' when grounded and counts down when airborne.
    private float _coyoteTimeCounter;

    // Jump Buffer variables
    // This counter stores how much jump buffer time is remaining after a jump input.
    // It's reset to 'jumpBufferTime' when jump is pressed and counts down.
    private float _jumpBufferCounter;

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Get a reference to the Rigidbody2D component on this GameObject.
        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
        {
            // Log an error and disable the script if Rigidbody2D is missing.
            // This prevents NullReferenceExceptions during runtime.
            Debug.LogError("PlayerCoyoteTimeJump: Rigidbody2D component not found! Please add a Rigidbody2D to this GameObject.");
            enabled = false; // Disable the script.
        }
    }

    private void Update()
    {
        // 1. Process Input
        // Get horizontal movement input from standard input axes (e.g., A/D keys, Left/Right arrow keys).
        _moveInput = Input.GetAxisRaw("Horizontal");

        // Check if the player is currently touching ground.
        _isGrounded = CheckGrounded();

        // 2. Coyote Time Logic
        // When the player is grounded, reset the coyote time counter to its full duration.
        // This means they have the full coyote time available if they walk off a ledge.
        if (_isGrounded)
        {
            _coyoteTimeCounter = coyoteTimeDuration;
        }
        // If the player is not grounded, start counting down the coyote time.
        // Once it reaches 0, the coyote time window has closed.
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }

        // 3. Jump Buffer Logic
        // If the jump button is pressed down (only on the initial press), activate the jump buffer counter.
        // This allows the player to "queue" a jump before actually being able to jump.
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = jumpBufferTime;
        }
        // If the jump button is not pressed, count down the jump buffer.
        // Once it reaches 0, the buffered jump input is no longer valid.
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }

        // 4. Execute Jump Logic
        // A jump should occur if:
        //   a) The jump buffer is active (meaning jump was pressed recently)
        //   AND
        //   b) The player is either currently grounded OR is within the coyote time window.
        if (_jumpBufferCounter > 0f && (_isGrounded || _coyoteTimeCounter > 0f))
        {
            // Apply a vertical force to the Rigidbody2D to make the player jump.
            // We set the velocity directly to ensure a consistent jump height regardless of current fall speed.
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpForce);

            // Crucially, reset both counters immediately after a jump.
            // This prevents multiple jumps from a single input and ensures the
            // coyote time/jump buffer is consumed upon a successful jump.
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;

            // Optional: You could trigger jump sound effects, particle effects, or animations here.
            // Debug.Log("Player Jumped! (Coyote Time or Jump Buffer used)");
        }
    }

    private void FixedUpdate()
    {
        // It's best practice to apply physics-related movement in FixedUpdate
        // for consistent behavior, as FixedUpdate runs at a fixed time interval.
        // We only modify the horizontal velocity, keeping the vertical velocity
        // (affected by gravity and jumps) intact.
        _rigidbody.velocity = new Vector2(_moveInput * moveSpeed, _rigidbody.velocity.y);
    }

    // --- Helper Methods ---

    /// <summary>
    /// Checks if the player is currently grounded using a Physics2D.OverlapCircle.
    /// </summary>
    /// <returns>True if the OverlapCircle detects any colliders on the groundLayer, false otherwise.</returns>
    private bool CheckGrounded()
    {
        // Calculate the world position of the ground check origin.
        // This uses the player's current position plus the local offset.
        Vector2 groundCheckWorldPosition = (Vector2)transform.position + groundCheckOffset;

        // Perform the OverlapCircleAll check. This will return an array of all colliders
        // that intersect with the specified circle.
        // We only care if *any* collider on the 'groundLayer' is hit.
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(groundCheckWorldPosition, groundCheckRadius, groundLayer);

        // If the array contains any colliders, it means we hit something on the ground layer, so the player is grounded.
        return hitColliders.Length > 0;
    }

    // --- Editor-only Visualization ---

    /// <summary>
    /// Draws a gizmo in the Scene view to visualize the ground check area.
    /// This is incredibly useful for debugging and setting up the ground check parameters.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only draw gizmos if a Rigidbody2D exists (meaning the script is likely active).
        if (_rigidbody != null)
        {
            Gizmos.color = Color.green; // Set gizmo color to green for visibility.
            // Draw a wire sphere at the ground check position with the specified radius.
            // The position is calculated the same way as in CheckGrounded().
            Gizmos.DrawWireSphere((Vector2)transform.position + groundCheckOffset, groundCheckRadius);
        }
    }
}

/*
 * --- HOW TO USE THIS SCRIPT IN YOUR UNITY PROJECT ---
 *
 * This section provides step-by-step instructions to get the Coyote Time Jumping
 * and Jump Buffer working in a real Unity project.
 *
 * 1.  Create a new 2D Project in Unity (or open an existing one).
 *
 * 2.  Create the C# Script:
 *     a. In the Project window, right-click -> Create -> C# Script.
 *     b. Name it "PlayerCoyoteTimeJump" (exactly matching the class name).
 *     c. Copy and paste the entire code above into this new script file.
 *     d. Save the script (Ctrl+S or Cmd+S).
 *
 * 3.  Create a Player GameObject:
 *     a. In the Hierarchy window, right-click -> 2D Object -> Sprites -> Square.
 *     b. Rename the new GameObject to "Player".
 *     c. Adjust its Scale if needed (e.g., X:1, Y:1).
 *
 * 4.  Add Required Components to the Player:
 *     a. Select the "Player" GameObject in the Hierarchy.
 *     b. In the Inspector window, click "Add Component".
 *     c. Search for and add "Rigidbody2D".
 *        - Set its "Gravity Scale" to something like 3-5 for a snappier jump feel.
 *        - Expand "Constraints" and check "Freeze Rotation Z" to prevent the player from rotating.
 *     d. Click "Add Component" again, search for and add "Box Collider 2D".
 *        - Adjust its "Size" and "Offset" to fit your player sprite accurately.
 *     e. Click "Add Component" one last time, search for and add the "PlayerCoyoteTimeJump" script.
 *
 * 5.  Configure the PlayerCoyoteTimeJump Script in the Inspector:
 *     a. Select the "Player" GameObject.
 *     b. In the Inspector, locate the "PlayerCoyoteTimeJump" component.
 *     c. **Movement Settings:**
 *        - Move Speed: e.g., 7 (how fast the player moves horizontally)
 *        - Jump Force: e.g., 12 (how high the player jumps)
 *     d. **Jump Forgiveness Settings:**
 *        - Coyote Time Duration: e.g., 0.15 (the crucial 'coyote time' window)
 *        - Jump Buffer Time: e.g., 0.1 (the 'jump buffer' window)
 *     e. **Ground Check Settings:**
 *        - Ground Check Offset: Adjust the Y value (e.g., Y: -0.9) so the green gizmo circle
 *          (visible in the Scene view when the Player is selected) is positioned just below
 *          the player's feet. This ensures accurate ground detection.
 *        - Ground Check Radius: e.g., 0.2 (adjust this to be a small circle that fits between
 *          the player's feet or slightly overlaps the bottom edge).
 *        - **Ground Layer:** This is CRITICAL. You need to tell the script what your "ground" is.
 *          - Go to the "Layers" dropdown in the Unity editor (top right, near the Inspector).
 *          - Click "Add Layer...".
 *          - In an empty "User Layer" slot (e.g., User Layer 8), type "Ground".
 *          - Back on your Player GameObject's "PlayerCoyoteTimeJump" script in the Inspector,
 *            click the dropdown next to "Ground Layer" and select "Ground".
 *
 * 6.  Create Ground/Platform GameObjects:
 *     a. In the Hierarchy, right-click -> 2D Object -> Sprites -> Square.
 *     b. Rename it to "Ground".
 *     c. Add a "Box Collider 2D" to it.
 *     d. In the Inspector for the "Ground" object, find the "Layer" dropdown (next to its name)
 *        and set its Layer to "Ground" (the one you created in step 5e).
 *     e. Position and scale this "Ground" object to form your platforms or terrain.
 *     f. Duplicate this "Ground" object (Ctrl+D or Cmd+D) to create multiple platforms for testing.
 *
 * 7.  Run the game!
 *     - Press 'A' and 'D' (or Left/Right arrow keys) to move the player horizontally.
 *     - Press 'Spacebar' to jump.
 *     - Test Coyote Time: Walk off a ledge and try pressing 'Spacebar' *just after* you leave the platform.
 *       You should still be able to jump for a brief moment.
 *     - Test Jump Buffer: Run towards a platform, and press 'Spacebar' *just before* you land on it.
 *       The jump should execute immediately upon touching the ground, even if your timing was slightly off.
 *
 * Experiment with the `coyoteTimeDuration` and `jumpBufferTime` values to find what feels
 * best for your game! Remember, small values (0.05s to 0.2s) are usually sufficient to
 * create a noticeable improvement in player feel without making the game feel too "easy" or floaty.
 */
```