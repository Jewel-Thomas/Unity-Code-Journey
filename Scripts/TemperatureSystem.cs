// Unity Design Pattern Example: TemperatureSystem
// This script demonstrates the TemperatureSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a "TemperatureSystem" pattern in Unity, which isn't a classical GoF design pattern, but rather a robust system design for managing temperature dynamics within a game. It focuses on modularity, extensibility, and practical application, using interfaces, ScriptableObjects, and well-defined MonoBehaviour components.

The system allows:
1.  **Entities to *have* a temperature:** Managed by `TemperatureComponent`.
2.  **Objects to *emit* temperature:** Managed by `TemperatureSource`.
3.  **Configurable effects:** Using `TemperatureEffectData` ScriptableObjects and `TemperatureThreshold` definitions.
4.  **Loose coupling:** Through the `ITemperatureAffectable` interface.

To use this code:
1.  Create a new C# script named `TemperatureSystem.cs` (or whatever you prefer) in your Unity project.
2.  Copy and paste the entire code below into this script.
3.  Create some `TemperatureEffectData` ScriptableObjects in your project (e.g., "Frostbite," "Overheating") using `Assets > Create > Temperature System > Temperature Effect Data`. Configure their properties.
4.  Attach `TemperatureComponent` to your player character or any object that needs to manage its temperature. Configure its initial temperature, min/max, change rate, and assign `TemperatureThreshold`s with your created `TemperatureEffectData` assets.
5.  Attach `TemperatureSource` to objects like campfires, ice blocks, or environmental zones. Configure its emitted temperature, range, and falloff curve.
6.  Ensure that `TemperatureSource` objects are on a layer specified by `TemperatureComponent`'s `_temperatureSourceLayer` (e.g., create a "TemperatureSources" layer in Unity and assign it).

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .Any(), .ToList()

// REGION: INTERFACES & ENUMS =========================================================================
#region Interfaces & Enums

/// <summary>
/// Defines a contract for any object in the game world that can be affected by temperature.
/// This promotes loose coupling, allowing temperature sources or managers to interact
/// with any object capable of implementing this interface without knowing its concrete type.
/// </summary>
public interface ITemperatureAffectable
{
    /// <summary>
    /// Gets the current temperature of the object.
    /// </summary>
    float CurrentTemperature { get; }

    /// <summary>
    /// Changes the object's temperature by a specified delta value.
    /// </summary>
    /// <param name="deltaTemperature">The amount to change the temperature by.</param>
    void ChangeTemperature(float deltaTemperature);

    /// <summary>
    /// Sets the object's temperature to a specific value.
    /// </summary>
    /// <param name="newTemperature">The new absolute temperature value.</param>
    void SetTemperature(float newTemperature);
}

/// <summary>
/// Enumerates different types of temperature effects that can be applied to an ITemperatureAffectable.
/// This allows for easy categorization and lookup of effects.
/// </summary>
public enum TemperatureEffectType
{
    None,
    Chilled,        // Mild cold
    Frostbite,      // Moderate cold, potential damage
    Hypothermia,    // Severe cold, critical damage
    Warm,           // Mild heat (could be positive or neutral)
    Overheating,    // Moderate heat, potential damage
    Burning         // Severe heat, critical damage
}

#endregion

// REGION: SCRIPTABLE OBJECTS =========================================================================
#region Scriptable Objects

/// <summary>
/// A ScriptableObject that defines the properties of a specific temperature-related effect.
/// Using ScriptableObjects allows designers to create and configure various effects
/// (e.g., Frostbite, Burning) as assets in the Unity Editor, making the system highly flexible
/// and extensible without modifying code.
/// </summary>
[CreateAssetMenu(fileName = "NewTemperatureEffect", menuName = "Temperature System/Temperature Effect Data")]
public class TemperatureEffectData : ScriptableObject
{
    [Tooltip("The unique type of this temperature effect.")]
    public TemperatureEffectType effectType;

    [Tooltip("A display name for the effect (e.g., 'Frostbite').")]
    public string effectName;

    [TextArea]
    [Tooltip("A brief description of what this effect does.")]
    public string description;

    [Tooltip("Damage dealt per second while this effect is active.")]
    public float damagePerSecond = 0f;

    [Tooltip("Multiplier for movement speed while this effect is active (e.g., 0.8 for slow).")]
    [Range(0f, 2f)]
    public float movementSpeedMultiplier = 1f;

    [Tooltip("Visual effect prefab to instantiate when this effect is active (e.g., particle system).")]
    public GameObject visualEffectPrefab;

    // You can add more properties here as needed, such as:
    // public float staminaDrainPerSecond;
    // public AudioClip soundEffect;
    // public Color tintColor;
}

#endregion

// REGION: MONOBEHAVIOUR COMPONENTS ===================================================================
#region MonoBehaviour Components

/// <summary>
/// Represents an object in the world that emits temperature (heat or cold).
/// Examples: a campfire, an ice block, a heating vent, a cold biome.
/// This component defines the temperature it emits, its range, and how the
/// temperature diminishes with distance (falloff curve).
/// </summary>
public class TemperatureSource : MonoBehaviour
{
    [Header("Temperature Source Settings")]
    [Tooltip("The absolute temperature emitted at the center of the source (e.g., 50 for hot, -10 for cold).")]
    [SerializeField] private float _emittedTemperature = 25f;

    [Tooltip("The maximum range at which this source can affect other objects.")]
    [SerializeField] private float _range = 10f;

    [Tooltip("A curve defining how the emitted temperature falls off with distance. " +
             "X-axis: normalized distance (0=center, 1=max range). Y-axis: temperature contribution factor (0=none, 1=full).")]
    [SerializeField] private AnimationCurve _falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public float EmittedTemperature => _emittedTemperature;
    public float Range => _range;

    /// <summary>
    /// Calculates the contribution factor of this source at a given distance.
    /// </summary>
    /// <param name="distance">The distance from the center of the source.</param>
    /// <returns>A factor between 0 and 1, indicating the strength of the temperature effect.</returns>
    public float GetFalloffFactor(float distance)
    {
        if (distance <= 0) return _falloffCurve.Evaluate(0); // At or very near center
        if (distance >= _range) return _falloffCurve.Evaluate(1); // At or beyond max range

        float normalizedDistance = distance / _range;
        return _falloffCurve.Evaluate(normalizedDistance);
    }

    /// <summary>
    /// Visualizes the temperature source's range in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = _emittedTemperature > 20f ? Color.red : Color.blue; // Red for hot, Blue for cold
        Gizmos.DrawWireSphere(transform.position, _range);

        // Draw smaller spheres to visualize falloff
        for (int i = 0; i <= 10; i++)
        {
            float r = _range * (i / 10f);
            float factor = GetFalloffFactor(r);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, factor * 0.5f);
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}

/// <summary>
/// Represents an object that has a temperature and can be affected by temperature sources.
/// This component manages the object's current temperature, applies effects based on thresholds,
/// and smooths temperature changes over time.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure there's a collider for sensing
public class TemperatureComponent : MonoBehaviour, ITemperatureAffectable
{
    /// <summary>
    /// Represents a single temperature threshold that triggers a specific effect.
    /// This struct allows configuring different effects to activate above or below certain temperatures.
    /// </summary>
    [System.Serializable]
    public struct TemperatureThreshold
    {
        [Tooltip("The temperature at which this effect is considered active.")]
        public float thresholdTemperature;
        [Tooltip("The ScriptableObject defining the effect that activates.")]
        public TemperatureEffectData effectData;
        [Tooltip("If true, effect activates when current temp >= threshold. If false, when current temp <= threshold.")]
        public bool activateAboveThreshold;
    }

    [Header("Temperature Properties")]
    [Tooltip("The current temperature of this object.")]
    [SerializeField] private float _currentTemperature = 20f;
    [Tooltip("The minimum allowed temperature for this object.")]
    [SerializeField] private float _minTemperature = -50f;
    [Tooltip("The maximum allowed temperature for this object.")]
    [SerializeField] private float _maxTemperature = 100f;
    [Tooltip("The rate at which the object's temperature adjusts towards the target temperature per second.")]
    [SerializeField] private float _temperatureChangeRate = 5f; // degrees per second

    [Header("Environmental Sensing")]
    [Tooltip("The default ambient temperature if no sources are nearby or active.")]
    [SerializeField] private float _ambientTemperature = 20f; // Default room temperature
    [Tooltip("How often (in seconds) this component scans for nearby temperature sources.")]
    [SerializeField] private float _scanInterval = 1.0f;
    [Tooltip("The radius around this object to scan for temperature sources.")]
    [SerializeField] private float _scanRadius = 15f;
    [Tooltip("The LayerMask containing 'TemperatureSource' objects to optimize scanning.")]
    [SerializeField] private LayerMask _temperatureSourceLayer;

    [Header("Temperature Effects")]
    [Tooltip("List of temperature thresholds and the effects they trigger.")]
    [SerializeField] private TemperatureThreshold[] _effectThresholds;

    // Private internal state variables
    private float _scanTimer;
    private float _targetTemperature;
    private List<TemperatureEffectData> _activeEffects = new List<TemperatureEffectData>();
    private Dictionary<TemperatureEffectType, GameObject> _activeEffectVisuals = new Dictionary<TemperatureEffectType, GameObject>();

    // ITemperatureAffectable Implementation
    public float CurrentTemperature => _currentTemperature;

    /// <summary>
    /// Changes the current temperature by a specified delta, clamping it within min/max bounds.
    /// </summary>
    /// <param name="deltaTemperature">The amount to add or subtract from current temperature.</param>
    public void ChangeTemperature(float deltaTemperature)
    {
        SetTemperature(_currentTemperature + deltaTemperature);
    }

    /// <summary>
    /// Sets the current temperature to a new absolute value, clamping it within min/max bounds.
    /// </summary>
    /// <param name="newTemperature">The new absolute temperature.</param>
    public void SetTemperature(float newTemperature)
    {
        _currentTemperature = Mathf.Clamp(newTemperature, _minTemperature, _maxTemperature);
    }

    private void Awake()
    {
        // Initialize timer to immediately scan on start
        _scanTimer = _scanInterval;
        _targetTemperature = _ambientTemperature; // Start with ambient as target
    }

    private void Update()
    {
        HandleTemperatureScanning();
        AdjustTemperature();
        HandleEffects();
    }

    /// <summary>
    /// Manages the periodic scanning for nearby temperature sources.
    /// This helps optimize performance by not scanning every frame.
    /// </summary>
    private void HandleTemperatureScanning()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0)
        {
            _scanTimer = _scanInterval;
            CalculateTargetTemperature();
        }
    }

    /// <summary>
    /// Calculates the desired target temperature based on ambient temperature and
    /// contributions from all nearby TemperatureSource objects.
    /// It uses a weighted average approach where sources contribute based on their falloff factor.
    /// </summary>
    private void CalculateTargetTemperature()
    {
        // Use Physics.OverlapSphere to find all colliders within the scanRadius on the specified layer.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _scanRadius, _temperatureSourceLayer);

        List<TemperatureSource> nearbySources = new List<TemperatureSource>();
        foreach (Collider hitCollider in hitColliders)
        {
            TemperatureSource source = hitCollider.GetComponentInParent<TemperatureSource>(); // Use GetComponentInParent if source is on a child
            if (source != null && source != this) // Ensure we don't pick up our own source if we also have one
            {
                nearbySources.Add(source);
            }
        }

        float totalWeight = 0f;
        float weightedTemperatureSum = 0f;

        // Start with ambient temperature as a base contribution.
        // A default weight ensures that if no sources are found, we still have a target.
        const float ambientWeight = 1.0f;
        weightedTemperatureSum += _ambientTemperature * ambientWeight;
        totalWeight += ambientWeight;

        foreach (TemperatureSource source in nearbySources)
        {
            float distance = Vector3.Distance(transform.position, source.transform.position);
            float falloffFactor = source.GetFalloffFactor(distance);

            // Each source contributes its temperature, weighted by its falloff factor.
            weightedTemperatureSum += source.EmittedTemperature * falloffFactor;
            totalWeight += falloffFactor;
        }

        // Calculate the weighted average to get the final target temperature.
        if (totalWeight > 0)
        {
            _targetTemperature = weightedTemperatureSum / totalWeight;
        }
        else
        {
            _targetTemperature = _ambientTemperature; // Fallback if no sources and 0 ambient weight (shouldn't happen with ambientWeight > 0)
        }

        // Debug.Log($"Target Temperature for {gameObject.name}: {_targetTemperature:F2}C (Sources: {nearbySources.Count})");
    }

    /// <summary>
    /// Smoothly adjusts the current temperature towards the calculated target temperature
    /// using a linear interpolation (Lerp) over time, controlled by _temperatureChangeRate.
    /// </summary>
    private void AdjustTemperature()
    {
        float temperatureDelta = _targetTemperature - _currentTemperature;
        float changeAmount = temperatureDelta * _temperatureChangeRate * Time.deltaTime;

        ChangeTemperature(changeAmount);

        // Optional: Clamp temperature directly here as well to be safe, although SetTemperature already does.
        // _currentTemperature = Mathf.Clamp(_currentTemperature, _minTemperature, _maxTemperature);

        // Debug.Log($"Current Temperature for {gameObject.name}: {_currentTemperature:F2}C");
    }

    /// <summary>
    /// Evaluates current temperature against defined thresholds and activates/deactivates
    /// associated temperature effects. Also applies effects' continuous actions (like damage).
    /// </summary>
    private void HandleEffects()
    {
        // Remove effects that are no longer valid
        foreach (var activeEffect in _activeEffects.ToList()) // ToList to allow modification during iteration
        {
            bool stillValid = false;
            foreach (var threshold in _effectThresholds)
            {
                if (activeEffect.effectType == threshold.effectData.effectType)
                {
                    bool conditionMet = threshold.activateAboveThreshold
                                        ? _currentTemperature >= threshold.thresholdTemperature
                                        : _currentTemperature <= threshold.thresholdTemperature;
                    if (conditionMet)
                    {
                        stillValid = true;
                        break;
                    }
                }
            }
            if (!stillValid)
            {
                RemoveEffect(activeEffect);
            }
        }

        // Apply new effects or re-apply existing ones (e.g., for continuous damage)
        foreach (var threshold in _effectThresholds)
        {
            bool shouldActivate = threshold.activateAboveThreshold
                                  ? _currentTemperature >= threshold.thresholdTemperature
                                  : _currentTemperature <= threshold.thresholdTemperature;

            if (shouldActivate && !_activeEffects.Any(e => e.effectType == threshold.effectData.effectType))
            {
                ApplyEffect(threshold.effectData);
            }
        }

        // Apply continuous effects for all currently active effects
        foreach (var effect in _activeEffects)
        {
            if (effect.damagePerSecond > 0)
            {
                // In a real game, you would interact with a HealthComponent here
                // Example: GetComponent<HealthComponent>()?.TakeDamage(effect.damagePerSecond * Time.deltaTime);
                Debug.Log($"{gameObject.name} is taking {effect.damagePerSecond * Time.deltaTime:F2} damage from {effect.effectName}.");
            }
            // Other continuous effects like stamina drain, etc.
        }
    }

    /// <summary>
    /// Activates a specific temperature effect, adding it to the list of active effects
    /// and instantiating its visual prefab if provided.
    /// </summary>
    /// <param name="effectData">The TemperatureEffectData to apply.</param>
    private void ApplyEffect(TemperatureEffectData effectData)
    {
        if (_activeEffects.Contains(effectData)) return; // Already active

        Debug.Log($"<color=orange>Applying effect:</color> {effectData.effectName} to {gameObject.name}");
        _activeEffects.Add(effectData);

        // Instantiate visual effect if present
        if (effectData.visualEffectPrefab != null)
        {
            GameObject vfx = Instantiate(effectData.visualEffectPrefab, transform);
            _activeEffectVisuals[effectData.effectType] = vfx;
        }

        // Apply immediate effects or modify player stats here (e.g., apply movement speed multiplier)
        // Example for player movement:
        // var playerMovement = GetComponent<PlayerMovement>();
        // if (playerMovement != null) playerMovement.ModifySpeedMultiplier(effectData.movementSpeedMultiplier);
    }

    /// <summary>
    /// Deactivates a specific temperature effect, removing it from the active effects list
    /// and destroying its associated visual prefab.
    /// </summary>
    /// <param name="effectData">The TemperatureEffectData to remove.</param>
    private void RemoveEffect(TemperatureEffectData effectData)
    {
        if (!_activeEffects.Contains(effectData)) return;

        Debug.Log($"<color=green>Removing effect:</color> {effectData.effectName} from {gameObject.name}");
        _activeEffects.Remove(effectData);

        // Destroy visual effect
        if (_activeEffectVisuals.TryGetValue(effectData.effectType, out GameObject vfx))
        {
            Destroy(vfx);
            _activeEffectVisuals.Remove(effectData.effectType);
        }

        // Reverse any immediate effects or stat modifications
        // Example for player movement:
        // var playerMovement = GetComponent<PlayerMovement>();
        // if (playerMovement != null) playerMovement.ResetSpeedMultiplier(); // Or pass 1.0f
    }

    /// <summary>
    /// Visualizes the component's scan radius and current temperature in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _scanRadius);

        // Display current temperature
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 20;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Temp: {_currentTemperature:F1}C", style);
    }
}

#endregion
```