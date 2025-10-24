// Unity Design Pattern Example: ToxicGasSystem
// This script demonstrates the ToxicGasSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "ToxicGasSystem" is not a standard, recognized design pattern in software engineering or game development. However, the request likely refers to a common game development challenge: creating an **Area of Effect (AoE) Hazard System** that applies continuous effects to entities within its range.

This example implements a "Toxic Gas Cloud System" that embodies these principles. It demonstrates:
*   **Loose Coupling** using an `IDamageable` interface.
*   **Component-Based Design** for reusability.
*   **Trigger-Based Detection** for environmental interactions.
*   **Coroutines** for managing timed effects.
*   **Efficient State Management** using a `HashSet` to track affected entities.

Below are three C# scripts:
1.  **`IDamageable.cs`**: An interface for any object that can take damage.
2.  **`HealthComponent.cs`**: A concrete implementation of `IDamageable`, demonstrating how entities can have health.
3.  **`ToxicGasCloudSystem.cs`**: The core script, which creates an environmental hazard that damages `IDamageable` entities over time.

---

### 1. `IDamageable.cs`

This interface defines a contract for any game object that can take damage. This is a fundamental step towards creating a flexible system, as the `ToxicGasCloudSystem` will interact with anything implementing this interface, without needing to know its specific class type.

```csharp
// IDamageable.cs
using UnityEngine;

/// <summary>
/// Defines an interface for any game object that can take damage.
/// This promotes loose coupling, allowing the ToxicGasCloudSystem to interact
/// with various entities (players, enemies, destructible objects)
/// without knowing their specific types.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the implementing entity.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    void TakeDamage(float amount);
    
    /// <summary>
    /// Gets whether the entity is currently alive.
    /// </summary>
    bool IsAlive { get; }
}
```

---

### 2. `HealthComponent.cs`

This script provides a basic health system that implements the `IDamageable` interface. Attach this to any GameObject (e.g., your player, enemies, destructible objects) that you want to be affected by the `ToxicGasCloudSystem`.

```csharp
// HealthComponent.cs
using UnityEngine;

/// <summary>
/// A concrete implementation of the IDamageable interface.
/// This component can be attached to any GameObject that needs health and can take damage.
/// </summary>
[DisallowMultipleComponent] // Ensures only one HealthComponent per GameObject
public class HealthComponent : MonoBehaviour, IDamageable
{
    [Tooltip("The maximum health of this entity.")]
    [SerializeField] private float maxHealth = 100f;

    private float _currentHealth;

    /// <summary>
    /// Gets the current health of the entity.
    /// </summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>
    /// Gets the maximum health of the entity.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Gets whether the entity is currently alive (health > 0).
    /// </summary>
    public bool IsAlive => _currentHealth > 0;

    /// <summary>
    /// Initializes the current health to the maximum health.
    /// </summary>
    void Awake()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the entity. If health drops to or below zero, the entity "dies".
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return; // Cannot take damage if already dead

        _currentHealth -= amount;
        Debug.Log($"<color=red>{gameObject.name}</color> took <color=orange>{amount}</color> damage. Current Health: <color=yellow>{_currentHealth}</color>");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Heals the entity by a specified amount, up to its maximum health.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(float amount)
    {
        if (!IsAlive) return; // Cannot heal if already dead

        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        Debug.Log($"<color=green>{gameObject.name}</color> healed <color=lime>{amount}</color>. Current Health: <color=yellow>{_currentHealth}</color>");
    }

    /// <summary>
    /// Handles the entity's death. In this example, it logs a message and disables components.
    /// In a real game, this might trigger animations, sound effects, particle effects,
    /// drop loot, or remove the object from the scene.
    /// </summary>
    private void Die()
    {
        Debug.Log($"<color=purple>{gameObject.name}</color> has died!");
        // Example: Disable renderer and collider to signify 'death' without destroying
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Renderer>(out var ren)) ren.enabled = false;
        // Or simply:
        // gameObject.SetActive(false); 
        // Destroy(gameObject); // Use with caution, might break references
    }

    /// <summary>
    /// Resets the entity's health to max. Useful for pooling or restarting levels.
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = maxHealth;
        // Re-enable components if they were disabled by Die()
        if (TryGetComponent<Collider>(out var col)) col.enabled = true;
        if (TryGetComponent<Renderer>(out var ren)) ren.enabled = true;
        Debug.Log($"<color=cyan>{gameObject.name}</color> health reset to <color=yellow>{_currentHealth}</color>.");
    }
}
```

---

### 3. `ToxicGasCloudSystem.cs`

This is the main script that implements the AoE hazard. It uses a `SphereCollider` as a trigger to detect `IDamageable` entities and applies damage over time using a coroutine. It also includes optional particle effects and sound.

```csharp
// ToxicGasCloudSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For .ToList() on HashSets

/// <summary>
/// <para>
/// **The 'ToxicGasSystem' Pattern (Interpreted as an AoE Hazard System)**
/// </para>
/// <para>
/// While 'ToxicGasSystem' is not a standard, recognized design pattern in software engineering
/// or game development, this script interprets the request as a common game development
/// scenario: an Area of Effect (AoE) environmental hazard system.
/// </para>
/// <para>
/// **Pattern Principles Demonstrated:**
/// </para>
/// <list type="bullet">
///     <item>
///         <term>Loose Coupling with Interfaces (IDamageable):</term>
///         <description>The system doesn't care *what* enters the gas, only that it can
///         take damage. This allows the gas to affect players, enemies, or any other
///         destructible object implementing `IDamageable`, making the system highly reusable.</description>
///     </item>
///     <item>
///         <term>Component-Based Architecture:</term>
///         <description>Leverages Unity's core strength by being a self-contained component
///         that can be added to any GameObject to create a hazardous zone.</description>
///     </item>
///     <item>
///         <term>Event-Driven Interaction (Triggers):</term>
///         <description>Uses Unity's physics triggers (`OnTriggerEnter`, `OnTriggerExit`)
///         to detect and track entities entering and leaving the hazard zone.</description>
///     </item>
///     <item>
///         <term>Time-Based Effects (Coroutines):</term>
///         <description>Manages continuous damage application and the cloud's duration
///         using C# coroutines, providing efficient and readable time-based logic.</description>
///     </item>
///     <item>
///         <term>State Management (HashSet):</term>
///         <description>Uses a `HashSet` to efficiently store and manage the unique set
///         of currently affected entities, allowing for quick additions, removals, and lookups.</description>
///     </item>
/// </list>
/// <para>
/// **Real-World Use Cases:**
/// </para>
/// <list type="bullet">
///     <item>Poison clouds, acid pools, lava fields.</item>
///     <item>Healing zones, buff/debuff zones.</item>
///     <item>Environmental hazards (blizzards, radiation, heat zones).</item>
///     <item>Traps that apply continuous effects.</item>
/// </list>
/// <para>
/// **How to Use This Script (Example Setup):**
/// </para>
/// <list type="number">
///     <item>
///         <term>Create IDamageable & HealthComponent:</term>
///         <description>
///             <list type="bullet">
///                 <item>Create a new C# Script named `IDamageable.cs` and paste the `IDamageable` interface code into it.</item>
///                 <item>Create a new C# Script named `HealthComponent.cs` and paste the `HealthComponent` class code into it.</item>
///             </list>
///         </description>
///     </item>
///     <item>
///         <term>Prepare your Damageable Entities (e.g., Player, Enemy):</term>
///         <description>
///             <list type="bullet">
///                 <item>Create a 3D Object (e.g., a Capsule or Cube) in your scene. Name it "Player" or "Enemy".</item>
///                 <item>Add a `HealthComponent` script to this GameObject.</item>
///                 <item>Add a `Rigidbody` component to this GameObject. Crucially, set its "Is Kinematic" property to `true`
///                 if you don't want it affected by physics, but still need trigger events. Otherwise, leave it `false`
///                 and ensure its collider is not also a trigger.</item>
///                 <item>Ensure your damageable entity has a Collider (e.g., Capsule Collider) that is *NOT* marked as "Is Trigger".
///                 This is so the gas cloud's trigger can detect its solid body.</item>
///             </list>
///         </description>
///     </item>
///     <item>
///         <term>Create the Toxic Gas Cloud:</term>
///         <description>
///             <list type="bullet">
///                 <item>Create an empty GameObject in your scene. Name it "ToxicGasCloud".</item>
///                 <item>Add the `ToxicGasCloudSystem.cs` script to this GameObject.</item>
///                 <item>In the Inspector, configure its parameters:
///                     <list type="bullet">
///                         <item>`Damage Amount`: e.g., 10 (damage per tick)</item>
///                         <item>`Damage Interval`: e.g., 1.5 (seconds between damage ticks)</item>
///                         <item>`Cloud Duration`: e.g., 30 (seconds before the cloud dissipates)</item>
///                         <item>`Effect Radius`: e.g., 5 (size of the gas cloud)</item>
///                         <item>`Gas Particle System Prefab`: (Optional) Create a simple particle system (e.g., from 'GameObject -> Effects -> Particle System')
///                         and drag it into this slot. Set its start color to green/purple for gas. Ensure 'Looping' is off if it's meant to dissipate.</item>
///                         <item>`Gas Sound Clip`: (Optional) Drag an audio clip here for a persistent gas sound.</item>
///                     </list>
///                 </item>
///             </list>
///         </description>
///     </item>
///     <item>
///         <term>Run the Scene:</term>
///         <description>Move your "Player" or "Enemy" into the "ToxicGasCloud". You should see debug messages
///         indicating damage being taken.</description>
///     </item>
/// </list>
/// </summary>
[RequireComponent(typeof(SphereCollider))] // Ensures the cloud always has a collider to detect entities
[RequireComponent(typeof(AudioSource))]    // Ensures an audio source for sound effects
public class ToxicGasCloudSystem : MonoBehaviour
{
    [Header("Gas Cloud Properties")]
    [Tooltip("The amount of damage applied per damage tick.")]
    [SerializeField] private float damageAmount = 5f;

    [Tooltip("The interval (in seconds) between damage applications.")]
    [SerializeField] private float damageInterval = 1.0f;

    [Tooltip("The total duration (in seconds) the gas cloud will exist before dissipating. Set to 0 for infinite.")]
    [SerializeField] private float cloudDuration = 15.0f;

    [Tooltip("The radius of the gas cloud's effect area. This determines the size of the Sphere Collider.")]
    [SerializeField] private float effectRadius = 5.0f;

    [Header("Visual & Audio")]
    [Tooltip("Prefab for the particle system representing the gas cloud visuals.")]
    [SerializeField] private GameObject gasParticleSystemPrefab;

    [Tooltip("Audio clip to play when the gas cloud is active.")]
    [SerializeField] private AudioClip gasSoundClip;

    private SphereCollider _sphereCollider;
    private AudioSource _audioSource;
    private ParticleSystem _particleSystemInstance;

    // Use a HashSet for efficient tracking of unique affected entities.
    // HashSets provide O(1) average time complexity for add, remove, and contains operations.
    private HashSet<IDamageable> _affectedEntities = new HashSet<IDamageable>();

    private Coroutine _damageCoroutine;
    private Coroutine _durationCoroutine;

    /// <summary>
    /// Initializes the gas cloud system: sets up collider, audio, and visual effects.
    /// </summary>
    void Awake()
    {
        // Ensure the collider is set up correctly
        _sphereCollider = GetComponent<SphereCollider>();
        _sphereCollider.isTrigger = true; // Essential for OnTriggerEnter/Exit events
        _sphereCollider.radius = effectRadius;

        // Set up the audio source
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = gasSoundClip;
        _audioSource.loop = true; // Gas sound should loop while active
        _audioSource.spatialBlend = 1.0f; // Make it a 3D sound
        _audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // More natural falloff
        _audioSource.maxDistance = effectRadius * 2; // Sound radius roughly twice the effect radius

        // Instantiate and set up particle system for visuals
        if (gasParticleSystemPrefab != null)
        {
            GameObject psGo = Instantiate(gasParticleSystemPrefab, transform.position, Quaternion.identity, transform);
            _particleSystemInstance = psGo.GetComponent<ParticleSystem>();

            if (_particleSystemInstance != null)
            {
                // Scale the particle system to match the cloud's effect radius
                // Note: Particle systems can be tricky to scale universally.
                // It's often better to design them to a specific scale or adjust
                // their emission/size properties. Here, we'll try a simple scale.
                psGo.transform.localScale = Vector3.one * (effectRadius / 5.0f); // Adjust multiplier as needed
                _particleSystemInstance.Play();
            }
        }
    }

    /// <summary>
    /// Starts the cloud's duration timer and initial sound playback.
    /// </summary>
    void Start()
    {
        if (cloudDuration > 0)
        {
            _durationCoroutine = StartCoroutine(ManageCloudDuration());
        }

        if (gasSoundClip != null)
        {
            _audioSource.Play();
        }

        Debug.Log($"<color=cyan>Toxic Gas Cloud</color> spawned at {transform.position} with radius {effectRadius}. " +
                  $"Damage: {damageAmount} every {damageInterval}s. Duration: {(cloudDuration > 0 ? cloudDuration + "s" : "Infinite")}");
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Attempt to get the IDamageable component from the entering GameObject.
        // This is where the loose coupling comes into play.
        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            if (damageable.IsAlive) // Only track living entities
            {
                if (_affectedEntities.Add(damageable)) // Add returns true if element was new
                {
                    Debug.Log($"<color=yellow>{other.name}</color> entered the toxic gas.");
                    // If this is the first entity to enter, start the damage routine.
                    if (_affectedEntities.Count == 1 && _damageCoroutine == null) // Check count to avoid restarting if already running
                    {
                        _damageCoroutine = StartCoroutine(ApplyDamageOverTime());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when another collider exits this trigger.
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    void OnTriggerExit(Collider other)
    {
        // Attempt to get the IDamageable component from the exiting GameObject.
        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            if (_affectedEntities.Remove(damageable)) // Remove returns true if element was found and removed
            {
                Debug.Log($"<color=green>{other.name}</color> exited the toxic gas.");
                // If no more entities are affected, stop the damage routine to save performance.
                if (_affectedEntities.Count == 0 && _damageCoroutine != null)
                {
                    StopCoroutine(_damageCoroutine);
                    _damageCoroutine = null;
                    Debug.Log("No more entities in gas. Stopping damage routine.");
                }
            }
        }
    }

    /// <summary>
    /// Coroutine responsible for applying damage to all affected entities periodically.
    /// </summary>
    private IEnumerator ApplyDamageOverTime()
    {
        while (_affectedEntities.Count > 0) // Continue as long as there are entities in the gas
        {
            // Create a temporary list to avoid modifying the collection while iterating.
            // This is crucial if an entity dies or exits the trigger during the iteration
            // and `OnTriggerExit` tries to remove it from `_affectedEntities`.
            List<IDamageable> currentAffected = _affectedEntities.ToList();

            foreach (IDamageable entity in currentAffected)
            {
                // Check if the entity is still alive and still in the affected set
                // (it might have died or exited between the ToList() call and this check).
                if (entity != null && entity.IsAlive && _affectedEntities.Contains(entity))
                {
                    entity.TakeDamage(damageAmount);
                }
                else if (entity != null && !entity.IsAlive) // If dead, remove from tracking
                {
                    _affectedEntities.Remove(entity);
                }
                // If entity is null, it means the GameObject was destroyed. It will naturally be removed by next sweep.
            }

            // After applying damage, check again if any entities remain.
            // If all have died or exited, the loop condition `_affectedEntities.Count > 0` will eventually become false.
            if (_affectedEntities.Count == 0)
            {
                Debug.Log("All entities in gas have been removed or died. Stopping damage routine.");
                _damageCoroutine = null; // Clear the reference as routine will end
                yield break; // Exit the coroutine
            }

            yield return new WaitForSeconds(damageInterval);
        }
        _damageCoroutine = null; // Ensure coroutine reference is null when it naturally ends
    }

    /// <summary>
    /// Coroutine to manage the total duration of the gas cloud.
    /// </summary>
    private IEnumerator ManageCloudDuration()
    {
        yield return new WaitForSeconds(cloudDuration);
        Debug.Log($"<color=orange>Toxic Gas Cloud</color> at {transform.position} dissipating after {cloudDuration} seconds.");
        DeactivateCloud();
    }

    /// <summary>
    /// Handles the dissipation and cleanup of the gas cloud.
    /// </summary>
    private void DeactivateCloud()
    {
        // Stop all active coroutines to prevent further damage or duration management.
        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }
        if (_durationCoroutine != null)
        {
            StopCoroutine(_durationCoroutine);
            _durationCoroutine = null;
        }

        // Stop and clean up particle effects
        if (_particleSystemInstance != null)
        {
            _particleSystemInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Destroy the particle system GameObject after its remaining particles fade
            Destroy(_particleSystemInstance.gameObject, _particleSystemInstance.main.duration + _particleSystemInstance.main.startLifetimeMultiplier);
        }

        // Stop audio playback
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        // Clear the list of affected entities
        _affectedEntities.Clear();

        // Optionally, disable the collider immediately
        _sphereCollider.enabled = false;

        // Finally, destroy the gas cloud GameObject after a short delay
        // to allow any remaining particle effects to finish.
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// Ensures coroutines are stopped if the GameObject is destroyed prematurely (e.g., scene unload).
    /// </summary>
    void OnDestroy()
    {
        if (_damageCoroutine != null) StopCoroutine(_damageCoroutine);
        if (_durationCoroutine != null) StopCoroutine(_durationCoroutine);
        if (_particleSystemInstance != null) Destroy(_particleSystemInstance.gameObject); // Clean up child particle system
    }

    /// <summary>
    /// Draws a gizmo in the editor to visualize the gas cloud's effect radius.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.3f); // Purple translucent
        Gizmos.DrawSphere(transform.position, effectRadius);
        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.7f); // Opaque purple outline
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
```