// Unity Design Pattern Example: CinematicTimelineSystem
// This script demonstrates the CinematicTimelineSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **CinematicTimelineSystem** design pattern in Unity focuses on orchestrating a sequence of time-based actions, often for cinematics, narrative events, or complex gameplay sequences. It provides a structured way to define individual actions (events or clips) and then sequence them over time, with a central controller managing their execution.

This example demonstrates the pattern using `ScriptableObject` for individual events and a `MonoBehaviour` for the main timeline controller, leveraging Unity's Coroutine system for asynchronous execution.

---

### CinematicTimelineSystem Pattern Breakdown:

1.  **`TimelineEvent` (Abstract Base Class / Interface):**
    *   Defines the common interface for all actions that can be part of the timeline.
    *   In this example, it's an `abstract class` inheriting from `ScriptableObject`, which allows individual events to be created as assets in the Unity project.
    *   It specifies an `IEnumerator Play(MonoBehaviour owner)` method. This is key: each event is responsible for its own logic and duration, yielding control back to the timeline controller when it's done.

2.  **Concrete `TimelineEvent` Implementations:**
    *   These are specific actions derived from `TimelineEvent`.
    *   Examples: `DebugLogEvent` (logs a message), `CameraMoveEvent` (moves the camera), `SimpleAnimationEvent` (plays an animation).
    *   Each implements the `Play` method, defining *what* happens and *for how long*.

3.  **`CinematicTimeline` (Orchestrator - `MonoBehaviour`):**
    *   This is the central controller responsible for:
        *   Holding a list of `TimelineEvent` assets.
        *   Iterating through the events in sequence.
        *   Starting and stopping the events by calling their `Play` method and yielding their results.
        *   Managing the overall state (playing, paused, stopped).
        *   Providing methods to control the timeline (e.g., `PlayTimeline()`, `StopTimeline()`).
        *   Emits events (using C# `Action` delegates) for external systems to react to timeline progression.

---

### Key Advantages of this Pattern:

*   **Modularity:** Individual `TimelineEvent`s are self-contained and reusable. You can create new types of events without modifying the `CinematicTimeline` controller.
*   **Decoupling:** The controller doesn't need to know the specifics of each event, only that it can `Play()` it.
*   **Flexibility:** Easily reorder, add, or remove events in the Inspector (since they are ScriptableObjects).
*   **Extensibility:** Simple to add new event types by just creating a new class derived from `TimelineEvent`.
*   **Readability:** The sequence of events is clear in the controller's list.

---

### C# Unity Example: CinematicTimelineSystem

This single script file contains all necessary components. You can save it as `CinematicTimeline.cs` in your Unity project.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action delegates

/// <summary>
/// --- CinematicTimelineSystem Design Pattern ---
///
/// This pattern organizes and executes a sequence of time-based actions (events)
/// for cinematics, narrative segments, or complex gameplay sequences.
///
/// It consists of three main parts:
/// 1.  An abstract base class/interface for individual Timeline Events.
/// 2.  Concrete implementations of these events for specific actions.
/// 3.  A central controller to sequence and manage the execution of these events.
///
/// Advantages:
/// -   Modular: Events are self-contained and reusable.
/// -   Extensible: Easily add new event types without changing the core timeline logic.
/// -   Decoupled: The timeline controller doesn't need to know the specifics of each event.
/// -   Configurable: Using ScriptableObjects allows events to be created and configured
///     as assets in the Unity Editor, then dragged into the timeline's list.
/// </summary>

// ---------------------------------------------------------------------------------------------------
// 1. TimelineEvent: The Abstract Base Class for all individual timeline actions.
//    Inheriting from ScriptableObject allows these events to be created as assets.
// ---------------------------------------------------------------------------------------------------
public abstract class TimelineEvent : ScriptableObject
{
    [Tooltip("Duration of this event in seconds.")]
    public float Duration = 1.0f;

    [Tooltip("A descriptive name for this event, useful for debugging and organization.")]
    public string Description = "Unnamed Event";

    /// <summary>
    /// The core method for an event to execute its logic.
    /// This method should return an IEnumerator, allowing the CinematicTimeline controller
    /// to yield it, meaning the event manages its own timing and signals completion
    /// by exhausting its coroutine.
    /// </summary>
    /// <param name="owner">
    /// The MonoBehaviour that owns and runs the timeline. This is typically the
    /// CinematicTimeline component itself, and is used to start nested coroutines
    /// within the event (e.g., for WaitForSeconds or custom lerping).
    /// </param>
    public abstract IEnumerator Play(MonoBehaviour owner);
}

// ---------------------------------------------------------------------------------------------------
// 2. Concrete TimelineEvent Implementations: Specific actions that can be part of the timeline.
//    These classes define the actual logic for what happens during an event.
// ---------------------------------------------------------------------------------------------------

/// <summary>
/// A simple TimelineEvent that logs a message to the console and waits for its duration.
/// </summary>
[CreateAssetMenu(fileName = "LogEvent", menuName = "Cinematic Timeline/Events/Log Message Event", order = 1)]
public class DebugLogEvent : TimelineEvent
{
    [TextArea]
    [Tooltip("The message to log to the console when this event plays.")]
    public string Message;

    public override IEnumerator Play(MonoBehaviour owner)
    {
        Debug.Log($"<color=cyan><b>Timeline Event:</b></color> {Description} - <color=white>{Message}</color>");
        // Wait for the specified duration before signaling completion.
        yield return new WaitForSeconds(Duration);
    }
}

/// <summary>
/// A TimelineEvent that moves and rotates a specified Transform (e.g., a Camera)
/// to a target position and rotation over its duration.
/// </summary>
[CreateAssetMenu(fileName = "CameraMoveEvent", menuName = "Cinematic Timeline/Events/Camera Move Event", order = 2)]
public class CameraMoveEvent : TimelineEvent
{
    [Header("Camera Move Settings")]
    [Tooltip("The GameObject's Transform to move (e.g., the Main Camera's Transform).")]
    public Transform TargetTransform;
    [Tooltip("The target position to move the Transform to.")]
    public Vector3 TargetPosition;
    [Tooltip("The target rotation (Euler angles) for the Transform.")]
    public Vector3 TargetRotation;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    public override IEnumerator Play(MonoBehaviour owner)
    {
        if (TargetTransform == null)
        {
            Debug.LogError($"<color=red><b>Timeline Error:</b></color> {Description} - Target Transform is not assigned! Skipping event.");
            yield break; // Exit coroutine immediately
        }

        Debug.Log($"<color=cyan><b>Timeline Event:</b></color> {Description} - Moving {TargetTransform.name} from {TargetTransform.position} to {TargetPosition}.");

        // Store initial state to smoothly interpolate from current position
        _initialPosition = TargetTransform.position;
        _initialRotation = TargetTransform.rotation;

        float timer = 0f;
        while (timer < Duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / Duration); // Normalized time (0 to 1)
            
            // Lerp position and Slerp rotation for smooth movement
            TargetTransform.position = Vector3.Lerp(_initialPosition, TargetPosition, t);
            TargetTransform.rotation = Quaternion.Slerp(_initialRotation, Quaternion.Euler(TargetRotation), t);
            
            yield return null; // Wait for the next frame
        }

        // Ensure the target reaches the exact final position/rotation
        TargetTransform.position = TargetPosition;
        TargetTransform.rotation = Quaternion.Euler(TargetRotation);
    }
}

/// <summary>
/// A TimelineEvent that plays a specific animation state on an Animator component.
/// It can optionally wait for the animation to complete its natural duration.
/// </summary>
[CreateAssetMenu(fileName = "PlayAnimationEvent", menuName = "Cinematic Timeline/Events/Play Animation Event", order = 3)]
public class SimpleAnimationEvent : TimelineEvent
{
    [Header("Animation Settings")]
    [Tooltip("The Animator component to control.")]
    public Animator TargetAnimator;
    [Tooltip("The name of the animation state (or trigger parameter) to activate.")]
    public string AnimationStateName;
    [Tooltip("If true, the timeline will wait for the animation state to complete its natural duration. " +
             "If false, it will wait for the event's 'Duration' field. " +
             "Note: Requires the animation state to actually exist in the Animator Controller.")]
    public bool WaitForAnimationCompletion = true;

    public override IEnumerator Play(MonoBehaviour owner)
    {
        if (TargetAnimator == null)
        {
            Debug.LogError($"<color=red><b>Timeline Error:</b></color> {Description} - Target Animator is not assigned! Skipping event.");
            yield break;
        }
        if (string.IsNullOrEmpty(AnimationStateName))
        {
            Debug.LogError($"<color=red><b>Timeline Error:</b></color> {Description} - Animation State Name is empty! Skipping event.");
            yield break;
        }

        Debug.Log($"<color=cyan><b>Timeline Event:</b></color> {Description} - Playing animation '{AnimationStateName}' on {TargetAnimator.name}.");

        // Activate the animation state. This assumes AnimationStateName refers to a state name
        // within the Animator Controller. For triggers, use TargetAnimator.SetTrigger().
        TargetAnimator.Play(AnimationStateName);

        if (WaitForAnimationCompletion)
        {
            // We need to wait a frame for the animator to update and transition to the new state
            yield return null; 

            // Get current state info to find its length
            AnimatorStateInfo stateInfo = TargetAnimator.GetCurrentAnimatorStateInfo(0); // Layer 0
            
            // Check if the animator is actually in the desired state
            if (stateInfo.IsName(AnimationStateName))
            {
                // Wait for the animation's actual length.
                // Note: For looping animations or complex state machines with transitions,
                // this might not be precise. A better approach for loops would be to
                // wait for a certain number of loops or a specific event within the animation.
                yield return new WaitForSeconds(stateInfo.length);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>Timeline Warning:</b></color> {Description} - Animation state '{AnimationStateName}' not found or not active on layer 0. Waiting for event's general duration ({Duration}s) instead.");
                yield return new WaitForSeconds(Duration);
            }
        }
        else
        {
            // If not waiting for animation completion, just wait for the event's general duration.
            yield return new WaitForSeconds(Duration);
        }
    }
}


// ---------------------------------------------------------------------------------------------------
// 3. CinematicTimeline: The main controller (orchestrator) for the timeline system.
//    This MonoBehaviour sequences and executes the TimelineEvents.
// ---------------------------------------------------------------------------------------------------

/// <summary>
/// The main controller for orchestrating a sequence of TimelineEvents.
/// This component embodies the 'Cinematic Timeline System' pattern by:
/// 1.  Holding a list of generic TimelineEvent instances (which are ScriptableObjects).
/// 2.  Providing methods to start, pause, resume, and stop the timeline.
/// 3.  Iterating through the events, executing each one sequentially using coroutines.
/// 4.  Emitting events (C# Actions) for external systems to react to timeline progression.
/// </summary>
public class CinematicTimeline : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("List of events to play in sequence. Drag your TimelineEvent ScriptableObjects here.")]
    public List<TimelineEvent> timelineEvents = new List<TimelineEvent>();

    [Tooltip("Automatically start the timeline when the scene begins.")]
    public bool PlayOnAwake = true;

    [Tooltip("Log detailed information about timeline progression to the console.")]
    public bool DebugLogging = true;

    [Header("Events (for external subscriptions)")]
    // Actions for external scripts to subscribe to
    public Action OnTimelineStarted;
    public Action OnTimelineCompleted;
    public Action<TimelineEvent> OnEventStarted;
    public Action<TimelineEvent> OnEventCompleted;

    private int _currentEventIndex = -1; // -1 indicates timeline has not started or is reset
    private Coroutine _timelineCoroutine;
    private bool _isPlaying = false;

    // Public accessors for timeline state
    public bool IsPlaying => _isPlaying;
    public int CurrentEventIndex => _currentEventIndex;

    void Start()
    {
        if (PlayOnAwake)
        {
            PlayTimeline();
        }
    }

    /// <summary>
    /// Starts the cinematic timeline from the beginning.
    /// If already playing, it will stop and restart.
    /// </summary>
    public void PlayTimeline()
    {
        StopTimeline(); // Ensure any existing timeline run is stopped
        _currentEventIndex = -1; // Reset to before the first event
        _isPlaying = true;
        _timelineCoroutine = StartCoroutine(RunTimeline());
        if (DebugLogging) Debug.Log("<color=green><b>Cinematic Timeline:</b></color> Timeline Started!");
        OnTimelineStarted?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Pauses the cinematic timeline at its current event.
    /// </summary>
    public void PauseTimeline()
    {
        if (_timelineCoroutine != null && _isPlaying)
        {
            StopCoroutine(_timelineCoroutine); // Stop the coroutine
            _isPlaying = false;
            if (DebugLogging) Debug.Log("<color=orange><b>Cinematic Timeline:</b></color> Timeline Paused.");
        }
    }

    /// <summary>
    /// Resumes the cinematic timeline from its paused state.
    /// If not paused, this method does nothing.
    /// </summary>
    public void ResumeTimeline()
    {
        // Only resume if not currently playing, but the timeline was previously started
        if (!_isPlaying && _currentEventIndex < timelineEvents.Count && _currentEventIndex >= -1)
        {
            _isPlaying = true;
            // Restart the coroutine from its current state (it will pick up where it left off)
            _timelineCoroutine = StartCoroutine(RunTimeline());
            if (DebugLogging) Debug.Log("<color=green><b>Cinematic Timeline:</b></color> Timeline Resumed.");
        }
    }

    /// <summary>
    /// Stops the cinematic timeline completely and resets its state to the beginning.
    /// </summary>
    public void StopTimeline()
    {
        if (_timelineCoroutine != null)
        {
            StopCoroutine(_timelineCoroutine); // Stop the coroutine
            _timelineCoroutine = null;
        }
        _isPlaying = false;
        _currentEventIndex = -1; // Reset event index
        if (DebugLogging) Debug.Log("<color=red><b>Cinematic Timeline:</b></color> Timeline Stopped and Reset.");
    }

    /// <summary>
    /// The main coroutine that drives the timeline. It iterates through the
    /// `timelineEvents` list, playing each event sequentially.
    /// </summary>
    private IEnumerator RunTimeline()
    {
        // Continue from the current event index (which might be -1 for new start,
        // or the index of the event that was paused).
        // If _currentEventIndex is -1, it increments to 0 for the first event.
        // If it's already at an event, it will re-enter that event's Play method
        // if it was paused in the middle of it. This might need refinement for
        // events that aren't idempotent or can't be resumed easily.
        // For simplicity, we'll assume events restart their internal coroutine from scratch.
        // A more advanced system might track progress *within* an event.

        // Loop through all events from the current index onwards
        for (int i = _currentEventIndex + 1; i < timelineEvents.Count; i++)
        {
            _currentEventIndex = i; // Update the current event index
            TimelineEvent currentEvent = timelineEvents[_currentEventIndex];

            if (currentEvent == null)
            {
                Debug.LogWarning($"<color=yellow><b>Cinematic Timeline:</b></color> Skipping null event at index {_currentEventIndex}.");
                continue; // Skip to the next event if this one is null
            }

            if (DebugLogging) Debug.Log($"<color=purple><b>Cinematic Timeline:</b></color> Starting Event {_currentEventIndex}: {currentEvent.Description}");
            OnEventStarted?.Invoke(currentEvent); // Notify subscribers

            // This is the core of the coroutine-based event execution:
            // The timeline yields control to the event's Play coroutine.
            // The timeline will resume ONLY after the event's Play coroutine finishes.
            yield return currentEvent.Play(this);

            if (DebugLogging) Debug.Log($"<color=purple><b>Cinematic Timeline:</b></color> Completed Event {_currentEventIndex}: {currentEvent.Description}");
            OnEventCompleted?.Invoke(currentEvent); // Notify subscribers
        }

        // All events have been processed
        _isPlaying = false;
        _timelineCoroutine = null; // Clear the coroutine reference
        if (DebugLogging) Debug.Log("<color=green><b>Cinematic Timeline:</b></color> Timeline Completed!");
        OnTimelineCompleted?.Invoke(); // Notify subscribers that the entire timeline has finished
    }

    /// <summary>
    /// --- EXAMPLE USAGE: How to create and populate a timeline programmatically ---
    /// This method can be called from the Editor (Context Menu) or another script
    /// to demonstrate how to build a timeline.
    /// In a real project, you'd typically create ScriptableObject assets in the Project window
    /// and drag them into the `timelineEvents` list in the Inspector.
    /// </summary>
    [ContextMenu("Example: Create Sample Timeline Programmatically")]
    public void CreateSampleTimeline()
    {
        Debug.Log("<color=yellow><b>Cinematic Timeline:</b></color> Creating Sample Timeline...");
        // Clear existing events for a fresh start
        timelineEvents.Clear();

        // --- Get References (these would typically be assigned in Inspector or found dynamically) ---
        // For demonstration, let's try to find common scene objects.
        // For a robust system, you might have specific slots for these in your events
        // or use dependency injection.

        // Find the Main Camera's Transform
        Transform mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (mainCameraTransform == null)
        {
            Debug.LogWarning("Main Camera not found. CameraMoveEvents will be skipped.");
        }

        // Find an Animator component in the scene (e.g., on a Player character)
        // This is a simple find; in a real project, you'd have a specific reference.
        Animator playerAnimator = FindObjectOfType<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogWarning("No Animator component found in the scene. SimpleAnimationEvents will be skipped.");
        }


        // --- Create and add events to the timeline ---

        // 1. Log a starting message
        DebugLogEvent logStart = ScriptableObject.CreateInstance<DebugLogEvent>();
        logStart.Description = "Intro Message";
        logStart.Message = "Welcome to the Cinematic Timeline System Demo!";
        logStart.Duration = 2f;
        timelineEvents.Add(logStart);

        // 2. First Camera Move Event
        if (mainCameraTransform != null)
        {
            CameraMoveEvent moveCamera1 = ScriptableObject.CreateInstance<CameraMoveEvent>();
            moveCamera1.Description = "Camera Fly-by to (5,10,-15)";
            moveCamera1.TargetTransform = mainCameraTransform;
            moveCamera1.TargetPosition = new Vector3(5, 10, -15);
            moveCamera1.TargetRotation = new Vector3(30, 45, 0); // Look somewhat towards the origin
            moveCamera1.Duration = 4f;
            timelineEvents.Add(moveCamera1);
        }
        else
        {
            DebugLogEvent logSkipCamera1 = ScriptableObject.CreateInstance<DebugLogEvent>();
            logSkipCamera1.Description = "Skipped Camera Move 1";
            logSkipCamera1.Message = "Main Camera not found, skipping CameraMoveEvent.";
            logSkipCamera1.Duration = 1f;
            timelineEvents.Add(logSkipCamera1);
        }

        // 3. Play Animation Event (if an animator is found)
        if (playerAnimator != null)
        {
            SimpleAnimationEvent animEvent = ScriptableObject.CreateInstance<SimpleAnimationEvent>();
            animEvent.Description = "Player Animation: Wave";
            animEvent.TargetAnimator = playerAnimator;
            animEvent.AnimationStateName = "Wave"; // IMPORTANT: Replace "Wave" with an actual state name from YOUR Animator Controller
            animEvent.Duration = 2.5f; // Fallback duration if animation state not found or WaitForAnimationCompletion is false
            animEvent.WaitForAnimationCompletion = true;
            timelineEvents.Add(animEvent);
        }
        else
        {
            DebugLogEvent logSkipAnim = ScriptableObject.CreateInstance<DebugLogEvent>();
            logSkipAnim.Description = "Skipped Animation";
            logSkipAnim.Message = "No Animator found, skipping SimpleAnimationEvent.";
            logSkipAnim.Duration = 1f;
            timelineEvents.Add(logSkipAnim);
        }

        // 4. Second Camera Move Event
        if (mainCameraTransform != null)
        {
            CameraMoveEvent moveCamera2 = ScriptableObject.CreateInstance<CameraMoveEvent>();
            moveCamera2.Description = "Camera Pan to (-10,5,-5)";
            moveCamera2.TargetTransform = mainCameraTransform;
            moveCamera2.TargetPosition = new Vector3(-10, 5, -5);
            moveCamera2.TargetRotation = new Vector3(15, -90, 0); // Look towards the right
            moveCamera2.Duration = 3.5f;
            timelineEvents.Add(moveCamera2);
        }
        else
        {
            DebugLogEvent logSkipCamera2 = ScriptableObject.CreateInstance<DebugLogEvent>();
            logSkipCamera2.Description = "Skipped Camera Move 2";
            logSkipCamera2.Message = "Main Camera not found, skipping CameraMoveEvent.";
            logSkipCamera2.Duration = 1f;
            timelineEvents.Add(logSkipCamera2);
        }

        // 5. Log a concluding message
        DebugLogEvent logEnd = ScriptableObject.CreateInstance<DebugLogEvent>();
        logEnd.Description = "Cinematic End";
        logEnd.Message = "The cinematic sequence has concluded. Enjoy the game!";
        logEnd.Duration = 3f;
        timelineEvents.Add(logEnd);

        Debug.Log("<color=yellow><b>Cinematic Timeline:</b></color> Sample Timeline created! " +
                  "You can now run the scene (if PlayOnAwake is true) or " +
                  "manually click 'Play Timeline' in the Inspector. " +
                  "Ensure your Main Camera and any Animator for the player are correctly set up if you want to see those events.");
    }
}
```

---

### How to Use This in Unity:

1.  **Create the Script:**
    *   Save the entire code block above as `CinematicTimeline.cs` in your Unity project's Assets folder (e.g., `Assets/Scripts/CinematicTimeline.cs`).

2.  **Create a Timeline GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., `GameObject -> Create Empty`).
    *   Rename it to `Cinematic Timeline Controller`.
    *   Attach the `CinematicTimeline.cs` script to this GameObject.

3.  **Populate the Timeline (Two Ways):**

    *   **A) Using the Inspector (Recommended for typical use):**
        *   In your Project window, right-click -> `Create -> Cinematic Timeline -> Events`. You'll see `Log Message Event`, `Camera Move Event`, `Play Animation Event`.
        *   Create several instances of these `ScriptableObject` assets (e.g., `MyIntroLog`, `MyCameraShot1`, `MyPlayerWave`, `MyCameraShot2`, `MyOutroLog`).
        *   **Configure each event asset:** Select each created asset and set its `Duration`, `Description`, and specific parameters (e.g., `Message` for `DebugLogEvent`, `TargetTransform`, `TargetPosition`, `TargetRotation` for `CameraMoveEvent`, `TargetAnimator`, `AnimationStateName` for `SimpleAnimationEvent`).
            *   For `CameraMoveEvent`, drag your `Main Camera`'s Transform into the `Target Transform` slot.
            *   For `SimpleAnimationEvent`, drag an `Animator` component (e.g., from your Player character) into the `Target Animator` slot and specify an `Animation State Name` that exists in your Animator Controller (e.g., "Idle", "Walk", "Wave").
        *   On your `Cinematic Timeline Controller` GameObject, select it. In the Inspector, you'll see the `Timeline Events` list. Drag your configured `TimelineEvent` assets from the Project window into this list in the desired order.

    *   **B) Using the `CreateSampleTimeline()` Context Menu (for quick testing/learning):**
        *   Select the `Cinematic Timeline Controller` GameObject in the scene.
        *   In the Inspector, right-click on the `CinematicTimeline` component header.
        *   Select `Example: Create Sample Timeline Programmatically`.
        *   This will automatically populate the `Timeline Events` list with example events. **You will still need to manually assign the `Main Camera`'s Transform to the `Target Transform` field in the `CameraMoveEvent` assets and an `Animator` component to the `SimpleAnimationEvent` assets if they aren't found by the script.**

4.  **Run the Scene:**
    *   If `Play On Awake` is checked on the `CinematicTimeline` component, the timeline will start automatically.
    *   Otherwise, click the `Play Timeline` button in the Inspector during runtime to start it.
    *   Observe the Console for logs from `DebugLogEvent` and the camera movement/animation playing out.

---

This complete example provides a robust and educational foundation for understanding and implementing the CinematicTimelineSystem pattern in your Unity projects.