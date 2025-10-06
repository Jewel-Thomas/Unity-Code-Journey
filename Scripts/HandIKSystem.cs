// Unity Design Pattern Example: HandIKSystem
// This script demonstrates the HandIKSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **HandIKSystem** design pattern in Unity. While not a classic GoF pattern, it represents a common architectural solution for managing Inverse Kinematics (IK) for character hands in a modular, reusable, and extendable way.

**The HandIKSystem Pattern's Core Ideas:**

1.  **Centralized Control:** A single component (`HandIKSystem`) is responsible for all hand IK logic, preventing scattering of IK code across multiple scripts.
2.  **Target-Driven:** Other systems (e.g., a weapon script, an object interaction script) provide IK targets (positions/rotations) to the `HandIKSystem`.
3.  **Weighting & Blending:** It manages the blend between animation and IK, allowing smooth transitions into and out of IK control.
4.  **Abstraction:** It hides the complexities of Unity's `OnAnimatorIK` callback and direct IK manipulation from client scripts. Client scripts only need to request IK for a hand with a target.
5.  **State Management:** It maintains the current state (target, weight, blend speed) for each hand independently.

---

### HandIKSystem.cs

This script should be attached to your character's root GameObject, which also has an `Animator` component.

```csharp
using UnityEngine;
using System; // For Action

/// <summary>
/// Defines the type of hand for IK operations.
/// </summary>
public enum HandType
{
    Left,
    Right
}

/// <summary>
/// A data class to hold all necessary IK information for a single hand.
/// This encapsulates the state of one hand's IK.
/// </summary>
[System.Serializable] // Make it visible in the Inspector for debugging if needed
public class HandIKData
{
    [Tooltip("The target Transform the hand should try to reach.")]
    public Transform target;

    [Tooltip("Offset applied to the target's position.")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Offset applied to the target's rotation.")]
    public Quaternion rotationOffset = Quaternion.identity;

    [Tooltip("The current IK weight for this hand (0 = animation, 1 = full IK).")]
    [Range(0f, 1f)]
    public float currentWeight = 0f;

    [Tooltip("The desired IK weight for this hand. The system will blend towards this.")]
    [Range(0f, 1f)]
    public float targetWeight = 0f;

    [Tooltip("Speed at which the currentWeight blends towards targetWeight.")]
    public float blendSpeed = 5f;

    // Internal state: True if an IK target has been set and IK is active
    public bool isActive = false;

    /// <summary>
    /// Updates the current IK weight towards the target weight.
    /// </summary>
    /// <param name="deltaTime">The time since the last frame.</param>
    public void UpdateWeight(float deltaTime)
    {
        if (isActive)
        {
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, blendSpeed * deltaTime);
        }
        else // If IK is not active, blend out to 0
        {
            currentWeight = Mathf.MoveTowards(currentWeight, 0f, blendSpeed * deltaTime);
        }
    }

    /// <summary>
    /// Prepares the HandIKData for a new IK target.
    /// </summary>
    /// <param name="newTarget">The new transform to track.</param>
    /// <param name="posOffset">Position offset from the target.</param>
    /// <param name="rotOffset">Rotation offset from the target.</param>
    /// <param name="weight">The desired target weight (usually 1 for active IK).</param>
    /// <param name="speed">The blend speed for this IK request.</param>
    public void SetTarget(Transform newTarget, Vector3 posOffset, Quaternion rotOffset, float weight, float speed)
    {
        target = newTarget;
        positionOffset = posOffset;
        rotationOffset = rotOffset;
        targetWeight = weight;
        blendSpeed = speed;
        isActive = true;
    }

    /// <summary>
    /// Resets the HandIKData, blending out IK control.
    /// </summary>
    /// <param name="speed">The blend speed for releasing IK.</param>
    public void Release(float speed)
    {
        target = null; // No longer tracking a specific target
        positionOffset = Vector3.zero;
        rotationOffset = Quaternion.identity;
        targetWeight = 0f; // Blend out
        blendSpeed = speed;
        isActive = false; // Mark as inactive, but continue blending out
    }
}

/// <summary>
/// HandIKSystem: A centralized system for managing Inverse Kinematics for character hands.
/// This script should be attached to the root GameObject of a character with an Animator component.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HandIKSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The Animator component on this GameObject.")]
    private Animator _animator;

    [Header("IK Settings")]
    [Tooltip("Data for the left hand's IK.")]
    [SerializeField] private HandIKData _leftHandIKData = new HandIKData();
    [Tooltip("Data for the right hand's IK.")]
    [SerializeField] private HandIKData _rightHandIKData = new HandIKData();

    // Private helper to get the HandIKData for a given hand type.
    private HandIKData GetHandData(HandType hand)
    {
        return hand == HandType.Left ? _leftHandIKData : _rightHandIKData;
    }

    // Unity Lifecycle Method: Called when the script instance is being loaded.
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("HandIKSystem requires an Animator component on this GameObject.", this);
            enabled = false; // Disable the script if no Animator is found
        }
    }

    // Unity Lifecycle Method: Called every frame.
    private void Update()
    {
        // Update the current blend weight for both hands.
        _leftHandIKData.UpdateWeight(Time.deltaTime);
        _rightHandIKData.UpdateWeight(Time.deltaTime);
    }

    /// <summary>
    /// Unity's built-in callback for applying IK. This is where the IK magic happens.
    /// It's called after the Animator updates and before the final frame rendering.
    /// </summary>
    /// <param name="layerIndex">The current animation layer index.</param>
    private void OnAnimatorIK(int layerIndex)
    {
        ApplyHandIK(_leftHandIKData, AvatarIKGoal.LeftHand);
        ApplyHandIK(_rightHandIKData, AvatarIKGoal.RightHand);
    }

    /// <summary>
    /// Applies IK to a specific hand based on its IK data.
    /// </summary>
    /// <param name="handData">The IK data for the hand.</param>
    /// <param name="ikGoal">The AvatarIKGoal enum for the specific hand.</param>
    private void ApplyHandIK(HandIKData handData, AvatarIKGoal ikGoal)
    {
        // Only apply IK if there's a target and the current weight is greater than a minimal threshold.
        // We check target != null AND handData.isActive to ensure we only apply IK when explicitly requested
        // and not just when blending out (where isActive would be false but currentWeight might still be > 0).
        if (handData.target != null && handData.currentWeight > 0.01f && handData.isActive)
        {
            // Set the IK position and rotation weights.
            _animator.SetIKPositionWeight(ikGoal, handData.currentWeight);
            _animator.SetIKRotationWeight(ikGoal, handData.currentWeight);

            // Calculate the target position and rotation, applying offsets.
            Vector3 targetPosition = handData.target.position + handData.target.TransformDirection(handData.positionOffset);
            Quaternion targetRotation = handData.target.rotation * handData.rotationOffset;

            // Set the IK position and rotation.
            _animator.SetIKPosition(ikGoal, targetPosition);
            _animator.SetIKRotation(ikGoal, targetRotation);
        }
        else
        {
            // If no target or weight is negligible, disable IK for this hand.
            // It's important to set weights to 0 to fully release IK control.
            _animator.SetIKPositionWeight(ikGoal, handData.currentWeight);
            _animator.SetIKRotationWeight(ikGoal, handData.currentWeight);
        }
    }

    /// <summary>
    /// PUBLIC API: Sets an IK target for a specific hand and blends its weight in.
    /// This is the primary method other systems will call to request IK control.
    /// </summary>
    /// <param name="hand">The hand (Left or Right) to apply IK to.</param>
    /// <param name="targetTransform">The Transform the hand should reach.</param>
    /// <param name="positionOffset">Local offset from the target's position.</param>
    /// <param name="rotationOffset">Local offset from the target's rotation.</param>
    /// <param name="blendSpeed">Speed at which to blend into IK (defaults to 5).</param>
    public void SetIKTarget(HandType hand, Transform targetTransform,
                            Vector3 positionOffset = default, Quaternion rotationOffset = default,
                            float blendSpeed = 5f)
    {
        HandIKData handData = GetHandData(hand);
        handData.SetTarget(targetTransform, positionOffset, rotationOffset, 1f, blendSpeed); // Target weight is 1 for full IK
    }

    /// <summary>
    /// PUBLIC API: Releases IK control for a specific hand, blending its weight out.
    /// </summary>
    /// <param name="hand">The hand (Left or Right) to release IK control from.</param>
    /// <param name="blendSpeed">Speed at which to blend out of IK (defaults to 5).</param>
    public void ReleaseIK(HandType hand, float blendSpeed = 5f)
    {
        HandIKData handData = GetHandData(hand);
        handData.Release(blendSpeed);
    }

    /// <summary>
    /// PUBLIC API: Directly sets the target IK weight for a specific hand.
    /// Useful for partial IK blending or debugging.
    /// </summary>
    /// <param name="hand">The hand (Left or Right) to adjust the weight for.</param>
    /// <param name="weight">The desired target weight (0 to 1).</param>
    /// <param name="blendSpeed">Speed at which to blend to the new weight (defaults to 5).</param>
    public void SetIKWeight(HandType hand, float weight, float blendSpeed = 5f)
    {
        HandIKData handData = GetHandData(hand);
        // Ensure that if weight is > 0, we set isActive to true, otherwise false
        handData.isActive = (weight > 0.01f);
        handData.targetWeight = Mathf.Clamp01(weight);
        handData.blendSpeed = blendSpeed;
        
        // If we are setting a weight > 0 but have no target, warn the user.
        // For partial IK, a target might not be needed, but for full IK it is.
        if (handData.isActive && handData.target == null)
        {
             Debug.LogWarning($"HandIKSystem: Setting IK weight to {weight} for {hand} hand, but no target Transform is assigned. " +
                              "IK will likely not function as expected for position/rotation if a target is required.", this);
        }
    }

    /// <summary>
    /// PUBLIC API: Gets the current IK weight of a specific hand.
    /// </summary>
    /// <param name="hand">The hand (Left or Right).</param>
    /// <returns>The current IK blend weight (0 to 1).</returns>
    public float GetIKWeight(HandType hand)
    {
        return GetHandData(hand).currentWeight;
    }

    /// <summary>
    /// PUBLIC API: Checks if a specific hand is currently actively managed by IK (i.e., has a target and blending in/out).
    /// </summary>
    /// <param name="hand">The hand (Left or Right).</param>
    /// <returns>True if IK is active for the hand, false otherwise.</returns>
    public bool IsIKActive(HandType hand)
    {
        return GetHandData(hand).isActive || GetHandData(hand).currentWeight > 0.01f;
    }
}
```

---

### How to Use the `HandIKSystem` (Example Usage)

Here's an example of how another script (e.g., a `WeaponHolder` or `ObjectInteractor`) would interact with the `HandIKSystem`.

1.  **Setup your Character:**
    *   Create a character model.
    *   Ensure it has an `Animator` component with an Avatar configured.
    *   Add the `HandIKSystem` script to the character's root GameObject.
    *   Make sure your Animator's Culling Mode is not set to `Always Animate` if you want `OnAnimatorIK` to be called when off-screen. `Cull Update Transforms` or `Cull Completely` work fine.
    *   In your Animator Controller, ensure the IK Pass is enabled for the relevant layers. You can right-click any layer, go to Layer Settings, and check "IK Pass".

2.  **Create IK Targets:**
    *   For the example below, create two empty GameObjects: `LeftHandTarget` and `RightHandTarget` as children of your character or any suitable parent. These will serve as the points your character's hands will try to reach. Adjust their positions to where you want the hands to be.

3.  **Example Client Script (`IKHandTest.cs`):**

```csharp
using UnityEngine;

/// <summary>
/// This is an example client script that demonstrates how to interact with the HandIKSystem.
/// It might represent a WeaponHolder, an ObjectInteraction script, or a climbing system.
/// </summary>
public class IKHandTest : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the HandIKSystem on the character.")]
    [SerializeField] private HandIKSystem _handIKSystem;

    [Header("Left Hand IK Target")]
    [Tooltip("The Transform the character's LEFT hand should try to reach.")]
    [SerializeField] private Transform _leftHandTarget;
    [Tooltip("Offset for the left hand's position relative to its target.")]
    [SerializeField] private Vector3 _leftHandPosOffset = new Vector3(0, 0, 0);
    [Tooltip("Offset for the left hand's rotation relative to its target (Euler angles).")]
    [SerializeField] private Vector3 _leftHandRotOffset = new Vector3(0, 0, 0);

    [Header("Right Hand IK Target")]
    [Tooltip("The Transform the character's RIGHT hand should try to reach.")]
    [SerializeField] private Transform _rightHandTarget;
    [Tooltip("Offset for the right hand's position relative to its target.")]
    [SerializeField] private Vector3 _rightHandPosOffset = new Vector3(0, 0, 0);
    [Tooltip("Offset for the right hand's rotation relative to its target (Euler angles).")]
    [SerializeField] private Vector3 _rightHandRotOffset = new Vector3(0, 0, 0);

    [Header("IK Control")]
    [Tooltip("Toggle to activate/deactivate left hand IK.")]
    [SerializeField] private bool _useLeftHandIK = false;
    [Tooltip("Toggle to activate/deactivate right hand IK.")]
    [SerializeField] private bool _useRightHandIK = false;
    [Tooltip("Speed for blending IK in and out.")]
    [SerializeField] private float _blendSpeed = 5f;

    private void Start()
    {
        if (_handIKSystem == null)
        {
            Debug.LogError("IKHandTest: HandIKSystem reference is not set! Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        // Initialize with current toggle states
        UpdateIKBasedOnToggles();
    }

    private void Update()
    {
        // Example: Toggling IK on/off with keyboard input
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _useLeftHandIK = !_useLeftHandIK;
            Debug.Log($"Left Hand IK Toggled: {_useLeftHandIK}");
            UpdateIKBasedOnToggles();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _useRightHandIK = !_useRightHandIK;
            Debug.Log($"Right Hand IK Toggled: {_useRightHandIK}");
            UpdateIKBasedOnToggles();
        }
        
        // You could also constantly update targets if they move, e.g., for a weapon
        // if (_useLeftHandIK)
        // {
        //     _handIKSystem.SetIKTarget(HandType.Left, _leftHandTarget, _leftHandPosOffset, Quaternion.Euler(_leftHandRotOffset), _blendSpeed);
        // }
    }

    /// <summary>
    /// Applies or releases IK based on the current toggle states.
    /// </summary>
    private void UpdateIKBasedOnToggles()
    {
        // Left Hand IK
        if (_useLeftHandIK && _leftHandTarget != null)
        {
            // Request IK for the left hand, providing the target and desired offsets.
            // The HandIKSystem will handle blending the weight in.
            _handIKSystem.SetIKTarget(HandType.Left, _leftHandTarget,
                                      _leftHandPosOffset, Quaternion.Euler(_leftHandRotOffset),
                                      _blendSpeed);
        }
        else
        {
            // Release IK for the left hand. The HandIKSystem will blend the weight out.
            _handIKSystem.ReleaseIK(HandType.Left, _blendSpeed);
        }

        // Right Hand IK
        if (_useRightHandIK && _rightHandTarget != null)
        {
            // Request IK for the right hand.
            _handIKSystem.SetIKTarget(HandType.Right, _rightHandTarget,
                                      _rightHandPosOffset, Quaternion.Euler(_rightHandRotOffset),
                                      _blendSpeed);
        }
        else
        {
            // Release IK for the right hand.
            _handIKSystem.ReleaseIK(HandType.Right, _blendSpeed);
        }
    }

    // You could also add gizmos to visualize the target positions in the editor
    private void OnDrawGizmos()
    {
        if (_leftHandTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_leftHandTarget.position + _leftHandTarget.TransformDirection(_leftHandPosOffset), 0.03f);
            Gizmos.DrawCube(_leftHandTarget.position + _leftHandTarget.TransformDirection(_leftHandPosOffset), Vector3.one * 0.02f);
        }
        if (_rightHandTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_rightHandTarget.position + _rightHandTarget.TransformDirection(_rightHandPosOffset), 0.03f);
            Gizmos.DrawCube(_rightHandTarget.position + _rightHandTarget.TransformDirection(_rightHandPosOffset), Vector3.one * 0.02f);
        }
    }
}
```

---

### Setup Steps in Unity:

1.  **Create a 3D Character:** Import a humanoid character model (e.g., from Mixamo, Unity Standard Assets, or your own). Ensure it has a properly configured Humanoid Avatar.
2.  **Add Animator:** Make sure the character's root GameObject has an `Animator` component with your character's `Avatar` assigned and a basic `Animator Controller` (even an empty one, or one with a simple idle animation).
3.  **Enable IK Pass:** In your `Animator Controller`, select the Base Layer (or any layer you want IK to affect). In the Inspector, open the Layer Settings (click the gear icon), and check the "IK Pass" checkbox.
4.  **Attach `HandIKSystem`:** Drag and drop the `HandIKSystem.cs` script onto the character's root GameObject (the one with the `Animator`).
5.  **Create IK Targets:**
    *   Create an empty GameObject named "LeftHandIKTarget" (e.g., as a child of your character's hip/root bone for easy positioning).
    *   Create another empty GameObject named "RightHandIKTarget".
    *   Position these targets in your scene where you want the character's hands to go.
6.  **Attach `IKHandTest`:** Create an empty GameObject in your scene (or attach to the character if you prefer). Drag and drop the `IKHandTest.cs` script onto it.
7.  **Configure `IKHandTest` in Inspector:**
    *   Drag your character's GameObject (the one with `HandIKSystem`) to the `Hand IK System` field.
    *   Drag the "LeftHandIKTarget" GameObject to the `Left Hand IK Target` field.
    *   Drag the "RightHandIKTarget" GameObject to the `Right Hand IK Target` field.
    *   Adjust `Left Hand Pos Offset`, `Left Hand Rot Offset`, `Right Hand Pos Offset`, `Right Hand Rot Offset` as needed to fine-tune the hand's exact position and rotation relative to its target. These are local offsets to the target.
    *   Toggle `Use Left Hand IK` and `Use Right Hand IK` to see the hands blend towards their targets.
    *   Press '1' and '2' in Play Mode to toggle IK for the left and right hands respectively.

Now, when you run the scene, your character's hands should blend towards the target transforms when IK is activated. If the targets move, the hands will follow!