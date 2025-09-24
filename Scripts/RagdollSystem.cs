// Unity Design Pattern Example: RagdollSystem
// This script demonstrates the RagdollSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `RagdollSystem` pattern in Unity is about creating a centralized manager to seamlessly switch a character between an animation-driven state and a physics-driven (ragdoll) state. This is a common requirement for game characters that need to react realistically to impacts, falls, or death.

This script demonstrates the pattern by:
1.  **Centralized Control:** A single script component (`RagdollSystem`) manages all relevant `Rigidbody` and `Collider` components, as well as the main `Animator` and `CharacterController`.
2.  **State Management:** It provides clear methods (`EnableRagdoll`, `DisableRagdoll`) to transition between the two states.
3.  **Component Awareness:** It automatically finds and caches all necessary physics components in the character's hierarchy.
4.  **Graceful Transition:** It handles enabling/disabling rigidbodies, colliders, and the animator to prevent conflicts and ensure the correct behavior for each state.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List

/// <summary>
/// RagdollSystem: A centralized manager for switching a character between an
/// animation-driven state and a physics-driven (ragdoll) state.
///
/// This script embodies the 'RagdollSystem' pattern by providing a unified
/// interface to control the complex interplay of Animator, CharacterController,
/// Rigidbodies, and Colliders that make up a character's physics and animation.
///
/// When the ragdoll is enabled, the Animator and primary character controllers
/// are disabled, and individual bone Rigidbodies and Colliders take over.
/// When disabled, the character reverts to animation control.
/// </summary>
public class RagdollSystem : MonoBehaviour
{
    // --- Public Fields for Inspector Configuration ---

    [Tooltip("The root bone of the ragdoll hierarchy (e.g., Hips). All Rigidbodies and Colliders " +
             "in its children will be managed by this system.")]
    [SerializeField]
    private Transform _ragdollRootBone;

    [Tooltip("The main Animator component on this GameObject or a child. It will be disabled " +
             "when the ragdoll is active and re-enabled when it's deactivated.")]
    [SerializeField]
    private Animator _animator;

    [Tooltip("The main CharacterController on this GameObject (if any). It will be disabled " +
             "when the ragdoll is active to prevent conflicts with bone physics.")]
    [SerializeField]
    private CharacterController _characterController;

    [Tooltip("Optional: A main Collider on this GameObject (e.g., a CapsuleCollider used for " +
             "general character collision). It will be disabled when ragdolling.")]
    [SerializeField]
    private Collider _mainCollider;

    // --- Private Internal State ---

    // Lists to hold all Rigidbody and Collider components that make up the ragdoll.
    // Storing these references prevents repeated GetComponent calls, improving performance.
    private List<Rigidbody> _ragdollRigidbodies = new List<Rigidbody>();
    private List<Collider> _ragdollColliders = new List<Collider>();

    // Stores the initial 'isKinematic' state of each ragdoll rigidbody.
    // This allows us to revert them to their original state (typically true for animation-driven)
    // when the ragdoll is disabled.
    private List<bool> _initialRigidbodyKinematicStates = new List<bool>();

    // Public property to query the current state of the ragdoll.
    public bool IsRagdollActive { get; private set; }

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to initialize the ragdoll components before any other scripts might
    /// try to interact with this system.
    /// </summary>
    private void Awake()
    {
        InitializeRagdollComponents();
        // Ensure the ragdoll starts in the disabled state (animation-driven) by default.
        DisableRagdoll();
    }

    // --- RagdollSystem Pattern Core Logic ---

    /// <summary>
    /// Initializes the RagdollSystem by finding and caching all relevant components.
    /// This method is the core of setting up the 'system' aspect of the pattern,
    /// preparing it to manage the transitions.
    /// </summary>
    private void InitializeRagdollComponents()
    {
        // Essential check: The root bone is critical for finding ragdoll parts.
        if (_ragdollRootBone == null)
        {
            Debug.LogError("RagdollSystem: Ragdoll Root Bone not assigned. Please assign the Hips bone or equivalent.", this);
            enabled = false; // Disable the script if it cannot function.
            return;
        }

        // Auto-assign Animator if not set in Inspector.
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning("RagdollSystem: Animator not assigned and not found on this GameObject.", this);
            }
        }

        // Auto-assign CharacterController if not set in Inspector.
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }

        // Auto-assign Main Collider if not set in Inspector.
        if (_mainCollider == null)
        {
            _mainCollider = GetComponent<Collider>();
        }

        // Clear previous lists to ensure a fresh initialization.
        _ragdollRigidbodies.Clear();
        _ragdollColliders.Clear();
        _initialRigidbodyKinematicStates.Clear();

        // 1. Collect Rigidbody and Collider on the root bone itself.
        Rigidbody rootRb = _ragdollRootBone.GetComponent<Rigidbody>();
        Collider rootCol = _ragdollRootBone.GetComponent<Collider>();

        // Exclude CharacterController type rigidbodies as they are managed separately.
        if (rootRb != null && !(rootRb is CharacterController))
        {
            _ragdollRigidbodies.Add(rootRb);
            _initialRigidbodyKinematicStates.Add(rootRb.isKinematic);
        }
        // Exclude CharacterController type colliders.
        if (rootCol != null && !(rootCol is CharacterController))
        {
            _ragdollColliders.Add(rootCol);
        }

        // 2. Collect all Rigidbodies and Colliders from the children of the ragdoll root bone.
        // This ensures we only manage components directly associated with the ragdoll bones.
        foreach (Rigidbody rb in _ragdollRootBone.GetComponentsInChildren<Rigidbody>(true))
        {
            // Avoid adding the root Rigidbody twice and exclude CharacterController.
            if (rb != rootRb && !(rb is CharacterController))
            {
                _ragdollRigidbodies.Add(rb);
                _initialRigidbodyKinematicStates.Add(rb.isKinematic);
            }
        }

        foreach (Collider col in _ragdollRootBone.GetComponentsInChildren<Collider>(true))
        {
            // Avoid adding the root Collider twice and exclude CharacterController/MainCollider.
            if (col != rootCol && col != _mainCollider && !(col is CharacterController))
            {
                _ragdollColliders.Add(col);
            }
        }

        // After collecting all components, ensure they are in the default (animation-driven) state.
        // This means rigidbodies are kinematic and colliders are disabled.
        SetRagdollPhysicsState(false);
        SetRagdollColliderState(false);

        Debug.Log($"RagdollSystem: Initialized with {_ragdollRigidbodies.Count} rigidbodies and {_ragdollColliders.Count} colliders.", this);
    }

    /// <summary>
    /// Enables the ragdoll physics, effectively transitioning the character from
    /// animation control to physics control. The Animator and primary character
    /// controllers are disabled, and individual bone Rigidbodies and Colliders are activated.
    /// </summary>
    /// <param name="initialForce">An optional force to apply to the ragdoll's root rigidbody upon activation.</param>
    /// <param name="forcePoint">The world space point to apply the force. If null, force is applied at center of mass.</param>
    /// <param name="forceMode">The mode for applying the force (Impulse, Force, VelocityChange, Acceleration).</param>
    public void EnableRagdoll(Vector3 initialForce = default, Vector3? forcePoint = null, ForceMode forceMode = ForceMode.Impulse)
    {
        // Prevent re-enabling if already active.
        if (IsRagdollActive) return;

        Debug.Log("RagdollSystem: Enabling Ragdoll.", this);

        // 1. Disable the Animator component to stop animation playback.
        if (_animator != null)
        {
            _animator.enabled = false;
        }

        // 2. Disable the main character controller and/or main collider to avoid conflicts
        // with the individual ragdoll bone colliders.
        if (_characterController != null)
        {
            _characterController.enabled = false;
        }
        if (_mainCollider != null)
        {
            _mainCollider.enabled = false;
        }

        // 3. Enable ragdoll physics: Make all ragdoll rigidbodies non-kinematic
        // so they respond to forces and collisions.
        SetRagdollPhysicsState(true);

        // 4. Enable ragdoll colliders so they can detect collisions.
        SetRagdollColliderState(true);

        // 5. Apply an initial force to the root rigidbody if provided, simulating an impact.
        if (initialForce != default && _ragdollRootBone.TryGetComponent<Rigidbody>(out Rigidbody rootRigidbody))
        {
            if (forcePoint.HasValue)
            {
                rootRigidbody.AddForceAtPosition(initialForce, forcePoint.Value, forceMode);
            }
            else
            {
                rootRigidbody.AddForce(initialForce, forceMode);
            }
        }

        IsRagdollActive = true;
    }

    /// <summary>
    /// Disables the ragdoll physics, transitioning the character back to animation control.
    /// The Animator and primary character controllers are re-enabled, and ragdoll
    /// Rigidbodies become kinematic again, with their Colliders deactivated.
    ///
    /// NOTE ON BLENDING: This implementation simply re-enables the Animator, causing
    /// the character to 'snap' back to its current animation pose. For a smoother
    /// transition (e.g., a "get up" animation or inverse kinematics blending),
    /// a more advanced system would be required, which is beyond the scope of this
    /// basic RagdollSystem pattern demonstration.
    /// </summary>
    public void DisableRagdoll()
    {
        // Prevent re-disabling if already inactive.
        if (!IsRagdollActive) return;

        Debug.Log("RagdollSystem: Disabling Ragdoll.", this);

        // 1. Disable ragdoll physics: Make all ragdoll rigidbodies kinematic again
        // so they are controlled by the Animator, not physics.
        SetRagdollPhysicsState(false);

        // 2. Disable ragdoll colliders to prevent conflicts with the main character collider.
        SetRagdollColliderState(false);

        // 3. Re-enable the main character controller and/or main collider.
        if (_characterController != null)
        {
            _characterController.enabled = true;
        }
        if (_mainCollider != null)
        {
            _mainCollider.enabled = true;
        }

        // 4. Re-enable the Animator component to resume animation playback.
        if (_animator != null)
        {
            _animator.enabled = true;
        }

        IsRagdollActive = false;
    }

    /// <summary>
    /// Helper method to set the kinematic state for all ragdoll rigidbodies.
    /// </summary>
    /// <param name="enablePhysics">
    /// True to enable physics (set `isKinematic` to false),
    /// False to disable physics (set `isKinematic` to its initial state, typically true).
    /// </param>
    private void SetRagdollPhysicsState(bool enablePhysics)
    {
        for (int i = 0; i < _ragdollRigidbodies.Count; i++)
        {
            Rigidbody rb = _ragdollRigidbodies[i];
            if (rb != null)
            {
                rb.isKinematic = enablePhysics ? false : _initialRigidbodyKinematicStates[i];

                // When disabling physics, reset velocities to stop any residual movement.
                if (!enablePhysics)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    /// <summary>
    /// Helper method to enable or disable all ragdoll colliders.
    /// </summary>
    /// <param name="enabled">True to enable colliders, False to disable them.</param>
    private void SetRagdollColliderState(bool enabled)
    {
        foreach (Collider col in _ragdollColliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
    }
}


/*
/// --- How to Use the RagdollSystem Pattern in Unity ---

The `RagdollSystem` script acts as a central control point. Here's how to integrate it:

1.  **Prepare Your Character Model:**
    *   **3D Model:** You need a rigged 3D character model.
    *   **Animator:** The character must have an `Animator` component with some animations (e.g., Idle, Walk).
    *   **Main Collider/Controller (Optional but common):** If your character is controlled by a `CharacterController` or has a single main `CapsuleCollider` for collision, ensure it's on the root GameObject.
    *   **Ragdoll Setup:** This is the most crucial part.
        *   Each significant bone (Hips, Spine, Head, Upper Arms, Forearms, Upper Legs, Lower Legs, etc.) needs a `Rigidbody` and a `Collider` component attached to it.
        *   Initially, all these `Rigidbody` components should have `Is Kinematic` set to `true` (meaning they are controlled by animation, not physics).
        *   Their `Collider` components should typically be disabled or set as `Is Trigger` when not ragdolling to avoid interference.
        *   `CharacterJoint` components usually connect each bone's `Rigidbody` to its parent's `Rigidbody`, defining the ragdoll's flexibility. Unity's "Ragdoll Wizard" (GameObject -> 3D Object -> Ragdoll...) can help set this up quickly.

2.  **Create a GameObject for the RagdollSystem:**
    *   Attach the `RagdollSystem.cs` script to the **root GameObject of your character** (the one with the `Animator` and `CharacterController`).

3.  **Configure in the Inspector:**
    *   Select your character's root GameObject in the Hierarchy.
    *   In the Inspector, locate the `RagdollSystem` component.
    *   **Ragdoll Root Bone:** Drag the **"Hips" bone** (or the equivalent root bone of your character's skeletal hierarchy, which is typically where your ragdoll starts) from the Hierarchy into this slot.
    *   **Animator:** Drag your character's `Animator` component (usually on the root) into this slot. (The script will try to auto-find if left blank).
    *   **Character Controller:** Drag your character's `CharacterController` component (if present on the root) into this slot. (The script will try to auto-find if left blank).
    *   **Main Collider:** Drag any other main `Collider` (e.g., a `CapsuleCollider` used for character collision on the root) into this slot. (The script will try to auto-find if left blank).

4.  **Integrate with Your Game Logic:**
    *   From other scripts (e.g., a `PlayerHealth.cs`, a `CombatSystem.cs`, an `EnemyAI.cs`), get a reference to the `RagdollSystem` component.
    *   Call `ragdollSystem.EnableRagdoll()` when the character should enter the ragdoll state (e.g., on death, a powerful impact).
        *   You can pass an optional `initialForce`, `forcePoint`, and `forceMode` to simulate the impact that caused the ragdoll.
    *   Call `ragdollSystem.DisableRagdoll()` when the character should recover from the ragdoll state and return to animation (e.g., after a few seconds, when a "get up" animation finishes, or upon revival).

---

**Example Usage in another script (e.g., a `PlayerHealth.cs`):**

```csharp
using UnityEngine;
using System.Collections; // For IEnumerator

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    // A reference to the RagdollSystem component on this GameObject or a child.
    // Assign this in the Inspector, or let it auto-find.
    [SerializeField]
    private RagdollSystem _ragdollSystem; 

    void Awake()
    {
        currentHealth = maxHealth;
        
        // Auto-find RagdollSystem if not assigned in Inspector.
        if (_ragdollSystem == null)
        {
            _ragdollSystem = GetComponent<RagdollSystem>();
            if (_ragdollSystem == null)
            {
                Debug.LogError("PlayerHealth: RagdollSystem component not found on this GameObject!", this);
            }
        }
    }

    /// <summary>
    /// Simulates taking damage, potentially activating the ragdoll.
    /// </summary>
    /// <param name="amount">The amount of damage taken.</param>
    /// <param name="hitForce">The force vector of the impact.</param>
    /// <param name="hitPoint">The world position of the impact.</param>
    public void TakeDamage(float amount, Vector3 hitForce, Vector3 hitPoint)
    {
        currentHealth -= amount;
        Debug.Log($"Took {amount} damage. Current health: {currentHealth}");

        // If health drops to zero and the character isn't already ragdolling,
        // trigger the ragdoll state.
        if (currentHealth <= 0 && _ragdollSystem != null && !_ragdollSystem.IsRagdollActive)
        {
            Die(hitForce, hitPoint);
        }
    }

    /// <summary>
    /// Handles the character's death, activating the ragdoll.
    /// </summary>
    private void Die(Vector3 hitForce, Vector3 hitPoint)
    {
        Debug.Log("Player Died! Activating Ragdoll.");
        if (_ragdollSystem != null)
        {
            // Enable the ragdoll, applying the force from the impact.
            _ragdollSystem.EnableRagdoll(hitForce, hitPoint, ForceMode.Impulse);

            // Optionally, set a timer to disable the ragdoll and 'revive' the character.
            StartCoroutine(ReviveAfterDelay(5f)); 
        }
    }

    /// <summary>
    /// Coroutine to disable the ragdoll and reset character state after a delay.
    /// </summary>
    private IEnumerator ReviveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_ragdollSystem != null && _ragdollSystem.IsRagdollActive)
        {
            Debug.Log("Attempting to revive/disable ragdoll.");
            _ragdollSystem.DisableRagdoll();
            
            // Reset health and any other relevant character states.
            currentHealth = maxHealth;

            // IMPORTANT: When disabling a ragdoll, the character will snap to the animator's
            // current pose. You might need to manually reposition the character's root
            // Transform (e.g., to a safe standing position) if the ragdoll ended up in an awkward spot.
            // For example:
            // transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            // transform.rotation = Quaternion.identity;
            // Or use an explicit "get up" animation if your animation system supports it.
        }
    }

    // Example: A debug method to force damage or ragdoll via a key press (for testing)
    void Update()
    {
        // Press 'K' to simulate taking fatal damage
        if (Input.GetKeyDown(KeyCode.K))
        {
            // Simulate a strong hit from the front-left
            TakeDamage(100f, (transform.up + -transform.forward + -transform.right) * 500f, transform.position + Vector3.up * 0.7f);
        }

        // Press 'L' to force disable ragdoll (e.g., for quick testing revival)
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_ragdollSystem != null && _ragdollSystem.IsRagdollActive)
            {
                StopAllCoroutines(); // Stop any pending revival coroutine
                _ragdollSystem.DisableRagdoll();
                currentHealth = maxHealth;
                Debug.Log("Forced ragdoll disable. Health reset.");
            }
        }
    }
}
```