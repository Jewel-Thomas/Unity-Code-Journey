// Unity Design Pattern Example: EventSystem
// This script demonstrates the EventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script demonstrates the **EventSystem (Publisher/Subscriber)** design pattern. It's designed to be educational, practical, and immediately usable in your Unity projects.

**How to Use This Script:**

1.  **Create a C# Script:** In your Unity project, create a new C# script (e.g., named `GlobalGameEvents`).
2.  **Copy and Paste:** Replace the entire content of the new script with the code provided below.
3.  **Create an EventManager GameObject:** In your Unity scene, create an empty GameObject and name it `EventManager`.
4.  **Attach Script:** Drag the `GlobalGameEvents` script onto the `EventManager` GameObject.
5.  **Create Example GameObjects:** Create three more empty GameObjects in your scene: `GameManager`, `PlayerManager`, and `UIManager`.
6.  **Attach Example Components:**
    *   Drag the `GlobalGameEvents` script onto the `GameManager` GameObject. (It will attach `GameManager` component from the script).
    *   Drag the `GlobalGameEvents` script onto the `PlayerManager` GameObject. (It will attach `PlayerManager` component from the script).
    *   Drag the `GlobalGameEvents` script onto the `UIManager` GameObject. (It will attach `UIManager` component from the script).
7.  **Run the Scene:** Play the scene and observe the Debug.Log messages in the console.
    *   Press `Enter` to start the game.
    *   Press `Space` to simulate player taking damage.
    *   Press `Left Shift` to simulate player gaining score.
    *   Press `Escape` to pause/resume the game.
    *   Observe how components communicate without direct references.

---

```csharp
// Required Unity and C# namespaces
using UnityEngine; // For MonoBehaviour, Debug.Log, etc.
using System;      // For Action, Delegate
using System.Collections.Generic; // For Dictionary

// Namespace for better organization (good practice for larger projects)
namespace MyGame.Events
{
    /// <summary>
    /// EventSystem Design Pattern in Unity.
    ///
    /// This script demonstrates a practical implementation of the EventSystem pattern (also known as Event Bus or Publisher/Subscriber).
    /// It allows different parts of your application to communicate with each other without direct references, promoting
    /// loose coupling and a more modular codebase.
    ///
    /// Components:
    /// 1.  **EventManager (Singleton MonoBehaviour):** The central hub for managing events.
    ///     -   `Subscribe<TEventData>(Action<TEventData> listener)`: Registers a method to be called when an event of `TEventData` type occurs.
    ///     -   `Unsubscribe<TEventData>(Action<TEventData> listener)`: Deregisters a method. Crucial for preventing memory leaks when objects are destroyed.
    ///     -   `Publish<TEventData>(TEventData eventData)`: Triggers an event, notifying all subscribed listeners.
    /// 2.  **Event Data Structures:** Simple C# classes or structs that carry information about a specific event.
    ///     -   E.g., `PlayerHealthChangedEvent`, `ScoreUpdatedEvent`, `GameStateChangedEvent`.
    /// 3.  **Publisher Components:** `MonoBehaviour`s that call `EventManager.Instance.Publish()` when something significant happens.
    ///     -   E.g., `PlayerManager` publishing `PlayerHealthChangedEvent` when player takes damage.
    /// 4.  **Listener Components:** `MonoBehaviour`s that call `EventManager.Instance.Subscribe()` in `OnEnable()` and
    ///     `EventManager.Instance.Unsubscribe()` in `OnDisable()`. They implement methods to handle specific event types.
    ///     -   E.g., `UIManager` subscribing to `PlayerHealthChangedEvent` to update a health bar.
    ///
    /// Why use this pattern?
    /// -   **Loose Coupling:** Components don't need to know about each other. A UI element doesn't need a direct reference to the Player object;
    ///     it just needs to know that a "PlayerHealthChangedEvent" might occur. This makes systems independent.
    /// -   **Modularity & Reusability:** New features (listeners) can be added or removed easily without modifying existing code.
    ///     Want a new analytics system? Just subscribe it to relevant events without touching game logic.
    /// -   **Scalability:** As your project grows, managing dependencies becomes harder. The EventSystem simplifies this by centralizing communication.
    /// -   **Clear Communication:** Events define a clear contract for how different systems communicate, making the codebase easier to understand.
    /// -   **Performance (Consideration):** While powerful, overuse can sometimes lead to difficulty debugging if event chains become complex,
    ///     and direct method calls are generally faster than event dispatches, though for most game logic, the difference is negligible.
    ///
    /// Key Best Practices:
    /// -   **Subscribe in OnEnable, Unsubscribe in OnDisable:** This ensures listeners are active only when their GameObject is active
    ///     and prevents memory leaks when GameObjects are destroyed or deactivated.
    /// -   **Immutable Event Data:** Make event data classes/structs immutable (set values in constructor, use private setters)
    ///     to prevent listeners from accidentally modifying data for other listeners.
    /// -   **Clear Event Naming:** Use descriptive names for event data classes (e.g., `PlayerHealthChangedEvent`).
    /// -   **Avoid Overuse:** Not every single interaction needs an event. Use it for communication between distinct, loosely-coupled systems.
    /// </summary>

    #region 1. EventManager (The Central Hub)

    /// <summary>
    /// The central Event Manager for the application, implemented as a Singleton MonoBehaviour.
    /// It provides generic methods to subscribe to, unsubscribe from, and publish events.
    /// Events are strongly typed using generic type parameters (TEventData).
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        // Singleton instance to allow global access without needing direct references.
        // Public property with a private setter ensures it can only be set internally.
        public static EventManager Instance { get; private set; }

        // A dictionary to store all event subscriptions.
        // Key:   The Type of the event data (e.g., typeof(PlayerHealthChangedEvent)).
        // Value: A Delegate that holds a chain of all Action<TEventData> listeners for that specific event type.
        // Using System.Delegate allows storing different Action<T> types in one dictionary.
        private Dictionary<Type, Delegate> eventDictionary = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Initializes the Singleton instance. Ensures only one EventManager exists in the scene.
        /// `DontDestroyOnLoad` makes it persist across scene changes, which is typical for global managers.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // If another instance already exists, destroy this duplicate.
                Debug.LogWarning("[EventManager] Duplicate EventManager instance detected and destroyed. " +
                                 "Ensure only one EventManager GameObject is present in your scene.");
                Destroy(gameObject);
            }
            else
            {
                // Set this as the singleton instance.
                Instance = this;
                // Make sure this object persists across scene loads.
                // Remove or modify this line if you want a new EventManager per scene,
                // but typically a global EventManager should persist.
                DontDestroyOnLoad(gameObject);
                Debug.Log("[EventManager] Initialized and set as DontDestroyOnLoad.");
            }
        }

        /// <summary>
        /// Subscribes a listener method to a specific event type.
        /// The listener will be invoked whenever an event of type TEventData is published.
        /// </summary>
        /// <typeparam name="TEventData">The type of the event data (e.g., PlayerHealthChangedEvent).</typeparam>
        /// <param name="listener">The method to be called when the event occurs. It must accept TEventData as a parameter.</param>
        public void Subscribe<TEventData>(Action<TEventData> listener)
        {
            // Get the Type object for the generic event data.
            Type eventType = typeof(TEventData);

            // If this event type is not yet in the dictionary, add it with a null delegate.
            if (!eventDictionary.ContainsKey(eventType))
            {
                eventDictionary.Add(eventType, null);
            }

            // Combine the new listener with any existing listeners for this event type.
            // We cast the stored Delegate to Action<TEventData> to correctly use the '+' operator for delegate chaining.
            eventDictionary[eventType] = (Action<TEventData>)eventDictionary[eventType] + listener;
            Debug.Log($"[EventManager] Listener subscribed to event: <color=cyan>{eventType.Name}</color>");
        }

        /// <summary>
        /// Unsubscribes a listener method from a specific event type.
        /// This is crucial to prevent memory leaks and dangling references when objects are destroyed or deactivated.
        /// Always call Unsubscribe when an object is no longer interested in an event (e.g., in OnDisable or OnDestroy).
        /// </summary>
        /// <typeparam name="TEventData">The type of the event data.</typeparam>
        /// <param name="listener">The method to be removed from the subscription list.</param>
        public void Unsubscribe<TEventData>(Action<TEventData> listener)
        {
            // If the singleton instance is null (e.g., application is quitting, or EventManager was destroyed),
            // we can't unsubscribe, so just return.
            if (Instance == null) return;

            Type eventType = typeof(TEventData);

            if (eventDictionary.ContainsKey(eventType))
            {
                // Remove the listener from the existing delegate chain.
                eventDictionary[eventType] = (Action<TEventData>)eventDictionary[eventType] - listener;

                // If there are no more listeners for this event type, remove the entry from the dictionary
                // to keep it clean and avoid holding onto empty delegates.
                if (eventDictionary[eventType] == null)
                {
                    eventDictionary.Remove(eventType);
                    Debug.Log($"[EventManager] All listeners removed for event: <color=grey>{eventType.Name}</color>. Entry cleared.");
                }
                else
                {
                    Debug.Log($"[EventManager] Listener unsubscribed from event: <color=cyan>{eventType.Name}</color>");
                }
            }
            else
            {
                Debug.LogWarning($"[EventManager] Attempted to unsubscribe from event: <color=yellow>{eventType.Name}</color>, but no listeners were found for it.");
            }
        }

        /// <summary>
        /// Publishes an event, triggering all subscribed listeners for that event type.
        /// </summary>
        /// <typeparam name="TEventData">The type of the event data.</typeparam>
        /// <param name="eventData">The actual event data object to be passed to listeners.</param>
        public void Publish<TEventData>(TEventData eventData)
        {
            Type eventType = typeof(TEventData);

            // Check if there are any listeners for this event type.
            if (eventDictionary.TryGetValue(eventType, out Delegate thisEvent))
            {
                // Cast the delegate to the correct Action<TEventData> type and invoke it.
                // The '?' (null-conditional operator) ensures it only invokes if the delegate is not null
                // (i.e., has active listeners).
                (thisEvent as Action<TEventData>)?.Invoke(eventData);
                Debug.Log($"[EventManager] Published event: <color=green>{eventType.Name}</color> with data: {eventData.ToString()}");
            }
            else
            {
                Debug.LogWarning($"[EventManager] Published event: <color=yellow>{eventType.Name}</color>, but no listeners are currently subscribed.");
            }
        }

        /// <summary>
        /// Cleans up the event dictionary when the EventManager GameObject is destroyed.
        /// This is vital to release all references to listener methods, preventing potential memory leaks
        /// and ensuring a clean state, especially important with `DontDestroyOnLoad`.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                eventDictionary.Clear(); // Clear all event listeners from the dictionary.
                Instance = null; // Dereference the singleton to allow garbage collection and indicate it's no longer available.
                Debug.Log("[EventManager] Instance destroyed and all event listeners cleared.");
            }
        }
    }

    #endregion

    #region 2. Event Data Structures

    // These are simple C# classes or structs that carry information about a specific event.
    // They don't need to inherit from anything specific.
    // A good practice is to make them immutable (set properties in constructor, read-only properties)
    // to prevent listeners from accidentally modifying event data for other listeners.

    /// <summary>
    /// Event data for when a player's health changes.
    /// </summary>
    public class PlayerHealthChangedEvent
    {
        public int PlayerID { get; private set; }
        public int OldHealth { get; private set; }
        public int NewHealth { get; private set; }

        public PlayerHealthChangedEvent(int playerID, int oldHealth, int newHealth)
        {
            PlayerID = playerID;
            OldHealth = oldHealth;
            NewHealth = newHealth;
        }

        public override string ToString() => $"Player {PlayerID} Health Changed: {OldHealth} -> {NewHealth}";
    }

    /// <summary>
    /// Event data for when a player's score is updated.
    /// </summary>
    public class ScoreUpdatedEvent
    {
        public int PlayerID { get; private set; }
        public int CurrentScore { get; private set; }
        public int ScoreDelta { get; private set; } // How much the score changed by

        public ScoreUpdatedEvent(int playerID, int currentScore, int scoreDelta)
        {
            PlayerID = playerID;
            CurrentScore = currentScore;
            ScoreDelta = scoreDelta;
        }

        public override string ToString() => $"Player {PlayerID} Score Updated: +{ScoreDelta}, Total: {CurrentScore}";
    }

    /// <summary>
    /// Enumeration of possible game states.
    /// </summary>
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Event data for when the overall game state changes.
    /// </summary>
    public class GameStateChangedEvent
    {
        public GameState OldState { get; private set; }
        public GameState NewState { get; private set; }

        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public override string ToString() => $"Game State Changed: {OldState} -> {NewState}";
    }

    #endregion

    #region 3. Example Publisher/Listener Components

    /// <summary>
    /// Example component that simulates player actions and state.
    /// It acts as a **Publisher** for `PlayerHealthChangedEvent` and `ScoreUpdatedEvent`.
    /// It also acts as a **Listener** for `GameStateChangedEvent` to react to game state changes.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private int playerID = 1; // Customizable player ID in Inspector
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int currentScore = 0;

        private GameState currentGameState; // Keep track of current game state

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// This is the ideal place to subscribe to events.
        /// </summary>
        private void OnEnable()
        {
            // Subscribe to events when this component is enabled.
            // We are interested in knowing when the game state changes.
            EventManager.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            Debug.Log($"[PlayerManager] Subscribed to GameStateChangedEvent.");
        }

        /// <summary>
        /// Called when the object becomes disabled or inactive.
        /// This is the crucial place to unsubscribe from events to prevent memory leaks and
        /// errors if the object is destroyed while still subscribed.
        /// </summary>
        private void OnDisable()
        {
            // Unsubscribe from events when this component is disabled.
            EventManager.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            Debug.Log($"[PlayerManager] Unsubscribed from GameStateChangedEvent.");
        }

        /// <summary>
        /// Called on the frame when a script is enabled, just before any of the Update methods are called the first time.
        /// </summary>
        void Start()
        {
            // Publish initial health and score events. This is useful for UI elements
            // that might need to display these values immediately upon scene load.
            EventManager.Instance.Publish(new PlayerHealthChangedEvent(playerID, currentHealth, currentHealth));
            EventManager.Instance.Publish(new ScoreUpdatedEvent(playerID, currentScore, 0));
        }

        /// <summary>
        /// Update is called once per frame. Used here to simulate player input.
        /// </summary>
        void Update()
        {
            // Only process player inputs if the game is in the 'Playing' state.
            if (currentGameState != GameState.Playing) return;

            // Simulate taking damage when 'Space' key is pressed.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TakeDamage(10);
            }

            // Simulate gaining score when 'Left Shift' key is pressed.
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                AddScore(50);
            }
        }

        /// <summary>
        /// Simulates taking damage and publishes a `PlayerHealthChangedEvent`.
        /// </summary>
        /// <param name="amount">Amount of damage to take.</param>
        public void TakeDamage(int amount)
        {
            int oldHealth = currentHealth;
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0; // Ensure health doesn't go below zero

            Debug.Log($"[PlayerManager] Player {playerID} takes {amount} damage. Health: {currentHealth}");

            // Publish the event. Any subscribed listener (e.g., UIManager, GameManager) will be notified.
            EventManager.Instance.Publish(new PlayerHealthChangedEvent(playerID, oldHealth, currentHealth));
        }

        /// <summary>
        /// Simulates gaining score and publishes a `ScoreUpdatedEvent`.
        /// </summary>
        /// <param name="amount">Amount of score to add.</param>
        public void AddScore(int amount)
        {
            currentScore += amount;
            Debug.Log($"[PlayerManager] Player {playerID} gained {amount} score. Total Score: {currentScore}");

            // Publish the event.
            EventManager.Instance.Publish(new ScoreUpdatedEvent(playerID, currentScore, amount));
        }

        /// <summary>
        /// Listener method for `GameStateChangedEvent`. This method is called by the EventManager
        /// whenever a `GameStateChangedEvent` is published.
        /// </summary>
        /// <param name="eventData">The event data containing old and new game states.</param>
        private void OnGameStateChanged(GameStateChangedEvent eventData)
        {
            currentGameState = eventData.NewState; // Update the player manager's internal state.
            Debug.Log($"[PlayerManager] Received GameStateChangedEvent. New state: {currentGameState}");
            // Example reaction: if the game pauses, you might disable specific player inputs or animations here.
        }
    }

    /// <summary>
    /// Example component that represents a UI display (e.g., health bar, score display).
    /// It acts purely as a **Listener** for `PlayerHealthChangedEvent` and `ScoreUpdatedEvent`.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // In a real project, these would be references to actual UI Text, Image components, etc.
        // For this example, we'll just use Debug.Log to simulate UI updates.

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// Subscribe to relevant events here.
        /// </summary>
        private void OnEnable()
        {
            // Subscribe to events that affect the UI display.
            EventManager.Instance.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventManager.Instance.Subscribe<ScoreUpdatedEvent>(OnScoreUpdated);
            Debug.Log($"[UIManager] Subscribed to PlayerHealthChangedEvent and ScoreUpdatedEvent.");
        }

        /// <summary>
        /// Called when the object becomes disabled or inactive.
        /// Unsubscribe from events here to prevent errors and memory leaks.
        /// </summary>
        private void OnDisable()
        {
            // Unsubscribe from events.
            EventManager.Instance.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventManager.Instance.Unsubscribe<ScoreUpdatedEvent>(OnScoreUpdated);
            Debug.Log($"[UIManager] Unsubscribed from PlayerHealthChangedEvent and ScoreUpdatedEvent.");
        }

        /// <summary>
        /// Listener method for `PlayerHealthChangedEvent`. This updates the simulated UI health display.
        /// </summary>
        /// <param name="eventData">The event data containing player health information.</param>
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
        {
            // In a real UI, you would update a health bar image's fill amount or a Text component here.
            Debug.Log($"[UIManager] <color=magenta>UI Health Update for Player {eventData.PlayerID}: {eventData.NewHealth} HP (from {eventData.OldHealth})</color>");
            // Example: healthBarImage.fillAmount = (float)eventData.NewHealth / maxHealth;
            // Example: healthText.text = $"HP: {eventData.NewHealth}";
        }

        /// <summary>
        /// Listener method for `ScoreUpdatedEvent`. This updates the simulated UI score display.
        /// </summary>
        /// <param name="eventData">The event data containing player score information.</param>
        private void OnScoreUpdated(ScoreUpdatedEvent eventData)
        {
            // In a real UI, you would update a score Text component here.
            Debug.Log($"[UIManager] <color=magenta>UI Score Update for Player {eventData.PlayerID}: Current Score {eventData.CurrentScore} (gained {eventData.ScoreDelta})</color>");
            // Example: scoreText.text = $"Score: {eventData.CurrentScore}";
        }
    }

    /// <summary>
    /// Example component that manages the overall game state.
    /// It acts as a **Publisher** for `GameStateChangedEvent`.
    /// It also acts as a **Listener** for `PlayerHealthChangedEvent` (e.g., to detect game over conditions).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameState currentGameState = GameState.Loading; // Initial game state

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// Subscribe to events relevant to game state management here.
        /// </summary>
        private void OnEnable()
        {
            // We want to know if player health reaches zero to potentially trigger a Game Over.
            EventManager.Instance.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            Debug.Log($"[GameManager] Subscribed to PlayerHealthChangedEvent.");
        }

        /// <summary>
        /// Called when the object becomes disabled or inactive.
        /// Unsubscribe from events here.
        /// </summary>
        private void OnDisable()
        {
            EventManager.Instance.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            Debug.Log($"[GameManager] Unsubscribed from PlayerHealthChangedEvent.");
        }

        /// <summary>
        /// Called on the frame when a script is enabled, just before any of the Update methods are called the first time.
        /// </summary>
        void Start()
        {
            // Set the initial game state after loading and publish the event.
            SetGameState(GameState.MainMenu);
        }

        /// <summary>
        /// Update is called once per frame. Used here to simulate changing game states via input.
        /// </summary>
        void Update()
        {
            // Simulate changing game states based on user input.
            if (currentGameState == GameState.MainMenu && Input.GetKeyDown(KeyCode.Return))
            {
                SetGameState(GameState.Playing); // Start game
            }
            else if (currentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                SetGameState(GameState.Paused); // Pause game
            }
            else if (currentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
            {
                SetGameState(GameState.Playing); // Resume game
            }
            else if (currentGameState == GameState.GameOver && Input.GetKeyDown(KeyCode.R))
            {
                // In a real game, you would reload the scene or reset game elements here.
                Debug.Log($"[GameManager] Resetting game... (simulated)");
                // Application.LoadLevel(Application.loadedLevel); // Example for full scene reload
                SetGameState(GameState.MainMenu);
            }
        }

        /// <summary>
        /// Changes the game's current state and publishes a `GameStateChangedEvent`.
        /// </summary>
        /// <param name="newState">The new game state.</param>
        public void SetGameState(GameState newState)
        {
            if (currentGameState == newState) return; // No change needed

            GameState oldState = currentGameState;
            currentGameState = newState;
            Debug.Log($"[GameManager] Game State changed from {oldState} to {newState}.");

            // Publish the event. Any subscribed system (e.g., PlayerManager, MusicManager) will be notified.
            EventManager.Instance.Publish(new GameStateChangedEvent(oldState, newState));
        }

        /// <summary>
        /// Listener method for `PlayerHealthChangedEvent`.
        /// This is where the GameManager might react to a player's health, e.g., trigger game over.
        /// </summary>
        /// <param name="eventData">The event data containing player health information.</param>
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
        {
            // If any player's health drops to 0 or below, and the game is currently playing,
            // trigger the Game Over state.
            if (eventData.NewHealth <= 0 && currentGameState == GameState.Playing)
            {
                Debug.Log($"[GameManager] Player {eventData.PlayerID} health reached 0. Triggering Game Over!");
                SetGameState(GameState.GameOver);
            }
        }
    }

    #endregion
}
```