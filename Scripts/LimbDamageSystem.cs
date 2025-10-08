// Unity Design Pattern Example: LimbDamageSystem
// This script demonstrates the LimbDamageSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Limb Damage System** design pattern. This pattern allows you to manage damage and health for individual body parts (limbs) of an entity, providing a more granular and often more realistic damage model than a simple single health bar.

The system is designed with extensibility in mind, using interfaces, events, and clear separation of concerns.

---

### LimbDamageSystem.cs

This script contains the core logic for the Limb Damage System. It defines limb types, limb properties, a damage information struct, an IDamageable interface, and the main `LimbDamageable` MonoBehaviour that orchestrates everything.

```csharp
using UnityEngine;
using System; // For Serializable
using System.Collections.Generic;
using System.Linq; // For Sum, Average, etc.
using UnityEngine.Events; // For UnityEvent

/// <summary>
/// Defines the different types of body parts (limbs) that can take damage.
/// Extend this enum with more specific body parts as needed for your game.
/// </summary>
public enum LimbType
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    // Add more specific limbs here, e.g., "Neck", "Hand", "Foot", "Tail", "Wing"
    None // For cases where no specific limb is targeted or damage applies generally
}

/// <summary>
/// A struct to encapsulate all relevant information about an incoming damage event.
/// This makes the damage system more flexible and extensible.
/// </summary>
public struct DamageInfo
{
    /// <summary>The base amount of damage to be applied.</summary>
    public float Amount;
    /// <summary>The specific limb targeted by this damage. If None, it might apply generally or to a default limb.</summary>
    public LimbType TargetLimb;
    /// <summary>The GameObject that caused the damage (e.g., the attacker's weapon or the attacker itself).</summary>
    public GameObject DamageSource;
    /// <summary>An optional type of damage (e.g., "Bullet", "Fire", "Melee"). Useful for resistances/vulnerabilities.</summary>
    public string DamageType;
    /// <summary>True if this damage is a critical hit, allowing for special effects or calculations.</summary>
    public bool IsCritical;

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageInfo"/> struct.
    /// </summary>
    /// <param name="amount">The base amount of damage.</param>
    /// <param name="targetLimb">The limb targeted. Defaults to None.</param>
    /// <param name="damageSource">The source GameObject. Defaults to null.</param>
    /// <param name="damageType">The type of damage. Defaults to empty string.</param>
    /// <param name="isCritical">Whether it's a critical hit. Defaults to false.</param>
    public DamageInfo(float amount, LimbType targetLimb = LimbType.None, GameObject damageSource = null, string damageType = "", bool isCritical = false)
    {
        Amount = amount;
        TargetLimb = targetLimb;
        DamageSource = damageSource;
        DamageType = damageType;
        IsCritical = isCritical;
    }
}

/// <summary>
/// Interface for any GameObject that can take damage.
/// This allows different damage systems (e.g., a simple health system vs. LimbDamageSystem)
/// to be interchangeable while interacting with damage dealers.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the object using a DamageInfo struct.
    /// </summary>
    /// <param name="damageInfo">Information about the damage event.</param>
    void TakeDamage(DamageInfo damageInfo);

    /// <summary>
    /// Gets the current overall health of the object.
    /// </summary>
    /// <returns>The current health value.</returns>
    float GetCurrentHealth();

    /// <summary>
    /// Gets the maximum overall health of the object.
    /// </summary>
    /// <returns>The maximum health value.</returns>
    float GetMaxHealth();

    /// <summary>
    /// Checks if the object is currently alive (health > 0).
    /// </summary>
    /// <returns>True if alive, false otherwise.</returns>
    bool IsAlive();
}

/// <summary>
/// Represents a single body part (limb) with its own health, properties, and status.
/// This class handles damage and healing for itself. It is a serializable class
/// allowing it to be easily managed by the <see cref="LimbDamageable"/> component.
/// </summary>
[System.Serializable]
public class Limb
{
    [Tooltip("The type of this limb (e.g., Head, Torso).")]
    public LimbType Type;

    [Tooltip("The initial and maximum health for this specific limb.")]
    [SerializeField] private float _maxHealth;
    public float MaxHealth => _maxHealth;

    [Tooltip("The current health of this limb.")]
    [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    [Tooltip("A multiplier applied to incoming damage specifically for this limb. E.g., 2.0 for headshots.")]
    [SerializeField] private float _damageMultiplier;
    public float DamageMultiplier => _damageMultiplier;

    [Tooltip("True if this limb has been incapacitated (e.g., severed, broken beyond use).")]
    public bool IsIncapacitated { get; private set; }

    // Events for specific limb status changes (can be subscribed to by UI, effects, etc.)
    public UnityEvent<LimbType, float, float> OnLimbHealthChanged = new UnityEvent<LimbType, float, float>();
    public UnityEvent<LimbType> OnLimbIncapacitated = new UnityEvent<LimbType>();
    public UnityEvent<LimbType> OnLimbRepaired = new UnityEvent<LimbType>(); // e.g., for limb reattachment

    /// <summary>
    /// Initializes a new Limb instance.
    /// </summary>
    /// <param name="type">The type of the limb.</param>
    /// <param name="maxHealth">The maximum health for this limb.</param>
    /// <param name="damageMultiplier">The damage multiplier for this limb.</param>
    public Limb(LimbType type, float maxHealth, float damageMultiplier)
    {
        Type = type;
        _maxHealth = Mathf.Max(0.1f, maxHealth); // Ensure max health is at least a small positive value
        _currentHealth = _maxHealth;
        _damageMultiplier = Mathf.Max(0.1f, damageMultiplier); // Ensure multiplier is positive
        IsIncapacitated = false;
    }

    /// <summary>
    /// Applies damage to this limb.
    /// </summary>
    /// <param name="amount">The base damage amount to apply.</param>
    /// <returns>The actual damage taken by the limb (after multiplier).</returns>
    public float TakeDamage(float amount)
    {
        if (IsIncapacitated)
        {
            // Optionally, incapacitated limbs could take no further damage or trigger specific effects.
            // For now, they simply don't take damage if already incapacitated.
            return 0;
        }

        float actualDamage = amount * _damageMultiplier;
        _currentHealth -= actualDamage;
        _currentHealth = Mathf.Max(0, _currentHealth); // Health cannot go below 0

        OnLimbHealthChanged?.Invoke(Type, _currentHealth, _maxHealth);

        if (_currentHealth <= 0 && !IsIncapacitated)
        {
            IsIncapacitated = true;
            OnLimbIncapacitated?.Invoke(Type);
            Debug.Log($"Limb {Type} has been incapacitated!", this);
        }

        return actualDamage;
    }

    /// <summary>
    /// Heals this limb.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    /// <returns>The actual amount healed (clamped by max health).</returns>
    public float Heal(float amount)
    {
        if (amount < 0) return 0; // Cannot heal negative amount

        float oldHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth); // Health cannot exceed max health

        if (IsIncapacitated && _currentHealth > 0)
        {
            IsIncapacitated = false;
            OnLimbRepaired?.Invoke(Type);
            Debug.Log($"Limb {Type} has been repaired!", this);
        }

        float actualHealed = _currentHealth - oldHealth;
        if (actualHealed > 0)
        {
            OnLimbHealthChanged?.Invoke(Type, _currentHealth, _maxHealth);
        }
        return actualHealed;
    }

    /// <summary>
    /// Gets the current health of this limb as a percentage of its max health.
    /// </summary>
    /// <returns>Health percentage (0.0 to 1.0).</returns>
    public float GetHealthPercentage()
    {
        return _maxHealth > 0 ? _currentHealth / _maxHealth : 0;
    }
}


/// <summary>
/// The core component of the Limb Damage System.
/// This MonoBehaviour manages a collection of individual <see cref="Limb"/> objects,
/// processes incoming damage by routing it to specific limbs, and calculates
/// the overall health of the entity based on its limbs.
/// It implements the <see cref="IDamageable"/> interface, making it compatible
/// with generic damage-dealing systems.
/// </summary>
[DisallowMultipleComponent] // Ensures only one LimbDamageable can be on a GameObject
public class LimbDamageable : MonoBehaviour, IDamageable
{
    [Header("Limb Configuration")]
    [Tooltip("Define the initial setup for each limb on this character.")]
    [SerializeField] private List<LimbSetup> _initialLimbConfigurations = new List<LimbSetup>();

    [Header("Overall Health Calculation")]
    [Tooltip("If true, damage to 'None' limb type will be distributed proportionally across all currently active (non-incapacitated) limbs.")]
    [SerializeField] private bool _distributeUntargetedDamage = true;
    
    [Tooltip("If true, the character dies immediately if the Head or Torso are incapacitated (health reaches zero).")]
    [SerializeField] private bool _instantDeathOnCriticalLimbLoss = true;

    // A dictionary to store the actual Limb objects, allowing quick lookup by LimbType.
    private Dictionary<LimbType, Limb> _limbs = new Dictionary<LimbType, Limb>();

    // Overall health metrics, derived from the sum of all *active* limb healths.
    // When a limb is incapacitated, it no longer contributes to overall current or max health.
    private float _overallCurrentHealth;
    public float OverallCurrentHealth => _overallCurrentHealth;

    private float _overallMaxHealth;
    public float OverallMaxHealth => _overallMaxHealth;

    // --- Public Events ---
    // These events allow other systems (UI, VFX, SFX, AI, game logic) to react to damage.
    [Header("Events")]
    [Tooltip("Invoked when any individual limb's health changes.")]
    public UnityEvent<LimbType, float, float> OnLimbHealthChanged = new UnityEvent<LimbType, float, float>(); // LimbType, CurrentHealth, MaxHealth
    [Tooltip("Invoked when an individual limb becomes incapacitated (e.g., health reaches zero).")]
    public UnityEvent<LimbType> OnLimbIncapacitated = new UnityEvent<LimbType>(); // LimbType
    [Tooltip("Invoked when an individual limb is repaired after being incapacitated.")]
    public UnityEvent<LimbType> OnLimbRepaired = new UnityEvent<LimbType>(); // LimbType
    [Tooltip("Invoked when the overall health of the entity changes.")]
    public UnityEvent<float, float> OnOverallHealthChanged = new UnityEvent<float, float>(); // CurrentOverallHealth, MaxOverallHealth
    [Tooltip("Invoked when the entity's overall health reaches zero or is explicitly set to die.")]
    public UnityEvent OnDied = new UnityEvent();

    /// <summary>
    /// A serializable struct used for setting up limb properties in the Inspector.
    /// </summary>
    [Serializable]
    private struct LimbSetup
    {
        public LimbType LimbType;
        [Min(0.1f)] public float InitialHealth; // Each limb must have some health
        [Min(0.1f)] public float DamageMultiplier; // Must be a positive multiplier
    }

    private void Awake()
    {
        InitializeLimbs();
    }

    /// <summary>
    /// Initializes all limbs based on the configurations provided in the Inspector.
    /// Calculates the initial overall max health and current health.
    /// </summary>
    private void InitializeLimbs()
    {
        _limbs.Clear();
        _overallMaxHealth = 0;

        foreach (var setup in _initialLimbConfigurations)
        {
            if (_limbs.ContainsKey(setup.LimbType))
            {
                Debug.LogWarning($"Duplicate limb type '{setup.LimbType}' found in configuration for {name}. Skipping duplicate.", this);
                continue;
            }

            Limb newLimb = new Limb(setup.LimbType, setup.InitialHealth, setup.DamageMultiplier);
            _limbs.Add(setup.LimbType, newLimb);

            // Subscribe to limb-specific events to re-broadcast them or react to them
            newLimb.OnLimbHealthChanged.AddListener(HandleLimbHealthChanged);
            newLimb.OnLimbIncapacitated.AddListener(HandleLimbIncapacitated);
            newLimb.OnLimbRepaired.AddListener(HandleLimbRepaired);

            _overallMaxHealth += setup.InitialHealth;
        }

        // Initialize current overall health to be equal to max overall health
        _overallCurrentHealth = _overallMaxHealth;
        UpdateOverallHealth(); // Broadcast initial health state
    }

    /// <summary>
    /// Handles the `OnLimbHealthChanged` event from an individual limb.
    /// Re-broadcasts the event and triggers an overall health recalculation.
    /// </summary>
    private void HandleLimbHealthChanged(LimbType type, float currentHealth, float maxHealth)
    {
        OnLimbHealthChanged?.Invoke(type, currentHealth, maxHealth);
        UpdateOverallHealth(); // Recalculate overall health (important if max health reduces when limbs are lost)
    }

    /// <summary>
    /// Handles the `OnLimbIncapacitated` event from an individual limb.
    /// Re-broadcasts the event, updates overall health, and checks for instant death conditions.
    /// </summary>
    private void HandleLimbIncapacitated(LimbType type)
    {
        OnLimbIncapacitated?.Invoke(type);
        UpdateOverallHealth(); // Recalculate overall health (incapacitated limbs contribute 0 health)

        // Check for instant death conditions
        if (_instantDeathOnCriticalLimbLoss)
        {
            if (type == LimbType.Head || type == LimbType.Torso)
            {
                Die(); // Instant death
            }
        }
    }

    /// <summary>
    /// Handles the `OnLimbRepaired` event from an individual limb.
    /// Re-broadcasts the event and updates overall health.
    /// </summary>
    private void HandleLimbRepaired(LimbType type)
    {
        OnLimbRepaired?.Invoke(type);
        UpdateOverallHealth(); // Recalculate overall health
    }

    /// <summary>
    /// Implementation of <see cref="IDamageable.TakeDamage"/>.
    /// Processes incoming damage, applying it to the specified limb or distributing it.
    /// </summary>
    /// <param name="damageInfo">The damage event information.</param>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (!IsAlive())
        {
            Debug.Log($"{name} is already dead and cannot take further damage.", this);
            return;
        }

        if (damageInfo.TargetLimb != LimbType.None && _limbs.TryGetValue(damageInfo.TargetLimb, out Limb targetLimb))
        {
            // Apply damage to a specific limb
            targetLimb.TakeDamage(damageInfo.Amount);
        }
        else if (_distributeUntargetedDamage && damageInfo.TargetLimb == LimbType.None)
        {
            // Distribute damage proportionally across all *healthy* limbs
            DistributeDamageToAllActiveLimbs(damageInfo.Amount);
        }
        else if (damageInfo.TargetLimb == LimbType.None)
        {
            // Fallback: If not distributing, apply to a default limb (e.g., Torso) if it exists.
            if (_limbs.TryGetValue(LimbType.Torso, out Limb torsoLimb) && !torsoLimb.IsIncapacitated)
            {
                Debug.LogWarning($"Damage targeted to 'None' and distribution is off. Applying {damageInfo.Amount} damage to Torso for {name}.", this);
                torsoLimb.TakeDamage(damageInfo.Amount);
            }
            else
            {
                // No specific limb targeted, no distribution, and no active torso. No damage applied.
                Debug.LogWarning($"Damage targeted to 'None' but no active default limb (Torso) found for {name}. No damage applied.", this);
            }
        }
        else
        {
            // Target limb specified but not found in configured limbs.
            Debug.LogWarning($"Attempted to damage unknown or unconfigured limb type '{damageInfo.TargetLimb}' on {name}. No damage applied.", this);
        }

        // UpdateOverallHealth is called by limb events already, but calling it here ensures
        // overall health is always up-to-date even if a limb took 0 actual damage
        // (e.g., trying to damage an already incapacitated limb, which returns 0).
        UpdateOverallHealth();
    }

    /// <summary>
    /// Distributes a given damage amount across all *non-incapacitated* limbs proportionally
    /// based on their current health. This ensures that limbs with more health absorb more damage,
    /// and limbs that are already heavily damaged or incapacitated are prioritized less or ignored.
    /// </summary>
    /// <param name="amount">The total damage amount to distribute.</param>
    private void DistributeDamageToAllActiveLimbs(float amount)
    {
        // Filter for limbs that are not incapacitated and have some health remaining
        List<Limb> activeLimbs = _limbs.Values.Where(l => !l.IsIncapacitated && l.CurrentHealth > 0).ToList();
        if (activeLimbs.Count == 0) return; // No active limbs to distribute damage to

        float totalCurrentHealthOfActiveLimbs = activeLimbs.Sum(l => l.CurrentHealth);
        if (totalCurrentHealthOfActiveLimbs <= 0) return; // All active limbs at 0 health, no more damage can be taken.

        foreach (Limb limb in activeLimbs)
        {
            // Proportionally distribute damage based on limb's current health share
            float damageShare = (limb.CurrentHealth / totalCurrentHealthOfActiveLimbs) * amount;
            limb.TakeDamage(damageShare);
        }
    }


    /// <summary>
    /// Recalculates the overall current and max health based on the status of all limbs.
    /// Overall health is considered the sum of all *currently active (non-incapacitated)* limbs.
    /// Invokes `OnOverallHealthChanged` and `OnDied` if health drops to zero.
    /// </summary>
    private void UpdateOverallHealth()
    {
        float newCurrentHealth = 0;
        float newMaxHealth = 0;

        foreach (var kvp in _limbs)
        {
            Limb limb = kvp.Value;
            // Only count health from non-incapacitated limbs towards overall health.
            // This means losing a limb reduces your maximum effective health pool.
            if (!limb.IsIncapacitated)
            {
                newCurrentHealth += limb.CurrentHealth;
                newMaxHealth += limb.MaxHealth;
            }
        }
        
        // Track previous state for event triggering
        bool wasAlive = IsAlive();

        _overallCurrentHealth = newCurrentHealth;
        _overallMaxHealth = newMaxHealth;

        OnOverallHealthChanged?.Invoke(_overallCurrentHealth, _overallMaxHealth);

        // If entity just died (was alive, now not), trigger OnDied event.
        if (!IsAlive() && wasAlive)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals a specific limb.
    /// </summary>
    /// <param name="type">The type of the limb to heal.</param>
    /// <param name="amount">The amount of health to restore.</param>
    public void HealLimb(LimbType type, float amount)
    {
        if (_limbs.TryGetValue(type, out Limb limb))
        {
            limb.Heal(amount);
            // Overall health will be updated via the limb's OnLimbHealthChanged event -> HandleLimbHealthChanged
        }
        else
        {
            Debug.LogWarning($"Attempted to heal unknown limb type '{type}' on {name}.");
        }
    }

    /// <summary>
    /// Heals all non-incapacitated limbs by a given total amount.
    /// The healing is distributed proportionally based on each limb's missing health,
    /// prioritizing limbs that need more healing.
    /// </summary>
    /// <param name="amount">The total amount of healing to distribute across all active limbs.</param>
    public void HealAllLimbs(float amount)
    {
        if (amount < 0) return;

        List<Limb> activeLimbs = _limbs.Values.Where(l => !l.IsIncapacitated).ToList();
        if (activeLimbs.Count == 0) return;

        float totalMissingHealth = activeLimbs.Sum(l => l.MaxHealth - l.CurrentHealth);
        if (totalMissingHealth <= 0) return; // No healing needed for any limb

        foreach (Limb limb in activeLimbs)
        {
            if (limb.CurrentHealth < limb.MaxHealth)
            {
                // Proportionally distribute healing based on how much health each limb is missing
                float healShare = (limb.MaxHealth - limb.CurrentHealth) / totalMissingHealth * amount;
                limb.Heal(healShare);
            }
        }
        // Overall health will be updated via individual limb events.
    }

    /// <summary>
    /// Gets the current health of a specific limb.
    /// </summary>
    /// <param name="type">The type of the limb.</param>
    /// <returns>The current health, or 0 if limb not found.</returns>
    public float GetLimbCurrentHealth(LimbType type)
    {
        return _limbs.TryGetValue(type, out Limb limb) ? limb.CurrentHealth : 0;
    }

    /// <summary>
    /// Gets the maximum health of a specific limb.
    /// </summary>
    /// <param name="type">The type of the limb.</param>
    /// <returns>The maximum health, or 0 if limb not found.</returns>
    public float GetLimbMaxHealth(LimbType type)
    {
        return _limbs.TryGetValue(type, out Limb limb) ? limb.MaxHealth : 0;
    }

    /// <summary>
    /// Checks if a specific limb is currently incapacitated.
    /// </summary>
    /// <param name="type">The type of the limb.</param>
    /// <returns>True if incapacitated, false otherwise or if limb not found.</returns>
    public bool IsLimbIncapacitated(LimbType type)
    {
        return _limbs.TryGetValue(type, out Limb limb) && limb.IsIncapacitated;
    }

    /// <summary>
    /// Gets a read-only dictionary of all limbs managed by this system.
    /// Useful for UI display or external querying of limb status.
    /// </summary>
    public IReadOnlyDictionary<LimbType, Limb> GetLimbs()
    {
        return _limbs;
    }

    /// <summary>
    /// Implementation of <see cref="IDamageable.GetCurrentHealth"/>.
    /// </summary>
    public float GetCurrentHealth() => _overallCurrentHealth;

    /// <summary>
    /// Implementation of <see cref="IDamageable.GetMaxHealth"/>.
    /// </summary>
    public float GetMaxHealth() => _overallMaxHealth;

    /// <summary>
    /// Implementation of <see cref="IDamageable.IsAlive"/>.
    /// The entity is considered alive as long as its overall current health is greater than 0.
    /// </summary>
    public bool IsAlive() => _overallCurrentHealth > 0;

    /// <summary>
    /// Marks the entity as defeated (dead). Invokes the OnDied event.
    /// This method can be called externally to force death, or it's called
    /// internally when overall health reaches zero.
    /// </summary>
    public void Die()
    {
        if (IsAlive()) // Only trigger if not already dead
        {
            _overallCurrentHealth = 0; // Ensure health is exactly zero
            OnOverallHealthChanged?.Invoke(0, _overallMaxHealth); // Broadcast final health state
        }
        
        // Prevent triggering the event multiple times or if object is already being destroyed.
        if (gameObject.activeInHierarchy) 
        {
            OnDied?.Invoke();
            Debug.Log($"{name} has died!", this);
            // Example: Disable renderer, start ragdoll, show death animation, disable further scripts
            // For a simple death, you might do:
            // GetComponent<Collider>()?.enabled = false;
            // GetComponent<Rigidbody>()?.isKinematic = false;
            // GetComponent<MeshRenderer>()?.material.color = Color.gray; // Simple visual death
            // enabled = false; // Disable this script
        }
    }
}
```

---

### DamageDealer.cs (Example Usage)

This script acts as a simple attacker that finds `IDamageable` targets and applies damage to them, demonstrating how an external system interacts with `LimbDamageable`.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For ElementAtOrDefault and Random
using UnityEngine.UI; // For UI elements (optional, for demo)

/// <summary>
/// An example MonoBehaviour to demonstrate dealing damage using the LimbDamageSystem.
/// This script simulates an attacker that can target a <see cref="IDamageable"/> object
/// and apply damage to specific limbs or generally.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("The base damage amount dealt per hit.")]
    [SerializeField] private float _damageAmount = 20f;
    [Tooltip("The range within which the damage dealer can target an enemy.")]
    [SerializeField] private float _attackRange = 5f;
    [Tooltip("How often the damage dealer can attack (seconds).")]
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Targeting")]
    [Tooltip("The layer(s) on which damageable entities reside.")]
    [SerializeField] private LayerMask _damageableLayer;
    [Tooltip("If true, damage will be applied to a random limb. If false, a specific limb can be chosen.")]
    [SerializeField] private bool _targetRandomLimb = true;
    [Tooltip("If _targetRandomLimb is false, this specific limb will be targeted.")]
    [SerializeField] private LimbType _specificLimbToTarget = LimbType.Torso;

    private float _nextAttackTime = 0f;
    private IDamageable _currentTarget;

    [Header("UI (Optional for Demo)")]
    [SerializeField] private Text _debugText; // Assign a UI Text element for feedback

    void Update()
    {
        // Simple line-of-sight check to find a target
        FindTarget();

        if (_currentTarget != null && Time.time >= _nextAttackTime)
        {
            AttackTarget();
            _nextAttackTime = Time.time + _attackCooldown;
        }

        UpdateDebugUI();
    }

    /// <summary>
    /// Attempts to find an IDamageable target within the attack range using an overlap sphere.
    /// </summary>
    private void FindTarget()
    {
        _currentTarget = null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _attackRange, _damageableLayer);

        if (hitColliders.Length > 0)
        {
            // Pick the first valid IDamageable in range
            foreach (var collider in hitColliders)
            {
                if (collider.TryGetComponent<IDamageable>(out IDamageable damageable))
                {
                    if (damageable.IsAlive()) // Only target living entities
                    {
                        _currentTarget = damageable;
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attacks the current target, applying damage to a specific or random limb.
    /// </summary>
    private void AttackTarget()
    {
        if (_currentTarget == null || !_currentTarget.IsAlive())
        {
            Debug.Log($"{name}: No target or target is dead.", this);
            _currentTarget = null; // Clear target if it's dead
            return;
        }

        LimbType limbToHit = LimbType.None; // Default to general damage
        string debugMessage = $"{name} is attacking {_currentTarget.GetType().Name} ({_currentTarget.GetMaxHealth()}) with {_damageAmount} damage. ";

        // If the target uses LimbDamageable, we can target specific limbs
        if (_currentTarget is LimbDamageable limbDamageable)
        {
            if (_targetRandomLimb)
            {
                // Get a list of all *active* (non-incapacitated) limb types to choose from
                List<LimbType> availableLimbs = limbDamageable.GetLimbs()
                                                            .Where(kvp => !kvp.Value.IsIncapacitated)
                                                            .Select(kvp => kvp.Key)
                                                            .ToList();

                if (availableLimbs.Count > 0)
                {
                    // Select a random limb type from the available ones
                    limbToHit = availableLimbs[Random.Range(0, availableLimbs.Count)];
                    debugMessage += $"Targeting random limb: {limbToHit}.";
                }
                else
                {
                    // All specific limbs are incapacitated, but overall health might still exist.
                    // This means damage will be handled by the LimbDamageable's fallback for LimbType.None
                    limbToHit = LimbType.None;
                    debugMessage += "All specific limbs are incapacitated. Applying damage generally.";
                }
            }
            else // Not targeting random limb, use specific limb
            {
                limbToHit = _specificLimbToTarget;
                debugMessage += $"Targeting specific limb: {limbToHit}.";
            }
        }
        else // Fallback if target is IDamageable but not LimbDamageable, or if _targetRandomLimb is false and _specificLimbToTarget is None
        {
            limbToHit = LimbType.None; // Apply general damage, the IDamageable will handle it
            debugMessage += "Applying general (untargeted) damage.";
        }

        // Create a DamageInfo struct with details and apply it
        DamageInfo damageInfo = new DamageInfo(_damageAmount, limbToHit, gameObject, "Melee", Random.value < 0.2f); // 20% critical chance example
        _currentTarget.TakeDamage(damageInfo);

        Debug.Log(debugMessage, this);
    }

    /// <summary>
    /// Updates optional UI text for debugging purposes.
    /// </summary>
    private void UpdateDebugUI()
    {
        if (_debugText != null)
        {
            string status = $"DamageDealer: {name}\n";
            if (_currentTarget != null)
            {
                status += $"Target: {_currentTarget.GetType().Name}\n";
                status += $"Health: {_currentTarget.GetCurrentHealth():F0}/{_currentTarget.GetMaxHealth():F0}\n";
                status += $"Alive: {_currentTarget.IsAlive()}\n";
            }
            else
            {
                status += "No target in range.\n";
            }
            status += $"Next Attack: {Mathf.Max(0, _nextAttackTime - Time.time):F1}s";
            _debugText.text = status;
        }
    }

    /// <summary>
    /// Draws a sphere in the editor to visualize the attack range.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
```

---

### LimbHealthDisplay.cs (Example UI)

This script demonstrates how to subscribe to the events provided by `LimbDamageable` to update a UI Text component, showing the health of each limb and the overall health.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // For OrderBy
using System.Collections.Generic; // For List

/// <summary>
/// An example MonoBehaviour to display the health of all limbs and overall health
/// of a <see cref="LimbDamageable"/> entity on a UI Text component.
/// This serves as a practical demonstration of how to subscribe to the LimbDamageSystem's events.
/// </summary>
[RequireComponent(typeof(Text))] // Ensures this GameObject has a Text component
public class LimbHealthDisplay : MonoBehaviour
{
    [Tooltip("The LimbDamageable component to monitor. If null, it will try to find one in parent.")]
    [SerializeField] private LimbDamageable _targetLimbDamageable;

    private Text _healthText;

    void Awake()
    {
        _healthText = GetComponent<Text>();
        if (_healthText == null)
        {
            Debug.LogError("LimbHealthDisplay requires a Text component on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (_targetLimbDamageable == null)
        {
            _targetLimbDamageable = GetComponentInParent<LimbDamageable>();
            if (_targetLimbDamageable == null)
            {
                Debug.LogError("No LimbDamageable target assigned or found in parent hierarchy. Disabling display.", this);
                enabled = false;
                return;
            }
        }

        // Subscribe to events from the LimbDamageable system
        _targetLimbDamageable.OnLimbHealthChanged.AddListener(UpdateDisplay);
        _targetLimbDamageable.OnLimbIncapacitated.AddListener(OnLimbStatusChanged);
        _targetLimbDamageable.OnLimbRepaired.AddListener(OnLimbStatusChanged);
        _targetLimbDamageable.OnOverallHealthChanged.AddListener(UpdateOverallHealthDisplay);
        _targetLimbDamageable.OnDied.AddListener(OnTargetDied);

        // Initial update to show the current state
        UpdateFullDisplay();
    }

    void OnDestroy()
    {
        // IMPORTANT: Unsubscribe from events to prevent memory leaks if the target or this object is destroyed.
        if (_targetLimbDamageable != null)
        {
            _targetLimbDamageable.OnLimbHealthChanged.RemoveListener(UpdateDisplay);
            _targetLimbDamageable.OnLimbIncapacitated.RemoveListener(OnLimbStatusChanged);
            _targetLimbDamageable.OnLimbRepaired.RemoveListener(OnLimbStatusChanged);
            _targetLimbDamageable.OnOverallHealthChanged.RemoveListener(UpdateOverallHealthDisplay);
            _targetLimbDamageable.OnDied.RemoveListener(OnTargetDied);
        }
    }

    /// <summary>
    /// Called when any limb's health changes. Triggers a full display update for simplicity.
    /// (For complex UIs, you might only update the specific limb's line.)
    /// </summary>
    private void UpdateDisplay(LimbType type, float currentHealth, float maxHealth)
    {
        UpdateFullDisplay();
    }

    /// <summary>
    /// Called when a limb's incapacitated status changes. Triggers a full display update.
    /// </summary>
    private void OnLimbStatusChanged(LimbType type)
    {
        UpdateFullDisplay();
    }

    /// <summary>
    /// Called when the overall health changes. Triggers a full display update.
    /// </summary>
    private void UpdateOverallHealthDisplay(float currentHealth, float maxHealth)
    {
        UpdateFullDisplay();
    }

    /// <summary>
    /// Called when the target entity dies. Updates the display to reflect death.
    /// </summary>
    private void OnTargetDied()
    {
        UpdateFullDisplay();
    }

    /// <summary>
    /// Refreshes the entire UI display with current limb and overall health information.
    /// </summary>
    private void UpdateFullDisplay()
    {
        if (_healthText == null || _targetLimbDamageable == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.Append($"<color=white><b>{_targetLimbDamageable.name} Status</b></color>\n");
        sb.Append("---------------------------\n");

        if (!_targetLimbDamageable.IsAlive())
        {
            sb.Append("<color=red><b>DEAD</b></color>\n");
        }

        sb.Append($"<color=green>Overall Health: {_targetLimbDamageable.OverallCurrentHealth:F0}/{_targetLimbDamageable.OverallMaxHealth:F0}</color>\n");
        sb.Append("---------------------------\n");

        // Display individual limb health, ordered by LimbType for consistency
        foreach (var limbKVP in _targetLimbDamageable.GetLimbs().OrderBy(kvp => kvp.Key.ToString())) 
        {
            LimbType type = limbKVP.Key;
            Limb limb = limbKVP.Value;

            string color = "white"; // Default color
            string status = "";

            if (limb.IsIncapacitated)
            {
                color = "red";
                status = " (Incapacitated)"; // Indicate severed/broken limb
            }
            else if (limb.CurrentHealth < limb.MaxHealth * 0.25f)
            {
                color = "orange"; // Critically low health
            }
            else if (limb.CurrentHealth < limb.MaxHealth * 0.75f)
            {
                color = "yellow"; // Damaged
            }

            sb.Append($"<color={color}>  {type}: {limb.CurrentHealth:F0}/{limb.MaxHealth:F0}{status}</color>\n");
        }

        _healthText.text = sb.ToString();
    }
}
```

---

### How to Implement and Use in Unity

1.  **Create a New C# Script:**
    *   Name it `LimbDamageSystem.cs`. Copy and paste the content of the `LimbDamageSystem.cs` section into it.
2.  **Create another C# Script:**
    *   Name it `DamageDealer.cs`. Copy and paste the content of the `DamageDealer.cs` section into it.
3.  **Create another C# Script:**
    *   Name it `LimbHealthDisplay.cs`. Copy and paste the content of the `LimbHealthDisplay.cs` section into it.

---

#### Setting up the Damageable Entity (e.g., "Enemy")

1.  **Create a 3D Object:** In your Unity scene, go to `GameObject > 3D Object > Cube` (or any other primitive). Rename it "Enemy".
2.  **Add `LimbDamageable` Component:** Select the "Enemy" GameObject. In the Inspector, click "Add Component" and search for `LimbDamageable`. Add it.
3.  **Configure Limbs:**
    *   In the `LimbDamageable` component, expand "Limb Configuration".
    *   Increase the "Size" to define how many body parts your enemy has (e.g., 6 for Head, Torso, 2 Arms, 2 Legs).
    *   For each element, assign a `LimbType` (from the dropdown), `Initial Health` (e.g., Head: 50, Torso: 100, Arms/Legs: 75), and `Damage Multiplier` (e.g., Head: 2.0 for double damage, Torso: 1.0, Arms/Legs: 0.8 for reduced damage).
    *   Adjust `Distribute Untargeted Damage` and `Instant Death On Critical Limb Loss` as desired.
4.  **Set Layer:** Select the "Enemy" GameObject. In the Inspector, next to "Layer", choose "Add Layer...". Create a new layer named "Damageable". Go back to the "Enemy" GameObject and set its Layer to "Damageable".

---

#### Setting up the Attacker (e.g., "Attacker")

1.  **Create a 3D Object:** In your Unity scene, go to `GameObject > 3D Object > Sphere`. Rename it "Attacker".
2.  **Add `DamageDealer` Component:** Select the "Attacker" GameObject. In the Inspector, click "Add Component" and search for `DamageDealer`. Add it.
3.  **Configure DamageDealer:**
    *   Set `Damage Amount` (e.g., 25).
    *   Set `Attack Range` (e.g., 5).
    *   Set `Attack Cooldown` (e.g., 1).
    *   For `Damageable Layer`, select the "Damageable" layer you created for the enemy.
    *   Choose whether to `Target Random Limb` or `Specific Limb To Target`.

---

#### Setting up the UI Display

1.  **Create a UI Canvas:** In the Hierarchy, right-click `UI > Canvas`.
2.  **Create a Text Element:** Right-click on the newly created `Canvas > UI > Text - TextMeshPro`. If prompted, import TMP Essentials.
3.  **Position the Text:** Adjust the Rect Transform of the Text object so it's visible on your screen (e.g., top-left corner). Change its Font Size and Color to be easily readable.
4.  **Add `LimbHealthDisplay` Component:** Select the TextMeshPro GameObject. In the Inspector, click "Add Component" and search for `LimbHealthDisplay`. Add it.
5.  **Assign Target:** Drag your "Enemy" GameObject from the Hierarchy into the `Target Limb Damageable` slot of the `LimbHealthDisplay` component.
6.  **Assign Debug Text for Attacker:** Select your "Attacker" GameObject. Drag the UI Text GameObject into the `Debug Text` slot of the `DamageDealer` component.

---

#### Run the Scene

1.  **Position Attacker and Enemy:** Place your "Attacker" and "Enemy" GameObjects within the `Attack Range` of the attacker.
2.  **Play:** Run your Unity scene.

You should observe:
*   The "Attacker" sphere will periodically deal damage to the "Enemy" cube.
*   The UI Text will dynamically update, showing the overall health and the individual health of each limb.
*   You'll see "Limb incapacitated!" messages in the Console when a limb's health reaches zero.
*   If `Instant Death On Critical Limb Loss` is enabled and Head or Torso are incapacitated, the enemy will die immediately.
*   The `DamageDealer`'s debug text will show its current target and health.

This setup provides a robust and observable example of the Limb Damage System in action, ready for further expansion and integration into your projects.