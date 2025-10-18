// Unity Design Pattern Example: SandstormSystem
// This script demonstrates the SandstormSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SandstormSystem' design pattern, as envisioned here, addresses the need to manage **complex, dynamic, and widespread environmental effects** within a Unity game. It combines several established patterns to achieve a robust and extensible system:

1.  **Singleton:** Ensures there is a single, globally accessible instance of the core `SandstormSystem` manager. This makes it easy for any part of your game to query the sandstorm's state or trigger a sandstorm event.
2.  **Observer/Publisher-Subscriber:** The `SandstormSystem` acts as a **publisher**, broadcasting events (e.g., "Sandstorm Started," "Sandstorm Updated," "Sandstorm Ended"). Various effect components (e.g., visual, audio, gameplay debuffs) act as **subscribers** or **observers**, reacting to these events without the core system needing to know about them directly. This decouples the core logic from its effects, making the system highly modular and extensible.
3.  **State Machine:** The `SandstormSystem` manages the sandstorm's lifecycle through distinct states (Inactive, RampingUp, Active, RampingDown), ensuring predictable and smooth transitions of effects.
4.  **Strategy/Component-based (for Effects):** Individual effects (visuals, audio, player debuffs) are implemented as separate `MonoBehaviour` components that implement a common `ISandstormEffect` interface. This allows you to easily add new types of effects, modify existing ones, or enable/disable them independently.

---

## 1. Core Sandstorm System Files

These files define the central manager, the properties for a sandstorm, and the interface for effects.

### `SandstormSystem/SandstormProperties.cs`

This struct defines the configurable parameters for a sandstorm event.

```csharp
// SandstormSystem/SandstormProperties.cs
using UnityEngine;
using System;

namespace SandstormSystemExample
{
    /// <summary>
    /// Represents the properties of a specific sandstorm event.
    /// This struct makes it easy to define different types or intensities of sandstorms
    /// and pass them around the system.
    /// </summary>
    [Serializable]
    public struct SandstormProperties
    {
        [Tooltip("The maximum intensity the sandstorm will reach (0.0 to 1.0).")]
        [Range(0f, 1f)] public float MaxIntensity;

        [Tooltip("The total duration the sandstorm will be active (in seconds), including ramp-up and ramp-down.")]
        public float TotalDuration;

        [Tooltip("How long it takes for the sandstorm to reach MaxIntensity (in seconds).")]
        public float RampUpDuration;

        [Tooltip("How long it takes for the sandstorm to fully dissipate (in seconds).")]
        public float RampDownDuration;

        [Tooltip("Damage applied per second to affected entities when active.")]
        public float DamagePerSecond;

        [Tooltip("The color tint for fog/skybox during the sandstorm.")]
        public Color FogColor;

        [Tooltip("The density of the fog during the sandstorm (0.0 to 1.0).")]
        [Range(0f, 1f)] public float FogDensity;

        [Tooltip("The wind force magnitude that might affect physics objects or particles.")]
        public float WindForceMagnitude;

        [Tooltip("Visibility reduction factor (e.g., for player cameras or UI effects).")]
        [Range(0f, 1f)] public float VisibilityReductionFactor;

        /// <summary>
        /// Provides a set of default properties for a typical sandstorm.
        /// </summary>
        public static SandstormProperties Default => new SandstormProperties
        {
            MaxIntensity = 0.8f,
            TotalDuration = 120f, // 2 minutes
            RampUpDuration = 10f,
            RampDownDuration = 20f,
            DamagePerSecond = 5f,
            FogColor = new Color(0.7f, 0.6f, 0.4f, 1f),
            FogDensity = 0.08f,
            WindForceMagnitude = 10f,
            VisibilityReductionFactor = 0.5f
        };
    }
}
```

### `SandstormSystem/ISandstormEffect.cs`

This interface defines the contract for any component that wants to react to the sandstorm's state.

```csharp
// SandstormSystem/ISandstormEffect.cs
using UnityEngine;

namespace SandstormSystemExample
{
    /// <summary>
    /// Interface for components that want to react to the SandstormSystem's state changes.
    /// This is part of the Observer pattern: components implement this to 'observe' the sandstorm.
    /// By implementing this, a component can subscribe to the SandstormSystem's events
    /// and define its specific behavior during different phases of the sandstorm.
    /// </summary>
    public interface ISandstormEffect
    {
        /// <summary>
        /// Called when the SandstormSystem officially starts a new sandstorm (begins ramping up).
        /// </summary>
        /// <param name="properties">The properties of the newly started sandstorm.</param>
        void OnSandstormStart(SandstormProperties properties);

        /// <summary>
        /// Called every frame (or periodically) while the sandstorm is active,
        /// providing its current intensity. Effect components should update their
        /// state based on this intensity.
        /// </summary>
        /// <param name="currentIntensity">The current normalized intensity of the sandstorm (0 to 1).</param>
        void OnSandstormUpdate(float currentIntensity);

        /// <summary>
        /// Called when the sandstorm fully dissipates and becomes inactive.
        /// Effect components should use this to reset to their default state.
        /// </summary>
        void OnSandstormEnd();
    }
}
```

### `SandstormSystem/SandstormSystem.cs`

The central manager that orchestrates the sandstorm's lifecycle and broadcasts its state changes.

```csharp
// SandstormSystem/SandstormSystem.cs
using UnityEngine;
using System;
// No explicit System.Collections is needed here, as the observer pattern uses C# events.

namespace SandstormSystemExample
{
    /// <summary>
    /// Enum to define the current state of the sandstorm system.
    /// </summary>
    public enum SandstormState
    {
        Inactive,      // No sandstorm is currently active or pending.
        RampingUp,     // Sandstorm is increasing in intensity.
        Active,        // Sandstorm is at its peak or sustained intensity.
        RampingDown    // Sandstorm is decreasing in intensity and dissipating.
    }

    /// <summary>
    /// The core SandstormSystem manager. This is a Singleton MonoBehaviour that orchestrates
    /// the sandstorm's lifecycle and broadcasts its state changes to other components.
    ///
    /// This 'SandstormSystem' pattern interpretation effectively combines:
    /// 1.  **Singleton:** Ensures a single, globally accessible instance for easy management and access.
    /// 2.  **Observer/Publisher-Subscriber:** Uses C# events to notify various effect components
    ///     (subscribers) about the sandstorm's state, without needing to know about them directly.
    ///     This promotes loose coupling and easy extensibility.
    /// 3.  **State Machine:** Manages the sandstorm's progression through different phases
    ///     (ramp-up, active, ramp-down) for smooth and controlled transitions.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensures this script runs before most other game logic scripts.
    public class SandstormSystem : MonoBehaviour
    {
        // --- Singleton Implementation ---
        // Provides a static, globally accessible instance of the SandstormSystem.
        public static SandstormSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // If another instance already exists, destroy this duplicate.
                Debug.LogWarning("SandstormSystem: Duplicate instance found! Destroying this one.", this);
                Destroy(gameObject);
            }
            else
            {
                // Set this instance as the singleton and prevent it from being destroyed on scene load.
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
        }

        private void Initialize()
        {
            _currentState = SandstormState.Inactive;
            _currentIntensity = 0f;
            _elapsedTime = 0f;
            Debug.Log("SandstormSystem Initialized.");
        }

        // --- Sandstorm State Variables ---
        private SandstormState _currentState = SandstormState.Inactive;
        public SandstormState CurrentState => _currentState; // Public getter for the current state.

        private float _currentIntensity = 0f;
        public float CurrentIntensity => _currentIntensity; // Public getter for the current intensity (normalized 0-1).

        private SandstormProperties _activeProperties; // Properties of the currently active sandstorm.
        public SandstormProperties ActiveProperties => _activeProperties; // Public getter for active properties.

        private float _elapsedTime = 0f; // Time since the current sandstorm started.

        // --- Event Declarations (Publisher) ---
        // These events are the core of the Observer pattern. Any component (an 'Observer')
        // interested in the sandstorm's status can subscribe to these events.

        /// <summary>
        /// Event fired when a sandstorm officially begins (enters RampingUp state).
        /// Passes the <see cref="SandstormProperties"/> that define this sandstorm.
        /// </summary>
        public event Action<SandstormProperties> OnSandstormStarted;

        /// <summary>
        /// Event fired every frame while the sandstorm is active (RampingUp, Active, RampingDown states).
        /// Passes the current intensity (normalized 0-1), allowing effects to scale dynamically.
        /// </summary>
        public event Action<float> OnSandstormUpdated;

        /// <summary>
        /// Event fired when the sandstorm fully dissipates and becomes Inactive.
        /// Observers should use this to reset to their default states.
        /// </summary>
        public event Action OnSandstormEnded;

        // --- Public Methods ---

        /// <summary>
        /// Initiates a sandstorm with the given properties.
        /// If a sandstorm is already active, it will be immediately stopped and replaced by the new one.
        /// </summary>
        /// <param name="properties">The <see cref="SandstormProperties"/> defining this sandstorm event.</param>
        public void StartSandstorm(SandstormProperties properties)
        {
            if (_currentState != SandstormState.Inactive)
            {
                Debug.LogWarning("SandstormSystem: A sandstorm is already active. Stopping current one and starting new.", this);
                StopSandstormImmediate(); // Clean up current storm before starting a new one.
            }

            _activeProperties = properties;
            _currentState = SandstormState.RampingUp;
            _elapsedTime = 0f;
            _currentIntensity = 0f; // Sandstorm starts with 0 intensity.
            Debug.Log($"SandstormSystem: Starting sandstorm with MaxIntensity: {_activeProperties.MaxIntensity}, Duration: {_activeProperties.TotalDuration}s", this);

            // Notify all subscribed observers that a sandstorm has started.
            OnSandstormStarted?.Invoke(_activeProperties);
        }

        /// <summary>
        /// Forces the currently active sandstorm to transition into the RampingDown state.
        /// The sandstorm will then gradually dissipate according to its RampDownDuration.
        /// </summary>
        public void StopSandstorm()
        {
            if (_currentState != SandstormState.Inactive && _currentState != SandstormState.RampingDown)
            {
                Debug.Log($"SandstormSystem: Forcing sandstorm to dissipate.", this);
                _currentState = SandstormState.RampingDown;
                // Optionally, one could adjust _elapsedTime here to immediately jump to the ramp-down phase
                // or start a new ramp-down timer. For simplicity, we'll let it naturally continue.
            }
            else if (_currentState == SandstormState.Inactive)
            {
                Debug.Log("SandstormSystem: No sandstorm is active to stop.", this);
            }
        }

        /// <summary>
        /// Instantly stops the sandstorm and resets its state to Inactive.
        /// This method is primarily for internal cleanup, immediate termination, or edge cases.
        /// </summary>
        private void StopSandstormImmediate()
        {
            if (_currentState != SandstormState.Inactive)
            {
                Debug.Log("SandstormSystem: Instantly stopping sandstorm and resetting.", this);
                _currentState = SandstormState.Inactive;
                _currentIntensity = 0f;
                _elapsedTime = 0f;
                OnSandstormEnded?.Invoke(); // Notify observers that it ended abruptly.
            }
        }

        // --- Update Loop (State Machine Logic) ---
        private void Update()
        {
            if (_currentState == SandstormState.Inactive)
            {
                return; // Nothing to do if no sandstorm is active.
            }

            _elapsedTime += Time.deltaTime; // Advance the sandstorm's internal timer.

            float prevIntensity = _currentIntensity; // Store previous intensity for change detection.

            switch (_currentState)
            {
                case SandstormState.RampingUp:
                    if (_elapsedTime < _activeProperties.RampUpDuration)
                    {
                        // Lerp intensity from 0 to MaxIntensity over RampUpDuration.
                        _currentIntensity = Mathf.Lerp(0f, _activeProperties.MaxIntensity, _elapsedTime / _activeProperties.RampUpDuration);
                    }
                    else
                    {
                        // Ramp-up complete, transition to Active state.
                        _currentIntensity = _activeProperties.MaxIntensity;
                        _currentState = SandstormState.Active;
                        _elapsedTime = _activeProperties.RampUpDuration; // Reset elapsed time for active phase calculation
                        Debug.Log("SandstormSystem: Sandstorm reached peak intensity.", this);
                    }
                    break;

                case SandstormState.Active:
                    // Calculate the duration for which the sandstorm should remain at peak intensity.
                    float activeDuration = _activeProperties.TotalDuration - _activeProperties.RampUpDuration - _activeProperties.RampDownDuration;
                    if (activeDuration < 0) activeDuration = 0; // Guard against negative active duration if total is too short.

                    if (_elapsedTime - _activeProperties.RampUpDuration < activeDuration)
                    {
                        // Maintain peak intensity during the active phase.
                        _currentIntensity = _activeProperties.MaxIntensity;
                    }
                    else
                    {
                        // Active phase complete, transition to RampingDown state.
                        _currentState = SandstormState.RampingDown;
                        _elapsedTime = _activeProperties.RampUpDuration + activeDuration; // Adjust elapsed time for ramp-down phase start.
                        Debug.Log("SandstormSystem: Sandstorm starting to dissipate.", this);
                    }
                    break;

                case SandstormState.RampingDown:
                    float rampDownStartIntensity = _activeProperties.MaxIntensity;
                    // Calculate time elapsed since the ramp-down phase began.
                    float timeIntoRampDown = _elapsedTime - (_activeProperties.TotalDuration - _activeProperties.RampDownDuration);

                    if (timeIntoRampDown < _activeProperties.RampDownDuration)
                    {
                        // Lerp intensity from MaxIntensity down to 0 over RampDownDuration.
                        _currentIntensity = Mathf.Lerp(rampDownStartIntensity, 0f, timeIntoRampDown / _activeProperties.RampDownDuration);
                    }
                    else
                    {
                        // Ramp-down complete, transition to Inactive state.
                        _currentIntensity = 0f;
                        _currentState = SandstormState.Inactive;
                        Debug.Log("SandstormSystem: Sandstorm fully dissipated.", this);
                        OnSandstormEnded?.Invoke(); // Notify all subscribed observers that the sandstorm has ended.
                    }
                    break;
            }

            // Optimization: Only invoke OnSandstormUpdated if the intensity has changed
            // significantly, to avoid unnecessary updates for observers.
            if (_currentState != SandstormState.Inactive && Mathf.Abs(_currentIntensity - prevIntensity) > 0.001f)
            {
                OnSandstormUpdated?.Invoke(_currentIntensity);
            }
        }

        // Optional: Gizmo for editor visualization to see the sandstorm's approximate "reach".
        private void OnDrawGizmos()
        {
            if (_currentState != SandstormState.Inactive)
            {
                Gizmos.color = _activeProperties.FogColor;
                // Draw a sphere whose radius scales with current intensity.
                Gizmos.DrawWireSphere(transform.position, _currentIntensity * 100f);
                // Display current state and intensity in the editor view.
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                    $"Sandstorm: {_currentState}\nIntensity: {_currentIntensity:F2}");
                #endif
            }
        }
    }
}
```

---

## 2. Example Effect Implementations

These scripts demonstrate how different game systems can act as **observers** and react to the `SandstormSystem`'s events.

### `SandstormEffects/SandstormVisualEffect.cs`

Manages visual aspects like fog, particle systems, and potential post-processing.

```csharp
// SandstormEffects/SandstormVisualEffect.cs
using UnityEngine;
using System.Collections; // For coroutines

namespace SandstormSystemExample
{
    /// <summary>
    /// An example implementation of <see cref="ISandstormEffect"/> that manages visual aspects
    /// like fog, particle systems, and potentially skybox or post-processing changes.
    ///
    /// This component subscribes to the <see cref="SandstormSystem"/>'s events to react to its state.
    /// </summary>
    public class SandstormVisualEffect : MonoBehaviour, ISandstormEffect
    {
        [Header("Fog Settings")]
        [Tooltip("The default fog color of the scene (used for lerping from/to).")]
        public Color DefaultFogColor = Color.white;
        [Tooltip("The default fog density of the scene (used for lerping from/to).")]
        [Range(0f, 0.1f)] public float DefaultFogDensity = 0.01f;

        [Header("Particle System")]
        [Tooltip("A Particle System to activate/deactivate during the sandstorm.")]
        public ParticleSystem sandstormParticles;

        private Color _initialSceneFogColor;
        private float _initialSceneFogDensity;
        private bool _initialFogEnabled;

        private void OnEnable()
        {
            // Subscribe to SandstormSystem events. This is how this component
            // 'observes' the sandstorm's state without the SandstormSystem knowing about it directly.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted += OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated += OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded += OnSandstormEnd;
                Debug.Log($"{GetType().Name} subscribed to SandstormSystem events.");
            }
            else
            {
                Debug.LogError("SandstormSystem instance not found! Visual effect won't work.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks and ensure clean shutdown.
            // This is crucial for proper Observer pattern implementation.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted -= OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated -= OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded -= OnSandstormEnd;
                Debug.Log($"{GetType().Name} unsubscribed from SandstormSystem events.");
            }
            // Ensure visuals are reset if the component is disabled or destroyed.
            RestoreDefaultVisuals();
        }

        private void Start()
        {
            // Store initial scene fog settings to restore them later.
            _initialSceneFogColor = RenderSettings.fogColor;
            _initialSceneFogDensity = RenderSettings.fogDensity;
            _initialFogEnabled = RenderSettings.fog;

            // Initialize particle system state to ensure it's off initially.
            if (sandstormParticles != null)
            {
                sandstormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormStart"/>.
        /// Activates sandstorm-specific visual effects.
        /// </summary>
        /// <param name="properties">The properties of the started sandstorm.</param>
        public void OnSandstormStart(SandstormProperties properties)
        {
            Debug.Log($"{GetType().Name}: Sandstorm starting. Activating visuals.");

            // Enable scene fog if it's not already, or if we want to force it during the sandstorm.
            RenderSettings.fog = true;

            if (sandstormParticles != null)
            {
                sandstormParticles.Play(); // Start playing the sandstorm particles.
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormUpdate"/>.
        /// Adjusts visual effects based on the sandstorm's current intensity.
        /// </summary>
        /// <param name="currentIntensity">The current normalized intensity (0 to 1).</param>
        public void OnSandstormUpdate(float currentIntensity)
        {
            // Get the active properties from the SandstormSystem to use in calculations.
            SandstormProperties properties = SandstormSystem.Instance.ActiveProperties;

            // Lerp fog color and density based on intensity, blending from default to sandstorm properties.
            RenderSettings.fogColor = Color.Lerp(_initialSceneFogColor, properties.FogColor, currentIntensity);
            RenderSettings.fogDensity = Mathf.Lerp(_initialSceneFogDensity, properties.FogDensity, currentIntensity);

            // Adjust particle system emission rate or other properties based on intensity.
            if (sandstormParticles != null)
            {
                var emission = sandstormParticles.emission;
                // Example: Emission rate scales with intensity, up to 100 particles/sec at max intensity.
                emission.rateOverTime = currentIntensity * 100f;
            }

            // You could also interact with Post-Processing Volumes here:
            // if (postProcessVolume != null) {
            //    // Example: Adjust saturation or add a vignette based on intensity.
            //    ColorGrading cg;
            //    if (postProcessVolume.profile.TryGetSettings(out cg)) {
            //        cg.saturation.value = Mathf.Lerp(0, -100, currentIntensity);
            //    }
            //    Vignette vignette;
            //    if (postProcessVolume.profile.TryGetSettings(out vignette)) {
            //        vignette.intensity.value = currentIntensity * 0.4f;
            //    }
            // }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormEnd"/>.
        /// Restores default scene visuals to their state before the sandstorm.
        /// </summary>
        public void OnSandstormEnd()
        {
            Debug.Log($"{GetType().Name}: Sandstorm ended. Restoring default visuals.");
            RestoreDefaultVisuals();
        }

        /// <summary>
        /// Restores the scene to its initial visual state before the sandstorm,
        /// using a gradual lerp for a smoother transition.
        /// </summary>
        private void RestoreDefaultVisuals()
        {
            // Stop any ongoing fog lerping coroutine before starting a new one.
            StopAllCoroutines();
            // Start a coroutine to smoothly fade fog back to default over 2 seconds.
            StartCoroutine(LerpFogBackToDefault(_initialSceneFogColor, _initialSceneFogDensity, _initialFogEnabled, 2f));

            if (sandstormParticles != null)
            {
                sandstormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop particles.
            }
        }

        /// <summary>
        /// Coroutine to smoothly transition scene fog settings back to their original values.
        /// </summary>
        private IEnumerator LerpFogBackToDefault(Color targetColor, float targetDensity, bool targetEnabled, float duration)
        {
            Color startColor = RenderSettings.fogColor;
            float startDensity = RenderSettings.fogDensity;
            float timer = 0f;

            while (timer < duration)
            {
                RenderSettings.fogColor = Color.Lerp(startColor, targetColor, timer / duration);
                RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }

            // Ensure final values are exactly the target values.
            RenderSettings.fogColor = targetColor;
            RenderSettings.fogDensity = targetDensity;
            RenderSettings.fog = targetEnabled; // Revert fog enabled state.
        }
    }
}
```

### `SandstormEffects/SandstormAudioEffect.cs`

Manages audio aspects like wind sounds and environmental ambience.

```csharp
// SandstormEffects/SandstormAudioEffect.cs
using UnityEngine;

namespace SandstormSystemExample
{
    /// <summary>
    /// An example implementation of <see cref="ISandstormEffect"/> that manages audio aspects
    /// like wind sounds and environmental ambience.
    ///
    /// This component subscribes to the <see cref="SandstormSystem"/>'s events to react to its state.
    /// Requires an <see cref="AudioSource"/> component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SandstormAudioEffect : MonoBehaviour, ISandstormEffect
    {
        [Header("Audio Settings")]
        [Tooltip("The audio clip for the sandstorm wind loop.")]
        public AudioClip sandstormWindClip;
        [Tooltip("Maximum volume for the wind sound (at peak intensity).")]
        [Range(0f, 1f)] public float maxWindVolume = 0.8f;
        [Tooltip("Pitch variation for the wind sound based on intensity.")]
        [Range(0f, 1f)] public float maxWindPitchOffset = 0.2f;

        private AudioSource _audioSource;
        private float _initialAudioSourceVolume;
        private float _initialAudioSourcePitch;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = sandstormWindClip;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0f; // Start silent, volume will be controlled by sandstorm intensity.

            // Store initial volume and pitch in case the AudioSource had pre-set values.
            _initialAudioSourceVolume = _audioSource.volume;
            _initialAudioSourcePitch = _audioSource.pitch;
        }

        private void OnEnable()
        {
            // Subscribe to SandstormSystem events.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted += OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated += OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded += OnSandstormEnd;
                Debug.Log($"{GetType().Name} subscribed to SandstormSystem events.");
            }
            else
            {
                Debug.LogError("SandstormSystem instance not found! Audio effect won't work.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted -= OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated -= OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded -= OnSandstormEnd;
                Debug.Log($"{GetType().Name} unsubscribed from SandstormSystem events.");
            }
            RestoreDefaultAudio(); // Ensure audio is reset if the component is disabled or destroyed.
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormStart"/>.
        /// Starts playing the sandstorm wind sound.
        /// </summary>
        /// <param name="properties">The properties of the started sandstorm.</param>
        public void OnSandstormStart(SandstormProperties properties)
        {
            Debug.Log($"{GetType().Name}: Sandstorm starting. Playing wind audio.");
            if (sandstormWindClip != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormUpdate"/>.
        /// Adjusts wind sound volume and pitch based on intensity.
        /// </summary>
        /// <param name="currentIntensity">The current normalized intensity (0 to 1).</param>
        public void OnSandstormUpdate(float currentIntensity)
        {
            if (_audioSource != null)
            {
                // Lerp volume from initial to max based on intensity.
                _audioSource.volume = Mathf.Lerp(_initialAudioSourceVolume, maxWindVolume, currentIntensity);
                // Lerp pitch slightly to give more dynamic sound based on intensity.
                _audioSource.pitch = Mathf.Lerp(_initialAudioSourcePitch, _initialAudioSourcePitch + maxWindPitchOffset, currentIntensity);
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormEnd"/>.
        /// Stops the sandstorm wind sound and restores default audio settings.
        /// </summary>
        public void OnSandstormEnd()
        {
            Debug.Log($"{GetType().Name}: Sandstorm ended. Stopping wind audio.");
            RestoreDefaultAudio();
        }

        /// <summary>
        /// Restores the audio source to its initial state (stops playing, resets volume/pitch).
        /// </summary>
        private void RestoreDefaultAudio()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.volume = _initialAudioSourceVolume;
                _audioSource.pitch = _initialAudioSourcePitch;
            }
        }
    }
}
```

### `SandstormEffects/SandstormPlayerEffect.cs`

Applies gameplay-related impacts specifically to the player (e.g., damage over time, visibility reduction).

```csharp
// SandstormEffects/SandstormPlayerEffect.cs
using UnityEngine;

namespace SandstormSystemExample
{
    /// <summary>
    /// An example implementation of <see cref="ISandstormEffect"/> that applies gameplay-related impacts
    /// specifically to the player (e.g., damage over time, visibility reduction, movement debuffs).
    ///
    /// This component demonstrates how game logic can react to the sandstorm by observing its events.
    /// </summary>
    public class SandstormPlayerEffect : MonoBehaviour, ISandstormEffect
    {
        [Header("Player References")]
        [Tooltip("Reference to the Player's Health Component.")]
        public PlayerHealth playerHealth; // Replace with your actual player health component.
        [Tooltip("Reference to the Player's Camera for simulating visibility reduction.")]
        public Camera playerCamera; // For simulating visibility reduction.
        // public PlayerMovement playerMovement; // Add this if you want to slow player movement.

        [Header("Effect Settings")]
        [Tooltip("Maximum player visibility reduction (e.g., multiplier for FOV, 0.5 means 50% visibility).")]
        [Range(0f, 1f)] public float maxPlayerVisibilityReduction = 0.5f;

        private float _originalCameraFOV;
        private bool _isEffectActive = false;

        private void Start()
        {
            if (playerCamera != null)
            {
                _originalCameraFOV = playerCamera.fieldOfView;
            }

            // Warn if references are not set, as effects won't apply.
            if (playerHealth == null)
            {
                Debug.LogWarning($"{GetType().Name}: PlayerHealth component not assigned. Damage over time will not function.", this);
            }
            if (playerCamera == null)
            {
                Debug.LogWarning($"{GetType().Name}: Player Camera not assigned. Visibility effects will not function.", this);
            }
        }

        private void OnEnable()
        {
            // Subscribe to SandstormSystem events.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted += OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated += OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded += OnSandstormEnd;
                Debug.Log($"{GetType().Name} subscribed to SandstormSystem events.");
            }
            else
            {
                Debug.LogError("SandstormSystem instance not found! Player effects won't work.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events.
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.OnSandstormStarted -= OnSandstormStart;
                SandstormSystem.Instance.OnSandstormUpdated -= OnSandstormUpdate;
                SandstormSystem.Instance.OnSandstormEnded -= OnSandstormEnd;
                Debug.Log($"{GetType().Name} unsubscribed from SandstormSystem events.");
            }
            RestoreDefaultPlayerEffects(); // Ensure effects are reset if the component is disabled or destroyed.
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormStart"/>.
        /// Marks the player effect as active.
        /// </summary>
        /// <param name="properties">The properties of the started sandstorm.</param>
        public void OnSandstormStart(SandstormProperties properties)
        {
            Debug.Log($"{GetType().Name}: Sandstorm starting. Player will be affected.");
            _isEffectActive = true;
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormUpdate"/>.
        /// Applies damage over time and adjusts player visibility based on intensity.
        /// </summary>
        /// <param name="currentIntensity">The current normalized intensity (0 to 1).</param>
        public void OnSandstormUpdate(float currentIntensity)
        {
            if (!_isEffectActive) return;

            // Get the active properties from the SandstormSystem for damage calculations.
            SandstormProperties properties = SandstormSystem.Instance.ActiveProperties;

            // Apply damage over time, scaled by intensity.
            if (playerHealth != null)
            {
                float damageThisFrame = properties.DamagePerSecond * currentIntensity * Time.deltaTime;
                playerHealth.TakeDamage(damageThisFrame);
            }

            // Adjust player visibility (e.g., by changing camera FOV or applying a blur/vignette post-process).
            if (playerCamera != null)
            {
                // Example: Reduce FOV (tunnel vision effect), scaling with intensity.
                float targetFOV = Mathf.Lerp(_originalCameraFOV, _originalCameraFOV * (1f - maxPlayerVisibilityReduction), currentIntensity);
                playerCamera.fieldOfView = targetFOV;

                // For a more realistic "blurry" or "sand-in-eyes" effect,
                // you would interact with a post-processing stack (e.g., blur, grain, color tint, vignette).
                // Example: PostProcessVolume.profile.GetSetting<Vignette>().intensity.value = currentIntensity * someMaxVignette;
            }

            // You could also apply movement speed debuffs here:
            // if (playerMovement != null) {
            //     float speedMultiplier = Mathf.Lerp(1f, 1f - properties.MovementSlowdownFactor, currentIntensity);
            //     playerMovement.ApplySpeedMultiplier(speedMultiplier);
            // }
        }

        /// <summary>
        /// Implementation of <see cref="ISandstormEffect.OnSandstormEnd"/>.
        /// Restores default player effects.
        /// </summary>
        public void OnSandstormEnd()
        {
            Debug.Log($"{GetType().Name}: Sandstorm ended. Restoring player effects.");
            _isEffectActive = false;
            RestoreDefaultPlayerEffects();
        }

        /// <summary>
        /// Restores the player to their normal state (e.g., camera FOV, removes debuffs).
        /// </summary>
        private void RestoreDefaultPlayerEffects()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = _originalCameraFOV;
            }
            // Reset any other debuffs or visual changes here (e.g., playerMovement.ResetSpeedMultiplier()).
        }
    }

    /// <summary>
    /// A dummy PlayerHealth component for demonstration purposes.
    /// In a real game, this would be your actual player health system.
    /// Attach this to your player GameObject.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Current health of the player.")]
        public float currentHealth = 100f;
        [Tooltip("Maximum health of the player.")]
        public float maxHealth = 100f;

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0f); // Don't go below 0.
            // Debug.Log($"Player took {amount:F2} damage from sandstorm. Current Health: {currentHealth:F2}");

            if (currentHealth <= 0)
            {
                Debug.Log("Player has been defeated by the sandstorm!");
                // Implement player death/respawn logic here.
            }
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth); // Don't exceed max health.
        }
    }
}
```

---

## 3. Example Trigger

This script provides a simple way to start and stop the sandstorm for testing.

### `SandstormTrigger.cs`

Allows manual or automatic starting/stopping of sandstorms.

```csharp
// SandstormTrigger.cs
using UnityEngine;

namespace SandstormSystemExample
{
    /// <summary>
    /// A simple demonstration script to start/stop the <see cref="SandstormSystem"/>.
    /// You can attach this to an empty GameObject in your scene.
    /// This acts as a 'controller' or 'event source' for triggering the sandstorm.
    /// In a real game, sandstorms might be triggered by game events, timers, or player actions.
    /// </summary>
    public class SandstormTrigger : MonoBehaviour
    {
        [Header("Sandstorm Settings")]
        [Tooltip("The properties for the sandstorm to be triggered.")]
        public SandstormProperties sandstormProperties = SandstormProperties.Default;

        [Header("Trigger Controls")]
        [Tooltip("Automatically start the sandstorm when the scene loads.")]
        public bool triggerOnStart = false;
        [Tooltip("Key to manually start the sandstorm.")]
        public KeyCode startKey = KeyCode.Alpha1; // Default: '1' key
        [Tooltip("Key to manually stop the sandstorm.")]
        public KeyCode stopKey = KeyCode.Alpha2; // Default: '2' key

        private void Start()
        {
            if (triggerOnStart)
            {
                StartSandstormButton();
            }
        }

        private void Update()
        {
            // Manual controls for starting and stopping the sandstorm.
            if (Input.GetKeyDown(startKey))
            {
                StartSandstormButton();
            }
            if (Input.GetKeyDown(stopKey))
            {
                StopSandstormButton();
            }

            // Display current state and intensity in the editor's console for easy monitoring.
            // This is for debug and demonstration purposes.
            if (SandstormSystem.Instance != null && SandstormSystem.Instance.CurrentState != SandstormState.Inactive)
            {
                // To avoid spamming the console, only log when state or intensity significantly changes,
                // or use a custom editor window for persistent display. For this example, it logs frequently.
                // Debug.Log($"Current Sandstorm State: {SandstormSystem.Instance.CurrentState}, Intensity: {SandstormSystem.Instance.CurrentIntensity:F2}");
            }
        }

        /// <summary>
        /// Public method to be called by UI buttons or other scripts to start a sandstorm.
        /// </summary>
        public void StartSandstormButton()
        {
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.StartSandstorm(sandstormProperties);
                Debug.Log("SandstormTrigger: Requested to START sandstorm.");
            }
            else
            {
                Debug.LogError("SandstormSystem.Instance is null. Make sure SandstormSystem is present and initialized in the scene.", this);
            }
        }

        /// <summary>
        /// Public method to be called by UI buttons or other scripts to stop a sandstorm.
        /// </summary>
        public void StopSandstormButton()
        {
            if (SandstormSystem.Instance != null)
            {
                SandstormSystem.Instance.StopSandstorm();
                Debug.Log("SandstormTrigger: Requested to STOP sandstorm.");
            }
            else
            {
                Debug.LogError("SandstormSystem.Instance is null. Make sure SandstormSystem is present and initialized in the scene.", this);
            }
        }
    }
}
```

---

## Unity Project Setup and Example Usage

To make this example work in your Unity project, follow these steps:

1.  **Create Folders:**
    *   Create a folder named `SandstormSystemExample` in your project's `Assets` directory.
    *   Inside `SandstormSystemExample`, create subfolders: `SandstormSystem`, `SandstormEffects`.

2.  **Place Scripts:**
    *   Place `SandstormProperties.cs`, `ISandstormEffect.cs`, and `SandstormSystem.cs` into the `SandstormSystem` folder.
    *   Place `SandstormVisualEffect.cs`, `SandstormAudioEffect.cs`, and `SandstormPlayerEffect.cs` into the `SandstormEffects` folder.
    *   Place `SandstormTrigger.cs` (and optionally the `PlayerHealth.cs` helper class from `SandstormPlayerEffect.cs`) anywhere convenient, e.g., in `SandstormSystemExample` root or a `Triggers` folder.

3.  **Scene Setup:**

    *   **Sandstorm System Manager:**
        *   Create an empty GameObject in your scene (Right-click in Hierarchy -> Create Empty).
        *   Rename it to `SandstormSystemManager`.
        *   Drag and drop the `SandstormSystem.cs` script onto `SandstormSystemManager`. This will be your singleton instance.

    *   **Sandstorm Effects Holder:**
        *   Create another empty GameObject.
        *   Rename it to `SandstormEffects`. This will hold all your various effect components.
        *   Drag `SandstormVisualEffect.cs`, `SandstormAudioEffect.cs`, and `SandstormPlayerEffect.cs` onto `SandstormEffects`.

    *   **Configure `SandstormVisualEffect`:**
        *   Create a new Particle System: `GameObject -> Effects -> Particle System`. Rename it to `SandstormParticles`.
        *   Adjust `SandstormParticles` settings to resemble sand or dust (e.g., yellow/orange color, small size, fast speed, fade out).
        *   Drag `SandstormParticles` from the Hierarchy into the `SandstormVisualEffect`'s `Sandstorm Particles` slot.
        *   (Optional but Recommended): Go to `Window -> Rendering -> Lighting -> Environment Tab` in Unity. Note down your `Fog Color` and `Fog Density`. You can then set these as `Default Fog Color` and `Default Fog Density` in the `SandstormVisualEffect` component for a smooth transition.

    *   **Configure `SandstormAudioEffect`:**
        *   Ensure the `SandstormEffects` GameObject (where `SandstormAudioEffect` is attached) has an `AudioSource` component. (It should be added automatically by `[RequireComponent(typeof(AudioSource))]`).
        *   Import an `AudioClip` (e.g., a wind sound, or a generic ambient sound).
        *   Drag this `AudioClip` into the `SandstormAudioEffect`'s `Sandstorm Wind Clip` slot.

    *   **Configure `SandstormPlayerEffect`:**
        *   **Player GameObject:** Create a simple player (e.g., `GameObject -> 3D Object -> Capsule`). Rename it `Player`.
        *   Drag `PlayerHealth.cs` onto your `Player` GameObject.
        *   **Camera:** Ensure your `Main Camera` is present in the scene. It could be a child of your `Player` or a separate camera.
        *   Drag your `Player` GameObject into the `SandstormPlayerEffect`'s `Player Health` slot.
        *   Drag your `Main Camera` into the `SandstormPlayerEffect`'s `Player Camera` slot.

    *   **Sandstorm Trigger:**
        *   Create an empty GameObject.
        *   Rename it to `SandstormTrigger`.
        *   Drag `SandstormTrigger.cs` onto `SandstormTrigger`.
        *   In the Inspector, you can customize the `SandstormProperties` (intensity, duration, colors, damage) or leave the `Default` values.
        *   Check `Trigger On Start` if you want the sandstorm to begin automatically when you play the scene. Otherwise, use the assigned keys (default: `1` to start, `2` to stop).

4.  **Run the Scene!**
    *   Press Play in the Unity editor.
    *   If `Trigger On Start` is checked, the sandstorm should immediately begin its ramp-up.
    *   Observe the changes: fog, particles, wind sounds, and if you move your player, its camera FOV will change, and its health will decrease (check PlayerHealth component in Inspector while playing).
    *   Press the '2' key (default) to force the sandstorm to dissipate.

This setup provides a complete, working example of the 'SandstormSystem' design pattern, demonstrating how to manage complex environmental effects using a centralized, event-driven, and modular approach in Unity.