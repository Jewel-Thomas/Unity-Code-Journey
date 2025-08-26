// Unity Design Pattern Example: ChainOfResponsibility
// This script demonstrates the ChainOfResponsibility pattern in Unity
// Generated automatically - ready to use in your Unity project

The Chain of Responsibility is a behavioral design pattern that allows you to pass requests along a chain of handlers. Upon receiving a request, each handler decides either to process the request or to pass it to the next handler in the chain.

This pattern is useful when:
*   More than one object may handle a request, and the actual handler is not known beforehand.
*   You want to issue a request to one of several objects without explicitly specifying the receiver.
*   The set of objects that can handle a request should be specified dynamically.

### Real-World Unity Example: Damage Calculation and Application

Imagine a game where a character takes damage. This damage might be affected by various factors:
1.  **Evasion:** The target might completely evade the damage.
2.  **Critical Hit:** The attack might be a critical hit, increasing its damage.
3.  **Armor/Resistance:** The target's armor or elemental resistance might reduce the damage.
4.  **Status Effects:** Certain damage types might apply status effects (e.g., fire damage applies "Burn").
5.  **Final Application:** The calculated damage is finally applied to the target's health.

Instead of having a single, monolithic function that performs all these checks with many `if/else` statements, we can use the Chain of Responsibility pattern. Each factor (Evasion, Critical Hit, Armor, Status Effect, Final Application) becomes a "handler" in the chain.

---

### `ChainOfResponsibilityDamageSystem.cs`

This script provides all the necessary components for the Chain of Responsibility pattern, including mock Unity components for demonstration purposes.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Text; // For StringBuilder

// =====================================================================================================================
// 0. Damage-related Enums and Data Structures
// =====================================================================================================================

/// <summary>
/// Represents different types of damage that can be inflicted.
/// </summary>
public enum DamageType
{
    Physical,
    Magical,
    Fire,
    Ice,
    Poison
}

/// <summary>
/// Represents a request for damage to be processed. This object will travel through the chain,
/// and its properties will be modified by each handler.
/// </summary>
public class DamageRequest
{
    public GameObject Source { get; private set; }
    public GameObject Target { get; private set; }
    public float BaseDamage { get; private set; }
    public DamageType Type { get; private set; }

    // Properties that handlers will modify
    public float CurrentDamage { get; set; }
    public bool IsCritical { get; set; }
    public bool IsEvaded { get; set; }
    public List<string> AppliedStatusEffects { get; private set; }
    public List<string> LogMessages { get; private set; } // To track what happened during processing

    /// <summary>
    /// A flag to indicate if the damage application process has completed.
    /// This allows a handler (like the final damage applier) to signal that no further
    /// processing is needed, effectively stopping the chain if desired.
    /// </summary>
    public bool IsDamageProcessedAndApplied { get; set; }

    public DamageRequest(GameObject source, GameObject target, float baseDamage, DamageType type)
    {
        Source = source;
        Target = target;
        BaseDamage = baseDamage;
        Type = type;

        CurrentDamage = baseDamage; // Initialize current damage with base damage
        IsCritical = false;
        IsEvaded = false;
        AppliedStatusEffects = new List<string>();
        LogMessages = new List<string>();
        IsDamageProcessedAndApplied = false;

        Log($"--- Initial Damage Request: {source.name} dealing {baseDamage} {type} damage to {target.name} ---");
    }

    /// <summary>
    /// Adds a message to the request's log, useful for debugging and summarizing the damage event.
    /// </summary>
    /// <param name="message"></param>
    public void Log(string message)
    {
        LogMessages.Add(message);
    }

    /// <summary>
    /// Returns a full summary of the damage request processing.
    /// </summary>
    public string GetSummary()
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine($"Damage Summary for {Target.name} from {Source.name}:");
        foreach (string msg in LogMessages)
        {
            summary.AppendLine($"- {msg}");
        }
        if (IsEvaded)
        {
            summary.AppendLine("Result: Damage was entirely EVADED!");
        }
        else if (IsDamageProcessedAndApplied)
        {
             summary.AppendLine($"Result: {CurrentDamage:F2} damage was dealt.");
             if (AppliedStatusEffects.Count > 0)
             {
                 summary.AppendLine($"Status Effects Applied: {string.Join(", ", AppliedStatusEffects)}");
             }
        }
        else
        {
            summary.AppendLine("Result: Damage processing was interrupted before final application.");
        }
        summary.AppendLine("---------------------------------------------------------------------");
        return summary.ToString();
    }
}

// =====================================================================================================================
// 1. Handler Interface
// Defines the interface for all handlers in the chain.
// =====================================================================================================================

/// <summary>
/// The Handler interface declares a method for building the chain of handlers.
/// It also declares a method for executing a request.
/// </summary>
public interface IDamageHandler
{
    /// <summary>
    /// Sets the next handler in the chain. Returns the handler passed in, allowing for fluent chaining.
    /// </summary>
    IDamageHandler SetNext(IDamageHandler handler);

    /// <summary>
    /// Attempts to handle the given damage request.
    /// </summary>
    void Handle(DamageRequest request);
}

// =====================================================================================================================
// 2. Abstract Base Handler
// Provides common functionality for all concrete handlers.
// =====================================================================================================================

/// <summary>
/// The Base Handler provides the default chaining behavior.
/// It contains a field for storing the next handler in the chain.
/// </summary>
public abstract class AbstractDamageHandler : IDamageHandler
{
    private IDamageHandler _nextHandler;

    /// <summary>
    /// Sets the next handler in the chain and returns it for fluent setup.
    /// </summary>
    public IDamageHandler SetNext(IDamageHandler handler)
    {
        _nextHandler = handler;
        return handler;
    }

    /// <summary>
    /// The default implementation of the Handle method. It calls the abstract ProcessRequest
    /// method for specific handler logic, and then passes the request to the next handler
    /// if it exists and the request hasn't been fully processed/applied.
    /// </summary>
    public virtual void Handle(DamageRequest request)
    {
        // Each handler processes the request.
        // The specific logic for what each handler does is in ProcessRequest.
        ProcessRequest(request);

        // If the request is marked as fully processed (e.g., damage has been applied
        // or target died), we can stop passing it down the chain.
        // Otherwise, pass it to the next handler.
        if (!request.IsDamageProcessedAndApplied && _nextHandler != null)
        {
            _nextHandler.Handle(request);
        }
    }

    /// <summary>
    /// Abstract method that concrete handlers must implement to define their specific processing logic.
    /// </summary>
    protected abstract void ProcessRequest(DamageRequest request);
}

// =====================================================================================================================
// 3. Concrete Handlers
// Implement the specific logic for handling parts of the request.
// =====================================================================================================================

/// <summary>
/// Handles evasion checks. If the target evades, the damage is set to 0 and marked as evaded.
/// </summary>
public class EvasionHandler : AbstractDamageHandler
{
    protected override void ProcessRequest(DamageRequest request)
    {
        Debug.Log($"[EvasionHandler] Processing damage request for {request.Target.name}.");
        EvasionComponent evasion = request.Target.GetComponent<EvasionComponent>();
        if (evasion != null && evasion.TryEvade())
        {
            request.IsEvaded = true;
            request.CurrentDamage = 0; // Nullify damage
            request.Log($"{request.Target.name} successfully EVADED the attack!");
            // IMPORTANT: If evasion completely stops all subsequent effects/damage,
            // you might set request.IsDamageProcessedAndApplied = true; here to short-circuit.
            // For this example, we'll let it pass through with 0 damage, as other handlers might log or add minor effects.
        }
        else
        {
            request.Log($"{request.Target.name} failed to evade.");
        }
    }
}

/// <summary>
/// Handles critical hit calculation. If the source critically hits, damage is multiplied.
/// </summary>
public class CriticalHitHandler : AbstractDamageHandler
{
    protected override void ProcessRequest(DamageRequest request)
    {
        // Skip if damage already evaded
        if (request.IsEvaded)
        {
            Debug.Log($"[CriticalHitHandler] Skipping critical hit check because damage was evaded.");
            return;
        }

        Debug.Log($"[CriticalHitHandler] Processing damage request for {request.Target.name}.");
        CritChanceComponent critChance = request.Source.GetComponent<CritChanceComponent>();
        if (critChance != null && critChance.TryCrit())
        {
            request.IsCritical = true;
            request.CurrentDamage *= critChance.CritMultiplier;
            request.Log($"Critical Hit! Damage multiplied by {critChance.CritMultiplier:F1}. Current damage: {request.CurrentDamage:F2}");
        }
        else
        {
            request.Log("No critical hit.");
        }
    }
}

/// <summary>
/// Handles armor and resistance reduction. Reduces damage based on target's defenses.
/// </summary>
public class ArmorResistanceHandler : AbstractDamageHandler
{
    protected override void ProcessRequest(DamageRequest request)
    {
        // Skip if damage already evaded
        if (request.IsEvaded)
        {
            Debug.Log($"[ArmorResistanceHandler] Skipping armor check because damage was evaded.");
            return;
        }

        Debug.Log($"[ArmorResistanceHandler] Processing damage request for {request.Target.name}.");
        ArmorComponent armor = request.Target.GetComponent<ArmorComponent>();
        if (armor != null)
        {
            float reducedDamage = armor.CalculateReducedDamage(request.Type, request.CurrentDamage);
            float damageReduction = request.CurrentDamage - reducedDamage;
            if (damageReduction > 0)
            {
                request.CurrentDamage = reducedDamage;
                request.Log($"Armor/Resistance reduced damage by {damageReduction:F2}. Current damage: {request.CurrentDamage:F2}");
            }
            else
            {
                request.Log("Damage not reduced by armor/resistance.");
            }
        }
    }
}

/// <summary>
/// Handles applying status effects based on damage type or other conditions.
/// </summary>
public class StatusEffectHandler : AbstractDamageHandler
{
    protected override void ProcessRequest(DamageRequest request)
    {
        // Status effects might still apply even if damage is 0 (e.g., 'chilled' on evaded ice attack).
        // Decide based on game design if this handler should run if damage is evaded.
        Debug.Log($"[StatusEffectHandler] Processing damage request for {request.Target.name}.");

        if (request.Type == DamageType.Fire)
        {
            request.AppliedStatusEffects.Add("Burn");
            request.Log($"{request.Target.name} is now Burning!");
        }
        if (request.Type == DamageType.Poison && request.CurrentDamage > 0)
        {
            request.AppliedStatusEffects.Add("Poisoned");
            request.Log($"{request.Target.name} is now Poisoned!");
        }
        if (request.IsCritical && request.Type == DamageType.Physical)
        {
            request.AppliedStatusEffects.Add("Stun (Minor)");
            request.Log($"{request.Target.name} received a minor Stun from critical physical hit!");
        }
    }
}

/// <summary>
/// The final handler in the chain, responsible for actually applying the damage to the target's HealthComponent.
/// It also marks the request as fully processed.
/// </summary>
public class FinalDamageApplicationHandler : AbstractDamageHandler
{
    protected override void ProcessRequest(DamageRequest request)
    {
        Debug.Log($"[FinalDamageApplicationHandler] Applying final damage for {request.Target.name}.");
        HealthComponent health = request.Target.GetComponent<HealthComponent>();
        if (health != null)
        {
            if (request.CurrentDamage > 0)
            {
                health.TakeDamage(request.CurrentDamage);
                request.Log($"Final damage of {request.CurrentDamage:F2} applied to {request.Target.name}'s health.");
            }
            else if (request.IsEvaded)
            {
                request.Log($"No damage applied to {request.Target.name} due to evasion.");
            }
            else
            {
                request.Log($"Damage was reduced to 0, no damage applied to {request.Target.name}.");
            }
        }
        else
        {
            request.Log($"Error: {request.Target.name} does not have a HealthComponent!");
        }

        // IMPORTANT: Mark the request as fully processed. This will stop the chain
        // from attempting to pass to a non-existent next handler or re-processing.
        request.IsDamageProcessedAndApplied = true;
    }
}

// =====================================================================================================================
// 4. Mock Unity Components (for Attacker and Target)
// These components simulate actual game mechanics that handlers would interact with.
// =====================================================================================================================

/// <summary>
/// A mock health component for game objects.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"{gameObject.name} Health: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"{gameObject.name} took {amount:F2} damage and was defeated!");
            // Potentially trigger death animation/logic here
        }
        else
        {
            Debug.Log($"{gameObject.name} took {amount:F2} damage. Remaining Health: {currentHealth:F2}");
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"{gameObject.name} healed for {amount:F2}. Current Health: {currentHealth:F2}");
    }
}

/// <summary>
/// A mock evasion component for target game objects.
/// </summary>
public class EvasionComponent : MonoBehaviour
{
    [Range(0f, 1f)]
    public float evasionChance = 0.2f; // 20% chance to evade

    public bool TryEvade()
    {
        return Random.value < evasionChance;
    }
}

/// <summary>
/// A mock armor component for target game objects.
/// </summary>
public class ArmorComponent : MonoBehaviour
{
    public float physicalArmor = 10f;
    public float magicalResistance = 5f;
    public float fireResistance = 2f;
    public float iceResistance = 2f;
    public float poisonResistance = 1f;

    /// <summary>
    /// Calculates damage after reduction based on armor/resistance and damage type.
    /// </summary>
    /// <param name="damageType">The type of damage being dealt.</param>
    /// <param name="incomingDamage">The raw incoming damage.</param>
    /// <returns>The damage after reduction.</returns>
    public float CalculateReducedDamage(DamageType damageType, float incomingDamage)
    {
        float defense = 0;
        switch (damageType)
        {
            case DamageType.Physical:
                defense = physicalArmor;
                break;
            case DamageType.Magical:
                defense = magicalResistance;
                break;
            case DamageType.Fire:
                defense = fireResistance;
                break;
            case DamageType.Ice:
                defense = iceResistance;
                break;
            case DamageType.Poison:
                defense = poisonResistance;
                break;
        }

        // Simple damage reduction formula: Damage = Damage * (1 - (Defense / (Defense + 100)))
        // This means 100 defense halves damage, 200 reduces by 2/3, etc. Max 100% reduction as defense -> infinity.
        float reductionFactor = defense / (defense + 100f);
        float reducedDamage = incomingDamage * (1f - reductionFactor);

        // Ensure damage doesn't go below zero due to high defense
        return Mathf.Max(0, reducedDamage);
    }
}

/// <summary>
/// A mock critical hit chance component for attacker game objects.
/// </summary>
public class CritChanceComponent : MonoBehaviour
{
    [Range(0f, 1f)]
    public float critChance = 0.15f; // 15% chance for a critical hit
    public float critMultiplier = 1.5f; // 1.5x damage on critical hit

    public bool TryCrit()
    {
        return Random.value < critChance;
    }
}


// =====================================================================================================================
// 5. Client (MonoBehaviour for Unity Scene)
// This MonoBehaviour sets up the chain and initiates damage requests.
// =====================================================================================================================

/// <summary>
/// The client class responsible for setting up the chain of responsibility and initiating damage requests.
/// This will be a MonoBehaviour in your Unity scene.
/// </summary>
public class ChainOfResponsibilityDamageSystem : MonoBehaviour
{
    [Header("Assign GameObjects with Health, Armor, Evasion, CritChance Components")]
    public GameObject attackerPrefab;
    public GameObject targetPrefab;

    private IDamageHandler _firstHandler;
    private GameObject _attackerInstance;
    private GameObject _targetInstance;

    void Awake()
    {
        // 1. Instantiate the mock game objects
        _attackerInstance = Instantiate(attackerPrefab, new Vector3(-2, 0, 0), Quaternion.identity);
        _attackerInstance.name = attackerPrefab.name + " (Instance)";
        _targetInstance = Instantiate(targetPrefab, new Vector3(2, 0, 0), Quaternion.identity);
        _targetInstance.name = targetPrefab.name + " (Instance)";

        // 2. Build the Chain of Responsibility
        // The order of handlers is crucial and defines the processing flow.
        // For damage: Evasion -> Critical Hit -> Armor/Resistance -> Status Effects -> Final Application
        Debug.Log("--- Building Damage Processing Chain ---");
        _firstHandler = new EvasionHandler();
        _firstHandler
            .SetNext(new CriticalHitHandler())
            .SetNext(new ArmorResistanceHandler())
            .SetNext(new StatusEffectHandler())
            .SetNext(new FinalDamageApplicationHandler()); // This is the last handler, applies damage
        Debug.Log("--- Chain Built ---");
    }

    void Start()
    {
        // 3. Initiate various damage requests to demonstrate the chain
        Debug.Log("\n--- Initiating Damage Scenarios ---");

        InitiateDamage(_attackerInstance, _targetInstance, 30f, DamageType.Physical);
        InitiateDamage(_attackerInstance, _targetInstance, 20f, DamageType.Fire);
        InitiateDamage(_attackerInstance, _targetInstance, 50f, DamageType.Magical);
        InitiateDamage(_attackerInstance, _targetInstance, 10f, DamageType.Poison);
        InitiateDamage(_attackerInstance, _targetInstance, 40f, DamageType.Physical); // Another physical hit

        // Example: Target dies
        HealthComponent targetHealth = _targetInstance.GetComponent<HealthComponent>();
        if (targetHealth != null)
        {
            targetHealth.currentHealth = 5f; // Set health low for a quick defeat
            InitiateDamage(_attackerInstance, _targetInstance, 20f, DamageType.Fire);
        }
    }

    /// <summary>
    /// Creates a DamageRequest and sends it to the first handler in the chain.
    /// </summary>
    /// <param name="source">The GameObject inflicting damage.</param>
    /// <param name="target">The GameObject receiving damage.</param>
    /// <param name="baseDamage">The initial damage value.</param>
    /// <param name="damageType">The type of damage.</param>
    public void InitiateDamage(GameObject source, GameObject target, float baseDamage, DamageType damageType)
    {
        if (_firstHandler == null)
        {
            Debug.LogError("Damage processing chain is not initialized!");
            return;
        }

        DamageRequest request = new DamageRequest(source, target, baseDamage, damageType);
        _firstHandler.Handle(request); // Start the chain!

        // After the chain has completed, log the full summary.
        Debug.Log(request.GetSummary());
    }
}

```

### How to Use in Unity:

1.  **Create a new C# script** named `ChainOfResponsibilityDamageSystem` and copy the entire code above into it.
2.  **Create an empty GameObject** in your scene (e.g., named "GameManager").
3.  **Attach the `ChainOfResponsibilityDamageSystem` script** to this "GameManager" GameObject.
4.  **Create two more empty GameObjects**:
    *   Rename one to "Attacker".
    *   Rename the other to "Target".
5.  **Configure "Attacker" GameObject:**
    *   Add `HealthComponent` to it. (Even attackers have health in most games.)
    *   Add `CritChanceComponent` to it. Adjust `critChance` and `critMultiplier` as desired.
6.  **Configure "Target" GameObject:**
    *   Add `HealthComponent` to it. Adjust `maxHealth` as desired.
    *   Add `EvasionComponent` to it. Adjust `evasionChance` as desired.
    *   Add `ArmorComponent` to it. Adjust `physicalArmor`, `magicalResistance`, etc., as desired.
7.  **Assign Prefabs in the Inspector:**
    *   Select your "GameManager" GameObject.
    *   In the `ChainOfResponsibilityDamageSystem` component in the Inspector, drag your "Attacker" GameObject into the `Attacker Prefab` slot.
    *   Drag your "Target" GameObject into the `Target Prefab` slot.
    *   *Note*: The script will instantiate copies of these GameObjects at `Awake()`. If you want to use existing scene objects directly, you could modify `Awake()` to use `GetComponent` instead of `Instantiate`. For this example, instantiating fresh copies each run is simpler for demonstration.
8.  **Run the Unity Scene!** Observe the `Debug.Log` output in the Console window. You'll see the damage processing chain in action, including evasion, critical hits, armor reduction, status effect application, and final damage.

This setup provides a clear, practical, and educational example of the Chain of Responsibility pattern applied to a common game development scenario.