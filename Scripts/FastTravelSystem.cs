// Unity Design Pattern Example: FastTravelSystem
// This script demonstrates the FastTravelSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'FastTravelSystem' isn't a traditional Gang of Four design pattern but rather a common game mechanic that can be implemented using several patterns, such as **Singleton** (for the manager), **Scriptable Objects** (for data definition), and **Observer/Events** (for decoupled communication).

This example provides a robust, practical implementation for a Fast Travel System in Unity.

**Key Components & Their Roles:**

1.  **`FastTravelPointSO` (ScriptableObject):**
    *   **Role:** Data container. Defines the properties of a single fast travel location (ID, display name, target position, target scene).
    *   **Pattern:** Leverages Unity's Scriptable Objects for easily creatable and reusable data assets in the editor, separating data from logic.

2.  **`FastTravelManager` (MonoBehaviour, Singleton):**
    *   **Role:** The central hub for the entire system. Manages all registered fast travel points and orchestrates the fast travel process.
    *   **Pattern:** Implements a Singleton pattern (`FastTravelManager.Instance`) for global, easy access from anywhere in your game. It also uses C# events (`Action`) for an **Observer** pattern, allowing other systems (like UI or a loading screen manager) to react to fast travel events without direct dependencies. It handles scene loading for inter-scene fast travel.

3.  **`FastTravelTrigger` (MonoBehaviour):**
    *   **Role:** World-placed component that allows players to "discover" or "activate" a fast travel point.
    *   **Pattern:** Acts as a view or controller for a `FastTravelPointSO` in the world. When a player enters its trigger, it registers its associated `FastTravelPointSO` with the `FastTravelManager`.

---

## 1. FastTravelPointSO.cs

This ScriptableObject defines the data for a fast travel destination.

```csharp
using UnityEngine;

/// <summary>
/// A ScriptableObject defining a single fast travel location.
/// This asset holds the data for a fast travel point, allowing it to be
/// easily created and configured in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewFastTravelPoint", menuName = "Fast Travel/Fast Travel Point")]
public class FastTravelPointSO : ScriptableObject
{
    [Tooltip("A unique identifier for this fast travel point (e.g., 'town_square', 'mountain_peak_outpost'). " +
             "Used internally by the system to reference locations.")]
    public string id;

    [Tooltip("The user-friendly name displayed in UI (e.g., 'The Grand Town Square', 'Rocky Mountain Camp').")]
    public string displayName;

    [Tooltip("The target world position where the player will be moved.")]
    public Vector3 position;

    [Tooltip("The name of the scene where this fast travel point exists. If fast traveling from a different scene, " +
             "this scene will be loaded. The FastTravelTrigger will automatically update this to its current scene name.")]
    public string targetSceneName;

    [Tooltip("Optional: An icon to display in the fast travel UI for visual representation.")]
    public Sprite icon;

    // A simple validation to remind developers to set an ID in the editor.
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"Fast Travel Point '{name}' has no ID. Please assign a unique ID to avoid issues.");
        }
    }
}
```

---

## 2. FastTravelManager.cs

This is the core of the system, managing all fast travel logic and locations.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// The central manager for the Fast Travel System.
/// This MonoBehaviour should be present in your scene (ideally a persistent one)
/// and manages registration of fast travel points, and executes the fast travel operation.
/// It uses a Singleton pattern to be easily accessible from anywhere.
/// </summary>
public class FastTravelManager : MonoBehaviour
{
    // Singleton instance for global access to the Fast Travel Manager.
    // This allows any script to call FastTravelManager.Instance.FastTravel(...)
    public static FastTravelManager Instance { get; private set; }

    [Tooltip("Assign the Transform of the player character that needs to be moved during fast travel. " +
             "Alternatively, the player can call SetPlayerTransform() at runtime, or ensure the player " +
             "GameObject has the tag 'Player' for dynamic lookup after scene loads.")]
    [SerializeField] private Transform _playerTransform;

    // A dictionary to store all currently registered fast travel locations, indexed by their unique ID.
    // This provides quick lookup for fast travel operations.
    private Dictionary<string, FastTravelPointSO> _registeredLocations = new Dictionary<string, FastTravelPointSO>();

    // --- Events for decoupled communication ---
    // These events allow other systems (e.g., UI, loading screen manager, game state manager)
    // to react to fast travel operations without having direct dependencies on the FastTravelManager.
    public static event Action<string, FastTravelPointSO> OnFastTravelStarted;     // Fired when fast travel begins.
    public static event Action<string, FastTravelPointSO> OnFastTravelCompleted;    // Fired when fast travel finishes (player moved).
    public static event Action<string> OnFastTravelFailed;                         // Fired if fast travel encounters an error.
    public static event Action<FastTravelPointSO> OnLocationRegistered;           // Fired when a new fast travel location is registered.
    public static event Action<string> OnLocationUnregistered;                     // Fired when a fast travel location is unregistered.

    // --- Singleton Initialization ---
    private void Awake()
    {
        // Implement the basic Singleton pattern:
        // Ensure only one instance of FastTravelManager exists at any time.
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this duplicate.
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Ensure this manager persists across scene loads.
            // This is crucial for managing fast travel between different scenes.
            DontDestroyOnLoad(gameObject);
        }
    }

    // --- Scene Loading Callbacks ---
    // We register for scene loaded events to handle fast travel that involves loading a new scene.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called by Unity's SceneManager AFTER a scene has completely loaded.
    /// This method checks if a fast travel was initiated across scenes and completes it.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if there's a pending fast travel target stored.
        // PlayerPrefs is used here as a simple, cross-scene persistent storage mechanism.
        // For more robust save data or complex inter-scene communication, consider a dedicated GameDataManager.
        if (PlayerPrefs.HasKey("FastTravelTargetID"))
        {
            string targetId = PlayerPrefs.GetString("FastTravelTargetID");
            PlayerPrefs.DeleteKey("FastTravelTargetID"); // Clear the key immediately to prevent re-triggering

            if (_registeredLocations.TryGetValue(targetId, out FastTravelPointSO targetLocation))
            {
                // If the player transform became null (e.g., player was destroyed and respawned in the new scene),
                // try to find it again. This assumes the player GameObject has the tag "Player".
                if (_playerTransform == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        _playerTransform = playerObj.transform;
                    }
                    else
                    {
                        Debug.LogError("Player Transform is null after scene load and cannot be found by tag 'Player'. Fast travel failed.");
                        OnFastTravelFailed?.Invoke($"Player not found in new scene '{scene.name}'.");
                        return;
                    }
                }

                // Crucial check: only perform travel if the currently loaded scene matches the target scene defined in the SO.
                // This prevents accidental warping if the key somehow persisted or a different scene was loaded.
                if (scene.name == targetLocation.targetSceneName)
                {
                    PerformTravel(targetLocation); // Execute the actual player movement
                    OnFastTravelCompleted?.Invoke(targetId, targetLocation); // Notify listeners that fast travel is complete
                }
                else
                {
                    Debug.LogWarning($"Fast travel target ID '{targetId}' was set, but the loaded scene '{scene.name}' does not match " +
                                     $"the target scene '{targetLocation.targetSceneName}' defined in the FastTravelPointSO. Aborting travel.");
                    OnFastTravelFailed?.Invoke("Scene mismatch during post-load fast travel.");
                }
            }
            else
            {
                Debug.LogError($"Fast travel target ID '{targetId}' was set, but the location is not registered after scene load. " +
                               $"This might happen if the FastTravelPointSO was not loaded or registered in the new scene. Fast travel failed.");
                OnFastTravelFailed?.Invoke($"Target location '{targetId}' not registered after scene load.");
            }
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Registers a FastTravelPoint with the system, making it available for future fast travel.
    /// If a point with the same ID already exists, its data (e.g., position, scene name) will be updated.
    /// </summary>
    /// <param name="location">The ScriptableObject defining the fast travel point.</param>
    public void RegisterLocation(FastTravelPointSO location)
    {
        if (location == null || string.IsNullOrEmpty(location.id))
        {
            Debug.LogError("Attempted to register a null or invalid FastTravelPointSO (ID is missing or null).");
            return;
        }

        if (_registeredLocations.ContainsKey(location.id))
        {
            // Update existing entry if a point with the same ID is re-registered.
            // This can happen if, for example, a scene containing the point is reloaded,
            // or a trigger updates its position dynamically.
            Debug.Log($"Fast Travel Location with ID '{location.id}' already registered. Updating existing entry with new data.");
            _registeredLocations[location.id] = location; 
        }
        else
        {
            _registeredLocations.Add(location.id, location);
            Debug.Log($"Fast Travel Location '{location.displayName}' (ID: {location.id}) registered.");
        }
        OnLocationRegistered?.Invoke(location); // Notify listeners that a new location is available.
    }

    /// <summary>
    /// Unregisters a FastTravelPoint from the system.
    /// This might be used if a point becomes unavailable (e.g., destroyed, story progression disables it).
    /// </summary>
    /// <param name="locationId">The unique ID of the fast travel point to unregister.</param>
    public void UnregisterLocation(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogError("Attempted to unregister a null or empty Fast Travel Location ID.");
            return;
        }

        if (_registeredLocations.Remove(locationId))
        {
            Debug.Log($"Fast Travel Location with ID '{locationId}' unregistered.");
            OnLocationUnregistered?.Invoke(locationId); // Notify listeners.
        }
        else
        {
            Debug.LogWarning($"Attempted to unregister Fast Travel Location with ID '{locationId}', but it was not found in registered locations.");
        }
    }

    /// <summary>
    /// Gets a list of all currently registered fast travel locations.
    /// This is typically used by a UI system to populate a fast travel map or list.
    /// </summary>
    /// <returns>A new List containing all registered FastTravelPointSO objects.</returns>
    public List<FastTravelPointSO> GetAllAvailableLocations()
    {
        return new List<FastTravelPointSO>(_registeredLocations.Values);
    }

    /// <summary>
    /// Initiates a fast travel operation to the specified location.
    /// This is the primary method called by UI or game logic to perform a fast travel.
    /// </summary>
    /// <param name="locationId">The unique ID of the target fast travel point.</param>
    public void FastTravel(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogError("Cannot fast travel: provided location ID is null or empty.");
            OnFastTravelFailed?.Invoke("Invalid location ID.");
            return;
        }

        // Try to retrieve the target location from the registered points.
        if (!_registeredLocations.TryGetValue(locationId, out FastTravelPointSO targetLocation))
        {
            Debug.LogError($"Fast Travel Location with ID '{locationId}' not found in registered locations. Cannot fast travel.");
            OnFastTravelFailed?.Invoke($"Location '{locationId}' not found.");
            return;
        }

        // Ensure the player transform is assigned before attempting to move.
        if (_playerTransform == null)
        {
            Debug.LogError("Player Transform is not assigned in FastTravelManager. Cannot perform fast travel.");
            OnFastTravelFailed?.Invoke("Player not found or assigned.");
            return;
        }

        Debug.Log($"Initiating fast travel to '{targetLocation.displayName}' (ID: {targetLocation.id})...");
        OnFastTravelStarted?.Invoke(locationId, targetLocation); // Notify listeners that fast travel has begun

        // Check if scene loading is required.
        // If the target scene is different from the current active scene, load the new scene asynchronously.
        if (!string.IsNullOrEmpty(targetLocation.targetSceneName) && SceneManager.GetActiveScene().name != targetLocation.targetSceneName)
        {
            Debug.Log($"Loading scene: {targetLocation.targetSceneName}...");
            // Store the target location ID using PlayerPrefs. This data will be retrieved by OnSceneLoaded
            // after the new scene is active, allowing the player to be moved to the correct spot.
            PlayerPrefs.SetString("FastTravelTargetID", locationId);
            SceneManager.LoadScene(targetLocation.targetSceneName);
            // The actual player movement will be completed in OnSceneLoaded after the scene fully loads.
        }
        else
        {
            // If fast traveling within the same scene, perform the movement immediately.
            PerformTravel(targetLocation);
            OnFastTravelCompleted?.Invoke(locationId, targetLocation); // Notify listeners that fast travel is complete
        }
    }

    /// <summary>
    /// Performs the actual movement of the player to the target location.
    /// This method handles setting player position and potentially resetting physics states.
    /// </summary>
    /// <param name="targetLocation">The FastTravelPointSO defining the destination.</param>
    private void PerformTravel(FastTravelPointSO targetLocation)
    {
        if (_playerTransform != null)
        {
            // Set the player's position to the target location's position.
            _playerTransform.position = targetLocation.position;
            
            // Optionally, reset player's velocity or other physics states
            // to prevent unexpected movement after warp, especially with Rigidbody or CharacterController.
            Rigidbody rb = _playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            CharacterController cc = _playerTransform.GetComponent<CharacterController>();
            if (cc != null)
            {
                // For CharacterController, simply setting position might cause issues with collision/grounding.
                // A common workaround is to temporarily disable it, set position, then re-enable.
                // Example:
                // cc.enabled = false;
                // _playerTransform.position = targetLocation.position;
                // cc.enabled = true;
                // For this example, direct position setting is shown, but be aware of CharacterController nuances.
            }

            Debug.Log($"Player fast traveled to '{targetLocation.displayName}' at {targetLocation.position}.");
        }
        else
        {
            Debug.LogError("Player Transform is null. Cannot perform travel.");
        }
    }

    /// <summary>
    /// Utility method to set the player transform dynamically at runtime.
    /// Useful if the player character is instantiated after the manager's Awake lifecycle.
    /// </summary>
    /// <param name="player">The transform of the player character.</param>
    public void SetPlayerTransform(Transform player)
    {
        _playerTransform = player;
        Debug.Log($"FastTravelManager: Player Transform set to {player.name}.");
    }
}
```

---

## 3. FastTravelTrigger.cs

This component is placed in the world to define interactive fast travel points.

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // Required for Handles.Label in OnDrawGizmos to display text in editor
#endif

/// <summary>
/// A MonoBehaviour placed in the world that represents an interactable
/// point for the Fast Travel System. When activated (e.g., by player collision),
/// it registers its associated FastTravelPointSO with the FastTravelManager.
/// </summary>
[RequireComponent(typeof(Collider))] // Requires a Collider component to detect triggers
public class FastTravelTrigger : MonoBehaviour
{
    [Tooltip("The FastTravelPointSO asset defining this fast travel point. Create these via 'Assets/Create/Fast Travel/Fast Travel Point'.")]
    public FastTravelPointSO fastTravelPoint;

    [Tooltip("If true, this point is automatically registered with the FastTravelManager when the trigger GameObject starts active.")]
    public bool autoRegisterOnStart = true;

    [Tooltip("If true, this point is automatically unregistered when the trigger is disabled or destroyed. " +
             "Useful for points that are temporarily unavailable or removed.")]
    public bool autoUnregisterOnDisableOrDestroy = true;

    [Tooltip("For debug/testing: If true, will register the point on Start() even if autoRegisterOnStart is false, " +
             "bypassing player detection. Helps with quick setup in editor.")]
    public bool debugRegisterOnStart = false;

    private void Start()
    {
        // Ensure the attached collider is configured as a trigger to detect overlaps without physics simulation.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"FastTravelTrigger on '{gameObject.name}' requires a Collider component.", this);
            enabled = false; // Disable script if no collider to prevent further errors.
            return;
        }

        // Auto-register logic based on inspector settings.
        if (autoRegisterOnStart || debugRegisterOnStart)
        {
            RegisterThisLocation();
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// Used here to detect when the player "discovers" or "activates" a fast travel point.
    /// </summary>
    /// <param name="other">The Collider that entered this trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider belongs to the player.
        // It's good practice to use a specific tag (e.g., "Player") for reliable detection.
        if (other.CompareTag("Player"))
        {
            RegisterThisLocation();
        }
    }

    /// <summary>
    /// Registers this trigger's associated FastTravelPointSO with the FastTravelManager.
    /// This method also dynamically updates the SO's position and target scene name
    /// to match the trigger's current world location and scene. This is useful for
    /// placing FastTravelPointSOs once and having their in-world coordinates set automatically.
    /// </summary>
    private void RegisterThisLocation()
    {
        if (fastTravelPoint == null)
        {
            Debug.LogError($"FastTravelTrigger on '{gameObject.name}' has no FastTravelPointSO assigned! Cannot register.", this);
            return;
        }
        if (FastTravelManager.Instance == null)
        {
            Debug.LogWarning($"FastTravelManager not found in the scene. Fast travel point '{fastTravelPoint.displayName}' " +
                             $"on '{gameObject.name}' cannot be registered. Make sure FastTravelManager is active and accessible.", this);
            return;
        }

        // Update the FastTravelPointSO with the trigger's current world position and the scene it's in.
        // This ensures the fast travel point's data is always current with its physical location in the game world.
        fastTravelPoint.position = transform.position;
        fastTravelPoint.targetSceneName = gameObject.scene.name;

        FastTravelManager.Instance.RegisterLocation(fastTravelPoint);
    }

    /// <summary>
    /// Called when the GameObject is disabled.
    /// If configured, it will unregister the fast travel point from the manager.
    /// </summary>
    private void OnDisable()
    {
        UnregisterThisLocation();
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// This provides a final chance to unregister if OnDisable wasn't called (e.g., scene unload).
    /// </summary>
    private void OnDestroy()
    {
        UnregisterThisLocation();
    }

    /// <summary>
    /// Unregisters this trigger's associated FastTravelPointSO from the FastTravelManager.
    /// </summary>
    private void UnregisterThisLocation()
    {
        if (autoUnregisterOnDisableOrDestroy && fastTravelPoint != null && !string.IsNullOrEmpty(fastTravelPoint.id))
        {
            FastTravelManager.Instance?.UnregisterLocation(fastTravelPoint.id);
        }
    }

    // --- Editor-only visualization ---
    // These methods provide visual feedback in the Unity Editor, but are compiled out of builds.
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Visualize the trigger in the editor with a sphere to show its approximate area.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f); 

        // Draw a fast-forward icon for better visibility and context.
        Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "d_FastForward@2x", true, Color.cyan);

        // Display the fast travel point's display name above the trigger.
        if (fastTravelPoint != null && !string.IsNullOrEmpty(fastTravelPoint.displayName))
        {
            Handles.Label(transform.position + Vector3.up * 2f, fastTravelPoint.displayName, EditorStyles.boldLabel);
        }
        else
        {
            Handles.Label(transform.position + Vector3.up * 1f, "No FastTravelPointSO!", EditorStyles.boldLabel);
        }

        // Draw a warning cube if no FastTravelPointSO is assigned, indicating a setup error.
        if (fastTravelPoint == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
#endif
}
```

---

## Example Usage Guide (How to Implement in Unity)

This section provides a step-by-step guide on how to set up and use the Fast Travel System in your Unity project using the provided scripts.

### 1. Setup the `FastTravelManager`

1.  **Create a Manager GameObject:**
    *   In your Unity project, create an empty GameObject (e.g., named "GameManagers" or "PersistentObjects").
    *   Attach the `FastTravelManager.cs` script to this GameObject.
2.  **Player Transform Assignment:**
    *   **Crucial:** In the Inspector for the `FastTravelManager`, drag your player character's `Transform` component into the `Player Transform` field.
    *   **Alternatively (for dynamic players):** If your player character is instantiated at runtime, or if it exists in a separate scene that loads after your manager, ensure your player's `Awake` or `Start` method calls `FastTravelManager.Instance.SetPlayerTransform(this.transform);`. Also, make sure your player GameObject has the tag "Player" so the manager can find it after a scene load.
3.  **Persistence:** It's highly recommended to place the `FastTravelManager` in a 'persistent' scene (e.g., a "Core" or "Bootstrap" scene) that loads first and remains loaded throughout your game. The `DontDestroyOnLoad` call in its `Awake` method ensures it persists across scene changes.

### 2. Create Fast Travel Points (ScriptableObjects)

1.  **Create Assets:**
    *   In your Project window, right-click -> Create -> Fast Travel -> Fast Travel Point.
    *   Give this new asset a descriptive name (e.g., "FT_TownSquare", "FT_MountainCamp").
2.  **Configure in Inspector:**
    *   For each `FastTravelPointSO` asset you create:
        *   **`ID`**: Set a unique identifier (e.g., `"town_square"`, `"mountain_camp_01"`). This is the key used by the `FastTravelManager`.
        *   **`Display Name`**: Set a user-friendly name (e.g., `"The Grand Town Square"`, `"Rocky Mountain Camp"`). This is what players will see in your UI.
        *   **`Position`**: You can often leave this at `(0,0,0)` initially. The `FastTravelTrigger` (see next step) will dynamically update this to its own world position when the point is registered. If you want to define a specific, fixed position for fast travel independent of a trigger, you can set it here.
        *   **`Target Scene Name`**: Leave this empty initially. The `FastTravelTrigger` will automatically set this to the name of the scene it's in when registered. If you're manually creating a fast travel point that exists in a specific scene but isn't tied to a trigger, you would manually enter the scene name here.
        *   **`Icon`**: (Optional) Assign a `Sprite` asset here if you want a visual icon for this fast travel point in your UI.

### 3. Place Fast Travel Triggers in your Scenes

1.  **Create a Trigger GameObject:**
    *   In your scene, create an empty GameObject (e.g., named "FastTravelTrigger_TownEntrance").
    *   Add a **Collider** component to it (e.g., a Box Collider, Sphere Collider).
    *   **Crucial:** Ensure the `Is Trigger` checkbox on the Collider component **is checked**.
2.  **Attach Script:**
    *   Attach the `FastTravelTrigger.cs` script to this GameObject.
3.  **Assign Fast Travel Point SO:**
    *   In the `FastTravelTrigger` Inspector, drag and drop one of your created `FastTravelPointSO` assets (e.g., "FT_TownSquare") into the `Fast Travel Point` field.
4.  **Position and Configure:**
    *   Position and scale the `FastTravelTrigger` GameObject where you want the player to "discover" or activate this fast travel point in your world.
    *   When the player (whose GameObject has the "Player" tag) enters this trigger, the `FastTravelPointSO` will be registered with the `FastTravelManager`. The trigger will also update the `FastTravelPointSO`'s `position` and `targetSceneName` to its own current transform and scene.
    *   Adjust `Auto Register On Start` and `Auto Unregister On Disable Or Destroy` as needed for your game's logic. `debugRegisterOnStart` is useful for quick testing.
5.  **Repeat:** Repeat these steps for all desired fast travel locations across all your scenes.

### 4. Player Setup

1.  **Player Tag:** Ensure your player character GameObject has the tag **"Player"**. This is used by `FastTravelTrigger` to identify the player and by `FastTravelManager` to potentially find the player after a scene load.
2.  **Player Collider/Rigidbody:** Your player character GameObject should have a Collider (e.g., CharacterController, Capsule Collider) and potentially a Rigidbody (if using physics-based movement) for `OnTriggerEnter` to work correctly.

### 5. Implement a Fast Travel UI (Conceptual Example)

You'll need a UI to let the player choose a destination. Here's a conceptual outline:

1.  **UI Canvas Setup:** Create a UI Canvas in your game. Add elements like a panel for the fast travel map/list, a button to open it, and a scrollable list area to display locations.
2.  **UI Script (Example `FastTravelUI.cs`):**
    *   Create a script (e.g., `FastTravelUI.cs`) and attach it to your UI Canvas or a dedicated UI manager.
    *   **Open UI:** When the player presses a hotkey (e.g., 'M') or interacts with a map, activate your fast travel UI panel.
    *   **Populate List:**
        ```csharp
        // In your FastTravelUI script when opening the panel:
        List<FastTravelPointSO> availableLocations = FastTravelManager.Instance.GetAllAvailableLocations();

        // Clear existing UI elements and create new buttons/entries for each available location.
        foreach (FastTravelPointSO location in availableLocations)
        {
            // Create a UI button/element for 'location'.
            // Display location.displayName and optionally location.icon.
            // When the button is clicked:
            // FastTravelManager.Instance.FastTravel(location.id);
        }
        ```
    *   **Listen to Events (for Loading Screens/Feedback):**
        ```csharp
        // In your FastTravelUI (or a LoadingScreenManager) Start/OnEnable method:
        void Start()
        {
            FastTravelManager.OnFastTravelStarted += OnFastTravelStarted;
            FastTravelManager.OnFastTravelCompleted += OnFastTravelCompleted;
            FastTravelManager.OnFastTravelFailed += OnFastTravelFailed;
            // ... your other setup
        }

        void OnDestroy() // Important: Unsubscribe to prevent memory leaks!
        {
            if (FastTravelManager.Instance != null)
            {
                FastTravelManager.OnFastTravelStarted -= OnFastTravelStarted;
                FastTravelManager.OnFastTravelCompleted -= OnFastTravelCompleted;
                FastTravelManager.OnFastTravelFailed -= OnFastTravelFailed;
            }
        }

        private void OnFastTravelStarted(string id, FastTravelPointSO location)
        {
            Debug.Log($"Fast travel to {location.displayName} started. Showing loading screen...");
            // Your logic to display a loading screen, disable player input, etc.
        }

        private void OnFastTravelCompleted(string id, FastTravelPointSO location)
        {
            Debug.Log($"Fast travel to {location.displayName} completed. Hiding loading screen.");
            // Your logic to hide the loading screen, re-enable player input, etc.
        }

        private void OnFastTravelFailed(string reason)
        {
            Debug.LogError($"Fast travel failed: {reason}. Displaying error message.");
            // Your logic to display an error message to the player.
            // Also hide loading screen if it was shown.
        }
        ```

### 6. Scene Setup for Multi-Scene Fast Travel

1.  **Build Settings:** Go to `File -> Build Settings...`.
2.  **Add Scenes:** Drag and drop all your relevant scenes (e.g., your "PersistentManagers" scene, "TownScene", "MountainScene", etc.) into the "Scenes In Build" list. Ensure your persistent scene (containing `FastTravelManager`) is listed at the top (index 0).

By following these steps, you'll have a fully functional and extensible Fast Travel System ready for your Unity project!