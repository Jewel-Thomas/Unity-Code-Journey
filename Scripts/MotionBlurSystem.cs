// Unity Design Pattern Example: MotionBlurSystem
// This script demonstrates the MotionBlurSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the `MotionBlurSystem` as a practical, self-contained Unity component that encapsulates the logic for applying a camera-based motion blur post-processing effect. While "MotionBlurSystem" isn't a named design pattern from the Gang of Four, this implementation adheres to several important software design principles, making it a robust and reusable "system" within a Unity project.

Here's how this example demonstrates the principles:

1.  **Component Pattern:** The `MotionBlurSystem` is a `MonoBehaviour` component, a fundamental Unity design pattern. It can be easily attached to any `Camera` GameObject, promoting modularity and reusability.
2.  **Encapsulation & Single Responsibility Principle:** All logic related to motion blur (calculating parameters, managing the shader, applying the effect, resource cleanup) is self-contained within this class. It has one clear purpose: to provide motion blur.
3.  **Resource Management:** It explicitly handles the creation and destruction of its `Material` (`OnEnable`, `OnDisable`), preventing memory leaks, especially when objects are enabled/disabled frequently or during editor workflow.
4.  **Configurability:** Public fields exposed in the Inspector allow designers and developers to tweak blur intensity, quality, and velocity thresholds without modifying code, promoting iteration and flexibility.
5.  **Dependency Management:** It uses `[RequireComponent(typeof(Camera))]` to ensure it always has a `Camera` component, and automatically obtains a reference to it, simplifying setup.
6.  **Separation of Concerns (Script vs. Shader):** The C# script manages the high-level logic and data passing, while the HLSL shader handles the low-level pixel manipulation and visual effect, demonstrating a clear separation of concerns.

---

### Step 1: Create the C# Script (`MotionBlurSystem.cs`)

Create a new C# script in your Unity project (e.g., in `Assets/Scripts/Effects`) and name it `MotionBlurSystem`. Copy and paste the following code into it:

```csharp
// MotionBlurSystem.cs
using UnityEngine;
using System.Collections; // System.Collections is generally not explicitly needed for basic MonoBehaviours, but common.

/// <summary>
///     The MotionBlurSystem design pattern, in this context, refers to a well-structured
///     and encapsulated component that manages and applies a motion blur post-processing effect
///     to a camera in Unity. While "MotionBlurSystem" isn't a named Gang of Four design pattern,
///     it embodies several core principles of good software design, making it a "system"
///     that is robust, maintainable, and reusable.
///
///     Key design principles demonstrated:
///     1.  **Component Pattern:** The MotionBlurSystem itself is a `MonoBehaviour` component
///         that can be attached to any Camera GameObject. This allows for modularity and
///         easy integration into Unity's GameObject-component architecture.
///     2.  **Encapsulation & Single Responsibility Principle:** All the logic for calculating
///         blur parameters, managing the shader, and applying the post-processing effect is
///         contained within this single class. Users of this system don't need to understand
///         the underlying shader code or complex matrix math, only how to configure the
///         exposed parameters. Its primary responsibility is to apply motion blur.
///     3.  **Resource Management:** It explicitly handles the creation and destruction of its
///         material resource (`OnEnable`, `OnDisable`), preventing memory leaks, especially
///         in the editor or when the component is frequently toggled.
///     4.  **Configurability:** Public fields allow designers and developers to easily adjust
///         blur intensity, quality, and thresholds directly in the Inspector, without
///         touching code.
///     5.  **Dependency Management:** It automatically fetches the `Camera` component it needs
///         via `RequireComponent` and `GetComponent`, ensuring it has its primary dependency
///         and streamlining setup.
///     6.  **Separation of Concerns:** The C# script manages the high-level logic and data
///         passing, while the HLSL shader handles the low-level pixel manipulation and visual
///         effect, clearly separating the responsibilities.
///
///     How it works:
///     The system operates by comparing the camera's current view-projection matrix
///     (which transforms world space to clip space) with its view-projection matrix from
///     the *previous* frame.
///
///     1.  **Initialization (`OnEnable`):** It ensures the Camera is set to render depth
///         information, as depth is crucial for reconstructing world positions in the shader.
///         It also creates a `Material` instance from the provided `Shader`. The
///         `_previousViewProjectionMatrix` is initialized to the current frame's matrix
///         to prevent a massive blur artifact on the very first frame.
///     2.  **Per-Frame Update (`OnRenderImage`):** This is Unity's built-in callback for
///         post-processing effects, called after the camera has finished rendering the scene.
///         a.  It calculates the current camera's view-projection matrix.
///         b.  It passes all necessary parameters (previous view-projection matrix, inverse
///             of current view-projection matrix, blur amount, sample count, velocity thresholds)
///             to the shader.
///         c.  The `Graphics.Blit` function then draws the `source` texture (the frame
///             rendered so far) to the `destination` texture, using our motion blur shader
///             to apply the effect.
///         d.  Crucially, before `OnRenderImage` finishes, it updates `_previousViewProjectionMatrix`
///             with the *current* frame's matrix, so it's ready for the *next* frame's calculation.
///     3.  **Shader Logic (see MotionBlurShader.shader):** The HLSL shader code performs
///         the actual visual blur:
///         a.  For each pixel, it samples its depth from the `_CameraDepthTexture`.
///         b.  Using the inverse of the current view-projection matrix and the pixel's depth
///             (transformed to clip-space Z), it reconstructs the pixel's position in 3D world space.
///         c.  It then transforms this world space position using the *previous* frame's
///             view-projection matrix to find where that pixel *would have been* on screen
///             in the previous frame.
///         d.  The difference between the current screen position and the previous screen position
///             gives a "velocity vector" in screen space.
///         e.  The shader samples the `_MainTex` (the current frame) multiple times along this
///             velocity vector and blends the results, creating the streaking effect of motion blur.
/// </summary>
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Post-processing/Motion Blur System")] // Categorize in Add Component menu
[ImageEffectAllowedInSceneView] // Allows the effect to be seen in the scene view as well
public class MotionBlurSystem : MonoBehaviour
{
    [Tooltip("The shader asset used for the motion blur effect.")]
    [SerializeField]
    private Shader motionBlurShader;

    [Tooltip("Intensity of the motion blur effect. Higher values mean more noticeable blur.")]
    [Range(0.0f, 1.0f)]
    public float blurAmount = 0.75f;

    [Tooltip("Number of samples to take along the blur vector. Higher values mean better quality but higher performance cost.")]
    [Range(2, 16)]
    public int sampleCount = 8;

    [Tooltip("Maximum pixel velocity to clamp the blur to (in screen UV space). Prevents extreme, unrealistic streaking.")]
    public float maxVelocity = 0.5f; // Max velocity in screen UV space (0 to 1)

    [Tooltip("Minimum pixel velocity threshold (in screen UV space). Pixels moving slower than this won't blur significantly, reducing noise in static scenes.")]
    public float minVelocityThreshold = 0.005f; // Min velocity in screen UV space (0 to 1)


    private Material _material;
    private Camera _camera;
    private Matrix4x4 _previousViewProjectionMatrix; // Stores the VP matrix from the previous frame

    /// <summary>
    /// Gets the material instance, creating it if necessary.
    /// Ensures material is only created once and managed properly.
    /// </summary>
    public Material Material
    {
        get
        {
            if (_material == null)
            {
                _material = CheckShaderAndCreateMaterial(motionBlurShader);
            }
            return _material;
        }
    }

    /// <summary>
    /// Called when the component is enabled. Initializes the camera, ensures depth texture, and material.
    /// </summary>
    protected virtual void OnEnable()
    {
        _camera = GetComponent<Camera>();
        // Ensure the camera renders depth information, which is crucial for motion blur.
        _camera.depthTextureMode |= DepthTextureMode.Depth;

        // Initialize previous matrix to current camera's state on enable.
        // This prevents a massive, incorrect blur artifact on the very first frame
        // or immediately after the component is enabled/disabled.
        _previousViewProjectionMatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
    }

    /// <summary>
    /// Called when the component is disabled or destroyed. Cleans up the material resource.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (_material != null)
        {
            // Destroy the material appropriate for play mode vs. editor mode
            if (Application.isPlaying)
            {
                Destroy(_material);
            }
            else
            {
                DestroyImmediate(_material);
            }
        }
        _material = null;
    }

    /// <summary>
    /// Unity's built-in callback for post-processing effects. Called by the camera after rendering the scene.
    /// </summary>
    /// <param name="source">The source render texture (the current frame rendered so far).</param>
    /// <param name="destination">The destination render texture (where the final result will be rendered).</param>
    [ImageEffectOpaque] // Ensures this effect runs after opaque geometry is rendered, before transparent objects.
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // If the material is not ready (e.g., shader not assigned or unsupported),
        // just blit (copy) the source to destination without applying the effect.
        if (Material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Calculate the current camera's view-projection matrix.
        Matrix4x4 currentViewProjectionMatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        
        // Pass all necessary parameters to the shader.
        Material.SetMatrix("_PreviousViewProjectionMatrix", _previousViewProjectionMatrix);
        // The inverse of the current view-projection matrix is needed in the shader
        // to reconstruct world positions from screen coordinates and depth.
        Material.SetMatrix("_InverseViewProjectionMatrix", currentViewProjectionMatrix.inverse); 

        Material.SetFloat("_BlurAmount", blurAmount);
        Material.SetInt("_SampleCount", sampleCount);
        Material.SetFloat("_MaxVelocity", maxVelocity);
        Material.SetFloat("_MinVelocityThreshold", minVelocityThreshold);

        // Perform the post-processing blit:
        // Render the 'source' texture to the 'destination' texture,
        // applying our 'Material' (which uses the motion blur shader).
        Graphics.Blit(source, destination, Material);

        // After rendering, update the '_previousViewProjectionMatrix' for the next frame.
        _previousViewProjectionMatrix = currentViewProjectionMatrix;
    }

    /// <summary>
    /// Helper method to check if the shader is assigned and create the material instance.
    /// </summary>
    /// <param name="s">The shader asset to use for the material.</param>
    /// <returns>A new Material instance, or null if the shader is not assigned or unsupported.</returns>
    protected Material CheckShaderAndCreateMaterial(Shader s)
    {
        if (s == null)
        {
            Debug.LogError("Motion Blur Shader is not assigned! Please assign a Motion Blur Shader asset to the MotionBlurSystem component.", this);
            return null;
        }
        if (!s.isSupported)
        {
            Debug.LogError($"Motion Blur Shader '{s.name}' is not supported on this platform! Please check your shader requirements and platform capabilities.", this);
            return null;
        }
        // Create a new material instance from the shader. HideFlags prevent it from being saved with the scene.
        return new Material(s) { hideFlags = HideFlags.HideAndDontSave };
    }

    /// <summary>
    /// Resets the previous view-projection matrix.
    /// This is useful if you teleport the camera (e.g., for a cutscene jump or level transition)
    /// and want to avoid a massive blur streak caused by the sudden perceived "movement".
    /// Call this method immediately after a sudden camera position/rotation change.
    /// </summary>
    public void ResetPreviousMatrix()
    {
        if (_camera != null)
        {
            _previousViewProjectionMatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        }
    }
}
```

---

### Step 2: Create the Shader (`MotionBlurShader.shader`)

Create a new Shader in your Unity project (e.g., in `Assets/Shaders`) and name it `MotionBlurShader`. Copy and paste the following HLSL code into it:

```shader
// MotionBlurShader.shader
Shader "Hidden/MotionBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // The screen texture of the current frame
    }
    SubShader
    {
        // For post-processing effects, we typically don't cull or write to the depth buffer.
        // We always pass the depth test as we're drawing a fullscreen quad.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // Shader model 3.0 provides good balance for modern Unity features

            #include "UnityCG.cginc" // Provides useful helper functions like ComputeScreenPos, LinearEyeDepth

            // Structure for vertex input
            struct appdata
            {
                float4 vertex : POSITION;   // Vertex position
                float2 uv : TEXCOORD0;      // UV coordinates for texture sampling
            };

            // Structure for vertex output (and fragment input)
            struct v2f
            {
                float2 uv : TEXCOORD0;      // UV coordinates passed to fragment shader
                float4 vertex : SV_POSITION; // Clip-space position for the vertex
                // screenPos is typically used for sampling _CameraDepthTexture because
                // depth textures are often rendered at full resolution even if _MainTex is not.
                // However, for fullscreen post-fx, uv is often sufficient for depth texture too.
            };

            sampler2D _MainTex; // The render texture of the current frame (source from OnRenderImage)
            sampler2D _CameraDepthTexture; // Unity's depth texture (requires camera.depthTextureMode)

            // Matrices passed from the C# script
            float4x4 _PreviousViewProjectionMatrix; // View-Projection matrix from the previous frame
            float4x4 _InverseViewProjectionMatrix;  // Inverse of the current frame's View-Projection matrix

            // Blur parameters passed from the C# script
            float _BlurAmount;
            int _SampleCount;
            float _MaxVelocity;
            float _MinVelocityThreshold;

            /// <summary>
            /// Vertex shader to set up the fullscreen quad.
            /// </summary>
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform vertex to clip space
                o.uv = v.uv; // Pass UVs directly
                return o;
            }

            /// <summary>
            /// Fragment shader to apply the motion blur effect.
            /// </summary>
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Get current pixel's depth from the depth texture.
                // SAMPLE_DEPTH_TEXTURE handles differences between platforms and depth texture types.
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                // 2. Reconstruct the current pixel's position in 3D world space.
                // The `rawDepth` (0-1 range from depth buffer) needs to be converted to clip-space Z.
                // For perspective cameras, the depth buffer value is (clip.z / clip.w + 1) * 0.5.
                // So, `clip.z / clip.w = rawDepth * 2.0 - 1.0`.
                // We construct a clip-space point (NDC x,y, and the derived z_clip/w_clip)
                // and then multiply by the inverse of the current View-Projection matrix
                // to get the world position.
                float4 currentClipPos = float4(i.uv.x * 2.0 - 1.0, i.uv.y * 2.0 - 1.0, rawDepth * 2.0 - 1.0, 1.0);
                
                // Transform from clip space to world space using the inverse of the current VP matrix.
                float4 worldPos = mul(_InverseViewProjectionMatrix, currentClipPos);
                worldPos.xyz /= worldPos.w; // Perform perspective divide to get actual world coordinates

                // 3. Project the world position into the *previous* frame's clip space.
                float4 previousClipPos = mul(_PreviousViewProjectionMatrix, worldPos);
                previousClipPos.xyz /= previousClipPos.w; // Perform perspective divide for previous NDC

                // 4. Calculate the screen-space velocity vector.
                // Convert clip space positions (-1 to 1) to normalized UV space (0 to 1).
                float2 currentScreenPos = i.uv;
                float2 previousScreenPos = (previousClipPos.xy * 0.5 + 0.5);

                // The velocity is the difference between current and previous screen positions.
                float2 velocity = currentScreenPos - previousScreenPos;

                // Scale the velocity by the blur amount from the C# script.
                velocity *= _BlurAmount;

                // 5. Clamp the velocity to a maximum value to prevent overly long, unrealistic streaks.
                float velocityMagnitude = length(velocity);
                if (velocityMagnitude > _MaxVelocity)
                {
                    velocity = normalize(velocity) * _MaxVelocity;
                    velocityMagnitude = _MaxVelocity; // Update magnitude for threshold check
                }
                
                // 6. Apply a minimum velocity threshold.
                // If velocity is too small, don't blur (avoids blurring static scenes or amplifying noise).
                if (velocityMagnitude < _MinVelocityThreshold)
                {
                    return tex2D(_MainTex, i.uv); // Return original pixel color if motion is insignificant
                }

                // 7. Sample along the calculated velocity vector to create the blur effect.
                fixed4 blendedColor = fixed4(0, 0, 0, 0);

                // Adjust the number of samples based on velocity magnitude.
                // This provides better performance for small blurs and higher quality for strong blurs.
                int effectiveSampleCount = (int)lerp(2.0, _SampleCount, saturate(velocityMagnitude / _MaxVelocity));
                effectiveSampleCount = max(2, effectiveSampleCount); // Ensure at least 2 samples

                for (int j = 0; j < effectiveSampleCount; j++)
                {
                    // Calculate normalized position along the blur vector (0.0 to 1.0).
                    // We sample from the current pixel backwards along the velocity vector.
                    float t = (float)j / (float)(effectiveSampleCount - 1);
                    float2 sampleUV = i.uv - velocity * t; 
                    
                    // Sample the main texture at the calculated UV.
                    blendedColor += tex2D(_MainTex, sampleUV);
                }

                // Average the sampled colors.
                blendedColor /= (float)effectiveSampleCount;

                return blendedColor;
            }
            ENDCG
        }
    }
}
```

---

### Step 3: Setup in Unity

1.  **Create/Open a Scene:** Ensure you have a Unity scene with a `Main Camera` and some moving objects (e.g., a simple cube with a `Rigidbody` and `AddForce`, or a script moving it).
2.  **Add `MotionBlurSystem` to Camera:**
    *   Select your `Main Camera` in the Hierarchy.
    *   In the Inspector, click "Add Component" and search for "Motion Blur System". Add it.
3.  **Assign the Shader:**
    *   In the `MotionBlurSystem` component's Inspector, you will see a field labeled "Motion Blur Shader".
    *   Drag your `MotionBlurShader.shader` asset from your Project window (e.g., `Assets/Shaders/MotionBlurShader.shader`) into this field.
4.  **Verify Camera Depth Texture Mode:** The `MotionBlurSystem` script automatically attempts to set `Camera.depthTextureMode |= DepthTextureMode.Depth;` in `OnEnable`. However, if you are using URP/HDRP, you might need to ensure depth texture rendering is enabled in your Universal Render Pipeline Asset or High Definition Render Pipeline Asset settings. For built-in render pipeline, the script's `depthTextureMode` setting is usually sufficient.
5.  **Adjust Parameters:**
    *   Play your scene.
    *   Adjust the `Blur Amount`, `Sample Count`, `Max Velocity`, and `Min Velocity Threshold` parameters in the Inspector while the game is running to see their effects in real-time.
    *   Observe how moving objects (or a moving camera) create the motion blur effect.

---

This complete example provides a functional and educational demonstration of the `MotionBlurSystem` pattern, ready to be used and experimented with in your Unity projects.