// Unity Design Pattern Example: GameClockSystem
// This script demonstrates the GameClockSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The GameClockSystem design pattern centralizes all time-related logic in a game. Instead of every object managing its own `Time.deltaTime` for game-specific timing (like day/night cycles, plant growth, quest timers), a single, authoritative GameClockSystem keeps track of the game's internal time (minutes, hours, days, seasons, etc.) and notifies other objects when significant time events occur.

This pattern offers several benefits:
*   **Decoupling:** Game objects don't need to know *how* time progresses; they just react to events from the clock.
*   **Control:** Easily pause, resume, speed up, slow down, or jump time for various game mechanics (e.g., fast-travel, sleeping, time-lapse effects).
*   **Consistency:** Ensures all time-dependent systems are perfectly synchronized to the same game clock.
*   **Maintainability:** Changes to how time is calculated are localized to one system.

## 1. GameClockSystem.cs Script

This script provides a complete and practical implementation of the GameClockSystem pattern. It's a `MonoBehaviour` singleton that updates the game's internal time and broadcasts events when minutes, hours, or days pass.

```csharp
// GameClockSystem.cs
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// The GameClockSystem design pattern centralizes all time-related logic in a game.
/// Instead of every object managing its own Time.deltaTime for game-specific timing,
/// a single, authoritative GameClockSystem keeps track of the game's internal time
/// (minutes, hours, days, etc.) and notifies other objects when significant time events occur.
///
/// This implementation provides a singleton MonoBehaviour that can be placed in a scene
/// and configured via the Inspector. It offers public events for other scripts to subscribe to
/// and methods to control time progression (pause, resume, advance time, set time).
/// </summary>
public class GameClockSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    /// <summary>
    /// Static instance of the GameClockSystem, ensuring there's only one throughout the game.
    /// Other scripts can access it via GameClockSystem.Instance.
    /// </summary>
    public static GameClockSystem Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Game Time Settings")]
    [Tooltip("How many real-world seconds equal one game minute. A value of 1.0 means 1 real second = 1 game minute.")]
    [SerializeField] private float _realSecondsPerGameMinute = 1.0f;

    [Tooltip("The starting hour of the game clock (0-23).")]
    [SerializeField] private int _startHour = 6; // E.g., 6 AM
    [Tooltip("The starting minute of the game clock (0-59).")]
    [SerializeField] private int _startMinute = 0;
    [Tooltip("The starting day of the game clock (1 or more).")]
    [SerializeField] private int _startDay = 1;

    [Tooltip("If checked, the clock will start in a paused state.")]
    [SerializeField] private bool _startPaused = false;

    // --- Internal Game Time State ---
    private float _elapsedRealTime = 0f; // Accumulates real-world Time.deltaTime
    private int _currentMinute;
    private int _currentHour;
    private int _currentDay;
    private bool _isPaused;

    // --- Public Properties to Access Current Time ---
    /// <summary>
    /// Gets the current minute of the game clock (0-59).
    /// </summary>
    public int CurrentMinute => _currentMinute;
    /// <summary>
    /// Gets the current hour of the game clock (0-23).
    /// </summary>
    public int CurrentHour => _currentHour;
    /// <summary>
    /// Gets the current day of the game clock (1 or more).
    /// </summary>
    public int CurrentDay => _currentDay;
    /// <summary>
    /// Gets a value indicating whether the game clock is currently paused.
    /// </summary>
    public bool IsPaused => _isPaused;
    /// <summary>
    /// Gets the current real-world seconds per game minute setting.
    /// </summary>
    public float RealSecondsPerGameMinute => _realSecondsPerGameMinute;

    // --- Game Time Events (Actions/Delegates) ---
    // These events allow other scripts to subscribe and react to specific time changes.

    /// <summary>
    /// Event fired every time a game minute passes.
    /// </summary>
    public event Action OnMinutePassed;
    /// <summary>
    /// Event fired every time a game hour passes (i.e., minute resets to 0, hour increments).
    /// </summary>
    public event Action OnHourPassed;
    /// <summary>
    /// Event fired every time a game day passes (i.e., hour resets to 0, day increments).
    /// </summary>
    public event Action OnDayPassed;
    /// <summary>
    /// General event fired whenever any component of the game time changes (minute, hour, or day).
    /// Provides the new hour, minute, and day as parameters.
    /// </summary>
    public event Action<int, int, int> OnGameTimeChanged;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Enforce Singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GameClockSystem instances found! Destroying this one to maintain singleton.", this);
            Destroy(gameObject);
            return;
        }

        // Set this as the singleton instance.
        Instance = this;

        // Ensure the GameClockSystem persists across scene loads.
        // This is common for managers like a clock or game state.
        DontDestroyOnLoad(gameObject);

        // Initialize the clock with the inspector settings.
        InitializeClock();
    }

    /// <summary>
    /// Initializes the internal state of the game clock based on inspector settings.
    /// </summary>
    private void InitializeClock()
    {
        _currentMinute = _startMinute;
        _currentHour = _startHour;
        _currentDay = _startDay;
        _isPaused = _startPaused;
        _elapsedRealTime = 0f; // Ensure no time accumulates before the first Update

        Debug.Log($"GameClockSystem initialized. Starting time: Day {_currentDay}, Hour {_currentHour}, Minute {_currentMinute}. Paused: {_isPaused}");

        // Immediately broadcast the initial time so subscribers can update their state.
        OnGameTimeChanged?.Invoke(_currentHour, _currentMinute, _currentDay);
    }

    private void Update()
    {
        // If the clock is paused, no time progression occurs.
        if (_isPaused) return;

        // Accumulate real-world time passed since the last frame.
        _elapsedRealTime += Time.deltaTime;

        // Check if enough real-world time has passed for at least one game minute to elapse.
        if (_elapsedRealTime >= _realSecondsPerGameMinute)
        {
            // Calculate how many game minutes have passed in this frame.
            int minutesToAdd = Mathf.FloorToInt(_elapsedRealTime / _realSecondsPerGameMinute);
            // Deduct the real-time consumed by the passed game minutes.
            _elapsedRealTime -= minutesToAdd * _realSecondsPerGameMinute;

            // Advance the game time by the calculated number of minutes.
            AdvanceTime(minutesToAdd);
        }
    }

    // --- Public Methods for Time Control ---

    /// <summary>
    /// Pauses the game clock, stopping time progression.
    /// </summary>
    public void Pause()
    {
        if (!_isPaused)
        {
            _isPaused = true;
            Debug.Log("Game Clock Paused.");
        }
    }

    /// <summary>
    /// Resumes the game clock, allowing time to progress again.
    /// </summary>
    public void Resume()
    {
        if (_isPaused)
        {
            _isPaused = false;
            Debug.Log("Game Clock Resumed.");
        }
    }

    /// <summary>
    /// Toggles the pause state of the game clock.
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    /// <summary>
    /// Sets the current game time directly to a specific hour, minute, and day.
    /// This will reset the internal real-time accumulator to prevent immediate minute-passing.
    /// </summary>
    /// <param name="hour">The hour to set (0-23).</param>
    /// <param name="minute">The minute to set (0-59).</param>
    /// <param name="day">The day to set (1 or more).</param>
    public void SetTime(int hour, int minute, int day)
    {
        // Basic validation for time components.
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || day < 1)
        {
            Debug.LogWarning($"Invalid time set: Hour {hour}, Minute {minute}, Day {day}. Time not changed.", this);
            return;
        }

        _currentHour = hour;
        _currentMinute = minute;
        _currentDay = day;
        _elapsedRealTime = 0f; // Reset real time accumulation to avoid immediate minute pass.

        Debug.Log($"Game Time Manually Set to: Day {_currentDay}, Hour {_currentHour:D2}, Minute {_currentMinute:D2}");

        // Broadcast the new time to all subscribers.
        OnGameTimeChanged?.Invoke(_currentHour, _currentMinute, _currentDay);
    }

    /// <summary>
    /// Advances the game time by a specified number of game minutes.
    /// This is useful for specific game events like fast travel, sleeping, or waiting mechanics.
    /// </summary>
    /// <param name="minutes">The number of game minutes to advance. Must be non-negative.</param>
    public void AdvanceTime(int minutes)
    {
        if (minutes < 0)
        {
            Debug.LogWarning("Cannot advance time by negative minutes.", this);
            return;
        }
        if (minutes == 0) return; // No need to do anything if no minutes to advance.

        // Loop through each minute to ensure all minute, hour, and day events are fired correctly.
        for (int i = 0; i < minutes; i++)
        {
            _currentMinute++;

            if (_currentMinute >= 60)
            {
                _currentMinute = 0;
                // A new minute (00) just started after the last one passed.
                // This is the trigger for OnMinutePassed.
                OnMinutePassed?.Invoke(); // Fire minute event for the start of the new minute (e.g., 1:00, 2:00)

                _currentHour++;
                if (_currentHour >= 24)
                {
                    _currentHour = 0;
                    // An hour just completed, and a new one (00) started.
                    OnHourPassed?.Invoke(); // Fire hour event

                    _currentDay++;
                    // A day just completed, and a new one started.
                    OnDayPassed?.Invoke(); // Fire day event
                }
            }
            // Always notify general time change for each minute advanced, even if only minute changed.
            OnGameTimeChanged?.Invoke(_currentHour, _currentMinute, _currentDay);
        }

        // Log the final state after advancing time.
        Debug.Log($"Time advanced by {minutes} minutes. Current: Day {_currentDay}, Hour {_currentHour:D2}, Minute {_currentMinute:D2}");
    }

    /// <summary>
    /// Gets the current game time as a formatted string (e.g., "Day 1, 06:30").
    /// </summary>
    /// <returns>A formatted string representing the current game time.</returns>
    public string GetFormattedTime()
    {
        return $"Day {_currentDay}, {_currentHour:D2}:{_currentMinute:D2}";
    }

    /// <summary>
    /// Sets the time scale factor. Higher values for `realSecondsPerGameMinute`
    /// mean game time passes slower; lower values mean it passes faster.
    /// (e.g., 0.5 real seconds per game minute means 2 game minutes pass per 1 real second).
    /// </summary>
    /// <param name="realSecondsPerGameMinute">The new real seconds per game minute value.</param>
    public void SetRealSecondsPerGameMinute(float realSecondsPerGameMinute)
    {
        if (realSecondsPerGameMinute <= 0)
        {
            Debug.LogWarning("Real seconds per game minute must be greater than 0.", this);
            return;
        }
        _realSecondsPerGameMinute = realSecondsPerGameMinute;
        Debug.Log($"Game time scale set: {_realSecondsPerGameMinute} real seconds per game minute.");
    }
}
```

## 2. Unity Setup Steps

To use the `GameClockSystem` in your Unity project:

1.  **Create a C# Script:** Create a new C# script named `GameClockSystem` in your Project window and copy the code above into it.
2.  **Create a GameObject:** In your Unity scene, create an empty GameObject (e.g., right-click in the Hierarchy -> Create Empty). Name it `_GameClockSystem` (the underscore often denotes a manager object).
3.  **Attach the Script:** Drag the `GameClockSystem.cs` script onto the `_GameClockSystem` GameObject in the Hierarchy or add it as a component in the Inspector.
4.  **Configure in Inspector:**
    *   **Real Seconds Per Game Minute:** This is the core time scale.
        *   `1.0` means 1 real-world second = 1 game minute. (A day will last 24 real minutes).
        *   `0.1` means 0.1 real-world seconds = 1 game minute. (10 game minutes per real second, very fast).
        *   `60.0` means 60 real-world seconds = 1 game minute. (1 game minute per real minute, very slow).
    *   **Start Hour/Minute/Day:** Set the initial time for your game.
    *   **Start Paused:** Check this if you want the clock to be paused immediately when the game starts.

## 3. Example Usage: GameTimeDisplay.cs (Subscriber Script)

This example script demonstrates how other MonoBehaviour scripts can subscribe to the `GameClockSystem`'s events to react to time changes and update a UI display.

You'll need `TextMeshPro` for the UI elements. Go to `Window > TextMeshPro > Import TMP Essential Resources` if you haven't already.

```csharp
// GameTimeDisplay.cs
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System; // Required for Action delegate

/// <summary>
/// This script demonstrates how to subscribe to the GameClockSystem's events
/// to update UI elements and react to time changes within the game.
/// It displays the current game time (Day, Hour, Minute) and logs specific
/// events (minute, hour, day passed).
/// </summary>
public class GameTimeDisplay : MonoBehaviour
{
    // --- Inspector References ---
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI element to display the current hour and minute.")]
    [SerializeField] private TextMeshProUGUI _timeText;
    [Tooltip("TextMeshProUGUI element to display the current day.")]
    [SerializeField] private TextMeshProUGUI _dayText;
    [Tooltip("TextMeshProUGUI element to log time-related events.")]
    [SerializeField] private TextMeshProUGUI _eventLogText;

    // --- MonoBehaviour Lifecycle ---

    private void Start()
    {
        // Basic validation for UI references.
        if (_timeText == null || _dayText == null || _eventLogText == null)
        {
            Debug.LogError("One or more TextMeshProUGUI references not assigned in GameTimeDisplay. Please assign them in the Inspector.", this);
            enabled = false; // Disable this script if references are missing
            return;
        }

        // Check if the GameClockSystem instance is available.
        if (GameClockSystem.Instance != null)
        {
            // --- Subscribe to GameClockSystem events ---
            // The OnGameTimeChanged event is useful for updating the display whenever any time component changes.
            GameClockSystem.Instance.OnGameTimeChanged += UpdateDisplay;
            // The specific minute/hour/day events are useful for triggering specific logic or logging.
            GameClockSystem.Instance.OnMinutePassed += LogMinutePassed;
            GameClockSystem.Instance.OnHourPassed += LogHourPassed;
            GameClockSystem.Instance.OnDayPassed += LogDayPassed;

            // Immediately update the display with the current time upon starting.
            UpdateDisplay(GameClockSystem.Instance.CurrentHour, GameClockSystem.Instance.CurrentMinute, GameClockSystem.Instance.CurrentDay);
            _eventLogText.text = "Game Clock System Initialized.\n";
        }
        else
        {
            Debug.LogError("GameClockSystem instance not found! Make sure it's in the scene and active.", this);
            enabled = false; // Disable this script if the clock system is missing
        }
    }

    private void OnDestroy()
    {
        // --- Unsubscribe from GameClockSystem events ---
        // It's crucial to unsubscribe from events when the object is destroyed
        // to prevent potential memory leaks or null reference exceptions
        // if the GameClockSystem persists longer than this subscriber.
        if (GameClockSystem.Instance != null)
        {
            GameClockSystem.Instance.OnGameTimeChanged -= UpdateDisplay;
            GameClockSystem.Instance.OnMinutePassed -= LogMinutePassed;
            GameClockSystem.Instance.OnHourPassed -= LogHourPassed;
            GameClockSystem.Instance.OnDayPassed -= LogDayPassed;
        }
    }

    // --- Event Handler Methods ---

    /// <summary>
    /// Updates the UI text elements with the current game time.
    /// This method is called by the OnGameTimeChanged event.
    /// </summary>
    /// <param name="hour">The current hour (0-23).</param>
    /// <param name="minute">The current minute (0-59).</param>
    /// <param name="day">The current day (1 or more).</param>
    private void UpdateDisplay(int hour, int minute, int day)
    {
        _timeText.text = $"{hour:D2}:{minute:D2}"; // Formats as HH:MM (e.g., "06:00")
        _dayText.text = $"Day {day}";
    }

    /// <summary>
    /// Logs a message when a game minute has passed.
    /// This method is called by the OnMinutePassed event.
    /// </summary>
    private void LogMinutePassed()
    {
        LogEvent($"[Minute] {_timeText.text}");
    }

    /// <summary>
    /// Logs a message when a game hour has passed.
    /// This method is called by the OnHourPassed event.
    /// </summary>
    private void LogHourPassed()
    {
        LogEvent($"[Hour] {_timeText.text}");
    }

    /// <summary>
    /// Logs a message when a game day has passed.
    /// This method is called by the OnDayPassed event.
    /// </summary>
    private void LogDayPassed()
    {
        LogEvent($"[Day] {_dayText.text}");
    }

    /// <summary>
    /// Helper method to append messages to the event log UI.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void LogEvent(string message)
    {
        _eventLogText.text = $"{message}\n{_eventLogText.text}";
        // Keep the log from getting excessively long
        if (_eventLogText.text.Length > 2000) // Trim log if it exceeds 2000 characters
        {
            _eventLogText.text = _eventLogText.text.Substring(0, 1000) + "...\n(Log trimmed)";
        }
    }

    // --- Example Methods for UI Button Interaction ---
    // These methods can be hooked up to Unity UI Buttons in the Inspector.

    public void OnClickTogglePauseButton()
    {
        GameClockSystem.Instance?.TogglePause();
    }

    public void OnClickFastForwardButton()
    {
        // Set real seconds per game minute to a lower value to speed up time (e.g., 10x faster)
        GameClockSystem.Instance?.SetRealSecondsPerGameMinute(0.1f); // 1 game minute in 0.1 real seconds
    }

    public void OnClickNormalSpeedButton()
    {
        // Set real seconds per game minute back to a normal value (e.g., 1 game minute per 1 real second)
        GameClockSystem.Instance?.SetRealSecondsPerGameMinute(1.0f);
    }

    public void OnClickAdvanceHourButton()
    {
        // Advance time by 60 game minutes (1 game hour)
        GameClockSystem.Instance?.AdvanceTime(60);
    }

    public void OnClickSetNightButton()
    {
        // Set the time to 8 PM (20:00) on the current day.
        GameClockSystem.Instance?.SetTime(20, 0, GameClockSystem.Instance.CurrentDay);
    }
}
```

## 4. Setting up the Example UI in Unity

1.  **Create UI Canvas:** In your scene, go to `GameObject > UI > Canvas`.
2.  **Add TextMeshPro Objects:**
    *   Right-click on the `Canvas` in the Hierarchy, then `UI > Text - TextMeshPro`.
    *   Rename one to `TimeDisplay`. Adjust its rect transform, font size, and color to be prominent.
    *   Duplicate `TimeDisplay`, rename it `DayDisplay`. Adjust its position.
    *   Duplicate again, rename it `EventLog`. Make it a larger text area (resize its Rect Transform) and set its vertical alignment to `Top` and horizontal to `Left`.
3.  **Add Buttons (Optional but recommended for demonstration):**
    *   Right-click on the `Canvas`, then `UI > Button - TextMeshPro`.
    *   Add buttons for "Toggle Pause", "Fast Forward", "Normal Speed", "Advance Hour", "Set Night". Position them appropriately.
4.  **Create GameTimeUI GameObject:** Create an empty GameObject (e.g., `_GameTimeUI`) in the Hierarchy.
5.  **Attach `GameTimeDisplay.cs`:** Drag the `GameTimeDisplay.cs` script onto the `_GameTimeUI` GameObject.
6.  **Assign UI References:** In the Inspector for `_GameTimeUI`, drag your `TimeDisplay`, `DayDisplay`, and `EventLog` TextMeshPro objects into the corresponding `_timeText`, `_dayText`, and `_eventLogText` fields of the `GameTimeDisplay` script.
7.  **Hook up Button Events:** For each button:
    *   Select the button in the Hierarchy.
    *   In the Inspector, find the `Button` component.
    *   Click the `+` button under `On Click ()`.
    *   Drag the `_GameTimeUI` GameObject from the Hierarchy into the `Runtime Only` slot.
    *   From the dropdown menu (which now shows functions from `_GameTimeUI`), select `GameTimeDisplay` and then the corresponding method (e.g., `OnClickTogglePauseButton`).

Now, run your scene. You should see the time progressing, the day count updating, and event messages appearing in the log as minutes, hours, and days pass in your game!