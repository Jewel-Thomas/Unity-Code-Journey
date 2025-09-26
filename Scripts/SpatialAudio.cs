// Unity Design Pattern Example: SpatialAudio
// This script demonstrates the SpatialAudio pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'SpatialAudio' design pattern in Unity. It's important to note that "Spatial Audio" isn't a traditional Gang of Four (GoF) design pattern like Singleton or Observer. Instead, in the context of game development, it represents a *pattern of how to effectively manage and apply 3D audio features* within your game.

This `SpatialAudioSource` component embodies this pattern by:

1.  **Encapsulating Spatial Audio Logic:** It centralizes all Unity `AudioSource` spatial properties and related control methods into a single, reusable component.
2.  **Abstracting Complexity:** It provides a simplified API for playing, stopping, and dynamically adjusting spatial audio settings, shielding other scripts from direct, low-level `AudioSource` manipulation.
3.  **Promoting Reusability & Consistency:** Developers can attach this component to any GameObject that needs to emit spatial sound, ensuring consistent configuration and behavior across the project.
4.  **Enhancing Configurability:** Exposes key spatial properties directly in the Unity Inspector, making it artist-friendly and easy to tune without writing code.
5.  **Enabling Dynamic Behavior:** Offers methods to programmatically override or blend spatial properties at runtime, allowing for adaptive and immersive audio experiences (e.g., sound becoming more 2D when a player enters a menu, or less spatialized in a small room).

---

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
///     The SpatialAudioSource component demonstrates the 'SpatialAudio' design pattern
///     by providing a reusable, configurable, and dynamically controllable wrapper
///     around Unity's AudioSource for spatial sound playback.
///
///     This pattern focuses on:
///     1.  **Encapsulation:** All spatial audio configuration and control logic
///         is centralized in this single component.
///     2.  **Abstraction:** Provides a higher-level API than directly manipulating
///         raw AudioSource properties, simplifying common spatial audio tasks.
///     3.  **Reusability:** Can be attached to any GameObject that needs to emit
///         spatial sound, promoting consistent setup across the project.
///     4.  **Configurability:** Exposes key spatial properties in the Inspector
///         for artist-friendly tuning.
///     5.  **Dynamic Control:** Allows for programmatic adjustments of spatial
///         properties during runtime, enabling adaptive audio experiences.
/// </summary>
/// <remarks>
///     This component requires an AudioSource to be present on the same GameObject.
///     If one is not found, it will automatically add one.
///
///     To use this script:
///     1. Create an empty GameObject in your scene (e.g., "CampfireSound").
///     2. Attach this `SpatialAudioSource` script to it.
///     3. Assign an `AudioClip` in the Inspector.
///     4. Configure the spatial properties (blend, min/max distance, rolloff, etc.).
///     5. To play/stop the sound, get a reference to this component from another script
///        and call `Play()`, `Stop()`, `PlayOneShot()`, or `OverrideSpatialBlend()`.
/// </remarks>
[RequireComponent(typeof(AudioSource))]
public class SpatialAudioSource : MonoBehaviour
{
    [Header("Audio Clip")]
    [Tooltip("The audio clip to play with spatial characteristics.")]
    public AudioClip audioClip;

    [Tooltip("If true, the audio clip will play automatically when the scene starts.")]
    public bool playOnAwake = false;

    [Tooltip("If true, the audio clip will loop continuously.")]
    public bool loop = false;

    [Header("Spatial Properties")]
    [Range(0f, 1f)]
    [Tooltip("Determines how much the sound is affected by 3D position. 0 = 2D (no spatialization), 1 = 3D (fully spatialized).")]
    public float spatialBlend = 1.0f;

    [Tooltip("The base volume of the audio source. This is the volume before spatial attenuation and distance rolloff.")]
    public float baseVolume = 1.0f;

    [Tooltip("Determines how the volume decreases over distance.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Tooltip("The distance from the listener at which the sound starts to attenuate.")]
    public float minDistance = 1.0f;

    [Tooltip("The distance from the listener at which the sound reaches its minimum volume (usually silent).")]
    public float maxDistance = 50.0f;

    [Tooltip("Only applies if Rolloff Mode is set to Custom. Defines the custom attenuation curve.")]
    public AnimationCurve customRolloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Range(0f, 360f)]
    [Tooltip("The spread angle (in degrees) of the sound source. 0 = point source, 360 = omnidirectional.")]
    public float spread = 0f;

    [Range(0f, 5f)]
    [Tooltip("The amount of Doppler effect applied. 0 = no Doppler, higher values increase the effect.")]
    public float dopplerLevel = 1.0f;

    // Internal reference to the Unity AudioSource component
    private AudioSource _audioSource;
    private float _initialSpatialBlend; // Stores the original spatial blend for dynamic resets

    /// <summary>
    /// Gets the underlying AudioSource component managed by this SpatialAudioSource.
    /// </summary>
    public AudioSource AudioSourceComponent => _audioSource;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used here to get a reference to the AudioSource and apply initial settings.
    /// </summary>
    void Awake()
    {
        // Get or add the AudioSource component. [RequireComponent] ensures it's there.
        _audioSource = GetComponent<AudioSource>();

        // Store the initial blend value from the Inspector to allow for dynamic overrides and resets.
        _initialSpatialBlend = spatialBlend;

        // Apply all the configured spatial properties to the AudioSource component.
        ApplySpatialProperties();

        // If 'playOnAwake' is true and an audio clip is assigned, start playing immediately.
        if (playOnAwake && audioClip != null)
        {
            Play();
        }
    }

    /// <summary>
    /// Applies all the current public spatial properties to the underlying AudioSource.
    /// This method can be called dynamically if properties are changed at runtime
    /// via other scripts, ensuring the AudioSource updates its behavior.
    /// </summary>
    public void ApplySpatialProperties()
    {
        if (_audioSource == null) return;

        _audioSource.clip = audioClip;
        _audioSource.loop = loop;
        _audioSource.volume = baseVolume;
        _audioSource.spatialBlend = spatialBlend; // This is the core spatialization property
        _audioSource.rolloffMode = rolloffMode;
        _audioSource.minDistance = minDistance;
        _audioSource.maxDistance = maxDistance;
        _audioSource.spread = spread;
        _audioSource.dopplerLevel = dopplerLevel;

        // Custom rolloff curve only applies if the mode is specifically set to Custom
        if (rolloffMode == AudioRolloffMode.Custom)
        {
            _audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
        }
        // Ensure other curves are not custom if the rolloff mode changed.
        else
        {
            // Reset custom curve if mode is not custom, to prevent unexpected behavior
            _audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Linear(0, 1, 1, 0));
        }
    }

    /// <summary>
    /// Plays the currently assigned audio clip.
    /// </summary>
    public void Play()
    {
        if (_audioSource != null && audioClip != null)
        {
            // Ensure properties are up-to-date before playing, in case they were changed externally
            ApplySpatialProperties();
            _audioSource.Play();
        }
        else if (audioClip == null)
        {
            Debug.LogWarning($"No AudioClip assigned to SpatialAudioSource on {gameObject.name}. Cannot play.", this);
        }
    }

    /// <summary>
    /// Plays a specific audio clip once, overriding the default clip temporarily.
    /// This is useful for one-shot sounds like impacts, UI feedback, or short environmental cues.
    /// The spatial properties already configured will apply to this one-shot sound.
    /// </summary>
    /// <param name="clipToPlay">The audio clip to play for this one-shot event.</param>
    /// <param name="volumeScale">Optional volume scaling for this specific play call.
    /// The final volume will be baseVolume * volumeScale.</param>
    public void PlayOneShot(AudioClip clipToPlay, float volumeScale = 1.0f)
    {
        if (_audioSource != null && clipToPlay != null)
        {
            // For one-shot, we use PlayOneShot which applies existing AudioSource properties,
            // but doesn't change the main clip.
            _audioSource.PlayOneShot(clipToPlay, baseVolume * volumeScale);
        }
        else if (clipToPlay == null)
        {
            Debug.LogWarning($"No AudioClip provided for PlayOneShot on {gameObject.name}.", this);
        }
    }

    /// <summary>
    /// Stops the currently playing audio clip.
    /// </summary>
    public void Stop()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
    }

    /// <summary>
    /// Pauses the currently playing audio clip.
    /// </summary>
    public void Pause()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Pause();
        }
    }

    /// <summary>
    /// Resumes a paused audio clip.
    /// </summary>
    public void Resume()
    {
        if (_audioSource != null && _audioSource.isPaused)
        {
            _audioSource.UnPause();
        }
    }

    /// <summary>
    /// Checks if the audio source is currently playing.
    /// </summary>
    public bool IsPlaying()
    {
        return _audioSource != null && _audioSource.isPlaying;
    }

    // --- Dynamic Spatial Audio Control (Demonstrates the pattern's flexibility) ---

    /// <summary>
    /// Temporarily overrides the spatial blend value. This is useful for scenarios
    /// where the sound needs to transition between 2D (like UI) and 3D (environmental)
    /// dynamically, for example, when a character enters an interior space, a menu opens,
    /// or a specific game event makes a sound feel more "internal."
    /// </summary>
    /// <param name="newBlend">The new spatial blend value (0f for 2D, 1f for 3D).</param>
    /// <param name="resetAfterDuration">If true, the blend will smoothly revert to its
    /// initial Inspector-configured value after the duration.</param>
    /// <param name="duration">The time over which to apply the blend. If 0, it applies instantly.</param>
    public void OverrideSpatialBlend(float newBlend, bool resetAfterDuration = false, float duration = 0f)
    {
        if (_audioSource == null) return;

        StopAllCoroutines(); // Stop any ongoing blend transitions to prevent conflicts

        if (duration <= 0f)
        {
            // Apply instantly
            _audioSource.spatialBlend = newBlend;
            spatialBlend = newBlend; // Keep the public property in sync for Inspector visibility
            if (resetAfterDuration)
            {
                // If resetting instantly, just set back to initial immediately after
                _audioSource.spatialBlend = _initialSpatialBlend;
                spatialBlend = _initialSpatialBlend;
            }
        }
        else
        {
            // Smoothly transition using a coroutine
            StartCoroutine(LerpSpatialBlendRoutine(newBlend, duration, resetAfterDuration));
        }
    }

    /// <summary>
    /// Coroutine to smoothly interpolate the spatial blend value over a given duration.
    /// </summary>
    private IEnumerator LerpSpatialBlendRoutine(float targetBlend, float duration, bool resetAfterDuration)
    {
        float startBlend = _audioSource.spatialBlend;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            // Mathf.Lerp is linear, consider Mathf.SmoothStep for non-linear easing
            float currentBlend = Mathf.Lerp(startBlend, targetBlend, progress);
            _audioSource.spatialBlend = currentBlend;
            spatialBlend = currentBlend; // Keep public property in sync
            yield return null;
        }

        // Ensure the target blend is exactly reached at the end of the duration
        _audioSource.spatialBlend = targetBlend;
        spatialBlend = targetBlend;

        if (resetAfterDuration)
        {
            // After the override period, smoothly transition back to the initial blend value
            // The third parameter is 'false' because we don't want to reset after this 'reset' transition.
            yield return StartCoroutine(LerpSpatialBlendRoutine(_initialSpatialBlend, duration, false));
        }
    }

    /// <summary>
    /// Call this method to reset the spatial blend to its initial value configured in the Inspector.
    /// This will instantly revert any dynamic overrides.
    /// </summary>
    public void ResetSpatialBlend()
    {
        OverrideSpatialBlend(_initialSpatialBlend, false, 0f); // Reset instantly without further resetting
    }

    // --- Editor-only logic for better workflow ---
#if UNITY_EDITOR
    /// <summary>
    /// OnValidate is called when the script is loaded or a value is changed in the Inspector.
    /// This is useful for immediately applying changes made in the editor without
    /// having to run the game, providing instant feedback for audio designers.
    /// </summary>
    void OnValidate()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
        if (_audioSource != null)
        {
            // Re-apply properties instantly when modified in the Inspector
            ApplySpatialProperties();
            _initialSpatialBlend = spatialBlend; // Also update initial blend if changed in Inspector
        }
    }
#endif
}

/*
/// --- EXAMPLE USAGE IN COMMENTS ---
///
/// This section demonstrates how to use the SpatialAudioSource component in your Unity project.
///
/// **Scenario:**
/// You have a GameObject that should emit a spatial sound (e.g., a campfire, a monster, a spell effect).
/// You also want to trigger this sound from another script, perhaps when a player enters a trigger zone.
///
/// **Part 1: Setting up the SpatialAudioSource Component on a GameObject**
///
/// 1.  **Create a new GameObject:**
///     In the Unity Editor, right-click in the Hierarchy window -> "Create Empty".
///     Rename it, for example, "CampfireSound".
///
/// 2.  **Add the SpatialAudioSource script:**
///     Drag the 'SpatialAudioSource.cs' script onto your "CampfireSound" GameObject in the Inspector,
///     or click "Add Component" and search for "SpatialAudioSource".
///     (An `AudioSource` component will be automatically added if not already present due to `[RequireComponent]`).
///
/// 3.  **Configure in the Inspector:**
///     -   **Audio Clip:** Drag an `.mp3`, `.wav`, or other compatible audio file from your Project window into this slot.
///         (e.g., a crackling fire sound).
///     -   **Play On Awake:** Check this if you want the sound to start playing immediately when the scene loads.
///         (e.g., for ambient sounds like wind, rain, background music).
///     -   **Loop:** Check this if the sound should repeat endlessly.
///         (e.g., for constant machinery hum, campfire sound).
///     -   **Spatial Blend:** Set to `1.0` for full 3D spatialization. This means its position in the world
///         will affect how you hear it. Set to `0.0` for 2D (like UI sounds or background music that
///         doesn't come from a specific point).
///     -   **Base Volume:** Adjust the overall volume.
///     -   **Rolloff Mode:**
///         -   `Logarithmic`: Volume decreases rapidly at first, then slower. Common for natural sounds.
///         -   `Linear`: Volume decreases steadily.
///         -   `Custom`: Allows you to define your own attenuation curve using `Custom Rolloff Curve`.
///     -   **Min Distance:** The distance from the listener where the sound plays at full volume.
///     -   **Max Distance:** The distance from the listener where the sound fades completely to silence.
///         Between minDistance and maxDistance, the volume attenuates according to the `Rolloff Mode`.
///     -   **Spread:** Controls how wide the sound appears to be. 0 means it comes from a single point,
///         360 means it's diffuse.
///     -   **Doppler Level:** Controls the Doppler effect (pitch shift due to relative velocity).
///         Higher values mean more noticeable pitch changes when the source or listener moves quickly.
///
/// **Part 2: Triggering the SpatialAudioSource from another Script**
///
/// Let's say you have a `PlayerTrigger` script that plays a sound when the player enters a zone.
///
/// 1.  **Create a C# Script:**
///     In your Project window, right-click -> "Create" -> "C# Script". Name it "AudioTriggerExample".
///
/// 2.  **Add the following code to `AudioTriggerExample.cs`:**
///
```csharp
/*
using UnityEngine;

public class AudioTriggerExample : MonoBehaviour
{
    [Tooltip("Reference to the SpatialAudioSource component we want to control.")]
    public SpatialAudioSource targetSpatialAudioSource;

    [Tooltip("An optional one-shot clip to play when activated.")]
    public AudioClip triggerSoundEffect;

    [Tooltip("If true, the main audio will play/stop when player enters/exits. If false, only one-shot will play on enter.")]
    public bool controlMainAudio = true;

    [Tooltip("If true, the spatial blend of the target will temporarily become 2D, then revert.")]
    public bool applyDynamicSpatialBlend = false;

    [Tooltip("Duration for the dynamic spatial blend change.")]
    public float blendTransitionDuration = 1.0f;

    private bool _isPlayerInZone = false;

    void Start()
    {
        // Basic validation
        if (targetSpatialAudioSource == null)
        {
            Debug.LogError("AudioTriggerExample: targetSpatialAudioSource is not assigned! This script will be disabled.", this);
            enabled = false; // Disable script if essential reference is missing
        }
        // Ensure this GameObject has a trigger collider
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogWarning("AudioTriggerExample: This GameObject needs an 'Is Trigger' collider to function!", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider is the player (assuming player has "Player" tag)
        // Ensure player tag exists: Select your player GameObject, in the Inspector -> Tag dropdown -> Add Tag... -> Add "Player".
        if (other.CompareTag("Player") && !_isPlayerInZone)
        {
            Debug.Log($"{other.name} entered trigger zone. Playing spatial audio from {targetSpatialAudioSource.gameObject.name}.");

            if (targetSpatialAudioSource != null)
            {
                if (controlMainAudio)
                {
                    targetSpatialAudioSource.Play();
                }

                if (triggerSoundEffect != null)
                {
                    targetSpatialAudioSource.PlayOneShot(triggerSoundEffect, 0.8f);
                }

                if (applyDynamicSpatialBlend)
                {
                    // Example of dynamic spatial blend: make the sound temporarily 2D for a period (e.g., a menu pops up)
                    // then smoothly blend it back to its original 3D setting.
                    Debug.Log($"Temporarily changing spatial blend for {targetSpatialAudioSource.gameObject.name} to 2D for {blendTransitionDuration}s.");
                    targetSpatialAudioSource.OverrideSpatialBlend(0.0f, true, blendTransitionDuration);
                }
            }
            _isPlayerInZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && _isPlayerInZone)
        {
            Debug.Log($"{other.name} exited trigger zone. Stopping spatial audio from {targetSpatialAudioSource.gameObject.name}.");
            if (targetSpatialAudioSource != null && controlMainAudio)
            {
                targetSpatialAudioSource.Stop();
            }
            if (applyDynamicSpatialBlend)
            {
                // If we applied a dynamic blend, ensure it's reset when player leaves,
                // or if it was resetting, let it finish.
                targetSpatialAudioSource.ResetSpatialBlend();
            }
            _isPlayerInZone = false;
        }
    }
}
*/
```
/*
/// 3.  **Setup the `AudioTriggerExample` GameObject:**
///     -   Create another Empty GameObject, name it "AudioTriggerZone".
///     -   Add a `BoxCollider` component to it.
///     -   Check the "Is Trigger" checkbox on the `BoxCollider`.
///     -   Adjust the collider's size (e.g., using the Transform scale) to define your trigger zone.
///     -   Add the `AudioTriggerExample` script to this "AudioTriggerZone" GameObject.
///     -   In the Inspector for "AudioTriggerZone", drag your "CampfireSound" GameObject
///         (which has the `SpatialAudioSource`) into the `Target Spatial Audio Source` slot.
///     -   Optionally, assign an `AudioClip` to `Trigger Sound Effect` for an additional sound.
///     -   Check `Apply Dynamic Spatial Blend` to see the `OverrideSpatialBlend` method in action.
///
/// **Part 3: Ensure your Player has a Tag and Collider**
///
/// -   Make sure your player character GameObject has a `CharacterController` or `Collider` component (e.g., `CapsuleCollider`).
/// -   Ensure your player character GameObject has the tag "Player" (you might need to add this tag:
///     Select your Player GameObject, go to the Inspector, click the "Tag" dropdown, select "Add Tag...",
///     click the `+` button, type "Player", then go back to your Player GameObject and assign the "Player" tag).
/// -   The Player's `Rigidbody` should NOT be kinematic for collision detection. If your player is moved by `Transform` directly, it needs a `Rigidbody` (and `isKinematic` checked). If moved by `CharacterController`, no `Rigidbody` is strictly needed for triggers, but it's good practice for general physics.
///
/// **How this demonstrates the 'SpatialAudio' Pattern:**
///
/// -   **Centralized Control:** Instead of `AudioTriggerExample` directly manipulating `AudioSource` properties (like `audioSource.spatialBlend = 0.5f;`),
///     it interacts with `SpatialAudioSource`, which handles all the complex spatial configurations. This makes `AudioTriggerExample` simpler and focused on its core logic.
/// -   **Ease of Use:** Developers only need to worry about `Play()`, `Stop()`, `PlayOneShot()`,
///     and `OverrideSpatialBlend()` on the `SpatialAudioSource`, not the individual `spatialBlend`, `minDistance`, `rolloffMode`, etc., every time they want to play a sound.
/// -   **Maintainability:** If Unity changes how spatial audio works (e.g., introduces a new property), only `SpatialAudioSource` needs updating,
///     not every script that plays sound in the project.
/// -   **Modularity:** `SpatialAudioSource` can be reused across countless different types of sound-emitting GameObjects
///     (e.g., a torch, a distant generator, a magical orb) each with unique spatial settings, without code duplication.
/// -   **Dynamic Behavior:** The `OverrideSpatialBlend` method shows how the pattern can go beyond static
///     configuration and react to game events, like altering how spatialized a sound is when entering
///     a specific environment or triggering a special ability.
///
/// This makes the process of adding and managing spatial audio much more structured, efficient, and less error-prone
/// in a large Unity project.
*/
```