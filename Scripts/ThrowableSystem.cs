// Unity Design Pattern Example: ThrowableSystem
// This script demonstrates the ThrowableSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ThrowableSystem' design pattern, as interpreted here, provides a centralized and modular way to manage throwable objects in a Unity game. It aims to decouple the act of throwing from the specific behavior of the throwable item and from the object pooling mechanism.

**Key Components of the ThrowableSystem Pattern:**

1.  **`IThrowable` (Interface):** Defines the contract that all throwable objects must adhere to. This allows the system to interact with any throwable polymorphically, regardless of its specific type.
2.  **`ThrowableBase` (Abstract Base Class):** Provides common functionalities and lifecycle management for all throwables. It handles basic physics setup (Rigidbody), collision detection, and integrates with the object pooling system.
3.  **`ObjectPool<T>` (Generic Pooling Utility):** A reusable class for managing object pools. This is crucial for performance, as it avoids constant instantiation and destruction of game objects.
4.  **`ThrowableSystem` (Singleton Manager):** The core of the pattern. It's a central service responsible for:
    *   Providing a `Throw` method that any character (player, AI) can call.
    *   Managing multiple object pools (one for each type of throwable prefab).
    *   Handling the lifecycle of thrown objects, including initializing them and returning them to their respective pools after use.
    *   Pre-warming pools to reduce runtime hitches.
5.  **Concrete Throwable Implementation (e.g., `ExampleGrenade`):** A specific type of throwable that implements `IThrowable` (via `ThrowableBase`) and defines its unique behaviors (e.g., exploding on impact, dealing damage).
6.  **Thrower Component (e.g., `PlayerThrower`):** A component (on a player or AI character) that initiates the throw action, calculates the trajectory, and requests the `ThrowableSystem` to throw a specific `IThrowable` prefab.

---

### Why use the ThrowableSystem Pattern?

*   **Decoupling:** The player/AI only needs to know *what* to throw (a prefab reference) and *how* to throw it (direction, force). It doesn't need to know *how* the object is instantiated, pooled, or what it does when it hits something.
*   **Reusability:** The `ThrowableSystem` and `ObjectPool` are generic and can be used for any throwable item. `ThrowableBase` provides a strong foundation for new throwable types.
*   **Maintainability:** Changes to pooling logic or the core throwing mechanism are isolated within the `ThrowableSystem`. Changes to a specific throwable's behavior are isolated within its own class.
*   **Performance:** Object pooling prevents constant `Instantiate` and `Destroy` calls, which can cause performance spikes and garbage collection overhead.
*   **Scalability:** Easily add new types of throwable items by simply creating a new class derived from `ThrowableBase` and configuring its prefab.

---

### File Structure and Code

You should create separate C# files in your Unity project for each of the following components.

**1. `IThrowable.cs`**
*   Defines the contract for all throwable objects.

```csharp
// IThrowable.cs
using UnityEngine;

/// <summary>
/// IThrowable Interface: Defines the contract for any object that can be thrown within the system.
/// This promotes polymorphism, allowing the ThrowableSystem to manage various types of throwables
/// (e.g., grenades, rocks, bottles) without knowing their concrete implementations.
/// </summary>
public interface IThrowable
{
    /// <summary>
    /// Gets the GameObject associated with this throwable.
    /// </summary>
    GameObject GameObject { get; }

    /// <summary>
    /// Gets the Rigidbody component of the throwable. Essential for physics-based throwing.
    /// </summary>
    Rigidbody Rigidbody { get; }

    /// <summary>
    /// Gets or sets the original prefab GameObject from which this throwable instance was created.
    /// This is crucial for returning the instance to the correct object pool.
    /// </summary>
    GameObject SourcePrefab { get; set; }

    /// <summary>
    /// Indicates whether this throwable is currently active (thrown and in play).
    /// Used primarily for object pooling and state management.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Initializes the throwable object.
    /// This method is called by the ThrowableSystem when a throwable is taken from the pool
    /// or instantiated, preparing it for a throw.
    /// </summary>
    /// <param name="thrower">The GameObject that initiated the throw (e.g., Player, AI).</param>
    /// <param name="sourcePrefab">The original prefab GameObject of this throwable instance.</param>
    void Initialize(GameObject thrower, GameObject sourcePrefab);

    /// <summary>
    /// Launches the throwable object with a given initial velocity.
    /// This typically involves enabling physics and applying force.
    /// </summary>
    /// <param name="initialVelocity">The calculated velocity vector for the throw.</param>
    void Launch(Vector3 initialVelocity);

    /// <summary>
    /// Called when the throwable object hits something.
    /// This method allows the throwable to react to collisions (e.g., play sound, apply damage, explode).
    /// </summary>
    /// <param name="collision">The collision data.</param>
    void OnHit(Collision collision);

    /// <summary>
    /// Resets the throwable object to its initial state, making it ready for reuse.
    /// This is crucial for object pooling.
    /// </summary>
    void ResetState();
}
```

**2. `ThrowableBase.cs`**
*   An abstract class providing common implementations for `IThrowable`, handling Rigidbody and collision.

```csharp
// ThrowableBase.cs
using UnityEngine;
using System.Collections; // For Coroutines

/// <summary>
/// ThrowableBase: An abstract MonoBehaviour that provides a common foundation for all throwable objects.
/// It implements the IThrowable interface and handles common functionalities like Rigidbody management
/// and collision detection, reducing boilerplate code for concrete throwable types.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present for physics
public abstract class ThrowableBase : MonoBehaviour, IThrowable
{
    [Header("Throwable Base Settings")]
    [Tooltip("The time after hitting something before the throwable is returned to the pool.")]
    [SerializeField] protected float returnToPoolDelay = 5f;

    protected Rigidbody _rigidbody;
    protected GameObject _thrower;
    protected bool _isThrownAndActive = false; // Internal state for IsActive
    private GameObject _sourcePrefab; // Stores the original prefab reference for pooling

    // --- IThrowable Implementation ---
    public GameObject GameObject => gameObject;
    public Rigidbody Rigidbody => _rigidbody;
    public bool IsActive => _isThrownAndActive;
    public GameObject SourcePrefab { get => _sourcePrefab; set => _sourcePrefab = value; } // Implement SourcePrefab

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"ThrowableBase: Rigidbody not found on {name}. This component requires a Rigidbody.", this);
        }
        // Ensure the Rigidbody is kinematic initially until thrown
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        gameObject.SetActive(false); // Start inactive, managed by pool
    }

    /// <summary>
    /// Initializes the throwable, setting its thrower and preparing it for launch.
    /// </summary>
    /// <param name="thrower">The GameObject that threw this object.</param>
    /// <param name="sourcePrefab">The original prefab GameObject of this throwable instance.</param>
    public virtual void Initialize(GameObject thrower, GameObject sourcePrefab)
    {
        _thrower = thrower;
        _sourcePrefab = sourcePrefab; // Store the source prefab
        _isThrownAndActive = true;
        // Optionally, reset any specific states here that are common for all throwables
        // e.g., visual effects, trail renderers.
    }

    /// <summary>
    /// Launches the throwable by enabling physics and applying an initial velocity.
    /// </summary>
    /// <param name="initialVelocity">The velocity to apply.</param>
    public virtual void Launch(Vector3 initialVelocity)
    {
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.velocity = Vector3.zero; // Clear any previous velocity
        _rigidbody.angularVelocity = Vector3.zero; // Clear any previous angular velocity
        _rigidbody.AddForce(initialVelocity, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Handles collision events. This method calls the abstract OnHit method,
    /// which concrete throwable types must implement.
    /// </summary>
    /// <param name="collision">The collision data.</param>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple hits immediately or hitting the thrower too soon
        if (!_isThrownAndActive) return;

        // If it collides with the thrower, ignore for a moment to prevent self-collision issues
        // You might want to use layers or tags for "Player" or "Thrower" to make this more robust.
        if (collision.gameObject == _thrower)
        {
            // Optionally, add a small delay before it can hit the thrower
            return;
        }

        OnHit(collision); // Delegate specific hit logic to concrete implementation
        
        // After hitting, start a timer to return to pool
        // This prevents the object from staying in the scene indefinitely.
        StartCoroutine(DelayedReturnToPool(returnToPoolDelay));
    }

    /// <summary>
    /// Abstract method for handling specific hit logic.
    /// Concrete throwable classes must implement this to define their unique reactions to impact.
    /// </summary>
    /// <param name="collision">The collision data.</param>
    public abstract void OnHit(Collision collision);

    /// <summary>
    /// Resets the throwable's state, preparing it to be returned to the object pool.
    /// </summary>
    public virtual void ResetState()
    {
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _thrower = null;
        _sourcePrefab = null; // Clear prefab reference
        _isThrownAndActive = false;
        // Stop any running coroutines that might interfere with pooling
        StopAllCoroutines(); 
        
        // Ensure it's deactivated, ThrowableSystem will handle actual pool return and deactivation
        gameObject.SetActive(false); 
    }

    /// <summary>
    /// Coroutine to delay the return of the throwable to the pool after it has hit something.
    /// This gives time for visual effects or sounds to play out.
    /// </summary>
    /// <param name="delay">The delay in seconds.</param>
    protected IEnumerator DelayedReturnToPool(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Only return to pool if still active
        if (_isThrownAndActive)
        {
            ThrowableSystem.Instance.ReturnThrowable(this);
        }
    }
}
```

**3. `ObjectPool.cs`**
*   A generic object pooling utility.

```csharp
// ObjectPool.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ObjectPool<T>: A generic object pooling utility.
/// This class helps manage a collection of reusable objects, reducing the overhead of
/// frequent instantiation and destruction, which is crucial for performance in games.
/// It's used internally by the ThrowableSystem to manage different types of IThrowables.
/// </summary>
/// <typeparam name="T">The type of object to pool, must be an IThrowable and a MonoBehaviour.</typeparam>
public class ObjectPool<T> where T : MonoBehaviour, IThrowable
{
    private Queue<T> _availableObjects = new Queue<T>();
    private List<T> _activeObjects = new List<T>(); // Keep track of active objects for management
    private T _prefab;
    private Transform _parentTransform;

    /// <summary>
    /// Initializes a new object pool.
    /// </summary>
    /// <param name="prefab">The prefab to instantiate for the pool.</param>
    /// <param name="initialSize">The initial number of objects to create in the pool.</param>
    /// <param name="parentTransform">The parent transform for pooled objects in the Hierarchy.</param>
    public ObjectPool(T prefab, int initialSize, Transform parentTransform)
    {
        _prefab = prefab;
        _parentTransform = parentTransform;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNewObject();
            Return(obj); // Add to available pool
        }
    }

    /// <summary>
    /// Gets an object from the pool. If no objects are available, a new one is instantiated.
    /// </summary>
    /// <returns>An active object of type T.</returns>
    public T Get()
    {
        T obj;
        if (_availableObjects.Count > 0)
        {
            obj = _availableObjects.Dequeue();
        }
        else
        {
            obj = CreateNewObject();
        }

        obj.GameObject.SetActive(true);
        _activeObjects.Add(obj);
        return obj;
    }

    /// <summary>
    /// Returns an object to the pool, making it available for reuse.
    /// The object is reset and deactivated.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    public void Return(T obj)
    {
        if (obj == null) return;

        obj.ResetState(); // Reset its state (position, velocity, etc.)
        obj.GameObject.SetActive(false); // Deactivate it
        _availableObjects.Enqueue(obj);
        _activeObjects.Remove(obj);
        obj.GameObject.transform.SetParent(_parentTransform); // Ensure it's under the pool parent
    }

    /// <summary>
    /// Creates a new instance of the prefab and prepares it for pooling.
    /// </summary>
    /// <returns>A new instance of type T.</returns>
    private T CreateNewObject()
    {
        // Instantiate the prefab and set its parent for organization
        T obj = Object.Instantiate(_prefab, _parentTransform);
        obj.GameObject.SetActive(false); // Initially inactive
        return obj;
    }

    /// <summary>
    /// Clears all active and available objects from the pool.
    /// This will destroy the GameObjects associated with the pooled items.
    /// </summary>
    public void ClearAll()
    {
        foreach (T obj in _activeObjects)
        {
            if (obj != null && obj.GameObject != null)
                Object.Destroy(obj.GameObject);
        }
        _activeObjects.Clear();

        foreach (T obj in _availableObjects)
        {
            if (obj != null && obj.GameObject != null)
                Object.Destroy(obj.GameObject);
        }
        _availableObjects.Clear();
    }
}
```

**4. `ThrowableSystem.cs`**
*   The central manager for all throwable objects (Singleton).

```csharp
// ThrowableSystem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ThrowableSystem: The core of the ThrowableSystem design pattern.
/// This class acts as a central manager (Singleton) for handling all throwable objects in the game.
/// It abstracts the process of throwing, managing object pools, and coordinating throwable lifecycle.
/// </summary>
public class ThrowableSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    public static ThrowableSystem Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("The initial size for new object pools if a prefab hasn't been pooled before.")]
    [SerializeField] private int defaultPoolSize = 5;

    // Dictionary to hold different object pools, keyed by the prefab GameObject
    private Dictionary<GameObject, ObjectPool<IThrowable>> _pools = new Dictionary<GameObject, ObjectPool<IThrowable>>();

    // Parent transform for all pooled objects to keep the Hierarchy clean
    private Transform _poolParent;

    private void Awake()
    {
        // Ensure only one instance of ThrowableSystem exists
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ThrowableSystem: Duplicate instance found, destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, use DontDestroyOnLoad if this system should persist across scene changes.
        // For simple examples, keeping it in the scene might be sufficient.
        // DontDestroyOnLoad(gameObject); 

        _poolParent = new GameObject("ThrowablePoolParent").transform;
        _poolParent.SetParent(transform); // Make it a child of ThrowableSystem for organization
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Clean up all pools when the system is destroyed
        foreach (var poolEntry in _pools.Values)
        {
            poolEntry.ClearAll(); // Destroy all objects managed by the pools
        }
        _pools.Clear();

        if (_poolParent != null)
        {
            Destroy(_poolParent.gameObject);
        }
    }

    /// <summary>
    /// Throws an instance of a given throwable prefab.
    /// This is the primary method for any 'thrower' (Player, AI) to interact with the system.
    /// The system handles getting an object from the pool, initializing it, and launching it.
    /// </summary>
    /// <param name="throwablePrefab">The prefab of the IThrowable to be thrown.</param>
    /// <param name="origin">The world position from where the throwable should start.</param>
    /// <param name="initialVelocity">The initial velocity to apply to the throwable.</param>
    /// <param name="thrower">The GameObject responsible for throwing (e.g., the player).</param>
    /// <returns>The IThrowable instance that was launched, or null if an error occurred.</returns>
    public IThrowable Throw(ThrowableBase throwablePrefab, Vector3 origin, Vector3 initialVelocity, GameObject thrower)
    {
        if (throwablePrefab == null || throwablePrefab.GameObject == null)
        {
            Debug.LogError("ThrowableSystem: Cannot throw a null or invalid throwable prefab.", this);
            return null;
        }

        // Get or create a pool for this specific throwable prefab
        if (!_pools.TryGetValue(throwablePrefab.GameObject, out ObjectPool<IThrowable> pool))
        {
            // Create a new pool if one doesn't exist for this prefab
            pool = new ObjectPool<IThrowable>(throwablePrefab, defaultPoolSize, _poolParent);
            _pools.Add(throwablePrefab.GameObject, pool);
            Debug.Log($"ThrowableSystem: Created new pool for {throwablePrefab.GameObject.name} with size {defaultPoolSize}.", this);
        }

        // Get a throwable instance from the pool
        IThrowable throwableInstance = pool.Get();
        if (throwableInstance == null)
        {
            Debug.LogError($"ThrowableSystem: Failed to get an instance of {throwablePrefab.GameObject.name} from the pool.", this);
            return null;
        }

        // Set its position and rotation before launching
        throwableInstance.GameObject.transform.position = origin;
        throwableInstance.GameObject.transform.rotation = Quaternion.identity; // Or align with velocity: Quaternion.LookRotation(initialVelocity.normalized)

        // Initialize the throwable with the thrower's info and its source prefab for returning to pool
        throwableInstance.Initialize(thrower, throwablePrefab.GameObject);

        // Launch the throwable with the calculated velocity
        throwableInstance.Launch(initialVelocity);

        return throwableInstance;
    }

    /// <summary>
    /// Returns a throwable object to its respective pool.
    /// This method is typically called by the throwable itself (e.g., from ThrowableBase.DelayedReturnToPool).
    /// </summary>
    /// <param name="throwable">The IThrowable instance to return.</param>
    public void ReturnThrowable(IThrowable throwable)
    {
        if (throwable == null || throwable.GameObject == null)
        {
            Debug.LogWarning("ThrowableSystem: Attempted to return a null or invalid throwable.", this);
            return;
        }

        GameObject sourcePrefab = throwable.SourcePrefab;
        if (sourcePrefab == null)
        {
            Debug.LogError($"ThrowableSystem: Cannot return throwable {throwable.GameObject.name} because its source prefab reference is null. Destroying it instead.", this);
            Destroy(throwable.GameObject); // Destroy it if we can't pool it
            return;
        }

        if (_pools.TryGetValue(sourcePrefab, out ObjectPool<IThrowable> pool))
        {
            pool.Return(throwable);
        }
        else
        {
            Debug.LogWarning($"ThrowableSystem: No pool found for prefab {sourcePrefab.name}. Destroying {throwable.GameObject.name} instead of pooling.", this);
            Destroy(throwable.GameObject); // Destroy if no pool found, prevent memory leaks
        }
    }

    /// <summary>
    /// Pre-warms a specific throwable pool by creating a given number of instances.
    /// This is useful for reducing hitches during gameplay by instantiating objects beforehand.
    /// </summary>
    /// <param name="throwablePrefab">The prefab of the IThrowable to pre-warm.</param>
    /// <param name="size">The number of instances to create if the pool doesn't exist or to ensure it has at least this many available.</param>
    public void PreWarmPool(ThrowableBase throwablePrefab, int size)
    {
        if (throwablePrefab == null || throwablePrefab.GameObject == null)
        {
            Debug.LogError("ThrowableSystem: Cannot pre-warm pool with a null or invalid throwable prefab.", this);
            return;
        }

        if (!_pools.TryGetValue(throwablePrefab.GameObject, out ObjectPool<IThrowable> pool))
        {
            pool = new ObjectPool<IThrowable>(throwablePrefab, size, _poolParent);
            _pools.Add(throwablePrefab.GameObject, pool);
            Debug.Log($"ThrowableSystem: Pre-warmed new pool for {throwablePrefab.GameObject.name} with size {size}.", this);
        }
        else
        {
            // If the pool already exists, ensure it has at least `size` objects.
            // A more complex ObjectPool would have an `EnsureCapacity` method.
            // For this example, we'll just log a warning for simplicity.
            Debug.LogWarning($"ThrowableSystem: Pool for {throwablePrefab.GameObject.name} already exists. Pre-warming request to add more objects might be ignored if the pool doesn't support dynamic resizing or already has enough objects.", this);
        }
    }
}
```

**5. `ExampleGrenade.cs`**
*   A concrete implementation of a throwable object. Includes an example `IDamageable` interface for demonstration.

```csharp
// ExampleGrenade.cs
using UnityEngine;

/// <summary>
/// ExampleGrenade: A concrete implementation of a throwable object, inheriting from ThrowableBase.
/// This class defines the specific behavior of a grenade when it's thrown and when it hits something.
/// </summary>
public class ExampleGrenade : ThrowableBase
{
    [Header("Grenade Specific Settings")]
    [Tooltip("Damage radius when the grenade explodes.")]
    [SerializeField] private float explosionRadius = 5f;
    [Tooltip("Damage dealt to objects within the explosion radius.")]
    [SerializeField] private float explosionDamage = 50f;
    [Tooltip("Force applied to objects within the explosion radius.")]
    [SerializeField] private float explosionForce = 500f;
    [Tooltip("Visual effect to play on explosion (e.g., particle system, prefab).")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [Tooltip("Sound effect to play on explosion.")]
    [SerializeField] private AudioClip explosionSound;

    private bool _hasExploded = false; // To prevent multiple explosions from one grenade

    // Override Initialize to add any grenade-specific setup
    public override void Initialize(GameObject thrower, GameObject sourcePrefab)
    {
        base.Initialize(thrower, sourcePrefab);
        _hasExploded = false; // Reset explosion state
        // You might want to enable a trail renderer here, if the grenade has one
        // Example: if (TryGetComponent(out TrailRenderer tr)) { tr.Clear(); tr.enabled = true; }
    }

    // Override OnHit to define grenade's reaction to impact
    public override void OnHit(Collision collision)
    {
        if (_hasExploded) return; // Prevent double explosion if DelayedReturnToPool is long

        Debug.Log($"Grenade {name} hit {collision.gameObject.name}!", this);
        Explode();
        _hasExploded = true; // Mark as exploded

        // The base class's OnCollisionEnter calls this OnHit method and then starts the
        // DelayedReturnToPool coroutine, so no need to explicitly call base.OnHit(collision).
    }

    /// <summary>
    /// Defines the explosion behavior of the grenade.
    /// </summary>
    private void Explode()
    {
        Debug.Log($"Grenade {name} exploded at {transform.position}!", this);

        // Instantiate explosion effect
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            // Optionally, pool explosion effects as well if they are used frequently.
            Destroy(effect, 3f); // Destroy effect after a few seconds
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Find all colliders within the explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // Avoid damaging self or the thrower
            if (hitCollider.gameObject == gameObject || hitCollider.gameObject == _thrower)
            {
                continue;
            }

            // Apply damage (example: assumes objects have an IDamageable interface or similar)
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage, _thrower);
            }

            // Apply explosion force
            Rigidbody hitRigidbody = hitCollider.GetComponent<Rigidbody>();
            if (hitRigidbody != null)
            {
                // Ensure the rigidbody is not kinematic so it can be affected by force
                if (hitRigidbody.isKinematic) hitRigidbody.isKinematic = false;
                hitRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
    }

    // Override ResetState to ensure all grenade-specific states are cleared
    public override void ResetState()
    {
        base.ResetState();
        _hasExploded = false;
        // Disable trail renderer if present
        // Example: if (TryGetComponent(out TrailRenderer tr)) tr.enabled = false;
    }

    // --- Example IDamageable Interface (for demonstration) ---
    // In a real project, this would ideally be in its own file (e.g., IDamageable.cs).
    // Included here for self-contained example.
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject dealer);
    }
}
```

**6. `PlayerThrower.cs`**
*   An example component demonstrating how a player can use the `ThrowableSystem`.

```csharp
// PlayerThrower.cs
using UnityEngine;

/// <summary>
/// PlayerThrower: An example component that allows a player to throw objects using the ThrowableSystem.
/// This class demonstrates how to integrate the ThrowableSystem into a game character's logic.
/// </summary>
public class PlayerThrower : MonoBehaviour
{
    [Header("Throwing Settings")]
    [Tooltip("The prefab of the throwable object to spawn and throw.")]
    [SerializeField] private ThrowableBase throwablePrefab;
    [Tooltip("The transform from which the throwable will originate (e.g., hand position).")]
    [SerializeField] private Transform throwOrigin;
    [Tooltip("The initial throwing force (magnitude of velocity).")]
    [SerializeField] private float throwForce = 15f;
    [Tooltip("Multiplier for vertical aim to allow higher throws.")]
    [SerializeField] private float verticalAimMultiplier = 1f;
    [Tooltip("Delay between throws.")]
    [SerializeField] private float throwCooldown = 1f;

    private float _nextThrowTime;

    private void Start()
    {
        if (throwablePrefab == null)
        {
            Debug.LogError("PlayerThrower: Throwable Prefab is not assigned! Please assign a ThrowableBase prefab.", this);
            enabled = false; // Disable script if no prefab
            return;
        }

        if (throwOrigin == null)
        {
            // Default to the player's position if no specific throw origin is set
            throwOrigin = transform; 
            Debug.LogWarning("PlayerThrower: Throw Origin not set, defaulting to player's transform.", this);
        }

        // Optional: Pre-warm the pool for the throwable prefab to avoid hitches on first throw
        // This ensures a number of grenade instances are ready before the first throw.
        if (ThrowableSystem.Instance != null)
        {
            ThrowableSystem.Instance.PreWarmPool(throwablePrefab, 10);
        }
        else
        {
            Debug.LogError("PlayerThrower: ThrowableSystem.Instance is null! Make sure ThrowableSystem is in the scene.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Example input: Left mouse click to throw
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextThrowTime)
        {
            TryThrow();
            _nextThrowTime = Time.time + throwCooldown;
        }
    }

    /// <summary>
    /// Attempts to throw the assigned throwable prefab using the ThrowableSystem.
    /// This method calculates the throw direction and delegates the actual throwing to the system.
    /// </summary>
    private void TryThrow()
    {
        if (ThrowableSystem.Instance == null)
        {
            Debug.LogError("PlayerThrower: ThrowableSystem.Instance is null! Make sure ThrowableSystem is in the scene.", this);
            return;
        }

        if (throwablePrefab == null)
        {
            Debug.LogError("PlayerThrower: No throwable prefab assigned to throw!", this);
            return;
        }

        // Calculate throw direction based on camera forward or character forward
        // Assumes a camera is present and its forward direction is appropriate for aiming.
        Vector3 throwDirection = Camera.main.transform.forward; 
        // Apply vertical aim multiplier if desired
        throwDirection.y *= verticalAimMultiplier;
        throwDirection.Normalize();

        Vector3 initialVelocity = throwDirection * throwForce;

        // Call the ThrowableSystem to handle the throw
        IThrowable thrownObject = ThrowableSystem.Instance.Throw(
            throwablePrefab,
            throwOrigin.position,
            initialVelocity,
            gameObject // Pass this GameObject as the thrower
        );

        if (thrownObject != null)
        {
            Debug.Log($"Player threw a {thrownObject.GameObject.name}!", this);
            // Optionally, add a visual effect (e.g., muzzle flash) or sound here
        }
    }
}
```

**7. `DamageableTarget.cs`**
*   A simple script for objects that can take damage, used by `ExampleGrenade`.

```csharp
// DamageableTarget.cs
using UnityEngine;

/// <summary>
/// DamageableTarget: A simple script for objects that can take damage, implementing the IDamageable interface.
/// This is used to demonstrate the functionality of the ExampleGrenade.
/// </summary>
public class DamageableTarget : MonoBehaviour, ExampleGrenade.IDamageable
{
    [SerializeField] private float health = 100f;
    [SerializeField] private GameObject hitEffectPrefab; // Optional: Particle effect on hit

    public void TakeDamage(float amount, GameObject dealer)
    {
        health -= amount;
        Debug.Log($"{name} took {amount} damage from {dealer.name}. Health: {health}", this);

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Destroy effect after a short time
        }

        if (health <= 0)
        {
            Debug.Log($"{name} destroyed!", this);
            Destroy(gameObject);
        }
    }
}
```

---

### Unity Project Setup Guide:

Follow these steps to get the example working in your Unity project:

1.  **Create C# Scripts:**
    *   In your Unity project, create a folder (e.g., `Assets/Scripts/ThrowableSystem`).
    *   Create new C# scripts with the exact names: `IThrowable`, `ObjectPool`, `ThrowableBase`, `ThrowableSystem`, `ExampleGrenade`, `PlayerThrower`, and `DamageableTarget`.
    *   Copy and paste the code for each script into its corresponding file.

2.  **Set up the `ThrowableSystem` Manager:**
    *   In your Unity scene, create an empty GameObject (e.g., `Managers`).
    *   As a child of `Managers`, create another empty GameObject named `ThrowableSystemManager`.
    *   Attach the `ThrowableSystem.cs` script to the `ThrowableSystemManager` GameObject.

3.  **Create a Throwable Prefab (ExampleGrenade):**
    *   Create a 3D Object in your scene (e.g., `3D Object > Sphere`). Name it `Grenade`.
    *   Ensure it has a `Rigidbody` component.
    *   Attach the `ExampleGrenade.cs` script to the `Grenade` GameObject.
    *   In the Inspector for `Grenade`:
        *   Adjust `Explosion Radius`, `Explosion Damage`, `Explosion Force` as desired.
        *   (Optional but recommended) Create a simple particle system for an explosion effect and drag it to the `Explosion Effect Prefab` slot.
        *   (Optional) Assign an `Explosion Sound` AudioClip.
    *   Drag this `Grenade` GameObject from the Hierarchy into your Project window (e.g., `Assets/Prefabs`) to create a Prefab.
    *   Delete the `Grenade` GameObject from your scene; the `ThrowableSystem` will instantiate it from the prefab.

4.  **Set up the Player Thrower:**
    *   Set up a simple player character (e.g., a `Capsule` with a `Camera` as a child for first-person view).
    *   Attach the `PlayerThrower.cs` script to your player GameObject.
    *   In the Inspector for `PlayerThrower`:
        *   Drag your `Grenade` Prefab (from `Assets/Prefabs`) to the `Throwable Prefab` slot.
        *   Create an empty GameObject as a child of your player, positioned where you want the throwable to originate (e.g., in front of the camera, named `ThrowPoint`). Drag this `ThrowPoint` to the `Throw Origin` slot.
        *   Adjust `Throw Force`, `Vertical Aim Multiplier`, and `Throw Cooldown` as desired.

5.  **Create Target Objects (for damage demonstration):**
    *   Create some 3D Objects in your scene (e.g., `3D Object > Cube`).
    *   Attach the `DamageableTarget.cs` script to these Cubes.
    *   In the Inspector for `DamageableTarget`, you can adjust the `Health` value.
    *   (Optional) Create a small particle system for a hit effect and assign it to the `Hit Effect Prefab` slot.

6.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Click the Left Mouse Button to throw grenades. Observe them being launched, exploding on impact, affecting nearby `DamageableTarget` objects, and then disappearing as they are returned to the object pool.

This comprehensive setup demonstrates a robust and practical implementation of the `ThrowableSystem` pattern in Unity, ideal for educational purposes and real-world game development.