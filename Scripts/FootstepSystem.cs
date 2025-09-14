// Unity Design Pattern Example: FootstepSystem
// This script demonstrates the FootstepSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the 'FootstepSystem' design pattern, offering a robust and extensible way to handle character footstep sounds based on the surface they are walking on.

The pattern decouples the character's movement logic from the sound-playing and surface-detection logic, making it easy to manage and extend.

---

### **FootstepSystem Design Pattern Explained:**

1.  **`SurfaceType` (Enum):** Defines the different types of ground surfaces the system recognizes (e.g., Grass, Wood, Stone).
2.  **`SurfaceTypeDetector` (Component):** Attached to ground GameObjects (like terrain, floors, etc.), this component explicitly tells the system what kind of surface that object represents.
3.  **`FootstepAudioSettings` (ScriptableObject):** This is a data asset that holds all the audio clips and sound properties (volume, pitch variation) for each `SurfaceType`. This centralizes sound configuration and separates data from code.
4.  **`FootstepSystem` (Singleton Manager):** This is the core of the system.
    *   It's a singleton, meaning only one instance exists in the scene, providing a global access point.
    *   It manages a pool of `AudioSource` components to efficiently play sounds without constant creation/destruction.
    *   It uses `Physics.Raycast` to detect the `SurfaceTypeDetector` under the character's feet.
    *   It retrieves the appropriate audio clips from the `FootstepAudioSettings` and plays a random one with specified variations.
5.  **`FootstepEmitter` (Character Component):** This component is attached to any character (player or NPC) that needs to make footstep sounds.
    *   It does *not* play sounds directly. Instead, when a footstep event occurs (e.g., triggered by an animation event or character controller logic), it simply notifies the `FootstepSystem` to play a sound at its current position.
    *   This keeps the character's script clean and unaware of the complexities of sound playing or surface detection.

---

### **How to Use This in Your Unity Project:**

1.  **Create the Scripts:**
    *   Create a new C# script named `FootstepSystem.cs` and paste the entire code below into it.
    *   This single file contains all the necessary classes and enums.

2.  **Create Footstep Audio Settings Asset:**
    *   In your Unity Project window, right-click -> Create -> Audio -> Footstep Audio Settings.
    *   Name it something descriptive, e.g., "MyGameFootstepSettings".

3.  **Configure Footstep Audio Settings:**
    *   Select your "MyGameFootstepSettings" asset.
    *   In the Inspector, increase the size of the `Surface Audio Settings` array.
    *   For each element:
        *   Choose a `Surface Type` from the dropdown (e.g., `Grass`, `Wood`, `Stone`).
        *   Drag your `AudioClip`s (short footstep sound effects) into the `Audio Clips` array. You can add multiple clips for variety.
        *   Adjust `Volume`, `Min Pitch`, and `Max Pitch` for each surface type.
    *   **Crucially, always include an entry for `SurfaceType.Default`** with some generic footstep sounds. This acts as a fallback if no specific surface type is detected.

4.  **Setup the `FootstepSystem` in Your Scene:**
    *   Create an empty GameObject in your scene (you might put it under a "Managers" GameObject).
    *   Add the `FootstepSystem` component to it.
    *   Drag your "MyGameFootstepSettings" asset into the `Audio Settings` field.
    *   Set `Initial Audio Source Pool Size` (e.g., 5-10 concurrent footstep sounds).
    *   Adjust `Raycast Height Offset` (how far above the foot to start the raycast) and `Raycast Distance` (how far down to check for ground).
    *   **Set the `Ground Layer` mask:** Select all layers that your ground objects (terrain, floors, ramps, etc.) are on. This ensures the raycast only hits relevant surfaces.

5.  **Setup Ground Objects:**
    *   For every GameObject that acts as ground (e.g., your terrain, floor prefabs, stone paths), add the `SurfaceTypeDetector` component.
    *   In the Inspector of the `SurfaceTypeDetector`, set its `Surface Type` property to match the material (e.g., `Grass` for your terrain, `Wood` for a wooden floor, `Stone` for a rock path).
    *   **Ensure these ground objects are on one of the layers specified in the `FootstepSystem`'s `Ground Layer` mask.**

6.  **Setup Your Character (Player/NPC):**
    *   On your character's root GameObject, add the `FootstepEmitter` component.
    *   If your character has a rig, drag any `Transform`s representing the feet (e.g., `LeftFoot`, `RightFoot` bones) into the `Footstep Points` array. These transforms will be used to determine the exact position for the raycast. If you only need one point, just use the character's root transform or create an empty GameObject at the character's base.
    *   (Optional) If you want a specific character to always play a particular surface type regardless of the ground, check `Use Manual Surface Type` and set the `Manual Surface Type`.

7.  **Trigger Footsteps (Animation Events Recommended):**
    *   Open your character's walk/run animation clip in the Unity Animation window.
    *   At the keyframes where a foot visibly hits the ground, add an `Animation Event`.
    *   In the Animation Event settings:
        *   Set the `Function` to `OnFootstep`.
        *   Set the `Int` parameter to the index of the corresponding foot in the `Footstep Points` array of your `FootstepEmitter` (e.g., `0` for the left foot, `1` for the right foot if you have two points). This allows the sound to be played precisely at the foot's position.
    *   Alternatively, you can call `GetComponent<FootstepEmitter>().OnFootstep(footIndex)` directly from your character movement script whenever you determine a footstep event should occur.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// This single file contains all necessary components for the FootstepSystem pattern.

namespace MyGame.Audio
{
    // 1. SurfaceType Enum: Defines the types of surfaces recognized by the system.
    //    Add more types as needed for your game.
    public enum SurfaceType
    {
        Default, // Fallback surface type if no specific type is detected.
        Grass,
        Wood,
        Stone,
        Metal,
        Sand,
        Water,
        Concrete
        // Add more surface types here
    }

    // 2. SurfaceTypeDetector Component: Attach this to ground objects to define their surface type.
    //    Example: Attach to your terrain and set Surface Type to 'Grass'.
    //    Example: Attach to a wooden floor prefab and set Surface Type to 'Wood'.
    public class SurfaceTypeDetector : MonoBehaviour
    {
        [Tooltip("The type of surface this GameObject represents.")]
        public SurfaceType surfaceType = SurfaceType.Default;
    }

    // FootstepAudioData: A serializable class to hold audio settings for a specific surface type.
    // This is used within the ScriptableObject for easy configuration in the Inspector.
    [System.Serializable]
    public class FootstepAudioData
    {
        [Tooltip("The surface type this audio data belongs to.")]
        public SurfaceType surfaceType;

        [Tooltip("An array of audio clips for this surface type. One will be chosen randomly.")]
        public AudioClip[] audioClips;

        [Range(0f, 1f)]
        [Tooltip("The base volume for footsteps on this surface.")]
        public float volume = 0.7f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Minimum pitch variation for footsteps on this surface.")]
        public float minPitch = 0.9f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Maximum pitch variation for footsteps on this surface.")]
        public float maxPitch = 1.1f;
    }

    // 3. FootstepAudioSettings ScriptableObject: A data asset to store all footstep sound configurations.
    //    Create this asset in your project (Right-click -> Create -> Audio -> Footstep Audio Settings).
    [CreateAssetMenu(fileName = "FootstepAudioSettings", menuName = "Audio/Footstep Audio Settings", order = 1)]
    public class FootstepAudioSettings : ScriptableObject
    {
        [Tooltip("List of audio data configurations for different surface types.")]
        public FootstepAudioData[] surfaceAudioSettings;

        /// <summary>
        /// Retrieves the FootstepAudioData for a given SurfaceType.
        /// Falls back to SurfaceType.Default if the specific type is not found.
        /// </summary>
        /// <param name="type">The SurfaceType to get data for.</param>
        /// <returns>The FootstepAudioData for the given type, or Default if not found.</returns>
        public FootstepAudioData GetFootstepAudioData(SurfaceType type)
        {
            foreach (var data in surfaceAudioSettings)
            {
                if (data.surfaceType == type)
                {
                    return data;
                }
            }

            // Fallback to Default if the requested type isn't explicitly configured.
            foreach (var data in surfaceAudioSettings)
            {
                if (data.surfaceType == SurfaceType.Default)
                {
                    Debug.LogWarning($"FootstepAudioSettings: No specific audio data found for {type}. Using Default.");
                    return data;
                }
            }

            Debug.LogError("FootstepAudioSettings: No audio data found for 'Default' surface type. Please ensure a 'Default' entry exists.");
            return null; // Should not happen if Default is always provided.
        }
    }

    // 4. FootstepSystem MonoBehaviour (Singleton): The central manager for playing footstep sounds.
    //    Place this script on a dedicated GameObject in your scene (e.g., "GameManagers").
    public class FootstepSystem : MonoBehaviour
    {
        // Singleton pattern for easy global access.
        public static FootstepSystem Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("The ScriptableObject containing all footstep audio configurations.")]
        public FootstepAudioSettings audioSettings;

        [Tooltip("Initial number of AudioSources to create for the pool. Increase if many footsteps play simultaneously.")]
        public int initialAudioSourcePoolSize = 5;

        [Tooltip("Height offset from the character's foot position to start the raycast downwards.")]
        public float raycastHeightOffset = 0.1f; // Start raycast slightly above foot.

        [Tooltip("Maximum distance for the raycast to detect ground surfaces.")]
        public float raycastDistance = 0.5f;     // How far down to check for ground.

        [Tooltip("The LayerMask for ground objects. Only objects on these layers will be considered for surface detection.")]
        public LayerMask groundLayer;            // Which layers to consider as ground.

        // Internal pooling system for AudioSources to avoid constant instantiation/destruction.
        private List<AudioSource> audioSourcePool;
        private List<bool> audioSourceBusy; // Tracks which pooled AudioSources are currently playing.

        private void Awake()
        {
            // Enforce singleton pattern.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Optional: Keep the system alive across scene loads if desired.
            // If you destroy and re-create it per scene, remove this.
            DontDestroyOnLoad(gameObject);

            InitializeAudioSourcePool();
        }

        /// <summary>
        /// Initializes the pool of AudioSources that the system will use.
        /// </summary>
        private void InitializeAudioSourcePool()
        {
            audioSourcePool = new List<AudioSource>();
            audioSourceBusy = new List<bool>();

            for (int i = 0; i < initialAudioSourcePoolSize; i++)
            {
                CreateNewAudioSource();
            }
        }

        /// <summary>
        /// Creates a new AudioSource GameObject and adds it to the pool.
        /// </summary>
        /// <returns>The newly created AudioSource.</returns>
        private AudioSource CreateNewAudioSource()
        {
            GameObject obj = new GameObject("PooledAudioSource");
            obj.transform.parent = this.transform; // Parent to the FootstepSystem for organization.
            AudioSource audioSource = obj.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Enable 3D sound.
            audioSource.playOnAwake = false; // Don't play automatically.
            audioSource.outputAudioMixerGroup = null; // Can be set to a specific mixer group if desired.
            audioSourcePool.Add(audioSource);
            audioSourceBusy.Add(false);
            return audioSource;
        }

        /// <summary>
        /// Gets an available AudioSource from the pool. If none are free, it creates a new one.
        /// </summary>
        /// <returns>An available AudioSource component.</returns>
        private AudioSource GetAvailableAudioSource()
        {
            for (int i = 0; i < audioSourcePool.Count; i++)
            {
                if (!audioSourceBusy[i])
                {
                    audioSourceBusy[i] = true;
                    // Start a coroutine to mark this AudioSource as free after its current clip finishes playing.
                    StartCoroutine(MarkAudioSourceFreeAfterClip(audioSourcePool[i], i));
                    return audioSourcePool[i];
                }
            }

            // If no available sources, expand the pool by creating a new one.
            Debug.LogWarning("FootstepSystem: Increasing AudioSource pool size dynamically. Consider increasing 'Initial Audio Source Pool Size'.");
            AudioSource newSource = CreateNewAudioSource();
            int newIndex = audioSourcePool.Count - 1; // Index of the newly added source.
            audioSourceBusy[newIndex] = true;
            StartCoroutine(MarkAudioSourceFreeAfterClip(newSource, newIndex));
            return newSource;
        }

        /// <summary>
        /// Coroutine to wait for an AudioSource to finish playing and then mark it as free in the pool.
        /// </summary>
        /// <param name="source">The AudioSource to monitor.</param>
        /// <param name="index">The index of the AudioSource in the pool's busy list.</param>
        private IEnumerator MarkAudioSourceFreeAfterClip(AudioSource source, int index)
        {
            // Wait until the audio clip finishes playing.
            while (source != null && source.isPlaying) // Check for null in case source is destroyed.
            {
                yield return null;
            }

            // Mark the source as free.
            if (index < audioSourceBusy.Count) // Ensure index is still valid.
            {
                audioSourceBusy[index] = false;
            }
        }

        /// <summary>
        /// Public method to play a footstep sound. It automatically detects the surface type
        /// at the given position using a raycast. This is the most common way to call it.
        /// </summary>
        /// <param name="position">The world position where the footstep occurred (e.g., character's foot position).</param>
        public void PlayFootstepSound(Vector3 position)
        {
            SurfaceType detectedType = DetectSurfaceType(position);
            PlayFootstepSound(position, detectedType);
        }

        /// <summary>
        /// Public method to play a footstep sound for a specific, pre-determined surface type.
        /// Use this if you already know the surface type (e.g., from a collision event or manual override).
        /// </summary>
        /// <param name="position">The world position where the footstep occurred.</param>
        /// <param name="surfaceType">The specific type of surface to play the sound for.</param>
        public void PlayFootstepSound(Vector3 position, SurfaceType surfaceType)
        {
            if (audioSettings == null)
            {
                Debug.LogWarning("FootstepSystem: audioSettings is not assigned in the Inspector. Cannot play footstep sound.");
                return;
            }

            // Get audio data for the detected or specified surface type.
            FootstepAudioData data = audioSettings.GetFootstepAudioData(surfaceType);
            if (data == null || data.audioClips == null || data.audioClips.Length == 0)
            {
                // GetFootstepAudioData already handles fallback to Default, so if it's still null here,
                // it means even Default is missing or has no clips.
                Debug.LogWarning($"FootstepSystem: No valid audio data or clips found for surface type: {surfaceType} (and no Default fallback). Aborting footstep sound.");
                return;
            }

            // Pick a random audio clip from the available ones for this surface.
            AudioClip clip = data.audioClips[Random.Range(0, data.audioClips.Length)];
            if (clip == null)
            {
                Debug.LogWarning($"FootstepSystem: A null AudioClip was found for {surfaceType}. Skipping sound.");
                return;
            }

            // Get an available AudioSource from the pool.
            AudioSource source = GetAvailableAudioSource();
            if (source == null)
            {
                Debug.LogWarning("FootstepSystem: Could not get an available AudioSource from the pool.");
                return;
            }

            // Configure and play the AudioSource.
            source.transform.position = position; // Position the 3D sound source at the footstep location.
            source.clip = clip;
            source.volume = data.volume;
            source.pitch = Random.Range(data.minPitch, data.maxPitch);
            source.Play();
        }

        /// <summary>
        /// Performs a raycast downwards from the given position to detect the surface type.
        /// </summary>
        /// <param name="position">The starting position for the raycast (e.g., character's foot).</param>
        /// <returns>The detected SurfaceType, or SurfaceType.Default if none is found or detected.</returns>
        private SurfaceType DetectSurfaceType(Vector3 position)
        {
            RaycastHit hit;
            // Adjust raycast origin slightly upwards to ensure it doesn't start inside the collider.
            Vector3 rayOrigin = position + Vector3.up * raycastHeightOffset;

            // Perform the raycast downwards.
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance + raycastHeightOffset, groundLayer))
            {
                // Check if the hit object has a SurfaceTypeDetector component.
                SurfaceTypeDetector detector = hit.collider.GetComponent<SurfaceTypeDetector>();
                if (detector != null)
                {
                    return detector.surfaceType;
                }
                else
                {
                    Debug.LogWarning($"FootstepSystem: Ground object '{hit.collider.name}' at {hit.point} does not have a SurfaceTypeDetector. Using Default.", hit.collider.gameObject);
                }
            }
            return SurfaceType.Default; // Fallback if no surface is detected or no detector found.
        }

        // Optional: Draws the raycast in the editor for debugging purposes.
        private void OnDrawGizmosSelected()
        {
            // Only draw gizmos in editor when not playing.
            if (Application.isPlaying) return;

            Gizmos.color = Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * raycastHeightOffset;
            Gizmos.DrawRay(rayOrigin, Vector3.down * (raycastDistance + raycastHeightOffset));
            Gizmos.DrawSphere(rayOrigin, 0.02f); // Small sphere at raycast origin.
        }
    }

    // 5. FootstepEmitter MonoBehaviour: Component attached to characters to trigger footsteps.
    //    This is the interface for character movement/animation to interact with the FootstepSystem.
    public class FootstepEmitter : MonoBehaviour
    {
        [Header("Emitter Settings")]
        [Tooltip("Optional: Transforms representing the actual foot positions (e.g., foot bones). Used for accurate raycasting.")]
        public Transform[] footstepPoints; // e.g., LeftFoot, RightFoot bones/empty GameObjects.

        [Tooltip("If true, this emitter will always use the 'Manual Surface Type' instead of raycasting.")]
        public bool useManualSurfaceType = false; // Overrides automatic detection.

        [Tooltip("The surface type to use if 'Use Manual Surface Type' is checked.")]
        public SurfaceType manualSurfaceType = SurfaceType.Default; // Used if useManualSurfaceType is true.

        /// <summary>
        /// Call this method (e.g., via an Animation Event or from a character controller)
        /// when a footstep sound should occur. It delegates the sound playing to the FootstepSystem.
        /// </summary>
        /// <param name="footIndex">
        /// The index of the footstep point to use from the 'footstepPoints' array.
        /// For example, 0 for left foot, 1 for right foot. If -1 or out of bounds,
        /// it defaults to the emitter's GameObject position.
        /// </param>
        public void OnFootstep(int footIndex = 0)
        {
            if (FootstepSystem.Instance == null)
            {
                Debug.LogWarning("FootstepEmitter: FootstepSystem instance not found in scene. Cannot play footstep sound. " +
                                 "Ensure FootstepSystem is present and initialized.", this);
                return;
            }

            // Determine the precise position for the footstep sound and raycast.
            Vector3 footPosition = transform.position; // Default to character's base position.
            if (footstepPoints != null && footstepPoints.Length > 0 && footIndex >= 0 && footIndex < footstepPoints.Length)
            {
                // Use the specific foot transform's position if available.
                if (footstepPoints[footIndex] != null)
                {
                    footPosition = footstepPoints[footIndex].position;
                }
            }

            // Delegate playing the sound to the FootstepSystem.
            if (useManualSurfaceType)
            {
                FootstepSystem.Instance.PlayFootstepSound(footPosition, manualSurfaceType);
            }
            else
            {
                FootstepSystem.Instance.PlayFootstepSound(footPosition);
            }
        }

        // To visualize the footstep points in the editor.
        private void OnDrawGizmosSelected()
        {
            if (footstepPoints == null) return;
            Gizmos.color = Color.cyan;
            foreach (Transform point in footstepPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.03f); // Small sphere at each point.
                    // Draw a short line downwards to indicate the raycast direction.
                    Gizmos.DrawLine(point.position, point.position + Vector3.down * 0.1f);
                }
            }
        }

        /*
        // Example Usage (for character controller, not animation events):
        // This code would typically be in your character movement script, not FootstepEmitter itself.
        private float lastFootstepTime = 0f;
        private float footstepInterval = 0.3f; // Adjust based on character speed/animation.

        void Update()
        {
            // Simulate character moving and needing a footstep.
            bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

            if (isMoving && Time.time - lastFootstepTime > footstepInterval)
            {
                // In a real game, you'd integrate this with your animation system
                // or actual foot down events. Here, we just alternate feet.
                int currentFootIndex = Random.Range(0, footstepPoints.Length); // Or alternate 0 and 1.
                OnFootstep(currentFootIndex);
                lastFootstepTime = Time.time;
            }
        }
        */
    }
}
```