// Unity Design Pattern Example: AdvancedPoolingSystem
// This script demonstrates the AdvancedPoolingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Advanced Pooling System' is a design pattern used in game development to optimize performance by reusing frequently created and destroyed objects instead of instantiating and destroying them from scratch. This significantly reduces garbage collection overhead and CPU spikes, leading to smoother gameplay.

This example provides a complete, self-contained C# script for Unity, demonstrating a robust and flexible Advanced Pooling System. It includes a central `AdvancedPoolManager`, a `PoolableObject` component for items to be pooled, and example `Bullet` and `BulletSpawner` classes to show practical usage.

---

### Key Concepts of the Advanced Pooling System:

1.  **`AdvancedPoolManager` (Singleton)**:
    *   **Central Hub**: Manages all object pools. It's a singleton, meaning only one instance exists throughout the game, providing a global access point.
    *   **Configurable Pools**: Allows defining different object pools (e.g., for bullets, enemies, particles) directly in the Unity Inspector, specifying initial size, whether they can grow, and maximum size.
    *   **Dynamic Growth (Optional)**: If a pool runs out of available objects and `canGrow` is true, it will instantiate new ones to meet demand. This prevents hitches when usage exceeds initial capacity.
    *   **Hierarchy Cleanliness**: Creates dedicated parent GameObjects for each pool (`_PoolManager/Bullets`, `_PoolManager/FX`), keeping the Unity Hierarchy organized.
    *   **API**: Provides `Spawn` and `Despawn` methods for easy interaction.

2.  **`PoolableObject` (Component)**:
    *   **Identifiable**: This component must be attached to any prefab that you want to pool. It stores a reference to its original prefab, which helps the `AdvancedPoolManager` determine which pool to return it to.
    *   **Lifecycle Callbacks**: Provides `OnSpawned` and `OnDespawned` events. Other components on the pooled object (like a `Bullet` script) can subscribe to these events to reset their state (e.g., set health, velocity, reset timers) when activated or deactivated.
    *   **`ReturnToPool()` Method**: A convenient method for the pooled object itself to return to its pool.
    *   **Automatic Despawn (Optional)**: Includes logic to automatically return the object to the pool after a specified `autoDespawnTime` once it's spawned. This is useful for temporary effects or projectiles.

3.  **Example Usage (`Bullet` and `BulletSpawner`)**:
    *   **`Bullet`**: Demonstrates how a script on a pooled object can reset its state (`velocity`, `timer`) when `OnSpawned` is called and how it might use `ReturnToPool()` on its own (e.g., when it hits something or goes out of bounds).
    *   **`BulletSpawner`**: Shows how to request an object from the pool (`AdvancedPoolManager.Instance.Spawn()`) and position/rotate it.

---

### How to Use This Script in Unity:

1.  **Create a New C# Script**: Name it `AdvancedPoolingSystem.cs` and copy all the code below into it.
2.  **Create an Empty GameObject**: In your scene, create an empty GameObject and name it `_PoolManager`.
3.  **Attach Script**: Attach the `AdvancedPoolManager` component from `AdvancedPoolingSystem.cs` to the `_PoolManager` GameObject.
4.  **Configure Pools**:
    *   In the Inspector for `_PoolManager`, you'll see a `Pool Configurations` list.
    *   Add new elements to the list for each type of object you want to pool (e.g., one for "Bullet").
    *   **Prefab**: Drag your prefab (e.g., a "Bullet" prefab you create in step 5) into this slot.
    *   **Initial Size**: Set how many objects to pre-instantiate (e.g., 20).
    *   **Can Grow**: Check this if the pool should create more objects if it runs out.
    *   **Max Size**: If `Can Grow` is true, set a maximum limit (e.g., 100).
5.  **Create a Poolable Prefab (e.g., a Bullet)**:
    *   Create a simple 3D object (e.g., a Sphere or Cube).
    *   **Add `PoolableObject` Component**: Attach the `PoolableObject` component from `AdvancedPoolingSystem.cs` to this object.
        *   **Auto Despawn Time**: Set a time (e.g., 3.0 seconds) if you want it to automatically return to the pool after that duration.
    *   **Add `Bullet` Component**: Attach the `Bullet` component from `AdvancedPoolingSystem.cs` to this object.
    *   **Make it a Prefab**: Drag this configured GameObject from the Hierarchy into your Project window to create a prefab. Delete the instance from the Hierarchy.
6.  **Create a Spawner (e.g., a Gun)**:
    *   Create an empty GameObject, name it `Gun` (or `BulletSpawner`).
    *   **Attach `BulletSpawner` Component**: Attach the `BulletSpawner` component from `AdvancedPoolingSystem.cs` to it.
    *   **Assign Bullet Prefab**: Drag your "Bullet" prefab from the Project window into the `Bullet Prefab` slot of the `BulletSpawner` component.
7.  **Run the Scene**:
    *   Press Play.
    *   Click your mouse to spawn bullets. Observe the Hierarchy under `_PoolManager/Bullet_Pool` – objects will appear and disappear.
    *   Check your Console for debug messages about spawning and despawning.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

// This script contains a complete Advanced Pooling System for Unity.
// It includes:
// 1. AdvancedPoolManager: A singleton to manage all object pools.
// 2. PoolableObject: A component for objects that can be pooled, providing lifecycle events.
// 3. Bullet: An example component to demonstrate a pooled object's behavior.
// 4. BulletSpawner: An example component to demonstrate how to spawn pooled objects.

// Regions are used to organize the code within this single file.

#region AdvancedPoolManager
/// <summary>
/// The central manager for all object pools.
/// Implements a singleton pattern for easy access throughout the application.
/// It pre-populates pools, manages object retrieval and return,
/// and handles dynamic pool growth.
/// </summary>
public class AdvancedPoolManager : MonoBehaviour
{
    // Singleton instance to allow global access.
    public static AdvancedPoolManager Instance { get; private set; }

    [Serializable]
    public struct PoolConfig
    {
        public GameObject prefab;
        [Min(0)] public int initialSize;
        public bool canGrow;
        [Min(0)] public int maxSize; // Only applies if canGrow is true. 0 means no max.
    }

    [Header("Pool Configurations")]
    [Tooltip("Define the prefabs to pool, their initial sizes, and growth settings.")]
    public List<PoolConfig> poolConfigurations = new List<PoolConfig>();

    // Dictionary to hold all active pools, keyed by their prefab.
    private Dictionary<GameObject, ObjectPool> _pools = new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("AdvancedPoolManager: Found multiple instances, destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Keep the manager alive across scene changes if needed (optional)
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializePools();
    }

    /// <summary>
    /// Initializes all pools based on the configurations provided in the Inspector.
    /// </summary>
    private void InitializePools()
    {
        foreach (var config in poolConfigurations)
        {
            if (config.prefab == null)
            {
                Debug.LogError("AdvancedPoolManager: Pool configuration has a null prefab!", this);
                continue;
            }

            if (_pools.ContainsKey(config.prefab))
            {
                Debug.LogWarning($"AdvancedPoolManager: Duplicate pool configuration for '{config.prefab.name}'. Skipping.", this);
                continue;
            }

            // Create a parent GameObject for this specific pool to keep the hierarchy clean.
            GameObject poolParent = new GameObject($"{config.prefab.name}_Pool");
            poolParent.transform.SetParent(this.transform); // Parent under the AdvancedPoolManager

            ObjectPool newPool = new ObjectPool(config.prefab, config.initialSize, config.canGrow, config.maxSize, poolParent.transform);
            _pools.Add(config.prefab, newPool);

            Debug.Log($"AdvancedPoolManager: Initialized pool for '{config.prefab.name}' with initial size {config.initialSize}.");
        }
    }

    /// <summary>
    /// Spawns an object from the pool at a given position and rotation.
    /// </summary>
    /// <param name="prefab">The prefab to retrieve from the pool.</param>
    /// <param name="position">The world position to spawn the object.</param>
    /// <param name="rotation">The world rotation to spawn the object.</param>
    /// <returns>The spawned GameObject, or null if the prefab is not pooled or max size reached.</returns>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(prefab, out ObjectPool pool))
        {
            Debug.LogError($"AdvancedPoolManager: Attempted to spawn '{prefab.name}' but it is not configured in any pool!", this);
            return null;
        }

        GameObject obj = pool.Get();
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Notify the PoolableObject component (and any subscribers) that it has been spawned.
            PoolableObject poolable = obj.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                poolable.originalPrefab = prefab; // Ensure it knows its original prefab for return
                poolable.NotifySpawned();
            }
            else
            {
                Debug.LogWarning($"AdvancedPoolManager: Spawned object '{prefab.name}' does not have a PoolableObject component!", obj);
            }
        }
        else
        {
            Debug.LogWarning($"AdvancedPoolManager: Failed to get '{prefab.name}' from pool. Max size reached or pool empty and cannot grow.", this);
        }

        return obj;
    }

    /// <summary>
    /// Despawns an object and returns it to its respective pool.
    /// </summary>
    /// <param name="objToDespawn">The GameObject to despawn.</param>
    public void Despawn(GameObject objToDespawn)
    {
        if (objToDespawn == null)
        {
            Debug.LogWarning("AdvancedPoolManager: Attempted to despawn a null object.", this);
            return;
        }

        PoolableObject poolable = objToDespawn.GetComponent<PoolableObject>();
        if (poolable == null || poolable.originalPrefab == null)
        {
            Debug.LogError($"AdvancedPoolManager: Attempted to despawn '{objToDespawn.name}' which is not a poolable object or lacks originalPrefab reference. Destroying instead.", objToDespawn);
            Destroy(objToDespawn); // Destroy unmanaged objects to prevent leaks
            return;
        }

        if (!_pools.TryGetValue(poolable.originalPrefab, out ObjectPool pool))
        {
            Debug.LogError($"AdvancedPoolManager: No pool found for original prefab '{poolable.originalPrefab.name}' of object '{objToDespawn.name}'. Destroying instead.", objToDespawn);
            Destroy(objToDespawn); // Destroy objects from unknown pools
            return;
        }

        poolable.NotifyDespawned(); // Notify the PoolableObject (and subscribers) that it has been despawned.
        pool.Return(objToDespawn);
    }


    /// <summary>
    /// Represents a single pool of GameObjects for a specific prefab.
    /// Handles the internal logic for getting, returning, and managing objects.
    /// </summary>
    private class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Queue<GameObject> _availableObjects;
        private readonly HashSet<GameObject> _activeObjects; // To keep track of currently active objects and prevent double-returns
        private readonly Transform _parentTransform; // Parent for pooled objects in the hierarchy
        private readonly bool _canGrow;
        private readonly int _maxSize;
        private int _currentPoolSize; // Tracks instantiated objects, active or inactive

        public ObjectPool(GameObject prefab, int initialSize, bool canGrow, int maxSize, Transform parentTransform)
        {
            _prefab = prefab;
            _availableObjects = new Queue<GameObject>(initialSize);
            _activeObjects = new HashSet<GameObject>();
            _parentTransform = parentTransform;
            _canGrow = canGrow;
            _maxSize = maxSize;
            _currentPoolSize = 0;

            Prepopulate(initialSize);
        }

        /// <summary>
        /// Instantiates and adds objects to the pool up to the initial size.
        /// </summary>
        /// <param name="count">The number of objects to create.</param>
        private void Prepopulate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_maxSize > 0 && _currentPoolSize >= _maxSize)
                {
                    Debug.LogWarning($"Pool for '{_prefab.name}' reached max size {_maxSize} during pre-population. Stopped at {_currentPoolSize} objects.");
                    break;
                }
                AddObjectToPool();
            }
        }

        /// <summary>
        /// Creates a new instance of the prefab and adds it to the available queue.
        /// Sets the new object as inactive and parents it.
        /// </summary>
        private void AddObjectToPool()
        {
            GameObject newObj = GameObject.Instantiate(_prefab);
            newObj.name = $"{_prefab.name} (Pooled)"; // Rename for clarity in hierarchy
            newObj.transform.SetParent(_parentTransform);
            newObj.SetActive(false); // Inactive until needed

            // Attach PoolableObject component if not already present, and set its original prefab.
            // This is primarily a safeguard; prefabs should have it pre-configured.
            PoolableObject poolable = newObj.GetComponent<PoolableObject>();
            if (poolable == null)
            {
                poolable = newObj.AddComponent<PoolableObject>();
                Debug.LogWarning($"Pool: Added missing PoolableObject to '{newObj.name}' during instantiation.", newObj);
            }
            poolable.originalPrefab = _prefab; // Crucial for returning to the correct pool

            _availableObjects.Enqueue(newObj);
            _currentPoolSize++;
        }

        /// <summary>
        /// Retrieves an object from the pool. If no objects are available,
        /// it may create a new one if the pool is configured to grow.
        /// </summary>
        /// <returns>An available GameObject, or null if none can be retrieved.</returns>
        public GameObject Get()
        {
            GameObject obj;
            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
            }
            else if (_canGrow && (_maxSize == 0 || _currentPoolSize < _maxSize))
            {
                // Pool is empty but can grow, so create a new one
                AddObjectToPool();
                obj = _availableObjects.Dequeue(); // Get the newly created object
                Debug.Log($"Pool for '{_prefab.name}' grew, new size: {_currentPoolSize}.");
            }
            else
            {
                // Pool is empty and cannot grow or has reached max size
                return null;
            }

            _activeObjects.Add(obj); // Mark as active
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool, making it available for reuse.
        /// </summary>
        /// <param name="obj">The GameObject to return.</param>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!_activeObjects.Contains(obj))
            {
                // This object was either already returned or was never gotten from this pool.
                Debug.LogWarning($"Attempted to return '{obj.name}' to pool, but it was not marked as active by this pool. Destroying it to prevent duplicates.", obj);
                GameObject.Destroy(obj);
                return;
            }

            _activeObjects.Remove(obj); // Mark as inactive
            obj.SetActive(false);
            obj.transform.SetParent(_parentTransform); // Ensure it's parented correctly
            _availableObjects.Enqueue(obj);
        }

        // Optional: Method to get the count of active objects
        public int GetActiveCount() => _activeObjects.Count;

        // Optional: Method to get the count of available (inactive) objects
        public int GetAvailableCount() => _availableObjects.Count;

        // Optional: Method to get the total size of the pool
        public int GetTotalSize() => _currentPoolSize;
    }
}
#endregion

#region PoolableObject
/// <summary>
/// This component must be attached to any GameObject prefab that is intended to be pooled.
/// It provides mechanisms for identifying the object's original prefab for returning to the correct pool,
/// and offers lifecycle events (OnSpawned, OnDespawned) for other components to subscribe to.
/// It also handles optional automatic despawning after a set duration.
/// </summary>
public class PoolableObject : MonoBehaviour
{
    // IMPORTANT: This field is set by the AdvancedPoolManager when the object is first instantiated
    // or when it's returned to the pool. It links the instance back to its original prefab.
    [HideInInspector]
    public GameObject originalPrefab;

    // Actions that other scripts can subscribe to for initialization/reset logic.
    public event Action OnSpawned;
    public event Action OnDespawned;

    [Header("Auto Despawn Settings")]
    [Tooltip("If greater than 0, this object will automatically return to its pool after this many seconds once spawned.")]
    public float autoDespawnTime = 0f;

    private Coroutine _autoDespawnCoroutine;
    private bool _isSpawned = false; // To prevent multiple despawn calls or issues

    /// <summary>
    /// Called internally by AdvancedPoolManager when this object is retrieved from the pool and activated.
    /// Notifies subscribers and starts auto-despawn timer if configured.
    /// </summary>
    internal void NotifySpawned()
    {
        if (_isSpawned)
        {
            Debug.LogWarning($"PoolableObject: '{gameObject.name}' notified as spawned but was already active. This might indicate an issue with pool management.", this);
            StopAutoDespawn(); // Ensure old timer is stopped if somehow re-spawned without despawn
        }

        _isSpawned = true;
        OnSpawned?.Invoke();

        if (autoDespawnTime > 0f)
        {
            _autoDespawnCoroutine = StartCoroutine(AutoDespawnTimer());
        }
    }

    /// <summary>
    /// Called internally by AdvancedPoolManager when this object is returned to the pool and deactivated.
    /// Notifies subscribers and stops any active auto-despawn timer.
    /// </summary>
    internal void NotifyDespawned()
    {
        if (!_isSpawned)
        {
            Debug.LogWarning($"PoolableObject: '{gameObject.name}' notified as despawned but was not active. This might indicate an issue with pool management.", this);
            return;
        }

        _isSpawned = false;
        StopAutoDespawn();
        OnDespawned?.Invoke();
    }

    /// <summary>
    /// Initiates the return of this object to its pool.
    /// This method is typically called by the object itself (e.g., a bullet hitting a wall)
    /// or by other game logic when the object is no longer needed.
    /// </summary>
    public void ReturnToPool()
    {
        if (_isSpawned) // Only return if currently active from a pool
        {
            AdvancedPoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Debug.LogWarning($"PoolableObject: Attempted to return '{gameObject.name}' to pool, but it was not marked as spawned. Destroying instead to prevent leaks.", this);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Coroutine for automatic despawning after 'autoDespawnTime' seconds.
    /// </summary>
    private IEnumerator AutoDespawnTimer()
    {
        yield return new WaitForSeconds(autoDespawnTime);
        if (_isSpawned) // Ensure it's still active when the timer finishes
        {
            ReturnToPool();
        }
        _autoDespawnCoroutine = null; // Clear reference once finished
    }

    /// <summary>
    /// Stops any active auto-despawn coroutine.
    /// </summary>
    private void StopAutoDespawn()
    {
        if (_autoDespawnCoroutine != null)
        {
            StopCoroutine(_autoDespawnCoroutine);
            _autoDespawnCoroutine = null;
        }
    }

    // Ensure the object returns to the pool if it's destroyed without being despawned properly
    private void OnDestroy()
    {
        if (_isSpawned)
        {
            Debug.LogWarning($"PoolableObject: '{gameObject.name}' was destroyed while still active. This indicates a potential issue or unmanaged destruction outside the pooling system. Please use ReturnToPool() or AdvancedPoolManager.Despawn().", this);
            // Optionally, try to remove from active objects in pool manager if possible, though it's safer to have pool manager handle all returns.
            // For now, just log and let it be destroyed.
        }
    }
}
#endregion

#region Example_Bullet
/// <summary>
/// Example component demonstrating how a pooled object (a bullet) can use the PoolableObject lifecycle events.
/// This script handles bullet movement and its state when spawned/despawned.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public float damage = 10f; // Example property

    private PoolableObject _poolableObject;
    private Rigidbody _rb;

    private void Awake()
    {
        _poolableObject = GetComponent<PoolableObject>();
        _rb = GetComponent<Rigidbody>();

        if (_poolableObject == null)
        {
            Debug.LogError("Bullet: Missing PoolableObject component! This bullet cannot be pooled correctly.", this);
            enabled = false; // Disable if not poolable
            return;
        }

        // Subscribe to the lifecycle events provided by PoolableObject.
        _poolableObject.OnSpawned += OnBulletSpawned;
        _poolableObject.OnDespawned += OnBulletDespawned;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks if this object is ever destroyed directly
        // (though in a pooling system, it should primarily be deactivated, not destroyed).
        if (_poolableObject != null)
        {
            _poolableObject.OnSpawned -= OnBulletSpawned;
            _poolableObject.OnDespawned -= OnBulletDespawned;
        }
    }

    /// <summary>
    /// Called when the bullet is spawned (retrieved from the pool and activated).
    /// Resets its state (e.g., velocity).
    /// </summary>
    private void OnBulletSpawned()
    {
        if (_rb != null)
        {
            _rb.velocity = transform.forward * speed;
            _rb.angularVelocity = Vector3.zero; // Clear any rotational velocity
        }
        else
        {
            // If no Rigidbody, we might move it manually in Update.
            // For this example, let's assume Rigidbody for physics.
        }
        Debug.Log($"Bullet '{gameObject.name}' spawned with speed {speed}.", this);
    }

    /// <summary>
    /// Called when the bullet is despawned (returned to the pool and deactivated).
    /// Clears its state (e.g., velocity) and performs cleanup.
    /// </summary>
    private void OnBulletDespawned()
    {
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        // Example cleanup: stop particle systems, reset visual effects, etc.
        Debug.Log($"Bullet '{gameObject.name}' despawned.", this);
    }

    // Example of collision detection for a bullet
    private void OnCollisionEnter(Collision collision)
    {
        // In a real game, you would check tags, apply damage, play effects, etc.
        Debug.Log($"Bullet '{gameObject.name}' hit '{collision.gameObject.name}'. Returning to pool.", this);

        // Immediately return the bullet to the pool after it hits something.
        // This takes precedence over the autoDespawnTime.
        _poolableObject.ReturnToPool();
    }
}
#endregion

#region Example_BulletSpawner
/// <summary>
/// Example component demonstrating how to use the AdvancedPoolManager to spawn pooled objects.
/// Spawns a bullet when the mouse button is clicked.
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The prefab of the bullet to spawn. This prefab MUST have a PoolableObject component.")]
    public GameObject bulletPrefab;

    public Transform spawnPoint; // The point from which bullets will be spawned.

    private void Start()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletSpawner: Bullet Prefab is not assigned! This spawner will not work.", this);
            enabled = false;
            return;
        }

        if (bulletPrefab.GetComponent<PoolableObject>() == null)
        {
            Debug.LogError($"BulletSpawner: Assigned bullet prefab '{bulletPrefab.name}' does not have a PoolableObject component. It cannot be pooled correctly!", this);
            enabled = false;
            return;
        }

        if (spawnPoint == null)
        {
            spawnPoint = this.transform; // Default to spawner's transform if none is assigned.
        }
    }

    private void Update()
    {
        // Example: Spawn a bullet when the left mouse button is clicked.
        if (Input.GetMouseButtonDown(0))
        {
            SpawnBullet();
        }
    }

    /// <summary>
    /// Requests a bullet from the AdvancedPoolManager and spawns it.
    /// </summary>
    private void SpawnBullet()
    {
        if (AdvancedPoolManager.Instance == null)
        {
            Debug.LogError("BulletSpawner: AdvancedPoolManager is not initialized or found in the scene!", this);
            return;
        }

        // Request a bullet from the pool manager.
        // The manager handles whether it's an existing object or a new one (if pool can grow).
        GameObject spawnedBullet = AdvancedPoolManager.Instance.Spawn(
            bulletPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (spawnedBullet != null)
        {
            Debug.Log($"BulletSpawner: Successfully spawned '{spawnedBullet.name}'.", this);
            // Additional logic for the spawned bullet can go here if needed,
            // though most setup should be handled by the Bullet's OnSpawned event.
        }
        else
        {
            Debug.LogWarning($"BulletSpawner: Failed to spawn bullet. Pool might be empty and unable to grow, or max size reached.", this);
        }
    }
}
#endregion
```