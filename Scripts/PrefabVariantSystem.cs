// Unity Design Pattern Example: PrefabVariantSystem
// This script demonstrates the PrefabVariantSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PrefabVariantSystem' design pattern in Unity allows developers to define a base prefab with a set of default properties, and then create "variants" that inherit from this base but can override specific properties. This is incredibly powerful for managing assets where you have many similar objects that differ only in a few attributes (e.g., different types of enemies, collectibles, power-ups, building modules).

Instead of creating entirely new prefabs for each variant (which leads to duplicated data and makes global changes difficult), this system uses ScriptableObjects to define variant configurations that are applied at runtime to instances of a *single base prefab*.

### Core Idea of the PrefabVariantSystem:

1.  **Base Prefab:** A standard Unity Prefab that has a `MonoBehaviour` script (e.g., `Collectible.cs`) which exposes properties that can be configured. This prefab represents the fundamental visual and interactive component.
2.  **Base Configuration (ScriptableObject):** A `ScriptableObject` (e.g., `CollectibleConfig.cs`) that holds the default values for the base prefab's properties. It also holds a reference to the base prefab itself.
3.  **Variant Configuration (ScriptableObject):** Another `ScriptableObject` (e.g., `CollectibleVariantConfig.cs`) that references a `Base Configuration`. For each property, it provides an option to either *use the value from the Base Configuration* or *override it with a new value*.
4.  **Spawner/Factory (MonoBehaviour):** A runtime script (e.g., `CollectibleSpawner.cs`) that takes a `Variant Configuration`, instantiates the base prefab (from the referenced Base Config), and then initializes the instantiated object using the *effective* properties from the Variant Configuration (applying overrides where specified).

---

### Example Use Case: Different Types of Collectibles

Imagine you have coins, gems, health potions, and power-ups. They all might:
*   Have a value.
*   Play a sound when collected.
*   Show a particle effect when collected.
*   Despawn after a certain time.

Using the PrefabVariantSystem, you would:
*   Create a single "Generic Collectible" prefab.
*   Create a `BaseCollectible_Config` ScriptableObject for its default properties.
*   Create `Variant_Coin`, `Variant_Gem`, `Variant_Potion` ScriptableObjects, each referencing `BaseCollectible_Config` and overriding only the properties that are different (e.g., `Value`, `DespawnDuration`).

This approach simplifies asset management, reduces redundancy, and makes it easy to introduce new variants or modify base properties globally.

---

### Complete C# Unity Example

Below are the C# scripts that implement the PrefabVariantSystem for collectibles.

**1. `OverrideProperty.cs`**
*   A generic helper struct to make managing overrideable properties cleaner in ScriptableObjects.

```csharp
using UnityEngine;
using System;

/// <summary>
/// A generic helper struct to manage properties that can either use a base value
/// or explicitly override it. This makes the Inspector clean and explicit for variants.
/// </summary>
/// <typeparam name="T">The type of the property being overridden (e.g., int, AudioClip, GameObject).</typeparam>
[Serializable]
public struct OverrideProperty<T>
{
    [Tooltip("Check this box to override the base configuration's value.")]
    public bool Override;

    [Tooltip("The value to use if 'Override' is checked.")]
    public T Value;

    /// <summary>
    /// Returns the effective value for this property. If 'Override' is true, it returns 'Value',
    /// otherwise, it returns the provided 'baseValue'.
    /// </summary>
    /// <param name="baseValue">The value from the base configuration.</param>
    /// <returns>The overridden value or the base value.</returns>
    public T GetEffectiveValue(T baseValue)
    {
        return Override ? Value : baseValue;
    }
}
```

---

**2. `CollectibleConfig.cs`**
*   The `ScriptableObject` representing the base configuration for all collectibles.

```csharp
using UnityEngine;

/// <summary>
/// Base configuration for all collectibles. This ScriptableObject defines the common
/// properties and a reference to the core Collectible Prefab.
/// All variants will ultimately derive properties from this base or override them.
/// </summary>
[CreateAssetMenu(fileName = "NewCollectibleConfig", menuName = "Collectible/Base Config")]
public class CollectibleConfig : ScriptableObject
{
    [Header("Base Collectible Properties")]
    [Tooltip("The actual GameObject prefab that will be instantiated. This prefab must have a 'Collectible' script.")]
    public GameObject CollectiblePrefab;

    [Tooltip("The default value (e.g., score, health points) gained when collected.")]
    public int Value = 1;

    [Tooltip("The default sound effect played when this collectible is gathered.")]
    public AudioClip CollectSound;

    [Tooltip("The default visual effect (e.g., particle system) instantiated when collected.")]
    public GameObject CollectEffectPrefab;

    [Tooltip("Default duration (in seconds) for which the collectible remains active before despawning. Set to 0 for infinite.")]
    public float DespawnDuration = 0f;

    [Tooltip("Default name for the collectible, useful for debugging or UI.")]
    public string CollectibleName = "Generic Collectible";

    // Add any other common properties here, e.g., Mesh, Material, etc.
}
```

---

**3. `CollectibleVariantConfig.cs`**
*   The `ScriptableObject` representing a specific variant. It references a `CollectibleConfig` and allows overriding its properties.

```csharp
using UnityEngine;

/// <summary>
/// A variant configuration for a collectible. This ScriptableObject references a
/// base CollectibleConfig and allows overriding specific properties.
/// If a property is not overridden, it defaults to the value defined in the BaseConfig.
/// </summary>
[CreateAssetMenu(fileName = "NewCollectibleVariant", menuName = "Collectible/Variant Config")]
public class CollectibleVariantConfig : ScriptableObject
{
    [Header("Variant Configuration")]
    [Tooltip("The base configuration from which this variant inherits properties.")]
    public CollectibleConfig BaseConfig;

    [Space(10)]
    [Header("Override Properties (Check 'Override' to apply)")]

    [Tooltip("Overrides the value gained when collected.")]
    public OverrideProperty<int> Value;

    [Tooltip("Overrides the sound effect played when collected.")]
    public OverrideProperty<AudioClip> CollectSound;

    [Tooltip("Overrides the visual effect played when collected.")]
    public OverrideProperty<GameObject> CollectEffectPrefab;

    [Tooltip("Overrides the despawn duration.")]
    public OverrideProperty<float> DespawnDuration;

    [Tooltip("Overrides the name of the collectible.")]
    public OverrideProperty<string> CollectibleName;

    // --- Accessor Methods to get the effective value ---
    // These methods implement the core 'inheritance' logic of the variant system.

    /// <summary>
    /// Returns the Collectible Prefab defined in the BaseConfig.
    /// In this design, the base prefab itself is not typically overridden by variants.
    /// </summary>
    public GameObject GetCollectiblePrefab()
    {
        if (BaseConfig == null)
        {
            Debug.LogError($"CollectibleVariantConfig '{name}' has no BaseConfig assigned! Cannot get prefab.", this);
            return null;
        }
        return BaseConfig.CollectiblePrefab;
    }

    /// <summary>
    /// Gets the effective value for this collectible (variant's value or base value).
    /// </summary>
    public int GetEffectiveValue()
    {
        if (BaseConfig == null) { Debug.LogError($"No BaseConfig assigned to {name}!", this); return default; }
        return Value.GetEffectiveValue(BaseConfig.Value);
    }

    /// <summary>
    /// Gets the effective collect sound for this collectible.
    /// </summary>
    public AudioClip GetEffectiveCollectSound()
    {
        if (BaseConfig == null) { Debug.LogError($"No BaseConfig assigned to {name}!", this); return default; }
        return CollectSound.GetEffectiveValue(BaseConfig.CollectSound);
    }

    /// <summary>
    /// Gets the effective collect effect prefab for this collectible.
    /// </summary>
    public GameObject GetEffectiveCollectEffectPrefab()
    {
        if (BaseConfig == null) { Debug.LogError($"No BaseConfig assigned to {name}!", this); return default; }
        return CollectEffectPrefab.GetEffectiveValue(BaseConfig.CollectEffectPrefab);
    }

    /// <summary>
    /// Gets the effective despawn duration for this collectible.
    /// </summary>
    public float GetEffectiveDespawnDuration()
    {
        if (BaseConfig == null) { Debug.LogError($"No BaseConfig assigned to {name}!", this); return default; }
        return DespawnDuration.GetEffectiveValue(BaseConfig.DespawnDuration);
    }

    /// <summary>
    /// Gets the effective name for this collectible.
    /// </summary>
    public string GetEffectiveCollectibleName()
    {
        if (BaseConfig == null) { Debug.LogError($"No BaseConfig assigned to {name}!", this); return default; }
        return CollectibleName.GetEffectiveValue(BaseConfig.CollectibleName);
    }
}
```

---

**4. `Collectible.cs`**
*   The `MonoBehaviour` script that resides on the actual collectible prefab. It receives and applies the variant configuration at runtime.

```csharp
using UnityEngine;

/// <summary>
/// This MonoBehaviour script is attached to the base collectible prefab.
/// It contains the logic for what happens when a collectible is initialized and gathered.
/// Properties are assigned at runtime by a `CollectibleVariantConfig`.
/// </summary>
[RequireComponent(typeof(Collider))] // Collectibles usually need a collider to be interactive
public class Collectible : MonoBehaviour
{
    [Header("Runtime Collectible Data")]
    // These fields store the *effective* values determined by the variant config.
    [SerializeField] private int _value;
    [SerializeField] private AudioClip _collectSound;
    [SerializeField] private GameObject _collectEffectPrefab;
    [SerializeField] private float _despawnDuration;
    [SerializeField] private string _collectibleName;

    // Cached components for performance
    private AudioSource _audioSource;
    private Collider _collider;
    private Renderer _renderer;

    // Public accessors for important data
    public int Value => _value;
    public string CollectibleName => _collectibleName;

    private void Awake()
    {
        // Ensure the collider is set to trigger for collection interaction
        _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }
        else
        {
            Debug.LogError("Collectible requires a Collider component!", this);
            enabled = false; // Disable if no collider for interaction
        }

        // Add or get an AudioSource component for playing sounds
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false; // Don't play sound automatically on spawn

        _renderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Initializes this collectible instance with properties from a specific variant configuration.
    /// This is where the variant's properties are applied to the instantiated object.
    /// </summary>
    /// <param name="variantConfig">The CollectibleVariantConfig to use for initialization.</param>
    public void Initialize(CollectibleVariantConfig variantConfig)
    {
        if (variantConfig == null)
        {
            Debug.LogError("Collectible initialized with a null variantConfig!", this);
            return;
        }

        // Apply the effective values from the variant configuration
        _value = variantConfig.GetEffectiveValue();
        _collectSound = variantConfig.GetEffectiveCollectSound();
        _collectEffectPrefab = variantConfig.GetEffectiveCollectEffectPrefab();
        _despawnDuration = variantConfig.GetEffectiveDespawnDuration();
        _collectibleName = variantConfig.GetEffectiveCollectibleName();

        // Start a despawn timer if a duration is specified
        if (_despawnDuration > 0)
        {
            Invoke(nameof(Despawn), _despawnDuration);
        }

        // Optional: Update visual representation if needed (e.g., color, mesh).
        // For simplicity, we assume the base prefab's visuals are good,
        // but a more complex system might change materials or models here.
        gameObject.name = _collectibleName; // Rename for easier debugging in hierarchy
        Debug.Log($"Initialized Collectible: '{_collectibleName}' (Value: {_value}, Despawn: {_despawnDuration}s)");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Simple example: Player collects it.
        // Ensure your player object has the tag "Player" and a Collider.
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    /// <summary>
    /// Handles the collection logic when the collectible is interacted with.
    /// </summary>
    public void Collect()
    {
        Debug.Log($"Collected '{_collectibleName}'! Gained {_value}.");

        // Play the collect sound effect
        if (_collectSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_collectSound);
        }

        // Instantiate the visual effect (e.g., particle system)
        if (_collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(_collectEffectPrefab, transform.position, Quaternion.identity);
            // Destroy the effect after its particle system finishes or after a default time
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f); // Default destroy time if no particle system
            }
        }

        // Disable rendering and collision immediately to make it disappear
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;

        // Destroy the GameObject after a short delay to allow sounds/effects to complete
        float destroyDelay = _collectSound != null ? _collectSound.length : 0.5f;
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Called if the despawn duration runs out.
    /// </summary>
    private void Despawn()
    {
        Debug.Log($"Collectible '{_collectibleName}' despawned due to duration expiring.");
        // Optionally play a despawn effect here before destroying
        Destroy(gameObject);
    }

    // You could add other methods here, e.g., for highlighting, hover effects, etc.
}
```

---

**5. `CollectibleSpawner.cs`**
*   A `MonoBehaviour` that demonstrates how to use the `CollectibleVariantConfig` to spawn and initialize collectibles.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script demonstrates how to use the PrefabVariantSystem by spawning
/// collectibles based on a list of `CollectibleVariantConfig` ScriptableObjects.
/// It acts as a factory, instantiating the base prefab and configuring it.
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [Tooltip("List of CollectibleVariantConfigs that this spawner can instantiate.")]
    public List<CollectibleVariantConfig> AvailableVariants;

    [Tooltip("The time delay (in seconds) between successive collectible spawns.")]
    public float SpawnInterval = 2f;

    [Tooltip("The radius around the spawner within which collectibles can appear.")]
    public float SpawnRadius = 5f;

    private float _nextSpawnTime;

    void Start()
    {
        _nextSpawnTime = Time.time + SpawnInterval;
        // Basic validation
        if (AvailableVariants == null || AvailableVariants.Count == 0)
        {
            Debug.LogError("CollectibleSpawner: No CollectibleVariantConfigs assigned! Disabling spawner.", this);
            enabled = false; // Disable the spawner if it has nothing to spawn
        }
    }

    void Update()
    {
        // Spawn a new collectible if enough time has passed
        if (Time.time >= _nextSpawnTime)
        {
            SpawnRandomCollectible();
            _nextSpawnTime = Time.time + SpawnInterval;
        }
    }

    /// <summary>
    /// Spawns a random collectible from the `AvailableVariants` list.
    /// This method demonstrates the core workflow of the Prefab Variant System:
    /// 1. Select a `CollectibleVariantConfig`.
    /// 2. Get the base `CollectiblePrefab` from the chosen variant's `BaseConfig`.
    /// 3. Instantiate this base prefab.
    /// 4. Retrieve the `Collectible` component from the instantiated object.
    /// 5. Call `Initialize()` on the `Collectible` component, passing the chosen variant config.
    ///    This applies all the effective (overridden or base) properties.
    /// </summary>
    public void SpawnRandomCollectible()
    {
        if (AvailableVariants == null || AvailableVariants.Count == 0)
        {
            Debug.LogWarning("Cannot spawn collectible: No variants available to the spawner.", this);
            return;
        }

        // 1. Pick a random variant configuration from the list
        CollectibleVariantConfig chosenVariant = AvailableVariants[Random.Range(0, AvailableVariants.Count)];

        if (chosenVariant == null)
        {
            Debug.LogError("CollectibleSpawner: A variant in the list is null! Please check the 'AvailableVariants' list.", this);
            return;
        }
        if (chosenVariant.BaseConfig == null)
        {
            Debug.LogError($"CollectibleSpawner: Variant '{chosenVariant.name}' has no BaseConfig assigned! Cannot spawn.", chosenVariant);
            return;
        }

        // 2. Get the base prefab to instantiate from the variant's base configuration
        GameObject basePrefab = chosenVariant.GetCollectiblePrefab();
        if (basePrefab == null)
        {
            Debug.LogError($"CollectibleSpawner: The 'Collectible Prefab' field in BaseConfig for variant '{chosenVariant.name}' is null! Cannot spawn.", chosenVariant.BaseConfig);
            return;
        }

        // 3. Instantiate the base prefab at a random position within the spawn radius
        Vector3 spawnOffset = Random.insideUnitSphere * SpawnRadius;
        spawnOffset.y = 0; // Keep collectibles at the same Y-level as the spawner for simplicity
        Vector3 spawnPosition = transform.position + spawnOffset;

        GameObject collectibleInstance = Instantiate(basePrefab, spawnPosition, Quaternion.identity);

        // 4. Get the Collectible component from the newly instantiated object
        Collectible collectible = collectibleInstance.GetComponent<Collectible>();
        if (collectible != null)
        {
            // 5. Initialize the collectible with the chosen variant's effective properties
            collectible.Initialize(chosenVariant);
        }
        else
        {
            Debug.LogError($"CollectibleSpawner: Instantiated prefab '{basePrefab.name}' does not have a 'Collectible' component! Destroying invalid instance.", collectibleInstance);
            Destroy(collectibleInstance); // Clean up if the prefab is not set up correctly
        }

        Debug.Log($"CollectibleSpawner: Spawned a '{chosenVariant.GetEffectiveCollectibleName()}' variant.");
    }

    /// <summary>
    /// Visualizes the spawn radius in the Unity editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, SpawnRadius);
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create C# Scripts:**
    *   Save each of the five code blocks (`OverrideProperty.cs`, `CollectibleConfig.cs`, `CollectibleVariantConfig.cs`, `Collectible.cs`, `CollectibleSpawner.cs`) into separate `.cs` files in your Unity Project (e.g., in a folder named `Scripts/CollectibleSystem`).

2.  **Create a Base Collectible Prefab:**
    *   In your Unity scene, create an empty GameObject (e.g., named "Collectible_Base").
    *   Add a visual element to it: Right-click on "Collectible_Base" -> 3D Object -> Sphere. Adjust its scale (e.g., 0.5, 0.5, 0.5).
    *   Add a `Collider` component to "Collectible_Base" (e.g., a `SphereCollider`). **Crucially, check the `Is Trigger` box** on the collider.
    *   Add the `Collectible.cs` script to "Collectible_Base".
    *   Drag "Collectible_Base" from the Hierarchy into your Project window (e.g., into a `Prefabs` folder) to create a prefab. Delete the instance from the scene.

3.  **Create a Base Collectible Configuration (ScriptableObject):**
    *   In your Project window, right-click -> Create -> Collectible -> Base Config.
    *   Name it `Base_CollectibleConfig`.
    *   Select `Base_CollectibleConfig`. In the Inspector:
        *   Drag your `Collectible_Base` prefab (from step 2) into the "Collectible Prefab" slot.
        *   Set a default `Value` (e.g., 1).
        *   Optionally, add a `Collect Sound` (create a simple sound effect or use a placeholder) and a `Collect Effect Prefab` (create a simple particle system prefab).
        *   Set a default `Collectible Name` (e.g., "Generic Item").

4.  **Create Collectible Variant Configurations (ScriptableObjects):**
    *   Right-click -> Create -> Collectible -> Variant Config.
    *   Name it `Variant_Coin`.
    *   Select `Variant_Coin`. In the Inspector:
        *   Drag `Base_CollectibleConfig` (from step 3) into the "Base Config" slot.
        *   Under "Override Properties":
            *   Check `Override` next to `Value`, set `Value` to `5`.
            *   Check `Override` next to `Collectible Name`, set `Value` to `Coin`.
            *   (Optional) If you have a specific sound or effect for coins, check its `Override` box and assign it.
    *   Repeat for another variant, e.g., `Variant_Gem`:
        *   Right-click -> Create -> Collectible -> Variant Config.
        *   Name it `Variant_Gem`.
        *   Assign `Base_CollectibleConfig` to "Base Config".
        *   Override `Value` to `25`.
        *   Override `Collectible Name` to `Gem`.
        *   Override `Despawn Duration` to `10.0` (meaning Gems will disappear after 10 seconds if not collected).

5.  **Set up the Spawner in Your Scene:**
    *   Create an empty GameObject in your scene (e.g., "GameManager" or "CollectibleSpawners").
    *   Add the `CollectibleSpawner.cs` script to it.
    *   Select your spawner GameObject. In the Inspector:
        *   Expand the "Available Variants" list.
        *   Drag `Variant_Coin` and `Variant_Gem` from your Project window into this list.
        *   Adjust `Spawn Interval` (e.g., 1.5) and `Spawn Radius` (e.g., 8).

6.  **Ensure a "Player" Object Exists:**
    *   Create a simple player GameObject (e.g., a Cube with a `CharacterController` or `Rigidbody` and a `CapsuleCollider`).
    *   **Set its Tag to "Player"** (you might need to create this tag). The `Collectible.cs` script uses `other.CompareTag("Player")` for interaction.

7.  **Run the Scene:**
    *   You should now see various collectible types (Coins, Gems) spawning periodically around your spawner.
    *   When your Player character walks into them, they will be collected, playing sounds/effects and disappearing.

This comprehensive setup demonstrates the PrefabVariantSystem, offering a flexible and scalable way to manage diverse object types from a shared base.