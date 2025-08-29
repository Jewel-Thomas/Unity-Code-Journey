// Unity Design Pattern Example: AnimationControllerPattern
// This script demonstrates the AnimationControllerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a common and highly effective architectural approach in Unity for managing character animations, which we'll refer to as the "Animation Controller Pattern." While not a formal Gang of Four design pattern, it's a vital component pattern for creating clean, maintainable, and scalable animation systems in games.

---

### Understanding the Animation Controller Pattern in Unity

**Goal:** To create a dedicated script that acts as an abstraction layer between your game logic (e.g., movement, combat, AI) and Unity's Mecanim Animator component.

**Why use this pattern?**

1.  **Separation of Concerns:** Your character's movement script shouldn't need to know the specific string names of your Animator parameters (e.g., "IsWalking", "SpeedMultiplier", "JumpTrigger"). It only needs to know "the character is walking" or "the character is jumping." The Animation Controller translates these high-level commands into low-level Animator parameter manipulations.
2.  **Encapsulation:** All logic related to the Mecanim Animator is centralized in one script. If you decide to rename an Animator parameter, change blend tree logic, or add new animation states, you only need to modify this one Animation Controller script, not every script that interacts with the character.
3.  **Maintainability and Readability:** Game logic scripts become cleaner and easier to read, as they call semantic methods (e.g., `animatorController.SetMovement(true, 5f)`) instead of direct `animator.SetBool("IsMoving", true)` calls.
4.  **Reusability:** A well-designed Animation Controller can often be reused or easily adapted for different character types, provided their Animator Controllers share a similar set of core parameters.
5.  **Robustness:** Reduces the chance of errors due to typos in string-based Animator parameter names.

**How it works:**

*   **`CharacterAnimatorController` (The Pattern Implementation):** This script requires an `Animator` component. It exposes public methods (e.g., `SetMovement`, `TriggerJump`, `TriggerAttack`) that represent high-level character actions. Internally, these methods call `Animator.SetBool`, `Animator.SetFloat`, `Animator.SetTrigger` using predefined constant string names for parameters.
*   **`CharacterMovementExample` (Example Consumer):** This script represents your game logic (movement, input handling, AI). Instead of directly interacting with `Animator`, it gets a reference to `CharacterAnimatorController` and calls its public methods.

---

### Unity Setup Guide (Crucial for the example to work)

1.  **Create a new Unity project** or open an existing one.
2.  **Create C# Scripts:**
    *   Create a new C# script named `CharacterAnimatorController` and paste the first code block into it.
    *   Create another C# script named `CharacterMovementExample` and paste the second code block into it.
3.  **Create a Player GameObject:**
    *   In the Hierarchy, right-click -> 3D Object -> Cube. Rename it "Player".
    *   Add a `Rigidbody` component to the "Player" (Component -> Physics -> Rigidbody). Make sure "Freeze Rotation" on X, Y, Z axes is checked in the Rigidbody settings to prevent the cube from toppling.
4.  **Create an Animator Controller Asset:**
    *   In the Project window, right-click -> Create -> Animator Controller. Name it "PlayerAnimatorController".
5.  **Assign Animator Controller to Player:**
    *   Select your "Player" GameObject.
    *   Add an `Animator` component (Component -> Animation -> Animator).
    *   Drag your "PlayerAnimatorController" asset from the Project window into the `Controller` field of the Animator component.
6.  **Configure Animator Parameters:**
    *   Open the Animator window (Window -> Animation -> Animator).
    *   In the `Parameters` tab (usually top-left of the Animator window), click the `+` button and add the following parameters. **Ensure the names are exact matches (case-sensitive)!**
        *   `Bool`: `IsMoving`
        *   `Float`: `Speed`
        *   `Trigger`: `Jump`
        *   `Trigger`: `Attack`
        *   `Bool`: `IsDead`
7.  **Create Basic Animator States and Transitions:**
    *   In the Animator window, right-click -> Create State -> Empty. Name it "Idle".
    *   Create another Empty State: "Walk_Run".
    *   Create another Empty State: "Jump".
    *   Create another Empty State: "Attack".
    *   Create another Empty State: "Die".
    *   **Drag placeholder animations** into these states if you have them (e.g., from Mixamo or Unity Assets). If not, the states will just be "empty" and transitions will still work, but you won't see visual changes.
    *   **Create Transitions:**
        *   Right-click "Idle" -> Make Transition -> "Walk_Run".
            *   Select this transition. In the Inspector, uncheck `Has Exit Time`. Add Condition: `IsMoving` is `true`.
        *   Right-click "Walk_Run" -> Make Transition -> "Idle".
            *   Select this transition. In the Inspector, uncheck `Has Exit Time`. Add Condition: `IsMoving` is `false`.
        *   Right-click "Any State" -> Make Transition -> "Jump".
            *   Select this transition. In the Inspector, uncheck `Has Exit Time`. Add Condition: `Jump` trigger.
            *   Add a transition from "Jump" back to "Idle" (with `Has Exit Time` checked, or a `JumpFinished` trigger if you wanted more control). For simplicity, let's omit the return for this example, or just use `Has Exit Time` on the Jump -> Idle transition.
        *   Right-click "Any State" -> Make Transition -> "Attack".
            *   Select this transition. In the Inspector, uncheck `Has Exit Time`. Add Condition: `Attack` trigger.
            *   Add a transition from "Attack" back to "Idle" (with `Has Exit Time` checked).
        *   Right-click "Any State" -> Make Transition -> "Die".
            *   Select this transition. In the Inspector, uncheck `Has Exit Time`. Add Condition: `IsDead` is `true`.
8.  **Add Scripts to Player:**
    *   Select your "Player" GameObject.
    *   Drag the `CharacterAnimatorController` script from your Project window onto the "Player".
    *   Drag the `CharacterMovementExample` script onto the "Player".
9.  **Run the Scene!**
    *   Use **WASD** to move (Shift to run).
    *   Press **Space** to jump.
    *   Click **Left Mouse Button** to attack.
    *   Press **K** to trigger death.
    *   Press **L** to revive.
    *   Observe the Debug Logs in the Console showing the Animator parameter changes.

---

### 1. `CharacterAnimatorController.cs` (The Pattern Implementation)

```csharp
using UnityEngine;
using System.Collections; // Often useful for coroutines, though not strictly needed here for the core pattern.

/// <summary>
/// --- Design Pattern: Animation Controller Pattern ---
///
/// This script demonstrates a common and practical "Animation Controller Pattern"
/// in Unity. Its primary purpose is to act as an abstraction layer over Unity's
/// Mecanim Animator component.
///
/// **Key Principles:**
/// 1.  **Separation of Concerns:** Keeps animation logic distinct from movement,
///     combat, or AI logic.
/// 2.  **Encapsulation:** Hides the specific Mecanim parameter names and their
///     manipulation from other scripts.
/// 3.  **Semantic Interface:** Provides high-level, human-readable methods (e.g.,
///     `SetMovement`, `TriggerJump`) that describe *what* the character is doing,
///     rather than *how* the Animator should be updated.
///
/// **How to Use:**
/// -   Attach this script to any GameObject that has an `Animator` component.
/// -   Configure your `Animator Controller` asset with the parameters defined
///     by the `PARAM_` constants below (e.g., "IsMoving", "Speed", "Jump").
/// -   Other scripts (like `CharacterMovementExample`) will get a reference to
///     this `CharacterAnimatorController` and call its public methods
///     to control animations, instead of directly interacting with `Animator`.
/// </summary>
[RequireComponent(typeof(Animator))] // Ensures an Animator component exists on this GameObject.
public class CharacterAnimatorController : MonoBehaviour
{
    // --- Public Constants for Animator Parameter Names ---
    // Using constants helps prevent typos in parameter names and makes it easier
    // to update them if they change in the Animator Controller.
    public const string PARAM_IS_MOVING    = "IsMoving";
    public const string PARAM_SPEED        = "Speed";
    public const string PARAM_JUMP_TRIGGER = "Jump";
    public const string PARAM_ATTACK_TRIGGER = "Attack";
    public const string PARAM_IS_DEAD      = "IsDead";

    // A cached reference to the Animator component on this GameObject.
    private Animator _animator;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to get the Animator component reference and perform initial setup.
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("CharacterAnimatorController requires an Animator component on the same GameObject.", this);
            enabled = false; // Disable this script if no Animator is found.
        }
        else
        {
            Debug.Log($"<color=cyan>CharacterAnimatorController initialized for: {gameObject.name}</color>", this);
        }
    }

    /// <summary>
    /// Sets the character's movement animation state. This method handles both
    /// idle and walking/running states by adjusting the `IsMoving` boolean
    /// and `Speed` float parameters in the Animator.
    /// </summary>
    /// <param name="isMoving">True if the character is currently moving, false for idle.</param>
    /// <param name="speed">The current movement speed. Used for blend trees (e.g., walk vs. run).</param>
    public void SetMovement(bool isMoving, float speed)
    {
        if (_animator == null || !_animator.enabled) return;

        // Set the boolean parameter for movement state.
        _animator.SetBool(PARAM_IS_MOVING, isMoving);

        // Set the float parameter for speed. This is typically used in a Blend Tree
        // to transition smoothly between walk and run animations based on speed.
        _animator.SetFloat(PARAM_SPEED, speed);

        // Debug.Log($"Animator: SetMovement(IsMoving={isMoving}, Speed={speed})");
    }

    /// <summary>
    /// Triggers the character's jump animation.
    /// This method abstracts away setting the 'Jump' trigger parameter.
    /// Triggers are one-shot events that immediately transition to a state.
    /// </summary>
    public void TriggerJump()
    {
        if (_animator == null || !_animator.enabled) return;

        _animator.SetTrigger(PARAM_JUMP_TRIGGER);
        Debug.Log("Animator: Triggered Jump animation.");
    }

    /// <summary>
    /// Triggers the character's attack animation.
    /// This method abstracts away setting the 'Attack' trigger parameter.
    /// </summary>
    public void TriggerAttack()
    {
        if (_animator == null || !_animator.enabled) return;

        _animator.SetTrigger(PARAM_ATTACK_TRIGGER);
        Debug.Log("Animator: Triggered Attack animation.");
    }

    /// <summary>
    /// Sets the character's death state. This typically transitions the Animator
    /// into a death animation and can prevent other animations from playing.
    /// </summary>
    /// <param name="isDead">True to set the character to a dead state, false to revive.</param>
    public void SetDead(bool isDead)
    {
        if (_animator == null || !_animator.enabled) return;

        _animator.SetBool(PARAM_IS_DEAD, isDead);
        Debug.Log($"Animator: Set IsDead to {isDead}.");
    }

    /// <summary>
    /// Resets all Animator parameters to a neutral or default state.
    /// This is useful for re-initializing a character (e.g., after being pooled,
    /// or starting a new round).
    /// </summary>
    public void ResetAllParameters()
    {
        if (_animator == null || !_animator.enabled) return;

        Debug.Log("<color=orange>Animator: Resetting all parameters to default.</color>");
        _animator.SetBool(PARAM_IS_MOVING, false);
        _animator.SetFloat(PARAM_SPEED, 0f);
        // Triggers are one-shot and automatically reset, but if a trigger was set
        // and hasn't fired yet, ResetTrigger will clear it.
        _animator.ResetTrigger(PARAM_JUMP_TRIGGER);
        _animator.ResetTrigger(PARAM_ATTACK_TRIGGER);
        _animator.SetBool(PARAM_IS_DEAD, false);
    }
}
```

---

### 2. `CharacterMovementExample.cs` (Example Usage of the Pattern)

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// This script serves as an example consumer of the `CharacterAnimatorController`.
/// It handles basic character movement, jumping, and attacking based on player input.
///
/// **Key Demonstration:**
/// Instead of directly accessing `GetComponent<Animator>().SetBool(...)`,
/// this script interacts *only* with the high-level, semantic methods provided
/// by the `CharacterAnimatorController` (e.g., `_animatorController.SetMovement(...)`).
/// This clearly shows the separation of concerns and the benefits of the pattern.
///
/// **Prerequisites:**
/// -   The GameObject must have a `CharacterAnimatorController` component.
/// -   The GameObject should have a `Rigidbody` for physics-based movement and jumping.
/// </summary>
[RequireComponent(typeof(CharacterAnimatorController))] // Ensures the Animator Controller is present.
public class CharacterMovementExample : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _runSpeed = 6f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _groundCheckDistance = 0.6f; // How far down to check for ground
    [SerializeField] private LayerMask _groundLayer; // Define what layers are considered ground

    // Reference to our CharacterAnimatorController. This is the core of the pattern!
    private CharacterAnimatorController _animatorController;
    private Rigidbody _rb;
    private bool _isGrounded; // To control jumping

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets references to required components and initializes settings.
    /// </summary>
    private void Awake()
    {
        _animatorController = GetComponent<CharacterAnimatorController>();
        _rb = GetComponent<Rigidbody>();

        if (_animatorController == null)
        {
            Debug.LogError("CharacterMovementExample requires a CharacterAnimatorController on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (_rb == null)
        {
            // Add a Rigidbody if not present, and configure it.
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.freezeRotation = true; // Prevents the character from toppling over
            Debug.LogWarning("Rigidbody added to CharacterMovementExample GameObject for physics operations.", this);
        }

        // Set default ground layer if not specified
        if (_groundLayer.value == 0)
        {
            _groundLayer = LayerMask.GetMask("Default"); // Assuming 'Default' layer is ground
            Debug.LogWarning("Ground Layer not set. Using 'Default' layer for ground checks. Consider assigning a specific layer.", this);
        }

        Debug.Log($"<color=cyan>CharacterMovementExample initialized for: {gameObject.name}</color>", this);
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate, ideal for physics operations.
    /// Used here for ground checking.
    /// </summary>
    private void FixedUpdate()
    {
        CheckGrounded();
    }

    /// <summary>
    /// Update is called once per frame.
    /// Handles player input for movement, jumping, and attacking, and translates
    /// these actions into calls to the CharacterAnimatorController.
    /// </summary>
    private void Update()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleAttackInput();
        HandleDeathInput(); // For demonstration purposes
    }

    /// <summary>
    /// Performs a raycast downwards to check if the character is currently on the ground.
    /// </summary>
    private void CheckGrounded()
    {
        // Adjust the origin slightly upwards to avoid hitting self immediately
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        _isGrounded = Physics.Raycast(rayOrigin, Vector3.down, _groundCheckDistance, _groundLayer);

        // Optional: Draw a debug ray to visualize the ground check
        Debug.DrawRay(rayOrigin, Vector3.down * _groundCheckDistance, _isGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// Handles player input for horizontal movement and updates the animator via the controller.
    /// </summary>
    private void HandleMovementInput()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement direction relative to the camera or world
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        bool isMoving = moveDirection.magnitude > 0.1f; // Check if there's significant input

        float currentSpeed = 0f;
        if (isMoving)
        {
            if (Input.GetKey(KeyCode.LeftShift)) // Running
            {
                currentSpeed = _runSpeed;
            }
            else // Walking
            {
                currentSpeed = _walkSpeed;
            }
            // Apply movement
            transform.position += moveDirection * currentSpeed * Time.deltaTime;

            // Optional: Rotate character to face movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        // Inform the CharacterAnimatorController about the current movement state.
        // This is where the pattern shines: simple, high-level command!
        _animatorController.SetMovement(isMoving, currentSpeed);
    }

    /// <summary>
    /// Handles player input for jumping, ensuring the character is grounded first.
    /// </summary>
    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            // Tell the animator controller to trigger the jump animation.
            _animatorController.TriggerJump();
            Debug.Log("Jump initiated.");
        }
    }

    /// <summary>
    /// Handles player input for attacking.
    /// </summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Tell the animator controller to trigger the attack animation.
            _animatorController.TriggerAttack();
            Debug.Log("Attack initiated.");
        }
    }

    /// <summary>
    /// Handles a specific input to trigger the death animation for demonstration.
    /// </summary>
    private void HandleDeathInput()
    {
        // Press 'K' to simulate death
        if (Input.GetKeyDown(KeyCode.K))
        {
            // Inform the animator controller that the character is dead.
            _animatorController.SetDead(true);
            // Optionally disable movement or other actions upon death
            enabled = false;
            _rb.velocity = Vector3.zero; // Stop movement
            Debug.Log("<color=red>Character died! Disabling movement script.</color>");
        }
        // Press 'L' to revive (for testing)
        else if (Input.GetKeyDown(KeyCode.L))
        {
            // Inform the animator controller that the character is no longer dead.
            _animatorController.SetDead(false);
            _animatorController.ResetAllParameters(); // Reset other states too
            enabled = true; // Re-enable movement
            Debug.Log("<color=green>Character revived! Re-enabling movement script.</color>");
        }
    }
}
```