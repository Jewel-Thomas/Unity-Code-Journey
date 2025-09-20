// Unity Design Pattern Example: ModularCharacterSystem
// This script demonstrates the ModularCharacterSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Modular Character System** design pattern in Unity. This pattern allows you to compose a character's features (appearance, abilities, stats, etc.) from interchangeable, independent modules. This promotes flexibility, reusability, and easier maintenance compared to monolithic character classes.

**Key Concepts:**

1.  **`ModularCharacter` (MonoBehaviour):** The central component attached to your character GameObject. It manages a collection of `CharacterModule` ScriptableObjects.
2.  **`CharacterModule` (ScriptableObject Base Class):** An abstract base class for all modules. Using ScriptableObjects allows you to define module types as assets in your project, making them easy to create, configure, and assign in the Inspector. Each module can have its own data and logic.
3.  **Specific Module Implementations:**
    *   **`VisualModule`:** Handles adding visual parts (e.g., head, torso, legs) to the character.
    *   **`AbilityModule`:** Defines character abilities (e.g., Jump, Attack, Dash). Includes cooldown logic.
    *   **`StatModule`:** Modifies character statistics.
4.  **Runtime Instantiation:** When a `ModularCharacter` is initialized, it creates runtime instances of its assigned `CharacterModule` ScriptableObjects. This ensures each character has its own independent state for its modules (e.g., separate cooldowns for abilities).
5.  **Dynamic Modification:** The system allows for adding or removing modules at runtime, enabling features like equipping new gear, learning new abilities, or applying temporary buffs/debuffs.

---

### Project Setup Instructions for Unity:

1.  **Create C# Scripts:** Create new C# scripts in your Unity project (e.g., in a "Scripts/ModularCharacter" folder) and paste the code below into the respective files.
    *   `CharacterModule.cs`
    *   `VisualModule.cs`
    *   `AbilityModule.cs`
    *   `JumpAbility.cs`
    *   `StatModule.cs`
    *   `ModularCharacter.cs`
    *   `PlayerController.cs`
2.  **Create Character GameObject:**
    *   In your scene, create an empty GameObject and name it `PlayerCharacter`.
    *   Add the `ModularCharacter.cs` script to `PlayerCharacter`.
    *   Add a `Rigidbody` component to `PlayerCharacter`. For demonstration, set its `Constraints` -> `Freeze Rotation` (X, Y, Z) to prevent it from toppling.
3.  **Create "Socket" GameObjects:**
    *   As children of `PlayerCharacter`, create two empty GameObjects: `HeadSocket` and `BodySocket`. Position them appropriately (e.g., `HeadSocket` slightly above the center of `PlayerCharacter`, `BodySocket` at the center). These will serve as attachment points for `VisualModule`s.
4.  **Create Ground Layer:**
    *   Go to `Edit > Project Settings > Tags and Layers`.
    *   Under `Layers`, add a new User Layer, e.g., named `Ground`.
    *   Create a simple 3D Plane or Cube in your scene to act as ground. Assign the `Ground` layer to it in the Inspector.
5.  **Create Module Assets:**
    *   In your Project window, right-click -> `Create` -> `Modular Character`. You will see options for `Visual Module`, `Ability Module`, `Stat Module`, and `Abilities/Jump Ability`.
    *   **Visual Modules:**
        *   Create `Visual Module` and name it `HeadVisual`.
        *   Assign a simple 3D model (e.g., Unity's default Cube or Sphere, or your custom head model) to its `Visual Prefab` field.
        *   Set `Attachment Socket Tag` to `HeadSocket`.
        *   Create another `Visual Module`, name it `BodyVisual`.
        *   Assign a simple 3D model (e.g., another Cube) to its `Visual Prefab` field.
        *   Set `Attachment Socket Tag` to `BodySocket`.
    *   **Ability Module:**
        *   Create `Abilities/Jump Ability` and name it `PlayerJumpAbility`.
        *   Set `Jump Force` to a reasonable value (e.g., `5`).
        *   Select the `Ground` layer from the `Ground Layer` dropdown.
    *   **Stat Module:**
        *   Create `Stat Module` and name it `BasicStats`.
        *   Set some bonus values (e.g., `Health Bonus = 25`, `Speed Bonus = 2`).
6.  **Assign Modules to Character:**
    *   Select `PlayerCharacter` in the Hierarchy.
    *   In the Inspector, expand the `Character Modules` list on the `ModularCharacter` component.
    *   Drag and drop `HeadVisual`, `BodyVisual`, `PlayerJumpAbility`, and `BasicStats` assets from your Project window into this list.
7.  **Create Player Controller:**
    *   Create an empty GameObject in your scene and name it `GameManager` (or `PlayerInput`).
    *   Add the `PlayerController.cs` script to `GameManager`.
    *   Drag `PlayerCharacter` from the Hierarchy into the `Target Character` slot of the `PlayerController` component.
8.  **Run the Scene:**
    *   Play the scene.
    *   You should see the `HeadVisual` and `BodyVisual` prefabs instantiated on your `PlayerCharacter` at the `HeadSocket` and `BodySocket` positions.
    *   Check the Console for initialization messages.
    *   Press **Spacebar** to make the character jump (using `PlayerJumpAbility`).
    *   Press **R** to dynamically remove the head visual.
    *   Press **A** to dynamically add a new generic ability.

---

### 1. `CharacterModule.cs` (Base Module)

```csharp
using UnityEngine;

// This is the abstract base class for all character modules.
// By inheriting from ScriptableObject, modules can be created as assets in the Unity editor.
// This allows for easy configuration and reuse across different character prefabs.
public abstract class CharacterModule : ScriptableObject
{
    [Tooltip("A display name for this module.")]
    public string moduleName = "New Module";

    // A reference to the owning character, set during initialization.
    // This allows modules to interact with the character they are attached to.
    protected ModularCharacter OwningCharacter { get; private set; }

    /// <summary>
    /// Called when the module is first added to and initialized by the ModularCharacter.
    /// This is where the module sets up its initial state, hooks into character events, etc.
    /// </summary>
    /// <param name="character">The ModularCharacter instance this module is attached to.</param>
    public virtual void Initialize(ModularCharacter character)
    {
        OwningCharacter = character;
        Debug.Log($"[{character.name}] Module '{moduleName}' ({GetType().Name}) initialized.");
    }

    /// <summary>
    /// Called when the module is removed from or the ModularCharacter is destroyed.
    /// This is where the module cleans up any resources, unsubscribes from events, etc.
    /// </summary>
    public virtual void Deinitialize()
    {
        Debug.Log($"[{OwningCharacter.name}] Module '{moduleName}' ({GetType().Name}) deinitialized.");
        OwningCharacter = null;
    }

    /// <summary>
    /// Optional method for modules that need per-frame updates.
    /// Called by the ModularCharacter's Update method.
    /// </summary>
    /// <param name="deltaTime">The time since the last frame.</param>
    public virtual void Tick(float deltaTime) { }
}
```

---

### 2. `VisualModule.cs` (Example Module: Visual)

```csharp
using UnityEngine;

// This module handles adding visual components to the character.
// Examples: equipping a helmet, changing a torso, adding particle effects.
[CreateAssetMenu(fileName = "NewVisualModule", menuName = "Modular Character/Visual Module")]
public class VisualModule : CharacterModule
{
    [Tooltip("The GameObject prefab to instantiate as a visual part.")]
    public GameObject visualPrefab;

    [Tooltip("The name or tag of the child transform on the character to attach this visual to.")]
    public string attachmentSocketTag = "DefaultSocket";

    private GameObject _instantiatedVisual; // Reference to the instantiated visual GameObject

    /// <summary>
    /// Initializes the visual module: instantiates the prefab and attaches it to the specified socket.
    /// </summary>
    /// <param name="character">The character to attach the visual to.</param>
    public override void Initialize(ModularCharacter character)
    {
        base.Initialize(character); // Call base initialization

        if (visualPrefab != null)
        {
            // Try to find the attachment socket on the character.
            // For more complex character rigs, this might involve a dedicated socket manager.
            Transform attachmentSocket = character.transform.Find(attachmentSocketTag); 
            if (attachmentSocket == null)
            {
                Debug.LogWarning($"VisualModule '{moduleName}': No attachment socket '{attachmentSocketTag}' found on {character.name}. Attaching to root transform.");
                attachmentSocket = character.transform; // Fallback to character's root
            }

            // Instantiate the visual prefab as a child of the socket.
            _instantiatedVisual = Instantiate(visualPrefab, attachmentSocket);
            _instantiatedVisual.transform.localPosition = Vector3.zero; // Reset local position
            _instantiatedVisual.transform.localRotation = Quaternion.identity; // Reset local rotation
            Debug.Log($"[{character.name}] Visual module '{moduleName}' instantiated '{visualPrefab.name}' on '{attachmentSocket.name}'.");
        }
        else
        {
            Debug.LogWarning($"[{character.name}] Visual module '{moduleName}' has no visual prefab assigned.");
        }
    }

    /// <summary>
    /// Deinitializes the visual module: destroys the instantiated visual GameObject.
    /// </summary>
    public override void Deinitialize()
    {
        if (_instantiatedVisual != null)
        {
            Destroy(_instantiatedVisual); // Clean up the instantiated visual
            _instantiatedVisual = null;
        }
        base.Deinitialize(); // Call base deinitialization
    }
}
```

---

### 3. `AbilityModule.cs` (Example Module: Base Ability)

```csharp
using UnityEngine;

// This is the abstract base class for all character abilities.
// It provides common functionality like cooldowns.
[CreateAssetMenu(fileName = "NewAbilityModule", menuName = "Modular Character/Ability Module")]
public class AbilityModule : CharacterModule
{
    [Tooltip("The display name for this ability.")]
    public string abilityName = "Default Ability";

    [Tooltip("The time (in seconds) this ability is on cooldown after use.")]
    public float cooldownTime = 1.0f;

    protected float _currentCooldown = 0.0f; // Internal tracker for current cooldown

    // Property to check if the ability is currently on cooldown.
    public bool IsOnCooldown => _currentCooldown > 0;

    /// <summary>
    /// Called every frame by the ModularCharacter to update module logic.
    /// Here, we decrement the cooldown timer.
    /// </summary>
    /// <param name="deltaTime">The time since the last frame.</param>
    public override void Tick(float deltaTime)
    {
        if (_currentCooldown > 0)
        {
            _currentCooldown -= deltaTime;
            if (_currentCooldown < 0) _currentCooldown = 0; // Ensure it doesn't go below zero
        }
    }

    /// <summary>
    /// Attempts to activate the ability.
    /// Checks for cooldown and calls the protected Activate() method if available.
    /// </summary>
    /// <returns>True if the ability was activated, false otherwise (e.g., on cooldown).</returns>
    public virtual bool TryActivate()
    {
        if (!IsOnCooldown)
        {
            Activate(); // Perform the ability's specific action
            _currentCooldown = cooldownTime; // Set cooldown
            return true;
        }
        Debug.Log($"[{OwningCharacter.name}] Ability '{abilityName}' is on cooldown. ({_currentCooldown:F1}s remaining)");
        return false;
    }

    /// <summary>
    /// The core logic for what the ability does. This method should be overridden by derived classes.
    /// </summary>
    protected virtual void Activate()
    {
        Debug.Log($"[{OwningCharacter.name}] Activated '{abilityName}'!");
        // Derived classes will implement the actual ability logic here.
        // e.g., trigger animations, deal damage, apply status effects.
    }
}
```

---

### 4. `JumpAbility.cs` (Example Module: Specific Ability)

```csharp
using UnityEngine;

// A specific implementation of an AbilityModule for jumping.
[CreateAssetMenu(fileName = "NewJumpAbility", menuName = "Modular Character/Abilities/Jump Ability")]
public class JumpAbility : AbilityModule
{
    [Tooltip("The force applied when the character jumps.")]
    public float jumpForce = 5f;

    [Tooltip("The LayerMask to use for checking if the character is grounded.")]
    public LayerMask groundLayer;

    [Tooltip("The radius of the sphere used for ground checking.")]
    public float groundCheckRadius = 0.4f;

    [Tooltip("The offset from the character's origin for the ground check.")]
    public Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);

    private Rigidbody _rb; // Reference to the character's Rigidbody

    /// <summary>
    /// Initializes the JumpAbility. Gets a reference to the character's Rigidbody.
    /// </summary>
    /// <param name="character">The character this ability is attached to.</param>
    public override void Initialize(ModularCharacter character)
    {
        base.Initialize(character); // Call base initialization
        _rb = OwningCharacter.GetComponent<Rigidbody>(); // Get the Rigidbody component
        if (_rb == null)
        {
            Debug.LogWarning($"[{character.name}] JumpAbility requires a Rigidbody component on the character to function.");
        }
        base.abilityName = "Jump"; // Override default ability name
    }

    /// <summary>
    /// The core logic for the jump ability. Applies an upward force if grounded.
    /// </summary>
    protected override void Activate()
    {
        if (_rb != null && IsGrounded())
        {
            Debug.Log($"[{OwningCharacter.name}] Performs a {abilityName} with force {jumpForce}!");
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Apply jump force
        }
        else if (_rb == null)
        {
            Debug.LogWarning($"[{OwningCharacter.name}] Cannot {abilityName}: No Rigidbody found.");
        }
        else
        {
            Debug.Log($"[{OwningCharacter.name}] Cannot {abilityName}: Not grounded.");
        }
    }

    /// <summary>
    /// Performs a simple sphere cast to check if the character is currently grounded.
    /// This is a basic example; a real game might use more sophisticated ground detection.
    /// </summary>
    /// <returns>True if grounded, false otherwise.</returns>
    private bool IsGrounded()
    {
        // Sphere cast downwards from a slightly elevated position to detect ground.
        // Adjust groundCheckRadius and groundCheckOffset based on your character's size.
        return Physics.SphereCast(
            OwningCharacter.transform.position + groundCheckOffset,
            groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            0.1f, // Max distance for the cast
            groundLayer);
    }
}
```

---

### 5. `StatModule.cs` (Example Module: Stats)

```csharp
using UnityEngine;

// This module defines and applies passive stat bonuses to the character.
[CreateAssetMenu(fileName = "NewStatModule", menuName = "Modular Character/Stat Module")]
public class StatModule : CharacterModule
{
    [Tooltip("Bonus health provided by this module.")]
    public float healthBonus = 0f;

    [Tooltip("Bonus movement speed provided by this module.")]
    public float speedBonus = 0f;

    [Tooltip("Bonus damage provided by this module.")]
    public float damageBonus = 0f;

    /// <summary>
    /// Initializes the stat module. In a real game, this would interface with a StatManager
    /// or directly modify the character's base stats. For this example, it just logs.
    /// </summary>
    /// <param name="character">The character whose stats are affected.</param>
    public override void Initialize(ModularCharacter character)
    {
        base.Initialize(character); // Call base initialization
        // In a real project, the ModularCharacter would likely have a StatManager component
        // or methods to modify its stats, and this module would call those methods.
        // Example: character.StatManager.ApplyBonus(this);
        Debug.Log($"[{character.name}] Stat module '{moduleName}' initialized. Applied bonuses: " +
                  $"Health +{healthBonus}, Speed +{speedBonus}, Damage +{damageBonus}.");
    }

    /// <summary>
    /// Deinitializes the stat module. Cleans up applied bonuses.
    /// </summary>
    public override void Deinitialize()
    {
        // In a real project, this would remove the bonuses applied during Initialize.
        // Example: OwningCharacter.StatManager.RemoveBonus(this);
        Debug.Log($"[{OwningCharacter.name}] Stat module '{moduleName}' deinitialized. Bonuses removed.");
        base.Deinitialize(); // Call base deinitialization
    }
}
```

---

### 6. `ModularCharacter.cs` (Core Character Component)

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .Cast<T>() and .ToList()

/// <summary>
/// The central component for a modular character.
/// It manages a collection of CharacterModule ScriptableObjects that define the character's features.
/// </summary>
public class ModularCharacter : MonoBehaviour
{
    [Tooltip("The list of module assets defining this character's appearance, abilities, and stats.")]
    // Serialized field to allow assigning modules in the Unity Inspector.
    // These are references to the ScriptableObject assets.
    public List<CharacterModule> characterModules = new List<CharacterModule>();

    // A dictionary to store runtime instances of modules, keyed by their System.Type.
    // This allows for quick lookup and access to modules.
    private Dictionary<System.Type, List<CharacterModule>> _moduleLookup = new Dictionary<System.Type, List<CharacterModule>>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes all modules assigned to the character.
    /// </summary>
    void Awake()
    {
        InitializeAllModules();
    }

    /// <summary>
    /// Called every frame.
    /// Iterates through all active modules and calls their Tick() method.
    /// This allows modules to perform per-frame logic (e.g., cooldowns, passive effects).
    /// </summary>
    void Update()
    {
        foreach (var moduleList in _moduleLookup.Values)
        {
            foreach (var module in moduleList)
            {
                module.Tick(Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// Deinitializes all modules and cleans up their runtime instances.
    /// </summary>
    void OnDestroy()
    {
        DeinitializeAllModules();
    }

    /// <summary>
    /// Initializes all modules listed in `characterModules`.
    /// Creates runtime instances of the ScriptableObject modules to ensure unique state per character.
    /// </summary>
    public void InitializeAllModules()
    {
        // Clear any existing modules to prevent duplicates on re-initialization.
        // This is important if InitializeAllModules is called multiple times.
        DeinitializeAllModules(); 
        _moduleLookup.Clear();

        foreach (var moduleAsset in characterModules)
        {
            if (moduleAsset == null)
            {
                Debug.LogWarning($"[{name}] ModularCharacter: Found a null module asset in the list. Skipping.");
                continue;
            }

            // Create a runtime instance of the ScriptableObject module.
            // This is crucial! Without Instantiate(), all characters using the same SO asset
            // would share the same module instance and its state (e.g., cooldowns).
            CharacterModule runtimeModule = Instantiate(moduleAsset);
            AddModuleInternal(runtimeModule);
        }
        Debug.Log($"[{name}] All initial modules processed.");
    }

    /// <summary>
    /// Deinitializes all currently active runtime modules and destroys their instances.
    /// </summary>
    public void DeinitializeAllModules()
    {
        foreach (var moduleList in _moduleLookup.Values)
        {
            foreach (var module in moduleList)
            {
                module.Deinitialize(); // Call module-specific cleanup
                Destroy(module); // Destroy the runtime ScriptableObject instance
            }
        }
        _moduleLookup.Clear(); // Clear the lookup dictionary
        characterModules.Clear(); // Also clear the list of assets for a full reset (optional, depending on desired behavior)
    }

    /// <summary>
    /// Adds a new module to the character at runtime.
    /// </summary>
    /// <param name="newModuleAsset">The ScriptableObject asset of the module to add.</param>
    public void AddModule(CharacterModule newModuleAsset)
    {
        if (newModuleAsset == null)
        {
            Debug.LogWarning($"[{name}] Attempted to add a null module asset.");
            return;
        }

        // Create a runtime instance of the new module asset.
        CharacterModule runtimeModule = Instantiate(newModuleAsset);
        AddModuleInternal(runtimeModule);

        // Also add the asset to the public list, so it persists if the character is saved/loaded.
        // This ensures the Inspector list stays in sync.
        if (!characterModules.Contains(newModuleAsset))
        {
             characterModules.Add(newModuleAsset);
        }
    }

    /// <summary>
    /// Internal method to add a runtime module instance to the lookup dictionary and initialize it.
    /// </summary>
    /// <param name="runtimeModule">The already instantiated module to add.</param>
    private void AddModuleInternal(CharacterModule runtimeModule)
    {
        // Get the actual runtime type of the module.
        // This allows derived modules (e.g., JumpAbility) to be looked up by their specific type.
        System.Type moduleType = runtimeModule.GetType(); 

        if (!_moduleLookup.ContainsKey(moduleType))
        {
            _moduleLookup[moduleType] = new List<CharacterModule>();
        }
        _moduleLookup[moduleType].Add(runtimeModule); // Add to the list for its type
        runtimeModule.Initialize(this); // Initialize the module, passing this character as owner
        Debug.Log($"[{name}] Added and initialized module: '{runtimeModule.moduleName}' (Type: {moduleType.Name})");
    }

    /// <summary>
    /// Removes a specific module instance from the character at runtime.
    /// </summary>
    /// <typeparam name="T">The specific type of the module to remove.</typeparam>
    /// <param name="moduleToRemove">The actual runtime instance of the module to remove.</param>
    /// <returns>True if the module was found and removed, false otherwise.</returns>
    public bool RemoveModule<T>(T moduleToRemove) where T : CharacterModule
    {
        if (moduleToRemove == null) return false;

        System.Type moduleType = moduleToRemove.GetType();
        if (_moduleLookup.TryGetValue(moduleType, out List<CharacterModule> modules))
        {
            if (modules.Remove(moduleToRemove)) // Remove the specific instance
            {
                moduleToRemove.Deinitialize(); // Deinitialize the module
                Destroy(moduleToRemove); // Destroy its runtime instance
                Debug.Log($"[{name}] Removed and deinitialized module: '{moduleToRemove.moduleName}' (Type: {moduleType.Name})");

                if (modules.Count == 0) // If no more modules of this type, remove the list from the dictionary
                {
                    _moduleLookup.Remove(moduleType);
                }

                // Also remove the corresponding asset from the public list if it's there
                if (characterModules.Contains(moduleToRemove))
                {
                    characterModules.Remove(moduleToRemove);
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Retrieves the first module of a specific type.
    /// Useful for unique modules (e.g., a specific jump ability).
    /// </summary>
    /// <typeparam name="T">The type of the module to retrieve.</typeparam>
    /// <returns>The first module of the specified type, or null if not found.</returns>
    public T GetModule<T>() where T : CharacterModule
    {
        // Try to get modules of the exact type T.
        // If that fails, search for modules whose type is assignable from T (i.e., T is a base class or interface).
        if (_moduleLookup.TryGetValue(typeof(T), out List<CharacterModule> modules))
        {
            return modules.FirstOrDefault() as T;
        }

        // If not found by exact type, search for assignable types (e.g., getting all AbilityModules might return a JumpAbility)
        foreach (var kvp in _moduleLookup)
        {
            if (typeof(T).IsAssignableFrom(kvp.Key)) // If T is a base type/interface of kvp.Key
            {
                 // Return the first one that matches the type constraint
                return kvp.Value.FirstOrDefault(m => m is T) as T;
            }
        }

        return null;
    }


    /// <summary>
    /// Retrieves all modules of a specific type.
    /// Useful for multiple modules of the same type (e.g., multiple buffs, multiple passive abilities).
    /// </summary>
    /// <typeparam name="T">The type of the modules to retrieve.</typeparam>
    /// <returns>A list of all modules of the specified type.</returns>
    public List<T> GetModules<T>() where T : CharacterModule
    {
        List<T> foundModules = new List<T>();

        // First, check for exact matches
        if (_moduleLookup.TryGetValue(typeof(T), out List<CharacterModule> exactModules))
        {
            foundModules.AddRange(exactModules.Cast<T>());
        }

        // Then, check for derived types that are assignable to T (e.g., JumpAbility when asking for AbilityModule)
        foreach (var kvp in _moduleLookup)
        {
            if (typeof(T).IsAssignableFrom(kvp.Key) && kvp.Key != typeof(T)) // Avoid double-adding exact matches
            {
                foundModules.AddRange(kvp.Value.Cast<T>());
            }
        }
        
        return foundModules;
    }
}
```

---

### 7. `PlayerController.cs` (Example Usage)

```csharp
using UnityEngine;
using System.Linq; // Required for LINQ extensions like .FirstOrDefault()
using System.Collections.Generic; // Required for List

/// <summary>
/// This script demonstrates how another component (e.g., a player input controller)
/// can interact with a ModularCharacter and its modules.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Tooltip("The ModularCharacter instance this controller will interact with.")]
    public ModularCharacter targetCharacter;

    [Header("Input Keys")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode ability1Key = KeyCode.Alpha1;
    public KeyCode removeHeadKey = KeyCode.R;
    public KeyCode addGenericAbilityKey = KeyCode.A;

    // Optional: Reference to a pre-made AbilityModule asset to add dynamically
    [Tooltip("Drag a generic AbilityModule SO here to add dynamically.")]
    public AbilityModule genericAbilityToAdd;

    void Update()
    {
        if (targetCharacter == null)
        {
            Debug.LogWarning("PlayerController: No target character assigned.");
            return;
        }

        // --- Example 1: Activating a specific ability module (JumpAbility) ---
        if (Input.GetKeyDown(jumpKey))
        {
            // GetModule<T>() fetches the first module of the specified type.
            // This is ideal when you expect only one instance of a specific ability.
            JumpAbility jumpAbility = targetCharacter.GetModule<JumpAbility>();
            if (jumpAbility != null)
            {
                jumpAbility.TryActivate(); // Attempt to activate the jump ability
            }
            else
            {
                Debug.Log($"[{targetCharacter.name}] Jump ability not found on character!");
            }
        }

        // --- Example 2: Activating a generic ability module (could be any AbilityModule) ---
        if (Input.GetKeyDown(ability1Key))
        {
            // GetModules<T>() fetches all modules of the specified base type.
            List<AbilityModule> abilities = targetCharacter.GetModules<AbilityModule>();
            if (abilities.Count > 0)
            {
                // For demonstration, try to activate the first ability that isn't a JumpAbility
                // (to show interaction with other potential generic abilities).
                AbilityModule genericAbility = abilities.FirstOrDefault(a => !(a is JumpAbility) && !a.IsOnCooldown);
                if (genericAbility != null)
                {
                    genericAbility.TryActivate();
                }
                else
                {
                    Debug.Log($"[{targetCharacter.name}] No other (non-jump) abilities available or all on cooldown.");
                }
            }
            else
            {
                Debug.Log($"[{targetCharacter.name}] No ability modules found on character!");
            }
        }

        // --- Example 3: Dynamically removing a module (e.g., removing a visual part) ---
        if (Input.GetKeyDown(removeHeadKey))
        {
            Debug.Log($"[{targetCharacter.name}] Attempting to remove head visual module...");
            // Find a VisualModule specifically attached to the "HeadSocket"
            VisualModule headModule = targetCharacter.GetModules<VisualModule>()
                                                   .FirstOrDefault(v => v.attachmentSocketTag == "HeadSocket");
            if (headModule != null)
            {
                targetCharacter.RemoveModule(headModule); // Remove the module instance
            }
            else
            {
                Debug.Log($"[{targetCharacter.name}] No head visual module to remove.");
            }
        }

        // --- Example 4: Dynamically adding a new module (e.g., learning a new ability) ---
        if (Input.GetKeyDown(addGenericAbilityKey))
        {
            Debug.Log($"[{targetCharacter.name}] Attempting to add a new generic ability...");
            if (genericAbilityToAdd != null)
            {
                targetCharacter.AddModule(genericAbilityToAdd); // Add the module asset
            }
            else
            {
                Debug.LogWarning("PlayerController: genericAbilityToAdd is null. Please assign an AbilityModule ScriptableObject in the Inspector.");
                // As a fallback for demonstration, create a temporary one if none is assigned
                AbilityModule tempAbility = ScriptableObject.CreateInstance<AbilityModule>();
                tempAbility.moduleName = "Temporary Generic Ability";
                tempAbility.abilityName = "Temp Attack";
                tempAbility.cooldownTime = 2f;
                targetCharacter.AddModule(tempAbility);
                Debug.Log($"[{targetCharacter.name}] Added a temporary generic ability.");
            }
        }
    }
}
```