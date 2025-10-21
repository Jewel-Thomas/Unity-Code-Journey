// Unity Design Pattern Example: SpellCastingSystem
// This script demonstrates the SpellCastingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust and flexible 'SpellCastingSystem' pattern in Unity, leveraging ScriptableObjects for data-driven spell definitions and the Strategy pattern for spell execution.

This system allows you to:
1.  **Define spells as data assets:** Designers can create new spells in the Unity Editor without writing code.
2.  **Separate spell data from spell logic:** Spell data (cost, cooldown, name) is in `ScriptableObject`s, while casting logic is implemented within each concrete spell class.
3.  **Manage resources and cooldowns:** The `SpellCaster` component handles mana, cooldowns, and the general flow of casting.
4.  **Easily extend with new spell types:** Just create a new `ScriptableObject` class inheriting from `BaseSpellSO` and implement its `Cast` method.

---

### Project Setup Instructions:

To get this working in your Unity project:

1.  **Create C# Scripts:**
    *   Create a new C# script named `BaseSpellSO.cs` and paste its content.
    *   Create a new C# script named `FireballSpellSO.cs` and paste its content.
    *   Create a new C# script named `HealSpellSO.cs` and paste its content.
    *   Create a new C# script named `SpellCaster.cs` and paste its content.
    *   Create a new C# script named `SpellProjectile.cs` and paste its content.
    *   Create a new C# script named `TargetDummy.cs` and paste its content.

2.  **Create Prefabs (Optional but Recommended for Visuals):**
    *   **Spell Projectile:**
        *   Create a 3D Sphere (GameObject -> 3D Object -> Sphere). Rename it `FireballProjectile`.
        *   Add a `Rigidbody` component (uncheck "Use Gravity" and check "Is Kinematic" or "Is Trigger").
        *   Add a `Sphere Collider` component (check "Is Trigger").
        *   Attach the `SpellProjectile.cs` script to it.
        *   Drag this `FireballProjectile` GameObject from your Hierarchy into your Project window to create a prefab. Delete it from the Hierarchy.
    *   **Dummy Target:**
        *   Create a 3D Cube (GameObject -> 3D Object -> Cube). Rename it `TargetDummy`.
        *   Attach the `TargetDummy.cs` script to it.
        *   Drag this `TargetDummy` GameObject from your Hierarchy into your Project window to create a prefab. Delete it from the Hierarchy.
    *   **Simple Visual Effects (Optional):**
        *   You can create simple particle system prefabs for `castingEffectPrefab` (e.g., a burst of particles at the caster's hand), `healEffectPrefab` (e.g., green particles around the caster), and `impactEffectPrefab` (e.g., explosion on hit). For simplicity, you can initially leave these fields empty in the ScriptableObjects.

3.  **Create Spell ScriptableObjects:**
    *   In your Project window, right-click -> Create -> Spells -> Fireball. Name it `FireballSpell`.
        *   Set its `Projectile Prefab` field to your `FireballProjectile` prefab.
        *   Set `Damage Amount` (e.g., 25).
        *   Set `Projectile Speed` (e.g., 10).
        *   Set `Mana Cost` (e.g., 10).
        *   Set `Cooldown` (e.g., 2).
    *   Right-click -> Create -> Spells -> Heal. Name it `HealSpell`.
        *   Set `Heal Amount` (e.g., 20).
        *   Set `Mana Cost` (e.g., 15).
        *   Set `Cooldown` (e.g., 5).

4.  **Set up the Scene:**
    *   Create an empty GameObject (or use your Player character). Rename it `PlayerCaster`.
    *   Add the `SpellCaster.cs` component to `PlayerCaster`.
    *   In the `SpellCaster` component, drag your `FireballSpell` and `HealSpell` ScriptableObjects into the `Known Spells` list.
    *   Place a `TargetDummy` prefab in your scene.
    *   **Ground Layer:** Ensure you have a plane or terrain in your scene and assign it a new `Layer` called "Ground". Update the `SpellCaster.cs`'s `GetMouseWorldPosition` method to use this layer for raycasting: `LayerMask.GetMask("Ground")`.

5.  **Run the Scene:**
    *   Press `1` to cast `FireballSpell`. Click on the ground or `TargetDummy` to aim.
    *   Press `2` to cast `HealSpell` (it will heal the `PlayerCaster`).
    *   Observe the Debug Log for mana, cooldown, damage, and healing messages.

---

### 1. `BaseSpellSO.cs`

This is the abstract base class for all spells. It defines the common properties that every spell will have and an abstract `Cast` method that concrete spells must implement.

```csharp
using UnityEngine;

/// <summary>
/// BaseSpellSO (Scriptable Object)
/// This abstract class defines the common properties and the core 'Cast' behavior for all spells.
/// It acts as the 'Strategy' interface in the Strategy Design Pattern.
/// By using ScriptableObjects, we can create data assets for spells directly in the Unity Editor,
/// separating spell data from runtime logic and promoting reusability.
/// </summary>
public abstract class BaseSpellSO : ScriptableObject
{
    [Header("Spell Core Data")]
    public string spellName = "New Spell";
    [TextArea(3, 5)]
    public string description = "A magical ability.";
    public Sprite icon; // UI icon for the spell

    [Header("Casting Mechanics")]
    public float manaCost = 10f;
    public float cooldown = 5f; // Cooldown duration in seconds
    public GameObject castingEffectPrefab; // Optional: Particle system or sound played at caster during cast

    /// <summary>
    /// The core method that concrete spell types must implement.
    /// This method defines what happens when the spell is cast.
    /// </summary>
    /// <param name="caster">The SpellCaster component that is casting this spell.</param>
    /// <param name="target">The primary GameObject target of the spell (can be null).</param>
    /// <param name="targetPosition">A world position target for spells that need it (e.g., area spells, projectiles).</param>
    public abstract void Cast(SpellCaster caster, GameObject target, Vector3 targetPosition);

    /// <summary>
    /// Provides a default check to determine if a spell can be cast.
    /// Concrete spells can override this for specific additional conditions (e.g., line of sight).
    /// </summary>
    /// <param name="caster">The SpellCaster attempting to cast.</param>
    /// <returns>True if the spell can be cast, false otherwise.</returns>
    public virtual bool CanCast(SpellCaster caster)
    {
        if (caster == null)
        {
            Debug.LogError("Caster is null when checking CanCast for " + spellName);
            return false;
        }

        // Check if caster has enough mana
        if (caster.CurrentMana < manaCost)
        {
            Debug.Log($"Not enough mana for {spellName}. Needed: {manaCost}, Have: {caster.CurrentMana:F1}");
            return false;
        }

        // Check if spell is on cooldown
        if (caster.IsOnCooldown(this))
        {
            Debug.Log($"{spellName} is on cooldown. Time remaining: {caster.GetCooldownRemaining(this):F1}s");
            return false;
        }

        return true;
    }
}
```

### 2. `FireballSpellSO.cs`

A concrete implementation of a damage-dealing spell that fires a projectile.

```csharp
using UnityEngine;

/// <summary>
/// FireballSpellSO (Scriptable Object)
/// A concrete spell implementation that fires a projectile dealing damage.
/// This inherits from BaseSpellSO and provides its specific casting logic.
/// </summary>
[CreateAssetMenu(fileName = "NewFireballSpell", menuName = "Spells/Fireball")]
public class FireballSpellSO : BaseSpellSO
{
    [Header("Fireball Specifics")]
    public GameObject projectilePrefab; // Prefab for the projectile fired by the spell
    public float damageAmount = 25f;
    public float projectileSpeed = 15f;
    public float projectileLaunchOffset = 1f; // Offset from caster to launch projectile

    /// <summary>
    /// Implements the abstract Cast method for a Fireball spell.
    /// This involves instantiating a projectile and initializing it.
    /// </summary>
    public override void Cast(SpellCaster caster, GameObject target, Vector3 targetPosition)
    {
        Debug.Log($"[{caster.name}] casts {spellName} towards {targetPosition}!");

        // Play the generic casting effect if provided
        if (castingEffectPrefab != null)
        {
            Instantiate(castingEffectPrefab, caster.transform.position, Quaternion.identity);
        }

        // Instantiate and initialize the projectile
        if (projectilePrefab != null)
        {
            // Calculate launch position slightly in front of the caster, at an appropriate height
            Vector3 launchPosition = caster.transform.position + caster.transform.forward * projectileLaunchOffset + Vector3.up * 0.5f; 
            
            GameObject projectileGO = Instantiate(projectilePrefab, launchPosition, Quaternion.identity);
            SpellProjectile projectile = projectileGO.GetComponent<SpellProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(caster.gameObject, targetPosition, damageAmount, projectileSpeed);
            }
            else
            {
                Debug.LogWarning($"Projectile prefab '{projectilePrefab.name}' is missing SpellProjectile component.");
            }
        }
        else
        {
            Debug.LogWarning($"FireballSpell '{spellName}' is missing a Projectile Prefab!");
        }
    }
}
```

### 3. `HealSpellSO.cs`

A concrete implementation of a healing spell that targets the caster.

```csharp
using UnityEngine;

/// <summary>
/// HealSpellSO (Scriptable Object)
/// A concrete spell implementation that heals the caster (or a friendly target).
/// This inherits from BaseSpellSO and provides its specific casting logic.
/// </summary>
[CreateAssetMenu(fileName = "NewHealSpell", menuName = "Spells/Heal")]
public class HealSpellSO : BaseSpellSO
{
    [Header("Heal Specifics")]
    public float healAmount = 20f;
    public GameObject healEffectPrefab; // Optional: Particle system or sound played at target during heal

    /// <summary>
    /// Implements the abstract Cast method for a Heal spell.
    /// This involves applying a healing amount to the caster.
    /// </summary>
    public override void Cast(SpellCaster caster, GameObject target, Vector3 targetPosition)
    {
        Debug.Log($"[{caster.name}] casts {spellName}, healing for {healAmount}!");

        // Play the generic casting effect if provided
        if (castingEffectPrefab != null)
        {
            Instantiate(castingEffectPrefab, caster.transform.position, Quaternion.identity);
        }

        // Apply healing. For this example, we directly call a method on the caster.
        // In a full game, this would likely interact with a HealthComponent on the target.
        caster.ApplyHealing(healAmount);

        // Play the healing effect if provided
        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab, caster.transform.position, Quaternion.identity);
        }
    }
}
```

### 4. `SpellCaster.cs`

This is the core `MonoBehaviour` component that you attach to your player or enemy characters. It holds the known spells, manages mana and cooldowns, and orchestrates the casting process.

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary and List

/// <summary>
/// SpellCaster (MonoBehaviour)
/// This component is attached to any GameObject that can cast spells (e.g., Player, Enemy).
/// It acts as the 'Context' in the Strategy Design Pattern, holding references to multiple
/// spell 'strategies' (BaseSpellSO ScriptableObjects) and orchestrating their execution.
/// It manages resources (mana), cooldowns, and handles input for casting.
/// </summary>
public class SpellCaster : MonoBehaviour
{
    [Header("Spell Settings")]
    [Tooltip("The list of spells this caster knows and can cast.")]
    public List<BaseSpellSO> knownSpells = new List<BaseSpellSO>();

    [Header("Resource Management")]
    public float maxMana = 100f;
    [SerializeField] // Show in inspector even if private, useful for debugging
    private float _currentMana;
    public float manaRegenRate = 5f; // Mana regenerated per second

    // Public property to access current mana
    public float CurrentMana => _currentMana;

    // Dictionary to track cooldowns for each spell.
    // Key: The spell ScriptableObject, Value: Remaining cooldown time.
    private Dictionary<BaseSpellSO, float> _cooldownTimers = new Dictionary<BaseSpellSO, float>();

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        _currentMana = maxMana; // Initialize mana
        InitializeCooldowns();
    }

    void Update()
    {
        RegenerateMana();
        UpdateCooldowns();
        HandleInput(); // For player-controlled casting
    }

    // --- Initialization and Management ---

    /// <summary>
    /// Initializes cooldown timers for all known spells.
    /// </summary>
    private void InitializeCooldowns()
    {
        foreach (var spell in knownSpells)
        {
            // Ensure all known spells are in the cooldown dictionary, initially not on cooldown.
            if (!_cooldownTimers.ContainsKey(spell))
            {
                _cooldownTimers[spell] = 0f;
            }
        }
    }

    /// <summary>
    /// Handles mana regeneration over time.
    /// </summary>
    private void RegenerateMana()
    {
        _currentMana = Mathf.Min(_currentMana + manaRegenRate * Time.deltaTime, maxMana);
    }

    /// <summary>
    /// Decrements active cooldowns.
    /// </summary>
    private void UpdateCooldowns()
    {
        // Iterate through a copy of the keys to avoid modifying the collection during iteration
        List<BaseSpellSO> spellsToUpdate = new List<BaseSpellSO>(_cooldownTimers.Keys);
        foreach (var spell in spellsToUpdate)
        {
            if (_cooldownTimers[spell] > 0)
            {
                _cooldownTimers[spell] -= Time.deltaTime;
                if (_cooldownTimers[spell] < 0)
                {
                    _cooldownTimers[spell] = 0; // Ensure cooldown doesn't go negative
                    Debug.Log($"{spell.spellName} is off cooldown!");
                }
            }
        }
    }

    /// <summary>
    /// Example input handling for player to cast spells.
    /// For enemies, this would be replaced by AI logic.
    /// </summary>
    private void HandleInput()
    {
        // Cast the first spell (index 0) with key '1'
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (knownSpells.Count > 0)
            {
                // For projectile spells, we need a target position (e.g., mouse click)
                // For self-target spells (like heal), target and targetPosition might not be used
                TryCastSpell(knownSpells[0], null, GetMouseWorldPosition());
            }
        }

        // Cast the second spell (index 1) with key '2'
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (knownSpells.Count > 1)
            {
                TryCastSpell(knownSpells[1]); // Heal spell might not need a specific target position
            }
        }

        // You can extend this for more spells (e.g., Alpha3, Alpha4, Q, E, R, F keys)
    }

    // --- Public API for Casting Spells ---

    /// <summary>
    /// Attempts to cast a given spell. Performs all necessary checks (mana, cooldown).
    /// </summary>
    /// <param name="spellToCast">The BaseSpellSO asset representing the spell to cast.</param>
    /// <param name="target">Optional: The GameObject target for the spell.</param>
    /// <param name="targetPosition">Optional: The world position target for the spell.</param>
    public void TryCastSpell(BaseSpellSO spellToCast, GameObject target = null, Vector3 targetPosition = default)
    {
        if (spellToCast == null)
        {
            Debug.LogWarning($"[{name}] Attempted to cast a null spell.");
            return;
        }

        // Use the spell's CanCast method for pre-conditions
        if (!spellToCast.CanCast(this))
        {
            return; // CanCast already logs why it failed
        }

        // All checks passed, proceed with casting
        _currentMana -= spellToCast.manaCost; // Deduct mana
        _cooldownTimers[spellToCast] = spellToCast.cooldown; // Set cooldown

        // Call the spell's concrete Cast method (Strategy Pattern in action!)
        // Pass 'this' (the SpellCaster) as context, along with target info.
        spellToCast.Cast(this, target, targetPosition);

        Debug.Log($"[{name}] Successfully cast {spellToCast.spellName}. Mana remaining: {_currentMana:F1}.");
    }

    /// <summary>
    /// Checks if a specific spell is currently on cooldown.
    /// </summary>
    /// <param name="spell">The spell to check.</param>
    /// <returns>True if the spell is on cooldown, false otherwise.</returns>
    public bool IsOnCooldown(BaseSpellSO spell)
    {
        return _cooldownTimers.ContainsKey(spell) && _cooldownTimers[spell] > 0;
    }

    /// <summary>
    /// Gets the remaining cooldown time for a spell.
    /// </summary>
    /// <param name="spell">The spell to check.</param>
    /// <returns>The remaining cooldown time in seconds, or 0 if not on cooldown.</returns>
    public float GetCooldownRemaining(BaseSpellSO spell)
    {
        return _cooldownTimers.ContainsKey(spell) ? _cooldownTimers[spell] : 0f;
    }

    /// <summary>
    /// Applies healing to this caster.
    /// (In a real game, this would typically interface with a HealthComponent).
    /// </summary>
    /// <param name="amount">The amount of healing to apply.</param>
    public void ApplyHealing(float amount)
    {
        // For demonstration purposes, we just log.
        // In a real game, you would do something like:
        // HealthComponent health = GetComponent<HealthComponent>();
        // if (health != null) health.Heal(amount);
        Debug.Log($"[{name}] was healed for {amount} HP. (Health component not implemented here.)");
    }

    // --- Utility Methods ---

    /// <summary>
    /// Helper method to get a world position from the mouse cursor.
    /// Useful for ground-targeted or single-target spells.
    /// Assumes a 'Ground' layer exists for raycasting.
    /// </summary>
    /// <returns>The world position hit by the raycast, or a point in front of the caster if no hit.</returns>
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // Adjust the layermask to match your scene's ground layer
        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Ground"))) 
        {
            return hit.point;
        }
        // Fallback: if no ground hit, cast in front of the caster
        Debug.LogWarning("No 'Ground' layer detected or raycast missed. Defaulting target position in front of caster.");
        return transform.position + transform.forward * 10f;
    }
}
```

### 5. `SpellProjectile.cs`

A simple script for a projectile GameObject. This will be attached to your `FireballProjectile` prefab.

```csharp
using UnityEngine;

/// <summary>
/// SpellProjectile (MonoBehaviour)
/// This script handles the movement and impact logic for a projectile spell.
/// It's a separate component because projectile behavior is distinct from the casting process itself.
/// </summary>
[RequireComponent(typeof(Collider))] // Projectiles need a collider to detect hits
public class SpellProjectile : MonoBehaviour
{
    private GameObject _caster;         // To prevent hitting the caster immediately
    private Vector3 _targetPosition;    // The world position the projectile is aiming for
    private float _damage;              // Damage to apply on impact
    private float _speed;               // Speed of the projectile

    [Header("Visuals & Effects")]
    public GameObject impactEffectPrefab; // Optional: Particle system for impact

    /// <summary>
    /// Initializes the projectile with its parameters.
    /// </summary>
    /// <param name="caster">The GameObject that cast this projectile.</param>
    /// <param name="targetPosition">The world position the projectile should travel towards.</param>
    /// <param name="damage">The damage this projectile will deal.</param>
    /// <param name="speed">The speed at which this projectile travels.</param>
    public void Initialize(GameObject caster, Vector3 targetPosition, float damage, float speed)
    {
        _caster = caster;
        _targetPosition = targetPosition;
        _damage = damage;
        _speed = speed;

        // Make the projectile face its target for visual consistency
        transform.LookAt(_targetPosition);
    }

    void Update()
    {
        // Move the projectile towards its target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);

        // If the projectile is very close to its target, consider it "hit"
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            ApplyImpact();
            Destroy(gameObject); // Destroy itself after reaching target
        }
    }

    /// <summary>
    /// Handles collision detection for the projectile.
    /// </summary>
    /// <param name="other">The collider of the object it hit.</param>
    void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with the caster or other projectiles
        if (other.gameObject == _caster || other.GetComponent<SpellProjectile>() != null)
        {
            return;
        }

        // Try to deal damage to a TargetDummy (or any IDamageable component)
        TargetDummy dummy = other.GetComponent<TargetDummy>();
        if (dummy != null)
        {
            dummy.TakeDamage(_damage);
            ApplyImpact();
            Destroy(gameObject); // Destroy itself after hitting a damageable target
            return;
        }

        // If it hits something else (e.g., environment), just destroy it
        // You might want a different impact effect for environment hits
        ApplyImpact();
        Destroy(gameObject);
    }

    /// <summary>
    /// Instantiates impact effects or plays sounds.
    /// </summary>
    private void ApplyImpact()
    {
        Debug.Log($"Projectile impact at {transform.position}!");
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
        // Example: Play sound: AudioSource.PlayClipAtPoint(impactSound, transform.position);
    }
}
```

### 6. `TargetDummy.cs`

A simple component to represent a target that can take damage and be healed. Attach this to your `TargetDummy` prefab.

```csharp
using UnityEngine;

/// <summary>
/// TargetDummy (MonoBehaviour)
/// A simple component for a target that can take damage and receive healing.
/// This is used to demonstrate spell effects like damage and healing.
/// </summary>
public class TargetDummy : MonoBehaviour
{
    [Header("Dummy Stats")]
    public float maxHealth = 100f;
    [SerializeField] // Show in inspector even if private
    private float _currentHealth;

    public float CurrentHealth => _currentHealth;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the dummy.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        Debug.Log($"[{name}] took {amount} damage. Health: {_currentHealth:F1}/{maxHealth:F1}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Debug.Log($"[{name}] has been defeated!");
            // In a real game, you might trigger an animation, drop loot, etc.
            Destroy(gameObject, 0.5f); // Destroy after a short delay
        }
    }

    /// <summary>
    /// Applies healing to the dummy.
    /// </summary>
    /// <param name="amount">The amount of healing to apply.</param>
    public void ApplyHealing(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        Debug.Log($"[{name}] healed for {amount}. Health: {_currentHealth:F1}/{maxHealth:F1}");
    }
}
```