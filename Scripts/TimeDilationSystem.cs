// Unity Design Pattern Example: TimeDilationSystem
// This script demonstrates the TimeDilationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The TimeDilationSystem is a design pattern used in games to provide fine-grained control over the passage of time for different game elements or "channels" independently of Unity's global `Time.timeScale`. This is incredibly useful for features like:

*   **Slow-motion/Bullet Time:** Affecting specific characters or the entire game for a short period.
*   **Paused Menus:** Stopping game logic while the UI remains responsive.
*   **Character-specific abilities:** A character might have a "speed boost" or "time warp" ability.
*   **Visual Effects:** Some VFX might need to run at normal speed even during slow-motion.
*   **UI Animations:** UI elements often need to animate normally even if the game is paused or slowed.

**How it Works:**

1.  **Time Channels:** The system defines different "channels" (e.g., `Global`, `Player`, `Enemies`, `UI`, `VFX`). Each channel can have its own independent time scale.
2.  **Time Modifiers:** When a game event needs to alter time (e.g., a slow-motion power-up), it creates a `TimeModifier`. This modifier specifies a `targetTimeScale` (e.g., 0.5 for half speed), a `duration`, and which `TimeChannels` it affects.
3.  **Central System (`TimeDilationSystem`):** This singleton manager tracks all active `TimeModifier` instances for each channel.
4.  **Effective Time Scale Calculation:** In its `Update` method, the system iterates through all active modifiers for each channel. It combines their `targetTimeScale` values (typically by multiplying them) to determine the *effective time scale* for that specific channel. It also handles the expiration of modifiers based on their duration.
5.  **Client Integration:** Game objects (clients) that need to obey the custom time scale (e.g., player movement, enemy AI, projectile physics) will *not* use `Time.deltaTime`. Instead, they will query the `TimeDilationSystem` for `GetDeltaTime(myChannel)` and use that value in their `Update` or `FixedUpdate` logic.

---

### **1. `TimeDilationSystem.cs`**

This script defines the core manager, the `TimeModifier` struct, and the `TimeDilationChannel` enum. It's a singleton that handles adding, removing, and calculating effective time scales.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeDilation
{
    /// <summary>
    /// Defines different categories or "channels" for time dilation.
    /// This allows different parts of the game to experience time at different rates.
    /// </summary>
    [Flags] // Allows combining multiple channels using bitwise operations
    public enum TimeDilationChannel
    {
        None = 0,
        Global = 1 << 0,   // Affects all game logic by default
        Player = 1 << 1,   // Specific to player movement, actions, etc.
        Enemies = 1 << 2,  // Specific to enemy AI, movement, attacks
        Projectiles = 1 << 3, // Specific to bullets, magic, thrown objects
        VFX = 1 << 4,      // Visual effects that might need to run at normal speed
        UI = 1 << 5,       // UI animations, timers, etc.
        Physics = 1 << 6,  // Affects specific physics calculations (less common, usually use Global)

        // Combinations for convenience
        Gameplay = Player | Enemies | Projectiles,
        All = Global | Player | Enemies | Projectiles | VFX | UI | Physics
    }

    /// <summary>
    /// Represents a single source of time dilation.
    /// Can be added to the TimeDilationSystem to modify the time scale of one or more channels.
    /// </summary>
    public struct TimeModifier
    {
        public Guid Id { get; private set; }            // Unique identifier for this modifier
        public float TargetTimeScale { get; private set; } // The desired time scale (e.g., 0.5 for half speed)
        public float Duration { get; private set; }     // How long this modifier lasts (-1 for indefinite)
        public float StartTime { get; private set; }    // The unscaled time when this modifier was activated
        public TimeDilationChannel Channels { get; private set; } // Which channels this modifier affects
        public string DebugName { get; private set; }   // Optional name for debugging

        /// <summary>
        /// Creates a new TimeModifier.
        /// </summary>
        /// <param name="targetTimeScale">The desired time scale (e.g., 0.5 for half speed).</param>
        /// <param name="duration">How long this modifier lasts in unscaled seconds (-1 for indefinite).</param>
        /// <param name="channels">The channels this modifier will affect.</param>
        /// <param name="debugName">An optional name for debugging purposes.</param>
        public TimeModifier(float targetTimeScale, float duration, TimeDilationChannel channels, string debugName = "TimeModifier")
        {
            Id = Guid.NewGuid();
            TargetTimeScale = Mathf.Max(0f, targetTimeScale); // Ensure time scale is not negative
            Duration = duration;
            Channels = channels;
            StartTime = Time.unscaledTime; // Use unscaled time to avoid recursion issues
            DebugName = debugName;
        }

        /// <summary>
        /// Checks if the modifier has expired.
        /// </summary>
        public bool HasExpired => Duration >= 0 && Time.unscaledTime > StartTime + Duration;
    }

    /// <summary>
    /// The central singleton system for managing time dilation across different channels.
    /// Game logic should query this system for its delta time instead of Time.deltaTime.
    /// </summary>
    public class TimeDilationSystem : MonoBehaviour
    {
        // Singleton instance
        public static TimeDilationSystem Instance { get; private set; }

        // Dictionary to store active modifiers, categorized by channel.
        // Each channel can have multiple modifiers affecting it.
        private readonly Dictionary<TimeDilationChannel, List<TimeModifier>> _activeModifiersByChannel =
            new Dictionary<TimeDilationChannel, List<TimeModifier>>();

        // Cache for current time scales to avoid recalculating every frame for every client.
        private readonly Dictionary<TimeDilationChannel, float> _cachedTimeScales =
            new Dictionary<TimeDilationChannel, float>();

        // Editor-exposed default time scale for channels that have no active modifiers.
        [Tooltip("The default time scale for channels when no modifiers are active.")]
        [Range(0f, 2f)]
        [SerializeField] private float _defaultTimeScale = 1f;

        // Editor-exposed option to automatically add the Global channel to all modifiers by default.
        [Tooltip("If true, all modifiers will implicitly affect the Global channel in addition to specified ones.")]
        [SerializeField] private bool _autoIncludeGlobalChannel = true;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnIoad(gameObject); // Persist across scenes
                InitializeChannels();
            }
        }

        /// <summary>
        /// Ensures all defined channels exist in our dictionaries.
        /// </summary>
        private void InitializeChannels()
        {
            foreach (TimeDilationChannel channel in Enum.GetValues(typeof(TimeDilationChannel)))
            {
                if (channel == TimeDilationChannel.None) continue; // Skip None
                if (!_activeModifiersByChannel.ContainsKey(channel))
                {
                    _activeModifiersByChannel[channel] = new List<TimeModifier>();
                }
                _cachedTimeScales[channel] = _defaultTimeScale; // Initialize with default
            }
        }

        /// <summary>
        /// Called once per frame. Manages modifier lifetimes and updates cached time scales.
        /// </summary>
        private void Update()
        {
            // Clear expired modifiers and recalculate time scales for affected channels.
            // Using a temporary list to collect channels that need recalculation.
            HashSet<TimeDilationChannel> channelsToRecalculate = new HashSet<TimeDilationChannel>();

            foreach (var kvp in _activeModifiersByChannel)
            {
                var channel = kvp.Key;
                var modifiers = kvp.Value;

                // Remove expired modifiers using a reverse loop to avoid issues with index changes
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    if (modifiers[i].HasExpired)
                    {
                        // Debug.Log($"Removing expired modifier: {modifiers[i].DebugName} from channel: {channel}");
                        modifiers.RemoveAt(i);
                        channelsToRecalculate.Add(channel);
                    }
                }

                // If any modifiers were removed or added in this frame, mark for recalculation
                if (channelsToRecalculate.Contains(channel) || modifiers.Any()) // Always recalculate if there are modifiers
                {
                    RecalculateTimeScaleForChannel(channel);
                }
                else // If no modifiers and not explicitly marked, ensure it's at default
                {
                    _cachedTimeScales[channel] = _defaultTimeScale;
                }
            }
        }

        /// <summary>
        /// Adds a time modifier to the system.
        /// </summary>
        /// <param name="modifier">The TimeModifier to add.</param>
        public void AddModifier(TimeModifier modifier)
        {
            // Ensure Global channel is included if _autoIncludeGlobalChannel is true
            if (_autoIncludeGlobalChannel)
            {
                modifier.Channels |= TimeDilationChannel.Global;
            }

            // Iterate through all possible channels and add the modifier if it affects them.
            foreach (TimeDilationChannel channel in Enum.GetValues(typeof(TimeDilationChannel)))
            {
                if (channel == TimeDilationChannel.None) continue;

                // Check if the modifier's channels include the current channel
                if ((modifier.Channels & channel) == channel)
                {
                    // Add modifier or replace existing one with the same ID (if we wanted to allow updates)
                    // For simplicity, we just add. If you need to update a modifier,
                    // you'd typically remove the old one by ID and add a new one.
                    _activeModifiersByChannel[channel].Add(modifier);
                    RecalculateTimeScaleForChannel(channel);
                }
            }
            // Debug.Log($"Added modifier: {modifier.DebugName} (ID: {modifier.Id}) for channels: {modifier.Channels}");
        }

        /// <summary>
        /// Removes a time modifier from the system using its unique ID.
        /// </summary>
        /// <param name="modifierId">The unique ID of the modifier to remove.</param>
        public void RemoveModifier(Guid modifierId)
        {
            // Track which channels were affected so we can recalculate their time scales.
            HashSet<TimeDilationChannel> channelsAffected = new HashSet<TimeDilationChannel>();

            foreach (var kvp in _activeModifiersByChannel)
            {
                var channel = kvp.Key;
                var modifiers = kvp.Value;

                // Find and remove the modifier
                int initialCount = modifiers.Count;
                modifiers.RemoveAll(m => m.Id == modifierId);

                if (modifiers.Count < initialCount)
                {
                    channelsAffected.Add(channel);
                }
            }

            // Recalculate time scales for all channels that were affected.
            foreach (var channel in channelsAffected)
            {
                RecalculateTimeScaleForChannel(channel);
            }
            // Debug.Log($"Removed modifier with ID: {modifierId}. Affected channels recalculated.");
        }

        /// <summary>
        /// Removes all modifiers from the system. Useful for scene transitions or resetting.
        /// </summary>
        public void ClearAllModifiers()
        {
            foreach (var kvp in _activeModifiersByChannel)
            {
                kvp.Value.Clear();
                _cachedTimeScales[kvp.Key] = _defaultTimeScale; // Reset to default
            }
            Debug.Log("TimeDilationSystem: All modifiers cleared.");
        }

        /// <summary>
        /// Calculates the effective time scale for a given channel by multiplying all active modifiers.
        /// </summary>
        /// <param name="channel">The channel to calculate the time scale for.</param>
        private void RecalculateTimeScaleForChannel(TimeDilationChannel channel)
        {
            float newTimeScale = _defaultTimeScale;

            if (_activeModifiersByChannel.TryGetValue(channel, out List<TimeModifier> modifiers))
            {
                // Start with the default time scale for the channel
                newTimeScale = _defaultTimeScale;

                // Apply modifiers multiplicatively
                foreach (var modifier in modifiers)
                {
                    newTimeScale *= modifier.TargetTimeScale;
                }
            }

            // Cache the calculated time scale
            _cachedTimeScales[channel] = newTimeScale;
            // Debug.Log($"Recalculated TimeScale for {channel}: {newTimeScale}");
        }

        /// <summary>
        /// Gets the current effective time scale for a specific channel.
        /// </summary>
        /// <param name="channel">The channel to query.</param>
        /// <returns>The effective time scale for the channel.</returns>
        public float GetTimeScale(TimeDilationChannel channel)
        {
            // If the channel isn't explicitly managed (e.g., None), return default.
            if (!_cachedTimeScales.ContainsKey(channel))
            {
                return _defaultTimeScale;
            }
            return _cachedTimeScales[channel];
        }

        /// <summary>
        /// Gets the custom delta time for a specific channel.
        /// This is the value that game objects should use instead of Time.deltaTime.
        /// It uses Time.unscaledDeltaTime internally to ensure the system works even if
        /// Unity's global Time.timeScale is changed (e.g., for a full pause).
        /// </summary>
        /// <param name="channel">The channel to query.</param>
        /// <returns>The custom delta time (Time.unscaledDeltaTime * effectiveTimeScale).</returns>
        public float GetDeltaTime(TimeDilationChannel channel)
        {
            return Time.unscaledDeltaTime * GetTimeScale(channel);
        }

        /// <summary>
        /// Gets the custom fixed delta time for a specific channel.
        /// Use this in FixedUpdate for physics-related operations if you need
        /// dilated physics for specific channels.
        /// </summary>
        /// <param name="channel">The channel to query.</param>
        /// <returns>The custom fixed delta time (Time.fixedUnscaledDeltaTime * effectiveTimeScale).</returns>
        public float GetFixedDeltaTime(TimeDilationChannel channel)
        {
            return Time.fixedUnscaledDeltaTime * GetTimeScale(channel);
        }

        /// <summary>
        /// Gets the current time for a specific channel. Useful for channel-specific timers.
        /// This is a simplified accumulation, not a true time object.
        /// </summary>
        /// <param name="channel">The channel to query.</param>
        /// <returns>The accumulated time for the channel.</returns>
        private readonly Dictionary<TimeDilationChannel, float> _channelTimes = new Dictionary<TimeDilationChannel, float>();

        public float GetChannelTime(TimeDilationChannel channel)
        {
            if (!_channelTimes.ContainsKey(channel))
            {
                _channelTimes[channel] = 0f;
            }
            _channelTimes[channel] += GetDeltaTime(channel);
            return _channelTimes[channel];
        }

        // Optional: Reset _channelTimes, e.g., on scene load
        public void ResetChannelTimes()
        {
            _channelTimes.Clear();
        }

        // --- Editor Helpers (Optional) ---
        [ContextMenu("Clear All Modifiers")]
        private void Editor_ClearAllModifiers()
        {
            ClearAllModifiers();
        }

        [ContextMenu("Log Current Time Scales")]
        private void Editor_LogCurrentTimeScales()
        {
            Debug.Log("--- Current Time Scales ---");
            foreach (var kvp in _cachedTimeScales)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
            Debug.Log("---------------------------");
        }
    }
}
```

---

### **2. `DilatedGameObject.cs`**

This is an example client script that uses the `TimeDilationSystem`. It could be any moving object, an enemy, a UI element, etc.

```csharp
using UnityEngine;
using TimeDilation; // Make sure to use the namespace where TimeDilationSystem is defined

namespace TimeDilation.Examples
{
    /// <summary>
    /// An example MonoBehaviour that uses the TimeDilationSystem for its updates.
    /// This script demonstrates how game objects should query for their specific
    /// delta time instead of relying on Unity's global Time.deltaTime.
    /// </summary>
    public class DilatedGameObject : MonoBehaviour
    {
        [Header("Time Dilation Settings")]
        [Tooltip("The time channel this GameObject belongs to.")]
        [SerializeField] private TimeDilationChannel _myChannel = TimeDilationChannel.Player;

        [Tooltip("The speed at which this object moves.")]
        [SerializeField] private float _movementSpeed = 5f;

        [Tooltip("The speed at which this object rotates.")]
        [SerializeField] private float _rotationSpeed = 100f;

        [Header("Debug Display")]
        [Tooltip("Display debug information on screen.")]
        [SerializeField] private bool _showDebugInfo = true;

        private float _currentDilatedDeltaTime; // Store for display

        void Update()
        {
            // Ensure the TimeDilationSystem instance exists before trying to access it
            if (TimeDilationSystem.Instance == null)
            {
                Debug.LogWarning("TimeDilationSystem.Instance is null. Is the TimeDilationSystem object in the scene?");
                return;
            }

            // --- Core Logic: Get the dilated delta time ---
            _currentDilatedDeltaTime = TimeDilationSystem.Instance.GetDeltaTime(_myChannel);
            float currentDilatedTimeScale = TimeDilationSystem.Instance.GetTimeScale(_myChannel);

            // --- Apply Movement (using dilated delta time) ---
            float horizontalInput = Input.GetAxis("Horizontal"); // Example input
            float verticalInput = Input.GetAxis("Vertical");     // Example input

            Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
            transform.Translate(moveDirection * _movementSpeed * _currentDilatedDeltaTime, Space.World);

            // --- Apply Rotation (using dilated delta time) ---
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * _currentDilatedDeltaTime);
            }

            // --- Example of a timer using channel time ---
            // If you needed a timer that respects this object's time scale:
            // float channelTime = TimeDilationSystem.Instance.GetChannelTime(_myChannel);
            // if (channelTime % 5f < 0.01f) { Debug.Log($"Channel time for {_myChannel} is {channelTime}"); } // Log every 5 seconds (dilated)
        }

        void OnGUI()
        {
            if (!_showDebugInfo) return;

            // Display current channel and its time scale/delta time for debugging
            GUI.Label(new Rect(10, 10 + transform.GetSiblingIndex() * 30, 300, 20),
                $"Object: {gameObject.name} | Channel: {_myChannel} | " +
                $"TimeScale: {TimeDilationSystem.Instance.GetTimeScale(_myChannel):F2} | " +
                $"DeltaTime: {_currentDilatedDeltaTime:F3}");
        }
    }
}
```

---

### **3. `TimeDilationEffector.cs`**

This script demonstrates how to *add* and *remove* `TimeModifier` instances to influence the system. This could be a power-up, a UI button, or an environmental hazard.

```csharp
using UnityEngine;
using TimeDilation;
using System;

namespace TimeDilation.Examples
{
    /// <summary>
    /// An example script to demonstrate adding and removing TimeModifiers to the TimeDilationSystem.
    /// Attach this to an empty GameObject or a UI button.
    /// </summary>
    public class TimeDilationEffector : MonoBehaviour
    {
        [Header("Modifier Settings")]
        [Tooltip("The target time scale this effector will apply.")]
        [SerializeField] private float _targetTimeScale = 0.2f; // e.g., 20% speed (bullet time)

        [Tooltip("The duration of the effect in unscaled seconds. Set to -1 for indefinite.")]
        [SerializeField] private float _duration = 3f;

        [Tooltip("The channels this effector will modify.")]
        [SerializeField] private TimeDilationChannel _affectedChannels = TimeDilationChannel.Gameplay;

        [Tooltip("A name for this modifier for debugging purposes.")]
        [SerializeField] private string _modifierDebugName = "SlowMotionEffect";

        // Stores the ID of the currently active modifier, so we can remove it later.
        private Guid _activeModifierId = Guid.Empty;
        private bool _isEffectActive = false;

        void Update()
        {
            if (TimeDilationSystem.Instance == null)
            {
                Debug.LogWarning("TimeDilationSystem.Instance is null. Is the TimeDilationSystem object in the scene?");
                return;
            }

            // --- Toggle Effect with 'Space' key ---
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!_isEffectActive)
                {
                    ApplyTimeDilationEffect();
                }
                else
                {
                    RemoveTimeDilationEffect();
                }
            }

            // If the effect has a duration, ensure it's removed by the system (handled internally)
            // Or you could manually check _isEffectActive and _activeModifierId here if you needed
            // to do something immediately after it expires.
            // For now, the system's Update() handles removal.
        }

        /// <summary>
        /// Applies the time dilation effect by creating and adding a TimeModifier.
        /// </summary>
        public void ApplyTimeDilationEffect()
        {
            if (TimeDilationSystem.Instance == null) return;

            if (_isEffectActive && _activeModifierId != Guid.Empty)
            {
                Debug.LogWarning("Time dilation effect is already active. Ignoring new application.");
                return;
            }

            // Create a new TimeModifier with specified properties
            TimeModifier newModifier = new TimeModifier(_targetTimeScale, _duration, _affectedChannels, _modifierDebugName);
            _activeModifierId = newModifier.Id;

            // Add the modifier to the system
            TimeDilationSystem.Instance.AddModifier(newModifier);
            _isEffectActive = true;

            Debug.Log($"Applied '{_modifierDebugName}' effect. Target: {_targetTimeScale}, Duration: {_duration}s, Channels: {_affectedChannels}");

            // If it's a timed effect, we could set a timer here to remove it,
            // but the TimeDilationSystem's Update method already handles expiration
            // based on the modifier's duration, so manual removal isn't strictly necessary.
            // However, if we want to toggle it OFF manually, we need to store the ID.
        }

        /// <summary>
        /// Removes the currently active time dilation effect.
        /// </summary>
        public void RemoveTimeDilationEffect()
        {
            if (TimeDilationSystem.Instance == null || _activeModifierId == Guid.Empty) return;

            TimeDilationSystem.Instance.RemoveModifier(_activeModifierId);
            _activeModifierId = Guid.Empty; // Reset the ID
            _isEffectActive = false;

            Debug.Log($"Removed '{_modifierDebugName}' effect.");
        }

        void OnGUI()
        {
            // Display current state for debugging
            GUI.Label(new Rect(10, 100, 300, 20), $"Spacebar: Toggle {_modifierDebugName} | State: " + (_isEffectActive ? "ACTIVE" : "INACTIVE"));
            GUI.Label(new Rect(10, 120, 300, 20), $"Current Time.timeScale: {Time.timeScale:F2}"); // Note: TimeDilationSystem works independently of this.
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create Folders:** In your Unity project, create a folder like `Scripts/TimeDilation`.
2.  **Add Scripts:** Place `TimeDilationSystem.cs`, `DilatedGameObject.cs`, and `TimeDilationEffector.cs` into this folder.
3.  **Create TimeDilationSystem GameObject:**
    *   Create an empty GameObject in your scene (e.g., named `_TimeDilationManager`).
    *   Attach the `TimeDilationSystem.cs` script to it.
4.  **Create Dilated GameObjects:**
    *   Create a few primitive 3D objects (e.g., `Cube`, `Sphere`, `Capsule`).
    *   Attach `DilatedGameObject.cs` to each of them.
    *   For each `DilatedGameObject`, in the Inspector, assign a `Time Dilation Channel`. For example:
        *   `Cube`: `Player`
        *   `Sphere`: `Enemies`
        *   `Capsule`: `UI` (even though it's not really UI, for demonstration)
        *   `Another Cube`: `Global` (or `Player` as well)
5.  **Create TimeDilationEffector GameObject:**
    *   Create another empty GameObject (e.g., named `TimeEffectTrigger`).
    *   Attach `TimeDilationEffector.cs` to it.
    *   In its Inspector, set `Target Time Scale` to something like `0.2` and `Duration` to `5` seconds.
    *   Set `Affected Channels` to `Gameplay` (which includes `Player`, `Enemies`, `Projectiles`).
    *   Add another `TimeDilationEffector` if you want to test different channels (e.g., one affecting `Player` only, another affecting `Enemies` only).
6.  **Run the Scene:**
    *   You will see the `DilatedGameObjects` moving and rotating.
    *   Press the **Spacebar** key. The `TimeDilationEffector` will activate, adding a `TimeModifier`.
    *   Objects belonging to the `Gameplay` channels (`Player`, `Enemies`, `Projectiles`) will slow down according to the `Target Time Scale` (e.g., move at 20% speed).
    *   Objects belonging to channels *not* affected by the `TimeDilationEffector` (e.g., `UI` channel if you assigned one) will continue to move at normal speed.
    *   After the `Duration` (or if you press Spacebar again to toggle it off), the `TimeModifier` will be removed, and the affected objects will return to their normal speed.

This setup provides a complete, working, and educational example of the TimeDilationSystem pattern in Unity.