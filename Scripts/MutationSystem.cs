// Unity Design Pattern Example: MutationSystem
// This script demonstrates the MutationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements the **MutationSystem** design pattern in Unity. This pattern is particularly useful for managing dynamic state changes, such as applying buffs, debuffs, or item modifications to game entities. It promotes a modular and extensible way to alter an object's properties without tightly coupling the modification logic to the object itself.

The core idea is:
1.  **Define a state `T`** (e.g., character attributes).
2.  **Define `IMutation<T>`** – an interface for operations that can change `T`.
3.  **Implement concrete `IMutation<T>`** classes (e.g., `SpeedBuffMutation`).
4.  **Create a `MutationSystem<T>`** that manages a collection of active `IMutation<T>` instances and applies them to a base state to calculate a final, mutated state.
5.  **Use `ScriptableObject`s** (`CharacterMutationSO`) to define mutation data as assets, making them configurable in the Unity Editor.
6.  **Integrate with a `MonoBehaviour`** (`CharacterMutator`) to apply and manage mutations on a game object.

---

### Complete C# Unity Code (`MutationSystemExample.cs`)

Create a new C# script in your Unity project, name it `MutationSystemExample.cs`, and copy the following code into it.

```csharp
using UnityEngine;
using System; // For Action, Func, IDisposable
using System.Collections.Generic;
using System.Collections; // For Coroutines

// --- 1. Data Structure for Attributes (The Generic Type 'T' in MutationSystem<T>) ---
// This struct holds the actual data that will be mutated.
// Using a struct ensures value-type semantics, making copies explicit and easier to manage immutability
// within the mutation process.
[System.Serializable] // Make it visible in the Inspector if part of a MonoBehaviour
public struct CharacterAttributeData
{
    // Base attributes: These are the immutable starting values for the character.
    public float baseHealth;
    public float baseSpeed;
    public float baseDamage;
    public float baseDefense;

    // Current attributes: These are the values *after* all mutations have been applied.
    // They are derived from base values + mutations.
    public float currentHealth;
    public float currentSpeed;
    public float currentDamage;
    public float currentDefense;

    public CharacterAttributeData(float health, float speed, float damage, float defense)
    {
        baseHealth = health;
        baseSpeed = speed;
        baseDamage = damage;
        baseDefense = defense;
        // Initialize current values to base values; mutations will then adjust them.
        currentHealth = health;
        currentSpeed = speed;
        currentDamage = damage;
        currentDefense = defense;
    }

    // Helper to reset current values to base values for a fresh calculation.
    // This is important before reapplying all mutations.
    public void ResetCurrentToBases()
    {
        currentHealth = baseHealth;
        currentSpeed = baseSpeed;
        currentDamage = baseDamage;
        currentDefense = baseDefense;
    }

    public override string ToString()
    {
        return $"H:{currentHealth:F1}, S:{currentSpeed:F1}, D:{currentDamage:F1}, Def:{currentDefense:F1}";
    }
}

// --- 2. IMutation<T> Interface (The Mutation Contract) ---
// Defines the contract for any object that wants to modify a state of type T.
// Each mutation takes the current state (a copy) and returns a new state with the modification applied.
// This approach treats mutations as functions that transform one state into another,
// promoting clarity and potentially immutability (though we modify the struct copy directly here).
public interface IMutation<T>
{
    // A unique identifier for the mutation, useful for removing specific effects or for debugging.
    string Id { get; }

    // A human-readable display name for UI or debugging.
    string DisplayName { get; }

    // Applies the mutation to the given data and returns the modified data.
    // When T is a struct, 'currentData' is a copy. Modifications to it are made on the copy,
    // which is then returned. If T were a class, you would typically create a new instance,
    // copy relevant fields, apply modifications, and return the new instance to avoid side-effects
    // on the 'currentData' object itself if it was shared.
    T Apply(T currentData);
}

// --- 3. Base ScriptableObject for Mutations (Mutation Definition as Assets) ---
// This allows us to define different types of mutations as assets in the Unity editor.
// Each concrete mutation ScriptableObject will hold specific parameters for its mutation type.
public abstract class CharacterMutationSO : ScriptableObject
{
    // Using GUID for unique IDs is robust, or allow manual override if specific IDs are needed.
    [SerializeField] private string _id = Guid.NewGuid().ToString();
    public string Id => _id; 
    
    [SerializeField] private string _displayName = "New Mutation";
    public string DisplayName => _displayName;

    [Tooltip("Icon for this mutation (optional)")]
    [SerializeField] private Sprite _icon;
    public Sprite Icon => _icon;

    // Abstract method to get the actual IMutation<CharacterAttributeData> instance.
    // This acts as a Factory Method, allowing the ScriptableObject to create the runtime
    // mutation logic with its stored parameters. This decouples the definition from the implementation.
    public abstract IMutation<CharacterAttributeData> GetMutation();
}

// --- 3a. Concrete ScriptableObject Mutations (Specific Mutation Definitions) ---
// These are actual asset definitions that designers can create in the Unity editor.

[CreateAssetMenu(fileName = "SO_SpeedBuff", menuName = "MutationSystem/Speed Buff")]
public class SO_SpeedBuff : CharacterMutationSO
{
    [Tooltip("Flat speed modifier (e.g., +5, -2)")]
    [SerializeField] private float _flatSpeedModifier = 0f;
    [Tooltip("Percentage speed modifier (e.g., +0.2 for 20%, -0.1 for -10%)")]
    [SerializeField] private float _percentageSpeedModifier = 0f;

    public override IMutation<CharacterAttributeData> GetMutation()
    {
        // Creates a runtime instance of SpeedModifierMutation with parameters from this ScriptableObject.
        return new SpeedModifierMutation(Id, DisplayName, _flatSpeedModifier, _percentageSpeedModifier);
    }
}

[CreateAssetMenu(fileName = "SO_DamageBoost", menuName = "MutationSystem/Damage Boost")]
public class SO_DamageBoost : CharacterMutationSO
{
    [Tooltip("Flat damage modifier (e.g., +10, -5)")]
    [SerializeField] private float _flatDamageModifier = 0f;
    [Tooltip("Percentage damage modifier (e.g., +0.15 for 15%)")]
    [SerializeField] private float _percentageDamageModifier = 0f;

    public override IMutation<CharacterAttributeData> GetMutation()
    {
        return new DamageModifierMutation(Id, DisplayName, _flatDamageModifier, _percentageDamageModifier);
    }
}

[CreateAssetMenu(fileName = "SO_DefenseBuff", menuName = "MutationSystem/Defense Buff")]
public class SO_DefenseBuff : CharacterMutationSO
{
    [Tooltip("Flat defense modifier (e.g., +3, -1)")]
    [SerializeField] private float _flatDefenseModifier = 0f;
    [Tooltip("Percentage defense modifier (e.g., +0.1 for 10%)")]
    [SerializeField] private float _percentageDefenseModifier = 0f;
    
    public override IMutation<CharacterAttributeData> GetMutation()
    {
        return new DefenseModifierMutation(Id, DisplayName, _flatDefenseModifier, _percentageDefenseModifier);
    }
}


// --- 4. Concrete IMutation<T> Implementations (The Mutation Logic) ---
// These are the actual C# classes that implement the specific modification logic.
// They are typically instantiated by their corresponding CharacterMutationSO's GetMutation() method.

public class SpeedModifierMutation : IMutation<CharacterAttributeData>
{
    public string Id { get; }
    public string DisplayName { get; }
    private readonly float _flatModifier;
    private readonly float _percentageModifier;

    public SpeedModifierMutation(string id, string displayName, float flatModifier, float percentageModifier)
    {
        Id = id;
        DisplayName = displayName;
        _flatModifier = flatModifier;
        _percentageModifier = percentageModifier;
    }

    public CharacterAttributeData Apply(CharacterAttributeData currentData)
    {
        // Apply flat modifier first
        currentData.currentSpeed += _flatModifier;
        // Then apply percentage modifier to the result of the flat modifier
        currentData.currentSpeed *= (1 + _percentageModifier);
        return currentData;
    }
}

public class DamageModifierMutation : IMutation<CharacterAttributeData>
{
    public string Id { get; }
    public string DisplayName { get; }
    private readonly float _flatModifier;
    private readonly float _percentageModifier;

    public DamageModifierMutation(string id, string displayName, float flatModifier, float percentageModifier)
    {
        Id = id;
        DisplayName = displayName;
        _flatModifier = flatModifier;
        _percentageModifier = percentageModifier;
    }

    public CharacterAttributeData Apply(CharacterAttributeData currentData)
    {
        currentData.currentDamage += _flatModifier;
        currentData.currentDamage *= (1 + _percentageModifier);
        return currentData;
    }
}

public class DefenseModifierMutation : IMutation<CharacterAttributeData>
{
    public string Id { get; }
    public string DisplayName { get; }
    private readonly float _flatModifier;
    private readonly float _percentageModifier;

    public DefenseModifierMutation(string id, string displayName, float flatModifier, float percentageModifier)
    {
        Id = id;
        DisplayName = displayName;
        _flatModifier = flatModifier;
        _percentageModifier = percentageModifier;
    }

    public CharacterAttributeData Apply(CharacterAttributeData currentData)
    {
        currentData.currentDefense += _flatModifier;
        currentData.currentDefense *= (1 + _percentageModifier);
        return currentData;
    }
}


// --- 5. MutationSystem<T> (The Core System Logic) ---
// This is a generic C# class (not MonoBehaviour) that encapsulates the core logic
// of managing and applying mutations to any data type T. It's reusable across different contexts.
public class MutationSystem<T>
{
    // List of currently active mutations.
    private readonly List<IMutation<T>> _activeMutations = new List<IMutation<T>>();

    // Read-only list of active mutations for external inspection or UI display.
    public IReadOnlyList<IMutation<T>> ActiveMutations => _activeMutations;

    // Event raised when mutations are added or removed, signaling a need for recalculation.
    public event Action OnMutationsChanged;

    /// <summary>
    /// Adds a mutation to the system. If a mutation with the same ID already exists,
    /// it is removed first (allowing a new effect to replace an old one).
    /// </summary>
    /// <param name="mutation">The IMutation<T> instance to add.</param>
    public void AddMutation(IMutation<T> mutation)
    {
        if (mutation == null) return;

        // Check if a mutation with this ID already exists and remove it.
        // This behavior ensures that applying the same effect again (e.g., another "Haste" potion)
        // can replace the existing one, or simply stack if the effect is designed to.
        // For simple stacking without replacement, remove this line.
        RemoveMutation(mutation.Id); 
        
        _activeMutations.Add(mutation);
        OnMutationsChanged?.Invoke(); // Notify subscribers that the mutation list has changed.
        Debug.Log($"MutationSystem: Added mutation '{mutation.DisplayName}' (ID: {mutation.Id})");
    }

    /// <summary>
    /// Removes a mutation from the system by its ID.
    /// </summary>
    /// <param name="mutationId">The ID of the mutation to remove.</param>
    public void RemoveMutation(string mutationId)
    {
        if (string.IsNullOrEmpty(mutationId)) return;

        // Find the mutation to remove.
        IMutation<T> mutationToRemove = null;
        foreach (var m in _activeMutations)
        {
            if (m.Id == mutationId)
            {
                mutationToRemove = m;
                break;
            }
        }

        // If found, remove it and trigger the change event.
        if (mutationToRemove != null)
        {
            _activeMutations.Remove(mutationToRemove);
            OnMutationsChanged?.Invoke();
            Debug.Log($"MutationSystem: Removed mutation '{mutationToRemove.DisplayName}' (ID: {mutationId})");
        }
    }

    /// <summary>
    /// Applies all active mutations to a given base state and returns the final mutated state.
    /// </summary>
    /// <param name="baseState">The initial state to which mutations will be applied.</param>
    /// <returns>The state after all active mutations have been applied in their current order.</returns>
    public T GetMutatedState(T baseState)
    {
        T currentState = baseState; // Start with the base state.

        // Apply each mutation sequentially. The order might matter for certain effects (e.g., flat bonuses before percentage).
        foreach (var mutation in _activeMutations)
        {
            currentState = mutation.Apply(currentState);
        }
        return currentState;
    }
}


// --- 6. CharacterMutator (MonoBehaviour for Unity Integration) ---
// This MonoBehaviour integrates the MutationSystem into a Unity game object.
// It manages character attributes, applies mutation effects, and handles temporary durations.
public class CharacterMutator : MonoBehaviour
{
    [Header("Base Attributes")]
    [Tooltip("The initial, unmodified attributes of the character.")]
    [SerializeField] private CharacterAttributeData _baseAttributes = new CharacterAttributeData(100, 5, 10, 2);

    // The current calculated attributes after all mutations.
    // This is not serialized directly as it's derived data, but shown in debug logs.
    private CharacterAttributeData _currentAttributes;
    public CharacterAttributeData CurrentAttributes => _currentAttributes; // Public accessor

    // The core mutation system instance.
    private MutationSystem<CharacterAttributeData> _mutationSystem;

    // Dictionary to keep track of temporary mutations and their corresponding coroutines for removal.
    private Dictionary<string, Coroutine> _activeTemporaryEffects = new Dictionary<string, Coroutine>();

    // Public properties to easily access calculated attributes from other components without exposing the raw struct.
    public float CurrentHealth => _currentAttributes.currentHealth;
    public float CurrentSpeed => _currentAttributes.currentSpeed;
    public float CurrentDamage => _currentAttributes.currentDamage;
    public float CurrentDefense => _currentAttributes.currentDefense;

    void Awake()
    {
        _mutationSystem = new MutationSystem<CharacterAttributeData>();
        // Subscribe to changes in the mutation system to automatically recalculate attributes.
        _mutationSystem.OnMutationsChanged += RecalculateAttributes;

        // Initialize current attributes to base attributes on Awake.
        _currentAttributes = _baseAttributes;
        _currentAttributes.ResetCurrentToBases(); // Ensure current values reflect base before any mutations.
    }

    void Start()
    {
        // Perform an initial calculation after all components might have initialized.
        RecalculateAttributes();
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks when the GameObject is destroyed.
        if (_mutationSystem != null)
        {
            _mutationSystem.OnMutationsChanged -= RecalculateAttributes;
        }
    }

    /// <summary>
    /// Recalculates all character attributes by applying all active mutations to the base attributes.
    /// This method is called automatically when mutations are added or removed (via OnMutationsChanged event).
    /// </summary>
    private void RecalculateAttributes()
    {
        // Start with a fresh copy of the base attributes.
        // This is crucial: don't mutate _baseAttributes directly, always work on a copy.
        CharacterAttributeData tempBase = _baseAttributes;
        tempBase.ResetCurrentToBases(); // Ensure current values are reset to base before applying mutations.

        // Apply all active mutations to the temporary base attributes.
        _currentAttributes = _mutationSystem.GetMutatedState(tempBase);
        
        Debug.Log($"Recalculated Attributes for {gameObject.name}: {_currentAttributes}");
        // Optional: Trigger an event here for other systems to react to attribute changes.
        // Example: OnAttributesUpdated?.Invoke(_currentAttributes);
    }

    /// <summary>
    /// Adds a new mutation effect to the character.
    /// </summary>
    /// <param name="mutationSO">The ScriptableObject defining the mutation to add.</param>
    /// <param name="duration">How long the mutation should last in seconds. 0 for permanent effects.</param>
    public void AddEffect(CharacterMutationSO mutationSO, float duration = 0f)
    {
        if (mutationSO == null)
        {
            Debug.LogWarning("Attempted to add a null mutation SO. Aborting.", this);
            return;
        }

        // Create the actual IMutation instance from the ScriptableObject definition.
        IMutation<CharacterAttributeData> mutation = mutationSO.GetMutation();

        // If there's an existing temporary effect with the same ID, stop its timer.
        // This ensures that applying the same effect refreshes its duration.
        if (_activeTemporaryEffects.ContainsKey(mutation.Id))
        {
            StopCoroutine(_activeTemporaryEffects[mutation.Id]);
            _activeTemporaryEffects.Remove(mutation.Id); 
            Debug.Log($"Stopped existing temporary effect for ID: {mutation.Id} to apply a new one.");
        }

        // Add the mutation to the core mutation system.
        _mutationSystem.AddMutation(mutation);

        // If a duration is specified, start a coroutine to remove the mutation after that time.
        if (duration > 0f)
        {
            Coroutine timer = StartCoroutine(RemoveMutationAfterDuration(mutation.Id, duration));
            _activeTemporaryEffects.Add(mutation.Id, timer);
            Debug.Log($"Started temporary effect '{mutation.DisplayName}' (ID: {mutation.Id}) for {duration} seconds.");
        }
    }

    /// <summary>
    /// Removes a mutation effect from the character by its ID.
    /// </summary>
    /// <param name="mutationId">The ID of the mutation to remove.</param>
    public void RemoveEffect(string mutationId)
    {
        // Stop the associated coroutine if it's a temporary effect that's still running.
        if (_activeTemporaryEffects.ContainsKey(mutationId))
        {
            StopCoroutine(_activeTemporaryEffects[mutationId]);
            _activeTemporaryEffects.Remove(mutationId);
        }
        _mutationSystem.RemoveMutation(mutationId); // Remove from the core system.
    }

    // Coroutine to handle timed mutation removal.
    private IEnumerator RemoveMutationAfterDuration(string mutationId, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveEffect(mutationId); // Use the public method to ensure proper cleanup.
        Debug.Log($"Temporary effect for ID: {mutationId} ended after {duration} seconds.");
    }

    // --- Example Usage Methods (for demonstration in editor/runtime) ---
    [ContextMenu("Add Random Speed Buff (5s)")]
    public void AddRandomSpeedBuff()
    {
        // In a real project, you would typically load a pre-made SO_SpeedBuff asset.
        // For demonstration, we create an instance dynamically.
        SO_SpeedBuff speedBuffSO = ScriptableObject.CreateInstance<SO_SpeedBuff>();
        // Manually set private fields using reflection for demonstration.
        // A better approach would be a public Init method on the SO if creating dynamically often.
        speedBuffSO.name = $"RandomSpeedBuff_{Guid.NewGuid().ToString().Substring(0, 4)}";
        speedBuffSO.GetType().GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(speedBuffSO, speedBuffSO.name);
        speedBuffSO.GetType().GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(speedBuffSO, "Random Speed Buff");
        speedBuffSO.GetType().GetField("_flatSpeedModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(speedBuffSO, UnityEngine.Random.Range(1f, 5f));
        speedBuffSO.GetType().GetField("_percentageSpeedModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(speedBuffSO, UnityEngine.Random.Range(0f, 0.3f)); // 0-30%
        
        AddEffect(speedBuffSO, 5f); // 5-second duration
        // Important: If you create SOs dynamically, you might need to destroy them if they are not meant to persist.
        // For this context menu demo, they are ephemeral.
    }

    [ContextMenu("Add Random Damage Buff (Permanent)")]
    public void AddRandomDamageBuffPermanent()
    {
        SO_DamageBoost damageBoostSO = ScriptableObject.CreateInstance<SO_DamageBoost>();
        damageBoostSO.name = $"RandomDamageBoost_{Guid.NewGuid().ToString().Substring(0, 4)}";
        damageBoostSO.GetType().GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(damageBoostSO, damageBoostSO.name);
        damageBoostSO.GetType().GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(damageBoostSO, "Random Damage Boost");
        damageBoostSO.GetType().GetField("_flatDamageModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(damageBoostSO, UnityEngine.Random.Range(5f, 15f));
        damageBoostSO.GetType().GetField("_percentageDamageModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(damageBoostSO, UnityEngine.Random.Range(0f, 0.5f)); // 0-50%
        
        AddEffect(damageBoostSO); // No duration means permanent
    }
    
    [ContextMenu("Remove All Temporary Effects")]
    public void RemoveAllTemporaryEffects()
    {
        // Get a copy of keys to avoid modifying collection while iterating
        string[] effectIds = new string[_activeTemporaryEffects.Keys.Count];
        _activeTemporaryEffects.Keys.CopyTo(effectIds, 0);

        foreach (string id in effectIds)
        {
            RemoveEffect(id); // This will stop coroutine and remove from mutation system
        }
        Debug.Log("All temporary effects removed.");
    }

    // This method could be triggered by a UI button or another script.
    [ContextMenu("Debug Log Current Attributes")]
    public void DebugLogCurrentAttributes()
    {
        Debug.Log($"--- Current Character Attributes for {gameObject.name} ---\n" +
                  $"- Health: {CurrentHealth:F1}\n" +
                  $"- Speed: {CurrentSpeed:F1}\n" +
                  $"- Damage: {CurrentDamage:F1}\n" +
                  $"- Defense: {CurrentDefense:F1}");
        Debug.Log($"--- Active Mutations ({_mutationSystem.ActiveMutations.Count}) ---");
        if (_mutationSystem.ActiveMutations.Count == 0)
        {
            Debug.Log("- No active mutations.");
        }
        foreach (var mut in _mutationSystem.ActiveMutations)
        {
            Debug.Log($"- {mut.DisplayName} (ID: {mut.Id})");
        }
        Debug.Log("--------------------------------------------------");
    }
}
```

---

### How to Implement and Test in Unity:

1.  **Create the Script:** Save the code above as `MutationSystemExample.cs` in your Unity project's `Assets` folder.
2.  **Create Mutation Assets:**
    *   In the Unity Editor, right-click in your Project window.
    *   Go to `Create -> MutationSystem`. You'll see "Damage Boost", "Defense Buff", and "Speed Buff".
    *   Create a few instances of these:
        *   `SO_SpeedBuff` (e.g., name it `HastePotion`). Set `Flat Speed Modifier` to `2` and `Percentage Speed Modifier` to `0.15` (15%).
        *   `SO_DamageBoost` (e.g., name it `BerserkerRage`). Set `Flat Damage Modifier` to `10` and `Percentage Damage Modifier` to `0.25` (25%).
        *   `SO_SpeedBuff` (e.g., name it `SlowDebuff`). Set `Flat Speed Modifier` to `-1` and `Percentage Speed Modifier` to `-0.3` (-30%).
3.  **Create a Game Object:**
    *   In your scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty). Name it `PlayerCharacter`.
    *   Add the `CharacterMutator` component to this `PlayerCharacter` GameObject (drag the script onto it, or use "Add Component" in the Inspector).
4.  **Configure Base Attributes:**
    *   Select the `PlayerCharacter` in the Hierarchy. In its Inspector, you'll see the `CharacterMutator` component.
    *   Set the `Base Attributes` (e.g., `Base Health: 100`, `Base Speed: 5`, `Base Damage: 10`, `Base Defense: 2`).
5.  **Run and Test:**
    *   Play the scene.
    *   Select the `PlayerCharacter` GameObject in the Hierarchy while the game is running.
    *   In the `CharacterMutator` component in the Inspector, right-click. You'll see several Context Menu options:
        *   `Add Random Speed Buff (5s)`: Adds a temporary speed boost. Watch the console for logs and the component's values (if you expand `Current Attributes`).
        *   `Add Random Damage Buff (Permanent)`: Adds a permanent damage buff.
        *   `Debug Log Current Attributes`: Logs all current attributes and active mutations to the console.
        *   `Remove All Temporary Effects`: Clears all timed effects.
    *   Experiment with adding multiple effects, including the permanent ones. Observe how `Recalculated Attributes` logs show the combined effect.

### Example Usage in Another Script:

You can easily apply these mutations from any other C# script:

```csharp
using UnityEngine;

public class EffectTrigger : MonoBehaviour
{
    public CharacterMutator targetCharacter; // Assign your PlayerCharacter GameObject here
    public SO_SpeedBuff hastePotion;        // Drag your 'HastePotion' ScriptableObject here
    public SO_DamageBoost berserkerPotion;  // Drag your 'BerserkerRage' ScriptableObject here
    public SO_SpeedBuff slowCurse;          // Drag your 'SlowDebuff' ScriptableObject here

    [Header("Durations")]
    public float hasteDuration = 10f;
    public float berserkDuration = 5f;
    public float slowDuration = 15f;

    void Update()
    {
        if (targetCharacter == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Applying Haste!");
            targetCharacter.AddEffect(hastePotion, hasteDuration);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Applying Berserker Rage!");
            targetCharacter.AddEffect(berserkerPotion, berserkDuration);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Applying Slow Curse!");
            targetCharacter.AddEffect(slowCurse, slowDuration);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            targetCharacter.DebugLogCurrentAttributes();
        }
    }
}
```

1.  Create a new Empty GameObject in your scene (e.g., `EffectManager`).
2.  Attach the `EffectTrigger` script to it.
3.  Drag your `PlayerCharacter` GameObject to the `Target Character` slot in the `EffectTrigger` Inspector.
4.  Drag your `HastePotion`, `BerserkerRage`, and `SlowDebuff` ScriptableObjects to their respective slots.
5.  Run the game and press `1`, `2`, `3` to apply effects, and `Space` to log current attributes.

This setup provides a robust and scalable solution for managing complex status effects and attribute modifications in your Unity games.