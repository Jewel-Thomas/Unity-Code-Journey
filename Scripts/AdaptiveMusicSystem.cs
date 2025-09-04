// Unity Design Pattern Example: AdaptiveMusicSystem
// This script demonstrates the AdaptiveMusicSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Adaptive Music System design pattern in games aims to create a dynamic and immersive audio experience by adjusting the background music based on the current game state, player actions, or other in-game parameters. Instead of having static background tracks, the music adapts, layers, crossfades, or switches to reflect the evolving gameplay.

This pattern typically involves:
1.  **Context/State:** Defining different game states (e.g., Exploration, Combat, Boss Battle, Low Health, Safe Zone).
2.  **Music Assets:** Assigning specific audio clips or layers to each state.
3.  **Transition Logic:** A central manager that detects state changes and smoothly transitions the music between tracks or layers (e.g., crossfading, volume ducking, adding/removing instrumental layers).
4.  **Audio Engine Integration:** Using features like `AudioSource`s, `AudioMixer`s, and `AudioMixerGroup`s in Unity for playback and advanced control.

## Adaptive Music System - Unity Example

This example demonstrates an `AdaptiveMusicSystem` in Unity that can:
*   Switch between **Exploration** and **Combat** music using a smooth crossfade.
*   Toggle an additional **Tension Layer** (e.g., for low health or high alert) that fades in/out on top of the current main music.

---

### `AdaptiveMusicSystem.cs`

This script is designed to be a singleton, meaning only one instance will exist in the scene, making it easily accessible from anywhere in your game.

```csharp
using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixerGroup
using System.Collections;

namespace DesignPatterns.AdaptiveMusic
{
    /// <summary>
    /// Represents the different high-level music contexts or states in the game.
    /// </summary>
    public enum MusicContext
    {
        Exploration,
        Combat,
        // Add more contexts as needed, e.g., Puzzle, BossBattle, Menu
    }

    /// <summary>
    /// A serializable class to hold an AudioClip and its specific volume setting.
    /// Useful for organizing music assets in the Inspector.
    /// </summary>
    [System.Serializable]
    public class MusicTrack
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f; // Target volume for this track
    }

    /// <summary>
    /// The core Adaptive Music System.
    /// Manages playback, crossfading between main music tracks, and layering dynamic elements.
    /// </summary>
    [RequireComponent(typeof(AudioSource))] // Ensure there's at least one AudioSource
    public class AdaptiveMusicSystem : MonoBehaviour
    {
        // --- Singleton Instance ---
        // Allows easy access to the music system from anywhere in the game.
        public static AdaptiveMusicSystem Instance { get; private set; }

        // --- Inspector Settings ---
        [Header("Main Music Tracks")]
        [Tooltip("Music played during exploration phases.")]
        public MusicTrack explorationMusic;
        [Tooltip("Music played during combat phases.")]
        public MusicTrack combatMusic;

        [Header("Dynamic Layers")]
        [Tooltip("An optional layer that can be faded in/out (e.g., for low health, high tension).")]
        public MusicTrack tensionLayer;

        [Header("Transition Settings")]
        [Tooltip("Duration for crossfading between main music tracks.")]
        [Range(0.1f, 5f)] public float crossfadeDuration = 2.0f;
        [Tooltip("Duration for fading the tension layer in or out.")]
        [Range(0.1f, 3f)] public float tensionLayerFadeDuration = 1.0f;

        [Header("Audio Sources & Mixers")]
        [Tooltip("AudioMixerGroup for the main background music.")]
        public AudioMixerGroup musicMixerGroup;
        [Tooltip("AudioMixerGroup for the tension layer, allowing separate control.")]
        public AudioMixerGroup tensionMixerGroup;

        // --- Internal Audio Sources ---
        // We use two main AudioSources for seamless crossfading between tracks.
        // primarySource plays the currently active main track.
        // secondarySource plays the track that is fading out or is about to fade in.
        private AudioSource primarySource;
        private AudioSource secondarySource;
        // A separate AudioSource for the tension layer, so it can be layered independently.
        private AudioSource tensionSource;

        // --- Internal State Variables ---
        private MusicContext currentContext = MusicContext.Exploration; // Default starting context
        private bool isTensionActive = false;
        private Coroutine currentCrossfadeCoroutine;
        private Coroutine currentTensionFadeCoroutine;

        // --- Lifecycle Methods ---

        private void Awake()
        {
            // Implement the Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the music playing across scene changes

            // Initialize AudioSources
            // We get the first AudioSource attached to this GameObject as primarySource
            primarySource = GetComponent<AudioSource>();
            if (primarySource == null)
            {
                Debug.LogError("AdaptiveMusicSystem requires an AudioSource component on its GameObject.");
                return;
            }

            // Create secondarySource and tensionSource dynamically if not assigned in Inspector
            // (or if we want to ensure they always exist and are configured correctly by code)
            secondarySource = CreateOrGetAudioSource("SecondaryMusicSource");
            tensionSource = CreateOrGetAudioSource("TensionLayerSource");

            // Configure all AudioSources
            ConfigureAudioSource(primarySource, musicMixerGroup, "PrimaryMusic");
            ConfigureAudioSource(secondarySource, musicMixerGroup, "SecondaryMusic");
            ConfigureAudioSource(tensionSource, tensionMixerGroup, "TensionLayer");
            tensionSource.volume = 0f; // Start tension layer silently
        }

        private void Start()
        {
            // Start the initial music context
            SetMusicContext(currentContext);
        }

        /// <summary>
        /// Helper to create or get an AudioSource and add it as a child GameObject.
        /// </summary>
        private AudioSource CreateOrGetAudioSource(string name)
        {
            Transform childTransform = transform.Find(name);
            GameObject childGO;
            if (childTransform == null)
            {
                childGO = new GameObject(name);
                childGO.transform.parent = transform;
            }
            else
            {
                childGO = childTransform.gameObject;
            }

            AudioSource source = childGO.GetComponent<AudioSource>();
            if (source == null)
            {
                source = childGO.AddComponent<AudioSource>();
            }
            return source;
        }

        /// <summary>
        /// Configures common properties for an AudioSource.
        /// </summary>
        private void ConfigureAudioSource(AudioSource source, AudioMixerGroup outputGroup, string debugName)
        {
            source.outputAudioMixerGroup = outputGroup;
            source.loop = true;          // Music usually loops
            source.playOnAwake = false;  // We'll control playback manually
            source.volume = 0f;          // Start silent, will be faded in
            source.spatialBlend = 0f;    // 2D sound for background music
            source.name = debugName;     // For easier debugging in Hierarchy
        }

        // --- Public Interface for Music Control ---

        /// <summary>
        /// Changes the main background music context (e.g., from Exploration to Combat).
        /// This will trigger a crossfade between the current main track and the new track.
        /// </summary>
        /// <param name="newContext">The new music context to switch to.</param>
        public void SetMusicContext(MusicContext newContext)
        {
            if (currentContext == newContext)
            {
                // Music is already in the requested context, no change needed.
                return;
            }

            Debug.Log($"AdaptiveMusicSystem: Changing context from {currentContext} to {newContext}");
            currentContext = newContext;

            MusicTrack targetTrack;
            switch (newContext)
            {
                case MusicContext.Exploration:
                    targetTrack = explorationMusic;
                    break;
                case MusicContext.Combat:
                    targetTrack = combatMusic;
                    break;
                default:
                    Debug.LogWarning($"AdaptiveMusicSystem: Unhandled music context: {newContext}. Playing default exploration music.");
                    targetTrack = explorationMusic;
                    break;
            }

            // Start the crossfade coroutine, stopping any previous one.
            if (currentCrossfadeCoroutine != null)
            {
                StopCoroutine(currentCrossfadeCoroutine);
            }
            currentCrossfadeCoroutine = StartCoroutine(CrossfadeMusic(targetTrack.clip, targetTrack.volume));
        }

        /// <summary>
        /// Toggles the tension layer (e.g., for low health or high alert states).
        /// This will smoothly fade the tension layer in or out.
        /// </summary>
        /// <param name="active">True to fade in the tension layer, false to fade it out.</param>
        public void SetTensionState(bool active)
        {
            if (isTensionActive == active)
            {
                // Tension state is already as requested.
                return;
            }

            Debug.Log($"AdaptiveMusicSystem: Tension state changing to: {active}");
            isTensionActive = active;

            // Stop any existing tension fade and start a new one.
            if (currentTensionFadeCoroutine != null)
            {
                StopCoroutine(currentTensionFadeCoroutine);
            }
            currentTensionFadeCoroutine = StartCoroutine(FadeTensionLayer(active));
        }

        // --- Coroutines for Smooth Transitions ---

        /// <summary>
        /// Coroutine to smoothly crossfade between the current main music track and a new one.
        /// </summary>
        private IEnumerator CrossfadeMusic(AudioClip newClip, float targetVolume)
        {
            if (newClip == null)
            {
                Debug.LogWarning("AdaptiveMusicSystem: New music clip is null. Stopping primary source.");
                primarySource.Stop();
                primarySource.clip = null;
                yield break;
            }

            // Swap roles: current primary becomes secondary, new track plays on primary.
            AudioSource oldPrimary = primarySource;
            primarySource = secondarySource;
            secondarySource = oldPrimary;

            // Configure the new primary source
            primarySource.clip = newClip;
            primarySource.volume = 0f; // Start new track silent
            if (!primarySource.isPlaying)
            {
                primarySource.Play();
            }

            float timer = 0f;
            float startVolumeSecondary = secondarySource.volume; // Capture current volume before fading
            
            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / crossfadeDuration;

                // Fade in the new track (primarySource)
                primarySource.volume = Mathf.Lerp(0f, targetVolume, progress);
                // Fade out the old track (secondarySource)
                secondarySource.volume = Mathf.Lerp(startVolumeSecondary, 0f, progress);

                yield return null;
            }

            // Ensure final volumes are set precisely
            primarySource.volume = targetVolume;
            secondarySource.volume = 0f;

            // Stop and clear the secondary source once fully faded out
            secondarySource.Stop();
            secondarySource.clip = null;

            currentCrossfadeCoroutine = null;
            Debug.Log($"AdaptiveMusicSystem: Crossfade complete. Now playing: {newClip.name}");
        }

        /// <summary>
        /// Coroutine to smoothly fade the tension layer in or out.
        /// </summary>
        private IEnumerator FadeTensionLayer(bool fadeIn)
        {
            if (tensionLayer.clip == null)
            {
                Debug.LogWarning("AdaptiveMusicSystem: Tension layer clip is null. Cannot fade tension layer.");
                yield break;
            }

            float targetVolume = fadeIn ? tensionLayer.volume : 0f;
            float startVolume = tensionSource.volume;

            if (fadeIn && !tensionSource.isPlaying)
            {
                tensionSource.clip = tensionLayer.clip;
                tensionSource.Play();
            }

            float timer = 0f;
            while (timer < tensionLayerFadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / tensionLayerFadeDuration;
                tensionSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                yield return null;
            }

            // Ensure final volume is set precisely
            tensionSource.volume = targetVolume;

            if (!fadeIn)
            {
                tensionSource.Stop();
                tensionSource.clip = null; // Clear the clip when not playing
            }

            currentTensionFadeCoroutine = null;
            Debug.Log($"AdaptiveMusicSystem: Tension layer fade {(fadeIn ? "in" : "out")} complete.");
        }
    }
}
```

---

### How to Implement and Use in Unity

**1. Create the `AdaptiveMusicSystem` GameObject:**
   *   In your Unity project, create an empty GameObject (e.g., `_MusicManager`).
   *   Attach the `AdaptiveMusicSystem.cs` script to this GameObject.

**2. Prepare AudioMixer (Recommended):**
   *   Go to `Window > Audio > Audio Mixer`.
   *   Create a new `Audio Mixer` (e.g., `MainAudioMixer`).
   *   Inside the mixer, click the "+" button next to "Groups" to create new `AudioMixerGroup`s.
   *   Create at least two groups:
      *   `Music` (for `musicMixerGroup`)
      *   `Tension` (for `tensionMixerGroup`)
   *   You can then adjust volumes, add effects, or create snapshots on these groups independently.

**3. Assign Settings in the Inspector:**
   *   Select the `_MusicManager` GameObject in the Hierarchy.
   *   In the `AdaptiveMusicSystem` component:
      *   **Main Music Tracks:**
         *   Drag your `Exploration.mp3`/`.wav` to `Exploration Music > Clip`. Set its desired `Volume`.
         *   Drag your `Combat.mp3`/`.wav` to `Combat Music > Clip`. Set its desired `Volume`.
      *   **Dynamic Layers:**
         *   Drag your `TensionLoop.mp3`/`.wav` to `Tension Layer > Clip`. Set its desired `Volume`.
      *   **Transition Settings:** Adjust `Crossfade Duration` and `Tension Layer Fade Duration` to your liking.
      *   **Audio Sources & Mixers:**
         *   Drag your `Music` `AudioMixerGroup` to the `Music Mixer Group` slot.
         *   Drag your `Tension` `AudioMixerGroup` to the `Tension Mixer Group` slot.

   *   The script will automatically create child GameObjects (`PrimaryMusicSource`, `SecondaryMusicSource`, `TensionLayerSource`) with `AudioSource` components if they don't exist, and configure them.

**4. Example Usage in Other Scripts:**

Now, from any other script in your game, you can easily control the music:

```csharp
using UnityEngine;
using DesignPatterns.AdaptiveMusic; // Important: include the namespace

public class GameStateManager : MonoBehaviour
{
    [Header("Player Health Simulation")]
    [Range(0, 100)] public int playerHealth = 100;
    private int _previousHealth;
    public int lowHealthThreshold = 25;

    void Start()
    {
        _previousHealth = playerHealth;
        // Ensure the music system exists and is initialized
        if (AdaptiveMusicSystem.Instance == null)
        {
            Debug.LogError("AdaptiveMusicSystem not found in the scene! Please add it.");
            enabled = false; // Disable this script if no music system
            return;
        }

        // Start with exploration music by default
        AdaptiveMusicSystem.Instance.SetMusicContext(MusicContext.Exploration);
        // Ensure tension is off initially
        AdaptiveMusicSystem.Instance.SetTensionState(false);
    }

    void Update()
    {
        // --- Simulate game state changes ---

        // Example: Toggle combat music with 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Switching to Combat music!");
            AdaptiveMusicSystem.Instance.SetMusicContext(MusicContext.Combat);
        }
        // Example: Toggle exploration music with 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Switching to Exploration music!");
            AdaptiveMusicSystem.Instance.SetMusicContext(MusicContext.Exploration);
        }

        // --- Simulate player health and tension layer ---
        if (playerHealth != _previousHealth)
        {
            if (playerHealth <= lowHealthThreshold)
            {
                Debug.Log("Player health is low! Activating tension music.");
                AdaptiveMusicSystem.Instance.SetTensionState(true);
            }
            else
            {
                Debug.Log("Player health is above threshold. Deactivating tension music.");
                AdaptiveMusicSystem.Instance.SetTensionState(false);
            }
            _previousHealth = playerHealth;
        }

        // Example: Quick way to simulate health changes
        if (Input.GetKeyDown(KeyCode.Alpha1)) playerHealth = 10; // Low Health
        if (Input.GetKeyDown(KeyCode.Alpha2)) playerHealth = 50; // Medium Health
        if (Input.GetKeyDown(KeyCode.Alpha3)) playerHealth = 100; // Full Health
    }

    // Example of calling from an event
    public void OnEnemyEncountered()
    {
        AdaptiveMusicSystem.Instance.SetMusicContext(MusicContext.Combat);
    }

    public void OnCombatEnded()
    {
        AdaptiveMusicSystem.Instance.SetMusicContext(MusicContext.Exploration);
    }

    public void OnPlayerTookDamage(int damage)
    {
        playerHealth -= damage;
        playerHealth = Mathf.Max(0, playerHealth); // Ensure health doesn't go below 0
    }

    public void OnPlayerHealed(int amount)
    {
        playerHealth += amount;
        playerHealth = Mathf.Min(100, playerHealth); // Ensure health doesn't go above 100
    }
}
```

**5. Create a `GameStateManager` (or similar) GameObject:**
   *   Create another empty GameObject (e.g., `_GameStateManager`).
   *   Attach the `GameStateManager.cs` script (or your equivalent game logic script) to it.
   *   You can then adjust the `Player Health` and `Low Health Threshold` in its Inspector to test the tension layer.

**Key Concepts Demonstrated:**

*   **Singleton Pattern:** `AdaptiveMusicSystem.Instance` provides a global, easily accessible point of control for music.
*   **State-Driven Logic:** The `MusicContext` enum and `SetMusicContext()` method allow the game to tell the music system what "kind" of music to play based on the current game state.
*   **Layering:** The `tensionSource` demonstrates how additional musical layers can be faded in/out on top of the main background music, enriching the adaptive experience.
*   **Smooth Transitions (Crossfading):** Using `AudioSource`s and `Coroutine`s with `Mathf.Lerp` ensures that music transitions are not abrupt but blend seamlessly.
*   **Unity Best Practices:**
    *   `[SerializeField]`, `[Header]`, `[Tooltip]` for clear Inspector usage.
    *   `RequireComponent` to ensure necessary components exist.
    *   `DontDestroyOnLoad` to persist the music manager across scenes.
    *   `AudioMixerGroup` integration for professional audio control (allowing sound designers to tweak levels and effects without code changes).
    *   Dynamic `AudioSource` creation for robustness.

This setup provides a robust and flexible foundation for an adaptive music system in any Unity game, allowing for complex and dynamic audio experiences.