// Unity Design Pattern Example: RayMarchingEffect
// This script demonstrates the RayMarchingEffect pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'RayMarchingEffect' pattern in Unity. While "Ray Marching" is primarily a rendering technique (an algorithm), the "pattern" refers to the common architectural approach used to integrate such complex, GPU-intensive effects into a Unity project.

**The 'RayMarchingEffect' Design Pattern in Unity:**

This pattern establishes a clear separation of concerns:

1.  **The C# Script (Controller):**
    *   A `MonoBehaviour` (e.g., `RayMarchingEffect.cs`) attached to a camera.
    *   **Purpose:** Orchestrates the effect. It references the ray marching `Shader`, creates and manages a `Material` from it, passes dynamic parameters (camera matrices, time, user-defined values) to the shader, and applies the effect using `OnRenderImage`.
    *   **Benefits:** Allows easy control of the effect from the Unity Editor or other scripts, manages shader parameters, and handles the post-processing pipeline integration.

2.  **The Material (Data Carrier):**
    *   A Unity `Material` asset.
    *   **Purpose:** Serves as the bridge between the C# script and the shader. It holds the reference to the `Shader` and stores the current values for its properties.
    *   **Benefits:** Can be pre-configured in the Editor, allowing different instances of the effect to use different visual styles or shaders without code changes.

3.  **The Shader (Core Logic):**
    *   An HLSL/GLSL shader (e.g., `RayMarchingShader.shader`).
    *   **Purpose:** Contains the actual ray marching algorithm. This includes:
        *   **Signed Distance Functions (SDFs):** Mathematical functions describing the shapes in the scene (e.g., spheres, boxes).
        *   **Scene Definition:** Combining SDFs to form complex geometry.
        *   **Ray Marching Loop:** Iteratively moving a ray from the camera until it hits a surface (distance to surface is very small) or goes too far.
        *   **Normal Calculation:** Estimating surface normals for lighting.
        *   **Lighting Model:** Calculating the final color based on normals, light direction, and surface properties.
    *   **Benefits:** Highly performant as it runs entirely on the GPU, allowing for complex procedural geometry and effects not easily achievable with traditional mesh rendering.

**Example Scenario:**
This example renders a procedurally generated scene with a moving sphere and a static box, demonstrating basic ray marching, SDFs, and lighting, all controlled by a C# script.

---

## Part 1: C# Script (`RayMarchingEffect.cs`)

Create a new C# script named `RayMarchingEffect` in your Unity project (e.g., in a `Scripts` folder).

```csharp
using UnityEngine;
using System.Collections;
using System; // For [Serializable] if needed for custom structs, though not used directly here.

/// <summary>
/// RayMarchingEffect: A Unity C# script demonstrating the Ray Marching effect design pattern.
/// This script orchestrates the rendering of a procedural scene using a ray marching shader.
/// </summary>
/// <remarks>
/// This script is designed to be attached to a Camera. It acts as a post-processing effect,
/// rendering the ray-marched scene directly to the screen.
///
/// **Design Pattern Explanation:**
/// 1.  **Controller (C# Script):** This script manages the effect. It holds a reference to the shader,
///     creates a material from it, and passes crucial data (camera parameters, time, effect settings)
///     to the shader before rendering.
/// 2.  **Material (Data Carrier):** The 'material' field acts as the bridge. It's an instance of
///     Unity's Material class, configured with our custom ray marching shader. It stores the values
///     of the shader properties that this script modifies.
/// 3.  **Shader (Core Logic):** The actual ray marching algorithm, SDFs, and lighting are implemented
///     in the associated HLSL/GLSL shader. The C# script just feeds it data.
///
/// This modularity allows for easy modification of parameters from C#, or swapping out the shader
/// for different ray-marched scenes without changing the C# controller logic.
/// </remarks>
[ExecuteInEditMode] // Allows the effect to run in the editor's scene view
[RequireComponent(typeof(Camera))] // Ensures a Camera component is present
[AddComponentMenu("Rendering/Ray Marching Effect")] // Adds to component menu for easy access
public class RayMarchingEffect : MonoBehaviour
{
    [Header("Shader References")]
    [Tooltip("The shader containing the ray marching logic.")]
    [SerializeField] private Shader rayMarchingShader;

    // A private material instance created from the shader.
    // We use a property to ensure it's always initialized.
    private Material _rayMarchingMaterial;
    private Material RayMarchingMaterial
    {
        get
        {
            if (_rayMarchingMaterial == null && rayMarchingShader != null)
            {
                _rayMarchingMaterial = new Material(rayMarchingShader);
                _rayMarchingMaterial.hideFlags = HideFlags.HideAndDontSave; // Don't save this material instance to disk
            }
            return _rayMarchingMaterial;
        }
    }

    [Header("Ray Marching Parameters")]
    [Tooltip("Maximum number of steps the ray can take.")]
    [Range(32, 256)]
    [SerializeField] private int maxSteps = 128;

    [Tooltip("Maximum distance the ray can travel before giving up.")]
    [Range(10f, 100f)]
    [SerializeField] private float maxDistance = 50.0f;

    [Tooltip("Distance threshold for considering a hit (surface accuracy).")]
    [Range(0.001f, 0.1f)]
    [SerializeField] private float surfaceDistance = 0.01f;

    [Header("Scene / Lighting Parameters")]
    [Tooltip("Direction of the primary light source.")]
    [SerializeField] private Vector3 lightDirection = new Vector3(0.5f, 0.8f, 0.3f);

    [Tooltip("Color of the dynamic sphere.")]
    [SerializeField] private Color sphereColor = new Color(0.2f, 0.7f, 1.0f);

    [Tooltip("Color of the static box.")]
    [SerializeField] private Color boxColor = new Color(1.0f, 0.5f, 0.2f);

    private Camera _camera;
    private Camera MyCamera
    {
        get
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
            return _camera;
        }
    }

    // Called when the script is enabled or the value of one of its exposed parameters is changed in the editor.
    private void OnEnable()
    {
        if (rayMarchingShader == null)
        {
            Debug.LogError("RayMarchingEffect: Ray Marching Shader is not assigned! Please assign a shader in the inspector.");
            enabled = false; // Disable the component if no shader is assigned
            return;
        }

        // Ensure the camera renders depth and Opaque objects correctly for post-processing context.
        // For a pure ray marching effect that ignores existing geometry, depth is not strictly needed.
        // However, if we later wanted to combine it, this would be useful.
        // MyCamera.depthTextureMode |= DepthTextureMode.Depth; 
    }

    // Called when the script is loaded or a value is changed in the inspector.
    // Good for validation during development.
    private void OnValidate()
    {
        if (rayMarchingShader == null)
        {
            // Try to find the shader by name if not assigned. Useful for initial setup.
            rayMarchingShader = Shader.Find("Hidden/RayMarchingShader");
        }
        // Ensure material is reset if shader changes or during editor recompile
        if (_rayMarchingMaterial != null && _rayMarchingMaterial.shader != rayMarchingShader)
        {
            DestroyImmediate(_rayMarchingMaterial);
            _rayMarchingMaterial = null;
        }
    }

    // This function is called by the camera after the scene has been rendered.
    // It's the core of Unity's post-processing effects.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // If the shader or material isn't ready, just blit the source directly to the destination
        // (i.e., render the scene without the effect).
        if (RayMarchingMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // --- Pass Camera and Scene Data to the Shader ---

        // 1. Camera Matrices: Essential for reconstructing the ray from camera space.
        //    _InverseProjectionMatrix: Converts clip space to camera space.
        //    _InverseViewMatrix: Converts camera space to world space.
        RayMarchingMaterial.SetMatrix("_InverseProjectionMatrix", MyCamera.projectionMatrix.inverse);
        RayMarchingMaterial.SetMatrix("_InverseViewMatrix", MyCamera.worldToCameraMatrix.inverse);

        // 2. Ray Marching Parameters:
        RayMarchingMaterial.SetInt("_MaxSteps", maxSteps);
        RayMarchingMaterial.SetFloat("_MaxDistance", maxDistance);
        RayMarchingMaterial.SetFloat("_SurfaceDistance", surfaceDistance);

        // 3. Scene / Lighting Parameters:
        //    Normalize light direction as it's used in lighting calculations.
        RayMarchingMaterial.SetVector("_LightDir", lightDirection.normalized);
        RayMarchingMaterial.SetColor("_SphereColor", sphereColor);
        RayMarchingMaterial.SetColor("_BoxColor", boxColor);

        // 4. Time: Useful for animating objects in the ray-marched scene.
        RayMarchingMaterial.SetFloat("_Time", Time.time);

        // --- Apply the Effect ---
        // Graphics.Blit draws a full-screen quad and applies the material's shader.
        // The '_MainTex' property of the shader will automatically receive the 'source' RenderTexture.
        Graphics.Blit(source, destination, RayMarchingMaterial);
    }

    // Clean up the dynamically created material when the script is disabled or destroyed.
    private void OnDisable()
    {
        if (_rayMarchingMaterial != null)
        {
            // DestroyImmediate is used in editor to clean up assets instantly.
            // In a build, Destroy() would be sufficient.
            if (Application.isEditor)
            {
                DestroyImmediate(_rayMarchingMaterial);
            }
            else
            {
                Destroy(_rayMarchingMaterial);
            }
            _rayMarchingMaterial = null;
        }
    }
}
```

---

## Part 2: Shader (`RayMarchingShader.shader`)

Create a new Shader Graph (or just a plain `Standard Surface Shader` and replace its content) and name it `RayMarchingShader` (e.g., in a `Shaders` folder). **Important: Ensure the shader name matches `Shader "Hidden/RayMarchingShader"` inside the file.** For post-processing effects, it's often good practice to put them in the `Hidden` category to prevent them from showing up in the regular shader selection menus.

```shader
Shader "Hidden/RayMarchingShader"
{
    // Properties are typically used for material settings exposed in the inspector.
    // For a post-processing shader where the C# script controls parameters,
    // they might not be strictly necessary here, but can be useful for debugging
    // or if you want to set some defaults directly on a material.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // Required for Graphics.Blit
    }

    SubShader
    {
        // We don't need culling, depth testing, or writing to the depth buffer
        // because we are rendering a full-screen quad.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // Provides useful helper functions like UnityObjectToClipPos

            // --- C# Script Parameters ---
            // These variables are set by the C# script (RayMarchingEffect.cs).
            // Naming convention: Use the same names as SetFloat(), SetVector(), etc., in C#.
            uniform int _MaxSteps;
            uniform float _MaxDistance;
            uniform float _SurfaceDistance;

            uniform float _Time; // Provided by Unity automatically or by our C# script

            uniform float3 _LightDir; // Normalized light direction
            uniform float4 _SphereColor;
            uniform float4 _BoxColor;

            // Camera matrices passed from C# for ray reconstruction
            uniform float4x4 _InverseProjectionMatrix;
            uniform float4x4 _InverseViewMatrix;

            // _MainTex is automatically provided by Graphics.Blit() and contains the previous frame's render.
            // For a pure raymarching effect, we might not sample from it, but it's required by the Blit function.
            sampler2D _MainTex;
            float4 _MainTex_ST; // Scale and Transform for _MainTex

            // Structure for vertex shader output (and fragment shader input)
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

            // --- Vertex Shader ---
            // This is a standard vertex shader for rendering a full-screen quad.
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform vertex to clip space
                o.uv = v.uv; // Pass texture coordinates
                return o;
            }

            // --- Helper Functions for Ray Marching ---

            // Reconstruct a world space ray from screen UVs.
            // This is crucial for ray marching from the camera.
            void GetRay(float2 uv, out float3 rayOrigin, out float3 rayDirection)
            {
                // Convert UV to clip space (-1 to 1)
                float2 clipXY = uv * 2.0 - 1.0;

                // Create a point in clip space at the far plane (z=1)
                // w=1 for a point, w=0 for a direction. Here we use 1, and the matrix multiplication
                // will correctly handle perspective division.
                float4 clipPos = float4(clipXY, 1.0, 1.0);

                // Convert clip space to camera space using inverse projection matrix
                float4 cameraPos = mul(_InverseProjectionMatrix, clipPos);
                // Correct for perspective divide (cameraPos.w is 0 if perspective.w was 0)
                cameraPos /= cameraPos.w;

                // Ray origin is the camera's world position
                rayOrigin = mul(_InverseViewMatrix, float4(0,0,0,1)).xyz; // Camera's world position (0,0,0 in camera space)

                // Ray direction is from camera origin to the world point derived from clip space
                rayDirection = normalize(mul(_InverseViewMatrix, cameraPos).xyz - rayOrigin);
            }

            // --- Signed Distance Functions (SDFs) ---
            // These functions return the shortest distance from a point 'p' to a primitive.
            // A negative distance means 'p' is inside the primitive.

            // Sphere SDF
            float sdSphere(float3 p, float r)
            {
                return length(p) - r;
            }

            // Box SDF
            float sdBox(float3 p, float3 b)
            {
                float3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }

            // Union (combine two SDFs, taking the minimum distance)
            float opUnion(float d1, float d2)
            {
                return min(d1, d2);
            }

            // --- Scene Definition ---
            // This function defines the geometry of our ray-marched world.
            // It returns the shortest distance from 'p' to any object in the scene,
            // and an 'id' to identify which object was closest.
            float2 map(float3 p)
            {
                float timeFactor = sin(_Time * 0.5) * 0.5 + 0.5; // Oscillates between 0 and 1
                float3 spherePos = float3(sin(_Time) * 3.0, 1.0, 5.0 + timeFactor * 5.0);
                float sphereRadius = 1.5 + timeFactor * 0.5;

                float dSphere = sdSphere(p - spherePos, sphereRadius); // Moving sphere
                float dBox = sdBox(p - float3(0.0, -1.0, 10.0), float3(4.0, 0.5, 4.0)); // Static box (ground-like)

                // Combine objects using union (min distance)
                float res = opUnion(dSphere, dBox);
                // Return distance and an object ID (0 for sphere, 1 for box)
                // This ID helps us pick colors or materials later.
                if (dSphere < dBox)
                    return float2(dSphere, 0.0); // Sphere is closer
                else
                    return float2(dBox, 1.0);    // Box is closer
            }

            // --- Normal Calculation ---
            // Estimates the surface normal at a point 'p' by sampling the SDF around it.
            float3 GetNormal(float3 p)
            {
                float2 epsilon = float2(_SurfaceDistance * 1.5, 0); // Use a slightly larger epsilon than surfaceDistance
                // Calculate gradients along each axis
                return normalize(float3(
                    map(p + epsilon.xyy).x - map(p - epsilon.xyy).x,
                    map(p + epsilon.yxy).x - map(p - epsilon.yxy).x,
                    map(p + epsilon.yyx).x - map(p - epsilon.yyx).x
                ));
            }

            // --- Fragment Shader ---
            // This is executed for each pixel on the screen.
            float4 frag (v2f i) : SV_Target
            {
                // 1. Reconstruct World Ray
                float3 rayOrigin, rayDirection;
                GetRay(i.uv, rayOrigin, rayDirection);

                float totalDistanceTraveled = 0.0;
                float2 hitResult = float2(_MaxDistance, -1.0); // .x = distance, .y = object ID

                // 2. Ray Marching Loop
                for (int step = 0; step < _MaxSteps; step++)
                {
                    float3 currentPosition = rayOrigin + rayDirection * totalDistanceTraveled;
                    hitResult = map(currentPosition);
                    float distanceToScene = hitResult.x;

                    // If we're very close to the surface, we've hit something.
                    if (distanceToScene < _SurfaceDistance)
                    {
                        break;
                    }

                    // If we've traveled too far, stop.
                    if (totalDistanceTraveled > _MaxDistance)
                    {
                        break;
                    }

                    // Advance the ray
                    totalDistanceTraveled += distanceToScene;
                }

                float4 finalColor = float4(0, 0, 0, 1); // Background color (black)

                // 3. Shading if Hit
                if (totalDistanceTraveled < _MaxDistance && hitResult.x < _SurfaceDistance)
                {
                    float3 hitPosition = rayOrigin + rayDirection * totalDistanceTraveled;
                    float3 normal = GetNormal(hitPosition);

                    // Basic Diffuse Lighting (Lambertian)
                    float diffuse = max(0.0, dot(normal, _LightDir));
                    float3 ambient = 0.1; // Simple ambient light

                    float4 objectColor;
                    if (hitResult.y == 0.0) // Sphere
                    {
                        objectColor = _SphereColor;
                    }
                    else // Box
                    {
                        objectColor = _BoxColor;
                    }

                    finalColor.rgb = objectColor.rgb * (ambient + diffuse);
                    finalColor.a = 1.0;
                }

                return finalColor;
            }
            ENDCG
        }
    }
}
```

---

## Example Usage and Setup Instructions:

1.  **Create the C# Script:**
    *   In your Unity project, navigate to `Assets` -> `Create` -> `C# Script`.
    *   Name it `RayMarchingEffect`.
    *   Copy and paste the code from **Part 1** into this new script, replacing its default content.

2.  **Create the Shader:**
    *   In your Unity project, navigate to `Assets` -> `Create` -> `Shader` -> `Standard Surface Shader`.
    *   Name it `RayMarchingShader`. (You can also pick `Unlit Shader` if you prefer a simpler template).
    *   **Crucially**, open the `RayMarchingShader.shader` file and **replace its entire content** with the code from **Part 2**.
    *   **Ensure the first line of the shader file reads `Shader "Hidden/RayMarchingShader"`** exactly. The `Hidden/` prefix prevents it from appearing in the regular shader selection menus, which is common for post-processing shaders.

3.  **Assign the Shader to the C# Script:**
    *   Select your `Main Camera` in the Hierarchy.
    *   Click `Add Component` in the Inspector and search for `Ray Marching Effect` and add it.
    *   In the `Ray Marching Effect` component, locate the `Ray Marching Shader` field.
    *   Drag and drop your `RayMarchingShader` asset (from your Project window) into this field.

4.  **Configure Camera (Optional but Recommended):**
    *   Ensure your camera's `Clear Flags` are set to `Solid Color` or `Skybox` if you want a background. For a pure ray marching effect, the clear color will only be visible where the ray marching does not draw.
    *   Adjust the `Far Clipping Plane` of the camera. While ray marching doesn't use it directly for geometry, it can affect other camera-related processes or for debugging. `_MaxDistance` in our shader controls the ray march render distance.

5.  **Run the Scene:**
    *   Press the Play button. You should now see a procedurally generated sphere animating above a static box, rendered entirely by the ray marching shader.
    *   Experiment with the parameters in the `Ray Marching Effect` component on your camera (e.g., `Max Steps`, `Light Direction`, `Sphere Color`, `Box Color`) to see the immediate results.

This setup provides a complete, working example of the 'RayMarchingEffect' pattern, making it educational and practical for your Unity projects!