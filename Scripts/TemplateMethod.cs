// Unity Design Pattern Example: TemplateMethod
// This script demonstrates the TemplateMethod pattern in Unity
// Generated automatically - ready to use in your Unity project

The Template Method is a behavioral design pattern that defines the skeletal structure of an algorithm in a base class, but defers some steps to subclasses. It lets subclasses redefine certain steps of an algorithm without changing the algorithm's structure.

This Unity example demonstrates a common game development scenario: **Character Ability Execution**. Different abilities (like a melee attack, a heal spell, or a buff) follow a similar sequence of steps (e.g., check conditions, play animation, apply effect, play sound), but the "apply effect" step is unique to each ability.

---

### Key Components of the Template Method Pattern:

1.  **`CharacterAbility` (AbstractClass):**
    *   Defines the `ExecuteAbility()` method, which is the **Template Method**. This method orchestrates the entire process, calling a sequence of steps. It should be non-overridable (public, not virtual).
    *   Defines abstract methods (e.g., `ApplySpecificEffect()`) that *must* be implemented by concrete subclasses. These are the "varying" steps.
    *   Defines concrete methods (e.g., `CheckConditions()`, `PlayAnimation()`, `PlaySoundEffect()`) that are implemented in the base class and shared by all subclasses. These are the "common" steps.
    *   Defines **Hook Methods** (e.g., `PostExecutionHook()`), which are virtual methods with an empty or default implementation. Subclasses can optionally override these to inject specific behavior at certain points without being forced to.

2.  **`MeleeAttackAbility`, `HealAbility`, `BuffAbility` (ConcreteClasses):**
    *   Each subclass implements the abstract methods defined in `CharacterAbility` to provide its specific functionality (e.g., how to apply damage, heal, or buff).
    *   They can also optionally override hook methods to add extra behavior.

3.  **`TemplateMethodDemonstrator` (Client/Context):**
    *   This `MonoBehaviour` acts as the client. It holds references to `CharacterAbility` objects (the abstract type).
    *   It calls the `ExecuteAbility()` template method on these objects. The client does not need to know the concrete type of the ability; it only interacts with the abstract interface, letting the pattern handle the varied steps.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For generic collections like List, if needed

// --- 1. Abstract Base Class (AbstractClass) ---
/// <summary>
/// The abstract base class that defines the 'template method' for executing a character ability.
/// It orchestrates the steps of an ability's execution, some of which are implemented here (common steps),
/// and some are left for subclasses to implement (varying steps).
/// </summary>
public abstract class CharacterAbility : MonoBehaviour
{
    // Properties common to all abilities, accessible in the Inspector for subclasses.
    [SerializeField] protected string abilityName = "Default Ability";
    [SerializeField] protected float cooldownTime = 1.0f;
    [SerializeField] protected GameObject particleEffectPrefab; // Optional visual effect
    [SerializeField] protected AudioClip soundEffectClip; // Optional audio effect

    private bool isOnCooldown = false;
    private float lastExecutedTime = -Mathf.Infinity;

    // --- The Template Method ---
    /// <summary>
    /// This is the central 'template method'. It defines the skeletal structure of the ability execution algorithm.
    /// It calls a sequence of steps, some implemented here and some delegated to subclasses.
    /// This method should NOT be overridden by subclasses to maintain the algorithm's structure.
    /// </summary>
    public void ExecuteAbility()
    {
        Debug.Log($"--- Starting {abilityName} Execution ---");

        // Step 1: Pre-conditions check (Common step, implemented here)
        // This step is mandatory for all abilities.
        if (!CheckConditions())
        {
            Debug.Log($"<color=orange>{abilityName}: Conditions not met. Aborting execution.</color>");
            return;
        }

        // Step 2: Play animation (Common step, implemented here)
        // All abilities play a generic animation, but subclasses could override this if needed (e.g., with 'virtual').
        PlayAnimation();

        // Step 3: Apply specific effect (Varying step, delegated to subclasses)
        // This is the core "hook" that makes the Template Method pattern powerful.
        // Subclasses MUST provide an implementation for this abstract method.
        ApplySpecificEffect();

        // Step 4: Play sound effect (Common step, implemented here)
        // All abilities play a sound if one is assigned.
        PlaySoundEffect();

        // Step 5: Post-execution actions (Hook method, optional for subclasses to override)
        // This is a "hook" method - it has a default (empty) implementation
        // that subclasses can optionally override to add custom behavior at this point.
        PostExecutionHook();

        Debug.Log($"--- {abilityName} Execution Finished ---");
    }

    /// <summary>
    /// Common step: Checks if the ability can be executed.
    /// Includes a cooldown check. This can be overridden by subclasses if they need
    /// a completely different pre-condition logic, but the base provides a useful default.
    /// </summary>
    protected virtual bool CheckConditions()
    {
        if (Time.time < lastExecutedTime + cooldownTime)
        {
            Debug.Log($"<color=red>{abilityName}: Still on cooldown for {Mathf.Max(0, lastExecutedTime + cooldownTime - Time.time):F1} more seconds.</color>");
            return false;
        }

        // In a real game, you might add checks for mana, stamina, target availability, etc.
        // E.g., if (!HasEnoughMana(manaCost)) return false;
        // E.g., if (!TargetIsValid(target)) return false;

        Debug.Log($"<color=green>{abilityName}: Conditions met. Ready to execute.</color>");
        lastExecutedTime = Time.time; // Reset cooldown
        return true;
    }

    /// <summary>
    /// Common step: Plays a generic ability animation.
    /// In a real game, this might trigger an Animator component or play a specific animation clip.
    /// This is virtual, so subclasses *could* override it if their animation logic significantly differs.
    /// </summary>
    protected virtual void PlayAnimation()
    {
        Debug.Log($"<color=cyan>{abilityName}: Playing generic ability animation.</color>");
        // Example: characterAnimator.Play("AbilityCast");
    }

    /// <summary>
    /// **Abstract step:** This is the method that defines the unique, varying behavior of each specific ability.
    /// Subclasses MUST implement this method to provide their distinct effect (e.g., apply damage, heal, buff).
    /// </summary>
    protected abstract void ApplySpecificEffect();

    /// <summary>
    /// Common step: Plays a specific sound effect for the ability, if one is assigned.
    /// </summary>
    protected virtual void PlaySoundEffect()
    {
        if (soundEffectClip != null)
        {
            Debug.Log($"<color=yellow>{abilityName}: Playing sound effect: {soundEffectClip.name}</color>");
            // Example: AudioSource.PlayClipAtPoint(soundEffectClip, transform.position);
        }
        else
        {
            Debug.Log($"<color=yellow>{abilityName}: No specific sound effect defined.</color>");
        }
    }

    /// <summary>
    /// Hook method: An optional step that subclasses can override.
    /// Provides a point for subclasses to inject custom behavior *after* the main effect and sound.
    /// Has a default (empty) implementation, so subclasses are not forced to implement it.
    /// </summary>
    protected virtual void PostExecutionHook()
    {
        // Default: Do nothing. Subclasses can override this to add custom final touches.
        Debug.Log($"    -> {abilityName}: No specific post-execution hook behavior defined by subclass (optional).");
    }

    // A helper method to simulate some visual/logical work or effects
    protected void SimulateEffect(string effectDescription)
    {
        Debug.Log($"    -> <color=white>Applying Effect: {effectDescription}</color>");
        if (particleEffectPrefab != null)
        {
            // In a real game: Instantiate(particleEffectPrefab, transform.position, Quaternion.identity);
            Debug.Log($"    -> <color=grey>Instantiating particle effect: {particleEffectPrefab.name}</color>");
        }
    }
}

// --- 2. Concrete Subclasses (ConcreteClass) ---

/// <summary>
/// Concrete implementation for a Melee Attack ability.
/// It implements the 'ApplySpecificEffect' abstract method from CharacterAbility.
/// </summary>
public class MeleeAttackAbility : CharacterAbility
{
    [SerializeField] private float damageAmount = 10f; // Specific property for melee attack

    // Initialize base class properties in Awake for MonoBehaviour consistency.
    void Awake()
    {
        abilityName = "Melee Attack";
        cooldownTime = 0.5f; // Fast cooldown
    }

    /// <summary>
    /// Implements the specific effect for a melee attack: dealing damage.
    /// This fulfills the abstract method requirement from CharacterAbility.
    /// </summary>
    protected override void ApplySpecificEffect()
    {
        SimulateEffect($"Dealing {damageAmount} damage to enemies in close range.");
        // In a real game: Perform raycasts/overlaps to find enemies, apply damage.
    }

    /// <summary>
    /// Optionally overrides the hook method to add custom behavior specific to Melee Attack.
    /// </summary>
    protected override void PostExecutionHook()
    {
        Debug.Log($"    -> <color=magenta>Melee Attack specific: Character lunges forward slightly after attack.</color>");
    }
}

/// <summary>
/// Concrete implementation for a Heal ability.
/// It implements the 'ApplySpecificEffect' abstract method from CharacterAbility.
/// </summary>
public class HealAbility : CharacterAbility
{
    [SerializeField] private float healAmount = 25f; // Specific property for healing

    void Awake()
    {
        abilityName = "Heal Spell";
        cooldownTime = 3.0f; // Moderate cooldown
    }

    /// <summary>
    /// Implements the specific effect for healing: restoring health.
    /// This fulfills the abstract method requirement from CharacterAbility.
    /// </summary>
    protected override void ApplySpecificEffect()
    {
        SimulateEffect($"Restoring {healAmount} health to self or nearby allies.");
        // In a real game: Find allies in range, add health to their HealthComponent.
    }
    // This ability does not override PostExecutionHook, demonstrating its optional nature.
}

/// <summary>
/// Concrete implementation for a Buff ability.
/// It implements the 'ApplySpecificEffect' abstract method from CharacterAbility.
/// </summary>
public class BuffAbility : CharacterAbility
{
    [SerializeField] private float buffDuration = 5f;
    [SerializeField] private float buffStrength = 0.2f; // e.g., 20% damage increase

    void Awake()
    {
        abilityName = "Battle Cry Buff";
        cooldownTime = 10.0f; // Long cooldown
    }

    /// <summary>
    /// Implements the specific effect for a buff: applying a temporary status effect.
    /// This fulfills the abstract method requirement from CharacterAbility.
    /// </summary>
    protected override void ApplySpecificEffect()
    {
        SimulateEffect($"Applying a {buffStrength * 100}% damage buff for {buffDuration} seconds to allies in range.");
        // In a real game: Create a BuffStatus effect, apply to target(s), manage its duration.
    }
}


// --- 3. Demonstrator/Client Class (MonoBehaviour for Unity) ---

/// <summary>
/// This MonoBehaviour demonstrates the Template Method pattern in action.
/// It acts as the client that uses the abstract 'CharacterAbility' interface
/// without needing to know the concrete types of the abilities it's executing.
/// </summary>
public class TemplateMethodDemonstrator : MonoBehaviour
{
    // These fields will be populated in the Unity Editor.
    // We hold references to the abstract type 'CharacterAbility'.
    // The Unity Inspector will allow you to drag any GameObject that has a
    // component derived from CharacterAbility (e.g., MeleeAttackAbility).
    [Header("Abilities to Demonstrate (Assign in Inspector)")]
    [SerializeField] private CharacterAbility meleeAttackAbility;
    [SerializeField] private CharacterAbility healAbility;
    [SerializeField] private CharacterAbility buffAbility;

    void Start()
    {
        Debug.Log("<color=lime>Template Method Demonstration Started!</color>");
        Debug.Log("Press <color=white>Spacebar</color> for Melee Attack, <color=white>H</color> for Heal, <color=white>B</color> for Buff.");
        Debug.Log("Observe how common steps (conditions, animation, sound) are shared, while specific effects vary.");

        // Basic check to ensure abilities are assigned in the Inspector.
        if (meleeAttackAbility == null || healAbility == null || buffAbility == null)
        {
            Debug.LogError("<color=red>ERROR: Please assign all ability types in the Inspector of TemplateMethodDemonstrator.</color>");
            enabled = false; // Disable this script if not properly set up to avoid NullReferenceExceptions.
        }
    }

    void Update()
    {
        // When a key is pressed, we call the template method `ExecuteAbility()`
        // on the respective CharacterAbility object.
        // The client (this script) doesn't care if it's a MeleeAttackAbility or HealAbility;
        // it just knows it's a CharacterAbility and calls its defined execution sequence.
        if (Input.GetKeyDown(KeyCode.Space) && meleeAttackAbility != null)
        {
            meleeAttackAbility.ExecuteAbility();
        }

        if (Input.GetKeyDown(KeyCode.H) && healAbility != null)
        {
            healAbility.ExecuteAbility();
        }

        if (Input.GetKeyDown(KeyCode.B) && buffAbility != null)
        {
            buffAbility.ExecuteAbility();
        }
    }
}

/*
// --- HOW TO USE THIS EXAMPLE IN UNITY ---

1.  **Create a new C# script** named "TemplateMethodDemonstrator".
2.  **Copy and paste ALL the code** above into this new script, replacing its default content.
3.  **Save the script.**

4.  **In your Unity scene:**
    a.  Create an empty GameObject (e.g., name it "AbilityManager").
    b.  Drag the "TemplateMethodDemonstrator" script onto this "AbilityManager" GameObject.

5.  **Set up the Ability Components:**
    a.  Create three **empty Child GameObjects** under "AbilityManager" (e.g., "MeleeAbilityComponent", "HealAbilityComponent", "BuffAbilityComponent").
        (Alternatively, you could add all ability components directly to "AbilityManager" if preferred, but children can help organize if abilities need distinct positions or more complex setups).
    b.  Drag the `MeleeAttackAbility` script onto the "MeleeAbilityComponent" GameObject.
    c.  Drag the `HealAbility` script onto the "HealAbilityComponent" GameObject.
    d.  Drag the `BuffAbility` script onto the "BuffAbilityComponent" GameObject.

6.  **Assign Abilities in the Inspector:**
    a.  Select the "AbilityManager" GameObject.
    b.  In its Inspector, you'll see the "TemplateMethodDemonstrator" component with three slots: "Melee Attack Ability", "Heal Ability", "Buff Ability".
    c.  Drag the "MeleeAbilityComponent" GameObject from the Hierarchy into the "Melee Attack Ability" slot.
    d.  Drag the "HealAbilityComponent" GameObject from the Hierarchy into the "Heal Ability" slot.
    e.  Drag the "BuffAbilityComponent" GameObject from the Hierarchy into the "Buff Ability" slot.

7.  **(Optional: Customize Abilities):**
    a.  Select each child ability GameObject (e.g., "MeleeAbilityComponent").
    b.  In its Inspector, you can customize properties like `Damage Amount`, `Heal Amount`, `Buff Duration`, `Cooldown Time`.
    c.  You can also assign `Particle Effect Prefab` (e.g., a simple particle system you create) and `Sound Effect Clip` (e.g., any audio clip) to see those debug messages. If left empty, the script will simply state that no effect is defined.

8.  **Run the scene!**
    a.  Open the Unity Console window (Window > General > Console).
    b.  Press `Spacebar` to execute the Melee Attack.
    c.  Press `H` to execute the Heal ability.
    d.  Press `B` to execute the Buff ability.

9.  **Observe the Console Output:**
    You will see how each ability consistently performs the `CheckConditions`, `PlayAnimation`, and `PlaySoundEffect` steps (defined in `CharacterAbility`), while dynamically executing its unique `ApplySpecificEffect` and optionally `PostExecutionHook` (defined in its concrete subclass). This vividly demonstrates the Template Method pattern at work.
*/
```