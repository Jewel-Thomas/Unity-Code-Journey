// Unity Design Pattern Example: PrecisionJumpingSystem
// This script demonstrates the PrecisionJumpingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PrecisionJumpingSystem' design pattern in Unity focuses on creating highly responsive, predictable, and fine-grained control over character jumps, crucial for platformers and games requiring precise movement. It integrates several common mechanics to enhance player agency and reduce frustration.

Here's a complete C# Unity example demonstrating this pattern, including variable jump height, coyote time, jump buffering, and air jumps.

---

### **1. Create the C# Script**

Create a new C# script in your Unity project, name it `PrecisionJumpingSystem`, and paste the following code:

```csharp
using UnityEngine;
using System.Collections; // Not strictly needed for this example, but common.

/// <summary>
/// PrecisionJumpingSystem: A comprehensive system for highly controllable character jumps.
///
/// This design pattern aims to provide players with highly responsive, predictable,
/// and fine-grained control over character jumps, essential for platformers and
/// games requiring precise movement. It integrates several common mechanics
/// to enhance player agency and reduce frustration beyond a simple "apply force once" jump.
///
/// Key Features Implemented:
/// 1.  Variable Jump Height: Jump height is proportional to the duration the jump button is held,
///     allowing for small hops or full leaps.
/// 2.  Coyote Time: A brief grace period after leaving a platform where the player can still
///     initiate a jump, mitigating unfair "pixel-perfect" fall-offs.
/// 3.  Jump Buffering: Registers a jump input for a short duration *before* landing,
///     automatically executing the jump on touchdown, preventing missed jumps due to
///     slight timing inaccuracies.
/// 4.  Air Jumps (Double Jump, etc.): Allows for one or more additional jumps while airborne,
///     adding mobility and requiring precise timing for advanced maneuvers.
/// 5.  Custom Gravity: Allows for snappier falls or greater control during the descent
///     than standard physics might offer.
///
/// Usage:
/// 1.  Attach this script to your player GameObject.
/// 2.  Ensure your player GameObject has a Rigidbody2D component.
/// 3.  Configure the various jump parameters in the Inspector.
/// 4.  Set the 'Ground Check Layer' to a layer that your ground/platform objects are on.
/// 5.  (Optional) Define "Jump" in your Input Manager (Edit -> Project Settings -> Input Manager).
///     By default, it uses 'Spacebar'.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures the Rigidbody2D component is present.
public class PrecisionJumpingSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the Rigidbody2D component on this GameObject.")]
    private Rigidbody2D rb;

    [Header("Jump Properties")]
    [Tooltip("The initial upward force applied when a jump begins.")]
    [SerializeField] private float initialJumpForce = 12f;
    [Tooltip("Additional upward force applied per second while the jump button is held, up to 'maxJumpHoldTime'.")]
    [SerializeField] private float variableJumpMultiplier = 15f;
    [Tooltip("Maximum duration in seconds the jump button can be held for variable jump height.")]
    [SerializeField] private float maxJumpHoldTime = 0.25f;
    [Tooltip("Allows for a certain number of additional jumps while airborne (e.g., 1 for double jump).")]
    [SerializeField] private int airJumpsAllowed = 1;
    [Tooltip("Multiplier for gravity when falling. Higher values make the character fall faster.")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [Tooltip("Multiplier for gravity when ascending or at the peak of a jump. Higher values can make the jump feel 'snappier'.")]
    [SerializeField] private float jumpApexGravityMultiplier = 1.5f;

    [Header("Ground Check")]
    [Tooltip("Radius of the circle used to detect if the character is grounded.")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [Tooltip("Offset from the character's pivot point for the ground check (e.g., negative Y to check below feet).")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.6f);
    [Tooltip("The LayerMask identifying what counts as 'ground' for jumping.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Precision Mechanics")]
    [Tooltip("Duration in seconds after leaving ground where a jump is still possible.")]
    [SerializeField] private float coyoteTimeDuration = 0.15f;
    [Tooltip("Duration in seconds before landing where a jump input will be buffered and executed on touch-down.")]
    [SerializeField] private float jumpBufferDuration = 0.1f;

    // Internal State Variables
    private bool isGrounded;
    private float timeSinceGrounded; // Timer for Coyote Time
    private float jumpBufferTimer;   // Timer for Jump Buffering
    private int currentAirJumps;     // Tracks remaining air jumps
    private bool isJumpButtonHeld;
    private float jumpHoldTimer;     // Tracks how long the jump button has been held
    private bool hasJumpedThisPress; // Prevents multiple jumps from a single button press (for initial jump)

    private const string JumpInputName = "Jump"; // Default Unity Input Manager jump button (Spacebar)

    // --- Core MonoBehaviour Methods ---

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Initialize air jumps
        currentAirJumps = airJumpsAllowed;
    }

    void Update()
    {
        // --- Input Handling ---
        if (Input.GetButtonDown(JumpInputName))
        {
            jumpBufferTimer = jumpBufferDuration; // Start/reset jump buffer timer
            isJumpButtonHeld = true;
            hasJumpedThisPress = false; // Reset for a new jump press
        }
        if (Input.GetButtonUp(JumpInputName))
        {
            isJumpButtonHeld = false;
            // Instantly stop upward velocity if button released early and still ascending
            if (rb.velocity.y > 0 && hasJumpedThisPress)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f); // Halve upward velocity
            }
        }

        // --- Timers Update ---
        // Decrement jump buffer timer
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Update jump hold timer for variable jump
        if (isJumpButtonHeld && hasJumpedThisPress && jumpHoldTimer < maxJumpHoldTime)
        {
            jumpHoldTimer += Time.deltaTime;
        }

        // Update coyote time timer
        if (isGrounded)
        {
            timeSinceGrounded = coyoteTimeDuration; // Reset coyote timer when grounded
        }
        else
        {
            if (timeSinceGrounded > 0)
            {
                timeSinceGrounded -= Time.deltaTime; // Decrement coyote timer when not grounded
            }
        }
    }

    void FixedUpdate()
    {
        // --- Ground Check ---
        CheckGrounded();

        // --- Reset Air Jumps on Grounded ---
        if (isGrounded)
        {
            currentAirJumps = airJumpsAllowed;
        }

        // --- Try to Jump (Coyote Time, Jump Buffering, Air Jumps) ---
        // Jump conditions:
        // 1. Jump button was pressed recently (buffer)
        // 2. Either grounded (or in coyote time) OR has air jumps available
        if (jumpBufferTimer > 0 && (timeSinceGrounded > 0 || currentAirJumps > 0))
        {
            ExecuteJump();
        }

        // --- Apply Custom Gravity ---
        ApplyCustomGravity();
    }

    // --- Helper Methods ---

    /// <summary>
    /// Performs a ground check using a Physics2D.OverlapCircle.
    /// Updates the `isGrounded` state variable.
    /// </summary>
    private void CheckGrounded()
    {
        Vector2 position = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.OverlapCircle(position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Executes a jump by applying an upward force to the Rigidbody2D.
    /// Handles consumption of air jumps and resets jump timers.
    /// </summary>
    private void ExecuteJump()
    {
        // If we are currently not grounded and have no coyote time left, it must be an air jump.
        bool isAirJump = !isGrounded && timeSinceGrounded <= 0;

        if (isAirJump)
        {
            if (currentAirJumps > 0)
            {
                currentAirJumps--;
            }
            else
            {
                // No air jumps left, so we can't jump.
                jumpBufferTimer = 0; // Clear buffer so it doesn't trigger repeatedly
                return;
            }
        }

        // Reset vertical velocity to ensure consistent jump height, especially after falling.
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        // Apply initial jump force
        rb.AddForce(Vector2.up * initialJumpForce, ForceMode2D.Impulse);

        // Reset jump state timers/flags
        jumpBufferTimer = 0; // Consume the buffered jump
        timeSinceGrounded = 0; // Consume coyote time
        jumpHoldTimer = 0; // Start variable jump timer from zero
        hasJumpedThisPress = true; // Mark that a jump has initiated from this press

        // Optionally, reset isGrounded to false immediately for more responsive air-time checks,
        // though FixedUpdate's CheckGrounded will update it next frame.
        // isGrounded = false;
    }

    /// <summary>
    /// Applies custom gravity based on the character's vertical velocity.
    /// Makes falling faster and controls jump apex for snappier feel.
    /// </summary>
    private void ApplyCustomGravity()
    {
        if (rb.velocity.y < 0) // Falling
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !isJumpButtonHeld) // Ascending but jump button released
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (jumpApexGravityMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && isJumpButtonHeld && hasJumpedThisPress) // Variable jump while holding
        {
            // Apply additional force while jump button is held for variable jump height
            float additionalForce = variableJumpMultiplier * Time.fixedDeltaTime;
            // Ensure we don't exceed max jump hold time
            if (jumpHoldTimer < maxJumpHoldTime)
            {
                rb.AddForce(Vector2.up * additionalForce, ForceMode2D.Force);
            }
        }
    }

    // --- Gizmos for Editor Visualization ---

    void OnDrawGizmos()
    {
        // Draw the ground check circle in the editor
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector2 position = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(position, groundCheckRadius);
    }
}
```

---

### **2. Setup in Unity Editor**

1.  **Create a Player GameObject:**
    *   In your scene, create an empty GameObject (e.g., `Player`).
    *   Add a `Sprite Renderer` component to give it a visual representation (e.g., a simple square sprite).
    *   Add a `Box Collider 2D` or `Capsule Collider 2D` to it. Adjust its size to fit your sprite.
    *   Add a `Rigidbody2D` component. Set its `Body Type` to `Dynamic`, and ensure `Gravity Scale` is `1`. Freeze `Rotation Z` if you don't want your player to rotate.

2.  **Create Ground/Platform GameObject:**
    *   Create another GameObject (e.g., `Ground`).
    *   Add a `Sprite Renderer` and a `Box Collider 2D` to it. Make it large enough to act as a floor.

3.  **Assign Layers:**
    *   Go to `Edit > Project Settings > Tags and Layers`.
    *   Add a new `Layer` (e.g., `Ground`).
    *   Select your `Ground` GameObject in the scene and assign it to the newly created `Ground` layer.

4.  **Attach and Configure `PrecisionJumpingSystem`:**
    *   Drag and drop the `PrecisionJumpingSystem` script onto your `Player` GameObject.
    *   In the Inspector, you'll see all the configurable parameters:
        *   **Jump Properties:** Adjust `initialJumpForce`, `variableJumpMultiplier`, `maxJumpHoldTime` for jump height and variable jump feel. Adjust `airJumpsAllowed` (e.g., `1` for double jump). Fine-tune `fallGravityMultiplier` and `jumpApexGravityMultiplier` for fall speed and jump arc.
        *   **Ground Check:** Adjust `groundCheckRadius` and `groundCheckOffset` to match your player's collider size and position.
        *   **Ground Check Layer:** Select the `Ground` layer you created earlier from the dropdown. This is critical for the `isGrounded` check to work.
        *   **Precision Mechanics:** Adjust `coyoteTimeDuration` and `jumpBufferDuration` to your preference.

5.  **Test:**
    *   Run the game.
    *   Press and hold the `Spacebar` (or your configured "Jump" input) to jump. You should observe:
        *   A short press results in a small hop.
        *   A longer press results in a higher jump, up to `maxJumpHoldTime`.
        *   You can jump for a brief moment after walking off a platform (coyote time).
        *   If you press jump just before landing, it should execute automatically on touchdown (jump buffering).
        *   If `airJumpsAllowed` is greater than 0, you can press jump again in the air for an additional jump.

---

This setup provides a robust and highly configurable precision jumping system, ready to be integrated into your Unity projects and tailored to the specific needs of your game's movement mechanics.