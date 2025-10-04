// Unity Design Pattern Example: EnvironmentalHazardSystem
// This script demonstrates the EnvironmentalHazardSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'EnvironmentalHazardSystem' is a design pattern often used in game development to manage areas or objects that can inflict damage or status effects on entities within them. It promotes a modular, data-driven approach to creating environmental dangers like lava pits, poison clouds, or freezing zones.

This pattern typically involves:
1.  **Hazard Data (Strategy/Data Object):** A ScriptableObject that defines the properties of a specific hazard type (e.g., damage amount, damage type, tick rate, status effects). This allows designers to create new hazard types without writing new code.
2.  **Damageable Interface:** An interface (`IDamageable`) that any game entity (player, enemy, destructible object) capable of being affected by a hazard must implement. This ensures loose coupling, allowing the hazard system to interact with various entity types generically.
3.  **Hazard Trigger (Context/System):** A MonoBehaviour that defines the physical area of the hazard (using a Collider as a trigger). It detects entities entering and staying within its bounds, and uses the associated Hazard Data to periodically apply effects to any `IDamageable` entities.

This structure makes the system highly scalable and maintainable.

---

Here is a complete C# Unity example demonstrating the EnvironmentalHazardSystem design pattern:

First, create separate C# scripts for the enums and the `IDamageable` interface:

**1. `DamageType.cs`**
```csharp
/// <summary>
/// Defines different types of damage, allowing for more complex interactions
/// (e.g., resistances, weaknesses) in your game.
/// </summary>
public enum DamageType
{
    Generic,
    Fire,
    Poison,
    Cold,
    Electric,
    Acid,
    Explosive
}
```

**2. `StatusEffectType.cs`**
```csharp
/// <summary>
/// Defines different types of status effects that hazards might apply.
/// This can be expanded significantly for various game mechanics.
/// </summary>
public enum StatusEffectType
{
    None,
    Burning,
    Poisoned,
    Frozen,
    Electrocuted,
    Slowed,
    Weakened
}
```

**3. `IDamageable.cs`**
```csharp
using UnityEngine; // Required for GameObject parameter

/// <summary>
/// An interface that any object capable of taking damage or being affected by hazards must implement.
/// This allows the hazard system to interact with various game entities (player, enemies, etc.)
/// without knowing their concrete types, adhering to the Dependency Inversion Principle.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the implementing entity.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="type">The type of damage (e.g., Fire, Poison).</param>
    /// <param name="damageSource">The GameObject that caused the damage.</param>
    void TakeDamage(float amount, DamageType type, GameObject damageSource);

    /// <summary>
    /// Applies a status effect to the implementing entity.
    /// </summary>
    /// <param name="effectType">The type of status effect to apply.</param>
    /// <param name="duration">How long the status effect should last.</param>
    /// <param name="source">The GameObject that caused the status effect.</param>
    void ApplyStatusEffect(StatusEffectType effectType, float duration, GameObject source);
}
```

---

Now, for the core components of the Environmental Hazard System:

**4. `HazardProfile.cs` (ScriptableObject - The Hazard Data)**
```csharp
using UnityEngine;

/// <summary>
/// <para>This ScriptableObject acts as the 'Strategy' or 'Data' component of our EnvironmentalHazardSystem.</para>
/// <para>It defines the specific characteristics of a single type of environmental hazard (e.g., "Lava", "Poison Gas").</para>
/// <para>By using ScriptableObjects, we can create many different hazard types through the Unity Editor
/// without writing new MonoBehaviour scripts for each, promoting reusability and data-driven design.</para>
/// </summary>
[CreateAssetMenu(fileName = "NewHazardProfile", menuName = "Environmental Hazards/Hazard Profile", order = 1)]
public class HazardProfile : ScriptableObject
{
    [Header("Hazard Identification")]
    [Tooltip("A unique name for this hazard profile.")]
    public string hazardName = "New Hazard";
    [TextArea(3, 5)]
    [Tooltip("A brief description of this hazard's effects.")]
    public string description = "A description of this hazard.";

    [Header("Damage Settings")]
    [Tooltip("The amount of damage applied per tick.")]
    public float damageAmount = 10f;
    [Tooltip("The type of damage this hazard inflicts (e.g., Fire, Poison).")]
    public DamageType damageType = DamageType.Generic;
    [Tooltip("How often (in seconds) the damage is applied to affected entities.")]
    public float tickInterval = 1.0f;

    [Header("Status Effect Settings (Optional)")]
    [Tooltip("The status effect applied by this hazard. Set to None for no effect.")]
    public StatusEffectType statusEffectType = StatusEffectType.None;
    [Tooltip("The duration (in seconds) of the status effect.")]
    public float statusEffectDuration = 5.0f;

    [Header("Visual & Audio Effects (Optional)")]
    [Tooltip("Prefab to spawn as a visual effect when damage is applied (e.g., particle system).")]
    public GameObject damageEffectPrefab;
    [Tooltip("Sound clip to play when damage is applied.")]
    public AudioClip damageSoundClip;
}
```

**5. `EnvironmentalHazardTrigger.cs` (MonoBehaviour - The Hazard Trigger/System Logic)**
```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary

/// <summary>
/// <para>This MonoBehaviour acts as the 'Context' and 'Trigger' for the EnvironmentalHazardSystem.</para>
/// <para>It detects entities entering its trigger volume and, using a linked HazardProfile,
/// applies periodic damage and/or status effects to any IDamageable entity within its bounds.</para>
/// <para>
/// How the EnvironmentalHazardSystem Pattern Works:
/// 1.  <b>HazardProfile (ScriptableObject):</b> Defines the specific data for a hazard type (e.g., damage, tick rate).
///     This allows for easy creation of various hazards (fire, poison, cold) without code changes.
/// 2.  <b>IDamageable (Interface):</b> Ensures that any entity capable of being affected by a hazard
///     (like a player or enemy) can be interacted with in a generic way, promoting loose coupling.
/// 3.  <b>EnvironmentalHazardTrigger (MonoBehaviour):</b> This component sits in the scene, uses a Collider
///     as a trigger volume, and orchestrates the application of effects defined by its HazardProfile
///     to any IDamageable entities that enter and remain within its bounds. It manages the timing
///     of damage ticks for all affected entities using a per-entity timer.
/// </para>
/// <para>
/// This setup makes it highly modular: you can create new hazard types just by making a new ScriptableObject,
/// and any new entity can become affected by hazards simply by implementing IDamageable.
/// </para>
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures this GameObject always has a Collider
public class EnvironmentalHazardTrigger : MonoBehaviour
{
    [Header("Hazard Configuration")]
    [Tooltip("The HazardProfile ScriptableObject defining the properties of this environmental hazard.")]
    public HazardProfile hazardProfile;

    [Tooltip("Which layers this hazard should affect. Only GameObjects on these layers will be considered.")]
    public LayerMask affectedLayers;

    // A dictionary to keep track of all IDamageable entities currently inside the trigger
    // and the time when they last received damage/effect from this hazard.
    private Dictionary<IDamageable, float> affectedEntities = new Dictionary<IDamageable, float>();

    private Collider hazardCollider; // Reference to our collider

    void Awake()
    {
        hazardCollider = GetComponent<Collider>();
        // Ensure the collider is set as a trigger
        // The RequireComponent ensures a Collider exists, this ensures it's configured correctly.
        if (!hazardCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on '{gameObject.name}' is not set to 'Is Trigger'. Setting it automatically.", this);
            hazardCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Called when another collider enters the trigger volume.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Check if the entering collider is on one of the affected layers.
        if (!IsLayerAffected(other.gameObject.layer))
        {
            return;
        }

        // Try to get the IDamageable component from the entering GameObject.
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && !affectedEntities.ContainsKey(damageable))
        {
            // If it's damageable and not already tracked, add it.
            // Initialize its last damage time to prevent immediate damage on entry if tickInterval > 0.
            // Instead, it will take damage after 'tickInterval' seconds.
            affectedEntities.Add(damageable, Time.time);
            // Debug.Log($"{other.name} entered hazard zone: {hazardProfile.hazardName}", other); // Uncomment for verbose logging
        }
    }

    /// <summary>
    /// Called every fixed framerate update. FixedUpdate is typically used for physics calculations
    /// and ensures consistent timing for damage ticks, regardless of fluctuating frame rates.
    /// </summary>
    void FixedUpdate()
    {
        if (hazardProfile == null)
        {
            Debug.LogError($"HazardProfile is not assigned on '{gameObject.name}'. EnvironmentalHazardTrigger will not function.", this);
            enabled = false; // Disable component to prevent further errors
            return;
        }

        // No need to process if no entities are currently affected.
        if (affectedEntities.Count == 0) return;

        // Create a temporary list to store entities that might need to be removed from the dictionary
        // (e.g., if they were destroyed while inside the trigger).
        List<IDamageable> entitiesToRemove = new List<IDamageable>();

        // Iterate through all currently affected entities.
        foreach (var entry in affectedEntities)
        {
            IDamageable damageable = entry.Key;
            float lastDamageTime = entry.Value;

            // Check if the entity is still valid (hasn't been destroyed).
            // Unity's == null operator correctly handles destroyed UnityEngine.Object instances.
            if (damageable == null || ((Component)damageable).gameObject == null) // Also check if the underlying GameObject is null.
            {
                entitiesToRemove.Add(damageable);
                continue;
            }

            // Check if enough time has passed since the last damage tick for this specific entity.
            if (Time.time >= lastDamageTime + hazardProfile.tickInterval)
            {
                // Apply damage
                damageable.TakeDamage(hazardProfile.damageAmount, hazardProfile.damageType, gameObject);
                
                // Apply status effect if configured and not 'None'
                if (hazardProfile.statusEffectType != StatusEffectType.None)
                {
                    damageable.ApplyStatusEffect(hazardProfile.statusEffectType, hazardProfile.statusEffectDuration, gameObject);
                }

                // Play visual/audio effects at the entity's position
                if (hazardProfile.damageEffectPrefab != null)
                {
                    Instantiate(hazardProfile.damageEffectPrefab, ((Component)damageable).transform.position, Quaternion.identity);
                }
                if (hazardProfile.damageSoundClip != null)
                {
                    // For a real project, consider using an AudioSource pool or a dedicated audio manager for efficiency
                    AudioSource.PlayClipAtPoint(hazardProfile.damageSoundClip, ((Component)damageable).transform.position);
                }

                // Update the last damage time for this entity to reflect when it last received an effect.
                affectedEntities[damageable] = Time.time;
            }
        }

        // Remove any entities that were identified as invalid during the iteration.
        foreach (var entity in entitiesToRemove)
        {
            affectedEntities.Remove(entity);
        }
    }

    /// <summary>
    /// Called when another collider exits the trigger volume.
    /// </summary>
    /// <param name="other">The Collider that exited the trigger.</param>
    void OnTriggerExit(Collider other)
    {
        // Check if the exiting collider is on one of the affected layers.
        if (!IsLayerAffected(other.gameObject.layer))
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && affectedEntities.ContainsKey(damageable))
        {
            // If it's a tracked damageable, remove it from the dictionary.
            affectedEntities.Remove(damageable);
            // Debug.Log($"{other.name} exited hazard zone: {hazardProfile.hazardName}", other); // Uncomment for verbose logging
        }
    }

    /// <summary>
    /// Helper method to check if a GameObject's layer is within the `affectedLayers` mask.
    /// </summary>
    /// <param name="layer">The layer index of the GameObject.</param>
    /// <returns>True if the layer is affected, false otherwise.</returns>
    private bool IsLayerAffected(int layer)
    {
        return ((1 << layer) & affectedLayers) != 0;
    }

    /// <summary>
    /// Visualizes the trigger volume in the editor using Gizmos.
    /// </summary>
    void OnDrawGizmos()
    {
        if (hazardCollider == null)
        {
            hazardCollider = GetComponent<Collider>();
            if (hazardCollider == null) return;
        }

        // Set Gizmo color based on hazard type for better visualization
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // Default orange, semi-transparent
        if (hazardProfile != null)
        {
            switch (hazardProfile.damageType)
            {
                case DamageType.Fire: Gizmos.color = new Color(1f, 0f, 0f, 0.4f); break; // Red for fire
                case DamageType.Poison: Gizmos.color = new Color(0f, 1f, 0f, 0.4f); break; // Green for poison
                case DamageType.Cold: Gizmos.color = new Color(0f, 0f, 1f, 0.4f); break; // Blue for cold
                case DamageType.Electric: Gizmos.color = new Color(0.8f, 0.8f, 0f, 0.4f); break; // Yellow for electric
                case DamageType.Acid: Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.4f); break; // Light green for acid
                case DamageType.Explosive: Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.4f); break; // Purple for explosive
            }
        }
        
        // Draw the collider shape
        if (hazardCollider is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix
        }
        else if (hazardCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
        else if (hazardCollider is CapsuleCollider capsule)
        {
            // Drawing capsules accurately with Gizmos can be complex depending on direction.
            // For simplicity, we'll draw a bounding sphere as a visual cue.
            float maxDim = Mathf.Max(capsule.radius * 2 * transform.lossyScale.x, capsule.height * transform.lossyScale.y);
            Gizmos.DrawSphere(transform.position + capsule.center, maxDim / 2f);
            Gizmos.DrawWireSphere(transform.position + capsule.center, maxDim / 2f);
        }
        // MeshCollider cannot be drawn accurately without the actual mesh, which might not be readily available or scaled.
    }

    /// <summary>
    /// Helper for editor to ensure correct collider setup. Called when script is loaded or a value changes in the Inspector.
    /// </summary>
    void OnValidate()
    {
        hazardCollider = GetComponent<Collider>();
        if (hazardCollider != null && !hazardCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on '{gameObject.name}' must be set to 'Is Trigger' for EnvironmentalHazardTrigger to work. Please enable 'Is Trigger' in the Inspector.", this);
        }
    }
}
```

**6. `HealthComponent.cs` (Example `IDamageable` Implementation)**
```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System.Linq; // For LINQ operations on dictionary

/// <summary>
/// A simple example of a HealthComponent that implements the IDamageable interface.
/// This would typically be attached to player characters, enemies, or any other
/// game object that can be affected by environmental hazards.
/// </summary>
public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Status Effects (Debug)")]
    // A simple dictionary to track active status effects and their remaining duration for demonstration.
    // In a real game, this might be a separate StatusEffectManager class.
    private Dictionary<StatusEffectType, float> activeStatusEffects = new Dictionary<StatusEffectType, float>();

    // Public properties to access health values
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Updates active status effects, ticking down their duration.
    /// </summary>
    void Update()
    {
        // Only process if there are active status effects
        if (activeStatusEffects.Count == 0) return;

        // Use a temporary list for keys to avoid modifying the dictionary during iteration
        List<StatusEffectType> effectsToEnd = new List<StatusEffectType>();

        foreach (var entry in activeStatusEffects)
        {
            StatusEffectType effectType = entry.Key;
            float remainingDuration = entry.Value - Time.deltaTime;
            
            activeStatusEffects[effectType] = remainingDuration; // Update remaining duration

            if (remainingDuration <= 0)
            {
                effectsToEnd.Add(effectType); // Mark for removal
            }
        }

        // Remove expired effects
        foreach (var effectType in effectsToEnd)
        {
            activeStatusEffects.Remove(effectType);
            Debug.Log($"Status Effect '{effectType}' ended on {gameObject.name}.", this);
            // Add specific 'on end' logic here (e.g., restore speed if Slowed)
        }
        
        // Example: Apply damage over time for burning/poisoned effects
        if (activeStatusEffects.ContainsKey(StatusEffectType.Burning))
        {
            TakeDamage(1f * Time.deltaTime, DamageType.Fire, null); // Small DoT from burning
        }
        if (activeStatusEffects.ContainsKey(StatusEffectType.Poisoned))
        {
            TakeDamage(0.5f * Time.deltaTime, DamageType.Poison, null); // Small DoT from poison
        }
    }

    /// <summary>
    /// Implements the <c>TakeDamage</c> method from the <c>IDamageable</c> interface.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="type">The type of damage (e.g., Fire, Poison).</param>
    /// <param name="damageSource">The GameObject that caused the damage.</param>
    public void TakeDamage(float amount, DamageType type, GameObject damageSource)
    {
        if (!IsAlive) return; // Already dead

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount:F1} {type} damage from {damageSource?.name ?? "an unknown source"}. Current Health: {currentHealth:F1}", this);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Implements the <c>ApplyStatusEffect</c> method from the <c>IDamageable</c> interface.
    /// </summary>
    /// <param name="effectType">The type of status effect to apply.</param>
    /// <param name="duration">How long the status effect should last.</param>
    /// <param name="source">The GameObject that caused the status effect.</param>
    public void ApplyStatusEffect(StatusEffectType effectType, float duration, GameObject source)
    {
        if (effectType == StatusEffectType.None || !IsAlive) return;

        // In a real game, you'd have more complex status effect management:
        // - Stacking rules (e.g., duration adds up, intensity increases)
        // - Overwriting rules (e.g., stronger poison replaces weaker)
        // - Visual/audio feedback for status effects (e.g., shader changes, particle effects)
        // - Actual game logic changes (e.g., burning takes damage, slowed reduces speed, frozen stops movement)

        if (activeStatusEffects.ContainsKey(effectType))
        {
            // If effect is already active, refresh duration (or add, or overwrite, based on game design)
            activeStatusEffects[effectType] = duration;
            Debug.Log($"Status Effect '{effectType}' refreshed on {gameObject.name} for {duration:F1}s from {source?.name ?? "an unknown source"}.", this);
        }
        else
        {
            activeStatusEffects.Add(effectType, duration);
            Debug.Log($"Status Effect '{effectType}' applied to {gameObject.name} for {duration:F1}s from {source?.name ?? "an unknown source"}.", this);
            // Add specific 'on start' logic here (e.g., reduce speed if Slowed)
        }
    }

    /// <summary>
    /// Handles the entity's death logic.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!", this);
        // Implement death logic here (e.g., play death animation, disable/destroy object, respawn player, drop loot)
        // For this example, we'll just disable the GameObject.
        gameObject.SetActive(false); 
    }

    /// <summary>
    /// Optional: For visualizing health and status effects in the editor/debug view.
    /// </summary>
    void OnGUI()
    {
        // This is a very basic debug display. For actual in-game UI, use Unity's UI Canvas system.
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screenPos.z < 0) return; // Don't draw if behind camera

        // Background box for health
        GUI.Box(new Rect(screenPos.x - 55, Screen.height - screenPos.y - 35, 110, 25 + activeStatusEffects.Count * 15), "");

        // Health text
        GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 20), $"Health: {currentHealth:F0}/{maxHealth:F0}");
        
        // Status effects text
        float yOffset = 0;
        foreach(var entry in activeStatusEffects.OrderBy(e => e.Key.ToString())) // Order for consistent display
        {
            string effectText = $"{entry.Key} ({entry.Value:F1}s)";
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 15 + yOffset, 100, 20), effectText);
            yOffset += 15;
        }
    }
}
```

---

## **Environmental Hazard System - Example Usage Guide**

This system provides a robust and flexible way to create various environmental hazards in your Unity game.

### **Step 1: Create the C# Scripts**

1.  Create a new C# script named `DamageType.cs` and paste its code into it.
2.  Create a new C# script named `StatusEffectType.cs` and paste its code into it.
3.  Create a new C# script named `IDamageable.cs` and paste its code into it.
4.  Create a new C# script named `HazardProfile.cs` and paste its code into it.
5.  Create a new C# script named `EnvironmentalHazardTrigger.cs` and paste its code.
6.  Create a new C# script named `HealthComponent.cs` and paste its code (this is an example of an `IDamageable` entity).

### **Step 2: Create Hazard Profiles (ScriptableObjects)**

These are your data definitions for different types of hazards.

1.  In your Unity Project window, right-click -> `Create` -> `Environmental Hazards` -> `Hazard Profile`.
2.  Name it something descriptive, like "FireHazardProfile".
3.  Select the newly created "FireHazardProfile" asset. In the Inspector:
    *   Set `Hazard Name`: "Lava Pit"
    *   Set `Description`: "Burns anything that touches it, applying fire damage and the burning status effect."
    *   Set `Damage Amount`: `15`
    *   Set `Damage Type`: `Fire`
    *   Set `Tick Interval`: `0.75` (damage every 0.75 seconds)
    *   Set `Status Effect Type`: `Burning`
    *   Set `Status Effect Duration`: `3`
    *   (Optional) Assign a `Damage Effect Prefab` (e.g., a small fire particle system you might create).
    *   (Optional) Assign a `Damage Sound Clip` (e.g., a "hiss" or "burn" sound).

4.  Repeat to create another profile, e.g., "PoisonGasProfile":
    *   Set `Hazard Name`: "Poison Gas"
    *   Set `Description`: "A cloud of toxic fumes that poisons entities."
    *   Set `Damage Amount`: `5`
    *   Set `Damage Type`: `Poison`
    *   Set `Tick Interval`: `1.5`
    *   Set `Status Effect Type`: `Poisoned`
    *   Set `Status Effect Duration`: `5`
    *   (Optional) Assign relevant visual and sound effects.

### **Step 3: Create a Damageable Entity (e.g., Player or Enemy)**

This is an example of an entity that can be affected by hazards.

1.  Create a 3D object in your scene (e.g., `GameObject` -> `3D Object` -> `Capsule`). Name it "Player".
2.  Add a `Rigidbody` component to the "Player" GameObject (required for `OnTriggerEnter` to work for non-kinematic colliders).
    *   You might want to uncheck `Use Gravity` or set its `Is Kinematic` property to `true` if you plan to move it with `transform.position` or `CharacterController`, otherwise, it will be subject to physics.
3.  Add the `HealthComponent.cs` script to the "Player" GameObject.
4.  **Create a Layer:** Go to `Layers` dropdown (top right of Unity Editor) -> `Add Layer...`. Add a new layer, e.g., "DamageableEntity".
5.  Select your "Player" GameObject and assign it to the "DamageableEntity" layer. Confirm the change for children if prompted.
6.  (Optional but recommended for testing) Add a simple movement script to the "Player" so you can move it into hazards (e.g., `CharacterController` or `Rigidbody` based movement).

### **Step 4: Set up the Environmental Hazard in the Scene**

1.  Create an empty GameObject in your scene (e.g., `GameObject` -> `Create Empty`). Name it "LavaPitHazardZone".
2.  Add the `EnvironmentalHazardTrigger.cs` script to "LavaPitHazardZone".
3.  Add a `Box Collider` component to "LavaPitHazardZone".
    *   Adjust its `Size` to define the hazard area (e.g., `X=10, Y=1, Z=10`).
    *   Adjust its `Center` to position it correctly (e.g., Y-offset if you want it sunken).
    *   **IMPORTANT**: Ensure `Is Trigger` is checked on the Box Collider (the `EnvironmentalHazardTrigger` script will warn you in the console if it's not, and attempt to set it).

4.  Select "LavaPitHazardZone". In the Inspector for the `EnvironmentalHazardTrigger` component:
    *   Drag and drop your "FireHazardProfile" (created in Step 2) into the `Hazard Profile` slot.
    *   Set `Affected Layers`: Click the dropdown and select the "DamageableEntity" layer you created in Step 3.

5.  Repeat this process to create a "PoisonGasZone" GameObject, attaching `EnvironmentalHazardTrigger` and your "PoisonGasProfile", and setting its collider and affected layers.

### **Step 5: Test!**

1.  Run the Unity scene.
2.  Move your "Player" (or any other `IDamageable` entity) into the "LavaPitHazardZone".
    *   You should see its health decrease periodically in the debug console and via the `OnGUI` overlay.
    *   The "Burning" status effect should be applied and its duration refreshed as long as the player is in the zone.
3.  Move your "Player" into the "PoisonGasZone".
    *   It should take poison damage and receive the "Poisoned" status effect.
4.  Move your "Player" out of the hazard zones.
    *   The periodic damage should stop, and any applied status effects will continue to tick down and eventually wear off.

This structure allows you to quickly design and implement various environmental dangers, making your game world more interactive and challenging with minimal code changes for new hazards or affected entities!