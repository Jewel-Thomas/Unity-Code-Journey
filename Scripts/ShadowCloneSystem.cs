// Unity Design Pattern Example: ShadowCloneSystem
// This script demonstrates the ShadowCloneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **ShadowCloneSystem** design pattern, as interpreted for Unity, is a robust system that combines the **Prototype Pattern** with **Object Pooling** and a **Centralized Manager**. It's designed for efficiently creating and managing numerous instances (clones) of complex GameObjects (prototypes), particularly when those objects are frequently created and destroyed, like enemies, projectiles, or particle effects.

Here's how it works:

1.  **Prototype Pattern**: Defines a `Clone()` operation for objects that can be copied. In Unity, this often means creating a new instance of a prefab or a GameObject from an existing one using `Instantiate()`.
2.  **Object Pooling**: Instead of constantly instantiating and destroying GameObjects (which can be performance-intensive), the system reuses inactive objects from a pool. When an object is "destroyed," it's simply returned to the pool and deactivated, ready for future reuse.
3.  **Centralized Management (The "System")**: A single manager (e.g., `ShadowCloneManager`) holds references to all prototypes and manages their respective object pools. It provides a simple interface to request a clone and return it to the pool.
4.  **"Shadow" Aspect**:
    *   **Shadow Prototypes**: The original GameObjects (prototypes) exist in the scene or as prefabs, but are generally inactive. They act as "shadows" or templates from which active instances are spawned.
    *   **Shadow Pool**: Inactive clones reside in the object pool, waiting in the "shadows" to be activated and participate in the game world.
    *   **Shared State**: All clones of a specific prototype initially share its base configuration and components, reflecting its "shadow" state. When a clone is activated, its state is reset or initialized from this shared prototype.

This pattern is highly practical in Unity for optimizing performance and simplifying the management of dynamic game elements.

---

## Complete C# Unity Example: ShadowCloneSystem

This example will demonstrate spawning and pooling `ExampleEnemy` instances using the `ShadowCloneSystem`.

**1. `ICloneablePrototype.cs`**
(Defines the cloning contract)

```csharp
using UnityEngine; // Not strictly needed for interface, but good for Unity context

/// <summary>
/// Interface for objects that can be cloned.
/// This defines the core 'Prototype' part of the ShadowCloneSystem, allowing
/// any implementing class to provide a method for creating copies of itself.
/// </summary>
/// <typeparam name="T">The type of object that will be returned by the clone operation.</typeparam>
public interface ICloneablePrototype<T>
{
    /// <summary>
    /// Creates a new object that is a copy of the current instance.
    /// This is the 'clone' operation. Implementations should handle
    /// creating a new instance and copying necessary data.
    /// </summary>
    /// <returns>A new object that is a deep or shallow copy of this instance.</returns>
    T Clone();
}
```

**2. `ShadowCloneableObject.cs`**
(Base class for GameObjects that can be cloned and pooled)

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for GameObjects that can be managed by the ShadowCloneSystem.
/// It implements the ICloneablePrototype interface specifically for Unity GameObjects,
/// providing the concrete mechanism for cloning using `Instantiate()`.
/// It also defines lifecycle methods (`OnActivate`, `OnDeactivate`) that the manager
/// calls when a clone is taken from or returned to the object pool.
/// </summary>
public abstract class ShadowCloneableObject : MonoBehaviour, ICloneablePrototype<GameObject>
{
    [Tooltip("A unique identifier for this type of cloneable object.")]
    [SerializeField]
    private string _prototypeID;
    /// <summary>
    /// Gets the unique identifier for this prototype.
    /// Used by the ShadowCloneManager to retrieve the correct prototype and manage its pool.
    /// </summary>
    public string PrototypeID => _prototypeID;

    /// <summary>
    /// Indicates if this instance is the original prototype or an active clone.
    /// Prototypes are typically inactive in the scene and serve only as templates.
    /// </summary>
    public bool IsPrototype { get; private set; } = false;

    // Reference to the ShadowCloneManager for returning this clone to the pool.
    protected ShadowCloneManager _manager;

    /// <summary>
    /// Initializes the cloneable object. This is called by the ShadowCloneManager
    /// to set up prototypes and new clones.
    /// </summary>
    /// <param name="manager">The ShadowCloneManager instance.</param>
    /// <param name="isPrototype">True if this instance is the original prototype prefab, false otherwise.</param>
    public void Initialize(ShadowCloneManager manager, bool isPrototype)
    {
        _manager = manager;
        IsPrototype = isPrototype;
        if (IsPrototype)
        {
            // Prototypes should generally not be active in the scene visually or functionally.
            // They are just templates.
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a clone (copy) of this GameObject.
    /// This is the concrete implementation of the ICloneablePrototype interface for Unity GameObjects.
    /// Unity's `Instantiate` method serves as the cloning mechanism for GameObjects.
    /// </summary>
    /// <returns>A new GameObject instance that is a copy of this prototype.</returns>
    public GameObject Clone()
    {
        // Instantiate creates a new GameObject from the existing GameObject (this prototype).
        GameObject cloneGO = Instantiate(this.gameObject);
        cloneGO.name = this.gameObject.name + " (Clone)"; // Rename for clarity in hierarchy

        // Get the ShadowCloneableObject component from the newly created clone.
        ShadowCloneableObject cloneComponent = cloneGO.GetComponent<ShadowCloneableObject>();
        if (cloneComponent != null)
        {
            // Initialize the clone: mark it as NOT a prototype and provide manager reference.
            cloneComponent.Initialize(_manager, false);
        }
        else
        {
            Debug.LogError($"Clone of {this.name} is missing ShadowCloneableObject component! This should not happen.");
        }

        return cloneGO;
    }

    /// <summary>
    /// Called when the clone is taken from the pool and made active in the scene.
    /// Override this in derived classes to reset specific state, enable components,
    /// play particle systems, etc., preparing the object for use.
    /// </summary>
    public virtual void OnActivate()
    {
        // Ensure the GameObject is active when taken from the pool.
        gameObject.SetActive(true);
        // Debug.Log($"{name} activated."); // Uncomment for debugging activation
    }

    /// <summary>
    /// Called when the clone is returned to the pool and deactivated.
    /// Override this in derived classes to reset specific state, disable components,
    /// stop particle systems, etc., preparing the object for storage in the pool.
    /// </summary>
    public virtual void OnDeactivate()
    {
        // Ensure the GameObject is inactive when returned to the pool.
        gameObject.SetActive(false);
        // Debug.Log($"{name} deactivated."); // Uncomment for debugging deactivation
    }

    /// <summary>
    /// Returns this clone instance to the ShadowCloneManager's pool.
    /// This method simplifies the process for the clone itself to manage its lifecycle.
    /// </summary>
    public void ReturnToPool()
    {
        if (_manager != null && !IsPrototype)
        {
            _manager.ReturnClone(this);
        }
        else if (IsPrototype)
        {
            Debug.LogWarning($"Attempted to return a prototype '{_prototypeID}' to the pool. Prototypes are not pooled.");
        }
        else
        {
            Debug.LogError($"Cannot return {name} to pool: ShadowCloneManager reference is null or it's a prototype.");
        }
    }
}
```

**3. `ShadowCloneManager.cs`**
(The core 'ShadowCloneSystem' manager with object pooling)

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like .Count()

/// <summary>
/// The central 'ShadowCloneSystem' manager.
/// This class acts as a singleton and is responsible for:
/// 1. Registering 'ShadowCloneableObject' prototypes.
/// 2. Maintaining object pools for each prototype type.
/// 3. Providing methods to get active clones from the pool or create new ones if needed.
/// 4. Handling the return of clones to their respective pools.
/// </summary>
public class ShadowCloneManager : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static ShadowCloneManager Instance { get; private set; }

    [Tooltip("List of all ShadowCloneableObject prototypes to manage.")]
    [SerializeField]
    private List<ShadowCloneableObject> _prototypes = new List<ShadowCloneableObject>();

    [Tooltip("Initial number of clones to pre-spawn for each prototype.")]
    [SerializeField]
    private int _initialPoolSize = 5;

    // Dictionary to store prototypes, keyed by their ID for quick lookup.
    private Dictionary<string, ShadowCloneableObject> _prototypeLookup = new Dictionary<string, ShadowCloneableObject>();

    // Dictionary to store object pools for each prototype type.
    // Each pool is a Queue of inactive ShadowCloneableObject instances.
    private Dictionary<string, Queue<ShadowCloneableObject>> _objectPools = new Dictionary<string, Queue<ShadowCloneableObject>>();

    private void Awake()
    {
        // Implement singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make the manager persist across scene loads.
        DontDestroyOnLoad(gameObject);

        InitializeManager();
    }

    /// <summary>
    /// Initializes the manager by registering all specified prototypes
    /// and pre-spawning clones to populate their object pools.
    /// </summary>
    private void InitializeManager()
    {
        Debug.Log("ShadowCloneManager: Initializing...");
        // 1. Register Prototypes and set them up
        foreach (var prototype in _prototypes)
        {
            if (prototype == null)
            {
                Debug.LogError("ShadowCloneManager contains a null prototype reference. Please check your setup.", this);
                continue;
            }

            if (_prototypeLookup.ContainsKey(prototype.PrototypeID))
            {
                Debug.LogError($"Duplicate PrototypeID '{prototype.PrototypeID}' found. Each prototype must have a unique ID.", prototype);
                continue;
            }

            _prototypeLookup.Add(prototype.PrototypeID, prototype);
            // Initialize the prototype instance itself. It remains inactive.
            prototype.Initialize(this, true); 

            // 2. Initialize Object Pools for each prototype
            _objectPools.Add(prototype.PrototypeID, new Queue<ShadowCloneableObject>());
            PreSpawnClones(prototype, _initialPoolSize);
        }
        Debug.Log($"ShadowCloneManager initialized with {_prototypes.Count} unique prototypes and {_initialPoolSize} initial clones each.");
    }

    /// <summary>
    /// Pre-spawns a specified number of clones for a given prototype and adds them to its pool.
    /// This helps to reduce performance spikes during gameplay by creating objects upfront.
    /// </summary>
    /// <param name="prototype">The prototype to clone.</param>
    /// <param name="count">The number of clones to create for the pool.</param>
    private void PreSpawnClones(ShadowCloneableObject prototype, int count)
    {
        if (prototype == null) return;

        Queue<ShadowCloneableObject> pool = _objectPools[prototype.PrototypeID];
        for (int i = 0; i < count; i++)
        {
            // Use the prototype's Clone method to create a new instance.
            GameObject cloneGO = prototype.Clone();
            ShadowCloneableObject cloneComponent = cloneGO.GetComponent<ShadowCloneableObject>();

            if (cloneComponent != null)
            {
                cloneComponent.Initialize(this, false); // Mark as a clone, not a prototype
                cloneComponent.OnDeactivate(); // Ensure it's deactivated and ready for use in the pool
                pool.Enqueue(cloneComponent); // Add to the pool
            }
            else
            {
                Debug.LogError($"Pre-spawned clone of '{prototype.PrototypeID}' is missing ShadowCloneableObject component! Destroying clone.", cloneGO);
                Destroy(cloneGO); // Clean up if something went wrong
            }
        }
        // Debug.Log($"Pre-spawned {count} clones for prototype '{prototype.PrototypeID}'. Total in pool: {pool.Count}");
    }


    /// <summary>
    /// Retrieves an active clone of the specified prototype ID from the pool.
    /// This is the core 'GetClone' operation of the ShadowCloneSystem.
    /// If the pool is empty, a new clone is created on demand (and a warning is logged).
    /// </summary>
    /// <param name="prototypeID">The unique ID of the prototype to clone.</param>
    /// <returns>An active GameObject clone, or null if the prototype ID is invalid.</returns>
    public GameObject GetClone(string prototypeID)
    {
        if (!_prototypeLookup.ContainsKey(prototypeID))
        {
            Debug.LogError($"ShadowCloneManager: Prototype with ID '{prototypeID}' not found. Please register it.", this);
            return null;
        }

        Queue<ShadowCloneableObject> pool = _objectPools[prototypeID];
        ShadowCloneableObject cloneComponent;

        // Try to get an instance from the pool.
        if (pool.Count > 0)
        {
            cloneComponent = pool.Dequeue();
        }
        else
        {
            // If the pool is empty, create a new clone directly from the prototype.
            // This expands the pool dynamically but can cause a minor performance hit.
            ShadowCloneableObject prototype = _prototypeLookup[prototypeID];
            GameObject newCloneGO = prototype.Clone();
            cloneComponent = newCloneGO.GetComponent<ShadowCloneableObject>();
            if (cloneComponent != null)
            {
                cloneComponent.Initialize(this, false); // Initialize the newly created clone
                Debug.LogWarning($"ShadowCloneManager: Pool for '{prototypeID}' was empty. Created a new clone instance. Consider increasing initial pool size.", this);
            }
            else
            {
                Debug.LogError($"Newly created clone of '{prototypeID}' is missing ShadowCloneableObject component! Destroying clone.", newCloneGO);
                Destroy(newCloneGO);
                return null;
            }
        }

        // Activate the clone and return its GameObject.
        cloneComponent.OnActivate();
        return cloneComponent.gameObject;
    }

    /// <summary>
    /// Returns a clone to its respective object pool, deactivating it in the process.
    /// This is crucial for the object pooling aspect of the ShadowCloneSystem, ensuring
    /// objects are reused instead of destroyed.
    /// </summary>
    /// <param name="cloneToReturn">The ShadowCloneableObject instance to return to the pool.</param>
    public void ReturnClone(ShadowCloneableObject cloneToReturn)
    {
        if (cloneToReturn == null)
        {
            Debug.LogWarning("ShadowCloneManager: Attempted to return a null clone to the pool.");
            return;
        }
        if (cloneToReturn.IsPrototype)
        {
            Debug.LogWarning($"ShadowCloneManager: Attempted to return a prototype '{cloneToReturn.PrototypeID}' to the pool. Prototypes are not pooled.");
            return;
        }

        string prototypeID = cloneToReturn.PrototypeID;
        if (!_objectPools.ContainsKey(prototypeID))
        {
            Debug.LogError($"ShadowCloneManager: No pool found for prototype ID '{prototypeID}'. Cannot return clone {cloneToReturn.name}. Destroying it instead.", cloneToReturn);
            Destroy(cloneToReturn.gameObject); // Destroy if we can't pool it
            return;
        }

        cloneToReturn.OnDeactivate(); // Deactivate before returning to pool
        _objectPools[prototypeID].Enqueue(cloneToReturn); // Add back to the pool
        // Debug.Log($"ShadowCloneManager: Returned {cloneToReturn.name} to pool. Pool size: {_objectPools[prototypeID].Count}");
    }

    // --- Optional Helper Methods for Debugging/Monitoring ---
    /// <summary>
    /// Gets the current number of available (inactive) clones in the pool for a given prototype.
    /// </summary>
    /// <param name="prototypeID">The ID of the prototype.</param>
    /// <returns>The count of pooled clones.</returns>
    public int GetPooledCloneCount(string prototypeID)
    {
        if (_objectPools.ContainsKey(prototypeID))
        {
            return _objectPools[prototypeID].Count;
        }
        return 0;
    }

    /// <summary>
    /// Gets a rough count of currently active clones for a given prototype.
    /// NOTE: This method is O(N) and uses `FindObjectsOfType`. For performance-critical
    /// scenarios, you would maintain a separate list/HashSet of active clones within
    /// the manager or on the `ShadowCloneableObject` itself (e.g., `bool isActiveClone`).
    /// This is provided for educational demonstration purposes.
    /// </summary>
    /// <param name="prototypeID">The ID of the prototype.</param>
    /// <returns>The count of active clones.</returns>
    public int GetActiveCloneCount(string prototypeID)
    {
        // This is an inefficient way to count active clones for a large number of objects.
        // It's meant for demonstration. In a real project, track active clones explicitly.
        if (!_prototypeLookup.ContainsKey(prototypeID)) return 0;
        return FindObjectsOfType<ShadowCloneableObject>().Count(c =>
            c.PrototypeID == prototypeID && c.gameObject.activeInHierarchy && !c.IsPrototype
        );
    }
}
```

**4. `ExampleEnemy.cs`**
(A concrete implementation of a `ShadowCloneableObject`)

```csharp
using UnityEngine;

/// <summary>
/// An example concrete implementation of a ShadowCloneableObject.
/// Represents an enemy that can be spawned, moves, takes damage, and
/// automatically returns to the ShadowCloneSystem's pool after a lifetime
/// or when its health drops to zero.
/// </summary>
public class ExampleEnemy : ShadowCloneableObject
{
    [Tooltip("Movement speed of the enemy.")]
    [SerializeField]
    private float _speed = 2f;
    
    [Tooltip("Initial health of the enemy when activated.")]
    [SerializeField]
    private int _maxHealth = 10;
    
    [Tooltip("How long the enemy stays active before returning to the pool.")]
    [SerializeField]
    private float _lifetime = 5f;

    private int _currentHealth;
    private float _currentLifetime;
    private Vector3 _targetPosition; // Example: enemy moves towards a target

    void Update()
    {
        // Prototypes should not execute game logic. Only active clones do.
        if (IsPrototype) return; 
        if (!gameObject.activeSelf) return; // Only process if active in hierarchy

        // Example: Move towards a target position.
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);

        // Example: Decrease lifetime and return to pool when time runs out.
        _currentLifetime -= Time.deltaTime;
        if (_currentLifetime <= 0)
        {
            Debug.Log($"{gameObject.name} lifetime ended. Returning to pool.");
            ReturnToPool();
        }
    }

    /// <summary>
    /// Overrides OnActivate to set initial state when the enemy is taken from the pool.
    /// This ensures the clone is ready for immediate use.
    /// </summary>
    public override void OnActivate()
    {
        base.OnActivate(); // Call base implementation to set GameObject active.
        
        _currentLifetime = _lifetime; // Reset lifetime
        _currentHealth = _maxHealth;   // Reset health
        
        // Example: Set a random target position within a small range.
        _targetPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        // You might want to assign a player's position or a predefined path here.
        
        Debug.Log($"Enemy {gameObject.name} activated! Health: {_currentHealth}. Moving to {_targetPosition}.");
    }

    /// <summary>
    /// Overrides OnDeactivate to clean up or reset state when the enemy is returned to the pool.
    /// This ensures it's in a clean state for next activation.
    /// </summary>
    public override void OnDeactivate()
    {
        base.OnDeactivate(); // Call base implementation to set GameObject inactive.
        // Any specific cleanup, e.g., stop particle systems, reset physics, clear references.
        // Debug.Log($"Enemy {gameObject.name} deactivated."); // Uncomment for debugging deactivation
    }

    /// <summary>
    /// Example method for the enemy to take damage.
    /// </summary>
    /// <param name="damage">Amount of damage to take.</param>
    public void TakeDamage(int damage)
    {
        if (IsPrototype) return; // Prototypes don't take damage.

        _currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Current Health: {_currentHealth}.");
        
        if (_currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} defeated! Returning to pool.");
            ReturnToPool(); // Enemy defeated, return to pool.
        }
    }
}
```

**5. `ExampleSpawner.cs`**
(Demonstrates how to use the `ShadowCloneSystem` to spawn enemies)

```csharp
using UnityEngine;

/// <summary>
/// An example script demonstrating how to use the ShadowCloneManager
/// to spawn and manage 'ShadowCloneableObject' instances.
/// This spawner periodically requests an 'ExampleEnemy' clone from the system.
/// </summary>
public class ExampleSpawner : MonoBehaviour
{
    [Tooltip("The PrototypeID of the enemy to spawn. This must match an ID in your ShadowCloneManager's prototypes.")]
    [SerializeField]
    private string _enemyPrototypeID = "ExampleEnemy"; // Default ID, ensure it matches your prefab.

    [Tooltip("Interval (in seconds) between spawning new enemies.")]
    [SerializeField]
    private float _spawnInterval = 2f;

    private float _spawnTimer;

    void Start()
    {
        _spawnTimer = _spawnInterval; // Start timer to spawn the first enemy after interval.

        // Check if the ShadowCloneManager exists. It's crucial for the system to work.
        if (ShadowCloneManager.Instance == null)
        {
            Debug.LogError("ShadowCloneManager not found in the scene! Please ensure it's set up correctly.", this);
            enabled = false; // Disable spawner if manager is missing.
        }
        else
        {
            Debug.Log($"ExampleSpawner started. Will spawn '{_enemyPrototypeID}' every {_spawnInterval} seconds.");
        }
    }

    void Update()
    {
        if (ShadowCloneManager.Instance == null) return; // Ensure manager is still available.

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            SpawnEnemy();
            _spawnTimer = _spawnInterval; // Reset timer for next spawn.
        }
    }

    /// <summary>
    /// Requests an enemy clone from the ShadowCloneSystem and positions it.
    /// </summary>
    void SpawnEnemy()
    {
        // Get a clone from the ShadowCloneSystem using its registered PrototypeID.
        // This will either dequeue an existing inactive clone or create a new one.
        GameObject enemyGO = ShadowCloneManager.Instance.GetClone(_enemyPrototypeID);

        if (enemyGO != null)
        {
            // Position the spawned enemy near the spawner.
            enemyGO.transform.position = transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            // Rotate randomly for visual variety.
            enemyGO.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // You can also get the component and call specific methods if needed.
            ExampleEnemy enemyComponent = enemyGO.GetComponent<ExampleEnemy>();
            if (enemyComponent != null)
            {
                // Optionally, apply initial unique settings here if needed,
                // otherwise, OnActivate() handles common resets.
                // e.g., enemyComponent.SetTarget(somePlayerTransform.position);
            }
            Debug.Log($"Spawned {enemyGO.name}. Active clones: {ShadowCloneManager.Instance.GetActiveCloneCount(_enemyPrototypeID)}, Pooled: {ShadowCloneManager.Instance.GetPooledCloneCount(_enemyPrototypeID)}");
        }
        else
        {
            Debug.LogWarning($"Failed to spawn enemy with ID '{_enemyPrototypeID}'. Check ShadowCloneManager setup.");
        }
    }

    // You could also add a button in the Inspector to spawn manually for testing.
    [ContextMenu("Spawn Single Enemy")]
    void SpawnSingleEnemy()
    {
        SpawnEnemy();
    }
}
```

---

### Unity Setup Instructions for ShadowCloneSystem

Follow these steps to get the example working in your Unity project:

1.  **Create C# Scripts**: Create five new C# scripts in your Unity project (e.g., in a `Scripts` folder) and name them exactly as above:
    *   `ICloneablePrototype.cs`
    *   `ShadowCloneableObject.cs`
    *   `ShadowCloneManager.cs`
    *   `ExampleEnemy.cs`
    *   `ExampleSpawner.cs`
    Copy and paste the corresponding code into each file.

2.  **Setup the `ShadowCloneManager`**:
    *   Create an Empty GameObject in your scene (e.g., `Hierarchy -> Create Empty`).
    *   Rename it to `ShadowCloneManager`.
    *   Drag and drop the `ShadowCloneManager.cs` script onto this `ShadowCloneManager` GameObject in the Inspector.

3.  **Create the `ExampleEnemy` Prototype Prefab**:
    *   Create another Empty GameObject in your scene.
    *   Rename it to `ExampleEnemyPrototype`.
    *   Add a visual element: `Add Component -> 3D Object -> Cube` (or any other mesh/sprite). Position and scale it as desired (e.g., scale Y to 0.5 to make it flat, or keep it as a cube).
    *   Drag and drop the `ExampleEnemy.cs` script onto this `ExampleEnemyPrototype` GameObject.
    *   In the `ExampleEnemy` script's Inspector, set the `Prototype ID` field to **`ExampleEnemy`** (this is crucial for the spawner to find it). Adjust `Speed`, `Max Health`, and `Lifetime` as you wish.
    *   Drag the `ExampleEnemyPrototype` GameObject from the Hierarchy into your Project window (e.g., into a `Prefabs` folder) to create a prefab.
    *   Delete the `ExampleEnemyPrototype` GameObject from your Hierarchy (the original prefab in the Project window is what the manager uses).

4.  **Configure `ShadowCloneManager` with the Prototype**:
    *   Select the `ShadowCloneManager` GameObject in your scene again.
    *   In its Inspector, locate the `_Prototypes` list.
    *   Click the "plus" button (`+`) to add a new slot.
    *   Drag your `ExampleEnemyPrototype` prefab (from your Project window's `Prefabs` folder) into the newly created slot in the `_Prototypes` list.
    *   You can adjust the `Initial Pool Size` (e.g., `10`) to pre-spawn more enemies at startup.

5.  **Setup the `ExampleSpawner`**:
    *   Create a new Empty GameObject in your scene.
    *   Rename it to `EnemySpawner`.
    *   Drag and drop the `ExampleSpawner.cs` script onto this `EnemySpawner` GameObject.
    *   In the `ExampleSpawner` script's Inspector, ensure the `Enemy Prototype ID` field is set to **`ExampleEnemy`** (matching the ID you set in the prototype prefab).
    *   Adjust the `Spawn Interval` as desired (e.g., `1`).
    *   Position the `EnemySpawner` GameObject in your scene where you want the enemies to appear.

6.  **Run the Scene**:
    *   Press the Play button in the Unity Editor.
    *   You should see `ExampleEnemy` clones appearing at the spawner's location, moving towards a random target, and then disappearing after their lifetime (or if they had a `TakeDamage` call). They are not destroyed but returned to the pool and deactivated, ready for reuse.
    *   Observe the console logs for activation/deactivation messages and pool status.

This setup provides a complete, practical demonstration of the ShadowCloneSystem design pattern in Unity, showcasing how to efficiently manage game objects using prototypes and object pooling.