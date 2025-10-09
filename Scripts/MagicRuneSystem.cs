// Unity Design Pattern Example: MagicRuneSystem
// This script demonstrates the MagicRuneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MagicRuneSystem' design pattern, while not a standard Gang-of-Four pattern, is a practical approach for creating flexible, data-driven systems involving items or abilities that apply diverse effects in games. This example interprets it as a system for managing "Runes" (game items) that encapsulate various "Effects" and apply them to "Targets." It leverages **ScriptableObjects** for data-driven content, the **Strategy Pattern** for interchangeable effects, and an implicit **Composite Pattern** to allow single runes to have multiple effects.

---

### MagicRuneSystem Design Pattern for Unity

**Concept:**
The 'MagicRuneSystem' is designed for games where entities (players, enemies, items) can acquire and apply "runes" which grant specific magical effects. It emphasizes modularity, extensibility, and data-driven content creation.

It leverages a combination of well-known patterns:

1.  **Strategy Pattern:** Each `MagicRuneEffect` acts as a concrete strategy, defining a unique way to alter a `MagicTarget` (e.g., deal damage, heal, buff stats). This allows different effects to be interchangeable.
2.  **Composite Pattern (Implicit):** A single `MagicRune` can contain a list of multiple `MagicRuneEffect`s, treating a group of effects uniformly as a single rune. This allows for complex runes that do multiple things (e.g., a "Fire Rune" might deal damage AND apply a burn debuff).
3.  **Data-Driven Design:** Both `MagicRune`s and `MagicRuneEffect`s are implemented as ScriptableObjects. This allows game designers to create, combine, and balance new runes and effects directly in the Unity Editor without writing a single line of code.
4.  **Command Pattern (Optional perspective):** The `Apply` and `Remove` methods on `MagicRuneEffect` can be seen as commands executed on a `MagicTarget`.

**Core Components:**

1.  **`MagicRuneEffect` (Abstract ScriptableObject):**
    *   The base class for all individual magical effects. It defines the contract (`Apply`, `Remove`) that all concrete effects must adhere to.
    *   Being an abstract ScriptableObject, it provides a powerful way to define common properties and expose specific effect parameters in the Inspector.
2.  **Concrete `MagicRuneEffect`s (ScriptableObjects):**
    *   Derived from `MagicRuneEffect`, these implement specific behaviors (e.g., Damage, Heal, Stat Buff).
    *   Each concrete effect can have unique public fields for designers to configure (e.g., damage amount, buff duration).
3.  **`MagicRune` (ScriptableObject):**
    *   Represents a collectible or usable "rune item" in the game.
    *   Crucially, it holds a list of references to one or more `MagicRuneEffect` ScriptableObjects. This means a single rune can have multiple effects.
4.  **`MagicTarget` (MonoBehaviour):**
    *   Any entity in the game that can be affected by runes (e.g., Player, Enemy, Interactable Object).
    *   It provides the necessary public methods (`ApplyRune`, `RemoveRune`) and properties (health, stats) that rune effects will interact with.
    *   It manages the lifecycle of timed effects (buffs/debuffs) through coroutines.
5.  **`RuneUser` (MonoBehaviour - Example):**
    *   A simple example component demonstrating how a character might hold and use `MagicRune`s.
    *   Serves as a practical entry point to test the system.

**Benefits:**

*   **High Extensibility:** Add new rune effects without modifying existing code. Just create a new `MagicRuneEffect` subclass.
*   **Easy Content Creation:** Designers can create complex runes and effects using ScriptableObjects in the editor, without programmer intervention.
*   **Flexibility:** Runes can easily be combined with multiple effects. Effects can be reused across different runes.
*   **Decoupling:** Effects are decoupled from the runes that hold them and the targets they affect.

---

### C# Unity Scripts

Create these scripts in your Unity project, preferably organized in a `MagicRuneSystem/Scripts` folder, with `Effects` as a subfolder.

#### 1. `MagicRuneEffect.cs` (Abstract Base Effect)

This is the abstract base class for all magical effects. It defines the interface for applying and removing effects.

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/MagicRuneEffect.cs
using UnityEngine;

namespace MagicRuneSystem
{
    /// <summary>
    /// Base abstract class for all magical rune effects.
    /// This uses the Strategy Pattern: each concrete effect is a specific strategy
    /// for how a rune modifies a target.
    /// Being a ScriptableObject allows designers to create and configure
    /// different effects directly in the Unity editor.
    /// </summary>
    public abstract class MagicRuneEffect : ScriptableObject
    {
        [Tooltip("A unique identifier for this effect, useful for debugging or UI.")]
        public string EffectId => name; // Using the asset's name as a unique ID.

        [Tooltip("A user-friendly name for this effect.")]
        public string EffectName = "Unnamed Effect";

        [TextArea(3, 5)]
        [Tooltip("A description explaining what this effect does.")]
        public string EffectDescription = "This is a generic magical rune effect.";

        /// <summary>
        /// Applies this rune effect to the given MagicTarget.
        /// This method must be implemented by all concrete effect types.
        /// </summary>
        /// <param name="target">The MagicTarget to which the effect is applied.</param>
        public abstract void Apply(MagicTarget target);

        /// <summary>
        /// Removes this rune effect from the given MagicTarget.
        /// This method is crucial for temporary buffs/debuffs.
        /// It's virtual to allow effects that don't need explicit removal (e.g., instant damage)
        /// to simply do nothing, or for effects to clean up in a custom way.
        /// </summary>
        /// <param name="target">The MagicTarget from which the effect is removed.</param>
        public virtual void Remove(MagicTarget target)
        {
            // Default implementation does nothing. Override in subclasses for temporary effects.
            Debug.Log($"[{EffectId}] Removed (default behavior: no explicit action).");
        }
    }
}
```

#### 2. Concrete `MagicRuneEffect` Implementations (Examples)

Place these scripts in `Assets/MagicRuneSystem/Scripts/Effects`.

##### `DamageEffectRune.cs` (Instant Effect)

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/Effects/DamageEffectRune.cs
using UnityEngine;

namespace MagicRuneSystem
{
    /// <summary>
    /// Concrete implementation of MagicRuneEffect that deals damage to a target.
    /// This demonstrates an instant, non-removable effect.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Magic Rune System/Effects/Damage Rune Effect")]
    public class DamageEffectRune : MagicRuneEffect
    {
        [Tooltip("The amount of damage this effect deals.")]
        public float damageAmount = 10f;

        public override void Apply(MagicTarget target)
        {
            if (target == null) return;
            target.TakeDamage(damageAmount);
            Debug.Log($"[{EffectId}] Applied to {target.name}: Deals {damageAmount} damage.");
        }

        // Damage is an instant effect, so Remove is not typically needed.
        // We inherit the default empty Remove implementation from MagicRuneEffect.
    }
}
```

##### `HealEffectRune.cs` (Instant Effect)

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/Effects/HealEffectRune.cs
using UnityEngine;

namespace MagicRuneSystem
{
    /// <summary>
    /// Concrete implementation of MagicRuneEffect that heals a target.
    /// This demonstrates an instant, non-removable effect.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHealEffect", menuName = "Magic Rune System/Effects/Heal Rune Effect")]
    public class HealEffectRune : MagicRuneEffect
    {
        [Tooltip("The amount of health this effect restores.")]
        public float healAmount = 15f;

        public override void Apply(MagicTarget target)
        {
            if (target == null) return;
            target.Heal(healAmount);
            Debug.Log($"[{EffectId}] Applied to {target.name}: Heals {healAmount} health.");
        }

        // Heal is an instant effect, so Remove is not typically needed.
        // We inherit the default empty Remove implementation from MagicRuneEffect.
    }
}
```

##### `StatBuffRune.cs` (Timed Effect)

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/Effects/StatBuffRune.cs
using UnityEngine;

namespace MagicRuneSystem
{
    /// <summary>
    /// Concrete implementation of MagicRuneEffect that applies a temporary stat buff to a target.
    /// This demonstrates a timed, removable effect.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatBuffEffect", menuName = "Magic Rune System/Effects/Stat Buff Rune")]
    public class StatBuffRune : MagicRuneEffect
    {
        public enum StatType { Strength, Speed }

        [Tooltip("The type of stat to buff.")]
        public StatType statToBuff;

        [Tooltip("The amount to add to the stat.")]
        public float buffAmount = 5f;

        [Tooltip("The duration of the buff in seconds. Set to 0 for permanent (though typically not recommended for buffs).")]
        public float duration = 10f;

        public override void Apply(MagicTarget target)
        {
            if (target == null) return;

            // Apply the buff
            switch (statToBuff)
            {
                case StatType.Strength:
                    target.strength += buffAmount;
                    break;
                case StatType.Speed:
                    target.speed += buffAmount;
                    break;
            }
            Debug.Log($"[{EffectId}] Applied to {target.name}: {statToBuff} buffed by {buffAmount}. Duration: {duration}s.");

            // Start coroutine to remove the buff if it's timed
            if (duration > 0)
            {
                target.StartTimedEffectCoroutine(this, duration);
            }
        }

        public override void Remove(MagicTarget target)
        {
            if (target == null) return;

            // Remove the buff
            switch (statToBuff)
            {
                case StatType.Strength:
                    target.strength -= buffAmount;
                    break;
                case StatType.Speed:
                    target.speed -= buffAmount;
                    break;
            }
            Debug.Log($"[{EffectId}] Removed from {target.name}: {statToBuff} debuffed by {buffAmount}.");
            base.Remove(target); // Call base for default logging
        }
    }
}
```

#### 3. `MagicRune.cs` (Rune Definition)

This ScriptableObject defines a single rune and references the effects it applies.

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/MagicRune.cs
using UnityEngine;
using System.Collections.Generic;

namespace MagicRuneSystem
{
    /// <summary>
    /// Represents a single Magic Rune item that can be found, equipped, or used.
    /// This ScriptableObject acts as a data container for the rune's properties
    /// and, crucially, a list of MagicRuneEffect ScriptableObjects it grants.
    /// This exemplifies the Composite Pattern (implicitly) and Data-Driven Design,
    /// allowing designers to create complex runes with multiple effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMagicRune", menuName = "Magic Rune System/Magic Rune")]
    public class MagicRune : ScriptableObject
    {
        [Tooltip("The name of this magic rune.")]
        public string runeName = "Basic Rune";

        [Tooltip("An optional icon for UI representation.")]
        public Sprite runeIcon;

        [TextArea(3, 10)]
        [Tooltip("A detailed description of the rune's lore or effects.")]
        public string description = "A simple rune with magical properties.";

        [Tooltip("The list of effects this rune applies when used.")]
        public List<MagicRuneEffect> effects = new List<MagicRuneEffect>();

        /// <summary>
        /// Applies all effects contained within this rune to the given target.
        /// </summary>
        /// <param name="target">The MagicTarget to apply effects to.</param>
        public void ApplyToTarget(MagicTarget target)
        {
            if (target == null)
            {
                Debug.LogError($"Cannot apply rune '{runeName}'. Target is null.");
                return;
            }

            Debug.Log($"Applying rune: {runeName} to {target.name}...");
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    effect.Apply(target);
                }
                else
                {
                    Debug.LogWarning($"Rune '{runeName}' has a null effect in its list. Please check your ScriptableObjects.");
                }
            }
        }

        /// <summary>
        /// Removes all effects contained within this rune from the given target.
        /// This is primarily used for revoking a rune's effects, useful if runes
        /// can be unequipped or dispelled.
        /// </summary>
        /// <param name="target">The MagicTarget to remove effects from.</param>
        public void RemoveFromTarget(MagicTarget target)
        {
            if (target == null)
            {
                Debug.LogError($"Cannot remove rune '{runeName}'. Target is null.");
                return;
            }

            Debug.Log($"Removing rune: {runeName} from {target.name}...");
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    effect.Remove(target);
                }
                else
                {
                    Debug.LogWarning($"Rune '{runeName}' has a null effect in its list. Please check your ScriptableObjects.");
                }
            }
        }
    }
}
```

#### 4. `MagicTarget.cs` (Entity Affected by Runes)

This MonoBehaviour represents any entity that can be affected by runes, managing its stats and active timed effects.

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/MagicTarget.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MagicRuneSystem
{
    /// <summary>
    /// Represents an entity in the game that can be affected by Magic Runes.
    /// This MonoBehaviour manages its own stats and the lifecycle of active effects.
    /// </summary>
    public class MagicTarget : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float _health = 100f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _strength = 10f;
        [SerializeField] private float _speed = 5f;

        public float health
        {
            get => _health;
            private set
            {
                _health = Mathf.Clamp(value, 0, _maxHealth);
                Debug.Log($"{name}'s Health: {_health}/{_maxHealth}");
                if (_health <= 0) Die();
            }
        }
        public float strength
        {
            get => _strength;
            set
            {
                _strength = Mathf.Max(0, value); // Strength cannot go below 0
                Debug.Log($"{name}'s Strength: {_strength}");
            }
        }
        public float speed
        {
            get => _speed;
            set
            {
                _speed = Mathf.Max(0, value); // Speed cannot go below 0
                Debug.Log($"{name}'s Speed: {_speed}");
            }
        }

        // Dictionary to track active timed effects (like buffs/debuffs)
        // Key: The MagicRuneEffect ScriptableObject instance
        // Value: The Coroutine that is managing its duration
        private Dictionary<MagicRuneEffect, Coroutine> _activeTimedEffects = new Dictionary<MagicRuneEffect, Coroutine>();

        void Start()
        {
            Debug.Log($"Initialized {name} (MagicTarget). Health: {health}, Strength: {strength}, Speed: {speed}");
        }

        /// <summary>
        /// Applies all effects from a given MagicRune to this target.
        /// </summary>
        /// <param name="rune">The MagicRune to apply.</param>
        public void ApplyRune(MagicRune rune)
        {
            if (rune == null)
            {
                Debug.LogError("Attempted to apply a null rune.");
                return;
            }
            rune.ApplyToTarget(this);
            // In a real game, you might want to track which runes are currently applied
            // For this example, we let effects manage their own removal or state.
        }

        /// <summary>
        /// Removes all effects from a given MagicRune from this target.
        /// This is useful if runes can be unequipped or dispelled.
        /// Note: For timed effects, this will stop the coroutine and call Remove immediately.
        /// </summary>
        /// <param name="rune">The MagicRune to remove.</param>
        public void RemoveRune(MagicRune rune)
        {
            if (rune == null)
            {
                Debug.LogError("Attempted to remove a null rune.");
                return;
            }
            rune.RemoveFromTarget(this);

            // Also stop any ongoing coroutines associated with effects from this rune
            foreach (var effect in rune.effects)
            {
                if (_activeTimedEffects.ContainsKey(effect))
                {
                    StopCoroutine(_activeTimedEffects[effect]);
                    _activeTimedEffects.Remove(effect);
                    Debug.Log($"Stopped timed effect coroutine for {effect.EffectId}.");
                }
            }
        }

        /// <summary>
        /// Handles incoming damage.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        public void TakeDamage(float amount)
        {
            health -= amount;
            Debug.Log($"!!! {name} took {amount} damage. Current Health: {health}");
        }

        /// <summary>
        /// Handles healing.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            health += amount;
            Debug.Log($"+++ {name} healed {amount}. Current Health: {health}");
        }

        /// <summary>
        /// Starts a coroutine to manage a timed effect (like a buff or debuff).
        /// This ensures the effect's 'Remove' method is called after its duration.
        /// </summary>
        /// <param name="effect">The MagicRuneEffect to manage.</param>
        /// <param name="duration">The duration in seconds.</param>
        public void StartTimedEffectCoroutine(MagicRuneEffect effect, float duration)
        {
            // If the effect is already active, stop the old one and start a new one (refresh duration)
            if (_activeTimedEffects.ContainsKey(effect) && _activeTimedEffects[effect] != null)
            {
                StopCoroutine(_activeTimedEffects[effect]);
                Debug.LogWarning($"Refreshed duration for active timed effect: {effect.EffectId}");
                effect.Remove(this); // Remove previous instance before re-applying
            }

            Coroutine newCoroutine = StartCoroutine(TimedEffectRoutine(effect, duration));
            _activeTimedEffects[effect] = newCoroutine;
        }

        private IEnumerator TimedEffectRoutine(MagicRuneEffect effect, float duration)
        {
            yield return new WaitForSeconds(duration);

            // Time's up, remove the effect
            if (_activeTimedEffects.ContainsKey(effect))
            {
                effect.Remove(this);
                _activeTimedEffects.Remove(effect);
            }
            else
            {
                Debug.LogWarning($"Timed effect {effect.EffectId} tried to remove itself but was already removed or not tracked.");
            }
        }

        private void Die()
        {
            Debug.Log($"{name} has fallen!");
            // Trigger death animations, destroy game object, etc.
            // For now, let's just make it inactive.
            gameObject.SetActive(false);
        }
    }
}
```

#### 5. `RuneUser.cs` (Example Usage)

This MonoBehaviour demonstrates how a character might interact with the rune system.

```csharp
// FILE: Assets/MagicRuneSystem/Scripts/RuneUser.cs
using UnityEngine;
using System.Collections.Generic;

namespace MagicRuneSystem
{
    /// <summary>
    /// An example MonoBehaviour demonstrating how a character (e.g., Player)
    /// might interact with and apply Magic Runes to a target.
    /// This script acts as a simple interface for testing the MagicRuneSystem.
    /// </summary>
    public class RuneUser : MonoBehaviour
    {
        [Tooltip("The MagicTarget that this RuneUser will apply runes to.")]
        public MagicTarget target;

        [Tooltip("A list of MagicRunes available to this user (e.g., player inventory).")]
        public List<MagicRune> availableRunes = new List<MagicRune>();

        private int _currentRuneIndex = 0;

        void Update()
        {
            if (target == null)
            {
                Debug.LogWarning("RuneUser: Target is not assigned.", this);
                return;
            }

            // Press Space to apply the current rune
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (availableRunes.Count > 0)
                {
                    ApplyCurrentRune();
                }
                else
                {
                    Debug.Log("No runes available to use.");
                }
            }

            // Press R to cycle to the next rune
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (availableRunes.Count > 1)
                {
                    _currentRuneIndex = (_currentRuneIndex + 1) % availableRunes.Count;
                    Debug.Log($"Switched to rune: {availableRunes[_currentRuneIndex].runeName}");
                }
                else if (availableRunes.Count == 1)
                {
                    Debug.Log("Only one rune available.");
                }
                else
                {
                    Debug.Log("No runes available to switch to.");
                }
            }

            // Press X to remove the currently selected rune's effects (if applicable)
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (availableRunes.Count > 0)
                {
                    RemoveCurrentRuneEffects();
                }
                else
                {
                     Debug.Log("No runes available to remove effects from.");
                }
            }
        }

        /// <summary>
        /// Applies the currently selected rune's effects to the target.
        /// </summary>
        public void ApplyCurrentRune()
        {
            if (availableRunes.Count == 0) return;

            MagicRune runeToUse = availableRunes[_currentRuneIndex];
            Debug.Log($"Using rune: {runeToUse.runeName} on {target.name}");
            target.ApplyRune(runeToUse);
        }

        /// <summary>
        /// Removes the currently selected rune's effects from the target.
        /// This is for demonstrating removal of effects, particularly timed ones.
        /// </summary>
        public void RemoveCurrentRuneEffects()
        {
            if (availableRunes.Count == 0) return;

            MagicRune runeToRemove = availableRunes[_currentRuneIndex];
            Debug.Log($"Manually removing effects of rune: {runeToRemove.runeName} from {target.name}");
            target.RemoveRune(runeToRemove);
        }
    }
}
```

---

### Unity Editor Setup and Example Usage

Follow these steps to get the example working in Unity:

1.  **Create Project:** Start a new Unity 3D project.
2.  **Create Folders:** In the Project window, create a folder structure: `Assets/MagicRuneSystem/Scripts` and `Assets/MagicRuneSystem/ScriptableObjects`. Inside `Scripts`, create an `Effects` subfolder.
3.  **Place C# Scripts:** Copy and paste the code for each script (`MagicRuneEffect.cs`, `DamageEffectRune.cs`, etc.) into their respective files in the folders you created.
4.  **Create ScriptableObject Assets:**
    *   Navigate to `Assets/MagicRuneSystem/ScriptableObjects`.
    *   **Create Effects:** Right-click -> Create -> Magic Rune System -> Effects. Create instances of each effect type and configure their values:
        *   `DamageRune_10`: `Damage Rune Effect`, `Damage Amount` = `10`.
        *   `HealRune_25`: `Heal Rune Effect`, `Heal Amount` = `25`.
        *   `StrengthBuff_5_10s`: `Stat Buff Rune`, `Stat To Buff` = `Strength`, `Buff Amount` = `5`, `Duration` = `10`.
        *   `SpeedBuff_2_5s`: `Stat Buff Rune`, `Stat To Buff` = `Speed`, `Buff Amount` = `2`, `Duration` = `5`.
        *   `ComboStrengthBuff_3s`: `Stat Buff Rune`, `Stat To Buff` = `Strength`, `Buff Amount` = `3`, `Duration` = `3`.
        *   `ComboSpeedBuff_1_5_3s`: `Stat Buff Rune`, `Stat To Buff` = `Speed`, `Buff Amount` = `1.5`, `Duration` = `3`.
    *   **Create Runes:** Right-click -> Create -> Magic Rune System -> Magic Rune. Create instances and assign your created effects:
        *   `RuneOfFire`: `Rune Name` = "Rune of Fire". Drag `DamageRune_10` into its `Effects` list.
        *   `RuneOfLife`: `Rune Name` = "Rune of Life". Drag `HealRune_25` into its `Effects` list.
        *   `RuneOfMight`: `Rune Name` = "Rune of Might". Drag `StrengthBuff_5_10s` into its `Effects` list.
        *   `RuneOfHaste`: `Rune Name` = "Rune of Haste". Drag `SpeedBuff_2_5s` into its `Effects` list.
        *   `RuneOfSynergy`: `Rune Name` = "Rune of Synergy". Drag *both* `ComboStrengthBuff_3s` and `ComboSpeedBuff_1_5_3s` into its `Effects` list. This rune will apply two buffs simultaneously!
5.  **Setup Scene:**
    *   In a new empty scene, create an empty GameObject named `Player`.
    *   Attach the `MagicTarget.cs` script to the `Player` GameObject. Set initial `Health`, `Strength`, `Speed` values (e.g., 100, 10, 5).
    *   Attach the `RuneUser.cs` script to the `Player` GameObject.
    *   Drag the `Player` GameObject itself from the Hierarchy into the `Target` field of the `RuneUser` component.
    *   Drag all the `MagicRune` ScriptableObjects (e.g., `RuneOfFire`, `RuneOfLife`, `RuneOfMight`, `RuneOfHaste`, `RuneOfSynergy`) from your Project window into the `Available Runes` list on the `RuneUser` component.
6.  **Run and Test:**
    *   Press Play in the Unity Editor.
    *   Open the Console window (Window -> General -> Console) to see debug messages.
    *   Select the `Player` GameObject in the Hierarchy and observe its `MagicTarget` component in the Inspector to see stats change in real-time.
    *   Press **Spacebar** repeatedly to apply the currently selected rune.
    *   Press **R** to cycle through the available runes.
    *   Press **X** to manually remove the effects of the currently selected rune (useful for seeing timed buffs disappear prematurely).

This setup provides a fully functional and extensible 'MagicRuneSystem' that you can use as a foundation for your game's item and ability systems.