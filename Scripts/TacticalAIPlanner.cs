// Unity Design Pattern Example: TacticalAIPlanner
// This script demonstrates the TacticalAIPlanner pattern in Unity
// Generated automatically - ready to use in your Unity project

The Tactical AI Planner pattern is a powerful way for AI agents in games to make decisions based on their current situation (World State), available actions, and overall goals. It's especially useful in dynamic environments where a fixed state machine might become too complex or rigid.

This example provides a simplified yet practical implementation of the Tactical AI Planner pattern in Unity. Instead of a full-blown Goal-Oriented Action Planning (GOAP) system (which involves complex graph search to find a sequence of actions), this approach focuses on a **Utility-Based Tactical Planner**. This means the AI evaluates all viable actions in its current context and chooses the one with the highest "utility" or "score."

**Core Components of this Implementation:**

1.  **`WorldState` (struct):** Represents the AI agent's current perception of the game world. This is crucial for actions to check their preconditions and calculate their utility.
2.  **`AIAction` (abstract ScriptableObject):** The base class for all actions an AI agent can perform.
    *   **`PreconditionsMet()`:** Defines what must be true in the `WorldState` for the action to be considered.
    *   **`GetUtility()`:** Calculates a score for how "good" this action is *right now*, given the `WorldState`.
    *   **`Execute()`:** Contains the actual logic for performing the action (e.g., moving, attacking, healing). It's an `IEnumerator` to support long-running actions.
    *   **`OnActionStart()`/`OnActionEnd()`:** Lifecycle callbacks for the action.
    *   **`IsComplete()`:** Checks if a long-running action has finished.
3.  **Concrete `AIAction` Implementations (ScriptableObjects):**
    *   `AttackAction`: Represents attacking an enemy.
    *   `MoveToCoverAction`: Represents seeking cover.
    *   `HealAction`: Represents using a health pack.
4.  **`AICharacter` (MonoBehaviour):** This is the AI agent itself. It:
    *   Manages its own health, target, etc.
    *   Updates its `WorldState` based on its perception.
    *   Acts as the **Tactical AI Planner**: It holds a list of `AIAction` ScriptableObjects, constantly evaluates them, and executes the highest utility action whose preconditions are met.

---

### How to Use This Example in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `WorldState.cs`.
    *   Create a new C# script named `AIAction.cs`.
    *   Create a new C# script named `AttackAction.cs`.
    *   Create a new C# script named `MoveToCoverAction.cs`.
    *   Create a new C# script named `HealAction.cs`.
    *   Create a new C# script named `AICharacter.cs`.
    *   Copy the respective code into each file.

2.  **Create ScriptableObject Actions:**
    *   In your Unity Project window, right-click -> Create -> AI -> Attack Action.
    *   Right-click -> Create -> AI -> Move To Cover Action.
    *   Right-click -> Create -> AI -> Heal Action.
    *   You now have three `ScriptableObject` assets representing your actions. You can select them and adjust their inspector values (e.g., `attackDamage`, `coverUtilityBonus`).

3.  **Setup the AI Character:**
    *   Create an empty GameObject in your scene (e.g., "AI_Agent").
    *   Add the `AICharacter.cs` component to this GameObject.
    *   Drag your created `AttackAction`, `MoveToCoverAction`, and `HealAction` ScriptableObject assets into the `Available Actions` list on the `AICharacter` component in the Inspector.
    *   **Simulate Environment:** For demonstration, you might want to:
        *   Create another empty GameObject (e.g., "EnemyTarget") and assign its `Transform` to the `Target Enemy` field on the `AICharacter`.
        *   Create an empty GameObject (e.g., "CoverSpot") and assign its `Transform` to the `Nearby Cover Spots` list.
        *   Create an empty GameObject (e.g., "HealthPack") and assign its `Transform` to the `Nearby Health Packs` list.
        *   Adjust the AI Character's `Health` and `Ammo` in the Inspector to see different actions chosen.

4.  **Run the Scene:**
    *   Observe the Console logs to see the AI character making decisions and executing actions based on its simulated `WorldState`.
    *   Try changing the AI Character's health or enemy's presence during runtime to see it adapt.

---

### `WorldState.cs`

```csharp
using System;
using UnityEngine;

namespace TacticalAIPlanner
{
    /// <summary>
    /// Represents the AI agent's current perception of the world.
    /// This struct holds key boolean flags and other relevant data that
    /// AI actions will use to check preconditions and calculate utility.
    /// </summary>
    [Serializable] // Make it serializable so it can be viewed in Inspector if needed
    public struct WorldState
    {
        // Core combat state
        public bool hasTarget;             // Does the AI have an enemy target?
        public bool targetInRange;         // Is the target within attack range?
        public bool canSeeTarget;          // Is the target currently visible?
        public bool lowHealth;             // Is the AI's health below a critical threshold?
        public bool ammoLow;               // Is the AI's ammo below a critical threshold?

        // Positional/Environmental state
        public bool inCover;               // Is the AI currently behind cover?
        public bool coverAvailable;        // Are there nearby cover spots?

        // Resource state
        public bool healthPackAvailable;   // Are there nearby health packs?

        /// <summary>
        /// A simple way to log the current world state for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"WS: Target({hasTarget}/{targetInRange}/{canSeeTarget}), " +
                   $"Health({lowHealth}), Ammo({ammoLow}), " +
                   $"Cover({inCover}/{coverAvailable}), " +
                   $"HealthPack({healthPackAvailable})";
        }

        // Example: Factory method for an initial or default world state
        public static WorldState Default()
        {
            return new WorldState
            {
                hasTarget = false,
                targetInRange = false,
                canSeeTarget = false,
                lowHealth = false,
                ammoLow = false,
                inCover = false,
                coverAvailable = false,
                healthPackAvailable = false
            };
        }
    }
}
```

---

### `AIAction.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace TacticalAIPlanner
{
    /// <summary>
    /// Base class for all AI actions. This is an abstract ScriptableObject
    /// which allows us to define different actions as assets in Unity.
    ///
    /// The Tactical AI Planner will evaluate instances of these actions.
    /// </summary>
    public abstract class AIAction : ScriptableObject
    {
        [Tooltip("Name of the action for debugging.")]
        public string actionName = "Default Action";

        /// <summary>
        /// Checks if the preconditions for this action are met given the current world state.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        /// <param name="worldState">The current perceived state of the world.</param>
        /// <returns>True if the action can be performed, false otherwise.</returns>
        public abstract bool PreconditionsMet(AICharacter agent, WorldState worldState);

        /// <summary>
        /// Calculates the utility (desirability/score) of performing this action.
        /// The planner will choose the action with the highest utility among those whose
        /// preconditions are met.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        /// <param name="worldState">The current perceived state of the world.</param>
        /// <returns>A float representing the utility score. Higher is better.</returns>
        public abstract float GetUtility(AICharacter agent, WorldState worldState);

        /// <summary>
        /// Executes the action. This can be a long-running operation,
        /// so it returns an IEnumerator for coroutine execution.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        public abstract IEnumerator Execute(AICharacter agent);

        /// <summary>
        /// Called when the action starts. Use for initial setup.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        public virtual void OnActionStart(AICharacter agent)
        {
            // Debug.Log($"Action '{actionName}' started by {agent.name}.");
        }

        /// <summary>
        /// Called when the action ends (either completed or interrupted). Use for cleanup.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        public virtual void OnActionEnd(AICharacter agent)
        {
            // Debug.Log($"Action '{actionName}' ended by {agent.name}.");
        }

        /// <summary>
        /// Checks if a long-running action has completed its task.
        /// For instant actions, this might always return true.
        /// </summary>
        /// <param name="agent">The AICharacter performing the action.</param>
        /// <returns>True if the action is complete, false otherwise.</returns>
        public abstract bool IsComplete(AICharacter agent);
    }
}
```

---

### `AttackAction.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace TacticalAIPlanner
{
    /// <summary>
    /// An AIAction representing an attack.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackAction", menuName = "AI/Attack Action", order = 1)]
    public class AttackAction : AIAction
    {
        [Header("Attack Settings")]
        [Tooltip("Damage dealt per attack.")]
        public float attackDamage = 10f;
        [Tooltip("Time between attacks.")]
        public float attackCooldown = 1.0f;
        [Tooltip("Minimum ammo required to perform attack.")]
        public int minAmmoRequired = 1;

        private float _lastAttackTime;

        public override bool PreconditionsMet(AICharacter agent, WorldState worldState)
        {
            // Can only attack if:
            // 1. Has a target
            // 2. Target is in range
            // 3. Can see the target
            // 4. Has enough ammo
            return worldState.hasTarget && worldState.targetInRange && worldState.canSeeTarget && agent.CurrentAmmo >= minAmmoRequired;
        }

        public override float GetUtility(AICharacter agent, WorldState worldState)
        {
            float utility = 0f;

            // Base utility for attacking
            utility += 0.6f;

            // Bonus if target is low health (e.g., trying to finish them off)
            if (agent.TargetEnemy != null && agent.TargetEnemy.TryGetComponent<AICharacter>(out var targetCharacter))
            {
                if (targetCharacter.CurrentHealth / targetCharacter.MaxHealth < 0.3f)
                {
                    utility += 0.3f; // High bonus for finishing move
                }
            }

            // Penalty if AI is low on health and should prioritize survival
            if (worldState.lowHealth)
            {
                utility -= 0.4f;
            }

            // Penalty if ammo is very low
            if (worldState.ammoLow)
            {
                utility -= 0.2f;
            }

            // Ensure utility doesn't go below zero
            return Mathf.Max(0, utility);
        }

        public override IEnumerator Execute(AICharacter agent)
        {
            Debug.Log($"<color=orange>{agent.name} is executing AttackAction.</color>");
            while (true)
            {
                // Only attack if cooldown has passed
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    if (agent.CurrentAmmo >= minAmmoRequired)
                    {
                        Debug.Log($"<color=red>{agent.name} is ATTACKING {agent.TargetEnemy.name}!</color>");
                        agent.Shoot(attackDamage);
                        _lastAttackTime = Time.time;
                        agent.CurrentAmmo--; // Simulate ammo usage
                    }
                    else
                    {
                        // Ran out of ammo during execution, action needs to complete
                        Debug.Log($"{agent.name} ran out of ammo for AttackAction.");
                        yield break;
                    }
                }
                yield return null; // Wait for next frame
            }
        }

        public override void OnActionStart(AICharacter agent)
        {
            base.OnActionStart(agent);
            _lastAttackTime = Time.time - attackCooldown; // Allow immediate first attack
        }

        public override bool IsComplete(AICharacter agent)
        {
            // An attack action isn't 'complete' until preconditions are no longer met
            // It continues as long as it's the best action and viable.
            // For a continuous action like this, the planner will decide when to switch.
            return !PreconditionsMet(agent, agent.GetCurrentWorldState()); // Or if target is dead, etc.
        }
    }
}
```

---

### `MoveToCoverAction.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace TacticalAIPlanner
{
    /// <summary>
    /// An AIAction representing moving to a cover spot.
    /// </summary>
    [CreateAssetMenu(fileName = "MoveToCoverAction", menuName = "AI/Move To Cover Action", order = 2)]
    public class MoveToCoverAction : AIAction
    {
        [Header("Movement Settings")]
        [Tooltip("Speed modifier for moving to cover.")]
        public float moveSpeedModifier = 0.8f; // Move slower/cautiously
        [Tooltip("Distance threshold to consider the agent 'in cover'.")]
        public float coverArrivalThreshold = 0.5f;

        private Transform _targetCoverSpot;

        public override bool PreconditionsMet(AICharacter agent, WorldState worldState)
        {
            // Can move to cover if:
            // 1. Not currently in cover
            // 2. Cover is available nearby
            // 3. AI is low on health (primary trigger)
            // 4. Optionally: has an enemy target (making cover more relevant)
            return !worldState.inCover && worldState.coverAvailable && worldState.lowHealth;
        }

        public override float GetUtility(AICharacter agent, WorldState worldState)
        {
            float utility = 0f;

            // High base utility if low health and not in cover
            if (worldState.lowHealth && !worldState.inCover)
            {
                utility += 0.8f;
            }

            // Bonus if target is actively attacking or threatening
            if (worldState.hasTarget && worldState.targetInRange && worldState.canSeeTarget)
            {
                utility += 0.2f;
            }

            // Penalty if agent already has decent health
            if (!worldState.lowHealth)
            {
                utility -= 0.5f;
            }

            // Factor in distance to cover (closer is better, but this can get complex
            // without a pathfinding system. For now, assume coverAvailable implies reachable)
            // If agent doesn't have a target cover spot yet, try to find one.
            if (_targetCoverSpot == null && agent.NearbyCoverSpots.Count > 0)
            {
                _targetCoverSpot = agent.FindNearestCoverSpot();
            }

            if (_targetCoverSpot == null)
            {
                // No actual cover spot found, reduce utility or make it impossible
                return 0f;
            }

            return Mathf.Max(0, utility);
        }

        public override IEnumerator Execute(AICharacter agent)
        {
            Debug.Log($"<color=blue>{agent.name} is executing MoveToCoverAction.</color>");

            // Ensure we have a target cover spot
            if (_targetCoverSpot == null)
            {
                _targetCoverSpot = agent.FindNearestCoverSpot();
                if (_targetCoverSpot == null)
                {
                    Debug.LogWarning($"{agent.name}: No suitable cover spot found during MoveToCoverAction execution. Aborting.");
                    yield break;
                }
            }

            while (Vector3.Distance(agent.transform.position, _targetCoverSpot.position) > coverArrivalThreshold)
            {
                agent.MoveTo(_targetCoverSpot.position, agent.MoveSpeed * moveSpeedModifier);
                yield return null; // Wait for next frame
            }

            Debug.Log($"<color=green>{agent.name} arrived at cover: {_targetCoverSpot.name}.</color>");
            agent.CurrentCover = _targetCoverSpot; // Mark agent as being in cover
        }

        public override void OnActionStart(AICharacter agent)
        {
            base.OnActionStart(agent);
            _targetCoverSpot = agent.FindNearestCoverSpot(); // Find cover immediately
            if (_targetCoverSpot == null)
            {
                Debug.LogWarning($"{agent.name}: No cover found for MoveToCoverAction start!");
            }
            agent.CurrentCover = null; // Clear current cover until reached new one
        }

        public override void OnActionEnd(AICharacter agent)
        {
            base.OnActionEnd(agent);
            // No specific cleanup needed if agent is now in cover or moving elsewhere
        }

        public override bool IsComplete(AICharacter agent)
        {
            // Action is complete if agent is close enough to the target cover spot
            // or if preconditions are no longer met (e.g., no longer low health, or no cover available).
            return agent.CurrentCover != null && Vector3.Distance(agent.transform.position, agent.CurrentCover.position) <= coverArrivalThreshold;
        }
    }
}
```

---

### `HealAction.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace TacticalAIPlanner
{
    /// <summary>
    /// An AIAction representing using a health pack.
    /// </summary>
    [CreateAssetMenu(fileName = "HealAction", menuName = "AI/Heal Action", order = 3)]
    public class HealAction : AIAction
    {
        [Header("Healing Settings")]
        [Tooltip("Amount of health restored.")]
        public float healAmount = 50f;
        [Tooltip("Time it takes to use the health pack.")]
        public float healDuration = 2.0f;
        [Tooltip("Distance threshold to a health pack to use it.")]
        public float healRange = 1.0f;

        private Transform _targetHealthPack;

        public override bool PreconditionsMet(AICharacter agent, WorldState worldState)
        {
            // Can heal if:
            // 1. AI is low on health
            // 2. A health pack is available nearby
            // 3. AI is not at full health
            return worldState.lowHealth && worldState.healthPackAvailable && agent.CurrentHealth < agent.MaxHealth;
        }

        public override float GetUtility(AICharacter agent, WorldState worldState)
        {
            float utility = 0f;

            // High utility if low health and not full
            if (worldState.lowHealth && agent.CurrentHealth < agent.MaxHealth)
            {
                utility += 0.9f;
            }

            // Bonus if very critical health
            if (agent.CurrentHealth / agent.MaxHealth < 0.15f)
            {
                utility += 0.5f;
            }

            // Penalty if health is already moderate
            if (agent.CurrentHealth / agent.MaxHealth > 0.5f)
            {
                utility -= 0.6f;
            }

            // Factor in distance to health pack (closer is better)
            if (_targetHealthPack == null && agent.NearbyHealthPacks.Count > 0)
            {
                _targetHealthPack = agent.FindNearestHealthPack();
            }

            if (_targetHealthPack == null)
            {
                return 0f; // No health pack found
            }

            // If health pack is too far, reduce utility significantly
            if (Vector3.Distance(agent.transform.position, _targetHealthPack.position) > agent.PerceptionRange)
            {
                utility -= 0.7f;
            }

            return Mathf.Max(0, utility);
        }

        public override IEnumerator Execute(AICharacter agent)
        {
            Debug.Log($"<color=purple>{agent.name} is executing HealAction.</color>");

            if (_targetHealthPack == null)
            {
                _targetHealthPack = agent.FindNearestHealthPack();
                if (_targetHealthPack == null)
                {
                    Debug.LogWarning($"{agent.name}: No suitable health pack found during HealAction execution. Aborting.");
                    yield break;
                }
            }

            // Move to health pack if not in range
            while (Vector3.Distance(agent.transform.position, _targetHealthPack.position) > healRange)
            {
                agent.MoveTo(_targetHealthPack.position, agent.MoveSpeed);
                yield return null;
            }

            Debug.Log($"<color=lime>{agent.name} is using health pack at {_targetHealthPack.name}.</color>");
            agent.IsPerformingAction = true; // Indicate agent is busy

            float startTime = Time.time;
            while (Time.time < startTime + healDuration)
            {
                // Play healing animation, sound, etc.
                yield return null;
            }

            agent.Heal(healAmount);
            // Optionally, disable/destroy the health pack GameObject
            if (_targetHealthPack != null)
            {
                Debug.Log($"Health pack {_targetHealthPack.name} consumed!");
                _targetHealthPack.gameObject.SetActive(false); // Or Destroy(_targetHealthPack.gameObject);
                agent.NearbyHealthPacks.Remove(_targetHealthPack); // Remove from agent's perception
            }
            _targetHealthPack = null; // Clear reference

            Debug.Log($"<color=green>{agent.name} finished healing. Current Health: {agent.CurrentHealth}.</color>");
            agent.IsPerformingAction = false;
        }

        public override void OnActionStart(AICharacter agent)
        {
            base.OnActionStart(agent);
            _targetHealthPack = agent.FindNearestHealthPack(); // Find pack immediately
        }

        public override void OnActionEnd(AICharacter agent)
        {
            base.OnActionEnd(agent);
            agent.IsPerformingAction = false; // Ensure this is reset if interrupted
        }

        public override bool IsComplete(AICharacter agent)
        {
            // Action is complete if health is full or health pack is consumed
            return agent.CurrentHealth >= agent.MaxHealth || (_targetHealthPack != null && !_targetHealthPack.gameObject.activeSelf);
        }
    }
}
```

---

### `AICharacter.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For OrderBy, etc.

namespace TacticalAIPlanner
{
    /// <summary>
    /// Represents an AI agent that uses the Tactical AI Planner pattern.
    /// This MonoBehaviour acts as both the agent and its planner, selecting
    /// and executing actions based on its current WorldState and a set of available AIAction ScriptableObjects.
    /// </summary>
    public class AICharacter : MonoBehaviour
    {
        [Header("AI Character Stats")]
        [Tooltip("Maximum health of the AI character.")]
        public float maxHealth = 100f;
        [Tooltip("Current health of the AI character.")]
        [SerializeField] private float currentHealth;
        public float CurrentHealth { get { return currentHealth; } private set { currentHealth = Mathf.Clamp(value, 0, maxHealth); } }

        [Tooltip("Maximum ammo capacity.")]
        public int maxAmmo = 100;
        [Tooltip("Current ammo count.")]
        [SerializeField] private int currentAmmo;
        public int CurrentAmmo { get { return currentAmmo; } set { currentAmmo = Mathf.Clamp(value, 0, maxAmmo); } }

        [Tooltip("Movement speed of the AI character.")]
        public float moveSpeed = 3f;
        [Tooltip("Health percentage below which the AI considers itself 'low health'.")]
        [Range(0.01f, 0.99f)]
        public float lowHealthThreshold = 0.3f;
        [Tooltip("Ammo percentage below which the AI considers itself 'ammo low'.")]
        [Range(0.01f, 0.99f)]
        public float lowAmmoThreshold = 0.2f;
        [Tooltip("Range for AI to perceive nearby objects (targets, cover, health packs).")]
        public float perceptionRange = 15f;
        [Tooltip("Distance threshold to consider a target 'in range' for attacking.")]
        public float attackRange = 10f;

        [Header("AI Perception & Planning")]
        [Tooltip("The enemy target this AI is currently focused on.")]
        public Transform targetEnemy;
        public Transform TargetEnemy => targetEnemy;

        [Tooltip("The cover spot this AI is currently using.")]
        public Transform currentCover;
        public Transform CurrentCover { get { return currentCover; } set { currentCover = value; } }

        [Tooltip("All available AI actions for this character.")]
        public List<AIAction> availableActions = new List<AIAction>();

        [Tooltip("List of nearby cover spots (assigned manually for demo, or dynamically found).")]
        public List<Transform> nearbyCoverSpots = new List<Transform>();
        public List<Transform> NearbyCoverSpots => nearbyCoverSpots;

        [Tooltip("List of nearby health packs (assigned manually for demo, or dynamically found).")]
        public List<Transform> nearbyHealthPacks = new List<Transform>();
        public List<Transform> NearbyHealthPacks => nearbyHealthPacks;

        // --- Internal Planner State ---
        private WorldState _currentWorldState;
        private AIAction _currentAction;
        private Coroutine _currentActionRoutine;
        private bool _isPerformingAction; // Flag to indicate if an action coroutine is running
        public bool IsPerformingAction { get { return _isPerformingAction; } set { _isPerformingAction = value; } }

        // --- Debugging ---
        [Header("Debug")]
        [SerializeField] private string _activeActionName = "None";
        [SerializeField] private WorldState _debugCurrentWorldState;

        void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentAmmo = maxAmmo;
            _currentWorldState = WorldState.Default();
        }

        void Start()
        {
            // Initial planning cycle
            PlanAndExecuteAction();
        }

        void Update()
        {
            // Update perception and plan periodically or every frame.
            // For a tactical planner, often every frame is desirable for responsiveness.
            UpdateWorldState();
            _debugCurrentWorldState = _currentWorldState; // For inspector debugging
            PlanAndExecuteAction();
        }

        /// <summary>
        /// Gathers information about the current environment and updates the AI's internal WorldState.
        /// This represents the AI's perception.
        /// </summary>
        private void UpdateWorldState()
        {
            // --- Target State ---
            _currentWorldState.hasTarget = targetEnemy != null;
            _currentWorldState.targetInRange = _currentWorldState.hasTarget && Vector3.Distance(transform.position, targetEnemy.position) <= attackRange;
            _currentWorldState.canSeeTarget = _currentWorldState.hasTarget && CanSeeTarget(targetEnemy); // Simplified: always true for demo
            if (targetEnemy != null)
            {
                _currentWorldState.canSeeTarget = !Physics.Linecast(transform.position, targetEnemy.position, LayerMask.GetMask("Obstacle")); // Example raycast
            }

            // --- Health/Ammo State ---
            _currentWorldState.lowHealth = CurrentHealth / maxHealth <= lowHealthThreshold;
            _currentWorldState.ammoLow = (float)CurrentAmmo / maxAmmo <= lowAmmoThreshold;

            // --- Cover State ---
            _currentWorldState.inCover = currentCover != null && Vector3.Distance(transform.position, currentCover.position) < 1.0f; // Simplified check
            _currentWorldState.coverAvailable = nearbyCoverSpots.Any(c => c != null && c.gameObject.activeSelf && Vector3.Distance(transform.position, c.position) <= perceptionRange);

            // --- Health Pack State ---
            _currentWorldState.healthPackAvailable = nearbyHealthPacks.Any(hp => hp != null && hp.gameObject.activeSelf && Vector3.Distance(transform.position, hp.position) <= perceptionRange);
        }

        /// <summary>
        /// The core of the Tactical AI Planner.
        /// It evaluates all available actions and selects the best one to execute.
        /// </summary>
        private void PlanAndExecuteAction()
        {
            AIAction bestAction = null;
            float highestUtility = -1f;

            // Step 1: Evaluate all available actions
            foreach (var action in availableActions)
            {
                if (action.PreconditionsMet(this, _currentWorldState))
                {
                    float utility = action.GetUtility(this, _currentWorldState);
                    if (utility > highestUtility)
                    {
                        highestUtility = utility;
                        bestAction = action;
                    }
                }
            }

            // Step 2: Decide whether to switch or continue current action
            if (bestAction != null)
            {
                if (_currentAction == null || _currentAction != bestAction)
                {
                    // New best action found or no action running
                    SwitchAction(bestAction);
                }
                else if (_currentAction.IsComplete(this))
                {
                    // Current action completed, but it's still the best. Let's restart or re-evaluate.
                    // For continuous actions, IsComplete might always be false, so this block won't trigger.
                    // For single-shot actions, it would allow a new instance/re-execution.
                    SwitchAction(bestAction); // Re-execute or re-evaluate.
                }
                // If _currentAction == bestAction and it's not complete, simply let it continue.
            }
            else if (_currentAction != null)
            {
                // No viable action found, but one was running. Stop the current action.
                SwitchAction(null);
            }

            // Update debug field
            _activeActionName = _currentAction != null ? _currentAction.actionName : "None";
        }

        /// <summary>
        /// Stops the current action (if any) and starts a new one.
        /// </summary>
        /// <param name="newAction">The new action to execute. Can be null to stop all actions.</param>
        private void SwitchAction(AIAction newAction)
        {
            if (_currentActionRoutine != null)
            {
                StopCoroutine(_currentActionRoutine);
                _currentActionRoutine = null;
            }

            if (_currentAction != null)
            {
                _currentAction.OnActionEnd(this);
            }

            _currentAction = newAction;

            if (_currentAction != null)
            {
                Debug.Log($"<color=cyan>{name} is switching to: {_currentAction.actionName} (Utility: {highestUtility:F2})</color>");
                _currentAction.OnActionStart(this);
                _currentActionRoutine = StartCoroutine(_currentAction.Execute(this));
            }
            else
            {
                Debug.Log($"<color=grey>{name} has no viable action to perform.</color>");
            }
        }

        // --- AI Character Abilities (called by Actions) ---

        /// <summary>
        /// Simulates the AI taking damage.
        /// </summary>
        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;
            Debug.Log($"{name} took {amount} damage. Health: {CurrentHealth}");
            if (CurrentHealth <= 0)
            {
                Debug.Log($"{name} has been defeated!");
                // Implement death logic here
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Simulates the AI healing.
        /// </summary>
        public void Heal(float amount)
        {
            CurrentHealth += amount;
            Debug.Log($"{name} healed {amount} health. Health: {CurrentHealth}");
        }

        /// <summary>
        /// Simulates the AI shooting its target.
        /// </summary>
        public void Shoot(float damage)
        {
            if (targetEnemy != null)
            {
                Debug.DrawLine(transform.position, targetEnemy.position, Color.red, 0.2f);
                if (targetEnemy.TryGetComponent<AICharacter>(out var enemyCharacter))
                {
                    enemyCharacter.TakeDamage(damage);
                }
            }
        }

        /// <summary>
        /// Simulates the AI moving to a target position.
        /// </summary>
        public void MoveTo(Vector3 destination, float speed)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            transform.LookAt(destination); // Simple look
        }

        // --- Helper Methods for Perception ---

        /// <summary>
        /// Finds the nearest available cover spot.
        /// </summary>
        public Transform FindNearestCoverSpot()
        {
            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var cover in nearbyCoverSpots)
            {
                if (cover != null && cover.gameObject.activeSelf)
                {
                    float dist = Vector3.Distance(transform.position, cover.position);
                    if (dist < minDist && dist <= perceptionRange)
                    {
                        minDist = dist;
                        nearest = cover;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// Finds the nearest available health pack.
        /// </summary>
        public Transform FindNearestHealthPack()
        {
            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var healthPack in nearbyHealthPacks)
            {
                if (healthPack != null && healthPack.gameObject.activeSelf)
                {
                    float dist = Vector3.Distance(transform.position, healthPack.position);
                    if (dist < minDist && dist <= perceptionRange)
                    {
                        minDist = dist;
                        nearest = healthPack;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// Simplified check if AI can 'see' the target (no actual raycasting for demo).
        /// For a real game, this would involve raycasts, cone of vision, etc.
        /// </summary>
        private bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;
            // For a real game, add raycasting from AI's eyes to target's center.
            // For this demo, let's just assume we can see if it's within perception range.
            return Vector3.Distance(transform.position, target.position) <= perceptionRange;
        }

        /// <summary>
        /// Returns a copy of the current WorldState. Used by actions for checks.
        /// </summary>
        public WorldState GetCurrentWorldState()
        {
            return _currentWorldState;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, perceptionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (targetEnemy != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, targetEnemy.position);
            }
        }
    }
}
```