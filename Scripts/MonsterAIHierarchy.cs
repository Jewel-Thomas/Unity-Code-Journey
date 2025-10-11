// Unity Design Pattern Example: MonsterAIHierarchy
// This script demonstrates the MonsterAIHierarchy pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MonsterAIHierarchy' design pattern, while not one of the classic GoF patterns, typically refers to a system where an AI's decision-making is structured in layers. In game development, especially for NPCs like monsters, this often translates to a **Hierarchical State Machine** or a system where high-level states delegate to more granular behaviors.

This example implements the MonsterAIHierarchy using a **State Machine** pattern, where:
1.  **Top-Level (MonsterAIController):** Determines the overall `MonsterState` (e.g., Idle, Patrol, Chase, Attack, Flee). This is the broadest decision.
2.  **Mid-Level (IMonsterState implementations):** Each concrete state (e.g., `PatrolState`, `AttackState`) encapsulates the specific logic and rules for that state. Within these states, further decisions are made (e.g., "Am I in attack range?", "Should I flee?"). This is where the *hierarchy* allows for more complex, state-specific behavior sets.
3.  **Low-Level (Actions):** The states trigger specific actions using Unity components (e.g., `NavMeshAgent.SetDestination`, `Animator.SetTrigger`) or helper methods on the controller.

This layered approach ensures that the AI's behavior is modular, manageable, and scales well with complexity.

---

```csharp
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// =================================================================================
// MONSTER AI HIERARCHY DESIGN PATTERN EXAMPLE
// =================================================================================
//
// This example demonstrates a 'MonsterAIHierarchy' pattern using a Hierarchical
// State Machine structure.
//
// The core idea is to break down complex monster AI into distinct, manageable
// layers of decision-making:
//
// 1.  TOP-LEVEL (MonsterAIController): Manages the overall current state of the monster.
//     It's responsible for transitioning between broad states like Idle, Patrol, Chase, Attack, Flee.
//     This acts as the orchestrator for the AI.
//
// 2.  MID-LEVEL (IMonsterState implementations): Each state class (e.g., PatrolState, AttackState)
//     contains the specific logic, rules, and sub-decisions for that particular mode of operation.
//     For instance, within the 'PatrolState', decisions are made about which waypoint to move to,
//     and whether to transition to 'ChaseState' if a player is sighted. This is where the
//     'hierarchy' manifests, as each state can have its own complex internal logic, effectively
//     delegating from the broad top-level decision to more specific behavioral logic.
//
// 3.  LOW-LEVEL (Actions): These are the actual commands executed on Unity components,
//     such as setting a destination on a NavMeshAgent, playing an animation, or applying damage.
//     These are the atomic operations performed by the monster, triggered by the mid-level states.
//
//
// Advantages of this pattern:
// -   Modularity: Each state is a self-contained unit of logic, making it easier to develop,
//     test, and debug specific behaviors.
// -   Scalability: New states or behaviors can be added without significantly altering existing ones.
//     Complex behaviors can be broken down into simpler states.
// -   Readability: The flow of AI logic is clear; you can easily see how the monster transitions
//     between different modes.
// -   Maintainability: Changes to one state's behavior are less likely to break others.
//
// =================================================================================

/// <summary>
/// Interface for all monster AI states.
/// Defines the contract for entering, executing, and exiting a state.
/// </summary>
public interface IMonsterState
{
    void EnterState();      // Called when the state is entered
    void ExecuteStateLogic(); // Called every frame while in this state
    void ExitState();       // Called when the state is exited
}

/// <summary>
/// The main Monster AI Controller MonoBehaviour.
/// This script manages the monster's overall state and provides access to its components.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class MonsterAIController : MonoBehaviour
{
    [Header("AI References")]
    [SerializeField] private Transform playerTarget; // The target (e.g., player) the monster will interact with.
    public Transform PlayerTarget => playerTarget;

    private NavMeshAgent navMeshAgent;
    public NavMeshAgent NavMeshAgent => navMeshAgent;

    private Animator animator;
    public Animator Animator => animator;

    private IMonsterState currentState; // The currently active AI state.

    [Header("AI Parameters")]
    [Tooltip("The range within which the monster can detect the player.")]
    public float sightRange = 10f;
    [Tooltip("The range within which the monster can attack the player.")]
    public float attackRange = 2f;
    [Tooltip("The cooldown duration between attacks.")]
    public float attackCooldown = 2f;
    [Tooltip("The speed at which the monster moves while patrolling.")]
    public float patrolSpeed = 2f;
    [Tooltip("The speed at which the monster moves while chasing.")]
    public float chaseSpeed = 4f;
    [Tooltip("The health percentage at which the monster will consider fleeing.")]
    [Range(0.01f, 0.99f)]
    public float fleeThreshold = 0.25f;
    [Tooltip("The duration the monster will flee before reconsidering its state.")]
    public float fleeDuration = 5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("Patrol Points")]
    [Tooltip("A list of transforms representing points for the monster to patrol between.")]
    public List<Transform> patrolPoints;
    private int currentPatrolPointIndex = 0;
    public int CurrentPatrolPointIndex { get => currentPatrolPointIndex; set => currentPatrolPointIndex = value; }

    // --- Internal State Variables ---
    private float lastAttackTime = -Mathf.Infinity; // Tracks when the last attack occurred.
    public float LastAttackTime { get => lastAttackTime; set => lastAttackTime = value; }

    private float fleeStartTime = -Mathf.Infinity; // Tracks when fleeing started.
    public float FleeStartTime { get => fleeStartTime; set => fleeStartTime = value; }


    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        if (playerTarget == null)
        {
            // Try to find a GameObject tagged "Player"
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("MonsterAIController: Player target not set and no GameObject with tag 'Player' found. AI might not function correctly without a target.", this);
            }
        }
    }

    void Start()
    {
        // Initialize the AI with the Idle state.
        ChangeState(new IdleState(this));
    }

    void Update()
    {
        // Execute the logic of the current state every frame.
        currentState?.ExecuteStateLogic();

        // Update animator parameters (e.g., speed for walk/run animations)
        UpdateAnimator();
    }

    /// <summary>
    /// Changes the monster's current AI state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public void ChangeState(IMonsterState newState)
    {
        currentState?.ExitState(); // Call Exit on the old state
        currentState = newState;    // Set the new state
        currentState.EnterState();  // Call Enter on the new state
        Debug.Log($"Monster {gameObject.name} changed state to: {newState.GetType().Name}");
    }

    /// <summary>
    /// Helper method to simulate taking damage.
    /// Used for testing state transitions like fleeing.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth); // Health can't go below zero
        Debug.Log($"Monster {gameObject.name} took {amount} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (currentHealth <= maxHealth * fleeThreshold && !(currentState is FleeState))
        {
            // If health is low and not already fleeing, switch to Flee state.
            ChangeState(new FleeState(this));
        }
    }

    /// <summary>
    /// Heals the monster. Used for testing and recovery scenarios.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Health can't exceed max health
        Debug.Log($"Monster {gameObject.name} healed {amount} health. Current Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Simulates the monster's death.
    /// </summary>
    private void Die()
    {
        Debug.Log($"Monster {gameObject.name} has died!");
        // Disable AI, play death animation, drop loot, etc.
        ChangeState(new DeadState(this)); // Example: a specific state for being dead
        navMeshAgent.isStopped = true;
        animator.SetTrigger("Die");
        // Disable the script or GameObject after a delay
        this.enabled = false;
        // Optionally, destroy the GameObject after a delay
        // Destroy(gameObject, 5f);
    }

    /// <summary>
    /// Updates the Animator with relevant parameters for movement.
    /// </summary>
    private void UpdateAnimator()
    {
        // Calculate normalized speed for blend trees (e.g., Idle -> Walk -> Run)
        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh) // Ensure agent is valid
        {
            float speed = navMeshAgent.velocity.magnitude / navMeshAgent.speed;
            animator.SetFloat("Speed", speed);
        }
        else
        {
            animator.SetFloat("Speed", 0f); // If agent is disabled or not on navmesh, monster is not moving
        }
    }

    /// <summary>
    /// Draws gizmos in the Unity editor to visualize AI ranges.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (transform == null) return; // Prevent errors if object is deleted

        // Sight Range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Attack Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol Points
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.5f);
                    if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints.Count > 1 && patrolPoints[0] != null)
                    {
                        // Loop back to the first point for a closed loop patrol
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }

    // --- Helper Methods accessible by States ---

    /// <summary>
    /// Checks if the player target is within the monster's sight range.
    /// </summary>
    public bool IsPlayerInSightRange()
    {
        if (playerTarget == null) return false;
        return Vector3.Distance(transform.position, playerTarget.position) <= sightRange;
    }

    /// <summary>
    /// Checks if the player target is within the monster's attack range.
    /// </summary>
    public bool IsPlayerInAttackRange()
    {
        if (playerTarget == null) return false;
        return Vector3.Distance(transform.position, playerTarget.position) <= attackRange;
    }

    /// <summary>
    /// Performs an attack on the player target. This would typically trigger an animation
    /// and apply damage via another component/script on the player.
    /// </summary>
    public void PerformAttack()
    {
        // Play attack animation
        animator.SetTrigger("Attack");

        // Simulate damage (replace with actual damage logic)
        // For demonstration, let's just log it. In a real game, this would
        // call a method on the player's health component.
        Debug.Log($"{gameObject.name} attacked {playerTarget.name}!");
        // Example: playerTarget.GetComponent<PlayerHealth>().TakeDamage(10);

        lastAttackTime = Time.time; // Update last attack time for cooldown
    }
}

// =================================================================================
// CONCRETE MONSTER AI STATES (MID-LEVEL HIERARCHY)
// =================================================================================

/// <summary>
/// State for when the monster is idle, doing nothing, possibly waiting for a trigger.
/// </summary>
public class IdleState : IMonsterState
{
    private MonsterAIController controller;
    private float idleDuration; // How long to stay idle
    private float idleStartTime;

    public IdleState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = true;
        controller.Animator.SetFloat("Speed", 0f); // Ensure idle animation plays
        idleDuration = Random.Range(2f, 5f); // Random idle time
        idleStartTime = Time.time;
    }

    public void ExecuteStateLogic()
    {
        // Hierarchy decision: Check for player target first (high priority)
        if (controller.PlayerTarget != null && controller.IsPlayerInSightRange())
        {
            controller.ChangeState(new ChaseState(controller));
            return;
        }

        // Hierarchy decision: If idle time is up, transition to Patrol
        if (Time.time >= idleStartTime + idleDuration)
        {
            controller.ChangeState(new PatrolState(controller));
        }
    }

    public void ExitState()
    {
        // Any cleanup when leaving idle state
    }
}

/// <summary>
/// State for when the monster is patrolling between designated points.
/// </summary>
public class PatrolState : IMonsterState
{
    private MonsterAIController controller;

    public PatrolState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = false;
        controller.NavMeshAgent.speed = controller.patrolSpeed;
        MoveToNextPatrolPoint();
    }

    public void ExecuteStateLogic()
    {
        // Hierarchy decision: Prioritize fleeing if health is low
        if (controller.CurrentHealth <= controller.maxHealth * controller.fleeThreshold)
        {
            controller.ChangeState(new FleeState(controller));
            return;
        }

        // Hierarchy decision: Check for player target in sight (high priority over patrolling)
        if (controller.PlayerTarget != null && controller.IsPlayerInSightRange())
        {
            controller.ChangeState(new ChaseState(controller));
            return;
        }

        // Hierarchy decision: If current patrol point reached, move to the next.
        if (controller.NavMeshAgent.remainingDistance <= controller.NavMeshAgent.stoppingDistance &&
            !controller.NavMeshAgent.pathPending)
        {
            MoveToNextPatrolPoint();
        }
    }

    public void ExitState()
    {
        // Cleanup: Stop the agent if it was moving to a patrol point.
        controller.NavMeshAgent.isStopped = true;
    }

    /// <summary>
    /// Determines and sets the next patrol point for the NavMeshAgent.
    /// This is a sub-behavior/decision within the PatrolState.
    /// </summary>
    private void MoveToNextPatrolPoint()
    {
        if (controller.patrolPoints == null || controller.patrolPoints.Count == 0)
        {
            // No patrol points, fallback to Idle or stay put
            Debug.LogWarning("No patrol points set for monster. Transitioning to IdleState.");
            controller.ChangeState(new IdleState(controller));
            return;
        }

        controller.NavMeshAgent.SetDestination(controller.patrolPoints[controller.CurrentPatrolPointIndex].position);
        controller.CurrentPatrolPointIndex = (controller.CurrentPatrolPointIndex + 1) % controller.patrolPoints.Count;
    }
}

/// <summary>
/// State for when the monster is actively chasing its player target.
/// </summary>
public class ChaseState : IMonsterState
{
    private MonsterAIController controller;

    public ChaseState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = false;
        controller.NavMeshAgent.speed = controller.chaseSpeed;
    }

    public void ExecuteStateLogic()
    {
        // No player target available, monster should go back to patrolling or idling.
        if (controller.PlayerTarget == null)
        {
            controller.ChangeState(new PatrolState(controller)); // Or IdleState
            return;
        }

        // Hierarchy decision: Prioritize fleeing if health is low
        if (controller.CurrentHealth <= controller.maxHealth * controller.fleeThreshold)
        {
            controller.ChangeState(new FleeState(controller));
            return;
        }

        // Hierarchy decision: If player is within attack range, transition to Attack state.
        if (controller.IsPlayerInAttackRange())
        {
            controller.ChangeState(new AttackState(controller));
            return;
        }

        // Hierarchy decision: If player is outside sight range, monster has lost target.
        if (!controller.IsPlayerInSightRange())
        {
            controller.ChangeState(new PatrolState(controller)); // Lost target, go back to patrol
            return;
        }

        // Low-level action: Move towards the player target.
        controller.NavMeshAgent.SetDestination(controller.PlayerTarget.position);
    }

    public void ExitState()
    {
        // Cleanup: Stop the agent.
        controller.NavMeshAgent.isStopped = true;
    }
}

/// <summary>
/// State for when the monster is attacking its player target.
/// </summary>
public class AttackState : IMonsterState
{
    private MonsterAIController controller;

    public AttackState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = true; // Stop moving to attack
        controller.Animator.SetFloat("Speed", 0f);
    }

    public void ExecuteStateLogic()
    {
        // No player target available, monster should go back to patrolling or idling.
        if (controller.PlayerTarget == null)
        {
            controller.ChangeState(new PatrolState(controller));
            return;
        }

        // Hierarchy decision: Prioritize fleeing if health is low
        if (controller.CurrentHealth <= controller.maxHealth * controller.fleeThreshold)
        {
            controller.ChangeState(new FleeState(controller));
            return;
        }

        // Hierarchy decision: If player moves out of attack range, chase them again.
        if (!controller.IsPlayerInAttackRange())
        {
            controller.ChangeState(new ChaseState(controller));
            return;
        }

        // Low-level action: Face the target while attacking.
        Vector3 directionToTarget = (controller.PlayerTarget.position - controller.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, lookRotation, Time.deltaTime * 5f);

        // Hierarchy decision: Check for attack cooldown before performing another attack.
        if (Time.time >= controller.LastAttackTime + controller.attackCooldown)
        {
            controller.PerformAttack(); // Perform the attack action
        }
    }

    public void ExitState()
    {
        // No specific cleanup needed here, but could stop attack animations if they were looping.
    }
}

/// <summary>
/// State for when the monster is fleeing due to low health or a powerful threat.
/// </summary>
public class FleeState : IMonsterState
{
    private MonsterAIController controller;
    private Vector3 fleeDirection;
    private float fleeDistance = 10f; // How far to attempt to flee

    public FleeState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = false;
        controller.NavMeshAgent.speed = controller.chaseSpeed; // Fleeing should be fast

        controller.FleeStartTime = Time.time;

        if (controller.PlayerTarget != null)
        {
            // Calculate a direction away from the player
            fleeDirection = (controller.transform.position - controller.PlayerTarget.position).normalized;
        }
        else
        {
            // If no target, just pick a random direction to flee
            fleeDirection = Random.insideUnitCircle.normalized;
        }

        // Try to find a valid point on the NavMesh to flee to
        Vector3 destination = controller.transform.position + fleeDirection * fleeDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, fleeDistance * 2, NavMesh.AllAreas))
        {
            controller.NavMeshAgent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning("Could not find a valid flee point on NavMesh. Monster might get stuck.", controller);
            // Fallback: Just go back to patrol if nowhere to flee
            controller.ChangeState(new PatrolState(controller));
        }
    }

    public void void ExecuteStateLogic()
    {
        // Hierarchy decision: If flee duration is over, or health has recovered enough, re-evaluate.
        if (Time.time >= controller.FleeStartTime + controller.fleeDuration ||
            controller.CurrentHealth > controller.maxHealth * (controller.fleeThreshold + 0.1f)) // Recovered slightly
        {
            controller.ChangeState(new PatrolState(controller)); // Return to patrolling
            return;
        }

        // Hierarchy decision: If the monster has reached its flee destination, but is still threatened/low health
        // (and not over flee duration), find a new flee point.
        if (controller.NavMeshAgent.remainingDistance <= controller.NavMeshAgent.stoppingDistance &&
            !controller.NavMeshAgent.pathPending)
        {
            // Find a new random direction to flee if still in a bad state
            fleeDirection = Random.insideUnitCircle.normalized; // Pick a new direction
            Vector3 destination = controller.transform.position + fleeDirection * fleeDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, fleeDistance * 2, NavMesh.AllAreas))
            {
                controller.NavMeshAgent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning("Could not find a valid *new* flee point on NavMesh. Attempting to patrol.", controller);
                controller.ChangeState(new PatrolState(controller));
            }
        }
    }

    public void ExitState()
    {
        controller.NavMeshAgent.isStopped = true;
    }
}

/// <summary>
/// A placeholder state for when the monster is dead.
/// This state ensures the AI stops all activity and remains in a 'dead' state.
/// </summary>
public class DeadState : IMonsterState
{
    private MonsterAIController controller;

    public DeadState(MonsterAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.NavMeshAgent.isStopped = true;
        controller.NavMeshAgent.enabled = false; // Disable NavMeshAgent completely
        controller.Animator.SetFloat("Speed", 0f);
        // Play death animation if not already triggered by the controller's Die() method
        // controller.Animator.SetTrigger("Die");
    }

    public void ExecuteStateLogic()
    {
        // A dead monster does nothing.
    }

    public void ExitState()
    {
        // Nothing to do when exiting a dead state, as it implies resurrection or removal.
    }
}

/*
 * =================================================================================
 * EXAMPLE USAGE IN UNITY
 * =================================================================================
 *
 * To use this script in your Unity project:
 *
 * 1.  CREATE A MONSTER GAMEOBJECT:
 *     -   Create an empty GameObject (e.g., "Monster").
 *     -   Add a 3D model (e.g., a Capsule, Cube, or your custom character model) as a child.
 *     -   Ensure the model has an Animator component with basic 'Idle', 'Walk', 'Run', 'Attack', 'Die'
 *         animations and corresponding parameters ("Speed" float for blend tree, "Attack" trigger, "Die" trigger).
 *         The provided code expects an "Speed" float and "Attack"/"Die" triggers.
 *
 * 2.  ADD REQUIRED COMPONENTS TO THE MONSTER GAMEOBJECT:
 *     -   Add a `NavMeshAgent` component to the "Monster" GameObject. Configure its radius, height, etc.
 *     -   Add this `MonsterAIController.cs` script to the "Monster" GameObject.
 *
 * 3.  SET UP THE SCENE:
 *     -   **NavMesh:** Ensure you have a NavMesh baked in your scene (Window > AI > Navigation, Bake tab).
 *     -   **Player Target:** Create a GameObject for the player (e.g., "Player").
 *         -   Give it the tag "Player" (or set the `PlayerTarget` field in the `MonsterAIController` directly).
 *         -   It doesn't need any special scripts for this example, just its Transform.
 *
 * 4.  CONFIGURE THE `MonsterAIController` IN THE INSPECTOR:
 *     -   **Player Target:** Drag your "Player" GameObject into this field, or ensure it has the "Player" tag.
 *     -   **AI Parameters:** Adjust `Sight Range`, `Attack Range`, `Attack Cooldown`, `Patrol Speed`, `Chase Speed`, `Flee Threshold`, `Flee Duration` as desired.
 *     -   **Health:** Set `Max Health`.
 *     -   **Patrol Points:** Create a few empty GameObjects (e.g., "PatrolPoint1", "PatrolPoint2") in your scene
 *         and drag them into the `Patrol Points` list on the `MonsterAIController`.
 *
 * 5.  (OPTIONAL) TESTING DAMAGE:
 *     -   To test the `TakeDamage` and `FleeState`, you can add a simple script to your player
 *         or another object that calls `monsterAI.TakeDamage(amount)` on interaction (e.g., mouse click, trigger enter).
 *     -   Example of a simple damage trigger script:
 *
 *       ```csharp
 *       // DamageTrigger.cs
 *       using UnityEngine;
 *
 *       public class DamageTrigger : MonoBehaviour
 *       {
 *           public int damageAmount = 20;
 *
 *           void OnTriggerEnter(Collider other)
 *           {
 *               MonsterAIController monster = other.GetComponent<MonsterAIController>();
 *               if (monster != null)
 *               {
 *                   monster.TakeDamage(damageAmount);
 *               }
 *           }
 *       }
 *       ```
 *       -   Add a `BoxCollider` (set to `Is Trigger`) and a `Rigidbody` to this `DamageTrigger` GameObject.
 *
 * This setup will allow you to see the monster transition between Idle, Patrol, Chase, Attack, and Flee states
 * based on its parameters, player proximity, and health.
 */
```