// Unity Design Pattern Example: SubtitleSystem
// This script demonstrates the SubtitleSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a practical 'SubtitleSystem' design pattern in Unity. While 'SubtitleSystem' isn't a formal GoF design pattern, it's an architectural pattern for managing temporal text (subtitles). It combines several core patterns like **Singleton**, **Observer (or Publish-Subscribe)**, and **Data-Driven Design** to create a robust and flexible system.

The core idea is:
1.  **Data:** Define how a single subtitle entry and a collection of entries (a "track") are structured. (Using `[System.Serializable]` and `ScriptableObject`).
2.  **Manager (Singleton):** A central component that loads subtitle tracks, tracks playback time, determines which subtitle is active, and *publishes* events.
3.  **Display (Observer):** UI components that *subscribe* to the manager's events and update the on-screen display when a subtitle changes.
4.  **Decoupling:** The Manager and Display components are decoupled; the Manager doesn't know *how* subtitles are displayed, only *that* they need to be displayed. The Display doesn't know *when* a subtitle is coming, only *what* subtitle it is when it arrives.

---

### **Setup Instructions for Unity Project:**

1.  **Create a new C# Script:** Name it `SubtitleSystemExample` (or whatever you prefer) and paste the entire code below into it.
2.  **Install TextMeshPro:** If you haven't already, go to `Window > TextMeshPro > Import TMP Essential Resources`. This is required for `TMP_Text`.
3.  **Create an Empty GameObject:** In your scene, create an empty GameObject and name it `SubtitleManager`. Attach the `SubtitleSystemExample` script to it.
4.  **Create a UI Canvas:**
    *   Right-click in the Hierarchy -> `UI` -> `Canvas`.
    *   Add an `Empty` GameObject under Canvas, name it `SubtitleDisplay`.
    *   Right-click `SubtitleDisplay` -> `UI` -> `TextMeshPro - Text`. Name this `SubtitleText`.
    *   Adjust `SubtitleText` properties: Set `Rect Transform` anchors to bottom-center, adjust position (e.g., `PosX: 0, PosY: 100`), `Width: 800, Height: 100`. Set `Alignment` to Middle-Center. Set `Font Size` larger (e.g., 36).
    *   Attach the `SubtitleSystemExample` script to the `SubtitleDisplay` GameObject.
    *   On the `SubtitleDisplay` GameObject, drag the `SubtitleText` `TextMeshPro - Text` component into the "Subtitle Text Element" slot in the Inspector.
5.  **Create a Subtitle Track Asset:**
    *   In your Project window, right-click -> `Create` -> `Subtitle System` -> `Subtitle Track`. Name it `MyFirstSubtitleTrack`.
    *   Select `MyFirstSubtitleTrack`. In the Inspector, expand "Subtitle Entries".
    *   Set "Size" to `3` (or more).
    *   Fill in example subtitles:
        *   **Element 0:** `Start Time: 0`, `Duration: 3`, `Text: Hello, and welcome to this example!`
        *   **Element 1:** `Start Time: 4`, `Duration: 4`, `Text: This demonstrates a practical Subtitle System.`
        *   **Element 2:** `Start Time: 9`, `Duration: 5`, `Text: Enjoy learning about design patterns in Unity!`
6.  **Create a Trigger (Optional for automated playback):**
    *   Create an empty GameObject in your scene, name it `SubtitleTrigger`.
    *   Attach the `SubtitleSystemExample` script to it.
    *   On the `SubtitleTrigger` GameObject, drag `MyFirstSubtitleTrack` asset from your Project window into the "Subtitle Track To Play" slot in the Inspector.
    *   Make sure `Play On Start` is checked.
7.  **Run the Scene:** Press Play. You should see the subtitles appear sequentially at the bottom of the screen.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro

// Important Note: In a real project, each of these classes (SubtitleEntry,
// SubtitleTrackSO, SubtitleManager, SubtitleDisplayUI, SubtitleTriggerExample)
// would typically be in its own C# file for better organization and maintainability.
// They are combined here into a single file as per the request for "a complete, working C# script"
// that demonstrates the pattern in one go.

// --- 1. SubtitleEntry: The Data Structure for a Single Subtitle ---
/// <summary>
/// Represents a single subtitle entry with its text, start time, and duration.
/// This class is marked [System.Serializable] so it can be embedded directly
/// into ScriptableObjects or MonoBehaviours and edited in the Unity Inspector.
/// </summary>
[System.Serializable]
public class SubtitleEntry
{
    [Tooltip("The time (in seconds) from the start of the audio/video when this subtitle should appear.")]
    public float startTime;
    [Tooltip("How long (in seconds) this subtitle should remain on screen.")]
    public float duration;
    [TextArea(1, 5)] // Allows for multi-line text input in the Inspector
    [Tooltip("The actual text content of the subtitle.")]
    public string text;

    // Optional: Add more fields for speaker, style, language key, etc.
    // public string speakerName;
    // public SubtitleStyle style;
    // public string languageKey;

    public float endTime => startTime + duration; // Convenience property
}

// --- 2. SubtitleTrackSO: ScriptableObject to store a collection of SubtitleEntries ---
/// <summary>
/// A ScriptableObject that holds a collection of SubtitleEntry objects.
/// This allows designers to create and manage subtitle tracks as assets directly
/// in the Unity Project window, making it easy to associate them with audio/video.
/// </summary>
[CreateAssetMenu(fileName = "NewSubtitleTrack", menuName = "Subtitle System/Subtitle Track")]
public class SubtitleTrackSO : ScriptableObject
{
    [Tooltip("A list of all subtitle entries in this track, ordered by start time.")]
    public List<SubtitleEntry> subtitleEntries = new List<SubtitleEntry>();

    // You might add metadata here like language, track name, etc.
    // public string trackName;
    // public SystemLanguage language;

    void OnValidate()
    {
        // Optional: Ensure subtitles are sorted by start time
        // This makes the playback logic simpler and more efficient.
        subtitleEntries.Sort((a, b) => a.startTime.CompareTo(b.startTime));
    }
}

// --- 3. SubtitleManager: The Core Singleton Manager ---
/// <summary>
/// The central component of the Subtitle System. It acts as a Singleton,
/// managing subtitle playback, tracking current time, and notifying
/// other components when subtitles need to be displayed or hidden.
///
/// Design Pattern:
/// - Singleton: Ensures there's only one instance globally accessible.
/// - Observer/Publish-Subscribe: Uses C# events to notify `SubtitleDisplayUI`
///   (and any other interested parties) about subtitle changes without
///   direct coupling.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    public static SubtitleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Another SubtitleManager instance found. Destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make it persist across scene loads if subtitles should continue
        // DontDestroyOnLoad(gameObject);
    }

    // --- Events (Publisher Role) ---
    // These events are the core of the Observer/Publish-Subscribe pattern.
    // SubtitleDisplayUI (and others) will subscribe to these.
    public delegate void SubtitleEventHandler(SubtitleEntry subtitle);
    public static event SubtitleEventHandler OnSubtitleDisplayed; // Fired when a new subtitle becomes active.
    public static event SubtitleEventHandler OnSubtitleHidden;    // Fired when the active subtitle ends.
    public static event SubtitleEventHandler OnSubtitleUpdated;   // Fired when an active subtitle's details might change (less common).
    public static event System.Action OnSubtitleTrackFinished; // Fired when the entire track has finished playing.

    // --- Manager State Variables ---
    private SubtitleTrackSO _currentTrack;
    private int _currentSubtitleIndex = -1;
    private SubtitleEntry _activeSubtitle = null;

    private float _playbackTime = 0f; // Current time relative to the start of the subtitle track
    private bool _isPlaying = false;
    private bool _isPaused = false;

    // --- Public API for Controlling Subtitles ---

    /// <summary>
    /// Loads a new subtitle track. This will reset the playback time and stop any current playback.
    /// </summary>
    /// <param name="track">The SubtitleTrackSO asset to load.</param>
    public void LoadSubtitleTrack(SubtitleTrackSO track)
    {
        if (track == null)
        {
            Debug.LogError("Attempted to load a null subtitle track.", this);
            return;
        }

        Stop(); // Stop any currently playing track
        _currentTrack = track;
        Debug.Log($"Loaded subtitle track: {_currentTrack.name}", this);
    }

    /// <summary>
    /// Starts or resumes playback of the currently loaded subtitle track.
    /// If no track is loaded, it will log an error.
    /// </summary>
    public void Play()
    {
        if (_currentTrack == null)
        {
            Debug.LogWarning("Cannot play subtitles: No track loaded.", this);
            return;
        }

        if (_isPlaying && !_isPaused) return; // Already playing

        _isPlaying = true;
        _isPaused = false;
        Debug.Log("Subtitle playback started/resumed.", this);
        // Ensure Update loop is active
        enabled = true;
    }

    /// <summary>
    /// Pauses playback of the current subtitle track.
    /// </summary>
    public void Pause()
    {
        if (!_isPlaying) return; // Not playing, nothing to pause

        _isPaused = true;
        Debug.Log("Subtitle playback paused.", this);
    }

    /// <summary>
    /// Stops playback, hides any active subtitle, and resets the playback time to 0.
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        _isPaused = false;
        _playbackTime = 0f;
        _currentSubtitleIndex = -1;

        if (_activeSubtitle != null)
        {
            // Notify subscribers that the subtitle is now hidden
            OnSubtitleHidden?.Invoke(_activeSubtitle);
            _activeSubtitle = null;
        }
        Debug.Log("Subtitle playback stopped.", this);
        // Disable Update loop if not playing to save performance
        enabled = false;
    }

    /// <summary>
    /// Seeks to a specific time in the subtitle track.
    /// This will also play the track if it's currently stopped or paused.
    /// </summary>
    /// <param name="timeInSeconds">The target time in seconds.</param>
    public void Seek(float timeInSeconds)
    {
        if (_currentTrack == null)
        {
            Debug.LogWarning("Cannot seek subtitles: No track loaded.", this);
            return;
        }

        _playbackTime = Mathf.Max(0f, timeInSeconds);
        _isPlaying = true;
        _isPaused = false;
        _currentSubtitleIndex = -1; // Reset index to re-evaluate from the start

        if (_activeSubtitle != null)
        {
            OnSubtitleHidden?.Invoke(_activeSubtitle);
            _activeSubtitle = null;
        }
        Debug.Log($"Subtitle seeked to: {timeInSeconds:F2}s", this);
        // Ensure Update loop is active
        enabled = true;
    }

    // --- Internal Playback Logic (Update Loop) ---

    private void Update()
    {
        if (!_isPlaying || _isPaused || _currentTrack == null || _currentTrack.subtitleEntries.Count == 0)
        {
            return; // No active playback or no track to play
        }

        _playbackTime += Time.deltaTime;

        // Check if playback has finished
        if (_currentSubtitleIndex >= _currentTrack.subtitleEntries.Count && _activeSubtitle == null)
        {
            Debug.Log("Subtitle track finished.", this);
            OnSubtitleTrackFinished?.Invoke();
            Stop(); // Automatically stop when finished
            return;
        }

        HandleSubtitleDisplay();
    }

    /// <summary>
    /// Manages the display and hiding of subtitles based on current playback time.
    /// This is where the core timing logic resides.
    /// </summary>
    private void HandleSubtitleDisplay()
    {
        // Case 1: An active subtitle is currently displayed. Check if it's time to hide it.
        if (_activeSubtitle != null)
        {
            if (_playbackTime >= _activeSubtitle.endTime)
            {
                // The active subtitle has ended.
                OnSubtitleHidden?.Invoke(_activeSubtitle); // Notify subscribers
                _activeSubtitle = null;
            }
            else
            {
                // Subtitle is still active, nothing to do (or invoke OnSubtitleUpdated if needed)
                return;
            }
        }

        // Case 2: No subtitle is active. Look for the next one to display.
        if (_activeSubtitle == null)
        {
            // Iterate from the current index (or start if reset) to find the next subtitle.
            // Using a while loop to potentially jump over multiple subtitles if seeking far ahead.
            while (_currentSubtitleIndex + 1 < _currentTrack.subtitleEntries.Count)
            {
                SubtitleEntry nextSubtitle = _currentTrack.subtitleEntries[_currentSubtitleIndex + 1];

                if (_playbackTime >= nextSubtitle.startTime && _playbackTime < nextSubtitle.endTime)
                {
                    // Found the next subtitle to display.
                    _activeSubtitle = nextSubtitle;
                    _currentSubtitleIndex++;
                    OnSubtitleDisplayed?.Invoke(_activeSubtitle); // Notify subscribers
                    return; // Found and displayed, exit.
                }
                else if (_playbackTime < nextSubtitle.startTime)
                {
                    // The next subtitle is in the future, wait for it.
                    return;
                }
                else
                {
                    // We've passed this subtitle without displaying it (e.g., due to a seek past its end).
                    // Move to the next one to check.
                    _currentSubtitleIndex++;
                }
            }
        }
    }

    // --- Cleanup ---
    private void OnDestroy()
    {
        // Good practice to clear static events to prevent memory leaks
        // if this is the only instance or if new scenes might subscribe.
        OnSubtitleDisplayed = null;
        OnSubtitleHidden = null;
        OnSubtitleUpdated = null;
        OnSubtitleTrackFinished = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }
}


// --- 4. SubtitleDisplayUI: The UI Component (Observer Role) ---
/// <summary>
/// This MonoBehaviour listens to events from the `SubtitleManager` and updates
/// UI Text elements to display the current subtitle.
///
/// Design Pattern:
/// - Observer: Subscribes to `SubtitleManager` events to react to subtitle changes.
/// - Separation of Concerns: Handles only the presentation logic, not playback timing.
/// </summary>
public class SubtitleDisplayUI : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component that will display the subtitle text.")]
    [SerializeField] private TMP_Text _subtitleTextElement;

    [Tooltip("The GameObject (or CanvasGroup) that holds the subtitle UI elements.")]
    [SerializeField] private GameObject _subtitlePanel;

    private void Awake()
    {
        // Ensure UI elements are assigned
        if (_subtitleTextElement == null)
        {
            _subtitleTextElement = GetComponentInChildren<TMP_Text>();
            if (_subtitleTextElement == null)
            {
                Debug.LogError("SubtitleDisplayUI: No TMP_Text component found in children. Please assign one.", this);
                enabled = false;
                return;
            }
        }

        if (_subtitlePanel == null)
        {
            _subtitlePanel = _subtitleTextElement.transform.parent.gameObject;
            if (_subtitlePanel == null)
            {
                Debug.LogWarning("SubtitleDisplayUI: No explicit subtitle panel assigned. Using parent of text element.", this);
            }
        }

        // Initially hide the panel
        if (_subtitlePanel != null) _subtitlePanel.SetActive(false);
        _subtitleTextElement.text = ""; // Clear any default text
    }

    private void OnEnable()
    {
        // Subscribe to SubtitleManager events
        SubtitleManager.OnSubtitleDisplayed += OnDisplaySubtitle;
        SubtitleManager.OnSubtitleHidden += OnHideSubtitle;
        SubtitleManager.OnSubtitleTrackFinished += OnTrackFinished;
        Debug.Log("SubtitleDisplayUI subscribed to SubtitleManager events.", this);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and unwanted behavior
        SubtitleManager.OnSubtitleDisplayed -= OnDisplaySubtitle;
        SubtitleManager.OnSubtitleHidden -= OnHideSubtitle;
        SubtitleManager.OnSubtitleTrackFinished -= OnTrackFinished;
        Debug.Log("SubtitleDisplayUI unsubscribed from SubtitleManager events.", this);
    }

    /// <summary>
    /// Event handler for when a new subtitle needs to be displayed.
    /// </summary>
    /// <param name="subtitle">The subtitle entry to display.</param>
    private void OnDisplaySubtitle(SubtitleEntry subtitle)
    {
        if (_subtitlePanel != null) _subtitlePanel.SetActive(true);
        _subtitleTextElement.text = subtitle.text;
        Debug.Log($"Displaying Subtitle: '{subtitle.text}'", this);
        // Here you could also handle speaker name, font changes, etc.
    }

    /// <summary>
    /// Event handler for when the active subtitle needs to be hidden.
    /// </summary>
    /// <param name="subtitle">The subtitle entry that was just hidden (optional, can be ignored).</param>
    private void OnHideSubtitle(SubtitleEntry subtitle)
    {
        if (_subtitlePanel != null) _subtitlePanel.SetActive(false);
        _subtitleTextElement.text = ""; // Clear text
        Debug.Log($"Hiding Subtitle: '{subtitle.text}'", this);
    }

    /// <summary>
    /// Event handler for when the entire subtitle track has finished.
    /// </summary>
    private void OnTrackFinished()
    {
        if (_subtitlePanel != null) _subtitlePanel.SetActive(false);
        _subtitleTextElement.text = "";
        Debug.Log("Subtitle track display finished.", this);
    }
}

// --- 5. SubtitleTriggerExample: A simple demonstration script ---
/// <summary>
/// This script demonstrates how to interact with the SubtitleManager
/// to load and play a subtitle track. It's a simple example of a "client"
/// of the Subtitle System.
/// </summary>
public class SubtitleTriggerExample : MonoBehaviour
{
    [Tooltip("The SubtitleTrackSO asset to play.")]
    [SerializeField] private SubtitleTrackSO _subtitleTrackToPlay;

    [Tooltip("If true, the subtitles will start playing automatically when the scene starts.")]
    [SerializeField] private bool _playOnStart = true;

    [Tooltip("Optional: A button to trigger subtitle playback manually.")]
    [SerializeField] private KeyCode _triggerKey = KeyCode.Space;

    void Start()
    {
        if (_playOnStart && _subtitleTrackToPlay != null)
        {
            StartCoroutine(DelayedPlay());
        }
        else if (_subtitleTrackToPlay == null)
        {
            Debug.LogWarning("SubtitleTriggerExample: No Subtitle Track assigned to play.", this);
        }
    }

    // A small delay to ensure SubtitleManager's Awake has run and events are set up.
    IEnumerator DelayedPlay()
    {
        yield return null; // Wait one frame

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.LoadSubtitleTrack(_subtitleTrackToPlay);
            SubtitleManager.Instance.Play();
        }
        else
        {
            Debug.LogError("SubtitleManager not found in scene.", this);
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(_triggerKey))
        {
            if (SubtitleManager.Instance != null && _subtitleTrackToPlay != null)
            {
                Debug.Log($"Triggering subtitle playback for track: {_subtitleTrackToPlay.name}", this);
                SubtitleManager.Instance.LoadSubtitleTrack(_subtitleTrackToPlay);
                SubtitleManager.Instance.Play();
            }
            else if (SubtitleManager.Instance == null)
            {
                Debug.LogError("SubtitleManager not found in scene.", this);
            }
            else if (_subtitleTrackToPlay == null)
            {
                Debug.LogWarning("No Subtitle Track assigned to play in SubtitleTriggerExample.", this);
            }
        }

        if (Input.GetKeyDown(KeyCode.P)) // Example: Pause/Resume
        {
            if (SubtitleManager.Instance != null)
            {
                if (SubtitleManager.Instance.enabled) // Check if manager is currently processing time
                {
                    SubtitleManager.Instance.Pause();
                }
                else
                {
                    SubtitleManager.Instance.Play();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.S)) // Example: Stop
        {
            SubtitleManager.Instance?.Stop();
        }
    }
}
```