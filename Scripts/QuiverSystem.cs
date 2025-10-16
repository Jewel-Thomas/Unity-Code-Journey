// Unity Design Pattern Example: QuiverSystem
// This script demonstrates the QuiverSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'QuiverSystem' design pattern, often referred to as an Object Pool, is a creational pattern used in game development to manage the instantiation and destruction of frequently used objects, like projectiles, enemies, or visual effects. Instead of creating new objects and destroying old ones, the QuiverSystem maintains a "quiver" (pool) of pre-instantiated, inactive objects. When an object is needed, it's "drawn" (activated) from the quiver. When it's no longer needed, it's "returned" (deactivated) to the quiver, ready for reuse.

This significantly reduces performance overhead from garbage collection and memory allocation, leading to smoother gameplay, especially in performance-critical scenarios.

---

**Scenario: Bullet QuiverSystem for a Top-Down Shooter**

We'll create a system to manage bullets. Instead of `Instantiate`ing and `Destroy`ing bullets every time we shoot, we'll pool them.

---

## 1. Project Setup in Unity

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a "Prefabs" Folder**: Inside `Assets`, create `Prefabs`.
3.  **Create a "Scripts" Folder**: Inside `Assets`, create `Scripts`.
4.  **Create a Material**: Right-click in `Assets`, `Create -> Material`. Name it `RedBulletMaterial`. Set its `Albedo` color to red.

---

## 2. Bullet Prefab Creation

1.  **Create a 3D Object (Sphere)**: In the Hierarchy, `Create Empty` -> `3D Object` -> `Sphere`.
2.  **Rename**: Rename it to `Bullet`.
3.  **Adjust Scale**: Set its `Scale` to `X:0.2, Y:0.2, Z:0.2`.
4.  **Add Rigidbody**: Select `Bullet`, `Add Component` -> `Rigidbody`. Uncheck `Use Gravity`.
5.  **Apply Material**: Drag `RedBulletMaterial` onto the `Bullet` object in the Scene view or Hierarchy.
6.  **Create Prefab**: Drag the `Bullet` GameObject from the Hierarchy into your `Prefabs` folder.
7.  **Delete from Hierarchy**: Delete the `Bullet` GameObject from the Hierarchy.

---

## 3. C# Scripts

Create the following three C# scripts in your `Scripts` folder:

1.  `Bullet.cs` (The pooled object)
2.  `BulletQuiverSystem.cs` (The QuiverSystem/Object Pool manager)
3.  `BulletShooter.cs` (An example script to demonstrate drawing bullets)

---

### Script 1: `Bullet.cs` (The Pooled Item)

This script defines the behavior of a single bullet. It handles its movement, lifetime, and notifies the QuiverSystem when it's done.

```csharp
using UnityEngine;
using System; // For Action delegate

/// <summary>
/// Represents a single bullet object that can be pooled.
/// It handles its own movement, lifetime, and collision,
/// then notifies the QuiverSystem when it's ready to be returned to the pool.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("The speed at which the bullet travels.")]
    [SerializeField] private float speed = 20f;
    [Tooltip("The maximum time (in seconds) the bullet will be active before being returned to the pool.")]
    [SerializeField] private float lifetime = 3f;

    private Rigidbody rb;
    private float currentLifetime;

    // An event that the BulletQuiverSystem subscribes to.
    // When the bullet is "finished" (lifetime expires or hits something),
    // it invokes this event, signaling the QuiverSystem to return it to the pool.
    public event Action<Bullet> OnBulletFinished;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes references.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Bullet requires a Rigidbody component!", this);
            enabled = false; // Disable script if Rigidbody is missing
        }
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// This is where we reset the bullet's state for reuse.
    /// </summary>
    private void OnEnable()
    {
        currentLifetime = lifetime; // Reset lifetime counter
        rb.velocity = Vector3.zero; // Ensure no residual velocity from previous use
        rb.angularVelocity = Vector3.zero; // Ensure no residual angular velocity
    }

    /// <summary>
    /// Called once per frame.
    /// Manages the bullet's lifetime.
    /// </summary>
    private void Update()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            FinishBullet(); // Lifetime expired, return to pool
        }
    }

    /// <summary>
    /// Launches the bullet in a specified direction.
    /// This method is called by the QuiverSystem after drawing the bullet.
    /// </summary>
    /// <param name="position">The starting position of the bullet.</param>
    /// <param name="rotation">The starting rotation of the bullet.</param>
    /// <param name="direction">The normalized direction vector for the bullet's movement.</param>
    public void Launch(Vector3 position, Quaternion rotation, Vector3 direction)
    {
        transform.position = position;
        transform.rotation = rotation;
        rb.velocity = direction * speed; // Apply initial velocity
        gameObject.SetActive(true); // Ensure it's active
    }

    /// <summary>
    /// Called when this collider/rigidbody has begun touching another rigidbody/collider.
    /// </summary>
    /// <param name="collision">The Collision data associated with this collision event.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // For simplicity, any collision makes the bullet finish.
        // In a real game, you might check tags, layers, or health.
        FinishBullet();
    }

    /// <summary>
    /// Deactivates the bullet and notifies the QuiverSystem to return it to the pool.
    /// </summary>
    private void FinishBullet()
    {
        if (gameObject.activeInHierarchy) // Only process if currently active to prevent double-return
        {
            OnBulletFinished?.Invoke(this); // Notify subscribers (the QuiverSystem)
            gameObject.SetActive(false);    // Deactivate the bullet
        }
    }
}

```

---

### Script 2: `BulletQuiverSystem.cs` (The QuiverSystem/Object Pool Manager)

This is the core of the QuiverSystem pattern. It manages the pool of `Bullet` objects.

```csharp
using UnityEngine;
using System.Collections.Generic; // For Queue
using System; // For Action delegate

/// <summary>
/// The central QuiverSystem (Object Pool) for Bullet objects.
/// It pre-instantiates a set number of bullets, stores them in a queue,
/// and provides methods to 'Draw' (get) and 'Return' (put back) bullets.
/// This pattern reduces the overhead of Instantiate/Destroy calls, improving performance.
/// </summary>
public class BulletQuiverSystem : MonoBehaviour
{
    // Singleton pattern for easy global access (optional but common for pool managers)
    public static BulletQuiverSystem Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("The Bullet prefab to be pooled.")]
    [SerializeField] private Bullet bulletPrefab;
    [Tooltip("The initial number of bullets to create in the pool at startup.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("The Transform that will parent all pooled bullet objects for organization.")]
    [SerializeField] private Transform parentForPooledObjects;

    // The core of the QuiverSystem: a queue of available (inactive) bullets.
    private Queue<Bullet> availableBullets = new Queue<Bullet>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and populates the initial pool.
    /// </summary>
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // If no parent is specified, use this GameObject as the parent.
        if (parentForPooledObjects == null)
        {
            parentForPooledObjects = this.transform;
        }

        InitializePool(initialPoolSize);
    }

    /// <summary>
    /// Pre-populates the object pool with a specified number of bullets.
    /// </summary>
    /// <param name="size">The number of bullets to create for the initial pool.</param>
    private void InitializePool(int size)
    {
        for (int i = 0; i < size; i++)
        {
            CreateNewBulletAndAddToPool();
        }
        Debug.Log($"BulletQuiverSystem initialized with {size} bullets.");
    }

    /// <summary>
    /// Instantiates a new bullet, sets its parent, deactivates it,
    /// and adds it to the available pool. It also subscribes to the bullet's
    /// OnBulletFinished event.
    /// </summary>
    private Bullet CreateNewBulletAndAddToPool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Quiver System: Bullet Prefab is not assigned!", this);
            return null;
        }

        Bullet newBullet = Instantiate(bulletPrefab, parentForPooledObjects);
        newBullet.gameObject.SetActive(false); // New bullets start inactive

        // Subscribe to the bullet's event so it can notify us when it's done.
        newBullet.OnBulletFinished += ReturnBulletToPool;

        availableBullets.Enqueue(newBullet);
        return newBullet;
    }

    /// <summary>
    /// Draws an inactive bullet from the pool. If the pool is empty,
    /// a new bullet is instantiated and added to the pool before being drawn.
    /// </summary>
    /// <returns>An active Bullet instance ready for use.</returns>
    public Bullet DrawBullet()
    {
        Bullet bulletToDraw;

        // If no bullets are available, create a new one to grow the pool dynamically.
        if (availableBullets.Count == 0)
        {
            Debug.LogWarning("Bullet Quiver System: Pool empty! Creating new bullet dynamically.", this);
            bulletToDraw = CreateNewBulletAndAddToPool(); // This also enqueues it
            bulletToDraw = availableBullets.Dequeue(); // Then dequeue the new one
        }
        else
        {
            bulletToDraw = availableBullets.Dequeue();
        }

        // Ensure the bullet is active when drawn. Its OnEnable will handle further setup.
        bulletToDraw.gameObject.SetActive(true);
        return bulletToDraw;
    }

    /// <summary>
    /// Returns a bullet to the pool, deactivating it and making it available for reuse.
    /// This method is called by the bullet itself via the OnBulletFinished event.
    /// </summary>
    /// <param name="bullet">The bullet instance to be returned to the pool.</param>
    private void ReturnBulletToPool(Bullet bullet)
    {
        // Reset its state before returning to the pool
        // The Bullet.OnEnable handles some resets, but Rigidbody velocity needs explicit reset here.
        // It's crucial that the bullet is fully reset before being returned.
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.velocity = Vector3.zero;
            bulletRb.angularVelocity = Vector3.zero;
        }

        bullet.transform.SetParent(parentForPooledObjects); // Re-parent for cleanliness
        bullet.gameObject.SetActive(false); // Deactivate it
        availableBullets.Enqueue(bullet);   // Add back to the pool
    }

    // Optional: For debugging, you can see current pool size.
    public int GetAvailableBulletCount()
    {
        return availableBullets.Count;
    }
}
```

---

### Script 3: `BulletShooter.cs` (Example Usage)

This script demonstrates how a game object would interact with the `BulletQuiverSystem` to "shoot" bullets.

```csharp
using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the BulletQuiverSystem to fire bullets.
/// Attach this to a GameObject that should be able to shoot.
/// </summary>
public class BulletShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("The rate at which the shooter can fire bullets (bullets per second).")]
    [SerializeField] private float fireRate = 5f;
    [Tooltip("The point from which bullets will be fired.")]
    [SerializeField] private Transform firePoint;

    private float nextFireTime;

    /// <summary>
    /// Called once per frame. Handles player input for shooting.
    /// </summary>
    private void Update()
    {
        // Check if the Space key is pressed and enough time has passed since the last shot
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
        {
            ShootBullet();
            nextFireTime = Time.time + 1f / fireRate; // Set next allowed fire time
        }
    }

    /// <summary>
    /// Retrieves a bullet from the BulletQuiverSystem and launches it.
    /// </summary>
    private void ShootBullet()
    {
        // 1. Get a bullet from the QuiverSystem
        if (BulletQuiverSystem.Instance == null)
        {
            Debug.LogError("BulletQuiverSystem.Instance is null! Make sure the Quiver System is in the scene.", this);
            return;
        }

        Bullet bullet = BulletQuiverSystem.Instance.DrawBullet();

        if (bullet != null)
        {
            // 2. Position and launch the bullet
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
            Vector3 forwardDirection = firePoint != null ? firePoint.forward : transform.forward;

            bullet.Launch(spawnPosition, spawnRotation, forwardDirection);
        }
    }

    /// <summary>
    /// Draws a gizmo to represent the fire point in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 1f);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
        }
    }
}
```

---

## 4. Unity Scene Setup

1.  **Create an Empty GameObject** in the Hierarchy. Rename it to `_GameManagers`.
2.  **Create another Empty GameObject** as a child of `_GameManagers`. Rename it to `BulletQuiverSystem`.
3.  **Attach `BulletQuiverSystem.cs`** to the `BulletQuiverSystem` GameObject.
4.  **Configure `BulletQuiverSystem`**:
    *   Drag the `Bullet` prefab from your `Prefabs` folder into the `Bullet Prefab` slot.
    *   You can leave `Parent For Pooled Objects` empty; it will default to the `BulletQuiverSystem` GameObject itself.
    *   Set `Initial Pool Size` to something like `20`.

5.  **Create a `Player` GameObject** (e.g., a Cube):
    *   In the Hierarchy, `Create Empty` -> `3D Object` -> `Cube`. Rename it `Player`.
    *   Position it at `X:0, Y:1, Z:0`.
    *   **Add a Child GameObject** to `Player` named `FirePoint`. Position it slightly in front of the Player (e.g., `X:0, Y:0, Z:0.6`). This will be where bullets originate.

6.  **Attach `BulletShooter.cs`** to the `Player` GameObject.
7.  **Configure `BulletShooter`**:
    *   Drag the `FirePoint` child GameObject from `Player` into the `Fire Point` slot of the `BulletShooter` component.
    *   You can adjust `Fire Rate` (e.g., `10` bullets/second).

8.  **Add a simple `Ground` plane** (optional, but good for collision):
    *   `Create Empty` -> `3D Object` -> `Plane`. Rename it `Ground`.
    *   Position it at `X:0, Y:0, Z:0`.
    *   Scale it up (e.g., `X:5, Y:1, Z:5`).

---

## 5. How it Works (QuiverSystem Explanation)

1.  **`Bullet.cs` (The Arrow/Pooled Item):**
    *   Each `Bullet` object is a self-contained unit that knows how to move, detect collisions, and measure its own lifetime.
    *   Crucially, it has an `OnBulletFinished` event. When its lifetime runs out or it hits something, it invokes this event, passing itself (`this`) as an argument.
    *   It uses `OnEnable()` to reset its state (lifetime, clear Rigidbody velocity) whenever it's drawn from the pool and activated. This ensures it starts fresh.
    *   It calls `gameObject.SetActive(false)` when it's finished, making itself invisible and inactive.

2.  **`BulletQuiverSystem.cs` (The Quiver/Pool Manager):**
    *   **Singleton:** `Instance` provides a global, easy way to access the pool manager from anywhere (`BulletQuiverSystem.Instance.DrawBullet()`).
    *   **Initialization (`Awake()` and `InitializePool()`):**
        *   When the game starts, it creates `initialPoolSize` instances of the `bulletPrefab`.
        *   Each new `Bullet` is immediately set to `gameObject.SetActive(false)` and added to a `Queue<Bullet>` called `availableBullets`. This is our "quiver" of inactive, ready-to-use objects.
        *   For each bullet created, the `BulletQuiverSystem` *subscribes* its `ReturnBulletToPool` method to the bullet's `OnBulletFinished` event. This is the magic handshake!
    *   **Drawing a Bullet (`DrawBullet()`):**
        *   When a component (like `BulletShooter`) needs a bullet, it calls `DrawBullet()`.
        *   The system first checks if `availableBullets` is empty.
        *   If it's empty, it dynamically `Instantiate`s a *new* bullet, sets up its parent and event subscription, and adds it to the pool (thus "growing" the quiver). This ensures the game never runs out of bullets.
        *   It then `Dequeue`s a bullet from the `availableBullets` queue.
        *   Finally, it calls `gameObject.SetActive(true)` on the drawn bullet, making it visible and active, and returns it.
    *   **Returning a Bullet (`ReturnBulletToPool()`):**
        *   This method is automatically called when a `Bullet` invokes its `OnBulletFinished` event.
        *   The returned `bullet` has its Rigidbody velocity cleared (important to reset physics state).
        *   Its `gameObject.SetActive(false)` is called, making it invisible and inactive again.
        *   It's then `Enqueue`d back into the `availableBullets` queue, ready for its next use.

3.  **`BulletShooter.cs` (The Archer/User of the QuiverSystem):**
    *   This script simply gets a reference to the `BulletQuiverSystem.Instance`.
    *   When the player shoots, instead of `Instantiate`ing a new `Bullet`, it calls `BulletQuiverSystem.Instance.DrawBullet()`.
    *   It then takes the returned `Bullet` object and calls its `Launch()` method, providing the starting position, rotation, and direction. This effectively "fires" the pre-existing, now active, bullet.

This setup ensures that `Instantiate` and `Destroy` are called very infrequently (only during initial setup or dynamic pool growth), leading to much better performance and reduced garbage collection spikes during gameplay.

---

You should now be able to run your Unity scene, press the Space bar, and observe bullets being fired and then disappearing/returning to the pool without new `Instantiate` or `Destroy` calls flooding your profiler!