// Unity Design Pattern Example: HitReactionSystem
// This script demonstrates the HitReactionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The HitReactionSystem design pattern in Unity is a robust way to manage how an entity reacts to being hit. It allows for different types of reactions (flinch, knockback, stagger, death, etc.) based on various factors like damage taken, hit type, current health, and specific conditions.

This pattern typically leverages a combination of:
1.  **Strategy Pattern**: Each specific reaction (e.g., Flinch, Knockback) is a "strategy" that implements a common interface.
2.  **Context/Manager**: A central component (`HitReactionSystem`) on the hit entity that receives hit information and decides which reaction strategy to execute.
3.  **Data Payload**: A `HitData` struct/class carries all necessary information about the hit.

This example provides a complete, practical implementation ready to be dropped into a Unity project.

---

### HitReactionSystem Design Pattern Overview

*   **`HitData` (Payload)**: A struct containing all relevant information about a hit (damage, force, hit point, attacker, hit type).
*   **`HitReactionType` (Enum)**: Categorizes different kinds of reactions for easier selection and prioritization.
*   **`IHitReaction` (Strategy Interface)**: Defines the common contract for all reaction strategies. It includes methods for checking if a reaction can be executed and for executing it.
*   **`HitReactionBase` (Abstract Base Strategy)**: A `[System.Serializable]` abstract class that implements `IHitReaction`. It provides common properties like `ReactionType` and `Priority`, making it easy to create concrete reaction strategies directly in the Inspector.
*   **Concrete Reactions (Concrete Strategies)**: Classes like `FlinchReaction`, `KnockbackReaction`, `StaggerReaction`, and `DeathReaction` inherit from `HitReactionBase`. Each encapsulates the specific logic for its reaction type (e.g., playing an animation, applying force, changing state). These are `[System.Serializable]` so they can be configured directly in the Unity Inspector.
*   **`Health` (Support Component)**: A simple MonoBehaviour to manage the entity's health, often crucial for reaction conditions (e.g., triggering a death reaction).
*   **`HitReactionSystem` (Context/Manager)**: The core MonoBehaviour that receives `HitData`, processes it, determines the most appropriate reaction strategy based on priority and conditions, and then executes it. It orchestrates the entire system.
*   **`Attacker` (Example Usage)**: A simple MonoBehaviour to simulate an entity dealing damage and triggering the `HitReactionSystem`.

---

### File Structure and Code

You can create these files in your Unity project. For instance, create a folder named `Scripts/HitReactionSystem` and place all files there.

**1. `HitData.cs`**
This struct holds all relevant information about a single hit.

```csharp
// HitData.cs
using UnityEngine;

/// <summary>
/// Represents all relevant information about a single hit event.
/// This data is passed to the HitReactionSystem to determine and execute reactions.
/// </summary>
[System.Serializable]
public struct HitData
{
    public float damage;
    public Vector3 hitPoint; // The world position where the hit occurred
    public Vector3 hitDirection; // The direction the hit came from (e.g., attacker's forward)
    public float hitForce; // The magnitude of force applied by the hit
    public Transform attackerTransform; // Reference to the attacker's transform (optional)
    public HitType hitType; // Categorization of the hit (e.g., Light, Heavy, Pierce)

    public enum HitType
    {
        Generic,
        Light,
        Heavy,
        Pierce,
        Blunt,
        Explosion
    }

    public HitData(float damage, Vector3 hitPoint, Vector3 hitDirection, float hitForce, Transform attackerTransform, HitType hitType = HitType.Generic)
    {
        this.damage = damage;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection.normalized; // Ensure direction is normalized
        this.hitForce = hitForce;
        this.attackerTransform = attackerTransform;
        this.hitType = hitType;
    }
}
```

**2. `HitReactionType.cs`**
An enum to categorize different reactions for easier organization and selection.

```csharp
// HitReactionType.cs
/// <summary>
/// Defines the various types of hit reactions supported by the system.
/// This enum helps categorize and prioritize different reaction strategies.
/// </summary>
public enum HitReactionType
{
    None = 0,
    Flinch = 1,      // A small, quick reaction without significant movement.
    Stagger = 2,     // A longer, more pronounced reaction, potentially interrupting actions.
    Knockback = 3,   // Reaction involving physical force and displacement.
    Death = 100,     // Special reaction for when health reaches zero.
    // Add more reaction types as needed (e.g., Explode, Burn, Freeze, Deflect)
}
```

**3. `IHitReaction.cs`**
The interface that all concrete reaction strategies must implement.

```csharp
// IHitReaction.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Defines the contract for any hit reaction strategy.
/// Each concrete reaction type (Flinch, Knockback, Death, etc.) will implement this interface.
/// </summary>
public interface IHitReaction
{
    /// <summary>
    /// The type of reaction this strategy represents. Used for identification and filtering.
    /// </summary>
    HitReactionType ReactionType { get; }

    /// <summary>
    /// The priority of this reaction. Higher priority reactions will be preferred
    /// when multiple reactions are eligible.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Initializes the reaction strategy. Called once when the HitReactionSystem is initialized.
    /// </summary>
    /// <param name="entityGameObject">The GameObject to which the HitReactionSystem is attached.</param>
    /// <param name="animator">The Animator component of the entity.</param>
    /// <param name="rigidbody">The Rigidbody component of the entity.</param>
    /// <param name="healthComponent">The Health component of the entity.</param>
    void Initialize(GameObject entityGameObject, Animator animator, Rigidbody rb, Health healthComponent);

    /// <summary>
    /// Determines if this reaction can be executed given the current hit data and entity state.
    /// </summary>
    /// <param name="hitData">The data describing the incoming hit.</param>
    /// <param name="currentHealth">The current health of the entity.</param>
    /// <returns>True if this reaction is a candidate for execution, false otherwise.</returns>
    bool CanExecute(HitData hitData, float currentHealth);

    /// <summary>
    /// Executes the specific logic for this hit reaction.
    /// This method is an IEnumerator because reactions often involve timed animations, forces, etc.
    /// The HitReactionSystem will start this as a Coroutine.
    /// </summary>
    /// <param name="hitData">The data describing the incoming hit.</param>
    /// <param name="ownerSystem">Reference to the HitReactionSystem that is executing this reaction.</param>
    /// <returns>An IEnumerator for a coroutine that manages the reaction's duration and effects.</returns>
    IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem);
}
```

**4. `HitReactionBase.cs`**
An abstract base class for all concrete reaction strategies. It's `[System.Serializable]` so derived classes can be configured in the Inspector.

```csharp
// HitReactionBase.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Abstract base class for all hit reaction strategies.
/// Implements IHitReaction and provides common properties and initialization logic.
/// Being [System.Serializable] allows concrete reaction classes inheriting from this
/// to be configured directly in the Unity Inspector as part of a list.
/// </summary>
[System.Serializable]
public abstract class HitReactionBase : IHitReaction
{
    // These fields are public for serialization but we'll use protected for internal access
    [field: SerializeField] public HitReactionType ReactionType { get; protected set; } = HitReactionType.None;
    [field: SerializeField] public int Priority { get; protected set; } = 0;

    [Tooltip("Minimum damage required for this reaction to be considered.")]
    [SerializeField] protected float minDamageThreshold = 0f;
    [Tooltip("Animation trigger or state name to play for this reaction.")]
    [SerializeField] protected string animationTrigger = "";
    [Tooltip("Duration of the reaction animation/effect.")]
    [SerializeField] protected float reactionDuration = 0.5f;

    // References to the entity's components, initialized by the HitReactionSystem
    protected GameObject _entityGameObject;
    protected Animator _animator;
    protected Rigidbody _rigidbody;
    protected Health _healthComponent;

    /// <summary>
    /// Initializes the reaction strategy with references to the entity's components.
    /// </summary>
    public virtual void Initialize(GameObject entityGameObject, Animator animator, Rigidbody rb, Health healthComponent)
    {
        _entityGameObject = entityGameObject;
        _animator = animator;
        _rigidbody = rb;
        _healthComponent = healthComponent;
    }

    /// <summary>
    /// Base implementation for checking if a reaction can be executed.
    /// Concrete reactions can override this to add specific conditions.
    /// </summary>
    public virtual bool CanExecute(HitData hitData, float currentHealth)
    {
        // Basic check: damage must meet the minimum threshold
        return hitData.damage >= minDamageThreshold;
    }

    /// <summary>
    /// Abstract method for executing the reaction. Must be implemented by concrete classes.
    /// </summary>
    public abstract IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem);

    /// <summary>
    /// Helper method to play an animation trigger if an animator is present.
    /// </summary>
    protected void PlayAnimation()
    {
        if (_animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            _animator.SetTrigger(animationTrigger);
        }
    }
}
```

**5. Concrete Reaction Strategies (`FlinchReaction.cs`, `KnockbackReaction.cs`, `StaggerReaction.cs`, `DeathReaction.cs`)**

These classes demonstrate different reaction types. Each is `[System.Serializable]` so it can be configured in the Inspector.

```csharp
// FlinchReaction.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// A concrete reaction strategy for a basic 'flinch' reaction.
/// Typically involves playing a short animation and no physical movement.
/// </summary>
[System.Serializable]
public class FlinchReaction : HitReactionBase
{
    [Tooltip("Minimum damage for a flinch reaction.")]
    [SerializeField] private float flinchMinDamage = 5f;

    public FlinchReaction()
    {
        ReactionType = HitReactionType.Flinch;
        Priority = 10; // Low priority
        animationTrigger = "Flinch";
        reactionDuration = 0.3f;
        minDamageThreshold = flinchMinDamage; // Set base min damage
    }

    public override bool CanExecute(HitData hitData, float currentHealth)
    {
        // Flinch only if damage is above threshold and entity is not dead
        return base.CanExecute(hitData, currentHealth) && currentHealth > 0;
    }

    public override IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem)
    {
        Debug.Log($"<color=orange>{_entityGameObject.name}</color> is Flinching from {hitData.damage} damage!");
        PlayAnimation();

        // Prevent movement during flinch (example: disable a character controller)
        // var characterController = _entityGameObject.GetComponent<MyCharacterController>();
        // if (characterController != null) characterController.SetCanMove(false);

        yield return new WaitForSeconds(reactionDuration);

        // Re-enable movement
        // if (characterController != null) characterController.SetCanMove(true);

        Debug.Log($"<color=orange>{_entityGameObject.name}</color> finished Flinching.");
    }
}
```

```csharp
// KnockbackReaction.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// A concrete reaction strategy for a 'knockback' reaction.
/// Involves playing an animation and applying physical force to the Rigidbody.
/// </summary>
[System.Serializable]
public class KnockbackReaction : HitReactionBase
{
    [Tooltip("Minimum damage for a knockback reaction.")]
    [SerializeField] private float knockbackMinDamage = 20f;
    [Tooltip("Force multiplier for knockback.")]
    [SerializeField] private float knockbackForceMultiplier = 5f;
    [Tooltip("How long the entity is stunned/unable to act after knockback.")]
    [SerializeField] private float stunDuration = 0.5f;

    public KnockbackReaction()
    {
        ReactionType = HitReactionType.Knockback;
        Priority = 20; // Medium priority, overrides flinch
        animationTrigger = "Knockback";
        reactionDuration = 0.8f;
        minDamageThreshold = knockbackMinDamage; // Set base min damage
    }

    public override bool CanExecute(HitData hitData, float currentHealth)
    {
        // Knockback only if damage is above threshold, entity is not dead, and has a Rigidbody
        return base.CanExecute(hitData, currentHealth) && currentHealth > 0 && _rigidbody != null;
    }

    public override IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem)
    {
        Debug.Log($"<color=red>{_entityGameObject.name}</color> is Knocked Back by {hitData.damage} damage!");
        PlayAnimation();

        // Calculate knockback direction (opposite of hit direction, or away from attacker)
        Vector3 forceDirection = -hitData.hitDirection;
        // Optionally, make it slightly upwards for a 'pop-up' effect
        forceDirection = (forceDirection + Vector3.up * 0.5f).normalized;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false; // Ensure Rigidbody is active for physics
            _rigidbody.AddForce(forceDirection * hitData.hitForce * knockbackForceMultiplier, ForceMode.Impulse);
        }

        // Prevent movement during knockback (example: disable a character controller)
        // var characterController = _entityGameObject.GetComponent<MyCharacterController>();
        // if (characterController != null) characterController.SetCanMove(false);

        yield return new WaitForSeconds(reactionDuration + stunDuration);

        // Re-enable movement and potentially make rigidbody kinematic again if it was
        // if (characterController != null) characterController.SetCanMove(true);
        // if (_rigidbody != null && wasRigidbodyKinematic) _rigidbody.isKinematic = true;

        Debug.Log($"<color=red>{_entityGameObject.name}</color> finished Knockback.");
    }
}
```

```csharp
// StaggerReaction.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// A concrete reaction strategy for a 'stagger' reaction.
/// Involves a longer animation, temporarily disabling player control, and no physical force.
/// </summary>
[System.Serializable]
public class StaggerReaction : HitReactionBase
{
    [Tooltip("Minimum damage for a stagger reaction.")]
    [SerializeField] private float staggerMinDamage = 15f;
    [Tooltip("Duration the entity is stunned/unable to act during stagger.")]
    [SerializeField] private float staggerStunDuration = 1.0f;

    public StaggerReaction()
    {
        ReactionType = HitReactionType.Stagger;
        Priority = 15; // Between flinch and knockback
        animationTrigger = "Stagger";
        reactionDuration = 0.8f;
        minDamageThreshold = staggerMinDamage; // Set base min damage
    }

    public override bool CanExecute(HitData hitData, float currentHealth)
    {
        // Stagger only if damage is above threshold and entity is not dead
        return base.CanExecute(hitData, currentHealth) && currentHealth > 0;
    }

    public override IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem)
    {
        Debug.Log($"<color=purple>{_entityGameObject.name}</color> is Staggering from {hitData.damage} damage!");
        PlayAnimation();

        // Temporarily disable input/movement during stagger
        // var playerInput = _entityGameObject.GetComponent<PlayerInput>();
        // if (playerInput != null) playerInput.DisableInput();

        yield return new WaitForSeconds(staggerStunDuration);

        // Re-enable input/movement
        // if (playerInput != null) playerInput.EnableInput();

        Debug.Log($"<color=purple>{_entityGameObject.name}</color> finished Staggering.");
    }
}
```

```csharp
// DeathReaction.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// A concrete reaction strategy for a 'death' reaction.
/// This reaction has the highest priority and typically plays a death animation
/// and disables further interaction with the entity.
/// </summary>
[System.Serializable]
public class DeathReaction : HitReactionBase
{
    public DeathReaction()
    {
        ReactionType = HitReactionType.Death;
        Priority = 1000; // Highest priority, should always execute if health is zero
        animationTrigger = "Death";
        reactionDuration = 3.0f; // Longer duration for death animation
        minDamageThreshold = 0f; // Death reaction can be triggered by any damage if health is 0
    }

    public override bool CanExecute(HitData hitData, float currentHealth)
    {
        // Death reaction only executes if health is 0 or less
        return currentHealth <= 0;
    }

    public override IEnumerator Execute(HitData hitData, HitReactionSystem ownerSystem)
    {
        Debug.Log($"<color=red>{_entityGameObject.name}</color> has DIED from {hitData.damage} damage!");
        PlayAnimation();

        // Disable health, collider, and other components to signify death
        if (_healthComponent != null) _healthComponent.enabled = false;
        if (_entityGameObject.TryGetComponent<Collider>(out var col)) col.enabled = false;
        // Optionally, disable Rigidbody physics or set it to kinematic if not already
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            // _rigidbody.isKinematic = true; // Uncomment if you want it to stop reacting to physics
        }

        // Inform other systems about death
        // EventManager.TriggerEvent("OnEntityDied", _entityGameObject);

        yield return new WaitForSeconds(reactionDuration); // Wait for death animation to play

        // Optionally, destroy the GameObject after the animation or deactivate it
        // GameObject.Destroy(_entityGameObject);
        Debug.Log($"<color=red>{_entityGameObject.name}</color> finished Death animation.");
    }
}
```

**6. `Health.cs`**
A simple health component for entities.

```csharp
// Health.cs
using UnityEngine;
using System; // For Action

/// <summary>
/// A simple component to manage the health of an entity.
/// Provides methods for taking damage and healing.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool invulnerable = false;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable => invulnerable;

    // Events for health changes
    public event Action<float> OnHealthChanged;
    public event Action OnDied;
    public event Action OnTookDamage;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the entity.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply.</param>
    /// <returns>The actual damage dealt (might be less if invulnerable or health clamps to 0).</returns>
    public float TakeDamage(float damageAmount)
    {
        if (IsDead || invulnerable || damageAmount <= 0)
        {
            return 0f; // No damage taken if already dead, invulnerable, or damage is non-positive
        }

        float damageDealt = damageAmount;
        currentHealth -= damageDealt;
        currentHealth = Mathf.Max(currentHealth, 0); // Clamp health to 0

        Debug.Log($"{gameObject.name} took {damageDealt} damage. Current Health: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth);
        OnTookDamage?.Invoke();

        if (IsDead)
        {
            OnDied?.Invoke();
            Debug.Log($"{gameObject.name} has died!");
        }
        return damageDealt;
    }

    /// <summary>
    /// Heals the entity.
    /// </summary>
    /// <param name="healAmount">The amount of health to restore.</param>
    public void Heal(float healAmount)
    {
        if (IsDead || healAmount <= 0) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Clamp health to max

        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"{gameObject.name} healed {healAmount}. Current Health: {currentHealth}");
    }

    /// <summary>
    /// Sets the invulnerability status.
    /// </summary>
    public void SetInvulnerable(bool status)
    {
        invulnerable = status;
        Debug.Log($"{gameObject.name} invulnerability set to: {invulnerable}");
    }

    /// <summary>
    /// Resets health to max.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"{gameObject.name} health reset to {currentHealth}");
    }
}
```

**7. `HitReactionSystem.cs`**
The core component that manages and executes hit reactions.

```csharp
// HitReactionSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The core component of the HitReactionSystem design pattern.
/// Attached to an entity, it manages a collection of IHitReaction strategies,
/// processes incoming HitData, and decides which reaction to execute based on
/// conditions, priorities, and entity state.
/// </summary>
[RequireComponent(typeof(Health))] // Reactions often depend on health
[RequireComponent(typeof(Animator))] // Reactions often involve animations
public class HitReactionSystem : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Health _healthComponent;

    [Header("Available Hit Reactions")]
    [Tooltip("Define and configure your hit reactions here. The system will choose the most appropriate one.")]
    [SerializeReference] // Allows serialization of interfaces/abstract classes without ScriptableObjects
    private List<HitReactionBase> _availableReactions = new List<HitReactionBase>
    {
        // Default reactions for easy setup (can be removed/modified in Inspector)
        new FlinchReaction(),
        new StaggerReaction(),
        new KnockbackReaction(),
        new DeathReaction()
    };

    private IEnumerator _currentReactionCoroutine;
    private bool _isReacting = false;
    public bool IsReacting => _isReacting;

    void Awake()
    {
        // Get component references if not assigned in Inspector
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_healthComponent == null) _healthComponent = GetComponent<Health>();

        // Initialize all available reaction strategies
        foreach (var reaction in _availableReactions)
        {
            reaction.Initialize(gameObject, _animator, _rigidbody, _healthComponent);
        }

        // Subscribe to death event to ensure death reaction is handled even if no damage is taken
        _healthComponent.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (_healthComponent != null)
        {
            _healthComponent.OnDied -= HandleDeath;
        }
    }

    /// <summary>
    /// Processes an incoming hit, applies damage, and triggers a suitable reaction.
    /// This is the primary entry point for other systems to interact with the HitReactionSystem.
    /// </summary>
    /// <param name="hitData">The data describing the hit event.</param>
    public void ProcessHit(HitData hitData)
    {
        if (_healthComponent.IsInvulnerable && hitData.damage > 0)
        {
            Debug.Log($"{gameObject.name} is invulnerable and took {hitData.damage} damage, no reaction.");
            return;
        }

        // 1. Apply Damage
        _healthComponent.TakeDamage(hitData.damage);

        // If a reaction is already in progress, decide if the new hit should interrupt it.
        // For simplicity, this example allows new reactions to interrupt, but you could
        // add logic here (e.g., only higher priority reactions interrupt).
        if (_isReacting && _currentReactionCoroutine != null)
        {
            StopCoroutine(_currentReactionCoroutine);
            _isReacting = false;
            Debug.LogWarning($"{gameObject.name} interrupted previous reaction.");
        }

        // 2. Select the most appropriate reaction strategy
        IHitReaction selectedReaction = ChooseReaction(hitData);

        // 3. Execute the selected reaction
        if (selectedReaction != null)
        {
            _currentReactionCoroutine = ExecuteReactionCoroutine(selectedReaction, hitData);
            StartCoroutine(_currentReactionCoroutine);
        }
        else
        {
            Debug.Log($"<color=grey>{gameObject.name} received {hitData.damage} damage but no suitable reaction was found.</color>");
        }
    }

    /// <summary>
    /// Determines the most appropriate hit reaction strategy based on the HitData and entity state.
    /// </summary>
    /// <param name="hitData">The data describing the hit event.</param>
    /// <returns>The IHitReaction strategy to execute, or null if no suitable reaction is found.</returns>
    private IHitReaction ChooseReaction(HitData hitData)
    {
        // First, check for Death reaction as it has highest priority and specific health condition
        if (_healthComponent.IsDead)
        {
            // Find the DeathReaction specifically
            IHitReaction deathReaction = _availableReactions.FirstOrDefault(r => r.ReactionType == HitReactionType.Death);
            if (deathReaction != null && deathReaction.CanExecute(hitData, _healthComponent.CurrentHealth))
            {
                return deathReaction;
            }
        }

        // Filter for reactions that can execute given the hit data and current health
        var eligibleReactions = _availableReactions
            .Where(r => r.ReactionType != HitReactionType.Death && r.CanExecute(hitData, _healthComponent.CurrentHealth))
            .OrderByDescending(r => r.Priority) // Order by priority, highest first
            .ToList();

        // If multiple reactions have the same highest priority, you could add
        // more complex logic here (e.g., pick based on hit type, random, etc.)
        return eligibleReactions.FirstOrDefault(); // Return the highest priority eligible reaction
    }

    /// <summary>
    /// Coroutine to manage the execution of a reaction strategy.
    /// Sets the _isReacting flag and handles its reset.
    /// </summary>
    /// <param name="reaction">The IHitReaction strategy to execute.</param>
    /// <param name="hitData">The data describing the hit event.</param>
    private IEnumerator ExecuteReactionCoroutine(IHitReaction reaction, HitData hitData)
    {
        _isReacting = true;
        // The Execute method of the reaction returns an IEnumerator, so we can yield it.
        yield return reaction.Execute(hitData, this);
        _isReacting = false;
        _currentReactionCoroutine = null; // Clear the reference
    }

    /// <summary>
    /// Handles the OnDied event from the Health component.
    /// Ensures a death reaction is always triggered if the entity dies,
    /// even if no damage was taken (e.g., from a fall, environmental hazard).
    /// </summary>
    private void HandleDeath()
    {
        // Create a dummy HitData for death, if needed.
        // The DeathReaction's CanExecute logic will primarily check health.
        HitData deathHitData = new HitData(0, transform.position, Vector3.zero, 0, null, HitData.HitType.Generic);

        // If a death reaction hasn't been triggered yet, trigger it now.
        // This prevents double-triggering if ProcessHit already handled death.
        if (!_isReacting || (_isReacting && _availableReactions.FirstOrDefault(r => r.ReactionType == HitReactionType.Death) != null && _currentReactionCoroutine == null))
        {
            IHitReaction deathReaction = _availableReactions.FirstOrDefault(r => r.ReactionType == HitReactionType.Death);
            if (deathReaction != null && deathReaction.CanExecute(deathHitData, _healthComponent.CurrentHealth))
            {
                // Ensure any current reaction is stopped before death.
                if (_currentReactionCoroutine != null)
                {
                    StopCoroutine(_currentReactionCoroutine);
                    _isReacting = false;
                }
                _currentReactionCoroutine = ExecuteReactionCoroutine(deathReaction, deathHitData);
                StartCoroutine(_currentReactionCoroutine);
            }
        }
    }

    /// <summary>
    /// Public method to get the current health status, often useful for reactions.
    /// </summary>
    public float GetCurrentHealth()
    {
        return _healthComponent.CurrentHealth;
    }

    /// <summary>
    /// Public method to check if the entity is currently dead.
    /// </summary>
    public bool IsEntityDead()
    {
        return _healthComponent.IsDead;
    }
}
```

**8. `Attacker.cs` (Example Usage)**
A script to simulate an attacker. Attach this to an empty GameObject or your player.

```csharp
// Attacker.cs
using UnityEngine;

/// <summary>
/// An example script to simulate an attacker dealing damage to a target
/// using the HitReactionSystem.
/// Attach this to an empty GameObject or a character that can attack.
/// </summary>
public class Attacker : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float lightAttackDamage = 10f;
    [SerializeField] private float heavyAttackDamage = 30f;
    [SerializeField] private float killAttackDamage = 100f; // For testing death
    [SerializeField] private float attackForce = 50f;
    [SerializeField] private LayerMask attackLayer; // Layer of entities that can be hit

    [Header("References")]
    [SerializeField] private Transform attackOrigin; // Point from which the attack originates
    [SerializeField] private float attackRange = 1.5f;

    void Update()
    {
        // Example: Press '1' for light attack
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PerformAttack(lightAttackDamage, HitData.HitType.Light);
        }

        // Example: Press '2' for heavy attack
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PerformAttack(heavyAttackDamage, HitData.HitType.Heavy);
        }

        // Example: Press '3' to kill (for testing death reaction)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PerformAttack(killAttackDamage, HitData.HitType.Heavy);
        }
    }

    void PerformAttack(float damage, HitData.HitType hitType)
    {
        // Simple sphere cast to find targets
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin.position, attackRange, attackLayer);

        foreach (var hitCollider in hitColliders)
        {
            // Try to get the HitReactionSystem from the hit object
            HitReactionSystem targetReactionSystem = hitCollider.GetComponent<HitReactionSystem>();

            if (targetReactionSystem != null)
            {
                // Create HitData with all relevant information
                HitData hitData = new HitData(
                    damage: damage,
                    hitPoint: hitCollider.ClosestPoint(attackOrigin.position), // Approximate hit point
                    hitDirection: (hitCollider.transform.position - attackOrigin.position).normalized, // Direction from attacker to target
                    hitForce: attackForce,
                    attackerTransform: this.transform,
                    hitType: hitType
                );

                // Pass the hit data to the target's HitReactionSystem
                targetReactionSystem.ProcessHit(hitData);
            }
        }

        if (hitColliders.Length == 0)
        {
            Debug.Log($"Attacked with {hitType} for {damage} damage, but hit nothing.");
        }
    }

    // Optional: Visualize attack range in editor
    void OnDrawGizmosSelected()
    {
        if (attackOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
        }
    }
}
```

---

### How to Use in Unity

1.  **Create a new Unity Project** or open an existing one.
2.  **Create a `Scripts` folder** (e.g., `Assets/Scripts/HitReactionSystem`).
3.  **Create all the `.cs` files** listed above inside this folder.
4.  **Setup your Target Entity (e.g., an Enemy):**
    *   Create a 3D object (e.g., a Cube or a more complex character model). Let's call it "Enemy".
    *   Add a `BoxCollider` (or `CapsuleCollider`) to it.
    *   Add a `Rigidbody` component. Set `Is Kinematic` to false if you want physics reactions like Knockback, otherwise true.
    *   Add an `Animator` component. Assign an Animator Controller with at least default "idle" and some reaction animation states (e.g., "Flinch", "Knockback", "Stagger", "Death") connected by `SetTrigger` parameters. (For this example, the animations will just be logged, but `PlayAnimation()` is there for actual animation calls).
    *   Add the `Health.cs` script to "Enemy".
    *   Add the `HitReactionSystem.cs` script to "Enemy".
        *   In the Inspector for `HitReactionSystem`, ensure `_animator`, `_rigidbody`, and `_healthComponent` are correctly assigned (they should auto-assign if on the same GameObject).
        *   The `_availableReactions` list will be pre-populated with `Flinch`, `Stagger`, `Knockback`, and `Death` reactions. You can customize their `minDamageThreshold`, `animationTrigger` names, `reactionDuration`, and `Priority` directly in the Inspector.
    *   Set the "Enemy" GameObject's Layer (e.g., to "Enemy" layer).

5.  **Setup your Attacker (e.g., Player or an empty GameObject):**
    *   Create an empty GameObject (e.g., "PlayerAttacker").
    *   Add the `Attacker.cs` script to "PlayerAttacker".
    *   In the Inspector for `Attacker`:
        *   Assign `Attack Origin` to an empty child GameObject positioned where your attacks should originate (e.g., at the player's hand).
        *   Set the `Attack Layer` to the layer your "Enemy" is on.
        *   Adjust `Attack Range` and `Damage` values as desired.

6.  **Run the Scene:**
    *   Play the scene.
    *   Select your "PlayerAttacker" GameObject.
    *   Press '1', '2', or '3' (default keybinds in `Attacker.cs`) to simulate different attacks.
    *   Observe the Console logs for which reaction is triggered on the "Enemy" based on its health and the incoming damage.
    *   Experiment with different damage values and reaction priorities in the `HitReactionSystem` inspector.

### Key Learnings & Practical Applications

*   **Modularity**: Adding new reaction types is as simple as creating a new class inheriting from `HitReactionBase`. No need to modify the central `HitReactionSystem` logic.
*   **Configurability**: Reactions are `[System.Serializable]` and can be extensively configured in the Inspector, allowing designers to tweak behavior without touching code.
*   **Prioritization**: The `Priority` field and `CanExecute` method allow for complex logic to determine the most appropriate reaction in any given scenario (e.g., a `DeathReaction` will always override a `FlinchReaction`).
*   **Separation of Concerns**: The `HitReactionSystem` is only responsible for *orchestrating* reactions, while each concrete reaction class handles its *specific implementation details*.
*   **Reusability**: `IHitReaction` and `HitReactionBase` provide a clear blueprint for any entity that needs to react to hits, promoting consistent behavior across different characters or objects.
*   **Extensibility**: You can easily add more conditions to `CanExecute` (e.g., `if (!IsBlocking)`), new parameters to `HitData` (e.g., element type, critical hit status), or new shared functionalities to `HitReactionBase` (e.g., common sound effects).

This example provides a solid foundation for building a robust and flexible hit reaction system for any Unity game.