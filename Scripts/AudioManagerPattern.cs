// Unity Design Pattern Example: AudioManagerPattern
// This script demonstrates the AudioManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AudioManagerPattern' in Unity is a common and highly practical design pattern, often implemented as a Singleton. It centralizes all audio playback and management, decoupling the "what" and "when" of playing a sound from the "how" it's played. This makes your audio system robust, performant, and easy to maintain.

This example provides a complete, drop-in solution for a Unity AudioManager.

---

### **AudioManagerPattern: C# Unity Example**

**1. Create the `AudioManager.cs` Script**

Create a new C# script named `AudioManager.cs` in your Unity project and paste the following code into it:

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary and List
using System.Collections; // Not strictly needed for this core logic, but often useful.

// Make the Sound class serializable so it can be edited in the Inspector
[System.Serializable]
public class Sound
{
    public string name; // Identifier for the sound (e.g., "PlayerJump", "BackgroundMusic1")
    public AudioClip clip; // The actual audio file

    [Range(0f, 1f)] public float volume = 1f; // Default volume for this sound (0 to 1)
    [Range(0.1f, 3f)] public float pitch = 1f; // Default pitch for this sound (0.1 to 3)
    public bool loop = false; // Whether this sound should loop by default (primarily for SFX)

    // Optional: Add a reference to the AudioSource playing this sound if needed for advanced control.
    // [HideInInspector] public AudioSource source; 
}

/// <summary>
/// AudioManagerPattern: A Singleton responsible for playing and managing all audio in the game.
/// This pattern centralizes audio control, making it easy to play sound effects (SFX) and
/// background music (BGM) from anywhere in your game without direct AudioSource references.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // The static instance of the AudioManager, ensuring there's only one throughout the game.
    // 'public static' allows global access. 'get; private set;' ensures only the AudioManager itself can set it.
    public static AudioManager Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Audio Sources")]
    [Tooltip("The AudioSource dedicated to playing background music (BGM).")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("The initial number of AudioSources to create for sound effects (SFX) pooling.")]
    [SerializeField] private int sfxSourcePoolSize = 5;

    [Header("Sound Definitions")]
    [Tooltip("Define all your sounds here. Assign a unique name and an AudioClip.")]
    [SerializeField] private Sound[] sounds;

    // --- Internal Data Structures ---
    // Dictionary for quick lookup of AudioClips by their names.
    private Dictionary<string, AudioClip> audioClipDictionary;

    // A pool of AudioSources used for playing SFX. This prevents constant
    // creation/destruction of AudioSources, improving performance for frequent SFX.
    private List<AudioSource> sfxSourcesPool;
    private int currentSFXSourceIndex = 0; // Index to cycle through the SFX pool for next available source.

    // --- Awake Method: Singleton Setup and Initialization ---
    private void Awake()
    {
        // Implement the Singleton pattern:
        // 1. If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // 2. Otherwise, set this as the singleton instance.
        Instance = this;
        // 3. Keep the AudioManager alive across scene loads so music doesn't stop and SFX pool persists.
        DontDestroyOnLoad(gameObject);

        InitializeAudioSystem();
    }

    /// <summary>
    /// Initializes the audio system: Populates the audio clip dictionary and sets up the SFX source pool.
    /// </summary>
    private void InitializeAudioSystem()
    {
        // 1. Populate the audioClipDictionary for quick lookups.
        audioClipDictionary = new Dictionary<string, AudioClip>();
        foreach (Sound sound in sounds)
        {
            if (audioClipDictionary.ContainsKey(sound.name))
            {
                Debug.LogWarning($"AudioManager: Duplicate sound name '{sound.name}' found. Please ensure all sound names are unique.");
                continue; // Skip adding duplicate names
            }
            audioClipDictionary.Add(sound.name, sound.clip);
        }

        // 2. Initialize the SFX AudioSource pool.
        sfxSourcesPool = new List<AudioSource>();
        for (int i = 0; i < sfxSourcePoolSize; i++)
        {
            CreateNewSFXSource();
        }

        // 3. Configure the music source (ensure it's set to loop and play on awake can be controlled).
        if (musicSource == null)
        {
            Debug.LogError("AudioManager: Music AudioSource is not assigned! Attempting to create one. Please assign it in the Inspector for better control.");
            // Try to create one if not assigned for robustness
            GameObject musicGO = new GameObject("Music_AudioSource_AutoCreated");
            musicGO.transform.SetParent(this.transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            // Can assign a specific mixer group here if needed: musicSource.outputAudioMixerGroup = myMusicMixerGroup;
        }
        musicSource.loop = true; // Music generally loops by default
        musicSource.playOnAwake = false; // We control when music starts
    }

    /// <summary>
    /// Creates a new AudioSource for SFX and adds it to the pool.
    /// These are created as child GameObjects to keep the hierarchy clean.
    /// </summary>
    private void CreateNewSFXSource()
    {
        GameObject sfxGO = new GameObject($"SFX_Source_{sfxSourcesPool.Count}");
        sfxGO.transform.SetParent(this.transform); // Keep it organized under the AudioManager
        AudioSource sfxSource = sfxGO.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // Default to 2D sound for SFX (0 = 2D, 1 = 3D)
        sfxSourcesPool.Add(sfxSource);
    }

    /// <summary>
    /// Retrieves the next available AudioSource from the pool for SFX playback.
    /// It cycles through existing sources. If all sources are currently playing,
    /// it will dynamically create a new one, effectively expanding the pool.
    /// </summary>
    /// <returns>An available AudioSource from the pool.</returns>
    private AudioSource GetNextAvailableSFXSource()
    {
        // Try to find a source that isn't currently playing
        for (int i = 0; i < sfxSourcesPool.Count; i++)
        {
            int index = (currentSFXSourceIndex + i) % sfxSourcesPool.Count;
            if (!sfxSourcesPool[index].isPlaying)
            {
                currentSFXSourceIndex = (index + 1) % sfxSourcesPool.Count; // Move to the next index for the next request
                return sfxSourcesPool[index];
            }
        }

        // If all existing sources are playing, create a new one and add it to the pool.
        // This handles bursts of SFX without dropping sounds, but warns the developer.
        Debug.LogWarning("AudioManager: SFX pool exhausted! Creating a new AudioSource. Consider increasing sfxSourcePoolSize.");
        CreateNewSFXSource();
        // Return the newly created source
        currentSFXSourceIndex = (sfxSourcesPool.Count - 1 + 1) % sfxSourcesPool.Count; // Update index to point after new source
        return sfxSourcesPool[sfxSourcesPool.Count - 1];
    }

    // --- Public API for Audio Playback ---

    /// <summary>
    /// Plays a sound effect (SFX) once.
    /// </summary>
    /// <param name="soundName">The unique name of the sound to play (as defined in the Inspector).</param>
    /// <param name="volume">Optional: Override the default volume for this playback (0-1). -1 uses default.</param>
    /// <param name="pitch">Optional: Override the default pitch for this playback (0.1-3). -1 uses default.</param>
    /// <param name="loop">Optional: Make this SFX loop. Remember to stop it manually if looping using StopSFX.</param>
    /// <returns>The AudioSource that is playing the SFX, or null if the sound was not found or an error occurred.</returns>
    public AudioSource PlaySFX(string soundName, float volume = -1f, float pitch = -1f, bool loop = false)
    {
        AudioClip clip = GetAudioClip(soundName);
        if (clip == null) return null; // Sound not found, already logged a warning

        AudioSource sfxSource = GetNextAvailableSFXSource();
        if (sfxSource == null) return null; // Should ideally not happen with pooling logic

        // Find the default settings for this sound from the 'sounds' array
        Sound soundConfig = System.Array.Find(sounds, s => s.name == soundName);

        // Apply clip and determine volume/pitch (use override if provided, else use sound config, else default to 1)
        sfxSource.clip = clip;
        sfxSource.volume = (volume >= 0) ? volume : (soundConfig != null ? soundConfig.volume : 1f);
        sfxSource.pitch = (pitch >= 0) ? pitch : (soundConfig != null ? soundConfig.pitch : 1f);
        sfxSource.loop = loop || (soundConfig != null ? soundConfig.loop : false); // Loop if explicitly requested or if sound config says so
        sfxSource.Play();

        return sfxSource; // Return the source so looping SFX can be stopped later
    }

    /// <summary>
    /// Stops a specific SFX that is currently looping. You need the AudioSource reference
    /// returned by `PlaySFX` when it was started.
    /// </summary>
    /// <param name="sfxSource">The AudioSource that is playing the looping SFX.</param>
    public void StopSFX(AudioSource sfxSource)
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
            sfxSource.clip = null; // Clear the clip to prepare for reuse
            sfxSource.loop = false; // Reset loop state
        }
    }

    /// <summary>
    /// Plays background music (BGM). If music is already playing, it stops it first and then plays the new track.
    /// </summary>
    /// <param name="soundName">The unique name of the music track to play.</param>
    /// <param name="volume">Optional: Override the default volume for this playback (0-1). -1 uses default.</param>
    public void PlayMusic(string soundName, float volume = -1f)
    {
        AudioClip clip = GetAudioClip(soundName);
        if (clip == null) return;

        // Stop current music if any, then play the new one
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        // Find the default settings for this sound
        Sound soundConfig = System.Array.Find(sounds, s => s.name == soundName);

        // Apply clip and determine volume (use override if provided, else use sound config, else default to 1)
        musicSource.clip = clip;
        musicSource.volume = (volume >= 0) ? volume : (soundConfig != null ? soundConfig.volume : 1f);
        musicSource.pitch = (soundConfig != null ? soundConfig.pitch : 1f); // Music pitch usually isn't changed per-play
        musicSource.Play();
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null; // Clear the clip
        }
    }

    /// <summary>
    /// Pauses the currently playing background music.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the paused background music.
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// Sets the global volume for background music.
    /// </summary>
    /// <param name="volume">The desired volume (0-1).</param>
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Sets the volume for all SFX sources in the pool. This affects both
    /// currently playing and future SFX played through these sources.
    /// Note: This is a simpler "global" SFX volume control. For individual SFX
    /// volume control, rely on the 'volume' parameter in `PlaySFX`.
    /// </summary>
    /// <param name="volume">The desired volume (0-1).</param>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        foreach (AudioSource source in sfxSourcesPool)
        {
            source.volume = volume;
        }
    }

    /// <summary>
    /// Helper method to retrieve an AudioClip by its name from the dictionary.
    /// </summary>
    /// <param name="soundName">The name of the sound.</param>
    /// <returns>The AudioClip if found, otherwise null.</returns>
    private AudioClip GetAudioClip(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            Debug.LogWarning("AudioManager: Sound name cannot be empty or null.");
            return null;
        }

        if (audioClipDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            return clip;
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found in dictionary. Make sure it's defined in the Inspector.");
            return null;
        }
    }
}
```

---

**2. Setup in Unity Editor**

1.  **Create AudioManager GameObject:** In your Unity scene, create an Empty GameObject and name it `AudioManager`.
2.  **Attach Script:** Drag and drop the `AudioManager.cs` script onto the `AudioManager` GameObject.
3.  **Assign Music Source:**
    *   Right-click on the `AudioManager` GameObject in the Hierarchy -> `Create Empty` and name this new child `MusicSource`.
    *   Add an `AudioSource` component to the `MusicSource` GameObject (click `Add Component` in its Inspector and search for `AudioSource`).
    *   Drag the `MusicSource` GameObject from the Hierarchy into the `Music Source` slot on the `AudioManager` script component in the Inspector.
    *   *(Optional: You can configure the `AudioMixerGroup` for this `MusicSource` if you use Unity's Audio Mixer).*
4.  **Configure SFX Pool Size:** Adjust the `SFX Source Pool Size` property on the `AudioManager` component (e.g., 5-10 is a good starting point). This defines how many `AudioSource` objects will be pre-created for sound effects.
5.  **Define Sounds:**
    *   Expand the `Sounds` array property on the `AudioManager` component.
    *   Set the `Size` to the number of unique sound effects and music tracks you have.
    *   For each element:
        *   **Name:** Give it a unique string name (e.g., "PlayerJump", "Explosion", "BackgroundTheme"). This is the name you'll use in code to refer to the sound.
        *   **Clip:** Drag your `AudioClip` asset (MP3, WAV, etc.) from your Project window into this slot.
        *   **Volume / Pitch / Loop:** Adjust these default properties for the specific sound. `Loop` is mainly useful if you have SFX that should loop (like an engine sound or ambient drone).

---

### **3. How to Use the AudioManager (Example Usage)**

Now, from any other script in your game, you can easily play sounds:

```csharp
/*
 * --- HOW TO USE THE AUDIO MANAGER PATTERN IN UNITY ---
 *
 * 1. Create an Empty GameObject in your scene and name it "AudioManager".
 * 2. Attach this `AudioManager.cs` script to it.
 * 3. In the Inspector for the AudioManager GameObject:
 *    a. Create a child GameObject named "MusicSource" and add an AudioSource component to it.
 *       Drag this "MusicSource" into the `Music Source` slot on the AudioManager script.
 *       (Alternatively, the script will create one if none is assigned, but assigning manually
 *       allows for specific AudioMixerGroup setup for music).
 *    b. Adjust `SFX Source Pool Size` as needed (e.g., 5-10 is a good starting point for most games).
 *    c. In the `Sounds` array, add new elements for each sound effect and music track you want to use:
 *       - Give it a unique `Name` (e.g., "PlayerJump", "Explosion", "BackgroundMusic1").
 *       - Drag your `AudioClip` (from your project assets) into the `Clip` slot.
 *       - Adjust default `Volume` and `Pitch` for that sound if desired.
 *       - Set `Loop` to true for SFX that should loop (e.g., engine hum, ambient effects).
 *
 * 4. To play sounds from any other script:
 */

// Example: From a PlayerMovement script
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private string jumpSoundName = "PlayerJump"; // Assign in Inspector (e.g., "PlayerJump")
    [SerializeField] private string landSoundName = "PlayerLand"; // Assign in Inspector (e.g., "PlayerLand")

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Play SFX using the AudioManager instance
            // It's safe to call this from anywhere after the AudioManager has initialized (i.e., in Start or later).
            AudioManager.Instance.PlaySFX(jumpSoundName, 0.7f); // Play at 70% volume
            Debug.Log($"Playing sound: {jumpSoundName}");
        }

        if (Input.GetMouseButtonDown(0)) // Example for a left mouse click
        {
            AudioManager.Instance.PlaySFX("GunShot", 0.8f, Random.Range(0.9f, 1.1f)); // Play with a random pitch variation
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            AudioManager.Instance.PlaySFX(landSoundName);
        }
    }
}

// Example: From a GameStateController script for background music and looping SFX
using UnityEngine;

public class GameStateController : MonoBehaviour
{
    [SerializeField] private string menuMusicName = "MenuTheme"; // Assign in Inspector
    [SerializeField] private string gameMusicName = "GameTheme";   // Assign in Inspector
    [SerializeField] private string ambientWindSound = "WindLoop"; // Assign in Inspector for a looping SFX

    private AudioSource currentAmbientWindSource; // To keep track of the looping SFX for stopping it

    void Start()
    {
        // Play music when the game starts (e.g., main menu)
        AudioManager.Instance.PlayMusic(menuMusicName, 0.5f); // Play at 50% volume
    }

    // Call this method when the game state changes (e.g., from menu to game)
    public void StartGame()
    {
        Debug.Log("Starting game, changing music...");
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlayMusic(gameMusicName);

        // Start a looping ambient sound
        if (currentAmbientWindSource == null)
        {
            currentAmbientWindSource = AudioManager.Instance.PlaySFX(ambientWindSound, 0.6f, 1f, true);
        }
    }

    // Call this method when the game ends or returns to menu
    public void EndGame()
    {
        Debug.Log("Ending game, stopping music and ambient sounds...");
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlayMusic(menuMusicName); // Return to menu music

        // Stop the looping ambient sound
        if (currentAmbientWindSource != null)
        {
            AudioManager.Instance.StopSFX(currentAmbientWindSource);
            currentAmbientWindSource = null;
        }
    }

    // Example for volume control (e.g., from UI sliders)
    public void OnSFXVolumeChanged(float newVolume)
    {
        AudioManager.Instance.SetSFXVolume(newVolume);
        Debug.Log($"SFX Volume set to: {newVolume}");
    }

    public void OnMusicVolumeChanged(float newVolume)
    {
        AudioManager.Instance.SetMusicVolume(newVolume);
        Debug.Log($"Music Volume set to: {newVolume}");
    }

    // Important: If an object plays a looping SFX, make sure to stop it if the object is destroyed
    void OnDestroy()
    {
        if (currentAmbientWindSource != null && currentAmbientWindSource.isPlaying)
        {
            AudioManager.Instance.StopSFX(currentAmbientWindSource);
        }
    }
}
```

---

### **Design Pattern Explained (AudioManagerPattern / Service Locator variant of Singleton):**

*   **Singleton:**
    *   **Purpose:** Ensures that there is exactly one instance of the `AudioManager` throughout the entire game. This is crucial because you generally want a single, authoritative point of control for all audio playback.
    *   **Implementation:** The `public static AudioManager Instance { get; private set; }` property and the `Awake()` method's logic (checking for existing instances and calling `DontDestroyOnLoad`) guarantee this. Any script can easily access the manager via `AudioManager.Instance`.

*   **Centralized Control (Service Locator):**
    *   **Purpose:** All requests to play, stop, or manage audio go through this single manager. This decouples audio playback logic from specific game objects. For example, a player character doesn't need to know *how* to play a jump sound; it just tells the `AudioManager` to play "PlayerJump" via `AudioManager.Instance.PlaySFX("PlayerJump")`.
    *   **Benefits:** This separation of concerns makes your code cleaner, more modular, and easier to maintain.

*   **AudioSource Pooling (for SFX):**
    *   **Purpose:** Creating and destroying `AudioSource` components dynamically is computationally expensive, especially for games with many rapid sound effects (e.g., gunshots, footsteps). To optimize this, the `AudioManager` pre-creates a pool of `AudioSource` objects (`sfxSourcesPool`).
    *   **Implementation:** When `PlaySFX` is called, it reuses an available `AudioSource` from this pool instead of creating a new one. If all sources in the pool are currently playing, it dynamically creates a new one (with a warning) to ensure no sounds are dropped, but it encourages you to adjust the initial `sfxSourcePoolSize`.
    *   **Benefits:** Significantly improves performance and prevents audio hitches or gaps.

*   **Configurable Sounds (`Sound` Class):**
    *   **Purpose:** Allows game designers and developers to define all audio clips with their names and default properties (volume, pitch, loop) directly in the Unity Inspector.
    *   **Implementation:** The `[System.Serializable] public class Sound` lets you expose a list of sound configurations in the Inspector. The `audioClipDictionary` then provides a fast lookup for these clips at runtime.
    *   **Benefits:** Avoids hardcoding audio clips in scripts, makes sound management flexible, and allows for easy iteration on sound properties without touching code.

*   **Dedicated Music Source:**
    *   **Purpose:** Background music often has different characteristics (always looping, typically a single track playing at a time) compared to SFX. A dedicated `AudioSource` (`musicSource`) simplifies its management.
    *   **Implementation:** The `musicSource` is explicitly configured for looping and offers specific `PlayMusic`, `StopMusic`, `PauseMusic`, and `ResumeMusic` methods.

---

This `AudioManager` implementation is practical and scalable for most Unity projects, providing a solid foundation for managing your game's soundscape efficiently.