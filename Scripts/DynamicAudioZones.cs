// Unity Design Pattern Example: DynamicAudioZones
// This script demonstrates the DynamicAudioZones pattern in Unity
// Generated automatically - ready to use in your Unity project

The Dynamic Audio Zones design pattern provides a robust and flexible way to manage audio in games, allowing music, ambient sounds, and even sound effects to change dynamically based on the player's location within defined areas. This example implements a practical version of the pattern in Unity.

### DynamicAudioZones Design Pattern Explained

**Core Idea:** Audio playback is controlled by the player's interaction with predefined "zones" in the game world. When the player enters or exits a zone, the active audio state is updated, often with smooth transitions like fading.

**Key Components:**

1.  **`AudioZoneManager` (Singleton):**
    *   **Role:** The central brain of the system. It's a singleton to ensure only one instance manages all audio.
    *   **Responsibilities:**
        *   Receives notifications from `AudioZone` components when the player enters or exits them.
        *   Maintains a list of currently active `AudioZone`s.
        *   Determines which zone's audio settings should be active based on a priority system (e.g., innermost or highest priority zone takes precedence).
        *   Manages `AudioSource` components for different audio types (music, ambient).
        *   Handles smooth transitions (fading in/out) between different audio clips or volume levels.
        *   Manages default audio when no specific zone is active.

2.  **`AudioZone` (Component):**
    *   **Role:** Represents a specific area in the game world that has unique audio properties.
    *   **Responsibilities:**
        *   Attached to a GameObject with a `Collider` set as a trigger.
        *   Holds references to its specific `AudioClip`s (music, ambient) and desired volume levels.
        *   Has a `Priority` value to resolve conflicts when zones overlap.
        *   On `OnTriggerEnter` and `OnTriggerExit` events, it notifies the `AudioZoneManager` about the player's presence change.
        *   Provides editor visualization (Gizmos) for easy setup.

3.  **Player/Listener:**
    *   **Role:** The entity whose position dictates the active audio zone.
    *   **Requirements:** Must have a `Collider` (e.g., `CharacterController`, `CapsuleCollider`) and be tagged with a specific identifier (e.g., "Player") so the `AudioZone` can detect it. It should also have a `Rigidbody` (can be kinematic) for trigger events to work reliably.

**How it Works (Flow):**

1.  **Initialization:** The `AudioZoneManager` initializes itself as a singleton and sets up default audio.
2.  **Player Movement:** As the player moves through the world, their `Collider` interacts with the `is Trigger` colliders of `AudioZone` GameObjects.
3.  **Zone Entry:**
    *   When the player enters an `AudioZone`'s trigger, the `AudioZone` calls `AudioZoneManager.Instance.OnZoneEnter(this)`.
    *   The `AudioZoneManager` adds this zone to its list of active zones.
    *   It then re-evaluates all active zones, sorts them by `Priority` (highest first), and applies the audio settings of the highest-priority zone.
4.  **Zone Exit:**
    *   When the player exits an `AudioZone`'s trigger, the `AudioZone` calls `AudioZoneManager.Instance.OnZoneExit(this)`.
    *   The `AudioZoneManager` removes this zone from its list of active zones.
    *   It again re-evaluates the remaining active zones, sorts them, and applies the audio settings of the new highest-priority zone.
    *   If no zones are active, it reverts to the manager's default audio settings.
5.  **Audio Transitions:** The `AudioZoneManager` handles the actual playback, ensuring smooth fading between clips and adjusting volumes using Coroutines.

---

### Complete C# Unity Example

Here are the two scripts (`AudioZoneManager.cs` and `AudioZone.cs`) that implement the Dynamic Audio Zones pattern.

#### 1. `AudioZoneManager.cs`

This script manages the audio playback. It should be placed on a dedicated GameObject in your scene.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for OrderByDescending

/// <summary>
/// Manages dynamic audio playback based on the player's presence in various AudioZones.
/// Implements the core of the Dynamic Audio Zones design pattern using a singleton.
/// It tracks active zones, handles audio transitions (fades), and ensures the correct
/// ambient and music tracks are playing based on zone priority.
/// </summary>
public class AudioZoneManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures there's only one instance of AudioZoneManager throughout the game.
    public static AudioZoneManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The AudioSource for background music. Set 'Play On Awake' to false and 'Loop' to true.")]
    [SerializeField] private AudioSource _musicSource;
    [Tooltip("The AudioSource for ambient sounds. Set 'Play On Awake' to false and 'Loop' to true.")]
    [SerializeField] private AudioSource _ambientSource;

    [Header("Default Audio Settings (when no zone is active)")]
    [Tooltip("The music clip to play when no specific audio zone is active.")]
    [SerializeField] private AudioClip _defaultMusicClip;
    [Tooltip("The default volume for music when no zone is active.")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultMusicVolume = 0.6f;
    [Tooltip("The ambient sound clip to play when no specific audio zone is active.")]
    [SerializeField] private AudioClip _defaultAmbientClip;
    [Tooltip("The default volume for ambient sounds when no zone is active.")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultAmbientVolume = 0.3f;

    [Header("Transition Settings")]
    [Tooltip("Default duration for fading between audio clips.")]
    [SerializeField] private float _defaultFadeDuration = 2.0f;

    // --- Internal State ---
    // List of currently active AudioZone objects.
    // Sorted by priority to quickly access the highest priority zone.
    private List<AudioZone> _activeZones = new List<AudioZone>(); 

    // References to ongoing fade coroutines to allow stopping them if a new fade is initiated.
    private Coroutine _musicFadeCoroutine;
    private Coroutine _ambientFadeCoroutine;

    // Keep track of the currently playing clips to avoid unnecessary re-fades if the same clip is targeted.
    private AudioClip _currentMusicClip;
    private AudioClip _currentAmbientClip;

    private void Awake()
    {
        // Enforce singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // Set this as the singleton instance.
            Instance = this;
            // Make the manager persist across scene loads.
            DontDestroyOnLoad(gameObject); 

            // Ensure AudioSources are assigned. If not, add them.
            if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
            if (_ambientSource == null) _ambientSource = gameObject.AddComponent<AudioSource>();

            // Configure AudioSources for background music and ambient sounds.
            _musicSource.playOnAwake = false; // Don't play immediately
            _musicSource.loop = true;         // Loop music
            _musicSource.spatialBlend = 0f;   // 2D sound for music

            _ambientSource.playOnAwake = false; // Don't play immediately
            _ambientSource.loop = true;           // Loop ambient sounds
            _ambientSource.spatialBlend = 0f;     // 2D sound for ambient (can be adjusted for 3D ambient)

            // Apply default audio settings when the manager first starts.
            ApplyAudioSettings(null); // Passing null triggers default settings.
        }
    }

    /// <summary>
    /// Called by an AudioZone when the player enters its trigger.
    /// This method adds the zone to the active zones list and updates the audio.
    /// </summary>
    /// <param name="zone">The AudioZone that was entered.</param>
    public void OnZoneEnter(AudioZone zone)
    {
        // Only add the zone if it's not already in the active list (prevents duplicates from overlapping triggers).
        if (!_activeZones.Contains(zone))
        {
            _activeZones.Add(zone);
            Debug.Log($"Player entered zone: {zone.ZoneName}. Active zones count: {_activeZones.Count}");
        }
        SortAndApplyAudio(); // Re-evaluate and apply audio based on current active zones.
    }

    /// <summary>
    /// Called by an AudioZone when the player exits its trigger.
    /// This method removes the zone from the active zones list and updates the audio.
    /// </summary>
    /// <param name="zone">The AudioZone that was exited.</param>
    public void OnZoneExit(AudioZone zone)
    {
        // Only remove the zone if it's actually in the active list.
        if (_activeZones.Contains(zone))
        {
            _activeZones.Remove(zone);
            Debug.Log($"Player exited zone: {zone.ZoneName}. Active zones count: {_activeZones.Count}");
        }
        SortAndApplyAudio(); // Re-evaluate and apply audio based on remaining active zones.
    }

    /// <summary>
    /// Sorts the active zones by priority (highest priority first) and applies the audio settings
    /// of the highest-priority zone. If no zones are active, it applies the default audio settings.
    /// </summary>
    private void SortAndApplyAudio()
    {
        if (_activeZones.Count > 0)
        {
            // Sort the list of active zones by their Priority in descending order.
            // This ensures the zone with the highest priority is always at index 0.
            _activeZones = _activeZones.OrderByDescending(z => z.Priority).ToList();
            ApplyAudioSettings(_activeZones[0]); // Apply settings of the highest priority zone.
        }
        else
        {
            // If no zones are active, apply the default audio settings.
            ApplyAudioSettings(null);
        }
    }

    /// <summary>
    /// Applies the music and ambient audio settings from the given AudioZone.
    /// Handles fading in/out clips and adjusting volumes. If 'zone' is null,
    /// the default audio settings configured in the manager are used.
    /// </summary>
    /// <param name="zone">The AudioZone whose settings to apply, or null for default settings.</param>
    private void ApplyAudioSettings(AudioZone zone)
    {
        // Determine the target clips, volumes, and fade duration based on the provided zone.
        // If zone is null, use the manager's default settings.
        AudioClip targetMusicClip = (zone != null) ? zone.MusicClip : _defaultMusicClip;
        float targetMusicVolume = (zone != null) ? zone.MusicVolume : _defaultMusicVolume;
        AudioClip targetAmbientClip = (zone != null) ? zone.AmbientClip : _defaultAmbientClip;
        float targetAmbientVolume = (zone != null) ? zone.AmbientVolume : _defaultAmbientVolume;
        
        // Use the zone's fade duration if it's explicitly set and positive; otherwise, use the manager's default.
        float fadeDuration = (zone != null && zone.FadeDuration > 0) ? zone.FadeDuration : _defaultFadeDuration;

        // --- Music Audio Management ---
        // Check if the target music clip is different from the one currently being played/targeted.
        if (_currentMusicClip != targetMusicClip)
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine); // Stop any previous music fade.
            // Start a new coroutine to fade the music source.
            _musicFadeCoroutine = StartCoroutine(FadeAudioSource(_musicSource, targetMusicClip, targetMusicVolume, fadeDuration));
            _currentMusicClip = targetMusicClip; // Update the internally tracked current music clip.
        }
        // If the clip is the same but the target volume has changed, or the clip should be playing but isn't.
        else if (targetMusicClip != null && (_musicSource.isPlaying && Mathf.Abs(_musicSource.volume - targetMusicVolume) > 0.01f || !_musicSource.isPlaying))
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            // If the clip is the same but the volume needs adjustment, fade volume only.
            _musicFadeCoroutine = StartCoroutine(FadeAudioSource(_musicSource, targetMusicClip, targetMusicVolume, fadeDuration, onlyVolume: true));
        }
        // If target is null but something is still playing, fade out and stop.
        else if (targetMusicClip == null && _musicSource.isPlaying)
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeAudioSource(_musicSource, null, 0f, fadeDuration));
        }


        // --- Ambient Audio Management (Logic is similar to Music) ---
        if (_currentAmbientClip != targetAmbientClip)
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAudioSource(_ambientSource, targetAmbientClip, targetAmbientVolume, fadeDuration));
            _currentAmbientClip = targetAmbientClip;
        }
        else if (targetAmbientClip != null && (_ambientSource.isPlaying && Mathf.Abs(_ambientSource.volume - targetAmbientVolume) > 0.01f || !_ambientSource.isPlaying))
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAudioSource(_ambientSource, targetAmbientClip, targetAmbientVolume, fadeDuration, onlyVolume: true));
        }
        else if (targetAmbientClip == null && _ambientSource.isPlaying)
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAudioSource(_ambientSource, null, 0f, fadeDuration));
        }
    }

    /// <summary>
    /// Coroutine to smoothly fade an AudioSource's volume and potentially change its clip.
    /// Handles both cross-fading to a new clip and just adjusting the volume of the current clip.
    /// </summary>
    /// <param name="source">The AudioSource to control (e.g., _musicSource or _ambientSource).</param>
    /// <param name="targetClip">The new clip to play after fading out (can be null to just fade out and stop).</param>
    /// <param name="targetVolume">The target volume for the new clip (or for the current clip if onlyVolume is true).</param>
    /// <param name="duration">The total duration of the fade transition.</param>
    /// <param name="onlyVolume">If true, only adjusts volume of the current clip; no clip change logic.</param>
    private IEnumerator FadeAudioSource(AudioSource source, AudioClip targetClip, float targetVolume, float duration, bool onlyVolume = false)
    {
        float startVolume = source.volume;
        float timer = 0f;

        // --- Phase 1: Fade out current audio if a new clip is being introduced or source should stop ---
        // This only happens if we're not just adjusting volume, AND the source is playing,
        // AND either the clip is changing OR the target is to stop playing (targetClip is null).
        if (!onlyVolume && source.isPlaying && (source.clip != targetClip || targetClip == null))
        {
            while (timer < duration / 2f) // Fade out for half the total duration
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2f));
                yield return null;
            }
            source.Stop(); // Stop the source once faded out
        }

        // --- Phase 2: Prepare and fade in new audio (if a target clip exists) ---
        if (targetClip != null)
        {
            // If the clip is changing, assign the new clip and set volume to 0 to start fade-in.
            if (source.clip != targetClip)
            {
                source.clip = targetClip;
                source.volume = 0f;
            }
            
            // Ensure the audio source is playing before fading in.
            if (!source.isPlaying)
            {
                 source.Play();
            }

            // Fade in new audio (or adjust volume of existing audio).
            timer = 0f;
            startVolume = source.volume; // Re-read start volume in case it was already playing at some volume
            // If only volume is true, use the full duration for the fade. Otherwise, use the remaining half.
            float fadeInDuration = onlyVolume ? duration : duration / 2f; 
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeInDuration);
                yield return null;
            }
            source.volume = targetVolume; // Ensure it reaches the exact target volume at the end.
        }
        else
        {
            // If targetClip is null (meaning we want to stop audio), ensure it's fully stopped and volume is 0.
            source.Stop();
            source.volume = 0f;
        }
    }

    // --- Public accessors for debugging or external control (optional) ---
    public AudioSource MusicSource => _musicSource;
    public AudioSource AmbientSource => _ambientSource;
}

```

#### 2. `AudioZone.cs`

This script defines an individual audio zone. Attach it to any GameObject that you want to act as an audio zone, along with a Collider.

```csharp
using UnityEngine;
// Using UnityEditor for Handles.Label in Gizmos, only compiles in editor.
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Represents a single audio zone in the game world.
/// When the player enters this zone's trigger, it notifies the AudioZoneManager
/// to potentially change the background music and/or ambient sounds.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a collider is present for trigger detection
public class AudioZone : MonoBehaviour
{
    // [field: SerializeField] allows auto-implemented properties to be shown in the Inspector.
    [field: SerializeField, Tooltip("A descriptive name for this zone (e.g., 'Forest Clearing', 'Underground Cave').")]
    public string ZoneName { get; private set; } = "New Audio Zone";

    [field: SerializeField, Tooltip("Higher priority zones will override lower priority zones if they overlap. (0=lowest, 100=highest)")]
    [field: Range(0, 100)]
    public int Priority { get; private set; } = 10;

    [field: Header("Audio Clips")]
    [field: SerializeField, Tooltip("The music clip to play when this zone is active.")]
    public AudioClip MusicClip { get; private set; }

    [field: SerializeField, Tooltip("The ambient sound clip to play when this zone is active.")]
    public AudioClip AmbientClip { get; private set; }

    [field: Header("Volume Settings")]
    [field: SerializeField, Tooltip("The target volume for music when this zone is active."), Range(0f, 1f)]
    public float MusicVolume { get; private set; } = 0.7f;

    [field: SerializeField, Tooltip("The target volume for ambient sounds when this zone is active."), Range(0f, 1f)]
    public float AmbientVolume { get; private set; } = 0.5f;

    [field: Header("Transition Settings")]
    [field: SerializeField, Tooltip("The duration for fading audio when entering/exiting this zone. Overrides manager's default if > 0.")]
    public float FadeDuration { get; private set; } = -1f; // Use manager's default if <= 0

    // A cached reference to the collider to ensure it's a trigger.
    private Collider _zoneCollider;

    void Awake()
    {
        // Get the collider component attached to this GameObject.
        _zoneCollider = GetComponent<Collider>();
        if (_zoneCollider == null)
        {
            Debug.LogError($"AudioZone '{ZoneName}' on GameObject '{gameObject.name}' is missing a Collider component! This script requires one.", this);
            enabled = false; // Disable script if no collider to prevent further errors.
            return;
        }

        // Ensure the collider is set to 'Is Trigger' for proper detection.
        if (!_zoneCollider.isTrigger)
        {
            Debug.LogWarning($"AudioZone '{ZoneName}' on GameObject '{gameObject.name}' has a non-trigger collider. Setting it to 'Is Trigger'.", this);
            _zoneCollider.isTrigger = true;
        }

        // Check if AudioZoneManager is present in the scene.
        if (AudioZoneManager.Instance == null)
        {
            Debug.LogError("AudioZoneManager not found in the scene! Please add an AudioZoneManager GameObject.", this);
            enabled = false; // Disable script if manager is missing.
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Only react if the entering collider is tagged as "Player".
        // Ensure your player GameObject has this tag.
        if (other.CompareTag("Player"))
        {
            AudioZoneManager.Instance.OnZoneEnter(this);
        }
    }

    /// <summary>
    /// Called when another collider exits this trigger.
    /// </summary>
    /// <param name="other">The collider that exited.</param>
    private void OnTriggerExit(Collider other)
    {
        // Only react if the exiting collider is tagged as "Player".
        if (other.CompareTag("Player"))
        {
            AudioZoneManager.Instance.OnZoneExit(this);
        }
    }

    // --- Gizmos for Editor Visualization ---
    // This method draws visual helpers in the Unity Editor to represent the audio zone.
    void OnDrawGizmos()
    {
        _zoneCollider = GetComponent<Collider>();
        if (_zoneCollider == null) return; // Don't draw if no collider.

        Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent color for the zone.

        // Store current Gizmos matrix to restore it later.
        Matrix4x4 originalMatrix = Gizmos.matrix;
        // Apply transform's position, rotation, and scale to Gizmos drawing.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        // Draw the specific collider shape.
        if (_zoneCollider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0, 1, 1, 0.7f); // Opaque cyan for wireframe.
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (_zoneCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0, 1, 1, 0.7f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (_zoneCollider is CapsuleCollider capsule)
        {
            // Drawing a capsule accurately with Gizmos is complex.
            // This provides a basic representation (sphere + bounds).
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;

            // Draw a semi-transparent sphere at the center.
            Gizmos.DrawSphere(center, radius);
            Gizmos.color = new Color(0, 1, 1, 0.7f);
            // Draw a wire cube representing the bounds for better visualization.
            Gizmos.DrawWireCube(center, new Vector3(radius * 2, height, radius * 2));
        }

        Gizmos.matrix = originalMatrix; // Restore original Gizmos matrix.

        // Draw the zone name and priority label above the zone in the Editor.
        #if UNITY_EDITOR // This block only compiles in the Unity Editor.
        Handles.Label(transform.position + Vector3.up * (_zoneCollider.bounds.extents.y + 0.5f), $"[{Priority}] {ZoneName}");
        #endif
    }
}
```

---

### Example Usage and Setup in Unity

Follow these steps to implement the DynamicAudioZones pattern in your Unity project:

1.  **Create Audio Clips:** Import your music and ambient sound `.mp3` or `.wav` files into your Unity project.

2.  **Setup the `AudioZoneManager`:**
    *   Create an empty GameObject in your scene and name it `AudioZoneManager`.
    *   Attach the `AudioZoneManager.cs` script to this GameObject.
    *   In the Inspector, Unity will automatically add two `AudioSource` components because they are referenced as `[SerializeField]` and initially null.
    *   **Configure the `AudioZoneManager`:**
        *   Drag your default music clip to `Default Music Clip`.
        *   Adjust `Default Music Volume`.
        *   Drag your default ambient clip to `Default Ambient Clip`.
        *   Adjust `Default Ambient Volume`.
        *   Set the `Default Fade Duration` (e.g., 2.0 seconds).

3.  **Setup your Player GameObject:**
    *   Ensure your Player GameObject (e.g., a Character Controller, a simple Cube, or your main character prefab) has a `Collider` component (e.g., `CapsuleCollider`, `BoxCollider`).
    *   Ensure its `Tag` is set to **"Player"**. (You can add this tag in the Inspector if it doesn't exist).
    *   For trigger events to work correctly, the player's GameObject should also have a `Rigidbody` component. If your player is controlled by a `CharacterController` or you don't want physics interactions, set the `Rigidbody`'s `Is Kinematic` property to `true`.

4.  **Create Audio Zones:**
    *   Create several empty GameObjects in your scene (e.g., "ForestZone", "CaveZone", "TownZone").
    *   Attach the `AudioZone.cs` script to each of these zone GameObjects.
    *   Add a `Collider` component to each zone GameObject (e.g., `BoxCollider`, `SphereCollider`).
    *   **Crucially, set the `Is Trigger` property of each zone's Collider to `true`.**
    *   Scale and position the colliders to define the boundaries of your audio zones.

5.  **Configure each `AudioZone`:**
    *   Select an `AudioZone` GameObject.
    *   In the Inspector, fill in its properties:
        *   `Zone Name`: A descriptive name (e.g., "Deep Forest", "Market Square").
        *   `Priority`: A number (0-100). Higher numbers mean higher priority. If zones overlap, the one with the highest priority will control the audio.
        *   `Music Clip`: Drag the music track for this specific zone.
        *   `Ambient Clip`: Drag the ambient sound for this specific zone.
        *   `Music Volume` & `Ambient Volume`: Adjust the desired volumes for this zone.
        *   `Fade Duration`: Optionally set a specific fade duration for *this* zone, overriding the manager's default. If 0 or negative, the manager's default will be used.

**Example Scenario:**

*   **Default (no zone):** Quiet background music, gentle wind ambient. (Set in `AudioZoneManager`)
*   **ForestZone:** `Priority: 10`, `Music: ForestTheme`, `Ambient: Birdsong & Rustling Leaves`.
*   **CaveZone:** `Priority: 20`, `Music: CreepyDungeonTheme`, `Ambient: Dripping Water & Echoes`.
*   **TownZone:** `Priority: 15`, `Music: TownSquareMelody`, `Ambient: DistantChatter & MarketSounds`.
*   **MarketStall (inside TownZone):** `Priority: 30`, `Music: UpbeatMarketTune`, `Ambient: SpecificVendorCalls`, `FadeDuration: 0.5` (faster fade).

If the player enters "TownZone", the town music and ambient fade in. If they then walk into "MarketStall" (which is inside "TownZone" and has higher priority), the `AudioZoneManager` will switch to "MarketStall"'s audio, fading out the town audio and fading in the market audio. When the player leaves "MarketStall" but is still in "TownZone", the manager will revert to "TownZone"'s audio. Finally, when leaving "TownZone", it will revert to the default audio.

This complete example provides a robust, educational, and practical implementation of the Dynamic Audio Zones design pattern in Unity.