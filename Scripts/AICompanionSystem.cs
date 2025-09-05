// Unity Design Pattern Example: AICompanionSystem
// This script demonstrates the AICompanionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AI Companion System' design pattern, while not one of the classic Gang of Four patterns, is a common and highly effective way to manage the complex behaviors of an AI character that assists or interacts with the player in a game. It often leverages the **State Pattern** internally to organize different companion behaviors.

**Core Idea:**
The system provides a central manager for the AI companion that can switch between various predefined "states" or "behaviors." Each state encapsulates a specific set of actions and rules for transitioning to other states. This makes the companion's AI modular, extensible, and easier to debug.

**Key Components Demonstrated:**

1.  **`AICompanionSystem` (The Context/Manager):**
    *   This is the main `MonoBehaviour` script attached to your AI companion GameObject.
    *   It holds references to essential components (like `NavMeshAgent`, `Animator`).
    *   It maintains the `currentState` of the companion.
    *   It has a `ChangeState()` method to smoothly transition between behaviors.
    *   It provides helper methods (e.g., `SetDestination`, `StopMovement`, `SetAnimationBool`) that states can use to control the companion.

2.  **`ICompanionState` (The State Interface):**
    *   An interface that defines the common contract for all companion behaviors.
    *   It specifies `Enter()`, `Execute()`, and `Exit()` methods that each concrete state must implement.
        *   `Enter()`: Called once when the companion transitions *into* this state. Used for initialization (e.g., setting animations, stopping movement).
        *   `Execute()`: Called every `Update()` frame while the companion is *in* this state. Contains the core logic for the behavior and conditions for state transitions.
        *   `Exit()`: Called once when the companion transitions *out of* this state. Used for cleanup (e.g., stopping animations, clearing targets).

3.  **Concrete Companion States (The Concrete States):**
    *   Individual classes (nested within `AICompanionSystem` in this example for a single-file solution) that implement `ICompanionState`.
    *   Each class represents a distinct behavior:
        *   `CompanionIdleState`: Companion stands still, observing.
        *   `CompanionFollowState`: Companion moves to follow the player.
        *   `CompanionAttackState`: Companion targets and attacks an enemy.
        *   `CompanionGuardState`: Companion stays in a designated area.

---

### AICompanionSystem.cs (Complete Unity Script)

```csharp
// AICompanionSystem.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections; // For potential coroutines (not strictly used here, but common)
using System.Collections.Generic; // For lists/dictionaries (not strictly used here, but common)

/// <summary>
/// AI Companion System Design Pattern Example.
///
/// This script demonstrates a practical implementation of an AI Companion System
/// in Unity, heavily leveraging the State Pattern for modular and extensible
/// companion behaviors.
///
/// Key Features:
/// -   **Centralized Companion Logic:** The `AICompanionSystem` class manages
///     the companion's overall state and provides access to its components.
/// -   **State-Based Behavior:** Companion's actions are dictated by its current
///     `ICompanionState` (e.g., Idle, Follow, Attack, Guard).
/// -   **Clear State Transitions:** The `ChangeState()` method handles entering
///     and exiting states gracefully.
/// -   **Modularity:** New behaviors can be added by creating new classes that
///     implement `ICompanionState` without modifying existing state logic.
/// -   **Animation Integration:** Basic Animator control for 'IsMoving', 'IsAttacking'
///     boolean parameters and an 'Attack' trigger.
/// -   **NavMeshAgent Integration:** Uses Unity's NavMeshAgent for movement and pathfinding.
///
/// How to Use This Example:
/// 1.  **Create a Companion GameObject:** In your Unity scene, create an empty
///     GameObject. Name it something like "MyCompanion".
/// 2.  **Attach Script:** Attach this `AICompanionSystem.cs` script to "MyCompanion".
/// 3.  **Add NavMeshAgent:** Add a `NavMeshAgent` component to "MyCompanion"
///     (the script automatically requires it, but you'll need to configure it).
///     -   Adjust "Speed", "Acceleration", "Angular Speed", and "Stopping Distance"
///         on the NavMeshAgent if desired. The script's `moveSpeed` will override
///         the NavMeshAgent's speed during `Awake()`.
/// 4.  **Add Animator (Optional):** If you want animations, add an `Animator`
///     component to "MyCompanion".
///     -   For basic animation feedback, create an Animator Controller and add
///         a `bool` parameter named "IsMoving" and a `Trigger` parameter named "Attack".
///         You can also add a `bool` parameter named "IsAttacking" if your attack
///         animation needs a sustained boolean.
///         -   Transition from Idle to Walk/Run when "IsMoving" is true.
///         -   Transition from Walk/Run to Idle when "IsMoving" is false.
///         -   Transition from any state to Attack when "Attack" trigger is set.
///         -   You might have an "Idle" animation for when `IsMoving` is false.
/// 5.  **Bake NavMesh:** Ensure you have a floor or terrain in your scene and
///     bake a NavMesh (Window > AI > Navigation > Bake tab > Click "Bake").
///     This is crucial for the `NavMeshAgent` to work.
/// 6.  **Create Player GameObject:** Create a simple 3D object (e.g., a Capsule)
///     to represent your player. Name it "Player".
/// 7.  **Create Enemy GameObject:** Create another simple 3D object (e.g., a Cube)
///     to represent an enemy. Name it "Enemy".
/// 8.  **Assign References in Inspector:**
///     -   Select "MyCompanion" in the Hierarchy.
///     -   Drag your "Player" GameObject into the `Player Transform` field.
///     -   Drag your "Enemy" GameObject into the `Target Enemy` field.
///     -   Adjust `Move Speed`, `Follow Stop Distance`, `Attack Stop Distance`,
///         and `Guard Radius` as needed.
/// 9.  **Run the Scene:**
///     -   The companion will start in the `Idle` state.
///     -   Press `1` on your keyboard to make the companion `Idle`.
///     -   Press `2` on your keyboard to make the companion `Follow` the player.
///     -   Press `3` on your keyboard to make the companion `Attack` the "Enemy".
///     -   Press `4` on your keyboard to make the companion `Guard` its current position.
///
/// This example provides a robust foundation. In a real project, you would
/// expand the states with more complex AI logic, integrate actual combat systems,
/// dialogue, visual/audio feedback, and more sophisticated decision-making.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))] // Ensures the companion GameObject has a NavMeshAgent
public class AICompanionSystem : MonoBehaviour
{
    // --- Companion Core Properties ---
    [Header("Companion Core Setup")]
    [Tooltip("The player's transform for the companion to follow.")]
    public Transform playerTransform;
    [Tooltip("The primary enemy target for the companion to engage. Can be dynamically changed.")]
    public Transform targetEnemy;
    [Tooltip("The companion's movement speed.")]
    public float moveSpeed = 3.5f;
    [Tooltip("The distance at which the companion stops when following the player.")]
    public float followStopDistance = 2.0f;
    [Tooltip("The distance at which the companion stops when attacking a target.")]
    public float attackStopDistance = 1.5f;
    [Tooltip("The radius around the guard position the companion will stay within and scan for threats.")]
    public float guardRadius = 10.0f;

    // --- Component References ---
    private NavMeshAgent navMeshAgent;
    private Animator animator; // Optional: for animation control

    // --- State Management ---
    // The current active state that dictates the companion's behavior.
    private ICompanionState currentState;
    public ICompanionState CurrentState => currentState; // Public getter for debugging/inspection

    // --- State Instances ---
    // Pre-instantiate states to avoid repetitive garbage collection.
    // This works well for stateless states. If states needed unique data
    // per instance, they would be created dynamically with 'new'.
    private CompanionIdleState idleState = new CompanionIdleState();
    private CompanionFollowState followState = new CompanionFollowState();
    private CompanionAttackState attackState = new CompanionAttackState();
    private CompanionGuardState guardState = new CompanionGuardState();


    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Get required components from the GameObject
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Animator is optional, will be null if not present

        // Initialize NavMeshAgent properties
        navMeshAgent.speed = moveSpeed;
        // The stoppingDistance will be set by individual states as needed
        navMeshAgent.updateRotation = true; // Let NavMeshAgent handle the companion's rotation

        // Set the initial state of the companion
        ChangeState(idleState);
    }

    void Update()
    {
        // Ensure critical references are not null before proceeding
        if (playerTransform == null)
        {
            Debug.LogWarning("AICompanionSystem: Player Transform is not assigned! Companion cannot follow.", this);
            return; // Skip execution if core dependency is missing
        }

        // Execute the logic defined by the current state for this frame.
        // The '?' (null-conditional operator) prevents errors if currentState is somehow null.
        currentState?.Execute(this);

        // --- Example Player Input for State Transitions ---
        // In a real game, these commands would be part of a more complex
        // input system, UI, or higher-level AI decision-making layer.
        HandlePlayerInput();
    }

    // --- Public Methods for State Transitions ---

    /// <summary>
    /// Core method for changing the companion's state.
    /// Handles exiting the old state and entering the new one.
    /// </summary>
    /// <param name="newState">The new <see cref="ICompanionState"/> to transition to.</param>
    public void ChangeState(ICompanionState newState)
    {
        // If there's an active state, call its Exit method for cleanup
        currentState?.Exit(this);

        // Update to the new state
        currentState = newState;
        Debug.Log($"AICompanion: Transitioning to <color=cyan>{currentState.GetType().Name}</color>");

        // Call the Enter method of the new state for initialization
        currentState.Enter(this);
    }

    // --- Helper Methods for States to Interact with the Companion ---
    // These methods allow concrete states to control the companion without
    // directly accessing its components, promoting encapsulation.

    /// <summary>
    /// Sets the NavMeshAgent's destination and ensures it starts moving.
    /// </summary>
    /// <param name="destination">The target world position for the companion to move to.</param>
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(destination);
        }
        else
        {
            // This warning is crucial for debugging NavMesh issues
            Debug.LogWarning("AICompanion: NavMeshAgent not ready or not on NavMesh! Cannot set destination.", this);
        }
    }

    /// <summary>
    /// Stops the companion's movement immediately.
    /// </summary>
    public void StopMovement()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath(); // Clear any pending path
        }
    }

    /// <summary>
    /// Sets an animation trigger on the companion's animator, if available.
    /// </summary>
    /// <param name="triggerName">The name of the animation trigger.</param>
    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    /// <summary>
    /// Sets an animation boolean parameter on the companion's animator, if available.
    /// </summary>
    /// <param name="boolName">The name of the boolean parameter.</param>
    /// <param name="value">The value to set the boolean parameter to.</param>
    public void SetAnimationBool(string boolName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(boolName, value);
        }
    }

    /// <summary>
    /// Rotates the companion to face a specific target position.
    /// </summary>
    /// <param name="targetPosition">The position to face.</param>
    /// <param name="rotationSpeed">How fast to rotate.</param>
    public void FaceTarget(Vector3 targetPosition, float rotationSpeed = 5f)
    {
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0; // Keep horizontal rotation only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }


    // --- Player Input Handling (for demonstration purposes only) ---
    // This method allows manual control over state transitions via keyboard input.
    private void HandlePlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Press '1' for Idle state
        {
            ChangeState(idleState);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // Press '2' for Follow state
        {
            ChangeState(followState);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) // Press '3' for Attack state
        {
            if (targetEnemy != null) // Only attack if there's a target
            {
                ChangeState(attackState);
            }
            else
            {
                Debug.LogWarning("AICompanionSystem: No Target Enemy assigned for Attack state. Cannot attack!", this);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) // Press '4' for Guard state
        {
            // When entering Guard state, it will store its current position as the guard point.
            // A more advanced system might allow the player to designate a guard point.
            ChangeState(guardState);
        }
    }


    // =========================================================================================
    // --- ICompanionState Interface ---
    // Defines the contract that all concrete companion states must implement.
    // =========================================================================================
    public interface ICompanionState
    {
        /// <summary>
        /// Called once when the companion transitions into this state.
        /// Use for initialization, setting animations, etc.
        /// </summary>
        /// <param name="companion">A reference to the <see cref="AICompanionSystem"/> managing this state.</param>
        void Enter(AICompanionSystem companion);

        /// <summary>
        /// Called every frame while the companion is in this state.
        /// Contains the primary logic for the state's behavior and conditions for transitions.
        /// </summary>
        /// <param name="companion">A reference to the <see cref="AICompanionSystem"/> managing this state.</param>
        void Execute(AICompanionSystem companion);

        /// <summary>
        /// Called once when the companion transitions out of this state.
        /// Use for cleanup, stopping animations, etc.
        /// </summary>
        /// <param name="companion">A reference to the <see cref="AICompanionSystem"/> managing this state.</param>
        void Exit(AICompanionSystem companion);
    }


    // =========================================================================================
    // --- Concrete Companion States ---
    // These classes implement `ICompanionState` and define specific behaviors.
    // They are nested classes for better organization within this single file example.
    // =========================================================================================

    /// <summary>
    /// <see cref="ICompanionState"/> implementation for the companion being idle.
    /// It stops movement and can optionally look around or check for environmental triggers.
    /// </summary>
    public class CompanionIdleState : ICompanionState
    {
        private float idleAnimationTimer;
        private const float idleAnimationInterval = 8.0f; // Time between potential idle animations

        public void Enter(AICompanionSystem companion)
        {
            Debug.Log("<color=green>Companion entered Idle state.</color>");
            companion.StopMovement(); // Ensure the companion is not moving
            // Reset NavMeshAgent stopping distance to a common default (e.g., follow distance)
            companion.navMeshAgent.stoppingDistance = companion.followStopDistance;
            companion.SetAnimationBool("IsMoving", false);
            // Optionally: Trigger a specific idle animation (e.g., "LookAround")
            idleAnimationTimer = Random.Range(0f, idleAnimationInterval); // Stagger initial timer
        }

        public void Execute(AICompanionSystem companion)
        {
            // Example of internal state logic: occasionally play an idle animation
            idleAnimationTimer += Time.deltaTime;
            if (idleAnimationTimer >= idleAnimationInterval)
            {
                // This could trigger a specific "look around" animation or subtle idle movements
                Debug.Log("Companion looking around idly...");
                // companion.SetAnimationTrigger("LookAround"); // Example
                idleAnimationTimer = 0f;
            }

            // In a more complex game, this state might automatically transition if:
            // - The player moves too far away (auto-follow).
            // - An enemy enters a detection radius (auto-attack).
            // - An interactive object appears nearby.
        }

        public void Exit(AICompanionSystem companion)
        {
            Debug.Log("<color=red>Companion exited Idle state.</color>");
            // Cleanup any specific idle state effects
        }
    }

    /// <summary>
    /// <see cref="ICompanionState"/> implementation for the companion following the player.
    /// It moves towards the player and stops at a specified follow distance.
    /// </summary>
    public class CompanionFollowState : ICompanionState
    {
        public void Enter(AICompanionSystem companion)
        {
            Debug.Log("<color=green>Companion entered Follow state.</color>");
            companion.navMeshAgent.stoppingDistance = companion.followStopDistance;
            companion.SetAnimationBool("IsMoving", true); // Start moving animation
        }

        public void Execute(AICompanionSystem companion)
        {
            // Ensure the player transform is still valid
            if (companion.playerTransform == null)
            {
                Debug.LogWarning("FollowState: Player Transform became null, returning to Idle.", companion);
                companion.ChangeState(companion.idleState);
                return;
            }

            float distanceToPlayer = Vector3.Distance(companion.transform.position, companion.playerTransform.position);

            if (distanceToPlayer > companion.followStopDistance)
            {
                // If the companion is too far, move towards the player's position
                companion.SetDestination(companion.playerTransform.position);
                companion.SetAnimationBool("IsMoving", true);
            }
            else
            {
                // If close enough, stop movement
                companion.StopMovement();
                companion.SetAnimationBool("IsMoving", false);
                // Optionally: Rotate to face the player when standing still
                companion.FaceTarget(companion.playerTransform.position);
            }

            // Example of a conditional transition from within a state:
            // If an enemy comes within a certain range, the companion automatically switches to Attack.
            if (companion.targetEnemy != null)
            {
                float distanceToEnemy = Vector3.Distance(companion.transform.position, companion.targetEnemy.position);
                if (distanceToEnemy < companion.guardRadius && companion.targetEnemy.gameObject.activeInHierarchy) // Check if enemy is active
                {
                    Debug.Log("Enemy detected while following! Switching to Attack state.");
                    companion.ChangeState(companion.attackState);
                }
            }
        }

        public void Exit(AICompanionSystem companion)
        {
            Debug.Log("<color=red>Companion exited Follow state.</color>");
            companion.SetAnimationBool("IsMoving", false); // Stop moving animation
        }
    }

    /// <summary>
    /// <see cref="ICompanionState"/> implementation for the companion attacking a target enemy.
    /// It moves into range, stops, and periodically performs an attack action.
    /// </summary>
    public class CompanionAttackState : ICompanionState
    {
        private float attackTimer;
        private const float attackCooldown = 2.0f; // Time between attacks
        private const float rotationSpeed = 10f; // Speed at which companion rotates to face enemy

        public void Enter(AICompanionSystem companion)
        {
            Debug.Log("<color=green>Companion entered Attack state.</color>");
            companion.navMeshAgent.stoppingDistance = companion.attackStopDistance;
            companion.SetAnimationBool("IsMoving", true); // Initially move towards target
            companion.SetAnimationBool("IsAttacking", false); // Ensure not stuck in attack pose
            attackTimer = attackCooldown; // Ready to attack immediately on entering state
        }

        public void Execute(AICompanionSystem companion)
        {
            // Ensure the target enemy is still valid and active
            if (companion.targetEnemy == null || !companion.targetEnemy.gameObject.activeInHierarchy)
            {
                Debug.Log("AttackState: Target enemy is null or inactive, returning to Follow state.", companion);
                companion.ChangeState(companion.followState); // Return to following or idle
                return;
            }

            float distanceToEnemy = Vector3.Distance(companion.transform.position, companion.targetEnemy.position);

            if (distanceToEnemy > companion.attackStopDistance)
            {
                // Move closer to the enemy
                companion.SetDestination(companion.targetEnemy.position);
                companion.SetAnimationBool("IsMoving", true);
                companion.SetAnimationBool("IsAttacking", false);
            }
            else
            {
                // Within attack range: stop, face enemy, and attack
                companion.StopMovement();
                companion.SetAnimationBool("IsMoving", false);

                // Rotate to face the enemy
                companion.FaceTarget(companion.targetEnemy.position, rotationSpeed);

                attackTimer += Time.deltaTime;
                if (attackTimer >= attackCooldown)
                {
                    PerformAttack(companion);
                    attackTimer = 0f; // Reset cooldown
                }
            }
        }

        /// <summary>
        /// Handles the logic for performing a single attack.
        /// </summary>
        /// <param name="companion">A reference to the <see cref="AICompanionSystem"/>.</param>
        private void PerformAttack(AICompanionSystem companion)
        {
            Debug.Log($"<color=orange>Companion attacked {companion.targetEnemy.name}!</color>");
            companion.SetAnimationTrigger("Attack"); // Trigger one-shot attack animation
            companion.SetAnimationBool("IsAttacking", true); // Keep attack pose if needed for duration

            // --- Real-world game logic would go here ---
            // Example:
            // if (companion.targetEnemy.TryGetComponent(out HealthComponent enemyHealth))
            // {
            //     enemyHealth.TakeDamage(companion.attackDamage);
            //     if (enemyHealth.IsDead())
            //     {
            //         Debug.Log("Enemy defeated! Returning to Follow state.");
            //         companion.ChangeState(companion.followState);
            //     }
            // }
            // Play attack sound, particle effects, etc.
        }

        public void Exit(AICompanionSystem companion)
        {
            Debug.Log("<color=red>Companion exited Attack state.</color>");
            companion.SetAnimationBool("IsMoving", false);
            companion.SetAnimationBool("IsAttacking", false); // Ensure attack pose is off
        }
    }

    /// <summary>
    /// <see cref="ICompanionState"/> implementation for the companion guarding a specific position.
    /// It returns to its guard point if it strays too far and scans for enemies.
    /// </summary>
    public class CompanionGuardState : ICompanionState
    {
        private Vector3 guardPosition;

        public void Enter(AICompanionSystem companion)
        {
            Debug.Log("<color=green>Companion entered Guard state.</color>");
            // Store the companion's current position as the guard point upon entering the state.
            guardPosition = companion.transform.position;
            companion.navMeshAgent.stoppingDistance = 0.5f; // Get quite close to the guard point
            companion.SetAnimationBool("IsMoving", true); // Initially move towards guard point if not there
        }

        public void Execute(AICompanionSystem companion)
        {
            float distanceFromGuardPoint = Vector3.Distance(companion.transform.position, guardPosition);

            if (distanceFromGuardPoint > companion.navMeshAgent.stoppingDistance)
            {
                // Move back to the guard point if the companion has strayed too far
                companion.SetDestination(guardPosition);
                companion.SetAnimationBool("IsMoving", true);
            }
            else
            {
                // At the guard point: stop, perhaps face a direction, and scan for threats
                companion.StopMovement();
                companion.SetAnimationBool("IsMoving", false);

                // Rotate slowly to scan the area (optional visual feedback)
                companion.transform.Rotate(0, 30 * Time.deltaTime, 0);

                // Check for enemies within the guard radius
                if (companion.targetEnemy != null && companion.targetEnemy.gameObject.activeInHierarchy)
                {
                    float distanceToEnemy = Vector3.Distance(companion.transform.position, companion.targetEnemy.position);
                    if (distanceToEnemy < companion.guardRadius)
                    {
                        Debug.Log("Enemy detected while guarding! Switching to Attack state.");
                        companion.ChangeState(companion.attackState);
                    }
                }
            }
        }

        public void Exit(AICompanionSystem companion)
        {
            Debug.Log("<color=red>Companion exited Guard state.</color>");
            companion.SetAnimationBool("IsMoving", false);
        }
    }
}
```