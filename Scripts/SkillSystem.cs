// Unity Design Pattern Example: SkillSystem
// This script demonstrates the SkillSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

Here's a complete, practical C# Unity example demonstrating the 'SkillSystem' design pattern. This script is designed to be educational, self-contained, and ready to drop into a Unity project.

It includes:
*   **ScriptableObjects** for defining skill data (easily created by designers).
*   **Runtime Skill Instances** to manage cooldowns and cast progress.
*   A **Skill User** component that can learn, manage, and cast skills.
*   A **Skill Effect Target** component for entities that can be affected by skills.
*   Examples of different skill types (Damage, Heal).
*   Resource management (Mana/Energy).
*   Cast times and cooldowns handled with Coroutines.
*   Detailed comments explaining the pattern and implementation.

---

**How to Use This Example in Unity:**

1.  **Create a new C# Script:** Name it `SkillSystemExample` and copy-paste the entire code below into it.
2.  **Create Skill ScriptableObjects:**
    *   In your Unity Project window, right-click -> Create -> Skills -> Damage Skill.
    *   Name it "Fireball" or "BasicAttack". Set `Damage Amount`, `Cast Time`, `Cooldown`, `Resource Cost`.
    *   Right-click -> Create -> Skills -> Heal Skill.
    *   Name it "Heal" or "Bandage". Set `Heal Amount`, `Cast Time`, `Cooldown`, `Resource Cost`.
3.  **Create a Player GameObject:**
    *   Create an empty GameObject (e.g., named "Player").
    *   Add the `SkillUser` component to it.
    *   Drag and drop your created "Fireball" and "Heal" ScriptableObjects into the `Initial Learned Skills` list on the `SkillUser` component.
4.  **Create a Target GameObject:**
    *   Create another empty GameObject (e.g., named "Enemy" or "Ally").
    *   Add the `SkillEffectTarget` component to it.
    *   Adjust its `Max Health` if desired.
5.  **Run the Scene:**
    *   The `SkillUser` on the "Player" will automatically try to cast "Fireball" at the "Enemy" (if an enemy is assigned) and "Heal" on itself every few seconds.
    *   Observe the Console window for output indicating skill usage, cooldowns, resources, damage, and healing.
    *   You can assign the `TargetForSkill1` and `TargetForSkill2` fields on the Player's `SkillUser` component in the Inspector to point to your "Enemy" or "Ally" GameObjects.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ========================================================================================
// 1. SkillEffectTarget - Represents an entity that can be affected by skills.
//    (e.g., has health, can receive buffs/debuffs)
// ========================================================================================
/// <summary>
/// A MonoBehaviour component that represents an entity in the game world
/// capable of being targeted and affected by skills.
/// This includes having health, taking damage, and receiving healing.
/// </summary>
public class SkillEffectTarget : MonoBehaviour
{
    [Header("Target Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to this target.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void ApplyDamage(float amount)
    {
        if (!IsAlive) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // Health can't go below zero.
        Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} has been defeated!");
            // Potentially trigger death animation, disable GameObject, etc.
        }
    }

    /// <summary>
    /// Applies healing to this target.
    /// </summary>
    /// <param name="amount">The amount of healing to apply.</param>
    public void ApplyHeal(float amount)
    {
        if (!IsAlive) return; // Can't heal a defeated target in this simple example.

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Health can't exceed max health.
        Debug.Log($"{gameObject.name} healed for {amount}. Current Health: {currentHealth}/{maxHealth}");
    }

    // You could extend this for buffs, debuffs, status effects, etc.
    // public void ApplyBuff(BuffData buff) { /* ... */ }
}

// ========================================================================================
// 2. BaseSkillSO - Abstract ScriptableObject for defining common skill properties.
// ========================================================================================
/// <summary>
/// <para>Base abstract class for all skill definitions in the game.</para>
/// <para>This is a ScriptableObject, meaning skill data can be created and configured
/// directly in the Unity Editor without writing code.</para>
/// <para>It defines common properties like name, description, cast time, cooldown,
/// and resource cost.</para>
/// </summary>
public abstract class BaseSkillSO : ScriptableObject
{
    [Header("Basic Skill Info")]
    public string skillName = "New Skill";
    [TextArea(3, 5)] public string description = "A generic skill.";
    public Sprite icon; // Optional: for UI display

    [Header("Skill Mechanics")]
    public float castTime = 0.5f; // Time until the skill's effect occurs
    public float cooldown = 5f;   // Time until the skill can be used again after activation
    public float resourceCost = 10f; // e.g., Mana, Energy, Stamina

    /// <summary>
    /// Checks if the skill can be potentially used by the caster on the target,
    /// based on static skill properties (e.g., target type, range - though range
    /// is not implemented in this basic example).
    /// Dynamic checks like cooldowns and resources are handled by SkillInstance/SkillUser.
    /// </summary>
    /// <param name="caster">The entity attempting to cast the skill.</param>
    /// <param name="target">The intended target of the skill.</param>
    /// <returns>True if the skill can be used on this target, false otherwise.</returns>
    public virtual bool CanUse(SkillUser caster, SkillEffectTarget target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{skillName}: No target provided.");
            return false;
        }
        // Basic check: is the target alive?
        if (!target.IsAlive)
        {
            Debug.LogWarning($"{skillName}: Target {target.name} is not alive.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// <para>The core method where the skill's effect is implemented.</para>
    /// <para>This method is called AFTER the cast time has elapsed and all checks pass.</para>
    /// <para>Derived classes must override this to define their specific behavior.</para>
    /// </summary>
    /// <param name="caster">The SkillUser component that initiated the skill.</param>
    /// <param name="target">The SkillEffectTarget component that is the primary target.</param>
    public abstract void Execute(SkillUser caster, SkillEffectTarget target);
}

// ========================================================================================
// 3. Concrete Skill Definitions (Derived ScriptableObjects)
// ========================================================================================

/// <summary>
/// A concrete skill definition for a damage-dealing skill.
/// Inherits from BaseSkillSO and adds a specific damage amount.
/// </summary>
[CreateAssetMenu(fileName = "NewDamageSkill", menuName = "Skills/Damage Skill")]
public class DamageSkillSO : BaseSkillSO
{
    [Header("Damage Skill Specifics")]
    public float damageAmount = 10f;

    /// <summary>
    /// Executes the damage skill by applying damage to the target.
    /// </summary>
    /// <param name="caster">The caster of the skill.</param>
    /// <param name="target">The target to receive damage.</param>
    public override void Execute(SkillUser caster, SkillEffectTarget target)
    {
        if (target != null && target.IsAlive)
        {
            Debug.Log($"<color=red>{caster.name} used {skillName} on {target.name}, dealing {damageAmount} damage!</color>");
            target.ApplyDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning($"{skillName} failed to execute: target invalid or dead.");
        }
    }
}

/// <summary>
/// A concrete skill definition for a healing skill.
/// Inherits from BaseSkillSO and adds a specific heal amount.
/// </summary>
[CreateAssetMenu(fileName = "NewHealSkill", menuName = "Skills/Heal Skill")]
public class HealSkillSO : BaseSkillSO
{
    [Header("Heal Skill Specifics")]
    public float healAmount = 15f;

    /// <summary>
    /// Executes the healing skill by applying healing to the target.
    /// </summary>
    /// <param name="caster">The caster of the skill.</param>
    /// <param name="target">The target to receive healing.</param>
    public override void Execute(SkillUser caster, SkillEffectTarget target)
    {
        if (target != null && target.IsAlive)
        {
            Debug.Log($"<color=green>{caster.name} used {skillName} on {target.name}, healing for {healAmount}!</color>");
            target.ApplyHeal(healAmount);
        }
        else
        {
            Debug.LogWarning($"{skillName} failed to execute: target invalid or dead.");
        }
    }
}

// You could add more skill types:
/*
[CreateAssetMenu(fileName = "NewBuffSkill", menuName = "Skills/Buff Skill")]
public class BuffSkillSO : BaseSkillSO
{
    public float buffDuration = 10f;
    public float statIncrease = 5f; // e.g., +5 attack
    public override void Execute(SkillUser caster, SkillEffectTarget target)
    {
        Debug.Log($"{caster.name} used {skillName} on {target.name}, granting a buff for {buffDuration} seconds!");
        // target.ApplyBuff(this); // Would need a BuffData class and apply logic on target
    }
}
*/

// ========================================================================================
// 4. SkillInstance - Represents a runtime instance of a skill for a specific user.
//    Manages dynamic state like cooldowns. This is NOT a MonoBehaviour or ScriptableObject.
// ========================================================================================
/// <summary>
/// <para>A runtime instance of a skill. This class wraps a BaseSkillSO and manages
/// dynamic state such as current cooldown.</para>
/// <para>It's a plain C# class, not a MonoBehaviour or ScriptableObject, because
/// it's specific to a character's current state.</para>
/// </summary>
public class SkillInstance
{
    public BaseSkillSO SkillDefinition { get; private set; }
    public float CurrentCooldown { get; private set; }
    public bool IsOnCooldown => CurrentCooldown > 0;
    public float CurrentCastProgress { get; private set; } // For visual feedback, not strictly used in logic here

    public SkillInstance(BaseSkillSO skillDefinition)
    {
        SkillDefinition = skillDefinition;
        CurrentCooldown = 0f;
        CurrentCastProgress = 0f;
    }

    /// <summary>
    /// Reduces the current cooldown by a given delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public void TickCooldown(float deltaTime)
    {
        if (IsOnCooldown)
        {
            CurrentCooldown -= deltaTime;
            if (CurrentCooldown < 0)
            {
                CurrentCooldown = 0;
            }
        }
    }

    /// <summary>
    /// Puts the skill on cooldown based on its definition.
    /// </summary>
    public void StartCooldown()
    {
        CurrentCooldown = SkillDefinition.cooldown;
        Debug.Log($"Skill '{SkillDefinition.skillName}' started cooldown for {SkillDefinition.cooldown} seconds.");
    }

    /// <summary>
    /// Resets the cast progress (e.g., if cast is interrupted).
    /// </summary>
    public void ResetCast()
    {
        CurrentCastProgress = 0f;
    }

    // You could also add methods like:
    // public void StartCast() { CurrentCastProgress = 0f; }
    // public void AdvanceCast(float deltaTime) { CurrentCastProgress += deltaTime; }
    // public bool IsCasting => CurrentCastProgress > 0 && CurrentCastProgress < SkillDefinition.castTime;
}


// ========================================================================================
// 5. SkillUser - The MonoBehaviour component that manages a character's skills.
// ========================================================================================
/// <summary>
/// <para>A MonoBehaviour component that allows a GameObject (e.g., Player, Enemy)
/// to learn, manage, and cast skills.</para>
/// <para>It handles resources (mana/energy), initiates cast times, and triggers
/// skill execution.</para>
/// </summary>
public class SkillUser : MonoBehaviour
{
    [Header("Skill User Resources")]
    [SerializeField] private float maxResource = 100f;
    [SerializeField] private float currentResource;
    [SerializeField] private float resourceRegenRate = 5f; // Resource regenerated per second

    [Header("Learned Skills")]
    [Tooltip("Drag the ScriptableObject skill definitions here to start with.")]
    [SerializeField] private List<BaseSkillSO> initialLearnedSkills = new List<BaseSkillSO>();

    [Header("Demo/Testing")]
    [SerializeField] private float autoCastDelay = 3f; // How often to try to cast skills
    [SerializeField] private SkillEffectTarget targetForSkill1;
    [SerializeField] private SkillEffectTarget targetForSkill2;

    private List<SkillInstance> learnedSkillInstances = new List<SkillInstance>();
    private Coroutine currentCastCoroutine;
    private Coroutine cooldownTickCoroutine;
    private float autoCastTimer;

    public float CurrentResource => currentResource;
    public float MaxResource => maxResource;
    public string Name => gameObject.name;

    void Awake()
    {
        currentResource = maxResource;

        // Learn initial skills provided in the inspector
        foreach (BaseSkillSO skillSO in initialLearnedSkills)
        {
            LearnSkill(skillSO);
        }

        // Start the global cooldown ticking coroutine for all skills
        cooldownTickCoroutine = StartCoroutine(TickAllSkillCooldowns());
    }

    void Update()
    {
        // Simple resource regeneration
        currentResource = Mathf.Min(currentResource + resourceRegenRate * Time.deltaTime, maxResource);

        // --- DEMO USAGE ---
        autoCastTimer -= Time.deltaTime;
        if (autoCastTimer <= 0)
        {
            autoCastTimer = autoCastDelay;
            // Try to cast the first learned skill (e.g., Fireball)
            if (learnedSkillInstances.Count > 0)
            {
                TryCastSkill(learnedSkillInstances[0], targetForSkill1);
            }
            // Try to cast the second learned skill (e.g., Heal)
            if (learnedSkillInstances.Count > 1)
            {
                TryCastSkill(learnedSkillInstances[1], targetForSkill2 != null ? targetForSkill2 : GetComponent<SkillEffectTarget>()); // Heal self if no specific target
            }
        }
        // --- END DEMO USAGE ---
    }

    /// <summary>
    /// Adds a new skill definition to this user's learned skills.
    /// Creates a runtime SkillInstance from the ScriptableObject definition.
    /// </summary>
    /// <param name="skillDefinition">The ScriptableObject defining the skill.</param>
    public void LearnSkill(BaseSkillSO skillDefinition)
    {
        if (skillDefinition == null)
        {
            Debug.LogError("Attempted to learn a null skill definition.");
            return;
        }

        // Prevent learning the same skill definition multiple times (optional, depending on game design)
        foreach (SkillInstance skill in learnedSkillInstances)
        {
            if (skill.SkillDefinition == skillDefinition)
            {
                Debug.LogWarning($"{Name} already knows skill: {skillDefinition.skillName}");
                return;
            }
        }

        SkillInstance newSkillInstance = new SkillInstance(skillDefinition);
        learnedSkillInstances.Add(newSkillInstance);
        Debug.Log($"{Name} learned new skill: {skillDefinition.skillName}");
    }

    /// <summary>
    /// Attempts to cast a specific skill. This is the main public entry point
    /// for initiating skill usage.
    /// </summary>
    /// <param name="skillInstance">The runtime instance of the skill to cast.</param>
    /// <param name="target">The target for the skill's effect.</param>
    /// <returns>True if the skill cast was initiated (or executed instantly), false otherwise.</returns>
    public bool TryCastSkill(SkillInstance skillInstance, SkillEffectTarget target)
    {
        if (skillInstance == null || skillInstance.SkillDefinition == null)
        {
            Debug.LogError($"{Name}: Attempted to cast a null or undefined skill.");
            return false;
        }

        BaseSkillSO skillSO = skillInstance.SkillDefinition;

        // 1. Basic checks from SkillDefinition
        if (!skillSO.CanUse(this, target))
        {
            Debug.Log($"{Name}: Cannot use {skillSO.skillName} (definition checks failed).");
            return false;
        }

        // 2. Check if the skill is on cooldown
        if (skillInstance.IsOnCooldown)
        {
            Debug.Log($"{Name}: {skillSO.skillName} is on cooldown. ({skillInstance.CurrentCooldown:F1}s remaining)");
            return false;
        }

        // 3. Check resources
        if (currentResource < skillSO.resourceCost)
        {
            Debug.Log($"{Name}: Not enough resource to cast {skillSO.skillName}. (Need {skillSO.resourceCost}, have {currentResource:F1})");
            return false;
        }

        // If all checks pass, start the casting process
        // Stop any previous cast if a new one is initiated (optional, depends on game design)
        if (currentCastCoroutine != null)
        {
            StopCoroutine(currentCastCoroutine);
            Debug.Log($"{Name}: Current cast interrupted by new skill attempt.");
            // Reset cast state of the previously casting skill if needed.
        }

        currentCastCoroutine = StartCoroutine(CastSkillWithDelay(skillInstance, target));
        return true;
    }

    /// <summary>
    /// Coroutine to handle skill casting with a delay (cast time).
    /// </summary>
    /// <param name="skillInstance">The skill instance being cast.</param>
    /// <param name="target">The target for the skill.</param>
    private IEnumerator CastSkillWithDelay(SkillInstance skillInstance, SkillEffectTarget target)
    {
        BaseSkillSO skillSO = skillInstance.SkillDefinition;

        Debug.Log($"{Name} begins casting {skillSO.skillName}... ({skillSO.castTime:F1}s cast time)");

        // Simulate casting animation/delay
        float timer = 0f;
        while (timer < skillSO.castTime)
        {
            // Update cast progress (for UI bars, etc.)
            skillInstance.CurrentCastProgress = timer / skillSO.castTime;
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        skillInstance.ResetCast(); // Cast complete, reset progress

        // After cast time, re-check some conditions before execution (e.g., target still valid, still enough resource)
        // This prevents wasting mana if target dies during cast
        if (!skillSO.CanUse(this, target))
        {
            Debug.Log($"{Name}: {skillSO.skillName} fizzled after cast time: target invalid.");
            // Don't apply cooldown or spend resource if it fizzled
            yield break;
        }
        if (currentResource < skillSO.resourceCost)
        {
            Debug.Log($"{Name}: {skillSO.skillName} fizzled after cast time: not enough resource anymore.");
            yield break;
        }

        // Spend resource and activate cooldown
        SpendResource(skillSO.resourceCost);
        skillInstance.StartCooldown();

        // Execute the skill's actual effect
        skillSO.Execute(this, target);

        currentCastCoroutine = null; // Clear the coroutine reference
    }

    /// <summary>
    /// Spends a specified amount of resource (e.g., mana).
    /// </summary>
    /// <param name="amount">The amount of resource to spend.</param>
    private void SpendResource(float amount)
    {
        currentResource -= amount;
        currentResource = Mathf.Max(currentResource, 0); // Resource can't go below zero.
        Debug.Log($"{Name} spent {amount} resource. Current Resource: {currentResource:F1}/{maxResource:F1}");
    }

    /// <summary>
    /// Coroutine that continuously ticks down the cooldowns of all learned skills.
    /// This runs once per SkillUser and updates all skills.
    /// </summary>
    private IEnumerator TickAllSkillCooldowns()
    {
        while (true)
        {
            foreach (SkillInstance skill in learnedSkillInstances)
            {
                skill.TickCooldown(Time.deltaTime);
            }
            yield return null; // Wait for the next frame
        }
    }

    /// <summary>
    /// Gets a read-only list of the currently learned skill instances.
    /// </summary>
    public IReadOnlyList<SkillInstance> GetLearnedSkills()
    {
        return learnedSkillInstances.AsReadOnly();
    }
}
```