// Unity Design Pattern Example: AnimationRiggingSystem
// This script demonstrates the AnimationRiggingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script demonstrates the **'AnimationRiggingSystem' design pattern** within Unity, leveraging the `Unity.Animation.Rigging` package. This pattern focuses on structuring how you integrate and manage various rigging behaviors for a character, allowing for modularity, dynamic control, and clear separation of concerns.

---

### **Animation Rigging System Design Pattern**

**Core Idea:**
Decouple specific rigging logic (e.g., Inverse Kinematics (IK), Look-At behavior, limb bending) from the main animation system and character controller. A central "Rigging System Controller" orchestrates these individual rigging "modules" (which are typically `Constraint` components from Unity's Animation Rigging package), allowing them to be dynamically enabled, weighted, and configured at runtime.

**Key Components of the Pattern:**

1.  **Rigging Modules (Constraints):**
    *   **Description:** Individual, self-contained units that perform a specific rigging task.
    *   **Unity Example:** `TwoBoneIKConstraint`, `MultiAimConstraint`, `BlendConstraint`, `TwistCorrection`, etc.
    *   **Role:** Each module operates on specific bones to achieve a localized effect. They are typically placed as children of the main `Rig` GameObject.

2.  **Rigging System Controller (This Script):**
    *   **Description:** A central manager script responsible for interacting with and orchestrating the various Rigging Modules.
    *   **Role:**
        *   Holds references to the various `Constraint` components.
        *   Provides a clean API (public methods) to enable/disable modules.
        *   Provides an API to adjust module weights (influence, 0.0 to 1.0).
        *   Provides an API to update module targets or other parameters dynamically.
        *   Manages the overall state of the character's rigging.

3.  **Rig (Unity `Rig` component):**
    *   **Description:** The foundational component from the `Unity.Animation.Rigging` package that processes all `IAnimationJob` constraints within its hierarchy.
    *   **Role:** It's the "engine" that evaluates and applies all the active constraints. The Rigging System Controller implicitly interacts *through* this component by managing its contained constraints.

4.  **Targets:**
    *   **Description:** `Transform`s or other data that the rigging modules use as input to define their behavior.
    *   **Role:** These dictate where an IK foot should go, what the head should look at, etc. They are typically separate GameObjects in the scene that can be moved or animated independently.

**Benefits of this Pattern:**

*   **Modularity:** Easily add or remove rigging features (e.g., adding an arm IK or finger curling) without affecting other parts of the system.
*   **Flexibility & Dynamic Control:** Rigging effects can be enabled, disabled, or blended in/out at runtime based on game logic (e.g., only enable foot IK when walking on uneven terrain, make a character look at the nearest enemy, or use different hand poses for gripping different objects).
*   **Separation of Concerns:** Animation logic (motion captured or keyframe animations) is cleanly separated from corrective or interactive rigging logic. Animators can create general movements, while riggers or gameplay programmers layer on dynamic behaviors.
*   **Maintainability:** Easier to debug and update individual rigging parts because they are encapsulated and managed centrally.
*   **Reusability:** Rigging modules and the controller logic can often be reused across different characters or scenarios with similar needs.

---

### **Example Use Case Demonstrated:**

This script will set up a character to use:
1.  **Two-Bone IK for feet:** To precisely plant feet on the ground or follow specific paths.
2.  **Multi-Aim Constraint for head:** To make the character's head look at a specific target (e.g., a player, enemy, or an object of interest).

---

### **Installation & Setup Instructions:**

To make this script work in your Unity project:

1.  **Install the 'Animation Rigging' package:**
    *   Go to `Window -> Package Manager`.
    *   In the Package Manager window, select "Unity Registry" from the dropdown.
    *   Search for "Animation Rigging" and click "Install".

2.  **Prepare your 3D Character:**
    *   You'll need a character with a humanoid rig (e.g., a simple humanoid from Unity's Asset Store, Mixamo, or your own model).
    *   Ensure your character has an `Animator` component with some animation playing (e.g., an idle animation).

3.  **Add the `Rig` Component:**
    *   Select your character's root `GameObject` (the one with the `Animator`).
    *   Click `Add Component` and search for "Rig". Add the `Rig` component.

4.  **Set up IK Constraints for Legs:**
    *   **Create a new empty `GameObject`** under your character's root. Name it something like "RiggingContainer". This is not strictly necessary, but helps organize your constraints.
    *   **Add `TwoBoneIKConstraint` for Left Leg:**
        *   Right-click "RiggingContainer" (or your character's root) -> `Create Empty`. Name it "LeftLegIK".
        *   Select "LeftLegIK". Click `Add Component` and search for "Two Bone IK Constraint".
        *   **Configure:**
            *   `Root`: Drag your character's upper leg bone (e.g., "LeftUpLeg") here.
            *   `Mid`: Drag your character's lower leg bone (e.g., "LeftLeg") here.
            *   `Tip`: Drag your character's foot bone (e.g., "LeftFoot") here.
    *   **Add `TwoBoneIKConstraint` for Right Leg:** Repeat the above steps, naming it "RightLegIK" and assigning the right leg bones.

5.  **Set up Multi-Aim Constraint for Head:**
    *   **Add `MultiAimConstraint` for Head:**
        *   Right-click "RiggingContainer" -> `Create Empty`. Name it "HeadLookAt".
        *   Select "HeadLookAt". Click `Add Component` and search for "Multi Aim Constraint".
        *   **Configure:**
            *   `Constrained Object`: Drag your character's head bone (e.g., "Head") here.
            *   For the 'Aim Sources' list, leave it empty for now, the script will populate it.

6.  **Create IK Target & Look-At Target GameObjects:**
    *   In your scene hierarchy, right-click -> `Create Empty`. Name it "LeftFootIKTarget". Position it near where the left foot would naturally rest on the ground.
    *   Repeat for "RightFootIKTarget".
    *   Right-click -> `Create Empty`. Name it "HeadLookAtTarget". Position it slightly in front of and above your character's head. These will be the dynamic targets.

7.  **Add this `RiggingSystemController` Script:**
    *   Create a new C# script named `RiggingSystemController.cs`.
    *   Copy and paste the code below into it.
    *   Drag this script onto your character's root `GameObject` (the same one with the `Rig` component).

8.  **Link Components in the Inspector:**
    *   Select your character's root `GameObject` in the Hierarchy.
    *   In the Inspector, locate the `RiggingSystemController` component.
    *   **Drag & Drop:**
        *   `Main Rig`: Drag the `Rig` component from your character's root onto this field (it might auto-populate due to `[RequireComponent]`).
        *   `Left Leg IK Constraint`: Drag the "LeftLegIK" `GameObject` (or its `TwoBoneIKConstraint` component) from your hierarchy into this field.
        *   `Right Leg IK Constraint`: Drag the "RightLegIK" `GameObject` (or its `TwoBoneIKConstraint` component) into this field.
        *   `Head Look At Constraint`: Drag the "HeadLookAt" `GameObject` (or its `MultiAimConstraint` component) into this field.
        *   `Left Leg IK Target`: Drag the "LeftFootIKTarget" `GameObject` from your scene into this field.
        *   `Right Leg IK Target`: Drag the "RightFootIKTarget" `GameObject` from your scene into this field.
        *   `Head Look At Target`: Drag the "HeadLookAtTarget" `GameObject` from your scene into this field.

9.  **Run the Scene!**
    *   Observe how the feet attempt to reach their targets and the head looks at its target.
    *   You can select the target `GameObjects` in the scene view and move them around to see the rigging update dynamically.
    *   Use the `[ContextMenu]` methods "Test Rigging On/Off" and "Test Change Look-At Target" in the Inspector of the `RiggingSystemController` to see dynamic control in action.

---

```csharp
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This script demonstrates the 'AnimationRiggingSystem' design pattern in Unity.
/// 
/// The 'AnimationRiggingSystem' pattern describes how to organize and manage a collection
/// of independent rigging components (like Unity's Rig constraints) to achieve complex
/// and dynamic character animation behaviors.
/// 
/// Core Idea:
/// Decouple specific rigging logic (e.g., IK calculation, Look-At behavior) from the main
/// animation system and character controller. A central "Rigging System Controller"
/// orchestrates these individual rigging "modules" (constraints), allowing them to be
/// dynamically enabled, weighted, and configured at runtime.
/// 
/// Key Components of the Pattern:
/// 1.  Rigging Modules (Constraints): Individual, self-contained units that perform a
///     specific rigging task. In Unity's 'Animation Rigging' package, these are
///     components like TwoBoneIKConstraint, MultiAimConstraint, BlendConstraint, etc.
///     They operate on specific bones.
/// 2.  Rigging System Controller: A central manager script responsible for:
///     - Holding references to the various Rigging Modules.
///     - Providing an API to enable/disable modules.
///     - Providing an API to adjust module weights (influence).
///     - Providing an API to update module targets or parameters.
/// 3.  Rig (Unity Rig component): The foundational component from the 'Animation Rigging'
///     package that processes all IAnimationJob constraints within its hierarchy. The
///     Rigging System Controller interacts *through* this component implicitly by
///     managing its contained constraints.
/// 4.  Targets: Transforms or other data that the rigging modules use as input
///     (e.g., IK target positions, look-at targets).
/// 
/// Benefits:
/// - Modularity: Easily add or remove rigging features without affecting other parts.
/// - Flexibility: Dynamic control over rigging effects at runtime.
/// - Separation of Concerns: Animation logic (curves) is separate from corrective/interactive
///   rigging logic.
/// - Maintainability: Easier to debug and update individual rigging parts.
/// - Reusability: Rigging modules can be reused across different characters or scenarios.
/// 
/// Example Use Case:
/// A human character that needs:
/// 1.  Two-Bone IK for feet: To plant feet precisely on the ground or follow specific paths.
/// 2.  Multi-Aim Constraint for head: To make the character look at a specific target
///     (player, enemy, object of interest).
/// 
/// See the extensive setup instructions at the top of this script file!
/// </summary>
[RequireComponent(typeof(Rig))] // Ensure the character has a Rig component
public class RiggingSystemController : MonoBehaviour
{
    [Header("Rigging System References")]
    [Tooltip("The main Rig component that orchestrates all constraints.")]
    [SerializeField] private Rig _mainRig;

    [Tooltip("Reference to the TwoBoneIKConstraint for the left leg.")]
    [SerializeField] private TwoBoneIKConstraint _leftLegIKConstraint;
    [Tooltip("Reference to the TwoBoneIKConstraint for the right leg.")]
    [SerializeField] private TwoBoneIKConstraint _rightLegIKConstraint;
    [Tooltip("Reference to the MultiAimConstraint for the head/neck.")]
    [SerializeField] private MultiAimConstraint _headLookAtConstraint;

    [Header("Rigging Targets")]
    [Tooltip("The Transform that the left leg IK will try to reach.")]
    public Transform leftLegIKTarget;
    [Tooltip("The Transform that the right leg IK will try to reach.")]
    public Transform rightLegIKTarget;
    [Tooltip("The Transform that the head will look at.")]
    public Transform headLookAtTarget;

    // Optional: Keep track of initial weights for resetting or blending
    private float _initialLeftLegIKWeight;
    private float _initialRightLegIKWeight;
    private float _initialHeadLookAtWeight;

    private void Awake()
    {
        // If not assigned in Inspector, try to find the Rig component on this GameObject.
        if (_mainRig == null)
        {
            _mainRig = GetComponent<Rig>();
        }

        // Store initial weights for potential resetting or complex blending.
        // It's assumed that the initial weights are set correctly in the Inspector
        // on the constraint components themselves.
        if (_leftLegIKConstraint != null)
        {
            _initialLeftLegIKWeight = _leftLegIKConstraint.weight;
        }
        if (_rightLegIKConstraint != null)
        {
            _initialRightLegIKWeight = _rightLegIKConstraint.weight;
        }
        if (_headLookAtConstraint != null)
        {
            _initialHeadLookAtWeight = _headLookAtConstraint.weight;
        }

        // Ensure constraint targets are initialized based on public fields.
        // This is important if targets are assigned dynamically or if the constraints
        // haven't had their targets linked in the Inspector.
        InitializeConstraintTargets();
    }

    private void Start()
    {
        // Example initial runtime setup:
        // Start with head look-at fully enabled, but IK off, ready to be activated by game logic.
        SetHeadLookAtWeight(1.0f);
        SetLeftLegIKWeight(0.0f);
        SetRightLegIKWeight(0.0f);
    }

    /// <summary>
    /// Initializes or updates the targets for the rigging constraints based on the public
    /// 'leftLegIKTarget', 'rightLegIKTarget', and 'headLookAtTarget' fields.
    /// This method ensures that the constraints know which Transforms to operate on.
    /// </summary>
    private void InitializeConstraintTargets()
    {
        if (_leftLegIKConstraint != null && leftLegIKTarget != null)
        {
            _leftLegIKConstraint.data.target = leftLegIKTarget;
        }
        if (_rightLegIKConstraint != null && rightLegIKTarget != null)
        {
            _rightLegIKConstraint.data.target = rightLegIKTarget;
        }
        
        if (_headLookAtConstraint != null && headLookAtTarget != null)
        {
            // MultiAimConstraint uses a list of sources. We'll manage one source for simplicity.
            // If no sources exist, add the new target.
            if (_headLookAtConstraint.data.sourceObjects.Count == 0)
            {
                _headLookAtConstraint.data.sourceObjects.Add(new WeightedTransform(headLookAtTarget, 1.0f));
            }
            else
            {
                // Otherwise, update the existing first source.
                // We fetch the existing WeightedTransform, update its transform, and reassign it.
                WeightedTransform existingSource = _headLookAtConstraint.data.sourceObjects[0];
                existingSource.transform = headLookAtTarget;
                _headLookAtConstraint.data.sourceObjects[0] = existingSource;
            }
        }
    }

    // --- Public API for Controlling Rigging Modules ---

    /// <summary>
    /// Sets the weight (influence) of the left leg IK constraint.
    /// A weight of 0 means the constraint has no effect, 1 means full effect,
    /// and values in between blend the effect.
    /// </summary>
    /// <param name="weight">The desired weight (0.0 to 1.0).</param>
    public void SetLeftLegIKWeight(float weight)
    {
        if (_leftLegIKConstraint != null)
        {
            _leftLegIKConstraint.weight = Mathf.Clamp01(weight);
            // Debug.Log($"Left Leg IK weight set to: {_leftLegIKConstraint.weight}");
        }
    }

    /// <summary>
    /// Sets the weight (influence) of the right leg IK constraint.
    /// </summary>
    /// <param name="weight">The desired weight (0.0 to 1.0).</param>
    public void SetRightLegIKWeight(float weight)
    {
        if (_rightLegIKConstraint != null)
        {
            _rightLegIKConstraint.weight = Mathf.Clamp01(weight);
            // Debug.Log($"Right Leg IK weight set to: {_rightLegIKConstraint.weight}");
        }
    }

    /// <summary>
    /// Sets the weight (influence) of the head look-at constraint.
    /// </summary>
    /// <param name="weight">The desired weight (0.0 to 1.0).</param>
    public void SetHeadLookAtWeight(float weight)
    {
        if (_headLookAtConstraint != null)
        {
            _headLookAtConstraint.weight = Mathf.Clamp01(weight);
            // Debug.Log($"Head Look-At weight set to: {_headLookAtConstraint.weight}");
        }
    }

    /// <summary>
    /// Updates the target Transform for the left leg IK.
    /// This allows dynamic repositioning of the IK goal based on game events
    /// (e.g., character stepping on a dynamic object, path following).
    /// </summary>
    /// <param name="newTarget">The new Transform for the left leg IK to target.</param>
    public void SetLeftLegIKTarget(Transform newTarget)
    {
        if (_leftLegIKConstraint != null)
        {
            leftLegIKTarget = newTarget; // Update the public field for inspector visibility
            _leftLegIKConstraint.data.target = newTarget;
            // Debug.Log($"Left Leg IK target updated to: {newTarget?.name}");
        }
    }

    /// <summary>
    /// Updates the target Transform for the right leg IK.
    /// </summary>
    /// <param name="newTarget">The new Transform for the right leg IK to target.</param>
    public void SetRightLegIKTarget(Transform newTarget)
    {
        if (_rightLegIKConstraint != null)
        {
            rightLegIKTarget = newTarget; // Update the public field
            _rightLegIKConstraint.data.target = newTarget;
            // Debug.Log($"Right Leg IK target updated to: {newTarget?.name}");
        }
    }

    /// <summary>
    /// Updates the target Transform for the head look-at constraint.
    /// This allows the character to dynamically look at different objects (e.g., nearest enemy,
    /// quest giver, interactive item).
    /// </summary>
    /// <param name="newTarget">The new Transform for the head to look at.</param>
    public void SetHeadLookAtTarget(Transform newTarget)
    {
        if (_headLookAtConstraint != null)
        {
            headLookAtTarget = newTarget; // Update the public field
            
            // Update the existing source or add a new one if none exist.
            // This assumes we are always managing the first (and perhaps only) source.
            if (_headLookAtConstraint.data.sourceObjects.Count > 0)
            {
                WeightedTransform existingSource = _headLookAtConstraint.data.sourceObjects[0];
                existingSource.transform = newTarget; // Update the transform of the source
                _headLookAtConstraint.data.sourceObjects[0] = existingSource; // Reassign the updated source
            }
            else if (newTarget != null)
            {
                // If there were no sources, add this new target as the first one.
                _headLookAtConstraint.data.sourceObjects.Add(new WeightedTransform(newTarget, 1.0f));
            }
            // Debug.Log($"Head Look-At target updated to: {newTarget?.name}");
        }
    }

    /// <summary>
    /// Resets all rigging weights to their initial values that were stored during Awake.
    /// This can be useful for returning the character to a default rigging state.
    /// </summary>
    public void ResetAllWeights()
    {
        SetLeftLegIKWeight(_initialLeftLegIKWeight);
        SetRightLegIKWeight(_initialRightLegIKWeight);
        SetHeadLookAtWeight(_initialHeadLookAtWeight);
        Debug.Log("All rigging weights reset to initial values.");
    }

    // --- Example Usage / Interaction Methods ---
    // These methods show how other scripts might call into this RiggingSystemController
    // to dynamically manipulate the character's rigging.

    /// <summary>
    /// Toggles all current rigging effects (IK and Look-At) on or off.
    /// This is an example of dynamic activation/deactivation.
    /// Accessible via a Context Menu in the Inspector for quick testing.
    /// </summary>
    [ContextMenu("Test Toggle All Rigging")]
    private void TestToggleAllRigging()
    {
        // Check if any IK is currently active (weight > 0.5)
        if ((_leftLegIKConstraint != null && _leftLegIKConstraint.weight > 0.5f) ||
            (_rightLegIKConstraint != null && _rightLegIKConstraint.weight > 0.5f) ||
            (_headLookAtConstraint != null && _headLookAtConstraint.weight > 0.5f))
        {
            // If active, disable all
            SetLeftLegIKWeight(0.0f);
            SetRightLegIKWeight(0.0f);
            SetHeadLookAtWeight(0.0f);
            Debug.Log("All Rigging: OFF");
        }
        else
        {
            // If inactive, enable all (or revert to initial weights)
            SetLeftLegIKWeight(1.0f); // Or _initialLeftLegIKWeight
            SetRightLegIKWeight(1.0f); // Or _initialRightLegIKWeight
            SetHeadLookAtWeight(1.0f); // Or _initialHeadLookAtWeight
            Debug.Log("All Rigging: ON");
        }
    }

    /// <summary>
    /// Dynamically changes the head look-at target to a temporary new position.
    /// This demonstrates how a gameplay script (e.g., an AI targeting system)
    /// could tell the rigging system where to look.
    /// Accessible via a Context Menu in the Inspector for quick testing.
    /// </summary>
    [ContextMenu("Test Change Look-At Target")]
    private void TestChangeLookAtTarget()
    {
        // For demonstration, we'll create a temporary new GameObject as a target.
        // In a real project, this would typically be an existing enemy, item, or player.
        GameObject tempTargetGO = new GameObject("DynamicLookAtTarget");
        // Position it slightly offset from the character
        tempTargetGO.transform.position = transform.position + transform.forward * 4f + Vector3.up * 1.8f;
        
        SetHeadLookAtTarget(tempTargetGO.transform);
        Debug.Log($"Look-At target temporarily moved to a new position.");

        // In a real scenario, this temp target would likely be a persistent object
        // like an enemy, and you wouldn't destroy it here.
        StartCoroutine(DestroyAfterDelay(tempTargetGO, 5f)); // Clean up temp object after 5 seconds
    }

    /// <summary>
    /// Coroutine to destroy a GameObject after a specified delay.
    /// Used for cleaning up temporary test objects.
    /// </summary>
    private IEnumerator DestroyAfterDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null)
        {
            Destroy(go);
            Debug.Log($"Temporary Look-At target '{go.name}' destroyed.");
            // Reset to original target if it was destroyed
            if (_headLookAtConstraint != null && _headLookAtConstraint.data.sourceObjects.Count > 0 && 
                _headLookAtConstraint.data.sourceObjects[0].transform == go.transform)
            {
                SetHeadLookAtTarget(headLookAtTarget); // Revert to the original public target
            }
        }
    }

    // You could also add methods like:
    // public void ActivateCombatModeRigging() { ... }
    // public void ActivateInteractionRigging(Transform interactionPoint) { ... }
    // public void BlendWalkRunIK(float blendFactor) { ... }
}
```