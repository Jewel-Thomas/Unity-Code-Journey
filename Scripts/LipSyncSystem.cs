// Unity Design Pattern Example: LipSyncSystem
// This script demonstrates the LipSyncSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'LipSyncSystem' design pattern, as interpreted for Unity, focuses on creating a modular and data-driven approach to animate character mouth shapes (typically blend shapes) in sync with spoken audio. It emphasizes separation of concerns:

1.  **LipSyncData (ScriptableObject):** Stores the timing and sequence of phonemes (mouth shapes) for a specific audio clip. This is the **'what'** and **'when'**.
2.  **PhonemeMap (ScriptableObject):** Translates generic phoneme identifiers (e.g., "M", "A", "OO") into model-specific animation parameters (e.g., blend shape indices, bone rotations). This is the **'how'** for a particular character model.
3.  **LipSyncController (MonoBehaviour):** The runtime component that orchestrates audio playback, reads the `LipSyncData`, and applies the animation using the `PhonemeMap`. This is the **'driver'**.

This structure makes the system highly flexible. You can swap `LipSyncData` for different dialogue lines, or `PhonemeMap` for different character models, without touching code.

---

Here is the complete C# Unity example demonstrating this pattern:

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For easier LINQ usage if needed, though trying to avoid heavy LINQ in Update for performance.

/// <summary>
/// LipSyncSystem Design Pattern:
/// 
/// This pattern separates the concerns of lip-syncing into three main components:
/// 1.  LipSyncData (ScriptableObject): Stores the sequence and timing of phonemes for an audio clip.
///     This is the 'what' to animate and 'when'.
/// 2.  PhonemeMap (ScriptableObject): Maps generic phoneme strings to model-specific blend shape indices
///     and their target weights. This handles the 'how' for a specific character model.
/// 3.  LipSyncController (MonoBehaviour): The runtime component that plays audio, interprets LipSyncData
///     using the PhonemeMap, and drives the SkinnedMeshRenderer's blend shapes. This is the 'driver'.
///
/// This modularity allows for easy data creation, character re-targeting, and flexible runtime control.
/// </summary>

// --- 1. LipSyncData: Stores the phoneme events for a specific audio clip ---
// This is a ScriptableObject, allowing us to create reusable lip-sync data assets
// separate from any specific MonoBehaviour. This promotes data reusability and
// makes it easy to assign different lip-sync data to different audio clips.
[CreateAssetMenu(fileName = "NewLipSyncData", menuName = "LipSync/LipSync Data")]
public class LipSyncData : ScriptableObject
{
    [System.Serializable]
    public struct PhonemeEvent
    {
        [Tooltip("The phoneme string (e.g., 'M', 'A', 'EE', 'OO'). Case-insensitive match with PhonemeMap.")]
        public string phoneme;
        [Tooltip("The time in seconds from the start of the audio clip when this phoneme begins.")]
        public float startTime;
        [Tooltip("The estimated duration in seconds this phoneme should ideally be held. Used for blending logic.")]
        public float duration;

        // Optional: Constructor for easier programmatic creation
        public PhonemeEvent(string p, float s, float d)
        {
            phoneme = p;
            startTime = s;
            duration = d;
        }
    }

    [Header("Lip Sync Phoneme Events")]
    [Tooltip("List of phonemes and their timings for an audio clip. Order matters for playback.")]
    public List<PhonemeEvent> phonemeEvents = new List<PhonemeEvent>();

    // You might add methods here to load from external files (e.g., .LIP files)
    // or to generate data programmatically. For this example, we assume
    // the data is manually entered or pre-generated into this ScriptableObject.
}

// --- 2. PhonemeMap: Maps phoneme strings to blend shape indices ---
// Another ScriptableObject. This allows a single LipSyncController to work
// with different character models that might have different blend shape setups
// (e.g., 'Mouth_M' blend shape might be index 5 on one model, and index 12 on another).
// This also supports characters that use different blend shape names for the same phoneme.
[CreateAssetMenu(fileName = "NewPhonemeMap", menuName = "LipSync/Phoneme Map")]
public class PhonemeMap : ScriptableObject
{
    [System.Serializable]
    public struct PhonemeBlendShapePair
    {
        [Tooltip("The phoneme string (e.g., 'M', 'A', 'EE', 'OO'). Must match names in LipSyncData.")]
        public string phoneme;
        [Tooltip("The index of the blend shape on the SkinnedMeshRenderer for this phoneme.")]
        public int blendShapeIndex;
        [Tooltip("The target weight (0-100) for this blend shape when active.")]
        [Range(0, 100)]
        public float targetWeight;
    }

    [Header("Phoneme to Blend Shape Mapping")]
    [Tooltip("Maps phoneme strings to specific blend shape indices and their target weights.")]
    public List<PhonemeBlendShapePair> mappings = new List<PhonemeBlendShapePair>();

    [Tooltip("The blend shape index for the 'Rest' or 'Idle' mouth pose. Set to -1 if no specific rest blend shape.")]
    public int restBlendShapeIndex = -1; 
    [Tooltip("The target weight (0-100) for the 'Rest' blend shape.")]
    [Range(0, 100)]
    public float restTargetWeight = 0; 

    /// <summary>
    /// Gets the blend shape index for a given phoneme string.
    /// </summary>
    /// <param name="phoneme">The phoneme string (e.g., "M", "A").</param>
    /// <returns>The blend shape index, or -1 if no mapping is found.</returns>
    public int GetBlendShapeIndex(string phoneme)
    {
        foreach (var mapping in mappings)
        {
            if (mapping.phoneme.Equals(phoneme, System.StringComparison.OrdinalIgnoreCase))
            {
                return mapping.blendShapeIndex;
            }
        }
        return -1; // Not found
    }

    /// <summary>
    /// Gets the target weight for a given phoneme string.
    /// </summary>
    /// <param name="phoneme">The phoneme string.</param>
    /// <returns>The target weight (0-100), or 0 if no mapping is found.</returns>
    public float GetTargetWeight(string phoneme)
    {
        foreach (var mapping in mappings)
        {
            if (mapping.phoneme.Equals(phoneme, System.StringComparison.OrdinalIgnoreCase))
            {
                return mapping.targetWeight;
            }
        }
        return 0f; // Default weight if not found
    }
}


// --- 3. LipSyncController: The core MonoBehaviour that drives the lip-sync ---
// This component orchestrates the audio playback, reads the LipSyncData,
// and applies blend shape weights to the SkinnedMeshRenderer.
// It uses the PhonemeMap to translate generic phonemes into model-specific blend shapes.
[RequireComponent(typeof(AudioSource))] // Ensure an AudioSource is present on the GameObject
public class LipSyncController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The AudioSource component used for playing speech on this GameObject.")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("The SkinnedMeshRenderer component that contains the blend shapes for the character's mouth.")]
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [Tooltip("The mapping of phoneme strings to blend shape indices and target weights for this character model.")]
    [SerializeField] private PhonemeMap _phonemeMap;

    [Header("Lip Sync Settings")]
    [Tooltip("How smoothly the blend shapes transition between phonemes.")]
    [Range(0.01f, 0.5f)]
    public float blendTransitionDuration = 0.1f; // Duration in seconds to blend between shapes

    private Coroutine _lipSyncCoroutine;
    private int _currentPhonemeEventIndex = -1; // Tracks the currently active phoneme event in _currentLipSyncData.phonemeEvents
    private float _blendTimer = 0f;             // Timer for blend shape transitions
    private float _startWeight = 0f;            // Weight of the blend shape at the start of transition
    private float _targetWeight = 0f;           // Weight of the blend shape at the end of transition
    private int _activeBlendShapeIndex = -1;    // The blend shape index currently being animated towards its target
    private int _fadingBlendShapeIndex = -1;    // The blend shape index that was active just before and is now fading out

    void Awake()
    {
        // Get AudioSource if not already assigned in Inspector
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        // Basic validation
        if (_skinnedMeshRenderer == null)
        {
            Debug.LogError("LipSyncController: No SkinnedMeshRenderer assigned! LipSync will not work. Please assign one.", this);
            enabled = false; // Disable component if essential dependency is missing
            return;
        }
        if (_phonemeMap == null)
        {
            Debug.LogError("LipSyncController: No PhonemeMap assigned! LipSync will not work. Please create and assign a PhonemeMap asset.", this);
            enabled = false;
            return;
        }

        // Initialize all blend shapes to 0 at start to ensure a neutral mouth pose
        ResetAllBlendShapes();
    }

    /// <summary>
    /// Resets all blend shapes on the SkinnedMeshRenderer to 0 weight.
    /// </summary>
    private void ResetAllBlendShapes()
    {
        if (_skinnedMeshRenderer != null && _skinnedMeshRenderer.sharedMesh != null)
        {
            for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
            }
        }
    }

    /// <summary>
    /// Starts the lip-sync process with a given audio clip and its corresponding lip-sync data.
    /// This is the primary method to call externally to initiate speech animation.
    /// </summary>
    /// <param name="audioClip">The audio clip to play.</param>
    /// <param name="lipSyncData">The LipSyncData asset corresponding to the audio clip.</param>
    public void PlayLipSync(AudioClip audioClip, LipSyncData lipSyncData)
    {
        if (audioClip == null || lipSyncData == null)
        {
            Debug.LogWarning("LipSyncController: Cannot play lip sync. Audio clip or LipSyncData is null.", this);
            return;
        }
        if (_skinnedMeshRenderer == null || _phonemeMap == null)
        {
            Debug.LogError("LipSyncController: Cannot play lip sync. Missing SkinnedMeshRenderer or PhonemeMap. Check Inspector assignments.", this);
            return;
        }

        StopLipSync(); // Stop any ongoing lip sync first for a clean start

        _audioSource.clip = audioClip;
        // The LipSyncData is passed directly here; _currentLipSyncData is internal state.
        
        // Reset all internal state for a new playback
        _currentPhonemeEventIndex = -1; 
        _blendTimer = 0f;
        _startWeight = 0f;
        _targetWeight = 0f;
        _activeBlendShapeIndex = -1;
        _fadingBlendShapeIndex = -1;

        ResetAllBlendShapes(); // Ensure mouth is closed/neutral before starting new speech

        _audioSource.Play();
        // Start the coroutine that manages the lip-sync timing
        _lipSyncCoroutine = StartCoroutine(LipSyncPlaybackCoroutine(lipSyncData));
    }

    /// <summary>
    /// Stops the current lip-sync playback, stops the audio, and resets all blend shapes.
    /// </summary>
    public void StopLipSync()
    {
        if (_lipSyncCoroutine != null)
        {
            StopCoroutine(_lipSyncCoroutine);
            _lipSyncCoroutine = null;
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
        ResetAllBlendShapes(); // Ensure mouth returns to neutral after stopping
        
        // Reset internal state
        _currentPhonemeEventIndex = -1;
        _activeBlendShapeIndex = -1;
        _fadingBlendShapeIndex = -1;
    }

    /// <summary>
    /// This Coroutine handles the progression through phoneme events and triggers blend shape updates.
    /// Using a Coroutine here is beneficial for precise timing relative to audio playback,
    /// as it allows yielding based on time, rather than relying solely on Update() which can be variable.
    /// </summary>
    private IEnumerator LipSyncPlaybackCoroutine(LipSyncData lipSyncData)
    {
        List<LipSyncData.PhonemeEvent> events = lipSyncData.phonemeEvents;
        int nextEventIndex = 0; // Index of the next phoneme event to process

        // Loop while audio is playing or there are still phoneme events that might need processing (e.g., final fade out).
        while (_audioSource.isPlaying || nextEventIndex < events.Count || _activeBlendShapeIndex != -1 || _fadingBlendShapeIndex != -1)
        {
            float currentAudioTime = _audioSource.time;

            // Advance to the next phoneme event if its start time has been reached
            if (nextEventIndex < events.Count && currentAudioTime >= events[nextEventIndex].startTime)
            {
                LipSyncData.PhonemeEvent currentEvent = events[nextEventIndex];
                
                // Get the blend shape index and target weight for this phoneme from the PhonemeMap
                int newActiveBlendShapeIndex = _phonemeMap.GetBlendShapeIndex(currentEvent.phoneme);
                float newTargetWeight = _phonemeMap.GetTargetWeight(currentEvent.phoneme);

                // Only start a new transition if the phoneme or its target weight has changed,
                // or if we're moving from a 'rest' state to a specific phoneme.
                if (newActiveBlendShapeIndex != _activeBlendShapeIndex || newTargetWeight != _targetWeight)
                {
                    StartBlendShapeTransition(newActiveBlendShapeIndex, newTargetWeight);
                }
                
                _currentPhonemeEventIndex = nextEventIndex;
                nextEventIndex++; // Move to the next potential event for the next check
            }
            // If no active phoneme or between phonemes, ensure the mouth goes to a 'rest' pose
            else if (_currentPhonemeEventIndex == -1 || // Initial state
                     (nextEventIndex < events.Count && currentAudioTime < events[nextEventIndex].startTime &&
                      (_currentPhonemeEventIndex == -1 || currentAudioTime >= events[_currentPhonemeEventIndex].startTime + events[_currentPhonemeEventIndex].duration)))
            {
                // Check if current phoneme event has ended and next one hasn't started yet
                // Or if we're at the very beginning before the first event.
                ApplyRestPose();
            }

            // Always update blend shapes to progress transitions
            UpdateBlendShapes();

            yield return null; // Wait for the next frame
        }

        // After the audio finishes or the loop exits, ensure all blend shapes are reset to the rest position.
        // This handles the clean end of speech.
        StartBlendShapeTransition(_phonemeMap.restBlendShapeIndex, _phonemeMap.restTargetWeight);
        // Wait for the final blend to complete
        while (_blendTimer < blendTransitionDuration)
        {
            UpdateBlendShapes();
            yield return null;
        }
        ResetAllBlendShapes(); // Final reset to ensure all are zeroed out if no rest blend shape is defined
        
        // Reset state after full playback and reset
        _currentPhonemeEventIndex = -1;
        _activeBlendShapeIndex = -1;
        _fadingBlendShapeIndex = -1;
    }

    /// <summary>
    /// Initiates a smooth blend shape transition to a new target blend shape and weight.
    /// </summary>
    /// <param name="newIndex">The blend shape index to transition to. Use -1 to indicate no specific active blend shape.</param>
    /// <param name="newWeight">The target weight (0-100) for the new blend shape.</param>
    private void StartBlendShapeTransition(int newIndex, float newWeight)
    {
        // The currently active blend shape will now start fading out.
        _fadingBlendShapeIndex = _activeBlendShapeIndex;
        
        // Store the current weight of the 'new' active blend shape as its starting point for the transition.
        // If it's a new blend shape, its starting weight is 0.
        _startWeight = (newIndex != -1) ? _skinnedMeshRenderer.GetBlendShapeWeight(newIndex) : 0f;
        
        _activeBlendShapeIndex = newIndex;
        _targetWeight = newWeight;
        _blendTimer = 0f; // Reset blend timer to begin the new transition
    }

    /// <summary>
    /// Updates blend shape weights over time for smooth transitions between phonemes.
    /// This method should be called continuously during lip-sync playback.
    /// </summary>
    private void UpdateBlendShapes()
    {
        if (_skinnedMeshRenderer == null) return;

        // Increment blend timer and calculate normalized time for Lerp
        _blendTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_blendTimer / blendTransitionDuration);

        // Apply weight to the currently active (target) blend shape
        if (_activeBlendShapeIndex != -1)
        {
            float currentActiveWeight = Mathf.Lerp(_startWeight, _targetWeight, t);
            _skinnedMeshRenderer.SetBlendShapeWeight(_activeBlendShapeIndex, currentActiveWeight);
        }

        // Smoothly fade out the previously active blend shape
        if (_fadingBlendShapeIndex != -1 && _fadingBlendShapeIndex != _activeBlendShapeIndex)
        {
            // Get the current weight of the fading blend shape before we modify it
            float currentFadingWeight = _skinnedMeshRenderer.GetBlendShapeWeight(_fadingBlendShapeIndex);
            // Lerp from its current weight towards 0
            float newFadingWeight = Mathf.Lerp(currentFadingWeight, 0f, t); // Use t to ensure a smooth fade-out synchronized with new phoneme fade-in
            _skinnedMeshRenderer.SetBlendShapeWeight(_fadingBlendShapeIndex, newFadingWeight);
        }

        // If the transition is complete, clean up the fading blend shape
        if (t >= 1f)
        {
            if (_fadingBlendShapeIndex != -1 && _fadingBlendShapeIndex != _activeBlendShapeIndex)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(_fadingBlendShapeIndex, 0f); // Ensure it's fully off
            }
            _fadingBlendShapeIndex = -1; // Clear previous blend shape index as it's fully faded
        }
    }

    /// <summary>
    /// Applies the 'Rest' pose if defined in the PhonemeMap, or ensures all mouth blend shapes are off.
    /// This is called when no specific phoneme event is active or between phonemes.
    /// </summary>
    private void ApplyRestPose()
    {
        // Check if the character has a specific 'rest' blend shape defined
        if (_phonemeMap.restBlendShapeIndex != -1)
        {
            // If the current active blend shape is not the rest pose, or it's not already fading out to rest,
            // initiate a transition to the rest pose.
            if (_activeBlendShapeIndex != _phonemeMap.restBlendShapeIndex || _fadingBlendShapeIndex != _phonemeMap.restBlendShapeIndex)
            {
                StartBlendShapeTransition(_phonemeMap.restBlendShapeIndex, _phonemeMap.restTargetWeight);
            }
        }
        else // No specific rest blend shape, so ensure all active mouth shapes are fading out
        {
            // If any blend shape is currently active, transition it to off.
            if (_activeBlendShapeIndex != -1)
            {
                StartBlendShapeTransition(-1, 0f); // Transition to 'no active blend shape' (all off)
            }
        }
    }

    // --- Editor-only helpers for debugging and setup ---
    [ContextMenu("Preview Blend Shapes (Example: 'M')")]
    void PreviewMPhoneme()
    {
        if (_skinnedMeshRenderer == null || _phonemeMap == null)
        {
            Debug.LogWarning("Cannot preview blend shapes: SkinnedMeshRenderer or PhonemeMap not assigned.", this);
            return;
        }

        ResetAllBlendShapes(); // Clear any existing blend shapes
        // Example: Set 'M' blend shape to 100
        int mIndex = _phonemeMap.GetBlendShapeIndex("M"); // Use a phoneme that likely exists
        if (mIndex != -1)
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(mIndex, 100f);
            Debug.Log($"Previewing 'M' phoneme (Blend Shape Index: {mIndex}).");
        }
        else
        {
            Debug.LogWarning("Phoneme 'M' not found in PhonemeMap. Please ensure 'M' is mapped or choose another phoneme to preview.", this);
        }
    }

    [ContextMenu("Reset All Blend Shapes")]
    void ResetAllBlendShapesMenu()
    {
        ResetAllBlendShapes();
        Debug.Log("All blend shapes reset to 0 weight.");
    }
}
```

---

## LipSyncSystem Design Pattern Example Usage in Unity

This example demonstrates how to set up and use the LipSyncSystem.
The pattern separates concerns into:
1.  **LipSyncData (ScriptableObject):** Stores the specific phoneme timings for an audio clip.
    This is the 'what' to animate and 'when'.
2.  **PhonemeMap (ScriptableObject):** Translates generic phonemes (e.g., "M", "A") into
    model-specific blend shape indices and their target weights. This is the 'how' for a specific model.
3.  **LipSyncController (MonoBehaviour):** The runtime component that orchestrates audio playback,
    reads `LipSyncData`, and applies blend shapes using the `PhonemeMap`. This is the 'driver'.

---

### Step-by-Step Setup Guide:

**1. Prepare your 3D Character Model:**
   - Your character model should have a `SkinnedMeshRenderer` component.
   - The `SkinnedMeshRenderer`'s mesh should contain blend shapes for various mouth poses
     (e.g., 'Mouth_M', 'Mouth_A', 'Mouth_O', 'Mouth_Rest').
   - Note down the names or indices of these blend shapes. You can inspect them by selecting
     your character's mesh in the Project view, then looking at the Blend Shapes section
     in the Inspector (under the Mesh property). Each blend shape has an implicit index,
     starting from 0.

**2. Create a 'PhonemeMap' Asset:**
   - In your Unity Project window, right-click -> Create -> LipSync -> **Phoneme Map**.
   - Name it appropriately (e.g., "MyCharacterPhonemeMap").
   - Select the new `PhonemeMap` asset. In the Inspector:
     - Expand the `Mappings` list.
     - Add entries for each phoneme you want to support.
       - **Phoneme:** Enter a descriptive string (e.g., "M", "A", "EE", "OO", "TH", "F", "W", "L"). The exact string used here must match what you put in `LipSyncData`.
       - **Blend Shape Index:** Find the corresponding index from your `SkinnedMeshRenderer`'s blend shapes.
         (You can often find this by expanding the mesh in the Project window and counting,
         or by temporarily putting the model in a scene and inspecting the `SkinnedMeshRenderer`
         component to see blend shape names and their order).
       - **Target Weight:** Set the desired weight (0-100) for this blend shape when active.
         Typically, this is 100 for primary mouth shapes, but can be less for subtle shapes.
     - Set the `Rest Blend Shape Index` and `Rest Target Weight` if your character has a specific
       "mouth closed" or "idle" blend shape. If not, leave `Rest Blend Shape Index` at `-1` and `Rest Target Weight` at `0`, which will default
       to closing all mouth blend shapes when no phoneme is active.

   *Example `PhonemeMap` Entries:*
     - Phoneme: "M", Blend Shape Index: 0, Target Weight: 100
     - Phoneme: "A", Blend Shape Index: 1, Target Weight: 100
     - Phoneme: "EE", Blend Shape Index: 2, Target Weight: 100
     - Phoneme: "OO", Blend Shape Index: 3, Target Weight: 100
     - Phoneme: "F", Blend Shape Index: 4, Target Weight: 100
     - Phoneme: "TH", Blend Shape Index: 5, Target Weight: 100
     - Rest Blend Shape Index: 6, Rest Target Weight: 100 (if you have a dedicated 'Mouth_Closed' or 'Rest' blend shape)

**3. Create 'LipSyncData' Assets:**
   - For each audio clip you want to lip-sync, you'll need a `LipSyncData` asset.
   - Right-click -> Create -> LipSync -> **LipSync Data**.
   - Name it after its corresponding audio clip (e.g., "HelloWordLipSyncData").
   - Select the new `LipSyncData` asset. In the Inspector:
     - Expand the `Phoneme Events` list.
     - Add entries for each phoneme in the speech. **Ensure phoneme strings match those in your `PhonemeMap` (case-insensitive).**
       - **Phoneme:** Match the phoneme string you defined in your `PhonemeMap` (e.g., "H", "EH", "L", "O", "W", "ER", "L", "D").
       - **Start Time:** The time in seconds from the start of the audio clip when this phoneme should begin.
       - **Duration:** The estimated duration of the phoneme. This helps the system determine when to transition to the next or to the rest pose.

   *Example `LipSyncData` for "Hello World" audio clip:*
     - Phoneme: "H", Start Time: 0.0s, Duration: 0.1s
     - Phoneme: "EH", Start Time: 0.1s, Duration: 0.15s
     - Phoneme: "L", Start Time: 0.25s, Duration: 0.1s
     - Phoneme: "O", Start Time: 0.35s, Duration: 0.2s
     - Phoneme: "W", Start Time: 0.6s, Duration: 0.1s
     - Phoneme: "ER", Start Time: 0.7s, Duration: 0.15s
     - Phoneme: "L", Start Time: 0.85s, Duration: 0.1s
     - Phoneme: "D", Start Time: 0.95s, Duration: 0.15s

   *(Note: Generating this data accurately usually requires specialized lip-sync software or manual phonetic transcription and timing. For testing, rough timings are fine.)*

**4. Add 'LipSyncController' to your Character GameObject:**
   - Select your character GameObject in the scene hierarchy.
   - Add Component -> search for "LipSyncController".
   - Drag and drop the necessary references in the Inspector:
     - **Audio Source:** (Should be automatically added by `RequireComponent`, or drag an existing one).
     - **Skinned Mesh Renderer:** Drag the `SkinnedMeshRenderer` component from your character (usually a child GameObject) into this slot.
     - **Phoneme Map:** Drag your created `MyCharacterPhonemeMap` asset here.
     - Adjust `Blend Transition Duration` if needed (e.g., 0.05 for fast, 0.2 for slower blending).

**5. Trigger Lip-Sync Programmatically:**
   - Create a new C# script (e.g., `LipSyncTrigger`) and attach it to an empty GameObject or directly to your character.
   - In this script, get a reference to your `LipSyncController`.
   - In `Start()` or on a button press, call `LipSyncController.PlayLipSync()`.

   *Example `LipSyncTrigger.cs` script:*
   ```csharp
   using UnityEngine;

   public class LipSyncTrigger : MonoBehaviour
   {
       [Header("References")]
       public LipSyncController lipSyncController;
       [Tooltip("The audio clip containing the speech.")]
       public AudioClip speechAudioClip;
       [Tooltip("The LipSyncData asset corresponding to the speechAudioClip.")]
       public LipSyncData speechLipSyncData;

       void Start()
       {
           // Basic validation
           if (lipSyncController == null)
           {
               Debug.LogError("LipSyncTrigger: LipSyncController not assigned! Drag the LipSyncController component here.", this);
               return;
           }
           if (speechAudioClip == null)
           {
               Debug.LogError("LipSyncTrigger: Speech AudioClip not assigned! Drag an AudioClip here.", this);
               return;
           }
           if (speechLipSyncData == null)
           {
               Debug.LogError("LipSyncTrigger: Speech LipSyncData not assigned! Drag a LipSyncData asset here.", this);
               return;
           }

           // Automatically play lip sync on start for demonstration purposes
           Debug.Log("Playing lip sync on start...");
           lipSyncController.PlayLipSync(speechAudioClip, speechLipSyncData);
       }

       // You could also add a button to trigger it:
       // void Update()
       // {
       //     if (Input.GetKeyDown(KeyCode.Space))
       //     {
       //         Debug.Log("Playing lip sync via Spacebar...");
       //         lipSyncController.PlayLipSync(speechAudioClip, speechLipSyncData);
       //     }
       // }

       // Optional: Stop lip sync on disable or destroy
       void OnDisable()
       {
           if (lipSyncController != null)
           {
               lipSyncController.StopLipSync();
           }
       }
   }
   ```
   - On the GameObject with `LipSyncTrigger`, drag:
     - Your `LipSyncController` component into the `Lip Sync Controller` slot.
     - Your `AudioClip` (e.g., "HelloWorld.wav") into the `Speech Audio Clip` slot.
     - Your `HelloWordLipSyncData` asset into the `Speech Lip Sync Data` slot.

---

### Understanding the Pattern:

-   **Data-Driven:** The `LipSyncData` and `PhonemeMap` are pure data assets. They don't contain any runtime logic. This makes them highly reusable and configurable without changing code. You can swap out `LipSyncData` for different dialogue lines, or `PhonemeMap` for different character models, all in the Inspector.
-   **Separation of Concerns:**
    -   `LipSyncData` defines *what* mouth shapes occur and *when*.
    -   `PhonemeMap` defines *how* those mouth shapes map to a specific 3D model's blend shapes.
    -   `LipSyncController` is the *engine* that reads this data and drives the animation.
    -   This clear separation makes the system modular, easy to maintain, and testable.
-   **Extensibility:**
    -   Want to support different animation types (e.g., bone-based, 2D sprites)? You could extend `PhonemeMap` or create different `ILipAnimator` interfaces and implementations, and `LipSyncController` would use the appropriate one.
    -   Want to import lip-sync data from external formats (e.g., a `.lip` file or a text file)? You would add a method to `LipSyncData` or a separate utility class to parse that file into the `PhonemeEvent` list.
-   **Performance:** Using `ScriptableObject`s avoids recreating data in memory at runtime for each instance. The `LipSyncController` uses a Coroutine to manage timing, which is generally efficient for time-based events compared to constantly polling in `Update()` for every single event in a long list. Blending is done with `Mathf.Lerp` for smooth transitions.

This 'LipSyncSystem' pattern is robust, flexible, and scalable for various Unity projects requiring character speech animation.