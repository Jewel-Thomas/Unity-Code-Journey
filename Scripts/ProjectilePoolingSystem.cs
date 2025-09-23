// Unity Design Pattern Example: ProjectilePoolingSystem
// This script demonstrates the ProjectilePoolingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Projectile Pooling System is a crucial optimization pattern in game development, especially in Unity, for handling frequently spawned and destroyed objects like bullets, rockets, or particle effects.

**Why Use It?**

1.  **Performance:** Instantiating and destroying GameObjects at runtime is an expensive operation. It involves memory allocation, garbage collection (GC), and object initialization, which can cause performance spikes (stuttering or "lag") in your game.
2.  **Garbage Collection (GC) Optimization:** Constant `Instantiate` and `Destroy` calls lead to a lot of garbage being created and collected by the C# garbage collector. This can interrupt gameplay as the GC pauses your game thread to clean up memory. Object pooling drastically reduces GC overhead by reusing objects instead of creating new ones.

**How It Works (Projectile Pooling System)**

The core idea is to maintain a "pool" of pre-instantiated, inactive projectiles. When a projectile is needed:

1.  **Retrieve:** Instead of creating a new one, we take an available projectile from the pool.
2.  **Activate & Position:** We activate it (`GameObject.SetActive(true)`), set its position, rotation, and any other necessary properties.
3.  **Use:** The projectile performs its function (e.g., moves, collides).
4.  **Return:** When the projectile is no longer needed (e.g., it hits something, goes off-screen, or its lifetime expires), it's deactivated (`GameObject.SetActive(false)`) and returned to the pool, ready to be reused.

This example provides a complete, self-contained system.

---

### Setup Instructions for Unity

1.  **Create Folders:**
    *   In your Unity Project window, create a folder named `Scripts`.
    *   Inside `Scripts`, create `PoolingSystem` and `Examples`.
    *   Create a folder named `Prefabs`.

2.  **Create C# Scripts:**
    *   Create `ProjectilePoolingSystem.cs` in `Scripts/PoolingSystem`.
    *   Create `Projectile.cs` in `Scripts/PoolingSystem`.
    *   Create `Shooter.cs` in `Scripts/Examples`.

3.  **Copy-Paste Code:** Paste the respective code blocks below into the newly created scripts.

4.  **Create Projectile Prefab:**
    *   Go to `GameObject -> 3D Object -> Sphere` (or Capsule, Cube, etc.).
    *   Rename it to `ProjectilePrefab`.
    *   Add a `Rigidbody` component to it (uncheck "Use Gravity"). This is important for physics-based movement or collision, even if we move it by `transform.position`.
    *   Drag the `Projectile.cs` script onto this `ProjectilePrefab` GameObject in the Hierarchy.
    *   Adjust `Move Speed` and `Lifespan` in the `Projectile` component if desired.
    *   Drag `ProjectilePrefab` from the Hierarchy into your `Prefabs` folder in the Project window to create a prefab.
    *   Delete the `ProjectilePrefab` GameObject from the Hierarchy (we only need the prefab asset).

5.  **Create Empty GameObjects:**
    *   Create an empty GameObject in your scene named `PoolingSystem`.
    *   Create an empty GameObject in your scene named `Shooter`.
    *   Create an empty GameObject named `FirePoint` as a child of `Shooter`. Position it slightly in front of the `Shooter` (e.g., `y=0, z=0.5`).

6.  **Assign Scripts and References:**
    *   Drag `ProjectilePoolingSystem.cs` onto the `PoolingSystem` GameObject.
    *   Drag `Shooter.cs` onto the `Shooter` GameObject.
    *   On the `PoolingSystem` GameObject, drag your `ProjectilePrefab` from the `Prefabs` folder into the `Projectile Prefab` slot in the Inspector. Set `Initial Pool Size` (e.g., 20).
    *   On the `Shooter` GameObject, drag its child `FirePoint` into the `Fire Point` slot in the Inspector. Adjust `Fire Rate` (e.g., 0.15).

7.  **Run the Scene:** Press Play. You should be able to click the left mouse button to shoot projectiles. Watch the Hierarchy; you'll see projectiles activate and deactivate, but not be destroyed and recreated, demonstrating the pooling system.

---

### 1. `ProjectilePoolingSystem.cs`

This script manages the pool of projectiles. It's designed as a Singleton for easy access from other scripts.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Queue<T>

namespace ProjectilePooling
{
    /// <summary>
    /// Manages a pool of projectile GameObjects for efficient reuse.
    /// This prevents constant Instantiate/Destroy calls, reducing GC overhead.
    /// </summary>
    public class ProjectilePoolingSystem : MonoBehaviour
    {
        // --- Singleton Pattern Implementation ---
        // Provides a global access point to the single instance of the pooling system.
        public static ProjectilePoolingSystem Instance { get; private set; }

        [Header("Pool Configuration")]
        [Tooltip("The Projectile prefab to be pooled. Must have a Projectile component.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("The initial number of projectiles to create when the game starts.")]
        [SerializeField] private int initialPoolSize = 10;

        [Tooltip("If true, the pool will grow dynamically if all projectiles are in use.")]
        [SerializeField] private bool canPoolGrow = true;

        // --- Internal Pool Storage ---
        // Queue for available (inactive) projectiles for fast enqueue/dequeue.
        private Queue<Projectile> availableProjectiles = new Queue<Projectile>();

        // List to keep track of all projectiles (active and inactive) for management purposes.
        // Useful if you need to iterate through all projectiles for debugging or global resets.
        private List<Projectile> allProjectiles = new List<Projectile>();

        // --- Awake Method: Initialize Singleton and Pool ---
        private void Awake()
        {
            // Enforce Singleton pattern:
            // If an instance already exists and it's not this one, destroy this duplicate.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                // Set this instance as the singleton.
                Instance = this;
                // Don't destroy this GameObject when loading new scenes if you want the pool to persist.
                // DontDestroyOnLoad(gameObject); // Uncomment if pooling system should persist across scenes.

                // Initialize the pool by pre-populating it with the initialPoolSize.
                InitializePool();
            }
        }

        /// <summary>
        /// Populates the pool with the initial set of projectiles.
        /// </summary>
        private void InitializePool()
        {
            // Create a parent GameObject to keep the pooled projectiles organized in the Hierarchy.
            GameObject poolParent = new GameObject("ProjectilePool");
            // Make the poolParent a child of this PoolingSystem for cleaner hierarchy.
            poolParent.transform.SetParent(transform);

            for (int i = 0; i < initialPoolSize; i++)
            {
                // Create a new projectile instance.
                Projectile newProjectile = CreateNewProjectile(poolParent.transform);
                // Add it to the list of all projectiles.
                allProjectiles.Add(newProjectile);
                // Add it to the queue of available projectiles.
                availableProjectiles.Enqueue(newProjectile);
            }

            Debug.Log($"ProjectilePool initialized with {initialPoolSize} projectiles.");
        }

        /// <summary>
        /// Creates a new projectile GameObject and gets its Projectile component.
        /// Deactivates it and sets its parent to the pool's container.
        /// </summary>
        /// <param name="parentTransform">The transform to parent the new projectile to.</param>
        /// <returns>The Projectile component of the newly created projectile.</returns>
        private Projectile CreateNewProjectile(Transform parentTransform)
        {
            // Instantiate the projectile prefab.
            GameObject obj = Instantiate(projectilePrefab, parentTransform);
            // Get the Projectile component from the instantiated GameObject.
            Projectile projectile = obj.GetComponent<Projectile>();

            if (projectile == null)
            {
                Debug.LogError($"Projectile prefab '{projectilePrefab.name}' is missing a 'Projectile' component!", projectilePrefab);
                // Destroy the illegally created object to prevent issues.
                Destroy(obj);
                return null;
            }

            // Deactivate the projectile immediately, as it's not yet in use.
            obj.SetActive(false);
            return projectile;
        }

        /// <summary>
        /// Retrieves an available projectile from the pool.
        /// If no projectiles are available and 'canPoolGrow' is true, a new one is created.
        /// </summary>
        /// <returns>An active Projectile component ready for use, or null if no projectile is available and pool cannot grow.</returns>
        public Projectile GetProjectile()
        {
            Projectile projectileToUse = null;

            // Check if there are available projectiles in the queue.
            if (availableProjectiles.Count > 0)
            {
                // Dequeue an existing projectile.
                projectileToUse = availableProjectiles.Dequeue();
            }
            // If no projectiles are available and the pool is allowed to grow.
            else if (canPoolGrow)
            {
                Debug.LogWarning("Projectile pool exhausted! Creating a new projectile dynamically.");
                // Create a new projectile and add it to the 'allProjectiles' list.
                // The parent is the same as the initial pool parent for consistency.
                projectileToUse = CreateNewProjectile(transform.Find("ProjectilePool"));
                allProjectiles.Add(projectileToUse);
            }
            else
            {
                Debug.LogWarning("No projectiles available and pool cannot grow. Cannot get projectile.");
                return null; // No projectile available.
            }

            // Activate the GameObject so it can be used.
            if (projectileToUse != null)
            {
                projectileToUse.gameObject.SetActive(true);
            }
            
            return projectileToUse;
        }

        /// <summary>
        /// Returns a projectile to the pool, deactivating it.
        /// </summary>
        /// <param name="projectile">The Projectile component to return.</param>
        public void ReturnProjectile(Projectile projectile)
        {
            if (projectile == null)
            {
                Debug.LogWarning("Attempted to return a null projectile to the pool.");
                return;
            }

            // Deactivate the GameObject.
            projectile.gameObject.SetActive(false);

            // Reset its position/rotation for cleanliness (optional but good practice).
            projectile.transform.position = Vector3.zero;
            projectile.transform.rotation = Quaternion.identity;

            // Clear any lingering Rigidbody forces/velocity.
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Add it back to the queue of available projectiles.
            availableProjectiles.Enqueue(projectile);
        }

        // Example of how to use this system:
        /*
        // In your Shooter script's Fire method:
        void Fire()
        {
            Projectile projectile = ProjectilePoolingSystem.Instance.GetProjectile();
            if (projectile != null)
            {
                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = firePoint.rotation;
                // Call a method on the projectile to initialize its specific behavior
                projectile.Launch(moveDirection, speed, lifespan);
            }
        }

        // In your Projectile script's Update or OnCollisionEnter method:
        void DeactivateProjectile()
        {
            // Call the pooling system to return this projectile.
            ProjectilePoolingSystem.Instance.ReturnProjectile(this);
        }
        */
    }
}
```

### 2. `Projectile.cs`

This script defines the behavior of an individual projectile. It's designed to work with the pooling system.

```csharp
using UnityEngine;

namespace ProjectilePooling
{
    /// <summary>
    /// Represents a single projectile managed by the ProjectilePoolingSystem.
    /// Handles its own movement, lifetime, and returns itself to the pool when done.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present for physics interactions
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [Tooltip("The speed at which the projectile moves forward.")]
        [SerializeField] private float moveSpeed = 20f;

        [Tooltip("The maximum time (in seconds) the projectile will be active before returning to the pool.")]
        [SerializeField] private float lifespan = 3f;

        private Rigidbody rb;
        private float currentLifespan;

        // --- MonoBehaviour Lifecycle Methods ---

        private void Awake()
        {
            // Get the Rigidbody component once to avoid repeated GetComponent calls.
            rb = GetComponent<Rigidbody>();
            // Ensure Rigidbody is kinematic or configured for desired movement
            // For simple forward movement, we might set velocity directly.
            rb.isKinematic = false; // Set to true if you are only moving via transform.position.
            rb.useGravity = false; // Projectiles often don't use gravity.
        }

        private void OnEnable()
        {
            // When the projectile is activated (taken from the pool), reset its lifespan.
            currentLifespan = lifespan;

            // OPTIONAL: Reset Rigidbody velocity/angular velocity here if it's not done in PoolingSystem.ReturnProjectile
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // For direct movement, you might apply an initial force or set velocity here.
                // rb.AddForce(transform.forward * moveSpeed, ForceMode.VelocityChange);
            }

            // Debug.Log($"Projectile {gameObject.name} activated.");
        }

        private void Update()
        {
            // Move the projectile forward.
            // Using transform.Translate or directly setting velocity is common.
            // For Rigidbody-based movement, it's better to use FixedUpdate or set rb.velocity.
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

            // Decrease the remaining lifespan.
            currentLifespan -= Time.deltaTime;

            // If lifespan runs out, deactivate the projectile and return it to the pool.
            if (currentLifespan <= 0)
            {
                Deactivate();
            }
        }

        // --- Custom Methods ---

        /// <summary>
        /// Deactivates the projectile and returns it to the pooling system.
        /// This should be called when the projectile is no longer needed (e.g., hits something, expires).
        /// </summary>
        public void Deactivate()
        {
            // Ensure the pooling system instance exists before trying to return.
            if (ProjectilePoolingSystem.Instance != null)
            {
                ProjectilePoolingSystem.Instance.ReturnProjectile(this);
            }
            else
            {
                // If the pooling system is destroyed or not initialized, destroy the projectile directly
                // to prevent orphaned GameObjects. This should ideally not happen in a well-managed setup.
                Debug.LogWarning($"Pooling System not found! Destroying {gameObject.name} directly.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when the projectile collides with another object.
        /// </summary>
        /// <param name="other">The Collision data.</param>
        private void OnCollisionEnter(Collision other)
        {
            // Example: If projectile hits a wall or enemy, deactivate it.
            // You might add logic here to check tags, apply damage, play effects, etc.
            Debug.Log($"Projectile {gameObject.name} hit {other.gameObject.name}!");
            Deactivate();
        }

        // A public method to initialize projectile properties if needed.
        // The shooter would call this after getting the projectile from the pool.
        public void Launch(Vector3 initialPosition, Quaternion initialRotation)
        {
            // The pooling system sets position/rotation on GetProjectile,
            // but this method can be used to set other initial states if needed.
            // Example: if projectiles had different speeds or colors based on origin.
            // this.moveSpeed = someNewSpeed; // If speed can vary.
            // this.transform.position = initialPosition; // Redundant if shooter sets it directly.
            // this.transform.rotation = initialRotation; // Redundant if shooter sets it directly.
            // currentLifespan = lifespan; // Redundant if done in OnEnable.
        }
    }
}
```

### 3. `Shooter.cs`

This script demonstrates how to retrieve and use projectiles from the `ProjectilePoolingSystem`.

```csharp
using UnityEngine;

namespace ProjectilePooling.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the ProjectilePoolingSystem to fire projectiles.
    /// </summary>
    public class Shooter : MonoBehaviour
    {
        [Header("Shooter Configuration")]
        [Tooltip("The point from which projectiles will be fired.")]
        [SerializeField] private Transform firePoint;

        [Tooltip("The rate at which projectiles can be fired (projectiles per second).")]
        [SerializeField] private float fireRate = 0.2f;

        private float nextFireTime = 0f;

        // --- MonoBehaviour Lifecycle Methods ---
        private void Update()
        {
            // Check for input (e.g., Left Mouse Button or Space Key)
            // and if enough time has passed since the last shot.
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                ShootProjectile();
                // Update the next allowed fire time.
                nextFireTime = Time.time + fireRate;
            }
        }

        /// <summary>
        /// Retrieves a projectile from the pool, positions it, and activates it.
        /// </summary>
        private void ShootProjectile()
        {
            // 1. Get a projectile from the pooling system.
            Projectile projectile = ProjectilePoolingSystem.Instance.GetProjectile();

            // Check if a projectile was successfully retrieved (might be null if pool cannot grow and is empty).
            if (projectile != null)
            {
                // 2. Position and orient the projectile at the fire point.
                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = firePoint.rotation;

                // 3. (Optional) Initialize any specific properties of the projectile.
                // The 'Launch' method in Projectile can be used for this if projectile has varying properties.
                // projectile.Launch(firePoint.position, firePoint.rotation);
                // Note: Launch method in Projectile is currently minimal as OnEnable handles most basic setup.
                // If you had different projectile types or variable speeds, you'd pass those here.

                Debug.Log($"Fired a projectile from {firePoint.name} at {firePoint.position}");
            }
            else
            {
                Debug.LogWarning("Failed to get projectile from pool. Pool might be exhausted.");
            }
        }
    }
}
```