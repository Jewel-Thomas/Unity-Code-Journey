// Unity Design Pattern Example: BehaviorDesignerIntegration
// This script demonstrates the BehaviorDesignerIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

The "BehaviorDesignerIntegration" pattern, while not a formal GoF design pattern, describes the common architectural approach used when integrating a Behavior Tree (BT) system like Behavior Designer into a Unity project. It primarily focuses on how the abstract AI decision-making logic (the behavior tree) interacts with concrete, game-specific components and systems (like movement, combat, health).

This pattern typically involves:
1.  **Component-Based Design:** Core game logic (movement, combat, health) is encapsulated in separate Unity components (MonoBehaviours).
2.  **Interfaces/Abstraction:** These game logic components often implement interfaces (e.g., `IMovementController`, `ICombatController`) to provide a clear contract for AI tasks to interact with, promoting loose coupling.
3.  **Custom Behavior Tree Tasks (Actions/Conditions):** The Behavior Tree system is extended with custom nodes (tasks) that directly call methods on these game logic components. These custom tasks act as **Adapters** or **Wrappers**.
4.  **Dependency Injection:** An AI Manager (often the main AI character script) is responsible for:
    *   Instantiating and configuring the Behavior Tree.
    *   Obtaining references to the necessary game logic components (e.g., `MovementController`, `CombatController`).
    *   **Injecting** these component references into the custom Behavior Tree tasks during their creation or initialization. This is crucial for the integration, allowing tasks to operate on the specific character's components.
5.  **Centralized Orchestration:** The AI Manager ticks the Behavior Tree regularly (e.g., in `Update()`), driving the AI's decision-making and actions.

Below is a complete, self-contained C# Unity script demonstrating this pattern with a simulated Behavior Tree system. It's ready to be dropped into a Unity project.

---

### How to Use This Script in Unity:

1.  **Create a new C# Script** named `BehaviorDesignerIntegrationExample.cs` in your Unity project (e.g., in `Assets/Scripts/`).
2.  **Copy and Paste** the entire code below into this new script, replacing its default content.
3.  **Create a 3D Plane or Terrain** to act as your ground.
4.  **Add a NavMesh**: Go to `Window > AI > Navigation`. In the `Bake` tab, click `Bake` to generate a NavMesh on your ground.
5.  **Create an Empty GameObject** in your scene, rename it to `AI_Enemy`.
    *   Add a `Capsule` component (for visualization).
    *   Add a `NavMeshAgent` component.
    *   Add the `BehaviorDesignerIntegrationExample` script to `AI_Enemy`.
6.  **Create another Empty GameObject** in your scene, rename it to `Player_Target`.
    *   Add a `Capsule` component (for visualization, set its Y scale to 2 to distinguish).
    *   Add the `BehaviorDesignerIntegrationExample` script to `Player_Target` as well. The script will automatically only activate the `Player` logic if `target` is not set on itself.
7.  **Configure `AI_Enemy`**:
    *   Select `AI_Enemy`. In the Inspector, drag the `Player_Target` GameObject into the `Target` field of the `Behavior Designer Integration Example` component.
    *   Adjust `Stopping Distance` as desired (e.g., 1.0).
8.  **Configure `Player_Target`**:
    *   Select `Player_Target`. No `Target` needs to be set here. Its Health component will be used by the AI.
9.  **Run the Scene**: The `AI_Enemy` should now chase and attack the `Player_Target`. Watch the console for debug messages about movement, attacks, and health.

---

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent
using System.Collections.Generic;
using System.Collections; // Required for Coroutines

// Ensure all code is within a namespace to prevent conflicts,
// especially important for larger projects.
namespace BehaviorDesignerIntegrationExample
{
    // --- Core Behavior Tree System (Simulated Behavior Designer Runtime) ---
    // These classes represent the generic framework of a Behavior Tree.
    // In a real Behavior Designer project, you would use their provided base classes.

    /// <summary>
    /// Enum to represent the state of a behavior tree node after execution.
    /// </summary>
    public enum BehaviorNodeResult
    {
        Success, // Node completed its task successfully
        Failure, // Node failed to complete its task
        Running  // Node is currently executing and needs to be ticked again
    }

    /// <summary>
    /// The base interface for any node in the behavior tree.
    /// Defines the contract for executing a node.
    /// </summary>
    public interface IBehaviorNode
    {
        BehaviorNodeResult Tick(); // Executes the node's logic
    }

    /// <summary>
    /// Abstract base class for action nodes (leaf nodes) in the behavior tree.
    /// Action nodes perform concrete tasks in the game world.
    /// In Behavior Designer, this would typically be `BehaviorDesigner.Runtime.Tasks.Action`.
    /// </summary>
    public abstract class BTActionNode : IBehaviorNode
    {
        // Concrete action nodes will implement the Tick method
        public abstract BehaviorNodeResult Tick();
    }

    /// <summary>
    /// Abstract base class for condition nodes (leaf nodes) in the behavior tree.
    /// Condition nodes check for certain states or facts in the game world.
    /// In Behavior Designer, this would typically be `BehaviorDesigner.Runtime.Tasks.Conditional`.
    /// </summary>
    public abstract class BTConditionNode : IBehaviorNode
    {
        // Concrete condition nodes will implement the Tick method
        public abstract BehaviorNodeResult Tick();
    }

    /// <summary>
    /// Abstract base class for composite nodes, which manage child nodes.
    /// Examples: Sequence, Selector, Parallel.
    /// In Behavior Designer, these are built-in nodes.
    /// </summary>
    public abstract class BTCompositeNode : IBehaviorNode
    {
        protected List<IBehaviorNode> children = new List<IBehaviorNode>();
        protected int currentChildIndex = 0; // Tracks which child is currently being processed

        /// <summary>
        /// Adds a child node to this composite.
        /// </summary>
        public void AddChild(IBehaviorNode child)
        {
            if (child != null)
            {
                children.Add(child);
            }
        }

        public abstract BehaviorNodeResult Tick();
    }

    /// <summary>
    /// A Sequence node executes its children in order.
    /// If a child succeeds, it moves to the next.
    /// If a child is running, the sequence is running.
    /// If a child fails, the sequence fails immediately.
    /// The sequence succeeds only if all children succeed.
    /// </summary>
    public class BTSequenceNode : BTCompositeNode
    {
        public override BehaviorNodeResult Tick()
        {
            // If we've processed all children, the sequence has succeeded.
            if (currentChildIndex >= children.Count)
            {
                currentChildIndex = 0; // Reset for next tick if the tree needs to re-evaluate
                return BehaviorNodeResult.Success;
            }

            IBehaviorNode currentChild = children[currentChildIndex];
            BehaviorNodeResult result = currentChild.Tick();

            switch (result)
            {
                case BehaviorNodeResult.Running:
                    // If a child is running, the sequence is still running.
                    return BehaviorNodeResult.Running;
                case BehaviorNodeResult.Failure:
                    // If a child fails, the entire sequence fails.
                    currentChildIndex = 0; // Reset for next tick
                    return BehaviorNodeResult.Failure;
                case BehaviorNodeResult.Success:
                    // If a child succeeds, move to the next child.
                    currentChildIndex++;
                    // Recursively tick to immediately check the next child if the current one succeeded.
                    // This prevents waiting a frame for the next child, mimicking common BT behavior.
                    return Tick();
                default:
                    return BehaviorNodeResult.Failure; // Should not happen
            }
        }
    }

    /// <summary>
    /// A Selector node executes its children in order until one succeeds or is running.
    /// If a child succeeds, the selector succeeds.
    /// If a child is running, the selector is running.
    /// If all children fail, the selector fails.
    /// </summary>
    public class BTSelectorNode : BTCompositeNode
    {
        public override BehaviorNodeResult Tick()
        {
            // Iterate through children
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                IBehaviorNode currentChild = children[i];
                BehaviorNodeResult result = currentChild.Tick();

                switch (result)
                {
                    case BehaviorNodeResult.Running:
                        // If a child is running, the selector is running and remembers this child.
                        currentChildIndex = i;
                        return BehaviorNodeResult.Running;
                    case BehaviorNodeResult.Success:
                        // If a child succeeds, the selector succeeds and resets.
                        currentChildIndex = 0; // Reset for next tick
                        return BehaviorNodeResult.Success;
                    case BehaviorNodeResult.Failure:
                        // If a child fails, move to the next child.
                        // We continue the loop to try the next child.
                        break;
                }
            }

            // If all children failed, the selector fails.
            currentChildIndex = 0; // Reset for next tick
            return BehaviorNodeResult.Failure;
        }
    }

    /// <summary>
    /// The main Behavior Tree class. It holds the root node and provides
    /// the entry point for ticking the tree.
    /// </summary>
    public class BehaviorTree
    {
        private IBehaviorNode rootNode;

        public BehaviorTree(IBehaviorNode root)
        {
            rootNode = root;
        }

        /// <summary>
        /// Executes the behavior tree logic for one frame.
        /// </summary>
        /// <returns>The result of the root node's tick.</returns>
        public BehaviorNodeResult Tick()
        {
            if (rootNode == null)
            {
                Debug.LogWarning("Behavior Tree has no root node.");
                return BehaviorNodeResult.Failure;
            }
            return rootNode.Tick();
        }
    }

    // --- Game Logic Interfaces (Core Game Components) ---
    // These interfaces define contracts for game features, allowing AI tasks
    // to interact with them without knowing their specific implementation details.

    /// <summary>
    /// Interface for character movement functionality.
    /// This abstracts the 'how' of movement from the 'what' of AI decisions.
    /// </summary>
    public interface IMovementController
    {
        bool MoveTo(Vector3 targetPosition); // Initiates movement to a target. Returns true if movement started/is ongoing.
        bool IsMoving { get; }               // True if the character is currently moving.
        void StopMoving();                   // Stops current movement.
        float RemainingDistance { get; }     // Distance to target if moving.
    }

    /// <summary>
    /// Interface for character combat functionality.
    /// This separates attack logic from AI decision-making.
    /// </summary>
    public interface ICombatController
    {
        bool Attack(Transform target); // Initiates an attack on the target. Returns true if attack started.
        float AttackRange { get; }     // The maximum range at which this character can attack.
        bool CanAttack { get; }        // True if the character is ready to attack (e.g., not on cooldown).
    }

    // --- Game Logic Implementations (Concrete Unity Components) ---
    // These are actual MonoBehaviours that implement the game feature interfaces.

    /// <summary>
    /// Concrete implementation of IMovementController using Unity's NavMeshAgent.
    /// This component handles the actual physics and pathfinding for character movement.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MovementController : MonoBehaviour, IMovementController
    {
        private NavMeshAgent navMeshAgent;

        // Public properties to reflect the current state of movement.
        public bool IsMoving => navMeshAgent.hasPath && !navMeshAgent.isStopped && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance;
        public float RemainingDistance => navMeshAgent.remainingDistance;

        void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        /// <summary>
        /// Attempts to move the character to a target position.
        /// </summary>
        /// <param name="targetPosition">The world position to move to.</param>
        /// <returns>True if the path was successfully set and movement started, false otherwise.</returns>
        public bool MoveTo(Vector3 targetPosition)
        {
            // Only set destination if not already moving towards it or if stopped.
            if (navMeshAgent.isStopped || Vector3.Distance(navMeshAgent.destination, targetPosition) > 0.1f) // Small epsilon check
            {
                navMeshAgent.isStopped = false;
                bool pathSet = navMeshAgent.SetDestination(targetPosition);
                if (!pathSet)
                {
                    Debug.LogWarning($"{gameObject.name}: Could not set path to {targetPosition}");
                }
                return pathSet;
            }
            return true; // Already moving towards the target
        }

        /// <summary>
        /// Stops the character's current movement.
        /// </summary>
        public void StopMoving()
        {
            if (navMeshAgent.isOnNavMesh && !navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }
    }

    /// <summary>
    /// Concrete implementation of ICombatController.
    /// This component handles the actual attack animations, cooldowns, and damage application.
    /// </summary>
    public class CombatController : MonoBehaviour, ICombatController
    {
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackDamage = 10f;

        private bool isOnCooldown = false;

        public float AttackRange => attackRange;
        public bool CanAttack => !isOnCooldown;

        /// <summary>
        /// Initiates an attack on the specified target.
        /// </summary>
        /// <param name="target">The transform of the target to attack.</param>
        /// <returns>True if the attack was successfully initiated, false if on cooldown or no target.</returns>
        public bool Attack(Transform target)
        {
            if (!CanAttack || target == null)
            {
                return false;
            }

            Debug.Log($"{gameObject.name} attacking {target.name} for {attackDamage} damage!");
            // In a real game, this would trigger animations, play sound, apply damage, etc.
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage);
            }

            StartCoroutine(AttackCooldownRoutine());
            return true;
        }

        private IEnumerator AttackCooldownRoutine()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(attackCooldown);
            isOnCooldown = false;
        }
    }

    /// <summary>
    /// Simple Health component for demonstration purposes.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);
            Debug.Log($"{gameObject.name} took {damage} damage. Current Health: {currentHealth}");

            if (IsDead)
            {
                Debug.Log($"{gameObject.name} has been defeated!");
                // You might trigger death animations, disable components, etc. here.
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            Debug.Log($"{gameObject.name} healed for {amount}. Current Health: {currentHealth}");
        }
    }

    // --- Custom Behavior Tree Nodes (Adapters/Wrappers) ---
    // These are specific implementations of BTActionNode/BTConditionNode that interact
    // with our game logic components. In Behavior Designer, these would be your
    // custom task scripts.

    /// <summary>
    /// Custom Behavior Tree Action Node: MoveToTarget.
    /// This node integrates with the game's IMovementController to move the AI character.
    ///
    /// In Behavior Designer, this would be a custom Action script inheriting from `BehaviorDesigner.Runtime.Tasks.Action`.
    /// </summary>
    public class MoveToTargetAction : BTActionNode
    {
        private IMovementController movementController;
        private Transform selfTransform;     // The AI character's transform
        private Transform targetTransform;   // The target's transform
        private float stopDistance;          // How close to get to the target before stopping

        /// <summary>
        /// Constructor for the MoveToTargetAction.
        /// This demonstrates **Dependency Injection**: this node receives references to the game components
        /// it needs to operate on from the AICharacter (the integrator) during its construction.
        /// This decouples the task from needing to 'find' these components itself.
        /// </summary>
        public MoveToTargetAction(IMovementController movement, Transform self, Transform target, float stopDist)
        {
            movementController = movement;
            selfTransform = self;
            targetTransform = target;
            stopDistance = stopDist;
        }

        public override BehaviorNodeResult Tick()
        {
            if (targetTransform == null || movementController == null)
            {
                Debug.LogWarning("MoveToTargetAction: Missing target or movement controller. Failing.");
                return BehaviorNodeResult.Failure;
            }

            // Calculate distance to target, considering only XZ plane for ground movement
            float distanceToTarget = Vector3.Distance(new Vector3(selfTransform.position.x, 0, selfTransform.position.z),
                                                      new Vector3(targetTransform.position.x, 0, targetTransform.position.z));

            // If we are already close enough, stop moving and succeed.
            if (distanceToTarget <= stopDistance)
            {
                movementController.StopMoving();
                return BehaviorNodeResult.Success;
            }

            // If not close enough, keep moving.
            if (movementController.MoveTo(targetTransform.position))
            {
                // If movement is still ongoing, the task is running.
                return BehaviorNodeResult.Running;
            }
            else
            {
                // If MoveTo failed (e.g., target unreachable), the task fails.
                Debug.LogWarning($"MoveToTargetAction: Failed to initiate movement to target {targetTransform.name}. Failing.");
                return BehaviorNodeResult.Failure;
            }
        }
    }

    /// <summary>
    /// Custom Behavior Tree Action Node: AttackTarget.
    /// This node integrates with the game's ICombatController to perform an attack.
    ///
    /// In Behavior Designer, this would be a custom Action script inheriting from `BehaviorDesigner.Runtime.Tasks.Action`.
    /// </summary>
    public class AttackTargetAction : BTActionNode
    {
        private ICombatController combatController;
        private Transform targetTransform; // The target's transform

        /// <summary>
        /// Constructor for the AttackTargetAction.
        /// Demonstrates dependency injection.
        /// </summary>
        public AttackTargetAction(ICombatController combat, Transform target)
        {
            combatController = combat;
            targetTransform = target;
        }

        public override BehaviorNodeResult Tick()
        {
            if (targetTransform == null || combatController == null)
            {
                Debug.LogWarning("AttackTargetAction: Missing target or combat controller. Failing.");
                return BehaviorNodeResult.Failure;
            }

            if (combatController.CanAttack)
            {
                if (combatController.Attack(targetTransform))
                {
                    // Attack was initiated. The node succeeds immediately and lets the CombatController handle cooldown.
                    // If you needed to wait for an attack animation to finish, this node would return Running.
                    return BehaviorNodeResult.Success;
                }
                else
                {
                    // Attack failed for some reason (e.g., target moved out of range between condition check and action tick)
                    return BehaviorNodeResult.Failure;
                }
            }
            else
            {
                // Combat controller is on cooldown or otherwise unable to attack right now.
                // The task is Running because it's waiting for the combat controller to be ready.
                // This allows the BT to effectively 'wait' without blocking.
                return BehaviorNodeResult.Running;
            }
        }
    }

    /// <summary>
    /// Custom Behavior Tree Condition Node: IsTargetInAttackRange.
    /// This node checks if the AI character is within attack range of its target.
    ///
    /// In Behavior Designer, this would be a custom Conditional script inheriting from `BehaviorDesigner.Runtime.Tasks.Conditional`.
    /// </summary>
    public class IsTargetInAttackRangeCondition : BTConditionNode
    {
        private Transform selfTransform;     // The AI character's transform
        private Transform targetTransform;   // The target's transform
        private float attackRange;           // The range to check

        /// <summary>
        /// Constructor for the IsTargetInAttackRangeCondition.
        /// Demonstrates dependency injection.
        /// </summary>
        public IsTargetInAttackRangeCondition(Transform self, Transform target, float range)
        {
            selfTransform = self;
            targetTransform = target;
            attackRange = range;
        }

        public override BehaviorNodeResult Tick()
        {
            if (targetTransform == null || selfTransform == null)
            {
                // Debug.LogWarning("IsTargetInAttackRangeCondition: Missing target or self transform. Failing.");
                return BehaviorNodeResult.Failure;
            }

            // Calculate distance, again primarily on the XZ plane for ground range checks.
            float distance = Vector3.Distance(new Vector3(selfTransform.position.x, 0, selfTransform.position.z),
                                              new Vector3(targetTransform.position.x, 0, targetTransform.position.z));

            if (distance <= attackRange)
            {
                return BehaviorNodeResult.Success;
            }
            else
            {
                return BehaviorNodeResult.Failure;
            }
        }
    }

    // --- The Integrator (AICharacter / Player) ---
    // This MonoBehaviour acts as the primary integration point.

    /// <summary>
    /// This script acts as the integrator for an AI agent, demonstrating the
    /// 'BehaviorDesignerIntegration' pattern.
    ///
    /// If a 'target' is assigned, this GameObject will function as an AI Character,
    /// managing and ticking its Behavior Tree.
    /// If no 'target' is assigned, this GameObject will function as a simple Player_Target
    /// for the AI to interact with, having only a Health component.
    ///
    /// It's responsible for:
    /// 1. Holding references to all necessary game components (MovementController, CombatController, Health).
    /// 2. Constructing the Behavior Tree at runtime.
    /// 3. Injecting dependencies (references to game components) into custom Behavior Tree nodes.
    /// 4. Ticking the Behavior Tree every frame.
    /// </summary>
    [RequireComponent(typeof(Health))] // All characters need health
    public class BehaviorDesignerIntegrationExample : MonoBehaviour
    {
        [Header("AI Configuration (Assign Target for AI Mode)")]
        [Tooltip("The target for this AI character to pursue and attack. If null, this object is a simple player target.")]
        [SerializeField] private Transform target;

        [Tooltip("Distance from target at which the AI will stop moving and consider attacking.")]
        [SerializeField] private float stoppingDistance = 1.0f; // Must be less than CombatController's AttackRange

        // References to game-specific components, which are managed by this AICharacter.
        private IMovementController movementController;
        private ICombatController combatController;
        private Health health;

        // The Behavior Tree instance that will drive this AI's decisions.
        private BehaviorTree behaviorTree;

        // Flag to know if this instance is an AI or a simple player target.
        private bool isAI = false;

        // --- Unity Lifecycle Methods ---
        void Awake()
        {
            health = GetComponent<Health>();

            // Determine if this GameObject should be an AI or a simple target.
            if (target != null)
            {
                isAI = true;
                // AI characters need movement and combat capabilities
                gameObject.AddComponent<MovementController>();
                gameObject.AddComponent<CombatController>();
                gameObject.AddComponent<NavMeshAgent>(); // NavMeshAgent required by MovementController

                movementController = GetComponent<IMovementController>();
                combatController = GetComponent<ICombatController>();

                // Configure NavMeshAgent's stopping distance.
                // It's important for this to be less than or equal to the attack range.
                NavMeshAgent agent = GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.stoppingDistance = stoppingDistance;
                    if (combatController != null && agent.stoppingDistance > combatController.AttackRange)
                    {
                        Debug.LogWarning($"AICharacter '{gameObject.name}': stoppingDistance ({stoppingDistance}) is greater than CombatController's AttackRange ({combatController.AttackRange}). This might lead to the AI never attacking. Adjust stoppingDistance or AttackRange on the CombatController.");
                    }
                }
            }
            else
            {
                isAI = false;
                Debug.Log($"'{gameObject.name}' initialized as a Player Target.");
            }
        }

        void Start()
        {
            if (!isAI)
            {
                Debug.Log($"Player Target '{gameObject.name}' health: {health.CurrentHealth}/{health.MaxHealth}");
                return;
            }

            // --- BEHAVIOR DESIGNER INTEGRATION PATTERN: Constructing the Behavior Tree ---
            // This is the core of the pattern. We instantiate custom BT nodes and inject
            // references to our game-specific components into them.

            // 1. Create a Condition node: Is the target within attack range?
            //    It needs: the AI's own transform, the target's transform, and the combat controller's attack range.
            IsTargetInAttackRangeCondition isInRangeCondition = new IsTargetInAttackRangeCondition(
                this.transform,
                target,
                combatController.AttackRange
            );

            // 2. Create an Action node: Attack the target.
            //    It needs: the combat controller and the target's transform.
            AttackTargetAction attackAction = new AttackTargetAction(
                combatController,
                target
            );

            // 3. Create an Action node: Move to the target.
            //    It needs: the movement controller, the AI's own transform, the target's transform, and a stop distance.
            MoveToTargetAction moveToTargetAction = new MoveToTargetAction(
                movementController,
                this.transform,
                target,
                stoppingDistance
            );

            // 4. Construct the Behavior Tree logic using composite nodes (e.g., Selector, Sequence).
            //    We'll build a common "Chase and Attack" AI tree:
            //
            //    Root (Selector):
            //      Sequence (Try to attack if in range):
            //        - IsTargetInAttackRangeCondition (Succeeds if in range, Fails otherwise)
            //        - AttackTargetAction (Executes attack)
            //      MoveToTargetAction (Fallback: if not in range or attack failed, move towards target)

            // Sub-Sequence for attacking (only if in range)
            BTSequenceNode attackSequence = new BTSequenceNode();
            attackSequence.AddChild(isInRangeCondition); // First check if in range
            attackSequence.AddChild(attackAction);       // Then execute attack

            // Root Selector: Tries children in order. The first one that succeeds or runs determines the Selector's state.
            BTSelectorNode rootSelector = new BTSelectorNode();
            rootSelector.AddChild(attackSequence);    // Priority 1: If in range, attack.
            rootSelector.AddChild(moveToTargetAction); // Priority 2: If not attacking (not in range/attack failed), then move.

            // Initialize the Behavior Tree with the root node.
            behaviorTree = new BehaviorTree(rootSelector);

            Debug.Log($"AICharacter '{gameObject.name}' initialized with target: {target.name}. Health: {health.CurrentHealth}/{health.MaxHealth}");
        }

        void Update()
        {
            if (!isAI)
            {
                // This instance is a player target, no AI logic needed.
                return;
            }

            // The AI character is dead, stop processing its behavior.
            if (health.IsDead)
            {
                movementController?.StopMoving();
                // Optionally disable this script or its components.
                enabled = false;
                Debug.Log($"AI '{gameObject.name}' is dead, stopping behavior tree.");
                return;
            }

            // Tick the behavior tree. This drives the AI's decisions and actions each frame.
            BehaviorNodeResult result = behaviorTree.Tick();

            // Optional: Debugging the overall tree state
            // if (result == BehaviorNodeResult.Running)
            // {
            //     Debug.Log($"AICharacter '{gameObject.name}' BT: Running");
            // }
            // else if (result == BehaviorNodeResult.Success)
            // {
            //     // The tree might briefly succeed if all tasks complete, then reset.
            //     // In a continuous AI, it will mostly be Running or Failure.
            //     Debug.Log($"AICharacter '{gameObject.name}' BT: Success (will re-evaluate next frame)");
            // }
            // else if (result == BehaviorNodeResult.Failure)
            // {
            //     Debug.LogWarning($"AICharacter '{gameObject.name}' BT: Failed! (e.g., target missing/unreachable)");
            // }
        }
    }
}
```