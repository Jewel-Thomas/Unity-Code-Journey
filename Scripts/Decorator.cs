// Unity Design Pattern Example: Decorator
// This script demonstrates the Decorator pattern in Unity
// Generated automatically - ready to use in your Unity project

The Decorator design pattern allows behavior to be added to an individual object, dynamically, without affecting the behavior of other objects from the same class. It's often used to extend the functionality of an object in a flexible way, adhering to the Open/Closed Principle (open for extension, closed for modification).

In Unity, this pattern is highly practical for scenarios like:
*   **Buffs/Debuffs**: Dynamically adding temporary stat changes to characters.
*   **Item Enchantments**: Adding special properties to weapons or armor.
*   **Enemy AI Variations**: Combining different behavior modules for enemy types.
*   **Skill Customization**: Modifying the properties of a base skill.

This example will demonstrate how to use the Decorator pattern to modify character stats (Health, Attack, Defense, Speed) dynamically using various "buffs" or "effects."

---

## Unity Decorator Pattern Example: Character Stat Modifiers

This script creates a flexible system for modifying character statistics without altering the base `BaseCharacterStats` class. You can combine various "decorators" (buffs/effects) in any order to create unique stat profiles for your characters.

**How to use this script in Unity:**

1.  Create a new C# script in your Unity project called `DecoratorExample.cs`.
2.  Copy and paste the entire code below into the new script.
3.  Create an empty GameObject in your scene (e.g., "DecoratorPatternManager").
4.  Attach the `DecoratorExample` script to this GameObject.
5.  Run the scene. Observe the `Debug.Log` output in the Unity Console, which will show how character stats change as decorators are applied.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // Not strictly needed for this example, but often useful

// --- 1. The Component Interface ---
// This interface defines the core functionality that all concrete components
// and decorators must implement. It's the common contract for character stats.
public interface ICharacterStats
{
    float Health { get; }
    float Attack { get; }
    float Defense { get; }
    float Speed { get; }
    string GetDescription(); // A method to describe the current state of stats
}

// --- 2. The Concrete Component ---
// This is the basic, unadorned object that decorators will wrap.
// It provides the base implementation of the ICharacterStats interface.
public class BaseCharacterStats : ICharacterStats
{
    private float _baseHealth;
    private float _baseAttack;
    private float _baseDefense;
    private float _baseSpeed;

    public BaseCharacterStats(float health, float attack, float defense, float speed)
    {
        _baseHealth = health;
        _baseAttack = attack;
        _baseDefense = defense;
        _baseSpeed = speed;
    }

    // Properties simply return their base values
    public float Health => _baseHealth;
    public float Attack => _baseAttack;
    public float Defense => _baseDefense;
    public float Speed => _baseSpeed;

    // Provides a base description
    public virtual string GetDescription() => "Base Character";
}

// --- 3. The Base Decorator ---
// This abstract class implements the ICharacterStats interface and maintains
// a reference to the ICharacterStats object it decorates.
// Its methods simply delegate calls to the wrapped component by default.
// Concrete decorators will inherit from this and override specific methods
// to add new behavior or modify existing behavior.
public abstract class CharacterStatsDecorator : ICharacterStats
{
    protected ICharacterStats _wrappedStats; // The component being decorated

    public CharacterStatsDecorator(ICharacterStats stats)
    {
        _wrappedStats = stats;
    }

    // By default, decorators pass calls to the wrapped component.
    // Concrete decorators will override these to add their specific modifications.
    public virtual float Health => _wrappedStats.Health;
    public virtual float Attack => _wrappedStats.Attack;
    public virtual float Defense => _wrappedStats.Defense;
    public virtual float Speed => _wrappedStats.Speed;

    // Decorators will typically append to the description of the wrapped component.
    public virtual string GetDescription() => _wrappedStats.GetDescription();
}

// --- 4. Concrete Decorators ---
// These classes add specific functionalities to the component.
// Each one extends the behavior of the base component (or another decorator)
// by overriding one or more of the ICharacterStats properties/methods.

// Decorator to increase Attack stat
public class AttackBuffDecorator : CharacterStatsDecorator
{
    private float _attackBonus;

    public AttackBuffDecorator(ICharacterStats stats, float attackBonus) : base(stats)
    {
        _attackBonus = attackBonus;
    }

    // Override Attack to add the bonus to the wrapped component's Attack
    public override float Attack => _wrappedStats.Attack + _attackBonus;

    // Append to the description
    public override string GetDescription() => _wrappedStats.GetDescription() + " +Attack Buff";
}

// Decorator to increase Defense stat
public class DefenseBoostDecorator : CharacterStatsDecorator
{
    private float _defenseBonus;

    public DefenseBoostDecorator(ICharacterStats stats, float defenseBonus) : base(stats)
    {
        _defenseBonus = defenseBonus;
    }

    // Override Defense to add the bonus
    public override float Defense => _wrappedStats.Defense + _defenseBonus;

    // Append to the description
    public override string GetDescription() => _wrappedStats.GetDescription() + " +Defense Boost";
}

// Decorator to modify Speed stat (example with a multiplier)
public class SpeedBoostDecorator : CharacterStatsDecorator
{
    private float _speedMultiplier;

    public SpeedBoostDecorator(ICharacterStats stats, float speedMultiplier) : base(stats)
    {
        _speedMultiplier = speedMultiplier;
    }

    // Override Speed to apply a multiplier
    public override float Speed => _wrappedStats.Speed * _speedMultiplier;

    // Append to the description
    public override string GetDescription() => _wrappedStats.GetDescription() + " xSpeed Multiplier";
}

// Decorator to increase Health stat
public class HealthBoostDecorator : CharacterStatsDecorator
{
    private float _healthBonus;

    public HealthBoostDecorator(ICharacterStats stats, float healthBonus) : base(stats)
    {
        _healthBonus = healthBonus;
    }

    // Override Health to add the bonus
    public override float Health => _wrappedStats.Health + _healthBonus;

    // Append to the description
    public override string GetDescription() => _wrappedStats.GetDescription() + " +Health Boost";
}

// --- 5. Unity Example Usage (MonoBehaviour) ---
// This MonoBehaviour class demonstrates how to use the Decorator pattern
// by creating a base character and dynamically applying decorators.
public class DecoratorExample : MonoBehaviour
{
    void Start()
    {
        Debug.Log("--- Decorator Pattern Example in Unity ---");
        Debug.Log("This example demonstrates how to dynamically add functionalities (stat buffs)");
        Debug.Log("to a character without modifying its base class, using the Decorator pattern.");

        // 1. Create a base character with initial stats
        ICharacterStats character = new BaseCharacterStats(100, 10, 5, 1.0f);
        LogStats(character, "Initial Character Stats:");

        // 2. Apply an Attack Buff to the character
        // The 'character' variable now holds an AttackBuffDecorator that wraps the BaseCharacterStats.
        character = new AttackBuffDecorator(character, 15); // Add 15 attack
        LogStats(character, "After applying Attack Buff (+15 Attack):");

        // 3. Apply a Defense Boost
        // The 'character' variable now holds a DefenseBoostDecorator that wraps the AttackBuffDecorator.
        // This shows how decorators can be nested.
        character = new DefenseBoostDecorator(character, 10); // Add 10 defense
        LogStats(character, "After applying Defense Boost (+10 Defense):");

        // 4. Apply a Speed Boost (using a multiplier)
        character = new SpeedBoostDecorator(character, 1.5f); // 50% faster
        LogStats(character, "After applying Speed Boost (x1.5 Speed):");

        // 5. Apply a Health Boost
        character = new HealthBoostDecorator(character, 50); // Add 50 health
        LogStats(character, "After applying Health Boost (+50 Health):");

        Debug.Log("\n--- Chaining Multiple Decorators (New Character) ---");
        Debug.Log("Decorators can be nested to combine effects during creation.");

        // Example of creating a new character and immediately chaining multiple decorators.
        // Read this from inside out: BaseCharacterStats -> AttackBuff -> DefenseBoost -> SpeedBoost -> HealthBoost
        ICharacterStats anotherCharacter = new HealthBoostDecorator(
            new SpeedBoostDecorator(
                new DefenseBoostDecorator(
                    new AttackBuffDecorator(
                        new BaseCharacterStats(80, 8, 3, 0.8f), // Base Character
                        12), // + Attack Buff
                    8), // + Defense Boost
                1.3f), // x Speed Multiplier
            40); // + Health Boost

        LogStats(anotherCharacter, "Another character with multiple chained decorators:");

        Debug.Log("\n--- Benefits of the Decorator Pattern ---");
        Debug.Log("- **Flexibility**: Add or remove functionalities dynamically at runtime.");
        Debug.Log("- **Open/Closed Principle**: Extend behavior without modifying existing code (BaseCharacterStats is closed for modification, open for extension).");
        Debug.Log("- **Avoids Subclassing Explosion**: Instead of creating dozens of subclasses for every stat combination (e.g., 'WarriorWithSwordAndShield', 'WarriorWithSword'), you compose objects dynamically.");
        Debug.Log("- **Single Responsibility Principle**: Each decorator focuses on a single modification (e.g., Attack, Defense).");
        Debug.Log("- **Modular Code**: Each decorator is a small, focused piece of code.");
    }

    /// <summary>
    /// Helper method to log the current stats of an ICharacterStats object to the console.
    /// </summary>
    /// <param name="stats">The character stats object (can be base or decorated).</param>
    /// <param name="header">A descriptive header for the log entry.</param>
    private void LogStats(ICharacterStats stats, string header)
    {
        Debug.Log($"\n<color=cyan>{header}</color>");
        Debug.Log($"  Description: <color=yellow>{stats.GetDescription()}</color>");
        Debug.Log($"  Health: {stats.Health}");
        Debug.Log($"  Attack: {stats.Attack}");
        Debug.Log($"  Defense: {stats.Defense}");
        Debug.Log($"  Speed: {stats.Speed}");
    }
}
```