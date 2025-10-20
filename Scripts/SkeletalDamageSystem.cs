// Unity Design Pattern Example: SkeletalDamageSystem
// This script demonstrates the SkeletalDamageSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Skeletal Damage System** design pattern in Unity. This pattern allows for detailed damage tracking on individual parts (bones, limbs) of a character, rather than just a single global health bar. The character's overall state (alive, dead, crippled) is then derived from the health and status of its constituent parts.

### Why use the Skeletal Damage System?

1.  **Granular Damage:** Apply damage to specific body parts (e.g., headshot, leg shot, arm shot).
2.  **Location-Based Effects:** Implement specific consequences for damaged parts (e.g., a broken leg slows movement, a damaged arm reduces attack power).
3.  **Critical Hits/Weak Points:** Designate certain parts as critical for instant death or bonus damage.
4.  **Realistic Feedback:** Provide more immersive and detailed feedback to players regarding damage.
5.  **Modular Design:** Each part manages its own health, making the system easier to extend and maintain.

---

## Complete C# Unity Example: SkeletalDamageSystem

This solution consists of three main parts:
1.  **`SkeletalHealthSystem.cs`**: The main controller that manages all damageable parts and determines the character's overall health and status.
2.  **`DamageablePart.cs`**: A component attached to individual bone/limb GameObjects, representing a specific part with its own health.
3.  **`DamageDealer.cs` (Example)**: A simple script to simulate applying damage via raycasting (e.g., a weapon).
4.  **Helper Enums/Structs**: For defining damage types and passing damage information.

---

### 1. `DamageTypes.cs` (Helper Enum & Structs)

This file defines the types of damage and a structure to encapsulate damage information.

```csharp
using UnityEngine; // Needed for GameObject reference in DamageInfo
using System; // Needed for Action

// --- ENUMS AND STRUCTS ---

/// <summary>
/// Defines various types of damage that can be applied.
/// Can be expanded to include specific elemental, status, etc. damage types.
/// </summary>
public enum DamageType
{
    Generic,
    Blunt,
    Piercing,
    Slashing,
    Fire,
    Frost,
    Electric,
    Explosion
}

/// <summary>
/// A struct to hold comprehensive information about a damage event.
/// This allows for passing rich data to damageable systems.
/// </summary>
public struct DamageInfo
{
    public float Amount;             // The base amount of damage.
    public DamageType Type;          // The type of damage (e.g., Fire, Piercing).
    public GameObject DamageSource;  // The GameObject that caused the damage (e.g., projectile, enemy weapon).
    public Vector3 HitPoint;         // The world position where the damage hit.
    public Vector3 HitDirection;     // The direction from which the damage came.

    public DamageInfo(float amount, DamageType type, GameObject source, Vector3 hitPoint, Vector3 hitDirection)
    {
        Amount = amount;
        Type = type;
        DamageSource = source;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
    }
}

// --- INTERFACES (Optional but good practice) ---

/// <summary>
/// Interface for any object that can receive damage.
/// Useful for generic damage application systems that don't need to know the specifics
/// of a SkeletalDamageSystem, just that something is damageable.
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}

/// <summary>
/// Interface for a skeletal health system.
/// Could be used if multiple types of skeletal systems exist (e.g., for different creature types).
/// </summary>
public interface ISkeletalHealthSystem : IDamageable
{
    bool IsAlive { get; }
    float CurrentTotalHealth { get; }
    float MaxTotalHealth { get; }
    event Action OnCharacterDied;
    event Action<DamageablePart, DamageInfo> OnPartDamaged;
    event Action<DamageablePart> OnPartDestroyed;
    // Potentially other events like OnCharacterCrippled
}

/// <summary>
/// Interface for an individual damageable part within a skeletal system.
/// </summary>
public interface IDamageablePart
{
    string PartName { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDestroyed { get; }
    void ApplyDamage(DamageInfo damageInfo);
    event Action<DamageablePart, DamageInfo> OnHealthChanged;
    event Action<DamageablePart> OnDestroyed;
}

```

### 2. `DamageablePart.cs`

This script represents an individual damageable segment of a character's "skeleton." Attach this to each GameObject that should act as a distinct damage zone (e.g., Head, Torso, LeftArm, RightLeg).

```csharp
using UnityEngine;
using System;

/// <summary>
/// Represents an individual damageable part of a character's 'skeleton'.
/// Each part has its own health, max health, and properties like damage multipliers
/// and criticality.
/// This script should be attached to a GameObject that represents a specific
/// body part (e.g., 'Head', 'Torso', 'LeftArm'), preferably with a Collider.
/// </summary>
public class DamageablePart : MonoBehaviour, IDamageablePart
{
    [Header("Part Configuration")]
    [Tooltip("The maximum health of this specific body part.")]
    [SerializeField]
    private float _maxHealth = 100f;

    [Tooltip("Multiplier for incoming damage. E.g., 2.0 for headshot, 0.5 for armored leg.")]
    [SerializeField]
    private float _damageMultiplier = 1.0f;

    [Tooltip("If true, destroying this part will instantly kill the character (e.g., Head, Torso).")]
    [SerializeField]
    private bool _isCriticalPart = false;

    // --- Private Internal State ---
    private float _currentHealth;
    private SkeletalHealthSystem _parentSystem; // Reference to the main health system
    private bool _isDestroyed = false;

    // --- Public Properties (Read-only) ---
    public string PartName => gameObject.name; // Uses the GameObject's name as the part identifier
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDestroyed => _isDestroyed;
    public bool IsCriticalPart => _isCriticalPart;
    public float DamageMultiplier => _damageMultiplier;

    // --- Events ---
    /// <summary>
    /// Event fired when this part's health changes.
    /// Passes the part itself and the DamageInfo that caused the change.
    /// </summary>
    public event Action<DamageablePart, DamageInfo> OnHealthChanged;

    /// <summary>
    /// Event fired when this part's health drops to 0 or below, marking it as 'destroyed'.
    /// </summary>
    public event Action<DamageablePart> OnDestroyed;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        _currentHealth = _maxHealth;
        // Optionally, find the parent system here, but it's more robust
        // for the SkeletalHealthSystem to find and register its parts.
    }

    /// <summary>
    /// Used in the editor to quickly set the GameObject's name as the PartName.
    /// This is a convenience for initial setup.
    /// </summary>
    private void OnValidate()
    {
        // Ensure health is not negative
        _currentHealth = Mathf.Max(0, _currentHealth);
        _maxHealth = Mathf.Max(1, _maxHealth); // Max health must be at least 1
    }

    // --- Public Methods ---

    /// <summary>
    /// Applies damage to this specific body part.
    /// Calculates final damage based on the part's damage multiplier.
    /// </summary>
    /// <param name="damageInfo">Information about the damage event.</param>
    public void ApplyDamage(DamageInfo damageInfo)
    {
        if (_isDestroyed)
        {
            // Part is already destroyed, cannot take more damage to its health.
            // Could still trigger visual effects or pass to parent for global effects.
            return;
        }

        // Apply damage multiplier
        float finalDamage = damageInfo.Amount * _damageMultiplier;
        _currentHealth -= finalDamage;

        // Ensure health doesn't go below zero
        _currentHealth = Mathf.Max(0, _currentHealth);

        // Notify subscribers that health has changed
        OnHealthChanged?.Invoke(this, damageInfo);

        // Check if the part has been destroyed
        if (_currentHealth <= 0 && !_isDestroyed)
        {
            _isDestroyed = true;
            OnDestroyed?.Invoke(this);
            Debug.Log($"<color=red>{PartName}</color> on {transform.root.name} has been <color=red>destroyed!</color>");

            // Notify parent system if it exists
            if (_parentSystem != null)
            {
                _parentSystem.NotifyPartDestroyed(this);
            }
        }
        else if (_currentHealth > 0)
        {
            Debug.Log($"{PartName} on {transform.root.name} took {finalDamage:F1} damage. Health: {_currentHealth:F1}/{_maxHealth:F1}");
        }
    }

    /// <summary>
    /// Heals this specific body part.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(float amount)
    {
        if (_isDestroyed && amount > 0)
        {
            // If the part was destroyed, healing it can potentially 'repair' it.
            // This might trigger an 'OnRepaired' event if implemented.
            _isDestroyed = false; // Part is no longer destroyed
            Debug.Log($"<color=green>{PartName}</color> on {transform.root.name} has been <color=green>repaired!</color>");
        }

        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth); // Cap at max health

        // Notify health changed (even if just healing)
        // For simplicity, we create a generic DamageInfo for healing,
        // you might want a separate 'HealInfo' struct for more detail.
        OnHealthChanged?.Invoke(this, new DamageInfo(-amount, DamageType.Generic, null, Vector3.zero, Vector3.zero));

        Debug.Log($"{PartName} on {transform.root.name} healed {amount:F1}. Health: {_currentHealth:F1}/{_maxHealth:F1}");

        if (_parentSystem != null)
        {
            _parentSystem.NotifyPartHealed(this);
        }
    }

    /// <summary>
    /// Resets the health of this part to its maximum.
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        _isDestroyed = false;
        OnHealthChanged?.Invoke(this, new DamageInfo(-_maxHealth, DamageType.Generic, null, Vector3.zero, Vector3.zero)); // Simulate full heal event
        Debug.Log($"{PartName} on {transform.root.name} health reset to {_maxHealth:F1}.");
    }

    /// <summary>
    /// Sets the parent SkeletalHealthSystem for this part. Called by the SkeletalHealthSystem itself.
    /// </summary>
    /// <param name="parentSystem">The SkeletalHealthSystem managing this part.</param>
    public void SetParentSystem(SkeletalHealthSystem parentSystem)
    {
        _parentSystem = parentSystem;
    }
}
```

### 3. `SkeletalHealthSystem.cs`

This script is the main controller that manages all `DamageablePart` components on a character. Attach this to the root GameObject of your character's hierarchy.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The main controller for the Skeletal Damage System.
/// This script manages all individual DamageableParts on a character,
/// calculates overall health, and determines the character's general status (alive/dead).
/// It also provides a central point for applying damage to the character's parts.
/// </summary>
public class SkeletalHealthSystem : MonoBehaviour, ISkeletalHealthSystem
{
    [Header("Skeletal Health System Configuration")]
    [Tooltip("If true, the character dies instantly if any critical part is destroyed.")]
    [SerializeField]
    private bool _dieOnCriticalPartDestroyed = true;

    [Tooltip("If true, the character dies if all non-critical parts are destroyed (or if total health reaches zero).")]
    [SerializeField]
    private bool _dieOnAllNonCriticalPartsDestroyed = true;

    [Tooltip("If true, the character dies if their total current health (sum of all parts) drops to 0.")]
    [SerializeField]
    private bool _dieOnZeroTotalHealth = true;

    // Dictionary to hold all damageable parts, accessible by their GameObject name.
    private Dictionary<string, DamageablePart> _bodyParts = new Dictionary<string, DamageablePart>();

    // --- Private Internal State ---
    private bool _isAlive = true;
    private float _currentTotalHealth;
    private float _maxTotalHealth;

    // --- Public Properties (Read-only) ---
    public bool IsAlive => _isAlive;
    public float CurrentTotalHealth => _currentTotalHealth;
    public float MaxTotalHealth => _maxTotalHealth;
    public IReadOnlyDictionary<string, DamageablePart> BodyParts => _bodyParts; // Expose parts read-only

    // --- Events ---
    /// <summary>
    /// Event fired when the character's overall health changes.
    /// </summary>
    public event Action<float, float> OnOverallHealthChanged; // Current, Max

    /// <summary>
    /// Event fired when the character's overall status changes to 'dead'.
    /// </summary>
    public event Action OnCharacterDied;

    /// <summary>
    /// Event fired when any individual part takes damage.
    /// Passes the damaged part and the DamageInfo.
    /// </summary>
    public event Action<DamageablePart, DamageInfo> OnPartDamaged;

    /// <summary>
    /// Event fired when any individual part is destroyed (health <= 0).
    /// </summary>
    public event Action<DamageablePart> OnPartDestroyed;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        InitializeSkeletalSystem();
    }

    /// <summary>
    /// Initializes the skeletal system by finding all DamageablePart components
    /// in the GameObject hierarchy and subscribing to their events.
    /// </summary>
    private void InitializeSkeletalSystem()
    {
        // Clear previous state if Awake is called multiple times (e.g., in editor)
        _bodyParts.Clear();
        _maxTotalHealth = 0;
        _currentTotalHealth = 0;

        // Find all DamageablePart components in children (and self)
        DamageablePart[] parts = GetComponentsInChildren<DamageablePart>(true);

        if (parts.Length == 0)
        {
            Debug.LogWarning($"SkeletalHealthSystem on {gameObject.name} found no DamageablePart components in its hierarchy. Is this intended?");
            return;
        }

        foreach (var part in parts)
        {
            if (_bodyParts.ContainsKey(part.PartName))
            {
                Debug.LogWarning($"Duplicate part name '{part.PartName}' found under {gameObject.name}. " +
                                 $"Each DamageablePart GameObject must have a unique name within the hierarchy. " +
                                 $"Only the first instance will be registered.");
                continue;
            }

            _bodyParts.Add(part.PartName, part);
            _maxTotalHealth += part.MaxHealth;
            _currentTotalHealth += part.CurrentHealth;

            // Subscribe to the part's health change and destroyed events
            part.OnHealthChanged += HandlePartHealthChanged;
            part.OnDestroyed += HandlePartDestroyed;
            part.SetParentSystem(this); // Let the part know who its parent system is
        }

        _isAlive = true; // Start as alive
        RecalculateOverallHealth(); // Ensure initial overall health is correct
        Debug.Log($"SkeletalHealthSystem for {gameObject.name} initialized with {_bodyParts.Count} parts. Total Health: {_currentTotalHealth}/{_maxTotalHealth}");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks, especially important
        // if this system or its parts can be destroyed/re-instantiated frequently.
        foreach (var part in _bodyParts.Values)
        {
            if (part != null) // Check if part still exists
            {
                part.OnHealthChanged -= HandlePartHealthChanged;
                part.OnDestroyed -= HandlePartDestroyed;
            }
        }
    }

    // --- Event Handlers from DamageableParts ---

    /// <summary>
    /// Handles health changes from any individual DamageablePart.
    /// </summary>
    private void HandlePartHealthChanged(DamageablePart part, DamageInfo damageInfo)
    {
        // Only trigger overall system events if character is still alive
        if (IsAlive)
        {
            OnPartDamaged?.Invoke(part, damageInfo);
            RecalculateOverallHealth();
        }
    }

    /// <summary>
    /// Handles a part being destroyed (health reaching zero).
    /// </summary>
    private void HandlePartDestroyed(DamageablePart part)
    {
        // Only trigger overall system events if character is still alive
        if (IsAlive)
        {
            OnPartDestroyed?.Invoke(part);
            RecalculateOverallHealth();
        }
    }

    /// <summary>
    /// This method is called by a DamageablePart when its health changes and it has a reference to this system.
    /// It ensures the overall system state is updated correctly.
    /// </summary>
    public void NotifyPartDestroyed(DamageablePart part)
    {
        // This is primarily to ensure the overall system reacts.
        // The event `OnPartDestroyed` will already be invoked by `HandlePartDestroyed`.
        // This method can be used for more specific logic if needed, but often `RecalculateOverallHealth` is sufficient.
        RecalculateOverallHealth();
    }

    /// <summary>
    /// Notifies the system that a part has been healed (potentially from a destroyed state).
    /// </summary>
    public void NotifyPartHealed(DamageablePart part)
    {
        RecalculateOverallHealth();
    }


    // --- Core Logic: Overall Health and Status Management ---

    /// <summary>
    /// Recalculates the total health of the character and updates its 'alive' status.
    /// This should be called whenever a part's health changes.
    /// </summary>
    private void RecalculateOverallHealth()
    {
        float newTotalHealth = 0;
        bool anyCriticalPartDestroyed = false;
        int destroyedNonCriticalParts = 0;
        int totalNonCriticalParts = 0;

        foreach (var part in _bodyParts.Values)
        {
            newTotalHealth += part.CurrentHealth;

            if (part.IsDestroyed)
            {
                if (part.IsCriticalPart)
                {
                    anyCriticalPartDestroyed = true;
                }
                else
                {
                    destroyedNonCriticalParts++;
                }
            }

            if (!part.IsCriticalPart)
            {
                totalNonCriticalParts++;
            }
        }

        _currentTotalHealth = newTotalHealth;
        OnOverallHealthChanged?.Invoke(_currentTotalHealth, _maxTotalHealth);

        // Check for death conditions only if currently alive
        if (_isAlive)
        {
            bool shouldDie = false;

            // Condition 1: Critical part destroyed
            if (_dieOnCriticalPartDestroyed && anyCriticalPartDestroyed)
            {
                shouldDie = true;
                Debug.Log($"Character died: Critical part destroyed!");
            }
            // Condition 2: All non-critical parts destroyed
            else if (_dieOnAllNonCriticalPartsDestroyed && totalNonCriticalParts > 0 && destroyedNonCriticalParts >= totalNonCriticalParts)
            {
                shouldDie = true;
                Debug.Log($"Character died: All non-critical parts destroyed!");
            }
            // Condition 3: Total health reached zero
            else if (_dieOnZeroTotalHealth && _currentTotalHealth <= 0)
            {
                shouldDie = true;
                Debug.Log($"Character died: Total health reached zero!");
            }

            if (shouldDie)
            {
                Die();
            }
            else if (_currentTotalHealth <= 0 && !_dieOnZeroTotalHealth && _dieOnCriticalPartDestroyed && !anyCriticalPartDestroyed)
            {
                // Edge case: All parts might be at 0, but no critical part destroyed and not configured to die on total health 0.
                // In such a scenario, the character might be 'crippled' but not dead.
                // This state can be expanded upon with an 'OnCharacterCrippled' event.
                Debug.Log($"Character is heavily damaged (0 total health) but not dead by configured rules.");
            }
        }
    }

    /// <summary>
    /// Marks the character as dead and triggers the OnCharacterDied event.
    /// </summary>
    private void Die()
    {
        if (!_isAlive) return; // Already dead

        _isAlive = false;
        Debug.Log($"<color=red>{gameObject.name} has DIED!</color>");
        OnCharacterDied?.Invoke();

        // Optionally, disable further damage or component activity here
        // For example: GetComponent<Collider>().enabled = false;
        // All DamageableParts should also stop processing damage as the parent system is dead.
    }

    /// <summary>
    /// Resets the health of all body parts to max and revives the character.
    /// </summary>
    public void ResetCharacterHealthAndRevive()
    {
        foreach (var part in _bodyParts.Values)
        {
            part.ResetHealth();
        }
        _isAlive = true;
        RecalculateOverallHealth();
        Debug.Log($"<color=green>{gameObject.name} has been REVIVED!</color>");
    }

    // --- Public API for Applying Damage ---

    /// <summary>
    /// Applies damage to the specific part hit by a raycast.
    /// This is the primary method for external scripts (e.g., weapons) to deal damage.
    /// </summary>
    /// <param name="hit">The RaycastHit information from the collision.</param>
    /// <param name="rawDamageAmount">The base amount of damage to apply before part multipliers.</param>
    /// <param name="damageType">The type of damage being applied.</param>
    /// <param name="damageSource">The GameObject that caused the damage.</param>
    public void ApplyDamage(RaycastHit hit, float rawDamageAmount, DamageType damageType, GameObject damageSource)
    {
        if (!_isAlive)
        {
            Debug.Log($"{gameObject.name} is already dead, no further damage applied.");
            return;
        }

        DamageablePart hitPart = hit.collider.GetComponent<DamageablePart>();
        if (hitPart != null && _bodyParts.ContainsValue(hitPart)) // Ensure it's a part of *this* system
        {
            DamageInfo damageInfo = new DamageInfo(rawDamageAmount, damageType, damageSource, hit.point, hit.normal);
            hitPart.ApplyDamage(damageInfo);
        }
        else
        {
            Debug.LogWarning($"Raycast hit {hit.collider.name} which is not a registered DamageablePart for {gameObject.name}. " +
                             $"Damage of {rawDamageAmount} applied to unknown part.");
            // Optionally, implement fallback damage to the root system or log error.
        }
    }

    /// <summary>
    /// Applies damage to a specific part by its name.
    /// Useful for status effects or specific area-of-effect damage that targets a known part.
    /// </summary>
    /// <param name="partName">The name of the DamageablePart GameObject.</param>
    /// <param name="rawDamageAmount">The base amount of damage.</param>
    /// <param name="damageType">The type of damage.</param>
    /// <param name="damageSource">The GameObject that caused the damage.</param>
    public void ApplyDamageToPart(string partName, float rawDamageAmount, DamageType damageType, GameObject damageSource = null)
    {
        if (!_isAlive)
        {
            Debug.Log($"{gameObject.name} is already dead, no further damage applied.");
            return;
        }

        if (_bodyParts.TryGetValue(partName, out DamageablePart part))
        {
            DamageInfo damageInfo = new DamageInfo(rawDamageAmount, damageType, damageSource, part.transform.position, Vector3.zero); // Position might be center of part
            part.ApplyDamage(damageInfo);
        }
        else
        {
            Debug.LogWarning($"Attempted to damage unknown part: {partName} on {gameObject.name}");
        }
    }

    /// <summary>
    /// Applies damage directly to a DamageablePart instance.
    /// </summary>
    public void ApplyDamage(DamageablePart part, float rawDamageAmount, DamageType damageType, GameObject damageSource = null)
    {
        if (!_isAlive)
        {
            Debug.Log($"{gameObject.name} is already dead, no further damage applied.");
            return;
        }

        if (_bodyParts.ContainsValue(part))
        {
            DamageInfo damageInfo = new DamageInfo(rawDamageAmount, damageType, damageSource, part.transform.position, Vector3.zero);
            part.ApplyDamage(damageInfo);
        }
        else
        {
            Debug.LogWarning($"Attempted to damage an unregistered part instance for {gameObject.name}.");
        }
    }

    /// <summary>
    /// Directly implements IDamageable's TakeDamage. This usually means damage to the "root" of the character
    /// without a specific part. Could distribute or apply to a default part (e.g., Torso).
    /// For this skeletal system, we'll try to apply to a 'Torso' if it exists, otherwise log.
    /// </summary>
    /// <param name="damageInfo">The full damage information.</param>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (!_isAlive)
        {
            Debug.Log($"{gameObject.name} is already dead, no further damage applied.");
            return;
        }

        // A generic TakeDamage often means damage to the whole entity, not a specific part.
        // For a skeletal system, we should decide how to distribute this.
        // Option 1: Apply to a default part (e.g., "Torso").
        if (_bodyParts.TryGetValue("Torso", out DamageablePart torsoPart))
        {
            Debug.Log($"Applying generic damage {damageInfo.Amount} to Torso.");
            torsoPart.ApplyDamage(damageInfo);
        }
        // Option 2: Distribute damage among all parts (e.g., AoE damage).
        // else if (_bodyParts.Count > 0)
        // {
        //     float distributedDamage = damageInfo.Amount / _bodyParts.Count;
        //     foreach (var part in _bodyParts.Values)
        //     {
        //         part.ApplyDamage(new DamageInfo(distributedDamage, damageInfo.Type, damageInfo.DamageSource, damageInfo.HitPoint, damageInfo.HitDirection));
        //     }
        //     Debug.Log($"Distributing generic damage {damageInfo.Amount} among {_bodyParts.Count} parts.");
        // }
        // Option 3: Simply log a warning or error if no specific part can be determined.
        else
        {
            Debug.LogWarning($"{gameObject.name} received generic damage but no 'Torso' part was found to apply it to, and no distribution logic is defined. Damage: {damageInfo.Amount}");
            // As a fallback, we could still update total health directly or kill the character if this is fatal.
            // For now, we'll just log.
        }
    }
}
```

### 4. `DamageDealer.cs` (Example Usage)

This script simulates a "weapon" or "projectile" that can deal damage. Attach this to a GameObject that should perform raycasts (e.g., a player character's camera, a gun barrel).

```csharp
using UnityEngine;

/// <summary>
/// Example script demonstrating how to apply damage to a SkeletalDamageSystem.
/// This simulates a weapon that fires a raycast and deals damage to the hit body part.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("The amount of raw damage to deal per hit.")]
    [SerializeField]
    private float _damageAmount = 30f;

    [Tooltip("The type of damage this dealer applies.")]
    [SerializeField]
    private DamageType _damageType = DamageType.Piercing;

    [Tooltip("The range of the raycast.")]
    [SerializeField]
    private float _attackRange = 100f;

    [Tooltip("Layers that the raycast should hit.")]
    [SerializeField]
    private LayerMask _hitLayers;

    [Header("Input Settings")]
    [Tooltip("The button used to trigger damage.")]
    [SerializeField]
    private KeyCode _fireKey = KeyCode.Mouse0;

    [Header("Visual Feedback")]
    [Tooltip("Reference to a LineRenderer for visualizing the raycast.")]
    [SerializeField]
    private LineRenderer _lineRenderer;

    [Tooltip("Duration for which the LineRenderer should be visible.")]
    [SerializeField]
    private float _lineDisplayDuration = 0.1f;

    private float _lineDisplayTimer;

    private void Start()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        // Simulate firing a weapon
        if (Input.GetKeyDown(_fireKey))
        {
            FireWeapon();
        }

        // Update LineRenderer visibility
        if (_lineDisplayTimer > 0)
        {
            _lineDisplayTimer -= Time.deltaTime;
            if (_lineDisplayTimer <= 0 && _lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// Fires a raycast from the current position and applies damage to any
    /// DamageablePart it hits.
    /// </summary>
    private void FireWeapon()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Vector3 hitPoint = transform.position + transform.forward * _attackRange; // Default if nothing is hit

        if (Physics.Raycast(ray, out hit, _attackRange, _hitLayers))
        {
            hitPoint = hit.point;
            // Attempt to get the SkeletalHealthSystem from the hit object's root.
            // This is robust as the DamageablePart might be deep in the hierarchy.
            SkeletalHealthSystem skeletalSystem = hit.collider.GetComponentInParent<SkeletalHealthSystem>();

            if (skeletalSystem != null)
            {
                // Use the SkeletalHealthSystem's public API to apply damage
                // The system will figure out which specific part was hit.
                skeletalSystem.ApplyDamage(hit, _damageAmount, _damageType, gameObject);
            }
            else
            {
                Debug.Log($"Hit {hit.collider.name} but no SkeletalHealthSystem found on its root or parent hierarchy.");
                // Fallback for non-skeletal damageable objects (e.g., environmental objects)
                IDamageable simpleDamageable = hit.collider.GetComponent<IDamageable>();
                if (simpleDamageable != null)
                {
                    simpleDamageable.TakeDamage(new DamageInfo(_damageAmount, _damageType, gameObject, hit.point, hit.normal));
                }
            }
        }
        else
        {
            Debug.Log("Missed!");
        }

        // Visual feedback for the raycast
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, hitPoint);
            _lineDisplayTimer = _lineDisplayDuration;
        }
    }

    // Optional: Draw the ray in the editor for debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * _attackRange);
    }
}
```

---

### How to Implement in Unity:

1.  **Create C# Scripts:**
    *   Create a C# script named `DamageTypes.cs` and paste the content from section 1.
    *   Create a C# script named `DamageablePart.cs` and paste the content from section 2.
    *   Create a C# script named `SkeletalHealthSystem.cs` and paste the content from section 3.
    *   Create a C# script named `DamageDealer.cs` and paste the content from section 4.

2.  **Prepare your Character Model:**
    *   Import a 3D character model (e.g., a humanoid, creature).
    *   Ensure its hierarchy has distinct GameObjects for different body parts (e.g., `CharacterRoot` -> `Torso` -> `Head`, `LeftArm`, `RightArm`, etc.). These GameObjects should usually correspond to the bone hierarchy.
    *   **Crucial:** Each of these body part GameObjects must have a `Collider` component (e.g., `BoxCollider`, `CapsuleCollider`, `SphereCollider`, or `MeshCollider` if it's a very detailed part). These colliders will be used by the `DamageDealer` to detect hits. Make sure colliders are appropriately sized and positioned. Set them as `Is Trigger = false`.

3.  **Attach Components:**
    *   **`SkeletalHealthSystem`**: Attach this script to the **root** GameObject of your character model (the one that contains the entire hierarchy).
    *   **`DamageablePart`**: For *each* significant body part (Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg, etc.) within your character's hierarchy, attach a `DamageablePart.cs` script to its corresponding GameObject.
        *   **Configure each `DamageablePart`**:
            *   Adjust `Max Health` for that part.
            *   Set `Damage Multiplier` (e.g., 2.0 for Head, 0.8 for Torso).
            *   Check `Is Critical Part` for parts like 'Head' or 'Torso' if you want a destroyed critical part to kill the character instantly.
        *   **Important**: Ensure the `GameObject.name` of each body part is unique within the character's hierarchy (e.g., "Head", "Torso", "LeftArm") as the `SkeletalHealthSystem` uses these names for lookup.

4.  **Setup `DamageDealer` (Example):**
    *   Create an empty GameObject in your scene (e.g., "PlayerWeapon" or "DamageSource").
    *   Attach the `DamageDealer.cs` script to this GameObject.
    *   Configure its `Damage Amount`, `Damage Type`, `Attack Range`.
    *   Set `Hit Layers` to include the layer(s) your character model's colliders are on.
    *   (Optional) Add a `LineRenderer` component to the `DamageDealer` GameObject and assign it to the `_lineRenderer` field for visual debugging of raycasts.

5.  **Test in Play Mode:**
    *   Run the scene.
    *   Position your `DamageDealer` GameObject to face your character.
    *   Press the configured `Fire Key` (default: Left Mouse Button).
    *   You should see debug messages in the Console indicating which part was hit, how much damage it took, and its remaining health. You'll also see messages for part destruction and character death.

### Example Hierarchy Setup:

```
- CharacterRoot (GameObject with SkeletalHealthSystem)
    - Torso (GameObject with CapsuleCollider and DamageablePart: MaxHealth=150, DamageMultiplier=0.8, IsCriticalPart=true)
        - Head (GameObject with SphereCollider and DamageablePart: MaxHealth=50, DamageMultiplier=2.0, IsCriticalPart=true)
        - LeftArm (GameObject with CapsuleCollider and DamageablePart: MaxHealth=70, DamageMultiplier=1.0)
            - LeftHand (GameObject with SphereCollider and DamageablePart: MaxHealth=30, DamageMultiplier=0.5)
        - RightArm (GameObject with CapsuleCollider and DamageablePart: MaxHealth=70, DamageMultiplier=1.0)
        - Pelvis (GameObject with BoxCollider and DamageablePart: MaxHealth=100, DamageMultiplier=0.9)
            - LeftLeg (GameObject with CapsuleCollider and DamageablePart: MaxHealth=80, DamageMultiplier=1.0)
            - RightLeg (GameObject with CapsuleCollider and DamageablePart: MaxHealth=80, DamageMultiplier=1.0)
```

This complete example provides a robust and extensible foundation for implementing a Skeletal Damage System in your Unity projects.