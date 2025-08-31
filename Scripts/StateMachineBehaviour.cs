// Unity Design Pattern Example: StateMachineBehaviour
// This script demonstrates the StateMachineBehaviour pattern in Unity
// Generated automatically - ready to use in your Unity project

The `StateMachineBehaviour` pattern in Unity allows you to attach scripts directly to states within an Animator Controller. This provides a powerful way to execute logic precisely when an animation state is entered, updated, or exited, making it ideal for state-specific actions like playing sounds, enabling/disabling hitboxes, managing character abilities, or triggering events tied to animation sequences.

This example will demonstrate a practical use case: managing an "Attack" state for a character. When the character enters the attack animation state, we'll enable a hitbox, play a sound, and when the state exits, we'll disable the hitbox.

---

### **Step 1: Create a Placeholder Component (`AttackHandler.cs`)**

This script will represent the component on your character that the `StateMachineBehaviour` will interact with. In a real project, this might be your `PlayerController`, `EnemyAI`, or a dedicated `CombatManager`.

```csharp
// AttackHandler.cs
using UnityEngine;
using System.Collections; // Required for Coroutines if you use them here

/// <summary>
/// A placeholder component to simulate character attack logic.
/// The StateMachineBehaviour will communicate with this script.
/// </summary>
public class AttackHandler : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float baseAttackDamage = 10f;
    [SerializeField] private AudioClip attackSoundClip;
    [SerializeField] private AudioSource audioSource; // Reference to an AudioSource component

    private bool isHitboxActive = false;

    void Awake()
    {
        // Get AudioSource if not already assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // Makes sound somewhat 3D
            }
        }
    }

    /// <summary>
    /// Called by the StateMachineBehaviour when the attack animation state begins.
    /// </summary>
    /// <param name="damageMultiplier">A multiplier for base damage, often from specific attack types.</param>
    public void StartAttackSequence(float damageMultiplier = 1f)
    {
        Debug.Log($"<color=cyan>AttackHandler:</color> Starting attack sequence. Base Damage: {baseAttackDamage * damageMultiplier}");
        
        // Play the attack sound if available
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSoundClip);
        }

        // In a real game, you might start a coroutine here to enable hitbox after a delay
        // and then disable it, or let the SMB handle hitbox timing more directly.
        // For this example, we'll let the SMB explicitly enable/disable.
    }

    /// <summary>
    /// Enables the character's hitbox or attack collider.
    /// The StateMachineBehaviour will call this when the attack animation reaches a certain point.
    /// </summary>
    /// <param name="damageAmount">The actual damage this attack instance should deal.</param>
    public void EnableHitbox(float damageAmount)
    {
        if (!isHitboxActive)
        {
            isHitboxActive = true;
            Debug.Log($"<color=green>AttackHandler:</color> Hitbox <color=green>ENABLED</color>! Damage: {damageAmount}");
            // In a real game:
            // - Activate a BoxCollider2D/3D on a child GameObject
            // - Set its tag to "Attack"
            // - Store damageAmount for when it collides with an enemy
            // - Subscribe to collision events
        }
    }

    /// <summary>
    /// Disables the character's hitbox or attack collider.
    /// Called by the StateMachineBehaviour when the attack animation state ends.
    /// </summary>
    public void DisableHitbox()
    {
        if (isHitboxActive)
        {
            isHitboxActive = false;
            Debug.Log($"<color=red>AttackHandler:</color> Hitbox <color=red>DISABLED</color>.");
            // In a real game:
            // - Deactivate the BoxCollider2D/3D
            // - Clear any collision event subscriptions
        }
    }

    /// <summary>
    /// Returns whether the hitbox is currently active.
    /// </summary>
    public bool IsHitboxActive()
    {
        return isHitboxActive;
    }

    // You could also add methods here for:
    // - Blocking movement during attack
    // - Applying status effects
    // - Handling combo chains
}
```

---

### **Step 2: Create the `StateMachineBehaviour` (`AttackStateSMB.cs`)**

This is the core of the example, demonstrating the `StateMachineBehaviour` pattern.

```csharp
// AttackStateSMB.cs
using UnityEngine;
using System.Collections; // Though not strictly necessary for this example, often useful.

/// <summary>
/// This StateMachineBehaviour script controls logic when an Animator state
/// associated with an attack animation is active.
///
/// It demonstrates how to:
/// 1. React to state entry and exit events (OnStateEnter, OnStateExit).
/// 2. React to state update events (OnStateUpdate).
/// 3. Access the Animator's GameObject and other components on it.
/// 4. Control specific timing within an animation using normalizedTime.
/// 5. Pass parameters (e.g., damage) from the SMB to other components.
/// </summary>
public class AttackStateSMB : StateMachineBehaviour
{
    [Header("Attack Configuration (via Inspector)")]
    [Tooltip("The damage multiplier for this specific attack animation.")]
    [SerializeField] private float attackDamageMultiplier = 1.0f;

    [Tooltip("Normalized time (0-1) in the animation when the hitbox should become active.")]
    [Range(0f, 1f)]
    [SerializeField] private float hitboxEnableNormalizedTime = 0.2f;

    [Tooltip("Normalized time (0-1) in the animation when the hitbox should become inactive.")]
    [Range(0f, 1f)]
    [SerializeField] private float hitboxDisableNormalizedTime = 0.8f;

    private AttackHandler attackHandler; // Reference to the custom AttackHandler component
    private bool hasTriggeredHitboxEnable = false; // Flag to ensure hitbox is enabled only once per attack cycle
    private bool hasTriggeredHitboxDisable = false; // Flag to ensure hitbox is disabled only once per attack cycle

    // This method is called when the Animator enters the state that this SMB is attached to.
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"<color=yellow>SMB:</color> Entering <color=yellow>{stateInfo.fullPathHash}</color> (Attack) state.");

        // 1. Get a reference to the AttackHandler component on the same GameObject as the Animator.
        //    This is crucial for the SMB to interact with character-specific logic.
        attackHandler = animator.GetComponent<AttackHandler>();

        if (attackHandler == null)
        {
            Debug.LogError($"<color=red>SMB Error:</color> AttackHandler component not found on {animator.gameObject.name}. " +
                           "Please ensure an AttackHandler is attached to the GameObject with the Animator.");
            return;
        }

        // 2. Reset flags for the new attack cycle.
        hasTriggeredHitboxEnable = false;
        hasTriggeredHitboxDisable = false; // Ensure it can be re-enabled next time

        // 3. Inform the AttackHandler that an attack sequence has started.
        //    This could trigger sound, particle effects, or other initial setup.
        attackHandler.StartAttackSequence(attackDamageMultiplier);
    }

    // This method is called every frame while the Animator is in the state this SMB is attached to.
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackHandler == null) return; // Safegaurd if AttackHandler was not found

        // stateInfo.normalizedTime represents the progress of the animation in the current state.
        // It's a float from 0.0 (start) to 1.0 (end) for one full cycle.
        // For looping animations, it can go above 1.0 (e.g., 1.5 for half of the second loop).
        // Use `stateInfo.normalizedTime % 1f` for normalized time within the current loop if needed.

        // Enable Hitbox Logic:
        // Check if the animation has passed the `hitboxEnableNormalizedTime`
        // and if the hitbox hasn't been enabled for this attack cycle yet.
        if (!hasTriggeredHitboxEnable && stateInfo.normalizedTime >= hitboxEnableNormalizedTime)
        {
            // Calculate actual damage to pass to the AttackHandler.
            // This allows the base damage to be stored in AttackHandler and modified by SMB.
            float currentAttackDamage = attackHandler.baseAttackDamage * attackDamageMultiplier;
            attackHandler.EnableHitbox(currentAttackDamage);
            hasTriggeredHitboxEnable = true; // Set flag to prevent re-enabling
        }

        // Disable Hitbox Logic:
        // Check if the animation has passed the `hitboxDisableNormalizedTime`
        // and if the hitbox hasn't been disabled for this attack cycle yet.
        // Also ensure it was actually enabled first.
        if (hasTriggeredHitboxEnable && !hasTriggeredHitboxDisable && stateInfo.normalizedTime >= hitboxDisableNormalizedTime)
        {
            attackHandler.DisableHitbox();
            hasTriggeredHitboxDisable = true; // Set flag to prevent re-disabling
        }

        // Example: If you wanted to do something *only* in the last 10% of the animation:
        // if (stateInfo.normalizedTime > 0.9f && stateInfo.normalizedTime < 1.0f) { /* ... */ }
    }

    // This method is called when the Animator exits the state that this SMB is attached to.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"<color=yellow>SMB:</color> Exiting <color=yellow>{stateInfo.fullPathHash}</color> (Attack) state.");

        if (attackHandler == null) return; // Safeguard

        // Ensure the hitbox is disabled when exiting the state, just in case it wasn't already.
        // This is a safety measure to prevent hitboxes from lingering if the animation is interrupted.
        if (attackHandler.IsHitboxActive())
        {
            attackHandler.DisableHitbox();
        }

        // Clear the reference to avoid memory leaks if the Animator is destroyed,
        // though Unity usually handles this well for components on the same GameObject.
        attackHandler = null;
    }

    // --- Optional StateMachineBehaviour Methods ---

    // This method is called right after Animator.OnAnimatorMove() for Animator.rootRotation and Animator.rootPosition.
    // Useful for controlling root motion directly from the state.
    // override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    //    // Implement code that processes and affects root motion
    // }

    // This method is called right after Animator.OnAnimatorIK() for setting up animation IK.
    // Useful for inverse kinematics (e.g., making a character look at a target during an animation).
    // override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    //    // Implement code that sets up animation IK (inverse kinematics)
    // }
}
```

---

### **Step 3: How to Set Up in Unity Editor**

1.  **Create a Character GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., `PlayerCharacter`).
    *   Add an `Animator` component to it.
    *   Add an `AudioSource` component (if your `AttackHandler` uses one).
    *   Attach the `AttackHandler.cs` script to this `PlayerCharacter` GameObject.
    *   Assign an `AudioClip` to the `attackSoundClip` field of the `AttackHandler` in the Inspector.

2.  **Create an Animator Controller:**
    *   Right-click in your Project window -> Create -> Animator Controller. Name it (e.g., `PlayerAnimatorController`).
    *   Assign this controller to the `Animator` component on your `PlayerCharacter`.

3.  **Set Up Animator States:**
    *   Double-click your `PlayerAnimatorController` to open the Animator window.
    *   Right-click in the Animator window -> Create State -> Empty. Name it `Idle`.
    *   Right-click -> Create State -> Empty. Name it `Attack`.
    *   Create a transition from `Idle` to `Attack` and from `Attack` back to `Idle`.
    *   **Crucially:** Assign an actual attack animation clip to the `Attack` state. Drag your animation clip from the Project window onto the `Motion` field in the Inspector when the `Attack` state is selected. Adjust its length and looping as needed (usually, attack animations don't loop).

4.  **Attach the `StateMachineBehaviour` to the `Attack` State:**
    *   Select the `Attack` state in the Animator window.
    *   In the Inspector, click the "Add Behaviour" button.
    *   Search for `AttackStateSMB` and select it.
    *   Now, you will see the `AttackStateSMB` component directly on the `Attack` state in the Inspector.
    *   Configure its `Attack Damage Multiplier`, `Hitbox Enable Normalized Time`, and `Hitbox Disable Normalized Time` as desired.

5.  **Trigger the Attack State (Example):**
    *   To test, you'll need a way to transition into the `Attack` state.
    *   In the Animator window, create a new `Parameter` of type `Trigger` (e.g., `DoAttack`).
    *   Select the transition from `Idle` to `Attack`. In the Inspector, add a new `Condition` and select your `DoAttack` trigger.
    *   In a separate script (e.g., on your `PlayerCharacter` or a testing script), you would get a reference to the Animator and call `animator.SetTrigger("DoAttack")` to initiate the attack.

    ```csharp
    // Example usage for triggering the attack from another script
    // PlayerInputHandler.cs (or similar)
    using UnityEngine;

    public class PlayerInputHandler : MonoBehaviour
    {
        private Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) // Left mouse click to attack
            {
                if (animator != null)
                {
                    animator.SetTrigger("DoAttack");
                }
            }
        }
    }
    ```
    Attach `PlayerInputHandler.cs` to your `PlayerCharacter` GameObject.

### **How it Works & Educational Points:**

*   **Decoupling Logic:** The `AttackStateSMB` encapsulates logic specific to the *animation state*, not the character as a whole. This means you could have different `AttackStateSMB` scripts for different attack animations (e.g., `HeavyAttackSMB`, `DashAttackSMB`), each with its own timing and damage multipliers.
*   **State-Driven Actions:** Actions like enabling/disabling hitboxes or playing specific sounds are perfectly synchronized with the animation itself because they are triggered by the animation state's lifecycle.
*   **Accessing Components:** The `animator.gameObject.GetComponent<AttackHandler>()` line is key. It shows how the `StateMachineBehaviour` (which lives on the Animator state) can reach out and interact with other components residing on the same GameObject as the Animator.
*   **`AnimatorStateInfo` and `normalizedTime`:** These are crucial for timing events within an animation. `normalizedTime` allows you to trigger events as a percentage of the animation's progress, independent of its actual duration.
*   **Flags (`hasTriggeredHitboxEnable`):** These are important in `OnStateUpdate` to ensure that actions that should only happen *once* per state (like enabling a hitbox) don't repeatedly fire every frame once their condition is met.
*   **Safety/Robustness:** Checking for `null` references (`attackHandler == null`) makes the script more resilient to setup errors.
*   **Configuration in Inspector:** `[SerializeField]` variables allow designers to tweak parameters like damage multipliers and hitbox timing directly on the Animator state in the Inspector, without touching code.

By following these steps and understanding the comments, you'll have a complete, practical, and educational example of the `StateMachineBehaviour` design pattern in Unity.