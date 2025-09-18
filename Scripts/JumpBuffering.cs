// Unity Design Pattern Example: JumpBuffering
// This script demonstrates the JumpBuffering pattern in Unity
// Generated automatically - ready to use in your Unity project

The **Jump Buffering** design pattern is a common technique in game development, particularly for platformers, to make player controls feel more responsive and forgiving. It addresses the common problem where players might press the jump button a fraction of a second *before* or *after* actually landing on the ground. Without buffering, these "near-miss" inputs would be ignored, leading to a frustrating experience where the player feels their input wasn't registered.

The pattern works by:
1.  **Buffering Input:** When the jump button is pressed, the game "remembers" this input for a short duration (`jumpBufferTime`).
2.  **Executing on Condition:** If the player becomes grounded *during* this buffer window, the buffered jump input is immediately consumed, and the jump action is performed.
3.  **Clearing Buffer:** The buffer is cleared either when the jump is successfully executed or when the `jumpBufferTime` expires without the player becoming grounded.

This ensures that a player who presses jump slightly early will still jump as soon as they touch the ground, making the game feel much smoother and more intuitive.

---

Here is a complete, practical C# Unity example demonstrating the Jump Buffering pattern:

```csharp
using UnityEngine;
using System.Collections; // System.Collections is typically included for Unity, though not strictly required for this specific timestamp-based buffering approach.

/// <summary>
/// A complete, practical C# Unity example demonstrating the 'JumpBuffering' design pattern.
/// This pattern makes jumping feel more forgiving and responsive by allowing jump inputs
/// to be "buffered" for a short duration even if the player isn't perfectly grounded
/// at the exact moment the jump button is pressed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures the GameObject has a Rigidbody2D component.
public class JumpBufferingController : MonoBehaviour
{
    // --- Public Inspector Variables ---

    [Header("Jump Settings")]
    [Tooltip("The vertical force applied when the player jumps.")]
    [SerializeField] private float jumpForce = 10f;

    [Tooltip("The duration (in seconds) that a jump input will be buffered. " +
             "If the player becomes grounded within this time after pressing jump, they will jump.")]
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Ground Check Settings")]
    [Tooltip("The Transform representing the origin point for the ground check.")]
    [SerializeField] private Transform groundCheckTransform;

    [Tooltip("The radius of the overlap sphere/circle used to detect if the player is grounded.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("A LayerMask indicating which layers are considered 'ground'.")]
    [SerializeField] private LayerMask groundLayer;

    // --- Private Internal Variables ---

    private Rigidbody2D rb;               // Reference to the Rigidbody2D component.
    private bool isGrounded;              // True if the player is currently touching the ground.
    private bool jumpBuffered;            // True if a jump input has been received and is currently buffered.
    private float lastJumpPressTime;      // The Time.time when the jump button was last pressed.

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D not found on " + gameObject.name + ". JumpBufferingController requires a Rigidbody2D.");
            enabled = false; // Disable the script if essential component is missing.
        }

        // Ensure groundCheckTransform is assigned. If not, try to use the player's own transform.
        if (groundCheckTransform == null)
        {
            Debug.LogWarning("Ground Check Transform not assigned. Using player's transform for ground checking. " +
                             "Consider creating an empty child GameObject slightly below the player's feet for more accurate ground checks.", this);
            groundCheckTransform = transform;
        }
    }

    private void Update()
    {
        // 1. Handle Jump Input
        // Check if the "Jump" button was pressed down this frame.
        // By default, "Jump" is mapped to Spacebar.
        if (Input.GetButtonDown("Jump"))
        {
            // If jump is pressed, activate the jump buffer.
            jumpBuffered = true;
            lastJumpPressTime = Time.time; // Record the time of the jump press.
            // Debug.Log($"Jump button pressed at {Time.time}. Jump buffered.");
        }

        // 2. Clear Jump Buffer if Time Expires
        // If the buffered jump has not been executed and the buffer time has passed,
        // clear the buffer so it doesn't trigger unexpectedly later.
        if (jumpBuffered && Time.time - lastJumpPressTime > jumpBufferTime)
        {
            jumpBuffered = false;
            // Debug.Log($"Jump buffer expired at {Time.time}.");
        }
    }

    private void FixedUpdate()
    {
        // Ground Check is typically done in FixedUpdate for physics accuracy,
        // as it runs at a fixed timestep, aligning with Rigidbody updates.
        CheckIsGrounded();

        // 3. Execute Jump if Buffered and Grounded
        // This is the core of the Jump Buffering pattern:
        // If the player is currently grounded AND a jump was buffered, then execute the jump.
        if (isGrounded && jumpBuffered)
        {
            // Debug.Log($"Executing buffered jump at {Time.time}. Player is grounded.");
            ExecuteJump();
        }
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Checks if the player is currently touching the ground using an OverlapCircle.
    /// Updates the 'isGrounded' internal state. This should be called in FixedUpdate
    /// for consistent physics interaction.
    /// </summary>
    private void CheckIsGrounded()
    {
        // Physics2D.OverlapCircle checks for colliders within a circular area.
        // We check around the groundCheckTransform's position with the specified radius and layer mask.
        // This is efficient for checking if any collider on the 'groundLayer' is within range.
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Applies vertical force to the Rigidbody2D to make the player jump.
    /// Also clears the jump buffer after a successful jump to prevent multiple jumps
    /// from a single button press.
    /// </summary>
    private void ExecuteJump()
    {
        // Apply an impulse force upwards.
        // We set the vertical velocity to 0 first to ensure consistent jump height,
        // regardless of any previous downward velocity (e.g., falling).
        rb.velocity = new Vector2(rb.velocity.x, 0f); // Reset vertical velocity.
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); // Apply jump force.

        // Crucially, clear the buffer once the jump has been successfully executed.
        // This prevents the same buffered input from triggering another jump if
        // the player somehow remains grounded or lands very quickly after the jump.
        jumpBuffered = false;
    }

    // --- Editor Debugging ---

    /// <summary>
    /// Draws gizmos in the Scene view for visual debugging,
    /// specifically showing the ground check radius. This helps developers
    /// visualize the area being used for ground detection.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (groundCheckTransform == null) return; // Only draw if a transform is assigned.

        // Set the color for the ground check gizmo.
        // Green if grounded, red if not.
        Gizmos.color = isGrounded ? Color.green : Color.red;

        // Draw a wire sphere/circle at the ground check position.
        // For 2D, a wire circle effectively represents the OverlapCircle.
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}

/*
/// --- Example Usage in a Unity Project ---

To implement the Jump Buffering pattern using this script:

1.  **Create a New Unity Project** (or open an existing one).

2.  **Setup a Player GameObject:**
    *   Create an empty GameObject in your scene (e.g., "Player").
    *   Add a `Sprite Renderer` component to it (you can give it a basic sprite or color).
    *   Add a `Rigidbody2D` component.
        *   Set "Gravity Scale" to a value like `3` for a snappier jump feel.
        *   Ensure "Body Type" is `Dynamic`.
    *   Add a `Capsule Collider 2D` or `Box Collider 2D` to the "Player" and adjust its size to fit.

3.  **Setup a Ground GameObject:**
    *   Create a Cube or Sprite in your scene (e.g., "Ground").
    *   Add a `Box Collider 2D` component to it.
    *   **Define a "Ground" Layer:**
        *   In the Unity Editor, click the "Layer" dropdown in the Inspector (top right, below GameObject name).
        *   Select "Add Layer...".
        *   Create a new layer named "Ground" (e.g., at Layer 8).
        *   Now, select your "Ground" GameObject and assign it to this newly created "Ground" layer.

4.  **Attach the `JumpBufferingController` Script:**
    *   Create a new C# script named `JumpBufferingController.cs` in your `Assets` folder (or copy-paste the above code into an existing one).
    *   Drag and drop `JumpBufferingController.cs` onto your "Player" GameObject in the Hierarchy.

5.  **Configure Script in Inspector (on your "Player" GameObject):**
    *   Locate the `Jump Buffering Controller` component in the Inspector.
    *   **Jump Force:** Set to a suitable value, e.g., `10` or `15`. (Experiment based on your game's scale and gravity).
    *   **Jump Buffer Time:** A common value is `0.1` to `0.2` seconds. This is your "forgiveness window."
    *   **Ground Check Transform:**
        *   **Recommended:** Create an empty child GameObject of your "Player" (Right-click "Player" -> Create Empty). Name it "GroundCheckPoint".
        *   Position "GroundCheckPoint" slightly below the player's pivot point. For example, if your player's pivot is in the center, move this child down by about half the player's height plus a small offset (e.g., `Y = -0.55` if player height is 1).
        *   Drag this "GroundCheckPoint" GameObject into the `Ground Check Transform` slot on the `Jump Buffering Controller`.
        *   (Alternatively, you can drag the "Player" GameObject itself into this slot, but a child point offers more precise control over ground detection).
    *   **Ground Check Radius:** Set to a small value, e.g., `0.2` or `0.3`. This determines the size of the circle used to detect ground.
    *   **Ground Layer:** From the dropdown, select the "Ground" layer you created earlier.

6.  **Run the Game:**
    *   Press Play in the Unity Editor.
    *   **Control:** Use the configured "Jump" input (typically the Spacebar by default).
    *   **Observe:**
        *   Jump as normal.
        *   Try pressing the jump button slightly *before* your player actually lands on the ground after a fall.
        *   You should notice that even with this slightly early input, the player still jumps as soon as they touch the ground. This demonstrates the buffering in action, making the controls feel significantly more responsive and forgiving.
        *   The green/red gizmo in the Scene view will show your ground check area and whether it's currently detecting ground.

This setup provides a robust and user-friendly jump system for 2D platformers and similar games!
*/
```