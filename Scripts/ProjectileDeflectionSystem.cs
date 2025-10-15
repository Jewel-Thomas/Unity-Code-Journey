// Unity Design Pattern Example: ProjectileDeflectionSystem
// This script demonstrates the ProjectileDeflectionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ProjectileDeflectionSystem' is a design pattern (or rather, a common system architecture) in games where certain game objects (deflectors) can intercept and alter the trajectory and properties of other game objects (projectiles). This system often involves:

1.  **Projectiles:** Entities that travel through the game world, potentially dealing damage.
2.  **Deflectors:** Entities that can interact with projectiles to change their direction, speed, damage, and even their 'owner' (e.g., a player shield deflecting an enemy projectile back at the enemy).
3.  **Interaction Logic:** A mechanism, usually based on collision detection, to trigger the deflection process.

This C# Unity example demonstrates a robust and extensible implementation of the Projectile Deflection System using interfaces, component-based design, and Unity's physics system.

---

### ProjectileDeflectionSystem Pattern Explained

1.  **`IDeflectable` Interface (The Contract):**
    *   Defines a common interface that any game object capable of being deflected must implement.
    *   It specifies a `Deflect()` method, along with properties like `Owner`, `CurrentDirection`, and `CurrentSpeed`.
    *   This is crucial for polymorphism: the `Deflector` doesn't need to know the concrete type of projectile, only that it can be deflected via this interface.

2.  **`Projectile` Component (The Deflectable Entity):**
    *   Implements the `IDeflectable` interface.
    *   Manages its own movement using a `Rigidbody` (for physics-based movement).
    *   Handles its lifecycle (lifetime, destruction on impact).
    *   Has properties like `initialSpeed`, `damage`, and `owner`.
    *   Its `OnTriggerEnter` method checks for impacts on specified layers and ignores deflectors (as deflectors will call `Deflect` on it).
    *   The `Deflect()` method is where the projectile updates its direction, speed, damage, and owner based on the deflector's instructions. It also includes visual feedback (color change, effects).

3.  **`Deflector` Component (The Interceptor):**
    *   Has a `Collider` set as a `trigger` to detect incoming projectiles.
    *   In its `OnTriggerEnter` method, it checks if the colliding object implements `IDeflectable`.
    *   It prevents deflecting projectiles that already belong to its `deflectorOwner`.
    *   Calculates a new `deflectionDirection` based on its `DeflectionType` (e.g., `Reflect`, `TowardsDeflectorForward`, `Custom`).
    *   Calls the `IDeflectable.Deflect()` method on the projectile, passing in the new direction, its own owner, and potential speed/damage boosts.

4.  **`ProjectileSpawner` (Example Usage):**
    *   A utility script to demonstrate how projectiles are created and initialized with an owner and direction.
    *   This highlights how easily a `Projectile` can be integrated into existing firing mechanics.

---

### Complete C# Unity Scripts

Create these four C# scripts in your Unity project:

1.  **`IDeflectable.cs`**
2.  **`Projectile.cs`**
3.  **`Deflector.cs`**
4.  **`ProjectileSpawner.cs`**

```csharp
// --- 1. IDeflectable Interface ---
// This interface defines the contract for any object that can be deflected.
// It ensures that different types of projectiles or other deflectable entities
// can be interacted with consistently by a Deflector.
public interface IDeflectable
{
    /// <summary>
    /// Initiates the deflection process on the object.
    /// </summary>
    /// <param name="newDirection">The new direction the object should travel after deflection.</param>
    /// <param name="newOwner">The new owner of the object (e.g., the deflector itself or its owner).</param>
    /// <param name="speedMultiplier">A factor to adjust the object's speed (e.g., 1.5 for faster, 0.8 for slower).</param>
    /// <param name="damageMultiplier">A factor to adjust the object's damage output.</param>
    void Deflect(UnityEngine.Vector3 newDirection, UnityEngine.GameObject newOwner, float speedMultiplier = 1f, float damageMultiplier = 1f);

    /// <summary>
    /// Gets the current owner of the deflectable object.
    /// Used to prevent deflecting one's own projectiles.
    /// </summary>
    UnityEngine.GameObject Owner { get; }

    /// <summary>
    /// Gets the current direction of the deflectable object.
    /// </summary>
    UnityEngine.Vector3 CurrentDirection { get; }

    /// <summary>
    /// Gets the current speed of the deflectable object.
    /// </summary>
    float CurrentSpeed { get; }
}
```

```csharp
using UnityEngine;
using System.Collections; // Needed for IEnumerator for coroutines.

// --- 2. Projectile Component ---
// This script represents a projectile that can be fired and potentially deflected.
// It implements the IDeflectable interface, making it identifiable by Deflector objects.
[RequireComponent(typeof(Rigidbody))] // Projectiles typically need a Rigidbody for physics interactions.
[RequireComponent(typeof(Collider))] // Projectiles typically need a Collider for collision detection.
public class Projectile : MonoBehaviour, IDeflectable
{
    [Header("Projectile Settings")]
    [Tooltip("The initial travel speed of the projectile.")]
    [SerializeField] private float initialSpeed = 10f;
    [Tooltip("The damage this projectile deals on impact.")]
    [SerializeField] private float damage = 10f;
    [Tooltip("How long the projectile will exist before being destroyed, if it doesn't hit anything.")]
    [SerializeField] private float lifetime = 5f;
    [Tooltip("A LayerMask to filter which layers this projectile can cause an 'impact' on. " +
             "Objects on these layers will trigger OnImpact(). Deflectors are handled separately.")]
    [SerializeField] private LayerMask impactLayers; 
    [Tooltip("Optional visual effect to spawn when the projectile hits something (not deflected).")]
    [SerializeField] private GameObject impactEffectPrefab; 
    [Tooltip("Optional visual effect to spawn when the projectile is deflected.")]
    [SerializeField] private GameObject deflectionEffectPrefab; 
    [Tooltip("The default color of the projectile.")]
    [SerializeField] private Color defaultColor = Color.yellow;
    [Tooltip("The color the projectile changes to when it's deflected.")]
    [SerializeField] private Color deflectedColor = Color.red;

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private Vector3 currentDirection;
    private GameObject currentOwner;
    private float currentSpeed;
    private float currentDamage;
    private bool isDeflected = false; // Flag to ensure deflection logic runs only once.

    // IDeflectable Properties
    public GameObject Owner => currentOwner;
    public Vector3 CurrentDirection => currentDirection;
    public float CurrentSpeed => currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Configure Rigidbody for projectile movement:
        // - No gravity, as movement is controlled by velocity.
        // - Not kinematic, so physics engine handles velocity and trigger events.
        rb.useGravity = false;
        rb.isKinematic = false; 
        
        // Ensure the collider is a trigger for detecting both deflection and impact.
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Projectile '{gameObject.name}' collider is not set to 'Is Trigger'. Setting it now. " +
                             "Please ensure this in the Inspector for proper functionality.");
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Initializes and launches the projectile.
    /// </summary>
    /// <param name="direction">The initial normalized direction of travel.</param>
    /// <param name="owner">The GameObject that fired this projectile. Used to prevent friendly fire/deflection.</param>
    /// <param name="initialSpeedOverride">Optional: override the default initial speed.</param>
    /// <param name="damageOverride">Optional: override the default damage.</param>
    public void Initialize(Vector3 direction, GameObject owner, float initialSpeedOverride = -1f, float damageOverride = -1f)
    {
        currentDirection = direction.normalized;
        currentOwner = owner;
        currentSpeed = (initialSpeedOverride > 0) ? initialSpeedOverride : initialSpeed;
        currentDamage = (damageOverride > 0) ? damageOverride : damage;
        isDeflected = false;

        // Apply initial velocity. The Rigidbody will maintain this velocity.
        rb.velocity = currentDirection * currentSpeed;

        // Reset color to default in case it's a recycled projectile.
        if (meshRenderer != null)
        {
            meshRenderer.material.color = defaultColor;
        }

        // Start the coroutine to destroy the projectile after its lifetime.
        StartCoroutine(DestroyAfterLifetime(lifetime));
    }

    /// <summary>
    /// Called when the Collider other enters this projectile's trigger.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with its own owner or other projectiles to prevent self-hitting or unwanted chain reactions.
        if (other.gameObject == currentOwner || other.GetComponent<Projectile>() != null)
        {
            return;
        }

        // Check if the collided object is a Deflector.
        // If it is, we let the Deflector script handle the interaction by calling this projectile's Deflect() method.
        // This ensures the deflector's logic (e.g., deflection type, boosts) is applied correctly.
        if (other.GetComponent<Deflector>() != null)
        {
            return; // The Deflector will call our Deflect() method.
        }

        // If it's not a Deflector, then it's an impact with an environment object, enemy, or player.
        // We use the LayerMask to filter what constitutes an impact.
        if (((1 << other.gameObject.layer) & impactLayers) != 0)
        {
            OnImpact(other.gameObject);
        }
    }

    /// <summary>
    /// Handles the logic for when the projectile impacts something (not deflected).
    /// </summary>
    /// <param name="hitObject">The GameObject that was hit.</param>
    private void OnImpact(GameObject hitObject)
    {
        // --- Real-world use case ---
        // Here you would implement logic for dealing damage, applying status effects, etc.
        // Example: Deal damage to the hit object if it has a Health component.
        // HealthSystem health = hitObject.GetComponent<HealthSystem>();
        // if (health != null)
        // {
        //     health.TakeDamage(currentDamage, currentOwner);
        //     Debug.Log($"{gameObject.name} hit {hitObject.name} for {currentDamage} damage from {currentOwner.name}.");
        // }
        // else
        // {
        //     Debug.Log($"{gameObject.name} hit {hitObject.name}. No health system found.");
        // }

        // Spawn impact effect.
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the projectile after impact.
        Destroy(gameObject);
    }

    /// <summary>
    /// IDeflectable implementation: Called by a Deflector when this projectile is intercepted and deflected.
    /// </summary>
    /// <param name="newDirection">The new travel direction after deflection.</param>
    /// <param name="newOwner">The new owner of the projectile (typically the deflector's owner).</param>
    /// <param name="speedMultiplier">Factor to multiply current speed by.</param>
    /// <param name="damageMultiplier">Factor to multiply current damage by.</param>
    public void Deflect(Vector3 newDirection, GameObject newOwner, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        // Ensure deflection logic only runs once to prevent multiple deflections from a single interaction.
        if (isDeflected) return; 

        currentDirection = newDirection.normalized; // Normalize to ensure consistent speed.
        currentOwner = newOwner;
        currentSpeed *= speedMultiplier;
        currentDamage *= damageMultiplier;
        isDeflected = true;

        // Apply new velocity. Rigidbody will maintain this.
        rb.velocity = currentDirection * currentSpeed;

        // Change color to visually indicate deflection.
        if (meshRenderer != null)
        {
            meshRenderer.material.color = deflectedColor;
        }

        // Play deflection effect.
        if (deflectionEffectPrefab != null)
        {
            Instantiate(deflectionEffectPrefab, transform.position, Quaternion.LookRotation(currentDirection));
        }

        Debug.Log($"{gameObject.name} deflected by {newOwner.name} towards {newDirection.normalized}! " +
                  $"New speed: {currentSpeed}, New damage: {currentDamage}");

        // Optionally, reset lifetime or extend it after deflection.
        // This prevents deflected projectiles from immediately expiring if they were near the end of their original life.
        StopAllCoroutines(); // Stop any previous DestroyAfterLifetime coroutine.
        StartCoroutine(DestroyAfterLifetime(lifetime * 1.5f)); // Give deflected projectiles a longer life.
    }

    /// <summary>
    /// Coroutine to destroy the projectile after a specified time.
    /// </summary>
    /// <param name="time">The duration before destruction.</param>
    IEnumerator DestroyAfterLifetime(float time)
    {
        yield return new WaitForSeconds(time);
        if (gameObject != null) // Ensure it hasn't already been destroyed by other means (e.g., impact or another deflection).
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Helper to visualize the projectile's current direction in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = isDeflected ? Color.red : Color.yellow;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
        }
        else if (!Application.isPlaying && transform != null) // Draw initial direction in editor before play
        {
            Gizmos.color = defaultColor;
            Gizmos.DrawRay(transform.position, transform.forward * 2f); // Assuming initial direction is forward
        }
    }
}
```

```csharp
using UnityEngine;

// --- 3. Deflector Component ---
// This script represents an object that can intercept and deflect projectiles.
// It detects IDeflectable objects and calls their Deflect method, altering their properties.
[RequireComponent(typeof(Collider))] // A deflector needs a Collider to detect projectiles.
public class Deflector : MonoBehaviour
{
    [Header("Deflector Settings")]
    [Tooltip("The GameObject considered the 'owner' of this deflector (e.g., the player holding a shield). " +
             "Projectiles from this owner will not be deflected.")]
    [SerializeField] private GameObject deflectorOwner; 
    [Tooltip("Defines how the projectile's new direction is calculated after deflection.")]
    [SerializeField] private DeflectionType deflectionType = DeflectionType.Reflect;
    [Tooltip("Multiplier for projectile speed after deflection (e.g., 1.2 for 20% faster).")]
    [SerializeField] private float deflectionSpeedBoost = 1.2f; 
    [Tooltip("Multiplier for projectile damage after deflection (e.g., 1.5 for 50% more damage).")]
    [SerializeField] private float deflectionDamageBoost = 1.5f; 
    [Tooltip("Specific direction used if DeflectionType is set to 'Custom'.")]
    [SerializeField] private Vector3 customDeflectionDirection = Vector3.forward; 

    /// <summary>
    /// Defines different ways a projectile can be deflected.
    /// </summary>
    public enum DeflectionType
    {
        /// <summary>
        /// Standard physics reflection, where the projectile's direction is reflected off the deflector's 'up' vector (surface normal).
        /// Ideal for shields or reflective surfaces.
        /// </summary>
        Reflect,        
        /// <summary>
        /// The projectile is redirected to always travel in the deflector's local forward direction.
        /// Useful for directional force fields or specific abilities.
        /// </summary>
        TowardsDeflectorForward, 
        /// <summary>
        /// The projectile is redirected to travel in a specific, custom-defined direction.
        /// </summary>
        Custom          
    }

    void Awake()
    {
        // Ensure the collider component is set to 'Is Trigger' for detection without physical collision.
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"Deflector '{gameObject.name}' collider is not set to 'Is Trigger'. Setting it now. " +
                             "Please ensure this in the Inspector for proper functionality.");
            col.isTrigger = true;
        }

        // If no explicit owner is set in the Inspector, default to this GameObject itself.
        if (deflectorOwner == null)
        {
            deflectorOwner = gameObject;
        }
    }

    /// <summary>
    /// Called when another Collider enters this deflector's trigger.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Attempt to get the IDeflectable interface from the collided object.
        IDeflectable deflectableProjectile = other.GetComponent<IDeflectable>();

        // If the collided object is indeed deflectable:
        if (deflectableProjectile != null)
        {
            // IMPORTANT: Prevent a deflector from affecting projectiles that belong to its own owner.
            // This prevents players from deflecting their own shots, or shields from deflecting shots from their allied units.
            if (deflectableProjectile.Owner == deflectorOwner)
            {
                Debug.Log($"Ignored deflection of {other.name} by {gameObject.name}: Projectile's owner is the same as deflector's owner.");
                // Optionally, play a "deflection failed" sound or visual effect here.
                return;
            }

            // Calculate the new deflection direction based on the chosen DeflectionType.
            Vector3 newDeflectionDirection = Vector3.forward; // Initialize with a default, will be overwritten.

            switch (deflectionType)
            {
                case DeflectionType.Reflect:
                    // Reflects the incoming projectile's direction off the deflector's local 'up' vector.
                    // 'transform.up' is commonly used as the outward-facing normal for flat shields.
                    newDeflectionDirection = Vector3.Reflect(deflectableProjectile.CurrentDirection, transform.up);
                    break;
                case DeflectionType.TowardsDeflectorForward:
                    // Projectile is redirected to move in the deflector's forward direction.
                    newDeflectionDirection = transform.forward;
                    break;
                case DeflectionType.Custom:
                    // Projectile is redirected to move in a user-defined custom direction.
                    newDeflectionDirection = customDeflectionDirection.normalized;
                    break;
            }

            // Perform the deflection by calling the Deflect method on the projectile itself.
            // Pass the new direction, the deflector's owner, and any speed/damage boosts.
            deflectableProjectile.Deflect(newDeflectionDirection, deflectorOwner, deflectionSpeedBoost, deflectionDamageBoost);
            
            // --- Real-world use case ---
            // Optional: Play a sound or particle effect specific to the deflector.
            // AudioSource audioSource = GetComponent<AudioSource>();
            // if (audioSource != null) audioSource.PlayOneShot(deflectionSound);
        }
    }

    /// <summary>
    /// Helper to visualize the deflector's forward, up (for reflection normal), and custom direction in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        // Visualize the deflector's forward direction (blue).
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1f); 
        Gizmos.DrawSphere(transform.position + transform.forward * 1f, 0.1f); // Arrowhead

        // Visualize specific deflection directions based on type.
        if (deflectionType == DeflectionType.Custom)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, customDeflectionDirection.normalized * 1f); 
            Gizmos.DrawSphere(transform.position + customDeflectionDirection.normalized * 1f, 0.1f);
        }
        else if (deflectionType == DeflectionType.Reflect)
        {
            // Show the 'normal' used for reflection (transform.up), which is common for shield surfaces.
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 1f);
            Gizmos.DrawSphere(transform.position + transform.up * 1f, 0.1f);
        }
    }
}
```

```csharp
using UnityEngine;

// --- 4. Projectile Spawner (Example Usage) ---
// This script demonstrates how to create and launch projectiles, assigning them an owner.
// It serves as a testing ground for the ProjectileDeflectionSystem.
public class ProjectileSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The Prefab of the Projectile GameObject to be spawned.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The Transform from which projectiles will originate and whose forward direction they will initially travel.")]
    [SerializeField] private Transform firePoint; 
    [Tooltip("The rate at which projectiles are fired per second.")]
    [SerializeField] private float fireRate = 1f; 
    [Tooltip("The initial speed of the spawned projectiles.")]
    [SerializeField] private float projectileSpeed = 15f;
    [Tooltip("The damage of the spawned projectiles.")]
    [SerializeField] private float projectileDamage = 10f;
    [Tooltip("The GameObject that owns the spawned projectiles. This is passed to the Projectile component.")]
    [SerializeField] private GameObject spawnerOwner; 

    private float nextFireTime;

    void Awake()
    {
        // If no firePoint is explicitly set, use the spawner's own transform.
        if (firePoint == null)
        {
            firePoint = transform;
        }
        // If no spawnerOwner is explicitly set, use this GameObject as the owner.
        if (spawnerOwner == null)
        {
            spawnerOwner = gameObject;
        }
    }

    void Update()
    {
        // Example: Fire projectiles automatically at a set rate.
        // In a real game, this might be triggered by player input (e.g., Input.GetButtonDown("Fire1"))
        // or AI logic (e.g., detecting an enemy).
        if (Time.time >= nextFireTime)
        {
            SpawnProjectile();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    /// <summary>
    /// Instantiates and initializes a new projectile.
    /// </summary>
    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned in ProjectileSpawner!", this);
            return;
        }

        // Instantiate the projectile at the fire point's position and rotation.
        GameObject newProjectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile newProjectile = newProjectileGO.GetComponent<Projectile>();

        if (newProjectile != null)
        {
            // Initialize the projectile with its direction, owner, speed, and damage.
            // This is where the ProjectileDeflectionSystem starts:
            // The projectile now knows its origin and owner, which is crucial for deflection logic.
            newProjectile.Initialize(firePoint.forward, spawnerOwner, projectileSpeed, projectileDamage);
        }
        else
        {
            Debug.LogError("Projectile Prefab does not have a Projectile component! Destroying invalid GameObject.", newProjectileGO);
            Destroy(newProjectileGO); // Clean up if the prefab is not set up correctly.
        }
    }

    /// <summary>
    /// Helper to visualize the fire point and its firing direction in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        Transform drawPoint = (firePoint != null) ? firePoint : transform;
        
        Gizmos.color = Color.magenta;
        // Draw a ray to show the firing direction.
        Gizmos.DrawRay(drawPoint.position, drawPoint.forward * 1f);
        // Draw a sphere to indicate the fire point's origin.
        Gizmos.DrawSphere(drawPoint.position, 0.1f);
    }
}
```

---

### Unity Setup Instructions

Follow these steps to set up the example in your Unity project:

1.  **Create a Projectile Prefab:**
    *   In your Unity Hierarchy, go to `Create -> 3D Object -> Sphere`.
    *   Rename the new Sphere GameObject to `Projectile_Prefab`.
    *   Add a `Rigidbody` component to `Projectile_Prefab`:
        *   Uncheck `Use Gravity`.
        *   Keep `Is Kinematic` unchecked (so the Rigidbody's velocity moves it).
    *   Add a `Sphere Collider` component to `Projectile_Prefab`:
        *   **Crucially, check `Is Trigger`**. This allows `OnTriggerEnter` to detect collisions without physical blocking.
    *   Add the `Projectile.cs` script to `Projectile_Prefab`.
    *   Configure the `Projectile.cs` script in the Inspector:
        *   Set `Initial Speed`, `Damage`, `Lifetime` as desired.
        *   Set `Default Color` (e.g., Yellow) and `Deflected Color` (e.g., Red).
        *   **Set the `Impact Layers`:** Click the dropdown and select the layers your projectile should hit (e.g., create a `Walls` layer and select it). You can create new layers via `Layers -> Add Layer...` in the Inspector.
        *   (Optional) Assign `Impact Effect Prefab` and `Deflection Effect Prefab` if you have particle systems or other visual effects.
    *   Drag `Projectile_Prefab` from the Hierarchy into your Project window (e.g., into a `Prefabs` folder) to create a Prefab.
    *   You can now delete `Projectile_Prefab` from the Hierarchy.

2.  **Create Deflector Objects:**
    *   In your Unity Hierarchy, go to `Create -> 3D Object -> Cube`.
    *   Rename it to `Deflector_Shield_Reflect`.
    *   Add a `Box Collider` component to `Deflector_Shield_Reflect`:
        *   **Crucially, check `Is Trigger`**.
    *   Add the `Deflector.cs` script to `Deflector_Shield_Reflect`.
    *   Configure the `Deflector.cs` script:
        *   Leave `Deflector Owner` empty for now; it will default to the Deflector GameObject itself. If this shield belongs to a `Player` GameObject, drag the `Player` GameObject into this slot.
        *   Set `Deflection Type` to `Reflect`.
        *   Adjust `Deflection Speed Boost` and `Deflection Damage Boost` as desired (e.g., 1.2 and 1.5).
    *   Position and rotate `Deflector_Shield_Reflect` in your scene. For `Reflect` type, the projectile will reflect off its local `transform.up` direction. Rotate it so its green Gizmo arrow points in the direction you want it to reflect incoming projectiles.

    *   **Create another Deflector (e.g., for `TowardsDeflectorForward`):**
        *   Duplicate `Deflector_Shield_Reflect`. Rename it `Deflector_Forward_Gate`.
        *   Change its `Deflection Type` to `TowardsDeflectorForward`.
        *   Rotate it to make its blue Gizmo arrow (`transform.forward`) point in the desired deflection direction.

3.  **Create Spawner Objects:**
    *   In your Unity Hierarchy, go to `Create -> Empty`.
    *   Rename it `Enemy_Projectile_Spawner`.
    *   Add the `ProjectileSpawner.cs` script to `Enemy_Projectile_Spawner`.
    *   Configure the `ProjectileSpawner.cs` script:
        *   Drag your `Projectile_Prefab` from the Project window into the `Projectile Prefab` slot.
        *   Create another Empty GameObject as a child of `Enemy_Projectile_Spawner`. Name it `FirePoint`. Position it slightly in front of the spawner (e.g., Local Position: 0, 0, 1).
        *   Drag this `FirePoint` child GameObject into the `Fire Point` slot on the `Enemy_Projectile_Spawner`.
        *   Set `Spawner Owner` to the `Enemy_Projectile_Spawner` GameObject itself.
        *   Adjust `Fire Rate`, `Projectile Speed`, `Projectile Damage` as needed.
    *   Position `Enemy_Projectile_Spawner` in your scene so it fires towards your Deflectors.

    *   (Optional) Create a `Player_Projectile_Spawner` in a similar way, perhaps placed at your player character's position, and set its `Spawner Owner` to your Player GameObject.

4.  **Physics Layers (Highly Recommended for Collision Filtering):**
    *   Go to `Edit -> Project Settings -> Physics`.
    *   In the `Layers` dropdown (top right of the Inspector), click `Add Layer...`
    *   Add new layers, for example:
        *   `Projectiles` (at an empty User Layer slot, e.g., Layer 8)
        *   `Deflectors` (e.g., Layer 9)
        *   `Walls` (e.g., Layer 10)
    *   Assign your `Projectile_Prefab` to the `Projectiles` layer.
    *   Assign your `Deflector_Shield_Reflect` and `Deflector_Forward_Gate` to the `Deflectors` layer.
    *   Assign any environment objects (e.g., Cubes, Planes) that projectiles should hit to the `Walls` layer.
    *   Go back to `Edit -> Project Settings -> Physics`.
    *   Look at the "Layer Collision Matrix" at the bottom.
    *   Uncheck collisions between `Projectiles` and `Deflectors` if you want ONLY trigger events, not physical collisions. (Since both are triggers, the physical collision will usually be ignored anyway, but it's good practice).
    *   Ensure `Projectiles` can collide/trigger with `Walls` (and any other layers you want them to impact).
    *   On your `Projectile_Prefab`'s `Projectile.cs` script, select the `Impact Layers` dropdown and choose `Walls` (and any other layers where you want `OnImpact` to occur).

Now, run your Unity scene. You should see projectiles being fired. When they hit a deflector, they will change color, direction, speed, and damage (as configured), effectively being deflected by the system!