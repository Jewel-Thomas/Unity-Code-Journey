// Unity Design Pattern Example: PoolingManager
// This script demonstrates the PoolingManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Pooling Manager** design pattern. It's designed to be educational, demonstrating the core concepts while adhering to Unity best practices.

**How to Use This Script:**

1.  **Create a C# Script:** In your Unity project, create a new C# script named `PoolingManager.cs`.
2.  **Copy & Paste:** Copy the entire code below and paste it into your `PoolingManager.cs` file, replacing its contents.
3.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., name it `_Managers`).
4.  **Add `PoolingManager` Component:** Drag and drop the `PoolingManager.cs` script onto the `_Managers` GameObject. This will make it a Singleton accessible globally.
5.  **Prepare Prefabs:** Create some prefabs (e.g., a "Bullet" prefab, an "Explosion" prefab). These are the objects you will pool.
    *   **Important:** Any object you want to be managed by the pool **must not be destroyed using `GameObject.Destroy()` directly**. Instead, its `PooledObject` component should call `ReturnToPool()`.
6.  **Implement Usage (See Example Section in Comments):**
    *   In a game manager or spawning script, call `PoolingManager.Instance.PreloadPool()` in `Awake()` or `Start()` to initialize your pools with a desired size.
    *   When you need an object, call `PoolingManager.Instance.GetPooledObject()`.
    *   When you're done with an object, either have the object itself call `this.GetComponent<PooledObject>().ReturnToPool()` (recommended for self-managing objects) or explicitly call `PoolingManager.Instance.ReturnPooledObject()` from another script.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary and Queue

/// <summary>
/// This component is automatically added to any GameObject instantiated by the PoolingManager.
/// It tracks the original prefab from which this instance was created and provides a convenient
/// method for the instance to return itself to the pool.
/// </summary>
public class PooledObject : MonoBehaviour
{
    // The original prefab from which this object was instantiated.
    // This is crucial for the PoolingManager to know which pool to return the object to.
    public GameObject OriginalPrefab { get; set; }

    /// <summary>
    /// Call this method on a pooled object when you want to "destroy" it.
    /// Instead of destroying, it will be deactivated and returned to its respective pool.
    /// </summary>
    public void ReturnToPool()
    {
        // Ensure the PoolingManager instance exists before attempting to return.
        if (PoolingManager.Instance != null)
        {
            PoolingManager.Instance.ReturnPooledObject(gameObject);
        }
        else
        {
            // If the manager is gone (e.g., scene unloaded), just destroy the object.
            Debug.LogWarning($"PoolingManager instance not found when trying to return {gameObject.name}. Destroying object directly.");
            Destroy(gameObject);
        }
    }

    // --- Optional: Implement OnDisable to reset object state ---
    // This is a good place to reset any dynamic properties of your pooled object
    // whenever it's returned to the pool (deactivated).
    private void OnDisable()
    {
        // Example: If this was a bullet, stop its movement, reset its damage, etc.
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb != null)
        // {
        //     rb.velocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        // }

        // ParticleSystem ps = GetComponent<ParticleSystem>();
        // if (ps != null)
        // {
        //     ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        // }

        // Renderer rend = GetComponent<Renderer>();
        // if (rend != null)
        // {
        //     rend.enabled = true; // Ensure it's visible if it was hidden for some reason
        // }
    }
}


/// <summary>
/// The PoolingManager is a central hub for managing object pooling in your Unity project.
/// It implements the Singleton pattern for easy global access.
/// Object pooling drastically reduces instantiation/destruction overhead and garbage collection
/// spikes by reusing GameObjects instead of constantly creating and destroying them.
/// </summary>
public class PoolingManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures there's only one instance of the PoolingManager throughout the application.
    public static PoolingManager Instance { get; private set; }

    // --- Pool Storage ---
    // A dictionary where the key is the original prefab GameObject, and the value is a Queue
    // containing inactive instances of that prefab.
    private Dictionary<GameObject, Queue<GameObject>> pools;

    // --- Hierarchy Organization ---
    // All pooled objects will be parented under this transform to keep the Hierarchy clean.
    private Transform pooledObjectsParent;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton and sets up the pooling system.
    /// </summary>
    private void Awake()
    {
        // Singleton implementation:
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Debug.LogWarning("Duplicate PoolingManager detected. Destroying this instance.");
            Destroy(gameObject);
        }
        else
        {
            // Set this as the active instance.
            Instance = this;
            // Make sure the manager persists across scene loads.
            // (Optional, depends on your game's architecture. Remove if you want a new manager per scene.)
            DontDestroyOnLoad(gameObject);

            // Initialize the dictionary to store our pools.
            pools = new Dictionary<GameObject, Queue<GameObject>>();

            // Create a parent GameObject for all pooled objects to keep the Hierarchy tidy.
            pooledObjectsParent = new GameObject("[Pooled Objects]").transform;
            pooledObjectsParent.SetParent(this.transform); // Make it a child of the PoolingManager
        }
    }

    /// <summary>
    /// Preloads a specified number of objects into a pool for a given prefab.
    /// It's good practice to call this method in your game's initialization phase
    /// for all frequently used prefabs.
    /// </summary>
    /// <param name="prefabToPool">The GameObject prefab to create a pool for.</param>
    /// <param name="initialSize">The initial number of instances to create for the pool.</param>
    public void PreloadPool(GameObject prefabToPool, int initialSize)
    {
        if (prefabToPool == null)
        {
            Debug.LogError("PoolingManager: Attempted to preload a null prefab.");
            return;
        }

        if (initialSize <= 0)
        {
            Debug.LogWarning($"PoolingManager: Initial pool size for '{prefabToPool.name}' must be greater than 0. Setting to 1.");
            initialSize = 1;
        }

        // Check if a pool for this prefab already exists.
        if (pools.ContainsKey(prefabToPool))
        {
            Debug.LogWarning($"PoolingManager: Pool for '{prefabToPool.name}' already exists. Skipping preloading.");
            return;
        }

        // Create a new queue for this prefab.
        Queue<GameObject> newPool = new Queue<GameObject>();

        // Instantiate objects and add them to the pool.
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefabToPool);
            obj.SetActive(false); // Objects start inactive.
            obj.transform.SetParent(pooledObjectsParent); // Parent to our pooled objects container.

            // Add the PooledObject component to track its origin and provide a return method.
            PooledObject pooledComponent = obj.AddComponent<PooledObject>();
            pooledComponent.OriginalPrefab = prefabToPool;

            newPool.Enqueue(obj);
        }

        // Add the new pool to our dictionary.
        pools.Add(prefabToPool, newPool);
        Debug.Log($"PoolingManager: Preloaded pool for '{prefabToPool.name}' with {initialSize} objects.");
    }

    /// <summary>
    /// Retrieves an object from the specified pool. If the pool is empty, a new object
    /// will be instantiated (expanding the pool).
    /// </summary>
    /// <param name="prefabToGet">The original prefab associated with the desired pool.</param>
    /// <param name="position">Optional: The world position to set the object to.</param>
    /// <param name="rotation">Optional: The world rotation to set the object to.</param>
    /// <param name="parent">Optional: The parent Transform to set the object to.</param>
    /// <returns>An active GameObject from the pool, or null if the prefab is invalid.</returns>
    public GameObject GetPooledObject(GameObject prefabToGet, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        if (prefabToGet == null)
        {
            Debug.LogError("PoolingManager: Attempted to get a null prefab from the pool.");
            return null;
        }

        Queue<GameObject> pool;

        // Try to get the pool for the given prefab.
        if (!pools.TryGetValue(prefabToGet, out pool))
        {
            // If the pool doesn't exist, create it on the fly with a small default size.
            // This is generally not recommended for performance-critical pools; preloading is better.
            Debug.LogWarning($"PoolingManager: Pool for '{prefabToGet.name}' not found. Creating pool with default size (5). Consider preloading.");
            PreloadPool(prefabToGet, 5); // Create a small pool
            pool = pools[prefabToGet]; // Retrieve the newly created pool
        }

        GameObject obj;

        // If the pool is empty, instantiate a new object (dynamically expands the pool).
        if (pool.Count == 0)
        {
            obj = Instantiate(prefabToGet);
            // Add the PooledObject component to track its origin and provide a return method.
            PooledObject pooledComponent = obj.AddComponent<PooledObject>();
            pooledComponent.OriginalPrefab = prefabToGet;
            obj.transform.SetParent(pooledObjectsParent); // Parent to our pooled objects container.
            Debug.LogWarning($"PoolingManager: Pool for '{prefabToGet.name}' was empty. Instantiated a new object. Consider increasing initial pool size for this prefab.");
        }
        else
        {
            // Get an object from the queue.
            obj = pool.Dequeue();
        }

        // Set its position, rotation, and parent (if provided).
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.SetParent(parent);

        obj.SetActive(true); // Activate the object.
        return obj;
    }

    /// <summary>
    /// Returns an object back to its appropriate pool. The object will be deactivated.
    /// This method is typically called by the PooledObject component itself.
    /// </summary>
    /// <param name="objectToReturn">The GameObject instance to return to the pool.</param>
    public void ReturnPooledObject(GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            Debug.LogError("PoolingManager: Attempted to return a null object to the pool.");
            return;
        }

        // Get the PooledObject component to identify its original prefab.
        PooledObject pooledComponent = objectToReturn.GetComponent<PooledObject>();

        // Validate that the object was indeed created by our pooling system.
        if (pooledComponent == null || pooledComponent.OriginalPrefab == null)
        {
            Debug.LogWarning($"PoolingManager: Attempted to return an object '{objectToReturn.name}' " +
                             $"that wasn't created by the PoolingManager or is missing its PooledObject component. " +
                             $"Destroying it instead.");
            Destroy(objectToReturn); // Fallback: destroy if not managed by pool.
            return;
        }

        // Deactivate the object.
        objectToReturn.SetActive(false);
        // Reset its parent to the pooled objects container for organization.
        objectToReturn.transform.SetParent(pooledObjectsParent);

        // Try to find the correct pool based on the original prefab.
        if (pools.TryGetValue(pooledComponent.OriginalPrefab, out var pool))
        {
            pool.Enqueue(objectToReturn); // Add the object back to its pool.
        }
        else
        {
            // This case indicates a potential issue if the prefab was in `PooledObject`
            // but its pool was somehow removed from the dictionary.
            Debug.LogError($"PoolingManager: Pool for prefab '{pooledComponent.OriginalPrefab.name}' " +
                           $"not found when trying to return '{objectToReturn.name}'. Destroying object.");
            Destroy(objectToReturn);
        }
    }
}

/*
/// --- EXAMPLE USAGE ---
/// To demonstrate how to use the PoolingManager, imagine you have a 'Bullet' prefab
/// and an 'Explosion' prefab.

/// 1. Create a `Bullet` prefab in your Unity project. Add a Rigidbody and a simple script like `Bullet.cs`.
/// 2. Create an `Explosion` prefab (e.g., a Particle System GameObject). Add a simple script like `Explosion.cs`.

/// --- Bullet.cs (Example Script for a Pooled Object) ---
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f; // How long before the bullet returns to the pool

    private Rigidbody rb;
    private float currentLifetime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>(); // Ensure bullet has a Rigidbody
        }
    }

    // Called when the object is retrieved from the pool and activated
    void OnEnable()
    {
        currentLifetime = lifetime;
        rb.velocity = transform.forward * speed; // Apply initial velocity
        Debug.Log($"{name} spawned at {transform.position}");
    }

    // Called when the object is returned to the pool and deactivated
    void OnDisable()
    {
        rb.velocity = Vector3.zero; // Stop movement
        rb.angularVelocity = Vector3.zero; // Stop rotation
        // Additional cleanup like resetting materials, damage values, etc.
        Debug.Log($"{name} returned to pool.");
    }

    void Update()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            GetComponent<PooledObject>().ReturnToPool(); // Self-destruct by returning to pool
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Example: If bullet hits something, return to pool and maybe spawn an explosion
        Debug.Log($"{name} hit {other.name}");
        GetComponent<PooledObject>().ReturnToPool();

        // Optional: Spawn an explosion (also pooled!)
        // This requires an 'Explosion' prefab to be preloaded.
        // PoolingManager.Instance.GetPooledObject(GameManager.Instance.explosionPrefab, transform.position, Quaternion.identity);
    }
}


/// --- Explosion.cs (Example Script for another Pooled Object) ---
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))] // Ensure it has a ParticleSystem component
public class Explosion : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Called when object is retrieved from pool and activated
    void OnEnable()
    {
        ps.Play(); // Start particle effect
        // Set a timer to return to pool after the particles finish
        Invoke("ReturnExplosionToPool", ps.main.duration);
        Debug.Log($"{name} explosion started at {transform.position}");
    }

    // Called when object is returned to pool and deactivated
    void OnDisable()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop and clear particles
        CancelInvoke("ReturnExplosionToPool"); // Cancel any pending invokes
        Debug.Log($"{name} explosion returned to pool.");
    }

    void ReturnExplosionToPool()
    {
        GetComponent<PooledObject>().ReturnToPool();
    }
}


/// --- GameManager.cs (Example Script to Manage Pools and Spawn Objects) ---
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject bulletPrefab;     // Assign your Bullet prefab in the Inspector
    public GameObject explosionPrefab;  // Assign your Explosion prefab in the Inspector

    public Transform spawnPoint;        // Assign a transform where bullets should spawn

    void Awake()
    {
        // Preload pools for frequently used objects when the game starts.
        // This is crucial for performance!
        if (PoolingManager.Instance != null)
        {
            PoolingManager.Instance.PreloadPool(bulletPrefab, 20);    // Preload 20 bullets
            PoolingManager.Instance.PreloadPool(explosionPrefab, 5); // Preload 5 explosions
        }
    }

    void Update()
    {
        // Example: Press space to shoot a bullet
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootBullet();
        }
    }

    void ShootBullet()
    {
        if (bulletPrefab == null || spawnPoint == null)
        {
            Debug.LogError("Bullet Prefab or Spawn Point not assigned in GameManager!");
            return;
        }

        if (PoolingManager.Instance != null)
        {
            // Get a bullet from the pool, position it, and set its rotation
            GameObject bullet = PoolingManager.Instance.GetPooledObject(
                bulletPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // The Bullet script will handle its own movement and returning to the pool.
            // You can also pass the spawnPoint as a parent if you wish.
            // bullet.transform.SetParent(spawnPoint); // Example of setting parent
        }
    }
}
*/
```