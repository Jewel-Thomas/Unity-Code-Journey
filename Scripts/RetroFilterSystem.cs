// Unity Design Pattern Example: RetroFilterSystem
// This script demonstrates the RetroFilterSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RetroFilterSystem' pattern in Unity is a practical approach to applying a chain of visual post-processing effects, particularly useful for achieving a stylized "retro" look. It leverages common design patterns like **Chain of Responsibility** (filters are applied sequentially) and principles of **Strategy** (each filter is a distinct algorithm).

This pattern allows you to:
1.  **Modularize Effects**: Each retro effect (pixelation, color palette reduction, scanlines) is encapsulated in its own class.
2.  **Chain Effects**: Effects are applied one after another, where the output of one filter becomes the input for the next.
3.  **Dynamic Management**: Filters can be added, removed, enabled, or disabled at runtime or configured in the Unity Inspector.
4.  **Decoupling**: The core system managing the filter chain is decoupled from the individual filter implementations, promoting reusability and maintainability.

---

### **RetroFilterSystem Pattern Components:**

1.  **`IRetroFilter` (Interface)**: Defines the contract for any retro filter. It specifies methods for initialization, application, cleanup, and an `IsActive` property.
2.  **`RetroFilterBase` (Abstract Class)**: Provides common functionality for all filters, such as managing `Material` and `Shader` resources, and a base implementation for `Initialize` and `Cleanup`.
3.  **`ConcreteRetroFilter` (Concrete Classes)**: Implement `IRetroFilter` (or extend `RetroFilterBase`). Each class encapsulates a specific retro visual effect (e.g., `PixelationFilter`, `ColorPaletteFilter`, `ScanlineFilter`). They typically hold a reference to a `Material` that uses a custom `Shader` for their effect and expose relevant parameters.
4.  **`RetroFilterSystem` (Manager MonoBehaviour)**: This is the main component that orchestrates the entire system. It holds a list of `IRetroFilter` instances, iterates through them in its `OnRenderImage` callback, applying each effect sequentially to the camera's render texture. It also manages the lifecycle of these filters.

---

### **Setup Instructions:**

For this example to work immediately in Unity, you need to:

1.  **Create Shaders:**
    *   In your Unity project, create a folder named `Shaders` (or similar).
    *   Inside `Shaders`, create three new `Shader` files. You can right-click in the Project window -> `Create` -> `Shader` -> `Unlit Shader`.
    *   Name them:
        *   `PixelationShader`
        *   `ColorPaletteShader`
        *   `ScanlineShader`
    *   Open each shader file and **replace its entire content** with the corresponding shader code provided in the `## Shader Code` section below.

2.  **Create C# Script:**
    *   In your Unity project, create a C# script (e.g., in a `Scripts` folder) named `RetroFilterSystem.cs`.
    *   **Copy and paste the entire C# code block** provided below into this `RetroFilterSystem.cs` file.

3.  **Apply to Camera:**
    *   Select your main camera (or any camera you want to apply effects to) in your Unity scene.
    *   Click `Add Component` in the Inspector and search for `RetroFilterSystem`, then add it.
    *   Ensure your camera's `Rendering Path` is set to `Deferred` or `Forward` (not `Legacy Vertex Lit`) for post-processing effects to work.

4.  **Configure Filters:**
    *   With the camera selected, look at the `Retro Filter System` component in the Inspector.
    *   Under `Filters`, click the `+` button. You will see a dropdown list with `Pixelation Filter`, `Color Palette Filter`, and `Scanline Filter`.
    *   Add one or more filters. You can reorder them by dragging them up/down.
    *   Adjust the parameters of each filter (e.g., `Pixel Size`, `Color Depth`, `Intensity`) directly in the Inspector.
    *   You can also toggle the `Is Active` checkbox for each filter to enable/disable it.

---

## **Complete C# Unity Script: `RetroFilterSystem.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for OfType<T>() and FirstOrDefault()

// Mark this script to execute in the editor as well as play mode.
// This allows you to see the post-processing effects directly in the Scene view without playing.
[ExecuteAlways]
// Ensure a Camera component is always present on the GameObject this script is attached to.
[RequireComponent(typeof(Camera))]
public class RetroFilterSystem : MonoBehaviour
{
    /// <summary>
    /// This is the core of the RetroFilterSystem pattern.
    /// [SerializeReference] is crucial here! It allows Unity to serialize polymorphic fields,
    /// meaning you can have a List of the interface `IRetroFilter` (or abstract `RetroFilterBase`)
    /// and still add and configure concrete filter classes (like PixelationFilter, ColorPaletteFilter)
    /// directly in the Unity Inspector. Without it, only concrete classes could be serialized directly.
    /// </summary>
    [SerializeReference]
    [Tooltip("List of retro filters to apply in sequence. Drag to reorder. Click '+' to add a filter.")]
    private List<IRetroFilter> _filters = new List<IRetroFilter>();

    // Public property to provide read-only access to the filters list from other scripts.
    // This allows for dynamic filter management at runtime (e.g., adding/removing filters).
    public List<IRetroFilter> Filters => _filters;

    private Camera _camera; // Cached reference to the camera component.

    // Temporary RenderTextures are used to chain effects efficiently.
    // The output of one filter becomes the input for the next.
    private RenderTexture _currentRenderTexture;
    private RenderTexture _tempRenderTexture;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the camera reference and all currently configured filters.
    /// </summary>
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        InitializeFilters();
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Useful for re-initializing filters if the component was disabled and re-enabled.
    /// </summary>
    private void OnEnable()
    {
        InitializeFilters();
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// This is particularly useful in editor mode ([ExecuteAlways]) to ensure filters
    /// are initialized and re-initialized when parameters change or new filters are added.
    /// </summary>
    private void OnValidate()
    {
        InitializeFilters();
    }

    /// <summary>
    /// Iterates through the list of filters, ensuring each is initialized.
    /// It handles cases where filters might be added in the Inspector but not yet initialized,
    /// or if a shader couldn't be found (disabling that filter).
    /// </summary>
    private void InitializeFilters()
    {
        if (_filters == null) return;

        for (int i = 0; i < _filters.Count; i++)
        {
            // Remove any null entries that might occur from serialization issues or accidental deletion.
            if (_filters[i] == null)
            {
                Debug.LogWarning($"RetroFilterSystem: Null filter found at index {i}. Removing.");
                _filters.RemoveAt(i);
                i--; // Adjust index after removal to check the new element at this position
                continue;
            }
            _filters[i].Initialize(); // Call the filter's specific initialization logic
        }
    }

    /// <summary>
    /// This is the main Unity callback for post-processing effects.
    /// It's called after the camera finishes rendering the scene.
    /// </summary>
    /// <param name="source">The original RenderTexture from the camera (input).</param>
    /// <param name="destination">The final RenderTexture to blit the processed result to (output).</param>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // If no filters are configured or the list is empty, just blit the source directly to destination.
        // This effectively bypasses the post-processing chain.
        if (_filters == null || _filters.Count == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // The 'source' RenderTexture is the initial input for our filter chain.
        // It's managed by Unity and should NOT be released with RenderTexture.ReleaseTemporary().
        _currentRenderTexture = source;

        // Iterate through each filter in the list to apply them sequentially.
        for (int i = 0; i < _filters.Count; i++)
        {
            IRetroFilter filter = _filters[i];

            // Skip null filters or inactive filters.
            if (filter == null || !filter.IsActive)
            {
                continue;
            }

            // Get a temporary RenderTexture to store the output of the current filter.
            // This texture will become the input for the next filter in the chain.
            // We match the dimensions and format of the original source for consistency.
            _tempRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

            // Apply the current filter:
            // - Input: _currentRenderTexture (output of previous filter, or original source)
            // - Output: _tempRenderTexture (this filter's result)
            filter.ApplyFilter(_currentRenderTexture, _tempRenderTexture);

            // IMPORTANT: If _currentRenderTexture was a temporary one (i.e., from a previous filter's output),
            // it needs to be released to prevent memory leaks.
            // The very first 'source' RenderTexture (i.e., when i == 0) is not temporary and not released here.
            if (i > 0)
            {
                RenderTexture.ReleaseTemporary(_currentRenderTexture);
            }

            // The output of this filter (_tempRenderTexture) now becomes the input for the next filter.
            _currentRenderTexture = _tempRenderTexture;
        }

        // After all active filters have been applied, _currentRenderTexture holds the final processed image.
        // Blit this final result to the camera's actual destination.
        Graphics.Blit(_currentRenderTexture, destination);

        // IMPORTANT: Release the *last* temporary RenderTexture used in the chain.
        // This is crucial for memory management. Only do this if at least one filter was processed.
        if (_filters.Any(f => f != null && f.IsActive)) // Check if any filter was actually applied
        {
            RenderTexture.ReleaseTemporary(_currentRenderTexture);
        }

        // Reset references to temporary textures to avoid accidental lingering references
        // to textures that have been released back to the pool.
        _currentRenderTexture = null;
        _tempRenderTexture = null;
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// Cleans up resources held by individual filters.
    /// </summary>
    private void OnDisable()
    {
        CleanupFilters();
    }

    /// <summary>
    /// Called when the behaviour is destroyed.
    /// Ensures all filter resources are properly released.
    /// </summary>
    private void OnDestroy()
    {
        CleanupFilters();
    }

    /// <summary>
    /// Iterates through all filters and calls their Cleanup method to release resources
    /// (like dynamically created Materials).
    /// </summary>
    private void CleanupFilters()
    {
        if (_filters != null)
        {
            foreach (var filter in _filters)
            {
                filter?.Cleanup(); // Use null-conditional operator for safety
            }
        }
    }

    // --- Public API for runtime filter management ---

    /// <summary>
    /// Adds a new filter instance to the end of the filter chain.
    /// The filter will be initialized immediately upon addition.
    /// </summary>
    /// <param name="filter">The IRetroFilter instance to add.</param>
    public void AddFilter(IRetroFilter filter)
    {
        if (filter == null) return;
        _filters.Add(filter);
        filter.Initialize(); // Initialize the new filter
    }

    /// <summary>
    /// Removes a specific filter instance from the chain and cleans up its resources.
    /// </summary>
    /// <param name="filter">The IRetroFilter instance to remove.</param>
    /// <returns>True if the filter was found and removed, false otherwise.</returns>
    public bool RemoveFilter(IRetroFilter filter)
    {
        if (filter == null) return false;
        if (_filters.Remove(filter))
        {
            filter.Cleanup(); // Clean up resources of the removed filter
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the filter at a specific index from the chain and cleans up its resources.
    /// </summary>
    /// <param name="index">The zero-based index of the filter to remove.</param>
    public void RemoveFilterAt(int index)
    {
        if (index >= 0 && index < _filters.Count)
        {
            IRetroFilter filter = _filters[index];
            _filters.RemoveAt(index);
            filter?.Cleanup(); // Safely clean up
        }
    }

    /// <summary>
    /// Clears all filters from the system and cleans up their resources.
    /// </summary>
    public void ClearAllFilters()
    {
        foreach (var filter in _filters)
        {
            filter?.Cleanup();
        }
        _filters.Clear();
    }

    /// <summary>
    /// Gets the first filter of a specific type from the chain.
    /// Useful for runtime modification of filter parameters.
    /// </summary>
    /// <typeparam name="T">The type of the filter to retrieve (must implement IRetroFilter).</typeparam>
    /// <returns>The first filter of type T found, or null if no such filter exists.</returns>
    public T GetFilter<T>() where T : class, IRetroFilter
    {
        return _filters.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Sets the active state of a specific filter type.
    /// </summary>
    /// <typeparam name="T">The type of the filter to activate/deactivate.</typeparam>
    /// <param name="isActive">The desired active state.</param>
    public void SetFilterActive<T>(bool isActive) where T : class, IRetroFilter
    {
        T filter = GetFilter<T>();
        if (filter != null)
        {
            filter.IsActive = isActive;
        }
    }
}

// --- IRetroFilter Interface ---
/// <summary>
/// Defines the contract for any retro filter.
/// All concrete retro filter classes must implement this interface.
/// </summary>
public interface IRetroFilter
{
    // Property to control if the filter is currently active and should be applied.
    bool IsActive { get; set; }

    // Called once to set up resources (e.g., load shader, create material).
    void Initialize();

    // The main method to apply the visual effect.
    // Takes the source RenderTexture (input) and blits the result to the destination RenderTexture (output).
    void ApplyFilter(RenderTexture source, RenderTexture destination);

    // Called to release any allocated resources (e.g., destroy material).
    void Cleanup();
}

// --- RetroFilterBase Abstract Class ---
/// <summary>
/// Provides common functionality and resource management for concrete retro filters.
/// All concrete filters should inherit from this base class.
/// </summary>
public abstract class RetroFilterBase : IRetroFilter
{
    // Material used by the filter's shader. Protected so derived classes can access it.
    protected Material material;

    // The name/path of the shader required for this filter (e.g., "Hidden/RetroFilter/Pixelation").
    // Derived classes must set this in their constructor.
    protected string shaderName;

    /// <summary>
    /// Controls whether this specific filter instance is active in the chain.
    /// [SerializeField] makes it configurable in the Inspector.
    /// </summary>
    [field: SerializeField, Tooltip("Enable or disable this specific filter.")]
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Initializes the filter by finding its shader and creating a Material.
    /// This method can be overridden by derived classes to set initial shader parameters.
    /// </summary>
    public virtual void Initialize()
    {
        if (material == null && !string.IsNullOrEmpty(shaderName))
        {
            // Attempt to find the shader by its name.
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader '{shaderName}' not found for {GetType().Name}. This filter will be disabled.");
                IsActive = false; // Disable the filter if its shader isn't found
                return;
            }
            // Create a new Material instance from the shader.
            // hideFlags prevents the material from being saved with the scene or shown in Project view.
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        else if (material != null && material.shader.name != shaderName)
        {
            // Handle case where shaderName might have changed or material was previously set to a different shader.
            Debug.LogWarning($"RetroFilterSystem: Material for {GetType().Name} has a different shader '{material.shader.name}' than expected '{shaderName}'. Re-initializing.");
            Cleanup(); // Clean up old material
            Initialize(); // Re-initialize with the correct shader
        }
    }

    /// <summary>
    /// Abstract method that must be implemented by derived classes to define their specific filtering logic.
    /// </summary>
    public abstract void ApplyFilter(RenderTexture source, RenderTexture destination);

    /// <summary>
    /// Cleans up the dynamically created Material instance when the filter is destroyed or disabled.
    /// This is crucial for preventing memory leaks in the editor and runtime.
    /// </summary>
    public virtual void Cleanup()
    {
        if (material != null)
        {
            // DestroyImmediate is used here because this might be called in editor mode ([ExecuteAlways]).
            // In runtime builds, Destroy(material) would typically be used.
            // For post-processing effects that dynamically create materials, DestroyImmediate is safer
            // across editor and play mode transitions to ensure resources are released promptly.
            DestroyImmediate(material);
            material = null;
        }
    }
}

// --- Concrete RetroFilter: PixelationFilter ---
/// <summary>
/// Applies a pixelation effect by rendering the scene at a lower resolution
/// and then scaling it up.
/// </summary>
[System.Serializable] // Allows Unity to serialize this custom class in the Inspector
public class PixelationFilter : RetroFilterBase
{
    // Filter-specific parameters, exposed to the Inspector.
    [SerializeField, Range(1, 512)]
    [Tooltip("The effective number of pixels along the smallest screen dimension.")]
    public int PixelSize = 128; // e.g., 64 means 64xN 'macro' pixels

    /// <summary>
    /// Constructor: Sets the shader name for this filter.
    /// </summary>
    public PixelationFilter() { shaderName = "Hidden/RetroFilter/Pixelation"; }

    /// <summary>
    /// Initializes the base class and then sets initial shader parameters.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize(); // Call base Initialize to create the material
        if (material != null) SetShaderParameters();
    }

    /// <summary>
    /// Applies the pixelation effect using Graphics.Blit and the filter's material.
    /// </summary>
    public override void ApplyFilter(RenderTexture source, RenderTexture destination)
    {
        // If inactive or material is missing, pass the image through without effect.
        if (!IsActive || material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        SetShaderParameters(); // Ensure shader parameters are up-to-date
        Graphics.Blit(source, destination, material); // Apply the shader effect
    }

    /// <summary>
    /// Sets the _PixelSize and _AspectRatio parameters on the shader.
    /// </summary>
    private void SetShaderParameters()
    {
        if (material != null)
        {
            material.SetFloat("_PixelSize", PixelSize);
            // Pass screen aspect ratio to the shader to maintain correct pixel shape.
            material.SetFloat("_AspectRatio", (float)Screen.width / Screen.height);
        }
    }
}

// --- Concrete RetroFilter: ColorPaletteFilter (Simplified Posterization) ---
/// <summary>
/// Reduces the number of distinct color values in the image, creating a posterized look.
/// </summary>
[System.Serializable]
public class ColorPaletteFilter : RetroFilterBase
{
    [SerializeField, Range(1, 16)]
    [Tooltip("Number of distinct color values per channel (e.g., 4 means 0, 1/3, 2/3, 1).")]
    public int ColorDepth = 4; // 1-16 distinct levels per RGB channel

    public ColorPaletteFilter() { shaderName = "Hidden/RetroFilter/ColorPalette"; }

    public override void Initialize()
    {
        base.Initialize();
        if (material != null) SetShaderParameters();
    }

    public override void ApplyFilter(RenderTexture source, RenderTexture destination)
    {
        if (!IsActive || material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        SetShaderParameters();
        Graphics.Blit(source, destination, material);
    }

    private void SetShaderParameters()
    {
        if (material != null)
        {
            material.SetFloat("_ColorDepth", ColorDepth);
        }
    }
}

// --- Concrete RetroFilter: ScanlineFilter ---
/// <summary>
/// Adds horizontal scanlines to simulate old CRT displays.
/// </summary>
[System.Serializable]
public class ScanlineFilter : RetroFilterBase
{
    [SerializeField, Range(0f, 1f)]
    [Tooltip("The intensity (darkness) of the scanlines.")]
    public float Intensity = 0.5f;

    [SerializeField, Range(1, 1024)]
    [Tooltip("The number of scanlines across the screen height. Higher = more lines.")]
    public int Density = 256; // e.g., 256 lines for a retro feel

    public ScanlineFilter() { shaderName = "Hidden/RetroFilter/Scanline"; }

    public override void Initialize()
    {
        base.Initialize();
        if (material != null) SetShaderParameters();
    }

    public override void ApplyFilter(RenderTexture source, RenderTexture destination)
    {
        if (!IsActive || material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        SetShaderParameters();
        Graphics.Blit(source, destination, material);
    }

    private void SetShaderParameters()
    {
        if (material != null)
        {
            material.SetFloat("_Intensity", Intensity);
            material.SetFloat("_Density", Density);
        }
    }
}
```

---

## **Shader Code**

You need to create three new `Unlit Shaders` in your Unity project (e.g., in a `Shaders` folder) and name them `PixelationShader`, `ColorPaletteShader`, and `ScanlineShader`. Then, replace their default content with the code below.

**1. `PixelationShader` (File: `PixelationShader.shader`)**

```shader
Shader "Hidden/RetroFilter/Pixelation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size (Height)", Float) = 64 // Number of macro-pixels along smallest screen dimension
        _AspectRatio ("Aspect Ratio (Width/Height)", Float) = 1.0 // Screen aspect ratio
    }
    SubShader
    {
        // Standard tags for post-processing shaders
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "ShaderGraph":"False" "IgnoreProjector"="True"}
        LOD 100

        Pass
        {
            // For post-processing, we don't care about Z-testing, culling, or Z-writing.
            ZTest Always Cull Off ZWrite Off
            // This enables Universal Render Pipeline support (if applicable, though these are simple enough for built-in too)
            // Can be removed if only targeting Built-in RP.
            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 vulkan ps4 xboxone switch
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // For URP, if needed. For Built-in, use "UnityCG.cginc"
            // For Built-in RP, uncomment this and comment the URP line above:
            // #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Texture sampler for the input screen texture
            sampler2D _MainTex;
            // Unity automatically provides _MainTex_TexelSize (1/width, 1/height, width, height)
            // float4 _MainTex_TexelSize;
            float _PixelSize; // Desired number of pixels on the screen's smaller dimension
            float _AspectRatio; // Screen aspect ratio (width / height)

            v2f vert (appdata v)
            {
                v2f o;
                // Transform vertex from object space to clip space (screen space)
                o.vertex = TransformObjectToHClip(v.vertex); // URP version
                // o.vertex = UnityObjectToClipPos(v.vertex); // Built-in RP version
                o.uv = v.uv; // Pass through UV coordinates
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate the actual number of "macro" pixels for the screen dimensions.
                // We base this on _PixelSize representing the vertical resolution of macro-pixels.
                float screenHeightInPixels = _PixelSize;
                // Adjust horizontal macro-pixel count based on aspect ratio to maintain square macro-pixels.
                float screenWidthInPixels = _PixelSize * _AspectRatio;

                // Scale UV coordinates to the macro-pixel grid, then round down to snap to grid.
                // This effectively samples only one pixel per "macro-pixel" block.
                float2 pixelatedUV = floor(i.uv * float2(screenWidthInPixels, screenHeightInPixels)) / float2(screenWidthInPixels, screenHeightInPixels);

                // Sample the original texture at the pixelated UV coordinate.
                return tex2D(_MainTex, pixelatedUV);
            }
            ENDHLSL
        }
    }
}
```

**2. `ColorPaletteShader` (File: `ColorPaletteShader.shader`)**

```shader
Shader "Hidden/RetroFilter/ColorPalette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorDepth ("Color Depth", Float) = 4 // Number of distinct color values per channel
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "ShaderGraph":"False" "IgnoreProjector"="True"}
        LOD 100

        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 vulkan ps4 xboxone switch
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _ColorDepth; // Number of desired color steps per channel (e.g., 4 steps: 0, 1/3, 2/3, 1)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                // o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Posterization algorithm:
                // 1. Calculate the number of intervals for color quantization.
                //    If ColorDepth = 1, all colors become 0 (black).
                //    If ColorDepth = 2, colors become 0 or 1 (binary).
                //    If ColorDepth = 4, colors become 0, 1/3, 2/3, 1.
                //    So, `numSteps` should be `_ColorDepth - 1` to get the correct denominator.
                //    Ensure `numSteps` is at least 1 to avoid division by zero or large values.
                float numSteps = max(1.0, _ColorDepth - 1.0);

                // Quantize each RGB channel:
                // - Multiply by `numSteps` to scale the [0,1] range to [0, numSteps].
                // - `floor` to snap to the nearest lower integer.
                // - Divide by `numSteps` to scale back to the [0,1] range, but now with quantized values.
                col.rgb = floor(col.rgb * numSteps) / numSteps;

                return col;
            }
            ENDHLSL
        }
    }
}
```

**3. `ScanlineShader` (File: `ScanlineShader.shader`)**

```shader
Shader "Hidden/RetroFilter/Scanline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Scanline Intensity", Range(0,1)) = 0.5 // How dark the scanlines are
        _Density ("Scanline Density", Float) = 256 // Number of visible scanlines across the screen height
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "ShaderGraph":"False" "IgnoreProjector"="True"}
        LOD 100

        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 vulkan ps4 xboxone switch
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Intensity; // Controls how much the scanlines darken the image.
            float _Density; // The target number of visible scanlines across the screen height.

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                // o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Calculate a repeating pattern based on the vertical UV coordinate and density.
                // `_Density` defines how many repeating dark/bright bands there are across the screen height.
                // `fmod(x, 1.0)` gives the fractional part of x (0 to 1).
                float linePattern = fmod(i.uv.y * _Density, 1.0);

                // Create a sharp step function for the scanline effect.
                // If `linePattern` is above 0.5 (i.e., in the second half of its 0-1 cycle),
                // `scanlineFactor` becomes 1, indicating a dark part of the scanline.
                // Otherwise, it's 0, indicating a bright part.
                float scanlineFactor = step(0.5, linePattern);

                // Mix the original color with a darker version.
                // `lerp(A, B, t)` returns `A` if `t` is 0, and `B` if `t` is 1.
                // When `scanlineFactor` is 1 (dark line), it mixes towards `col.rgb * (1.0 - _Intensity)`.
                // When `scanlineFactor` is 0 (bright line), it returns the original `col.rgb`.
                col.rgb = lerp(col.rgb, col.rgb * (1.0 - _Intensity), scanlineFactor);

                return col;
            }
            ENDHLSL
        }
    }
}
```

---

### **Example Usage (Runtime Script)**

You can also interact with the `RetroFilterSystem` at runtime using another script. Attach this example script to an empty GameObject in your scene to see it in action.

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator

public class RetroFilterSystemExample : MonoBehaviour
{
    public RetroFilterSystem retroFilterSystem; // Assign your camera's RetroFilterSystem here in the Inspector

    void Start()
    {
        if (retroFilterSystem == null)
        {
            // Try to find the RetroFilterSystem on the main camera if not assigned
            retroFilterSystem = Camera.main?.GetComponent<RetroFilterSystem>();
            if (retroFilterSystem == null)
            {
                Debug.LogError("RetroFilterSystem not found. Please assign it in the Inspector or ensure your main camera has one.");
                return;
            }
        }

        Debug.Log("RetroFilterSystem Example Started. Press keys to interact:");
        Debug.Log("P: Toggle Pixelation Filter");
        Debug.Log("C: Toggle Color Palette Filter");
        Debug.Log("S: Toggle Scanline Filter");
        Debug.Log("A: Add a new Pixelation Filter dynamically (if none exists)");
        Debug.Log("R: Remove first Pixelation Filter dynamically");
    }

    void Update()
    {
        if (retroFilterSystem == null) return;

        // --- Toggle filters on/off ---
        if (Input.GetKeyDown(KeyCode.P))
        {
            PixelationFilter pixelFilter = retroFilterSystem.GetFilter<PixelationFilter>();
            if (pixelFilter != null)
            {
                pixelFilter.IsActive = !pixelFilter.IsActive;
                Debug.Log($"Pixelation Filter: {(pixelFilter.IsActive ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.LogWarning("Pixelation Filter not found in the system.");
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ColorPaletteFilter colorFilter = retroFilterSystem.GetFilter<ColorPaletteFilter>();
            if (colorFilter != null)
            {
                colorFilter.IsActive = !colorFilter.IsActive;
                Debug.Log($"Color Palette Filter: {(colorFilter.IsActive ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.LogWarning("Color Palette Filter not found in the system.");
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            ScanlineFilter scanlineFilter = retroFilterSystem.GetFilter<ScanlineFilter>();
            if (scanlineFilter != null)
            {
                scanlineFilter.IsActive = !scanlineFilter.IsActive;
                Debug.Log($"Scanline Filter: {(scanlineFilter.IsActive ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.LogWarning("Scanline Filter not found in the system.");
            }
        }

        // --- Add/Remove filters dynamically ---
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Add a new PixelationFilter if one doesn't already exist or if we want multiple
            if (retroFilterSystem.GetFilter<PixelationFilter>() == null)
            {
                PixelationFilter newPixelFilter = new PixelationFilter { PixelSize = 96, IsActive = true };
                retroFilterSystem.AddFilter(newPixelFilter);
                Debug.Log("Dynamically added a new Pixelation Filter.");
            }
            else
            {
                Debug.Log("Pixelation Filter already exists.");
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // Remove the first PixelationFilter found
            PixelationFilter pixelFilterToRemove = retroFilterSystem.GetFilter<PixelationFilter>();
            if (pixelFilterToRemove != null)
            {
                retroFilterSystem.RemoveFilter(pixelFilterToRemove);
                Debug.Log("Dynamically removed a Pixelation Filter.");
            }
            else
            {
                Debug.Log("No Pixelation Filter found to remove.");
            }
        }
    }
}
```