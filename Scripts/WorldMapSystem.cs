// Unity Design Pattern Example: WorldMapSystem
// This script demonstrates the WorldMapSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'WorldMapSystem' design pattern in Unity. This pattern provides a structured way to manage different regions (or zones/levels) of your game world, allowing for efficient loading, unloading, and transitions between them. It centralizes world management, decoupling it from individual region logic.

We'll create four interconnected scripts:
1.  **`RegionData.cs`**: A `ScriptableObject` to define properties of each world region.
2.  **`WorldMapSystem.cs`**: The core manager (Singleton) that handles loading/unloading regions and tracks the player's current region.
3.  **`RegionPortal.cs`**: A `MonoBehaviour` placed in a scene that acts as a gateway to another region.
4.  **`PlayerController.cs`**: A basic player script that interacts with portals and responds to region changes.

---

### **1. `RegionData.cs`**
(Create this as `RegionData.cs` in your Assets folder)

This `ScriptableObject` defines the properties of a single region in your game world. It allows designers to create new regions without writing code.

```csharp
using UnityEngine;
using System.Collections.Generic; // Not strictly needed for this SO, but good practice for other SOs

/// <summary>
/// RegionData ScriptableObject
/// Defines a single region (or zone/level) in the game world.
/// This acts as a blueprint for each navigable area.
/// </summary>
[CreateAssetMenu(fileName = "NewRegionData", menuName = "WorldMapSystem/Region Data")]
public class RegionData : ScriptableObject
{
    [Header("Region Identification")]
    [Tooltip("A unique, user-friendly name for this region.")]
    public string regionName = "New Region";

    [Tooltip("The exact name of the Unity scene file associated with this region. " +
             "Ensure this scene is added to File -> Build Settings.")]
    public string sceneName = "NewRegionScene";

    [Header("Player Spawn Points")]
    [Tooltip("The tag of a GameObject in this region's scene where the player should spawn " +
             "when entering this region from another. E.g., 'SpawnPoint_EntranceA'.")]
    public string defaultPlayerSpawnPointTag = "PlayerSpawnPoint";

    [Header("Description")]
    [TextArea(3, 5)]
    [Tooltip("A brief description of this region for internal use or debugging.")]
    public string description = "A new game world region.";

    /// <summary>
    /// Gets the unique identifier for this region.
    /// In this simple example, we use the sceneName as a unique ID,
    /// but a GUID could be used for more robust systems.
    /// </summary>
    public string RegionID => sceneName;

    // Optional: Add other region-specific data here, e.g.:
    // public Sprite worldMapIcon;
    // public AudioClip ambientMusic;
    // public List<EnemySpawnGroup> enemyGroups;
    // public List<QuestMarker> questsInRegion;

    private void OnValidate()
    {
        // Ensure sceneName is not empty, as it's crucial for loading
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"RegionData '{name}' has an empty Scene Name. " +
                             $"Please set a valid scene name for region loading.", this);
        }
    }
}
```

---

### **2. `WorldMapSystem.cs`**
(Create this as `WorldMapSystem.cs` in your Assets folder)

This is the central manager for your world. It's a Singleton, meaning there's only one instance throughout the game. It handles loading and unloading scenes additively, updating the current region, and notifying other systems of region changes.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// WorldMapSystem Design Pattern
///
/// This script implements the core logic for the WorldMapSystem design pattern.
/// It acts as a centralized manager (Singleton) for handling different regions
/// (scenes) in your game world.
///
/// Key responsibilities:
/// 1.  **Region Management:** Loads and unloads regions (Unity scenes) additively.
/// 2.  **Current Region Tracking:** Keeps track of the region the player is currently in.
/// 3.  **Event Notification:** Notifies other systems when the current region changes.
/// 4.  **Player Spawning:** Positions the player correctly when entering a new region.
/// </summary>
public class WorldMapSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    private static WorldMapSystem _instance;
    public static WorldMapSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance
                _instance = FindObjectOfType<WorldMapSystem>();

                if (_instance == null)
                {
                    // If none exists, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject(typeof(WorldMapSystem).Name);
                    _instance = singletonObject.AddComponent<WorldMapSystem>();
                }
            }
            return _instance;
        }
    }

    // --- Public Events ---
    /// <summary>
    /// Event fired when the player's current region changes.
    /// Subscribers can react to new region data (e.g., update UI, load region-specific resources).
    /// </summary>
    public static event Action<RegionData> OnRegionChanged;

    // --- Editor-Configurable Properties ---
    [Header("World Map Configuration")]
    [Tooltip("The initial region to load when the game starts.")]
    [SerializeField] private RegionData _startingRegion;

    [Tooltip("A list of all possible regions in your game. " +
             "Useful for debugging, world map UI, or pre-loading data.")]
    [SerializeField] private List<RegionData> _allRegions = new List<RegionData>();

    [Header("Player Settings")]
    [Tooltip("The tag used to identify the player GameObject in the scene.")]
    [SerializeField] private string _playerTag = "Player";

    // --- Private State ---
    private RegionData _currentRegion;
    private bool _isLoadingRegion = false; // To prevent multiple simultaneous loads

    /// <summary>
    /// Public getter for the current active region.
    /// </summary>
    public RegionData CurrentRegion => _currentRegion;

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Keep the system alive across scene loads

        // Subscribe to scene events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Start()
    {
        // Load the starting region if one is defined and no region is currently loaded.
        // This handles cases where the game starts directly from the main game scene
        // or a loading scene.
        if (_startingRegion != null && _currentRegion == null && SceneManager.sceneCount <= 1)
        {
            Debug.Log($"WorldMapSystem: Initializing and loading starting region: {_startingRegion.regionName}");
            LoadRegion(_startingRegion);
        }
        else if (_startingRegion == null)
        {
            Debug.LogWarning("WorldMapSystem: No starting region assigned. Please assign one in the Inspector.", this);
        }
        else if (_currentRegion == null && SceneManager.sceneCount > 1)
        {
            // This might happen if you have a persistent 'Manager' scene and then load a game scene
            // as the initial 'current' region. We need to identify it.
            Scene activeGameScene = GetFirstGameScene();
            if (activeGameScene.IsValid())
            {
                RegionData initialRegion = _allRegions.Find(r => r.sceneName == activeGameScene.name);
                if (initialRegion != null)
                {
                    Debug.Log($"WorldMapSystem: Detected initial region from loaded scene: {initialRegion.regionName}");
                    _currentRegion = initialRegion;
                    OnRegionChanged?.Invoke(_currentRegion);
                    PositionPlayerInRegion(_currentRegion);
                }
                else
                {
                    Debug.LogWarning($"WorldMapSystem: Scene '{activeGameScene.name}' is loaded but no matching RegionData found. " +
                                     $"Please ensure all game scenes have corresponding RegionData assets.");
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // --- Core WorldMapSystem Functionality ---
    /// <summary>
    /// Initiates the loading process for a new region.
    /// This is the primary method for changing regions.
    /// </summary>
    /// <param name="targetRegion">The RegionData asset of the region to load.</param>
    public void LoadRegion(RegionData targetRegion)
    {
        if (targetRegion == null)
        {
            Debug.LogError("WorldMapSystem: Attempted to load a null region data.", this);
            return;
        }

        if (_isLoadingRegion)
        {
            Debug.LogWarning($"WorldMapSystem: Already loading a region. Request to load {targetRegion.regionName} ignored.", this);
            return;
        }

        if (_currentRegion == targetRegion)
        {
            Debug.Log($"WorldMapSystem: Player is already in region: {targetRegion.regionName}. No action needed.", this);
            return;
        }

        Debug.Log($"WorldMapSystem: Attempting to load region: {targetRegion.regionName} (Scene: {targetRegion.sceneName})");
        _isLoadingRegion = true;
        StartCoroutine(LoadRegionAsync(targetRegion));
    }

    /// <summary>
    /// Asynchronously loads the target region and unloads the current region.
    /// </summary>
    private IEnumerator LoadRegionAsync(RegionData targetRegion)
    {
        // 1. Unload the current region (if any)
        if (_currentRegion != null)
        {
            Debug.Log($"WorldMapSystem: Unloading current region: {_currentRegion.regionName} (Scene: {_currentRegion.sceneName})");
            // Check if the scene is actually loaded before trying to unload
            if (SceneManager.GetSceneByName(_currentRegion.sceneName).IsValid() && SceneManager.GetSceneByName(_currentRegion.sceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_currentRegion.sceneName);
                Debug.Log($"WorldMapSystem: Successfully unloaded scene: {_currentRegion.sceneName}");
            }
            else
            {
                Debug.LogWarning($"WorldMapSystem: Attempted to unload {_currentRegion.sceneName}, but it was not loaded or valid.", this);
            }
        }

        // 2. Load the new region additively
        Debug.Log($"WorldMapSystem: Loading target region: {targetRegion.regionName} (Scene: {targetRegion.sceneName})");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetRegion.sceneName, LoadSceneMode.Additive);

        // Check if the scene exists in build settings
        if (asyncLoad == null)
        {
            Debug.LogError($"WorldMapSystem: Failed to start loading scene '{targetRegion.sceneName}'. " +
                           $"Make sure it's added to 'File > Build Settings'.", this);
            _isLoadingRegion = false;
            yield break; // Exit early if loading failed
        }

        while (!asyncLoad.isDone)
        {
            // Update loading progress (e.g., for a loading screen)
            // Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");
            yield return null;
        }

        Debug.Log($"WorldMapSystem: Successfully loaded scene: {targetRegion.sceneName}");

        // 3. Update internal state and notify subscribers
        _currentRegion = targetRegion;
        _isLoadingRegion = false;

        // Set the newly loaded scene as the active scene for lighting and rendering
        // (Optional, but often desirable for proper rendering setup)
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetRegion.sceneName));
        
        Debug.Log($"WorldMapSystem: Current region is now: {_currentRegion.regionName}");

        // 4. Position the player in the new region
        PositionPlayerInRegion(_currentRegion);

        // 5. Fire the event
        OnRegionChanged?.Invoke(_currentRegion);
    }

    /// <summary>
    /// Finds a GameObject with the specified tag in the current region's scene
    /// and positions the player GameObject there.
    /// </summary>
    /// <param name="region">The RegionData for the current region.</param>
    private void PositionPlayerInRegion(RegionData region)
    {
        GameObject player = GameObject.FindWithTag(_playerTag);
        if (player == null)
        {
            Debug.LogWarning($"WorldMapSystem: Player GameObject with tag '{_playerTag}' not found. Cannot position player.", this);
            return;
        }

        Transform spawnPoint = null;
        if (!string.IsNullOrWhiteSpace(region.defaultPlayerSpawnPointTag))
        {
            GameObject spawnObject = GameObject.FindWithTag(region.defaultPlayerSpawnPointTag);
            if (spawnObject != null)
            {
                spawnPoint = spawnObject.transform;
            }
            else
            {
                Debug.LogWarning($"WorldMapSystem: Spawn point with tag '{region.defaultPlayerSpawnPointTag}' not found in scene '{region.sceneName}'. " +
                                 $"Player might be in the wrong position.", this);
            }
        }

        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            Debug.Log($"WorldMapSystem: Player moved to spawn point '{region.defaultPlayerSpawnPointTag}' in region '{region.regionName}'.");
        }
        else
        {
            // Fallback: If no specific spawn point, player might stay at previous location
            // or we could enforce a default (0,0,0) or last known position.
            Debug.Log($"WorldMapSystem: No specific spawn point for player in region '{region.regionName}'. Player position unchanged.");
        }
    }

    /// <summary>
    /// Helper to find the first valid game scene, excluding the persistent manager scene itself.
    /// </summary>
    private Scene GetFirstGameScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            // Assuming the WorldMapSystem (and its scene) is not considered a "game region" scene.
            // Adjust this logic if your persistent manager is in a specific scene name.
            if (scene.name != gameObject.scene.name && scene.isLoaded) 
            {
                return scene;
            }
        }
        return new Scene(); // Return an invalid scene if no game scene is found
    }

    // --- SceneManager Callbacks (for debugging/monitoring) ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"WorldMapSystem: Scene '{scene.name}' loaded (Mode: {mode}).");
        // You could add logic here, e.g., enabling/disabling GameObjects specific to a region
        // if they're not handled by the scene load process itself.
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"WorldMapSystem: Scene '{scene.name}' unloaded.");
        // You could add logic here, e.g., clearing region-specific caches.
    }
}
```

---

### **3. `RegionPortal.cs`**
(Create this as `RegionPortal.cs` in your Assets folder)

This `MonoBehaviour` is placed in a scene and acts as a trigger that, when entered by the player, tells the `WorldMapSystem` to load a new region.

```csharp
using UnityEngine;

/// <summary>
/// RegionPortal
///
/// This script defines a physical gateway within a region (scene) that,
/// when triggered by the player, initiates a transition to a different region
/// via the WorldMapSystem.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a Collider component is present
public class RegionPortal : MonoBehaviour
{
    [Header("Portal Configuration")]
    [Tooltip("The RegionData asset for the region this portal leads to.")]
    [SerializeField] private RegionData _targetRegion;

    [Tooltip("The tag of the GameObject expected to trigger this portal (e.g., 'Player').")]
    [SerializeField] private string _playerTag = "Player";

    private Collider _portalCollider; // Reference to our collider

    private void Awake()
    {
        _portalCollider = GetComponent<Collider>();
        if (_portalCollider == null)
        {
            Debug.LogError("RegionPortal: Collider component missing! " +
                           "This script requires a Collider to function as a trigger.", this);
            enabled = false; // Disable script if no collider
            return;
        }

        // Ensure the collider is set as a trigger
        if (!_portalCollider.isTrigger)
        {
            Debug.LogWarning($"RegionPortal: Collider on '{name}' is not set as a Trigger. " +
                             $"Setting it to Trigger automatically.", this);
            _portalCollider.isTrigger = true;
        }

        if (_targetRegion == null)
        {
            Debug.LogError($"RegionPortal: Target RegionData is not assigned on '{name}'. " +
                           "This portal will not function correctly.", this);
            enabled = false; // Disable script if no target region
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider belongs to the player
        if (other.CompareTag(_playerTag))
        {
            Debug.Log($"RegionPortal: Player entered portal '{name}'. Requesting load of region: {_targetRegion.regionName}.");
            
            // Request the WorldMapSystem to load the target region
            // This is the core interaction point with the WorldMapSystem.
            if (WorldMapSystem.Instance != null)
            {
                WorldMapSystem.Instance.LoadRegion(_targetRegion);
            }
            else
            {
                Debug.LogError("RegionPortal: WorldMapSystem.Instance is null. Is the WorldMapSystem initialized?", this);
            }
        }
    }

    // Optional: Add gizmos for better visualization in the editor
    private void OnDrawGizmos()
    {
        if (_portalCollider == null)
        {
            _portalCollider = GetComponent<Collider>();
        }

        if (_portalCollider != null)
        {
            Gizmos.color = Color.cyan;
            // Draw a wireframe representation of the trigger
            if (_portalCollider is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_portalCollider is SphereCollider sphere)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            // Add other collider types if needed
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix
        }

        if (_targetRegion != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"To: {_targetRegion.regionName}");
        }
    }
}
```

---

### **4. `PlayerController.cs` (Example Player)**
(Create this as `PlayerController.cs` in your Assets folder)

A very basic player script. It moves the player and subscribes to the `OnRegionChanged` event to demonstrate how other systems react to region transitions.

```csharp
using UnityEngine;

/// <summary>
/// PlayerController (Example)
///
/// This is a simple player script demonstrating movement and how to react
/// to the WorldMapSystem's OnRegionChanged event.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Movement speed of the player.")]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("The tag assigned to this player GameObject. Must match WorldMapSystem's player tag.")]
    [SerializeField] private string _playerTag = "Player";

    private void Awake()
    {
        // Ensure the player has the correct tag for the WorldMapSystem and RegionPortals
        if (!gameObject.CompareTag(_playerTag))
        {
            Debug.LogWarning($"Player GameObject '{name}' does not have the tag '{_playerTag}'. " +
                             $"WorldMapSystem and RegionPortals might not detect it correctly. " +
                             $"Please set the GameObject's tag to '{_playerTag}'.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the region changed event
        WorldMapSystem.OnRegionChanged += OnRegionChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        WorldMapSystem.OnRegionChanged -= OnRegionChanged;
    }

    private void Update()
    {
        // Basic player movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        transform.Translate(moveDirection * _moveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Callback method when the WorldMapSystem's OnRegionChanged event is fired.
    /// This demonstrates how other game systems can react to region transitions.
    /// </summary>
    /// <param name="newRegion">The RegionData of the newly loaded region.</param>
    private void OnRegionChanged(RegionData newRegion)
    {
        Debug.Log($"PlayerController: Player entered new region: {newRegion.regionName}. " +
                  $"Welcome to {newRegion.description}");

        // Example: Update UI elements based on new region
        // UIManager.Instance.UpdateRegionName(newRegion.regionName);
        // MapSystem.Instance.HighlightRegionOnMap(newRegion);

        // Note: The WorldMapSystem itself handles player positioning.
        // If you needed specific player logic *after* positioning, it would go here.
    }
}
```

---

### **How to Implement in Unity (Example Usage):**

**Project Setup:**

1.  **Create a Folder Structure:**
    *   `Assets/Scripts/WorldMapSystem/` (for the C# files)
    *   `Assets/Regions/` (for `RegionData` assets)
    *   `Assets/Scenes/` (for your game scenes)

2.  **Add Scenes to Build Settings:** Go to `File > Build Settings...` and drag all your region scenes (e.g., `StartRegion`, `ForestRegion`, `CaveRegion`) into the "Scenes In Build" list. This is crucial for `SceneManager.LoadSceneAsync` to work.

**Step-by-Step Implementation:**

**A. Create Region Scenes:**

1.  Create a few new Unity scenes (e.g., `StartRegion.unity`, `ForestRegion.unity`, `CaveRegion.unity`).
2.  In each scene:
    *   Add a simple 3D object (e.g., Cube, Plane) to represent the ground.
    *   Add a **Player Spawn Point**: Create an empty GameObject, name it `PlayerSpawnPoint` (or whatever you use for `defaultPlayerSpawnPointTag`), and position it where the player should appear in that scene.
    *   (Optional but recommended) Add a light source if your scene is dark.

**B. Create RegionData Assets:**

1.  In your `Assets/Regions/` folder, right-click -> `Create` -> `WorldMapSystem` -> `Region Data`.
2.  Create three `RegionData` assets, for example:
    *   **`RD_StartRegion`**:
        *   `Region Name`: `Starting Area`
        *   `Scene Name`: `StartRegion` (must match scene file name exactly!)
        *   `Default Player Spawn Point Tag`: `PlayerSpawnPoint`
        *   `Description`: `The peaceful beginning of your journey.`
    *   **`RD_ForestRegion`**:
        *   `Region Name`: `Whispering Forest`
        *   `Scene Name`: `ForestRegion`
        *   `Default Player Spawn Point Tag`: `PlayerSpawnPoint`
        *   `Description`: `A dense forest known for ancient trees.`
    *   **`RD_CaveRegion`**:
        *   `Region Name`: `Dark Cave`
        *   `Scene Name`: `CaveRegion`
        *   `Default Player Spawn Point Tag`: `PlayerSpawnPoint`
        *   `Description`: `A mysterious cave with hidden treasures.`

**C. Setup the WorldMapSystem:**

1.  Create an empty GameObject in your **initial scene** (e.g., `StartRegion`), name it `WorldMapSystemManager`.
2.  Add the `WorldMapSystem.cs` script to this GameObject.
3.  In the Inspector of `WorldMapSystemManager`:
    *   Drag `RD_StartRegion` to the `Starting Region` slot.
    *   Drag all your `RegionData` assets (`RD_StartRegion`, `RD_ForestRegion`, `RD_CaveRegion`) into the `All Regions` list.
    *   Ensure `Player Tag` is set to `Player`.

**D. Create the Player:**

1.  In your `StartRegion` scene, create a 3D Capsule GameObject.
2.  Rename it to `Player`.
3.  Add a `CharacterController` component (or a `Rigidbody` + `Collider` if you prefer physics-based movement).
4.  **Crucially**, set its **Tag** to `Player` (you might need to add this tag first: `Tags & Layers` dropdown -> `Add Tag...`).
5.  Add the `PlayerController.cs` script to the `Player` GameObject.
6.  Position the `Player` at the `PlayerSpawnPoint` in `StartRegion`.

**E. Create RegionPortals:**

1.  In your `StartRegion` scene, create an empty GameObject, name it `PortalToForest`.
2.  Add a `BoxCollider` component to it. Set `Is Trigger` to `true`. Adjust its size and position to form an archway or a designated exit.
3.  Add the `RegionPortal.cs` script to `PortalToForest`.
4.  In the Inspector, drag `RD_ForestRegion` to the `Target Region` slot.
5.  Repeat this process in `ForestRegion.unity`:
    *   Create `PortalToCave`: `Target Region` = `RD_CaveRegion`.
    *   Create `PortalToStart`: `Target Region` = `RD_StartRegion`.
6.  Repeat for `CaveRegion.unity` (e.g., a `PortalToForest` leading back).

**Testing:**

1.  Open the `StartRegion.unity` scene.
2.  Run the game.
3.  You should see your player capsule.
4.  Move the player into the `PortalToForest` trigger.
5.  The `ForestRegion` scene should load, and the `StartRegion` scene should unload (you'll see debug logs). The player should appear at the `PlayerSpawnPoint` in the `ForestRegion`.
6.  Navigate to other portals to test transitions between all regions.

---

**Educational Value & Practical Use Cases:**

*   **Decoupling:** `RegionData` decouples region definitions from code, allowing designers to manage world structure easily. `RegionPortal` decouples transition logic from core player movement.
*   **Centralized Control:** `WorldMapSystem` is a single point of truth for world state, making it easier to manage and extend.
*   **Scalability:** Adding new regions involves creating a scene, a `RegionData` asset, and a `RegionPortal`. No modification to core logic is needed.
*   **Event-Driven:** The `OnRegionChanged` event allows any other system (UI, audio, quest manager, enemy AI) to react dynamically to changes in the player's location without direct dependencies on the `WorldMapSystem` itself.
    *   **Example:** A UI script could subscribe to `OnRegionChanged` to update a "Current Location" display.
    *   **Example:** An audio manager could fade out current ambient music and fade in new music based on the `RegionData`'s `ambientMusic` field (if added).
    *   **Example:** A minimap system could update its displayed map based on the `newRegion`.
*   **Additive Scene Loading:** This pattern leverages Unity's additive scene loading, which is crucial for open-world or hub-and-spoke game architectures to efficiently manage memory and performance by loading only relevant parts of the world.
*   **Persistence:** The `DontDestroyOnLoad` on `WorldMapSystem` ensures it persists across scene changes, maintaining its state and enabling continuous world management.