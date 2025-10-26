// Unity Design Pattern Example: VFXGraphIntegration
// This script demonstrates the VFXGraphIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

The `VFXGraphIntegration` design pattern, as demonstrated here, provides a **Facade** over Unity's Visual Effect Graph (VFX Graph) components. It encapsulates the complex interactions with `VisualEffect` objects (setting parameters, triggering events) behind a simple, high-level API. This makes your main game logic cleaner, easier to read, and less coupled to the specifics of how a VFX Graph is implemented.

**Key Benefits of this Pattern:**
1.  **Abstraction:** Game logic doesn't directly call `VisualEffect.SetFloat("ParameterName", value)`. Instead, it calls `vfxController.SetHealingAuraIntensity(value)`, which is more descriptive and less error-prone.
2.  **Maintainability:** If the VFX Graph's internal parameter names change (e.g., "Intensity" becomes "AuraPower"), you only need to update the `VFXIntegrationController` script, not every script that interacts with that VFX.
3.  **Performance:** Property names are converted to integer IDs (`Shader.PropertyToID` and `VisualEffect.StringToID`) once in `Awake`, avoiding slower string lookups during runtime updates.
4.  **Centralization:** All control logic for a specific set of VFX can be managed in one place, making it easier to understand, debug, and optimize.
5.  **Reusability:** The controller can be easily attached to different GameObjects that need to exhibit similar VFX behaviors.

---

### **VFXGraphIntegration.cs**

To use this script:

1.  **Create VFX Graphs:** You'll need at least two VFX Graph assets.
    *   **"HealingAuraVFXGraph"**: Needs a `Float` parameter named `Intensity` and a `Color` parameter named `AuraColor`. It should be a continuous effect.
    *   **"DamageBurstVFXGraph"**: Needs a `Vector3` parameter named `BurstPosition` and an **Event** named `OnBurst`. It should be a one-shot effect that plays when `OnBurst` is triggered.
2.  **Create a GameObject:** In your Unity scene, create an empty GameObject (e.g., "PlayerVFXManager").
3.  **Add VisualEffect Components:**
    *   Add a `VisualEffect` component to the "PlayerVFXManager" GameObject. Assign your "HealingAuraVFXGraph" to its `VFX Graph` field.
    *   Add another `VisualEffect` component to the "PlayerVFXManager" (or a child GameObject). Assign your "DamageBurstVFXGraph" to its `VFX Graph` field.
4.  **Attach the Script:** Attach the `VFXIntegrationController.cs` script to the "PlayerVFXManager" GameObject.
5.  **Assign References:** In the Inspector, drag the `VisualEffect` components you just added to the `Healing Aura VFX` and `Damage Burst VFX` fields of the `VFXIntegrationController`.

Now, other scripts (like a `PlayerHealth` script) can easily control these VFX through the `VFXIntegrationController`.

```csharp
using UnityEngine;
using UnityEngine.VFX; // Required for VisualEffect class

/// <summary>
/// The 'VFXGraphIntegration' design pattern provides a Facade for interacting
/// with Unity's Visual Effect Graph (VFX Graph) components.
///
/// It encapsulates the complexities of setting VFX parameters and triggering
/// events, offering a clean, high-level API to other game systems.
/// This pattern improves readability, maintainability, and performance by
/// centralizing VFX control logic and caching parameter IDs.
/// </summary>
public class VFXIntegrationController : MonoBehaviour
{
    [Header("VFX Component References")]
    [Tooltip("The VisualEffect component for the persistent healing aura.")]
    [SerializeField] private VisualEffect _healingAuraVFX;

    [Tooltip("The VisualEffect component for the one-shot damage burst effect.")]
    [SerializeField] private VisualEffect _damageBurstVFX;

    [Header("VFX Graph Parameter Names (Must match VFX Graph assets!)")]
    [Tooltip("Name of the float parameter for healing aura intensity.")]
    [SerializeField] private string _healingAuraIntensityParamName = "Intensity";
    [Tooltip("Name of the color parameter for healing aura color.")]
    [SerializeField] private string _healingAuraColorParamName = "AuraColor";
    [Tooltip("Name of the Vector3 parameter for damage burst position.")]
    [SerializeField] private string _damageBurstPositionParamName = "BurstPosition";
    [Tooltip("Name of the event to trigger the damage burst.")]
    [SerializeField] private string _damageBurstEventName = "OnBurst";

    // --- Cached Property IDs for Performance ---
    // Caching string names to integer IDs prevents expensive string lookups at runtime.
    private int _healingAuraIntensityID;
    private int _healingAuraColorID;
    private int _damageBurstPositionID;
    private int _damageBurstEventID;

    private bool _isHealingAuraPlaying = false;

    /// <summary>
    /// Initializes the VFXIntegrationController.
    /// Caches parameter IDs and performs initial setup.
    /// </summary>
    private void Awake()
    {
        // --- Cache all string parameter/event names to integer IDs ---
        // This is a critical performance optimization for VFX Graph interaction.
        // Calling SetFloat("Name", ...) is slower than SetFloat(ID, ...).
        _healingAuraIntensityID = Shader.PropertyToID(_healingAuraIntensityParamName);
        _healingAuraColorID = Shader.PropertyToID(_healingAuraColorParamName);
        _damageBurstPositionID = Shader.PropertyToID(_damageBurstPositionParamName);
        _damageBurstEventID = VisualEffect.StringToID(_damageBurstEventName);

        // --- Initial Sanity Checks ---
        // Ensure all required VisualEffect components are assigned.
        if (_healingAuraVFX == null)
        {
            Debug.LogWarning($"VFXIntegrationController on '{gameObject.name}': Healing Aura VFX component is not assigned. Healing aura effects will not play.", this);
        }
        if (_damageBurstVFX == null)
        {
            Debug.LogWarning($"VFXIntegrationController on '{gameObject.name}': Damage Burst VFX component is not assigned. Damage burst effects will not play.", this);
        }

        // --- Initial State Setup for Persistent VFX ---
        // Start the healing aura at a default (e.g., zero intensity) state.
        // This ensures the VFX graph is initialized and ready to receive parameter updates.
        if (_healingAuraVFX != null)
        {
            _healingAuraVFX.Stop(); // Ensure it starts stopped, then play when needed.
            SetHealingAuraIntensity(0f); // Start invisible
            SetHealingAuraColor(Color.white); // Default color
        }
    }

    // ======================================================================
    // --- Public API for Controlling VFX ---
    // These methods provide the clean, high-level interface for other scripts.
    // ======================================================================

    #region Healing Aura Control

    /// <summary>
    /// Sets the intensity of the healing aura VFX.
    /// </summary>
    /// <param name="intensity">The desired intensity (e.g., 0.0 to 1.0).</param>
    public void SetHealingAuraIntensity(float intensity)
    {
        if (_healingAuraVFX != null && _healingAuraVFX.HasFloat(_healingAuraIntensityID))
        {
            _healingAuraVFX.SetFloat(_healingAuraIntensityID, intensity);
        }
    }

    /// <summary>
    /// Sets the color of the healing aura VFX.
    /// </summary>
    /// <param name="color">The desired color for the aura.</param>
    public void SetHealingAuraColor(Color color)
    {
        if (_healingAuraVFX != null && _healingAuraVFX.HasVector4(_healingAuraColorID)) // Color is internally a Vector4
        {
            _healingAuraVFX.SetVector4(_healingAuraColorID, color);
        }
    }

    /// <summary>
    /// Starts or resumes the healing aura VFX.
    /// </summary>
    public void PlayHealingAura()
    {
        if (_healingAuraVFX != null && !_isHealingAuraPlaying)
        {
            _healingAuraVFX.Play();
            _isHealingAuraPlaying = true;
            Debug.Log($"VFX: Healing Aura Started on '{gameObject.name}'");
        }
    }

    /// <summary>
    /// Stops the healing aura VFX.
    /// </summary>
    public void StopHealingAura()
    {
        if (_healingAuraVFX != null && _isHealingAuraPlaying)
        {
            _healingAuraVFX.Stop();
            _isHealingAuraPlaying = false;
            Debug.Log($"VFX: Healing Aura Stopped on '{gameObject.name}'");
        }
    }

    /// <summary>
    /// Checks if the healing aura VFX is currently playing.
    /// </summary>
    /// <returns>True if the healing aura is playing, false otherwise.</returns>
    public bool IsHealingAuraPlaying()
    {
        return _isHealingAuraPlaying;
    }

    #endregion

    #region Damage Burst Control

    /// <summary>
    /// Triggers a one-shot damage burst VFX at a specified world position.
    /// </summary>
    /// <param name="position">The world position where the burst should appear.</param>
    public void TriggerDamageBurst(Vector3 position)
    {
        if (_damageBurstVFX != null)
        {
            // Set the position parameter *before* sending the event
            if (_damageBurstVFX.HasVector3(_damageBurstPositionID))
            {
                _damageBurstVFX.SetVector3(_damageBurstPositionID, position);
            }
            else
            {
                Debug.LogWarning($"VFX Graph '{_damageBurstVFX.visualEffectAsset.name}' does not have a Vector3 parameter named '{_damageBurstPositionParamName}'.", this);
            }

            // Send the event to trigger the VFX effect
            _damageBurstVFX.SendEvent(_damageBurstEventID);
            Debug.Log($"VFX: Damage Burst Triggered at {position} on '{gameObject.name}'");
        }
    }

    #endregion

    #region General VFX Control (Optional)

    /// <summary>
    /// Stops all managed VFX components.
    /// </summary>
    public void StopAllVFX()
    {
        StopHealingAura();
        // If damage burst is a persistent effect that needs stopping, add it here.
        // For one-shot effects, this is usually not necessary.
    }

    #endregion

    // ======================================================================
    // --- Editor-only Context Menu for Testing ---
    // These methods provide quick ways to test functionality directly in the Inspector.
    // ======================================================================
    #region Editor Testing

    [ContextMenu("Test: Play Healing Aura")]
    private void TestPlayHealingAura()
    {
        PlayHealingAura();
        SetHealingAuraIntensity(0.5f);
        SetHealingAuraColor(Color.blue);
    }

    [ContextMenu("Test: Stop Healing Aura")]
    private void TestStopHealingAura()
    {
        StopHealingAura();
        SetHealingAuraIntensity(0f); // Make sure it visually fades out
    }

    [ContextMenu("Test: Max Healing Aura (Green)")]
    private void TestMaxHealingAura()
    {
        PlayHealingAura();
        SetHealingAuraIntensity(1f);
        SetHealingAuraColor(Color.green);
        Debug.Log("VFX Test: Max Healing Aura (Green)");
    }

    [ContextMenu("Test: Min Healing Aura (Red)")]
    private void TestMinHealingAura()
    {
        PlayHealingAura();
        SetHealingAuraIntensity(0.1f);
        SetHealingAuraColor(Color.red);
        Debug.Log("VFX Test: Min Healing Aura (Red)");
    }

    [ContextMenu("Test: Trigger Damage Burst at Self")]
    private void TestTriggerDamageBurstAtSelf()
    {
        TriggerDamageBurst(transform.position + Vector3.up * 0.5f); // Burst slightly above this GameObject
        Debug.Log("VFX Test: Damage Burst triggered at self.");
    }

    [ContextMenu("Test: Trigger Damage Burst 2m in front")]
    private void TestTriggerDamageBurstInFront()
    {
        TriggerDamageBurst(transform.position + transform.forward * 2f);
        Debug.Log("VFX Test: Damage Burst triggered 2m in front.");
    }

    #endregion
}

/*
// ======================================================================
// --- Example Usage: How another script would interact with this controller ---
// ======================================================================
// Imagine you have a 'PlayerHealth' script that needs to react visually.

public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Reference to the VFXIntegrationController on the player or a child GameObject.")]
    [SerializeField] private VFXIntegrationController _vfxController;
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;

    private void Start()
    {
        _currentHealth = _maxHealth;
        // Ensure the VFX controller is referenced
        if (_vfxController == null)
        {
            Debug.LogError("PlayerHealth: VFXIntegrationController not assigned!", this);
            return;
        }
        UpdateHealingAuraVisuals(); // Set initial aura state
    }

    /// <summary>
    /// Simulates the player taking damage.
    /// </summary>
    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(0, _currentHealth); // Clamp to 0
        Debug.Log($"Player took {amount} damage. Current health: {_currentHealth}");

        // Trigger a visual damage burst using the VFXIntegrationController
        if (_vfxController != null)
        {
            // Position the burst slightly above the player's base position
            _vfxController.TriggerDamageBurst(transform.position + Vector3.up * 0.75f);
        }

        UpdateHealingAuraVisuals(); // Update healing aura based on new health

        if (_currentHealth <= 0)
        {
            Debug.Log("Player defeated!");
            if (_vfxController != null)
            {
                _vfxController.StopHealingAura(); // Stop the aura when health is gone
                _vfxController.SetHealingAuraIntensity(0f); // Ensure it visually fades out
            }
        }
    }

    /// <summary>
    /// Simulates the player healing.
    /// </summary>
    public void Heal(float amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth); // Clamp to max
        Debug.Log($"Player healed {amount}. Current health: {_currentHealth}");

        UpdateHealingAuraVisuals(); // Update healing aura based on new health
    }

    /// <summary>
    /// Updates the healing aura's appearance based on current health.
    /// </summary>
    private void UpdateHealingAuraVisuals()
    {
        if (_vfxController == null) return;

        float healthRatio = _currentHealth / _maxHealth;
        
        // Define intensity and color interpolation based on health ratio
        float intensity = Mathf.Lerp(0f, 1f, healthRatio); // Aura becomes stronger with more health
        Color auraColor = Color.Lerp(Color.red, Color.green, healthRatio); // Aura shifts from red (low) to green (high)

        // Use the VFXIntegrationController's public API to update the aura
        _vfxController.SetHealingAuraIntensity(intensity);
        _vfxController.SetHealingAuraColor(auraColor);

        // Manage playing/stopping the aura based on health
        if (_currentHealth > 0 && !_vfxController.IsHealingAuraPlaying())
        {
            _vfxController.PlayHealingAura();
        }
        else if (_currentHealth <= 0 && _vfxController.IsHealingAuraPlaying())
        {
            _vfxController.StopHealingAura();
        }
    }

    // --- Editor Testing (Context Menu) ---
    [ContextMenu("Test: Take 30 Damage")]
    public void TestTakeDamage() => TakeDamage(30f);

    [ContextMenu("Test: Heal 20 Health")]
    public void TestHeal() => Heal(20f);

    [ContextMenu("Test: Set Full Health")]
    public void TestSetFullHealth() { _currentHealth = _maxHealth; UpdateHealingAuraVisuals(); }

    [ContextMenu("Test: Set Low Health (10)")]
    public void TestSetLowHealth() { _currentHealth = 10f; UpdateHealingAuraVisuals(); }
}
*/
```