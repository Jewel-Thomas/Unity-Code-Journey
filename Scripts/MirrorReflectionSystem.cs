// Unity Design Pattern Example: MirrorReflectionSystem
// This script demonstrates the MirrorReflectionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a "MirrorReflectionSystem" design pattern in Unity.
The core idea is to use C# `System.Reflection` to automatically **discover** and **register** methods from various components based on custom attributes. This central system then acts as a "mirror" reflecting these capabilities, allowing you to invoke them dynamically without direct hard-coded references.

**Why "MirrorReflectionSystem"?**
1.  **Mirroring:** The central system (e.g., `GameEventMirrorSystem`) *mirrors* the existence and intent of methods scattered across different `MonoBehaviour` instances into a unified, discoverable registry. It doesn't know about `PlayerController` or `EnemyAI` directly, but it mirrors their `HandlePlayerDied` method into its internal event map.
2.  **Reflection:** It heavily relies on `System.Reflection` to discover these methods, inspect their custom attributes, and later invoke them dynamically at runtime.

**Real-world Use Case:**
Imagine you have many different game systems (player stats, UI, enemy AI, particle effects) that need to react to various game events (player died, level complete, item collected). Instead of having each system explicitly subscribe to a central `EventManager` or creating tight coupling, you can use this pattern. Components simply mark their reaction methods with an attribute, and the `MirrorReflectionSystem` automatically hooks them up.

---

```csharp
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MirrorReflectionSystemExample
{
    /// <summary>
    /// Defines the types of game events that can be mirrored and triggered.
    /// This enum acts as the 'key' for our event-method mapping.
    /// </summary>
    public enum GameEventType
    {
        PlayerDied,
        LevelCompleted,
        EnemySpawned,
        AchievementUnlocked,
        // Add more game events as needed
    }

    /// <summary>
    /// Custom attribute used to mark methods that should be automatically
    /// discovered and registered by the <see cref="GameEventMirrorSystem"/>.
    /// Methods decorated with this attribute will be invoked when the
    /// specified <see cref="GameEventType"/> is triggered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class MirrorEventAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="GameEventType"/> this method should react to.
        /// </summary>
        public GameEventType EventType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MirrorEventAttribute"/> class.
        /// </summary>
        /// <param name="eventType">The type of game event this method listens for.</param>
        public MirrorEventAttribute(GameEventType eventType)
        {
            EventType = eventType;
        }
    }

    /// <summary>
    /// The central "Mirror Reflection System" responsible for:
    /// 1. Scanning active <see cref="MonoBehaviour"/> instances for methods decorated with <see cref="MirrorEventAttribute"/>.
    /// 2. Registering these methods based on their associated <see cref="GameEventType"/>.
    /// 3. Providing a public API to trigger events, which then dynamically invokes all registered methods for that event type.
    /// This class follows the Singleton pattern to ensure only one instance manages all game events.
    /// </summary>
    public class GameEventMirrorSystem : MonoBehaviour
    {
        // Singleton instance
        public static GameEventMirrorSystem Instance { get; private set; }

        // Stores a mapping from GameEventType to a list of (MonoBehaviour instance, MethodInfo) tuples.
        // This dictionary holds all the "mirrored" capabilities.
        private Dictionary<GameEventType, List<(MonoBehaviour instance, MethodInfo method)>> _eventListeners;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the singleton and scans for event-mirroring methods.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Ensure only one instance exists.
                Debug.LogWarning("Multiple GameEventMirrorSystem instances found. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optionally, prevent the system from being destroyed when loading new scenes.
            // This is often desirable for central game managers.
            DontDestroyOnLoad(gameObject);

            _eventListeners = new Dictionary<GameEventType, List<(MonoBehaviour instance, MethodInfo method)>>();

            Debug.Log("<color=cyan>GameEventMirrorSystem: Initializing and scanning for MirrorEvent methods...</color>");
            ScanForMirrorEvents();
            Debug.Log($"<color=cyan>GameEventMirrorSystem: Scan complete. Total registered events: {_eventListeners.Count}</color>");
        }

        /// <summary>
        /// Scans all active <see cref="MonoBehaviour"/> instances in the currently loaded scenes
        /// for methods decorated with <see cref="MirrorEventAttribute"/>.
        /// Each found method is registered with its corresponding <see cref="GameEventType"/>.
        /// This is the core 'mirroring' part using reflection.
        /// </summary>
        private void ScanForMirrorEvents()
        {
            // IMPORTANT: FindObjectsOfType can be performance-intensive on startup, especially in large projects.
            // For production, consider alternatives like:
            // 1. Scanning specific assemblies known to contain event listeners.
            // 2. Having components manually register themselves (less "discovery", more "event bus").
            // 3. Using a pre-generated lookup table if build-time reflection is an option.
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();

            foreach (MonoBehaviour monoBehaviour in allMonoBehaviours)
            {
                // Get all public and non-public (private, protected, internal), instance methods.
                // We use NonPublic to allow developers to make their event handlers private if they choose.
                MethodInfo[] methods = monoBehaviour.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (MethodInfo method in methods)
                {
                    // Check if the method has our custom MirrorEventAttribute.
                    MirrorEventAttribute[] attributes = method.GetCustomAttributes(
                        typeof(MirrorEventAttribute), true) as MirrorEventAttribute[];

                    if (attributes != null && attributes.Length > 0)
                    {
                        foreach (MirrorEventAttribute attribute in attributes)
                        {
                            // Validate method signature.
                            // For simplicity, this example only supports methods with no parameters.
                            // You could extend this to support specific parameter types (e.g., event data objects).
                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length > 0)
                            {
                                Debug.LogWarning($"<color=orange>[MirrorReflectionSystem Warning]</color> Method '{method.Name}' on '{monoBehaviour.name}' " +
                                                 $"has a [MirrorEvent] attribute but takes parameters. " +
                                                 $"Current system implementation only supports parameterless methods. " +
                                                 $"It will not be invoked for {attribute.EventType} until modified.", monoBehaviour);
                                continue; // Skip this method if it has parameters
                            }

                            // If the event type is not yet in our dictionary, add it.
                            if (!_eventListeners.ContainsKey(attribute.EventType))
                            {
                                _eventListeners[attribute.EventType] = new List<(MonoBehaviour instance, MethodInfo method)>();
                            }
                            // Register the method and its instance.
                            _eventListeners[attribute.EventType].Add((monoBehaviour, method));
                            Debug.Log($"<color=green>Registered MirrorEvent:</color> Method '{method.Name}' for event '{attribute.EventType}' on '{monoBehaviour.name}' (Instance ID: {monoBehaviour.GetInstanceID()})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Triggers a specific <see cref="GameEventType"/>, causing all registered methods
        /// for that event type to be dynamically invoked.
        /// This is the 'reflection' (invocation) part of the system.
        /// </summary>
        /// <param name="eventType">The type of event to trigger.</param>
        public void TriggerEvent(GameEventType eventType)
        {
            if (_eventListeners.TryGetValue(eventType, out var listeners))
            {
                Debug.Log($"<color=blue>MirrorEvent Triggered:</color> Event '{eventType}' initiated. Invoking {listeners.Count} listeners...");
                foreach (var listener in listeners)
                {
                    // Invoke the method using reflection.
                    // listener.instance is the MonoBehaviour instance on which the method exists.
                    // null for parameters because we validated methods to be parameterless.
                    try
                    {
                        // Check if the instance is still valid (not destroyed)
                        if (listener.instance != null)
                        {
                            listener.method.Invoke(listener.instance, null);
                            Debug.Log($"<color=grey>  Invoked:</color> '{listener.method.Name}' on '{listener.instance.name}' for '{eventType}'");
                        }
                        else
                        {
                            Debug.LogWarning($"<color=orange>[MirrorReflectionSystem Warning]</color> Listener instance for method '{listener.method.Name}' " +
                                             $"on event '{eventType}' is null/destroyed. It will be removed on next scan (if applicable).");
                            // In a more robust system, you might remove this listener from the list here.
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"<color=red>[MirrorReflectionSystem Error]</color> Error invoking method '{listener.method.Name}' " +
                                       $"for event '{eventType}' on '{listener.instance?.name ?? "NULL"}' (Instance ID: {listener.instance?.GetInstanceID() ?? 0}): {e}", listener.instance);
                    }
                }
            }
            else
            {
                Debug.Log($"<color=yellow>MirrorEvent:</color> No listeners registered for event '{eventType}'.");
            }
        }

        // --- Optional: Methods for dynamic registration/deregistration ---
        // For scenarios where GameObjects are spawned/destroyed at runtime,
        // you might need methods to register/deregister individual components
        // or trigger a partial rescan. For this example, a full initial scan suffices.
    }

    /// <summary>
    /// Example component: PlayerHealth
    /// Demonstrates how a component can react to events using the MirrorReflectionSystem
    /// by simply marking methods with <see cref="MirrorEventAttribute"/>.
    /// It also triggers events itself.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        private int _health = 100;

        void Start()
        {
            Debug.Log($"[PlayerHealth] Initialized with {_health} health.");
        }

        void Update()
        {
            // Simulate taking damage
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TakeDamage(20);
            }
            // Simulate completing a level
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("[PlayerHealth] Simulating Level Completion event.");
                GameEventMirrorSystem.Instance?.TriggerEvent(GameEventType.LevelCompleted);
            }
        }

        /// <summary>
        /// Reduces player health and triggers the PlayerDied event if health drops to zero.
        /// </summary>
        /// <param name="damage">Amount of damage to take.</param>
        private void TakeDamage(int damage)
        {
            _health -= damage;
            Debug.Log($"[PlayerHealth] Player takes {damage} damage. Current Health: {_health}");
            if (_health <= 0)
            {
                Debug.Log("<color=red>[PlayerHealth] Player Died! Triggering PlayerDied event...</color>");
                GameEventMirrorSystem.Instance?.TriggerEvent(GameEventType.PlayerDied);
                _health = 100; // Reset for demonstration purposes
            }
        }

        /// <summary>
        /// This method is automatically registered by the GameEventMirrorSystem
        /// to be called when <see cref="GameEventType.PlayerDied"/> is triggered.
        /// </summary>
        [MirrorEvent(GameEventType.PlayerDied)]
        private void HandlePlayerDied()
        {
            Debug.Log("<color=red>[PlayerHealth Listener]</color> Received 'PlayerDied' event! Displaying game over screen...");
            // Perform actions like showing game over UI, playing death animation, resetting level etc.
        }

        /// <summary>
        /// This method is automatically registered for the LevelCompleted event.
        /// Demonstrates multiple events handled by one component.
        /// </summary>
        [MirrorEvent(GameEventType.LevelCompleted)]
        private void AwardBonusOnLevelComplete()
        {
            Debug.Log("<color=green>[PlayerHealth Listener]</color> Received 'LevelCompleted' event! Awarding bonus points!");
            // Add experience, currency, unlock next level, etc.
        }
    }

    /// <summary>
    /// Example component: EnemyManager
    /// Demonstrates another component reacting to game events.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        /// <summary>
        /// This method is automatically registered to be called when <see cref="GameEventType.PlayerDied"/> is triggered.
        /// </summary>
        [MirrorEvent(GameEventType.PlayerDied)]
        private void OnPlayerDeathCleanup()
        {
            Debug.Log("<color=red>[EnemyManager Listener]</color> Received 'PlayerDied' event! Despawning all enemies...");
            // Implement logic to despawn enemies, reset AI states, etc.
        }

        /// <summary>
        /// This method is automatically registered for the LevelCompleted event.
        /// </summary>
        [MirrorEvent(GameEventType.LevelCompleted)]
        private void OnLevelComplete()
        {
            Debug.Log("<color=green>[EnemyManager Listener]</color> Received 'LevelCompleted' event! Enemies are celebrating!");
            // Make enemies dance or despawn as appropriate.
        }

        /// <summary>
        /// This method demonstrates a scenario where a method has parameters,
        /// and thus will NOT be registered by the current system.
        /// A warning will be logged during the scan.
        /// </summary>
        [MirrorEvent(GameEventType.EnemySpawned)]
        private void SpawnEnemy(string enemyType)
        {
            Debug.Log($"[EnemyManager] Spawning a {enemyType} enemy (this method will not be invoked by the system).");
        }
    }

    /// <summary>
    /// Example component: UIManager
    /// Another component demonstrating reaction to an event.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [MirrorEvent(GameEventType.PlayerDied)]
        private void ShowGameOverScreen()
        {
            Debug.Log("<color=red>[UIManager Listener]</color> Received 'PlayerDied' event! Activating Game Over UI Panel.");
            // Logic to activate a game over UI panel.
        }

        [MirrorEvent(GameEventType.AchievementUnlocked)]
        private void DisplayAchievementNotification()
        {
            Debug.Log("<color=purple>[UIManager Listener]</color> Received 'AchievementUnlocked' event! Showing popup!");
            // Logic to display a notification.
        }

        // Simulate an achievement unlock trigger
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("[UIManager] Simulating Achievement Unlocked event.");
                GameEventMirrorSystem.Instance?.TriggerEvent(GameEventType.AchievementUnlocked);
            }
        }
    }
}
```

---

### How to Use This Example in Unity:

1.  **Create a C# Script:**
    *   In your Unity project, create a new C# script (e.g., `MirrorReflectionSystemDemo.cs`).
    *   Copy and paste the entire code block above into this new script.
2.  **Create an Empty GameObject for the System:**
    *   In your Unity scene, create an empty GameObject (e.g., `_GameEventMirrorSystem`).
    *   Attach the `GameEventMirrorSystem` component to this GameObject.
    *   **Important:** Ensure this GameObject is active in the scene when the game starts, as `Awake` is used for initialization.
3.  **Create Example GameObjects:**
    *   Create another empty GameObject (e.g., `Player`).
    *   Attach the `PlayerHealth` component to this `Player` GameObject.
    *   Create another empty GameObject (e.g., `GameManagers`).
    *   Attach the `EnemyManager` component to `GameManagers`.
    *   Attach the `UIManager` component to `GameManagers` (or a dedicated UI GameObject).
4.  **Run the Scene:**
    *   Enter Play Mode in Unity.
    *   Observe the Console window for logs about the system scanning and registering methods.
    *   **Press `Spacebar`:** The `PlayerHealth` component will take damage. When health reaches 0, it triggers `GameEventType.PlayerDied`. You will see logs from `PlayerHealth`, `EnemyManager`, and `UIManager` reacting to this event.
    *   **Press `L`:** The `PlayerHealth` component will trigger `GameEventType.LevelCompleted`. You will see reactions from `PlayerHealth` and `EnemyManager`.
    *   **Press `A`:** The `UIManager` component will trigger `GameEventType.AchievementUnlocked`. You will see a reaction from `UIManager`.

---

### Explanation and Design Pattern Details:

**1. `GameEventType` Enum:**
*   **Purpose:** Defines a set of discrete game events that the system can recognize and dispatch.
*   **Role in Pattern:** Acts as the common identifier for different events, allowing the MirrorReflectionSystem to map methods to specific event types.

**2. `MirrorEventAttribute`:**
*   **Purpose:** A custom C# attribute used to "tag" methods in `MonoBehaviour` scripts.
*   **Role in Pattern:** This is the key mechanism for *mirroring* intent. Developers simply apply this attribute to a method, declaring that this method should react to a specific `GameEventType`. The system then "reflects" upon this attribute to discover the method's purpose. `[AttributeUsage(..., AllowMultiple = true)]` allows a single method to react to multiple event types.

**3. `GameEventMirrorSystem` (The Core "MirrorReflectionSystem"):**
*   **Singleton Pattern:** Uses `Instance` to provide a globally accessible point to trigger events, ensuring only one central manager exists.
*   **`Awake()` Method:**
    *   Initializes the Singleton pattern.
    *   Calls `ScanForMirrorEvents()`. This is where the core "mirroring" (discovery) process happens during startup.
*   **`_eventListeners` Dictionary:**
    *   `Dictionary<GameEventType, List<(MonoBehaviour instance, MethodInfo method)>>`: This data structure stores the "mirrored" information. For each `GameEventType`, it holds a list of tuples. Each tuple contains:
        *   `MonoBehaviour instance`: The specific object instance on which the method should be called. This is crucial because methods are typically instance methods.
        *   `MethodInfo method`: A reflection object representing the actual method to be invoked.
*   **`ScanForMirrorEvents()` Method:**
    *   **Discovery (`FindObjectsOfType<MonoBehaviour>()`):** Iterates through all active `MonoBehaviour` instances in the scene. This is a common way to discover components at runtime in Unity, though performance considerations for very large projects are noted.
    *   **Reflection (`GetType().GetMethods()`, `GetCustomAttributes()`):** For each `MonoBehaviour`, it uses `System.Reflection` to:
        *   Get all its instance methods (public/non-public).
        *   Check if any method has the `MirrorEventAttribute`.
        *   If found, it extracts the `EventType` from the attribute.
        *   **Method Signature Validation:** It checks if the method has parameters. In this example, methods with parameters are skipped with a warning, enforcing a simple, parameterless event handler contract. This is a best practice to ensure reliable invocation.
        *   **Registration:** Stores the `MonoBehaviour` instance and the `MethodInfo` in the `_eventListeners` dictionary. This completes the "mirroring" process, as the system now centrally knows about all these disparate methods and their event intentions.
*   **`TriggerEvent(GameEventType eventType)` Method:**
    *   **Dynamic Invocation (Reflection - `MethodInfo.Invoke()`):** When an event is triggered, the system looks up the `eventType` in `_eventListeners`.
    *   It then iterates through all registered methods for that event and calls `listener.method.Invoke(listener.instance, null)`. This is the "reflection" (invocation) part, where the system dynamically executes the previously discovered capabilities.
    *   **Error Handling:** Includes `try-catch` blocks to gracefully handle potential runtime exceptions during method invocation (e.g., if a method throws an error). It also checks if the `MonoBehaviour` instance is still valid.

**4. Example Components (`PlayerHealth`, `EnemyManager`, `UIManager`):**
*   These components demonstrate the simplicity of using the system. They don't need to implement interfaces, subscribe to delegates, or hold direct references to an `EventManager`.
*   They simply define a method that reacts to an event and decorate it with `[MirrorEvent(GameEventType.YourEvent)]`.
*   They can also trigger events by calling `GameEventMirrorSystem.Instance?.TriggerEvent(GameEventType.SomeEvent)`.

### Benefits of the MirrorReflectionSystem Pattern:

*   **Decoupling:** Components that trigger events don't need to know about the components that react to them. Similarly, reactive components don't need to know about the event source or a central event manager's API beyond applying an attribute.
*   **Extensibility:** Adding new event reactions is incredibly easy. Just create a new method in any `MonoBehaviour`, add the `MirrorEventAttribute`, and the system automatically discovers it. No changes to the central system or existing components are required.
*   **Automatic Discovery:** No manual registration or explicit subscription code is needed for methods marked with the attribute. The system handles all the wiring at startup.
*   **Readability:** The attribute clearly indicates a method's purpose as an event handler.

### Drawbacks and Considerations:

*   **Performance Overhead:**
    *   **Startup Scan:** `FindObjectsOfType` and reflection (`GetMethods`, `GetCustomAttributes`) can be slow, especially in large projects or with many components, as they occur at runtime initialization.
    *   **Method Invocation:** Calling methods via `MethodInfo.Invoke()` is generally slower than direct method calls or delegate invocations. For performance-critical, high-frequency events, consider alternatives.
*   **Type Safety / Compile-Time Checks:**
    *   Method signature mismatches (e.g., if `TriggerEvent` supported parameters, but a handler method expected different ones) are not caught at compile time but result in runtime errors.
    *   Refactoring a method name or signature needs care; if the `MirrorEventAttribute` isn't updated, the system might fail to find or invoke it, leading to silent failures or runtime errors.
*   **Debugging:** Tracing event flow can be harder than with direct calls or delegates, as the connection between an event trigger and its handlers is indirect (via reflection).
*   **Limited Features:** This basic example only supports parameterless methods. Extending it to pass event data (e.g., `TriggerEvent(EventType, object eventData)`) would require more complex method signature validation and parameter handling.

### Alternatives:

Depending on your specific needs, other patterns might be more suitable:

*   **Standard C# Events/Delegates:** Strongly typed, compile-time checked, performant. Requires explicit subscription and unsubscription.
*   **UnityEvents:** Configurable in the Inspector, great for visual workflow, but requires manual setup for each listener.
*   **ScriptableObject Events:** A popular pattern in Unity for decoupling, where a `ScriptableObject` acts as an event channel. Requires components to manually subscribe to the `ScriptableObject`'s event.
*   **Interface-based Systems:** Components implement an interface (e.g., `IPlayerDiedHandler`), and an event manager finds and calls all implementers. Strongly typed.

The MirrorReflectionSystem pattern is particularly useful when you prioritize **automatic discovery** and **extreme decoupling** at the cost of some runtime performance and compile-time safety.