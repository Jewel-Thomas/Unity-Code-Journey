// Unity Design Pattern Example: WaterBuoyancySystem
// This script demonstrates the WaterBuoyancySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `WaterBuoyancySystem` design pattern, as interpreted for Unity, focuses on creating a robust, centralized, and extensible system for handling buoyancy physics. It effectively combines several common design principles to manage how objects float, sink, and interact with water.

**Core Principles & Design Patterns Illustrated:**

1.  **System/Manager Pattern:** The `WaterBuoyancySystem` class acts as a central hub, managing all buoyant objects in the scene. It defines global water properties (level, density, drag) and orchestrates the physics calculations for all registered participants. This centralizes physics logic, making it easy to modify global water behavior.
2.  **Component Pattern:** The `BuoyantObject` class is a modular component that can be added to any `GameObject`. It imbues the `GameObject` with buoyancy capabilities without requiring modification of its core class. This promotes reusability and clean architecture.
3.  **Observer/Publisher-Subscriber (Simplified):** `BuoyantObject` components "register" themselves with the `WaterBuoyancySystem` when they become active (`OnEnable`) and "unregister" when they are disabled or destroyed (`OnDisable`). The system then "observes" and updates these registered objects during its `FixedUpdate` cycle.
4.  **Strategy Pattern (Potential Extension):** While not explicitly implemented with an interface in this example for simplicity, the buoyancy calculation logic itself could be abstracted into different "strategies" (e.g., `IBuoyancyCalculator`). This would allow for different types of buoyancy calculations (e.g., simple point-based, complex hydrodynamic, wave-aware) to be swapped out without altering the core `WaterBuoyancySystem` or `BuoyantObject` classes.

**Advantages of this System:**

*   **Centralized Logic:** All water physics rules are defined in one place (`WaterBuoyancySystem`), making maintenance and modification straightforward.
*   **Decoupling:** Buoyant objects don't need to know the specifics of the water (its level, density, etc.). They just provide their own characteristics (buoyancy points, volume per point), and the system handles the rest.
*   **Modularity & Reusability:** Easily add or remove buoyancy capabilities from any object by attaching/detaching the `BuoyantObject` component.
*   **Scalability:** The system can efficiently manage multiple buoyant objects, performing calculations in a single `FixedUpdate` loop.
*   **Performance:** Physics updates are handled in `FixedUpdate`, which is appropriate for `Rigidbody` interactions.

---

### Complete C# Unity Script: `WaterBuoyancySystem.cs` and `BuoyantObject.cs`

To use this, save the following code as two separate C# files in your Unity project (e.g., `WaterBuoyancySystem.cs` and `BuoyantObject.cs`).

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// --- Design Pattern Explanation: WaterBuoyancySystem ---
//
// This example demonstrates a 'WaterBuoyancySystem' which can be seen as a specific application
// of several common design patterns, tailored for game development:
//
// 1.  System/Manager Pattern:
//     - The 'WaterBuoyancySystem' class acts as a central orchestrator. It manages all
//       'BuoyantObject' instances that interact with the water. Instead of each object
//       managing its own water interaction, the system is responsible for calculating
//       and applying forces. This centralizes physics logic and makes it easier to
//       modify water properties (density, level, etc.) globally.
//
// 2.  Component Pattern:
//     - The 'BuoyantObject' class is a component that can be added to any GameObject.
//       It extends the GameObject's capabilities by making it 'buoyant'. This promotes
//       modularity and reusability, allowing developers to easily add buoyancy to
//       various game objects without modifying their core classes.
//
// 3.  Observer/Publisher-Subscriber (Implicit/Simplified):
//     - 'BuoyantObject' instances "register" themselves with the 'WaterBuoyancySystem'
//       when enabled and "unregister" when disabled. In a more complex scenario,
//       this could involve explicit events, but here it's a direct method call,
//       where the system 'observes' the state of its registered objects in its
//       FixedUpdate loop.
//
// 4.  Strategy Pattern (Potential):
//     - While not fully implemented here, the *buoyancy calculation logic* within
//       'WaterBuoyancySystem' could be abstracted into an interface (e.g., IBuoyancyCalculator).
//       Different strategies (e.g., simplified cubic buoyancy, detailed hydrodynamics)
//       could then be swapped in and out. For this practical example, the calculation
//       is directly in the system's FixedUpdate, but the pattern allows for this extension.
//
// Advantages of this pattern:
// - Centralized Logic: All buoyancy calculations are in one place, easy to modify.
// - Decoupling: Buoyant objects don't need to know the water level or density; the system handles it.
// - Modularity: Easily add or remove buoyancy capabilities from objects.
// - Scalability: The system can manage many buoyant objects efficiently.
// - Performance: Physics calculations happen in FixedUpdate, suitable for Rigidbody interactions.
//
// How it works:
// 1.  A 'WaterBuoyancySystem' MonoBehaviour is placed in the scene (e.g., on a water plane object).
//     It defines global water properties like level, density, and drag.
// 2.  Any GameObject that needs to float gets a 'Rigidbody' component and a 'BuoyantObject' component.
// 3.  The 'BuoyantObject' component has configurable 'buoyancy points'. These are child transforms
//     that define where buoyancy forces will be applied. More points typically lead to
//     more realistic rotational behavior.
// 4.  When a 'BuoyantObject' is enabled, it registers itself with the 'WaterBuoyancySystem'.
// 5.  In its `FixedUpdate` loop, the 'WaterBuoyancySystem' iterates through all registered
//     'BuoyantObject's.
// 6.  For each object, it checks each of its buoyancy points. If a point is submerged,
//     it calculates the displaced water volume and applies an upward buoyancy force
//     (Archimedes' principle) and a drag force (proportional to velocity) to the object's Rigidbody.
//
// This setup makes it easy to add new buoyant objects or change global water behavior.
//
// -----------------------------------------------------------------------------------

/// <summary>
/// Manages buoyancy calculations for all registered BuoyantObject components.
/// This acts as the central 'System' in the WaterBuoyancySystem pattern.
/// </summary>
public class WaterBuoyancySystem : MonoBehaviour
{
    // --- Public Water Properties (Configurable in Inspector) ---
    [Header("Water Properties")]
    [Tooltip("The Y-coordinate of the water surface.")]
    [SerializeField] private float waterLevel = 0f;
    [Tooltip("Density of the water in kg/m^3. Pure water is ~1000.")]
    [SerializeField] private float waterDensity = 1000f;
    [Tooltip("Gravitational acceleration (approx. 9.81 m/s^2).")]
    [SerializeField] private float gravity = 9.81f; // Using a direct gravity value for clarity

    [Header("Buoyancy Force Parameters")]
    [Tooltip("Multiplier for upward buoyancy force.")]
    [SerializeField] private float buoyancyForceMultiplier = 1f;

    [Header("Drag Parameters")]
    [Tooltip("Linear drag coefficient applied when submerged.")]
    [SerializeField] private float dragCoefficient = 1f;
    [Tooltip("Angular drag coefficient applied when submerged.")]
    [SerializeField] private float angularDragCoefficient = 0.5f;

    // --- Internal State ---
    // List of all BuoyantObject components currently interacting with this water system.
    private readonly List<BuoyantObject> _buoyantObjects = new List<BuoyantObject>();

    // --- Singleton-like access for BuoyantObjects to find this system ---
    // In a larger project, consider a proper Dependency Injection framework
    // or manually assigning the system reference. For a drop-in example,
    // a static instance is common but can have issues in multi-scene setups.
    public static WaterBuoyancySystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaterBuoyancySystem instances found. Destroying duplicate " + gameObject.name);
            Destroy(this); // Ensure only one instance exists.
            return;
        }
        Instance = this;

        // Ensure Unity's global gravity matches or is accounted for if we're applying our own.
        // For simplicity, we'll let Unity's Rigidbody handle gravity, and we just add buoyancy force.
        // If Rigidbody.useGravity is false, you'd apply gravity here too.
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a BuoyantObject with this system. Called by BuoyantObject.OnEnable().
    /// </summary>
    /// <param name="obj">The BuoyantObject to register.</param>
    public void RegisterBuoyantObject(BuoyantObject obj)
    {
        if (!_buoyantObjects.Contains(obj))
        {
            _buoyantObjects.Add(obj);
            // Debug.Log($"Registered {obj.name} with WaterBuoyancySystem.");
        }
    }

    /// <summary>
    /// Unregisters a BuoyantObject from this system. Called by BuoyantObject.OnDisable().
    /// </summary>
    /// <param name="obj">The BuoyantObject to unregister.</param>
    public void UnregisterBuoyantObject(BuoyantObject obj)
    {
        if (_buoyantObjects.Contains(obj))
        {
            _buoyantObjects.Remove(obj);
            // Debug.Log($"Unregistered {obj.name} from WaterBuoyancySystem.");
        }
    }

    /// <summary>
    /// FixedUpdate is called every fixed framerate frame. Use for physics calculations.
    /// This is where the core buoyancy logic for all registered objects resides.
    /// </summary>
    private void FixedUpdate()
    {
        // Iterate through all registered buoyant objects and apply forces.
        for (int i = 0; i < _buoyantObjects.Count; i++)
        {
            BuoyantObject buoyantObject = _buoyantObjects[i];

            // Ensure the object and its Rigidbody are valid.
            if (buoyantObject == null || buoyantObject.Rigidbody == null)
            {
                // Clean up invalid objects. This can happen if an object is destroyed
                // without properly unregistering.
                _buoyantObjects.RemoveAt(i);
                i--; // Adjust index due to removal
                continue;
            }

            // Get the Rigidbody for applying forces.
            Rigidbody rb = buoyantObject.Rigidbody;

            // Get the positions of the buoyancy points in world space.
            List<Vector3> buoyancyPoints = buoyantObject.GetBuoyancyPointsPositions();
            float totalSubmergedVolume = 0f;
            int submergedPointsCount = 0;
            Vector3 centerOfBuoyancy = Vector3.zero;

            // Apply forces for each buoyancy point.
            foreach (Vector3 point in buoyancyPoints)
            {
                float depth = waterLevel - point.y;

                if (depth > 0) // Point is submerged
                {
                    submergedPointsCount++;

                    // Calculate the submerged volume for this point.
                    // For simplicity, we assume each point represents an equal fraction of the object's volume
                    // when submerged up to a certain depth.
                    // A more accurate model would calculate the exact submerged volume based on mesh/shape.
                    float currentPointSubmergedVolume = buoyantObject.VolumePerBuoyancyPoint; 

                    totalSubmergedVolume += currentPointSubmergedVolume;
                    centerOfBuoyancy += point;

                    // Calculate buoyancy force (Archimedes' principle: F_b = rho * g * V_displaced)
                    Vector3 buoyancyForce = Vector3.up * (waterDensity * gravity * currentPointSubmergedVolume * buoyancyForceMultiplier);
                    rb.AddForceAtPosition(buoyancyForce, point, ForceMode.Force);
                }
            }

            // --- Apply Drag Forces (Linear and Angular) ---
            if (submergedPointsCount > 0)
            {
                // Calculate average center of buoyancy for drag application point
                centerOfBuoyancy /= submergedPointsCount;

                // Linear drag (opposes velocity)
                // We'll apply this proportionally to the submerged volume
                float currentLinearDrag = dragCoefficient * (totalSubmergedVolume / (buoyancyPoints.Count * buoyantObject.VolumePerBuoyancyPoint));
                Vector3 drag = -rb.velocity * currentLinearDrag;
                rb.AddForce(drag, ForceMode.Force);

                // Angular drag (opposes angular velocity)
                // Applied as a torque at the center of buoyancy
                float currentAngularDrag = angularDragCoefficient * (totalSubmergedVolume / (buoyancyPoints.Count * buoyantObject.VolumePerBuoyancyPoint));
                Vector3 angularDrag = -rb.angularVelocity * currentAngularDrag;
                rb.AddTorque(angularDrag, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// Helper to visualize the water level in the Unity editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f); // Blue, semi-transparent
        Vector3 size = new Vector3(1000, 0.01f, 1000); // Large flat plane
        Vector3 center = new Vector3(transform.position.x, waterLevel, transform.position.z);
        Gizmos.DrawCube(center, size);

        // Also draw the water level line for reference
        Gizmos.color = new Color(0, 0.5f, 1f, 0.8f);
        Gizmos.DrawLine(new Vector3(-500, waterLevel, 0), new Vector3(500, waterLevel, 0));
        Gizmos.DrawLine(new Vector3(0, waterLevel, -500), new Vector3(0, waterLevel, 500));
    }
}
```

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For cleaner LINQ operations if needed.

/// <summary>
/// Component for objects that should float. This acts as the 'Buoyant Object' in the pattern.
/// It registers itself with the WaterBuoyancySystem and provides necessary data.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is always present.
public class BuoyantObject : MonoBehaviour
{
    // --- Public Object Properties (Configurable in Inspector) ---
    [Header("Buoyancy Configuration")]
    [Tooltip("Reference to the Rigidbody component.")]
    [SerializeField] private Rigidbody _rigidbody;

    [Tooltip("List of Transforms representing points where buoyancy forces are calculated.")]
    [SerializeField] private List<Transform> buoyancyPoints = new List<Transform>();

    [Tooltip("The approximate volume (in cubic meters) that each buoyancy point represents.")]
    [SerializeField] private float volumePerBuoyancyPoint = 0.1f; // Example: 0.1 m^3 per point

    // --- Accessors for WaterBuoyancySystem ---
    public Rigidbody Rigidbody => _rigidbody;
    public float VolumePerBuoyancyPoint => volumePerBuoyancyPoint;

    private WaterBuoyancySystem _waterSystem; // Reference to the managing system

    private void Awake()
    {
        // Get the Rigidbody component. Required by [RequireComponent(typeof(Rigidbody))].
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"BuoyantObject on {gameObject.name} requires a Rigidbody!", this);
            enabled = false; // Disable this component if Rigidbody is missing.
            return;
        }

        // Basic validation for buoyancy points.
        if (buoyancyPoints == null || buoyancyPoints.Count == 0)
        {
            Debug.LogWarning($"BuoyantObject on {gameObject.name} has no buoyancy points assigned. It might not float correctly.", this);
            // Optionally, try to create default points or disable.
        }
    }

    private void OnEnable()
    {
        // Find and register with the WaterBuoyancySystem.
        // In a real project, it's better to get this reference via dependency injection
        // or a direct assignment to avoid runtime lookups (e.g., a public field in Inspector).
        // For a simple drop-in example, FindObjectOfType is acceptable.
        _waterSystem = WaterBuoyancySystem.Instance; // Using the static instance

        if (_waterSystem != null)
        {
            _waterSystem.RegisterBuoyantObject(this);
        }
        else
        {
            Debug.LogError("No WaterBuoyancySystem found in the scene! BuoyantObject will not function.", this);
            enabled = false; // Disable if no system is found.
        }
    }

    private void OnDisable()
    {
        // Unregister from the WaterBuoyancySystem when disabled or destroyed.
        if (_waterSystem != null)
        {
            _waterSystem.UnregisterBuoyantObject(this);
        }
    }

    /// <summary>
    /// Returns the world positions of all buoyancy points.
    /// Used by the WaterBuoyancySystem to calculate forces.
    /// </summary>
    /// <returns>A list of world position vectors for buoyancy points.</returns>
    public List<Vector3> GetBuoyancyPointsPositions()
    {
        // Using LINQ to easily get the world positions.
        // Can be optimized by pre-allocating a list if performance is critical
        // and calling this frequently for many objects.
        return buoyancyPoints.Where(p => p != null).Select(p => p.position).ToList();
    }

    /// <summary>
    /// Visualizes buoyancy points in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (buoyancyPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in buoyancyPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.05f); // Draw a small sphere at each point
                    Gizmos.DrawLine(transform.position, point.position); // Connect to object origin for clarity
                }
            }
        }
    }
}
```

---

### Example Usage in a Unity Project

Follow these steps to implement the `WaterBuoyancySystem` in your Unity scene:

1.  **Create WaterBuoyancySystem:**
    *   Create an empty GameObject in your scene (e.g., name it "WaterSystem").
    *   Attach the `WaterBuoyancySystem.cs` script to this GameObject.
    *   In the Inspector, configure its properties:
        *   `Water Level`: Set the Y-coordinate where your water surface will be (e.g., `0` for the ground plane).
        *   `Water Density`: Use `1000` for pure water. Higher values make objects float more easily.
        *   `Buoyancy Force Multiplier`: Fine-tune the strength of the upward force.
        *   `Drag Coefficient` and `Angular Drag Coefficient`: Control how quickly objects slow down and stop rotating in the water.
    *   *(Optional)* Add a visual plane or cube for your water at the `Water Level` to represent the water surface.

2.  **Create a Buoyant Object:**
    *   Create a 3D object in your scene (e.g., a Cube, Sphere, or any custom mesh). Position it such that part or all of it is below your `WaterSystem`'s `Water Level`.
    *   Add a `Rigidbody` component to this object:
        *   Ensure `Use Gravity` is checked (this is usually the default).
        *   Adjust `Mass`. For example, a 1x1x1m object that you want to float like wood might have a mass of `500kg`, while one that sinks like stone might be `2500kg`.
        *   Set `Drag` and `Angular Drag` on the Rigidbody to `0` or very low, as the `WaterBuoyancySystem` will apply its own drag when submerged.
    *   Attach the `BuoyantObject.cs` script to the same object.
    *   Configure `BuoyantObject` properties:
        *   `Volume Per Buoyancy Point`: This is crucial for realistic behavior. If your object has a total volume of, say, 1 cubic meter and you use 8 buoyancy points, then each point should represent approximately `0.125` (`1/8`) cubic meters of volume. Adjust this based on your object's actual volume and the number of points.
        *   `Buoyancy Points`: This is the most important part for realistic floating and rotation.
            *   Create several **empty child GameObjects** under your buoyant object (e.g., "BuoyPoint1", "BuoyPoint2", etc.).
            *   Position these child GameObjects at strategic locations around the object where you want buoyancy forces to be calculated. For a simple box, placing them at the 8 corners is a good start. For a boat, you'd place them along the hull and keel. The more points, and the better distributed they are, the more detailed and realistic the buoyancy calculation will be, especially for rotational stability.
            *   Drag these child GameObjects from the Hierarchy into the `Buoyancy Points` list in the `BuoyantObject` component's Inspector.

3.  **Run the Scene:**
    *   Your buoyant object should now float!
    *   If it sinks, its `Rigidbody.mass` is too high relative to the `WaterBuoyancySystem.waterDensity` multiplied by the `BuoyantObject.volumePerBuoyancyPoint` and the number of `Buoyancy Points`.
    *   If it flies too high or bobs violently, reduce the `WaterBuoyancySystem.buoyancyForceMultiplier` or `BuoyantObject.volumePerBuoyancyPoint`.
    *   If it slides endlessly or doesn't slow down, increase the `WaterBuoyancySystem.dragCoefficient` and `WaterBuoyancySystem.angularDragCoefficient`.
    *   Use the `OnDrawGizmos` visualization in the editor (ensure Gizmos are enabled) to see your water plane and buoyancy points.

This setup provides a robust, extensible, and practical system for managing water buoyancy in your Unity projects, clearly demonstrating the principles of the 'WaterBuoyancySystem' pattern.