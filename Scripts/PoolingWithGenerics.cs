// Unity Design Pattern Example: PoolingWithGenerics
// This script demonstrates the PoolingWithGenerics pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **PoolingWithGenerics** design pattern in Unity. It provides a generic object pooling solution that can be used with any `Component` type, along with a practical example of pooling `Projectile` objects fired by a `ProjectileSpawner`.

**Key Concepts Explained:**

1.  **Object Pooling:** Instead of instantiating and destroying objects frequently (which causes performance spikes due to memory allocation/deallocation), object pooling reuses a pre-instantiated set of objects. When an object is "destroyed," it's simply returned to the pool and deactivated, ready to be "re-spawned" later.

2.  **Generics (`<T>`):** The `ObjectPool<T>` class is generic, meaning it can work with any type `T` that satisfies its constraints. Here, `T` must be a `UnityEngine.Component`, making it flexible for pooling various Unity game objects.

3.  **Custom Actions (`System.Action<T>`):** The pool allows you to define custom actions (callbacks) for various stages of an object's lifecycle:
    *   `onCreate`: When a new object is first instantiated for the pool.
    *   `onGet`: When an object is taken from the pool.
    *   `onReturn`: When an object is returned to the pool.
    *   `onDestroy`: When an object is permanently removed from the pool (e.g., when the pool is cleared).
    This makes the pool highly customizable without requiring `T` to implement a specific interface.

**How it Works (Real-World Use Case):**

*   **`ObjectPool<T>`:** This is the core generic class. It holds a `Stack` of `T` instances. When `Get()` is called, it tries to pop an object from the stack. If the stack is empty, it instantiates a new one. When `Return()` is called, the object is pushed back onto the stack. It handles activating/deactivating the GameObject and other setup/teardown via the custom actions.
*   **`Projectile`:** A simple `MonoBehaviour` that represents a bullet. It has a speed and a lifetime. When its lifetime expires, it tells its assigned pool to `Return` itself.
*   **`ProjectileSpawner`:** This `MonoBehaviour` uses an instance of `ObjectPool<Projectile>`. When the player presses a key, it `Get()`s a `Projectile` from the pool, positions it, and lets it fly.

---

### **Instructions to Use in Unity:**

1.  **Create a C# Script:** Create a new C# script in your Unity project (e.g., `PoolingWithGenerics.cs`).
2.  **Copy the Code:** Copy and paste the entire code block below into the `PoolingWithGenerics.cs` file.
3.  **Create a Projectile Prefab:**
    *   Create an empty GameObject (e.g., `Projectile_Prefab`).
    *   Add a `Cube` or `Sphere` as a child to give it a visual representation.
    *   Add the `Projectile` component to `Projectile_Prefab`.
    *   Drag `Projectile_Prefab` from the Hierarchy into your Project window to create a Prefab.
    *   Delete `Projectile_Prefab` from the Hierarchy.
4.  **Create a Spawner:**
    *   Create an empty GameObject in your scene (e.g., `Spawner`).
    *   Add the `ProjectileSpawner` component to it.
5.  **Configure in Inspector:**
    *   On the `Spawner` GameObject, drag your `Projectile_Prefab` into the `Projectile Prefab` slot.
    *   Optionally, set an `Initial Pool Size` (e.g., 10-20) and assign a `Pool Parent` Transform for organization.
6.  **Run the Scene:** Press Play. Press the **Spacebar** to spawn projectiles. Observe how they fly, disappear, and are reused without constant instantiation/destruction. You can also look at the Hierarchy: all pooled objects will be children of the `Pool Parent` (or a newly created one).

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for System.Action

/// <summary>
/// A generic object pooling system for Unity Components.
/// This class demonstrates the 'PoolingWithGenerics' design pattern.
/// </summary>
/// <typeparam name="T">The type of Component to be pooled. Must inherit from Component.</typeparam>
public class ObjectPool<T> where T : Component
{
    // --- Private Fields ---
    private readonly Stack<T> _pooledObjects = new Stack<T>(); // Using a Stack for LIFO (Last-In, First-Out) reuse
    private readonly GameObject _prefab;                      // The GameObject prefab to instantiate
    private readonly Transform _parentTransform;             // The parent Transform for pooled objects (for scene hierarchy cleanliness)

    // --- Action Callbacks for Customization ---
    // These actions are invoked at different stages of an object's lifecycle.
    // They allow the user of the pool to define specific behaviors without modifying the pool itself.
    private readonly Action<T> _onCreate;    // Called when a new object is first instantiated by the pool.
    private readonly Action<T> _onGet;       // Called when an object is retrieved from the pool (becomes active).
    private readonly Action<T> _onReturn;    // Called when an object is returned to the pool (becomes inactive).
    private readonly Action<T> _onDestroy;   // Called when an object is permanently destroyed by the pool.

    // --- Constructor ---
    /// <summary>
    /// Initializes a new instance of the ObjectPool.
    /// </summary>
    /// <param name="prefab">The GameObject prefab that contains the Component T.</param>
    /// <param name="parent">The Transform to parent all pooled objects under. If null, a new GameObject will be created as parent.</param>
    /// <param name="onCreateCallback">Action to perform when a new object is created (e.g., set pool reference).</param>
    /// <param name="onGetCallback">Action to perform when an object is retrieved from the pool (e.g., activate GameObject, reset state).</param>
    /// <param name="onReturnCallback">Action to perform when an object is returned to the pool (e.g., deactivate GameObject, clear state).</param>
    /// <param name="onDestroyCallback">Action to perform when an object is destroyed (e.g., remove listeners).</param>
    /// <param name="initialSize">The number of objects to pre-instantiate when the pool is created (pre-warming).</param>
    public ObjectPool(
        GameObject prefab,
        Transform parent,
        Action<T> onCreateCallback,
        Action<T> onGetCallback,
        Action<T> onReturnCallback,
        Action<T> onDestroyCallback,
        int initialSize = 0)
    {
        if (prefab == null)
        {
            Debug.LogError("ObjectPool: Prefab cannot be null.");
            return;
        }
        if (prefab.GetComponent<T>() == null)
        {
            Debug.LogError($"ObjectPool: Prefab '{prefab.name}' does not contain a component of type '{typeof(T).Name}'.");
            return;
        }

        _prefab = prefab;
        _onCreate = onCreateCallback;
        _onGet = onGetCallback;
        _onReturn = onReturnCallback;
        _onDestroy = onDestroyCallback;

        // Create a parent object for pool cleanliness if one isn't provided
        if (parent == null)
        {
            GameObject parentGO = new GameObject($"ObjectPool_{typeof(T).Name}s_Parent");
            _parentTransform = parentGO.transform;
            // Optionally, make it a child of the current scene if this pool is managed by a GameObject.
            // For a general utility class, keeping it at root is fine.
        }
        else
        {
            _parentTransform = parent;
        }

        // Pre-warm the pool by creating initialSize objects
        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreatePooledObject();
            _pooledObjects.Push(obj); // Add to stack
            _onReturn?.Invoke(obj);   // Immediately call onReturn to set initial state (e.g., SetActive(false))
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Retrieves an object from the pool. If the pool is empty, a new object is instantiated.
    /// </summary>
    /// <returns>An active instance of the pooled component T.</returns>
    public T Get()
    {
        T obj;
        if (_pooledObjects.Count > 0)
        {
            obj = _pooledObjects.Pop(); // Get from pool
        }
        else
        {
            obj = CreatePooledObject(); // Pool is empty, instantiate new one
            Debug.LogWarning($"ObjectPool: Instantiated a new '{typeof(T).Name}' because the pool was empty. Consider increasing initial pool size.");
        }

        _onGet?.Invoke(obj); // Invoke custom 'onGet' action
        return obj;
    }

    /// <summary>
    /// Returns an object to the pool, making it available for reuse.
    /// </summary>
    /// <param name="obj">The component T to return to the pool.</param>
    public void Return(T obj)
    {
        if (obj == null)
        {
            Debug.LogWarning($"ObjectPool: Attempted to return a null object to the pool for type '{typeof(T).Name}'.");
            return;
        }

        _onReturn?.Invoke(obj); // Invoke custom 'onReturn' action
        _pooledObjects.Push(obj); // Add back to stack
    }

    /// <summary>
    /// Destroys all objects currently in the pool and clears the pool.
    /// This should be called when the pool is no longer needed (e.g., on scene unload).
    /// </summary>
    public void Clear()
    {
        while (_pooledObjects.Count > 0)
        {
            T obj = _pooledObjects.Pop();
            _onDestroy?.Invoke(obj); // Invoke custom 'onDestroy' action
            if (obj != null && obj.gameObject != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject); // Permanently destroy the GameObject
            }
        }
        // If the pool created its own parent, destroy it as well.
        if (_parentTransform != null && _parentTransform.name.StartsWith($"ObjectPool_{typeof(T).Name}s_Parent"))
        {
            UnityEngine.Object.Destroy(_parentTransform.gameObject);
        }
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Instantiates a new GameObject from the prefab and retrieves its Component T.
    /// </summary>
    /// <returns>A new instance of Component T.</returns>
    private T CreatePooledObject()
    {
        GameObject go = UnityEngine.Object.Instantiate(_prefab, _parentTransform);
        T obj = go.GetComponent<T>();
        _onCreate?.Invoke(obj); // Invoke custom 'onCreate' action
        return obj;
    }
}


/// <summary>
/// Example pooled object: A simple projectile that flies forward and returns itself to the pool after a lifetime.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Speed at which the projectile moves.")]
    [SerializeField] private float speed = 10f;

    [Tooltip("Time in seconds before the projectile returns to the pool.")]
    [SerializeField] private float lifetime = 3f;

    private float _currentLifetime;                 // Current time remaining for the projectile
    private ObjectPool<Projectile> _myPool;         // Reference back to the pool that spawned this projectile

    /// <summary>
    /// Called by the spawner to provide this projectile with a reference to its pool.
    /// This is a good place for the 'onCreate' action.
    /// </summary>
    /// <param name="pool">The ObjectPool instance that created this projectile.</param>
    public void SetPool(ObjectPool<Projectile> pool)
    {
        _myPool = pool;
    }

    /// <summary>
    /// Called when the GameObject becomes enabled (e.g., when retrieved from the pool).
    /// This is where we reset its state.
    /// </summary>
    private void OnEnable()
    {
        _currentLifetime = lifetime; // Reset lifetime
    }

    /// <summary>
    /// Updates the projectile's position and checks its lifetime.
    /// </summary>
    private void Update()
    {
        // Move forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Decrease lifetime
        _currentLifetime -= Time.deltaTime;

        // If lifetime expires, return to pool
        if (_currentLifetime <= 0)
        {
            // IMPORTANT: Null check _myPool in case Projectile is placed directly in scene
            // or pool is cleared while projectile is active.
            _myPool?.Return(this);
        }
    }
}


/// <summary>
/// Example usage of the ObjectPool: A spawner that fires Projectiles using the pool.
/// </summary>
public class ProjectileSpawner : MonoBehaviour
{
    [Tooltip("The prefab GameObject that contains the Projectile component.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("The initial number of projectiles to create when the pool starts.")]
    [SerializeField] private int initialPoolSize = 10;

    [Tooltip("Optional: Parent Transform for pooled objects in the Hierarchy. If null, a new GameObject will be created.")]
    [SerializeField] private Transform poolParent;

    private ObjectPool<Projectile> _projectilePool; // Our instance of the generic object pool

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        // Initialize the object pool for Projectiles.
        // We define the custom actions here, demonstrating the flexibility of the generic pool.
        _projectilePool = new ObjectPool<Projectile>(
            projectilePrefab,
            poolParent,
            // onCreate: When a new Projectile GameObject is instantiated by the pool.
            // We set its reference back to *this* pool so it can return itself later.
            obj => obj.SetPool(_projectilePool),

            // onGet: When a Projectile is retrieved from the pool.
            // We ensure its GameObject is active.
            obj => obj.gameObject.SetActive(true),

            // onReturn: When a Projectile is returned to the pool.
            // We ensure its GameObject is inactive.
            obj => obj.gameObject.SetActive(false),

            // onDestroy: When a Projectile is permanently destroyed by the pool (e.g., pool is cleared).
            // No specific action needed here for this simple example, but could be used to clean up events.
            obj => { /* Debug.Log($"Destroying {obj.name}"); */ },
            
            initialPoolSize // Initial number of projectiles to pre-create
        );
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        // Spawn a projectile when the Space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnProjectile();
        }
    }

    /// <summary>
    /// Retrieves a projectile from the pool and positions it.
    /// </summary>
    private void SpawnProjectile()
    {
        // Get a Projectile from the pool
        Projectile newProjectile = _projectilePool.Get();

        // Position and orient the projectile at the spawner's location
        newProjectile.transform.position = transform.position;
        newProjectile.transform.rotation = transform.rotation;
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // IMPORTANT: Clear the pool when the spawner is destroyed to clean up all pooled objects.
        // This prevents memory leaks and orphaned GameObjects.
        _projectilePool?.Clear();
    }
}
```