// Unity Design Pattern Example: DayNightCycle
// This script demonstrates the DayNightCycle pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of a **Day-Night Cycle Manager**, which can be considered a design pattern for managing time-based global states and events in a game. It uses a **Singleton** for easy global access and an **Event-Driven** approach (Observer pattern) to notify other systems about time changes.

This script is designed to be highly configurable via the Unity Inspector and easily extensible by allowing other scripts to subscribe to its events.

---

### **DayNightCycleManager.cs**

To use this script:
1.  Create an empty GameObject in your scene, name it "DayNightCycle".
2.  Attach this `DayNightCycleManager.cs` script to it.
3.  Assign your main **Directional Light** to the `Directional Light` field in the Inspector. If you don't have one, create a new Directional Light (GameObject -> Light -> Directional Light).
4.  Configure the `Day Duration In Minutes` and other parameters as needed.
5.  (Optional) Create a `Skybox Material` if you want to visually change the sky. You might need a custom shader for smooth blending, but this script will handle the `_Tint` property if available, or just set the skybox material.

```csharp
using UnityEngine;
using System; // For Action delegate
using System.Collections; // For IEnumerator

/// <summary>
/// DayNightCycleManager: A comprehensive Unity script for managing a game's day-night cycle.
///
/// This script implements a DayNightCycle pattern by:
/// 1.  **Singleton:** Providing a single, globally accessible instance for easy management.
/// 2.  **Event-Driven (Observer Pattern):** Notifying other systems of time changes (hourly, daily, sunrise/sunset)
///     allowing them to react dynamically without direct coupling.
/// 3.  **Configurability:** Exposing key parameters in the Unity Inspector for designers.
/// 4.  **Real-Time Simulation:** Progressing time based on real-world seconds.
/// 5.  **Environmental Updates:** Automatically adjusting directional light, ambient light, and skybox.
///
/// How to Use:
/// 1. Create an empty GameObject in your scene (e.g., "DayNightCycle").
/// 2. Attach this script to it.
/// 3. Assign your main Directional Light (Sun/Moon) to the 'Directional Light' field in the Inspector.
/// 4. Configure 'Day Duration In Minutes' to set how long a full game day lasts in real-world minutes.
/// 5. Adjust light colors, ambient colors, and start time as desired.
/// 6. Other scripts can subscribe to its events to react to time changes (see example below).
/// </summary>
public class DayNightCycleManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // This allows easy global access to the DayNightCycleManager instance.
    public static DayNightCycleManager Instance { get; private set; }

    [Header("Day Cycle Settings")]
    [Tooltip("How many REAL-WORLD minutes a full GAME day (24 hours) lasts.")]
    [SerializeField]
    private float _dayDurationInRealMinutes = 5f; // A full day lasts 5 real minutes

    [Tooltip("The initial hour when the game starts (0-23).")]
    [SerializeField]
    private int _startHour = 6; // Start at 6 AM

    [Tooltip("The hour when sunrise events are triggered.")]
    [SerializeField]
    private int _sunriseHour = 6; // 6 AM

    [Tooltip("The hour when sunset events are triggered.")]
    [SerializeField]
    private int _sunsetHour = 18; // 6 PM

    [Header("Lighting Settings")]
    [Tooltip("The main directional light representing the sun and moon.")]
    [SerializeField]
    private Light _directionalLight;

    [Tooltip("Color of the directional light during the day.")]
    [SerializeField]
    private Color _dayLightColor = Color.white;

    [Tooltip("Color of the directional light during the night.")]
    [SerializeField]
    private Color _nightLightColor = new Color(0.1f, 0.1f, 0.2f); // Dark blue/purple for moon

    [Tooltip("Intensity of the directional light during the day.")]
    [SerializeField]
    private float _dayLightIntensity = 1f;

    [Tooltip("Intensity of the directional light during the night.")]
    [SerializeField]
    private float _nightLightIntensity = 0.3f; // Moon light intensity

    [Tooltip("Ambient light color during the day.")]
    [SerializeField]
    private Color _dayAmbientColor = new Color(0.6f, 0.6f, 0.7f);

    [Tooltip("Ambient light color during the night.")]
    [SerializeField]
    private Color _nightAmbientColor = new Color(0.1f, 0.1f, 0.25f);

    [Tooltip("Optional: Skybox material to adjust. Requires a skybox shader with a '_Tint' property for smooth transitions.")]
    [SerializeField]
    private Material _skyboxMaterial;

    [Tooltip("Color tint for the skybox during the day.")]
    [SerializeField]
    private Color _daySkyboxTint = new Color(0.5f, 0.7f, 1f);

    [Tooltip("Color tint for the skybox during the night.")]
    [SerializeField]
    private Color _nightSkyboxTint = new Color(0.1f, 0.2f, 0.4f);


    // --- Internal State Variables ---
    private float _currentTimeInGameSeconds; // Current time of day in game seconds (0 to 86400)
    private int _currentDay = 1; // Current day number
    private int _currentHour; // Current hour (0-23)
    private int _currentMinute; // Current minute (0-59)
    private int _currentSecond; // Current second (0-59)
    private int _previousHour = -1; // Used to detect hour changes
    private bool _isDay = true; // Simple state for day/night transition tracking

    // Constants for time calculation
    private const float TotalGameSecondsInDay = 24 * 60 * 60; // 86400 seconds in a game day
    private float _gameSecondsPerRealSecond; // How many game seconds pass per real second

    // --- Events (Observer Pattern) ---
    // These actions allow other scripts to subscribe and react to time changes.

    /// <summary>
    /// Event fired every frame with the current time.
    /// Parameters: (int hour, int minute, int second)
    /// </summary>
    public static event Action<int, int, int> OnTimeChanged;

    /// <summary>
    /// Event fired when the hour changes.
    /// Parameters: (int newHour)
    /// </summary>
    public static event Action<int> OnHourChanged;

    /// <summary>
    /// Event fired when a new day begins (at 00:00).
    /// Parameters: (int newDayNumber)
    /// </summary>
    public static event Action<int> OnDayStarted;

    /// <summary>
    /// Event fired when the cycle transitions to sunrise (e.g., 6 AM).
    /// </summary>
    public static event Action OnSunrise;

    /// <summary>
    /// Event fired when the cycle transitions to sunset (e.g., 6 PM).
    /// </summary>
    public static event Action OnSunset;


    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DayNightCycleManager: Destroying duplicate instance. Ensure only one exists in the scene.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally make the manager persistent across scene loads
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        InitializeCycle();
    }

    private void Update()
    {
        UpdateTime();
        UpdateEnvironment();
    }

    // --- Core Logic Methods ---

    /// <summary>
    /// Initializes the day-night cycle parameters and sets the initial time.
    /// </summary>
    private void InitializeCycle()
    {
        // Calculate how many game seconds pass per real-world second
        // (Total game seconds in a day) / (Total real seconds in _dayDurationInRealMinutes)
        _gameSecondsPerRealSecond = TotalGameSecondsInDay / (_dayDurationInRealMinutes * 60f);

        // Set initial time based on _startHour
        SetTime(_startHour, 0, 0);

        // Ensure the directional light is assigned
        if (_directionalLight == null)
        {
            Debug.LogError("DayNightCycleManager: Directional Light is not assigned! Please assign your main directional light in the Inspector.");
            enabled = false; // Disable the script if no light is assigned
            return;
        }

        // Set initial skybox material if provided
        if (_skyboxMaterial != null)
        {
            RenderSettings.skybox = _skyboxMaterial;
            DynamicGI.UpdateEnvironment(); // Update global illumination
        }
    }

    /// <summary>
    /// Advances the current time based on real-world time progression.
    /// </summary>
    private void UpdateTime()
    {
        _currentTimeInGameSeconds += Time.deltaTime * _gameSecondsPerRealSecond;

        // Handle day rollover
        if (_currentTimeInGameSeconds >= TotalGameSecondsInDay)
        {
            _currentTimeInGameSeconds -= TotalGameSecondsInDay; // Reset to 0 for the new day
            _currentDay++;
            OnDayStarted?.Invoke(_currentDay); // Fire day started event
            Debug.Log($"Day {_currentDay} has begun!");
        }

        // Calculate current hour, minute, second from total game seconds
        _currentHour = (int)(_currentTimeInGameSeconds / 3600f);
        _currentMinute = (int)((_currentTimeInGameSeconds % 3600f) / 60f);
        _currentSecond = (int)(_currentTimeInGameSeconds % 60f);

        // Fire OnTimeChanged event every frame
        OnTimeChanged?.Invoke(_currentHour, _currentMinute, _currentSecond);

        // Check for hour change and fire event
        if (_currentHour != _previousHour)
        {
            OnHourChanged?.Invoke(_currentHour);
            // Debug.Log($"It is now {GetCurrentTimeString()} on Day {_currentDay}");

            // Check for sunrise/sunset events
            if (_currentHour == _sunriseHour && !_isDay)
            {
                _isDay = true;
                OnSunrise?.Invoke();
                Debug.Log($"Sunrise at {GetCurrentTimeString()} on Day {_currentDay}!");
            }
            else if (_currentHour == _sunsetHour && _isDay)
            {
                _isDay = false;
                OnSunset?.Invoke();
                Debug.Log($"Sunset at {GetCurrentTimeString()} on Day {_currentDay}!");
            }

            _previousHour = _currentHour;
        }
    }

    /// <summary>
    /// Updates the environment (light, ambient, skybox) based on the current time.
    /// </summary>
    private void UpdateEnvironment()
    {
        // Calculate the percentage of the day completed (0 to 1)
        float percentageOfDay = _currentTimeInGameSeconds / TotalGameSecondsInDay;

        // --- Update Directional Light Rotation ---
        // Map 0-24 hours to 0-360 degrees rotation around the X-axis.
        // Offset by -90 degrees so that 6 AM (sunrise) is at 0 degrees X (horizontal),
        // 12 PM (noon) is at 90 degrees X (straight up),
        // 6 PM (sunset) is at 180 degrees X (horizontal).
        // The Y rotation can be adjusted in the inspector to change the sun's path.
        float rotationAngleX = (percentageOfDay * 360f) - 90f;
        _directionalLight.transform.rotation = Quaternion.Euler(rotationAngleX, _directionalLight.transform.eulerAngles.y, _directionalLight.transform.eulerAngles.z);


        // --- Update Directional Light Color and Intensity ---
        // Use a time-based blend factor for smooth transitions.
        // For simplicity, we'll assume the transition from night to day happens around sunrise
        // and day to night around sunset. A more complex curve could be used for dawn/dusk.
        float transitionFactor = 0f;
        if (_currentHour >= _sunriseHour && _currentHour < _sunsetHour)
        {
            // Daytime range, transition to day values
            transitionFactor = Mathf.InverseLerp(_sunriseHour, _sunriseHour + 2, _currentHour); // Fully day 2 hours after sunrise
            _isDay = true;
        }
        else
        {
            // Nighttime range, transition to night values
            transitionFactor = Mathf.InverseLerp(_sunsetHour, _sunsetHour + 2, _currentHour); // Fully night 2 hours after sunset
            _isDay = false;
        }

        // Handle wrap-around for night transition (e.g. 22:00 -> 04:00)
        // Lerp from night to day and back. A simple sinus wave could also work.
        // Let's use a normalized time for a smoother, symmetrical transition.
        float normalizedTime = _currentTimeInGameSeconds / TotalGameSecondsInDay; // 0 to 1

        // A factor that smoothly goes from 0 (midnight) to 1 (noon) and back to 0 (midnight)
        float lightBlend = Mathf.Clamp01(Mathf.Lerp(-0.5f, 1.5f, normalizedTime * 2f)); // From -0.5 to 1.5 and clamp for smooth transition
        lightBlend = 1 - Mathf.Abs(normalizedTime * 2f - 1f); // Go from 0 (midnight), to 1 (noon), back to 0 (midnight)
        lightBlend = Mathf.Clamp01(Mathf.Lerp(-0.3f, 1.3f, lightBlend)); // Further fine-tune for dawn/dusk


        // A simpler approach: use a curve for blending, or a direct time ratio
        // Let's create a curve to represent the sun's strength over the day.
        // This is a common way to handle light color/intensity changes over a day.
        AnimationCurve lightIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f), // Midnight
            new Keyframe(0.25f, 0.5f), // Early morning (sunrise transition)
            new Keyframe(0.5f, 1f), // Noon
            new Keyframe(0.75f, 0.5f), // Evening (sunset transition)
            new Keyframe(1f, 0.1f) // Midnight again
        );
        float curveValue = lightIntensityCurve.Evaluate(normalizedTime);
        curveValue = Mathf.Clamp01(curveValue); // Ensure it's between 0 and 1

        _directionalLight.color = Color.Lerp(_nightLightColor, _dayLightColor, curveValue);
        _directionalLight.intensity = Mathf.Lerp(_nightLightIntensity, _dayLightIntensity, curveValue);


        // --- Update Ambient Light ---
        RenderSettings.ambientLight = Color.Lerp(_nightAmbientColor, _dayAmbientColor, curveValue);

        // --- Update Skybox (if material is assigned and supports _Tint) ---
        if (_skyboxMaterial != null && _skyboxMaterial.HasProperty("_Tint"))
        {
            _skyboxMaterial.SetColor("_Tint", Color.Lerp(_nightSkyboxTint, _daySkyboxTint, curveValue));
        }

        // Important: Update environment lighting for changes to take effect in real-time GI
        DynamicGI.UpdateEnvironment();
    }


    /// <summary>
    /// Sets the current time of day. Useful for debugging or specific game events.
    /// </summary>
    /// <param name="hour">The hour to set (0-23).</param>
    /// <param name="minute">The minute to set (0-59).</param>
    /// <param name="second">The second to set (0-59).</param>
    public void SetTime(int hour, int minute, int second)
    {
        _currentTimeInGameSeconds = (hour * 3600f) + (minute * 60f) + second;
        _currentTimeInGameSeconds = Mathf.Clamp(_currentTimeInGameSeconds, 0, TotalGameSecondsInDay - 1); // Clamp within a day
        _previousHour = -1; // Force hour change check on next update
        UpdateEnvironment(); // Immediately update environment
    }

    // --- Public Getters for current time ---
    public int CurrentHour => _currentHour;
    public int CurrentMinute => _currentMinute;
    public int CurrentSecond => _currentSecond;
    public int CurrentDay => _currentDay;
    public bool IsDay => _isDay; // Returns true if it's currently considered 'day' (between sunrise and sunset hours)

    /// <summary>
    /// Returns the current game time as a formatted string (HH:MM:SS).
    /// </summary>
    public string GetCurrentTimeString()
    {
        return $"{_currentHour:00}:{_currentMinute:00}:{_currentSecond:00}";
    }

    /// <summary>
    /// Returns the current game time as a float representing hours (e.g., 14.5 for 2:30 PM).
    /// </summary>
    public float GetCurrentTimeAsHoursFloat()
    {
        return _currentTimeInGameSeconds / 3600f;
    }
}

/// <summary>
/// Example Usage: A simple script that listens to DayNightCycleManager events.
///
/// To use this example:
/// 1. Create a new C# script named "LightBulb".
/// 2. Copy the content below into it.
/// 3. Attach this script to a GameObject with a Light component (e.g., a Point Light for a street lamp).
/// 4. Adjust the Light component's intensity and color when it's off. This script will only turn it on/off.
/// </summary>
/*
using UnityEngine;

public class LightBulb : MonoBehaviour
{
    private Light _myLight;

    [Tooltip("The hour when this light should turn ON.")]
    [SerializeField]
    private int _turnOnHour = 19; // 7 PM

    [Tooltip("The hour when this light should turn OFF.")]
    [SerializeField]
    private int _turnOffHour = 6; // 6 AM

    private void Awake()
    {
        _myLight = GetComponent<Light>();
        if (_myLight == null)
        {
            Debug.LogError("LightBulb: No Light component found on this GameObject. Disabling script.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the hour changed event
        DayNightCycleManager.OnHourChanged += OnHourChanged;
        // Also subscribe to OnDayStarted to handle cases where script enables mid-day or time is set
        DayNightCycleManager.OnDayStarted += OnDayStarted;

        // Immediately update state based on current time when enabled
        if (DayNightCycleManager.Instance != null)
        {
            UpdateLightState(DayNightCycleManager.Instance.CurrentHour);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and errors when the GameObject is destroyed
        DayNightCycleManager.OnHourChanged -= OnHourChanged;
        DayNightCycleManager.OnDayStarted -= OnDayStarted;
    }

    private void OnHourChanged(int newHour)
    {
        UpdateLightState(newHour);
    }

    private void OnDayStarted(int dayNumber)
    {
        // Re-evaluate light state at the start of a new day
        UpdateLightState(DayNightCycleManager.Instance.CurrentHour);
    }

    private void UpdateLightState(int currentHour)
    {
        if (_myLight == null) return;

        bool shouldBeOn = false;

        // Handle cases where turn-on hour is before turn-off hour (e.g., night light)
        if (_turnOnHour < _turnOffHour)
        {
            // Simple day/night cycle
            shouldBeOn = (currentHour >= _turnOnHour && currentHour < _turnOffHour);
        }
        else
        {
            // Night-spanning cycle (e.g., 19:00 ON -> 6:00 OFF)
            shouldBeOn = (currentHour >= _turnOnHour || currentHour < _turnOffHour);
        }

        _myLight.enabled = shouldBeOn;
    }

    // Optional: You could also subscribe to OnTimeChanged for more granular control
    // private void OnTimeChanged(int hour, int minute, int second)
    // {
    //     // Update more frequently, perhaps for fading lights
    // }
}
*/
```