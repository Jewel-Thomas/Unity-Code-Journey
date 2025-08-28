// Unity Design Pattern Example: BehaviorTree
// This script demonstrates the BehaviorTree pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script provides a practical and educational implementation of the **Behavior Tree** design pattern. It's designed to be dropped directly into a Unity project, offering a solid foundation for creating complex AI behaviors.

---

### How to Use This Script in Unity:

1.  **Create a C# Script:** In your Unity project, right-click in the Project window -> Create -> C# Script. Name it `BehaviorTreeRunner`.
2.  **Copy and Paste:** Replace the entire content of the new `BehaviorTreeRunner.cs` file with the code provided below.
3.  **Create an AI Agent:**
    *   Create an empty GameObject in your scene (e.g., named "AIAgent").
    *   Attach the `BehaviorTreeRunner` script to this GameObject.
4.  **Set Up Scene Elements:**
    *   **Patrol Points:** Create several empty GameObjects (e.g., "PatrolPoint1", "PatrolPoint2") and position them in your scene. Drag these into the `Patrol Points` array slot on your "AIAgent" in the Inspector.
    *   **Food:** Create a Cube or Sphere, tag it `Food` (select the GameObject, then in Inspector -> Tag -> Add Tag -> create "Food", then select it for the GameObject). Position it somewhere.
    *   **Enemy:** Create another Cube or Sphere, tag it `Enemy` (similarly, add and select "Enemy" tag). Position it somewhere. Ensure it has a Collider (e.g., Box Collider, Sphere Collider) and set its Layer to `Enemy` (Add Layer -> create "Enemy", then select it for the GameObject).
5.  **Run the Scene:** Observe the debug logs and the agent's behavior (movement, pauses) in the Scene view (with Gizmos enabled) and Console.

---

### `BehaviorTreeRunner.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action and Func delegates

// ====================================================================================================
// BEHAVIOR TREE DESIGN PATTERN IMPLEMENTATION FOR UNITY
// ====================================================================================================

// --- 1. Core Node Definitions ---

/// <summary>
/// The base class for all nodes in the Behavior Tree.
/// Defines the common interface and status for nodes.
/// </summary>
public abstract class Node
{
    // The current status of the node's execution.
    public enum Status
    {
        Running,    // Node is currently executing an asynchronous task (e.g., waiting, moving over time).
        Success,    // Node completed its task successfully.
        Failure     // Node failed to complete its task or condition was false.
    }

    /// <summary>
    /// Evaluates the node's logic. This is the core method for any Behavior Tree node.
    /// Child classes must implement this to define their specific behavior.
    /// </summary>
    /// <returns>The status of the node after evaluation (Success, Failure, or Running).</returns>
    public abstract Status Evaluate();

    /// <summary>
    /// Resets the internal state of the node. This is crucial for stateful nodes (like WaitNode, Sequence, Selector)
    /// that need to clear their temporary data (e.g., timers, child indices) when a branch finishes
    /// or when the entire tree is reset.
    /// </summary>
    public virtual void Reset() { } // Default empty implementation for stateless nodes.
}

/// <summary>
/// Base class for nodes that can have multiple child nodes (e.g., Sequence, Selector).
/// Manages the collection of children.
/// </summary>
public abstract class CompositeNode : Node
{
    protected List<Node> children = new List<Node>();

    /// <summary>
    /// Adds a child node to this composite node.
    /// </summary>
    /// <param name="child">The node to add as a child.</param>
    public void AddChild(Node child)
    {
        children.Add(child);
    }

    /// <summary>
    /// Resets all children nodes recursively.
    /// </summary>
    public override void Reset()
    {
        foreach (var child in children)
        {
            child.Reset();
        }
    }
}

/// <summary>
/// Base class for nodes that decorate (modify) the behavior of a single child node.
/// </summary>
public abstract class DecoratorNode : Node
{
    protected Node child;

    /// <summary>
    /// Constructor for DecoratorNode.
    /// </summary>
    /// <param name="child">The single child node this decorator wraps.</param>
    public DecoratorNode(Node child)
    {
        this.child = child;
    }

    /// <summary>
    /// Resets the child node.
    /// </summary>
    public override void Reset()
    {
        child.Reset();
    }
}

/// <summary>
/// Base class for leaf nodes, which perform actual actions or check conditions.
/// These nodes have no children.
/// </summary>
public abstract class LeafNode : Node
{
    // No specific properties or methods beyond Node's basic requirements,
    // but serves as a conceptual distinction in the tree structure.
}

// --- 2. Concrete Composite Nodes ---

/// <summary>
/// A Sequence node executes its children one by one in order.
/// It succeeds if all children succeed.
/// It fails if any child fails.
/// If a child returns Running, the Sequence also returns Running and remembers its state to resume from that child next time.
/// This makes Sequence nodes stateful.
/// </summary>
public class Sequence : CompositeNode
{
    private int currentChildIndex = 0; // State: Tracks which child to execute next.

    public Sequence(params Node[] nodes)
    {
        foreach (var node in nodes)
        {
            AddChild(node);
        }
    }

    public override Status Evaluate()
    {
        // Iterate through children from the current index.
        for (int i = currentChildIndex; i < children.Count; i++)
        {
            Node.Status childStatus = children[i].Evaluate();

            switch (childStatus)
            {
                case Status.Running:
                    currentChildIndex = i; // Remember to resume from this child next time.
                    return Status.Running; // Sequence is still running.
                case Status.Failure:
                    ResetChildrenState(); // Sequence fails, so reset state for future evaluations.
                    return Status.Failure; // Sequence fails if any child fails.
                case Status.Success:
                    // Child succeeded, move to the next child in the loop.
                    continue;
            }
        }

        // If all children succeeded, the sequence succeeds.
        ResetChildrenState(); // Sequence succeeds, so reset state for future evaluations.
        return Status.Success;
    }

    /// <summary>
    /// Resets the internal index and all children's state.
    /// Called when the sequence completes (succeeds or fails) or when the entire tree is reset.
    /// </summary>
    private void ResetChildrenState()
    {
        currentChildIndex = 0;
        foreach (var child in children)
        {
            child.Reset();
        }
    }

    /// <summary>
    /// Overrides the base Reset to also reset this sequence's internal state.
    /// </summary>
    public override void Reset()
    {
        ResetChildrenState();
        base.Reset(); // Call base reset to recursively reset all children.
    }
}

/// <summary>
/// A Selector node executes its children one by one in order.
/// It succeeds if any child succeeds.
/// It fails if all children fail.
/// If a child returns Running, the Selector also returns Running and remembers its state to resume from that child next time.
/// This makes Selector nodes stateful.
/// </summary>
public class Selector : CompositeNode
{
    private int currentChildIndex = 0; // State: Tracks which child to execute next.

    public Selector(params Node[] nodes)
    {
        foreach (var node in nodes)
        {
            AddChild(node);
        }
    }

    public override Status Evaluate()
    {
        // Iterate through children from the current index.
        for (int i = currentChildIndex; i < children.Count; i++)
        {
            Node.Status childStatus = children[i].Evaluate();

            switch (childStatus)
            {
                case Status.Running:
                    currentChildIndex = i; // Remember to resume from this child next time.
                    return Status.Running; // Selector is still running.
                case Status.Success:
                    ResetChildrenState(); // Selector succeeds, so reset state for future evaluations.
                    return Status.Success; // Selector succeeds if any child succeeds.
                case Status.Failure:
                    // Child failed, move to the next child to try in the loop.
                    continue;
            }
        }

        // If all children failed, the selector fails.
        ResetChildrenState(); // Selector fails, so reset state for future evaluations.
        return Status.Failure;
    }

    /// <summary>
    /// Resets the internal index and all children's state.
    /// Called when the selector completes (succeeds or fails) or when the entire tree is reset.
    /// </summary>
    private void ResetChildrenState()
    {
        currentChildIndex = 0;
        foreach (var child in children)
        {
            child.Reset();
        }
    }

    /// <summary>
    /// Overrides the base Reset to also reset this selector's internal state.
    /// </summary>
    public override void Reset()
    {
        ResetChildrenState();
        base.Reset(); // Call base reset to recursively reset all children.
    }
}

// --- 3. Concrete Decorator Nodes ---

/// <summary>
/// Inverts the status of its child node.
/// Success becomes Failure, Failure becomes Success. Running remains Running.
/// </summary>
public class Inverter : DecoratorNode
{
    public Inverter(Node child) : base(child) { }

    public override Status Evaluate()
    {
        switch (child.Evaluate())
        {
            case Status.Success:
                return Status.Failure;
            case Status.Failure:
                return Status.Success;
            case Status.Running:
                return Status.Running;
            default:
                Debug.LogError("Inverter: Unexpected child status.");
                return Status.Failure;
        }
    }
}

/// <summary>
/// Always returns Success, regardless of its child's status (unless the child is Running).
/// Useful for making optional branches that should not cause the parent to fail.
/// If the child returns Running, this decorator also returns Running until the child completes.
/// Once the child completes (Success or Failure), this decorator converts it to Success.
/// </summary>
public class AlwaysSucceed : DecoratorNode
{
    public AlwaysSucceed(Node child) : base(child) { }

    public override Status Evaluate()
    {
        Node.Status childStatus = child.Evaluate();
        if (childStatus == Status.Running)
        {
            return Status.Running; // Continue running if child is still running.
        }
        return Status.Success; // Otherwise, always succeed.
    }
}

/// <summary>
/// Decorator that repeatedly executes its child node.
/// It effectively creates an infinite loop for a child that keeps returning Success.
/// If the child returns Failure, the Loop breaks and returns Failure.
/// If child returns Running, the Loop also returns Running.
/// An optional 'maxLoops' can limit the number of successful executions.
/// This node is stateful.
/// </summary>
public class Loop : DecoratorNode
{
    private int maxLoops; // -1 for infinite loops.
    private int currentLoops = 0;

    public Loop(Node child, int maxLoops = -1) : base(child)
    {
        this.maxLoops = maxLoops;
    }

    public override Status Evaluate()
    {
        // If maxLoops is set and we've reached it, consider the loop complete (Success).
        if (maxLoops != -1 && currentLoops >= maxLoops)
        {
            Reset(); // Ensure internal state is reset if we succeed and break the loop.
            return Status.Success;
        }

        Node.Status childStatus = child.Evaluate();

        switch (childStatus)
        {
            case Status.Success:
                currentLoops++;
                child.Reset(); // Reset the child so it can be re-evaluated from a fresh state next time.
                return Status.Running; // Keep looping by returning Running.
            case Status.Failure:
                Reset(); // Loop breaks and fails, so reset state.
                return Status.Failure; // Loop breaks and fails if child fails.
            case Status.Running:
                return Status.Running; // Keep running if child is running.
            default:
                Debug.LogError("Loop: Unexpected child status.");
                return Status.Failure;
        }
    }

    /// <summary>
    /// Resets the internal loop count and the child node.
    /// </summary>
    public override void Reset()
    {
        currentLoops = 0;
        base.Reset(); // Also reset the child.
    }
}

/// <summary>
/// Decorator that retries its child node a certain number of times upon failure before finally failing itself.
/// If the child succeeds, this decorator succeeds immediately.
/// If child returns Running, this decorator also returns Running.
/// This node is stateful.
/// </summary>
public class Retry : DecoratorNode
{
    private int maxRetries;
    private int currentRetries = 0;

    public Retry(Node child, int maxRetries) : base(child)
    {
        this.maxRetries = maxRetries;
    }

    public override Status Evaluate()
    {
        Node.Status childStatus = child.Evaluate();

        switch (childStatus)
        {
            case Status.Success:
                currentRetries = 0; // Reset retry count on success.
                return Status.Success;
            case Status.Failure:
                if (currentRetries < maxRetries)
                {
                    currentRetries++;
                    child.Reset(); // Reset the child to try again from fresh state.
                    return Status.Running; // From the parent's perspective, we are still trying.
                }
                else
                {
                    Reset(); // Failed after max retries, reset state.
                    return Status.Failure; // Finally failed.
                }
            case Status.Running:
                return Status.Running; // Keep running if child is running.
            default:
                Debug.LogError("Retry: Unexpected child status.");
                return Status.Failure;
        }
    }

    /// <summary>
    /// Resets the internal retry count and the child node.
    /// </summary>
    public override void Reset()
    {
        currentRetries = 0;
        base.Reset();
    }
}

// --- 4. Concrete Leaf Nodes (Actions & Conditions) ---

/// <summary>
/// A LeafNode that checks a boolean condition.
/// Uses a `Func<bool>` delegate to allow flexible condition checks defined externally.
/// </summary>
public class CheckCondition : LeafNode
{
    private Func<bool> condition; // A delegate (function pointer) for the condition to check.

    public CheckCondition(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override Status Evaluate()
    {
        return condition() ? Status.Success : Status.Failure;
    }
}

/// <summary>
/// A LeafNode that performs an action.
/// Uses an `Action` delegate to allow flexible actions defined externally.
/// It always returns Success after performing the action.
/// </summary>
public class DoAction : LeafNode
{
    private Action action; // A delegate for the action to perform.

    public DoAction(Action action)
    {
        this.action = action;
    }

    public override Status Evaluate()
    {
        action();
        return Status.Success;
    }
}

/// <summary>
/// A LeafNode that waits for a specified duration.
/// This is a stateful node, as it needs to track time across multiple evaluations.
/// Returns Running until the duration is met, then Success.
/// </summary>
public class WaitNode : LeafNode
{
    private float waitDuration;
    private float startTime;
    private bool isWaiting = false;

    public WaitNode(float duration)
    {
        waitDuration = duration;
    }

    public override Status Evaluate()
    {
        if (!isWaiting)
        {
            startTime = Time.time;
            isWaiting = true;
            Debug.Log($"WaitNode: Started waiting for {waitDuration:F2}s.");
        }

        if (Time.time - startTime >= waitDuration)
        {
            Debug.Log("WaitNode: Finished waiting. Success.");
            isWaiting = false; // Reset for next time this node is evaluated.
            return Status.Success;
        }

        // Optional: log remaining time only if significant change or for debugging.
        // Debug.Log($"WaitNode: Still waiting... {waitDuration - (Time.time - startTime):F2}s remaining.");
        return Status.Running;
    }

    /// <summary>
    /// Resets the internal waiting state.
    /// </summary>
    public override void Reset()
    {
        isWaiting = false;
        Debug.Log("WaitNode: Reset.");
    }
}

/// <summary>
/// A LeafNode that logs a message to the Unity console.
/// Always returns Success.
/// </summary>
public class LogNode : LeafNode
{
    public enum LogType { Info, Warning, Error }

    private string message;
    private LogType logType;

    public LogNode(string message, LogType type = LogType.Info)
    {
        this.message = message;
        this.logType = type;
    }

    public override Status Evaluate()
    {
        switch (logType)
        {
            case LogType.Info:
                Debug.Log(message);
                break;
            case LogType.Warning:
                Debug.LogWarning(message);
                break;
            case LogType.Error:
                Debug.LogError(message);
                break;
        }
        return Status.Success;
    }
}

// ====================================================================================================
// BEHAVIOR TREE RUNNER (MONOBEHAVIOUR)
// ====================================================================================================

/// <summary>
/// The main Behavior Tree controller component for Unity.
/// This component should be attached to a GameObject that will run the Behavior Tree.
/// It initializes the tree structure and evaluates it periodically.
/// </summary>
public class BehaviorTreeRunner : MonoBehaviour
{
    private Node rootNode; // The root node of our Behavior Tree.

    [Tooltip("How often the Behavior Tree's root node is evaluated (in seconds).")]
    [SerializeField] private float evaluationInterval = 0.2f; // Prevents excessive evaluations per frame.
    private float lastEvaluationTime;

    // --- Example Character Properties (for demonstration) ---
    // In a real project, these properties would typically be part of a separate AI Agent class
    // that the Behavior Tree interacts with.
    [Header("Agent Stats")]
    public float health = 100f;
    public float hunger = 0f; // 0 = not hungry, 100 = starving
    public float movementSpeed = 2f;

    [Header("Agent Senses & Targets")]
    public GameObject targetFood;
    public GameObject currentTargetEnemy;
    public float attackRange = 2f;
    public float sightRange = 10f;
    public float eatRange = 1f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;


    void Start()
    {
        // Initialize the Behavior Tree structure.
        // This is where you define your AI's decision-making logic.
        rootNode = SetupBehaviorTree();

        // Initialize the timer for periodic evaluation.
        lastEvaluationTime = Time.time;

        if (rootNode == null)
        {
            Debug.LogError("Behavior Tree root node is null. Ensure SetupBehaviorTree() returns a valid node.");
        }
        else
        {
            Debug.Log("Behavior Tree initialized.");
        }
    }

    void Update()
    {
        // Evaluate the tree at a fixed interval to prevent too many evaluations per frame,
        // especially important for complex trees or performance-critical scenarios.
        if (rootNode != null && Time.time - lastEvaluationTime >= evaluationInterval)
        {
            rootNode.Evaluate();
            lastEvaluationTime = Time.time;
        }

        // Example: Hunger increases over time.
        hunger += Time.deltaTime * 2;
        hunger = Mathf.Min(hunger, 100); // Cap hunger at 100.
    }

    /// <summary>
    /// This method defines the entire Behavior Tree structure for our AI agent.
    /// It's the core of how the agent decides what to do, prioritizing tasks.
    /// </summary>
    /// <returns>The root node of the constructed Behavior Tree.</returns>
    private Node SetupBehaviorTree()
    {
        // --- Leaf Nodes (Conditions & Actions) ---
        // These are the "building blocks" that interact with the game world.

        // Conditions (return Success/Failure based on game state):
        var isHungry = new CheckCondition(() => IsHungry());
        var hasTargetFood = new CheckCondition(() => targetFood != null);
        var isFoodInRange = new CheckCondition(() => IsTargetFoodInRange());
        var hasEnemy = new CheckCondition(() => currentTargetEnemy != null);
        var isEnemyInRange = new CheckCondition(() => IsEnemyInRange());
        var isEnemyAlive = new CheckCondition(() => IsEnemyAlive());
        var lowHealth = new CheckCondition(() => health < 30);
        var reachedPatrolPoint = new CheckCondition(() => HasReachedPatrolPoint());

        // Actions (perform a game-world action and typically return Success):
        var findFood = new DoAction(() => FindFood());
        var moveToFood = new DoAction(() => MoveTo(targetFood.transform.position));
        var eatFood = new DoAction(() => EatFood());
        var findEnemy = new DoAction(() => FindEnemy());
        var moveToEnemy = new DoAction(() => MoveTo(currentTargetEnemy.transform.position));
        var attackEnemy = new DoAction(() => AttackEnemy());
        var healSelf = new DoAction(() => HealSelf());
        var patrolToPoint = new DoAction(() => PatrolToCurrentPoint());
        var incrementPatrolPoint = new DoAction(() => currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length);
        var doNothing = new LogNode("Agent is idle.", LogNode.LogType.Info);

        // Debug/Logging Nodes:
        var logEnemyFound = new LogNode("BehaviorTree: Found an enemy! Preparing to attack.", LogNode.LogType.Warning);
        var logFoodFound = new LogNode("BehaviorTree: Found food! Moving to eat.", LogNode.LogType.Info);
        var logPatrolling = new LogNode("BehaviorTree: Agent is patrolling...", LogNode.LogType.Info);
        var logEating = new LogNode("BehaviorTree: Agent is eating.", LogNode.LogType.Info);
        var logAttacking = new LogNode("BehaviorTree: Agent is attacking!", LogNode.LogType.Error);
        var logHealing = new LogNode("BehaviorTree: Agent is healing.", LogNode.LogType.Info);


        // --- Behavior Tree Structure (Composites and Decorators) ---

        // 1. Emergency Healing Branch (Highest Priority)
        // If health is low, agent attempts to heal itself.
        var emergencyHealBranch = new Sequence(
            lowHealth,
            logHealing,
            healSelf,
            new WaitNode(2f) // Simulate healing time. This is a stateful node.
        );

        // 2. Attack Enemy Branch (Second Highest Priority)
        // If an enemy is detected and alive, agent will prioritize attacking it.
        var attackBranch = new Sequence(
            hasEnemy, // Condition: Is there an enemy target?
            isEnemyAlive, // Condition: Is the enemy still alive?
            logEnemyFound,
            new Selector( // Selector: Try to attack, or move closer if out of range.
                new Sequence( // Sequence: If in range, attack.
                    isEnemyInRange, // Condition: Is enemy within attack range?
                    logAttacking,
                    attackEnemy,
                    new WaitNode(1.5f) // Simulate attack cooldown.
                ),
                moveToEnemy // Action: If not in range, move closer to enemy.
            )
        );

        // 3. Find and Eat Food Branch
        // If hungry, agent will try to find and eat food.
        var eatFoodBranch = new Sequence(
            isHungry, // Condition: Is the agent hungry?
            new Selector( // Selector: Try to eat existing food, or find new food.
                new Sequence( // Sequence: If there's a target food, try to eat it.
                    hasTargetFood, // Condition: Is there a target food object?
                    new Selector( // Selector: Try to eat, or move closer to food.
                        new Sequence( // Sequence: If food is in range, eat it.
                            isFoodInRange, // Condition: Is food within eating range?
                            logEating,
                            eatFood,
                            new WaitNode(3f) // Simulate eating time.
                        ),
                        moveToFood // Action: If food is not in range, move closer.
                    )
                ),
                new Sequence( // Sequence: If no food target, find food.
                    logFoodFound,
                    findFood // Action: Search for food in the environment.
                )
            )
        );

        // 4. Patrol Branch (Lowest Priority / Default Behavior)
        // If nothing else to do, the agent will continuously patrol between points.
        var singlePatrolSequence = new Sequence(
            logPatrolling,
            patrolToPoint, // Action: Move towards the current patrol point.
            new WaitNode(2f), // Simulate a short pause at each patrol point.
            incrementPatrolPoint // Action: Set the next patrol point.
        );
        // Decorator: Loop this patrol behavior indefinitely.
        var loopingPatrol = new Loop(singlePatrolSequence);


        // The Root of the Behavior Tree: A Selector to prioritize actions.
        // It tries actions in this order: Heal -> Attack -> Eat -> Patrol -> Do Nothing.
        // The first branch to return Success or Running will be chosen.
        var mainBehaviorTree = new Selector(
            emergencyHealBranch,
            attackBranch,
            eatFoodBranch,
            loopingPatrol, // If nothing else is active, agent patrols continuously.
            doNothing // Fallback: If for some reason even patrolling isn't possible, just log idle.
        );

        return mainBehaviorTree;
    }

    // --- Helper Methods (Simulating AI Agent's capabilities and game world interaction) ---
    // In a real project, these would typically be encapsulated within a dedicated AI Agent class
    // or a Manager class, and the Behavior Tree would call these methods via delegates.

    private bool IsHungry()
    {
        bool hungry = hunger > 50;
        // Debug.Log($"IsHungry: {hungry} (Hunger: {hunger:F1})"); // Uncomment for verbose hunger debug
        return hungry;
    }

    private void FindFood()
    {
        Debug.Log("Agent Action: Searching for food...");
        // Simulate finding food: find the closest GameObject tagged "Food".
        // A real implementation might use more complex pathfinding, line-of-sight, etc.
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        float closestDist = float.MaxValue;
        GameObject closestFood = null;

        foreach (var food in foods)
        {
            float dist = Vector3.Distance(transform.position, food.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestFood = food;
            }
        }

        if (closestFood != null)
        {
            targetFood = closestFood;
            Debug.Log($"Agent Action: Found food at: {targetFood.transform.position}");
        }
        else
        {
            Debug.Log("Agent Action: No food found.");
            targetFood = null; // Clear target if no food is found.
        }
    }

    private bool IsTargetFoodInRange()
    {
        if (targetFood == null) return false;
        bool inRange = Vector3.Distance(transform.position, targetFood.transform.position) <= eatRange;
        // Debug.Log($"IsFoodInRange: {inRange} (Dist: {Vector3.Distance(transform.position, targetFood.transform.position):F1})"); // Uncomment for verbose food range debug
        return inRange;
    }

    private void EatFood()
    {
        if (targetFood != null)
        {
            Debug.Log("Agent Action: Eating food!");
            hunger = 0; // Reset hunger.
            Destroy(targetFood); // Consume food by destroying the GameObject.
            targetFood = null; // Clear target after eating.
        }
    }

    private void FindEnemy()
    {
        Debug.Log("Agent Action: Searching for enemy...");
        // Simulate finding an enemy: find the closest GameObject tagged "Enemy" within sight range.
        // Uses Physics.OverlapSphere for simplicity.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightRange, LayerMask.GetMask("Enemy"));
        float closestDist = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (var hitCollider in hitColliders)
        {
            float dist = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = hitCollider.gameObject;
            }
        }

        if (closestEnemy != null)
        {
            currentTargetEnemy = closestEnemy;
            Debug.Log($"Agent Action: Found enemy: {currentTargetEnemy.name} at {currentTargetEnemy.transform.position}");
        }
        else
        {
            Debug.Log("Agent Action: No enemy found.");
            currentTargetEnemy = null; // Clear target if no enemy is found.
        }
    }

    private bool IsEnemyInRange()
    {
        if (currentTargetEnemy == null) return false;
        bool inRange = Vector3.Distance(transform.position, currentTargetEnemy.transform.position) <= attackRange;
        // Debug.Log($"IsEnemyInRange: {inRange} (Dist: {Vector3.Distance(transform.position, currentTargetEnemy.transform.position):F1})"); // Uncomment for verbose enemy range debug
        return inRange;
    }

    private bool IsEnemyAlive()
    {
        // Simple check: if the GameObject reference is not null, assume enemy is alive.
        // In a real game, you would check a specific EnemyHealth component.
        return currentTargetEnemy != null;
    }

    private void AttackEnemy()
    {
        if (currentTargetEnemy != null)
        {
            Debug.Log($"Agent Action: Attacking enemy: {currentTargetEnemy.name}!");
            // Simulate dealing damage to the enemy.
            // For a real game: currentTargetEnemy.GetComponent<EnemyHealth>().TakeDamage(10);
            if (UnityEngine.Random.value < 0.3f) // 30% chance to "kill" the enemy in this example.
            {
                Debug.Log($"Agent Action: Killed enemy: {currentTargetEnemy.name}!");
                Destroy(currentTargetEnemy); // Remove enemy from scene.
                currentTargetEnemy = null; // Clear target.
            }
        }
    }

    private void HealSelf()
    {
        Debug.Log("Agent Action: Healing self!");
        health += 20;
        health = Mathf.Min(health, 100); // Cap health.
    }

    private void PatrolToCurrentPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("Agent Action: No patrol points set!");
            return;
        }

        if (currentPatrolIndex >= patrolPoints.Length || patrolPoints[currentPatrolIndex] == null)
        {
            Debug.LogWarning("Agent Action: Invalid patrol point index or null patrol point!");
            currentPatrolIndex = 0; // Reset to first point or handle error.
            return;
        }

        Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.deltaTime);
        // Debug.Log($"Agent Action: Patrolling towards: {targetPos} (Current: {transform.position})"); // Uncomment for verbose patrol debug
    }

    private bool HasReachedPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || currentPatrolIndex >= patrolPoints.Length || patrolPoints[currentPatrolIndex] == null)
            return false;

        return Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.5f;
    }


    private void MoveTo(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);
        // Debug.Log($"Agent Action: Moving towards: {destination} (Current: {transform.position})"); // Uncomment for verbose move debug
    }


    // --- Gizmos for visualization in the Unity editor ---
    void OnDrawGizmos()
    {
        // Visualize patrol points and paths
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f); // Draw sphere at each point.
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position); // Draw line between points.
                    }
                }
            }
            // Draw line to loop back to the first patrol point.
            if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
            }
        }

        // Visualize attack and sight range around the agent.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // Attack range (red).
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange); // Sight range (yellow).
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, eatRange); // Eat range (green).
    }
}
```