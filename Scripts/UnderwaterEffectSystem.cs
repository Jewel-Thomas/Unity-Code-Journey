// Unity Design Pattern Example: UnderwaterEffectSystem
// This script demonstrates the UnderwaterEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **UnderwaterEffectSystem** pattern in Unity. This pattern acts as a **Facade** that provides a simplified, unified interface (`EnterWater()`, `ExitWater()`) to a complex subsystem of various visual, audio, and potentially physical effects that should activate when a player or camera goes underwater, and deactivate when they resurface.

It also leverages the **Strategy** pattern internally, where each individual effect (fog, post-processing, audio, particles, caustics) is a concrete strategy implementing an `IEffect` interface. This makes the system highly extensible – new effects can be added without modifying the core `UnderwaterEffectManager`.

---

### **`UnderwaterEffectSystem.cs`**

```csharp
using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // Required for Post-Processing effects
using System.Collections.Generic;          // For List<T>
using System;                              // For Action delegate (events)

// ==============================================================================================
// SECTION 1: The IEffect Interface
// ==============================================================================================
// This interface defines the contract for any effect that can be part of the UnderwaterEffectSystem.
// By using an interface, we achieve polymorphism, allowing the manager to treat different types
// of effects (fog, post-processing, audio, particles) uniformly.
// This is a key aspect of making the system extensible and adhering to the Open/Closed Principle.
public interface IEffect
{
    // Initializes the effect with necessary scene objects or references from the manager.
    // This allows plain C# effect classes to interact with Unity components without being MonoBehaviours themselves.
    void Initialize(GameObject userObject, UnderwaterEffectManager manager);

    // Activates the effect when entering the underwater state.
    void Activate();

    // Deactivates the effect when exiting the underwater state, often restoring original settings.
    void Deactivate();

    // Updates the effect continuously while active (e.g., for animations, particle emission).
    void UpdateEffect();
}

// ==============================================================================================
// SECTION 2: Concrete Effect Implementations
// ==============================================================================================
// These are specific implementations of the IEffect interface. They are plain C# classes,
// not MonoBehaviours. They encapsulate the logic for a single type of underwater effect.
// The UnderwaterEffectManager will instantiate and manage these classes.

/// <summary>
/// Manages global fog settings to simulate underwater visibility.
/// Stores and restores original fog settings.
/// </summary>
public class FogUnderwaterEffect : IEffect
{
    // Store original RenderSettings to restore them upon exiting water
    private bool _originalFogEnabled;
    private Color _originalFogColor;
    private float _originalFogDensity;
    private FogMode _originalFogMode;

    // Configuration for the underwater fog (set by the manager)
    public bool ApplyUnderwaterFog = true;
    public Color UnderwaterFogColor = new Color(0.08f, 0.2f, 0.25f, 1f);
    public float UnderwaterFogDensity = 0.05f;
    public FogMode UnderwaterFogMode = FogMode.ExponentialSquared;

    public void Initialize(GameObject userObject, UnderwaterEffectManager manager)
    {
        // Global fog settings don't require specific userObject or manager references for initialization.
    }

    public void Activate()
    {
        if (!ApplyUnderwaterFog) return;

        // Store current global fog settings
        _originalFogEnabled = RenderSettings.fog;
        _originalFogColor = RenderSettings.fogColor;
        _originalFogDensity = RenderSettings.fogDensity;
        _originalFogMode = RenderSettings.fogMode;

        // Apply underwater fog settings
        RenderSettings.fog = true;
        RenderSettings.fogColor = UnderwaterFogColor;
        RenderSettings.fogDensity = UnderwaterFogDensity;
        RenderSettings.fogMode = UnderwaterFogMode;
        Debug.Log("FogUnderwaterEffect: Activated underwater fog.");
    }

    public void Deactivate()
    {
        if (!ApplyUnderwaterFog) return;

        // Restore original fog settings
        RenderSettings.fog = _originalFogEnabled;
        RenderSettings.fogColor = _originalFogColor;
        RenderSettings.fogDensity = _originalFogDensity;
        RenderSettings.fogMode = _originalFogMode;
        Debug.Log("FogUnderwaterEffect: Deactivated underwater fog.");
    }

    public void UpdateEffect()
    {
        // Fog settings typically don't require per-frame updates unless animated.
    }
}

/// <summary>
/// Manages a PostProcessVolume to apply visual distortions, color grading, etc.
/// It switches the PostProcessVolume's profile to an underwater specific one.
/// </summary>
public class PostProcessUnderwaterEffect : IEffect
{
    // References to scene components, assigned by the manager
    private PostProcessVolume _postProcessVolume;
    private PostProcessProfile _underwaterProfile;

    // Store original PostProcessVolume state
    private PostProcessProfile _originalProfile;
    private bool _originalVolumeEnabledState;

    // Configuration (set by the manager)
    public PostProcessVolume TargetPostProcessVolume;
    public PostProcessProfile UnderwaterPostProcessProfile;

    public void Initialize(GameObject userObject, UnderwaterEffectManager manager)
    {
        _postProcessVolume = TargetPostProcessVolume;
        _underwaterProfile = UnderwaterPostProcessProfile;

        if (_postProcessVolume == null)
        {
            Debug.LogError("PostProcessUnderwaterEffect: Target PostProcessVolume reference is missing.", manager);
            return;
        }
        if (_underwaterProfile == null)
        {
            Debug.LogError("PostProcessUnderwaterEffect: Underwater PostProcessProfile reference is missing.", manager);
            return;
        }

        // Cache original state
        _originalVolumeEnabledState = _postProcessVolume.enabled;
        _originalProfile = _postProcessVolume.profile;
    }

    public void Activate()
    {
        if (_postProcessVolume == null || _underwaterProfile == null) return;

        _postProcessVolume.profile = _underwaterProfile;
        _postProcessVolume.enabled = true; // Ensure the volume is active
        Debug.Log("PostProcessUnderwaterEffect: Activated underwater post-processing.");
    }

    public void Deactivate()
    {
        if (_postProcessVolume == null) return;

        // Restore original profile and enabled state
        _postProcessVolume.profile = _originalProfile;
        _postProcessVolume.enabled = _originalVolumeEnabledState;
        Debug.Log("PostProcessUnderwaterEffect: Deactivated underwater post-processing.");
    }

    public void UpdateEffect()
    {
        // Post-processing parameters usually don't need per-frame updates unless animated.
    }
}

/// <summary>
/// Muffles global audio and can play an ambient underwater loop sound.
/// Affects the global AudioListener volume.
/// </summary>
public class AudioMuffleEffect : IEffect
{
    private AudioListener _audioListener; // The AudioListener on the player/camera
    private AudioSource _underwaterLoopAudioSource; // Dedicated AudioSource for ambient sound

    // Store original audio volume
    private float _originalAudioVolume;

    // Configuration (set by the manager)
    public float MuffledVolume = 0.3f;
    public AudioClip UnderwaterLoopSound;
    public AudioSource TargetUnderwaterLoopAudioSource;

    public void Initialize(GameObject userObject, UnderwaterEffectManager manager)
    {
        if (userObject != null)
        {
            _audioListener = userObject.GetComponent<AudioListener>();
            if (_audioListener == null)
            {
                Debug.LogWarning("AudioMuffleEffect: No AudioListener found on userObject. Audio muffling may not work as expected.", userObject);
            }
        }

        _underwaterLoopAudioSource = TargetUnderwaterLoopAudioSource;
        if (UnderwaterLoopSound != null && _underwaterLoopAudioSource == null)
        {
             Debug.LogWarning("AudioMuffleEffect: Underwater loop sound provided, but no AudioSource assigned for it. Assign a TargetUnderwaterLoopAudioSource.", manager);
        }
        else if (_underwaterLoopAudioSource != null)
        {
            _underwaterLoopAudioSource.loop = true;
            _underwaterLoopAudioSource.playOnAwake = false;
        }
    }

    public void Activate()
    {
        if (_audioListener != null)
        {
            _originalAudioVolume = AudioListener.volume;
            AudioListener.volume = MuffledVolume;
            Debug.Log("AudioMuffleEffect: Audio muffled.");
        }
        if (UnderwaterLoopSound != null && _underwaterLoopAudioSource != null)
        {
            _underwaterLoopAudioSource.clip = UnderwaterLoopSound;
            if (!_underwaterLoopAudioSource.isPlaying)
            {
                _underwaterLoopAudioSource.Play();
            }
            Debug.Log("AudioMuffleEffect: Underwater ambient sound playing.");
        }
    }

    public void Deactivate()
    {
        if (_audioListener != null)
        {
            AudioListener.volume = _originalAudioVolume;
            Debug.Log("AudioMuffleEffect: Audio unmuffled.");
        }
        if (_underwaterLoopAudioSource != null && _underwaterLoopAudioSource.isPlaying)
        {
            _underwaterLoopAudioSource.Stop();
            Debug.Log("AudioMuffleEffect: Underwater ambient sound stopped.");
        }
    }

    public void UpdateEffect()
    {
        // No per-frame updates needed for simple muffling or loop playback.
    }
}

/// <summary>
/// Activates/deactivates a Light component (e.g., a spotlight configured to project caustics).
/// </summary>
public class CausticsLightEffect : IEffect
{
    private Light _causticsLight;

    // Store original light enabled state
    private bool _originalLightEnabledState;

    // Configuration (set by the manager)
    public Light TargetCausticsLight;

    public void Initialize(GameObject userObject, UnderwaterEffectManager manager)
    {
        _causticsLight = TargetCausticsLight;
        if (_causticsLight == null)
        {
            Debug.LogError("CausticsLightEffect: Target Caustics Light reference is missing.", manager);
            return;
        }
        _originalLightEnabledState = _causticsLight.enabled;
    }

    public void Activate()
    {
        if (_causticsLight == null) return;
        _causticsLight.enabled = true;
        Debug.Log("CausticsLightEffect: Caustics light activated.");
    }

    public void Deactivate()
    {
        if (_causticsLight == null) return;
        // Restore original enabled state, or force off if desired (e.g., if it was always off)
        _causticsLight.enabled = _originalLightEnabledState;
        Debug.Log("CausticsLightEffect: Caustics light deactivated.");
    }

    public void UpdateEffect()
    {
        // For animated caustics (e.g., texture scroll on a projector), update logic would go here.
        // For a simple light toggle, no per-frame update is needed.
    }
}

/// <summary>
/// Manages an underwater particle system (e.g., for bubbles).
/// Activates/deactivates the particle system.
/// </summary>
public class ParticleBubbleEffect : IEffect
{
    private ParticleSystem _bubbleParticleSystem;

    // Store original particle system active state
    private bool _originalParticleSystemActiveState;

    // Configuration (set by the manager)
    public ParticleSystem TargetBubbleParticleSystem;

    public void Initialize(GameObject userObject, UnderwaterEffectManager manager)
    {
        _bubbleParticleSystem = TargetBubbleParticleSystem;
        if (_bubbleParticleSystem == null)
        {
            Debug.LogError("ParticleBubbleEffect: Target Bubble Particle System reference is missing.", manager);
            return;
        }
        _originalParticleSystemActiveState = _bubbleParticleSystem.gameObject.activeSelf;
    }

    public void Activate()
    {
        if (_bubbleParticleSystem == null) return;
        _bubbleParticleSystem.gameObject.SetActive(true); // Ensure GameObject is active
        if (!_bubbleParticleSystem.isPlaying)
        {
            _bubbleParticleSystem.Play();
        }
        Debug.Log("ParticleBubbleEffect: Bubble particle system activated.");
    }

    public void Deactivate()
    {
        if (_bubbleParticleSystem == null) return;
        _bubbleParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _bubbleParticleSystem.gameObject.SetActive(_originalParticleSystemActiveState); // Restore original active state
        Debug.Log("ParticleBubbleEffect: Bubble particle system deactivated.");
    }

    public void UpdateEffect()
    {
        // Particle systems typically manage their own updates once started,
        // so no specific logic is usually needed here unless custom animation is required.
    }
}


// ==============================================================================================
// SECTION 3: The UnderwaterEffectManager (Facade & Context for Strategies)
// ==============================================================================================
// This is the core of the 'UnderwaterEffectSystem' pattern.
// It acts as a Facade, providing a simplified interface (EnterWater(), ExitWater())
// to a complex subsystem of various visual and audio effects.
// It also serves as the 'Context' for the IEffect 'Strategies', managing their lifecycle and state.
// It detects when the 'playerTransform' enters/exits the water and orchestrates the effects.

public class UnderwaterEffectManager : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("The Transform that represents the 'user' (e.g., Main Camera or Player). " +
             "Its Y-position relative to the water surface determines if effects are active.")]
    public Transform playerTransform;

    [Tooltip("The Y-coordinate of the water surface. Effects activate when player is below this.")]
    public float waterSurfaceY = 0f;

    [Tooltip("How far (in Unity units) below the water surface the player must be " +
             "for effects to fully activate. This prevents flickering near the surface.")]
    public float submergenceThreshold = 0.5f;

    [Header("Effect References (Assign in Inspector)")]
    // These SerializedField references are used to configure the individual IEffect instances.
    public PostProcessVolume globalPostProcessVolume;       // Scene's main PP Volume
    public PostProcessProfile underwaterPostProcessProfile; // Specific profile for underwater PP
    public Light causticsLight;                             // A Light component for caustics projection
    public AudioSource underwaterLoopAudioSource;           // AudioSource for ambient underwater sound
    public ParticleSystem bubbleParticleSystem;             // A particle system for underwater bubbles

    // Internal list to hold all the concrete IEffect implementations (strategies)
    private List<IEffect> _effects = new List<IEffect>();
    private bool _isUnderwater = false;

    // Optional: An event to notify other systems about the underwater state change.
    // This allows decoupling other game systems from direct dependency on the manager.
    public static event Action<bool> OnUnderwaterStateChanged;

    void Awake()
    {
        // --- 1. Validate crucial references ---
        if (playerTransform == null)
        {
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform; // Default to main camera
            }
            else
            {
                Debug.LogError("UnderwaterEffectManager: No playerTransform assigned and no Main Camera found! Effects will not trigger.", this);
                enabled = false; // Disable script if essential references are missing
                return;
            }
        }

        // --- 2. Instantiate and Configure Concrete Effect Implementations ---
        // This is where the manager sets up its internal "strategy" objects.
        // Each effect class is instantiated and its public configuration fields are set
        // using the references exposed in the Unity Inspector on this manager.

        _effects.Add(new FogUnderwaterEffect()); // Fog effect instantiated with default settings

        if (globalPostProcessVolume != null && underwaterPostProcessProfile != null)
        {
            _effects.Add(new PostProcessUnderwaterEffect
            {
                TargetPostProcessVolume = globalPostProcessVolume,
                UnderwaterPostProcessProfile = underwaterPostProcessProfile
            });
        }
        else
        {
            Debug.LogWarning("UnderwaterEffectManager: Post-Process Volume or Underwater Profile missing. Post-processing effects will not be applied.", this);
        }

        // The AudioListener is usually on the Camera (which is the playerTransform).
        _effects.Add(new AudioMuffleEffect
        {
            TargetUnderwaterLoopAudioSource = underwaterLoopAudioSource // Configured via inspector
        });

        if (causticsLight != null)
        {
            _effects.Add(new CausticsLightEffect
            {
                TargetCausticsLight = causticsLight
            });
        }
        else
        {
            Debug.LogWarning("UnderwaterEffectManager: Caustics Light reference missing. Caustics effect will not be applied.", this);
        }

        if (bubbleParticleSystem != null)
        {
            _effects.Add(new ParticleBubbleEffect
            {
                TargetBubbleParticleSystem = bubbleParticleSystem
            });
        }
        else
        {
            Debug.LogWarning("UnderwaterEffectManager: Bubble Particle System reference missing. Bubble effect will not be applied.", this);
        }

        // --- 3. Initialize all effects ---
        // Pass the playerTransform's GameObject (which might contain AudioListener, Camera, etc.)
        // and a reference to this manager (if effects need to call back or access manager state).
        foreach (var effect in _effects)
        {
            effect.Initialize(playerTransform.gameObject, this);
        }
    }

    void Start()
    {
        // Perform an initial check in Start to ensure effects are correctly set up
        // if the player starts underwater.
        CheckUnderwaterState();
    }

    void Update()
    {
        // Continuously check the underwater state based on player's Y position.
        // This is robust and works even without dedicated water trigger colliders.
        CheckUnderwaterState();

        // If currently underwater, tell all active effects to update themselves.
        if (_isUnderwater)
        {
            foreach (var effect in _effects)
            {
                effect.UpdateEffect();
            }
        }
    }

    /// <summary>
    /// Determines if the player is currently underwater based on Y position and threshold.
    /// </summary>
    private void CheckUnderwaterState()
    {
        if (playerTransform == null) return;

        // Player is underwater if their Y position is below the water surface Y,
        // accounting for the submergence threshold.
        bool currentlyUnderwater = playerTransform.position.y < waterSurfaceY - submergenceThreshold;

        if (currentlyUnderwater && !_isUnderwater)
        {
            EnterWater();
        }
        else if (!currentlyUnderwater && _isUnderwater)
        {
            ExitWater();
        }
    }

    /// <summary>
    /// Public Facade method: Activates all registered underwater effects.
    /// This is the primary entry point for other systems to trigger the underwater state.
    /// </summary>
    public void EnterWater()
    {
        if (_isUnderwater) return; // Already underwater, prevent re-triggering
        _isUnderwater = true;
        Debug.Log("<color=cyan>UnderwaterEffectManager: Entering water.</color>");

        foreach (var effect in _effects)
        {
            effect.Activate();
        }

        // Notify subscribers that the state has changed
        OnUnderwaterStateChanged?.Invoke(true);
    }

    /// <summary>
    /// Public Facade method: Deactivates all registered underwater effects and restores original settings.
    /// This is the primary entry point for other systems to exit the underwater state.
    /// </summary>
    public void ExitWater()
    {
        if (!_isUnderwater) return; // Already above water, prevent re-triggering
        _isUnderwater = false;
        Debug.Log("<color=green>UnderwaterEffectManager: Exiting water.</color>");

        foreach (var effect in _effects)
        {
            effect.Deactivate();
        }

        // Notify subscribers that the state has changed
        OnUnderwaterStateChanged?.Invoke(false);
    }

    // You could optionally use Unity's OnTriggerEnter/Exit events on a water volume collider
    // to trigger EnterWater/ExitWater calls, but the Y-position check is often more robust
    // for camera-based effects. If using triggers, the water collider would usually have
    // its own script to call these methods on the manager.
}
```

---

### **How to Use This in Unity:**

1.  **Create a New C# Script:** Name it `UnderwaterEffectSystem.cs` and copy the entire code above into it.
2.  **Create a Manager GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., `_GameManagers`).
    *   Create a child empty GameObject named `UnderwaterEffectManager`.
    *   Attach the `UnderwaterEffectSystem.cs` script to this `UnderwaterEffectManager` GameObject.

3.  **Scene Setup - Core Components:**

    *   **Player/Camera:** Ensure you have a main camera (or player character) in your scene. This will be your `playerTransform`. If you don't assign `playerTransform` in the inspector, the script will try to find `Camera.main`.
    *   **Water Surface:** Create a simple plane or a custom water mesh. Note its Y-position. This will be your `waterSurfaceY`.
    *   **Set `waterSurfaceY`:** On the `UnderwaterEffectManager` GameObject, adjust the `Water Surface Y` field in the Inspector to match the Y-coordinate of your water plane.
    *   **Set `Submergence Threshold`:** This determines how far into the water the `playerTransform` must go before effects activate. A value like `0.5` means the player's pivot must be 0.5 units below `waterSurfaceY`.

4.  **Scene Setup - Specific Effects:**

    *   **Fog:** `RenderSettings.fog` must be enabled (`Window > Rendering > Lighting > Environment` tab, check `Fog`). The script will toggle this and change properties.
    *   **Post-Processing:**
        *   Install the `Post Processing` package (`Window > Package Manager > Unity Registry > Post Processing`).
        *   Create a Post-Process Volume: `GameObject > 3D Object > Post-process Volume`.
        *   Set its `Is Global` property to `true`.
        *   Create two `Post-process Profile` assets (`Assets > Create > Post-process Profile`):
            *   One for `Default` (e.g., just basic AA).
            *   One for `Underwater` (e.g., add `Color Grading` for a greenish tint, `Depth of Field` for blur, `Vignette`).
        *   Assign the **Global Post Process Volume** and **Underwater Post Process Profile** to the `UnderwaterEffectManager` in the Inspector. The `Default` profile can remain on your global volume, the script will temporarily switch it.
    *   **Audio Muffling & Loop:**
        *   On your `playerTransform` (or a child of it), ensure you have an `AudioListener` component.
        *   Create an empty GameObject as a child of your `playerTransform` (e.g., `UnderwaterAudio`). Add an `AudioSource` component to it. This will be your `Underwater Loop Audio Source`.
        *   Find or create an `AudioClip` for ambient underwater sounds (e.g., subtle bubbles, distant ocean hum). Assign it to `Underwater Loop Sound` on the `UnderwaterEffectManager`.
    *   **Caustics Light:**
        *   Create a `Light` GameObject (`GameObject > Light > Spot Light`). Position it above your water.
        *   Add a `Light Cookie` (a texture to project the caustics pattern) to its `Light` component. You'll need to create or find a caustics texture.
        *   Assign this `Light` to the `Caustics Light` field on the `UnderwaterEffectManager`.
    *   **Bubble Particles:**
        *   Create a `Particle System` (`GameObject > Effects > Particle System`).
        *   Configure it to emit small, slow-moving particles upwards to simulate bubbles. Adjust its position to be relative to your `playerTransform` so it follows the camera underwater.
        *   Initially, disable its GameObject (or stop the particle system).
        *   Assign this `Particle System` to the `Bubble Particle System` field on the `UnderwaterEffectManager`.

5.  **Run the Scene:** Move your `playerTransform` (or camera) up and down across the `waterSurfaceY`. You should observe the effects activating when you go below the surface and deactivating when you resurface.

This setup provides a clear, modular, and extensible system for managing complex underwater effects in Unity, following a common design pattern approach.