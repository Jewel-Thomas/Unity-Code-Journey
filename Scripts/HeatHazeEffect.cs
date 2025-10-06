// Unity Design Pattern Example: HeatHazeEffect
// This script demonstrates the HeatHazeEffect pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **'HeatHazeEffect' design pattern** in Unity, which is a practical application of the **Component Pattern** combined with principles of the **Strategy Pattern** for visual effects.

**Understanding the 'HeatHazeEffect' Design Pattern:**

The 'HeatHazeEffect' design pattern, as implemented here, defines a reusable, configurable component for applying a specific visual post-processing effect (heat haze distortion) to a camera's rendering.

**Key Principles:**

1.  **Component-Based (`MonoBehaviour`):** The effect is encapsulated within a `MonoBehaviour` script. This makes it a self-contained component that can be easily attached to any `Camera` object, enabled/disabled, and configured independently in the Unity Inspector. This adheres to Unity's core component-based architecture.
2.  **Shader-Driven Strategy:** The actual visual logic (how the distortion is calculated and applied to pixels) is delegated to a separate **Shader** asset. This shader acts as the "strategy" for rendering the heat haze. By using different shaders, you could implement various types of haze (e.g., simple distortion, chromatic aberration haze, specific particle-based haze) without modifying the core C# component. The C# script manages the shader's lifecycle and parameters.
3.  **Configurable Parameters:** The `HeatHazeEffect` component exposes public fields in the Unity Inspector (e.g., distortion strength, scroll speed, distortion texture, color). These parameters allow designers and developers to easily customize the effect's appearance at runtime or design-time, effectively changing the 'strategy's' behavior without writing code.
4.  **Post-Processing Pipeline Integration:** It leverages Unity's `OnRenderImage` callback, which is a powerful hook into the rendering pipeline. This allows the effect to process the entire scene after it has been rendered by the camera but before it is displayed on screen, making it a true post-processing effect.
5.  **Resource Management:** The component responsibly creates and destroys necessary rendering resources (specifically, the `Material` instance derived from the shader) to prevent memory leaks and ensure optimal performance. This includes creating the material once and destroying it when the component is disabled or destroyed.

---

### Part 1: HeatHazeEffect.cs (C# Script)

This script should be attached to your camera. It manages the post-processing effect, passes parameters to the shader, and handles the rendering.

```csharp
using UnityEngine;
using System.Collections; // Required for coroutines if any, good practice to include.

/// <summary>
/// HeatHazeEffect.cs
/// 
/// This script implements the 'HeatHazeEffect' design pattern in Unity.
/// It's a MonoBehaviour component that acts as a post-processing effect,
/// distorting the screen based on a provided shader and configurable parameters.
/// 
/// Pattern Breakdown:
/// - Component Pattern: The script itself is a reusable component attachable to a Camera.
/// - Strategy Pattern (via Shader): The core rendering logic (how the haze looks) is
///   encapsulated in a Shader. Different shaders can be swapped to change the haze 'strategy'.
/// - Configurable Strategy: Public fields allow artists and designers to customize
///   the effect's parameters in the Inspector, influencing the shader's behavior.
/// - Resource Management: Handles the creation and destruction of the Material instance.
/// </summary>
[ExecuteInEditMode] // Allows the effect to be seen and tweaked in the Scene View
[RequireComponent(typeof(Camera))] // Ensures this script is always on a Camera
[AddComponentMenu("Post-processing/Heat Haze Effect")] // Adds to component menu
public class HeatHazeEffect : MonoBehaviour
{
    [Header("Shader and Material")]
    [Tooltip("The shader used to render the heat haze effect.")]
    public Shader hazeShader; // Reference to the custom shader asset.

    private Material hazeMaterial; // The material instance created from the shader.

    /// <summary>
    /// Property to safely get and lazily initialize the haze material.
    /// This ensures the material is only created when needed and is correctly linked to the shader.
    /// This is part of the resource management and setup phase of the pattern.
    /// </summary>
    protected Material material
    {
        get
        {
            if (hazeMaterial == null && hazeShader != null)
            {
                hazeMaterial = new Material(hazeShader);
                hazeMaterial.hideFlags = HideFlags.HideAndDontSave; // Prevents saving the material asset.
            }
            return hazeMaterial;
        }
    }

    [Header("Haze Parameters")]
    [Tooltip("The texture used to define the distortion pattern. Perlin noise or cloud textures work well.")]
    public Texture2D distortionTexture;

    [Range(0.0f, 1.0f)]
    [Tooltip("Overall strength of the distortion effect.")]
    public float distortionStrength = 0.05f;

    [Tooltip("Speed at which the distortion texture scrolls, creating a 'wavy' effect.")]
    public Vector2 scrollSpeed = new Vector2(0.1f, 0.15f);

    [ColorUsage(false, true)] // HDR color support, no alpha for tinting
    [Tooltip("Color tint applied to the distorted area. Can be used for thermal vision effects.")]
    public Color hazeColor = new Color(1, 1, 1, 1); // Default to white (no tint)

    [Range(0.0f, 1.0f)]
    [Tooltip("How much the effect blends with the original image (0=full effect, 1=no effect).")]
    public float blendAmount = 0.0f;

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Verifies the shader is assigned and initializes the material.
    /// This is crucial for the component's lifecycle and ensures resources are ready.
    /// </summary>
    protected virtual void OnEnable()
    {
        // Check if the shader is assigned; if not, disable the component to avoid errors.
        if (hazeShader == null)
        {
            Debug.LogError("Heat Haze Shader is not assigned on " + gameObject.name + ". Disabling effect.");
            enabled = false;
        }
        else
        {
            // Ensure material is initialized.
            // Accessing 'material' property will create it if null.
            _ = material; 
        }
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Cleans up the material to prevent memory leaks.
    /// Part of responsible resource management in the pattern.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (hazeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(hazeMaterial); // In play mode, use Destroy
            }
            else
            {
                DestroyImmediate(hazeMaterial); // In editor, use DestroyImmediate
            }
            hazeMaterial = null;
        }
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Useful for re-applying settings to the material in edit mode.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (hazeShader == null) return;
        if (hazeMaterial == null) // If material is null, OnEnable will create it, but ensure _material property works
        {
            _ = material; // Access the property to create the material if not already.
            return;
        }
        // Apply parameters to the material immediately if validated in editor
        ApplyMaterialParameters();
    }

    /// <summary>
    /// Unity's post-processing hook. This method is called after all rendering is complete
    /// and allows you to modify the final image before it's displayed on screen.
    /// This is the core of how the 'Strategy' (shader) is applied to the 'source' (rendered scene).
    /// </summary>
    /// <param name="source">The source render texture (the fully rendered scene).</param>
    /// <param name="destination">The destination render texture (where the processed image goes).</param>
    [ImageEffectOpaque] // Indicates that the effect should run after opaque geometry but before transparent.
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Early exit if shader or material is not ready.
        if (material == null)
        {
            // If the shader is missing or material couldn't be created,
            // just blit the source to destination without processing.
            Graphics.Blit(source, destination);
            return;
        }

        // Apply all public parameters to the material (shader variables).
        // This is where the configurable 'strategy' parameters are passed to the shader.
        ApplyMaterialParameters();

        // Pass the source texture to the shader (Unity handles _MainTex automatically for Graphics.Blit)
        // material.SetTexture("_MainTex", source); // Not strictly necessary for Graphics.Blit

        // Perform the actual image effect using Graphics.Blit.
        // It draws a full-screen quad and applies the material's shader to it.
        // The result is written to the destination render texture.
        Graphics.Blit(source, destination, material);
    }

    /// <summary>
    /// Helper method to set all public parameters on the material.
    /// Centralizes parameter assignment, making it clean and reusable for OnValidate and OnRenderImage.
    /// </summary>
    private void ApplyMaterialParameters()
    {
        if (material == null) return;

        material.SetTexture("_DistortionTex", distortionTexture);
        material.SetFloat("_DistortionStrength", distortionStrength);
        material.SetVector("_ScrollSpeed", scrollSpeed);
        material.SetColor("_HazeColor", hazeColor);
        material.SetFloat("_BlendAmount", blendAmount);

        // Calculate time for scrolling animation
        material.SetFloat("_Time", Time.time); 
    }
}
```

---

### Part 2: HeatHazeShader.shader (Shader Code)

Create a new Shader asset (e.g., `Assets/Shaders/HeatHazeShader.shader`) and paste this code into it. This shader defines the visual 'strategy' for the heat haze.

```shader
Shader "Custom/PostProcessing/HeatHaze"
{
    Properties
    {
        // _MainTex is automatically populated by Unity's Graphics.Blit with the screen texture.
        // It is defined here for clarity and compatibility.
        _MainTex ("Screen Texture", 2D) = "white" {} 

        _DistortionTex ("Distortion Texture", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.05
        _ScrollSpeed ("Scroll Speed (UV)", Vector) = (0.1,0.1,0,0)
        _HazeColor ("Haze Color Tint", Color) = (1,1,1,1)
        _BlendAmount ("Blend With Original", Range(0, 1)) = 0.0
        _Time ("Time", Float) = 0 // Used for animation, set by script
    }
    SubShader
    {
        // For post-processing effects, we typically don't need culling,
        // depth testing, or depth writing. We just draw a full-screen quad.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Optimize for faster execution, potentially at the cost of some precision.
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc" // Includes common Unity shader helper functions.

            // Structure for vertex input data.
            struct appdata
            {
                float4 vertex : POSITION; // Vertex position
                float2 uv : TEXCOORD0;    // Texture coordinates (for screen)
            };

            // Structure for data passed from vertex shader to fragment shader.
            struct v2f
            {
                float2 uv : TEXCOORD0;      // Screen UVs
                float4 vertex : SV_POSITION; // Clip-space vertex position
            };

            // Declare shader properties (variables).
            sampler2D _MainTex;          // The source screen texture.
            sampler2D _DistortionTex;    // Texture used for distortion (e.g., Perlin noise).
            float _DistortionStrength;   // How strong the distortion is.
            float2 _ScrollSpeed;         // Speed to scroll the distortion texture.
            fixed4 _HazeColor;           // Color tint for the haze.
            float _BlendAmount;          // Blend factor with the original image.
            float _Time;                 // Current time (for animation), passed from script.

            /// <summary>
            /// Vertex Shader: Simply passes the screen UVs and vertex position.
            /// For a full-screen quad in post-processing, vertices are usually
            /// already in clip space or converted directly.
            /// </summary>
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform vertex to clip space.
                o.uv = v.uv; // Pass screen UVs directly to fragment shader.
                return o;
            }

            /// <summary>
            /// Fragment Shader: Calculates and applies the heat haze distortion.
            /// This is the core "strategy" implementation.
            /// </summary>
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate scrolling distortion UVs:
                // Combine the input UVs with a time-based scroll to animate the distortion texture.
                float2 scrolledUV = i.uv + _Time * _ScrollSpeed;

                // 2. Sample the distortion texture:
                // Use the sampled value (usually red or green channel) as the base for distortion.
                // We use the red and green channels to create a 2D offset.
                fixed4 distortionSample = tex2D(_DistortionTex, scrolledUV);

                // 3. Normalize distortion values and scale by strength:
                // The distortion texture typically ranges from [0,1]. To use it as an offset,
                // we want it to range from [-0.5, 0.5] then scale by strength.
                float2 distortionOffset = (distortionSample.rg - 0.5) * _DistortionStrength;

                // 4. Apply distortion to the main screen UVs:
                // This shifts the UV coordinates used to sample the main screen texture.
                float2 distortedUV = i.uv + distortionOffset;

                // 5. Sample the main screen texture with distorted UVs:
                fixed4 distortedColor = tex2D(_MainTex, distortedUV);

                // 6. Apply optional color tint:
                fixed4 finalColor = distortedColor * _HazeColor;

                // 7. Blend with the original image:
                // If _BlendAmount is 0, show full effect. If 1, show original.
                // This allows fine-tuning the intensity of the haze.
                fixed4 originalColor = tex2D(_MainTex, i.uv);
                finalColor = lerp(finalColor, originalColor, _BlendAmount);

                return finalColor;
            }
            ENDCG
        }
    }
    // Fallback shader in case this one isn't supported.
    // "Hidden/InternalErrorShader" is a good default for post-processing.
    FallBack "Hidden/InternalErrorShader"
}
```

---

### Part 3: Example Usage (How to Implement)

To use the `HeatHazeEffect` in your Unity project:

1.  **Create the Shader File:**
    *   In your Unity Project window, navigate to `Assets` (or a `Shaders` folder).
    *   Right-click -> Create -> Shader -> Unlit Shader (or any basic shader type, then replace the content).
    *   Name it `HeatHazeShader` (or whatever you prefer).
    *   Open the newly created `HeatHazeShader` file and paste the **Shader Code** from Part 2 into it, overwriting the default content. Save the file.

2.  **Create the C# Script File:**
    *   In your Unity Project window, navigate to `Assets` (or a `Scripts` folder).
    *   Right-click -> Create -> C# Script.
    *   Name it `HeatHazeEffect`.
    *   Open the `HeatHazeEffect.cs` file and paste the **C# Script** from Part 1 into it, overwriting the default content. Save the file.

3.  **Prepare the Distortion Texture:**
    *   You'll need a grayscale texture for distortion. A simple Perlin noise texture or a cloudy texture works best.
    *   You can create one in an image editor or find a suitable one online.
    *   Import this texture into your Unity project.
    *   **Important:** Select the imported texture in the Project window and ensure its `Wrap Mode` is set to `Repeat` (for seamless scrolling) and `Filter Mode` is set to `Bilinear` or `Trilinear`.

4.  **Add the Effect to Your Camera:**
    *   Select your `Main Camera` (or any camera you want to apply the effect to) in the Hierarchy.
    *   In the Inspector, click "Add Component".
    *   Search for "Heat Haze Effect" (the name defined by `AddComponentMenu`).
    *   Click to add it.

5.  **Configure the Effect in the Inspector:**
    *   With the camera still selected, look at the `Heat Haze Effect` component in the Inspector.
    *   **Haze Shader:** Drag and drop your `HeatHazeShader` asset from the Project window into this slot.
    *   **Distortion Texture:** Drag and drop your prepared grayscale noise texture into this slot.
    *   **Adjust Parameters:**
        *   **Distortion Strength:** Experiment with values between 0.01 and 0.1 for a subtle effect. Higher values create stronger distortion.
        *   **Scroll Speed:** Adjust the X and Y components to control how fast the haze moves.
        *   **Haze Color:** Change this to tint the distorted areas (e.g., orange for intense heat, red for thermal vision).
        *   **Blend Amount:** Use this to fade the effect in or out (0 = full effect, 1 = no effect).

Now, when you run your scene (or even in the Scene View if `ExecuteInEditMode` is enabled), you should see a heat haze distortion effect applied to your camera's view!