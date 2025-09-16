// Unity Design Pattern Example: HealthSystem
// This script demonstrates the HealthSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **HealthSystem Design Pattern**, focusing on **encapsulation** and **decoupling** through **events**. This pattern is crucial for managing any entity's health (Player, Enemy, destructible object) in a clean, scalable, and maintainable way.

When you use this `HealthSystem` script:
*   Other systems (e.g., `PlayerCombat`, `EnemyAI`) will *call* its public methods (`TakeDamage`, `Heal`).
*   Other systems (e.g., `HealthBarUI`, `GameManager`, `SoundFXManager`) will *subscribe* to its events (`OnHealthChanged`, `OnDied`) to react to changes without knowing the details of how health is managed.

This complete script is ready to be dropped into a Unity project.

---

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
///     The HealthSystem Design Pattern in Unity.
///     This script provides a robust and decoupled way to manage an entity's health.
///     It encapsulates health logic, notifies other systems via events, and promotes
///     a clear API for interaction.
/// </summary>
/// <remarks>
///     **Why use the HealthSystem Pattern?**
///     1.  **Encapsulation:** All health-related logic (taking damage, healing, dying, clamping values)
///         is contained within this single class. Other scripts don't directly modify health.
///     2.  **Decoupling:** Instead of directly calling methods on a UI script or an AI script,
///         the HealthSystem broadcasts events. Any script interested in health changes or death
///         can subscribe to these events. This means the HealthSystem doesn't need to know
///         anything about the UI, combat, or AI, making it reusable and easier to maintain.
///     3.  **Scalability:** Adding new features that react to health (e.g., a "low health" visual effect,
///         a specific sound on death) only requires subscribing to the existing events,
///         without modifying the core HealthSystem class.
///     4.  **Maintainability:** Changes to how health is calculated or managed (e.g., adding armor,
///         resistance) only need to be done in one place.
/// </remarks>
public class HealthSystem : MonoBehaviour
{
    // --- Inspector Fields ---
    [Header("Health Settings")]
    [Tooltip("The maximum health this entity can have.")]
    [SerializeField]
    private float _maxHealth = 100f;

    [Tooltip("The current health of this entity. Initialized to Max Health on Awake.")]
    [SerializeField]
    private float _currentHealth; // Exposed in Inspector for debugging. Managed internally.

    // --- Public Properties ---
    /// <summary>
    /// Gets the maximum health of this entity.
    /// </summary>
    public float MaxHealth => _maxHealth;

    /// <summary>
    /// Gets the current health of this entity.
    /// </summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>
    /// Gets a value indicating whether the entity is currently alive (health > 0).
    /// </summary>
    public bool IsAlive => _currentHealth > 0;

    /// <summary>
    /// Gets the current health as a percentage of max health (0 to 1).
    /// Returns 0 if MaxHealth is 0 to prevent division by zero.
    /// </summary>
    public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

    // --- Events (The core of decoupling in this pattern) ---

    /// <summary>
    /// **Event fired when the health changes.**
    /// Subscribers will receive the current health and max health.
    ///
    /// **Purpose:**
    /// -   **UI Updates:** A health bar UI script can subscribe to update visuals.
    /// -   **Visual Effects:** Play a "hit" flash or "heal" particle effect.
    /// -   **Sound Effects:** Play a "damage taken" or "healing received" sound.
    /// -   **AI Logic:** An enemy AI might react differently if its target's health is low.
    /// </summary>
    /// <param name="currentHealth">The new current health value.</param>
    /// <param name="maxHealth">The entity's maximum health value.</param>
    public event Action<float, float> OnHealthChanged;

    /// <summary>
    /// **Event fired when the entity's health drops to 0 or below, signifying death.**
    /// Subscribers will be notified of death.
    ///
    /// **Purpose:**
    /// -   **Game Over/Victory:** A GameManager can detect player or enemy death.
    /// -   **Animations:** Trigger death animations (ragdoll, dissolve, etc.).
    /// -   **Loot Dropping:** An ItemManager can spawn loot at the entity's position.
    /// -   **Entity Removal:** Destroy the GameObject, disable components, or despawn.
    /// -   **Sound Effects:** Play a death sound.
    /// </summary>
    public event Action OnDied;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Initialize current health to max health when the component starts.
        // This ensures the entity is full health at the beginning.
        _currentHealth = _maxHealth;
        // Optionally, invoke OnHealthChanged here if listeners need the initial state immediately.
        // OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    // --- Public API for Health Interaction ---

    /// <summary>
    /// Reduces the entity's current health by a specified amount.
    /// This is the primary method for inflicting damage.
    /// </summary>
    /// <param name="amount">The amount of damage to take. Must be positive.</param>
    public void TakeDamage(float amount)
    {
        if (!IsAlive)
        {
            // Already dead, cannot take more damage.
            Debug.LogWarning($"{gameObject.name}: Tried to take {amount} damage but is already dead.");
            return;
        }

        if (amount < 0)
        {
            Debug.LogWarning($"{gameObject.name}: Damage amount cannot be negative. Consider using Heal() instead. Amount: {amount}");
            return;
        }

        // Calculate new health and clamp it between 0 and _currentHealth
        // We use Mathf.Max(0, ...) to ensure health doesn't go below zero *before* death is processed,
        // which helps maintain clarity in the _UpdateHealth logic.
        float newHealth = Mathf.Max(0, _currentHealth - amount);
        _UpdateHealth(newHealth);
    }

    /// <summary>
    /// Increases the entity's current health by a specified amount.
    /// This is the primary method for restoring health.
    /// </summary>
    /// <param name="amount">The amount of health to restore. Must be positive.</param>
    public void Heal(float amount)
    {
        if (!IsAlive && _currentHealth <= 0) // Only heal if not explicitly dead (health 0 or less).
                                            // If health is somehow negative but entity isn't 'dead' in another system's eyes,
                                            // this allows healing out of that state.
        {
            Debug.LogWarning($"{gameObject.name}: Tried to heal {amount} but is explicitly dead. Revive first?");
            return;
        }

        if (amount < 0)
        {
            Debug.LogWarning($"{gameObject.name}: Heal amount cannot be negative. Consider using TakeDamage() instead. Amount: {amount}");
            return;
        }

        // Calculate new health and clamp it between _currentHealth and _maxHealth.
        // We use Mathf.Min(_maxHealth, ...) to ensure health doesn't exceed maximum.
        float newHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        _UpdateHealth(newHealth);
    }

    /// <summary>
    /// Restores the entity's health to its maximum value.
    /// </summary>
    public void RestoreFullHealth()
    {
        if (_currentHealth == _maxHealth)
        {
            Debug.Log($"{gameObject.name}: Already at full health.");
            return; // Already full health, no need to update.
        }

        _UpdateHealth(_maxHealth);
    }

    /// <summary>
    /// Immediately sets the entity's health to zero and triggers the death event.
    /// Useful for instant-kill mechanics or when death is determined by external factors.
    /// </summary>
    public void Die()
    {
        if (!IsAlive)
        {
            Debug.LogWarning($"{gameObject.name}: Tried to Die() but is already dead.");
            return;
        }

        // Set health to 0, which will trigger the OnHealthChanged and OnDied events
        // through the _UpdateHealth helper method.
        _UpdateHealth(0);
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Internal method to update the health value, clamp it, and invoke necessary events.
    /// This centralizes the core health update logic, preventing repetition.
    /// </summary>
    /// <param name="newHealth">The target health value after damage or healing.</param>
    private void _UpdateHealth(float newHealth)
    {
        // Store the state before the update to check if the entity *was* alive.
        bool wasAlive = IsAlive;

        // Clamp newHealth to ensure it stays within valid bounds (0 to _maxHealth).
        // This is a final safeguard, even if TakeDamage/Heal already apply some clamping.
        newHealth = Mathf.Clamp(newHealth, 0, _maxHealth);

        // Only update and notify if health has actually changed.
        // This prevents unnecessary event invocations and saves performance.
        if (Mathf.Approximately(newHealth, _currentHealth))
        {
            return;
        }

        _currentHealth = newHealth;

        // Invoke the OnHealthChanged event, notifying all subscribers.
        // This is where UI, visual effects, and sounds would react to health changes.
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        // Check for death condition AFTER updating current health.
        // We ensure OnDied is only invoked once when health *crosses* zero from a positive value.
        if (_currentHealth <= 0 && wasAlive)
        {
            // The entity has just died.
            // Invoke the OnDied event, notifying all subscribers.
            // This is where death animations, game over, loot drops, etc., would be triggered.
            OnDied?.Invoke();

            Debug.Log($"{gameObject.name} has died! ({Time.timeSinceLevelLoad:F2}s)");
            // Optional: You might want to disable collision, physics, or the GameObject itself here,
            // or delegate that to a subscriber of OnDied.
            // For example: GetComponent<Collider>().enabled = false;
            // For example: this.enabled = false; // Disable this HealthSystem component
        }
    }
}


/*
/// --- EXAMPLE USAGE IN OTHER SCRIPTS ---

// To use this HealthSystem, you would attach it to any GameObject that needs health,
// such as a Player, Enemy, or a destructible environment object.

// Here are examples of how other scripts would interact with the HealthSystem:

// 1.  **A Script for Handling Combat/Damage/Healing (e.g., PlayerCombat.cs or EnemyAI.cs)**
//     This script would *call* methods on the HealthSystem to apply changes.

//     public class PlayerCombat : MonoBehaviour
//     {
//         [SerializeField] private HealthSystem _healthSystem; // Drag HealthSystem component here in Inspector

//         void Start()
//         {
//             // Get the HealthSystem component if not assigned in Inspector
//             if (_healthSystem == null)
//             {
//                 _healthSystem = GetComponent<HealthSystem>();
//                 if (_healthSystem == null)
//                 {
//                     Debug.LogError("PlayerCombat: HealthSystem not found on " + gameObject.name + ". Disabling combat.", this);
//                     enabled = false; // Disable this script if no HealthSystem
//                     return;
//                 }
//             }

//             // Optional: This script might also need to react to health changes itself,
//             // e.g., to play a 'hit' animation or check if it died to disable player input.
//             _healthSystem.OnHealthChanged += HandleHealthChangedExternally;
//             _healthSystem.OnDied += HandleDeathExternally;
//         }

//         void OnDestroy()
//         {
//             // IMPORTANT: Always unsubscribe from events to prevent memory leaks and
//             // NullReferenceExceptions if the GameObject with HealthSystem is destroyed before this one.
//             if (_healthSystem != null)
//             {
//                 _healthSystem.OnHealthChanged -= HandleHealthChangedExternally;
//                 _healthSystem.OnDied -= HandleDeathExternally;
//             }
//         }

//         // Example method called by an external source (e.g., an enemy projectile hitting the player)
//         public void ApplyDamage(float amount)
//         {
//             if (_healthSystem != null && _healthSystem.IsAlive)
//             {
//                 _healthSystem.TakeDamage(amount);
//                 Debug.Log($"<color=red>Player Combat: Took {amount} damage. Current health: {_healthSystem.CurrentHealth}</color>");
//                 // Play hit sound, visual effect, etc., that is specific to THIS combat script.
//             }
//         }

//         // Example method called by an external source (e.g., picking up a health pack)
//         public void ApplyHealing(float amount)
//         {
//             if (_healthSystem != null && _healthSystem.IsAlive)
//             {
//                 _healthSystem.Heal(amount);
//                 Debug.Log($"<color=green>Player Combat: Healed {amount}. Current health: {_healthSystem.CurrentHealth}</color>");
//                 // Play heal sound, visual effect, etc.
//             }
//         }

//         // Example method for instantly killing the player (e.g., falling into a void)
//         public void InstantKill()
//         {
//             if (_healthSystem != null && _healthSystem.IsAlive)
//             {
//                 _healthSystem.Die();
//                 Debug.Log("<color=red>Player Combat: Instant kill triggered!</color>");
//             }
//         }

//         // Example of how this script itself might react to health changes (optional)
//         private void HandleHealthChangedExternally(float currentHealth, float maxHealth)
//         {
//             // For example, if health drops below a certain threshold, play a "low health" sound effect.
//             if (currentHealth / maxHealth < 0.25f && currentHealth > 0)
//             {
//                 Debug.Log("Player Combat: Health is critically low!");
//                 // Trigger a specific low-health animation or UI overlay.
//             }
//         }

//         private void HandleDeathExternally()
//         {
//             // For example, disable player input, trigger a unique player death animation,
//             // or notify a Game Manager about player defeat.
//             Debug.Log("<color=red>Player Combat: Player has died! Initiating player death sequence.</color>");
//             // GetComponent<PlayerInput>().Disable(); // Assuming you have a player input script
//             // Destroy(gameObject, 5f); // Destroy after a delay for death animation
//         }
//     }

// 2.  **A Script for Displaying UI (e.g., HealthBarUI.cs)**
//     This script would *subscribe* to events from the HealthSystem to update its UI.

//     public class HealthBarUI : MonoBehaviour
//     {
//         // IMPORTANT: Assign these in the Inspector!
//         [SerializeField] private HealthSystem _targetHealthSystem; // Drag the HealthSystem of the player/enemy here
//         [SerializeField] private UnityEngine.UI.Image _healthBarFill; // Drag your UI Image component (e.g., 'Fill' part of health bar)
//         [SerializeField] private UnityEngine.UI.Text _healthText;   // Drag your UI Text component for displaying numbers

//         void Start()
//         {
//             if (_targetHealthSystem == null)
//             {
//                 Debug.LogError("HealthBarUI: Target HealthSystem not assigned. Disabling UI.", this);
//                 this.enabled = false;
//                 return;
//             }
//             if (_healthBarFill == null)
//             {
//                 Debug.LogWarning("HealthBarUI: Health Bar Fill Image not assigned. UI will not update visually.", this);
//             }
//             if (_healthText == null)
//             {
//                 Debug.LogWarning("HealthBarUI: Health Text component not assigned. UI will not update numbers.", this);
//             }

//             // Subscribe to the health changed event.
//             // This method will be called every time _targetHealthSystem's health changes.
//             _targetHealthSystem.OnHealthChanged += UpdateHealthUI;
//             // Subscribe to the death event to react when the target dies.
//             _targetHealthSystem.OnDied += OnTargetDied;

//             // Initialize UI with the current health state immediately at start.
//             UpdateHealthUI(_targetHealthSystem.CurrentHealth, _targetHealthSystem.MaxHealth);
//         }

//         void OnDestroy()
//         {
//             // IMPORTANT: Unsubscribe from events when this UI element is destroyed
//             // to prevent NullReferenceExceptions and memory leaks.
//             if (_targetHealthSystem != null)
//             {
//                 _targetHealthSystem.OnHealthChanged -= UpdateHealthUI;
//                 _targetHealthSystem.OnDied -= OnTargetDied;
//             }
//         }

//         /// <summary>
//         /// This method is called automatically when the _targetHealthSystem's health changes.
//         /// </summary>
//         private void UpdateHealthUI(float currentHealth, float maxHealth)
//         {
//             if (_healthBarFill != null)
//             {
//                 _healthBarFill.fillAmount = currentHealth / maxHealth;
//             }
//             if (_healthText != null)
//             {
//                 _healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
//             }
//             Debug.Log($"<color=blue>HealthBarUI: Updating UI - Current: {currentHealth}, Max: {maxHealth}</color>");
//         }

//         /// <summary>
//         /// This method is called automatically when the _targetHealthSystem dies.
//         /// </summary>
//         private void OnTargetDied()
//         {
//             // For example, hide the health bar, show a 'dead' indicator, or play a specific UI animation.
//             Debug.Log($"<color=blue>HealthBarUI: Target {_targetHealthSystem.gameObject.name} has died. Hiding health bar.</color>");
//             if (_healthBarFill != null) _healthBarFill.gameObject.SetActive(false);
//             if (_healthText != null) _healthText.gameObject.SetActive(false);
//             // You might also want to disable this script if the target is permanently dead.
//             // this.enabled = false;
//         }
//     }

// --- HOW TO IMPLEMENT THE EXAMPLES IN YOUR UNITY PROJECT ---
//
// 1.  **Create the HealthSystem Script:**
//     -   In your Unity project, right-click in the Project window -> Create -> C# Script.
//     -   Name it "HealthSystem" (exactly as the class name).
//     -   Copy and paste the entire code block above into this new script. Save it.
//
// 2.  **Create a Test GameObject:**
//     -   In your Hierarchy window, right-click -> Create Empty.
//     -   Rename it to "Player" (or "Enemy", "DestructibleCrate", etc.).
//     -   Drag the "HealthSystem" script from your Project window onto the "Player" GameObject in the Hierarchy.
//         You'll now see its `Max Health` and `Current Health` properties in the Inspector.
//
// 3.  **(Optional) Implement PlayerCombat for interaction:**
//     -   Create another C# script named "PlayerCombat".
//     -   Copy the "PlayerCombat" example usage code (from the comments above) into this script. Save it.
//     -   Drag the "PlayerCombat" script onto your "Player" GameObject.
//     -   In the Inspector for "PlayerCombat" on your "Player" GameObject, drag the "HealthSystem" component (from the same GameObject) into the `_healthSystem` slot.
//
// 4.  **(Optional) Implement HealthBarUI for visual feedback:**
//     -   In your Hierarchy, right-click -> UI -> Canvas. This creates a Canvas and an EventSystem.
//     -   Right-click on the Canvas -> UI -> Image. Rename this to "HealthBarBackground".
//     -   Right-click on "HealthBarBackground" -> UI -> Image. Rename this to "HealthBarFill".
//         -   Select "HealthBarFill". In the Inspector, set its Image Type to "Filled", Fill Method to "Horizontal", and Fill Origin to "Left". Set its color (e.g., green).
//     -   Right-click on the Canvas -> UI -> Text - TextMeshPro (if you have TextMeshPro imported, otherwise regular Text). Rename this to "HealthText".
//         -   Adjust its position, font size, and color so it's visible over your health bar.
//     -   Create another C# script named "HealthBarUI".
//     -   Copy the "HealthBarUI" example usage code (from the comments above) into this script. Save it.
//     -   Create an empty GameObject in your Hierarchy (e.g., "UIManager" or just put it directly on the Canvas). Drag the "HealthBarUI" script onto this GameObject.
//     -   In the Inspector for "HealthBarUI":
//         -   Drag the "HealthSystem" component from your "Player" GameObject into the `_targetHealthSystem` slot.
//         -   Drag the "HealthBarFill" UI Image component into the `_healthBarFill` slot.
//         -   Drag the "HealthText" UI Text component into the `_healthText` slot.
//
// 5.  **Test in Play Mode:**
//     -   Run the scene.
//     -   In the Inspector for your "Player" GameObject, find the "PlayerCombat" component.
//     -   You can now call `ApplyDamage`, `ApplyHealing`, or `InstantKill` methods manually using the Inspector's script interaction during Play Mode (click the three dots on the component -> select the public method).
//     -   Observe the Console for logs from HealthSystem, PlayerCombat, and HealthBarUI.
//     -   Observe the Health Bar UI changing dynamically.
//     -   This demonstrates how different parts of your game (combat logic, UI) react to health changes through the HealthSystem's events without direct dependencies.
*/
```