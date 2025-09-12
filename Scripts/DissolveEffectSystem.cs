// Unity Design Pattern Example: DissolveEffectSystem
// This script demonstrates the DissolveEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Dissolve Effect System** design pattern in Unity.

The core idea is to:
1.  **`DissolveEffectSystem` (Manager/Facade):** A central singleton that provides a simple API to trigger dissolve/appear effects on any GameObject. It handles the details of finding or adding the necessary `DissolvableObject` component.
2.  **`DissolvableObject` (Component/Worker):** Attached to individual GameObjects, this script manages the actual material manipulation and the dissolve/appear animation using coroutines. It instantiates materials, handles multiple renderers, and ensures proper cleanup.

**How it works:**

*   When `DissolveEffectSystem.Instance.Dissolve()` is called on a GameObject, the system:
    1.  Ensures the GameObject has a `DissolvableObject` component.
    2.  The `DissolvableObject` component then takes over:
        *   It stores references to the GameObject's original materials.
        *   It creates *new instances* of a specified `DissolveMaterial` (provided by the system) for each renderer.
        *   It copies relevant properties (like main texture, color) from the original materials to these new dissolve material instances.
        *   It assigns these new dissolve material instances to the renderers.
        *   It starts a coroutine to animate the `_DissolveAmount` property of these new materials from 0 to 1 (or 1 to 0 for `Appear`), while also applying edge color and width.
        *   Once the dissolve/appear animation is complete, it restores the original materials to the renderers.

---

### **Step 1: Create the Dissolve Shader**

You'll need a shader that supports a `_DissolveAmount` property (usually from 0 to 1), an `_EdgeColor`, and an `_EdgeWidth`.

Here's a very basic **Unlit** shader for demonstration purposes. For a real project, you'd likely use a PBR or URP/HDRP graph shader.

1.  In your Unity project, right-click in the Project window -> Create -> Shader -> Standard Surface Shader (or Universal Render Pipeline -> Shader -> Lit, if using URP).
2.  Rename it to `DissolveShader.shader`.
3.  Replace its content with the following:

```shader
Shader "Custom/DissolveShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _DissolveMap ("Dissolve Map (R)", 2D) = "white" {} // A noise texture to control dissolve pattern
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0 // 0 = fully solid, 1 = fully dissolved
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        _BurnStrength ("Burn Strength", Range(0, 5)) = 1 // How intense the burn effect is
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _DissolveMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DissolveMap;
        };

        fixed4 _Color;
        half _DissolveAmount;
        fixed4 _EdgeColor;
        half _EdgeWidth;
        half _BurnStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tint
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Sample dissolve map
            half dissolveValue = tex2D(_DissolveMap, IN.uv_DissolveMap).r;

            // Calculate dissolve threshold
            half dissolveThreshold = _DissolveAmount;

            // Calculate current dissolve edge
            half edge = saturate(dissolveThreshold - dissolveValue);
            half burn = saturate((edge / _EdgeWidth) * _BurnStrength);

            // If dissolveValue is less than threshold, it's dissolved
            if (dissolveValue < dissolveThreshold)
            {
                // If within edge range, apply edge color
                if (dissolveValue > dissolveThreshold - _EdgeWidth)
                {
                    o.Emission = _EdgeColor.rgb * burn;
                    // Slightly reduce albedo for burn edge to make emission more prominent
                    o.Albedo *= (1 - burn * 0.5); 
                }
                else
                {
                    // Fully dissolved parts become transparent (or you can discard them)
                    // For opaque render type, simply don't render. We will discard here.
                    discard;
                }
            }
            
            o.Metallic = 0;
            o.Smoothness = 0.5;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Standard"
}

```

### **Step 2: Create a Dissolve Material**

1.  In your Unity project, right-click in the Project window -> Create -> Material.
2.  Rename it to `DefaultDissolveMaterial`.
3.  In the Inspector, change its Shader to `Custom/DissolveShader`.
4.  Optionally, assign a `Dissolve Map` texture (e.g., a Perlin noise or cloud texture) for a more organic dissolve pattern. If you don't have one, Unity's default "Default-Diffuse" or "Default-Material" can work as a placeholder if you set the Dissolve Map to the texture, but a noise texture is better. You can create a simple black and white noise texture or download one.

### **Step 3: Create the C# Scripts**

Create two C# scripts: `DissolvableObject.cs` and `DissolveEffectSystem.cs`.

#### **`DissolvableObject.cs`**

This component handles the actual dissolve animation for a single object.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action

namespace DissolveSystem
{
    /// <summary>
    /// Represents an object that can be dissolved or made to appear using a shader effect.
    /// This component manages the material instances and the dissolve animation.
    /// </summary>
    [RequireComponent(typeof(Renderer))] // Ensures a Renderer is always present
    public class DissolvableObject : MonoBehaviour
    {
        // --- Shader Property IDs (cached for performance) ---
        private static readonly int _DissolveAmountPropertyID = Shader.PropertyToID("_DissolveAmount");
        private static readonly int _EdgeColorPropertyID = Shader.PropertyToID("_EdgeColor");
        private static readonly int _EdgeWidthPropertyID = Shader.PropertyToID("_EdgeWidth");
        private static readonly int _MainTexPropertyID = Shader.PropertyToID("_MainTex");
        private static readonly int _ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int _DissolveMapPropertyID = Shader.PropertyToID("_DissolveMap");
        private static readonly int _BurnStrengthPropertyID = Shader.PropertyToID("_BurnStrength");

        // --- Component References ---
        private Renderer[] _renderers;
        private List<Material[]> _originalMaterials = new List<Material[]>(); // Stores original materials for each renderer
        private List<Material[]> _dissolveMaterialInstances = new List<Material[]>(); // Stores instantiated dissolve materials

        // --- State ---
        private Coroutine _activeDissolveCoroutine;
        public bool IsDissolvingOrAppearing { get; private set; } = false;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();

            if (_renderers.Length == 0)
            {
                Debug.LogWarning($"DissolvableObject on {gameObject.name} found no renderers. Disabling component.", this);
                enabled = false;
                return;
            }

            // Store original materials for each renderer
            foreach (Renderer r in _renderers)
            {
                // We use sharedMaterials to get the array of materials if there are multiple.
                // We make copies to ensure we don't modify the original assets.
                Material[] currentMaterials = r.sharedMaterials;
                Material[] originalCopies = new Material[currentMaterials.Length];
                for (int i = 0; i < currentMaterials.Length; i++)
                {
                    originalCopies[i] = new Material(currentMaterials[i]); // Create a copy of the original material
                }
                _originalMaterials.Add(originalCopies);
            }
        }

        /// <summary>
        /// Prepares the object for dissolving by replacing its current materials
        /// with instances of the provided dissolve material, copying relevant properties.
        /// </summary>
        /// <param name="baseDissolveMaterial">The material template to use for dissolving.</param>
        /// <returns>True if preparation was successful, false otherwise.</returns>
        public bool PrepareForDissolve(Material baseDissolveMaterial)
        {
            if (baseDissolveMaterial == null)
            {
                Debug.LogError($"Dissolve material not provided for {gameObject.name}. Cannot prepare for dissolve.", this);
                return false;
            }
            if (_renderers == null || _renderers.Length == 0) return false;

            // Clear previous dissolve instances if any
            foreach (Material[] mats in _dissolveMaterialInstances)
            {
                foreach (Material mat in mats)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            _dissolveMaterialInstances.Clear();

            for (int rIdx = 0; rIdx < _renderers.Length; rIdx++)
            {
                Renderer r = _renderers[rIdx];
                Material[] currentOriginalMats = _originalMaterials[rIdx];
                Material[] newDissolveMats = new Material[currentOriginalMats.Length];

                for (int mIdx = 0; mIdx < currentOriginalMats.Length; mIdx++)
                {
                    Material originalMat = currentOriginalMats[mIdx];
                    Material dissolveMatInstance = new Material(baseDissolveMaterial);

                    // Copy essential properties from the original material to the dissolve material instance
                    if (originalMat.HasProperty(_MainTexPropertyID))
                    {
                        dissolveMatInstance.SetTexture(_MainTexPropertyID, originalMat.GetTexture(_MainTexPropertyID));
                    }
                    if (originalMat.HasProperty(_ColorPropertyID))
                    {
                        dissolveMatInstance.SetColor(_ColorPropertyID, originalMat.GetColor(_ColorPropertyID));
                    }
                    // If the dissolve material has its own _DissolveMap, we might want to keep it or override it
                    // For this example, we assume the dissolve material already has a suitable _DissolveMap
                    // If your original material *also* had a dissolve map, you'd copy it here:
                    // if (originalMat.HasProperty(_DissolveMapPropertyID)) {
                    //    dissolveMatInstance.SetTexture(_DissolveMapPropertyID, originalMat.GetTexture(_DissolveMapPropertyID));
                    // }

                    newDissolveMats[mIdx] = dissolveMatInstance;
                }
                _dissolveMaterialInstances.Add(newDissolveMats);
                r.sharedMaterials = newDissolveMats; // Assign the new instances to the renderer
            }

            return true;
        }

        /// <summary>
        /// Starts the dissolve effect, making the object disappear.
        /// </summary>
        public void StartDissolve(float duration, Color edgeColor, float edgeWidth, float burnStrength, Action onComplete = null)
        {
            if (!ValidateMaterialProperties()) return;
            if (_activeDissolveCoroutine != null) StopCoroutine(_activeDissolveCoroutine);
            _activeDissolveCoroutine = StartCoroutine(DissolveCoroutine(0f, 1f, duration, edgeColor, edgeWidth, burnStrength, onComplete));
        }

        /// <summary>
        /// Starts the appear effect, making the object reappear.
        /// </summary>
        public void StartAppear(float duration, Color edgeColor, float edgeWidth, float burnStrength, Action onComplete = null)
        {
            if (!ValidateMaterialProperties()) return;
            if (_activeDissolveCoroutine != null) StopCoroutine(_activeDissolveCoroutine);
            _activeDissolveCoroutine = StartCoroutine(DissolveCoroutine(1f, 0f, duration, edgeColor, edgeWidth, burnStrength, onComplete));
        }

        /// <summary>
        /// The main coroutine for animating the dissolve amount.
        /// </summary>
        private IEnumerator DissolveCoroutine(float startAmount, float endAmount, float duration, Color edgeColor, float edgeWidth, float burnStrength, Action onComplete)
        {
            IsDissolvingOrAppearing = true;
            float timer = 0f;

            // Set initial edge properties
            ApplyMaterialProperties(startAmount, edgeColor, edgeWidth, burnStrength);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                float currentDissolveAmount = Mathf.Lerp(startAmount, endAmount, progress);
                ApplyMaterialProperties(currentDissolveAmount, edgeColor, edgeWidth, burnStrength);
                yield return null;
            }

            // Ensure final state is set precisely
            ApplyMaterialProperties(endAmount, edgeColor, edgeWidth, burnStrength);

            IsDissolvingOrAppearing = false;
            _activeDissolveCoroutine = null;

            if (onComplete != null)
            {
                onComplete.Invoke();
            }

            // If fully dissolved, optionally disable the object or restore materials
            if (endAmount >= 0.99f) // Considered fully dissolved
            {
                gameObject.SetActive(false); // Common practice for dissolved objects
                RestoreOriginalMaterials(); // Clean up material instances
            }
            else if (endAmount <= 0.01f) // Considered fully appeared
            {
                RestoreOriginalMaterials(); // Restore original materials
            }
        }

        /// <summary>
        /// Applies the dissolve amount and edge properties to all current material instances.
        /// </summary>
        private void ApplyMaterialProperties(float dissolveAmount, Color edgeColor, float edgeWidth, float burnStrength)
        {
            foreach (Material[] mats in _dissolveMaterialInstances)
            {
                foreach (Material mat in mats)
                {
                    if (mat != null)
                    {
                        mat.SetFloat(_DissolveAmountPropertyID, dissolveAmount);
                        mat.SetColor(_EdgeColorPropertyID, edgeColor);
                        mat.SetFloat(_EdgeWidthPropertyID, edgeWidth);
                        mat.SetFloat(_BurnStrengthPropertyID, burnStrength);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the object's original materials and destroys the temporary dissolve material instances.
        /// </summary>
        public void RestoreOriginalMaterials()
        {
            if (_renderers == null || _renderers.Length == 0) return;

            for (int rIdx = 0; rIdx < _renderers.Length; rIdx++)
            {
                Renderer r = _renderers[rIdx];
                if (r != null && _originalMaterials.Count > rIdx)
                {
                    r.sharedMaterials = _originalMaterials[rIdx]; // Revert to original materials
                }
            }

            // Clean up dissolve material instances
            foreach (Material[] mats in _dissolveMaterialInstances)
            {
                foreach (Material mat in mats)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            _dissolveMaterialInstances.Clear();
        }

        /// <summary>
        /// Validates that the current materials assigned to the renderers have the necessary dissolve properties.
        /// </summary>
        private bool ValidateMaterialProperties()
        {
            if (_dissolveMaterialInstances == null || _dissolveMaterialInstances.Count == 0)
            {
                Debug.LogWarning($"Dissolve effect started on {gameObject.name} but no dissolve materials are prepared. Call PrepareForDissolve() first.", this);
                return false;
            }

            foreach (Material[] mats in _dissolveMaterialInstances)
            {
                foreach (Material mat in mats)
                {
                    if (mat == null) continue;
                    if (!mat.HasProperty(_DissolveAmountPropertyID) ||
                        !mat.HasProperty(_EdgeColorPropertyID) ||
                        !mat.HasProperty(_EdgeWidthPropertyID))
                    {
                        Debug.LogError($"Material '{mat.name}' on {gameObject.name} does not have the required dissolve properties. " +
                                       "Ensure it uses a shader like 'Custom/DissolveShader'.", mat);
                        return false;
                    }
                }
            }
            return true;
        }

        private void OnDestroy()
        {
            // Ensure all instantiated materials are cleaned up
            RestoreOriginalMaterials();
        }
    }
}
```

#### **`DissolveEffectSystem.cs`**

This is the central manager (singleton) that provides the public API to trigger dissolve effects.

```csharp
using UnityEngine;
using System; // For Action

namespace DissolveSystem
{
    /// <summary>
    /// The central system for managing and triggering dissolve effects on GameObjects.
    /// Implements a simple singleton pattern for easy global access.
    /// </summary>
    public class DissolveEffectSystem : MonoBehaviour
    {
        // --- Singleton Instance ---
        public static DissolveEffectSystem Instance { get; private set; }

        // --- Default Settings ---
        [Header("Default Dissolve Settings")]
        [Tooltip("The base material that will be instantiated and used for dissolving. " +
                 "It must use a shader with _DissolveAmount, _EdgeColor, _EdgeWidth properties.")]
        [SerializeField] private Material _defaultDissolveMaterial;
        [SerializeField] private float _defaultDuration = 2.0f;
        [SerializeField] private Color _defaultEdgeColor = new Color(1f, 0.5f, 0f, 1f); // Orange
        [SerializeField] private float _defaultEdgeWidth = 0.05f;
        [SerializeField] private float _defaultBurnStrength = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the system alive across scenes if desired
        }

        private void OnValidate()
        {
            if (_defaultDuration <= 0) _defaultDuration = 0.1f;
            if (_defaultEdgeWidth < 0) _defaultEdgeWidth = 0f;
            if (_defaultBurnStrength < 0) _defaultBurnStrength = 0f;
        }

        /// <summary>
        /// Triggers a dissolve effect on the target GameObject, making it disappear.
        /// If the GameObject doesn't have a DissolvableObject component, one will be added.
        /// </summary>
        /// <param name="target">The GameObject to dissolve.</param>
        /// <param name="duration">How long the dissolve effect should take (in seconds).</param>
        /// <param name="edgeColor">The color of the dissolve edge.</param>
        /// <param name="edgeWidth">The width of the dissolve edge.</param>
        /// <param name="burnStrength">The intensity of the burn effect at the edge.</param>
        /// <param name="onComplete">Action to invoke when the dissolve effect finishes.</param>
        public void Dissolve(GameObject target, float? duration = null, Color? edgeColor = null, float? edgeWidth = null, float? burnStrength = null, Action onComplete = null)
        {
            if (target == null)
            {
                Debug.LogWarning("Dissolve target is null.");
                return;
            }

            DissolvableObject dissolvable = GetOrCreateDissolvableObject(target);
            if (dissolvable == null) return;

            if (dissolvable.PrepareForDissolve(_defaultDissolveMaterial))
            {
                dissolvable.gameObject.SetActive(true); // Ensure object is active to start dissolve
                dissolvable.StartDissolve(
                    duration ?? _defaultDuration,
                    edgeColor ?? _defaultEdgeColor,
                    edgeWidth ?? _defaultEdgeWidth,
                    burnStrength ?? _defaultBurnStrength,
                    onComplete
                );
            }
        }

        /// <summary>
        /// Triggers an appear effect on the target GameObject, making it reappear.
        /// If the GameObject doesn't have a DissolvableObject component, one will be added.
        /// </summary>
        /// <param name="target">The GameObject to make appear.</param>
        /// <param name="duration">How long the appear effect should take (in seconds).</param>
        /// <param name="edgeColor">The color of the dissolve edge during appearance.</param>
        /// <param name="edgeWidth">The width of the dissolve edge during appearance.</param>
        /// <param name="burnStrength">The intensity of the burn effect at the edge.</param>
        /// <param name="onComplete">Action to invoke when the appear effect finishes.</param>
        public void Appear(GameObject target, float? duration = null, Color? edgeColor = null, float? edgeWidth = null, float? burnStrength = null, Action onComplete = null)
        {
            if (target == null)
            {
                Debug.LogWarning("Appear target is null.");
                return;
            }

            DissolvableObject dissolvable = GetOrCreateDissolvableObject(target);
            if (dissolvable == null) return;

            if (dissolvable.PrepareForDissolve(_defaultDissolveMaterial))
            {
                dissolvable.gameObject.SetActive(true); // Ensure object is active to start appear
                dissolvable.StartAppear(
                    duration ?? _defaultDuration,
                    edgeColor ?? _defaultEdgeColor,
                    edgeWidth ?? _defaultEdgeWidth,
                    burnStrength ?? _defaultBurnStrength,
                    onComplete
                );
            }
        }

        /// <summary>
        /// Helper method to get an existing DissolvableObject component or add a new one.
        /// </summary>
        private DissolvableObject GetOrCreateDissolvableObject(GameObject target)
        {
            DissolvableObject dissolvable = target.GetComponent<DissolvableObject>();
            if (dissolvable == null)
            {
                dissolvable = target.AddComponent<DissolvableObject>();
                // It will Awake() and get its renderers
            }
            if (!dissolvable.enabled)
            {
                // If it was disabled because no renderers were found on Awake, re-enable it and try again.
                // Or, if it legitimately has no renderers, it should remain disabled.
                // For simplicity, we assume if it's disabled, it's not ready, so we warn.
                Debug.LogWarning($"DissolvableObject on {target.name} is disabled. It might not have a Renderer, or needs re-initialization. Cannot perform dissolve.", target);
                return null;
            }
            return dissolvable;
        }

        // --- Example of how you might stop a dissolve prematurely ---
        public void StopDissolve(GameObject target)
        {
            DissolvableObject dissolvable = target.GetComponent<DissolvableObject>();
            if (dissolvable != null && dissolvable.IsDissolvingOrAppearing)
            {
                // In DissolvableObject, you'd need a public method to stop its coroutine
                // For example: dissolvable.StopCurrentEffect();
                // Then restore materials: dissolvable.RestoreOriginalMaterials();
                // For this example, re-triggering will stop the previous one.
                Debug.LogWarning($"Stopping dissolve/appear on {target.name} is not directly implemented via a public API in this example, " +
                                 "but you can add dissolvable.StopAllCoroutines() and dissolvable.RestoreOriginalMaterials() inside DissolvableObject to achieve this.", target);
            }
        }
    }
}
```

### **Step 4: Set up in Unity Scene**

1.  **Create an Empty GameObject** in your scene. Name it `DissolveEffectSystem`.
2.  **Attach the `DissolveEffectSystem.cs` script** to this GameObject.
3.  In the Inspector of `DissolveEffectSystem`, **assign your `DefaultDissolveMaterial`** (created in Step 2) to the `Default Dissolve Material` slot.
4.  **Place some 3D objects** in your scene (e.g., Unity's default Cube, Sphere, Capsule).
5.  **Assign an initial material** to these objects (e.g., a simple white material or any PBR material).
    *   *Important:* The `DissolveEffectSystem` will temporarily replace these with its `DefaultDissolveMaterial` but will copy over the `_MainTex` and `_Color` properties if they exist on your original material and the dissolve shader has them.

### **Step 5: Example Usage (Triggering Dissolves)**

To demonstrate, let's create a simple script that triggers dissolves when you press keys.

Create a new C# script named `DissolveDemo.cs` and attach it to an empty GameObject in your scene (e.g., `GameManager`).

```csharp
using UnityEngine;
using DissolveSystem; // Make sure to include the namespace

public class DissolveDemo : MonoBehaviour
{
    [Header("Objects to Dissolve")]
    [Tooltip("Drag the GameObjects you want to control here.")]
    [SerializeField] private GameObject[] _dissolvableObjects;

    [Header("Dissolve Parameters (Optional override)")]
    [SerializeField] private float _customDuration = 1.5f;
    [SerializeField] private Color _customEdgeColor = Color.cyan;
    [SerializeField] private float _customEdgeWidth = 0.08f;
    [SerializeField] private float _customBurnStrength = 2.0f;

    void Update()
    {
        // Example: Press 'D' to dissolve all assigned objects
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("Triggering Dissolve...");
            foreach (GameObject obj in _dissolvableObjects)
            {
                if (obj != null)
                {
                    // Basic dissolve with default system settings
                    // DissolveEffectSystem.Instance.Dissolve(obj, onComplete: () => {
                    //     Debug.Log($"{obj.name} fully dissolved!");
                    // });

                    // Dissolve with custom parameters
                    DissolveEffectSystem.Instance.Dissolve(
                        obj,
                        _customDuration,
                        _customEdgeColor,
                        _customEdgeWidth,
                        _customBurnStrength,
                        () => { Debug.Log($"{obj.name} fully dissolved!"); }
                    );
                }
            }
        }

        // Example: Press 'A' to make all assigned objects appear
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Triggering Appear...");
            foreach (GameObject obj in _dissolvableObjects)
            {
                if (obj != null)
                {
                    // Ensure the object is active before trying to make it appear
                    obj.SetActive(true);

                    // Basic appear with default system settings
                    // DissolveEffectSystem.Instance.Appear(obj, onComplete: () => {
                    //     Debug.Log($"{obj.name} fully appeared!");
                    // });

                    // Appear with custom parameters
                    DissolveEffectSystem.Instance.Appear(
                        obj,
                        _customDuration,
                        _customEdgeColor,
                        _customEdgeWidth,
                        _customBurnStrength,
                        () => { Debug.Log($"{obj.name} fully appeared!"); }
                    );
                }
            }
        }
    }
}
```

### **Explanation of the Pattern:**

*   **`DissolveEffectSystem` (Facade / Singleton):**
    *   **Facade:** It provides a simplified interface (`Dissolve`, `Appear`) to a complex subsystem (`DissolvableObject`, material management, coroutines). Users don't need to know the internal workings.
    *   **Singleton:** Ensures there's only one instance of the manager, making it easily accessible from anywhere in your code (`DissolveEffectSystem.Instance.Dissolve(...)`). This is practical for a system that controls a global effect.
    *   **Centralized Control:** Manages default parameters and the logic for getting/creating `DissolvableObject` components on demand.

*   **`DissolvableObject` (Component / Strategy):**
    *   **Component:** It's designed to be attached to individual GameObjects, making any object "dissolvable." This adheres to Unity's component-based architecture.
    *   **Strategy (Implicit):** The `DissolveCoroutine` encapsulates the specific algorithm for animating the dissolve, which can be seen as a strategy for applying the effect.
    *   **Material Management:** Crucially, it handles:
        *   Storing original materials for restoration.
        *   Instantiating new materials from the `DissolveEffectSystem`'s base material to avoid modifying asset files.
        *   Copying relevant properties (like textures and colors) from the original material to the new dissolve material.
        *   Cleaning up instantiated materials (`Destroy`) to prevent memory leaks.
    *   **Encapsulation:** All the nitty-gritty details of shader property manipulation and coroutine management are hidden within this component.

**Benefits of this pattern:**

*   **Decoupling:** Game logic (e.g., player dies, enemy spawns) simply calls `DissolveEffectSystem.Instance.Dissolve()`. It doesn't need to know *how* the dissolve happens.
*   **Reusability:** The `DissolveEffectSystem` and `DissolvableObject` can be used across many different objects and scenarios without rewriting dissolve logic.
*   **Maintainability:** Changes to the dissolve effect (e.g., adding more shader parameters, changing animation curve) only require modifying `DissolvableObject.cs` and potentially the shader, not every script that triggers a dissolve.
*   **Flexibility:** Default parameters can be set in the manager, but individual dissolves can override them, offering fine-grained control.
*   **Unity-friendly:** Leverages `MonoBehaviour` components, `Renderer` access, `Material` instancing, and coroutines, which are standard Unity practices.

This system is now ready to drop into a Unity project, and with the shader and material setup, it will work immediately.