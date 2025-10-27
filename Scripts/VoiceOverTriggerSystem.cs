// Unity Design Pattern Example: VoiceOverTriggerSystem
// This script demonstrates the VoiceOverTriggerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This solution provides a complete, practical, and well-documented C# Unity example for the 'VoiceOverTriggerSystem' design pattern. It's broken down into four scripts:

1.  **`VoiceOverClipData.cs`**: A ScriptableObject to define individual voice-over clips and their metadata.
2.  **`VoiceOverManager.cs`**: A singleton MonoBehaviour that handles playing, queuing, and managing voice-over playback.
3.  **`VoiceOverTrigger.cs`**: A MonoBehaviour that defines how and when voice-overs are initiated in the scene (e.g., entering a zone, interaction).
4.  **`SimpleSubtitleDisplay.cs`**: An example UI script demonstrating how to subscribe to manager events to display subtitles.

---

### 1. `VoiceOverClipData.cs`

This ScriptableObject acts as the data container for a single voice-over audio clip. By using ScriptableObjects, you can create and manage your voice-over assets directly in the Unity Editor, making them reusable and easy to organize.

```csharp
using UnityEngine;

/// <summary>
/// Represents a single voice-over clip with its audio, subtitle, and playback rules.
/// This is a ScriptableObject, allowing you to create individual voice-over assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewVoiceOverClip", menuName = "VoiceOver System/VoiceOver Clip Data")]
public class VoiceOverClipData : ScriptableObject
{
    [Header("Audio Settings")]
    [Tooltip("The actual audio clip to play for this voice-over.")]
    public AudioClip audioClip;

    [Tooltip("Volume multiplier for this specific clip. 1 = full volume.")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Optional delay before this clip starts playing after it's activated by the manager.")]
    public float delayBeforePlay = 0f;

    [Header("Subtitle Settings")]
    [Tooltip("The text to display as a subtitle while this voice-over plays.")]
    [TextArea(3, 6)] // Makes the string field a multi-line text area in the Inspector
    public string subtitleText;

    [Tooltip("Optional name of the character speaking (for UI display, e.g., 'Narrator', 'Guard').")]
    public string characterName;

    [Header("Playback Rules")]
    [Tooltip("A unique identifier for this voice-over. Used to track if it has been played before (if 'playOnlyOnce' is true). " +
             "Ensure this is unique across all your VoiceOverClipData assets.")]
    public string voiceOverID;

    [Tooltip("If true, this voice-over will only play once per game session (or until 'ResetPlayedVoiceOvers' is called on the manager).")]
    public bool playOnlyOnce = false;

    [Tooltip("If true, this voice-over can be interrupted by another incoming voice-over that wants to play immediately.")]
    public bool canBeInterrupted = true;

    [Tooltip("If true, other voice-overs should wait for this one to finish before starting. " +
             "This prevents incoming 'PlayVoiceOver' calls from interrupting this one unless 'forcePlay' is used.")]
    public bool blocksOtherVoiceOvers = true;


    // --- Editor-time Validation/Utility ---
    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Used here to provide a default 'voiceOverID' if it's empty.
    /// </summary>
    void OnValidate()
    {
        // Ensure ID is not empty. If it's a new asset, suggest a name based on asset name.
        if (string.IsNullOrEmpty(voiceOverID))
        {
            // Use the asset's name as a default ID for easier identification.
            voiceOverID = this.name;
        }
    }
}
```

---

### 2. `VoiceOverManager.cs`

This is the central hub of the VoiceOverTriggerSystem. It's implemented as a `Singleton` to provide easy global access. It manages an `AudioSource`, a queue of voice-overs, and handles playback logic, including interruption, sequencing, and "play only once" rules. It also provides `events` for UI components (like subtitle display) to subscribe to.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action events

/// <summary>
/// The central manager for playing, queuing, and stopping voice-overs.
/// Implemented as a Singleton for easy global access.
/// </summary>
public class VoiceOverManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global point of access to the VoiceOverManager instance.
    public static VoiceOverManager Instance { get; private set; }

    [Header("Core Setup")]
    [Tooltip("The AudioSource component used to play voice-over clips. " +
             "It's recommended to create a dedicated GameObject for this, or attach it here. " +
             "If none is assigned, one will be added automatically.")]
    [SerializeField] private AudioSource voiceOverAudioSource;

    [Tooltip("If true, voice-overs and manager actions will be logged to the console for debugging.")]
    [SerializeField] private bool enableDebugLogs = true;

    // --- Internal State ---
    private Queue<VoiceOverClipData> voiceOverQueue = new Queue<VoiceOverClipData>();
    private VoiceOverClipData currentVoiceOver;
    private Coroutine playRoutine; // Reference to the currently running playback coroutine.

    // Keeps track of voice-overs that have already been played, based on their unique ID.
    // This supports the 'playOnlyOnce' functionality per game session.
    private HashSet<string> playedVoiceOverIDs = new HashSet<string>();

    // --- Events for UI and other systems to subscribe to ---
    // Example usage: VoiceOverManager.OnVoiceOverStarted += MySubtitleDisplayScript.DisplaySubtitle;
    /// <summary>Fired when a voice-over starts playing.</summary>
    public static event Action<VoiceOverClipData> OnVoiceOverStarted;
    /// <summary>Fired when a voice-over finishes playing naturally.</summary>
    public static event Action<VoiceOverClipData> OnVoiceOverFinished;
    /// <summary>Fired when a voice-over's subtitle text should be displayed/updated (characterName, subtitleText).</summary>
    public static event Action<string, string> OnSubtitleUpdate;
    /// <summary>Fired when no voice-over is playing and subtitles should be hidden.</summary>
    public static event Action OnSubtitleHidden;

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances.
            return;
        }
        Instance = this;
        // Keep the manager alive across scene changes, important for game-wide voice-overs.
        DontDestroyOnLoad(gameObject); 

        // Ensure an AudioSource is present. If not, try to get one or add one.
        if (voiceOverAudioSource == null)
        {
            voiceOverAudioSource = GetComponent<AudioSource>();
            if (voiceOverAudioSource == null)
            {
                voiceOverAudioSource = gameObject.AddComponent<AudioSource>();
                LogWarning("No AudioSource assigned to VoiceOverManager. Added one automatically. " +
                           "Consider assigning a pre-configured AudioSource in the Inspector for better control (e.g., output mixer).");
            }
        }

        // Basic AudioSource setup for voice-overs.
        voiceOverAudioSource.playOnAwake = false; // We control playback manually.
        voiceOverAudioSource.spatialBlend = 0f;   // Force 2D sound for voice-overs.
        voiceOverAudioSource.loop = false;        // Voice-overs typically don't loop.
    }

    // --- Public API for Triggering Voice-Overs ---

    /// <summary>
    /// Plays a voice-over clip immediately. If another voice-over is playing,
    /// it may be interrupted based on the 'canBeInterrupted' and 'blocksOtherVoiceOvers' rules.
    /// </summary>
    /// <param name="clip">The VoiceOverClipData to play.</param>
    /// <param name="forcePlay">If true, ignores 'canBeInterrupted' and 'blocksOtherVoiceOvers' rules, forcing playback and interrupting current VO.</param>
    public void PlayVoiceOver(VoiceOverClipData clip, bool forcePlay = false)
    {
        if (clip == null || clip.audioClip == null)
        {
            LogWarning("Attempted to play a null or empty VoiceOverClipData. Aborting.");
            return;
        }

        // Check 'playOnlyOnce' rule.
        if (clip.playOnlyOnce && playedVoiceOverIDs.Contains(clip.voiceOverID))
        {
            Log("Voice-over '" + clip.name + "' (ID: " + clip.voiceOverID + ") has already been played and is set to 'playOnlyOnce'. Skipping.");
            return;
        }

        // If a voice-over is currently playing...
        if (IsPlaying())
        {
            // If not forced, check if current voice-over blocks incoming or if incoming can't interrupt.
            if (!forcePlay && currentVoiceOver != null && currentVoiceOver.blocksOtherVoiceOvers && !clip.canBeInterrupted)
            {
                Log("Voice-over '" + clip.name + "' cannot interrupt current voice-over '" + currentVoiceOver.name + "' because it blocks others and incoming is not interruptible. Enqueueing instead.");
                EnqueueVoiceOver(clip); // Fallback to enqueueing
                return;
            }

            // Stop the current voice-over if it can be interrupted or if forced.
            if (forcePlay || (currentVoiceOver != null && currentVoiceOver.canBeInterrupted))
            {
                StopCurrentVoiceOver();
                Log("Voice-over '" + currentVoiceOver.name + "' interrupted by '" + clip.name + "'.");
            }
            else
            {
                // If current VO cannot be interrupted and incoming cannot interrupt, enqueue it.
                Log("Voice-over '" + clip.name + "' cannot interrupt current voice-over '" + currentVoiceOver.name + "'. Enqueueing.");
                EnqueueVoiceOver(clip);
                return;
            }
        }

        // If playing immediately, clear any pending queued voice-overs to prioritize this one.
        voiceOverQueue.Clear();
        StartPlayRoutine(clip);
    }

    /// <summary>
    /// Adds a voice-over clip to the queue. It will play after any currently playing
    /// voice-over and all previously enqueued ones have finished.
    /// </summary>
    /// <param name="clip">The VoiceOverClipData to enqueue.</param>
    public void EnqueueVoiceOver(VoiceOverClipData clip)
    {
        if (clip == null || clip.audioClip == null)
        {
            LogWarning("Attempted to enqueue a null or empty VoiceOverClipData. Aborting.");
            return;
        }

        // Check 'playOnlyOnce' rule before enqueueing.
        if (clip.playOnlyOnce && playedVoiceOverIDs.Contains(clip.voiceOverID))
        {
            Log("Voice-over '" + clip.name + "' (ID: " + clip.voiceOverID + ") has already been played and is set to 'playOnlyOnce'. Skipping enqueue.");
            return;
        }

        voiceOverQueue.Enqueue(clip);
        Log("Voice-over '" + clip.name + "' enqueued. Current queue size: " + voiceOverQueue.Count);

        // If nothing is currently playing and no routine is active, start processing the queue.
        if (!IsPlaying() && playRoutine == null)
        {
            PlayNextQueuedVoiceOver();
        }
    }

    /// <summary>
    /// Stops the currently playing voice-over (if any) and clears the entire queue.
    /// </summary>
    public void StopAllVoiceOvers()
    {
        StopCurrentVoiceOver(); // Stop audio and coroutine.
        voiceOverQueue.Clear(); // Clear any pending items.
        currentVoiceOver = null;
        OnSubtitleHidden?.Invoke(); // Hide subtitles.
        Log("All voice-overs stopped and queue cleared.");
    }

    /// <summary>
    /// Checks if any voice-over is currently playing or is queued to play.
    /// </summary>
    public bool IsPlayingOrQueued()
    {
        return IsPlaying() || voiceOverQueue.Count > 0;
    }

    /// <summary>
    /// Checks if a voice-over is currently playing.
    /// </summary>
    public bool IsPlaying()
    {
        return voiceOverAudioSource != null && voiceOverAudioSource.isPlaying;
    }

    /// <summary>
    /// Resets the 'playOnlyOnce' status for all voice-overs.
    /// This means all voice-overs can be played again, regardless of previous plays in this session.
    /// </summary>
    public void ResetPlayedVoiceOvers()
    {
        playedVoiceOverIDs.Clear();
        Log("All 'playOnlyOnce' voice-over statuses have been reset.");
    }

    /// <summary>
    /// Returns the currently playing voice-over clip data.
    /// </summary>
    public VoiceOverClipData GetCurrentVoiceOver()
    {
        return currentVoiceOver;
    }

    // --- Private Playback Management ---

    /// <summary>
    /// Starts the coroutine responsible for playing a single voice-over clip.
    /// Handles stopping any previous routine to avoid conflicts.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    private void StartPlayRoutine(VoiceOverClipData clip)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }
        playRoutine = StartCoroutine(PlayVoiceOverRoutine(clip));
    }

    /// <summary>
    /// Immediately stops the current voice-over playback and its associated coroutine.
    /// </summary>
    private void StopCurrentVoiceOver()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        if (voiceOverAudioSource != null && voiceOverAudioSource.isPlaying)
        {
            voiceOverAudioSource.Stop();
        }
        currentVoiceOver = null;
        OnSubtitleHidden?.Invoke(); // Ensure subtitles are hidden immediately upon stopping.
    }

    /// <summary>
    /// Coroutine that manages the lifecycle of playing a single voice-over clip:
    /// delay, audio playback, subtitle display, and marking as played.
    /// </summary>
    /// <param name="clip">The VoiceOverClipData to play.</param>
    private IEnumerator PlayVoiceOverRoutine(VoiceOverClipData clip)
    {
        currentVoiceOver = clip;
        Log("Playing voice-over: '" + clip.name + "' (ID: " + clip.voiceOverID + ").");

        // Step 1: Apply optional delay before actual playback.
        if (clip.delayBeforePlay > 0)
        {
            yield return new WaitForSeconds(clip.delayBeforePlay);
        }

        // Step 2: Trigger 'started' event and show subtitles.
        OnVoiceOverStarted?.Invoke(clip);
        OnSubtitleUpdate?.Invoke(clip.characterName, clip.subtitleText);

        // Step 3: Play audio clip.
        if (voiceOverAudioSource == null)
        {
            LogWarning("AudioSource is null, cannot play voice-over: " + clip.name);
            yield break; // Exit routine if no AudioSource.
        }
        voiceOverAudioSource.clip = clip.audioClip;
        voiceOverAudioSource.volume = clip.volume;
        voiceOverAudioSource.Play();

        // Step 4: Wait for audio to finish.
        // Using audioClip.length is generally reliable, but can be slightly off for very short clips or specific codecs.
        // For robustness, one could poll voiceOverAudioSource.isPlaying in a loop.
        yield return new WaitForSeconds(clip.audioClip.length);

        // Step 5: Mark as played (if applicable) and trigger 'finished' event.
        if (clip.playOnlyOnce)
        {
            playedVoiceOverIDs.Add(clip.voiceOverID);
        }

        OnVoiceOverFinished?.Invoke(clip);
        OnSubtitleHidden?.Invoke(); // Hide subtitles when audio finishes.
        Log("Finished voice-over: '" + clip.name + "'.");

        // Step 6: Clear current voice-over and check for the next in queue.
        currentVoiceOver = null;
        playRoutine = null; // Mark routine as finished.
        PlayNextQueuedVoiceOver(); // Attempt to play the next queued item.
    }

    /// <summary>
    /// Attempts to dequeue and play the next voice-over clip from the queue.
    /// Only proceeds if nothing is currently playing and no playback routine is active.
    /// </summary>
    private void PlayNextQueuedVoiceOver()
    {
        if (voiceOverQueue.Count > 0 && !IsPlaying() && playRoutine == null)
        {
            VoiceOverClipData nextClip = voiceOverQueue.Dequeue();
            StartPlayRoutine(nextClip);
        }
    }

    // --- Debug Logging ---
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log("[VoiceOverManager] " + message, this);
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning("[VoiceOverManager] " + message, this);
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogError("[VoiceOverManager] " + message, this);
        }
    }
}
```

---

### 3. `VoiceOverTrigger.cs`

This component is attached to GameObjects in your scene to define trigger points for voice-overs. It supports various trigger types (e.g., player entering a zone, manual interaction) and allows you to specify a sequence of `VoiceOverClipData` assets to be played.

```csharp
using UnityEngine;
using System.Collections;
using System.Linq; // For .Any() method on string arrays

/// <summary>
/// A component placed in the scene to define areas or objects that will
/// trigger voice-overs when specific conditions are met (e.g., player enters a zone).
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures this GameObject has a Collider for trigger/collision detection.
public class VoiceOverTrigger : MonoBehaviour
{
    // --- Configuration ---
    [Header("Voice Over Clips")]
    [Tooltip("The VoiceOverClipData assets to play when this trigger is activated. " +
             "If multiple clips are provided, they will play in the order listed.")]
    [SerializeField] private VoiceOverClipData[] voiceOverClips;

    /// <summary>
    /// Defines how the voice-over clips associated with this trigger should be played.
    /// </summary>
    public enum PlaybackMode
    {
        PlayImmediately,        // Tries to play the first clip immediately (may interrupt current VO if rules allow).
                                // Subsequent clips in the array will be enqueued.
        EnqueueSequentially     // Adds all clips from the array to the VoiceOverManager's queue to play in order.
    }
    [Tooltip("How the voice-overs should be played: immediately or enqueued sequentially.")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.EnqueueSequentially;

    /// <summary>
    /// Defines the type of event that will activate this voice-over trigger.
    /// </summary>
    public enum TriggerType
    {
        OnTriggerEnter,     // Activates when a specified collider enters this trigger zone (requires Is Trigger=true).
        OnCollisionEnter,   // Activates when a specified collider physically collides with this object (requires Is Trigger=false).
        OnInteraction,      // Requires an external script to call TriggerVoiceOver() (e.g., player clicks an NPC).
        OnStart,            // Plays automatically when the scene starts and this object is active.
        Manual              // Can only be triggered by explicitly calling TriggerVoiceOver() from another script.
    }
    [Tooltip("The event that will activate this voice-over trigger.")]
    [SerializeField] private TriggerType triggerType = TriggerType.OnTriggerEnter;

    [Header("Trigger Conditions")]
    [Tooltip("If true, this trigger will only activate once per game session (or until 'ResetPlayedVoiceOvers' is called on the manager). " +
             "After activation, it will not trigger again.")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Specifies which GameObject tags can activate this trigger (e.g., 'Player', 'Companion'). " +
             "Leave empty to allow any tag (use with caution for OnTriggerEnter/OnCollisionEnter).")]
    [SerializeField] private string[] activatableTags = { "Player" }; // Default to "Player"

    [Tooltip("Delay in seconds after the trigger condition is met, before the voice-over sequence actually starts playing.")]
    [SerializeField] private float activationDelay = 0f;

    // --- Internal State ---
    private bool hasBeenTriggeredSession = false; // Tracks if this specific trigger has fired (for 'triggerOnce').
    private Collider triggerCollider;

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("VoiceOverTrigger requires a Collider component to function on GameObject: " + gameObject.name, this);
            enabled = false; // Disable script if no collider.
            return;
        }

        // Adjust collider settings based on trigger type for better user experience/validation.
        if (triggerType == TriggerType.OnTriggerEnter && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on '{gameObject.name}' is not set to 'Is Trigger' but VoiceOverTrigger is set to OnTriggerEnter. Setting 'Is Trigger' to true.", this);
            triggerCollider.isTrigger = true;
        }
        else if (triggerType == TriggerType.OnCollisionEnter && triggerCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on '{gameObject.name}' is set to 'Is Trigger' but VoiceOverTrigger is set to OnCollisionEnter. Setting 'Is Trigger' to false.", this);
            triggerCollider.isTrigger = false;
        }
    }

    private void Start()
    {
        if (triggerType == TriggerType.OnStart)
        {
            AttemptTriggerVoiceOver();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerType != TriggerType.OnTriggerEnter) return;

        // Check if the entering collider matches one of the activatable tags.
        if (activatableTags.Length > 0 && !activatableTags.Any(tag => other.CompareTag(tag)))
        {
            return; // Not an activatable tag.
        }

        AttemptTriggerVoiceOver();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (triggerType != TriggerType.OnCollisionEnter) return;

        // Check if the colliding object matches one of the activatable tags.
        if (activatableTags.Length > 0 && !activatableTags.Any(tag => collision.gameObject.CompareTag(tag)))
        {
            return; // Not an activatable tag.
        }

        AttemptTriggerVoiceOver();
    }

    /// <summary>
    /// Public method to manually trigger the voice-over (e.g., from an interaction script).
    /// This is used for TriggerType.OnInteraction and TriggerType.Manual.
    /// </summary>
    public void TriggerVoiceOver()
    {
        // Provide a warning if this is called when the trigger type isn't expecting manual activation.
        if (triggerType != TriggerType.Manual && triggerType != TriggerType.OnInteraction)
        {
            Debug.LogWarning($"Calling TriggerVoiceOver() on '{gameObject.name}' but its triggerType is {triggerType}. " +
                             $"Consider changing triggerType to 'Manual' or 'OnInteraction' for clarity.", this);
        }
        AttemptTriggerVoiceOver();
    }

    // --- Internal Triggering Logic ---
    /// <summary>
    /// Tries to activate the voice-over sequence, respecting 'triggerOnce' and manager availability.
    /// </summary>
    private void AttemptTriggerVoiceOver()
    {
        if (VoiceOverManager.Instance == null)
        {
            Debug.LogError("VoiceOverManager.Instance is null. Make sure a VoiceOverManager GameObject exists in your scene and is active.", this);
            return;
        }

        if (hasBeenTriggeredSession && triggerOnce)
        {
            // Debug.Log($"VoiceOverTrigger '{gameObject.name}' already triggered and set to trigger once. Skipping subsequent activations.");
            return; // Already triggered and set to trigger once.
        }

        // Apply an initial activation delay if specified.
        if (activationDelay > 0f)
        {
            StartCoroutine(ActivateWithDelay(activationDelay));
        }
        else
        {
            ActivateTriggerLogic();
        }

        if (triggerOnce)
        {
            hasBeenTriggeredSession = true;
            // Optionally disable the collider/script after one use to prevent further triggers.
            // triggerCollider.enabled = false;
            // enabled = false;
        }
    }

    /// <summary>
    /// Coroutine to handle the activation delay before playing voice-overs.
    /// </summary>
    private IEnumerator ActivateWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ActivateTriggerLogic();
    }

    /// <summary>
    /// The core logic for sending voice-overs to the VoiceOverManager based on playback mode.
    /// </summary>
    private void ActivateTriggerLogic()
    {
        if (voiceOverClips == null || voiceOverClips.Length == 0)
        {
            Debug.LogWarning($"VoiceOverTrigger on '{gameObject.name}' has no VoiceOverClipData assigned! Skipping activation.", this);
            return;
        }

        switch (playbackMode)
        {
            case PlaybackMode.PlayImmediately:
                // Play the first clip immediately. If there are more, enqueue them to play sequentially afterwards.
                VoiceOverManager.Instance.PlayVoiceOver(voiceOverClips[0]);
                for (int i = 1; i < voiceOverClips.Length; i++)
                {
                    VoiceOverManager.Instance.EnqueueVoiceOver(voiceOverClips[i]);
                }
                break;
            case PlaybackMode.EnqueueSequentially:
                // Enqueue all clips to play one after another.
                foreach (var clip in voiceOverClips)
                {
                    VoiceOverManager.Instance.EnqueueVoiceOver(clip);
                }
                break;
        }
    }

    // --- Editor Gizmos for Visualization ---
    /// <summary>
    /// Draws a visual representation of the trigger zone in the Unity editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Re-get collider if it's null (e.g., when scene is first loaded in editor).
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (triggerCollider != null)
        {
            // Apply the object's transform to the Gizmos for correct sizing/positioning.
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            // Choose color based on whether it's a trigger or a solid collider.
            if (triggerCollider.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f); // Green for triggers (semi-transparent)
            }
            else
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // Orange for colliders (semi-transparent)
            }

            // Draw different shapes based on the collider type.
            if (triggerCollider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0, 1, 0, 0.8f);
                Gizmos.DrawWireCube(box.center, box.size); // Wireframe outline
            }
            else if (triggerCollider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(0, 1, 0, 0.8f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius); // Wireframe outline
            }
            else if (triggerCollider is CapsuleCollider capsule)
            {
                // Drawing a wire capsule with Gizmos is a bit more involved,
                // so drawing a wire sphere at each end is a good approximation for visualization.
                // You could add custom editor code for a perfect capsule gizmo if needed.
                Gizmos.DrawWireSphere(capsule.center + (Vector3.up * (capsule.height / 2f - capsule.radius)), capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - (Vector3.up * (capsule.height / 2f - capsule.radius)), capsule.radius);
                // Draw a simple cube at the center to mark it
                Gizmos.color = new Color(0, 1, 0, 0.8f);
                Gizmos.DrawWireCube(capsule.center, Vector3.one * 0.1f);
            }
        }
    }
}
```

---

### 4. `SimpleSubtitleDisplay.cs`

This is an example script demonstrating how to subscribe to the `VoiceOverManager`'s events to display subtitles on a UI TextMeshPro element. This shows how to decouple the UI from the core voice-over system.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements. Make sure you've imported TMP Essentials!

/// <summary>
/// An example script to demonstrate how to listen to VoiceOverManager events
/// and display subtitles on UI TextMeshPro elements.
/// </summary>
public class SimpleSubtitleDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI element to display character names.")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Tooltip("The TextMeshProUGUI element to display subtitle text.")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("The parent UI Panel or GameObject that holds the subtitle elements. " +
             "Will be enabled when subtitles appear and disabled when they hide.")]
    [SerializeField] private GameObject subtitlePanel;

    private void OnEnable()
    {
        // Subscribe to events from the VoiceOverManager.
        // This ensures our UI updates when voice-overs change their state.
        VoiceOverManager.OnSubtitleUpdate += DisplaySubtitle;
        VoiceOverManager.OnSubtitleHidden += HideSubtitle;

        // Ensure subtitle panel is hidden when this script starts, in case no VO is playing.
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // IMPORTANT: Unsubscribe from events when this object is disabled or destroyed
        // to prevent potential memory leaks or errors if the manager tries to invoke events
        // on a null reference.
        VoiceOverManager.OnSubtitleUpdate -= DisplaySubtitle;
        VoiceOverManager.OnSubtitleHidden -= HideSubtitle;
    }

    /// <summary>
    /// Called by VoiceOverManager when new subtitle text should be displayed.
    /// </summary>
    /// <param name="character">The name of the character speaking.</param>
    /// <param name="text">The actual subtitle text.</param>
    private void DisplaySubtitle(string character, string text)
    {
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(true); // Show the subtitle panel.
        }

        if (characterNameText != null)
        {
            // Add a colon after the character name if it's not empty.
            characterNameText.text = string.IsNullOrEmpty(character) ? "" : character + ":";
        }

        if (subtitleText != null)
        {
            subtitleText.text = text; // Set the main subtitle text.
        }
    }

    /// <summary>
    /// Called by VoiceOverManager when subtitles should be hidden (e.g., voice-over finished or interrupted).
    /// </summary>
    private void HideSubtitle()
    {
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false); // Hide the entire subtitle panel.
        }
        // Clear text to ensure old subtitles don't flicker if panel is quickly re-enabled.
        if (characterNameText != null)
        {
            characterNameText.text = "";
        }
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }
    }
}
```

---

### Example Usage: Implementing the VoiceOverTriggerSystem

This section provides a step-by-step guide on how to set up and use the VoiceOverTriggerSystem in your Unity project.

#### 1. Initial Setup: Create the `VoiceOverManager`

1.  **Create Manager GameObject**: In your Unity scene, create an empty GameObject (e.g., named "VoiceOverSystem").
2.  **Attach `VoiceOverManager` Script**: Drag the `VoiceOverManager.cs` script onto this new GameObject in the Inspector.
3.  **Assign `AudioSource`**:
    *   The `VoiceOverManager` script will automatically add an `AudioSource` if one isn't present, but it's good practice to add one manually and configure its settings (e.g., assign an `Audio Mixer Group` for voice-overs, adjust default volume).
    *   Drag this `AudioSource` component into the `Voice Over Audio Source` slot on the `VoiceOverManager` component in the Inspector.
    *   Ensure its `Spatial Blend` is set to 2D (0). The `VoiceOverManager` script enforces this at runtime, but setting it in the editor is clearer.
4.  **Debug Logs**: Optionally, check `Enable Debug Logs` on the `VoiceOverManager` in the Inspector to see playback messages in the console.

#### 2. Create `VoiceOverClipData` Assets

These are your actual voice lines.

1.  **Create Asset**: In your Project window, right-click -> Create -> VoiceOver System -> VoiceOver Clip Data.
2.  **Name It**: Name it something descriptive (e.g., "Narrator_Intro_Part1", "NPC_QuestStart").
3.  **Configure in Inspector**:
    *   **Audio Clip**: Drag an `AudioClip` (e.g., an `.mp3` or `.wav` file) into this slot.
    *   **Volume**: Adjust the playback volume for this specific clip (0 to 1).
    *   **Delay Before Play**: If you want a short pause *after* the manager activates this clip but *before* its audio starts, set this (e.g., for dramatic effect).
    *   **Subtitle Text**: Type the text to be displayed as a subtitle while this voice-over plays.
    *   **Character Name**: Enter the name of the character speaking (e.g., "Narrator", "Guard Captain").
    *   **Voice Over ID**: Assign a **unique string** for this clip (e.g., "Intro_VO_001"). This is critical for the `Play Only Once` feature. The `OnValidate` method will suggest the asset name if you leave it empty.
    *   **Play Only Once**: If true, this specific clip will only play once per game session.
    *   **Can Be Interrupted**: If true, an incoming `PlayVoiceOver` call can stop this clip early.
    *   **Blocks Other VoiceOvers**: If true, this clip will prevent other `PlayVoiceOver` calls from starting (they will be enqueued instead), unless the incoming call `forcePlay`s.
4.  **Repeat**: Create several `VoiceOverClipData` assets for different voice lines you'll use.

#### 3. Create `VoiceOverTrigger` GameObjects

These are the "sensors" or "interaction points" in your scene that will initiate voice-overs.

##### a. For an Area Trigger (e.g., Player enters a zone)

1.  **Create GameObject**: Create an empty GameObject (e.g., "IntroZoneTrigger").
2.  **Add Collider**: Add a `BoxCollider` (or `SphereCollider`, `CapsuleCollider`) component to it.
3.  **Set `Is Trigger`**: **Crucially, enable `Is Trigger` on the Collider component in the Inspector.**
4.  **Attach `VoiceOverTrigger` Script**: Drag the `VoiceOverTrigger.cs` script onto this GameObject.
5.  **Configure in Inspector**:
    *   **Voice Over Clips**: Drag and drop the `VoiceOverClipData` assets you created earlier into this array. If you add multiple, they will play in the order listed.
    *   **Playback Mode**: Choose `Enqueue Sequentially` if you have multiple clips for this trigger and want them to play one after another. Choose `Play Immediately` if the first clip should try to play instantly (and subsequent ones will enqueue).
    *   **Trigger Type**: Set to `On Trigger Enter`.
    *   **Trigger Once**: Set to true if you only want the VO to play the first time the player enters this zone.
    *   **Activatable Tags**: Ensure "Player" is in this list (or whatever tag your player character has). This makes sure only the player (or specific objects) can activate it.
    *   **Activation Delay**: Optionally, add a delay *after* the trigger condition is met but *before* the first voice-over starts its playback.

##### b. For an Interaction Trigger (e.g., Player clicks an NPC, opens a door)

1.  **Create GameObject**: Create an empty GameObject (e.g., "NPC_DialogueTrigger") or attach directly to your interactable NPC/object.
2.  **Attach `VoiceOverTrigger` Script**: Drag the `VoiceOverTrigger.cs` script onto it.
3.  **Configure in Inspector**:
    *   **Voice Over Clips**: Assign your `VoiceOverClipData` assets.
    *   **Playback Mode**: Choose as desired.
    *   **Trigger Type**: Set to `On Interaction`.
    *   **Trigger Once**: Configure as needed.
    *   `Activatable Tags` and `Activation Delay` can be left default as the `TriggerVoiceOver()` method will be called manually.
4.  **Call from your Interaction Script**: In your NPC's or interactable object's script (e.g., `NPCInteraction.cs`), when the player interacts, call the `TriggerVoiceOver()` method:

    ```csharp
    // Inside your NPCInteraction.cs (example)
    using UnityEngine;

    public class NPCInteraction : MonoBehaviour
    {
        public VoiceOverTrigger dialogueTrigger; // Assign this in the Inspector

        void Start()
        {
            // Optional: Ensure the VoiceOverTrigger is configured correctly for interaction.
            if (dialogueTrigger != null && dialogueTrigger.GetComponent<VoiceOverTrigger>().triggerType != VoiceOverTrigger.TriggerType.OnInteraction)
            {
                Debug.LogWarning($"NPCInteraction references a VoiceOverTrigger '{dialogueTrigger.name}' not set to 'On Interaction'. Ensure correct TriggerType.", this);
            }
        }

        public void OnPlayerInteract() // Call this method when player interacts
        {
            Debug.Log($"Player interacted with {gameObject.name}.");
            if (dialogueTrigger != null)
            {
                dialogueTrigger.TriggerVoiceOver(); // This starts the voice-over sequence
            }
            else
            {
                Debug.LogWarning("NPCInteraction: dialogueTrigger is not assigned!", this);
            }
        }
    }
    ```

##### c. For a Scene Start Trigger

1.  **Create GameObject**: Create an empty GameObject (e.g., "SceneStartVO").
2.  **Attach `VoiceOverTrigger` Script**: Drag the `VoiceOverTrigger.cs` script onto it.
3.  **Configure in Inspector**:
    *   **Voice Over Clips**: Assign your `VoiceOverClipData` assets.
    *   **Playback Mode**: Choose as desired.
    *   **Trigger Type**: Set to `On Start`.
    *   **Trigger Once**: Usually true for intro voice-overs.
    *   `Activatable Tags` are ignored for `OnStart`.
    *   **Activation Delay**: Useful if you want a short fade-in or camera animation before the VO begins.
4.  The voice-over will play automatically when the scene loads and this GameObject is active.

#### 4. Implement Subtitle Display (Optional but Recommended)

1.  **Create UI Canvas**: In Unity, go to GameObject -> UI -> Canvas.
2.  **Create Subtitle Panel**: Inside the Canvas, create a UI Panel (GameObject -> UI -> Panel). Rename it to "SubtitlePanel". This will be the parent of your subtitle text elements.
3.  **Add Text Elements**: Inside the "SubtitlePanel", create two `TextMeshPro - Text (UI)` elements (GameObject -> UI -> Text - TextMeshPro).
    *   Name one "CharacterNameText" and the other "SubtitleText".
    *   Position and style them within the "SubtitlePanel" as desired (e.g., character name above subtitle text, centered, appropriate font size).
    *   *(If you don't have TextMeshPro, Unity will prompt you to import its Essentials. If you prefer `UnityEngine.UI.Text`, modify `SimpleSubtitleDisplay.cs` accordingly).*
4.  **Create Subtitle Display GameObject**: Create an empty GameObject in your scene (e.g., "SubtitleDisplay").
5.  **Attach `SimpleSubtitleDisplay` Script**: Drag the `SimpleSubtitleDisplay.cs` script onto it.
6.  **Assign UI References**: In the Inspector for `SimpleSubtitleDisplay`:
    *   Drag your "CharacterNameText" TMPro object into the `Character Name Text` slot.
    *   Drag your "SubtitleText" TMPro object into the `Subtitle Text` slot.
    *   Drag your "SubtitlePanel" GameObject into the `Subtitle Panel` slot.
7.  Now, whenever a voice-over plays, its character name and subtitle text will appear on the UI, and the panel will hide when the voice-over finishes!

#### 5. Run Your Scene!

*   Make sure your Player GameObject has the tag "Player" (or whatever tag you specified in `VoiceOverTrigger`'s `Activatable Tags`).
*   Walk into your trigger zone, interact with your NPC, or start the scene (for `OnStart` triggers), and observe the voice-overs playing and subtitles appearing.

This completes a basic yet comprehensive setup of the VoiceOverTriggerSystem. You can now easily integrate voice-overs into your Unity game using this flexible pattern.