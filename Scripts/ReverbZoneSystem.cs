// Unity Design Pattern Example: ReverbZoneSystem
// This script demonstrates the ReverbZoneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ReverbZoneSystem' design pattern in Unity provides a robust and modular way to apply different environmental effects (like audio reverb, but extendable to visual effects, light changes, etc.) based on the player's or a designated listener's location within defined areas in the game world.

This example focuses on **Audio Reverb**, using Unity's built-in `AudioReverbFilter` to create a dynamic acoustic environment.

### ReverbZoneSystem Design Pattern Explained

The pattern separates concerns into two main components:

1.  **`ReverbZone`**:
    *   **Role**: Defines a specific physical area in the world and the properties (in this case, an `AudioReverbPreset`) associated with that area.
    *   **Implementation**: A `MonoBehaviour` attached to a GameObject with a Trigger Collider. It exposes the `AudioReverbPreset` via a `[SerializeField]` field. It doesn't *apply* any effects; it just *describes* the zone.
    *   **Benefits**: Easy to place, scale, and configure in the editor. New zones can be added without modifying core logic.

2.  **`AudioListenerReverbApplier`**:
    *   **Role**: The active component that detects entry/exit from `ReverbZone`s and applies the corresponding effects to the `AudioListener`.
    *   **Implementation**: A `MonoBehaviour` attached to the GameObject containing the `AudioListener` (typically the Main Camera or Player). It has a `List` to keep track of all currently active (overlapping) `ReverbZone`s. It utilizes `OnTriggerEnter` and `OnTriggerExit` to manage this list and update the `AudioReverbFilter` component.
    *   **Benefits**: Centralizes the logic for applying effects. Handles multiple overlapping zones gracefully (in this example, prioritizing the most recently entered zone). Can be extended to manage other types of zone-based effects.

### How It Works:

1.  You place `ReverbZone` GameObjects throughout your scene, each defining an area and its desired `AudioReverbPreset`. These zones have trigger colliders.
2.  Your `AudioListener` (on the player or camera) has the `AudioListenerReverbApplier` script attached, along with a `Rigidbody` (required for trigger detection).
3.  As the `AudioListener` enters a `ReverbZone`'s trigger, the `AudioListenerReverbApplier` adds that zone to its internal list of active zones.
4.  It then evaluates the active zones (e.g., picking the last one entered) and applies its `AudioReverbPreset` to the `AudioReverbFilter` component on the same GameObject as the `AudioListener`.
5.  When the `AudioListener` exits a `ReverbZone`, the `AudioListenerReverbApplier` removes it from the active zones list and re-evaluates which reverb preset to apply (either from another active zone or the default if no zones are active).

This pattern ensures clear separation of concerns, making your project easier to manage, extend, and debug.

---

### Complete C# Unity Script (`ReverbZoneSystem.cs`)

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

namespace ReverbZoneSystemExample
{
    /// <summary>
    /// REVERB ZONE SYSTEM DESIGN PATTERN EXAMPLE
    ///
    /// This script demonstrates a practical implementation of the ReverbZoneSystem pattern in Unity.
    /// It provides a flexible and modular way to apply different audio reverb
    /// effects based on the player's (or any designated listener's) current location
    /// within defined "reverb zones" in the game world.
    ///
    /// The pattern consists of two main components:
    ///
    /// 1.  ReverbZone:
    ///     -   Attached to a GameObject with a Trigger Collider.
    ///     -   Defines a physical area in the world.
    ///     -   Holds a specific AudioReverbPreset (e.g., "Room", "Cave", "Forest").
    ///     -   Its sole responsibility is to *define* the zone and its associated reverb properties.
    ///
    /// 2.  AudioListenerReverbApplier:
    ///     -   Attached to the GameObject that contains the AudioListener (usually the Main Camera or Player).
    ///     -   Detects when the listener enters or exits ReverbZones.
    ///     -   Manages an AudioReverbFilter component on the same GameObject.
    ///     -   Applies the appropriate reverb preset from the currently active ReverbZone(s).
    ///     -   Handles overlapping zones by prioritizing the most recently entered zone, or reverting
    ///         to the previous zone if multiple are active.
    ///
    /// Benefits of this pattern:
    /// -   **Modularity:** Reverb zones are independent GameObjects, easy to create, move, and modify.
    /// -   **Scalability:** Easily add more zones with different presets without modifying core logic.
    /// -   **Separation of Concerns:** `ReverbZone` defines, `AudioListenerReverbApplier` applies.
    /// -   **Flexibility:** Can be adapted for other zone-based effects (e.g., environmental particles,
    ///     lighting changes, soundscapes) by changing the properties held by `ReverbZone` and the
    ///     logic in the `Applier` component.
    ///
    /// How it works in a nutshell:
    /// 1.  Player (with `AudioListenerReverbApplier` and `AudioListener`) moves.
    /// 2.  Player enters a `ReverbZone`'s trigger collider.
    /// 3.  `AudioListenerReverbApplier` detects the entry, adds the `ReverbZone` to its list of active zones.
    /// 4.  `AudioListenerReverbApplier` applies the reverb preset of the "top" (most recent or highest priority)
    ///     active `ReverbZone` to its `AudioReverbFilter`.
    /// 5.  Player exits a `ReverbZone`.
    /// 6.  `AudioListenerReverbApplier` detects the exit, removes the `ReverbZone` from its list.
    /// 7.  `AudioListenerReverbApplier` re-evaluates the active zones and applies the appropriate reverb
    ///     (either from another active zone or the default preset if no zones are active).
    /// </summary>


    /// <summary>
    /// Component defining a reverb zone. Attach this to a GameObject with a Trigger Collider.
    /// This object's sole purpose is to define an area and its associated AudioReverbPreset.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Ensures a Collider component exists on the GameObject.
    public class ReverbZone : MonoBehaviour
    {
        [Tooltip("The audio reverb preset to apply when the listener is inside this zone.")]
        [SerializeField] private AudioReverbPreset _reverbPreset = AudioReverbPreset.Generic;

        /// <summary>
        /// Gets the AudioReverbPreset associated with this zone.
        /// </summary>
        public AudioReverbPreset ReverbPreset => _reverbPreset;

        private void Awake()
        {
            // Ensure the collider is set as a trigger. This is crucial for OnTriggerEnter/Exit to fire.
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                Debug.LogError($"ReverbZone on {gameObject.name} requires a Collider component. Please add one.", this);
            }
        }

        // --- Editor Visualisation (Optional but recommended for usability) ---
        // This method draws a visual representation of the zone in the Unity editor.
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                // Set a semi-transparent color for the gizmo.
                Gizmos.color = new Color(0, 0.8f, 1, 0.3f); // Light blue, semi-transparent.

                // Save current matrix to restore later, allowing local scaling/rotation.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

                // Draw different collider types.
                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    // Capsule drawing can be complex; drawing a cube representing bounds for simplicity.
                    Gizmos.DrawCube(Vector3.zero, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
                }
                else
                {
                    // For other colliders (e.g., MeshCollider), just draw the global bounds.
                    // Note: For MeshCollider, ensure it's convex if used as a trigger.
                    Gizmos.matrix = Matrix4x4.identity; // Reset matrix for global bounds.
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
                Gizmos.matrix = Matrix4x4.identity; // Always reset Gizmos matrix.
            }
        }
    }


    /// <summary>
    /// Manages and applies audio reverb effects to the AudioListener based on the
    /// ReverbZones it enters or exits. This component represents the "applier" part
    /// of the ReverbZoneSystem pattern.
    /// </summary>
    [RequireComponent(typeof(AudioListener))] // The AudioListener is what receives the processed audio.
    [RequireComponent(typeof(Rigidbody))]      // A Rigidbody is essential for trigger detection to work.
    public class AudioListenerReverbApplier : MonoBehaviour
    {
        [Tooltip("The default reverb preset to use when the listener is not inside any ReverbZone.")]
        [SerializeField] private AudioReverbPreset _defaultReverbPreset = AudioReverbPreset.Off;

        private AudioReverbFilter _audioReverbFilter;
        // A list to keep track of all ReverbZones the listener is currently inside.
        // The last zone in the list is considered the "most recently entered" and takes precedence.
        private readonly List<ReverbZone> _activeReverbZones = new List<ReverbZone>();

        private void Awake()
        {
            // Get or add the AudioReverbFilter component. This component is responsible for applying the reverb effect.
            _audioReverbFilter = GetComponent<AudioReverbFilter>();
            if (_audioReverbFilter == null)
            {
                _audioReverbFilter = gameObject.AddComponent<AudioReverbFilter>();
                Debug.Log($"AudioReverbFilter component automatically added to {gameObject.name}.", this);
            }

            // Configure the Rigidbody for trigger detection.
            // For a listener (like a camera), a kinematic Rigidbody is typically sufficient.
            // If your player uses physics-driven movement, ensure its Rigidbody is configured
            // to interact with triggers (e.g., non-kinematic or has continuous collision detection).
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Set to kinematic if this GameObject isn't driven by physics.
                                       // This means it won't be affected by forces but can still detect triggers.
                Debug.Log($"Rigidbody on {gameObject.name} set to isKinematic = true for trigger detection. " +
                          "If your player uses physics, ensure its Rigidbody interacts correctly with triggers.", this);
            }
            else
            {
                // This case should not happen due to [RequireComponent(typeof(Rigidbody))]
                Debug.LogError($"AudioListenerReverbApplier on {gameObject.name} requires a Rigidbody for trigger detection to function. " +
                               "Please ensure it exists and is configured.", this);
            }

            // Apply the default reverb preset when the scene starts.
            ApplyCurrentReverb();
        }

        /// <summary>
        /// Called when this object's collider enters a trigger collider.
        /// </summary>
        /// <param name="other">The other Collider involved in this collision (e.g., a ReverbZone's collider).</param>
        private void OnTriggerEnter(Collider other)
        {
            ReverbZone zone = other.GetComponent<ReverbZone>();
            // If the entering collider belongs to a ReverbZone and it's not already in our active list, add it.
            if (zone != null && !_activeReverbZones.Contains(zone))
            {
                _activeReverbZones.Add(zone);
                Debug.Log($"Entered ReverbZone: '{zone.name}'. Active zones count: {_activeReverbZones.Count}", this);
                ApplyCurrentReverb(); // Re-evaluate and apply the reverb.
            }
        }

        /// <summary>
        /// Called when this object's collider exits a trigger collider.
        /// </summary>
        /// <param name="other">The other Collider involved in this collision (e.g., a ReverbZone's collider).</param>
        private void OnTriggerExit(Collider other)
        {
            ReverbZone zone = other.GetComponent<ReverbZone>();
            // If the exiting collider belongs to a ReverbZone, remove it from our active list.
            if (zone != null)
            {
                _activeReverbZones.Remove(zone);
                Debug.Log($"Exited ReverbZone: '{zone.name}'. Active zones count: {_activeReverbZones.Count}", this);
                ApplyCurrentReverb(); // Re-evaluate and apply the reverb.
            }
        }

        /// <summary>
        /// Determines the appropriate reverb preset based on active zones and applies it
        /// to the AudioReverbFilter. This method is the core logic for selecting the active effect.
        /// </summary>
        private void ApplyCurrentReverb()
        {
            AudioReverbPreset targetPreset = _defaultReverbPreset;

            // If there are active zones, use the preset from the most recently entered one.
            // This simple approach handles overlapping zones by prioritizing the latest entry.
            // For more complex priority systems (e.g., smallest zone, named priority, specific tag),
            // you would modify this logic to sort or select from `_activeReverbZones` differently.
            if (_activeReverbZones.Count > 0)
            {
                targetPreset = _activeReverbZones[_activeReverbZones.Count - 1].ReverbPreset;
            }

            // Only update the filter if the preset has changed to avoid unnecessary API calls.
            if (_audioReverbFilter.reverbPreset != targetPreset)
            {
                _audioReverbFilter.reverbPreset = targetPreset;
                Debug.Log($"Applying new reverb preset: {targetPreset}", this);
            }
        }
    }
}

/*
/// EXAMPLE USAGE IN UNITY PROJECT:
///
/// **STEP 1: Create the Listener/Player GameObject**
/// 1.  Locate the GameObject that contains your scene's `AudioListener`. This is often the Main Camera.
///     -   If you're using a custom player character that has its own `AudioListener`, use that GameObject.
///     -   Ensure there is ONLY ONE active `AudioListener` in your scene.
/// 2.  Add the `AudioListenerReverbApplier` script to this GameObject (e.g., "Main Camera").
/// 3.  In the Inspector for your "Main Camera" (or chosen listener GameObject):
///     -   You'll observe that an `AudioReverbFilter` and `Rigidbody` component are automatically
///         added (or found if they already existed) due to the `[RequireComponent]` attributes.
///     -   Configure the `Default Reverb Preset` (e.g., "Off" or "Generic"). This will be the sound
///         when the listener is not inside any custom reverb zone.
///
/// **STEP 2: Create ReverbZone GameObjects**
/// 1.  Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty).
///     Name it something descriptive, like "Cave Reverb Zone".
/// 2.  Add a `Collider` component to this GameObject (e.g., `Box Collider` or `Sphere Collider`).
///     -   **IMPORTANT**: In the Inspector for the Collider, check the "Is Trigger" checkbox.
/// 3.  Adjust the Collider's size and position in the Scene View to define the physical area for your zone.
/// 4.  Add the `ReverbZone` script to this "Cave Reverb Zone" GameObject.
/// 5.  In the Inspector for "Cave Reverb Zone":
///     -   Set the `Reverb Preset` property to your desired effect (e.g., "Cave", "SewerPipe", "Arena").
///
/// 6.  Repeat steps 1-5 to create more reverb zones with different presets (e.g., "Forest Reverb Zone" with `Reverb Preset` set to "Forest").
///
/// **STEP 3: Test It!**
/// 1.  Ensure your player/camera can move through the scene, interacting with the `ReverbZone` colliders.
/// 2.  Run the game.
/// 3.  Move your "Player Listener" (or Main Camera) GameObject into and out of the `ReverbZone` areas.
/// 4.  Observe the `AudioReverbFilter` component on your "Player Listener" in the Inspector:
///     -   The `Reverb Preset` property should dynamically change as you enter/exit zones.
///     -   Any sounds playing in your scene (e.g., from `AudioSource` components) will now be
///         processed by this `AudioReverbFilter`, applying the corresponding reverb effect, creating an
///         immersive spatial audio experience.
///
/// **Important Considerations for Physics & Triggers:**
/// -   For `OnTriggerEnter`/`OnTriggerExit` events to fire, at least one of the two colliders involved
///     in the collision must have a `Rigidbody` attached.
/// -   In this example, the `AudioListenerReverbApplier` automatically adds a `Rigidbody` to its GameObject
///     and sets `isKinematic = true`. This is ideal for components that move but don't require physics simulation
///     (like a camera or a player moved by a `CharacterController`).
/// -   If your player character already has a `Rigidbody` for physics-based movement, ensure it is configured
///     correctly to interact with triggers. For example, its collision detection mode should be appropriate
///     (e.g., `Continuous` or `ContinuousDynamic` for fast-moving objects) if needed, and it must not be kinematic
///     if it's intended to be moved by the physics engine.
/// -   Both colliders involved must have at least one of them *not* being a trigger, OR both must be triggers
///     and one has a Rigidbody. In our setup, `ReverbZone` has `isTrigger = true`, and the listener's
///     `Rigidbody` facilitates the trigger interaction.
///
/// **Handling Overlapping Zones (Current Logic):**
/// -   The current implementation prioritizes the *last entered* zone. If you enter Zone A, then Zone B (overlapping A),
///     Zone B's reverb will become active. If you then exit Zone B while still inside Zone A, Zone A's reverb
///     will become active again because it's still in the `_activeReverbZones` list and becomes the effective "top" zone.
/// -   If you need a different priority system (e.g., smallest zone takes precedence, zones with a specific tag,
///     or a custom priority level), you would modify the `ApplyCurrentReverb()` method to sort or select
///     from the `_activeReverbZones` list based on your custom criteria.
*/
```