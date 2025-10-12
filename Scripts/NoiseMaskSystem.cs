// Unity Design Pattern Example: NoiseMaskSystem
// This script demonstrates the NoiseMaskSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "NoiseMaskSystem" design pattern, while not a standard Gang of Four pattern, is a highly practical and commonly used approach in game development, especially for procedural content generation (terrains, textures, object placement, biomes) and visual effects.

**Concept of the NoiseMaskSystem Pattern:**

The core idea is to generate a base value (e.g., height, density, color component) using a **Noise Source**, and then refine or filter that value by applying one or more **Masks**. Each mask defines conditions or regions where the noise should be amplified, diminished, or completely overridden, based on contextual data (like height, slope, distance, or even another texture).

This pattern provides a modular and flexible way to:
1.  **Decompose Complex Logic:** Instead of one monolithic function, break down the generation into smaller, reusable noise and mask components.
2.  **Combine Different Influences:** Easily combine various factors (e.g., Perlin noise, along with height-based and slope-based restrictions).
3.  **Visual Control:** Masks often provide intuitive, artist-friendly controls for procedural generation.
4.  **Runtime Flexibility:** Masks can be added, removed, or reconfigured dynamically at runtime.

**Real-World Use Cases:**

*   **Terrain Biome Generation:** Use noise for the base distribution, then masks for elevation (mountains vs. valleys), temperature (equator vs. poles), or moisture (deserts vs. forests).
*   **Procedural Texture Blending:** Blend different terrain textures (grass, rock, snow, sand) based on noise, height, and slope.
*   **Resource Distribution:** Place resources (ores, trees, specific flora) based on underlying noise patterns, masked by biome type, elevation, or proximity to other features.
*   **Effect Zones:** Define areas where a specific visual effect (e.g., fog, distortion, glow) should be applied, using noise for detail and masks for the boundaries.

---

### Complete C# Unity Example: NoiseMaskSystem for Terrain Weighting

This example demonstrates how to create a `NoiseMaskSystem` to calculate a "weight" at any given point, which can be used for purposes like blending terrain textures (e.g., "rockiness" or "grassiness").

The script includes:
1.  **`BaseNoiseSource`**: An abstract base class for noise generation.
2.  **`PerlinNoiseSource`**: A concrete implementation using `Mathf.PerlinNoise`.
3.  **`BaseMask`**: An abstract base class for defining various masks.
4.  **`HeightRangeMask`**: A mask that limits influence to a specified height range.
5.  **`SlopeMask`**: A mask that limits influence to a specified slope range.
6.  **`TextureMask`**: A mask that uses a `Texture2D`'s red channel to define influence.
7.  **`NoiseMaskSystem`**: The main MonoBehaviour component that orchestrates a noise source and a list of masks, evaluating them to produce a final value.
8.  **Editor Visualization**: A built-in editor visualization that shows the system's output on a plane in the scene, updating dynamically.
9.  **Custom Editor**: A custom Unity Editor script to provide a user-friendly interface for adding masks.

To use `TextureMask`, ensure the `Texture2D` asset's import settings have "Read/Write Enabled" checked.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For [Serializable]

#if UNITY_EDITOR
using UnityEditor;
#endif

// Organize classes within a namespace to prevent naming conflicts
namespace NoiseMasking
{
    /// <summary>
    /// Base abstract class for defining a noise source.
    /// This allows different noise types to be configured in the Inspector using [SerializeReference].
    /// </summary>
    [Serializable] // Essential for Unity to serialize derived classes in lists/arrays
    public abstract class BaseNoiseSource
    {
        /// <summary>
        /// Gets the noise value at a given (x, y) coordinate.
        /// Implementations should typically return a value in the 0-1 range.
        /// </summary>
        /// <param name="x">The X coordinate in world or texture space.</param>
        /// <param name="y">The Y (or Z) coordinate in world or texture space.</param>
        /// <returns>A float noise value, usually between 0 and 1.</returns>
        public abstract float GetNoise(float x, float y);
    }

    /// <summary>
    /// Concrete implementation of Perlin Noise as a noise source.
    /// Uses Unity's built-in Mathf.PerlinNoise function.
    /// </summary>
    [Serializable]
    public class PerlinNoiseSource : BaseNoiseSource
    {
        [Tooltip("The frequency or 'zoom' level of the noise. Higher values mean more detail.")]
        public float scale = 0.05f;

        [Tooltip("An offset applied to the coordinates, useful for getting different noise patterns.")]
        public Vector2 offset = Vector2.zero;

        /// <inheritdoc/>
        public override float GetNoise(float x, float y)
        {
            // Mathf.PerlinNoise returns a value between 0.0 and 1.0.
            return Mathf.PerlinNoise(x * scale + offset.x, y * scale + offset.y);
        }
    }

    /// <summary>
    /// Base abstract class for defining a mask.
    /// Masks modify or filter the noise value based on contextual data.
    /// </summary>
    [Serializable] // Essential for Unity to serialize derived classes in lists/arrays
    public abstract class BaseMask
    {
        /// <summary>
        /// Evaluates the mask at a given world coordinate, taking context data into account.
        /// The return value acts as a multiplier (0-1) for the noise, or a direct contribution.
        /// </summary>
        /// <param name="worldX">World X coordinate.</param>
        /// <param name="worldZ">World Z coordinate.</param>
        /// <param name="noiseValue">The raw noise value before masks are applied (could be -1 to 1).</param>
        /// <param name="terrainHeight">The height of the terrain at this coordinate.</param>
        /// <param name="terrainSlope">The slope of the terrain at this coordinate (in degrees).</param>
        /// <returns>A float value (typically 0-1) representing the mask's influence or filtering factor.</returns>
        public abstract float Evaluate(float worldX, float worldZ, float noiseValue, float terrainHeight, float terrainSlope);
    }

    /// <summary>
    /// A mask that applies influence only within a specified height range.
    /// Provides smooth blending at the range edges.
    /// </summary>
    [Serializable]
    public class HeightRangeMask : BaseMask
    {
        [Tooltip("The minimum height for the mask to have full effect.")]
        public float minHeight = 0f;

        [Tooltip("The maximum height for the mask to have full effect.")]
        public float maxHeight = 100f;

        [Range(0, 50)]
        [Tooltip("The distance over which the mask smoothly transitions in/out of the height range.")]
        public float blendRadius = 10f;

        /// <inheritdoc/>
        public override float Evaluate(float worldX, float worldZ, float noiseValue, float terrainHeight, float terrainSlope)
        {
            float factor = 1.0f; // Full effect by default within range

            // Below min height, blend out
            if (terrainHeight < minHeight)
            {
                // InverseLerp maps terrainHeight from (minHeight - blendRadius) to minHeight, to a 0-1 range.
                // So, if terrainHeight is (minHeight - blendRadius), factor is 0. If terrainHeight is minHeight, factor is 1.
                factor = Mathf.InverseLerp(minHeight - blendRadius, minHeight, terrainHeight);
            }
            // Above max height, blend out
            else if (terrainHeight > maxHeight)
            {
                // InverseLerp maps terrainHeight from maxHeight to (maxHeight + blendRadius), to a 1-0 range.
                // So, if terrainHeight is maxHeight, factor is 1. If terrainHeight is (maxHeight + blendRadius), factor is 0.
                factor = Mathf.InverseLerp(maxHeight + blendRadius, maxHeight, terrainHeight);
            }
            return Mathf.Clamp01(factor); // Ensure factor is between 0 and 1
        }
    }

    /// <summary>
    /// A mask that applies influence based on the terrain's slope.
    /// Provides smooth blending for slopes outside the specified range.
    /// </summary>
    [Serializable]
    public class SlopeMask : BaseMask
    {
        [Range(0, 90)]
        [Tooltip("The minimum slope angle (in degrees) for the mask to have full effect.")]
        public float minSlope = 0f; // Degrees

        [Range(0, 90)]
        [Tooltip("The maximum slope angle (in degrees) for the mask to have full effect.")]
        public float maxSlope = 45f; // Degrees

        [Range(0, 45)]
        [Tooltip("The range (in degrees) over which the mask smoothly transitions in/out of the slope range.")]
        public float blendRadius = 5f; // For smooth transition (in degrees)

        /// <inheritdoc/>
        public override float Evaluate(float worldX, float worldZ, float noiseValue, float terrainHeight, float terrainSlope)
        {
            float factor = 1.0f; // Full effect by default within range

            // Below min slope, blend out
            if (terrainSlope < minSlope)
            {
                factor = Mathf.InverseLerp(minSlope - blendRadius, minSlope, terrainSlope);
            }
            // Above max slope, blend out
            else if (terrainSlope > maxSlope)
            {
                factor = Mathf.InverseLerp(maxSlope + blendRadius, maxSlope, terrainSlope);
            }
            return Mathf.Clamp01(factor); // Ensure factor is between 0 and 1
        }
    }

    /// <summary>
    /// A mask that uses a 2D texture's red channel to define areas of influence.
    /// This allows for custom painted or generated masks.
    /// NOTE: Texture must be set to 'Read/Write Enabled' in its import settings for GetPixelBilinear to work.
    /// </summary>
    [Serializable]
    public class TextureMask : BaseMask
    {
        [Tooltip("The 2D texture to use as a mask. The red channel will be sampled.")]
        public Texture2D maskTexture;

        [Tooltip("Scales world coordinates to texture UVs. Smaller values 'stretch' the texture over a larger area.")]
        public Vector2 textureScale = new Vector2(0.01f, 0.01f); // Map world units to UVs

        [Tooltip("Offsets the texture sampling, useful for aligning the mask.")]
        public Vector2 textureOffset = Vector2.zero;

        /// <inheritdoc/>
        public override float Evaluate(float worldX, float worldZ, float noiseValue, float terrainHeight, float terrainSlope)
        {
            if (maskTexture == null) return 1.0f; // No texture, no masking influence

            // Map world coordinates to texture UVs. GetPixelBilinear handles tiling by default.
            float u = (worldX * textureScale.x + textureOffset.x);
            float v = (worldZ * textureScale.y + textureOffset.y);

            // GetPixelBilinear samples with linear interpolation for smoothness.
            // Requires the texture to be 'Read/Write Enabled' in its import settings.
            Color pixel = maskTexture.GetPixelBilinear(u, v);
            return pixel.r; // Using red channel as the mask factor
        }
    }

    /// <summary>
    /// Defines how multiple masks are combined together to form a final mask factor.
    /// </summary>
    public enum MaskCombinationMode
    {
        /// <summary>
        /// Final mask factor = mask1_factor * mask2_factor * ...
        /// This acts like an "AND" operation, where all masks must permit the noise for full effect.
        /// </summary>
        Multiply,
        /// <summary>
        /// Final mask factor = mask1_factor + mask2_factor + ... (clamped to a reasonable range).
        /// This acts like an "OR" operation, where each mask adds to the noise's base strength.
        /// </summary>
        Additive,
        /// <summary>
        /// Final mask factor = min(mask1_factor, mask2_factor, ...).
        /// Takes the minimum influence of all masks; the most restrictive mask dominates.
        /// </summary>
        Min,
        /// <summary>
        /// Final mask factor = max(mask1_factor, mask2_factor, ...).
        /// Takes the maximum influence of all masks; the least restrictive mask dominates.
        /// </summary>
        Max
    }

    /// <summary>
    /// The core NoiseMaskSystem component.
    /// This MonoBehaviour allows you to define a noise source and multiple masks,
    /// and then combine them to produce a final weighted value at any given point.
    /// This is highly useful for procedural generation of terrain textures, biomes,
    /// resource distribution, or visual effects.
    /// </summary>
    [HelpURL("https://github.com/YourRepo/NoiseMaskSystem")] // Placeholder for potential documentation
    public class NoiseMaskSystem : MonoBehaviour
    {
        [Header("Noise Configuration")]
        [SerializeReference] // Crucial for polymorphic serialization of abstract classes like BaseNoiseSource
        [Tooltip("The primary noise source used to generate base values.")]
        public BaseNoiseSource noiseSource = new PerlinNoiseSource();

        [Header("Mask Configurations")]
        [SerializeReference] // Crucial for polymorphic serialization of abstract classes like BaseMask
        [Tooltip("A list of masks that will be applied to the noise value.")]
        public List<BaseMask> masks = new List<BaseMask>();

        [Tooltip("Determines how the evaluated mask factors are combined before being applied to the noise.")]
        public MaskCombinationMode combinationMode = MaskCombinationMode.Multiply;

        [Tooltip("If true, the raw noise value (0-1) is remapped to (-1 to 1) before applying masks.")]
        public bool remapNoiseToSigned = false;

        [Tooltip("If true, the final combined masked noise value will be clamped to the 0-1 range.")]
        public bool clampFinalValue01 = true;

        /// <summary>
        /// Calculates the final masked noise value at a given world coordinate,
        /// considering provided context data like terrain height and slope.
        /// This is the core method for the NoiseMaskSystem.
        /// </summary>
        /// <param name="worldX">World X coordinate.</param>
        /// <param name="worldZ">World Z coordinate.</param>
        /// <param name="terrainHeight">The height of the terrain at this coordinate.</param>
        /// <param name="terrainSlope">The slope of the terrain at this coordinate (in degrees).</param>
        /// <returns>A float value (typically 0-1, or -1 to 1 if remapNoiseToSigned is true)
        /// representing the combined masked noise at the given point.</returns>
        public float Evaluate(float worldX, float worldZ, float terrainHeight, float terrainSlope)
        {
            if (noiseSource == null)
            {
                Debug.LogWarning("NoiseMaskSystem: No noise source defined. Returning 0.");
                return 0f;
            }

            // 1. Get raw noise value from the primary source
            float noiseValue = noiseSource.GetNoise(worldX, worldZ);

            // Remap noise if specified (e.g., for noise values that can be negative)
            if (remapNoiseToSigned)
            {
                noiseValue = noiseValue * 2.0f - 1.0f; // Remap from 0-1 to -1-1
            }

            // 2. Initialize the combined mask factor based on the combination mode
            float combinedMaskFactor = (combinationMode == MaskCombinationMode.Additive) ? 0.0f : 1.0f;

            // 3. Evaluate and combine all masks
            foreach (var mask in masks)
            {
                if (mask == null) continue; // Skip null entries in the mask list

                // Each mask evaluates its influence factor (typically 0-1)
                float currentMaskFactor = mask.Evaluate(worldX, worldZ, noiseValue, terrainHeight, terrainSlope);

                // Combine the current mask's factor with the running total
                switch (combinationMode)
                {
                    case MaskCombinationMode.Multiply:
                        combinedMaskFactor *= currentMaskFactor;
                        break;
                    case MaskCombinationMode.Additive:
                        combinedMaskFactor += currentMaskFactor;
                        break;
                    case MaskCombinationMode.Min:
                        combinedMaskFactor = Mathf.Min(combinedMaskFactor, currentMaskFactor);
                        break;
                    case MaskCombinationMode.Max:
                        combinedMaskFactor = Mathf.Max(combinedMaskFactor, currentMaskFactor);
                        break;
                }
            }

            // 4. Apply the combined mask factor to the initial noise value
            float finalValue;
            if (combinationMode == MaskCombinationMode.Additive)
            {
                finalValue = noiseValue + combinedMaskFactor;
            }
            else // Multiply, Min, Max modes
            {
                finalValue = noiseValue * combinedMaskFactor;
            }

            // 5. Clamp the final value to 0-1 if specified
            if (clampFinalValue01)
            {
                finalValue = Mathf.Clamp01(finalValue);
            }

            return finalValue;
        }


        // --- Editor Visualization (Optional but highly recommended for usability) ---

        [Header("Editor Visualization")]
        [Tooltip("Enable to show a grayscale representation of the NoiseMaskSystem output in the editor.")]
        public bool visualizeInEditor = true;

        [Tooltip("Resolution of the visualization texture (e.g., 128 for a 128x128 pixel texture).")]
        [Range(32, 512)]
        public int visualizeResolution = 128;

        [Tooltip("The size of the square area in world units that the visualization texture covers.")]
        public float visualizeSize = 100f;

        [Tooltip("Reference to a Unity Terrain object to sample height and slope data from.")]
        public Terrain terrainToSample;

        private Texture2D _visualizationTexture;
        private Renderer _visualizationRenderer;
        private GameObject _visualizationPlane;

        /// <summary>
        /// Called when the script is enabled or loaded. Initializes visualization resources.
        /// </summary>
        void OnEnable()
        {
            if (visualizeInEditor)
            {
                SetupVisualization();
            }
        }

        /// <summary>
        /// Called when the script is disabled or destroyed. Cleans up visualization resources.
        /// </summary>
        void OnDisable()
        {
            CleanupVisualization();
        }

        /// <summary>
        /// Sets up the plane and texture for editor visualization.
        /// </summary>
        private void SetupVisualization()
        {
            if (_visualizationPlane == null)
            {
                // Try to find an existing plane for this instance to avoid creating duplicates
                _visualizationPlane = GameObject.Find("NoiseMaskVizPlane_" + GetInstanceID());
                if (_visualizationPlane == null)
                {
                    _visualizationPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    _visualizationPlane.name = "NoiseMaskVizPlane_" + GetInstanceID();
                    _visualizationPlane.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild; // Don't save this in scene or build
                }
                _visualizationPlane.transform.position = transform.position + Vector3.up * 0.1f;
                _visualizationPlane.transform.localScale = Vector3.one * (visualizeSize / 10.0f); // Plane primitive is 10x10 by default
                _visualizationRenderer = _visualizationPlane.GetComponent<Renderer>();
                _visualizationRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _visualizationRenderer.receiveShadows = false;
                if (_visualizationRenderer.sharedMaterial == null || _visualizationRenderer.sharedMaterial.shader.name != "Unlit/Texture")
                {
                    _visualizationRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
                }
            }

            if (_visualizationTexture != null)
            {
                // Destroy previous texture if resolution changed or re-enabling
                if (Application.isEditor) DestroyImmediate(_visualizationTexture);
                else Destroy(_visualizationTexture);
            }
            _visualizationTexture = new Texture2D(visualizeResolution, visualizeResolution, TextureFormat.RGBA32, false);
            _visualizationTexture.filterMode = FilterMode.Bilinear; // Smooth visualization
            _visualizationTexture.wrapMode = TextureWrapMode.Clamp; // Avoid tiling issues at edges
            _visualizationRenderer.sharedMaterial.mainTexture = _visualizationTexture;

            UpdateVisualization(); // Perform initial update
        }

        /// <summary>
        /// Cleans up the visualization plane and texture.
        /// </summary>
        private void CleanupVisualization()
        {
            if (_visualizationTexture != null)
            {
                if (Application.isEditor) DestroyImmediate(_visualizationTexture);
                else Destroy(_visualizationTexture);
                _visualizationTexture = null;
            }
            if (_visualizationPlane != null)
            {
                if (Application.isEditor) DestroyImmediate(_visualizationPlane);
                else Destroy(_visualizationPlane);
                _visualizationPlane = null;
                _visualizationRenderer = null;
            }
        }

        /// <summary>
        /// Recalculates and updates the visualization texture.
        /// This method is designed to be called manually or from a custom editor
        /// whenever parameters change to provide real-time feedback.
        /// </summary>
        public void UpdateVisualization()
        {
            if (!visualizeInEditor || _visualizationTexture == null || _visualizationRenderer == null)
            {
                CleanupVisualization(); // Ensure visualization is hidden if disabled
                return;
            }

            // Ensure visualization plane is visible and correctly positioned/scaled
            _visualizationPlane.SetActive(true);
            float halfSize = visualizeSize / 2.0f;
            Vector3 systemCenter = transform.position;
            _visualizationPlane.transform.position = new Vector3(systemCenter.x, systemCenter.y + 0.1f, systemCenter.z);
            _visualizationPlane.transform.localScale = Vector3.one * (visualizeSize / 10.0f);

            Color[] pixels = new Color[visualizeResolution * visualizeResolution];

            TerrainData terrainData = (terrainToSample != null) ? terrainToSample.terrainData : null;
            
            for (int y = 0; y < visualizeResolution; y++)
            {
                for (int x = 0; x < visualizeResolution; x++)
                {
                    // Map pixel coordinates to world coordinates centered around the system's transform
                    float sampleX = systemCenter.x - halfSize + (float)x / visualizeResolution * visualizeSize;
                    float sampleZ = systemCenter.z - halfSize + (float)y / visualizeResolution * visualizeSize;

                    float terrainHeight = 0f;
                    float terrainSlope = 0f;

                    if (terrainData != null)
                    {
                        // Get height from the terrain
                        terrainHeight = terrainToSample.SampleHeight(new Vector3(sampleX, 0, sampleZ));

                        // Calculate slope: Approximate by sampling heights nearby
                        // This uses a central difference approximation. A smaller delta might be more accurate
                        // but can be more sensitive to floating point precision and performance.
                        float delta = 1.0f; // Small world units delta for slope calculation
                        float h_x_plus = terrainToSample.SampleHeight(new Vector3(sampleX + delta, 0, sampleZ));
                        float h_x_minus = terrainToSample.SampleHeight(new Vector3(sampleX - delta, 0, sampleZ));
                        float h_z_plus = terrainToSample.SampleHeight(new Vector3(sampleX, 0, sampleZ + delta));
                        float h_z_minus = terrainToSample.SampleHeight(new Vector3(sampleX, 0, sampleZ - delta));

                        float dx = (h_x_plus - h_x_minus) / (2 * delta);
                        float dz = (h_z_plus - h_z_minus) / (2 * delta);

                        float slopeRad = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz));
                        terrainSlope = slopeRad * Mathf.Rad2Deg; // Convert to degrees
                    }

                    // Evaluate the NoiseMaskSystem at this world point
                    float value = Evaluate(sampleX, sampleZ, terrainHeight, terrainSlope);

                    // Assign grayscale color based on the evaluated value
                    pixels[y * visualizeResolution + x] = new Color(value, value, value, 1.0f);
                }
            }
            _visualizationTexture.SetPixels(pixels);
            _visualizationTexture.Apply();
        }

        // --- Custom Editor for better Inspector UI (only compiles in Unity Editor) ---
        #if UNITY_EDITOR
        [CustomEditor(typeof(NoiseMaskSystem))]
        public class NoiseMaskSystemEditor : Editor
        {
            // Cached SerializedProperties for better performance and to handle Undo/Redo
            private SerializedProperty noiseSourceProp;
            private SerializedProperty masksProp;
            private SerializedProperty combinationModeProp;
            private SerializedProperty remapNoiseToSignedProp;
            private SerializedProperty clampFinalValue01Prop;
            private SerializedProperty visualizeInEditorProp;
            private SerializedProperty visualizeResolutionProp;
            private SerializedProperty visualizeSizeProp;
            private SerializedProperty terrainToSampleProp;

            /// <summary>
            /// Called when the Inspector is enabled. Initializes SerializedProperties.
            /// </summary>
            void OnEnable()
            {
                noiseSourceProp = serializedObject.FindProperty("noiseSource");
                masksProp = serializedObject.FindProperty("masks");
                combinationModeProp = serializedObject.FindProperty("combinationMode");
                remapNoiseToSignedProp = serializedObject.FindProperty("remapNoiseToSigned");
                clampFinalValue01Prop = serializedObject.FindProperty("clampFinalValue01");
                visualizeInEditorProp = serializedObject.FindProperty("visualizeInEditor");
                visualizeResolutionProp = serializedObject.FindProperty("visualizeResolution");
                visualizeSizeProp = serializedObject.FindProperty("visualizeSize");
                terrainToSampleProp = serializedObject.FindProperty("terrainToSample");
            }

            /// <summary>
            /// Draws the custom Inspector UI.
            /// </summary>
            public override void OnInspectorGUI()
            {
                serializedObject.Update(); // Always update the serialized object at the start

                // Draw Noise Configuration
                EditorGUILayout.PropertyField(noiseSourceProp, true); // true to draw children (e.g., PerlinNoiseSource parameters)

                // Draw Mask Configurations
                EditorGUILayout.PropertyField(masksProp, true); // true to draw children (the masks themselves with their parameters)

                EditorGUILayout.PropertyField(combinationModeProp);
                EditorGUILayout.PropertyField(remapNoiseToSignedProp);
                EditorGUILayout.PropertyField(clampFinalValue01Prop);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Add New Mask", EditorStyles.boldLabel);

                NoiseMaskSystem system = (NoiseMaskSystem)target;

                // Buttons to dynamically add new mask types to the list
                if (GUILayout.Button("Add Height Range Mask"))
                {
                    system.masks.Add(new HeightRangeMask());
                    EditorUtility.SetDirty(target); // Mark object as dirty to save changes
                }
                if (GUILayout.Button("Add Slope Mask"))
                {
                    system.masks.Add(new SlopeMask());
                    EditorUtility.SetDirty(target);
                }
                if (GUILayout.Button("Add Texture Mask"))
                {
                    system.masks.Add(new TextureMask());
                    EditorUtility.SetDirty(target);
                }

                EditorGUILayout.Space();

                // Draw Visualization Settings
                EditorGUILayout.PropertyField(visualizeInEditorProp);
                if (visualizeInEditorProp.boolValue)
                {
                    EditorGUI.indentLevel++; // Indent following properties
                    EditorGUILayout.PropertyField(visualizeResolutionProp);
                    EditorGUILayout.PropertyField(visualizeSizeProp);
                    EditorGUILayout.PropertyField(terrainToSampleProp);

                    // Button to manually refresh visualization
                    if (GUILayout.Button("Refresh Visualization"))
                    {
                        system.UpdateVisualization();
                    }
                    EditorGUI.indentLevel--; // Unindent
                    
                    // Automatically update visualization if any property changes (only in editor, not play mode)
                    if (GUI.changed && !Application.isPlaying)
                    {
                        // Use EditorApplication.delayCall to avoid GUI layout errors if UpdateVisualization changes GUI.
                        EditorApplication.delayCall += () => system.UpdateVisualization();
                    }
                }

                serializedObject.ApplyModifiedProperties(); // Apply changes back to the actual object
            }
        }
        #endif
    }
}
```

---

### How to Use the `NoiseMaskSystem` in Unity:

1.  **Save the Script:** Create a new C# script named `NoiseMaskSystem.cs` in your Unity project and copy-paste the entire code above into it.
2.  **Create a Manager GameObject:** In your Unity scene, create an empty GameObject (e.g., `GameObject > Create Empty`) and name it `NoiseSystemManager`.
3.  **Attach the Component:** Drag and drop the `NoiseMaskSystem.cs` script onto the `NoiseSystemManager` GameObject in the Inspector.
4.  **Configure Noise:**
    *   In the Inspector, expand the `Noise Configuration` section. By default, it uses a `PerlinNoiseSource`. Adjust its `Scale` and `Offset` to change the noise pattern.
5.  **Add Masks:**
    *   Under `Mask Configurations`, you'll see an empty list.
    *   Click the "Add New Mask" buttons (e.g., "Add Height Range Mask", "Add Slope Mask", "Add Texture Mask") to add different mask types.
    *   **For `HeightRangeMask` and `SlopeMask`:**
        *   You'll need a Unity `Terrain` in your scene (`GameObject > 3D Object > Terrain`).
        *   Drag your `Terrain` GameObject into the `Terrain To Sample` slot in the `Editor Visualization` section of the `NoiseMaskSystem` Inspector.
        *   Adjust `Min Height`, `Max Height`, `Blend Radius` for `HeightRangeMask`.
        *   Adjust `Min Slope`, `Max Slope`, `Blend Radius` for `SlopeMask`.
    *   **For `TextureMask`:**
        *   Import a `Texture2D` (e.g., a simple grayscale image or a pattern) into your project.
        *   Select the texture in the Project window. In the Inspector, ensure "Read/Write Enabled" is checked in its import settings, then click "Apply". This is crucial for the `GetPixelBilinear` method to work.
        *   Drag this texture into the `Mask Texture` slot of your `TextureMask`. Adjust `Texture Scale` and `Texture Offset`.
6.  **Set Combination Mode:**
    *   Choose how your masks combine from the `Mask Combination Mode` dropdown (e.g., `Multiply` for "AND" logic, `Additive` for "OR" logic).
7.  **Visualize:**
    *   Ensure `Visualize In Editor` is checked. A gray plane will appear in your scene, centered on the `NoiseSystemManager` GameObject. This plane will display the real-time output of your `NoiseMaskSystem`.
    *   Adjust `Visualize Resolution` and `Visualize Size` as needed.
    *   Move the `NoiseSystemManager` GameObject or the plane itself to match your terrain.
    *   As you change noise and mask parameters, the visualization will update automatically (or click "Refresh Visualization").

---

### Example Usage in Another Script (e.g., for Terrain Texture Blending):

This demonstrates how you would integrate `NoiseMaskSystem` into a script that modifies a Unity Terrain's texture blend (alphamaps).

```csharp
using UnityEngine;
using NoiseMasking; // Access the NoiseMaskSystem classes

public class TerrainTextureBlender : MonoBehaviour
{
    [Header("NoiseMask Systems for Layers")]
    [Tooltip("Defines where the 'Grass' texture should appear.")]
    public NoiseMaskSystem grassNoiseMaskSystem; 
    [Tooltip("Defines where the 'Rock' texture should appear.")]
    public NoiseMaskSystem rockNoiseMaskSystem;  
    [Tooltip("Defines where the 'Snow' texture should appear.")]
    public NoiseMaskSystem snowNoiseMaskSystem;  

    [Header("Terrain References")]
    [Tooltip("The Unity Terrain component to modify.")]
    public Terrain targetTerrain; 
    
    [Tooltip("Resolution of the terrain's alphamaps (texture blend maps).")]
    public int alphamapResolution = 256;

    void Start()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("Terrain reference is missing! Please assign a Terrain GameObject.", this);
            enabled = false; // Disable script if no terrain
            return;
        }

        // Ensure NoiseMaskSystems are assigned
        if (grassNoiseMaskSystem == null || rockNoiseMaskSystem == null || snowNoiseMaskSystem == null)
        {
            Debug.LogError("One or more NoiseMaskSystems are not assigned! Please assign them in the Inspector.", this);
            enabled = false;
            return;
        }

        // Important: Link the NoiseMaskSystems to the terrain for context data
        // This ensures the masks (like HeightRangeMask, SlopeMask) get relevant data.
        grassNoiseMaskSystem.terrainToSample = targetTerrain;
        rockNoiseMaskSystem.terrainToSample = targetTerrain;
        snowNoiseMaskSystem.terrainToSample = targetTerrain;

        // Optionally update their visualizations
        grassNoiseMaskSystem.UpdateVisualization();
        rockNoiseMaskSystem.UpdateVisualization();
        snowNoiseMaskSystem.UpdateVisualization();

        GenerateTerrainAlphamaps();
    }

    /// <summary>
    /// Generates and applies new alphamaps (texture blend weights) to the terrain
    /// based on the configured NoiseMaskSystems.
    /// </summary>
    [ContextMenu("Generate Terrain Alphamaps")] // Allows calling from Inspector
    public void GenerateTerrainAlphamaps()
    {
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError("No terrain data available.", this);
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainWorldPos = targetTerrain.transform.position;

        // Retrieve the current alphamaps. We'll modify a copy and then re-apply.
        // The last dimension [texture_index] must match the number of terrain layers.
        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // Ensure the alphamap array has enough layers for our systems (e.g., Grass, Rock, Snow needs 3 layers)
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length < 3)
        {
            Debug.LogError("Please add at least 3 Terrain Layers (e.g., Grass, Rock, Snow) to your terrain.", this);
            return;
        }

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Calculate world coordinates for this alphamap pixel
                // The alphamap coordinates (x, y) map to a normalized position on the terrain (0-1).
                float normalizedX = (float)x / (terrainData.alphamapWidth - 1);
                float normalizedZ = (float)y / (terrainData.alphamapHeight - 1);

                // Convert normalized position to world position
                float worldX = terrainWorldPos.x + normalizedX * terrainSize.x;
                float worldZ = terrainWorldPos.z + normalizedZ * terrainSize.z;

                // Get height at this world coordinate
                float height = targetTerrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                
                // Get terrain normal to calculate slope. TerrainData.GetInterpolatedNormal requires normalized coordinates.
                Vector3 normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
                float slope = Vector3.Angle(Vector3.up, normal); // Slope in degrees

                // Evaluate each NoiseMaskSystem to get the weight for its respective material
                float grassWeight = grassNoiseMaskSystem.Evaluate(worldX, worldZ, height, slope);
                float rockWeight = rockNoiseMaskSystem.Evaluate(worldX, worldZ, height, slope);
                float snowWeight = snowNoiseMaskSystem.Evaluate(worldX, worldZ, height, slope);

                // Normalize weights so they sum to 1.0 for terrain alphamaps
                float totalWeight = grassWeight + rockWeight + snowWeight;

                if (totalWeight > 0.001f) // Avoid division by zero
                {
                    alphaMaps[y, x, 0] = grassWeight / totalWeight; // Assign to first layer (e.g., Grass)
                    alphaMaps[y, x, 1] = rockWeight / totalWeight;  // Assign to second layer (e.g., Rock)
                    alphaMaps[y, x, 2] = snowWeight / totalWeight;  // Assign to third layer (e.g., Snow)
                }
                else
                {
                    // Fallback: If all weights are zero, assign full weight to the first texture
                    alphaMaps[y, x, 0] = 1.0f;
                    alphaMaps[y, x, 1] = 0.0f;
                    alphaMaps[y, x, 2] = 0.0f;
                }
            }
        }

        // Apply the modified alpha maps back to the terrain
        terrainData.SetAlphamaps(0, 0, alphaMaps);
        Debug.Log("Terrain alphamaps generated using NoiseMaskSystem!");
    }

    /// <summary>
    /// Example of how to query a specific mask at runtime for a single point.
    /// </summary>
    public float GetRockinessAtPoint(Vector3 worldPoint)
    {
        if (targetTerrain == null || rockNoiseMaskSystem == null) return 0f;

        float height = targetTerrain.SampleHeight(worldPoint);
        
        // Convert world position to normalized terrain coordinates for normal sampling
        float normalizedX = Mathf.InverseLerp(targetTerrain.transform.position.x, targetTerrain.transform.position.x + targetTerrain.terrainData.size.x, worldPoint.x);
        float normalizedZ = Mathf.InverseLerp(targetTerrain.transform.position.z, targetTerrain.transform.position.z + targetTerrain.terrainData.size.z, worldPoint.z);
        Vector3 normal = targetTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        float slope = Vector3.Angle(Vector3.up, normal);

        return rockNoiseMaskSystem.Evaluate(worldPoint.x, worldPoint.z, height, slope);
    }
}
```

To use the `TerrainTextureBlender` script:
1.  Create a new C# script named `TerrainTextureBlender.cs` and paste the code.
2.  Create an empty GameObject in your scene and attach `TerrainTextureBlender` to it.
3.  Assign your `Terrain` GameObject to the `Target Terrain` slot.
4.  Create three separate `NoiseSystemManager` GameObjects (or one and duplicate it). Name them `GrassSystem`, `RockSystem`, `SnowSystem`. Configure each with appropriate noise and masks for their respective texture types (e.g., `SnowSystem` might have a `HeightRangeMask` for high altitudes).
5.  Drag the `GrassSystem`, `RockSystem`, and `SnowSystem` GameObjects into their respective slots in the `TerrainTextureBlender`'s Inspector.
6.  Right-click on the `TerrainTextureBlender` component in the Inspector and select "Generate Terrain Alphamaps" from the context menu. Observe your terrain's textures change based on your noise and mask configurations!