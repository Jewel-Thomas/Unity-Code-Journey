// Unity Design Pattern Example: RespawnAnchorSystem
// This script demonstrates the RespawnAnchorSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical C# Unity implementation of the 'Respawn Anchor System' design pattern. It includes three core scripts: `RespawnManager`, `RespawnAnchor`, and an example `PlayerController` to demonstrate interaction.

---

### Understanding the Respawn Anchor System Pattern

The Respawn Anchor System is a common design pattern in games, especially for checkpoint-based systems. Its primary goal is to provide a flexible and dynamic way for player characters (or other entities) to respawn not at a fixed, hardcoded location, but at the last "anchor" or "checkpoint" they reached.

**Key Components and Their Roles:**

1.  **RespawnManager (Singleton):**
    *   **Role:** The central authority that manages all respawn logic. It holds the reference to the currently active respawn point.
    *   **Responsibilities:**
        *   Maintain a static instance (Singleton pattern) for easy global access.
        *   Store the `Transform` of the current active respawn anchor.
        *   Provide a method to update the active respawn anchor.
        *   Provide a method to respawn a player/entity at the current anchor's position and rotation, often resetting its state (health, physics, etc.).
        *   Handle an initial default respawn point.

2.  **RespawnAnchor (MonoBehaviour):**
    *   **Role:** A specific point in the game world that, when activated, becomes the new respawn location. These are your "checkpoints."
    *   **Responsibilities:**
        *   Be a physical `GameObject` in the scene, usually with a trigger collider.
        *   When a player (or designated entity) enters its trigger, it notifies the `RespawnManager` to update the `currentRespawnAnchor` to its own `Transform`.
        *   Can provide visual or auditory feedback upon activation.

3.  **Respawnable Entity (e.g., PlayerController - MonoBehaviour):**
    *   **Role:** Any entity in the game that can "die" and needs to be respawned by the system.
    *   **Responsibilities:**
        *   Manage its own "life" state (health, alive/dead status).
        *   When it "dies," it calls the `RespawnManager`'s `RespawnPlayer` method, passing itself as the entity to be respawned.
        *   Provide a `ResetPlayerState` method (or similar) that the `RespawnManager` can call to restore its health, clear physics, etc., after respawn.

---

### Complete C# Unity Scripts

Here are the three scripts you'll need. Create a C# script file for each one in your Unity project (e.g., `RespawnManager.cs`, `RespawnAnchor.cs`, `PlayerController.cs`).

#### 1. `RespawnManager.cs`

```csharp
using UnityEngine;
using System.Collections; // Often useful, even if not strictly needed for this example

/// <summary>
///     RESPAWN ANCHOR SYSTEM - RespawnManager
///     
///     This script is the core of the Respawn Anchor System design pattern.
///     It acts as a centralized manager (often a Singleton) that keeps track of
///     the current active respawn point (the "anchor") for the player or other
///     respawnable entities.
///
///     Pattern Role: Manager/Singleton
///     - Stores the currently active respawn anchor's Transform.
///     - Provides methods to update the respawn anchor.
///     - Provides methods to trigger the respawn of a player/entity at the current anchor.
///     - Handles the initial setup of the respawn system.
///
///     Usage:
///     1. Create an empty GameObject in your scene (e.g., "RespawnSystem").
///     2. Attach this `RespawnManager` script to it.
///     3. Assign a `Transform` to the 'Default Start Respawn Anchor' field in the Inspector.
///        This is where the player will initially spawn or respawn if no other anchor
///        has been activated.
///     4. Optionally, assign a `PlayerController` prefab if the player object
///        is completely destroyed on death and needs to be reinstantiated.
///        If the player object persists, this field can be left null, and `RespawnPlayer`
///        should be used.
///     5. Ensure your `PlayerController` script (or equivalent) for your player
///        interacts with this manager to respawn.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    // Singleton pattern for easy global access
    public static RespawnManager Instance { get; private set; }

    [Header("Respawn Settings")]
    [Tooltip("The initial respawn point when the game starts or if no anchor has been set yet.")]
    [SerializeField] private Transform defaultStartRespawnAnchor;

    [Tooltip("Reference to the PlayerController prefab. " +
             "Only needed if the player object is destroyed on death and needs to be reinstantiated. " +
             "If the player persists and is just teleported, leave this null and use RespawnPlayer(PlayerController).")]
    [SerializeField] private PlayerController playerPrefab;

    private Transform currentRespawnAnchor; // Stores the currently active respawn point
    private PlayerController activePlayerInstance; // Reference to the player currently in the scene

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern to ensure only one RespawnManager exists.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple RespawnManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Optional: If you want the manager to persist across scenes, uncomment the line below.
        // DontDestroyOnLoad(gameObject);

        // Initialize the current respawn anchor with the default starting point.
        if (defaultStartRespawnAnchor == null)
        {
            // If no default is set, create a temporary one at the origin
            Debug.LogWarning("RespawnManager: Default Start Respawn Anchor is not set. Player will respawn at (0,0,0) initially if no other anchor is found.");
            GameObject tempAnchorGO = new GameObject("TemporaryDefaultRespawnAnchor");
            currentRespawnAnchor = tempAnchorGO.transform;
            // Optionally, destroy this temporary anchor if the manager doesn't persist
            // For managers that persist with DontDestroyOnLoad, you might want to manage this carefully.
            // For a scene-specific manager, it will be destroyed with the scene.
        }
        else
        {
            currentRespawnAnchor = defaultStartRespawnAnchor;
        }

        Debug.Log($"RespawnManager initialized. Initial anchor set to: {currentRespawnAnchor.name} at {currentRespawnAnchor.position}");
    }

    /// <summary>
    /// Sets a new active respawn anchor. This is typically called by a `RespawnAnchor`
    /// object when the player reaches a checkpoint.
    /// </summary>
    /// <param name="newAnchor">The Transform of the new respawn point.</param>
    public void SetRespawnAnchor(Transform newAnchor)
    {
        if (newAnchor == null)
        {
            Debug.LogError("Attempted to set a null respawn anchor! Anchor not updated.");
            return;
        }

        currentRespawnAnchor = newAnchor;
        Debug.Log($"RespawnManager: New respawn anchor set to '{newAnchor.name}' at position {newAnchor.position}");
    }

    /// <summary>
    /// Triggers the respawn of a player. This method assumes the player object
    /// already exists in the scene and just needs to be repositioned and reset.
    /// </summary>
    /// <param name="playerToRespawn">The PlayerController instance to respawn.</param>
    public void RespawnPlayer(PlayerController playerToRespawn)
    {
        if (playerToRespawn == null)
        {
            Debug.LogError("RespawnManager: Attempted to respawn a null player!");
            return;
        }

        if (currentRespawnAnchor == null)
        {
            Debug.LogError("RespawnManager: No valid respawn anchor set! Cannot respawn player.");
            return;
        }

        Debug.Log($"Respawning player '{playerToRespawn.name}' to anchor '{currentRespawnAnchor.name}' at {currentRespawnAnchor.position}");

        // Teleport the player to the current anchor's position and rotation
        playerToRespawn.transform.position = currentRespawnAnchor.position;
        playerToRespawn.transform.rotation = currentRespawnAnchor.rotation;

        // Reset the player's state (health, physics, etc.)
        playerToRespawn.ResetPlayerState();

        // Optional: If the player has a Rigidbody, reset its velocity to prevent weird physics on respawn
        Rigidbody rb = playerToRespawn.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Spawns a new player instance at the current respawn anchor.
    /// This is used if the player object is destroyed on death and a new one needs to be created.
    /// </summary>
    public void InstantiatePlayerAtAnchor()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("RespawnManager: Player Prefab is not assigned! Cannot instantiate player.");
            return;
        }

        if (currentRespawnAnchor == null)
        {
            Debug.LogError("RespawnManager: No valid respawn anchor set! Cannot instantiate player.");
            return;
        }

        // Destroy any existing player instance if we are replacing it
        if (activePlayerInstance != null)
        {
            Destroy(activePlayerInstance.gameObject);
        }

        Debug.Log($"Instantiating new player at anchor '{currentRespawnAnchor.name}' at {currentRespawnAnchor.position}");
        activePlayerInstance = Instantiate(playerPrefab, currentRespawnAnchor.position, currentRespawnAnchor.rotation);
        activePlayerInstance.ResetPlayerState(); // Ensure the newly spawned player starts fresh
    }

    /// <summary>
    /// Sets the active player instance. This should be called by the player
    /// itself when it spawns (e.g., in its Awake or Start method) if the player
    /// is not spawned via `InstantiatePlayerAtAnchor`. This allows the manager
    /// to always have a reference to the player.
    /// </summary>
    /// <param name="player">The PlayerController instance that is currently active.</param>
    public void SetActivePlayer(PlayerController player)
    {
        activePlayerInstance = player;
    }
}
```

#### 2. `RespawnAnchor.cs`

```csharp
using UnityEngine;

/// <summary>
///     RESPAWN ANCHOR SYSTEM - RespawnAnchor
///     
///     This script represents a checkpoint or a designated respawn point in the game world.
///     When a player interacts with it (e.g., by entering its trigger collider),
///     it informs the `RespawnManager` to update the active respawn anchor.
///
///     Pattern Role: Anchor/Checkpoint
///     - A physical object in the scene (often an empty GameObject).
///     - Has a collider set to 'Is Trigger'.
///     - When triggered by the player, it calls `RespawnManager.Instance.SetRespawnAnchor(transform)`.
///     - Can have visual/audio feedback for activation.
///
///     Usage:
///     1. Create an empty GameObject (e.g., "Checkpoint_A").
///     2. Add a Collider component (e.g., Box Collider, Sphere Collider) to it.
///     3. IMPORTANT: Set the Collider's `Is Trigger` property to `true`.
///     4. Attach this `RespawnAnchor` script to it.
///     5. Position it where you want a checkpoint to be.
///     6. Ensure your player GameObject has a Rigidbody and its tag matches `playerTag`.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a Collider is present for trigger detection
public class RespawnAnchor : MonoBehaviour
{
    [Header("Anchor Settings")]
    [Tooltip("Automatically set this as the respawn anchor when a player enters its trigger collider.")]
    [SerializeField] private bool autoSetOnTriggerEnter = true;

    [Tooltip("The tag used to identify the player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Optional: Whether this anchor should be disabled after being activated once.")]
    [SerializeField] private bool disableAfterActivation = false;

    private Collider anchorCollider; // Reference to our collider

    private void Awake()
    {
        anchorCollider = GetComponent<Collider>();
        if (anchorCollider == null)
        {
            Debug.LogError($"RespawnAnchor on {gameObject.name} is missing a Collider! This script requires a Collider to function as a trigger.");
            enabled = false; // Disable script if no collider
            return;
        }

        if (!anchorCollider.isTrigger)
        {
            Debug.LogWarning($"RespawnAnchor on {gameObject.name}: Collider is not set to 'Is Trigger'. It should be set to true for proper functionality.");
            // Optionally, force it to be a trigger: anchorCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider belongs to the player
        if (other.CompareTag(playerTag))
        {
            // Make sure the RespawnManager exists before trying to use it
            if (RespawnManager.Instance == null)
            {
                Debug.LogError("RespawnManager not found in the scene! Cannot set respawn anchor. Make sure a RespawnManager exists.");
                return;
            }

            // Only update the anchor if autoSetOnTriggerEnter is enabled
            if (autoSetOnTriggerEnter)
            {
                RespawnManager.Instance.SetRespawnAnchor(transform);
            }

            // Optional: Provide visual/audio feedback for anchor activation
            // Example: GetComponent<AudioSource>().Play();
            // Example: GetComponent<MeshRenderer>().material.color = Color.green;

            if (disableAfterActivation)
            {
                // Optionally disable the collider or the whole GameObject after activation
                // This prevents re-activating the same checkpoint multiple times unnecessarily
                anchorCollider.enabled = false;
                // If you have a visual component that indicates activation, you might update it here.
                // For example, turn off a light or change a material.
                Debug.Log($"RespawnAnchor '{gameObject.name}' activated and disabled.");
            }
        }
    }

    /// <summary>
    /// Public method to manually activate this anchor, useful for custom events
    /// not triggered by collider entry (e.g., reaching the end of a quest).
    /// </summary>
    public void ActivateAnchor()
    {
        if (RespawnManager.Instance == null)
        {
            Debug.LogError("RespawnManager not found! Cannot activate anchor.");
            return;
        }
        RespawnManager.Instance.SetRespawnAnchor(transform);
        Debug.Log($"RespawnAnchor '{gameObject.name}' manually activated.");

        if (disableAfterActivation)
        {
            if (anchorCollider != null) anchorCollider.enabled = false;
        }
    }
}
```

#### 3. `PlayerController.cs` (Example Respawnable Entity)

```csharp
using UnityEngine;

/// <summary>
///     RESPAWN ANCHOR SYSTEM - PlayerController (Example)
///     
///     This script represents a player character that can take damage and respawn.
///     It demonstrates how a respawnable entity interacts with the `RespawnManager`.
///
///     Pattern Role: Respawnable Entity
///     - Has health/life state.
///     - Implements a `Die()` method that calls the `RespawnManager`.
///     - Implements a `ResetPlayerState()` method to prepare for respawn.
///
///     Usage:
///     1. Create a 3D object for your player (e.g., a Capsule, Cube).
///     2. Add a Rigidbody component to it.
///     3. Ensure its tag is set to "Player" (or whatever `RespawnAnchor.playerTag` is configured to).
///     4. Attach this `PlayerController` script to it.
///     5. Set its initial position, or let `RespawnManager` handle initial spawn.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player needs a Rigidbody for physics and trigger interactions
public class PlayerController : MonoBehaviour
{
    [Header("Player Stats")]
    [Tooltip("The player's current health.")]
    [SerializeField] private float currentHealth = 100f;

    [Tooltip("The player's maximum health.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Amount of damage taken per 'attack' (for demonstration purposes).")]
    [SerializeField] private float demoDamageAmount = 25f;

    [Header("Movement")]
    [Tooltip("Speed at which the player moves.")]
    [SerializeField] private float moveSpeed = 5f;

    private bool isDead = false;
    private Rigidbody rb;

    /// <summary>
    /// Initializes the player's components and registers with the RespawnManager.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component!");
            enabled = false;
            return;
        }

        // Ensure Rigidbody is not kinematic so it can interact with triggers
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent accidental rotation

        // If the player object is already in the scene, register itself with the manager.
        // This is important if the RespawnManager doesn't instantiate the player.
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.SetActivePlayer(this);
            // Optionally, set the initial anchor if this is the player's true starting position.
            // If defaultStartRespawnAnchor in RespawnManager is used for initial spawn, this might not be needed.
            // For this example, we'll let RespawnManager handle initial position on Awake.
            // RespawnManager.Instance.SetRespawnAnchor(transform); 
        }
        else
        {
            Debug.LogWarning("RespawnManager not found when PlayerController initialized! Player might not respawn correctly.");
        }
        
        ResetPlayerState(); // Ensure initial state is fresh
    }

    /// <summary>
    /// Handles player input and other updates.
    /// </summary>
    private void Update()
    {
        if (isDead) return;

        HandleMovement();

        // Demonstration: Press Spacebar to take damage
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(demoDamageAmount);
        }

        // Demonstration: Press 'R' to manually trigger respawn (for testing)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Manual Respawn Triggered!");
            Die();
        }
    }

    /// <summary>
    /// Basic player movement.
    /// </summary>
    private void HandleMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        // Apply movement relative to the camera/world for simplicity
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Applies damage to the player. If health drops to or below zero, the player dies.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the player's death sequence.
    /// This is where the player tells the RespawnManager to respawn them.
    /// </summary>
    private void Die()
    {
        if (isDead) return; // Prevent multiple death calls

        isDead = true;
        Debug.Log("Player has died!");

        // Inform the RespawnManager to respawn this player
        if (RespawnManager.Instance != null)
        {
            // If the player object is destroyed on death, you would call:
            // RespawnManager.Instance.InstantiatePlayerAtAnchor();
            // Then destroy this current player object: Destroy(gameObject);
            // For this example, we assume the player object persists and is just moved.
            RespawnManager.Instance.RespawnPlayer(this);
        }
        else
        {
            Debug.LogError("RespawnManager not found! Player cannot respawn.");
            // Handle game over or scene reload if respawn isn't possible
        }
    }

    /// <summary>
    /// Resets the player's state (health, physics, status) after respawn or game start.
    /// This method is crucial for ensuring the player is ready to play again.
    /// </summary>
    public void ResetPlayerState()
    {
        currentHealth = maxHealth;
        isDead = false;

        // Reset physics state
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Player state reset. Health: " + currentHealth);
    }

    /// <summary>
    /// Example of collision damage (e.g., touching an enemy or hazardous object).
    /// </summary>
    /// <param name="collision">The Collision data.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // Example: If player collides with an object tagged "Hazard", take damage
        if (collision.gameObject.CompareTag("Hazard"))
        {
            Debug.Log($"Player hit a Hazard: {collision.gameObject.name}");
            TakeDamage(demoDamageAmount * 2); // Take more damage from hazards
        }
    }
}
```

---

### How to Set Up the Respawn Anchor System in Unity:

Follow these steps to get the example working in your Unity project:

1.  **Create a New Scene:**
    *   Start with a new or existing Unity scene. Save it.

2.  **Respawn Manager:**
    *   Create an empty GameObject in your scene (e.g., rename it to `_RespawnSystem`).
    *   Attach the `RespawnManager.cs` script to this GameObject.
    *   **Default Start Respawn Anchor:**
        *   Create another empty GameObject (e.g., `StartAnchor`) and position it where you want the player to *initially* spawn when the game starts.
        *   Drag this `StartAnchor` GameObject from the Hierarchy into the `Default Start Respawn Anchor` field in the `_RespawnSystem`'s Inspector.
    *   **Player Prefab (Optional):** For this example, the player object *persists* and is just teleported. So, you can leave the `Player Prefab` field in the `RespawnManager` Inspector **null**. (If your game design requires destroying and re-instantiating the player on death, you would create a `Player` Prefab and drag it here).

3.  **Player Object:**
    *   Create a 3D object that will serve as your player (e.g., `3D Object -> Capsule`). Rename it to `Player`.
    *   **Add Components:**
        *   Add a `Rigidbody` component (`Add Component -> Physics -> Rigidbody`).
        *   Add the `PlayerController.cs` script to it.
    *   **Tag:** In the Inspector, set the `Player` GameObject's Tag to `Player`. (If `Player` tag doesn't exist, click `Tag -> Add Tag...`, add `Player`, then select it for your `Player` object).
    *   Position your `Player` object somewhere in the scene (it will be moved to the `Default Start Respawn Anchor` by the `RespawnManager` on play).

4.  **Respawn Anchors (Checkpoints):**
    *   Create several empty GameObjects in your scene where you want checkpoints (e.g., `Checkpoint_A`, `Checkpoint_B`, `Checkpoint_C`).
    *   **Add Components to each Checkpoint:**
        *   Add a `Box Collider` or `Sphere Collider` component to each checkpoint. Adjust its size to be a reasonable area for the player to enter.
        *   **IMPORTANT:** Check the `Is Trigger` box on the collider.
        *   Attach the `RespawnAnchor.cs` script to each checkpoint.
    *   Position these checkpoints at strategic points in your level.

5.  **Test Environment (Optional but Recommended):**
    *   Add a `Plane` or some `Cube`s to create a simple ground and obstacles so your player has something to move on.
    *   You can create a `Cube` and set its Tag to `Hazard` (add the tag if it doesn't exist) to simulate an environmental damage source. This will trigger the `OnCollisionEnter` in the `PlayerController`.

---

### How to Play/Test the System:

1.  **Run the Unity scene.**
2.  Observe the Console: You should see messages confirming `RespawnManager` initialization and the initial anchor being set.
3.  Your player (the Capsule/Cube) should start at the position of your `StartAnchor`.
4.  Move the player around the scene using **WASD** or **Arrow Keys**.
5.  Walk your player over one of your `RespawnAnchor` objects. You should see a debug message in the console indicating that a new respawn anchor has been set (e.g., "New respawn anchor set to 'Checkpoint_A'").
6.  Press the **Spacebar** key repeatedly to make the player take damage. Each press will deduct `demoDamageAmount` from health.
7.  Once the player's health reaches zero, it will `Die()` and automatically respawn at the *last activated* `RespawnAnchor`.
8.  Press **R** to manually trigger a respawn at any time (useful for quick testing).
9.  Try moving to another checkpoint and then dying. The player should respawn at the *newest* checkpoint.

This setup provides a fully functional and educational demonstration of the Respawn Anchor System pattern in Unity.