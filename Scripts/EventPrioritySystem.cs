// Unity Design Pattern Example: EventPrioritySystem
// This script demonstrates the EventPrioritySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Event Priority System design pattern provides a robust way to manage events where the order of listener notification is crucial. Instead of all listeners reacting simultaneously or in an arbitrary order, this pattern allows you to assign a priority to each listener, ensuring that higher-priority listeners are notified before lower-priority ones. This is particularly useful in Unity where different game systems (UI, AI, VFX, Audio, Game Logic) might need to react to the same event in a specific sequence.

**Why use an Event Priority System in Unity?**

*   **Deterministic Order:** Ensures that critical game logic (e.g., updating player state, saving data) happens before less critical things (e.g., playing a sound, showing a particle effect).
*   **System Dependencies:** If one system's reaction depends on another system having already processed the event, priority helps enforce this. For example, a UI health bar might need to update *before* a damage number popup appears, or an AI system needs to know the player's new state *before* it decides on its next action.
*   **Modularity:** Decouples event publishers from subscribers while maintaining control over the execution flow.
*   **Debugging:** Predictable event handling makes debugging easier.

---

### **`EventPrioritySystem.cs`**

This script contains the complete implementation of the Event Priority System. It's designed to be a static class, making it globally accessible throughout your Unity project without needing to attach it to a GameObject.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for potential LINQ operations, though not strictly necessary for core logic here

/// <summary>
/// Marker interface for all events in the system.
/// This helps to enforce that only actual event types are used with the system.
/// </summary>
public interface IGameEvent { }

/// <summary>
/// Represents a subscription to an event.
/// Implementing IDisposable allows for easy unsubscription using a 'using' statement or by calling Dispose().
/// </summary>
public class EventSubscription<TEvent> : IDisposable where TEvent : IGameEvent
{
    private readonly Action<TEvent> _handler;
    private readonly int _priority;

    /// <summary>
    /// Initializes a new instance of the EventSubscription.
    /// </summary>
    /// <param name="handler">The event handler delegate.</param>
    /// <param name="priority">The priority of this subscription.</param>
    public EventSubscription(Action<TEvent> handler, int priority)
    {
        _handler = handler;
        _priority = priority;
    }

    /// <summary>
    /// Unsubscribes the handler from the EventPrioritySystem when disposed.
    /// </summary>
    public void Dispose()
    {
        // Call the static Unsubscribe method of the EventPrioritySystem.
        // This ensures the correct handler and priority are removed.
        EventPrioritySystem.Unsubscribe(_handler, _priority);
        // Prevent multiple calls to Dispose.
        GC.SuppressFinalize(this); 
    }
}

/// <summary>
/// A static event priority system that allows for subscribing to and publishing events
/// with an associated priority. Listeners with higher priority (lower integer value)
/// are notified before listeners with lower priority (higher integer value).
/// </summary>
public static class EventPrioritySystem
{
    // Dictionary to store listeners, organized by event type.
    // Each event type has a SortedList, where keys are priorities and values are lists of delegates.
    // SortedList automatically keeps entries sorted by priority (key).
    private static readonly Dictionary<Type, SortedList<int, List<Delegate>>> _listenersByType = 
        new Dictionary<Type, SortedList<int, List<Delegate>>>();

    /// <summary>
    /// Subscribes an event handler to a specific event type with a given priority.
    /// Lower priority values mean higher priority (e.g., 0 is highest, 1 is next, etc.).
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The delegate to be called when the event is published.</param>
    /// <param name="priority">The priority of this listener. Lower value means higher priority.</param>
    /// <returns>An IDisposable object that, when disposed, will unsubscribe the handler.</returns>
    public static IDisposable Subscribe<TEvent>(Action<TEvent> handler, int priority = 100) where TEvent : IGameEvent
    {
        Type eventType = typeof(TEvent);

        // If no listeners exist for this event type yet, create a new SortedList.
        if (!_listenersByType.ContainsKey(eventType))
        {
            _listenersByType[eventType] = new SortedList<int, List<Delegate>>();
        }

        // Get the SortedList for this event type.
        SortedList<int, List<Delegate>> priorityList = _listenersByType[eventType];

        // If no listeners exist at this specific priority, create a new list of delegates.
        if (!priorityList.ContainsKey(priority))
        {
            priorityList[priority] = new List<Delegate>();
        }

        // Add the handler to the list of delegates at this priority.
        priorityList[priority].Add(handler);

        // Return an EventSubscription object. When this object is disposed,
        // it will automatically call the Unsubscribe method.
        return new EventSubscription<TEvent>(handler, priority);
    }

    /// <summary>
    /// Unsubscribes a specific event handler from a specific event type and priority.
    /// This method is primarily called by the EventSubscription's Dispose method.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The handler delegate to remove.</param>
    /// <param name="priority">The priority at which the handler was subscribed.</param>
    internal static void Unsubscribe<TEvent>(Action<TEvent> handler, int priority) where TEvent : IGameEvent
    {
        Type eventType = typeof(TEvent);

        // Check if the event type exists in our listener dictionary.
        if (_listenersByType.TryGetValue(eventType, out SortedList<int, List<Delegate>> priorityList))
        {
            // Check if the priority level exists for this event type.
            if (priorityList.TryGetValue(priority, out List<Delegate> handlers))
            {
                // Remove the specific handler.
                handlers.Remove(handler);

                // If no more handlers exist at this priority level, remove the priority entry.
                if (handlers.Count == 0)
                {
                    priorityList.Remove(priority);
                }
            }

            // If no more priority levels exist for this event type, remove the event type entry.
            if (priorityList.Count == 0)
            {
                _listenersByType.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all registered listeners.
    /// Listeners are notified in order of their priority (lowest priority value first).
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to publish.</typeparam>
    /// <param name="eventArgs">The event data object.</param>
    public static void Publish<TEvent>(TEvent eventArgs) where TEvent : IGameEvent
    {
        Type eventType = typeof(TEvent);

        // If no listeners exist for this event type, do nothing.
        if (!_listenersByType.TryGetValue(eventType, out SortedList<int, List<Delegate>> priorityList))
        {
            return;
        }

        // Iterate through the SortedList. It's already sorted by key (priority).
        // We iterate over a copy of the list of delegates to prevent issues if listeners
        // unsubscribe themselves during event processing.
        foreach (var entry in priorityList)
        {
            // Create a copy of the list of delegates to prevent issues if a handler
            // unsubscribes itself during the event dispatch.
            List<Delegate> handlersCopy = new List<Delegate>(entry.Value);

            foreach (Delegate handler in handlersCopy)
            {
                try
                {
                    // Cast the general Delegate back to the specific Action<TEvent> and invoke it.
                    (handler as Action<TEvent>)?.Invoke(eventArgs);
                }
                catch (Exception e)
                {
                    // Log any exceptions that occur during handler execution
                    // to prevent one faulty handler from stopping the entire event chain.
                    Debug.LogError($"EventPrioritySystem: Error dispatching event {eventType.Name} to handler {handler.Method.Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }

    /// <summary>
    /// Clears all event listeners from the system. Use with caution, typically for scene changes or game shutdown.
    /// </summary>
    public static void ClearAllListeners()
    {
        _listenersByType.Clear();
        Debug.Log("EventPrioritySystem: All listeners cleared.");
    }
}


// --- EXAMPLE EVENTS ---
// These are concrete implementations of IGameEvent.
// Define your events as simple classes that implement IGameEvent.
// They can hold any data relevant to the event.

/// <summary>
/// Event for when the player's health changes.
/// </summary>
public class PlayerHealthChangedEvent : IGameEvent
{
    public int NewHealth { get; private set; }
    public int OldHealth { get; private set; }
    public GameObject PlayerObject { get; private set; }

    public PlayerHealthChangedEvent(int oldHealth, int newHealth, GameObject player)
    {
        OldHealth = oldHealth;
        NewHealth = newHealth;
        PlayerObject = player;
    }
}

/// <summary>
/// Event for when the game state changes (e.g., Pause, Resume, Game Over).
/// </summary>
public class GameStateChangedEvent : IGameEvent
{
    public GameStateType NewState { get; private set; }

    public GameStateChangedEvent(GameStateType newState)
    {
        NewState = newState;
    }
}

public enum GameStateType
{
    Running,
    Paused,
    GameOver,
    Loading
}

// --- EXAMPLE USAGE IN UNITY ---
// Create a new C# script (e.g., 'EventPrioritySystemDemo.cs')
// and attach it to an empty GameObject in your scene.

public class EventPrioritySystemDemo : MonoBehaviour
{
    // Store subscriptions to easily dispose of them when the object is destroyed.
    private IDisposable _healthSubscriptionHigh;
    private IDisposable _healthSubscriptionMedium;
    private IDisposable _healthSubscriptionLow;
    private IDisposable _gameStateSubscriptionUI;
    private IDisposable _gameStateSubscriptionLogic;

    void OnEnable()
    {
        Debug.Log("EventPrioritySystemDemo: Subscribing to events...");

        // Subscribe to PlayerHealthChangedEvent with different priorities
        // Priority 0: Highest - UI updates first
        _healthSubscriptionHigh = EventPrioritySystem.Subscribe<PlayerHealthChangedEvent>(
            OnPlayerHealthChanged_UI, 0); 

        // Priority 10: Medium - Sound plays next
        _healthSubscriptionMedium = EventPrioritySystem.Subscribe<PlayerHealthChangedEvent>(
            OnPlayerHealthChanged_Sound, 10);

        // Priority 20: Low - Visual effects last
        _healthSubscriptionLow = EventPrioritySystem.Subscribe<PlayerHealthChangedEvent>(
            OnPlayerHealthChanged_VFX, 20);

        // Subscribe to GameStateChangedEvent
        // Priority 5: Higher - Game logic reacts first (e.g., disable player input)
        _gameStateSubscriptionLogic = EventPrioritySystem.Subscribe<GameStateChangedEvent>(
            OnGameStateChanged_GameLogic, 5);

        // Priority 15: Lower - UI reacts next (e.g., show pause menu)
        _gameStateSubscriptionUI = EventPrioritySystem.Subscribe<GameStateChangedEvent>(
            OnGameStateChanged_UI, 15);

        // Example: Subscribing to an event with the default priority (100)
        EventPrioritySystem.Subscribe<PlayerHealthChangedEvent>(
            OnPlayerHealthChanged_DefaultPriority);
    }

    void Update()
    {
        // Example: Publish events based on user input for demonstration

        // Simulate player taking damage
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int oldHealth = UnityEngine.Random.Range(50, 100);
            int newHealth = UnityEngine.Random.Range(10, 40);
            Debug.Log($"<color=orange>Publishing PlayerHealthChangedEvent: Old={oldHealth}, New={newHealth}</color>");
            EventPrioritySystem.Publish(new PlayerHealthChangedEvent(oldHealth, newHealth, this.gameObject));
        }

        // Simulate game pause/resume
        if (Input.GetKeyDown(KeyCode.P))
        {
            GameStateType currentState = GameStateType.Paused;
            if (Time.timeScale == 0)
            {
                Time.timeScale = 1;
                currentState = GameStateType.Running;
                Debug.Log($"<color=cyan>Publishing GameStateChangedEvent: {currentState}</color>");
                EventPrioritySystem.Publish(new GameStateChangedEvent(currentState));
            }
            else
            {
                Time.timeScale = 0; // Pause game
                currentState = GameStateType.Paused;
                Debug.Log($"<color=cyan>Publishing GameStateChangedEvent: {currentState}</color>");
                EventPrioritySystem.Publish(new GameStateChangedEvent(currentState));
            }
        }
    }

    // --- PlayerHealthChangedEvent Handlers ---

    private void OnPlayerHealthChanged_UI(PlayerHealthChangedEvent e)
    {
        // High priority: Update UI elements immediately
        Debug.Log($"<color=green>[UI (P:0)] Health Bar Updated: {e.OldHealth} -> {e.NewHealth}</color>");
    }

    private void OnPlayerHealthChanged_Sound(PlayerHealthChangedEvent e)
    {
        // Medium priority: Play sound effect after UI update
        Debug.Log($"<color=yellow>[Sound (P:10)] Playing damage sound for {e.PlayerObject.name}</color>");
    }

    private void OnPlayerHealthChanged_VFX(PlayerHealthChangedEvent e)
    {
        // Low priority: Show visual effects last
        Debug.Log($"<color=red>[VFX (P:20)] Spawning damage particles on {e.PlayerObject.name}</color>");
    }

    private void OnPlayerHealthChanged_DefaultPriority(PlayerHealthChangedEvent e)
    {
        // Default priority: This will fire after P:0, P:10, P:20 (as default is 100)
        Debug.Log($"<color=magenta>[Game Logic (P:100)] Processing post-damage logic: Health difference: {e.OldHealth - e.NewHealth}</color>");
    }

    // --- GameStateChangedEvent Handlers ---

    private void OnGameStateChanged_GameLogic(GameStateChangedEvent e)
    {
        // Higher priority: Game logic responds first (e.g., disable player input, pause AI)
        Debug.Log($"<color=blue>[Game Logic (P:5)] Game state changed to: {e.NewState}. {(e.NewState == GameStateType.Paused ? "Disabling input." : "Enabling input.")}</color>");
        // Example: Find player script and disable/enable input
        // FindObjectOfType<PlayerController>()?.ToggleInput(e.NewState != GameStateType.Paused);
    }

    private void OnGameStateChanged_UI(GameStateChangedEvent e)
    {
        // Lower priority: UI responds after game logic
        Debug.Log($"<color=cyan>[UI (P:15)] Displaying {e.NewState} screen.</color>");
        // Example: Show/hide pause menu
        // FindObjectOfType<PauseMenuUI>()?.SetVisible(e.NewState == GameStateType.Paused);
    }

    void OnDisable()
    {
        // It's crucial to unsubscribe from events when your MonoBehaviour is disabled or destroyed
        // to prevent memory leaks and 'MissingReferenceException' errors.
        // The IDisposable pattern makes this clean and easy.
        _healthSubscriptionHigh?.Dispose();
        _healthSubscriptionMedium?.Dispose();
        _healthSubscriptionLow?.Dispose();
        _gameStateSubscriptionUI?.Dispose();
        _gameStateSubscriptionLogic?.Dispose();

        Debug.Log("EventPrioritySystemDemo: Unsubscribed from events.");

        // Optionally, clear all listeners if this is the last object managing events
        // or during scene unloading. Be careful with this, as it affects all listeners globally.
        // EventPrioritySystem.ClearAllListeners(); 
    }
}
```

---

### **How to use this in your Unity Project:**

1.  **Create the Script:**
    *   Create a new C# script in your Unity project (e.g., `Assets/Scripts/Utils/EventPrioritySystem.cs`).
    *   Copy and paste the entire code block above into this script.

2.  **Demonstration Setup:**
    *   Create an empty GameObject in your scene (e.g., `EventPriorityDemo`).
    *   Attach the `EventPrioritySystemDemo` component to this GameObject.

3.  **Run and Observe:**
    *   Play your scene.
    *   Open the Unity Console.
    *   Press the **Spacebar** key: You will see `PlayerHealthChangedEvent` being published. Observe the order of logs: UI (P:0) -> Sound (P:10) -> VFX (P:20) -> Game Logic (P:100).
    *   Press the **P** key: You will see `GameStateChangedEvent` being published (pausing/unpausing the game by setting `Time.timeScale`). Observe the order of logs: Game Logic (P:5) -> UI (P:15).

### **Explanation of the Code:**

*   **`IGameEvent` Interface:** A simple marker interface. All your custom event classes should implement this to be usable with `EventPrioritySystem`. This adds type safety.
*   **`EventSubscription<TEvent>` Class:**
    *   Implements `IDisposable`. This is a common and clean pattern for managing subscriptions.
    *   When an `EventSubscription` object is created (by calling `EventPrioritySystem.Subscribe`), it holds a reference to the handler and its priority.
    *   When `Dispose()` is called on this object (e.g., in `OnDisable` of a `MonoBehaviour` or using a `using` statement), it automatically calls `EventPrioritySystem.Unsubscribe` to remove the handler from the system. This prevents memory leaks.
*   **`EventPrioritySystem` Static Class:**
    *   **`_listenersByType`:** The core data structure. It's a `Dictionary` where:
        *   The **key** is `Type` (the specific type of event, e.g., `PlayerHealthChangedEvent`).
        *   The **value** is a `SortedList<int, List<Delegate>>`.
            *   The `SortedList` automatically maintains entries sorted by its key (the `int` priority). Lower integer values are considered higher priority.
            *   Each priority `int` maps to a `List<Delegate>`, allowing multiple handlers to subscribe to the *same event at the same priority level*. These handlers will then be invoked in the order they were added.
    *   **`Subscribe<TEvent>(Action<TEvent> handler, int priority)`:**
        *   Registers an `Action<TEvent>` delegate (`handler`) for a specific `TEvent` type with a given `priority`.
        *   Handles the creation of necessary `SortedList` and `List<Delegate>` entries if they don't exist.
        *   Returns an `IDisposable` `EventSubscription` object.
    *   **`Unsubscribe<TEvent>(Action<TEvent> handler, int priority)`:**
        *   Removes a specific `handler` from the system.
        *   This method is called internally by the `EventSubscription.Dispose()` method.
        *   It cleans up empty `List<Delegate>` and `SortedList` entries to keep the dictionary tidy.
    *   **`Publish<TEvent>(TEvent eventArgs)`:**
        *   The method to trigger an event.
        *   Retrieves all listeners for the given `TEvent` type.
        *   Iterates through the `SortedList` of priorities. Since `SortedList` is inherently ordered by key, this ensures handlers are called from highest priority (lowest `int` value) to lowest priority.
        *   For each priority level, it iterates through the `List<Delegate>` and invokes each handler.
        *   **Important:** It creates a *copy* of the handler list (`handlersCopy`) before iterating. This is crucial to prevent `InvalidOperationException` if a handler unsubscribes itself or another handler during the event dispatch process.
        *   Includes robust error handling (`try-catch`) for individual handler invocations to ensure that one faulty listener doesn't break the entire event dispatch chain.
    *   **`ClearAllListeners()`:** A utility method to remove all subscriptions. Use with caution, typically for full scene unloads or game shutdowns, as it affects the entire global system.

### **Best Practices and Considerations:**

*   **Priority Convention:** This implementation uses *lower integer values for higher priority* (e.g., 0 is highest, 100 is default). Be consistent with your chosen convention.
*   **Performance:** For very frequent events with many listeners, consider if an event system with dynamic sorting on dispatch (like this one) is optimal. However, for most game events, the performance overhead is negligible. The `SortedList` offers `O(log N)` for insertions/deletions and `O(N)` for iteration, where N is the number of distinct priority levels.
*   **Memory Management:** The `IDisposable` pattern for subscriptions is vital. Always `Dispose()` your subscriptions when the listening object is destroyed or disabled (`OnDisable`, `OnDestroy`). For temporary subscriptions, a `using` block can also be very effective.
*   **Thread Safety:** This implementation is not thread-safe. Unity's main thread handles most game logic, so this is generally not an issue. If you were dispatching events from background threads, you would need to add `lock` statements around modifications to `_listenersByType`.
*   **Debugging:** Use `Debug.Log` liberally when setting up your event system to confirm the order of operations. The colored logs in the demo help visualize this.
*   **Event Data:** Event classes (`PlayerHealthChangedEvent`, `GameStateChangedEvent`) should be simple data containers. Avoid putting complex logic directly into event objects.
*   **Flexibility:** You can easily extend this system with features like "event cancellation" (where a high-priority listener can stop lower-priority listeners from receiving the event) by adding a boolean `IsCancelled` property to `IGameEvent` and checking it in the `Publish` loop.