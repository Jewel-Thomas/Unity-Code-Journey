// Unity Design Pattern Example: AreaOfEffectSystem
// This script demonstrates the AreaOfEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Area of Effect (AoE) System design pattern in Unity is used to manage and apply effects to multiple entities within a defined geometric area. This is a common pattern for spells, explosions, healing zones, debuff fields, and more.

This example provides a complete, practical implementation ready to be dropped into your Unity project.

**Core Components of the AoE System:**

1.  **`IAoETargetable` Interface:** Defines what an object needs to implement to be affected by an AoE. This promotes loose coupling.
2.  **`AoEEffectData` Struct:** A simple data container to pass all relevant information about an effect (type, magnitude, source) to targetable objects.
3.  **`Health` Component:** A practical example of an `IAoETargetable` implementation, allowing entities to take damage or be healed.
4.  **`AreaOfEffectTrigger` Component:** The main component that defines an AoE. It specifies the shape, size, duration, effect type, and finds/applies effects to targets within its range.
5.  **`AoETester` Component (Example Usage):** A simple script to trigger an `AreaOfEffectTrigger` for demonstration purposes.

---

### **1. AoEEffectData.cs**
*(A simple struct to bundle effect information)*

```csharp
using UnityEngine;

/// <summary>
/// Defines the types of effects an Area of Effect system can apply.
/// Extend this enum with more effect types as needed (e.g., Stun, BuffSpeed, ApplyDOT).
/// </summary>
public enum EffectType
{
    Damage,
    Heal,
    // Add more effect types here as your game requires
    // Stun,
    // ApplyStatusEffect,
    // BuffAttribute,
}

/// <summary>
/// A data structure to hold all necessary information about an effect to be applied by an AoE.
/// This allows for a flexible and extensible way to pass effect data to targetable entities.
/// </summary>
public struct AoEEffectData
{
    /// <summary>
    /// The type of effect being applied (e.g., Damage, Heal).
    /// </summary>
    public EffectType Type;

    /// <summary>
    /// The magnitude of the effect (e.g., amount of damage, amount of healing).
    /// </summary>
    public float Magnitude;

    /// <summary>
    /// The duration of the effect if it's a persistent status effect (e.g., stun duration, DOT/HOT duration).
    /// For instant effects, this can be 0.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The GameObject that initiated this AoE effect. Useful for attributing effects or checking alliances.
    /// </summary>
    public GameObject Source;
}
```

---

### **2. IAoETargetable.cs**
*(The interface for objects that can be affected by an AoE)*

```csharp
using UnityEngine;

/// <summary>
/// Defines the contract for any game object that can be targeted and affected by an Area of Effect system.
/// This interface promotes loose coupling, allowing the AoE system to interact with various types of entities
/// without knowing their specific implementations (e.g., Player, Enemy, DestructibleObject).
/// </summary>
public interface IAoETargetable
{
    /// <summary>
    /// Applies a given effect data to this targetable entity.
    /// The implementation of this method will define how the entity reacts to different effects.
    /// </summary>
    /// <param name="effectData">The data structure containing information about the effect to apply.</param>
    void ApplyEffect(AoEEffectData effectData);

    /// <summary>
    /// Provides access to the GameObject associated with this targetable entity.
    /// This is useful for physics queries (e.g., checking collider parent) and debugging.
    /// </summary>
    /// <returns>The GameObject that this component is attached to.</returns>
    GameObject GetGameObject();
}
```

---

### **3. Health.cs**
*(An example implementation of `IAoETargetable`)*

```csharp
using UnityEngine;

/// <summary>
/// A simple Health component that implements the <see cref="IAoETargetable"/> interface.
/// This component allows a GameObject to take damage, be healed, and potentially react to other effects.
/// It demonstrates how different game objects can become part of the AoE system.
/// </summary>
public class Health : MonoBehaviour, IAoETargetable
{
    [Tooltip("The current health of this entity.")]
    public float currentHealth = 100f;

    [Tooltip("The maximum health this entity can have.")]
    public float maxHealth = 100f;

    [Tooltip("When true, debug messages will be logged for health changes.")]
    public bool debugLogs = true;

    /// <summary>
    /// Applies an effect based on the provided <see cref="AoEEffectData"/>.
    /// This method is the entry point for the AoE system to interact with this entity.
    /// </summary>
    /// <param name="effectData">The data containing the type and magnitude of the effect.</param>
    public void ApplyEffect(AoEEffectData effectData)
    {
        switch (effectData.Type)
        {
            case EffectType.Damage:
                currentHealth -= effectData.Magnitude;
                if (debugLogs)
                    Debug.Log($"{gameObject.name} took {effectData.Magnitude} damage from {effectData.Source?.name ?? "unknown source"}. Current Health: {currentHealth}/{maxHealth}", this);
                break;
            case EffectType.Heal:
                currentHealth += effectData.Magnitude;
                if (debugLogs)
                    Debug.Log($"{gameObject.name} healed {effectData.Magnitude} from {effectData.Source?.name ?? "unknown source"}. Current Health: {currentHealth}/{maxHealth}", this);
                break;
                // Extend this with other effect types as defined in AoEEffectData
                // case EffectType.Stun:
                //     Debug.Log($"{gameObject.name} was stunned for {effectData.Duration} seconds!");
                //     // Implement stun logic here (e.g., disable movement, play stun animation)
                //     break;
        }

        // Clamp health to ensure it stays within valid bounds (0 to maxHealth)
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Check for defeat condition
        if (currentHealth <= 0)
        {
            if (debugLogs)
                Debug.Log($"{gameObject.name} has been defeated!", this);
            OnDefeated();
        }
    }

    /// <summary>
    /// Provides access to the GameObject this component is attached to.
    /// Required by the <see cref="IAoETargetable"/> interface.
    /// </summary>
    /// <returns>This GameObject.</returns>
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    /// <summary>
    /// Called when the entity's health drops to 0 or below.
    /// Override or extend this method for specific death behaviors (e.g., play death animation, disable GameObject).
    /// </summary>
    protected virtual void OnDefeated()
    {
        // Example: Disable the GameObject or trigger a destruction
        // gameObject.SetActive(false);
        // Destroy(gameObject);
    }
}
```

---

### **4. AreaOfEffectTrigger.cs**
*(The main AoE component)*

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like .Where()

/// <summary>
/// The central component for defining and triggering an Area of Effect (AoE).
/// This script allows you to configure various aspects of an AoE, including its shape, size,
/// the type and magnitude of effect it applies, and its duration.
///
/// HOW IT WORKS:
/// 1.  **Configuration:** Set the AoE's shape (Sphere, Box, Cone), size, effect type (Damage, Heal, etc.),
///     magnitude, duration, pulse interval, and target layers in the Inspector.
/// 2.  **Activation:** Call the <see cref="ActivateAoE"/> method. This can be done from another script
///     (e.g., a player's spell casting script, an enemy's ability script, or the <see cref="AoETester"/> example).
/// 3.  **Target Finding:** Based on the configured shape, it uses Unity's physics overlaps
///     (OverlapSphere, OverlapBox, or a filtered OverlapSphere for Cone) to find colliders within the AoE.
/// 4.  **Target Filtering:** It then checks if the found colliders belong to an object that implements
///     <see cref="IAoETargetable"/>. This ensures only valid targets are affected.
/// 5.  **Effect Application:**
///     *   **Instant Effects:** If <see cref="effectDuration"/> is 0 or <see cref="maxPulses"/> is 1, effects are applied once.
///     *   **Continuous Effects:** If <see cref="effectDuration"/> is > 0 and <see cref="maxPulses"/> > 1 (or 0 for infinite),
///         it starts a coroutine that repeatedly applies the effect at <see cref="pulseInterval"/> until the duration ends
///         or max pulses are reached.
/// 6.  **Visualization:** Uses Gizmos to draw the AoE in the editor for easy debugging and setup.
///     Optionally, it can instantiate a <see cref="visualEffectPrefab"/> (e.g., a particle system) when activated.
///
/// HOW TO USE IN UNITY:
/// 1.  Create an empty GameObject in your scene (e.g., "Fireball_AoE" or "HealingZone").
/// 2.  Attach this `AreaOfEffectTrigger` script to it.
/// 3.  **Configure in Inspector:**
///     *   **Shape:** Choose `Sphere`, `Box`, or `Cone`.
///     *   **Radius/Size/Angle:** Adjust according to your chosen shape.
///     *   **Effect Type:** Select `Damage`, `Heal`, etc.
///     *   **Effect Magnitude:** Set the amount of damage/healing.
///     *   **Effect Duration:**
///         *   `0` for instant effects (applied once, then done).
///         *   `> 0` for continuous effects (will pulse over this duration).
///     *   **Pulse Interval:** If `Effect Duration` is > 0, how often the effect applies.
///     *   **Max Pulses:** If `Effect Duration` is > 0, the maximum number of times the effect will pulse.
///                       Set to 0 for infinite pulses within the duration.
///     *   **Target Layers:** Crucially, select the Unity layers that your targetable objects (e.g., players, enemies) reside on.
///     *   **Visual Effect Prefab (Optional):** Assign a particle system or other visual GameObject that spawns when the AoE activates.
/// 4.  **Create Targets:** For any GameObject you want to be affected (e.g., your player, enemy NPCs):
///     *   Ensure it has a `Collider` component (e.g., Box Collider, Capsule Collider).
///     *   Add the `Health` script (or any other script implementing `IAoETargetable`).
///     *   Assign its GameObject to one of the `Target Layers` you selected in the `AreaOfEffectTrigger`.
/// 5.  **Trigger the AoE:** Call the `ActivateAoE()` method from another script.
///     For testing, you can attach the `AoETester` script to this GameObject, or a separate one, and configure it
///     to activate on a key press or automatically.
/// </summary>
[DisallowMultipleComponent] // Ensures only one AoETrigger per GameObject
public class AreaOfEffectTrigger : MonoBehaviour
{
    // --- AoE Shape & Size Configuration ---
    public enum AoEShape { Sphere, Box, Cone }
    [Tooltip("The geometric shape of the Area of Effect.")]
    public AoEShape shape = AoEShape.Sphere;

    [Tooltip("The radius of the sphere or the length of the cone, in meters. Only applies to Sphere and Cone shapes.")]
    public float radius = 5f;

    [Tooltip("The dimensions of the box, in meters. Only applies to Box shape.")]
    public Vector3 boxSize = new Vector3(5, 5, 5);

    [Range(0, 180)]
    [Tooltip("The angle of the cone, in degrees. Only applies to Cone shape. (0 to 180 degrees).")]
    public float coneAngle = 45f;

    [Tooltip("The local direction of the cone's apex. For example, Vector3.forward will point along the Z-axis of this GameObject.")]
    public Vector3 coneDirection = Vector3.forward;

    // --- Effect Configuration ---
    [Header("Effect Properties")]
    [Tooltip("The type of effect this AoE will apply (e.g., Damage, Heal).")]
    public EffectType effectType = EffectType.Damage;

    [Tooltip("The magnitude of the effect (e.g., 10 for 10 damage/healing).")]
    public float effectMagnitude = 10f;

    [Tooltip("The duration over which the AoE is active. 0 for instant, >0 for continuous. " +
             "If > 0, effects will be applied repeatedly at 'Pulse Interval'.")]
    public float effectDuration = 0f; // 0 for instant, >0 for continuous/lasting

    [Tooltip("If 'Effect Duration' > 0, this is the time in seconds between each effect application (pulse).")]
    public float pulseInterval = 1f;

    [Tooltip("If 'Effect Duration' > 0, this is the maximum number of times the effect will pulse. " +
             "Set to 0 for infinite pulses until 'Effect Duration' ends.")]
    public int maxPulses = 1; // How many times it pulses if duration > 0. 1 means one pulse only regardless of duration.

    [Tooltip("Only colliders on these layers will be considered as potential targets.")]
    public LayerMask targetLayers;

    // --- Visuals & State ---
    [Header("Visuals & Debug")]
    [Tooltip("An optional particle system or visual prefab to instantiate when the AoE is activated.")]
    public GameObject visualEffectPrefab;

    [Tooltip("When true, debug messages will be logged for AoE activations and target findings.")]
    public bool debugLogs = true;

    private bool _isActive = false; // Internal flag to prevent multiple simultaneous activations
    private GameObject _visualEffectInstance; // Reference to the spawned visual effect


    /// <summary>
    /// Activates the Area of Effect.
    /// This method can be called from other scripts (e.g., a spell casting system, a weapon script).
    /// </summary>
    public void ActivateAoE()
    {
        if (_isActive)
        {
            if (debugLogs) Debug.LogWarning($"AoE '{gameObject.name}' is already active. Ignoring activation request.", this);
            return; // Prevent re-activation if already running
        }

        _isActive = true;
        if (debugLogs) Debug.Log($"AoE '{gameObject.name}' activated!", this);

        // --- Spawn Visual Effect ---
        if (visualEffectPrefab != null)
        {
            _visualEffectInstance = Instantiate(visualEffectPrefab, transform.position, transform.rotation);
            // Optionally parent the visual effect to the AoE trigger if it needs to follow its movement
            // _visualEffectInstance.transform.parent = transform;

            // Destroy the visual effect after the AoE's lifecycle or a short time for instant effects
            if (effectDuration > 0)
            {
                Destroy(_visualEffectInstance, effectDuration);
            }
            else // For instant effects, destroy after a short visual display time
            {
                Destroy(_visualEffectInstance, 2f);
            }
        }

        // --- Handle Instant vs. Continuous Effects ---
        if (effectDuration <= 0f || maxPulses == 1)
        {
            // Instant effect or single pulse continuous effect
            ApplyEffectOnce();
            _isActive = false; // Instant effects complete immediately
            // Optionally destroy this AoETrigger GameObject after an instant effect
            // Destroy(gameObject, 0.1f); // Add a small delay for any last frame updates
        }
        else
        {
            // Continuous effect
            StartCoroutine(ApplyEffectContinuously());
        }
    }

    /// <summary>
    /// Finds all targets within the AoE and applies the effect once.
    /// This is used for both instant effects and each pulse of a continuous effect.
    /// </summary>
    private void ApplyEffectOnce()
    {
        List<IAoETargetable> targets = FindTargets();
        if (debugLogs) Debug.Log($"AoE '{gameObject.name}' found {targets.Count} targets for this pulse.", this);

        foreach (IAoETargetable target in targets)
        {
            target.ApplyEffect(new AoEEffectData
            {
                Type = effectType,
                Magnitude = effectMagnitude,
                Duration = 0f, // Individual pulse is instant in terms of its application
                Source = gameObject
            });
        }
    }

    /// <summary>
    /// Coroutine for applying effects continuously over a duration.
    /// </summary>
    private IEnumerator ApplyEffectContinuously()
    {
        int currentPulses = 0;
        float startTime = Time.time;

        // Loop while the AoE is active, within its duration, and hasn't exceeded max pulses (if defined)
        while (_isActive && Time.time < startTime + effectDuration && (maxPulses == 0 || currentPulses < maxPulses))
        {
            ApplyEffectOnce(); // Apply the effect to targets
            currentPulses++;

            // Wait for the next pulse interval, ensuring we don't go over duration
            float waitTime = Mathf.Min(pulseInterval, (startTime + effectDuration) - Time.time);
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                break; // Duration ended before waiting for full interval
            }
        }

        // --- Cleanup after continuous effect ends ---
        _isActive = false;
        if (debugLogs) Debug.Log($"AoE '{gameObject.name}' finished its continuous duration (total pulses: {currentPulses}).", this);

        // If the visual effect was parented to this object and needs explicit destruction (not handled by Destroy(instance, time))
        if (_visualEffectInstance != null && _visualEffectInstance.transform.parent == transform)
        {
             Destroy(_visualEffectInstance);
        }
        // Optionally destroy this AoETrigger GameObject after its lifecycle
        // Destroy(gameObject);
    }

    /// <summary>
    /// Finds all <see cref="IAoETargetable"/> components within the defined AoE shape and layers.
    /// </summary>
    /// <returns>A list of unique targetable entities found.</returns>
    private List<IAoETargetable> FindTargets()
    {
        List<IAoETargetable> foundTargets = new List<IAoETargetable>();
        Collider[] hitColliders = new Collider[0]; // Initialize as empty array

        // Perform physics overlap based on the selected shape
        switch (shape)
        {
            case AoEShape.Sphere:
                hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayers);
                break;
            case AoEShape.Box:
                // OverlapBox uses half extents, so divide boxSize by 2
                hitColliders = Physics.OverlapBox(transform.position, boxSize / 2, transform.rotation, targetLayers);
                break;
            case AoEShape.Cone:
                // For cone, first get all colliders in a sphere, then filter by angle
                hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayers);
                Vector3 worldConeDirection = transform.TransformDirection(coneDirection).normalized; // Convert local direction to world
                hitColliders = hitColliders.Where(col =>
                {
                    Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                    return Vector3.Angle(worldConeDirection, directionToTarget) < coneAngle / 2;
                }).ToArray();
                break;
        }

        // Iterate through found colliders and extract IAoETargetable components
        foreach (Collider col in hitColliders)
        {
            // Use GetComponentInParent to handle cases where collider is on a child object
            IAoETargetable targetable = col.GetComponentInParent<IAoETargetable>();
            if (targetable != null && !foundTargets.Contains(targetable))
            {
                // Add to list only if it's a valid targetable object and not already added (to prevent duplicates
                // if an object has multiple colliders within the AoE).
                foundTargets.Add(targetable);
            }
        }
        return foundTargets;
    }

    // --- Editor-only Visualization using Gizmos ---
    void OnDrawGizmosSelected()
    {
        // Gizmos are drawn in the editor when the GameObject is selected
        Gizmos.color = Color.red; // Set a color for the AoE visualization

        switch (shape)
        {
            case AoEShape.Sphere:
                Gizmos.DrawWireSphere(transform.position, radius);
                break;
            case AoEShape.Box:
                // Apply the GameObject's transform for correct box orientation
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
                Gizmos.matrix = Matrix4x4.identity; // Reset Gizmos matrix after drawing
                break;
            case AoEShape.Cone:
                // Draw a wire sphere to indicate the outer radius
                Gizmos.DrawWireSphere(transform.position, radius);

                Vector3 worldConeDirection = transform.TransformDirection(coneDirection).normalized;
                Vector3 origin = transform.position;

                // To draw the cone, we need to find vectors at the edge of the cone's angle
                // We'll use cross products to get perpendicular vectors for rotating
                Vector3 right = Vector3.Cross(worldConeDirection, Vector3.up).normalized;
                if (right == Vector3.zero) // Handle cases where coneDirection is straight up/down
                {
                    right = Vector3.Cross(worldConeDirection, Vector3.right).normalized;
                }
                Vector3 up = Vector3.Cross(right, worldConeDirection).normalized;

                // Draw rays to represent the cone's edges (4 rays for a more visible cone)
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(coneAngle / 2, up) * worldConeDirection * radius);
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(-coneAngle / 2, up) * worldConeDirection * radius);
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(coneAngle / 2, right) * worldConeDirection * radius);
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(-coneAngle / 2, right) * worldConeDirection * radius);

                // Optionally, draw an arc at the end of the cone for better visualization
                // This is more complex and might not be necessary for simple cone visualization.
                // For a 2D arc, you'd need to define a plane perpendicular to the cone direction at 'radius' distance.
                break;
        }
    }
}
```

---

### **5. AoETester.cs**
*(An example script to demonstrate triggering an AoE)*

```csharp
using UnityEngine;

/// <summary>
/// A simple test script to demonstrate how to activate an <see cref="AreaOfEffectTrigger"/>.
/// Attach this script to a GameObject that also has an <see cref="AreaOfEffectTrigger"/> component,
/// or assign a reference to an <see cref="AreaOfEffectTrigger"/> from another GameObject.
/// </summary>
public class AoETester : MonoBehaviour
{
    [Tooltip("The AreaOfEffectTrigger component to activate. If left null, it will try to find one on this GameObject.")]
    public AreaOfEffectTrigger aoeTrigger;

    [Tooltip("The key to press to activate the AoE.")]
    public KeyCode activationKey = KeyCode.Space;

    [Tooltip("If greater than 0, the AoE will automatically activate after this delay in seconds when the scene starts.")]
    public float autoActivationDelay = 0f;

    void Start()
    {
        // If aoeTrigger is not manually assigned, try to get it from this GameObject
        if (aoeTrigger == null)
        {
            aoeTrigger = GetComponent<AreaOfEffectTrigger>();
        }

        // Log an error and disable the script if no AoETrigger is found
        if (aoeTrigger == null)
        {
            Debug.LogError("AoETester requires an AreaOfEffectTrigger component on this or a linked GameObject!", this);
            enabled = false; // Disable this component
            return;
        }

        // Schedule auto-activation if a delay is specified
        if (autoActivationDelay > 0)
        {
            Invoke("ActivateAoE", autoActivationDelay);
        }
    }

    void Update()
    {
        // Activate the AoE when the specified key is pressed
        if (Input.GetKeyDown(activationKey))
        {
            ActivateAoE();
        }
    }

    /// <summary>
    /// Calls the ActivateAoE method on the assigned AreaOfEffectTrigger.
    /// </summary>
    public void ActivateAoE()
    {
        if (aoeTrigger != null)
        {
            aoeTrigger.ActivateAoE();
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create C# Scripts:**
    *   Create a new C# script named `AoEEffectData.cs` and copy the code for `AoEEffectData` into it.
    *   Create a new C# script named `IAoETargetable.cs` and copy the code for `IAoETargetable` into it.
    *   Create a new C# script named `Health.cs` and copy the code for `Health` into it.
    *   Create a new C# script named `AreaOfEffectTrigger.cs` and copy the code for `AreaOfEffectTrigger` into it.
    *   Create a new C# script named `AoETester.cs` and copy the code for `AoETester` into it.

2.  **Create a Target Object (e.g., an Enemy):**
    *   In your Unity scene, create a 3D Object (e.g., a "Cube" or "Capsule"). Name it "Enemy_01".
    *   Add a `Health` component to "Enemy_01". You can leave default values.
    *   Make sure "Enemy_01" has a `Collider` (e.g., Box Collider for a Cube, Capsule Collider for a Capsule).
    *   **Crucially:** Assign "Enemy_01" to a specific `Layer` in the Inspector (e.g., create a new Layer called "Enemies" and assign it).

3.  **Create an AoE Trigger Object:**
    *   Create an empty GameObject in your scene. Name it "Fireball_AoE".
    *   Add an `AreaOfEffectTrigger` component to "Fireball_AoE".
    *   **Configure `AreaOfEffectTrigger` in the Inspector:**
        *   **Shape:** Choose `Sphere`.
        *   **Radius:** Set to `5`.
        *   **Effect Type:** Set to `Damage`.
        *   **Effect Magnitude:** Set to `25`.
        *   **Effect Duration:** Set to `0` (for an instant effect).
        *   **Target Layers:** Select the "Enemies" layer you created earlier.
        *   *Optional:* Drag a particle system prefab into the `Visual Effect Prefab` slot (e.g., from Unity's Standard Assets if you have them, or create a simple one).
    *   Add an `AoETester` component to "Fireball_AoE".
        *   The `AoE Trigger` field should automatically link to the component on the same GameObject.
        *   Set `Activation Key` to `Space`.
        *   You can set `Auto Activation Delay` to `0` for manual triggering, or `2` for it to activate 2 seconds after play.

4.  **Run the Scene:**
    *   Press Play.
    *   If you set `Auto Activation Delay`, observe the "Enemy_01" taking damage after the delay.
    *   Otherwise, press the `Space` key. You should see "Enemy_01" take damage in the Console window. If you have multiple enemies, all within the AoE range on the "Enemies" layer will be affected.

This setup demonstrates a fully functional, extensible Area of Effect System using a common design pattern in Unity.