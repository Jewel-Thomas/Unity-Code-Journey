// Unity Design Pattern Example: EffectPoolingSystem
// This script demonstrates the EffectPoolingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **Effect Pooling System** is a crucial design pattern in Unity for optimizing performance, especially when dealing with frequently instantiated and destroyed GameObjects like visual effects (particles, explosions, muzzle flashes), sound effects, or temporary UI elements. Instead of destroying and re-instantiating objects, which causes garbage collection and CPU spikes, objects are "pooled": deactivated and stored for later reuse.

This example provides a complete, practical implementation ready to be dropped into any Unity project.

---

### **`EffectPoolingSystem.cs`**

This script contains all the necessary classes:
1.  **`EffectPoolConfig`**: A serializable class to configure each type of effect pool in the inspector.
2.  **`PooledObject`**: A component attached to each pooled GameObject to help identify its origin and provide a convenient `ReturnToPool()` method.
3.  **`EffectPool`**: An internal class that manages a single type of effect (e.g., "Explosion Effect"). It handles the queuing, activation, and deactivation of objects.
4.  **`EffectPoolingSystem`**: The main singleton class that manages multiple `EffectPool` instances and provides the public API (`GetEffect`, `ReturnEffect`).

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and Dictionary
using System.Collections;         // For Coroutines

/// <summary>
/// Serializable class to configure an effect pool directly in the Unity Inspector.
/// Each instance of this class defines a specific type of effect to be pooled.
/// </summary>
[System.Serializable]
public class EffectPoolConfig
{
    [Tooltip("A unique name for this effect type (e.g., 'Explosion', 'MuzzleFlash'). Used to request effects.")]
    public string effectName;

    [Tooltip("The prefab GameObject for this effect. It should contain the visual/audio components.")]
    public GameObject effectPrefab;

    [Tooltip("The initial number of effects to create and pre-populate in the pool when the game starts.")]
    public int initialPoolSize = 5;

    [Tooltip("The maximum number of effects of this type that can exist simultaneously. " +
             "Set to 0 for no limit (though not recommended for performance).")]
    public int maxPoolSize = 10;

    [Tooltip("If true, the pool can create new instances beyond 'initialPoolSize' up to 'maxPoolSize' if needed.")]
    public bool canGrow = true;
}

/// <summary>
/// Component attached to pooled GameObjects. It stores the name of the pool it belongs to
/// and provides a convenient method for the effect itself to return to its pool.
/// </summary>
public class PooledObject : MonoBehaviour
{
    // The name of the pool this object originated from.
    // This allows the EffectPoolingSystem to know which pool to return it to.
    [HideInInspector] public string poolName;

    private EffectPoolingSystem _poolSystem; // Cached reference to the pooling system

    /// <summary>
    /// Initializes the PooledObject with a reference to the main pooling system.
    /// This is called internally by the EffectPool when an object is created.
    /// </summary>
    /// <param name="system">The EffectPoolingSystem instance.</param>
    /// <param name="name">The name of the pool this object belongs to.</param>
    public void Initialize(EffectPoolingSystem system, string name)
    {
        _poolSystem = system;
        poolName = name;
    }

    /// <summary>
    /// Returns this GameObject back to its originating pool.
    /// This method can be called by a script attached to the pooled effect itself
    /// (e.g., when a particle system finishes playing, or an animation ends).
    /// </summary>
    public void ReturnToPool()
    {
        if (_poolSystem != null && !string.IsNullOrEmpty(poolName))
        {
            _poolSystem.ReturnEffect(this.gameObject);
        }
        else
        {
            // Fallback for objects that were not correctly initialized or pool system is destroyed.
            Debug.LogWarning($"PooledObject '{gameObject.name}' tried to return to pool '{poolName}' but PoolSystem is missing or poolName is invalid. Destroying object instead.", this);
            Destroy(this.gameObject);
        }
    }
}

/// <summary>
/// A helper script for ParticleSystem effects. When attached to an effect prefab,
/// it automatically returns the GameObject to the pool once its ParticleSystem(s) have finished.
/// </summary>
[RequireComponent(typeof(PooledObject))] // Ensures PooledObject is present
public class ReturnToPoolOnParticleFinish : MonoBehaviour
{
    private ParticleSystem _mainParticleSystem;
    private PooledObject _pooledObject;

    void Awake()
    {
        _mainParticleSystem = GetComponent<ParticleSystem>();
        _pooledObject = GetComponent<PooledObject>();

        if (_mainParticleSystem == null)
        {
            Debug.LogError($"ReturnToPoolOnParticleFinish requires a ParticleSystem component on '{gameObject.name}'. Disabling script.", this);
            enabled = false;
        }
        if (_pooledObject == null)
        {
            Debug.LogError($"ReturnToPoolOnParticleFinish requires a PooledObject component on '{gameObject.name}'. Disabling script.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        // When the object is enabled (retrieved from pool), start checking for particle completion.
        if (_mainParticleSystem != null && _pooledObject != null)
        {
            StartCoroutine(CheckParticleSystemCompletion());
        }
    }

    /// <summary>
    /// Coroutine to continuously check if the particle system (and its children) has finished playing.
    /// </summary>
    private IEnumerator CheckParticleSystemCompletion()
    {
        // Wait until the particle system is no longer alive.
        // `isAlive(true)` checks if any particles are still active, including children.
        // `isPlaying` would be true as long as the system is emitting, even if no particles are on screen.
        yield return new WaitWhile(() => _mainParticleSystem.IsAlive(true));

        // Once finished, return the GameObject to its pool.
        _pooledObject.ReturnToPool();
    }
}


/// <summary>
/// Internal class that manages a single pool of GameObjects for a specific effect type.
/// It handles instantiation, storage, and retrieval of pooled objects.
/// </summary>
internal class EffectPool
{
    private GameObject _prefab;                      // The prefab to instantiate for this pool
    private string _poolName;                        // The name of this pool (matches EffectPoolConfig.effectName)
    private Queue<GameObject> _availableObjects;     // Objects currently in the pool and ready for reuse
    private List<GameObject> _allObjectsInPool;      // All objects ever created by this pool (for tracking/safety)
    private int _maxSize;                            // Maximum number of objects this pool can hold
    private bool _canGrow;                           // Whether the pool can create new objects if all are in use
    private EffectPoolingSystem _parentSystem;       // Reference to the main pooling system

    /// <summary>
    /// Constructor for EffectPool. Initializes the pool based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration for this specific effect pool.</param>
    /// <param name="parent">The main EffectPoolingSystem instance.</param>
    public EffectPool(EffectPoolConfig config, EffectPoolingSystem parent)
    {
        _prefab = config.effectPrefab;
        _poolName = config.effectName;
        _maxSize = config.maxPoolSize;
        _canGrow = config.canGrow;
        _parentSystem = parent;

        _availableObjects = new Queue<GameObject>(config.initialPoolSize);
        _allObjectsInPool = new List<GameObject>(config.initialPoolSize);

        // Pre-populate the pool with initial objects
        for (int i = 0; i < config.initialPoolSize; i++)
        {
            GameObject newObj = CreateNewObject();
            if (newObj != null)
            {
                _availableObjects.Enqueue(newObj);
                _allObjectsInPool.Add(newObj);
            }
        }

        Debug.Log($"Initialized Effect Pool '{_poolName}' with {config.initialPoolSize} objects. Max size: {_maxSize}", _prefab);
    }

    /// <summary>
    /// Creates a new GameObject instance from the prefab and sets it up for pooling.
    /// </summary>
    /// <returns>The newly created GameObject, or null if creation failed (e.g., prefab is null).</returns>
    private GameObject CreateNewObject()
    {
        if (_prefab == null)
        {
            Debug.LogError($"Effect Pool '{_poolName}' has no prefab assigned!", _parentSystem);
            return null;
        }

        GameObject newObj = GameObject.Instantiate(_prefab);
        newObj.name = _poolName + " (Pooled)"; // Renames for clarity in Hierarchy
        newObj.transform.SetParent(_parentSystem.transform); // Parent to the pooling system's GameObject for organization
        newObj.SetActive(false); // Objects start deactivated

        // Add and initialize PooledObject component for tracking and returning
        PooledObject pooledObj = newObj.GetComponent<PooledObject>();
        if (pooledObj == null)
        {
            pooledObj = newObj.AddComponent<PooledObject>();
        }
        pooledObj.Initialize(_parentSystem, _poolName);

        return newObj;
    }

    /// <summary>
    /// Retrieves an effect GameObject from this pool.
    /// </summary>
    /// <returns>An active GameObject from the pool, or null if no objects are available and cannot grow.</returns>
    public GameObject Get()
    {
        GameObject objToReturn = null;

        if (_availableObjects.Count > 0)
        {
            objToReturn = _availableObjects.Dequeue();
        }
        else if (_canGrow && (_maxSize == 0 || _allObjectsInPool.Count < _maxSize))
        {
            // If the pool can grow and hasn't reached its max size, create a new object
            objToReturn = CreateNewObject();
            if (objToReturn != null)
            {
                _allObjectsInPool.Add(objToReturn);
                Debug.LogWarning($"Effect Pool '{_poolName}' grew to {_allObjectsInPool.Count} objects due to demand. Consider increasing initialPoolSize.", _prefab);
            }
        }
        else
        {
            // No objects available and cannot grow or max size reached
            Debug.LogWarning($"Effect Pool '{_poolName}' is empty and cannot grow (or reached max size {_maxSize}). Returning null.", _prefab);
        }

        if (objToReturn != null)
        {
            objToReturn.SetActive(true); // Activate the object
        }

        return objToReturn;
    }

    /// <summary>
    /// Returns an effect GameObject to this pool, deactivating it.
    /// </summary>
    /// <param name="obj">The GameObject to return.</param>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Basic check to prevent returning objects not managed by this pool
        // (A more robust check could compare the PooledObject.poolName)
        if (!_allObjectsInPool.Contains(obj))
        {
            Debug.LogWarning($"Attempted to return object '{obj.name}' to pool '{_poolName}', but it was not created by this pool. Destroying instead.", obj);
            GameObject.Destroy(obj);
            return;
        }

        if (_availableObjects.Contains(obj))
        {
            Debug.LogWarning($"Attempted to return object '{obj.name}' to pool '{_poolName}', but it's already in the pool. Ignoring.", obj);
            return;
        }

        obj.SetActive(false); // Deactivate the object
        _availableObjects.Enqueue(obj); // Add back to the available queue
    }

    /// <summary>
    /// Cleans up all objects managed by this pool.
    /// </summary>
    public void CleanUp()
    {
        foreach (GameObject obj in _allObjectsInPool)
        {
            if (obj != null)
            {
                GameObject.Destroy(obj);
            }
        }
        _allObjectsInPool.Clear();
        _availableObjects.Clear();
        Debug.Log($"Effect Pool '{_poolName}' cleaned up.");
    }
}


/// <summary>
/// The main EffectPoolingSystem singleton. Manages multiple effect pools.
/// Attach this script to an empty GameObject in your scene (e.g., named "EffectPoolingSystem").
/// </summary>
public class EffectPoolingSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static EffectPoolingSystem Instance { get; private set; }

    [Header("Effect Pool Configurations")]
    [Tooltip("Define different types of effects here. Each entry creates a new pool.")]
    [SerializeField]
    private List<EffectPoolConfig> effectPoolConfigs = new List<EffectPoolConfig>();

    // Dictionary to store and retrieve EffectPools by their name.
    private Dictionary<string, EffectPool> _pools = new Dictionary<string, EffectPool>();

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the singleton and pools.
    /// </summary>
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate EffectPoolingSystem found. Destroying this duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads

        InitializePools();
    }

    /// <summary>
    /// Initializes all effect pools based on the configurations provided in the inspector.
    /// </summary>
    private void InitializePools()
    {
        _pools.Clear(); // Clear existing pools in case of re-initialization

        foreach (EffectPoolConfig config in effectPoolConfigs)
        {
            if (config.effectPrefab == null)
            {
                Debug.LogError($"Effect Pool '{config.effectName}' has no prefab assigned. Skipping initialization.", this);
                continue;
            }
            if (_pools.ContainsKey(config.effectName))
            {
                Debug.LogError($"Duplicate effect name '{config.effectName}' found in configurations. Each effect name must be unique. Skipping duplicate.", this);
                continue;
            }

            EffectPool newPool = new EffectPool(config, this);
            _pools.Add(config.effectName, newPool);
        }
        Debug.Log($"EffectPoolingSystem: All {effectPoolConfigs.Count} pools initialized.");
    }

    /// <summary>
    /// Retrieves an effect GameObject from the specified pool.
    /// </summary>
    /// <param name="effectName">The unique name of the effect type (e.g., "Explosion").</param>
    /// <param name="position">The world position to place the effect at.</param>
    /// <param name="rotation">The world rotation to apply to the effect.</param>
    /// <param name="parent">Optional: A Transform to parent the effect to. If null, parented to the pooling system.</param>
    /// <returns>The activated effect GameObject, or null if the pool doesn't exist or is exhausted.</returns>
    public GameObject GetEffect(string effectName, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (_pools.TryGetValue(effectName, out EffectPool pool))
        {
            GameObject effect = pool.Get();
            if (effect != null)
            {
                effect.transform.position = position;
                effect.transform.rotation = rotation;
                effect.transform.SetParent(parent == null ? this.transform : parent); // Use parent if provided, else system's transform
            }
            return effect;
        }
        else
        {
            Debug.LogWarning($"Effect Pool for '{effectName}' not found. Did you configure it in the inspector?", this);
            return null;
        }
    }

    /// <summary>
    /// Returns an effect GameObject back to its originating pool.
    /// It automatically determines which pool the object belongs to via its PooledObject component.
    /// </summary>
    /// <param name="effectToReturn">The GameObject to return to the pool.</param>
    public void ReturnEffect(GameObject effectToReturn)
    {
        if (effectToReturn == null) return;

        PooledObject pooledObj = effectToReturn.GetComponent<PooledObject>();
        if (pooledObj != null && !string.IsNullOrEmpty(pooledObj.poolName))
        {
            if (_pools.TryGetValue(pooledObj.poolName, out EffectPool pool))
            {
                pool.Return(effectToReturn);
            }
            else
            {
                Debug.LogWarning($"Effect Pool '{pooledObj.poolName}' not found for object '{effectToReturn.name}'. Destroying object instead.", effectToReturn);
                Destroy(effectToReturn);
            }
        }
        else
        {
            Debug.LogWarning($"Object '{effectToReturn.name}' does not have a PooledObject component or its poolName is not set. Destroying object instead.", effectToReturn);
            Destroy(effectToReturn);
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Cleans up all managed pools.
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            // Clean up each pool
            foreach (var kvp in _pools)
            {
                kvp.Value.CleanUp();
            }
            _pools.Clear();
            Instance = null; // Clear the static instance
            Debug.Log("EffectPoolingSystem: Cleaned up all pools and instance.");
        }
    }
}

/*
/// EXAMPLE USAGE SCRIPT: EffectSpawner.cs
/// -------------------------------------------------------------
/// Create a new C# script named "EffectSpawner.cs" and attach it
/// to any empty GameObject in your scene.
///
/// This script demonstrates how to request effects from the pooling
/// system and automatically have them return.
///
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Header("Effect Names (must match Pool Configs)")]
    public string explosionEffectName = "Explosion";
    public string hitParticleEffectName = "HitParticle";

    [Header("Spawn Settings")]
    public float spawnInterval = 1f;
    public float spawnRadius = 5f;

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnRandomEffect();
        }
    }

    void SpawnRandomEffect()
    {
        // Randomly choose an effect type
        string effectToSpawn = (Random.value < 0.5f) ? explosionEffectName : hitParticleEffectName;

        // Calculate a random position within the spawnRadius
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = Mathf.Abs(spawnPosition.y); // Ensure effects appear above the ground

        // Get an effect from the EffectPoolingSystem
        GameObject effect = EffectPoolingSystem.Instance.GetEffect(effectToSpawn, spawnPosition, Quaternion.identity);

        if (effect != null)
        {
            Debug.Log($"Spawned '{effect.name}' at {spawnPosition}");

            // IMPORTANT: For ParticleSystems, ensure the prefab has the 'ReturnToPoolOnParticleFinish' component
            // attached to it, along with the 'PooledObject' component. This will automatically
            // handle returning the effect to the pool when its particles finish.

            // If it's a non-particle effect (e.g., an animation or sound that plays for a fixed duration),
            // you might want to use a Coroutine to manually return it after a delay:
            // StartCoroutine(ReturnEffectAfterDelay(effect, 3f)); // Example: return after 3 seconds
        }
        else
        {
            Debug.LogWarning($"Failed to get effect '{effectToSpawn}'. Pool might be exhausted or not configured.");
        }
    }

    // Example Coroutine for non-particle effects (e.g., sound effects, simple animations)
    // You would typically attach this to the effect prefab itself, or manage it externally
    // if the effect doesn't know its own duration.
    IEnumerator ReturnEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            // Only return if it's still active. It might have been returned by another script.
            if (effect.activeSelf)
            {
                EffectPoolingSystem.Instance.ReturnEffect(effect);
                Debug.Log($"Manually returned '{effect.name}' to pool after {delay} seconds.");
            }
        }
    }
}
*/
```

---

### **How to Set Up in Unity:**

1.  **Create the Script:**
    *   In your Unity project, create a new C# script named `EffectPoolingSystem.cs`.
    *   Copy and paste the entire code block above into this script.

2.  **Create the Pooling System GameObject:**
    *   In your scene, create an empty GameObject (e.g., Right-click in Hierarchy -> Create Empty).
    *   Rename it to `EffectPoolingSystem`.
    *   Drag and drop the `EffectPoolingSystem.cs` script onto this new GameObject.

3.  **Prepare Effect Prefabs:**
    *   Create two example Particle System prefabs (e.g., one for an "Explosion" and one for a "HitParticle").
        *   Right-click in Project window -> Create -> Effects -> Particle System.
        *   Customize its appearance (e.g., duration, shape, color, emission).
        *   **Crucially:** Ensure your Particle System **does not loop** (`Looping` checkbox unchecked in Particle System component). If it loops, `ReturnToPoolOnParticleFinish` will never return it.
        *   Drag the configured Particle System from the Hierarchy into your Project window to create a prefab.
    *   For **EACH** effect prefab:
        *   Select the prefab in the Project window.
        *   In the Inspector, click "Add Component".
        *   Search for and add the `PooledObject` component.
        *   Search for and add the `ReturnToPoolOnParticleFinish` component. (This script automatically calls `PooledObject.ReturnToPool()` when the particles finish playing).

4.  **Configure the Pooling System:**
    *   Select the `EffectPoolingSystem` GameObject in your scene.
    *   In the Inspector, expand the `Effect Pool Configs` list.
    *   Add two elements (or more, for each effect type you want to pool):
        *   **Element 0 (Explosion):**
            *   `Effect Name`: `Explosion` (This must be unique and will be used to request the effect).
            *   `Effect Prefab`: Drag your "Explosion" prefab from the Project window here.
            *   `Initial Pool Size`: `5` (or desired initial amount).
            *   `Max Pool Size`: `10` (or desired max amount, 0 for unlimited, but beware of performance).
            *   `Can Grow`: `True` (allows the pool to instantiate more if needed, up to `Max Pool Size`).
        *   **Element 1 (HitParticle):**
            *   `Effect Name`: `HitParticle`
            *   `Effect Prefab`: Drag your "HitParticle" prefab here.
            *   Configure `Initial Pool Size`, `Max Pool Size`, `Can Grow` as desired.

5.  **Create and Configure the `EffectSpawner` (Example Usage):**
    *   Create another empty GameObject in your scene (e.g., `EffectSpawner`).
    *   Create a new C# script named `EffectSpawner.cs`.
    *   Copy and paste the commented-out `EffectSpawner` example script from the bottom of `EffectPoolingSystem.cs` into this new `EffectSpawner.cs` file (make sure to remove the `/* ... */` comment block).
    *   Attach `EffectSpawner.cs` to the `EffectSpawner` GameObject.
    *   In the Inspector for `EffectSpawner`, ensure `Explosion Effect Name` and `Hit Particle Effect Name` exactly match the `Effect Name`s you configured in the `EffectPoolingSystem`.
    *   Adjust `Spawn Interval` and `Spawn Radius` as needed.

6.  **Run Your Scene:**
    *   Press Play in Unity. You should see effects spawning, and they will automatically return to the pool after their particles finish.
    *   Observe the Hierarchy: You'll see the `EffectPoolingSystem` GameObject with its child pooled effects. They will activate/deactivate instead of being destroyed/recreated.

This complete setup provides a robust and educational example of the Effect Pooling System design pattern in Unity, ready for your projects!