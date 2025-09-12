// Unity Design Pattern Example: DynamicLightingSystem
// This script demonstrates the DynamicLightingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the 'DynamicLightingSystem' design pattern. This pattern provides a centralized, flexible, and data-driven way to manage and transition scene lighting based on various in-game conditions (e.g., time of day, player location, game events).

It consists of two main parts:
1.  **`LightingProfile` (ScriptableObject):** Defines a complete set of lighting and environment settings for a particular state.
2.  **`DynamicLightingSystem` (MonoBehaviour Singleton):** The core manager that applies and smoothly transitions between these `LightingProfile`s.

This setup allows designers to configure lighting presets in the editor without coding and enables game logic to easily request lighting changes.

---

## 1. `LightingProfile.cs`

This `ScriptableObject` defines a data structure to hold all the lighting and environment settings for a particular "state" (e.g., "Day," "Night," "Cave").

```csharp
using UnityEngine;
using UnityEngine.Rendering; // Required for LightShadows and AmbientMode

/// <summary>
/// Represents a complete lighting configuration for a scene or a specific state (e.g., Day, Night, Dungeon).
/// This ScriptableObject allows designers to easily create and manage different lighting presets
/// directly within the Unity editor.
/// </summary>
[CreateAssetMenu(fileName = "NewLightingProfile", menuName = "Dynamic Lighting/Lighting Profile", order = 1)]
public class LightingProfile : ScriptableObject
{
    [Header("Profile Identification")]
    [Tooltip("A unique name for this lighting profile.")]
    public string profileName = "Default Profile";
    [TextArea]
    [Tooltip("A brief description of this lighting profile's intended use.")]
    public string description = "A predefined set of lighting and environment settings.";

    [Header("Directional Light Settings")]
    [Tooltip("The color of the main directional light in the scene.")]
    public Color directionalLightColor = Color.white;
    [Tooltip("The intensity multiplier of the main directional light.")]
    [Range(0, 8)] // Common range for light intensity in Unity
    public float directionalLightIntensity = 1.0f;
    [Tooltip("The strength of shadows cast by the main directional light.")]
    [Range(0, 1)]
    public float directionalLightShadowStrength = 1.0f;
    [Tooltip("The render mode for shadows cast by the main directional light.")]
    public LightShadows directionalLightShadows = LightShadows.Hard;

    [Header("Ambient Light Settings")]
    [Tooltip("The color of the ambient light in the scene (used when Ambient Mode is Flat).")]
    [ColorUsage(false, true)] // HDR color picker allows values > 1 for intensity
    public Color ambientLightColor = Color.gray;
    [Tooltip("The ambient light source type. Skybox uses the scene's skybox, Flat uses Ambient Light Color.")]
    public AmbientMode ambientMode = AmbientMode.Skybox;
    [Tooltip("The skybox material to use for the scene's background and ambient lighting (if Ambient Mode is Skybox).")]
    public Material skyboxMaterial;

    [Header("Fog Settings")]
    [Tooltip("Whether fog should be enabled for this lighting profile.")]
    public bool useFog = false;
    [Tooltip("The color of the fog.")]
    public Color fogColor = Color.grey;
    [Tooltip("The mode of the fog (e.g., Linear, Exponential).")]
    public FogMode fogMode = FogMode.Linear;
    [Tooltip("The starting distance for linear fog.")]
    public float fogStartDistance = 0f;
    [Tooltip("The ending distance for linear fog.")]
    public float fogEndDistance = 300f;
    [Tooltip("The density of exponential fog.")]
    public float fogDensity = 0.01f;

    // --- Extension Points ---
    // You can extend this class to include more settings relevant to your project, e.g.:
    // [Header("Post-Processing Settings (requires Post-Processing Stack/URP/HDRP)")]
    // public UnityEngine.Rendering.VolumeProfile postProcessVolumeProfile;
    // public Color cameraBackgroundColor = Color.black; // Useful if no skybox
    // public float exposure = 1.0f; // Could be part of a custom post-processing setup
}
```

---

## 2. `DynamicLightingSystem.cs`

This is the central manager script. It uses the Singleton pattern to provide easy, global access. It handles applying `LightingProfile`s instantly or smoothly transitioning between them.

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // Required for RenderSettings

/// <summary>
/// The DynamicLightingSystem manages and orchestrates scene lighting changes
/// based on predefined LightingProfile ScriptableObjects.
/// It acts as a central control point for modifying directional light, ambient light,
/// skybox, and fog settings, allowing for smooth transitions between different
/// lighting states (e.g., day, night, indoor, outdoor).
///
/// This script implements the Singleton pattern to ensure a single, globally
/// accessible instance controls the scene's dynamic lighting.
/// </summary>
public class DynamicLightingSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static DynamicLightingSystem _instance;
    public static DynamicLightingSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<DynamicLightingSystem>();

                if (_instance == null)
                {
                    // If no instance found, create a new GameObject and add the component
                    GameObject go = new GameObject("DynamicLightingSystem");
                    _instance = go.AddComponent<DynamicLightingSystem>();
                    Debug.LogWarning("DynamicLightingSystem: No instance found in scene, created a new one. " +
                                     "Consider adding it manually to a GameObject (e.g., 'GameManagers') for easier configuration.");
                }
            }
            return _instance;
        }
    }

    // --- Editor-Configurable References ---
    [Header("Scene References")]
    [Tooltip("The main Directional Light in the scene that this system will control.")]
    [SerializeField] private Light _directionalLight;
    
    [Header("Default & Transition Settings")]
    [Tooltip("The lighting profile to apply when the system starts.")]
    [SerializeField] private LightingProfile _defaultLightingProfile;
    
    [Tooltip("The default duration for smooth lighting transitions in seconds.")]
    [SerializeField] private float _defaultTransitionDuration = 2.0f;

    // --- Internal State Management for Transitions ---
    private LightingProfile _currentAppliedProfile; // The profile whose settings are currently fully active (no transition ongoing)
    private LightingProfile _sourceTransitionProfile; // The profile we are transitioning FROM
    private LightingProfile _targetTransitionProfile; // The profile we are transitioning TO
    private float _transitionTimer; // Tracks the progress of the current transition (0 to _defaultTransitionDuration)
    private bool _isTransitioning; // Flag to indicate if a transition is active

    // --- Public Properties (Read-Only) ---
    public LightingProfile CurrentAppliedProfile => _currentAppliedProfile;
    public bool IsTransitioning => _isTransitioning;

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        // Ensure only one instance of the singleton exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        _instance = this;
        // Optionally, prevent this GameObject from being destroyed on scene load
        // DontDestroyOnLoad(gameObject); 

        // Attempt to find the main directional light if not assigned in the inspector
        if (_directionalLight == null)
        {
            _directionalLight = FindMainDirectionalLight();
            if (_directionalLight == null)
            {
                Debug.LogError("DynamicLightingSystem: No directional light found in the scene! " +
                               "Please assign one in the inspector or ensure one exists and is tagged 'MainDirectionalLight'.");
            }
        }
    }

    private void Start()
    {
        // Apply the default profile immediately on start if specified
        if (_defaultLightingProfile != null)
        {
            ApplyProfileImmediate(_defaultLightingProfile);
            _currentAppliedProfile = _defaultLightingProfile; // Set as the initial fully applied profile
            _sourceTransitionProfile = _defaultLightingProfile; // Also set as the source for future transitions
        }
        else
        {
            // If no default profile, capture current scene settings as a temporary base profile.
            // This ensures smooth transitions even if the scene starts with unconfigured lighting.
            _currentAppliedProfile = CaptureCurrentSceneSettingsAsProfile("RuntimeInitialProfile");
            _sourceTransitionProfile = _currentAppliedProfile;
        }
    }

    private void Update()
    {
        // Handle smooth transitions between lighting profiles over time
        if (_isTransitioning)
        {
            _transitionTimer += Time.deltaTime;
            // Calculate interpolation factor (t) from 0 to 1
            float t = Mathf.Clamp01(_transitionTimer / _defaultTransitionDuration); 

            // If transition is complete, snap to the target profile and stop transitioning
            if (t >= 1.0f)
            {
                ApplyProfileImmediate(_targetTransitionProfile); // Ensure target is fully applied
                _currentAppliedProfile = _targetTransitionProfile; // Update current applied profile
                _sourceTransitionProfile = _targetTransitionProfile; // Source for next transition is now this target
                _isTransitioning = false;
                _transitionTimer = 0f;
            }
            else
            {
                // Interpolate settings between source and target profiles based on 't'
                ApplyInterpolatedProfile(_sourceTransitionProfile, _targetTransitionProfile, t);
            }
        }
    }

    // --- Public API for Lighting Control ---

    /// <summary>
    /// Applies a lighting profile to the scene immediately without any transition.
    /// This will instantly change all relevant directional light, ambient, skybox, and fog settings.
    /// </summary>
    /// <param name="profile">The LightingProfile to apply.</param>
    public void ApplyProfileImmediate(LightingProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("DynamicLightingSystem: Attempted to apply a null lighting profile.");
            return;
        }
        if (_directionalLight == null)
        {
            Debug.LogError($"DynamicLightingSystem: Cannot apply profile '{profile.profileName}', directional light reference is missing.");
            return;
        }

        // Apply Directional Light Settings
        _directionalLight.color = profile.directionalLightColor;
        _directionalLight.intensity = profile.directionalLightIntensity;
        _directionalLight.shadowStrength = profile.directionalLightShadowStrength;
        _directionalLight.shadows = profile.directionalLightShadows;

        // Apply Ambient Light Settings
        RenderSettings.ambientMode = profile.ambientMode;
        if (profile.ambientMode == AmbientMode.Flat)
        {
            RenderSettings.ambientLight = profile.ambientLightColor;
        }
        else if (profile.ambientMode == AmbientMode.Skybox)
        {
            RenderSettings.skybox = profile.skyboxMaterial;
            if (profile.skyboxMaterial != null)
            {
                // Force a global illumination update if skybox changed for immediate reflection in ambient lighting
                DynamicGI.UpdateEnvironment(); 
            }
            // For Skybox mode, ambientLightColor can still be used as a tint, but we'll prioritize skybox.
            // If ambient light is explicitly set in the profile for Skybox mode, one might apply a tint:
            // RenderSettings.ambientLight = profile.ambientLightColor; 
        }
        // Additional modes (e.g., Trilight) would require more properties in LightingProfile.

        // Apply Fog Settings
        RenderSettings.fog = profile.useFog;
        RenderSettings.fogColor = profile.fogColor;
        RenderSettings.fogMode = profile.fogMode;
        RenderSettings.fogStartDistance = profile.fogStartDistance;
        RenderSettings.fogEndDistance = profile.fogEndDistance;
        RenderSettings.fogDensity = profile.fogDensity;

        // Update internal state
        _currentAppliedProfile = profile;
        _sourceTransitionProfile = profile; // If applied immediately, it also becomes the new source for subsequent transitions
        _isTransitioning = false;
        _transitionTimer = 0f;
        
        Debug.Log($"DynamicLightingSystem: Applied profile '{profile.profileName}' immediately.");
    }

    /// <summary>
    /// Initiates a smooth transition from the current lighting settings (or currently interpolated state)
    /// to a new lighting profile. The transition duration can be customized for this specific transition.
    /// </summary>
    /// <param name="newProfile">The LightingProfile to transition to.</param>
    /// <param name="duration">The time in seconds for the transition. If 0 or less, the default transition duration is used.</param>
    public void TransitionToProfile(LightingProfile newProfile, float duration = -1f)
    {
        if (newProfile == null)
        {
            Debug.LogWarning("DynamicLightingSystem: Attempted to transition to a null lighting profile.");
            return;
        }
        if (_directionalLight == null)
        {
            Debug.LogError($"DynamicLightingSystem: Cannot transition to profile '{newProfile.profileName}', directional light reference is missing.");
            return;
        }

        // Use default duration if not specified or invalid
        if (duration <= 0) duration = _defaultTransitionDuration;

        // If the new profile is already the target, or already fully applied and not transitioning, do nothing
        if (newProfile == _targetTransitionProfile || (newProfile == _currentAppliedProfile && !_isTransitioning))
        {
            Debug.Log($"DynamicLightingSystem: Already targeting or applied profile '{newProfile.profileName}'. No new transition initiated.");
            return;
        }

        // Determine the source profile for the new transition:
        // If already transitioning, the source is the *currently interpolated state*.
        // Otherwise, the source is the last *fully applied profile* (or the scene's current settings if none applied yet).
        if (_isTransitioning)
        {
            // Capture the current interpolated state as the source for the new transition
            float currentT = _transitionTimer / _defaultTransitionDuration;
            _sourceTransitionProfile = CaptureCurrentInterpolatedStateAsProfile("MidTransitionSnapshot", _sourceTransitionProfile, _targetTransitionProfile, currentT);
        }
        else
        {
            // If not transitioning, the source is the last fully applied profile.
            // If _currentAppliedProfile is null (e.g., first ever transition call), capture current scene state.
            _sourceTransitionProfile = _currentAppliedProfile ?? CaptureCurrentSceneSettingsAsProfile("InitialSceneState");
        }

        _targetTransitionProfile = newProfile;
        _defaultTransitionDuration = duration; // Set the duration for this specific transition
        _transitionTimer = 0f; // Reset timer to begin the new transition
        _isTransitioning = true;
        
        Debug.Log($"DynamicLightingSystem: Initiating transition from '{_sourceTransitionProfile?.profileName ?? "Current Scene"}' to '{newProfile.profileName}' over {duration} seconds.");
    }

    // --- Helper Methods ---

    /// <summary>
    /// Finds the main directional light in the scene. Prioritizes lights tagged "MainDirectionalLight"
    /// or lights whose name contains "Sun", otherwise picks the first active directional light.
    /// </summary>
    /// <returns>The found directional light, or null if none is found.</returns>
    private Light FindMainDirectionalLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional && (light.gameObject.CompareTag("MainDirectionalLight") || light.gameObject.name.Contains("Sun")))
            {
                return light;
            }
        }
        // If no specifically tagged/named light, just take the first directional light found
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                return light;
            }
        }
        return null;
    }

    /// <summary>
    /// Interpolates between two lighting profiles based on a 't' value (0 to 1)
    /// and applies the interpolated settings to the scene.
    /// This method is called repeatedly during a transition in Update().
    /// </summary>
    /// <param name="fromProfile">The starting lighting profile.</param>
    /// <param name="toProfile">The target lighting profile.</param>
    /// <param name="t">Interpolation factor (0 = fromProfile, 1 = toProfile).</param>
    private void ApplyInterpolatedProfile(LightingProfile fromProfile, LightingProfile toProfile, float t)
    {
        if (_directionalLight == null) return;

        // Interpolate Directional Light Settings
        _directionalLight.color = Color.Lerp(fromProfile.directionalLightColor, toProfile.directionalLightColor, t);
        _directionalLight.intensity = Mathf.Lerp(fromProfile.directionalLightIntensity, toProfile.directionalLightIntensity, t);
        _directionalLight.shadowStrength = Mathf.Lerp(fromProfile.directionalLightShadowStrength, toProfile.directionalLightShadowStrength, t);
        // Shadow type cannot be smoothly interpolated, so we snap to the target type at 50% transition.
        _directionalLight.shadows = (t < 0.5f) ? fromProfile.directionalLightShadows : toProfile.directionalLightShadows;

        // Interpolate Ambient Light Settings
        RenderSettings.ambientLight = Color.Lerp(fromProfile.ambientLightColor, toProfile.ambientLightColor, t);
        // Ambient mode cannot be smoothly interpolated, snap to target.
        RenderSettings.ambientMode = (t < 0.5f) ? fromProfile.ambientMode : toProfile.ambientMode;

        // Skybox material cannot be smoothly interpolated directly without special shaders.
        // For simplicity, we snap it once during the transition.
        Material currentSkybox = RenderSettings.skybox;
        if (t < 0.5f && currentSkybox != fromProfile.skyboxMaterial)
        {
            RenderSettings.skybox = fromProfile.skyboxMaterial;
            DynamicGI.UpdateEnvironment(); // Update GI if skybox changed
        }
        else if (t >= 0.5f && currentSkybox != toProfile.skyboxMaterial)
        {
            RenderSettings.skybox = toProfile.skyboxMaterial;
            DynamicGI.UpdateEnvironment(); // Update GI if skybox changed
        }

        // Interpolate Fog Settings
        RenderSettings.fog = (t < 0.5f) ? fromProfile.useFog : toProfile.useFog; // Snap fog on/off
        RenderSettings.fogColor = Color.Lerp(fromProfile.fogColor, toProfile.fogColor, t);
        RenderSettings.fogMode = (t < 0.5f) ? fromProfile.fogMode : toProfile.fogMode; // Snap fog mode
        RenderSettings.fogStartDistance = Mathf.Lerp(fromProfile.fogStartDistance, toProfile.fogStartDistance, t);
        RenderSettings.fogEndDistance = Mathf.Lerp(fromProfile.fogEndDistance, toProfile.fogEndDistance, t);
        RenderSettings.fogDensity = Mathf.Lerp(fromProfile.fogDensity, toProfile.fogDensity, t);
    }

    /// <summary>
    /// Captures the current scene's lighting settings and creates a new temporary LightingProfile
    /// from them. This is useful for starting transitions from the current scene state, especially
    /// if no default profile was specified or if you need a baseline for the first transition.
    /// </summary>
    /// <param name="profileName">The name for the new temporary profile.</param>
    /// <returns>A new LightingProfile ScriptableObject filled with current scene settings.</returns>
    private LightingProfile CaptureCurrentSceneSettingsAsProfile(string profileName)
    {
        LightingProfile tempProfile = ScriptableObject.CreateInstance<LightingProfile>();
        tempProfile.profileName = profileName;

        if (_directionalLight != null)
        {
            tempProfile.directionalLightColor = _directionalLight.color;
            tempProfile.directionalLightIntensity = _directionalLight.intensity;
            tempProfile.directionalLightShadowStrength = _directionalLight.shadowStrength;
            tempProfile.directionalLightShadows = _directionalLight.shadows;
        }
        else
        {
            // Provide default light settings if no directional light is found in scene
            tempProfile.directionalLightColor = Color.white;
            tempProfile.directionalLightIntensity = 1f;
            tempProfile.directionalLightShadowStrength = 1f;
            tempProfile.directionalLightShadows = LightShadows.Hard;
        }

        tempProfile.ambientMode = RenderSettings.ambientMode;
        tempProfile.ambientLightColor = RenderSettings.ambientLight;
        tempProfile.skyboxMaterial = RenderSettings.skybox;

        tempProfile.useFog = RenderSettings.fog;
        tempProfile.fogColor = RenderSettings.fogColor;
        tempProfile.fogMode = RenderSettings.fogMode;
        tempProfile.fogStartDistance = RenderSettings.fogStartDistance;
        tempProfile.fogEndDistance = RenderSettings.fogEndDistance;
        tempProfile.fogDensity = RenderSettings.fogDensity;
        
        // This temporary profile exists only in memory for runtime use.
        // It is NOT saved as an asset to your project folder.
        return tempProfile;
    }

    /// <summary>
    /// Creates a temporary LightingProfile by interpolating between two existing profiles
    /// at a given 't' value. This is used to capture the current interpolated state during
    /// an ongoing transition, allowing for smooth re-transitions if a new profile is requested.
    /// </summary>
    /// <param name="profileName">The name for the new temporary profile.</param>
    /// <param name="fromProfile">The starting profile of the previous transition.</param>
    /// <param name="toProfile">The target profile of the previous transition.</param>
    /// <param name="t">The current interpolation factor (0 to 1) of the previous transition.</param>
    /// <returns>A new LightingProfile representing the interpolated state.</returns>
    private LightingProfile CaptureCurrentInterpolatedStateAsProfile(string profileName, LightingProfile fromProfile, LightingProfile toProfile, float t)
    {
        LightingProfile interpolatedProfile = ScriptableObject.CreateInstance<LightingProfile>();
        interpolatedProfile.profileName = profileName;

        // Directional Light
        interpolatedProfile.directionalLightColor = Color.Lerp(fromProfile.directionalLightColor, toProfile.directionalLightColor, t);
        interpolatedProfile.directionalLightIntensity = Mathf.Lerp(fromProfile.directionalLightIntensity, toProfile.directionalLightIntensity, t);
        interpolatedProfile.directionalLightShadowStrength = Mathf.Lerp(fromProfile.directionalLightShadowStrength, toProfile.directionalLightShadowStrength, t);
        interpolatedProfile.directionalLightShadows = (t < 0.5f) ? fromProfile.directionalLightShadows : toProfile.directionalLightShadows;

        // Ambient Light
        interpolatedProfile.ambientLightColor = Color.Lerp(fromProfile.ambientLightColor, toProfile.ambientLightColor, t);
        interpolatedProfile.ambientMode = (t < 0.5f) ? fromProfile.ambientMode : toProfile.ambientMode;
        interpolatedProfile.skyboxMaterial = (t < 0.5f) ? fromProfile.skyboxMaterial : toProfile.skyboxMaterial;

        // Fog Settings
        interpolatedProfile.useFog = (t < 0.5f) ? fromProfile.useFog : toProfile.useFog;
        interpolatedProfile.fogColor = Color.Lerp(fromProfile.fogColor, toProfile.fogColor, t);
        interpolatedProfile.fogMode = (t < 0.5f) ? fromProfile.fogMode : toProfile.fogMode;
        interpolatedProfile.fogStartDistance = Mathf.Lerp(fromProfile.fogStartDistance, toProfile.fogStartDistance, t);
        interpolatedProfile.fogEndDistance = Mathf.Lerp(fromProfile.fogEndDistance, toProfile.fogEndDistance, t);
        interpolatedProfile.fogDensity = Mathf.Lerp(fromProfile.fogDensity, toProfile.fogDensity, t);

        return interpolatedProfile;
    }


    // --- Example Usage (for demonstration purposes within this script) ---
    // In a real project, you would typically call TransitionToProfile from other
    // game logic scripts (e.g., GameManager, TimeOfDayController, ZoneManager).

    private float _timeOfDay = 0f; // Represents hours (0-24)
    private float _dayDuration = 60f; // How many real-world seconds a full 24-hour cycle takes
    
    [Header("Example: Time-of-Day Cycle (for demonstration)")]
    [Tooltip("Assign your 'DayProfile' asset here.")]
    public LightingProfile dayProfile;
    [Tooltip("Assign your 'NightProfile' asset here.")]
    public LightingProfile nightProfile;
    [Tooltip("If true, the system will automatically cycle between day and night for demonstration purposes.")]
    public bool enableTimeOfDayCycle = false;

    private void FixedUpdate() // FixedUpdate for consistent time updates, can also be Update()
    {
        if (!enableTimeOfDayCycle || dayProfile == null || nightProfile == null) return;

        // Advance time
        _timeOfDay += Time.fixedDeltaTime * (24f / _dayDuration); 
        if (_timeOfDay >= 24f) _timeOfDay -= 24f; // Wrap time around 24 hours

        // Example logic:
        // Transition to Day profile between 5:00 and 19:00 (7 PM)
        // Transition to Night profile between 19:00 (7 PM) and 5:00 AM
        float transitionTime = _defaultTransitionDuration * 2; // Longer transition for day/night

        if (_timeOfDay > 5f && _timeOfDay < 19f)
        {
            // If current target is not day, start transition to day
            if (_targetTransitionProfile != dayProfile)
            {
                TransitionToProfile(dayProfile, transitionTime);
            }
        }
        else 
        {
            // If current target is not night, start transition to night
            if (_targetTransitionProfile != nightProfile)
            {
                TransitionToProfile(nightProfile, transitionTime);
            }
        }
    }
}

/*
    --- How to Implement the DynamicLightingSystem in your Unity Project ---

    **1. Create LightingProfile Assets:**
        -   In your Unity Project window, right-click -> Create -> Dynamic Lighting -> Lighting Profile.
        -   Name it appropriately (e.g., "DayProfile", "NightProfile", "CaveProfile").
        -   Select the created asset and configure its properties in the Inspector
            (directional light color/intensity, ambient light settings, skybox material, fog settings).
        -   Create as many different profiles as your game needs for various lighting scenarios.

    **2. Add the DynamicLightingSystem to your Scene:**
        -   Create an empty GameObject in your scene (e.g., named "GameManagers" or "LightingSystem").
        -   Add the 'DynamicLightingSystem.cs' script as a component to this GameObject.
        -   In the Inspector for the DynamicLightingSystem component:
            -   **Directional Light:** Drag your scene's primary directional light (usually named "Directional Light" or "Sun") into this slot.
            -   **Default Lighting Profile:** Drag one of your created LightingProfile assets (e.g., "DayProfile") into this slot. This profile will be applied when the game starts.
            -   **Default Transition Duration:** Set how long lighting transitions should typically take (e.g., 2-5 seconds).
            -   *(Optional Demonstration)*: For the built-in time-of-day example, drag your 'DayProfile' and 'NightProfile' assets into the respective slots and check 'Enable Time of Day Cycle'.

    **3. Trigger Lighting Changes from Other Scripts:**
        -   To change lighting, you simply call a method on the `DynamicLightingSystem.Instance` from any other script in your game.

        **Example A: Instant Lighting Change (e.g., entering a dark cave, sudden event)**
        ```csharp
        // In a script attached to a collider trigger, or a game event handler:
        using UnityEngine; // Don't forget this!

        public class CaveTrigger : MonoBehaviour
        {
            public LightingProfile caveLightingProfile; // Drag your 'CaveProfile' asset here in the Inspector

            private void OnTriggerEnter(Collider other)
            {
                if (other.CompareTag("Player")) // Check if the collider belongs to the player
                {
                    DynamicLightingSystem.Instance.ApplyProfileImmediate(caveLightingProfile);
                    Debug.Log("Player entered cave, lighting changed instantly to: " + caveLightingProfile.profileName);
                }
            }
        }
        ```

        **Example B: Smooth Lighting Transition (e.g., day/night cycle, area transition)**
        ```csharp
        // In a GameManager or AreaManager script:
        using UnityEngine; // Don't forget this!

        public class EnvironmentController : MonoBehaviour
        {
            public LightingProfile sunnyDayProfile;    // Assign your "SunnyDayProfile" asset
            public LightingProfile cloudyDayProfile;   // Assign your "CloudyDayProfile" asset
            public LightingProfile rainyNightProfile;  // Assign your "RainyNightProfile" asset

            // Call this method when it's time to transition to a new lighting state
            public void SetSunnyDayLighting()
            {
                DynamicLightingSystem.Instance.TransitionToProfile(sunnyDayProfile, 4.0f); // 4-second transition
                Debug.Log("Transitioning to Sunny Day lighting.");
            }

            public void SetCloudyDayLighting()
            {
                DynamicLightingSystem.Instance.TransitionToProfile(cloudyDayProfile, 3.0f); // 3-second transition
                Debug.Log("Transitioning to Cloudy Day lighting.");
            }

            public void SetRainyNightLighting()
            {
                DynamicLightingSystem.Instance.TransitionToProfile(rainyNightProfile, 6.0f); // 6-second, slower transition
                Debug.Log("Transitioning to Rainy Night lighting.");
            }

            // Example of triggering from input (for testing)
            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetSunnyDayLighting();
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetCloudyDayLighting();
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetRainyNightLighting();
            }
        }
        ```

    **Key Design Pattern Concepts Demonstrated:**

    *   **Singleton:** `DynamicLightingSystem.Instance` provides a single, globally accessible point of control for managing scene lighting. This ensures consistency and prevents conflicts from multiple objects trying to control global settings.
    *   **Strategy / State:** `LightingProfile` acts as a "strategy" or "state" object. Each profile encapsulates a complete set of lighting parameters. The `DynamicLightingSystem` can switch between these strategies/states dynamically. This decouples the specific lighting configurations from the core logic that applies them.
    *   **Centralized Control:** All lighting adjustments are funneled through the `DynamicLightingSystem`. This makes it easy to manage, debug, and extend lighting behaviors across your entire game.
    *   **Data-Driven Design:** `LightingProfile` ScriptableObjects allow artists and designers to configure and iterate on lighting settings directly within the Unity editor without touching code, significantly streamlining the development workflow.
    *   **Observer/Event-driven (implicit):** While not explicitly using C# events internally, the system is designed to be "observed" or "triggered" by other game systems (e.g., a `TimeOfDayController`, `ZoneManager`, or `QuestManager`) which "request" lighting changes. The `DynamicLightingSystem` then carries out the "response."
*/
```