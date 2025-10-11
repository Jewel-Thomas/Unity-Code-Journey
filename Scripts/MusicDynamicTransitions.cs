// Unity Design Pattern Example: MusicDynamicTransitions
// This script demonstrates the MusicDynamicTransitions pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Music Dynamic Transitions** design pattern in Unity. The core idea is to seamlessly change background music based on different game states or events, often using crossfading to ensure a smooth, professional-sounding transition.

The `MusicManager` script uses two `AudioSource` components to achieve smooth crossfades. While one `AudioSource` is fading out, the other is fading in the new track.

---

### **MusicDynamicTransitions Pattern: Unity Example**

**Goal:** Provide a centralized system for playing and transitioning background music based on game states.

**Key Components:**
1.  **`MusicManager.cs`**: A Singleton script that controls all music playback and transitions. It manages multiple `AudioSource` components for crossfading.
2.  **`MusicState` Enum**: Defines the different named states for which specific music tracks exist (e.g., Exploration, Combat, Menu).
3.  **`MusicTrackData` Struct**: A serializable struct to pair a `MusicState` with its `AudioClip` and other properties in the Inspector.
4.  **`AudioSource` Components**: Two `AudioSource` components attached to the `MusicManager` GameObject for crossfading.

---

### **1. `MusicManager.cs` (The Core Script)**

This script will be a `Singleton` for easy access from any other part of your game.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for Dictionary and List

/// <summary>
/// Defines the different states for which unique music tracks can be assigned.
/// This enum makes it easy to refer to specific music without magic strings.
/// </summary>
public enum MusicState
{
    None,        // No specific music state, or silence
    Menu,        // Music for the main menu or character selection
    Exploration, // Music for general gameplay, moving around
    Combat,      // Music when engaged in battle
    BossFight,   // Special music for boss encounters
    Victory,     // Music after winning a battle/level
    GameOver     // Music for when the player loses
}

/// <summary>
/// A serializable struct to easily link a MusicState to its AudioClip
/// and other properties in the Unity Inspector.
/// </summary>
[System.Serializable]
public struct MusicTrackData
{
    [Tooltip("The game state this music track belongs to.")]
    public MusicState state;

    [Tooltip("The actual audio clip for this state.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for this specific track.")]
    public float volumeMultiplier; // Allows per-track volume adjustment

    public MusicTrackData(MusicState s, AudioClip c, float vm = 1f)
    {
        state = s;
        clip = c;
        volumeMultiplier = vm;
    }
}

/// <summary>
/// MusicManager is a Singleton class responsible for dynamic music transitions.
/// It uses two AudioSource components to crossfade between different music tracks
/// based on the current game state.
/// </summary>
public class MusicManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static MusicManager Instance { get; private set; }

    // --- Inspector Assigned Fields ---
    [Header("Audio Sources")]
    [Tooltip("The first AudioSource. This will play the initial music.")]
    public AudioSource audioSourceA;
    [Tooltip("The second AudioSource. Used for crossfading with AudioSource A.")]
    public AudioSource audioSourceB;

    [Header("Music Tracks")]
    [Tooltip("List of all music tracks and their corresponding states.")]
    public List<MusicTrackData> musicTracks = new List<MusicTrackData>();

    [Header("Transition Settings")]
    [Tooltip("Duration in seconds for music crossfading.")]
    [Range(0.1f, 10f)]
    public float transitionDuration = 2.0f;

    [Tooltip("Maximum global volume for the music.")]
    [Range(0f, 1f)]
    public float maxVolume = 0.7f;

    [Tooltip("The music state to start with when the game begins.")]
    public MusicState initialMusicState = MusicState.Menu;

    // --- Private Internal State ---
    private Dictionary<MusicState, MusicTrackData> musicLibrary; // Fast lookup for clips
    private AudioSource activeAudioSource; // The AudioSource currently playing foreground music
    private AudioSource inactiveAudioSource; // The AudioSource that will take over during transition

    private MusicState currentMusicState = MusicState.None;
    private Coroutine transitionCoroutine; // To manage ongoing transitions

    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep MusicManager alive across scenes
        }

        // Initialize the music library dictionary for quick lookups
        musicLibrary = new Dictionary<MusicState, MusicTrackData>();
        foreach (var trackData in musicTracks)
        {
            if (trackData.clip != null && !musicLibrary.ContainsKey(trackData.state))
            {
                musicLibrary.Add(trackData.state, trackData);
            }
            else if (trackData.clip == null)
            {
                Debug.LogWarning($"MusicManager: Track for state '{trackData.state}' has no AudioClip assigned.", this);
            }
            else if (musicLibrary.ContainsKey(trackData.state))
            {
                Debug.LogWarning($"MusicManager: Duplicate entry for MusicState '{trackData.state}'. Only the first will be used.", this);
            }
        }

        // Validate AudioSources
        if (audioSourceA == null || audioSourceB == null)
        {
            Debug.LogError("MusicManager: Both AudioSourceA and AudioSourceB must be assigned in the Inspector!", this);
            enabled = false; // Disable script if critical components are missing
            return;
        }

        // Set up initial AudioSource states
        audioSourceA.loop = true;
        audioSourceB.loop = true;
        audioSourceA.playOnAwake = false; // We'll manage playback manually
        audioSourceB.playOnAwake = false;
        audioSourceA.volume = 0f;
        audioSourceB.volume = 0f;

        // Arbitrarily set A as active initially
        activeAudioSource = audioSourceA;
        inactiveAudioSource = audioSourceB;
    }

    private void Start()
    {
        // Start playing the initial music defined in the Inspector
        if (initialMusicState != MusicState.None)
        {
            PlayMusicState(initialMusicState);
        }
    }

    // --- Public API for Music Control ---

    /// <summary>
    /// Changes the current background music to the track associated with the new state.
    /// If the new state is already playing, or if the clip for the state is missing,
    /// no action is taken.
    /// </summary>
    /// <param name="newState">The target MusicState to transition to.</param>
    public void PlayMusicState(MusicState newState)
    {
        if (newState == currentMusicState)
        {
            // Music for this state is already playing, do nothing
            return;
        }

        if (!musicLibrary.TryGetValue(newState, out MusicTrackData newTrackData))
        {
            Debug.LogWarning($"MusicManager: No music clip found for state: {newState}. Playing silence instead or stopping current music.", this);
            // Option 1: Stop current music if no clip found
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeOutCurrentMusic());
            currentMusicState = MusicState.None; // Reflect that nothing is playing
            return;
        }

        // Stop any ongoing transition before starting a new one
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        // Start the crossfade transition
        transitionCoroutine = StartCoroutine(TransitionMusic(newTrackData));
    }

    /// <summary>
    /// Stops all music playback gracefully by fading out the current track.
    /// </summary>
    public void StopMusic()
    {
        if (currentMusicState == MusicState.None && activeAudioSource.volume <= 0.01f)
        {
            // Already stopped or very quiet, do nothing
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(FadeOutCurrentMusic());
        currentMusicState = MusicState.None;
    }

    // --- Private Coroutines for Transitions ---

    /// <summary>
    /// Coroutine to handle the crossfade between the current and new music tracks.
    /// </summary>
    /// <param name="newTrackData">The MusicTrackData for the incoming track.</param>
    private IEnumerator TransitionMusic(MusicTrackData newTrackData)
    {
        AudioClip newClip = newTrackData.clip;
        float newTrackVolume = maxVolume * newTrackData.volumeMultiplier;

        // If no music is currently playing, just fade in the new track
        if (currentMusicState == MusicState.None || activeAudioSource.clip == null || !activeAudioSource.isPlaying)
        {
            activeAudioSource.clip = newClip;
            activeAudioSource.volume = 0f;
            activeAudioSource.Play();
            currentMusicState = newTrackData.state;

            float timer = 0f;
            while (timer < transitionDuration)
            {
                timer += Time.unscaledDeltaTime; // Use unscaledDeltaTime for pause-independent transitions
                activeAudioSource.volume = Mathf.Lerp(0f, newTrackVolume, timer / transitionDuration);
                yield return null;
            }
            activeAudioSource.volume = newTrackVolume; // Ensure it reaches full volume
            yield break; // Exit coroutine
        }

        // Normal crossfade scenario: prepare inactive, fade active out, fade inactive in
        inactiveAudioSource.clip = newClip;
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.Play(); // Start playing silently

        float timer = 0f;
        float startActiveVolume = activeAudioSource.volume; // Capture current volume for smooth fade-out

        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaledDeltaTime for pause-independent transitions

            // Fade out the active AudioSource
            activeAudioSource.volume = Mathf.Lerp(startActiveVolume, 0f, timer / transitionDuration);

            // Fade in the inactive AudioSource
            inactiveAudioSource.volume = Mathf.Lerp(0f, newTrackVolume, timer / transitionDuration);

            yield return null;
        }

        // Ensure final volumes are set correctly
        activeAudioSource.volume = 0f;
        inactiveAudioSource.volume = newTrackVolume;

        // Stop the now-inactive AudioSource and clear its clip
        activeAudioSource.Stop();
        activeAudioSource.clip = null;

        // Swap the roles of active and inactive AudioSources
        AudioSource temp = activeAudioSource;
        activeAudioSource = inactiveAudioSource;
        inactiveAudioSource = temp;

        currentMusicState = newTrackData.state; // Update the current music state
        transitionCoroutine = null; // Mark transition as complete
    }

    /// <summary>
    /// Coroutine to fade out the current music to complete silence.
    /// </summary>
    private IEnumerator FadeOutCurrentMusic()
    {
        if (activeAudioSource.clip == null || !activeAudioSource.isPlaying)
        {
            yield break; // Nothing to fade out
        }

        float startVolume = activeAudioSource.volume;
        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            activeAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / transitionDuration);
            yield return null;
        }

        activeAudioSource.volume = 0f;
        activeAudioSource.Stop();
        activeAudioSource.clip = null;
        currentMusicState = MusicState.None;
        transitionCoroutine = null;
    }

    /// <summary>
    /// Gets the currently playing MusicState.
    /// </summary>
    public MusicState GetCurrentMusicState()
    {
        return currentMusicState;
    }
}
```

---

### **2. Example Usage (`GameManager.cs` - for demonstration)**

You would create another script, like a `GameManager` or `SceneController`, to actually call the `MusicManager`'s methods.

```csharp
using UnityEngine;

/// <summary>
/// A simple example GameManager to demonstrate how to use the MusicManager.
/// Assign this script to any GameObject in your scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Tooltip("Press this key to switch to Exploration Music.")]
    public KeyCode explorationKey = KeyCode.Alpha1;
    [Tooltip("Press this key to switch to Combat Music.")]
    public KeyCode combatKey = KeyCode.Alpha2;
    [Tooltip("Press this key to switch to Boss Fight Music.")]
    public KeyCode bossKey = KeyCode.Alpha3;
    [Tooltip("Press this key to switch to Menu Music.")]
    public KeyCode menuKey = KeyCode.Alpha4;
    [Tooltip("Press this key to switch to Game Over Music.")]
    public KeyCode gameOverKey = KeyCode.Alpha5;
    [Tooltip("Press this key to stop all music.")]
    public KeyCode stopMusicKey = KeyCode.Alpha0;

    void Update()
    {
        // Ensure the MusicManager instance exists before trying to use it
        if (MusicManager.Instance == null)
        {
            Debug.LogError("GameManager: MusicManager.Instance is null. Make sure MusicManager GameObject is in the scene and set up correctly.", this);
            return;
        }

        // Example key presses to trigger music changes
        if (Input.GetKeyDown(explorationKey))
        {
            Debug.Log($"Switching to {MusicState.Exploration} music.");
            MusicManager.Instance.PlayMusicState(MusicState.Exploration);
        }
        else if (Input.GetKeyDown(combatKey))
        {
            Debug.Log($"Switching to {MusicState.Combat} music.");
            MusicManager.Instance.PlayMusicState(MusicState.Combat);
        }
        else if (Input.GetKeyDown(bossKey))
        {
            Debug.Log($"Switching to {MusicState.BossFight} music.");
            MusicManager.Instance.PlayMusicState(MusicState.BossFight);
        }
        else if (Input.GetKeyDown(menuKey))
        {
            Debug.Log($"Switching to {MusicState.Menu} music.");
            MusicManager.Instance.PlayMusicState(MusicState.Menu);
        }
        else if (Input.GetKeyDown(gameOverKey))
        {
            Debug.Log($"Switching to {MusicState.GameOver} music.");
            MusicManager.Instance.PlayMusicState(MusicState.GameOver);
        }
        else if (Input.GetKeyDown(stopMusicKey))
        {
            Debug.Log($"Stopping all music.");
            MusicManager.Instance.StopMusic();
        }

        // Example: Transition based on a hypothetical game state
        // In a real game, these conditions would come from your game logic.
        /*
        if (Player.IsInCombat && MusicManager.Instance.GetCurrentMusicState() != MusicState.Combat)
        {
            MusicManager.Instance.PlayMusicState(MusicState.Combat);
        }
        else if (!Player.IsInCombat && MusicManager.Instance.GetCurrentMusicState() == MusicState.Combat)
        {
            MusicManager.Instance.PlayMusicState(MusicState.Exploration); // Or whatever the ambient state is
        }
        */
    }

    void OnGUI()
    {
        // Simple GUI to show instructions
        GUI.Label(new Rect(10, 10, 300, 25), $"Current Music: {MusicManager.Instance?.GetCurrentMusicState()}");
        GUI.Label(new Rect(10, 30, 300, 25), $"Press {explorationKey} for Exploration");
        GUI.Label(new Rect(10, 50, 300, 25), $"Press {combatKey} for Combat");
        GUI.Label(new Rect(10, 70, 300, 25), $"Press {bossKey} for Boss Fight");
        GUI.Label(new Rect(10, 90, 300, 25), $"Press {menuKey} for Menu");
        GUI.Label(new Rect(10, 110, 300, 25), $"Press {gameOverKey} for Game Over");
        GUI.Label(new Rect(10, 130, 300, 25), $"Press {stopMusicKey} to Stop Music");
    }
}
```

---

### **3. Unity Setup Instructions:**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject and name it `MusicManager`.
2.  **Add `MusicManager.cs`:** Drag and drop the `MusicManager.cs` script onto the `MusicManager` GameObject in the Inspector.
3.  **Add Two `AudioSource` Components:**
    *   Select the `MusicManager` GameObject.
    *   Click "Add Component" and search for "Audio Source". Add two of them.
    *   **Crucially**: Drag the first `AudioSource` component from the Inspector to the `Audio Source A` slot on your `MusicManager` script component.
    *   Drag the second `AudioSource` component to the `Audio Source B` slot.
    *   **Settings for AudioSources:** For both `AudioSource` components:
        *   Uncheck `Play On Awake`.
        *   Check `Loop` (the script manages this, but it's good practice).
        *   Set `Volume` to `0` initially (the script controls this).
        *   Optionally, set `Spatial Blend` to `0` (2D sound) unless you specifically want 3D music.
4.  **Prepare Audio Clips:** Import some `.mp3` or `.wav` music files into your Unity project. You'll need at least one for each `MusicState` you want to use (e.g., "MenuMusic.mp3", "ExplorationTheme.wav", "CombatTrack.ogg").
5.  **Populate Music Tracks:**
    *   Select the `MusicManager` GameObject.
    *   In the `MusicManager` script component, find the `Music Tracks` list.
    *   Increase the `Size` to match the number of music tracks you have.
    *   For each element:
        *   Select the `Music State` from the dropdown (e.g., `Menu`, `Exploration`).
        *   Drag your corresponding `AudioClip` from the Project window into the `Clip` slot.
        *   Adjust `Volume Multiplier` if you want a specific track to be louder or quieter than the global `Max Volume`.
6.  **Set Initial Music State:** Choose the `Initial Music State` (e.g., `Menu`) from the dropdown. This is the music that will start playing when your game begins.
7.  **Adjust Transition Settings:**
    *   Set `Transition Duration` (e.g., `2` seconds for a smooth fade).
    *   Set `Max Volume` (e.g., `0.7` to leave room for sound effects).
8.  **Add `GameManager.cs` (or your equivalent):** Create a new empty GameObject (e.g., `_GameLogic`) and attach the `GameManager.cs` script to it. This will allow you to test the transitions with key presses.
9.  **Run the Scene:** Play your Unity scene and press the assigned keys (Alpha0-5 by default) to hear the music transition dynamically. Watch the Inspector of the `MusicManager` GameObject to see which `AudioSource` is `active` and how their volumes change.

---

### **How the Music Dynamic Transitions Pattern Works Here:**

*   **Singleton for Global Access:** `MusicManager.Instance` provides a single point of access, making it easy for any script (e.g., `GameManager`, `PlayerController`, `EnemyAI`) to request music changes without needing a direct reference. `DontDestroyOnLoad` ensures the music continues across scene changes.
*   **State-Based Music:** The `MusicState` enum and `musicLibrary` dictionary map logical game states to specific `AudioClip`s. This decouples music selection from hardcoded file paths.
*   **Two Audio Sources for Crossfading:**
    *   `activeAudioSource` is the one currently playing the main music.
    *   `inactiveAudioSource` is the one that's either silent or preparing to take over.
    *   When `PlayMusicState()` is called for a *new* state, the `inactiveAudioSource` is assigned the new clip and starts playing silently.
    *   The `TransitionMusic` coroutine then simultaneously fades `activeAudioSource` out and `inactiveAudioSource` in over a specified `transitionDuration`.
    *   Once the fade is complete, the `activeAudioSource` is stopped and its clip cleared, and the roles of `activeAudioSource` and `inactiveAudioSource` are swapped. This ensures the system is always ready for the next transition.
*   **Coroutines for Smooth Transitions:** `IEnumerator TransitionMusic()` handles the gradual change in volume over time. Using `Time.unscaledDeltaTime` ensures that transitions can still occur smoothly even if the game is paused (where `Time.deltaTime` would be 0).
*   **Error Handling and Robustness:** Checks for null `AudioSource`s, missing audio clips, and duplicate `MusicState` entries ensure the manager behaves predictably. It also stops any ongoing transition before starting a new one to prevent conflicts.
*   **Flexibility:**
    *   `MusicTrackData` allows per-track volume adjustment.
    *   `initialMusicState` configures starting music easily.
    *   `StopMusic()` provides a way to gracefully fade out all music.

This setup provides a powerful and flexible way to manage your game's background music, making it react dynamically to gameplay and enhancing the player's experience.