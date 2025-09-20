// Unity Design Pattern Example: MusicManager
// This script demonstrates the MusicManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script provides a complete and practical implementation of the **MusicManager design pattern** in Unity, commonly used for handling background music (BGM) across different scenes. It leverages the **Singleton pattern** for easy global access and includes features like playing specific tracks, managing a playlist, volume control, and smooth crossfading.

To use this script:
1.  Create a new C# script named `MusicManager` in your Unity project.
2.  Copy and paste the code below into the script.
3.  Drag the `MusicManager` script onto an empty GameObject in your first scene (e.g., a "Bootstrap" or "Managers" scene).
4.  Populate the `Music Tracks` array in the Inspector with your `AudioClip` assets.
5.  Optionally, adjust the `Default Volume` and `Fade Duration`.

The `MusicManager` GameObject will automatically persist across scene changes, and a new one will be created if it doesn't exist when `MusicManager.Instance` is first accessed.

---

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Not strictly needed for this basic version but good practice for collections

/// <summary>
/// MusicManager Singleton for playing and managing background music (BGM) in Unity scenes.
///
/// This script implements the **Singleton design pattern** to ensure only one instance
/// of the MusicManager exists throughout the game. It persists across scene loads
/// using `DontDestroyOnLoad`, providing a centralized and easily accessible way to
/// control music playback from anywhere in your project.
///
/// Features:
/// - Singleton access: `MusicManager.Instance`
/// - Persists across scenes
/// - Manages a single AudioSource for music playback
/// - Play specific AudioClips or manage a playlist from the Inspector
/// - Volume control
/// - Play, Pause, Resume, Stop functionality
/// - Smooth crossfading between tracks
/// - Error handling for null clips or invalid indices
/// </summary>
public class MusicManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // A private static reference to the single instance of the MusicManager.
    private static MusicManager _instance;

    /// <summary>
    /// Public static property to access the singleton instance.
    /// This uses a lazy initialization pattern, creating the instance if it doesn't already exist.
    /// </summary>
    public static MusicManager Instance
    {
        get
        {
            // If the instance is null, try to find an existing one in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<MusicManager>();

                // If no instance exists after searching, create a new GameObject and add the MusicManager component to it.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("MusicManager");
                    _instance = singletonObject.AddComponent<MusicManager>();
                    Debug.Log("[MusicManager] A new MusicManager instance was created automatically.");
                }
            }
            return _instance;
        }
    }

    // --- Inspector Settings ---
    [Header("Audio Settings")]
    [Tooltip("The AudioSource component used for playing background music.")]
    [SerializeField]
    private AudioSource _musicAudioSource;

    [Tooltip("Default volume for the music (0.0 to 1.0).")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _defaultVolume = 0.7f;

    [Tooltip("The duration in seconds for crossfading between music tracks.")]
    [SerializeField]
    private float _fadeDuration = 1.5f;

    [Header("Music Tracks (Playlist)")]
    [Tooltip("An array of AudioClips that can be played by the MusicManager's internal playlist functions.")]
    [SerializeField]
    private AudioClip[] _musicTracks;

    // --- Private Members ---
    private int _currentTrackIndex = -1; // -1 indicates no track is currently selected from the array
    private bool _isFading = false; // Flag to prevent multiple fade coroutines from running simultaneously

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the singleton pattern is correctly enforced and initializes the AudioSource.
    /// </summary>
    private void Awake()
    {
        // If an instance already exists and it's not THIS instance, destroy this duplicate.
        // This handles cases where a MusicManager might be accidentally placed in multiple scenes.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy the new GameObject attempting to become a duplicate.
            Debug.LogWarning("[MusicManager] Duplicate MusicManager instance detected and destroyed.");
            return;
        }

        // Set this instance as the official singleton.
        _instance = this;

        // Make sure this GameObject (and its MusicManager component) persists across scene loads.
        DontDestroyOnLoad(gameObject);

        // Ensure an AudioSource component exists on this GameObject. If not, add one.
        if (_musicAudioSource == null)
        {
            _musicAudioSource = GetComponent<AudioSource>();
            if (_musicAudioSource == null)
            {
                _musicAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("[MusicManager] Added AudioSource component to MusicManager GameObject.");
            }
        }

        // Configure the AudioSource for music playback.
        _musicAudioSource.loop = false; // Music typically doesn't loop forever unless explicitly told.
        _musicAudioSource.playOnAwake = false; // We'll control playback manually.
        _musicAudioSource.volume = _defaultVolume; // Set initial volume from Inspector setting.
        _musicAudioSource.bypassReverbZones = true; // Music usually shouldn't be affected by reverb zones.
        _musicAudioSource.priority = 0; // Highest priority for music.
    }

    /// <summary>
    /// Called once after all Awake calls.
    /// Can be used to play an initial track when the game starts.
    /// Uncomment the example code if you want music to start automatically.
    /// </summary>
    private void Start()
    {
        // Example: Automatically play the first track if available when the game starts.
        // This is optional; you might prefer to trigger music from a specific game state script.
        // if (_musicTracks != null && _musicTracks.Length > 0 && !_musicAudioSource.isPlaying)
        // {
        //     PlayTrackByIndex(0); // Use crossfade for initial play as well for consistency.
        // }
    }


    // --- Public Music Control API ---

    /// <summary>
    /// Plays a specific AudioClip immediately, stopping any currently playing music.
    /// This method does NOT use crossfading. It's an abrupt switch.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="loop">If true, the clip will loop indefinitely.</param>
    /// <param name="startTime">The time in seconds to start playing the clip from.</param>
    public void PlayMusic(AudioClip clip, bool loop = false, float startTime = 0f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[MusicManager] Attempted to play a null AudioClip.");
            return;
        }

        // Stop any ongoing fade coroutines to prevent conflicts.
        if (_isFading)
        {
            StopAllCoroutines();
            _isFading = false;
        }

        _musicAudioSource.Stop();       // Stop current sound
        _musicAudioSource.clip = clip;  // Assign new clip
        _musicAudioSource.loop = loop;  // Set loop status
        _musicAudioSource.time = startTime; // Set start time
        _musicAudioSource.Play();       // Start playing
        _musicAudioSource.volume = _defaultVolume; // Ensure volume is at default level

        Debug.Log($"[MusicManager] Started playing immediately: {clip.name}");

        // Reset current track index if playing a custom clip not from the internal array.
        _currentTrackIndex = -1;
    }

    /// <summary>
    /// Plays a specific AudioClip with a smooth crossfade effect.
    /// Fades out the current music and fades in the new music simultaneously (or sequentially
    /// if using a single AudioSource as implemented here for simplicity).
    /// </summary>
    /// <param name="clip">The AudioClip to crossfade to.</param>
    /// <param name="loop">If true, the new clip will loop indefinitely.</param>
    public void CrossfadeMusic(AudioClip clip, bool loop = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("[MusicManager] Attempted to crossfade to a null AudioClip.");
            return;
        }

        // If a fade is already in progress, stop it to start a new one.
        if (_isFading)
        {
            StopAllCoroutines();
            _isFading = false;
            // Optionally, you might want to immediately stop or reset volume here
            // if an interrupted fade would leave the audio in an undesirable state.
        }

        StartCoroutine(CrossfadeCoroutine(clip, loop, _fadeDuration));
        Debug.Log($"[MusicManager] Initiating crossfade to: {clip.name}");
    }

    /// <summary>
    /// Plays the music track at the specified index from the `_musicTracks` array.
    /// Uses the crossfade effect.
    /// </summary>
    /// <param name="trackIndex">The zero-based index of the track in the `_musicTracks` array.</param>
    /// <param name="loop">If true, the selected track will loop indefinitely.</param>
    public void PlayTrackByIndex(int trackIndex, bool loop = false)
    {
        if (_musicTracks == null || trackIndex < 0 || trackIndex >= _musicTracks.Length)
        {
            Debug.LogWarning($"[MusicManager] Invalid track index: {trackIndex}. MusicTracks array is null or index is out of bounds.");
            return;
        }

        // Prevent restarting the same track if it's already playing.
        if (_currentTrackIndex == trackIndex && _musicAudioSource.isPlaying && _musicAudioSource.clip == _musicTracks[trackIndex])
        {
            Debug.Log($"[MusicManager] Track '{_musicTracks[trackIndex].name}' is already playing.");
            return;
        }

        _currentTrackIndex = trackIndex;
        CrossfadeMusic(_musicTracks[trackIndex], loop);
    }

    /// <summary>
    /// Plays the next music track in the `_musicTracks` array (playlist).
    /// If at the end of the array, it loops back to the first track. Uses crossfading.
    /// </summary>
    /// <param name="loop">If true, the next track played will loop indefinitely.</param>
    public void PlayNextTrack(bool loop = false)
    {
        if (_musicTracks == null || _musicTracks.Length == 0)
        {
            Debug.LogWarning("[MusicManager] No music tracks available to play next in the playlist.");
            return;
        }

        // Increment index, looping back to 0 if we go past the last track.
        _currentTrackIndex = (_currentTrackIndex + 1) % _musicTracks.Length;
        CrossfadeMusic(_musicTracks[_currentTrackIndex], loop);
    }

    /// <summary>
    /// Plays a random music track from the `_musicTracks` array. Uses crossfading.
    /// Attempts to avoid playing the same track twice in a row if there are multiple options.
    /// </summary>
    /// <param name="loop">If true, the random track played will loop indefinitely.</param>
    public void PlayRandomTrack(bool loop = false)
    {
        if (_musicTracks == null || _musicTracks.Length == 0)
        {
            Debug.LogWarning("[MusicManager] No music tracks available to play randomly.");
            return;
        }

        int randomIndex = Random.Range(0, _musicTracks.Length);
        // If there's more than one track, try to pick a different one than the current.
        if (_musicTracks.Length > 1)
        {
            while (randomIndex == _currentTrackIndex)
            {
                randomIndex = Random.Range(0, _musicTracks.Length);
            }
        }
        _currentTrackIndex = randomIndex;
        CrossfadeMusic(_musicTracks[_currentTrackIndex], loop);
    }


    /// <summary>
    /// Pauses the current music playback.
    /// </summary>
    public void PauseMusic()
    {
        if (_musicAudioSource.isPlaying)
        {
            _musicAudioSource.Pause();
            Debug.Log("[MusicManager] Music paused.");
        }
    }

    /// <summary>
    /// Resumes the current music playback from where it was paused.
    /// </summary>
    public void ResumeMusic()
    {
        if (!_musicAudioSource.isPlaying && _musicAudioSource.clip != null)
        {
            _musicAudioSource.Play();
            Debug.Log("[MusicManager] Music resumed.");
        } else if (_musicAudioSource.isPlaying) {
             Debug.Log("[MusicManager] Music is already playing, cannot resume.");
        } else if (_musicAudioSource.clip == null) {
            Debug.LogWarning("[MusicManager] No AudioClip set to resume.");
        }
    }

    /// <summary>
    /// Stops the current music playback and clears the AudioSource's clip.
    /// </summary>
    public void StopMusic()
    {
        if (_musicAudioSource.isPlaying || _musicAudioSource.clip != null)
        {
            // Stop any ongoing fade coroutines.
            if (_isFading)
            {
                StopAllCoroutines();
                _isFading = false;
            }
            _musicAudioSource.Stop();
            _musicAudioSource.clip = null; // Clear the clip
            Debug.Log("[MusicManager] Music stopped.");
        }
    }

    /// <summary>
    /// Fades out the current music over a specified duration.
    /// </summary>
    /// <param name="duration">The duration of the fade out in seconds.</param>
    public void FadeOutMusic(float duration)
    {
        if (_musicAudioSource.isPlaying && !_isFading)
        {
            StartCoroutine(FadeOutCoroutine(duration));
            Debug.Log($"[MusicManager] Initiating music fade out over {duration} seconds.");
        } else if (_isFading) {
            Debug.Log("[MusicManager] Already fading. Cannot start new fade out.");
        } else if (!_musicAudioSource.isPlaying) {
            Debug.Log("[MusicManager] Music is not playing. Cannot fade out.");
        }
    }

    /// <summary>
    /// Fades in the current music from silence over a specified duration.
    /// This method assumes an AudioClip is already assigned to the AudioSource (and possibly paused).
    /// If you want to play a new clip and fade it in, consider using `CrossfadeMusic` instead.
    /// </summary>
    /// <param name="duration">The duration of the fade in in seconds.</param>
    public void FadeInMusic(float duration)
    {
        if (_musicAudioSource.clip != null && !_musicAudioSource.isPlaying && !_isFading)
        {
            _musicAudioSource.volume = 0f; // Start from silence
            _musicAudioSource.Play();      // Start playing
            StartCoroutine(FadeInCoroutine(duration));
            Debug.Log($"[MusicManager] Initiating music fade in over {duration} seconds.");
        } else if (_musicAudioSource.isPlaying) {
             Debug.LogWarning("[MusicManager] Music is already playing. Cannot fade in.");
        } else if (_musicAudioSource.clip == null) {
            Debug.LogWarning("[MusicManager] No AudioClip set to fade in.");
        } else if (_isFading) {
            Debug.LogWarning("[MusicManager] Already fading. Cannot start new fade in.");
        }
    }

    /// <summary>
    /// Sets the global music volume. This also updates the default volume setting.
    /// </summary>
    /// <param name="volume">The new volume level (0.0 to 1.0).</param>
    public void SetVolume(float volume)
    {
        _defaultVolume = Mathf.Clamp01(volume); // Clamp to ensure valid range (0.0 to 1.0)
        if (!_isFading) // Only update AudioSource volume directly if not currently fading.
        {
            _musicAudioSource.volume = _defaultVolume;
        }
        Debug.Log($"[MusicManager] Music volume set to: {_defaultVolume}");
    }

    /// <summary>
    /// Gets the current *default* global music volume.
    /// Note: This returns the `_defaultVolume`, not the current `AudioSource.volume`
    /// which might be temporarily lower during a fade out.
    /// </summary>
    /// <returns>The current default music volume.</returns>
    public float GetVolume()
    {
        return _defaultVolume;
    }

    /// <summary>
    /// Checks if music is currently playing from the AudioSource.
    /// </summary>
    /// <returns>True if music is playing, false otherwise.</returns>
    public bool IsPlaying()
    {
        return _musicAudioSource.isPlaying;
    }

    /// <summary>
    /// Checks if a fade operation (in or out) is currently in progress.
    /// </summary>
    /// <returns>True if a fade is active, false otherwise.</returns>
    public bool IsFading()
    {
        return _isFading;
    }

    // --- Coroutines for Fading ---
    // Note: For true *simultaneous* crossfading, a second AudioSource would typically be used.
    // This implementation uses a single AudioSource by fading out, switching clips, then fading in,
    // which gives a smooth but not strictly simultaneous transition.

    /// <summary>
    /// Coroutine to handle crossfading between music tracks using a single AudioSource.
    /// It fades out the current track, then immediately switches the clip and fades in the new one.
    /// The total time for this sequence is `duration`.
    /// </summary>
    /// <param name="newClip">The new AudioClip to fade in.</param>
    /// <param name="loop">If true, the new clip will loop.</param>
    /// <param name="duration">The total duration of the crossfade (half for out, half for in).</param>
    private IEnumerator CrossfadeCoroutine(AudioClip newClip, bool loop, float duration)
    {
        _isFading = true; // Set the flag to indicate a fade is in progress

        float timer = 0f;
        float startVolume = _musicAudioSource.volume; // Capture current volume before fade out

        // If no music is currently playing, or no clip is set, just fade in the new music.
        if (_musicAudioSource.clip == null || !_musicAudioSource.isPlaying)
        {
            _musicAudioSource.clip = newClip;
            _musicAudioSource.loop = loop;
            _musicAudioSource.volume = 0f; // Start from silence
            _musicAudioSource.Play();
            yield return StartCoroutine(FadeInCoroutine(duration)); // Use full duration for just the fade in
            _isFading = false;
            yield break; // Exit the coroutine
        }

        // --- Phase 1: Fade out current music ---
        // Use half the total duration for fading out.
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            _musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2f));
            yield return null; // Wait for the next frame
        }

        // Ensure volume is completely down and stop the old clip
        _musicAudioSource.volume = 0f;
        _musicAudioSource.Stop();

        // --- Phase 2: Switch to the new clip ---
        _musicAudioSource.clip = newClip;
        _musicAudioSource.loop = loop;
        _musicAudioSource.Play(); // Start playing the new clip silently

        // --- Phase 3: Fade in the new music ---
        // Reset timer and use the second half of the total duration for fading in.
        timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            _musicAudioSource.volume = Mathf.Lerp(0f, _defaultVolume, timer / (duration / 2f));
            yield return null; // Wait for the next frame
        }

        // Ensure volume reaches the target default volume
        _musicAudioSource.volume = _defaultVolume;
        _isFading = false; // Reset the fade flag
        Debug.Log($"[MusicManager] Crossfade completed. Now playing: {newClip.name}");
    }


    /// <summary>
    /// Coroutine to smoothly fade out the current music over a given duration.
    /// </summary>
    /// <param name="duration">The duration in seconds for the fade out.</param>
    private IEnumerator FadeOutCoroutine(float duration)
    {
        _isFading = true;
        float startVolume = _musicAudioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Lerp the volume from its current level down to 0.
            _musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        _musicAudioSource.volume = 0f; // Ensure volume is exactly 0 at the end
        _musicAudioSource.Stop();      // Stop the playback
        _isFading = false;
        Debug.Log("[MusicManager] Music faded out and stopped.");
    }

    /// <summary>
    /// Coroutine to smoothly fade in the current music (assumes a clip is already set and playing or paused)
    /// from silence up to the default volume over a given duration.
    /// </summary>
    /// <param name="duration">The duration in seconds for the fade in.</param>
    private IEnumerator FadeInCoroutine(float duration)
    {
        _isFading = true;
        // Ensure starting from silence. If it was already playing, this might cause a slight dip.
        // It's usually best to call Play() before FadeIn, so volume is implicitly 0 or near 0.
        _musicAudioSource.volume = 0f;
        float targetVolume = _defaultVolume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Lerp the volume from 0 up to the default target volume.
            _musicAudioSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }

        _musicAudioSource.volume = targetVolume; // Ensure it reaches the target volume
        _isFading = false;
        Debug.Log("[MusicManager] Music faded in.");
    }
}

/*
/// --- Example Usage in another C# script (e.g., a GameState or LevelManager script) ---

// To use these examples, create a new C# script (e.g., 'GameMusicController')
// and attach it to any GameObject in your scene.
// Ensure you have a 'MusicManager' GameObject in your scene with the MusicManager script attached,
// and some AudioClips assigned to its 'Music Tracks' array.

public class GameMusicController : MonoBehaviour
{
    // You can also reference specific AudioClips in your controller if they are not
    // part of the MusicManager's default playlist, or if you want to swap specific ones.
    [SerializeField] private AudioClip _levelOneMusic; // Example for a specific, custom clip
    [SerializeField] private AudioClip _gameOverMusic;
    [SerializeField] private AudioClip _menuMusic;

    void Start()
    {
        Debug.Log("GameMusicController started. Press N, R, S, X, Up/Down Arrow for music control demos.");

        // --- Playing music from the MusicManager's internal tracks array ---
        // Play the first track in the MusicManager's 'Music Tracks' array with crossfade.
        // This is often good for starting initial level music.
        if (MusicManager.Instance.GetVolume() > 0.05f) { // Only play if sound is not muted
             MusicManager.Instance.PlayTrackByIndex(0, loop: true); 
        } else {
             Debug.Log("Music is muted, not playing initial track.");
        }


        // --- Playing a custom AudioClip not necessarily in the MusicManager's array ---
        // To play menu music at the start of the game, for example:
        // MusicManager.Instance.CrossfadeMusic(_menuMusic, loop: true);

        // If you need an immediate, non-fading switch (e.g., for short jingles or very abrupt changes):
        // MusicManager.Instance.PlayMusic(_levelOneMusic, loop: true);
    }

    void Update()
    {
        // --- Example: Adjust volume with keyboard input ---
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MusicManager.Instance.SetVolume(MusicManager.Instance.GetVolume() + 0.1f);
            Debug.Log($"Current music volume: {MusicManager.Instance.GetVolume():F1}");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MusicManager.Instance.SetVolume(MusicManager.Instance.GetVolume() - 0.1f);
            Debug.Log($"Current music volume: {MusicManager.Instance.GetVolume():F1}");
        }

        // --- Example: Play the next track in the playlist ---
        if (Input.GetKeyDown(KeyCode.N))
        {
            MusicManager.Instance.PlayNextTrack(loop: true);
        }
        
        // --- Example: Play a random track from the playlist ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            MusicManager.Instance.PlayRandomTrack(loop: true);
        }

        // --- Example: Simulate game over music on a condition ---
        if (Input.GetKeyDown(KeyCode.S)) // 'S' for Stop/GameOver
        {
            // You can use a specific clip directly (e.g., _gameOverMusic)
            // or one from the MusicManager's internal array via index.
            MusicManager.Instance.CrossfadeMusic(_gameOverMusic, loop: false); 
            // Alternatively: MusicManager.Instance.PlayTrackByIndex(1, loop: false); // If game over music is at index 1
            Debug.Log("Game Over! Playing game over music.");
        }

        // --- Example: Stop all music ---
        if (Input.GetKeyDown(KeyCode.X)) // 'X' for X-terminate music
        {
            MusicManager.Instance.StopMusic();
            Debug.Log("Music explicitly stopped.");
        }

        // --- Example: Pause and Resume with P and O keys ---
        if (Input.GetKeyDown(KeyCode.P)) // 'P' for Pause
        {
            MusicManager.Instance.PauseMusic();
        }
        if (Input.GetKeyDown(KeyCode.O)) // 'O' for Resume
        {
            MusicManager.Instance.ResumeMusic();
        }
    }

    // You can also call MusicManager methods from other events, like button clicks,
    // scene transitions, or game state changes.
    public void OnLevelEnd()
    {
        // Example: Fade out music when a level ends
        MusicManager.Instance.FadeOutMusic(2.0f);
    }

    public void OnPlayerDeath()
    {
        // Example: Play a specific short jingle immediately
        // Consider having a separate AudioSource for sound effects if you want background music to continue
        // during short sound effects, or use a non-fading PlayMusic for quick changes.
        // MusicManager.Instance.PlayMusic(someDeathJingleClip, loop: false); 
    }
}
*/
```