// Unity Design Pattern Example: WindZoneSystem
// This script demonstrates the WindZoneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the "WindZoneSystem" design pattern in Unity. This pattern provides a centralized system for managing environmental wind (like a `WindZone` component, but with more dynamic control and extensibility) and allows various game objects to register themselves to be affected by this wind in their own unique ways.

**Design Pattern Breakdown:**

1.  **Observer Pattern:**
    *   **Subject:** `WindZoneSystem` acts as the subject. It maintains a list of objects interested in wind data and notifies them when the wind state changes.
    *   **Observer Interface:** `IWindAffected` is the interface that all wind-sensitive objects must implement. It defines the method `ApplyWindForce` that the subject will call.
    *   **Concrete Observers:** Components like `WindAffectedRigidbody` and `WindAffectedDebugText` are concrete observers. They register with the `WindZoneSystem` and implement `ApplyWindForce` to define how they react to wind.

2.  **Singleton Pattern:**
    *   `WindZoneSystem` is implemented as a singleton (`Instance` property). This ensures there's only one global wind system in the scene, providing a single, easily accessible point for other objects to query or register with.

3.  **Strategy Pattern (Implicit):**
    *   Each concrete `IWindAffected` component employs its own strategy for how to interpret and apply the wind force. One might apply physics forces, another might animate foliage, another might affect particle systems, etc. The `WindZoneSystem` doesn't care about the specific strategy, only that it can `ApplyWindForce` on the registered objects.

**Benefits of this Pattern:**

*   **Decoupling:** The `WindZoneSystem` doesn't need to know the specific types of objects it's affecting, only that they implement `IWindAffected`. Similarly, affected objects don't need to know how the wind is generated, only that they receive the `ApplyWindForce` call.
*   **Extensibility:** Adding new types of wind-affected objects is easy. Just create a new component that implements `IWindAffected` and registers itself.
*   **Centralized Control:** All global wind properties are managed in one place, making it easy to adjust, animate, or visualize the wind effects globally.
*   **Performance (Selective Updates):** Only objects that explicitly register themselves are updated, avoiding unnecessary computations on unaffected objects.

---

### **How to Use This Example in Unity:**

1.  **Create a new C# script** named `WindZoneSystemExample.cs` (or any name you prefer, but keep the class names as they are).
2.  **Copy and paste** the entire code below into this new script.
3.  **Import TextMeshPro:** If you haven't already, go to `Window > TextMeshPro > Import TMP Essential Resources` to ensure the `WindAffectedDebugText` script works without errors.
4.  **Scene Setup:**
    *   **Create an Empty GameObject** in your scene (e.g., "WindManager").
    *   **Attach the `WindZoneSystem` component** to this GameObject.
    *   **Adjust WindZoneSystem Parameters** in the Inspector (Wind Direction, Strength, Turbulence, etc.).
    *   **Create a 3D Object** (e.g., a "Cube" or "Sphere").
    *   **Add a `Rigidbody` component** to it.
    *   **Attach the `WindAffectedRigidbody` component** to the same 3D Object.
    *   **Adjust `WindAffectedRigidbody` parameters** (Wind Effect Multiplier, Updraft Multiplier).
    *   **Create a UI Text element (TextMeshPro)**: Go to `GameObject > UI > Text - TextMeshPro`.
    *   **Create an Empty GameObject** (e.g., "WindDebugText").
    *   **Attach the `WindAffectedDebugText` component** to this "WindDebugText" GameObject.
    *   **Drag your TextMeshProUGUI element** from the Hierarchy into the `_debugText` field of the `WindAffectedDebugText` component in the Inspector.
    *   **Run the scene!** You should see the Rigidbody object being pushed by the wind, and the TextMeshPro UI updating with current wind data. Experiment with `WindZoneSystem` parameters during runtime.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<T>
using TMPro; // Required for TextMeshProUGUI, make sure TextMeshPro is imported!

// ----------------------------------------------------------------------------------------------------
// IWindAffected Interface
// Defines the contract for any object that wants to be influenced by the WindZoneSystem.
// ----------------------------------------------------------------------------------------------------
public interface IWindAffected
{
    /// <summary>
    /// Called by the WindZoneSystem to apply wind force/data to this object.
    /// </    summary>
    /// <param name="windDirection">The normalized vector indicating the current wind direction.</param>
    /// <param name="windMagnitude">The scalar value representing the current wind strength.</param>
    /// <param name="turbulence">The current turbulence factor, which can be used to modulate effects.</param>
    void ApplyWindForce(Vector3 windDirection, float windMagnitude, float turbulence);
}

// ----------------------------------------------------------------------------------------------------
// WindZoneSystem - The Central Wind Manager (Singleton Subject)
// This script manages global wind parameters and dispatches them to registered IWindAffected objects.
// ----------------------------------------------------------------------------------------------------
public class WindZoneSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static WindZoneSystem Instance { get; private set; }

    [Header("Wind Parameters")]
    [Tooltip("The base direction of the wind.")]
    [SerializeField] private Vector3 _baseWindDirection = Vector3.right;
    [Tooltip("The base strength of the wind.")]
    [SerializeField] private float _baseWindStrength = 5f;
    [Tooltip("How much turbulence affects the wind direction and magnitude.")]
    [Range(0f, 1f)]
    [SerializeField] private float _turbulenceFactor = 0.5f;
    [Tooltip("How quickly the turbulence changes over time.")]
    [SerializeField] private float _turbulenceFrequency = 1f;
    [Tooltip("The maximum intensity of the turbulence fluctuations.")]
    [SerializeField] private float _turbulenceStrength = 1f;

    // Internal state variables for turbulence calculation
    private float _turbulenceOffsetTimer; // Used for Perlin noise sampling
    private readonly List<IWindAffected> _affectedObjects = new List<IWindAffected>();

    // Public properties to get the current calculated wind data (including turbulence)
    public Vector3 CurrentWindDirection { get; private set; }
    public float CurrentWindMagnitude { get; private set; }
    public float CurrentTurbulenceFactor { get; private set; } // The base turbulence factor applied

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures only one instance of WindZoneSystem exists (Singleton pattern).
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WindZoneSystem: Multiple instances detected! Destroying new instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make this object persist across scene loads if wind is global.
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Updates the wind parameters and dispatches them to all registered objects.
    /// </summary>
    private void Update()
    {
        CalculateCurrentWind();
        DispatchWindData();
    }

    /// <summary>
    /// Calculates the current wind direction and magnitude, incorporating turbulence.
    /// </summary>
    private void CalculateCurrentWind()
    {
        // Advance the turbulence timer for Perlin noise sampling
        _turbulenceOffsetTimer += Time.deltaTime * _turbulenceFrequency;

        // Generate turbulence offsets using Perlin noise for direction and magnitude
        // Perlin noise returns values between 0 and 1. We remap to -1 to 1 for bidirectional turbulence.
        float turbulenceNoiseX = (Mathf.PerlinNoise(_turbulenceOffsetTimer, 0f) * 2 - 1) * _turbulenceStrength;
        float turbulenceNoiseY = (Mathf.PerlinNoise(_turbulenceOffsetTimer + 100f, 0f) * 2 - 1) * _turbulenceStrength;
        float turbulenceNoiseZ = (Mathf.PerlinNoise(_turbulenceOffsetTimer + 200f, 0f) * 2 - 1) * _turbulenceStrength;
        float turbulenceNoiseMagnitude = (Mathf.PerlinNoise(_turbulenceOffsetTimer + 300f, 0f) * 2 - 1) * _turbulenceStrength;

        // Apply turbulence to the base direction
        Vector3 turbulentDirectionOffset = new Vector3(turbulenceNoiseX, turbulenceNoiseY, turbulenceNoiseZ) * _turbulenceFactor;
        CurrentWindDirection = (_baseWindDirection.normalized + turbulentDirectionOffset).normalized;

        // Apply turbulence to the base strength
        CurrentWindMagnitude = Mathf.Max(0, _baseWindStrength + turbulenceNoiseMagnitude * _turbulenceFactor);

        CurrentTurbulenceFactor = _turbulenceFactor;
    }

    /// <summary>
    /// Iterates through all registered IWindAffected objects and calls their ApplyWindForce method.
    /// </summary>
    private void DispatchWindData()
    {
        // Iterate through a copy of the list to prevent issues if an object unregisters itself
        // during the iteration (e.g., if it's destroyed).
        foreach (IWindAffected obj in new List<IWindAffected>(_affectedObjects))
        {
            // IMPORTANT: Check if the object is still valid (not destroyed) before calling its method.
            // This prevents MissingReferenceException if an object was destroyed but not yet unregistered.
            if (obj is MonoBehaviour monoBehaviour && monoBehaviour == null)
            {
                // Object was destroyed, remove it from the list
                Unregister(obj);
                continue;
            }
            obj.ApplyWindForce(CurrentWindDirection, CurrentWindMagnitude, CurrentTurbulenceFactor);
        }
    }

    /// <summary>
    /// Registers an IWindAffected object to receive wind updates.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    public void Register(IWindAffected obj)
    {
        if (obj == null) return;
        if (!_affectedObjects.Contains(obj))
        {
            _affectedObjects.Add(obj);
            // Debug.Log($"WindZoneSystem: Registered {((MonoBehaviour)obj).gameObject.name}");
        }
    }

    /// <summary>
    /// Unregisters an IWindAffected object, stopping it from receiving wind updates.
    /// </summary>
    /// <param name="obj">The object to unregister.</param>
    public void Unregister(IWindAffected obj)
    {
        if (obj == null) return;
        if (_affectedObjects.Contains(obj))
        {
            _affectedObjects.Remove(obj);
            // Debug.Log($"WindZoneSystem: Unregistered {((MonoBehaviour)obj).gameObject.name}");
        }
    }

    /// <summary>
    /// Visualizes the wind direction in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Show base wind direction in editor mode
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _baseWindDirection.normalized * _baseWindStrength);
            Gizmos.DrawSphere(transform.position + _baseWindDirection.normalized * _baseWindStrength, 0.2f);
        }
        else // Show current calculated wind direction in play mode
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, CurrentWindDirection * CurrentWindMagnitude);
            Gizmos.DrawSphere(transform.position + CurrentWindDirection * CurrentWindMagnitude, 0.2f);
        }

        // Optional: Draw a visual representation of the wind zone's general area
        Gizmos.color = new Color(0, 0, 1, 0.05f);
        Gizmos.DrawWireSphere(transform.position, 10f); // Example: A 10-unit radius sphere
    }
}

// ----------------------------------------------------------------------------------------------------
// WindAffectedRigidbody - Concrete Observer for Rigidbody Objects
// This script makes a Rigidbody object respond to wind forces.
// ----------------------------------------------------------------------------------------------------
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present on this GameObject
public class WindAffectedRigidbody : MonoBehaviour, IWindAffected
{
    [Header("Rigidbody Wind Properties")]
    [Tooltip("Multiplier for how strongly the wind affects this Rigidbody.")]
    [SerializeField] private float _windEffectMultiplier = 1f;
    [Tooltip("Multiplier for upward force (buoyancy/updraft) based on wind magnitude.")]
    [SerializeField] private float _updraftMultiplier = 0f;
    [Tooltip("Additional drag applied to the rigidbody based on wind strength.")]
    [SerializeField] private float _windDragMultiplier = 0.01f;

    private Rigidbody _rigidbody;
    private float _initialDrag; // Store initial drag to restore if needed

    /// <summary>
    /// Gets the Rigidbody component and stores its initial drag.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _initialDrag = _rigidbody.drag;
    }

    /// <summary>
    /// Called when the component becomes enabled. Registers with the WindZoneSystem.
    /// </summary>
    private void OnEnable()
    {
        if (WindZoneSystem.Instance != null)
        {
            WindZoneSystem.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning("WindAffectedRigidbody: WindZoneSystem instance not found. Make sure it's in the scene and active.");
        }
    }

    /// <summary>
    /// Called when the component becomes disabled or destroyed. Unregisters from the WindZoneSystem.
    /// </summary>
    private void OnDisable()
    {
        if (WindZoneSystem.Instance != null)
        {
            WindZoneSystem.Instance.Unregister(this);
        }
        // Restore initial drag when disabled
        if (_rigidbody != null)
        {
            _rigidbody.drag = _initialDrag;
        }
    }

    /// <summary>
    /// Implements IWindAffected. Applies calculated wind force to the Rigidbody.
    /// </summary>
    /// <param name="windDirection">The normalized vector indicating the current wind direction.</param>
    /// <param name="windMagnitude">The scalar value representing the current wind strength.</param>
    /// <param name="turbulence">The current turbulence factor, which can be used to modulate effects.</param>
    public void ApplyWindForce(Vector3 windDirection, float windMagnitude, float turbulence)
    {
        // Only apply force if the rigidbody is not kinematic
        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            // Calculate base wind force
            Vector3 force = windDirection * (windMagnitude * _windEffectMultiplier);

            // Add updraft force, potentially influenced by turbulence
            force += Vector3.up * (windMagnitude * _updraftMultiplier * (1f + turbulence * 0.5f));

            _rigidbody.AddForce(force, ForceMode.Force);

            // Dynamically adjust drag based on wind magnitude
            // Lerp towards the new drag value for a smoother transition
            float targetDrag = _initialDrag + (windMagnitude * _windDragMultiplier);
            _rigidbody.drag = Mathf.Lerp(_rigidbody.drag, targetDrag, Time.deltaTime * 5f); // 5f is a smoothing factor
        }
    }
}

// ----------------------------------------------------------------------------------------------------
// WindAffectedDebugText - Concrete Observer for UI Text
// This script updates a TextMeshProUGUI component with current wind information.
// Requires TextMeshPro to be imported into your project.
// ----------------------------------------------------------------------------------------------------
public class WindAffectedDebugText : MonoBehaviour, IWindAffected
{
    [Tooltip("Reference to the TextMeshProUGUI component to display wind data.")]
    [SerializeField] private TextMeshProUGUI _debugText;

    /// <summary>
    /// Ensures the TextMeshProUGUI reference is set.
    /// </summary>
    private void Awake()
    {
        if (_debugText == null)
        {
            _debugText = GetComponent<TextMeshProUGUI>();
            if (_debugText == null)
            {
                Debug.LogError("WindAffectedDebugText: A TextMeshProUGUI component must be assigned or be on this GameObject.");
                enabled = false; // Disable this component if no text element is found
                return;
            }
        }
        _debugText.text = "Wind System Initializing...";
    }

    /// <summary>
    /// Called when the component becomes enabled. Registers with the WindZoneSystem.
    /// </summary>
    private void OnEnable()
    {
        if (WindZoneSystem.Instance != null)
        {
            WindZoneSystem.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning("WindAffectedDebugText: WindZoneSystem instance not found. Make sure it's in the scene and active.");
        }
    }

    /// <summary>
    /// Called when the component becomes disabled or destroyed. Unregisters from the WindZoneSystem.
    /// </summary>
    private void OnDisable()
    {
        if (WindZoneSystem.Instance != null)
        {
            WindZoneSystem.Instance.Unregister(this);
        }
        if (_debugText != null)
        {
            _debugText.text = "Wind System Disabled.";
        }
    }

    /// <summary>
    /// Implements IWindAffected. Updates the TextMeshProUGUI with current wind data.
    /// </summary>
    /// <param name="windDirection">The normalized vector indicating the current wind direction.</param>
    /// <param name="windMagnitude">The scalar value representing the current wind strength.</param>
    /// <param name="turbulence">The current turbulence factor.</param>
    public void ApplyWindForce(Vector3 windDirection, float windMagnitude, float turbulence)
    {
        if (_debugText != null)
        {
            _debugText.text = $"<color=#00BFFF><b>Wind Data:</b></color>\n" +
                              $"Direction: {windDirection.normalized:F2}\n" +
                              $"Magnitude: {windMagnitude:F2}\n" +
                              $"Turbulence: {turbulence:F2}";
        }
    }
}
```