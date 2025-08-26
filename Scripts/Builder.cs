// Unity Design Pattern Example: Builder
// This script demonstrates the Builder pattern in Unity
// Generated automatically - ready to use in your Unity project

The Builder design pattern is a creational pattern that allows you to construct complex objects step by step. It separates the construction of a complex object from its representation, so that the same construction process can create different representations. This is particularly useful when an object has many possible optional parameters, or when its construction involves a complex sequence of steps.

In Unity, the Builder pattern can be applied to create GameObjects, UI elements, character configurations, level sections, or any other complex object that requires multiple steps or varying configurations during its creation.

---

## BuilderPatternExample.cs

To use this example:
1.  Create a new C# script in your Unity project called `BuilderPatternExample.cs`.
2.  Copy and paste the entire code below into the new script.
3.  Create an empty GameObject in your scene (e.g., named "BuilderDemo").
4.  Attach the `BuilderPatternDemo` component (found at the bottom of the script) to this GameObject.
5.  Run the scene.
6.  Observe the output in the Unity Console, which demonstrates the different characters being built.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Text; // Required for StringBuilder

// --- 1. Product: The complex object to be built ---
// The Character class represents the complex object we want to construct.
// It has several properties that can be configured.
public class Character
{
    public string Type { get; set; }        // E.g., Warrior, Mage, Rogue
    public int Health { get; set; }
    public int Strength { get; set; }
    public int Agility { get; set; }
    public List<string> Equipment { get; private set; } // List of equipment items

    public Character()
    {
        Equipment = new List<string>();
    }

    // A method to display the character's properties in a readable format.
    public void DisplayCharacter()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"--- Character Profile: {Type} ---");
        sb.AppendLine($"  Health: {Health}");
        sb.AppendLine($"  Strength: {Strength}");
        sb.AppendLine($"  Agility: {Agility}");
        sb.Append("  Equipment: ");
        if (Equipment.Count > 0)
        {
            sb.AppendLine(string.Join(", ", Equipment));
        }
        else
        {
            sb.AppendLine("None");
        }
        sb.AppendLine("--------------------------");

        Debug.Log(sb.ToString());
    }
}

// --- 2. Builder Interface: Defines the steps to build the product ---
// ICharacterBuilder declares a common interface for all concrete builders.
// It outlines the methods for building different parts of the Character object.
public interface ICharacterBuilder
{
    // Resets the builder to clear any previous construction,
    // ensuring a fresh start for a new product.
    void Reset();

    // Methods to set different properties of the Character.
    void SetType(string type);
    void SetHealth(int health);
    void SetStrength(int strength);
    void SetAgility(int agility);
    void AddEquipment(string item); // Allows adding multiple equipment items

    // Retrieves the final constructed Character object.
    Character GetCharacter();
}

// --- 3. Concrete Builders: Implement the Builder interface ---
// Each Concrete Builder provides a specific implementation of the building steps,
// constructing and assembling parts of the product.
// They store the product internally and return it when requested.

public class WarriorBuilder : ICharacterBuilder
{
    private Character _character; // The product being built by this builder

    // Constructor initializes the builder and creates a new Character instance.
    public WarriorBuilder()
    {
        Reset();
    }

    // Resets the builder, creating a new, empty Character object
    // to ensure subsequent builds start fresh.
    public void Reset()
    {
        _character = new Character();
    }

    // Implementations for setting specific character properties.
    // These methods configure the internal _character instance.
    public void SetType(string type)
    {
        _character.Type = type;
    }

    public void SetHealth(int health)
    {
        _character.Health = health;
    }

    public void SetStrength(int strength)
    {
        _character.Strength = strength;
    }

    public void SetAgility(int agility)
    {
        _character.Agility = agility;
    }

    public void AddEquipment(string item)
    {
        _character.Equipment.Add(item);
    }

    // Returns the constructed character.
    // IMPORTANT: It also calls Reset() immediately after returning the character
    // to ensure the builder is ready to build a new character from scratch
    // without carrying over properties from the just-built one.
    public Character GetCharacter()
    {
        Character result = _character;
        Reset(); // Prepare builder for next product
        return result;
    }
}

public class MageBuilder : ICharacterBuilder
{
    private Character _character;

    public MageBuilder()
    {
        Reset();
    }

    public void Reset()
    {
        _character = new Character();
    }

    public void SetType(string type)
    {
        _character.Type = type;
    }

    public void SetHealth(int health)
    {
        _character.Health = health;
    }

    public void SetStrength(int strength)
    {
        _character.Strength = strength;
    }

    public void SetAgility(int agility)
    {
        _character.Agility = agility;
    }

    public void AddEquipment(string item)
    {
        _character.Equipment.Add(item);
    }

    public Character GetCharacter()
    {
        Character result = _character;
        Reset();
        return result;
    }
}

public class RogueBuilder : ICharacterBuilder
{
    private Character _character;

    public RogueBuilder()
    {
        Reset();
    }

    public void Reset()
    {
        _character = new Character();
    }

    public void SetType(string type)
    {
        _character.Type = type;
    }

    public void SetHealth(int health)
    {
        _character.Health = health;
    }

    public void SetStrength(int strength)
    {
        _character.Strength = strength;
    }

    public void SetAgility(int agility)
    {
        _character.Agility = agility;
    }

    public void AddEquipment(string item)
    {
        _character.Equipment.Add(item);
    }

    public Character GetCharacter()
    {
        Character result = _character;
        Reset();
        return result;
    }
}


// --- 4. Director: Manages the construction process ---
// The Director class contains complex construction algorithms for common object types.
// It works with any builder instance passed to it by the client.
// It's responsible for orchestrating the building steps in a specific order.
public class CharacterDirector
{
    private ICharacterBuilder _builder; // The builder currently in use

    // Allows the client to set or change the builder dynamically.
    public ICharacterBuilder Builder
    {
        set { _builder = value; }
    }

    // Methods to construct predefined character configurations using the assigned builder.
    // The director doesn't know the concrete type of the builder, only its interface.

    public void ConstructWarrior()
    {
        if (_builder == null)
        {
            Debug.LogError("Director: Builder not set! Cannot construct character.");
            return;
        }
        _builder.Reset(); // Always reset the builder before starting a new construction
        _builder.SetType("Warrior");
        _builder.SetHealth(100);
        _builder.SetStrength(15);
        _builder.SetAgility(5);
        _builder.AddEquipment("Greatsword");
        _builder.AddEquipment("Plate Armor");
    }

    public void ConstructMage()
    {
        if (_builder == null)
        {
            Debug.LogError("Director: Builder not set! Cannot construct character.");
            return;
        }
        _builder.Reset();
        _builder.SetType("Mage");
        _builder.SetHealth(70);
        _builder.SetStrength(3);
        _builder.SetAgility(8);
        _builder.AddEquipment("Staff of Arcane Power");
        _builder.AddEquipment("Robe of the Archmage");
        _builder.AddEquipment("Spellbook");
    }

    public void ConstructRogue()
    {
        if (_builder == null)
        {
            Debug.LogError("Director: Builder not set! Cannot construct character.");
            return;
        }
        _builder.Reset();
        _builder.SetType("Rogue");
        _builder.SetHealth(80);
        _builder.SetStrength(8);
        _builder.SetAgility(18);
        _builder.AddEquipment("Dagger of Shadows");
        _builder.AddEquipment("Leather Armor");
        _builder.AddEquipment("Lockpicking Tools");
    }
}


// --- Unity Integration: Client code demonstrating the Builder pattern ---
// This MonoBehaviour script acts as the client that uses the Builder pattern
// to construct different types of characters.
public class BuilderPatternDemo : MonoBehaviour
{
    void Start()
    {
        Debug.Log("--- Builder Pattern Demo ---");

        // 1. Instantiate the Director
        // The Director orchestrates the building process.
        CharacterDirector director = new CharacterDirector();

        // 2. Instantiate Concrete Builders
        WarriorBuilder warriorBuilder = new WarriorBuilder();
        MageBuilder mageBuilder = new MageBuilder();
        RogueBuilder rogueBuilder = new RogueBuilder();

        // --- Example 1: Building a Warrior using WarriorBuilder via Director ---
        Debug.Log("\n--- Building a Warrior ---");
        // Assign the WarriorBuilder to the Director.
        // The Director now knows how to build using the Warrior's specific steps.
        director.Builder = warriorBuilder;

        // Director constructs a Warrior character.
        // The Director uses the builder's methods without knowing its concrete type.
        director.ConstructWarrior();

        // Get the final Character product from the builder.
        // The builder holds the partially constructed product until it's finished.
        Character warrior = warriorBuilder.GetCharacter();
        warrior.DisplayCharacter();


        // --- Example 2: Building a Mage using MageBuilder via Director ---
        Debug.Log("\n--- Building a Mage ---");
        // Change the builder for the Director. The same Director can now build a Mage.
        director.Builder = mageBuilder;
        director.ConstructMage();
        Character mage = mageBuilder.GetCharacter();
        mage.DisplayCharacter();

        // --- Example 3: Building a Rogue using RogueBuilder via Director ---
        Debug.Log("\n--- Building a Rogue ---");
        director.Builder = rogueBuilder;
        director.ConstructRogue();
        Character rogue = rogueBuilder.GetCharacter();
        rogue.DisplayCharacter();


        // --- Example 4: Custom Character Construction (without Director) ---
        // The Builder pattern also allows for direct, step-by-step construction
        // without a Director, giving more fine-grained control for unique builds.
        Debug.Log("\n--- Custom Character Construction (Paladin) ---");
        // Reusing a builder, but configuring it manually step-by-step.
        // This shows the flexibility of the builder.
        ICharacterBuilder customBuilder = new WarriorBuilder();
        customBuilder.Reset(); // Always reset before starting a new custom build
        customBuilder.SetType("Paladin");
        customBuilder.SetHealth(120);
        customBuilder.SetStrength(12);
        customBuilder.SetAgility(3);
        customBuilder.AddEquipment("Holy Avenger");
        customBuilder.AddEquipment("Full Plate of Righteousness");
        customBuilder.AddEquipment("Shield of Faith");
        Character paladin = customBuilder.GetCharacter();
        paladin.DisplayCharacter();

        // --- Example 5: Another Custom Character (Goblin Shaman) ---
        // Demonstrating how to use a different builder type as a base for custom configs.
        Debug.Log("\n--- Another Custom Character (Goblin Shaman) ---");
        MageBuilder goblinShamanBuilder = new MageBuilder(); // Using MageBuilder as a base
        goblinShamanBuilder.Reset();
        goblinShamanBuilder.SetType("Goblin Shaman");
        goblinShamanBuilder.SetHealth(60);
        goblinShamanBuilder.SetStrength(7);
        goblinShamanBuilder.SetAgility(10);
        goblinShamanBuilder.AddEquipment("Bone Staff");
        goblinShamanBuilder.AddEquipment("Shamanic Robes");
        goblinShamanBuilder.AddEquipment("Voodoo Fetish");
        Character goblinShaman = goblinShamanBuilder.GetCharacter();
        goblinShaman.DisplayCharacter();

        Debug.Log("--- Builder Pattern Demo Complete ---");
    }
}
```