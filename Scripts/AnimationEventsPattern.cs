// Unity Design Pattern Example: AnimationEventsPattern
// This script demonstrates the AnimationEventsPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AnimationEventsPattern' is a design pattern in Unity that leverages Unity's built-in Animation Events feature. It allows you to trigger specific methods in your scripts at precise moments during an animation clip's playback. This pattern is incredibly useful for synchronizing game logic (like dealing damage, playing sound effects, spawning particles, enabling/disabling colliders, or updating UI) with visual animations.

### Why use the Animation Events Pattern?

*   **Precise Synchronization:** Ensures actions happen exactly when they look like they should in the animation (e.g., damage is dealt the moment a sword connects, not just when the attack animation starts or ends).
*   **Decoupling Logic:** Separates the visual animation (how something looks) from the game logic (what something does). The animation clip itself doesn't need to know *what* `OnAttackStrike` does, only that it needs to call it.
*   **Designer-Friendly:** Animators can directly place events in the Animation window, giving them control over timing without needing a programmer to write timing-specific code (e.g., `yield return new WaitForSeconds(0.5f)`).
*   **Flexibility:** Easily adjust timing in the editor without changing code.

### How the Pattern Works:

1.  **Script with Public Methods:** You create a C# script attached to the GameObject that has an `Animator` component. This script contains public methods that you want to be called. These methods can have no parameters, or a single parameter of type `float`, `int`, `string`, `bool`, or `Object`.
2.  **Animation Clip:** You have an animation clip (e.g., an `.anim` file) that defines the visual movement.
3.  **Animation Window:** In the Unity Editor, with the GameObject and its animation clip selected, you open the `Animation` window.
4.  **Adding Events:** At a specific frame in the animation timeline, you add an "Animation Event".
5.  **Assigning Method:** For each event, you select the target function (your public method from step 1) from a dropdown list. If your method takes a parameter, you can provide its value directly in the editor.
6.  **Runtime Trigger:** When the animation plays and reaches the frame with the event, Unity automatically invokes the specified method on the script attached to the same GameObject (or a parent/child if configured).

---

### Complete C# Unity Example: `AnimationEventsDemo.cs`

This script demonstrates an attack animation for a character (e.g., a sword swing) where:
*   An effect plays at the start of the swing.
*   A temporary hitbox is spawned and a sound is "played" at the moment of impact.
*   The character's internal state is reset when the animation finishes.

```csharp
using UnityEngine;
using System.Collections; // Required for coroutines, if you were to use them within the event methods.

/// <summary>
/// Demonstrates the AnimationEventsPattern in Unity.
/// This script is designed to be attached to a GameObject with an Animator component.
/// It defines public methods that can be triggered at specific points within an animation clip
/// via Unity's Animation Events feature.
/// </summary>
/// <remarks>
/// The AnimationEventsPattern is crucial for synchronizing gameplay logic (e.g., dealing damage,
/// playing sound effects, spawning particles) with visual animations.
/// It decouples the animation's visual representation from the game's functional logic,
/// allowing animators to control timing without needing to modify code directly.
/// </remarks>
public class AnimationEventsDemo : MonoBehaviour
{
    [Header("Animation Event Settings")]
    [Tooltip("Name of the sound clip to 'play' when the attack strikes. (Simulated via Debug.Log)")]
    public string attackSoundClipName = "SwordSwing_SFX";

    [Tooltip("Prefab of a temporary hitbox collider to instantiate during the attack strike.")]
    public GameObject attackHitboxPrefab;

    [Tooltip("Transform indicating where the attack hitbox should be spawned.")]
    public Transform attackSpawnPoint;

    [Tooltip("How long the instantiated hitbox should remain active before being destroyed.")]
    public float hitboxDuration = 0.2f; // e.g., 200 milliseconds

    // Private reference to the Animator component on this GameObject.
    private Animator _animator;

    // Simple state flag to prevent re-triggering attack while one is ongoing.
    private bool _isAttacking = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to get a reference to the Animator component.
    /// </summary>
    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("AnimationEventsDemo: Animator component not found on this GameObject. " +
                           "This script requires an Animator to function. Disabling script.");
            enabled = false; // Disable this script if no Animator is found.
        }
    }

    /// <summary>
    /// Called once per frame. Handles player input to trigger the attack animation.
    /// </summary>
    void Update()
    {
        // Example: Press Spacebar to trigger the attack animation.
        if (Input.GetKeyDown(KeyCode.Space) && !_isAttacking)
        {
            TriggerAttackAnimation();
        }
    }

    /// <summary>
    /// This method is designed to be called by an Animation Event during the attack animation,
    /// precisely when the weapon/character should *actually* hit its target.
    /// </summary>
    /// <remarks>
    /// **Setup in Unity Editor:**
    /// 1. Select the GameObject with this script and its Animator.
    /// 2. Open the Animation Window (`Window > Animation > Animation`).
    /// 3. Select the 'AttackAnimation' clip.
    /// 4. Move the scrubber (vertical white line) to the desired frame (e.g., mid-swing where impact occurs).
    /// 5. Click the 'Add Event' button (small button with a plus icon).
    /// 6. Select `OnAttackStrike` from the 'Function' dropdown in the Inspector (below the Animation window).
    /// </remarks>
    public void OnAttackStrike()
    {
        Debug.Log($"<color=cyan>[Animation Event] OnAttackStrike() triggered at frame: {Time.frameCount}</color>");

        _isAttacking = true; // Mark character as actively hitting/attacking.

        // --- Practical Action 1: Instantiate a temporary hitbox for damage detection ---
        if (attackHitboxPrefab != null && attackSpawnPoint != null)
        {
            // Create the hitbox at the specified spawn point's position and rotation.
            GameObject hitboxInstance = Instantiate(attackHitboxPrefab, attackSpawnPoint.position, attackSpawnPoint.rotation);
            // Parent the hitbox to the character's weapon or relevant bone if needed.
            // hitboxInstance.transform.SetParent(attackSpawnPoint);

            // A simple way to manage its lifetime: destroy it after a set duration.
            // In a real game, this hitbox might have a script to detect collisions and apply damage.
            Destroy(hitboxInstance, hitboxDuration);
            Debug.Log($"<color=yellow>Spawned '{hitboxInstance.name}' hitbox for {hitboxDuration:F2} seconds.</color>");
        }
        else
        {
            Debug.LogWarning("Attack Hitbox Prefab or Attack Spawn Point not assigned. Cannot spawn hitbox.");
        }

        // --- Practical Action 2: Simulate playing an attack sound ---
        // In a real project, you'd use an AudioManager or an AudioSource component.
        Debug.Log($"<color=green>Playing attack sound: {attackSoundClipName}</color>");
        // Example: AudioManager.Instance.PlaySound(attackSoundClipName);
    }

    /// <summary>
    /// This method is designed to be called by an Animation Event when the attack animation
    /// has visually completed its cycle.
    /// </summary>
    /// <remarks>
    /// **Setup in Unity Editor:**
    /// 1. Select the GameObject with this script and its Animator.
    /// 2. Open the Animation Window (`Window > Animation > Animation`).
    /// 3. Select the 'AttackAnimation' clip.
    /// 4. Move the scrubber to the very end of the animation clip (or just before it loops/transitions).
    /// 5. Click the 'Add Event' button.
    /// 6. Select `OnAttackEnd` from the 'Function' dropdown.
    /// </remarks>
    public void OnAttackEnd()
    {
        Debug.Log($"<color=orange>[Animation Event] OnAttackEnd() triggered at frame: {Time.frameCount}</color>");

        // Reset the attacking state, allowing new actions or attacks.
        _isAttacking = false;
        Debug.Log("Attack animation finished. Character is now ready for next action.");

        // Additional cleanup or state resets can happen here.
        // E.g., disable certain effects, reset physics states.
    }

    /// <summary>
    /// This method demonstrates how to pass a string parameter to an Animation Event.
    /// Useful for playing specific effects or sounds determined by the animator.
    /// </summary>
    /// <param name="effectName">A string identifying the specific effect to play.</param>
    /// <remarks>
    /// **Setup in Unity Editor:**
    /// 1. Select the GameObject with this script and its Animator.
    /// 2. Open the Animation Window (`Window > Animation > Animation`).
    /// 3. Select the 'AttackAnimation' clip.
    /// 4. Move the scrubber to an early frame (e.g., start of the swing).
    /// 5. Click the 'Add Event' button.
    /// 6. Select `PlayEffect` from the 'Function' dropdown.
    /// 7. In the 'String' parameter field (below the Function dropdown), type in a value like "SwingStartEffect".
    /// </remarks>
    public void PlayEffect(string effectName)
    {
        Debug.Log($"<color=magenta>[Animation Event] PlayEffect('{effectName}') triggered!</color>");
        // Practical action: Trigger a particle system, play a specific sound, or apply a buff.
        // Example: ParticleSystemManager.Instance.PlayParticle(effectName, transform.position);
        Debug.Log($"Attempting to play effect: '{effectName}' based on animator's instruction.");
    }

    /// <summary>
    /// Helper method to programmatically trigger the attack animation from code.
    /// This typically sets a trigger parameter in the Animator Controller.
    /// </summary>
    public void TriggerAttackAnimation()
    {
        if (_animator != null)
        {
            // Set the "Attack" trigger in the Animator Controller.
            // Ensure you have a 'Trigger' parameter named "Attack" in your Animator Controller.
            _animator.SetTrigger("Attack");
            Debug.Log("<color=white>Initiating Attack animation...</color>");
        }
    }

    // ###############################################################################################
    // ### UNITY EDITOR SETUP FOR ANIMATION EVENTS PATTERN ###########################################
    // ###############################################################################################
    /*
    To make this `AnimationEventsDemo` script work with the AnimationEventsPattern,
    follow these steps in the Unity Editor:

    1.  **Create a 3D Object for Animation:**
        *   In the Unity Editor, go to `GameObject -> 3D Object -> Cube`. This will be our animated character.
        *   Rename it to `Character` for clarity.

    2.  **Attach `AnimationEventsDemo` Script:**
        *   Drag and drop the `AnimationEventsDemo.cs` script onto your `Character` GameObject in the Hierarchy.

    3.  **Create an Animator Controller:**
        *   In the Project window, right-click `Create -> Animator Controller`. Name it `CharacterAnimator`.
        *   Drag the `CharacterAnimator` from the Project window onto your `Character` GameObject in the Hierarchy. This will automatically add an `Animator` component.

    4.  **Create an Animation Clip:**
        *   With the `Character` GameObject selected in the Hierarchy, open the `Animation` window (`Window -> Animation -> Animation`).
        *   If it says "To begin animating Character, create an Animator and an AnimationClip.", click the `Create` button.
        *   Name the new animation clip `AttackAnimation` and save it in your Assets folder.

    5.  **Record a Simple Attack Animation (for demonstration):**
        *   In the `Animation` window, ensure `AttackAnimation` is selected in the dropdown.
        *   Click the red record button (circle icon).
        *   At `0:00` (first frame): Set `Character`'s `Rotation Z` to `0`.
        *   At `0:08` (8th frame): Set `Character`'s `Rotation Z` to `90`. (This represents the mid-swing/impact point).
        *   At `0:16` (16th frame): Set `Character`'s `Rotation Z` to `-90`.
        *   At `0:24` (24th frame): Set `Character`'s `Rotation Z` back to `0`. (End of the swing).
        *   Click the red record button again to stop recording.

    6.  **Configure the Animator Controller:**
        *   Open the `Animator` window (`Window -> Animation -> Animator`).
        *   You should see `AttackAnimation` as the default (orange) state.
        *   Right-click in an empty area -> `Create State -> Empty`. Name this state `Idle`.
        *   **Create Parameters:** In the `Animator` window, go to the `Parameters` tab (usually next to `Layers`). Click the `+` icon and select `Trigger`. Name it `Attack`. This trigger will be used by the script to start the animation.
        *   **Create Transitions:**
            *   Right-click on `Idle` state -> `Make Transition` -> click on `AttackAnimation`.
            *   Right-click on `AttackAnimation` state -> `Make Transition` -> click on `Idle`.
        *   **Configure `Idle` to `AttackAnimation` Transition:**
            *   Select the transition arrow from `Idle` to `AttackAnimation`.
            *   In the Inspector:
                *   Uncheck `Has Exit Time`.
                *   Under `Conditions`, click `+` and select `Attack` (this is the trigger we just created).
        *   **Configure `AttackAnimation` to `Idle` Transition:**
            *   Select the transition arrow from `AttackAnimation` to `Idle`.
            *   In the Inspector:
                *   Check `Has Exit Time`.
                *   Set `Exit Time` to `1` (this ensures the animation plays fully before transitioning).
                *   Set `Transition Duration` to `0` or a very small value if you want an immediate snap back to idle.
                *   No `Conditions` are needed as `Has Exit Time` handles the timing.

    7.  **Add Animation Events to `AttackAnimation`:**
        *   Select your `Character` GameObject in the Hierarchy.
        *   Open the `Animation` window and ensure `AttackAnimation` is selected.
        *   **Add `OnAttackStrike` Event:**
            *   Move the scrub bar (the white vertical line) to approximately `0:08` (where the `Rotation Z` was set to `90` – our simulated impact point).
            *   Click the `Add Event` button (small button with a plus sign, usually on the timeline header). A small white marker appears.
            *   Click on the newly created white event marker.
            *   In the Inspector (below the Animation window), a `Function` dropdown will appear. Select `OnAttackStrike` from the list.
        *   **Add `OnAttackEnd` Event:**
            *   Move the scrub bar to `0:24` (the very end of the animation, where `Rotation Z` returns to `0`).
            *   Click the `Add Event` button.
            *   Click on the new event marker and select `OnAttackEnd` from the `Function` dropdown.
        *   **Add `PlayEffect` Event with a Parameter:**
            *   Move the scrub bar to approximately `0:02` (just after the swing starts).
            *   Click the `Add Event` button.
            *   Click on the new event marker and select `PlayEffect` from the `Function` dropdown.
            *   In the `String` parameter field that appears below the Function dropdown, type `SwingStartParticles`.

    8.  **Setup Prefabs for `AnimationEventsDemo` Script (Optional but Recommended for Practicality):**
        *   **Create AttackHitbox Prefab:**
            *   In the Hierarchy, right-click `Create Empty`. Name it `AttackHitbox`.
            *   Add a `Box Collider` component to it (`Add Component -> Physics -> Box Collider`).
            *   Check the `Is Trigger` box on the `Box Collider`.
            *   Adjust the `Size` of the collider in the Inspector to represent a weapon's hit area (e.g., `X: 0.5`, `Y: 0.5`, `Z: 1`).
            *   Drag the `AttackHitbox` GameObject from the Hierarchy into your Project window (e.g., into an `Assets/Prefabs` folder) to create a prefab.
            *   Delete the `AttackHitbox` from the Hierarchy.
        *   **Create AttackSpawnPoint:**
            *   Select your `Character` GameObject.
            *   Right-click on `Character` in the Hierarchy `Create Empty`. Name it `AttackSpawnPoint`.
            *   Position `AttackSpawnPoint` slightly in front of your `Character` (e.g., `Position Z: 0.75`). This will be where the hitbox appears.
        *   **Assign in Script:**
            *   Select your `Character` GameObject.
            *   In the `AnimationEventsDemo` component in the Inspector:
                *   Drag the `AttackHitbox` prefab from your Project window into the `Attack Hitbox Prefab` slot.
                *   Drag the `AttackSpawnPoint` child GameObject from the Hierarchy into the `Attack Spawn Point` slot.

    9.  **Run the Scene:**
        *   Press the Play button in the Unity Editor.
        *   Press the `Spacebar` key.
        *   Observe the Cube's animation and check the Console window for the `Debug.Log` messages, which will confirm when `PlayEffect`, `OnAttackStrike`, and `OnAttackEnd` are called at their respective animation event points. You should also see a temporary hitbox appear and disappear in the Scene view during the `OnAttackStrike` event.
    */
    // ###############################################################################################
}
```