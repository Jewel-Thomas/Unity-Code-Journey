// Unity Design Pattern Example: SlowMotionController
// This script demonstrates the SlowMotionController pattern in Unity
// Generated automatically - ready to use in your Unity project

The `SlowMotionController` design pattern centralizes the management of Unity's `Time.timeScale` to create smooth, non-conflicting slow-motion effects. Instead of individual game objects directly modifying `Time.timeScale`, they request slow-motion or release it through this central controller. This approach offers several benefits:

1.  **Conflict Resolution:** Multiple systems (e.g., player hit effect, explosion, bullet-time ability) can all request slow motion without overwriting each other's desired `timeScale` or duration. The controller automatically determines the "deepest" slow-motion factor and the longest active duration.
2.  **Smooth Transitions:** All changes to `Time.timeScale` are handled with smooth lerping, avoiding jarring instantaneous jumps.
3.  **Centralized Logic:** All complex logic related to `Time.timeScale` (e.g., handling `Time.unscaledTime` for durations, managing active requests) is encapsulated in one place.
4.  **Extensibility:** Easy to add features like "pause" states, specific transition curves, or different slow-motion profiles.

This example provides a robust, production-ready implementation of a `SlowMotionController` for Unity.

---

### `SlowMotionController.cs`

To use this, create an empty GameObject in your scene, name it "SlowMotionController," and attach this script to it.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .Any(), .Min(), .Max()

/// <summary>
/// SlowMotionController: A centralized manager for slow-motion effects.
/// This script implements the SlowMotionController design pattern, allowing multiple game systems
/// to request and release slow-motion effects without conflicting. It handles smooth transitions
/// and prioritizes requests (e.g., deeper slow-motion, longer duration).
/// </summary>
public class SlowMotionController : MonoBehaviour
{
    // Singleton pattern for easy global access from any script.
    // Make sure there is only one SlowMotionController in your scene.
    public static SlowMotionController Instance { get; private set; }

    [Header("Default Slow Motion Settings")]
    [Tooltip("The default time scale factor during slow motion (e.g., 0.3 means 30% speed).")]
    [SerializeField] private float _defaultSlowMotionFactor = 0.3f;
    [Tooltip("The default duration for a slow motion effect (in unscaled seconds).")]
    [SerializeField] private float _defaultSlowMotionDuration = 2.0f;
    [Tooltip("The default time it takes to smoothly transition into or out of slow motion.")]
    [SerializeField] private float _defaultTransitionDuration = 0.5f;

    // Internal class to hold details for an active slow motion request.
    // Using a class here allows for easier modification within the list and avoids copying.
    private class SlowMotionRequest
    {
        // A unique identifier for this request (e.g., "PlayerHit", "Explosion").
        // Used to update or release specific requests.
        public string Id { get; }
        // The target time scale this specific request wants to achieve (clamped between 0.001f and 1.0f).
        public float TargetTimeScale { get; }
        // When this request was made (using Time.unscaledTime for accuracy).
        public float StartTime { get; }
        // The total duration this request should last.
        public float Duration { get; }
        // The preferred time to transition into this slow motion state.
        public float TransitionInDuration { get; }
        // The preferred time to transition out of this slow motion state.
        public float TransitionOutDuration { get; }
        // Calculated end time for this request (StartTime + Duration).
        public float EndTime => StartTime + Duration;

        public SlowMotionRequest(string id, float targetTimeScale, float duration, float transitionIn, float transitionOut)
        {
            // If no ID is provided, generate a new GUID to ensure uniqueness.
            Id = string.IsNullOrEmpty(id) ? System.Guid.NewGuid().ToString() : id;
            // Clamp targetTimeScale to prevent issues with Time.timeScale = 0 (which halts Update and most coroutines).
            // 0.001f is effectively paused for gameplay but allows Unity's internal systems and unscaled coroutines to run.
            TargetTimeScale = Mathf.Clamp(targetTimeScale, 0.001f, 1.0f);
            StartTime = Time.unscaledTime;
            Duration = Mathf.Max(0, duration); // Ensure duration is not negative.
            TransitionInDuration = Mathf.Max(0, transitionIn);
            TransitionOutDuration = Mathf.Max(0, transitionOut);
        }
    }

    // A list to keep track of all currently active slow-motion requests.
    private readonly List<SlowMotionRequest> _activeRequests = new List<SlowMotionRequest>();
    // Reference to the currently running transition coroutine, allowing it to be stopped.
    private Coroutine _currentTransitionCoroutine;

    // The target time scale that we are currently either at or transitioning towards.
    private float _currentCalculatedTargetTimeScale = 1.0f;

    /// <summary>
    /// Initializes the singleton instance and ensures the time scale is normal.
    /// </summary>
    private void Awake()
    {
        // Singleton enforcement: destroy duplicates.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SlowMotionController: Multiple instances found. Destroying duplicate '" + gameObject.name + "'.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Keep the controller alive across scene loads.
            DontDestroyOnLoad(gameObject);
        }

        // Ensure time scale is normal when the controller starts up.
        Time.timeScale = 1.0f;
        _currentCalculatedTargetTimeScale = 1.0f;
        Debug.Log("SlowMotionController initialized. Time.timeScale set to 1.0f.");
    }

    /// <summary>
    /// Checks for expired slow-motion requests and updates the global time scale if necessary.
    /// This uses Time.unscaledTime to ensure it functions correctly even when timeScale is very low.
    /// </summary>
    private void Update()
    {
        bool changed = false;
        // Iterate backwards to safely remove elements during iteration.
        for (int i = _activeRequests.Count - 1; i >= 0; i--)
        {
            // If the request's end time has passed (using unscaled time), remove it.
            if (Time.unscaledTime >= _activeRequests[i].EndTime)
            {
                // Debug.Log($"SlowMotionController: Request '{_activeRequests[i].Id}' expired.");
                _activeRequests.RemoveAt(i);
                changed = true;
            }
        }

        // If any requests were removed, recalculate the desired time scale.
        if (changed)
        {
            UpdateInternalState();
        }
    }

    /// <summary>
    /// Requests a slow-motion effect.
    /// If an existing request with the same ID is present, it will be updated/overridden with the new parameters.
    /// Otherwise, a new request is added to the stack. The controller automatically
    /// applies the deepest (lowest time scale) and longest-lasting effect among all active requests.
    /// </summary>
    /// <param name="id">A unique identifier for this request (e.g., "PlayerHit", "Explosion").
    /// If null or empty, a GUID will be generated. Use the same ID to update or release specific requests.</param>
    /// <param name="factor">The target time scale (e.g., 0.5 for half speed). Clamped between 0.001f and 1.0f.
    /// Uses default if less than 0.</param>
    /// <param name="duration">How long the slow-motion effect should last (in unscaled seconds).
    /// Uses default if less than 0.</param>
    /// <param name="transitionInDuration">Time to smoothly transition into the slow motion.
    /// Uses default if less than 0.</param>
    /// <param name="transitionOutDuration">Time to smoothly transition out of the slow motion (only used when
    /// this specific request is the highest priority when exiting slow motion). Uses default if less than 0.</param>
    /// <returns>The ID of the request, either the one provided or a newly generated GUID.</returns>
    public string RequestSlowMotion(
        string id = null,
        float factor = -1f, // Sentinel value to use default
        float duration = -1f,
        float transitionInDuration = -1f,
        float transitionOutDuration = -1f)
    {
        // Apply default values if not explicitly provided (indicated by -1f).
        if (factor < 0) factor = _defaultSlowMotionFactor;
        if (duration < 0) duration = _defaultSlowMotionDuration;
        if (transitionInDuration < 0) transitionInDuration = _defaultTransitionDuration;
        if (transitionOutDuration < 0) transitionOutDuration = _defaultTransitionDuration;

        // Create a new request object. The constructor handles ID generation if null/empty.
        SlowMotionRequest newRequest = new SlowMotionRequest(id, factor, duration, transitionInDuration, transitionOutDuration);

        // Check if a request with this ID already exists.
        // If it does, we update it; otherwise, we add a new one.
        // This ensures that multiple calls for the same event type (e.g., "PlayerHit") extend or override.
        int existingIndex = _activeRequests.FindIndex(r => r.Id == newRequest.Id);
        if (existingIndex != -1)
        {
            _activeRequests[existingIndex] = newRequest; // Replace the old request with the new one
            // Debug.Log($"SlowMotionController: Updated slow motion request '{newRequest.Id}'. Factor: {newRequest.TargetTimeScale}, Duration: {newRequest.Duration}");
        }
        else
        {
            _activeRequests.Add(newRequest); // Add new request to the list.
            // Debug.Log($"SlowMotionController: Added new slow motion request '{newRequest.Id}'. Factor: {newRequest.TargetTimeScale}, Duration: {newRequest.Duration}");
        }

        UpdateInternalState(); // Re-evaluate the global time scale based on all active requests.
        return newRequest.Id; // Return the ID used for this request.
    }

    /// <summary>
    /// Manually releases a specific slow-motion request before its duration expires.
    /// This is useful for persistent slow-motion states (e.g., "focus mode") that
    /// don't have a fixed duration, or for overriding timed requests.
    /// </summary>
    /// <param name="id">The unique ID of the request to release.</param>
    public void ReleaseSlowMotion(string id)
    {
        int removedCount = _activeRequests.RemoveAll(r => r.Id == id);
        if (removedCount > 0)
        {
            // Debug.Log($"SlowMotionController: Released slow motion request '{id}'.");
            UpdateInternalState(); // Re-evaluate the global time scale.
        }
    }

    /// <summary>
    /// Forces the time scale back to normal (1.0f) immediately or with a transition,
    /// removing all pending slow-motion requests.
    /// </summary>
    /// <param name="transitionDuration">Duration to transition back to normal time. Uses default if -1.</param>
    public void ForceNormalTime(float transitionDuration = -1f)
    {
        // Debug.Log("SlowMotionController: Forcing normal time scale.");
        _activeRequests.Clear(); // Clear all active requests.
        if (transitionDuration < 0) transitionDuration = _defaultTransitionDuration;
        UpdateInternalState(transitionDuration); // Update state with the specified transition.
    }

    /// <summary>
    /// Checks if a slow motion request with the given ID is currently active.
    /// </summary>
    /// <param name="id">The ID of the request to check.</param>
    /// <returns>True if the request is active, false otherwise.</returns>
    public bool IsSlowMotionActive(string id)
    {
        return _activeRequests.Any(r => r.Id == id);
    }

    /// <summary>
    /// Checks if any slow motion requests are currently active.
    /// </summary>
    /// <returns>True if any slow motion is active, false otherwise.</returns>
    public bool IsAnySlowMotionActive()
    {
        return _activeRequests.Any();
    }

    /// <summary>
    /// Gets the current Time.timeScale value.
    /// </summary>
    public float GetCurrentTimeScale()
    {
        return Time.timeScale;
    }

    /// <summary>
    /// Internal method to recalculate the desired global time scale and initiate transitions.
    /// This is called whenever a request is added, removed, or expires.
    /// </summary>
    /// <param name="specificTransitionDuration">Optional: Override the calculated transition duration.</param>
    private void UpdateInternalState(float? specificTransitionDuration = null)
    {
        float targetTimeScale = 1.0f; // Default to normal speed if no requests.
        float transitionDuration = _defaultTransitionDuration; // Default transition.

        if (_activeRequests.Any())
        {
            // Find the deepest (lowest) slow motion factor among all active requests.
            targetTimeScale = _activeRequests.Min(r => r.TargetTimeScale);

            // Determine the transition duration.
            // If we are currently going to a slower state, use the maximum TransitionInDuration from active requests.
            // If we are going to a faster state (or back to normal), use the maximum TransitionOutDuration.
            if (targetTimeScale < Time.timeScale) // Going slower
            {
                transitionDuration = _activeRequests.Max(r => r.TransitionInDuration);
            }
            else if (targetTimeScale > Time.timeScale) // Going faster (or back to 1.0f)
            {
                transitionDuration = _activeRequests.Max(r => r.TransitionOutDuration);
            }
            else // Target time scale is the same as current time scale, but a new request might have changed durations
            {
                // In this case, we're not changing speed, but if we had to transition (e.g. from current actual Time.timeScale
                // to the desired targetTimeScale if they weren't equal yet), we'd take the max of both.
                transitionDuration = Mathf.Max(_activeRequests.Max(r => r.TransitionInDuration), _activeRequests.Max(r => r.TransitionOutDuration));
            }
        }
        
        // If a specific transition duration was provided (e.g., by ForceNormalTime), use that.
        if (specificTransitionDuration.HasValue)
        {
            transitionDuration = specificTransitionDuration.Value;
        }

        // Only start a new transition if:
        // 1. The calculated target time scale is different from our current internal target, OR
        // 2. We are not currently at the target time scale AND there's no transition running (meaning we stopped abruptly or are stuck).
        if (!Mathf.Approximately(_currentCalculatedTargetTimeScale, targetTimeScale) ||
            (!Mathf.Approximately(Time.timeScale, targetTimeScale) && _currentTransitionCoroutine == null))
        {
            _currentCalculatedTargetTimeScale = targetTimeScale; // Update our internal target.
            
            // Stop any existing transition coroutine to start a new one with updated parameters.
            if (_currentTransitionCoroutine != null)
            {
                StopCoroutine(_currentTransitionCoroutine);
            }
            // Debug.Log($"SlowMotionController: Initiating time scale transition to {targetTimeScale} over {transitionDuration}s.");
            _currentTransitionCoroutine = StartCoroutine(SmoothlyChangeTimeScale(targetTimeScale, transitionDuration));
        }
    }

    /// <summary>
    /// Coroutine to smoothly change Time.timeScale over a specified duration.
    /// Uses unscaled time for its internal timer to ensure accurate transitions
    /// regardless of the current Time.timeScale value.
    /// </summary>
    /// <param name="targetScale">The desired Time.timeScale value.</param>
    /// <param name="duration">The time (in unscaled seconds) it takes to reach the target scale.</param>
    private IEnumerator SmoothlyChangeTimeScale(float targetScale, float duration)
    {
        float startScale = Time.timeScale;
        float timer = 0f;

        // If duration is zero or very small, set the time scale immediately.
        if (duration <= 0.001f)
        {
            Time.timeScale = targetScale;
            _currentTransitionCoroutine = null; // Mark coroutine as finished.
            yield break;
        }

        while (timer < duration)
        {
            // Use Time.unscaledDeltaTime for the timer to ensure consistent speed
            // even if Time.timeScale is already very low.
            timer += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, timer / duration);
            yield return null; // Wait for the next frame before continuing.
        }

        Time.timeScale = targetScale; // Ensure the target is precisely hit at the end.
        _currentTransitionCoroutine = null; // Mark coroutine as finished.
        // Debug.Log($"SlowMotionController: Time scale transition finished. Current Time.timeScale: {Time.timeScale}.");
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Clean up references.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            // Reset Time.timeScale to normal just in case the controller is destroyed during slow-mo.
            if (!Mathf.Approximately(Time.timeScale, 1.0f))
            {
                Time.timeScale = 1.0f;
                Debug.Log("SlowMotionController destroyed. Resetting Time.timeScale to 1.0f.");
            }
        }
    }
}
```

---

### Example Usage in Another Script (`ExampleGameLogic.cs`)

This example script demonstrates how different game elements would interact with the `SlowMotionController`. Create a new C# script named `ExampleGameLogic.cs`, paste the code below, and attach it to any active GameObject in your scene (e.g., your Player GameObject).

```csharp
using UnityEngine;

/// <summary>
/// ExampleGameLogic demonstrates how various game systems can interact
/// with the SlowMotionController to request and release slow-motion effects.
/// </summary>
public class ExampleGameLogic : MonoBehaviour
{
    [Header("Player Hit Settings")]
    [Tooltip("The ID for slow motion triggered by player taking damage.")]
    [SerializeField] private string _playerHitSlowMoId = "PlayerHit";
    [Tooltip("Factor for player hit slow motion (e.g., 0.1 for 10% speed).")]
    [SerializeField] private float _playerHitFactor = 0.1f;
    [Tooltip("Duration for player hit slow motion.")]
    [SerializeField] private float _playerHitDuration = 0.8f;
    [Tooltip("Transition time into player hit slow motion.")]
    [SerializeField] private float _playerHitTransitionIn = 0.1f;
    [Tooltip("Transition time out of player hit slow motion.")]
    [SerializeField] private float _playerHitTransitionOut = 0.3f;

    [Header("Bullet Time Ability Settings")]
    [Tooltip("The ID for slow motion triggered by a 'Bullet Time' ability.")]
    [SerializeField] private string _bulletTimeAbilityId = "BulletTimeAbility";
    [Tooltip("Factor for bullet time slow motion (e.g., 0.4 for 40% speed).")]
    [SerializeField] private float _bulletTimeFactor = 0.4f;
    [Tooltip("Duration for bullet time slow motion.")]
    [SerializeField] private float _bulletTimeDuration = 5.0f;
    [Tooltip("Transition time into bullet time slow motion.")]
    [SerializeField] private float _bulletTimeTransitionIn = 0.5f;
    [Tooltip("Transition time out of bullet time slow motion.")]
    [SerializeField] private float _bulletTimeTransitionOut = 0.5f;

    private void Update()
    {
        // Ensure the SlowMotionController instance is available.
        if (SlowMotionController.Instance == null)
        {
            Debug.LogWarning("ExampleGameLogic: SlowMotionController instance not found. Make sure it's in the scene.", this);
            return;
        }

        // --- Simulate Player Taking Damage (e.g., on collision) ---
        // Press 'F' to simulate the player taking damage, triggering a short, intense slow-mo.
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Player took damage! Requesting intense slow motion.");
            SlowMotionController.Instance.RequestSlowMotion(
                id: _playerHitSlowMoId,
                factor: _playerHitFactor,
                duration: _playerHitDuration,
                transitionInDuration: _playerHitTransitionIn,
                transitionOutDuration: _playerHitTransitionOut
            );
        }

        // --- Simulate a "Bullet Time" Ability (toggle on/off) ---
        // Press 'E' to toggle a longer, less intense slow-mo ability.
        if (Input.GetKeyDown(KeyCode.E))
        {
            // If bullet time is currently active, release it.
            if (SlowMotionController.Instance.IsSlowMotionActive(_bulletTimeAbilityId))
            {
                SlowMotionController.Instance.ReleaseSlowMotion(_bulletTimeAbilityId);
                Debug.Log("Bullet Time deactivated.");
            }
            // Otherwise, activate bullet time.
            else
            {
                Debug.Log("Bullet Time activated! Requesting moderate slow motion.");
                SlowMotionController.Instance.RequestSlowMotion(
                    id: _bulletTimeAbilityId,
                    factor: _bulletTimeFactor,
                    duration: _bulletTimeDuration,
                    transitionInDuration: _bulletTimeTransitionIn,
                    transitionOutDuration: _bulletTimeTransitionOut
                );
            }
        }

        // --- Simulate a Global "Panic Button" or UI Pause ---
        // Press 'Space' to instantly force time back to normal, overriding all current effects.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SlowMotionController.Instance.ForceNormalTime(0.2f); // Quick transition back to normal
            Debug.Log("Forcing normal time scale with quick transition.");
        }

        // --- Display current time scale in console (for debugging) ---
        if (Time.frameCount % 60 == 0) // Log once per second (approx)
        {
            // Debug.Log($"Current Time.timeScale: {Time.timeScale:F3}");
        }
    }

    private void OnGUI()
    {
        // Simple GUI for interaction instructions
        GUI.Label(new Rect(10, 10, 300, 30), "Press 'F' for Player Hit (short, intense slow-mo)");
        GUI.Label(new Rect(10, 40, 300, 30), "Press 'E' to toggle Bullet Time (longer, moderate slow-mo)");
        GUI.Label(new Rect(10, 70, 300, 30), "Press 'Space' to Force Normal Time (resets all)");
        GUI.Label(new Rect(10, 100, 300, 30), $"Current Time.timeScale: {Time.timeScale:F3}");
    }
}
```