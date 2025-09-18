// Unity Design Pattern Example: LightingManagerSystem
// This script demonstrates the LightingManagerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script provides a practical implementation of the 'LightingManagerSystem' design pattern. It uses a Singleton pattern for easy global access, ScriptableObjects for flexible lighting presets, and coroutines for smooth transitions between different lighting environments.

**How to use this script in your Unity project:**

1.  **Create C# Script:** Create a new C# script named `LightingManagerSystem` in your Unity project and paste the entire code below into it.
2.  **Create LightingManager GameObject:**
    *   In your scene, create an empty GameObject and name it `LightingManager`.
    *   Attach the `LightingManagerSystem` script to this `LightingManager` GameObject.
3.  **Assign Directional Light:**
    *   In the Inspector for the `LightingManager` GameObject, drag your scene's primary directional light (usually named "Directional Light" by default) into the "Directional Light" field. If you don't have one, create a new "Light -> Directional Light" in your scene.
4.  **Create Lighting Presets (ScriptableObjects):**
    *   Go to `Assets/Create/Lighting/Lighting Preset`.
    *   Create several presets (e.g., `Day_Preset`, `Night_Preset`, `Dusk_Preset`, `Indoor_Preset`).
    *   For each preset, select it in your Project window and adjust its properties in the Inspector (Ambient Color, Directional Light Color/Intensity, Fog settings, Skybox Material, Light Rotation, etc.).
    *   **Crucially**, set the `Lighting State` enum for each preset to match its intended role (e.g., `Day_Preset` should have `Lighting State: Day`).
5.  **Assign Presets to Manager:**
    *   Select the `LightingManager` GameObject in your scene.
    *   In the Inspector, expand the "Available Presets" list.
    *   Drag and drop the `LightingPreset` ScriptableObjects you created into this list. Ensure you have one preset for each `LightingState` enum you plan to use (e.g., one for `Day`, one for `Night`, etc.).
6.  **Set Initial State:**
    *   In the `LightingManager` Inspector, set the `Initial Lighting State` to the state you want your scene to start with (e.g., `Day`).
7.  **Example Usage from another script:**
    *   To change the lighting, call `LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.Night, 5f);` from any other script in your scene. The second parameter is the transition duration in seconds.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // Required for Action event

/// <summary>
/// ScriptableObject to define a collection of lighting settings for a specific time of day or mood.
/// This allows designers to create and adjust different lighting environments from the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "NewLightingPreset", menuName = "Lighting/Lighting Preset")]
public class LightingPreset : ScriptableObject
{
    // === Core Lighting Properties ===
    [Header("Core Lighting Settings")]
    [Tooltip("The type of lighting state this preset represents (e.g., Day, Night).")]
    public LightingManagerSystem.LightingState lightingState;

    [GradientUsage(true)]
    [Tooltip("Ambient light color. Affects overall scene brightness.")]
    public Color ambientLightColor = Color.white;

    [Tooltip("Fog color. If fog is enabled in RenderSettings, this color will be used.")]
    public Color fogColor = Color.grey;

    [Tooltip("Fog density (for exponential fog) or end distance (for linear fog).")]
    public float fogDensity = 0.01f;

    [Tooltip("Multiplier for indirect light, influencing global illumination bounces.")]
    [Range(0f, 5f)]
    public float lightBounceIntensity = 1.0f;

    // === Directional Light Properties (Sun/Moon) ===
    [Header("Directional Light Settings")]
    [GradientUsage(true)]
    [Tooltip("Color of the primary directional light (e.g., sun or moon).")]
    public Color directionalLightColor = Color.white;

    [Tooltip("Intensity of the primary directional light.")]
    [Range(0f, 5f)]
    public float directionalLightIntensity = 1.0f;

    [Tooltip("X-axis rotation of the directional light (determines sun/moon height).")]
    [Range(0f, 360f)]
    public float directionalLightRotationX = 50f;

    [Tooltip("Y-axis rotation of the directional light (determines sun/moon horizontal position).")]
    [Range(0f, 360f)]
    public float directionalLightRotationY = -30f;

    [Tooltip("Strength of shadows cast by the directional light.")]
    [Range(0f, 1f)]
    public float shadowStrength = 1.0f;

    // === Skybox Properties ===
    [Header("Skybox Settings")]
    [Tooltip("Material for the skybox. Set this to null to use solid color ambient.")]
    public Material skyboxMaterial;
}

/// <summary>
/// The LightingManagerSystem implements the Singleton pattern and serves as a central
/// controller for all global lighting settings in the game. It allows for defining
/// and smoothly transitioning between different lighting presets (e.g., Day, Night, Indoor).
/// </summary>
[DefaultExecutionOrder(-100)] // Ensures this manager initializes before most other scripts
public class LightingManagerSystem : MonoBehaviour
{
    // =====================================================================================
    // Singleton Pattern Implementation
    // =====================================================================================
    private static LightingManagerSystem _instance;
    public static LightingManagerSystem Instance
    {
        get
        {
            // If the instance doesn't exist, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<LightingManagerSystem>();

                // If still null, create a new GameObject and attach the script.
                // This is useful for managers that might not be manually placed in every scene.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(LightingManagerSystem).Name);
                    _instance = singletonObject.AddComponent<LightingManagerSystem>();
                    Debug.LogWarning($"[LightingManagerSystem] No existing instance found. Creating a new one on GameObject '{singletonObject.name}'.");
                }
            }
            return _instance;
        }
    }

    // Ensure the instance is correctly set up and persists across scene loads if needed.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If another instance already exists, destroy this duplicate.
            Destroy(gameObject);
            Debug.LogWarning("[LightingManagerSystem] Duplicate instance of LightingManagerSystem found and destroyed.");
        }
        else
        {
            _instance = this;
            // Optionally, uncomment the line below if you want the manager to persist across scene loads.
            // DontDestroyOnLoad(gameObject);
            Debug.Log("[LightingManagerSystem] Initialized.");
        }
    }

    // =====================================================================================
    // Public Enums & Events
    // =====================================================================================

    /// <summary>
    /// Defines the different named states for lighting environments.
    /// </summary>
    public enum LightingState
    {
        Day,
        Night,
        Dusk,
        IndoorBright,
        IndoorDim,
        Custom // For a preset that doesn't fit a standard category or is temporary
    }

    /// <summary>
    /// Event fired when the lighting state changes. Other systems can subscribe to this.
    /// </summary>
    public event Action<LightingState> OnLightingStateChanged;

    // =====================================================================================
    // Serialized Fields (Configurable in Inspector)
    // =====================================================================================

    [Header("Manager Settings")]
    [SerializeField, Tooltip("The main directional light in the scene (usually the sun/moon).")]
    private Light _directionalLight;

    [SerializeField, Tooltip("The initial lighting state to apply when the scene starts.")]
    private LightingState _initialLightingState = LightingState.Day;

    [SerializeField, Tooltip("List of all available lighting presets.")]
    private List<LightingPreset> _availablePresets = new List<LightingPreset>();

    [SerializeField, Tooltip("Default duration for lighting transitions if not specified.")]
    private float _defaultTransitionDuration = 2.0f;

    // =====================================================================================
    // Private Internal State
    // =====================================================================================

    private LightingState _currentLightingState;
    private LightingPreset _currentAppliedPreset;
    private Coroutine _transitionCoroutine; // To manage ongoing transitions

    // =====================================================================================
    // Public Properties
    // =====================================================================================

    /// <summary>
    /// Gets the currently active lighting state.
    /// </summary>
    public LightingState CurrentLightingState => _currentLightingState;

    // =====================================================================================
    // MonoBehaviour Lifecycle
    // =====================================================================================

    private void Start()
    {
        if (_directionalLight == null)
        {
            // Try to find a directional light if not assigned in the inspector.
            _directionalLight = FindObjectOfType<Light>();
            if (_directionalLight != null && _directionalLight.type != LightType.Directional)
            {
                _directionalLight = null; // Don't use non-directional lights
            }

            if (_directionalLight == null)
            {
                Debug.LogError("[LightingManagerSystem] No directional light assigned or found in the scene! Global lighting will not be fully managed.");
                // We can still apply other RenderSettings, but the directional light won't be controlled.
            }
        }

        // Apply the initial lighting state
        SetLightingState(_initialLightingState, 0f); // Apply immediately at start
    }

    // =====================================================================================
    // Public Methods
    // =====================================================================================

    /// <summary>
    /// Initiates a transition to a new lighting state over a specified duration.
    /// If duration is 0, the change is applied instantly.
    /// </summary>
    /// <param name="newState">The target lighting state (e.g., Day, Night).</param>
    /// <param name="transitionDuration">Duration of the smooth transition in seconds.</param>
    public void SetLightingState(LightingState newState, float transitionDuration = -1f)
    {
        if (transitionDuration < 0) transitionDuration = _defaultTransitionDuration;

        LightingPreset targetPreset = GetPresetForState(newState);
        if (targetPreset == null)
        {
            Debug.LogWarning($"[LightingManagerSystem] No preset found for state '{newState}'. Lighting will not change for this state.");
            return;
        }

        if (_currentLightingState == newState && _currentAppliedPreset == targetPreset)
        {
            Debug.Log($"[LightingManagerSystem] Already in state '{newState}'. No change needed.");
            return;
        }

        Debug.Log($"[LightingManagerSystem] Transitioning to state: {newState} over {transitionDuration} seconds.");

        _currentLightingState = newState;
        
        // Stop any ongoing transition before starting a new one
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        if (transitionDuration <= 0.01f) // Apply immediately if duration is very small
        {
            ApplyLightingPresetImmediate(targetPreset);
        }
        else
        {
            // Start a new smooth transition
            _transitionCoroutine = StartCoroutine(TransitionLighting(targetPreset, transitionDuration));
        }

        // Invoke the event after the state change is initiated (or completed immediately)
        OnLightingStateChanged?.Invoke(_currentLightingState);
    }

    /// <summary>
    /// Applies a lighting preset instantly without any transition.
    /// This is useful for immediate changes or initial setup.
    /// </summary>
    /// <param name="preset">The LightingPreset to apply.</param>
    public void ApplyLightingPresetImmediate(LightingPreset preset)
    {
        if (preset == null)
        {
            Debug.LogError("[LightingManagerSystem] Attempted to apply a null lighting preset.");
            return;
        }

        Debug.Log($"[LightingManagerSystem] Applying preset '{preset.name}' immediately.");

        // Apply Render Settings
        RenderSettings.ambientLight = preset.ambientLightColor;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;
        RenderSettings.reflectionIntensity = preset.lightBounceIntensity; // Using this for overall bounce intensity
        RenderSettings.skybox = preset.skyboxMaterial;

        // Apply Directional Light Settings
        if (_directionalLight != null)
        {
            _directionalLight.color = preset.directionalLightColor;
            _directionalLight.intensity = preset.directionalLightIntensity;
            _directionalLight.shadowStrength = preset.shadowStrength;
            _directionalLight.transform.rotation = Quaternion.Euler(preset.directionalLightRotationX, preset.directionalLightRotationY, 0);
        }
        else
        {
            Debug.LogWarning("[LightingManagerSystem] Directional light is null. Cannot apply directional light settings.");
        }

        _currentAppliedPreset = preset;
    }

    // =====================================================================================
    // Private Helper Methods
    // =====================================================================================

    /// <summary>
    /// Retrieves a LightingPreset ScriptableObject based on the given LightingState.
    /// </summary>
    /// <param name="state">The target lighting state.</param>
    /// <returns>The corresponding LightingPreset, or null if not found.</returns>
    private LightingPreset GetPresetForState(LightingState state)
    {
        foreach (var preset in _availablePresets)
        {
            if (preset.lightingState == state)
            {
                return preset;
            }
        }
        return null;
    }

    /// <summary>
    /// Coroutine for smoothly transitioning between the current lighting settings
    /// and a target LightingPreset.
    /// </summary>
    /// <param name="targetPreset">The LightingPreset to transition to.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    private IEnumerator TransitionLighting(LightingPreset targetPreset, float duration)
    {
        if (targetPreset == null || duration <= 0)
        {
            ApplyLightingPresetImmediate(targetPreset);
            yield break;
        }

        // Capture current settings to use as start points for Lerp
        Color startAmbientColor = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;
        float startBounceIntensity = RenderSettings.reflectionIntensity; // Assuming this is used for bounce

        Color startDirectionalLightColor = Color.white;
        float startDirectionalLightIntensity = 0f;
        Quaternion startDirectionalLightRotation = Quaternion.identity;
        float startShadowStrength = 0f;

        if (_directionalLight != null)
        {
            startDirectionalLightColor = _directionalLight.color;
            startDirectionalLightIntensity = _directionalLight.intensity;
            startDirectionalLightRotation = _directionalLight.transform.rotation;
            startShadowStrength = _directionalLight.shadowStrength;
        }

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration; // Normalized time (0 to 1)
            t = t * t * (3f - 2f * t); // Smoothstep interpolation for a smoother feel

            // Lerp Render Settings
            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, targetPreset.ambientLightColor, t);
            RenderSettings.fogColor = Color.Lerp(startFogColor, targetPreset.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, targetPreset.fogDensity, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startBounceIntensity, targetPreset.lightBounceIntensity, t);
            // Skybox transition is more complex, usually handled by blending two skybox materials
            // For simplicity, we just set the target skybox once the transition is complete or near complete.
            // A more advanced system might use a custom shader for skybox blending.
            if (t > 0.5f && RenderSettings.skybox != targetPreset.skyboxMaterial)
            {
                RenderSettings.skybox = targetPreset.skyboxMaterial;
            }

            // Lerp Directional Light Settings
            if (_directionalLight != null)
            {
                _directionalLight.color = Color.Lerp(startDirectionalLightColor, targetPreset.directionalLightColor, t);
                _directionalLight.intensity = Mathf.Lerp(startDirectionalLightIntensity, targetPreset.directionalLightIntensity, t);
                _directionalLight.transform.rotation = Quaternion.Slerp(startDirectionalLightRotation, Quaternion.Euler(targetPreset.directionalLightRotationX, targetPreset.directionalLightRotationY, 0), t);
                _directionalLight.shadowStrength = Mathf.Lerp(startShadowStrength, targetPreset.shadowStrength, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set precisely at the end of the transition
        ApplyLightingPresetImmediate(targetPreset);
        Debug.Log($"[LightingManagerSystem] Transition to '{targetPreset.name}' complete.");
        _transitionCoroutine = null; // Clear the coroutine reference
    }
}

/*
// =====================================================================================
// EXAMPLE USAGE IN ANOTHER SCRIPT
// =====================================================================================

// Attach this script to any GameObject in your scene to test the LightingManagerSystem.
public class LightingTestController : MonoBehaviour
{
    [Header("Testing Lighting States")]
    public LightingManagerSystem.LightingState targetState = LightingManagerSystem.LightingState.Night;
    public float transitionTime = 5.0f;

    void Start()
    {
        // Example 1: Change to Day lighting after 2 seconds on scene start
        // This is primarily for demonstrating how to call it.
        // Usually, you'd trigger this based on game events (e.g., "level loaded", "time of day cycle").
        Invoke("SetDayLighting", 2f);

        // Example: Subscribe to the lighting change event
        LightingManagerSystem.Instance.OnLightingStateChanged += HandleLightingStateChange;
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks when the object is destroyed.
        if (LightingManagerSystem.Instance != null)
        {
            LightingManagerSystem.Instance.OnLightingStateChanged -= HandleLightingStateChange;
        }
    }

    void Update()
    {
        // Example 2: Change lighting with keyboard input
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.Day, transitionTime);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.Dusk, transitionTime);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.Night, transitionTime);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.IndoorBright, transitionTime);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.IndoorDim, transitionTime);
        }
    }

    // Method called by Invoke
    void SetDayLighting()
    {
        Debug.Log("[LightingTestController] Invoked: Setting initial Day lighting.");
        LightingManagerSystem.Instance.SetLightingState(LightingManagerSystem.LightingState.Day, transitionTime);
    }

    // Event handler for lighting state changes
    void HandleLightingStateChange(LightingManagerSystem.LightingState newState)
    {
        Debug.Log($"[LightingTestController] Lighting state just changed to: {newState}. Current state: {LightingManagerSystem.Instance.CurrentLightingState}");
        // Here you could trigger other game logic based on lighting changes:
        // - Adjust UI elements for readability
        // - Change NPC behavior (e.g., sleep at night, wake at day)
        // - Play specific ambient sounds
        // - Enable/disable specific post-processing effects
    }

    // Example of a button click or event callback
    public void OnChangeLightingButtonClicked()
    {
        Debug.Log($"[LightingTestController] Button clicked: Changing to {targetState} lighting.");
        LightingManagerSystem.Instance.SetLightingState(targetState, transitionTime);
    }
}
*/
```