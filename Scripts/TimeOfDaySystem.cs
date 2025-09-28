// Unity Design Pattern Example: TimeOfDaySystem
// This script demonstrates the TimeOfDaySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **TimeOfDaySystem design pattern** in Unity, focusing on creating a centralized, easily accessible, and extensible system for managing in-game time.

This pattern is highly useful for games with dynamic environments, NPC schedules, quest timers, or any feature that needs to react to the passage of time.

---

### **How to Use This Example in Unity:**

1.  **Create an Empty GameObject:** In your Unity scene, create a new empty GameObject (e.g., `GameTimeManager`).
2.  **Attach the Script:** Copy and paste the C# code below into a new script file named `TimeOfDaySystem.cs` and attach it to the `GameTimeManager` GameObject.
3.  **Configure in Inspector:** Select the `GameTimeManager` object. In the Inspector, you can adjust:
    *   **Time Scale:** How fast game time passes relative to real time (e.g., 60 means 1 real second = 1 game minute).
    *   **Start Hour:** The initial hour of the day (0-23).
    *   **Start Day:** The initial day number.
4.  **Observe:** Run the scene. You'll see debug messages in the console confirming the system is initialized.
5.  **Implement Dependent Systems:** Create other scripts (like the `LightController` example provided in the comments) that subscribe to `TimeOfDaySystem.Instance.OnTimeUpdated`, `OnHourChanged`, or `OnDayChanged` to react to time changes.

---

### **`TimeOfDaySystem.cs`**

```csharp
using UnityEngine;
using System; // Required for the Action delegate used in events

/// <summary>
///     [TimeOfDaySystem Pattern]
///     
///     This class serves as the central authority for managing the game's time of day, day cycles, and related events.
///     It embodies the TimeOfDaySystem pattern by:
///     
///     1.  **Centralized Management:** A single point of truth for the current time, hour, minute, and day.
///     2.  **Decoupling:** Other systems (lighting, NPCs, UI, gameplay mechanics) don't need to know *how* time progresses.
///         They simply subscribe to events or query properties of this system.
///     3.  **Event-Driven:** It provides events (`OnTimeUpdated`, `OnHourChanged`, `OnDayChanged`) that other systems can
///         subscribe to, allowing them to react immediately to significant time changes.
///     4.  **Configurability:** Allows designers to easily adjust time progression speed and starting conditions via the Inspector.
///     5.  **Global Access:** Implemented as a Singleton for easy access from any script in the game.
///     
///     How to use this pattern:
///     -------------------------
///     1.  Create an empty GameObject in your scene (e.g., "GameTimeManager").
///     2.  Attach this `TimeOfDaySystem` script to it.
///     3.  Configure the `Time Scale`, `Start Hour`, and `Start Day` in the Unity Inspector.
///     
///     Example of another script (e.g., a LightController) subscribing to time events:
///     --------------------------------------------------------------------------------
///     <code>
///     using UnityEngine;
///     using System;
///     
///     public class LightController : MonoBehaviour
///     {
///         [SerializeField] private Light _directionalLight;
///         [SerializeField] private Gradient _skyColorGradient; // Maps 0-1 (normalized time) to a color
///         [SerializeField] private AnimationCurve _lightIntensityCurve; // Maps 0-1 (normalized time) to intensity
///         [SerializeField] private AnimationCurve _fogDensityCurve; // Maps 0-1 (normalized time) to fog density
///         [SerializeField] private float _maxFogDensity = 0.05f;
/// 
///         private void Awake()
///         {
///             if (_directionalLight == null)
///             {
///                 // Try to find the main directional light if not assigned
///                 _directionalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
///                 if (_directionalLight == null)
///                 {
///                     Debug.LogWarning("LightController: Directional Light not assigned and could not be found.");
///                 }
///             }
///         }
/// 
///         private void OnEnable()
///         {
///             // Subscribe to TimeOfDaySystem events when this component is enabled
///             if (TimeOfDaySystem.Instance != null)
///             {
///                 TimeOfDaySystem.Instance.OnTimeUpdated += HandleTimeUpdate;
///                 TimeOfDaySystem.Instance.OnHourChanged += HandleHourChange;
///                 TimeOfDaySystem.Instance.OnDayChanged += HandleDayChange;
///             }
///             else
///             {
///                 Debug.LogError("LightController: TimeOfDaySystem.Instance is null. Make sure TimeOfDaySystem is in the scene and initialized.");
///             }
///         }
/// 
///         private void OnDisable()
///         {
///             // Unsubscribe from TimeOfDaySystem events when this component is disabled to prevent memory leaks
///             if (TimeOfDaySystem.Instance != null)
///             {
///                 TimeOfDaySystem.Instance.OnTimeUpdated -= HandleTimeUpdate;
///                 TimeOfDaySystem.Instance.OnHourChanged -= HandleHourChange;
///                 TimeOfDaySystem.Instance.OnDayChanged -= HandleDayChange;
///             }
///         }
/// 
///         /// <summary>
///         /// Called every frame when the time updates.
///         /// </summary>
///         /// <param name="currentHourOfDay">The current hour as a float (0.0 to 24.0).</param>
///         private void HandleTimeUpdate(float currentHourOfDay)
///         {
///             // Normalize time to a 0-1 range for gradients and curves
///             float normalizedTime = currentHourOfDay / 24f; 
/// 
///             // Update light color based on time
///             if (_directionalLight != null && _skyColorGradient != null)
///             {
///                 _directionalLight.color = _skyColorGradient.Evaluate(normalizedTime);
///             }
///             
///             // Update light intensity based on time
///             if (_directionalLight != null && _lightIntensityCurve != null)
///             {
///                 _directionalLight.intensity = _lightIntensityCurve.Evaluate(normalizedTime);
///             }
/// 
///             // Rotate the directional light to simulate sun/moon movement
///             if (_directionalLight != null)
///             {
///                 // A full day (24 hours) is 360 degrees rotation.
///                 // 0h (midnight) -> 0 degrees (pointing straight down, or specific angle)
///                 // 6h (sunrise)  -> 90 degrees
///                 // 12h (noon)    -> 180 degrees
///                 // 18h (sunset)  -> 270 degrees
///                 // We'll map 0-24 hours to a 0-360 degree rotation around the X-axis for elevation.
///                 // A common setup is that noon (12h) is when the sun is highest (e.g., 90 degrees elevation),
///                 // and midnight (0h/24h) is lowest (e.g., -90 degrees elevation).
///                 // So, 12 hours maps to 180 degrees from -90 to 90. 
///                 // (currentHourOfDay - 6) * (360 / 24) shifts it so 6 AM is sunrise (90 deg from horizon)
///                 // Or, a simpler approach:
///                 float xRotation = (currentHourOfDay / 24f) * 360f - 90f; // Noon at 90 deg, midnight at -90 deg
///                 _directionalLight.transform.rotation = Quaternion.Euler(new Vector3(xRotation, 50f, 0f)); // Y axis for direction
///             }
/// 
///             // Update fog density based on time (e.g., denser at night/early morning)
///             if (_fogDensityCurve != null)
///             {
///                 RenderSettings.fogDensity = _fogDensityCurve.Evaluate(normalizedTime) * _maxFogDensity;
///             }
///         }
/// 
///         /// <summary>
///         /// Called when the game hour changes.
///         /// </summary>
///         /// <param name="newHour">The new current hour (0-23).</param>
///         private void HandleHourChange(int newHour)
///         {
///             // Debug.Log($"LightController: It's now {newHour:D2}:00");
///             // This is a good place to trigger hourly events like NPC schedule updates,
///             // specific sound effects, or UI notifications for a new hour.
///         }
/// 
///         /// <summary>
///         /// Called when a new game day begins.
///         /// </summary>
///         /// <param name="newDay">The new current day number.</param>
///         private void HandleDayChange(int newDay)
///         {
///             // Debug.Log($"LightController: A new day has begun: Day {newDay}");
///             // This is useful for daily resets (e.g., daily quests, daily login bonuses, shop stock refresh).
///         }
///     }
///     </code>
/// </summary>
public class TimeOfDaySystem : MonoBehaviour
{
    // --- [ Singleton Instance ] ---
    /// <summary>
    /// Static singleton instance of the TimeOfDaySystem.
    /// Allows other scripts to easily access the time system globally without direct references.
    /// </summary>
    public static TimeOfDaySystem Instance { get; private set; }

    // --- [ Inspector Settings ] ---
    [Header("Time Settings")]
    [Tooltip("The speed at which game time passes relative to real time. " +
             "E.g., 1.0 = real-time, 60.0 = 1 real second = 1 game minute, " +
             "1440.0 = 1 real second = 1 game hour (24 real seconds per game day).")]
    [SerializeField] private float _timeScale = 60f; // Default: 1 real second = 1 game minute

    [Tooltip("The starting hour of the day (0.0 to 23.99).")]
    [Range(0f, 23.99f)]
    [SerializeField] private float _startHour = 6f; // Start at 6 AM

    [Tooltip("The starting day number (must be 1 or greater).")]
    [SerializeField] private int _startDay = 1;

    // --- [ Internal Time State ] ---
    private float _currentTimeInHours; // Tracks the current time as a float (0.0 to 24.0)
    private int _currentHour;          // Current whole hour (0-23)
    private int _currentMinute;        // Current whole minute (0-59)
    private int _currentDay;           // Current day number

    private int _previousHour = -1; // Used to detect hour changes and trigger events
    private int _previousDay = -1;  // Used to detect day changes and trigger events

    // --- [ Events ] ---
    /// <summary>
    /// Event fired every frame when time is updated.
    /// Provides the current hour of the day as a float (0.0 to 24.0).
    /// </summary>
    public event Action<float> OnTimeUpdated;

    /// <summary>
    /// Event fired when the hour changes (e.g., from 05:59 to 06:00).
    /// Provides the new current hour (0-23).
    /// </summary>
    public event Action<int> OnHourChanged;

    /// <summary>
    /// Event fired when a new day begins (e.g., from Day 1, 23:59 to Day 2, 00:00).
    /// Provides the new current day number.
    /// </summary>
    public event Action<int> OnDayChanged;

    // --- [ Public Properties for Current Time ] ---
    /// <summary>
    /// Gets the current hour of the day as a float (0.0 to 24.0).
    /// Useful for systems that need smooth time progression (e.g., light rotation, gradient evaluation).
    /// </summary>
    public float CurrentTimeOfDay => _currentTimeInHours;

    /// <summary>
    /// Gets the current whole hour (0-23).
    /// </summary>
    public int CurrentHour => _currentHour;

    /// <summary>
    /// Gets the current whole minute (0-59).
    /// </summary>
    public int CurrentMinute => _currentMinute;

    /// <summary>
    /// Gets the current day number.
    /// </summary>
    public int CurrentDay => _currentDay;

    // --- [ MonoBehaviour Lifecycle ] ---

    private void Awake()
    {
        // Implement Singleton pattern to ensure only one instance exists and is globally accessible.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TimeOfDaySystem: Duplicate instance found! Destroying this one. " +
                             "Ensure only one TimeOfDaySystem exists in your scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Keep the time system persistent across scene loads.
        // Remove this line if you want time to reset or be managed per scene.
        DontDestroyOnLoad(gameObject); 

        InitializeTime();
    }

    private void Update()
    {
        AdvanceTime();
        UpdateCurrentTimeComponents(); // Update hour/minute based on advanced float time
        CheckForEvents();              // Check if hour/day changed and invoke events
    }

    // --- [ Private Helper Methods ] ---

    /// <summary>
    /// Initializes the time system's internal state.
    /// </summary>
    private void InitializeTime()
    {
        // Ensure the start hour is within the 0-23.99 range
        _currentTimeInHours = Mathf.Repeat(_startHour, 24f); 
        // Ensure day starts at a valid number (at least 1)
        _currentDay = Mathf.Max(1, _startDay);

        // Force an initial update to set properties and trigger initial events for subscribers
        UpdateCurrentTimeComponents();
        // Set previous values to current to avoid triggering change events immediately after initialization
        _previousHour = _currentHour; 
        _previousDay = _currentDay;   
        
        // Invoke initial events so any subscribed systems get the correct starting state
        OnTimeUpdated?.Invoke(_currentTimeInHours);
        OnHourChanged?.Invoke(_currentHour);
        OnDayChanged?.Invoke(_currentDay);
        
        Debug.Log($"TimeOfDaySystem initialized. Current Day: {_currentDay}, Time: {_currentHour:D2}:{_currentMinute:D2}");
    }

    /// <summary>
    /// Advances the internal float time based on real-world time and the configured time scale.
    /// Handles day transitions by incrementing the day counter.
    /// </summary>
    private void AdvanceTime()
    {
        // Calculate how many game hours pass this frame:
        // (Time.deltaTime seconds * _timeScale game_minutes/real_second) / 60 game_minutes/game_hour
        _currentTimeInHours += (Time.deltaTime * _timeScale) / 60f;

        // Wrap _currentTimeInHours around 24 to handle day transitions.
        // Using a while loop is robust for very high time scales or dropped frames where
        // _currentTimeInHours might jump by more than 24.
        while (_currentTimeInHours >= 24f)
        {
            _currentTimeInHours -= 24f; // Reset hours for the new day
            _currentDay++;              // Increment day
        }
    }

    /// <summary>
    /// Updates the integer hour and minute components from the float `_currentTimeInHours`.
    /// </summary>
    private void UpdateCurrentTimeComponents()
    {
        _currentHour = Mathf.FloorToInt(_currentTimeInHours);
        _currentMinute = Mathf.FloorToInt((_currentTimeInHours - _currentHour) * 60f);
    }

    /// <summary>
    /// Checks for changes in the current hour and day, and invokes the corresponding events.
    /// </summary>
    private void CheckForEvents()
    {
        // Always invoke OnTimeUpdated every frame, as it provides continuous time information
        OnTimeUpdated?.Invoke(_currentTimeInHours);

        // Check for hour change
        if (_currentHour != _previousHour)
        {
            OnHourChanged?.Invoke(_currentHour);
            _previousHour = _currentHour; // Update previous hour for the next check
            // Debug.Log($"TimeOfDaySystem: New Hour: {_currentHour:D2}:00"); // Uncomment for detailed debugging
        }

        // Check for day change
        if (_currentDay != _previousDay)
        {
            OnDayChanged?.Invoke(_currentDay);
            _previousDay = _currentDay; // Update previous day for the next check
            // Debug.Log($"TimeOfDaySystem: New Day: {_currentDay}"); // Uncomment for detailed debugging
        }
    }

    /// <summary>
    /// Public method to programmatically set the time of day and current day.
    /// This can be used for debugging, cheat codes, or specific gameplay events (e.g., sleeping until morning).
    /// </summary>
    /// <param name="hour">The hour to set (0.0 to 23.99).</param>
    /// <param name="day">The day to set (must be 1 or greater).</param>
    public void SetTime(float hour, int day)
    {
        _startHour = hour; // Update startHour for consistency with future initializations
        _startDay = day;   // Update startDay for consistency
        InitializeTime();  // Re-initialize the system with the new time
        Debug.Log($"TimeOfDaySystem: Time set to Day: {_currentDay}, Hour: {_currentHour:D2}:{_currentMinute:D2}");
    }

    // --- [ Debugging / Editor Functionality ] ---
    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// Used here to clamp input values to valid ranges.
    /// </summary>
    private void OnValidate()
    {
        // Clamp start hour to ensure it's always within a valid 24-hour cycle
        _startHour = Mathf.Repeat(_startHour, 24f);
        // Ensure day starts at 1 or greater
        _startDay = Mathf.Max(1, _startDay);
    }
}
```