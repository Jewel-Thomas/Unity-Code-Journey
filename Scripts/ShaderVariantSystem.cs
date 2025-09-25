// Unity Design Pattern Example: ShaderVariantSystem
// This script demonstrates the ShaderVariantSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ShaderVariantSystem' design pattern in Unity provides an elegant way to manage shader keywords and their corresponding variants on a material. Instead of directly enabling and disabling individual keywords, you define higher-level concepts (like "Low Quality," "High Quality," or "Enable Reflections") and map them to sets of shader keywords. This pattern enhances maintainability, reduces boilerplate code, and ensures material consistency.

Below is a complete C# Unity script that implements this pattern, along with an example shader to demonstrate its functionality.

---

### **1. The ShaderVariantSystem C# Script**

Create a new C# script named `ShaderVariantManager.cs` in your Unity project and paste the following code:

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for .ToUpperInvariant() for keyword consistency

// --- 1. Define Your Shader Variants (Features or Quality Levels) ---
/// <summary>
/// Defines different quality levels or feature sets that your shader can support.
/// Each enum value corresponds to a specific configuration of shader keywords.
/// </summary>
public enum GraphicsQuality
{
    Low,    // Example: Minimal features, cheaper rendering
    Medium, // Example: Balance of features and performance
    High,   // Example: More advanced features, higher quality
    Ultra   // Example: All features enabled, highest quality
}

// --- 2. Create a Serializable Structure for Editor Setup ---
/// <summary>
/// A serializable struct to map a GraphicsQuality enum value to an array of shader keywords.
/// This allows you to set up the keyword associations directly in the Unity Inspector.
/// </summary>
[System.Serializable]
public struct ShaderQualityVariant
{
    [Tooltip("The quality level this variant represents.")]
    public GraphicsQuality qualityLevel;

    [Tooltip("The shader keywords that should be enabled for this quality level. " +
             "Other keywords managed by this system will be disabled.")]
    public string[] keywordsToEnable;
}

/// <summary>
/// Implements the 'ShaderVariantSystem' design pattern in Unity.
/// This script manages shader keywords on a target material based on a selected 'GraphicsQuality' level.
///
/// **How it works:**
/// 1.  **Define Variants:** You define an enum (e.g., `GraphicsQuality`) to represent different
///     configurations or feature sets for your shader.
/// 2.  **Map Keywords:** In the Inspector, you associate each enum value with a specific
///     array of shader keywords (e.g., `GraphicsQuality.High` maps to `_HIGH_QUALITY_LIGHTING`, `_SPECULAR_ON`).
/// 3.  **Abstracted Control:** Instead of directly calling `material.EnableKeyword()` and `material.DisableKeyword()`,
///     you call a single method like `SetQuality(GraphicsQuality.High)`.
/// 4.  **Runtime Management:** The system disables all *known* keywords it manages and then enables
///     only the keywords associated with the chosen variant. This prevents keyword conflicts and ensures
///     the material is always in a consistent state for the selected variant.
///
/// **Benefits:**
/// -   **Abstraction:** Game logic doesn't need to know specific shader keywords.
///     It just deals with high-level concepts like "Low Quality" or "Enable Reflections".
/// -   **Maintainability:** If shader keywords change, you only update the mapping in this system,
///     not every place in your code that uses them.
/// -   **Consistency:** Guarantees that only the correct keywords for a variant are active,
///     avoiding unintended combinations or lingering keywords from previous states.
/// -   **Editor-Friendly:** Easy to configure in the Inspector with `[SerializeField]` fields.
/// -   **Performance:** Reduces runtime overhead by managing keyword changes efficiently.
///
/// **Practical Use Cases:**
/// -   Global graphics settings (Low/Medium/High quality).
/// -   Per-object visual effects (e.g., enabling/disabling dissolve, glowing, outlines).
/// -   Switching between different rendering techniques (e.g., PBR vs. unlit for mobile).
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensures there's a Renderer to manage a material
public class ShaderVariantManager : MonoBehaviour
{
    [Header("Target Renderer and Initial State")]
    [Tooltip("The Renderer component whose material's shader keywords will be managed. " +
             "If left unassigned, this script will try to find one on this GameObject.")]
    [SerializeField]
    private Renderer targetRenderer;

    [Tooltip("The initial graphics quality level to apply when the script starts.")]
    [SerializeField]
    private GraphicsQuality _currentQuality = GraphicsQuality.Medium;

    [Header("Shader Keyword Mappings")]
    [Tooltip("Define the keywords associated with each GraphicsQuality level. " +
             "Ensure your shaders have these keywords defined (e.g., #pragma shader_feature _LOW_QUALITY_FEATURE_ON).")]
    [SerializeField]
    private ShaderQualityVariant[] qualityVariants;

    // --- Internal State ---
    private Material _material; // The actual material instance to modify at runtime.
    private Dictionary<GraphicsQuality, HashSet<string>> _variantKeywordMap; // Fast lookup for keywords by quality.
    private HashSet<string> _allManagedKeywords; // A collection of all unique keywords this system can manage.

    // --- Public Property for Runtime Control ---
    /// <summary>
    /// Gets or sets the current GraphicsQuality level.
    /// Setting this property will automatically update the shader keywords on the material.
    /// </summary>
    public GraphicsQuality CurrentQuality
    {
        get => _currentQuality;
        set
        {
            if (_currentQuality != value)
            {
                _currentQuality = value;
                ApplyVariant(_currentQuality);
            }
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the system and applies the initial quality setting.
    /// </summary>
    void Awake()
    {
        InitializeManager();
        if (_material != null)
        {
            ApplyVariant(_currentQuality); // Apply initial quality after initialization
        }
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// This allows for previewing shader variants directly in the editor.
    /// </summary>
    void OnValidate()
    {
        // Only run if the renderer and its material are available.
        // This ensures editor preview works correctly.
        if (targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            // Re-initialize and apply variant for editor preview.
            // Accessing targetRenderer.material will create a material instance if not already existing,
            // which is generally desired for per-object modifications in the editor.
            InitializeManager();
            if (_material != null)
            {
                ApplyVariant(_currentQuality);
            }
        }
    }

    /// <summary>
    /// Initializes the internal data structures and retrieves the target material.
    /// This method sets up the mappings between enum values and shader keywords.
    /// </summary>
    private void InitializeManager()
    {
        // If targetRenderer is not assigned in the Inspector, try to get it from this GameObject.
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError("ShaderVariantManager: No Renderer component found or assigned!", this);
            return;
        }

        // Get the material instance. This creates a unique instance of the material
        // for this renderer at runtime, allowing per-object keyword modification
        // without affecting other objects using the same shared material.
        _material = targetRenderer.material;

        if (_material == null)
        {
            Debug.LogError($"ShaderVariantManager: No material found on '{targetRenderer.name}'!", this);
            return;
        }

        _variantKeywordMap = new Dictionary<GraphicsQuality, HashSet<string>>();
        _allManagedKeywords = new HashSet<string>();

        // Populate the internal dictionary and collect all unique managed keywords.
        foreach (var variant in qualityVariants)
        {
            HashSet<string> keywordsForVariant = new HashSet<string>();
            if (variant.keywordsToEnable != null)
            {
                foreach (string keyword in variant.keywordsToEnable)
                {
                    // Shader keywords are case-insensitive, but storing and comparing in uppercase
                    // ensures consistency and avoids potential issues.
                    string upperKeyword = keyword.ToUpperInvariant(); 
                    keywordsForVariant.Add(upperKeyword);
                    _allManagedKeywords.Add(upperKeyword); // Add to the master list of all keywords we manage.
                }
            }
            _variantKeywordMap[variant.qualityLevel] = keywordsForVariant;
        }

        Debug.Log($"ShaderVariantManager: Initialized for material '{_material.name}'. Total managed keywords: {_allManagedKeywords.Count}", this);
    }

    /// <summary>
    /// Applies the specified GraphicsQuality variant to the material.
    /// This method is the core of the ShaderVariantSystem pattern.
    /// </summary>
    /// <param name="quality">The GraphicsQuality level to apply.</param>
    private void ApplyVariant(GraphicsQuality quality)
    {
        // Ensure initialization has occurred before attempting to apply variants.
        if (_material == null || _variantKeywordMap == null || _allManagedKeywords == null)
        {
            InitializeManager(); // Try to re-initialize if something went wrong or wasn't ready.
            if (_material == null) return; // If still no material after re-initialization, give up.
        }

        // 1. Disable all keywords that this system is aware of and manages.
        // This ensures a clean state before enabling new ones, preventing lingering keywords
        // from previous quality settings.
        foreach (string keyword in _allManagedKeywords)
        {
            if (_material.IsKeywordEnabled(keyword)) // Only disable if currently enabled to avoid redundant calls
            {
                _material.DisableKeyword(keyword);
            }
        }

        // 2. Enable only the keywords associated with the chosen quality level.
        HashSet<string> keywordsToEnable = new HashSet<string>();
        if (_variantKeywordMap.TryGetValue(quality, out keywordsToEnable))
        {
            foreach (string keyword in keywordsToEnable)
            {
                if (!_material.IsKeywordEnabled(keyword)) // Only enable if currently disabled
                {
                    _material.EnableKeyword(keyword);
                }
            }
            // Log the change for debugging purposes.
            Debug.Log($"ShaderVariantManager: Applied {quality} variant to '{_material.name}'. Enabled keywords: {string.Join(", ", keywordsToEnable)}", this);
        }
        else
        {
            // This warning helps identify if an enum value is not mapped in the Inspector.
            Debug.LogWarning($"ShaderVariantManager: No keywords defined for quality level '{quality}'. All managed keywords have been disabled.", this);
        }
    }

    /// <summary>
    /// Public method to programmatically change the graphics quality from other scripts.
    /// </summary>
    /// <param name="newQuality">The new graphics quality to set.</param>
    public void SetQuality(GraphicsQuality newQuality)
    {
        CurrentQuality = newQuality;
    }

    /// <summary>
    /// Public method to check if a specific keyword is currently enabled on the material.
    /// Useful for debugging or conditional logic based on active variants.
    /// </summary>
    /// <param name="keyword">The keyword string to check (case-insensitive).</param>
    /// <returns>True if the keyword is enabled, false otherwise.</returns>
    public bool IsKeywordActive(string keyword)
    {
        if (_material == null) return false;
        return _material.IsKeywordEnabled(keyword.ToUpperInvariant());
    }
}
```

---

### **2. Example Shader**

Create a new Shader (e.g., `Shader/ShaderVariantSystemExample`) and paste the following code. This shader uses `#pragma shader_feature` to define variants and `#if`/`#elif` to change its rendering logic based on active keywords.

```shader
Shader "Custom/ShaderVariantSystemExample"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        // These properties are just for the inspector, not directly used in the shader logic below.
        // The actual switching happens via keywords.
        [Toggle(_LOW_QUALITY_FEATURE_ON)] _LowQualityToggle ("Low Quality Active", Float) = 0
        [Toggle(_MEDIUM_QUALITY_FEATURE_ON)] _MediumQualityToggle ("Medium Quality Active", Float) = 0
        [Toggle(_HIGH_QUALITY_FEATURE_ON)] _HighQualityToggle ("High Quality Active", Float) = 0
        [Toggle(_ULTRA_QUALITY_FEATURE_ON)] _UltraQualityToggle ("Ultra Quality Active", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" } // Adjust for URP/HDRP if needed.
        LOD 200

        CGPROGRAM
        // Use the Standard surface shader model
        #pragma surface surf Standard fullforwardshadows

        // Define shader features using #pragma shader_feature
        // These keywords will be controlled by the ShaderVariantManager script.
        // #pragma shader_feature will generate shader variants only for used keywords,
        // reducing build size compared to #pragma multi_compile if not all combinations are needed.
        #pragma shader_feature _LOW_QUALITY_FEATURE_ON
        #pragma shader_feature _MEDIUM_QUALITY_FEATURE_ON
        #pragma shader_feature _HIGH_QUALITY_FEATURE_ON
        #pragma shader_feature _ULTRA_QUALITY_FEATURE_ON

        #pragma target 3.0 // Minimum shader model target

        // Texture and property declarations
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;

        half _Smoothness;
        half _Metallic;
        fixed4 _Color;

        // Input structure for surface shader
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        // Surface function where rendering logic is defined
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base Albedo from main texture and color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Apply different logic based on which quality keyword is currently active.
            // Only one of these #if blocks will execute based on the enabled keyword.
            #if _LOW_QUALITY_FEATURE_ON
                // Low quality: Simpler look, reduced metallic/smoothness.
                o.Albedo *= 0.8; // Slightly darken
                o.Metallic = 0;
                o.Smoothness = 0.1;
            #elif _MEDIUM_QUALITY_FEATURE_ON
                // Medium quality: Basic metallic/smoothness from properties.
                o.Metallic = _Metallic;
                o.Smoothness = _Smoothness;
            #elif _HIGH_QUALITY_FEATURE_ON
                // High quality: Add normal mapping, use metallic/smoothness.
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
                o.Metallic = _Metallic;
                o.Smoothness = _Smoothness;
            #elif _ULTRA_QUALITY_FEATURE_ON
                // Ultra quality: Add normal mapping, and use a metallic/gloss map for more detail.
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
                fixed4 mg = tex2D(_MetallicGlossMap, IN.uv_MainTex);
                o.Metallic = mg.r * _Metallic; // Multiply by base metallic for control
                o.Smoothness = mg.a * _Smoothness; // Multiply by base smoothness for control
            #else
                // Fallback if no specific quality keyword is active (shouldn't happen with the manager).
                o.Metallic = 0.1;
                o.Smoothness = 0.1;
            #endif

            o.Alpha = c.a; // Pass through alpha
        }
        ENDCG
    }
    FallBack "Standard" // Fallback shader if this one fails to compile
}
```

---

### **3. Setup in Unity Editor**

1.  **Create a Material:**
    *   Right-click in your Project window -> `Create` -> `Material`.
    *   Name it `ExampleMaterial`.
    *   In the Inspector for `ExampleMaterial`, change its Shader to `Custom/ShaderVariantSystemExample`.

2.  **Create a 3D Object:**
    *   Right-click in your Hierarchy window -> `3D Object` -> `Sphere` (or Cube, Plane, etc.).
    *   Drag and drop `ExampleMaterial` onto this new 3D object in the Scene view or Hierarchy.

3.  **Add the `ShaderVariantManager` component:**
    *   Select your 3D object in the Hierarchy.
    *   In the Inspector, click `Add Component` and search for `Shader Variant Manager`.

4.  **Configure the `ShaderVariantManager` in the Inspector:**
    *   **Target Renderer:** The `Renderer` field should automatically populate with the `Mesh Renderer` component on your 3D object because of `[RequireComponent(typeof(Renderer))]`. If not, drag the `Mesh Renderer` from the same GameObject into this slot.
    *   **Initial Graphics Quality:** Set this to `Medium` (or any default you prefer).
    *   **Quality Variants:** This is where you define the mappings.
        *   Set the `Size` of `Quality Variants` to `4` (for Low, Medium, High, Ultra).
        *   **Element 0:**
            *   `Quality Level`: `Low`
            *   `Keywords To Enable` (Size `1`): `_LOW_QUALITY_FEATURE_ON`
        *   **Element 1:**
            *   `Quality Level`: `Medium`
            *   `Keywords To Enable` (Size `1`): `_MEDIUM_QUALITY_FEATURE_ON`
        *   **Element 2:**
            *   `Quality Level`: `High`
            *   `Keywords To Enable` (Size `1`): `_HIGH_QUALITY_FEATURE_ON`
        *   **Element 3:**
            *   `Quality Level`: `Ultra`
            *   `Keywords To Enable` (Size `1`): `_ULTRA_QUALITY_FEATURE_ON`

    *   *(Optional: You can now change the `Initial Graphics Quality` dropdown in the Inspector, and you will see the material's appearance change in the Scene view instantly, demonstrating the `OnValidate` functionality.)*

---

### **4. Example Usage in Another Script (Runtime)**

To change the quality at runtime, you can get a reference to the `ShaderVariantManager` and call its `SetQuality` method.

Create another C# script (e.g., `QualityChanger.cs`) and attach it to an empty GameObject or the same 3D object:

```csharp
using UnityEngine;

public class QualityChanger : MonoBehaviour
{
    public ShaderVariantManager shaderVariantManager;
    public KeyCode cycleQualityKey = KeyCode.Q; // Press 'Q' to cycle quality

    private GraphicsQuality[] allQualities;
    private int currentQualityIndex = 0;

    void Start()
    {
        if (shaderVariantManager == null)
        {
            shaderVariantManager = FindObjectOfType<ShaderVariantManager>();
            if (shaderVariantManager == null)
            {
                Debug.LogError("QualityChanger: ShaderVariantManager not found in scene!", this);
                enabled = false;
                return;
            }
        }

        // Get all defined enum values for GraphicsQuality
        allQualities = (GraphicsQuality[])System.Enum.GetValues(typeof(GraphicsQuality));
        
        // Ensure initial quality index matches the manager's current quality
        currentQualityIndex = (int)shaderVariantManager.CurrentQuality;

        Debug.Log("Press 'Q' to cycle through graphics quality settings.");
    }

    void Update()
    {
        if (Input.GetKeyDown(cycleQualityKey))
        {
            CycleQuality();
        }
    }

    void CycleQuality()
    {
        currentQualityIndex = (currentQualityIndex + 1) % allQualities.Length;
        GraphicsQuality newQuality = allQualities[currentQualityIndex];
        shaderVariantManager.SetQuality(newQuality);
        Debug.Log($"Quality changed to: {newQuality}");
    }

    // Optional: Add UI buttons for more intuitive control
    void OnGUI()
    {
        if (shaderVariantManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        GUILayout.Label($"Current Quality: {shaderVariantManager.CurrentQuality}");
        if (GUILayout.Button("Cycle Quality (Q)"))
        {
            CycleQuality();
        }
        GUILayout.EndArea();
    }
}
```

*   **Attach `QualityChanger.cs`:** Drag and drop this script onto the same 3D object that has the `ShaderVariantManager`.
*   **Assign `Shader Variant Manager` field:** Drag the `Shader Variant Manager` component from your 3D object into the `Shader Variant Manager` slot on the `QualityChanger` component.

---

### **5. Run the Scene**

Play the scene.
*   You should see your 3D object rendered with the "Medium" quality (or whatever you set as initial).
*   Press the 'Q' key (or click the button if you enabled the `OnGUI` part) to cycle through "Low," "Medium," "High," and "Ultra" quality settings.
*   Observe how the object's appearance changes based on the shader logic for each quality level. Check the Console for debug messages indicating which keywords are enabled.

This example provides a robust, educational, and practical implementation of the ShaderVariantSystem pattern, ready for use and extension in your Unity projects.