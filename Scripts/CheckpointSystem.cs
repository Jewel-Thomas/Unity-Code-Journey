// Unity Design Pattern Example: CheckpointSystem
// This script demonstrates the CheckpointSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The CheckpointSystem design pattern in Unity allows developers to save the player's progress and game state at specific points (checkpoints) and then restore that state upon events like player death, game over, or manual loading. This pattern is crucial for creating robust and forgiving game experiences.

This example provides a complete, practical implementation of a CheckpointSystem in C# for Unity.

---

## CheckpointSystem Design Pattern in Unity

The CheckpointSystem typically involves three main components:

1.  **CheckpointManager (Singleton):** A central class responsible for storing the last activated checkpoint's location and the player's saved game state. It provides methods for checkpoints to register themselves and for the player (or game logic) to save and load player state.
2.  **Checkpoint (Trigger Object):** A GameObject in the scene that, when triggered by the player, notifies the `CheckpointManager` to update the last known checkpoint location and save the player's current state.
3.  **SavablePlayer (State Provider):** A component attached to the player (or any savable entity) that knows how to extract its current state into a serializable data structure (`SavablePlayerState`) and how to apply a saved state back to itself. This decouples the actual player logic from the saving/loading mechanism.

---

### Project Setup and Usage Instructions:

1.  **Create C# Scripts:**
    *   Create a new C# script named `CheckpointManager.cs`.
    *   Create a new C# script named `SavablePlayer.cs`.
    *   Create a new C# script named `Checkpoint.cs`.
    *   Create a new C# script named `PlayerControllerExample.cs`.

2.  **`CheckpointManager` Setup:**
    *   Create an empty GameObject in your scene (e.g., named "GameManagers").
    *   Attach the `CheckpointManager.cs` script to this GameObject.
    *   The `CheckpointManager` will automatically persist across scenes due to `DontDestroyOnLoad`.

3.  **Player Setup:**
    *   Create a 3D Cube or Capsule (or use your actual player model). Name it "Player".
    *   Add a `Rigidbody` component to the Player (ensure "Use Gravity" is checked if desired).
    *   Add a `Capsule Collider` (or appropriate collider) to the Player.
    *   Attach the `SavablePlayer.cs` script to your "Player" GameObject.
    *   Attach the `PlayerControllerExample.cs` script to your "Player" GameObject.

4.  **Checkpoint Setup:**
    *   Create a 3D Cube. Name it "Checkpoint_1".
    *   Set its scale to something visible (e.g., X=1, Y=0.2, Z=1 for a flat platform).
    *   Attach the `Checkpoint.cs` script to "Checkpoint_1".
    *   Ensure its `Box Collider` (or any collider you use) is checked as **"Is Trigger"**.
    *   Create two new materials: one for `DefaultMaterial` (e.g., grey) and one for `ActivatedMaterial` (e.g., green). Assign these to the `Checkpoint` script's inspector fields.
    *   Assign the `MeshRenderer` of the checkpoint to the `_meshRenderer` field in the inspector.
    *   (Optional) Check `_isStartingCheckpoint` on your first checkpoint if you want it to be the default spawn point and save the initial player state when the game starts.
    *   Duplicate "Checkpoint_1" multiple times (e.g., "Checkpoint_2", "Checkpoint_3") and place them at different locations in your scene to simulate progress.

5.  **Test the System:**
    *   Run the scene.
    *   Move your player (`W,A,S,D` keys).
    *   Walk over a checkpoint. You should see a debug message and the checkpoint's material change (if configured).
    *   Press `D` to take damage.
    *   Press `S` to add score.
    *   Press `I` to add an item to inventory.
    *   Continue taking damage until health reaches 0, or press `R` to manually trigger a respawn.
    *   The player should respawn at the last activated checkpoint's position with their health, score, and inventory restored to the state they were in when that checkpoint was activated.

---

### The C# Scripts:

**1. `CheckpointManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // For SavablePlayerState's list

/// <summary>
/// The CheckpointSystem design pattern's central manager.
/// Manages the current checkpoint location and the player's saved game state.
/// It's implemented as a Singleton for easy global access from any script.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static CheckpointManager _instance;
    public static CheckpointManager Instance
    {
        get
        {
            // If the instance doesn't exist, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<CheckpointManager>();

                // If still no instance, create a new GameObject and add the component.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(CheckpointManager).Name);
                    _instance = singletonObject.AddComponent<CheckpointManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure only one instance of CheckpointManager exists.
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Multiple CheckpointManagers found. Destroying this duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // Make the manager persist across scene loads.
            DontDestroyOnLoad(gameObject); 
            Debug.Log("CheckpointManager initialized and ready.");

            // Initialize with a default checkpoint, e.g., the scene's starting point.
            // This ensures there's always a valid spawn point even if no checkpoint is hit.
            _currentCheckpointPosition = Vector3.zero; // Default to origin
            _currentCheckpointRotation = Quaternion.identity; // Default to no rotation
            _lastSavedState = null; // No player state saved yet
        }
    }

    // --- Checkpoint Location Management ---
    private Vector3 _currentCheckpointPosition;
    private Quaternion _currentCheckpointRotation;

    /// <summary>
    /// Registers a new checkpoint's location and rotation.
    /// This method is typically called by a Checkpoint object when the player triggers it.
    /// It updates where the player will respawn.
    /// </summary>
    /// <param name="position">The world position of the checkpoint.</param>
    /// <param name="rotation">The world rotation of the checkpoint (e.g., player's facing direction).</param>
    public void RegisterCheckpoint(Vector3 position, Quaternion rotation)
    {
        _currentCheckpointPosition = position;
        _currentCheckpointRotation = rotation;
        Debug.Log($"Checkpoint registered at: {position} with rotation: {rotation.eulerAngles}");
    }

    /// <summary>
    /// Teleports a GameObject (typically the player) to the last registered checkpoint's position and rotation.
    /// This is a crucial step in a respawn mechanism.
    /// </summary>
    /// <param name="objectToTeleport">The GameObject to move (e.g., the player's GameObject).</param>
    public void TeleportPlayerToLastCheckpoint(GameObject objectToTeleport)
    {
        if (objectToTeleport != null)
        {
            objectToTeleport.transform.position = _currentCheckpointPosition;
            objectToTeleport.transform.rotation = _currentCheckpointRotation;
            Debug.Log($"Teleported {objectToTeleport.name} to last checkpoint: {_currentCheckpointPosition}");

            // If the player has a Rigidbody, reset its velocity to prevent unwanted momentum.
            Rigidbody rb = objectToTeleport.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogError("Attempted to teleport a null object to checkpoint.");
        }
    }

    // --- Player State Management ---
    // This private field holds the last successfully saved state of the player.
    private SavablePlayerState _lastSavedState;

    /// <summary>
    /// Saves the current state of a SavablePlayer component.
    /// This method is called by Checkpoint objects when activated, or by other game logic
    /// that needs to preserve the player's progress (health, score, inventory, etc.).
    /// </summary>
    /// <param name="savablePlayer">The SavablePlayer component whose state should be saved.</param>
    public void SavePlayerState(SavablePlayer savablePlayer)
    {
        if (savablePlayer != null)
        {
            _lastSavedState = savablePlayer.GetCurrentState();
            Debug.Log($"Player state saved. Health: {_lastSavedState.health}, Score: {_lastSavedState.score}, Items: {_lastSavedState.inventoryItems.Count}");
        }
        else
        {
            Debug.LogError("Attempted to save state for a null SavablePlayer component.");
        }
    }

    /// <summary>
    /// Loads the last saved state into a SavablePlayer component.
    /// This method is typically called when the player dies and needs to respawn,
    /// or when a "Load Game" function is triggered.
    /// </summary>
    /// <param name="savablePlayer">The SavablePlayer component to which the state should be applied.</param>
    public void LoadPlayerState(SavablePlayer savablePlayer)
    {
        if (savablePlayer != null && _lastSavedState != null)
        {
            savablePlayer.ApplyState(_lastSavedState);
            Debug.Log($"Player state loaded. Health: {_lastSavedState.health}, Score: {_lastSavedState.score}, Items: {_lastSavedState.inventoryItems.Count}");
        }
        else if (savablePlayer == null)
        {
            Debug.LogError("Attempted to load state for a null SavablePlayer component.");
        }
        else // _lastSavedState == null
        {
            Debug.LogWarning("No player state has been saved yet. Cannot load.");
        }
    }

    /// <summary>
    /// Checks if there's any player state currently saved in the manager.
    /// Useful for determining if a "Load Game" option should be available or to set an initial state.
    /// </summary>
    public bool HasSavedState => _lastSavedState != null;

    /// <summary>
    /// Resets all saved player data and checkpoint information.
    /// Useful for starting a new game or resetting progress.
    /// </summary>
    public void ResetSavedData()
    {
        _lastSavedState = null;
        _currentCheckpointPosition = Vector3.zero; // Reset to a default start
        _currentCheckpointRotation = Quaternion.identity;
        Debug.Log("CheckpointManager: All saved data reset to default.");
    }
}
```

**2. `SavablePlayer.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<string>

/// <summary>
/// This class defines the data structure that holds all relevant player state
/// that needs to be saved and loaded by the CheckpointSystem.
/// It MUST be marked with [System.Serializable] to allow Unity (and the CheckpointManager)
/// to properly store and retrieve its data.
/// </summary>
[System.Serializable]
public class SavablePlayerState
{
    // Basic transform data (position and rotation)
    // While CheckpointManager handles direct teleport, including it here ensures the state is complete.
    public Vector3 position;
    public Quaternion rotation;

    // Game-specific player stats that should be saved/loaded
    public float health;
    public int score;
    public List<string> inventoryItems; // Example: a list of item names or IDs

    // Constructor to easily create a SavablePlayerState object
    public SavablePlayerState(Vector3 pos, Quaternion rot, float hp, int scr, List<string> items)
    {
        position = pos;
        rotation = rot;
        health = hp;
        score = scr;
        // Create a new list here to ensure the saved state has its own copy,
        // preventing future modifications to the player's active inventory from changing the saved state.
        inventoryItems = new List<string>(items); 
    }
}

/// <summary>
/// This component is attached to the player GameObject.
/// It acts as an interface for the CheckpointManager to interact with the player's state.
/// It knows how to extract the player's current data into a SavablePlayerState object
/// and how to apply a previously saved SavablePlayerState back to the player.
/// </summary>
public class SavablePlayer : MonoBehaviour
{
    [Header("Player Stats (for demonstration)")]
    [SerializeField] private float _currentHealth = 100f;
    [SerializeField] private int _currentScore = 0;
    [SerializeField] private List<string> _inventory = new List<string>();

    // Public properties to allow other scripts to interact with player stats.
    // Ensure health doesn't go below zero.
    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Max(0, value); 
    }
    public int CurrentScore
    {
        get => _currentScore;
        set => _currentScore = value;
    }
    // Read-only access to the inventory list. Modify via AddItem/RemoveItem methods.
    public List<string> Inventory => _inventory; 

    /// <summary>
    /// Example method: Player takes damage.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current Health: {_currentHealth}");
        // Game logic for death condition would typically be handled by a PlayerController.
    }

    /// <summary>
    /// Example method: Player gains score.
    /// </summary>
    /// <param name="amount">The amount of score to add.</param>
    public void AddScore(int amount)
    {
        _currentScore += amount;
        Debug.Log($"Player gained {amount} score. Total Score: {_currentScore}");
    }

    /// <summary>
    /// Example method: Player picks up an item and adds it to inventory.
    /// </summary>
    /// <param name="item">The name or ID of the item to add.</param>
    public void AddItemToInventory(string item)
    {
        if (!_inventory.Contains(item)) // Prevent duplicates for this simple example
        {
            _inventory.Add(item);
            Debug.Log($"Added '{item}' to inventory. Inventory size: {_inventory.Count}");
        }
        else
        {
            Debug.Log($"Inventory already contains '{item}'.");
        }
    }

    /// <summary>
    /// Gathers all relevant player data into a new SavablePlayerState object.
    /// This method is called by the CheckpointManager when the player's state needs to be saved.
    /// </summary>
    /// <returns>A new SavablePlayerState object containing the player's current data.</returns>
    public SavablePlayerState GetCurrentState()
    {
        return new SavablePlayerState(
            transform.position,     // Current world position
            transform.rotation,     // Current world rotation
            _currentHealth,         // Current health
            _currentScore,          // Current score
            _inventory              // Current inventory items
        );
    }

    /// <summary>
    /// Applies the data from a SavablePlayerState object to the player.
    /// This method is called by the CheckpointManager when a previously saved state needs to be loaded.
    /// </summary>
    /// <param name="state">The SavablePlayerState object containing the data to apply.</param>
    public void ApplyState(SavablePlayerState state)
    {
        // Apply position and rotation. Note: CheckpointManager.TeleportPlayerToLastCheckpoint
        // also handles this, but including it here makes SavablePlayerState comprehensive.
        transform.position = state.position;
        transform.rotation = state.rotation;

        _currentHealth = state.health;
        _currentScore = state.score;
        // Create a new list for inventory to avoid reference issues with the old list.
        _inventory = new List<string>(state.inventoryItems); 

        Debug.Log($"Applied saved state: Health={_currentHealth}, Score={_currentScore}, Items={_inventory.Count}");
    }
}
```

**3. `Checkpoint.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Represents a single checkpoint in the game world.
/// When the player enters its trigger, it notifies the CheckpointManager
/// to register its location and save the player's current state.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures there's a collider for trigger detection
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Check this if this checkpoint should be activated at the start of the game.")]
    [SerializeField] private bool _isStartingCheckpoint = false;

    [Header("Visual Feedback")]
    [Tooltip("Material to use when the checkpoint is active/hit.")]
    [SerializeField] private Material _activatedMaterial;
    [Tooltip("Material to use when the checkpoint is inactive/default.")]
    [SerializeField] private Material _defaultMaterial;
    [Tooltip("Assign the MeshRenderer of this checkpoint GameObject.")]
    [SerializeField] private MeshRenderer _meshRenderer; 

    private bool _hasBeenActivated = false; // Prevents redundant saves on re-entry

    private void Awake()
    {
        // Ensure the attached collider is set to be a trigger for detection.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"Checkpoint '{gameObject.name}' has no Collider component! It won't detect triggers.", this);
        }

        // Try to get MeshRenderer if not assigned in inspector.
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        // Apply default material at start to show it's initially inactive.
        if (_meshRenderer != null && _defaultMaterial != null)
        {
            _meshRenderer.material = _defaultMaterial;
        }
        else if (_meshRenderer == null)
        {
            Debug.LogWarning($"Checkpoint '{gameObject.name}': No MeshRenderer assigned or found. Visual feedback disabled.", this);
        }
    }

    private void Start()
    {
        // If this checkpoint is designated as a starting point, activate it immediately.
        if (_isStartingCheckpoint)
        {
            ActivateCheckpoint(null); // Pass null as we don't have a specific player collider
            _hasBeenActivated = true; // Mark as activated for subsequent checks
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player (by looking for SavablePlayer component).
        SavablePlayer player = other.GetComponent<SavablePlayer>();
        if (player != null && !_hasBeenActivated)
        {
            Debug.Log($"Player entered checkpoint: {gameObject.name}");
            ActivateCheckpoint(player);
            _hasBeenActivated = true; // Mark as activated to prevent re-saving immediately
        }
        else if (player != null && _hasBeenActivated)
        {
            Debug.Log($"Player re-entered already activated checkpoint: {gameObject.name} (no new save).");
        }
    }

    /// <summary>
    /// Performs the actions required when a checkpoint is activated.
    /// </summary>
    /// <param name="player">The SavablePlayer that activated the checkpoint (can be null for starting checkpoint).</param>
    private void ActivateCheckpoint(SavablePlayer player)
    {
        // 1. Register this checkpoint's location and rotation with the manager.
        CheckpointManager.Instance.RegisterCheckpoint(transform.position, transform.rotation);

        // 2. If a player is present, save their current game state.
        if (player != null)
        {
            CheckpointManager.Instance.SavePlayerState(player);
        }
        else if (_isStartingCheckpoint)
        {
            // For starting checkpoint, if player not found via trigger, try to find one.
            // This handles cases where player might not trigger it (e.g., spawns on it).
            SavablePlayer initialPlayer = FindObjectOfType<SavablePlayer>();
            if (initialPlayer != null)
            {
                CheckpointManager.Instance.SavePlayerState(initialPlayer);
                Debug.Log($"Starting checkpoint '{gameObject.name}' registered and initial player state saved (via FindObjectOfType).");
            }
            else
            {
                Debug.LogWarning($"Starting checkpoint '{gameObject.name}' registered, but no SavablePlayer found to save initial state.", this);
            }
        }

        // 3. Provide visual feedback that the checkpoint has been activated.
        ApplyActivatedVisuals();
    }

    /// <summary>
    /// Changes the checkpoint's visual appearance to indicate activation.
    /// </summary>
    private void ApplyActivatedVisuals()
    {
        if (_meshRenderer != null && _activatedMaterial != null)
        {
            _meshRenderer.material = _activatedMaterial;
            Debug.Log($"Checkpoint '{gameObject.name}' visuals updated to activated state.");
        }
    }

    /// <summary>
    /// Resets this checkpoint to its inactive state.
    /// Useful if game logic requires checkpoints to be re-activatable.
    /// </summary>
    public void ResetCheckpoint()
    {
        _hasBeenActivated = false;
        if (_meshRenderer != null && _defaultMaterial != null)
        {
            _meshRenderer.material = _defaultMaterial;
        }
        Debug.Log($"Checkpoint '{gameObject.name}' reset to inactive state.");
    }
}
```

**4. `PlayerControllerExample.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // For list operations if player holds items

/// <summary>
/// An example Player Controller that demonstrates interaction with the CheckpointSystem.
/// It provides basic player movement, simulates taking damage, and triggers respawning
/// at the last activated checkpoint with restored state.
/// Attach this script to your player GameObject, which MUST also have a SavablePlayer component.
/// </summary>
[RequireComponent(typeof(SavablePlayer))] // Ensures the player GameObject has SavablePlayer
public class PlayerControllerExample : MonoBehaviour
{
    [Header("Player Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 100f;

    private SavablePlayer _savablePlayer; // Reference to the player's savable state component
    private Rigidbody _rb; // Reference to the player's Rigidbody for physics movement

    void Awake()
    {
        // Get references to required components.
        _savablePlayer = GetComponent<SavablePlayer>();
        _rb = GetComponent<Rigidbody>();

        if (_rb == null)
        {
            Debug.LogWarning("PlayerControllerExample: No Rigidbody found on player. Movement might not work as expected.", this);
        }
    }

    void Start()
    {
        // Ensure the CheckpointManager has an initial state from the player.
        // This is important if no starting checkpoint is immediately activated.
        // It guarantees there's always a valid state to revert to if the player dies early.
        if (!CheckpointManager.Instance.HasSavedState)
        {
            Debug.Log("No initial state saved. Saving player's current state as the default start point.");
            CheckpointManager.Instance.RegisterCheckpoint(transform.position, transform.rotation);
            CheckpointManager.Instance.SavePlayerState(_savablePlayer);
        }
    }

    void Update()
    {
        HandleMovement(); // Process player movement input
        HandleInput();    // Process other player actions (damage, respawn, etc.)
    }

    /// <summary>
    /// Handles basic player movement based on input (W,A,S,D or arrow keys).
    /// Uses Rigidbody.MovePosition and Rigidbody.MoveRotation for physics-based movement.
    /// </summary>
    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down arrows

        // Calculate movement direction relative to the player's forward vector.
        Vector3 moveDirection = transform.forward * verticalInput * _moveSpeed * Time.deltaTime;

        // Apply movement using Rigidbody if available, otherwise directly to transform.
        if (_rb != null)
        {
            _rb.MovePosition(_rb.position + moveDirection);
        }
        else
        {
            transform.Translate(moveDirection, Space.World);
        }

        // Calculate rotation amount.
        float rotationAmount = horizontalInput * _rotationSpeed * Time.deltaTime;

        // Apply rotation using Rigidbody if available, otherwise directly to transform.
        if (_rb != null)
        {
            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * rotationAmount);
            _rb.MoveRotation(_rb.rotation * deltaRotation);
        }
        else
        {
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }

    /// <summary>
    /// Handles other player-controlled actions like taking damage, respawning, adding score, or items.
    /// </summary>
    private void HandleInput()
    {
        // Example: Press 'D' to simulate taking damage.
        if (Input.GetKeyDown(KeyCode.D))
        {
            _savablePlayer.TakeDamage(20f);
            if (_savablePlayer.CurrentHealth <= 0)
            {
                Die(); // If health drops to 0 or below, trigger player death/respawn.
            }
        }

        // Example: Press 'R' to manually respawn (simulates immediate death/reload).
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Manual respawn initiated by player input.");
            Die();
        }

        // Example: Press 'S' to add score.
        if (Input.GetKeyDown(KeyCode.S))
        {
            _savablePlayer.AddScore(100);
        }

        // Example: Press 'I' to add a unique item to inventory.
        if (Input.GetKeyDown(KeyCode.I))
        {
            string newItem = "Keycard " + (_savablePlayer.Inventory.Count + 1); // Simple unique item name
            _savablePlayer.AddItemToInventory(newItem);
        }
    }

    /// <summary>
    /// Simulates player death and initiates the respawn sequence using the CheckpointSystem.
    /// This method is the core demonstration of loading from a checkpoint.
    /// </summary>
    public void Die()
    {
        Debug.Log("Player has died! Triggering respawn sequence...");

        // 1. Load the player's saved state (health, score, inventory, etc.) from the CheckpointManager.
        // This effectively "resets" the player's internal state to what it was at the last checkpoint.
        CheckpointManager.Instance.LoadPlayerState(_savablePlayer);

        // 2. Teleport the player's GameObject to the last checkpoint's physical location and rotation.
        // This moves the player back to the correct spot in the world.
        CheckpointManager.Instance.TeleportPlayerToLastCheckpoint(gameObject);

        Debug.Log("Player respawned successfully with saved state from last checkpoint.");
    }
}
```