// Unity Design Pattern Example: SurvivalSystem
// This script demonstrates the SurvivalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements a 'SurvivalSystem' pattern in Unity by centralizing the management of various player survival stats (Health, Hunger, Thirst, Stamina) into a single controller. Each stat is a modular, extensible object that handles its own logic, decay/regeneration, and events.

This approach promotes:
1.  **Modularity:** Easily add or remove new survival stats (e.g., Temperature, Sanity) without altering the core system.
2.  **Encapsulation:** Each stat manages its own properties and behavior.
3.  **Observability:** Uses `UnityEvent`s to allow other systems (UI, VFX, audio) to react to stat changes without direct coupling.
4.  **Configurability:** All stats and their properties are exposed in the Unity Inspector for easy balancing by designers.

---

### Instructions to Use in Unity:

1.  **Create a C# Script:** Create a new C# script in your Unity project named `SurvivalSystemController`.
2.  **Copy and Paste:** Replace the entire content of the new `SurvivalSystemController.cs` script with the code provided below.
3.  **Create Player GameObject:** Create an empty GameObject in your scene, name it "Player" (or whatever your player entity is).
4.  **Attach Script:** Drag and drop the `SurvivalSystemController.cs` script onto your "Player" GameObject.
5.  **Configure in Inspector:**
    *   Select the "Player" GameObject.
    *   In the Inspector, you will see the `SurvivalSystemController` component.
    *   Expand each stat (Health, Hunger, Thirst, Stamina) and adjust their `MaxValue`, `DecayRate`, `MinCriticalValue`, `RegenerationRate`, etc., as desired.
    *   You can assign `UnityEvent` listeners directly in the inspector for `OnPlayerDied`, `OnPlayerCriticalState`, and individual stat `OnValueChanged` events (e.g., to update UI).
6.  **ExamplePlayerController (Optional but Recommended):**
    *   Create another C# script named `ExamplePlayerController.cs`.
    *   Copy the commented-out `ExamplePlayerController` code (at the bottom of the provided `SurvivalSystemController.cs`) into this new script.
    *   Attach `ExamplePlayerController.cs` to your "Player" GameObject.
    *   Create some UI elements (e.g., Sliders and Text for Health, Hunger, Thirst, Stamina) and a `GameObject` for a critical warning (like a red overlay).
    *   Drag these UI elements from your Hierarchy into the corresponding fields in the `ExamplePlayerController` component in the Inspector.
7.  **Run the Scene:** Play your scene. You will see debug logs for stat changes, and if you've set up the UI, you'll see it update dynamically. Use `F` (Food), `W` (Water), `T` (Take Damage), `H` (Heal), and `LeftShift` (Sprint/Use Stamina) to interact.

---

### `SurvivalSystemController.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent support

/// <summary>
/// This is the central controller for the 'SurvivalSystem' design pattern.
/// It manages a collection of various survival stats (Health, Hunger, Thirst, etc.)
/// and orchestrates their updates, interactions, and critical state handling.
/// </summary>
/// <remarks>
/// The 'SurvivalSystem' pattern here focuses on a modular and extensible approach
/// to managing player or entity survival metrics.
/// Key principles:
/// 1.  **Modular Stats:** Each survival aspect (Hunger, Thirst) is its own `SurvivalStat` class,
///     inheriting from a common base, allowing easy addition/removal of stats.
/// 2.  **Centralized Management:** The `SurvivalSystemController` updates all stats,
///     processes interactions, and acts as a single point of access for external systems.
/// 3.  **Event-Driven:** Uses UnityEvents for value changes and critical states, enabling
///     UI updates, sound effects, visual feedback, and other game logic to react easily
///     without direct polling.
/// 4.  **Configurability:** Stats are `[Serializable]` and exposed in the Inspector,
///     allowing designers to tune values without code changes.
/// </remarks>
public class SurvivalSystemController : MonoBehaviour
{
    [Header("Core Survival Stats")]
    // Health is usually the most critical stat, leading to death if depleted.
    [SerializeField] private HealthStat _health = new HealthStat();
    // Hunger decreases over time and might cause negative effects if critical/depleted.
    [SerializeField] private HungerStat _hunger = new HungerStat();
    // Thirst decreases over time, often faster than hunger, with similar negative effects.
    [SerializeField] private ThirstStat _thirst = new ThirstStat();
    // Stamina is often used for actions like sprinting or attacking and regenerates.
    [SerializeField] private StaminaStat _stamina = new StaminaStat();
    // You can add more stats here, e.g., TemperatureStat, SanityStat, etc.

    [Header("Survival System Events")]
    // Event fired when the player's health reaches zero.
    public UnityEvent OnPlayerDied;
    // Event fired when the player enters a critical survival state (e.g., starving, dehydrated).
    public UnityEvent OnPlayerCriticalState;
    // Event fired when the player exits a critical survival state.
    public UnityEvent OnPlayerExitedCriticalState;

    private List<SurvivalStat> _allStats;
    private bool _inCriticalState = false;

    // Public properties to access individual stats from other scripts.
    // This allows external systems to get current values or subscribe to specific stat events.
    public HealthStat Health => _health;
    public HungerStat Hunger => _hunger;
    public ThirstStat Thirst => _thirst;
    public StaminaStat Stamina => _stamina;

    private void Awake()
    {
        // Initialize the list of all survival stats.
        // This makes it easy to iterate and manage them uniformly.
        _allStats = new List<SurvivalStat>
        {
            _health,
            _hunger,
            _thirst,
            _stamina
        };

        // Initialize each stat. This is important for setting up initial values
        // and any internal state logic.
        foreach (var stat in _allStats)
        {
            stat.Initialize();
        }

        // Subscribe to individual stat events.
        // This controller can then react to specific stat events and potentially
        // trigger broader system events.
        _health.OnDepleted.AddListener(HandleHealthDepleted);
        _hunger.OnCriticalStateReached.AddListener(HandleCriticalStateEntered);
        _hunger.OnExitedCriticalState.AddListener(HandleCriticalStateExited);
        _thirst.OnCriticalStateReached.AddListener(HandleCriticalStateEntered);
        _thirst.OnExitedCriticalState.AddListener(HandleCriticalStateExited);
        // Add listeners for other stats as needed.
    }

    private void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks, especially important
        // for global events or when objects are destroyed and recreated.
        if (_health != null) _health.OnDepleted.RemoveListener(HandleHealthDepleted);
        if (_hunger != null)
        {
            _hunger.OnCriticalStateReached.RemoveListener(HandleCriticalStateEntered);
            _hunger.OnExitedCriticalState.RemoveListener(HandleCriticalStateExited);
        }
        if (_thirst != null)
        {
            _thirst.OnCriticalStateReached.RemoveListener(HandleCriticalStateEntered);
            _thirst.OnExitedCriticalState.RemoveListener(HandleCriticalStateExited);
        }
    }

    private void Update()
    {
        // Iterate through all managed stats and call their UpdateStat method.
        // This is where decay, regeneration, and other time-based stat changes occur.
        foreach (var stat in _allStats)
        {
            stat.UpdateStat(Time.deltaTime);
        }

        // Example: If Hunger or Thirst are critical, apply small health damage over time.
        if ((_hunger.IsCritical && _hunger.CurrentValue <= _hunger.MinValue) || 
            (_thirst.IsCritical && _thirst.CurrentValue <= _thirst.MinValue))
        {
            _health.ApplyChange(-0.5f * Time.deltaTime); // Small continuous damage for starvation/dehydration
        }
        else if (_hunger.IsCritical || _thirst.IsCritical)
        {
            _health.ApplyChange(-0.1f * Time.deltaTime); // Lesser continuous damage for just being critical
        }
    }

    /// <summary>
    /// Applies damage to the player's health.
    /// </summary>
    /// <param name="amount">The amount of damage to apply (positive value).</param>
    public void ApplyDamage(float amount)
    {
        if (amount < 0) amount = -amount; // Ensure positive damage
        _health.ApplyChange(-amount);
    }

    /// <summary>
    /// Heals the player's health.
    /// </summary>
    /// <param name="amount">The amount of health to restore (positive value).</param>
    public void ApplyHealing(float amount)
    {
        if (amount < 0) amount = -amount;
        _health.ApplyChange(amount);
    }

    /// <summary>
    /// Consumes a food item, increasing hunger.
    /// </summary>
    /// <param name="nutritionValue">The amount of hunger to restore.</param>
    public void ConsumeFood(float nutritionValue)
    {
        if (nutritionValue < 0) nutritionValue = -nutritionValue;
        _hunger.ApplyChange(nutritionValue);
    }

    /// <summary>
    /// Drinks water, increasing thirst.
    /// </summary>
    /// <param name="hydrationValue">The amount of thirst to restore.</param>
    public void DrinkWater(float hydrationValue)
    {
        if (hydrationValue < 0) hydrationValue = -hydrationValue;
        _thirst.ApplyChange(hydrationValue);
    }

    /// <summary>
    /// Uses stamina for an action (e.g., sprinting, attacking).
    /// </summary>
    /// <param name="cost">The amount of stamina to consume.</param>
    /// <returns>True if stamina was successfully used, false if not enough stamina.</returns>
    public bool UseStamina(float cost)
    {
        return _stamina.TryUseStamina(cost);
    }

    /// <summary>
    /// Handles the event when health is depleted. This usually means game over.
    /// </summary>
    private void HandleHealthDepleted()
    {
        Debug.Log("Player Health Depleted! Game Over.");
        OnPlayerDied.Invoke();
        // Here you would typically trigger game over logic, e.g.,
        // Time.timeScale = 0f;
        // UIManager.Instance.ShowGameOverScreen();
        // Disable player input, etc.
    }

    /// <summary>
    /// Handles a stat reaching its critical threshold.
    /// Checks if any stat is critical and updates the overall critical state.
    /// </summary>
    private void HandleCriticalStateEntered()
    {
        if (!_inCriticalState)
        {
            foreach (var stat in _allStats)
            {
                // Health critical is handled by OnPlayerDied; we look for other stats
                if (stat.IsCritical && stat != _health) 
                {
                    _inCriticalState = true;
                    Debug.Log("Player entered critical survival state!");
                    OnPlayerCriticalState.Invoke();
                    return; // Only need to invoke once for the system
                }
            }
        }
    }

    /// <summary>
    /// Handles a stat exiting its critical threshold.
    /// Checks if *no* stats are critical and updates the overall critical state.
    /// </summary>
    private void HandleCriticalStateExited()
    {
        if (_inCriticalState)
        {
            bool anyStatStillCritical = false;
            foreach (var stat in _allStats)
            {
                if (stat.IsCritical && stat != _health)
                {
                    anyStatStillCritical = true;
                    break;
                }
            }

            if (!anyStatStillCritical)
            {
                _inCriticalState = false;
                Debug.Log("Player exited critical survival state!");
                OnPlayerExitedCriticalState.Invoke();
            }
        }
    }
}

// ====================================================================================================
// Base Survival Stat Class
// ====================================================================================================

/// <summary>
/// Abstract base class for all survival statistics.
/// Provides common properties and methods for managing a stat's value, decay, and events.
/// Uses `[Serializable]` so it can be embedded directly in a MonoBehaviour in the Inspector.
/// </summary>
[Serializable]
public abstract class SurvivalStat
{
    [Tooltip("The display name of this stat (e.g., Health, Hunger).")]
    public string Name;

    [Tooltip("The maximum value this stat can reach.")]
    public float MaxValue = 100f;
    
    [Tooltip("The minimum value this stat can reach. Usually 0.")]
    public float MinValue = 0f;

    [Tooltip("The current value of this stat.")]
    // Use [SerializeField] on the private backing field to expose it in the Inspector
    // while keeping the property read-only from outside.
    [SerializeField]
    protected float _currentValue;

    [Tooltip("The rate at which this stat decays per second (e.g., Hunger decay). Set to 0 for no decay.")]
    public float DecayRate = 0f;

    [Tooltip("The value at or below which this stat is considered 'critical' (e.g., player is starving).")]
    public float MinCriticalValue = 20f;

    [Tooltip("Event invoked when the stat's current value changes. Passes the new current value.")]
    public UnityEvent<float> OnValueChanged;

    [Tooltip("Event invoked when the stat's value falls to or below MinCriticalValue.")]
    public UnityEvent OnCriticalStateReached;

    [Tooltip("Event invoked when the stat's value rises above MinCriticalValue after being critical.")]
    public UnityEvent OnExitedCriticalState;

    [Tooltip("Event invoked when the stat's value reaches MinValue (usually 0).")]
    public UnityEvent OnDepleted;

    protected bool _isCritical = false; // Internal flag to track critical state
    protected bool _isDepleted = false; // Internal flag to track depleted state

    // Public property to read the current value.
    public float CurrentValue
    {
        get => _currentValue;
        protected set
        {
            float previousValue = _currentValue;
            _currentValue = Mathf.Clamp(value, MinValue, MaxValue);

            if (_currentValue != previousValue)
            {
                // Invoke the value changed event with the new value.
                OnValueChanged?.Invoke(_currentValue);
                CheckStateChanges(previousValue);
            }
        }
    }

    // Public read-only property to check if the stat is currently in a critical state.
    public bool IsCritical => _isCritical;

    // Public read-only property to check if the stat is currently depleted.
    public bool IsDepleted => _isDepleted;

    /// <summary>
    /// Initializes the stat, setting its initial current value to MaxValue by default.
    /// This method should be called once, e.g., in `Awake` of the `SurvivalSystemController`.
    /// </summary>
    public virtual void Initialize()
    {
        // Ensure events are initialized if they haven't been in the inspector
        if (OnValueChanged == null) OnValueChanged = new UnityEvent<float>();
        if (OnCriticalStateReached == null) OnCriticalStateReached = new UnityEvent();
        if (OnExitedCriticalState == null) OnExitedCriticalState = new UnityEvent();
        if (OnDepleted == null) OnDepleted = new UnityEvent();

        CurrentValue = MaxValue; // Start at full
        _isCritical = false;
        _isDepleted = false;
        // Initial check to fire events if stat starts critical (unlikely but possible)
        CheckStateChanges(MaxValue + 1); // Pass a value guaranteed to be different
    }

    /// <summary>
    /// Applies a change to the current value of the stat.
    /// Positive amounts increase the stat, negative amounts decrease it.
    /// </summary>
    /// <param name="amount">The value to add or subtract from the current stat.</param>
    public virtual void ApplyChange(float amount)
    {
        CurrentValue += amount;
        // Debug.Log($"{Name} changed by {amount}. New value: {CurrentValue}"); // Too chatty for frequent updates
    }

    /// <summary>
    /// Abstract method to be implemented by concrete stat classes.
    /// This method is called every frame to update the stat's value based on time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    public abstract void UpdateStat(float deltaTime);

    /// <summary>
    /// Checks for changes in critical or depleted state and invokes relevant events.
    /// </summary>
    /// <param name="previousValue">The stat's value before the current change.</param>
    protected virtual void CheckStateChanges(float previousValue)
    {
        // Check for Depleted State
        bool nowDepleted = CurrentValue <= MinValue;
        if (nowDepleted && !_isDepleted)
        {
            _isDepleted = true;
            OnDepleted?.Invoke();
            Debug.Log($"{Name} depleted!");
        }
        else if (!nowDepleted && _isDepleted)
        {
            _isDepleted = false; // Recovered from depleted, though usually not possible for health
        }

        // Check for Critical State
        bool nowCritical = CurrentValue <= MinCriticalValue;
        if (nowCritical && !_isCritical)
        {
            _isCritical = true;
            OnCriticalStateReached?.Invoke();
            Debug.Log($"{Name} entered critical state!");
        }
        else if (!nowCritical && _isCritical)
        {
            _isCritical = false;
            OnExitedCriticalState?.Invoke();
            Debug.Log($"{Name} exited critical state!");
        }
    }
}

// ====================================================================================================
// Concrete Survival Stat Implementations
// ====================================================================================================

/// <summary>
/// Represents the Health stat.
/// Health typically doesn't decay naturally but can be damaged or healed.
/// </summary>
[Serializable]
public class HealthStat : SurvivalStat
{
    [Tooltip("Rate at which health regenerates per second. Set to 0 for no regeneration.")]
    public float RegenerationRate = 0f;

    public HealthStat()
    {
        Name = "Health";
        MaxValue = 100f;
        MinValue = 0f;
        DecayRate = 0f; // Health usually doesn't decay on its own.
        MinCriticalValue = 20f; // Low health warning threshold.
        RegenerationRate = 1f; // Example: small passive regen
    }

    public override void Initialize()
    {
        base.Initialize();
        // Health starts full by default from base Initialize
    }

    public override void UpdateStat(float deltaTime)
    {
        // Apply regeneration if not at max health and not depleted
        if (RegenerationRate > 0 && CurrentValue < MaxValue && !_isDepleted)
        {
            ApplyChange(RegenerationRate * deltaTime);
        }

        // No decay for health typically, but could be added here if needed
        // CurrentValue -= DecayRate * deltaTime;
    }
}

/// <summary>
/// Represents the Hunger stat.
/// Hunger decays over time and can be restored by consuming food.
/// </summary>
[Serializable]
public class HungerStat : SurvivalStat
{
    public HungerStat()
    {
        Name = "Hunger";
        MaxValue = 100f;
        MinValue = 0f;
        DecayRate = 1f; // Hunger decreases by 1 unit per second.
        MinCriticalValue = 20f; // Player starts feeling hungry.
    }

    public override void UpdateStat(float deltaTime)
    {
        // Hunger constantly decays
        ApplyChange(-DecayRate * deltaTime);
    }
}

/// <summary>
/// Represents the Thirst stat.
/// Thirst decays over time (often faster than hunger) and is restored by drinking.
/// </summary>
[Serializable]
public class ThirstStat : SurvivalStat
{
    public ThirstStat()
    {
        Name = "Thirst";
        MaxValue = 100f;
        MinValue = 0f;
        DecayRate = 2f; // Thirst decreases faster than hunger.
        MinCriticalValue = 20f; // Player starts feeling thirsty.
    }

    public override void UpdateStat(float deltaTime)
    {
        // Thirst constantly decays
        ApplyChange(-DecayRate * deltaTime);
    }
}

/// <summary>
/// Represents the Stamina stat.
/// Stamina is used for actions and usually regenerates quickly when not in use.
/// </summary>
[Serializable]
public class StaminaStat : SurvivalStat
{
    [Tooltip("Rate at which stamina regenerates per second when not being used.")]
    public float RegenerationRate = 5f;
    [Tooltip("Delay before stamina starts regenerating after use.")]
    public float RegenerationDelay = 1f;

    private float _timeSinceLastUse = 0f;
    private bool _isCurrentlyUsingStamina = false;

    public StaminaStat()
    {
        Name = "Stamina";
        MaxValue = 100f;
        MinValue = 0f;
        DecayRate = 0f; // Stamina doesn't decay like hunger; it's consumed.
        MinCriticalValue = 20f;
        RegenerationRate = 20f; // Fast regeneration for stamina
        RegenerationDelay = 1f;
    }

    public override void Initialize()
    {
        base.Initialize();
        _timeSinceLastUse = RegenerationDelay; // Start ready to regen
    }

    public override void UpdateStat(float deltaTime)
    {
        // Only regenerate if not currently using stamina and after the delay, and not at max
        if (!_isCurrentlyUsingStamina && CurrentValue < MaxValue)
        {
            _timeSinceLastUse += deltaTime;
            if (_timeSinceLastUse >= RegenerationDelay)
            {
                ApplyChange(RegenerationRate * deltaTime);
            }
        }
        else if (_isCurrentlyUsingStamina)
        {
            // If currently using, reset timer for next regen delay
            _timeSinceLastUse = 0f;
        }
        _isCurrentlyUsingStamina = false; // Reset for next frame
    }

    /// <summary>
    /// Attempts to use a specified amount of stamina.
    /// </summary>
    /// <param name="cost">The amount of stamina required for the action.</param>
    /// <returns>True if there was enough stamina and it was used, false otherwise.</returns>
    public bool TryUseStamina(float cost)
    {
        if (cost < 0) cost = -cost; // Ensure positive cost
        if (CurrentValue >= cost)
        {
            ApplyChange(-cost);
            _isCurrentlyUsingStamina = true; // Mark as currently using
            _timeSinceLastUse = 0f; // Reset delay timer
            return true;
        }
        return false;
    }
}

// ====================================================================================================
// Example Usage in other scripts (These are NOT part of the SurvivalSystem pattern itself,
// but demonstrate how to interact with it).
// ====================================================================================================


/*
// ExamplePlayerController.cs (Attach to your Player GameObject alongside SurvivalSystemController)
using UnityEngine;
using UnityEngine.UI; // For UI Text elements and Sliders
using System.Collections; // For Coroutines

public class ExamplePlayerController : MonoBehaviour
{
    [Header("Survival System")]
    [Tooltip("Reference to the SurvivalSystemController on this GameObject.")]
    [SerializeField]
    private SurvivalSystemController _survivalSystem;

    [Header("UI References (Optional)")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Text hungerText;
    [SerializeField] private Slider thirstSlider;
    [SerializeField] private Text thirstText;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Text staminaText;
    [SerializeField] private GameObject criticalWarningUI; // E.g., a red overlay, disable by default

    private void Awake()
    {
        // Get the SurvivalSystemController component from this GameObject.
        // It's good practice to ensure it's present.
        if (_survivalSystem == null)
        {
            _survivalSystem = GetComponent<SurvivalSystemController>();
            if (_survivalSystem == null)
            {
                Debug.LogError("SurvivalSystemController not found on this GameObject.", this);
                enabled = false; // Disable this script if controller is missing.
                return;
            }
        }

        // Subscribe to events for UI updates
        _survivalSystem.Health.OnValueChanged.AddListener(UpdateHealthUI);
        _survivalSystem.Hunger.OnValueChanged.AddListener(UpdateHungerUI);
        _survivalSystem.Thirst.OnValueChanged.AddListener(UpdateThirstUI);
        _survivalSystem.Stamina.OnValueChanged.AddListener(UpdateStaminaUI);

        // Subscribe to global survival system events
        _survivalSystem.OnPlayerDied.AddListener(HandlePlayerDied);
        _survivalSystem.OnPlayerCriticalState.AddListener(ShowCriticalWarning);
        _survivalSystem.OnPlayerExitedCriticalState.AddListener(HideCriticalWarning);

        // Initial UI update
        UpdateHealthUI(_survivalSystem.Health.CurrentValue);
        UpdateHungerUI(_survivalSystem.Hunger.CurrentValue);
        UpdateThirstUI(_survivalSystem.Thirst.CurrentValue);
        UpdateStaminaUI(_survivalSystem.Stamina.CurrentValue);
        criticalWarningUI?.SetActive(false); // Ensure warning is off initially
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks.
        if (_survivalSystem != null)
        {
            _survivalSystem.Health.OnValueChanged.RemoveListener(UpdateHealthUI);
            _survivalSystem.Hunger.OnValueChanged.RemoveListener(UpdateHungerUI);
            _survivalSystem.Thirst.OnValueChanged.RemoveListener(UpdateThirstUI);
            _survivalSystem.Stamina.OnValueChanged.RemoveListener(UpdateStaminaUI);

            _survivalSystem.OnPlayerDied.RemoveListener(HandlePlayerDied);
            _survivalSystem.OnPlayerCriticalState.RemoveListener(ShowCriticalWarning);
            _survivalSystem.OnPlayerExitedCriticalState.RemoveListener(HideCriticalWarning);
        }
    }

    private void Update()
    {
        // Example: Player input for interaction
        if (Input.GetKeyDown(KeyCode.F)) // F for Food
        {
            _survivalSystem.ConsumeFood(25f);
            Debug.Log("Ate some food!");
        }
        if (Input.GetKeyDown(KeyCode.W)) // W for Water
        {
            _survivalSystem.DrinkWater(40f);
            Debug.Log("Drank some water!");
        }
        if (Input.GetKeyDown(KeyCode.T)) // T for Take damage
        {
            _survivalSystem.ApplyDamage(10f);
            Debug.Log("Took 10 damage!");
        }
        if (Input.GetKeyDown(KeyCode.H)) // H for Heal
        {
            _survivalSystem.ApplyHealing(15f);
            Debug.Log("Healed 15 health!");
        }

        // Example: Using stamina for sprinting
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (_survivalSystem.UseStamina(15f * Time.deltaTime)) // Cost 15 stamina per second
            {
                // Player is sprinting
                // E.g., move faster here
                // Debug.Log("Sprinting..."); // Too chatty
            }
            else
            {
                // Not enough stamina to sprint
                // Debug.Log("Not enough stamina to sprint!"); // Too chatty
            }
        }
    }

    // --- UI Update Methods ---
    private void UpdateHealthUI(float value)
    {
        if (healthSlider != null) healthSlider.value = value / _survivalSystem.Health.MaxValue;
        if (healthText != null) healthText.text = $"Health: {Mathf.RoundToInt(value)} / {_survivalSystem.Health.MaxValue}";
    }

    private void UpdateHungerUI(float value)
    {
        if (hungerSlider != null) hungerSlider.value = value / _survivalSystem.Hunger.MaxValue;
        if (hungerText != null) hungerText.text = $"Hunger: {Mathf.RoundToInt(value)} / {_survivalSystem.Hunger.MaxValue}";
    }

    private void UpdateThirstUI(float value)
    {
        if (thirstSlider != null) thirstSlider.value = value / _survivalSystem.Thirst.MaxValue;
        if (thirstText != null) thirstText.text = $"Thirst: {Mathf.RoundToInt(value)} / {_survivalSystem.Thirst.MaxValue}";
    }

    private void UpdateStaminaUI(float value)
    {
        if (staminaSlider != null) staminaSlider.value = value / _survivalSystem.Stamina.MaxValue;
        if (staminaText != null) staminaText.text = $"Stamina: {Mathf.RoundToInt(value)} / {_survivalSystem.Stamina.MaxValue}";
    }

    // --- Global Survival Event Handlers ---
    private void HandlePlayerDied()
    {
        Debug.Log("GAME OVER! Player has died.");
        // Implement game over logic here (e.g., load game over scene, disable player controls)
    }

    private void ShowCriticalWarning()
    {
        criticalWarningUI?.SetActive(true);
        // Play an urgent sound, flash screen, etc.
        Debug.Log("Warning: Player is in a critical survival state!");
    }

    private void HideCriticalWarning()
    {
        criticalWarningUI?.SetActive(false);
        // Stop urgent sound, etc.
        Debug.Log("Player is no longer in a critical survival state.");
    }

    // Example: Interaction from an Item script (could be on a clickable object)
    // public class FoodItem : MonoBehaviour
    // {
    //     public float nutritionValue = 25f;
    //     void OnMouseDown() // Or using an Interaction system
    //     {
    //         SurvivalSystemController playerSurvivalSystem = FindObjectOfType<SurvivalSystemController>(); // Simpler for example
    //         if (playerSurvivalSystem != null)
    //         {
    //             playerSurvivalSystem.ConsumeFood(nutritionValue);
    //             Destroy(gameObject); // Consume the item
    //         }
    //     }
    // }

    // Example: Interaction from an Enemy script
    // public class EnemyAttack : MonoBehaviour
    // {
    //     public float attackDamage = 15f;
    //     void OnTriggerEnter(Collider other)
    //     {
    //         if (other.CompareTag("Player"))
    //         {
    //             SurvivalSystemController playerSurvival = other.GetComponent<SurvivalSystemController>();
    //             if (playerSurvival != null)
    //             {
    //                 playerSurvival.ApplyDamage(attackDamage);
    //                 Debug.Log($"Enemy attacked player for {attackDamage} damage!");
    //             }
    //         }
    //     }
    // }
}
*/