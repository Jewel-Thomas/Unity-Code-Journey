// Unity Design Pattern Example: AudioMixerPattern
// This script demonstrates the AudioMixerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The `AudioMixerPattern` in Unity is a robust and flexible way to manage and control all audio aspects of your game. It leverages Unity's `AudioMixer` asset to create distinct audio groups (e.g., Music, SFX, UI, Master) and expose their parameters (like volume, pitch, or effects) to be controlled programmatically.

This pattern centralizes audio management, making it easy to:
*   Adjust volumes of different audio categories independently.
*   Apply global effects to certain sound types.
*   Implement features like pausing/unpausing audio, ducking music for dialogue, or crossfading between tracks.
*   Save and load user audio preferences.

### How the `AudioMixerPattern` Works:

1.  **AudioMixer Asset:** You create an `AudioMixer` asset in Unity. This is the core hub for all your game's audio.
2.  **Audio Mixer Groups:** Within the `AudioMixer`, you define groups (e.g., "Master", "Music", "SFX", "UI"). Each `AudioSource` in your scene is then assigned to one of these groups.
3.  **Exposed Parameters:** For each group, you can expose parameters (like the group's `Volume`) to script. These exposed parameters become the 'hooks' your C# code will use to interact with the mixer.
4.  **`AudioManager` Script (Singleton/Facade):** A central C# script (often a Singleton) acts as the `AudioManager`. This script holds a reference to the `AudioMixer` and provides public methods (e.g., `SetMusicVolume(float volume)`) that manipulate the exposed parameters.
5.  **Volume Conversion:** A crucial aspect is converting linear UI slider values (0-1) to the logarithmic decibel (dB) scale used by the `AudioMixer` (typically -80dB for mute, 0dB for max).

---

### Unity Editor Setup Instructions (Before using the script):

1.  **Create an AudioMixer Asset:**
    *   In your Unity Project window, right-click -> `Create` -> `Audio Mixer`. Name it `MainMixer`.
2.  **Configure AudioMixer Groups:**
    *   Select the `MainMixer` asset. This will open the Audio Mixer window.
    *   You'll see a default `Master` group.
    *   Click the **'+'** button next to "Groups" to add new groups:
        *   `Music` (Drag it under `Master` to make it a child)
        *   `SFX` (Drag it under `Master`)
        *   `UI` (Drag it under `Master`)
3.  **Expose Volume Parameters:**
    *   For each group (`Master`, `Music`, `SFX`, `UI`):
        *   Select the group in the Audio Mixer window.
        *   In the Inspector window, locate the `Volume` property.
        *   **Right-click** on the `Volume` label -> `Expose 'Volume (of <GroupName>)' to script`.
    *   After exposing, go to the "Exposed Parameters" section (top-right of the Audio Mixer window, usually a dropdown).
    *   Rename the exposed parameters to match the strings used in the `AudioManager` script:
        *   `MyExposedParam` (for Master) -> `MasterVolume`
        *   `MyExposedParam 1` (for Music) -> `MusicVolume`
        *   `MyExposedParam 2` (for SFX) -> `SFXVolume`
        *   `MyExposedParam 3` (for UI) -> `UIVolume`
        *(Ensure these names are **exact** and case-sensitive!)*
4.  **Create AudioManager GameObject:**
    *   In your scene, create an empty GameObject (e.g., `_AudioManager`).
    *   Attach the `AudioManager.cs` script (provided below) to this GameObject.
5.  **Assign References in Inspector:**
    *   Select the `_AudioManager` GameObject.
    *   Drag your `MainMixer` asset into the `Main Audio Mixer` slot in the Inspector.
    *   **Recommended:** Create two empty GameObjects as children of `_AudioManager`, name them `MusicAudioSource` and `SFXAudioSource`. Add `AudioSource` components to them.
        *   For `MusicAudioSource`'s `AudioSource`, set its `Output` to `MainMixer/Music`.
        *   For `SFXAudioSource`'s `AudioSource`, set its `Output` to `MainMixer/SFX`.
        *   Drag these `MusicAudioSource` and `SFXAudioSource` GameObjects into the respective `Music Audio Source` and `SFX Audio Source` slots in the `_AudioManager` script's Inspector. (The script will try to create them if unassigned, but manually assigning ensures correct mixer group routing).
6.  **Assign AudioMixerGroup to other AudioSources:**
    *   For any `AudioSource` components in your scene (e.g., on a Button for UI sound, on a character for SFX), ensure their `Output` property is set to the appropriate `AudioMixerGroup` (e.g., `MainMixer/UI` for UI sounds, `MainMixer/SFX` for general sound effects).

---

### `AudioManager.cs` Script

This script provides a complete implementation of the `AudioMixerPattern` in Unity, incorporating a Singleton pattern for easy access and `PlayerPrefs` for saving/loading settings.

```csharp
using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer
using System; // For EventHandler (though not directly used in this basic version, good to include for event-driven patterns)

/// <summary>
/// The AudioManager implements the AudioMixerPattern, acting as a centralized
/// control point for all audio settings and playback in the game.
///
/// It uses Unity's AudioMixer asset to manage different audio groups (Master, Music, SFX, UI)
/// and exposes parameters (like volume) from these groups to be controlled via script.
///
/// This script also incorporates the Singleton pattern for easy global access and
/// uses PlayerPrefs to save and load user-defined volume settings.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // Provides a globally accessible instance of the AudioManager.
    // This ensures there's only one audio manager throughout the game.
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Setup")]
    [Tooltip("Assign your main AudioMixer asset here. E.g., 'Assets/Audio/MainMixer.mixer'")]
    [SerializeField] private AudioMixer mainAudioMixer;

    [Header("Exposed Mixer Parameters (Match these exactly to your AudioMixer!)")]
    [Tooltip("The name of the exposed volume parameter for the Master group. (e.g., 'MasterVolume')")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [Tooltip("The name of the exposed volume parameter for the Music group. (e.g., 'MusicVolume')")]
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [Tooltip("The name of the exposed volume parameter for the SFX group. (e.g., 'SFXVolume')")]
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [Tooltip("The name of the exposed volume parameter for the UI group. (e.g., 'UIVolume')")]
    [SerializeField] private string uiVolumeParam = "UIVolume";

    // --- AudioSource for playing one-shot sounds and music ---
    // It's recommended to assign these in the Inspector to specific GameObjects
    // that have their AudioSource's Output linked to the correct mixer group.
    [Header("Audio Sources (Recommended to assign in Inspector)")]
    [Tooltip("AudioSource specifically for playing background music. Link its output to 'Music' group.")]
    [SerializeField] private AudioSource musicAudioSource;
    [Tooltip("AudioSource specifically for playing SFX. Link its output to 'SFX' group. For many SFX, consider an AudioSource pool.")]
    [SerializeField] private AudioSource sfxAudioSource;

    // --- Private Variables to hold current volumes (linear 0.0-1.0 range, useful for UI sliders) ---
    private float currentMasterVolume = 0.75f; // Default linear volume (75%)
    private float currentMusicVolume = 0.75f;
    private float currentSfxVolume = 0.75f;
    private float currentUIVolume = 0.75f;

    // --- PlayerPrefs Keys ---
    // Constants for PlayerPrefs keys to avoid magic strings and typos.
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string UI_VOLUME_KEY = "UIVolume";

    private void Awake()
    {
        // --- Singleton Pattern Implementation ---
        // Ensures only one instance of AudioManager exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep AudioManager alive across scene loads

        // --- Validation ---
        // Crucial check: Ensure the AudioMixer is assigned.
        if (mainAudioMixer == null)
        {
            Debug.LogError("AudioManager: No AudioMixer assigned! Please assign an AudioMixer asset in the Inspector.", this);
            return; // Cannot operate without a mixer
        }

        // --- Initialize AudioSources (if not assigned in Inspector) ---
        // If AudioSources are null, create them dynamically.
        // It's highly recommended to assign these in the Inspector and link their outputs to specific mixer groups for better control.
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.outputAudioMixerGroup = GetMixerGroup("Music", mainAudioMixer.outputAudioMixerGroup); // Link to Music group or Master fallback
            musicAudioSource.loop = true;
            Debug.LogWarning("AudioManager: Music AudioSource was not assigned in Inspector, a new one was created. Ensure its mixer group output is correct.", this);
        }
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.outputAudioMixerGroup = GetMixerGroup("SFX", mainAudioMixer.outputAudioMixerGroup); // Link to SFX group or Master fallback
            sfxAudioSource.loop = false; // SFX generally don't loop by default
            Debug.LogWarning("AudioManager: SFX AudioSource was not assigned in Inspector, a new one was created. Ensure its mixer group output is correct.", this);
        }

        LoadVolumes(); // Load saved volume settings on startup
    }

    /// <summary>
    /// Helper to safely get an AudioMixerGroup by name.
    /// If the specific group is not found, it falls back to the provided fallback group (usually Master).
    /// </summary>
    /// <param name="groupName">The name of the AudioMixerGroup to find.</param>
    /// <param name="fallbackGroup">The AudioMixerGroup to use if the named group is not found.</param>
    /// <returns>The found AudioMixerGroup or the fallback group.</returns>
    private AudioMixerGroup GetMixerGroup(string groupName, AudioMixerGroup fallbackGroup)
    {
        AudioMixerGroup[] groups = mainAudioMixer.FindMatchingGroups(groupName);
        if (groups != null && groups.Length > 0)
        {
            return groups[0];
        }
        Debug.LogWarning($"AudioManager: AudioMixerGroup '{groupName}' not found in mixer. Falling back to '{fallbackGroup.name}'. Ensure '{groupName}' group exists in your AudioMixer.", this);
        return fallbackGroup; // Fallback to the mixer's default output (often the Master group)
    }

    /// <summary>
    /// Loads volume settings from PlayerPrefs and applies them to the AudioMixer.
    /// If no settings are found, the current default values are used.
    /// </summary>
    private void LoadVolumes()
    {
        // Retrieve saved linear volume values from PlayerPrefs.
        // If a key doesn't exist, it uses the current (default) value.
        currentMasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, currentMasterVolume);
        currentMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, currentMusicVolume);
        currentSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, currentSfxVolume);
        currentUIVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, currentUIVolume);

        // Apply the loaded values to the AudioMixer.
        SetMasterVolume(currentMasterVolume);
        SetMusicVolume(currentMusicVolume);
        SetSfxVolume(currentSfxVolume);
        SetUIVolume(currentUIVolume);

        Debug.Log("AudioManager: Volumes loaded and applied from PlayerPrefs.");
    }

    /// <summary>
    /// Saves the current volume settings to PlayerPrefs.
    /// This should be called when settings change (e.g., after a slider is adjusted)
    /// or when the application is about to quit.
    /// </summary>
    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, currentMasterVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, currentMusicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, currentSfxVolume);
        PlayerPrefs.SetFloat(UI_VOLUME_KEY, currentUIVolume);
        PlayerPrefs.Save(); // Ensures changes are written to disk immediately
        Debug.Log("AudioManager: Volumes saved to PlayerPrefs.");
    }

    private void OnApplicationQuit()
    {
        SaveVolumes(); // Ensure volumes are saved automatically when the application closes
    }

    // --- Volume Control Methods (Public API) ---

    /// <summary>
    /// Sets the master volume of the game.
    /// This directly interacts with the exposed parameter on the AudioMixer.
    /// </summary>
    /// <param name="volume">A linear volume value between 0.0 (mute) and 1.0 (max).</param>
    public void SetMasterVolume(float volume)
    {
        currentMasterVolume = Mathf.Clamp01(volume); // Clamp to ensure value is within 0-1 range
        SetMixerVolume(masterVolumeParam, currentMasterVolume);
    }

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    /// <param name="volume">A linear volume value between 0.0 (mute) and 1.0 (max).</param>
    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = Mathf.Clamp01(volume);
        SetMixerVolume(musicVolumeParam, currentMusicVolume);
    }

    /// <summary>
    /// Sets the SFX (sound effects) volume.
    /// </summary>
    /// <param name="volume">A linear volume value between 0.0 (mute) and 1.0 (max).</param>
    public void SetSfxVolume(float volume)
    {
        currentSfxVolume = Mathf.Clamp01(volume);
        SetMixerVolume(sfxVolumeParam, currentSfxVolume);
    }

    /// <summary>
    /// Sets the UI (user interface) volume.
    /// </summary>
    /// <param name="volume">A linear volume value between 0.0 (mute) and 1.0 (max).</param>
    public void SetUIVolume(float volume)
    {
        currentUIVolume = Mathf.Clamp01(volume);
        SetMixerVolume(uiVolumeParam, currentUIVolume);
    }

    /// <summary>
    /// Generic helper method to set an AudioMixer parameter.
    /// It converts a linear 0-1 range (common for UI sliders) to a logarithmic dB scale,
    /// which is what Unity's AudioMixer uses for volume.
    /// </summary>
    /// <param name="parameterName">The exact string name of the exposed mixer parameter (e.g., "MasterVolume").</param>
    /// <param name="linearVolume">A linear volume value between 0.0 (mute) and 1.0 (max).</param>
    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (mainAudioMixer == null)
        {
            Debug.LogError($"AudioManager: Cannot set volume for '{parameterName}'. AudioMixer is null.", this);
            return;
        }

        // --- Linear to Logarithmic (dB) Conversion ---
        // Unity's AudioMixer volumes are in decibels (dB), which are logarithmic.
        // UI sliders usually provide a linear 0-1 value.
        // We convert the linear 0-1 value to a dB value:
        // 0.0f (mute) maps to -80dB (Unity's typical minimum).
        // 1.0f (max) maps to 0dB.
        // A small epsilon (0.0001f) is used to avoid Log10(0) which is undefined (-infinity).
        float mixerVolume = (linearVolume > 0.0001f) ? 20f * Mathf.Log10(linearVolume) : -80f;

        mainAudioMixer.SetFloat(parameterName, mixerVolume);
    }

    // --- Get Volume Methods (Public API for UI initialization) ---
    // These return the last set linear volume values.

    public float GetMasterVolume() => currentMasterVolume;
    public float GetMusicVolume() => currentMusicVolume;
    public float GetSfxVolume() => currentSfxVolume;
    public float GetUIVolume() => currentUIVolume;

    /// <summary>
    /// Retrieves a volume value directly from the mixer (in dB) and converts it
    /// back to a linear 0-1 range. Useful if you need the absolute current mixer value.
    /// </summary>
    /// <param name="parameterName">The name of the exposed mixer parameter.</param>
    /// <returns>Linear volume value between 0.0 and 1.0.</returns>
    public float GetMixerVolumeLinear(string parameterName)
    {
        if (mainAudioMixer == null)
        {
            Debug.LogError($"AudioManager: Cannot get volume for '{parameterName}'. AudioMixer is null.", this);
            return 0f;
        }

        float mixerVolume;
        if (mainAudioMixer.GetFloat(parameterName, out mixerVolume))
        {
            // --- Logarithmic (dB) to Linear Conversion ---
            // Convert dB back to linear 0-1 scale.
            return Mathf.Pow(10f, mixerVolume / 20f);
        }
        return 0f; // Default if parameter not found or error
    }


    // --- Basic Audio Playback Methods (Demonstrative) ---
    // These methods show how to play sounds using the AudioSources managed by AudioManager.
    // For more complex games, consider an AudioSource pooling system for SFX.

    /// <summary>
    /// Plays background music using the dedicated music AudioSource.
    /// Only one music track can play at a time via this method.
    /// </summary>
    /// <param name="clip">The AudioClip to play as music.</param>
    /// <param name="loop">Whether the music should loop indefinitely.</param>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicAudioSource == null)
        {
            Debug.LogWarning("AudioManager: Music AudioSource is not assigned or created. Cannot play music.");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: No music clip provided to play.");
            return;
        }

        // Only play if the clip is different or if the current clip has stopped.
        if (musicAudioSource.clip != clip || !musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop(); // Stop current music
            musicAudioSource.clip = clip;
            musicAudioSource.loop = loop;
            musicAudioSource.Play();
            Debug.Log($"AudioManager: Playing music '{clip.name}'.");
        }
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            Debug.Log("AudioManager: Music stopped.");
        }
    }

    /// <summary>
    /// Plays a single sound effect using the dedicated SFX AudioSource.
    /// This uses PlayOneShot, so it can layer multiple short SFX without interrupting each other.
    /// For many simultaneous SFX, an AudioSource pool is recommended for performance.
    /// </summary>
    /// <param name="clip">The AudioClip to play as a sound effect.</param>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioManager: SFX AudioSource is not assigned or created. Cannot play SFX.");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: No SFX clip provided to play.");
            return;
        }

        sfxAudioSource.PlayOneShot(clip); // Play the clip once.
        // Debug.Log($"AudioManager: Playing SFX '{clip.name}'."); // Uncomment for verbose SFX logging
    }

    /// <summary>
    /// Plays a single UI sound effect. This method simply calls PlaySFX,
    /// assuming UI sounds are routed through the SFX AudioMixerGroup.
    /// You could have a separate `uiAudioSource` for more granular control.
    /// </summary>
    /// <param name="clip">The AudioClip to play as a UI sound effect.</param>
    public void PlayUISound(AudioClip clip)
    {
        PlaySFX(clip); // UI sounds often go through the SFX channel or a dedicated UI channel
    }
}
```

---

### Example Usage in Other Scripts:

Once the `AudioManager` is set up in your scene, you can easily control audio from any other script:

```csharp
/*
/// Example Usage:
///
/// 1. Controlling Volumes from a UI Slider:
///    In your UI Canvas, create a Slider.
///    In the Slider's Inspector, find the "On Value Changed (Single)" event.
///    Drag your '_AudioManager' GameObject into the event slot.
///    From the dropdown, select 'AudioManager' -> 'SetMasterVolume'.
///    Repeat for Music, SFX, UI sliders.
///
///    // You can also write a dedicated UI script like this:
///    public class GameSettingsUI : MonoBehaviour
///    {
///        public Slider masterVolumeSlider;
///        public Slider musicVolumeSlider;
///        public Slider sfxVolumeSlider;
///
///        void Start()
///        {
///            // Initialize sliders to current volume settings
///            if (AudioManager.Instance != null)
///            {
///                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
///                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
///                sfxVolumeSlider.value = AudioManager.Instance.GetSfxVolume();
///            }
///
///            // Add listeners for value changes
///            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
///            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
///            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
///        }
///
///        public void SetMasterVolume(float value)
///        {
///            if (AudioManager.Instance != null)
///                AudioManager.Instance.SetMasterVolume(value);
///        }
///
///        public void SetMusicVolume(float value)
///        {
///            if (AudioManager.Instance != null)
///                AudioManager.Instance.SetMusicVolume(value);
///        }
///
///        public void SetSfxVolume(float value)
///        {
///            if (AudioManager.Instance != null)
///                AudioManager.Instance.SetSfxVolume(value);
///        }
///
///        public void OnApplySettingsClicked()
///        {
///            // You might have other settings to apply, then save volumes
///            if (AudioManager.Instance != null)
///                AudioManager.Instance.SaveVolumes();
///        }
///    }
///
/// 2. Playing Music:
///    Assign an AudioClip (e.g., your background music track) in the Inspector
///    to a public AudioClip variable in your scene manager or music trigger script.
///
///    public class SceneMusicManager : MonoBehaviour
///    {
///        public AudioClip backgroundMusicClip;
///
///        void Start()
///        {
///            if (AudioManager.Instance != null && backgroundMusicClip != null)
///            {
///                AudioManager.Instance.PlayMusic(backgroundMusicClip);
///            }
///        }
///
///        void OnDestroy()
///        {
///             // Optionally stop music when the scene unloads
///             // AudioManager.Instance?.StopMusic();
///        }
///    }
///
/// 3. Playing SFX:
///    Assign an AudioClip (e.g., a button click, a jump sound) in the Inspector
///    of the script that triggers the sound.
///
///    public class PlayerController : MonoBehaviour
///    {
///        public AudioClip jumpSFX;
///        public AudioClip landSFX;
///
///        void Update()
///        {
///            if (Input.GetButtonDown("Jump") && AudioManager.Instance != null && jumpSFX != null)
///            {
///                AudioManager.Instance.PlaySFX(jumpSFX);
///            }
///        }
///
///        // Example for collision sound
///        void OnCollisionEnter(Collision collision)
///        {
///            if (collision.gameObject.CompareTag("Ground") && AudioManager.Instance != null && landSFX != null)
///            {
///                AudioManager.Instance.PlaySFX(landSFX);
///            }
///        }
///    }
///
/// 4. Playing UI Sounds:
///    public class UIButtonHandler : MonoBehaviour
///    {
///        public AudioClip clickSound;
///
///        public void OnButtonClick()
///        {
///            if (AudioManager.Instance != null && clickSound != null)
///            {
///                AudioManager.Instance.PlayUISound(clickSound);
///            }
///        }
///    }
///
/// 5. Saving and Loading:
///    The AudioManager automatically saves volumes on ApplicationQuit and loads on Awake.
///    You can also manually trigger a save if settings change mid-game (e.g., upon applying options in a menu):
///
///    public void OnSettingsApplied()
///    {
///        AudioManager.Instance.SaveVolumes();
///    }
*/
```