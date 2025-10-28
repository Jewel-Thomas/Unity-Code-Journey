// Unity Design Pattern Example: VolcanoEruptionSystem
// This script demonstrates the VolcanoEruptionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'VolcanoEruptionSystem' is not a standard, recognized design pattern like Singleton or Observer. However, given its name, it strongly suggests a system for **broadcasting a major event** (the eruption) and allowing **multiple disparate parts of a game to react** to it in a decoupled way. This aligns very closely with the **Observer (or Publish-Subscribe) pattern**.

In this interpretation:
*   **The Volcano** (Subject/Publisher) initiates the eruption.
*   **The Eruption System** (Event Manager) acts as the central hub, providing a global event that other systems can subscribe to.
*   **Various Game Elements** (Observers/Subscribers) listen for the eruption event and react accordingly (e.g., spawning ash, shaking the camera, playing sounds, triggering lava flow).

This approach promotes **loose coupling**, meaning the volcano doesn't need to know about all the specific effects it triggers. It simply announces an eruption, and any interested party can react. This makes the system highly **extensible**; new effects can be added simply by creating a new script that subscribes to the event, without modifying the volcano or the eruption system itself.

---

## Volcano Eruption System Example

This example provides a complete C# Unity setup demonstrating the 'VolcanoEruptionSystem' pattern using C# events.

**How to Use This in Unity:**

1.  **Create a new C# script** named `VolcanoEruptionSystem` (or similar, but copy all code below into it).
2.  **Create an Empty GameObject** in your scene, rename it to "Volcano".
3.  **Attach the `VolcanoEruptor` component** to the "Volcano" GameObject. Configure its eruption parameters in the Inspector.
4.  **Create several other Empty GameObjects** (e.g., "AshEffect", "ShakeEffect", "LavaEffect", "SoundEffect").
5.  **Attach one of the `EruptionEffect` scripts** (e.g., `AshCloudEffect`, `GroundShakeEffect`, `LavaFlowEffect`, `SoundEffectPlayer`) to each of these new GameObjects.
6.  **Configure the properties** of each effect script in the Inspector (e.g., assign particle systems, audio clips, camera for shaking).
7.  **Run the scene.** The volcano will go through its eruption phases, and all subscribed effects will react. You can also trigger an eruption manually via the `Start Eruption` button on the `VolcanoEruptor` component in the Inspector.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic; // Not strictly needed for this pattern, but common

// --- 1. Eruption Data & Phases ---
// This defines the information that will be passed along with each eruption event.
// It allows observers to react differently based on the specific phase or intensity.

/// <summary>
/// Represents the current phase of a volcano eruption.
/// </summary>
public enum EruptionPhase
{
    RUMBLING,            // Initial warning signs, tremors.
    INITIAL_EXPLOSION,   // The first big blast, often with ash and projectiles.
    LAVA_FLOW_ACTIVE,    // Lava is actively flowing from the volcano.
    ASH_FALL_ACTIVE,     // Ash continues to fall over a wider area.
    CALMING_DOWN,        // Eruption intensity decreases, effects subside.
    DORMANT              // The volcano is currently inactive.
}

/// <summary>
/// A struct to hold all relevant data about a specific eruption event or phase.
/// This data is sent to all subscribers of the VolcanoEruptionSystem.
/// </summary>
public struct EruptionData
{
    public VolcanoEruptor SourceVolcano; // Reference to the volcano initiating the event.
    public EruptionPhase CurrentPhase;   // The specific phase the eruption is currently in.
    public float Intensity;              // A normalized value (0-1) indicating the magnitude of the current phase.
                                         // 0 = no effect, 1 = maximum effect.
    public float PhaseDuration;          // How long this specific phase is expected to last.
    public string Description;           // A short descriptive message for this phase.

    public EruptionData(VolcanoEruptor source, EruptionPhase phase, float intensity, float duration, string description)
    {
        SourceVolcano = source;
        CurrentPhase = phase;
        Intensity = Mathf.Clamp01(intensity); // Ensure intensity is between 0 and 1.
        PhaseDuration = duration;
        Description = description;
    }
}

// --- 2. The VolcanoEruptionSystem (Static Event Manager - Publisher/Subject) ---
// This is the core of the 'VolcanoEruptionSystem' pattern. It acts as a global
// event dispatcher that doesn't need to be attached to any GameObject.
// Any component can subscribe to its events, and any VolcanoEruptor can trigger them.

/// <summary>
/// The central static class for the VolcanoEruptionSystem.
/// It provides a global event that other systems can subscribe to,
/// enabling a decoupled way to react to volcano eruption phases.
/// </summary>
public static class VolcanoEruptionSystem
{
    /// <summary>
    /// This is the main event. Any MonoBehaviour that needs to react to volcano
    /// eruptions should subscribe to this event.
    /// The Action takes an EruptionData struct, providing all necessary details.
    /// </summary>
    public static event Action<EruptionData> OnEruptionPhaseChange;

    /// <summary>
    /// Triggers the <c>OnEruptionPhaseChange</c> event, broadcasting the
    /// current eruption phase data to all subscribed listeners.
    /// This method is called by a <c>VolcanoEruptor</c> script when a phase changes.
    /// </summary>
    /// <param name="source">The <c>VolcanoEruptor</c> instance that triggered this event.</param>
    /// <param name="phase">The new <c>EruptionPhase</c>.</param>
    /// <param name="intensity">The intensity of this phase (0-1).</param>
    /// <param name="phaseDuration">The expected duration of this phase.</param>
    /// <param name="description">A descriptive message for this phase.</param>
    public static void AnnounceEruptionPhase(VolcanoEruptor source, EruptionPhase phase, float intensity, float phaseDuration, string description)
    {
        EruptionData data = new EruptionData(source, phase, intensity, phaseDuration, description);

        // Check if there are any subscribers before invoking to prevent NullReferenceException.
        OnEruptionPhaseChange?.Invoke(data);

        Debug.Log($"<color=orange>[VolcanoEruptionSystem]</color> Announced: {data.SourceVolcano.name} - Phase: {data.CurrentPhase}, Intensity: {data.Intensity:F2}, Desc: '{data.Description}'");
    }

    // Optional: A method to reset the system, useful for scene clean-up or testing.
    // In a real game, you might not need this for a static event, as event subscriptions
    // typically handle their own OnDisable/OnEnable.
    public static void ResetSystem()
    {
        OnEruptionPhaseChange = null;
        Debug.Log("<color=orange>[VolcanoEruptionSystem]</color> System reset.");
    }
}


// --- 3. VolcanoEruptor (The Actual Volcano in the Scene - Event Initiator) ---
// This MonoBehaviour represents a physical volcano in the scene. It manages its
// own state and triggers the global eruption events via the VolcanoEruptionSystem.

/// <summary>
/// Represents a volcano in the scene that can erupt.
/// It manages its own eruption sequence and broadcasts phase changes
/// through the global <c>VolcanoEruptionSystem</c>.
/// </summary>
public class VolcanoEruptor : MonoBehaviour
{
    [Header("Eruption Settings")]
    [Tooltip("Minimum time between eruptions (in seconds).")]
    [SerializeField] private float minEruptionInterval = 30f;
    [Tooltip("Maximum time between eruptions (in seconds).")]
    [SerializeField] private float maxEruptionInterval = 90f;
    [Tooltip("Whether the volcano should erupt automatically after a delay.")]
    [SerializeField] private bool autoErupt = true;

    private bool _isErupting = false;
    private Coroutine _eruptionSequenceCoroutine;

    void Start()
    {
        if (autoErupt)
        {
            StartCoroutine(AutoEruptionCycle());
        }
        // Announce initial dormant phase
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.DORMANT, 0f, 0f, "Volcano is dormant.");
    }

    /// <summary>
    /// Public method to manually start an eruption, e.g., from a UI button or game event.
    /// </summary>
    [ContextMenu("Start Eruption")] // Allows triggering from Unity Editor context menu
    public void StartEruptionSequence()
    {
        if (_isErupting)
        {
            Debug.LogWarning($"<color=yellow>{name}</color>: Already erupting!");
            return;
        }

        Debug.Log($"<color=green>{name}</color>: Starting eruption sequence!");
        _eruptionSequenceCoroutine = StartCoroutine(EruptionSequence());
    }

    /// <summary>
    /// Stops any ongoing eruption sequence.
    /// </summary>
    [ContextMenu("Stop Eruption")]
    public void StopEruptionSequence()
    {
        if (_eruptionSequenceCoroutine != null)
        {
            StopCoroutine(_eruptionSequenceCoroutine);
            _isErupting = false;
            _eruptionSequenceCoroutine = null;
            Debug.Log($"<color=red>{name}</color>: Eruption sequence stopped prematurely.");
            VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.DORMANT, 0f, 0f, "Eruption stopped.");
        }
    }

    /// <summary>
    /// Coroutine to handle the full eruption lifecycle, phase by phase.
    /// This is where the volcano triggers events through the VolcanoEruptionSystem.
    /// </summary>
    private IEnumerator EruptionSequence()
    {
        _isErupting = true;

        // Phase 1: Rumbling (Pre-eruption)
        float rumbleDuration = UnityEngine.Random.Range(3f, 7f);
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.RUMBLING, 0.2f, rumbleDuration, "Ground rumbling...");
        yield return new WaitForSeconds(rumbleDuration);

        // Phase 2: Initial Explosion
        float explosionDuration = UnityEngine.Random.Range(2f, 4f);
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.INITIAL_EXPLOSION, 1.0f, explosionDuration, "Massive explosion!");
        yield return new WaitForSeconds(explosionDuration);

        // Phase 3: Lava Flow Active
        float lavaFlowDuration = UnityEngine.Random.Range(10f, 20f);
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.LAVA_FLOW_ACTIVE, 0.8f, lavaFlowDuration, "Lava streams flowing down!");
        yield return new WaitForSeconds(lavaFlowDuration);

        // Phase 4: Ash Fall Active
        float ashFallDuration = UnityEngine.Random.Range(15f, 30f);
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.ASH_FALL_ACTIVE, 0.6f, ashFallDuration, "Heavy ash falling...");
        yield return new WaitForSeconds(ashFallDuration);

        // Phase 5: Calming Down
        float calmingDuration = UnityEngine.Random.Range(5f, 10f);
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.CALMING_DOWN, 0.3f, calmingDuration, "Eruption calming down.");
        yield return new WaitForSeconds(calmingDuration);

        // Phase 6: Dormant (Post-eruption)
        _isErupting = false;
        VolcanoEruptionSystem.AnnounceEruptionPhase(this, EruptionPhase.DORMANT, 0f, 0f, "Volcano is dormant again.");
        Debug.Log($"<color=green>{name}</color>: Eruption sequence completed.");

        if (autoErupt)
        {
            StartCoroutine(AutoEruptionCycle()); // Schedule next eruption
        }
    }

    /// <summary>
    /// Manages the automatic eruption cycle, waiting for a random interval before erupting again.
    /// </summary>
    private IEnumerator AutoEruptionCycle()
    {
        float nextEruptionTime = UnityEngine.Random.Range(minEruptionInterval, maxEruptionInterval);
        Debug.Log($"<color=blue>{name}</color>: Next eruption in {nextEruptionTime:F1} seconds...");
        yield return new WaitForSeconds(nextEruptionTime);
        StartEruptionSequence();
    }

    void OnDisable()
    {
        // Ensure any running coroutine is stopped if the GameObject is disabled
        if (_eruptionSequenceCoroutine != null)
        {
            StopCoroutine(_eruptionSequenceCoroutine);
            _isErupting = false;
            _eruptionSequenceCoroutine = null;
        }
    }
}


// --- 4. Eruption Effect Examples (Subscribers/Observers) ---
// These MonoBehaviour scripts demonstrate different ways game elements can react
// to the eruption events broadcast by the VolcanoEruptionSystem.
// Each script subscribes to the system's event and implements its unique logic.

// --- Example Effect 1: Ash Cloud Spawner ---
/// <summary>
/// Reacts to eruption phases by activating/deactivating a particle system
/// to simulate ash clouds.
/// </summary>
public class AshCloudEffect : MonoBehaviour
{
    [Header("Ash Cloud Settings")]
    [Tooltip("The Particle System to control for ash effects.")]
    [SerializeField] private ParticleSystem ashParticleSystem;
    [Tooltip("Minimum intensity for ash particles to activate.")]
    [SerializeField] private float activationThreshold = 0.5f;

    void OnEnable()
    {
        // Crucial for the Observer pattern: Subscribe to the event.
        VolcanoEruptionSystem.OnEruptionPhaseChange += HandleEruptionPhase;
        Debug.Log($"<color=cyan>{name}</color>: Subscribed to VolcanoEruptionSystem events.");

        // Ensure particles are off initially if system is dormant
        if (ashParticleSystem != null)
        {
            ashParticleSystem.Stop();
        }
    }

    void OnDisable()
    {
        // Crucial for the Observer pattern: Unsubscribe from the event
        // to prevent memory leaks and unexpected behavior when the GameObject is disabled or destroyed.
        VolcanoEruptionSystem.OnEruptionPhaseChange -= HandleEruptionPhase;
        Debug.Log($"<color=cyan>{name}</color>: Unsubscribed from VolcanoEruptionSystem events.");
    }

    /// <summary>
    /// This method is called whenever the VolcanoEruptionSystem announces a new phase.
    /// It checks the EruptionData and reacts accordingly.
    /// </summary>
    /// <param name="data">The data describing the current eruption phase.</param>
    private void HandleEruptionPhase(EruptionData data)
    {
        if (ashParticleSystem == null) return;

        switch (data.CurrentPhase)
        {
            case EruptionPhase.INITIAL_EXPLOSION:
            case EruptionPhase.ASH_FALL_ACTIVE:
                // Activate ash if intensity is high enough
                if (!ashParticleSystem.isPlaying && data.Intensity >= activationThreshold)
                {
                    ashParticleSystem.Play();
                    var main = ashParticleSystem.main;
                    // Adjust particle intensity based on eruption intensity
                    main.startLifetime = data.Intensity * 5f; // Example: more intense = longer lasting particles
                    var emission = ashParticleSystem.emission;
                    emission.rateOverTime = data.Intensity * 100f; // Example: more intense = more particles
                    Debug.Log($"<color=cyan>{name}</color>: Activating ash cloud for {data.CurrentPhase} (Intensity: {data.Intensity:F2})");
                }
                break;
            case EruptionPhase.CALMING_DOWN:
            case EruptionPhase.DORMANT:
                // Stop ash particles when eruption calms down or goes dormant
                if (ashParticleSystem.isPlaying)
                {
                    ashParticleSystem.Stop();
                    Debug.Log($"<color=cyan>{name}</color>: Stopping ash cloud.");
                }
                break;
            default:
                // For other phases, ensure it's off if not relevant
                if (ashParticleSystem.isPlaying && data.Intensity < activationThreshold)
                {
                    ashParticleSystem.Stop();
                    Debug.Log($"<color=cyan>{name}</color>: Stopping ash cloud (low intensity for phase {data.CurrentPhase}).");
                }
                break;
        }
    }
}

// --- Example Effect 2: Ground Shake ---
/// <summary>
/// Reacts to eruption phases by triggering a camera shake effect.
/// Requires a reference to the main camera.
/// </summary>
public class GroundShakeEffect : MonoBehaviour
{
    [Header("Ground Shake Settings")]
    [Tooltip("The Camera to shake. If null, will try to find Camera.main.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("Base shake magnitude.")]
    [SerializeField] private float baseShakeMagnitude = 0.1f;
    [Tooltip("Base shake duration.")]
    [SerializeField] private float baseShakeDuration = 0.5f;

    private Vector3 _originalCameraPosition;
    private Coroutine _shakeCoroutine;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            _originalCameraPosition = mainCamera.transform.localPosition;
        }
    }

    void OnEnable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange += HandleEruptionPhase;
        Debug.Log($"<color=yellow>{name}</color>: Subscribed to VolcanoEruptionSystem events.");
    }

    void OnDisable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange -= HandleEruptionPhase;
        Debug.Log($"<color=yellow>{name}</color>: Unsubscribed from VolcanoEruptionSystem events.");

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }
        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = _originalCameraPosition; // Reset camera position
        }
    }

    private void HandleEruptionPhase(EruptionData data)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning($"<color=red>{name}</color>: Main Camera not assigned for shaking!");
            return;
        }

        float shakeMagnitude = 0f;
        float shakeDuration = 0f;

        switch (data.CurrentPhase)
        {
            case EruptionPhase.RUMBLING:
                shakeMagnitude = baseShakeMagnitude * data.Intensity * 0.5f; // Small shake for rumbling
                shakeDuration = baseShakeDuration * data.Intensity * 2f;
                break;
            case EruptionPhase.INITIAL_EXPLOSION:
                shakeMagnitude = baseShakeMagnitude * data.Intensity * 2f; // Big shake for explosion
                shakeDuration = baseShakeDuration * data.Intensity * 3f;
                break;
            case EruptionPhase.ASH_FALL_ACTIVE:
                shakeMagnitude = baseShakeMagnitude * data.Intensity * 0.3f; // Gentle shake for ash fall
                shakeDuration = baseShakeDuration * data.Intensity * 1.5f;
                break;
            default:
                break;
        }

        if (shakeMagnitude > 0.01f && shakeDuration > 0.1f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine); // Stop previous shake to start a new one
            }
            _shakeCoroutine = StartCoroutine(Shake(shakeMagnitude, shakeDuration));
            Debug.Log($"<color=yellow>{name}</color>: Shaking camera for {data.CurrentPhase} (Magnitude: {shakeMagnitude:F2})");
        }
        else if (_shakeCoroutine != null && shakeMagnitude <= 0.01f)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
            mainCamera.transform.localPosition = _originalCameraPosition;
            Debug.Log($"<color=yellow>{name}</color>: Stopping camera shake.");
        }
    }

    private IEnumerator Shake(float magnitude, float duration)
    {
        if (mainCamera == null) yield break;

        _originalCameraPosition = mainCamera.transform.localPosition; // Store current position before shaking
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.localPosition = _originalCameraPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = _originalCameraPosition; // Reset to original position
        _shakeCoroutine = null;
    }
}

// --- Example Effect 3: Lava Flow Visuals ---
/// <summary>
/// Activates/deactivates a lava flow object or particle system based on eruption phase.
/// </summary>
public class LavaFlowEffect : MonoBehaviour
{
    [Header("Lava Flow Settings")]
    [Tooltip("The GameObject representing the lava flow (e.g., a mesh with a shader, or particle system).")]
    [SerializeField] private GameObject lavaFlowObject;
    [Tooltip("The speed at which lava 'flows' or scales up/down.")]
    [SerializeField] private float flowSpeed = 1f;

    private Vector3 _initialLavaScale; // Assuming lava scales up for effect
    private Coroutine _lavaFlowCoroutine;

    void Awake()
    {
        if (lavaFlowObject != null)
        {
            _initialLavaScale = lavaFlowObject.transform.localScale;
            lavaFlowObject.SetActive(false); // Start inactive
        }
    }

    void OnEnable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange += HandleEruptionPhase;
        Debug.Log($"<color=red>{name}</color>: Subscribed to VolcanoEruptionSystem events.");
    }

    void OnDisable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange -= HandleEruptionPhase;
        Debug.Log($"<color=red>{name}</color>: Unsubscribed from VolcanoEruptionSystem events.");

        if (_lavaFlowCoroutine != null)
        {
            StopCoroutine(_lavaFlowCoroutine);
            _lavaFlowCoroutine = null;
        }
        if (lavaFlowObject != null)
        {
            lavaFlowObject.SetActive(false); // Ensure it's off
            lavaFlowObject.transform.localScale = _initialLavaScale; // Reset scale
        }
    }

    private void HandleEruptionPhase(EruptionData data)
    {
        if (lavaFlowObject == null) return;

        if (data.CurrentPhase == EruptionPhase.LAVA_FLOW_ACTIVE)
        {
            if (!lavaFlowObject.activeSelf)
            {
                lavaFlowObject.SetActive(true);
                // Maybe start a particle system if lavaFlowObject holds one
                ParticleSystem ps = lavaFlowObject.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                Debug.Log($"<color=red>{name}</color>: Activating lava flow (Intensity: {data.Intensity:F2})");
            }

            // Start a scale animation based on intensity
            if (_lavaFlowCoroutine != null) StopCoroutine(_lavaFlowCoroutine);
            _lavaFlowCoroutine = StartCoroutine(AnimateLavaFlow(data.Intensity));
        }
        else
        {
            if (lavaFlowObject.activeSelf)
            {
                if (_lavaFlowCoroutine != null) StopCoroutine(_lavaFlowCoroutine);
                _lavaFlowCoroutine = StartCoroutine(DeactivateLavaFlow());
                Debug.Log($"<color=red>{name}</color>: Deactivating lava flow.");
            }
        }
    }

    private IEnumerator AnimateLavaFlow(float targetIntensity)
    {
        Vector3 targetScale = _initialLavaScale * (1f + targetIntensity); // Scale up based on intensity
        float currentScaleFactor = lavaFlowObject.transform.localScale.x / _initialLavaScale.x;

        while (Mathf.Abs(currentScaleFactor - (1f + targetIntensity)) > 0.01f)
        {
            currentScaleFactor = Mathf.Lerp(currentScaleFactor, (1f + targetIntensity), Time.deltaTime * flowSpeed);
            lavaFlowObject.transform.localScale = _initialLavaScale * currentScaleFactor;
            yield return null;
        }
        lavaFlowObject.transform.localScale = targetScale;
    }

    private IEnumerator DeactivateLavaFlow()
    {
        Vector3 currentScale = lavaFlowObject.transform.localScale;
        while (currentScale.x > _initialLavaScale.x)
        {
            currentScale = Vector3.Lerp(currentScale, _initialLavaScale, Time.deltaTime * flowSpeed * 2f); // Shrink faster
            lavaFlowObject.transform.localScale = currentScale;
            yield return null;
        }
        lavaFlowObject.transform.localScale = _initialLavaScale;
        lavaFlowObject.SetActive(false);
        // Maybe stop a particle system if lavaFlowObject holds one
        ParticleSystem ps = lavaFlowObject.GetComponent<ParticleSystem>();
        if (ps != null) ps.Stop();
    }
}

// --- Example Effect 4: Sound Effect Player ---
/// <summary>
/// Plays different audio clips based on the eruption phase.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundEffectPlayer : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Audio clip for rumbling phase.")]
    [SerializeField] private AudioClip rumblingSound;
    [Tooltip("Audio clip for initial explosion.")]
    [SerializeField] private AudioClip explosionSound;
    [Tooltip("Audio clip for general lava/ash activity.")]
    [SerializeField] private AudioClip generalEruptionSound;
    [Tooltip("Volume multiplier for all sounds.")]
    [SerializeField] private float volumeMultiplier = 1f;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 1f; // Make it a 3D sound source
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange += HandleEruptionPhase;
        Debug.Log($"<color=magenta>{name}</color>: Subscribed to VolcanoEruptionSystem events.");
    }

    void OnDisable()
    {
        VolcanoEruptionSystem.OnEruptionPhaseChange -= HandleEruptionPhase;
        Debug.Log($"<color=magenta>{name}</color>: Unsubscribed from VolcanoEruptionSystem events.");
        _audioSource.Stop(); // Stop any playing sound
    }

    private void HandleEruptionPhase(EruptionData data)
    {
        _audioSource.Stop(); // Stop previous sound before playing new one

        AudioClip clipToPlay = null;
        bool loopClip = false;

        switch (data.CurrentPhase)
        {
            case EruptionPhase.RUMBLING:
                clipToPlay = rumblingSound;
                loopClip = true;
                break;
            case EruptionPhase.INITIAL_EXPLOSION:
                clipToPlay = explosionSound;
                break;
            case EruptionPhase.LAVA_FLOW_ACTIVE:
            case EruptionPhase.ASH_FALL_ACTIVE:
                clipToPlay = generalEruptionSound;
                loopClip = true;
                break;
            case EruptionPhase.CALMING_DOWN:
            case EruptionPhase.DORMANT:
                // No specific sound for calming or dormant, or play an 'all clear' sound
                break;
        }

        if (clipToPlay != null)
        {
            _audioSource.clip = clipToPlay;
            _audioSource.loop = loopClip;
            _audioSource.volume = data.Intensity * volumeMultiplier; // Adjust volume by intensity
            _audioSource.Play();
            Debug.Log($"<color=magenta>{name}</color>: Playing '{clipToPlay.name}' for {data.CurrentPhase} (Volume: {_audioSource.volume:F2})");
        }
    }
}
```