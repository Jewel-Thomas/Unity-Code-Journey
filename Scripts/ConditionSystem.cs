// Unity Design Pattern Example: ConditionSystem
// This script demonstrates the ConditionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Condition System design pattern allows for the flexible and decoupled evaluation of game states or requirements. Instead of hardcoding `if/else` statements everywhere, you define individual conditions, combine them using logical operators (AND, OR, NOT), and then evaluate this composite structure against a specific context.

This makes your code more:
*   **Modular:** Conditions are small, reusable units.
*   **Flexible:** Easily add, remove, or change conditions without affecting other parts of the system.
*   **Readable:** Complex logic can be expressed clearly as a hierarchy of conditions.
*   **Maintainable:** Bugs are easier to locate and fix within isolated condition logic.

**Real-world Use Cases in Unity:**
*   **AI Behavior Trees:** "Can I attack?" (IsTargetInRange AND HasLineOfSight AND HasEnoughStamina).
*   **Quest System:** "Can I complete this quest?" (HasItemA AND TalkedToNPCB AND ReachedLevelX).
*   **UI Activation:** "Should this button be enabled?" (PlayerHasEnoughGold OR PlayerHasPermission).
*   **Dialogue Options:** "Can I select this dialogue option?" (HasMetNPC OR CompletedSpecificQuest).
*   **State Machine Transitions:** "Should I transition from 'Patrol' to 'Chase'?" (TargetDetected AND TargetIsInSight).

---

## Complete C# Unity Example: ConditionSystem

This example demonstrates a Condition System used to evaluate if an AI character should "Attack" or "Idle".

**Setup in Unity:**

1.  Create a new C# script named `ConditionSystemDemo`.
2.  Copy and paste the entire code below into `ConditionSystemDemo.cs`.
3.  Create an empty GameObject in your scene, name it `ConditionSystemManager`.
4.  Attach the `ConditionSystemDemo` script to `ConditionSystemManager`.
5.  Create two more empty GameObjects:
    *   `Player` (drag it to the `Player Transform` field in the Inspector of `ConditionSystemManager`).
    *   `Target` (drag it to the `Target Transform` field).
6.  Create a UI Text element (or TextMeshPro Text if you have it imported). Name it `StatusText`. Drag it to the `Status Text` field in the Inspector of `ConditionSystemManager`.
7.  Run the scene. Move the `Player` and `Target` GameObjects around in the scene view to observe the `StatusText` changing based on the conditions.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using TMPro; // Use this for TextMeshPro, otherwise use 'UnityEngine.UI.Text'
             // If you don't have TextMeshPro, go to Window -> TextMeshPro -> Import TMP Essential Resources

namespace MyGame.ConditionSystem
{
    /// <summary>
    /// Represents the contextual data needed for conditions to evaluate against.
    /// This makes conditions reusable and independent of where the data comes from.
    /// </summary>
    public class ConditionContext
    {
        public Transform PlayerTransform;
        public Transform TargetTransform;
        public float TargetDistance;
        public int PlayerHealth;
        public int PlayerAmmo;

        // Constructor to easily populate the context
        public ConditionContext(Transform player, Transform target, int health, int ammo)
        {
            PlayerTransform = player;
            TargetTransform = target;
            PlayerHealth = health;
            PlayerAmmo = ammo;

            // Calculate distance if both transforms are available
            if (PlayerTransform != null && TargetTransform != null)
            {
                TargetDistance = Vector3.Distance(PlayerTransform.position, TargetTransform.position);
            }
            else
            {
                TargetDistance = float.MaxValue; // Indicate no valid distance
            }
        }
    }

    /// <summary>
    /// The base interface for all conditions.
    /// Each condition must implement an Evaluate method that takes a ConditionContext.
    /// </summary>
    public interface ICondition
    {
        bool Evaluate(ConditionContext context);
    }

    // --- Concrete Simple Conditions ---

    /// <summary>
    /// Checks if the target is within a specified range of the player.
    /// </summary>
    public class IsTargetInRangeCondition : ICondition
    {
        private float _range;

        public IsTargetInRangeCondition(float range)
        {
            _range = range;
        }

        public bool Evaluate(ConditionContext context)
        {
            if (context.PlayerTransform == null || context.TargetTransform == null)
            {
                Debug.LogWarning("IsTargetInRangeCondition: Player or Target Transform is null in context.");
                return false;
            }
            return context.TargetDistance <= _range;
        }
    }

    /// <summary>
    /// Checks if the player's health is above a certain threshold.
    /// </summary>
    public class IsPlayerHealthAboveCondition : ICondition
    {
        private int _requiredHealth;

        public IsPlayerHealthAboveCondition(int requiredHealth)
        {
            _requiredHealth = requiredHealth;
        }

        public bool Evaluate(ConditionContext context)
        {
            return context.PlayerHealth >= _requiredHealth;
        }
    }

    /// <summary>
    /// Checks if the player has enough ammunition.
    /// </summary>
    public class HasEnoughAmmoCondition : ICondition
    {
        private int _requiredAmmo;

        public HasEnoughAmmoCondition(int requiredAmmo)
        {
            _requiredAmmo = requiredAmmo;
        }

        public bool Evaluate(ConditionContext context)
        {
            return context.PlayerAmmo >= _requiredAmmo;
        }
    }

    // --- Composite Conditions (Logical Operators) ---

    /// <summary>
    /// A composite condition that evaluates to true only if ALL its child conditions are true.
    /// This implements the logical 'AND' operation.
    /// </summary>
    public class AndCondition : ICondition
    {
        private List<ICondition> _conditions;

        public AndCondition(params ICondition[] conditions)
        {
            _conditions = new List<ICondition>(conditions);
        }

        public bool Evaluate(ConditionContext context)
        {
            foreach (var condition in _conditions)
            {
                if (!condition.Evaluate(context))
                {
                    return false; // If any condition is false, the AND condition is false
                }
            }
            return true; // All conditions were true
        }

        // Add condition dynamically
        public void AddCondition(ICondition condition)
        {
            _conditions.Add(condition);
        }
    }

    /// <summary>
    /// A composite condition that evaluates to true if ANY of its child conditions are true.
    /// This implements the logical 'OR' operation.
    /// </summary>
    public class OrCondition : ICondition
    {
        private List<ICondition> _conditions;

        public OrCondition(params ICondition[] conditions)
        {
            _conditions = new List<ICondition>(conditions);
        }

        public bool Evaluate(ConditionContext context)
        {
            foreach (var condition in _conditions)
            {
                if (condition.Evaluate(context))
                {
                    return true; // If any condition is true, the OR condition is true
                }
            }
            return false; // All conditions were false
        }

        // Add condition dynamically
        public void AddCondition(ICondition condition)
        {
            _conditions.Add(condition);
        }
    }

    /// <summary>
    /// A composite condition that negates the result of its single child condition.
    /// This implements the logical 'NOT' operation.
    /// </summary>
    public class NotCondition : ICondition
    {
        private ICondition _condition;

        public NotCondition(ICondition condition)
        {
            _condition = condition;
        }

        public bool Evaluate(ConditionContext context)
        {
            return !_condition.Evaluate(context);
        }
    }


    /// <summary>
    /// DEMO SCRIPT: Demonstrates how to set up and use the Condition System in Unity.
    /// Attach this to an empty GameObject in your scene.
    /// Assign PlayerTransform, TargetTransform, and StatusText in the Inspector.
    /// </summary>
    public class ConditionSystemDemo : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Assign the Player's Transform here.")]
        public Transform PlayerTransform;
        [Tooltip("Assign the Target's Transform here.")]
        public Transform TargetTransform;
        [Tooltip("Assign a UI Text or TextMeshPro text component to display the status.")]
        public TextMeshProUGUI StatusText; // Use Text for UnityEngine.UI.Text

        [Header("Demo Settings (Simulated Game State)")]
        [Range(0, 100)]
        public int SimulatedPlayerHealth = 100;
        [Range(0, 50)]
        public int SimulatedPlayerAmmo = 10;
        public float AttackRange = 5f;
        public int MinHealthToAttack = 20;
        public int MinAmmoToAttack = 5;

        private ICondition _shouldAttackCondition;
        private ICondition _shouldIdleCondition;

        void Awake()
        {
            // --- Define our conditions ---

            // Condition 1: Is the target within attack range?
            ICondition inRange = new IsTargetInRangeCondition(AttackRange);

            // Condition 2: Does the player have enough health to attack?
            ICondition enoughHealth = new IsPlayerHealthAboveCondition(MinHealthToAttack);

            // Condition 3: Does the player have enough ammo?
            ICondition enoughAmmo = new HasEnoughAmmoCondition(MinAmmoToAttack);

            // --- Compose the "Should Attack" condition ---
            // An AI should attack if (In Range AND Enough Health AND Enough Ammo)
            _shouldAttackCondition = new AndCondition(inRange, enoughHealth, enoughAmmo);

            // --- Compose the "Should Idle" condition ---
            // An AI should idle if NOT (Should Attack) OR (Player has low health OR Low Ammo)
            // For simplicity, let's say "Should Idle" is simply "NOT Should Attack"
            // We could also make a more complex idle condition like:
            // _shouldIdleCondition = new OrCondition(new NotCondition(inRange), new NotCondition(enoughHealth));
            // But for this demo, let's keep it simple:
            _shouldIdleCondition = new NotCondition(_shouldAttackCondition);


            // You could also create another condition, e.g., "IsPlayerLowHealth":
            ICondition isLowHealth = new NotCondition(new IsPlayerHealthAboveCondition(MinHealthToAttack));

            // Or more complex like:
            // ICondition canFlank = new AndCondition(
            //     new IsTargetInRangeCondition(10f),
            //     new NotCondition(new HasLineOfSightCondition()) // Hypothetical condition
            // );

            UpdateStatusUI("Initializing Condition System...");
        }

        void Update()
        {
            // 1. Create a fresh context with the current game state
            // In a real game, this data would come from actual game systems (PlayerController, Inventory, etc.)
            ConditionContext currentContext = new ConditionContext(
                PlayerTransform,
                TargetTransform,
                SimulatedPlayerHealth,
                SimulatedPlayerAmmo
            );

            // 2. Evaluate the conditions
            bool canAttack = _shouldAttackCondition.Evaluate(currentContext);
            bool canIdle = _shouldIdleCondition.Evaluate(currentContext); // This will be the opposite of canAttack in this demo

            // 3. Take action based on evaluation
            if (canAttack)
            {
                // In a real game: Trigger attack animation, cast ability, move towards target, etc.
                UpdateStatusUI($"<color=red>ATTACKING!</color>\n(Range: {currentContext.TargetDistance:F2}m, Health: {SimulatedPlayerHealth}, Ammo: {SimulatedPlayerAmmo})");
            }
            else if (canIdle)
            {
                // In a real game: Play idle animation, wander, wait, seek cover, etc.
                UpdateStatusUI($"<color=green>IDLING...</color>\n(Range: {currentContext.TargetDistance:F2}m, Health: {SimulatedPlayerHealth}, Ammo: {SimulatedPlayerAmmo})");
            }
            else
            {
                // Fallback, though should not be reached with a simple NOT condition for Idle
                UpdateStatusUI($"<color=blue>UNDECIDED</color>\n(Range: {currentContext.TargetDistance:F2}m, Health: {SimulatedPlayerHealth}, Ammo: {SimulatedPlayerAmmo})");
            }

            // Optional: Simulate changing conditions over time for demo purposes
            SimulateChanges();
        }

        private void SimulateChanges()
        {
            // Example: Gradually reduce ammo
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SimulatedPlayerAmmo--;
                if (SimulatedPlayerAmmo < 0) SimulatedPlayerAmmo = 0;
                Debug.Log($"Simulated Ammo: {SimulatedPlayerAmmo}");
            }
            // Example: Reduce health
            if (Input.GetKeyDown(KeyCode.H))
            {
                SimulatedPlayerHealth -= 10;
                if (SimulatedPlayerHealth < 0) SimulatedPlayerHealth = 0;
                Debug.Log($"Simulated Health: {SimulatedPlayerHealth}");
            }
            // Example: Reset health and ammo
            if (Input.GetKeyDown(KeyCode.R))
            {
                SimulatedPlayerHealth = 100;
                SimulatedPlayerAmmo = 10;
                Debug.Log("Reset Health and Ammo");
            }
        }

        private void UpdateStatusUI(string message)
        {
            if (StatusText != null)
            {
                StatusText.text = message;
            }
        }

        // --- Example Usage in Comments (How to implement other conditions) ---
        /*
        // To add a new simple condition:
        // 1. Create a new class that implements ICondition
        public class HasLineOfSightCondition : ICondition
        {
            private LayerMask _obstacleLayer; // Example parameter

            public HasLineOfSightCondition(LayerMask obstacleLayer)
            {
                _obstacleLayer = obstacleLayer;
            }

            public bool Evaluate(ConditionContext context)
            {
                if (context.PlayerTransform == null || context.TargetTransform == null) return false;
                
                // Perform a Raycast from player to target
                RaycastHit hit;
                Vector3 direction = (context.TargetTransform.position - context.PlayerTransform.position).normalized;
                if (Physics.Raycast(context.PlayerTransform.position, direction, out hit, context.TargetDistance, _obstacleLayer))
                {
                    return hit.transform == context.TargetTransform; // True if target hit, false if obstacle hit
                }
                return true; // No obstacles found within range
            }
        }

        // To use it:
        // In Awake():
        // LayerMask obstacleLayer = LayerMask.GetMask("Obstacles"); // Assuming you have an "Obstacles" layer
        // ICondition hasLOS = new HasLineOfSightCondition(obstacleLayer);
        // _shouldAttackCondition = new AndCondition(inRange, enoughHealth, enoughAmmo, hasLOS);
        */
    }
}
```