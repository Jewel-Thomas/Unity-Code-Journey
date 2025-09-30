// Unity Design Pattern Example: WeatherSystem
// This script demonstrates the WeatherSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'WeatherSystem' design pattern, while not one of the classic Gang of Four patterns, is a common architectural component in game development. It refers to a centralized system responsible for managing the game's weather state, transitioning between different weather conditions, and propagating these changes to other game systems (visuals, audio, gameplay).

This example demonstrates a practical Unity implementation of a WeatherSystem using:
*   **ScriptableObjects:** To define various weather types and their properties in a data-driven way.
*   **Singleton Pattern:** For easy, global access to the weather system manager.
*   **Event-Driven Communication:** Using C# events (`Action<T>`) to notify other game components about weather changes, promoting loose coupling.
*   **Coroutines:** For smooth, gradual transitions between weather states.
*   **Unity's RenderSettings:** To control global lighting, ambient color, fog, and skybox.
*   **Particle Systems & Audio Sources:** To handle visual precipitation and ambient sounds.

---

### **Step 1: Create the `WeatherCondition` Enum**
This enum defines the unique identifiers for your weather types.

**File: `Assets/Scripts/WeatherSystem/WeatherCondition.cs`**
```csharp
// WeatherCondition.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the various weather conditions supported by the WeatherSystem.
/// This enum allows for clear identification and selection of weather types.
/// </summary>
public enum WeatherCondition
{
    Sunny,
    Cloudy,
    Rainy,
    Snowy,
    Stormy,
    // Add more weather conditions as needed
}
```

### **Step 2: Create the `WeatherType` ScriptableObject**
This `ScriptableObject` acts as a data container for each specific weather condition. You'll create assets based on this in the Unity Editor.

**File: `Assets/Scripts/WeatherSystem/WeatherType.cs`**
```csharp
// WeatherType.cs
using UnityEngine;
using System.Collections; // Not strictly needed here, but good practice for Unity scripts

/// <summary>
/// A ScriptableObject representing a single weather type.
/// It holds all the properties defining a specific weather condition,
/// such as visual settings (skybox, light, fog), audio, and particle effects.
/// This makes the weather system data-driven and easily extensible without
/// modifying core code.
/// </summary>
[CreateAssetMenu(fileName = "NewWeatherType", menuName = "Weather System/Weather Type")]
public class WeatherType : ScriptableObject
{
    [Header("Basic Settings")]
    [Tooltip("The unique identifier for this weather condition.")]
    public WeatherCondition condition;

    [Tooltip("A display name for this weather type (e.g., 'Clear Skies', 'Heavy Rain').")]
    public string displayName;

    [Header("Visual Settings")]
    [Tooltip("The skybox material to display during this weather.")]
    public Material skyboxMaterial;

    [Tooltip("The ambient light color for this weather. HDR enabled.")]
    [ColorUsage(false, true)] // Allows for HDR colors in the inspector
    public Color ambientLightColor = Color.white;

    [Tooltip("The fog color for this weather.")]
    public Color fogColor = Color.gray;

    [Tooltip("The fog density for this weather (for exponential fog mode).")]
    [Range(0.0f, 1.0f)]
    public float fogDensity = 0.01f;

    [Tooltip("The intensity of the main directional light during this weather.")]
    [Range(0.0f, 5.0f)]
    public float sunIntensity = 1.0f;

    [Header("Particle Effects")]
    [Tooltip("Should rain particles be active during this weather?")]
    public bool enableRainParticles = false;

    [Tooltip("Should snow particles be active during this weather?")]
    public bool enableSnowParticles = false;

    [Header("Audio Settings")]
    [Tooltip("The ambient sound loop to play during this weather (e.g., rain sounds, wind).")]
    public AudioClip weatherSoundLoop;

    [Tooltip("The volume for the ambient weather sound.")]
    [Range(0.0f, 1.0f)]
    public float weatherSoundVolume = 0.5f;

    [Header("Gameplay Modifiers (Example)")]
    [Tooltip("Example: A factor affecting player movement speed (e.g., 0.8 for slippery ground).")]
    public float movementSpeedFactor = 1.0f;

    // You can add more properties here to customize different aspects of your game:
    // - Wind strength/direction
    // - AI behavior modifiers
    // - Environmental damage over time
    // - Post-processing profiles
    // - Light color of directional light
}
```

### **Step 3: Create the `WeatherSystem` MonoBehaviour**
This is the core manager script. It's a singleton, manages transitions, and dispatches events.

**File: `Assets/Scripts/WeatherSystem/WeatherSystem.cs`**
```csharp
// WeatherSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List or Dictionary if needed
using System; // For Action event delegates

/// <summary>
/// The central Weather System manager.
/// This MonoBehaviour implements the Singleton pattern, providing global access
/// to weather information and control. It handles weather transitions, applies
/// global visual (lighting, fog, skybox) and audio effects, and notifies
/// other game systems when the weather changes.
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    public static WeatherSystem Instance { get; private set; }

    // --- Editor-Configurable Properties ---
    [Header("Weather Configuration")]
    [Tooltip("List of all available WeatherType ScriptableObjects.")]
    [SerializeField] private WeatherType[] _availableWeatherTypes;
    [Tooltip("The weather condition to start with when the scene loads.")]
    [SerializeField] private WeatherCondition _initialWeatherCondition = WeatherCondition.Sunny;
    [Tooltip("How long (in seconds) it takes to smoothly transition between weather types.")]
    [SerializeField] private float _weatherTransitionDuration = 5.0f;

    [Header("Global Effect References")]
    [Tooltip("Reference to the particle system for rain effects in the scene.")]
    [SerializeField] private ParticleSystem _rainParticleSystem;
    [Tooltip("Reference to the particle system for snow effects in the scene.")]
    [SerializeField] private ParticleSystem _snowParticleSystem;
    [Tooltip("AudioSource to play ambient weather sounds.")]
    [SerializeField] private AudioSource _weatherAudioSource;
    [Tooltip("The main directional light in the scene (usually represents the sun/moon).")]
    [SerializeField] private Light _directionalLight;

    // --- Public Properties & Events ---
    /// <summary>
    /// The currently active weather type. Other scripts can read this.
    /// </summary>
    public WeatherType CurrentWeather { get; private set; }

    /// <summary>
    /// Event fired when the weather changes. Subscribers receive the new WeatherType.
    /// This is crucial for other game objects to react to weather changes (Observer pattern).
    /// Example: WeatherSystem.Instance.OnWeatherChanged += MyMethodToReactToWeather;
    /// </summary>
    public event Action<WeatherType> OnWeatherChanged;

    // --- Internal State ---
    private Coroutine _transitionCoroutine;
    private Dictionary<WeatherCondition, WeatherType> _weatherTypeMap; // For quick lookup

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WeatherSystem instances found. Destroying duplicate.", this);
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject); // Optional: Persist across scene loads
        }

        // Initialize weather type map for efficient lookup
        _weatherTypeMap = new Dictionary<WeatherCondition, WeatherType>();
        foreach (WeatherType wt in _availableWeatherTypes)
        {
            if (_weatherTypeMap.ContainsKey(wt.condition))
            {
                Debug.LogWarning($"WeatherSystem: Duplicate WeatherCondition '{wt.condition}' found in available weather types. Only the first will be used.", wt);
            }
            else
            {
                _weatherTypeMap.Add(wt.condition, wt);
            }
        }
    }

    private void Start()
    {
        // Ensure required references are set
        if (_rainParticleSystem == null) Debug.LogWarning("WeatherSystem: Rain Particle System not assigned!", this);
        if (_snowParticleSystem == null) Debug.LogWarning("WeatherSystem: Snow Particle System not assigned!", this);
        if (_weatherAudioSource == null) Debug.LogWarning("WeatherSystem: Weather Audio Source not assigned!", this);
        if (_directionalLight == null) Debug.LogWarning("WeatherSystem: Directional Light not assigned!", this);

        // Set initial weather state
        SetWeather(_initialWeatherCondition, true); // True for instant change at start
    }

    // --- Public API ---
    /// <summary>
    /// Changes the current weather to the specified condition.
    /// </summary>
    /// <param name="condition">The target weather condition.</param>
    /// <param name="instant">If true, the change happens immediately without transition.</param>
    public void SetWeather(WeatherCondition condition, bool instant = false)
    {
        if (!_weatherTypeMap.TryGetValue(condition, out WeatherType newWeather))
        {
            Debug.LogError($"WeatherSystem: WeatherType for condition '{condition}' not found!", this);
            return;
        }

        if (newWeather == CurrentWeather)
        {
            Debug.Log($"WeatherSystem: Already in {newWeather.displayName} state.", this);
            return;
        }

        Debug.Log($"WeatherSystem: Changing weather to {newWeather.displayName}...");

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine); // Stop any ongoing transition
        }

        if (instant)
        {
            ApplyWeatherImmediately(newWeather);
        }
        else
        {
            _transitionCoroutine = StartCoroutine(TransitionToWeatherRoutine(newWeather, _weatherTransitionDuration));
        }
    }

    /// <summary>
    /// Gets the WeatherType ScriptableObject for a given WeatherCondition.
    /// Useful if other systems need specific data from a weather type.
    /// </summary>
    /// <param name="condition">The weather condition to look up.</param>
    /// <returns>The corresponding WeatherType ScriptableObject, or null if not found.</returns>
    public WeatherType GetWeatherType(WeatherCondition condition)
    {
        _weatherTypeMap.TryGetValue(condition, out WeatherType weather);
        return weather;
    }

    // --- Private Helper Methods ---
    /// <summary>
    /// Coroutine to smoothly transition all weather-related properties over time.
    /// </summary>
    private IEnumerator TransitionToWeatherRoutine(WeatherType newWeather, float duration)
    {
        // Capture initial state for interpolation
        WeatherType oldWeather = CurrentWeather;

        Color startAmbient = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;
        Material startSkybox = RenderSettings.skybox;
        float startSunIntensity = _directionalLight ? _directionalLight.intensity : 1f;
        float startAudioVolume = _weatherAudioSource ? _weatherAudioSource.volume : 0f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            // Easing function for smoother transitions (optional, can use Lerp directly)
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Interpolate RenderSettings
            RenderSettings.ambientLight = Color.Lerp(startAmbient, newWeather.ambientLightColor, easedProgress);
            RenderSettings.fogColor = Color.Lerp(startFogColor, newWeather.fogColor, easedProgress);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, newWeather.fogDensity, easedProgress);

            // Interpolate directional light intensity
            if (_directionalLight != null)
            {
                _directionalLight.intensity = Mathf.Lerp(startSunIntensity, newWeather.sunIntensity, easedProgress);
            }

            // Interpolate audio volume and crossfade if sound changes
            if (_weatherAudioSource != null)
            {
                if (oldWeather != null && newWeather.weatherSoundLoop != oldWeather.weatherSoundLoop)
                {
                    // If weather sound is changing, fade out old and fade in new
                    float fadeOutVolume = Mathf.Lerp(startAudioVolume, 0f, easedProgress);
                    _weatherAudioSource.volume = fadeOutVolume;
                    // If halfway, switch audio clip. This is a simple crossfade.
                    // For more advanced, use two audio sources.
                    if (progress >= 0.5f && _weatherAudioSource.clip != newWeather.weatherSoundLoop)
                    {
                        _weatherAudioSource.clip = newWeather.weatherSoundLoop;
                        _weatherAudioSource.Play();
                    }
                    else if (progress < 0.5f && _weatherAudioSource.clip != oldWeather.weatherSoundLoop)
                    {
                        // Ensure old clip plays during fade out if it was different
                        _weatherAudioSource.clip = oldWeather.weatherSoundLoop;
                        _weatherAudioSource.Play();
                    }
                }
                // If same sound or new sound, just interpolate to target volume
                _weatherAudioSource.volume = Mathf.Lerp(startAudioVolume, newWeather.weatherSoundVolume, easedProgress);
            }

            // Skybox transition (simple blend - more complex requires custom shader)
            // For now, it will instantly snap at the end, or you can use a skybox blend shader.
            // A simple approach is to swap at a certain progress point, or at the end.
            if (newWeather.skyboxMaterial != startSkybox)
            {
                if (progress >= 0.5f) // Swap skybox halfway through for a simple blend effect
                {
                    RenderSettings.skybox = newWeather.skyboxMaterial;
                }
            }

            yield return null;
        }

        // Ensure final state is exactly the target state
        ApplyWeatherImmediately(newWeather);
        _transitionCoroutine = null;
    }

    /// <summary>
    /// Applies all weather-related properties instantly to the target weather type.
    /// </summary>
    /// <param name="weather">The WeatherType to apply.</param>
    private void ApplyWeatherImmediately(WeatherType weather)
    {
        CurrentWeather = weather;

        // Apply RenderSettings
        RenderSettings.skybox = weather.skyboxMaterial;
        RenderSettings.ambientLight = weather.ambientLightColor;
        RenderSettings.fogColor = weather.fogColor;
        RenderSettings.fogDensity = weather.fogDensity;
        RenderSettings.fog = true; // Ensure fog is always enabled if fog values are set

        // Apply directional light intensity
        if (_directionalLight != null)
        {
            _directionalLight.intensity = weather.sunIntensity;
        }

        // Activate/deactivate particle systems
        UpdateParticleSystems(weather);

        // Play/stop ambient weather sound
        if (_weatherAudioSource != null)
        {
            if (weather.weatherSoundLoop != null)
            {
                if (_weatherAudioSource.clip != weather.weatherSoundLoop)
                {
                    _weatherAudioSource.clip = weather.weatherSoundLoop;
                    _weatherAudioSource.Play();
                }
                _weatherAudioSource.volume = weather.weatherSoundVolume;
                _weatherAudioSource.loop = true;
            }
            else
            {
                _weatherAudioSource.Stop();
                _weatherAudioSource.clip = null;
            }
        }

        // Notify subscribers that the weather has changed
        OnWeatherChanged?.Invoke(CurrentWeather);

        Debug.Log($"WeatherSystem: Weather now set to {CurrentWeather.displayName} (instant).");
    }

    /// <summary>
    /// Enables/disables global particle systems based on the current weather type.
    /// </summary>
    private void UpdateParticleSystems(WeatherType weather)
    {
        if (_rainParticleSystem != null)
        {
            if (weather.enableRainParticles && !_rainParticleSystem.isPlaying)
            {
                _rainParticleSystem.Play();
            }
            else if (!weather.enableRainParticles && _rainParticleSystem.isPlaying)
            {
                _rainParticleSystem.Stop();
                _rainParticleSystem.Clear(); // Clear existing particles
            }
        }

        if (_snowParticleSystem != null)
        {
            if (weather.enableSnowParticles && !_snowParticleSystem.isPlaying)
            {
                _snowParticleSystem.Play();
            }
            else if (!weather.enableSnowParticles && _snowParticleSystem.isPlaying)
            {
                _snowParticleSystem.Stop();
                _snowParticleSystem.Clear(); // Clear existing particles
            }
        }
    }
}
```

### **Step 4: Example Usage - A Weather Dependent Object**
This script demonstrates how other game objects can react to weather changes by subscribing to the `OnWeatherChanged` event.

**File: `Assets/Scripts/WeatherSystem/WeatherDependentObject.cs`**
```csharp
// WeatherDependentObject.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// An example script demonstrating how other game objects can react to weather changes.
/// It subscribes to the WeatherSystem's OnWeatherChanged event.
/// </summary>
public class WeatherDependentObject : MonoBehaviour
{
    [SerializeField] private Renderer _myRenderer;
    [SerializeField] private Material _normalMaterial;
    [SerializeField] private Material _wetMaterial;
    [SerializeField] private Material _snowyMaterial;

    [Tooltip("Text to display current weather effect. Requires a TextMeshPro or UI Text component.")]
    [SerializeField] private TMPro.TextMeshProUGUI _weatherEffectText; // Example for UI display

    void Awake()
    {
        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        // Ensure WeatherSystem exists before subscribing
        if (WeatherSystem.Instance != null)
        {
            WeatherSystem.Instance.OnWeatherChanged += HandleWeatherChange;
            // Immediately apply current weather if already initialized
            if (WeatherSystem.Instance.CurrentWeather != null)
            {
                HandleWeatherChange(WeatherSystem.Instance.CurrentWeather);
            }
        }
        else
        {
            Debug.LogError("WeatherSystem.Instance is null. Is WeatherSystem in the scene and initialized?", this);
        }
    }

    void OnDisable()
    {
        if (WeatherSystem.Instance != null)
        {
            WeatherSystem.Instance.OnWeatherChanged -= HandleWeatherChange;
        }
    }

    /// <summary>
    /// This method is called by the WeatherSystem when the weather changes.
    /// It demonstrates reacting to the new weather condition.
    /// </summary>
    /// <param name="newWeather">The WeatherType that is now active.</param>
    private void HandleWeatherChange(WeatherType newWeather)
    {
        Debug.Log($"WeatherDependentObject '{gameObject.name}' reacting to new weather: {newWeather.displayName}");

        // Example: Change material based on weather
        if (_myRenderer != null)
        {
            if (newWeather.condition == WeatherCondition.Rainy || newWeather.condition == WeatherCondition.Stormy)
            {
                _myRenderer.material = _wetMaterial;
            }
            else if (newWeather.condition == WeatherCondition.Snowy)
            {
                _myRenderer.material = _snowyMaterial;
            }
            else
            {
                _myRenderer.material = _normalMaterial;
            }
        }

        // Example: Update UI text with weather effects
        if (_weatherEffectText != null)
        {
            _weatherEffectText.text = $"Current Weather: {newWeather.displayName}\n" +
                                      $"Movement Factor: {newWeather.movementSpeedFactor:F1}";
        }

        // You could also:
        // - Adjust AI behavior
        // - Enable/disable specific sound effects
        // - Apply buffs/debuffs to the player
        // - Change light colors of local lights
        // - Play specific animations
    }
}
```

### **Step 5: Example Usage - A Weather Trigger**
This script shows how to programmatically change the weather, for instance, when a player enters a specific area.

**File: `Assets/Scripts/WeatherSystem/WeatherTrigger.cs`**
```csharp
// WeatherTrigger.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// An example script that triggers a weather change when a player enters its collider.
/// This demonstrates how other parts of the game can interact with the WeatherSystem.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure there's a collider to trigger
public class WeatherTrigger : MonoBehaviour
{
    [Tooltip("The weather condition this trigger will set.")]
    [SerializeField] private WeatherCondition _targetWeather = WeatherCondition.Rainy;
    [Tooltip("If true, the weather changes instantly; otherwise, it transitions smoothly.")]
    [SerializeField] private bool _instantChange = false;
    [Tooltip("The tag of the GameObject that can trigger the weather change (e.g., 'Player').")]
    [SerializeField] private string _triggerTag = "Player";

    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            _collider.isTrigger = true; // Ensure it's a trigger
        }
        else
        {
            Debug.LogError("WeatherTrigger requires a Collider component.", this);
            enabled = false; // Disable if no collider
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_triggerTag))
        {
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.SetWeather(_targetWeather, _instantChange);
            }
            else
            {
                Debug.LogError("WeatherSystem.Instance is null. Cannot trigger weather change.", this);
            }
        }
    }
}
```

---

### **How to Set Up in Unity Editor:**

1.  **Create Folders:**
    *   `Assets/Scripts/WeatherSystem`
    *   `Assets/Materials/Skyboxes` (or reuse existing skyboxes)
    *   `Assets/Audio/Weather` (for ambient sounds)
    *   `Assets/Particles/Weather` (for rain/snow particle systems)

2.  **Create WeatherType ScriptableObjects:**
    *   In the Project window, right-click -> `Create` -> `Weather System` -> `Weather Type`.
    *   Create several (e.g., `WT_Sunny`, `WT_Cloudy`, `WT_Rainy`, `WT_Snowy`).
    *   **Configure each:**
        *   Assign a `WeatherCondition` enum value.
        *   Set `displayName`.
        *   Assign `Skybox Material` (e.g., a simple clear skybox for Sunny).
        *   Adjust `Ambient Light Color`, `Fog Color`, `Fog Density`, `Sun Intensity`.
        *   For `WT_Rainy`: set `enableRainParticles = true`, assign `Weather Sound Loop` (rain sound).
        *   For `WT_Snowy`: set `enableSnowParticles = true`, assign `Weather Sound Loop` (wind/snow sound).
        *   For `WT_Stormy`: set `enableRainParticles = true`, maybe a darker `Ambient Light Color`, higher `Fog Density`, lower `Sun Intensity`, and a stormy sound.

3.  **Create Particle Systems:**
    *   Create two empty GameObjects: `GlobalRainParticles` and `GlobalSnowParticles`.
    *   Add a `Particle System` component to each. Configure them appropriately (e.g., `GlobalRainParticles` with falling particles, `GlobalSnowParticles` with softer, slower falling particles). Make sure their `Looping` property is `true`.
    *   Position them appropriately in the scene (e.g., high above the player's typical location).

4.  **Create an Audio Source:**
    *   Create an empty GameObject: `WeatherAudioSource`.
    *   Add an `Audio Source` component to it. Set `Loop` to true, `Play On Awake` to false (the WeatherSystem will manage it).
    *   **Crucially, set `Spatial Blend` to 0 (2D) for ambient sounds that aren't positional.**

5.  **Create a Directional Light:**
    *   Ensure you have a `Directional Light` in your scene (usually named "Directional Light"). This is typically your sun/moon.

6.  **Set up the `WeatherSystem` GameObject:**
    *   Create an empty GameObject in your scene named `_WeatherSystem`.
    *   Add the `WeatherSystem.cs` script to it.
    *   **Drag your created `WeatherType` ScriptableObjects into the `Available Weather Types` array.**
    *   **Drag your `GlobalRainParticles`, `GlobalSnowParticles`, `WeatherAudioSource`, and `Directional Light` GameObjects/components into their respective slots.**
    *   Set an `Initial Weather Condition`.
    *   Adjust `Weather Transition Duration`.

7.  **Set up `WeatherDependentObject` (Example):**
    *   Create a 3D object (e.g., a Cube) in your scene.
    *   Create three materials: `Mat_Normal`, `Mat_Wet` (shiny/darker), `Mat_Snowy` (white/textured).
    *   Add the `WeatherDependentObject.cs` script to the Cube.
    *   Drag your three materials into the `Normal Material`, `Wet Material`, and `Snowy Material` slots.
    *   (Optional) If you have TextMeshPro installed, create a UI TextMeshPro element and drag it into the `Weather Effect Text` slot to see real-time updates.

8.  **Set up `WeatherTrigger` (Example):**
    *   Create an empty GameObject, name it `RainTrigger`.
    *   Add a `Box Collider` component to it. Make sure `Is Trigger` is checked.
    *   Scale the collider to form an area (e.g., a "rainy zone").
    *   Add the `WeatherTrigger.cs` script to `RainTrigger`.
    *   Set `Target Weather` to `Rainy`.
    *   Ensure your player GameObject has a `Collider` and a `Rigidbody` (even kinematic) and its `Tag` is set to "Player".
    *   Repeat for other weather conditions (e.g., `SnowTrigger` for `Snowy`).

**Play the Scene:**
You should see the weather start with your `Initial Weather Condition`. If you walk your player into a `WeatherTrigger`, the weather will smoothly transition to the new state, affecting the skybox, lighting, fog, particle systems, audio, and your `WeatherDependentObject`'s material.