// Unity Design Pattern Example: ComponentBasedArchitecture
// This script demonstrates the ComponentBasedArchitecture pattern in Unity
// Generated automatically - ready to use in your Unity project

The Component-Based Architecture (CBA) is a fundamental design pattern in game development, and it's the core philosophy behind Unity's design. Instead of building large, monolithic classes that contain all the logic for a game object (like a Player or Enemy), CBA encourages you to break down functionality into small, reusable, self-contained components.

In Unity:
*   **Entity:** A `GameObject` acts as the entity. It's essentially an empty container that holds components.
*   **Component:** Any script that inherits from `MonoBehaviour` is a component. Each `MonoBehaviour` should ideally handle a single, specific responsibility (e.g., movement, health, attacking, input).

This approach promotes:
*   **Modularity:** Each component is a distinct piece of logic.
*   **Reusability:** Components can be reused across different types of game objects (e.g., a `MovementComponent` can be used by a player, an enemy, or an NPC).
*   **Flexibility:** You can easily add, remove, or change components on an entity at runtime or design time, drastically altering its behavior without modifying its core code.
*   **Testability:** Smaller, focused components are easier to test in isolation.
*   **Collaboration:** Different team members can work on different components simultaneously.

---

Here's a complete, practical C# Unity example demonstrating the Component-Based Architecture pattern.

**How to Use This Script in Unity:**

1.  **Create a New C# Script:** In your Unity project, create a new C# script named `ComponentBasedArchitectureExample.cs`.
2.  **Copy and Paste:** Replace the entire content of the new script with the code provided below.
3.  **Create a Player GameObject:**
    *   In the Hierarchy, right-click -> Create Empty. Name it `Player`.
    *   Select the `Player` GameObject.
    *   In the Inspector, click "Add Component" and add:
        *   `HealthComponent`
        *   `MovementComponent`
        *   `AttackComponent`
        *   `PlayerInputComponent`
    *   **Important:** On the `PlayerInputComponent`, assign a `Target Enemy` by dragging the `Enemy` GameObject (which you'll create next) into the slot.
4.  **Create an Enemy GameObject:**
    *   In the Hierarchy, right-click -> Create Empty. Name it `Enemy`.
    *   Select the `Enemy` GameObject.
    *   In the Inspector, click "Add Component" and add:
        *   `HealthComponent`
        *   `MovementComponent`
        *   `AttackComponent`
        *   `EnemyAIComponent`
    *   **Important:** On the `EnemyAIComponent`, assign `Player` as the `Target Player` by dragging the `Player` GameObject into the slot.
    *   **Optional:** Add a `Tag` named "Player" to your Player GameObject (`GameObject -> Tag -> Add Tag... -> 'Player'`). The `EnemyAIComponent` will try to find a player with this tag if `targetPlayer` isn't assigned.
5.  **Adjust Inspector Values (Optional):** You can tweak `moveSpeed`, `maxHealth`, `attackDamage`, etc., on each component in the Inspector to see how easily you can customize behavior.
6.  **Run the Scene:**
    *   Use WASD to move the Player.
    *   Left-click to make the Player attack the Enemy (if in range and assigned).
    *   Press 'H' to heal the Player.
    *   Press 'T' to make the Player take damage.
    *   Observe the Enemy chasing, attacking, and patrolling. Watch the Console for debug messages.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random; // To disambiguate from System.Random if needed

// IMPORTANT NOTE FOR REAL PROJECTS:
// In a real Unity project, each of these MonoBehaviour classes (HealthComponent,
// MovementComponent, AttackComponent, PlayerInputComponent, EnemyAIComponent)
// should ideally be in its own separate C# file for better organization,
// maintainability, and compilation speed.
// They are combined here into one file purely for the convenience of a single,
// self-contained example script as requested.

namespace ComponentBasedArchitectureExample
{
    /// <summary>
    /// Component: HealthComponent
    /// Responsibility: Manages the health of an entity, including taking damage, healing,
    /// and notifying other components/systems when health changes or the entity dies.
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("The maximum health this entity can have.")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("The current health of this entity.")]
        [SerializeField] private float currentHealth;

        // Events for other components to subscribe to, promoting loose coupling.
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action OnDied;

        /// <summary>
        /// Gets the current health of the entity.
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the maximum health of the entity.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Checks if the entity is currently alive (health > 0).
        /// </summary>
        public bool IsAlive => currentHealth > 0;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes current health to max health.
        /// </summary>
        void Awake()
        {
            currentHealth = maxHealth;
            // Optionally notify initial health state
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Reduces the entity's current health by the specified amount.
        /// Notifies subscribers if health changes or if the entity dies.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        public void TakeDamage(float amount)
        {
            if (!IsAlive) return; // Cannot take damage if already dead

            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0); // Health cannot go below 0

            Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {currentHealth}.");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                OnDied?.Invoke(); // Notify that the entity has died
                Debug.Log($"{gameObject.name} has died!");
            }
        }

        /// <summary>
        /// Increases the entity's current health by the specified amount.
        /// Notifies subscribers if health changes.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            if (!IsAlive) return; // Cannot heal if already dead

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth); // Health cannot exceed max

            Debug.Log($"{gameObject.name} healed {amount}. Current Health: {currentHealth}.");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Component: MovementComponent
    /// Responsibility: Handles the physical movement and rotation of an entity's GameObject.
    /// It provides methods for moving and looking in a specified direction.
    /// </summary>
    public class MovementComponent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The speed at which the entity moves.")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("The speed at which the entity rotates to face a direction.")]
        [SerializeField] private float rotationSpeed = 10f;

        /// <summary>
        /// Moves the GameObject in the given direction.
        /// For more advanced movement (collisions, physics interactions), a Rigidbody
        /// or CharacterController would be used, and this method would interact with it.
        /// </summary>
        /// <param name="direction">The direction vector to move in.</param>
        public void Move(Vector3 direction)
        {
            if (direction.magnitude > 1f) // Normalize diagonal movement to prevent faster movement
                direction.Normalize();

            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Rotates the GameObject to face towards a specific target position.
        /// </summary>
        /// <param name="targetPosition">The world position to look at.</param>
        public void LookAt(Vector3 targetPosition)
        {
            Vector3 lookDir = targetPosition - transform.position;
            lookDir.y = 0; // Keep rotation flat on the XZ plane

            if (lookDir == Vector3.zero) return; // Avoid looking at self or zero direction

            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Component: AttackComponent
    /// Responsibility: Manages an entity's ability to attack, including damage, range,
    /// and cooldowns. It interacts with the target's HealthComponent.
    /// </summary>
    public class AttackComponent : MonoBehaviour
    {
        [Header("Attack Settings")]
        [Tooltip("The amount of damage dealt per attack.")]
        [SerializeField] private float attackDamage = 10f;
        [Tooltip("The maximum distance from which an attack can hit a target.")]
        [SerializeField] private float attackRange = 2f;
        [Tooltip("The time in seconds before another attack can be performed.")]
        [SerializeField] private float attackCooldown = 1f;

        private float lastAttackTime; // Tracks when the last attack occurred

        /// <summary>
        /// Gets the damage dealt by this component.
        /// </summary>
        public float AttackDamage => attackDamage;

        /// <summary>
        /// Gets the range of attacks from this component.
        /// </summary>
        public float AttackRange => attackRange;

        /// <summary>
        /// Checks if the entity is currently able to perform an attack (i.e., cooldown has passed).
        /// </summary>
        public bool CanAttack()
        {
            return Time.time >= lastAttackTime + attackCooldown;
        }

        /// <summary>
        /// Attempts to attack a specified target GameObject.
        /// Checks for range, cooldown, and whether the target has a HealthComponent.
        /// </summary>
        /// <param name="target">The GameObject to attack.</param>
        /// <returns>True if the attack was successful, false otherwise.</returns>
        public bool TryAttack(GameObject target)
        {
            if (!CanAttack())
            {
                Debug.Log($"{gameObject.name}: Attack on cooldown. ({lastAttackTime + attackCooldown - Time.time:F2}s remaining)");
                return false;
            }
            if (target == null)
            {
                Debug.LogWarning($"{gameObject.name}: Cannot attack null target.");
                return false;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > attackRange)
            {
                Debug.Log($"{gameObject.name}: Target {target.name} is out of range ({distance:F2}m > {attackRange:F2}m).");
                return false;
            }

            // Inter-component communication: Get the target's HealthComponent
            HealthComponent targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsAlive)
            {
                targetHealth.TakeDamage(attackDamage); // Deal damage via target's HealthComponent
                lastAttackTime = Time.time; // Reset cooldown
                Debug.Log($"{gameObject.name} attacked {target.name} for {attackDamage} damage.");
                return true;
            }
            else
            {
                Debug.Log($"{gameObject.name}: Target {target.name} has no HealthComponent or is already dead.");
                return false;
            }
        }
    }

    /// <summary>
    /// Component: PlayerInputComponent
    /// Responsibility: Processes player input and translates it into actions for
    /// other components (MovementComponent, AttackComponent) on the same GameObject.
    /// This demonstrates inter-component communication within the same entity.
    /// </summary>
    // RequireComponent ensures that these essential components are always present.
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(AttackComponent))]
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerInputComponent : MonoBehaviour
    {
        private MovementComponent movementComponent;
        private AttackComponent attackComponent;
        private HealthComponent healthComponent;

        [Header("Player Input Settings")]
        [Tooltip("The enemy GameObject this player will attempt to attack.")]
        [SerializeField] private GameObject targetEnemy; // Reference to an enemy for attacking

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Gets references to other components on this GameObject.
        /// </summary>
        void Awake()
        {
            movementComponent = GetComponent<MovementComponent>();
            attackComponent = GetComponent<AttackComponent>();
            healthComponent = GetComponent<HealthComponent>();

            // Defensive check: ensure all required components are found.
            if (movementComponent == null || attackComponent == null || healthComponent == null)
            {
                Debug.LogError($"[{gameObject.name}] PlayerInputComponent requires MovementComponent, AttackComponent, and HealthComponent on the same GameObject to function. Disabling.", this);
                enabled = false; // Disable this component if dependencies are missing
            }
            else
            {
                // Subscribe to the HealthComponent's OnDied event to stop input when player dies.
                healthComponent.OnDied += HandlePlayerDeath;
            }
        }

        /// <summary>
        /// Called when the behaviour is destroyed.
        /// Unsubscribe from events to prevent memory leaks.
        /// </summary>
        void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDied -= HandlePlayerDeath;
            }
        }

        /// <summary>
        /// Called once per frame. Processes player input.
        /// </summary>
        void Update()
        {
            if (!healthComponent.IsAlive) return; // Don't process input if player is dead

            HandleMovementInput();
            HandleAttackInput();
            HandleDebugInput(); // For testing health
        }

        /// <summary>
        /// Reads keyboard input for movement (WASD) and passes it to the MovementComponent.
        /// </summary>
        private void HandleMovementInput()
        {
            float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
            float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down arrows

            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);

            if (moveDirection.magnitude > 0.1f) // Only move if there's significant input
            {
                movementComponent.Move(moveDirection);
                movementComponent.LookAt(transform.position + moveDirection); // Look in the direction of movement
            }
        }

        /// <summary>
        /// Reads mouse input for attacking (left mouse button) and passes it to the AttackComponent.
        /// </summary>
        private void HandleAttackInput()
        {
            if (Input.GetButtonDown("Fire1")) // Left mouse button
            {
                if (targetEnemy != null)
                {
                    attackComponent.TryAttack(targetEnemy);
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] PlayerInputComponent: No target enemy assigned for attack.");
                }
            }
        }

        /// <summary>
        /// Handles debug input for testing health (H for heal, T for take damage).
        /// </summary>
        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                healthComponent.Heal(10);
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                healthComponent.TakeDamage(20);
            }
        }

        /// <summary>
        /// Event handler for when the player's HealthComponent signals death.
        /// Disables input and other relevant components.
        /// </summary>
        private void HandlePlayerDeath()
        {
            Debug.Log($"[{gameObject.name}] Player is dead. Disabling input and other active components.");
            this.enabled = false; // Disable this input component
            movementComponent.enabled = false; // Stop movement
            attackComponent.enabled = false; // Stop attacking
            // You might also trigger a death animation, disable colliders, etc.
        }
    }

    /// <summary>
    /// Component: EnemyAIComponent
    /// Responsibility: Provides artificial intelligence logic for an enemy entity.
    /// It manages patrolling, chasing a player, and attacking using other components.
    /// This shows how different "input" components (PlayerInputComponent vs. EnemyAIComponent)
    /// can drive the same core components (Movement, Attack, Health).
    /// </summary>
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(AttackComponent))]
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyAIComponent : MonoBehaviour
    {
        private MovementComponent movementComponent;
        private AttackComponent attackComponent;
        private HealthComponent healthComponent;

        [Header("AI Settings")]
        [Tooltip("The player GameObject to chase and attack.")]
        [SerializeField] private GameObject targetPlayer;
        [Tooltip("Distance at which the enemy starts chasing the player.")]
        [SerializeField] private float chaseRange = 10f;
        [Tooltip("Distance from the player at which the enemy stops moving to attack.")]
        [SerializeField] private float attackStopDistance = 1.5f;
        [Tooltip("Radius around its initial position for patrolling when no player is in range.")]
        [SerializeField] private float patrolRadius = 5f;
        [Tooltip("How long the enemy moves towards a patrol point before picking a new one.")]
        [SerializeField] private float patrolMoveDuration = 3f;
        [Tooltip("Min/Max time to pause between patrol moves.")]
        [SerializeField] private Vector2 patrolPauseRange = new Vector2(1f, 3f);

        private Vector3 initialPosition;
        private Coroutine patrolCoroutine;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Gets references to other components on this GameObject.
        /// </summary>
        void Awake()
        {
            movementComponent = GetComponent<MovementComponent>();
            attackComponent = GetComponent<AttackComponent>();
            healthComponent = GetComponent<HealthComponent>();

            if (movementComponent == null || attackComponent == null || healthComponent == null)
            {
                Debug.LogError($"[{gameObject.name}] EnemyAIComponent requires MovementComponent, AttackComponent, and HealthComponent. Disabling.", this);
                enabled = false;
            }
            else
            {
                initialPosition = transform.position;
                healthComponent.OnDied += HandleEnemyDeath; // Subscribe to death event
            }
        }

        /// <summary>
        /// Called when the behaviour is destroyed.
        /// Unsubscribe from events and stop coroutines to prevent memory leaks.
        /// </summary>
        void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDied -= HandleEnemyDeath;
            }
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
            }
        }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// Tries to find player if not assigned, then starts patrol.
        /// </summary>
        void Start()
        {
            // If target player is not set in the Inspector, try to find one by tag.
            if (targetPlayer == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    targetPlayer = playerObj;
                    Debug.Log($"[{gameObject.name}] Found player with tag 'Player': {targetPlayer.name}");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] No target player found (either unassigned in Inspector or no GameObject with tag 'Player'). Enemy will only patrol.");
                }
            }

            if (patrolCoroutine == null)
            {
                patrolCoroutine = StartCoroutine(PatrolRoutine());
            }
        }

        /// <summary>
        /// Called once per frame. Updates the AI's behavior based on player proximity.
        /// </summary>
        void Update()
        {
            if (!healthComponent.IsAlive) return; // Don't process AI if dead

            // Check if player exists and is active
            if (targetPlayer != null && targetPlayer.activeInHierarchy)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

                if (distanceToPlayer <= chaseRange)
                {
                    // Player is in range: stop patrolling, chase, and attack.
                    if (patrolCoroutine != null)
                    {
                        StopCoroutine(patrolCoroutine);
                        patrolCoroutine = null;
                    }
                    HandleChaseAndAttack(distanceToPlayer);
                }
                else // Player is out of chase range, resume patrol if not already.
                {
                    if (patrolCoroutine == null)
                    {
                        patrolCoroutine = StartCoroutine(PatrolRoutine());
                    }
                }
            }
            else // No player, ensure patrolling.
            {
                if (patrolCoroutine == null)
                {
                    patrolCoroutine = StartCoroutine(PatrolRoutine());
                }
            }
        }

        /// <summary>
        /// Handles the logic for chasing and attacking the player.
        /// </summary>
        /// <param name="distanceToPlayer">The current distance to the player.</param>
        private void HandleChaseAndAttack(float distanceToPlayer)
        {
            movementComponent.LookAt(targetPlayer.transform.position);

            if (distanceToPlayer > attackStopDistance)
            {
                // Move towards the player
                Vector3 directionToPlayer = (targetPlayer.transform.position - transform.position).normalized;
                movementComponent.Move(directionToPlayer);
            }
            else
            {
                // Stop moving and try to attack if close enough
                if (attackComponent.CanAttack())
                {
                    attackComponent.TryAttack(targetPlayer);
                }
            }
        }

        /// <summary>
        /// Coroutine for patrolling behavior. Moves to random points within a radius.
        /// </summary>
        IEnumerator PatrolRoutine()
        {
            while (healthComponent.IsAlive)
            {
                // Pick a random point within patrolRadius from the enemy's initial position.
                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                randomDirection.y = 0; // Keep patrol on the XZ plane
                Vector3 targetPatrolPoint = initialPosition + randomDirection;

                // Move towards the target patrol point for a set duration.
                float startTime = Time.time;
                while (Time.time < startTime + patrolMoveDuration &&
                       Vector3.Distance(transform.position, targetPatrolPoint) > 0.5f && // Stop if close enough
                       healthComponent.IsAlive &&
                       (targetPlayer == null || Vector3.Distance(transform.position, targetPlayer.transform.position) > chaseRange)) // Stop if player enters chase range
                {
                    Vector3 directionToPoint = (targetPatrolPoint - transform.position).normalized;
                    movementComponent.Move(directionToPoint);
                    movementComponent.LookAt(targetPatrolPoint);
                    yield return null; // Wait for the next frame
                }

                // Pause for a random duration before moving to the next patrol point.
                yield return new WaitForSeconds(Random.Range(patrolPauseRange.x, patrolPauseRange.y));
            }
        }

        /// <summary>
        /// Event handler for when the enemy's HealthComponent signals death.
        /// Disables AI and other relevant components, then destroys the GameObject.
        /// </summary>
        private void HandleEnemyDeath()
        {
            Debug.Log($"[{gameObject.name}] is dead. Stopping AI and destroying object.");
            this.enabled = false; // Disable this AI component
            movementComponent.enabled = false; // Stop movement
            attackComponent.enabled = false; // Stop attacking
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }
            // You could play a death animation, drop loot, etc., before destroying.
            Destroy(gameObject, 5f); // Destroy the GameObject after a delay
        }
    }
}
```