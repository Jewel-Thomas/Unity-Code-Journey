// Unity Design Pattern Example: EventBus
// This script demonstrates the EventBus pattern in Unity
// Generated automatically - ready to use in your Unity project

The EventBus design pattern is a powerful way to implement the **Observer pattern** and achieve **loose coupling** in your Unity projects. It allows different parts of your application to communicate with each other without needing direct references, making your code more modular, testable, and easier to maintain.

Here's a complete, practical C# Unity example demonstrating the EventBus pattern.

---

**How the EventBus Works:**

1.  **Events:** These are simple data structures (usually `struct`s or lightweight `class`es) that carry information about something that has happened. They don't contain any logic, just data.
2.  **EventBus:** A central static class responsible for:
    *   **Subscribing:** Components register methods (handlers) to be called when a specific event type is published.
    *   **Unsubscribing:** Components unregister their methods when they no longer need to listen. This is crucial for preventing memory leaks and errors.
    *   **Publishing (Posting):** When something happens, a component creates an event object and tells the EventBus to publish it. The EventBus then notifies all registered handlers for that event type.
3.  **Subscribers (Listeners):** Components that are interested in certain events implement methods to handle them. They subscribe to the EventBus in `OnEnable()` and unsubscribe in `OnDisable()`.
4.  **Publishers (Dispatchers):** Components that trigger events. They create an event object and call `EventBus.Publish()` when something significant occurs.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // Used by the EventBus itself

// =================================================================================================
// 1. EVENT DEFINITIONS
//    These are simple data structures (structs or classes) that represent something that happened.
//    They should contain only data relevant to the event.
//    Using an empty interface 'IGameEvent' is good practice for type constraint in the EventBus,
//    ensuring only actual event types can be published/subscribed.
// =================================================================================================

/// <summary>
/// Base interface for all game events. Helps to constrain generic types in EventBus.
/// </summary>
public interface IGameEvent {}

/// <summary>
/// Event triggered when the player's score changes.
/// </summary>
public struct ScoreChangedEvent : IGameEvent {
    public int NewScore;
    public ScoreChangedEvent(int newScore) { NewScore = newScore; }
}

/// <summary>
/// Event triggered when a player character dies.
/// </summary>
public struct PlayerDiedEvent : IGameEvent {
    public string PlayerName;
    public Vector3 DeathLocation;
    public PlayerDiedEvent(string playerName, Vector3 deathLocation) {
        PlayerName = playerName;
        DeathLocation = deathLocation;
    }
}

/// <summary>
/// Event triggered when a game level is completed.
/// </summary>
public struct LevelCompletedEvent : IGameEvent {
    public int LevelNumber;
    public float TimeTaken;
    public int StarsAwarded;
    public LevelCompletedEvent(int levelNumber, float timeTaken, int starsAwarded) {
        LevelNumber = levelNumber;
        TimeTaken = timeTaken;
        StarsAwarded = starsAwarded;
    }
}

// =================================================================================================
// 2. THE EVENT BUS CORE
//    This static class is the central hub for all event communication.
//    It manages subscriptions, unsubscriptions, and event publishing.
// =================================================================================================

/// <summary>
/// A static EventBus for publishing and subscribing to game events.
/// This acts as a central hub, allowing different components to communicate without direct references.
/// </summary>
public static class EventBus {
    // Dictionary to store event types (keys) and their corresponding lists of handlers (values).
    // Key: Type of the event (e.g., typeof(ScoreChangedEvent))
    // Value: A list of Delegate objects. We use 'Delegate' because Action<T> is generic,
    // and we need a common base type to store all handlers in a non-generic list.
    private static readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();

    // A lock object to ensure thread safety when modifying the eventHandlers dictionary.
    // While most Unity game logic runs on the main thread, it's good practice for robustness
    // if any part of your game might interact with the EventBus from a background thread.
    private static readonly object _lock = new object();

    /// <summary>
    /// Subscribes a handler method to a specific event type.
    /// When an event of type TEvent is published, the provided handler will be invoked.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to subscribe to (must implement IGameEvent).</typeparam>
    /// <param name="handler">The method (an Action delegate) to be called when the event occurs.</param>
    public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent {
        lock (_lock) { // Protect dictionary access with a lock
            Type eventType = typeof(TEvent);

            if (!_eventHandlers.ContainsKey(eventType)) {
                _eventHandlers.Add(eventType, new List<Delegate>());
            }

            // Add the handler to the list. Note that List.Add allows duplicates.
            // If you want to ensure a handler isn't subscribed multiple times,
            // you'd add a check like `if (!_eventHandlers[eventType].Contains(handler))`.
            _eventHandlers[eventType].Add(handler);
            // Debug.Log($"Subscribed: {handler.Method.Name} to {eventType.Name}"); // Optional debug
        }
    }

    /// <summary>
    /// Unsubscribes a handler method from a specific event type.
    /// It's CRUCIAL to call this when a subscriber is no longer active (e.g., in OnDisable or OnDestroy)
    /// to prevent memory leaks and invalid calls on destroyed GameObjects.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to unsubscribe from (must implement IGameEvent).</typeparam>
    /// <param name="handler">The method (Action delegate) that was previously subscribed.</param>
    public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent {
        lock (_lock) { // Protect dictionary access with a lock
            Type eventType = typeof(TEvent);

            if (_eventHandlers.TryGetValue(eventType, out List<Delegate> handlers)) {
                handlers.Remove(handler); // Remove the specific handler

                // If the list of handlers for this event type becomes empty, remove the entry
                // from the dictionary to keep it clean and prevent unnecessary lookups.
                if (handlers.Count == 0) {
                    _eventHandlers.Remove(eventType);
                }
                // Debug.Log($"Unsubscribed: {handler.Method.Name} from {eventType.Name}"); // Optional debug
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// All registered methods for this specific TEvent type will be invoked, passing the eventData.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to publish (must implement IGameEvent).</typeparam>
    /// <param name="eventData">The event data object to be passed to the handlers.</param>
    public static void Publish<TEvent>(TEvent eventData) where TEvent : IGameEvent {
        List<Delegate> handlersToInvoke = null; // Temporary list to safely iterate over handlers.

        lock (_lock) { // Protect dictionary access with a lock
            Type eventType = typeof(TEvent);

            if (_eventHandlers.TryGetValue(eventType, out List<Delegate> handlers)) {
                // Create a copy of the handlers list. This is CRITICAL.
                // It prevents issues if a handler unsubscribes itself (or another handler)
                // during the iteration, which would otherwise modify the collection
                // being iterated over, leading to an InvalidOperationException.
                handlersToInvoke = new List<Delegate>(handlers);
            }
        }

        if (handlersToInvoke != null) {
            foreach (Delegate handler in handlersToInvoke) {
                try {
                    // Cast the general 'Delegate' back to its specific 'Action<TEvent>' type
                    // and then invoke it with the provided event data.
                    (handler as Action<TEvent>)?.Invoke(eventData);
                } catch (Exception e) {
                    // Log any exceptions thrown by individual handlers.
                    // This prevents a single faulty handler from crashing the entire event bus
                    // and stopping other handlers from being called.
                    Debug.LogError($"EventBus: Error processing event {typeof(TEvent).Name} by handler {handler.Method.Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}

// =================================================================================================
// 3. EXAMPLE USAGE COMPONENTS
//    These MonoBehaviour scripts demonstrate how to publish and subscribe to events.
//    Drag these onto GameObjects in your scene to see them in action.
// =================================================================================================

/// <summary>
/// PUBLISHER EXAMPLE: Simulates a game system that publishes events.
/// This component doesn't need to know anything about who is listening.
/// </summary>
public class ScoreManager : MonoBehaviour {
    private int _currentScore = 0;
    private int _levelCounter = 0;

    void Update() {
        // --- Simulate Score Changes ---
        if (Input.GetKeyDown(KeyCode.Space)) {
            AddScore(10);
        }
        if (Input.GetKeyDown(KeyCode.Return)) {
            AddScore(25);
        }

        // --- Simulate Player Death ---
        if (Input.GetKeyDown(KeyCode.D)) {
            // Publish PlayerDiedEvent with relevant data
            EventBus.Publish(new PlayerDiedEvent("Player One", transform.position));
            Debug.Log($"ScoreManager: Published PlayerDiedEvent for Player One at {transform.position}.");
        }

        // --- Simulate Level Completion ---
        if (Input.GetKeyDown(KeyCode.L)) {
            _levelCounter++;
            float timeTaken = UnityEngine.Random.Range(30f, 120f);
            int stars = timeTaken < 60f ? 3 : (timeTaken < 90f ? 2 : 1);
            // Publish LevelCompletedEvent with relevant data
            EventBus.Publish(new LevelCompletedEvent(_levelCounter, timeTaken, stars));
            Debug.Log($"ScoreManager: Published LevelCompletedEvent for Level {_levelCounter}.");
        }
    }

    /// <summary>
    /// Increments the score and publishes a ScoreChangedEvent.
    /// </summary>
    /// <param name="points">Points to add.</param>
    public void AddScore(int points) {
        _currentScore += points;
        Debug.Log($"ScoreManager: Current Score: {_currentScore}");
        // Publishers simply create an event and publish it. They don't care who receives it.
        EventBus.Publish(new ScoreChangedEvent(_currentScore));
    }
}

/// <summary>
/// SUBSCRIBER EXAMPLE 1: Updates UI based on game events.
/// This component subscribes to events it's interested in.
/// </summary>
public class ScoreDisplay : MonoBehaviour {
    // In a real game, this would update a TextMeshProUGUI or UI.Text component.
    // For this example, we'll just log to the console.

    void OnEnable() {
        // Subscribe to events when this component becomes active.
        // The `HandleScoreChanged` method will be called whenever a `ScoreChangedEvent` is published.
        EventBus.Subscribe<ScoreChangedEvent>(HandleScoreChanged);
        Debug.Log($"{gameObject.name}: Subscribed to ScoreChangedEvent.");
    }

    void OnDisable() {
        // Unsubscribe from events when this component becomes inactive or is destroyed.
        // This is crucial to prevent memory leaks and ensure that disabled/destroyed
        // GameObjects do not receive event callbacks.
        EventBus.Unsubscribe<ScoreChangedEvent>(HandleScoreChanged);
        Debug.Log($"{gameObject.name}: Unsubscribed from ScoreChangedEvent.");
    }

    /// <summary>
    /// Event handler for ScoreChangedEvent.
    /// </summary>
    /// <param name="e">The ScoreChangedEvent data.</param>
    private void HandleScoreChanged(ScoreChangedEvent e) {
        Debug.Log($"ScoreDisplay: UI updated! New Score: {e.NewScore}");
        // Example: myScoreText.text = "Score: " + e.NewScore.ToString();
    }
}

/// <summary>
/// SUBSCRIBER EXAMPLE 2: Plays sounds in response to game events.
/// Demonstrates that multiple components can listen to different events.
/// </summary>
public class SoundManager : MonoBehaviour {
    void OnEnable() {
        // Subscribe to player death and level completion events.
        EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDied);
        EventBus.Subscribe<LevelCompletedEvent>(HandleLevelCompleted);
        Debug.Log($"{gameObject.name}: Subscribed to PlayerDiedEvent and LevelCompletedEvent.");
    }

    void OnDisable() {
        // Unsubscribe from events.
        EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDied);
        EventBus.Unsubscribe<LevelCompletedEvent>(HandleLevelCompleted);
        Debug.Log($"{gameObject.name}: Unsubscribed from PlayerDiedEvent and LevelCompletedEvent.");
    }

    /// <summary>
    /// Event handler for PlayerDiedEvent.
    /// </summary>
    /// <param name="e">The PlayerDiedEvent data.</param>
    private void HandlePlayerDied(PlayerDiedEvent e) {
        Debug.Log($"SoundManager: Playing 'player down' sound for {e.PlayerName} near {e.DeathLocation}.");
        // Example: audioSource.PlayOneShot(playerDeathClip);
    }

    /// <summary>
    /// Event handler for LevelCompletedEvent.
    /// </summary>
    /// <param name="e">The LevelCompletedEvent data.</param>
    private void HandleLevelCompleted(LevelCompletedEvent e) {
        Debug.Log($"SoundManager: Playing 'level complete' sound for Level {e.LevelNumber} with {e.StarsAwarded} stars.");
        // Example: audioSource.PlayOneShot(levelCompleteClip);
    }
}

/// <summary>
/// SUBSCRIBER EXAMPLE 3: Manages achievements based on game events.
/// Shows how different systems can react to the same events independently.
/// </summary>
public class AchievementManager : MonoBehaviour {
    private int _highestScoreAchieved = 0;

    void OnEnable() {
        // Subscribe to score changes and level completions.
        EventBus.Subscribe<ScoreChangedEvent>(HandleScoreChanged);
        EventBus.Subscribe<LevelCompletedEvent>(HandleLevelCompleted);
        Debug.Log($"{gameObject.name}: Subscribed to ScoreChangedEvent and LevelCompletedEvent.");
    }

    void OnDisable() {
        // Unsubscribe from events.
        EventBus.Unsubscribe<ScoreChangedEvent>(HandleScoreChanged);
        EventBus.Unsubscribe<LevelCompletedEvent>(HandleLevelCompleted);
        Debug.Log($"{gameObject.name}: Unsubscribed from ScoreChangedEvent and LevelCompletedEvent.");
    }

    /// <summary>
    /// Event handler for ScoreChangedEvent. Checks for score-based achievements.
    /// </summary>
    /// <param name="e">The ScoreChangedEvent data.</param>
    private void HandleScoreChanged(ScoreChangedEvent e) {
        Debug.Log($"AchievementManager: Checking score {e.NewScore} for achievements...");
        if (e.NewScore >= 50 && _highestScoreAchieved < 50) {
            Debug.Log("AchievementManager: UNLOCKED 'Half-Centurion!' (Reached 50 points)");
            _highestScoreAchieved = 50;
        }
        if (e.NewScore >= 100 && _highestScoreAchieved < 100) {
            Debug.Log("AchievementManager: UNLOCKED 'Centurion!' (Reached 100 points)");
            _highestScoreAchieved = 100;
        }
    }

    /// <summary>
    /// Event handler for LevelCompletedEvent. Checks for level-based achievements.
    /// </summary>
    /// <param name="e">The LevelCompletedEvent data.</param>
    private void HandleLevelCompleted(LevelCompletedEvent e) {
        Debug.Log($"AchievementManager: Checking level {e.LevelNumber} completion for achievements...");
        if (e.StarsAwarded == 3) {
            Debug.Log($"AchievementManager: UNLOCKED 'Perfect Finish!' (Completed Level {e.LevelNumber} with 3 stars)");
        }
        if (e.TimeTaken < 45f) {
            Debug.Log($"AchievementManager: UNLOCKED 'Speed Runner!' (Completed Level {e.LevelNumber} in under 45 seconds)");
        }
    }
}


/*
 * =================================================================================================
 * HOW TO USE THIS EXAMPLE IN UNITY:
 * =================================================================================================
 *
 * 1. Create a new C# script in your Unity project, name it `EventBusExample` (or similar).
 * 2. Copy the ENTIRE content of this file and paste it into your new script, overwriting
 *    any existing code. Save the script.
 *
 * 3. In your Unity scene, create an empty GameObject (e.g., named "GameManagers").
 * 4. Add the following components to the "GameManagers" GameObject:
 *    - `ScoreManager` (This will publish events)
 *    - `ScoreDisplay` (This will subscribe to ScoreChangedEvent)
 *    - `SoundManager` (This will subscribe to PlayerDiedEvent and LevelCompletedEvent)
 *    - `AchievementManager` (This will subscribe to ScoreChangedEvent and LevelCompletedEvent)
 *    (You can also put these on separate GameObjects if it suits your project structure).
 *
 * 5. Run the scene in the Unity Editor.
 *
 * 6. Observe the Console window as you interact with the game using keyboard inputs:
 *    - Press **Spacebar** or **Return**: `ScoreManager` adds score and publishes a `ScoreChangedEvent`.
 *      You will see `ScoreManager`, `ScoreDisplay`, and `AchievementManager` react in the console.
 *    - Press **D**: `ScoreManager` simulates a player death and publishes a `PlayerDiedEvent`.
 *      You will see `ScoreManager` and `SoundManager` react.
 *    - Press **L**: `ScoreManager` simulates level completion and publishes a `LevelCompletedEvent`.
 *      You will see `ScoreManager`, `SoundManager`, and `AchievementManager` react.
 *
 * EXPECTED OUTPUT IN CONSOLE:
 * - You will see messages like "Subscribed to..." and "Unsubscribed from..." when the scene starts and stops.
 * - When you press the keys, you will see the `ScoreManager` publishing events, followed by
 *   the various subscriber components (`ScoreDisplay`, `SoundManager`, `AchievementManager`)
 *   reacting to those events independently.
 * - Notice how the `ScoreManager` (publisher) has no direct knowledge or reference to any of the
 *   subscriber components. This demonstrates the loose coupling achieved by the EventBus pattern.
 *
 *
 * BENEFITS OF THE EVENTBUS PATTERN:
 * ---------------------------------
 * - **Loose Coupling:** Components don't need direct references to each other. A publisher doesn't
 *   know who is listening, and subscribers don't need to know who is publishing.
 * - **Modularity:** New features (e.g., a new achievement system) can be added by simply creating
 *   a new subscriber that listens to existing events, without modifying existing code.
 * - **Testability:** Individual components can be tested in isolation more easily, as their dependencies
 *   on other game systems are reduced to simply publishing/subscribing to the EventBus.
 * - **Scalability:** Makes it easier to manage complex interactions in large projects.
 * - **Clear Communication:** Defines explicit 'events' as the language for system communication.
 *
 * CONSIDERATIONS AND BEST PRACTICES:
 * -----------------------------------
 * - **Performance:** While generally good, excessively frequent events or very large event data
 *   could have a minor performance impact. Use judiciously.
 * - **Debugging:** Can sometimes be harder to trace the flow of execution without direct calls.
 *   Careful logging and clear event names help.
 * - **Overuse:** Don't use for every single interaction. Simple direct calls are often fine for
 *   tightly coupled systems. EventBus shines for cross-cutting concerns or when many-to-many
 *   communication is needed.
 * - **Subscription Management:** ALWAYS pair `Subscribe` with `Unsubscribe` (typically in `OnEnable`/`OnDisable`
 *   for MonoBehaviour components) to prevent memory leaks and ensure handlers aren't called on
 *   inactive or destroyed objects.
 * - **Event Data:** Keep event structs/classes lightweight. Avoid complex logic within event objects.
 * - **Thread Safety:** The provided EventBus includes a `lock` for thread safety, which is good practice
 *   even if most Unity game logic is on the main thread.
 *
 */
```