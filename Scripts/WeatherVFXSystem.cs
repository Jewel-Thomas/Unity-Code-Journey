// Unity Design Pattern Example: WeatherVFXSystem
// This script demonstrates the WeatherVFXSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'WeatherVFXSystem' design pattern in Unity provides a centralized and modular way to manage visual effects (VFX) associated with different weather conditions. It encapsulates the logic for switching between weather states, activating/deactivating their corresponding VFX, and handling smooth transitions. This makes it easy to add new weather types, update VFX, and control the global weather state from anywhere in your application.

### WeatherVFXSystem Pattern Breakdown:

1.  **Weather States (Enum)**: Defines the distinct weather conditions (e.g., Clear, Rain, Snow, Fog).
2.  **VFX Data (Serializable Class)**: Associates each weather state with its specific VFX asset (e.g., a `ParticleSystem` prefab), and potentially transition parameters.
3.  **VFX Manager (Singleton MonoBehaviour)**:
    *   Holds a collection of `VFX Data` configurations.
    *   Provides a public API to change the current weather.
    *   Manages the instantiation, activation, deactivation, and destruction of VFX game objects.
    *   Handles smooth transitions between different weather VFX.
    *   Often implemented as a singleton for easy global access.

### Advantages:

*   **Decoupling**: Weather logic is separated from VFX implementation.
*   **Modularity**: Easy to add or modify weather types and their VFX without altering core logic.
*   **Centralized Control**: A single point of control for the global weather state.
*   **Reusability**: The system can be reused across multiple scenes or projects.
*   **Performance**: Ensures only relevant VFX are active, potentially improving performance compared to having all VFX systems present and toggled.

---

Here's a complete C# Unity example demonstrating the WeatherVFXSystem pattern:

You'll need three C# scripts:
1.  `WeatherType.cs` (an enum)
2.  `WeatherVFXData.cs` (a serializable class)
3.  `WeatherVFXSystem.cs` (the core manager MonoBehaviour)

### 1. `WeatherType.cs`

This script defines an enumeration for various weather conditions.

```csharp
using UnityEngine;

/// <summary>
/// Defines the different types of weather conditions supported by the system.
/// This enum can be expanded to include more specific weather types as needed.
/// </summary>
public enum WeatherType
{
    /// <summary>No active weather effects, typically clear skies.</summary>
    Clear,
    /// <summary>Light rain effects.</summary>
    LightRain,
    /// <summary>Heavy rain effects with more intensity.</summary>
    HeavyRain,
    /// <summary>Snowfall effects.</summary>
    Snow,
    /// <summary>Fog effects, reducing visibility.</summary>
    Fog,
    /// <summary>A stormy weather condition, potentially with rain and thunder effects.</summary>
    Storm,
    /// <summary>Custom weather, to be defined with unique VFX assets.</summary>
    Custom
}
```

### 2. `WeatherVFXData.cs`

This script defines a serializable class to hold configuration for each weather type, linking it to a specific VFX prefab and a transition duration.

```csharp
using UnityEngine;
using System; // Required for [Serializable]

/// <summary>
/// Represents the data structure for a single weather VFX configuration.
/// This class links a specific WeatherType to its associated GameObject prefab
/// (which should contain the ParticleSystem(s) or other visual effects)
/// and defines how long the transition to/from this weather should take.
/// </summary>
[Serializable]
public class WeatherVFXData
{
    [Tooltip("The type of weather this configuration applies to.")]
    public WeatherType weatherType;

    [Tooltip("The GameObject prefab containing the VFX (e.g., Particle System) for this weather type. " +
             "Leave empty for weather types that have no visual effects (e.g., Clear).")]
    public GameObject vfxPrefab;

    [Tooltip("The duration in seconds for fading in/out the VFX during weather transitions.")]
    [Range(0.1f, 10f)]
    public float transitionDuration = 2.0f;
}
```

### 3. `WeatherVFXSystem.cs`

This is the core manager script. It's a singleton that handles switching between weather conditions and managing their visual effects.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List and Dictionary

/// <summary>
/// The WeatherVFXSystem is a core component that manages and orchestrates
/// visual effects (VFX) based on the current weather state.
/// It implements the WeatherVFXSystem design pattern by centralizing
/// weather state management, VFX association, and smooth transitions.
/// </summary>
/// <remarks>
/// This system operates as a Singleton, meaning there should only be one instance
/// in the scene at any given time, providing a global access point for
/// setting the weather.
/// </remarks>
public class WeatherVFXSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    public static WeatherVFXSystem Instance { get; private set; }

    // --- Editor-Configurable Fields ---
    [Tooltip("List of weather type configurations, linking WeatherType to its VFX prefab and transition duration.")]
    [SerializeField]
    private List<WeatherVFXData> weatherVFXSettings = new List<WeatherVFXData>();

    [Tooltip("The Transform that will act as a parent for all instantiated weather VFX GameObjects. " +
             "It's recommended to place this at the camera's position or (0,0,0) for world-based effects.")]
    [SerializeField]
    private Transform vfxParentTransform;

    // --- Internal State Variables ---
    private WeatherType _currentWeather = WeatherType.Clear;
    /// <summary>
    /// Gets the current active weather type.
    /// </summary>
    public WeatherType CurrentWeather => _currentWeather;

    private GameObject _currentActiveVFXInstance; // The currently active instantiated VFX GameObject
    private Coroutine _transitionCoroutine;      // Reference to the active transition coroutine

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton instance and sets up the VFX parent transform.
    /// </summary>
    void Awake()
    {
        // Enforce Singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WeatherVFXSystem: Duplicate instance detected, destroying this one. " +
                             "Only one WeatherVFXSystem should exist in the scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure a parent transform exists for VFX. If not assigned in Inspector, create a default one.
        if (vfxParentTransform == null)
        {
            GameObject vfxParentGO = new GameObject("WeatherVFX_Container");
            vfxParentTransform = vfxParentGO.transform;
            // Optionally, make it a child of this manager for organization
            vfxParentTransform.SetParent(this.transform);
            vfxParentTransform.localPosition = Vector3.zero;
            vfxParentTransform.localRotation = Quaternion.identity;
            Debug.LogWarning("WeatherVFXSystem: 'VFX Parent Transform' was not assigned. " +
                             "Created a default 'WeatherVFX_Container' GameObject as a child of this manager. " +
                             "Consider assigning one manually for better control.");
        }
    }

    /// <summary>
    /// Called once after Awake. Sets initial weather to Clear if not explicitly set.
    /// </summary>
    void Start()
    {
        // Ensure some weather is active on start, defaulting to Clear.
        // This implicitly calls SetWeather(Clear) which handles initial setup.
        SetWeather(_currentWeather, true); 
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Clears the singleton instance.
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public API ---

    /// <summary>
    /// Initiates a change to a new weather condition.
    /// This is the primary method to call from other scripts to change the weather.
    /// </summary>
    /// <param name="newWeatherType">The <see cref="WeatherType"/> to transition to.</param>
    /// <param name="forceTransition">If true, forces the transition even if the new weather is the same as current.
    /// Useful for initial setup or re-applying effects.</param>
    public void SetWeather(WeatherType newWeatherType, bool forceTransition = false)
    {
        if (_currentWeather == newWeatherType && !forceTransition)
        {
            Debug.Log($"WeatherVFXSystem: Weather is already {_currentWeather}. No change needed.");
            return;
        }

        Debug.Log($"WeatherVFXSystem: Changing weather from {_currentWeather} to {newWeatherType}...");

        // Find the VFX data for the new weather type
        WeatherVFXData newVFXData = weatherVFXSettings.Find(data => data.weatherType == newWeatherType);

        if (newVFXData == null && newWeatherType != WeatherType.Clear)
        {
            Debug.LogWarning($"WeatherVFXSystem: No VFX data found for {newWeatherType}. " +
                             "Ensure it's configured in the Inspector. Reverting to Clear.");
            newWeatherType = WeatherType.Clear; // Fallback to clear
            newVFXData = weatherVFXSettings.Find(data => data.weatherType == WeatherType.Clear);
        }
        // If still null, newVFXData will be null, which is handled gracefully by TransitionVFX.

        // Stop any ongoing transition to prevent conflicts
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            Debug.Log("WeatherVFXSystem: Stopped previous weather transition.");
        }

        // Start the new transition
        _transitionCoroutine = StartCoroutine(TransitionVFX(newWeatherType, newVFXData));
    }

    // --- Internal Transition Logic ---

    /// <summary>
    /// Coroutine to handle the smooth transition between the current weather's VFX and the new weather's VFX.
    /// This includes fading out old effects and fading in new ones.
    /// </summary>
    /// <param name="targetWeather">The weather type being transitioned to.</param>
    /// <param name="newVFXData">The <see cref="WeatherVFXData"/> for the target weather.</param>
    private IEnumerator TransitionVFX(WeatherType targetWeather, WeatherVFXData newVFXData)
    {
        // Store old VFX instance for fading out
        GameObject oldVFXInstance = _currentActiveVFXInstance;
        ParticleSystem[] oldParticleSystems = null;
        float fadeDuration = (newVFXData != null) ? newVFXData.transitionDuration : 0.5f;

        if (oldVFXInstance != null)
        {
            oldParticleSystems = oldVFXInstance.GetComponentsInChildren<ParticleSystem>(true); // Include inactive
        }

        // Phase 1: Fade out old VFX
        if (oldParticleSystems != null && oldParticleSystems.Length > 0)
        {
            // Capture initial emission rates
            Dictionary<ParticleSystem, float> initialOldRates = new Dictionary<ParticleSystem, float>();
            foreach (var ps in oldParticleSystems)
            {
                if (ps.emission.enabled)
                {
                    initialOldRates[ps] = ps.emission.rateOverTime.constant;
                }
                else
                {
                    initialOldRates[ps] = 0f; // If not emitting, treat as 0
                }
            }

            float timer = 0f;
            while (timer < fadeDuration)
            {
                float t = 1f - (timer / fadeDuration); // Lerp from 1 to 0
                foreach (var ps in oldParticleSystems)
                {
                    if (ps != null)
                    {
                        var emission = ps.emission;
                        emission.rateOverTime = new ParticleSystem.MinMaxCurve(initialOldRates[ps] * t);
                    }
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // Ensure old systems are fully stopped and then destroy the GameObject
            foreach (var ps in oldParticleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            Destroy(oldVFXInstance);
            _currentActiveVFXInstance = null; // Clear reference
        }
        else if (oldVFXInstance != null)
        {
            // If the old VFX was not a particle system, simply destroy it
            Destroy(oldVFXInstance);
            _currentActiveVFXInstance = null;
        }

        // Phase 2: Instantiate and Fade in new VFX
        GameObject newVFXInstance = null;
        ParticleSystem[] newParticleSystems = null;

        if (newVFXData != null && newVFXData.vfxPrefab != null)
        {
            newVFXInstance = Instantiate(newVFXData.vfxPrefab, vfxParentTransform);
            newVFXInstance.transform.localPosition = Vector3.zero; // Position at parent's origin
            newVFXInstance.transform.localRotation = Quaternion.identity; // No local rotation
            newVFXInstance.SetActive(true); // Ensure it's active for GetComponentsInChildren

            newParticleSystems = newVFXInstance.GetComponentsInChildren<ParticleSystem>(true);

            // Capture target emission rates from the prefab and set initial rates to zero
            Dictionary<ParticleSystem, float> targetNewRates = new Dictionary<ParticleSystem, float>();
            foreach (var ps in newParticleSystems)
            {
                // Store the prefab's intended full emission rate
                targetNewRates[ps] = ps.emission.rateOverTime.constant;
                
                // Set initial emission to zero and start playing (so existing particles can move if configured)
                var emission = ps.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
                ps.Play(true); // Start playing so particles can appear as emission increases
            }

            float timer = 0f;
            while (timer < fadeDuration)
            {
                float t = timer / fadeDuration; // Lerp from 0 to 1
                foreach (var ps in newParticleSystems)
                {
                    if (ps != null)
                    {
                        var emission = ps.emission;
                        emission.rateOverTime = new ParticleSystem.MinMaxCurve(targetNewRates[ps] * t);
                    }
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // Ensure full emission is reached
            foreach (var ps in newParticleSystems)
            {
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(targetNewRates[ps]);
                }
            }
        }
        else if (newVFXData != null && newVFXData.vfxPrefab == null)
        {
            // No VFX prefab for this weather type (e.g., Clear), just ensure no old VFX is active.
            Debug.Log($"WeatherVFXSystem: {targetWeather} has no VFX prefab assigned.");
        }

        // Update the system's state after the transition is complete
        _currentActiveVFXInstance = newVFXInstance;
        _currentWeather = targetWeather;
        _transitionCoroutine = null; // Mark transition as complete
        Debug.Log($"WeatherVFXSystem: Weather successfully transitioned to {_currentWeather}.");
    }
}
```

---

### How to Implement in Unity:

1.  **Create the Scripts**:
    *   Create a C# script named `WeatherType.cs` and copy the code from the first block.
    *   Create a C# script named `WeatherVFXData.cs` and copy the code from the second block.
    *   Create a C# script named `WeatherVFXSystem.cs` and copy the code from the third block.

2.  **Create the Manager GameObject**:
    *   In your Unity scene, create an empty GameObject (Right-click in Hierarchy -> Create Empty).
    *   Rename it to `WeatherVFXManager`.
    *   Attach the `WeatherVFXSystem.cs` script to this `WeatherVFXManager` GameObject.

3.  **Create the VFX Container**:
    *   Create another empty GameObject in your scene (e.g., as a child of `WeatherVFXManager` or at world origin).
    *   Rename it to `WeatherVFXContainer`. This GameObject will be the parent for all instantiated weather VFX prefabs. Its position and rotation will determine where the VFX appears (e.g., place it at your player camera's position for camera-centric effects, or at `Vector3.zero` for world-centric effects like rain hitting the ground).

4.  **Configure the `WeatherVFXSystem` in the Inspector**:
    *   Select `WeatherVFXManager` in the Hierarchy.
    *   In the Inspector, drag the `WeatherVFXContainer` GameObject into the `VFX Parent Transform` slot.
    *   Expand the `Weather VFX Settings` list.
    *   **Add elements for each `WeatherType` you want to support**:
        *   For `Clear` weather, you typically leave the `VFX Prefab` empty.
        *   For `LightRain`, `HeavyRain`, `Snow`, `Fog`, etc., you'll need `ParticleSystem` prefabs (or other VFX prefabs). Unity's Standard Assets (or other asset store packages) often provide good examples. Create or import these prefabs into your project.
        *   Drag your `ParticleSystem` prefabs into the `VFX Prefab` slot for the corresponding `Weather Type`.
        *   Adjust the `Transition Duration` for how long the fade-in/out should take.

    *Example Setup in Inspector:*
    *   `Element 0`
        *   `Weather Type`: `Clear`
        *   `VFX Prefab`: (None)
        *   `Transition Duration`: `2`
    *   `Element 1`
        *   `Weather Type`: `LightRain`
        *   `VFX Prefab`: (Drag your 'LightRain_Particles' Prefab here)
        *   `Transition Duration`: `3`
    *   `Element 2`
        *   `Weather Type`: `Snow`
        *   `VFX Prefab`: (Drag your 'Snowfall_Particles' Prefab here)
        *   `Transition Duration`: `4`
    *   (Add more elements for other weather types)

5.  **Example Usage from Another Script**:

    You can call `SetWeather()` from any other script, a UI button, or an event system.

    ```csharp
    using UnityEngine;
    using System.Collections; // Required for Coroutines

    public class WeatherControllerExample : MonoBehaviour
    {
        [Header("Weather Settings")]
        [Tooltip("Assign a key to trigger a weather change.")]
        public KeyCode changeWeatherKey = KeyCode.Space;
        
        [Tooltip("The weather type to switch to when the key is pressed.")]
        public WeatherType targetWeather = WeatherType.HeavyRain;

        private WeatherType[] _allWeatherTypes;
        private int _currentWeatherIndex = 0;

        void Start()
        {
            _allWeatherTypes = (WeatherType[])System.Enum.GetValues(typeof(WeatherType));
            // Set initial weather on start (WeatherVFXSystem usually does this, but good to know you can call it)
            // WeatherVFXSystem.Instance?.SetWeather(WeatherType.Clear);
        }

        void Update()
        {
            // Example: Change weather with a key press
            if (Input.GetKeyDown(changeWeatherKey))
            {
                // Call the SetWeather method on the singleton instance
                if (WeatherVFXSystem.Instance != null)
                {
                    WeatherVFXSystem.Instance.SetWeather(targetWeather);
                    Debug.Log($"Changing weather to: {targetWeather}");
                }
                else
                {
                    Debug.LogError("WeatherVFXSystem.Instance is null! Is the manager in the scene?");
                }
            }

            // Example: Cycle through all weather types with another key
            if (Input.GetKeyDown(KeyCode.Return))
            {
                _currentWeatherIndex = (_currentWeatherIndex + 1) % _allWeatherTypes.Length;
                WeatherType nextWeather = _allWeatherTypes[_currentWeatherIndex];

                if (WeatherVFXSystem.Instance != null)
                {
                    WeatherVFXSystem.Instance.SetWeather(nextWeather);
                    Debug.Log($"Cycling weather to: {nextWeather}");
                }
            }
        }

        // Example: Method to be called by a UI button
        public void SetWeatherFromUI(int weatherTypeIndex)
        {
            if (weatherTypeIndex >= 0 && weatherTypeIndex < _allWeatherTypes.Length)
            {
                WeatherType selectedWeather = _allWeatherTypes[weatherTypeIndex];
                if (WeatherVFXSystem.Instance != null)
                {
                    WeatherVFXSystem.Instance.SetWeather(selectedWeather);
                    Debug.Log($"Setting weather from UI to: {selectedWeather}");
                }
            }
        }
    }
    ```
    Attach `WeatherControllerExample.cs` to any GameObject in your scene (e.g., your Player or a dedicated 'GameManager'). Run the scene and press the configured keys (e.g., Space or Return) to see the weather VFX change smoothly.

This setup provides a robust, extensible, and easy-to-use weather VFX system for your Unity projects.