// Unity Design Pattern Example: DynamicWeatherSystem
// This script demonstrates the DynamicWeatherSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DynamicWeatherSystem' pattern in Unity typically refers to a system design where the game environment's weather conditions dynamically change over time or in response to game events. It combines several common design patterns like **Singleton**, **Observer**, and aspects of **State** or **Strategy** patterns to create a modular, extensible, and manageable weather system.

Here's a breakdown of the pattern applied to Unity:

1.  **Weather Presets (Strategy/State):**
    *   Each distinct weather condition (e.g., Sunny, Rainy, Snowy, Foggy) is defined as a 'preset'.
    *   In Unity, **ScriptableObjects** are perfect for this. Each `WeatherPreset` ScriptableObject holds all the data defining that weather state (sky tint, ambient light, fog settings, particle effects, sounds, etc.). This makes it easy for designers to create and modify weather types without touching code.

2.  **DynamicWeatherSystem (Singleton & Subject/Notifier):**
    *   A central manager responsible for the current weather state and initiating transitions.
    *   Implemented as a **Singleton** to ensure a single, easily accessible point of control throughout the game.
    *   Acts as the **Subject** in the **Observer pattern**, notifying other parts of the game when the weather changes. It provides an `event` that listeners can subscribe to.

3.  **Weather Effect Appliers (Observers):**
    *   Specialized components (e.g., `WeatherVisualsApplier`, `WeatherAudioApplier`) that subscribe to the `DynamicWeatherSystem`'s weather change event.
    *   When notified, they read the data from the new `WeatherPreset` and smoothly apply the changes to the specific aspects of the game world they are responsible for (e.g., visual effects, lighting, skybox, ambient sounds).
    *   This adheres to the **Single Responsibility Principle**, as the weather system manages state and notification, while appliers handle the actual rendering and audio logic.

4.  **Transition Logic:**
    *   When weather changes, the appliers use **Coroutines** to smoothly interpolate between the old and new weather settings (e.g., fading sky colors, adjusting light intensity, cross-fading audio, activating/deactivating particle systems). This prevents jarring, instant changes.

This setup makes the weather system robust, easy to expand with new weather types or new types of effects, and decouples the weather logic from its visual and audio representation.

---

### Complete Unity C# Example: Dynamic Weather System

This example provides three C# scripts:
1.  `WeatherPreset.cs`: A ScriptableObject to define different weather types.
2.  `DynamicWeatherSystem.cs`: The central manager (Singleton) that orchestrates weather changes and notifies listeners.
3.  `WeatherEffectApplier.cs`: A component that listens to weather changes and applies visual effects (sky, light, fog, particles).
4.  `WeatherSoundApplier.cs`: A component that listens to weather changes and applies ambient sound effects.

---

**1. `WeatherPreset.cs` (ScriptableObject)**

This script defines a blueprint for different weather conditions. Designers can create instances of this ScriptableObject through the Unity Editor to define various weather types like "Sunny," "Rainy," "Snowy," etc.

```csharp
using UnityEngine;

/// <summary>
/// WeatherPreset ScriptableObject
/// Defines all properties for a specific weather condition.
/// This acts as the 'Strategy' or 'State' for our weather system.
/// </summary>
[CreateAssetMenu(fileName = "NewWeatherPreset", menuName = "Dynamic Weather/Weather Preset", order = 1)]
public class WeatherPreset : ScriptableObject
{
    [Header("Basic Settings")]
    public string weatherName = "New Weather";
    [Tooltip("Default duration for transitioning into this weather state.")]
    public float defaultTransitionDuration = 5.0f;

    [Header("Skybox & Ambient Light")]
    [Tooltip("Color to tint the skybox.")]
    public Color skyTint = Color.white;
    [Tooltip("Color of the ambient light.")]
    public Color ambientLightColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Directional Light (Sun/Moon)")]
    [Tooltip("Color of the directional light.")]
    public Color directionalLightColor = Color.white;
    [Tooltip("Intensity of the directional light.")]
    [Range(0f, 2f)] public float directionalLightIntensity = 1.0f;
    [Tooltip("Rotation of the directional light (e.g., for sun position).")]
    public Vector3 directionalLightRotation = new Vector3(50f, -30f, 0f); // Default sun angle

    [Header("Fog Settings")]
    [Tooltip("Should fog be enabled for this weather?")]
    public bool enableFog = false;
    [Tooltip("Color of the fog.")]
    public Color fogColor = Color.grey;
    [Tooltip("Density of the fog (for exponential fog).")]
    [Range(0f, 1f)] public float fogDensity = 0.01f;
    [Tooltip("Start distance for linear fog.")]
    public float fogStartDistance = 0f;
    [Tooltip("End distance for linear fog.")]
    public float fogEndDistance = 300f;

    [Header("Weather Effects (Particles, etc.)")]
    [Tooltip("Prefab containing particle systems or other visual effects for this weather (e.g., rain, snow).")]
    public GameObject weatherEffectPrefab;

    [Header("Ambient Audio")]
    [Tooltip("Audio clip for ambient weather sounds (e.g., rain, wind).")]
    public AudioClip ambientSoundClip;
    [Tooltip("Volume of the ambient sound clip.")]
    [Range(0f, 1f)] public float ambientSoundVolume = 0.5f;

    // You can add more properties here as needed, like:
    // - Wind strength/direction
    // - Cloud settings
    // - Specific skybox material overrides
    // - Effects on player speed, AI behavior, etc.
}
```

---

**2. `DynamicWeatherSystem.cs` (Singleton Manager)**

This is the core manager of the weather system. It's a Singleton, meaning only one instance exists in the game, making it easy for any script to access. It manages the current weather state and notifies other components about changes using an event (`OnWeatherChangeStarted`).

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // Required for List

/// <summary>
/// DynamicWeatherSystem
/// This is the central manager for the weather system, implemented as a Singleton.
/// It holds available weather presets, manages the current weather state,
/// and notifies other components when the weather changes.
///
/// Design Pattern: Singleton, Subject (Observer Pattern)
/// </summary>
public class DynamicWeatherSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    public static DynamicWeatherSystem Instance { get; private set; }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used for initializing the Singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DynamicWeatherSystem: Multiple instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene loads
        }
    }

    // --- Weather System Properties ---

    [Header("Weather Settings")]
    [Tooltip("List of all available weather presets in your game.")]
    [SerializeField] private List<WeatherPreset> availableWeatherPresets;
    [Tooltip("The weather preset to start with when the game begins.")]
    [SerializeField] private WeatherPreset initialWeather;

    /// <summary>
    /// The currently active weather preset.
    /// </summary>
    public WeatherPreset CurrentWeather { get; private set; }

    /// <summary>
    /// Event triggered when a weather change is initiated.
    /// Listeners (Observers) should subscribe to this event to react to weather changes.
    /// Arguments: newWeatherPreset, oldWeatherPreset, transitionDuration.
    /// </summary>
    public event Action<WeatherPreset, WeatherPreset, float> OnWeatherChangeStarted;

    /// <summary>
    /// Start is called before the first frame update.
    /// Initializes the weather system with the initial preset.
    /// </summary>
    private void Start()
    {
        if (initialWeather == null)
        {
            Debug.LogError("DynamicWeatherSystem: No initial weather preset assigned! Please assign one in the inspector.");
            return;
        }

        // Immediately set the initial weather without a transition from 'nothing'
        CurrentWeather = initialWeather;
        // Notify listeners that the initial weather is set. Transition duration is 0.
        OnWeatherChangeStarted?.Invoke(CurrentWeather, null, 0f);
        Debug.Log($"DynamicWeatherSystem: Initial weather set to {CurrentWeather.weatherName}.");
    }

    /// <summary>
    /// Public method to change the current weather.
    /// This is the primary way other scripts interact with the weather system.
    /// </summary>
    /// <param name="newPreset">The WeatherPreset to transition to.</param>
    /// <param name="customTransitionDuration">Optional: Override the preset's default transition duration.</param>
    public void SetWeather(WeatherPreset newPreset, float customTransitionDuration = -1f)
    {
        if (newPreset == null)
        {
            Debug.LogError("DynamicWeatherSystem: Attempted to set null weather preset.");
            return;
        }

        if (newPreset == CurrentWeather)
        {
            Debug.Log($"DynamicWeatherSystem: Weather is already {newPreset.weatherName}. No change needed.");
            return;
        }

        // Store the old weather for listeners
        WeatherPreset oldWeather = CurrentWeather;
        CurrentWeather = newPreset;

        // Determine the actual transition duration
        float effectiveDuration = customTransitionDuration >= 0 ? customTransitionDuration : newPreset.defaultTransitionDuration;

        Debug.Log($"DynamicWeatherSystem: Changing weather from '{oldWeather?.weatherName ?? "None"}' to '{newPreset.weatherName}' over {effectiveDuration:F2} seconds.");

        // Notify all subscribed listeners (Observers) about the impending weather change.
        // The listeners are responsible for implementing the actual visual/audio transitions.
        OnWeatherChangeStarted?.Invoke(CurrentWeather, oldWeather, effectiveDuration);
    }

    /// <summary>
    /// Helper method to change weather by name.
    /// </summary>
    /// <param name="weatherName">The name of the weather preset to transition to.</param>
    /// <param name="customTransitionDuration">Optional: Override the preset's default transition duration.</param>
    public void SetWeatherByName(string weatherName, float customTransitionDuration = -1f)
    {
        WeatherPreset targetPreset = availableWeatherPresets.Find(p => p.weatherName.Equals(weatherName, StringComparison.OrdinalIgnoreCase));
        if (targetPreset != null)
        {
            SetWeather(targetPreset, customTransitionDuration);
        }
        else
        {
            Debug.LogWarning($"DynamicWeatherSystem: Weather preset '{weatherName}' not found in available presets.");
        }
    }
}
```

---

**3. `WeatherEffectApplier.cs` (Visual Observer)**

This component is responsible for applying the visual aspects of the weather preset to the scene. It subscribes to the `DynamicWeatherSystem`'s event and uses coroutines to smoothly transition skybox tint, ambient light, directional light, fog, and manages weather effect prefabs (like particle systems).

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// WeatherEffectApplier
/// This component listens to weather change events from the DynamicWeatherSystem
/// and applies visual effects to the scene (skybox, lighting, fog, particle systems).
///
/// Design Pattern: Observer
/// </summary>
public class WeatherEffectApplier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The main directional light in your scene (e.g., your 'Sun' or 'Moon').")]
    [SerializeField] private Light directionalLight;
    [Tooltip("The main camera in your scene, used for fog settings.")]
    [SerializeField] private Camera mainCamera; // Useful for setting Camera.backgroundColor if needed, or just for fog.

    private GameObject _currentEffectInstance; // Stores the instantiated weather effect prefab
    private Coroutine _currentTransitionCoroutine;

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active.
    /// We subscribe to the weather change event here.
    /// </summary>
    private void OnEnable()
    {
        if (DynamicWeatherSystem.Instance != null)
        {
            DynamicWeatherSystem.Instance.OnWeatherChangeStarted += OnWeatherChange;
        }
        else
        {
            Debug.LogError("WeatherEffectApplier: DynamicWeatherSystem not found! Make sure it's in the scene and initialized.");
        }

        // Find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("WeatherEffectApplier: Main Camera not found. Fog settings might not apply correctly.");
            }
        }
        
        // Find directional light if not assigned
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
            if (directionalLight == null)
            {
                 Debug.LogWarning("WeatherEffectApplier: Directional Light not found. Light settings will not apply.");
            }
            else if (directionalLight.type != LightType.Directional)
            {
                 Debug.LogWarning("WeatherEffectApplier: Found a Light, but it's not directional. Please assign the correct directional light.");
                 directionalLight = null;
            }
        }
    }

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive.
    /// We unsubscribe from the event to prevent memory leaks or errors.
    /// </summary>
    private void OnDisable()
    {
        if (DynamicWeatherSystem.Instance != null)
        {
            DynamicWeatherSystem.Instance.OnWeatherChangeStarted -= OnWeatherChange;
        }
    }

    /// <summary>
    /// Callback method for the OnWeatherChangeStarted event.
    /// This is where the actual visual transition is initiated.
    /// </summary>
    /// <param name="newWeather">The new weather preset to transition to.</param>
    /// <param name="oldWeather">The previous weather preset.</param>
    /// <param name="transitionDuration">The duration for the transition.</param>
    private void OnWeatherChange(WeatherPreset newWeather, WeatherPreset oldWeather, float transitionDuration)
    {
        // Stop any ongoing transition to start a new one
        if (_currentTransitionCoroutine != null)
        {
            StopCoroutine(_currentTransitionCoroutine);
        }

        _currentTransitionCoroutine = StartCoroutine(TransitionVisualsCoroutine(newWeather, oldWeather, transitionDuration));
    }

    /// <summary>
    /// Coroutine to smoothly transition visual elements based on the new weather preset.
    /// </summary>
    private IEnumerator TransitionVisualsCoroutine(WeatherPreset targetWeather, WeatherPreset oldWeather, float duration)
    {
        float timer = 0f;

        // Store initial values if an old weather exists, otherwise assume current scene state
        Color startSkyTint = oldWeather != null ? oldWeather.skyTint : RenderSettings.skybox.GetColor("_Tint");
        Color startAmbientColor = oldWeather != null ? oldWeather.ambientLightColor : RenderSettings.ambientLight;
        Color startLightColor = oldWeather != null ? oldWeather.directionalLightColor : (directionalLight ? directionalLight.color : Color.white);
        float startLightIntensity = oldWeather != null ? oldWeather.directionalLightIntensity : (directionalLight ? directionalLight.intensity : 1f);
        Quaternion startLightRotation = oldWeather != null ? Quaternion.Euler(oldWeather.directionalLightRotation) : (directionalLight ? directionalLight.transform.rotation : Quaternion.identity);
        Color startFogColor = oldWeather != null ? oldWeather.fogColor : RenderSettings.fogColor;
        float startFogDensity = oldWeather != null ? oldWeather.fogDensity : RenderSettings.fogDensity;
        float startFogStart = oldWeather != null ? oldWeather.fogStartDistance : RenderSettings.fogStartDistance;
        float startFogEnd = oldWeather != null ? oldWeather.fogEndDistance : RenderSettings.fogEndDistance;
        bool startFogEnabled = oldWeather != null ? oldWeather.enableFog : RenderSettings.fog;


        // Handle Weather Effect Prefab (Particle Systems, etc.)
        // Destroy the old effect instance if it exists
        if (_currentEffectInstance != null)
        {
            // For particle systems, fading out is often better than instant destruction
            ParticleSystem[] oldParticles = _currentEffectInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in oldParticles)
            {
                var main = ps.main;
                main.loop = false; // Stop looping
                var emission = ps.emission;
                emission.enabled = false; // Stop new emissions
            }
            Destroy(_currentEffectInstance, duration); // Destroy after transition
            _currentEffectInstance = null;
        }

        // Instantiate the new effect prefab
        if (targetWeather.weatherEffectPrefab != null)
        {
            _currentEffectInstance = Instantiate(targetWeather.weatherEffectPrefab, transform);
            _currentEffectInstance.transform.localPosition = Vector3.zero; // Place at applier's position
            _currentEffectInstance.name = $"{targetWeather.weatherName} Effect";
            // Ensure particles start correctly (e.g., if prefab was disabled)
            ParticleSystem[] newParticles = _currentEffectInstance.GetComponentsInChildren<ParticleSystem>(true); // include inactive
            foreach (ParticleSystem ps in newParticles)
            {
                ps.gameObject.SetActive(true); // Ensure game object is active
                var main = ps.main;
                main.loop = true;
                ps.Play();
            }
        }
        
        // Handle immediate changes for 0 duration transitions
        if (duration <= 0.01f) // Use a small epsilon for float comparison
        {
            ApplyVisualsImmediately(targetWeather);
            yield break; // Exit coroutine
        }


        while (timer < duration)
        {
            float t = timer / duration;
            // Smoothstep for a more natural transition (starts and ends slowly)
            t = t * t * (3f - 2f * t); 

            // Skybox Tint
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetColor("_Tint", Color.Lerp(startSkyTint, targetWeather.skyTint, t));
            }

            // Ambient Light
            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, targetWeather.ambientLightColor, t);

            // Directional Light
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, targetWeather.directionalLightColor, t);
                directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetWeather.directionalLightIntensity, t);
                directionalLight.transform.rotation = Quaternion.Slerp(startLightRotation, Quaternion.Euler(targetWeather.directionalLightRotation), t);
            }

            // Fog Settings
            RenderSettings.fog = startFogEnabled || targetWeather.enableFog; // Enable if either current or target has fog
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = Color.Lerp(startFogColor, targetWeather.fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, targetWeather.fogDensity, t);
                RenderSettings.fogStartDistance = Mathf.Lerp(startFogStart, targetWeather.fogStartDistance, t);
                RenderSettings.fogEndDistance = Mathf.Lerp(startFogEnd, targetWeather.fogEndDistance, t);
                // Note: Fog mode change (Linear, Exponential) is not smoothly interpolatable.
                // It's best to stick to one mode or make an instant switch.
                // For simplicity, this example assumes Exponential fog via density and linear via start/end.
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are exactly set
        ApplyVisualsImmediately(targetWeather);
        _currentTransitionCoroutine = null; // Mark coroutine as finished
    }

    /// <summary>
    /// Immediately applies all visual settings from a weather preset without transition.
    /// Used for initial setup and ensuring final values after a transition.
    /// </summary>
    /// <param name="preset">The weather preset to apply.</param>
    private void ApplyVisualsImmediately(WeatherPreset preset)
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetColor("_Tint", preset.skyTint);
        }
        RenderSettings.ambientLight = preset.ambientLightColor;

        if (directionalLight != null)
        {
            directionalLight.color = preset.directionalLightColor;
            directionalLight.intensity = preset.directionalLightIntensity;
            directionalLight.transform.rotation = Quaternion.Euler(preset.directionalLightRotation);
        }

        RenderSettings.fog = preset.enableFog;
        if (preset.enableFog)
        {
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;
            RenderSettings.fogStartDistance = preset.fogStartDistance;
            RenderSettings.fogEndDistance = preset.fogEndDistance;
            // You might also want to set RenderSettings.fogMode here based on your preference
            // e.g., RenderSettings.fogMode = FogMode.Exponential;
        }
    }
}
```

---

**4. `WeatherSoundApplier.cs` (Audio Observer)**

This component handles the audio aspects of the weather. It subscribes to the `DynamicWeatherSystem`'s event and smoothly fades out the old ambient sound while fading in the new one.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// WeatherSoundApplier
/// This component listens to weather change events from the DynamicWeatherSystem
/// and applies ambient audio effects to the scene.
/// It requires an AudioSource component on the same GameObject.
///
/// Design Pattern: Observer
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeatherSoundApplier : MonoBehaviour
{
    private AudioSource _audioSource;
    private Coroutine _currentTransitionCoroutine;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Gets the AudioSource component.
    /// </summary>
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true; // Ambient sounds typically loop
        _audioSource.playOnAwake = false; // Don't play until weather is set
    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active.
    /// We subscribe to the weather change event here.
    /// </summary>
    private void OnEnable()
    {
        if (DynamicWeatherSystem.Instance != null)
        {
            DynamicWeatherSystem.Instance.OnWeatherChangeStarted += OnWeatherChange;
        }
        else
        {
            Debug.LogError("WeatherSoundApplier: DynamicWeatherSystem not found! Make sure it's in the scene and initialized.");
        }
    }

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive.
    /// We unsubscribe from the event to prevent memory leaks or errors.
    /// </summary>
    private void OnDisable()
    {
        if (DynamicWeatherSystem.Instance != null)
        {
            DynamicWeatherSystem.Instance.OnWeatherChangeStarted -= OnWeatherChange;
        }
    }

    /// <summary>
    /// Callback method for the OnWeatherChangeStarted event.
    /// This is where the actual audio transition is initiated.
    /// </summary>
    /// <param name="newWeather">The new weather preset to transition to.</param>
    /// <param name="oldWeather">The previous weather preset.</param>
    /// <param name="transitionDuration">The duration for the transition.</param>
    private void OnWeatherChange(WeatherPreset newWeather, WeatherPreset oldWeather, float transitionDuration)
    {
        // Stop any ongoing transition to start a new one
        if (_currentTransitionCoroutine != null)
        {
            StopCoroutine(_currentTransitionCoroutine);
        }

        _currentTransitionCoroutine = StartCoroutine(TransitionAudioCoroutine(newWeather, oldWeather, transitionDuration));
    }

    /// <summary>
    /// Coroutine to smoothly transition ambient audio based on the new weather preset.
    /// Fades out the old sound (if playing) and fades in the new one.
    /// </summary>
    private IEnumerator TransitionAudioCoroutine(WeatherPreset targetWeather, WeatherPreset oldWeather, float duration)
    {
        float timer = 0f;
        float startVolume = _audioSource.volume;
        AudioClip oldClip = _audioSource.clip;

        // Handle immediate changes for 0 duration transitions
        if (duration <= 0.01f)
        {
            ApplyAudioImmediately(targetWeather);
            yield break;
        }

        // If a new clip is being set, stop the old one if different,
        // but only assign if it's actually changing.
        if (targetWeather.ambientSoundClip != _audioSource.clip)
        {
            _audioSource.clip = targetWeather.ambientSoundClip;
        }

        // If the new clip is null or volume is 0, just fade out
        if (targetWeather.ambientSoundClip == null || targetWeather.ambientSoundVolume <= 0.01f)
        {
            if (_audioSource.isPlaying)
            {
                while (timer < duration)
                {
                    _audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                    timer += Time.deltaTime;
                    yield return null;
                }
                _audioSource.Stop();
                _audioSource.volume = 0f; // Ensure it's off
            }
        }
        else // New clip and valid volume, fade in
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
                _audioSource.volume = 0f; // Start from silent
            }

            while (timer < duration)
            {
                // Fade from current volume to target volume
                _audioSource.volume = Mathf.Lerp(startVolume, targetWeather.ambientSoundVolume, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // Ensure final values are exactly set
        ApplyAudioImmediately(targetWeather);
        _currentTransitionCoroutine = null; // Mark coroutine as finished
    }

    /// <summary>
    /// Immediately applies all audio settings from a weather preset without transition.
    /// Used for initial setup and ensuring final values after a transition.
    /// </summary>
    /// <param name="preset">The weather preset to apply.</param>
    private void ApplyAudioImmediately(WeatherPreset preset)
    {
        _audioSource.clip = preset.ambientSoundClip;
        if (preset.ambientSoundClip != null && preset.ambientSoundVolume > 0f)
        {
            if (!_audioSource.isPlaying) _audioSource.Play();
            _audioSource.volume = preset.ambientSoundVolume;
        }
        else
        {
            _audioSource.Stop();
            _audioSource.volume = 0f;
        }
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create C# Scripts:**
    *   Save each of the four code blocks above into separate `.cs` files with their respective names (`WeatherPreset.cs`, `DynamicWeatherSystem.cs`, `WeatherEffectApplier.cs`, `WeatherSoundApplier.cs`) in your Unity project's Assets folder.

2.  **Set up Weather Presets (ScriptableObjects):**
    *   In the Unity Editor, right-click in your Project window (e.g., `Assets/Weather/Presets`).
    *   Go to `Create > Dynamic Weather > Weather Preset`.
    *   Create several presets (e.g., "Sunny", "Rainy", "Snowy", "Foggy").
    *   **Configure each preset:**
        *   Adjust `Sky Tint`, `Ambient Light Color`, `Directional Light` properties, `Fog` settings, `Default Transition Duration`.
        *   **For `Weather Effect Prefab`:** Create a new empty GameObject, add a `Particle System` component to it (e.g., configure it for rain or snow), and then turn this GameObject into a Prefab. Drag this Prefab into the `Weather Effect Prefab` slot of your `WeatherPreset`.
        *   **For `Ambient Sound Clip`:** Drag an `AudioClip` (e.g., rain sounds, wind sounds) into this slot.

3.  **Set up the Dynamic Weather System GameObject:**
    *   Create an empty GameObject in your scene (e.g., name it `_WeatherSystem`).
    *   Attach the `DynamicWeatherSystem.cs` script to this GameObject.
    *   In the Inspector, populate the `Available Weather Presets` list by dragging your created `WeatherPreset` assets into it.
    *   Assign an `Initial Weather` preset from your list.

4.  **Set up Weather Effect Applier:**
    *   Create another empty GameObject (e.g., name it `_WeatherVisuals`).
    *   Attach the `WeatherEffectApplier.cs` script to it.
    *   In the Inspector:
        *   Drag your scene's main `Directional Light` (e.g., "Sun") to the `Directional Light` slot.
        *   Drag your scene's `Main Camera` to the `Main Camera` slot. (These will try to find themselves if left null, but explicit assignment is safer).
    *   Ensure `RenderSettings.skybox` has a material assigned (e.g., `Default-Skybox` or a custom one) for tinting to work.

5.  **Set up Weather Sound Applier:**
    *   Create another empty GameObject (e.g., name it `_WeatherAudio`).
    *   Attach the `WeatherSoundApplier.cs` script to it. An `AudioSource` component will be automatically added because of `[RequireComponent(typeof(AudioSource))]`.
    *   Configure the `AudioSource` if needed (e.g., spatial blend, output mixer).

6.  **Trigger Weather Changes from another Script:**
    Now, any other script in your game can easily change the weather using the `DynamicWeatherSystem`'s public methods:

    ```csharp
    using UnityEngine;

    public class WeatherTriggerExample : MonoBehaviour
    {
        [Header("Weather Trigger Settings")]
        [Tooltip("The weather preset to change to when triggered.")]
        public WeatherPreset targetWeatherPreset;
        [Tooltip("Optional: Custom transition duration for this specific trigger.")]
        public float customDuration = -1f; // -1 means use preset's default

        // Optional: Trigger weather change by key press
        public KeyCode triggerKey = KeyCode.P;

        void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                TriggerWeatherChange();
            }
        }

        public void TriggerWeatherChange()
        {
            if (DynamicWeatherSystem.Instance == null)
            {
                Debug.LogError("WeatherTriggerExample: DynamicWeatherSystem.Instance is null. Is it in the scene?");
                return;
            }

            if (targetWeatherPreset == null)
            {
                Debug.LogWarning("WeatherTriggerExample: No target weather preset assigned to trigger.");
                return;
            }

            // --- Using the SetWeather method with a preset directly ---
            DynamicWeatherSystem.Instance.SetWeather(targetWeatherPreset, customDuration);

            // --- Alternatively, you could use SetWeatherByName if you only have the name ---
            // DynamicWeatherSystem.Instance.SetWeatherByName("Rainy", customDuration);
        }

        // Example: Change weather after a delay
        public void StartDelayedWeatherChange(WeatherPreset preset, float delay, float duration)
        {
            StartCoroutine(DelayedChange(preset, delay, duration));
        }

        private System.Collections.IEnumerator DelayedChange(WeatherPreset preset, float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            DynamicWeatherSystem.Instance.SetWeather(preset, duration);
        }
    }
    ```
    *   Attach this `WeatherTriggerExample.cs` script to any GameObject (e.g., your player).
    *   Assign a `WeatherPreset` to its `Target Weather Preset` slot in the Inspector.
    *   Run the game, and press the `P` key (or whatever `triggerKey` you set) to see the weather transition!

This complete setup demonstrates a practical and extensible 'DynamicWeatherSystem' using common Unity patterns and best practices.