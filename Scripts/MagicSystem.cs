// Unity Design Pattern Example: MagicSystem
// This script demonstrates the MagicSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'MagicSystem' design pattern in Unity. While 'MagicSystem' isn't a formally recognized GoF (Gang of Four) pattern, in game development, it commonly refers to a central **Facade/Manager** that orchestrates various magic-related functionalities, often leveraging other patterns like **Strategy** (for spell behaviors) and **Data-Driven Design** (using ScriptableObjects for spell data). This approach creates a flexible, scalable, and designer-friendly system for managing spells and abilities.

**Core Principles Illustrated:**

1.  **Facade/Manager:** The `MagicSystem` class provides a simplified, unified interface to a complex subsystem (all spell logic, cooldowns, mana checks). Other parts of the game (e.g., player controller, AI) only interact with `MagicSystem.Instance.CastSpell(...)` without needing to know the internal complexities of each spell.
2.  **Strategy Pattern:** Each `SpellDefinition` (and its concrete implementations like `FireballSpell`, `HealSpell`, `BuffSpell`) encapsulates a different algorithm (the spell's effect). The `MagicSystem` executes these strategies polymorphically via the `Execute` method, allowing for easy addition of new spell types.
3.  **Data-Driven Design (ScriptableObjects):** Spells are defined as `ScriptableObject` assets. This allows game designers to create, configure, and balance spells directly in the Unity Editor without modifying code. New spells can be added by simply creating new `ScriptableObject` assets that derive from `SpellDefinition`.
4.  **Observer/Event Pattern:** The `MagicSystem` uses `Actions` (which could easily be `UnityEvents` for Inspector integration) to notify other systems (UI, VFX, SFX, AI) about important spell events (e.g., cast success, failure, effect applied). This decouples these systems from the core casting logic.

---

## 1. `MagicSystem.cs` (The Facade/Manager)

This script manages all registered spells, provides methods to cast them, and handles global events.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action and other System types
using System.Linq; // For LINQ operations if needed, not strictly for basic ops

/// <summary>
/// Defines the core 'MagicSystem' design pattern in Unity.
/// This system acts as a central manager (Facade) for all magic-related operations
/// in the game, such as spell casting, managing known spells, and handling global
/// magic effects. It uses ScriptableObjects for spell definitions (Strategy pattern)
/// to allow for easy content creation and flexible spell behaviors.
/// </summary>
/// <remarks>
/// Pattern Components:
/// 1.  **Facade/Manager:** The MagicSystem class itself provides a simplified interface
///     to a complex subsystem (all spells and their effects).
/// 2.  **Strategy Pattern (via ScriptableObjects):** Each SpellDefinition (and its
///     concrete implementations like FireballSpell) encapsulates a different algorithm
///     (the spell's effect) that can be swapped interchangeably. The MagicSystem
///     executes these strategies.
/// 3.  **Data-Driven Design (via ScriptableObjects):** Spells are defined as
///     ScriptableObjects, allowing game designers to create and modify spells
///     without touching code, and to assign them easily in the Unity editor.
/// 4.  **Observer/Event Pattern (optional but recommended):** The system uses C# events
///     (Actions in this case) to notify other parts of the game (UI, VFX, SFX systems)
///     about spell-related events.
///
/// This setup promotes high cohesion within magic-related logic and low coupling
/// between the MagicSystem and other game systems (e.g., UI, character controllers).
/// </remarks>
[DefaultExecutionOrder(-100)] // Ensures this system initializes before most other scripts
public class MagicSystem : MonoBehaviour
{
    // --- Singleton Pattern for easy global access ---
    // This ensures there's only one instance of the MagicSystem throughout the game.
    public static MagicSystem Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("All spell definitions known to the MagicSystem. Assign ScriptableObject spells here.")]
    [SerializeField] private SpellDefinition[] registeredSpells;

    // A dictionary for quick lookup of spells by their unique ID
    private Dictionary<string, SpellDefinition> _spellDictionary = new Dictionary<string, SpellDefinition>();

    // --- Event System for spell feedback (Observer Pattern) ---
    // These events allow other systems (UI, VFX, SFX, AI) to react to spell activity
    // without directly coupling to the MagicSystem's internal logic.
    public static event Action<SpellDefinition, Character, Character> OnSpellCastAttempted;
    public static event Action<SpellDefinition, Character, Character> OnSpellCastFailed; // e.g., not enough mana, on cooldown
    public static event Action<SpellDefinition, Character, Character> OnSpellCastSuccess;
    public static event Action<SpellDefinition, Character, Character> OnSpellEffectApplied;

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MagicSystem: Another instance of MagicSystem already exists. Destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Uncomment if MagicSystem should persist across scene loads.
            // DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
    }

    /// <summary>
    /// Initializes the MagicSystem by populating its internal spell dictionary.
    /// This should be called once at startup.
    /// </summary>
    private void InitializeSystem()
    {
        _spellDictionary.Clear();
        if (registeredSpells != null)
        {
            foreach (SpellDefinition spell in registeredSpells)
            {
                if (spell == null)
                {
                    Debug.LogWarning("MagicSystem: A null spell definition was found in registeredSpells array. Skipping.");
                    continue;
                }
                if (_spellDictionary.ContainsKey(spell.ID))
                {
                    Debug.LogWarning($"MagicSystem: Duplicate spell ID '{spell.ID}' found for spell '{spell.SpellName}'. " +
                                     $"Only the first instance will be registered.");
                    continue;
                }
                _spellDictionary.Add(spell.ID, spell);
                Debug.Log($"MagicSystem: Registered spell '{spell.SpellName}' with ID '{spell.ID}'.");
            }
        }
        Debug.Log("MagicSystem initialized with " + _spellDictionary.Count + " spells.");
    }

    /// <summary>
    /// Attempts to retrieve a spell definition by its unique ID.
    /// </summary>
    /// <param name="spellID">The unique string ID of the spell.</param>
    /// <param name="spell">Output parameter for the found SpellDefinition.</param>
    /// <returns>True if the spell was found, false otherwise.</returns>
    public bool TryGetSpell(string spellID, out SpellDefinition spell)
    {
        return _spellDictionary.TryGetValue(spellID, out spell);
    }

    /// <summary>
    /// Main method to cast a spell using its ID. This is the primary entry point for
    /// other game systems to interact with magic. It handles checks (mana, cooldown)
    /// and triggers the spell's effect.
    /// </summary>
    /// <param name="spellID">The unique ID of the spell to cast.</param>
    /// <param name="caster">The character attempting to cast the spell.</param>
    /// <param name="target">The character targeted by the spell.</param>
    /// <returns>True if the spell was successfully cast, false otherwise.</returns>
    public bool CastSpell(string spellID, Character caster, Character target)
    {
        if (!TryGetSpell(spellID, out SpellDefinition spell))
        {
            Debug.LogError($"MagicSystem: Attempted to cast unknown spell with ID '{spellID}'.");
            return false;
        }

        return CastSpell(spell, caster, target);
    }

    /// <summary>
    /// Overload for casting a spell directly using its definition object.
    /// </summary>
    /// <param name="spell">The SpellDefinition object to cast.</param>
    /// <param name="caster">The character attempting to cast the spell.</param>
    /// <param name="target">The character targeted by the spell.</param>
    /// <returns>True if the spell was successfully cast, false otherwise.</returns>
    public bool CastSpell(SpellDefinition spell, Character caster, Character target)
    {
        if (spell == null || caster == null || target == null)
        {
            Debug.LogError("MagicSystem: CastSpell called with null spell, caster, or target.");
            return false;
        }

        OnSpellCastAttempted?.Invoke(spell, caster, target);

        // --- Pre-cast checks (mana, cooldowns) ---
        // The Character class manages its own mana and cooldowns, keeping MagicSystem cleaner.
        if (!caster.CanCast(spell))
        {
            Debug.Log($"MagicSystem: {caster.name} failed to cast {spell.SpellName}. (e.g., not enough mana or on cooldown)");
            OnSpellCastFailed?.Invoke(spell, caster, target);
            return false;
        }

        // --- Deduct mana and apply cooldowns ---
        caster.ConsumeMana(spell.ManaCost);
        caster.ApplyCooldown(spell);

        // --- Execute the spell's effect (Strategy Pattern in action) ---
        Debug.Log($"MagicSystem: {caster.name} successfully cast {spell.SpellName} on {target.name}.");
        spell.Execute(caster, target); // This calls the specific implementation of the spell
        OnSpellCastSuccess?.Invoke(spell, caster, target);
        OnSpellEffectApplied?.Invoke(spell, caster, target); // Can be split further if cast vs effect are distinct timings

        return true;
    }

    // You could also add more advanced methods here, for example:
    // - IsMagicSuppressedGlobally(): Checks for global anti-magic fields.
    // - LearnSpell(Character character, SpellDefinition spell): Assigns a spell to a character.
    // - RegisterRuntimeSpell(SpellDefinition newSpell): For spells dynamically created or loaded.
}
```

---

## 2. `Character.cs` (Caster and Target Context)

This component represents an entity in the game that can cast spells and/or be affected by them. It holds character-specific stats like health, mana, and manages its own spell cooldowns.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents an entity in the game that can cast or be affected by spells.
/// This acts as a 'Context' for the SpellDefinition 'Strategy' objects.
/// </summary>
public class Character : MonoBehaviour
{
    [Header("Character Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float manaRegenRate = 5f; // Mana per second

    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    // Dictionary to track active cooldowns for spells specific to this character
    private Dictionary<SpellDefinition, float> _activeCooldowns = new Dictionary<SpellDefinition, float>();

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
    }

    private void Update()
    {
        // Regenerate mana
        if (CurrentMana < maxMana)
        {
            CurrentMana += manaRegenRate * Time.deltaTime;
            CurrentMana = Mathf.Min(CurrentMana, maxMana);
        }

        // Update cooldowns
        // Create a temporary list of keys to avoid modifying the dictionary while iterating.
        List<SpellDefinition> spellsOnCooldown = new List<SpellDefinition>(_activeCooldowns.Keys);
        foreach (SpellDefinition spell in spellsOnCooldown)
        {
            _activeCooldowns[spell] -= Time.deltaTime;
            if (_activeCooldowns[spell] <= 0)
            {
                _activeCooldowns.Remove(spell);
                // Optional: Notify UI that cooldown is finished for this spell
                // Debug.Log($"{name}: {spell.SpellName} cooldown finished!");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        Debug.Log($"{name} took {amount} damage. Current Health: {CurrentHealth:F1}");
        if (CurrentHealth <= 0)
        {
            Debug.Log($"{name} has been defeated!");
            // Implement death logic here (e.g., play animation, disable collider, despawn)
            gameObject.SetActive(false); // Simple example death
        }
    }

    public void RestoreHealth(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        Debug.Log($"{name} healed {amount}. Current Health: {CurrentHealth:F1}");
    }

    public bool ConsumeMana(float amount)
    {
        if (CurrentMana >= amount)
        {
            CurrentMana -= amount;
            Debug.Log($"{name} consumed {amount:F1} mana. Current Mana: {CurrentMana:F1}");
            return true;
        }
        Debug.Log($"{name} tried to consume {amount:F1} mana but only has {CurrentMana:F1}.");
        return false;
    }

    /// <summary>
    /// Checks if this character can cast a specific spell based on mana and cooldown.
    /// </summary>
    /// <param name="spell">The SpellDefinition to check.</param>
    /// <returns>True if the character can cast the spell, false otherwise.</returns>
    public bool CanCast(SpellDefinition spell)
    {
        if (CurrentMana < spell.ManaCost)
        {
            Debug.Log($"{name} doesn't have enough mana for {spell.SpellName}. Needed: {spell.ManaCost:F1}, Has: {CurrentMana:F1}");
            return false;
        }

        if (_activeCooldowns.ContainsKey(spell) && _activeCooldowns[spell] > 0)
        {
            Debug.Log($"{name} {spell.SpellName} is on cooldown. Remaining: {_activeCooldowns[spell]:F1}s");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies the cooldown for a spell to this character.
    /// </summary>
    /// <param name="spell">The SpellDefinition for which to apply cooldown.</param>
    public void ApplyCooldown(SpellDefinition spell)
    {
        if (spell.CooldownTime > 0)
        {
            _activeCooldowns[spell] = spell.CooldownTime;
            Debug.Log($"{name}: {spell.SpellName} is now on cooldown for {spell.CooldownTime:F1}s.");
        }
    }
}
```

---

## 3. `SpellDefinition.cs` (Abstract Base for Spell Strategies)

This `ScriptableObject` is the abstract base class for all spells. It defines common properties and the `Execute` method which is the core of the Strategy pattern.

```csharp
using UnityEngine;
using System; // For Guid

/// <summary>
/// Abstract base class for all spells in the MagicSystem.
/// This acts as the 'Strategy' interface in the Strategy design pattern.
/// Each concrete spell will be a ScriptableObject asset, allowing designers
/// to define spell properties and behaviors without code changes.
/// </summary>
public abstract class SpellDefinition : ScriptableObject
{
    [Header("Spell Base Data")]
    [Tooltip("A unique identifier for this spell. Used for lookup in MagicSystem.")]
    public string ID = Guid.NewGuid().ToString(); // Auto-generate a unique ID
    public string SpellName = "New Spell";
    [TextArea] public string Description = "A basic spell effect.";
    public Sprite Icon; // For UI display
    public float ManaCost = 10f;
    public float CooldownTime = 5f; // In seconds
    public float CastTime = 0f; // Time it takes to cast before effect (for future extension)

    /// <summary>
    /// The core method where the spell's unique effect is implemented.
    /// Concrete spell types must override this method.
    /// This is where the specific 'Strategy' for each spell is defined.
    /// </summary>
    /// <param name="caster">The character casting the spell.</param>
    /// <param name="target">The character targeted by the spell.</param>
    public abstract void Execute(Character caster, Character target);

    // Override OnValidate to ensure ID is set and unique (helpful during asset creation)
    protected virtual void OnValidate()
    {
        // If ID is empty or default, generate a new one. This helps ensure unique IDs for lookup.
        if (string.IsNullOrEmpty(ID) || ID.Contains("New Spell"))
        {
            ID = Guid.NewGuid().ToString();
            // Mark the object dirty so Unity saves the new ID to the asset.
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
```

---

## 4. Concrete Spell Implementations (Specific Strategies)

These are `ScriptableObject` assets that derive from `SpellDefinition`, each implementing a unique spell effect.

### `FireballSpell.cs`

```csharp
using UnityEngine;

/// <summary>
/// Concrete spell definition for a Fireball spell.
/// This is one specific 'Strategy' implementation for the SpellDefinition interface.
/// </summary>
[CreateAssetMenu(fileName = "FireballSpell", menuName = "Magic System/Spells/Fireball Spell")]
public class FireballSpell : SpellDefinition
{
    [Header("Fireball Specific Data")]
    public float DamageAmount = 20f;
    public GameObject ProjectilePrefab; // Optional: reference to a visual projectile

    /// <summary>
    /// Implements the specific logic for a Fireball spell: deals damage to the target.
    /// </summary>
    public override void Execute(Character caster, Character target)
    {
        Debug.Log($"<color=orange>{caster.name} casts Fireball on {target.name}, dealing {DamageAmount:F1} damage!</color>");
        target.TakeDamage(DamageAmount);

        // --- Visuals/SFX (example) ---
        if (ProjectilePrefab != null)
        {
            // In a real game, you'd instantiate a projectile, set its target, and let it fly.
            // For simplicity in this example, we'll just log its intended action.
            // GameObject projectile = Instantiate(ProjectilePrefab, caster.transform.position, Quaternion.identity);
            // projectile.GetComponent<Projectile>().Initialize(target.transform, ProjectileSpeed, DamageAmount);
            Debug.Log($"    (Visual effect: A fireball projectile launched from {caster.name} towards {target.name}.)");
        }
    }
}
```

### `HealSpell.cs`

```csharp
using UnityEngine;

/// <summary>
/// Concrete spell definition for a Heal spell.
/// This is another specific 'Strategy' implementation for the SpellDefinition interface.
/// </summary>
[CreateAssetMenu(fileName = "HealSpell", menuName = "Magic System/Spells/Heal Spell")]
public class HealSpell : SpellDefinition
{
    [Header("Heal Spell Specific Data")]
    public float HealAmount = 30f;
    public GameObject HealEffectPrefab; // Optional: reference to a healing visual effect

    /// <summary>
    /// Implements the specific logic for a Heal spell: restores health to the target.
    /// </summary>
    public override void Execute(Character caster, Character target)
    {
        Debug.Log($"<color=green>{caster.name} casts Heal on {target.name}, restoring {HealAmount:F1} health!</color>");
        target.RestoreHealth(HealAmount);

        // --- Visuals/SFX (example) ---
        if (HealEffectPrefab != null)
        {
            // Instantiate visual effect on the target
            // GameObject effect = Instantiate(HealEffectPrefab, target.transform.position, Quaternion.identity);
            // Destroy(effect, 2f); // Destroy effect after a duration
            Debug.Log($"    (Visual effect: A healing aura appears around {target.name}.)");
        }
    }
}
```

### `BuffSpell.cs`

```csharp
using UnityEngine;

/// <summary>
/// Concrete spell definition for a Buff spell.
/// This is a specific 'Strategy' for applying temporary positive effects.
/// </summary>
[CreateAssetMenu(fileName = "BuffSpell", menuName = "Magic System/Spells/Buff Spell")]
public class BuffSpell : SpellDefinition
{
    [Header("Buff Spell Specific Data")]
    public float BuffStrength = 0.2f; // e.g., 20% damage reduction or increase
    public float BuffDuration = 10f; // In seconds
    public string BuffDescription = "Increases target's defense.";

    /// <summary>
    /// Implements the specific logic for a Buff spell: applies a temporary buff.
    /// </summary>
    public override void Execute(Character caster, Character target)
    {
        Debug.Log($"<color=blue>{caster.name} casts {SpellName} on {target.name} for {BuffDuration:F1}s. " +
                  $"Effect: {BuffDescription} (Strength: {BuffStrength:P0}).</color>");

        // In a full implementation, you'd have a dedicated StatusEffectManager component
        // on the Character, or a global one that tracks and applies buffs/debuffs over time.
        // For this example, we'll just log the intended action.
        // target.GetComponent<StatusEffectManager>()?.ApplyBuff(this, BuffStrength, BuffDuration);
        Debug.Log($"    (Status Effect System: {target.name} now has '{SpellName}' buff applied.)");
    }
}
```

---

## 5. `SpellCasterTest.cs` (Example Usage Script)

This MonoBehaviour demonstrates how other game systems (like a Player Controller or AI) would interact with the `MagicSystem`. It also shows how to subscribe to its events.

```csharp
using UnityEngine;

/// <summary>
/// This script demonstrates how to use the MagicSystem.
/// It acts as a client that requests spells from the MagicSystem.
/// Attach this to a GameObject (e.g., 'Player' or 'GameManager') in your scene.
/// </summary>
public class SpellCasterTest : MonoBehaviour
{
    [Header("Casting Test Setup")]
    [Tooltip("Assign the Character that will cast spells (e.g., your Player character).")]
    public Character casterCharacter;
    [Tooltip("Assign the Character that will be targeted by spells (e.g., an Enemy character).")]
    public Character targetCharacter;

    [Space(10)]
    [Tooltip("The ID of the Fireball spell. Copy this from your Fireball Spell ScriptableObject asset.")]
    public string fireballSpellID = "PASTE_FIREBALL_GUID_HERE";
    [Tooltip("The ID of the Heal spell. Copy this from your Heal Spell ScriptableObject asset.")]
    public string healSpellID = "PASTE_HEAL_GUID_HERE";
    [Tooltip("The ID of the Buff spell. Copy this from your Buff Spell ScriptableObject asset.")]
    public string buffSpellID = "PASTE_BUFF_GUID_HERE";

    void Start()
    {
        // --- Example of subscribing to MagicSystem events for UI/VFX/SFX feedback ---
        // This demonstrates the Observer Pattern. Other systems can react to spell events.
        MagicSystem.OnSpellCastSuccess += HandleSpellCastSuccess;
        MagicSystem.OnSpellCastFailed += HandleSpellCastFailed;
        MagicSystem.OnSpellEffectApplied += HandleSpellEffectApplied;

        Debug.Log("SpellCasterTest initialized. Make sure MagicSystem, Caster, and Target are assigned.");
        if (casterCharacter == null) Debug.LogError("SpellCasterTest: Caster Character not assigned!");
        if (targetCharacter == null) Debug.LogError("SpellCasterTest: Target Character not assigned!");
        if (fireballSpellID == "PASTE_FIREBALL_GUID_HERE") Debug.LogWarning("SpellCasterTest: Fireball Spell ID not updated. Please copy the GUID from the asset.");
    }

    void OnDestroy()
    {
        // --- IMPORTANT: Unsubscribe from events to prevent memory leaks ---
        MagicSystem.OnSpellCastSuccess -= HandleSpellCastSuccess;
        MagicSystem.OnSpellCastFailed -= HandleSpellCastFailed;
        MagicSystem.OnSpellEffectApplied -= HandleSpellEffectApplied;
    }

    void Update()
    {
        // Basic safety checks
        if (casterCharacter == null || targetCharacter == null || MagicSystem.Instance == null)
        {
            Debug.LogWarning("SpellCasterTest: Missing references for casting. Cannot proceed. Check assignments in Inspector.");
            return;
        }

        // --- Example Usage: Casting Spells with Keyboard Inputs ---
        // The client (SpellCasterTest) only knows about the MagicSystem Facade and spell IDs.
        // It doesn't need to know how each spell works internally.
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Press '1' to cast Fireball
        {
            Debug.Log($"--- {casterCharacter.name} attempting to cast Fireball (ID: {fireballSpellID}) ---");
            MagicSystem.Instance.CastSpell(fireballSpellID, casterCharacter, targetCharacter);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Press '2' to cast Heal
        {
            Debug.Log($"--- {casterCharacter.name} attempting to cast Heal (ID: {healSpellID}) ---");
            MagicSystem.Instance.CastSpell(healSpellID, casterCharacter, targetCharacter);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) // Press '3' to cast Buff
        {
            Debug.Log($"--- {casterCharacter.name} attempting to cast Buff (ID: {buffSpellID}) ---");
            MagicSystem.Instance.CastSpell(buffSpellID, casterCharacter, targetCharacter);
        }
    }

    // --- Event Handlers for MagicSystem feedback (Observer Pattern) ---
    // These methods would typically trigger UI updates, visual effects, sound effects, etc.
    private void HandleSpellCastSuccess(SpellDefinition spell, Character caster, Character target)
    {
        Debug.Log($"<color=cyan>[UI/VFX/SFX Manager]: '{spell.SpellName}' successfully cast by {caster.name} on {target.name}. Triggering casting animation/sound.</color>");
        // Example: Play caster's spell casting animation, emit casting sound.
    }

    private void HandleSpellCastFailed(SpellDefinition spell, Character caster, Character target)
    {
        Debug.Log($"<color=red>[UI/VFX/SFX Manager]: '{spell.SpellName}' failed by {caster.name}. Show 'Not enough mana' or 'Spell on cooldown' UI message.</color>");
        // Example: Play a "failure" sound, show UI notification.
    }

    private void HandleSpellEffectApplied(SpellDefinition spell, Character caster, Character target)
    {
        Debug.Log($"<color=magenta>[UI/VFX/SFX Manager]: '{spell.SpellName}' effect applied by {caster.name} on {target.name}. Triggering impact VFX/SFX.</color>");
        // Example: Play impact visual effect on target, emit impact sound.
    }
}
```

---

## Unity Setup Instructions:

1.  **Create C# Scripts:**
    *   Create a new C# script for each of the files above (`MagicSystem.cs`, `Character.cs`, `SpellDefinition.cs`, `FireballSpell.cs`, `HealSpell.cs`, `BuffSpell.cs`, `SpellCasterTest.cs`).
    *   Copy-paste the respective code into each script file.

2.  **Create Spell ScriptableObject Assets:**
    *   In the Unity Project window, right-click -> Create -> Magic System -> Spells -> **Fireball Spell**. Name it `Fireball`.
    *   Repeat for **Heal Spell** (name it `Heal`).
    *   Repeat for **Buff Spell** (name it `Buff`).
    *   Select each of these newly created ScriptableObjects in the Project window and inspect their properties in the Inspector.
        *   **Fireball:** Set `Mana Cost` (e.g., 15), `Cooldown Time` (e.g., 3), `Damage Amount` (e.g., 20).
        *   **Heal:** Set `Mana Cost` (e.g., 20), `Cooldown Time` (e.g., 5), `Heal Amount` (e.g., 30).
        *   **Buff:** Set `Mana Cost` (e.g., 10), `Cooldown Time` (e.g., 7), `Buff Duration` (e.g., 10).
    *   **CRITICALLY IMPORTANT:** For each spell asset, copy its generated `ID` (the GUID string) from the Inspector. You'll need these IDs for the `SpellCasterTest` script.

3.  **Set up the Scene:**
    *   **MagicSystem GameObject:**
        *   Create an empty GameObject in your scene named `MagicSystem`.
        *   Add the `MagicSystem.cs` component to it.
        *   In the `MagicSystem` component's Inspector, drag and drop your `Fireball`, `Heal`, and `Buff` ScriptableObject assets into the `Registered Spells` array.
    *   **Player Character:**
        *   Create an empty GameObject named `Player`.
        *   Add the `Character.cs` component to it.
        *   Adjust its `Max Health`, `Max Mana`, `Mana Regen Rate` (e.g., 100 Health, 50 Mana, 5 Mana Regen).
    *   **Enemy Character:**
        *   Create an empty GameObject named `Enemy`.
        *   Add the `Character.cs` component to it.
        *   Adjust its stats (e.g., 100 Health, 0 Mana, 0 Mana Regen if it's not meant to cast spells).
    *   **SpellCasterTest GameObject:**
        *   Create an empty GameObject named `GameLogic` (or attach `SpellCasterTest.cs` to the `Player` GameObject).
        *   Add the `SpellCasterTest.cs` component to it.
        *   In the `SpellCasterTest` component's Inspector:
            *   Drag your `Player` GameObject into the `Caster Character` slot.
            *   Drag your `Enemy` GameObject into the `Target Character` slot.
            *   Paste the GUIDs you copied from the spell assets into the `Fireball Spell ID`, `Heal Spell ID`, and `Buff Spell ID` fields respectively.

4.  **Run the Scene:**
    *   Press Play in Unity.
    *   Observe the Console window.
    *   Press '1' on your keyboard to cast the Fireball spell.
    *   Press '2' to cast the Heal spell.
    *   Press '3' to cast the Buff spell.
    *   Try casting repeatedly to see the mana cost and cooldown mechanisms in action. Watch how health and mana change for the Player and Enemy.

This complete setup demonstrates a practical and extensible 'MagicSystem' in Unity, ready for use in your projects!