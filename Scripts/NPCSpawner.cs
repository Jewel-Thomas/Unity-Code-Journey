// Unity Design Pattern Example: NPCSpawner
// This script demonstrates the NPCSpawner pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'NPCSpawner' design pattern, often coupled with **Object Pooling**, is a crucial pattern in Unity game development for managing game entities like enemies, friendly NPCs, or environmental objects. It centralizes the logic for creating, recycling, and tracking these objects, significantly improving performance and organization.

Here's how it addresses common development challenges:

1.  **Performance:** Repeatedly calling `Instantiate()` (to create new objects) and `Destroy()` (to remove them) at runtime can be very costly, leading to performance spikes and garbage collection overhead. Object pooling mitigates this by reusing inactive objects rather than destroying and recreating them.
2.  **Organization:** All NPC creation and management logic resides in one place, making the codebase cleaner and easier to maintain.
3.  **Control:** The spawner can enforce limits on the number of active NPCs, control their initial states, and manage their hierarchy in the scene.

This example provides two C# scripts:
*   `NPC.cs`: A simple script representing an NPC, capable of being initialized and returning itself to the pool.
*   `NPCSpawner.cs`: The core implementation of the spawner pattern, including object pooling.

---

### 1. `NPC.cs` - The Basic NPC Representation

This script defines the fundamental behavior and properties of an NPC. Importantly, it holds a reference to its spawner to facilitate returning itself to the object pool when it's no longer needed.

```csharp
using UnityEngine;

/// <summary>
/// Represents a basic Non-Player Character (NPC) in the game.
/// This script demonstrates how an NPC can be initialized by the spawner
/// and how it can signal its return to the object pool.
/// </summary>
public class NPC : MonoBehaviour
{
    // A private reference to its spawner so the NPC can return itself to the pool.
    // This is passed during initialization.
    private NPCSpawner spawner;
    private string npcName;
    private int currentHealth;

    // For demonstration: NPC will automatically despawn after a set lifetime.
    private float despawnTimer = 0f;
    private float lifeTime = 5f; // NPC will return to pool after 5 seconds for this demo.

    /// <summary>
    /// Initializes the NPC with its spawner, name, and health.
    /// This method is called by the NPCSpawner immediately after activating the NPC.
    /// </summary>
    /// <param name="parentSpawner">The NPCSpawner that created this NPC.</param>
    /// <param name="name">The name to assign to this NPC.</param>
    /// <param name="health">The initial health of the NPC.</param>
    public void Initialize(NPCSpawner parentSpawner, string name, int health)
    {
        spawner = parentSpawner;
        npcName = name;
        currentHealth = health;
        Debug.Log($"NPC '{npcName}' spawned with {currentHealth} health at {transform.position}.");
        
        // Reset any state specific to the NPC when it's initialized for reuse.
        // For example, if it had an animation state, a target, etc.
        despawnTimer = 0f; // Reset the despawn timer.
        gameObject.name = name; // Update GameObject name for clarity in Hierarchy.
    }

    /// <summary>
    /// Simulates the NPC taking damage. If health drops to zero, the NPC dies.
    /// </summary>
    /// <param name="damage">The amount of damage to inflict.</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"{npcName} took {damage} damage. Health: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the NPC's death sequence.
    /// In a real game, this might involve playing animations, dropping loot, etc.
    /// Finally, it signals the spawner to return the NPC to the pool.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{npcName} has died.");
        // After death sequence, return to pool.
        ReturnToPool();
    }

    /// <summary>
    /// Tells the owning NPCSpawner to return this NPC to its object pool.
    /// This is the standard way for an NPC to "despawn" itself.
    /// </summary>
    public void ReturnToPool()
    {
        if (spawner != null)
        {
            spawner.DespawnNPC(gameObject);
        }
        else
        {
            // Fallback: If for some reason there's no spawner reference (e.g., NPC was
            // instantiated manually without the spawner), destroy it directly.
            Debug.LogWarning($"NPC '{npcName}' has no spawner reference. Destroying GameObject directly.", this);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Used here to reset the despawn timer for demo purposes.
    /// </summary>
    void OnEnable()
    {
        despawnTimer = 0f; // Reset timer when enabled
    }

    /// <summary>
    /// Called once per frame. Used here to implement the automatic despawn after a lifetime.
    /// </summary>
    void Update()
    {
        if (gameObject.activeSelf) // Only update if active
        {
            despawnTimer += Time.deltaTime;
            if (despawnTimer >= lifeTime)
            {
                Debug.Log($"{npcName} lifetime expired. Returning to pool.");
                ReturnToPool();
            }
        }
    }
}
```

---

### 2. `NPCSpawner.cs` - The NPC Spawner with Object Pooling

This script is the core of the NPCSpawner pattern. It manages a pool of NPC GameObjects, handling their creation, activation, deactivation, and reuse.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Queue and List

/// <summary>
/// The NPCSpawner demonstrates the 'Spawner' design pattern, specifically with object pooling
/// for performance. It centralizes the logic for creating, managing, and recycling NPC game objects.
/// </summary>
/// <remarks>
/// This pattern is highly practical in games for:
/// 1.  **Performance:** Avoiding costly `Instantiate` and `Destroy` operations during gameplay
///     by reusing objects (Object Pooling).
/// 2.  **Organization:** Centralizing NPC creation logic (e.g., initialization, random positioning).
/// 3.  **Control:** Managing the number of active NPCs, their parentage, and their lifecycle.
/// </remarks>
public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Prefab")]
    [Tooltip("The GameObject prefab that represents the NPC to be spawned.")]
    [SerializeField]
    private GameObject npcPrefab;

    [Header("Pooling Settings")]
    [Tooltip("The initial number of NPCs to create and store in the pool when the game starts.")]
    [SerializeField]
    private int poolSize = 10;
    [Tooltip("If true, the spawner will create new NPCs if the pool runs out. If false, it will log a warning.")]
    [SerializeField]
    private bool allowDynamicPooling = true;

    [Header("Spawn Area & Parent")]
    [Tooltip("The parent Transform under which spawned NPCs will be organized in the Hierarchy.")]
    [SerializeField]
    private Transform spawnParent;
    [Tooltip("The minimum bounds for random NPC spawn positions.")]
    [SerializeField]
    private Vector3 spawnAreaMin = new Vector3(-5f, 0f, -5f);
    [Tooltip("The maximum bounds for random NPC spawn positions.")]
    [SerializeField]
    private Vector3 spawnAreaMax = new Vector3(5f, 0f, 5f);

    [Header("Auto-Spawn Settings (for demonstration)")]
    [Tooltip("If true, the spawner will automatically spawn NPCs at regular intervals.")]
    [SerializeField]
    private bool autoSpawnEnabled = true;
    [Tooltip("The time interval (in seconds) between automatic NPC spawns.")]
    [SerializeField]
    private float spawnInterval = 2f;
    [Tooltip("The maximum number of NPCs that can be active simultaneously when auto-spawning.")]
    [SerializeField]
    private int maxActiveNPCs = 5;

    // --- Private Members ---
    private Queue<GameObject> npcPool;      // Stores inactive NPCs ready for reuse (the pool)
    private List<GameObject> activeNPCs;    // Stores currently active NPCs for tracking
    private float lastSpawnTime;            // Used for tracking auto-spawn intervals

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the object pool and performs initial setup.
    /// </summary>
    void Awake()
    {
        // --- Input Validation ---
        if (npcPrefab == null)
        {
            Debug.LogError("NPCSpawner: NPC Prefab is not assigned! Please assign a prefab in the Inspector.", this);
            enabled = false; // Disable the spawner if no prefab is set
            return;
        }
        // Ensure the prefab has the NPC script required for initialization and despawning.
        if (npcPrefab.GetComponent<NPC>() == null)
        {
            Debug.LogError("NPCSpawner: The assigned NPC Prefab does not have an 'NPC' script component. " +
                           "Please ensure your prefab has the required script.", this);
            enabled = false;
            return;
        }

        // Initialize the pool and active list
        npcPool = new Queue<GameObject>();
        activeNPCs = new List<GameObject>();

        // If no specific spawn parent is set, use this spawner's transform as the default.
        // This helps keep the Hierarchy clean by grouping spawned NPCs.
        if (spawnParent == null)
        {
            spawnParent = this.transform;
        }

        // Pre-populate the object pool with initial inactive NPCs.
        PrepopulatePool();

        // Initialize last spawn time for auto-spawning mechanism.
        lastSpawnTime = Time.time;
    }

    /// <summary>
    /// Creates the initial set of NPCs based on 'poolSize' and adds them to the pool,
    /// setting them inactive (ready for reuse).
    /// </summary>
    private void PrepopulatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject npc = CreateNewNPC();
            if (npc != null)
            {
                npc.SetActive(false); // Make it inactive initially
                npcPool.Enqueue(npc); // Add to the pool
            }
        }
        Debug.Log($"NPCSpawner: Pre-populated pool with {npcPool.Count} NPCs.");
    }

    /// <summary>
    /// Creates a brand new NPC GameObject instance from the assigned prefab.
    /// This method is called when pre-populating the pool or when dynamic pooling is needed.
    /// </summary>
    /// <returns>The newly created NPC GameObject, or null if the prefab is missing.</returns>
    private GameObject CreateNewNPC()
    {
        if (npcPrefab == null)
        {
            Debug.LogError("NPCSpawner: Cannot create new NPC, prefab is null.");
            return null;
        }

        // Instantiate the prefab, parent it under 'spawnParent' for organization,
        // and name it for clarity in the Hierarchy.
        GameObject newNPC = Instantiate(npcPrefab, spawnParent);
        newNPC.name = $"NPC_Pooled_{npcPool.Count + activeNPCs.Count + 1}";
        return newNPC;
    }

    /// <summary>
    /// Called once per frame. This method handles automatic NPC spawning if enabled,
    /// based on the 'spawnInterval' and 'maxActiveNPCs' settings.
    /// </summary>
    void Update()
    {
        if (autoSpawnEnabled && Time.time >= lastSpawnTime + spawnInterval)
        {
            // Only spawn if we haven't reached the maximum allowed active NPCs.
            if (activeNPCs.Count < maxActiveNPCs)
            {
                // Spawn at a random position within the defined spawn area.
                Vector3 randomPosition = GetRandomSpawnPosition();
                // We provide a generic name for auto-spawned NPCs in this demo.
                SpawnNPC(randomPosition, Quaternion.identity, "Auto-Spawned NPC");
            }
            // else { Debug.Log("NPCSpawner: Max active NPCs reached. Waiting for NPCs to despawn."); }
            
            lastSpawnTime = Time.time; // Reset the timer for the next spawn.
        }
    }

    /// <summary>
    /// Calculates a random position within the defined 'spawnAreaMin' and 'spawnAreaMax' bounds.
    /// </summary>
    /// <returns>A random Vector3 position within the spawn area.</returns>
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomY = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        float randomZ = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
        return new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// Requests an NPC from the spawner. This is the primary public method for other scripts
    /// to get an NPC instance. It retrieves an NPC from the pool (or creates a new one),
    /// positions it, activates it, and initializes its 'NPC' script.
    /// </summary>
    /// <param name="position">The world position where the NPC should be spawned.</param>
    /// <param name="rotation">The world rotation to apply to the NPC.</param>
    /// <param name="nameOverride">Optional: A custom name for the spawned NPC GameObject.</param>
    /// <param name="initialHealth">Optional: The initial health for the spawned NPC.</param>
    /// <returns>The spawned and initialized NPC GameObject, or null if none could be spawned.</returns>
    public GameObject SpawnNPC(Vector3 position, Quaternion rotation, string nameOverride = "NPC", int initialHealth = 100)
    {
        GameObject npcToSpawn = GetNPCFromPool(); // Try to get an NPC from the pool first.

        if (npcToSpawn != null)
        {
            // Set the NPC's position and rotation, then activate it.
            npcToSpawn.transform.position = position;
            npcToSpawn.transform.rotation = rotation;
            npcToSpawn.SetActive(true);

            // Get the NPC script component and initialize it.
            // This is crucial for setting up the NPC's state and linking it back to this spawner.
            NPC npcScript = npcToSpawn.GetComponent<NPC>();
            if (npcScript != null)
            {
                npcScript.Initialize(this, nameOverride, initialHealth);
            }
            else
            {
                Debug.LogWarning($"NPCSpawner: Spawned object '{npcToSpawn.name}' does not have an NPC script. " +
                                 "Cannot call Initialize method.", npcToSpawn);
            }

            // Add to the list of currently active NPCs for tracking.
            activeNPCs.Add(npcToSpawn);
            return npcToSpawn;
        }

        Debug.LogWarning("NPCSpawner: Could not spawn NPC. Pool is empty and dynamic pooling is disabled.", this);
        return null;
    }

    /// <summary>
    /// Retrieves an inactive NPC GameObject from the pool. If the pool is empty,
    /// it either creates a new one (if 'allowDynamicPooling' is true) or returns null.
    /// </summary>
    /// <returns>An inactive NPC GameObject ready for activation, or null if none available.</returns>
    private GameObject GetNPCFromPool()
    {
        if (npcPool.Count > 0)
        {
            return npcPool.Dequeue(); // Return the next available NPC from the queue.
        }
        else if (allowDynamicPooling)
        {
            // If pool is empty but dynamic pooling is allowed, create a new one.
            Debug.LogWarning("NPCSpawner: Pool exhausted. Creating a new NPC dynamically.", this);
            GameObject newNPC = CreateNewNPC();
            if (newNPC != null)
            {
                newNPC.SetActive(false); // Ensure it's inactive before being prepared for use.
            }
            return newNPC;
        }
        return null; // No NPCs available and dynamic pooling is off.
    }

    /// <summary>
    /// Returns an NPC GameObject to the pool, making it inactive and ready for reuse.
    /// This is the primary method for other scripts (or the NPC itself) to return an NPC.
    /// </summary>
    /// <param name="npcToReturn">The NPC GameObject to be returned to the pool.</param>
    public void DespawnNPC(GameObject npcToReturn)
    {
        if (npcToReturn == null)
        {
            Debug.LogWarning("NPCSpawner: Attempted to despawn a null NPC.");
            return;
        }

        // Check if the NPC is actually being managed by this spawner and is currently active.
        if (!activeNPCs.Contains(npcToReturn))
        {
            Debug.LogWarning($"NPCSpawner: Attempted to despawn an NPC ('{npcToReturn.name}') " +
                             $"that was not managed by this spawner or was already inactive. Destroying it instead.", npcToReturn);
            Destroy(npcToReturn); // Clean up unmanaged or already inactive NPCs.
            return;
        }

        // Remove from the list of active NPCs.
        activeNPCs.Remove(npcToReturn);

        // Reset its state (e.g., parent, position, rotation) and make it inactive.
        // It's good practice to reset transform properties before pooling.
        npcToReturn.transform.SetParent(spawnParent); // Re-parent to the pool parent (for organization).
        npcToReturn.transform.position = Vector3.zero; // Reset position
        npcToReturn.transform.rotation = Quaternion.identity; // Reset rotation
        npcToReturn.SetActive(false); // Deactivate the GameObject.

        // Add the NPC back to the pool for future reuse.
        npcPool.Enqueue(npcToReturn);
        Debug.Log($"NPCSpawner: NPC '{npcToReturn.name}' returned to pool. Pool size: {npcPool.Count}. Active NPCs: {activeNPCs.Count}");
    }

    /// <summary>
    /// Immediately despawns all currently active NPCs managed by this spawner
    /// and returns them to the pool.
    /// </summary>
    public void DespawnAllActiveNPCs()
    {
        // Create a temporary copy to iterate over, as the original list will be modified during despawning.
        GameObject[] activeCopy = activeNPCs.ToArray();
        foreach (GameObject npc in activeCopy)
        {
            DespawnNPC(npc); // Use the existing DespawnNPC method for each.
        }
        Debug.Log("NPCSpawner: All active NPCs have been despawned.");
    }
}
```

---

### How to Use This Example in Unity

Follow these steps to set up and run the NPCSpawner in your Unity project:

1.  **Create a New Unity Project** or open an existing one.

2.  **Create `NPC.cs` Script:**
    *   In the Project window, right-click -> Create -> C# Script.
    *   Name it `NPC`.
    *   Paste the content of the `NPC.cs` script (from above) into this new file, overwriting any default code.

3.  **Create `NPCSpawner.cs` Script:**
    *   In the Project window, right-click -> Create -> C# Script.
    *   Name it `NPCSpawner`.
    *   Paste the content of the `NPCSpawner.cs` script (from above) into this new file, overwriting any default code.

4.  **Create an `NPC Prefab`:**
    *   In the Hierarchy window, right-click -> 3D Object -> Cube (or Sphere, Capsule, etc.). Rename it to "NPC_Prefab".
    *   **Drag** "NPC_Prefab" from the Hierarchy into your Project window (e.g., create a "Prefabs" folder and drag it there) to create a Prefab Asset.
    *   You can now **delete** the "NPC_Prefab" from the Hierarchy; we only need the asset.
    *   Select the "NPC_Prefab" asset in your Project window.
    *   In the Inspector, click "Add Component" and search for `NPC`. Add the `NPC` script to it.
    *   *(Optional)* Add a Material to your prefab (e.g., a bright color) to easily distinguish spawned NPCs.

5.  **Create the `NPC Spawner` GameObject:**
    *   In the Hierarchy window, right-click -> Create Empty. Rename it to "NPC_Spawner".
    *   Select "NPC_Spawner" in the Hierarchy.
    *   In the Inspector, click "Add Component" and search for `NPCSpawner`. Add the script to it.

6.  **Configure the `NPC Spawner` in the Inspector:**
    *   **NPC Prefab:** Drag your "NPC_Prefab" asset from the Project window into this slot.
    *   **Pool Size:** Set an initial number (e.g., `10` or `20`). This is how many NPCs will be created at startup.
    *   **Allow Dynamic Pooling:** Keep `true` if you want new NPCs to be created if the pool runs out.
    *   **Spawn Parent:** Leave empty to automatically parent spawned NPCs under the "NPC_Spawner" GameObject, keeping your Hierarchy organized. Alternatively, create an empty GameObject (e.g., "ActiveNPCs") and assign it here.
    *   **Spawn Area Min/Max:** Adjust these `Vector3` values to define a visible area where NPCs will randomly appear (e.g., Min: `-10, 0, -10`, Max: `10, 0, 10`).
    *   **Auto-Spawn Enabled:** Keep `true` for continuous spawning.
    *   **Spawn Interval:** Set the time between spawns (e.g., `2` seconds).
    *   **Max Active NPCs:** Set the maximum number of NPCs that can be in the scene at one time (e.g., `5`).

7.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   You should see NPCs automatically spawning within your defined area at regular intervals.
    *   Observe the **Hierarchy:** Inactive NPCs will be nested under `NPC_Spawner`, and active ones will become visible. As NPCs despawn (after their 5-second lifetime, or when you manually despawn them), they will return to the inactive pool.
    *   Observe the **Console:** It will log when NPCs spawn, take damage (via their built-in timer), and return to the pool, demonstrating the pooling mechanism.

8.  **(Optional) Programmatic Interaction Example:**
    To see how another script might interact with the spawner (e.g., a `GameManager` or `InputHandler`):

    ```csharp
    // Create a new C# script (e.g., 'GameManager.cs') and attach it to an empty GameObject.
    // Assign your 'NPC_Spawner' GameObject to the 'mySpawner' field in the Inspector.
    
    using UnityEngine;

    public class GameManager : MonoBehaviour
    {
        [Tooltip("Assign the NPC_Spawner GameObject from your Hierarchy here.")]
        public NPCSpawner mySpawner; 

        void Update()
        {
            if (mySpawner == null) return;

            // Press 'S' to manually spawn an NPC at a specific location
            if (Input.GetKeyDown(KeyCode.S))
            {
                Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
                mySpawner.SpawnNPC(spawnPos, Quaternion.identity, "Manual NPC", 75);
                Debug.Log("Manually spawned an NPC!");
            }

            // Press 'A' to despawn all currently active NPCs
            if (Input.GetKeyDown(KeyCode.A))
            {
                mySpawner.DespawnAllActiveNPCs();
                Debug.Log("Manually despawned all active NPCs.");
            }
        }
    }
    ```
    Attach this `GameManager.cs` to any GameObject in your scene (e.g., an empty "GameManager" object). Drag your "NPC_Spawner" from the Hierarchy to the `My Spawner` slot in the `GameManager` component's Inspector. Run the scene and use 'S' and 'A' keys to manually control spawning and despawning.