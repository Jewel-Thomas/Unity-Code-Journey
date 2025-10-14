// Unity Design Pattern Example: PluginHotReload
// This script demonstrates the PluginHotReload pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Plugin Hot Reload** design pattern in Unity using `ScriptableObject` assets. This approach allows you to define flexible "plugins" as data assets that can be swapped, modified, and reloaded at runtime without recompiling your main game logic.

**Core Idea:**
1.  **Interface (`IPlugin`):** Defines a common contract for all plugins.
2.  **Base `ScriptableObject` (`BasePluginSO`):** An abstract `ScriptableObject` that implements `IPlugin`, providing common functionality and allowing easy asset creation.
3.  **Concrete `ScriptableObject` Plugins:** Specific `ScriptableObject` classes that inherit from `BasePluginSO` and implement unique logic.
4.  **Plugin Manager (`PluginManager`):** A `MonoBehaviour` that holds a reference to the active plugin, manages its lifecycle (initialize, execute, terminate), and allows for dynamic swapping or reloading of plugin assets at runtime.

---

### **Instructions to Use:**

1.  **Create the Scripts:** Copy each of the C# code blocks below into separate `.cs` files in your Unity project (e.g., `IPlugin.cs`, `BasePluginSO.cs`, `DamageModifierPluginSO.cs`, `StatusEffectPluginSO.cs`, `PluginManager.cs`).
2.  **Create Plugin Assets:**
    *   In the Unity Editor, right-click in your Project window.
    *   Go to `Create > Plugin > Damage Modifier`. Create a few instances, e.g., "HighDamagePlugin" (multiplier 2.0), "LowDamagePlugin" (multiplier 0.5), "NormalDamagePlugin" (multiplier 1.0).
    *   Go to `Create > Plugin > Status Effect`. Create a few instances, e.g., "StunEffectPlugin" (Type Stun, Duration 3s), "SlowEffectPlugin" (Type Slow, Magnitude 0.5), "HealOverTimePlugin" (Type Heal, Magnitude 10, Duration 5s).
3.  **Set up the PluginManager:**
    *   Create an empty GameObject in your scene (e.g., "PluginManager").
    *   Attach the `PluginManager.cs` script to it.
    *   In the Inspector for the "PluginManager" GameObject:
        *   Drag one of your created `ScriptableObject` plugin assets (e.g., "HighDamagePlugin") into the `_activePlugin` slot. This will be the initially loaded plugin.
        *   Drag a few more plugin assets into the `_availablePlugins` list (e.g., "LowDamagePlugin", "StunEffectPlugin", "HealOverTimePlugin"). These will be used for dynamic loading.
        *   Adjust `_pluginLoadIndex` to point to a different plugin in the `_availablePlugins` list if desired.
4.  **Run and Observe Hot Reloading:**
    *   Start Play Mode.
    *   Observe the Unity Console. The initial plugin (`_activePlugin`) will be initialized and its `Execute()` method will run every frame.
    *   **Hot Swap Plugin:** While playing, go to the `PluginManager` GameObject's Inspector. Drag a *different* plugin `ScriptableObject` asset (e.g., "LowDamagePlugin") from your Project window directly into the `_activePlugin` slot. You'll immediately see the old plugin terminate and the new one initialize and execute.
    *   **Hot Modify Plugin Data:** Select the *currently active* plugin asset in your Project window (e.g., "HighDamagePlugin"). In its Inspector, change a value (e.g., `Damage Multiplier` from 2.0 to 5.0). The `PluginManager`'s `Execute()` method will instantly reflect this change without recompiling or restarting.
    *   **Dynamic Load from List:** Press the **'L'** key. The plugin at the `_pluginLoadIndex` in the `_availablePlugins` list will be loaded, replacing the current one. Use **Left/Right Arrow** keys to change `_pluginLoadIndex`.
    *   **Reload Current Plugin:** Press the **'R'** key. The currently active plugin will be terminated and then re-initialized, simulating a full reload of its internal state (if applicable).
    *   **Unload Plugin:** Press the **'U'** key. The currently active plugin will be terminated and removed.

---

### **1. `IPlugin.cs`**

```csharp
using UnityEngine; // Included for consistency in Unity projects, though not strictly required for an interface.

/// <summary>
/// The core interface for our plugins.
/// Defines the contract that all hot-reloadable plugins must adhere to.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the display name of the plugin.
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// Called once when the plugin is loaded and activated.
    /// Use this for initial setup, resource loading, etc.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Called every frame (or periodically) while the plugin is active.
    /// This is where the plugin's main logic resides.
    /// </summary>
    void Execute();

    /// <summary>
    /// Called once when the plugin is unloaded or swapped out.
    /// Use this for cleanup, releasing resources, etc.
    /// </summary>
    void Terminate();
}
```

### **2. `BasePluginSO.cs`**

```csharp
using UnityEngine;

/// <summary>
/// An abstract base class for all ScriptableObject-based plugins.
/// It implements the IPlugin interface and provides common properties and default behaviors.
/// Concrete plugins will inherit from this class.
/// </summary>
public abstract class BasePluginSO : ScriptableObject, IPlugin
{
    // --- Editor-exposed Properties ---
    [Tooltip("A user-friendly name for this plugin.")]
    [SerializeField] private string _pluginName = "Unnamed Plugin";

    /// <summary>
    /// Public accessor for the plugin's name.
    /// </summary>
    public string PluginName => _pluginName;

    // --- IPlugin Implementations (with default behavior) ---

    /// <summary>
    /// Default initialization for all plugins. Logs a message.
    /// Derived classes can override this and call base.Initialize().
    /// </summary>
    public virtual void Initialize()
    {
        Debug.Log($"<color=blue>[{PluginName}]</color> Initialized.");
    }

    /// <summary>
    /// Abstract method that must be implemented by concrete plugin classes.
    /// This is where the plugin's specific logic will be defined.
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// Default termination for all plugins. Logs a message.
    /// Derived classes can override this and call base.Terminate().
    /// </summary>
    public virtual void Terminate()
    {
        Debug.Log($"<color=blue>[{PluginName}]</color> Terminated.");
    }
}
```

### **3. `DamageModifierPluginSO.cs`**

```csharp
using UnityEngine;

/// <summary>
/// A concrete plugin example: modifies damage.
/// This ScriptableObject can be created as an asset and contains data
/// that defines how it functions.
/// </summary>
[CreateAssetMenu(fileName = "DamageModifierPlugin", menuName = "Plugin/Damage Modifier")]
public class DamageModifierPluginSO : BasePluginSO
{
    // --- Plugin-specific Data ---
    [Tooltip("The multiplier to apply to damage (e.g., 1.5 for 50% extra damage).")]
    [SerializeField] private float _damageMultiplier = 1.0f;

    [Tooltip("A descriptive text for the effect.")]
    [SerializeField] private string _effectDescription = "Applies a general damage modifier.";

    /// <summary>
    /// Public accessor for the damage multiplier.
    /// </summary>
    public float DamageMultiplier => _damageMultiplier;

    /// <summary>
    /// Public accessor for the effect description.
    /// </summary>
    public string EffectDescription => _effectDescription;

    // --- IPlugin Implementations ---

    /// <summary>
    /// Overrides Initialize to add plugin-specific setup.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize(); // Call the base class's Initialize first
        Debug.Log($"<color=cyan>[{PluginName}]</color> Activated with multiplier: <color=yellow>{_damageMultiplier:F2}</color>. Effect: '{_effectDescription}'");
    }

    /// <summary>
    /// The core logic of this damage modifier plugin.
    /// In a real game, this would interact with a damage system, player stats, etc.
    /// </summary>
    public override void Execute()
    {
        // This method runs every frame while the plugin is active.
        // For demonstration, we just log the current state.
        // In a real scenario, you might continuously apply a status,
        // or check for conditions to apply the modifier.
        Debug.Log($"<color=grey>[{PluginName}]</color> Executing: Damage output is currently multiplied by <color=yellow>{_damageMultiplier:F2}</color>. ({_effectDescription})");
        // Example: PlayerStats.ApplyDamageModifier(_damageMultiplier);
    }

    /// <summary>
    /// Overrides Terminate to add plugin-specific cleanup.
    /// </summary>
    public override void Terminate()
    {
        base.Terminate(); // Call the base class's Terminate first
        Debug.Log($"<color=cyan>[{PluginName}]</color> Deactivated. Damage modifier removed.");
        // Example: PlayerStats.RemoveDamageModifier(_damageMultiplier);
    }
}
```

### **4. `StatusEffectPluginSO.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Another concrete plugin example: applies a status effect.
/// Demonstrates different types of data and logic a plugin can encapsulate.
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectPlugin", menuName = "Plugin/Status Effect")]
public class StatusEffectPluginSO : BasePluginSO
{
    // --- Plugin-specific Data ---
    public enum EffectType { Stun, Slow, Burn, Heal, Buff }

    [Tooltip("The type of status effect this plugin applies.")]
    [SerializeField] private EffectType _effectType = EffectType.Slow;

    [Tooltip("The duration of the effect in seconds.")]
    [SerializeField] private float _duration = 5.0f;

    [Tooltip("The magnitude of the effect (e.g., slow percentage, damage per tick, heal amount).")]
    [SerializeField] private float _magnitude = 1.0f;

    /// <summary>
    /// Public accessor for the effect type.
    /// </summary>
    public EffectType Type => _effectType;

    /// <summary>
    /// Public accessor for the effect duration.
    /// </summary>
    public float Duration => _duration;

    /// <summary>
    /// Public accessor for the effect magnitude.
    /// </summary>
    public float Magnitude => _magnitude;

    // --- IPlugin Implementations ---

    /// <summary>
    /// Overrides Initialize to add plugin-specific setup.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        Debug.Log($"<color=magenta>[{PluginName}]</color> Started: Type='{_effectType}', Duration=<color=yellow>{_duration:F1}s</color>, Magnitude=<color=yellow>{_magnitude:F1}</color>.");
        // In a real scenario, this might register the effect with a StatManager.
    }

    /// <summary>
    /// The core logic of this status effect plugin.
    /// In a real game, this would apply continuous effects or check remaining duration.
    /// </summary>
    public override void Execute()
    {
        // This runs every frame. For demonstration, we just log.
        // In a real scenario, a status effect might decrement a timer, apply damage over time, etc.
        Debug.Log($"<color=grey>[{PluginName}]</color> Executing: Applying <color=orange>{_effectType}</color> effect with magnitude <color=yellow>{_magnitude:F1}</color>.");
        // Example: TargetCharacter.ApplyContinuousEffect(_effectType, _magnitude);
    }

    /// <summary>
    /// Overrides Terminate to add plugin-specific cleanup.
    /// </summary>
    public override void Terminate()
    {
        base.Terminate();
        Debug.Log($"<color=magenta>[{PluginName}]</color> Ended: {_effectType} effect removed.");
        // Example: TargetCharacter.RemoveEffect(_effectType);
    }
}
```

### **5. `PluginManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

/// <summary>
/// Demonstrates the Plugin Hot Reload design pattern using ScriptableObjects in Unity.
/// This manager allows dynamically loading, unloading, and swapping "plugins"
/// which are defined as ScriptableObject assets implementing an IPlugin interface.
///
/// Use 'L' to load a plugin from the available list, 'U' to unload, 'R' to reload,
/// and Left/Right arrows to change the plugin index.
/// </summary>
public class PluginManager : MonoBehaviour
{
    // --- Public Inspector Fields ---
    [Header("Current Plugin")]
    [Tooltip("The currently active plugin. Drag a ScriptableObject asset implementing IPlugin here.")]
    [SerializeField]
    private BasePluginSO _activePlugin;

    [Header("Available Plugins for Dynamic Loading")]
    [Tooltip("A list of available plugins to demonstrate dynamic loading/swapping.")]
    [SerializeField]
    private List<BasePluginSO> _availablePlugins = new List<BasePluginSO>();

    [Tooltip("Index of the plugin from the 'Available Plugins' list to load on key press ('L').")]
    [SerializeField]
    private int _pluginLoadIndex = 0;


    // --- Private Members ---
    // The actual instance of the plugin being used. For ScriptableObjects,
    // we often use the asset directly, as it manages its own state.
    private IPlugin _currentPluginInstance;


    // --- Unity Lifecycle Methods ---

    void Start()
    {
        // Initialize the plugin if one is assigned in the Inspector at Start.
        if (_activePlugin != null)
        {
            LoadPlugin(_activePlugin);
        }
        else
        {
            Debug.LogWarning("PluginManager: No initial plugin assigned. Assign one in the Inspector or load dynamically using 'L'.");
        }
    }

    void Update()
    {
        // Execute the current plugin's logic every frame
        // This is where the plugin continuously performs its actions (e.g., checking timers, applying effects).
        _currentPluginInstance?.Execute();

        // --- Hot Reload / Swap Demonstration Controls ---
        HandleInput();
    }

    void OnDestroy()
    {
        // Ensure the current plugin is terminated when the manager GameObject is destroyed,
        // preventing memory leaks or lingering effects.
        UnloadCurrentPlugin();
    }


    // --- Private Helper Methods ---

    private void HandleInput()
    {
        // Press 'L' to load the plugin at _pluginLoadIndex from _availablePlugins.
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_availablePlugins != null && _availablePlugins.Count > 0)
            {
                int indexToLoad = Mathf.Clamp(_pluginLoadIndex, 0, _availablePlugins.Count - 1);
                Debug.Log($"PluginManager: Attempting to load plugin at index {indexToLoad} from available list...");
                LoadPlugin(_availablePlugins[indexToLoad]);
            }
            else
            {
                Debug.LogWarning("PluginManager: No available plugins in the list to load from.");
            }
        }

        // Press 'U' to unload the current plugin.
        if (Input.GetKeyDown(KeyCode.U))
        {
            UnloadCurrentPlugin();
        }

        // Press 'R' to simulate reloading the *currently active* plugin.
        // In this ScriptableObject context, it means re-initializing it,
        // useful if the plugin's internal state needs to be reset without swapping it.
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadCurrentPlugin();
        }

        // Increment/Decrement plugin load index for easier testing with Left/Right Arrow keys.
        if (_availablePlugins != null && _availablePlugins.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _pluginLoadIndex = (_pluginLoadIndex + 1) % _availablePlugins.Count;
                Debug.Log($"PluginManager: Next plugin index: <color=lime>{_pluginLoadIndex}</color> (Plugin: {_availablePlugins[_pluginLoadIndex]?.PluginName ?? "None"})");
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _pluginLoadIndex = (_pluginLoadIndex - 1 + _availablePlugins.Count) % _availablePlugins.Count;
                Debug.Log($"PluginManager: Next plugin index: <color=lime>{_pluginLoadIndex}</color> (Plugin: {_availablePlugins[_pluginLoadIndex]?.PluginName ?? "None"})");
            }
        }
    }


    // --- Plugin Management Methods ---

    /// <summary>
    /// Loads and activates a new plugin. This is the core "hot swap" method.
    /// It first unloads any existing plugin and then initializes the new one.
    /// </summary>
    /// <param name="newPluginSO">The ScriptableObject asset representing the plugin to load.</param>
    public void LoadPlugin(BasePluginSO newPluginSO)
    {
        if (newPluginSO == null)
        {
            Debug.LogError("PluginManager: Attempted to load a null plugin. Aborting load.");
            return;
        }

        // 1. Unload the currently active plugin (if any) to clean up its resources/effects.
        UnloadCurrentPlugin();

        // 2. Assign the new plugin ScriptableObject.
        // This updates the Inspector reference, making it clear which plugin is active.
        _activePlugin = newPluginSO;

        // 3. Assign the actual plugin instance.
        // For ScriptableObjects, we often use the asset directly as the instance.
        // If you needed a *unique, mutable* instance of the plugin's state per load,
        // you would use: _currentPluginInstance = Instantiate(newPluginSO);
        // However, for most data-driven plugins, using the asset directly is simpler
        // and allows hot-modifying the asset in the Project window.
        _currentPluginInstance = _activePlugin;

        // 4. Initialize the new plugin. This runs its one-time setup logic.
        _currentPluginInstance.Initialize();
        Debug.Log($"<color=lime>PluginManager: Successfully loaded and initialized plugin: '{_currentPluginInstance.PluginName}'</color>");

        // This is where the "hot reload" truly shines:
        // - You can change the `_activePlugin` reference in the Inspector while playing,
        //   and the changes will take effect immediately when LoadPlugin is called
        //   (e.g., via the 'L' key press, or directly in code).
        // - Even better, modifying the public serialized values within the *currently loaded*
        //   ScriptableObject asset in the Project window's Inspector will reflect immediately
        //   in the game, as _currentPluginInstance points directly to that asset.
    }

    /// <summary>
    /// Terminates and unloads the currently active plugin.
    /// </summary>
    public void UnloadCurrentPlugin()
    {
        if (_currentPluginInstance != null)
        {
            _currentPluginInstance.Terminate(); // Call the plugin's cleanup method
            Debug.Log($"<color=yellow>PluginManager: Unloaded plugin: '{_currentPluginInstance.PluginName}'</color>");
            _currentPluginInstance = null; // Clear the instance reference
            _activePlugin = null; // Also clear the Inspector reference
        }
        else
        {
            Debug.LogWarning("PluginManager: No plugin currently active to unload.");
        }
    }

    /// <summary>
    /// Simulates reloading the current plugin by terminating and then re-initializing it.
    /// This is useful if the plugin's internal state needs to be reset without swapping to a different plugin.
    /// For example, if a timed effect needs to restart its timer.
    /// </summary>
    public void ReloadCurrentPlugin()
    {
        if (_currentPluginInstance != null)
        {
            Debug.Log($"<color=orange>PluginManager: Reloading plugin: '{_currentPluginInstance.PluginName}'...</color>");
            _currentPluginInstance.Terminate();   // Clean up current state
            _currentPluginInstance.Initialize();  // Re-initialize to a fresh state
            Debug.Log($"<color=orange>PluginManager: Plugin '{_currentPluginInstance.PluginName}' reloaded.</color>");
        }
        else
        {
            Debug.LogWarning("PluginManager: No plugin currently active to reload.");
        }
    }


    /*
    ======================================================================================
    Detailed Explanation and Real-World Usage of Plugin Hot Reload with ScriptableObjects
    ======================================================================================

    The "Plugin Hot Reload" design pattern, as demonstrated here in Unity, focuses on
    dynamically changing application behavior or data at runtime without requiring a
    full recompilation or restart of the entire application. In Unity, this is most
    practically achieved using ScriptableObjects for C# code logic that is configured
    via data assets.

    Why this approach is practical for Unity:

    1.  Data-Driven Design: ScriptableObjects are perfect for defining data assets.
        By combining them with interfaces, we create "data-driven plugins" where
        behavior is controlled by asset configuration, not just hardcoded logic.

    2.  Runtime Swapping: The PluginManager can easily swap between different
        ScriptableObject assets (e.g., "HighDamagePlugin" vs. "LowDamagePlugin")
        at runtime. This is the core "hot reload" capability – changing logic on the fly.

    3.  Runtime Modification of Data: While the game is playing, you can select a
        ScriptableObject plugin asset in your Project window and modify its serialized
        fields (e.g., `_damageMultiplier`). These changes are *immediately* reflected
        in the game, as the PluginManager is directly referencing that asset. This is
        incredibly powerful for rapid iteration and balancing.

    4.  Reduced Compilation Time: When you create a new concrete plugin type (e.g.,
        `NewMovementPluginSO`), you only compile that new C# class. Once compiled, you can
        create countless instances of that plugin as ScriptableObject assets, each with
        different configurations, without any further C# compilation. Modifying the data
        in these assets requires no compilation at all.

    5.  Clear Separation of Concerns: The `PluginManager` doesn't need to know the
        specific implementation details of `DamageModifierPluginSO` or `StatusEffectPluginSO`.
        It only interacts with the `IPlugin` interface, making the system highly modular
        and extensible. You can add new plugin types without changing the manager.

    6.  Ease of Use for Designers: Game designers can create, configure, and swap plugins
        (e.g., different power-ups, enemy behaviors, quest stages) directly in the
        Unity Editor without writing any C# code.

    Real-World Use Cases:

    *   Game Modifiers/Power-ups: Implement different power-ups (e.g., "Double Damage,"
        "Slow Time," "Invincibility") as plugins. A `PlayerController` could simply
        tell the `PluginManager` to "load power-up X" based on what the player picks up.

    *   Enemy AI States/Behaviors: Define different enemy behaviors (e.g., "Aggressive,"
        "Patrol," "Flee") as plugins. An AI controller could swap between these plugins
        based on game events (player proximity, health level).

    *   Quest/Event System: Each stage of a quest or a dynamic game event could be a plugin.
        When a condition is met, the system loads the next quest stage plugin, which
        might involve activating NPCs, spawning items, or triggering dialogues.

    *   Status Effects: As shown in the example, different debuffs or buffs can be
        plugins applied to characters.

    *   Configuration Presets: Different difficulty settings, game modes, or region-specific
        configurations can be managed as plugins.

    *   Prototyping: Rapidly experiment with different game mechanics. Create a new plugin
        asset, tweak values, and instantly see the results without restarting.

    Limitations (and when true C# hot reloading might be considered):

    This ScriptableObject-based approach is excellent for data-driven behaviors.
    However, it doesn't allow you to:
    *   Dynamically load *new C# classes* that were not compiled into your game's
        assemblies *at build time*.
    *   Modify the *C# code* of an existing plugin class and have those changes
        apply without a Unity recompile (i.e., you still need to restart Play Mode
        if you change `DamageModifierPluginSO.cs` itself, not just its asset data).

    For scenarios requiring true dynamic C# code compilation and loading (e.g.,
    user-created mods with entirely new code, advanced developer tools), you would
    need a much more complex system involving:
    *   A C# compiler at runtime (e.g., `Microsoft.CSharp.CSharpCodeProvider` or Roslyn).
    *   `Assembly.Load` to load compiled `.dll` files.
    *   Reflection to instantiate types from the loaded assembly.
    *   Sandboxing for security.
    *   Extensive error handling.
    This level of complexity is typically beyond a "drop-in" Unity solution
    and is often discouraged for runtime game logic due to performance, security,
    and platform compatibility concerns. The ScriptableObject approach is a
    more robust and Unity-idiomatic solution for most "plugin hot reload" needs.
    */
}
```