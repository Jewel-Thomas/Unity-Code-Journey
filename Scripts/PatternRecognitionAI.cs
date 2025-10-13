// Unity Design Pattern Example: PatternRecognitionAI
// This script demonstrates the PatternRecognitionAI pattern in Unity
// Generated automatically - ready to use in your Unity project

The Pattern Recognition AI design pattern focuses on an AI system that continuously observes its environment, identifies specific data patterns (e.g., player behavior, game state, environmental conditions), and reacts with predefined actions when a pattern is recognized. This pattern is highly effective for creating adaptive and intelligent AI that can respond dynamically to complex situations rather than following rigid state machines.

**Real-World Unity Use Case:**
An enemy AI that observes the player's actions and health, then chooses an optimal strategy (e.g., charge when the player is low on health, retreat when the enemy itself is low, use a ranged attack if the player is far away, deploy a shield if the player is attacking rapidly).

---

### Key Components of the Pattern Recognition AI:

1.  **`RecognitionContext`**: A data object (struct or class) that holds all relevant information the patterns might need to evaluate the current game state. This centralizes data gathering and makes patterns more reusable and testable.
2.  **`IPattern` (or Abstract Base Class `EnemyPattern`)**: An interface or abstract class that defines a method (e.g., `IsRecognized(RecognitionContext context)`) which returns `true` if the pattern matches the current context. Each concrete pattern implements this to define its specific recognition logic.
3.  **`IResponse` (or Abstract Base Class `EnemyAIResponse`)**: An interface or abstract class that defines a method (e.g., `Execute(RecognitionContext context)`) which performs an action when a pattern is recognized. Each concrete response implements this to define its specific action.
4.  **`PatternRecognitionRule`**: A container that pairs a specific `EnemyPattern` with an `EnemyAIResponse`. This makes it easy to associate "if this pattern, then do this response."
5.  **`PatternRecognitionAI` (or `EnemyPatternRecognitionAI`)**: The main AI component. It holds a collection of `PatternRecognitionRule`s. In its `Update` loop, it gathers the `RecognitionContext`, iterates through its rules, checks if any pattern is recognized, and executes the associated response. It often prioritizes rules, executing only the first one found or all matching ones.

---

### Unity Implementation Details:

*   We'll use `[System.Serializable]` for our base pattern and response classes, and for their concrete implementations, allowing them to be configured directly in the Unity Inspector.
*   For flexible assignment of derived patterns and responses in lists, we'll leverage `[SerializeReference]`. **Note:** `[SerializeReference]` requires Unity 2019.3 or newer. If you are using an older version, you would need to use `ScriptableObject` assets for each pattern/response type, or a custom editor to draw them.
*   We'll create dummy `Player` and `Enemy` classes to provide a runnable example.

---

### Complete Unity Example

This example includes a basic `PlayerController`, an `EnemyHealth` script, and the core `PatternRecognitionEnemyAI`.

**1. Create the `PlayerController.cs` script:**
This will simulate a player with health and an attack.

```csharp
// PlayerController.cs
using UnityEngine;

namespace PatternRecognitionAI
{
    /// <summary>
    /// A simple Player Controller for demonstration purposes.
    /// Manages player health and simulated actions.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Stats")]
        [Tooltip("The maximum health of the player.")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("The player's current health.")]
        [SerializeField] private float currentHealth;

        [Header("Player Actions (Simulated)")]
        [Tooltip("Simulates the player attacking.")]
        [SerializeField] private bool isAttacking = false;
        [Tooltip("Simulates the player healing.")]
        [SerializeField] private bool isHealing = false;
        [Tooltip("Simulates the player dodging.")]
        [SerializeField] private bool isDodging = false;
        [Tooltip("Damage dealt per attack.")]
        [SerializeField] private float attackDamage = 10f;
        [Tooltip("Health restored per heal.")]
        [SerializeField] private float healAmount = 15f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => currentHealth / maxHealth;
        public bool IsAttacking => isAttacking;
        public bool IsHealing => isHealing;
        public bool IsDodging => isDodging;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            // --- Simulate Player Actions via Inspector toggles ---
            // These would normally be driven by player input or other game logic.

            if (isHealing)
            {
                Heal(healAmount * Time.deltaTime); // Heal over time if toggled
            }
            // Reset actions after a frame, unless persistent for demo
            // For this demo, leave toggles to manually show states.
        }

        /// <summary>
        /// Applies damage to the player.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
            Debug.Log($"Player took {damage} damage. Current Health: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"Player healed {amount}. Current Health: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Simulates the player attacking another entity.
        /// </summary>
        /// <param name="target">The transform of the entity being attacked.</param>
        public void PerformAttack(Transform target)
        {
            // In a real game, this would trigger animations, deal damage to target, etc.
            Debug.Log($"Player performs an attack towards {target.name}.");
            isAttacking = true; // Set for one frame for pattern recognition
            // In a real game, you might reset this after an animation.
            // For demo purposes, we'll keep it true as long as the inspector toggle is true.
        }

        /// <summary>
        /// Resets the attack state. Call this after an attack animation/event.
        /// </summary>
        public void ResetAttackState()
        {
            isAttacking = false;
        }
    }
}

```

**2. Create the `EnemyHealth.cs` script:**
A simple health component for the enemy.

```csharp
// EnemyHealth.cs
using UnityEngine;

namespace PatternRecognitionAI
{
    /// <summary>
    /// Simple health component for the enemy, for demonstration purposes.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Tooltip("The maximum health of the enemy.")]
        [SerializeField] private float maxHealth = 150f;
        [Tooltip("The enemy's current health.")]
        [SerializeField] private float currentHealth;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => currentHealth / maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Applies damage to the enemy.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
            Debug.Log($"Enemy took {damage} damage. Current Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} has been defeated!");
                // Optionally disable or destroy the GameObject
                // gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Heals the enemy.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"Enemy healed {amount}. Current Health: {currentHealth}/{maxHealth}");
        }
    }
}
```

**3. Create the `PatternRecognitionEnemyAI.cs` script:**
This is the core implementation of the Pattern Recognition AI.

```csharp
// PatternRecognitionEnemyAI.cs
using UnityEngine;
using System.Collections.Generic;

namespace PatternRecognitionAI
{
    /// <summary>
    /// Represents the current game state context that patterns will evaluate against.
    /// This centralizes data gathering for all patterns.
    /// </summary>
    public class EnemyAIContext
    {
        public Transform enemyTransform;
        public EnemyHealth enemyHealth;
        public Transform playerTransform;
        public PlayerController playerController;

        public float playerDistance;
        public float playerHealthNormalized;
        public bool isPlayerAttacking;
        public bool isPlayerHealing;
        public bool isPlayerDodging;
        public float enemyHealthNormalized;

        public EnemyAIContext(Transform enemyT, EnemyHealth enemyH, Transform playerT, PlayerController playerC)
        {
            enemyTransform = enemyT;
            enemyHealth = enemyH;
            playerTransform = playerT;
            playerController = playerC;

            // Initialize context data
            if (enemyTransform != null && playerTransform != null)
            {
                playerDistance = Vector3.Distance(enemyTransform.position, playerTransform.position);
            }
            playerHealthNormalized = playerController?.HealthNormalized ?? 1f;
            isPlayerAttacking = playerController?.IsAttacking ?? false;
            isPlayerHealing = playerController?.IsHealing ?? false;
            isPlayerDodging = playerController?.IsDodging ?? false;
            enemyHealthNormalized = enemyHealth?.HealthNormalized ?? 1f;
        }
    }

    /// <summary>
    /// Base abstract class for all enemy patterns.
    /// Concrete patterns will inherit from this and implement the IsRecognized method.
    /// Mark with [System.Serializable] to allow it to be shown and configured in the Inspector.
    /// </summary>
    [System.Serializable]
    public abstract class EnemyPattern
    {
        [Tooltip("A descriptive name for this pattern.")]
        public string patternName = "New Pattern";

        /// <summary>
        /// Determines if this pattern is recognized based on the current context.
        /// </summary>
        /// <param name="context">The current game state context.</param>
        /// <returns>True if the pattern is recognized, false otherwise.</returns>
        public abstract bool IsRecognized(EnemyAIContext context);
    }

    /// <summary>
    /// Concrete Pattern: Player's health is below a certain threshold.
    /// </summary>
    [System.Serializable]
    public class PlayerLowHealthPattern : EnemyPattern
    {
        [Range(0, 1)]
        [Tooltip("Player's health percentage below which this pattern is recognized.")]
        public float threshold = 0.25f; // 25% health

        public override bool IsRecognized(EnemyAIContext context)
        {
            return context.playerHealthNormalized < threshold;
        }
    }

    /// <summary>
    /// Concrete Pattern: Player is far away from the enemy.
    /// </summary>
    [System.Serializable]
    public class PlayerFarAwayPattern : EnemyPattern
    {
        [Tooltip("Distance threshold beyond which the player is considered 'far away'.")]
        public float distanceThreshold = 10f;

        public override bool IsRecognized(EnemyAIContext context)
        {
            return context.playerDistance > distanceThreshold;
        }
    }

    /// <summary>
    /// Concrete Pattern: Player is close and actively attacking the enemy.
    /// </summary>
    [System.Serializable]
    public class PlayerCloseAndAggressivePattern : EnemyPattern
    {
        [Tooltip("Distance threshold below which the player is considered 'close'.")]
        public float distanceThreshold = 3f;

        public override bool IsRecognized(EnemyAIContext context)
        {
            return context.playerDistance < distanceThreshold && context.isPlayerAttacking;
        }
    }

    /// <summary>
    /// Concrete Pattern: Enemy's own health is low.
    /// </summary>
    [System.Serializable]
    public class EnemyLowHealthPattern : EnemyPattern
    {
        [Range(0, 1)]
        [Tooltip("Enemy's health percentage below which this pattern is recognized.")]
        public float threshold = 0.30f; // 30% health

        public override bool IsRecognized(EnemyAIContext context)
        {
            return context.enemyHealthNormalized < threshold;
        }
    }

    /// <summary>
    /// Concrete Pattern: Player is attempting to heal.
    /// </summary>
    [System.Serializable]
    public class PlayerHealingPattern : EnemyPattern
    {
        public override bool IsRecognized(EnemyAIContext context)
        {
            return context.isPlayerHealing;
        }
    }


    /// <summary>
    /// Base abstract class for all enemy AI responses/actions.
    /// Concrete responses will inherit from this and implement the Execute method.
    /// Mark with [System.Serializable] to allow it to be shown and configured in the Inspector.
    /// </summary>
    [System.Serializable]
    public abstract class EnemyAIResponse
    {
        [Tooltip("A descriptive name for this response.")]
        public string responseName = "New Response";

        /// <summary>
        /// Executes the AI's action based on the current context.
        /// </summary>
        /// <param name="context">The current game state context.</param>
        public abstract void Execute(EnemyAIContext context);
    }

    /// <summary>
    /// Concrete Response: Perform a charge attack.
    /// </summary>
    [System.Serializable]
    public class ChargeAttackResponse : EnemyAIResponse
    {
        [Tooltip("Damage dealt by the charge attack.")]
        public float damage = 20f;

        public override void Execute(EnemyAIContext context)
        {
            Debug.Log($"ENEMY AI: Initiating Charge Attack! Dealing {damage} damage to player.");
            context.playerController?.TakeDamage(damage);
            // In a real game, this would trigger animations, movement towards player, etc.
        }
    }

    /// <summary>
    /// Concrete Response: Perform a ranged attack.
    /// </summary>
    [System.Serializable]
    public class RangedAttackResponse : EnemyAIResponse
    {
        [Tooltip("Damage dealt by the ranged attack.")]
        public float damage = 15f;
        [Tooltip("VFX for ranged attack (e.g., projectile prefab).")]
        public GameObject projectilePrefab; // Example: Add a projectile prefab to shoot

        public override void Execute(EnemyAIContext context)
        {
            Debug.Log($"ENEMY AI: Firing Ranged Attack! Dealing {damage} damage to player.");
            context.playerController?.TakeDamage(damage); // Direct damage for simplicity
            // In a real game, instantiate projectilePrefab, aim at player, etc.
        }
    }

    /// <summary>
    /// Concrete Response: Retreat and try to heal.
    /// </summary>
    [System.Serializable]
    public class RetreatAndHealResponse : EnemyAIResponse
    {
        [Tooltip("Amount of health to heal over time during retreat.")]
        public float healAmount = 5f;
        [Tooltip("Speed multiplier for retreat movement.")]
        public float retreatSpeedMultiplier = 1.5f;

        public override void Execute(EnemyAIContext context)
        {
            Debug.Log($"ENEMY AI: Retreating and healing {healAmount} health!");
            context.enemyHealth?.Heal(healAmount * Time.deltaTime); // Heal over time
            // In a real game, this would involve pathfinding to a safe spot, changing movement speed.
            // Example: context.enemyTransform.Translate(-context.enemyTransform.forward * retreatSpeedMultiplier * Time.deltaTime);
        }
    }

    /// <summary>
    /// Concrete Response: Use a special ability (e.g., taunt).
    /// </summary>
    [System.Serializable]
    public class UseSpecialAbilityResponse : EnemyAIResponse
    {
        [Tooltip("The name of the special ability.")]
        public string abilityName = "Taunt";

        public override void Execute(EnemyAIContext context)
        {
            Debug.Log($"ENEMY AI: Using special ability: {abilityName}!");
            // In a real game, trigger ability specific effects, cooldowns, etc.
            // context.playerController.ApplyTauntEffect();
        }
    }

    /// <summary>
    /// Represents a single rule: if this pattern is recognized, then execute this response.
    /// Uses [SerializeReference] for flexible serialization of derived classes in the Inspector.
    /// </summary>
    [System.Serializable]
    public class PatternRecognitionRule
    {
        [Tooltip("The pattern to look for.")]
        [SerializeReference] // Requires Unity 2019.3+ for inspector support
        public EnemyPattern pattern;

        [Tooltip("The response to execute if the pattern is recognized.")]
        [SerializeReference] // Requires Unity 2019.3+ for inspector support
        public EnemyAIResponse response;
    }


    /// <summary>
    /// The main MonoBehaviour for the Pattern Recognition Enemy AI.
    /// It observes the game state, identifies patterns, and triggers responses.
    /// </summary>
    public class PatternRecognitionEnemyAI : MonoBehaviour
    {
        [Header("AI Dependencies")]
        [Tooltip("Reference to the player's Transform.")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("Reference to the player's health/controller script.")]
        [SerializeField] private PlayerController playerController;
        [Tooltip("Reference to this enemy's health script.")]
        [SerializeField] private EnemyHealth enemyHealth;

        [Header("AI Rules")]
        [Tooltip("A list of pattern recognition rules. The AI will evaluate these in order.")]
        [SerializeField] private List<PatternRecognitionRule> rules = new List<PatternRecognitionRule>();

        [Header("Default Behavior")]
        [Tooltip("Optional: A default response if no pattern is recognized.")]
        [SerializeField] private EnemyAIResponse defaultResponse;

        private EnemyAIContext _currentContext;

        private void Awake()
        {
            if (playerTransform == null)
            {
                Debug.LogError("Player Transform is not assigned to EnemyAI!", this);
                enabled = false;
                return;
            }
            if (playerController == null)
            {
                playerController = playerTransform.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("PlayerController not found on Player Transform!", this);
                    enabled = false;
                    return;
                }
            }
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
                if (enemyHealth == null)
                {
                    Debug.LogError("EnemyHealth not found on Enemy AI GameObject!", this);
                    enabled = false;
                    return;
                }
            }
        }

        private void Update()
        {
            // 1. Gather current game state into the context
            _currentContext = new EnemyAIContext(transform, enemyHealth, playerTransform, playerController);

            // 2. Evaluate patterns and execute responses
            bool patternRecognizedAndExecuted = false;
            foreach (var rule in rules)
            {
                if (rule.pattern != null && rule.response != null)
                {
                    if (rule.pattern.IsRecognized(_currentContext))
                    {
                        Debug.Log($"<color=cyan>AI recognized pattern: {rule.pattern.patternName}</color>");
                        rule.response.Execute(_currentContext);
                        patternRecognizedAndExecuted = true;
                        break; // Execute only the first recognized pattern (priority-based)
                               // For multiple pattern execution, remove this break.
                    }
                }
            }

            // 3. Execute default behavior if no specific pattern was recognized
            if (!patternRecognizedAndExecuted && defaultResponse != null)
            {
                Debug.Log("<color=grey>AI: No specific pattern recognized. Executing default response.</color>");
                defaultResponse.Execute(_currentContext);
            }

            // Reset player's attack state after evaluation for patterns that check for single-frame actions
            // In a real game, this might be handled by animation events or a more robust action manager.
            playerController.ResetAttackState();
        }
    }
}
```

---

### How to Set Up in Unity:

1.  **Create an Empty GameObject** in your scene, name it "Player".
    *   Add the `PlayerController.cs` script to it.
    *   Set its `Max Health` to `100`.

2.  **Create another Empty GameObject**, name it "Enemy".
    *   Add the `EnemyHealth.cs` script to it.
    *   Add the `PatternRecognitionEnemyAI.cs` script to it.
    *   Set its `Max Health` to `150`.

3.  **Configure the `PatternRecognitionEnemyAI` component on the "Enemy" GameObject:**
    *   Drag the "Player" GameObject from the Hierarchy into the `Player Transform` slot.
    *   The `Player Controller` and `Enemy Health` fields should auto-populate if the scripts are on the respective GameObjects.

4.  **Add Rules to the `Rules` List:**
    *   In the Inspector, expand the `Rules` list.
    *   Click the `+` button to add a new `Pattern Recognition Rule`.
    *   For each rule, you'll see two fields: `Pattern` and `Response`.
    *   Click the small target icon next to `Pattern` (or `Response`) and select `Make New ...` from the dropdown. This will let you choose a concrete pattern/response class from the derived types.

    **Example Rule Configuration:**

    *   **Rule 1: Player Low Health (High Priority)**
        *   **Pattern:** `Make New -> PlayerLowHealthPattern`
            *   `Pattern Name`: Player Is Low Health
            *   `Threshold`: 0.25 (player is below 25% health)
        *   **Response:** `Make New -> ChargeAttackResponse`
            *   `Response Name`: Finish Him!
            *   `Damage`: 50
        *(This rule should be at the top of the list for highest priority)*

    *   **Rule 2: Enemy Low Health (Self-Preservation)**
        *   **Pattern:** `Make New -> EnemyLowHealthPattern`
            *   `Pattern Name`: Enemy Is Low Health
            *   `Threshold`: 0.30 (enemy is below 30% health)
        *   **Response:** `Make New -> RetreatAndHealResponse`
            *   `Response Name`: Retreat and Heal
            *   `Heal Amount`: 10
            *   `Retreat Speed Multiplier`: 2
        *(Place this below 'Player Low Health' if you want to prioritize killing the player over self-preservation, or above if the enemy should prioritize its own survival)*

    *   **Rule 3: Player Healing (Interrupt)**
        *   **Pattern:** `Make New -> PlayerHealingPattern`
            *   `Pattern Name`: Player Is Healing
        *   **Response:** `Make New -> UseSpecialAbilityResponse`
            *   `Response Name`: Interrupt Healing
            *   `Ability Name`: Stun Bolt
        *(This would typically be a high-priority action)*

    *   **Rule 4: Player Far Away**
        *   **Pattern:** `Make New -> PlayerFarAwayPattern`
            *   `Pattern Name`: Player Is Far Away
            *   `Distance Threshold`: 10
        *   **Response:** `Make New -> RangedAttackResponse`
            *   `Response Name`: Shoot Ranged Projectile
            *   `Damage`: 15

    *   **Rule 5: Player Close and Aggressive**
        *   **Pattern:** `Make New -> PlayerCloseAndAggressivePattern`
            *   `Pattern Name`: Player Is Close & Aggressive
            *   `Distance Threshold`: 3
        *   **Response:** `Make New -> ChargeAttackResponse`
            *   `Response Name`: Counter Attack
            *   `Damage`: 25

    *   **Default Behavior (Optional)**
        *   **Default Response:** `Make New -> UseSpecialAbilityResponse`
            *   `Response Name`: Idle Taunt
            *   `Ability Name`: Taunt

5.  **Run the Scene:**
    *   Observe the Console output. The enemy AI will log which pattern it recognizes and what response it executes.
    *   **To test:**
        *   Select the "Player" GameObject in the Hierarchy.
        *   In its `PlayerController` component, try manually adjusting `Current Health` or toggling `Is Attacking`, `Is Healing`.
        *   Move the "Player" and "Enemy" GameObjects around in the Scene view to change their distance.
        *   Select the "Enemy" GameObject and adjust its `EnemyHealth` to trigger its low health pattern.

This setup provides a highly flexible and observable AI system that demonstrates the Pattern Recognition AI design pattern effectively in Unity. You can easily add more complex patterns and responses by simply creating new classes inheriting from `EnemyPattern` and `EnemyAIResponse`.