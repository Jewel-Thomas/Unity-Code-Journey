// Unity Design Pattern Example: Prototype
// This script demonstrates the Prototype pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Prototype' design pattern is a creational pattern that allows you to create new objects by copying an existing object, known as the prototype, rather than creating new objects from scratch. This pattern is particularly useful when the cost of creating a new object is expensive, or when you want to create many instances of an object that share similar characteristics but might have slight variations.

In Unity, the Prototype pattern is very practical for:
*   **Spawning enemies or items:** Instead of instantiating a prefab and then manually setting its properties (health, speed, color) every time, you can pre-configure several "prototype" GameObjects in your scene or as prefabs, and then simply "clone" them when needed.
*   **Object Pooling:** Prototypes can be used to generate the initial set of objects in a pool.
*   **Variations of objects:** Easily create different variations of a base object (e.g., "Basic Orc," "Strong Orc," "Fast Orc") without complex subclassing hierarchies.

---

### **Prototype Pattern Example in Unity**

This example demonstrates how to use the Prototype pattern to spawn various types of monsters (Orcs, Goblins) with pre-defined characteristics (health, speed, color, damage).

**Key Components:**

1.  **`IPrototypeMonster` (Prototype Interface):** Defines the `Clone()` method that all clonable monsters must implement.
2.  **`MonsterPrototype` (Concrete Prototype):** A `MonoBehaviour` that acts as the blueprint for creating new monsters. It holds the base properties for a specific monster type. When its `Clone()` method is called, it instantiates a new `GameObject`, attaches a `MonsterRuntime` component, and copies its properties to that component.
3.  **`MonsterRuntime` (Cloned Object):** A `MonoBehaviour` that represents an *actual* monster instance in the scene. It holds the unique state of the monster and its behavior.
4.  **`PrototypeSpawner` (Client):** A `MonoBehaviour` that holds references to different `MonsterPrototype` objects and uses their `Clone()` method to create new monster instances. It doesn't need to know the concrete class of the monster it's creating, only that it can be cloned.

---

```csharp
// File: Assets/Scripts/PrototypePatternExample.cs

using UnityEngine;
using System.Collections.Generic; // For List<T>
using System; // For Guid (to generate unique names)

// --- 1. The Prototype Interface ---
// This interface defines the contract for objects that can be cloned.
// In Unity, we typically clone GameObjects or components that represent entities.
// The Clone method will return a new GameObject configured based on the prototype.
public interface IPrototypeMonster
{
    // The Clone method returns a new GameObject that is a copy of the current prototype's configuration.
    GameObject Clone();
}

// --- 2. The Concrete Prototype ---
// This class serves as the blueprint (the prototype itself).
// Instead of directly cloning this MonoBehaviour instance, this MonoBehaviour *is* the prototype.
// When its Clone() method is called, it instantiates a *new GameObject* and initializes it
// with a 'MonsterRuntime' component based on its own properties.
// This is crucial because MonoBehaviours cannot be instantiated directly with 'new' in Unity;
// they must be attached to GameObjects in the scene or instantiated from prefabs.
public class MonsterPrototype : MonoBehaviour, IPrototypeMonster
{
    [Header("Prototype Properties")]
    [Tooltip("The base health for monsters cloned from this prototype.")]
    public int baseHealth = 100;
    [Tooltip("The base movement speed for monsters cloned from this prototype.")]
    public float baseSpeed = 5.0f;
    [Tooltip("The base color for monsters cloned from this prototype.")]
    public Color baseColor = Color.red;
    [Tooltip("The base damage value for monsters cloned from this prototype.")]
    public int baseDamage = 10;
    [Tooltip("The prefab to use for the visual representation of the cloned monster. E.g., a simple Sphere or Cube.")]
    public GameObject monsterVisualPrefab;

    // An internal identifier for this specific prototype type (useful for logging/debug).
    [Tooltip("An identifier for this specific prototype type (e.g., 'Orc', 'Goblin').")]
    public string prototypeName = "GenericMonster";

    // Implement the Clone method from IPrototypeMonster.
    // This method contains the specific logic for creating a new object instance
    // that is a copy of this prototype.
    public GameObject Clone()
    {
        // --- Step 1: Validate the visual prefab. ---
        if (monsterVisualPrefab == null)
        {
            Debug.LogError($"MonsterPrototype '{prototypeName}' is missing a 'Monster Visual Prefab'. Cannot clone.", this);
            return null;
        }

        // --- Step 2: Instantiate a new GameObject from the visual prefab. ---
        // This creates a completely new entity in the scene.
        GameObject clonedMonsterGO = Instantiate(monsterVisualPrefab);
        // Give it a unique name for easier identification in the Hierarchy.
        clonedMonsterGO.name = $"{prototypeName}_Clone_{Guid.NewGuid().ToString().Substring(0, 4)}";

        // --- Step 3: Add the runtime component to the new GameObject. ---
        // This component will hold the actual state and behavior of the spawned monster.
        // We separate this from the Prototype itself because the Prototype acts purely as a blueprint.
        MonsterRuntime runtimeMonster = clonedMonsterGO.AddComponent<MonsterRuntime>();

        // --- Step 4: Copy properties from this prototype to the new runtime component. ---
        // This is where the "cloning" of the *state* happens.
        // For value types (int, float, Color), this is inherently a deep copy.
        // For complex reference types (e.g., custom classes, arrays), you would need to
        // implement explicit deep-copying logic here if each cloned object needs its own
        // independent instance of those reference types.
        runtimeMonster.currentHealth = baseHealth;
        runtimeMonster.currentSpeed = baseSpeed;
        runtimeMonster.monsterColor = baseColor;
        runtimeMonster.damage = baseDamage;
        runtimeMonster.sourcePrototypeName = prototypeName; // Keep track of its origin

        // --- Step 5: Configure visual aspects if the prefab has a Renderer. ---
        // This ensures the cloned monster reflects the prototype's color.
        Renderer renderer = clonedMonsterGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            // IMPORTANT: Create a *new material instance* for the cloned object.
            // If you directly modify `renderer.sharedMaterial`, you will change the material
            // for ALL objects that share that material (including the original prefab).
            Material newMat = new Material(renderer.sharedMaterial);
            newMat.color = baseColor;
            renderer.material = newMat;
        }

        Debug.Log($"PROTOTYPE: Cloned '{prototypeName}' to '{clonedMonsterGO.name}' with Health: {runtimeMonster.currentHealth}, Speed: {runtimeMonster.currentSpeed}, Color: {runtimeMonster.monsterColor}");

        return clonedMonsterGO; // Return the newly created GameObject
    }
}

// --- 3. The Runtime Component for Cloned Objects ---
// This script is attached to the GameObjects created by the cloning process.
// It represents an actual, active instance of a monster in the game world,
// carrying its current state and defining its dynamic behavior.
public class MonsterRuntime : MonoBehaviour
{
    [Header("Current Monster State")]
    public int currentHealth;
    public float currentSpeed;
    public Color monsterColor;
    public int damage;
    public string sourcePrototypeName; // To identify which prototype created this instance

    // Simple movement variables for demonstration
    private Vector3 targetPosition;
    private float moveTimer;
    private float moveDuration = 3f; // How long to move towards a target before picking a new one

    void Start()
    {
        // Example: Log initial state and set up initial behavior.
        Debug.Log($"RUNTIME: Monster '{gameObject.name}' (from '{sourcePrototypeName}') spawned! Health: {currentHealth}, Speed: {currentSpeed}, Color: {monsterColor}");

        // Set an initial random target for movement
        SetRandomTargetPosition();
    }

    void Update()
    {
        // Simple movement logic: move towards a target, then pick a new one.
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || moveTimer <= 0)
        {
            SetRandomTargetPosition();
            moveTimer = moveDuration;
        }
        moveTimer -= Time.deltaTime;
    }

    // Assigns a random position within a range for the monster to move towards.
    void SetRandomTargetPosition()
    {
        float range = 10f; // Defines the movement area
        targetPosition = new Vector3(
            UnityEngine.Random.Range(-range, range),
            transform.position.y, // Keep Y constant for movement on the XZ plane (like a ground unit)
            UnityEngine.Random.Range(-range, range)
        );
    }

    // Example method: Allows this monster instance to take damage.
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Example method: Handles the monster's death.
    void Die()
    {
        Debug.Log($"{gameObject.name} (from '{sourcePrototypeName}') has been defeated!");
        Destroy(gameObject); // Remove the monster from the scene
    }
}

// --- 4. The Client (Spawner) ---
// This class uses the MonsterPrototypes to create new monster instances.
// It acts as the "client" in the Prototype pattern, requesting new objects
// by calling the Clone() method on its prototypes. It doesn't need to know
// the internal details of how each monster type is created.
public class PrototypeSpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [Tooltip("Drag your MonsterPrototype GameObjects here from the scene or Project window. " +
             "These are your pre-configured monster blueprints.")]
    public List<MonsterPrototype> monsterPrototypes;

    [Tooltip("The number of monsters to spawn for each assigned prototype.")]
    public int spawnCountPerPrototype = 3;

    [Tooltip("The radius around the spawner where monsters will be randomly positioned.")]
    public float spawnRadius = 5f;

    [Tooltip("Press this key to trigger the monster spawning.")]
    public KeyCode spawnKey = KeyCode.Space;

    void Start()
    {
        if (monsterPrototypes == null || monsterPrototypes.Count == 0)
        {
            Debug.LogWarning("No Monster Prototypes assigned to the Spawner. " +
                             "Please assign them in the Inspector to see the pattern in action.", this);
        }
        else
        {
            Debug.Log($"Prototype Spawner initialized. Press '{spawnKey}' to spawn monsters.");
        }
    }

    void Update()
    {
        // Trigger spawning when the designated key is pressed.
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnMonsters();
        }
    }

    // The core method that demonstrates the Prototype pattern in action.
    void SpawnMonsters()
    {
        if (monsterPrototypes == null || monsterPrototypes.Count == 0)
        {
            Debug.LogWarning("Cannot spawn monsters: No prototypes are assigned to the Spawner.", this);
            return;
        }

        Debug.Log($"SPAWNER: Attempting to spawn {spawnCountPerPrototype} monsters for each of {monsterPrototypes.Count} prototypes...");

        // Iterate through each prototype we have configured.
        foreach (MonsterPrototype prototype in monsterPrototypes)
        {
            if (prototype == null)
            {
                Debug.LogWarning("A null prototype reference was found in the list. Skipping it.", this);
                continue;
            }

            // For each prototype, spawn the desired number of copies.
            for (int i = 0; i < spawnCountPerPrototype; i++)
            {
                // This is where the Prototype pattern truly shines:
                // The client (PrototypeSpawner) calls Clone() on the prototype.
                // It doesn't know *how* the monster is created, only that it gets a new instance.
                GameObject newMonsterGO = prototype.Clone();

                if (newMonsterGO != null)
                {
                    // Position the new monster randomly around the spawner's location.
                    Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * spawnRadius;
                    randomOffset.y = 0; // Keep monsters on the ground plane for this example
                    newMonsterGO.transform.position = transform.position + randomOffset;
                    Debug.Log($"SPAWNER: Successfully spawned monster '{newMonsterGO.name}' from prototype '{prototype.prototypeName}' at {newMonsterGO.transform.position}");
                }
            }
        }
        Debug.Log("SPAWNER: Monster spawning complete.");
    }
}

/*
--- How to Use This Prototype Pattern Example in Unity ---

1.  **Create a New Unity Project** (or open an existing one).

2.  **Create the C# Script:**
    *   In your Project window, create a new C# script named `PrototypePatternExample`.
    *   Copy and paste the entire code above into this script.

3.  **Create a Monster Visual Prefab:**
    *   This will be the base visual representation for all your monsters.
    *   In your Project window (e.g., in a "Prefabs" folder), right-click -> `3D Object` -> `Sphere` (or Cube, Capsule).
    *   Rename it to `MonsterVisualPrefab`.
    *   (Optional but Recommended) Create a new Material (e.g., "DefaultMonsterMaterial"), give it a base color like gray, and apply it to `MonsterVisualPrefab`'s Renderer component. This ensures that when the `MonsterRuntime` script sets a new color, it's modifying a specific material instance.
    *   Drag this `MonsterVisualPrefab` from the Hierarchy into your Project window to turn it into a reusable prefab.
    *   You can delete the `MonsterVisualPrefab` from your Hierarchy now.

4.  **Set Up Monster Prototypes:**
    *   These will be your blueprint GameObjects. You can create them directly in the scene or as prefabs.
    *   **Create Orc Prototype:**
        *   In your Hierarchy, right-click -> `Create Empty`. Rename it `OrcPrototype`.
        *   Attach the `MonsterPrototype` script to `OrcPrototype`.
        *   In the Inspector for `OrcPrototype`:
            *   Set `Base Health`: `120`
            *   Set `Base Speed`: `6.0`
            *   Set `Base Color`: Choose a distinct color, like **Green**.
            *   Set `Base Damage`: `15`
            *   Set `Prototype Name`: `Orc`
            *   **Drag your `MonsterVisualPrefab` from the Project window into the `Monster Visual Prefab` slot.**
    *   **Create Goblin Prototype:**
        *   In your Hierarchy, right-click -> `Create Empty`. Rename it `GoblinPrototype`.
        *   Attach the `MonsterPrototype` script to `GoblinPrototype`.
        *   In the Inspector for `GoblinPrototype`:
            *   Set `Base Health`: `80`
            *   Set `Base Speed`: `8.0`
            *   Set `Base Color`: Choose another distinct color, like **Yellow/Brown**.
            *   Set `Base Damage`: `8`
            *   Set `Prototype Name`: `Goblin`
            *   **Drag your `MonsterVisualPrefab` into the `Monster Visual Prefab` slot.**
    *   (Optional but Recommended): Drag your `OrcPrototype` and `GoblinPrototype` from the Hierarchy into your Project window (e.g., into the "Prefabs" folder) to create prefab versions of your prototypes. You can then delete them from the Hierarchy if you wish; the Spawner can reference them from the Project window.

5.  **Set Up the Spawner:**
    *   In your Hierarchy, right-click -> `Create Empty`. Rename it `MonsterSpawner`.
    *   Attach the `PrototypeSpawner` script to `MonsterSpawner`.
    *   In the Inspector for `MonsterSpawner`:
        *   Expand the `Monster Prototypes` list.
        *   Set `Size` to `2`.
        *   **Drag your `OrcPrototype` and `GoblinPrototype` (from either the Hierarchy or your Project Prefabs folder) into the Element 0 and Element 1 slots respectively.**
        *   Set `Spawn Count Per Prototype`: `3` (or any desired number).
        *   Adjust `Spawn Radius` if you want monsters to appear closer or further from the spawner.
        *   Leave `Spawn Key` as `Space` or change it.

6.  **Run the Scene:**
    *   Press the **Play** button in Unity.
    *   Observe the Console for initial messages from the spawner.
    *   Press the `Spacebar` (or whatever `spawnKey` you chose).
    *   You should see multiple `Orc` and `Goblin` monsters appear in your scene, each colored and with the properties defined by their respective prototypes. They will start to move randomly as per their `MonsterRuntime` script.
    *   Each spawned monster is a unique instance in your scene, but they were all efficiently created by "cloning" the pre-configured prototype GameObjects.

This setup clearly illustrates how the Prototype pattern enables you to define templates for objects and then easily generate many instances with varying initial configurations, without tightly coupling your client (the spawner) to the concrete construction logic of each object type.