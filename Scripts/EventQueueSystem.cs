// Unity Design Pattern Example: EventQueueSystem
// This script demonstrates the EventQueueSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The EventQueueSystem design pattern is a powerful way to decouple different parts of your game logic. Instead of components directly calling methods on each other, they publish events to a central queue, and other components subscribe to specific event types to react. This promotes loose coupling, making your code more modular, maintainable, and scalable.

Here's a complete C# Unity script demonstrating the EventQueueSystem pattern. It includes event definitions, the core queue system, and example MonoBehaviours for producing and consuming events, all within a single file for easy setup.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for Queue<string>.Reverse() in UI logging example

// This script demonstrates the EventQueueSystem design pattern in Unity.
// It provides a centralized, decoupled way for different parts of your game
// to communicate by publishing and subscribing to events through a queue.

// The entire system (event definitions, the queue system, and example MonoBehaviours)
// is contained within this single file for easy drop-in and demonstration.

#region Event Definitions

/// <summary>
/// Defines the different types of events that can be processed by the EventQueueSystem.
/// Add new event types here as your game requires.
/// This enum acts as a key for identifying and routing events.
/// </summary>
public enum EventType
{
    None, // Default or unassigned event type
    PlayerDied,
    EnemyHit,
    ItemCollected,
    QuestCompleted,
    // Add more specific event types here as your game evolves
}

/// <summary>
/// Base abstract class for all game events.
/// All specific game events should inherit from this class.
/// It ensures all events have a type and a timestamp, which can be useful for debugging
/// or for systems that need to know when an event occurred.
/// </summary>
public abstract class GameEvent
{
    public EventType Type { get; protected set; }
    public DateTime Timestamp { get; private set; }

    public GameEvent()
    {
        Timestamp = DateTime.UtcNow; // Record when the event was created
    }

    public override string ToString()
    {
        // Provides a basic string representation for logging
        return $"[Event] Type: {Type}, Time: {Timestamp:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Concrete event for when a player character dies.
/// Contains specific data related to the event, such as player name, ID, and location.
/// This data is carried along with the event to its listeners.
/// </summary>
public class PlayerDiedEvent : GameEvent
{
    public string PlayerName { get; private set; }
    public int PlayerID { get; private set; }
    public Vector3 DeathLocation { get; private set; }

    public PlayerDiedEvent(string playerName, int playerID, Vector3 deathLocation)
    {
        Type = EventType.PlayerDied; // Assign the specific event type
        PlayerName = playerName;
        PlayerID = playerID;
        DeathLocation = deathLocation;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Player: {PlayerName} (ID:{PlayerID}), Location: {DeathLocation}";
    }
}

/// <summary>
/// Concrete event for when an enemy character is hit.
/// Carries information about the enemy, damage dealt, and the attacker.
/// </summary>
public class EnemyHitEvent : GameEvent
{
    public int EnemyID { get; private set; }
    public float DamageDealt { get; private set; }
    public string AttackerName { get; private set; }

    public EnemyHitEvent(int enemyID, float damageDealt, string attackerName)
    {
        Type = EventType.EnemyHit;
        EnemyID = enemyID;
        DamageDealt = damageDealt;
        AttackerName = attackerName;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, EnemyID: {EnemyID}, Damage: {DamageDealt:F1}, Attacker: {AttackerName}";
    }
}

/// <summary>
/// Concrete event for when an item is collected by a player.
/// Includes details about the item, quantity, and the collector.
/// </summary>
public class ItemCollectedEvent : GameEvent
{
    public string ItemName { get; private set; }
    public int Quantity { get; private set; }
    public int CollectorID { get; private set; }

    public ItemCollectedEvent(string itemName, int quantity, int collectorID)
    {
        Type = EventType.ItemCollected;
        ItemName = itemName;
        Quantity = quantity;
        CollectorID = collectorID;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Item: {ItemName} (x{Quantity}), Collector: {CollectorID}";
    }
}

/// <summary>
/// Concrete event for when a quest is completed by a player.
/// Contains information about the quest and the player who completed it.
/// </summary>
public class QuestCompletedEvent : GameEvent
{
    public string QuestName { get; private set; }
    public int QuestID { get; private set; }
    public int PlayerID { get; private set; }

    public QuestCompletedEvent(string questName, int questID, int playerID)
    {
        Type = EventType.QuestCompleted;
        QuestName = questName;
        QuestID = questID;
        PlayerID = playerID;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, Quest: {QuestName} (ID:{QuestID}), Player: {PlayerID}";
    }
}

#endregion

#region EventQueueSystem Core Logic

/// <summary>
/// The core static class for the Event Queue System.
/// It manages a queue of events waiting to be processed and a dictionary of event listeners (subscribers).
/// This system achieves strong decoupling:
/// - Event Producers don't know who (or if anyone) is listening. They just publish.
/// - Event Consumers don't know who produced an event. They just subscribe and react.
/// - Events are processed asynchronously (in a later frame/update tick), preventing immediate cascading calls.
/// </summary>
public static class EventQueueSystem
{
    // A queue to hold events that are waiting to be processed.
    // Events are added to the end (Enqueue) and processed from the front (Dequeue) following FIFO (First-In, First-Out).
    private static readonly Queue<GameEvent> _eventQueue = new Queue<GameEvent>();

    // A dictionary to map EventTypes to lists of actions (delegates).
    // Each action is a delegate that takes a GameEvent as an argument.
    // When an event of a certain type is processed, all actions in its corresponding list are invoked.
    private static readonly Dictionary<EventType, List<Action<GameEvent>>> _eventListeners =
        new Dictionary<EventType, List<Action<GameEvent>>>();

    // A flag to prevent re-entrant calls to ProcessQueue.
    // This is important to avoid issues if an event handler itself publishes events,
    // which could lead to an infinite loop or unexpected order of operations if processed immediately.
    private static bool _isProcessingQueue = false;

    // --- Public Methods ---

    /// <summary>
    /// Publishes an event to the queue.
    /// The event is not processed immediately but is added to the queue
    /// and will be processed during the next call to <see cref="ProcessQueue"/>.
    /// </summary>
    /// <param name="gameEvent">The event instance to be published.</param>
    public static void Publish(GameEvent gameEvent)
    {
        if (gameEvent == null)
        {
            Debug.LogWarning("[EventQueueSystem] Attempted to publish a null event. Aborting.");
            return;
        }

        // Using 'lock' ensures thread-safety for queue operations.
        // In most single-threaded Unity game logic, this might not be strictly necessary,
        // but it's good practice for robust systems, especially if events could be published
        // from background threads (e.g., async operations or network threads).
        lock (_eventQueue)
        {
            _eventQueue.Enqueue(gameEvent);
        }
        Debug.Log($"[EventQueueSystem] Event published: {gameEvent.Type} (Queue size: {_eventQueue.Count})");
    }

    /// <summary>
    /// Subscribes a listener (a method/action) to a specific event type.
    /// When an event of the specified type is processed from the queue,
    /// the provided action will be invoked with the event data.
    /// </summary>
    /// <param name="eventType">The type of event to listen for.</param>
    /// <param name="listener">The action (method) to be invoked when the event occurs.
    ///                        This action will receive a <see cref="GameEvent"/> parameter.</param>
    public static void Subscribe(EventType eventType, Action<GameEvent> listener)
    {
        if (listener == null)
        {
            Debug.LogWarning($"[EventQueueSystem] Attempted to subscribe a null listener for {eventType}. Aborting.");
            return;
        }

        lock (_eventListeners)
        {
            // If there are no listeners for this event type yet, create a new list.
            if (!_eventListeners.ContainsKey(eventType))
            {
                _eventListeners.Add(eventType, new List<Action<GameEvent>>());
            }
            // Add the listener to the list for this event type.
            _eventListeners[eventType].Add(listener);
        }
        Debug.Log($"[EventQueueSystem] Listener subscribed to {eventType}.");
    }

    /// <summary>
    /// Unsubscribes a listener from a specific event type.
    /// This is crucial for preventing memory leaks and 'MissingReferenceException' errors
    /// when GameObjects (that contain the subscribed methods) are disabled or destroyed.
    /// Always call this when a listener is no longer active (e.g., in OnDisable or OnDestroy).
    /// </summary>
    /// <param name="eventType">The type of event to unsubscribe from.</param>
    /// <param name="listener">The specific action (method) instance that was previously subscribed.</param>
    public static void Unsubscribe(EventType eventType, Action<GameEvent> listener)
    {
        if (listener == null)
        {
            Debug.LogWarning($"[EventQueueSystem] Attempted to unsubscribe a null listener for {eventType}. Aborting.");
            return;
        }

        lock (_eventListeners)
        {
            if (_eventListeners.TryGetValue(eventType, out List<Action<GameEvent>> listeners))
            {
                // Remove the specific listener instance.
                // Using RemoveAll with a predicate is robust, especially if an identical listener
                // somehow got added multiple times (though generally, a listener should only subscribe once).
                int removedCount = listeners.RemoveAll(l => l == listener);
                if (removedCount > 0)
                {
                    Debug.Log($"[EventQueueSystem] Listener unsubscribed from {eventType}. Removed {removedCount} instances.");
                }
                else
                {
                    Debug.LogWarning($"[EventQueueSystem] Listener not found for {eventType} during unsubscribe attempt. " +
                                     "It might have already been unsubscribed or was never subscribed with this exact instance.");
                }

                // Optionally, clean up the list for the event type if it becomes empty.
                if (listeners.Count == 0)
                {
                    _eventListeners.Remove(eventType);
                }
            }
            else
            {
                Debug.LogWarning($"[EventQueueSystem] No listeners registered for {eventType} during unsubscribe attempt.");
            }
        }
    }

    /// <summary>
    /// Processes all events currently in the queue.
    /// This method should be called regularly by a central manager (e.g., <see cref="EventManager"/> MonoBehaviour)
    /// during your game loop (e.g., in Update or FixedUpdate).
    /// It dequeues events one by one and dispatches them to their registered listeners.
    /// </summary>
    public static void ProcessQueue()
    {
        // Prevent re-entrant calls to avoid complex logic and potential stack overflows
        // if a listener publishes an event and that event immediately tries to process the queue again.
        if (_isProcessingQueue)
        {
            // Debug.LogWarning("[EventQueueSystem] ProcessQueue called re-entrantly. Skipping this call.");
            return;
        }

        _isProcessingQueue = true; // Set flag to indicate processing is active

        try
        {
            // Create a temporary list to hold events dequeued from the main queue.
            // This is crucial: we process a snapshot of events that were in the queue
            // when ProcessQueue started. This prevents an infinite loop if an event handler
            // itself adds new events to the queue, as those new events will be processed
            // in the *next* call to ProcessQueue, not the current one.
            List<GameEvent> eventsToProcess = new List<GameEvent>();
            lock (_eventQueue)
            {
                while (_eventQueue.Count > 0)
                {
                    eventsToProcess.Add(_eventQueue.Dequeue());
                }
            }

            // Iterate through the snapshot of events and dispatch them.
            foreach (GameEvent gameEvent in eventsToProcess)
            {
                // Debug.Log($"[EventQueueSystem] Dispatching event: {gameEvent}");

                // Get the list of listeners for this specific event type.
                List<Action<GameEvent>> currentListeners;
                lock (_eventListeners)
                {
                    // If no listeners are registered for this event type, skip to the next event.
                    if (!_eventListeners.TryGetValue(gameEvent.Type, out currentListeners))
                    {
                        // Debug.Log($"[EventQueueSystem] No listeners for event type: {gameEvent.Type}");
                        continue;
                    }
                    // Create a *copy* of the listener list. This prevents issues if a listener
                    // unsubscribes itself or other listeners while we are iterating through the list.
                    currentListeners = new List<Action<GameEvent>>(currentListeners);
                }

                // Invoke each listener for the current event.
                foreach (Action<GameEvent> listener in currentListeners)
                {
                    try
                    {
                        // Safely invoke the listener. Any exceptions in a listener won't stop
                        // the processing of other listeners or other events.
                        listener.Invoke(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventQueueSystem] Error invoking listener for event {gameEvent.Type}: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
                    }
                }
            }
        }
        finally
        {
            // Ensure the processing flag is reset even if an unhandled exception occurs.
            _isProcessingQueue = false;
        }
    }

    /// <summary>
    /// Clears all pending events from the queue and removes all registered listeners.
    /// Use with caution, typically for scene changes, application shutdown, or major system resets.
    /// </summary>
    public static void Reset()
    {
        lock (_eventQueue)
        {
            _eventQueue.Clear();
        }
        lock (_eventListeners)
        {
            _eventListeners.Clear();
        }
        Debug.Log("[EventQueueSystem] System reset: Queue cleared, all listeners removed.");
    }
}

#endregion

#region Unity Integration - EventManager MonoBehaviour (The Driver)

/// <summary>
/// A MonoBehaviour that acts as the central driver for the EventQueueSystem.
/// It's responsible for regularly calling <see cref="EventQueueSystem.ProcessQueue()"/>
/// to process pending events.
/// This component should exist once in your scene (e.g., on a "GameManager" object).
/// </summary>
public class EventManager : MonoBehaviour
{
    // Singleton pattern for easy global access to this manager.
    public static EventManager Instance { get; private set; }

    [Tooltip("How often (in seconds) the event queue should be processed.")]
    [SerializeField] private float _processInterval = 0.05f; // Process every 50 milliseconds
    private float _timer;

    void Awake()
    {
        // Implement the singleton pattern to ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EventManager] Duplicate EventManager found, destroying this instance to maintain singleton integrity.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, prevent this GameObject from being destroyed when loading new scenes.
            // This is common for managers that need to persist throughout the game.
            DontDestroyOnLoad(gameObject);
            Debug.Log("[EventManager] Initialized successfully.");
        }
    }

    void Update()
    {
        // Use a timer to control how often the queue is processed.
        // This prevents excessive processing if the queue is usually empty
        // and provides a controlled "tick" for event handling.
        _timer += Time.deltaTime;
        if (_timer >= _processInterval)
        {
            EventQueueSystem.ProcessQueue(); // Call the static method to process events
            _timer = 0f; // Reset timer for the next interval
        }
    }

    void OnApplicationQuit()
    {
        // It's good practice to reset the system when the application is closing
        // to ensure no pending events or listeners remain.
        EventQueueSystem.Reset();
        Debug.Log("[EventManager] Application quitting, EventQueueSystem reset.");
    }
}

#endregion

#region Unity Integration - Example Event Producers (Source of Events)

/// <summary>
/// Example MonoBehaviour that simulates game entities producing events.
/// This could be a Player script, Enemy AI, Item Spawner, Quest Giver, etc.
/// It demonstrates how to create and publish specific <see cref="GameEvent"/> types.
/// </summary>
public class EventProducer : MonoBehaviour
{
    [Header("Producer Settings")]
    public string ProducerName = "Generic Producer";
    public int ProducerID = 100; // A unique identifier for this producer

    [Header("Event Simulation Controls")]
    public bool AutoPublishEvents = false;
    public float AutoPublishInterval = 2f;
    private float _autoPublishTimer;

    // Simple counters for unique event data in simulation
    private int _enemyHitCounter = 0;
    private int _itemCollectedCounter = 0;
    private int _questCompletedCounter = 0;

    void Update()
    {
        // Automatic event publishing for continuous demonstration
        if (AutoPublishEvents)
        {
            _autoPublishTimer += Time.deltaTime;
            if (_autoPublishTimer >= AutoPublishInterval)
            {
                SimulateRandomEvent(); // Publish a random event type
                _autoPublishTimer = 0f;
            }
        }

        // Manual event triggers via keyboard input for specific demonstrations
        if (Input.GetKeyDown(KeyCode.D)) SimulatePlayerDeath();
        if (Input.GetKeyDown(KeyCode.H)) SimulateEnemyHit();
        if (Input.GetKeyDown(KeyCode.I)) SimulateItemCollection();
        if (Input.GetKeyDown(KeyCode.Q)) SimulateQuestCompletion();
    }

    /// <summary>
    /// Publishes a <see cref="PlayerDiedEvent"/>.
    /// </summary>
    public void SimulatePlayerDeath()
    {
        Debug.Log($"[{ProducerName}] Triggering Player Death event...");
        // Create an instance of the specific event type with relevant data
        PlayerDiedEvent playerDied = new PlayerDiedEvent("Player One", ProducerID, transform.position);
        EventQueueSystem.Publish(playerDied); // Publish the event to the queue
    }

    /// <summary>
    /// Publishes an <see cref="EnemyHitEvent"/>.
    /// </summary>
    public void SimulateEnemyHit()
    {
        Debug.Log($"[{ProducerName}] Triggering Enemy Hit event...");
        _enemyHitCounter++;
        // Create and publish an EnemyHitEvent with dynamic damage and enemy ID
        EnemyHitEvent enemyHit = new EnemyHitEvent(ProducerID + _enemyHitCounter, UnityEngine.Random.Range(10f, 50f), ProducerName);
        EventQueueSystem.Publish(enemyHit);
    }

    /// <summary>
    /// Publishes an <see cref="ItemCollectedEvent"/>.
    /// </summary>
    public void SimulateItemCollection()
    {
        Debug.Log($"[{ProducerName}] Triggering Item Collected event...");
        _itemCollectedCounter++;
        // Publish a "Coin" event, and occasionally a "Health Potion"
        EventQueueSystem.Publish(new ItemCollectedEvent("Coin", 1, ProducerID));
        if (_itemCollectedCounter % 5 == 0)
        {
            EventQueueSystem.Publish(new ItemCollectedEvent("Health Potion", 1, ProducerID));
        }
    }

    /// <summary>
    /// Publishes a <see cref="QuestCompletedEvent"/>.
    /// </summary>
    public void SimulateQuestCompletion()
    {
        Debug.Log($"[{ProducerName}] Triggering Quest Completed event...");
        _questCompletedCounter++;
        EventQueueSystem.Publish(new QuestCompletedEvent($"Mighty Quest {_questCompletedCounter}", 100 + _questCompletedCounter, ProducerID));
    }

    /// <summary>
    /// Randomly picks and simulates one of the defined events.
    /// </summary>
    private void SimulateRandomEvent()
    {
        int randomEvent = UnityEngine.Random.Range(0, 4); // 0-3 for the 4 event types
        switch (randomEvent)
        {
            case 0: SimulatePlayerDeath(); break;
            case 1: SimulateEnemyHit(); break;
            case 2: SimulateItemCollection(); break;
            case 3: SimulateQuestCompletion(); break;
        }
    }
}

#endregion

#region Unity Integration - Example Event Consumers (Reactors to Events)

/// <summary>
/// Example MonoBehaviour that simulates game entities consuming events.
/// This could be a UI Manager, Score Manager, Achievement System, Sound Manager, Debug Logger, etc.
/// It demonstrates how to subscribe to specific event types and react to their data.
/// </summary>
public class EventConsumer : MonoBehaviour
{
    [Header("Consumer Settings")]
    public string ConsumerName = "Generic Consumer";

    [Header("Events to Listen For (Check all that apply in Inspector)")]
    public bool ListenToPlayerDied = false;
    public bool ListenToEnemyHit = false;
    public bool ListenToItemCollected = false;
    public bool ListenToQuestCompleted = false;

    // Optional: Reference to a UI Text component for displaying logs directly in the game view.
    [SerializeField] private UnityEngine.UI.Text _logText;
    private const int MAX_LOG_LINES = 10;
    private Queue<string> _logBuffer = new Queue<string>(); // Buffer for UI log lines

    void OnEnable()
    {
        // IMPORTANT: Subscribe to events when the GameObject becomes active.
        // The event system uses delegates, which point to methods.
        // We subscribe based on the public boolean flags set in the Inspector.
        if (ListenToPlayerDied) EventQueueSystem.Subscribe(EventType.PlayerDied, HandlePlayerDiedEvent);
        if (ListenToEnemyHit) EventQueueSystem.Subscribe(EventType.EnemyHit, HandleEnemyHitEvent);
        if (ListenToItemCollected) EventQueueSystem.Subscribe(EventType.ItemCollected, HandleItemCollectedEvent);
        if (ListenToQuestCompleted) EventQueueSystem.Subscribe(EventType.QuestCompleted, HandleQuestCompletedEvent);
        Debug.Log($"[{ConsumerName}] Subscribed to selected events.");
    }

    void OnDisable()
    {
        // IMPORTANT: Unsubscribe from events when the GameObject is disabled or destroyed.
        // Failing to unsubscribe leads to:
        // 1. Memory Leaks: The EventQueueSystem would still hold a reference to this GameObject's methods,
        //    preventing the GameObject from being garbage collected.
        // 2. MissingReferenceException: If the EventQueueSystem tries to invoke a method on a destroyed
        //    GameObject, Unity will throw an error.
        if (ListenToPlayerDied) EventQueueSystem.Unsubscribe(EventType.PlayerDied, HandlePlayerDiedEvent);
        if (ListenToEnemyHit) EventQueueSystem.Unsubscribe(EventType.EnemyHit, HandleEnemyHitEvent);
        if (ListenToItemCollected) EventQueueSystem.Unsubscribe(EventType.ItemCollected, HandleItemCollectedEvent);
        if (ListenToQuestCompleted) EventQueueSystem.Unsubscribe(EventType.QuestCompleted, HandleQuestCompletedEvent);
        Debug.Log($"[{ConsumerName}] Unsubscribed from selected events.");
    }

    // --- Event Handler Methods ---
    // Each handler receives a generic GameEvent. It then casts it to the specific
    // event type to access its unique data. This is a common pattern for type-safe
    // handling of different event types through a common delegate signature.

    private void HandlePlayerDiedEvent(GameEvent gameEvent)
    {
        // Cast the base GameEvent to the specific type to access its unique properties.
        PlayerDiedEvent playerDiedEvent = gameEvent as PlayerDiedEvent;
        if (playerDiedEvent != null) // Always check for null after casting
        {
            string message = $"[{ConsumerName}] NOTIFIED: Player '{playerDiedEvent.PlayerName}' (ID:{playerDiedEvent.PlayerID}) died at {playerDiedEvent.DeathLocation}!";
            Debug.Log(message);
            LogToUI(message);
            // Example real-world actions:
            // - Play a "Game Over" sound.
            // - Display a "Player Died" UI screen.
            // - Update a player statistics manager.
        }
    }

    private void HandleEnemyHitEvent(GameEvent gameEvent)
    {
        EnemyHitEvent enemyHitEvent = gameEvent as EnemyHitEvent;
        if (enemyHitEvent != null)
        {
            string message = $"[{ConsumerName}] NOTIFIED: Enemy (ID:{enemyHitEvent.EnemyID}) took {enemyHitEvent.DamageDealt:F1} damage from {enemyHitEvent.AttackerName}!";
            Debug.Log(message);
            LogToUI(message);
            // Example real-world actions:
            // - Apply damage to the enemy's health component.
            // - Play a hit sound effect.
            // - Spawn particle effects.
            // - Update an enemy health bar UI.
        }
    }

    private void HandleItemCollectedEvent(GameEvent gameEvent)
    {
        ItemCollectedEvent itemCollectedEvent = gameEvent as ItemCollectedEvent;
        if (itemCollectedEvent != null)
        {
            string message = $"[{ConsumerName}] NOTIFIED: Player (ID:{itemCollectedEvent.CollectorID}) collected {itemCollectedEvent.Quantity} x {itemCollectedEvent.ItemName}!";
            Debug.Log(message);
            LogToUI(message);
            // Example real-world actions:
            // - Add item to player's inventory.
            // - Update player's score.
            // - Play a pickup sound.
            // - Show a "Item Collected" UI notification.
        }
    }

    private void HandleQuestCompletedEvent(GameEvent gameEvent)
    {
        QuestCompletedEvent questCompletedEvent = gameEvent as QuestCompletedEvent;
        if (questCompletedEvent != null)
        {
            string message = $"[{ConsumerName}] NOTIFIED: Quest '{questCompletedEvent.QuestName}' (ID:{questCompletedEvent.QuestID}) completed by Player (ID:{questCompletedEvent.PlayerID})!";
            Debug.Log(message);
            LogToUI(message);
            // Example real-world actions:
            // - Grant quest rewards (XP, gold, items).
            // - Unlock new quests or story content.
            // - Show an achievement pop-up.
        }
    }

    // --- UI Logging Helper ---
    // This helper method updates the optional UI Text element with event logs.
    private void LogToUI(string message)
    {
        if (_logText == null) return;

        // Add the new message to the buffer
        _logBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");

        // Keep only the last MAX_LOG_LINES to prevent the UI from becoming too long
        while (_logBuffer.Count > MAX_LOG_LINES)
        {
            _logBuffer.Dequeue();
        }

        // Update the UI text by joining the buffered messages.
        // .Reverse() is used so the newest message appears at the top.
        _logText.text = string.Join("\n", _logBuffer.Reverse());
    }
}

#endregion

/*
*   ==========================================================================================
*   HOW TO USE THIS SCRIPT IN UNITY:
*   ==========================================================================================
*
*   1.  **Create a C# Script File:**
*       -   In your Unity project, create a new C# script (e.g., "EventQueueSystemExample.cs").
*       -   Copy the *entire content* of this file (from top `using` statements to this comment block)
*           and paste it into your new C# script file, replacing any default content.
*
*   2.  **Set up the EventManager (The Central Hub):**
*       -   In your Unity scene, create an empty GameObject (e.g., name it "GameManager").
*       -   Drag and drop the `EventManager` component (from your "EventQueueSystemExample.cs" script)
*           onto this "GameManager" GameObject in the Inspector.
*       -   This `EventManager` will be responsible for periodically processing the event queue.
*           You can adjust the "Process Interval" in its Inspector (e.g., 0.05 seconds means events are processed every 50ms).
*           It also implements a basic singleton pattern and `DontDestroyOnLoad` for persistence.
*
*   3.  **Create Event Producers (The Event Creators):**
*       -   Create one or more empty GameObjects in your scene (e.g., "Player", "EnemySpawner", "QuestGiver").
*       -   Drag and drop the `EventProducer` component onto each of these GameObjects.
*       -   In the Inspector for each `EventProducer`:
*           -   Set a unique "Producer Name" and "Producer ID".
*           -   Optionally, check "Auto Publish Events" to have them automatically trigger events at a set interval.
*           -   When running the scene, you can manually trigger events by selecting a `EventProducer` GameObject
*               and pressing the following keys:
*               -   'D': Simulate Player Died
*               -   'H': Simulate Enemy Hit
*               -   'I': Simulate Item Collected
*               -   'Q': Simulate Quest Completed
*
*   4.  **Create Event Consumers (The Event Reactors):**
*       -   Create one or more empty GameObjects in your scene (e.g., "UIManager", "ScoreDisplay", "AchievementSystem").
*       -   Drag and drop the `EventConsumer` component onto each of these GameObjects.
*       -   In the Inspector for each `EventConsumer`:
*           -   Set a unique "Consumer Name".
*           -   Check the "Events to Listen For" checkboxes to specify which event types this consumer should react to.
*           -   **For UI Logging (Optional but Recommended for Demo):**
*               -   Create a Canvas in your scene (GameObject -> UI -> Canvas).
*               -   Inside the Canvas, create a Text element (e.g., GameObject -> UI -> Text - TextMeshPro).
*                   (If using TextMeshPro, Unity will prompt you to import its essential resources; do so.)
*               -   Drag this Text element from your Hierarchy into the `_Log Text` field of your `EventConsumer`
*                   in the Inspector. This will allow the consumer to display event logs directly in the game view.
*
*   5.  **Run the Scene:**
*       -   Press the Play button in the Unity Editor.
*       -   Observe the Unity Console and (if configured) your UI Text element.
*       -   You will see messages indicating when events are published by producers,
*           queued by the `EventQueueSystem`, processed by the `EventManager`,
*           and finally handled by the subscribed consumers.
*
*   ==========================================================================================
*   BENEFITS OF THE EVENT QUEUE SYSTEM:
*   ==========================================================================================
*   -   **Decoupling:** Components (producers and consumers) do not need direct references to each other.
*       This reduces dependencies, making individual components easier to develop, test, and reuse.
*   -   **Asynchronous Processing:** Events are queued and processed on a schedule (e.g., once per frame, or on a timer).
*       This prevents immediate, cascading function calls, which can make debugging difficult and can sometimes
*       lead to unexpected order-of-operation issues or even stack overflows in complex systems.
*   -   **Maintainability & Scalability:** Easily add new event types or new listeners without modifying
*       existing producer or consumer code. Want a new achievement for collecting 100 coins? Just add
*       an `AchievementSystem` consumer that listens to `ItemCollectedEvent` without touching the `Player` script.
*   -   **Flexibility:** An event can have multiple listeners, and a listener can subscribe to multiple event types.
*   -   **Testability:** Individual components can be tested in isolation. You can simulate events
*       being published to a consumer, or verify that a producer publishes the correct events,
*       without needing the entire game system running.
*
*   ==========================================================================================
*   CONSIDERATIONS AND POTENTIAL DRAWBACKS:
*   ==========================================================================================
*   -   **Debugging Complexity:** The indirect nature of event communication can make it harder to trace
*       the flow of execution compared to direct method calls. You might need good logging or specialized
*       debugging tools to follow an event from its origin to all its reactions.
*   -   **Overhead:** There's a slight performance overhead associated with queuing events, dictionary lookups,
*       and delegate invocations compared to direct method calls. For extremely high-frequency, performance-critical
*       interactions, a more direct approach might be considered, but for most game events, this overhead is negligible.
*   -   **Order of Operations:** While events are processed in the order they are published, the exact game tick/frame
*       they are handled depends on when `ProcessQueue` is called. This "eventual consistency" might not be suitable
*       for scenarios requiring immediate, synchronous feedback or strict ordering dependencies across different systems.
*   -   **Event Storms:** If too many events are published in a very short period (e.g., every physics tick for many objects),
*       the queue can grow very large, leading to performance issues when `ProcessQueue` attempts to dispatch them all.
*       Careful design of event frequency and data can mitigate this.
*   -   **Thread Safety:** The provided system includes `lock` statements for basic thread-safety around queue and listener collection modifications.
*       However, Unity's main thread handles most game logic. If you were to process events on background threads,
*       you'd need to ensure all subsequent game state modifications (e.g., changing a GameObject's position) are properly marshaled
*       back to the main thread, which adds significant complexity. This example assumes main-thread processing.
*/
```