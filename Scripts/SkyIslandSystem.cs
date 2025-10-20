// Unity Design Pattern Example: SkyIslandSystem
// This script demonstrates the SkyIslandSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SkyIslandSystem' is a conceptual design pattern, which we'll interpret and implement in a practical way for Unity. It's designed to help structure large game projects by creating a centralized manager ("Sky") that orchestrates various independent, modular game systems ("Islands").

**Concept of 'SkyIslandSystem' in Unity:**

*   **The Sky (SkyIslandManager):** A central, usually singleton, component responsible for:
    *   Registering and unregistering all 'Island' modules.
    *   Providing a lookup mechanism for 'Islands' (acting as a lightweight Service Locator).
    *   Managing the global lifecycle (initialization, updates, deinitialization) of its registered 'Islands'.
    *   Facilitating (or at least providing a channel for) inter-island communication.
*   **The Islands (ISkyIsland implementations):** Self-contained, independent game features or subsystems, such as:
    *   Inventory System
    *   Quest System
    *   Audio System
    *   UI Manager
    *   Input System
    *   Save/Load System
    They implement a common `ISkyIsland` interface, allowing the `SkyIslandManager` to interact with them uniformly. Each island encapsulates its own logic, data, and potentially its own GameObject hierarchy.

**Benefits of this pattern:**

1.  **Modularity & Decoupling:** Game features are isolated. Changes in one island generally don't break others. Components can access features via the manager without direct dependencies on concrete island types.
2.  **Scalability:** Easily add, remove, or modify game systems by simply creating or updating an `ISkyIsland` implementation.
3.  **Centralized Control:** The `SkyIslandManager` handles global lifecycle events, ensuring systems are initialized and updated consistently.
4.  **Service Discovery:** Islands (and other game components) can discover and use services provided by other islands through the manager.

---

## SkyIslandSystem Unity Example

This complete C# script includes the `ISkyIsland` interface, the `SkyIslandManager` (the "Sky"), and three example `ISkyIsland` implementations (the "Islands"): `InventoryIsland`, `PlayerInputIsland`, and `QuestIsland`. It also includes a `PlayerControllerClient` script to demonstrate how game components would interact with these islands.

**File Name:** `SkyIslandSystem.cs` (You can copy all the code below into a single C# file named `SkyIslandSystem.cs` in your Unity project).

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for Action events

// ====================================================================================================================
// THE SKY ISLAND SYSTEM DESIGN PATTERN
// ====================================================================================================================
//
// This pattern is designed for building modular, scalable, and decoupled game systems in Unity.
// It consists of a central 'SkyIslandManager' (the "Sky") that orchestrates various independent
// 'ISkyIsland' modules (the "Islands").
//
// Goals:
// 1.  **Decoupling:** Islands don't directly depend on each other's concrete implementations. They
//     communicate via the SkyIslandManager or through defined interfaces/events.
// 2.  **Modularity:** Each Island is a self-contained feature (e.g., Inventory, Quests, Audio, UI).
//     It can be developed, tested, enabled, or disabled independently.
// 3.  **Scalability:** Easily add new game systems by creating new ISkyIsland implementations
//     without altering existing code significantly.
// 4.  **Centralized Management:** The SkyIslandManager handles the lifecycle (initialization, updates,
//     deactivation, deinitialization) of all registered Islands.
// 5.  **Service Locator (Lite):** The SkyIslandManager acts as a central registry, allowing Islands
//     (or any other MonoBehaviour) to discover and interact with other Islands.
//
// Analogy:
// Imagine a collection of floating islands in the sky. Each island has its own purpose and inhabitants.
// A central "Sky" entity manages the weather, time of day, and ensures the stability of all islands,
// sometimes facilitating communication or resource transfer between them.
//
// How it works:
// 1.  **ISkyIsland Interface:** Defines the common contract for all modular game systems.
// 2.  **SkyIslandManager:** A central singleton MonoBehaviour that:
//     a.  Maintains a registry of all active ISkyIsland instances.
//     b.  Manages their global lifecycle (Initialize, Update, Deinitialize).
//     c.  Provides methods for other components to retrieve specific ISkyIsland instances.
// 3.  **Concrete SkyIslands:** Specific game systems (e.g., InventoryIsland, QuestIsland)
//     implement the ISkyIsland interface. They register themselves with the SkyIslandManager
//     typically in their `Awake()` method and unregister in `OnDestroy()`.
//
// ====================================================================================================================

// --------------------------------------------------------------------------------------------------------------------
// 1. ISkyIsland Interface: The Contract for all Modular Game Systems (Islands)
// --------------------------------------------------------------------------------------------------------------------
/// <summary>
/// Defines the contract for a modular game system ("Island") managed by the SkyIslandManager.
/// Each implementing class represents a distinct, self-contained game feature.
/// </summary>
public interface ISkyIsland
{
    /// <summary>
    /// Gets the unique name of this island. Used for identification and logging.
    /// </summary>
    string IslandName { get; }

    /// <summary>
    /// Gets the current active state of the island.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Initializes the island. This is called once by the SkyIslandManager after all islands are registered.
    /// Use this for one-time setup that requires other islands to potentially be available.
    /// </summary>
    /// <param name="manager">A reference to the SkyIslandManager for inter-island communication.</param>
    void Initialize(SkyIslandManager manager);

    /// <summary>
    /// Deinitializes the island. Called by the SkyIslandManager when the application quits or the manager is destroyed.
    /// Use this for cleanup and resource release.
    /// </summary>
    void Deinitialize();

    /// <summary>
    /// Activates the island. Can be called at runtime to enable the island's functionality.
    /// </summary>
    void Activate();

    /// <summary>
    /// Deactivates the island. Can be called at runtime to disable the island's functionality.
    /// </summary>
    void Deactivate();

    /// <summary>
    /// Called by the SkyIslandManager every frame (like Unity's Update).
    /// Use this for frame-dependent logic when the island is active.
    /// </summary>
    void OnSkyUpdate();

    /// <summary>
    /// Called by the SkyIslandManager every fixed framerate frame (like Unity's FixedUpdate).
    /// Use this for physics or fixed-time step logic when the island is active.
    /// </summary>
    void OnSkyFixedUpdate();

    /// <summary>
    /// Called by the SkyIslandManager after all OnSkyUpdate calls have been made (like Unity's LateUpdate).
    /// Use this for camera logic or actions that depend on all other updates when the island is active.
    /// </summary>
    void OnSkyLateUpdate();
}

// --------------------------------------------------------------------------------------------------------------------
// 2. SkyIslandManager: The Central Orchestrator (The "Sky")
// --------------------------------------------------------------------------------------------------------------------
/// <summary>
/// The central manager (the "Sky") for the SkyIslandSystem.
/// This MonoBehaviour acts as a singleton, managing the lifecycle and interaction of all registered ISkyIsland modules.
/// </summary>
[DefaultExecutionOrder(-1000)] // Ensures this script runs very early in the Unity execution order
public class SkyIslandManager : MonoBehaviour
{
    // Singleton pattern for easy global access to the SkyIslandManager
    public static SkyIslandManager Instance { get; private set; }

    // Dictionary to store all registered islands by their type for quick lookup (e.g., GetIsland<InventoryIsland>())
    private readonly Dictionary<Type, ISkyIsland> _islandsByType = new Dictionary<Type, ISkyIsland>();
    // Dictionary to store all registered islands by their name for quick lookup (if unique names are used)
    private readonly Dictionary<string, ISkyIsland> _islandsByName = new Dictionary<string, ISkyIsland>();
    // List of active islands to iterate over for Update/FixedUpdate/LateUpdate calls, optimizing performance.
    private readonly List<ISkyIsland> _activeIslands = new List<ISkyIsland>();

    private bool _isInitialized = false; // Flag to track if the manager has completed its initialization phase

    // ================================================================================================================
    // Unity Lifecycle Methods
    // ================================================================================================================

    private void Awake()
    {
        // Enforce singleton pattern: if an instance already exists, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple SkyIslandManager instances detected. Destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this; // Assign this instance as the singleton
        DontDestroyOnLoad(gameObject); // Keep the manager alive across scene changes

        Debug.Log("SkyIslandManager Awake.");
    }

    // Called after all objects in the scene have called their Awake methods.
    // This is the ideal point to initialize all registered islands, ensuring they exist and have their
    // own internal Awake logic completed before the manager starts orchestrating them.
    private void Start()
    {
        InitializeAllIslands();
    }

    // Called once per frame. Delegates the update call to all currently active islands.
    private void Update()
    {
        if (!_isInitialized) return; // Only update if the manager itself is fully initialized

        // Iterate through active islands and call their OnSkyUpdate method
        foreach (var island in _activeIslands)
        {
            if (island.IsActive) // Only update islands that are currently active
            {
                island.OnSkyUpdate();
            }
        }
    }

    // Called every fixed framerate frame. Delegates the fixed update call to all active islands.
    private void FixedUpdate()
    {
        if (!_isInitialized) return;

        foreach (var island in _activeIslands)
        {
            if (island.IsActive)
            {
                island.OnSkyFixedUpdate();
            }
        }
    }

    // Called once per frame after all Update calls. Delegates the late update call to all active islands.
    private void LateUpdate()
    {
        if (!_isInitialized) return;

        foreach (var island in _activeIslands)
        {
            if (island.IsActive)
            {
                island.OnSkyLateUpdate();
            }
        }
    }

    // Called when the application is quitting. Ensures all islands are properly deinitialized.
    private void OnApplicationQuit()
    {
        DeinitializeAllIslands();
        Debug.Log("SkyIslandManager OnApplicationQuit: All islands deinitialized.");
    }

    // Called when the GameObject is destroyed. Handles cleanup and resets the singleton instance.
    private void OnDestroy()
    {
        if (Instance == this)
        {
            DeinitializeAllIslands(); // Deinitialize if this is the active singleton instance
            Instance = null; // Clear the singleton reference
            Debug.Log("SkyIslandManager OnDestroy: Instance cleared.");
        }
    }

    // ================================================================================================================
    // Public API for Island Management and Access
    // ================================================================================================================

    /// <summary>
    /// Registers an ISkyIsland with the manager. Islands should call this in their Awake method
    /// to ensure they are available before the manager's Start() for initialization.
    /// </summary>
    /// <param name="island">The ISkyIsland instance to register.</param>
    public void RegisterIsland(ISkyIsland island)
    {
        if (island == null)
        {
            Debug.LogError("Attempted to register a null island.");
            return;
        }

        Type islandType = island.GetType();
        // Register by type: If an island of this type already exists, it will be replaced.
        if (_islandsByType.ContainsKey(islandType))
        {
            Debug.LogWarning($"Island of type '{islandType.Name}' already registered. Overwriting with new instance.");
            _islandsByType[islandType] = island;
        }
        else
        {
            _islandsByType.Add(islandType, island);
        }

        // Register by name: If an island with this name already exists, it will be replaced.
        // Ensure names are unique if you plan to retrieve by name.
        if (_islandsByName.ContainsKey(island.IslandName))
        {
            Debug.LogWarning($"Island with name '{island.IslandName}' already registered. Overwriting with new instance.");
            _islandsByName[island.IslandName] = island;
        }
        else
        {
            _islandsByName.Add(island.IslandName, island);
        }

        _activeIslands.Add(island); // Add to a list for global updates
        Debug.Log($"Island Registered: '{island.IslandName}' (Type: {islandType.Name})");

        // If the SkyIslandManager is already initialized (meaning Start() has been called),
        // immediately initialize and activate this newly registered island. This handles dynamic island loading.
        if (_isInitialized)
        {
            island.Initialize(this);
            island.Activate();
        }
    }

    /// <summary>
    /// Unregisters an ISkyIsland from the manager. Islands should call this in their OnDestroy method.
    /// </summary>
    /// <param name="island">The ISkyIsland instance to unregister.</param>
    public void UnregisterIsland(ISkyIsland island)
    {
        if (island == null) return;

        Type islandType = island.GetType();
        // Remove from type-based dictionary
        if (_islandsByType.ContainsKey(islandType) && _islandsByType[islandType] == island)
        {
            _islandsByType.Remove(islandType);
        }

        // Remove from name-based dictionary
        if (_islandsByName.ContainsKey(island.IslandName) && _islandsByName[island.IslandName] == island)
        {
            _islandsByName.Remove(island.IslandName);
        }

        // Remove from the active islands list
        _activeIslands.Remove(island);
        Debug.Log($"Island Unregistered: '{island.IslandName}' (Type: {islandType.Name})");

        // If the manager is initialized, deinitialize and deactivate the island immediately.
        if (_isInitialized)
        {
            island.Deactivate();
            island.Deinitialize();
        }
    }

    /// <summary>
    /// Retrieves a registered ISkyIsland by its type. This is the primary way for other components
    /// to access specific game systems (e.g., `SkyIslandManager.Instance.GetIsland<InventoryIsland>()`).
    /// </summary>
    /// <typeparam name="T">The type of the ISkyIsland to retrieve (e.g., InventoryIsland).</typeparam>
    /// <returns>The ISkyIsland instance, or null if not found or not of the specified type.</returns>
    public T GetIsland<T>() where T : class, ISkyIsland
    {
        if (_islandsByType.TryGetValue(typeof(T), out ISkyIsland island))
        {
            return island as T; // Cast to the specific type T
        }
        Debug.LogWarning($"SkyIslandManager: Island of type '{typeof(T).Name}' not found.");
        return null;
    }

    /// <summary>
    /// Retrieves a registered ISkyIsland by its name.
    /// Use this if you have multiple islands of the same type but with different configurations,
    /// and you've given them unique names in the Inspector.
    /// </summary>
    /// <param name="islandName">The unique name of the island.</param>
    /// <returns>The ISkyIsland instance, or null if not found.</returns>
    public ISkyIsland GetIsland(string islandName)
    {
        if (_islandsByName.TryGetValue(islandName, out ISkyIsland island))
        {
            return island;
        }
        Debug.LogWarning($"SkyIslandManager: Island with name '{islandName}' not found.");
        return null;
    }

    /// <summary>
    /// Activates a specific island by its type, enabling its OnSkyUpdate methods.
    /// </summary>
    /// <typeparam name="T">The type of the island to activate.</typeparam>
    public void ActivateIsland<T>() where T : class, ISkyIsland
    {
        T island = GetIsland<T>();
        if (island != null && !island.IsActive)
        {
            island.Activate();
            Debug.Log($"Island '{island.IslandName}' activated via manager.");
        }
    }

    /// <summary>
    /// Deactivates a specific island by its type, disabling its OnSkyUpdate methods.
    /// </summary>
    /// <typeparam name="T">The type of the island to deactivate.</typeparam>
    public void DeactivateIsland<T>() where T : class, ISkyIsland
    {
        T island = GetIsland<T>();
        if (island != null && island.IsActive)
        {
            island.Deactivate();
            Debug.Log($"Island '{island.IslandName}' deactivated via manager.");
        }
    }

    // ================================================================================================================
    // Internal Lifecycle Management
    // ================================================================================================================

    /// <summary>
    /// Initializes all currently registered islands. Called once by the manager after all `Awake` calls
    /// (specifically in the manager's `Start()` method). This ensures all islands are available before initialization.
    /// </summary>
    private void InitializeAllIslands()
    {
        if (_isInitialized) return; // Prevent double initialization

        Debug.Log("SkyIslandManager Initializing all registered islands...");
        // Iterate through all registered islands (by type)
        foreach (var island in _islandsByType.Values)
        {
            try
            {
                island.Initialize(this); // Pass a reference to the manager for inter-island communication
                island.Activate(); // Activate the island immediately after initialization
                Debug.Log($"Island '{island.IslandName}' initialized and activated.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize island '{island.IslandName}': {e.Message}");
            }
        }
        _isInitialized = true; // Mark the manager as fully initialized
        Debug.Log("SkyIslandManager: All islands initialized.");
    }

    /// <summary>
    /// Deinitializes all currently registered islands. Called by the manager on application quit or destruction.
    /// This ensures proper cleanup and resource release for all game systems.
    /// </summary>
    private void DeinitializeAllIslands()
    {
        if (!_isInitialized) return; // Only deinitialize if it was previously initialized

        Debug.Log("SkyIslandManager Deinitializing all registered islands...");
        // Create a copy of the list to iterate, in case an island unregisters itself during deinitialization.
        var islandsToDeinitialize = new List<ISkyIsland>(_islandsByType.Values);
        foreach (var island in islandsToDeinitialize)
        {
            try
            {
                if (island.IsActive)
                {
                    island.Deactivate(); // Ensure deactivation before deinitialization
                }
                island.Deinitialize();
                Debug.Log($"Island '{island.IslandName}' deinitialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deinitialize island '{island.IslandName}': {e.Message}");
            }
        }
        _islandsByType.Clear(); // Clear all registries
        _islandsByName.Clear();
        _activeIslands.Clear();
        _isInitialized = false; // Reset initialization flag
        Debug.Log("SkyIslandManager: All islands deinitialized.");
    }
}

// --------------------------------------------------------------------------------------------------------------------
// 3. Concrete SkyIsland Implementations: Example Game Systems (The "Islands")
// --------------------------------------------------------------------------------------------------------------------

/// <summary>
/// Example SkyIsland: Manages the player's inventory.
/// This island demonstrates self-contained logic and data for a game feature.
/// </summary>
[AddComponentMenu("SkyIslandSystem/Inventory Island")]
public class InventoryIsland : MonoBehaviour, ISkyIsland
{
    [Header("Island Configuration")]
    [SerializeField] private string islandName = "InventorySystem";
    public string IslandName => islandName;

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 20;

    private Dictionary<string, int> _items = new Dictionary<string, int>();
    private bool _isActive = false;
    public bool IsActive => _isActive;

    // ================================================================================================================
    // ISkyIsland Interface Implementation
    // ================================================================================================================

    /// <summary>
    /// Initializes the inventory system. Called by SkyIslandManager.
    /// </summary>
    /// <param name="manager">Reference to the SkyIslandManager.</param>
    public void Initialize(SkyIslandManager manager)
    {
        Debug.Log($"'{IslandName}' Initializing. Max Slots: {maxSlots}.");
        _items.Clear();
        // Add some starting items for demonstration purposes
        _items.Add("Gold", 100);
        _items.Add("Health Potion", 2);
    }

    /// <summary>
    /// Deinitializes the inventory system. Called by SkyIslandManager.
    /// </summary>
    public void Deinitialize()
    {
        Debug.Log($"'{IslandName}' Deinitializing. Clearing inventory data.");
        _items.Clear();
    }

    /// <summary>
    /// Activates the inventory system. Enables its functionality.
    /// </summary>
    public void Activate()
    {
        if (_isActive) return; // Prevent double activation
        _isActive = true;
        Debug.Log($"'{IslandName}' Activated. Inventory is now functional.");
        // Example: Potentially enable inventory UI here or register for input events specific to inventory.
    }

    /// <summary>
    /// Deactivates the inventory system. Disables its functionality.
    /// </summary>
    public void Deactivate()
    {
        if (!_isActive) return; // Prevent double deactivation
        _isActive = false;
        Debug.Log($"'{IslandName}' Deactivated. Inventory is not functional.");
        // Example: Potentially disable inventory UI here or unregister from input events.
    }

    /// <summary>
    /// Called by SkyIslandManager's Update loop when active.
    /// </summary>
    public void OnSkyUpdate()
    {
        // Example: If an inventory UI was active, this might update item counts, highlight selected items, etc.
        // For demonstration, we'll keep it quiet to avoid spamming the console.
        // Debug.Log($"'{IslandName}' OnSkyUpdate. Gold: {_items.GetValueOrDefault("Gold", 0)}");
    }

    public void OnSkyFixedUpdate() { /* Physics-related inventory updates, if any (e.g., item physics) */ }
    public void OnSkyLateUpdate() { /* Late updates for inventory, e.g., UI adjustments after character moves */ }

    // ================================================================================================================
    // Public API for InventoryIsland: Methods specific to inventory functionality
    // ================================================================================================================

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="itemName">The name of the item.</param>
    /// <param name="quantity">The quantity to add.</param>
    public void AddItem(string itemName, int quantity)
    {
        if (!_isActive)
        {
            Debug.LogWarning($"'{IslandName}': Cannot add item '{itemName}'. Inventory is deactivated.");
            return;
        }
        if (quantity <= 0) return;

        if (_items.ContainsKey(itemName))
        {
            _items[itemName] += quantity;
        }
        else
        {
            _items.Add(itemName, quantity);
        }
        Debug.Log($"'{IslandName}': Added {quantity} x {itemName}. Current: {_items[itemName]}");
        // Example: Trigger an inventory updated event for UI to react
    }

    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    /// <param name="itemName">The name of the item.</param>
    /// <param name="quantity">The quantity to remove.</param>
    /// <returns>True if items were successfully removed, false otherwise (e.g., not enough items).</returns>
    public bool RemoveItem(string itemName, int quantity)
    {
        if (!_isActive)
        {
            Debug.LogWarning($"'{IslandName}': Cannot remove item '{itemName}'. Inventory is deactivated.");
            return false;
        }
        if (quantity <= 0) return false;

        if (_items.ContainsKey(itemName))
        {
            if (_items[itemName] >= quantity)
            {
                _items[itemName] -= quantity;
                Debug.Log($"'{IslandName}': Removed {quantity} x {itemName}. Remaining: {_items[itemName]}");
                if (_items[itemName] == 0) // Remove item completely if quantity drops to zero
                {
                    _items.Remove(itemName);
                }
                // Example: Trigger an inventory updated event for UI to react
                return true;
            }
            else
            {
                Debug.LogWarning($"'{IslandName}': Not enough {itemName} to remove {quantity}. Has: {_items[itemName]}");
            }
        }
        else
        {
            Debug.LogWarning($"'{IslandName}': Item '{itemName}' not found in inventory.");
        }
        return false;
    }

    /// <summary>
    /// Gets the quantity of a specific item in the inventory.
    /// </summary>
    /// <param name="itemName">The name of the item.</param>
    /// <returns>The quantity of the item, or 0 if not found.</returns>
    public int GetItemCount(string itemName)
    {
        _items.TryGetValue(itemName, out int count);
        return count;
    }

    /// <summary>
    /// Logs the current inventory contents to the console.
    /// </summary>
    public void LogInventory()
    {
        Debug.Log($"'{IslandName}' Current Inventory ({_items.Count} unique items, Active: {_isActive}):");
        if (_items.Count == 0)
        {
            Debug.Log("    (Empty)");
            return;
        }
        foreach (var item in _items)
        {
            Debug.Log($"    - {item.Key}: {item.Value}");
        }
    }

    // ================================================================================================================
    // MonoBehaviour Lifecycle (for registering/unregistering with SkyIslandManager)
    // ================================================================================================================

    private void Awake()
    {
        // Register this island with the SkyIslandManager as early as possible.
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.RegisterIsland(this);
        }
        else
        {
            Debug.LogError($"SkyIslandManager not found! Cannot register '{IslandName}'. " +
                           "Make sure SkyIslandManager GameObject exists and has correct execution order (-1000).");
            enabled = false; // Disable this component if the manager isn't available
        }
    }

    private void OnDestroy()
    {
        // Unregister this island when its GameObject is destroyed.
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.UnregisterIsland(this);
        }
    }
}

/// <summary>
/// Example SkyIsland: Manages player input events.
/// This island demonstrates providing services (events) to other game components, promoting decoupling.
/// </summary>
[AddComponentMenu("SkyIslandSystem/Player Input Island")]
public class PlayerInputIsland : MonoBehaviour, ISkyIsland
{
    [Header("Island Configuration")]
    [SerializeField] private string islandName = "PlayerInputSystem";
    public string IslandName => islandName;

    private bool _isActive = false;
    public bool IsActive => _isActive;

    // Events that other components can subscribe to, to react to player input without
    // directly polling Input.GetKeyDown/GetKey.
    public event Action OnJumpPressed;
    public event Action OnInteractPressed;
    public event Action OnInventoryTogglePressed;

    // ================================================================================================================
    // ISkyIsland Interface Implementation
    // ================================================================================================================

    public void Initialize(SkyIslandManager manager)
    {
        Debug.Log($"'{IslandName}' Initializing.");
        // No specific complex setup needed here; events are ready to be subscribed by clients.
        // If using Unity's new Input System, you might enable/disable an action map here.
    }

    public void Deinitialize()
    {
        Debug.Log($"'{IslandName}' Deinitializing. Clearing event subscribers.");
        // Clear all event subscribers to prevent potential memory leaks when the island is destroyed.
        OnJumpPressed = null;
        OnInteractPressed = null;
        OnInventoryTogglePressed = null;
    }

    public void Activate()
    {
        if (_isActive) return;
        _isActive = true;
        Debug.Log($"'{IslandName}' Activated. Player input is now being processed.");
        // Example: Enable a specific Input System action map.
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;
        Debug.Log($"'{IslandName}' Deactivated. Player input is no longer being processed.");
        // Example: Disable a specific Input System action map.
    }

    /// <summary>
    /// Called by SkyIslandManager's Update loop when active. Checks for input and invokes events.
    /// </summary>
    public void OnSkyUpdate()
    {
        if (!_isActive) return; // Only process input if the island is active

        // Traditional Unity Input checks (can be replaced by new Input System)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpPressed?.Invoke(); // Invoke the Jump event if subscribers exist
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteractPressed?.Invoke(); // Invoke the Interact event
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            OnInventoryTogglePressed?.Invoke(); // Invoke the Inventory Toggle event
        }
    }

    public void OnSkyFixedUpdate() { /* Input checks relevant for physics (e.g., movement vectors), if needed */ }
    public void OnSkyLateUpdate() { /* Late input processing, e.g., camera rotation from mouse input */ }

    // ================================================================================================================
    // MonoBehaviour Lifecycle (for registering/unregistering with SkyIslandManager)
    // ================================================================================================================

    private void Awake()
    {
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.RegisterIsland(this);
        }
        else
        {
            Debug.LogError($"SkyIslandManager not found! Cannot register '{IslandName}'. " +
                           "Make sure SkyIslandManager GameObject exists and has correct execution order.");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.UnregisterIsland(this);
        }
    }
}

/// <summary>
/// Example SkyIsland: Manages quest logic and progress.
/// This island demonstrates inter-island communication by interacting with InventoryIsland to grant rewards.
/// </summary>
[AddComponentMenu("SkyIslandSystem/Quest Island")]
public class QuestIsland : MonoBehaviour, ISkyIsland
{
    [Header("Island Configuration")]
    [SerializeField] private string islandName = "QuestSystem";
    public string IslandName => islandName;

    private Dictionary<string, bool> _activeQuests = new Dictionary<string, bool>();
    private bool _isActive = false;
    public bool IsActive => _isActive;

    // ================================================================================================================
    // ISkyIsland Interface Implementation
    // ================================================================================================================

    public void Initialize(SkyIslandManager manager)
    {
        Debug.Log($"'{IslandName}' Initializing.");
        _activeQuests.Clear();
        // Add some initial quests for demonstration
        _activeQuests.Add("FindAncientRelic", false);
        _activeQuests.Add("DefeatGoblinKing", false);

        // Example: An island could subscribe to another island's events during initialization.
        // PlayerInputIsland inputIsland = manager.GetIsland<PlayerInputIsland>();
        // if (inputIsland != null) {
        //     inputIsland.OnInteractPressed += CheckInteractionForQuestProgress;
        // }
    }

    public void Deinitialize()
    {
        Debug.Log($"'{IslandName}' Deinitializing. Clearing quest data.");
        _activeQuests.Clear();
        // Example: Unsubscribe from events here to prevent memory leaks.
        // PlayerInputIsland inputIsland = SkyIslandManager.Instance?.GetIsland<PlayerInputIsland>();
        // if (inputIsland != null) {
        //     inputIsland.OnInteractPressed -= CheckInteractionForQuestProgress;
        // }
    }

    public void Activate()
    {
        if (_isActive) return;
        _isActive = true;
        Debug.Log($"'{IslandName}' Activated. Quest tracking is now active.");
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;
        Debug.Log($"'{IslandName}' Deactivated. Quest tracking is paused.");
    }

    public void OnSkyUpdate()
    {
        // Example: Check for quest completion conditions, update quest markers, etc.
        // For demonstration, this is left empty.
    }

    public void OnSkyFixedUpdate() { }
    public void OnSkyLateUpdate() { }

    // ================================================================================================================
    // Public API for QuestIsland: Methods specific to quest functionality
    // ================================================================================================================

    /// <summary>
    /// Attempts to complete a quest by ID. If successful, grants a reward using the InventoryIsland.
    /// This is a key demonstration of inter-island communication facilitated by the SkyIslandManager.
    /// </summary>
    /// <param name="questId">The ID of the quest to complete.</param>
    /// <returns>True if the quest was completed, false otherwise.</returns>
    public bool CompleteQuest(string questId)
    {
        if (!_isActive)
        {
            Debug.LogWarning($"'{IslandName}': Cannot complete quest '{questId}'. Quest system is deactivated.");
            return false;
        }

        if (_activeQuests.ContainsKey(questId) && !_activeQuests[questId])
        {
            _activeQuests[questId] = true; // Mark quest as completed
            Debug.Log($"'{IslandName}': Quest '{questId}' completed!");

            // --- Inter-Island Communication Example ---
            // The QuestIsland needs to interact with the InventoryIsland to grant rewards.
            // It does this by requesting the InventoryIsland instance from the SkyIslandManager.
            InventoryIsland inventory = SkyIslandManager.Instance?.GetIsland<InventoryIsland>();
            if (inventory != null)
            {
                // Grant rewards using the InventoryIsland's public API
                switch (questId)
                {
                    case "FindAncientRelic":
                        inventory.AddItem("Ancient Relic", 1);
                        inventory.AddItem("Gold", 250);
                        Debug.Log($"'{IslandName}': Granted rewards for '{questId}': Ancient Relic and 250 Gold.");
                        break;
                    case "DefeatGoblinKing":
                        inventory.AddItem("Goblin King's Crown", 1);
                        inventory.AddItem("Gold", 500);
                        inventory.AddItem("Health Potion", 3);
                        Debug.Log($"'{IslandName}': Granted rewards for '{questId}': Goblin King's Crown, 500 Gold, and 3 Health Potions.");
                        break;
                    default:
                        Debug.Log($"'{IslandName}': No specific rewards defined for quest '{questId}'.");
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"'{IslandName}': Could not find InventoryIsland to grant quest rewards for '{questId}'.");
            }
            // --- End Inter-Island Communication Example ---

            return true;
        }
        else if (_activeQuests.ContainsKey(questId) && _activeQuests[questId])
        {
            Debug.LogWarning($"'{IslandName}': Quest '{questId}' is already completed.");
        }
        else
        {
            Debug.LogWarning($"'{IslandName}': Quest '{questId}' not found or not active.");
        }
        return false;
    }

    /// <summary>
    /// Logs the status of all active quests to the console.
    /// </summary>
    public void LogQuestStatus()
    {
        Debug.Log($"'{IslandName}' Current Quest Status ({_activeQuests.Count} quests, Active: {_isActive}):");
        if (_activeQuests.Count == 0)
        {
            Debug.Log("    (No active quests)");
            return;
        }
        foreach (var quest in _activeQuests)
        {
            Debug.Log($"    - {quest.Key}: {(quest.Value ? "Completed" : "Active")}");
        }
    }

    // ================================================================================================================
    // MonoBehaviour Lifecycle (for registering/unregistering with SkyIslandManager)
    // ================================================================================================================

    private void Awake()
    {
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.RegisterIsland(this);
        }
        else
        {
            Debug.LogError($"SkyIslandManager not found! Cannot register '{IslandName}'. " +
                           "Make sure SkyIslandManager GameObject exists and has correct execution order.");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (SkyIslandManager.Instance != null)
        {
            SkyIslandManager.Instance.UnregisterIsland(this);
        }
    }
}


/// <summary>
/// Example: A client class (e.g., a Player Character or Game State Controller) that uses the SkyIslandSystem.
/// This demonstrates how other parts of the game interact with the modular islands, requesting services
/// and subscribing to events without direct knowledge of the island's implementation details.
/// </summary>
[AddComponentMenu("SkyIslandSystem/Client/Player Controller Client")]
public class PlayerControllerClient : MonoBehaviour
{
    [Header("Client Dependencies")]
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private bool enableInteractionDemo = true; // Toggle for interact actions

    // References to the islands this client needs to interact with.
    // These are obtained from the SkyIslandManager, not assigned directly in the Inspector.
    private PlayerInputIsland _inputIsland;
    private InventoryIsland _inventoryIsland;
    private QuestIsland _questIsland;

    private Rigidbody _rb; // Required for player movement and jumping

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("PlayerControllerClient requires a Rigidbody component on its GameObject.", this);
            enabled = false; // Disable if no Rigidbody is found
        }
    }

    private void Start()
    {
        // It's generally safer to get island references in Start() or OnEnable() because
        // SkyIslandManager's InitializeAllIslands() is called in its Start(), ensuring all
        // islands are fully set up and activated before clients try to use them.

        // Get the PlayerInputIsland and subscribe to its events
        _inputIsland = SkyIslandManager.Instance?.GetIsland<PlayerInputIsland>();
        if (_inputIsland != null)
        {
            _inputIsland.OnJumpPressed += HandleJump;
            _inputIsland.OnInteractPressed += HandleInteract;
            _inputIsland.OnInventoryTogglePressed += HandleInventoryToggle;
            Debug.Log("PlayerControllerClient: Subscribed to PlayerInputIsland events.");
        }
        else
        {
            Debug.LogWarning("PlayerControllerClient: PlayerInputIsland not found. Player input actions will not work.");
        }

        // Get the InventoryIsland and query its initial state
        _inventoryIsland = SkyIslandManager.Instance?.GetIsland<InventoryIsland>();
        if (_inventoryIsland != null)
        {
            Debug.Log($"PlayerControllerClient: Initial Gold: {_inventoryIsland.GetItemCount("Gold")}");
        }
        else
        {
            Debug.LogWarning("PlayerControllerClient: InventoryIsland not found.");
        }

        // Get the QuestIsland and log its initial state
        _questIsland = SkyIslandManager.Instance?.GetIsland<QuestIsland>();
        if (_questIsland != null)
        {
            _questIsland.LogQuestStatus();
        }
        else
        {
            Debug.LogWarning("PlayerControllerClient: QuestIsland not found.");
        }
    }

    private void OnDestroy()
    {
        // Crucially, unsubscribe from events to prevent memory leaks and null reference exceptions
        // if the _inputIsland or this client GameObject is destroyed.
        if (_inputIsland != null)
        {
            _inputIsland.OnJumpPressed -= HandleJump;
            _inputIsland.OnInteractPressed -= HandleInteract;
            _inputIsland.OnInventoryTogglePressed -= HandleInventoryToggle;
            Debug.Log("PlayerControllerClient: Unsubscribed from PlayerInputIsland events.");
        }
    }

    private void FixedUpdate()
    {
        // Example for continuous movement, handled by Rigidbody in FixedUpdate.
        // This relies on Unity's traditional input axes, which could also be abstracted
        // via the PlayerInputIsland if it provided movement vectors.
        if (_inputIsland != null && _inputIsland.IsActive) // Check if input system is active
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, 0, vertical) * playerSpeed * Time.fixedDeltaTime;
            // Apply movement relative to the player's forward direction
            _rb.MovePosition(_rb.position + transform.TransformDirection(movement));
        }
    }

    // ================================================================================================================
    // Event Handlers for PlayerInputIsland events
    // ================================================================================================================

    private void HandleJump()
    {
        Debug.Log("Client: Jump action received! Applying force.");
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void HandleInteract()
    {
        Debug.Log("Client: Interact action received!");
        if (!enableInteractionDemo)
        {
            Debug.Log("Interaction demo is disabled.");
            return;
        }

        // Example: Interact with an item (add potion) via InventoryIsland
        if (_inventoryIsland != null)
        {
            _inventoryIsland.AddItem("Health Potion", 1);
            Debug.Log($"Client: Current Health Potions: {_inventoryIsland.GetItemCount("Health Potion")}");
        }

        // Example: Attempt to complete a quest via QuestIsland
        if (_questIsland != null)
        {
            // Try completing the first quest
            bool relicQuestCompleted = _questIsland.CompleteQuest("FindAncientRelic");
            if (relicQuestCompleted)
            {
                Debug.Log("Client: Quest 'FindAncientRelic' successfully completed!");
                _questIsland.LogQuestStatus();
                _inventoryIsland?.LogInventory(); // Log inventory to see rewards
            }
            else // If first quest is already done or not completable, try another one
            {
                bool goblinQuestCompleted = _questIsland.CompleteQuest("DefeatGoblinKing");
                 if (goblinQuestCompleted) {
                    Debug.Log("Client: Quest 'DefeatGoblinKing' successfully completed!");
                    _questIsland.LogQuestStatus();
                    _inventoryIsland?.LogInventory();
                }
            }
        }
    }

    private void HandleInventoryToggle()
    {
        Debug.Log("Client: Inventory toggle action received!");
        if (_inventoryIsland != null)
        {
            // Toggle the active state of the inventory island
            if (_inventoryIsland.IsActive)
            {
                _inventoryIsland.Deactivate();
            }
            else
            {
                _inventoryIsland.Activate();
            }
            _inventoryIsland.LogInventory(); // Log inventory state after toggle
        }
        else
        {
            Debug.LogWarning("PlayerControllerClient: Cannot toggle inventory. InventoryIsland not found.");
        }
    }
}
```

---

### How to Use in Unity (Setup Steps):

1.  **Create a New C# Script:** In your Unity project, create a new C# script (e.g., right-click in Project window > `Create` > `C# Script`) and name it `SkyIslandSystem`.
2.  **Copy the Code:** Copy the entire C# code provided above and paste it into the `SkyIslandSystem.cs` file, overwriting any existing content.
3.  **Create the Sky Island Manager:**
    *   Create an empty GameObject in your scene (right-click in Hierarchy > `Create Empty`).
    *   Rename it to `_SkyIslandManager` (the underscore helps it appear at the top of the Hierarchy).
    *   Add the `SkyIslandManager` component to this GameObject (select `_SkyIslandManager`, then in the Inspector click `Add Component` and search for `SkyIslandManager`).
4.  **Create the Game Systems (Islands):**
    *   Create another empty GameObject in your scene.
    *   Rename it to `GameSystems`. This will be a parent for your modular systems.
    *   Add the following components to the `GameSystems` GameObject (one by one, using `Add Component`):
        *   `InventoryIsland`
        *   `PlayerInputIsland`
        *   `QuestIsland`
    *   You can inspect each Island component in the Inspector to see its configuration fields (e.g., `Island Name`, `Max Slots` for InventoryIsland).
5.  **Create a Player Character (Client):**
    *   Create a simple 3D object for your player (e.g., right-click in Hierarchy > `3D Object` > `Capsule`).
    *   Rename it to `Player`.
    *   Add a `Rigidbody` component to the `Player` GameObject (required for movement and jumping in the `PlayerControllerClient`).
    *   Add the `PlayerControllerClient` component to the `Player` GameObject.
    *   Ensure the `Player` GameObject has a `Collider` (Capsule usually comes with one) and position it above ground.
6.  **Run the Scene:**
    *   Press the `Play` button in Unity.
    *   Observe the Unity Console. You will see detailed logs about islands being registered, initialized, activated, and their interactions.
    *   **Interact with the Player:**
        *   Use `WASD` or `Arrow Keys` to move the player (based on Unity's default input axes).
        *   Press `Space` to make the player jump (handled by `PlayerInputIsland` and `PlayerControllerClient`).
        *   Press `E` to simulate an "Interact" action. This will:
            *   Add a "Health Potion" to the inventory.
            *   Attempt to complete a quest ("FindAncientRelic", then "DefeatGoblinKing"). Completing a quest will grant rewards (Gold, other items) to the inventory.
        *   Press `I` to toggle the active state of the `InventoryIsland`. When deactivated, `AddItem` or `RemoveItem` calls will fail.

This setup provides a fully functional demonstration of the SkyIslandSystem pattern, allowing you to see its benefits in decoupling, modularity, and centralized management.