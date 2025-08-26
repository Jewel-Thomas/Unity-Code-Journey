// Unity Design Pattern Example: Facade
// This script demonstrates the Facade pattern in Unity
// Generated automatically - ready to use in your Unity project

The Facade design pattern provides a unified interface to a set of interfaces in a subsystem. Facade defines a higher-level interface that makes the subsystem easier to use. In Unity, this is particularly useful when a single action (e.g., "perform special attack," "load game," "interact with NPC") requires coordination across multiple independent systems (animation, audio, UI, save data, AI).

This example demonstrates a `SpecialAttackFacade` that simplifies the process of performing a complex special attack. A special attack typically involves:
1.  Playing an animation.
2.  Playing a sound effect.
3.  Spawning a visual particle effect.
4.  Applying damage to a target.
5.  Updating the user interface (e.g., showing cooldown).

Without the Facade, a `PlayerController` script would need to directly interact with `Animator`, `AudioSource`, `Instantiate` particle effects, call a `DamageSystem`, and update a `UISystem`. This leads to tightly coupled code and makes the `PlayerController` overly complex. The Facade pattern solves this by providing a single, clean method (`PerformSpecialAttack`) that orchestrates all these actions.

---

**`SpecialAttackFacade.cs`**

This script contains:
*   **Subsystem Helper Classes:** `AnimationHandler`, `AudioHandler`, `ParticleEffectHandler`, `DamageHandler`, `UIHandler`. These are simple C# classes that encapsulate the logic for a specific part of the special attack. In a real project, these could be full MonoBehaviours, ScriptableObjects, or dedicated service classes. For this example, they are simple helper classes instantiated and managed by the Facade.
*   **`SpecialAttackFacade` (MonoBehaviour):** This is the core Facade. It holds references to Unity components (Animator, AudioSource, prefabs) and internally initializes and uses instances of the subsystem helper classes. Its public `PerformSpecialAttack` method is the simplified interface for clients.
*   **`FacadeClient` (MonoBehaviour):** This acts as an example "client" that wants to perform a special attack. Notice how it only needs to know about the `SpecialAttackFacade` and calls a single method on it, without any knowledge of the underlying complexities.

```csharp
using UnityEngine;
using System.Collections; // Not strictly needed for this specific example, but often useful.
using System.Collections.Generic; // Not strictly needed for this specific example, but often useful.

namespace DesignPatterns.Facade
{
    // --- Subsystem Helper Classes (Internal details of the Facade) ---
    // These are simple C# classes, not MonoBehaviours, managed internally by the Facade.
    // In a larger project, these might be full MonoBehaviours on separate GameObjects,
    // ScriptableObjects, or dedicated service classes, but the Facade's role remains
    // to simplify interaction with them.

    /// <summary>
    /// Represents the Animation System responsible for playing character animations.
    /// </summary>
    internal class AnimationHandler
    {
        private Animator _animator;

        public AnimationHandler(Animator animator)
        {
            _animator = animator;
        }

        public void PlayAttackAnimation(string animationName)
        {
            if (_animator != null)
            {
                Debug.Log($"<color=cyan>[Animation System]</color> Playing animation: '{animationName}'");
                // Assuming animationName corresponds to a Trigger parameter in the Animator Controller.
                _animator.SetTrigger(animationName);
            }
            else
            {
                Debug.LogWarning("<color=orange>[Animation System]</color> Animator not assigned. Cannot play animation.");
            }
        }
    }

    /// <summary>
    /// Represents the Audio System responsible for playing sound effects.
    /// </summary>
    internal class AudioHandler
    {
        private AudioSource _audioSource;

        public AudioHandler(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }

        public void PlaySoundEffect(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                Debug.Log($"<color=magenta>[Audio System]</color> Playing sound effect: '{clip.name}'");
                _audioSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning("<color=orange>[Audio System]</color> AudioSource or AudioClip not assigned. Cannot play sound.");
            }
        }
    }

    /// <summary>
    /// Represents the Particle Effect System responsible for spawning visual effects.
    /// This handler is kept simple for demonstration, directly instantiating prefabs.
    /// In a real game, this might use an object pooling system.
    /// </summary>
    internal class ParticleEffectHandler
    {
        public void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
            {
                Debug.Log($"<color=yellow>[Particle Effect System]</color> Spawning effect: '{effectPrefab.name}' at {position}");
                GameObject.Instantiate(effectPrefab, position, rotation);
            }
            else
            {
                Debug.LogWarning("<color=orange>[Particle Effect System]</color> Effect prefab not assigned. Cannot spawn effect.");
            }
        }
    }

    /// <summary>
    /// Represents the Damage System responsible for applying damage to targets.
    /// </summary>
    internal class DamageHandler
    {
        public void ApplyDamage(int amount, GameObject target)
        {
            if (target != null)
            {
                Debug.Log($"<color=red>[Damage System]</color> Applied {amount} damage to '{target.name}'.");
                // In a real game, this would interact with a Health component on the target.
                // Example: target.GetComponent<HealthComponent>()?.TakeDamage(amount);
            }
            else
            {
                Debug.LogWarning("<color=orange>[Damage System]</color> Target is null. Cannot apply damage.");
            }
        }
    }

    /// <summary>
    /// Represents the UI System responsible for updating game interface elements.
    /// </summary>
    internal class UIHandler
    {
        public void UpdateSpecialAttackUI(float cooldownProgress)
        {
            // For demonstration, we just log the progress.
            // In a real game, this would update a UI slider, image fill amount, or text element.
            // A value of 0.0f typically means cooldown started, 1.0f means ready.
            Debug.Log($"<color=green>[UI System]</color> Updating special attack UI. Cooldown progress: {cooldownProgress:F2}");
        }
    }


    /// <summary>
    /// The Facade class: SpecialAttackFacade.
    /// This class provides a simplified, unified interface to a complex subsystem
    /// (the various handlers for animation, audio, particles, damage, and UI).
    /// It encapsulates the internal logic for performing a 'Special Attack'
    /// by coordinating calls to these underlying systems.
    /// Clients only interact with this Facade, shielding them from subsystem complexity.
    /// </summary>
    [RequireComponent(typeof(Animator))]    // Ensures an Animator component is on this GameObject.
    [RequireComponent(typeof(AudioSource))] // Ensures an AudioSource component is on this GameObject.
    public class SpecialAttackFacade : MonoBehaviour
    {
        [Header("Subsystem References (Unity Components/Assets)")]
        [Tooltip("The Animator component on this GameObject to control animations.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The AudioSource component on this GameObject to play sound effects.")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("The AudioClip to play when the special attack is performed.")]
        [SerializeField] private AudioClip _attackSound;
        [Tooltip("The GameObject prefab for the visual particle effect.")]
        [SerializeField] private GameObject _attackEffectPrefab;
        [Tooltip("The transform where particle effects will be spawned (e.g., character's hand, muzzle).")]
        [SerializeField] private Transform _effectSpawnPoint;
        [Tooltip("The base damage amount for the special attack.")]
        [SerializeField] private int _baseDamage = 50;
        [Tooltip("The name of the animation trigger for the special attack in the Animator Controller.")]
        [SerializeField] private string _attackAnimationTriggerName = "SpecialAttack";
        [Tooltip("The cooldown duration for the special attack in seconds.")]
        [SerializeField] private float _cooldownDuration = 3.0f;

        // Internal instances of our subsystem handlers.
        // The Facade creates and manages these, hiding them from clients.
        private AnimationHandler _animationHandler;
        private AudioHandler _audioHandler;
        private ParticleEffectHandler _particleEffectHandler;
        private DamageHandler _damageHandler;
        private UIHandler _uiHandler;

        private float _lastAttackTime = -Mathf.Infinity; // Tracks the last time attack was used for cooldown.

        // --- Unity Lifecycle Methods ---
        private void Awake()
        {
            // Ensure Unity components are assigned or try to get them from this GameObject.
            // Using [RequireComponent] helps, but checking for null is good practice.
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

            // Initialize the subsystem handlers.
            // The Facade is responsible for providing the necessary Unity components/data to them.
            _animationHandler = new AnimationHandler(_animator);
            _audioHandler = new AudioHandler(_audioSource);
            _particleEffectHandler = new ParticleEffectHandler(); // This handler is simple, doesn't need constructor args.
            _damageHandler = new DamageHandler();
            _uiHandler = new UIHandler();

            Debug.Log("<color=white>[Facade]</color> SpecialAttackFacade initialized.");
        }

        private void Update()
        {
            // Constantly update UI to reflect cooldown status.
            if (Time.time < _lastAttackTime + _cooldownDuration)
            {
                // Calculate cooldown progress (0.0f = just started, 1.0f = ready).
                float progress = (Time.time - _lastAttackTime) / _cooldownDuration;
                _uiHandler.UpdateSpecialAttackUI(progress);
            }
            else
            {
                // Indicate that the special attack is ready to be used.
                _uiHandler.UpdateSpecialAttackUI(1.0f);
            }
        }

        // --- Public Facade Method ---

        /// <summary>
        /// Performs a comprehensive special attack action.
        /// This is the single, simplified entry point for clients to trigger a complex sequence.
        /// The Facade orchestrates all the necessary steps across different underlying subsystems.
        /// </summary>
        /// <param name="targetEnemy">The GameObject representing the enemy to be attacked.</param>
        /// <returns>True if the attack was successfully performed, false if on cooldown.</returns>
        public bool PerformSpecialAttack(GameObject targetEnemy)
        {
            // First, check if the attack is on cooldown.
            if (Time.time < _lastAttackTime + _cooldownDuration)
            {
                Debug.Log($"<color=red>[Facade]</color> Special attack is on cooldown! " +
                          $"({_lastAttackTime + _cooldownDuration - Time.time:F1}s remaining)");
                return false; // Attack failed due to cooldown.
            }

            Debug.Log("\n<color=white>--- [Facade Triggered: PerformSpecialAttack] ---</color>");

            // 1. Play animation via the AnimationHandler.
            _animationHandler.PlayAttackAnimation(_attackAnimationTriggerName);

            // 2. Play sound effect via the AudioHandler.
            _audioHandler.PlaySoundEffect(_attackSound);

            // 3. Spawn particle effect via the ParticleEffectHandler.
            if (_effectSpawnPoint != null)
            {
                _particleEffectHandler.SpawnEffect(_attackEffectPrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
            }
            else
            {
                // Fallback if spawn point isn't set.
                Debug.LogWarning("<color=orange>[Facade]</color> Effect spawn point not assigned. Spawning effect at Facade's position.");
                _particleEffectHandler.SpawnEffect(_attackEffectPrefab, transform.position, Quaternion.identity);
            }

            // 4. Apply damage to the target via the DamageHandler.
            _damageHandler.ApplyDamage(_baseDamage, targetEnemy);

            // 5. Update UI, indicating the start of the cooldown, via the UIHandler.
            _lastAttackTime = Time.time; // Reset cooldown timer.
            _uiHandler.UpdateSpecialAttackUI(0.0f); // Set to 0 to indicate cooldown has just started.

            Debug.Log("<color=white>--- [Facade Action Complete] ---\n</color>");
            return true; // Attack successfully performed.
        }
    }


    // --- Client Class (Interacts only with the Facade) ---

    /// <summary>
    /// Example Client class that uses the SpecialAttackFacade.
    /// This client demonstrates how a higher-level system (e.g., a player controller)
    /// interacts with the simplified interface provided by the Facade.
    /// It does not need to know the complex internal workings or dependencies of the attack.
    /// </summary>
    public class FacadeClient : MonoBehaviour
    {
        [Header("Facade and Target References")]
        [Tooltip("Reference to the SpecialAttackFacade in the scene.")]
        [SerializeField] private SpecialAttackFacade _specialAttackFacade;
        [Tooltip("The GameObject representing the target enemy for the attack.")]
        [SerializeField] private GameObject _targetEnemy;

        void Update()
        {
            // Check for user input (e.g., Spacebar) to trigger the special attack.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_specialAttackFacade != null)
                {
                    Debug.Log("<color=lightblue>[Client]</color> Player attempting to use special attack...");
                    // The client simply calls the single, simplified method on the Facade.
                    // It doesn't know or care about animations, sounds, particles, damage, or UI updates.
                    _specialAttackFacade.PerformSpecialAttack(_targetEnemy);
                }
                else
                {
                    Debug.LogError("<color=red>[Client]</color> SpecialAttackFacade reference is missing! Please assign it in the Inspector.");
                }

                if (_targetEnemy == null)
                {
                    Debug.LogWarning("<color=orange>[Client]</color> Target Enemy is null. The attack will still proceed, but no damage will be logged.");
                }
            }
        }
    }
}
```

---

### How to Set Up and Use in Unity:

1.  **Create a C# Script:**
    *   In your Unity Project window, create a new C# script named `SpecialAttackFacade.cs`.
    *   Copy and paste the entire code block provided above into this new script.

2.  **Create a Facade GameObject:**
    *   In your Unity scene, create an empty GameObject (Right-click in Hierarchy -> Create Empty).
    *   Rename it to `PlayerCharacter` (or `SpecialAttackManager`).
    *   Drag and drop the `SpecialAttackFacade.cs` script onto this `PlayerCharacter` GameObject in the Inspector. This will attach the `SpecialAttackFacade` component.

3.  **Configure the `PlayerCharacter` GameObject (Facade) in the Inspector:**
    *   **Animator:** The `SpecialAttackFacade` has `[RequireComponent(typeof(Animator))]` and `[RequireComponent(typeof(AudioSource))]`. These will be automatically added if not present.
        *   To simulate animation, add an `Animator` component (if not already there). You don't need a complex Animator Controller for this example, but if you want to test the trigger, create a simple Animator Controller (Right-click in Project window -> Create -> Animator Controller), name it `PlayerAnimator`, and drag it into the `Controller` slot of the `Animator` component. Inside `PlayerAnimator`, add a `Trigger` parameter named `SpecialAttack` (case-sensitive, matching `_attackAnimationTriggerName` in the script).
    *   **AudioSource:** Add an `AudioSource` component (if not already there).
    *   **Attack Sound:** Drag an `AudioClip` (e.g., a simple "zap" or "hit" sound from your Assets) into this slot. If you don't have one, leave it blank; the console will show warnings for missing sound.
    *   **Attack Effect Prefab:** Create a simple particle system (GameObject -> Effects -> Particle System), then drag it from your Hierarchy into your Project window to create a prefab. Drag this new prefab into the `Attack Effect Prefab` slot.
    *   **Effect Spawn Point:** Create an empty GameObject as a child of `PlayerCharacter` (e.g., named `HandPosition` or `EffectOrigin`). Position this child where you'd like the particle effect to appear (e.g., slightly in front of the character). Drag this child GameObject into the `Effect Spawn Point` slot on the `SpecialAttackFacade` component.
    *   **Base Damage:** Set a value, e.g., `50`.
    *   **Attack Animation Trigger Name:** Keep it `SpecialAttack` or change it to match your Animator Controller's trigger name.
    *   **Cooldown Duration:** Set a value, e.g., `3`.

4.  **Create an Enemy GameObject:**
    *   Create another empty GameObject in your scene.
    *   Rename it to `Enemy`. This will serve as the target for the attack. You don't need to add any components to it for this example, as damage is simulated with `Debug.Log`.

5.  **Create a Client GameObject:**
    *   Create a third empty GameObject.
    *   Rename it to `GameController` (or `PlayerInput`).
    *   Drag and drop the *same* `SpecialAttackFacade.cs` script onto this `GameController` GameObject. This will attach the `FacadeClient` component (since `FacadeClient` is defined within the same file).

6.  **Configure the `GameController` GameObject (Client) in the Inspector:**
    *   Select the `GameController` GameObject.
    *   Drag the `PlayerCharacter` GameObject (which has the `SpecialAttackFacade`) from your Hierarchy into the `Special Attack Facade` slot on the `FacadeClient` component.
    *   Drag the `Enemy` GameObject from your Hierarchy into the `Target Enemy` slot on the `FacadeClient` component.

7.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   In the Game view (or just when the editor is playing), press the `Spacebar` key.
    *   Observe the Unity Console window. You should see a sequence of colored log messages demonstrating the coordinated actions of the special attack, including messages about cooldown if you try to spam the attack.

This setup clearly shows how the `FacadeClient` component only needs to interact with the `SpecialAttackFacade`, completely unaware of the underlying `AnimationHandler`, `AudioHandler`, `ParticleEffectHandler`, `DamageHandler`, and `UIHandler` that are working together. This simplifies the client's code, reduces coupling, and makes the system easier to maintain and extend.