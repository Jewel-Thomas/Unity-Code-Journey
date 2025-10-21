// Unity Design Pattern Example: SplashScreenManager
// This script demonstrates the SplashScreenManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete `SplashScreenManager` script for Unity, designed to be practical, educational, and ready to use in your projects. It demonstrates how to sequence multiple splash screens, handle fading effects, and transition to a main game scene.

---

### `SplashScreenManager.cs` Script

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // Although not strictly needed for the array, good to include

/// <summary>
/// Serializable class to define properties for a single splash screen.
/// This allows us to configure multiple splash screens directly in the Unity Inspector.
/// </summary>
[System.Serializable]
public class SplashScreenData
{
    [Tooltip("The sprite (image) to display for this splash screen.")]
    public Sprite splashSprite;

    [Tooltip("How long this specific splash screen should be displayed (in seconds). " +
             "If 0, the manager's default display duration will be used.")]
    public float displayDuration = 0f;
}

/// <summary>
/// Manages the display of a sequence of splash screens before loading the main game scene.
/// This script implements the 'SplashScreenManager' design pattern, centralizing splash screen logic.
/// </summary>
public class SplashScreenManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // A common approach for managers to ensure there's only one instance
    // and provide a global access point.
    public static SplashScreenManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The CanvasGroup component that contains the splash screen UI elements. Used for fading the entire splash screen.")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    [Tooltip("The Image component used to display the splash screen sprites.")]
    [SerializeField] private Image splashImage;

    [Header("Splash Screen Configuration")]
    [Tooltip("An array of SplashScreenData objects, defining the sequence of splash screens.")]
    [SerializeField] private SplashScreenData[] splashScreens;

    [Tooltip("The default duration (in seconds) for which each splash screen is displayed if not specified individually.")]
    [SerializeField] private float defaultDisplayDuration = 3.0f;

    [Tooltip("The duration (in seconds) for the fade-in animation of each splash screen.")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("The duration (in seconds) for the fade-out animation of each splash screen.")]
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("Scene Transition")]
    [Tooltip("The name of the scene to load after all splash screens have been displayed.")]
    [SerializeField] private string nextSceneName = "MainMenu";

    // --- Internal State ---
    private bool isLoadingScene = false;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Implement the Singleton pattern:
        // If an instance already exists and it's not this one, destroy this one.
        // Otherwise, set this as the instance and prevent it from being destroyed on scene load.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Ensures the SplashScreenManager persists across the initial scene load
            // until it has finished its job and transitioned to the next scene.
            DontDestroyOnLoad(gameObject);
        }

        // Initialize UI state: Ensure the splash screen is initially hidden.
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f; // Fully transparent
            splashCanvasGroup.blocksRaycasts = false; // Does not block clicks/input
            splashCanvasGroup.interactable = false; // Not interactable
        }
    }

    private void Start()
    {
        // Start the splash screen sequence as soon as the manager is active.
        // It's good practice to call coroutines from Start or methods triggered by user input/game events.
        StartCoroutine(StartSplashSequence());
    }

    // --- Core Logic ---

    /// <summary>
    /// Coroutine to manage the entire splash screen display sequence.
    /// </summary>
    private IEnumerator StartSplashSequence()
    {
        // Basic validation to ensure we have necessary components and data.
        if (splashCanvasGroup == null || splashImage == null)
        {
            Debug.LogError("SplashScreenManager: Missing CanvasGroup or Image reference! Cannot display splash screens.", this);
            LoadNextScene(); // Attempt to load the next scene anyway to prevent being stuck.
            yield break; // Exit the coroutine
        }

        if (splashScreens == null || splashScreens.Length == 0)
        {
            Debug.LogWarning("SplashScreenManager: No splash screens configured. Loading next scene immediately.", this);
            LoadNextScene();
            yield break;
        }

        // Make sure the canvas group is ready to be shown
        splashCanvasGroup.blocksRaycasts = true;
        splashCanvasGroup.interactable = true;

        // Iterate through each configured splash screen data.
        foreach (SplashScreenData splashData in splashScreens)
        {
            // Set the sprite for the current splash screen.
            if (splashData.splashSprite != null)
            {
                splashImage.sprite = splashData.splashSprite;
                splashImage.gameObject.SetActive(true); // Ensure image is active
            }
            else
            {
                Debug.LogWarning($"SplashScreenManager: Splash screen data missing sprite. Skipping this splash screen.", this);
                continue; // Skip to the next splash screen
            }

            // Determine the display duration for the current splash.
            // Prioritize individual splash duration; otherwise, use the default.
            float currentDisplayDuration = splashData.displayDuration > 0 ? splashData.displayDuration : defaultDisplayDuration;

            // --- Fade In ---
            yield return StartCoroutine(FadeCanvasGroup(splashCanvasGroup, 0f, 1f, fadeInDuration));

            // --- Display Duration ---
            // Keep the splash screen fully visible for its determined duration.
            yield return new WaitForSeconds(currentDisplayDuration);

            // --- Fade Out ---
            yield return StartCoroutine(FadeCanvasGroup(splashCanvasGroup, 1f, 0f, fadeOutDuration));

            // Hide the image immediately after fading out for a cleaner transition between splashes.
            splashImage.gameObject.SetActive(false); 
        }

        // After all splash screens are shown, load the next scene.
        LoadNextScene();
    }

    /// <summary>
    /// Coroutine to smoothly fade a CanvasGroup's alpha over a specified duration.
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to fade.</param>
    /// <param name="startAlpha">The starting alpha value (0 for fully transparent, 1 for fully opaque).</param>
    /// <param name="endAlpha">The target alpha value.</param>
    /// <param name="duration">The duration of the fade animation in seconds.</param>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null; // Wait for the next frame
        }
        canvasGroup.alpha = endAlpha; // Ensure the final alpha value is set precisely
    }

    /// <summary>
    /// Loads the next scene specified in the 'nextSceneName' field.
    /// This method also cleans up the SplashScreenManager instance as its job is done.
    /// </summary>
    private void LoadNextScene()
    {
        if (isLoadingScene) return; // Prevent multiple scene load requests

        isLoadingScene = true;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("SplashScreenManager: No 'Next Scene Name' specified! Please set the name of the scene to load.", this);
            // In a real game, you might load a default error scene or quit.
            return;
        }

        Debug.Log($"SplashScreenManager: All splash screens displayed. Loading scene: '{nextSceneName}'...");

        // Load the next scene asynchronously for a smoother user experience.
        // You could also add a loading progress bar here if needed.
        StartCoroutine(LoadSceneAsyncAndCleanup(nextSceneName));
    }

    /// <summary>
    /// Loads a scene asynchronously and then destroys this SplashScreenManager GameObject.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private IEnumerator LoadSceneAsyncAndCleanup(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // While the scene is loading, you could update a progress bar.
        while (!asyncLoad.isDone)
        {
            // Example: float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            // Debug.Log($"Loading progress: {progress * 100}%");
            yield return null;
        }

        // Once the scene is loaded, this SplashScreenManager has completed its task.
        // We can now safely destroy its GameObject.
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
```

---

### How to Use the `SplashScreenManager` in Unity

1.  **Create a dedicated "Splash Scene":**
    *   In Unity, go to `File > New Scene`. Save it as `SplashScene` (or `BootstrapScene`, `IntroScene`, etc.).
    *   Go to `File > Build Settings...`. Add `SplashScene` to the "Scenes In Build" list at index 0. This ensures it's the first scene your game loads.
    *   Add your `MainMenu` scene (or whatever `nextSceneName` you choose) to the "Scenes In Build" list as well, after `SplashScene`.

2.  **Set up the UI in `SplashScene`:**
    *   Right-click in the Hierarchy -> `UI` -> `Canvas`. Rename it to `SplashCanvas`.
    *   Select `SplashCanvas`. In the Inspector, click `Add Component` and add a `Canvas Group` component.
        *   Ensure `Alpha` is `0` initially (this will be handled by the script's `Awake` anyway, but good to know).
        *   Uncheck `Blocks Raycasts` and `Interactable` initially.
    *   Right-click `SplashCanvas` -> `UI` -> `Image`. Rename it to `SplashImage`.
        *   Make sure `SplashImage` is a child of `SplashCanvas` (which has the `CanvasGroup`).
        *   Set its `Rect Transform` to stretch across the entire canvas (e.g., `Left: 0, Top: 0, Right: 0, Bottom: 0`).
        *   Set `Image Type` to `Simple`, `Preserve Aspect` if desired.

3.  **Create the `SplashScreenManager` GameObject:**
    *   In the `SplashScene` Hierarchy, create an Empty GameObject: Right-click -> `Create Empty`. Rename it to `SplashScreenManager`.
    *   Attach the `SplashScreenManager.cs` script to this `SplashScreenManager` GameObject.

4.  **Configure the `SplashScreenManager` in the Inspector:**
    *   **UI References:**
        *   Drag the `SplashCanvas` (which has the `Canvas Group` component) into the `Splash Canvas Group` field.
        *   Drag the `SplashImage` into the `Splash Image` field.
    *   **Splash Screen Configuration:**
        *   Set the `Size` of the `Splash Screens` array (e.g., `2` for two splash screens).
        *   For each element in the array:
            *   Drag a `Sprite` (e.g., your company logo, game logo) into the `Splash Sprite` field. You can import images as sprites into Unity.
            *   Adjust `Display Duration` if you want it to be different from the `Default Display Duration`.
        *   Adjust `Default Display Duration`, `Fade In Duration`, `Fade Out Duration` as desired.
    *   **Scene Transition:**
        *   Set `Next Scene Name` to the name of your main menu scene (e.g., `MainMenu`). Make sure this scene exists and is added to Build Settings!

5.  **Run the game:**
    *   Play the `SplashScene`. You should see your splash screens appear sequentially with fades, and then your `MainMenu` scene will load.

---

### Understanding the SplashScreenManager Pattern

*   **Centralized Control:** The `SplashScreenManager` class encapsulates all the logic related to displaying splash screens. This means other parts of your game don't need to know *how* splash screens are displayed, only that they *will* be displayed before the main game.
*   **Decoupling:** The main game logic (e.g., `GameManager` for main menu, player spawning) doesn't need to worry about splash screens. It simply waits for the `SplashScreenManager` to load the `MainMenu` scene.
*   **Configurability:** By using `SplashScreenData` and `[SerializeField]` fields, developers can easily configure the sequence, sprites, durations, and fade times of splash screens directly in the Unity Inspector without changing code.
*   **Reusability:** This manager can be dropped into any Unity project to handle initial intro sequences.
*   **Singleton:** The `Instance` property provides a global, easy way to access the manager if other scripts (though unlikely for this pattern's primary function) needed to interact with it. `DontDestroyOnLoad` ensures it remains active through the initial scene load, allowing it to complete its task before transitioning.
*   **Coroutines for Asynchronous Operations:** `IEnumerator` and `yield return` are crucial for handling timed events (display durations, fades) and asynchronous operations (scene loading) without blocking the main thread, keeping the application responsive.
*   **CanvasGroup for UI Fades:** Fading a `CanvasGroup` is more efficient and reliable than fading individual UI elements, as it controls the alpha of all its children simultaneously.

This setup creates a robust and flexible system for managing your game's initial branding and loading sequence.