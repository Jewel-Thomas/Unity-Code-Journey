// Unity Design Pattern Example: MeteorShowerSystem
// This script demonstrates the MeteorShowerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "MeteorShowerSystem" pattern, as we'll define it, is a composite design pattern used in game development to efficiently create and manage a large number of transient, similar objects (like meteors, projectiles, particles, or enemies) that appear over time. It primarily combines the **Manager/Orchestrator**, **Object Pool**, and **Product** patterns to achieve performance, flexibility, and modularity.

---

### **MeteorShowerSystem Design Pattern Definition**

The "MeteorShowerSystem" describes a robust and performant way to implement a continuous or burst generation of game objects. It consists of:

1.  **The Manager (e.g., `MeteorShowerManager`):**
    *   **Role:** The central orchestrator. It defines *when*, *where*, and *how often* objects should be spawned. It sets the overall parameters for the "shower" (e.g., duration, intensity, general properties like min/max speed/size).
    *   **Delegation:** It does *not* create objects directly, but instead requests them from an Object Pool.
    *   **Control:** Starts, stops, and configures the shower.

2.  **The Object Pool (e.g., `MeteorPool`):**
    *   **Role:** An efficient factory for the individual objects. Instead of constantly `Instantiate`ing and `Destroy`ing objects (which causes garbage collection spikes and performance issues), it recycles them.
    *   **Provision:** Provides "ready-to-use" objects when requested by the Manager.
    *   **Recycling:** Accepts objects back when they are no longer needed, deactivating them and holding them for future use.

3.  **The Product (e.g., `Meteor`):**
    *   **Role:** The individual game object that is spawned and managed by the system. It encapsulates its own unique behavior, appearance, and lifecycle (e.g., movement, collision, self-destruction, or return to pool).
    *   **Self-Contained:** It should be able to function mostly independently once activated and configured by the Manager.
    *   **Pool-Aware:** It often includes a mechanism to notify the pool when it's ready to be recycled.

---

### **Benefits of this Pattern**

*   **Performance:** Object pooling drastically reduces garbage collection overhead and CPU spikes associated with frequent object creation/destruction, crucial for real-time games with many transient objects.
*   **Modularity:** Each component has a clear, single responsibility, making the system easier to understand, maintain, and extend.
*   **Flexibility:** You can easily change shower parameters (spawn rate, object properties, duration) via the Manager without touching the individual object logic or the pooling mechanism. You can also swap out different "Product" prefabs or introduce new pooling strategies.
*   **Scalability:** Handles a large number of objects efficiently without complex state management in a single script.

---

### **Complete C# Unity Example**

This example will create a system that spawns meteors from the top of the screen, moving downwards, and recycling them when they go out of bounds.

**Setup in Unity:**

1.  Create a new Unity project.
2.  Create an empty GameObject in your scene named `MeteorShowerSystem`.
3.  Create three C# scripts: `Meteor.cs`, `MeteorPool.cs`, and `MeteorShowerManager.cs`.
4.  Attach `MeteorPool.cs` and `MeteorShowerManager.cs` to the `MeteorShowerSystem` GameObject.
5.  Create a simple 3D Cube (or Sphere) GameObject. Rename it `MeteorPrefab`.
    *   Add a `Rigidbody` component to it (uncheck "Use Gravity" and check "Is Kinematic" as movement will be handled by script).
    *   Attach `Meteor.cs` to the `MeteorPrefab`.
    *   Make it a Prefab by dragging it from the Hierarchy into your Project window.
    *   Delete the `MeteorPrefab` from the Hierarchy.
6.  Assign the `MeteorPrefab` to the `Meteor Pool` script's `Meteor Prefab` field in the Inspector.
7.  Configure `MeteorShowerManager` parameters in the Inspector (e.g., `Meteor Pool`, `Spawn Area`, `Shower Duration`, etc.).
8.  Run the scene!

---

### **1. `Meteor.cs` (The Product)**

This script defines the behavior of an individual meteor.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// Meteor (Product): Represents an individual meteor object.
/// Encapsulates its movement, appearance, and lifetime logic.
/// It's designed to be recycled by an Object Pool.
/// </summary>
public class Meteor : MonoBehaviour
{
    // Public fields to be set by the MeteorShowerManager via the pool
    [HideInInspector] public float speed;
    [HideInInspector] public float lifetime; // How long before it's returned to the pool
    [HideInInspector] public Color color;
    [HideInInspector] public float size;

    // Private references for internal use
    private Rigidbody rb;
    private Renderer meteorRenderer;
    private Vector3 initialScale; // Store the original scale for resizing

    private MeteorPool parentPool; // Reference to the pool that spawned it

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Resets necessary state for reuse from the pool.
    /// </summary>
    void OnEnable()
    {
        // Start the self-destruction countdown if a lifetime is set
        if (lifetime > 0)
        {
            StartCoroutine(ReturnToPoolAfterDelay(lifetime));
        }
    }

    /// <summary>
    /// Called when the GameObject is disabled or destroyed.
    /// Cleans up any active coroutines or states.
    /// </summary>
    void OnDisable()
    {
        // Stop any active coroutines to prevent issues when returned to pool
        StopAllCoroutines();
    }

    /// <summary>
    /// Initializes the meteor with specific properties.
    /// This is called by the MeteorPool when a meteor is retrieved.
    /// </summary>
    /// <param name="pool">The MeteorPool instance that created this meteor.</param>
    /// <param name="initialSpeed">The movement speed of the meteor.</param>
    /// <param name="initialSize">The scale multiplier for the meteor.</param>
    /// <param name="initialColor">The color of the meteor's material.</param>
    /// <param name="initialLifetime">How long the meteor will exist before returning to the pool.</param>
    public void Setup(MeteorPool pool, float initialSpeed, float initialSize, Color initialColor, float initialLifetime)
    {
        parentPool = pool;
        speed = initialSpeed;
        size = initialSize;
        color = initialColor;
        lifetime = initialLifetime;

        // Ensure components are cached
        CacheComponents();

        // Apply scale and color
        if (initialScale == Vector3.zero) initialScale = transform.localScale; // Capture initial scale once
        transform.localScale = initialScale * size;

        if (meteorRenderer != null)
        {
            // Use MaterialPropertyBlock for performance to avoid creating new materials
            // for each instance if using the same shared material.
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            meteorRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            meteorRenderer.SetPropertyBlock(propBlock);
        }
    }

    /// <summary>
    /// Caches component references for performance.
    /// </summary>
    private void CacheComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) Debug.LogWarning("Meteor Rigidbody not found!");
        }
        if (meteorRenderer == null)
        {
            meteorRenderer = GetComponent<Renderer>();
            if (meteorRenderer == null) Debug.LogWarning("Meteor Renderer not found!");
        }
    }

    /// <summary>
    /// Called once per frame. Handles the meteor's movement.
    /// </summary>
    void Update()
    {
        // Move the meteor downwards using its speed
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);

        // Optional: If the meteor goes too far off-screen downwards, return it to the pool
        // This acts as a backup for the lifetime-based return.
        // Adjust the Y threshold based on your game camera and scene setup.
        if (transform.position.y < -15f) // Example threshold
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Coroutine to return the meteor to the pool after a specified delay.
    /// </summary>
    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    /// <summary>
    /// Returns this meteor instance back to its parent pool.
    /// </summary>
    public void ReturnToPool()
    {
        if (parentPool != null)
        {
            parentPool.ReturnMeteor(this);
        }
        else
        {
            // Fallback: If no pool reference, destroy the object (not ideal for pooling)
            Debug.LogWarning("Meteor has no parent pool reference, destroying object instead of recycling!");
            Destroy(gameObject);
        }
    }

    // Optional: Add collision detection logic here
    /*
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Meteor hit {collision.gameObject.name}");
        // Example: If it hits something, return to pool immediately
        ReturnToPool();
    }
    */
}
```

---

### **2. `MeteorPool.cs` (The Object Pool)**

This script manages the creation and recycling of `Meteor` objects.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List and Queue

/// <summary>
/// MeteorPool (Object Pool): Manages the efficient creation and recycling
/// of Meteor objects. It prevents constant instantiation/destruction,
/// reducing garbage collection and improving performance.
/// </summary>
public class MeteorPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [Tooltip("The prefab of the Meteor object to be pooled.")]
    [SerializeField] private Meteor meteorPrefab;
    [Tooltip("The initial number of meteors to pre-instantiate in the pool.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("The maximum number of meteors allowed in the pool. If exceeded, extra meteors are destroyed.")]
    [SerializeField] private int maxPoolSize = 50;

    // A queue to store inactive (available) meteors. FIFO order.
    private Queue<Meteor> availableMeteors = new Queue<Meteor>();
    // A list to track all meteors (active and inactive) in the pool.
    private List<Meteor> allMeteors = new List<Meteor>();

    // Property to get the number of currently active meteors
    public int ActiveMeteorCount => allMeteors.Count - availableMeteors.Count;
    // Property to get the total number of meteors managed by the pool
    public int TotalMeteorCount => allMeteors.Count;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the pool by pre-instantiating meteors.
    /// </summary>
    void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// Pre-instantiates the initial set of meteors and adds them to the pool.
    /// </summary>
    private void InitializePool()
    {
        // Create an empty parent GameObject for scene hierarchy organization
        GameObject poolParent = new GameObject("MeteorPool_Parent");
        poolParent.transform.SetParent(this.transform); // Parent under the MeteorShowerSystem

        for (int i = 0; i < initialPoolSize; i++)
        {
            Meteor newMeteor = Instantiate(meteorPrefab, poolParent.transform);
            newMeteor.gameObject.SetActive(false); // Make it inactive initially
            availableMeteors.Enqueue(newMeteor);
            allMeteors.Add(newMeteor);
        }
        Debug.Log($"Initialized MeteorPool with {initialPoolSize} meteors.");
    }

    /// <summary>
    /// Retrieves an available Meteor from the pool. If none are available,
    /// it creates a new one (up to maxPoolSize).
    /// </summary>
    /// <returns>An active Meteor instance ready for use.</returns>
    public Meteor GetMeteor()
    {
        Meteor meteorToUse;

        if (availableMeteors.Count > 0)
        {
            meteorToUse = availableMeteors.Dequeue();
        }
        else
        {
            // If the pool needs to grow, check against maxPoolSize
            if (allMeteors.Count < maxPoolSize)
            {
                // Instantiate a new one if no available and pool is not full
                GameObject poolParent = transform.Find("MeteorPool_Parent")?.gameObject;
                if (poolParent == null) poolParent = new GameObject("MeteorPool_Parent"); // Fallback
                
                meteorToUse = Instantiate(meteorPrefab, poolParent.transform);
                allMeteors.Add(meteorToUse);
                Debug.Log($"Pool grew: Instantiated new meteor. Total: {allMeteors.Count}");
            }
            else
            {
                // If max pool size is reached, return null or an error.
                // For a shower, you might want to log a warning and skip spawning.
                Debug.LogWarning("MeteorPool is at max capacity! Cannot get more meteors.");
                return null;
            }
        }

        meteorToUse.gameObject.SetActive(true); // Activate the meteor
        return meteorToUse;
    }

    /// <summary>
    /// Returns a Meteor instance to the pool, making it available for reuse.
    /// </summary>
    /// <param name="meteor">The Meteor object to return.</param>
    public void ReturnMeteor(Meteor meteor)
    {
        if (meteor == null) return;

        // Ensure the meteor belongs to this pool (optional check)
        if (!allMeteors.Contains(meteor))
        {
            Debug.LogWarning("Attempted to return a meteor not managed by this pool. Destroying it.", meteor.gameObject);
            Destroy(meteor.gameObject);
            return;
        }

        // If the pool is already at max size and we're returning an overflow meteor, destroy it.
        // This handles cases where the pool grew beyond initialPoolSize temporarily.
        if (availableMeteors.Count >= maxPoolSize && allMeteors.Count > initialPoolSize)
        {
             // This logic needs to be careful: We only want to destroy if the total count
             // is above the target `initialPoolSize` and we have enough available.
             // Or, more simply, just let `allMeteors` track what was actually instantiated,
             // and only destroy if `availableMeteors.Count` is high and total `allMeteors.Count` is too high.
            if (allMeteors.Count > initialPoolSize) // Can be optimized based on specific policy
            {
                Debug.Log($"Destroying surplus meteor as pool is full ({availableMeteors.Count} available) and total count ({allMeteors.Count}) is above initial size.");
                allMeteors.Remove(meteor);
                Destroy(meteor.gameObject);
                return;
            }
        }
        
        meteor.gameObject.SetActive(false); // Deactivate the meteor
        availableMeteors.Enqueue(meteor); // Add it back to the queue
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Cleans up all pooled meteors.
    /// </summary>
    void OnDestroy()
    {
        // Clean up all instantiated meteors to prevent memory leaks in editor
        foreach (Meteor meteor in allMeteors)
        {
            if (meteor != null && meteor.gameObject != null)
            {
                Destroy(meteor.gameObject);
            }
        }
        allMeteors.Clear();
        availableMeteors.Clear();
        Debug.Log("MeteorPool destroyed and all meteors cleaned up.");
    }
}
```

---

### **3. `MeteorShowerManager.cs` (The Manager/Orchestrator)**

This script controls the overall meteor shower event.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for Lists (e.g., colors)

/// <summary>
/// MeteorShowerManager (Manager/Orchestrator): The central component of the MeteorShowerSystem.
/// It orchestrates when and where meteors are spawned, and with what properties,
/// by requesting them from the MeteorPool.
/// </summary>
public class MeteorShowerManager : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Reference to the MeteorPool responsible for providing meteor objects.")]
    [SerializeField] private MeteorPool meteorPool;

    [Header("Shower Parameters")]
    [Tooltip("Determines if the shower should start automatically on game start.")]
    [SerializeField] private bool autoStartShower = true;
    [Tooltip("Delay in seconds before the shower begins after auto-starting.")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("Total duration of the meteor shower in seconds. Set to 0 for infinite.")]
    [SerializeField] private float showerDuration = 30f;
    [Tooltip("Minimum time (seconds) between meteor spawns.")]
    [SerializeField] private float minSpawnInterval = 0.1f;
    [Tooltip("Maximum time (seconds) between meteor spawns.")]
    [SerializeField] private float maxSpawnInterval = 0.5f;

    [Header("Meteor Properties")]
    [Tooltip("Minimum speed for spawned meteors.")]
    [SerializeField] private float minMeteorSpeed = 5f;
    [Tooltip("Maximum speed for spawned meteors.")]
    [SerializeField] private float maxMeteorSpeed = 15f;
    [Tooltip("Minimum scale multiplier for spawned meteors.")]
    [SerializeField] private float minMeteorSize = 0.5f;
    [Tooltip("Maximum scale multiplier for spawned meteors.")]
    [SerializeField] private float maxMeteorSize = 2.0f;
    [Tooltip("List of possible colors for meteors. One will be chosen randomly.")]
    [SerializeField] private List<Color> meteorColors = new List<Color>()
    {
        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan
    };
    [Tooltip("How long each meteor will live before being returned to the pool, regardless of leaving screen.")]
    [SerializeField] private float meteorLifetime = 10f;


    [Header("Spawn Area (Gizmo visual in Scene View)")]
    [Tooltip("The X-axis range for meteor spawning (left to right).")]
    [SerializeField] private float spawnXMin = -10f;
    [Tooltip("The X-axis range for meteor spawning (left to right).")]
    [SerializeField] private float spawnXMax = 10f;
    [Tooltip("The Y-axis position where meteors will start spawning.")]
    [SerializeField] private float spawnY = 10f;
    [Tooltip("The Z-axis position for meteors (usually 0 in 2D or fixed in 3D).")]
    [SerializeField] private float spawnZ = 0f;

    private Coroutine showerCoroutine; // To hold a reference to the active shower coroutine
    private bool isShowerActive = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Performs initial setup and starts the shower if auto-start is enabled.
    /// </summary>
    void Awake()
    {
        if (meteorPool == null)
        {
            Debug.LogError("MeteorPool reference is missing in MeteorShowerManager!", this);
            enabled = false; // Disable the manager if the pool is not set
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    void Start()
    {
        if (autoStartShower)
        {
            Invoke(nameof(StartShower), startDelay);
        }
    }

    /// <summary>
    /// Initiates the meteor shower.
    /// </summary>
    public void StartShower()
    {
        if (!isShowerActive && meteorPool != null)
        {
            Debug.Log("Starting Meteor Shower...");
            isShowerActive = true;
            showerCoroutine = StartCoroutine(ShowerRoutine());
        }
        else if (isShowerActive)
        {
            Debug.LogWarning("Meteor Shower is already active!");
        }
        else if (meteorPool == null)
        {
            Debug.LogError("Cannot start shower: MeteorPool is not assigned.");
        }
    }

    /// <summary>
    /// Stops the meteor shower.
    /// </summary>
    public void StopShower()
    {
        if (isShowerActive && showerCoroutine != null)
        {
            Debug.Log("Stopping Meteor Shower.");
            StopCoroutine(showerCoroutine);
            isShowerActive = false;
        }
        else if (!isShowerActive)
        {
            Debug.LogWarning("Meteor Shower is not active, nothing to stop.");
        }
    }

    /// <summary>
    /// The main coroutine responsible for spawning meteors periodically.
    /// </summary>
    private IEnumerator ShowerRoutine()
    {
        float startTime = Time.time;

        while (isShowerActive && (showerDuration <= 0 || Time.time < startTime + showerDuration))
        {
            // Get a meteor from the pool
            Meteor meteor = meteorPool.GetMeteor();

            if (meteor != null)
            {
                // Randomize meteor properties
                float randomX = Random.Range(spawnXMin, spawnXMax);
                Vector3 spawnPosition = new Vector3(randomX, spawnY, spawnZ);
                float randomSpeed = Random.Range(minMeteorSpeed, maxMeteorSpeed);
                float randomSize = Random.Range(minMeteorSize, maxMeteorSize);
                Color randomColor = meteorColors[Random.Range(0, meteorColors.Count)];

                // Set its position and setup its properties
                meteor.transform.position = spawnPosition;
                // Keep meteors upright, or apply a random rotation for aesthetic variety
                meteor.transform.rotation = Quaternion.identity; // or Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                meteor.Setup(meteorPool, randomSpeed, randomSize, randomColor, meteorLifetime);
            }

            // Wait for a random interval before spawning the next meteor
            float randomInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(randomInterval);
        }

        // After the loop finishes (either duration ended or manually stopped)
        isShowerActive = false;
        Debug.Log("Meteor Shower finished.");
    }

    /// <summary>
    /// Draws gizmos in the Scene view to visualize the spawn area.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 spawnStart = new Vector3(spawnXMin, spawnY, spawnZ);
        Vector3 spawnEnd = new Vector3(spawnXMax, spawnY, spawnZ);
        Gizmos.DrawLine(spawnStart, spawnEnd);
        Gizmos.DrawWireCube((spawnStart + spawnEnd) / 2f, new Vector3(spawnXMax - spawnXMin, 0.1f, 0.1f));

        // Draw a line to show direction of movement (e.g., 5 units down)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(spawnStart, spawnStart + Vector3.down * 5);
        Gizmos.DrawLine(spawnEnd, spawnEnd + Vector3.down * 5);
        Gizmos.DrawLine(spawnStart + Vector3.down * 5, spawnEnd + Vector3.down * 5);
    }
}
```

---

### **How to Implement in a Real Project (Example Usage)**

1.  **Scene Setup:**
    *   Create an empty GameObject named `GameManager` (or `Systems`).
    *   Attach `MeteorPool.cs` and `MeteorShowerManager.cs` to it.

2.  **Prefab Creation:**
    *   Create a simple 3D Cube (or Sphere).
    *   Rename it `Meteor_Prefab`.
    *   Add a `Rigidbody` component. Uncheck `Use Gravity`, check `Is Kinematic` (because we control movement via `transform.Translate`).
    *   Add a `BoxCollider` (or `SphereCollider`).
    *   Attach the `Meteor.cs` script to it.
    *   Drag this GameObject from the Hierarchy into your Project window to create a Prefab.
    *   Delete the `Meteor_Prefab` from the Hierarchy.

3.  **Configure in Inspector:**
    *   Select your `GameManager` object.
    *   **Meteor Pool Script:** Drag your `Meteor_Prefab` from the Project window into the `Meteor Prefab` slot. Adjust `Initial Pool Size` and `Max Pool Size` based on your expected density.
    *   **Meteor Shower Manager Script:**
        *   Drag the `Meteor Pool` component from the `GameManager` itself into the `Meteor Pool` slot.
        *   Configure `Auto Start Shower`, `Start Delay`, `Shower Duration` (0 for infinite).
        *   Adjust `Min/Max Spawn Interval` for shower intensity.
        *   Set `Min/Max Meteor Speed` and `Min/Max Meteor Size`.
        *   Add desired `Meteor Colors`.
        *   Define the `Spawn Area` (`spawnXMin`, `spawnXMax`, `spawnY`, `spawnZ`). Use the Gizmos in the Scene view to visualize this.

4.  **Runtime Control (Optional):**
    You can trigger the shower programmatically from another script:

    ```csharp
    using UnityEngine;

    public class GameController : MonoBehaviour
    {
        public MeteorShowerManager showerManager;

        void Start()
        {
            // Assuming showerManager is assigned in the Inspector or found at runtime
            if (showerManager != null && !showerManager.autoStartShower)
            {
                // Start the shower after 5 seconds if not auto-started
                Invoke("TriggerShower", 5f);
            }
        }

        void TriggerShower()
        {
            if (showerManager != null)
            {
                showerManager.StartShower();
                // Schedule to stop the shower after 20 seconds
                Invoke("StopTheShower", 25f);
            }
        }

        void StopTheShower()
        {
            if (showerManager != null)
            {
                showerManager.StopShower();
            }
        }
    }
    ```

This complete example provides a robust, performant, and flexible system for managing dynamic object generation, illustrating the power of combining design patterns for practical game development challenges.