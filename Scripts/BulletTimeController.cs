// Unity Design Pattern Example: BulletTimeController
// This script demonstrates the BulletTimeController pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'BulletTimeController' design pattern in Unity allows for dynamic manipulation of the game's time scale, most commonly used to slow down time for dramatic effect (like in "The Matrix" or "Max Payne"). This controller centralizes the logic for initiating, managing, and ending these time-altering effects, ensuring consistency and ease of use throughout your project.

This example provides a robust, production-ready implementation using a **Singleton** pattern for easy global access. It includes smooth transitions, audio pitch adjustments, and handles multiple bullet time requests gracefully.

---

### **How to Use This Script in Unity:**

1.  **Create a New C# Script:** In your Unity project, go to `Assets/Scripts` (or wherever you keep your scripts), right-click, choose `Create > C# Script`, and name it `BulletTimeController`.
2.  **Copy and Paste:** Copy the entire code below and paste it into your new `BulletTimeController.cs` file, replacing its default content.
3.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (right-click in Hierarchy > `Create Empty`). Name it `BulletTimeManager`.
4.  **Attach the Script:** Drag and drop the `BulletTimeController.cs` script onto the `BulletTimeManager` GameObject in the Hierarchy or Inspector.
5.  **Install TextMeshPro (Optional):** If you want the on-screen UI feedback, go to `Window > TextMeshPro > Import TMPro Essential Resources`. Then, create a `UI > Text - TextMeshPro` object in your scene and drag it into the `Status Text` slot on the `BulletTimeManager`'s Inspector.
6.  **Run Your Scene:** Press Play!
    *   Press **'B'** to activate bullet time with default settings.
    *   Press **'N'** to immediately return to normal time.
    *   Press **'V'** to activate a custom, very slow bullet time (0.05x speed for 5 seconds).

---

### **BulletTimeController.cs**

```csharp
using UnityEngine;
using System.Collections;
using TMPro; // Required for TextMeshPro UI components

/// <summary>
/// The BulletTimeController design pattern manages the game's time scale globally.
/// It provides methods to slow down time (bullet time) and smoothly transition
/// back to normal speed, often adjusting audio pitch for realism.
///
/// This implementation uses a Singleton pattern, ensuring only one instance
/// exists and is easily accessible from any script.
/// </summary>
[DisallowMultipleComponent] // Prevents multiple instances on the same GameObject
public class BulletTimeController : MonoBehaviour
{
    // --- Singleton Implementation ---
    // The static instance property allows global access to the controller.
    public static BulletTimeController Instance { get; private set; }

    [Header("Bullet Time Settings")]
    [Tooltip("The target Time.timeScale during bullet time (e.g., 0.1 for 10% speed).")]
    [Range(0.01f, 0.99f)] public float targetSlowMotionFactor = 0.1f;

    [Tooltip("Default duration for bullet time if not specified in a method call.")]
    public float defaultBulletTimeDuration = 2.0f;

    [Tooltip("How quickly time transitions between normal and slow motion (rate per unscaled second).")]
    public float transitionSpeed = 3.0f;

    [Header("Audio Settings")]
    [Tooltip("Whether to adjust AudioListener pitch with time scale for realistic sound effects.")]
    public bool adjustAudioPitch = true;

    [Tooltip("Minimum pitch for AudioListener when time scale is very low. Prevents extreme distortion.")]
    [Range(0.01f, 1.0f)] public float minAudioPitch = 0.5f;

    // --- Private Fields ---
    private float _originalTimeScale; // Stores the initial Time.timeScale (usually 1.0f)
    private float _originalAudioPitch; // Stores the initial AudioListener.pitch (usually 1.0f)

    private bool _isBulletTimeActive; // True if bullet time duration is currently counting down
    private Coroutine _bulletTimeDurationCoroutine; // Manages the countdown of bullet time duration
    private Coroutine _timeScaleTransitionCoroutine; // Manages the smooth transition of Time.timeScale

    // Optional UI Text element for visual feedback in the demo scene.
    [Header("UI Feedback (Optional)")]
    [Tooltip("Assign a TextMeshProUGUI component here to display current time status.")]
    public TextMeshProUGUI statusText;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the Singleton.
    /// </summary>
    private void Awake()
    {
        // Ensure only one instance of the controller exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            // Prevents the manager from being destroyed when loading new scenes.
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    private void Start()
    {
        // Store the initial time scale and audio pitch for resetting.
        _originalTimeScale = Time.timeScale;
        _originalAudioPitch = AudioListener.pitch;
        UpdateStatusText("Normal Time");
    }

    /// <summary>
    /// Example usage: Demonstrates how to trigger bullet time with keyboard input.
    /// </summary>
    private void Update()
    {
        // Example 1: Press 'B' for default bullet time
        if (Input.GetKeyDown(KeyCode.B))
        {
            InitiateBulletTime(); // Uses default values
        }

        // Example 2: Press 'N' to return to normal time immediately
        if (Input.GetKeyDown(KeyCode.N))
        {
            EndBulletTime();
        }

        // Example 3: Press 'V' for a custom, very slow bullet time (e.0.5f% speed for 5 seconds)
        if (Input.GetKeyDown(KeyCode.V))
        {
            InitiateBulletTime(0.05f, 5.0f);
        }
    }

    /// <summary>
    /// Called when the application quits or when the GameObject is destroyed.
    /// Ensures Time.timeScale and AudioListener.pitch are reset to normal.
    /// This is crucial for not leaving the editor in a slow-motion state.
    /// </summary>
    private void OnApplicationQuit()
    {
        ResetTimeAndAudio();
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Ensures Time.timeScale and AudioListener.pitch are reset to normal.
    /// </summary>
    private void OnDestroy()
    {
        // Only reset if this is the active singleton instance being destroyed.
        if (Instance == this)
        {
            ResetTimeAndAudio();
            Instance = null; // Clear the instance reference.
        }
    }

    // --- Public API Methods ---

    /// <summary>
    /// Initiates bullet time with the default slow-motion factor and duration.
    /// </summary>
    public void InitiateBulletTime()
    {
        InitiateBulletTime(targetSlowMotionFactor, defaultBulletTimeDuration);
    }

    /// <summary>
    /// Initiates bullet time with a specified slow-motion factor and duration.
    /// If bullet time is already active, this will restart its duration and potentially
    /// transition to the new slow-motion factor.
    /// </summary>
    /// <param name="slowMotionFactor">The target Time.timeScale (e.g., 0.1 for 10% speed).</param>
    /// <param name="duration">How long the bullet time should last in real-time seconds.</param>
    public void InitiateBulletTime(float slowMotionFactor, float duration)
    {
        // Clamp the slow-motion factor to a safe range.
        slowMotionFactor = Mathf.Clamp(slowMotionFactor, 0.01f, _originalTimeScale);

        // Stop any ongoing transition to prevent conflicts.
        if (_timeScaleTransitionCoroutine != null)
        {
            StopCoroutine(_timeScaleTransitionCoroutine);
        }

        // Start the transition to the target slow motion.
        // Once the transition is complete, start the duration timer.
        _timeScaleTransitionCoroutine = StartCoroutine(TransitionTimeScaleRoutine(slowMotionFactor, () =>
        {
            // Stop any existing duration timer and start a new one.
            if (_bulletTimeDurationCoroutine != null)
            {
                StopCoroutine(_bulletTimeDurationCoroutine);
            }
            _bulletTimeDurationCoroutine = StartCoroutine(BulletTimeDurationRoutine(duration));
            _isBulletTimeActive = true;
            UpdateStatusText($"Bullet Time Active ({(slowMotionFactor / _originalTimeScale) * 100:F0}%)");
            Debug.Log($"Bullet Time Initiated: Factor={slowMotionFactor}, Duration={duration}s");
        }));
    }

    /// <summary>
    /// Immediately ends bullet time and transitions back to normal speed.
    /// If no bullet time is active or already transitioning to normal, this does nothing.
    /// </summary>
    public void EndBulletTime()
    {
        // If already at normal speed or speeding up, do nothing.
        if (Time.timeScale >= _originalTimeScale && !_isBulletTimeActive) return;

        // Stop any active duration countdown.
        if (_bulletTimeDurationCoroutine != null)
        {
            StopCoroutine(_bulletTimeDurationCoroutine);
            _bulletTimeDurationCoroutine = null;
        }

        // Stop any ongoing transition and start a new one back to normal.
        if (_timeScaleTransitionCoroutine != null)
        {
            StopCoroutine(_timeScaleTransitionCoroutine);
        }

        _timeScaleTransitionCoroutine = StartCoroutine(TransitionTimeScaleRoutine(_originalTimeScale, () =>
        {
            _isBulletTimeActive = false;
            UpdateStatusText("Normal Time");
            Debug.Log("Bullet Time Ended.");
        }));
    }

    /// <summary>
    /// Checks if bullet time is currently active (i.e., the game time scale is slowed down).
    /// This returns true even during the transition to or from bullet time.
    /// </summary>
    /// <returns>True if Time.timeScale is less than the original Time.timeScale, or if the duration timer is active.</returns>
    public bool IsBulletTimeActive()
    {
        // Consider bullet time active if the actual timeScale is below normal,
        // or if the duration timer is still running (meaning we're in the slow period).
        return Time.timeScale < _originalTimeScale || _isBulletTimeActive;
    }

    /// <summary>
    /// Gets the current Time.timeScale value.
    /// </summary>
    /// <returns>The current Time.timeScale.</returns>
    public float GetCurrentTimeScale()
    {
        return Time.timeScale;
    }

    // --- Private Coroutines ---

    /// <summary>
    /// Coroutine that manages the duration of the bullet time.
    /// It waits for the specified real-time duration and then ends bullet time.
    /// </summary>
    /// <param name="duration">The duration in real-time seconds.</param>
    private IEnumerator BulletTimeDurationRoutine(float duration)
    {
        // WaitForSecondsRealtime uses unscaled time, so it's not affected by Time.timeScale.
        yield return new WaitForSecondsRealtime(duration);

        // After the duration, end bullet time.
        EndBulletTime();
    }

    /// <summary>
    /// Coroutine that smoothly transitions Time.timeScale and AudioListener.pitch
    /// from the current value to a target value over time.
    /// </summary>
    /// <param name="targetScale">The desired Time.timeScale to transition to.</param>
    /// <param name="onComplete">An optional action to call once the transition is finished.</param>
    private IEnumerator TransitionTimeScaleRoutine(float targetScale, System.Action onComplete = null)
    {
        // Continue transitioning until the current Time.timeScale is very close to the target.
        // Mathf.Approximately is used to handle floating-point precision issues.
        while (!Mathf.Approximately(Time.timeScale, targetScale))
        {
            // MoveTowards ensures a consistent transition speed regardless of frame rate.
            // Time.unscaledDeltaTime is crucial here, as it's not affected by Time.timeScale,
            // ensuring the transition speed itself remains constant.
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetScale, transitionSpeed * Time.unscaledDeltaTime);

            // Adjust audio pitch to match the time scale.
            if (adjustAudioPitch)
            {
                // Linearly interpolate audio pitch based on the current time scale relative to the original.
                // Ensures pitch goes from minAudioPitch (at 0 timeScale) to originalAudioPitch (at original timeScale).
                AudioListener.pitch = Mathf.Lerp(minAudioPitch, _originalAudioPitch, Time.timeScale / _originalTimeScale);
                // Clamp to prevent values outside the desired range.
                AudioListener.pitch = Mathf.Clamp(AudioListener.pitch, minAudioPitch, _originalAudioPitch);
            }

            yield return null; // Wait for the next frame.
        }

        // Ensure Time.timeScale and AudioListener.pitch hit their exact target values.
        Time.timeScale = targetScale;
        if (adjustAudioPitch)
        {
            AudioListener.pitch = Mathf.Lerp(minAudioPitch, _originalAudioPitch, Time.timeScale / _originalTimeScale);
            AudioListener.pitch = Mathf.Clamp(AudioListener.pitch, minAudioPitch, _originalAudioPitch);
        }

        // Invoke the onComplete action if provided.
        onComplete?.Invoke();
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Resets Time.timeScale and AudioListener.pitch to their original values.
    /// Used for cleanup when the application quits or the manager is destroyed.
    /// </summary>
    private void ResetTimeAndAudio()
    {
        Time.timeScale = _originalTimeScale;
        AudioListener.pitch = _originalAudioPitch;
    }

    /// <summary>
    /// Updates an optional UI TextMeshProUGUI component with the current status.
    /// </summary>
    /// <param name="status">The status string to display.</param>
    private void UpdateStatusText(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }
}

/*
/// --- Example Usage from Another Script ---
/// You can call BulletTimeController methods from any other script like this:
///
/// public class MyPlayerController : MonoBehaviour
/// {
///     void Update()
///     {
///         // Trigger default bullet time on left mouse click
///         if (Input.GetMouseButtonDown(0))
///         {
///             // Ensure the controller exists before trying to access it
///             if (BulletTimeController.Instance != null)
///             {
///                 BulletTimeController.Instance.InitiateBulletTime();
///             }
///         }
///
///         // Trigger a specific bullet time (e.g., 20% speed for 3 seconds) on right mouse click
///         if (Input.GetMouseButtonDown(1))
///         {
///             if (BulletTimeController.Instance != null)
///             {
///                 BulletTimeController.Instance.InitiateBulletTime(0.2f, 3.0f);
///             }
///         }
///
///         // End bullet time immediately on 'E' key press
///         if (Input.GetKeyDown(KeyCode.E))
///         {
///             if (BulletTimeController.Instance != null)
///             {
///                 BulletTimeController.Instance.EndBulletTime();
///             }
///         }
///
///         // Check if bullet time is active
///         if (BulletTimeController.Instance != null && BulletTimeController.Instance.IsBulletTimeActive())
///         {
///             Debug.Log("Bullet time is currently active!");
///         }
///     }
/// }
*/
```