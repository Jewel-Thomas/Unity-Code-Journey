// Unity Design Pattern Example: LoadingScreenSystem
// This script demonstrates the LoadingScreenSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust and practical `LoadingScreenSystem` pattern in Unity using C#. It uses a singleton pattern for easy global access, Coroutines for asynchronous operations, and provides methods to manage UI elements like a progress bar and text.

---

### **1. Unity Setup Instructions:**

Before using the script, set up your Unity project as follows:

1.  **Create a New Scene (or use an existing one):** This will be your "Main Menu" or "Bootstrap" scene.
2.  **Create a Canvas:**
    *   Right-click in the Hierarchy -> `UI` -> `Canvas`.
    *   Set its `Render Mode` to `Screen Space - Overlay`.
    *   Set `UI Scale Mode` to `Scale With Screen Size` (e.g., Reference Resolution: 1920x1080).
3.  **Create a UI Panel for the Loading Screen:**
    *   Right-click on the `Canvas` -> `UI` -> `Panel`.
    *   Rename it to `LoadingScreenPanel`.
    *   Set its `Rect Transform` anchors to stretch across the whole screen (hold `Alt` while clicking the anchor preset, choose the bottom-right stretch option).
    *   Adjust its `Image` component (e.g., set color to black or a suitable background).
    *   **Crucially, uncheck its `GameObject` in the Inspector to make it inactive by default.**
4.  **Add a Progress Slider:**
    *   Right-click on `LoadingScreenPanel` -> `UI` -> `Slider`.
    *   Rename it to `LoadingProgressBar`.
    *   Adjust its `Rect Transform` for desired position (e.g., bottom-middle).
    *   Remove the `Handle Slide Area` child if you only want a progress bar (not user-interactive).
    *   Ensure the `Fill Area` and `Fill` are correctly configured to show progress.
5.  **Add a Progress Text:**
    *   Right-click on `LoadingScreenPanel` -> `UI` -> `Text - TextMeshPro` (recommended, import TMP Essentials if prompted, or use Legacy Text).
    *   Rename it to `LoadingProgressText`.
    *   Adjust its `Rect Transform` (e.g., above the slider).
    *   Set `Font Size` and `Color` for visibility.
    *   You can set initial text like "Loading..." or leave it blank.
6.  **Create an Empty GameObject:**
    *   Right-click in the Hierarchy -> `Create Empty`.
    *   Rename it to `LoadingScreenManager`.
7.  **Attach the Script:**
    *   Create a new C# script named `LoadingScreenManager.cs` (matching the class name).
    *   Copy and paste the code below into this script.
    *   Drag and drop the `LoadingScreenManager.cs` script onto the `LoadingScreenManager` GameObject in the Hierarchy.
8.  **Assign References in Inspector:**
    *   Select the `LoadingScreenManager` GameObject.
    *   Drag `LoadingScreenPanel` (the inactive GameObject) to the `Loading Screen Root` field.
    *   Drag `LoadingProgressBar` to the `Progress Slider` field.
    *   Drag `LoadingProgressText` to the `Progress Text` field.
9.  **Create other scenes:** For the example to work, create two more scenes named "GameScene" and "AnotherScene" and add them to `File -> Build Settings...`.

---

### **2. `LoadingScreenManager.cs` Script:**

This script implements the `LoadingScreenSystem` pattern.

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // For potential future use, e.g., list of tips

/// <summary>
///     The LoadingScreenSystem design pattern provides a centralized, reusable, and
///     configurable way to manage loading screens in a Unity application.
///
///     Key aspects of the pattern:
///     1.  Singleton Access: A single instance globally accessible (LoadingScreenManager.Instance).
///     2.  Encapsulation: Manages all loading screen UI, progress updates, and transitions internally.
///     3.  Asynchronous Operations: Integrates seamlessly with Unity's async operations (e.g., SceneManager.LoadSceneAsync).
///     4.  Extensibility: Can be easily extended to handle various types of loading (scenes, assets, network data).
///     5.  Decoupling: Other parts of the application don't need to know the specifics of how the loading screen works,
///         only that they can request a load operation.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // A static reference to the single instance of the LoadingScreenManager.
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The root GameObject of your loading screen UI (e.g., a Panel). Must be inactive by default.")]
    [SerializeField] private GameObject loadingScreenRoot;

    [Tooltip("The UI Slider to display loading progress.")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("The UI Text to display loading messages or percentage.")]
    [SerializeField] private TMPro.TextMeshProUGUI progressText; // Using TextMeshProUGUI, change to UnityEngine.UI.Text if not using TMP

    [Header("Settings")]
    [Tooltip("Minimum time (in seconds) the loading screen will be displayed, even if loading is fast. Prevents flickering.")]
    [SerializeField] private float minDisplayTime = 1.0f;

    // Internal state variables
    private float currentProgress;
    private bool isLoading;
    private Coroutine activeLoadingCoroutine;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern: ensures only one instance exists.
    /// Sets up the loading screen's initial state.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Debug.LogWarning("LoadingScreenManager: Duplicate instance detected, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this; // Set this as the singleton instance
        DontDestroyOnLoad(gameObject); // Keep the manager persistent across scenes

        // Ensure the loading screen UI is hidden at start.
        if (loadingScreenRoot != null)
        {
            loadingScreenRoot.SetActive(false);
        }
        else
        {
            Debug.LogError("LoadingScreenManager: Loading Screen Root is not assigned! Please assign it in the Inspector.");
        }

        // Initialize UI components
        if (progressSlider != null) progressSlider.value = 0;
        if (progressText != null) progressText.text = "Loading...";

        isLoading = false;
    }

    /// <summary>
    /// Updates the UI elements based on the current loading progress.
    /// This method can be called externally or internally during a loading operation.
    /// </summary>
    /// <param name="progress">The current loading progress (0.0 to 1.0).</param>
    /// <param name="message">An optional message to display (e.g., "Loading Assets...", "Initializing Game...").</param>
    private void UpdateProgressUI(float progress, string message = null)
    {
        currentProgress = Mathf.Clamp01(progress); // Ensure progress is between 0 and 1

        if (progressSlider != null)
        {
            progressSlider.value = currentProgress;
        }

        if (progressText != null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                progressText.text = message;
            }
            else
            {
                // Display percentage if no specific message is provided
                progressText.text = $"Loading... {Mathf.RoundToInt(currentProgress * 100)}%";
            }
        }
    }

    /// <summary>
    /// Public method to start loading a new scene asynchronously with a loading screen.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("LoadingScreenManager: Already loading a scene or operation. Request ignored.");
            return;
        }

        activeLoadingCoroutine = StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    /// <summary>
    /// Public method to start a custom asynchronous operation with a loading screen.
    /// </summary>
    /// <param name="customOperation">An IEnumerator representing the custom loading steps.</param>
    public void PerformCustomLoading(IEnumerator customOperation)
    {
        if (isLoading)
        {
            Debug.LogWarning("LoadingScreenManager: Already loading a scene or operation. Request ignored.");
            return;
        }

        activeLoadingCoroutine = StartCoroutine(PerformCustomLoadingAsyncCoroutine(customOperation));
    }

    /// <summary>
    /// The core Coroutine for loading a scene asynchronously.
    /// This handles showing the UI, updating progress, and hiding the UI.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        isLoading = true;
        ShowLoadingScreen(); // Activate the loading screen UI

        float startTime = Time.realtimeSinceStartup; // For minDisplayTime calculation

        // Begin loading the scene asynchronously
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; // Prevent scene from activating immediately

        // While the scene is not fully loaded
        while (!operation.isDone)
        {
            // Calculate progress (operation.progress goes from 0.0 to 0.9 when ready, then jumps to 1.0)
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            UpdateProgressUI(progress, "Loading " + sceneName + "...");

            // If loading is nearly complete (0.9 means it's ready to activate)
            if (operation.progress >= 0.9f)
            {
                // Wait for the minimum display time
                float elapsedTime = Time.realtimeSinceStartup - startTime;
                if (elapsedTime < minDisplayTime)
                {
                    float timeRemaining = minDisplayTime - elapsedTime;
                    // Gradually fill the remaining progress to indicate finalization
                    float fillProgress = Mathf.Lerp(progress, 1.0f, timeRemaining / minDisplayTime);
                    UpdateProgressUI(fillProgress, "Preparing game...");
                    yield return null; // Wait one frame
                    continue; // Check time again
                }
                
                UpdateProgressUI(1.0f, "Entering " + sceneName + "...");
                yield return new WaitForSeconds(0.5f); // Small pause before activation

                operation.allowSceneActivation = true; // Allow the scene to fully activate
            }

            yield return null; // Wait for the next frame
        }

        // Once operation.isDone is true, the new scene is active.
        HideLoadingScreen(); // Deactivate the loading screen UI
        isLoading = false;
        activeLoadingCoroutine = null;
    }

    /// <summary>
    /// Coroutine to perform a generic custom asynchronous operation.
    /// This wraps any IEnumerator with the loading screen UI.
    /// The custom operation is responsible for yielding and potentially updating progress.
    /// </summary>
    /// <param name="customOperation">An IEnumerator representing the custom loading steps.</param>
    private IEnumerator PerformCustomLoadingAsyncCoroutine(IEnumerator customOperation)
    {
        isLoading = true;
        ShowLoadingScreen(); // Activate the loading screen UI

        float startTime = Time.realtimeSinceStartup; // For minDisplayTime calculation

        // Run the custom operation
        while (customOperation.MoveNext())
        {
            // If the custom operation yields an object that can be cast to float,
            // we'll assume it's progress. Otherwise, just update with a generic message.
            if (customOperation.Current is float progressValue)
            {
                UpdateProgressUI(progressValue, "Processing...");
            }
            else
            {
                // Generic update if custom operation doesn't provide progress directly
                UpdateProgressUI(Mathf.Lerp(currentProgress, 0.9f, Time.deltaTime * 0.5f), "Processing...");
            }
            yield return customOperation.Current; // Yield whatever the custom operation yields
        }

        // Ensure minimum display time is met
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        if (elapsedTime < minDisplayTime)
        {
            float timeRemaining = minDisplayTime - elapsedTime;
            UpdateProgressUI(1.0f, "Finalizing...");
            yield return new WaitForSeconds(timeRemaining);
        }

        HideLoadingScreen(); // Deactivate the loading screen UI
        isLoading = false;
        activeLoadingCoroutine = null;
    }

    /// <summary>
    /// Activates the loading screen UI.
    /// </summary>
    private void ShowLoadingScreen()
    {
        if (loadingScreenRoot != null)
        {
            loadingScreenRoot.SetActive(true);
            UpdateProgressUI(0f, "Loading..."); // Reset progress at start
            Debug.Log("LoadingScreenManager: Showing loading screen.");
        }
    }

    /// <summary>
    /// Deactivates the loading screen UI.
    /// </summary>
    private void HideLoadingScreen()
    {
        if (loadingScreenRoot != null)
        {
            loadingScreenRoot.SetActive(false);
            Debug.Log("LoadingScreenManager: Hiding loading screen.");
        }
    }

    // --- Example Custom Loading Operations ---
    // These are examples that can be passed to PerformCustomLoading.

    /// <summary>
    /// Example: Simulates loading multiple assets or performing several initialization steps.
    /// This method yields progress floats, which our PerformCustomLoadingAsyncCoroutine can pick up.
    /// </summary>
    /// <returns>An IEnumerator for a custom loading routine.</returns>
    public IEnumerator SimulateLongAssetLoading()
    {
        Debug.Log("Simulating long asset loading...");
        int totalSteps = 5;
        for (int i = 0; i < totalSteps; i++)
        {
            // Simulate some work being done
            yield return new WaitForSeconds(0.7f); // Simulate network delay or heavy processing

            float progress = (float)(i + 1) / totalSteps;
            Debug.Log($"Simulated Asset Loading Progress: {progress * 100}%");
            yield return progress; // Yield the progress for the manager to update UI
        }
        Debug.Log("Simulated asset loading complete.");
    }

    /// <summary>
    /// Example: Simulates game initialization steps.
    /// </summary>
    public IEnumerator SimulateGameInitialization()
    {
        Debug.Log("Simulating game initialization...");
        yield return new WaitForSeconds(1.0f); // Initialize player data
        Debug.Log("Player data initialized.");
        yield return 0.3f;

        yield return new WaitForSeconds(0.8f); // Load game settings
        Debug.Log("Game settings loaded.");
        yield return 0.6f;

        yield return new WaitForSeconds(1.2f); // Prepare world generation
        Debug.Log("World generation prepared.");
        yield return 0.9f;

        yield return new WaitForSeconds(0.5f); // Final checks
        Debug.Log("Final checks complete.");
        yield return 1.0f;
    }
}
```

---

### **3. Example Usage (How to call the LoadingScreenManager):**

To demonstrate how to use `LoadingScreenManager`, create another script (e.g., `MainMenuController.cs`) and attach it to an empty GameObject in your "Main Menu" or starting scene.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Required for Coroutines

/// <summary>
///     Example usage script for the LoadingScreenManager.
///     This simulates a Main Menu or similar scene that triggers various loading operations.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI Buttons for Demonstration")]
    [SerializeField] private Button loadGameSceneButton;
    [SerializeField] private Button loadAnotherSceneButton;
    [SerializeField] private Button performCustomLoadButton;

    void Start()
    {
        // Add listeners to the buttons to trigger loading operations
        if (loadGameSceneButton != null)
        {
            loadGameSceneButton.onClick.AddListener(OnLoadGameSceneClicked);
        }
        if (loadAnotherSceneButton != null)
        {
            loadAnotherSceneButton.onClick.AddListener(OnLoadAnotherSceneClicked);
        }
        if (performCustomLoadButton != null)
        {
            performCustomLoadButton.onClick.AddListener(OnPerformCustomLoadClicked);
        }
    }

    // --- Example Scene Loading ---
    private void OnLoadGameSceneClicked()
    {
        Debug.Log("MainMenuController: Requesting to load 'GameScene'...");
        // Call the LoadingScreenManager's method to load a scene
        LoadingScreenManager.Instance.LoadScene("GameScene");
    }

    private void OnLoadAnotherSceneClicked()
    {
        Debug.Log("MainMenuController: Requesting to load 'AnotherScene'...");
        // Call the LoadingScreenManager's method to load another scene
        LoadingScreenManager.Instance.LoadScene("AnotherScene");
    }

    // --- Example Custom Operation Loading ---
    private void OnPerformCustomLoadClicked()
    {
        Debug.Log("MainMenuController: Requesting to perform a custom loading operation...");
        // Call the LoadingScreenManager's method to perform a custom Coroutine
        // You can pass any IEnumerator here.
        LoadingScreenManager.Instance.PerformCustomLoading(LoadingScreenManager.Instance.SimulateLongAssetLoading());
        // Or: LoadingScreenManager.Instance.PerformCustomLoading(LoadingScreenManager.Instance.SimulateGameInitialization());
        // Or: LoadingScreenManager.Instance.PerformCustomLoading(MyCustomLoadingRoutine());
    }

    // You can define your own custom loading routines here if needed
    private IEnumerator MyCustomLoadingRoutine()
    {
        Debug.Log("Starting my custom loading routine...");
        yield return new WaitForSeconds(1.0f);
        Debug.Log("Step 1 complete.");
        yield return 0.25f; // Update progress

        yield return new WaitForSeconds(0.5f);
        Debug.Log("Step 2 complete.");
        yield return 0.75f; // Update progress

        yield return new WaitForSeconds(0.7f);
        Debug.Log("My custom routine finished.");
        yield return 1.0f; // Update progress
    }
}
```

**Setup for `MainMenuController`:**

1.  In your starting scene (e.g., `Main Menu`), add 3 UI Buttons (Right-click on Canvas -> UI -> Button - TextMeshPro).
2.  Rename them, for example: `LoadGameButton`, `LoadAnotherButton`, `CustomLoadButton`.
3.  Change their text to "Load Game Scene", "Load Another Scene", "Perform Custom Load".
4.  Create an empty GameObject named `MainMenuController` and attach the `MainMenuController.cs` script to it.
5.  Drag your three UI Buttons from the Hierarchy to the corresponding `Button` fields in the `MainMenuController` component in the Inspector.

---

### **How the LoadingScreenSystem Pattern Works (Detailed Explanation):**

1.  **`LoadingScreenManager` (The System Core):**
    *   **Singleton (`Instance` property and `Awake()`):** This ensures there's only one `LoadingScreenManager` in your game at any time. The `Awake()` method checks if an instance already exists; if so, it destroys itself. Otherwise, it sets itself as the `Instance`. `DontDestroyOnLoad(gameObject)` keeps it active across scene changes, which is crucial for a persistent loading system.
    *   **UI References (`[SerializeField]`):** `loadingScreenRoot`, `progressSlider`, `progressText` are exposed in the Inspector. This allows you to hook up your specific UI elements without modifying the code. The `loadingScreenRoot` is usually a Panel that covers the entire screen and is initially inactive.
    *   **`UpdateProgressUI(float progress, string message)`:** This private helper method is responsible for updating the UI elements. It clamps the progress to 0-1, sets the slider's value, and updates the text, optionally displaying a custom message or a percentage.
    *   **`ShowLoadingScreen()` & `HideLoadingScreen()`:** Simple methods to activate and deactivate the `loadingScreenRoot` GameObject, making the loading screen visible or invisible. They also reset the progress when shown.
    *   **`LoadScene(string sceneName)` (Public API):** This is the primary public method to trigger a scene load. It checks if another loading operation is already active to prevent conflicts. It then starts a Coroutine (`LoadSceneAsyncCoroutine`) to handle the actual loading process.
    *   **`PerformCustomLoading(IEnumerator customOperation)` (Public API):** A more generic public method that allows you to wrap *any* `IEnumerator` (a Unity Coroutine) with the loading screen UI. This is highly flexible for tasks like loading assets, making network requests, or performing complex initializations that don't involve scene changes directly. It starts `PerformCustomLoadingAsyncCoroutine`.

2.  **Loading Coroutines (`LoadSceneAsyncCoroutine`, `PerformCustomLoadingAsyncCoroutine`):**
    *   **`LoadSceneAsyncCoroutine(string sceneName)`:**
        *   Sets `isLoading = true` to prevent concurrent loads.
        *   Calls `ShowLoadingScreen()`.
        *   Initiates `SceneManager.LoadSceneAsync(sceneName)`.
        *   `operation.allowSceneActivation = false;`: This is important! It loads the scene in the background but prevents it from fully activating until you're ready. This allows you to show "Loading X%" even when the new scene is technically ready at 90% progress.
        *   **Progress Loop (`while (!operation.isDone)`):** Continuously checks the `operation.progress` (which goes from 0.0 to 0.9 for background loading).
        *   **Progress Mapping:** `Mathf.Clamp01(operation.progress / 0.9f)` maps the 0-0.9 range to 0-1 for a smoother progress bar.
        *   **`minDisplayTime` Logic:** If `operation.progress` reaches 0.9f (meaning the scene is fully loaded but not yet activated), it checks if the `minDisplayTime` has passed. If not, it waits for the remaining time, preventing the loading screen from just flashing.
        *   Once `minDisplayTime` is met and progress is at 0.9f, it sets `operation.allowSceneActivation = true;`, allowing the new scene to become active.
        *   After `operation.isDone` becomes true (meaning the new scene is fully loaded and active), it calls `HideLoadingScreen()` and resets `isLoading`.
    *   **`PerformCustomLoadingAsyncCoroutine(IEnumerator customOperation)`:**
        *   Similar setup with `isLoading = true` and `ShowLoadingScreen()`.
        *   It then runs the `customOperation` provided. The `while (customOperation.MoveNext())` loop executes each step of the custom Coroutine.
        *   If the `customOperation` `yield return`s a `float`, this is interpreted as a progress update, allowing the custom operation to directly control the progress bar. Otherwise, it shows a generic "Processing..." message.
        *   It also incorporates the `minDisplayTime` logic to ensure the loading screen doesn't disappear too quickly.
        *   Finally, `HideLoadingScreen()` and `isLoading` reset.

3.  **Example Custom Operations (`SimulateLongAssetLoading`, `SimulateGameInitialization`):**
    *   These are `IEnumerator` methods within `LoadingScreenManager` itself (though they could be in any other script) that demonstrate how a long-running task can be structured.
    *   They use `yield return new WaitForSeconds(...)` to simulate work or delays.
    *   Crucially, they use `yield return progress;` (a float) to update the loading bar during their execution, showcasing how a custom routine can communicate its progress back to the manager.

4.  **`MainMenuController` (Example Consumer):**
    *   This script acts as an entry point in your game.
    *   It uses buttons to trigger loading operations.
    *   Notice that it simply calls `LoadingScreenManager.Instance.LoadScene("SceneName")` or `LoadingScreenManager.Instance.PerformCustomLoading(...)`. It *doesn't* need to know how the loading screen works internally, or how scenes are loaded, or how progress is calculated. This demonstrates the **decoupling** benefit of the pattern.

**Benefits of this Design Pattern:**

*   **Centralized Control:** All loading screen logic is in one place, making it easier to manage, update, and debug.
*   **Reusability:** The same loading screen system can be used for any loading operation throughout your game (scene changes, asset loading, network data, etc.).
*   **Consistency:** Ensures a consistent user experience for loading across your entire application.
*   **Decoupling:** Game logic that needs to load something doesn't need to know the UI details of the loading screen. It just tells the `LoadingScreenManager` what to load.
*   **Maintainability:** Changes to the loading screen UI or loading logic only affect the `LoadingScreenManager`, not every script that triggers a load.
*   **Flexibility:** Easily extensible for features like loading tips, different loading animations, or custom progress visualizations.