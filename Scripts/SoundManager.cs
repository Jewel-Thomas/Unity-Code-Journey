// Unity Design Pattern Example: SoundManager
// This script demonstrates the SoundManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script demonstrates the **SoundManager** design pattern. It provides a robust, easy-to-use system for managing background music (BGM) and sound effects (SFX) in your Unity projects.

**Key Features:**
*   **Singleton Pattern:** Ensures a single, globally accessible instance of the SoundManager.
*   **BGM Management:** Plays, stops, pauses, resumes, and cross-fades background music.
*   **SFX Management:** Plays various sound effects using an efficient object pooling system to prevent performance overhead.
*   **Volume Control:** Global volume control for BGM and SFX, plus per-clip volume and pitch settings.
*   **Editor Integration:** `[SerializeField]` attributes allow easy configuration of sounds directly in the Unity Inspector.
*   **Clear Structure:** Separates BGM and SFX for logical organization.
*   **Detailed Comments:** Explains the design choices and implementation details for educational purposes.

---

### SoundManager.cs

To use this script:

1.  **Create an Empty GameObject** in your *first* scene (e.g., "SoundManager").
2.  **Attach this `SoundManager.cs` script** to that GameObject.
3.  **Populate the `BGM Sounds` and `SFX Sounds` arrays** in the Inspector with your audio clips.
    *   For each `Sound` entry, provide a unique `Name` (this is how you'll reference it in code), drag in an `AudioClip`, and adjust its `Volume` and `Pitch` as default.
4.  Optionally, adjust the `SFX Pool Size`, `Default BGM Volume`, and `Default SFX Volume`.
5.  Call its methods from any other script using `SoundManager.Instance.MethodName()`.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for Dictionaries and Lists

/// <summary>
/// A Serializable class to hold properties for individual sound clips.
/// This allows designers to configure sounds directly in the Unity Inspector.
/// </summary>
[System.Serializable]
public class Sound
{
    public string name; // The unique identifier for this sound (used in code to play it)
    public AudioClip clip; // The actual audio asset

    [Range(0f, 1f)]
    public float volume = 1f; // Default volume for this specific clip (multiplied by global volume)
    [Range(0.1f, 3f)]
    public float pitch = 1f; // Default pitch for this specific clip
}

/// <summary>
/// The SoundManager class implements the Singleton pattern to provide a global
/// access point for playing background music (BGM) and sound effects (SFX).
/// It manages AudioSource components, volume controls, and allows for features
/// like BGM cross-fading and SFX pooling for efficiency.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // A static reference to the SoundManager instance, making it globally accessible.
    // 'public static' allows access from anywhere (e.g., SoundManager.Instance).
    // 'get; private set;' ensures it can only be set internally by the SoundManager itself.
    public static SoundManager Instance { get; private set; }

    // --- Editor-Configurable Sound Data ---
    [Header("Sound Settings")]
    [Tooltip("Collection of all background music clips available in the game.")]
    [SerializeField] private Sound[] _bgmSounds;
    [Tooltip("Collection of all sound effect clips available in the game.")]
    [SerializeField] private Sound[] _sfxSounds;

    [Tooltip("The number of AudioSources to pre-create for SFX pooling. " +
             "More sources allow more simultaneous SFX, but consume more resources.")]
    [SerializeField] private int _sfxPoolSize = 10;

    [Range(0f, 1f)]
    [SerializeField] private float _defaultBGMVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _defaultSFXVolume = 1f;

    // --- Private AudioSource References ---
    // Two AudioSources for BGM to enable seamless cross-fading. When one is playing,
    // the other is ready to be faded in with a new track.
    private AudioSource _bgmSource1;
    private AudioSource _bgmSource2;
    // Reference to the currently active BGM AudioSource.
    private AudioSource _currentBGMSource;
    // Reference to the BGM AudioSource that will be faded in next.
    private AudioSource _nextBGMSource;

    // A list of AudioSources for SFX, acting as a simple object pool.
    // This avoids creating and destroying AudioSource GameObjects repeatedly, improving performance.
    private List<AudioSource> _sfxSources;
    // Index to keep track of the next available SFX AudioSource in the pool.
    private int _currentSfxSourceIndex = 0;

    // --- Internal Dictionaries for Quick Lookup ---
    // Dictionaries store AudioClips and their default properties (volume, pitch)
    // for quick retrieval by name, avoiding repeated array searches which are slower.
    private Dictionary<string, AudioClip> _bgmClips;
    private Dictionary<string, float> _bgmVolumes;

    private Dictionary<string, AudioClip> _sfxClips;
    private Dictionary<string, float> _sfxVolumes;
    private Dictionary<string, float> _sfxPitches;

    // --- Current Volume States (Runtime adjustable) ---
    // These store the global volume multipliers for BGM and SFX.
    private float _currentBGMGlobalVolume;
    private float _currentSFXGlobalVolume;

    // --- Core Initialization ---
    private void Awake()
    {
        // --- Singleton Enforcement ---
        // Ensures that there is only one instance of SoundManager throughout the game's lifecycle.
        if (Instance == null)
        {
            Instance = this;
            // Prevents the SoundManager GameObject from being destroyed when loading new scenes.
            // This allows music and sound settings to persist across scenes.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another instance of SoundManager already exists, destroy this duplicate.
            // This handles cases where a SoundManager might be placed in multiple scenes.
            Destroy(gameObject);
            return; // Stop execution to prevent further initialization issues.
        }

        // Initialize global volumes to their default settings as specified in the Inspector.
        _currentBGMGlobalVolume = _defaultBGMVolume;
        _currentSFXGlobalVolume = _defaultSFXVolume;

        // --- Initialize BGM AudioSources ---
        // Create and configure the first BGM AudioSource dynamically.
        _bgmSource1 = gameObject.AddComponent<AudioSource>();
        _bgmSource1.playOnAwake = false; // Don't play automatically when scene loads
        _bgmSource1.loop = true;          // BGM typically loops indefinitely
        _bgmSource1.volume = _currentBGMGlobalVolume; // Start with global default BGM volume
        _bgmSource1.outputAudioMixerGroup = null; // Can be assigned to an AudioMixerGroup for advanced control (e.g., master/music groups)

        // Create and configure the second BGM AudioSource (for cross-fading).
        _bgmSource2 = gameObject.AddComponent<AudioSource>();
        _bgmSource2.playOnAwake = false;
        _bgmSource2.loop = true;
        _bgmSource2.volume = 0f; // Starts muted, will be faded in during a cross-fade
        _bgmSource2.outputAudioMixerGroup = null;

        // Set the first source as the current active one initially.
        // The other will be the 'next' source ready for a fade-in.
        _currentBGMSource = _bgmSource1;
        _nextBGMSource = _bgmSource2;

        // --- Initialize SFX AudioSource Pool ---
        _sfxSources = new List<AudioSource>();
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            // Create an empty GameObject for each SFX AudioSource and parent it to the SoundManager.
            // This keeps the Hierarchy clean.
            GameObject sfxGO = new GameObject($"SFX_Source_{i}");
            sfxGO.transform.SetParent(transform);
            AudioSource sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false; // SFX typically do not loop
            sfxSource.volume = _currentSFXGlobalVolume; // Start with global default SFX volume
            sfxSource.outputAudioMixerGroup = null;
            _sfxSources.Add(sfxSource);
        }

        // --- Populate Dictionaries for Fast Lookup ---
        // Transfer data from Inspector-configured arrays to Dictionaries for O(1) average time complexity lookup.
        // This is much faster than iterating through arrays every time a sound needs to be played.

        _bgmClips = new Dictionary<string, AudioClip>();
        _bgmVolumes = new Dictionary<string, float>();
        foreach (Sound s in _bgmSounds)
        {
            if (s.clip != null && !string.IsNullOrEmpty(s.name) && !_bgmClips.ContainsKey(s.name))
            {
                _bgmClips.Add(s.name, s.clip);
                _bgmVolumes.Add(s.name, s.volume);
            }
            else
            {
                Debug.LogWarning($"SoundManager: Duplicate or invalid BGM entry for name '{s.name}' or missing clip. Skipping.");
            }
        }

        _sfxClips = new Dictionary<string, AudioClip>();
        _sfxVolumes = new Dictionary<string, float>();
        _sfxPitches = new Dictionary<string, float>();
        foreach (Sound s in _sfxSounds)
        {
            if (s.clip != null && !string.IsNullOrEmpty(s.name) && !_sfxClips.ContainsKey(s.name))
            {
                _sfxClips.Add(s.name, s.clip);
                _sfxVolumes.Add(s.name, s.volume);
                _sfxPitches.Add(s.name, s.pitch);
            }
            else
            {
                Debug.LogWarning($"SoundManager: Duplicate or invalid SFX entry for name '{s.name}' or missing clip. Skipping.");
            }
        }
    }

    // --- Public BGM Control Methods ---

    /// <summary>
    /// Plays background music by its registered name. Can cross-fade from the currently playing BGM.
    /// </summary>
    /// <param name="name">The unique name of the BGM clip to play (as set in the Inspector).</param>
    /// <param name="crossFade">If true, gradually transitions from current BGM to new BGM. If false, switches immediately.</param>
    /// <param name="fadeTime">The duration of the cross-fade in seconds (only relevant if crossFade is true).</param>
    public void PlayBGM(string name, bool crossFade = false, float fadeTime = 1f)
    {
        // Try to retrieve the AudioClip for the given name.
        if (!_bgmClips.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"SoundManager: BGM clip '{name}' not found! Check your SoundManager configuration.");
            return;
        }

        // If the requested BGM is already playing on the current source, do nothing.
        if (_currentBGMSource.clip == clip && _currentBGMSource.isPlaying)
        {
            return;
        }

        // Get the default volume for this specific clip, applying a fallback of 1f if not defined.
        float targetClipVolume = _bgmVolumes.TryGetValue(name, out float clipVolume) ? clipVolume : 1f;
        // Apply the global BGM volume multiplier.
        float finalTargetVolume = targetClipVolume * _currentBGMGlobalVolume;

        if (crossFade)
        {
            // Stop any ongoing fade coroutines to prevent conflicts or abrupt changes.
            StopAllCoroutines(); 
            // Start the cross-fade coroutine.
            StartCoroutine(CrossFadeBGMCoroutine(_currentBGMSource, _nextBGMSource, clip, finalTargetVolume, fadeTime));
        }
        else
        {
            // No cross-fade: immediately stop the current BGM, set the new clip, and play it.
            _currentBGMSource.Stop();
            _currentBGMSource.clip = clip;
            _currentBGMSource.volume = finalTargetVolume;
            _currentBGMSource.Play();
            // Ensure the next BGM source is also stopped and muted in case it was active.
            _nextBGMSource.Stop();
            _nextBGMSource.volume = 0f;
        }
    }

    /// <summary>
    /// Stops the currently playing background music immediately.
    /// </summary>
    public void StopBGM()
    {
        // Stop any pending cross-fade coroutines.
        StopAllCoroutines(); 
        if (_currentBGMSource.isPlaying)
        {
            _currentBGMSource.Stop();
        }
        // Also stop the next BGM source in case a fade was interrupted and it was playing.
        if (_nextBGMSource.isPlaying)
        {
            _nextBGMSource.Stop();
        }
    }

    /// <summary>
    /// Pauses the currently playing background music.
    /// </summary>
    public void PauseBGM()
    {
        if (_currentBGMSource.isPlaying)
        {
            _currentBGMSource.Pause();
        }
        // If a cross-fade is in progress, pause the incoming BGM source as well.
        if (_nextBGMSource.isPlaying)
        {
            _nextBGMSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the currently paused background music.
    /// </summary>
    public void ResumeBGM()
    {
        // Only resume if the source is paused (i.e., not playing but has a positive time).
        if (!_currentBGMSource.isPlaying && _currentBGMSource.time > 0)
        {
            _currentBGMSource.UnPause();
        }
        if (!_nextBGMSource.isPlaying && _nextBGMSource.time > 0)
        {
            _nextBGMSource.UnPause();
        }
    }

    /// <summary>
    /// Sets the global volume for all background music.
    /// This will immediately apply to the currently playing BGM and any future BGM.
    /// </summary>
    /// <param name="volume">The new global BGM volume (0.0 to 1.0).</param>
    public void SetBGMVolume(float volume)
    {
        _currentBGMGlobalVolume = Mathf.Clamp01(volume); // Ensure volume stays within 0-1 range

        // Apply to current BGM source, maintaining its relative clip volume.
        if (_currentBGMSource.clip != null)
        {
            // Get the original clip's volume, default to 1 if not found.
            _bgmVolumes.TryGetValue(_currentBGMSource.clip.name, out float clipVolume);
            _currentBGMSource.volume = (clipVolume > 0 ? clipVolume : 1f) * _currentBGMGlobalVolume;
        }
        // If a cross-fade is in progress, also adjust the volume of the incoming BGM source.
        if (_nextBGMSource.clip != null && _nextBGMSource.isPlaying)
        {
            _bgmVolumes.TryGetValue(_nextBGMSource.clip.name, out float clipVolume);
            _nextBGMSource.volume = (clipVolume > 0 ? clipVolume : 1f) * _currentBGMGlobalVolume;
        }
    }

    /// <summary>
    /// Gets the current global volume for background music.
    /// </summary>
    /// <returns>The current global BGM volume (0.0 to 1.0).</returns>
    public float GetBGMVolume()
    {
        return _currentBGMGlobalVolume;
    }


    // --- Public SFX Control Methods ---

    /// <summary>
    /// Plays a sound effect by name using an available AudioSource from the pool.
    /// </summary>
    /// <param name="name">The unique name of the SFX clip to play (as set in the Inspector).</param>
    /// <returns>The AudioSource that is playing the SFX, or null if the clip was not found/played.</returns>
    public AudioSource PlaySFX(string name)
    {
        // Try to retrieve the AudioClip for the given name.
        if (!_sfxClips.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"SoundManager: SFX clip '{name}' not found! Check your SoundManager configuration.");
            return null;
        }

        // Get an AudioSource from the pool using a round-robin approach.
        // If all are busy, it will stop the oldest playing sound and reuse its source.
        AudioSource sfxSource = _sfxSources[_currentSfxSourceIndex];
        // Increment index, wrapping around to the beginning of the pool if it reaches the end.
        _currentSfxSourceIndex = (_currentSfxSourceIndex + 1) % _sfxPoolSize;

        // Configure the SFX AudioSource with the specific clip's properties and global multipliers.
        sfxSource.Stop(); // Ensure it's stopped before re-use to prevent cutting off new sound
        sfxSource.clip = clip;

        float clipVolume = _sfxVolumes.TryGetValue(name, out float vol) ? vol : 1f;
        sfxSource.volume = clipVolume * _currentSFXGlobalVolume; // Apply global SFX volume

        float clipPitch = _sfxPitches.TryGetValue(name, out float pit) ? pit : 1f;
        sfxSource.pitch = clipPitch;

        sfxSource.Play();
        return sfxSource;
    }

    /// <summary>
    /// Sets the global volume for all sound effects.
    /// This will apply to any *future* SFX played. It will not affect currently playing SFX
    /// because they are fire-and-forget from the pool.
    /// </summary>
    /// <param name="volume">The new global SFX volume (0.0 to 1.0).</param>
    public void SetSFXVolume(float volume)
    {
        _currentSFXGlobalVolume = Mathf.Clamp01(volume); // Ensure volume stays within 0-1 range
        // Note: For a more advanced system that affects *currently playing* SFX,
        // you would need to iterate through _sfxSources and adjust their volumes here.
        // This simple implementation focuses on future SFX for efficiency.
    }

    /// <summary>
    /// Gets the current global volume for sound effects.
    /// </summary>
    /// <returns>The current global SFX volume (0.0 to 1.0).</returns>
    public float GetSFXVolume()
    {
        return _currentSFXGlobalVolume;
    }

    /// <summary>
    /// Stops all currently playing sound effects in the pool.
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource sfxSource in _sfxSources)
        {
            if (sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
        }
    }

    /// <summary>
    /// Stops all currently playing sounds (both BGM and SFX).
    /// </summary>
    public void StopAllSounds()
    {
        StopBGM();
        StopAllSFX();
    }


    // --- Coroutines for Advanced Features ---

    /// <summary>
    /// Coroutine to handle cross-fading between two BGM AudioSources.
    /// Fades out the `fromSource` while fading in the `toSource` over a specified time.
    /// </summary>
    /// <param name="fromSource">The AudioSource currently playing and to be faded out.</param>
    /// <param name="toSource">The AudioSource to be faded in with the new clip.</param>
    /// <param name="newClip">The new AudioClip to play on the `toSource`.</param>
    /// <param name="targetVolume">The final target volume for the `newClip`.</param>
    /// <param name="fadeTime">The duration of the fade in seconds.</param>
    private IEnumerator CrossFadeBGMCoroutine(AudioSource fromSource, AudioSource toSource, AudioClip newClip, float targetVolume, float fadeTime)
    {
        // Handle immediate switch if fadeTime is zero or negative.
        if (fadeTime <= 0)
        {
            fromSource.Stop();
            fromSource.volume = 0f; // Ensure it's silent
            toSource.clip = newClip;
            toSource.volume = targetVolume;
            toSource.Play();
            // Swap references immediately.
            _currentBGMSource = toSource;
            _nextBGMSource = fromSource;
            yield break;
        }

        // Store initial volumes to calculate fade interpolation.
        float startFromVolume = fromSource.volume;
        float startToVolume = toSource.volume; // Should typically be 0, but good for robustness.

        // Prepare the 'toSource' to play the new clip.
        toSource.clip = newClip;
        toSource.Play(); // Start playing immediately, but its volume will be faded in from `startToVolume`.

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime; // Normalized time (0 to 1)

            // Fade out the 'fromSource' linearly.
            fromSource.volume = Mathf.Lerp(startFromVolume, 0f, t);
            // Fade in the 'toSource' to its target volume linearly.
            toSource.volume = Mathf.Lerp(startToVolume, targetVolume, t);

            yield return null; // Wait for the next frame.
        }

        // Ensure final volumes are set precisely after the loop finishes.
        fromSource.Stop(); // Stop the old BGM completely
        fromSource.volume = 0f; // Mute the old source
        toSource.volume = targetVolume; // Ensure new BGM is at full target volume

        // Swap the references so the 'toSource' becomes the new 'currentBGMSource'.
        // The old source (_fromSource) becomes the 'nextBGMSource' ready for the next fade.
        _currentBGMSource = toSource;
        _nextBGMSource = fromSource; 
    }
}

/*
// =====================================================================================================================
// === EXAMPLE USAGE: How to use the SoundManager in other C# scripts ===
// =====================================================================================================================

// To use the SoundManager, ensure you have an instance of it in your scene,
// usually attached to an empty GameObject named "SoundManager" in your first scene.
// You then populate its 'BGM Sounds' and 'SFX Sounds' arrays in the Unity Inspector.

// --- 1. Basic Setup in the Unity Editor (Review if you haven't done it) ---
//   a. Create an Empty GameObject in your initial scene (e.g., "SoundManager").
//   b. Attach this `SoundManager.cs` script to that GameObject.
//   c. In the Inspector for the "SoundManager" GameObject:
//      - Expand "Sound Settings".
//      - Increase the "Size" of "BGM Sounds" and "SFX Sounds" arrays to add elements.
//      - For each element:
//          - Give it a unique `Name` (this is the string you'll use in code, e.g., "MenuTheme", "PlayerJump").
//          - Drag an `AudioClip` from your Project window into the `Clip` field.
//          - Adjust `Volume` and `Pitch` as desired (these are default values for that specific sound).
//      - Adjust `SFX Pool Size` (e.g., 10-20 is a good starting point for games with many concurrent SFX).
//      - Adjust `Default BGM Volume` and `Default SFX Volume` (these are global multipliers).

// --- 2. Example C# Script for Playing Sounds (e.g., attached to a Player, GameController, or UI Button) ---

using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    // Define constants for your sound names to prevent typos and make code cleaner.
    // These names MUST match the 'Name' fields you set in the SoundManager Inspector.
    private const string MENU_BGM = "MenuTheme";
    private const string GAME_BGM = "GameTheme";

    private const string COLLECT_COIN_SFX = "CoinCollect";
    private const string JUMP_SFX = "PlayerJump";
    private const string HIT_SFX = "EnemyHit";
    private const string BUTTON_CLICK_SFX = "ButtonClick";

    void Start()
    {
        // Play the menu music when this script starts (e.g., when the main menu scene loads).
        Debug.Log("Playing Menu BGM...");
        SoundManager.Instance.PlayBGM(MENU_BGM, crossFade: false);
    }

    // Call this method when the player transitions from the menu to the game world.
    public void OnPlayerEnterGameScene()
    {
        Debug.Log("Playing Game BGM (with cross-fade)...");
        // Cross-fade smoothly from the current BGM to the game BGM over 2 seconds.
        SoundManager.Instance.PlayBGM(GAME_BGM, crossFade: true, fadeTime: 2f);
    }

    // Call this method when the player collects an item.
    public void OnPlayerCollectCoin()
    {
        Debug.Log("Playing Coin Collect SFX...");
        SoundManager.Instance.PlaySFX(COLLECT_COIN_SFX);
    }

    // Call this method when the player performs a jump action.
    public void OnPlayerJump()
    {
        Debug.Log("Playing Jump SFX...");
        SoundManager.Instance.PlaySFX(JUMP_SFX);
    }

    // Call this method when the player hits an enemy.
    public void OnPlayerHitEnemy()
    {
        Debug.Log("Playing Hit SFX...");
        SoundManager.Instance.PlaySFX(HIT_SFX);
    }

    // Call this method when a UI button is clicked.
    public void OnUIButtonClick()
    {
        Debug.Log("Playing Button Click SFX...");
        SoundManager.Instance.PlaySFX(BUTTON_CLICK_SFX);
    }

    // Call this to stop all background music and sound effects, e.g., when exiting the game.
    public void StopAllGameAudio()
    {
        Debug.Log("Stopping all audio...");
        SoundManager.Instance.StopAllSounds();
    }

    // Example of adjusting volumes, often hooked up to UI sliders in a settings menu.
    public void AdjustGlobalVolumes(float bgmVolume, float sfxVolume)
    {
        // Ensure volumes are clamped between 0 and 1.
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

        Debug.Log($"Adjusting BGM volume to {bgmVolume} and SFX volume to {sfxVolume}");
        SoundManager.Instance.SetBGMVolume(bgmVolume);
        SoundManager.Instance.SetSFXVolume(sfxVolume);
    }

    // Example of toggling BGM mute (e.g., for a mute button).
    public void OnToggleBGMMute()
    {
        float currentBGMVolume = SoundManager.Instance.GetBGMVolume();
        // If current volume is effectively muted (0), set it back to a default (e.g., 0.5).
        // Otherwise, mute it.
        float newVolume = (currentBGMVolume <= 0.01f) ? 0.5f : 0f; 
        SoundManager.Instance.SetBGMVolume(newVolume);
        Debug.Log($"BGM Muted: {newVolume == 0f}");
    }

    // Example of pausing and resuming BGM (e.g., when the game is paused).
    public void OnGamePause()
    {
        Debug.Log("Pausing BGM...");
        SoundManager.Instance.PauseBGM();
    }

    public void OnGameResume()
    {
        Debug.Log("Resuming BGM...");
        SoundManager.Instance.ResumeBGM();
    }
}
*/
```