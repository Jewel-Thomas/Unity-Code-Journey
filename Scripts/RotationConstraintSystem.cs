// Unity Design Pattern Example: RotationConstraintSystem
// This script demonstrates the RotationConstraintSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RotationConstraintSystem' design pattern in Unity provides a modular and extensible way to manage multiple influences on a GameObject's rotation. Instead of having a single monolithic script that tries to handle all rotation logic (e.g., looking at a target, clamping angles, maintaining a relative orientation), this pattern separates these concerns into individual 'constraints'.

**Core Idea:**

1.  **`IRotationConstraint`**: An interface that defines how a single rotation influence should propose its desired rotation.
2.  **`RotationConstraintBase`**: An abstract `ScriptableObject` base class implementing `IRotationConstraint`. Using `ScriptableObject` allows constraints to be created as reusable assets and easily assigned in the Inspector.
3.  **`RotationConstraintSystem`**: A `MonoBehaviour` that acts as the central manager. It collects all `IRotationConstraint` instances, evaluates their proposed rotations, and combines them (typically with weighted `Quaternion.Slerp`) before applying the final rotation to a target `Transform`.

**Benefits of this Pattern:**

*   **Modularity**: Each constraint is a self-contained unit responsible for one specific rotation influence.
*   **Extensibility**: Easily add new types of rotation constraints without modifying existing code. Just create a new `ScriptableObject` class inheriting from `RotationConstraintBase`.
*   **Flexibility**: Enable/disable constraints, adjust their weights, and reorder them easily in the Inspector.
*   **Separation of Concerns**: The `RotationConstraintSystem` focuses solely on combining rotations, while individual constraints focus on their specific logic.
*   **Reusability**: `ScriptableObject` constraints can be shared across multiple `RotationConstraintSystem` instances or even different projects.

---

## Complete Unity C# Example: RotationConstraintSystem

This example includes:
1.  **`IRotationConstraint.cs`**: The interface for all rotation constraints.
2.  **`RotationConstraintBase.cs`**: The abstract base `ScriptableObject` for concrete constraints.
3.  **`RotationConstraintSystem.cs`**: The main `MonoBehaviour` that orchestrates the constraints.
4.  **`LookAtConstraintSO.cs`**: An example constraint to make the target look at another object.
5.  **`ClampRotationXConstraintSO.cs`**: An example constraint to limit rotation around the X-axis.
6.  **`KeepRelativeRotationSO.cs`**: An example constraint to maintain a rotation relative to a parent.

---

### 1. `IRotationConstraint.cs`

This interface defines the contract for any object that wants to act as a rotation constraint.

```csharp
// IRotationConstraint.cs
using UnityEngine;

/// <summary>
/// Interface for a rotation constraint.
/// Each constraint proposes a target rotation and has a weight influencing its contribution.
/// </summary>
public interface IRotationConstraint
{
    /// <summary>
    /// The weight of this constraint. A higher weight means more influence on the final rotation.
    /// A weight of 0 effectively disables the constraint.
    /// </summary>
    float Weight { get; }

    /// <summary>
    /// Initializes the constraint. Called once by the RotationConstraintSystem at Start().
    /// Useful for constraints that need to capture an initial runtime state (e.g., relative rotation).
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    void Initialize(Transform constrainedTransform);

    /// <summary>
    /// Calculates and returns this constraint's proposed target rotation for the constrained transform.
    /// This should be an absolute world-space rotation. The system will then blend these proposals.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    /// <returns>A Quaternion representing this constraint's desired world-space rotation.</returns>
    Quaternion GetProposedRotation(Transform constrainedTransform);
}
```

### 2. `RotationConstraintBase.cs`

This abstract `ScriptableObject` provides a common base for all concrete rotation constraints. It implements the `IRotationConstraint` interface and handles the `Weight` property, making it easy to create new constraint types.

```csharp
// RotationConstraintBase.cs
using UnityEngine;

/// <summary>
/// Abstract base class for all Rotation Constraints, implemented as ScriptableObjects.
/// This allows constraints to be reusable assets and easily configurable in the Inspector.
/// </summary>
public abstract class RotationConstraintBase : ScriptableObject, IRotationConstraint
{
    [Tooltip("How much this constraint contributes to the final rotation. A weight of 0 means no contribution.")]
    [SerializeField] private float _weight = 1f;

    /// <summary>
    /// Gets the weight of this constraint.
    /// </summary>
    public float Weight => _weight;

    /// <summary>
    /// Default empty implementation for Initialize. Concrete constraints can override if they need to capture initial state.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    public virtual void Initialize(Transform constrainedTransform) { }

    /// <summary>
    /// Abstract method to be implemented by concrete constraint classes.
    /// It should calculate and return this constraint's proposed target rotation.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    /// <returns>A Quaternion representing this constraint's desired world-space rotation.</returns>
    public abstract Quaternion GetProposedRotation(Transform constrainedTransform);
}
```

### 3. `RotationConstraintSystem.cs`

This `MonoBehaviour` is the heart of the system. Attach it to the GameObject whose rotation you want to constrain. It holds a list of `RotationConstraintBase` assets, evaluates them, and applies the combined rotation.

```csharp
// RotationConstraintSystem.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central manager for the RotationConstraintSystem design pattern.
/// Attach this script to the GameObject whose rotation you want to constrain.
/// It collects multiple IRotationConstraint ScriptableObjects, evaluates their proposed rotations,
/// and combines them into a final rotation for the target Transform.
/// </summary>
[DisallowMultipleComponent] // Only one system per GameObject
public class RotationConstraintSystem : MonoBehaviour
{
    [Tooltip("The Transform whose rotation will be controlled by this system.")]
    [SerializeField] private Transform _targetTransform;

    [Tooltip("List of RotationConstraint ScriptableObjects that will influence the target's rotation.")]
    [SerializeField] private List<RotationConstraintBase> _constraints = new List<RotationConstraintBase>();

    // Store active constraints and their proposed rotations for efficient blending.
    private readonly List<(Quaternion rotation, float weight)> _activeProposals = new List<(Quaternion rotation, float weight)>();

    private void Awake()
    {
        // If no target transform is explicitly set, default to this GameObject's transform.
        if (_targetTransform == null)
        {
            _targetTransform = transform;
            Debug.LogWarning($"RotationConstraintSystem on {name}: Target Transform was not set. Defaulting to this GameObject's Transform.", this);
        }
    }

    private void Start()
    {
        // Initialize all constraints. This is important for constraints that capture initial state.
        foreach (var constraint in _constraints)
        {
            if (constraint != null)
            {
                constraint.Initialize(_targetTransform);
            }
        }
    }

    /// <summary>
    /// LateUpdate is typically used for camera, animation, and constraint systems
    /// to ensure all movement and animation updates have completed first.
    /// </summary>
    private void LateUpdate()
    {
        ApplyConstraints();
    }

    /// <summary>
    /// Evaluates all active constraints and applies the blended rotation to the target transform.
    /// </summary>
    private void ApplyConstraints()
    {
        _activeProposals.Clear();
        float totalWeight = 0f;

        // 1. Collect all active proposals from constraints
        foreach (var constraint in _constraints)
        {
            if (constraint == null)
            {
                Debug.LogWarning($"RotationConstraintSystem on {name}: Found a null constraint in the list. Please remove it.", this);
                continue;
            }

            // Only consider constraints with a positive weight
            if (constraint.Weight > 0.0001f) // Use a small epsilon to avoid floating point issues
            {
                Quaternion proposedRotation = constraint.GetProposedRotation(_targetTransform);
                _activeProposals.Add((proposedRotation, constraint.Weight));
                totalWeight += constraint.Weight;
            }
        }

        // 2. If no active constraints, do nothing
        if (_activeProposals.Count == 0 || totalWeight == 0f)
        {
            return;
        }

        Quaternion finalRotation;

        // 3. Blend the proposed rotations using weighted Slerp accumulation
        if (_activeProposals.Count == 1)
        {
            finalRotation = _activeProposals[0].rotation;
        }
        else
        {
            // Start blending with the first proposal
            finalRotation = _activeProposals[0].rotation;
            float accumulatedWeight = _activeProposals[0].weight;

            // Iterate through the rest of the proposals, blending them one by one
            for (int i = 1; i < _activeProposals.Count; i++)
            {
                var currentProposal = _activeProposals[i];
                // Calculate slerpFactor relative to the accumulated weight so far.
                // This ensures each new constraint contributes proportionally to its weight.
                float slerpFactor = currentProposal.weight / (accumulatedWeight + currentProposal.weight);
                finalRotation = Quaternion.Slerp(finalRotation, currentProposal.rotation, slerpFactor);
                accumulatedWeight += currentProposal.weight;
            }
        }

        // 4. Apply the final blended rotation to the target transform
        _targetTransform.rotation = finalRotation;
    }

    /// <summary>
    /// Adds a new constraint to the system at runtime.
    /// </summary>
    /// <param name="newConstraint">The constraint to add.</param>
    /// <param name="initializeNow">If true, the constraint's Initialize method will be called immediately.</param>
    public void AddConstraint(RotationConstraintBase newConstraint, bool initializeNow = true)
    {
        if (newConstraint == null)
        {
            Debug.LogError("Cannot add a null constraint.");
            return;
        }
        if (!_constraints.Contains(newConstraint))
        {
            _constraints.Add(newConstraint);
            if (initializeNow)
            {
                newConstraint.Initialize(_targetTransform);
            }
        }
        else
        {
            Debug.LogWarning($"Constraint '{newConstraint.name}' is already in the system.", newConstraint);
        }
    }

    /// <summary>
    /// Removes a constraint from the system at runtime.
    /// </summary>
    /// <param name="constraintToRemove">The constraint to remove.</param>
    public void RemoveConstraint(RotationConstraintBase constraintToRemove)
    {
        if (constraintToRemove == null) return;
        _constraints.Remove(constraintToRemove);
    }
}
```

### 4. `LookAtConstraintSO.cs`

This constraint makes the `_targetTransform` (from the system) look at a specified `_lookAtTarget`.

```csharp
// LookAtConstraintSO.cs
using UnityEngine;

/// <summary>
/// A concrete rotation constraint that makes the target Transform look at another specified Transform.
/// </summary>
[CreateAssetMenu(fileName = "NewLookAtConstraint", menuName = "Rotation Constraints/Look At Constraint")]
public class LookAtConstraintSO : RotationConstraintBase
{
    [Tooltip("The Transform that the constrained object should look at.")]
    [SerializeField] private Transform _lookAtTarget;

    [Tooltip("The local up-axis of the constrained object (e.g., Vector3.up for a normal object).")]
    [SerializeField] private Vector3 _forwardAxis = Vector3.forward;

    [Tooltip("The world up-axis to use when calculating the LookAt rotation (e.g., Vector3.up to prevent rolling).")]
    [SerializeField] private Vector3 _worldUpAxis = Vector3.up;

    /// <summary>
    /// Calculates the rotation needed to make the constrained transform face the look-at target.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    /// <returns>A Quaternion representing the desired world-space rotation to look at the target.</returns>
    public override Quaternion GetProposedRotation(Transform constrainedTransform)
    {
        if (_lookAtTarget == null)
        {
            Debug.LogWarning($"{name}: Look At Target is null. Returning current rotation.", this);
            return constrainedTransform.rotation;
        }

        // Calculate the direction vector from the constrained transform to the look-at target
        Vector3 direction = _lookAtTarget.position - constrainedTransform.position;

        if (direction == Vector3.zero)
        {
            return constrainedTransform.rotation; // Avoid LookRotation with zero vector
        }

        // Calculate the target rotation using Quaternion.LookRotation
        // We need to account for the object's own forward axis if it's not Vector3.forward
        Quaternion targetLookRotation = Quaternion.LookRotation(direction, _worldUpAxis);

        // If the object's forward axis isn't Vector3.forward, adjust the target rotation
        // by rotating from Vector3.forward to the object's _forwardAxis.
        if (_forwardAxis != Vector3.forward)
        {
            Quaternion axisCorrection = Quaternion.FromToRotation(Vector3.forward, _forwardAxis);
            targetLookRotation *= Quaternion.Inverse(axisCorrection);
        }
        
        return targetLookRotation;
    }
}
```

### 5. `ClampRotationXConstraintSO.cs`

This constraint tries to clamp the target's rotation around its local X-axis within specified min/max angles. Note that in a blending system, a "clamp" often acts as an "influence" towards the clamped value rather than an absolute override.

```csharp
// ClampRotationXConstraintSO.cs
using UnityEngine;

/// <summary>
/// A concrete rotation constraint that attempts to clamp the target's local X-axis rotation
/// within a specified range. In a blending system, this acts as an "influence" towards
/// the clamped value rather than a hard override.
/// </summary>
[CreateAssetMenu(fileName = "NewClampXRotationConstraint", menuName = "Rotation Constraints/Clamp X-Axis Constraint")]
public class ClampRotationXConstraintSO : RotationConstraintBase
{
    [Tooltip("The minimum local X-axis angle (in degrees).")]
    [SerializeField] private float _minXAngle = -45f;

    [Tooltip("The maximum local X-axis angle (in degrees).")]
    [SerializeField] private float _maxXAngle = 45f;

    /// <summary>
    /// Calculates a proposed rotation that clamps the X-axis of the constrained transform's
    /// current local rotation, while keeping other axes as they are.
    /// This proposal will then be blended with other constraints.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    /// <returns>A Quaternion representing the desired world-space rotation with X-axis clamped.</returns>
    public override Quaternion GetProposedRotation(Transform constrainedTransform)
    {
        // Get the current local Euler angles of the constrained transform.
        Vector3 currentLocalEuler = constrainedTransform.localEulerAngles;

        // Normalize the X-axis angle to a -180 to 180 range for consistent clamping.
        currentLocalEuler.x = Mathf.DeltaAngle(0, currentLocalEuler.x);

        // Clamp the normalized X-axis angle.
        float clampedX = Mathf.Clamp(currentLocalEuler.x, _minXAngle, _maxXAngle);

        // Create a proposed rotation using the clamped X and the original Y and Z angles.
        // This is a "proposal" that will be blended. It aims to restore the Y and Z
        // while enforcing the X clamp.
        Quaternion proposedLocalRotation = Quaternion.Euler(clampedX, currentLocalEuler.y, currentLocalEuler.z);

        // Convert the local proposed rotation back to world space.
        // If the constrainedTransform has a parent, this will correctly convert.
        if (constrainedTransform.parent != null)
        {
            return constrainedTransform.parent.rotation * proposedLocalRotation;
        }
        else
        {
            return proposedLocalRotation; // Already in world space if no parent
        }
    }
}
```

### 6. `KeepRelativeRotationSO.cs`

This constraint tries to maintain a specific relative rotation to a chosen parent/reference transform.

```csharp
// KeepRelativeRotationSO.cs
using UnityEngine;

/// <summary>
/// A concrete rotation constraint that attempts to maintain a constant relative rotation
/// between the constrained transform and a specified parent/reference transform.
/// </summary>
[CreateAssetMenu(fileName = "NewKeepRelativeRotationConstraint", menuName = "Rotation Constraints/Keep Relative Rotation Constraint")]
public class KeepRelativeRotationSO : RotationConstraintBase
{
    [Tooltip("The parent/reference Transform to maintain relative rotation to.")]
    [SerializeField] private Transform _parentReference;

    // Stores the initial rotation of the constrained object relative to the parent reference.
    private Quaternion _initialRelativeRotation;

    /// <summary>
    /// Initializes the constraint by capturing the current relative rotation
    /// between the constrained transform and its parent reference.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    public override void Initialize(Transform constrainedTransform)
    {
        if (_parentReference == null)
        {
            Debug.LogWarning($"{name}: Parent Reference is null. This constraint will have no effect.", this);
            return;
        }
        // Calculate and store the initial rotation of the constrained transform relative to the parent.
        _initialRelativeRotation = Quaternion.Inverse(_parentReference.rotation) * constrainedTransform.rotation;
    }

    /// <summary>
    /// Calculates the world rotation needed to maintain the initial relative rotation
    /// with respect to the parent reference.
    /// </summary>
    /// <param name="constrainedTransform">The transform whose rotation is being constrained.</param>
    /// <returns>A Quaternion representing the desired world-space rotation to maintain relative orientation.</returns>
    public override Quaternion GetProposedRotation(Transform constrainedTransform)
    {
        if (_parentReference == null)
        {
            // If parent reference is null, this constraint cannot do its job,
            // so it proposes the current rotation (no change).
            return constrainedTransform.rotation;
        }
        // Calculate the world rotation that would result in the initial relative rotation.
        return _parentReference.rotation * _initialRelativeRotation;
    }
}
```

---

### Example Usage in Unity

Here's how to set up and use these scripts in a Unity project:

1.  **Create the Scripts**:
    *   Create a folder named `RotationConstraintSystem` (or similar) in your Unity `Assets` directory.
    *   Create the six C# script files listed above (`IRotationConstraint.cs`, `RotationConstraintBase.cs`, `RotationConstraintSystem.cs`, `LookAtConstraintSO.cs`, `ClampRotationXConstraintSO.cs`, `KeepRelativeRotationSO.cs`) and copy-paste the code into them.

2.  **Create Constraint Assets**:
    *   In the Project window, right-click -> Create -> Rotation Constraints. You'll see the menu items for "Look At Constraint", "Clamp X-Axis Constraint", and "Keep Relative Rotation Constraint".
    *   Create one of each (or multiple instances if you want different configurations). Name them descriptively, e.g., "PlayerLookAtCamera", "PlayerPitchClamp", "PlayerHeadKeepRelative".

3.  **Set up Scene Objects**:
    *   **Constrained Object**: Create a 3D object (e.g., a Cube or Capsule) that you want to rotate. This will be the `_targetTransform`. Let's call it "Player".
    *   **Look At Target**: Create another 3D object (e.g., a Sphere). This will be the target for the `LookAtConstraintSO`. Let's call it "TargetObject".
    *   **Parent Reference** (for `KeepRelativeRotationSO`): Create an empty GameObject or another 3D object to act as a parent reference. Let's call it "SceneReference".

4.  **Configure Constraint Assets**:
    *   **`PlayerLookAtCamera` (LookAtConstraintSO)**:
        *   Drag your "TargetObject" into the `Look At Target` field.
        *   Adjust `Weight` (e.g., 1.0).
        *   (Optional) Adjust `Forward Axis` or `World Up Axis` if your object's default forward isn't Z.
    *   **`PlayerPitchClamp` (ClampRotationXConstraintSO)**:
        *   Set `Min X Angle` (e.g., -45) and `Max X Angle` (e.g., 45) to define the pitching limits.
        *   Adjust `Weight` (e.g., 0.8).
    *   **`PlayerHeadKeepRelative` (KeepRelativeRotationSO)**:
        *   Drag your "SceneReference" into the `Parent Reference` field.
        *   Adjust `Weight` (e.g., 0.5).

5.  **Attach `RotationConstraintSystem`**:
    *   Select your "Player" GameObject.
    *   Add Component -> Search for "Rotation Constraint System" and add it.
    *   **Configure the `RotationConstraintSystem` on "Player"**:
        *   Ensure `Target Transform` is set to "Player" (or leave it empty if "Player" is the GameObject this script is on).
        *   In the `Constraints` list, click the "+" button to add slots.
        *   Drag your created constraint assets (e.g., "PlayerLookAtCamera", "PlayerPitchClamp", "PlayerHeadKeepRelative") into these slots.
        *   You can change the order or remove constraints here.

6.  **Run the Scene**:
    *   Press Play in the Unity editor.
    *   Move the "TargetObject" around, and your "Player" should rotate to look at it.
    *   Observe how the "Player" attempts to look at the "TargetObject" while also being influenced by the X-axis clamp and trying to maintain a relative rotation to the "SceneReference" (if all are enabled).
    *   Experiment with changing the `Weight` values on your `ScriptableObject` constraints at runtime to see how their influence changes. For example, set `PlayerPitchClamp` weight to 0 to disable it, or increase it to make the clamp stronger.

This setup provides a highly flexible and extensible system for managing complex rotational behaviors in your Unity projects.