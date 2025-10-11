// Unity Design Pattern Example: NarrationSystem
// This script demonstrates the NarrationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The NarrationSystem design pattern in Unity provides a centralized and flexible way to manage and play various forms of narrative content, such as voice-overs, subtitles, and triggered events. It decouples the act of *triggering* narration from the *delivery* and *display* of that narration. This makes your game's narrative elements easier to manage, modify, and scale.

**Key Components of this NarrationSystem Implementation:**

1.  **`NarrationClip.cs` (ScriptableObject):**
    *   **Purpose:** Defines a single atomic piece of narrative content.
    *   **Contents:** An `AudioClip` (for voice-over), `string` (for subtitle text), a `customDuration` (for text-only narration), and an optional `UnityEvent` that fires when this specific clip finishes.
    *   **Benefit:** Allows narrative content to be created and managed as re-usable assets in the Unity Editor, separate from game logic.

2.  **`NarrationSystem.cs` (Singleton MonoBehaviour):**
    *   **Purpose:** The central manager responsible for orchestrating narration playback.
    *   **Features:**
        *   **Singleton:** Ensures only one instance exists in the game, providing a global access point.
        *   **Queue Management:** Handles a `Queue<NarrationClip>`, playing clips sequentially.
        *   **Audio Playback:** Manages an `AudioSource` for voice-overs.
        *   **Event Broadcasting:** Uses C# events (`OnSubtitleRequested`, `OnNarrationClipFinished`, `OnNarrationQueueEmpty`) to notify other systems (like UI) when updates are needed.
        *   **Control Methods:** `PlayNarration` (single or sequence), `StopNarration`, `SkipCurrentNarration`.
    *   **Benefit:** Centralizes all narration logic, making it easy to control and ensuring consistent behavior.

3.  **`NarrationUI.cs` (MonoBehaviour):**
    *   **Purpose:** Listens to the `NarrationSystem`'s subtitle events and displays them on the UI.
    *   **Features:**
        *   Subscribes to `NarrationSystem.OnSubtitleRequested`.
        *   Updates a `TextMeshProUGUI` component with the subtitle text.
        *   Includes a fade-in/fade-out effect using a `CanvasGroup` for a smoother user experience.
    *   **Benefit:** Decouples the UI display from the narration logic, allowing designers to change the subtitle appearance without touching the core system.

4.  **`NarrationTrigger.cs` (MonoBehaviour):**
    *   **Purpose:** An example component that triggers narration based on game events.
    *   **Features:**
        *   Can trigger a single `NarrationClip` or a `List` of clips.
        *   Configurable to play `OnStart`, `OnTriggerEnter` (with a specific tag), and optionally `_playOnce`.
        *   Includes a `[ContextMenu]` option for easy testing in the editor.
    *   **Benefit:** Provides a concrete example of how game objects can initiate narration without directly managing the complex playback state.

---

## 1. NarrationClip.cs

This ScriptableObject defines the data structure for a single piece of narration.

```csharp
// NarrationClip.cs
using UnityEngine;
using UnityEngine.Events; // For custom events at the end of narration

/// <summary>
/// ScriptableObject representing a single piece of narration content.
/// This decouples the narration data from the system that plays it,
/// allowing for easy creation and management of narrative clips as assets.
/// </summary>
[CreateAssetMenu(fileName = "NewNarrationClip", menuName = "Narration System/Narration Clip")]
public class NarrationClip : ScriptableObject
{
    [Tooltip("The audio clip for voice-over narration. Optional.")]
    public AudioClip voiceOver;

    [Tooltip("The text to display as subtitles. Can be left empty if only audio is desired.")]
    [TextArea(3, 6)]
    public string subtitleText;

    [Tooltip("Custom duration for the narration if there's no audio clip (e.g., for text-only narration).")]
    public float customDuration = 3.0f;

    [Tooltip("Event to be invoked when this specific narration clip finishes playing.")]
    public UnityEvent onNarrationEnd;

    /// <summary>
    /// Calculates and returns the duration of this narration clip.
    /// If an audio clip is provided, its length is used. Otherwise, the customDuration is used.
    /// </summary>
    public float GetDuration()
    {
        if (voiceOver != null)
        {
            return voiceOver.length;
        }
        return customDuration;
    }
}
```

## 2. NarrationSystem.cs

This is the core manager, implemented as a singleton.

```csharp
// NarrationSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For Queue
using TMPro; // Although NarrationUI directly uses it, the system might need it for direct subtitle control or logging.
using UnityEngine.Events; // For UnityEvent in NarrationClip

/// <summary>
/// The core NarrationSystem design pattern implementation.
/// This is a centralized, persistent manager (Singleton) responsible for:
/// 1. Queuing and playing NarrationClips (audio, text, events).
/// 2. Managing an AudioSource for voiceovers.
/// 3. Broadcasting events for subtitle display.
/// 4. Providing methods to control narration (play, stop, skip).
/// </summary>
public class NarrationSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Ensures there's only one instance of the NarrationSystem throughout the game.
    public static NarrationSystem Instance { get; private set; }

    // --- Editor-Configurable References ---
    [Header("Core Components")]
    [Tooltip("The AudioSource component used for playing voice-over narration.")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Narration Control")]
    [Tooltip("Allow narration clips to be skipped by calling SkipCurrentNarration().")]
    [SerializeField] private bool _allowSkipping = true;

    // --- Internal State Variables ---
    private Queue<NarrationClip> _narrationQueue = new Queue<NarrationClip>();
    private NarrationClip _currentNarration;
    private Coroutine _narrationRoutine;
    private bool _isPlayingCurrentClip; // Tracks if the current clip is actively playing (e.g., audio is playing or waiting for duration)

    // --- Events for UI and Game Logic ---
    // These events allow other components (like a UI manager or game logic) to react to narration changes.
    // Example: NarrationUI subscribes to OnSubtitleRequested to update text.
    public delegate void SubtitleUpdateHandler(string text);
    public static event SubtitleUpdateHandler OnSubtitleRequested; // Fired when subtitles should be shown/hidden

    public delegate void NarrationClipFinishedHandler(NarrationClip finishedClip);
    public static event NarrationClipFinishedHandler OnNarrationClipFinished; // Fired when an individual clip finishes

    public delegate void NarrationSystemFinishedHandler();
    public static event NarrationSystemFinishedHandler OnNarrationQueueEmpty; // Fired when the entire queue is empty

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            // Optionally make it persistent across scene loads
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Debug.LogWarning("Multiple NarrationSystem instances found! Destroying duplicate.", gameObject);
            Destroy(gameObject);
            return; // Exit to prevent further execution for the duplicate
        }

        // Ensure an AudioSource is present
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                Debug.LogWarning("NarrationSystem: No AudioSource assigned, added one automatically.", this);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // Ensure any running coroutine is stopped if the GameObject is destroyed
        if (_narrationRoutine != null)
        {
            StopCoroutine(_narrationRoutine);
            _narrationRoutine = null;
        }
    }

    // --- Public API for Triggering Narration ---

    /// <summary>
    /// Adds a single NarrationClip to the queue for playback.
    /// If clearQueue is true, any existing clips in the queue will be discarded.
    /// </summary>
    /// <param name="clip">The NarrationClip to play.</param>
    /// <param name="clearQueue">If true, clears the current queue before adding this clip.</param>
    public void PlayNarration(NarrationClip clip, bool clearQueue = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to play a null NarrationClip.");
            return;
        }

        if (clearQueue)
        {
            StopNarration(true); // Stop current and clear queue
        }
        
        _narrationQueue.Enqueue(clip);
        Debug.Log($"NarrationSystem: Enqueued clip '{clip.name}'. Queue size: {_narrationQueue.Count}");

        // If not already playing, start the narration routine.
        if (_narrationRoutine == null)
        {
            _narrationRoutine = StartCoroutine(NarrationSequenceCoroutine());
        }
    }

    /// <summary>
    /// Adds a sequence of NarrationClips to the queue for playback.
    /// If clearQueue is true, any existing clips in the queue will be discarded.
    /// </summary>
    /// <param name="clips">The collection of NarrationClips to play in order.</param>
    /// <param name="clearQueue">If true, clears the current queue before adding these clips.</param>
    public void PlayNarration(IEnumerable<NarrationClip> clips, bool clearQueue = false)
    {
        if (clips == null)
        {
            Debug.LogWarning("Attempted to play a null collection of NarrationClips.");
            return;
        }

        if (clearQueue)
        {
            StopNarration(true); // Stop current and clear queue
        }

        int addedCount = 0;
        foreach (NarrationClip clip in clips)
        {
            if (clip != null)
            {
                _narrationQueue.Enqueue(clip);
                addedCount++;
            }
            else
            {
                Debug.LogWarning("A null NarrationClip was found in the provided sequence. Skipping it.");
            }
        }
        Debug.Log($"NarrationSystem: Enqueued {addedCount} clips from sequence. Queue size: {_narrationQueue.Count}");

        // If not already playing, start the narration routine.
        if (_narrationRoutine == null)
        {
            _narrationRoutine = StartCoroutine(NarrationSequenceCoroutine());
        }
    }

    /// <summary>
    /// Immediately stops the current narration clip and optionally clears the entire queue.
    /// This will also stop any audio playback and hide subtitles.
    /// </summary>
    /// <param name="clearQueue">If true, all pending clips in the queue will be removed.</param>
    public void StopNarration(bool clearQueue = false)
    {
        if (_narrationRoutine != null)
        {
            StopCoroutine(_narrationRoutine);
            _narrationRoutine = null;
        }

        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        // Hide any currently displayed subtitles.
        OnSubtitleRequested?.Invoke(string.Empty);

        _currentNarration = null;
        _isPlayingCurrentClip = false;

        if (clearQueue)
        {
            _narrationQueue.Clear();
            Debug.Log("NarrationSystem: Stopped current narration and cleared queue.");
        }
        else
        {
            Debug.Log("NarrationSystem: Stopped current narration.");
        }
    }

    /// <summary>
    /// Skips the currently playing narration clip and immediately proceeds to the next one in the queue.
    /// If no more clips, narration will stop.
    /// Only works if skipping is allowed via `_allowSkipping`.
    /// </summary>
    public void SkipCurrentNarration()
    {
        if (!_allowSkipping)
        {
            Debug.Log("NarrationSystem: Skipping is currently disabled.");
            return;
        }

        if (_isPlayingCurrentClip && _narrationRoutine != null)
        {
            // By stopping and restarting the coroutine, we force it to immediately
            // check the queue for the next item. This effectively 'skips' the current WaitForSeconds.
            Debug.Log($"NarrationSystem: Skipping current narration clip '{_currentNarration?.name}'.");
            StopCoroutine(_narrationRoutine);
            _narrationRoutine = StartCoroutine(NarrationSequenceCoroutine());
        }
        else if (_narrationQueue.Count > 0)
        {
            // If nothing is playing but there are items in the queue, start playing the next.
            Debug.Log("NarrationSystem: No narration actively playing, but queue has items. Starting next due to skip request.");
            if (_narrationRoutine == null)
            {
                _narrationRoutine = StartCoroutine(NarrationSequenceCoroutine());
            }
        }
        else
        {
            Debug.Log("NarrationSystem: No narration currently playing or in queue to skip.");
        }
    }

    /// <summary>
    /// Coroutine that manages the sequential playback of narration clips from the queue.
    /// This is the heart of the NarrationSystem's playback logic.
    /// </summary>
    private IEnumerator NarrationSequenceCoroutine()
    {
        while (_narrationQueue.Count > 0)
        {
            _currentNarration = _narrationQueue.Dequeue();
            _isPlayingCurrentClip = true;

            Debug.Log($"NarrationSystem: Playing clip '{_currentNarration.name}' (Audio: {_currentNarration.voiceOver?.name ?? "None"}, Text: '{_currentNarration.subtitleText}')");

            // --- Play Audio (if available) ---
            if (_currentNarration.voiceOver != null)
            {
                _audioSource.clip = _currentNarration.voiceOver;
                _audioSource.Play();
            }

            // --- Display Subtitles (if available) ---
            if (!string.IsNullOrEmpty(_currentNarration.subtitleText))
            {
                OnSubtitleRequested?.Invoke(_currentNarration.subtitleText);
            }
            else
            {
                OnSubtitleRequested?.Invoke(string.Empty); // Clear subtitles if this clip has none
            }

            // --- Wait for Narration Duration ---
            // If audio is playing, wait for its length. Otherwise, use customDuration.
            float duration = _currentNarration.GetDuration();
            yield return new WaitForSeconds(duration);

            // --- Narration Clip Finished Actions ---
            // Ensure audio is stopped
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            // Hide subtitles
            OnSubtitleRequested?.Invoke(string.Empty);

            // Invoke clip-specific end event (e.g., trigger an animation or enable player input)
            _currentNarration.onNarrationEnd?.Invoke();

            // Invoke general event that a clip has finished
            OnNarrationClipFinished?.Invoke(_currentNarration);

            _currentNarration = null;
            _isPlayingCurrentClip = false;

            // Small optional delay before processing the next clip, for pacing.
            // yield return new WaitForSeconds(0.1f); 
        }

        // --- All Narration Finished ---
        Debug.Log("NarrationSystem: All queued narrations finished.");
        _narrationRoutine = null; // Reset the coroutine reference
        OnNarrationQueueEmpty?.Invoke(); // Notify listeners that the entire queue is empty
    }

    /// <summary>
    /// Check if the narration system is currently playing any clip or has clips in the queue.
    /// </summary>
    public bool IsPlaying()
    {
        return _isPlayingCurrentClip || _narrationQueue.Count > 0;
    }

    /// <summary>
    /// Get the currently playing narration clip. Returns null if nothing is playing.
    /// </summary>
    public NarrationClip GetCurrentNarrationClip()
    {
        return _currentNarration;
    }

    /// <summary>
    /// Gets the number of narration clips currently in the queue (excluding the one playing).
    /// </summary>
    public int GetQueueCount()
    {
        return _narrationQueue.Count;
    }
}
```

## 3. NarrationUI.cs

This script handles the visual display of subtitles.

```csharp
// NarrationUI.cs
using UnityEngine;
using TMPro; // For TextMeshProUGUI
using System.Collections; // For Coroutines
using UnityEngine.UI; // For CanvasGroup

/// <summary>
/// This component listens to the NarrationSystem's subtitle events
/// and displays the text using a TextMeshProUGUI component,
/// with an optional fade-in/fade-out effect.
/// </summary>
[RequireComponent(typeof(CanvasGroup))] // Ensures a CanvasGroup is present for fading
public class NarrationUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI component used to display subtitles.")]
    [SerializeField] private TextMeshProUGUI _subtitleText;

    [Header("Display Settings")]
    [Tooltip("How long it takes for subtitles to fade in/out.")]
    [SerializeField] private float _fadeDuration = 0.5f;
    [Tooltip("If true, subtitles will appear immediately without fading.")]
    [SerializeField] private bool _instantDisplay = false;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogError("NarrationUI requires a CanvasGroup component on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (_subtitleText == null)
        {
            Debug.LogError("NarrationUI: Subtitle Text (TextMeshProUGUI) is not assigned. Please assign it in the inspector.", this);
            enabled = false;
            return;
        }

        // Initialize with subtitles hidden
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _subtitleText.text = string.Empty;
    }

    private void OnEnable()
    {
        // Subscribe to the NarrationSystem's event for subtitle requests.
        NarrationSystem.OnSubtitleRequested += HandleSubtitleRequest;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and ensure event handlers are cleaned up.
        NarrationSystem.OnSubtitleRequested -= HandleSubtitleRequest;
        
        // Ensure any active fade coroutine is stopped and subtitles are hidden on disable
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _subtitleText.text = string.Empty;
    }

    /// <summary>
    /// Event handler for when the NarrationSystem requests a subtitle update.
    /// </summary>
    /// <param name="text">The subtitle text to display. An empty string means hide subtitles.</param>
    private void HandleSubtitleRequest(string text)
    {
        // Optimization: If the same text is already displayed and visible, do nothing.
        // Or if trying to hide when already hidden, do nothing.
        if ((text == _subtitleText.text && _canvasGroup.alpha > 0 && !string.IsNullOrEmpty(text)) ||
            (string.IsNullOrEmpty(text) && _canvasGroup.alpha == 0))
        {
            return;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        if (!string.IsNullOrEmpty(text))
        {
            // Display new text and fade in
            _subtitleText.text = text;
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup, 1f, _fadeDuration, true));
        }
        else
        {
            // Hide text and fade out
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup, 0f, _fadeDuration, false));
        }
    }

    /// <summary>
    /// Coroutine to fade a CanvasGroup's alpha.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration, bool enableInteractions)
    {
        // Instant display option
        if (_instantDisplay)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = enableInteractions;
            canvasGroup.interactable = enableInteractions;
            if (targetAlpha == 0f) _subtitleText.text = string.Empty; // Clear text instantly when hidden
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha; // Ensure final alpha is exact

        // Manage interaction and raycast blocking after fade
        canvasGroup.blocksRaycasts = enableInteractions;
        canvasGroup.interactable = enableInteractions;

        // If fading out completely, clear the text after it's invisible
        if (targetAlpha == 0f)
        {
            _subtitleText.text = string.Empty;
        }
    }
}
```

## 4. NarrationTrigger.cs

An example script to initiate narration.

```csharp
// NarrationTrigger.cs
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// A component that acts as a trigger for the NarrationSystem.
/// It can initiate single narration clips or a sequence of clips
/// based on various events like game start, trigger collision, or a manual call.
/// </summary>
public class NarrationTrigger : MonoBehaviour
{
    [Header("Narration Content")]
    [Tooltip("Assign a single NarrationClip to play. This takes precedence over Narration Sequence if both are assigned.")]
    [SerializeField] private NarrationClip _singleNarration;

    [Tooltip("Assign a list of NarrationClips to play in order. Used if _singleNarration is null.")]
    [SerializeField] private List<NarrationClip> _narrationSequence;

    [Header("Trigger Conditions")]
    [Tooltip("If true, the narration will play when this GameObject starts.")]
    [SerializeField] private bool _playOnStart = false;

    [Tooltip("If true, the narration will play when another collider with the specified tag enters this trigger.")]
    [SerializeField] private bool _playOnTriggerEnter = false;
    [Tooltip("The tag of the GameObject that can trigger this narration (e.g., 'Player').")]
    [SerializeField] private string _triggerTag = "Player";

    [Tooltip("If true, this trigger can only be activated once.")]
    [SerializeField] private bool _playOnce = true;
    private bool _hasPlayed = false;

    private void Start()
    {
        if (_playOnStart && !_hasPlayed)
        {
            TriggerNarration();
            if (_playOnce) _hasPlayed = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playOnTriggerEnter && !_hasPlayed && other.CompareTag(_triggerTag))
        {
            TriggerNarration();
            if (_playOnce) _hasPlayed = true;
        }
    }

    /// <summary>
    /// Public method to manually trigger the narration from other scripts or UI buttons.
    /// </summary>
    public void TriggerNarration()
    {
        if (NarrationSystem.Instance == null)
        {
            Debug.LogError("NarrationSystem not found in the scene. Please add a NarrationSystem GameObject.", this);
            return;
        }

        // Only block subsequent plays if _playOnce is true AND we've already played.
        if (_playOnce && _hasPlayed) 
        {
            Debug.Log($"NarrationTrigger on '{gameObject.name}' has already played once and is set to play once. Ignoring.", this);
            return;
        }
        
        // Prioritize single narration over a sequence
        if (_singleNarration != null)
        {
            Debug.Log($"Triggering single narration: '{_singleNarration.name}'", this);
            NarrationSystem.Instance.PlayNarration(_singleNarration);
        }
        else if (_narrationSequence != null && _narrationSequence.Count > 0)
        {
            Debug.Log($"Triggering narration sequence with {_narrationSequence.Count} clips.", this);
            NarrationSystem.Instance.PlayNarration(_narrationSequence);
        }
        else
        {
            Debug.LogWarning("NarrationTrigger has no NarrationClip or Narration Sequence assigned. Nothing to play.", this);
        }

        // Mark as played after attempting to trigger narration
        if (_playOnce) _hasPlayed = true;
    }

    /// <summary>
    /// Editor-only method to test playing narration directly from the Inspector's context menu.
    /// </summary>
    [ContextMenu("Test Play Narration")]
    private void TestPlayNarration()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Testing Narration Playback...", this);
            TriggerNarration();
        }
        else
        {
            Debug.LogWarning("Can only test narration playback in Play Mode.", this);
        }
    }

    /// <summary>
    /// Resets the 'hasPlayed' state, allowing the trigger to activate again if _playOnce is true.
    /// </summary>
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        _hasPlayed = false;
        Debug.Log($"NarrationTrigger on '{gameObject.name}' has been reset.", this);
    }
}
```

---

## How to Set Up in Unity

Follow these steps to integrate the NarrationSystem into your Unity project:

1.  **Create Scripts:**
    *   Create four new C# scripts in your Unity Project window (e.g., in a "Scripts/NarrationSystem" folder): `NarrationClip.cs`, `NarrationSystem.cs`, `NarrationUI.cs`, and `NarrationTrigger.cs`.
    *   Copy and paste the respective code into each script file.

2.  **Set up the Narration System Manager:**
    *   In your scene, create an empty GameObject (e.g., name it `_NarrationManager`).
    *   Attach the `NarrationSystem.cs` script to this GameObject.
    *   An `AudioSource` component will be automatically added to this GameObject if one isn't already present. This `AudioSource` will be used for playing voice-overs.
    *   *(Optional but recommended)* If you want the NarrationSystem to persist across scene changes, you can add `DontDestroyOnLoad(gameObject);` in `NarrationSystem.cs`'s `Awake()` method (currently commented out).

3.  **Set up the Subtitle UI:**
    *   Create a UI Canvas (Right-click in Hierarchy -> UI -> Canvas).
    *   Inside the Canvas, create an empty GameObject (e.g., name it `SubtitleDisplay`).
    *   Attach the `NarrationUI.cs` script to `SubtitleDisplay`.
    *   A `CanvasGroup` component will be automatically added to `SubtitleDisplay` (due to `[RequireComponent(typeof(CanvasGroup))]`).
    *   Inside `SubtitleDisplay`, create a `TextMeshPro - Text (UI)` GameObject (Right-click `SubtitleDisplay` -> UI -> Text - TextMeshPro). Make sure you've imported TMP Essentials (Window -> TextMeshPro -> Import TMP Essentials).
    *   Adjust the `RectTransform` of this `TextMeshProUGUI` component to fit your desired subtitle display area (e.g., stretch horizontally at the bottom of the screen). Customize its font, size, color, alignment, etc.
    *   Drag this `TextMeshProUGUI` component into the `_subtitleText` field of the `NarrationUI` script in the Inspector.

4.  **Create NarrationClip Assets:**
    *   In your Project window, right-click -> Create -> Narration System -> Narration Clip.
    *   Name the new asset descriptively (e.g., "Intro_Part1", "GameStart_Greeting").
    *   In the Inspector for your `NarrationClip` asset:
        *   **Voice Over:** Assign an `AudioClip` (e.g., an `.wav` or `.mp3` file) if you have voice narration.
        *   **Subtitle Text:** Type the corresponding subtitle text.
        *   **Custom Duration:** If there's no `AudioClip`, set a `customDuration` (e.g., 5 seconds) for how long the text should be displayed.
        *   **On Narration End (Optional):** You can use this `UnityEvent` to trigger specific actions when this clip finishes (e.g., enable a button, play an animation, move to next quest step).

5.  **Implement NarrationTriggers:**
    *   For any in-game object or event that should initiate narration:
    *   Create an empty GameObject (e.g., `StartGameTrigger`) or select an existing one (e.g., an NPC character).
    *   Attach the `NarrationTrigger.cs` script to it.
    *   In the Inspector for `NarrationTrigger`:
        *   **Narration Content:** Drag one of your `NarrationClip` assets into the `_singleNarration` slot, OR populate the `_narrationSequence` list with multiple clips to play them in order.
        *   **Trigger Conditions:**
            *   Check `_playOnStart` if you want it to play when the GameObject loads.
            *   Check `_playOnTriggerEnter` if a player (or other tagged object) entering a collider should trigger it. If so, ensure your GameObject has a `Collider` component (set to `Is Trigger`) and a `Rigidbody` (even if kinematic). Set the `_triggerTag` (e.g., "Player").
            *   Check `_playOnce` if the narration should only play a single time from this trigger.
    *   You can also call `TriggerNarration()` from other scripts or UI buttons.

---

## Example Usage in Other Scripts

Here's how other scripts in your game can interact with the `NarrationSystem` directly:

```csharp
// ExampleUsage_GameStateManager.cs
using UnityEngine;
using System.Collections.Generic;

public class ExampleUsage_GameStateManager : MonoBehaviour
{
    [Header("Narration Clips for Game State Management")]
    public NarrationClip gameStartIntroClip;
    public List<NarrationClip> tutorialSequenceClips;
    public NarrationClip missionCompleteClip;

    void Start()
    {
        // Example 1: Play a single narration clip immediately at the start of the game
        if (NarrationSystem.Instance != null && gameStartIntroClip != null)
        {
            Debug.Log("GameStateManager: Playing game start intro narration.");
            NarrationSystem.Instance.PlayNarration(gameStartIntroClip);
        }
        
        // Example 2: Subscribe to NarrationSystem events to react to narration progress
        NarrationSystem.OnNarrationClipFinished += HandleSpecificNarrationClipFinished;
        NarrationSystem.OnNarrationQueueEmpty += HandleAllNarrationFinished;
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks or errors
        NarrationSystem.OnNarrationClipFinished -= HandleSpecificNarrationClipFinished;
        NarrationSystem.OnNarrationQueueEmpty -= HandleAllNarrationFinished;
    }

    /// <summary>
    /// Event handler for when any single narration clip finishes playing.
    /// </summary>
    /// <param name="finishedClip">The NarrationClip that just finished.</param>
    void HandleSpecificNarrationClipFinished(NarrationClip finishedClip)
    {
        Debug.Log($"GameStateManager: Narration clip '{finishedClip.name}' finished. Time to do something!");
        
        // Example: Perform specific game logic based on the finished clip
        if (finishedClip == gameStartIntroClip)
        {
            Debug.Log("Intro narration is complete. Enabling player movement and tutorial.");
            // PlayerController.Instance.EnableMovement();
            StartTutorialSequence(); // Automatically start tutorial after intro
        }
        // You can add more conditions here for other specific clips
        else if (finishedClip == missionCompleteClip)
        {
            Debug.Log("Mission complete narration done. Showing score screen.");
            // UIManager.Instance.ShowScoreScreen();
        }
    }

    /// <summary>
    /// Event handler for when the entire narration queue is empty (all scheduled clips have played).
    /// </summary>
    void HandleAllNarrationFinished()
    {
        Debug.Log("GameStateManager: All queued narration has completely finished playing!");
        // This is a good point to transition game states, activate new areas, etc.
        // For example, if the tutorial sequence just ended:
        // LevelManager.Instance.UnlockNextArea();
    }

    /// <summary>
    /// Public method to start the tutorial sequence, callable from UI or other game events.
    /// </summary>
    public void StartTutorialSequence()
    {
        if (NarrationSystem.Instance != null && tutorialSequenceClips != null && tutorialSequenceClips.Count > 0)
        {
            Debug.Log("GameStateManager: Starting tutorial narration sequence.");
            // Use clearQueue: true to stop any current narration and start this new sequence immediately.
            NarrationSystem.Instance.PlayNarration(tutorialSequenceClips, clearQueue: true); 
        }
        else
        {
            Debug.LogWarning("GameStateManager: Tutorial sequence is empty or NarrationSystem not available.");
        }
    }

    /// <summary>
    /// Example of skipping current narration, e.g., bound to a keyboard key or a UI "Skip" button.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (NarrationSystem.Instance != null && NarrationSystem.Instance.IsPlaying())
            {
                NarrationSystem.Instance.SkipCurrentNarration();
                Debug.Log("GameStateManager: Skip Narration requested.");
            }
        }

        // Example: Stop all narration (e.g., if player opens a pause menu)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (NarrationSystem.Instance != null && NarrationSystem.Instance.IsPlaying())
            {
                NarrationSystem.Instance.StopNarration(clearQueue: true);
                Debug.Log("GameStateManager: All narration stopped and queue cleared.");
            }
        }
    }

    public void TriggerMissionCompleteNarration()
    {
        if (NarrationSystem.Instance != null && missionCompleteClip != null)
        {
            Debug.Log("GameStateManager: Triggering mission complete narration.");
            // We might want this to interrupt any ongoing narration, so clear the queue.
            NarrationSystem.Instance.PlayNarration(missionCompleteClip, clearQueue: true);
        }
    }
}
```

This complete example provides a robust, educational, and practical foundation for managing narrative elements in your Unity projects using the NarrationSystem design pattern.