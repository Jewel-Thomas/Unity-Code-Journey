// Unity Design Pattern Example: GravityShiftSystem
// This script demonstrates the GravityShiftSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'GravityShiftSystem' design pattern, as interpreted here, combines the **Strategy Pattern** with a **Singleton Manager** to provide a flexible and centralized way to control gravity for specific objects in a Unity project.

Here's how it works:

1.  **`IGravityStrategy` (Strategy Interface):** Defines a common interface for all gravity behaviors. Any class that implements this interface can be a "gravity strategy."
2.  **Concrete Gravity Strategies:** These are specific implementations of `IGravityStrategy` (e.g., `NormalGravityStrategy`, `ZeroGravityStrategy`, `UpwardGravityStrategy`, `CustomDirectionGravityStrategy`, `PointGravityStrategy`). Each encapsulates a different way to apply gravity.
3.  **`GravityAffected` (Context/Target Component):** This component is attached to any `GameObject` with a `Rigidbody` that should respond to the `GravityShiftSystem`. It's crucial for it to disable Unity's default `useGravity` to avoid conflicts. It registers and unregisters itself with the central system.
4.  **`GravityShiftSystem` (Singleton Manager):** This is the central control hub, implemented as a Singleton.
    *   It holds a reference to the currently active `IGravityStrategy`.
    *   It maintains a list of all `GravityAffected` objects.
    *   In its `FixedUpdate` (for physics consistency), it iterates through all registered `GravityAffected` objects and tells the *current* gravity strategy to apply its force to their `Rigidbody`s.
    *   It provides public methods (`SetGravityStrategy`, `SetCustomGravityStrategy`) to change the active gravity strategy at runtime, allowing seamless transitions between different gravity behaviors.
    *   It also exposes an event (`OnGravityStrategyChanged`) for other systems to react when gravity shifts.

This pattern allows you to easily introduce new gravity types without modifying existing code (Open/Closed Principle), manage gravity consistently across multiple objects, and shift gravity dynamically during gameplay (e.g., entering a low-gravity zone, solving a puzzle by rotating gravity, or creating a black hole effect).

---

## Complete C# Unity Script: GravityShiftSystem.cs

This script contains all the necessary components for the GravityShiftSystem pattern. Save it as `GravityShiftSystem.cs` in your Unity project.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action event delegate

/// <summary>
/// --- 1. The IGravityStrategy Interface ---
/// This interface defines the contract for any gravity behavior.
/// It's the core of the Strategy design pattern, allowing different gravity algorithms
/// to be interchangeable.
/// </summary>
public interface IGravityStrategy
{
    /// <summary>
    /// Applies a force representing gravity to the given Rigidbody.
    /// For ForceMode.Force, Unity handles the physics timestep internally (FixedUpdate),
    /// so a direct deltaTime parameter is not explicitly needed here.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply gravity to.</param>
    void ApplyGravity(Rigidbody rb);
}

/// <summary>
/// --- 2. Concrete Gravity Strategy Implementations ---
/// These classes implement the IGravityStrategy interface, each defining a specific
/// way gravity should act.
/// </summary>

/// <summary>
/// Implements standard downward gravity, similar to Unity's default, but custom-controlled.
/// </summary>
public class NormalGravityStrategy : IGravityStrategy
{
    private Vector3 _gravityDirection;
    private float _gravityMagnitude;

    public NormalGravityStrategy(float magnitude = 9.81f, Vector3? direction = null)
    {
        _gravityMagnitude = magnitude;
        _gravityDirection = direction ?? Vector3.down; // Default to down if not specified
    }

    public void ApplyGravity(Rigidbody rb)
    {
        // Apply a continuous force. Using ForceMode.Force adds a continuous force
        // to the rigidbody, using its mass, resulting in constant acceleration.
        rb.AddForce(_gravityDirection.normalized * _gravityMagnitude * rb.mass, ForceMode.Force);
    }
}

/// <summary>
/// Implements zero gravity (floating in space).
/// </summary>
public class ZeroGravityStrategy : IGravityStrategy
{
    public void ApplyGravity(Rigidbody rb)
    {
        // Do nothing, effectively applying zero gravity.
    }
}

/// <summary>
/// Implements upward gravity (anti-gravity).
/// </summary>
public class UpwardGravityStrategy : IGravityStrategy
{
    private float _gravityMagnitude;

    public UpwardGravityStrategy(float magnitude = 9.81f)
    {
        _gravityMagnitude = magnitude;
    }

    public void ApplyGravity(Rigidbody rb)
    {
        rb.AddForce(Vector3.up * _gravityMagnitude * rb.mass, ForceMode.Force);
    }
}

/// <summary>
/// Implements gravity in an arbitrary custom direction.
/// </summary>
public class CustomDirectionGravityStrategy : IGravityStrategy
{
    private Vector3 _gravityDirection;
    private float _gravityMagnitude;

    public CustomDirectionGravityStrategy(Vector3 direction, float magnitude = 9.81f)
    {
        _gravityDirection = direction.normalized; // Ensure direction is a unit vector
        _gravityMagnitude = magnitude;
    }

    public void ApplyGravity(Rigidbody rb)
    {
        rb.AddForce(_gravityDirection * _gravityMagnitude * rb.mass, ForceMode.Force);
    }
}

/// <summary>
/// Implements gravity pulling towards a specific point in space, like a miniature planet or black hole.
/// </summary>
public class PointGravityStrategy : IGravityStrategy
{
    public Transform _centerOfGravity; // Public field for easier inspection/access in manager
    private float _gravityMagnitude;
    private float _maxDistance; // Optional: beyond this, gravity might weaken or stop.

    public PointGravityStrategy(Transform center, float magnitude = 9.81f, float maxDistance = float.MaxValue)
    {
        _centerOfGravity = center;
        _gravityMagnitude = magnitude;
        _maxDistance = maxDistance;
    }

    public void ApplyGravity(Rigidbody rb)
    {
        if (_centerOfGravity == null) return;

        Vector3 direction = _centerOfGravity.position - rb.position;
        float distance = direction.magnitude;

        // Apply force only if the object is within a valid range and not at the exact center
        if (distance > 0.1f && distance < _maxDistance) // Avoid division by zero, and respect max distance
        {
            // For simplicity, using a constant force magnitude towards the center.
            // For inverse square law (more realistic):
            // float forceMagnitude = _gravityMagnitude * rb.mass / (distance * distance + 0.1f); // Add a small value to avoid division by zero near center
            float forceMagnitude = _gravityMagnitude * rb.mass; // Constant force towards center
            rb.AddForce(direction.normalized * forceMagnitude, ForceMode.Force);
        }
    }
}


/// <summary>
/// --- 3. The GravityAffected Component ---
/// This component marks a Rigidbody as being managed by the GravityShiftSystem.
/// It automatically disables Unity's default gravity for these GameObjects to prevent conflicts.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityAffected : MonoBehaviour
{
    private Rigidbody _rigidbody;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            // IMPORTANT: Disable Unity's default gravity for this Rigidbody
            // to prevent double application of gravity (our custom gravity + Unity's).
            _rigidbody.useGravity = false;
        }
    }

    void OnEnable()
    {
        // Register this Rigidbody with the GravityShiftSystem when enabled.
        // It's crucial that GravityShiftSystem.Instance is accessible here.
        // Ensure GravityShiftSystem initializes before any GravityAffected objects if possible,
        // e.g., by setting Unity's script execution order, or having the system GameObject placed
        // earlier in the scene hierarchy.
        GravityShiftSystem.Instance?.RegisterAffectedObject(this);
    }

    void OnDisable()
    {
        // Unregister this Rigidbody when disabled or destroyed.
        // Check for Instance existence as the system might be destroyed before this object.
        GravityShiftSystem.Instance?.UnregisterAffectedObject(this);
    }

    // Public getter for the Rigidbody
    public Rigidbody Rigidbody => _rigidbody;
}


/// <summary>
/// --- 4. The GravityShiftSystem (Singleton Manager) ---
/// This is the central hub for managing and applying gravity across the scene.
/// It uses the Singleton pattern to ensure only one instance exists and
/// is easily accessible globally.
/// </summary>
public class GravityShiftSystem : MonoBehaviour
{
    // Singleton instance for global access.
    public static GravityShiftSystem Instance { get; private set; }

    // Enum to represent common gravity types for easy switching via Inspector or code.
    public enum GravityPresetType
    {
        Normal,
        ZeroG,
        Upward,
        CustomDirection, // Requires a specific direction to be set externally or use default
        PointTowards,    // Requires a specific target Transform to pull objects towards
        Custom           // For completely unique, runtime-defined strategies
    }

    [Header("Default Gravity Settings")]
    [Tooltip("The initial type of gravity to apply when the system starts.")]
    [SerializeField] private GravityPresetType _initialGravityType = GravityPresetType.Normal;
    [Tooltip("The magnitude of gravity force for Normal, Upward, CustomDirection, and PointTowards strategies.")]
    [SerializeField] private float _defaultGravityMagnitude = 9.81f;
    [Tooltip("The default direction for the CustomDirection strategy (e.g., Vector3.right).")]
    [SerializeField] private Vector3 _defaultCustomDirection = Vector3.down;
    [Tooltip("The default target Transform for the PointTowards strategy. Required for initial Point Towards gravity.")]
    [SerializeField] private Transform _defaultPointGravityTarget;

    // The currently active gravity strategy. This reference is swapped when gravity shifts.
    private IGravityStrategy _currentGravityStrategy;

    // A list of all GravityAffected components whose Rigidbodies are managed by this system.
    private readonly List<GravityAffected> _affectedObjects = new List<GravityAffected>();

    // Event that can be subscribed to when the gravity strategy changes.
    // This allows other systems to react to gravity shifts (e.g., visual effects, UI updates).
    public event Action<GravityPresetType> OnGravityStrategyChanged;


    void Awake()
    {
        // Implement the Singleton pattern: Ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        Instance = this;
        // Optionally keep the system alive across scene loads.
        // Remove this line if each scene should have its own GravityShiftSystem.
        DontDestroyOnLoad(gameObject);

        InitializeGravityStrategies();
    }

    void Start()
    {
        // Set the initial gravity strategy based on inspector settings.
        // We defer this to Start() to ensure all Awake() calls (including GravityAffected registrations) have completed.
        SetGravityStrategy(_initialGravityType, _defaultCustomDirection, _defaultPointGravityTarget);
    }

    // Store common strategy instances to avoid repeated allocations.
    // Strategies with dynamic parameters (like CustomDirection or PointTowards) might need
    // re-instantiation if their parameters change, or require specific setters.
    private Dictionary<GravityPresetType, IGravityStrategy> _presetStrategies = new Dictionary<GravityPresetType, IGravityStrategy>();

    /// <summary>
    /// Initializes and caches instances of common gravity strategies.
    /// </summary>
    private void InitializeGravityStrategies()
    {
        _presetStrategies[GravityPresetType.Normal] = new NormalGravityStrategy(_defaultGravityMagnitude);
        _presetStrategies[GravityPresetType.ZeroG] = new ZeroGravityStrategy();
        _presetStrategies[GravityPresetType.Upward] = new UpwardGravityStrategy(_defaultGravityMagnitude);
        
        // Initialize CustomDirection and PointTowards strategies with defaults.
        // These might be re-created if different parameters are passed to SetGravityStrategy later.
        _presetStrategies[GravityPresetType.CustomDirection] = new CustomDirectionGravityStrategy(_defaultCustomDirection.normalized, _defaultGravityMagnitude);
        if (_defaultPointGravityTarget != null)
        {
            _presetStrategies[GravityPresetType.PointTowards] = new PointGravityStrategy(_defaultPointGravityTarget, _defaultGravityMagnitude);
        }
        else if (_initialGravityType == GravityPresetType.PointTowards)
        {
            // Handle case where initial point gravity is requested but no target is set.
            Debug.LogWarning("GravityShiftSystem: Initial gravity set to 'Point Towards' but no 'Default Point Gravity Target' is assigned. Will fall back to Normal gravity.");
            _initialGravityType = GravityPresetType.Normal; // Fallback
        }
    }


    /// <summary>
    /// Registers a GravityAffected object with the system so its Rigidbody can be influenced.
    /// </summary>
    /// <param name="obj">The GravityAffected component to register.</param>
    public void RegisterAffectedObject(GravityAffected obj)
    {
        if (obj != null && obj.Rigidbody != null && !_affectedObjects.Contains(obj))
        {
            _affectedObjects.Add(obj);
            // Debug.Log($"GravityAffected object '{obj.name}' registered."); // Uncomment for verbose logging
        }
    }

    /// <summary>
    /// Unregisters a GravityAffected object from the system.
    /// </summary>
    /// <param name="obj">The GravityAffected component to unregister.</param>
    public void UnregisterAffectedObject(GravityAffected obj)
    {
        if (obj != null && _affectedObjects.Remove(obj))
        {
            // Debug.Log($"GravityAffected object '{obj.name}' unregistered."); // Uncomment for verbose logging
        }
    }

    /// <summary>
    /// FixedUpdate is used for physics calculations to ensure consistent behavior
    /// regardless of frame rate. This is where gravity forces are applied.
    /// </summary>
    void FixedUpdate()
    {
        if (_currentGravityStrategy == null) return;

        // Iterate through all registered objects and apply the current gravity strategy.
        // We iterate backward to safely remove null/destroyed objects during iteration
        // (e.g., if a GravityAffected GameObject was destroyed in the middle of a frame).
        for (int i = _affectedObjects.Count - 1; i >= 0; i--)
        {
            GravityAffected affectedObj = _affectedObjects[i];
            if (affectedObj == null || affectedObj.Rigidbody == null)
            {
                _affectedObjects.RemoveAt(i); // Clean up null entries (object was destroyed/removed)
                continue;
            }
            _currentGravityStrategy.ApplyGravity(affectedObj.Rigidbody);
        }
    }

    /// <summary>
    /// Shifts the gravity to a new predefined type using an enum.
    /// Use this for common gravity states like Normal, ZeroG, Upward, or dynamically parameterized ones.
    /// </summary>
    /// <param name="type">The desired GravityPresetType.</param>
    /// <param name="customDirection">Optional: The direction for CustomDirection strategy. Overrides default if provided.</param>
    /// <param name="pointTarget">Optional: The target Transform for PointTowards strategy. Overrides default if provided.</param>
    public void SetGravityStrategy(GravityPresetType type, Vector3? customDirection = null, Transform pointTarget = null)
    {
        IGravityStrategy newStrategy = null;

        // Logic to get or create the appropriate strategy instance based on the requested type.
        switch (type)
        {
            case GravityPresetType.Normal:
            case GravityPresetType.ZeroG:
            case GravityPresetType.Upward:
                // For these constant strategies, retrieve from cache.
                _presetStrategies.TryGetValue(type, out newStrategy);
                break;
            case GravityPresetType.CustomDirection:
                // Create a new instance if a specific direction is passed, otherwise use the cached default.
                if (customDirection.HasValue)
                {
                    newStrategy = new CustomDirectionGravityStrategy(customDirection.Value.normalized, _defaultGravityMagnitude);
                }
                else
                {
                    _presetStrategies.TryGetValue(type, out newStrategy); // Use the default custom direction strategy
                }
                break;
            case GravityPresetType.PointTowards:
                // Create a new instance if a specific target is passed, otherwise use the cached default.
                if (pointTarget != null)
                {
                    newStrategy = new PointGravityStrategy(pointTarget, _defaultGravityMagnitude);
                }
                else if (_presetStrategies.TryGetValue(type, out newStrategy))
                {
                    // If using a cached point gravity strategy, ensure its target is still valid.
                    if (((PointGravityStrategy)newStrategy)._centerOfGravity == null)
                    {
                        Debug.LogError("GravityShiftSystem: PointTowardsGravity set without a target and the default target was not initialized or is null.");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("GravityShiftSystem: PointTowardsGravity requires a 'pointTarget' Transform parameter, and no default was set.");
                    return;
                }
                break;
            case GravityPresetType.Custom:
                // This enum value is primarily for event notification. For setting a custom strategy,
                // `SetCustomGravityStrategy` should be used directly with an IGravityStrategy instance.
                Debug.LogWarning("GravityShiftSystem: SetGravityStrategy called with GravityPresetType.Custom. Use SetCustomGravityStrategy(IGravityStrategy instance) for truly custom strategies.");
                return;
            default:
                Debug.LogError($"GravityShiftSystem: Unknown gravity preset type '{type}'.");
                return;
        }

        if (newStrategy == null)
        {
            Debug.LogError($"GravityShiftSystem: Failed to create or retrieve strategy for type '{type}'.");
            return;
        }

        // Only switch and notify if the new strategy is different from the current one.
        if (newStrategy != _currentGravityStrategy)
        {
            _currentGravityStrategy = newStrategy;
            Debug.Log($"Gravity shifted to: {type}");
            // Notify any subscribers that gravity has changed.
            OnGravityStrategyChanged?.Invoke(type);
        }
    }

    /// <summary>
    /// Sets a completely custom gravity strategy directly.
    /// This allows for highly dynamic or unique gravity behaviors not covered by presets.
    /// </summary>
    /// <param name="customStrategy">An instance of a class implementing IGravityStrategy.</param>
    public void SetCustomGravityStrategy(IGravityStrategy customStrategy)
    {
        if (customStrategy == null)
        {
            Debug.LogError("Cannot set a null custom gravity strategy.");
            return;
        }

        // Only switch and notify if the new strategy is different from the current one.
        if (customStrategy != _currentGravityStrategy)
        {
            _currentGravityStrategy = customStrategy;
            Debug.Log($"Gravity shifted to a custom strategy: {customStrategy.GetType().Name}");
            // Notify subscribers, indicating a generic custom gravity type.
            OnGravityStrategyChanged?.Invoke(GravityPresetType.Custom);
        }
    }
}


/*
* --- Example Usage (How to implement in your Unity project) ---
*
* 1.  **Create the GravityShiftSystem GameObject:**
*     - In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
*     - Rename it to "GravityShiftSystem".
*     - Attach the `GravityShiftSystem.cs` script to this GameObject.
*     - In the Inspector for "GravityShiftSystem", configure its 'Default Gravity Settings':
*         - Set `Initial Gravity Type` (e.g., Normal, ZeroG).
*         - Set `Default Gravity Magnitude` (e.g., 9.81 for Earth-like gravity).
*         - If planning to use `CustomDirection` gravity, define a `Default Custom Direction`.
*         - If planning to use `PointTowards` gravity, drag a target Transform (e.g., another GameObject or an empty GameObject used as a gravity well)
*           into the `Default Point Gravity Target` slot. This target will act as the center of gravity.
*
* 2.  **Make GameObjects Affected by Custom Gravity:**
*     - For any GameObject you want to be affected by the `GravityShiftSystem` (e.g., your player, a throwable item):
*         a. Ensure it has a `Rigidbody` component.
*         b. Attach the `GravityAffected.cs` script to the same GameObject.
*         c. **IMPORTANT:** The `Rigidbody` component's `Use Gravity` checkbox should ideally be *unchecked* in the Inspector.
*            The `GravityAffected` script automatically handles this in `Awake` for robustness, but manual unchecking avoids any potential initial frame issues where Unity's gravity might briefly apply.
*
* 3.  **Shift Gravity from Another Script:**
*     - To change the global gravity from any other script (e.g., a `PlayerController`, a `GravityZone` trigger, a UI button handler):
*         a. Get a reference to the `GravityShiftSystem.Instance`.
*         b. Call `SetGravityStrategy` with one of the predefined `GravityPresetType`s, or `SetCustomGravityStrategy` for a unique behavior.
*/

// --- Example Script for demonstrating gravity shifts (can be attached to an empty GameObject or a UI Manager) ---
/*
using UnityEngine;

public class GravityShifterDemo : MonoBehaviour
{
    [Header("Gravity Shift Demo Settings")]
    [Tooltip("Magnitude of gravity for directional strategies in this demo.")]
    [SerializeField] private float _demoGravityMagnitude = 15f;
    [Tooltip("Target for Point Towards gravity strategy in this demo.")]
    [SerializeField] private Transform _demoPointGravityTarget; // Assign a GameObject in inspector, e.g., a central planet

    // Example of a custom, highly dynamic gravity strategy not covered by presets
    private class DynamicOscillatingGravity : IGravityStrategy
    {
        private float _magnitude;
        private float _oscillationSpeed;
        private Vector3 _baseDirection; // The direction around which gravity oscillates

        public DynamicOscillatingGravity(float magnitude, float oscillationSpeed, Vector3 baseDirection)
        {
            _magnitude = magnitude;
            _oscillationSpeed = oscillationSpeed;
            _baseDirection = baseDirection.normalized; // Ensure it's a unit vector
        }

        public void ApplyGravity(Rigidbody rb)
        {
            // Oscillate the direction of gravity over time, e.g., rotate around Z-axis relative to the base direction.
            float angle = Time.time * _oscillationSpeed;
            Quaternion oscillationRotation = Quaternion.Euler(0, 0, Mathf.Sin(angle) * 90f); // Rotate +/- 90 degrees
            Vector3 currentDirection = oscillationRotation * _baseDirection; // Apply rotation to base direction
            
            rb.AddForce(currentDirection.normalized * _magnitude * rb.mass, ForceMode.Force);
        }
    }


    void Start()
    {
        // Optional: Subscribe to gravity change events to react to shifts.
        if (GravityShiftSystem.Instance != null)
        {
            GravityShiftSystem.Instance.OnGravityStrategyChanged += OnGravityChanged;
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks and null reference exceptions.
        if (GravityShiftSystem.Instance != null)
        {
            GravityShiftSystem.Instance.OnGravityStrategyChanged -= OnGravityChanged;
        }
    }

    /// <summary>
    /// Event handler for when the gravity strategy changes.
    /// </summary>
    /// <param name="newType">The new GravityPresetType being applied.</param>
    private void OnGravityChanged(GravityShiftSystem.GravityPresetType newType)
    {
        Debug.Log($"Gravity type changed to: {newType}");
        // You could update UI elements, play sounds, or change visual effects here
        // based on the new gravity type (e.g., particles for zero-G).
    }

    // --- Public methods to be called by UI buttons or other game logic ---

    public void SetNormalGravity()
    {
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.Normal);
    }

    public void SetZeroGravity()
    {
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.ZeroG);
    }

    public void SetUpwardGravity()
    {
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.Upward);
    }

    public void SetLeftwardGravity()
    {
        // For CustomDirection, provide the desired direction.
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.CustomDirection, Vector3.left);
    }

    public void SetRightwardGravity()
    {
        // For CustomDirection, provide the desired direction.
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.CustomDirection, Vector3.right);
    }

    public void SetPointGravity()
    {
        if (_demoPointGravityTarget == null)
        {
            Debug.LogWarning("Point Gravity Target is not assigned in the inspector for the demo script! Please assign a Transform.");
            return;
        }
        // For PointTowards, provide the target Transform.
        GravityShiftSystem.Instance?.SetGravityStrategy(GravityShiftSystem.GravityPresetType.PointTowards, null, _demoPointGravityTarget);
    }

    public void SetOscillatingGravity()
    {
        // Demonstrate setting a completely custom, runtime-created strategy
        // This gravity will oscillate its direction around Vector3.down.
        GravityShiftSystem.Instance?.SetCustomGravityStrategy(new DynamicOscillatingGravity(_demoGravityMagnitude, 2f, Vector3.down));
    }
}
*/
```