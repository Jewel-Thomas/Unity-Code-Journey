// Unity Design Pattern Example: PlayerRespawnSystem
// This script demonstrates the PlayerRespawnSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **PlayerRespawnSystem** design pattern. It provides a robust, reusable system for handling player deaths and respawns, emphasizing loose coupling and clear separation of concerns.

**Key Components and Design Pattern Principles:**

1.  **`PlayerRespawnSystem` (Singleton Manager):**
    *   Acts as a central point of control for all respawn logic.
    *   **Singleton Pattern:** Ensures there's only one instance of the respawn system throughout the game, easily accessible from anywhere.
    *   Manages a collection of potential `respawnPoints`.
    *   Coordinates the respawn process, including delays and choosing a respawn location.
    *   Registers and unregisters `IRespawnable` entities.

2.  **`IRespawnable` (Interface):**
    *   **Interface Segregation Principle:** Defines a contract that any game object capable of being respawned must adhere to. This decouples the `PlayerRespawnSystem` from specific player implementations.
    *   Requires `Respawn(Transform spawnPoint)`: The method the system calls on the player to reset its state.
    *   Requires `Die()`: The method the player calls when it needs to initiate its death sequence and notify the `PlayerRespawnSystem`.
    *   Requires `GetGameObject()`: To get the GameObject associated with the respawnable entity for disabling/enabling.

3.  **`PlayerExampleController` (Concrete Implementation):**
    *   Implements the `IRespawnable` interface.
    *   Handles player-specific logic for taking damage, dying, and resetting its state upon respawn (e.g., health, position, velocity, controls).
    *   Communicates with the `PlayerRespawnSystem` when it dies.

4.  **`RespawnPointVisualizer` (Editor Helper):**
    *   A simple `MonoBehaviour` to mark GameObjects as respawn points and visualize them in the editor. This improves workflow and clarity.

**How the Pattern Works:**

1.  **Initialization:** The `PlayerRespawnSystem` singleton is created. `PlayerExampleController` instances register themselves with the system when they awaken.
2.  **Death:** When a `PlayerExampleController` takes fatal damage, its `Die()` method is called. This method handles player-specific death logic (e.g., disabling controls, playing death animation) and then notifies the `PlayerRespawnSystem` by calling `PlayerRespawnSystem.Instance.OnPlayerDied(this)`.
3.  **Respawn Request:** The `PlayerRespawnSystem` receives the death notification. It determines the next respawn point and starts a coroutine to handle the `respawnDelay`.
4.  **Respawn Execution:** After the delay, the `PlayerRespawnSystem` calls the `Respawn(Transform spawnPoint)` method on the dying `IRespawnable` player.
5.  **Player Resurrection:** The `PlayerExampleController`'s `Respawn` method then resets its health, moves it to the `spawnPoint`, re-enables its controls, and restores any other necessary state.

---

### **1. `PlayerRespawnSystem.cs`**

This single script contains all the core components (interface, system, example player, helper). In a larger project, `IRespawnable` and `PlayerExampleController` would typically be in separate files.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- 1. IRespawnable Interface ---
// This interface defines the contract for any object that can be respawned by the PlayerRespawnSystem.
// It promotes loose coupling, allowing the system to work with any player implementation that adheres to this contract.
public interface IRespawnable
{
    // Called by the PlayerRespawnSystem to instruct the entity to respawn at a given point.
    void Respawn(Transform spawnPoint);

    // Called by the respawnable entity itself when it dies, notifying the PlayerRespawnSystem.
    void Die();

    // Provides the GameObject associated with this respawnable entity.
    // Useful for the system to enable/disable the object during death/respawn.
    GameObject GetGameObject();
}

// --- 2. PlayerRespawnSystem (Singleton Manager) ---
// This class manages all player respawn logic. It acts as a central hub
// for registered players to notify their death and to coordinate their respawn.
public class PlayerRespawnSystem : MonoBehaviour
{
    // Singleton pattern for easy access from anywhere in the game.
    public static PlayerRespawnSystem Instance { get; private set; }

    [Header("Respawn Settings")]
    [Tooltip("Array of Transforms representing possible respawn points.")]
    public Transform[] respawnPoints;
    [Tooltip("Time in seconds before a player respawns after dying.")]
    public float respawnDelay = 3.0f;
    [Tooltip("Optional: Particle effect to play when a player respawns.")]
    public GameObject respawnEffectPrefab;

    // A list of all IRespawnable entities currently registered with the system.
    private List<IRespawnable> registeredPlayers = new List<IRespawnable>();
    // Index to cycle through respawn points. Can be adapted for more complex logic.
    private int currentRespawnPointIndex = 0;

    void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Ensure the system persists across scene loads if desired (e.g., for a multi-scene game).
            // DontDestroyOnLoad(gameObject);
        }

        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogError("PlayerRespawnSystem: No respawn points assigned! Please assign transforms to the 'Respawn Points' array in the Inspector.");
        }
    }

    /// <summary>
    /// Registers an IRespawnable entity with the system.
    /// This should be called by the entity (e.g., PlayerExampleController) when it initializes.
    /// </summary>
    /// <param name="player">The IRespawnable entity to register.</param>
    public void RegisterPlayer(IRespawnable player)
    {
        if (!registeredPlayers.Contains(player))
        {
            registeredPlayers.Add(player);
            Debug.Log($"PlayerRespawnSystem: Registered player: {player.GetGameObject().name}");
        }
    }

    /// <summary>
    /// Unregisters an IRespawnable entity from the system.
    /// This should be called when the entity is destroyed or no longer needs to be managed.
    /// </summary>
    /// <param name="player">The IRespawnable entity to unregister.</param>
    public void UnregisterPlayer(IRespawnable player)
    {
        if (registeredPlayers.Contains(player))
        {
            registeredPlayers.Remove(player);
            Debug.Log($"PlayerRespawnSystem: Unregistered player: {player.GetGameObject().name}");
        }
    }

    /// <summary>
    /// This is the core method called by an IRespawnable entity when it "dies".
    /// It initiates the respawn sequence for that specific player.
    /// </summary>
    /// <param name="player">The IRespawnable entity that died.</param>
    public void OnPlayerDied(IRespawnable player)
    {
        Debug.Log($"PlayerRespawnSystem: {player.GetGameObject().name} has died. Initiating respawn sequence...");

        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogError("PlayerRespawnSystem: Cannot respawn, no respawn points defined!");
            return;
        }

        // Disable the player's GameObject immediately after death notification.
        // The player's Die() method should also handle internal state (e.g., controls).
        player.GetGameObject().SetActive(false);

        // Get the next respawn point.
        Transform spawnPoint = GetNextRespawnPoint();

        // Start the respawn coroutine to handle the delay.
        StartCoroutine(RespawnPlayerCoroutine(player, spawnPoint));
    }

    /// <summary>
    /// Coroutine that handles the respawn delay and then calls the player's Respawn method.
    /// </summary>
    /// <param name="player">The IRespawnable entity to respawn.</param>
    /// <param name="spawnPoint">The Transform where the player should respawn.</param>
    private IEnumerator RespawnPlayerCoroutine(IRespawnable player, Transform spawnPoint)
    {
        yield return new WaitForSeconds(respawnDelay);

        Debug.Log($"PlayerRespawnSystem: Respawning {player.GetGameObject().name} at {spawnPoint.position}...");

        // Re-enable the player's GameObject just before calling its Respawn method.
        player.GetGameObject().SetActive(true);

        // Notify the player to perform its respawn logic.
        player.Respawn(spawnPoint);

        // Play respawn effect if specified.
        if (respawnEffectPrefab != null)
        {
            Instantiate(respawnEffectPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Determines the next respawn point. This logic can be customized
    /// (e.g., random, nearest, checkpoint-based).
    /// </summary>
    /// <returns>The Transform of the chosen respawn point.</returns>
    private Transform GetNextRespawnPoint()
    {
        if (respawnPoints.Length == 0) return null;

        Transform chosenPoint = respawnPoints[currentRespawnPointIndex];
        currentRespawnPointIndex = (currentRespawnPointIndex + 1) % respawnPoints.Length; // Cycle through points
        return chosenPoint;
    }

    // --- Editor-specific visualization (Optional, but good for usability) ---
    void OnDrawGizmos()
    {
        if (respawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in respawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 1f);
                    Gizmos.DrawLine(point.position, point.position + point.forward * 2f);
                }
            }
        }
    }
}


// --- 3. PlayerExampleController (Concrete IRespawnable Implementation) ---
// This is an example player script that implements the IRespawnable interface.
// It manages its own health, movement, and interaction with the PlayerRespawnSystem.
[RequireComponent(typeof(CharacterController))]
public class PlayerExampleController : MonoBehaviour, IRespawnable
{
    [Header("Player Settings")]
    public int maxHealth = 100;
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f; // For simple Y-axis rotation
    public float gravity = -9.8f;

    [Header("Death Settings")]
    [Tooltip("Optional: Particle effect to play when the player dies.")]
    public GameObject deathEffectPrefab;

    private int currentHealth;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private bool isDead = false;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        // Register this player with the Respawn System when it becomes active.
        if (PlayerRespawnSystem.Instance != null)
        {
            PlayerRespawnSystem.Instance.RegisterPlayer(this);
        }
        else
        {
            Debug.LogWarning("PlayerExampleController: PlayerRespawnSystem.Instance is null. Cannot register player.");
        }
        InitializePlayer(); // Call this when the player is enabled (e.g., after respawn)
    }

    void OnDisable()
    {
        // Unregister this player when it's disabled or destroyed.
        if (PlayerRespawnSystem.Instance != null)
        {
            PlayerRespawnSystem.Instance.UnregisterPlayer(this);
        }
    }

    void Update()
    {
        if (isDead) return; // Prevent movement if dead

        // Simple Player Movement (Example)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Apply rotation
        transform.Rotate(0, horizontal * rotationSpeed * Time.deltaTime, 0);

        // Move forward/backward relative to player's facing direction
        moveDirection = transform.forward * vertical * moveSpeed;

        // Apply gravity
        moveDirection.y += gravity * Time.deltaTime;

        // Move the character controller
        characterController.Move(moveDirection * Time.deltaTime);

        // For demonstration: Press 'K' to simulate taking damage and potentially dying
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(25); // Simulate taking damage
        }
    }

    // --- IRespawnable Implementation ---

    public void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        isDead = true;

        Debug.Log($"{gameObject.name} has died!");

        // Play death effect if specified.
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Notify the PlayerRespawnSystem that this player has died.
        // The system will then handle the respawn delay and calling Respawn().
        PlayerRespawnSystem.Instance.OnPlayerDied(this);

        // Optional: Disable player input/rendering immediately (system already sets active to false)
        // characterController.enabled = false;
        // gameObject.GetComponent<Renderer>().enabled = false; // If player has a direct renderer
    }

    public void Respawn(Transform spawnPoint)
    {
        Debug.Log($"{gameObject.name} is respawning at {spawnPoint.position}!");

        // Reset player state
        InitializePlayer();

        // Teleport player to respawn point
        characterController.enabled = false; // Disable to prevent character controller issues during teleport
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        characterController.enabled = true; // Re-enable character controller

        // Reset any physics velocity if using Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Optional: Re-enable rendering, controls etc. if they were disabled only by player's Die()
        // gameObject.GetComponent<Renderer>().enabled = true;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    // --- Player-specific Logic ---

    /// <summary>
    /// Resets the player's health and state to initial values.
    /// </summary>
    private void InitializePlayer()
    {
        currentHealth = maxHealth;
        isDead = false;
        Debug.Log($"{gameObject.name} initialized/reset. Health: {currentHealth}/{maxHealth}");
        // Ensure character controller is active for movement
        characterController.enabled = true;
    }

    /// <summary>
    /// Applies damage to the player. If health drops to 0 or below, the player dies.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die(); // Player dies when health reaches zero
        }
    }
}


// --- 4. RespawnPointVisualizer (Editor Helper) ---
// This script simply helps visualize respawn points in the Unity editor.
// Attach this to any GameObject you want to designate as a respawn point.
public class RespawnPointVisualizer : MonoBehaviour
{
    public float gizmoRadius = 0.75f;
    public Color gizmoColor = Color.cyan;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoRadius * 2);
    }
}

// --- 5. DamageTrigger (Example for Player Death) ---
// A simple trigger that causes damage to any IRespawnable that enters it.
public class DamageTrigger : MonoBehaviour
{
    [Tooltip("Amount of damage to inflict when an IRespawnable enters the trigger.")]
    public int damageAmount = 50;
    [Tooltip("If true, the trigger will instantly kill the player.")]
    public bool instantKill = false;

    void OnTriggerEnter(Collider other)
    {
        IRespawnable respawnable = other.GetComponent<IRespawnable>();
        if (respawnable != null)
        {
            PlayerExampleController player = respawnable.GetGameObject().GetComponent<PlayerExampleController>();
            if (player != null)
            {
                if (instantKill)
                {
                    player.TakeDamage(player.maxHealth + 100); // Ensure instant kill
                }
                else
                {
                    player.TakeDamage(damageAmount);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the trigger area
        Gizmos.color = new Color(1f, 0.2f, 0f, 0.5f); // Reddish-orange, semi-transparent
        if (GetComponent<Collider>() is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (GetComponent<Collider>() is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
        else if (GetComponent<Collider>() is CapsuleCollider capsule)
        {
             // This is a bit more complex for Gizmos, often just draw a sphere
             Gizmos.DrawSphere(transform.position + capsule.center, capsule.radius * transform.lossyScale.x);
        }
    }
}
```

---

### **Example Usage in Unity:**

To get this working in your Unity project:

1.  **Create a C# Script:** Create a new C# script named `PlayerRespawnSystem.cs` in your Assets folder.
2.  **Copy and Paste:** Copy the entire code block above and paste it into your `PlayerRespawnSystem.cs` file, replacing its default content.
3.  **Create the `PlayerRespawnSystem` GameObject:**
    *   Create an Empty GameObject in your scene (e.g., `_GameManager`).
    *   Attach the `PlayerRespawnSystem.cs` script to this `_GameManager` GameObject.
4.  **Create Respawn Points:**
    *   Create several Empty GameObjects in your scene (e.g., `SpawnPoint1`, `SpawnPoint2`).
    *   Position them where you want players to respawn.
    *   (Optional but Recommended) Attach the `RespawnPointVisualizer.cs` script to each `SpawnPoint` to see them in the editor.
    *   Drag these `SpawnPoint` GameObjects into the `Respawn Points` array on the `PlayerRespawnSystem` component in the Inspector.
5.  **Create a Player:**
    *   Create a new 3D Object (e.g., a `Capsule` or `Cube`). Rename it to `Player`.
    *   Add a `Character Controller` component to the `Player` GameObject.
    *   Attach the `PlayerExampleController.cs` script to the `Player` GameObject.
    *   Set its initial `Max Health`, `Move Speed`, etc., in the Inspector.
6.  **Create a Death Zone / Damage Trigger:**
    *   Create another 3D Object (e.g., a `Cube`) and stretch it to make a floor-level trigger or a pit.
    *   Set its `Collider` to `Is Trigger`.
    *   Attach the `DamageTrigger.cs` script to this GameObject.
    *   Adjust `Damage Amount` or check `Instant Kill` in the Inspector.
7.  **Optional: Add Visual Effects:**
    *   Create simple particle systems for `Death Effect Prefab` and `Respawn Effect Prefab` (e.g., `GameObject -> Effects -> Particle System`).
    *   Drag these prefabs into the respective slots on the `PlayerRespawnSystem` and `PlayerExampleController` components.
8.  **Run the Scene:** Play your scene. Move the player (using WASD or Arrow Keys for horizontal rotation and forward/backward movement). Walk into the `DamageTrigger` or press `K` repeatedly to see the player die and respawn after the `respawnDelay`.

This setup provides a complete, functional, and educational example of the PlayerRespawnSystem design pattern in Unity.