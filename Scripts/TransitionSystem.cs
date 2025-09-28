// Unity Design Pattern Example: TransitionSystem
// This script demonstrates the TransitionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive example demonstrates the 'TransitionSystem' design pattern in Unity. It provides a flexible and extensible way to manage transitions between game states, scenes, or UI screens, decoupling the transition logic from its visual presentation.

The solution consists of three main parts:
1.  **`ITransitionEffect`**: An interface defining what any transition animation must do.
2.  **`FadeTransitionEffect`**: A concrete implementation of `ITransitionEffect` using a simple fade to black.
3.  **`TransitionSystem`**: The central manager (a singleton) that orchestrates transitions using any `ITransitionEffect`.

---

**1. `ITransitionEffect.cs` (Interface Definition)**

This script defines the contract for any custom transition effect you want to create.

```csharp
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ITransitionEffect Interface
/// Defines the contract for any visual or logical transition effect.
/// Implementations will handle the specific animation or presentation logic
/// for a transition (e.g., fade to black, slide, wipe).
/// </summary>
public interface ITransitionEffect
{
    /// <summary>
    /// Gets the GameObject associated with this transition effect.
    /// This is typically a UI element that will be shown/hidden during the transition.
    /// </summary>
    GameObject EffectGameObject { get; }

    /// <summary>
    /// Initializes the effect, ensuring it's in a hidden or default state.
    /// Called once when the TransitionSystem is set up, and potentially before each transition.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Plays the 'intro' part of the transition (e.g., fading in to fully obscure the screen).
    /// This is where the screen becomes completely covered, allowing the scene change or
    /// other background logic to happen invisibly.
    /// </summary>
    /// <param name="onIntroComplete">Action to invoke once the intro animation is finished
    /// and the screen is fully obscured. The TransitionSystem waits for this.</param>
    IEnumerator PlayIntro(Action onIntroComplete);

    /// <summary>
    /// Plays the 'outro' part of the transition (e.g., fading out to reveal the new screen).
    /// This animation reveals the new content after the core transition logic has completed.
    /// </summary>
    /// <param name="onOutroComplete">Action to invoke once the outro animation is finished
    /// and the screen is fully revealed. The TransitionSystem waits for this.</param>
    IEnumerator PlayOutro(Action onOutroComplete);
}
```

---

**2. `FadeTransitionEffect.cs` (Concrete Effect Implementation)**

This script provides a practical example of how to implement the `ITransitionEffect` interface using a `CanvasGroup` and `Image` to create a simple fade effect.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Concrete implementation of ITransitionEffect for a simple fade transition.
/// This effect uses a CanvasGroup and an Image to fade the screen to a solid color.
/// It fades in (obscures screen) and then fades out (reveals screen).
/// </summary>
[RequireComponent(typeof(CanvasGroup))] // Ensures a CanvasGroup is present for fading
[RequireComponent(typeof(Image))]      // Ensures an Image is present for the fade color
public class FadeTransitionEffect : MonoBehaviour, ITransitionEffect
{
    [Tooltip("The CanvasGroup controlling the fade transparency.")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("The Image component that will be colored and stretched to cover the screen.")]
    [SerializeField] private Image _fadeImage;
    [Tooltip("The duration of each fade phase (intro or outro) in seconds.")]
    [SerializeField] private float _fadeDuration = 0.5f;
    [Tooltip("The solid color to fade to/from.")]
    [SerializeField] private Color _fadeColor = Color.black;

    // Public property to satisfy ITransitionEffect interface
    public GameObject EffectGameObject => gameObject;

    // --- Unity Lifecycle ---
    private void Awake()
    {
        // Ensure components are assigned, helpful for editor setup and robustness
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_fadeImage == null) _fadeImage = GetComponent<Image>();

        // Set initial state of the effect
        Initialize();
    }

    // --- ITransitionEffect Implementation ---

    /// <summary>
    /// Initializes the effect by setting the image color and making it completely transparent.
    /// Also disables raycasts and interaction when hidden.
    /// </summary>
    public void Initialize()
    {
        if (_fadeImage != null)
        {
            _fadeImage.color = _fadeColor;
        }
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;            // Start completely transparent
            _canvasGroup.blocksRaycasts = false; // Don't block input when hidden
            _canvasGroup.interactable = false;   // Don't interact with hidden overlay
        }
    }

    /// <summary>
    /// Coroutine to play the intro animation (fade in to full opacity).
    /// The screen will become completely covered.
    /// </summary>
    /// <param name="onIntroComplete">Callback when the fade-in is done and the screen is fully obscured.</param>
    public IEnumerator PlayIntro(Action onIntroComplete)
    {
        if (_canvasGroup == null)
        {
            Debug.LogError("FadeTransitionEffect: CanvasGroup is null. Cannot play intro.");
            onIntroComplete?.Invoke();
            yield break;
        }

        // Enable input blocking and interaction to prevent user input during the transition
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        _canvasGroup.alpha = 1f; // Ensure fully opaque at the end

        onIntroComplete?.Invoke(); // Signal completion
    }

    /// <summary>
    /// Coroutine to play the outro animation (fade out from full opacity).
    /// The screen will become completely transparent again.
    /// </summary>
    /// <param name="onOutroComplete">Callback when the fade-out is done and the screen is fully revealed.</param>
    public IEnumerator PlayOutro(Action onOutroComplete)
    {
        if (_canvasGroup == null)
        {
            Debug.LogError("FadeTransitionEffect: CanvasGroup is null. Cannot play outro.");
            onOutroComplete?.Invoke();
            yield break;
        }

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        _canvasGroup.alpha = 0f; // Ensure fully transparent at the end

        // Disable input blocking and interaction now that the transition is complete
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        onOutroComplete?.Invoke(); // Signal completion
    }
}
```

---

**3. `TransitionSystem.cs` (The Core Manager)**

This is the central singleton that manages and orchestrates all transitions. It uses a specified `ITransitionEffect` to handle the visual aspect while it manages scene loading or custom logic.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic; // Can be useful for more complex scenarios, e.g., a pool of effects

/// <summary>
/// The TransitionSystem is a singleton manager responsible for coordinating transitions
/// between different game states, often involving scene loading and visual effects.
/// It decouples the "what" of a transition (e.g., load Scene X, do Y logic)
/// from the "how" (e.g., fade, slide, wipe).
///
/// Pattern: Transition System (a variation of Service Locator/Singleton for specific domain logic)
/// This system provides a centralized point to initiate and manage transitions, ensuring
/// visual consistency and handling asynchronous operations.
/// </summary>
public class TransitionSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static TransitionSystem _instance;
    public static TransitionSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Attempt to find an existing instance in the scene
                _instance = FindObjectOfType<TransitionSystem>();

                if (_instance == null)
                {
                    // If none found, create a new GameObject and attach the script
                    GameObject singletonObject = new GameObject(typeof(TransitionSystem).Name);
                    _instance = singletonObject.AddComponent<TransitionSystem>();
                    Debug.Log($"TransitionSystem: Created a new instance on GameObject '{singletonObject.name}'.");
                }
                else
                {
                    Debug.Log("TransitionSystem: Found existing instance.");
                }

                // Ensure the instance persists across scene loads
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // --- Inspector Settings ---
    [Tooltip("The default transition effect to use if none is specified for a transition call.")]
    [SerializeField] private MonoBehaviour _defaultTransitionEffect; // Use MonoBehaviour to drag any ITransitionEffect
    private ITransitionEffect _cachedDefaultEffect; // Cached reference to the default effect after casting

    // --- Internal State ---
    private bool _isTransitioning = false;
    public bool IsTransitioning => _isTransitioning; // Public accessor to check transition state

    // --- Events (for external listeners) ---
    /// <summary>
    /// Fired when a transition process officially begins (e.g., visual effect starts).
    /// </summary>
    public event Action OnTransitionStarted;
    /// <summary>
    /// Fired when a transition process officially completes (e.g., new scene loaded, visual effect ends).
    /// </summary>
    public event Action OnTransitionCompleted;

    // --- Unity Lifecycle ---
    private void Awake()
    {
        // Enforce singleton pattern: destroy duplicate instances
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"TransitionSystem: Duplicate instance of TransitionSystem found on '{gameObject.name}'. Destroying this duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Ensure this system persists across scenes

        // Cache the default effect and initialize it
        if (_defaultTransitionEffect != null)
        {
            _cachedDefaultEffect = _defaultTransitionEffect as ITransitionEffect;
            if (_cachedDefaultEffect == null)
            {
                Debug.LogError($"TransitionSystem: Default transition effect '{_defaultTransitionEffect.name}' assigned, but it does not implement ITransitionEffect.");
            }
            else
            {
                _cachedDefaultEffect.Initialize(); // Set the effect to its initial, hidden state
                // Parent the effect's GameObject under this system for organizational purposes
                _cachedDefaultEffect.EffectGameObject.transform.SetParent(this.transform);
                _cachedDefaultEffect.EffectGameObject.SetActive(false); // Ensure it's hidden until needed
            }
        }
        else
        {
            Debug.LogWarning("TransitionSystem: No default transition effect assigned. Transitions might not have visual feedback.");
        }
    }

    // --- Public API for Initiating Transitions ---

    /// <summary>
    /// Initiates a transition to a new scene using the default transition effect.
    /// </summary>
    /// <param name="targetSceneName">The name of the scene to load.</param>
    /// <param name="onTransitionLogic">Optional: An action to execute *during* the transition's obscured phase
    /// (e.g., loading game data, updating UI elements). This runs after the intro effect
    /// is complete and before the outro effect begins.</param>
    public void DoSceneTransition(string targetSceneName, Action onTransitionLogic = null)
    {
        DoTransition(_cachedDefaultEffect, targetSceneName, onTransitionLogic);
    }

    /// <summary>
    /// Initiates a transition without changing scenes, using the default transition effect.
    /// Useful for internal state changes or loading dynamic content within the same scene.
    /// </summary>
    /// <param name="onTransitionLogic">An action to execute *during* the transition's obscured phase.
    /// This is where the main work of the transition should occur (e.g., loading assets,
    /// setting up new game state). This action is mandatory for internal transitions.</param>
    public void DoInternalTransition(Action onTransitionLogic)
    {
        if (onTransitionLogic == null)
        {
            Debug.LogError("TransitionSystem: DoInternalTransition called with null onTransitionLogic. An internal transition must have logic to perform.");
            return;
        }
        DoTransition(_cachedDefaultEffect, null, onTransitionLogic);
    }

    /// <summary>
    /// The core method to execute a transition, allowing a specific effect to be passed.
    /// </summary>
    /// <param name="effect">The ITransitionEffect to use for this specific transition.
    /// If null, the system will attempt to use its default effect.</param>
    /// <param name="targetSceneName">Optional: The name of the scene to load. If null or empty,
    /// no scene will be loaded, and the transition will be for internal logic only.</param>
    /// <param name="onTransitionLogic">Optional: An action to execute *during* the transition's obscured phase.
    /// This runs after the intro effect is complete and before the outro effect begins.</param>
    public void DoTransition(ITransitionEffect effect, string targetSceneName, Action onTransitionLogic = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("TransitionSystem: Already in a transition. Ignoring new request to transition to '" + (targetSceneName ?? "internal logic") + "'.");
            return;
        }

        // Use default effect if none is explicitly provided or if the provided one is invalid
        if (effect == null || effect.EffectGameObject == null)
        {
            if (_cachedDefaultEffect == null)
            {
                Debug.LogError("TransitionSystem: No valid transition effect provided and no default effect assigned. Cannot perform transition.");
                return;
            }
            effect = _cachedDefaultEffect;
        }

        // Ensure the chosen effect is initialized and active for this transition
        effect.Initialize(); // Re-initialize in case its state was modified externally
        effect.EffectGameObject.SetActive(true); // Make the effect's GameObject visible

        // Start the main transition coroutine
        StartCoroutine(TransitionRoutine(effect, targetSceneName, onTransitionLogic));
    }

    /// <summary>
    /// The main coroutine that orchestrates the sequence of a transition:
    /// 1. Play intro effect (obscure screen).
    /// 2. Execute custom transition logic and/or load new scene.
    /// 3. Play outro effect (reveal new screen).
    /// </summary>
    private IEnumerator TransitionRoutine(ITransitionEffect effect, string targetSceneName, Action onTransitionLogic)
    {
        _isTransitioning = true;
        OnTransitionStarted?.Invoke(); // Notify listeners that a transition has begun
        Debug.Log($"TransitionSystem: Starting transition. Target Scene: '{targetSceneName ?? "Internal Logic"}'.");

        // --- Step 1: Play Intro Effect ---
        bool introComplete = false;
        yield return effect.PlayIntro(() => introComplete = true); // Start intro animation
        yield return new WaitUntil(() => introComplete);           // Wait until the intro effect confirms completion

        Debug.Log("TransitionSystem: Intro effect completed. Performing transition logic...");

        // --- Step 2: Perform Core Transition Logic (Scene Load and/or Custom Action) ---
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            // Load scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            // Optionally: asyncLoad.allowSceneActivation = false; can be used here
            // to manually control when the new scene becomes active (e.g., after loading all assets).
            // For simplicity, we let it activate when ready.

            while (!asyncLoad.isDone)
            {
                // You could pass asyncLoad.progress to the effect here if it supported a loading bar
                // (e.g., (effect as ILoadingBarEffect)?.UpdateProgress(asyncLoad.progress);)
                yield return null; // Wait until the next frame while scene loads
            }
            Debug.Log($"TransitionSystem: Scene '{targetSceneName}' loaded.");
        }

        // Execute any custom logic provided by the caller during the obscured phase
        onTransitionLogic?.Invoke();
        Debug.Log("TransitionSystem: Custom transition logic completed (if any).");

        // --- Step 3: Play Outro Effect ---
        bool outroComplete = false;
        yield return effect.PlayOutro(() => outroComplete = true); // Start outro animation
        yield return new WaitUntil(() => outroComplete);           // Wait until the outro effect confirms completion

        Debug.Log("TransitionSystem: Outro effect completed. Transition finished.");

        // --- Step 4: Finalize Transition ---
        effect.EffectGameObject.SetActive(false); // Hide the effect's GameObject now that it's no longer needed
        _isTransitioning = false;                 // Mark transition as complete
        OnTransitionCompleted?.Invoke();          // Notify listeners that the transition has finished
    }
}
```

---

**Example Usage in Unity Project**

Follow these steps to integrate and test the `TransitionSystem` in your Unity project:

**1. Create Scripts:**
   - Create a C# script named `ITransitionEffect.cs` and paste the `ITransitionEffect` interface code into it.
   - Create a C# script named `FadeTransitionEffect.cs` and paste the `FadeTransitionEffect` class code into it.
   - Create a C# script named `TransitionSystem.cs` and paste the `TransitionSystem` class code into it.

**2. Scene Setup:**

   a.  **`TransitionSystem` GameObject (in your first scene):**
       - Create an empty GameObject in your *initial* scene (e.g., "Bootstrap", "MainMenu", or "PersistentScene") and name it `TransitionSystem`.
       - Attach the `TransitionSystem.cs` script to it. This script will automatically persist across scenes due to `DontDestroyOnLoad`.

   b.  **`FadeTransitionEffect` Setup (as a child of Canvas):**
       - Create a new Canvas: `GameObject -> UI -> Canvas`.
       - Ensure its `Render Mode` is set to `Screen Space - Overlay`. This ensures it covers everything.
       - Inside this Canvas, create an empty GameObject named `FadeTransitionEffect`.
       - Attach the `FadeTransitionEffect.cs` script to this `FadeTransitionEffect` GameObject.
       - Unity will automatically add `Canvas Group` and `Image` components because of `[RequireComponent]`.
       - Select the `FadeTransitionEffect` GameObject in the Hierarchy. In the Inspector for its `Image` component:
         - Set the `Color` to `Black`.
         - Set the `Alpha` (A value) to `0` initially (the `Initialize` method will handle this, but it's good practice).
         - **Stretch the Image:** Right-click on the `Rect Transform` component, go to `Set Anchor Presets`, and then **hold `Alt` and `Shift`** while clicking the bottom-right stretch preset (the one that fills the entire parent). This will make the image cover the whole screen.

   c.  **Link Default Effect:**
       - Select the `TransitionSystem` GameObject in your Hierarchy.
       - In the Inspector, drag the `FadeTransitionEffect` GameObject (the one with the `FadeTransitionEffect.cs` script attached) from your Hierarchy into the `Default Transition Effect` slot on the `TransitionSystem` component.

   d.  **Create Test Scenes:**
       - Create at least two additional scenes: e.g., `Scene_A` and `Scene_B`.
       - Save them and add all your scenes (`TransitionSystem`'s initial scene, `Scene_A`, `Scene_B`) to `File -> Build Settings...`.

**3. Example Game Logic Script (`SceneChanger.cs`):**

   Create a new C# script named `SceneChanger.cs` and paste the following code into it:

```csharp
using UnityEngine;
using System;
using System.Collections; // Required for Coroutines
using System.Threading; // Used for Thread.Sleep in example, be cautious in real game code

/// <summary>
/// Example script demonstrating how to use the TransitionSystem.
/// Attach this to any GameObject in your scenes, then hook up UI Buttons to its methods.
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [SerializeField] private string _targetSceneName = "Scene_B";
    [SerializeField] private string _anotherTargetSceneName = "Scene_A";

    /// <summary>
    /// Initiates a scene transition to `_targetSceneName` with a custom action during the transition.
    /// </summary>
    public void LoadSceneWithCustomLogic()
    {
        Debug.Log($"SceneChanger: Requesting transition to {_targetSceneName} with custom logic.");
        if (TransitionSystem.Instance == null)
        {
            Debug.LogError("TransitionSystem.Instance is null! Make sure TransitionSystem GameObject is in your first scene.");
            return;
        }

        TransitionSystem.Instance.DoSceneTransition(_targetSceneName, () =>
        {
            Debug.Log("SceneChanger: Custom logic executing *during* the transition's obscured phase.");
            // --- IMPORTANT: Avoid Thread.Sleep in actual game code! ---
            // This is for demonstration purposes only to simulate work.
            // In a real game, this would be non-blocking async operations (e.g., loading assets,
            // fetching data from a server, setting up player data).
            Thread.Sleep(800); // Simulate 0.8 seconds of blocking work
            Debug.Log("SceneChanger: Custom logic during transition finished.");
        });
    }

    /// <summary>
    /// Initiates a scene transition to `_anotherTargetSceneName` without any additional custom logic.
    /// </summary>
    public void LoadSceneSimple()
    {
        Debug.Log($"SceneChanger: Requesting simple transition to {_anotherTargetSceneName}.");
        if (TransitionSystem.Instance == null)
        {
            Debug.LogError("TransitionSystem.Instance is null! Make sure TransitionSystem GameObject is in your first scene.");
            return;
        }
        TransitionSystem.Instance.DoSceneTransition(_anotherTargetSceneName, null); // No custom logic
    }

    /// <summary>
    /// Initiates an "internal" transition, where no scene change occurs, but a visual effect
    /// plays while some internal game state logic is executed.
    /// </summary>
    public void DoInternalTransitionExample()
    {
        Debug.Log("SceneChanger: Requesting internal transition (no scene change).");
        if (TransitionSystem.Instance == null)
        {
            Debug.LogError("TransitionSystem.Instance is null! Make sure TransitionSystem GameObject is in your first scene.");
            return;
        }

        TransitionSystem.Instance.DoInternalTransition(() =>
        {
            Debug.Log("SceneChanger: Performing internal state change (e.g., showing a modal, updating UI, loading dynamic content).");
            // Simulate an asynchronous internal task using a Coroutine
            StartCoroutine(SimulateInternalWork());
        });
    }

    private IEnumerator SimulateInternalWork()
    {
        Debug.Log("SceneChanger: Internal work started (simulating 1.5s).");
        yield return new WaitForSeconds(1.5f); // Simulate a 1.5-second non-blocking task
        Debug.Log("SceneChanger: Internal work completed.");
    }

    // You can subscribe to TransitionSystem events if needed:
    private void OnEnable()
    {
        if (TransitionSystem.Instance != null)
        {
            TransitionSystem.Instance.OnTransitionStarted += HandleTransitionStarted;
            TransitionSystem.Instance.OnTransitionCompleted += HandleTransitionCompleted;
        }
    }

    private void OnDisable()
    {
        if (TransitionSystem.Instance != null)
        {
            TransitionSystem.Instance.OnTransitionStarted -= HandleTransitionStarted;
            TransitionSystem.Instance.OnTransitionCompleted -= HandleTransitionCompleted;
        }
    }

    private void HandleTransitionStarted()
    {
        Debug.Log($"SceneChanger: TransitionSystem reported: Transition started! Is currently transitioning: {TransitionSystem.Instance.IsTransitioning}");
    }

    private void HandleTransitionCompleted()
    {
        Debug.Log($"SceneChanger: TransitionSystem reported: Transition completed! Is currently transitioning: {TransitionSystem.Instance.IsTransitioning}");
    }
}
```

**4. Connect in Scenes:**

   - In `Scene_A` and `Scene_B`:
     - Create an empty GameObject (e.g., `SceneLogic`) and attach `SceneChanger.cs` to it.
     - Create a UI Button (`GameObject -> UI -> Button`).
     - On the Button's `OnClick()` event, drag the `SceneLogic` GameObject into the object slot.
     - From the dropdown, select `SceneChanger -> LoadSceneWithCustomLogic()` (or `LoadSceneSimple()`, `DoInternalTransitionExample()`).
     - Adjust the `_targetSceneName` and `_anotherTargetSceneName` fields in the `SceneChanger` script to point to your respective scenes (e.g., in `Scene_A` set `_targetSceneName` to "Scene_B", and in `Scene_B` set `_anotherTargetSceneName` to "Scene_A").
     - Add more buttons to test `DoInternalTransitionExample()` if desired.

**5. Run:**

   - Start your game from the initial scene where you set up the `TransitionSystem` (e.g., `Scene_A`).
   - Click the buttons to initiate transitions.
   - Observe the smooth fade effect and the debug logs in the Console to see the sequence of events during the transition (intro, custom logic/scene load, outro).

---

**How the TransitionSystem Pattern Works (Detailed Explanation):**

1.  **Centralization (The `TransitionSystem` Class):**
    *   **Singleton Access:** `TransitionSystem.Instance` provides a single, global access point for all transition requests throughout your game. This prevents scattered, duplicate transition logic.
    *   **Orchestration:** It acts as the conductor, managing the entire sequence of a transition:
        1.  Instructing the `ITransitionEffect` to `PlayIntro()` (e.g., fade in to cover the screen).
        2.  Performing the core transition logic (loading a new scene asynchronously via `SceneManager.LoadSceneAsync` and/or executing custom actions provided by the caller).
        3.  Instructing the `ITransitionEffect` to `PlayOutro()` (e.g., fade out to reveal the new content).
    *   **Persistence:** `DontDestroyOnLoad` ensures the `TransitionSystem` remains active and ready across scene changes.

2.  **Abstraction (The `ITransitionEffect` Interface):**
    *   **Decoupling:** This is the heart of the Transition System pattern. It completely separates *what* a transition does (load scene, change state) from *how* it looks or animates (fade, slide, wipe, dissolve).
    *   **Contract:** It defines a clear contract (`Initialize`, `PlayIntro`, `PlayOutro`) that any visual transition effect must adhere to. The `TransitionSystem` only interacts with this interface, not specific implementations.
    *   **Extensibility:** Want a new transition? Create a new script that implements `ITransitionEffect`. You never need to modify the `TransitionSystem` itself.

3.  **Concrete Implementations (`FadeTransitionEffect`):**
    *   **Specifics:** These classes provide the actual logic and visual components for a particular transition style. `FadeTransitionEffect` handles setting up a `CanvasGroup` and `Image` to perform a linear fade.
    *   **Encapsulation:** All the details of how a fade works (timers, `Mathf.Lerp`, `CanvasGroup.alpha`) are encapsulated within this class, hidden from the `TransitionSystem`.
    *   **Callbacks (`Action onIntroComplete`):** Crucially, the effect's `PlayIntro` and `PlayOutro` methods take `Action` callbacks. This allows the effect to signal back to the `TransitionSystem` exactly when its animation phase is truly finished, ensuring the sequence proceeds correctly.

4.  **Asynchronous Operations (`IEnumerator` and `AsyncOperation`):**
    *   **Non-Blocking:** Transitions often involve loading new scenes or heavy assets, which can take time. Unity's `Coroutines` (`IEnumerator`) are used extensively here to perform these operations asynchronously without freezing the game.
    *   **Scene Loading:** `SceneManager.LoadSceneAsync` is the standard way to load scenes in the background. The `TransitionSystem` waits for this operation to complete.
    *   **Timed Animations:** Coroutines also enable smooth, time-based animations for the `ITransitionEffect` implementations.

**Benefits in Real Unity Projects:**

*   **Clean Code & Separation of Concerns:** Your game logic simply requests a transition. It doesn't need to know *how* scenes load or *how* animations play.
*   **Highly Extensible:** Easily add new types of transition effects (e.g., "DoorWipeTransitionEffect", "CircleExpandTransitionEffect") by implementing `ITransitionEffect` without altering the core `TransitionSystem`.
*   **Consistent User Experience:** All transitions will follow the same reliable "obscure -> load/work -> reveal" sequence, providing a predictable and polished feel.
*   **Easy to Maintain:** If you need to change how a fade works, you only touch `FadeTransitionEffect.cs`. If you need to change the overall transition flow, you modify `TransitionSystem.cs`.
*   **Flexibility:** The `onTransitionLogic` `Action` parameter allows you to inject any game-specific code (e.g., saving game data, loading player progress, modifying game state) directly into the transition phase, ensuring it happens while the screen is obscured.
*   **Robustness:** Manages the lifecycle of effects, handles potential errors (like missing effects), and ensures the system is ready for subsequent transitions.