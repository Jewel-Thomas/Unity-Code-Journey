// Unity Design Pattern Example: HotReloadPatterns
// This script demonstrates the HotReloadPatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the "Hot Reload Patterns" design pattern in Unity. This pattern focuses on how to manage and preserve application state across Unity's domain reloads, which occur during script recompilation (hot reloading) or when entering/exiting Play Mode in the editor.

The core challenge addressed is that C# static fields and non-serialized private fields on MonoBehaviours are reset during a domain reload, leading to loss of runtime state. This pattern provides strategies to gracefully handle this.

**Key Components & Strategies Demonstrated:**

1.  **`ScriptableObject` for Persistent Asset Data:**
    *   `GlobalGameSettings`: Shows how to store data that needs to persist across *all* editor sessions and play sessions, as it's an asset.

2.  **`[RuntimeInitializeOnLoadMethod]` for Early Initialization and Singleton Re-establishment:**
    *   Used in `GameDataManager` to ensure a static singleton is re-initialized and its state restored *very early* in the application lifecycle after a domain reload, even before `Awake()` calls. This is crucial for singletons whose static fields are reset.

3.  **`UnityEditor.SessionState` for Editor-specific, Hot-Reload Persistent Data:**
    *   Used in `GameDataManager` to save and restore complex runtime state (like `ItemsCollectedCount` and `LastVisitedTimestamp`) that should persist across hot reloads *within the same editor session*. This data is cleared when the editor closes.

4.  **`UnityEditor.EditorApplication.playModeStateChanged` for Pre-Reload State Saving:**
    *   Used in `GameDataManager` to detect when Unity is about to perform a domain reload (e.g., `ExitingPlayMode` or `ExitingEditMode`) and save the current runtime state *before* it's lost.

5.  **`[SerializeField]` for MonoBehaviour Field Persistence:**
    *   Standard Unity practice, but essential for MonoBehaviours. It ensures that fields marked `[SerializeField]` (or public fields) have their values stored by Unity and automatically restored after a domain reload.

**Scenario:**
We simulate a game where:
*   `GlobalGameSettings` stores unchanging game configurations (e.g., player starting health).
*   `GameDataManager` is a static singleton that tracks runtime progress (e.g., items collected, last interaction time) and needs this data to survive script recompilations during play.
*   `HotReloadManager` is a MonoBehaviour in the scene that orchestrates and displays this information.

---

### **1. `GlobalGameSettings.cs` (ScriptableObject)**

This `ScriptableObject` holds global game settings. Data stored here persists as an asset, meaning it's saved to disk and survives editor restarts, script recompilations, and play mode changes. It's the most robust way to store static, configuration-like data.

```csharp
// File: Assets/Scripts/GlobalGameSettings.cs
using UnityEngine;

/// <summary>
/// A ScriptableObject to hold global game settings.
/// Data in ScriptableObjects persists as an asset, surviving editor restarts,
/// script recompilations, and play mode changes.
/// This is the most robust way to store global configuration data.
/// </summary>
[CreateAssetMenu(fileName = "GlobalGameSettings", menuName = "HotReloadPatterns/Global Game Settings", order = 1)]
public class GlobalGameSettings : ScriptableObject
{
    [Tooltip("Player's starting health. This value persists permanently as an asset.")]
    public int PlayerStartingHealth = 100;

    [Tooltip("Prefix for level names. This value persists permanently as an asset.")]
    public string LevelPrefix = "Area_";

    [Tooltip("A global game speed multiplier. Value is stored in the asset.")]
    public float GameSpeedMultiplier = 1.0f;

    /// <summary>
    /// ScriptableObjects also receive OnEnable/OnDisable calls during domain reloads.
    /// This is a good place for validation or caching data from this asset.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"GlobalGameSettings: OnEnable called. Player Health: {PlayerStartingHealth}, Level Prefix: {LevelPrefix}");
    }
}
```

---

### **2. `GameDataManager.cs` (Static/Singleton Class with Hot Reload Persistence)**

This class demonstrates how to manage a static, non-MonoBehaviour singleton's state such that it survives Unity's domain reloads (script recompilations) within the same editor session.

*   **`[RuntimeInitializeOnLoadMethod]`**: Ensures the singleton is re-established and its state restored very early after a domain reload.
*   **`UnityEditor.SessionState`**: Stores the manager's state temporarily across reloads in the editor.
*   **`UnityEditor.EditorApplication.playModeStateChanged`**: Used to save the state *before* a domain reload occurs.

```csharp
// File: Assets/Scripts/GameDataManager.cs
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A static/singleton class that manages game data which needs to persist
/// across Unity's domain reloads (script recompilations) within the same editor session.
/// </summary>
public class GameDataManager
{
    // A unique key for storing/retrieving our state from SessionState.
    private const string SessionStateKey = "HotReloadPattern_GameDataManagerState";

    // --- Private Static Instance (Singleton Pattern) ---
    private static GameDataManager _instance;

    /// <summary>
    /// Public static property to access the singleton instance.
    /// It includes a fallback initialization, though the primary initialization
    /// should happen via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // This scenario should ideally be rare if RuntimeInitializeOnLoadMethod
                // works as expected (runs very early). This serves as a defensive fallback.
                Debug.LogWarning("GameDataManager.Instance accessed before explicit initialization. Forcing initialization.");
                _instance = new GameDataManager();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    // --- Internal State (These fields would be lost on hot reload without this pattern) ---
    public int ItemsCollectedCount { get; private set; }
    public DateTime LastVisitedTimestamp { get; private set; }

    /// <summary>
    /// Private constructor to enforce the singleton pattern.
    /// State initialization should happen in Initialize() to allow for restoration.
    /// </summary>
    private GameDataManager() { }

    /// <summary>
    /// This method is crucial for re-establishing the singleton and restoring its state
    /// after a domain reload (e.g., script recompilation or entering play mode).
    /// RuntimeInitializeLoadType.SubsystemRegistration ensures it runs very early,
    /// before any MonoBehaviour's Awake() methods.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeOnLoad()
    {
        Debug.Log("GameDataManager: Initializing On Load (SubsystemRegistration).");
        
        // Re-create the instance as static fields are reset during domain reload.
        _instance = new GameDataManager();
        _instance.Initialize();

        // In the Unity Editor, we need to handle saving state *before* a domain reload occurs.
        // This subscription ensures SaveState() is called at the correct time.
#if UNITY_EDITOR
        // Unsubscribe first to prevent double-subscription issues if called multiple times.
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    /// <summary>
    /// The main initialization logic for the manager. Attempts to restore state.
    /// </summary>
    private void Initialize()
    {
        // Try to restore state from SessionState first.
        if (!RestoreState())
        {
            // If no previous state was found (e.g., first time starting play mode),
            // initialize with default values.
            Debug.Log("GameDataManager: No previous state found, initializing with defaults.");
            ItemsCollectedCount = 0;
            LastVisitedTimestamp = DateTime.Now;
        }
        else
        {
            Debug.Log($"GameDataManager: State restored! Items: {ItemsCollectedCount}, Last Visit: {LastVisitedTimestamp:HH:mm:ss}");
        }
    }

    /// <summary>
    /// Saves the current state of the manager to UnityEditor.SessionState.
    /// SessionState persists across hot reloads within the same editor session.
    /// Complex types (like DateTime) need to be serialized (e.g., to JSON).
    /// </summary>
    private void SaveState()
    {
#if UNITY_EDITOR
        GameDataManagerState stateToSave = new GameDataManagerState
        {
            itemsCollectedCount = ItemsCollectedCount,
            // Store DateTime as Ticks for simple JSON serialization.
            lastVisitedTimestampTicks = LastVisitedTimestamp.Ticks
        };
        string jsonState = JsonUtility.ToJson(stateToSave);
        SessionState.SetString(SessionStateKey, jsonState);
        Debug.Log("GameDataManager: State saved to SessionState.");
#endif
    }

    /// <summary>
    /// Restores the state of the manager from UnityEditor.SessionState.
    /// </summary>
    /// <returns>True if state was successfully restored, false otherwise.</returns>
    private bool RestoreState()
    {
#if UNITY_EDITOR
        if (SessionState.HasString(SessionStateKey))
        {
            string jsonState = SessionState.GetString(SessionStateKey);
            GameDataManagerState restoredState = JsonUtility.FromJson<GameDataManagerState>(jsonState);
            ItemsCollectedCount = restoredState.itemsCollectedCount;
            LastVisitedTimestamp = new DateTime(restoredState.lastVisitedTimestampTicks);
            return true;
        }
#endif
        return false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Callback method for UnityEditor.EditorApplication.playModeStateChanged.
    /// This allows us to save state *before* a domain reload happens.
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // When Unity is exiting edit mode (to enter play mode, which causes a domain reload)
        // or exiting play mode (to enter edit mode, also causes a domain reload),
        // we want to save the current runtime state.
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.ExitingPlayMode)
        {
            if (_instance != null)
            {
                _instance.SaveState();
            }
        }
        // If you want to clear the GameDataManager's state when you explicitly stop play mode
        // and go back to editor, you could uncomment the following.
        // For hot-reload *persistence*, we generally want to keep it.
        // else if (state == PlayModeStateChange.EnteredEditMode)
        // {
        //     SessionState.EraseString(SessionStateKey);
        //     Debug.Log("GameDataManager: SessionState cleared on entering Edit Mode.");
        // }
    }
#endif

    /// <summary>
    /// Public method to modify the manager's state.
    /// Immediately saves state after modification to ensure persistence on next hot reload.
    /// </summary>
    public void CollectItem()
    {
        ItemsCollectedCount++;
        LastVisitedTimestamp = DateTime.Now;
        Debug.Log($"GameDataManager: Item collected! Total: {ItemsCollectedCount}");
        // It's good practice to save the state immediately after a significant change
        // to ensure the latest data is available for a hot reload.
        SaveState();
    }

    /// <summary>
    /// Helper struct for serializing GameDataManager's state using JsonUtility.
    /// Needs to be [System.Serializable].
    /// </summary>
    [System.Serializable]
    private struct GameDataManagerState
    {
        public int itemsCollectedCount;
        public long lastVisitedTimestampTicks; // DateTime can be stored as Ticks
    }
}
```

---

### **3. `HotReloadManager.cs` (MonoBehaviour for Scene Interaction)**

This `MonoBehaviour` is placed in the scene. It references the `GlobalGameSettings` `ScriptableObject` and interacts with the `GameDataManager` singleton, demonstrating how to access and display data from both persistent sources.

```csharp
// File: Assets/Scripts/HotReloadManager.cs
using UnityEngine;

/// <summary>
/// This MonoBehaviour demonstrates how to interact with and display data
/// from both ScriptableObjects and hot-reload persistent singletons.
/// It acts as the entry point in the scene for this example.
/// </summary>
public class HotReloadManager : MonoBehaviour
{
    [Tooltip("Reference to the GlobalGameSettings ScriptableObject. Assign this in the Inspector.")]
    [SerializeField] private GlobalGameSettings _gameSettings;

    // Cached reference to the GameDataManager instance.
    // This field itself will be reset on hot reload, but we re-obtain it in Awake().
    private GameDataManager _gameDataManager;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// After a domain reload, Awake will be called again.
    /// This is where we re-establish connections to singletons and check their state.
    /// </summary>
    private void Awake()
    {
        Debug.Log("HotReloadManager: Awake called.");

        if (_gameSettings == null)
        {
            Debug.LogError("HotReloadManager: GlobalGameSettings reference is missing! Please assign it in the Inspector.");
            return;
        }

        // Access the GameDataManager singleton.
        // Thanks to [RuntimeInitializeOnLoadMethod] in GameDataManager, its static
        // instance will have already been re-initialized and its state restored
        // before this Awake() method is called.
        _gameDataManager = GameDataManager.Instance;

        // Demonstrate using data from the ScriptableObject
        Debug.Log($"HotReloadManager: Using GlobalGameSettings: PlayerStartingHealth = {_gameSettings.PlayerStartingHealth}, LevelPrefix = {_gameSettings.LevelPrefix}");

        // Demonstrate using data from GameDataManager (which survived hot reload)
        Debug.Log($"HotReloadManager: GameDataManager State: Items Collected = {_gameDataManager.ItemsCollectedCount}, Last Visit = {_gameDataManager.LastVisitedTimestamp:HH:mm:ss}");
    }

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// Used here to display current data and provide interaction buttons.
    /// </summary>
    private void OnGUI()
    {
        // Only draw GUI if required references are assigned
        if (_gameSettings == null || _gameDataManager == null)
            return;

        // Create a scrollable area for better readability
        GUILayout.BeginArea(new Rect(10, 10, 450, Screen.height - 20));
        GUILayout.BeginVertical(GUI.skin.box); // Use a box style for better visual grouping

        GUILayout.Label("<color=white><size=24>Hot Reload Patterns Demo</size></color>");
        GUILayout.Space(15);

        // --- Display Global Game Settings ---
        GUILayout.Label($"<color=cyan><size=18>Global Game Settings (ScriptableObject)</size></color>", GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.Label($"<b>Player Starting Health:</b> {_gameSettings.PlayerStartingHealth}");
        GUILayout.Label($"<b>Level Prefix:</b> {_gameSettings.LevelPrefix}");
        GUILayout.Label($"<b>Game Speed Multiplier:</b> {_gameSettings.GameSpeedMultiplier}");
        GUILayout.Space(20);

        // --- Display Game Data Manager State ---
        GUILayout.Label($"<color=lime><size=18>Game Data Manager (Static/Singleton)</size></color>", GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.Label($"<b>Items Collected:</b> {_gameDataManager.ItemsCollectedCount}");
        GUILayout.Label($"<b>Last Visited:</b> {_gameDataManager.LastVisitedTimestamp:yyyy-MM-dd HH:mm:ss}");
        GUILayout.Space(20);

        // --- Interaction Button ---
        if (GUILayout.Button("Collect Item (Update GameDataManager)", GUILayout.Height(50)))
        {
            _gameDataManager.CollectItem();
        }
        GUILayout.Space(30);

        // --- Instructions for Testing Hot Reload ---
        GUILayout.Label("<color=yellow><size=16>How to Test Hot Reload:</size></color>", GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.Label("1. Press the 'Collect Item' button a few times.");
        GUILayout.Label("2. Modify this `HotReloadManager.cs` script (e.g., change a debug log message, add a comment, or add a space).");
        GUILayout.Label("3. Save the script (Ctrl+S or Cmd+S). Unity will recompile.");
        GUILayout.Label("4. Observe the console output and the GUI:");
        GUILayout.Label("   - `GameDataManager` values ('Items Collected', 'Last Visited') remain the same, demonstrating persistence across hot reload.");
        GUILayout.Label("   - `GlobalGameSettings` values are also preserved, as they are asset-based.");
        GUILayout.Label("   - `HotReloadManager: Awake called.` will appear in the console again, showing the MonoBehaviour re-initialized.");
        GUILayout.Space(10);
        GUILayout.Label("<i>Note: This persistence works within the current editor play session. If you stop Play Mode entirely, the `GameDataManager` state will reset unless you implement broader persistence (e.g., saving to disk).</i>");


        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

### **Setup Instructions in Unity:**

1.  **Create Scripts:**
    *   Create a folder named `Scripts` (e.g., `Assets/Scripts`).
    *   Create three C# scripts inside it: `GlobalGameSettings.cs`, `GameDataManager.cs`, and `HotReloadManager.cs`.
    *   Copy and paste the respective code into each file.

2.  **Create GlobalGameSettings Asset:**
    *   In the Unity Editor, go to `Assets -> Create -> HotReloadPatterns -> Global Game Settings`.
    *   Name the new asset `MyGlobalGameSettings` (or anything you prefer).
    *   You can select this asset in the Project window and modify its `Player Starting Health`, `Level Prefix`, etc., in the Inspector. These changes are saved permanently.

3.  **Create HotReloadManager GameObject:**
    *   In your scene (e.g., the default `SampleScene`), create an empty GameObject.
    *   Rename it `HotReloadManager`.
    *   Add the `HotReloadManager` script component to this GameObject.

4.  **Assign ScriptableObject Reference:**
    *   Select the `HotReloadManager` GameObject in the Hierarchy.
    *   In its Inspector, drag your `MyGlobalGameSettings` asset (from step 2) into the `Global Game Settings` slot of the `Hot Reload Manager` component.

5.  **Run and Test:**
    *   Press Play in the Unity Editor.
    *   Observe the GUI. Press the "Collect Item" button a few times.
    *   While still in Play Mode, make a minor change to the `HotReloadManager.cs` script (e.g., add a space, change a string literal in a `Debug.Log`).
    *   Save the script (Ctrl+S or Cmd+S).
    *   Unity will recompile the scripts, triggering a hot reload.
    *   Notice that the "Items Collected" and "Last Visited" values from `GameDataManager` remain the same, demonstrating successful state preservation across the hot reload! The `GlobalGameSettings` values also persist as expected.