// Unity Design Pattern Example: AIRagdollBlending
// This script demonstrates the AIRagdollBlending pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AIRagdollBlending' design pattern in Unity focuses on creating seamless transitions between a character's animated state and a physics-driven ragdoll state, often initiated by AI decisions, game events, or physics interactions. The "blending" part is crucial for avoiding jarring pops and making the transitions look natural.

This pattern is particularly useful for:
*   **Hit Reactions:** When a character is hit, they go ragdoll, then blend back to an upright animation.
*   **Falling/Stumbling:** AI characters might stumble, ragdoll, and then recover.
*   **Death Animations:** Characters can go ragdoll upon death, with a potential for a final, settled pose.

### Core Principles of AIRagdollBlending:

1.  **Ragdoll Activation:**
    *   Disable the `Animator` component.
    *   Disable any locomotion controllers (e.g., `CharacterController`, `NavMeshAgent`).
    *   Enable physics on all `Rigidbody` components in the ragdoll (set `isKinematic = false`).
    *   Enable all `Collider` components.
    *   This puts the character entirely under physics control.

2.  **Ragdoll Deactivation & Blending Back (The Tricky Part):**
    *   **Capture Ragdoll Pose:** Before disabling physics, record the current world position and rotation of *each bone* in the ragdoll. This is the target pose the animation needs to blend *from*.
    *   **Disable Ragdoll Physics:** Set `isKinematic = true` for all rigidbodies, disable all colliders.
    *   **Enable Animation & Locomotion:** Re-enable the `Animator`, `CharacterController`, `NavMeshAgent`.
    *   **Root Alignment:** The most important step for a smooth transition is to align the character's root transform (the GameObject that holds the `Animator`) with a key rigidbody of the ragdoll (e.g., the Hips or Spine). This ensures the whole character snaps to the ragdoll's starting position before blending.
    *   **Blending Loop (in `LateUpdate` or `OnAnimatorIK`):** Over a short duration, continuously interpolate the local position and rotation of each bone from its captured ragdoll pose towards its current animated pose. `LateUpdate` is often used as it runs after the `Animator` has evaluated its frame, allowing you to override or blend its output.

### Practical Unity Example: `AIRagdollBlender.cs`

This script demonstrates a robust implementation of the AIRagdollBlending pattern. It will manage the transition to and from a ragdoll state, including the crucial blending phase.

**Setup Instructions:**

1.  **Character with Animator:** You need a 3D character model with an `Animator` component and an Avatar configured.
2.  **Create Ragdoll:** Use Unity's `GameObject -> 3D Object -> Ragdoll Wizard` to create a ragdoll for your character. This will add `Rigidbody` and `Collider` components to the relevant bones.
3.  **Attach Script:**
    *   Create a new C# script named `AIRagdollBlender`.
    *   Copy and paste the code below into it.
    *   Attach this script to the root GameObject of your character (the one with the `Animator` component).
4.  **Assign Components:**
    *   In the Inspector, drag your character's `Animator` component into the `Animator` slot.
    *   The script will *automatically* find all `Rigidbody` and `Collider` components on `Awake()`.
    *   If you have a `CharacterController` or `NavMeshAgent`, make sure they are on the same root GameObject or a child that will be enabled/disabled with the main character, and the script will try to find and manage them.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // Required for NavMeshAgent

/// <summary>
/// Represents the current state of the character's ragdoll system.
/// </summary>
public enum RagdollState
{
    Animated,           // Character is fully animated, no ragdoll physics active.
    Ragdolling,         // Character is fully under physics control (ragdoll active).
    BlendingToAnimation // Character is transitioning from ragdoll back to animated, blending poses.
}

/// <summary>
/// Stores the world position and rotation of a bone's transform.
/// Used to capture the ragdoll's pose before blending back to animation.
/// </summary>
[System.Serializable]
public struct BoneTransformData
{
    public Transform boneTransform; // Reference to the actual bone's Transform component.
    public Vector3 position;        // World position of the bone.
    public Quaternion rotation;     // World rotation of the bone.

    public BoneTransformData(Transform transform)
    {
        boneTransform = transform;
        position = transform.position;
        rotation = transform.rotation;
    }
}

/// <summary>
/// AIRagdollBlender demonstrates the AIRagdollBlending design pattern in Unity.
/// It manages the transition between an animated character and a physics-driven ragdoll,
/// including a smooth blending phase when recovering from the ragdoll state.
/// </summary>
[RequireComponent(typeof(Animator))] // Ensures an Animator is present on the GameObject.
public class AIRagdollBlender : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The Animator component of the character.")]
    [SerializeField] private Animator _animator;

    [Tooltip("The main rigidbody of the ragdoll (e.g., Hips). Used for alignment.")]
    [SerializeField] private Rigidbody _mainRagdollRigidbody;

    [Header("Ragdoll Physics Settings")]
    [Tooltip("All Rigidbodies that make up the ragdoll. Automatically found if not assigned.")]
    [SerializeField] private Rigidbody[] _allRigidbodies;

    [Tooltip("All Colliders that make up the ragdoll. Automatically found if not assigned.")]
    [SerializeField] private Collider[] _allColliders;

    [Tooltip("List of all bone transforms to capture and blend. Automatically found if not assigned.")]
    [SerializeField] private List<BoneTransformData> _boneTransforms = new List<BoneTransformData>();

    [Header("Blending Settings")]
    [Tooltip("Duration in seconds for blending from ragdoll pose back to animation.")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float _blendDuration = 0.5f;

    [Tooltip("The layer for ragdoll colliders. Useful for ignoring specific interactions.")]
    [SerializeField] private int _ragdollLayer = 0; // Default or a specific ragdoll layer

    [Header("Locomotion Components (Optional)")]
    [Tooltip("Reference to the CharacterController. Will be disabled during ragdoll.")]
    [SerializeField] private CharacterController _characterController;

    [Tooltip("Reference to the NavMeshAgent. Will be disabled during ragdoll.")]
    [SerializeField] private NavMeshAgent _navMeshAgent;

    // Internal state variables
    private RagdollState _ragdollState = RagdollState.Animated;
    private float _blendTimer; // Tracks progress during the blending phase.

    /// <summary>
    /// Gets the current ragdoll state of the character.
    /// </summary>
    public RagdollState CurrentRagdollState => _ragdollState;

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Auto-assign Animator if not set
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("AIRagdollBlender requires an Animator component on this GameObject.", this);
                enabled = false;
                return;
            }
        }

        // Auto-assign CharacterController and NavMeshAgent if not set
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }
        if (_navMeshAgent == null)
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }

        // Auto-find all Rigidbodies, Colliders, and setup _boneTransforms list
        FindRagdollComponents();

        // Ensure ragdoll physics are initially disabled
        SetRagdollPhysics(false);
    }

    private void Update()
    {
        // Handle the blending process if currently blending
        if (_ragdollState == RagdollState.BlendingToAnimation)
        {
            _blendTimer += Time.deltaTime;
            if (_blendTimer >= _blendDuration)
            {
                // Blending complete, transition to Animated state
                _ragdollState = RagdollState.Animated;
                Debug.Log($"[{gameObject.name}] Ragdoll blending complete. Back to Animated.", this);
                // Clear captured pose data (not strictly necessary as we overwrite, but good for memory)
                _boneTransforms.Clear();
            }
        }
    }

    private void LateUpdate()
    {
        // Only perform pose blending when transitioning back to animation
        if (_ragdollState == RagdollState.BlendingToAnimation)
        {
            // Calculate the blend factor: 0 = fully animated, 1 = fully ragdoll pose
            float blendFactor = 1f - Mathf.Clamp01(_blendTimer / _blendDuration);

            // Iterate through each bone and blend its transform
            foreach (var boneData in _boneTransforms)
            {
                if (boneData.boneTransform != null)
                {
                    // Blend local position and local rotation
                    // We need to calculate the target local position/rotation based on the stored world data
                    // relative to the bone's parent.

                    // Get the current animated local position and rotation
                    Vector3 animatedLocalPosition = boneData.boneTransform.localPosition;
                    Quaternion animatedLocalRotation = boneData.boneTransform.localRotation;

                    // Calculate the local position and rotation from the captured world position/rotation
                    // This is crucial: we want to blend from the *ragdoll's local pose* to the *animator's local pose*.
                    Vector3 ragdollLocalPosition = boneData.boneTransform.parent != null ?
                                                   boneData.boneTransform.parent.InverseTransformPoint(boneData.position) :
                                                   boneData.position;
                    Quaternion ragdollLocalRotation = boneData.boneTransform.parent != null ?
                                                      Quaternion.Inverse(boneData.boneTransform.parent.rotation) * boneData.rotation :
                                                      boneData.rotation;


                    // Interpolate between the ragdoll's local pose and the animator's local pose
                    boneData.boneTransform.localPosition = Vector3.Lerp(animatedLocalPosition, ragdollLocalPosition, blendFactor);
                    boneData.boneTransform.localRotation = Quaternion.Slerp(animatedLocalRotation, ragdollLocalRotation, blendFactor);
                }
            }
        }
    }

    // --- Public API Methods ---

    /// <summary>
    /// Activates the ragdoll state, making the character physics-driven.
    /// </summary>
    public void GoRagdoll(Vector3 hitForce = default, Vector3 hitPoint = default)
    {
        if (_ragdollState == RagdollState.Ragdolling)
        {
            Debug.Log($"[{gameObject.name}] Already in ragdoll state. Ignoring GoRagdoll call.", this);
            return;
        }

        Debug.Log($"[{gameObject.name}] Going Ragdoll!", this);
        _ragdollState = RagdollState.Ragdolling;

        // 1. Disable Animator and locomotion components
        _animator.enabled = false;
        if (_characterController != null) _characterController.enabled = false;
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;

        // 2. Enable ragdoll physics
        SetRagdollPhysics(true);

        // 3. Apply optional force to the main rigidbody
        if (hitForce != default && _mainRagdollRigidbody != null)
        {
            _mainRagdollRigidbody.AddForceAtPosition(hitForce, hitPoint, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Initiates the process of getting the character up from a ragdoll state,
    /// blending back to the animated state.
    /// </summary>
    public void GetUp()
    {
        if (_ragdollState == RagdollState.Animated || _ragdollState == RagdollState.BlendingToAnimation)
        {
            Debug.Log($"[{gameObject.name}] Not in ragdoll state or already blending. Ignoring GetUp call.", this);
            return;
        }

        Debug.Log($"[{gameObject.name}] Getting Up from Ragdoll, blending back to animation.", this);
        _ragdollState = RagdollState.BlendingToAnimation;
        _blendTimer = 0f; // Reset blend timer for a new blend cycle

        // 1. Capture the current ragdoll pose
        CaptureRagdollPose();

        // 2. Align the character's root transform with the main ragdoll rigidbody
        // This is crucial to prevent the entire character from "popping" to a new location.
        if (_mainRagdollRigidbody != null)
        {
            transform.position = _mainRagdollRigidbody.position;
            transform.rotation = _mainRagdollRigidbody.rotation;
        }

        // 3. Disable ragdoll physics (kinematic, no collision)
        SetRagdollPhysics(false);

        // 4. Re-enable Animator and locomotion components
        _animator.enabled = true;
        if (_characterController != null) _characterController.enabled = true;
        if (_navMeshAgent != null) _navMeshAgent.enabled = true;
    }

    // --- Internal Helper Methods ---

    /// <summary>
    /// Finds all Rigidbody, Collider, and relevant Transforms within the character hierarchy.
    /// </summary>
    private void FindRagdollComponents()
    {
        _allRigidbodies = GetComponentsInChildren<Rigidbody>();
        _allColliders = GetComponentsInChildren<Collider>();

        // Populate _boneTransforms list with all transforms that have a Rigidbody (i.e., ragdoll bones)
        _boneTransforms.Clear();
        foreach (var rb in _allRigidbodies)
        {
            _boneTransforms.Add(new BoneTransformData(rb.transform));
            if (_mainRagdollRigidbody == null && rb.name.ToLower().Contains("hip")) // Common heuristic for main rigidbody
            {
                _mainRagdollRigidbody = rb;
            }
        }

        if (_mainRagdollRigidbody == null && _allRigidbodies.Length > 0)
        {
            _mainRagdollRigidbody = _allRigidbodies[0]; // Fallback to the first found rigidbody
            Debug.LogWarning($"[{gameObject.name}] No main ragdoll rigidbody (e.g., 'Hips') found. Using '{_mainRagdollRigidbody.name}' as fallback. Consider assigning it manually.", this);
        }

        // Set all ragdoll colliders to the specified layer
        foreach (var col in _allColliders)
        {
            col.gameObject.layer = _ragdollLayer;
        }

        Debug.Log($"[{gameObject.name}] Found {_allRigidbodies.Length} Rigidbodies, {_allColliders.Length} Colliders for ragdoll.", this);
    }

    /// <summary>
    /// Enables or disables physics interaction for all ragdoll components.
    /// </summary>
    /// <param name="enablePhysics">True to enable physics (ragdoll), false to disable (animated).</param>
    private void SetRagdollPhysics(bool enablePhysics)
    {
        foreach (var rb in _allRigidbodies)
        {
            if (rb != null) // Check for null in case a bone was deleted
            {
                rb.isKinematic = !enablePhysics; // If physics enabled, not kinematic. If disabled, is kinematic.
                rb.useGravity = enablePhysics;
            }
        }

        foreach (var col in _allColliders)
        {
            if (col != null)
            {
                col.enabled = enablePhysics;
            }
        }
    }

    /// <summary>
    /// Captures the current world position and rotation of each ragdoll bone.
    /// This is used as the starting point for blending back to animation.
    /// </summary>
    private void CaptureRagdollPose()
    {
        // Re-populate and update the data with current world transforms
        for (int i = 0; i < _boneTransforms.Count; i++)
        {
            if (_boneTransforms[i].boneTransform != null)
            {
                _boneTransforms[i] = new BoneTransformData(_boneTransforms[i].boneTransform);
            }
            else
            {
                // Remove null entries if bones were deleted dynamically
                _boneTransforms.RemoveAt(i);
                i--;
            }
        }
    }


    // --- Example Usage (Commented Out) ---
    /*
    // Example of how another script (e.g., a PlayerController or EnemyAI) would use this.
    // Attach this 'AIRagdollBlender' script to your character GameObject.

    public class ExampleCharacterController : MonoBehaviour
    {
        private AIRagdollBlender _ragdollBlender;
        private HealthSystem _healthSystem; // Assume you have a health system

        void Start()
        {
            _ragdollBlender = GetComponent<AIRagdollBlender>();
            _healthSystem = GetComponent<HealthSystem>(); // Or whatever system triggers ragdoll/getup

            if (_ragdollBlender == null)
            {
                Debug.LogError("AIRagdollBlender not found on this GameObject!", this);
                enabled = false;
            }

            // Example: Subscribe to a health event
            // _healthSystem.OnCharacterDamaged += HandleDamage;
            // _healthSystem.OnCharacterDied += HandleDeath;
        }

        void Update()
        {
            // Example: Manual trigger for testing
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_ragdollBlender.CurrentRagdollState == RagdollState.Animated)
                {
                    _ragdollBlender.GoRagdoll(new Vector3(0, 500, 500), transform.position + transform.forward); // Apply an upward and forward force
                }
                else if (_ragdollBlender.CurrentRagdollState == RagdollState.Ragdolling)
                {
                    _ragdollBlender.GetUp();
                }
            }

            // Example: AI decision to get up after some time
            if (_ragdollBlender.CurrentRagdollState == RagdollState.Ragdolling)
            {
                // After 3 seconds, try to get up
                if (Time.time > _ragdollBlender.LastRagdollTime + 3f) // (You'd need a 'LastRagdollTime' property in AIRagdollBlender)
                {
                    // _ragdollBlender.GetUp();
                }
            }
        }

        // Example: Handle damage event
        // void HandleDamage(float damageAmount, Vector3 hitPoint, Vector3 hitDirection)
        // {
        //     if (_ragdollBlender.CurrentRagdollState == RagdollState.Animated && Random.value < 0.5f) // 50% chance to ragdoll on hit
        //     {
        //         _ragdollBlender.GoRagdoll(hitDirection.normalized * 500f + Vector3.up * 200f, hitPoint);
        //     }
        // }

        // Example: Handle death event
        // void HandleDeath()
        // {
        //     if (_ragdollBlender.CurrentRagdollState != RagdollState.Ragdolling)
        //     {
        //         _ragdollBlender.GoRagdoll(Vector3.zero, Vector3.zero); // Just drop dead, no force
        //     }
        //     // Maybe disable character logic permanently here
        // }
    }
    */
}
```