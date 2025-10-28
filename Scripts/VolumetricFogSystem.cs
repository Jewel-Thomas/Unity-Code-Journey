// Unity Design Pattern Example: VolumetricFogSystem
// This script demonstrates the VolumetricFogSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "VolumetricFogSystem" design pattern, as conceptualized here, aims to provide a robust and extensible way to manage global and localized volumetric fog effects within a Unity project. While not a classic GoF pattern, it embodies principles of **Singleton** (for the main manager), **Strategy/Composition** (in how fog properties are determined), and **Service Locator** (for other systems to query fog properties).

**Core Idea:**
A central `VolumetricFogSystem` manager orchestrates global fog settings and integrates contributions from various `LocalVolumetricFogVolume` components. It provides a unified API for other parts of the game (e.g., cameras, post-processing effects) to query the effective fog properties at any given world position.

---

## VolumetricFogSystem Design Pattern Breakdown:

1.  **VolumetricFogSettings (ScriptableObject):**
    *   **Purpose:** Defines a reusable set of fog properties (color, density, falloff, etc.). This allows designers to easily create and modify different fog presets as assets.
    *   **Pattern Relevance:** Acts as the data payload, promoting reusability and separating configuration from logic.

2.  **VolumetricFogSystem (MonoBehaviour, Singleton):**
    *   **Purpose:** The central manager for all fog-related operations. It maintains the global fog settings and a list of active local fog volumes. It provides a public API to query effective fog properties at any point in the world.
    *   **Pattern Relevance:**
        *   **Singleton:** Ensures there's only one instance of the fog system, providing a global access point.
        *   **Service Locator:** Other game systems can "locate" this manager to get current fog settings.
        *   **Orchestrator:** Coordinates global settings with local overrides.

3.  **LocalVolumetricFogVolume (MonoBehaviour):**
    *   **Purpose:** Defines a specific area in the world where fog properties should deviate from or override the global settings. It has its own `VolumetricFogSettings` and a collider to define its boundaries.
    *   **Pattern Relevance:**
        *   **Component:** Allows easy attachment to GameObjects, leveraging Unity's component-based architecture.
        *   **Strategy (implicit):** Contributes its own "strategy" (local settings) to the overall fog calculation performed by the `VolumetricFogSystem`.
        *   **Observer/Registrar:** Registers and unregisters itself with the `VolumetricFogSystem` upon activation/deactivation.

4.  **Interaction Flow:**
    *   The `VolumetricFogSystem` initializes as a singleton.
    *   `VolumetricFogSettings` assets are created by designers.
    *   `LocalVolumetricFogVolume` components are added to GameObjects, configured with specific colliders and local fog settings. They automatically register themselves with the `VolumetricFogSystem` when active.
    *   Every frame (or on demand), the `VolumetricFogSystem` calculates the "effective" fog properties for the current camera position (or any queried position). It starts with the global settings and then iterates through all registered `LocalVolumetricFogVolume`s. If the query position is inside a volume, it blends the local volume's settings with the global ones.
    *   For demonstration, the `VolumetricFogSystem` applies these effective settings to Unity's built-in `RenderSettings.fog`. In a real volumetric fog system, these properties would feed into a custom rendering pipeline (URP/HDRP custom renderer feature, post-processing shader, etc.).

---

## Complete C# Unity Example:

To use this example, create three C# scripts and one ScriptableObject asset:

1.  `VolumetricFogSettings.cs`
2.  `VolumetricFogSystem.cs`
3.  `LocalVolumetricFogVolume.cs`

### 1. `VolumetricFogSettings.cs`

This ScriptableObject defines the properties for a fog preset.

```csharp
using UnityEngine;

/// <summary>
/// [VolumetricFogSystem Pattern] - VolumetricFogSettings (ScriptableObject)
///
/// Purpose:
/// Defines a reusable set of volumetric fog properties. This allows designers to create
/// different fog presets (e.g., "Dense Fog", "Light Mist", "Swamp Haze") as assets.
/// These assets can then be referenced by the global VolumetricFogSystem or by
/// individual LocalVolumetricFogVolume components.
///
/// Pattern Relevance:
/// - Data-Driven Configuration: Separates configuration data from logic.
/// - Reusability: Multiple fog systems or local volumes can reference the same settings.
/// - Designer-Friendly: Easily adjustable in the Inspector without touching code.
/// </summary>
[CreateAssetMenu(fileName = "NewVolumetricFogSettings", menuName = "Volumetric Fog/Fog Settings")]
public class VolumetricFogSettings : ScriptableObject
{
    [Header("Basic Fog Properties")]
    [Tooltip("The color of the fog.")]
    public Color fogColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);

    [Tooltip("The overall density/intensity of the fog.")]
    [Range(0.0f, 1.0f)]
    public float density = 0.05f;

    [Tooltip("The distance at which the fog starts to appear.")]
    public float startDistance = 0.0f;

    [Tooltip("The distance at which the fog reaches its maximum density.")]
    public float endDistance = 200.0f;

    [Tooltip("How quickly the fog density falls off with height (0 = uniform, higher = more vertical falloff).")]
    [Range(0.0f, 1.0f)]
    public float heightFalloff = 0.2f;

    [Tooltip("The base world height for vertical fog effects.")]
    public float baseHeight = 0.0f;

    [Header("Volumetric Noise Properties (for advanced rendering)")]
    [Tooltip("Texture used for adding volumetric noise to the fog (e.g., a 3D noise texture).")]
    public Texture3D noiseTexture;

    [Tooltip("The intensity of the noise effect on fog density.")]
    [Range(0.0f, 2.0f)]
    public float noiseIntensity = 0.5f;

    [Tooltip("The scale of the noise texture in world space.")]
    public Vector3 noiseScale = new Vector3(0.1f, 0.1f, 0.1f);

    [Tooltip("The speed and direction the noise scrolls.")]
    public Vector3 noiseScrollSpeed = new Vector3(0.01f, 0.01f, 0.01f);

    // Add other properties as needed for your specific volumetric fog implementation
    // e.g., wind direction, light scattering properties, etc.

    /// <summary>
    /// Linearly interpolates between two VolumetricFogSettings objects.
    /// This is useful for blending global fog with local volumes, or transitioning
    /// between different fog states.
    /// </summary>
    public static VolumetricFogSettings Lerp(VolumetricFogSettings a, VolumetricFogSettings b, float t)
    {
        // Create a temporary settings object to hold the blended values.
        // In a real system, you might not create a new object every time,
        // but rather apply values to an existing "current settings" object.
        VolumetricFogSettings blended = ScriptableObject.CreateInstance<VolumetricFogSettings>();
        blended.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
        blended.density = Mathf.Lerp(a.density, b.density, t);
        blended.startDistance = Mathf.Lerp(a.startDistance, b.startDistance, t);
        blended.endDistance = Mathf.Lerp(a.endDistance, b.endDistance, t);
        blended.heightFalloff = Mathf.Lerp(a.heightFalloff, b.heightFalloff, t);
        blended.baseHeight = Mathf.Lerp(a.baseHeight, b.baseHeight, t);

        blended.noiseTexture = t < 0.5f ? a.noiseTexture : b.noiseTexture; // Texture lerp is tricky, pick one
        blended.noiseIntensity = Mathf.Lerp(a.noiseIntensity, b.noiseIntensity, t);
        blended.noiseScale = Vector3.Lerp(a.noiseScale, b.noiseScale, t);
        blended.noiseScrollSpeed = Vector3.Lerp(a.noiseScrollSpeed, b.noiseScrollSpeed, t);
        
        return blended;
    }
}
```

### 2. `VolumetricFogSystem.cs`

This is the central manager, acting as a Singleton.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For ordering the volumes

/// <summary>
/// [VolumetricFogSystem Pattern] - VolumetricFogSystem (MonoBehaviour, Singleton)
///
/// Purpose:
/// The central manager for all volumetric fog operations. It holds the global fog settings,
/// manages a list of active local fog volumes, and provides a unified API for other
/// systems (e.g., the main camera's rendering pipeline) to query the effective fog properties
/// at any given world position.
///
/// This script also demonstrates how the system *would* apply settings to Unity's
/// built-in fog for immediate visual feedback, though a real volumetric fog system
/// would feed these properties into a custom shader or render feature.
///
/// Pattern Relevance:
/// - Singleton: Ensures only one instance exists, providing a global access point.
/// - Service Locator: Other game systems can "locate" this manager to get current fog settings.
/// - Orchestrator: Coordinates global settings with contributions/overrides from local volumes.
/// </summary>
public class VolumetricFogSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    public static VolumetricFogSystem Instance { get; private set; }

    [Header("Global Fog Settings")]
    [Tooltip("The default or global volumetric fog settings used when no local volumes are active or overriding.")]
    [SerializeField]
    private VolumetricFogSettings globalSettings;

    [Tooltip("When true, the system will apply calculated fog settings to Unity's built-in RenderSettings.fog properties. " +
             "Set to false if you're using a custom rendering solution (URP/HDRP post-process, custom shader).")]
    [SerializeField]
    private bool applyToBuiltInUnityFog = true;

    // List of currently active local fog volumes that can influence the fog.
    // The system will iterate through these to determine the effective fog properties.
    private List<LocalVolumetricFogVolume> activeVolumes = new List<LocalVolumetricFogVolume>();

    // Stores the currently calculated effective fog settings for convenience.
    // This would be consumed by a rendering system.
    public VolumetricFogSettings CurrentEffectiveSettings { get; private set; }

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("VolumetricFogSystem: Duplicate instance detected, destroying new one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, don't destroy this object when loading new scenes.
        // If you want a persistent fog system across scenes: DontDestroyOnLoad(gameObject);

        InitializeSystem();
    }

    private void OnEnable()
    {
        // Re-initialize if the object was disabled and re-enabled, especially useful if using DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            InitializeSystem();
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // For demonstration, we assume the camera is the primary viewer.
        // In a real game, you might query for multiple cameras or specific viewpoints.
        if (Camera.main != null)
        {
            Vector3 cameraPosition = Camera.main.transform.position;
            CurrentEffectiveSettings = GetEffectiveFogProperties(cameraPosition);

            if (applyToBuiltInUnityFog)
            {
                ApplySettingsToBuiltInUnityFog(CurrentEffectiveSettings);
            }
        }
        else if (applyToBuiltInUnityFog && RenderSettings.fog)
        {
            // If main camera is not found, default to global settings for built-in fog
            ApplySettingsToBuiltInUnityFog(globalSettings);
        }
    }

    // --- System Initialization and Configuration ---
    private void InitializeSystem()
    {
        if (globalSettings == null)
        {
            Debug.LogError("VolumetricFogSystem: No Global Fog Settings assigned! Please assign a 'VolumetricFogSettings' ScriptableObject.", this);
            // Create a default one for safety if none is assigned
            globalSettings = ScriptableObject.CreateInstance<VolumetricFogSettings>();
            globalSettings.name = "Default Global Fog Settings (Generated)";
        }

        // Initially set current effective settings to global
        CurrentEffectiveSettings = globalSettings;

        // Apply global settings to built-in fog if enabled, ensuring fog is configured from the start.
        if (applyToBuiltInUnityFog)
        {
            ApplySettingsToBuiltInUnityFog(globalSettings);
        }
        else
        {
            // If not using built-in fog, ensure it's disabled to avoid conflicts.
            RenderSettings.fog = false;
        }

        Debug.Log("VolumetricFogSystem initialized.");
    }

    /// <summary>
    /// Sets a new global fog settings asset. This allows dynamic changes to the overall fog.
    /// </summary>
    /// <param name="newSettings">The new VolumetricFogSettings ScriptableObject to use globally.</param>
    public void SetGlobalFogSettings(VolumetricFogSettings newSettings)
    {
        if (newSettings == null)
        {
            Debug.LogError("VolumetricFogSystem: Attempted to set null global settings.", this);
            return;
        }
        globalSettings = newSettings;
        Debug.Log($"VolumetricFogSystem: Global settings updated to '{globalSettings.name}'.");
    }

    // --- Local Volume Management ---
    /// <summary>
    /// Registers a LocalVolumetricFogVolume with the system.
    /// Called automatically by LocalVolumetricFogVolume's OnEnable.
    /// </summary>
    /// <param name="volume">The LocalVolumetricFogVolume to register.</param>
    public void RegisterVolume(LocalVolumetricFogVolume volume)
    {
        if (!activeVolumes.Contains(volume))
        {
            activeVolumes.Add(volume);
            // Sort volumes by their blending priority or other criteria if needed.
            // For simplicity, we'll process them in the order they are added or a default.
            // activeVolumes = activeVolumes.OrderBy(v => v.priority).ToList(); // Example if you add a priority field
            Debug.Log($"VolumetricFogSystem: Registered local fog volume: {volume.name}");
        }
    }

    /// <summary>
    /// Unregisters a LocalVolumetricFogVolume from the system.
    /// Called automatically by LocalVolumetricFogVolume's OnDisable.
    /// </summary>
    /// <param name="volume">The LocalVolumetricFogVolume to unregister.</param>
    public void UnregisterVolume(LocalVolumetricFogVolume volume)
    {
        if (activeVolumes.Remove(volume))
        {
            Debug.Log($"VolumetricFogSystem: Unregistered local fog volume: {volume.name}");
        }
    }

    // --- Public API for Fog Property Queries ---
    /// <summary>
    /// Calculates and returns the effective volumetric fog properties at a given world position.
    /// This is the core logic that combines global settings with any influencing local volumes.
    /// </summary>
    /// <param name="queryPosition">The world position to query fog properties for.</param>
    /// <returns>A VolumetricFogSettings object representing the combined fog properties.</returns>
    public VolumetricFogSettings GetEffectiveFogProperties(Vector3 queryPosition)
    {
        // Start with global settings as the base.
        VolumetricFogSettings effectiveSettings = globalSettings;

        // Iterate through all active local volumes to find influential ones.
        // For simplicity, we'll just apply the first (or most prominent) override.
        // A more complex system might blend multiple overlapping volumes.
        foreach (var volume in activeVolumes)
        {
            if (volume.Contains(queryPosition))
            {
                // If inside a volume, blend its settings.
                // The 'GetLocalFogProperties' method handles blending based on proximity within the volume.
                effectiveSettings = volume.GetLocalFogProperties(queryPosition, effectiveSettings);
                // For a simple override, you might just use: effectiveSettings = volume.localOverrideSettings;
            }
        }

        return effectiveSettings;
    }

    // --- Built-in Unity Fog Integration (Demonstration Only) ---
    /// <summary>
    /// Applies the given VolumetricFogSettings to Unity's built-in RenderSettings.fog properties.
    /// This is for demonstration purposes only, to provide immediate visual feedback.
    /// A real volumetric fog system would use these settings to drive custom shaders or rendering.
    /// </summary>
    /// <param name="settings">The VolumetricFogSettings to apply.</param>
    private void ApplySettingsToBuiltInUnityFog(VolumetricFogSettings settings)
    {
        if (settings == null) return;

        RenderSettings.fog = true; // Ensure built-in fog is enabled
        RenderSettings.fogColor = settings.fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared; // A common mode for dense fog
        RenderSettings.fogDensity = settings.density;
        RenderSettings.fogStartDistance = settings.startDistance;
        RenderSettings.fogEndDistance = settings.endDistance;

        // Built-in fog doesn't directly support height falloff or noise textures.
        // These properties would be used by a custom shader.
        // Debug.Log($"Applied fog: Color={settings.fogColor}, Density={settings.density}, Start={settings.startDistance}, End={settings.endDistance}");
    }
}
```

### 3. `LocalVolumetricFogVolume.cs`

Attach this to GameObjects to define local fog zones.

```csharp
using UnityEngine;

/// <summary>
/// [VolumetricFogSystem Pattern] - LocalVolumetricFogVolume (MonoBehaviour)
///
/// Purpose:
/// Defines a specific area in the world where volumetric fog properties should
/// deviate from or override the global settings managed by the VolumetricFogSystem.
/// It requires a Collider component to define its boundaries (e.g., BoxCollider, SphereCollider).
///
/// Pattern Relevance:
/// - Component: Integrates seamlessly into Unity's GameObject architecture.
/// - Strategy (implicit): Provides a "strategy" (local settings) that the
///   VolumetricFogSystem uses when calculating effective fog properties.
/// - Registrar/Observer: Automatically registers and unregisters itself with the
///   VolumetricFogSystem when enabled/disabled, ensuring the manager is always aware
///   of active local fog zones.
/// </summary>
[RequireComponent(typeof(Collider))] // Local volumes need a collider to define their shape
public class LocalVolumetricFogVolume : MonoBehaviour
{
    [Header("Local Fog Settings")]
    [Tooltip("The specific fog settings for this local volume. If left null, it will modify global settings based on multipliers.")]
    public VolumetricFogSettings localOverrideSettings;

    [Tooltip("How far into the volume the blending effect takes place. 0 means instant change at boundary, higher means smoother transition.")]
    public float blendDistance = 5.0f;

    [Header("Overrides & Multipliers (if localOverrideSettings is null)")]
    [Tooltip("Multiplier for global fog density within this volume.")]
    [Range(0.0f, 5.0f)]
    public float densityMultiplier = 1.0f;

    [Tooltip("Multiplier for global fog height falloff within this volume.")]
    [Range(0.0f, 5.0f)]
    public float heightFalloffMultiplier = 1.0f;

    [Tooltip("If true, this volume will completely override the global fog color. Otherwise, global color is used or multiplied.")]
    public bool overrideFogColor = false;
    public Color customFogColor = Color.white;

    private Collider volumeCollider;

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        volumeCollider = GetComponent<Collider>();
        if (volumeCollider == null)
        {
            Debug.LogError($"LocalVolumetricFogVolume on {name} requires a Collider component!", this);
            enabled = false;
            return;
        }
        // Ensure the collider is a trigger, so it doesn't block physics.
        volumeCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (VolumetricFogSystem.Instance != null)
        {
            VolumetricFogSystem.Instance.RegisterVolume(this);
        }
        else
        {
            Debug.LogWarning("LocalVolumetricFogVolume: VolumetricFogSystem instance not found. Make sure it's in the scene.", this);
        }
    }

    private void OnDisable()
    {
        if (VolumetricFogSystem.Instance != null)
        {
            VolumetricFogSystem.Instance.UnregisterVolume(this);
        }
    }

    /// <summary>
    /// Checks if a world position is within this local fog volume's collider.
    /// </summary>
    /// <param name="position">The world position to check.</param>
    /// <returns>True if the position is inside the volume, false otherwise.</returns>
    public bool Contains(Vector3 position)
    {
        // Use the collider's bounds for a quick initial check, then a more precise check if needed.
        if (volumeCollider.bounds.Contains(position))
        {
            // For complex colliders (MeshCollider that isn't convex), this might not be perfectly accurate.
            // For BoxCollider/SphereCollider, it's generally fine.
            return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates the effective fog properties by blending global settings with this volume's local overrides.
    /// The blending is based on the query position's proximity to the volume's edge (using blendDistance).
    /// </summary>
    /// <param name="queryPosition">The world position within or near the volume.</param>
    /// <param name="baseSettings">The current base settings (usually global settings) to blend from.</param>
    /// <returns>A new VolumetricFogSettings object with blended properties.</returns>
    public VolumetricFogSettings GetLocalFogProperties(Vector3 queryPosition, VolumetricFogSettings baseSettings)
    {
        if (baseSettings == null) return null; // Should not happen if system is initialized correctly

        // Calculate blend factor based on distance from the volume's edge.
        // We want 0 at the edge, and 1 deeper inside the volume (past blendDistance).
        float distanceToCenter = Vector3.Distance(transform.position, queryPosition);
        float normalizedDistance = 0f;

        // This blending logic needs to be tailored to the collider type.
        // For a sphere, it's distance to center vs radius. For a box, it's distance to bounds.
        // For simplicity, let's approximate with a generic distance from bounds.
        float closestPointDistance = Vector3.Distance(queryPosition, volumeCollider.ClosestPoint(queryPosition));
        float blendFactor = 0f;

        // If 'localOverrideSettings' is provided, we lerp directly to those.
        // Otherwise, we apply multipliers to the 'baseSettings'.
        if (localOverrideSettings != null)
        {
            // Calculate how deep the query position is into the volume
            // Distance from closest point on bounds to the actual query position.
            // A point right on the boundary has distance 0.
            // A point blendDistance units inside has blend factor 1.
            blendFactor = Mathf.Clamp01(closestPointDistance / blendDistance);
            return VolumetricFogSettings.Lerp(baseSettings, localOverrideSettings, blendFactor);
        }
        else
        {
            // If no specific override settings, apply multipliers to the base settings.
            // This is useful for subtle local adjustments like 'slightly denser here'.
            VolumetricFogSettings modifiedSettings = ScriptableObject.CreateInstance<VolumetricFogSettings>();
            modifiedSettings.fogColor = overrideFogColor ? customFogColor : baseSettings.fogColor;
            modifiedSettings.density = baseSettings.density * densityMultiplier;
            modifiedSettings.startDistance = baseSettings.startDistance; // Not applying multiplier here for simplicity
            modifiedSettings.endDistance = baseSettings.endDistance;     // Not applying multiplier here for simplicity
            modifiedSettings.heightFalloff = baseSettings.heightFalloff * heightFalloffMultiplier;
            modifiedSettings.baseHeight = baseSettings.baseHeight;

            // Texture properties are usually not multiplied, but picked or kept.
            modifiedSettings.noiseTexture = baseSettings.noiseTexture;
            modifiedSettings.noiseIntensity = baseSettings.noiseIntensity;
            modifiedSettings.noiseScale = baseSettings.noiseScale;
            modifiedSettings.noiseScrollSpeed = baseSettings.noiseScrollSpeed;

            // Apply blend factor to the modified settings.
            blendFactor = Mathf.Clamp01(closestPointDistance / blendDistance);
            return VolumetricFogSettings.Lerp(baseSettings, modifiedSettings, blendFactor);
        }
    }

    // --- Editor-only Visuals ---
    private void OnDrawGizmos()
    {
        if (volumeCollider == null)
        {
            volumeCollider = GetComponent<Collider>();
            if (volumeCollider == null) return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 0.8f, 0.3f); // Tealish transparent
        Gizmos.matrix = transform.localToWorldMatrix;

        if (volumeCollider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (volumeCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0.2f, 0.8f, 0.8f, 0.8f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        // Add other collider types as needed (e.g., CapsuleCollider)

        // Draw blend distance
        if (blendDistance > 0 && volumeCollider is SphereCollider sphereBlend)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(sphereBlend.center, sphereBlend.radius - blendDistance);
        }
        else if (blendDistance > 0 && volumeCollider is BoxCollider boxBlend)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 0.5f, 0.2f);
            Gizmos.DrawWireCube(boxBlend.center, boxBlend.size - Vector3.one * blendDistance * 2);
        }

        Gizmos.matrix = Matrix4x4.identity; // Reset Gizmos matrix
    }
}
```

---

## How to Set Up and Use in Unity:

1.  **Create Scripts:** Copy the three C# code blocks into three separate files named `VolumetricFogSettings.cs`, `VolumetricFogSystem.cs`, and `LocalVolumetricFogVolume.cs` in your Unity project's Assets folder.

2.  **Create Global Fog Settings:**
    *   In your Project window, right-click -> `Create` -> `Volumetric Fog` -> `Fog Settings`.
    *   Name it something like "GlobalFogDefaults".
    *   Select this asset and adjust its `Fog Color`, `Density`, `Start Distance`, `End Distance`, etc., in the Inspector. These will be your default world fog properties.

3.  **Create VolumetricFogSystem GameObject:**
    *   Create an empty GameObject in your scene (e.g., named `VolumetricFogManager`).
    *   Add the `VolumetricFogSystem` component to it.
    *   Drag your "GlobalFogDefaults" ScriptableObject from the Project window into the `Global Settings` field of the `VolumetricFogSystem` component.
    *   Ensure `Apply To Built-In Unity Fog` is checked to see immediate effects.

4.  **Create Local Fog Volumes:**
    *   Create an empty GameObject (e.g., `DenseFogZone`).
    *   Add a `Box Collider` or `Sphere Collider` component to it. Make sure `Is Trigger` is checked. Adjust its size/radius to define the area.
    *   Add the `LocalVolumetricFogVolume` component to it.
    *   **Option A (Full Override):**
        *   Right-click in your Project window -> `Create` -> `Volumetric Fog` -> `Fog Settings`. Name it "DenseFogOverride".
        *   Adjust its properties (e.g., higher density, darker color).
        *   Drag this "DenseFogOverride" asset into the `Local Override Settings` field of your `LocalVolumetricFogVolume` component.
        *   Adjust `Blend Distance` for a smoother transition.
    *   **Option B (Multipliers/Partial Override):**
        *   Leave `Local Override Settings` field as `None`.
        *   Adjust `Density Multiplier` (e.g., to 2.0 for double density).
        *   Check `Override Fog Color` and pick a `Custom Fog Color` if you want to change the color without defining a full settings asset.
        *   Adjust `Blend Distance`.
    *   Move this `DenseFogZone` GameObject to a location in your scene.

5.  **Run the Scene:**
    *   As you move your `Main Camera` (or any other camera queried by the system) through the `LocalVolumetricFogVolume`s, you will see the built-in Unity fog properties change dynamically according to the rules defined by your system.
    *   The Gizmos in the Scene view will show the boundaries of your local fog volumes.

---

### How to Extend to a Real Volumetric Fog Shader/Pipeline:

The current implementation uses `RenderSettings.fog` for easy demonstration. For actual volumetric fog:

1.  **Disable Built-in Fog:** Set `VolumetricFogSystem.applyToBuiltInUnityFog` to `false`.
2.  **Custom Post-Processing/Render Feature:**
    *   Create a custom post-processing effect (e.g., using URP/HDRP custom renderer features, or a custom CommandBuffer in built-in pipeline).
    *   In this post-processing script/shader, you would call `VolumetricFogSystem.Instance.GetEffectiveFogProperties(Camera.main.transform.position)` (or pass camera parameters).
    *   Use the returned `VolumetricFogSettings` to drive your volumetric fog shader. You would pass values like `settings.fogColor`, `settings.density`, `settings.noiseTexture`, `settings.noiseIntensity`, etc., as shader uniforms.
    *   Your shader would then calculate the actual volumetric fog based on raymarching through a volume texture or other techniques.

**Example of an external script querying the system:**

```csharp
using UnityEngine;

public class MyCustomFogEffect : MonoBehaviour
{
    private VolumetricFogSettings currentFogSettings;

    // A dummy method that would represent part of a render feature or post-process effect
    void OnPreRender()
    {
        if (VolumetricFogSystem.Instance != null)
        {
            // Query the system for the effective fog properties at the camera's position
            currentFogSettings = VolumetricFogSystem.Instance.GetEffectiveFogProperties(transform.position);

            // Now, 'currentFogSettings' contains the blended global and local fog properties.
            // You would pass these to your custom volumetric fog shader.

            // Example: Logging properties (replace with shader uniform setting)
            // Debug.Log($"Custom Fog: Color={currentFogSettings.fogColor}, Density={currentFogSettings.density}, Noise={currentFogSettings.noiseIntensity}");

            // Example of setting shader properties (assuming you have a material with a custom fog shader)
            // if (myFogMaterial != null)
            // {
            //     myFogMaterial.SetColor("_FogColor", currentFogSettings.fogColor);
            //     myFogMaterial.SetFloat("_FogDensity", currentFogSettings.density);
            //     myFogMaterial.SetTexture("_NoiseTexture", currentFogSettings.noiseTexture);
            //     myFogMaterial.SetFloat("_NoiseIntensity", currentFogSettings.noiseIntensity);
            //     // ... and so on for all relevant properties
            // }
        }
    }
}
```