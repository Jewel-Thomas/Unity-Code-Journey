// Unity Design Pattern Example: DynamicSkySystem
// This script demonstrates the DynamicSkySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DynamicSkySystem' pattern in Unity development centralizes the management of all sky-related elements in a game. This includes time of day, lighting, fog, skybox, and any associated visual effects. The core idea is to provide a single, consistent source of truth and control for the dynamic environment, allowing other game systems (e.g., AI, gameplay, UI, VFX) to react to environmental changes without directly manipulating low-level `RenderSettings` or `Light` components.

This pattern typically involves:
1.  **A Central Manager (Singleton):** A `MonoBehaviour` that acts as the primary orchestrator for the sky system. It ensures global access and handles the logic for updating the sky.
2.  **Sky Presets (ScriptableObjects):** Data containers that define distinct sky states (e.g., "Day," "Night," "Dusk," "Rainy"). These store properties like ambient light color, directional light color/intensity, fog settings, and skybox material.
3.  **Time of Day/Transition Logic:** The manager tracks the game's time and smoothly interpolates between different sky presets or their properties based on the current time or triggered events.
4.  **Event System:** The manager exposes events that other game systems can subscribe to, allowing them to react to changes in the sky (e.g., an event for `OnTimeUpdated` or `OnSkyChanged`).

Below are two C# scripts that implement the 'DynamicSkySystem' pattern in Unity.

---

### 1. `SkyPreset.cs` (ScriptableObject)

This script defines a `SkyPreset` which is a `ScriptableObject`. It holds all the configurable parameters for a specific sky state (e.g., Day, Night).

```csharp
using UnityEngine;

/// <summary>
/// SkyPreset ScriptableObject: Defines a specific set of sky and lighting properties.
/// These presets can be created in the Unity Editor and assigned to the DynamicSkyManager.
/// </summary>
[CreateAssetMenu(fileName = "NewSkyPreset", menuName = "Dynamic Sky System/Sky Preset")]
public class SkyPreset : ScriptableObject
{
    [Header("Skybox Settings")]
    [Tooltip("The skybox material to use for this preset. Can be null if using a procedural skybox shader that's managed externally.")]
    public Material skyboxMaterial;

    [Tooltip("Exposure value for the skybox material. Adjusts overall brightness of the skybox.")]
    [Range(0.1f, 8f)]
    public float skyboxExposure = 1f;

    [Tooltip("Intensity of stars or other night-specific skybox features. Can be used to control shader properties or particle systems.")]
    [Range(0f, 1f)]
    public float starsIntensity = 0f; // Could control a stars particle system or skybox shader property

    [Header("Lighting Settings")]
    [Tooltip("Color of the ambient light in the scene. Affects overall scene brightness and tint.")]
    [ColorUsage(true, true)] // HDR color picker
    public Color ambientLightColor = Color.white;

    [Tooltip("Color of the primary directional light (sun/moon) during this preset.")]
    [ColorUsage(true, true)] // HDR color picker
    public Color sunLightColor = Color.white;

    [Tooltip("Intensity of the primary directional light (sun/moon) during this preset.")]
    [Range(0f, 10f)]
    public float sunLightIntensity = 1f;

    [Header("Fog Settings")]
    [Tooltip("Color of the global fog.")]
    [ColorUsage(true, false)]
    public Color fogColor = Color.gray;

    [Tooltip("Density of the global exponential fog. Higher values mean thicker fog.")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;

    [Tooltip("Start distance for linear fog. Fog begins at this distance from the camera.")]
    public float fogStartDistance = 0f;

    [Tooltip("End distance for linear fog. Fog is fully opaque at this distance.")]
    public float fogEndDistance = 300f;
}
```

---

### 2. `DynamicSkyManager.cs` (Singleton MonoBehaviour)

This is the central manager script. It's a `MonoBehaviour` that implements the Singleton pattern, ensuring only one instance exists. It controls the time of day, blends properties between `SkyPreset`s, and updates `RenderSettings` and the main `DirectionalLight`.

```csharp
using UnityEngine;
using System; // For Action event
using System.Collections.Generic; // For List

/// <summary>
/// DynamicSkyManager: The central manager for the game's sky system.
/// Implements the Singleton pattern to provide a global access point.
/// It controls the time of day, blends between SkyPresets, and updates
/// RenderSettings (ambient light, fog, skybox) and the main directional light.
/// </summary>
public class DynamicSkyManager : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Ensures there's only one instance of the Sky Manager throughout the game.
    public static DynamicSkyManager Instance { get; private set; }

    [Tooltip("The main directional light representing the sun/moon. Assign a GameObject with a Light component.")]
    public Light sunMoonLight;

    [Header("Time of Day Settings")]
    [Range(0f, 24f)]
    [Tooltip("Current time of day in hours (0-24).")]
    public float currentTimeInHours = 12f;

    [Tooltip("Speed at which time progresses (hours per real second). Set to 0 for static time.")]
    public float timeProgressionSpeed = 0.5f; // E.g., 0.5 hours per real second

    [Range(0f, 24f)]
    [Tooltip("The hour when sunrise starts (e.g., 6 for 6 AM).")]
    public float sunriseHour = 6f;

    [Range(0f, 24f)]
    [Tooltip("The hour when sunset starts (e.g., 18 for 6 PM).")]
    public float sunsetHour = 18f;

    [Range(0.1f, 6f)]
    [Tooltip("Duration of the sunrise/sunset transition in hours. Controls the smoothness of lighting changes.")]
    public float transitionDurationHours = 2f; // How long sunrise/sunset lasts (total, not per side)

    [Header("Sky Presets")]
    [Tooltip("The SkyPreset to use for daytime. Create these via Create -> Dynamic Sky System -> Sky Preset.")]
    public SkyPreset dayPreset;
    [Tooltip("The SkyPreset to use for nighttime. Create these via Create -> Dynamic Sky System -> Sky Preset.")]
    public SkyPreset nightPreset;

    [Header("Debug")]
    [Tooltip("Display current time in a UI Text element for debugging.")]
    public TMPro.TextMeshProUGUI debugTimeText; // Requires TextMeshPro installed in project

    // --- Events for Subscribing Systems ---
    // Other systems can subscribe to these events to react to sky changes.
    // Example: A UI element displaying the time, or a particle system changing based on day/night.
    /// <summary>
    /// Event fired when the sky's visual state is updated.
    /// Provides the blended SkyPreset and the normalized time of day (0-1).
    /// </summary>
    public event Action<SkyPreset, float> OnSkyUpdated;

    /// <summary>
    /// Event fired when the time of day advances.
    /// Provides the current time in hours (0-24).
    /// </summary>
    public event Action<float> OnTimeUpdated;

    private float _normalizedTime; // 0-1 representing the day cycle (0 and 1 are midnight, 0.5 is noon)
    private SkyPreset _blendedPreset; // A runtime instance to hold the blended properties

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize RenderSettings if not already set (e.g., from an environment profile)
        if (RenderSettings.skybox == null && dayPreset != null && dayPreset.skyboxMaterial != null)
        {
            RenderSettings.skybox = dayPreset.skyboxMaterial;
        }

        // Create a runtime SkyPreset instance to store blended values
        // This avoids creating new ScriptableObjects every frame and allows GC to manage it later.
        _blendedPreset = ScriptableObject.CreateInstance<SkyPreset>();
        _blendedPreset.name = "BlendedSkyPreset_Runtime"; // For easier identification in profiler
    }

    private void Start()
    {
        // Ensure sunMoonLight is assigned. If not, try to find one.
        if (sunMoonLight == null)
        {
            sunMoonLight = FindObjectOfType<Light>();
            if (sunMoonLight != null && sunMoonLight.type != LightType.Directional)
            {
                sunMoonLight = null; // We specifically need a directional light
                Debug.LogWarning("DynamicSkyManager: Found a Light component, but it's not a Directional Light. Please assign a Directional Light to 'Sun Moon Light'.", this);
            }
            else if (sunMoonLight == null)
            {
                Debug.LogWarning("DynamicSkyManager: No Directional Light assigned or found in scene. Dynamic lighting will not function.", this);
            }
        }

        // Apply initial sky state
        UpdateSkyState();
    }

    private void Update()
    {
        // Advance time only if progression speed is greater than 0
        if (timeProgressionSpeed > 0f)
        {
            AdvanceTime(Time.deltaTime);
        }
        UpdateSkyState();

        // Debug display
        if (debugTimeText != null)
        {
            int hours = (int)currentTimeInHours;
            int minutes = (int)((currentTimeInHours - hours) * 60f);
            debugTimeText.text = string.Format("Time: {0:00}:{1:00}", hours, minutes);
        }
    }

    /// <summary>
    /// Advances the time of day by a given delta.
    /// </summary>
    /// <param name="deltaTime">The time passed in seconds since the last frame.</param>
    public void AdvanceTime(float deltaTime)
    {
        currentTimeInHours += deltaTime * timeProgressionSpeed;
        if (currentTimeInHours >= 24f)
        {
            currentTimeInHours -= 24f; // Loop back to the start of the day (0-24 hours)
        }
        _normalizedTime = currentTimeInHours / 24f; // Normalize for 0-1 range

        OnTimeUpdated?.Invoke(currentTimeInHours);
    }

    /// <summary>
    /// Forcefully sets the time of day to a specific hour.
    /// </summary>
    /// <param name="hours">The desired time in hours (0-24).</param>
    public void SetTimeOfDay(float hours)
    {
        currentTimeInHours = Mathf.Clamp(hours, 0f, 24f);
        _normalizedTime = currentTimeInHours / 24f;
        UpdateSkyState();
        OnTimeUpdated?.Invoke(currentTimeInHours);
    }

    /// <summary>
    /// Instantly applies the properties of a given SkyPreset to the scene.
    /// This bypasses time-based blending.
    /// </summary>
    /// <param name="preset">The SkyPreset to apply immediately.</param>
    public void SetPresetImmediately(SkyPreset preset)
    {
        if (preset == null)
        {
            Debug.LogError("Attempted to set a null SkyPreset immediately.");
            return;
        }
        ApplySkyPreset(preset);
        OnSkyUpdated?.Invoke(preset, _normalizedTime);
    }

    /// <summary>
    /// The core logic that updates all sky-related elements based on the current time of day.
    /// It blends properties between the `dayPreset` and `nightPreset`.
    /// </summary>
    private void UpdateSkyState()
    {
        if (dayPreset == null || nightPreset == null)
        {
            Debug.LogWarning("DynamicSkyManager: Day or Night Sky Preset is not assigned. Please assign them in the inspector.");
            return;
        }

        // Get a blend factor (0 = full night, 1 = full day) based on current time
        float blendFactor = GetTimeOfDayBlendFactor(currentTimeInHours, sunriseHour, sunsetHour, transitionDurationHours);

        // Blend all properties from dayPreset to nightPreset (or vice versa)
        // Lerp from nightPreset (blendFactor = 0) to dayPreset (blendFactor = 1)
        _blendedPreset.ambientLightColor = Color.Lerp(nightPreset.ambientLightColor, dayPreset.ambientLightColor, blendFactor);
        _blendedPreset.sunLightColor = Color.Lerp(nightPreset.sunLightColor, dayPreset.sunLightColor, blendFactor);
        _blendedPreset.sunLightIntensity = Mathf.Lerp(nightPreset.sunLightIntensity, dayPreset.sunLightIntensity, blendFactor);

        _blendedPreset.fogColor = Color.Lerp(nightPreset.fogColor, dayPreset.fogColor, blendFactor);
        _blendedPreset.fogDensity = Mathf.Lerp(nightPreset.fogDensity, dayPreset.fogDensity, blendFactor);
        _blendedPreset.fogStartDistance = Mathf.Lerp(nightPreset.fogStartDistance, dayPreset.fogStartDistance, blendFactor);
        _blendedPreset.fogEndDistance = Mathf.Lerp(nightPreset.fogEndDistance, dayPreset.fogEndDistance, blendFactor);

        // Skybox Material and Exposure:
        // For simplicity, we switch skybox material directly or prioritize one.
        // For advanced blending, a custom skybox shader with blend parameters would be ideal.
        if (blendFactor > 0.5f) // Closer to day
        {
            _blendedPreset.skyboxMaterial = dayPreset.skyboxMaterial != null ? dayPreset.skyboxMaterial : RenderSettings.skybox;
            _blendedPreset.skyboxExposure = dayPreset.skyboxExposure;
        }
        else // Closer to night
        {
            _blendedPreset.skyboxMaterial = nightPreset.skyboxMaterial != null ? nightPreset.skyboxMaterial : RenderSettings.skybox;
            _blendedPreset.skyboxExposure = nightPreset.skyboxExposure;
        }
        _blendedPreset.starsIntensity = Mathf.Lerp(nightPreset.starsIntensity, dayPreset.starsIntensity, blendFactor);


        // Apply the blended values to Unity's RenderSettings and the Directional Light
        ApplySkyPreset(_blendedPreset);

        // Rotate the directional light to simulate sun/moon movement
        if (sunMoonLight != null)
        {
            // Rotate around X-axis. 0 degrees at 6 AM, 90 degrees at 12 PM, 180 degrees at 6 PM, 270 degrees at 12 AM.
            // Sun is highest at noon (12:00), lowest at midnight (0:00).
            // Default Unity directional light has Z-axis pointing forward.
            // A common setup: 12:00 (noon) = 90 degrees, 0:00 (midnight) = 270 degrees.
            float rotationDegrees = (currentTimeInHours / 24f) * 360f - 90f; // Noon is 12, so 12/24 * 360 = 180, -90 = 90 degrees.
            sunMoonLight.transform.rotation = Quaternion.Euler(rotationDegrees, 0f, 0f);

            // In some cases, you might want to switch between a 'sun' and 'moon' light object
            // or modify the shadow strength/color here based on time.
        }

        OnSkyUpdated?.Invoke(_blendedPreset, _normalizedTime);
    }

    /// <summary>
    /// Calculates a blend factor (0 to 1) between night (0) and day (1) based on current time.
    /// This function handles smooth transitions during sunrise and sunset periods.
    /// </summary>
    /// <param name="currentHour">Current time in hours (0-24).</param>
    /// <param name="sunriseH">Hour when sunrise starts.</param>
    /// <param name="sunsetH">Hour when sunset starts.</param>
    /// <param name="transitionH">Total duration of the transition window (e.g., 2 hours means 1 hour before and 1 hour after the event).</param>
    /// <returns>A float from 0 (night) to 1 (day).</returns>
    private float GetTimeOfDayBlendFactor(float currentHour, float sunriseH, float sunsetH, float transitionH)
    {
        float blendFactor = 0f; // Default to night (0)
        float totalHours = 24f;

        // Ensure transitionH is within reasonable limits to prevent overlapping phases
        // It shouldn't be larger than half the day length or half the night length
        transitionH = Mathf.Min(transitionH, (sunsetH - sunriseH) / 2f - 0.1f); // For day period
        transitionH = Mathf.Min(transitionH, (totalHours - (sunsetH - sunriseH)) / 2f - 0.1f); // For night period
        transitionH = Mathf.Max(0.01f, transitionH); // Minimum transition duration

        // Define the start and end hours for each phase (transition + full day/night)
        float sunriseTransitionStart = (sunriseH - transitionH + totalHours) % totalHours; // Night -> Day transition begins
        float sunriseTransitionEnd = (sunriseH + transitionH) % totalHours;               // Night -> Day transition ends (full day)

        float sunsetTransitionStart = (sunsetH - transitionH + totalHours) % totalHours;   // Day -> Night transition begins
        float sunsetTransitionEnd = (sunsetH + transitionH) % totalHours;                 // Day -> Night transition ends (full night)

        // Case 1: Within the Sunrise transition window (Night to Day)
        // This condition needs to handle wrap-around from `sunriseTransitionStart` if it's past midnight.
        if (IsTimeInWindow(currentHour, sunriseTransitionStart, sunriseTransitionEnd))
        {
            blendFactor = Mathf.InverseLerp(sunriseTransitionStart, sunriseTransitionEnd, currentHour);
        }
        // Case 2: Full Day (after sunrise transition, before sunset transition)
        else if (IsTimeInWindow(currentHour, sunriseTransitionEnd, sunsetTransitionStart))
        {
            blendFactor = 1f;
        }
        // Case 3: Within the Sunset transition window (Day to Night)
        // This condition also needs to handle wrap-around for `sunsetTransitionEnd`.
        else if (IsTimeInWindow(currentHour, sunsetTransitionStart, sunsetTransitionEnd))
        {
            blendFactor = 1f - Mathf.InverseLerp(sunsetTransitionStart, sunsetTransitionEnd, currentHour);
        }
        // Case 4: Full Night (after sunset transition, before sunrise transition)
        else
        {
            blendFactor = 0f;
        }

        return Mathf.Clamp01(blendFactor);
    }

    /// <summary>
    /// Helper to check if a current hour falls within a time window, handling wrap-around midnight.
    /// </summary>
    private bool IsTimeInWindow(float currentTime, float windowStart, float windowEnd)
    {
        if (windowStart <= windowEnd)
        {
            return currentTime >= windowStart && currentTime < windowEnd;
        }
        else // Window wraps around midnight
        {
            return currentTime >= windowStart || currentTime < windowEnd;
        }
    }

    /// <summary>
    /// Applies the properties of a given SkyPreset (blended or direct) to the scene's RenderSettings and Directional Light.
    /// </summary>
    /// <param name="preset">The SkyPreset to apply.</param>
    private void ApplySkyPreset(SkyPreset preset)
    {
        if (preset == null) return;

        // Apply Skybox
        if (preset.skyboxMaterial != null && RenderSettings.skybox != preset.skyboxMaterial)
        {
            RenderSettings.skybox = preset.skyboxMaterial;
        }
        if (RenderSettings.skybox != null)
        {
            // Assuming the skybox material has a "_Exposure" property (standard for Unity skybox shaders)
            RenderSettings.skybox.SetFloat("_Exposure", preset.skyboxExposure);
            // You might add more skybox material property setting here, e.g., "_SunDiskIntensity", "_StarsIntensity"
            // For a simple demo, we just set the material and exposure.
        }

        // Apply Lighting
        RenderSettings.ambientLight = preset.ambientLightColor;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; // Or TriLight/Custom for more sophisticated ambient lighting

        if (sunMoonLight != null)
        {
            sunMoonLight.color = preset.sunLightColor;
            sunMoonLight.intensity = preset.sunLightIntensity;
        }

        // Apply Fog
        RenderSettings.fog = true; // Always enable fog to manage it here
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;
        RenderSettings.fogStartDistance = preset.fogStartDistance;
        RenderSettings.fogEndDistance = preset.fogEndDistance;
    }

    // --- Public methods for other systems to interact ---
    /// <summary>
    /// Gets the current normalized time of day (0-1).
    /// 0 and 1 represent midnight, 0.5 represents noon.
    /// </summary>
    public float GetNormalizedTimeOfDay()
    {
        return _normalizedTime;
    }

    /// <summary>
    /// Gets the current time of day in hours (0-24).
    /// </summary>
    public float GetTimeInHours()
    {
        return currentTimeInHours;
    }

    /// <summary>
    /// Gets the current active SkyPreset (blended or directly set).
    /// Note: This returns the *runtime blended* preset, not necessarily the original dayPreset or nightPreset.
    /// </summary>
    public SkyPreset GetCurrentActiveSkyPreset()
    {
        return _blendedPreset;
    }
}
```

---

### Example Usage and Setup in Unity:

**1. Create Scripts:**
   - Create a new C# script named `SkyPreset` and paste the `SkyPreset.cs` code into it.
   - Create a new C# script named `DynamicSkyManager` and paste the `DynamicSkyManager.cs` code into it.

**2. Create Sky Presets (ScriptableObjects):**
   - In the Unity Editor, right-click in your Project window.
   - Go to `Create -> Dynamic Sky System -> Sky Preset`.
   - Create two presets:
     -   **`DayPreset`**: Configure bright `ambientLightColor`, `sunLightColor`, high `sunLightIntensity`. Set `starsIntensity` to 0. Use a bright `skyboxMaterial` (e.g., Unity's Default-Skybox). Adjust fog as desired for daytime.
     -   **`NightPreset`**: Configure dark `ambientLightColor`, a bluish/white `sunLightColor` (for moon light), low `sunLightIntensity`. Set `starsIntensity` to 1 (if your skybox supports it or for a particle system). Use a dark `skyboxMaterial` or the same Default-Skybox with lower exposure. Adjust fog for nighttime (often thicker or clearer depending on desired effect).

**3. Setup the `DynamicSkySystem` GameObject:**
   - Create an empty GameObject in your scene (Right-click in Hierarchy -> `Create Empty`).
   - Name it `DynamicSkySystem`.
   - Attach the `DynamicSkyManager` script to this `DynamicSkySystem` GameObject.

**4. Configure the `DynamicSkyManager`:**
   - In the Inspector, drag your created `DayPreset` and `NightPreset` into their respective fields.
   - **`Sun Moon Light`**: Create a `Directional Light` in your scene (GameObject -> Light -> Directional Light). Drag this `Directional Light` into the `Sun Moon Light` field of the `DynamicSkyManager`. This light will simulate the sun and moon.
   - Adjust `timeProgressionSpeed`, `sunriseHour`, `sunsetHour`, and `transitionDurationHours` to your liking.
   - (Optional) For debugging, create a UI Text (TextMeshPro preferred) and assign it to the `Debug Time Text` field to see the current time.

**5. Play the Scene:**
   - Run your scene. You should observe the sky, lighting, and fog smoothly transitioning between day and night based on the `timeProgressionSpeed`. The directional light will also rotate.

**Example of Subscribing to Events (e.g., for a UI or Weather System):**

```csharp
using UnityEngine;
using TMPro; // For TextMeshPro
using System;

public class GameStateMonitor : MonoBehaviour
{
    public TextMeshProUGUI timeDisplayUI;
    public ParticleSystem rainParticles; // Example for weather effect

    void OnEnable()
    {
        // Subscribe to events when this component is enabled
        if (DynamicSkyManager.Instance != null)
        {
            DynamicSkyManager.Instance.OnTimeUpdated += HandleTimeUpdated;
            DynamicSkyManager.Instance.OnSkyUpdated += HandleSkyUpdated;
        }
        else
        {
            Debug.LogWarning("DynamicSkyManager not found, GameStateMonitor cannot subscribe to events.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe from events when this component is disabled to prevent memory leaks
        if (DynamicSkyManager.Instance != null)
        {
            DynamicSkyManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
            DynamicSkyManager.Instance.OnSkyUpdated -= HandleSkyUpdated;
        }
    }

    private void HandleTimeUpdated(float currentHour)
    {
        if (timeDisplayUI != null)
        {
            int hours = (int)currentHour;
            int minutes = (int)((currentHour - hours) * 60f);
            timeDisplayUI.text = string.Format("Current Time: {0:00}:{1:00}", hours, minutes);
        }

        // Example: Trigger specific events at certain hours
        if (Mathf.Abs(currentHour - 7f) < 0.1f) // Around 7 AM
        {
            Debug.Log("Good morning! Time to start the day's quests.");
        }
    }

    private void HandleSkyUpdated(SkyPreset activePreset, float normalizedTime)
    {
        Debug.Log($"Sky updated to: {activePreset.name} (normalized time: {normalizedTime:F2})");

        // Example: Control a particle system based on day/night
        if (rainParticles != null)
        {
            // If it's more 'night-like' (e.g., blendFactor < 0.2 or > 0.8 on 0-1 scale)
            // For this specific setup, 0 is night, 1 is day.
            if (normalizedTime < 0.2f || normalizedTime > 0.8f) // Very early morning or late evening/night
            {
                 // Maybe enable night-specific particles like fireflies or fog FX
                 // For rain, you might tie it to a separate weather system.
            }

            // A more direct weather system would likely call DynamicSkyManager.Instance.SetPresetImmediately(RainyPreset);
            // And then this would react to that specific preset being active.
        }

        // Example: Adjust UI elements or soundscapes based on active sky preset
        // if (activePreset == DynamicSkyManager.Instance.dayPreset) { /* Play cheerful music */ }
        // else if (activePreset == DynamicSkyManager.Instance.nightPreset) { /* Play eerie music */ }
    }
}
```