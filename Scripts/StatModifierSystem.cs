// Unity Design Pattern Example: StatModifierSystem
// This script demonstrates the StatModifierSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script provides a complete and practical implementation of the **Stat Modifier System** design pattern for Unity projects. It allows you to define base character or item stats and apply various types of modifiers (flat, percentage additive, percentage multiplicative) from different sources (items, buffs, abilities) in a structured and easily manageable way.

The system is designed to be highly flexible, allowing for complex stat calculations and dynamic changes during gameplay.

---

### How to Use This Script:

1.  **Create a new C# script** in your Unity project, name it `StatModifierSystemExample`.
2.  **Copy and paste** the entire code below into this new script.
3.  **Create an empty GameObject** in your scene (e.g., named "PlayerCharacter").
4.  **Attach the `StatModifierSystemExample` script** to the "PlayerCharacter" GameObject.
5.  **Run the scene.** Observe the Console for detailed logs demonstrating stat changes as modifiers are applied and removed.
6.  You can adjust the `_baseHealth`, `_baseStrength`, and `_baseAgility` values in the Inspector of the "PlayerCharacter" GameObject.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // Required for IEnumerator and Coroutines

/// <summary>
/// Defines the type of a StatModifier. This enum dictates how the modifier's value
/// is applied to the base stat. The order of these values implicitly suggests
/// the order of application in the Recalculate method (though explicit sorting
/// by 'Order' property in StatModifier is more robust).
/// </summary>
public enum StatModifierType
{
    /// <summary>
    /// Adds or subtracts directly from the base value.
    /// Example: +5 Strength, -10 Health.
    /// Applied first in the calculation order.
    /// </summary>
    Flat = 100, // Using arbitrary int values to hint at order if sorting by type
    /// <summary>
    /// Adds or subtracts a percentage of the *current value* after flat modifiers.
    /// These are typically combined additively among themselves before being applied.
    /// Example: +10% Strength (adds 10% of (Base + Flat) Strength).
    /// Applied second in the calculation order.
    /// </summary>
    PercentAdd = 200,
    /// <summary>
    /// Multiplies the current value (after Flat and PercentAdd modifiers) by a percentage.
    /// These are typically combined multiplicatively among themselves or applied sequentially.
    /// Example: x1.5 Damage (i.e., +50% total Damage).
    /// Applied last in the calculation order.
    /// </summary>
    PercentMult = 300
}

/// <summary>
/// Represents a single modification to a stat.
/// This class holds the value of the modifier, its type, and an optional source object
/// (e.g., an Item, Buff, or ability) for tracking and easy removal.
/// </summary>
[System.Serializable] // Makes it visible in the Inspector (though not directly used here, good practice)
public class StatModifier
{
    public float Value;
    public StatModifierType Type;
    [NonSerialized] // Don't serialize the source object reference
    public object Source; // Who or what applied this modifier (e.g., an Item, a Buff instance)

    /// <summary>
    /// Constructor for a StatModifier.
    /// </summary>
    /// <param name="value">The amount of the modification (e.g., 5 for Flat, 0.1 for 10%).</param>
    /// <param name="type">The type of modification (Flat, PercentAdd, PercentMult).</param>
    /// <param name="source">The object that applied this modifier (can be null).</param>
    public StatModifier(float value, StatModifierType type, object source)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}

/// <summary>
/// Represents a stat (like Health, Strength, Agility) that can be modified.
/// It holds a base value and manages a list of active StatModifiers,
/// automatically recalculating its effective value whenever modifiers are added or removed.
/// </summary>
[System.Serializable]
public class ModifiableStat
{
    // The base value of the stat, before any modifiers are applied.
    public float BaseValue { get; private set; }

    // The current calculated value of the stat, after all modifiers are applied.
    private float _currentValue;
    // Stores the last calculated value to detect changes and prevent redundant event invocations.
    private float _lastValue;

    // A list of all active modifiers affecting this stat.
    private readonly List<StatModifier> _modifiers;

    /// <summary>
    /// Event triggered when the stat's calculated value changes.
    /// Subscribers can react to stat updates (e.g., update UI, trigger effects).
    /// </summary>
    public event Action<float> OnStatValueChanged;

    /// <summary>
    /// Constructor for ModifiableStat.
    /// </summary>
    /// <param name="baseValue">The initial base value for this stat.</param>
    public ModifiableStat(float baseValue)
    {
        BaseValue = baseValue;
        _currentValue = baseValue;
        _lastValue = baseValue;
        _modifiers = new List<StatModifier>();
    }

    /// <summary>
    /// Gets the current calculated value of the stat.
    /// This property returns the cached calculated value. Call Recalculate()
    /// if modifiers have changed and the value hasn't been updated yet.
    /// </summary>
    /// <returns>The effective value of the stat.</returns>
    public float GetCalculatedValue()
    {
        return _currentValue;
    }

    /// <summary>
    /// Sets a new base value for the stat and triggers a recalculation.
    /// </summary>
    /// <param name="newBaseValue">The new base value.</param>
    public void SetBaseValue(float newBaseValue)
    {
        // Only update and recalculate if the base value has actually changed significantly.
        if (Mathf.Abs(BaseValue - newBaseValue) > 0.001f) // Use epsilon for float comparison
        {
            BaseValue = newBaseValue;
            Recalculate();
        }
    }

    /// <summary>
    /// Adds a StatModifier to this stat and triggers a recalculation.
    /// </summary>
    /// <param name="modifier">The StatModifier to add.</param>
    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        Recalculate();
    }

    /// <summary>
    /// Removes a specific StatModifier from this stat and triggers a recalculation.
    /// </summary>
    /// <param name="modifier">The StatModifier to remove.</param>
    /// <returns>True if the modifier was found and removed, false otherwise.</returns>
    public bool RemoveModifier(StatModifier modifier)
    {
        if (_modifiers.Remove(modifier))
        {
            Recalculate();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all StatModifiers that originated from a specific source object.
    /// This is useful for removing all buffs from an expired spell or all stats from an unequipped item.
    /// </summary>
    /// <param name="source">The source object whose modifiers should be removed.</param>
    /// <returns>True if any modifiers were removed, false otherwise.</returns>
    public bool RemoveAllModifiersFromSource(object source)
    {
        int numRemoved = _modifiers.RemoveAll(mod => mod.Source == source);
        if (numRemoved > 0)
        {
            Recalculate();
            return true;
        }
        return false;
    }

    /// <summary>
    /// The core logic for calculating the effective stat value based on base value and all active modifiers.
    /// Modifiers are applied in a specific order: Flat -> PercentAdd -> PercentMult.
    /// </summary>
    private void Recalculate()
    {
        float newValue = BaseValue;
        float sumPercentAdd = 0; // Accumulates the values of all PercentAdd modifiers

        // --- Step 1: Apply Flat modifiers ---
        // These add or subtract directly from the base value.
        foreach (StatModifier mod in _modifiers)
        {
            if (mod.Type == StatModifierType.Flat)
            {
                newValue += mod.Value;
            }
        }

        // --- Step 2: Apply PercentAdd modifiers ---
        // These are typically combined additively (e.g., +10% and +5% becomes +15%)
        // and then applied to the current value (Base + Flat modifiers).
        foreach (StatModifier mod in _modifiers)
        {
            if (mod.Type == StatModifierType.PercentAdd)
            {
                sumPercentAdd += mod.Value; // e.g., 0.10 for +10%
            }
        }
        newValue *= (1 + sumPercentAdd); // Apply combined percentage increase/decrease

        // --- Step 3: Apply PercentMult modifiers ---
        // These multiply the current value (after Flat and PercentAdd modifiers).
        // They are typically applied sequentially, each modifying the result of the previous.
        // E.g., a +50% Mult (value = 0.5) and a +20% Mult (value = 0.2) would be:
        // current_value * (1 + 0.5) * (1 + 0.2)
        foreach (StatModifier mod in _modifiers)
        {
            if (mod.Type == StatModifierType.PercentMult)
            {
                newValue *= (1 + mod.Value); // e.g., 0.10 for +10%
            }
        }

        // Ensure the stat value doesn't go below zero unless specific game logic allows it.
        _currentValue = Mathf.Max(0, newValue);

        // Check if the value has significantly changed before invoking the event.
        if (Mathf.Abs(_currentValue - _lastValue) > 0.001f)
        {
            _lastValue = _currentValue;
            OnStatValueChanged?.Invoke(_currentValue); // Notify subscribers of the change
        }
    }
}


/// <summary>
/// A simple example class to represent an item that can provide stat modifiers.
/// </summary>
public class Item
{
    public string Name;
    public List<StatModifier> Modifiers;

    public Item(string name, params StatModifier[] modifiers)
    {
        Name = name;
        Modifiers = new List<StatModifier>(modifiers);
    }
}

/// <summary>
/// A simple example class to represent a temporary buff that provides stat modifiers.
/// </summary>
public class Buff
{
    public string Name;
    public float Duration; // For simulation purposes, not managed by the StatModifierSystem itself
    public List<StatModifier> Modifiers;

    public Buff(string name, float duration, params StatModifier[] modifiers)
    {
        Name = name;
        Duration = duration;
        Modifiers = new List<StatModifier>(modifiers);
    }
}

/// <summary>
/// The main MonoBehaviour class demonstrating the StatModifierSystem in Unity.
/// Attach this script to a GameObject to see it in action.
/// </summary>
public class StatModifierSystemExample : MonoBehaviour
{
    [Header("Base Stats (Editable in Inspector)")]
    [SerializeField] private float _baseHealth = 100f;
    [SerializeField] private float _baseStrength = 10f;
    [SerializeField] private float _baseAgility = 5f;

    // Public ModifiableStat instances for our character
    public ModifiableStat Health;
    public ModifiableStat Strength;
    public ModifiableStat Agility;

    private void Awake()
    {
        // Initialize our ModifiableStats with their base values.
        Health = new ModifiableStat(_baseHealth);
        Strength = new ModifiableStat(_baseStrength);
        Agility = new ModifiableStat(_baseAgility);

        // Subscribe to stat change events to log updates to the console.
        // This demonstrates how other systems (e.g., UI, combat system)
        // can react to stat changes.
        Health.OnStatValueChanged += (newValue) => Debug.Log($"<color=cyan>[Stat Changed]</color> Health changed to: {newValue:F2}");
        Strength.OnStatValueChanged += (newValue) => Debug.Log($"<color=cyan>[Stat Changed]</color> Strength changed to: {newValue:F2}");
        Agility.OnStatValueChanged += (newValue) => Debug.Log($"<color=cyan>[Stat Changed]</color> Agility changed to: {newValue:F2}");
    }

    // Start is called before the first frame update.
    // We use Start() to demonstrate adding and removing various modifiers.
    void Start()
    {
        Debug.Log("--- Initial Character Stats ---");
        LogStats();

        // --- DEMONSTRATION OF ADDING MODIFIERS ---

        // 1. Add a Flat Modifier (e.g., from a temporary potion)
        // We use 'this' (the CharacterStatsExample MonoBehaviour) as the source for simplicity.
        // In a real game, this might be a PotionItem instance.
        StatModifier strengthPotionBuff = new StatModifier(15f, StatModifierType.Flat, this);
        Strength.AddModifier(strengthPotionBuff);
        Debug.Log("\n--- After drinking a Strength Potion (+15 Flat Strength) ---");
        LogStats();

        // 2. Add a PercentAdd Modifier (e.g., from an environmental aura)
        StatModifier agilityAuraBuff = new StatModifier(0.25f, StatModifierType.PercentAdd, this); // +25% Agility
        Agility.AddModifier(agilityAuraBuff);
        Debug.Log("\n--- After entering Agility Aura (+25% Agility of current Agility after flat) ---");
        LogStats();

        // 3. Add multiple Modifiers from an 'Item' object
        // This demonstrates how an item can encapsulate multiple stat changes.
        Item swordOfMight = new Item("Sword of Might",
            new StatModifier(10f, StatModifierType.Flat, null),      // Flat +10 Strength
            new StatModifier(0.1f, StatModifierType.PercentAdd, null) // +10% Strength
        );

        // It's crucial to pass the *actual item object* (swordOfMight) as the source
        // so we can remove all its modifiers easily when the item is unequipped.
        Debug.Log($"\n--- Equipping {swordOfMight.Name} (+10 Flat, +10% Add Strength) ---");
        foreach (var mod in swordOfMight.Modifiers)
        {
            mod.Source = swordOfMight; // Assign the item itself as the source
            Strength.AddModifier(mod);
        }
        LogStats();

        // 4. Add Modifiers from a 'Buff' object, including PercentMult
        // This buff affects multiple stats with different modifier types.
        Buff berserkerRage = new Buff("Berserker Rage", 10f, // 10-second duration (for demo purposes)
            new StatModifier(0.5f, StatModifierType.PercentMult, null), // +50% total Health
            new StatModifier(0.3f, StatModifierType.PercentAdd, null)   // +30% total Strength
        );

        Debug.Log($"\n--- Activating {berserkerRage.Name} (+50% Mult Health, +30% Add Strength) ---");
        foreach (var mod in berserkerRage.Modifiers)
        {
            mod.Source = berserkerRage; // Assign the buff itself as the source
            if (mod.Type == StatModifierType.PercentMult) Health.AddModifier(mod);
            else if (mod.Type == StatModifierType.PercentAdd) Strength.AddModifier(mod);
        }
        LogStats();

        // --- DEMONSTRATION OF REMOVING MODIFIERS ---

        // 5. Remove a specific modifier (e.g., potion effect wears off)
        Debug.Log("\n--- Strength Potion wears off (-15 Flat Strength) ---");
        Strength.RemoveModifier(strengthPotionBuff);
        LogStats();

        // 6. Remove all modifiers from a specific source (e.g., unequipping the sword)
        Debug.Log($"\n--- Unequipping {swordOfMight.Name} (-10 Flat, -10% Add Strength) ---");
        Strength.RemoveAllModifiersFromSource(swordOfMight);
        LogStats();

        // 7. Simulate a buff expiring after a delay (using a Coroutine)
        // This showcases how temporary effects are handled.
        // We use a short delay for demonstration purposes.
        StartCoroutine(SimulateBuffExpiration(berserkerRage, 3f)); // Buff expires after 3 seconds
    }

    /// <summary>
    /// Coroutine to simulate a buff expiring after a set delay.
    /// </summary>
    /// <param name="buff">The buff object to remove modifiers from.</param>
    /// <param name="delay">The time in seconds before the buff expires.</param>
    IEnumerator SimulateBuffExpiration(Buff buff, float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified duration

        Debug.Log($"\n--- <color=orange>{buff.Name} expires!</color> Removing all its modifiers ---");
        // Remove all modifiers associated with this buff from all affected stats.
        Health.RemoveAllModifiersFromSource(buff);
        Strength.RemoveAllModifiersFromSource(buff);
        // Agility.RemoveAllModifiersFromSource(buff); // If Agility was affected
        LogStats();
    }

    /// <summary>
    /// Helper method to log the current base and calculated values of all stats.
    /// </summary>
    void LogStats()
    {
        Debug.Log($"<color=green>Health:</color> Base={Health.BaseValue:F2}, Current={Health.GetCalculatedValue():F2}");
        Debug.Log($"<color=green>Strength:</color> Base={Strength.BaseValue:F2}, Current={Strength.GetCalculatedValue():F2}");
        Debug.Log($"<color=green>Agility:</color> Base={Agility.BaseValue:F2}, Current={Agility.GetCalculatedValue():F2}");
    }
}

```