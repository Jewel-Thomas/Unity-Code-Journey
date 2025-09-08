// Unity Design Pattern Example: AttributeModifierSystem
// This script demonstrates the AttributeModifierSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# script demonstrates the **Attribute Modifier System** design pattern in Unity. It allows you to define base attributes (like Strength, Health, Speed) and then apply various types of modifiers (flat bonuses, percentage bonuses) that dynamically change these attributes.

This pattern is fundamental for RPGs, character stats, item effects, buffs, and debuffs.

**Key Features:**

1.  **`Attribute` Class:** Represents a single game attribute with a base value and a calculated current value.
2.  **`AttributeModifier` Struct:** Defines a specific change to an attribute (value, type, source).
3.  **`ModifierType` Enum:** Categorizes modifiers (Flat, PercentAdd, PercentMult) for proper calculation order.
4.  **`ModifiableAttributes` Component:** A `MonoBehaviour` that manages a collection of attributes for a game entity.
5.  **Recalculation Logic:** Attributes automatically recalculate their current value whenever modifiers are added, removed, or their base value changes.
6.  **Clear Order of Operations:** Modifiers are applied in a specific, sensible order (Flat -> PercentAdd -> PercentMult) to ensure consistent results.
7.  **Source Tracking:** Modifiers can be associated with a "source" (e.g., "Sword of Might", "StrengthBuffSpell"), making it easy to remove all modifiers from a specific origin.

---

### How to Use This Script in Unity:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `AttributeModifierSystem.cs`.
2.  **Copy and Paste:** Replace the default content of the new script with the code provided below.
3.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., "PlayerCharacter" or "EnemyUnit").
4.  **Add `ModifiableAttributes` Component:** Drag and drop the `AttributeModifierSystem.cs` script onto this GameObject in the Inspector, or use the "Add Component" button and search for "ModifiableAttributes".
5.  **Define Initial Attributes:** In the Inspector, for the `ModifiableAttributes` component, expand the "Attributes" list.
    *   Click the '+' button to add new attributes.
    *   Give them names (e.g., "Strength", "Health", "Speed").
    *   Set their `Base Value`.
6.  **Run the Scene:** Observe the `Debug.Log` output in the Console to see how attributes change with applied modifiers. You can also modify the `Start()` method in `ModifiableAttributes` to experiment with adding/removing modifiers.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for summing modifiers

/// <summary>
/// Defines the types of modifiers that can be applied to an attribute.
/// The order of these enums is crucial as it dictates the order of calculation.
/// </summary>
public enum ModifierType
{
    /// <summary>
    /// Flat value added directly to the base value.
    /// Example: +5 Strength from a ring.
    /// </summary>
    Flat,

    /// <summary>
    /// Percentage added to the base value.
    /// Example: +10% Strength from a spell (if base strength is 100, adds 10).
    /// </summary>
    PercentAdd,

    /// <summary>
    /// Percentage multiplied against the *total* value calculated so far (base + Flat + PercentAdd).
    /// Example: +20% Total Damage from a powerful relic.
    /// </summary>
    PercentMult
}

/// <summary>
/// Represents a single modifier that can be applied to an attribute.
/// This struct is serializable so it can be viewed in the Inspector if needed,
/// though modifiers are typically created dynamically at runtime.
/// </summary>
[System.Serializable]
public struct AttributeModifier
{
    /// <summary>
    /// The value of the modifier (e.g., 5 for Flat, 0.10 for 10% PercentAdd).
    /// </summary>
    public float Value;

    /// <summary>
    /// The type of modifier (Flat, PercentAdd, PercentMult).
    /// </summary>
    public ModifierType Type;

    /// <summary>
    /// An object representing the source of the modifier (e.g., a specific item, a spell, a buff).
    /// This is useful for removing all modifiers originating from a particular source.
    /// </summary>
    public object Source; // Using 'object' allows any type to be a source (e.g., an Item script, a string, a unique ID)

    public AttributeModifier(float value, ModifierType type, object source)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}


/// <summary>
/// Represents a single game attribute (e.g., Strength, Health, Speed).
/// It has a base value and a dynamically calculated current value based on applied modifiers.
/// </summary>
[System.Serializable]
public class Attribute
{
    [SerializeField] private string _name; // The name of the attribute (e.g., "Strength")
    [SerializeField] private float _baseValue; // The initial, unmodified value of the attribute.

    private float _currentValue; // The dynamically calculated value of the attribute.
    private readonly List<AttributeModifier> _modifiers = new List<AttributeModifier>(); // List of active modifiers.

    /// <summary>
    /// Event fired when the attribute's current value changes.
    /// </summary>
    public event Action<Attribute> OnValueRecalculated;

    /// <summary>
    /// Gets the name of the attribute.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets or sets the base value of the attribute.
    /// Setting a new base value will trigger a recalculation.
    /// </summary>
    public float BaseValue
    {
        get => _baseValue;
        set
        {
            if (_baseValue != value)
            {
                _baseValue = value;
                RecalculateValue();
            }
        }
    }

    /// <summary>
    /// Gets the current calculated value of the attribute, including all modifiers.
    /// </summary>
    public float CurrentValue => _currentValue;

    /// <summary>
    /// Initializes a new instance of the Attribute class.
    /// </summary>
    /// <param name="name">The name of the attribute (e.g., "Strength").</param>
    /// <param name="baseValue">The initial base value.</param>
    public Attribute(string name, float baseValue)
    {
        _name = name;
        _baseValue = baseValue;
        _currentValue = baseValue; // Initialize current value to base value
    }

    /// <summary>
    /// Adds a modifier to this attribute and triggers a recalculation.
    /// </summary>
    /// <param name="modifier">The modifier to add.</param>
    public void AddModifier(AttributeModifier modifier)
    {
        _modifiers.Add(modifier);
        RecalculateValue();
    }

    /// <summary>
    /// Removes a specific modifier from this attribute and triggers a recalculation.
    /// Note: This uses reference equality for objects, and value equality for structs.
    /// For structs, all fields (Value, Type, Source) must match.
    /// </summary>
    /// <param name="modifier">The modifier to remove.</param>
    public void RemoveModifier(AttributeModifier modifier)
    {
        if (_modifiers.Remove(modifier))
        {
            RecalculateValue();
        }
    }

    /// <summary>
    /// Removes all modifiers from a specific source (e.g., all buffs from a particular spell).
    /// </summary>
    /// <param name="source">The source identifier.</param>
    public void RemoveAllModifiersFromSource(object source)
    {
        int initialCount = _modifiers.Count;
        _modifiers.RemoveAll(mod => mod.Source == source); // Use reference equality for source
        if (_modifiers.Count != initialCount)
        {
            RecalculateValue();
        }
    }

    /// <summary>
    /// Recalculates the attribute's current value based on its base value and all active modifiers.
    /// Modifiers are applied in a specific order: Flat -> PercentAdd -> PercentMult.
    /// </summary>
    private void RecalculateValue()
    {
        float newValue = _baseValue;

        // 1. Apply Flat modifiers
        newValue += _modifiers.Where(mod => mod.Type == ModifierType.Flat).Sum(mod => mod.Value);

        // 2. Apply PercentAdd modifiers
        // These are percentages added to the base value.
        // Example: Base 100, +10% from spell, +5% from talent -> 100 + (100 * 0.10) + (100 * 0.05) = 115
        float percentAddSum = _modifiers.Where(mod => mod.Type == ModifierType.PercentAdd).Sum(mod => mod.Value);
        newValue *= (1 + percentAddSum); // Apply sum of percentage adds

        // 3. Apply PercentMult modifiers
        // These are percentages that multiply against the *current total* value.
        // Example: If current total is 115, and a relic gives +20% Total Damage -> 115 * (1 + 0.20) = 138
        float percentMultSum = _modifiers.Where(mod => mod.Type == ModifierType.PercentMult).Sum(mod => mod.Value);
        newValue *= (1 + percentMultSum); // Apply sum of percentage mults

        // Ensure value doesn't go below 0 unless specifically desired (e.g., for negative stats)
        _currentValue = Mathf.Max(0, newValue); 

        // Inform subscribers that the value has changed
        OnValueRecalculated?.Invoke(this);
    }
}


/// <summary>
/// A MonoBehaviour component that manages a collection of Attributes for a game entity.
/// This component would typically be attached to a Player, Enemy, or Item GameObject.
/// </summary>
public class ModifiableAttributes : MonoBehaviour
{
    [Tooltip("List of attributes for this entity. Define initial attributes here.")]
    [SerializeField] private List<Attribute> _attributes = new List<Attribute>();

    /// <summary>
    /// Provides read-only access to the list of attributes.
    /// </summary>
    public IReadOnlyList<Attribute> Attributes => _attributes;

    void Awake()
    {
        // Ensure all attributes are properly initialized and have their current value calculated.
        foreach (var attribute in _attributes)
        {
            // Subscribe to the recalculation event for each attribute
            attribute.OnValueRecalculated += OnAttributeValueRecalculated;
            // Force an initial recalculation in case base values were set directly in editor
            // and not through the property setter.
            attribute.BaseValue = attribute.BaseValue; 
        }

        Debug.Log("ModifiableAttributes: Awake - Initializing attributes.");
    }

    void Start()
    {
        Debug.Log("ModifiableAttributes: Start - Demonstrating attribute modification.");

        // --- Example Usage ---

        // Get an attribute by name
        Attribute strength = GetAttribute("Strength");
        Attribute health = GetAttribute("Health");
        Attribute speed = GetAttribute("Speed");

        if (strength != null)
        {
            Debug.Log($"Initial Strength: {strength.CurrentValue}");

            // 1. Add a Flat Modifier
            var ringOfPower = new AttributeModifier(10f, ModifierType.Flat, "RingOfPower");
            strength.AddModifier(ringOfPower);
            Debug.Log($"Strength after RingOfPower (+10 Flat): {strength.CurrentValue}"); // Should be Base + 10

            // 2. Add a PercentAdd Modifier (based on BASE value)
            var strengthBuff = new AttributeModifier(0.25f, ModifierType.PercentAdd, "StrengthBuffSpell"); // +25%
            strength.AddModifier(strengthBuff);
            Debug.Log($"Strength after StrengthBuffSpell (+25% Base): {strength.CurrentValue}"); // Should be Base + 10 + (Base * 0.25)

            // 3. Add a PercentMult Modifier (based on CURRENT total)
            var berserkPotion = new AttributeModifier(0.50f, ModifierType.PercentMult, "BerserkPotion"); // +50% Total
            strength.AddModifier(berserkPotion);
            Debug.Log($"Strength after BerserkPotion (+50% Total): {strength.CurrentValue}"); // Should be (Base + 10 + (Base * 0.25)) * 1.50

            // 4. Remove a specific modifier
            strength.RemoveModifier(strengthBuff);
            Debug.Log($"Strength after removing StrengthBuffSpell: {strength.CurrentValue}"); // BerserkPotion and RingOfPower remain

            // 5. Add another modifier to show order again
            var debuffSlow = new AttributeModifier(-0.3f, ModifierType.PercentMult, "SlowDebuff"); // -30% total
            speed?.AddModifier(debuffSlow);
            Debug.Log($"Speed after SlowDebuff (-30% Total): {speed?.CurrentValue ?? 0}");

            // 6. Change base value and see recalculation
            if (health != null)
            {
                Debug.Log($"Initial Health: {health.CurrentValue}");
                health.BaseValue = 200f; // This will trigger recalculation
                Debug.Log($"Health after increasing Base Value to 200: {health.CurrentValue}");
            }

            // 7. Remove all modifiers from a specific source
            strength.RemoveAllModifiersFromSource("BerserkPotion");
            Debug.Log($"Strength after removing all BerserkPotion modifiers: {strength.CurrentValue}"); // Only RingOfPower remains
        }
        else
        {
            Debug.LogError("Strength attribute not found. Please add 'Strength', 'Health', and 'Speed' attributes in the Inspector.");
        }
    }

    /// <summary>
    /// Event handler for when an attribute's value is recalculated.
    /// Can be used to update UI, trigger other game logic, etc.
    /// </summary>
    /// <param name="attribute">The attribute that was recalculated.</param>
    private void OnAttributeValueRecalculated(Attribute attribute)
    {
        Debug.Log($"'{attribute.Name}' attribute recalculated. New Value: {attribute.CurrentValue}");
        // Example: Update a UI text for this attribute
        // UIManager.Instance.UpdateStatText(attribute.Name, attribute.CurrentValue);
    }


    /// <summary>
    /// Gets an attribute by its name.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to retrieve.</param>
    /// <returns>The Attribute object if found, otherwise null.</returns>
    public Attribute GetAttribute(string attributeName)
    {
        return _attributes.Find(attr => attr.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a new attribute to the entity.
    /// </summary>
    /// <param name="attribute">The attribute to add.</param>
    public void AddAttribute(Attribute attribute)
    {
        if (GetAttribute(attribute.Name) == null)
        {
            _attributes.Add(attribute);
            attribute.OnValueRecalculated += OnAttributeValueRecalculated;
            attribute.BaseValue = attribute.BaseValue; // Trigger initial calculation
            Debug.Log($"Added new attribute: {attribute.Name}");
        }
        else
        {
            Debug.LogWarning($"Attribute '{attribute.Name}' already exists.");
        }
    }

    /// <summary>
    /// Removes an attribute from the entity.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to remove.</param>
    public void RemoveAttribute(string attributeName)
    {
        Attribute attributeToRemove = GetAttribute(attributeName);
        if (attributeToRemove != null)
        {
            attributeToRemove.OnValueRecalculated -= OnAttributeValueRecalculated;
            _attributes.Remove(attributeToRemove);
            Debug.Log($"Removed attribute: {attributeName}");
        }
        else
        {
            Debug.LogWarning($"Attribute '{attributeName}' not found.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when the object is destroyed
        foreach (var attribute in _attributes)
        {
            attribute.OnValueRecalculated -= OnAttributeValueRecalculated;
        }
    }
}
```