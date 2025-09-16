// Unity Design Pattern Example: GrassRenderingSystem
// This script demonstrates the GrassRenderingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates a **GrassRenderingSystem** pattern, focusing on efficient rendering of large quantities of grass using GPU instancing, culling, and a structured design.

The system is broken down into the following components:

1.  **`GrassType.cs` (ScriptableObject):** Defines the visual properties of a single type of grass (mesh, material, scale, color, wind).
2.  **`GrassConfig.cs` (ScriptableObject):** Defines the global settings for the grass system (generation area, density, render distance, LODs).
3.  **`GrassPatch.cs` (C# Class):** Represents a logical region of grass, holding the transform data for its blades and its spatial bounds. This is a helper class, not a MonoBehaviour.
4.  **`GrassRenderingSystem.cs` (MonoBehaviour):** The core manager that orchestrates grass generation, culling, and drawing using `Graphics.DrawMeshInstanced`.
5.  **`GrassInstanced.shader`:** A custom shader designed to work with GPU instancing, supporting color variation, basic wind, and LOD fading.

---

### Step-by-Step Implementation Guide in Unity:

1.  **Create Folders:** In your Unity Project window, create the following folders: `Assets/Scripts/GrassSystem`, `Assets/Shaders`, `Assets/Meshes`, `Assets/Materials`.
2.  **Create Grass Blade Mesh:**
    *   Right-click in `Assets/Meshes` -> Create -> 3D Object -> Quad.
    *   Rename it to `GrassBlade_Quad1`.
    *   In the Inspector, set its position to `(0, 0, 0)`.
    *   Scale it to be tall and thin, e.g., `(0.5, 2, 0.1)`.
    *   Duplicate `GrassBlade_Quad1` (Ctrl+D or Cmd+D). Rename it `GrassBlade_Quad2`.
    *   Select `GrassBlade_Quad2`. In the Inspector, set its Y rotation to `90`.
    *   Select both `GrassBlade_Quad1` and `GrassBlade_Quad2` in the Hierarchy.
    *   Go to `GameObject` -> `Combine Meshes`. A new GameObject `Combined Mesh` will appear.
    *   Drag this `Combined Mesh` from the Hierarchy into `Assets/Meshes` to create a prefab.
    *   Select the newly created prefab, e.g., `Combined Mesh.prefab`. In its Inspector, expand the Mesh Filter component. Drag the mesh asset (e.g., `Combined Mesh (Mesh)`) from the Mesh Filter to a location in `Assets/Meshes` to save it as a standalone mesh asset. Rename this mesh asset to `CrossPlaneGrassMesh`.
    *   You can now delete the `Combined Mesh` GameObject and its children from the Hierarchy.
3.  **Create Grass Material:**
    *   Right-click in `Assets/Materials` -> Create -> Material.
    *   Name it `GrassInstancedMat`.
    *   In the Inspector for `GrassInstancedMat`:
        *   Click the "Shader" dropdown and select `Custom/GrassInstanced`.
        *   **Crucially, ensure the "GPU Instancing" checkbox is ticked.**
        *   (Optional) Assign a grass blade texture to the `_MainTex` slot (you can find free ones online or create a simple one with an alpha channel).
4.  **Create GrassType ScriptableObject:**
    *   Right-click in `Assets/Scripts/GrassSystem` -> Create -> Grass System -> Grass Type.
    *   Name it `MyGrassType`.
    *   In its Inspector:
        *   Drag your `CrossPlaneGrassMesh` from `Assets/Meshes` into the `Grass Mesh` field.
        *   Drag your `GrassInstancedMat` from `Assets/Materials` into the `Grass Material` field.
        *   Adjust `Min Scale`, `Max Scale`, `Base Color`, `Color Variation`, `Wind Strength` as desired.
5.  **Create GrassConfig ScriptableObject:**
    *   Right-click in `Assets/Scripts/GrassSystem` -> Create -> Grass System -> Grass Config.
    *   Name it `MyGrassConfig`.
    *   In its Inspector:
        *   Drag your `MyGrassType` into the `Grass Type` field.
        *   Adjust `Area Size` (e.g., `(200, 200)`), `Patch Size` (e.g., `15`), `Grass Per Patch` (e.g., `1500`), `Render Distance` (e.g., `80`), `LOD Fade Distance` (e.g., `40`).
        *   Ensure `Generate On Start` is checked.
6.  **Create GrassRenderingSystem GameObject:**
    *   In your scene, create an empty GameObject (Right-click in Hierarchy -> Create Empty).
    *   Name it `GrassManager`.
    *   Add the `GrassRenderingSystem` component to `GrassManager` (click "Add Component" and search for it).
    *   In the Inspector for `GrassManager`:
        *   Drag your `MyGrassConfig` from `Assets/Scripts/GrassSystem` into the `System Config` field.
7.  **Run the Scene:** Press Play. You should now see a vast field of procedurally generated grass rendered efficiently! You can also click the "Generate Grass" button on the `GrassRenderingSystem` component in the Inspector while in Editor mode to preview the grass without running the game.

---

### 1. `GrassType.cs`

This ScriptableObject defines the properties for a single visual type of grass blade.

```csharp
// GrassType.cs
using UnityEngine;

/// <summary>
/// Defines the visual properties for a specific type of grass blade.
/// This includes its mesh, material, scale range, color, and wind interaction.
/// </summary>
[CreateAssetMenu(fileName = "NewGrassType", menuName = "Grass System/Grass Type", order = 1)]
public class GrassType : ScriptableObject
{
    [Tooltip("The mesh used for a single grass blade (e.g., a simple cross-plane quad).")]
    public Mesh grassMesh;

    [Tooltip("The material used for rendering the grass. IMPORTANT: It MUST have GPU Instancing enabled.")]
    public Material grassMaterial;

    [Header("Scale & Color")]
    [Range(0.1f, 5.0f)]
    [Tooltip("Minimum random scale for a grass blade.")]
    public float minScale = 0.5f;

    [Range(0.1f, 5.0f)]
    [Tooltip("Maximum random scale for a grass blade.")]
    public float maxScale = 1.5f;

    [Tooltip("Base color for the grass.")]
    public Color baseColor = new Color(0.2f, 0.8f, 0.2f, 1.0f);

    [Tooltip("Random color variation added to the base color for each blade.")]
    public Color colorVariation = new Color(0.1f, 0.1f, 0.1f, 0f); // RGB for variation, A unused

    [Header("Wind & Shadows")]
    [Range(0f, 1f)]
    [Tooltip("Strength of the wind effect applied to grass blades (requires shader support).")]
    public float windStrength = 0.5f;

    [Tooltip("How this grass type casts shadows.")]
    public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

    [Tooltip("Whether this grass type receives shadows from other objects.")]
    public bool receiveShadows = true;
}
```

### 2. `GrassConfig.cs`

This ScriptableObject holds the global configuration settings for the entire grass rendering system, including generation parameters and rendering distances.

```csharp
// GrassConfig.cs
using UnityEngine;

/// <summary>
/// Defines global settings for the Grass Rendering System, including generation parameters
/// and rendering distances.
/// </summary>
[CreateAssetMenu(fileName = "NewGrassConfig", menuName = "Grass System/Grass Config", order = 0)]
public class GrassConfig : ScriptableObject
{
    [Tooltip("The specific type of grass to be rendered with this configuration.")]
    public GrassType grassType;

    [Header("Grass Generation Settings")]
    [Tooltip("The XZ dimensions (width, depth) of the square area where grass will be generated.")]
    public Vector2 areaSize = new Vector2(100, 100);

    [Tooltip("The side length of a square 'patch' of grass. Smaller patches allow for finer-grained culling.")]
    public float patchSize = 10f;

    [Tooltip("The target number of grass blades to attempt to place within each patch.")]
    public int grassPerPatch = 1000;

    [Range(0.01f, 1f)]
    [Tooltip("Scale for Perlin noise used to create density variations in grass placement.")]
    public float noiseScale = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("Minimum Perlin noise value for grass to be placed at a specific spot.")]
    public float densityThreshold = 0.5f;

    [Tooltip("Seed for random number generation, ensuring reproducible grass patterns.")]
    public int randomSeed = 123;

    [Tooltip("If true, grass will be generated automatically when the system starts or on editor changes.")]
    public bool generateOnStart = true;

    [Header("Grass Rendering Settings")]
    [Tooltip("Maximum distance from the camera at which grass patches will be rendered.")]
    public float renderDistance = 60f;

    [Tooltip("Distance from the camera where grass starts to fade out (requires shader support).")]
    public float lodFadeDistance = 30f;

    [Range(0f, 0.5f)]
    [Tooltip("Extra margin added to patch bounds for frustum culling, preventing early culling at screen edges.")]
    public float frustumCullingMargin = 0.1f;

    [Tooltip("Whether to draw gizmos in the editor to visualize grass patches and their visibility.")]
    public bool visualizePatches = true;
}
```

### 3. `GrassPatch.cs`

This helper class represents a localized group of grass blades. It contains the transforms for each blade within its bounds and manages its visibility status for culling.

```csharp
// GrassPatch.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single square patch of grass. It holds the transforms for all grass blades
/// within its boundaries and manages its visibility state for culling.
/// </summary>
public class GrassPatch
{
    /// <summary>World-space bounds of this grass patch.</summary>
    public Bounds bounds;

    /// <summary>List of Matrix4x4 transforms for each grass blade in this patch.</summary>
    public List<Matrix4x4> bladeTransforms;

    /// <summary>True if this patch is currently visible and should be rendered; otherwise, false.</summary>
    public bool isVisible = false;

    private System.Random _random;

    /// <summary>
    /// Initializes a new grass patch.
    /// </summary>
    /// <param name="center">The world-space center point of the patch.</param>
    /// <param name="size">The side length of the square patch.</param>
    /// <param name="randomSeed">The base seed for random generation within this patch.</param>
    public GrassPatch(Vector3 center, float size, int randomSeed)
    {
        // Y-height of bounds is usually arbitrary for flat grass, adjust if sampling terrain height.
        // It should encompass the highest possible grass blade.
        bounds = new Bounds(center, new Vector3(size, size * 2, size)); 
        bladeTransforms = new List<Matrix4x4>();
        // Create a unique seed for each patch for varied generation
        _random = new System.Random(randomSeed + (int)(center.x * 1000 + center.z * 100)); 
    }

    /// <summary>
    /// Generates the grass blade transforms for this patch.
    /// </summary>
    /// <param name="count">The target number of blades to generate.</param>
    /// <param name="minScale">Minimum random scale for blades.</param>
    /// <param name="maxScale">Maximum random scale for blades.</param>
    /// <param name="noiseScale">Scale for Perlin noise for density variation.</param>
    /// <param name="densityThreshold">Minimum noise value to place a blade.</param>
    public void GenerateGrass(int count, float minScale, float maxScale, float noiseScale, float densityThreshold)
    {
        bladeTransforms.Clear();
        Vector3 minBound = bounds.min;
        Vector3 maxBound = bounds.max;

        for (int i = 0; i < count; i++)
        {
            // Random position within the patch's XZ bounds
            float x = (float)(_random.NextDouble() * bounds.size.x) + minBound.x;
            float z = (float)(_random.NextDouble() * bounds.size.z) + minBound.z;

            // Use Perlin noise to create areas of varying grass density
            float noiseVal = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
            if (noiseVal < densityThreshold)
            {
                continue; // Skip placement if noise is below threshold
            }

            // Simple Y position slightly above the base of the patch.
            // In a more advanced system, this would sample actual terrain height.
            float y = bounds.center.y - bounds.extents.y + 0.1f; 

            Vector3 position = new Vector3(x, y, z);

            // Random rotation around the Y-axis for varied orientation
            Quaternion rotation = Quaternion.Euler(0, (float)(_random.NextDouble() * 360f), 0);

            // Random scale within the defined range
            float scale = Mathf.Lerp(minScale, maxScale, (float)_random.NextDouble());
            Vector3 finalScale = new Vector3(scale, scale, scale);

            // Add the generated transform matrix to the list
            bladeTransforms.Add(Matrix4x4.TRS(position, rotation, finalScale));
        }
    }
}
```

### 4. `GrassRenderingSystem.cs`

This is the main MonoBehaviour that drives the entire grass rendering system. It manages generation, culling (frustum and distance), and efficient drawing using `Graphics.DrawMeshInstanced`.

```csharp
// GrassRenderingSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for potential future LINQ operations, currently not strictly needed.

/// <summary>
/// The core MonoBehaviour for the Grass Rendering System.
/// It orchestrates grass generation, culling, and efficient drawing using GPU instancing.
/// </summary>
[ExecuteInEditMode] // Allows grass visualization and generation directly in the editor
[DisallowMultipleComponent] // Ensures only one instance of this system can be on a GameObject
public class GrassRenderingSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField, Tooltip("The ScriptableObject containing all global grass system settings.")]
    private GrassConfig systemConfig;

    // Internal state variables
    private List<GrassPatch> _grassPatches = new List<GrassPatch>();
    private Camera _mainCamera;
    private Plane[] _frustumPlanes = new Plane[6]; // Used for camera frustum culling
    private MaterialPropertyBlock _materialPropertyBlock; // Used to pass per-draw-call properties to the shader

    // Unity's Graphics.DrawMeshInstanced has a limit on the number of instances per call.
    private const int MAX_INSTANCES_PER_BATCH = 1023;
    private List<Matrix4x4> _currentBatchTransforms = new List<Matrix4x4>(MAX_INSTANCES_PER_BATCH);

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the system.
    /// </summary>
    private void OnEnable()
    {
        // Basic validation for essential configurations
        if (systemConfig == null || systemConfig.grassType == null || 
            systemConfig.grassType.grassMesh == null || systemConfig.grassType.grassMaterial == null)
        {
            Debug.LogError("GrassRenderingSystem: System Config, Grass Type, Mesh, or Material not assigned. " +
                           "Please ensure all ScriptableObjects are correctly configured.", this);
            return;
        }

        // Initialize MaterialPropertyBlock if not already done
        if (_materialPropertyBlock == null)
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        // Apply shared material properties from GrassType to the block once.
        // These can be overridden per batch if needed, but for simplicity, we set them once.
        _materialPropertyBlock.SetColor("_BaseColor", systemConfig.grassType.baseColor);
        _materialPropertyBlock.SetColor("_ColorVariation", systemConfig.grassType.colorVariation);
        _materialPropertyBlock.SetFloat("_WindStrength", systemConfig.grassType.windStrength);

        // Find the main camera. In a production game, you might want to assign this explicitly
        // or have a more robust camera management system.
        if (Camera.main != null)
        {
            _mainCamera = Camera.main;
        }
        else
        {
            Debug.LogWarning("GrassRenderingSystem: No Main Camera found. Grass will not render without a camera. " +
                             "Ensure your camera is tagged 'MainCamera'.", this);
        }

        // Generate grass on start if configured and not already generated
        if (systemConfig.generateOnStart && _grassPatches.Count == 0)
        {
            GenerateGrass();
        }
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive. Cleans up resources.
    /// </summary>
    private void OnDisable()
    {
        // For Graphics.DrawMeshInstanced, no specific native resource cleanup (like ComputeBuffers) is required.
        // If using Graphics.DrawMeshInstancedIndirect or custom ComputeBuffers, they would be released here.
        _grassPatches.Clear();
        _currentBatchTransforms.Clear();
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// Used here to trigger grass regeneration when configuration changes.
    /// </summary>
    private void OnValidate()
    {
        // Only regenerate grass in editor mode if configured and changes are detected.
        // This is a simple heuristic; a more robust solution might store hashes of config properties.
        if (systemConfig != null && systemConfig.generateOnStart && (Application.isEditor && !Application.isPlaying))
        {
            if (_grassPatches.Count == 0 || _lastSystemConfigHash != systemConfig.GetHashCode()) 
            {
                GenerateGrass();
                _lastSystemConfigHash = systemConfig.GetHashCode();
            }
        }
    }
    private int _lastSystemConfigHash = 0; // Simple hash to detect config changes in editor

    /// <summary>
    /// Generates all grass patches based on the system configuration.
    /// This method can be called manually in the editor or programmatically.
    /// </summary>
    [ContextMenu("Generate Grass")] // Adds a button to the component's inspector
    public void GenerateGrass()
    {
        if (systemConfig == null || systemConfig.grassType == null)
        {
            Debug.LogError("Cannot generate grass: System Config or Grass Type is null. Please assign them.");
            return;
        }

        _grassPatches.Clear(); // Clear existing patches before generating new ones
        Debug.Log($"Generating grass for area {systemConfig.areaSize.x}x{systemConfig.areaSize.y} with patch size {systemConfig.patchSize}...");

        float halfAreaX = systemConfig.areaSize.x / 2f;
        float halfAreaZ = systemConfig.areaSize.y / 2f; // Using Y for Z-dimension for consistency

        // Calculate number of patches along X and Z dimensions
        int patchCountX = Mathf.CeilToInt(systemConfig.areaSize.x / systemConfig.patchSize);
        int patchCountZ = Mathf.CeilToInt(systemConfig.areaSize.y / systemConfig.patchSize);

        // Iterate through the grid to create individual grass patches
        for (int x = 0; x < patchCountX; x++)
        {
            for (int z = 0; z < patchCountZ; z++)
            {
                // Calculate the world-space center for the current patch
                Vector3 patchCenter = new Vector3(
                    (x * systemConfig.patchSize) - halfAreaX + (systemConfig.patchSize / 2f),
                    transform.position.y, // Patches are centered at the Y level of the GrassRenderingSystem GameObject
                    (z * systemConfig.patchSize) - halfAreaZ + (systemConfig.patchSize / 2f)
                );

                // Create and generate grass for the new patch
                GrassPatch patch = new GrassPatch(patchCenter, systemConfig.patchSize, systemConfig.randomSeed);
                patch.GenerateGrass(systemConfig.grassPerPatch,
                                    systemConfig.grassType.minScale,
                                    systemConfig.grassType.maxScale,
                                    systemConfig.noiseScale,
                                    systemConfig.densityThreshold);
                _grassPatches.Add(patch);
            }
        }
        Debug.Log($"Generated {_grassPatches.Count} grass patches with a total of { _grassPatches.Sum(p => p.bladeTransforms.Count)} blades.");
    }

    /// <summary>
    /// Called once per frame. Performs culling and renders visible grass patches.
    /// </summary>
    private void Update()
    {
        // Early exit if essential components/configurations are missing
        if (_mainCamera == null || systemConfig == null || systemConfig.grassType == null || 
            systemConfig.grassType.grassMesh == null || systemConfig.grassType.grassMaterial == null)
        {
            // Try to find the camera again if it was null
            if (_mainCamera == null && Camera.main != null) _mainCamera = Camera.main;
            return;
        }

        // 1. Culling: Update camera frustum planes
        GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);

        // Clear the batch list for the current frame's rendering
        _currentBatchTransforms.Clear();

        // Pre-calculate squared render distance for efficient comparison
        Vector3 cameraPosition = _mainCamera.transform.position;
        float renderDistanceSq = systemConfig.renderDistance * systemConfig.renderDistance;
        float frustumMargin = systemConfig.frustumCullingMargin;

        // 2. Iterate through all grass patches, perform culling, and collect visible transforms
        foreach (var patch in _grassPatches)
        {
            // Expand patch bounds slightly for frustum culling margin
            // This prevents grass from popping in/out right at the screen edge.
            Bounds cullingBounds = patch.bounds;
            cullingBounds.Expand(cullingBounds.size.magnitude * frustumMargin); // Expand by a percentage of diagonal length

            // Distance culling: Check if patch is beyond max render distance
            if (Vector3.SqrMagnitude(patch.bounds.center - cameraPosition) > renderDistanceSq)
            {
                patch.isVisible = false;
                continue;
            }

            // Frustum culling: Check if patch is outside camera view
            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, cullingBounds))
            {
                patch.isVisible = false;
                continue;
            }

            // If it passes culling, mark as visible and add its blades to the rendering queue
            patch.isVisible = true;

            foreach (var transformMatrix in patch.bladeTransforms)
            {
                _currentBatchTransforms.Add(transformMatrix);

                // If the current batch reaches the Unity instance limit, draw it and start a new batch
                if (_currentBatchTransforms.Count >= MAX_INSTANCES_PER_BATCH)
                {
                    DrawCurrentBatch();
                    _currentBatchTransforms.Clear();
                }
            }
        }

        // Draw any remaining transforms in the last (potentially partial) batch
        if (_currentBatchTransforms.Count > 0)
        {
            DrawCurrentBatch();
        }
    }

    /// <summary>
    /// Executes a Graphics.DrawMeshInstanced call for the current batch of accumulated transforms.
    /// </summary>
    private void DrawCurrentBatch()
    {
        // Calculate LOD fade based on camera distance (requires shader support)
        float distanceToCamera = Vector3.Distance(transform.position, _mainCamera.transform.position);
        float lodFade = 1.0f;
        if (distanceToCamera > systemConfig.lodFadeDistance)
        {
            // Linearly fade out grass between lodFadeDistance and renderDistance
            lodFade = 1.0f - Mathf.Clamp01((distanceToCamera - systemConfig.lodFadeDistance) / 
                                          (systemConfig.renderDistance - systemConfig.lodFadeDistance));
        }

        // Apply per-draw-call properties to the MaterialPropertyBlock
        // This allows changing properties like LOD fade without creating new materials
        _materialPropertyBlock.SetFloat("_LodFade", lodFade); // Shader needs a _LodFade property

        Graphics.DrawMeshInstanced(
            systemConfig.grassType.grassMesh, // The mesh to draw
            0,                                 // Submesh index (0 for most meshes)
            systemConfig.grassType.grassMaterial, // The material to use (MUST have GPU instancing enabled)
            _currentBatchTransforms,           // List of Matrix4x4 transforms for all instances in this batch
            _materialPropertyBlock,            // Custom properties for this draw call
            systemConfig.grassType.shadowCastingMode, // How instances cast shadows
            systemConfig.grassType.receiveShadows,    // Whether instances receive shadows
            0,                                 // Layer (0 for Default)
            _mainCamera                        // Camera to render for (usually Camera.main)
        );
    }

    /// <summary>
    /// Called for drawing gizmos in the editor. Visualizes patches and their visibility.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (systemConfig == null || !systemConfig.visualizePatches)
        {
            return;
        }

        // Visualize the overall generation area
        Gizmos.color = new Color(0, 0.5f, 0, 0.3f); // Semi-transparent green
        Vector3 areaCenter = transform.position;
        // Draw a flat cube for the total area
        Gizmos.DrawCube(areaCenter, new Vector3(systemConfig.areaSize.x, 0.1f, systemConfig.areaSize.y));
        Gizmos.DrawWireCube(areaCenter, new Vector3(systemConfig.areaSize.x, 0.1f, systemConfig.areaSize.y));

        // Visualize individual grass patches
        if (_grassPatches != null)
        {
            foreach (var patch in _grassPatches)
            {
                // Use different colors for visible vs. culled patches
                Gizmos.color = patch.isVisible ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.2f);
                Gizmos.DrawWireCube(patch.bounds.center, patch.bounds.size);
            }
        }
    }
}
```

### 5. `GrassInstanced.shader`

This custom shader is designed to render grass blades with GPU instancing. It supports a base color, color variation per blade (using a trick with world position, ideally this would be a per-instance random value), simple wind animation, and an LOD fade.

```shader
// GrassInstanced.shader
Shader "Custom/GrassInstanced"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2,0.8,0.2,1)
        _ColorVariation ("Color Variation (RGB)", Color) = (0.1,0.1,0.1,0) // RGB for variation, A unused
        _MainTex ("Grass Texture (Alpha Cutout)", 2D) = "white" {}
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.5
        _LodFade ("LOD Fade (0-1)", Range(0, 1)) = 1.0 // For fading out grass based on distance
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" } // AlphaTest for transparent parts of grass texture
        LOD 100

        Pass
        {
            Cull Off // Render both sides of grass quads (important for cross-plane meshes)

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // Enable GPU instancing for this shader
            #pragma instancing_options assumeuniformscaling // Optimizes instancing if all instances have uniform scale

            #include "UnityCG.cginc" // Includes common Unity shader macros and functions
            #include "UnityLightingCommon.cginc" // For basic lighting setup

            // Input structure for vertex shader
            struct appdata
            {
                float4 vertex : POSITION;   // Vertex position in object space
                float2 uv : TEXCOORD0;      // UV coordinates
                float3 normal : NORMAL;     // Vertex normal in object space
                UNITY_VERTEX_INPUT_INSTANCE_ID // Required for instancing; identifies the current instance
            };

            // Output structure from vertex shader to fragment shader
            struct v2f
            {
                float2 uv : TEXCOORD0;      // UV coordinates
                float4 vertex : SV_POSITION; // Clip-space position
                float3 worldNormal : NORMAL; // World-space normal
                float3 worldPos : TEXCOORD1; // World-space position
                fixed4 color : COLOR;       // Per-instance color (Base + Variation)
                float lodFade : TEXCOORD2;  // LOD fade value
                UNITY_VERTEX_OUTPUT_INSTANCE_ID // Required for instancing
            };

            // Shader properties defined in the Properties block
            sampler2D _MainTex;
            float4 _MainTex_ST; // Tiling and offset for _MainTex
            fixed4 _BaseColor;
            fixed4 _ColorVariation;
            float _WindStrength;
            float _LodFade; // Passed from C# MaterialPropertyBlock

            /// <summary>
            /// Vertex Shader: Transforms vertices to clip space, applies wind, and calculates per-instance color.
            /// </summary>
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); // Setup instance ID for this vertex
                UNITY_TRANSFER_INSTANCE_ID(v, o); // Transfer instance ID to fragment shader

                // Apply instance transform (UNITY_MATRIX_M is the instance's world matrix)
                float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);

                // Simple wind effect: Offset grass blade tips slightly based on time and world position
                // This creates a subtle swaying motion.
                float windPhase = (worldPos.x + worldPos.z) * 0.5 + _Time.y; // Use world pos for varying wind effect across terrain
                // Offset scales with vertex height (v.vertex.y) to simulate flexible blades
                float windOffset = sin(windPhase * 2.0) * _WindStrength * v.vertex.y * 0.1; 
                // Apply offset in the XZ plane, normalized by a direction derived from windPhase
                worldPos.xz += windOffset * normalize(float2(cos(windPhase), sin(windPhase))) * worldPos.y; 

                // Transform world position to clip space
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos = worldPos.xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); // Apply texture tiling/offset

                // Calculate world-space normal for lighting
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                // Calculate random color variation per instance.
                // Using world position for a "random" seed. A more robust solution would pass
                // a true random number per instance via a custom vertex attribute or ComputeBuffer.
                float instanceRand = frac(dot(worldPos.xz * 0.1, float2(12.9898, 78.233)));
                fixed4 variation = lerp(0, _ColorVariation, instanceRand);
                o.color = _BaseColor + variation; // Final color for this instance

                o.lodFade = _LodFade; // Pass LOD fade value to fragment shader
                
                return o;
            }

            /// <summary>
            /// Fragment Shader: Samples texture, applies alpha cutout, lighting, and LOD fade.
            /// </summary>
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // Setup instance ID for this fragment

                // Sample the grass texture and apply the per-instance color
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Basic Alpha Cutout: Discard pixels with alpha below a threshold
                // This is how non-rectangular grass blades are achieved.
                if (col.a < 0.5) discard;

                // Simple directional lighting (assuming Unity's main directional light)
                float3 normal = normalize(i.worldNormal);
                // NdotL is the dot product of the normal and light direction, clamped to 0-1
                float NdotL = saturate(dot(normal, _LightDirection0.xyz)); 
                // Final color includes ambient light and diffuse light
                fixed4 finalColor = col * _LightColor0 * NdotL + col * _AmbientLight;

                // Apply LOD Fade (e.g., smoothly fade out by reducing alpha)
                finalColor.a *= i.lodFade;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse" // Fallback shader if this one fails to compile or run
}
```