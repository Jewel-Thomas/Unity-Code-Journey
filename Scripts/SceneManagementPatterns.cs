// Unity Design Pattern Example: SceneManagementPatterns
// This script demonstrates the SceneManagementPatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates a practical implementation of 'Scene Management Patterns'. This isn't a single, rigid design pattern but rather a collection of best practices and patterns (like Service Locator, Event-Driven Architecture, Asynchronous Loading, Additive Scene Loading) that work together to create a robust and flexible scene management system.

The core idea is to:
1.  **Centralize Scene Logic:** Use a dedicated `SceneLoader` service (often a Singleton) to handle all scene operations.
2.  **Decouple Requests:** Utilize an event system (`GameEvents`) so that any part of your game can request a scene change without directly knowing or depending on the `SceneLoader` implementation.
3.  **Asynchronous Operations:** Load scenes in the background to prevent the game from freezing, providing a smooth user experience.
4.  **Loading Screens:** Show a progress bar and visual feedback during asynchronous loading.
5.  **Additive Loading:** Allow multiple scenes to be loaded simultaneously (e.g., a persistent manager scene, a UI scene, and a level content scene).
6.  **Scene Bundling/Groups:** Load a set of related scenes (and optionally unload others) as a single operation.

---

### **Project Setup in Unity**

To make this example work, follow these steps in a new Unity project:

1.  **Create Folders:**
    *   `Assets/Scripts/Managers`
    *   `Assets/Scripts/Events`
    *   `Assets/Scripts/UI`
    *   `Assets/Scenes`

2.  **Import TextMeshPro:** Go to `Window > TextMeshPro > Import TMP Essential Resources` if you haven't already.

3.  **Create Scenes (File > New Scene):**
    *   Save as `_PersistentScene` (or whatever you set `persistentSceneName` to in `SceneLoader.cs`).
    *   Save as `Menu`
    *   Save as `Level1`
    *   Save as `Level2`
    *   Save as `Additive_Managers` (a simple scene, maybe just add a Cube to visualize its presence)
    *   Save as `Level1_QuestArea` (another simple scene for in-level additive content)

4.  **Add Scenes to Build Settings:** Go to `File > Build Settings` and drag all created scenes into the "Scenes In Build" list. Ensure `_PersistentScene` is at index 0 (or your starting scene).

5.  **Set up `_PersistentScene`:**
    *   Create an empty GameObject named `SceneLoaderManager`.
    *   Create an empty GameObject named `LoadingScreenCanvas`.
    *   Create a UI Canvas (Right-click in Hierarchy > UI > Canvas) named `LoadingScreenUI`.
    *   Inside `LoadingScreenUI` (the Canvas), create a Panel named `LoadingPanel`.
        *   Set its `Rect Transform` to cover the entire screen (e.g., Anchors Min/Max X/Y to 0/1, Left/Right/Top/Bottom to 0).
        *   Add a `Canvas Group` component to `LoadingPanel`.
        *   Add a TextMeshPro Text component (for percentage) named `ProgressText`.
        *   Add a Slider component (for progress bar) named `ProgressBar`.
    *   Attach the `SceneLoader.cs` script to `SceneLoaderManager`.
    *   Attach the `LoadingScreenUI.cs` script to `LoadingScreenCanvas`.
        *   Drag `LoadingPanel` (from your Canvas) into the `Loading Screen Panel` field of `LoadingScreenUI.cs`.
        *   Drag `ProgressText` into `Progress Text` field.
        *   Drag `ProgressBar` into `Progress Bar` field.

6.  **Set up `Menu` Scene:**
    *   Create a UI Canvas.
    *   Add 5 Buttons: "Load Level 1", "Load Level 2", "Load Level 1 + Additive Managers", "Unload Additive Managers", "Reload Current Scene".
    *   Add a TextMeshPro Text to display scene info.
    *   Create an empty GameObject named `MenuManager`.
    *   Attach the `MenuManager.cs` script to `MenuManager`.
        *   Assign your created buttons and the TextMeshPro text to the respective fields in the Inspector.
        *   Ensure `levelOneSceneName`, `levelTwoSceneName`, and `additiveSceneName` are correctly set (e.g., "Level1", "Level2", "Additive_Managers").

7.  **Set up `Level1` and `Level2` Scenes:**
    *   For each level, you can add some visual elements (e.g., a simple cube or terrain) to distinguish them.
    *   Create a UI Canvas.
    *   Add 4 Buttons: "Load Next Level", "Load In-Level Additive", "Unload In-Level Additive", "Reload Current Level".
    *   Add a TextMeshPro Text to display current scene info.
    *   Create an empty GameObject named `LevelManager`.
    *   Attach the `LevelManager.cs` script to `LevelManager`.
        *   Assign your created buttons and the TextMeshPro text.
        *   Set `nextLevelSceneName` appropriately (e.g., "Level2" for Level1, "Menu" for Level2).
        *   Set `inLevelAdditiveContent` (e.g., "Level1_QuestArea").

8.  **Run the Game:** Start the game from the `_PersistentScene` or `Menu` scene. The `SceneLoader` will ensure the persistent scene loads first, then you can navigate using the UI buttons.

---

### **1. GameEvents.cs**

This static class acts as a central event hub, decoupling the components that request scene operations (e.g., `MenuManager`) from the component that executes them (`SceneLoader`).

```csharp
// Assets/Scripts/Events/GameEvents.cs
using System;
using System.Collections.Generic;
using UnityEngine; // Needed for basic Unity types like Debug.Log (though not strictly for events)

/// <summary>
/// This static class acts as a central hub for all game-wide events,
/// especially for scene management. It decouples the requestor from the executor.
/// </summary>
public static class GameEvents
{
    // --- Scene Management Events ---

    // Event for requesting a single scene load.
    // Parameters: sceneName (string), additive (bool), onCompleteCallback (Action)
    public static event Action<string, bool, Action> OnSceneLoadRequested;

    // Event for requesting a group of scenes to be loaded (and optionally unloaded).
    // Parameters: scenesToLoad (List<string>), scenesToUnload (List<string>), onCompleteCallback (Action)
    public static event Action<List<string>, List<string>, Action> OnSceneGroupLoadRequested;

    // Event for requesting a single scene to be unloaded.
    // Parameters: sceneName (string), onCompleteCallback (Action)
    public static event Action<string, Action> OnSceneUnloadRequested;

    // Event reported by the SceneLoader during asynchronous loading.
    // Parameter: progress (float, from 0 to 1)
    public static event Action<float> OnSceneLoadProgress;

    // Event reported when a specific scene has finished loading and is activated.
    // Parameter: sceneName (string)
    public static event Action<string> OnSceneLoaded;

    // Event reported when all requested scenes for a single operation (load or group load) are fully loaded and ready.
    public static event Action OnAllRequestedScenesReady;

    // --- Loading Screen Events ---

    // Event to signal that the loading screen should be shown.
    public static event Action OnLoadingScreenShown;

    // Event to signal that the loading screen should be hidden.
    public static event Action OnLoadingScreenHidden;


    // --- Public Methods to Invoke Events (Helper methods for convenience) ---

    /// <summary>
    /// Invokes the OnSceneLoadRequested event to initiate a single scene load.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <param name="additive">If true, the scene will be loaded additively (on top of existing scenes).</param>
    /// <param name="onComplete">An optional callback to invoke when the scene load is complete.</param>
    public static void RequestSceneLoad(string sceneName, bool additive = false, Action onComplete = null)
    {
        OnSceneLoadRequested?.Invoke(sceneName, additive, onComplete);
    }

    /// <summary>
    /// Invokes the OnSceneGroupLoadRequested event to initiate loading of multiple scenes.
    /// </summary>
    /// <param name="scenesToLoad">A list of scene names to load.</param>
    /// <param name="scenesToUnload">An optional list of scene names to unload before or during the load process.</param>
    /// <param name="onComplete">An optional callback to invoke when all scenes in the group are loaded.</param>
    public static void RequestSceneGroupLoad(List<string> scenesToLoad, List<string> scenesToUnload = null, Action onComplete = null)
    {
        OnSceneGroupLoadRequested?.Invoke(scenesToLoad, scenesToUnload, onComplete);
    }

    /// <summary>
    /// Invokes the OnSceneUnloadRequested event to unload a specific scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unload.</param>
    /// <param name="onComplete">An optional callback to invoke when the scene unload is complete.</param>
    public static void RequestSceneUnload(string sceneName, Action onComplete = null)
    {
        OnSceneUnloadRequested?.Invoke(sceneName, onComplete);
    }

    /// <summary>
    /// Reports the current progress of scene loading.
    /// </summary>
    /// <param name="progress">A float value representing the loading progress (0-1).</param>
    public static void ReportSceneLoadProgress(float progress)
    {
        OnSceneLoadProgress?.Invoke(progress);
    }

    /// <summary>
    /// Reports that a specific scene has finished loading and is activated.
    /// </summary>
    /// <param name="sceneName">The name of the scene that finished loading.</param>
    public static void ReportSceneLoaded(string sceneName)
    {
        OnSceneLoaded?.Invoke(sceneName);
    }

    /// <summary>
    /// Reports that all scenes requested in the current operation are ready.
    /// This signifies the end of a scene transition.
    /// </summary>
    public static void ReportAllRequestedScenesReady()
    {
        OnAllRequestedScenesReady?.Invoke();
    }

    /// <summary>
    /// Invokes the OnLoadingScreenShown event.
    /// </summary>
    public static void ShowLoadingScreen()
    {
        OnLoadingScreenShown?.Invoke();
    }

    /// <summary>
    /// Invokes the OnLoadingScreenHidden event.
    /// </summary>
    public static void HideLoadingScreen()
    {
        OnLoadingScreenHidden?.Invoke();
    }
}
```

---

### **2. SceneLoader.cs**

This is the core component of the Scene Management Patterns. It's a Singleton that persists across scenes, listens to `GameEvents` for scene change requests, and orchestrates the loading/unloading process asynchronously, including managing the loading screen.

```csharp
// Assets/Scripts/Managers/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq; // For LINQ operations like .Average()

/// <summary>
/// SceneLoader is a central singleton service responsible for managing all scene loading and unloading operations.
/// It uses asynchronous operations, additive loading, and an event-driven system for robust scene transitions.
/// This class demonstrates the core principles of the 'Scene Management Patterns' design pattern:
/// 1.  **Service Locator / Singleton:** Provides a globally accessible service.
/// 2.  **Event-Driven Architecture:** Decouples scene requests from their execution via GameEvents.
/// 3.  **Asynchronous Loading:** Prevents game freezes during scene transitions.
/// 4.  **Additive Scene Loading:** Allows for persistent scenes (like this manager itself) and complex level structures.
/// 5.  **Loading Screen Integration:** Manages showing/hiding a loading screen to provide user feedback.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // Singleton instance to ensure only one SceneLoader exists and is easily accessible.
    public static SceneLoader Instance { get; private set; }

    // Private list to keep track of all ongoing asynchronous scene operations.
    // This is crucial for managing multiple additive scene loads or a scene group load.
    private readonly List<AsyncOperation> _currentLoadingOperations = new List<AsyncOperation>();

    // Private list to hold coroutines, useful if we need to stop specific loading processes (though less common).
    private readonly List<Coroutine> _activeLoadingCoroutines = new List<Coroutine>();

    // Tracks if a loading screen is currently active to prevent multiple show/hide calls.
    private bool _isLoadingScreenActive = false;

    // Optional: Reference to a specific scene that should always remain loaded, e.g., for persistent managers.
    // This scene typically contains GameObjects with DontDestroyOnLoad and essential services.
    [Tooltip("Optional: The name of a scene that should always remain loaded (e.g., your _PersistentScene).")]
    public string persistentSceneName = "_PersistentScene";

    private void Awake()
    {
        // Singleton enforcement: If another instance already exists, destroy this one.
        // This ensures there's only one SceneLoader across all scenes.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate SceneLoader detected, destroying the new one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Ensure this GameObject (and thus the SceneLoader) persists across scene loads.
        // This is a cornerstone of the persistent scene pattern.
        DontDestroyOnLoad(gameObject);

        // Subscribe to relevant GameEvents. This makes the SceneLoader reactive to requests
        // from any other part of the application without direct coupling.
        GameEvents.OnSceneLoadRequested += HandleSceneLoadRequest;
        GameEvents.OnSceneGroupLoadRequested += HandleSceneGroupLoadRequest;
        GameEvents.OnSceneUnloadRequested += HandleSceneUnloadRequest;

        // Optionally, check if this is the very first scene load (e.g., from Editor Play).
        // If the persistent scene isn't loaded, load it additively to ensure managers are present.
        if (!string.IsNullOrEmpty(persistentSceneName) && !SceneManager.GetSceneByName(persistentSceneName).isLoaded)
        {
            StartCoroutine(LoadPersistentSceneAdditive());
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks and null reference exceptions
        // when the SceneLoader object is destroyed (e.g., on application quit).
        GameEvents.OnSceneLoadRequested -= HandleSceneLoadRequest;
        GameEvents.OnSceneGroupLoadRequested -= HandleSceneGroupLoadRequest;
        GameEvents.OnSceneUnloadRequested -= HandleSceneUnloadRequest;

        // Stop all active loading coroutines if the manager is destroyed mid-load.
        foreach (Coroutine coroutine in _activeLoadingCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _activeLoadingCoroutines.Clear();
        _currentLoadingOperations.Clear();
    }

    // --- Event Handlers (Called when GameEvents are invoked) ---

    private void HandleSceneLoadRequest(string sceneName, bool additive, Action onComplete)
    {
        Debug.Log($"SceneLoader: Received request to load scene '{sceneName}' (Additive: {additive}).");
        // Start a coroutine for the loading process and keep its reference.
        _activeLoadingCoroutines.Add(StartCoroutine(LoadSceneRoutine(sceneName, additive, onComplete)));
    }

    private void HandleSceneGroupLoadRequest(List<string> scenesToLoad, List<string> scenesToUnload, Action onComplete)
    {
        Debug.Log($"SceneLoader: Received request to load scene group. Loading: {string.Join(", ", scenesToLoad)}, Unloading: {(scenesToUnload != null ? string.Join(", ", scenesToUnload) : "None")}.");
        _activeLoadingCoroutines.Add(StartCoroutine(LoadSceneGroupRoutine(scenesToLoad, scenesToUnload, onComplete)));
    }

    private void HandleSceneUnloadRequest(string sceneName, Action onComplete)
    {
        Debug.Log($"SceneLoader: Received request to unload scene '{sceneName}'.");
        _activeLoadingCoroutines.Add(StartCoroutine(UnloadSceneRoutine(sceneName, onComplete)));
    }

    // --- Core Loading Coroutines ---

    /// <summary>
    /// Special coroutine to load the persistent scene additively.
    /// This runs during SceneLoader's Awake to ensure critical managers are loaded.
    /// </summary>
    private IEnumerator LoadPersistentSceneAdditive()
    {
        if (!string.IsNullOrEmpty(persistentSceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
            while (!op.isDone)
            {
                yield return null;
            }
            Debug.Log($"SceneLoader: Persistent scene '{persistentSceneName}' loaded additively.");
        }
    }

    /// <summary>
    /// Coroutine to handle loading a single scene.
    /// If not additive, it first unloads all other non-persistent scenes.
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName, bool additive, Action onComplete)
    {
        ShowLoadingScreen(); // Signal the UI to show the loading screen.

        // If not an additive load, we need to unload all currently loaded scenes
        // except for the persistent one, effectively performing a full scene change.
        if (!additive)
        {
            List<string> scenesToUnload = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                // Don't unload the persistent scene, as it should always remain.
                // Also, ensure the scene is actually loaded.
                if (s.name != persistentSceneName && s.isLoaded)
                {
                    scenesToUnload.Add(s.name);
                }
            }

            if (scenesToUnload.Any())
            {
                Debug.Log($"SceneLoader: Unloading existing scenes: {string.Join(", ", scenesToUnload)} for non-additive load.");
                // Use a sub-coroutine to await unloading.
                yield return StartCoroutine(UnloadScenesRoutine(scenesToUnload, null));
            }
        }

        // Now, perform the actual loading of the new scene.
        // The primarySceneName argument is used to set the active scene and report completion.
        yield return StartCoroutine(PerformLoadOperations(new List<string> { sceneName }, onComplete, sceneName));
    }

    /// <summary>
    /// Coroutine to handle loading a group of scenes, and optionally unloading other scenes first.
    /// </summary>
    private IEnumerator LoadSceneGroupRoutine(List<string> scenesToLoad, List<string> scenesToUnload, Action onComplete)
    {
        ShowLoadingScreen(); // Signal the UI to show the loading screen.

        // First, handle any scenes that need to be unloaded.
        if (scenesToUnload != null && scenesToUnload.Any())
        {
            Debug.Log($"SceneLoader: Unloading specific scenes in group: {string.Join(", ", scenesToUnload)}.");
            yield return StartCoroutine(UnloadScenesRoutine(scenesToUnload, null));
        }

        // Then, proceed with loading the new scenes.
        // The first scene in the list is typically considered the 'primary' for setting active scene.
        yield return StartCoroutine(PerformLoadOperations(scenesToLoad, onComplete, scenesToLoad.FirstOrDefault()));
    }

    /// <summary>
    /// The core routine for asynchronously loading a list of scenes.
    /// It handles progress reporting and scene activation.
    /// </summary>
    private IEnumerator PerformLoadOperations(List<string> scenesToLoad, Action onCompleteCallback, string primarySceneName = null)
    {
        _currentLoadingOperations.Clear(); // Clear any previous operations.

        // Start loading all requested scenes asynchronously.
        foreach (string sceneName in scenesToLoad)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"Attempted to load an empty or null scene name. Skipping.");
                continue;
            }
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                Debug.LogWarning($"Scene '{sceneName}' is already loaded. Skipping.");
                continue;
            }

            // LoadSceneMode.Additive is used for all operations to maintain control.
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = false; // Prevent scene from activating immediately until all are ready.
            _currentLoadingOperations.Add(op);
            Debug.Log($"SceneLoader: Started loading '{sceneName}'.");
        }

        // If no valid scenes were added to load, abort.
        if (!_currentLoadingOperations.Any())
        {
            Debug.LogWarning("No scenes to load. Aborting load operations.");
            HideLoadingScreen();
            onCompleteCallback?.Invoke();
            GameEvents.ReportAllRequestedScenesReady();
            yield break;
        }

        float totalProgress;
        // Wait for all scenes to be loaded to 90% (this is before 'allowSceneActivation = true').
        // This phase updates the progress bar to give feedback.
        while (_currentLoadingOperations.Any(op => op.progress < 0.9f))
        {
            totalProgress = _currentLoadingOperations.Average(op => op.progress);
            // Scale progress to 0-0.9 to visually distinguish the loading phase from activation.
            GameEvents.ReportSceneLoadProgress(totalProgress * 0.9f);
            yield return null;
        }

        // All scenes are loaded up to 90%. Now, allow them to activate.
        // This part often happens very quickly.
        foreach (AsyncOperation op in _currentLoadingOperations)
        {
            op.allowSceneActivation = true;
        }

        // Wait for all scenes to be fully activated (i.e., op.isDone becomes true).
        while (_currentLoadingOperations.Any(op => !op.isDone))
        {
            totalProgress = _currentLoadingOperations.Average(op => op.progress);
            // Report progress from 90% to 100% during the activation phase.
            GameEvents.ReportSceneLoadProgress(0.9f + (totalProgress * 0.1f));
            yield return null;
        }

        // All scenes are now fully loaded and activated.
        _currentLoadingOperations.Clear(); // Clear the list as operations are complete.
        GameEvents.ReportSceneLoadProgress(1f); // Ensure final progress is exactly 100%.

        // Set the primary loaded scene as active. This is crucial for Unity's context (e.g.,
        // where new GameObjects are instantiated, for lighting, and for SceneManager.GetActiveScene()).
        if (!string.IsNullOrEmpty(primarySceneName))
        {
            Scene newActiveScene = SceneManager.GetSceneByName(primarySceneName);
            if (newActiveScene.IsValid() && newActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(newActiveScene);
                Debug.Log($"SceneLoader: Set active scene to '{primarySceneName}'.");
                GameEvents.ReportSceneLoaded(primarySceneName); // Report the primary scene as loaded.
            }
            else
            {
                Debug.LogWarning($"Scene '{primarySceneName}' was requested as primary but is not valid or loaded.");
            }
        }
        else if (scenesToLoad.Any()) // If no specific primary, use the first one as a fallback.
        {
            Scene newActiveScene = SceneManager.GetSceneByName(scenesToLoad[0]);
            if (newActiveScene.IsValid() && newActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(newActiveScene);
                Debug.Log($"SceneLoader: Set active scene to '{scenesToLoad[0]}'.");
                GameEvents.ReportSceneLoaded(scenesToLoad[0]);
            }
        }

        // After a small delay (optional, for visual polish or to ensure all Start() methods run)
        yield return new WaitForSeconds(0.1f);

        HideLoadingScreen(); // Signal the UI to hide the loading screen.
        onCompleteCallback?.Invoke(); // Invoke the completion callback if provided.
        GameEvents.ReportAllRequestedScenesReady(); // Report overall completion of the entire request.
    }

    /// <summary>
    /// Coroutine to handle unloading a single scene.
    /// </summary>
    private IEnumerator UnloadSceneRoutine(string sceneName, Action onComplete)
    {
        // Wrap a single scene name in a list for the general UnloadScenesRoutine.
        List<string> scenesToUnload = new List<string> { sceneName };
        yield return StartCoroutine(UnloadScenesRoutine(scenesToUnload, onComplete));
    }

    /// <summary>
    /// Coroutine to handle unloading multiple scenes.
    /// </summary>
    private IEnumerator UnloadScenesRoutine(List<string> sceneNames, Action onComplete)
    {
        List<AsyncOperation> unloadOperations = new List<AsyncOperation>();
        foreach (string sceneName in sceneNames)
        {
            if (string.IsNullOrEmpty(sceneName)) continue;
            Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);

            // Validate scene and prevent unloading the persistent scene.
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded && sceneName != persistentSceneName)
            {
                Debug.Log($"SceneLoader: Initiating unload for scene '{sceneName}'.");
                unloadOperations.Add(SceneManager.UnloadSceneAsync(sceneName));
            }
            else if (sceneName == persistentSceneName)
            {
                Debug.LogWarning($"Attempted to unload persistent scene '{persistentSceneName}'. This is not allowed.");
            }
            else
            {
                Debug.LogWarning($"Attempted to unload scene '{sceneName}' which is not loaded or valid.");
            }
        }

        // Wait for all unload operations to complete.
        if (unloadOperations.Any())
        {
            foreach (AsyncOperation op in unloadOperations)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }
            Debug.Log($"SceneLoader: All specified scenes successfully unloaded.");
        }
        else
        {
            Debug.Log($"SceneLoader: No scenes actually unloaded by this request.");
        }

        onComplete?.Invoke(); // Invoke the completion callback.
    }

    // --- Loading Screen Management (Using GameEvents) ---

    /// <summary>
    /// Sends an event to show the loading screen if it's not already active.
    /// </summary>
    private void ShowLoadingScreen()
    {
        if (!_isLoadingScreenActive)
        {
            _isLoadingScreenActive = true;
            GameEvents.ShowLoadingScreen(); // Publish event for LoadingScreenUI to react.
            Debug.Log("SceneLoader: Signaling to show loading screen.");
        }
    }

    /// <summary>
    /// Sends an event to hide the loading screen if it's currently active.
    /// </summary>
    private void HideLoadingScreen()
    {
        if (_isLoadingScreenActive)
        {
            _isLoadingScreenActive = false;
            GameEvents.HideLoadingScreen(); // Publish event for LoadingScreenUI to react.
            Debug.Log("SceneLoader: Signaling to hide loading screen.");
        }
    }
}
```

---

### **3. LoadingScreenUI.cs**

This script manages the visual representation of the loading screen. It's designed to react to `GameEvents` published by the `SceneLoader`, ensuring that the UI is decoupled from the loading logic.

```csharp
// Assets/Scripts/UI/LoadingScreenUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Assuming TextMeshPro is used for better text rendering.
             // If not, replace with 'using UnityEngine.UI;' and Text component.

/// <summary>
/// Manages the visual display of the loading screen UI, reacting to scene loading events.
/// This component listens to GameEvents to show/hide itself and update progress.
/// It uses a CanvasGroup for fading effects and should be placed on a GameObject
/// that persists across scenes (e.g., in your _PersistentScene).
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The main panel or canvas group that contains all loading screen elements.")]
    public GameObject loadingScreenPanel;
    [Tooltip("TextMeshPro Text component to display loading percentage.")]
    public TextMeshProUGUI progressText; // Use Text if not using TextMeshPro
    [Tooltip("Slider component to display a progress bar.")]
    public Slider progressBar; // Or Image for fill amount

    [Header("Animation Settings")]
    [Tooltip("Time taken for the loading screen to fade in/out.")]
    public float fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup; // Used for smooth fade-in/out effects.
    private Coroutine _fadeCoroutine; // Keeps track of the current fading animation.

    private void Awake()
    {
        if (loadingScreenPanel == null)
        {
            Debug.LogError("LoadingScreenUI: 'loadingScreenPanel' is not assigned! This UI will not function.");
            enabled = false; // Disable the script if essential components are missing.
            return;
        }

        // Get or add a CanvasGroup component to the loadingScreenPanel for fading.
        _canvasGroup = loadingScreenPanel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = loadingScreenPanel.AddComponent<CanvasGroup>();
        }

        // Initialize UI state: fully transparent and inactive.
        _canvasGroup.alpha = 0f;
        loadingScreenPanel.SetActive(false);

        // Set initial progress display.
        UpdateProgressUI(0f);
    }

    private void OnEnable()
    {
        // Subscribe to GameEvents for showing/hiding and progress updates.
        // This makes the LoadingScreenUI completely decoupled from the SceneLoader logic.
        GameEvents.OnLoadingScreenShown += HandleLoadingScreenShown;
        GameEvents.OnLoadingScreenHidden += HandleLoadingScreenHidden;
        GameEvents.OnSceneLoadProgress += UpdateProgressUI;
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and null reference exceptions
        // if this GameObject is destroyed or disabled.
        GameEvents.OnLoadingScreenShown -= HandleLoadingScreenShown;
        GameEvents.OnLoadingScreenHidden -= HandleLoadingScreenHidden;
        GameEvents.OnSceneLoadProgress -= UpdateProgressUI;

        // Stop any ongoing fade coroutine to prevent visual glitches if disabled mid-fade.
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    // --- Event Handlers ---

    /// <summary>
    /// Handles the event when the loading screen should be shown.
    /// Initiates a fade-in animation.
    /// </summary>
    private void HandleLoadingScreenShown()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine); // Stop any existing fade.
        loadingScreenPanel.SetActive(true); // Ensure the panel is active for the fade-in to be visible.
        _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup.alpha, 1f, fadeDuration));
        Debug.Log("LoadingScreenUI: Showing loading screen.");
    }

    /// <summary>
    /// Handles the event when the loading screen should be hidden.
    /// Initiates a fade-out animation and deactivates the panel afterward.
    /// </summary>
    private void HandleLoadingScreenHidden()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine); // Stop any existing fade.
        _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup.alpha, 0f, fadeDuration, () => {
            loadingScreenPanel.SetActive(false); // Hide the panel completely after fade-out.
            Debug.Log("LoadingScreenUI: Hidden loading screen.");
        }));
    }

    /// <summary>
    /// Updates the text and progress bar to reflect the current loading progress.
    /// </summary>
    /// <param name="progress">A float value from 0 to 1 representing the loading progress.</param>
    private void UpdateProgressUI(float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"Loading... {Mathf.FloorToInt(progress * 100)}%";
        }
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    // --- Helper Coroutine for Fading ---

    /// <summary>
    /// Coroutine to smoothly animate the alpha of a CanvasGroup.
    /// </summary>
    /// <param name="startAlpha">The starting alpha value.</param>
    /// <param name="endAlpha">The target alpha value.</param>
    /// <param name="duration">The duration of the fade animation.</param>
    /// <param name="onComplete">An optional callback to execute when the fade is complete.</param>
    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaledDeltaTime for UI that shouldn't be affected by Time.timeScale.
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        _canvasGroup.alpha = endAlpha; // Ensure the final alpha is exact.
        onComplete?.Invoke();
    }
}
```

---

### **4. MenuManager.cs (Example Usage)**

This script exemplifies how a menu scene (or any game component) would initiate scene transitions by sending requests through `GameEvents`.

```csharp
// Assets/Scripts/MenuManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // For TextMeshProUGUI

/// <summary>
/// Example script for a main menu or hub scene.
/// It demonstrates how to interact with the SceneLoader via GameEvents to change scenes,
/// load additive content, and manage scene groups.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The name of your main game level scene.")]
    public string levelOneSceneName = "Level1";
    [Tooltip("The name of another game level scene.")]
    public string levelTwoSceneName = "Level2";
    [Tooltip("The name of an additive scene (e.g., UI, common managers, environmental assets).")]
    public string additiveManagersSceneName = "Additive_Managers"; // Make sure this is added to Build Settings!

    [Header("UI References")]
    public Button loadLevel1Button;
    public Button loadLevel2Button;
    public Button loadLevel1WithAdditiveButton;
    public Button unloadAdditiveManagersButton;
    public TextMeshProUGUI currentSceneInfoText;

    private bool _isAdditiveManagersSceneLoaded = false;

    void Awake()
    {
        // Assign button listeners to invoke GameEvents.
        // This is how other parts of the game request scene changes without direct SceneLoader dependency.
        loadLevel1Button?.onClick.AddListener(() => GameEvents.RequestSceneLoad(levelOneSceneName));
        loadLevel2Button?.onClick.AddListener(() => GameEvents.RequestSceneLoad(levelTwoSceneName));
        loadLevel1WithAdditiveButton?.onClick.AddListener(LoadLevel1AndAdditiveManagers);
        unloadAdditiveManagersButton?.onClick.AddListener(UnloadAdditiveManagersScene);

        // Initial state of buttons, checking if the additive scene is already loaded (e.g., if reloaded menu)
        UpdateAdditiveButtonsState();
        UpdateUIInfo();
    }

    void OnEnable()
    {
        // Listen for scene load/unload completions to update button states and UI.
        GameEvents.OnSceneLoaded += HandleSceneLoaded;
        GameEvents.OnAllRequestedScenesReady += UpdateAdditiveButtonsState; // Update after any scene operation completes
        GameEvents.OnAllRequestedScenesReady += UpdateUIInfo;
    }

    void OnDisable()
    {
        GameEvents.OnSceneLoaded -= HandleSceneLoaded;
        GameEvents.OnAllRequestedScenesReady -= UpdateAdditiveButtonsState;
        GameEvents.OnAllRequestedScenesReady -= UpdateUIInfo;
    }

    /// <summary>
    /// Handles the event when a scene has finished loading.
    /// Used here to track the additive managers scene state.
    /// </summary>
    private void HandleSceneLoaded(string sceneName)
    {
        if (sceneName == additiveManagersSceneName)
        {
            _isAdditiveManagersSceneLoaded = true;
            Debug.Log($"MenuManager: Additive Managers Scene '{additiveManagersSceneName}' detected as loaded.");
            UpdateAdditiveButtonsState();
        }
    }

    /// <summary>
    /// Demonstrates loading a main scene and an additive scene as a group.
    /// This is useful for loading a level along with persistent UI or specialized level managers.
    /// </summary>
    private void LoadLevel1AndAdditiveManagers()
    {
        List<string> scenesToLoad = new List<string> { levelOneSceneName, additiveManagersSceneName };
        List<string> scenesToUnload = new List<string> { }; // No specific scenes to unload from the menu context.

        GameEvents.RequestSceneGroupLoad(scenesToLoad, scenesToUnload, () =>
        {
            Debug.Log($"MenuManager: Level 1 and {additiveManagersSceneName} group load completed!");
            _isAdditiveManagersSceneLoaded = true; // Assume success
            UpdateAdditiveButtonsState();
        });
    }

    /// <summary>
    /// Demonstrates unloading a specific additive scene.
    /// </summary>
    private void UnloadAdditiveManagersScene()
    {
        GameEvents.RequestSceneUnload(additiveManagersSceneName, () =>
        {
            Debug.Log($"MenuManager: {additiveManagersSceneName} unload completed!");
            _isAdditiveManagersSceneLoaded = false; // Assume success
            UpdateAdditiveButtonsState();
        });
    }

    /// <summary>
    /// Updates the interactive state of buttons related to additive scenes.
    /// This reflects the current loaded state of the `additiveManagersSceneName`.
    /// </summary>
    private void UpdateAdditiveButtonsState()
    {
        // Check actual scene manager state, not just internal flag.
        bool isManagersCurrentlyLoaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(additiveManagersSceneName).isLoaded;
        _isAdditiveManagersSceneLoaded = isManagersCurrentlyLoaded; // Sync internal flag

        if (unloadAdditiveManagersButton != null)
        {
            unloadAdditiveManagersButton.interactable = _isAdditiveManagersSceneLoaded;
        }
        if (loadLevel1WithAdditiveButton != null)
        {
            // Only allow loading if Level1 or Additive_Managers is not currently loaded.
            loadLevel1WithAdditiveButton.interactable = !UnityEngine.SceneManagement.SceneManager.GetSceneByName(levelOneSceneName).isLoaded || !_isAdditiveManagersSceneLoaded;
        }
    }

    /// <summary>
    /// Updates the TextMeshPro UI element to show which scenes are currently loaded.
    /// </summary>
    private void UpdateUIInfo()
    {
        if (currentSceneInfoText != null)
        {
            string info = "Current Loaded Scenes:\n";
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                info += $"- {s.name} (Active: {s == UnityEngine.SceneManagement.SceneManager.GetActiveScene()})\n";
            }
            currentSceneInfoText.text = info;
        }
    }
}
```

---

### **5. LevelManager.cs (Example Usage in a Game Scene)**

This script demonstrates how a game level itself can interact with the `SceneLoader` to transition to other levels, reload, or load/unload in-level additive content (like specific quest areas or mini-game scenes).

```csharp
// Assets/Scripts/LevelManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // For TextMeshProUGUI
using UnityEngine.SceneManagement; // For SceneManager.GetActiveScene()

/// <summary>
/// This script demonstrates how a game level itself can interact with the SceneLoader.
/// For example, loading more additive content, transitioning to another level, or reloading the current level.
/// This would be placed on a GameObject in your Level1 or Level2 scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Next Level Configuration")]
    [Tooltip("The name of the next level to load when a transition is requested.")]
    public string nextLevelSceneName = "Level2"; // Could also be "Menu" to return to menu.

    [Header("In-Level Additive Content")]
    [Tooltip("The name of an additional additive scene to load within this level (e.g., a specific quest area or a special event scene).")]
    public string inLevelAdditiveContentSceneName = "Level1_QuestArea"; // Ensure this scene exists and is in Build Settings!

    [Header("UI References")]
    public Button loadNextLevelButton;
    public Button loadAdditiveContentButton;
    public Button unloadAdditiveContentButton;
    public Button reloadCurrentLevelButton;
    public TextMeshProUGUI currentSceneInfoText;

    private bool _isInLevelAdditiveContentLoaded = false;

    void Awake()
    {
        // Assign button listeners to trigger scene operations via GameEvents.
        loadNextLevelButton?.onClick.AddListener(LoadNextLevel);
        loadAdditiveContentButton?.onClick.AddListener(LoadInLevelAdditiveContent);
        unloadAdditiveContentButton?.onClick.AddListener(UnloadInLevelAdditiveContent);
        reloadCurrentLevelButton?.onClick.AddListener(ReloadCurrentLevel);

        // Initial state update for UI.
        UpdateUIInfo();
        UpdateInLevelAdditiveContentButtons();
    }

    void OnEnable()
    {
        // Subscribe to relevant GameEvents to react to scene changes and update UI.
        GameEvents.OnSceneLoaded += HandleSceneLoaded;
        GameEvents.OnAllRequestedScenesReady += UpdateUIInfo; // Update scene info after any scene operation
        GameEvents.OnAllRequestedScenesReady += UpdateInLevelAdditiveContentButtons; // Update button states
    }

    void OnDisable()
    {
        // Unsubscribe from events to prevent issues.
        GameEvents.OnSceneLoaded -= HandleSceneLoaded;
        GameEvents.OnAllRequestedScenesReady -= UpdateUIInfo;
        GameEvents.OnAllRequestedScenesReady -= UpdateInLevelAdditiveContentButtons;
    }

    /// <summary>
    /// Handles the event when a scene has finished loading.
    /// Used here to track the in-level additive content state.
    /// </summary>
    private void HandleSceneLoaded(string sceneName)
    {
        if (sceneName == inLevelAdditiveContentSceneName)
        {
            _isInLevelAdditiveContentLoaded = true;
            Debug.Log($"LevelManager: In-level additive content '{inLevelAdditiveContentSceneName}' detected as loaded.");
            UpdateInLevelAdditiveContentButtons();
        }
    }

    /// <summary>
    /// Requests to load the next specified level. This will typically be a full scene transition,
    /// unloading the current level and loading the new one.
    /// </summary>
    private void LoadNextLevel()
    {
        Debug.Log($"LevelManager: Requesting to load next level: {nextLevelSceneName}");
        // Request a full scene change (additive=false).
        GameEvents.RequestSceneLoad(nextLevelSceneName);
    }

    /// <summary>
    /// Requests to reload the current active level. This effectively performs a full scene transition
    /// from the current scene back to itself.
    /// </summary>
    private void ReloadCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"LevelManager: Requesting to reload current level: {currentSceneName}");
        GameEvents.RequestSceneLoad(currentSceneName);
    }

    /// <summary>
    /// Requests to load additional content additively into the current level.
    /// This could be a new area, a cutscene, or temporary managers.
    /// </summary>
    private void LoadInLevelAdditiveContent()
    {
        // Prevent loading if already loaded.
        if (SceneManager.GetSceneByName(inLevelAdditiveContentSceneName).isLoaded)
        {
            Debug.LogWarning($"Additive content '{inLevelAdditiveContentSceneName}' is already loaded.");
            return;
        }

        Debug.Log($"LevelManager: Requesting to load in-level additive content: {inLevelAdditiveContentSceneName}");
        // Request an additive scene load. The 'true' parameter is crucial.
        GameEvents.RequestSceneLoad(inLevelAdditiveContentSceneName, true, () =>
        {
            Debug.Log($"LevelManager: Additive content '{inLevelAdditiveContentSceneName}' loaded!");
            _isInLevelAdditiveContentLoaded = true; // Assume success
            UpdateInLevelAdditiveContentButtons();
        });
    }

    /// <summary>
    /// Requests to unload the previously loaded in-level additive content.
    /// </summary>
    private void UnloadInLevelAdditiveContent()
    {
        // Prevent unloading if not currently loaded.
        if (!SceneManager.GetSceneByName(inLevelAdditiveContentSceneName).isLoaded)
        {
            Debug.LogWarning($"Additive content '{inLevelAdditiveContentSceneName}' is not loaded, cannot unload.");
            return;
        }

        Debug.Log($"LevelManager: Requesting to unload in-level additive content: {inLevelAdditiveContentSceneName}");
        GameEvents.RequestSceneUnload(inLevelAdditiveContentSceneName, () =>
        {
            Debug.Log($"LevelManager: Additive content '{inLevelAdditiveContentSceneName}' unloaded!");
            _isInLevelAdditiveContentLoaded = false; // Assume success
            UpdateInLevelAdditiveContentButtons();
        });
    }

    /// <summary>
    /// Updates the interactive state of buttons related to in-level additive content.
    /// </summary>
    private void UpdateInLevelAdditiveContentButtons()
    {
        // Check actual scene manager state, not just internal flag.
        bool isContentCurrentlyLoaded = SceneManager.GetSceneByName(inLevelAdditiveContentSceneName).isLoaded;
        _isInLevelAdditiveContentLoaded = isContentCurrentlyLoaded; // Sync internal flag

        if (loadAdditiveContentButton != null)
        {
            loadAdditiveContentButton.interactable = !_isInLevelAdditiveContentLoaded;
        }
        if (unloadAdditiveContentButton != null)
        {
            unloadAdditiveContentButton.interactable = _isInLevelAdditiveContentLoaded;
        }
    }

    /// <summary>
    /// Updates the TextMeshPro UI element to show which scenes are currently loaded in the game.
    /// </summary>
    private void UpdateUIInfo()
    {
        if (currentSceneInfoText != null)
        {
            string info = "Current Loaded Scenes:\n";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                info += $"- {s.name} (Active: {s == SceneManager.GetActiveScene()})\n";
            }
            currentSceneInfoText.text = info;
        }
    }
}
```