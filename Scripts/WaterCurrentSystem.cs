// Unity Design Pattern Example: WaterCurrentSystem
// This script demonstrates the WaterCurrentSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Water Current System** design pattern in Unity.

The pattern essentially centralizes the management of water currents and allows any physics-enabled object to query this system to determine the forces acting upon it due to currents.

**Key Components of the Pattern:**

1.  **`WaterCurrentSystem` (The Manager/System):**
    *   A central, often singleton-like, component that maintains a list of all active `WaterCurrentSource` objects in the scene.
    *   Provides a public method (`GetCurrentForceAtPosition`) that other objects can call to get the combined current force at any given world position.
    *   This decouples `WaterCurrentReceiver` objects from directly interacting with individual current sources.

2.  **`WaterCurrentSource` (Current Emitter):**
    *   A component attached to a GameObject that defines an area and properties of a water current (e.g., strength, direction).
    *   It uses a Unity `Collider` (e.g., SphereCollider, BoxCollider) to define its area of effect.
    *   Registers itself with the `WaterCurrentSystem` when enabled and unregisters when disabled.
    *   Provides a method to calculate the force it would exert at a specific position if that position is within its effect area.

3.  **`WaterCurrentReceiver` (Affected Object):**
    *   A component attached to a physics object (one with a `Rigidbody`).
    *   In its `FixedUpdate`, it queries the `WaterCurrentSystem` for the total current force at its current position.
    *   Applies this force to its `Rigidbody`.
    *   Can have properties like `currentEffectiveness` to control how much it's affected.

---

### How to Use This Example in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `WaterCurrentSystem.cs`.
    *   Create another C# script named `WaterCurrentSource.cs`.
    *   Create a third C# script named `WaterCurrentReceiver.cs`.
    *   Copy the respective code blocks below into each script.

2.  **Setup the `WaterCurrentSystem` Manager:**
    *   Create an empty GameObject in your scene (e.g., "WaterCurrentManager").
    *   Add the `WaterCurrentSystem.cs` component to it. This object will manage all currents.

3.  **Create `WaterCurrentSource` Objects:**
    *   Create an empty GameObject (e.g., "RiverCurrent").
    *   Add a `WaterCurrentSource.cs` component to it.
    *   **Crucially, add a Collider component to this GameObject as well.** For example, a `SphereCollider` or `BoxCollider`. This collider defines the area of the current. Make sure it's a **Trigger**.
    *   Adjust the `Strength` and `Direction` properties in the Inspector for `WaterCurrentSource`.
    *   Resize and position the Collider to define your current's area (e.g., a river segment, a whirlpool area).
    *   You can create multiple `WaterCurrentSource` objects to have different currents in different areas.

4.  **Create `WaterCurrentReceiver` Objects:**
    *   Create a 3D object that you want to be affected by currents (e.g., a Cube, a custom boat model).
    *   Add a `Rigidbody` component to it (Physics -> Rigidbody).
    *   Add the `WaterCurrentReceiver.cs` component to it.
    *   Adjust the `Current Effectiveness` property if you want objects to be more or less affected.

5.  **Run the Scene:**
    *   Observe how the `WaterCurrentReceiver` objects are pushed by the `WaterCurrentSource` currents.
    *   You can move the `WaterCurrentSource` GameObjects or the `WaterCurrentReceiver` objects around to see the effects change.

---

### 1. `WaterCurrentSystem.cs` (The Manager)

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The WaterCurrentSystem acts as a central manager for all water currents in the scene.
/// It follows a Singleton-like pattern for easy global access.
///
/// Pattern Role: System/Manager
/// - Manages a collection of WaterCurrentSource objects.
/// - Provides a public API for any object (WaterCurrentReceiver) to query the total current force at a given position.
/// - Decouples current receivers from individual current sources, making the system flexible and scalable.
/// </summary>
public class WaterCurrentSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static WaterCurrentSystem Instance { get; private set; }

    // A list to hold all active WaterCurrentSource components in the scene.
    private List<WaterCurrentSource> activeSources = new List<WaterCurrentSource>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures only one instance of WaterCurrentSystem exists.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaterCurrentSystem instances found. Destroying duplicate.", this);
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Make this object persistent across scene loads if needed.
            // DontDestroyOnLoad(this.gameObject);
        }
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Clears the singleton instance reference.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a WaterCurrentSource with the system.
    /// WaterCurrentSource components call this when they are enabled.
    /// </summary>
    /// <param name="source">The WaterCurrentSource to register.</param>
    public void RegisterSource(WaterCurrentSource source)
    {
        if (!activeSources.Contains(source))
        {
            activeSources.Add(source);
            // Debug.Log($"Registered WaterCurrentSource: {source.name}. Total sources: {activeSources.Count}");
        }
    }

    /// <summary>
    /// Unregisters a WaterCurrentSource from the system.
    /// WaterCurrentSource components call this when they are disabled or destroyed.
    /// </summary>
    /// <param name="source">The WaterCurrentSource to unregister.</param>
    public void UnregisterSource(WaterCurrentSource source)
    {
        if (activeSources.Contains(source))
        {
            activeSources.Remove(source);
            // Debug.Log($"Unregistered WaterCurrentSource: {source.name}. Total sources: {activeSources.Count}");
        }
    }

    /// <summary>
    /// Calculates the combined current force at a given world position.
    /// This is the primary API for WaterCurrentReceiver objects.
    /// </summary>
    /// <param name="position">The world position to check for current forces.</param>
    /// <returns>The total Vector3 force applied by all active currents at the given position.</returns>
    public Vector3 GetCurrentForceAtPosition(Vector3 position)
    {
        Vector3 totalForce = Vector3.zero;

        // Iterate through all registered current sources.
        foreach (WaterCurrentSource source in activeSources)
        {
            // Each source determines if the position is within its influence
            // and calculates its individual force contribution.
            totalForce += source.CalculateForceAtPosition(position);
        }

        return totalForce;
    }
}
```

### 2. `WaterCurrentSource.cs` (Current Emitter)

```csharp
using UnityEngine;

/// <summary>
/// Represents a source of water current within the game world.
/// Attaching this component to a GameObject with a Collider defines an area where currents are active.
///
/// Pattern Role: Current Emitter
/// - Defines the properties of a current (strength, direction).
/// - Uses a Collider to define its area of effect.
/// - Registers itself with the WaterCurrentSystem manager.
/// - Calculates the specific force it exerts at a given position.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a Collider component is present.
public class WaterCurrentSource : MonoBehaviour
{
    [Tooltip("The strength of the current in Newtons.")]
    public float strength = 10f;

    [Tooltip("The normalized direction of the current.")]
    public Vector3 direction = Vector3.forward;

    [Tooltip("How quickly the current force diminishes as objects move away from the center/boundary of the current source. 0 = no falloff, 1 = linear falloff to zero at edge.")]
    [Range(0f, 1f)]
    public float falloffFactor = 0.5f;

    private Collider currentArea; // The collider defining the current's area of effect.
    private SphereCollider sphereCollider;
    private BoxCollider boxCollider;
    private CapsuleCollider capsuleCollider;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the collider and normalizes the direction.
    /// </summary>
    private void Awake()
    {
        currentArea = GetComponent<Collider>();
        if (currentArea == null)
        {
            Debug.LogError("WaterCurrentSource requires a Collider component.", this);
            enabled = false; // Disable if no collider found
            return;
        }

        // Ensure the collider is a trigger, so it doesn't block other physics.
        currentArea.isTrigger = true;

        // Cache specific collider types for efficient checks.
        sphereCollider = currentArea as SphereCollider;
        boxCollider = currentArea as BoxCollider;
        capsuleCollider = currentArea as CapsuleCollider;

        direction.Normalize(); // Ensure direction is always normalized.
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Registers this source with the central WaterCurrentSystem.
    /// </summary>
    private void OnEnable()
    {
        if (WaterCurrentSystem.Instance != null)
        {
            WaterCurrentSystem.Instance.RegisterSource(this);
        }
        else
        {
            Debug.LogWarning("WaterCurrentSystem not found in scene. Currents will not be active for " + name, this);
        }
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Unregisters this source from the central WaterCurrentSystem.
    /// </summary>
    private void OnDisable()
    {
        if (WaterCurrentSystem.Instance != null)
        {
            WaterCurrentSystem.Instance.UnregisterSource(this);
        }
    }

    /// <summary>
    /// Calculates the force this specific current source applies at a given world position.
    /// </summary>
    /// <param name="position">The world position to calculate force for.</param>
    /// <returns>The Vector3 force, or Vector3.zero if the position is outside the current's effect area.</returns>
    public Vector3 CalculateForceAtPosition(Vector3 position)
    {
        // First, quickly check if the position is within the collider's bounds.
        // This is a broad-phase check, more precise checks follow if needed.
        if (!currentArea.bounds.Contains(position))
        {
            return Vector3.zero;
        }

        // For more precise "point in collider" checks, we can use different logic
        // depending on the collider type.
        bool isInInfluence = false;
        float distanceRatio = 0f; // 0 at center/surface, 1 at edge

        if (sphereCollider != null)
        {
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * transform.lossyScale.x;
            float dist = Vector3.Distance(position, center);
            if (dist <= radius)
            {
                isInInfluence = true;
                distanceRatio = dist / radius; // 0 at center, 1 at edge
            }
        }
        else if (boxCollider != null)
        {
            // For box colliders, checking if bounds.Contains is often sufficient for practical purposes.
            // A more precise check would involve transforming the point into local space and checking against min/max.
            // For simplicity, we'll rely on bounds.Contains for now.
            isInInfluence = true; // bounds.Contains already filtered it
            // Could calculate distance ratio based on distance to closest face if desired.
            // For this example, we'll treat it as uniform within the box.
        }
        else if (capsuleCollider != null)
        {
            // For capsule colliders, checking against the sphere bounds is a reasonable approximation for many cases.
            // A more precise point-in-capsule test is complex.
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            float radius = capsuleCollider.radius * transform.lossyScale.x;
            float height = capsuleCollider.height * transform.lossyScale.y;

            // Simplified: Treat as sphere for general check if within general area,
            // or perform a more complex segment-point distance check if strictly needed.
            // For this example, we'll consider it "in influence" if bounds.Contains passed.
            isInInfluence = true;
        }
        // MeshCollider is not easily supported for 'point in collider' without complex checks.
        // It's generally not recommended for defining simple current areas.
        else
        {
            // For other collider types (like MeshCollider), rely purely on bounds.Contains as a coarse check.
            isInInfluence = true;
        }

        if (isInInfluence)
        {
            float effectiveStrength = strength;

            // Apply falloff if configured and the collider type allows for distance ratio calculation.
            if (falloffFactor > 0f)
            {
                // Falloff is max at edge (distanceRatio = 1), min at center (distanceRatio = 0)
                float falloffMultiplier = 1f - (distanceRatio * falloffFactor);
                effectiveStrength *= Mathf.Clamp01(falloffMultiplier);
            }
            return direction * effectiveStrength;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize the current's direction and area.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (currentArea == null)
        {
            currentArea = GetComponent<Collider>();
            if (currentArea == null) return;
        }

        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f); // Blue translucent
        Gizmos.matrix = transform.localToWorldMatrix; // Apply object's transform to gizmo drawing

        if (sphereCollider != null)
        {
            Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (boxCollider != null)
        {
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (capsuleCollider != null)
        {
            Gizmos.DrawWireSphere(capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.radius);
            Gizmos.DrawWireSphere(capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.radius);
            Gizmos.DrawWireCube(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2, capsuleCollider.height - capsuleCollider.radius * 2, capsuleCollider.radius * 2));
        }
        else
        {
            Gizmos.DrawWireCube(currentArea.bounds.center - transform.position, currentArea.bounds.size); // Generic bounds
        }


        Gizmos.matrix = Matrix4x4.identity; // Reset Gizmo matrix

        // Draw current direction arrow
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position + Vector3.up * 0.1f; // Slightly above for visibility
        Vector3 endPos = startPos + direction.normalized * (strength / 20f); // Length relative to strength
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(endPos, 0.1f); // Arrowhead
    }
}
```

### 3. `WaterCurrentReceiver.cs` (Affected Object)

```csharp
using UnityEngine;

/// <summary>
/// This component allows a Rigidbody-enabled object to be affected by water currents.
/// It queries the central WaterCurrentSystem for forces at its position and applies them.
///
/// Pattern Role: Current Receiver
/// - Requires a Rigidbody to apply physics forces.
/// - Queries the WaterCurrentSystem in FixedUpdate (for physics consistency).
/// - Applies the calculated force to its Rigidbody.
/// - Can have adjustable properties like 'currentEffectiveness'.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody component is present.
public class WaterCurrentReceiver : MonoBehaviour
{
    [Tooltip("How much this object is affected by currents. 0 = not affected, 1 = fully affected.")]
    [Range(0f, 1f)]
    public float currentEffectiveness = 1.0f;

    private Rigidbody rb; // Reference to the Rigidbody component.

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets the Rigidbody component reference.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("WaterCurrentReceiver requires a Rigidbody component.", this);
            enabled = false; // Disable if no Rigidbody found
        }
    }

    /// <summary>
    /// FixedUpdate is called every fixed framerate frame, ideal for physics calculations.
    /// Queries the WaterCurrentSystem and applies forces.
    /// </summary>
    private void FixedUpdate()
    {
        // Ensure the WaterCurrentSystem instance exists.
        if (WaterCurrentSystem.Instance == null)
        {
            // Debug.LogWarning("WaterCurrentSystem not found in scene. Cannot apply current forces.", this);
            return;
        }

        // Get the total current force at this object's current position.
        Vector3 force = WaterCurrentSystem.Instance.GetCurrentForceAtPosition(transform.position);

        // If there's any force, apply it to the Rigidbody.
        if (force != Vector3.zero && currentEffectiveness > 0f)
        {
            rb.AddForce(force * currentEffectiveness, ForceMode.Force);
        }
    }
}
```