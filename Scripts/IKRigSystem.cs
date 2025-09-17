// Unity Design Pattern Example: IKRigSystem
// This script demonstrates the IKRigSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `IKRigSystem` design pattern, while not a universally recognized Gamma-style pattern, represents a common architectural approach in game development for managing Inverse Kinematics (IK) within a flexible and extensible system. Its core idea is to abstract different IK solving algorithms behind a common interface, and then provide a manager (the `IKRigSystem` itself) to configure, activate, and control multiple IK chains.

This pattern promotes:
1.  **Separation of Concerns**: IK solving logic is separate from the management system.
2.  **Extensibility**: Easily add new IK solver types (e.g., CCD, FABRIK, Full Body IK) without modifying the core `IKRigSystem`.
3.  **Flexibility**: Configure multiple IK rigs, each with a different solver, target, and weight, through data.
4.  **Runtime Control**: Enable/disable rigs, change targets or pole targets, and adjust weights dynamically.

This example will demonstrate a simple 2-Bone IK solver as the concrete implementation, which is a common and practical use case for character limbs (arms, legs).

---

### **IKRigSystem Design Pattern Components:**

1.  **`IKSolver` (Interface):**
    *   Defines the contract for any IK solving algorithm.
    *   Methods: `Initialize`, `Solve`.
    *   Properties: `Root`, `Mid`, `End` (the bones in the chain), `IsInitialized`.

2.  **`TwoBoneIKSolver` (Concrete Implementation):**
    *   Implements the `IKSolver` interface.
    *   Contains the specific logic for a 2-Bone IK algorithm.
    *   Caches bone lengths and initial rotations for correct behavior and blending.

3.  **`IKSolverType` (Enum):**
    *   An enumeration to easily select which type of `IKSolver` to instantiate for a given rig.

4.  **`IKRigConfiguration` (Serializable Data Class):**
    *   A data structure that defines a single IK chain's properties.
    *   Configurable in the Unity Inspector.
    *   Includes bone references, target, pole target, solver type, active state, and weight.

5.  **`IKRig` (Runtime Wrapper Class):**
    *   A runtime instance that couples an `IKRigConfiguration` with its concrete `IKSolver` instance.
    *   Managed by the `IKRigSystem`.

6.  **`IKRigSystem` (MonoBehaviour):**
    *   The central manager component that holds a list of `IKRigConfiguration`s.
    *   Instantiates the appropriate `IKSolver`s based on configurations.
    *   Orchestrates the `Solve` calls in `LateUpdate` (crucial for IK to run after animations).
    *   Provides public methods for runtime control of the IK rigs.

---

### **Unity Setup Instructions:**

1.  Create an empty GameObject in your scene (e.g., "IKManager").
2.  Attach the `IKRigSystem.cs` script to this GameObject.
3.  Create a 3-bone hierarchy (e.g., `UpperArm -> Forearm -> Hand`). Ensure the bones point along a consistent local axis (e.g., local Y-axis is the bone's forward direction).
4.  Create two empty GameObjects: one for the IK `Target` (e.g., "HandTarget") and one for the `Pole Target` (e.g., "ElbowPole"). Position them appropriately.
5.  In the Inspector of your "IKManager" GameObject, expand the `Rig Configurations` list.
6.  Add a new element:
    *   **Rig Name**: "RightArmIK" (or whatever you like).
    *   **Root Bone**: Drag your `UpperArm` GameObject here.
    *   **Mid Bone**: Drag your `Forearm` GameObject here.
    *   **End Bone**: Drag your `Hand` GameObject here.
    *   **Target**: Drag your "HandTarget" GameObject here.
    *   **Pole Target**: Drag your "ElbowPole" GameObject here.
    *   **Solver Type**: Select `TwoBoneIK`.
    *   **Is Active**: Check this box.
    *   **Weight**: Set to `1`.
7.  Run the scene. Move the "HandTarget" and "ElbowPole" GameObjects to see the IK in action.

---

### **C# Scripts:**

Create these three C# scripts in your Unity project (e.g., in a `Scripts/IKSystem` folder):

#### 1. `IKSolver.cs`

```csharp
using UnityEngine;
using System; // For Serializable

namespace IKRigSystemExample
{
    /// <summary>
    /// Defines the contract for any Inverse Kinematics (IK) solver.
    /// This is the core abstraction of the IKRigSystem design pattern.
    /// New IK algorithms can be added by implementing this interface.
    /// </summary>
    public interface IKSolver
    {
        /// <summary>
        /// Initializes the solver with the bone chain transforms.
        /// This should be called once when the rig is set up to cache bone lengths, initial rotations, etc.
        /// </summary>
        /// <param name="root">The root bone of the IK chain.</param>
        /// <param name="mid">The middle bone of the IK chain.</param>
        /// <param name="end">The end effector bone of the IK chain.</param>
        void Initialize(Transform root, Transform mid, Transform end);

        /// <summary>
        /// Solves the IK for the current frame, moving the bones towards the target.
        /// This method is typically called in LateUpdate.
        /// </summary>
        /// <param name="target">The target transform the end effector should try to reach.</param>
        /// <param name="poleTarget">The pole target transform that guides the middle joint's direction.</param>
        /// <param name="weight">The blending weight (0-1) for the IK effect. 0 means no IK, 1 means full IK.</param>
        void Solve(Transform target, Transform poleTarget, float weight);

        /// <summary>
        /// Gets a value indicating whether the solver has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the root bone of the IK chain.
        /// </summary>
        Transform Root { get; }

        /// <summary>
        /// Gets the middle bone of the IK chain.
        /// </summary>
        Transform Mid { get; }

        /// <summary>
        /// Gets the end effector bone of the IK chain.
        /// </summary>
        Transform End { get; }
    }

    /// <summary>
    /// Enumeration for different types of IK solvers.
    /// This allows the IKRigSystem to instantiate the correct solver based on configuration.
    /// </summary>
    public enum IKSolverType
    {
        TwoBoneIK,
        // Future solvers can be added here, e.g., CCDIK, FABRIK, etc.
    }
}
```

#### 2. `TwoBoneIKSolver.cs`

```csharp
using UnityEngine;

namespace IKRigSystemExample
{
    /// <summary>
    /// A concrete implementation of the IKSolver interface for a 2-Bone IK chain.
    /// This solver assumes bones extend along their local Y-axis and bend around their local X-axis.
    /// </summary>
    public class TwoBoneIKSolver : IKSolver
    {
        // --- IKSolver Interface Properties ---
        public Transform Root { get; private set; }
        public Transform Mid { get; private set; }
        public Transform End { get; private set; }
        public bool IsInitialized { get; private set; }

        // --- Internal Solver Properties ---
        private float boneLength1; // Distance from Root to Mid
        private float boneLength2; // Distance from Mid to End

        // Store initial local rotations to apply IK as an offset, preserving animator's pose
        private Quaternion initialLocalRootRotation;
        private Quaternion initialLocalMidRotation;
        private Quaternion initialLocalEndRotation;

        /// <summary>
        /// Initializes the 2-Bone IK solver.
        /// Caches bone references, lengths, and initial local rotations.
        /// </summary>
        public void Initialize(Transform root, Transform mid, Transform end)
        {
            Root = root;
            Mid = mid;
            End = end;

            if (Root == null || Mid == null || End == null)
            {
                Debug.LogError("TwoBoneIKSolver initialization failed: One or more bone transforms are null.");
                IsInitialized = false;
                return;
            }

            boneLength1 = Vector3.Distance(Root.position, Mid.position);
            boneLength2 = Vector3.Distance(Mid.position, End.position);

            // Store initial local rotations. These are used as base rotations
            // upon which IK adjustments are made, and for blending.
            initialLocalRootRotation = Root.localRotation;
            initialLocalMidRotation = Mid.localRotation;
            initialLocalEndRotation = End.localRotation;

            IsInitialized = true;
        }

        /// <summary>
        /// Solves the 2-Bone IK chain, moving the Root and Mid bones to reach the target.
        /// Applies the IK solution blended by the given weight.
        /// </summary>
        /// <param name="target">The target transform the End bone should reach.</param>
        /// <param name="poleTarget">The pole target transform that guides the Mid bone's direction (e.g., elbow/knee direction).</param>
        /// <param name="weight">The blending weight (0-1) for the IK effect.</param>
        public void Solve(Transform target, Transform poleTarget, float weight)
        {
            // Early exit if not initialized, target/pole are missing, or weight is negligible
            if (!IsInitialized || target == null || poleTarget == null || Root == null || Mid == null || End == null || weight <= 0.0001f)
            {
                // If weight is 0, revert to original pose
                if (weight <= 0.0001f && IsInitialized)
                {
                    Root.localRotation = Quaternion.Slerp(Root.localRotation, initialLocalRootRotation, 1 - weight); // Blend out completely
                    Mid.localRotation = Quaternion.Slerp(Mid.localRotation, initialLocalMidRotation, 1 - weight);
                    End.localRotation = Quaternion.Slerp(End.localRotation, initialLocalEndRotation, 1 - weight);
                }
                return;
            }

            // Store original local rotations for blending
            Quaternion currentRootLocalRotation = Root.localRotation;
            Quaternion currentMidLocalRotation = Mid.localRotation;
            Quaternion currentEndLocalRotation = End.localRotation;

            // --- IK Calculation ---
            Vector3 rootPos = Root.position;
            Vector3 targetPos = target.position;
            Vector3 polePos = poleTarget.position;

            // 1. Calculate the distance from Root to Target
            Vector3 rootToTarget = targetPos - rootPos;
            float targetDist = rootToTarget.magnitude;

            // Clamp targetDist to avoid impossible solutions (stretch/compress beyond limits)
            targetDist = Mathf.Clamp(targetDist, Mathf.Abs(boneLength1 - boneLength2), boneLength1 + boneLength2);
            rootToTarget = rootToTarget.normalized * targetDist; // Recalculate if clamped

            // 2. Solve for angles using Law of Cosines
            // alpha: Angle at the Root joint (between Root->Target and Root->Mid direction)
            float cosAlpha = (boneLength1 * boneLength1 + targetDist * targetDist - boneLength2 * boneLength2) / (2 * boneLength1 * targetDist);
            float alpha = Mathf.Acos(Mathf.Clamp(cosAlpha, -1f, 1f)); // in radians

            // beta: Angle at the Mid joint (the actual elbow/knee bend)
            float cosBeta = (boneLength1 * boneLength1 + boneLength2 * boneLength2 - targetDist * targetDist) / (2 * boneLength1 * boneLength2);
            float beta = Mathf.Acos(Mathf.Clamp(cosBeta, -1f, 1f)); // in radians

            // 3. Determine the plane for the Mid bone (elbow/knee).
            // This plane is defined by Root, Target, and Pole Target.
            Vector3 planeNormal = Vector3.Cross(rootToTarget, polePos - rootPos).normalized;

            // Fallback for collinear Pole Target: Use a world up/right vector
            if (planeNormal == Vector3.zero)
            {
                planeNormal = Vector3.Cross(rootToTarget, Vector3.up).normalized;
                if (planeNormal == Vector3.zero)
                {
                    planeNormal = Vector3.Cross(rootToTarget, Vector3.right).normalized;
                }
            }

            // 4. Calculate the desired world rotation for the Root bone
            // This rotation aligns Root's local Y-axis (forward) towards `rootToTarget`
            // and its local Z-axis (twist/side) with `planeNormal`.
            // The result is that the X-axis becomes the bend axis, perpendicular to the plane.
            Quaternion baseRotation = Quaternion.LookRotation(rootToTarget, planeNormal);

            // Apply the Root bend angle (alpha). This rotates around the local X-axis.
            // `-alpha` because the arm/leg typically bends "backwards" to reach the target.
            Quaternion rootBendRotation = Quaternion.AngleAxis(-alpha * Mathf.Rad2Deg, Vector3.right);

            // Combine the base rotation (aligning with target/pole) with the initial local rotation
            // and the bend angle.
            // The initialLocalRootRotation ensures the bone's original orientation is considered.
            Quaternion desiredRootLocalRotation = initialLocalRootRotation * rootBendRotation;

            // Apply the Mid bend angle (beta). This rotates around the local X-axis.
            // `beta` is the interior angle of the elbow/knee, so it's a positive bend.
            Quaternion desiredMidLocalRotation = initialLocalMidRotation * Quaternion.AngleAxis(beta * Mathf.Rad2Deg, Vector3.right);

            // For the End bone, typically its original local rotation relative to the Mid bone is preserved.
            Quaternion desiredEndLocalRotation = initialLocalEndRotation;

            // 5. Apply the calculated rotations, blending with the current (animated) rotations
            // using the provided weight.
            Root.localRotation = Quaternion.Slerp(currentRootLocalRotation, desiredRootLocalRotation, weight);
            Mid.localRotation = Quaternion.Slerp(currentMidLocalRotation, desiredMidLocalRotation, weight);
            End.localRotation = Quaternion.Slerp(currentEndLocalRotation, desiredEndLocalRotation, weight);
        }
    }
}
```

#### 3. `IKRigSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic; // For List
using System; // For Serializable

namespace IKRigSystemExample
{
    /// <summary>
    /// Configuration data for a single IK rig.
    /// This class is [Serializable] so it can be configured directly in the Unity Inspector.
    /// It defines which bones are part of the chain, the target, pole target, and the solver type.
    /// </summary>
    [Serializable]
    public class IKRigConfiguration
    {
        public string RigName = "New IKRig";

        [Header("Bone Chain (Root -> Mid -> End)")]
        [Tooltip("The root bone of the IK chain (e.g., UpperArm).")]
        public Transform rootBone;
        [Tooltip("The middle bone of the IK chain (e.g., Forearm).")]
        public Transform midBone;
        [Tooltip("The end effector bone of the IK chain (e.g., Hand).")]
        public Transform endBone;

        [Header("Targets")]
        [Tooltip("The target transform the end bone should try to reach.")]
        public Transform target;
        [Tooltip("The pole target transform that guides the middle joint's direction (e.g., ElbowPole).")]
        public Transform poleTarget;

        [Header("Solver Settings")]
        [Tooltip("The type of IK solver to use for this rig.")]
        public IKSolverType solverType = IKSolverType.TwoBoneIK;
        [Tooltip("Is this IK rig currently active?")]
        public bool isActive = true;
        [Range(0f, 1f)]
        [Tooltip("The blending weight for the IK effect (0 = no IK, 1 = full IK).")]
        public float weight = 1f;

        // Private fields to ensure settings are applied
        private bool _prevIsActive;
        private float _prevWeight;
        private Transform _prevTarget;
        private Transform _prevPoleTarget;

        /// <summary>
        /// Applies the current configuration settings to the solver instance.
        /// Used internally by IKRigSystem to synchronize changes from Inspector.
        /// </summary>
        public void ApplySettings(IKRig rig)
        {
            if (rig == null || rig.Config != this) return;

            // If active state changes, re-initialize or disable
            if (_prevIsActive != isActive)
            {
                rig.SetActive(isActive);
                _prevIsActive = isActive;
            }
            rig.Weight = weight;
            rig.Target = target;
            rig.PoleTarget = poleTarget;
        }

        /// <summary>
        /// Stores the current state as previous state for change detection.
        /// </summary>
        public void StorePreviousState()
        {
            _prevIsActive = isActive;
            _prevWeight = weight;
            _prevTarget = target;
            _prevPoleTarget = poleTarget;
        }
    }

    /// <summary>
    /// Runtime class representing an active IK Rig.
    /// It holds a reference to its configuration and the actual IKSolver instance.
    /// This allows for runtime control and separation of configuration from live solver.
    /// </summary>
    public class IKRig
    {
        public IKRigConfiguration Config { get; private set; }
        private IKSolver _solver;

        public float Weight { get { return Config.weight; } set { Config.weight = Mathf.Clamp01(value); } }
        public Transform Target { get { return Config.target; } set { Config.target = value; } }
        public Transform PoleTarget { get { return Config.poleTarget; } set { Config.poleTarget = value; } }
        public bool IsActive { get { return Config.isActive; } }

        public IKRig(IKRigConfiguration config)
        {
            Config = config;
            _solver = CreateSolver(config.solverType);
            if (_solver != null)
            {
                _solver.Initialize(config.rootBone, config.midBone, config.endBone);
            }
            else
            {
                Debug.LogError($"IKRig: Failed to create solver of type {config.solverType} for rig '{config.RigName}'.");
            }
        }

        /// <summary>
        /// Creates an IKSolver instance based on the specified solver type.
        /// This is the "Factory" part of the pattern, abstracting solver instantiation.
        /// </summary>
        private IKSolver CreateSolver(IKSolverType type)
        {
            switch (type)
            {
                case IKSolverType.TwoBoneIK:
                    return new TwoBoneIKSolver();
                default:
                    Debug.LogError($"Unknown IKSolverType: {type}. Returning null.");
                    return null;
            }
        }

        /// <summary>
        /// Sets the active state of this IK rig.
        /// </summary>
        /// <param name="active">True to activate, false to deactivate.</param>
        public void SetActive(bool active)
        {
            Config.isActive = active;
        }

        /// <summary>
        /// Updates the IK solver for this rig.
        /// Calls the Solve method of the underlying IKSolver instance.
        /// </summary>
        public void UpdateSolver()
        {
            if (_solver != null && _solver.IsInitialized && IsActive)
            {
                _solver.Solve(Target, PoleTarget, Weight);
            }
        }

        /// <summary>
        /// Gets the underlying IKSolver instance for this rig.
        /// </summary>
        public IKSolver GetSolver()
        {
            return _solver;
        }
    }

    /// <summary>
    /// The IKRigSystem MonoBehaviour is the central manager for all IK rigs in the scene.
    /// It maintains a list of IKRigConfigurations and orchestrates their runtime behavior.
    /// This demonstrates the core of the 'IKRigSystem' design pattern.
    /// </summary>
    [DisallowMultipleComponent]
    public class IKRigSystem : MonoBehaviour
    {
        [Tooltip("List of IK rig configurations to manage.")]
        public List<IKRigConfiguration> rigConfigurations = new List<IKRigConfiguration>();

        // Runtime list of active IKRig instances
        private List<IKRig> _runtimeIKRigs = new List<IKRig>();
        private Dictionary<string, IKRig> _rigsByName = new Dictionary<string, IKRig>();

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes all IK rigs based on the configurations.
        /// </summary>
        void Awake()
        {
            InitializeAllRigs();
        }

        /// <summary>
        /// Called every frame after all Update functions have been called.
        /// This is the ideal place for IK calculations as it runs after animation updates.
        /// </summary>
        void LateUpdate()
        {
            // Iterate through all active runtime rigs and update their solvers.
            foreach (var rig in _runtimeIKRigs)
            {
                if (rig.IsActive)
                {
                    rig.UpdateSolver();
                }
                // Update properties from Inspector changes in editor
                #if UNITY_EDITOR
                rig.Config.ApplySettings(rig);
                #endif
            }
        }

        /// <summary>
        /// Initializes all IK rigs specified in the `rigConfigurations` list.
        /// This method is called once on Awake.
        /// </summary>
        private void InitializeAllRigs()
        {
            _runtimeIKRigs.Clear();
            _rigsByName.Clear();

            foreach (var config in rigConfigurations)
            {
                // Validate configuration before creating a rig
                if (config.rootBone == null || config.midBone == null || config.endBone == null)
                {
                    Debug.LogWarning($"IKRigSystem: Rig '{config.RigName}' is missing one or more bone transforms and will not be initialized.", this);
                    continue;
                }
                if (config.target == null)
                {
                    Debug.LogWarning($"IKRigSystem: Rig '{config.RigName}' is missing a target transform and will not be initialized.", this);
                    continue;
                }
                if (config.poleTarget == null)
                {
                    Debug.LogWarning($"IKRigSystem: Rig '{config.RigName}' is missing a pole target transform. This may lead to unpredictable IK behavior.", this);
                }

                if (_rigsByName.ContainsKey(config.RigName))
                {
                    Debug.LogWarning($"IKRigSystem: Duplicate rig name '{config.RigName}' found. Only the first instance will be active.", this);
                    continue;
                }

                IKRig newRig = new IKRig(config);
                _runtimeIKRigs.Add(newRig);
                _rigsByName.Add(config.RigName, newRig);
                config.StorePreviousState(); // Store initial state for change detection
            }
        }

        /// <summary>
        /// Retrieves an IKRig instance by its name.
        /// </summary>
        /// <param name="rigName">The name of the rig to retrieve.</param>
        /// <returns>The IKRig instance if found, otherwise null.</returns>
        public IKRig GetRig(string rigName)
        {
            _rigsByName.TryGetValue(rigName, out IKRig rig);
            return rig;
        }

        /// <summary>
        /// Sets the active state of a specific IK rig by name.
        /// </summary>
        /// <param name="rigName">The name of the rig to activate/deactivate.</param>
        /// <param name="active">True to activate, false to deactivate.</param>
        public void SetRigActive(string rigName, bool active)
        {
            IKRig rig = GetRig(rigName);
            if (rig != null)
            {
                rig.SetActive(active);
                rig.Config.isActive = active; // Update config for Inspector visibility
            }
            else
            {
                Debug.LogWarning($"IKRigSystem: Rig '{rigName}' not found.");
            }
        }

        /// <summary>
        /// Sets the target transform for a specific IK rig by name.
        /// </summary>
        /// <param name="rigName">The name of the rig.</param>
        /// <param name="newTarget">The new target transform.</param>
        public void SetRigTarget(string rigName, Transform newTarget)
        {
            IKRig rig = GetRig(rigName);
            if (rig != null)
            {
                rig.Target = newTarget;
                rig.Config.target = newTarget; // Update config for Inspector visibility
            }
            else
            {
                Debug.LogWarning($"IKRigSystem: Rig '{rigName}' not found.");
            }
        }

        /// <summary>
        /// Sets the blending weight for a specific IK rig by name.
        /// </summary>
        /// <param name="rigName">The name of the rig.</param>
        /// <param name="weight">The new weight (0-1).</param>
        public void SetRigWeight(string rigName, float weight)
        {
            IKRig rig = GetRig(rigName);
            if (rig != null)
            {
                rig.Weight = weight;
                rig.Config.weight = weight; // Update config for Inspector visibility
            }
            else
            {
                Debug.LogWarning($"IKRigSystem: Rig '{rigName}' not found.");
            }
        }

        // --- Example Usage (Commented Out) ---
        /*
        [ContextMenu("Toggle RightArmIK Active")]
        void ToggleRightArmIK()
        {
            IKRig rightArmRig = GetRig("RightArmIK");
            if (rightArmRig != null)
            {
                SetRigActive("RightArmIK", !rightArmRig.IsActive);
                Debug.Log($"RightArmIK active status: {rightArmRig.IsActive}");
            }
        }

        // Example: Changing the target at runtime
        public Transform alternateHandTarget;
        [ContextMenu("Switch Hand Target")]
        void SwitchHandTarget()
        {
            IKRig rightArmRig = GetRig("RightArmIK");
            if (rightArmRig != null)
            {
                if (rightArmRig.Target == alternateHandTarget)
                {
                    // Assuming you have an original target set in the Inspector
                    SetRigTarget("RightArmIK", rigConfigurations[0].target); // Revert to original config target
                }
                else
                {
                    SetRigTarget("RightArmIK", alternateHandTarget);
                }
            }
        }

        // Example: Gradually increase/decrease weight over time
        private bool _increasingWeight = true;
        void Update()
        {
            IKRig rightArmRig = GetRig("RightArmIK");
            if (rightArmRig != null)
            {
                float currentWeight = rightArmRig.Weight;
                if (_increasingWeight)
                {
                    currentWeight += Time.deltaTime * 0.5f; // Increase over 2 seconds
                    if (currentWeight >= 1f)
                    {
                        currentWeight = 1f;
                        _increasingWeight = false;
                    }
                }
                else
                {
                    currentWeight -= Time.deltaTime * 0.5f; // Decrease over 2 seconds
                    if (currentWeight <= 0f)
                    {
                        currentWeight = 0f;
                        _increasingWeight = true;
                    }
                }
                SetRigWeight("RightArmIK", currentWeight);
            }
        }
        */
    }
}
```