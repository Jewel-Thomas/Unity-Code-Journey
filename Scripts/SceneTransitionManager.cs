// Unity Design Pattern Example: SceneTransitionManager
// This script demonstrates the SceneTransitionManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity script demonstrates the **SceneTransitionManager** design pattern. It provides a robust, reusable, and extendable way to handle scene loading and transitions in your Unity projects.

---

## SceneTransitionManager Script

This script should be placed in your Unity project, for example, at `Assets/Scripts/Managers/SceneTransitionManager.cs`.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager operations
using System.Collections;          // Required for Coroutines
using System;                      // Required for Action delegate

/// <summary>
/// SceneTransitionManager: A Singleton responsible for managing all scene loading and transitions.
/// This pattern centralizes scene management logic, making it easy to add visual transitions
/// (like fades), handle asynchronous loading, and even pass data between scenes.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // =====================================
    // Singleton Implementation
    // =====================================

    /// <summary>
    /// The static instance of the SceneTransitionManager, ensuring only one exists.
    /// Access this instance via `SceneTransitionManager.Instance`.
    /// </summary>
    public static SceneTransitionManager Instance { get; private set; }

    // =====================================
    // Inspector Settings
    // =====================================

    [Header("Transition Settings")]
    [Tooltip("The duration of the fade-in and fade-out animations in seconds.")]
    [SerializeField] private float _fadeDuration = 1.0f;

    [Tooltip("Assign a UI CanvasGroup component here. This will be used to create " +
             "a fullscreen fade effect. It should cover the entire screen and " +
             "its alpha will be animated between 0 (transparent) and 1 (opaque).")]
    [SerializeField] private CanvasGroup _fadePanel;

    // =====================================
    // Private Internal State
    // =====================================

    /// <summary>
    /// Stores arbitrary data that needs to be passed from the *current* scene
    /// to the *next* scene being loaded. This is cleared after the transition.
    /// </summary>
    public object DataToPass { get; private set; }

    /// <summary>
    /// A flag to prevent multiple scene transitions from starting simultaneously.
    /// </summary>
    private bool _isTransitioning = false;

    // =====================================
    // Unity Lifecycle Methods
    // =====================================

    /// <summary>
    /// Called when the script instance is being loaded. This is where the Singleton
    /// pattern is enforced and initial setup for the fade panel is done.
    /// </summary>
    void Awake()
    {
        // Enforce Singleton:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // If this is the first instance, set it and ensure it persists across scenes.
            Instance = this;
            DontDestroyOnLoad(gameObject); // Important for a persistent manager.
        }

        // Initial setup for the fade panel:
        // Ensure it starts transparent and doesn't block interactions.
        if (_fadePanel != null)
        {
            _fadePanel.alpha = 0;
            _fadePanel.blocksRaycasts = false; // Allow interaction with the scene initially.
            _fadePanel.interactable = false;   // Not interactable initially.
        }
        else
        {
            // Log an error if the fade panel isn't assigned, as transitions won't work.
            Debug.LogError("SceneTransitionManager: Fade Panel (CanvasGroup) is not assigned! " +
                           "Please assign a CanvasGroup to the '_fadePanel' field in the Inspector " +
                           "for transitions to work properly.");
        }
    }

    // =====================================
    // Public API for Scene Loading
    // =====================================

    /// <summary>
    /// Initiates an asynchronous scene load by name, with a fade transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load (must be in Build Settings).</param>
    /// <param name="onSceneLoaded">Optional callback Action to execute once the new scene is fully loaded and faded in.</param>
    /// <param name="data">Optional data object to pass to the next scene.</param>
    public void LoadScene(string sceneName, Action onSceneLoaded = null, object data = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Already transitioning. Ignoring new load request.");
            return; // Prevent starting a new transition while one is in progress.
        }

        _isTransitioning = true;    // Set flag to indicate a transition is active.
        DataToPass = data;          // Store data for the next scene to retrieve.
        StartCoroutine(LoadSceneRoutine(sceneName, onSceneLoaded));
    }

    /// <summary>
    /// Initiates an asynchronous scene load by build index, with a fade transition.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load (must be in Build Settings).</param>
    /// <param name="onSceneLoaded">Optional callback Action to execute once the new scene is fully loaded and faded in.</param>
    /// <param name="data">Optional data object to pass to the next scene.</param>
    public void LoadScene(int sceneIndex, Action onSceneLoaded = null, object data = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Already transitioning. Ignoring new load request.");
            return;
        }

        _isTransitioning = true;
        DataToPass = data;
        StartCoroutine(LoadSceneRoutine(sceneIndex, onSceneLoaded));
    }

    // =====================================
    // Core Scene Loading Coroutine
    // =====================================

    /// <summary>
    /// The main coroutine that orchestrates the entire scene transition process:
    /// 1. Fades out the current scene to black.
    /// 2. Asynchronously loads the new scene in the background.
    /// 3. Invokes an optional callback.
    /// 4. Fades in the new scene from black.
    /// </summary>
    /// <param name="sceneIdentifier">Can be a string (scene name) or int (scene build index).</param>
    /// <param name="onSceneLoaded">Callback to execute after the scene loads and before fade-in.</param>
    private IEnumerator LoadSceneRoutine(object sceneIdentifier, Action onSceneLoaded)
    {
        // --- Step 1: Fade Out ---
        // Completely darken the screen to hide the scene loading process.
        yield return StartCoroutine(FadeRoutine(1, _fadeDuration)); // Fade to opaque (alpha = 1)

        // --- Step 2: Asynchronously Load Scene ---
        AsyncOperation operation;

        // Determine whether to load by name or index.
        if (sceneIdentifier is string sceneName)
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        else if (sceneIdentifier is int sceneIndex)
        {
            operation = SceneManager.LoadSceneAsync(sceneIndex);
        }
        else
        {
            // If an invalid identifier is passed, log an error and stop.
            Debug.LogError("SceneTransitionManager: Invalid scene identifier provided to LoadSceneRoutine. " +
                           "Must be a string (scene name) or int (scene index).");
            _isTransitioning = false;
            DataToPass = null;
            yield break;
        }

        // Crucial: Prevent the new scene from activating immediately upon loading.
        // This keeps the screen black until we are ready to fade it back in.
        operation.allowSceneActivation = false;

        // Wait until the scene has almost finished loading (Unity reports 0.9f when ready).
        while (!operation.isDone && operation.progress < 0.9f)
        {
            // You can update a loading bar UI here if you have one.
            // Example: Debug.Log($"Loading progress: {operation.progress * 100}%");
            yield return null; // Wait for the next frame.
        }

        // Now that the scene is loaded and the screen is black, allow it to activate.
        operation.allowSceneActivation = true;

        // Wait for the scene to fully activate and complete its loading process.
        while (!operation.isDone)
        {
            yield return null;
        }

        // --- Step 3: Invoke Callback ---
        // If a callback was provided, execute it. This is useful for initial setup
        // in the new scene that needs to happen *before* it becomes visible.
        onSceneLoaded?.Invoke();

        // --- Step 4: Fade In ---
        // Reveal the newly loaded scene by fading the black screen away.
        yield return StartCoroutine(FadeRoutine(0, _fadeDuration)); // Fade to transparent (alpha = 0)

        // --- Step 5: Clean Up ---
        _isTransitioning = false; // Reset transition flag.
        DataToPass = null;        // Clear any passed data, as it's now consumed or no longer needed.
    }

    // =====================================
    // UI Fading Coroutine
    // =====================================

    /// <summary>
    /// Coroutine to smoothly animate the alpha of the _fadePanel CanvasGroup.
    /// </summary>
    /// <param name="targetAlpha">The desired final alpha value (0 for transparent, 1 for opaque).</param>
    /// <param name="duration">How long the fade should take in seconds.</param>
    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        // Safety check: if no fade panel is assigned, skip the fade.
        if (_fadePanel == null)
        {
            Debug.LogWarning("SceneTransitionManager: No fade panel assigned. Skipping fade routine.");
            yield break;
        }

        // Block raycasts and make interactable when fading to or from opaque
        // This prevents users from interacting with UI elements during the transition.
        _fadePanel.blocksRaycasts = true;
        _fadePanel.interactable = true;

        float startAlpha = _fadePanel.alpha; // Current alpha of the fade panel.
        float timer = 0;                     // Timer to track fade progress.

        // Loop until the fade duration is complete.
        while (timer < duration)
        {
            // Use unscaledDeltaTime to ensure the fade duration is not affected by Time.timeScale,
            // which might be paused or slowed down in game.
            timer += Time.unscaledDeltaTime;
            // Linearly interpolate the alpha from start to target.
            _fadePanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null; // Wait for the next frame.
        }

        // Ensure the final alpha is exactly the targetAlpha to avoid precision issues.
        _fadePanel.alpha = targetAlpha;

        // After the fade, only block raycasts if the panel is fully opaque (i.e., screen is black).
        // If fading out (targetAlpha = 0), allow interactions with the new scene.
        _fadePanel.blocksRaycasts = targetAlpha == 1;
        _fadePanel.interactable = targetAlpha == 1;
    }

    // =====================================
    // Data Passing API
    // =====================================

    /// <summary>
    /// Retrieves data that was passed from the previous scene during a transition.
    /// This method should be called in the `Awake()` or `Start()` of the new scene.
    /// </summary>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <returns>The data object if it matches the type, otherwise default(T).</returns>
    public T GetData<T>() where T : class
    {
        if (DataToPass is T data)
        {
            // Data is retrieved, so we can clear it to prevent stale data for future transitions.
            ClearData();
            return data;
        }
        // If data is null or not of the expected type, return default.
        return default(T);
    }

    /// <summary>
    /// Explicitly clears any stored data. Useful if the data isn't retrieved immediately
    /// or if you want to ensure it's gone for specific reasons.
    /// </summary>
    public void ClearData()
    {
        DataToPass = null;
    }
}
```

---

## How to Set Up in Unity

To make this `SceneTransitionManager` work in your project, follow these steps:

1.  **Create the Script:** Save the code above as `SceneTransitionManager.cs` in your `Assets/Scripts` folder (or `Assets/Scripts/Managers`).

2.  **Create the Manager GameObject:**
    *   In your first scene (e.g., your "Loading" or "MainMenu" scene), create an empty GameObject.
    *   Rename it to `SceneTransitionManager`.
    *   Attach the `SceneTransitionManager.cs` script to this GameObject.

3.  **Create the UI Fade Panel:**
    *   In the same scene, create a UI Canvas: Right-click in the Hierarchy -> `UI` -> `Canvas`.
    *   Set the Canvas `Render Mode` to `Screen Space - Overlay` (this is usually the default).
    *   Inside this Canvas, create a UI Panel: Right-click on Canvas -> `UI` -> `Panel`.
    *   Rename this Panel to `FadePanel`.
    *   Select `FadePanel`. In its `Rect Transform` component, make sure its anchors are set to stretch across the entire screen (click the anchor preset icon and choose the stretch option, holding Alt + Shift).
    *   Set the `Image` component's color to Black (or any color you want for your transition). Ensure its alpha is 255 (fully opaque) in the inspector *for design time*, but the script will set it to 0 initially.
    *   Add a `Canvas Group` component to the `FadePanel`: Click `Add Component` -> search for `Canvas Group`. You don't need to change any settings on the `Canvas Group` itself at design time.

4.  **Assign the Fade Panel to the Manager:**
    *   Select your `SceneTransitionManager` GameObject.
    *   In its Inspector, locate the `_fadePanel` field under `Transition Settings`.
    *   Drag your `FadePanel` GameObject (the one with the `Canvas Group`) from the Hierarchy into this `_fadePanel` slot.

5.  **Add Scenes to Build Settings:**
    *   Go to `File` -> `Build Settings...`.
    *   Drag all the scenes you want to be able to transition between into the "Scenes In Build" list. Note their names or indices.

---

## Example Usage

Here are some examples of how to use the `SceneTransitionManager` in your other scripts.

### 1. Simple Scene Load (e.g., from a Button)

```csharp
// Example: MainMenuController.cs
using UnityEngine;
using UnityEngine.UI; // If using UI Buttons

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public Button optionsButton;

    void Awake()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        }
    }

    private void OnPlayButtonClicked()
    {
        // Load the "GameScene" using its name.
        // No callback needed, no data to pass.
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("GameScene");
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found!");
            // Fallback: load directly if manager is missing (not recommended for production)
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }

    private void OnOptionsButtonClicked()
    {
        // Load the "OptionsScene" using its build index.
        // Let's assume OptionsScene is at index 2 in Build Settings.
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(2);
        }
    }
}
```

### 2. Loading with a Callback (e.g., for Post-Load Setup)

```csharp
// Example: GameManager.cs (or LevelLoader)
using UnityEngine;
using System; // Required for Action

public class GameManager : MonoBehaviour
{
    public void StartNewGame()
    {
        Debug.Log("Starting new game transition...");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("Level1", () =>
            {
                // This code runs AFTER "Level1" is fully loaded AND the fade-in is complete.
                Debug.Log("Level1 has finished loading and transition complete! Initializing game state...");
                InitializeLevelData();
            });
        }
    }

    private void InitializeLevelData()
    {
        // Perform scene-specific setup here, e.g., spawn player, load saved data.
        Debug.Log("Game data initialized for Level1.");
    }
}
```

### 3. Passing Data Between Scenes

**Sender Script (e.g., CharacterSelection.cs):**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public Button selectWarriorButton;
    public Button selectMageButton;

    void Awake()
    {
        if (selectWarriorButton != null)
        {
            selectWarriorButton.onClick.AddListener(() => SelectCharacter("Warrior", 100, 10));
        }
        if (selectMageButton != null)
        {
            selectMageButton.onClick.AddListener(() => SelectCharacter("Mage", 70, 30));
        }
    }

    private void SelectCharacter(string charName, int health, int damage)
    {
        Debug.Log($"Selected {charName}. Loading game scene...");

        // Create an anonymous object or a custom class to hold the data.
        var characterData = new { Name = charName, Health = health, Damage = damage };

        if (SceneTransitionManager.Instance != null)
        {
            // Pass the characterData object along with the scene load request.
            SceneTransitionManager.Instance.LoadScene("GameScene", null, characterData);
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found!");
        }
    }
}
```

**Receiver Script (e.g., PlayerSpawnManager.cs in "GameScene"):**

```csharp
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    // A simple class to represent character data (optional, can also use anonymous types)
    public class CharacterStats
    {
        public string Name;
        public int Health;
        public int Damage;
    }

    void Awake()
    {
        if (SceneTransitionManager.Instance != null)
        {
            // Try to retrieve the passed data.
            // Using `GetData<CharacterStats>()` or `GetData<dynamic>()` if using anonymous type directly.
            var characterData = SceneTransitionManager.Instance.GetData<CharacterStats>();

            if (characterData != null)
            {
                Debug.Log($"Received character data: Name: {characterData.Name}, Health: {characterData.Health}, Damage: {characterData.Damage}");
                SpawnPlayer(characterData);
            }
            else
            {
                Debug.LogWarning("No character data received. Spawning default player.");
                SpawnDefaultPlayer();
            }
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found! Cannot retrieve passed data.");
            SpawnDefaultPlayer();
        }
    }

    private void SpawnPlayer(CharacterStats stats)
    {
        Debug.Log($"Spawning {stats.Name} with {stats.Health} HP and {stats.Damage} DMG.");
        // Instantiate player prefab, set stats, etc.
    }

    private void SpawnDefaultPlayer()
    {
        Debug.Log("Spawning a default player.");
        // Instantiate default player.
    }
}
```

---

This `SceneTransitionManager` provides a solid foundation for managing scene transitions in your Unity projects, promoting cleaner code and a better user experience with smooth visual transitions.