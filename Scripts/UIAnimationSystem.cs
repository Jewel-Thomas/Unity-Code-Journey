// Unity Design Pattern Example: UIAnimationSystem
// This script demonstrates the UIAnimationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a practical **UIAnimationSystem** design pattern in Unity. It centralizes the management and execution of UI animations, making your UI code cleaner, more maintainable, and easier to extend.

**Key Components of the UIAnimationSystem Pattern:**

1.  **`IUIAnimation` (Interface):** Defines the common contract for any UI animation. This allows the system to treat all animations uniformly.
2.  **`UIAnimationSystem` (Singleton Manager):** The central hub responsible for registering, playing, stopping, and managing the lifecycle of all UI animations. It acts as an abstraction layer.
3.  **Concrete Animation Implementations:** Specific scripts (e.g., `UIPanelFadeAnimation`, `UIPanelScaleAnimation`) that implement `IUIAnimation` and handle the actual visual changes using a tweening library (like DOTween) or Unity's built-in Coroutines.
4.  **Animation Trigger:** A script or system that requests the `UIAnimationSystem` to play a specific animation by its unique ID.

---

### **Before You Start (DOTween Dependency)**

This example uses [DOTween](http://dotween.demigiant.com/index.php) for smooth, powerful animations. DOTween is a free asset from the Unity Asset Store.

**Steps:**
1.  Open your Unity project.
2.  Go to `Window` -> `Asset Store`.
3.  Search for "DOTween (HOTween v2)".
4.  Download and import it into your project.
5.  After importing, DOTween might prompt you to run its setup. Please do so (`Tools` -> `Demigiant` -> `DOTween Utility Panel` -> `Setup DOTween...` -> `Apply`).

If you **cannot** or **do not want to use DOTween**, I've provided commented-out alternative implementations for `UIPanelFadeAnimation` using `Coroutines` (though they are more verbose).

---

### **1. `IUIAnimation.cs` - The Animation Contract**

This interface defines what every animation in our system must be able to do.

```csharp
using UnityEngine;
using System;

/// <summary>
/// Interface for all UI animations managed by the UIAnimationSystem.
/// Defines the common contract for playing, stopping, pausing, resuming, and resetting an animation.
/// </summary>
public interface IUIAnimation
{
    /// <summary>
    /// Gets the unique identifier for this animation.
    /// This ID is used by the UIAnimationSystem to reference and control the animation.
    /// </summary>
    string AnimationID { get; }

    /// <summary>
    /// Gets the GameObject that this animation targets.
    /// </summary>
    GameObject TargetGameObject { get; }

    /// <summary>
    /// Indicates whether the animation is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Plays the animation.
    /// </summary>
    /// <param name="onComplete">An optional action to invoke when the animation finishes.</param>
    void Play(Action onComplete = null);

    /// <summary>
    /// Stops the animation immediately and resets it to its initial state.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the animation at its current state.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused animation from its current state.
    /// </summary>
    void Resume();

    /// <summary>
    /// Resets the animation to its initial state without playing it.
    /// </summary>
    void ResetToInitialState();
}
```

---

### **2. `UIAnimationSystem.cs` - The Central Manager**

This is the core of the pattern. It's a Singleton that provides a centralized API for controlling all registered UI animations.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// A centralized system for managing and playing UI animations.
/// Implements the Singleton pattern for easy global access.
/// </summary>
public class UIAnimationSystem : MonoBehaviour
{
    // Singleton instance
    public static UIAnimationSystem Instance { get; private set; }

    // Dictionary to store all registered animations, keyed by their unique AnimationID.
    private readonly Dictionary<string, IUIAnimation> _registeredAnimations = new Dictionary<string, IUIAnimation>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures only one instance of the UIAnimationSystem exists.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UIAnimationSystem: Another instance of UIAnimationSystem already exists. Destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, if you want this system to persist across scenes:
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Registers an animation with the system.
    /// Animations should register themselves (e.g., in their Awake() or Start() methods).
    /// </summary>
    /// <param name="animation">The IUIAnimation instance to register.</param>
    public void RegisterAnimation(IUIAnimation animation)
    {
        if (string.IsNullOrEmpty(animation.AnimationID))
        {
            Debug.LogError($"UIAnimationSystem: Cannot register animation with a null or empty AnimationID. Target: {animation.TargetGameObject.name}");
            return;
        }

        if (_registeredAnimations.ContainsKey(animation.AnimationID))
        {
            Debug.LogWarning($"UIAnimationSystem: An animation with ID '{animation.AnimationID}' is already registered. Overwriting.");
            _registeredAnimations[animation.AnimationID] = animation;
        }
        else
        {
            _registeredAnimations.Add(animation.AnimationID, animation);
            // Debug.Log($"UIAnimationSystem: Registered animation '{animation.AnimationID}' for GameObject '{animation.TargetGameObject.name}'.");
        }
    }

    /// <summary>
    /// Unregisters an animation from the system.
    /// </summary>
    /// <param name="animationID">The ID of the animation to unregister.</param>
    public void UnregisterAnimation(string animationID)
    {
        if (_registeredAnimations.Remove(animationID))
        {
            // Debug.Log($"UIAnimationSystem: Unregistered animation '{animationID}'.");
        }
        else
        {
            Debug.LogWarning($"UIAnimationSystem: Attempted to unregister animation '{animationID}' but it was not found.");
        }
    }

    /// <summary>
    /// Plays an animation by its ID.
    /// </summary>
    /// <param name="animationID">The unique ID of the animation to play.</param>
    /// <param name="onComplete">An optional action to invoke when the animation finishes.</param>
    /// <returns>True if the animation was found and started, false otherwise.</returns>
    public bool PlayAnimation(string animationID, Action onComplete = null)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            animation.Play(onComplete);
            return true;
        }
        Debug.LogWarning($"UIAnimationSystem: Animation with ID '{animationID}' not found for Play operation.");
        return false;
    }

    /// <summary>
    /// Stops an animation by its ID, resetting it to its initial state.
    /// </summary>
    /// <param name="animationID">The unique ID of the animation to stop.</param>
    /// <returns>True if the animation was found and stopped, false otherwise.</returns>
    public bool StopAnimation(string animationID)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            animation.Stop();
            return true;
        }
        Debug.LogWarning($"UIAnimationSystem: Animation with ID '{animationID}' not found for Stop operation.");
        return false;
    }

    /// <summary>
    /// Pauses an animation by its ID.
    /// </summary>
    /// <param name="animationID">The unique ID of the animation to pause.</param>
    /// <returns>True if the animation was found and paused, false otherwise.</returns>
    public bool PauseAnimation(string animationID)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            animation.Pause();
            return true;
        }
        Debug.LogWarning($"UIAnimationSystem: Animation with ID '{animationID}' not found for Pause operation.");
        return false;
    }

    /// <summary>
    /// Resumes a paused animation by its ID.
    /// </summary>
    /// <param name="animationID">The unique ID of the animation to resume.</param>
    /// <returns>True if the animation was found and resumed, false otherwise.</returns>
    public bool ResumeAnimation(string animationID)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            animation.Resume();
            return true;
        }
        Debug.LogWarning($"UIAnimationSystem: Animation with ID '{animationID}' not found for Resume operation.");
        return false;
    }

    /// <summary>
    /// Resets an animation to its initial state without playing it.
    /// </summary>
    /// <param name="animationID">The unique ID of the animation to reset.</param>
    /// <returns>True if the animation was found and reset, false otherwise.</returns>
    public bool ResetAnimationToInitialState(string animationID)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            animation.ResetToInitialState();
            return true;
        }
        Debug.LogWarning($"UIAnimationSystem: Animation with ID '{animationID}' not found for Reset operation.");
        return false;
    }

    /// <summary>
    /// Checks if a specific animation is currently playing.
    /// </summary>
    /// <param name="animationID">The ID of the animation to check.</param>
    /// <returns>True if the animation exists and is playing, false otherwise.</returns>
    public bool IsAnimationPlaying(string animationID)
    {
        if (_registeredAnimations.TryGetValue(animationID, out IUIAnimation animation))
        {
            return animation.IsPlaying;
        }
        return false;
    }

    /// <summary>
    /// Returns a list of all currently registered animation IDs.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredAnimationIDs()
    {
        return _registeredAnimations.Keys;
    }

    /// <summary>
    /// Clears all registered animations. Use with caution.
    /// </summary>
    public void ClearAllAnimations()
    {
        foreach (var animation in _registeredAnimations.Values)
        {
            animation.Stop(); // Ensure all animations are stopped before clearing.
        }
        _registeredAnimations.Clear();
        Debug.Log("UIAnimationSystem: All registered animations cleared.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            ClearAllAnimations(); // Clean up if the system is destroyed
        }
    }
}
```

---

### **3. `UIPanelFadeAnimation.cs` - Concrete Fade Animation**

An example implementation of `IUIAnimation` that fades a `CanvasGroup`.

```csharp
using UnityEngine;
using DG.Tweening; // Requires DOTween asset
using System;
using System.Collections; // Required for Coroutine alternative

/// <summary>
/// Implements a fade-in/fade-out animation for a UI panel using a CanvasGroup.
/// This component registers itself with the UIAnimationSystem.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelFadeAnimation : MonoBehaviour, IUIAnimation
{
    [Header("Animation Settings")]
    [Tooltip("Unique ID for this animation. Used by UIAnimationSystem to trigger it.")]
    [SerializeField] private string animationID = "DefaultFadeAnimation";
    [Tooltip("Duration of the fade animation in seconds.")]
    [SerializeField] private float duration = 0.5f;
    [Tooltip("Starting alpha value for the CanvasGroup.")]
    [SerializeField] private float startAlpha = 0f;
    [Tooltip("Ending alpha value for the CanvasGroup.")]
    [SerializeField] private float endAlpha = 1f;
    [Tooltip("Delay before the animation starts.")]
    [SerializeField] private float delay = 0f;
    [Tooltip("Ease type for the animation curve.")]
    [SerializeField] private Ease easeType = Ease.OutQuad;
    [Tooltip("If true, the panel will start hidden (startAlpha).")]
    [SerializeField] private bool startHidden = true;

    private CanvasGroup _canvasGroup;
    private Tween _currentTween; // For DOTween management
    private Action _onCompleteCallback;

    // --- IUIAnimation Properties ---
    public string AnimationID => animationID;
    public GameObject TargetGameObject => gameObject;
    public bool IsPlaying => _currentTween != null && _currentTween.IsActive() && _currentTween.IsPlaying();
    // For Coroutine alternative: public bool IsPlaying => _currentCoroutine != null;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogError($"UIPanelFadeAnimation on {gameObject.name}: Requires a CanvasGroup component.");
            enabled = false;
            return;
        }

        // Set initial state
        if (startHidden)
        {
            _canvasGroup.alpha = startAlpha;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        else
        {
            _canvasGroup.alpha = endAlpha;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
    }

    private void Start()
    {
        // Register this animation with the system
        if (UIAnimationSystem.Instance != null)
        {
            UIAnimationSystem.Instance.RegisterAnimation(this);
        }
        else
        {
            Debug.LogError($"UIPanelFadeAnimation on {gameObject.name}: UIAnimationSystem.Instance is null. Animation will not be registered.");
        }
    }

    private void OnDestroy()
    {
        // Unregister this animation when the GameObject is destroyed
        if (UIAnimationSystem.Instance != null)
        {
            UIAnimationSystem.Instance.UnregisterAnimation(AnimationID);
        }
        Stop(); // Ensure any active tween is killed
    }

    // --- IUIAnimation Methods ---

    /// <summary>
    /// Plays the fade animation.
    /// </summary>
    /// <param name="onComplete">Optional callback when animation finishes.</param>
    public void Play(Action onComplete = null)
    {
        Stop(); // Stop any previous animation

        _onCompleteCallback = onComplete;

        // Use DOTween for a smooth fade
        _currentTween = _canvasGroup.DOFade(endAlpha, duration)
            .SetEase(easeType)
            .SetDelay(delay)
            .OnStart(() =>
            {
                // Ensure the panel is visible and interactive during fade-in
                if (endAlpha > startAlpha) // If fading in
                {
                    _canvasGroup.blocksRaycasts = true;
                    _canvasGroup.interactable = true;
                }
            })
            .OnComplete(() =>
            {
                // If fading out, disable interaction and raycasts
                if (endAlpha < startAlpha)
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                }
                _onCompleteCallback?.Invoke();
                _currentTween = null; // Clear the reference
            })
            .Play();

        // --- COROUTINE ALTERNATIVE (if not using DOTween) ---
        // StartCoroutine(FadeCoroutine(onComplete));
    }

    /// <summary>
    /// Stops the animation immediately and resets to initial state.
    /// </summary>
    public void Stop()
    {
        if (_currentTween != null)
        {
            _currentTween.Kill(true); // Kill with 'complete: true' jumps to end, then resets below.
                                      // If you want to stop exactly where it is, use Kill(false) and then manually ResetToInitialState();
            _currentTween = null;
        }
        ResetToInitialState(); // Always reset state on stop
    }

    /// <summary>
    /// Pauses the current fade animation.
    /// </summary>
    public void Pause()
    {
        _currentTween?.Pause();
    }

    /// <summary>
    /// Resumes a paused fade animation.
    /// </summary>
    public void Resume()
    {
        _currentTween?.Play(); // DOTween's Play() also resumes a paused tween
    }

    /// <summary>
    /// Resets the panel to its initial alpha and interactivity state.
    /// </summary>
    public void ResetToInitialState()
    {
        if (startHidden)
        {
            _canvasGroup.alpha = startAlpha;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        else
        {
            _canvasGroup.alpha = endAlpha;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
    }


    /*
    // --- COROUTINE ALTERNATIVE FOR FADE ANIMATION (if NOT using DOTween) ---
    private Coroutine _currentCoroutine;

    private IEnumerator FadeCoroutine(Action onComplete)
    {
        float targetAlpha = endAlpha;
        float initialAlpha = _canvasGroup.alpha;
        float timer = 0f;

        // Set interactivity based on fade direction
        if (targetAlpha > initialAlpha) // Fading in
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            // Apply easing manually if desired, e.g., using AnimationCurve or simple functions
            float easedProgress = Mathf.SmoothStep(0, 1, progress); // Example simple ease
            _canvasGroup.alpha = Mathf.Lerp(initialAlpha, targetAlpha, easedProgress);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha; // Ensure final state is exact

        // Set interactivity based on fade direction
        if (targetAlpha < initialAlpha) // Fading out
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        _onCompleteCallback?.Invoke();
        _currentCoroutine = null;
    }

    // Adjust Stop() for coroutine alternative
    // public void Stop()
    // {
    //     if (_currentCoroutine != null)
    //     {
    //         StopCoroutine(_currentCoroutine);
    //         _currentCoroutine = null;
    //     }
    //     ResetToInitialState();
    // }

    // Pause/Resume are harder with simple Coroutines without extra logic
    // For simple Coroutines, you'd often just stop and restart or implement
    // custom pausing logic within the coroutine using a state variable.
    // For a robust Coroutine solution, consider something like
    // https://github.com/UnityCommunity/Unity-Coroutine-Helpers
    // For this example, DOTween is much superior for pause/resume.
    // public void Pause() { Debug.LogWarning("Pause not fully implemented for Coroutine-based fade."); }
    // public void Resume() { Debug.LogWarning("Resume not fully implemented for Coroutine-based fade."); }
    */
}
```

---

### **4. `UIPanelScaleAnimation.cs` - Concrete Scale Animation**

Another example implementing `IUIAnimation` for scaling a `RectTransform`.

```csharp
using UnityEngine;
using DG.Tweening; // Requires DOTween asset
using System;

/// <summary>
/// Implements a scale-in/scale-out animation for a UI element using a RectTransform.
/// This component registers itself with the UIAnimationSystem.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPanelScaleAnimation : MonoBehaviour, IUIAnimation
{
    [Header("Animation Settings")]
    [Tooltip("Unique ID for this animation. Used by UIAnimationSystem to trigger it.")]
    [SerializeField] private string animationID = "DefaultScaleAnimation";
    [Tooltip("Duration of the scale animation in seconds.")]
    [SerializeField] private float duration = 0.5f;
    [Tooltip("Starting scale vector for the RectTransform.")]
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [Tooltip("Ending scale vector for the RectTransform.")]
    [SerializeField] private Vector3 endScale = Vector3.one;
    [Tooltip("Delay before the animation starts.")]
    [SerializeField] private float delay = 0f;
    [Tooltip("Ease type for the animation curve.")]
    [SerializeField] private Ease easeType = Ease.OutBack;
    [Tooltip("If true, the panel will start at the startScale.")]
    [SerializeField] private bool startScaledDown = true;

    private RectTransform _rectTransform;
    private Tween _currentTween;
    private Action _onCompleteCallback;

    // --- IUIAnimation Properties ---
    public string AnimationID => animationID;
    public GameObject TargetGameObject => gameObject;
    public bool IsPlaying => _currentTween != null && _currentTween.IsActive() && _currentTween.IsPlaying();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError($"UIPanelScaleAnimation on {gameObject.name}: Requires a RectTransform component.");
            enabled = false;
            return;
        }

        // Set initial state
        if (startScaledDown)
        {
            _rectTransform.localScale = startScale;
            // Optionally disable canvas group interaction if scaled down to zero
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null && startScale == Vector3.zero)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }
        else
        {
            _rectTransform.localScale = endScale;
        }
    }

    private void Start()
    {
        // Register this animation with the system
        if (UIAnimationSystem.Instance != null)
        {
            UIAnimationSystem.Instance.RegisterAnimation(this);
        }
        else
        {
            Debug.LogError($"UIPanelScaleAnimation on {gameObject.name}: UIAnimationSystem.Instance is null. Animation will not be registered.");
        }
    }

    private void OnDestroy()
    {
        // Unregister this animation when the GameObject is destroyed
        if (UIAnimationSystem.Instance != null)
        {
            UIAnimationSystem.Instance.UnregisterAnimation(AnimationID);
        }
        Stop(); // Ensure any active tween is killed
    }

    // --- IUIAnimation Methods ---

    /// <summary>
    /// Plays the scale animation.
    /// </summary>
    /// <param name="onComplete">Optional callback when animation finishes.</param>
    public void Play(Action onComplete = null)
    {
        Stop(); // Stop any previous animation

        _onCompleteCallback = onComplete;

        // Use DOTween for a smooth scale animation
        _currentTween = _rectTransform.DOScale(endScale, duration)
            .SetEase(easeType)
            .SetDelay(delay)
            .OnStart(() =>
            {
                // Ensure interactivity during scale-in
                if (endScale.magnitude > startScale.magnitude) // If scaling up
                {
                    CanvasGroup cg = GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.blocksRaycasts = true;
                        cg.interactable = true;
                    }
                }
            })
            .OnComplete(() =>
            {
                // If scaled down to zero, disable interactivity
                if (endScale.magnitude < startScale.magnitude && endScale == Vector3.zero)
                {
                    CanvasGroup cg = GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.blocksRaycasts = false;
                        cg.interactable = false;
                    }
                }
                _onCompleteCallback?.Invoke();
                _currentTween = null; // Clear the reference
            })
            .Play();
    }

    /// <summary>
    /// Stops the animation immediately and resets to initial state.
    /// </summary>
    public void Stop()
    {
        if (_currentTween != null)
        {
            _currentTween.Kill(true); // Kill with 'complete: true' jumps to end, then resets below.
            _currentTween = null;
        }
        ResetToInitialState(); // Always reset state on stop
    }

    /// <summary>
    /// Pauses the current scale animation.
    /// </summary>
    public void Pause()
    {
        _currentTween?.Pause();
    }

    /// <summary>
    /// Resumes a paused scale animation.
    /// </summary>
    public void Resume()
    {
        _currentTween?.Play(); // DOTween's Play() also resumes a paused tween
    }

    /// <summary>
    /// Resets the panel to its initial scale and interactivity state.
    /// </summary>
    public void ResetToInitialState()
    {
        if (startScaledDown)
        {
            _rectTransform.localScale = startScale;
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null && startScale == Vector3.zero)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }
        else
        {
            _rectTransform.localScale = endScale;
        }
    }
}
```

---

### **5. `UIPanelAnimationTrigger.cs` - Example Usage / Trigger Script**

This script demonstrates how an external component (like a button click handler) interacts with the `UIAnimationSystem` to trigger animations by their `AnimationID`.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button component
using System;

/// <summary>
/// An example script to demonstrate how to trigger UI animations
/// using the UIAnimationSystem.
/// Attach this to a GameObject with a Button component.
/// </summary>
public class UIPanelAnimationTrigger : MonoBehaviour
{
    [Header("Animation IDs to Trigger")]
    [Tooltip("The ID of the animation to play when the button is clicked.")]
    [SerializeField] private string playAnimationID = "MainMenuPanel_FadeIn";
    [Tooltip("The ID of the animation to play when the secondary button is clicked.")]
    [SerializeField] private string secondaryAnimationID = "MainMenuPanel_ScaleOut"; // Example: for a "Hide" button

    [Header("UI References")]
    [Tooltip("Reference to the button that triggers the main animation.")]
    [SerializeField] private Button playButton;
    [Tooltip("Reference to an optional button that triggers a secondary animation (e.g., hide).")]
    [SerializeField] private Button secondaryButton; // e.g., a "Hide" button

    private void Awake()
    {
        if (playButton == null)
        {
            playButton = GetComponent<Button>();
        }

        if (playButton == null)
        {
            Debug.LogError($"UIPanelAnimationTrigger on {gameObject.name}: Play Button reference is missing.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClick);
        }
        if (secondaryButton != null)
        {
            secondaryButton.onClick.AddListener(OnSecondaryButtonClick);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClick);
        }
        if (secondaryButton != null)
        {
            secondaryButton.onClick.RemoveListener(OnSecondaryButtonClick);
        }
    }

    /// <summary>
    /// Handler for the main button click. Triggers the primary animation.
    /// </summary>
    private void OnPlayButtonClick()
    {
        if (UIAnimationSystem.Instance == null)
        {
            Debug.LogError("UIAnimationSystem not found in the scene! Cannot trigger animation.");
            return;
        }

        Debug.Log($"Attempting to play animation: {playAnimationID}");

        // Play the animation. The second parameter is an optional callback
        // that will be executed once the animation completes.
        bool started = UIAnimationSystem.Instance.PlayAnimation(playAnimationID, () =>
        {
            Debug.Log($"Animation '{playAnimationID}' completed!");
            // Example: Do something else after the animation finishes
            // For instance, enable another button, load a scene, etc.
        });

        if (!started)
        {
            Debug.LogWarning($"Failed to start animation '{playAnimationID}'. It might not be registered.");
        }
    }

    /// <summary>
    /// Handler for the secondary button click. Triggers the secondary animation.
    /// </summary>
    private void OnSecondaryButtonClick()
    {
        if (UIAnimationSystem.Instance == null)
        {
            Debug.LogError("UIAnimationSystem not found in the scene! Cannot trigger secondary animation.");
            return;
        }

        Debug.Log($"Attempting to play secondary animation: {secondaryAnimationID}");

        bool started = UIAnimationSystem.Instance.PlayAnimation(secondaryAnimationID, () =>
        {
            Debug.Log($"Animation '{secondaryAnimationID}' completed!");
        });

        if (!started)
        {
            Debug.LogWarning($"Failed to start secondary animation '{secondaryAnimationID}'. It might not be registered.");
        }
    }

    /// <summary>
    /// Example of how you might stop an animation programmatically.
    /// This could be triggered by another event, not necessarily a button.
    /// </summary>
    public void StopCurrentAnimation()
    {
        if (UIAnimationSystem.Instance == null) return;
        if (UIAnimationSystem.Instance.IsAnimationPlaying(playAnimationID))
        {
            UIAnimationSystem.Instance.StopAnimation(playAnimationID);
            Debug.Log($"Animation '{playAnimationID}' stopped.");
        }
        else if (UIAnimationSystem.Instance.IsAnimationPlaying(secondaryAnimationID))
        {
            UIAnimationSystem.Instance.StopAnimation(secondaryAnimationID);
            Debug.Log($"Animation '{secondaryAnimationID}' stopped.");
        }
    }

    /// <summary>
    /// Example of how to chain animations (sequentially) using callbacks.
    /// </summary>
    public void PlayChainedAnimations()
    {
        if (UIAnimationSystem.Instance == null) return;

        Debug.Log("Playing chained animations...");
        UIAnimationSystem.Instance.PlayAnimation("MainMenuPanel_FadeIn", () =>
        {
            Debug.Log("FadeIn complete, starting ScaleOut...");
            UIAnimationSystem.Instance.PlayAnimation("MainMenuPanel_ScaleOut", () =>
            {
                Debug.Log("ScaleOut complete, chain finished!");
            });
        });
    }

    /// <summary>
    /// Example of how to reset all animations for a panel.
    /// </summary>
    public void ResetPanelAnimations()
    {
        if (UIAnimationSystem.Instance == null) return;
        UIAnimationSystem.Instance.ResetAnimationToInitialState(playAnimationID);
        UIAnimationSystem.Instance.ResetAnimationToInitialState(secondaryAnimationID);
        Debug.Log("Panel animations reset to initial state.");
    }
}
```

---

### **How to Set Up in Unity (Example Scene)**

1.  **Create an Empty GameObject for the System:**
    *   In your scene, create an empty GameObject (e.g., `_Managers`).
    *   Add the `UIAnimationSystem` script to it.

2.  **Create a UI Panel:**
    *   Go to `GameObject` -> `UI` -> `Panel`. Name it `MainMenuPanel`.
    *   Ensure it has a `RectTransform` and a `CanvasGroup` component. If not, add `CanvasGroup` (`Add Component` -> `Canvas Group`).

3.  **Add Animation Components to the Panel:**
    *   Select `MainMenuPanel`.
    *   Add the `UIPanelFadeAnimation` script.
        *   Set `AnimationID` to `MainMenuPanel_FadeIn`.
        *   `Start Alpha`: `0`, `End Alpha`: `1`.
        *   `Start Hidden`: `true`.
    *   Add the `UIPanelScaleAnimation` script.
        *   Set `AnimationID` to `MainMenuPanel_ScaleOut`.
        *   `Start Scale`: `Vector3.one` (this will be the starting scale for a 'scale out' animation).
        *   `End Scale`: `Vector3.zero` (to scale it to nothing).
        *   `Start Scaled Down`: `false` (so it assumes it starts at full scale).

    *Self-Correction for `UIPanelScaleAnimation`:* If `MainMenuPanel_FadeIn` sets `startHidden` to `true`, the `CanvasGroup`'s `blocksRaycasts` and `interactable` will be `false`. `UIPanelScaleAnimation` also modifies these based on `startScaledDown`. You need to be mindful of how multiple animations on the same target component interact. For this example, let's make `FadeIn` reveal the panel and `ScaleOut` hide it.

    *   **Revised `UIPanelFadeAnimation` settings for `MainMenuPanel`:**
        *   `AnimationID`: `MainMenuPanel_Show` (rename for clarity)
        *   `Start Alpha`: `0`, `End Alpha`: `1`
        *   `Duration`: `0.5`
        *   `Start Hidden`: `true` (panel starts fully transparent and non-interactive)

    *   **Revised `UIPanelScaleAnimation` settings for `MainMenuPanel`:**
        *   `AnimationID`: `MainMenuPanel_Hide` (rename for clarity)
        *   `Start Scale`: `Vector3.one`, `End Scale`: `Vector3.zero`
        *   `Duration`: `0.4`
        *   `Ease Type`: `Ease.InBack`
        *   `Start Scaled Down`: `false` (panel is assumed to be fully scaled up when this 'hide' animation starts)

4.  **Create Buttons to Trigger Animations:**
    *   Go to `GameObject` -> `UI` -> `Button`. Name it `ShowPanelButton`.
    *   Duplicate it, name the second `HidePanelButton`.
    *   Create another empty GameObject `AnimationControls`. Add the `UIPanelAnimationTrigger` script to it.
    *   **Configure `AnimationControls`:**
        *   Drag `ShowPanelButton` to the `Play Button` slot.
        *   Set `Play Animation ID` to `MainMenuPanel_Show`.
        *   Drag `HidePanelButton` to the `Secondary Button` slot.
        *   Set `Secondary Animation ID` to `MainMenuPanel_Hide`.

5.  **Test in Play Mode:**
    *   Run the scene.
    *   The `MainMenuPanel` should start transparent and non-interactive.
    *   Click `ShowPanelButton`: The panel should fade in and become interactive.
    *   Click `HidePanelButton`: The panel should scale down and become non-interactive.

---

### **Benefits of the UIAnimationSystem Pattern:**

*   **Centralized Control:** All animation logic is abstracted behind the `UIAnimationSystem`, making it easy to manage.
*   **Decoupling:** UI elements don't directly control their animations; they just expose them. Other parts of the code (e.g., `UIPanelAnimationTrigger`, game logic) simply tell the system *which* animation to play by its ID.
*   **Reusability:** Once an `IUIAnimation` implementation (like `UIPanelFadeAnimation`) is created, it can be attached to any UI element and configured with different IDs and parameters.
*   **Extensibility:** Adding new animation types (e.g., slide, bounce, rotation) simply requires creating a new script that implements `IUIAnimation` and registers itself. The `UIAnimationSystem` doesn't need to change.
*   **Maintainability:** If you decide to switch animation libraries (e.g., from DOTween to LeanTween or a custom solution), you only need to modify the concrete `IUIAnimation` implementations, not the rest of your game logic.
*   **Readability:** Code that triggers animations is clean and clear: `UIAnimationSystem.Instance.PlayAnimation("SomePanel_Intro");`
*   **Callback Support:** Easily chain animations or execute code after an animation completes using the `onComplete` Action.

This pattern makes managing complex UI interactions much more organized and scalable in your Unity projects.