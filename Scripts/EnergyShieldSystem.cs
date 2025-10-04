// Unity Design Pattern Example: EnergyShieldSystem
// This script demonstrates the EnergyShieldSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'EnergyShieldSystem' pattern, while not a formal GoF (Gang of Four) design pattern, represents a common and practical **architectural approach** or **system design pattern** for implementing energy-based defensive layers in game development. It effectively leverages several established design patterns to achieve a robust, flexible, and maintainable implementation.

This example provides a complete, working C# Unity script that demonstrates this pattern, ready to be dropped into a Unity project.

---

```csharp
// Unity Specific Usings
using UnityEngine;
using System; // For Action delegate
using System.Collections; // Not strictly needed for this version, but good to include for general Unity scripts
using System.Collections.Generic; // Not strictly needed

// --- Design Pattern Explanation: The 'EnergyShieldSystem' Architectural Pattern ---
/*
 * The 'EnergyShieldSystem' Design Pattern (Architectural Approach)
 *
 * In game development, an "Energy Shield System" typically refers to a common architectural
 * strategy for implementing a temporary, regenerating defensive layer on top of a primary health pool.
 * It's not a GoF pattern but a composite system built using well-known design patterns and principles.
 *
 * Core Concepts & Applied Design Patterns:
 *
 * 1.  Decorator Pattern (for Damage Handling):
 *     - **How it applies:** The EnergyShieldSystem component acts as a 'decorator' for an
 *       underlying HealthSystem (or any other `IDamageable` component) on the same GameObject.
 *     - **Mechanism:** When damage is inflicted upon the GameObject, the EnergyShieldSystem
 *       intercepts this damage first. It processes the damage by reducing its own energy.
 *       Only if the shield is completely depleted or the incoming damage exceeds the shield's
 *       remaining capacity, the *excess* damage is then forwarded to the decorated HealthSystem.
 *     - **Benefit:** This provides a clean separation of concerns, allowing damage to be
 *       handled by the shield without the core health system needing to know about shields.
 *       It makes the damage pipeline extensible.
 *
 * 2.  Observer Pattern (for UI, VFX, SFX, and Game Logic):
 *     - **How it applies:** The EnergyShieldSystem exposes various events (e.g., `OnShieldEnergyChanged`,
 *       `OnShieldDepleted`, `OnShieldActivated`, `OnShieldDamaged`).
 *     - **Mechanism:** Other components (like UI elements, visual effect managers, sound effect players,
 *       or game logic controllers) can "observe" (subscribe to) these events. When an event occurs,
 *       all subscribed observers are notified and can react accordingly.
 *     - **Benefit:** This promotes loose coupling. The shield system doesn't need to know about
 *       specific UI elements or VFX systems; it just broadcasts its state changes. Observers
 *       can be added or removed without modifying the core shield logic.
 *
 * 3.  State Management (Implicit or Explicit State Pattern):
 *     - **How it applies:** An energy shield naturally transitions through different states:
 *       - `Active`: Actively absorbing damage and possibly regenerating.
 *       - `Depleted`: No energy, unable to absorb damage, often undergoing a cooldown period.
 *       - `Recharging`: Gaining back energy after taking damage or being depleted.
 *     - **Mechanism:** In this implementation, a simpler, implicit state management is used via
 *       boolean flags (`_isShieldActive`) and timers (`_timeSinceLastDamage`, `_timeSinceDepletion`).
 *       For more complex shield behaviors (e.g., multiple regeneration modes, different damage types),
 *       an explicit State Pattern (with dedicated state classes like `ShieldActiveState`, `ShieldDepletedState`)
 *       could be used for greater flexibility.
 *     - **Benefit:** Organizes complex behavior into manageable states, making the system's
 *       logic easier to understand, extend, and debug.
 *
 * 4.  Interface Segregation Principle (IDamageable):
 *     - **How it applies:** A common interface (`IDamageable`) is defined for anything that can
 *       receive damage.
 *     - **Mechanism:** Both the HealthSystem and EnergyShieldSystem implement this interface. This
 *       allows damage-dealing entities to interact with defensive systems polymorphically, meaning
 *       a damage dealer doesn't need to know if it's hitting a shield, health, or some other defensive
 *       component directly. It just calls `target.TakeDamage(amount)`.
 *     - **Benefit:** Increases flexibility and reusability. Damage sources are decoupled from
 *       the specific implementation details of the target's defensive layers.
 *
 * Overall Benefits of this 'EnergyShieldSystem' Architectural Approach:
 * - Modularity: Shield logic is self-contained and operates independently of other systems unless explicitly interacting via interfaces or events.
 * - Reusability: The `IDamageable` interface and the shield system itself can be easily reused across different game entities.
 * - Extensibility: New shield types, regeneration mechanics, or UI/VFX feedback systems can be added with minimal changes to existing code.
 * - Maintainability: Changes or bug fixes to one part of the system (e.g., shield regen logic) are less likely to impact other parts (e.g., health mechanics or UI).
 *
 * Real-world Use Case:
 * This pattern is ideal for game entities such as player characters, enemy bosses, vehicles, or
 * structures that feature a temporary, regenerating defensive energy barrier distinct from their
 * core health.
 *
 * Example Scenario:
 * A player character has both a HealthSystem and an EnergyShieldSystem. When the player takes damage,
 * the EnergyShieldSystem absorbs the initial impact. If the shield's energy is depleted, any
 * subsequent or excess damage is then applied to the HealthSystem. The shield will automatically
 * regenerate its energy after a short delay, but only after a longer cooldown period if it was fully depleted.
 * UI elements update in real-time to visually represent the shield's and health's current status.
 */

// ====================================================================================================
// SECTION 1: COMMON INTERFACES AND UTILITIES
// ====================================================================================================

/// <summary>
/// Interface for any game object or component that can take damage.
/// This promotes loose coupling and allows damage sources to interact
/// with various defensive systems polymorphically.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the implementing component.
    /// </summary>
    /// <param name="amount">The amount of damage to inflict.</param>
    void TakeDamage(float amount);
    // Additional common methods or properties could be added here, e.g.:
    // float CurrentValue { get; }
    // float MaxValue { get; }
    // event Action<float> OnDamaged;
}

// ====================================================================================================
// SECTION 2: EXAMPLE CORE COMPONENTS (e.g., Health System, which the shield decorates)
// ====================================================================================================

/// <summary>
/// An example HealthSystem component that manages the health of a game object.
/// This component implements IDamageable and can be "decorated" by an EnergyShieldSystem.
/// </summary>
[DisallowMultipleComponent] // Ensures only one HealthSystem can be on a GameObject
public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Configuration")]
    [Tooltip("Maximum health capacity of the game object.")]
    [SerializeField] private float maxHealth = 100f;

    private float _currentHealth;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;

    // Events (Observer Pattern): Other systems (e.g., UI, Game Over Logic) can subscribe to these.
    public event Action<float> OnHealthChanged; // Notifies when health changes
    public event Action OnDied;                 // Notifies when health drops to 0 or below

    void Awake()
    {
        _currentHealth = maxHealth;
        OnHealthChanged?.Invoke(_currentHealth); // Initial notification for UI/observers
        Debug.Log($"{gameObject.name} HealthSystem initialized with {maxHealth} health.");
    }

    /// <summary>
    /// Reduces the current health by the specified amount.
    /// This is the primary method for taking damage.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // Ignore non-positive damage

        _currentHealth -= amount;
        OnHealthChanged?.Invoke(_currentHealth); // Notify observers of health change

        if (_currentHealth <= 0)
        {
            _currentHealth = 0; // Ensure health doesn't go negative
            OnDied?.Invoke();   // Notify observers of death
            Debug.Log($"<color=red>{gameObject.name} HealthSystem died!</color>");
            // Example: Destroy(gameObject); // Or trigger a death animation/respawn logic
        }
        else
        {
            Debug.Log($"{gameObject.name} HealthSystem took {amount:F1} damage, remaining health: {_currentHealth:F1}");
        }
    }

    /// <summary>
    /// Increases the current health by the specified amount, up to max health.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(float amount)
    {
        if (amount <= 0) return; // Ignore non-positive healing

        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth); // Cap health at maximum
        OnHealthChanged?.Invoke(_currentHealth); // Notify observers
        Debug.Log($"{gameObject.name} HealthSystem healed {amount:F1}, current health: {_currentHealth:F1}");
    }
}

// ====================================================================================================
// SECTION 3: THE ENERGY SHIELD SYSTEM (Core Implementation of the Pattern)
// ====================================================================================================

/// <summary>
/// Implements the 'EnergyShieldSystem' pattern. This component acts as a defensive layer
/// that intercepts damage before it potentially reaches an underlying HealthSystem.
/// It features configurable energy capacity, regeneration, cooldowns after damage/depletion,
/// and provides events for other systems to observe its state changes.
/// </summary>
[DisallowMultipleComponent] // Ensures only one shield system per GameObject
// [RequireComponent] ensures that a GameObject with EnergyShieldSystem also has a HealthSystem.
// This supports the Decorator pattern by guaranteeing an underlying component for damage forwarding.
[RequireComponent(typeof(HealthSystem))]
public class EnergyShieldSystem : MonoBehaviour, IDamageable
{
    // --- Shield Configuration ---
    [Header("Shield Configuration")]
    [Tooltip("Maximum energy capacity of the shield.")]
    [SerializeField] private float maxShieldEnergy = 100f;
    [Tooltip("Amount of shield energy regenerated per second when regenerating.")]
    [SerializeField] private float shieldRegenRate = 10f;
    [Tooltip("Time delay after taking damage before the shield starts regenerating.")]
    [SerializeField] private float shieldRegenDelay = 3f;
    [Tooltip("Time delay after the shield is fully depleted before it can reactivate and start regenerating.")]
    [SerializeField] private float depletedCooldownTime = 5f;

    // --- Current State Variables ---
    private float _currentShieldEnergy;
    public float CurrentShieldEnergy => _currentShieldEnergy;
    public float MaxShieldEnergy => maxShieldEnergy;

    private bool _isShieldActive = true; // True if shield is currently up and absorbing damage
    public bool IsShieldActive => _isShieldActive;

    private float _timeSinceLastDamage;   // Timer to track delay before regeneration can begin
    private float _timeSinceDepletion;    // Timer to track cooldown after shield completely breaks

    // --- References ---
    // Reference to the underlying HealthSystem component on the same GameObject.
    // This enables the EnergyShieldSystem to act as a 'Decorator' for damage handling.
    private HealthSystem _healthSystem;

    // --- Events (Observer Pattern) ---
    // Other components can subscribe to these events to react to shield state changes
    public event Action<float> OnShieldEnergyChanged; // Notifies when shield energy value changes
    public event Action OnShieldDepleted;            // Notifies when shield is fully depleted
    public event Action OnShieldActivated;           // Notifies when shield becomes active again
    public event Action<float> OnShieldDamaged;      // Notifies when shield absorbs damage

    void Awake()
    {
        _currentShieldEnergy = maxShieldEnergy; // Start with full shield energy

        // Initialize timers to allow immediate regeneration/activation if no damage/depletion occurs initially.
        _timeSinceLastDamage = shieldRegenDelay;
        _timeSinceDepletion = depletedCooldownTime;
        _isShieldActive = true; // Start with the shield active

        // Get reference to the HealthSystem. The [RequireComponent] attribute guarantees its presence.
        _healthSystem = GetComponent<HealthSystem>();
        if (_healthSystem == null)
        {
            // This error should ideally not be reached if RequireComponent works correctly.
            Debug.LogError($"EnergyShieldSystem on {gameObject.name} requires a HealthSystem component, but none was found.");
            enabled = false; // Disable this component if an essential dependency is missing
            return;
        }

        OnShieldEnergyChanged?.Invoke(_currentShieldEnergy); // Initial UI update/notification
        Debug.Log($"<color=white>{gameObject.name} EnergyShieldSystem initialized with {maxShieldEnergy} energy.</color>");
    }

    void Update()
    {
        // Increment timers each frame
        _timeSinceLastDamage += Time.deltaTime;
        _timeSinceDepletion += Time.deltaTime;

        // --- Shield Regeneration Logic ---
        // Regeneration only occurs if the shield is not at maximum energy.
        if (_currentShieldEnergy < maxShieldEnergy)
        {
            // If the shield is currently inactive (due to being depleted)
            if (!_isShieldActive)
            {
                // Check if the depleted cooldown period is still active
                if (_timeSinceDepletion < depletedCooldownTime)
                {
                    // Still in depleted cooldown, so cannot regenerate or reactivate yet.
                    return; // Exit Update early if in cooldown
                }
                else // Depleted cooldown is over, so the shield can now begin to reactivate/regenerate
                {
                    _isShieldActive = true; // Mark as active (even if energy is 0, it's ready to absorb)
                    OnShieldActivated?.Invoke(); // Notify observers (e.g., for activation VFX/SFX)
                    Debug.Log($"<color=green>{gameObject.name} Shield ACTIVATED after cooldown!</color>");
                }
            }

            // If the shield is active (or just reactivated) AND enough time has passed since last damage,
            // then regeneration can proceed.
            if (_isShieldActive && _timeSinceLastDamage >= shieldRegenDelay)
            {
                _currentShieldEnergy += shieldRegenRate * Time.deltaTime; // Add energy based on rate and time
                _currentShieldEnergy = Mathf.Min(_currentShieldEnergy, maxShieldEnergy); // Cap energy at max

                OnShieldEnergyChanged?.Invoke(_currentShieldEnergy); // Notify UI/observers of energy change
                // Debug.Log($"Shield regenerating. Current: {_currentShieldEnergy:F1}"); // Uncomment for detailed regen logs
            }
        }
    }

    /// <summary>
    /// Processes incoming damage. This method is the core of the Decorator pattern
    /// implementation for damage handling. The shield intercepts damage first.
    /// </summary>
    /// <param name="amount">The total amount of damage to inflict.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // Ignore non-positive damage

        _timeSinceLastDamage = 0f; // Reset the regeneration timer whenever any damage is taken

        float damageRemaining = amount;

        // --- Damage absorption by shield ---
        if (_isShieldActive)
        {
            // Calculate how much damage the shield can absorb from the incoming amount
            float damageAbsorbedByShield = Mathf.Min(damageRemaining, _currentShieldEnergy);

            _currentShieldEnergy -= damageAbsorbedByShield; // Reduce shield energy
            damageRemaining -= damageAbsorbedByShield;      // Reduce remaining damage to be applied

            OnShieldEnergyChanged?.Invoke(_currentShieldEnergy); // Notify UI/observers of energy change
            OnShieldDamaged?.Invoke(damageAbsorbedByShield);     // Notify for specific damage effects (e.g., hit VFX)

            Debug.Log($"<color=blue>{gameObject.name} Shield absorbed {damageAbsorbedByShield:F1} damage. " +
                      $"Remaining shield energy: {_currentShieldEnergy:F1}/{maxShieldEnergy:F1}</color>");

            // Check if the shield has been depleted after taking damage
            if (_currentShieldEnergy <= 0)
            {
                _currentShieldEnergy = 0; // Ensure energy doesn't go negative
                _isShieldActive = false;  // Mark shield as inactive/depleted
                _timeSinceDepletion = 0f; // Start the depleted cooldown timer
                OnShieldDepleted?.Invoke(); // Notify observers (e.g., for depletion VFX/SFX)
                Debug.LogWarning($"<color=red>{gameObject.name} Shield DEPLETED!</color>");
            }
        }

        // --- Forward remaining damage to HealthSystem ---
        // If there's still damage left (either because the shield was already down,
        // or it broke during this damage instance and couldn't absorb all of it),
        // pass the remainder to the underlying HealthSystem.
        if (damageRemaining > 0 && _healthSystem != null)
        {
            Debug.Log($"<color=orange>{gameObject.name} Shield broke or was down. Forwarding {damageRemaining:F1} damage to HealthSystem.</color>");
            _healthSystem.TakeDamage(damageRemaining); // HealthSystem handles the rest of the damage
        }
        else if (damageRemaining > 0 && _healthSystem == null)
        {
            // This case should ideally not happen due to [RequireComponent(typeof(HealthSystem))]
            Debug.LogWarning($"{gameObject.name} took {damageRemaining:F1} damage, but no HealthSystem " +
                             "was found to absorb it after shield interaction. Damage effectively lost.");
        }
    }

    /// <summary>
    /// Attempts to manually activate the shield, typically used for abilities or power-ups.
    /// Respects the depleted cooldown time.
    /// </summary>
    public void ManuallyActivateShield()
    {
        // Can only activate if not already active and depleted cooldown is over
        if (!_isShieldActive && _timeSinceDepletion >= depletedCooldownTime)
        {
            // Optionally, give it some minimum energy immediately on activation.
            // This ensures it has some buffer to absorb immediate damage.
            _currentShieldEnergy = Mathf.Max(_currentShieldEnergy, maxShieldEnergy * 0.1f);
            _isShieldActive = true;
            OnShieldActivated?.Invoke();
            OnShieldEnergyChanged?.Invoke(_currentShieldEnergy);
            Debug.Log($"<color=green>{gameObject.name} Shield manually activated!</color>");
        }
        else
        {
            Debug.Log($"{gameObject.name} Shield cannot be activated now " +
                      $"(already active or still on cooldown for {_depletedCooldownTime - _timeSinceDepletion:F1}s).");
        }
    }

    /// <summary>
    /// Manually deactivates the shield, typically for status effects, game mechanics, or player actions.
    /// </summary>
    public void ManuallyDeactivateShield()
    {
        if (_isShieldActive)
        {
            _isShieldActive = false;
            _timeSinceDepletion = 0f; // Start the cooldown period as if it was depleted
            OnShieldDepleted?.Invoke(); // Treat manual deactivation similar to depletion for observers
            Debug.Log($"<color=red>{gameObject.name} Shield manually deactivated!</color>");
        }
        else
        {
            Debug.Log($"{gameObject.name} Shield is already inactive.");
        }
    }

    // --- Editor-only Visualization (Gizmos for practicality and debugging) ---
    void OnDrawGizmos()
    {
        // Only draw gizmos in Play Mode to reflect runtime values accurately
        if (!Application.isPlaying) return;

        // Determine gizmo color based on shield state for quick visual feedback
        Color gizmoColor = Color.blue; // Default: Active and full
        if (!_isShieldActive)
        {
            gizmoColor = Color.red; // Shield is depleted/inactive
        }
        else if (_currentShieldEnergy < maxShieldEnergy)
        {
            gizmoColor = Color.cyan; // Shield is active but not full (damaged/regenerating)
        }

        Gizmos.color = gizmoColor;
        // Draw a simple wire sphere around the object to visually represent the shield
        Gizmos.DrawWireSphere(transform.position, 1.0f); // Adjust sphere size as needed

        // Display current shield status using Handles.Label for readability in the Scene view
        // UnityEditor.Handles is an editor-only API, so it must be enclosed in UNITY_EDITOR preprocessor directives.
        #if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white; // Text color for the label
        style.fontSize = 14;                  // Font size for the label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, // Position the label above the object
                                  $"Shield: {_currentShieldEnergy:F0}/{maxShieldEnergy:F0} ({(IsShieldActive ? "Active" : "Inactive")})", style);
        #endif
    }
}

// ====================================================================================================
// SECTION 4: EXAMPLE OBSERVER (UI Updater)
// ====================================================================================================

/// <summary>
/// An example UI component that observes the EnergyShieldSystem and HealthSystem
/// on the same GameObject to update UI elements (e.g., TextMeshPro text fields or UI sliders)
/// in real-time. This explicitly demonstrates the Observer Pattern in action.
///
/// NOTE: This script expects to be placed on the SAME GameObject as the EnergyShieldSystem
/// and HealthSystem it observes, due to the [RequireComponent] attributes and GetComponent calls.
/// If you prefer a global UI Manager, you would modify this to find the target systems
/// (e.g., using FindObjectOfType or a direct reference).
/// </summary>
[RequireComponent(typeof(EnergyShieldSystem))] // This UI needs a shield to observe
[RequireComponent(typeof(HealthSystem))]       // This UI needs health to observe
public class ShieldUIUpdater : MonoBehaviour
{
    [Header("UI References (Assign in Inspector)")]
    [Tooltip("TextMeshPro Text element to display current shield energy.")]
    [SerializeField] private TMPro.TextMeshProUGUI shieldText;
    [Tooltip("UI Slider element to visualize shield energy.")]
    [SerializeField] private UnityEngine.UI.Slider shieldSlider;
    [Tooltip("TextMeshPro Text element to display current health.")]
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [Tooltip("UI Slider element to visualize health.")]
    [SerializeField] private UnityEngine.UI.Slider healthSlider;

    private EnergyShieldSystem _shieldSystem;
    private HealthSystem _healthSystem;

    void Awake()
    {
        // Get references to the systems on the same GameObject (guaranteed by RequireComponent)
        _shieldSystem = GetComponent<EnergyShieldSystem>();
        _healthSystem = GetComponent<HealthSystem>();

        // Log warnings if UI elements are not assigned, but don't stop execution,
        // as some UI might still be functional.
        if (shieldText == null || shieldSlider == null || healthText == null || healthSlider == null)
        {
            Debug.LogWarning("ShieldUIUpdater: Not all UI references are set. Some UI elements may not update correctly.");
        }
    }

    void OnEnable()
    {
        // --- Subscribe to EnergyShieldSystem events ---
        if (_shieldSystem != null)
        {
            _shieldSystem.OnShieldEnergyChanged += UpdateShieldUI;
            _shieldSystem.OnShieldDepleted += OnShieldDepletedUI;
            _shieldSystem.OnShieldActivated += OnShieldActivatedUI;
            _shieldSystem.OnShieldDamaged += OnShieldDamagedUI;
            // Call once to set the initial UI state right after subscribing
            UpdateShieldUI(_shieldSystem.CurrentShieldEnergy);
        }

        // --- Subscribe to HealthSystem events ---
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged += UpdateHealthUI;
            _healthSystem.OnDied += OnDiedUI;
            // Call once to set the initial UI state
            UpdateHealthUI(_healthSystem.CurrentHealth);
        }
    }

    void OnDisable()
    {
        // --- Unsubscribe from events to prevent memory leaks and errors when the GameObject is disabled or destroyed ---
        if (_shieldSystem != null)
        {
            _shieldSystem.OnShieldEnergyChanged -= UpdateShieldUI;
            _shieldSystem.OnShieldDepleted -= OnShieldDepletedUI;
            _shieldSystem.OnShieldActivated -= OnShieldActivatedUI;
            _shieldSystem.OnShieldDamaged -= OnShieldDamagedUI;
        }
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
            _healthSystem.OnDied -= OnDiedUI;
        }
    }

    // --- Shield UI Update Methods (Event Handlers) ---
    private void UpdateShieldUI(float currentShieldEnergy)
    {
        if (shieldText != null)
        {
            shieldText.text = $"Shield: {currentShieldEnergy:F0}/{_shieldSystem.MaxShieldEnergy:F0}";
            // Dynamic text color based on shield state for better visual feedback
            if (!_shieldSystem.IsShieldActive)
                shieldText.color = Color.red; // Depleted
            else if (currentShieldEnergy < _shieldSystem.MaxShieldEnergy)
                shieldText.color = Color.yellow; // Damaged/Regenerating
            else
                shieldText.color = Color.white; // Full and active
        }
        if (shieldSlider != null)
        {
            shieldSlider.maxValue = _shieldSystem.MaxShieldEnergy;
            shieldSlider.value = currentShieldEnergy;
            // Optionally, change slider color based on state too
            if (shieldSlider.fillRect != null)
            {
                shieldSlider.fillRect.GetComponent<Image>().color = _shieldSystem.IsShieldActive ? Color.blue : Color.gray;
            }
        }
    }

    private void OnShieldDepletedUI()
    {
        Debug.Log("<color=red>UI: Shield Depleted! (Playing sound/VFX)</color>");
        // Example: Play a shield break sound, trigger a screen shake, or flash a UI element.
    }

    private void OnShieldActivatedUI()
    {
        Debug.Log("<color=green>UI: Shield Activated! (Playing sound/VFX)</color>");
        // Example: Play a shield activation sound, or a subtle UI animation.
    }

    private void OnShieldDamagedUI(float damageTaken)
    {
        // Example: Play a generic shield hit sound, briefly flash shield UI, or trigger a hit marker.
        Debug.Log($"UI: Shield took {damageTaken:F1} damage feedback.");
    }

    // --- Health UI Update Methods (Event Handlers) ---
    private void UpdateHealthUI(float currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth:F0}/{_healthSystem.MaxHealth:F0}";
            // Dynamic text color for health, e.g., red for low health
            if (currentHealth <= _healthSystem.MaxHealth * 0.2f && currentHealth > 0)
                healthText.color = Color.red; // Low health warning
            else if (currentHealth <= 0)
                healthText.color = Color.gray; // Died
            else
                healthText.color = Color.white; // Normal health
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = _healthSystem.MaxHealth;
            healthSlider.value = currentHealth;
            // Optionally, change slider color based on state
            if (healthSlider.fillRect != null)
            {
                healthSlider.fillRect.GetComponent<Image>().color = (currentHealth <= _healthSystem.MaxHealth * 0.2f && currentHealth > 0) ? Color.red : Color.green;
            }
        }
    }

    private void OnDiedUI()
    {
        Debug.Log("<color=red>UI: Player Died! (Displaying Game Over screen)</color>");
        // Example: Activate a "Game Over" screen, fade out, or respawn prompt.
    }
}

// ====================================================================================================
// SECTION 5: EXAMPLE DAMAGE DEALER (for Testing and Interaction Demonstration)
// ====================================================================================================

/// <summary>
/// A simple component to inflict damage on an IDamageable target for testing purposes.
/// This demonstrates how a separate system interacts with the shield/health polymorphically,
/// without needing to know the specific defensive components.
/// Also includes manual controls for shield activation/deactivation.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("The amount of damage to inflict with each automatic hit.")]
    [SerializeField] private float damageAmount = 15f;
    [Tooltip("The delay between automatic damage hits.")]
    [SerializeField] private float damageInterval = 1f;

    [Header("Target Configuration")]
    [Tooltip("The GameObject whose IDamageable component will receive damage.")]
    [SerializeField] private GameObject targetGameObject;

    private IDamageable _damageableTarget;
    private float _timeSinceLastDamage;

    void Start()
    {
        if (targetGameObject == null)
        {
            Debug.LogError("DamageDealer: Target GameObject is not set. Please assign it in the Inspector.");
            enabled = false; // Disable this component if no target
            return;
        }

        // Try to get an IDamageable component from the target GameObject.
        // This is key to the Interface Segregation Principle: the DamageDealer
        // doesn't care if it's hitting a HealthSystem, an EnergyShieldSystem,
        // or any other component that implements IDamageable.
        _damageableTarget = targetGameObject.GetComponent<IDamageable>();

        if (_damageableTarget == null)
        {
            Debug.LogError($"DamageDealer: Target GameObject '{targetGameObject.name}' does not have an IDamageable component.");
            enabled = false; // Disable if target cannot take damage
            return;
        }

        _timeSinceLastDamage = 0f; // Initialize timer
        Debug.Log("DamageDealer initialized. Auto-damaging target every " + damageInterval + "s. " +
                  "Press 'D' for manual damage, 'A' to activate shield, 'S' to deactivate shield.");
    }

    void Update()
    {
        // --- Automatic damage over time ---
        _timeSinceLastDamage += Time.deltaTime;
        if (_damageableTarget != null && _timeSinceLastDamage >= damageInterval)
        {
            _damageableTarget.TakeDamage(damageAmount); // Inflict damage polymorphically
            _timeSinceLastDamage = 0f; // Reset timer
        }

        // --- Manual damage on key press (for quick testing) ---
        if (Input.GetKeyDown(KeyCode.D) && _damageableTarget != null)
        {
            _damageableTarget.TakeDamage(damageAmount * 2); // Inflict double damage for a manual hit
            Debug.Log($"<color=purple>Manual Damage: {damageAmount * 2:F1} inflicted on {targetGameObject.name}</color>");
        }

        // --- Manual shield activation/deactivation (for quick testing of specific shield features) ---
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Directly get the EnergyShieldSystem component for specific shield control
            EnergyShieldSystem shield = targetGameObject.GetComponent<EnergyShieldSystem>();
            if (shield != null) shield.ManuallyActivateShield();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            // Directly get the EnergyShieldSystem component for specific shield control
            EnergyShieldSystem shield = targetGameObject.GetComponent<EnergyShieldSystem>();
            if (shield != null) shield.ManuallyDeactivateShield();
        }
    }
}

// ====================================================================================================
// SECTION 6: EXAMPLE USAGE IN UNITY EDITOR (Comments for Setup)
// ====================================================================================================

/*
 * HOW TO USE THIS COMPLETE EXAMPLE IN YOUR UNITY PROJECT:
 *
 * 1.  **Create a C# Script File:**
 *     - In your Unity Project window, right-click > Create > C# Script.
 *     - Name it `EnergyShieldSystemExample` (or any single name, as all classes are in one file).
 *     - Copy and paste the ENTIRE content of this code into the new script file, overwriting everything.
 *
 * 2.  **Import TextMeshPro Essentials (if you haven't already):**
 *     - Go to `Window > TextMeshPro > Import TMP Essential Resources`. This is required for the UI components.
 *
 * --- SETUP THE PLAYER/TARGET OBJECT ---
 * 3.  **Create the Target Entity:**
 *     - In your Unity scene Hierarchy, right-click > Create Empty.
 *     - Rename this new GameObject to "Player".
 *     - You can add a 3D object like a `Sphere` as a child to "Player" for visual representation.
 *
 * 4.  **Add Core Components to "Player":**
 *     - Select the "Player" GameObject.
 *     - In the Inspector, click "Add Component" and search for `HealthSystem`. Add it.
 *       - You can adjust `Max Health` (e.g., `200`).
 *     - Click "Add Component" again and search for `EnergyShieldSystem`. Add it.
 *       - Note how `[RequireComponent(typeof(HealthSystem))]` automatically ensures HealthSystem is present.
 *       - Adjust shield settings in the Inspector to experiment (e.g., `Max Shield Energy: 100`, `Shield Regen Rate: 10`, `Shield Regen Delay: 3s`, `Depleted Cooldown Time: 5s`).
 *
 * --- SETUP THE UI (Visual Feedback - Highly Recommended) ---
 * 5.  **Create a UI Canvas:**
 *     - In Hierarchy, right-click > UI > Canvas.
 *
 * 6.  **Create UI Elements for Shield:**
 *     - Right-click on `Canvas` > UI > Text - TextMeshPro. Rename it "ShieldText".
 *       - Position it (e.g., top-left, `Rect Transform` > `Anchor Presets` to top-left). Adjust font size.
 *     - Right-click on `Canvas` > UI > Slider. Rename it "ShieldSlider".
 *       - Position it below "ShieldText". For a simple fill bar, you might want to remove the "Handle Slide Area" child object from the Slider.
 *
 * 7.  **Create UI Elements for Health:**
 *     - Repeat step 6 to create "HealthText" (TextMeshPro Text) and "HealthSlider" (UI Slider).
 *       - Position these below the shield UI elements.
 *
 * 8.  **Add UI Updater Component to "Player" (Important Placement!):**
 *     - Select the "Player" GameObject.
 *     - Click "Add Component" and search for `ShieldUIUpdater`. Add it.
 *       - Due to `[RequireComponent]`, this script must reside on the same object as the shield/health.
 *     - In the Inspector for `ShieldUIUpdater`, drag the UI elements you created (ShieldText, ShieldSlider, HealthText, HealthSlider) from the Hierarchy into their respective fields.
 *
 * --- SETUP THE DAMAGE DEALER ---
 * 9.  **Create the Damage Source:**
 *     - In Hierarchy, right-click > Create Empty.
 *     - Rename this GameObject to "DamageSource".
 *
 * 10. **Add DamageDealer Component:**
 *     - Select the "DamageSource" GameObject.
 *     - Click "Add Component" and search for `DamageDealer`. Add it.
 *     - In the Inspector for `DamageDealer`, drag your "Player" GameObject into the `Target GameObject` field.
 *     - Adjust `Damage Amount` (e.g., `15`) and `Damage Interval` (e.g., `1s`) to control how damage is applied automatically.
 *
 * --- RUN THE SCENE ---
 * 11. **Press Play in the Unity Editor:**
 *     - Observe the debug logs in the Console and the UI updating in real-time.
 *     - The `DamageDealer` will automatically hit the "Player".
 *     - Watch how the `EnergyShieldSystem` absorbs damage first.
 *     - When the shield energy depletes, subsequent damage (or excess damage from the same hit) will go to the `HealthSystem`.
 *     - The shield will enter a `Depleted Cooldown` period before it can start regenerating again.
 *     - Experiment with manual controls:
 *       - Press `D` to inflict a burst of damage.
 *       - Press `A` to manually attempt to activate the shield (will only work if off cooldown).
 *       - Press `S` to manually deactivate the shield.
 *     - Observe the Gizmos in the Scene view for visual cues of the shield's state (blue for active/full, cyan for active/damaged, red for depleted/inactive).
 */
```