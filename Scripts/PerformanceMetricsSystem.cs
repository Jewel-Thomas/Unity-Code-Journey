// Unity Design Pattern Example: PerformanceMetricsSystem
// This script demonstrates the PerformanceMetricsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of a 'PerformanceMetricsSystem' design pattern. While not one of the classic GoF patterns, it's a common architectural need in games: a centralized system to collect, store, and report various performance and game-specific metrics.

This example focuses on ease of use, demonstrating how other game systems can contribute data with minimal effort, and how the system can display and log that data.

---

### How to Use This Script in Unity:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `PerformanceMetricsSystem.cs`.
2.  **Copy and Paste:** Copy the entire code block below and paste it into your newly created `PerformanceMetricsSystem.cs` file, replacing its default content.
3.  **Create a GameObject:** In any of your Unity scenes, create an empty GameObject (e.g., right-click in the Hierarchy -> Create Empty).
4.  **Attach Script:** Name this GameObject something like "PerformanceMetrics" and attach the `PerformanceMetricsSystem.cs` script to it.
5.  **Run Your Scene:** Play the scene.
    *   You will see a translucent white overlay in the top-left corner displaying various metrics (FPS, Memory, and simulated game events).
    *   Press the **'M' key** (default) to toggle the visibility of this overlay.
    *   Check the Unity Console for periodic metric logs if `Log To Console` is enabled in the Inspector.
6.  **Customize:** Adjust the public variables in the Inspector on your "PerformanceMetrics" GameObject to configure update intervals, overlay visibility, logging, etc.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Text; // For StringBuilder for efficient string concatenation in OnGUI
using System.Linq; // For Average() calculation on Queue

// PerformanceMetricsSystem.cs
// This script demonstrates the 'PerformanceMetricsSystem' design pattern.
// It provides a centralized, easy-to-use system for tracking various performance-related
// and game-specific metrics in a Unity application.

// The pattern involves:
// 1. A central data collection point (this class, implemented as a MonoBehaviour Singleton).
// 2. Simple static APIs for other parts of the game to record metrics.
// 3. Internal storage for different types of metrics (counters, timers, gauges).
// 4. A reporting mechanism (here, an in-game UI overlay using OnGUI and console logs).
// 5. Automatic collection of core system metrics (FPS, Memory usage).
// 6. Persistence across scenes (using DontDestroyOnLoad).

// To use this system:
// 1. Create an empty GameObject in your scene (e.g., "PerformanceMetrics").
// 2. Attach this script to it.
// 3. The system will automatically persist across scenes.
// 4. To record metrics from any other script, call its static methods:
//    - PerformanceMetricsSystem.RecordCounter("EnemiesKilled");
//    - PerformanceMetricsSystem.StartTimer("LoadingTime");
//    - // ... perform loading operations ...
//    - PerformanceMetricsSystem.EndTimer("LoadingTime");
//    - PerformanceMetricsSystem.SetGauge("PlayerHealth", playerHealth);
// 5. Toggle the overlay visibility with the 'M' key (default).

public class PerformanceMetricsSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    // Provides a global point of access to the PerformanceMetricsSystem.
    // This ensures there's only one instance managing metrics throughout the game.
    public static PerformanceMetricsSystem Instance { get; private set; }

    // --- Configuration ---
    [Header("System Configuration")]
    [Tooltip("Enable or disable the metric collection and display entirely.")]
    public bool enableMetrics = true;

    [Tooltip("How often, in seconds, the system updates its internal metrics (like FPS, Memory) and refreshes the display.")]
    [Range(0.1f, 2.0f)]
    public float updateInterval = 0.5f;

    [Tooltip("If true, a basic UI overlay will display the collected metrics in the top-left corner.")]
    public bool showOverlay = true;

    [Tooltip("If true, metrics will also be logged to the Unity console periodically at the update interval.")]
    public bool logToConsole = false;

    [Tooltip("The keyboard key used to toggle the visibility of the UI overlay.")]
    public KeyCode toggleOverlayKey = KeyCode.M;

    // --- Internal Data Storage ---
    // Dictionaries are used to hold different types of metrics,
    // allowing flexible naming with string keys.

    // Counters: For events that accumulate integer values (e.g., enemies killed, items collected, actions performed).
    private Dictionary<string, int> _counters = new Dictionary<string, int>();

    // Gauges: For values that represent a current state or measurement (e.g., player health, current FPS, memory usage, resource levels).
    private Dictionary<string, float> _gauges = new Dictionary<string, float>();

    // Active Timers: Stores the Time.realtimeSinceStartup value when a timer began.
    // Used to calculate duration when EndTimer is called.
    private Dictionary<string, float> _activeTimers = new Dictionary<string, float>();

    // Completed Timers: Stores the last recorded duration for a specific timer key.
    // This allows the overlay to show the duration of recently completed events.
    private Dictionary<string, float> _completedTimers = new Dictionary<string, float>();

    // --- FPS Calculation ---
    // A queue stores recent FPS samples to calculate an average FPS, providing a smoother reading.
    private Queue<float> _fpsSamples = new Queue<float>();
    [Tooltip("The number of recent frames to average for the FPS calculation.")]
    [Range(10, 200)]
    public int maxFpsSamples = 60;

    private float _lastUpdateTime = 0f; // Tracks when the last periodic update occurred.
    private float _smoothedDeltaTime = 0f; // Used for a more stable 'current FPS' reading.

    // --- OnGUI Display ---
    // GUIStyle for custom text appearance in the OnGUI overlay.
    private GUIStyle _guiStyle;
    // StringBuilder is used for efficient string concatenation for the UI display,
    // reducing GC allocations compared to repeated string + operations.
    private StringBuilder _stringBuilder = new StringBuilder();

    // --- Lifecycle Methods ---
    private void Awake()
    {
        // Singleton enforcement: If an instance already exists, destroy this duplicate.
        // Otherwise, set this as the singleton instance.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PerformanceMetricsSystem: Destroying duplicate instance. Ensure only one exists in your scenes.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Keep this GameObject alive across scene changes, so metrics persist.
        DontDestroyOnLoad(gameObject);

        // Initialize GUI style for the overlay.
        _guiStyle = new GUIStyle();
        _guiStyle.fontSize = 18;
        _guiStyle.normal.textColor = Color.white;
        _guiStyle.padding = new RectOffset(10, 10, 10, 10);
        _guiStyle.fontStyle = FontStyle.Bold;
        _guiStyle.alignment = TextAnchor.UpperLeft;
        // Add a background to the text for better readability
        _guiStyle.normal.background = MakeTex(1, 1, new Color(0, 0, 0, 0.7f)); 
    }

    // Helper to create a single-pixel texture for background (for OnGUI)
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Update()
    {
        if (!enableMetrics) return;

        // Toggle overlay visibility with the configured key.
        if (Input.GetKeyDown(toggleOverlayKey))
        {
            showOverlay = !showOverlay;
            Debug.Log($"PerformanceMetricsSystem overlay toggled: {showOverlay}");
        }

        // Collect FPS samples for averaging.
        // Using unscaledDeltaTime for true frame rate, independent of Time.timeScale.
        // Smoothed delta time provides a more stable instantaneous FPS reading.
        _smoothedDeltaTime += (Time.unscaledDeltaTime - _smoothedDeltaTime) * 0.1f;
        float currentInstantFPS = 1.0f / _smoothedDeltaTime;
        _fpsSamples.Enqueue(currentInstantFPS);
        // Maintain a fixed number of samples in the queue.
        while (_fpsSamples.Count > maxFpsSamples)
        {
            _fpsSamples.Dequeue();
        }

        // Update system-level metrics (FPS, Memory) and display/log periodically.
        if (Time.realtimeSinceStartup - _lastUpdateTime >= updateInterval)
        {
            UpdateSystemMetrics(); // Recalculate and update internal system metrics.
            if (logToConsole)
            {
                LogMetricsToConsole(); // Output metrics to the console.
            }
            _lastUpdateTime = Time.realtimeSinceStartup; // Reset timer for next update.
        }
    }

    // OnGUI is called for rendering and handling GUI events.
    // It draws the metric overlay if enabled.
    private void OnGUI()
    {
        if (!enableMetrics || !showOverlay) return;

        // Build the metric string efficiently using StringBuilder.
        _stringBuilder.Clear(); // Clear previous content.
        _stringBuilder.AppendLine("--- Performance Metrics ---");

        // Display Gauges
        _stringBuilder.AppendLine("\n<b>GAUGES:</b>"); // Using HTML-like tags for rich text in OnGUI
        foreach (var pair in _gauges)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value:F2}");
        }

        // Display Counters
        _stringBuilder.AppendLine("\n<b>COUNTERS:</b>");
        foreach (var pair in _counters)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value}");
        }

        // Display Completed Timers (showing their last recorded duration)
        _stringBuilder.AppendLine("\n<b>TIMERS (Last Duration):</b>");
        foreach (var pair in _completedTimers)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value:F4}s");
        }

        // Display Active Timers (showing their current running duration)
        if (_activeTimers.Count > 0)
        {
            _stringBuilder.AppendLine("\n<b>TIMERS (Currently Active):</b>");
            foreach (var pair in _activeTimers)
            {
                _stringBuilder.AppendLine($"- {pair.Key}: {(Time.realtimeSinceStartup - pair.Value):F4}s (running)");
            }
        }

        // Draw the text overlay on screen.
        // Rect defines position and size. Using Screen.height for dynamic sizing.
        GUI.Label(new Rect(10, 10, 300, Screen.height - 20), _stringBuilder.ToString(), _guiStyle);
    }

    // --- Public Static API for Recording Metrics ---
    // These methods provide the interface for other game scripts to interact with the system.

    /// <summary>
    /// Increments a named counter. Useful for tracking events like kills, pickups, ability uses, etc.
    /// If the counter doesn't exist, it's initialized.
    /// </summary>
    /// <param name="key">The unique name/identifier of the counter.</param>
    /// <param name="increment">The amount to increment the counter by (default is 1).</param>
    public static void RecordCounter(string key, int increment = 1)
    {
        // Only proceed if the system is enabled and an instance exists.
        if (Instance == null || !Instance.enableMetrics) return;

        if (Instance._counters.ContainsKey(key))
        {
            Instance._counters[key] += increment;
        }
        else
        {
            Instance._counters.Add(key, increment);
        }
    }

    /// <summary>
    /// Sets the value of a named gauge. Useful for tracking current states like player health, ammo,
    /// resource levels, current FPS, memory usage, etc.
    /// If the gauge doesn't exist, it's initialized.
    /// </summary>
    /// <param name="key">The unique name/identifier of the gauge.</param>
    /// <param name="value">The current value to set for the gauge.</param>
    public static void SetGauge(string key, float value)
    {
        if (Instance == null || !Instance.enableMetrics) return;

        if (Instance._gauges.ContainsKey(key))
        {
            Instance._gauges[key] = value;
        }
        else
        {
            Instance._gauges.Add(key, value);
        }
    }

    /// <summary>
    /// Starts a timer for a specific event. The timer will track the duration until <see cref="EndTimer"/>
    /// is called with the same key.
    /// </summary>
    /// <param name="key">The unique name/identifier of the timer.</param>
    public static void StartTimer(string key)
    {
        if (Instance == null || !Instance.enableMetrics) return;

        // If the timer is already active, log a warning and restart it.
        if (Instance._activeTimers.ContainsKey(key))
        {
            Debug.LogWarning($"PerformanceMetricsSystem: Timer '{key}' was already started. Restarting it now.");
            Instance._activeTimers[key] = Time.realtimeSinceStartup;
        }
        else
        {
            Instance._activeTimers.Add(key, Time.realtimeSinceStartup);
        }
    }

    /// <summary>
    /// Stops a previously started timer and records its duration.
    /// </summary>
    /// <param name="key">The unique name/identifier of the timer.</param>
    /// <returns>The duration of the timer in seconds, or -1 if the timer was not found (i.e., not started or already ended).</returns>
    public static float EndTimer(string key)
    {
        if (Instance == null || !Instance.enableMetrics) return -1f;

        // Try to retrieve the start time of the timer.
        if (Instance._activeTimers.TryGetValue(key, out float startTime))
        {
            float duration = Time.realtimeSinceStartup - startTime; // Calculate duration.
            Instance._activeTimers.Remove(key); // Remove from active timers.

            // Store the completed duration.
            if (Instance._completedTimers.ContainsKey(key))
            {
                Instance._completedTimers[key] = duration;
            }
            else
            {
                Instance._completedTimers.Add(key, duration);
            }
            return duration; // Return the calculated duration.
        }
        else
        {
            // If EndTimer is called without a corresponding StartTimer.
            Debug.LogWarning($"PerformanceMetricsSystem: Attempted to end timer '{key}' but it was not started or already ended.");
            return -1f;
        }
    }

    // --- Internal Metric Collection and Reporting ---

    /// <summary>
    /// Updates built-in system metrics like average FPS and memory usage.
    /// This is called periodically based on the 'updateInterval'.
    /// </summary>
    private void UpdateSystemMetrics()
    {
        if (!enableMetrics) return;

        // Calculate average FPS from the collected samples.
        float averageFPS = _fpsSamples.Count > 0 ? _fpsSamples.Average() : 0f;
        SetGauge("AverageFPS", averageFPS);
        SetGauge("CurrentFPS", 1.0f / _smoothedDeltaTime); // Set instantaneous FPS (smoothed)

        // Get current total allocated memory from the garbage collector.
        // Passing 'false' prevents a forced garbage collection, which could cause a performance spike.
        long totalMemoryBytes = System.GC.GetTotalMemory(false);
        float totalMemoryMB = totalMemoryBytes / (1024f * 1024f); // Convert bytes to megabytes.
        SetGauge("MemoryUsageMB", totalMemoryMB);

        // You can add more system metrics here, such as:
        // - Unity's profiler statistics (requires using UnityEditor.Profiler, only in editor).
        // - Custom counters for physics updates, AI calculations, render loop phases, etc.
    }

    /// <summary>
    /// Logs all current metrics to the Unity console.
    /// This is called periodically if 'logToConsole' is true.
    /// </summary>
    private void LogMetricsToConsole()
    {
        _stringBuilder.Clear();
        _stringBuilder.AppendLine("--- Performance Metrics Report ---");
        _stringBuilder.AppendLine($"Timestamp: {System.DateTime.Now:HH:mm:ss}"); // Add a timestamp to logs.

        _stringBuilder.AppendLine("\n--- Gauges ---");
        foreach (var pair in _gauges)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value:F2}");
        }

        _stringBuilder.AppendLine("\n--- Counters ---");
        foreach (var pair in _counters)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value}");
        }

        _stringBuilder.AppendLine("\n--- Timers (Last Duration) ---");
        foreach (var pair in _completedTimers)
        {
            _stringBuilder.AppendLine($"- {pair.Key}: {pair.Value:F4}s");
        }

        if (_activeTimers.Count > 0)
        {
            _stringBuilder.AppendLine("\n--- Timers (Active) ---");
            foreach (var pair in _activeTimers)
            {
                _stringBuilder.AppendLine($"- {pair.Key}: {(Time.realtimeSinceStartup - pair.Value):F4}s (running)");
            }
        }

        Debug.Log(_stringBuilder.ToString()); // Output the compiled string to the console.
    }

    // --- Example Usage (Demonstration Purposes) ---
    // The following private fields and methods simulate game events
    // that would typically occur in other game scripts.
    // They are included here to make the example immediately runnable and illustrative.

    private int _exampleEnemyCount = 0;
    private float _examplePlayerHealth = 100f;
    private bool _isPerformingHeavyOperation = false;
    private float _heavyOperationStartTime = 0f;

    // Called once when the script instance is being loaded.
    private void Start()
    {
        // Example: Initialize a custom gauge for player health.
        // This would usually be called from a Player script or GameManager.
        SetGauge("PlayerHealth", _examplePlayerHealth);
        Debug.Log("PerformanceMetricsSystem is active. Press 'M' to toggle overlay.");
    }

    // This method simulates various game events that trigger metric recording.
    // In a real project, these calls would be distributed across various game logic scripts.
    public void SimulateGameEvents()
    {
        if (!enableMetrics) return;

        // Simulate an enemy being killed (increments a counter).
        if (Random.value < 0.05f) // 5% chance to simulate an enemy kill.
        {
            _exampleEnemyCount++;
            RecordCounter("EnemiesKilled");
            // Debug.Log($"Simulated: Enemy killed. Total: {_exampleEnemyCount}");
        }

        // Simulate player taking damage or healing (updates a gauge).
        if (Random.value < 0.03f) // 3% chance to change player health.
        {
            _examplePlayerHealth += Random.Range(-20f, 10f); // Damage or heal.
            _examplePlayerHealth = Mathf.Clamp(_examplePlayerHealth, 0f, 100f); // Keep within bounds.
            SetGauge("PlayerHealth", _examplePlayerHealth);
            // Debug.Log($"Simulated: Player health changed to {_examplePlayerHealth:F1}");
        }

        // Simulate a "heavy operation" (e.g., loading assets, complex pathfinding, AI calculation)
        // and time its duration using StartTimer and EndTimer.
        if (!_isPerformingHeavyOperation && Random.value < 0.01f) // 1% chance to start a heavy op.
        {
            _isPerformingHeavyOperation = true;
            _heavyOperationStartTime = Time.realtimeSinceStartup;
            StartTimer("HeavyOperationDuration");
            Debug.Log("Simulated: Starting heavy operation...");
        }

        if (_isPerformingHeavyOperation && Time.realtimeSinceStartup - _heavyOperationStartTime > Random.Range(1.0f, 3.0f)) // Operation lasts 1-3 seconds.
        {
            float duration = EndTimer("HeavyOperationDuration");
            Debug.Log($"Simulated: Heavy operation finished in {duration:F4}s");
            _isPerformingHeavyOperation = false;
        }

        // Example: Update another custom gauge, e.g., current number of active abilities.
        SetGauge("ActiveAbilities", Random.Range(0, 5));
    }

    // To ensure consistent simulation updates, we call SimulateGameEvents from FixedUpdate.
    // This runs on a fixed time step, independent of frame rate.
    // In a real project, game logic would call the static methods from their respective Update/FixedUpdate loops.
    private float _lastSimulateTime = 0f;
    private float _simulateInterval = 0.1f; // Simulate events 10 times per second.

    private void FixedUpdate()
    {
        if (Time.time - _lastSimulateTime >= _simulateInterval)
        {
            SimulateGameEvents();
            _lastSimulateTime = Time.time;
        }
    }
}
```