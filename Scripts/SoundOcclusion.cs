// Unity Design Pattern Example: SoundOcclusion
// This script demonstrates the SoundOcclusion pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SoundOcclusion' design pattern in Unity describes a system where the characteristics of a sound (like volume, pitch, or frequency content) change based on obstacles between the sound source and the listener. It enhances realism by simulating how sound travels through environments.

This example provides a `SoundOcclusion` component that can be attached to any GameObject with an `AudioSource`. It periodically casts a ray from the sound source towards the active `AudioListener` to detect occluding objects. If an occluder is found, it dynamically adjusts the sound's volume and applies an `AudioLowPassFilter` to simulate muffling.

---

### Key Concepts of the SoundOcclusion Pattern:

1.  **Sound Source:** An `AudioSource` component emits the sound.
2.  **Listener:** An `AudioListener` (typically on the Main Camera) receives the sound.
3.  **Occluders:** GameObjects with `Collider` components that are configured to block sound (usually via a specific `LayerMask`).
4.  **Detection Mechanism:** Often a `Physics.Raycast` or `Physics.SphereCast` from the sound source to the listener, checking for obstacles.
5.  **Occlusion Effects:**
    *   **Volume Attenuation:** Reducing the `AudioSource.volume`.
    *   **Frequency Filtering:** Applying an `AudioLowPassFilter` to muffle higher frequencies, simulating sound absorption.
    *   **Pitch Modification:** Less common, but could also be used.
6.  **Smooth Transitions:** Effects should transition smoothly (e.g., using `Mathf.Lerp`) rather than snapping instantly to avoid jarring audio changes.
7.  **Performance Considerations:** Raycasting can be expensive. It's usually done periodically using a `Coroutine` rather than every frame.

---

### `SoundOcclusion.cs` Script

To use this, create a new C# script named `SoundOcclusion.cs` and paste the following code into it.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// Implements the Sound Occlusion design pattern in Unity.
/// This script is attached to an AudioSource and dynamically adjusts its volume and
/// applies a low-pass filter based on obstacles between the sound source and the AudioListener.
/// </summary>
/// <remarks>
/// The 'Sound Occlusion' pattern aims to enhance audio realism by simulating
/// how sound is affected by environmental geometry.
/// </remarks>
[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource is present
public class SoundOcclusion : MonoBehaviour
{
    [Header("Occlusion Settings")]
    [Tooltip("The layer(s) that will block sound. Make sure your occluding objects are on these layers.")]
    [SerializeField] private LayerMask occluderLayers = ~0; // Default to all layers

    [Tooltip("How often, in seconds, the occlusion check should run. A lower value is more responsive but more performance-intensive.")]
    [SerializeField] private float checkInterval = 0.2f; // Check every 0.2 seconds

    [Tooltip("How quickly the volume and filter should transition (0 = instant, 1 = very slow).")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float transitionSpeed = 0.1f; // Lerp factor for smooth transitions

    [Header("Volume Attenuation")]
    [Tooltip("The volume multiplier when sound is fully occluded (e.g., 0.3 means 30% of original volume).")]
    [Range(0f, 1f)]
    [SerializeField] private float occludedVolumeMultiplier = 0.3f;

    [Header("Low Pass Filter (Optional)")]
    [Tooltip("Whether to apply a low-pass filter to muffle the sound when occluded.")]
    [SerializeField] private bool useLowPassFilter = true;

    [Tooltip("The cutoff frequency for the low-pass filter when fully occluded. Lower values muffle the sound more.")]
    [Range(220f, 22000f)] // Human speech range typically 300-3000 Hz, full range 20-20k Hz
    [SerializeField] private float occludedLowPassCutoff = 800f;

    [Tooltip("The resonance quality for the low-pass filter when fully occluded. Higher values create a more pronounced filter effect.")]
    [Range(1f, 10f)]
    [SerializeField] private float occludedLowPassResonance = 1f;

    // --- Private Members ---
    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private AudioListener audioListener;

    private float originalVolume;
    private float targetVolume;

    private float originalLowPassCutoff = 22000f; // Default high pass-through
    private float originalLowPassResonance = 1f; // Default neutral resonance
    private float targetLowPassCutoff;
    private float targetLowPassResonance;

    private Coroutine occlusionCheckCoroutine;
    private bool isCurrentlyOccluded = false;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes components and sets up original audio parameters.
    /// </summary>
    void Awake()
    {
        // 1. Get AudioSource component (guaranteed by [RequireComponent])
        audioSource = GetComponent<AudioSource>();

        // 2. Handle AudioLowPassFilter: get existing or add one if needed.
        if (useLowPassFilter)
        {
            lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null)
            {
                lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            }
            // Store original filter settings before modification.
            originalLowPassCutoff = lowPassFilter.cutoffFrequency;
            originalLowPassResonance = lowPassFilter.lowpassResonanceQ;
        }

        // 3. Find the active AudioListener in the scene.
        audioListener = FindObjectOfType<AudioListener>();
        if (audioListener == null)
        {
            Debug.LogWarning("SoundOcclusion: No AudioListener found in the scene. Occlusion will not function.", this);
            enabled = false; // Disable script if no listener
            return;
        }

        // 4. Store the original volume of the AudioSource.
        originalVolume = audioSource.volume;
        targetVolume = originalVolume; // Initially, no occlusion

        // 5. Initialize target filter values to their originals
        if (lowPassFilter != null)
        {
            targetLowPassCutoff = originalLowPassCutoff;
            targetLowPassResonance = originalLowPassResonance;
        }
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Starts the periodic occlusion check coroutine.
    /// </summary>
    void OnEnable()
    {
        // Ensure only one coroutine is running at a time
        if (occlusionCheckCoroutine != null)
        {
            StopCoroutine(occlusionCheckCoroutine);
        }
        occlusionCheckCoroutine = StartCoroutine(CheckOcclusionPeriodically());
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// Stops the coroutine and resets audio parameters to their original state.
    /// </summary>
    void OnDisable()
    {
        // Stop the coroutine to prevent errors or unnecessary work
        if (occlusionCheckCoroutine != null)
        {
            StopCoroutine(occlusionCheckCoroutine);
            occlusionCheckCoroutine = null;
        }

        // Reset audio properties to their original state
        if (audioSource != null)
        {
            audioSource.volume = originalVolume;
        }
        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = originalLowPassCutoff;
            lowPassFilter.lowpassResonanceQ = originalLowPassResonance;
            // Optionally, disable the filter if it was added by this script
            // lowPassFilter.enabled = false; // Or remove it with Destroy(lowPassFilter); if desired
        }
    }

    /// <summary>
    /// Called once per frame.
    /// Smoothly interpolates the AudioSource's properties towards their target values.
    /// </summary>
    void Update()
    {
        // Smoothly transition the volume
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, transitionSpeed);

        // Smoothly transition the low-pass filter properties
        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetLowPassCutoff, transitionSpeed);
            lowPassFilter.lowpassResonanceQ = Mathf.Lerp(lowPassFilter.lowpassResonanceQ, targetLowPassResonance, transitionSpeed);

            // Enable/disable the filter component based on whether it's effectively "active"
            // This prevents the filter from processing when it's at its "open" state, saving some CPU.
            if (lowPassFilter.enabled != isCurrentlyOccluded)
            {
                 // Only enable filter if actually occluded to save performance
                lowPassFilter.enabled = isCurrentlyOccluded;
            }
        }
    }

    // --- Occlusion Logic ---

    /// <summary>
    /// Coroutine to periodically check for occlusion.
    /// This is more performant than checking every frame in Update().
    /// </summary>
    private IEnumerator CheckOcclusionPeriodically()
    {
        while (true) // Loop indefinitely while script is enabled
        {
            // Only perform the check if the AudioSource is actually playing to save performance
            if (audioSource.isPlaying)
            {
                PerformOcclusionCheck();
            }
            yield return new WaitForSeconds(checkInterval); // Wait for the specified interval
        }
    }

    /// <summary>
    /// Performs the raycast to determine if the sound source is occluded from the listener.
    /// </summary>
    private void PerformOcclusionCheck()
    {
        Vector3 listenerPosition = audioListener.transform.position;
        Vector3 sourcePosition = audioSource.transform.position;
        Vector3 directionToListener = (listenerPosition - sourcePosition).normalized;
        float distanceToListener = Vector3.Distance(sourcePosition, listenerPosition);

        RaycastHit hit;
        // Cast a ray from the sound source towards the listener, checking only specified occluder layers.
        if (Physics.Raycast(sourcePosition, directionToListener, out hit, distanceToListener, occluderLayers))
        {
            // Ray hit something on an occluder layer before reaching the listener.
            // This means the sound is occluded.
            isCurrentlyOccluded = true;
            Debug.DrawLine(sourcePosition, hit.point, Color.red, checkInterval); // Visual debug for occluded path
        }
        else
        {
            // No occluder detected between source and listener.
            isCurrentlyOccluded = false;
            Debug.DrawLine(sourcePosition, listenerPosition, Color.green, checkInterval); // Visual debug for clear path
        }

        // Apply the appropriate target effects based on occlusion status
        SetOcclusionTargets(isCurrentlyOccluded);
    }

    /// <summary>
    /// Sets the target values for volume and low-pass filter based on occlusion status.
    /// These target values are then smoothly interpolated towards in Update().
    /// </summary>
    /// <param name="occluded">True if currently occluded, false otherwise.</param>
    private void SetOcclusionTargets(bool occluded)
    {
        if (occluded)
        {
            // Set target volume to the occluded level
            targetVolume = originalVolume * occludedVolumeMultiplier;

            // Set target low-pass filter values for occlusion
            if (lowPassFilter != null)
            {
                targetLowPassCutoff = occludedLowPassCutoff;
                targetLowPassResonance = occludedLowPassResonance;
            }
        }
        else
        {
            // Set target volume back to original
            targetVolume = originalVolume;

            // Set target low-pass filter values back to original (or "off" state)
            if (lowPassFilter != null)
            {
                targetLowPassCutoff = originalLowPassCutoff;
                targetLowPassResonance = originalLowPassResonance;
            }
        }
    }

    // --- Debugging and Editor Visuals ---

    /// <summary>
    /// Draws a line between the sound source and the listener in the editor,
    /// providing a visual representation of the occlusion check path.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (audioSource != null && audioListener != null)
        {
            Vector3 sourcePosition = audioSource.transform.position;
            Vector3 listenerPosition = audioListener.transform.position;

            Gizmos.color = Color.yellow; // Indicate the raycast path
            Gizmos.DrawLine(sourcePosition, listenerPosition);
            Gizmos.DrawSphere(sourcePosition, 0.1f); // Mark the sound source
            Gizmos.DrawSphere(listenerPosition, 0.1f); // Mark the listener
        }
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create a New C# Script:** In your Unity project, right-click in the Project window, go to `Create > C# Script`, and name it `SoundOcclusion`.
2.  **Paste the Code:** Replace the default content of the new script with the code provided above.
3.  **Prepare your Sound Source:**
    *   Create an empty GameObject (e.g., "SFX_Doorbell", "Ambient_Waterfall").
    *   Add an `AudioSource` component to this GameObject (`Add Component > Audio > Audio Source`).
    *   Assign an `AudioClip` to the `AudioSource`.
    *   (Optional but recommended for ambient sounds) Check `Loop` on the `AudioSource`.
4.  **Attach `SoundOcclusion` Script:** Add the `SoundOcclusion.cs` script to the same GameObject that has the `AudioSource` component (`Add Component > Scripts > Sound Occlusion`).
5.  **Configure `SoundOcclusion` Component:**
    *   **Occluder Layers:** In the Inspector for the `SoundOcclusion` component, select the `Occluder Layers` dropdown. Choose the layer(s) that your walls, doors, or other objects that should block sound are on.
        *   **Important:** You might need to create a new layer (e.g., "SoundOccluder") in `Layers > Add Layer...` and then assign your geometry to it.
    *   **Check Interval:** Adjust how often the script checks for occlusion. A smaller value means more responsiveness but higher CPU usage.
    *   **Transition Speed:** Controls how smoothly the sound changes.
    *   **Occluded Volume Multiplier:** Set how much the volume should drop when occluded.
    *   **Use Low Pass Filter:** Check this if you want to muffle the sound (recommended for realism).
    *   **Occluded Low Pass Cutoff/Resonance:** Fine-tune the muffling effect. Lower cutoff values make the sound more muffled.
6.  **Ensure an `AudioListener` Exists:** By default, your Main Camera GameObject will have an `AudioListener` component. If not, add one. The `SoundOcclusion` script automatically finds the active `AudioListener` in the scene.
7.  **Run the Scene:** Play your scene. As you move occluding objects between your sound source and the camera (or move the camera behind objects), you should hear the sound's volume decrease and become muffled according to your settings. You'll also see green/red debug lines in the Scene view indicating whether the path is clear or occluded.

---

### Example Scenario:

Imagine a game with a campfire sound.
1.  Create an empty GameObject named `CampfireSound`.
2.  Add an `AudioSource` component to `CampfireSound` and assign a campfire `AudioClip`. Set it to `Loop`.
3.  Add the `SoundOcclusion` script to `CampfireSound`.
4.  Create a new `Layer` called "Environment" in Unity. Assign all your walls, trees, etc., to this layer.
5.  In the `SoundOcclusion` component on `CampfireSound`, set `Occluder Layers` to "Environment".
6.  Adjust `Occluded Volume Multiplier` to `0.4` and `Occluded Low Pass Cutoff` to `1500`Hz.
7.  When the player (with the `AudioListener` on their camera) is in direct line of sight of the campfire, the sound plays normally.
8.  When a wall from the "Environment" layer comes between the player and the campfire, the campfire sound will smoothly become quieter and muffled, simulating the sound being blocked.