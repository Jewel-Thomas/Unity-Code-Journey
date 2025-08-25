// Unity Design Pattern Example: State
// This script demonstrates the State pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'State' design pattern allows an object to alter its behavior when its internal state changes. The object will appear to change its class. This pattern is particularly useful in game development for managing character behaviors, AI states, or UI states, where an entity's actions depend heavily on its current mode or condition.

**Key Components of the State Pattern:**

1.  **Context (PlayerCharacterController):** This is the object whose behavior changes depending on its state. It holds a reference to the current concrete state object and delegates state-specific requests to it. It also provides the interface for changing states.
2.  **State Interface (IPlayerState):** Defines an interface for encapsulating the behavior associated with a particular state of the Context. All concrete state classes will implement this interface.
3.  **Concrete States (IdleState, WalkState, JumpState, AttackState):** Each concrete state implements the State interface and provides its own specific behavior for the methods defined in the interface. They also define the conditions under which the Context transitions to another state.

---

### C# Unity Example: Player Character States

This example demonstrates a `PlayerCharacterController` that can be in one of several states: `Idle`, `Walk`, `Jump`, and `Attack`. The character's behavior (e.g., movement, input handling) changes based on its current state.

**How to Use in Unity:**

1.  Create a new C# script named `PlayerCharacterController`.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene, name it "Player" (or similar).
4.  Add a `Rigidbody` component to the "Player" GameObject. Ensure "Is Kinematic" is **NOT** checked, and "Use Gravity" **IS** checked.
5.  Add a `Capsule Collider` (or any collider) to the "Player" GameObject.
6.  Attach the `PlayerCharacterController` script to the "Player" GameObject.
7.  Run the scene and use the specified keys to observe state changes in the Console.

**Controls:**
*   **W / A / S / D:** Move the character (enters Walk state).
*   **Spacebar:** Jump (enters Jump state).
*   **E:** Attack (enters Attack state).

---

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator and Coroutines

/// <summary>
/// The Context class: PlayerCharacterController.
/// This class holds the current state and delegates state-specific behavior to it.
/// It also provides methods for states to request state changes.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present for physics
public class PlayerCharacterController : MonoBehaviour
{
    // --- Context Fields ---
    [Header("Player Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer; // Layer(s) considered ground for jumping
    [SerializeField] private Transform groundCheck; // Position for ground checking
    [SerializeField] private float groundCheckRadius = 0.2f; // Radius for ground check sphere

    // The current state of the player character
    private IPlayerState currentState;
    private Rigidbody rb;
    private Coroutine attackCoroutine; // To manage the attack duration

    // --- Context Initialization ---
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerCharacterController. Please add a Rigidbody component.");
            enabled = false; // Disable script if no Rigidbody
            return;
        }

        // Set the initial state to Idle
        ChangeState(new IdleState());
    }

    void Start()
    {
        // Ensure the initial state's Enter method is called
        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    // --- Context Update Loop ---
    void Update()
    {
        // Delegate execution to the current state
        if (currentState != null)
        {
            currentState.ExecuteState(this);
        }
    }

    // --- Context State Management ---

    /// <summary>
    /// Changes the player's current state.
    /// This method is called by concrete state classes when a transition is needed.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public void ChangeState(IPlayerState newState)
    {
        if (currentState != null)
        {
            // First, exit the current state
            currentState.ExitState(this);
        }

        // Set the new state
        currentState = newState;
        Debug.Log($"<color=cyan>Player State Changed To: {currentState.GetType().Name}</color>");

        // Then, enter the new state
        if (currentState != null)
        {
            currentState.EnterState(this);
        }
    }

    // --- Context Helper Methods (Accessed by states) ---

    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public float GetJumpForce()
    {
        return jumpForce;
    }

    /// <summary>
    /// Performs a simple ground check.
    /// </summary>
    /// <returns>True if the player is currently grounded, false otherwise.</returns>
    public bool IsGrounded()
    {
        // Check if a sphere at groundCheck position overlaps with anything on the groundLayer
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Starts a coroutine from the context. Useful for states needing timed actions.
    /// </summary>
    /// <param name="coroutine">The IEnumerator coroutine to start.</param>
    public Coroutine StartPlayerCoroutine(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }

    /// <summary>
    /// Stops a previously started coroutine.
    /// </summary>
    /// <param name="coroutine">The coroutine to stop.</param>
    public void StopPlayerCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }

    /// <summary>
    /// Allows the AttackState to set/clear its coroutine reference in the context.
    /// </summary>
    public void SetAttackCoroutine(Coroutine coroutine)
    {
        attackCoroutine = coroutine;
    }

    /// <summary>
    /// Gets the current attack coroutine.
    /// </summary>
    public Coroutine GetAttackCoroutine()
    {
        return attackCoroutine;
    }

    // Optional: Draw ground check gizmo in editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

/// <summary>
/// The State Interface.
/// Defines the common methods that all concrete states must implement.
/// These methods are called by the Context (PlayerCharacterController).
/// </summary>
public interface IPlayerState
{
    /// <summary>
    /// Called when the player enters this state.
    /// Use this for initialization, starting animations, or one-time actions.
    /// </summary>
    /// <param name="player">A reference to the PlayerCharacterController (Context).</param>
    void EnterState(PlayerCharacterController player);

    /// <summary>
    /// Called every frame while the player is in this state.
    /// Use this for continuous actions, input checking, and state transition logic.
    /// </summary>
    /// <param name="player">A reference to the PlayerCharacterController (Context).</param>
    void ExecuteState(PlayerCharacterController player);

    /// <summary>
    /// Called when the player exits this state.
    /// Use this for cleanup, stopping animations, or resetting values.
    /// </summary>
    /// <param name="player">A reference to the PlayerCharacterController (Context).</param>
    void ExitState(PlayerCharacterController player);
}

/// <summary>
/// Concrete State: Idle State.
/// The player is standing still, awaiting input.
/// </summary>
public class IdleState : IPlayerState
{
    public void EnterState(PlayerCharacterController player)
    {
        Debug.Log("Player is now Idle. (Press W/A/S/D to walk, Space to jump, E to attack)");
        // Stop any residual movement if coming from a movement state
        player.GetRigidbody().velocity = Vector3.zero;
        player.GetRigidbody().angularVelocity = Vector3.zero;
    }

    public void ExecuteState(PlayerCharacterController player)
    {
        // Check for movement input
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            player.ChangeState(new WalkState());
            return;
        }

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && player.IsGrounded())
        {
            player.ChangeState(new JumpState());
            return;
        }

        // Check for attack input
        if (Input.GetKeyDown(KeyCode.E))
        {
            player.ChangeState(new AttackState());
            return;
        }

        // No continuous actions in Idle state
    }

    public void ExitState(PlayerCharacterController player)
    {
        Debug.Log("Exiting Idle State.");
    }
}

/// <summary>
/// Concrete State: Walk State.
/// The player is moving based on input.
/// </summary>
public class WalkState : IPlayerState
{
    public void EnterState(PlayerCharacterController player)
    {
        Debug.Log("Player is now Walking. (Release keys to idle, Space to jump, E to attack)");
    }

    public void ExecuteState(PlayerCharacterController player)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Calculate movement direction
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // Apply movement velocity
        if (moveDirection.magnitude > 0.1f)
        {
            player.GetRigidbody().velocity = new Vector3(
                moveDirection.x * player.GetMoveSpeed(),
                player.GetRigidbody().velocity.y, // Maintain current Y velocity for gravity/jumps
                moveDirection.z * player.GetMoveSpeed()
            );

            // Optional: Rotate player to face movement direction
            if (moveDirection != Vector3.zero)
            {
                player.transform.forward = moveDirection;
            }
        }
        else
        {
            // If no movement input, transition back to Idle
            player.ChangeState(new IdleState());
            return;
        }

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && player.IsGrounded())
        {
            player.ChangeState(new JumpState());
            return;
        }

        // Check for attack input
        if (Input.GetKeyDown(KeyCode.E))
        {
            player.ChangeState(new AttackState());
            return;
        }
    }

    public void ExitState(PlayerCharacterController player)
    {
        Debug.Log("Exiting Walk State.");
        // Stop horizontal/vertical movement, but keep gravity effect
        player.GetRigidbody().velocity = new Vector3(0, player.GetRigidbody().velocity.y, 0);
    }
}

/// <summary>
/// Concrete State: Jump State.
/// The player is performing a jump action.
/// </summary>
public class JumpState : IPlayerState
{
    public void EnterState(PlayerCharacterController player)
    {
        Debug.Log("Player is now Jumping!");
        // Apply jump force immediately
        player.GetRigidbody().AddForce(Vector3.up * player.GetJumpForce(), ForceMode.Impulse);
    }

    public void ExecuteState(PlayerCharacterController player)
    {
        // Once the player lands, transition to Idle or Walk based on input
        if (player.IsGrounded())
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            {
                player.ChangeState(new WalkState());
            }
            else
            {
                player.ChangeState(new IdleState());
            }
        }
        // Even in air, allow attacking (e.g., jump attack)
        else if (Input.GetKeyDown(KeyCode.E))
        {
            player.ChangeState(new AttackState());
            // Note: If you want to return to JumpState after attack, AttackState needs to know previous state
            // For simplicity, this example just goes to Idle/Walk after attack finishes
        }
    }

    public void ExitState(PlayerCharacterController player)
    {
        Debug.Log("Exiting Jump State.");
    }
}

/// <summary>
/// Concrete State: Attack State.
/// The player is performing an attack animation/action.
/// This state has a duration and transitions back to a movement state afterwards.
/// </summary>
public class AttackState : IPlayerState
{
    private const float ATTACK_DURATION = 0.5f; // How long the attack animation/action lasts
    private Coroutine attackRoutine; // Reference to the coroutine managing attack duration

    public void EnterState(PlayerCharacterController player)
    {
        Debug.Log($"Player is Attacking! ({ATTACK_DURATION}s duration)");
        // Stop any ongoing attack coroutine to prevent issues if attacked mid-attack
        player.StopPlayerCoroutine(player.GetAttackCoroutine());

        // Start a coroutine to manage the attack duration
        attackRoutine = player.StartPlayerCoroutine(AttackDuration(player));
        player.SetAttackCoroutine(attackRoutine); // Store coroutine reference in context
    }

    public void ExecuteState(PlayerCharacterController player)
    {
        // While attacking, player might not be able to move or jump
        // For this example, we'll block other inputs during attack
        // If movement/jump was allowed, this logic would be more complex
    }

    public void ExitState(PlayerCharacterController player)
    {
        Debug.Log("Exiting Attack State.");
        // Stop the attack coroutine if it's still running (e.g., if interrupted)
        player.StopPlayerCoroutine(attackRoutine);
        player.SetAttackCoroutine(null); // Clear the reference
    }

    // Coroutine to handle the duration of the attack
    private IEnumerator AttackDuration(PlayerCharacterController player)
    {
        // Simulate attack animation or logic
        Debug.Log("Performing attack action...");
        yield return new WaitForSeconds(ATTACK_DURATION); // Wait for the attack to finish

        Debug.Log("Attack finished.");

        // After attack, transition back to Idle or Walk based on current input and ground status
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (player.IsGrounded())
        {
            if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            {
                player.ChangeState(new WalkState());
            }
            else
            {
                player.ChangeState(new IdleState());
            }
        }
        else // If still in air after attack (e.g., jump attack), transition back to JumpState if it makes sense
        {
            // For simplicity, just going to idle if in air after attack,
            // but a more robust system might have an 'AirborneState' to return to.
            player.ChangeState(new IdleState());
        }
    }
}
```