// Unity Design Pattern Example: BlueprintsForPrefabs
// This script demonstrates the BlueprintsForPrefabs pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Blueprints for Prefabs** design pattern in Unity, which separates an object's configuration data (the "blueprint") from its generic visual and logical structure (the "prefab"). This allows you to create many variations of an object using a single base prefab, configured by different ScriptableObject blueprints.

**Real-world use case:** Creating various types of enemy units (e.g., Archer, Warrior, Mage) from a single base `Enemy` prefab, each with unique stats, appearance, and abilities defined by their respective blueprints.

---

### Understanding the Blueprints for Prefabs Pattern:

1.  **The Blueprint (`ScriptableObject`):**
    *   This is a `ScriptableObject` asset that holds all the customizable data for a specific *type* of object.
    *   It contains properties like stats (health, damage), visual modifiers (color, texture), behavior flags, etc.
    *   You create multiple instances of this ScriptableObject in your project (e.g., "ArcherBlueprint", "WarriorBlueprint"), each defining a unique configuration.

2.  **The Generic Prefab (`GameObject` with `MonoBehaviour`):**
    *   This is a standard Unity prefab (e.g., a simple capsule or a rigged character model).
    *   It has a `MonoBehaviour` component (e.g., `Minion.cs` in our example) attached to its root.
    *   This `MonoBehaviour` has an `Initialize()` method that takes a Blueprint ScriptableObject as an argument.
    *   The prefab itself holds the *common* structure, components (e.g., Rigidbody, Collider, Animator), and base scripts.

3.  **The Spawner/Factory (`MonoBehaviour`):**
    *   This is a `MonoBehaviour` that holds a reference to the generic prefab and an array/list of different Blueprint ScriptableObjects.
    *   When it needs to create an object, it:
        *   Instantiates the generic prefab.
        *   Retrieves the relevant `MonoBehaviour` component from the instantiated object.
        *   Calls the `Initialize()` method on that component, passing the desired Blueprint ScriptableObject.
        *   The `Initialize()` method then applies the data from the blueprint to the instantiated object, customizing its stats, appearance, and behavior.

**Benefits:**
*   **Reduced Prefab Count:** Instead of creating a new prefab for every unit type, you have one generic prefab and many data-only Blueprint assets.
*   **Easier Data Management:** All specific unit configurations are in easily editable ScriptableObject assets, separate from the GameObject hierarchy.
*   **Flexible Creation:** Easily create new unit types by simply creating a new Blueprint asset.
*   **Clear Separation of Concerns:** Data is separate from presentation and logic.

---

### Project Setup and Usage:

1.  **Create C# Scripts:**
    *   Create three new C# scripts in your Unity project: `MinionBlueprint.cs`, `Minion.cs`, and `MinionSpawner.cs`.
    *   Copy and paste the code below into their respective files.

2.  **Create a Base Minion Prefab:**
    *   In the Unity Editor, go to `GameObject -> 3D Object -> Capsule` (or Cube/Sphere).
    *   Rename it to `Minion_Base`.
    *   Drag the `Minion.cs` script onto this `Minion_Base` GameObject.
    *   Drag the `Minion_Base` GameObject from the Hierarchy into your Project window (e.g., into an `Assets/Prefabs` folder) to create a prefab.
    *   Delete the `Minion_Base` GameObject from the Hierarchy (we only need the prefab now).

3.  **Create Minion Blueprint ScriptableObjects:**
    *   In your Project window, right-click -> `Create -> Blueprints -> Minion Blueprint`.
    *   Create at least three different blueprints, for example:
        *   **ArcherBlueprint:**
            *   Name: Archer
            *   Health: 80
            *   Attack Damage: 15
            *   Movement Speed: 4.5
            *   Minion Color: Yellow
        *   **WarriorBlueprint:**
            *   Name: Warrior
            *   Health: 150
            *   Attack Damage: 25
            *   Movement Speed: 3.0
            *   Minion Color: Red
        *   **MageBlueprint:**
            *   Name: Mage
            *   Health: 90
            *   Attack Damage: 10
            *   Movement Speed: 4.0
            *   Minion Color: Blue

4.  **Create a Minion Spawner:**
    *   Create an empty GameObject in your scene (`GameObject -> Create Empty`).
    *   Rename it to `MinionSpawner_Manager`.
    *   Drag the `MinionSpawner.cs` script onto this `MinionSpawner_Manager` GameObject.
    *   In the Inspector for `MinionSpawner_Manager`:
        *   Drag your `Minion_Base` prefab from your Project window into the "Minion Prefab" slot.
        *   Increase the size of the "Available Blueprints" array (e.g., to 3).
        *   Drag your `ArcherBlueprint`, `WarriorBlueprint`, and `MageBlueprint` assets from your Project window into the respective slots of the "Available Blueprints" array.

5.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   The `MinionSpawner_Manager` will automatically spawn one of each minion type, each configured by its specific blueprint.
    *   Observe how each minion has different stats (visible in the Inspector if you select them) and a different color, all originating from the *same base prefab*.

---

### C# Scripts:

#### 1. `MinionBlueprint.cs` (The Blueprint - `ScriptableObject`)

```csharp
using UnityEngine;

/// <summary>
/// MinionBlueprint is a ScriptableObject that defines the specific configuration
/// for a type of Minion. This acts as the "blueprint" for our generic Minion prefab.
///
/// By using ScriptableObjects, we can create multiple data assets in the Project window
/// (e.g., ArcherBlueprint, WarriorBlueprint, MageBlueprint), each customizing the
/// Minion's properties without needing to create separate prefabs.
/// </summary>
[CreateAssetMenu(fileName = "NewMinionBlueprint", menuName = "Blueprints/Minion Blueprint")]
public class MinionBlueprint : ScriptableObject
{
    [Header("Minion Core Properties")]
    public string minionName = "Default Minion";
    public int health = 100;
    public float attackDamage = 10f;
    public float movementSpeed = 3f;

    [Header("Visual Properties")]
    // The color applied to the minion's renderer
    public Color minionColor = Color.grey; 

    [Header("Behavioral Properties (Example)")]
    // Example: A list of abilities, could be further ScriptableObjects for complex abilities
    public string[] abilities = { "Basic Attack" };

    /// <summary>
    /// This method can be used to log the blueprint's properties for debugging.
    /// </summary>
    public void LogBlueprintDetails()
    {
        Debug.Log($"--- Blueprint Details: {minionName} ---");
        Debug.Log($"  Health: {health}");
        Debug.Log($"  Attack Damage: {attackDamage}");
        Debug.Log($"  Movement Speed: {movementSpeed}");
        Debug.Log($"  Color: {minionColor}");
        Debug.Log($"  Abilities: {string.Join(", ", abilities)}");
    }
}
```

#### 2. `Minion.cs` (The Generic Prefab's Component - `MonoBehaviour`)

```csharp
using UnityEngine;

/// <summary>
/// Minion is a MonoBehaviour component that resides on the generic Minion prefab.
/// It holds the actual runtime stats and appearance of an instantiated Minion.
///
/// The Minion's properties are initialized by a MinionBlueprint via the Initialize() method.
/// This component represents the "template" that gets filled with specific data.
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensure the prefab has a Renderer to change color
public class Minion : MonoBehaviour
{
    [Header("Current Minion Stats")]
    [SerializeField] private string _minionName;
    [SerializeField] private int _health;
    [SerializeField] private float _attackDamage;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private Color _currentMinionColor;

    [Header("Minion Components")]
    private Renderer _minionRenderer;
    private MaterialPropertyBlock _propertyBlock; // Used for efficient material property changes

    // Public properties to access current stats (read-only)
    public string MinionName => _minionName;
    public int Health => _health;
    public float AttackDamage => _attackDamage;
    public float MovementSpeed => _movementSpeed;
    public Color CurrentMinionColor => _currentMinionColor;

    void Awake()
    {
        _minionRenderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Initializes this Minion instance with data from a provided MinionBlueprint.
    /// This is the core method demonstrating how the blueprint configures the prefab.
    /// </summary>
    /// <param name="blueprint">The MinionBlueprint to apply.</param>
    public void Initialize(MinionBlueprint blueprint)
    {
        // 1. Apply core properties from the blueprint
        _minionName = blueprint.minionName;
        _health = blueprint.health;
        _attackDamage = blueprint.attackDamage;
        _movementSpeed = blueprint.movementSpeed;
        _currentMinionColor = blueprint.minionColor;

        // 2. Apply visual properties (e.g., color)
        // Using MaterialPropertyBlock is more efficient than modifying renderer.material
        // directly, as it avoids creating a new material instance for each object.
        if (_minionRenderer != null && _propertyBlock != null)
        {
            _minionRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", blueprint.minionColor); // Assuming standard shader's _Color property
            _minionRenderer.SetPropertyBlock(_propertyBlock);
        }
        else
        {
            Debug.LogWarning("Minion Renderer or MaterialPropertyBlock not found on " + gameObject.name, this);
        }

        // 3. Apply behavioral properties (example: log abilities)
        // In a real game, this might involve adding specific ability components,
        // setting up AI behaviors, etc.
        Debug.Log($"{_minionName} initialized with abilities: {string.Join(", ", blueprint.abilities)}");

        Debug.Log($"Minion '{_minionName}' created. Health: {_health}, Attack: {_attackDamage}, Speed: {_movementSpeed}.");
    }

    /// <summary>
    /// Example method for Minion behavior.
    /// </summary>
    public void TakeDamage(int damage)
    {
        _health -= damage;
        Debug.Log($"{_minionName} took {damage} damage. Remaining health: {_health}");
        if (_health <= 0)
        {
            Debug.Log($"{_minionName} has been defeated!");
            Destroy(gameObject);
        }
    }
}
```

#### 3. `MinionSpawner.cs` (The Spawner/Factory - `MonoBehaviour`)

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MinionSpawner is a MonoBehaviour that demonstrates how to use MinionBlueprints
/// to instantiate and configure generic Minion prefabs.
///
/// This acts as a factory, taking a base prefab and a blueprint, and combining them
/// to create a fully configured game object.
/// </summary>
public class MinionSpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [Tooltip("The generic Minion prefab that will be instantiated.")]
    [SerializeField] private GameObject _minionPrefab;

    [Tooltip("A list of MinionBlueprint ScriptableObjects to choose from when spawning.")]
    [SerializeField] private List<MinionBlueprint> _availableBlueprints;

    [Tooltip("The spacing between spawned minions.")]
    [SerializeField] private float _spawnSpacing = 2.5f;

    void Start()
    {
        // Demonstrate spawning all available minion types
        SpawnAllMinionTypes();
    }

    /// <summary>
    /// Spawns a new minion based on the provided blueprint.
    /// This is the core of the Blueprints for Prefabs pattern.
    /// </summary>
    /// <param name="blueprint">The MinionBlueprint to use for configuration.</param>
    /// <param name="spawnPosition">The position where the minion should be spawned.</param>
    /// <returns>The instantiated and configured GameObject of the minion.</returns>
    public GameObject SpawnMinion(MinionBlueprint blueprint, Vector3 spawnPosition)
    {
        if (_minionPrefab == null)
        {
            Debug.LogError("Minion Prefab is not assigned to the MinionSpawner!", this);
            return null;
        }

        if (blueprint == null)
        {
            Debug.LogError("Attempted to spawn minion with a null blueprint!", this);
            return null;
        }

        // 1. Instantiate the generic prefab
        GameObject newMinionGO = Instantiate(_minionPrefab, spawnPosition, Quaternion.identity);
        newMinionGO.name = $"{blueprint.minionName}_Instance"; // Give it a descriptive name

        // 2. Get the Minion component from the instantiated object
        Minion minionComponent = newMinionGO.GetComponent<Minion>();

        if (minionComponent != null)
        {
            // 3. Initialize the Minion component with the blueprint's data
            minionComponent.Initialize(blueprint);
        }
        else
        {
            Debug.LogError("Minion prefab does not have a Minion component!", _minionPrefab);
            Destroy(newMinionGO); // Clean up if initialization fails
            return null;
        }

        Debug.Log($"Spawned {minionComponent.MinionName} at {spawnPosition}");
        return newMinionGO;
    }

    /// <summary>
    /// Spawns one of each minion type defined in the available blueprints list.
    /// This method is for demonstration purposes.
    /// </summary>
    [ContextMenu("Spawn All Minion Types")] // Allows calling from Editor right-click menu
    public void SpawnAllMinionTypes()
    {
        if (_availableBlueprints == null || _availableBlueprints.Count == 0)
        {
            Debug.LogWarning("No Minion Blueprints assigned to the spawner.", this);
            return;
        }

        Debug.Log("Spawning all available minion types...");
        for (int i = 0; i < _availableBlueprints.Count; i++)
        {
            MinionBlueprint blueprint = _availableBlueprints[i];
            // Calculate a position for each minion to avoid overlapping
            Vector3 spawnPos = transform.position + new Vector3(i * _spawnSpacing, 0, 0);
            SpawnMinion(blueprint, spawnPos);
        }
    }

    /// <summary>
    /// Example of spawning a random minion.
    /// </summary>
    [ContextMenu("Spawn Random Minion")]
    public void SpawnRandomMinion()
    {
        if (_availableBlueprints == null || _availableBlueprints.Count == 0)
        {
            Debug.LogWarning("No Minion Blueprints assigned to the spawner.", this);
            return;
        }

        int randomIndex = Random.Range(0, _availableBlueprints.Count);
        MinionBlueprint randomBlueprint = _availableBlueprints[randomIndex];
        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        SpawnMinion(randomBlueprint, spawnPos);
    }
}
```