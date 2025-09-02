// Unity Design Pattern Example: DynamicLevelLoader
// This script demonstrates the DynamicLevelLoader pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Dynamic Level Loader** design pattern. It enables dynamic loading and unloading of Unity scenes and prefabs, essential for modular game worlds, streaming content, or procedurally generated levels.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System; // For Action delegate

/// <summary>
///     [DynamicLevelLoader Design Pattern Example]
///
///     The Dynamic Level Loader pattern is responsible for asynchronously loading and unloading
///     game content (like scenes, level segments as prefabs, or other assets) during runtime.
///     It decouples the request for content loading from the actual loading mechanism, allowing
///     different parts of the game (e.g., UI, Game Manager, Player) to request a level change
///     without knowing the specifics of *how* that level is loaded.
///
///     Key Benefits of the DynamicLevelLoader Pattern:
///     -   **Decoupling:** Game logic doesn't directly interact with Unity's `SceneManager`
///         or `Resources.Load`. It requests a "level" by identifier (e.g., scene name, prefab path),
///         and the loader handles the implementation details. This makes the system more modular.
///     -   **Asynchronous Loading:** By leveraging Unity's Coroutines (`IEnumerator` with `yield return`),
///         content is loaded over multiple frames. This prevents game freezes and maintains a smooth
///         user experience during transitions, crucial for modern games.
///     -   **Centralized Control:** All level loading/unloading logic resides in one dedicated place.
///         This simplifies management, debugging, and future extensions (e.g., adding loading screens,
///         progress bars, resource pooling, or integrating with Unity Addressables for advanced asset management).
///     -   **Dynamic Content:** Facilitates the loading of different level configurations, procedural chunks,
///         or downloadable content (DLC) on the fly, enabling vast and varied game worlds.
///     -   **Resource Management:** The loader can track loaded assets and instantiated GameObjects,
///         making it easier to perform proper cleanup and optimize memory usage by unloading content
///         that is no longer needed.
///     -   **Event-Driven:** Uses C# events (`Action`) to notify other systems about loading states,
///         allowing for flexible reactions (e.g., UI updates, game state changes) without tight coupling.
///
///     This implementation provides methods for:
///     1.  Asynchronously loading Unity scenes (in `Single` or `Additive` mode).
///     2.  Asynchronously unloading additive Unity scenes.
///     3.  Synchronously instantiating prefabs from `Resources` folders (acting as dynamic level chunks or objects).
///     4.  Unloading dynamically instantiated prefabs.
///     5.  A comprehensive method for cleaning up all dynamic content managed by the loader.
///     6.  Public events for external systems to react to loading completion.
///
///     To use this script:
///     1.  Create an empty GameObject in your *first* scene (e.g., a "Boot" or "Launcher" scene).
///     2.  Name it "DynamicLevelLoader" and attach this script to it.
///     3.  Ensure your desired scenes (e.g., "GameLevel_01", "Environment_Sky") are added to
///         `File -> Build Settings -> Scenes In Build`. This is essential for `SceneManager.LoadSceneAsync` to work.
///     4.  For prefab loading, place your prefabs in a `Resources` folder (e.g., `Assets/Resources/Prefabs/LevelChunkA.prefab`).
///         (For production, Unity's Addressable Asset System is generally recommended over `Resources.Load`
///         for better asset management, memory control, and asynchronous loading of any asset type.)
/// </summary>
public class DynamicLevelLoader : MonoBehaviour
{
    // --- Singleton Instance ---
    // The Singleton pattern ensures that there's only one instance of the DynamicLevelLoader
    // throughout the application's lifetime, and provides a global point of access to it.
    // This allows any script to easily call `DynamicLevelLoader.Instance.LoadSceneAsync(...)`
    // without needing a direct reference.
    public static DynamicLevelLoader Instance { get; private set; }

    // --- Events ---
    // Events are a core part of the observer pattern, promoting loose coupling.
    // Other systems can subscribe to these events to react to loading states
    // without needing direct knowledge of the loader's internal workings.

    /// <summary>
    /// Event fired when a scene load operation begins.
    /// Passes the name of the scene being loaded.
    /// </summary>
    public event Action<string> OnSceneLoadStarted;

    /// <summary>
    /// Event fired when a scene load operation successfully completes.
    /// Passes the name of the scene that was loaded.
    /// </summary>
    public event Action<string> OnSceneLoadCompleted;

    /// <summary>
    /// Event fired when a prefab is successfully instantiated.
    /// Passes the instantiated GameObject.
    /// </summary>
    public event Action<GameObject> OnPrefabLoadCompleted;

    // --- Internal State Tracking ---
    // These lists keep track of content loaded by this manager, enabling proper unloading and management.

    // Stores names of scenes loaded additively, allowing them to be specifically unloaded later.
    private List<string> _loadedAdditiveScenes = new List<string>();

    // Stores references to GameObjects instantiated from prefabs, for easy cleanup.
    private List<GameObject> _instantiatedDynamicPrefabs = new List<GameObject>();

    // --- MonoBehaviour Lifecycle ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and ensures the loader persists across scene changes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance of the DynamicLevelLoader already exists,
            // destroy this duplicate to maintain singleton integrity.
            Destroy(gameObject);
        }
        else
        {
            // Set this instance as the singleton.
            Instance = this;
            // Prevent this GameObject (and its attached script) from being destroyed
            // when new scenes are loaded. This is crucial for a persistent level loader
            // that needs to manage loading across the entire game.
            DontDestroyOnLoad(gameObject);
        }
    }

    // The responsibility of initiating the *first* scene load (e.g., from a launcher scene to GameLevel_01)
    // is typically handled by a dedicated Game Manager or a "Bootstrapper" script, not the loader itself.
    // This makes the DynamicLevelLoader more flexible and less opinionated about the game's initial flow.
    // private void Start() { /* ... Initial loading logic handled by other scripts ... */ }

    // --- Core Loading Methods ---

    /// <summary>
    /// Asynchronously loads a Unity scene. This method is an IEnumerator, allowing it to be
    /// started as a Coroutine, which is Unity's way of handling asynchronous operations
    /// over multiple frames without blocking the main thread.
    /// </summary>
    /// <param name="sceneName">The exact name of the scene to load. This scene MUST be
    ///     listed in `File -> Build Settings -> Scenes In Build`.</param>
    /// <param name="mode">The mode to load the scene.
    ///     `LoadSceneMode.Single`: Unloads all currently loaded scenes and loads this one,
    ///                            making it the new active scene.
    ///     `LoadSceneMode.Additive`: Loads this scene on top of existing scenes, keeping
    ///                               them loaded. Useful for environments, UI, or level chunks.</param>
    /// <param name="onCompleted">An optional callback `Action` invoked when the scene finishes loading.
    ///     Useful for specific logic that needs to run immediately after a scene is ready (e.g.,
    ///     spawning the player, initializing scene-specific managers).</param>
    public IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, Action onCompleted = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("DynamicLevelLoader: Scene name cannot be null or empty for loading.");
            onCompleted?.Invoke(); // Invoke callback even on error for consistent flow.
            yield break; // Exit the coroutine.
        }

        Debug.Log($"DynamicLevelLoader: Loading scene '{sceneName}' in {mode} mode...");
        OnSceneLoadStarted?.Invoke(sceneName); // Notify subscribers that loading has begun.

        // Prevent loading the same additive scene multiple times if it's already tracked.
        if (mode == LoadSceneMode.Additive && _loadedAdditiveScenes.Contains(sceneName))
        {
            Debug.LogWarning($"DynamicLevelLoader: Scene '{sceneName}' is already loaded additively. Skipping re-load.");
            OnSceneLoadCompleted?.Invoke(sceneName); // Still notify completion for consistency.
            onCompleted?.Invoke();
            yield break;
        }

        // Start the asynchronous scene loading operation.
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

        // Crucial: Check if the operation itself failed to start. This happens if the scene
        // name is invalid or the scene is not included in the 'Build Settings'.
        if (operation == null)
        {
            Debug.LogError($"DynamicLevelLoader: Failed to start loading operation for scene '{sceneName}'. " +
                           "Please ensure the scene is added to 'File -> Build Settings -> Scenes In Build'.");
            OnSceneLoadCompleted?.Invoke(sceneName); // Notify completion even on failure for consistency.
            onCompleted?.Invoke();
            yield break;
        }

        // If loading additively, track its name for later management (e.g., unloading).
        if (mode == LoadSceneMode.Additive)
        {
            _loadedAdditiveScenes.Add(sceneName);
        }

        // Wait until the scene loading is truly complete.
        while (!operation.isDone)
        {
            // You can use `operation.progress` (ranges from 0.0 to 0.9) to update a loading bar here.
            // Example: `UIManager.Instance.UpdateLoadingProgress(operation.progress);`
            yield return null; // Wait for the next frame before checking progress again.
        }

        Debug.Log($"DynamicLevelLoader: Scene '{sceneName}' loaded successfully.");
        OnSceneLoadCompleted?.Invoke(sceneName); // Notify subscribers that loading is complete.
        onCompleted?.Invoke(); // Invoke the specific callback provided for this load request.
    }

    /// <summary>
    /// Asynchronously unloads an additive Unity scene.
    /// This method is an IEnumerator, allowing it to be started as a Coroutine.
    /// Note: This method is specifically for scenes loaded with `LoadSceneMode.Additive`.
    /// You cannot "unload" the *active* single scene this way; `LoadSceneMode.Single`
    /// implicitly unloads previous scenes when a new one is loaded.
    /// </summary>
    /// <param name="sceneName">The name of the additive scene to unload.</param>
    /// <param name="onCompleted">An optional callback `Action` invoked when the scene finishes unloading.</param>
    public IEnumerator UnloadSceneAsync(string sceneName, Action onCompleted = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("DynamicLevelLoader: Scene name cannot be null or empty for unloading.");
            onCompleted?.Invoke();
            yield break;
        }

        // Ensure we only try to unload scenes we've tracked as loaded additively.
        if (!_loadedAdditiveScenes.Contains(sceneName))
        {
            Debug.LogWarning($"DynamicLevelLoader: Scene '{sceneName}' is not currently tracked as loaded additively. Cannot unload.");
            onCompleted?.Invoke();
            yield break;
        }

        Debug.Log($"DynamicLevelLoader: Unloading scene '{sceneName}'...");

        // Start the asynchronous scene unloading operation.
        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        // Check if the unloading operation itself failed to start.
        if (operation == null)
        {
            Debug.LogError($"DynamicLevelLoader: Failed to start unloading operation for scene '{sceneName}'.");
            onCompleted?.Invoke();
            yield break;
        }

        // Wait until the scene unloading is complete.
        while (!operation.isDone)
        {
            yield return null; // Wait for the next frame.
        }

        _loadedAdditiveScenes.Remove(sceneName); // Remove the scene from our internal tracking list.
        Debug.Log($"DynamicLevelLoader: Scene '{sceneName}' unloaded successfully.");
        onCompleted?.Invoke();
    }

    /// <summary>
    /// Instantiates a prefab from the `Resources` folder, effectively loading a dynamic level chunk or object.
    /// This is a synchronous operation (meaning `Resources.Load` loads the asset immediately and blocks
    /// the main thread if the asset is large). For very large prefabs or for a production game,
    /// consider using Unity's **Addressable Asset System** for truly asynchronous loading and
    /// better asset management across the entire project.
    /// </summary>
    /// <param name="resourcePath">The path to the prefab within any `Resources` folder in your project
    ///     (e.g., "Prefabs/LevelChunkA" if the prefab is located at `Assets/Resources/Prefabs/LevelChunkA.prefab`).
    ///     Do NOT include the file extension in the path.</param>
    /// <param name="position">The world position where the instantiated prefab should be placed.</param>
    /// <param name="rotation">The world rotation for the instantiated prefab.</param>
    /// <param name="parent">Optional parent Transform for the instantiated prefab in the scene hierarchy.
    ///     If null, it will be instantiated at the root of the scene.</param>
    /// <returns>The instantiated GameObject, or null if loading/instantiation failed (e.g., prefab not found).</returns>
    public GameObject LoadLevelPrefab(string resourcePath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogError("DynamicLevelLoader: Resource path cannot be null or empty for prefab loading.");
            return null;
        }

        // Load the prefab asset from a Resources folder.
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"DynamicLevelLoader: Failed to load prefab from path '{resourcePath}'. " +
                           "Make sure it's in a 'Resources' folder and the path is correct (without extension).");
            return null;
        }

        // Instantiate the prefab in the scene.
        GameObject instance = Instantiate(prefab, position, rotation, parent);
        _instantiatedDynamicPrefabs.Add(instance); // Track the instantiated object for later cleanup.
        Debug.Log($"DynamicLevelLoader: Prefab '{resourcePath}' instantiated successfully as '{instance.name}' at {position}.");
        OnPrefabLoadCompleted?.Invoke(instance); // Notify subscribers that a prefab has been loaded.
        return instance;
    }

    /// <summary>
    /// Destroys all prefabs that were instantiated by this loader and are currently being tracked.
    /// This is important for memory management and cleaning up dynamically added level elements.
    /// </summary>
    public void UnloadAllDynamicPrefabs()
    {
        foreach (GameObject go in _instantiatedDynamicPrefabs)
        {
            if (go != null) // Check if the GameObject still exists in the scene before destroying.
            {
                Destroy(go);
            }
        }
        _instantiatedDynamicPrefabs.Clear(); // Clear the tracking list as all references are now invalid.
        Debug.Log("DynamicLevelLoader: All dynamically loaded prefabs have been unloaded.");
    }

    /// <summary>
    /// Unloads all currently loaded additive scenes and all instantiated prefabs that were
    /// loaded and tracked by this DynamicLevelLoader.
    /// This does NOT unload the current active single scene (e.g., the main game scene).
    /// It's a comprehensive cleanup for all dynamically added content.
    /// </summary>
    public IEnumerator UnloadAllDynamicContent()
    {
        Debug.Log("DynamicLevelLoader: Starting to unload all dynamic content (additive scenes and prefabs)...");

        // Unload all tracked additive scenes.
        // We iterate over a copy of the list (`new List<string>(_loadedAdditiveScenes)`)
        // because the original list (`_loadedAdditiveScenes`) will be modified
        // by `UnloadSceneAsync` as scenes are successfully unloaded.
        List<string> scenesToUnload = new List<string>(_loadedAdditiveScenes);
        foreach (string sceneName in scenesToUnload)
        {
            // Yield until each scene unloading operation is complete.
            yield return StartCoroutine(UnloadSceneAsync(sceneName));
        }

        // Destroy all tracked prefabs.
        UnloadAllDynamicPrefabs();

        Debug.Log("DynamicLevelLoader: All dynamic content has been unloaded.");
    }

    // --- Utility/Helper Methods ---

    /// <summary>
    /// Gets a list of names of all scenes currently loaded additively by this loader.
    /// </summary>
    /// <returns>A new list containing the names of loaded additive scenes.
    ///     Returns a copy to prevent external modification of the internal tracking list.</returns>
    public List<string> GetLoadedAdditiveSceneNames()
    {
        return new List<string>(_loadedAdditiveScenes);
    }

    /// <summary>
    /// Gets a list of all GameObjects that were instantiated from prefabs by this loader.
    /// </summary>
    /// <returns>A new list containing references to instantiated prefabs.
    ///     Returns a copy to prevent external modification of the internal tracking list.</returns>
    public List<GameObject> GetInstantiatedPrefabs()
    {
        return new List<GameObject>(_instantiatedDynamicPrefabs);
    }
}

/*
    ========================================
    [  DYNAMICLEVELLOADER - EXAMPLE USAGE  ]
    ========================================

    This section demonstrates how you would typically interact with the DynamicLevelLoader
    from other scripts (e.g., a GameManager, LevelSelector, or PlayerController) in your Unity project.

    To use this example:
    1.  Create a new C# script named "MyGameManager".
    2.  Copy the `MyGameManager` class below into `MyGameManager.cs`, replacing its default content.
    3.  Create an empty GameObject in your scene (e.g., "GameManager") and attach `MyGameManager.cs` to it.
    4.  Create some test scenes:
        -   Add "GameLevel_01" to your `Build Settings`.
        -   Add "Environment_Sky" to your `Build Settings`.
        -   Add "GameUI" to your `Build Settings`.
        (These can be empty scenes for testing purposes, but ensure they exist in Build Settings.)
    5.  Create some test prefabs:
        -   Create a folder named `Resources` anywhere in your Assets folder (e.g., `Assets/Resources/`).
        -   Inside `Resources`, create another folder `Prefabs` (`Assets/Resources/Prefabs/`).
        -   Create two simple prefabs (e.g., a Cube and a Sphere), name them "LevelChunkA" and "LevelChunkB",
            and drag them into the `Assets/Resources/Prefabs/` folder.
    6.  In the Unity Inspector, select your "GameManager" GameObject and assign the public string fields
        (`initialGameScene`, `environmentScene`, `uiScene`, `levelChunkAPath`, `levelChunkBPath`)
        with the exact names/paths you used.
    7.  You can also create UI Buttons (Canvas -> Button) and link their `OnClick()` events
        to the `OnUnloadAllButtonClick` and `OnUnloadEnvironmentButtonClick` methods of your
        "GameManager" GameObject for interactive testing.
*/

public class MyGameManager : MonoBehaviour
{
    [Header("Scene Names (Must be in Build Settings)")]
    [Tooltip("The main game scene to load first.")]
    public string initialGameScene = "GameLevel_01";
    [Tooltip("An additive scene for environmental details.")]
    public string environmentScene = "Environment_Sky";
    [Tooltip("An additive scene for the game's user interface.")]
    public string uiScene = "GameUI";

    [Header("Prefab Paths (Must be in a Resources folder)")]
    [Tooltip("Path to a prefab acting as a level segment A.")]
    public string levelChunkAPath = "Prefabs/LevelChunkA"; // e.g., Assets/Resources/Prefabs/LevelChunkA.prefab
    [Tooltip("Path to a prefab acting as a level segment B.")]
    public string levelChunkBPath = "Prefabs/LevelChunkB"; // e.g., Assets/Resources/Prefabs/LevelChunkB.prefab

    private void Start()
    {
        // --- Option 1: Load an initial game scene (single mode) ---
        // This is typically the first "real" game scene loaded after a launcher or boot scene.
        // `LoadSceneMode.Single` will unload any currently active scene before loading the new one.
        Debug.Log("MyGameManager: Initiating initial scene load...");
        DynamicLevelLoader.Instance.StartCoroutine(
            DynamicLevelLoader.Instance.LoadSceneAsync(initialGameScene, LoadSceneMode.Single, () =>
            {
                Debug.Log($"MyGameManager: '{initialGameScene}' loaded and ready! Now loading additive content...");
                // Once the main game scene is loaded, we can proceed to add more content additively.
                LoadAdditiveContent();
            })
        );

        // --- Option 2: Subscribe to global events for more generic responses ---
        // Subscribing to events is a powerful way for different systems (e.g., UI, Analytics, PlayerSpawner)
        // to react to level loading states without knowing who initiated the load.
        DynamicLevelLoader.Instance.OnSceneLoadStarted += HandleSceneLoadStarted;
        DynamicLevelLoader.Instance.OnSceneLoadCompleted += HandleSceneLoadCompleted;
        DynamicLevelLoader.Instance.OnPrefabLoadCompleted += HandlePrefabLoadCompleted;
    }

    private void OnDestroy()
    {
        // It's crucial to unsubscribe from events to prevent memory leaks (null reference exceptions)
        // if the `DynamicLevelLoader` outlives this `MyGameManager` object.
        if (DynamicLevelLoader.Instance != null)
        {
            DynamicLevelLoader.Instance.OnSceneLoadStarted -= HandleSceneLoadStarted;
            DynamicLevelLoader.Instance.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
            DynamicLevelLoader.Instance.OnPrefabLoadCompleted -= HandlePrefabLoadCompleted;
        }
    }

    /// <summary>
    /// Demonstrates loading additional scenes and prefabs additively, after the main scene is ready.
    /// </summary>
    private void LoadAdditiveContent()
    {
        // --- Load scenes additively (e.g., UI, environment details, or modular level parts) ---
        // These scenes will load on top of the active `initialGameScene`.
        DynamicLevelLoader.Instance.StartCoroutine(
            DynamicLevelLoader.Instance.LoadSceneAsync(environmentScene, LoadSceneMode.Additive, () =>
            {
                Debug.Log($"MyGameManager: Additive scene '{environmentScene}' loaded.");
                // Perhaps configure environment settings or enable specific effects here.
            })
        );

        DynamicLevelLoader.Instance.StartCoroutine(
            DynamicLevelLoader.Instance.LoadSceneAsync(uiScene, LoadSceneMode.Additive, () =>
            {
                Debug.Log($"MyGameManager: Additive scene '{uiScene}' loaded.");
                // Maybe find and initialize a UI controller script in this new scene.
            })
        );

        // --- Load prefabs as dynamic level chunks or interactive elements ---
        // These will be instantiated into the currently active scene.
        GameObject chunk1 = DynamicLevelLoader.Instance.LoadLevelPrefab(levelChunkAPath, new Vector3(0, 0, 0), Quaternion.identity);
        if (chunk1 != null) {
            Debug.Log($"MyGameManager: Instantiated dynamic level chunk '{chunk1.name}' at (0,0,0).");
            // Perform post-instantiation setup if needed, e.g., placing enemies within the chunk.
        }

        GameObject chunk2 = DynamicLevelLoader.Instance.LoadLevelPrefab(levelChunkBPath, new Vector3(50, 0, 0), Quaternion.identity);
        if (chunk2 != null) {
            Debug.Log($"MyGameManager: Instantiated dynamic level chunk '{chunk2.name}' at (50,0,0).");
        }
    }

    // --- Example of a button click or game event triggering content unloading ---

    /// <summary>
    /// Callable method to unload all content managed by the DynamicLevelLoader (additive scenes and prefabs).
    /// Typically triggered by a "Return to Main Menu" or "End Level" event.
    /// </summary>
    public void OnUnloadAllButtonClick()
    {
        Debug.Log("MyGameManager: User requested to unload all dynamic content...");
        DynamicLevelLoader.Instance.StartCoroutine(
            DynamicLevelLoader.Instance.UnloadAllDynamicContent()
        );
    }

    /// <summary>
    /// Callable method to specifically unload the environment scene.
    /// Demonstrates granular control over unloading additive content.
    /// </summary>
    public void OnUnloadEnvironmentButtonClick()
    {
        Debug.Log($"MyGameManager: User requested to unload additive scene '{environmentScene}'...");
        DynamicLevelLoader.Instance.StartCoroutine(
            DynamicLevelLoader.Instance.UnloadSceneAsync(environmentScene, () => {
                Debug.Log($"MyGameManager: '{environmentScene}' explicitly unloaded.");
            })
        );
    }

    // --- Event Handlers (examples of how other scripts can react to loader events) ---

    private void HandleSceneLoadStarted(string sceneName)
    {
        Debug.Log($"MyGameManager (Event Listener): Loading of scene '{sceneName}' has started.");
        // This is an ideal place to:
        // - Display a loading screen or spinner UI.
        // - Pause game input or physics.
    }

    private void HandleSceneLoadCompleted(string sceneName)
    {
        Debug.Log($"MyGameManager (Event Listener): Scene '{sceneName}' finished loading.");
        // This is an ideal place to:
        // - Hide the loading screen.
        // - Initialize scene-specific managers or objects.
        // - Unpause game input/physics.
        if (sceneName == uiScene)
        {
            // Example: Find a specific UI element in the newly loaded UI scene and enable it.
            // var mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
            // if (mainCanvas != null) mainCanvas.enabled = true;
        }
    }

    private void HandlePrefabLoadCompleted(GameObject instance)
    {
        Debug.Log($"MyGameManager (Event Listener): Prefab '{instance.name}' finished loading and was instantiated.");
        // This is an ideal place to:
        // - Perform additional setup on the newly spawned prefab (e.g., activate components, link to other systems).
        // - Register the prefab with a game state manager.
    }
}


/*
    ========================================
    [  FURTHER CONSIDERATIONS / ADVANCED USAGE  ]
    ========================================

    -   **Loading Progress UI:** The `AsyncOperation.progress` property (which ranges from 0.0 to 0.9)
        can be used within the `while (!operation.isDone)` loop in `LoadSceneAsync` to update a visual
        loading bar or percentage display on a loading screen.
    -   **Scene Activation Control:** `AsyncOperation.allowSceneActivation` can be set to `false`
        after starting a scene load. This keeps the scene at 90% loaded (and un-activated) until
        you explicitly set `allowSceneActivation` back to `true`. This gives precise control over
        when the new scene actually becomes visible and its `Awake`/`Start` methods are called,
        useful for synchronization or seamless transitions.
    -   **Unity Addressable Asset System:** For large-scale projects, the Addressable Asset System
        is Unity's recommended solution for dynamic asset management. It replaces `Resources.Load`
        and provides highly optimized, asynchronous loading for *any* asset type (not just prefabs
        in `Resources` folders), remote content delivery, and robust memory management.
        The `DynamicLevelLoader` pattern can be easily adapted to use Addressables instead.
        e.g., `Addressables.LoadSceneAsync("MyAddressableSceneKey")` or `Addressables.InstantiateAsync("MyAddressablePrefabKey")`.
    -   **Level Data ScriptableObjects:** Instead of just using raw strings for scene or prefab names,
        you could define a custom `ScriptableObject` (e.g., `LevelConfig`) that holds all
        data related to a specific level or level segment (e.g., scene names to load, prefabs to instantiate,
        initial player spawn points, environmental settings, difficulty, required assets).
        This makes level definition more robust, artist-friendly, and easier to manage.
    -   **Loading Screen Management:** The `OnSceneLoadStarted` and `OnSceneLoadCompleted` events
        are perfectly suited for a dedicated `LoadingScreenManager` script to listen to. This manager
        would be responsible for displaying and hiding loading UI elements, playing animations,
        and perhaps showing dynamic tips or lore during loading.
    -   **Error Handling and Fallbacks:** More robust error handling might include retries, fallback
        content, or detailed user feedback if loading fails.
*/
```