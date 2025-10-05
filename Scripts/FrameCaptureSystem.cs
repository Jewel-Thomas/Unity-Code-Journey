// Unity Design Pattern Example: FrameCaptureSystem
// This script demonstrates the FrameCaptureSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'FrameCaptureSystem' design pattern in Unity focuses on centralizing and managing actions that need to be executed precisely on specific frames or after a certain frame delay. This pattern decouples the "what" (the action), the "when" (the frame number), and the "who" (the object triggering the action), making frame-dependent task management robust, readable, and easier to maintain.

It's particularly useful for:
*   **Precise Timing:** Executing code exactly when `Time.frameCount` reaches a certain value.
*   **Deferred Execution:** Scheduling an action to run after a set number of frames, similar to `Invoke` but frame-based and managed centrally.
*   **Screenshot/Capture Systems:** Triggering screen captures or data logging at specific moments in a game sequence.
*   **Replay Systems:** Capturing game state at regular frame intervals.
*   **Animation Synchronization:** Ensuring certain events happen precisely in sync with animations.
*   **Performance Optimization:** Spreading heavy computations across multiple frames by scheduling chunks of work.

### `FrameCaptureSystem.cs`

This script provides a complete implementation of the `FrameCaptureSystem` as a Unity singleton. It manages a `SortedDictionary` of actions, processing them efficiently in its `Update` method.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .Sum() in GetScheduledActionCount()

/// <summary>
/// The FrameCaptureSystem is a centralized manager that allows scheduling and executing
/// actions (delegates) to run on specific future frames or after a certain frame delay.
///
/// Design Pattern: This system embodies a variation of the Command pattern combined with
/// a Scheduler pattern. It centralizes frame-dependent tasks, decoupling the "when"
/// (the frame it executes) from the "what" (the action itself) and the "who" (the object
/// that triggered it). This promotes a clean separation of concerns and makes frame-precise
/// task management robust and easy to reason about.
///
/// Real-World Use Cases:
/// - **Screenshot Capturing:** Taking screenshots after a specific event (e.g., 5 frames after a UI button press).
/// - **Timed Events:** Logging debug information periodically or after a short delay for frame-perfect analysis.
/// - **Game State Synchronization:** Triggering game state changes or animations on precise frame numbers.
/// - **Slow-Motion Effects:** Implementing effects where certain events need frame-accurate timing during altered time scales.
/// - **Performance Batching:** Batching or deferring computationally expensive operations to spread them across frames.
/// - **Replay Systems:** Creating replay functionality by capturing game state at specific frame intervals.
/// </summary>
public class FrameCaptureSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    // This ensures there is only one instance of the FrameCaptureSystem throughout the application.
    private static FrameCaptureSystem _instance;
    public static FrameCaptureSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene first.
                _instance = FindObjectOfType<FrameCaptureSystem>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the component to it.
                    GameObject singletonObject = new GameObject(typeof(FrameCaptureSystem).Name);
                    _instance = singletonObject.AddComponent<FrameCaptureSystem>();
                }

                // Ensure the singleton persists across scene loads.
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // --- Data Structure for Scheduled Actions ---
    // A SortedDictionary is used to efficiently store and retrieve actions.
    // The key is the target frame number, and the value is a list of actions
    // to be executed on that specific frame.
    // SortedDictionary automatically keeps keys (frame numbers) in ascending order,
    // which is ideal for processing actions chronologically in the Update loop.
    private SortedDictionary<int, List<Action>> _scheduledActions;

    // --- Initialization ---
    private void Awake()
    {
        // Implement robust singleton pattern to handle potential duplicates.
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[FrameCaptureSystem] Destroying duplicate instance on '{gameObject.name}'.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Ensure this instance persists.

        _scheduledActions = new SortedDictionary<int, List<Action>>();
        Debug.Log("[FrameCaptureSystem] Initialized and ready to capture frames.");
    }

    // --- Main Frame Processing Loop ---
    private void Update()
    {
        // Get the current frame count from Unity. This is our reference point for execution.
        int currentFrame = Time.frameCount;

        // We need a temporary list to collect frame numbers whose actions have been executed.
        // This is because we cannot modify the dictionary (_scheduledActions) while iterating over it.
        List<int> framesToRemove = new List<int>();

        // Iterate through the scheduled actions. Since _scheduledActions is a SortedDictionary,
        // it automatically processes actions in chronological order of their target frame numbers.
        foreach (var entry in _scheduledActions)
        {
            int targetFrame = entry.Key;
            List<Action> actionsToExecute = entry.Value;

            // If the target frame for these actions is less than or equal to the current frame,
            // it means they are due to be executed now.
            if (targetFrame <= currentFrame)
            {
                foreach (Action action in actionsToExecute)
                {
                    try
                    {
                        // Execute the scheduled action. The '?' handles cases where an action might be null.
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Log any exceptions that occur during action execution. This prevents one
                        // faulty action from crashing the entire FrameCaptureSystem and aids debugging.
                        Debug.LogError($"[FrameCaptureSystem] Error executing scheduled action on frame {targetFrame}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                framesToRemove.Add(targetFrame); // Mark this frame's actions for removal.
            }
            else
            {
                // Optimization: Since the dictionary is sorted by frame number, if we encounter a targetFrame
                // that is in the future, all subsequent entries will also be in the future.
                // We can safely break the loop early for efficiency.
                break;
            }
        }

        // After iterating, remove all actions that have been executed from the dictionary.
        foreach (int frame in framesToRemove)
        {
            _scheduledActions.Remove(frame);
        }
    }

    // --- Public API for Scheduling Actions ---

    /// <summary>
    /// Schedules an action to be executed after a specified number of frames from the current frame.
    /// This is useful for "wait N frames then do X".
    /// </summary>
    /// <param name="action">The delegate (method or lambda) to execute.</param>
    /// <param name="framesDelay">The number of frames to wait before executing the action. Must be non-negative.
    /// A delay of 0 means it will execute on the current frame or the very next Update cycle.</param>
    public void ScheduleAction(Action action, int framesDelay)
    {
        if (action == null)
        {
            Debug.LogWarning("[FrameCaptureSystem] Cannot schedule a null action.");
            return;
        }
        if (framesDelay < 0)
        {
            Debug.LogWarning($"[FrameCaptureSystem] framesDelay must be non-negative. Correcting '{framesDelay}' to 0.");
            framesDelay = 0;
        }

        // Calculate the absolute target frame number based on the current frame and the desired delay.
        int targetFrame = Time.frameCount + framesDelay;
        AddActionToSchedule(targetFrame, action);
    }

    /// <summary>
    /// Schedules an action to be executed precisely on a given absolute frame number.
    /// This is useful for "do X when Time.frameCount is Y".
    /// </summary>
    /// <param name="action">The delegate (method or lambda) to execute.</param>
    /// <param name="targetFrame">The absolute frame number (Time.frameCount value) at which to execute the action.
    /// If targetFrame is less than or equal to the current frame, the action will execute on the current frame.</param>
    public void ScheduleActionAtFrame(Action action, int targetFrame)
    {
        if (action == null)
        {
            Debug.LogWarning("[FrameCaptureSystem] Cannot schedule a null action.");
            return;
        }

        // If the specified target frame is in the past, execute the action immediately on the current frame.
        if (targetFrame < Time.frameCount)
        {
            Debug.LogWarning($"[FrameCaptureSystem] Scheduled action for frame '{targetFrame}' is in the past (current frame: {Time.frameCount}). Executing immediately.");
            targetFrame = Time.frameCount;
        }

        AddActionToSchedule(targetFrame, action);
    }

    /// <summary>
    /// Internal helper method to add an action to the _scheduledActions dictionary.
    /// Manages creating new lists for a frame if one doesn't exist.
    /// </summary>
    private void AddActionToSchedule(int targetFrame, Action action)
    {
        // If there are no actions scheduled for this target frame yet, create a new list.
        if (!_scheduledActions.ContainsKey(targetFrame))
        {
            _scheduledActions.Add(targetFrame, new List<Action>());
        }
        _scheduledActions[targetFrame].Add(action); // Add the action to the list for the specified target frame.
        // Debug.Log($"[FrameCaptureSystem] Scheduled action for frame: {targetFrame}. Current frame: {Time.frameCount}");
    }

    /// <summary>
    /// Clears all currently scheduled actions. Use this if you need to stop all pending tasks.
    /// </summary>
    public void ClearAllScheduledActions()
    {
        _scheduledActions.Clear();
        Debug.Log("[FrameCaptureSystem] All scheduled actions cleared.");
    }

    /// <summary>
    /// Returns the total number of individual actions currently scheduled in the system.
    /// </summary>
    public int GetScheduledActionCount()
    {
        // Sum the counts of all action lists within the dictionary.
        return _scheduledActions.Values.Sum(list => list.Count);
    }
}

```

---

### Example Usage: `FrameCaptureExample.cs`

To use the `FrameCaptureSystem`, simply attach the `FrameCaptureSystem.cs` script to an empty GameObject in your scene (e.g., named "FrameCaptureManager"). The system will automatically manage its singleton instance and persist across scenes.

You can then call its methods from any other script, as shown below:

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// This class demonstrates various ways to use the FrameCaptureSystem.
/// Attach this script to any GameObject in your scene to see it in action.
/// Remember to have a 'FrameCaptureSystem' GameObject with the 'FrameCaptureSystem.cs' script attached.
/// </summary>
public class FrameCaptureExample : MonoBehaviour
{
    private int _periodicLogCounter = 0;

    void Start()
    {
        Debug.Log("--- FrameCaptureExample Start ---");

        // --- Example 1: Schedule an action after a delay of frames ---
        // This will log a message 5 frames after Start() is called.
        FrameCaptureSystem.Instance.ScheduleAction(() =>
        {
            Debug.Log($"[Example 1] Scheduled action (delay 5 frames) executed on frame: {Time.frameCount}");
        }, 5);

        // --- Example 2: Schedule multiple actions on the same target frame ---
        // Both messages will appear on the same frame, 10 frames from now.
        int targetFrameForExample2 = Time.frameCount + 10;
        FrameCaptureSystem.Instance.ScheduleActionAtFrame(() =>
        {
            Debug.Log($"[Example 2-A] Action A (at frame {targetFrameForExample2}) executed on frame: {Time.frameCount}");
        }, targetFrameForExample2);

        FrameCaptureSystem.Instance.ScheduleActionAtFrame(() =>
        {
            Debug.Log($"[Example 2-B] Action B (at frame {targetFrameForExample2}) executed on frame: {Time.frameCount}");
        }, targetFrameForExample2);

        // --- Example 3: Schedule a repeating action (requires rescheduling) ---
        // This action will schedule itself again after execution, demonstrating a loop
        // for tasks that need to run repeatedly with a frame-accurate interval.
        ScheduleRepeatingAction(20); // First execution 20 frames from now, then every 20 frames.

        // --- Example 4: Taking a "screenshot" (simulated) 3 frames after a button press ---
        // This simulates a game event (e.g., a player pressing a "photo mode" button)
        // and schedules a frame-accurate "screenshot" capture.
        Invoke(nameof(SimulateEventTrigger), 2f); // Simulate a button press after 2 seconds
    }

    void Update()
    {
        // --- Example 5: Schedule a periodic action using a modulo check ---
        // This demonstrates how to use the system for tasks that should run, for instance,
        // every 60 frames, but using the FCS for processing.
        if (Time.frameCount % 60 == 0 && Time.frameCount > 0)
        {
            _periodicLogCounter++;
            // Scheduling with a 0-frame delay ensures it's processed by the FCS
            // within the current frame's Update cycle, but after other things.
            FrameCaptureSystem.Instance.ScheduleAction(() =>
            {
                Debug.Log($"[Example 5] Periodic log ({_periodicLogCounter}) executed on frame: {Time.frameCount}");
            }, 0);
        }
    }

    /// <summary>
    /// Recursive method to schedule an action that repeats itself.
    /// </summary>
    /// <param name="delayFrames">The delay in frames before the action executes again.</param>
    void ScheduleRepeatingAction(int delayFrames)
    {
        FrameCaptureSystem.Instance.ScheduleAction(() =>
        {
            Debug.Log($"[Example 3] Repeating action executed on frame: {Time.frameCount}. Rescheduling for +{delayFrames} frames.");
            // Reschedule itself for another 'delayFrames' frames later.
            ScheduleRepeatingAction(delayFrames);
        }, delayFrames);
    }

    /// <summary>
    /// Simulates a game event (like a button press) that triggers a frame-captured action.
    /// </summary>
    void SimulateEventTrigger()
    {
        Debug.Log($"[Example 4] SIMULATED EVENT TRIGGERED on frame: {Time.frameCount}. Scheduling 'screenshot' capture.");
        FrameCaptureSystem.Instance.ScheduleAction(() =>
        {
            PerformScreenshotCapture(); // This method would encapsulate actual screenshot logic.
        }, 3); // Take "screenshot" 3 frames from now to capture the moment accurately.
    }

    /// <summary>
    /// Placeholder for actual screenshot capture logic.
    /// </summary>
    void PerformScreenshotCapture()
    {
        // In a real Unity project, this would involve:
        // - Reading from a RenderTexture (e.g., Camera.targetTexture).
        // - Using ScreenCapture.CaptureScreenshotAsTexture() or ScreenCapture.CaptureScreenshot().
        // For this example, we'll just log a message to indicate the capture.
        Debug.Log($"[Example 4] --- 'Screenshot' captured on frame: {Time.frameCount} ---");
        // Example: ScreenCapture.CaptureScreenshot("MyGameMoment_" + Time.frameCount + ".png");
    }

    void OnDestroy()
    {
        // It's good practice to clear scheduled actions if your MonoBehaviour is destroyed
        // and its actions should no longer execute, especially if they are specific to this object.
        // If the FrameCaptureSystem is configured to persist across scenes, actions scheduled by a
        // destroyed object might still run unless explicitly cleared.
        // FrameCaptureSystem.Instance?.ClearAllScheduledActions(); // Uncomment if you want THIS OBJECT's actions to be cleared on destroy.
                                                                  // (Note: ClearAllScheduledActions clears ALL, not just this object's).
                                                                  // A more advanced system might offer a way to cancel specific actions by ID.
    }
}
```