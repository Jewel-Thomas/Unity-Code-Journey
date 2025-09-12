// Unity Design Pattern Example: DynamicMusicSystem
// This script demonstrates the DynamicMusicSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Dynamic Music System** design pattern. It allows your game's background music to change dynamically based on different game states (e.g., exploration, combat, menu, low health) with smooth cross-fading transitions.

The core idea is to have a central `MusicManager` (a Singleton) that other game systems can tell to switch music states. The `MusicManager` then handles the logic of finding the correct track, fading the current one out, and fading the new one in, creating a seamless audio experience.

---

### **`DynamicMusicSystem.cs`**

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The MusicState enum defines the different contexts or moods for the game's music.
/// Other game systems will use these states to tell the MusicManager which music to play.
/// </summary>
public enum MusicState
{
    None,        // No music explicitly playing, or initial state
    Menu,        // Music for the main menu or pause screens
    Exploration, // General background music for exploring
    Combat,      // Music when the player is in combat
    BossFight,   // Intense music for a boss encounter
    LowHealth,   // Music or an overlay track to indicate low player health
    Quiet,       // A state for minimal or no music, perhaps for cutscenes or specific areas
    Victory,     // Music for triumph or winning a level
    Defeat       // Music for failure or game over
}

/// <summary>
/// A serializable struct to hold an AudioClip and its specific volume setting for a given MusicState.
/// This makes it easy to assign and configure music tracks in the Unity Inspector.
/// </summary>
[Serializable]
public struct MusicTrackConfig
{
    public MusicState state;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

/// <summary>
/// DynamicMusicSystem: A Singleton MonoBehaviour that manages all dynamic music in the game.
/// It uses a cross-fading mechanism between two AudioSource components to smoothly transition
/// between different music tracks based on the current MusicState.
///
/// DESIGN PATTERN:
/// - Singleton: Ensures only one instance of the MusicManager exists and provides a global access point.
/// - State Machine (Implicit): Reacts to changes in 'MusicState' to play appropriate music.
/// - Strategy (via MusicTrackConfig): Defines the 'strategy' (which clip at what volume) for each state.
/// </summary>
[RequireComponent(typeof(AudioSource))] // Ensure there's at least one AudioSource
public class DynamicMusicSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    public static DynamicMusicSystem Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Music Settings")]
    [Tooltip("List of all music tracks, mapped to their respective MusicStates.")]
    [SerializeField] private List<MusicTrackConfig> musicConfigurations = new List<MusicTrackConfig>();

    [Tooltip("Duration in seconds for music to fade in/out during transitions.")]
    [SerializeField] private float fadeDuration = 2.0f;

    [Tooltip("Global master volume for all music played by this system.")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 0.7f;

    // --- Private Members ---
    private AudioSource _audioSource1; // Primary audio source for currently playing music
    private AudioSource _audioSource2; // Secondary audio source for cross-fading to new music
    private AudioSource _activeAudioSource; // Reference to the currently playing source
    private AudioSource _inactiveAudioSource; // Reference to the source ready for the next track

    private MusicState _currentMusicState = MusicState.None; // The currently active music state
    private Coroutine _fadeCoroutine; // Reference to the current fading coroutine to stop it if a new one starts

    // --- MonoBehaviour Lifecycle ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern: ensures only one instance exists.
    /// Also initializes AudioSources.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the MusicManager alive across scene changes

        InitializeAudioSources();
    }

    /// <summary>
    /// Finds or creates the two AudioSource components needed for cross-fading.
    /// Configures them to loop, not play on awake, and to be 2D (spatial blend 0).
    /// </summary>
    private void InitializeAudioSources()
    {
        // Get existing or add new AudioSources
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 1) _audioSource1 = gameObject.AddComponent<AudioSource>();
        else _audioSource1 = sources[0];

        if (sources.Length < 2) _audioSource2 = gameObject.AddComponent<AudioSource>();
        else _audioSource2 = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        // Configure AudioSources
        ConfigureAudioSource(_audioSource1);
        ConfigureAudioSource(_audioSource2);

        // Initially, audioSource1 is the active one, audioSource2 is inactive
        _activeAudioSource = _audioSource1;
        _inactiveAudioSource = _audioSource2;
    }

    /// <summary>
    /// Applies common settings to an AudioSource for background music.
    /// </summary>
    private void ConfigureAudioSource(AudioSource source)
    {
        source.loop = true;          // Music should loop by default
        source.playOnAwake = false;  // Don't play any music immediately on scene load
        source.spatialBlend = 0f;    // 2D sound, not affected by listener position
        source.volume = 0f;          // Start silent, will fade in
    }

    /// <summary>
    /// Update is called once per frame.
    /// Here, we can apply the master volume to the active source if needed,
    /// though it's typically handled directly by the fade coroutine.
    /// </summary>
    private void Update()
    {
        // Example: If you wanted a dynamic master volume control tied to a slider,
        // you would update _activeAudioSource.volume based on masterVolume here,
        // ensuring not to conflict with the fading coroutine's volume control.
        // For simplicity, we'll let the fade coroutine handle the final volume * masterVolume.
    }

    // --- Public API ---

    /// <summary>
    /// The primary method to request a music state change.
    /// Other game scripts (e.g., PlayerHealth, EnemySpawner, SceneLoader) will call this.
    /// </summary>
    /// <param name="newState">The desired MusicState to transition to.</param>
    public void SetMusicState(MusicState newState)
    {
        if (_currentMusicState == newState)
        {
            // Music is already in the requested state, no action needed.
            return;
        }

        Debug.Log($"[DynamicMusicSystem] Changing music state from '{_currentMusicState}' to '{newState}'");

        // Stop any ongoing fade operation to prevent conflicts
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // Start a new fade coroutine for the transition
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(newState));
        _currentMusicState = newState;
    }

    /// <summary>
    /// Stops all music immediately without fading.
    /// Useful for specific cutscenes or game over screens where silence is required.
    /// </summary>
    public void StopAllMusicImmediately()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _activeAudioSource.Stop();
        _activeAudioSource.volume = 0f;
        _inactiveAudioSource.Stop();
        _inactiveAudioSource.volume = 0f;
        _currentMusicState = MusicState.None;
        Debug.Log("[DynamicMusicSystem] All music stopped immediately.");
    }

    // --- Internal Logic for Fading ---

    /// <summary>
    /// Coroutine that handles the cross-fading between the current and new music tracks.
    /// It fades out the active source, sets up and fades in the inactive source,
    /// then swaps them.
    /// </summary>
    /// <param name="targetState">The MusicState to transition to.</param>
    private IEnumerator FadeMusicCoroutine(MusicState targetState)
    {
        MusicTrackConfig targetConfig = GetMusicConfigForState(targetState);
        AudioClip newClip = targetConfig.clip;
        float targetVolume = targetConfig.volume * masterVolume;

        // If no clip is configured for the target state, just fade out existing music.
        if (newClip == null)
        {
            Debug.LogWarning($"[DynamicMusicSystem] No music clip configured for state: {targetState}. Fading out current music.");
            yield return StartCoroutine(FadeOutCoroutine(_activeAudioSource));
            _activeAudioSource.Stop();
            _activeAudioSource.volume = 0f; // Ensure it's fully silent
            yield break; // Exit the coroutine if no new music to play
        }

        // 1. Prepare the inactive AudioSource for the new clip
        _inactiveAudioSource.clip = newClip;
        _inactiveAudioSource.Play(); // Start playing immediately, but it's still silent

        float timer = 0f;
        float startVolumeActive = _activeAudioSource.volume;
        float startVolumeInactive = _inactiveAudioSource.volume; // Should be 0, but for robustness

        // 2. Cross-fade: Fade out the active source, fade in the inactive source
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            // Fade out the currently playing music
            _activeAudioSource.volume = Mathf.Lerp(startVolumeActive, 0f, progress) * masterVolume;

            // Fade in the new music
            _inactiveAudioSource.volume = Mathf.Lerp(startVolumeInactive, targetVolume, progress) * masterVolume;

            yield return null;
        }

        // 3. Ensure final volumes are set correctly
        _activeAudioSource.volume = 0f;
        _inactiveAudioSource.volume = targetVolume;

        // 4. Stop the old music and clean up
        _activeAudioSource.Stop();
        _activeAudioSource.clip = null; // Release the old clip

        // 5. Swap the active and inactive audio sources for the next transition
        AudioSource temp = _activeAudioSource;
        _activeAudioSource = _inactiveAudioSource;
        _inactiveAudioSource = temp;

        Debug.Log($"[DynamicMusicSystem] Transition complete. Now playing '{targetState}' on {_activeAudioSource.name}.");
    }

    /// <summary>
    /// Helper coroutine to fade out a given AudioSource.
    /// </summary>
    private IEnumerator FadeOutCoroutine(AudioSource sourceToFade)
    {
        float startVolume = sourceToFade.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            sourceToFade.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration) * masterVolume;
            yield return null;
        }
        sourceToFade.volume = 0f;
        sourceToFade.Stop();
        sourceToFade.clip = null;
    }


    /// <summary>
    /// Retrieves the MusicTrackConfig for a given MusicState.
    /// </summary>
    /// <param name="state">The MusicState to look up.</param>
    /// <returns>The MusicTrackConfig associated with the state, or a default empty one if not found.</returns>
    private MusicTrackConfig GetMusicConfigForState(MusicState state)
    {
        foreach (var config in musicConfigurations)
        {
            if (config.state == state)
            {
                return config;
            }
        }
        // Return a default config if the state is not found
        Debug.LogWarning($"[DynamicMusicSystem] No music configuration found for state: {state}");
        return new MusicTrackConfig { state = state, clip = null, volume = 0f };
    }

    // --- Editor-only Functionality (for debugging/testing) ---
    [ContextMenu("Test: Play Exploration Music")]
    private void TestPlayExploration() => SetMusicState(MusicState.Exploration);

    [ContextMenu("Test: Play Combat Music")]
    private void TestPlayCombat() => SetMusicState(MusicState.Combat);

    [ContextMenu("Test: Play Menu Music")]
    private void TestPlayMenu() => SetMusicState(MusicState.Menu);

    [ContextMenu("Test: Stop All Music")]
    private void TestStopAllMusic() => StopAllMusicImmediately();
}

```

---

### **Example Usage in Other Scripts**

Here's how other scripts in your game would interact with the `DynamicMusicSystem`.

#### 1. Example: `GameManager.cs` (for general state changes)

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState { get; private set; }

    public enum GameState { MainMenu, Loading, Exploring, InCombat, BossFight, GameOver, Paused }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // Example: Start with menu music when the game begins
        SetGameState(GameState.MainMenu);
    }

    public void SetGameState(GameState newState)
    {
        CurrentGameState = newState;
        Debug.Log($"Game State changed to: {newState}");

        // Inform the music system about the new state
        switch (newState)
        {
            case GameState.MainMenu:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.Menu);
                break;
            case GameState.Exploring:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.Exploration);
                break;
            case GameState.InCombat:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.Combat);
                break;
            case GameState.BossFight:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.BossFight);
                break;
            case GameState.GameOver:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.Defeat); // Or stop music
                break;
            case GameState.Paused:
                DynamicMusicSystem.Instance.SetMusicState(MusicState.Quiet); // Or pause the current music
                // A more advanced system might "pause" the current music and resume it later.
                // For this example, we'll just switch to a quiet state.
                break;
            case GameState.Loading:
                DynamicMusicSystem.Instance.StopAllMusicImmediately();
                break;
        }
    }

    // Example methods to trigger state changes
    public void StartExploration() => SetGameState(GameState.Exploring);
    public void EnterCombat() => SetGameState(GameState.InCombat);
    public void EnterBossFight() => SetGameState(GameState.BossFight);
    public void ReturnToMenu() => SetGameState(GameState.MainMenu);
    public void PauseGame() => SetGameState(GameState.Paused);
    public void ResumeGame() => SetGameState(GameState.Exploring); // Assuming we resume to exploration
}
```

#### 2. Example: `PlayerHealth.cs` (for health-based music)

```csharp
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float lowHealthThreshold = 25f;

    private bool _isLowHealthMusicPlaying = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        // Simulate health changes (e.g., for testing)
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            TakeDamage(10);
        }
        if (Input.GetKeyDown(KeyCode.Equals)) // On some keyboards, '+' is shift + '='
        {
            Heal(10);
        }

        CheckHealthForMusic();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"Player took {amount} damage. Current Health: {currentHealth}");
        CheckHealthForMusic();
        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!");
            // GameManager.Instance.SetGameState(GameManager.GameState.GameOver);
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        Debug.Log($"Player healed {amount}. Current Health: {currentHealth}");
        CheckHealthForMusic();
    }

    private void CheckHealthForMusic()
    {
        if (currentHealth <= lowHealthThreshold && !_isLowHealthMusicPlaying)
        {
            // IMPORTANT: If 'LowHealth' is meant to be an *overlay* or *layer* on top of
            // existing music (e.g., Exploration/Combat), then DynamicMusicSystem would
            // need to support layering. For this example, it switches the *main* track.
            // If it's an overlay, you'd have a separate AudioSource for it.
            DynamicMusicSystem.Instance.SetMusicState(MusicState.LowHealth);
            _isLowHealthMusicPlaying = true;
        }
        else if (currentHealth > lowHealthThreshold && _isLowHealthMusicPlaying)
        {
            // Assuming we revert to the previous state.
            // A more robust system would need to track the *actual* previous state
            // or ask the GameManager for the current primary state.
            // For simplicity, let's assume we go back to exploration.
            // In a real game, you'd likely ask GameManager.Instance.CurrentGameState
            // and map that to a MusicState.
            DynamicMusicSystem.Instance.SetMusicState(MusicState.Exploration); // Or GameManager.Instance.CurrentGameState equivalent
            _isLowHealthMusicPlaying = false;
        }
    }
}
```

#### 3. Example: `CombatTrigger.cs` (for area-based music)

```csharp
using UnityEngine;

public class CombatTrigger : MonoBehaviour
{
    [Tooltip("The tag of the player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    private bool _playerIsInTrigger = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !_playerIsInTrigger)
        {
            _playerIsInTrigger = true;
            Debug.Log("Player entered combat trigger area. Initiating combat music.");
            // GameManager.Instance.EnterCombat(); // If you have a GameManager managing combat state
            DynamicMusicSystem.Instance.SetMusicState(MusicState.Combat);
            // Optionally, disable the trigger after activation if it's a one-time event
            // gameObject.SetActive(false);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && _playerIsInTrigger)
        {
            _playerIsInTrigger = false;
            Debug.Log("Player exited combat trigger area. Returning to exploration music.");
            // GameManager.Instance.StartExploration(); // If you have a GameManager
            DynamicMusicSystem.Instance.SetMusicState(MusicState.Exploration);
        }
    }
}
```

---

### **How to Use in Unity**

1.  **Create a New C# Script:** Name it `DynamicMusicSystem.cs` and copy the first code block into it.
2.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., `_GameManagers`).
3.  **Attach the Script:** Drag and drop the `DynamicMusicSystem.cs` script onto the `_GameManagers` GameObject.
4.  **Configure AudioSources:**
    *   The script will automatically add two `AudioSource` components if they don't exist.
    *   You'll see a `Music Settings` section in the Inspector.
5.  **Assign Music Clips:**
    *   Expand `Music Configurations`.
    *   Increase its `Size`.
    *   For each element:
        *   Choose a `State` from the dropdown (e.g., `Exploration`, `Combat`, `Menu`).
        *   Drag and drop an `AudioClip` (your `.mp3`, `.wav`, etc., music files) from your Project window into the `Clip` slot.
        *   Set the `Volume` for that specific track (it will be multiplied by the `Master Volume`).
    *   Repeat for all desired `MusicState`s.
6.  **Set Fade Duration and Master Volume:** Adjust these values to your liking.
7.  **Implement in Other Scripts:** Use the `DynamicMusicSystem.Instance.SetMusicState(MusicState.YourState);` call in your `GameManager`, `PlayerHealth`, `CombatTrigger`, or any other script that needs to influence the music.
8.  **Test:** You can use the `[ContextMenu]` items on the `DynamicMusicSystem` component in the Inspector (click the gear icon or right-click the component) to quickly test different music states in Play Mode.

This setup provides a robust and flexible way to manage your game's music, making it react dynamically to player actions and game events, significantly enhancing the player experience.