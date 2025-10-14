// Unity Design Pattern Example: PhysicsConstraintSystem
// This script demonstrates the PhysicsConstraintSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PhysicsConstraintSystem' design pattern provides a structured way to manage and apply custom physics constraints in your game. Instead of relying solely on Unity's built-in `Joint` components, this pattern allows you to define your own rules for how `Rigidbody` objects interact and behave, enforcing these rules iteratively each physics frame.

This pattern is highly useful for:
*   Creating unique physical behaviors not covered by standard joints.
*   Implementing soft-body physics or cloth-like simulations where points need to maintain relative distances.
*   Building custom machinery or character components with specific movement limitations.
*   Centralizing constraint management for better organization and performance.

---

## PhysicsConstraintSystem Design Pattern in Unity

Here's a complete C# Unity script demonstrating the PhysicsConstraintSystem pattern. This script includes two common custom constraints:
1.  **Distance Constraint:** Keeps two `Rigidbody` objects at a specified distance from each other.
2.  **Positional Lock Constraint:** Locks a `Rigidbody` object to a specific world position (or an offset relative to the `PhysicsConstraintSystem`'s GameObject).

### `PhysicsConstraintSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for [Serializable] and Func<T>

/// <summary>
/// The core interface for any physics constraint in our system.
/// This defines the contract that all specific constraint types must fulfill.
/// </summary>
public interface IPhysicsConstraint
{
    /// <summary>
    /// Initializes the constraint. Called once when the system starts or a constraint is added.
    /// Provides the owner GameObject for context if needed (e.g., for relative positioning).
    /// </summary>
    /// <param name="owner">The GameObject hosting the PhysicsConstraintSystem.</param>
    void Initialize(GameObject owner);

    /// <summary>
    /// Applies the constraint logic. This method is called every FixedUpdate
    /// to enforce the constraint on the associated Rigidbodies.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last FixedUpdate call (Time.fixedDeltaTime).</param>
    void ApplyConstraint(float deltaTime);

    /// <summary>
    /// Draws visual representations of the constraint in the editor, for debugging and visualization.
    /// </summary>
    void OnDrawGizmos();
}

/// <summary>
/// An abstract base class for serializable constraint data.
/// This allows us to define constraint properties in the Unity Inspector.
/// Each concrete constraint type will have its own data class inheriting from this.
/// </summary>
[Serializable]
public abstract class ConstraintData
{
    /// <summary>
    /// Factory method to create an actual runtime IPhysicsConstraint object
    /// from the Inspector-configured data.
    /// </summary>
    /// <returns>A concrete implementation of IPhysicsConstraint.</returns>
    public abstract IPhysicsConstraint CreateConstraint();
}

/// <summary>
/// Serializable data class for a Distance Constraint, configurable in the Inspector.
/// This class holds references to the Rigidbodies and the target distance.
/// </summary>
[Serializable]
public class DistanceConstraintData : ConstraintData
{
    [Tooltip("The first Rigidbody involved in the distance constraint.")]
    public Rigidbody rbA;
    [Tooltip("The second Rigidbody involved in the distance constraint.")]
    public Rigidbody rbB;
    [Tooltip("The desired distance between the centers of the two Rigidbodies.")]
    public float targetDistance = 1f;
    [Range(0.01f, 1f)]
    [Tooltip("How strongly the constraint tries to maintain the distance (0 = loose, 1 = rigid).")]
    public float stiffness = 0.5f;

    /// <summary>
    /// Creates a runtime DistanceConstraint object using the data configured in the Inspector.
    /// </summary>
    public override IPhysicsConstraint CreateConstraint()
    {
        return new DistanceConstraint(rbA, rbB, targetDistance, stiffness);
    }
}

/// <summary>
/// Serializable data class for a Positional Lock Constraint, configurable in the Inspector.
/// This class holds a reference to the Rigidbody and the target position offset.
/// </summary>
[Serializable]
public class PositionalLockConstraintData : ConstraintData
{
    [Tooltip("The Rigidbody to be locked to a specific position.")]
    public Rigidbody rb;
    [Tooltip("The offset from the PhysicsConstraintSystem's GameObject position that the Rigidbody should be locked to.")]
    public Vector3 targetPositionOffset;
    [Range(0.01f, 1f)]
    [Tooltip("How strongly the constraint tries to maintain the position (0 = loose, 1 = rigid).")]
    public float stiffness = 0.5f;

    // A reference to the owner's transform (PhysicsConstraintSystem) to calculate world target position.
    private Transform ownerTransform;

    /// <summary>
    /// Sets the transform of the GameObject that owns the PhysicsConstraintSystem.
    /// This is used to calculate the world target position from the local offset.
    /// </summary>
    /// <param name="owner">The transform of the PhysicsConstraintSystem's GameObject.</param>
    public void SetOwnerTransform(Transform owner)
    {
        ownerTransform = owner;
    }

    /// <summary>
    /// Calculates the world target position based on the owner's position and the local offset.
    /// </summary>
    /// <returns>The world coordinates where the Rigidbody should be locked.</returns>
    public Vector3 GetWorldTargetPosition()
    {
        if (ownerTransform == null)
        {
            Debug.LogError("Owner Transform not set for PositionalLockConstraintData. Make sure InitializeConstraints is called.", rb);
            return Vector3.zero;
        }
        return ownerTransform.position + targetPositionOffset;
    }

    /// <summary>
    /// Creates a runtime PositionalLockConstraint object using the data configured in the Inspector.
    /// It passes a Func<Vector3> to allow the constraint to dynamically query the target position.
    /// </summary>
    public override IPhysicsConstraint CreateConstraint()
    {
        return new PositionalLockConstraint(rb, GetWorldTargetPosition, stiffness);
    }
}

/// <summary>
/// Concrete implementation of the IPhysicsConstraint for a distance constraint.
/// This class contains the actual physics logic to keep two Rigidbodies at a specific distance.
/// </summary>
public class DistanceConstraint : IPhysicsConstraint
{
    private Rigidbody rbA;
    private Rigidbody rbB;
    private float targetDistance;
    private float stiffness; // Controls how aggressively the constraint is enforced

    /// <summary>
    /// Constructor for the DistanceConstraint.
    /// </summary>
    /// <param name="rbA">The first Rigidbody.</param>
    /// <param name="rbB">The second Rigidbody.</param>
    /// <param name="targetDistance">The desired distance between them.</param>
    /// <param name="stiffness">The strength of the correction (0-1).</param>
    public DistanceConstraint(Rigidbody rbA, Rigidbody rbB, float targetDistance, float stiffness)
    {
        this.rbA = rbA;
        this.rbB = rbB;
        this.targetDistance = targetDistance;
        this.stiffness = stiffness;
    }

    public void Initialize(GameObject owner)
    {
        // No specific initialization needed for this constraint type,
        // but could be used for caching components, validating setup, etc.
    }

    /// <summary>
    /// Applies the distance constraint by adjusting positions and velocities of the Rigidbodies.
    /// This uses a simple iterative correction method, common in physics engines.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last FixedUpdate.</param>
    public void ApplyConstraint(float deltaTime)
    {
        if (rbA == null || rbB == null) return;

        Vector3 currentDiff = rbB.position - rbA.position;
        float currentDistance = currentDiff.magnitude;

        // Avoid division by zero if objects are perfectly overlapping
        if (currentDistance == 0) return;

        float error = currentDistance - targetDistance;

        // If the error is very small, no significant correction is needed
        if (Mathf.Abs(error) < 0.001f) return;

        // Calculate a correction vector based on the error and stiffness
        // Each rigidbody is moved half the correction amount
        Vector3 correctionDirection = currentDiff.normalized;
        Vector3 positionCorrection = correctionDirection * (error * stiffness);

        // Apply position correction directly. This makes the constraint 'rigid'.
        // For 'softer' constraints, you might apply forces instead.
        rbA.position += positionCorrection * 0.5f;
        rbB.position -= positionCorrection * 0.5f;

        // Also adjust velocities to dampen oscillations and prevent overshooting.
        // This ensures the objects don't bounce back and forth around the target distance.
        Vector3 relativeVelocity = rbB.velocity - rbA.velocity;
        float closingSpeed = Vector3.Dot(relativeVelocity, correctionDirection);
        // Desired closing speed: try to close the error over the duration of a frame
        float desiredClosingSpeed = -error / deltaTime * stiffness;

        float velocityCorrectionMagnitude = desiredClosingSpeed - closingSpeed;
        Vector3 velocityCorrection = correctionDirection * velocityCorrectionMagnitude * 0.5f;

        rbA.velocity -= velocityCorrection;
        rbB.velocity += velocityCorrection;
    }

    /// <summary>
    /// Draws a cyan line between the two Rigidbodies to visualize the distance constraint.
    /// A yellow ray shows the target distance.
    /// </summary>
    public void OnDrawGizmos()
    {
        if (rbA != null && rbB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rbA.position, rbB.position);
            Gizmos.DrawSphere(rbA.position, 0.05f);
            Gizmos.DrawSphere(rbB.position, 0.05f);

            // Draw a visual representation of the target distance
            Gizmos.color = Color.yellow;
            Vector3 midPoint = (rbA.position + rbB.position) / 2f;
            Vector3 dir = (rbB.position - rbA.position).normalized;
            Gizmos.DrawRay(midPoint - dir * targetDistance / 2f, dir * targetDistance);
        }
    }
}

/// <summary>
/// Concrete implementation of the IPhysicsConstraint for a positional lock constraint.
/// This class contains the actual physics logic to lock a Rigidbody to a specific position.
/// </summary>
public class PositionalLockConstraint : IPhysicsConstraint
{
    private Rigidbody rb;
    // Using a Func<Vector3> allows the target position to be dynamically determined
    // (e.g., relative to another object, or based on the system's owner position).
    private Func<Vector3> getTargetPosition;
    private float stiffness; // Controls how aggressively the constraint is enforced

    /// <summary>
    /// Constructor for the PositionalLockConstraint.
    /// </summary>
    /// <param name="rb">The Rigidbody to be constrained.</param>
    /// <param name="getTargetPosition">A function that returns the current world target position.</param>
    /// <param name="stiffness">The strength of the correction (0-1).</param>
    public PositionalLockConstraint(Rigidbody rb, Func<Vector3> getTargetPosition, float stiffness)
    {
        this.rb = rb;
        this.getTargetPosition = getTargetPosition;
        this.stiffness = stiffness;
    }

    public void Initialize(GameObject owner)
    {
        // No specific initialization needed here. Target position is handled by the Func.
    }

    /// <summary>
    /// Applies the positional lock constraint by adjusting the Rigidbody's position and velocity.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last FixedUpdate.</param>
    public void ApplyConstraint(float deltaTime)
    {
        if (rb == null) return;

        Vector3 targetPos = getTargetPosition();
        Vector3 currentPos = rb.position;
        Vector3 error = targetPos - currentPos;

        // If the error is very small, no significant correction is needed
        if (error.magnitude < 0.001f) return;

        // Apply position correction directly based on the error and stiffness.
        rb.position += error * stiffness;

        // Also adjust velocity to dampen oscillations and ensure it settles at the target.
        // This acts like a spring and damper, moving it towards the target and slowing it down.
        rb.velocity += error / deltaTime * stiffness;
    }

    /// <summary>
    /// Draws a magenta wire sphere at the target position and a line to the Rigidbody
    /// to visualize the positional lock constraint.
    /// </summary>
    public void OnDrawGizmos()
    {
        if (rb != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 targetPos = getTargetPosition();
            Gizmos.DrawWireSphere(targetPos, 0.1f); // Show target position
            Gizmos.DrawLine(rb.position, targetPos); // Line from current to target
        }
    }
}


/// <summary>
/// The main MonoBehaviour component that acts as the 'PhysicsConstraintSystem'.
/// It manages a collection of IPhysicsConstraint objects, initializes them,
/// and applies them during FixedUpdate.
/// </summary>
[DisallowMultipleComponent] // Only one constraint system per GameObject is usually desired
public class PhysicsConstraintSystem : MonoBehaviour
{
    [Header("Constraint Configurations")]
    [Tooltip("List of Distance Constraints to apply between pairs of Rigidbodies.")]
    public List<DistanceConstraintData> distanceConstraints = new List<DistanceConstraintData>();

    [Tooltip("List of Positional Lock Constraints to apply to individual Rigidbodies.")]
    public List<PositionalLockConstraintData> positionalLockConstraints = new List<PositionalLockConstraintData>();

    // The runtime list of active constraint objects.
    // These are instantiated from the serializable ConstraintData classes.
    private List<IPhysicsConstraint> activeConstraints = new List<IPhysicsConstraint>();

    void Awake()
    {
        InitializeConstraints();
    }

    /// <summary>
    /// Initializes all constraints from the Inspector-configured data.
    /// This clears any existing runtime constraints and creates new ones.
    /// </summary>
    private void InitializeConstraints()
    {
        activeConstraints.Clear(); // Clear previous constraints to avoid duplicates or stale data

        // Process Distance Constraints
        foreach (var data in distanceConstraints)
        {
            if (data.rbA == null || data.rbB == null)
            {
                Debug.LogWarning($"Distance Constraint requires two Rigidbodies. Skipping one in {name}.", this);
                continue;
            }
            IPhysicsConstraint constraint = data.CreateConstraint();
            constraint.Initialize(gameObject); // Pass this GameObject as owner
            activeConstraints.Add(constraint);
        }

        // Process Positional Lock Constraints
        foreach (var data in positionalLockConstraints)
        {
            if (data.rb == null)
            {
                Debug.LogWarning($"Positional Lock Constraint requires a Rigidbody. Skipping one in {name}.", this);
                continue;
            }
            // Pass the owner's transform to the data class for dynamic world position calculation
            data.SetOwnerTransform(transform);
            IPhysicsConstraint constraint = data.CreateConstraint();
            constraint.Initialize(gameObject); // Pass this GameObject as owner
            activeConstraints.Add(constraint);
        }

        Debug.Log($"Initialized {activeConstraints.Count} physics constraints in {name}.", this);
    }

    /// <summary>
    /// FixedUpdate is called at a fixed framerate and is the recommended place
    /// to apply physics-related logic, ensuring consistent behavior.
    /// </summary>
    void FixedUpdate()
    {
        // Iterate through all active constraints and apply their logic.
        foreach (var constraint in activeConstraints)
        {
            constraint.ApplyConstraint(Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// OnDrawGizmos is called in the editor for visualization.
    /// It allows us to see the constraints and their target states directly in the scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw gizmos if activeConstraints has been initialized (i.e., not during editor-only operations
        // before Awake has run, or if the component is disabled).
        if (activeConstraints == null || activeConstraints.Count == 0)
        {
            // Attempt to initialize for editor-time gizmos if possible, but safely.
            // This allows gizmos to show even when not playing, but without full runtime setup.
            // For a robust solution in editor, a custom editor might be better.
            // For this basic example, we'll rely on the runtime activeConstraints list.
            if (!Application.isPlaying)
            {
                // Can iterate through `distanceConstraints` and `positionalLockConstraints`
                // directly for simple Gizmo drawing in edit mode without instantiating `IPhysicsConstraint`.
                // For simplicity, we'll stick to runtime-initialized `activeConstraints` for gizmos.
            }
            return;
        }

        // Iterate through all active constraints and call their gizmo drawing method.
        foreach (var constraint in activeConstraints)
        {
            constraint.OnDrawGizmos();
        }
    }

    // --- Public methods for runtime management (optional but good for a flexible system) ---

    /// <summary>
    /// Adds a new constraint at runtime.
    /// </summary>
    /// <param name="constraintData">The serializable data for the constraint to add.</param>
    public void AddConstraint(ConstraintData constraintData)
    {
        if (constraintData == null)
        {
            Debug.LogWarning("Attempted to add a null constraint data.", this);
            return;
        }

        // Specific handling for PositionalLockConstraintData to set the owner transform
        if (constraintData is PositionalLockConstraintData positionalData)
        {
            positionalData.SetOwnerTransform(transform);
        }

        IPhysicsConstraint newConstraint = constraintData.CreateConstraint();
        newConstraint.Initialize(gameObject); // Initialize the new constraint
        activeConstraints.Add(newConstraint);
        Debug.Log($"Added new physics constraint of type {constraintData.GetType().Name} at runtime.", this);
    }

    /// <summary>
    /// Removes a specific constraint instance at runtime.
    /// Note: This removes the runtime object, not the Inspector data list entries.
    /// </summary>
    /// <param name="constraintToRemove">The IPhysicsConstraint object to remove.</param>
    /// <returns>True if the constraint was found and removed, false otherwise.</returns>
    public bool RemoveConstraint(IPhysicsConstraint constraintToRemove)
    {
        if (activeConstraints.Remove(constraintToRemove))
        {
            Debug.Log("Removed physics constraint at runtime.", this);
            return true;
        }
        Debug.LogWarning("Attempted to remove a constraint that was not found in the active list.", this);
        return false;
    }

    /// <summary>
    /// Clears all active constraints at runtime.
    /// Note: This does not affect the Inspector-configured lists.
    /// </summary>
    public void ClearRuntimeConstraints()
    {
        activeConstraints.Clear();
        Debug.Log("Cleared all physics constraints at runtime.", this);
    }

    /// <summary>
    /// Reinitializes all constraints based on the current Inspector settings.
    /// Useful if you modify the Inspector lists during play mode and want them to take effect.
    /// </summary>
    [ContextMenu("Reinitialize Constraints from Inspector")]
    public void ReinitializeFromInspector()
    {
        InitializeConstraints();
    }
}

```

---

### How the PhysicsConstraintSystem Pattern Works

1.  **`IPhysicsConstraint` (Interface):**
    *   This is the core contract. Any object that wants to be a "physics constraint" must implement this interface.
    *   It defines methods like `Initialize`, `ApplyConstraint`, and `OnDrawGizmos`, ensuring all constraints have a consistent way to be set up, executed, and visualized.

2.  **`ConstraintData` (Abstract Base Class for Inspector Data):**
    *   This abstract class, marked with `[System.Serializable]`, allows us to create different constraint configurations directly in the Unity Inspector.
    *   It includes an abstract `CreateConstraint()` method, acting as a **Factory Method**. Each specific `ConstraintData` implementation (e.g., `DistanceConstraintData`) will override this to instantiate its corresponding runtime `IPhysicsConstraint` object.

3.  **Concrete `ConstraintData` Implementations (e.g., `DistanceConstraintData`, `PositionalLockConstraintData`):**
    *   These are also `[System.Serializable]` classes. They hold the specific parameters required for a particular type of constraint (e.g., `Rigidbody` references, target distance, stiffness).
    *   They serve as the "blueprint" configured in the Inspector. When the game starts, the `PhysicsConstraintSystem` reads this blueprint and creates the actual runtime constraint objects.

4.  **Concrete `IPhysicsConstraint` Implementations (e.g., `DistanceConstraint`, `PositionalLockConstraint`):**
    *   These classes contain the actual physics logic. For example, `DistanceConstraint` calculates the current distance between two `Rigidbody`s, compares it to the `targetDistance`, and then applies position/velocity corrections to bring them back into line.
    *   The `ApplyConstraint` method typically runs in `FixedUpdate` to ensure physics calculations are consistent.
    *   The `OnDrawGizmos` method provides visual feedback in the Unity Editor, showing where the constraints are and what their target states are.

5.  **`PhysicsConstraintSystem` (MonoBehaviour):**
    *   This is the central manager component that you attach to a GameObject in your scene.
    *   It holds lists of `ConstraintData` objects (e.g., `distanceConstraints`, `positionalLockConstraints`), allowing you to configure multiple constraints of different types in the Inspector.
    *   In `Awake()`, it iterates through all `ConstraintData` entries, calling `CreateConstraint()` on each to build a runtime list (`activeConstraints`) of `IPhysicsConstraint` objects.
    *   In `FixedUpdate()`, it iterates through `activeConstraints` and calls `ApplyConstraint()` on each, enforcing all physics rules.
    *   In `OnDrawGizmos()`, it iterates and calls `OnDrawGizmos()` on each for visualization.
    *   It provides public methods (`AddConstraint`, `RemoveConstraint`, `ClearRuntimeConstraints`, `ReinitializeFromInspector`) for dynamic management of constraints at runtime.

### Example Usage:

To use this `PhysicsConstraintSystem` in your Unity project:

1.  **Create the Script:**
    *   Create a new C# script named "PhysicsConstraintSystem" in your Unity project.
    *   Copy and paste the entire code provided above into this script.

2.  **Setup the Manager:**
    *   Create an empty GameObject in your scene (e.g., name it "ConstraintManager").
    *   Add the `PhysicsConstraintSystem` component to this "ConstraintManager" GameObject.

**Scenario 1: Distance Constraint (Keeping two objects a fixed distance apart)**

    a.  Create two 3D Sphere GameObjects (e.g., "SphereA", "SphereB").
    b.  Add a `Rigidbody` component to both "SphereA" and "SphereB". (Make sure "Use Gravity" is enabled if you want to see them fall).
    c.  On your "ConstraintManager" GameObject, in the `PhysicsConstraintSystem` component:
        i.  Expand the "Distance Constraints" list.
        ii. Click the `+` button to add a new constraint.
        iii. Drag "SphereA" into the 'Rb A' slot.
        iv. Drag "SphereB" into the 'Rb B' slot.
        v.  Set 'Target Distance' to, for example, `2.0`.
        vi. Adjust 'Stiffness' (e.g., `0.8`) to control how strongly they maintain the distance.
    d.  Run the scene. The spheres will now try to stay `2.0` units apart. You can move or apply forces to one, and the other will react to maintain the distance. A **cyan line gizmo** will connect them in the editor.

**Scenario 2: Positional Lock Constraint (Locking an object to a specific position)**

    a.  Create a 3D Cube GameObject (e.g., "CubeA").
    b.  Add a `Rigidbody` component to "CubeA".
    c.  On your "ConstraintManager" GameObject, in the `PhysicsConstraintSystem` component:
        i.  Expand the "Positional Lock Constraints" list.
        ii. Click the `+` button to add a new constraint.
        iii. Drag "CubeA" into the 'Rb' slot.
        iv. Set 'Target Position Offset' to, for example, `(0, 2, 0)`. This will lock the cube `2` units above the "ConstraintManager" GameObject's position. If "ConstraintManager" is at `(0,0,0)`, the cube will lock at `(0,2,0)`.
        v.  Adjust 'Stiffness' (e.g., `0.9`) to control how strongly it stays at the target.
    d.  Run the scene. "CubeA" will snap to and stay at its target position. It will resist attempts to move it. A **magenta wire sphere gizmo** will mark the target position, and a line will connect it to the cube.

**Runtime Management Example (Optional Script):**

You can also add or remove constraints dynamically during gameplay. Create a new C# script (e.g., `RuntimeConstraintAdder`) and attach it to an empty GameObject, then configure its public fields in the Inspector.

```csharp
using UnityEngine;

public class RuntimeConstraintAdder : MonoBehaviour
{
    [Tooltip("Reference to the PhysicsConstraintSystem in the scene.")]
    public PhysicsConstraintSystem constraintSystem;

    [Header("Rigidbodies for Runtime Constraints")]
    [Tooltip("Rigidbody for a new distance constraint.")]
    public Rigidbody dynamicRbA;
    [Tooltip("Another Rigidbody for a new distance constraint.")]
    public Rigidbody dynamicRbB;
    [Tooltip("Rigidbody for a new positional lock constraint.")]
    public Rigidbody dynamicLockRb;

    void Start()
    {
        // Automatically find the PhysicsConstraintSystem if not assigned
        if (constraintSystem == null)
        {
            constraintSystem = FindObjectOfType<PhysicsConstraintSystem>();
            if (constraintSystem == null)
            {
                Debug.LogError("PhysicsConstraintSystem not found in scene! Please assign it or ensure it exists.", this);
                enabled = false; // Disable this script if manager is missing
                return;
            }
        }

        // Example: Add a new distance constraint 3 seconds after start
        Invoke(nameof(AddRuntimeDistanceConstraint), 3f);
        // Example: Add a new positional constraint 6 seconds after start
        Invoke(nameof(AddRuntimePositionalConstraint), 6f);
    }

    /// <summary>
    /// Adds a new distance constraint at runtime.
    /// </summary>
    void AddRuntimeDistanceConstraint()
    {
        if (dynamicRbA != null && dynamicRbB != null)
        {
            DistanceConstraintData newDistanceData = new DistanceConstraintData
            {
                rbA = dynamicRbA,
                rbB = dynamicRbB,
                targetDistance = 3.5f, // A new target distance
                stiffness = 0.7f
            };
            constraintSystem.AddConstraint(newDistanceData);
            Debug.Log("Added a new distance constraint at runtime.", this);
        }
        else
        {
            Debug.LogWarning("Cannot add runtime distance constraint: Rigidbodies not assigned.", this);
        }
    }

    /// <summary>
    /// Adds a new positional lock constraint at runtime.
    /// </summary>
    void AddRuntimePositionalConstraint()
    {
        if (dynamicLockRb != null)
        {
            PositionalLockConstraintData newPositionalData = new PositionalLockConstraintData
            {
                rb = dynamicLockRb,
                // Lock 3 units right, 1 unit up from the PhysicsConstraintSystem's GameObject
                targetPositionOffset = new Vector3(3, 1, 0),
                stiffness = 0.95f
            };
            constraintSystem.AddConstraint(newPositionalData);
            Debug.Log("Added a new positional lock constraint at runtime.", this);
        }
        else
        {
            Debug.LogWarning("Cannot add runtime positional constraint: Rigidbody not assigned.", this);
        }
    }

    // You could also add methods to remove constraints dynamically:
    // public void RemoveLastDistanceConstraint() { /* ... */ }
}
```

This complete example provides a robust foundation for building custom physics behaviors in your Unity projects using the PhysicsConstraintSystem design pattern.