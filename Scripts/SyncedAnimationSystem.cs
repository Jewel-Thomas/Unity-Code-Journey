// Unity Design Pattern Example: SyncedAnimationSystem
// This script demonstrates the SyncedAnimationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **SyncedAnimationSystem** design pattern in Unity provides a robust way to manage and synchronize animations across multiple `Animator` components that are part of a single logical entity (e.g., a complex character with separate models for body, weapon, and accessories, each with its own `Animator`).

Instead of a `PlayerController` having to find and explicitly call `animator.Play()` on several different `Animator` components, it simply sends a generic animation command to a central system. This system then broadcasts the command to all registered animation players, which each interpret the command based on their own specific `Animator` and animation clips.

This pattern promotes:
1.  **Decoupling:** The `PlayerController` doesn't need to know *how many* `Animator` components exist or what their specific animation clip names are.
2.  **Maintainability:** Adding or removing an animated part of the character (e.g., a new shield with its own animations) only requires adding/removing a `SyncedAnimationPlayer` component, not changing core logic.
3.  **Flexibility:** Each `SyncedAnimationPlayer` can map a generic animation key (e.g., "Attack") to a *different* animation clip name (e.g., "Body_Attack", "Weapon_Attack") and even play it on a different `Animator` layer or starting time.

---

### **How to Use This Example in Your Unity Project:**

1.  **Create C# Scripts:**
    *   Create a new C# script named `SyncedAnimationSystem.cs` and copy the code for `SyncedAnimationSystem` into it.
    *   Create another C# script named `SyncedAnimationPlayer.cs` and copy the code for `SyncedAnimationPlayer` into it.

2.  **Set up the SyncedAnimationSystem (The Manager):**
    *   Create an empty GameObject in your scene (e.g., "AnimationSystem").
    *   Attach the `SyncedAnimationSystem.cs` script to this GameObject. This will be your central animation manager.

3.  **Set up Your Character (The Players):**
    *   Imagine you have a character GameObject (e.g., "PlayerCharacter") that has child GameObjects for its `Body`, `Weapon`, and `Shield`.
    *   **For each child GameObject that has an `Animator`:**
        *   Attach an `Animator` component to it (if it doesn't already have one) and assign an `Animator Controller` with relevant animation clips.
        *   Attach the `SyncedAnimationPlayer.cs` script to this same child GameObject.
        *   In the Inspector for the `SyncedAnimationPlayer` component:
            *   Drag its own `Animator` component into the `Target Animator` slot (or leave it empty if the Animator is on the same GameObject, as `Awake` will try to find it).
            *   **Populate the `Animation Mappings` list:**
                *   Click the "+" button to add new mappings.
                *   **Animation Key:** Enter a generic name (e.g., "Attack", "Idle", "Walk", "Jump"). This is what your `PlayerController` will use.
                *   **Animation Clip Name:** Enter the *exact name* of the animation clip within *this specific Animator's controller* that corresponds to the `Animation Key`.
                *   **Animator Layer:** (Optional) Set a specific layer for *this part* of the animation. Use `-1` to use the layer provided by the `PlaySyncedAnimation` call.
                *   **Normalized Start Time:** (Optional) Set a specific normalized start time for *this part* of the animation. Use `-1f` to use the time provided by the `PlaySyncedAnimation` call.

    *   **Example Character Setup:**
        *   `PlayerCharacter` (Empty GameObject)
            *   `BodyModel` (Mesh + `Animator` + `SyncedAnimationPlayer`)
                *   `Animation Mappings` for `BodyModel`:
                    *   Key: "Attack", Clip: "Player_Body_Attack", Layer: 0, Start Time: 0
                    *   Key: "Idle", Clip: "Player_Body_Idle", Layer: 0, Start Time: 0
            *   `WeaponModel` (Mesh + `Animator` + `SyncedAnimationPlayer`)
                *   `Animation Mappings` for `WeaponModel`:
                    *   Key: "Attack", Clip: "Player_Weapon_Attack", Layer: 1 (for additive blending), Start Time: 0
                    *   Key: "Idle", Clip: "Player_Weapon_Idle", Layer: 0, Start Time: 0
            *   `ShieldModel` (Mesh + `Animator` + `SyncedAnimationPlayer`)
                *   `Animation Mappings` for `ShieldModel`:
                    *   Key: "Attack", Clip: "Player_Shield_Block", Layer: 0, Start Time: 0 // Shield might block during attack.
                    *   Key: "Idle", Clip: "Player_Shield_Relax", Layer: 0, Start Time: 0

4.  **Trigger Animations from Your Game Logic:**
    *   From any script (e.g., your `PlayerController.cs`), you can now trigger a synchronized animation like this:

    ```csharp
    // Example: PlayerCharacterController.cs
    using UnityEngine;

    public class PlayerCharacterController : MonoBehaviour
    {
        // Reference to the SyncedAnimationSystem (optional, can also use SyncedAnimationSystem.Instance directly)
        // private SyncedAnimationSystem _animationSystem;

        void Start()
        {
            // If you need to ensure the system exists or want a direct reference
            // _animationSystem = FindObjectOfType<SyncedAnimationSystem>();
            // if (_animationSystem == null)
            // {
            //     Debug.LogError("SyncedAnimationSystem not found in scene!");
            //     enabled = false;
            // }
        }

        void Update()
        {
            // Example: Trigger attack animation on mouse click
            if (Input.GetMouseButtonDown(0))
            {
                // Play the synced "Attack" animation. All registered SyncedAnimationPlayers
                // will attempt to play their specific "Attack" mapped animation.
                // You can also specify a default layer and normalized time for all players.
                if (SyncedAnimationSystem.Instance != null)
                {
                    SyncedAnimationSystem.Instance.PlaySyncedAnimation("Attack", 0, 0f);
                    Debug.Log("Triggering Synced 'Attack' Animation!");
                }
                else
                {
                    Debug.LogError("SyncedAnimationSystem.Instance is null. Is it in the scene and active?");
                }
            }

            // Example: Trigger idle animation when no input
            // (You'd typically have more complex state machine for this)
            if (Input.GetKeyUp(KeyCode.Space))
            {
                 if (SyncedAnimationSystem.Instance != null)
                {
                    SyncedAnimationSystem.Instance.PlaySyncedAnimation("Idle");
                    Debug.Log("Triggering Synced 'Idle' Animation!");
                }
            }

            // Example: Trigger walk animation
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (SyncedAnimationSystem.Instance != null)
                {
                    SyncedAnimationSystem.Instance.PlaySyncedAnimation("Walk");
                    Debug.Log("Triggering Synced 'Walk' Animation!");
                }
            }
        }
    }
    ```

---

### **The SyncedAnimationSystem Code:**

This script acts as the central orchestrator. It's a singleton, meaning there's only one instance throughout your game, making it easy for other scripts to find and interact with it.

```csharp
// SyncedAnimationSystem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The central hub for the Synced Animation System design pattern.
/// Manages and orchestrates animation playback across multiple SyncedAnimationPlayer components.
/// This acts as a singleton to allow easy access from any part of the game.
/// </summary>
public class SyncedAnimationSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    // 'private set' ensures that only this class can assign the Instance,
    // while others can only read it.
    public static SyncedAnimationSystem Instance { get; private set; }

    // A list of all SyncedAnimationPlayer components currently registered with the system.
    // Using 'readonly' ensures the list itself cannot be reassigned after initialization,
    // though its contents can still be modified.
    private readonly List<SyncedAnimationPlayer> _registeredPlayers = new List<SyncedAnimationPlayer>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where the singleton pattern is enforced. It checks if an instance already exists,
    /// and if not, assigns itself as the Instance. If a duplicate is found, it destroys itself.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SyncedAnimationSystem instances found! Destroying duplicate.", this);
            Destroy(this.gameObject); // Destroy the new GameObject if a system already exists.
        }
        else
        {
            Instance = this; // Assign this instance as the singleton.
            // Uncomment the line below if this system should persist across scene loads.
            // DontDestroyOnLoad(this.gameObject);
        }
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Clears the singleton instance if this is the active instance to prevent stale references.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a SyncedAnimationPlayer with the system.
    /// Once registered, this player will receive animation commands from the SyncedAnimationSystem.
    /// Players typically register themselves in their OnEnable method.
    /// </summary>
    /// <param name="player">The SyncedAnimationPlayer component to register.</param>
    public void RegisterPlayer(SyncedAnimationPlayer player)
    {
        if (player == null)
        {
            Debug.LogError("Attempted to register a null SyncedAnimationPlayer.", this);
            return;
        }
        if (!_registeredPlayers.Contains(player)) // Prevent duplicate registrations.
        {
            _registeredPlayers.Add(player);
            // Debug.Log($"Registered player: {player.name}. Total players: {_registeredPlayers.Count}", this);
        }
    }

    /// <summary>
    /// Deregisters a SyncedAnimationPlayer from the system.
    /// This player will no longer receive animation commands from the SyncedAnimationSystem.
    /// Players typically deregister themselves in their OnDisable method.
    /// </summary>
    /// <param name="player">The SyncedAnimationPlayer component to deregister.</param>
    public void DeregisterPlayer(SyncedAnimationPlayer player)
    {
        if (player == null)
        {
            // Debug.LogWarning("Attempted to deregister a null SyncedAnimationPlayer.", this);
            return;
        }
        if (_registeredPlayers.Contains(player))
        {
            _registeredPlayers.Remove(player);
            // Debug.Log($"Deregistered player: {player.name}. Total players: {_registeredPlayers.Count}", this);
        }
    }

    /// <summary>
    /// Plays a synchronized animation across all registered SyncedAnimationPlayer components.
    /// Each registered player will receive this command and attempt to play an animation
    /// mapped to the given 'animationKey' on its own Animator.
    /// </summary>
    /// <param name="animationKey">The common, generic key identifying the animation to play
    /// (e.g., "Attack", "Walk", "Jump").</param>
    /// <param name="layer">The default Animator layer to play the animation on for players
    /// that don't specify their own layer in their mapping.</param>
    /// <param name="normalizedTime">The default normalized time at which to start playing
    /// the animation (0 for beginning) for players that don't specify their own start time.</param>
    public void PlaySyncedAnimation(string animationKey, int layer = 0, float normalizedTime = 0f)
    {
        if (string.IsNullOrWhiteSpace(animationKey))
        {
            Debug.LogWarning("Attempted to play a synced animation with a null or empty key.", this);
            return;
        }

        // Iterate through all currently registered players and tell each one to play the animation.
        foreach (var player in _registeredPlayers)
        {
            player.PlayAnimation(animationKey, layer, normalizedTime);
        }
    }
}
```

---

### **The SyncedAnimationPlayer Code:**

This script is attached to individual GameObjects that have their own `Animator`. It acts as a bridge, translating the generic animation commands from the `SyncedAnimationSystem` into specific actions for its `Animator`.

```csharp
// SyncedAnimationPlayer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single animation source (e.g., an Animator component on a character's body, weapon, etc.)
/// that participates in the Synced Animation System. It maps generic animation keys to specific animation clip names
/// and parameters for its own Animator.
/// </summary>
public class SyncedAnimationPlayer : MonoBehaviour
{
    /// <summary>
    /// Defines a mapping between a generic animation key (e.g., "Attack")
    /// and the actual animation clip name used by this specific Animator (e.g., "Player_Attack_Body").
    /// This struct makes the mappings visible and editable in the Unity Inspector.
    /// </summary>
    [System.Serializable] // Makes this struct editable in the Inspector.
    public struct AnimationMapping
    {
        [Tooltip("A generic key to identify the animation (e.g., 'Attack', 'Idle', 'Run'). " +
                 "This key is used by the SyncedAnimationSystem to command this player.")]
        public string animationKey;

        [Tooltip("The actual name of the animation clip in this Animator's controller " +
                 "that corresponds to the generic animationKey.")]
        public string animationClipName;

        [Tooltip("Optional: The Animator layer on which this specific animation should play. " +
                 "Set to -1 to use the default layer provided by the PlaySyncedAnimation call " +
                 "from the SyncedAnimationSystem.")]
        public int animatorLayer; // Allows specific parts to animate on specific layers.

        [Tooltip("Optional: The normalized time (0.0 to 1.0) at which to start playing this specific animation. " +
                 "Set to -1f to use the default normalizedTime provided by the PlaySyncedAnimation call " +
                 "from the SyncedAnimationSystem.")]
        public float normalizedStartTime; // Allows specific parts to start at a different point in the animation.

        // Constructor for convenience, though Unity's inspector will handle direct assignment.
        public AnimationMapping(string key, string clipName, int layer = 0, float startTime = 0f)
        {
            animationKey = key;
            animationClipName = clipName;
            animatorLayer = layer;
            normalizedStartTime = startTime;
        }
    }

    [Tooltip("The Animator component this player will control. " +
             "If left empty, the script will try to find an Animator on this GameObject.")]
    [SerializeField]
    private Animator targetAnimator;

    [Tooltip("A list of mappings from generic animation keys to specific animation clip names " +
             "and parameters for this Animator. Define what animation to play for each generic key.")]
    [SerializeField]
    private List<AnimationMapping> animationMappings = new List<AnimationMapping>();

    // A dictionary for efficient runtime lookup of animation clip names and parameters based on their keys.
    private Dictionary<string, AnimationMapping> _mappingDictionary;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the mapping dictionary for quick lookups and ensures an Animator is present.
    /// </summary>
    private void Awake()
    {
        // If no Animator is explicitly assigned, try to find one on this GameObject.
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
            if (targetAnimator == null)
            {
                Debug.LogError($"SyncedAnimationPlayer on {gameObject.name} requires an Animator component " +
                               $"or a 'Target Animator' to be set in the Inspector. Disabling component.", this);
                enabled = false; // Disable this component if no animator is found.
                return;
            }
        }

        // Populate the runtime dictionary from the inspector-exposed list for efficient lookups.
        _mappingDictionary = new Dictionary<string, AnimationMapping>();
        foreach (var mapping in animationMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.animationKey))
            {
                Debug.LogWarning($"SyncedAnimationPlayer on {gameObject.name} has an empty 'Animation Key' in its mappings. This entry will be ignored.", this);
                continue;
            }
            if (_mappingDictionary.ContainsKey(mapping.animationKey))
            {
                Debug.LogWarning($"Duplicate animation key '{mapping.animationKey}' found in {gameObject.name}. " +
                                 "Overwriting with the last entry in the list.", this);
            }
            _mappingDictionary[mapping.animationKey] = mapping;
        }
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Registers this player with the SyncedAnimationSystem so it can receive commands.
    /// </summary>
    private void OnEnable()
    {
        // Only register if the system exists and an Animator is assigned.
        if (targetAnimator != null)
        {
            if (SyncedAnimationSystem.Instance != null)
            {
                SyncedAnimationSystem.Instance.RegisterPlayer(this);
            }
            else
            {
                Debug.LogError($"SyncedAnimationSystem instance not found while enabling {gameObject.name}. " +
                               "Make sure it's in the scene and active. This player will not receive animation commands.", this);
            }
        }
        // If targetAnimator is null, a warning was already logged in Awake.
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Deregisters this player from the SyncedAnimationSystem to prevent it from receiving commands
    /// when it's not active.
    /// </summary>
    private void OnDisable()
    {
        if (SyncedAnimationSystem.Instance != null)
        {
            SyncedAnimationSystem.Instance.DeregisterPlayer(this);
        }
    }

    /// <summary>
    /// Attempts to play an animation on this player's Animator based on the given animation key.
    /// This method is typically called by the SyncedAnimationSystem.
    /// </summary>
    /// <param name="animationKey">The generic key for the animation to play (e.g., "Attack").</param>
    /// <param name="systemLayer">The default Animator layer specified by the SyncedAnimationSystem.
    /// This will be overridden if the player's specific mapping provides a layer other than -1.</param>
    /// <param name="systemNormalizedTime">The default normalized time specified by the SyncedAnimationSystem.
    /// This will be overridden if the player's specific mapping provides a time other than -1f.</param>
    public void PlayAnimation(string animationKey, int systemLayer, float systemNormalizedTime)
    {
        if (targetAnimator == null)
        {
            Debug.LogWarning($"No Animator assigned to SyncedAnimationPlayer on {gameObject.name}. " +
                             $"Cannot play animation '{animationKey}'.", this);
            return;
        }

        // Try to retrieve the specific animation mapping for this key.
        if (_mappingDictionary.TryGetValue(animationKey, out AnimationMapping mapping))
        {
            // Determine the final layer and normalized time to use:
            // Prioritize the player's specific mapping; otherwise, use the system's default.
            int finalLayer = mapping.animatorLayer == -1 ? systemLayer : mapping.animatorLayer;
            float finalNormalizedTime = mapping.normalizedStartTime == -1f ? systemNormalizedTime : mapping.normalizedStartTime;

            // Play the animation on the target Animator.
            targetAnimator.Play(mapping.animationClipName, finalLayer, finalNormalizedTime);
            // Debug.Log($"Playing '{mapping.animationClipName}' (key: '{animationKey}') on {gameObject.name}'s Animator " +
            //           $"layer {finalLayer} at time {finalNormalizedTime:F2}.", this);
        }
        else
        {
            Debug.LogWarning($"No animation mapping found for key '{animationKey}' on {gameObject.name}. " +
                             $"Animation will not play on this Animator.", this);
        }
    }
}
```