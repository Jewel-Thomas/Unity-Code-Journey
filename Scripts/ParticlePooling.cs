// Unity Design Pattern Example: ParticlePooling
// This script demonstrates the ParticlePooling pattern in Unity
// Generated automatically - ready to use in your Unity project

The Particle Pooling design pattern is a crucial optimization technique in Unity (and game development in general) for managing frequently created and destroyed objects, particularly particle effects. Instantiating and destroying GameObjects repeatedly can lead to performance spikes and increased garbage collection (GC) overhead, which causes frame rate drops.

Particle Pooling addresses this by:
1.  **Pre-instantiating:** Creating a set number of objects (particles) at the start of the scene.
2.  **Recycling:** Instead of destroying objects, they are deactivated and stored in a "pool" when no longer needed.
3.  **Reusing:** When a new object is required, one is taken from the pool, reactivated, and reset, rather than creating a new one.

This script provides a complete, practical implementation of the Particle Pooling pattern, ready to be dropped into a Unity project.

---

### **ParticlePooling.cs**

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Queue

/// <summary>
///     Represents a single particle system that can be pooled.
///     This component should be attached to the root GameObject of your ParticleSystem prefab.
/// </summary>
/// <remarks>
///     It listens for the ParticleSystem.OnParticleSystemStopped event to automatically
///     return itself to the pool, minimizing manual management.
/// </remarks>
public class PoolableParticle : MonoBehaviour
{
    // A reference to the ParticleSystem component on this GameObject.
    // [HideInInspector] makes it not show in the Inspector, as it's managed internally.
    [HideInInspector] public ParticleSystem ps;

    // A reference to the ParticlePooler instance that this particle belongs to.
    private ParticlePooler _pooler;

    /// <summary>
    ///     Called when the script instance is being loaded.
    ///     Initializes the ParticleSystem reference.
    /// </summary>
    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError($"PoolableParticle: No ParticleSystem found on GameObject '{gameObject.name}'. " +
                           "This component requires a ParticleSystem to function.", this);
            enabled = false; // Disable the component if no ParticleSystem is found.
            return;
        }

        // Ensure that OnParticleSystemStopped is triggered when the system finishes.
        // This is crucial for automatic return to the pool.
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.CallbackAndDisable; // Or .Callback if you want to control disable manually.
                                                                    // CallbackAndDisable is often best for pooling.
    }

    /// <summary>
    ///     This Unity callback is invoked when the Particle System has finished playing
    ///     (i.e., all particles have died and no more are being emitted).
    ///     We use this to automatically return the particle to the pool.
    /// </summary>
    private void OnParticleSystemStopped()
    {
        // Only return if a pooler reference is set.
        if (_pooler != null)
        {
            _pooler.ReturnParticle(this);
        }
        else
        {
            Debug.LogWarning($"PoolableParticle '{gameObject.name}' finished playing but has no pooler reference. " +
                             "It will not be returned to a pool and might leak.", this);
            // In a real game, you might want to destroy it here if it's truly unmanaged.
            Destroy(gameObject);
        }
    }

    /// <summary>
    ///     Sets up this PoolableParticle with its managing pooler.
    ///     This is called by the ParticlePooler when creating or initializing the particle.
    /// </summary>
    /// <param name="poolerInstance">The ParticlePooler instance managing this particle.</param>
    public void Setup(ParticlePooler poolerInstance)
    {
        _pooler = poolerInstance;
    }

    /// <summary>
    ///     Resets the particle system to a ready state for reuse.
    ///     This is called when the particle is returned to the pool.
    /// </summary>
    public void ResetParticle()
    {
        // Stop any active particles and clear them.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Deactivate the GameObject. This effectively "hides" it until it's needed again.
        gameObject.SetActive(false);

        // Reset its position and rotation to a default or safe state (e.g., at the pooler's position).
        // This prevents old transform data from affecting new uses if not explicitly set by the caller.
        transform.SetPositionAndRotation(_pooler.transform.position, Quaternion.identity);

        // Parent it back to the pooler's GameObject for scene hierarchy organization.
        transform.SetParent(_pooler.transform);
    }
}


/// <summary>
///     The main Particle Pooling manager. This is a Singleton that handles the creation,
///     management, and recycling of PoolableParticle instances.
/// </summary>
/// <remarks>
///     Place this script on an empty GameObject in your scene (e.g., named "ParticlePooler").
///     Assign your ParticleSystem prefab (which must have a PoolableParticle component)
///     to the 'Particle Prefab' slot in the Inspector.
/// </remarks>
public class ParticlePooler : MonoBehaviour
{
    // Singleton instance. This allows easy access to the pooler from anywhere in your code.
    public static ParticlePooler Instance { get; private set; }

    [Header("Pool Configuration")]
    [Tooltip("The ParticleSystem prefab to be pooled. It must have a 'PoolableParticle' component attached.")]
    [SerializeField] private PoolableParticle particlePrefab;

    [Tooltip("The initial number of particles to create when the pool starts.")]
    [SerializeField] private int initialPoolSize = 10;

    [Tooltip("If true, new particles will be created if the pool runs out. If false, GetParticle() will return null.")]
    [SerializeField] private bool allowGrowth = true;

    // The core data structure for our pool: a Queue of available (inactive) particles.
    private readonly Queue<PoolableParticle> _availableParticles = new Queue<PoolableParticle>();

    /// <summary>
    ///     Called when the script instance is being loaded.
    ///     Initializes the Singleton and the particle pool.
    /// </summary>
    private void Awake()
    {
        // Enforce Singleton pattern:
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate poolers.
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if you want the pool to persist across scene changes.
            InitializePool();
        }
    }

    /// <summary>
    ///     Pre-populates the particle pool with the specified initial size.
    /// </summary>
    private void InitializePool()
    {
        if (particlePrefab == null)
        {
            Debug.LogError("ParticlePooler: Particle Prefab is not assigned! Cannot initialize pool.", this);
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewParticleAndAddToPool();
        }

        Debug.Log($"ParticlePooler: Initialized pool with {initialPoolSize} particles.");
    }

    /// <summary>
    ///     Instantiates a new PoolableParticle, sets it up, and adds it to the pool queue.
    /// </summary>
    /// <returns>The newly created PoolableParticle instance.</returns>
    private PoolableParticle CreateNewParticleAndAddToPool()
    {
        // Instantiate the prefab. 'false' means don't parent it immediately, we'll do that below.
        PoolableParticle newParticle = Instantiate(particlePrefab, transform.position, Quaternion.identity, transform);
        newParticle.Setup(this); // Provide the pooler reference to the new particle.
        newParticle.gameObject.name = $"{particlePrefab.name}_Pooled_{_availableParticles.Count + 1}";
        newParticle.ResetParticle(); // Deactivate it and reset its state.
        _availableParticles.Enqueue(newParticle); // Add it to the pool.
        return newParticle;
    }

    /// <summary>
    ///     Retrieves an available particle from the pool.
    ///     If the pool is empty and 'allowGrowth' is true, a new particle will be created.
    /// </summary>
    /// <returns>A ready-to-use PoolableParticle, or null if no particle is available and growth is not allowed.</returns>
    public PoolableParticle GetParticle()
    {
        PoolableParticle particleToUse = null;

        if (_availableParticles.Count > 0)
        {
            // Take an existing particle from the queue.
            particleToUse = _availableParticles.Dequeue();
        }
        else if (allowGrowth)
        {
            // Pool is empty but growth is allowed, so create a new one.
            Debug.LogWarning("ParticlePooler: Pool ran out of particles. Creating a new one (consider increasing initialPoolSize).", this);
            particleToUse = CreateNewParticleAndAddToPool(); // Create and immediately "get" it.
            _availableParticles.Dequeue(); // Remove it from the queue as it's being used.
        }
        else
        {
            // Pool is empty and growth is not allowed.
            Debug.LogError("ParticlePooler: Pool is empty and 'allowGrowth' is false. Cannot provide a particle.", this);
            return null;
        }

        // Activate the particle's GameObject so it can be used.
        if (particleToUse != null)
        {
            particleToUse.gameObject.SetActive(true);
            // It's crucial to ensure the ParticleSystem component itself is also enabled if it was disabled.
            // PoolableParticle's ResetParticle() only sets GameObject.SetActive(false), so this is fine.
        }

        return particleToUse;
    }

    /// <summary>
    ///     Returns a particle to the pool, deactivating and resetting it.
    ///     This is typically called automatically by the PoolableParticle's OnParticleSystemStopped.
    /// </summary>
    /// <param name="particle">The PoolableParticle instance to return.</param>
    public void ReturnParticle(PoolableParticle particle)
    {
        if (particle == null)
        {
            Debug.LogWarning("Attempted to return a null particle to the pool.", this);
            return;
        }

        if (!_availableParticles.Contains(particle)) // Check if it's already in the pool (prevents double-return issues).
        {
            particle.ResetParticle(); // Deactivate, clear, and reset transform.
            _availableParticles.Enqueue(particle); // Add it back to the queue.
        }
        else
        {
            Debug.LogWarning($"Particle '{particle.gameObject.name}' is already in the pool. Skipping return.", this);
        }
    }

    /// <summary>
    ///     Optional: Called when the GameObject is destroyed.
    ///     Cleans up the Singleton instance.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
///     EXAMPLE USAGE: This script demonstrates how to use the ParticlePooler.
///     Attach this to any GameObject in your scene (e.g., an empty GameObject named "Spawner").
/// </summary>
/// <remarks>
///     It will periodically request a particle from the pool and play it at a random position.
/// </remarks>
public class ParticleSpawner : MonoBehaviour
{
    [Tooltip("How often a new particle should be spawned (in seconds).")]
    [SerializeField] private float spawnRate = 0.5f;

    [Tooltip("The maximum distance from the spawner's position where particles can appear.")]
    [SerializeField] private float spawnRadius = 5f;

    private float _nextSpawnTime;

    /// <summary>
    ///     Called once per frame. Handles spawning particles based on the spawn rate.
    /// </summary>
    private void Update()
    {
        // Ensure the ParticlePooler is ready and enough time has passed since the last spawn.
        if (ParticlePooler.Instance != null && Time.time >= _nextSpawnTime)
        {
            SpawnParticle();
            _nextSpawnTime = Time.time + spawnRate; // Schedule the next spawn.
        }
    }

    /// <summary>
    ///     Requests a particle from the pool, positions it, and plays it.
    /// </summary>
    private void SpawnParticle()
    {
        // 1. Request a particle from the pool.
        PoolableParticle p = ParticlePooler.Instance.GetParticle();

        if (p != null)
        {
            // 2. Position the particle at a random location within the spawn radius.
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            p.transform.position = transform.position + randomOffset;

            // 3. Ensure the particle is parented back to nothing or the scene root if you want it unparented after Get.
            // ParticlePooler.GetParticle() already activates it and doesn't parent it after retrieval,
            // so its parent will be whatever was last set (the pooler's transform).
            // If you want it unparented, you might do: p.transform.SetParent(null); here.
            // For most particle effects, a temporary parent or no parent is fine.

            // 4. Play the particle system.
            // The PoolableParticle's OnParticleSystemStopped will automatically return it to the pool when it finishes.
            p.ps.Play();
        }
        else
        {
            Debug.LogWarning("ParticleSpawner: Failed to get a particle from the pool. " +
                             "Check if pool is empty and 'allowGrowth' is false.");
        }
    }

    /// <summary>
    ///     Draws the spawn radius in the Scene view for easier visualization.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
```

---

### **How to Use This in Unity:**

1.  **Create the Script:**
    *   Create a new C# script named `ParticlePooling` in your Unity project (e.g., in `Assets/Scripts/DesignPatterns`).
    *   Copy and paste all the code above into this new script file.

2.  **Prepare Your Particle System Prefab:**
    *   Create a new Unity Particle System (GameObject -> Effects -> Particle System).
    *   Configure it as desired (e.g., a simple explosion, smoke, sparks).
    *   **Crucially:** Add a `PoolableParticle` component to this Particle System's root GameObject.
        *   With the Particle System GameObject selected, click "Add Component" in the Inspector.
        *   Search for `PoolableParticle` and add it.
    *   Drag this Particle System GameObject from your Hierarchy into your Project window to create a Prefab (e.g., `Assets/Prefabs/ExplosionParticle`).
    *   Delete the Particle System GameObject from your Hierarchy, as it's now a prefab.

3.  **Set Up the Particle Pooler:**
    *   Create an empty GameObject in your scene (GameObject -> Create Empty).
    *   Rename it to `ParticlePooler`.
    *   Add the `ParticlePooler` component to this GameObject.
    *   In the Inspector of the `ParticlePooler` GameObject:
        *   Drag your **Particle System Prefab** (e.g., `ExplosionParticle`) from your Project window to the `Particle Prefab` slot.
        *   Adjust `Initial Pool Size` (e.g., 20) and `Allow Growth` (usually `true` for flexibility, but `false` for strict performance control).

4.  **Set Up the Particle Spawner (Example Usage):**
    *   Create another empty GameObject in your scene.
    *   Rename it to `Spawner`.
    *   Add the `ParticleSpawner` component to this GameObject.
    *   In the Inspector of the `Spawner` GameObject:
        *   Adjust `Spawn Rate` (e.g., 0.1 for rapid spawning) and `Spawn Radius` (e.g., 5).

5.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   You should see your particle effects spawning periodically around the `Spawner` GameObject.
    *   Observe the Hierarchy: Initially, the `ParticlePooler` GameObject will expand to show all the inactive pooled particles. As particles spawn, they activate and move, then return to the `ParticlePooler` and deactivate when they finish playing. You will *not* see new `(Clone)` objects appearing and disappearing in the Hierarchy after the initial pool setup, demonstrating the recycling in action.

This setup provides a robust and educational example of the Particle Pooling pattern, emphasizing reusability and minimizing runtime allocations for efficient particle management in Unity.