// Unity Design Pattern Example: VolumetricLighting
// This script demonstrates the VolumetricLighting pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'VolumetricLighting' pattern in Unity, which isn't a traditional GoF (Gang of Four) pattern, but rather an architectural approach to implementing a visual system. Here, it refers to a **Manager-Component-Shader** pattern:

1.  **`VolumetricLightingManager` (Manager/Singleton):** Manages global settings and orchestrates the system, acting as a central point of control.
2.  **`VolumetricLightSource` (Component/Strategy):** Attached to individual light sources, it creates and manages the visual representation (mesh, material) for that specific light's volumetric effect, acting as a strategy for different light types.
3.  **`VolumetricLightBeam.shader` (Shader/Strategy):** The core rendering logic that takes parameters from the C# components and visualizes the volumetric effect.

This combination allows for flexible, localized control over individual light effects while maintaining global consistency and performance optimizations through a manager.

---

### **Instructions for Use:**

1.  **Create the Shader:**
    *   In your Unity project, navigate to `Assets/Shaders` (or any folder).
    *   Right-click -> `Create` -> `Shader` -> `Standard Surface Shader` (or `Unlit Shader`, then replace content).
    *   Rename the new shader to `VolumetricLightBeam`.
    *   **Open the `VolumetricLightBeam` shader and replace its entire content with the shader code provided below.**
    *   Save the shader.

2.  **Create the C# Scripts:**
    *   In your Unity project, navigate to `Assets/Scripts` (or any folder).
    *   Right-click -> `Create` -> `C# Script`.
    *   Rename it to `VolumetricLightingManager`.
    *   **Open `VolumetricLightingManager.cs` and paste the code below into it.**
    *   Save the script.
    *   Repeat this for `VolumetricLightSource.cs`.

3.  **Setup in Scene:**
    *   **Manager:** Create an empty GameObject in your scene (e.g., `VolumetricLightingSystem`). Add the `VolumetricLightingManager` component to it. Configure global settings like `Overall Density`, `Noise Texture`, etc.
    *   **Light Sources:** Add a `VolumetricLightSource` component to any `Light` GameObject (Spotlight recommended for visible beams) you want to have a volumetric effect. Adjust its specific settings like `Volumetric Color`, `Density`, `Scattering`, etc.

**Recommended:** Use a `Directional Light` or `Skybox` for ambient lighting, and add `Spotlights` with `VolumetricLightSource` components for noticeable effects.

---

### **1. `VolumetricLightBeam.shader` (Save this as `Assets/Shaders/VolumetricLightBeam.shader`)**

This shader is crucial. It's a simple unlit shader designed to render transparent geometry (like cones for light beams) and simulate volumetric properties using distance, angle falloff, and noise.

```shader
// ==============================================================================
// VolumetricLightBeam.shader
// Custom shader for rendering volumetric light beams.
// This shader is designed to be used with the VolumetricLightSource C# component.
// It applies color, density, range falloff, spot angle falloff, and noise
// to a transparent mesh (typically a cone for spotlights) to simulate
// a volumetric lighting effect.
// ==============================================================================
Shader "Volumetric/LightBeam"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _Density ("Density Multiplier", Range(0, 5)) = 1
        _NoiseTexture ("Noise Texture (R for density)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1
        _NoiseScrollSpeed ("Noise Scroll Speed (XY)", Vector) = (0,0,0,0)
        _LightAngleFalloff ("Light Angle Falloff Power", Range(0.1, 10)) = 2 // How quickly light fades from center to edge
        _LightRange ("Light Range (World Units)", Float) = 10
        _LightSpotAngle ("Light Spot Angle (Degrees)", Range(0,180)) = 45 // Used for calculating angle falloff
        _OverallDensity ("Global Density Multiplier", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Blending for transparency: SrcAlpha (source alpha) * Source Color + (1 - SrcAlpha) * Destination Color
        Blend SrcAlpha OneMinusSrcAlpha 
        Cull Off // Render both sides of the mesh (so light beam is visible from inside and outside)
        ZWrite Off // Don't write to Z-buffer (ensures objects behind the beam are still visible)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Make fog work with this shader
            #pragma multi_compile_fog

            #include "UnityCG.cginc" // Includes common Unity shader functions and variables

            // Structure for application data (vertex input)
            struct appdata
            {
                float4 vertex : POSITION;   // Vertex position in object space
                float2 uv : TEXCOORD0;      // UV coordinates for texture sampling
            };

            // Structure for vertex to fragment data (interpolated)
            struct v2f
            {
                float4 vertex : SV_POSITION; // Vertex position in clip space
                float3 worldPos : TEXCOORD0; // Vertex position in world space
                float3 localPos : TEXCOORD1; // Vertex position in object space (for falloffs)
                float2 uv : TEXCOORD2;       // UV coordinates
                UNITY_FOG_COORDS(3)          // Fog coordinates
            };

            // Shader properties defined in the Properties block
            fixed4 _MainColor;
            float _Density;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST; // Scale and offset for noise texture
            float _NoiseScale;
            float4 _NoiseScrollSpeed;
            float _LightAngleFalloff;
            float _LightRange;
            float _LightSpotAngle;
            float _OverallDensity;

            // Vertex shader: transforms vertex positions and prepares data for fragment shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform vertex to clip space
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // Transform vertex to world space
                o.localPos = v.vertex.xyz; // Keep local space position for falloff calculations
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTexture); // Apply texture tiling/offset
                UNITY_TRANSFER_FOG(o,o.vertex); // Pass fog coordinates
                return o;
            }

            // Fragment shader: calculates final color for each pixel
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance along the light ray (local Z axis of the cone mesh)
                // The cone is assumed to be oriented such that its tip is at (0,0,0) and extends along +Z.
                float distAlongLight = i.localPos.z;

                // 1. Range Falloff: Fades the beam as it gets further from the light source.
                // Linear falloff, clamped between 0 and 1.
                float rangeFalloff = 1.0 - saturate(distAlongLight / _LightRange);

                // 2. Angle Falloff: Fades the beam as it gets further from the center axis of the spotlight.
                // This uses the local X/Y coordinates to calculate the angle from the Z-axis.
                float2 localXY = i.localPos.xy;
                float angleFromCenter = degrees(atan2(length(localXY), distAlongLight)); // Angle in degrees from the cone's central Z-axis
                
                // Normalize angle relative to half the spot angle (0 at center, 1 at edge)
                float spotAngleRatio = saturate(angleFromCenter / (_LightSpotAngle * 0.5)); 
                
                // Apply a power curve for a smoother falloff effect
                float angleFalloff = pow(1.0 - spotAngleRatio, _LightAngleFalloff);

                // 3. Combine Falloffs: Both distance and angle contribute to the fading of the beam.
                float totalFalloff = rangeFalloff * angleFalloff;

                // 4. Noise Texture: Adds a dynamic, "misty" or "smoky" appearance to the light.
                // Sample the noise texture, scroll it over time, and apply the scale.
                float2 noiseUV = i.uv * _NoiseScale + _Time.y * _NoiseScrollSpeed.xy;
                float noise = tex2D(_NoiseTexture, noiseUV).r;
                
                // Amplify noise effect based on density, making it more pronounced with higher density.
                // Lerp from a neutral gray (0.5) to the actual noise value.
                noise = lerp(0.5, noise, saturate(_Density * 0.1)); 
                noise = pow(noise, 0.8); // Slightly flatten noise distribution

                // 5. Final Alpha Calculation: Determines the transparency of the volumetric effect.
                // Combines falloffs, density, global density, and noise.
                // The '0.05' is a magic number to scale the density into a visible alpha range.
                float alpha = totalFalloff * (_Density * 0.05) * noise * _OverallDensity; 
                alpha = saturate(alpha); // Ensure alpha is between 0 and 1

                fixed4 col = _MainColor;
                col.a *= alpha; // Apply the calculated alpha to the main color

                // Apply scene fog (if enabled in project settings)
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
```

---

### **2. `VolumetricLightingManager.cs`**

This script acts as a global manager for volumetric lighting. It's a singleton, meaning there's only one instance of it in the scene, which centralizes global settings and can be used for system-wide control.

```csharp
// ==============================================================================
// VolumetricLightingManager.cs
// Manages global settings and orchestrates volumetric lighting effects.
// This script implements a Singleton pattern to ensure only one instance exists
// in the scene, providing a central point for configuration and control.
// ==============================================================================
using UnityEngine;
using System.Collections.Generic; // For managing lists of light sources

// ExecuteAlways allows the script to run in Edit Mode, which is useful for
// previewing volumetric light effects directly in the scene view.
[ExecuteAlways]
public class VolumetricLightingManager : MonoBehaviour
{
    // === Singleton Pattern Implementation ===
    // Provides a globally accessible instance of the manager.
    public static VolumetricLightingManager Instance { get; private set; }

    // === Global Volumetric Lighting Settings ===
    // These settings affect all VolumetricLightSource components in the scene.
    [Header("Global Settings")]
    [Tooltip("Overall density multiplier for all volumetric lights.")]
    [Range(0, 2f)]
    public float overallDensity = 1.0f;

    [Tooltip("Noise texture used for volumetric effects, providing a misty/smoky look.")]
    public Texture2D noiseTexture;

    [Tooltip("Global scale of the noise texture.")]
    [Range(0.1f, 10f)]
    public float noiseScale = 1.0f;

    [Tooltip("Speed and direction at which the noise texture scrolls.")]
    public Vector2 noiseScrollSpeed = new Vector2(0.01f, 0.01f);

    [Tooltip("The shader used by all volumetric light sources. Must be 'Volumetric/LightBeam'.")]
    public Shader volumetricBeamShader;

    // List to keep track of all active volumetric light sources.
    // This allows the manager to potentially update them or iterate over them.
    private List<VolumetricLightSource> registeredLightSources = new List<VolumetricLightSource>();

    // Called when the script instance is being loaded.
    private void Awake()
    {
        // Enforce singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("VolumetricLightingManager: Another instance found, destroying this one.", this);
            DestroyImmediate(this); // Use DestroyImmediate for editor mode
            return;
        }
        // Otherwise, this is the valid instance.
        Instance = this;
    }

    // Called when the object becomes enabled and active.
    private void OnEnable()
    {
        // Ensure the singleton is correctly set up, especially after a domain reload in editor.
        if (Instance == null)
        {
            Instance = this;
        }
        InitializeManager();
    }

    // Called when the behaviour becomes disabled or inactive.
    private void OnDisable()
    {
        // If this is the active instance, clear it when disabled.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Called when the script is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // Clean up any references or resources if necessary.
        registeredLightSources.Clear();
    }

    // Initializes manager properties and ensures a default shader/texture is set.
    private void InitializeManager()
    {
        if (noiseTexture == null)
        {
            // Try to load a default noise texture if none is assigned.
            // You might want to create a default simple noise texture and put it in Resources.
            noiseTexture = Resources.Load<Texture2D>("Textures/DefaultVolumetricNoise");
            if (noiseTexture == null)
            {
                Debug.LogWarning("VolumetricLightingManager: No noise texture assigned and no default 'Textures/DefaultVolumetricNoise' found. Please assign one manually.", this);
            }
        }

        if (volumetricBeamShader == null)
        {
            // Try to find the shader by name.
            volumetricBeamShader = Shader.Find("Volumetric/LightBeam");
            if (volumetricBeamShader == null)
            {
                Debug.LogError("VolumetricLightingManager: 'Volumetric/LightBeam' shader not found! Please ensure the shader file exists and is named correctly.", this);
            }
        }
        // Tell all registered lights to update their materials with new global settings.
        UpdateAllLightSources();
    }

    // Registers a VolumetricLightSource with the manager.
    public void RegisterLightSource(VolumetricLightSource source)
    {
        if (!registeredLightSources.Contains(source))
        {
            registeredLightSources.Add(source);
            source.UpdateVolumetricMaterialProperties(); // Ensure new light gets current global settings
            Debug.Log($"VolumetricLightingManager: Registered light source: {source.gameObject.name}");
        }
    }

    // Unregisters a VolumetricLightSource from the manager.
    public void UnregisterLightSource(VolumetricLightSource source)
    {
        if (registeredLightSources.Contains(source))
        {
            registeredLightSources.Remove(source);
            Debug.Log($"VolumetricLightingManager: Unregistered light source: {source.gameObject.name}");
        }
    }

    // Updates the material properties of all registered light sources.
    // This is called when global settings change or when a light is registered.
    public void UpdateAllLightSources()
    {
        foreach (var source in registeredLightSources)
        {
            source.UpdateVolumetricMaterialProperties();
        }
    }

    // Called in the editor when script properties are changed or inspector is updated.
    private void OnValidate()
    {
        // Ensure settings are applied immediately in the editor.
        UpdateAllLightSources();
    }
}
```

---

### **3. `VolumetricLightSource.cs`**

This component is attached to individual `Light` GameObjects. It creates the visual mesh (e.g., a cone for spotlights) and applies the custom shader, passing all necessary parameters to it.

```csharp
// ==============================================================================
// VolumetricLightSource.cs
// Component attached to a Unity Light GameObject to create a volumetric effect.
// This script acts as a 'strategy' for how a light source renders its volume.
// It creates a visual mesh (e.g., cone for spotlights) and dynamically applies
// a shader with properties derived from the light and user settings.
// ==============================================================================
using UnityEngine;
using UnityEngine.Rendering; // For CommandBuffer if needed, or other rendering utilities

// Require a Light component on the same GameObject for this script to function.
[RequireComponent(typeof(Light))]
// ExecuteAlways allows the script to run in Edit Mode, enabling live preview.
[ExecuteAlways]
public class VolumetricLightSource : MonoBehaviour
{
    // === Inspector Settings for this specific volumetric light ===
    [Header("Volumetric Effect Settings")]
    [Tooltip("Color of the volumetric light beam.")]
    public Color volumetricColor = Color.white;

    [Tooltip("Density multiplier for this light's volumetric effect.")]
    [Range(0, 5f)]
    public float density = 1.0f;

    [Tooltip("How quickly the beam fades out towards its edges.")]
    [Range(0.1f, 10f)]
    public float angleFalloffPower = 2.0f;

    // === Internal References ===
    private Light _light;               // The Unity Light component this script is attached to.
    private GameObject _volumetricLightObject; // The GameObject holding the mesh renderer for the beam.
    private MeshRenderer _meshRenderer; // Renderer for the volumetric mesh.
    private MeshFilter _meshFilter;     // Filter for the volumetric mesh.
    private Material _volumetricMaterial; // Material used for the volumetric effect.

    // Shader Property IDs (optimization: cache these once)
    private static readonly int _MainColorID = Shader.PropertyToID("_MainColor");
    private static readonly int _DensityID = Shader.PropertyToID("_Density");
    private static readonly int _NoiseTextureID = Shader.PropertyToID("_NoiseTexture");
    private static readonly int _NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int _NoiseScrollSpeedID = Shader.PropertyToID("_NoiseScrollSpeed");
    private static readonly int _LightAngleFalloffID = Shader.PropertyToID("_LightAngleFalloff");
    private static readonly int _LightRangeID = Shader.PropertyToID("_LightRange");
    private static readonly int _LightSpotAngleID = Shader.PropertyToID("_LightSpotAngle");
    private static readonly int _OverallDensityID = Shader.PropertyToID("_OverallDensity");


    // Called when the script instance is being loaded.
    private void Awake()
    {
        _light = GetComponent<Light>();
    }

    // Called when the object becomes enabled and active.
    private void OnEnable()
    {
        // Ensure the manager exists and register this light source.
        if (VolumetricLightingManager.Instance == null)
        {
            Debug.LogError("VolumetricLightingManager not found in scene. Please add one to a GameObject.", this);
            enabled = false; // Disable this component if manager is missing
            return;
        }
        
        VolumetricLightingManager.Instance.RegisterLightSource(this);
        SetupVolumetricMesh();
    }

    // Called when the behaviour becomes disabled or inactive.
    private void OnDisable()
    {
        // Unregister from the manager.
        if (VolumetricLightingManager.Instance != null)
        {
            VolumetricLightingManager.Instance.UnregisterLightSource(this);
        }
        // Destroy the dynamically created volumetric mesh object.
        if (_volumetricLightObject != null)
        {
            DestroyImmediate(_volumetricLightObject); // Use Immediate for editor
            _volumetricLightObject = null;
            _meshRenderer = null;
            _meshFilter = null;
            _volumetricMaterial = null;
        }
    }

    // LateUpdate is used to ensure all light transformations are finalized before updating the beam.
    private void LateUpdate()
    {
        UpdateVolumetricMaterialProperties();
    }

    // Creates or updates the GameObject and components for the volumetric mesh.
    private void SetupVolumetricMesh()
    {
        if (_volumetricLightObject == null)
        {
            _volumetricLightObject = new GameObject("VolumetricLightBeam_" + _light.name);
            _volumetricLightObject.transform.SetParent(transform); // Parent to the light source
            _volumetricLightObject.transform.localPosition = Vector3.zero;
            _volumetricLightObject.transform.localRotation = Quaternion.identity;
            _volumetricLightObject.transform.localScale = Vector3.one;

            // Add MeshFilter and MeshRenderer
            _meshFilter = _volumetricLightObject.AddComponent<MeshFilter>();
            _meshRenderer = _volumetricLightObject.AddComponent<MeshRenderer>();

            // Create or assign material
            if (VolumetricLightingManager.Instance != null && VolumetricLightingManager.Instance.volumetricBeamShader != null)
            {
                _volumetricMaterial = new Material(VolumetricLightingManager.Instance.volumetricBeamShader);
                _meshRenderer.material = _volumetricMaterial;
            }
            else
            {
                Debug.LogError("VolumetricLightSource: VolumetricLightBeam shader not set on manager or not found.", this);
                enabled = false;
                return;
            }
        }

        // Generate the mesh based on light type. Currently, only Spot is fully supported.
        switch (_light.type)
        {
            case LightType.Spot:
                GenerateConeMesh(_light.range, _light.spotAngle);
                break;
            case LightType.Directional:
                // For directional, you'd typically use a large box or screen-space effect.
                // For this example, we'll disable the mesh as it's not a direct cone.
                if (_meshRenderer != null) _meshRenderer.enabled = false;
                Debug.LogWarning("VolumetricLightSource: Directional lights require a different volumetric mesh setup (e.g., large cube or screen-space). Beam disabled for this type.", this);
                break;
            case LightType.Point:
                // For point light, you'd use a sphere or a collection of quads.
                if (_meshRenderer != null) _meshRenderer.enabled = false;
                Debug.LogWarning("VolumetricLightSource: Point lights require a different volumetric mesh setup (e.g., sphere). Beam disabled for this type.", this);
                break;
            default:
                if (_meshRenderer != null) _meshRenderer.enabled = false;
                break;
        }

        // Ensure the mesh is enabled if it was disabled for unsupported types
        if (_light.type == LightType.Spot && _meshRenderer != null)
        {
            _meshRenderer.enabled = true;
        }
        
        UpdateVolumetricMaterialProperties();
    }

    // Generates a cone mesh suitable for a spotlight beam.
    private void GenerateConeMesh(float length, float angleDegrees)
    {
        if (_meshFilter == null) return;

        int segments = 16; // Number of sides for the cone
        float radius = length * Mathf.Tan(angleDegrees * 0.5f * Mathf.Deg2Rad); // Calculate radius at the far end

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Tip of the cone (light source position)
        vertices.Add(Vector3.zero); // Vertex 0
        uvs.Add(new Vector2(0.5f, 0f));

        // Generate base vertices
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = (float)i / segments * Mathf.PI * 2f;
            float x = radius * Mathf.Sin(currentAngle);
            float y = radius * Mathf.Cos(currentAngle);
            vertices.Add(new Vector3(x, y, length)); // Vertices 1 to segments+1
            uvs.Add(new Vector2((float)i / segments, 1f));
        }

        // Triangles for cone sides
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(0); // Tip vertex
            triangles.Add(i + 2); // Next base vertex
            triangles.Add(i + 1); // Current base vertex
        }

        // Triangles for the base (optional, but good for enclosed volume)
        int baseCenterIndex = vertices.Count;
        vertices.Add(new Vector3(0, 0, length)); // Base center
        uvs.Add(new Vector2(0.5f, 1f));

        for (int i = 0; i < segments; i++)
        {
            triangles.Add(baseCenterIndex);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }
        
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); // Recalculate normals for proper lighting (if shader uses them)
        mesh.RecalculateBounds();

        _meshFilter.mesh = mesh;
    }

    // Updates the material properties of the volumetric mesh.
    // This is called by the manager or locally when settings change.
    public void UpdateVolumetricMaterialProperties()
    {
        if (_volumetricMaterial == null || _light == null || VolumetricLightingManager.Instance == null)
        {
            // Re-setup if material is missing, possibly due to domain reload in editor.
            if (VolumetricLightingManager.Instance != null && _light != null)
            {
                SetupVolumetricMesh();
            }
            return;
        }

        // Apply light's transform to the volumetric object
        if (_volumetricLightObject != null)
        {
            // For spotlights, the mesh points along its local Z-axis (forward)
            // Unity's default spotlight forward is along its local Z-axis.
            _volumetricLightObject.transform.localPosition = Vector3.zero;
            _volumetricLightObject.transform.localRotation = Quaternion.identity;
            _volumetricLightObject.transform.localScale = Vector3.one; // Should be handled by mesh itself
        }

        // Pass properties from this light source to the material
        _volumetricMaterial.SetColor(_MainColorID, volumetricColor * _light.color * _light.intensity);
        _volumetricMaterial.SetFloat(_DensityID, density);
        _volumetricMaterial.SetFloat(_LightAngleFalloffID, angleFalloffPower);
        _volumetricMaterial.SetFloat(_LightRangeID, _light.range);
        _volumetricMaterial.SetFloat(_LightSpotAngleID, _light.spotAngle);

        // Pass global properties from the VolumetricLightingManager to the material
        _volumetricMaterial.SetTexture(_NoiseTextureID, VolumetricLightingManager.Instance.noiseTexture);
        _volumetricMaterial.SetFloat(_NoiseScaleID, VolumetricLightingManager.Instance.noiseScale);
        _volumetricMaterial.SetVector(_NoiseScrollSpeedID, VolumetricLightingManager.Instance.noiseScrollSpeed);
        _volumetricMaterial.SetFloat(_OverallDensityID, VolumetricLightingManager.Instance.overallDensity);
    }

    // Called in the editor when script properties are changed or inspector is updated.
    private void OnValidate()
    {
        // Ensure properties are updated immediately in the editor.
        _light = GetComponent<Light>(); // Ensure _light is assigned in OnValidate
        if (_light != null && enabled && gameObject.activeInHierarchy && _meshRenderer != null)
        {
            SetupVolumetricMesh(); // Re-generate mesh if light properties change
            UpdateVolumetricMaterialProperties();
        }
    }

    // Optional: Draw Gizmos to visualize the volumetric light in the editor.
    private void OnDrawGizmos()
    {
        if (_light == null) return;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = _light.color * 0.5f; // Semi-transparent for visualization

        if (_light.type == LightType.Spot)
        {
            // Draw a cone for spotlight visualization
            Gizmos.DrawFrustum(Vector3.zero, _light.spotAngle, _light.range, 0.0f, 1.0f);
            Gizmos.color = _light.color * 0.8f;
            Gizmos.DrawWireFrustum(Vector3.zero, _light.spotAngle, _light.range, 0.0f, 1.0f);
        }
        Gizmos.matrix = Matrix4x4.identity; // Reset Gizmos matrix
    }
}
```