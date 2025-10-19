// Unity Design Pattern Example: ShockwaveEffectSystem
// This script demonstrates the ShockwaveEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ShockwaveEffectSystem' pattern in Unity provides a robust and decoupled way to manage propagating area-of-effect abilities or environmental hazards. It centralizes the logic for detecting targets, applying effects, and managing the lifecycle of multiple concurrent shockwaves.

This pattern typically involves:

1.  **`ShockwaveEffectSystem` (Manager)**: A central singleton that orchestrates all active shockwaves. It provides a simple API to trigger new shockwaves and updates their propagation over time.
2.  **`ShockwaveConfig` (ScriptableObject)**: A data-driven asset that defines the properties and effects of a specific type of shockwave (e.g., its speed, radius, damage, force, and associated visual/audio effects). This allows designers to easily create and tweak different shockwave types.
3.  **`Shockwave` (Internal Class/Struct)**: Represents a single, active instance of a propagating shockwave. It tracks its current state (origin, radius, elapsed time) and handles its own propagation logic, including detecting targets and applying effects based on its `ShockwaveConfig`.
4.  **`ShockwaveTarget` (Component)**: A component attached to any GameObject that can be affected by a shockwave. It provides a standardized callback method for the `Shockwave` to notify it of a hit, allowing the target to react appropriately (e.g., take damage, get pushed, play hit animations).

---

### Complete C# Unity Example: ShockwaveEffectSystem

This example includes three main scripts:
1.  **`ShockwaveEffectSystem.cs`**: The core manager and the internal `Shockwave` class.
2.  **`ShockwaveConfig.cs`**: A ScriptableObject to define shockwave types.
3.  **`ShockwaveTarget.cs`**: A component for objects that can be hit.

To get this working:

1.  Create a new C# script named `ShockwaveEffectSystem`.
2.  Create a new C# script named `ShockwaveConfig`.
3.  Create a new C# script named `ShockwaveTarget`.
4.  Copy the respective code into each file.
5.  Follow the "Example Usage in Unity" instructions below.

---

### 1. `ShockwaveEffectSystem.cs`

This script manages all active shockwaves in the scene, handling their creation, propagation, and removal. It uses a singleton pattern for easy access.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and HashSet
using System.Collections; // For Coroutines (if needed for SFX/VFX)

namespace MyGame.Effects
{
    /// <summary>
    /// The ShockwaveEffectSystem manages the lifecycle and propagation of all active shockwaves.
    /// It's a singleton for easy global access.
    /// </summary>
    public class ShockwaveEffectSystem : MonoBehaviour
    {
        // Singleton instance
        public static ShockwaveEffectSystem Instance { get; private set; }

        // List of currently active shockwave instances
        private readonly List<Shockwave> _activeShockwaves = new List<Shockwave>();

        /// <summary>
        /// Represents a single active shockwave propagating through the world.
        /// </summary>
        private class Shockwave
        {
            public Vector3 Origin { get; private set; }
            public ShockwaveConfig Config { get; private set; }
            public float CurrentRadius { get; private set; }
            public float TimeElapsed { get; private set; }

            // To ensure objects are only hit once per shockwave propagation instance
            private readonly HashSet<Collider> _hitCollidersThisShockwave = new HashSet<Collider>();

            // Reference to the instantiated VFX object for this shockwave
            private GameObject _vfxInstance;
            private ParticleSystem _vfxParticleSystem; // Cached for easier control

            /// <summary>
            /// Constructor for a new Shockwave instance.
            /// </summary>
            /// <param name="origin">The world position where the shockwave originates.</param>
            /// <param name="config">The ShockwaveConfig asset defining its properties.</param>
            public Shockwave(Vector3 origin, ShockwaveConfig config)
            {
                Origin = origin;
                Config = config;
                CurrentRadius = 0f;
                TimeElapsed = 0f;

                // Instantiate VFX if provided
                if (Config.vfxPrefab != null)
                {
                    _vfxInstance = GameObject.Instantiate(Config.vfxPrefab, Origin, Quaternion.identity);
                    _vfxParticleSystem = _vfxInstance.GetComponent<ParticleSystem>();
                    // If it's a particle system, ensure it plays on creation if needed,
                    // or its lifetime is managed by its own properties.
                    // For an expanding ring, we'd typically scale it.
                    if (_vfxParticleSystem != null)
                    {
                        var main = _vfxParticleSystem.main;
                        main.scalingMode = ParticleSystemScalingMode.Local; // Scale particles locally
                    }
                }
            }

            /// <summary>
            /// Updates the shockwave's propagation, checks for hits, and applies effects.
            /// </summary>
            /// <param name="deltaTime">The time passed since the last frame.</param>
            /// <returns>True if the shockwave is still active, false if it has finished.</returns>
            public bool UpdateShockwave(float deltaTime)
            {
                TimeElapsed += deltaTime;
                CurrentRadius = Config.speed * TimeElapsed;

                // Update VFX scale if it exists and is meant to represent the expanding radius
                if (_vfxInstance != null)
                {
                    // Assuming the VFX prefab is designed to scale from 0 outwards
                    _vfxInstance.transform.localScale = Vector3.one * (CurrentRadius * Config.vfxScaleFactor);
                }

                // Check if the shockwave has reached its maximum radius
                if (CurrentRadius > Config.maxRadius)
                {
                    // Clean up VFX when the shockwave is done
                    if (_vfxInstance != null)
                    {
                        // Stop particle system and let it finish, then destroy or pool
                        if (_vfxParticleSystem != null)
                        {
                            _vfxParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            // Schedule destruction after its particles have faded out
                            GameObject.Destroy(_vfxInstance, _vfxParticleSystem.main.duration + _vfxParticleSystem.main.startLifetime.constantMax);
                        }
                        else
                        {
                            GameObject.Destroy(_vfxInstance);
                        }
                    }
                    return false; // Shockwave finished
                }

                // Perform an overlap sphere check to find colliders within the current radius
                // Use the configured LayerMask to filter targets
                Collider[] hitColliders = Physics.OverlapSphere(Origin, CurrentRadius, Config.layerMask);

                foreach (Collider collider in hitColliders)
                {
                    // Only process colliders that haven't been hit by this specific shockwave instance yet
                    // This prevents multiple hits as the radius expands over several frames
                    if (_hitCollidersThisShockwave.Add(collider))
                    {
                        // Attempt to get the ShockwaveTarget component from the hit collider's GameObject
                        if (collider.TryGetComponent(out ShockwaveTarget target))
                        {
                            // Calculate hit point and direction for the target's reaction
                            // Using ClosestPoint for more accurate impact point, or transform.position for center
                            Vector3 hitPoint = collider.ClosestPoint(Origin);
                            Vector3 hitDirection = (target.transform.position - Origin).normalized;

                            // Notify the target that it has been hit by this shockwave
                            target.OnShockwaveHit(Config, hitPoint, hitDirection);
                        }
                    }
                }

                return true; // Shockwave is still active
            }
        }

        // --- MonoBehaviour Lifecycle Methods ---

        private void Awake()
        {
            // Implement Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple ShockwaveEffectSystem instances found. Destroying duplicate.");
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            // Clear instance reference when destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Iterate through active shockwaves and update them.
            // Iterate backwards to safely remove finished shockwaves during iteration.
            for (int i = _activeShockwaves.Count - 1; i >= 0; i--)
            {
                Shockwave shockwave = _activeShockwaves[i];
                if (!shockwave.UpdateShockwave(Time.deltaTime))
                {
                    // If UpdateShockwave returns false, it means the shockwave has finished.
                    _activeShockwaves.RemoveAt(i);
                }
            }
        }

        // --- Public API ---

        /// <summary>
        /// Triggers a new shockwave effect at a specified origin with a given configuration.
        /// </summary>
        /// <param name="origin">The world position where the shockwave will start.</param>
        /// <param name="config">The ShockwaveConfig asset defining this shockwave's behavior.</param>
        public void TriggerShockwave(Vector3 origin, ShockwaveConfig config)
        {
            if (config == null)
            {
                Debug.LogError("Cannot trigger shockwave: ShockwaveConfig is null.");
                return;
            }

            // Play one-shot SFX at the origin
            if (config.sfxClip != null)
            {
                AudioSource.PlayClipAtPoint(config.sfxClip, origin, config.sfxVolume);
            }

            // Create and add a new shockwave instance to be managed by the system
            _activeShockwaves.Add(new Shockwave(origin, config));
            Debug.Log($"Shockwave '{config.name}' triggered at {origin}!");
        }

        // Optional: Draw debug spheres in the editor for active shockwaves
        private void OnDrawGizmos()
        {
            if (_activeShockwaves == null) return;
            foreach (var shockwave in _activeShockwaves)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange translucent
                Gizmos.DrawWireSphere(shockwave.Origin, shockwave.CurrentRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(shockwave.Origin, 0.2f);
            }
        }
    }
}
```

---

### 2. `ShockwaveConfig.cs`

This ScriptableObject defines the parameters for different types of shockwaves. Designers can create multiple assets based on this script to define various shockwave behaviors.

```csharp
using UnityEngine;

namespace MyGame.Effects
{
    /// <summary>
    /// ShockwaveConfig is a ScriptableObject that defines the properties and effects
    /// of a specific type of shockwave. This allows designers to easily create
    /// different shockwave behaviors as data assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShockwaveConfig", menuName = "Scriptable Objects/Shockwave Config", order = 1)]
    public class ShockwaveConfig : ScriptableObject
    {
        [Header("Shockwave Propagation Properties")]
        [Tooltip("The maximum radius the shockwave will expand to.")]
        public float maxRadius = 10f;
        [Tooltip("The speed at which the shockwave expands outwards (units per second).")]
        public float speed = 5f;
        [Tooltip("A LayerMask to filter which colliders the shockwave can hit.")]
        public LayerMask layerMask;

        [Header("Effect Parameters")]
        [Tooltip("The magnitude of the physics force applied to hit Rigidbodies.")]
        public float forceMagnitude = 1000f;
        [Tooltip("A multiplier for applying force slightly upwards. 0 for purely horizontal.")]
        [Range(0f, 1f)]
        public float upwardsForceModifier = 0.5f;
        [Tooltip("The amount of damage applied to hit targets.")]
        public float damageAmount = 25f;
        [Tooltip("The duration targets will be stunned (if applicable).")]
        public float stunDuration = 2f;

        [Header("Visual & Audio Effects")]
        [Tooltip("Prefab to instantiate at the shockwave's origin. Should scale to match radius.")]
        public GameObject vfxPrefab;
        [Tooltip("Factor to multiply CurrentRadius by when scaling the VFX prefab.")]
        public float vfxScaleFactor = 1f; // Adjust based on your VFX prefab's default scale
        [Tooltip("Audio clip to play when the shockwave is triggered.")]
        public AudioClip sfxClip;
        [Tooltip("Volume of the SFX clip.")]
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
    }
}
```

---

### 3. `ShockwaveTarget.cs`

This component is placed on any GameObject that should react to a shockwave hit. It processes the `ShockwaveConfig` data to apply specific effects.

```csharp
using UnityEngine;

namespace MyGame.Effects
{
    /// <summary>
    /// ShockwaveTarget is a component attached to GameObjects that can be affected
    /// by shockwaves. It provides a callback method for the ShockwaveEffectSystem
    /// to notify it of a hit and apply effects based on the ShockwaveConfig.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Ensures the GameObject has a Collider
    public class ShockwaveTarget : MonoBehaviour
    {
        // Optional: Reference to a Rigidbody for physics effects
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            // If we have a Rigidbody, ensure it's not kinematic so force can be applied
            if (_rigidbody != null && _rigidbody.isKinematic)
            {
                Debug.LogWarning($"ShockwaveTarget on {gameObject.name} has a Kinematic Rigidbody. Force will not be applied unless it's non-kinematic.");
            }
        }

        /// <summary>
        /// Called by the ShockwaveEffectSystem when this target is hit by a shockwave.
        /// This method applies the effects defined in the ShockwaveConfig.
        /// </summary>
        /// <param name="config">The ShockwaveConfig that describes the hitting shockwave's properties.</param>
        /// <param name="hitPoint">The closest point on the target's collider to the shockwave's origin.</param>
        /// <param name="hitDirection">The normalized direction vector from the shockwave's origin to the target's position.</param>
        public virtual void OnShockwaveHit(ShockwaveConfig config, Vector3 hitPoint, Vector3 hitDirection)
        {
            Debug.Log($"<color=cyan>{gameObject.name}</color> hit by shockwave '{config.name}'!");

            // --- Apply Force (if Rigidbody exists) ---
            if (_rigidbody != null && config.forceMagnitude > 0)
            {
                // Calculate the force vector: horizontal push + optional upwards lift
                Vector3 force = (hitDirection + Vector3.up * config.upwardsForceModifier).normalized * config.forceMagnitude;
                _rigidbody.AddForce(force, ForceMode.Impulse);
                Debug.Log($"Applied <color=yellow>{force.magnitude:F2}</color> force to <color=cyan>{gameObject.name}</color>.");
            }

            // --- Apply Damage (Example: Would interact with an IDamageable interface or Health component) ---
            if (config.damageAmount > 0)
            {
                // In a real game, you would do something like:
                // if (TryGetComponent(out IDamageable damageable)) {
                //     damageable.TakeDamage(config.damageAmount);
                // } else {
                //     Debug.LogWarning($"No IDamageable component on {gameObject.name}. Cannot apply damage.");
                // }
                Debug.Log($"<color=red>{gameObject.name}</color> took <color=red>{config.damageAmount}</color> damage.");
            }

            // --- Apply Stun (Example: Would interact with an IStunnable interface or StatusEffectManager) ---
            if (config.stunDuration > 0)
            {
                // In a real game, you would do something like:
                // if (TryGetComponent(out IStunnable stunnable)) {
                //     stunnable.Stun(config.stunDuration);
                // } else {
                //     Debug.LogWarning($"No IStunnable component on {gameObject.name}. Cannot apply stun.");
                // }
                Debug.Log($"<color=magenta>{gameObject.name}</color> stunned for <color=magenta>{config.stunDuration}</color> seconds.");
            }

            // --- Play Hit Visual/Audio Effects on the target itself ---
            // You could spawn a small impact particle system or play a specific hit sound here.
            // Example: AudioSource.PlayClipAtPoint(hitSound, hitPoint);
            // Example: Instantiate(hitVFXPrefab, hitPoint, Quaternion.identity);
        }
    }
}
```

---

### Example Usage in Unity

To demonstrate how to use this ShockwaveEffectSystem in a Unity project:

#### 1. Setup the Scene

*   Create an empty GameObject in your scene and name it `ShockwaveSystem`.
*   Attach the `ShockwaveEffectSystem.cs` script to this GameObject.

#### 2. Create Shockwave Configurations

*   In your Project window, right-click -> `Create` -> `Scriptable Objects` -> `Shockwave Config`.
*   Create a few different configurations, e.g., `ExplosionShockwaveConfig`, `PushWaveConfig`.
*   **Configure `ExplosionShockwaveConfig`**:
    *   `Max Radius`: 15
    *   `Speed`: 10
    *   `Layer Mask`: Select "Default" or "Everything" (or specific layers for your targets).
    *   `Force Magnitude`: 1500
    *   `Upwards Force Modifier`: 0.7
    *   `Damage Amount`: 50
    *   `Stun Duration`: 3
    *   `VFX Prefab`: (Leave empty for now, or assign a simple particle system that scales)
    *   `SFX Clip`: (Assign an explosion sound if you have one)
*   **Configure `PushWaveConfig`**:
    *   `Max Radius`: 8
    *   `Speed`: 7
    *   `Layer Mask`: "Default"
    *   `Force Magnitude`: 800
    *   `Upwards Force Modifier`: 0.2
    *   `Damage Amount`: 0 (or a small amount)
    *   `Stun Duration`: 0
    *   `VFX Prefab`: (Leave empty)
    *   `SFX Clip`: (Assign a whoosh sound if you have one)

#### 3. Prepare Target Objects

*   Create some 3D Cube GameObjects in your scene. Position them around.
*   For each Cube:
    *   Add a `Rigidbody` component (ensure "Use Gravity" is checked and "Is Kinematic" is **unchecked** if you want force applied).
    *   Add the `ShockwaveTarget.cs` script.
    *   (Optional but recommended for `Physics.OverlapSphere` accuracy): Ensure the Cube has a `BoxCollider` (Unity adds one by default).

#### 4. Create a Trigger Script

Create a new C# script named `ShockwaveTrigger.cs`:

```csharp
using UnityEngine;
using MyGame.Effects; // Required to access ShockwaveEffectSystem and ShockwaveConfig

/// <summary>
/// Simple script to demonstrate triggering shockwaves.
/// Attach this to any GameObject (e.g., your player, an empty GameObject).
/// </summary>
public class ShockwaveTrigger : MonoBehaviour
{
    [Tooltip("The ShockwaveConfig asset to use when triggering.")]
    public ShockwaveConfig currentShockwaveConfig;

    [Tooltip("Key to press to trigger the shockwave.")]
    public KeyCode triggerKey = KeyCode.Space;

    [Tooltip("Offset from the trigger object's position for the shockwave origin.")]
    public Vector3 triggerOffset = Vector3.zero;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (currentShockwaveConfig == null)
            {
                Debug.LogError("ShockwaveTrigger: currentShockwaveConfig is not assigned!");
                return;
            }

            // Get the origin point for the shockwave
            Vector3 shockwaveOrigin = transform.position + triggerOffset;

            // Trigger the shockwave using the central system
            ShockwaveEffectSystem.Instance.TriggerShockwave(shockwaveOrigin, currentShockwaveConfig);
        }
    }

    // Optional: Draw a gizmo to show the trigger point offset
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + triggerOffset, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + triggerOffset);
    }
}
```

#### 5. Assign Trigger Script

*   Create an empty GameObject (or use your Player character). Name it `ShockwaveActivator`.
*   Attach the `ShockwaveTrigger.cs` script to it.
*   In the Inspector of `ShockwaveActivator`, drag your `ExplosionShockwaveConfig` asset into the `Current Shockwave Config` field.
*   You can set `Trigger Key` to `Space` (default) or any other key.

#### 6. Run the Scene

*   Play the scene.
*   Press the `Space` key (or your assigned trigger key).
*   Observe how the cubes with `ShockwaveTarget` components are pushed, and messages appear in the Console indicating damage and stun.
*   You can swap the `currentShockwaveConfig` on the `ShockwaveActivator` during runtime to test different shockwave types.

This complete setup demonstrates the `ShockwaveEffectSystem` pattern, offering a flexible and scalable solution for managing area-of-effect abilities in your Unity projects.