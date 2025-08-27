// Unity Design Pattern Example: ObjectPooling
// This script demonstrates the ObjectPooling pattern in Unity
// Generated automatically - ready to use in your Unity project

The Object Pooling design pattern is a creational pattern used in game development to efficiently manage objects that are frequently created and destroyed, such as bullets, enemies, or particle effects. Instead of instantiating new objects and destroying old ones, which can cause performance spikes due to memory allocation and garbage collection, Object Pooling pre-allocates a pool of objects and reuses them.

**Benefits:**
*   **Performance:** Reduces the overhead of `Instantiate()` and `Destroy()`, leading to smoother gameplay.
*   **Memory Management:** Decreases garbage collection pressure by reusing existing memory.
*   **Control:** Provides better control over the lifecycle of objects.

**How it Works:**
1.  **Initialization:** A set number of objects (e.g., bullets) are created at the start of the game (or when the pool is first needed) and deactivated. They are stored in a collection (e.g., a `Queue`).
2.  **Requesting an Object:** When an object is needed, it's taken from the pool's collection, activated, and positioned in the game world.
3.  **Returning an Object:** When the object is no longer needed (e.g., a bullet goes off-screen, an enemy is defeated), it's deactivated and returned to the pool's collection, ready for reuse.
4.  **Expansion (Optional):** If the pool runs out of available objects, it can optionally create new ones to meet demand, or it can simply return `null` or a default object.

This example provides three scripts:
1.  **`ObjectPool.cs`**: A generic component that manages a pool for a specific prefab.
2.  **`PooledBullet.cs`**: An example of a `MonoBehaviour` script that would be attached to a prefab to be pooled (e.g., a bullet). It defines its lifetime and how it interacts with the pool.
3.  **`BulletSpawner.cs`**: An example script that demonstrates how to use the `ObjectPool` to retrieve and return objects.

---

## 1. `ObjectPool.cs`

This script manages a pool of `GameObject`s for a single specific prefab. Attach this script to an empty GameObject in your scene, and assign the prefab you want to pool.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action

/// <summary>
/// A generic Object Pooling system for Unity.
/// This script manages a pool of GameObjects based on a single prefab.
/// Attach this script to an empty GameObject in your scene for each type of object you want to pool.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    // --- Configuration Parameters ---
    [Tooltip("The prefab GameObject that this pool will manage.")]
    [SerializeField]
    private GameObject _prefabToPool;

    [Tooltip("The initial number of objects to create and store in the pool on Awake.")]
    [SerializeField]
    private int _poolSize = 10;

    [Tooltip("If true, the pool will create new objects if it runs out of available ones. " +
             "If false, GetPooledObject() will return null when the pool is empty.")]
    [SerializeField]
    private bool _shouldExpand = true;

    // --- Internal State ---
    // A Queue is ideal for pooling as it provides efficient O(1) enqueue and dequeue operations,
    // following a First-In, First-Out (FIFO) principle.
    private Queue<GameObject> _availableObjects = new Queue<GameObject>();

    // A List to keep track of all objects ever created by this pool (both active and inactive).
    // This is useful for cleanup, or for robustly checking if an object belongs to this pool.
    private List<GameObject> _allPooledObjects = new List<GameObject>();

    /// <summary>
    /// Public property to access the prefab this pool is managing.
    /// Useful for identifying the pool or for systems that need to know the type.
    /// </summary>
    public GameObject PooledPrefab => _prefabToPool;

    void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// Initializes the object pool by creating the initial set of objects
    /// and adding them to the available objects queue in an inactive state.
    /// </summary>
    private void InitializePool()
    {
        if (_prefabToPool == null)
        {
            Debug.LogError("ObjectPool: PrefabToPool is not assigned. Please assign a prefab in the inspector.", this);
            enabled = false; // Disable the component if it can't function
            return;
        }

        // Create the initial 'poolSize' number of objects
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = InstantiateNewObject();
            // Immediately return the new object to the pool so it's ready for use (deactivated).
            ReturnObjectToPool(obj);
        }

        Debug.Log($"ObjectPool: Initialized pool for '{_prefabToPool.name}' with {_poolSize} objects.", this);
    }

    /// <summary>
    /// Instantiates a new object from the prefab and parents it under this ObjectPool's GameObject
    /// for better hierarchy organization.
    /// </summary>
    /// <returns>The newly instantiated GameObject.</returns>
    private GameObject InstantiateNewObject()
    {
        // Instantiate the prefab, parent it to this pool's GameObject
        // This keeps the hierarchy clean and makes it easy to find all pooled objects.
        GameObject obj = Instantiate(_prefabToPool, transform);
        obj.name = _prefabToPool.name + " (Pooled)"; // Renaming for clarity in hierarchy
        _allPooledObjects.Add(obj); // Keep track of all objects ever created by this pool
        return obj;
    }

    /// <summary>
    /// Retrieves an object from the pool.
    /// If an object is available in the queue, it's dequeued and activated.
    /// If no objects are available and _shouldExpand is true, a new object is created.
    /// Otherwise, if the pool is empty and cannot expand, null is returned.
    /// </summary>
    /// <returns>An active GameObject from the pool, or null if no object is available and the pool cannot expand.</returns>
    public GameObject GetPooledObject()
    {
        GameObject obj;

        // 1. Try to get an object from the queue of available objects
        if (_availableObjects.Count > 0)
        {
            obj = _availableObjects.Dequeue();
        }
        // 2. If no objects are available, check if the pool is allowed to expand
        else if (_shouldExpand)
        {
            obj = InstantiateNewObject(); // Create a new object
            Debug.LogWarning($"ObjectPool: Pool for '{_prefabToPool.name}' expanded. Consider increasing initial pool size if this happens frequently.", this);
        }
        // 3. If no objects are available and the pool cannot expand, return null
        else
        {
            Debug.LogWarning($"ObjectPool: No available objects for '{_prefabToPool.name}' and pool cannot expand.", this);
            return null;
        }

        // Always activate the object before returning it for use
        obj.SetActive(true);

        // Reset its parent if it was changed by the user when it was active
        // This ensures all pooled objects maintain their parentage under the pool for organization
        if (obj.transform.parent != transform)
        {
            obj.transform.SetParent(transform);
        }

        return obj;
    }

    /// <summary>
    /// Returns an object to the pool, making it available for reuse.
    /// The object will be deactivated and reparented under this pool's GameObject.
    /// A robust check ensures only objects from this pool are returned.
    /// </summary>
    /// <param name="objectToReturn">The GameObject to return to the pool.</param>
    public void ReturnObjectToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            Debug.LogWarning("ObjectPool: Attempted to return a null object to the pool.", this);
            return;
        }

        // IMPORTANT: Verify that the object truly belongs to this pool.
        // This prevents returning external objects or objects from other pools.
        if (!_allPooledObjects.Contains(objectToReturn))
        {
            Debug.LogWarning($"ObjectPool: Attempted to return an object ('{objectToReturn.name}') that was not created by this pool ('{_prefabToPool.name}'). Destroying it instead.", this);
            Destroy(objectToReturn);
            return;
        }
        
        // Ensure the object is not already in the available queue (can happen with multiple returns)
        if (_availableObjects.Contains(objectToReturn))
        {
             Debug.LogWarning($"ObjectPool: Attempted to return object '{objectToReturn.name}' which is already in the pool. Ignoring.", this);
             return;
        }


        // Deactivate the object so it's not visible or interactive in the scene
        objectToReturn.SetActive(false);

        // Reparent the object to the pool's GameObject for organization.
        // This is good practice to keep the hierarchy clean and grouped.
        objectToReturn.transform.SetParent(transform);

        // Add the object back to the queue, making it available for future requests
        _availableObjects.Enqueue(objectToReturn);
    }

    /// <summary>
    /// Clears all objects currently managed by this pool and destroys them.
    /// This is useful when transitioning between scenes or when a pool is no longer needed.
    /// </summary>
    public void ClearPool()
    {
        foreach (GameObject obj in _allPooledObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _allPooledObjects.Clear();
        _availableObjects.Clear();
        Debug.Log($"ObjectPool: Cleared all objects for '{_prefabToPool.name}'.", this);
    }

    // --- Editor-only utility for visual debugging ---
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw a small icon to easily locate the pool in the scene view
        Gizmos.DrawIcon(transform.position, "ObjectPool.png", true);
    }
    #endif
}
```

---

## 2. `PooledBullet.cs`

This script is an example of an object that will be managed by the `ObjectPool`. It handles its own behavior (moving forward) and, crucially, when to return itself to the pool. Attach this to your bullet prefab.

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// An example MonoBehaviour script for a pooled object, like a bullet.
/// This script defines the object's behavior and when it should return itself to the ObjectPool.
/// It receives a callback action from the spawner/pool to facilitate its return.
/// </summary>
public class PooledBullet : MonoBehaviour
{
    [Tooltip("How long this bullet will be active before automatically returning to the pool.")]
    [SerializeField]
    private float _lifeTime = 3.0f; // Default lifetime of 3 seconds

    [Tooltip("The speed at which the bullet moves forward.")]
    [SerializeField]
    private float _speed = 15.0f; // Default speed

    // A delegate (callback function) that will be invoked to return this object to the pool.
    // This is set externally by the entity that retrieves the bullet from the pool (e.g., the Spawner).
    private Action<GameObject> _returnToPoolAction;

    /// <summary>
    /// Initializes the pooled bullet with the necessary action to return it to the pool.
    /// This method should be called immediately after the bullet is retrieved from the pool.
    /// </summary>
    /// <param name="returnAction">The Action<GameObject> delegate to invoke when this bullet needs to be returned.</param>
    public void Initialize(Action<GameObject> returnAction)
    {
        _returnToPoolAction = returnAction;
        // You can add other initialization logic here, like setting damage, target, etc.
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// This is where we reset the bullet's state and start its lifecycle.
    /// </summary>
    void OnEnable()
    {
        // Start a countdown to automatically return the bullet to the pool after its lifetime expires.
        Invoke(nameof(Deactivate), _lifeTime);

        // Reset physics properties if this object has a Rigidbody.
        // This is crucial for reusable objects to prevent unexpected behaviors from previous uses.
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Optionally reset other states like trail renderers, particle systems, etc.
        // if (TryGetComponent<TrailRenderer>(out var tr)) tr.Clear();
        // if (TryGetComponent<ParticleSystem>(out var ps)) ps.Play();
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// This is where we clean up any pending operations.
    /// </summary>
    void OnDisable()
    {
        // Cancel any pending Invoke calls to prevent errors if the object is returned manually
        // or through another mechanism before its lifetime expires.
        CancelInvoke(nameof(Deactivate));
        
        // Optionally stop particle systems, clear trail renderers, etc.
        // if (TryGetComponent<ParticleSystem>(out var ps)) ps.Stop();
    }

    /// <summary>
    /// Moves the bullet forward each frame.
    /// </summary>
    void Update()
    {
        // Move the bullet in its local forward direction at the specified speed.
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    /// <summary>
    /// This method is called to signal that the bullet is done and should return to the pool.
    /// It invokes the stored _returnToPoolAction, passing its own GameObject.
    /// </summary>
    private void Deactivate()
    {
        // Check if the action is set to prevent NullReferenceException.
        // Invoke the action, which will typically be ObjectPool.ReturnObjectToPool.
        _returnToPoolAction?.Invoke(gameObject);
    }

    /// <summary>
    /// Example of returning the bullet to the pool upon collision.
    /// Ensure the collider is marked as 'Is Trigger' for OnTriggerEnter to work.
    /// </summary>
    /// <param name="other">The Collider that this object entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Basic example: return to pool on any trigger collision.
        // In a real game, you'd add logic to check tags or layers (e.g., "Enemy", "Wall")
        // to only deactivate on relevant collisions.
        // Example: if (other.CompareTag("Enemy") || other.CompareTag("Wall"))
        // {
        //     Deactivate();
        // }

        // For this example, let's just make it deactivate on collision with anything.
        // Only if not already deactivated or about to be (e.g. by lifetime timer)
        if (gameObject.activeInHierarchy)
        {
            Deactivate();
        }
    }
}
```

---

## 3. `BulletSpawner.cs`

This script demonstrates how to use the `ObjectPool` to spawn `PooledBullet` objects at a regular interval.

```csharp
using UnityEngine;

/// <summary>
/// An example script that demonstrates how to use the ObjectPool to spawn bullets.
/// It periodically requests a bullet from the pool, initializes it, and positions it.
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Tooltip("Reference to the ObjectPool instance that manages bullets.")]
    [SerializeField]
    private ObjectPool _bulletPool;

    [Tooltip("How often a new bullet should be spawned (in seconds).")]
    [SerializeField]
    private float _spawnInterval = 0.2f;

    [Tooltip("The Transform representing the point from which bullets will be spawned.")]
    [SerializeField]
    private Transform _spawnPoint;

    private float _nextSpawnTime; // Tracks when the next bullet should be spawned

    void Start()
    {
        // Basic error checking to ensure the spawner has a pool reference.
        if (_bulletPool == null)
        {
            Debug.LogError("BulletSpawner: Bullet Pool is not assigned. Please assign an ObjectPool in the inspector.", this);
            enabled = false; // Disable the spawner if it can't function
            return;
        }

        // If no specific spawn point is assigned, use the spawner's own transform.
        if (_spawnPoint == null)
        {
            _spawnPoint = transform;
        }

        // Initialize the time for the first spawn.
        _nextSpawnTime = Time.time;
    }

    void Update()
    {
        // Check if it's time to spawn a new bullet.
        if (Time.time >= _nextSpawnTime)
        {
            SpawnBullet();
            // Schedule the next spawn.
            _nextSpawnTime = Time.time + _spawnInterval;
        }
    }

    /// <summary>
    /// Requests a bullet from the ObjectPool, positions it, and initializes its behavior.
    /// </summary>
    private void SpawnBullet()
    {
        // 1. Request a GameObject from the pool.
        // This will either return an existing inactive object or create a new one if allowed.
        GameObject bulletGO = _bulletPool.GetPooledObject();

        if (bulletGO != null)
        {
            // 2. Position and rotate the retrieved object at the spawner's location.
            bulletGO.transform.position = _spawnPoint.position;
            bulletGO.transform.rotation = _spawnPoint.rotation;

            // 3. Get the specific component (PooledBullet) from the GameObject.
            // This component contains the logic for the bullet's behavior and its return to the pool.
            PooledBullet bullet = bulletGO.GetComponent<PooledBullet>();

            if (bullet != null)
            {
                // 4. Initialize the PooledBullet, providing the ObjectPool's ReturnObjectToPool method as a callback.
                // This allows the bullet to call back to the pool manager when it's done (e.g., after a certain lifetime or collision).
                bullet.Initialize(_bulletPool.ReturnObjectToPool);
                
                // At this point, the bullet is active and will manage its own lifecycle,
                // calling _bulletPool.ReturnObjectToPool(this.gameObject) when it's finished.
            }
            else
            {
                Debug.LogWarning($"BulletSpawner: Pooled object '{bulletGO.name}' does not have a 'PooledBullet' component. " +
                                 "It will never return to the pool on its own! Ensure your prefab has the component.", this);
                // If the pooled object doesn't have the expected component, it won't be able to return itself.
                // In such scenarios, the spawner would need to manage its lifetime (e.g., via a DelayedCallback or Coroutine).
                // For this example, we assume PooledBullet is always present on the pooled prefab.
            }

            // Example: Log the number of active bullets (illustrative, not efficient for high counts)
            // Debug.Log($"BulletSpawner: Spawned bullet '{bulletGO.name}'. Current active bullets: {GameObject.FindObjectsOfType<PooledBullet>().Length}", this);
        }
        else
        {
            Debug.LogWarning("BulletSpawner: Failed to get a bullet from the pool. " +
                             "The pool might be exhausted and configured not to expand.", this);
        }
    }
}
```

---

## Unity Setup Instructions:

To get this example running in your Unity project:

1.  **Create your Bullet Prefab:**
    *   In a new or existing Unity scene, create a 3D object (e.g., a **Cube** or **Sphere**). Rename it `BulletPrefab`.
    *   **Add Components:**
        *   Add a `Rigidbody` component. **Uncheck "Use Gravity"** so it flies straight.
        *   Add a `Box Collider` (if it's a cube) or `Sphere Collider` (if it's a sphere). **Check "Is Trigger"** so `OnTriggerEnter` works.
        *   Attach the `PooledBullet.cs` script to `BulletPrefab`.
    *   **Drag to Project:** Drag the `BulletPrefab` from your Hierarchy window into your Project window (e.g., into a `Prefabs` folder) to create a reusable prefab asset.
    *   **Delete from Scene:** Delete `BulletPrefab` from the Hierarchy.

2.  **Create the Object Pool Manager:**
    *   Create an empty GameObject in your scene and rename it `BulletPoolManager`.
    *   Attach the `ObjectPool.cs` script to this GameObject.
    *   In the Inspector for `BulletPoolManager`:
        *   Drag your `BulletPrefab` asset from your Project window into the **`Prefab To Pool`** slot.
        *   Set **`Pool Size`** to a reasonable number (e.g., `20`).
        *   Leave **`Should Expand`** checked (recommended for most games, or uncheck it to see what happens when the pool is exhausted).

3.  **Create the Spawner:**
    *   Create another empty GameObject in your scene and rename it `BulletSpawner`.
    *   Attach the `BulletSpawner.cs` script to this GameObject.
    *   In the Inspector for `BulletSpawner`:
        *   Drag the `BulletPoolManager` GameObject from your Hierarchy into the **`Bullet Pool`** slot.
        *   (Optional) Create a child empty GameObject under `BulletSpawner`, name it `SpawnPoint`, position it slightly in front of the spawner, and then drag this `SpawnPoint` child into the **`Spawn Point`** slot on the `Bullet Spawner` component. This allows you to easily control where bullets emerge.
        *   Adjust **`Spawn Interval`** as desired (e.g., `0.1` for rapid firing).

4.  **Run the Scene:**
    *   Press Play in Unity. You should see bullets being spawned from your `BulletSpawner` and flying forward.
    *   Observe the Hierarchy: Initially, `BulletPoolManager` will contain `_poolSize` (e.g., 20) inactive `BulletPrefab (Pooled)` objects. As bullets are spawned, they become active and move. After their `_lifeTime` or a collision, they deactivate and return under `BulletPoolManager`, ready for reuse. If `_shouldExpand` is true and you exceed the `_poolSize`, new objects will be instantiated.

This setup provides a complete and practical example of the Object Pooling pattern, ready to be used and adapted in your Unity projects.