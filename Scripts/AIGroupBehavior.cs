// Unity Design Pattern Example: AIGroupBehavior
// This script demonstrates the AIGroupBehavior pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AIGroupBehavior' design pattern, often a specialized application of the State and Strategy patterns, focuses on coordinating the actions of multiple AI agents as a single cohesive unit. Instead of each AI making independent decisions, a central manager (or 'group leader') dictates the overall strategy, and individual agents execute their part of that strategy.

This example provides a complete, practical C# Unity script demonstrating the AIGroupBehavior pattern. It includes an `AIGroupManager`, individual `AIGroupMember`s, and two concrete `IGroupBehavior` implementations: `GroupPatrolBehavior` and `GroupAttackBehavior`.

---

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like .All()
// Using UnityEditor for Gizmos.Label is only for editor-time drawing, not for runtime builds.
// For runtime, you would use UI Text or Debug.Log.
#if UNITY_EDITOR
using UnityEditor; 
#endif

namespace AIGroupBehaviorPattern
{
    // --- 1. The IGroupBehavior Interface ---
    // This interface defines the contract for any behavior that a group of AI agents can perform.
    // It's the core of the pattern, allowing for interchangeable group strategies (Strategy Pattern).
    public interface IGroupBehavior
    {
        /// <summary>
        /// Called when this behavior becomes active for the group. Use for initialization.
        /// </summary>
        /// <param name="manager">The AIGroupManager instance managing this behavior.</param>
        void Enter(AIGroupManager manager);

        /// <summary>
        /// Called every frame while this behavior is active. This is where the core group logic lives,
        /// commanding individual members to perform their roles within the group's strategy.
        /// </summary>
        /// <param name="manager">The AIGroupManager instance managing this behavior.</param>
        void Execute(AIGroupManager manager);

        /// <summary>
        /// Called when this behavior is no longer active (e.g., when switching to another behavior).
        /// Use for cleanup or resetting member states.
        /// </summary>
        /// <param name="manager">The AIGroupManager instance managing this behavior.</param>
        void Exit(AIGroupManager manager);
    }

    // --- 2. The AIGroupMember Class ---
    // Represents an individual AI agent that is part of a group.
    // It's responsible for its own movement and individual actions based on commands from the group manager.
    // Requires a NavMeshAgent component for navigation.
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIGroupMember : MonoBehaviour
    {
        [Header("Member Settings")]
        [Tooltip("The range at which this member can attack its target.")]
        [SerializeField] private float attackRange = 1.5f;
        [Tooltip("The damage this member deals per attack.")]
        [SerializeField] private float attackDamage = 10f;
        [Tooltip("The cooldown period between attacks.")]
        [SerializeField] private float attackCooldown = 1f;
        [Tooltip("The speed at which the member rotates to face its target.")]
        [SerializeField] private float rotationSpeed = 5f;

        private NavMeshAgent agent;
        private Transform currentAttackTarget; // The specific target this member is trying to attack
        private float lastAttackTime;

        // Public properties for the AIGroupManager to query the member's status.
        public bool IsMoving => agent.hasPath && !agent.isStopped && agent.remainingDistance > agent.stoppingDistance + 0.1f;
        public bool IsAtDestination => !IsMoving && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        public NavMeshAgent Agent => agent;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            // Ensure agent doesn't stop too far from its target destination
            agent.stoppingDistance = 0.5f;
        }

        void Update()
        {
            // If we have an attack target and are not currently moving to a new position, face it.
            // This ensures the member always faces its combat target when idle or attacking.
            if (currentAttackTarget != null && !IsMoving)
            {
                FaceTarget(currentAttackTarget.position);
            }
        }

        // --- Public methods for AIGroupManager (or IGroupBehaviors) to command members ---

        /// <summary>
        /// Commands the member to move to a specific world position. Clears any current attack target.
        /// </summary>
        /// <param name="position">The target position to move to.</param>
        public void MoveTo(Vector3 position)
        {
            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false; // Ensure agent is not stopped
                agent.SetDestination(position);
            }
            currentAttackTarget = null; // Clear attack target when moving to a general position
        }

        /// <summary>
        /// Commands the member to move towards a specific position, while also setting an attack target.
        /// The member will try to move to the position and then attack the target if in range.
        /// </summary>
        /// <param name="target">The transform of the object to attack.</param>
        /// <param name="positionToMoveTo">The specific world position the member should move to first (e.g., a formation spot).</param>
        public void MoveAndAttack(Transform target, Vector3 positionToMoveTo)
        {
            currentAttackTarget = target; // Set target for potential attack and facing

            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false; // Ensure agent is not stopped
                agent.SetDestination(positionToMoveTo);
            }
        }

        /// <summary>
        /// Attempts to perform an attack if an attack target is set, is within range, and attack cooldown allows.
        /// </summary>
        public void PerformAttackIfReady()
        {
            if (currentAttackTarget == null || !currentAttackTarget.gameObject.activeInHierarchy) return;

            // Check if within attack range of the currentAttackTarget
            if (Vector3.Distance(transform.position, currentAttackTarget.position) <= attackRange)
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    // Simulate attack (e.g., play animation, deal damage).
                    Debug.Log($"{name} attacking {currentAttackTarget.name}!");
                    // In a real game, you would call a method on the target:
                    // currentAttackTarget.GetComponent<HealthComponent>()?.TakeDamage(attackDamage);
                    #if UNITY_EDITOR // For demonstration, if SimpleEnemy exists and is in editor
                    SimpleEnemy enemyHealth = currentAttackTarget.GetComponent<SimpleEnemy>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(attackDamage);
                    }
                    #endif
                    
                    lastAttackTime = Time.time;
                    FaceTarget(currentAttackTarget.position); // Ensure facing while attacking
                }
            }
        }

        /// <summary>
        /// Stops any current movement and clears any attack target.
        /// </summary>
        public void StopActions()
        {
            if (agent.isOnNavMesh && agent.enabled && agent.hasPath)
            {
                agent.ResetPath(); // Stop current pathfinding
            }
            agent.isStopped = true; // Ensure agent is stopped
            currentAttackTarget = null;
        }

        // Rotates the agent to face a target position on the horizontal plane.
        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                // Only consider XZ plane for rotation
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }

        // --- Editor Visualizations ---
        void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw current path
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.cyan;
                Vector3 lastCorner = transform.position;
                foreach (var corner in agent.path.corners)
                {
                    Gizmos.DrawLine(lastCorner, corner);
                    Gizmos.DrawSphere(corner, 0.1f);
                    lastCorner = corner;
                }
            }
        }
    }

    // --- 3. Concrete Group Behavior: Patrol ---
    // This behavior instructs the group to patrol a set of predefined points.
    public class GroupPatrolBehavior : IGroupBehavior
    {
        private int currentPatrolPointIndex;
        // Radius around a patrol point where members are considered to have reached it.
        private const float patrolWaypointRadius = 2f; 
        // Offset to spread members out around a patrol point, preventing stacking.
        private const float memberSpreadRadius = 1.5f;

        public void Enter(AIGroupManager manager)
        {
            Debug.Log("Group entering Patrol Behavior.");
            currentPatrolPointIndex = 0;
            // Immediately assign the first patrol point to members when entering the behavior.
            AssignCurrentPatrolPointToMembers(manager);
        }

        public void Execute(AIGroupManager manager)
        {
            // Ensure there are patrol points defined.
            if (manager.GroupPatrolPoints == null || manager.GroupPatrolPoints.Length == 0)
            {
                Debug.LogWarning("No patrol points defined for the group. Patrol behavior cannot function.");
                return;
            }

            // Get the current target patrol point for the entire group.
            Vector3 currentGroupTargetPoint = manager.GroupPatrolPoints[currentPatrolPointIndex].position;

            // Draw a line to the current group patrol point for visualization in the editor.
            Debug.DrawLine(currentGroupTargetPoint + Vector3.up * 5, currentGroupTargetPoint, Color.yellow);

            // Command all members to move towards their assigned positions around the current group patrol point.
            // This logic allows for dynamic re-assignment if a member stops or gets stuck.
            for (int i = 0; i < manager.Members.Count; i++)
            {
                AIGroupMember member = manager.Members[i];
                // Only re-assign destination if the member isn't actively moving towards it
                // or if it got too far off course.
                if (!member.IsMoving || Vector3.Distance(member.Agent.destination, currentGroupTargetPoint) > memberSpreadRadius * 2) 
                {
                    // Calculate a spread-out position around the current group patrol point for each member.
                    Vector3 offset = new Vector3(
                        Mathf.Cos(i * (Mathf.PI * 2 / manager.Members.Count)) * memberSpreadRadius, 
                        0,
                        Mathf.Sin(i * (Mathf.PI * 2 / manager.Members.Count)) * memberSpreadRadius
                    );
                    member.MoveTo(currentGroupTargetPoint + offset);
                }
            }

            // Check if all members are near the current group patrol point.
            bool allMembersNearPoint = manager.Members.All(m =>
                Vector3.Distance(m.transform.position, currentGroupTargetPoint) <= patrolWaypointRadius);

            // If all members have reached the current point, advance to the next.
            if (allMembersNearPoint)
            {
                Debug.Log($"All members reached patrol point {currentPatrolPointIndex}. Moving to next.");
                currentPatrolPointIndex = (currentPatrolPointIndex + 1) % manager.GroupPatrolPoints.Length;
                // Assign new patrol point immediately after advancing the index.
                AssignCurrentPatrolPointToMembers(manager);
            }
        }

        public void Exit(AIGroupManager manager)
        {
            Debug.Log("Group exiting Patrol Behavior.");
            // Stop all members from moving when exiting patrol.
            foreach (var member in manager.Members)
            {
                member.StopActions();
            }
        }

        // Helper method to assign destinations around the current group patrol point to all members.
        private void AssignCurrentPatrolPointToMembers(AIGroupManager manager)
        {
            if (manager.GroupPatrolPoints == null || manager.GroupPatrolPoints.Length == 0) return;

            Vector3 currentGroupTargetPoint = manager.GroupPatrolPoints[currentPatrolPointIndex].position;
            for (int i = 0; i < manager.Members.Count; i++)
            {
                AIGroupMember member = manager.Members[i];
                // Calculate an individual position around the group's current patrol point.
                Vector3 offset = new Vector3(
                    Mathf.Cos(i * (Mathf.PI * 2 / manager.Members.Count)) * memberSpreadRadius, 
                    0,
                    Mathf.Sin(i * (Mathf.PI * 2 / manager.Members.Count)) * memberSpreadRadius
                );
                member.MoveTo(currentGroupTargetPoint + offset);
            }
        }
    }

    // --- 4. Concrete Group Behavior: Attack ---
    // This behavior instructs the group to move to and attack a specific target.
    public class GroupAttackBehavior : IGroupBehavior
    {
        // Radius around the target where members will try to position themselves for attack.
        private const float attackFormationRadius = 4f; 
        // Distance from target within which members will start moving into attack formation.
        private const float engageDistance = 15f; 

        public void Enter(AIGroupManager manager)
        {
            Debug.Log("Group entering Attack Behavior.");
            // No initial assignments, as Execute will handle continuous positioning and attacking.
        }

        public void Execute(AIGroupManager manager)
        {
            // If the group target is no longer valid (e.g., destroyed, disappeared),
            // switch back to patrol behavior.
            if (manager.GroupTarget == null || !manager.GroupTarget.gameObject.activeInHierarchy)
            {
                Debug.Log("Target destroyed or disappeared. Switching back to Patrol.");
                manager.SetBehavior(new GroupPatrolBehavior()); // Fallback
                return;
            }

            // Draw a line from the manager's position to the target for visualization.
            Debug.DrawLine(manager.transform.position, manager.GroupTarget.position, Color.magenta);

            // Command members to move to strategic positions around the target and attack.
            for (int i = 0; i < manager.Members.Count; i++)
            {
                AIGroupMember member = manager.Members[i];
                Transform target = manager.GroupTarget;

                // Calculate a position around the target for the member to move to.
                // This creates a simple encircling or flanking formation.
                // Adding Time.time*0.1f makes the formation slowly rotate, for a more dynamic look.
                Vector3 offset = new Vector3(
                    Mathf.Cos(i * (Mathf.PI * 2 / manager.Members.Count) + Time.time * 0.1f) * attackFormationRadius,
                    0,
                    Mathf.Sin(i * (Mathf.PI * 2 / manager.Members.Count) + Time.time * 0.1f) * attackFormationRadius
                );
                Vector3 targetPositionForMember = target.position + offset;

                // If the target is within engaging distance, move to the calculated formation position and attack.
                if (Vector3.Distance(member.transform.position, target.position) <= engageDistance)
                {
                    member.MoveAndAttack(target, targetPositionForMember);
                    member.PerformAttackIfReady(); // Try to attack if in range and cooldown allows.
                }
                else // If too far, move towards a general rallying point closer to the target.
                {
                    // Move the member closer to the target's general area to get within engageDistance.
                    Vector3 moveTowardTarget = target.position + (member.transform.position - target.position).normalized * (engageDistance - 2f);
                    member.MoveTo(moveTowardTarget);
                }
            }
        }

        public void Exit(AIGroupManager manager)
        {
            Debug.Log("Group exiting Attack Behavior.");
            // Stop all members from moving/attacking when exiting attack behavior.
            foreach (var member in manager.Members)
            {
                member.StopActions();
            }
        }
    }

    // --- 5. The AIGroupManager Class ---
    // The central component that manages the group's behavior.
    // It holds references to individual group members, defines group-level data (like patrol points, target),
    // and determines which IGroupBehavior is currently active (State Pattern).
    public class AIGroupManager : MonoBehaviour
    {
        [Header("Group Members")]
        [Tooltip("List of all individual AI members belonging to this group.")]
        [SerializeField] private List<AIGroupMember> members = new List<AIGroupMember>();

        [Header("Patrol Settings")]
        [Tooltip("The sequence of Transforms defining the group's patrol route.")]
        [SerializeField] private Transform[] groupPatrolPoints;

        [Header("Target Detection")]
        [Tooltip("The radius within which the manager will search for targets.")]
        [SerializeField] private float targetDetectionRadius = 15f;
        [Tooltip("The Layer(s) on which targets (enemies) can be found.")]
        [SerializeField] private LayerMask targetLayer;

        private IGroupBehavior currentGroupBehavior; // The currently active behavior for the group.
        private Transform groupTarget;              // The current target for the entire group (e.g., an enemy).

        // Public accessors for IGroupBehavior implementations to get group data.
        public List<AIGroupMember> Members => members;
        public Transform[] GroupPatrolPoints => groupPatrolPoints;
        public Transform GroupTarget => groupTarget;

        void Awake()
        {
            // Initialize the group with a default behavior (e.g., patrolling).
            SetBehavior(new GroupPatrolBehavior());
        }

        void Update()
        {
            // Before executing current behavior, continuously check for potential targets.
            // This detection logic can trigger a switch in group behavior.
            DetectTarget();

            // Execute the logic of the currently active group behavior.
            // The 'this' keyword passes the AIGroupManager instance to the behavior,
            // allowing the behavior to command members and access group data.
            currentGroupBehavior?.Execute(this); 
        }

        /// <summary>
        /// Changes the current group behavior to a new one.
        /// It properly calls Exit() on the old behavior and Enter() on the new one.
        /// </summary>
        /// <param name="newBehavior">The new IGroupBehavior to activate.</param>
        public void SetBehavior(IGroupBehavior newBehavior)
        {
            if (currentGroupBehavior != null)
            {
                currentGroupBehavior.Exit(this); // Clean up the old behavior.
            }

            currentGroupBehavior = newBehavior;
            currentGroupBehavior.Enter(this); // Initialize the new behavior.
        }

        // Detects enemies within range and sets the GroupTarget, triggering behavior changes.
        private void DetectTarget()
        {
            // If we are currently in attack mode and our target is still valid,
            // we don't need to re-scan unless the current behavior decides to re-evaluate.
            // This prevents rapid switching if the target briefly leaves and re-enters range.
            if (groupTarget != null && groupTarget.gameObject.activeInHierarchy && currentGroupBehavior is GroupAttackBehavior)
            {
                // Optionally add logic here to ensure target is still in range,
                // or let the Attack behavior handle loss of target directly.
                return;
            }

            // Search for colliders within the detection radius on the specified target layer.
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, targetDetectionRadius, targetLayer);

            if (hitColliders.Length > 0)
            {
                // For simplicity, we just pick the first target found.
                // In a real game, you might prioritize by closest, highest threat, lowest health, etc.
                groupTarget = hitColliders[0].transform;
                // If a target is found and we are not already in attack mode, switch to attack.
                if (!(currentGroupBehavior is GroupAttackBehavior))
                {
                    Debug.Log($"Target {groupTarget.name} detected! Switching to Attack Behavior.");
                    SetBehavior(new GroupAttackBehavior());
                }
            }
            else // No target found in range.
            {
                groupTarget = null;
                // If no target is found and we are not already patrolling, switch to patrol.
                if (!(currentGroupBehavior is GroupPatrolBehavior))
                {
                    Debug.Log("No target detected. Switching to Patrol Behavior.");
                    SetBehavior(new GroupPatrolBehavior());
                }
            }
        }

        // --- Editor Visualizations ---
        void OnDrawGizmos()
        {
            // Draw detection radius sphere.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

            // Draw patrol points and lines connecting them.
            if (groupPatrolPoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < groupPatrolPoints.Length; i++)
                {
                    if (groupPatrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(groupPatrolPoints[i].position, 0.7f);
                        // Draw line to the next patrol point, wrapping around for a loop.
                        if (groupPatrolPoints.Length > 1 && groupPatrolPoints[(i + 1) % groupPatrolPoints.Length] != null)
                        {
                            Gizmos.DrawLine(groupPatrolPoints[i].position, groupPatrolPoints[(i + 1) % groupPatrolPoints.Length].position);
                        }
                    }
                }
            }

            // Draw line to the current group target.
            if (groupTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, groupTarget.position);
                Gizmos.DrawWireSphere(groupTarget.position, 1f);
            }
        }
    }

    // --- Example Enemy (Optional, for demonstration purposes) ---
    // A simple script to represent an enemy that can be targeted and destroyed.
    // This allows testing the attack behavior and target loss.
    public class SimpleEnemy : MonoBehaviour
    {
        [SerializeField] private float health = 50f;

        /// <summary>
        /// Applies damage to the enemy. If health drops to 0 or below, the enemy is destroyed.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        public void TakeDamage(float amount)
        {
            health -= amount;
            Debug.Log($"{name} took {amount} damage. Health: {health}");
            if (health <= 0)
            {
                Debug.Log($"{name} defeated!");
                Destroy(gameObject); // Remove enemy from scene
            }
        }

        // --- Editor Visualizations ---
        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            #if UNITY_EDITOR
            // Display health in the editor. Requires UnityEditor namespace.
            Handles.Label(transform.position + Vector3.up * 1f, $"Health: {health:F0}");
            #endif
        }
    }
}

/*
// --- HOW TO USE THIS EXAMPLE IN UNITY ---

1.  **Create a new C# script** in your Unity project, name it `AIGroupBehaviorExample`, and copy-paste the entire code above into it.

2.  **Scene Setup:**
    *   **Bake a NavMesh:** Ensure you have a ground plane (e.g., a Unity Plane or custom terrain) and a **NavMesh baked on it** (`Window > AI > Navigation > Bake tab`). This is crucial for `NavMeshAgent` to work correctly.
    *   **Create Group Members:**
        *   Create several 3D objects (e.g., Capsules, Cylinders) to represent your AI agents.
        *   Name them something like "AI_Member_1", "AI_Member_2", etc.
        *   Add the `AIGroupMember` component to each of them.
        *   A `NavMeshAgent` component will automatically be added (due to `[RequireComponent]`).
    *   **Create Patrol Points:**
        *   Create empty GameObjects (`GameObject > Create Empty`) and position them in your scene where you want the group to patrol.
        *   Name them "PatrolPoint_1", "PatrolPoint_2", etc.
    *   **Create an Enemy (Target):**
        *   Create another 3D object (e.g., a Sphere) to represent an enemy.
        *   Add a `Rigidbody` component to it (you can mark it `Is Kinematic` if you don't want physics interactions, but a Rigidbody is often needed for `Physics.OverlapSphere` to detect it).
        *   Add a `Collider` component (e.g., `SphereCollider`).
        *   Add the `SimpleEnemy` component to it (this makes it take damage and be destroyable).
        *   **Crucially, set its Layer** (in the Inspector, top right dropdown) to a new Layer, e.g., "Enemy". You'll need to define this new layer in `Tags & Layers` dropdown if it doesn't exist.

3.  **Create the AIGroupManager:**
    *   Create an empty GameObject in your scene, name it "AI_GroupManager".
    *   Add the `AIGroupManager` component to it.

4.  **Configure AIGroupManager in Inspector:**
    *   **Members:** Drag and drop all your "AI_Member_X" GameObjects from the Hierarchy into the `Members` list.
    *   **Group Patrol Points:** Drag and drop all your "PatrolPoint_X" GameObjects into the `Group Patrol Points` array in the desired order.
    *   **Target Detection Radius:** Set a value (e.g., `15-20`). This defines how far the manager will look for enemies.
    *   **Target Layer:** Select the "Enemy" layer (or whatever layer you assigned to your enemy GameObject).

5.  **Run the Scene:**
    *   The AI members should start patrolling the defined points, moving in a coordinated fashion.
    *   Move the "Enemy" GameObject within the `Target Detection Radius` of the `AIGroupManager`. The group should switch to "Attack Behavior", move into formation around the enemy, and start "attacking" it (reducing its health if `SimpleEnemy` is attached).
    *   If the enemy is destroyed (health reaches zero) or moves out of the `Target Detection Radius`, the group should switch back to "Patrol Behavior" and resume patrolling.

---

**Detailed Explanation of the AIGroupBehavior Pattern:**

*   **Problem:** In games, AI groups often need to act as a single unit (e.g., a squad of soldiers, a pack of monsters). If each AI agent makes decisions independently, they might act inefficiently or non-cohesively (e.g., all agents attacking the same pixel on an enemy, or getting stuck trying to occupy the same spot). We need a way to manage their collective actions and goals.

*   **Solution (AIGroupBehavior Pattern):**
    The pattern introduces a central entity, the `AIGroupManager` (often conceptually a 'Group Leader' or 'Squad Commander'), which orchestrates the overall actions of a collection of `AIGroupMember`s. The manager doesn't micro-manage each member's every move, but rather defines the *group's objective* and the *strategy* (behavior) to achieve it. Individual members then interpret and execute their specific part of that strategy.

*   **Key Components in this Example:**

    1.  **`IGroupBehavior` (Interface):**
        *   This is the abstract definition of a 'group strategy' or 'group state'. Each concrete class implementing this interface (like `GroupPatrolBehavior`, `GroupAttackBehavior`) represents a distinct way the group can act.
        *   It typically includes methods for `Enter()`, `Execute()`, and `Exit()`. This design is a clear application of the **State Pattern** (managing group states) combined with the **Strategy Pattern** (interchangeable group strategies).
        *   `Enter(AIGroupManager manager)`: Called once when the behavior starts. Useful for initial setup.
        *   `Execute(AIGroupManager manager)`: Called every frame while the behavior is active. This is where the core logic for *how the group acts* resides. It iterates through the `AIGroupManager`'s members and gives them individual commands.
        *   `Exit(AIGroupManager manager)`: Called once when the behavior ends. Useful for cleanup (e.g., stopping all members).

    2.  **`AIGroupManager` (The Context/Coordinator):**
        *   This is a `MonoBehaviour` that lives in the Unity scene.
        *   It holds a `List` of all `AIGroupMember`s belonging to this group.
        *   It maintains a reference to the `currentGroupBehavior` (an instance of `IGroupBehavior`).
        *   Its `Update()` method primarily delegates execution to the `currentGroupBehavior.Execute(this)`. This means the *active behavior* determines what the group does each frame.
        *   It is responsible for *deciding* when to switch between different `IGroupBehavior`s (e.g., "If enemy detected, switch to Attack; otherwise, patrol"). The `SetBehavior()` method handles this transition gracefully.
        *   It provides common group-level data (like `GroupPatrolPoints` and `GroupTarget`) that any `IGroupBehavior` can access to formulate its commands.

    3.  **`AIGroupMember` (The Individual Agent):**
        *   These are also `MonoBehaviour`s, representing the individual units (soldiers, monsters, NPCs).
        *   They **do not** make group-level strategic decisions. Their role is to receive and execute commands from the `AIGroupManager` (which are issued via the active `IGroupBehavior`).
        *   They implement the low-level actions (e.g., `MoveTo(position)`, `PerformAttackIfReady()`).
        *   They might provide status information back to the manager (e.g., `IsAtDestination`), allowing the `IGroupBehavior` to make informed decisions.

*   **How it works in this example:**
    1.  The `AIGroupManager` starts in the `GroupPatrolBehavior`.
    2.  The `GroupPatrolBehavior`'s `Execute` method, when active, continuously loops through all `AIGroupMember`s. It calculates individual positions around the current group patrol point (with an offset to prevent stacking) and tells each `AIGroupMember` to `MoveTo()` that position. Once all members are near the point, it advances to the next patrol point in the sequence.
    3.  The `AIGroupManager` concurrently runs its `DetectTarget()` logic in its `Update` method. If an enemy (on the specified `targetLayer`) is found within the `targetDetectionRadius`, the manager calls `SetBehavior(new GroupAttackBehavior())`.
    4.  The `GroupAttackBehavior` then becomes the active behavior. Its `Execute` method calculates individual positions in a formation around the `GroupTarget` for each `AIGroupMember`. It tells members to `MoveAndAttack()` to their assigned formation spot and `PerformAttackIfReady()` if they are in range of the target.
    5.  If the `GroupTarget` is destroyed or moves out of range, the `AIGroupManager` (or the `GroupAttackBehavior` itself, which checks for target validity) detects this and switches back to `GroupPatrolBehavior`.

*   **Benefits of the AIGroupBehavior Pattern:**
    *   **Modularity:** New group behaviors (e.g., `GroupDefendBehavior`, `GroupFlankBehavior`, `GroupFleeBehavior`) can be added by simply creating a new class that implements `IGroupBehavior`. Existing manager or member code doesn't need to be modified.
    *   **Separation of Concerns:**
        *   `AIGroupManager`: Focuses on *what* the group should do (high-level decisions, behavior switching).
        *   `IGroupBehavior` implementations: Focus on *how* a specific group goal is achieved (coordinating members for a particular task).
        *   `AIGroupMember`: Focuses on its *individual actions* (low-level movement, attacking).
    *   **Maintainability:** Easier to understand, debug, and modify specific group behaviors without affecting others.
    *   **Scalability:** Efficiently manages complex coordinated actions for potentially large groups of AI agents.