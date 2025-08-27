// Unity Design Pattern Example: DataDrivenDesign
// This script demonstrates the DataDrivenDesign pattern in Unity
// Generated automatically - ready to use in your Unity project

The Data-Driven Design pattern is a powerful approach in game development (and software development in general) where the application's behavior is dictated by external data rather than being hardcoded. This allows for immense flexibility, easier iteration, and better collaboration between designers and programmers.

In Unity, ScriptableObjects are a primary tool for implementing Data-Driven Design. They are data containers that can be saved as assets in your project, allowing you to define complex game entities, configurations, or rules without writing new code.

This example demonstrates how to create different types of in-game characters (heroes, enemies, NPCs) and their abilities purely through data assets, while a single C# script (`CharacterManager`) interprets and acts upon that data.

---

### Key Concepts of Data-Driven Design Demonstrated:

1.  **Separation of Concerns:** The character's definition (stats, abilities, visual model) is entirely separate from the logic that uses and displays that character (`CharacterManager`).
2.  **Flexibility & Iteration:** New characters or abilities can be created, modified, or balanced by simply creating/editing ScriptableObject assets in the Unity Editor, without touching a single line of C# code.
3.  **Reusability:** Ability definitions (`AbilityData`) can be reused across multiple different character types, promoting modularity.
4.  **Designer Empowerment:** Game designers can directly adjust character properties and abilities without needing a programmer, speeding up development and iteration cycles.
5.  **Scalability:** Managing a large number of distinct game entities becomes manageable as their configurations are externalized.

---

### Unity Setup Instructions:

Follow these steps to get the example working in your Unity project:

1.  **Create C# Scripts:**
    *   Create a new C# script named `AbilityData.cs` and paste the first code block into it.
    *   Create a new C# script named `CharacterStats.cs` and paste the second code block into it.
    *   Create a new C# script named `CharacterData.cs` and paste the third code block into it.
    *   Create a new C# script named `CharacterManager.cs` and paste the fourth (main) code block into it.

2.  **Create ScriptableObject Assets (Data):**
    *   **Abilities:** In your Project window, right-click -> `Create/Game Data/Ability Data`. Create a few different abilities (e.g., "Fireball", "Heal Spell", "Stun Shot"). Configure their `abilityName`, `basePower`, `cooldown`, `manaCost`, and `effectType` in the Inspector.
    *   **Characters:** Right-click -> `Create/Game Data/Character Data`. Create a few different characters (e.g., "Hero_Knight", "Enemy_Goblin", "NPC_Merchant").
        *   Configure their `characterName`, `description`, and `baseStats`.
        *   **Crucially:** Drag and drop your created `AbilityData` assets from the Project window into the `Abilities` list of each `CharacterData` asset.
        *   (Optional but Recommended): Create some simple 3D prefabs (e.g., a colored Cube, a Sphere, or any existing simple model) and assign them to the `Character Prefab` field in your `CharacterData` assets. This makes the visual representation data-driven too. If you don't assign a prefab, a fallback cube will be used.

3.  **Create Scene Objects (Consumers):**
    *   In your Unity Scene, create an empty GameObject (e.g., `GameObject/Create Empty`). Name it "PlayerCharacter".
    *   Select "PlayerCharacter" and in the Inspector, click `Add Component` and search for `CharacterManager`. Add it.
    *   Drag one of your created `CharacterData` assets (e.g., "Hero_Knight" from your Project window) into the `Character Data` slot of the `CharacterManager` component.
    *   Repeat this process for another character (e.g., create an "EnemyCharacter" GameObject, add `CharacterManager`, and assign your "Enemy_Goblin" `CharacterData` asset). Position them differently in the scene so you can see them.

4.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe how each `CharacterManager` instance in the scene automatically configures itself (sets its name, instantiates its model, displays its stats, and simulates an ability use) based *entirely* on the `CharacterData` ScriptableObject it references.
    *   You will see different behaviors, stats, and models for each character, all driven by the data assets you created.
    *   Stop play mode, modify an `AbilityData` or `CharacterData` asset (e.g., change the 'Fireball' damage or 'Hero_Knight's health), and run again to see the changes immediately without modifying any C# code.

---

### 1. `AbilityData.cs`

This ScriptableObject defines the properties of a generic ability. It's a reusable data structure for any ability in your game.

```csharp
using UnityEngine;

/// <summary>
/// Defines different types of effects an ability can have.
/// This is an example of data itself driving logic paths (via switch statements).
/// </summary>
public enum AbilityEffectType { Damage, Heal, Buff, Debuff }

/// <summary>
/// AbilityData is a ScriptableObject that represents a single ability definition.
/// It's a data container for all properties an ability might have.
/// This separates ability data from any character that uses it, promoting reusability.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Game Data/Ability Data")]
public class AbilityData : ScriptableObject
{
    [Header("Basic Info")]
    public string abilityName = "New Ability";
    [TextArea]
    public string description = "Ability description.";

    [Header("Combat Stats")]
    public int basePower = 10;          // e.g., damage amount, heal amount, buff strength
    public float cooldown = 5f;         // How long before it can be used again
    public int manaCost = 5;            // Resource cost to use the ability
    public AbilityEffectType effectType = AbilityEffectType.Damage; // What kind of effect it has

    /// <summary>
    /// Simulates using this ability. In a real game, this would trigger actual game logic.
    /// The logic here is also driven by data (the 'effectType').
    /// </summary>
    /// <param name="user">The character using this ability.</param>
    /// <param name="target">The target of this ability.</param>
    public void UseAbility(CharacterManager user, CharacterManager target)
    {
        // Log the action to demonstrate the ability being "used" based on its data.
        Debug.Log($"<color=cyan>{user.CharacterData.characterName}</color> uses <color=yellow>{abilityName}</color> on <color=green>{target.CharacterData.characterName}</color>!");

        // The specific effect logic is determined by the 'effectType' data.
        switch (effectType)
        {
            case AbilityEffectType.Damage:
                Debug.Log($"   -> Deals <color=red>{basePower}</color> damage.");
                // In a real game, 'target' would have a TakeDamage method:
                // target.TakeDamage(basePower);
                break;
            case AbilityEffectType.Heal:
                Debug.Log($"   -> Heals for <color=green>{basePower}</color> health.");
                // target.Heal(basePower);
                break;
            case AbilityEffectType.Buff:
                Debug.Log($"   -> Applies a buff (e.g., +{basePower} attack).");
                // target.ApplyBuff(this);
                break;
            case AbilityEffectType.Debuff:
                Debug.Log($"   -> Applies a debuff (e.g., -{basePower} defense).");
                // target.ApplyDebuff(this);
                break;
        }
    }
}
```

---

### 2. `CharacterStats.cs`

A simple `struct` to hold basic character statistics. This is marked `[System.Serializable]` so it can be embedded and edited directly within other ScriptableObjects (like `CharacterData`) in the Unity Inspector.

```csharp
using System;
using UnityEngine; // For Range attribute if desired

/// <summary>
/// A serializable struct to define a character's core statistics.
/// Using a struct means stats are copied by value, ensuring base data integrity.
/// Runtime stats (current health, current mana) would typically live on the MonoBehaviour.
/// </summary>
[Serializable] // This is crucial for Unity to display and edit this struct in the Inspector.
public struct CharacterStats
{
    [Range(1, 1000)] public int maxHealth;
    [Range(1, 100)] public int attackPower;
    [Range(0, 50)] public int defense;
    [Range(1, 20)] public float movementSpeed; // For example, units per second

    public CharacterStats(int health, int attack, int def, float speed)
    {
        maxHealth = health;
        attackPower = attack;
        defense = def;
        movementSpeed = speed;
    }

    /// <summary>
    /// Helper method to log the character's base stats.
    /// </summary>
    public void DisplayStats()
    {
        Debug.Log($"  Health: <color=lime>{maxHealth}</color>");
        Debug.Log($"  Attack: <color=orange>{attackPower}</color>");
        Debug.Log($"  Defense: <color=grey>{defense}</color>");
        Debug.Log($"  Speed: <color=blue>{movementSpeed}</color>");
    }
}
```

---

### 3. `CharacterData.cs`

This is the central ScriptableObject for defining a character. It combines basic information, stats, and a list of abilities. This is the "data" that drives our `CharacterManager`.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CharacterData is a ScriptableObject that defines a complete character archetype.
/// It contains all the static data for a character: name, visual model, base stats, and abilities.
/// By creating multiple CharacterData assets, you can define many unique characters
/// without changing any code.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Definition")]
    public string characterName = "New Character";
    [TextArea]
    public string description = "A brave new character ready for adventure!";

    [Tooltip("Optional: Assign a GameObject Prefab for the visual representation of this character.")]
    public GameObject characterPrefab; // Reference to a visual prefab (e.g., a 3D model)

    [Header("Core Stats")]
    public CharacterStats baseStats = new CharacterStats(100, 10, 5, 5f); // Uses our CharacterStats struct

    [Header("Abilities")]
    [Tooltip("Drag and drop AbilityData ScriptableObjects here to grant abilities to this character.")]
    public List<AbilityData> abilities = new List<AbilityData>(); // A list of references to AbilityData assets

    /// <summary>
    /// Helper method to display all character information defined in this data asset.
    /// </summary>
    public void DisplayCharacterInfo()
    {
        Debug.Log($"--- <color=yellow>{characterName}</color> ---");
        Debug.Log($"Description: {description}");
        Debug.Log($"Base Stats:");
        baseStats.DisplayStats(); // Call the DisplayStats method from our struct
        Debug.Log($"Abilities ({abilities.Count}):");
        foreach (var ability in abilities)
        {
            Debug.Log($"  - {ability.abilityName} (Power: {ability.basePower}, Cooldown: {ability.cooldown}s, Cost: {ability.manaCost})");
        }
        Debug.Log("--------------------");
    }
}
```

---

### 4. `CharacterManager.cs`

This is the `MonoBehaviour` script that acts as the "consumer" of the data. It doesn't hardcode any character properties or abilities. Instead, it references a `CharacterData` ScriptableObject and configures itself entirely based on the data provided by that asset.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CharacterManager is a MonoBehaviour that demonstrates the Data-Driven Design pattern.
/// It uses a 'CharacterData' ScriptableObject to define its properties and behavior
/// at runtime, rather than having them hardcoded within this script itself.
/// This script is the "logic" component that interprets and acts on the "data" component.
/// </summary>
public class CharacterManager : MonoBehaviour
{
    [Tooltip("Assign a CharacterData ScriptableObject here to define this character's properties and abilities.")]
    public CharacterData CharacterData; // The core data asset that drives this manager's behavior.

    // Runtime variables that might be influenced by CharacterData but track current state.
    private GameObject _instantiatedCharacterModel;
    private CharacterStats _runtimeStats; // A copy of the base stats from CharacterData
    private List<AbilityData> _runtimeAbilities = new List<AbilityData>(); // References to actual ability assets
    private bool _isInitialized = false;

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Initialize the character when the GameObject awakes.
        // This makes sure the character is set up based on its data as soon as possible.
        InitializeCharacter();
    }

    void Start()
    {
        // Example: Simulate ability usage after a short delay, to show abilities in action.
        if (_runtimeAbilities.Count > 0)
        {
            Invoke("SimulateAbilityUsage", 3f); // Call SimulateAbilityUsage after 3 seconds
        }
        else
        {
            Debug.LogWarning($"<color=red>[{gameObject.name}]</color> has no abilities assigned in its CharacterData. Cannot simulate ability usage.", this);
        }
    }

    void Update()
    {
        if (!_isInitialized) return;

        // Example of dynamic behavior driven by data: Move the character.
        // The movement speed is directly pulled from the _runtimeStats, which came from CharacterData.
        transform.Translate(Vector3.forward * _runtimeStats.movementSpeed * Time.deltaTime);

        // Optional: Rotate the fallback cube for visual interest if no custom prefab was used.
        if (_instantiatedCharacterModel != null && CharacterData.characterPrefab == null)
        {
            _instantiatedCharacterModel.transform.Rotate(Vector3.up, 50f * Time.deltaTime);
        }
    }

    // --- Core Data-Driven Initialization Logic ---

    /// <summary>
    /// Initializes or re-initializes the character based on the assigned CharacterData.
    /// This is the heart of the Data-Driven Design pattern for this example.
    /// All character properties and initial setup are derived from the CharacterData asset.
    /// </summary>
    public void InitializeCharacter()
    {
        if (CharacterData == null)
        {
            Debug.LogError($"<color=red>[{gameObject.name}]</color> CharacterManager has no CharacterData assigned! Please assign a ScriptableObject.", this);
            return;
        }

        // Clean up previous setup if we are re-initializing
        if (_isInitialized)
        {
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> Re-initializing character from new data.");
            if (_instantiatedCharacterModel != null)
            {
                Destroy(_instantiatedCharacterModel);
                _instantiatedCharacterModel = null;
            }
            _runtimeAbilities.Clear();
        }

        Debug.Log($"<color=lime>Initializing Character: {CharacterData.characterName}</color>");

        // 1. Set the GameObject's name based on CharacterData.
        // This is a simple example of data driving a Unity property.
        gameObject.name = CharacterData.characterName;

        // 2. Instantiate the visual model defined in CharacterData.
        // If a prefab is provided, use it; otherwise, create a simple fallback.
        if (CharacterData.characterPrefab != null)
        {
            _instantiatedCharacterModel = Instantiate(CharacterData.characterPrefab, transform);
            _instantiatedCharacterModel.name = $"{CharacterData.characterName}_Model";
            _instantiatedCharacterModel.transform.localPosition = Vector3.zero; // Ensure it's at the parent's origin
            Debug.Log($"   Spawned model '{_instantiatedCharacterModel.name}' for {CharacterData.characterName}.");
        }
        else
        {
            Debug.LogWarning($"   No character prefab assigned for <color=yellow>{CharacterData.characterName}</color>. Creating a simple cube as fallback.", this);
            _instantiatedCharacterModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _instantiatedCharacterModel.transform.SetParent(transform);
            _instantiatedCharacterModel.transform.localPosition = Vector3.zero;
            _instantiatedCharacterModel.name = $"{CharacterData.characterName}_FallbackCube";
            // Make the fallback cube distinctive for better visualization
            Renderer renderer = _instantiatedCharacterModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material newMat = new Material(Shader.Find("Standard"));
                newMat.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f); // Random vibrant color
                renderer.material = newMat;
            }
        }

        // 3. Set runtime stats from CharacterData.
        // We copy the struct to _runtimeStats. If we were dealing with classes here,
        // we might clone them to avoid modifying the ScriptableObject directly.
        _runtimeStats = CharacterData.baseStats;

        // 4. Populate runtime abilities list from CharacterData.
        // We add references to the AbilityData assets.
        foreach (var ability in CharacterData.abilities)
        {
            _runtimeAbilities.Add(ability);
        }

        Debug.Log($"   Character '{CharacterData.characterName}' initialized with base stats:");
        _runtimeStats.DisplayStats(); // Display stats using the struct's method
        Debug.Log($"   Current Movement Speed: <color=blue>{_runtimeStats.movementSpeed}</color> units/sec.");

        _isInitialized = true;
    }

    /// <summary>
    /// Simulates the character using its first ability.
    /// The specific ability and its effect are determined by the AbilityData.
    /// </summary>
    private void SimulateAbilityUsage()
    {
        if (_runtimeAbilities.Count > 0)
        {
            AbilityData firstAbility = _runtimeAbilities[0];
            // For a simple demo, the character targets itself.
            // In a real game, you would have targeting logic to select another CharacterManager.
            firstAbility.UseAbility(this, this); // 'this' as both user and target for simplicity
        }
    }

    // --- Public Access for Data ---

    /// <summary>
    /// Provides public access to the CharacterData asset currently driving this manager.
    /// </summary>
    /// <returns>The CharacterData ScriptableObject.</returns>
    public CharacterData GetCharacterData()
    {
        return CharacterData;
    }

    /// <summary>
    /// Provides public access to the character's current runtime stats.
    /// </summary>
    /// <returns>The CharacterStats struct.</returns>
    public CharacterStats GetRuntimeStats()
    {
        return _runtimeStats;
    }

    // You could add other methods here (e.g., TakeDamage(int amount), GainExperience(int xp))
    // whose internal logic might also be influenced by CharacterData properties (e.g., defense, maxHealth).
}
```