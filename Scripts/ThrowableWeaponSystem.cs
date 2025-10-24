// Unity Design Pattern Example: ThrowableWeaponSystem
// This script demonstrates the ThrowableWeaponSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a 'ThrowableWeaponSystem' design pattern in Unity, which is a common approach for managing various types of thrown projectiles (grenades, axes, boomerangs, etc.) in a flexible, extensible, and performant manner.

The pattern combines elements of the **Strategy Pattern**, **Template Method Pattern**, and **Object Pool Pattern** to achieve its goals.

**Key Components of the ThrowableWeaponSystem Pattern:**

1.  **`IThrowable` Interface (Strategy)**: Defines the common contract for all throwable objects. This ensures that any object implementing this interface can be treated as a throwable weapon, regardless of its specific type.
2.  **`ThrowableWeaponBase` Abstract Class (Template Method)**: Provides a common base for all throwable weapons. It handles shared logic (e.g., physics component setup, basic collision detection, pooling mechanisms) and defines abstract methods that concrete weapon types *must* implement for their unique behaviors.
3.  **Concrete `ThrowableWeapon` Implementations (Concrete Strategies)**: Specific weapon types like `GrenadeWeapon` and `AxeWeapon` extend `ThrowableWeaponBase` and provide their unique logic for throwing, impacting, and resetting.
4.  **`ThrowableObjectPool` (Object Pool)**: A static helper class responsible for efficiently managing instances of throwable weapons. Instead of instantiating and destroying weapons frequently, it reuses inactive objects from a pool, significantly reducing performance overhead.
5.  **`ThrowableWeaponSystem` Manager (Context)**: This is the main component attached to the entity that performs the throwing (e.g., the player). It manages the currently selected throwable weapon, initiates the throwing action, and handles cooldowns. It interacts with `IThrowable` instances, abstracting away the concrete weapon type.
6.  **`IDamageable` Interface & `HealthComponent` (Helper)**: A simple mechanism to demonstrate how throwable weapons can interact with other game objects by dealing damage, following the Interface Segregation Principle.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- 1. IThrowable Interface ---
/// <summary>
/// Defines the contract for any object that can be thrown.
/// This is the core interface of the ThrowableWeaponSystem pattern, acting as the Strategy.
/// It ensures all throwable weapons expose a common set of methods for the system to interact with.
/// </summary>
public interface IThrowable
{
    /// <summary>
    /// Initializes the throwable weapon with its starting conditions.
    /// Called when the weapon is pulled from the pool and before its specific throw mechanics begin.
    /// </summary>
    /// <param name="thrower">The GameObject that is throwing this weapon (e.g., Player, Enemy).</param>
    /// <param name="initialPosition">The world position where the throw starts.</param>
    /// <param name="initialRotation">The world rotation of the weapon at throw start.</param>
    /// <param name="initialForce">The base force vector to apply for the throw (before weapon-specific multipliers).</param>
    void Initialize(GameObject thrower, Vector3 initialPosition, Quaternion initialRotation, Vector3 initialForce);

    /// <summary>
    /// Initiates the actual weapon-specific throwing action (e.g., applying physics force, starting timers).
    /// This method is called after Initialize.
    /// </summary>
    void Throw();

    /// <summary>
    /// Called when the throwable weapon hits something.
    /// Concrete implementations define what happens on impact (e.g., explode, deal damage, bounce).
    /// </summary>
    /// <param name="collision">The collision data from Unity's physics engine.</param>
    void OnHit(Collision collision);

    /// <summary>
    /// Resets the throwable weapon's state, making it ready to be returned to the object pool.
    /// This cleans up any active effects, timers, or physics states.
    /// </summary>
    void ResetThrowable();
}

// --- 2. ThrowableObjectPool (Object Pool Pattern) ---
/// <summary>
/// A simple static object pool for managing throwable weapons.
/// This is crucial for performance in games with many projectiles, as it reuses
/// GameObject instances instead of constantly instantiating and destroying them.
/// </summary>
public static class ThrowableObjectPool
{
    // Dictionary to hold pools for different prefabs. Key: original prefab, Value: list of pooled instances.
    private static Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();
    private static Transform poolParent; // Parent object in hierarchy to keep pooled objects organized.

    /// <summary>
    /// Gets a pooled object for a given prefab.
    /// If an inactive object exists in the pool, it's reused. Otherwise, a new one is instantiated.
    /// </summary>
    /// <param name="prefab">The original prefab to get an instance of.</param>
    /// <returns>A ready-to-use GameObject instance.</returns>
    public static GameObject GetPooledObject(GameObject prefab)
    {
        // Ensure the pool parent exists and is persistent across scene loads
        if (poolParent == null)
        {
            GameObject poolGO = new GameObject("ThrowableObjectPool");
            poolParent = poolGO.transform;
            Object.DontDestroyOnLoad(poolGO); // Prevent destruction when loading new scenes
        }

        // Create a new list for this prefab type if it doesn't exist
        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new List<GameObject>();
        }

        // Try to find an inactive object in the pool
        foreach (GameObject obj in pool[prefab])
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true); // Activate it for use
                return obj;
            }
        }

        // No inactive object found, so instantiate a new one
        GameObject newObj = Object.Instantiate(prefab, poolParent);
        newObj.SetActive(true); // Ensure it's active when first created and returned
        pool[prefab].Add(newObj); // Add to the pool list
        return newObj;
    }

    /// <summary>
    /// Returns an object to the pool, making it inactive and resetting its state.
    /// </summary>
    /// <param name="obj">The GameObject to return to the pool.</param>
    public static void ReturnPooledObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false); // Deactivate the object
            obj.transform.parent = poolParent; // Re-parent to the pool organization object
            
            // Call ResetThrowable on the object to clean up its state
            IThrowable throwable = obj.GetComponent<IThrowable>();
            if (throwable != null)
            {
                throwable.ResetThrowable();
            }
        }
    }
}

// --- 3. ThrowableWeaponBase Abstract Class (Template Method Pattern) ---
/// <summary>
/// Abstract base class for all throwable weapons.
/// This class implements the common functionalities shared by all throwables
/// and defines abstract methods for weapon-specific behaviors.
/// It uses the Template Method pattern by providing a skeletal structure
/// for the `Initialize` and `OnCollisionEnter` lifecycle, deferring
/// the specifics of `Throw` and `OnHit` to derived classes.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // All throwables need a Rigidbody for physics
[RequireComponent(typeof(Collider))] // All throwables need a Collider for impacts
public abstract class ThrowableWeaponBase : MonoBehaviour, IThrowable
{
    [Header("Base Throwable Settings")]
    [Tooltip("Multiplier applied to the base throw force from the ThrowableWeaponSystem.")]
    [SerializeField] protected float throwForceMultiplier = 1f;
    [Tooltip("Time in seconds before the weapon is returned to the object pool if it hasn't despawned (e.g., missed target).")]
    [SerializeField] protected float despawnDelay = 5f;
    [Tooltip("Optional: Tag of objects that specifically trigger an impact event (e.g., 'Enemy', 'Ground'). If empty, any collision is processed.")]
    [SerializeField] protected string impactTag = "";

    protected Rigidbody rb;
    protected Collider col;
    protected GameObject thrower; // Reference to the entity that threw this weapon
    protected bool isThrown = false; // Flag to ensure OnHit only processes valid impacts and prevents multiple hits from one weapon
    protected Coroutine despawnCoroutine; // Reference to the despawn coroutine to stop it if needed

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets references to Rigidbody and Collider components, which are required.
    /// </summary>
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Ensure collider is not a trigger for physical collisions
        if (col != null) col.isTrigger = false; 
    }

    /// <summary>
    /// Initializes the weapon's physical state (position, rotation, velocity) and sets its thrower.
    /// This method is called from the ThrowableWeaponSystem before the specific Throw method.
    /// </summary>
    /// <param name="thrower">The GameObject that threw this weapon.</param>
    /// <param name="initialPosition">The starting world position.</param>
    /// <param name="initialRotation">The starting world rotation.</param>
    /// <param name="initialForce">The base force vector for the throw.</param>
    public virtual void Initialize(GameObject thrower, Vector3 initialPosition, Quaternion initialRotation, Vector3 initialForce)
    {
        this.thrower = thrower;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        rb.isKinematic = false; // Ensure physics is active for movement
        rb.velocity = Vector3.zero; // Reset velocities
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false; // Disable gravity by default; concrete weapons enable it if needed
        
        isThrown = false; // Reset thrown state
        
        // Apply the initial force
        rb.AddForce(initialForce * throwForceMultiplier, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Abstract method for weapon-specific throwing logic.
    /// Derived classes must implement how the weapon behaves right after being initialized and sent off.
    /// (e.g., starting timers, applying visual effects, setting angular velocity).
    /// </summary>
    public abstract void Throw();

    /// <summary>
    /// Abstract method for weapon-specific hit logic.
    /// Derived classes must implement what happens when the weapon collides with something.
    /// (e.g., explode, deal damage, stick, bounce).
    /// </summary>
    /// <param name="collision">The collision data.</param>
    public abstract void OnHit(Collision collision);

    /// <summary>
    /// Resets the weapon's state for reuse from the object pool.
    /// This includes stopping movement, disabling physics, clearing flags, and cancelling any active coroutines.
    /// </summary>
    public virtual void ResetThrowable()
    {
        // Stop any active despawn or weapon-specific coroutines
        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }

        rb.isKinematic = true; // Make kinematic when in pool to prevent physics interference
        rb.useGravity = false; // Disable gravity
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        thrower = null; // Clear thrower reference
        isThrown = false; // Reset thrown flag
        transform.parent = ThrowableObjectPool.poolParent; // Ensure it's parented correctly in the pool
    }

    /// <summary>
    /// Handles Unity physics collision events.
    /// This method acts as a template hook, calling the abstract OnHit method for weapon-specific collision logic.
    /// </summary>
    /// <param name="collision">The collision data.</param>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Prevent processing hits if the weapon hasn't been "officially" thrown,
        // or if it's colliding with its own thrower (which might happen on spawn).
        if (!isThrown || collision.collider.gameObject == thrower) return;

        // If an impactTag is specified, only react to collisions with that tag.
        // If empty, react to any collision (except the thrower).
        if (!string.IsNullOrEmpty(impactTag) && !collision.collider.CompareTag(impactTag))
        {
            return; // Filter collisions by tag if specified
        }

        OnHit(collision); // Delegate to concrete weapon's hit logic
    }

    /// <summary>
    /// Coroutine to return the weapon to the pool after a set delay.
    /// This is used for weapons that might miss or need to disappear after a duration
    /// without explicitly hitting a target (e.g., a grenade fuse timer, or a missed axe).
    /// </summary>
    protected IEnumerator DespawnAfterDelayCo()
    {
        yield return new WaitForSeconds(despawnDelay);
        ThrowableObjectPool.ReturnPooledObject(gameObject); // Return to pool
    }
}

// --- 4. Concrete Throwable Weapons ---

/// <summary>
/// Represents a Grenade throwable weapon.
/// This concrete implementation defines grenade-specific behaviors like a fuse timer and explosion logic.
/// </summary>
public class GrenadeWeapon : ThrowableWeaponBase
{
    [Header("Grenade Settings")]
    [Tooltip("Time in seconds until the grenade explodes after being thrown.")]
    [SerializeField] private float fuseTime = 3f;
    [Tooltip("The radius of the explosion effect.")]
    [SerializeField] private float explosionRadius = 5f;
    [Tooltip("The physics force applied to Rigidbodies within the explosion radius.")]
    [SerializeField] private float explosionForce = 700f;
    [Tooltip("The amount of damage dealt to IDamageable objects within the explosion radius.")]
    [SerializeField] private int explosionDamage = 50;
    [Tooltip("Prefab for explosion visual effect (optional, will be instantiated at explosion point).")]
    [SerializeField] private GameObject explosionEffectPrefab;

    private bool hasExploded = false; // Flag to prevent multiple explosions

    /// <summary>
    /// Initializes the grenade, enables gravity, marks it as thrown, and starts the fuse timer.
    /// </summary>
    public override void Initialize(GameObject thrower, Vector3 initialPosition, Quaternion initialRotation, Vector3 initialForce)
    {
        base.Initialize(thrower, initialPosition, initialRotation, initialForce);
        hasExploded = false;
        rb.useGravity = true; // Grenades typically follow gravity immediately
        isThrown = true; // Mark as thrown to allow OnCollisionEnter checks

        // Start the fuse timer coroutine
        despawnCoroutine = StartCoroutine(FuseAndExplodeCo());
    }

    /// <summary>
    /// Grenade-specific throwing logic.
    /// The initial force is applied in `ThrowableWeaponBase.Initialize`.
    /// This method can be used for specific sound effects, visual cues, etc.
    /// </summary>
    public override void Throw()
    {
        Debug.Log($"{gameObject.name} thrown by {thrower.name}! Fuse started for {fuseTime}s.");
    }

    /// <summary>
    /// Grenade-specific hit logic. For a typical grenade, hitting something usually means it bounces or rolls,
    /// but doesn't necessarily explode immediately unless specifically configured (e.g., "impact grenade").
    /// </summary>
    /// <param name="collision">The collision data.</param>
    public override void OnHit(Collision collision)
    {
        Debug.Log($"{gameObject.name} bounced off {collision.gameObject.name}. (Continuing fuse)");
        // Add specific bouncing sound or effect here if desired.
        // If it was an "impact grenade", you might call Explode() here.
    }

    /// <summary>
    /// Resets the grenade's state for pooling.
    /// </summary>
    public override void ResetThrowable()
    {
        base.ResetThrowable();
        hasExploded = false;
    }

    /// <summary>
    /// Coroutine to manage the grenade's fuse and trigger the explosion after `fuseTime`.
    /// </summary>
    private IEnumerator FuseAndExplodeCo()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    /// <summary>
    /// Handles the explosion logic: applying physics force, dealing damage, and playing visual effects.
    /// </summary>
    private void Explode()
    {
        if (hasExploded) return; // Prevent multiple explosions
        hasExploded = true;

        Debug.Log($"{gameObject.name} EXPLODED at {transform.position}!");

        // 1. Play Visual/Audio Effect
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Destroy the effect after a short duration
        }

        // 2. Apply Explosion Force and Damage to nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            // Apply physics force
            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            // Deal damage to IDamageable objects (excluding the thrower)
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && hit.gameObject != thrower)
            {
                damageable.TakeDamage(explosionDamage);
                Debug.Log($"   Dealt {explosionDamage} damage to {hit.gameObject.name}");
            }
        }

        // 3. Return the grenade to the pool after explosion
        ThrowableObjectPool.ReturnPooledObject(gameObject);
    }
}

/// <summary>
/// Represents an Axe throwable weapon.
/// This concrete implementation handles axe-specific behaviors like rotation while flying,
/// and applying direct damage on impact.
/// </summary>
public class AxeWeapon : ThrowableWeaponBase
{
    [Header("Axe Settings")]
    [Tooltip("Amount of damage dealt on direct impact.")]
    [SerializeField] private int damageAmount = 25;
    [Tooltip("Visual rotation speed of the axe while flying (degrees per second around its forward axis).")]
    [SerializeField] private float rotationSpeed = 720f; // degrees per second

    /// <summary>
    /// Initializes the axe, enables gravity, marks it as thrown, and starts a despawn timer
    /// in case it misses or gets stuck somewhere.
    /// </summary>
    public override void Initialize(GameObject thrower, Vector3 initialPosition, Quaternion initialRotation, Vector3 initialForce)
    {
        base.Initialize(thrower, initialPosition, initialRotation, initialForce);
        rb.useGravity = true; // Axes usually follow gravity
        isThrown = true; // Mark as thrown immediately

        // Start despawn timer for cleanup if it doesn't hit anything or gets stuck
        despawnCoroutine = StartCoroutine(DespawnAfterDelayCo());
    }

    /// <summary>
    /// Axe-specific throwing logic. Applies angular velocity for a spinning effect.
    /// </summary>
    public override void Throw()
    {
        // Initial force applied in base.Initialize.
        // Add some angular velocity for a visual spinning effect.
        rb.AddRelativeTorque(Vector3.forward * rotationSpeed, ForceMode.VelocityChange);
        Debug.Log($"{gameObject.name} thrown by {thrower.name}!");
    }

    /// <summary>
    /// Axe-specific hit logic. Applies damage to the collided object and then despawns (returns to pool).
    /// </summary>
    /// <param name="collision">The collision data.</param>
    public override void OnHit(Collision collision)
    {
        // Ensure it only processes one hit per throw
        if (!isThrown) return; 

        Debug.Log($"{gameObject.name} hit {collision.gameObject.name}!");

        // Apply damage if the hit object is IDamageable and not the thrower
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && collision.gameObject != thrower)
        {
            damageable.TakeDamage(damageAmount);
            Debug.Log($"   Dealt {damageAmount} damage to {collision.gameObject.name}");
        }

        // For simplicity, the axe is returned to the pool after its first significant hit.
        // You could add logic here for it to stick into surfaces, or bounce off, etc.
        ThrowableObjectPool.ReturnPooledObject(gameObject);
        isThrown = false; // Prevent further hit processing from this instance
    }
}

// --- Helper Interface for Damageable Objects ---
/// <summary>
/// Simple interface for objects that can take damage.
/// This allows throwable weapons to interact with a variety of game objects
/// without needing specific knowledge of their health system implementation.
/// Adheres to the Interface Segregation Principle.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}

// --- Example Damageable Component ---
/// <summary>
/// A simple component to demonstrate an object taking damage.
/// Attach this to any GameObject that you want to be affected by throwable weapons.
/// </summary>
public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        // In a real game, this would trigger death animations, loot drops, disable colliders, etc.
        // For this example, we simply destroy the GameObject.
        Destroy(gameObject);
    }
}

// --- 5. ThrowableWeaponSystem (The Manager/Context) ---
/// <summary>
/// The main ThrowableWeaponSystem component.
/// This acts as the context or client for the `IThrowable` interface, managing
/// which weapon is currently selected, initiating throws, and handling cooldowns.
/// It uses the `ThrowableObjectPool` to efficiently manage weapon instances.
/// This class doesn't need to know the specific type of weapon, only that it
/// implements `IThrowable`, demonstrating the power of the Strategy Pattern.
/// </summary>
public class ThrowableWeaponSystem : MonoBehaviour
{
    [Header("Weapon System Settings")]
    [Tooltip("The point in world space from which weapons will be thrown.")]
    [SerializeField] private Transform throwPoint;
    [Tooltip("A list of throwable weapon prefabs that can be used. Each must have an IThrowable component.")]
    [SerializeField] private GameObject[] weaponPrefabs;
    [Tooltip("The initial index of the weapon prefab to start with from the `weaponPrefabs` array.")]
    [SerializeField] private int initialWeaponIndex = 0;
    [Tooltip("The base force applied when throwing a weapon. This is multiplied by the weapon's own `throwForceMultiplier`.")]
    [SerializeField] private float baseThrowForce = 20f;
    [Tooltip("Cooldown time in seconds between throws.")]
    [SerializeField] private float throwCooldown = 1f;

    private int currentWeaponIndex;
    private bool canThrow = true;

    void Start()
    {
        // Basic validation
        if (throwPoint == null)
        {
            Debug.LogError("Throw Point not assigned in ThrowableWeaponSystem! Disabling system.", this);
            enabled = false;
            return;
        }
        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            Debug.LogError("No weapon prefabs assigned in ThrowableWeaponSystem! Disabling system.", this);
            enabled = false;
            return;
        }

        // Set the initial weapon
        currentWeaponIndex = Mathf.Clamp(initialWeaponIndex, 0, weaponPrefabs.Length - 1);
        Debug.Log($"ThrowableWeaponSystem initialized. Current weapon: {weaponPrefabs[currentWeaponIndex].name}");
    }

    void Update()
    {
        // Example input for throwing (e.g., Left Mouse Button or Left Ctrl)
        if (Input.GetButtonDown("Fire1"))
        {
            TryThrowWeapon();
        }

        // Example input for switching weapons (e.g., Q and E keys)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchWeapon(-1); // Switch to previous weapon
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon(1); // Switch to next weapon
        }
    }

    /// <summary>
    /// Attempts to throw the currently selected weapon if the cooldown allows.
    /// This method orchestrates the creation, initialization, and throwing of a weapon.
    /// </summary>
    public void TryThrowWeapon()
    {
        if (!canThrow)
        {
            Debug.Log("Cannot throw: Weapon is on cooldown.");
            return;
        }

        // Validate current weapon selection
        if (weaponPrefabs == null || weaponPrefabs.Length == 0 || currentWeaponIndex < 0 || currentWeaponIndex >= weaponPrefabs.Length)
        {
            Debug.LogError("No valid weapon selected to throw. Check weaponPrefabs array.");
            return;
        }

        GameObject currentWeaponPrefab = weaponPrefabs[currentWeaponIndex];
        
        // 1. Get a weapon instance from the object pool
        GameObject weaponInstance = ThrowableObjectPool.GetPooledObject(currentWeaponPrefab);
        
        // 2. Ensure the instance has an IThrowable component
        IThrowable throwable = weaponInstance.GetComponent<IThrowable>();
        if (throwable == null)
        {
            Debug.LogError($"Weapon prefab '{currentWeaponPrefab.name}' does not have an IThrowable component! Cannot throw.", currentWeaponPrefab);
            ThrowableObjectPool.ReturnPooledObject(weaponInstance); // Return invalid object to pool
            return;
        }

        // 3. Calculate throw direction (e.g., forward from the throw point)
        Vector3 throwDirection = throwPoint.forward;
        Vector3 finalThrowForce = throwDirection * baseThrowForce;

        // 4. Initialize and Throw the weapon via its IThrowable interface
        throwable.Initialize(gameObject, throwPoint.position, throwPoint.rotation, finalThrowForce);
        throwable.Throw(); // Calls the weapon-specific throw logic

        // 5. Start cooldown
        canThrow = false;
        StartCoroutine(ThrowCooldownCo());

        Debug.Log($"Successfully threw: {currentWeaponPrefab.name}!");
    }

    /// <summary>
    /// Switches to the next or previous weapon in the `weaponPrefabs` array.
    /// </summary>
    /// <param name="direction">1 for next weapon, -1 for previous weapon.</param>
    public void SwitchWeapon(int direction)
    {
        if (weaponPrefabs == null || weaponPrefabs.Length <= 1)
        {
            Debug.Log("Cannot switch weapon: No or only one weapon available.");
            return;
        }

        currentWeaponIndex = (currentWeaponIndex + direction + weaponPrefabs.Length) % weaponPrefabs.Length;
        Debug.Log($"Switched to weapon: {weaponPrefabs[currentWeaponIndex].name}");
    }

    /// <summary>
    /// Coroutine to manage the throw cooldown, setting `canThrow` back to true after `throwCooldown` seconds.
    /// </summary>
    private IEnumerator ThrowCooldownCo()
    {
        yield return new WaitForSeconds(throwCooldown);
        canThrow = true;
        Debug.Log("Throwing cooldown finished. Ready to throw again.");
    }
}


/*
// --- HOW TO USE THIS THROWABLE WEAPON SYSTEM IN UNITY ---

1.  **Create a Player/Thrower GameObject:**
    *   Create an Empty GameObject in your scene (e.g., named "Player").
    *   Attach the `ThrowableWeaponSystem` script to this GameObject.

2.  **Create a "Throw Point":**
    *   As a child of your Player GameObject, create another Empty GameObject (e.g., named "ThrowPoint").
    *   Position this "ThrowPoint" where you want the weapons to originate from (e.g., slightly in front of the player's hand or camera).
    *   Drag this "ThrowPoint" GameObject into the `Throw Point` slot on the `ThrowableWeaponSystem` component in the Inspector.

3.  **Create Your Throwable Weapon Prefabs:**
    *   **For a Grenade (e.g., Sphere):**
        *   Create a new 3D Object (e.g., a "Sphere"). Name it "GrenadePrefab".
        *   Add a `Rigidbody` component. Ensure `Use Gravity` is checked.
        *   Add a `Sphere Collider` (or other appropriate collider) and ensure `Is Trigger` is OFF.
        *   Attach the `GrenadeWeapon` script.
        *   Adjust `Fuse Time`, `Explosion Radius`, `Explosion Force`, `Explosion Damage` in the Inspector as desired.
        *   (Optional) Create a simple particle effect prefab for the explosion and assign it to `Explosion Effect Prefab`.
        *   Drag this "GrenadePrefab" GameObject from the Hierarchy into your Project window to create a Prefab. Delete the instance from the Hierarchy.
    *   **For an Axe (e.g., Capsule):**
        *   Create a new 3D Object (e.g., a "Capsule"). Name it "AxePrefab". Adjust its scale and rotation for an axe-like appearance.
        *   Add a `Rigidbody` component. Ensure `Use Gravity` is checked.
        *   Add a `Capsule Collider` (or other appropriate collider) and ensure `Is Trigger` is OFF.
        *   Attach the `AxeWeapon` script.
        *   Adjust `Damage Amount`, `Rotation Speed` in the Inspector.
        *   Drag this "AxePrefab" GameObject from the Hierarchy into your Project window to create a Prefab. Delete the instance from the Hierarchy.

4.  **Assign Prefabs to the System:**
    *   On your Player's `ThrowableWeaponSystem` component in the Inspector, expand the `Weapon Prefabs` array.
    *   Drag your "GrenadePrefab" and "AxePrefab" into the array slots.
    *   Set `Initial Weapon Index` (e.g., 0 for Grenade, 1 for Axe).
    *   Adjust `Base Throw Force` and `Throw Cooldown` as needed.

5.  **Create Damageable Targets:**
    *   Create some 3D Objects in your scene (e.g., "Cube" or "Cylinder").
    *   Attach the `HealthComponent` script to these objects.
    *   Adjust `Max Health` in the Inspector. These objects will take damage from your thrown weapons.

6.  **Run the Scene:**
    *   Press Play.
    *   Press Left Mouse Button (`Fire1`) to throw the currently selected weapon.
    *   Press 'Q' or 'E' to switch between your Grenade and Axe.
    *   Observe how grenades explode and axes deal damage to `HealthComponent` objects.

// --- Design Pattern Breakdown ---

This 'ThrowableWeaponSystem' example utilizes several fundamental design patterns to achieve its flexibility, extensibility, and performance goals:

1.  **Strategy Pattern:**
    *   `IThrowable`: This interface defines the *strategy* (the common contract) for all throwable weapons. It declares methods like `Initialize`, `Throw`, `OnHit`, and `ResetThrowable`.
    *   `GrenadeWeapon` and `AxeWeapon`: These are *concrete strategies*, each providing its unique implementation of the `IThrowable` methods.
    *   `ThrowableWeaponSystem` (the manager): This acts as the *context* or *client*. It holds a reference to the currently selected `IThrowable` (via the `weaponPrefabs` array and `currentWeaponIndex`) and delegates the actual throwing operations to it. The manager doesn't need to know if it's throwing a grenade or an axe; it just knows it's throwing *something* that implements `IThrowable`. This allows for easy addition of new weapon types without modifying the core `ThrowableWeaponSystem` logic.

2.  **Template Method Pattern:**
    *   `ThrowableWeaponBase`: This abstract class serves as the *template*. It defines the skeletal structure of common operations (e.g., getting `Rigidbody`/`Collider`, basic `Initialize` steps, `OnCollisionEnter` handling, and the `ResetThrowable` logic for pooling). However, it defers (via abstract methods) the specific implementation details of `Throw()` and `OnHit()` to its concrete subclasses (`GrenadeWeapon`, `AxeWeapon`). `OnCollisionEnter` acts as a "hook" that invokes the abstract `OnHit` template method.

3.  **Object Pool Pattern:**
    *   `ThrowableObjectPool`: This static utility class implements the Object Pool pattern. It manages a collection of pre-instantiated (or dynamically created and then reused) game objects (our throwable weapons). Instead of frequently `Instantiate`ing and `Destroy`ing weapon prefabs, which can cause performance spikes due to memory allocation and garbage collection, the pool reuses inactive objects. This is crucial for games with many short-lived projectiles.

4.  **Interface Segregation Principle (from SOLID):**
    *   `IThrowable`: Keeps the interface focused solely on the behaviors related to being a throwable object.
    *   `IDamageable`: A separate, minimal interface for objects that can take damage. This ensures that classes only implement methods that are directly relevant to their role. A weapon needs to know *that* an object can take damage, not *how* it takes damage. This promotes loose coupling.

This combined approach makes the ThrowableWeaponSystem highly **flexible**, **extensible**, **performant**, and **maintainable**, making it an excellent example for Unity developers learning design patterns.
*/
```