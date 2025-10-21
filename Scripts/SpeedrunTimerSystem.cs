// Unity Design Pattern Example: SpeedrunTimerSystem
// This script demonstrates the SpeedrunTimerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical C# Unity implementation of a 'SpeedrunTimerSystem'. It leverages common Unity patterns like Singletons and event-driven architecture to create a flexible and usable timer system.

**Key Design Pattern Concepts Demonstrated:**

1.  **Singleton:** Ensures a single, globally accessible instance of the `SpeedrunTimerSystem` throughout the application.
2.  **Event-Driven Architecture:** Uses C# events (`Action` delegates) to notify other components (like UI displays or game logic) about timer state changes and recorded splits. This promotes loose coupling.
3.  **Data Encapsulation:** Timer state (`_isRunning`, `_isPaused`, `_elapsedTime`) is managed internally, exposed via read-only properties.
4.  **Clear API:** Provides distinct methods for common timer operations (`StartTimer`, `StopTimer`, `PauseTimer`, `ResumeTimer`, `ResetTimer`, `RecordSplit`).

---

### Setup in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `SpeedrunTimerSystem.cs` and paste the first code block into it.
    *   Create another C# script named `TimerUIController.cs` and paste the second code block into it.

2.  **Install TextMeshPro (if not already):**
    *   Go to `Window > TextMeshPro > Import TMP Essential Resources`.

3.  **Create UI Elements:**
    *   Right-click in the Hierarchy: `UI > Canvas`.
    *   On the Canvas, right-click: `UI > Text - TextMeshPro`. Rename this to `MainTimerText`.
    *   Create another `Text - TextMeshPro` on the Canvas. Rename this to `SplitsText`.
    *   Adjust their positions and sizes on the Canvas so they don't overlap. You might want `MainTimerText` at the top and `SplitsText` below it, or off to the side.

4.  **Create a Game Object for the System:**
    *   Create an empty GameObject in your scene (e.g., named `_GameManagers`).
    *   Drag and drop the `SpeedrunTimerSystem.cs` script onto this new GameObject.

5.  **Create a Game Object for the UI Controller:**
    *   Create another empty GameObject (e.g., named `_UI`).
    *   Drag and drop the `TimerUIController.cs` script onto this GameObject.
    *   In the Inspector for `_UI`, drag your `MainTimerText` UI element into the `Main Timer Text` slot.
    *   Drag your `SplitsText` UI element into the `Splits Text` slot.

6.  **Add Input Listener (Optional but Recommended for Demo):**
    *   Create a new C# script called `SpeedrunInputDemo.cs`.
    *   Paste the third code block into it.
    *   Attach `SpeedrunInputDemo.cs` to *any* GameObject in your scene (e.g., the `_GameManagers` GameObject).

7.  **Run the Scene:**
    *   Press Play.
    *   Press `Space` to start/stop.
    *   Press `S` to record a split.
    *   Press `P` to pause/resume.
    *   Press `R` to reset.
    *   Observe the timer and splits updating on your UI.

---

### 1. `SpeedrunTimerSystem.cs`

This is the core script that manages the timer logic.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text; // For building split strings efficiently

/// <summary>
/// Represents a single recorded split (segment) in the speedrun.
/// </summary>
[System.Serializable] // Allows it to be visible in the Inspector for debugging/serialization
public struct SplitData
{
    public string Name;         // Name of the split (e.g., "Level 1 End", "Boss Defeated")
    public float AbsoluteTime;  // The total elapsed time when this split was recorded
    public float SegmentTime;   // The time elapsed since the *previous* split or the timer start

    public SplitData(string name, float absoluteTime, float segmentTime)
    {
        Name = name;
        AbsoluteTime = absoluteTime;
        SegmentTime = segmentTime;
    }

    public override string ToString()
    {
        return $"{Name}: {FormatTime(AbsoluteTime)} (Segment: {FormatTime(SegmentTime)})";
    }

    // Helper to format time for display
    private static string FormatTime(float timeInSeconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(timeInSeconds);
        return string.Format("{0:00}:{1:00}.{2:000}", t.Minutes, t.Seconds, t.Milliseconds);
    }
}

/// <summary>
/// The central Speedrun Timer System, implemented as a Singleton.
/// Manages starting, stopping, pausing, resetting, and recording splits for a speedrun.
/// Uses events to notify other components of timer state changes and recorded splits.
/// </summary>
public class SpeedrunTimerSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static SpeedrunTimerSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SpeedrunTimerSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the timer running across scenes
            Debug.Log("SpeedrunTimerSystem initialized.");
        }
    }

    // --- Timer State Variables ---
    private float _startTime;           // The time.realtimeSinceStartup when the timer started
    private float _elapsedTime;         // The current elapsed time since the timer started (minus pauses)
    private float _timeBeforePause;     // Stores _elapsedTime when pausing to resume accurately
    private bool _isRunning = false;    // Is the timer actively counting up?
    private bool _isPaused = false;     // Is the timer paused? (Can be running but paused, or stopped and paused)

    // --- Split Data ---
    private List<SplitData> _recordedSplits = new List<SplitData>();
    private float _lastSplitAbsoluteTime; // To calculate segment time

    // --- Public Properties (Read-Only Access) ---
    public float CurrentTime => _elapsedTime;
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public IReadOnlyList<SplitData> RecordedSplits => _recordedSplits.AsReadOnly();

    // --- Events for loose coupling ---
    // Other components (e.g., UI, game logic) can subscribe to these events.
    public static event Action OnTimerStarted;
    public static event Action<float> OnTimerStopped; // Passes final time
    public static event Action OnTimerPaused;
    public static event Action OnTimerResumed;
    public static event Action OnTimerReset;
    public static event Action<SplitData> OnSplitRecorded; // Passes the recorded split data

    // --- Unity Lifecycle ---
    private void Update()
    {
        if (_isRunning && !_isPaused)
        {
            _elapsedTime = Time.realtimeSinceStartup - _startTime + _timeBeforePause;
        }
    }

    // --- Timer Control Methods ---

    /// <summary>
    /// Starts or resumes the timer. If already running, does nothing.
    /// If paused, resumes. If stopped, starts fresh.
    /// </summary>
    public void StartTimer()
    {
        if (_isRunning && !_isPaused)
        {
            Debug.LogWarning("Timer is already running.");
            return;
        }

        if (_isPaused)
        {
            ResumeTimer(); // If paused, just resume
            return;
        }

        Debug.Log("Timer Started.");
        _startTime = Time.realtimeSinceStartup;
        _elapsedTime = 0f;
        _timeBeforePause = 0f;
        _isRunning = true;
        _isPaused = false;
        _recordedSplits.Clear();
        _lastSplitAbsoluteTime = 0f;

        OnTimerStarted?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Stops the timer. Records the final time.
    /// </summary>
    public void StopTimer()
    {
        if (!_isRunning)
        {
            Debug.LogWarning("Timer is not running, cannot stop.");
            return;
        }

        Debug.Log($"Timer Stopped. Final Time: {FormatTime(_elapsedTime)}");
        _isRunning = false;
        _isPaused = false; // Ensure it's not left in a paused state if stopped while paused

        OnTimerStopped?.Invoke(_elapsedTime); // Notify subscribers with the final time
    }

    /// <summary>
    /// Pauses the timer. Time stops counting but the run state is maintained.
    /// </summary>
    public void PauseTimer()
    {
        if (!_isRunning || _isPaused)
        {
            Debug.LogWarning("Timer is not running or already paused, cannot pause.");
            return;
        }

        Debug.Log($"Timer Paused at {FormatTime(_elapsedTime)}");
        _isPaused = true;
        _timeBeforePause = _elapsedTime; // Store current elapsed time
        _startTime = Time.realtimeSinceStartup; // Reset start time for accurate resume calculation

        OnTimerPaused?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Resumes the timer from a paused state.
    /// </summary>
    public void ResumeTimer()
    {
        if (!_isRunning || !_isPaused)
        {
            Debug.LogWarning("Timer is not paused, cannot resume.");
            return;
        }

        Debug.Log($"Timer Resumed from {FormatTime(_elapsedTime)}");
        _isPaused = false;
        _startTime = Time.realtimeSinceStartup; // Adjust startTime to account for the pause duration

        OnTimerResumed?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Resets the timer to its initial state (0, not running, not paused).
    /// Clears all recorded splits.
    /// </summary>
    public void ResetTimer()
    {
        Debug.Log("Timer Reset.");
        _startTime = 0f;
        _elapsedTime = 0f;
        _timeBeforePause = 0f;
        _isRunning = false;
        _isPaused = false;
        _recordedSplits.Clear();
        _lastSplitAbsoluteTime = 0f;

        OnTimerReset?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Records a new split (segment) time.
    /// </summary>
    /// <param name="splitName">A descriptive name for the split.</param>
    public void RecordSplit(string splitName)
    {
        if (!_isRunning || _isPaused)
        {
            Debug.LogWarning("Timer is not running or is paused, cannot record split.");
            return;
        }

        float currentAbsoluteTime = _elapsedTime;
        float segmentTime = currentAbsoluteTime - _lastSplitAbsoluteTime;

        SplitData newSplit = new SplitData(splitName, currentAbsoluteTime, segmentTime);
        _recordedSplits.Add(newSplit);
        _lastSplitAbsoluteTime = currentAbsoluteTime;

        Debug.Log($"Split Recorded: {newSplit}");
        OnSplitRecorded?.Invoke(newSplit); // Notify subscribers with the new split data
    }

    // --- Helper for Time Formatting ---
    public static string FormatTime(float timeInSeconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(timeInSeconds);
        // Format as MM:SS.mmm
        return string.Format("{0:00}:{1:00}.{2:000}", t.Minutes, t.Seconds, t.Milliseconds);
    }

    // --- Optional: Save/Load functionality (concept only) ---
    // public void SaveBestTimes() { /* Implement using PlayerPrefs or custom serialization */ }
    // public void LoadBestTimes() { /* Implement using PlayerPrefs or custom serialization */ }
}
```

---

### 2. `TimerUIController.cs`

This script listens to events from `SpeedrunTimerSystem` and updates TextMeshPro UI elements.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Text; // For efficient string building

/// <summary>
/// Manages the display of the speedrun timer and recorded splits in the UI.
/// Subscribes to events from the SpeedrunTimerSystem to update itself.
/// </summary>
public class TimerUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI mainTimerText;
    [SerializeField] private TextMeshProUGUI splitsText;

    private StringBuilder _splitsStringBuilder = new StringBuilder();

    // --- Unity Lifecycle ---
    private void OnEnable()
    {
        // Subscribe to timer events
        SpeedrunTimerSystem.OnTimerStarted += HandleTimerStarted;
        SpeedrunTimerSystem.OnTimerStopped += HandleTimerStopped;
        SpeedrunTimerSystem.OnTimerPaused += HandleTimerPaused;
        SpeedrunTimerSystem.OnTimerResumed += HandleTimerResumed;
        SpeedrunTimerSystem.OnTimerReset += HandleTimerReset;
        SpeedrunTimerSystem.OnSplitRecorded += HandleSplitRecorded;
    }

    private void OnDisable()
    {
        // Unsubscribe from timer events to prevent memory leaks and errors
        SpeedrunTimerSystem.OnTimerStarted -= HandleTimerStarted;
        SpeedrunTimerSystem.OnTimerStopped -= HandleTimerStopped;
        SpeedrunTimerSystem.OnTimerPaused -= HandleTimerPaused;
        SpeedrunTimerSystem.OnTimerResumed -= HandleTimerResumed;
        SpeedrunTimerSystem.OnTimerReset -= HandleTimerReset;
        SpeedrunTimerSystem.OnSplitRecorded -= HandleSplitRecorded;
    }

    private void Start()
    {
        // Initialize UI display when the component starts
        UpdateMainTimerDisplay(0f);
        UpdateSplitsDisplay();
    }

    private void Update()
    {
        // Continuously update the main timer display if the system is running
        if (SpeedrunTimerSystem.Instance != null && SpeedrunTimerSystem.Instance.IsRunning && !SpeedrunTimerSystem.Instance.IsPaused)
        {
            UpdateMainTimerDisplay(SpeedrunTimerSystem.Instance.CurrentTime);
        }
    }

    // --- Event Handlers ---

    private void HandleTimerStarted()
    {
        Debug.Log("UI: Timer Started, refreshing display.");
        _splitsStringBuilder.Clear(); // Clear previous splits
        UpdateMainTimerDisplay(0f);
        UpdateSplitsDisplay();
        mainTimerText.color = Color.white; // Reset color
    }

    private void HandleTimerStopped(float finalTime)
    {
        Debug.Log($"UI: Timer Stopped at {SpeedrunTimerSystem.FormatTime(finalTime)}");
        UpdateMainTimerDisplay(finalTime);
        mainTimerText.color = Color.yellow; // Indicate stop
    }

    private void HandleTimerPaused()
    {
        Debug.Log("UI: Timer Paused.");
        mainTimerText.color = Color.red; // Indicate pause
    }

    private void HandleTimerResumed()
    {
        Debug.Log("UI: Timer Resumed.");
        mainTimerText.color = Color.white; // Reset color
    }

    private void HandleTimerReset()
    {
        Debug.Log("UI: Timer Reset, clearing display.");
        _splitsStringBuilder.Clear(); // Clear splits
        UpdateMainTimerDisplay(0f);
        UpdateSplitsDisplay();
        mainTimerText.color = Color.white; // Reset color
    }

    private void HandleSplitRecorded(SplitData split)
    {
        Debug.Log($"UI: Split Recorded: {split.Name}");
        _splitsStringBuilder.AppendLine(split.ToString()); // Add new split to the display string
        UpdateSplitsDisplay();
    }

    // --- UI Update Methods ---

    private void UpdateMainTimerDisplay(float time)
    {
        if (mainTimerText != null)
        {
            mainTimerText.text = SpeedrunTimerSystem.FormatTime(time);
        }
    }

    private void UpdateSplitsDisplay()
    {
        if (splitsText != null)
        {
            splitsText.text = _splitsStringBuilder.ToString();
        }
    }
}
```

---

### 3. `SpeedrunInputDemo.cs` (Example Usage)

This script demonstrates how game logic or a player controller would interact with the `SpeedrunTimerSystem`.

```csharp
using UnityEngine;

/// <summary>
/// Demonstrates how other scripts can interact with the SpeedrunTimerSystem.
/// This acts as a simple input listener to control the timer.
/// </summary>
public class SpeedrunInputDemo : MonoBehaviour
{
    private int _splitCounter = 0; // To give unique names to splits

    void Update()
    {
        // Ensure the timer system is initialized
        if (SpeedrunTimerSystem.Instance == null)
        {
            Debug.LogWarning("SpeedrunTimerSystem.Instance is null. Make sure it's in the scene and initialized.");
            return;
        }

        // --- Start/Stop Timer (Toggle) ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (SpeedrunTimerSystem.Instance.IsRunning)
            {
                SpeedrunTimerSystem.Instance.StopTimer();
            }
            else
            {
                SpeedrunTimerSystem.Instance.StartTimer();
                _splitCounter = 0; // Reset split counter on new run
            }
        }

        // --- Record Split ---
        if (Input.GetKeyDown(KeyCode.S))
        {
            _splitCounter++;
            SpeedrunTimerSystem.Instance.RecordSplit($"Split {_splitCounter}");
        }

        // --- Pause/Resume Timer (Toggle) ---
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (SpeedrunTimerSystem.Instance.IsPaused)
            {
                SpeedrunTimerSystem.Instance.ResumeTimer();
            }
            else
            {
                SpeedrunTimerSystem.Instance.PauseTimer();
            }
        }

        // --- Reset Timer ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            SpeedrunTimerSystem.Instance.ResetTimer();
        }

        // Example of a game event triggering a split
        // This would typically be in a LevelManager, PlayerController, or Objective script
        // For instance, when a player crosses a finish line:
        // if (playerCrossedFinishLine)
        // {
        //     SpeedrunTimerSystem.Instance.RecordSplit("Finish Line");
        //     SpeedrunTimerSystem.Instance.StopTimer();
        // }

        // Or on level load:
        // void OnLevelLoaded(string levelName)
        // {
        //     SpeedrunTimerSystem.Instance.RecordSplit($"Level {levelName} Loaded");
        // }
    }
}
```