// Unity Design Pattern Example: AbilitySystem
// This script demonstrates the AbilitySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the 'AbilitySystem' design pattern. It provides a flexible and scalable way to manage character abilities, resources, and their interactions, suitable for various game genres (RPGs, MOBAs, FPS, etc.).

The system is broken down into the following core components:

1.  **`CharacterAttribute` & `AttributeComponent`**: Manages numerical stats like Health and Mana. Abilities interact with these.
2.  **`AbilityData` (ScriptableObject)**: Defines the *parameters* of an ability (e.g., name, cost, cooldown). This allows designers to create new abilities in the editor without coding.
3.  **`Ability` (Abstract Class)**: The base class for all ability *logic*. It handles common functionality like cooldowns, mana checks, and activation flow. Concrete abilities inherit from this.
4.  **`AbilitySystemComponent`**: The central manager on a character that holds and activates abilities. It grants abilities, requests activations, and manages their states.
5.  **Concrete Abilities (`FireballAbility`, `HealAbility`)**: Implement specific game actions by inheriting from `Ability` and using their respective `AbilityData`.
6.  **`PlayerAbilityController` (Example Usage)**: Demonstrates how a player or AI controller would interact with the `AbilitySystemComponent` to trigger abilities.

---

### 1. `AttributeComponent.cs`

This script defines individual character attributes and a component to manage them on a GameObject.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

// --- 1. Core Data Structures ---

/// <summary>
/// Represents a single character attribute like Health, Mana, Stamina.
/// It encapsulates the base, current, and max values, and provides events for changes.
/// </summary>
[System.Serializable]
public class CharacterAttribute
{
    [SerializeField] private float baseValue;
    [SerializeField] private float currentValue;
    [SerializeField] private float maxValue;

    public float BaseValue => baseValue;
    public float CurrentValue => currentValue;
    public float MaxValue => maxValue;

    // Events to notify other systems when values change
    public event Action<float, float> OnValueChanged; // Old value, New value
    public event Action<float, float> OnMaxValueChanged; // Old max, New max

    /// <summary>
    /// Constructor for a CharacterAttribute.
    /// </summary>
    /// <param name="baseVal">The initial base value.</param>
    /// <param name="maxVal">The initial maximum value.</param>
    public CharacterAttribute(float baseVal, float maxVal)
    {
        baseValue = baseVal;
        maxValue = maxVal;
        currentValue = baseVal; // Start with current value at base/max
    }

    /// <summary>
    /// Sets the base value of the attribute. Often, max value is tied to base.
    /// </summary>
    public void SetBaseValue(float newValue)
    {
        if (baseValue == newValue) return;
        baseValue = newValue;
        // In many games, base value dictates max value, so we update max here.
        SetMaxValue(newValue);
    }

    /// <summary>
    /// Sets the maximum value of the attribute.
    /// </summary>
    public void SetMaxValue(float newMax)
    {
        if (maxValue == newMax) return;
        float oldMax = maxValue;
        maxValue = newMax;
        OnMaxValueChanged?.Invoke(oldMax, maxValue);

        // Ensure current value doesn't exceed the new maximum
        if (currentValue > maxValue)
        {
            SetCurrentValue(maxValue);
        }
    }

    /// <summary>
    /// Sets the current value of the attribute, clamped between 0 and MaxValue.
    /// </summary>
    public void SetCurrentValue(float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, maxValue);
        if (currentValue == newValue) return;

        float oldValue = currentValue;
        currentValue = newValue;
        OnValueChanged?.Invoke(oldValue, currentValue); // Trigger event
    }

    /// <summary>
    /// Changes the current value by a specified amount (e.g., for damage or healing).
    /// </summary>
    public void ChangeCurrentValue(float amount)
    {
        SetCurrentValue(currentValue + amount);
    }

    /// <summary>
    /// Restores the current value to its maximum.
    /// </summary>
    public void RestoreToMax()
    {
        SetCurrentValue(maxValue);
    }
}

/// <summary>
/// Component responsible for managing a character's attributes (Health, Mana, etc.).
/// Other systems (like abilities) interact with these attributes to consume resources or apply effects.
/// </summary>
public class AttributeComponent : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private CharacterAttribute health = new CharacterAttribute(100, 100);
    [SerializeField] private CharacterAttribute mana = new CharacterAttribute(50, 50);

    // Public accessors for attributes
    public CharacterAttribute Health => health;
    public CharacterAttribute Mana => mana;

    private void Awake()
    {
        // Example: Subscribe to attribute changes for debugging or UI updates
        health.OnValueChanged += (oldVal, newVal) => Debug.Log($"[{gameObject.name}] Health: {oldVal:F1} -> {newVal:F1}");
        mana.OnValueChanged += (oldVal, newVal) => Debug.Log($"[{gameObject.name}] Mana: {oldVal:F1} -> {newVal:F1}");
    }

    /// <summary>
    /// Applies damage to the character's health attribute.
    /// </summary>
    /// <param name="amount">The amount of damage to apply (should be positive).</param>
    public void ApplyDamage(float amount)
    {
        if (amount < 0) amount = 0; // Ensure damage is never negative
        health.ChangeCurrentValue(-amount);
        if (health.CurrentValue <= 0)
        {
            Debug.Log($"[{gameObject.name}] has been defeated!");
            // TODO: Trigger death event or logic (e.g., disable controls, play animation)
        }
    }

    /// <summary>
    /// Restores health to the character's health attribute.
    /// </summary>
    /// <param name="amount">The amount of health to restore (should be positive).</param>
    public void RestoreHealth(float amount)
    {
        if (amount < 0) amount = 0; // Ensure healing is never negative
        health.ChangeCurrentValue(amount);
    }

    /// <summary>
    /// Consumes mana from the character's mana attribute.
    /// </summary>
    /// <param name="amount">The amount of mana to consume (should be positive).</param>
    /// <returns>True if mana was successfully consumed, false otherwise (e.g., not enough mana).</returns>
    public bool ConsumeMana(float amount)
    {
        if (amount < 0) amount = 0;
        if (mana.CurrentValue >= amount)
        {
            mana.ChangeCurrentValue(-amount);
            Debug.Log($"[{gameObject.name}] consumed {amount:F1} mana. Remaining: {mana.CurrentValue:F1}");
            return true;
        }
        Debug.LogWarning($"[{gameObject.name}] tried to consume {amount:F1} mana but only has {mana.CurrentValue:F1}. Not enough mana!");
        return false;
    }

    /// <summary>
    /// Restores mana to the character's mana attribute.
    /// </summary>
    /// <param name="amount">The amount of mana to restore (should be positive).</param>
    public void RestoreMana(float amount)
    {
        if (amount < 0) amount = 0;
        mana.ChangeCurrentValue(amount);
    }
}
```

---

### 2. `Ability.cs`

This file contains the abstract `Ability` class, which defines the core logic and lifecycle of any ability, and the abstract `AbilityData` ScriptableObject, which is used to create ability definitions in the Unity editor.

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// --- 2. Ability System Core ---

/// <summary>
/// Abstract base class for all abilities.
/// An ability defines the logic and state for a specific action (e.g., Fireball, Heal, Dash).
/// It handles common aspects like cooldowns, mana costs, and activation flow.
/// </summary>
public abstract class Ability
{
    protected AbilitySystemComponent ownerAbilitySystem; // The component that owns this ability
    protected AbilityData abilityData; // ScriptableObject containing static ability parameters

    private float _cooldownEndTime; // When the cooldown for this ability ends
    private bool _isActivating; // Is this ability currently in its activation phase?

    // Public properties to query ability state
    public bool IsOnCooldown => Time.time < _cooldownEndTime;
    public bool IsActivating => _isActivating;
    public float RemainingCooldown => Mathf.Max(0, _cooldownEndTime - Time.time);
    public AbilityData Data => abilityData; // Read-only access to the ability's static data

    /// <summary>
    /// Initializes the ability instance with its owner and static data.
    /// This is called once when the ability is granted to an AbilitySystemComponent.
    /// </summary>
    /// <param name="owner">The AbilitySystemComponent that owns this ability.</param>
    /// <param name="data">The ScriptableObject defining this ability's parameters.</param>
    public void Initialize(AbilitySystemComponent owner, AbilityData data)
    {
        ownerAbilitySystem = owner;
        abilityData = data;
    }

    /// <summary>
    /// Checks if the ability can be activated based on various conditions
    /// (e.g., mana cost, cooldown, target validity, etc.).
    /// This method is called BEFORE `Activate()`. Concrete abilities can override this
    /// to add their specific checks.
    /// </summary>
    /// <returns>True if the ability can be activated, false otherwise.</returns>
    public virtual bool CanActivate()
    {
        if (IsOnCooldown)
        {
            Debug.Log($"Ability '{abilityData.AbilityName}' is on cooldown. Remaining: {RemainingCooldown:F1}s");
            return false;
        }
        if (_isActivating)
        {
            Debug.Log($"Ability '{abilityData.AbilityName}' is already activating.");
            return false;
        }

        // Common resource checks for all abilities
        if (ownerAbilitySystem.AttributeComponent == null)
        {
            Debug.LogError($"Ability '{abilityData.AbilityName}' owner has no AttributeComponent!");
            return false;
        }
        if (ownerAbilitySystem.AttributeComponent.Mana.CurrentValue < abilityData.ManaCost)
        {
            Debug.Log($"Not enough mana for '{abilityData.AbilityName}'. Need {abilityData.ManaCost:F1}, have {ownerAbilitySystem.AttributeComponent.Mana.CurrentValue:F1}.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Starts the ability's execution. This method handles common tasks like
    /// consuming resources and starting cooldowns, then delegates to `OnActivate()`
    /// for specific ability logic.
    /// </summary>
    public void Activate()
    {
        // Double-check CanActivate() to prevent activation if conditions changed rapidly
        if (!CanActivate())
        {
            Debug.LogWarning($"Attempted to activate '{abilityData.AbilityName}' but CanActivate() returned false. This might indicate a timing issue.");
            return;
        }

        // Consume resources (mana, stamina, etc.)
        ownerAbilitySystem.AttributeComponent.ConsumeMana(abilityData.ManaCost);

        // Start the ability's cooldown
        StartCooldown();

        _isActivating = true; // Mark as active
        Debug.Log($"[{ownerAbilitySystem.gameObject.name}] Activating ability: {abilityData.AbilityName}");

        // Call the abstract method for concrete ability implementation
        OnActivate();

        // If the ability has a duration, the AbilitySystemComponent will track it.
        // If it's an instant ability (duration <= 0), it ends immediately.
        if (abilityData.Duration > 0)
        {
            ownerAbilitySystem.StartAbilityDuration(this, abilityData.Duration);
        }
        else
        {
            EndAbility(); // Instant abilities end immediately
        }
    }

    /// <summary>
    /// Abstract method that concrete abilities must implement to define their specific activation logic.
    /// This is where the actual game effect of the ability happens.
    /// </summary>
    protected abstract void OnActivate();

    /// <summary>
    /// Ends the ability's execution, performing any cleanup or final effects.
    /// This method should be called when the ability's effects are complete,
    /// or when a duration-based ability expires naturally.
    /// </summary>
    public virtual void EndAbility()
    {
        if (!_isActivating) return; // Only end if it was truly active
        _isActivating = false;
        Debug.Log($"[{ownerAbilitySystem.gameObject.name}] Ending ability: {abilityData.AbilityName}");
        OnEndAbility();
    }

    /// <summary>
    /// Virtual method for concrete ability implementations to add specific logic
    /// when the ability naturally ends (e.g., stop particle effects, remove buffs).
    /// </summary>
    protected virtual void OnEndAbility() { }

    /// <summary>
    /// Cancels an ongoing ability prematurely.
    /// This should revert any temporary effects or stop ongoing processes.
    /// </summary>
    public virtual void CancelAbility()
    {
        if (!_isActivating) return; // Only cancel if it was active
        _isActivating = false;
        Debug.Log($"[{ownerAbilitySystem.gameObject.name}] Cancelling ability: {abilityData.AbilityName}");
        OnCancelAbility();
    }

    /// <summary>
    /// Virtual method for concrete ability implementations to add specific logic
    /// when the ability is cancelled (e.g., refund mana, interrupt animation).
    /// </summary>
    protected virtual void OnCancelAbility() { }

    /// <summary>
    /// Starts the cooldown timer for this ability.
    /// </summary>
    protected void StartCooldown()
    {
        _cooldownEndTime = Time.time + abilityData.CooldownTime;
        Debug.Log($"Ability '{abilityData.AbilityName}' starting cooldown for {abilityData.CooldownTime:F1} seconds. Ready at: {(_cooldownEndTime):F1}");
    }
}


/// <summary>
/// ScriptableObject base class for defining ability parameters in the editor.
/// This allows designers to create new ability types and configure their values
/// without modifying code. Each concrete ability will have a corresponding `AbilityData` subclass.
/// </summary>
public abstract class AbilityData : ScriptableObject
{
    [Header("Base Ability Data")]
    public string AbilityName = "New Ability";
    public Sprite Icon; // Icon to display in UI
    public float CooldownTime = 1.0f; // Time before ability can be used again
    public float ManaCost = 10.0f; // Resource cost to activate
    public float Duration = 0.0f; // 0 for instant, > 0 for channeled/timed abilities

    /// <summary>
    /// Creates an instance of the concrete `Ability` class associated with this `AbilityData`.
    /// This is the crucial link between the editor-defined data and the runtime logic.
    /// </summary>
    /// <returns>A new instance of the concrete Ability.</returns>
    public abstract Ability CreateAbility();
}
```

---

### 3. `AbilitySystemComponent.cs`

This component is attached to any GameObject (e.g., player, enemy) that can use abilities. It acts as the central hub for managing granted abilities.

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The core component that manages all abilities for a character.
/// It holds granted abilities, handles activation requests, and manages durations.
/// This component requires an AttributeComponent to manage resources like Health and Mana.
/// </summary>
[RequireComponent(typeof(AttributeComponent))]
public class AbilitySystemComponent : MonoBehaviour
{
    // Public accessor for the AttributeComponent on the same GameObject
    public AttributeComponent AttributeComponent { get; private set; }

    [Header("Initial Abilities")]
    [SerializeField]
    private List<AbilityData> initialAbilityData = new List<AbilityData>();

    // Internal lists/dictionaries to manage abilities
    private List<Ability> _grantedAbilities = new List<Ability>();
    private Dictionary<string, Ability> _abilityMap = new Dictionary<string, Ability>(); // For quick lookup by name
    private List<Ability> _activeDurationAbilities = new List<Ability>(); // Abilities that are currently running a duration coroutine

    private void Awake()
    {
        // Get reference to the required AttributeComponent
        AttributeComponent = GetComponent<AttributeComponent>();
        if (AttributeComponent == null)
        {
            Debug.LogError("AbilitySystemComponent requires an AttributeComponent!", this);
            enabled = false; // Disable if requirements are not met
            return;
        }

        // Grant any abilities defined in the editor on start
        foreach (var data in initialAbilityData)
        {
            if (data != null)
            {
                GrantAbility(data);
            }
        }
    }

    /// <summary>
    /// Grants a new ability to this AbilitySystemComponent.
    /// It creates a runtime instance of the Ability logic from the provided AbilityData.
    /// </summary>
    /// <param name="data">The ScriptableObject containing the ability's parameters.</param>
    /// <returns>The newly granted Ability instance, or null if granting failed.</returns>
    public Ability GrantAbility(AbilityData data)
    {
        if (data == null)
        {
            Debug.LogError($"[{gameObject.name}] Attempted to grant a null AbilityData.", this);
            return null;
        }

        // Prevent granting the same ability by name twice (or implement stacking logic if needed)
        if (_abilityMap.ContainsKey(data.AbilityName))
        {
            Debug.LogWarning($"Ability '{data.AbilityName}' already granted to {gameObject.name}. Skipping.", this);
            return _abilityMap[data.AbilityName];
        }

        // Create the runtime Ability instance from its ScriptableObject data
        Ability newAbility = data.CreateAbility();
        newAbility.Initialize(this, data); // Initialize with owner and data

        _grantedAbilities.Add(newAbility);
        _abilityMap.Add(data.AbilityName, newAbility); // Store for easy lookup

        Debug.Log($"[{gameObject.name}] Granted ability '{data.AbilityName}'.");
        return newAbility;
    }

    /// <summary>
    /// Attempts to activate an ability by its registered name.
    /// </summary>
    /// <param name="abilityName">The name of the ability to activate.</param>
    /// <returns>True if the ability was successfully activated, false otherwise.</returns>
    public bool TryActivateAbility(string abilityName)
    {
        if (_abilityMap.TryGetValue(abilityName, out Ability ability))
        {
            return TryActivateAbility(ability);
        }
        Debug.LogWarning($"[{gameObject.name}] Ability '{abilityName}' not found on this component.");
        return false;
    }

    /// <summary>
    /// Attempts to activate a specific Ability instance.
    /// This method first checks `CanActivate()` and then calls `Activate()` if conditions are met.
    /// </summary>
    /// <param name="ability">The Ability instance to activate.</param>
    /// <returns>True if the ability was successfully activated, false otherwise.</returns>
    public bool TryActivateAbility(Ability ability)
    {
        if (ability == null)
        {
            Debug.LogError($"[{gameObject.name}] Attempted to activate a null ability.", this);
            return false;
        }

        if (ability.CanActivate())
        {
            ability.Activate();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a granted ability by its name. Useful for querying status or UI.
    /// </summary>
    /// <param name="abilityName">The name of the ability to retrieve.</param>
    /// <returns>The Ability instance if found, null otherwise.</returns>
    public Ability GetAbility(string abilityName)
    {
        _abilityMap.TryGetValue(abilityName, out Ability ability);
        return ability;
    }

    /// <summary>
    /// Removes a granted ability from this AbilitySystemComponent.
    /// If the ability is currently active, it will be cancelled.
    /// </summary>
    /// <param name="abilityName">The name of the ability to remove.</param>
    public void RemoveAbility(string abilityName)
    {
        if (_abilityMap.TryGetValue(abilityName, out Ability abilityToRemove))
        {
            if (abilityToRemove.IsActivating)
            {
                abilityToRemove.CancelAbility(); // Cancel if active before removing
            }
            _grantedAbilities.Remove(abilityToRemove);
            _abilityMap.Remove(abilityName);
            Debug.Log($"[{gameObject.name}] Removed ability '{abilityName}'.");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Attempted to remove non-existent ability '{abilityName}'.");
        }
    }

    /// <summary>
    /// Starts a coroutine to track the duration of an ability.
    /// This allows for abilities with ongoing effects or delayed completion.
    /// </summary>
    /// <param name="ability">The ability whose duration needs to be tracked.</param>
    /// <param name="duration">The duration of the ability in seconds.</param>
    public void StartAbilityDuration(Ability ability, float duration)
    {
        if (duration <= 0) // Should not be called for instant abilities
        {
            ability.EndAbility();
            return;
        }

        if (!_activeDurationAbilities.Contains(ability))
        {
            _activeDurationAbilities.Add(ability);
            StartCoroutine(AbilityDurationCoroutine(ability, duration));
        }
    }

    /// <summary>
    /// Coroutine that waits for an ability's duration to complete, then calls `EndAbility()`.
    /// </summary>
    private IEnumerator AbilityDurationCoroutine(Ability ability, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Ensure the ability is still active and hasn't been cancelled prematurely
        if (ability.IsActivating)
        {
            ability.EndAbility();
        }
        _activeDurationAbilities.Remove(ability); // Remove from active list
    }

    // You could add an Update method here if you needed to tick abilities
    // or manage other global ability state, but for this basic duration
    // tracking, coroutines are sufficient.
}
```

---

### 4. Concrete Ability Implementations (`FireballAbilityData`, `FireballAbility`, `HealAbilityData`, `HealAbility`)

These files define specific abilities for our example: a Fireball that deals damage and a Heal that restores health. Each pair (Data + Ability) represents one distinct ability.

#### `FireballAbilityData.cs`

```csharp
using UnityEngine;

/// <summary>
/// ScriptableObject for Fireball ability specific data.
/// Designers configure Fireball parameters here.
/// </summary>
[CreateAssetMenu(fileName = "FireballAbilityData", menuName = "AbilitySystem/Ability Data/Fireball")]
public class FireballAbilityData : AbilityData
{
    [Header("Fireball Specific Data")]
    public float DamageAmount = 25f; // How much damage the fireball deals
    public GameObject ProjectilePrefab; // Visual prefab for the fireball projectile
    public float ProjectileSpeed = 15f; // Speed of the projectile
    public float Range = 20f; // Max range for targeting (not used in this simple demo but useful)

    /// <summary>
    /// Tells the system to create an instance of FireballAbility when this data is granted.
    /// </summary>
    public override Ability CreateAbility()
    {
        return new FireballAbility();
    }
}
```

#### `FireballAbility.cs`

```csharp
using UnityEngine;

/// <summary>
/// Concrete implementation of a Fireball ability.
/// Inherits from the base Ability class and defines how a fireball behaves.
/// </summary>
public class FireballAbility : Ability
{
    // Cast the generic AbilityData to the specific FireballAbilityData for easy access to its properties
    private FireballAbilityData FireballData => abilityData as FireballAbilityData;

    /// <summary>
    /// Overrides the base CanActivate to add any Fireball-specific checks.
    /// (e.g., checking for a valid target in range, line of sight, etc.)
    /// </summary>
    public override bool CanActivate()
    {
        if (!base.CanActivate()) return false; // Always call base check first

        // Add Fireball specific conditions here, e.g.:
        // if (target == null || !IsTargetInRange(target, FireballData.Range)) return false;
        // For this simple example, we assume it can always be cast if mana/cooldown allows.
        return true;
    }

    /// <summary>
    /// Defines the actual effects of the Fireball ability.
    /// </summary>
    protected override void OnActivate()
    {
        Debug.Log($"[{ownerAbilitySystem.gameObject.name}] casts Fireball! Deals {FireballData.DamageAmount:F1} damage.");

        // Example: Instantiate a visual projectile
        if (FireballData.ProjectilePrefab != null)
        {
            // Position the projectile slightly in front of the caster
            GameObject projectile = GameObject.Instantiate(
                FireballData.ProjectilePrefab,
                ownerAbilitySystem.transform.position + ownerAbilitySystem.transform.forward * 0.8f + Vector3.up * 0.5f, // Offset slightly
                ownerAbilitySystem.transform.rotation);

            // Add simple forward movement. In a real game, this projectile would
            // have its own script (e.g., SimpleProjectile) to handle collision, damage, and destruction.
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = ownerAbilitySystem.transform.forward * FireballData.ProjectileSpeed;
            }
            else
            {
                // If no rigidbody, just make it disappear after some time
                MonoBehaviour.Destroy(projectile, 3f);
            }
        }
        else
        {
            Debug.LogWarning($"FireballAbilityData for '{abilityData.AbilityName}' is missing Projectile Prefab!");
        }

        // In a real game, damage would be applied when the projectile hits.
        // For this demo, we're just logging. If you want instant damage,
        // you'd perform a raycast or target lookup here and apply damage directly.
        // Example: ApplyDamageToTarget(FireballData.DamageAmount);
    }

    /// <summary>
    /// Cleanup or final effects when the Fireball ability naturally ends.
    /// (Usually instant, so this might not have much specific logic).
    /// </summary>
    protected override void OnEndAbility()
    {
        Debug.Log($"Fireball ability effects complete.");
    }

    /// <summary>
    /// Logic for when the Fireball ability is cancelled prematurely.
    /// (Usually instant, so cancellation is less common unless it's a channeled spell).
    /// </summary>
    protected override void OnCancelAbility()
    {
        Debug.Log($"Fireball ability was cancelled.");
    }
}
```

#### `HealAbilityData.cs`

```csharp
using UnityEngine;

/// <summary>
/// ScriptableObject for Heal ability specific data.
/// Designers configure Heal parameters here.
/// </summary>
[CreateAssetMenu(fileName = "HealAbilityData", menuName = "AbilitySystem/Ability Data/Heal")]
public class HealAbilityData : AbilityData
{
    [Header("Heal Specific Data")]
    public float HealAmount = 30f; // How much health to restore

    /// <summary>
    /// Tells the system to create an instance of HealAbility when this data is granted.
    /// </summary>
    public override Ability CreateAbility()
    {
        return new HealAbility();
    }
}
```

#### `HealAbility.cs`

```csharp
using UnityEngine;

/// <summary>
/// Concrete implementation of a Heal ability.
/// Restores health to the caster.
/// </summary>
public class HealAbility : Ability
{
    // Cast the generic AbilityData to the specific HealAbilityData
    private HealAbilityData HealData => abilityData as HealAbilityData;

    /// <summary>
    /// Overrides the base CanActivate to add Heal-specific conditions.
    /// </summary>
    public override bool CanActivate()
    {
        if (!base.CanActivate()) return false; // Always call base check first

        // Additional check: Can only heal if not at max health.
        if (ownerAbilitySystem.AttributeComponent.Health.CurrentValue >= ownerAbilitySystem.AttributeComponent.Health.MaxValue)
        {
            Debug.Log($"[{ownerAbilitySystem.gameObject.name}] is already at max health. Cannot heal.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Defines the actual effects of the Heal ability.
    /// </summary>
    protected override void OnActivate()
    {
        Debug.Log($"[{ownerAbilitySystem.gameObject.name}] casts Heal! Restores {HealData.HealAmount:F1} health.");
        ownerAbilitySystem.AttributeComponent.RestoreHealth(HealData.HealAmount);

        // Example: Play a particle effect or sound at the caster's position
        // Instantiate(HealData.ParticleEffectPrefab, ownerAbilitySystem.transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Cleanup or final effects when the Heal ability naturally ends.
    /// </summary>
    protected override void OnEndAbility()
    {
        Debug.Log($"Heal ability effects complete.");
    }

    /// <summary>
    /// Logic for when the Heal ability is cancelled prematurely.
    /// (Usually instant, so cancellation is less common).
    /// </summary>
    protected override void OnCancelAbility()
    {
        Debug.Log($"Heal ability was cancelled.");
    }
}
```

---

### 5. Example Usage (`PlayerAbilityController.cs`)

This script shows how a player's input could trigger abilities through the `AbilitySystemComponent`.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example Player Controller demonstrating how to interact with the AbilitySystem.
/// This script listens for input and requests the AbilitySystemComponent to activate abilities.
/// </summary>
public class PlayerAbilityController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AbilitySystemComponent abilitySystem;

    [Header("Setup Initial Abilities (Optional)")]
    // This list can be used to grant additional abilities specifically through this controller,
    // complementing or overriding abilities set directly on the AbilitySystemComponent.
    [SerializeField] private List<AbilityData> initialAbilitiesForController = new List<AbilityData>();

    private void Awake()
    {
        // Automatically find AbilitySystemComponent if not assigned in inspector
        if (abilitySystem == null)
        {
            abilitySystem = GetComponent<AbilitySystemComponent>();
            if (abilitySystem == null)
            {
                Debug.LogError("PlayerAbilityController requires an AbilitySystemComponent on the same GameObject or assigned in inspector.", this);
                enabled = false; // Disable controller if ASC is missing
            }
        }
    }

    private void Start()
    {
        // Grant abilities specified in this controller's list, if any.
        foreach (var abilityData in initialAbabilitiesForController)
        {
            abilitySystem.GrantAbility(abilityData);
        }

        // Example: Manually setting initial attribute values (e.g., for a new game start)
        // These will override the default values set in AttributeComponent if called after Awake.
        abilitySystem.AttributeComponent.Health.SetCurrentValue(80);
        abilitySystem.AttributeComponent.Mana.SetCurrentValue(30);
    }

    private void Update()
    {
        HandleAbilityInput();
        HandleDebugInput();
        DisplayDebugStats();
    }

    private void HandleAbilityInput()
    {
        // Example input for activating abilities by their AbilityName
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Player pressed 1: Trying to cast Fireball...");
            abilitySystem.TryActivateAbility("Fireball"); // Matches AbilityName in FireballAbilityData
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Player pressed 2: Trying to cast Heal...");
            abilitySystem.TryActivateAbility("Heal"); // Matches AbilityName in HealAbilityData
        }
    }

    private void HandleDebugInput()
    {
        // Debug input for attributes
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Player pressed Space: Taking 15 damage...");
            abilitySystem.AttributeComponent.ApplyDamage(15);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Player pressed R: Restoring 10 mana...");
            abilitySystem.AttributeComponent.RestoreMana(10);
        }
    }

    private void DisplayDebugStats()
    {
        // In a real game, this information would be displayed on a UI canvas.
        // Logging every frame is too spammy, but useful for quick debugging in development.
        if (abilitySystem != null && abilitySystem.AttributeComponent != null)
        {
            string debugText = $"Health: {abilitySystem.AttributeComponent.Health.CurrentValue:F1}/{abilitySystem.AttributeComponent.Health.MaxValue:F1} | " +
                               $"Mana: {abilitySystem.AttributeComponent.Mana.CurrentValue:F1}/{abilitySystem.AttributeComponent.Mana.MaxValue:F1}";

            // Get specific abilities to display their cooldowns
            Ability fireballAbility = abilitySystem.GetAbility("Fireball");
            if (fireballAbility != null)
            {
                debugText += $" | Fireball CD: {(fireballAbility.IsOnCooldown ? fireballAbility.RemainingCooldown.ToString("F1") + "s" : "Ready")}";
            }

            Ability healAbility = abilitySystem.GetAbility("Heal");
            if (healAbility != null)
            {
                debugText += $" | Heal CD: {(healAbility.IsOnCooldown ? healAbility.RemainingCooldown.ToString("F1") + "s" : "Ready")}";
            }
            // Uncomment the line below if you want constant debug output in console (can be very spammy!)
            // Debug.Log(debugText);
        }
    }
}
```

---

### Optional: `SimpleProjectile.cs`

This script is a simple example for a visual projectile that the `FireballAbility` can instantiate. If you want visual fireballs, create a new script named `SimpleProjectile.cs` and attach it to your projectile prefab.

```csharp
using UnityEngine;

/// <summary>
/// Simple script for a projectile that moves forward and applies damage on collision.
/// Designed to be attached to a GameObject with a Rigidbody and Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : MonoBehaviour
{
    public float lifetime = 3f; // How long the projectile exists before self-destructing
    public float damage = 20f; // Damage amount (can be overridden by ability if desired)

    void Start()
    {
        // Automatically destroy the projectile after its lifetime
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Attempt to get an AttributeComponent from the collided object
        AttributeComponent targetAttributeComponent = collision.gameObject.GetComponent<AttributeComponent>();
        if (targetAttributeComponent != null)
        {
            // Apply damage to the collided object
            targetAttributeComponent.ApplyDamage(damage);
            Debug.Log($"{gameObject.name} hit {collision.gameObject.name} for {damage:F1} damage!");
        }

        // Destroy the projectile on impact, regardless of whether it hit a valid target
        Destroy(gameObject);
    }
}
```

---

### Unity Editor Setup Instructions:

To get this example working in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `AttributeComponent.cs` and paste the `CharacterAttribute` and `AttributeComponent` code into it.
    *   Create a new C# script named `Ability.cs` and paste the `Ability` and `AbilityData` abstract classes into it.
    *   Create a new C# script named `AbilitySystemComponent.cs` and paste the `AbilitySystemComponent` class into it.
    *   Create a new C# script named `FireballAbilityData.cs` and paste the `FireballAbilityData` class into it.
    *   Create a new C# script named `FireballAbility.cs` and paste the `FireballAbility` class into it.
    *   Create a new C# script named `HealAbilityData.cs` and paste the `HealAbilityData` class into it.
    *   Create a new C# script named `HealAbility.cs` and paste the `HealAbility` class into it.
    *   Create a new C# script named `PlayerAbilityController.cs` and paste the `PlayerAbilityController` class into it.
    *   (Optional, for visual fireballs) Create `SimpleProjectile.cs` and paste the code.

2.  **Create Ability ScriptableObjects:**
    *   In your Unity Project window, right-click -> `Create` -> `AbilitySystem` -> `Ability Data` -> `Fireball`. Name it `Fireball_Ability_Data`.
        *   In the Inspector, set `Ability Name` to `Fireball`.
        *   Set `Cooldown Time` to `2.0`.
        *   Set `Mana Cost` to `15.0`.
        *   Set `Damage Amount` to `25.0`.
        *   Leave `Projectile Prefab` empty for now, or assign later (see step 4).
    *   Right-click -> `Create` -> `AbilitySystem` -> `Ability Data` -> `Heal`. Name it `Heal_Ability_Data`.
        *   In the Inspector, set `Ability Name` to `Heal`.
        *   Set `Cooldown Time` to `5.0`.
        *   Set `Mana Cost` to `20.0`.
        *   Set `Heal Amount` to `35.0`.

3.  **Create a Player GameObject:**
    *   In your Hierarchy window, right-click -> `3D Object` -> `Capsule`. Name it `Player`.
    *   Select the `Player` GameObject.
    *   **Add Component:** `Attribute Component`.
        *   You can adjust initial `Health` and `Mana` values here if desired (e.g., `Health: 100/100`, `Mana: 50/50`).
    *   **Add Component:** `Ability System Component`.
        *   In the Inspector, expand the `Initial Abilities` list.
        *   Drag your `Fireball_Ability_Data` and `Heal_Ability_Data` ScriptableObjects from your Project window into this list. This grants the player these abilities at the start of the game.
    *   **Add Component:** `Player Ability Controller`.
        *   The `Ability System` field should automatically link to the `AbilitySystemComponent` on the same GameObject. If not, drag it manually.
        *   You can also add abilities to the `Initial Abilities For Controller` list here if you prefer defining them on the controller itself, but for simplicity, setting them directly on the `AbilitySystemComponent` is often sufficient.

4.  **Optional: Create a Projectile Prefab (for `FireballAbility` visuals)**
    *   Right-click in Hierarchy -> `3D Object` -> `Sphere`. Name it `FireballProjectile`.
    *   Add Component: `Rigidbody`. (Uncheck `Use Gravity` for simple straight flight, set `Is Kinematic` to false).
    *   Add Component: `Sphere Collider` (default should be fine, ensure `Is Trigger` is unchecked if you want actual collisions).
    *   (Optional, if you created `SimpleProjectile.cs`) Add Component: `Simple Projectile`.
        *   Set its `Damage` to match your Fireball's damage (e.g., `25.0`).
    *   Drag the `FireballProjectile` GameObject from the Hierarchy into your Project window to make it a Prefab.
    *   Delete the `FireballProjectile` from the Hierarchy.
    *   Go back to your `Fireball_Ability_Data` ScriptableObject in the Project window and drag the `FireballProjectile` Prefab into the `Projectile Prefab` slot.

5.  **Run the Scene:**
    *   Press the Unity Play button.
    *   Watch the Console window for logs.
    *   Press `1` to cast Fireball.
    *   Press `2` to cast Heal.
    *   Press `Space` to take damage (to test healing).
    *   Press `R` to restore mana.
    *   Observe how cooldowns, mana consumption, health changes, and ability activations/ends are logged in the Console.

This setup provides a robust and extensible foundation for managing abilities in your Unity projects.