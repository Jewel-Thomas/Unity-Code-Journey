// Unity Design Pattern Example: SoundtrackRandomizer
// This script demonstrates the SoundtrackRandomizer pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Soundtrack Randomizer** design pattern. This pattern is ideal for managing background music in games, ensuring variety by playing tracks from a pool in a random order, often with smooth transitions.

### Soundtrack Randomizer Design Pattern Explained

**Purpose:** The Soundtrack Randomizer pattern is designed to provide dynamic and varied background music by randomly selecting and playing audio tracks from a predefined collection. It enhances the player experience by preventing music from becoming repetitive while maintaining a cohesive auditory theme.

**Key Components:**

1.  **Track Pool:** A collection (e.g., `List<AudioClip>`) of all potential music tracks for a given area or game state.
2.  **Audio Player:** An `AudioSource` component responsible for playing the selected tracks.
3.  **Random Selector:** A mechanism to pick a random track from the pool, often with logic to avoid immediate repetitions.
4.  **Playback Manager:** A controller that handles starting, stopping, pausing, and sequencing of tracks. It typically manages transitions (fades) between tracks for a smoother experience.
5.  **State Management:** Flags or variables to track the current state (playing, paused, stopped) and potentially the currently playing track.

**How it Works (in this implementation):**

*   The script keeps a list of `AudioClip`s (`soundtrackClips`).
*   When `StartMusic()` is called, it kicks off a Coroutine (`PlaybackRoutine`).
*   `PlaybackRoutine` continuously:
    *   Selects a random track from `soundtrackClips` (ensuring it's not the same as the previous one if possible).
    *   Assigns the track to the `AudioSource`.
    *   Plays the track and smoothly fades its volume in.
    *   Waits until the track finishes playing.
    *   Smoothly fades the current track's volume out before starting the next one, creating a cross-fade effect.
*   Public methods (`StartMusic`, `StopMusic`, `PauseMusic`, `UnpauseMusic`, `SetVolume`) allow other scripts or UI elements to control the soundtrack.
*   Fading is handled using Coroutines, providing smooth volume changes.

---

```csharp
using UnityEngine;
using System.Collections;       // For Coroutines
using System.Collections.Generic; // For List<T>

/// <summary>
/// Implements the Soundtrack Randomizer design pattern in Unity.
/// This script manages a pool of background music tracks, playing them randomly
/// with smooth fade-in/fade-out transitions.
/// </summary>
/// <remarks>
/// Attach this script to a GameObject in your scene. It requires an AudioSource component.
/// Assign your desired music AudioClips to the 'Soundtrack Clips' list in the Inspector.
/// </remarks>
[RequireComponent(typeof(AudioSource))]
public class SoundtrackRandomizer : MonoBehaviour
{
    // =====================================================================================
    // Inspector Fields
    // =====================================================================================

    [Tooltip("List of all audio clips available for random playback in the soundtrack.")]
    [SerializeField] private List<AudioClip> soundtrackClips = new List<AudioClip>();

    [Tooltip("Master volume for the soundtrack (0.0 to 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Tooltip("Duration in seconds for fading the music in/out during transitions.")]
    [SerializeField] private float fadeDuration = 1.5f;

    // =====================================================================================
    // Private Internal State
    // =====================================================================================

    // Reference to the AudioSource component on this GameObject, used for playing audio.
    private AudioSource _audioSource;

    // Stores the index of the last played clip to help avoid immediate repetitions.
    private int _lastPlayedClipIndex = -1;

    // A flag indicating if the music system is currently active and intended to play.
    private bool _isPlayingMusic = false;

    // A reference to the currently running playback coroutine, allowing it to be stopped.
    private Coroutine _playbackCoroutine;

    // =====================================================================================
    // Unity Lifecycle Methods
    // =====================================================================================

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to get a reference to the AudioSource and initialize its basic settings.
    /// </summary>
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // Configure the AudioSource for our specific use case:
        _audioSource.playOnAwake = false; // We control when to play.
        _audioSource.loop = false;       // We manage looping manually via our coroutine.
        _audioSource.volume = 0f;        // Start at 0 volume, we'll fade in.
        _audioSource.spatialBlend = 0f;  // Usually 2D for background music.
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// This helps enforce correct AudioSource settings during design time.
    /// </summary>
    private void OnValidate()
    {
        // Get AudioSource reference if it's null (e.g., after script compilation).
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        // Apply recommended settings for background music.
        if (_audioSource != null)
        {
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f; // Ensure it's 2D for background music.
            // Clamp the AudioSource's current volume to our max volume setting.
            _audioSource.volume = Mathf.Min(_audioSource.volume, volume);
        }
    }

    // =====================================================================================
    // Public API for Controlling the Soundtrack
    // =====================================================================================

    /// <summary>
    /// Starts the random soundtrack playback. If music is already playing,
    /// it will stop the current track and begin a new random sequence.
    /// </summary>
    public void StartMusic()
    {
        if (soundtrackClips == null || soundtrackClips.Count == 0)
        {
            Debug.LogWarning("SoundtrackRandomizer: No audio clips assigned. Cannot start music.");
            return;
        }

        // Stop any currently running playback routines to ensure a clean start.
        StopAllCoroutines();
        // Immediately stop the AudioSource if it's playing.
        _audioSource.Stop();
        // Reset volume to 0 to prepare for fade-in.
        _audioSource.volume = 0f;

        _isPlayingMusic = true;
        // Start the main coroutine that manages track selection and playback.
        _playbackCoroutine = StartCoroutine(PlaybackRoutine());
        Debug.Log("SoundtrackRandomizer: Music playback initiated.");
    }

    /// <summary>
    /// Stops the random soundtrack playback. The current track will fade out.
    /// </summary>
    public void StopMusic()
    {
        if (!_isPlayingMusic) return; // Music is not active, nothing to stop.

        // Stop the main playback routine if it's active.
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }
        _isPlayingMusic = false; // Mark the system as inactive.

        // Fade out the current track and then stop the AudioSource completely.
        StartCoroutine(FadeOutAndStopRoutine(_audioSource, fadeDuration));
        Debug.Log("SoundtrackRandomizer: Music playback stopped.");
    }

    /// <summary>
    /// Pauses the current track playback. Music will resume from where it left off.
    /// </summary>
    public void PauseMusic()
    {
        if (!_isPlayingMusic) return; // Cannot pause if not intended to be playing.

        if (_audioSource.isPlaying)
        {
            _audioSource.Pause();
            Debug.Log("SoundtrackRandomizer: Music paused.");
        }
    }

    /// <summary>
    /// Resumes the current track playback if it was paused.
    /// If paused before any track started, or after a track finished while paused,
    /// it will restart the music sequence.
    /// </summary>
    public void UnpauseMusic()
    {
        if (!_isPlayingMusic) return; // Cannot unpause if not intended to be playing.

        // If audioSource was paused and has a valid clip and time, unpause it.
        if (_audioSource.time > 0 && !_audioSource.isPlaying && _audioSource.clip != null)
        {
            _audioSource.UnPause();
            Debug.Log("SoundtrackRandomizer: Music unpaused.");
        }
        else if (!_audioSource.isPlaying)
        {
            // If the AudioSource is not playing (e.g., paused before a track could start,
            // or a track finished while the system was paused), restart the whole routine.
            Debug.Log("SoundtrackRandomizer: Attempting to unpause but no current track, restarting music.");
            StartMusic();
        }
    }

    /// <summary>
    /// Sets the master volume for the soundtrack. The change will be smoothly faded.
    /// </summary>
    /// <param name="newVolume">The new volume level (0.0 to 1.0).</param>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume); // Ensure volume is within valid range.
        
        // If music is currently active, smoothly adjust the AudioSource's volume to the new target.
        if (_isPlayingMusic && _audioSource.isPlaying)
        {
            StartCoroutine(FadeAudioSourceVolume(_audioSource, volume, fadeDuration / 2f));
        } else {
            // If not playing, just set the volume directly for when it starts.
            _audioSource.volume = volume;
        }
        Debug.Log($"SoundtrackRandomizer: Volume set to {volume:F2}.");
    }

    // =====================================================================================
    // Private Coroutines and Helper Methods (Core Logic of the Pattern)
    // =====================================================================================

    /// <summary>
    /// The main coroutine that orchestrates the random playback of soundtrack tracks.
    /// It continuously selects, plays, fades, and waits for tracks to complete.
    /// </summary>
    private IEnumerator PlaybackRoutine()
    {
        while (_isPlayingMusic) // Continue as long as music is intended to be playing.
        {
            // 1. Select a new random clip.
            AudioClip nextClip = GetRandomClip();

            if (nextClip == null)
            {
                Debug.LogWarning("SoundtrackRandomizer: No valid clip found to play. Stopping music.");
                _isPlayingMusic = false;
                yield break; // Exit the coroutine if no clip can be found.
            }

            // 2. Assign and play the new clip.
            _audioSource.clip = nextClip;
            _audioSource.Play();
            Debug.Log($"SoundtrackRandomizer: Now playing '{nextClip.name}'.");

            // 3. Fade in the new track to the target volume.
            yield return StartCoroutine(FadeAudioSourceVolume(_audioSource, volume, fadeDuration));

            // 4. Wait for the current clip to finish playing naturally.
            // We check _isPlayingMusic to allow external StopMusic calls to interrupt.
            while (_audioSource.isPlaying && _audioSource.clip != null && _isPlayingMusic)
            {
                yield return null; // Wait for the next frame.
            }

            // If _isPlayingMusic became false during the wait (e.g., StopMusic was called),
            // or if the clip somehow became null, break the loop and stop.
            if (!_isPlayingMusic || _audioSource.clip == null)
            {
                break;
            }

            // 5. Fade out the current track to 0 volume before the next track starts.
            // This creates a smooth transition gap or crossfade effect.
            yield return StartCoroutine(FadeAudioSourceVolume(_audioSource, 0f, fadeDuration));

            // Optional: Add a small deliberate pause between tracks here if desired.
            // For example: yield return new WaitForSeconds(0.5f);
        }

        // Ensure AudioSource is stopped and volume is reset if the loop exits (e.g., StopMusic was called).
        _audioSource.Stop();
        _audioSource.clip = null; // Clear the clip reference.
        _audioSource.volume = 0f;
    }

    /// <summary>
    /// Selects a random audio clip from the 'soundtrackClips' list.
    /// It attempts to avoid playing the same clip twice in a row if there are multiple clips available.
    /// </summary>
    /// <returns>A randomly selected AudioClip, or null if the list is empty.</returns>
    private AudioClip GetRandomClip()
    {
        if (soundtrackClips == null || soundtrackClips.Count == 0)
        {
            return null; // No clips to play.
        }

        if (soundtrackClips.Count == 1)
        {
            _lastPlayedClipIndex = 0;
            return soundtrackClips[0]; // If only one clip, always play that one.
        }

        int newClipIndex;
        // Generate a random index, ensuring it's different from the last played clip's index.
        do
        {
            newClipIndex = Random.Range(0, soundtrackClips.Count);
        } while (newClipIndex == _lastPlayedClipIndex); // Loop until a different index is found.

        _lastPlayedClipIndex = newClipIndex; // Update the last played index.
        return soundtrackClips[newClipIndex];
    }

    /// <summary>
    /// Smoothly changes the volume of an AudioSource to a target volume over a specified duration.
    /// </summary>
    /// <param name="source">The AudioSource whose volume needs to be faded.</param>
    /// <param name="targetVolume">The desired final volume (0.0 to 1.0).</param>
    /// <param name="duration">The time in seconds over which the fade should occur.</param>
    private IEnumerator FadeAudioSourceVolume(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume; // Capture the current volume.
        float timer = 0f;

        // If duration is zero or negative, immediately set the target volume.
        if (duration <= 0.01f) 
        {
            source.volume = targetVolume;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime; // Increment timer.
            // Lerp (Linear Interpolation) calculates a smooth transition between start and target volume.
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null; // Wait for the next frame before continuing.
        }

        source.volume = targetVolume; // Ensure the volume is exactly the target at the end.
    }

    /// <summary>
    /// A helper coroutine that first fades out an AudioSource and then stops it completely.
    /// </summary>
    /// <param name="source">The AudioSource to fade out and stop.</param>
    /// <param name="duration">The time in seconds for the fade out.</param>
    private IEnumerator FadeOutAndStopRoutine(AudioSource source, float duration)
    {
        yield return StartCoroutine(FadeAudioSourceVolume(source, 0f, duration)); // Fade to zero.
        source.Stop(); // After fading, stop the audio playback.
        source.clip = null; // Clear the clip reference for good measure.
    }

    // =====================================================================================
    // Example Usage (for educational purposes - uncomment to use)
    // =====================================================================================
    /*
    /// <summary>
    /// Example of how to use the SoundtrackRandomizer from another script
    /// or from within itself for quick testing.
    /// </summary>
    private void Start()
    {
        // To start music automatically when the game begins:
        // StartMusic(); 
    }

    /// <summary>
    /// Example of how to trigger music actions based on input (e.g., UI button, keyboard).
    /// </summary>
    private void Update()
    {
        // --- Music Control ---
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Attempting to Start Music (Key 'S')");
            StartMusic();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Attempting to Stop Music (Key 'X')");
            StopMusic();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Attempting to Pause/Unpause Music (Key 'P')");
            if (_audioSource.isPlaying) // Check if the AudioSource is currently playing
            {
                PauseMusic();
            }
            else if (_isPlayingMusic) // Check if our system *intends* to be playing (i.e., paused)
            {
                UnpauseMusic();
            }
            else // If not playing at all, start it.
            {
                StartMusic();
            }
        }

        // --- Volume Control (example with arrow keys) ---
        if (Input.GetKey(KeyCode.UpArrow))
        {
            SetVolume(volume + Time.deltaTime * 0.1f); // Increase volume slowly
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            SetVolume(volume - Time.deltaTime * 0.1f); // Decrease volume slowly
        }
    }
    */
}
```

### How to Use in a Unity Project:

1.  **Create a New C# Script:** In your Unity project, right-click in the Project window -> Create -> C# Script. Name it `SoundtrackRandomizer`.
2.  **Copy and Paste:** Replace the default content of the new script with the code provided above.
3.  **Create a GameObject:** In your Hierarchy, right-click -> Create Empty. Name it something like `MusicManager`.
4.  **Add Script to GameObject:** Drag the `SoundtrackRandomizer` script from your Project window onto the `MusicManager` GameObject in the Hierarchy, or select `MusicManager` and click "Add Component" in the Inspector, then search for `SoundtrackRandomizer`.
5.  **Assign Audio Clips:** Select the `MusicManager` GameObject. In the Inspector, you will see the `Soundtrack Randomizer` component.
    *   Find the "Soundtrack Clips" list.
    *   Set its size to the number of music tracks you want.
    *   Drag your `AudioClip` assets (your music files) from your Project window into the individual slots in the "Soundtrack Clips" list.
6.  **Adjust Settings:**
    *   `Volume`: Set your desired default master volume.
    *   `Fade Duration`: Adjust how quickly tracks fade in and out.
7.  **Start Music (Programmatically):**
    *   To start the music from another script (e.g., when the game starts, entering a new scene):
        ```csharp
        public class MyGameController : MonoBehaviour
        {
            public SoundtrackRandomizer musicManager; // Assign in Inspector

            void Start()
            {
                musicManager.StartMusic();
            }

            // Example for UI button click:
            public void OnPlayMusicButtonClicked()
            {
                musicManager.StartMusic();
            }
        }
        ```
    *   Alternatively, uncomment the `Start()` method in the `SoundtrackRandomizer` script itself if you want the music to start automatically when the GameObject is active.
8.  **Control Music (Programmatically):** Use the public methods:
    *   `musicManager.StopMusic();`
    *   `musicManager.PauseMusic();`
    *   `musicManager.UnpauseMusic();`
    *   `musicManager.SetVolume(0.5f);`
9.  **Test with Example Usage:** Uncomment the `Update()` method in the `SoundtrackRandomizer` script to test with keyboard input (S, X, P, Up/Down Arrow keys). Remember to re-comment or remove it for production builds.

This setup provides a robust and flexible background music system that enhances the player's audio experience with dynamic track selection and smooth transitions.