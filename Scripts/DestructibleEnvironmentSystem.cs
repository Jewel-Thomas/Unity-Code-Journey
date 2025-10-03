// Unity Design Pattern Example: DestructibleEnvironmentSystem
// This script demonstrates the DestructibleEnvironmentSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `DestructibleEnvironmentSystem` isn't a single, formally recognized design pattern like Singleton or Factory. Instead, it's an architectural concept or system in game development that often *leverages* several common design patterns to achieve its goal of creating interactive, destructible environments.

This example will demonstrate a practical implementation of such a system in Unity, highlighting how it uses patterns like:

1.  **Component Pattern:** The core of Unity development. `DestructibleObject` is a component attached to any GameObject that needs destruction logic.
2.  **Observer Pattern (via UnityEvents):** `DestructibleObject` exposes events (`OnDamaged`, `OnDestroyed`) that other systems (UI, AI, quest systems) can subscribe to, enabling loose coupling.
3.  **Strategy Pattern (Implicit/Configurable):** The specific way an object is destroyed (e.g., replacing with a shattered prefab, playing particles, applying physics force) is configured through inspector parameters, acting as a data-driven "strategy" for destruction.

---

## 1. `DestructibleObject.cs`

This is the central component for any object that can be damaged and destroyed.

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Events; // For Observer Pattern

// Define custom UnityEvent types for better type safety and clarity in the Inspector
[System.Serializable]
public class DestructibleEvent : UnityEvent<GameObject> { } // Event when object is destroyed
[System.Serializable]
public class DamageEvent : UnityEvent<float, GameObject> { } // Event when object takes damage (amount, damagedObject)

/// <summary>
/// DestructibleEnvironmentSystem: DestructibleObject Component
///
/// This script implements the core logic for any object that can be damaged and destroyed
/// within the game environment. It demonstrates a practical application of several
/// design patterns in Unity, making it educational and usable in real projects.
///
/// Design Patterns Illustrated:
/// 1.  Component Pattern: This script itself is a component that can be attached to
///     any GameObject. This allows you to add destruction capabilities to existing
///     objects (e.g., a wall, a crate, a barrier) without modifying their core classes.
///     It promotes modularity and reusability.
///
/// 2.  Observer Pattern (via UnityEvents): It exposes `OnDamaged` and `OnDestroyed`
///     UnityEvents. Other systems (e.g., UI for health bars, quest systems for "destroy X objects",
///     AI for reacting to cover destruction) can subscribe to these events. This creates
///     loose coupling: the DestructibleObject doesn't need to know who is listening,
///     and listeners don't need direct references to every destructible object.
///
/// 3.  Strategy Pattern (Implicit/Configurable): The specific behavior of destruction
///     (e.g., replacing with a shattered prefab, playing particle effects, applying
///     physics forces) is configured through inspector parameters. While not a formal
///     interface-based Strategy pattern, it allows for different "destruction strategies"
///     to be chosen and configured per object at design time, making the component flexible.
///     For more complex, runtime-swappable destruction behaviors, a formal Strategy
///     pattern with interfaces could be implemented (e.g., IDestructionStrategy).
///
/// How to use:
/// 1.  Attach this script to any GameObject you want to make destructible (e.g., a wall, a crate).
///     Ensure the GameObject has a Collider component so it can be hit by other objects.
/// 2.  Set its 'Max Health' in the Inspector.
/// 3.  (Optional) Create a 'Destroyed State Prefab': This is a prefab that will replace
///     the original object when it's destroyed. This could be a pre-fractured mesh,
///     a pile of debris, or an empty object if you only want particles.
///     If this prefab contains Rigidbodies, they will have an explosion force applied.
/// 4.  (Optional) Assign a 'Destruction Effect Prefab' (e.g., an explosion particle system).
/// 5.  (Optional) Assign a 'Destruction Sound' (e.g., an explosion sound effect).
/// 6.  (Optional) Configure 'Explosion Force' and 'Explosion Radius' for physical destruction.
/// 7.  Other scripts (e.g., projectiles, player attacks, environmental hazards) can
///     call the public `TakeDamage(float amount)` method to inflict damage.
/// 8.  You can subscribe to the 'OnDamaged' and 'OnDestroyed' UnityEvents from other
///     scripts or directly in the Inspector to react to these events.
/// </summary>
[RequireComponent(typeof(Collider))] // Destructible objects usually need a collider to receive hits
public class DestructibleObject : MonoBehaviour
{
    [Header("Destructible Properties")]
    [Tooltip("The initial and maximum health of this object.")]
    [SerializeField]
    private float maxHealth = 100f;

    [Tooltip("The current health of the object. Read-only at runtime.")]
    [SerializeField]
    private float currentHealth;

    [Tooltip("If true, the object is currently in a destroyed state.")]
    [SerializeField]
    private bool isDestroyed = false;

    [Header("Destruction Visuals & Effects")]
    [Tooltip("Prefab to instantiate when this object is destroyed (e.g., shattered pieces, rubble).")]
    [SerializeField]
    private GameObject destroyedStatePrefab;

    [Tooltip("Particle effect prefab to play on destruction (e.g., explosion).")]
    [SerializeField]
    private GameObject destructionEffectPrefab;

    [Tooltip("Sound clip to play on destruction.")]
    [SerializeField]
    private AudioClip destructionSound;

    [Tooltip("AudioSource component to play destruction sound. If none is assigned or found, one will be created temporarily.")]
    [SerializeField]
    private AudioSource audioSource;

    [Header("Physics Destruction")]
    [Tooltip("Applies an explosion force to rigidbodies found in the 'Destroyed State Prefab' (if it has any).")]
    [SerializeField]
    private bool applyExplosionForce = true;

    [Tooltip("The strength of the explosion force applied to destroyed pieces.")]
    [SerializeField]
    private float explosionForce = 500f;

    [Tooltip("The radius within which rigidbodies are affected by the explosion force.")]
    [SerializeField]
    private float explosionRadius = 5f;

    [Tooltip("The upward modifier for the explosion force, making debris fly upwards.")]
    [SerializeField]
    private float explosionUpwardModifier = 0.5f;

    [Header("Events (Observer Pattern)")]
    [Tooltip("Invoked when the object takes damage. Parameters: (amount of damage, damaged GameObject).")]
    public DamageEvent OnDamaged = new DamageEvent();

    [Tooltip("Invoked when the object is destroyed. Parameters: (destroyed GameObject).")]
    public DestructibleEvent OnDestroyed = new DestructibleEvent();

    /// <summary>
    /// Gets the current health of the object.
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// Gets the maximum health of the object.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Checks if the object is currently destroyed.
    /// </summary>
    public bool IsDestroyed => isDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth; // Initialize health to max on start
        isDestroyed = false;

        // Ensure an AudioSource exists or is assigned for playing sounds.
        // If not assigned in Inspector, try to get one, or add a temporary one.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // If no AudioSource, add one and configure it for destruction sounds.
                // This ensures sounds can play even if the original object is destroyed.
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false; // Don't play on scene load
                audioSource.spatialBlend = 1f;   // Make it a 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // Realistic falloff
                audioSource.maxDistance = 50f;   // How far the sound can be heard
            }
        }
    }

    /// <summary>
    /// Applies damage to the destructible object.
    /// This is the primary method for other systems to interact with the destructible.
    /// </summary>
    /// <param name="amount">The amount of damage to apply. Must be positive.</param>
    public void TakeDamage(float amount)
    {
        // Prevent damage if already destroyed or if damage amount is invalid
        if (isDestroyed || amount <= 0)
        {
            return;
        }

        currentHealth -= amount; // Reduce health
        OnDamaged?.Invoke(amount, gameObject); // Notify subscribers about damage taken

        // Check if health has dropped to or below zero
        if (currentHealth <= 0)
        {
            currentHealth = 0; // Ensure health doesn't go negative
            HandleDestruction(); // Trigger the destruction process
        }
    }

    /// <summary>
    /// Instantly destroys the object, bypassing health checks.
    /// Useful for instant environmental collapses, special events, or cheat codes.
    /// </summary>
    public void InstantDestroy()
    {
        if (isDestroyed) return; // Prevent multiple destructions
        currentHealth = 0;
        HandleDestruction();
    }

    /// <summary>
    /// Handles the visual, audio, and physics aspects of the object's destruction.
    /// This method embodies the configured "destruction strategy."
    /// </summary>
    private void HandleDestruction()
    {
        isDestroyed = true; // Mark as destroyed
        Debug.Log($"{gameObject.name} has been destroyed!", this); // Log for debugging

        // 1. Instantiate the destroyed state prefab (e.g., shattered pieces)
        if (destroyedStatePrefab != null)
        {
            GameObject destroyedPieces = Instantiate(destroyedStatePrefab, transform.position, transform.rotation);
            ApplyExplosionToPieces(destroyedPieces); // Apply physics to the new pieces
        }

        // 2. Play destruction effect (e.g., an explosion particle system)
        if (destructionEffectPrefab != null)
        {
            // Instantiate at object's position, then destroy it after its particle system finishes
            GameObject effect = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Destroy the effect GameObject once all particles have played out
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // If it's not a particle system, destroy after a short default delay
                Destroy(effect, 3f);
            }
        }

        // 3. Play destruction sound
        if (destructionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destructionSound);
        }

        OnDestroyed?.Invoke(gameObject); // Notify subscribers about the destruction

        // 4. Disable or destroy the original object.
        // We use a coroutine to allow sounds and effects to begin playing before the original object disappears.
        // The delay is based on the sound length to prevent abrupt cutoff.
        float destroyDelay = (destructionSound != null && audioSource != null) ? destructionSound.length : 0f;
        StartCoroutine(DeactivateAndDestroyRoutine(destroyDelay));
    }

    /// <summary>
    /// Coroutine to deactivate the original object's visuals/physics, then destroy it.
    /// </summary>
    /// <param name="delay">Time to wait before fully destroying the GameObject.</param>
    private IEnumerator DeactivateAndDestroyRoutine(float delay)
    {
        // Hide the original object immediately by disabling its renderer and collider
        if (TryGetComponent<Renderer>(out var rend)) rend.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        // If an AudioSource is playing a destruction sound on this GameObject, it needs time to finish.
        // We ensure a minimum delay even if no sound is playing.
        yield return new WaitForSeconds(Mathf.Max(delay, 0.1f));

        // After the delay, destroy the original GameObject.
        // For pooling scenarios, you would instead deactivate the GameObject here.
        Destroy(gameObject);
    }

    /// <summary>
    /// Applies an explosion force to all Rigidbody components found in the instantiated destroyed pieces.
    /// This gives a dynamic, physics-driven feel to the destruction.
    /// </summary>
    /// <param name="destroyedPiecesRoot">The root GameObject of the instantiated destroyed prefab.</param>
    private void ApplyExplosionToPieces(GameObject destroyedPiecesRoot)
    {
        if (!applyExplosionForce) return;

        // Get all rigidbodies in the destroyed pieces and apply an explosion force
        Rigidbody[] rbs = destroyedPiecesRoot.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, explosionUpwardModifier, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Draws the explosion radius in the editor for visualization during development.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (applyExplosionForce && explosionRadius > 0)
        {
            Gizmos.color = Color.red; // Visualize the explosion radius in red
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
```

---

## 2. `DamageDealer.cs`

This is an example script for a projectile or any entity that can inflict damage, demonstrating how it interacts with `DestructibleObject`.

```csharp
using UnityEngine;

/// <summary>
/// DestructibleEnvironmentSystem: DamageDealer Component (Example)
///
/// This script serves as a simple example of how a projectile or other
/// damaging entity would interact with a DestructibleObject.
/// It's designed to be attached to a projectile prefab (e.g., a bullet, an arrow, a magic missile)
/// or a melee attack trigger.
///
/// How to use:
/// 1.  Attach this script to a projectile GameObject.
///     Ensure the projectile has a Collider (set to IsTrigger if it's a non-physics projectile)
///     and a Rigidbody (if it's a physics projectile, even if kinematic).
/// 2.  Set the 'Damage Amount' in the Inspector.
/// 3.  (Optional) Set 'Destroy On Hit' to true if the projectile should disappear after hitting something.
/// 4.  (Optional) Assign a 'Hit Effect Prefab' (e.g., a spark, dust puff) to play on impact.
/// 5.  (Optional) Assign a 'Hit Sound' to play on impact.
/// 6.  When this projectile collides with an object containing a 'DestructibleObject' component,
///     it will call the `TakeDamage` method on that component.
/// </summary>
[RequireComponent(typeof(Collider))] // Projectile needs a collider to detect hits
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Properties")]
    [Tooltip("The amount of damage this entity inflicts upon a DestructibleObject.")]
    [SerializeField]
    private float damageAmount = 25f;

    [Tooltip("If true, this damage dealer will be destroyed after hitting any collider.")]
    [SerializeField]
    private bool destroyOnHit = true;

    [Tooltip("Delay before destroying this object after a hit, allowing effects/sounds to play.")]
    [SerializeField]
    private float destroyDelay = 0.1f;

    [Header("Hit Effects")]
    [Tooltip("Particle effect prefab to play on impact (e.g., sparks, dust).")]
    [SerializeField]
    private GameObject hitEffectPrefab;

    [Tooltip("Sound clip to play on impact (e.g., bullet hit sound).")]
    [SerializeField]
    private AudioClip hitSound;

    // Use OnCollisionEnter for physics-based projectiles (Collider.isTrigger = false)
    private void OnCollisionEnter(Collision collision)
    {
        // Pass the collider that was hit, the exact point of contact, and the normal of the surface
        HandleHit(collision.collider, collision.contacts[0].point, collision.contacts[0].normal);
    }

    // Use OnTriggerEnter for non-physics projectiles or area-of-effect damage (Collider.isTrigger = true)
    private void OnTriggerEnter(Collider other)
    {
        // For triggers, hit point and normal are approximated, as there's no direct contact info
        HandleHit(other, transform.position, Vector3.up); // Simplified hit point/normal for trigger
    }

    /// <summary>
    /// Processes a hit event, applying damage if the target is destructible and playing effects.
    /// </summary>
    /// <param name="other">The collider that was hit.</param>
    /// <param name="hitPoint">The world position of the impact.</param>
    /// <param name="hitNormal">The surface normal at the point of impact.</param>
    private void HandleHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Try to get DestructibleObject component from the hit object
        DestructibleObject destructible = other.GetComponent<DestructibleObject>();

        // If the hit object is destructible and not already destroyed
        if (destructible != null && !destructible.IsDestroyed)
        {
            // Apply damage to the destructible object
            destructible.TakeDamage(damageAmount);
            Debug.Log($"DamageDealer hit {other.name} for {damageAmount} damage. {other.name} health: {destructible.CurrentHealth}", other);

            // Play hit effect at the impact point
            PlayHitEffect(hitPoint, hitNormal);

            // Play hit sound at the impact point
            PlayHitSound(hitPoint);
        }
        else
        {
            // Optionally, handle hits on non-destructible objects (e.g., play a generic impact sound/effect)
            // Debug.Log($"DamageDealer hit {other.name}, but it's not destructible or already destroyed.");
            PlayHitEffect(hitPoint, hitNormal); // Still play generic impact if desired
            PlayHitSound(hitPoint); // Still play generic impact sound if desired
        }

        // Destroy the damage dealer (projectile) after a short delay
        if (destroyOnHit)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>
    /// Instantiates and plays a particle effect at the hit location.
    /// </summary>
    /// <param name="position">World position for the effect.</param>
    /// <param name="normal">Surface normal to orient the effect.</param>
    private void PlayHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            // Instantiate effect at hit point, aligned with hit normal (e.g., sparks spread away from surface)
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Destroy the effect GameObject once all particles have played out
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // If it's not a particle system, destroy after a short default delay
                Destroy(effect, 2f);
            }
        }
    }

    /// <summary>
    /// Plays a hit sound at the specified world position.
    /// </summary>
    /// <param name="position">World position for the sound.</param>
    private void PlayHitSound(Vector3 position)
    {
        if (hitSound != null)
        {
            // Play a 3D sound at the hit point with a default volume.
            // AudioSource.PlayClipAtPoint creates a temporary GameObject with an AudioSource.
            AudioSource.PlayClipAtPoint(hitSound, position, 0.7f);
        }
    }
}
```

---

## 3. `ProjectileLauncher.cs` (Example Controller)

This script serves as a simple user input controller to demonstrate launching projectiles.

```csharp
using UnityEngine;

/// <summary>
/// DestructibleEnvironmentSystem: ProjectileLauncher Component (Example)
///
/// This script provides a simple way to launch projectiles using player input.
/// It's a common pattern for player controllers or weapon systems to instantiate
/// and propel damaging entities like the `DamageDealer`.
///
/// How to use:
/// 1.  Create an empty GameObject in your scene (e.g., "Player" or "WeaponManager").
/// 2.  Attach this `ProjectileLauncher` script to it.
/// 3.  Assign your `Projectile_Prefab` (from the steps below) to the 'Projectile Prefab' slot in the Inspector.
/// 4.  Adjust 'Launch Force' to control projectile speed.
/// 5.  Position the GameObject with this script appropriately (e.g., at the player's weapon tip or camera position).
/// </summary>
public class ProjectileLauncher : MonoBehaviour
{
    [Tooltip("The prefab of the projectile to launch (should have a DamageDealer component).")]
    public GameObject projectilePrefab;

    [Tooltip("The force applied to the projectile upon launch.")]
    public float launchForce = 1000f;

    [Tooltip("The offset from the launcher's position where the projectile will spawn.")]
    public Vector3 spawnOffset = new Vector3(0, 0, 1f); // 1 unit in front

    void Update()
    {
        // Check for left mouse button click (or fire button)
        if (Input.GetMouseButtonDown(0))
        {
            LaunchProjectile();
        }
    }

    /// <summary>
    /// Instantiates a projectile and applies an initial force to it.
    /// </summary>
    void LaunchProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab not assigned to ProjectileLauncher! Please assign one in the Inspector.", this);
            return;
        }

        // Calculate spawn position (slightly in front of the launcher)
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);

        // Instantiate the projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, transform.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Apply force to propel the projectile forward
            rb.AddForce(transform.forward * launchForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Projectile prefab is missing a Rigidbody component! It might not move as expected.", projectile);
        }
        // The projectile will self-destroy via its DamageDealer script after hitting something.
    }
}
```

---

## Example Usage in Unity Editor

To see this system in action, follow these steps in your Unity project:

1.  **Create a `DestructibleObject`:**
    *   In the Hierarchy, create a 3D Object -> Cube. Rename it "DestructibleWall".
    *   Add the `DestructibleObject` component to "DestructibleWall".
    *   In the Inspector for `DestructibleObject`:
        *   Set 'Max Health' to, say, `50`.
        *   **Create a "Destroyed State Prefab":**
            *   Duplicate "DestructibleWall" in the Hierarchy. Rename the duplicate "DestroyedWall_Prefab".
            *   Remove the `DestructibleObject` component from "DestroyedWall_Prefab".
            *   Add a `Rigidbody` component to "DestroyedWall_Prefab".
            *   *(Optional: For more realistic destruction, you'd model a fractured version of your object, add rigidbodies to each piece, and make that your prefab. For this example, just a Rigidbody on the simple cube is fine.)*
            *   Drag "DestroyedWall_Prefab" from the Hierarchy into your Project panel to make it a prefab.
            *   Delete "DestroyedWall_Prefab" from the Hierarchy.
        *   Assign the "DestroyedWall_Prefab" (from your Project panel) to the 'Destroyed State Prefab' slot on your original "DestructibleWall".
        *   *(Optional)* Assign a particle effect prefab (e.g., "Explosion" from Unity's Standard Assets or a simple custom one) to 'Destruction Effect Prefab'.
        *   *(Optional)* Assign an audio clip (e.g., an explosion sound) to 'Destruction Sound'.
        *   Ensure 'Apply Explosion Force' is checked if your destroyed prefab has Rigidbodies.
    *   *(Optional)* Test the UnityEvents:
        *   In the Inspector for "DestructibleWall", expand 'OnDamaged' and 'OnDestroyed'.
        *   Click the '+' button. You can drag any object from your scene with a script that has a public method (e.g., a UI Manager, a ScoreManager, or even a simple `Debug.Log` callback).

2.  **Create a `DamageDealer` (Projectile):**
    *   In the Hierarchy, create a 3D Object -> Sphere. Rename it "Projectile_Prefab".
    *   Add the `DamageDealer` component to "Projectile_Prefab".
    *   Add a `Rigidbody` component to "Projectile_Prefab".
        *   Uncheck 'Use Gravity' (unless you want gravity-affected projectiles).
    *   Add a `SphereCollider` component to "Projectile_Prefab".
        *   Make sure 'Is Trigger' is unchecked if you want to use `OnCollisionEnter`. (For this example, we'll use `OnCollisionEnter`, so leave 'Is Trigger' unchecked.)
    *   In the Inspector for `DamageDealer`:
        *   Set 'Damage Amount' to, say, `20`.
        *   Check 'Destroy On Hit'.
        *   *(Optional)* Assign a particle effect prefab to 'Hit Effect Prefab'.
        *   *(Optional)* Assign an audio clip to 'Hit Sound'.
    *   Drag "Projectile_Prefab" from the Hierarchy into your Project panel to make it a prefab.
    *   Delete "Projectile_Prefab" from the Hierarchy.

3.  **Create a simple `ProjectileLauncher` (Controller):**
    *   In the Hierarchy, create an empty GameObject. Rename it "Player".
    *   Add the `ProjectileLauncher` component to the "Player" GameObject.
    *   In the Inspector for `ProjectileLauncher`:
        *   Assign "Projectile_Prefab" (from your Project panel) to the 'Projectile Prefab' slot.
        *   Adjust 'Launch Force' (e.g., `1000`).
    *   Position your "Player" GameObject in the scene so it faces the "DestructibleWall" and has a clear line of sight.

**Run the scene!** When you click the left mouse button, projectiles will be launched. When they hit the "DestructibleWall", its health will decrease. Once health reaches zero, it will be replaced by the "DestroyedWall_Prefab", play effects, and the original object will disappear.