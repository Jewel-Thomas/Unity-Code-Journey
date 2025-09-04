// Unity Design Pattern Example: AIPlanner
// This script demonstrates the AIPlanner pattern in Unity
// Generated automatically - ready to use in your Unity project

The AI Planner design pattern is a powerful approach for developing intelligent agents in games. Instead of hardcoding behavior trees or state machines for every possible scenario, an AI Planner allows an agent to **reason about its goals** and **construct a sequence of actions** (a plan) to achieve them. It works by understanding the current state of the world, what actions are available, what preconditions each action needs, and what effects each action has.

This example provides a complete, practical implementation of the AI Planner pattern in Unity, demonstrating how an NPC can plan to achieve a goal like "Build a House."

---

### AI Planner Design Pattern Explanation

1.  **World State:** Represents the current state of the game world relevant to the AI. This is typically a collection of facts (e.g., "HasWood," "AtTree," "HouseBuilt").
2.  **Goals:** A desired future `WorldState`. The AI will try to find a plan to reach this state.
3.  **Actions:** Discrete, atomic operations the AI can perform. Each action defines:
    *   **Preconditions:** What must be true in the `WorldState` *before* the action can be executed.
    *   **Effects (Postconditions):** What changes in the `WorldState` *after* the action is executed.
    *   **Cost:** A numerical value representing the effort, time, or risk associated with the action.
4.  **Planner:** The core algorithm (often an A* search variant) that takes the current `WorldState`, a `Goal`, and a list of `AvailableActions`, and attempts to find the lowest-cost sequence of actions that transforms the current state into the goal state.
5.  **Agent:** The MonoBehaviour that owns the current `WorldState`, sets its `Goal`, utilizes the `Planner`, and executes the `Actions` in the generated plan.

---

### Real-World Use Case: A Worker AI

Our example demonstrates a `WorkerAI` that needs to `BuildHouse`. To do this, it will need to:
1.  Move to a tree.
2.  Gather wood.
3.  Move to a workshop.
4.  Build the house.

The AI Planner will figure out this sequence automatically based on the defined actions and their preconditions/effects.

---

### Project Setup in Unity

1.  Create a new Unity project or open an existing one.
2.  Create a new C# script named `AIPlannerExample.cs`.
3.  Copy and paste the entire code below into `AIPlannerExample.cs`.
4.  In your scene:
    *   Create an empty GameObject named `WorkerAI`.
    *   Add the `AIAgent` component (from the script) to the `WorkerAI` GameObject.
    *   Create two empty GameObjects named `TreeLocation` and `WorkshopLocation`. Place them somewhere distinct in your scene.
    *   Select the `WorkerAI` GameObject. In its Inspector, drag `TreeLocation` to the `Tree Location` field and `WorkshopLocation` to the `Workshop Location` field.
    *   Position your `WorkerAI` GameObject (e.g., at `(0, 0, 0)`).

5.  Run the scene. Observe the Debug.Log output as the AI plans and executes its actions. The `WorkerAI` will simulate moving and performing tasks, ultimately achieving its goal.

---

### `AIPlannerExample.cs` Code

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections; // For IEnumerator

// --- 1. World State Representation ---
/// <summary>
/// Represents the current state of the world as a collection of boolean facts.
/// This struct is immutable after creation and provides methods for combining states.
/// </summary>
[System.Serializable]
public struct WorldState : IEquatable<WorldState>
{
    // Using a Dictionary for flexibility, but for performance with many states,
    // a bitmask or custom enum flags might be preferred.
    private readonly Dictionary<string, bool> _states;

    public WorldState(Dictionary<string, bool> states = null)
    {
        _states = states != null ? new Dictionary<string, bool>(states) : new Dictionary<string, bool>();
    }

    /// <summary>
    /// Checks if a specific fact is true in this WorldState.
    /// </summary>
    public bool Get(string fact)
    {
        return _states.TryGetValue(fact, out bool value) && value;
    }

    /// <summary>
    /// Creates a new WorldState with a specific fact set to a new value.
    /// </summary>
    public WorldState Set(string fact, bool value)
    {
        var newStates = new Dictionary<string, bool>(_states);
        newStates[fact] = value;
        return new WorldState(newStates);
    }

    /// <summary>
    /// Checks if this WorldState meets all conditions specified in another WorldState (preconditions).
    /// Only cares about facts explicitly defined as true in the other state.
    /// </summary>
    public bool Matches(WorldState other)
    {
        foreach (var fact in other._states)
        {
            // If the other state requires a fact to be true, and it's not true in this state, it doesn't match.
            // If the other state requires a fact to be false, and it's true in this state, it doesn't match.
            if (fact.Value != Get(fact.Key))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Applies the effects of another WorldState (postconditions) to this WorldState, creating a new one.
    /// </summary>
    public WorldState Apply(WorldState effects)
    {
        var newStates = new Dictionary<string, bool>(_states);
        foreach (var fact in effects._states)
        {
            newStates[fact.Key] = fact.Value;
        }
        return new WorldState(newStates);
    }

    public override string ToString()
    {
        return "{" + string.Join(", ", _states.Select(kv => $"{kv.Key}: {kv.Value}")) + "}";
    }

    // --- IEquatable and Object overrides for dictionary keys and comparisons ---
    public bool Equals(WorldState other)
    {
        if (_states.Count != other._states.Count) return false;
        foreach (var kvp in _states)
        {
            if (!other._states.TryGetValue(kvp.Key, out bool otherValue) || kvp.Value != otherValue)
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object obj)
    {
        return obj is WorldState other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Simple hash code. For performance-critical scenarios,
        // a more robust hashing algorithm for dictionaries might be needed.
        int hash = 17;
        foreach (var kvp in _states.OrderBy(kv => kv.Key)) // Order by key for consistent hash
        {
            hash = hash * 23 + kvp.Key.GetHashCode();
            hash = hash * 23 + kvp.Value.GetHashCode();
        }
        return hash;
    }

    public static bool operator ==(WorldState a, WorldState b) => a.Equals(b);
    public static bool operator !=(WorldState a, WorldState b) => !a.Equals(b);
}


// --- 2. Base AI Action Class ---
/// <summary>
/// Abstract base class for all AI actions.
/// Defines preconditions, effects, and the cost of an action.
/// </summary>
public abstract class AIAction : ScriptableObject // Using ScriptableObject allows actions to be asset-based
{
    [Tooltip("A unique name for this action.")]
    public string ActionName;
    [Tooltip("The state facts that must be true for this action to be executable.")]
    public WorldState Preconditions;
    [Tooltip("The state facts that become true (or false) after this action is executed.")]
    public WorldState Effects;
    [Tooltip("The cost/effort associated with performing this action.")]
    public float Cost = 1f;

    /// <summary>
    /// Placeholder for any initialization specific to an action.
    /// </summary>
    public virtual void OnEnable() { }

    /// <summary>
    /// Checks if the current world state satisfies the preconditions for this action.
    /// </summary>
    public bool MeetsPreconditions(WorldState currentState)
    {
        return currentState.Matches(Preconditions);
    }

    /// <summary>
    /// Simulates the execution of the action by applying its effects to a given world state.
    /// </summary>
    public WorldState ApplyEffectsTo(WorldState currentState)
    {
        return currentState.Apply(Effects);
    }

    /// <summary>
    /// The actual implementation of the action that modifies the game world.
    /// This is an IEnumerator to allow for actions that take time (e.g., movement, animation).
    /// The agent will yield return this coroutine.
    /// </summary>
    /// <param name="agent">The AIAgent performing this action.</param>
    public abstract IEnumerator PerformAction(AIAgent agent);

    public override string ToString()
    {
        return $"[{ActionName}, Cost: {Cost}] Pre: {Preconditions} Eff: {Effects}";
    }
}


// --- 3. Concrete AI Action Implementations ---

// Example Actions: (These would typically be separate ScriptableObject assets)

[CreateAssetMenu(fileName = "GatherWoodAction", menuName = "AI/Actions/Gather Wood")]
public class GatherWoodAction : AIAction
{
    public override void OnEnable()
    {
        ActionName = "GatherWood";
        Preconditions = new WorldState(new Dictionary<string, bool> { { "AtTree", true } });
        Effects = new WorldState(new Dictionary<string, bool> { { "HasWood", true } });
        Cost = 5f;
    }

    public override IEnumerator PerformAction(AIAgent agent)
    {
        Debug.Log($"<color=cyan>{agent.name}</color>: Gathering wood...");
        yield return new WaitForSeconds(2.0f); // Simulate gathering time
        Debug.Log($"<color=cyan>{agent.name}</color>: Wood gathered!");
    }
}

[CreateAssetMenu(fileName = "MoveToTreeAction", menuName = "AI/Actions/Move To Tree")]
public class MoveToTreeAction : AIAction
{
    public override void OnEnable()
    {
        ActionName = "MoveToTree";
        Preconditions = new WorldState(new Dictionary<string, bool> { { "AtTree", false } });
        Effects = new WorldState(new Dictionary<string, bool> { { "AtTree", true }, { "AtWorkshop", false } }); // Ensure exclusive location
        Cost = 3f;
    }

    public override IEnumerator PerformAction(AIAgent agent)
    {
        Debug.Log($"<color=cyan>{agent.name}</color>: Moving to tree...");
        Vector3 startPos = agent.transform.position;
        Vector3 targetPos = agent.TreeLocation.position;
        float duration = Vector3.Distance(startPos, targetPos) / agent.MoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            agent.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agent.transform.position = targetPos; // Ensure exact position
        Debug.Log($"<color=cyan>{agent.name}</color>: Arrived at tree!");
    }
}

[CreateAssetMenu(fileName = "MoveToWorkshopAction", menuName = "AI/Actions/Move To Workshop")]
public class MoveToWorkshopAction : AIAction
{
    public override void OnEnable()
    {
        ActionName = "MoveToWorkshop";
        Preconditions = new WorldState(new Dictionary<string, bool> { { "AtWorkshop", false } });
        Effects = new WorldState(new Dictionary<string, bool> { { "AtWorkshop", true }, { "AtTree", false } }); // Ensure exclusive location
        Cost = 3f;
    }

    public override IEnumerator PerformAction(AIAgent agent)
    {
        Debug.Log($"<color=cyan>{agent.name}</color>: Moving to workshop...");
        Vector3 startPos = agent.transform.position;
        Vector3 targetPos = agent.WorkshopLocation.position;
        float duration = Vector3.Distance(startPos, targetPos) / agent.MoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            agent.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agent.transform.position = targetPos; // Ensure exact position
        Debug.Log($"<color=cyan>{agent.name}</color>: Arrived at workshop!");
    }
}

[CreateAssetMenu(fileName = "BuildHouseAction", menuName = "AI/Actions/Build House")]
public class BuildHouseAction : AIAction
{
    public override void OnEnable()
    {
        ActionName = "BuildHouse";
        Preconditions = new WorldState(new Dictionary<string, bool> { { "HasWood", true }, { "AtWorkshop", true } });
        Effects = new WorldState(new Dictionary<string, bool> { { "HouseBuilt", true }, { "HasWood", false } }); // Wood is consumed
        Cost = 10f;
    }

    public override IEnumerator PerformAction(AIAgent agent)
    {
        Debug.Log($"<color=cyan>{agent.name}</color>: Building house...");
        yield return new WaitForSeconds(3.0f); // Simulate building time
        Debug.Log($"<color=cyan>{agent.name}</color>: House built!");
    }
}


// --- 4. The AI Planner ---
/// <summary>
/// Static class that implements the A* search algorithm to find a plan.
/// A plan is a sequence of actions that transforms a start state into a goal state.
/// </summary>
public static class AIPlanner
{
    /// <summary>
    /// Represents a node in the A* search graph.
    /// </summary>
    private class Node
    {
        public WorldState State;         // The world state at this node
        public AIAction Action;          // The action that led to this state (null for start node)
        public Node Parent;              // The parent node in the path
        public float GCost;              // Cost from start node to this node
        public float HCost;              // Heuristic estimate from this node to goal
        public float FCost => GCost + HCost; // Total cost

        public Node(WorldState state, AIAction action, Node parent, float gCost, float hCost)
        {
            State = state;
            Action = action;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }
    }

    /// <summary>
    /// Finds a plan (a sequence of actions) to achieve the goal state from the start state.
    /// Uses the A* search algorithm.
    /// </summary>
    /// <param name="startState">The current world state.</param>
    /// <param name="goalState">The desired world state.</param>
    /// <param name="availableActions">A list of all possible actions the agent can perform.</param>
    /// <returns>A Queue of AIAction representing the plan, or null if no plan is found.</returns>
    public static Queue<AIAction> Plan(WorldState startState, WorldState goalState, List<AIAction> availableActions)
    {
        // Debug.Log($"Planning from {startState} to {goalState}");

        // --- A* Search Setup ---
        // openSet: Nodes to be evaluated, sorted by FCost
        var openSet = new List<Node>();
        // closedSet: Nodes already evaluated, stores the lowest GCost to reach a given WorldState
        var closedSet = new Dictionary<WorldState, float>();

        // Create the initial node
        Node startNode = new Node(startState, null, null, 0, CalculateHeuristic(startState, goalState));
        openSet.Add(startNode);
        closedSet[startState] = 0; // Record the GCost for the start state

        while (openSet.Count > 0)
        {
            // Get the node with the lowest FCost from the open set
            openSet.Sort((a, b) => a.FCost.CompareTo(b.FCost));
            Node currentNode = openSet[0];
            openSet.RemoveAt(0);

            // Debug.Log($"  Evaluating node: {currentNode.State}, Action: {currentNode.Action?.ActionName}, F: {currentNode.FCost}");

            // If the current node's state matches the goal, we've found a plan!
            if (currentNode.State.Matches(goalState))
            {
                // Debug.Log($"  Goal reached: {currentNode.State}");
                return ReconstructPath(currentNode);
            }

            // Explore neighbors (possible actions from the current state)
            foreach (AIAction action in availableActions)
            {
                // Check if the action's preconditions are met in the current state
                if (action.MeetsPreconditions(currentNode.State))
                {
                    // Calculate the state after applying this action
                    WorldState nextState = action.ApplyEffectsTo(currentNode.State);
                    float newGCost = currentNode.GCost + action.Cost;

                    // If this state has already been visited with a lower or equal cost, skip
                    if (closedSet.TryGetValue(nextState, out float existingGCost) && newGCost >= existingGCost)
                    {
                        continue;
                    }

                    // This path to nextState is better or new
                    closedSet[nextState] = newGCost;
                    Node nextNode = new Node(nextState, action, currentNode, newGCost, CalculateHeuristic(nextState, goalState));
                    openSet.Add(nextNode);
                }
            }
        }

        // No plan found
        Debug.LogWarning("AI Planner: No plan found to achieve the goal.");
        return null;
    }

    /// <summary>
    /// Calculates the heuristic (estimated cost) from a current state to the goal state.
    /// A simple heuristic: count the number of goal conditions not yet met.
    /// This is an admissible heuristic (never overestimates) but not very informative.
    /// </summary>
    private static float CalculateHeuristic(WorldState currentState, WorldState goalState)
    {
        float unmetConditions = 0;
        foreach (var goalFact in goalState._states)
        {
            if (goalFact.Value && !currentState.Get(goalFact.Key))
            {
                unmetConditions++;
            }
            else if (!goalFact.Value && currentState.Get(goalFact.Key))
            {
                unmetConditions++;
            }
        }
        return unmetConditions; // Each unmet condition adds 1 to the heuristic
    }

    /// <summary>
    /// Reconstructs the action plan by backtracking from the goal node to the start node.
    /// </summary>
    private static Queue<AIAction> ReconstructPath(Node goalNode)
    {
        var path = new Stack<AIAction>(); // Use a stack to build in reverse, then convert to queue
        Node currentNode = goalNode;

        while (currentNode.Action != null) // Stop when we reach the start node (which has no action)
        {
            path.Push(currentNode.Action);
            currentNode = currentNode.Parent;
        }
        return new Queue<AIAction>(path);
    }
}


// --- 5. The AI Agent MonoBehaviour ---
/// <summary>
/// The main AI agent that holds its current world state, defines a goal,
/// uses the planner to find a plan, and executes the plan.
/// </summary>
public class AIAgent : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("The goal this AI agent is trying to achieve.")]
    public WorldState CurrentGoal;

    [Tooltip("All possible actions this agent can perform.")]
    public List<AIAction> AvailableActions;

    [Tooltip("The speed at which the agent moves.")]
    public float MoveSpeed = 5f;

    [Header("World State References")]
    [Tooltip("The transform representing the tree location.")]
    public Transform TreeLocation;
    [Tooltip("The transform representing the workshop location.")]
    public Transform WorkshopLocation;

    private WorldState _currentWorldState;
    private Queue<AIAction> _currentPlan;
    private IEnumerator _currentActionCoroutine; // To keep track of running actions

    // Public getter for current world state for debug/external access
    public WorldState AgentWorldState => _currentWorldState;

    void Awake()
    {
        // Initialize the goal. For this example, a fixed goal.
        // In a real game, this might come from a Goal Selector system.
        CurrentGoal = new WorldState(new Dictionary<string, bool> { { "HouseBuilt", true } });

        // Ensure actions are initialized (if they were ScriptableObjects created as assets)
        foreach (var action in AvailableActions)
        {
            action.OnEnable();
        }
    }

    void Start()
    {
        // Initialize agent's position for demonstration
        if (TreeLocation != null)
        {
            // Start agent near the tree, but not AtTree, to force movement planning
            transform.position = TreeLocation.position + new Vector3(2, 0, 2);
        }
    }

    void Update()
    {
        // Check if the goal is already met
        if (_currentWorldState.Matches(CurrentGoal))
        {
            if (_currentActionCoroutine == null) // Only log if not in the middle of an action
            {
                Debug.Log($"<color=green>{name}</color>: Goal '{CurrentGoal}' already achieved! Waiting for new goal.");
            }
            return;
        }

        // If no plan, or current plan is empty, try to generate one
        if (_currentPlan == null || _currentPlan.Count == 0)
        {
            if (_currentActionCoroutine == null) // Only plan if not currently executing an action
            {
                Debug.Log($"<color=orange>{name}</color>: No plan or plan finished. Generating new plan...");
                GenerateWorldState(); // Update agent's perception of the world
                _currentPlan = AIPlanner.Plan(_currentWorldState, CurrentGoal, AvailableActions);

                if (_currentPlan != null && _currentPlan.Count > 0)
                {
                    Debug.Log($"<color=green>{name}</color>: Plan generated successfully! Plan length: {_currentPlan.Count}");
                    // Log the plan actions
                    string planString = "Plan: ";
                    foreach (var action in _currentPlan)
                    {
                        planString += action.ActionName + " -> ";
                    }
                    Debug.Log(planString.TrimEnd(' ', '-', '>'));
                }
                else
                {
                    Debug.LogWarning($"<color=red>{name}</color>: Failed to find a plan for goal: {CurrentGoal}. Current State: {_currentWorldState}");
                }
            }
        }

        // If a plan exists and no action is currently running, execute the next action
        if (_currentPlan != null && _currentPlan.Count > 0 && _currentActionCoroutine == null)
        {
            AIAction nextAction = _currentPlan.Dequeue();
            Debug.Log($"<color=yellow>{name}</color>: Executing action: {nextAction.ActionName}");

            // Apply the action's effects immediately to the agent's internal world model
            // This is crucial for the planner to have an up-to-date internal model for future planning.
            _currentWorldState = nextAction.ApplyEffectsTo(_currentWorldState);
            Debug.Log($"<color=magenta>{name}</color>: Internal state after '{nextAction.ActionName}': {_currentWorldState}");

            // Start the coroutine for the action's actual execution in the game world
            _currentActionCoroutine = StartCoroutine(PerformActionAndCallback(nextAction));
        }
    }

    /// <summary>
    /// Gathers information from the environment to update the agent's current world state.
    /// This is where the agent perceives the game world.
    /// </summary>
    private void GenerateWorldState()
    {
        var states = new Dictionary<string, bool>();

        // Check if the agent is at the tree location
        if (Vector3.Distance(transform.position, TreeLocation.position) < 1.5f) // Small tolerance
        {
            states["AtTree"] = true;
        }
        else
        {
            states["AtTree"] = false;
        }

        // Check if the agent is at the workshop location
        if (Vector3.Distance(transform.position, WorkshopLocation.position) < 1.5f)
        {
            states["AtWorkshop"] = true;
        }
        else
        {
            states["AtWorkshop"] = false;
        }

        // Other facts are usually maintained by the agent itself
        // e.g., if "HasWood" was set by GatherWoodAction, it remains true until consumed.
        // We initialize with the previous state's facts and then overwrite with observations.
        _currentWorldState = new WorldState(_currentWorldState._states).Apply(new WorldState(states));
        Debug.Log($"<color=blue>{name}</color>: Observed World State: {_currentWorldState}");
    }

    /// <summary>
    /// Helper coroutine to execute an action and then reset the current action tracker.
    /// </summary>
    private IEnumerator PerformActionAndCallback(AIAction action)
    {
        yield return action.PerformAction(this);
        _currentActionCoroutine = null; // Mark action as finished
        Debug.Log($"<color=green>{name}</color>: Action '{action.ActionName}' completed.");
        GenerateWorldState(); // Re-evaluate world state after action completion for next planning cycle
    }

    void OnDrawGizmos()
    {
        // Draw gizmos for locations for easier setup visualization
        if (TreeLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(TreeLocation.position, 1.5f);
            UnityEditor.Handles.Label(TreeLocation.position + Vector3.up * 2, "Tree");
        }
        if (WorkshopLocation != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(WorkshopLocation.position, 1.5f);
            UnityEditor.Handles.Label(WorkshopLocation.position + Vector3.up * 2, "Workshop");
        }

        // Draw agent's current goal status
        if (!Application.isPlaying) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 16;
        string goalStatus = _currentWorldState.Matches(CurrentGoal) ? $"Goal Achieved!\n{CurrentGoal}" : $"Current Goal:\n{CurrentGoal}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3, goalStatus, style);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"Current State:\n{_currentWorldState}", style);
    }
}
```