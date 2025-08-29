// Unity Design Pattern Example: PrefabSystem
// This script demonstrates the PrefabSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive example demonstrates the 'PrefabSystem' design pattern in Unity. This pattern is commonly used to manage the instantiation and recycling of frequently created and destroyed GameObjects (like bullets, enemies, particle effects, or UI elements) to improve performance and reduce memory allocations (GC pressure) by using object pooling.

**Key Components of the PrefabSystem Pattern:**

1.  **PrefabSystem (Manager):** A central singleton responsible for:
    *   Registering prefabs with unique IDs.
    *   Maintaining pools of inactive GameObject instances for each registered prefab.
    *   Providing a method (`GetPrefab`) to retrieve an active instance from a pool (or create a new one if the pool is empty).
    *   Providing a method (`ReturnPrefab`) to return an instance to its pool, making it inactive and available for reuse.
    *   Ensuring the pooled objects are parented cleanly in the hierarchy.
2.  **PooledObject (Component):** A small script attached to every pooled GameObject instance. It identifies which prefab pool the object belongs to and provides a convenient way for the object to return itself to the `PrefabSystem`.
3.  **IPooledObjectLifecycle (Interface):** An optional interface that pooled objects can implement to receive callbacks (`OnGetFromPool`, `OnReturnToPool`) when they are retrieved from or returned to the pool. This is useful for resetting their state, re-initializing properties, or performing cleanup.
4.  **Client Code (Spawner):** Any script that needs to create or destroy objects using the `PrefabSystem`'s interface, without knowing the underlying instantiation or pooling logic.

---

### **1. `PrefabSystem.cs`**

This is the core manager. It's a singleton, handles registration, pooling, and retrieval.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for some LINQ operations like ToList() for safe iteration

namespace PrefabSystemExample
{
    /// <summary>
    /// A serializable struct used by the PrefabSystem to define prefabs
    /// that should be registered and have their pools pre-populated in the Inspector.
    /// </summary>
    [System.Serializable]
    public struct PrefabPoolEntry
    {
        [Tooltip("A unique identifier for this prefab. Used to request/return instances.")]
        public string id;

        [Tooltip("The GameObject prefab asset to be managed.")]
        public GameObject prefab;

        [Tooltip("The number of instances to create initially for this pool on Awake.")]
        public int initialPoolSize;
    }

    /// <summary>
    /// The central PrefabSystem manager. It's a singleton responsible for:
    /// - Registering prefabs and their associated pools.
    /// - Providing instances from pools (or creating new ones if needed).
    /// - Receiving instances back into pools for reuse.
    /// - Managing the lifecycle callbacks for pooled objects.
    /// </summary>
    public class PrefabSystem : MonoBehaviour
    {
        // Singleton instance for global access
        public static PrefabSystem Instance { get; private set; }

        [SerializeField]
        [Tooltip("List of prefabs to register and pre-populate pools with on Awake.")]
        private List<PrefabPoolEntry> initialPrefabPools = new List<PrefabPoolEntry>();

        // Dictionary to store available (inactive) pooled objects, keyed by prefab ID.
        // Each value is a Queue, representing a pool for a specific prefab type.
        private Dictionary<string, Queue<GameObject>> availablePools = new Dictionary<string, Queue<GameObject>>();

        // Dictionary to map an active instance back to its prefab ID.
        // This allows the system to know which pool to return an object to without
        // relying solely on the PooledObject component (though it's usually consistent).
        private Dictionary<GameObject, string> activeInstanceToPrefabIdMap = new Dictionary<GameObject, string>();

        // A parent transform for all inactive pooled objects to keep the Hierarchy clean.
        private Transform poolParent;

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("PrefabSystem: Multiple instances found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Make the manager persistent across scene loads if desired.
            // Remove if this system should only exist within a single scene.
            DontDestroyOnLoad(gameObject);

            // Create a parent GameObject for all pooled objects to maintain a clean Hierarchy.
            GameObject poolParentGO = new GameObject("___PrefabPool___");
            poolParent = poolParentGO.transform;
            poolParent.SetParent(this.transform); // Make it a child of the PrefabSystem for better organization.
            DontDestroyOnLoad(poolParentGO); // Ensure the pool parent also persists with the system.

            // Initialize pools based on the settings provided in the Inspector.
            foreach (var entry in initialPrefabPools)
            {
                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogError("PrefabSystem: Initial pool entry has an empty ID. Skipping registration.");
                    continue;
                }
                if (entry.prefab == null)
                {
                    Debug.LogError($"PrefabSystem: Prefab for ID '{entry.id}' is null. Skipping registration.");
                    continue;
                }
                RegisterPrefab(entry.id, entry.prefab, entry.initialPoolSize);
            }

            Debug.Log("PrefabSystem: Initialized and ready.");
        }

        private void OnDestroy()
        {
            // Clean up the singleton reference.
            if (Instance == this)
            {
                Instance = null;
            }

            // Destroy the pool parent and all its children (pooled objects)
            if (poolParent != null)
            {
                Destroy(poolParent.gameObject);
            }

            Debug.Log("PrefabSystem: Shut down.");
        }

        /// <summary>
        /// Registers a prefab with the system and optionally pre-populates its pool.
        /// Call this method if you need to register prefabs dynamically at runtime.
        /// </summary>
        /// <param name="id">A unique string identifier for this prefab.</param>
        /// <param name="prefab">The GameObject prefab asset to be managed.</param>
        /// <param name="initialPoolSize">The number of instances to create initially for the pool.</param>
        public void RegisterPrefab(string id, GameObject prefab, int initialPoolSize = 0)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("PrefabSystem: Cannot register prefab with an empty or null ID.");
                return;
            }
            if (prefab == null)
            {
                Debug.LogError($"PrefabSystem: Cannot register null prefab for ID '{id}'.");
                return;
            }
            if (availablePools.ContainsKey(id))
            {
                Debug.LogWarning($"PrefabSystem: Prefab with ID '{id}' is already registered. " +
                                 "Consider calling ClearPool() before re-registering or ensuring unique IDs.");
                // For simplicity, we'll clear the old pool and re-register.
                ClearPool(id);
            }

            // Create a new queue for this prefab's pool.
            availablePools[id] = new Queue<GameObject>();

            // Pre-populate the pool with inactive instances.
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject obj = Instantiate(prefab, poolParent); // Instantiate as child of the pool parent.
                obj.name = $"{prefab.name} (Pooled)"; // Rename for clarity in Hierarchy.
                obj.SetActive(false); // Make it inactive.
                availablePools[id].Enqueue(obj); // Add to the queue.

                // Ensure the PooledObject component exists and is configured.
                SetupPooledObjectComponent(obj, id);
            }

            // Also ensure the prefab is added to initialPrefabPools if registered dynamically
            // so that GetPrefab can find the original prefab if a new instance needs to be created.
            if (!initialPrefabPools.Any(entry => entry.id == id))
            {
                initialPrefabPools.Add(new PrefabPoolEntry { id = id, prefab = prefab, initialPoolSize = initialPoolSize });
            }

            Debug.Log($"PrefabSystem: Registered prefab '{id}' with initial pool size {initialPoolSize}.");
        }

        /// <summary>
        /// Retrieves an instance of the specified prefab from the pool.
        /// If no instances are available, a new one is created.
        /// </summary>
        /// <param name="id">The unique identifier of the prefab to retrieve.</param>
        /// <param name="position">The world position for the new instance.</param>
        /// <param name="rotation">The world rotation for the new instance.</param>
        /// <param name="parent">Optional parent transform for the new instance. If null, it will be at the root of the scene (or its default poolParent).</param>
        /// <returns>An active GameObject instance, or null if the prefab ID is not registered.</returns>
        public GameObject GetPrefab(string id, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!availablePools.ContainsKey(id))
            {
                Debug.LogError($"PrefabSystem: Prefab with ID '{id}' is not registered. Please register it first.");
                return null;
            }

            GameObject obj;
            if (availablePools[id].Count > 0)
            {
                // Retrieve an existing object from the pool.
                obj = availablePools[id].Dequeue();
                obj.transform.SetParent(parent); // Set parent before position/rotation to handle local vs. world space.
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true); // Activate the object.
            }
            else
            {
                // Pool is empty, create a new instance directly from the original prefab asset.
                // Find the original prefab from our registered list.
                GameObject originalPrefab = initialPrefabPools.Find(p => p.id == id).prefab;
                if (originalPrefab == null)
                {
                    Debug.LogError($"PrefabSystem: Could not find original prefab for ID '{id}' to instantiate a new one. " +
                                   "This indicates a registration issue or a missing prefab asset.");
                    return null;
                }

                obj = Instantiate(originalPrefab, position, rotation, parent);
                obj.name = $"{originalPrefab.name} (Cloned)"; // Rename for clarity.

                // Ensure the newly created object also has the PooledObject component and is configured.
                SetupPooledObjectComponent(obj, id);
            }

            // Track the instance as active for future return operations.
            activeInstanceToPrefabIdMap[obj] = id;

            // Call the OnGetFromPool lifecycle method if the object implements IPooledObjectLifecycle.
            obj.GetComponent<IPooledObjectLifecycle>()?.OnGetFromPool();

            return obj;
        }

        /// <summary>
        /// Returns an active GameObject instance back to its corresponding pool.
        /// The object will be deactivated and re-parented to the internal pool parent.
        /// </summary>
        /// <param name="instance">The GameObject instance to return.</param>
        public void ReturnPrefab(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("PrefabSystem: Attempted to return a null instance to the pool. Ignoring.");
                return;
            }

            // Try to find the prefab ID associated with this instance.
            if (!activeInstanceToPrefabIdMap.TryGetValue(instance, out string id))
            {
                Debug.LogWarning($"PrefabSystem: Attempted to return object '{instance.name}' which was not obtained from this system, " +
                                 "or has already been returned. Destroying instance to prevent leaks.");
                // If it's not tracked by us, it might be a regular GameObject or already returned. Destroy it.
                Destroy(instance);
                return;
            }

            // Call the OnReturnToPool lifecycle method if the object implements IPooledObjectLifecycle.
            instance.GetComponent<IPooledObjectLifecycle>()?.OnReturnToPool();

            // Remove from the active tracking map.
            activeInstanceToPrefabIdMap.Remove(instance);

            // Deactivate and re-parent to the pool parent for cleanliness.
            instance.SetActive(false);
            instance.transform.SetParent(poolParent);
            availablePools[id].Enqueue(instance); // Add back to its respective pool.
        }

        /// <summary>
        /// Clears all instances from a specific prefab pool.
        /// Note: This will destroy all active *and* inactive instances of that prefab type.
        /// Use with caution, especially if objects are currently active in the scene.
        /// </summary>
        /// <param name="id">The ID of the prefab pool to clear.</param>
        public void ClearPool(string id)
        {
            if (!availablePools.ContainsKey(id))
            {
                Debug.LogWarning($"PrefabSystem: Cannot clear pool for ID '{id}' as it is not registered.");
                return;
            }

            // Destroy all available (inactive) objects in the pool.
            while (availablePools[id].Count > 0)
            {
                GameObject obj = availablePools[id].Dequeue();
                Destroy(obj);
            }

            // Destroy any active objects that belong to this pool.
            // We create a list to avoid modifying the dictionary while iterating.
            var activeInstancesToDestroy = activeInstanceToPrefabIdMap
                .Where(pair => pair.Value == id)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var instance in activeInstancesToDestroy)
            {
                activeInstanceToPrefabIdMap.Remove(instance);
                Destroy(instance);
            }

            availablePools.Remove(id); // Remove the pool itself.
            initialPrefabPools.RemoveAll(entry => entry.id == id); // Remove from initial list as well.

            Debug.Log($"PrefabSystem: Cleared and unregistered pool for '{id}'.");
        }

        /// <summary>
        /// Clears all prefab pools and destroys all managed instances (both active and inactive).
        /// Use this when completely resetting the system, e.g., on game over or scene transition.
        /// </summary>
        public void ClearAllPools()
        {
            // Iterate over a copy of keys to avoid modification during iteration.
            foreach (var id in availablePools.Keys.ToList())
            {
                ClearPool(id); // Calls ClearPool for each individual pool.
            }
            availablePools.Clear();
            activeInstanceToPrefabIdMap.Clear();
            initialPrefabPools.Clear();
            Debug.Log("PrefabSystem: Cleared all pools and unregistered all prefabs.");
        }

        /// <summary>
        /// Helper method to ensure the PooledObject component is present and configured.
        /// This is crucial for objects to correctly identify their pool and return themselves.
        /// </summary>
        /// <param name="obj">The GameObject to configure.</param>
        /// <param name="id">The prefab ID this object belongs to.</param>
        private void SetupPooledObjectComponent(GameObject obj, string id)
        {
            PooledObject pooledObjComponent = obj.GetComponent<PooledObject>();
            if (pooledObjComponent == null)
            {
                pooledObjComponent = obj.AddComponent<PooledObject>();
            }
            pooledObjComponent.PrefabID = id;
            pooledObjComponent.PrefabSystemInstance = this; // Link back to this system.
        }
    }
}
```

---

### **2. `IPooledObjectLifecycle.cs`**

This interface allows pooled objects to define custom logic for when they are reused or returned.

```csharp
namespace PrefabSystemExample
{
    /// <summary>
    /// Interface for objects that want to receive notifications when they are
    /// retrieved from or returned to the PrefabSystem's object pool.
    /// This is useful for resetting state, re-initializing, or performing cleanup.
    /// </summary>
    public interface IPooledObjectLifecycle
    {
        /// <summary>
        /// Called by the PrefabSystem when this object is retrieved from the pool
        /// and made active. Use this to reset its state for new use.
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// Called by the PrefabSystem when this object is returned to the pool
        /// and made inactive. Use this to perform cleanup or prepare for future reuse.
        /// </summary>
        void OnReturnToPool();
    }
}
```

---

### **3. `PooledObject.cs`**

This component identifies a GameObject as a pooled object and provides a method to return itself to the system.

```csharp
using UnityEngine;

namespace PrefabSystemExample
{
    /// <summary>
    /// Component attached to objects managed by the PrefabSystem.
    /// It holds the PrefabID and a reference to the PrefabSystem for returning itself to the pool.
    /// This component should be on any prefab that is intended to be pooled.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        // The ID of the prefab this instance originated from.
        // This is set by the PrefabSystem upon instantiation or pooling.
        [HideInInspector] // Hidden because PrefabSystem manages it
        public string PrefabID;

        // A reference to the PrefabSystem instance that owns this pooled object.
        // This is set by the PrefabSystem to allow self-return.
        [HideInInspector]
        public PrefabSystem PrefabSystemInstance;

        /// <summary>
        /// Returns this GameObject instance to the PrefabSystem's pool.
        /// This method is typically called by a component on the object when it's
        /// no longer needed (e.g., after a duration, collision, or animation ends).
        /// </summary>
        public void ReturnToPool()
        {
            if (PrefabSystemInstance != null && !string.IsNullOrEmpty(PrefabID))
            {
                PrefabSystemInstance.ReturnPrefab(this.gameObject);
            }
            else
            {
                // Fallback: If it can't be returned to the pool (e.g., PrefabSystem was destroyed,
                // or not properly set up), destroy it to prevent memory leaks.
                Debug.LogWarning($"PooledObject '{gameObject.name}' could not be returned to pool " +
                                 $"(ID: {PrefabID ?? "N/A"}, System: {PrefabSystemInstance != null}). Destroying instead.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Optional: Automatically return to the pool when the object is disabled.
        /// This is a common pattern for things like projectiles or effects that simply
        /// disappear after their job is done. However, be cautious if your objects
        /// might be disabled for other temporary reasons.
        /// The `PrefabSystem.ReturnPrefab` method includes checks to prevent double-returns.
        /// </summary>
        private void OnDisable()
        {
            // Ensure the object is genuinely meant to be returned, not just scene cleanup or temporary disable.
            // We check if it's not being destroyed as part of a scene unload or explicit Destroy call.
            if (!gameObject.scene.isLoaded) return; // Object is being destroyed or scene unloaded.

            // Only attempt to return if the PrefabSystem is still active and this object is tied to a pool.
            if (PrefabSystemInstance != null && !string.IsNullOrEmpty(PrefabID))
            {
                ReturnToPool();
            }
        }
    }
}
```

---

### **4. `ExampleSpawner.cs`**

This script demonstrates how client code uses the `PrefabSystem` to spawn and manage objects. It also includes an example `BulletBehavior` that implements `IPooledObjectLifecycle`.

```csharp
using UnityEngine;
using System.Collections;

namespace PrefabSystemExample
{
    /// <summary>
    /// Example client class demonstrating how to use the PrefabSystem to spawn and return objects.
    /// Attach this to an empty GameObject in your scene.
    /// </summary>
    public class ExampleSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The ID of the prefab to spawn (must be registered in PrefabSystem).")]
        private string prefabIdToSpawn = "Bullet"; // Default example ID

        [SerializeField]
        [Tooltip("How often to spawn an object (in seconds).")]
        private float spawnInterval = 1f;

        [SerializeField]
        [Tooltip("How long each spawned object should live before being returned to the pool (in seconds).")]
        private float objectLifetime = 3f;

        [SerializeField]
        [Tooltip("The speed at which spawned objects move.")]
        private float moveSpeed = 5f;

        [SerializeField]
        [Tooltip("The direction objects will move after spawning.")]
        private Vector3 spawnDirection = Vector3.forward;

        private void Start()
        {
            // Start spawning objects after a short delay to ensure PrefabSystem has fully initialized.
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            // Wait a bit to ensure PrefabSystem.Awake has definitely run.
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                SpawnObject();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnObject()
        {
            if (PrefabSystem.Instance == null)
            {
                Debug.LogError("ExampleSpawner: PrefabSystem instance is not available. Cannot spawn object.");
                return;
            }

            // 1. Request an instance from the PrefabSystem using its unique ID.
            GameObject spawnedObject = PrefabSystem.Instance.GetPrefab(
                prefabIdToSpawn,
                transform.position,
                Quaternion.identity
            );

            if (spawnedObject != null)
            {
                Debug.Log($"ExampleSpawner: Spawned '{spawnedObject.name}' from pool ID '{prefabIdToSpawn}'.");

                // Get the BulletBehavior component (which implements IPooledObjectLifecycle).
                BulletBehavior bulletBehavior = spawnedObject.GetComponent<BulletBehavior>();
                if (bulletBehavior != null)
                {
                    // For this example, the BulletBehavior itself will handle movement and auto-return.
                    bulletBehavior.InitializeBullet(spawnDirection.normalized * moveSpeed, objectLifetime);
                }
                else
                {
                    Debug.LogWarning($"ExampleSpawner: No BulletBehavior found on '{spawnedObject.name}'. " +
                                     "Object will not move or automatically return to pool based on lifetime.");
                }
            }
        }

        /// <summary>
        /// An example component for a pooled 'bullet' object.
        /// It demonstrates how to use the IPooledObjectLifecycle interface and interact with the pool.
        /// Attach this script *to your prefab* alongside the 'PooledObject' component.
        /// </summary>
        public class BulletBehavior : MonoBehaviour, IPooledObjectLifecycle
        {
            private Renderer _renderer;
            private Rigidbody _rigidbody;
            private Vector3 _initialScale; // To demonstrate resetting scale

            private void Awake()
            {
                _renderer = GetComponent<Renderer>();
                _rigidbody = GetComponent<Rigidbody>();
                _initialScale = transform.localScale;
            }

            /// <summary>
            /// Custom initialization for the bullet, called by the spawner after it's retrieved.
            /// </summary>
            /// <param name="velocity">The initial velocity for the bullet.</param>
            /// <param name="lifetime">How long the bullet should stay active before returning to the pool.</param>
            public void InitializeBullet(Vector3 velocity, float lifetime)
            {
                if (_rigidbody != null)
                {
                    _rigidbody.velocity = velocity;
                }
                else
                {
                    // Fallback for non-rigidbody objects: simply move by transform
                    // In a real scenario, you'd pick one movement method (Rigidbody or Transform)
                    StartCoroutine(MoveBulletWithoutRigidbody(velocity.normalized, lifetime));
                }

                StartCoroutine(LifetimeCountdown(lifetime));
            }

            private IEnumerator MoveBulletWithoutRigidbody(Vector3 direction, float lifetime)
            {
                float startTime = Time.time;
                while (Time.time < startTime + lifetime && gameObject.activeSelf)
                {
                    transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
                    yield return null;
                }
            }


            private IEnumerator LifetimeCountdown(float lifetime)
            {
                yield return new WaitForSeconds(lifetime);

                // If the object is still active after its lifetime, return it to the pool.
                // This handles cases where it didn't hit anything and needs to despawn.
                if (gameObject.activeSelf)
                {
                    Debug.Log($"{gameObject.name}: Lifetime expired. Returning to pool.");
                    GetComponent<PooledObject>()?.ReturnToPool();
                }
            }


            // --- IPooledObjectLifecycle Implementation ---

            public void OnGetFromPool()
            {
                // This method is called by PrefabSystem *after* the object is activated and positioned.
                Debug.Log($"{gameObject.name} (BulletBehavior): OnGetFromPool - Resetting state.");

                // Reset any physics state.
                if (_rigidbody != null)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    _rigidbody.isKinematic = false; // Ensure it's not kinematic if it was made so for pooling
                }

                // Reset visual or other properties.
                if (_renderer != null)
                {
                    _renderer.material.color = Color.red; // Example: Set active color.
                }
                transform.localScale = _initialScale; // Reset scale.

                // Re-enable any specific components if they were disabled for pooling (e.g., colliders if needed)
                // For a simple bullet, usually not needed.
            }

            public void OnReturnToPool()
            {
                // This method is called by PrefabSystem *before* the object is deactivated.
                Debug.Log($"{gameObject.name} (BulletBehavior): OnReturnToPool - Preparing for reuse.");

                // Clean up or reset for next use.
                if (_rigidbody != null)
                {
                    // Clear velocity to prevent residual motion when reactivated.
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    _rigidbody.isKinematic = true; // Make kinematic to prevent physics interactions while in pool.
                }

                if (_renderer != null)
                {
                    _renderer.material.color = Color.gray; // Example: Set inactive color.
                }

                // Stop any running coroutines specific to this instance's active lifetime.
                StopAllCoroutines();
            }

            // Example: When the bullet hits something, it should be returned to the pool.
            private void OnCollisionEnter(Collision collision)
            {
                Debug.Log($"{gameObject.name} collided with {collision.gameObject.name}. Returning to pool.");

                // Immediately return to pool on collision, instead of waiting for lifetime.
                // It's important to call ReturnToPool() from the PooledObject component.
                GetComponent<PooledObject>()?.ReturnToPool();
            }
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create C# Scripts:**
    *   Create a folder named `Scripts/PrefabSystemExample` in your Unity project.
    *   Create four new C# scripts in this folder: `PrefabSystem.cs`, `IPooledObjectLifecycle.cs`, `PooledObject.cs`, and `ExampleSpawner.cs`.
    *   Copy and paste the code for each respective file into these new scripts.

2.  **Create a Prefab (e.g., a Bullet):**
    *   In Unity, go to `GameObject > 3D Object > Cube`.
    *   Rename the new Cube GameObject to `BulletPrefab`.
    *   In the Inspector for `BulletPrefab`:
        *   Add a `Rigidbody` component (`Add Component > Physics > Rigidbody`).
        *   Add the `PooledObject` script (`Add Component > Scripts > Prefab System Example > Pooled Object`).
        *   Add the `BulletBehavior` script (`Add Component > Scripts > Prefab System Example > Example Spawner+Bullet Behavior`).
    *   Drag `BulletPrefab` from the Hierarchy into your Project window (e.g., into a new `Prefabs` folder) to create a reusable prefab asset.
    *   Delete `BulletPrefab` from the Hierarchy (it's now an asset).

3.  **Create the PrefabSystem Manager:**
    *   In your scene, create an empty GameObject (`GameObject > Create Empty`).
    *   Rename it to `PrefabSystemManager`.
    *   In the Inspector for `PrefabSystemManager`, add the `PrefabSystem` script (`Add Component > Scripts > Prefab System Example > Prefab System`).
    *   In the `PrefabSystem` component's Inspector:
        *   Expand the `Initial Prefab Pools` list.
        *   Click the `+` button to add a new entry.
        *   For the new entry:
            *   Set `ID`: `Bullet` (this is the string you'll use to request it).
            *   Drag your `BulletPrefab` asset from the Project window into the `Prefab` slot.
            *   Set `Initial Pool Size`: `10` (this will pre-instantiate 10 inactive bullets when the game starts).

4.  **Create the Example Spawner:**
    *   In your scene, create another empty GameObject.
    *   Rename it to `BulletSpawner`.
    *   Position `BulletSpawner` somewhere visible in your scene (e.g., `Y=1`).
    *   In the Inspector for `BulletSpawner`, add the `ExampleSpawner` script (`Add Component > Scripts > Prefab System Example > Example Spawner`).
    *   In the `ExampleSpawner` component's Inspector:
        *   Ensure `Prefab Id To Spawn` is set to `Bullet` (matching the ID you set in `PrefabSystemManager`).
        *   Adjust `Spawn Interval`, `Object Lifetime`, `Move Speed`, and `Spawn Direction` as desired.

5.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   You should see cubes (bullets) spawning from the `BulletSpawner`, moving, and then disappearing (being returned to the pool).
    *   Observe your Hierarchy:
        *   You'll see `PrefabSystemManager` and its child `___PrefabPool___`.
        *   Inactive `BulletPrefab (Pooled)` objects will be children of `___PrefabPool___`.
        *   Active bullets will be at the root of the hierarchy (or parented to something else if specified in `GetPrefab`) for their short lifetime.

This setup provides a robust and educational example of the PrefabSystem pattern in Unity, ready to be dropped into a project and adapted for various object pooling needs.