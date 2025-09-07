// Unity Design Pattern Example: AsyncSceneLoading
// This script demonstrates the AsyncSceneLoading pattern in Unity
// Generated automatically - ready to use in your Unity project

The `AsyncSceneLoading` design pattern in Unity allows you to load scenes in the background without freezing the main thread, providing a much smoother user experience. This is crucial for large scenes or games with complex initializations.

This example demonstrates a complete implementation of this pattern, including a `LoadingManager` singleton, a dedicated `LoadingScene` with UI, and how to initiate a load from a `MainMenuScene`.

---

## AsyncSceneLoading Design Pattern: Unity Example

**Concept:**
The core idea is to separate the scene loading process from the user interface. When a new scene is requested, instead of loading it directly, we first load a *dedicated loading scene*. This loading scene then displays a progress bar while the *actual target scene* is loaded asynchronously in the background. Once the target scene is mostly loaded (usually 90%), we can choose to immediately activate it, or wait for user input (e.g., "Press any key to continue") to ensure the loading screen is displayed for a minimum duration.

**Benefits:**
*   **Smooth User Experience:** Prevents the game from freezing while a large scene loads.
*   **Progress Feedback:** Allows displaying a progress bar, making the wait less frustrating.
*   **Controlled Transitions:** Gives developers control over when the new scene becomes active, allowing for custom animations or minimum display times.
*   **Resource Management:** Can offload heavy initialization tasks to a loading scene, keeping main gameplay scenes lean.

---

### Project Setup Steps:

1.  **Create a New Unity Project.**
2.  **Install TextMeshPro Essentials:** In Unity, go to `Window > TextMeshPro > Import TMP Essential Resources`. This is needed for `TextMeshProUGUI`.
3.  **Create Three Scenes:**
    *   `MainMenuScene`
    *   `LoadingScene`
    *   `GameScene` (or any name for your target scene)
4.  **Add Scenes to Build Settings:** Go to `File > Build Settings...` and drag all three scenes into the "Scenes In Build" list. Ensure they are in this order: `MainMenuScene`, `LoadingScene`, `GameScene`.
5.  **Create C# Scripts:** Create the following three C# scripts in your `Assets/Scripts` folder:
    *   `LoadingManager.cs`
    *   `LoadingScreenUI.cs`
    *   `MainMenuController.cs`

---

### 1. `LoadingManager.cs` (The Core Logic - Singleton)

This script is the heart of the asynchronous loading system. It's a `DontDestroyOnLoad` singleton, meaning it will persist across scene changes and can be accessed from anywhere. It handles initiating the loading of a target scene, tracks its progress, and provides events for the UI to subscribe to.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager operations
using System.Collections;         // Required for Coroutines
using System;                     // Required for Action (events)

/// <summary>
/// A singleton manager responsible for handling asynchronous scene loading.
/// It persists across scenes and orchestrates the loading process, including
/// displaying a loading screen and providing progress updates.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static LoadingManager Instance { get; private set; }

    // --- Events for UI Updates ---
    // Action to notify subscribers (e.g., LoadingScreenUI) about loading progress.
    public event Action<float> OnProgressUpdate;
    // Action to notify when the scene is fully loaded (90%) but not yet activated.
    public event Action OnSceneReadyToActivate;
    // Action to notify when the target scene has been fully activated and is ready.
    public event Action OnLoadingComplete;

    // --- Editor-Configurable Settings ---
    [Header("Loading Settings")]
    [Tooltip("Minimum time the loading screen will be displayed, even if loading finishes earlier.")]
    [SerializeField] private float minDisplayTime = 2.0f; 

    [Tooltip("Key to press to activate the scene after loading is 90% complete and minDisplayTime has passed. " +
             "Set to KeyCode.None for automatic activation.")]
    [SerializeField] private KeyCode activationKey = KeyCode.Space; 

    [Tooltip("Name of the dedicated loading scene. This scene should contain the LoadingScreenUI.")]
    [SerializeField] private string loadingSceneName = "LoadingScene"; 

    // --- Internal State Variables ---
    private string _targetSceneName;      // The name of the scene to be loaded asynchronously.
    private AsyncOperation _asyncOperation; // The Unity operation tracking the asynchronous scene load.
    private float _currentDisplayedProgress; // The smoothed progress value displayed on the UI.
    private bool _isLoading;              // Flag to prevent multiple concurrent loads.
    private bool _isReadyToActivate;      // Flag indicating the target scene is 90% loaded.

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement the Singleton pattern to ensure only one instance exists.
        if (Instance == null)
        {
            Instance = this;
            // Prevent this GameObject from being destroyed when loading new scenes.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another instance already exists, destroy this one.
            Destroy(gameObject);
        }
    }

    // --- Public API for Initiating Scene Loads ---

    /// <summary>
    /// Initiates the asynchronous loading of a new scene.
    /// This method first loads the dedicated 'LoadingScene', which then triggers the
    /// actual target scene loading via StartAsyncLoadingProcess().
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void LoadScene(string sceneName)
    {
        if (_isLoading)
        {
            Debug.LogWarning("LoadingManager: Already loading a scene. Cannot start a new load for " + sceneName);
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("LoadingManager: Target scene name cannot be null or empty.");
            return;
        }

        _targetSceneName = sceneName;
        _isLoading = true;
        _isReadyToActivate = false;
        _currentDisplayedProgress = 0f; // Reset progress for the new load.

        Debug.Log($"LoadingManager: Requesting to load '{_targetSceneName}'. First loading '{loadingSceneName}'.");
        // Load the dedicated loading scene synchronously. This scene will then
        // find the LoadingManager and call StartAsyncLoadingProcess().
        SceneManager.LoadScene(loadingSceneName); 
    }

    /// <summary>
    /// This method is called by the 'LoadingScene' (specifically, the LoadingScreenUI script)
    /// once the loading screen is fully initialized and ready to display progress.
    /// It kicks off the actual asynchronous loading of the target scene.
    /// </summary>
    public void StartAsyncLoadingProcess()
    {
        if (string.IsNullOrEmpty(_targetSceneName))
        {
            Debug.LogError("LoadingManager: No target scene name set. Cannot start async loading process.");
            _isLoading = false;
            return;
        }

        Debug.Log($"LoadingManager: Starting async load for target scene: '{_targetSceneName}'");
        StartCoroutine(LoadSceneAsyncCoroutine(_targetSceneName));
    }

    // --- The Core Asynchronous Loading Coroutine ---

    /// <summary>
    /// Coroutine that handles the actual asynchronous loading of the target scene.
    /// It monitors progress, manages scene activation, and incorporates minimum display time
    /// and optional user input for activation.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load asynchronously.</param>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        // Yield a frame to ensure the LoadingScreenUI has completed its Start() method
        // and has subscribed to the events.
        yield return null; 

        // Start the asynchronous loading operation for the target scene.
        _asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        // Prevent the scene from automatically activating when it's 90% loaded.
        // We'll manually set allowSceneActivation to true later.
        _asyncOperation.allowSceneActivation = false; 

        float loadingStartTime = Time.time; // Record the start time for minDisplayTime calculation.
        float actualSceneLoadProgress = 0f; // Stores the raw progress from AsyncOperation.

        // Loop while the asynchronous operation is not yet complete.
        while (!_asyncOperation.isDone)
        {
            // Get the raw loading progress (clamped at 0.9 by Unity when allowSceneActivation is false).
            actualSceneLoadProgress = _asyncOperation.progress;

            // If the scene is 90% loaded, and we haven't already marked it as ready,
            // then set the flag and trigger the OnSceneReadyToActivate event.
            if (actualSceneLoadProgress >= 0.9f && !_isReadyToActivate)
            {
                _isReadyToActivate = true;
                OnSceneReadyToActivate?.Invoke(); // Notify UI to show "Press key to continue" prompt.
            }

            // Smoothly interpolate the displayed progress towards the actual progress.
            // This makes the progress bar move more fluidly and less 'jumpy'.
            // The 0.9f scaling ensures the bar visually reaches 100% only when activation is ready.
            _currentDisplayedProgress = Mathf.Lerp(_currentDisplayedProgress, actualSceneLoadProgress / 0.9f, Time.deltaTime * 3f);
            // Ensure progress doesn't go over 1.0 (for display purposes).
            _currentDisplayedProgress = Mathf.Clamp09(_currentDisplayedProgress);

            // Notify all subscribers about the current progress.
            OnProgressUpdate?.Invoke(_currentDisplayedProgress);

            // Check if the minimum display time for the loading screen has passed.
            bool minTimePassed = (Time.time - loadingStartTime) >= minDisplayTime;
            
            // If the scene is 90% loaded AND the minimum display time has passed...
            if (_isReadyToActivate && minTimePassed)
            {
                // ...then check for activation conditions:
                // If activationKey is None (auto-activate) OR the activation key is pressed...
                if (activationKey == KeyCode.None || Input.GetKeyDown(activationKey))
                {
                    Debug.Log($"LoadingManager: Activating scene '{sceneName}'.");
                    _asyncOperation.allowSceneActivation = true; // Allow the scene to become active.
                }
            }

            yield return null; // Wait for the next frame before continuing the loop.
        }

        // Once _asyncOperation.isDone is true, the scene is fully loaded and active.
        Debug.Log($"LoadingManager: Target scene '{sceneName}' fully loaded and active.");

        // Reset state for future loads.
        _isLoading = false;
        _isReadyToActivate = false;
        _currentDisplayedProgress = 1f; // Ensure progress bar shows 100%.

        OnProgressUpdate?.Invoke(1f); // Final progress update.
        OnLoadingComplete?.Invoke();  // Notify that loading is fully complete.
        
        _targetSceneName = null; // Clear the target scene name.
    }
}
```

---

### 2. `LoadingScreenUI.cs` (Handles Loading Scene UI)

This script is placed on a GameObject within the `LoadingScene`. It subscribes to the `LoadingManager`'s events to update UI elements (like a progress bar and text) and to show/hide a "Press any key" prompt.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Slider and Text
using TMPro;          // Required for TextMeshProUGUI (recommended for UI Text)

/// <summary>
/// Manages the UI elements displayed on the dedicated loading screen.
/// It subscribes to events from the LoadingManager to update progress and messages.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    // --- UI Element References ---
    [Header("UI References")]
    [Tooltip("The Slider UI component to display loading progress.")]
    [SerializeField] private Slider progressBar;
    
    [Tooltip("The TextMeshProUGUI component to display loading percentage.")]
    [SerializeField] private TextMeshProUGUI progressText; // Using TextMeshPro for better text rendering
    
    [Tooltip("The GameObject containing the 'Press any key to continue' prompt.")]
    [SerializeField] private GameObject pressKeyPrompt;

    // --- MonoBehaviour Lifecycle ---
    private void Start()
    {
        // Ensure the LoadingManager instance exists before subscribing.
        if (LoadingManager.Instance == null)
        {
            Debug.LogError("LoadingScreenUI: LoadingManager not found! Make sure it's present " +
                           "in the initial scene and configured as a DontDestroyOnLoad object.");
            return;
        }

        // --- Subscribe to LoadingManager Events ---
        LoadingManager.Instance.OnProgressUpdate += UpdateProgressBar;
        LoadingManager.Instance.OnSceneReadyToActivate += ShowPressKeyPrompt;
        LoadingManager.Instance.OnLoadingComplete += OnLoadingFinished;

        // --- Initialize UI State ---
        UpdateProgressBar(0f); // Start with 0% progress.
        if (pressKeyPrompt != null)
        {
            pressKeyPrompt.SetActive(false); // Hide the prompt initially.
        }

        // --- Initiate the Async Loading Process ---
        // After the LoadingScene's UI is set up, tell the LoadingManager to start
        // loading the actual target scene in the background.
        LoadingManager.Instance.StartAsyncLoadingProcess();
    }

    private void OnDestroy()
    {
        // --- Unsubscribe from LoadingManager Events ---
        // It's crucial to unsubscribe to prevent potential memory leaks or null reference errors
        // if this object is destroyed but the LoadingManager persists.
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.OnProgressUpdate -= UpdateProgressBar;
            LoadingManager.Instance.OnSceneReadyToActivate -= ShowPressKeyPrompt;
            LoadingManager.Instance.OnLoadingComplete -= OnLoadingFinished;
        }
    }

    // --- Event Handlers ---

    /// <summary>
    /// Updates the UI progress bar and text based on the received progress value.
    /// </summary>
    /// <param name="progress">The current loading progress (0.0 to 1.0).</param>
    private void UpdateProgressBar(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress; // Update slider value.
        }
        if (progressText != null)
        {
            // Format progress as a percentage.
            progressText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    /// <summary>
    /// Activates the "Press any key to continue" prompt when the target scene is 90% loaded.
    /// </summary>
    private void ShowPressKeyPrompt()
    {
        if (pressKeyPrompt != null)
        {
            pressKeyPrompt.SetActive(true);
            Debug.Log("LoadingScreenUI: Scene is ready to activate. Press the specified key.");
        }
    }

    /// <summary>
    /// Called when the target scene has been fully loaded and activated.
    /// At this point, the LoadingScene is usually about to be unloaded itself.
    /// </summary>
    private void OnLoadingFinished()
    {
        Debug.Log("LoadingScreenUI: Loading process complete.");
        // Optional: Add a fade-out animation or other post-loading effects here
        // before the LoadingScene is replaced by the target scene.
    }
}
```

---

### 3. `MainMenuController.cs` (Initiates Scene Load)

This script is typically attached to a UI button in your `MainMenuScene`. When the button is clicked, it uses the `LoadingManager` to initiate the loading process of the `GameScene`.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button component

/// <summary>
/// A simple controller for the Main Menu scene that demonstrates how to initiate
/// an asynchronous scene load using the LoadingManager.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load when the 'Start Game' button is clicked.")]
    [SerializeField] private string gameSceneName = "GameScene"; 

    [Header("UI References")]
    [Tooltip("The UI Button that triggers the game start.")]
    [SerializeField] private Button startGameButton;

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Add a listener to the button's onClick event.
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            Debug.LogError("MainMenuController: Start Game Button is not assigned in the Inspector.");
        }

        // Ensure the LoadingManager is initialized if it's placed in the first scene.
        // This is important if LoadingManager is not a prefab and instantiated on demand,
        // but for a DontDestroyOnLoad singleton, it should already be ready if in the first scene.
        if (LoadingManager.Instance == null)
        {
            Debug.LogWarning("MainMenuController: LoadingManager not found. Ensure it's in the scene or an existing DontDestroyOnLoad object.");
            // Optionally, instantiate it if it's a prefab
            // GameObject loadingManagerPrefab = Resources.Load<GameObject>("LoadingManagerPrefab");
            // if (loadingManagerPrefab != null) Instantiate(loadingManagerPrefab);
        }
    }

    private void OnDestroy()
    {
        // Remove the listener to prevent memory leaks when this GameObject is destroyed.
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }
    }

    // --- Event Handler ---

    /// <summary>
    /// Called when the "Start Game" button is clicked.
    /// It instructs the LoadingManager to begin loading the target game scene.
    /// </summary>
    private void OnStartGameClicked()
    {
        Debug.Log("MainMenuController: 'Start Game' button clicked.");
        if (LoadingManager.Instance != null)
        {
            // Use the LoadingManager to start the asynchronous scene loading process.
            LoadingManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("MainMenuController: LoadingManager instance is not available. Cannot load scene asynchronously.");
            // Fallback: Directly load the scene (not recommended for async pattern)
            // SceneManager.LoadScene(gameSceneName);
        }
    }
}
```

---

### Scene Configuration Guide:

#### 1. `MainMenuScene` Setup:

*   Create an empty GameObject named `_Managers`. Drag the `LoadingManager.cs` script onto it.
    *   **Inspector for `_Managers` (with `LoadingManager` component):**
        *   `Min Display Time`: `3` (seconds)
        *   `Activation Key`: `Space` (or `None` for automatic)
        *   `Loading Scene Name`: `LoadingScene` (ensure this matches your scene name)
*   Create a UI Canvas (`GameObject > UI > Canvas`).
*   Inside the Canvas, create a Button (`GameObject > UI > Button - TextMeshPro`).
    *   Change the button's text to "Start Game".
*   Create an empty GameObject named `MainMenuController`. Drag the `MainMenuController.cs` script onto it.
    *   **Inspector for `MainMenuController`:**
        *   `Game Scene Name`: `GameScene` (ensure this matches your target scene name)
        *   Drag your "Start Game" Button from the Hierarchy to the `Start Game Button` field.

#### 2. `LoadingScene` Setup:

*   Create a UI Canvas (`GameObject > UI > Canvas`).
*   Inside the Canvas, create a Slider (`GameObject > UI > Slider`).
    *   Rename it to `ProgressBar`.
    *   Set `Value` to `0`, `Min Value` to `0`, `Max Value` to `1`.
    *   Disable `Interactable`.
*   Inside the Canvas, create a Text (`GameObject > UI > Text - TextMeshPro`).
    *   Rename it to `ProgressText`.
    *   Set its initial text to "Loading..."
    *   Adjust font size and position as desired.
*   Inside the Canvas, create another Text (`GameObject > UI > Text - TextMeshPro`).
    *   Rename it to `PressKeyPrompt`.
    *   Set its initial text to "Press SPACE to continue..." (or whatever your `activationKey` is).
    *   Initially **disable this GameObject** in the Inspector.
*   Create an empty GameObject named `LoadingScreenUI`. Drag the `LoadingScreenUI.cs` script onto it.
    *   **Inspector for `LoadingScreenUI`:**
        *   Drag the `ProgressBar` Slider from the Hierarchy to the `Progress Bar` field.
        *   Drag the `ProgressText` TextMeshPro object to the `Progress Text` field.
        *   Drag the `PressKeyPrompt` GameObject to the `Press Key Prompt` field.

#### 3. `GameScene` Setup:

*   Simply add some unique objects to this scene (e.g., a Cube, a Sphere, a directional light) so you can clearly see when it has loaded. This scene doesn't require any specific scripts for this example.

---

### How to Run:

1.  Open the `MainMenuScene`.
2.  Press the Play button in the Unity Editor.
3.  Click the "Start Game" button.
4.  You should transition to the `LoadingScene`, see a progress bar update, and then be prompted to press `SPACE` (or automatically transition if `KeyCode.None` was chosen) to enter the `GameScene`.

This setup provides a robust and flexible asynchronous scene loading solution, ready for use in your Unity projects.