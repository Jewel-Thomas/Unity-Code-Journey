// Unity Design Pattern Example: AIUtilitySystem
// This script demonstrates the AIUtilitySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The AI Utility System is a powerful design pattern that allows AI agents to make decisions by evaluating the utility (or "score") of various potential actions based on the current state of the world. It's highly flexible, data-driven, and excels in scenarios where an AI needs to balance multiple competing goals.

This complete C# Unity example demonstrates the AI Utility System pattern. It uses a **ScriptableObject-based** approach, which is a best practice for Unity development as it allows designers to create and configure AI behaviors as assets directly in the editor without touching code.

The example simulates a simple creature (AI Character) that needs to manage its `Health`, `Hunger`, and `Thirst`, and react to `Enemies` and `Resources`.

---

## **AI Utility System Example: Creature Survival AI**

This single script file contains all the necessary classes for the AI Utility System. To use it:

1.  **Create a new C# Script** in your Unity project, name it `AIUtilitySystemExample`, and copy the entire code into it.
2.  **Create an Empty GameObject** in your scene, rename it to `MyAICharacter`.
3.  **Attach the `AICharacterController` component** (from this script) to `MyAICharacter`.
4.  **Attach the `UtilitySystem` component** (from this script) to `MyAICharacter`.
5.  **Create Assets:**
    *   Right-click in your Project window -> Create -> AI Utility System -> **Considerations** (create several: `HungerConsideration`, `ThirstConsideration`, `EnemyProximityConsideration`, `HasResourceConsideration`, `LowHealthConsideration`). Configure their values in the Inspector.
    *   Right-click in your Project window -> Create -> AI Utility System -> **Actions** (create several: `EatAction`, `DrinkAction`, `AttackAction`, `FleeAction`, `WanderAction`, `HealAction`). Configure their values.
    *   Right-click in your Project window -> Create -> AI Utility System -> **Appraisals** (create one for each desired behavior, e.g., `EatFoodAppraisal`, `DrinkWaterAppraisal`, `AttackEnemyAppraisal`, `FleeEnemyAppraisal`, `WanderAppraisal`, `HealSelfAppraisal`). For each Appraisal:
        *   Assign its corresponding `Action`.
        *   Drag and drop the relevant `Consideration` assets into its `Considerations` list.
        *   Choose an `Aggregator` (Product is usually good).
6.  **Configure `UtilitySystem`:** Drag all your created `Appraisal` assets into the `Appraisals` list on the `UtilitySystem` component attached to `MyAICharacter`. Set a `Decision Frequency`.
7.  **Create Placeholder Objects:**
    *   Create a 3D Cube, tag it `Enemy`.
    *   Create a 3D Sphere, tag it `Food`.
    *   Create a 3D Cylinder, tag it `Water`.
    *   Position them around your `MyAICharacter`.
8.  **Run the Scene!** Observe the debug logs and the AI Character's state changes.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // For LINQ operations like .OrderByDescending

// We put everything in a namespace to prevent naming conflicts in larger projects.
namespace AIUtilitySystem
{
    // ====================================================================================================
    // 0. AI Context: The Data Hub
    // This class holds all the relevant information an AI agent needs to make decisions.
    // It acts as a snapshot of the world state for the AI at a given moment.
    // Considerations read from it, and Actions may modify it (indirectly, via AICharacterController).
    // ====================================================================================================
    public class AIContext
    {
        public AICharacterController Character; // Reference to the AI agent itself
        public float CurrentHealth;
        public float MaxHealth;
        public float CurrentHunger; // 0 = full, 1 = starving
        public float CurrentThirst; // 0 = hydrated, 1 = dehydrated

        public GameObject NearestEnemy; // Null if no enemy detected
        public float DistanceToEnemy;
        public GameObject NearestFoodSource; // Null if no food detected
        public float DistanceToFood;
        public GameObject NearestWaterSource; // Null if no water detected
        public float DistanceToWater;

        // Add any other relevant information for decision making
        public bool HasLowHealth => CurrentHealth <= MaxHealth * 0.3f;
        public bool IsHungry => CurrentHunger >= 0.5f;
        public bool IsThirsty => CurrentThirst >= 0.5f;
    }

    // ====================================================================================================
    // 1. Consideration: Evaluating Factors
    // Considerations are individual metrics that contribute to an action's utility score.
    // Each consideration evaluates a specific aspect of the AIContext and returns a score between 0 and 1.
    // A score of 1 typically means "this factor strongly supports the action," and 0 means "this factor strongly opposes/prevents the action."
    // By making Considerations ScriptableObjects, we can create reusable assets that define scoring logic.
    // ====================================================================================================
    public abstract class Consideration : ScriptableObject
    {
        [Tooltip("Higher weight means this consideration has a greater impact on the final appraisal score.")]
        [Range(0.01f, 2f)] public float Weight = 1f;

        // The core method that all considerations must implement.
        // It takes the current AIContext and returns a score (0-1).
        public abstract float ScoreConsideration(AIContext context);
    }

    // --- Concrete Consideration Examples ---

    [CreateAssetMenu(fileName = "HungerConsideration", menuName = "AI Utility System/Considerations/Hunger")]
    public class HungerConsideration : Consideration
    {
        [Tooltip("The hunger level at which this consideration starts to have a significant effect.")]
        [Range(0f, 1f)] public float HungerThreshold = 0.5f;

        public override float ScoreConsideration(AIContext context)
        {
            // Score increases linearly from 0 at HungerThreshold to 1 at 1.0 (starving).
            // If hunger is below threshold, score is 0.
            if (context.CurrentHunger < HungerThreshold)
            {
                return 0f;
            }
            return Mathf.InverseLerp(HungerThreshold, 1f, context.CurrentHunger) * Weight;
        }
    }

    [CreateAssetMenu(fileName = "ThirstConsideration", menuName = "AI Utility System/Considerations/Thirst")]
    public class ThirstConsideration : Consideration
    {
        [Tooltip("The thirst level at which this consideration starts to have a significant effect.")]
        [Range(0f, 1f)] public float ThirstThreshold = 0.5f;

        public override float ScoreConsideration(AIContext context)
        {
            if (context.CurrentThirst < ThirstThreshold)
            {
                return 0f;
            }
            return Mathf.InverseLerp(ThirstThreshold, 1f, context.CurrentThirst) * Weight;
        }
    }

    [CreateAssetMenu(fileName = "EnemyProximityConsideration", menuName = "AI Utility System/Considerations/Enemy Proximity")]
    public class EnemyProximityConsideration : Consideration
    {
        [Tooltip("The maximum distance an enemy can be to influence this consideration's score.")]
        public float MaxEffectiveDistance = 20f;
        [Tooltip("If true, score increases as enemy gets closer. If false, score decreases as enemy gets closer.")]
        public bool ScoreInverseToDistance = true; // e.g., for attacking (higher when close) or fleeing (higher when close)

        public override float ScoreConsideration(AIContext context)
        {
            if (context.NearestEnemy == null)
            {
                return 0f; // No enemy, so this consideration doesn't apply
            }

            float normalizedDistance = Mathf.Clamp01(context.DistanceToEnemy / MaxEffectiveDistance);

            if (ScoreInverseToDistance)
            {
                // Score is higher when closer (e.g., for attacking or fearing)
                return (1f - normalizedDistance) * Weight;
            }
            else
            {
                // Score is higher when further (e.g., if you want to approach them cautiously)
                return normalizedDistance * Weight;
            }
        }
    }

    [CreateAssetMenu(fileName = "HasResourceConsideration", menuName = "AI Utility System/Considerations/Has Resource")]
    public class HasResourceConsideration : Consideration
    {
        public enum ResourceType { Food, Water, Enemy }
        public ResourceType resourceType;
        [Tooltip("If true, score is 1 if resource is present, 0 if not. If false, score is 1 if resource is ABSENT, 0 if present.")]
        public bool MustHaveResource = true;

        public override float ScoreConsideration(AIContext context)
        {
            bool hasResource = false;
            switch (resourceType)
            {
                case ResourceType.Food:
                    hasResource = context.NearestFoodSource != null;
                    break;
                case ResourceType.Water:
                    hasResource = context.NearestWaterSource != null;
                    break;
                case ResourceType.Enemy:
                    hasResource = context.NearestEnemy != null;
                    break;
            }

            return (MustHaveResource == hasResource ? 1f : 0f) * Weight;
        }
    }

    [CreateAssetMenu(fileName = "LowHealthConsideration", menuName = "AI Utility System/Considerations/Low Health")]
    public class LowHealthConsideration : Consideration
    {
        [Tooltip("The health percentage (0-1) below which this consideration starts to score higher.")]
        [Range(0f, 1f)] public float HealthThreshold = 0.3f;

        public override float ScoreConsideration(AIContext context)
        {
            // Score increases as health drops below the threshold.
            if (context.CurrentHealth > context.MaxHealth * HealthThreshold)
            {
                return 0f;
            }
            // Inverse lerp: At 0 health, score is 1. At HealthThreshold, score is 0.
            return Mathf.InverseLerp(context.MaxHealth * HealthThreshold, 0f, context.CurrentHealth) * Weight;
        }
    }


    // ====================================================================================================
    // 2. Action: The Behavior to Execute
    // Actions represent the actual behaviors an AI agent can perform.
    // They define what happens when an action is chosen by the Utility System.
    // Actions are also ScriptableObjects for reusability and design-time configuration.
    // ====================================================================================================
    public abstract class Action : ScriptableObject
    {
        [Header("Action Configuration")]
        [Tooltip("A unique name for this action, used for debugging and logging.")]
        public string ActionName = "Default Action";
        [Tooltip("Can this action only be executed once, or is it repeatable immediately?")]
        public bool IsOneShot = false; // e.g., eat once, then maybe need to find new food

        // Called when the action is chosen and needs to be executed.
        public abstract void Execute(AIContext context);

        // Optional: A precondition check that ensures the action can *actually* be executed.
        // This is distinct from considerations, which merely score desirability.
        // For example, an "Eat" action might only be executable if there's food *actually* reachable.
        public virtual bool CanExecute(AIContext context) { return true; }

        public string GetActionName() => ActionName;
    }

    // --- Concrete Action Examples ---

    [CreateAssetMenu(fileName = "EatAction", menuName = "AI Utility System/Actions/Eat")]
    public class EatAction : Action
    {
        public override void Execute(AIContext context)
        {
            if (context.NearestFoodSource != null)
            {
                Debug.Log($"<color=green>{context.Character.name} is eating from {context.NearestFoodSource.name}. Hunger -{context.Character.HungerDecreaseRateWhenEating}.</color>");
                context.Character.ConsumeFood();
                // In a real game, you might destroy/deplete the food source
                // Destroy(context.NearestFoodSource);
            }
            else
            {
                Debug.LogWarning($"{context.Character.name} tried to eat but no food source found!");
            }
        }

        public override bool CanExecute(AIContext context)
        {
            return context.NearestFoodSource != null;
        }
    }

    [CreateAssetMenu(fileName = "DrinkAction", menuName = "AI Utility System/Actions/Drink")]
    public class DrinkAction : Action
    {
        public override void Execute(AIContext context)
        {
            if (context.NearestWaterSource != null)
            {
                Debug.Log($"<color=blue>{context.Character.name} is drinking from {context.NearestWaterSource.name}. Thirst -{context.Character.ThirstDecreaseRateWhenDrinking}.</color>");
                context.Character.ConsumeWater();
            }
            else
            {
                Debug.LogWarning($"{context.Character.name} tried to drink but no water source found!");
            }
        }

        public override bool CanExecute(AIContext context)
        {
            return context.NearestWaterSource != null;
        }
    }

    [CreateAssetMenu(fileName = "AttackAction", menuName = "AI Utility System/Actions/Attack")]
    public class AttackAction : Action
    {
        public override void Execute(AIContext context)
        {
            if (context.NearestEnemy != null)
            {
                Debug.Log($"<color=red>{context.Character.name} is attacking {context.NearestEnemy.name}! Target Health: {context.NearestEnemy.GetComponent<AICharacterController>()?.Health ?? -1}.</color>");
                // Simulate damage or combat logic
                AICharacterController enemyController = context.NearestEnemy.GetComponent<AICharacterController>();
                if (enemyController != null)
                {
                    enemyController.TakeDamage(10); // Example damage
                }
            }
            else
            {
                Debug.LogWarning($"{context.Character.name} tried to attack but no enemy found!");
            }
        }

        public override bool CanExecute(AIContext context)
        {
            return context.NearestEnemy != null; // And maybe range check, line of sight, etc.
        }
    }

    [CreateAssetMenu(fileName = "FleeAction", menuName = "AI Utility System/Actions/Flee")]
    public class FleeAction : Action
    {
        public override void Execute(AIContext context)
        {
            if (context.NearestEnemy != null)
            {
                Vector3 fleeDirection = (context.Character.transform.position - context.NearestEnemy.transform.position).normalized;
                Vector3 fleeTarget = context.Character.transform.position + fleeDirection * 10f; // Flee 10 units away
                Debug.Log($"<color=orange>{context.Character.name} is fleeing from {context.NearestEnemy.name} towards {fleeTarget}.</color>");
                context.Character.MoveTo(fleeTarget);
            }
            else
            {
                Debug.LogWarning($"{context.Character.name} tried to flee but no enemy found!");
            }
        }

        public override bool CanExecute(AIContext context)
        {
            return context.NearestEnemy != null;
        }
    }

    [CreateAssetMenu(fileName = "WanderAction", menuName = "AI Utility System/Actions/Wander")]
    public class WanderAction : Action
    {
        public float WanderRadius = 10f;

        public override void Execute(AIContext context)
        {
            Vector3 randomPoint = context.Character.transform.position + Random.insideUnitSphere * WanderRadius;
            randomPoint.y = context.Character.transform.position.y; // Keep on ground plane
            Debug.Log($"<color=grey>{context.Character.name} is wandering to {randomPoint}.</color>");
            context.Character.MoveTo(randomPoint);
        }
    }

    [CreateAssetMenu(fileName = "HealAction", menuName = "AI Utility System/Actions/Heal")]
    public class HealAction : Action
    {
        public float HealAmount = 20f;

        public override void Execute(AIContext context)
        {
            if (context.CurrentHealth < context.MaxHealth)
            {
                Debug.Log($"<color=purple>{context.Character.name} is healing. Health +{HealAmount}.</color>");
                context.Character.Heal(HealAmount);
            }
            else
            {
                Debug.LogWarning($"{context.Character.name} tried to heal but already at full health!");
            }
        }

        public override bool CanExecute(AIContext context)
        {
            return context.CurrentHealth < context.MaxHealth;
        }
    }


    // ====================================================================================================
    // 3. Appraisal: Combining Considerations for an Action
    // An Appraisal links an Action with a set of Considerations.
    // It evaluates the total utility score for performing that specific Action given the current context.
    // Appraisals are ScriptableObjects, allowing for flexible configuration of behaviors.
    // ====================================================================================================
    public enum ConsiderationAggregator { Product, Sum, Average }

    [CreateAssetMenu(fileName = "NewAppraisal", menuName = "AI Utility System/Appraisal")]
    public class Appraisal : ScriptableObject
    {
        [Header("Behavior")]
        [Tooltip("The action that this appraisal evaluates.")]
        public Action Action;
        [Tooltip("A list of considerations that determine the utility of this action.")]
        public List<Consideration> Considerations = new List<Consideration>();
        [Tooltip("The method used to combine the scores from all considerations.")]
        public ConsiderationAggregator Aggregator = ConsiderationAggregator.Product;

        [Header("Base Score & Thresholds")]
        [Tooltip("A base score applied to this appraisal, regardless of considerations. Useful for default actions.")]
        [Range(0f, 1f)] public float BaseScore = 0f;
        [Tooltip("If the final score is below this threshold, the action will not be considered.")]
        [Range(0f, 1f)] public float ScoreThreshold = 0f;

        // Evaluates the total utility score for this appraisal's action.
        public float Evaluate(AIContext context)
        {
            if (Action == null)
            {
                Debug.LogError($"Appraisal '{name}' has no Action assigned!", this);
                return 0f;
            }

            // If the action's pre-condition is not met, return 0 immediately.
            if (!Action.CanExecute(context))
            {
                return 0f;
            }

            float aggregateScore = BaseScore;
            if (Considerations.Count == 0)
            {
                // If no considerations, just use the base score (e.g., for a default "wander" action)
                aggregateScore = Mathf.Clamp01(BaseScore);
            }
            else
            {
                // Calculate individual consideration scores
                List<float> considerationScores = new List<float>();
                foreach (var consideration in Considerations)
                {
                    if (consideration == null)
                    {
                        Debug.LogWarning($"Appraisal '{name}' has a null consideration in its list. Skipping.", this);
                        continue;
                    }
                    considerationScores.Add(consideration.ScoreConsideration(context));
                }

                // Aggregate scores based on the chosen method
                if (considerationScores.Count > 0)
                {
                    switch (Aggregator)
                    {
                        case ConsiderationAggregator.Product:
                            aggregateScore = 1.0f; // Start at 1 for product
                            foreach (float score in considerationScores)
                            {
                                aggregateScore *= score;
                            }
                            break;
                        case ConsiderationAggregator.Sum:
                            aggregateScore = considerationScores.Sum();
                            break;
                        case ConsiderationAggregator.Average:
                            aggregateScore = considerationScores.Average();
                            break;
                    }
                }
            }

            // Ensure score is clamped between 0 and 1
            aggregateScore = Mathf.Clamp01(aggregateScore);

            // Apply threshold
            if (aggregateScore < ScoreThreshold)
            {
                return 0f;
            }

            return aggregateScore;
        }
    }


    // ====================================================================================================
    // 4. UtilitySystem: The Decision Maker (MonoBehaviour)
    // This is the core MonoBehaviour that lives on the AI agent.
    // It orchestrates the decision-making process by evaluating all appraisals and executing the best action.
    // ====================================================================================================
    [RequireComponent(typeof(AICharacterController))]
    public class UtilitySystem : MonoBehaviour
    {
        [Header("Utility System Configuration")]
        [Tooltip("List of all possible appraisals (action + considerations) for this AI.")]
        public List<Appraisal> Appraisals = new List<Appraisal>();
        [Tooltip("How often (in seconds) the AI should re-evaluate its actions.")]
        public float DecisionFrequency = 0.5f;

        // Internal state
        private AIContext _currentContext;
        private AICharacterController _aiCharacter;
        private Action _activeAction;
        private Coroutine _decisionCoroutine;

        void Awake()
        {
            _aiCharacter = GetComponent<AICharacterController>();
            if (_aiCharacter == null)
            {
                Debug.LogError("AIUtilitySystem requires an AICharacterController component!", this);
                enabled = false;
                return;
            }

            _currentContext = new AIContext();
            _currentContext.Character = _aiCharacter; // Link context to character controller
        }

        void OnEnable()
        {
            // Start the decision loop when the component is enabled.
            if (_decisionCoroutine != null) StopCoroutine(_decisionCoroutine);
            _decisionCoroutine = StartCoroutine(DecisionLoop());
        }

        void OnDisable()
        {
            // Stop the decision loop when the component is disabled.
            if (_decisionCoroutine != null) StopCoroutine(_decisionCoroutine);
            _decisionCoroutine = null;
        }

        // The main decision-making loop
        private IEnumerator DecisionLoop()
        {
            while (true)
            {
                MakeDecision();
                yield return new WaitForSeconds(DecisionFrequency);
            }
        }

        private void MakeDecision()
        {
            // 1. Populate the AIContext with the current world state.
            PopulateContext();

            Appraisal bestAppraisal = null;
            float highestScore = 0f;

            // 2. Evaluate all available appraisals.
            foreach (var appraisal in Appraisals)
            {
                if (appraisal == null)
                {
                    Debug.LogWarning($"UtilitySystem on '{name}' has a null appraisal in its list. Skipping.", this);
                    continue;
                }

                float score = appraisal.Evaluate(_currentContext);

                // For debugging:
                // Debug.Log($"Appraisal: {appraisal.name}, Action: {appraisal.Action?.GetActionName() ?? "None"}, Score: {score:F2}");

                if (score > highestScore)
                {
                    highestScore = score;
                    bestAppraisal = appraisal;
                }
            }

            // 3. Execute the action from the appraisal with the highest score.
            if (bestAppraisal != null && highestScore > 0)
            {
                if (_activeAction != bestAppraisal.Action)
                {
                    _activeAction = bestAppraisal.Action;
                    Debug.Log($"<color=cyan>AI Decision: Executing '{_activeAction.GetActionName()}' with score: {highestScore:F2}</color>");
                }
                _activeAction.Execute(_currentContext);
            }
            else
            {
                // Fallback if no appraisal scored high enough.
                Debug.LogWarning("<color=red>AI Decision: No suitable action found (or score too low).</color>");
                _activeAction = null;
            }
        }

        // Gathers all relevant data for the AI's decision.
        private void PopulateContext()
        {
            _currentContext.CurrentHealth = _aiCharacter.Health;
            _currentContext.MaxHealth = _aiCharacter.MaxHealth;
            _currentContext.CurrentHunger = _aiCharacter.Hunger;
            _currentContext.CurrentThirst = _aiCharacter.Thirst;
            _currentContext.AICharacterTransform = _aiCharacter.transform; // The AI's own transform

            // Find nearest enemy, food, water
            _currentContext.NearestEnemy = FindNearestObjectByTag("Enemy", out _currentContext.DistanceToEnemy);
            _currentContext.NearestFoodSource = FindNearestObjectByTag("Food", out _currentContext.DistanceToFood);
            _currentContext.NearestWaterSource = FindNearestObjectByTag("Water", out _currentContext.DistanceToWater);
        }

        // Helper function to find the nearest object by tag.
        private GameObject FindNearestObjectByTag(string tag, out float distance)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            GameObject nearestObject = null;
            float minDistance = float.MaxValue;
            distance = float.MaxValue;

            foreach (GameObject obj in objects)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestObject = obj;
                }
            }

            distance = minDistance;
            return nearestObject;
        }
    }


    // ====================================================================================================
    // 5. AICharacterController: The AI Agent's State and Interface (MonoBehaviour)
    // This component manages the AI character's internal state (health, hunger, thirst)
    // and provides methods for actions to interact with the character (e.g., MoveTo, ConsumeFood).
    // It's the physical representation of the AI in the world.
    // ====================================================================================================
    public class AICharacterController : MonoBehaviour
    {
        [Header("Character Stats")]
        public float MaxHealth = 100f;
        [Range(0f, 100f)] public float Health = 100f;
        [Range(0f, 1f)] public float Hunger = 0f; // 0 = full, 1 = starving
        [Range(0f, 1f)] public float Thirst = 0f; // 0 = hydrated, 1 = dehydrated

        [Header("Stat Management")]
        public float HungerRate = 0.01f; // How much hunger increases per second
        public float ThirstRate = 0.02f; // How much thirst increases per second
        public float HungerDecreaseRateWhenEating = 0.3f; // How much hunger decreases per eat action
        public float ThirstDecreaseRateWhenDrinking = 0.4f; // How much thirst decreases per drink action
        public float HealthRegenRate = 0.05f; // Health regen per second when not low and not fighting

        [Header("Movement")]
        public float MoveSpeed = 3f;
        public float RotationSpeed = 5f;
        public float MinArrivalDistance = 0.5f;

        private Vector3 _targetPosition;
        private bool _isMoving = false;

        void Start()
        {
            Health = MaxHealth;
            _targetPosition = transform.position; // Start at current position
        }

        void Update()
        {
            // Simulate natural stat changes
            Hunger = Mathf.Min(1f, Hunger + HungerRate * Time.deltaTime);
            Thirst = Mathf.Min(1f, Thirst + ThirstRate * Time.deltaTime);

            // Basic health regeneration (if not critical)
            if (Health < MaxHealth && Health > MaxHealth * 0.3f)
            {
                Health = Mathf.Min(MaxHealth, Health + HealthRegenRate * Time.deltaTime);
            }

            // Movement logic
            if (_isMoving)
            {
                Vector3 direction = (_targetPosition - transform.position).normalized;
                transform.position += direction * MoveSpeed * Time.deltaTime;

                // Rotate towards target
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetPosition) < MinArrivalDistance)
                {
                    _isMoving = false;
                    Debug.Log($"Arrived at {_targetPosition}.");
                }
            }

            // Simple visual feedback
            // Change color based on primary need
            if (Hunger >= 0.7f) GetComponent<Renderer>().material.color = Color.yellow;
            else if (Thirst >= 0.7f) GetComponent<Renderer>().material.color = Color.cyan;
            else if (Health <= MaxHealth * 0.3f) GetComponent<Renderer>().material.color = Color.magenta;
            else GetComponent<Renderer>().material.color = Color.gray; // Default
        }

        public void ConsumeFood()
        {
            Hunger = Mathf.Max(0f, Hunger - HungerDecreaseRateWhenEating);
            // Optionally remove or signal the food source
        }

        public void ConsumeWater()
        {
            Thirst = Mathf.Max(0f, Thirst - ThirstDecreaseRateWhenDrinking);
            // Optionally remove or signal the water source
        }

        public void TakeDamage(float amount)
        {
            Health = Mathf.Max(0f, Health - amount);
            Debug.Log($"{name} took {amount} damage. Health: {Health}");
            if (Health <= 0)
            {
                Debug.Log($"{name} has been defeated!");
                Destroy(gameObject); // Example: destroy the character
            }
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void MoveTo(Vector3 position)
        {
            _targetPosition = position;
            _isMoving = true;
        }
    }
}
```