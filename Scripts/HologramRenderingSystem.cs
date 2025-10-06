// Unity Design Pattern Example: HologramRenderingSystem
// This script demonstrates the HologramRenderingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "HologramRenderingSystem" design pattern, as a practical application in Unity, is best implemented using a combination of the **Strategy Pattern** and a **Manager/Service Locator Pattern**.

Here's how it works:

1.  **Strategy Pattern (`IHologramRenderStrategy`)**: Defines a family of algorithms (different ways to render a hologram) and encapsulates each one. This allows the rendering behavior to be swapped independently from the `HologramRenderer` (the context) that uses it.
    *   **Concrete Strategies**: Implement various visual effects (e.g., solid, fading, glitching, pulsating).
2.  **Context (`HologramRenderer`)**: This is a MonoBehaviour attached to any GameObject that should act as a hologram. It holds a reference to an `IHologramRenderStrategy` and delegates the actual rendering logic to it. It manages the GameObject's material and exposes properties like `intensity`.
3.  **Manager/Service Locator (`HologramRenderingSystemManager`)**: A singleton or static class that provides and manages instances of different rendering strategies. It acts as the central point for other game systems to request or apply specific hologram rendering behaviors. This provides the "System" aspect to the pattern.

This approach makes it highly flexible:
*   **Easily add new hologram effects**: Just create a new concrete strategy.
*   **Dynamically change effects**: Swap the strategy on a `HologramRenderer` at runtime.
*   **Decoupled rendering logic**: `HologramRenderer` doesn't need to know the specifics of *how* it's being rendered, only *that* it needs to render using its current strategy.

---

### **Setup Instructions in Unity:**

1.  **Create the Scripts**: Create a new C# script for each of the files listed below (e.g., `IHologramRenderStrategy.cs`, `DefaultHologramStrategy.cs`, etc.) and paste the respective code.
2.  **Create a Hologram Object**:
    *   In your Unity scene, create a 3D object (e.g., a `Cube`, `Sphere`, or any custom mesh).
    *   Attach the `HologramRenderer.cs` script to this GameObject.
3.  **Material Setup (Important!):**
    *   The `HologramRenderer` script will attempt to configure the object's existing material for transparency, or create a new default transparent material if none exists.
    *   For best results, you should ideally **create a new Material** (e.g., `Assets/Materials/HologramMat`).
    *   **Assign this Material to your 3D object's Mesh Renderer.**
    *   **For URP/HDRP:** Use a `Universal Render Pipeline/Lit` or `HDRP/Lit` shader. The script will try to set its `_Surface` mode to `Transparent`.
    *   **For Built-in RP:** Use a `Standard` shader. The script will try to set its `Rendering Mode` to `Fade`.
    *   If you have a custom unlit transparent shader, that will work too, as long as it responds to `_BaseColor` or `_Color` (specifically its alpha channel).
4.  **Initialize the Manager**:
    *   Create an empty GameObject in your scene (e.g., named `_GameManagers`).
    *   Attach the `HologramRenderingSystemManager.cs` script to this GameObject. This will make it available as a singleton.
5.  **Example Usage**: Look at the `HologramDemoController.cs` script provided at the end for how to interact with the `HologramRenderer` and the `HologramRenderingSystemManager`. Attach this `HologramDemoController.cs` to any empty GameObject in your scene and link your hologram objects in its inspector.

---

### 1. `IHologramRenderStrategy.cs`

This interface defines the contract for all hologram rendering strategies.

```csharp
using UnityEngine;

/// <summary>
/// Interface for defining different hologram rendering strategies.
/// This is the core of the Strategy Pattern. Each concrete strategy
/// will implement these methods to provide a specific visual effect.
/// </summary>
public interface IHologramRenderStrategy
{
    /// <summary>
    /// Initializes the material properties when this strategy is first applied.
    /// This method should set up the material's initial state for the given strategy.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="initialIntensity">The initial intensity of the hologram (0-1).</param>
    void Initialize(Material material, Renderer renderer, float initialIntensity);

    /// <summary>
    /// Updates the rendering state over time. Called every frame by HologramRenderer.
    /// This method is where the dynamic visual effects (like fading, glitching, pulsing) occur.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <param name="currentIntensity">The current global intensity of the hologram (0-1).</param>
    void UpdateRender(Material material, Renderer renderer, float deltaTime, float currentIntensity);

    /// <summary>
    /// Resets the material properties to a default or non-hologram state,
    /// or cleans up specific effects before a new strategy is applied or the object is destroyed.
    /// This ensures that when a strategy is removed or changed, the material isn't left in an undesirable state.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    void ResetRender(Material material, Renderer renderer);
}

```

---

### 2. `DefaultHologramStrategy.cs`

A basic strategy that renders a solid, semi-transparent hologram based on intensity.

```csharp
using UnityEngine;

/// <summary>
/// A concrete implementation of IHologramRenderStrategy for a default, solid hologram effect.
/// This strategy maintains a consistent alpha value based on the hologram's intensity.
/// </summary>
public class DefaultHologramStrategy : IHologramRenderStrategy
{
    private Color _originalColor;

    /// <summary>
    /// Initializes the hologram to a stable, semi-transparent state.
    /// Stores the original color to restore later if needed, and sets the initial alpha.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="initialIntensity">The initial intensity of the hologram (0-1).</param>
    public void Initialize(Material material, Renderer renderer, float initialIntensity)
    {
        // Store original color for potential restoration, or simply ensure it's not fully opaque.
        // Assuming "_BaseColor" for URP/HDRP Lit shaders, or "_Color" for others.
        _originalColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        
        Color targetColor = _originalColor;
        targetColor.a = initialIntensity * 0.7f; // A bit of transparency by default
        
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", targetColor);
        else material.color = targetColor;
    }

    /// <summary>
    /// Updates the hologram's appearance. For a default strategy, this typically
    /// means just adjusting the alpha based on the current intensity.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <param name="currentIntensity">The current global intensity of the hologram (0-1).</param>
    public void UpdateRender(Material material, Renderer renderer, float deltaTime, float currentIntensity)
    {
        Color currentColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        currentColor.a = currentIntensity * 0.7f; // Keep a base transparency, scale with intensity
        
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", currentColor);
        else material.color = currentColor;

        renderer.enabled = currentIntensity > 0; // Disable renderer if intensity is 0
    }

    /// <summary>
    /// Resets the hologram material to its original state (or a fully opaque/visible state)
    /// when this strategy is no longer active.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    public void ResetRender(Material material, Renderer renderer)
    {
        // Restore to original color if necessary, or just ensure it's visible.
        // For simplicity, we just set it to fully opaque if active, otherwise disable.
        if (material.HasProperty("_BaseColor"))
        {
            Color currentColor = material.GetColor("_BaseColor");
            currentColor.a = 1.0f; // Make fully opaque
            material.SetColor("_BaseColor", currentColor);
        }
        else
        {
            Color currentColor = material.color;
            currentColor.a = 1.0f; // Make fully opaque
            material.color = currentColor;
        }

        renderer.enabled = true; // Ensure renderer is enabled after reset
    }
}
```

---

### 3. `FadingHologramStrategy.cs`

A strategy that makes the hologram fade in or out over a specified duration.

```csharp
using UnityEngine;

/// <summary>
/// A concrete implementation of IHologramRenderStrategy for a fading hologram effect.
/// This strategy makes the hologram fade in or out over a specified duration.
/// </summary>
public class FadingHologramStrategy : IHologramRenderStrategy
{
    private float _fadeDuration;
    private float _currentFadeTime;
    private bool _isFadingIn;
    private Color _baseColor;

    /// <summary>
    /// Creates a new FadingHologramStrategy.
    /// </summary>
    /// <param name="duration">The total duration of the fade effect (in seconds).</param>
    /// <param name="fadeIn">True if the hologram should fade in, false if it should fade out.</param>
    public FadingHologramStrategy(float duration, bool fadeIn)
    {
        _fadeDuration = Mathf.Max(0.01f, duration); // Ensure duration is not zero
        _isFadingIn = fadeIn;
    }

    /// <summary>
    /// Initializes the hologram's fade state.
    /// If fading in, starts with alpha 0. If fading out, starts with full alpha.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="initialIntensity">The initial intensity of the hologram (0-1).</param>
    public void Initialize(Material material, Renderer renderer, float initialIntensity)
    {
        _baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        _currentFadeTime = _isFadingIn ? 0f : _fadeDuration;
        
        // Ensure renderer is enabled before fading
        renderer.enabled = true; 
        
        UpdateRender(material, renderer, 0, initialIntensity); // Apply initial state
    }

    /// <summary>
    /// Updates the fade effect over time, adjusting the material's alpha.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <param name="currentIntensity">The current global intensity of the hologram (0-1).</param>
    public void UpdateRender(Material material, Renderer renderer, float deltaTime, float currentIntensity)
    {
        if (_isFadingIn)
        {
            _currentFadeTime += deltaTime;
        }
        else
        {
            _currentFadeTime -= deltaTime;
        }

        float normalizedTime = Mathf.Clamp01(_currentFadeTime / _fadeDuration);
        float alpha = _isFadingIn ? normalizedTime : (1f - normalizedTime);
        alpha *= currentIntensity; // Scale by global intensity

        Color currentColor = _baseColor;
        currentColor.a = alpha;
        
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", currentColor);
        else material.color = currentColor;

        // Disable renderer completely when fully faded out
        renderer.enabled = alpha > 0 || _isFadingIn && normalizedTime < 1; 
    }

    /// <summary>
    /// Resets the hologram material to a fully visible state when the strategy is changed.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    public void ResetRender(Material material, Renderer renderer)
    {
        Color currentColor = _baseColor;
        currentColor.a = 1.0f; // Make fully opaque
        
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", currentColor);
        else material.color = currentColor;
        
        renderer.enabled = true; // Ensure renderer is enabled after reset
    }
}

```

---

### 4. `GlitchedHologramStrategy.cs`

A strategy that adds a flickering, glitch-like effect to the hologram.

```csharp
using UnityEngine;

/// <summary>
/// A concrete implementation of IHologramRenderStrategy for a glitched hologram effect.
/// This strategy applies random flickering and color shifts to simulate a glitch.
/// </summary>
public class GlitchedHologramStrategy : IHologramRenderStrategy
{
    private float _glitchFrequency;
    private float _glitchStrength;
    private float _timer;
    private Color _baseColor;

    /// <summary>
    /// Creates a new GlitchedHologramStrategy.
    /// </summary>
    /// <param name="frequency">How often glitches occur (e.g., 0.1 for subtle, 1.0 for frequent).</param>
    /// <param name="strength">How strong the visual glitch effect is (e.g., 0.1 for subtle, 0.5 for pronounced).</param>
    public GlitchedHologramStrategy(float frequency = 0.5f, float strength = 0.2f)
    {
        _glitchFrequency = Mathf.Max(0.01f, frequency);
        _glitchStrength = Mathf.Clamp01(strength);
    }

    /// <summary>
    /// Initializes the glitch effect, storing the base color and enabling the renderer.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="initialIntensity">The initial intensity of the hologram (0-1).</param>
    public void Initialize(Material material, Renderer renderer, float initialIntensity)
    {
        _baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        _timer = 0f;
        renderer.enabled = true; // Ensure renderer is enabled
        UpdateRender(material, renderer, 0, initialIntensity); // Apply initial state
    }

    /// <summary>
    /// Updates the glitch effect over time, randomly adjusting alpha and color tint.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <param name="currentIntensity">The current global intensity of the hologram (0-1).</param>
    public void UpdateRender(Material material, Renderer renderer, float deltaTime, float currentIntensity)
    {
        _timer += deltaTime;

        // Base alpha from intensity
        float alpha = currentIntensity * 0.7f;
        
        Color currentColor = _baseColor;

        if (_timer >= (1f / _glitchFrequency))
        {
            // Apply random alpha flicker
            float flicker = Random.Range(0.5f, 1.5f);
            alpha *= flicker;

            // Apply slight random color shift
            float colorShift = Random.Range(-_glitchStrength, _glitchStrength);
            currentColor.r = Mathf.Clamp01(_baseColor.r + colorShift);
            currentColor.g = Mathf.Clamp01(_baseColor.g + colorShift);
            currentColor.b = Mathf.Clamp01(_baseColor.b + colorShift);

            _timer = 0f; // Reset timer for next glitch
        }
        
        currentColor.a = Mathf.Clamp01(alpha);

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", currentColor);
        else material.color = currentColor;

        renderer.enabled = currentIntensity > 0 && currentColor.a > 0.01f; // Disable if intensity is 0 or completely transparent
    }

    /// <summary>
    /// Resets the material to its original state when the strategy is changed.
    /// </summary>
    /// <param name="material">The material instance to modify.</param>
    /// <param name="renderer">The Renderer component of the hologram object.</param>
    public void ResetRender(Material material, Renderer renderer)
    {
        Color currentColor = _baseColor;
        currentColor.a = 1.0f; // Make fully opaque
        
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", currentColor);
        else material.color = currentColor;
        
        renderer.enabled = true; // Ensure renderer is enabled after reset
    }
}
```

---

### 5. `HologramRenderer.cs`

This is the `MonoBehaviour` component that acts as the context for the Strategy Pattern. Attach this to your hologram GameObjects.

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // Required for RenderQueue

/// <summary>
/// The Context in the Strategy Pattern for hologram rendering.
/// This component is attached to any GameObject that should be rendered as a hologram.
/// It holds a reference to an IHologramRenderStrategy and delegates rendering updates to it.
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensures a Renderer component exists
public class HologramRenderer : MonoBehaviour
{
    [Tooltip("The initial intensity of the hologram (0-1).")]
    [SerializeField] private float _defaultIntensity = 1.0f;

    [Tooltip("The default base material to use if the object doesn't have one, or if instancing is desired.")]
    [SerializeField] private Material _defaultHologramMaterialPreset;

    private float _currentIntensity;
    /// <summary>
    /// Gets or sets the current intensity of the hologram. (0-1)
    /// This value is passed to the current rendering strategy.
    /// </summary>
    public float CurrentIntensity
    {
        get => _currentIntensity;
        set => _currentIntensity = Mathf.Clamp01(value);
    }

    private Renderer _targetRenderer;
    private Material _hologramMaterialInstance; // An instance specific to this object to avoid modifying shared assets.

    private IHologramRenderStrategy _currentStrategy;

    void Awake()
    {
        _targetRenderer = GetComponent<Renderer>();
        CurrentIntensity = _defaultIntensity;

        // Instantiate the material to ensure each hologram has its own unique material properties.
        // This prevents changes to one hologram from affecting others using the same material asset.
        if (_targetRenderer.sharedMaterial == null)
        {
            if (_defaultHologramMaterialPreset != null)
            {
                _hologramMaterialInstance = new Material(_defaultHologramMaterialPreset);
            }
            else
            {
                // Create a basic default transparent material if no preset is provided.
                _hologramMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Lit")); // URP Lit as a robust default
                if (_hologramMaterialInstance.shader == null)
                {
                    _hologramMaterialInstance = new Material(Shader.Find("Standard")); // Fallback to Standard RP
                }
                if (_hologramMaterialInstance.shader == null)
                {
                    _hologramMaterialInstance = new Material(Shader.Find("Unlit/Color")); // Basic unlit
                }
                _hologramMaterialInstance.color = Color.cyan; // Default color
            }
            _targetRenderer.material = _hologramMaterialInstance;
        }
        else
        {
            _hologramMaterialInstance = _targetRenderer.material; // Use the existing instance if already set
        }

        // Ensure the material is set up for transparency, common for holograms.
        SetMaterialToTransparent(_hologramMaterialInstance);

        // Set an initial strategy (e.g., DefaultHologramStrategy)
        // This will often be done by the HologramRenderingSystemManager or other scripts.
        SetStrategy(new DefaultHologramStrategy());
    }

    void Update()
    {
        // Delegate the rendering update to the current strategy.
        _currentStrategy?.UpdateRender(_hologramMaterialInstance, _targetRenderer, Time.deltaTime, CurrentIntensity);
    }

    /// <summary>
    /// Sets a new rendering strategy for this hologram.
    /// The old strategy's ResetRender is called, and the new strategy's Initialize is called.
    /// </summary>
    /// <param name="newStrategy">The new strategy to apply.</param>
    public void SetStrategy(IHologramRenderStrategy newStrategy)
    {
        if (newStrategy == null)
        {
            Debug.LogError("Attempted to set a null hologram rendering strategy on " + gameObject.name);
            return;
        }

        // Reset the old strategy if one exists, to clean up its effects.
        _currentStrategy?.ResetRender(_hologramMaterialInstance, _targetRenderer);

        _currentStrategy = newStrategy;
        _currentStrategy.Initialize(_hologramMaterialInstance, _targetRenderer, CurrentIntensity);
    }

    /// <summary>
    /// Configures a material to be transparent.
    /// This attempts to work with URP/HDRP Lit shaders and Built-in RP Standard shaders.
    /// </summary>
    /// <param name="material">The material to configure.</param>
    private void SetMaterialToTransparent(Material material)
    {
        if (material == null) return;

        // Check if it's a URP Lit/Unlit shader
        if (material.shader.name.Contains("Universal Render Pipeline"))
        {
            material.SetInt("_Surface", 1); // 0 = Opaque, 1 = Transparent
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_BlendMode", 0); // Alpha Blending
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0); // No ZWrite for transparency
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        // Check if it's a Built-in RP Standard shader
        else if (material.shader.name == "Standard")
        {
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.SetInt("_Mode", 2); // 2 is for Fade/Transparent
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (material.shader.name == "Unlit/Color")
        {
            // For simple Unlit/Color, we assume it's transparent by default if its alpha is less than 1.
            // No special settings usually needed beyond setting renderQueue.
             material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            Debug.LogWarning($"HologramRenderer: Material '{material.name}' uses shader '{material.shader.name}' which might not be fully supported for automatic transparency setup. Please ensure it's a transparent shader.");
        }
    }

    void OnDestroy()
    {
        // Clean up the instantiated material to prevent memory leaks.
        if (_hologramMaterialInstance != null)
        {
            _currentStrategy?.ResetRender(_hologramMaterialInstance, _targetRenderer); // Final reset
            Destroy(_hologramMaterialInstance);
        }
    }
}
```

---

### 6. `HologramRenderingSystemManager.cs`

This is the central manager (Singleton) for the HologramRenderingSystem. It provides a convenient way to get and apply strategies.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the types of hologram rendering strategies available.
/// Used by the manager to provide specific strategy instances.
/// </summary>
public enum HologramRenderStrategyType
{
    Default,
    FadingIn,
    FadingOut,
    Glitched
}

/// <summary>
/// The central manager for the Hologram Rendering System.
/// This is a Singleton that provides access to different hologram rendering strategies
/// and allows other systems to easily apply them to HologramRenderer components.
/// This acts as the Service Locator or Facade for the hologram rendering.
/// </summary>
public class HologramRenderingSystemManager : MonoBehaviour
{
    // Singleton instance
    public static HologramRenderingSystemManager Instance { get; private set; }

    // Pre-instantiated common strategies
    private DefaultHologramStrategy _defaultStrategy = new DefaultHologramStrategy();
    private FadingHologramStrategy _fadingInStrategy; // Needs parameters, so instantiated in Awake
    private FadingHologramStrategy _fadingOutStrategy; // Needs parameters
    private GlitchedHologramStrategy _glitchedStrategy; // Needs parameters

    [Header("Default Strategy Parameters")]
    [SerializeField] private float _defaultFadeDuration = 2.0f;
    [SerializeField] private float _defaultGlitchFrequency = 0.5f;
    [SerializeField] private float _defaultGlitchStrength = 0.2f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep manager alive across scenes

            // Instantiate strategies with default parameters
            _fadingInStrategy = new FadingHologramStrategy(_defaultFadeDuration, true);
            _fadingOutStrategy = new FadingHologramStrategy(_defaultFadeDuration, false);
            _glitchedStrategy = new GlitchedHologramStrategy(_defaultGlitchFrequency, _defaultGlitchStrength);
        }
    }

    /// <summary>
    /// Applies a specific rendering strategy to a HologramRenderer.
    /// This is the primary method for external systems to interact with the HologramRenderingSystem.
    /// </summary>
    /// <param name="hologramRenderer">The HologramRenderer component to apply the strategy to.</param>
    /// <param name="strategyType">The type of hologram effect to apply.</param>
    public void ApplyStrategy(HologramRenderer hologramRenderer, HologramRenderStrategyType strategyType)
    {
        if (hologramRenderer == null)
        {
            Debug.LogWarning("Cannot apply strategy to a null HologramRenderer.");
            return;
        }

        IHologramRenderStrategy strategyToApply = GetStrategy(strategyType);
        if (strategyToApply != null)
        {
            hologramRenderer.SetStrategy(strategyToApply);
        }
        else
        {
            Debug.LogError($"Could not find strategy for type: {strategyType}");
        }
    }

    /// <summary>
    /// Retrieves a specific IHologramRenderStrategy instance based on its type.
    /// Can be used if a system wants to get a strategy and apply it manually,
    /// or combine with other logic.
    /// </summary>
    /// <param name="strategyType">The type of strategy to retrieve.</param>
    /// <returns>An instance of the requested strategy, or null if not found.</returns>
    public IHologramRenderStrategy GetStrategy(HologramRenderStrategyType strategyType)
    {
        switch (strategyType)
        {
            case HologramRenderStrategyType.Default:
                return _defaultStrategy;
            case HologramRenderStrategyType.FadingIn:
                return _fadingInStrategy;
            case HologramRenderStrategyType.FadingOut:
                return _fadingOutStrategy;
            case HologramRenderStrategyType.Glitched:
                return _glitchedStrategy;
            default:
                return null;
        }
    }

    /// <summary>
    /// Allows applying a custom Fading strategy with specific duration and direction.
    /// </summary>
    public void ApplyCustomFadingStrategy(HologramRenderer hologramRenderer, float duration, bool fadeIn)
    {
        if (hologramRenderer == null) return;
        hologramRenderer.SetStrategy(new FadingHologramStrategy(duration, fadeIn));
    }

    /// <summary>
    /// Allows applying a custom Glitched strategy with specific frequency and strength.
    /// </summary>
    public void ApplyCustomGlitchedStrategy(HologramRenderer hologramRenderer, float frequency, float strength)
    {
        if (hologramRenderer == null) return;
        hologramRenderer.SetStrategy(new GlitchedHologramStrategy(frequency, strength));
    }
}
```

---

### 7. `HologramDemoController.cs` (Example Usage)

This script demonstrates how to interact with the `HologramRenderer` components and the `HologramRenderingSystemManager`. Attach this to an empty GameObject in your scene.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Demo controller to showcase the Hologram Rendering System pattern.
/// Attach this script to an empty GameObject in your scene and
/// assign your HologramRenderer objects to the list in the Inspector.
/// </summary>
public class HologramDemoController : MonoBehaviour
{
    [SerializeField] private List<HologramRenderer> _hologramsToControl;
    [SerializeField] private float _intensityChangeSpeed = 0.5f;

    private int _currentHologramIndex = 0;
    private bool _isFading = false;
    private bool _fadeInDirection = true; // For the cycle through intensity

    void Start()
    {
        if (HologramRenderingSystemManager.Instance == null)
        {
            Debug.LogError("HologramRenderingSystemManager not found in scene. Please add it to a GameObject.");
            enabled = false;
            return;
        }

        if (_hologramsToControl == null || _hologramsToControl.Count == 0)
        {
            Debug.LogWarning("No HologramRenderer components assigned to the demo controller.");
            enabled = false;
            return;
        }

        Debug.Log("Hologram Demo Controls:\n" +
                  "Press '1' to apply Default Strategy to current hologram.\n" +
                  "Press '2' to apply Fading In Strategy to current hologram.\n" +
                  "Press '3' to apply Fading Out Strategy to current hologram.\n" +
                  "Press '4' to apply Glitched Strategy to current hologram.\n" +
                  "Press 'Q' or 'E' to cycle through holograms.\n" +
                  "Press 'W' or 'S' to adjust current hologram's intensity.\n" +
                  "Press 'A' to start/stop automatic intensity fade cycle.");
        
        // Ensure all holograms start with the default strategy
        foreach (HologramRenderer hr in _hologramsToControl)
        {
            if (hr != null)
            {
                HologramRenderingSystemManager.Instance.ApplyStrategy(hr, HologramRenderStrategyType.Default);
            }
        }
        UpdateSelectionHighlight();
    }

    void Update()
    {
        if (_hologramsToControl.Count == 0 || _hologramsToControl[_currentHologramIndex] == null) return;

        HologramRenderer currentHologram = _hologramsToControl[_currentHologramIndex];

        // --- Strategy Application ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log($"Applying Default strategy to Hologram {currentHologram.name}");
            HologramRenderingSystemManager.Instance.ApplyStrategy(currentHologram, HologramRenderStrategyType.Default);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log($"Applying Fading In strategy to Hologram {currentHologram.name}");
            HologramRenderingSystemManager.Instance.ApplyCustomFadingStrategy(currentHologram, 3f, true);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log($"Applying Fading Out strategy to Hologram {currentHologram.name}");
            HologramRenderingSystemManager.Instance.ApplyCustomFadingStrategy(currentHologram, 3f, false);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log($"Applying Glitched strategy to Hologram {currentHologram.name}");
            HologramRenderingSystemManager.Instance.ApplyCustomGlitchedStrategy(currentHologram, 1.5f, 0.3f);
        }

        // --- Hologram Selection ---
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _currentHologramIndex--;
            if (_currentHologramIndex < 0) _currentHologramIndex = _hologramsToControl.Count - 1;
            UpdateSelectionHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _currentHologramIndex++;
            if (_currentHologramIndex >= _hologramsToControl.Count) _currentHologramIndex = 0;
            UpdateSelectionHighlight();
        }

        // --- Intensity Control ---
        if (Input.GetKey(KeyCode.W))
        {
            currentHologram.CurrentIntensity += Time.deltaTime * _intensityChangeSpeed;
            _isFading = false;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            currentHologram.CurrentIntensity -= Time.deltaTime * _intensityChangeSpeed;
            _isFading = false;
        }
        
        // Automatic intensity fade cycle
        if (Input.GetKeyDown(KeyCode.A))
        {
            _isFading = !_isFading;
            Debug.Log($"Automatic intensity fade: {_isFading}");
        }

        if (_isFading)
        {
            if (_fadeInDirection)
            {
                currentHologram.CurrentIntensity += Time.deltaTime * _intensityChangeSpeed * 0.5f;
                if (currentHologram.CurrentIntensity >= 1.0f) _fadeInDirection = false;
            }
            else
            {
                currentHologram.CurrentIntensity -= Time.deltaTime * _intensityChangeSpeed * 0.5f;
                if (currentHologram.CurrentIntensity <= 0.0f) _fadeInDirection = true;
            }
        }
    }

    private void UpdateSelectionHighlight()
    {
        Debug.Log($"Selected Hologram: {_hologramsToControl[_currentHologramIndex].name}");
        // You could add a visual highlight here, e.g., a temporary outline shader,
        // but for this example, a debug log is sufficient.
    }
}

```