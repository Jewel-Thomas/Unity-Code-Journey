// Unity Design Pattern Example: AnalyticsEventSystem
// This script demonstrates the AnalyticsEventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Analytics Event System** design pattern, which is a specialized form of the Observer/Publish-Subscribe pattern. It provides a robust, flexible, and decoupled way to send analytics data from various parts of your game to a central logging system, without each game component needing to know about the specifics of your analytics provider (e.g., Google Analytics, Mixpanel, custom backend).

---

### **AnalyticsEventSystemExample.cs**

To use this, create a new C# script named `AnalyticsEventSystemExample` in your Unity project, copy the entire content below into it, and then follow the **Setup Instructions** provided in the comments.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

// =====================================================================================
// SECTION 1: CORE ANALYTICS EVENT SYSTEM COMPONENTS
// This section defines the fundamental building blocks of our analytics event system.
// =====================================================================================

/// <summary>
/// Defines the various types of analytics events that can be published in the game.
/// Using an enum makes event types clear, type-safe, and easy to manage.
/// Add new event types here as your game evolves.
/// </summary>
public enum AnalyticsEventType
{
    GameStarted,
    LevelStarted,
    LevelCompleted,
    ItemCollected,
    PlayerDied,
    ButtonClicked,
    AchievementUnlocked,
    // Add more specific event types as needed, e.g.,
    // CharacterSpawned, EnemyKilled, ShopOpened, PurchaseMade, AdWatched
}

/// <summary>
/// A struct to encapsulate all data related to a single analytics event.
/// This allows for a standardized way to pass event information, including custom parameters.
/// Using a struct makes it a value type, which can be slightly more performant for small,
/// frequently passed data compared to a class, but a class is also perfectly fine.
/// </summary>
public struct AnalyticsEventData
{
    /// <summary>
    /// The specific type of analytics event (e.g., LevelCompleted, ItemCollected).
    /// </summary>
    public AnalyticsEventType EventType;

    /// <summary>
    /// The UTC timestamp when the event occurred. Useful for chronological analysis.
    /// </summary>
    public DateTime Timestamp;

    /// <summary>
    /// A flexible dictionary to store any custom parameters relevant to this specific event.
    /// Keys are strings (parameter names), values are objects (parameter values).
    /// This allows different event types to have different sets of parameters without
    /// needing separate data structures for each.
    /// </summary>
    public Dictionary<string, object> Parameters;

    /// <summary>
    /// Constructor for AnalyticsEventData.
    /// </summary>
    /// <param name="type">The type of the analytics event.</param>
    /// <param name="parameters">Optional dictionary of custom parameters for the event.</param>
    public AnalyticsEventData(AnalyticsEventType type, Dictionary<string, object> parameters = null)
    {
        EventType = type;
        Timestamp = DateTime.UtcNow; // Record the current UTC time
        Parameters = parameters ?? new Dictionary<string, object>(); // Initialize with empty dict if null
    }
}

/// <summary>
/// The central hub for the Analytics Event System.
/// This is a Singleton MonoBehaviour, ensuring there's only one instance throughout the game.
/// It manages subscriptions and publications of all analytics events.
///
/// Pattern: Singleton (for easy global access) + Publish-Subscribe (for event management)
/// </summary>
public class AnalyticsEventSystem : MonoBehaviour
{
    // Static instance of the AnalyticsEventSystem, accessible globally.
    public static AnalyticsEventSystem Instance { get; private set; }

    // A dictionary where keys are AnalyticsEventType and values are Actions.
    // Each Action<AnalyticsEventData> represents a "channel" for a specific event type.
    // Listeners subscribe to these Actions, and when an event is published, the corresponding
    // Action's delegates are invoked.
    private Dictionary<AnalyticsEventType, Action<AnalyticsEventData>> eventDictionary;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton pattern: ensures only one instance exists.
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            eventDictionary = new Dictionary<AnalyticsEventType, Action<AnalyticsEventData>>();
            // Optional: Prevent this GameObject from being destroyed when loading new scenes.
            // This is typical for managers that need to persist throughout the game lifecycle.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another instance already exists, destroy this one to enforce the Singleton.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the GameObject is being destroyed.
    /// Cleans up the event dictionary to prevent potential memory leaks, especially
    /// if DontDestroyOnLoad was used and the application is quitting.
    /// </summary>
    void OnDestroy()
    {
        if (eventDictionary != null)
        {
            // Clear all subscriptions. This helps prevent 'stale' subscriptions
            // if the system is ever re-initialized or the application quits.
            eventDictionary.Clear();
        }
        // Ensure the static Instance reference is cleared if this is the instance being destroyed.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Subscribes a listener method to a specific analytics event type.
    /// When an event of the specified type is published, the listener method will be invoked.
    /// </summary>
    /// <param name="type">The type of analytics event to listen for.</param>
    /// <param name="listener">The action (method) to be invoked when the event is published.</param>
    public void Subscribe(AnalyticsEventType type, Action<AnalyticsEventData> listener)
    {
        if (eventDictionary.ContainsKey(type))
        {
            // If the event type already has an associated action, add the new listener to it.
            eventDictionary[type] += listener;
        }
        else
        {
            // If it's a new event type, create a new action and add the listener.
            eventDictionary.Add(type, listener);
        }
        // Debug.Log($"[AnalyticsSystem] Subscribed listener to {type}"); // Uncomment for detailed debug
    }

    /// <summary>
    /// Unsubscribes a listener method from a specific analytics event type.
    /// It is CRUCIAL to unsubscribe when a listener object is destroyed or disabled (e.g., in OnDisable or OnDestroy)
    /// to prevent memory leaks (where a destroyed object is still referenced by the event system)
    /// and potential NullReferenceExceptions if the event tries to invoke a method on a destroyed object.
    /// </summary>
    /// <param name="type">The type of analytics event to unsubscribe from.</param>
    /// <param name="listener">The action (method) to be removed from the event's invocation list.</param>
    public void Unsubscribe(AnalyticsEventType type, Action<AnalyticsEventData> listener)
    {
        // Check if the instance is still valid. It might have been destroyed if the game is quitting.
        if (Instance == null || eventDictionary == null) return;

        if (eventDictionary.ContainsKey(type))
        {
            // Remove the listener from the action.
            eventDictionary[type] -= listener;
        }
        // Debug.Log($"[AnalyticsSystem] Unsubscribed listener from {type}"); // Uncomment for detailed debug
    }

    /// <summary>
    /// Publishes an analytics event, notifying all subscribed listeners for that event type.
    /// Any part of your game can call this method to signal that an event has occurred.
    /// </summary>
    /// <param name="type">The type of analytics event being published.</param>
    /// <param name="data">The data associated with the event.</param>
    public void Publish(AnalyticsEventType type, AnalyticsEventData data)
    {
        // Check if the instance is valid and if there are any listeners for this event type.
        if (Instance == null || eventDictionary == null) return;

        // Try to get the action associated with the event type.
        if (eventDictionary.TryGetValue(type, out Action<AnalyticsEventData> thisEvent))
        {
            // If there are listeners, invoke them, passing the event data.
            thisEvent?.Invoke(data);
            // Debug.Log($"[AnalyticsSystem] Published event: {type}"); // Uncomment for detailed debug
        }
        else
        {
            // This is often just informational. Not every event needs a listener.
            // Debug.LogWarning($"[AnalyticsSystem] Attempted to publish event {type} but no listeners are subscribed.");
        }
    }
}


// =====================================================================================
// SECTION 2: EXAMPLE USAGE - PUBLISHERS
// These scripts demonstrate how different game components publish analytics events.
// They don't know who is listening or what happens to the data.
// =====================================================================================

/// <summary>
/// Example: GameManager - A script responsible for general game flow, level management, etc.
/// It publishes analytics events when key game state changes occur.
/// </summary>
public class GameManager : MonoBehaviour
{
    private int currentLevel = 0;
    private int itemsCollected = 0;
    private bool gameStarted = false;

    // --- Unity Lifecycle Methods ---
    void Start()
    {
        // Ensure the AnalyticsEventSystem exists in the scene.
        // If it's not manually added, this will create it.
        // This is a robust way to ensure the Singleton is present.
        if (AnalyticsEventSystem.Instance == null)
        {
            GameObject eventSystemGO = new GameObject("AnalyticsEventSystem_Runtime");
            eventSystemGO.AddComponent<AnalyticsEventSystem>();
            Debug.Log("<color=orange>AnalyticsEventSystem created at runtime!</color>");
        }

        // Simulate game start
        StartGame();
    }

    void Update()
    {
        // --- Simulate Player Actions for demonstration ---
        if (Input.GetKeyDown(KeyCode.N))
        {
            AdvanceLevel();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            CollectItem();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            PlayerDies();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            ClickButton("ShopButton");
        }
    }

    // --- Game Logic Methods that Publish Analytics Events ---

    public void StartGame()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            Debug.Log("<color=cyan>[GameManager]</color> Game Started!");

            // Publish GameStarted event with relevant parameters
            AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.GameStarted,
                new AnalyticsEventData(AnalyticsEventType.GameStarted, new Dictionary<string, object> {
                    { "GameVersion", Application.version },
                    { "Platform", Application.platform.ToString() },
                    { "PlayerID", "player_12345" } // Example: Unique player identifier
                })
            );
            AdvanceLevel(); // Automatically start the first level after game start
        }
    }

    public void AdvanceLevel()
    {
        // If it's not the first level, publish LevelCompleted for the previous one
        if (currentLevel > 0)
        {
            Debug.Log($"<color=cyan>[GameManager]</color> Level {currentLevel} Completed!");
            AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.LevelCompleted,
                new AnalyticsEventData(AnalyticsEventType.LevelCompleted, new Dictionary<string, object> {
                    { "LevelNumber", currentLevel },
                    { "Score", UnityEngine.Random.Range(100, 1000) },
                    { "StarsEarned", UnityEngine.Random.Range(1, 4) },
                    { "TimeTakenSeconds", UnityEngine.Random.Range(30f, 180f) }
                })
            );
        }

        currentLevel++;
        Debug.Log($"<color=cyan>[GameManager]</color> Level {currentLevel} Started!");

        // Publish LevelStarted event with relevant parameters
        AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.LevelStarted,
            new AnalyticsEventData(AnalyticsEventType.LevelStarted, new Dictionary<string, object> {
                { "LevelNumber", currentLevel },
                { "Difficulty", "Normal" }
            })
        );
    }

    public void CollectItem()
    {
        itemsCollected++;
        string itemName = "Coin"; // Example item
        Debug.Log($"<color=green>[GameManager]</color> Item '{itemName}' collected! Total: {itemsCollected}</color>");

        // Publish ItemCollected event
        AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.ItemCollected,
            new AnalyticsEventData(AnalyticsEventType.ItemCollected, new Dictionary<string, object> {
                { "ItemName", itemName },
                { "Quantity", 1 },
                { "TotalItemsCollected", itemsCollected },
                { "CurrentLevel", currentLevel },
                { "ItemType", "Currency" }
            })
        );
    }

    public void PlayerDies()
    {
        Debug.Log("<color=red>[GameManager]</color> Player Died!");

        // Publish PlayerDied event
        AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.PlayerDied,
            new AnalyticsEventData(AnalyticsEventType.PlayerDied, new Dictionary<string, object> {
                { "CurrentLevel", currentLevel },
                { "TimeSurvivedInLevelSeconds", UnityEngine.Random.Range(10f, 60f) },
                { "CauseOfDeath", "SpikeTrap" } // Example cause
            })
        );
    }

    public void ClickButton(string buttonName)
    {
        Debug.Log($"<color=magenta>[GameManager]</color> Button '{buttonName}' clicked!");

        // Publish ButtonClicked event
        AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.ButtonClicked,
            new AnalyticsEventData(AnalyticsEventType.ButtonClicked, new Dictionary<string, object> {
                { "ButtonName", buttonName },
                { "ScreenName", "MainMenu" } // Example screen where button was clicked
            })
        );
    }
}


// =====================================================================================
// SECTION 3: EXAMPLE USAGE - LISTENERS
// These scripts demonstrate how different game components subscribe to and react to
// analytics events. They don't know who published the event.
// =====================================================================================

/// <summary>
/// Example: AnalyticsLogger - A central component that subscribes to ALL analytics events.
/// In a real project, this script would handle sending data to a third-party analytics service
/// (e.g., Google Analytics, Firebase, Mixpanel) or a custom backend.
/// For this example, it simply logs the events to the Unity console.
/// </summary>
public class AnalyticsLogger : MonoBehaviour
{
    /// <summary>
    /// Called when the object becomes enabled and active.
    /// This is where we subscribe to the events. It's a good practice to
    /// subscribe in OnEnable and unsubscribe in OnDisable to manage event listeners
    /// based on GameObject active state.
    /// </summary>
    void OnEnable()
    {
        // Check if the AnalyticsEventSystem instance is available.
        if (AnalyticsEventSystem.Instance != null)
        {
            // Subscribe to ALL event types. This is common for a central logger.
            // We iterate through all defined AnalyticsEventType enum values and subscribe
            // the same handler method (`OnAnalyticsEvent`) to each.
            foreach (AnalyticsEventType type in Enum.GetValues(typeof(AnalyticsEventType)))
            {
                AnalyticsEventSystem.Instance.Subscribe(type, OnAnalyticsEvent);
            }
            Debug.Log("<color=lime>[AnalyticsLogger]</color> Subscribed to all analytics events.");
        }
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// It is CRUCIAL to unsubscribe here to prevent memory leaks and null reference exceptions.
    /// If the GameObject is destroyed but still subscribed, the event system would try
    /// to call a method on a non-existent object.
    /// </summary>
    void OnDisable()
    {
        // Check if the AnalyticsEventSystem instance is still available before unsubscribing.
        // It might be null if the system itself was destroyed before this listener.
        if (AnalyticsEventSystem.Instance != null)
        {
            foreach (AnalyticsEventType type in Enum.GetValues(typeof(AnalyticsEventType)))
            {
                AnalyticsEventSystem.Instance.Unsubscribe(type, OnAnalyticsEvent);
            }
            Debug.Log("<color=red>[AnalyticsLogger]</color> Unsubscribed from all analytics events.");
        }
    }

    /// <summary>
    /// The handler method invoked when any analytics event is published.
    /// </summary>
    /// <param name="data">The AnalyticsEventData containing event type and parameters.</param>
    private void OnAnalyticsEvent(AnalyticsEventData data)
    {
        // --- REAL-WORLD SCENARIO ---
        // In a real project, this is where you'd integrate with your analytics SDK:
        // switch (data.EventType) {
        //     case AnalyticsEventType.GameStarted:
        //         FirebaseAnalytics.LogEvent("game_start", data.Parameters);
        //         break;
        //     case AnalyticsEventType.LevelCompleted:
        //         // Convert data.Parameters to a format suitable for your SDK
        //         FirebaseAnalytics.LogEvent("level_completed", data.Parameters);
        //         break;
        //     // ... handle other event types
        // }
        // OR, send all events to a generic logger:
        // AnalyticsSDK.LogEvent(data.EventType.ToString(), data.Parameters);


        // --- DEMONSTRATION: Log to Unity Console ---
        string paramString = "";
        foreach (var param in data.Parameters)
        {
            paramString += $"{param.Key}: {param.Value}, ";
        }
        if (paramString.Length > 0)
        {
            paramString = paramString.TrimEnd(',', ' '); // Remove trailing comma and space
        }

        Debug.Log($"<color=yellow>[Analytics Log - {data.EventType}]</color> Timestamp: {data.Timestamp:yyyy-MM-dd HH:mm:ss} | Params: [{paramString}]");
    }
}

/// <summary>
/// Example: AchievementTracker - A specific game system that listens for certain events
/// to check for achievement unlocks.
/// It only subscribes to events relevant to its functionality.
/// </summary>
public class AchievementTracker : MonoBehaviour
{
    private bool firstLevelCompletedAchievement = false;
    private bool tenItemsCollectedAchievement = false;

    /// <summary>
    /// Subscribes to specific events when enabled.
    /// </summary>
    void OnEnable()
    {
        if (AnalyticsEventSystem.Instance != null)
        {
            // Subscribe only to events relevant for achievement tracking.
            AnalyticsEventSystem.Instance.Subscribe(AnalyticsEventType.LevelCompleted, OnLevelCompleted);
            AnalyticsEventSystem.Instance.Subscribe(AnalyticsEventType.ItemCollected, OnItemCollected);
            Debug.Log("<color=lime>[AchievementTracker]</color> Subscribed to LevelCompleted and ItemCollected events.");
        }
    }

    /// <summary>
    /// Unsubscribes from events when disabled.
    /// </summary>
    void OnDisable()
    {
        if (AnalyticsEventSystem.Instance != null)
        {
            AnalyticsEventSystem.Instance.Unsubscribe(AnalyticsEventType.LevelCompleted, OnLevelCompleted);
            AnalyticsEventSystem.Instance.Unsubscribe(AnalyticsEventType.ItemCollected, OnItemCollected);
            Debug.Log("<color=red>[AchievementTracker]</color> Unsubscribed from LevelCompleted and ItemCollected events.");
        }
    }

    /// <summary>
    /// Event handler for LevelCompleted events. Checks for 'First Level Conqueror' achievement.
    /// </summary>
    /// <param name="data">The event data.</param>
    private void OnLevelCompleted(AnalyticsEventData data)
    {
        if (!firstLevelCompletedAchievement &&
            data.Parameters.TryGetValue("LevelNumber", out object levelObj) &&
            (int)levelObj == 1)
        {
            firstLevelCompletedAchievement = true;
            Debug.Log("<color=purple>[AchievementTracker]</color> ACHIEVEMENT UNLOCKED: 'First Level Conqueror'!");

            // Also publish an analytics event for the achievement unlock itself
            AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.AchievementUnlocked,
                new AnalyticsEventData(AnalyticsEventType.AchievementUnlocked, new Dictionary<string, object> {
                    { "AchievementName", "First Level Conqueror" },
                    { "LevelNumber", 1 }
                })
            );
        }
    }

    /// <summary>
    /// Event handler for ItemCollected events. Checks for 'Hoarder' achievement.
    /// </summary>
    /// <param name="data">The event data.</param>
    private void OnItemCollected(AnalyticsEventData data)
    {
        if (!tenItemsCollectedAchievement &&
            data.Parameters.TryGetValue("TotalItemsCollected", out object totalItemsObj) &&
            (int)totalItemsObj >= 10)
        {
            tenItemsCollectedAchievement = true;
            Debug.Log("<color=purple>[AchievementTracker]</color> ACHIEVEMENT UNLOCKED: 'Hoarder'!");

            // Publish an analytics event for the achievement unlock
            AnalyticsEventSystem.Instance.Publish(AnalyticsEventType.AchievementUnlocked,
                new AnalyticsEventData(AnalyticsEventType.AchievementUnlocked, new Dictionary<string, object> {
                    { "AchievementName", "Hoarder" },
                    { "ItemsCollected", (int)totalItemsObj }
                })
            );
        }
    }
}
```

---

### **Setup Instructions in Unity:**

1.  **Create Script:** Create a new C# script in your Unity project named `AnalyticsEventSystemExample.cs`.
2.  **Copy Code:** Copy the entire C# code block above and paste it into this new script, replacing its default content.
3.  **Create GameObjects:**
    *   In your current scene (or a new empty one), create an empty GameObject and name it `GameManager`.
    *   Create another empty GameObject and name it `AnalyticsListeners`.
4.  **Add Components:**
    *   Drag the `GameManager` component (from your script file) onto the `GameManager` GameObject.
    *   Drag the `AnalyticsLogger` component (from your script file) onto the `AnalyticsListeners` GameObject.
    *   Drag the `AchievementTracker` component (from your script file) onto the `AnalyticsListeners` GameObject.
    *   *(Optional: You can also manually add the `AnalyticsEventSystem` component to an empty GameObject named "AnalyticsEventSystem", but the `GameManager` will automatically create it at runtime if it doesn't find one, making setup easier.)*
5.  **Run Scene:** Play your Unity scene.
6.  **Observe & Interact:**
    *   Watch the Unity Console for detailed logs from the `GameManager` (publishing), `AnalyticsLogger` (logging all events), and `AchievementTracker` (unlocking achievements).
    *   Press the following keys to simulate game events:
        *   `N`: Advance to the next level (this will also complete the previous one).
        *   `I`: Collect an item. (Collect 10 to unlock "Hoarder".)
        *   `D`: Player dies.
        *   `B`: Click a button.

---

### **Explanation of the Analytics Event System Design Pattern:**

This example implements a common variant of the **Publish-Subscribe (or Observer)** design pattern, specifically tailored for analytics events.

1.  **`AnalyticsEventType` Enum:**
    *   **Purpose:** Provides a clear, type-safe, and exhaustive list of all possible analytics events your game might want to track.
    *   **Benefit:** Prevents typos when referring to event names and allows for easy expansion.

2.  **`AnalyticsEventData` Struct:**
    *   **Purpose:** A standardized container for all information related to a single event. It includes the `EventType`, a `Timestamp`, and a flexible `Dictionary<string, object>` for custom parameters.
    *   **Benefit:**
        *   **Flexibility:** The `Parameters` dictionary allows different events to have completely different data without requiring a unique class for each event type.
        *   **Consistency:** All event data is wrapped in the same structure, making it easy for listeners to process.

3.  **`AnalyticsEventSystem` (Singleton MonoBehaviour):**
    *   **Purpose:** This is the central "broker" or "dispatcher" of all analytics events. It's implemented as a **Singleton** (`Instance` property) to ensure there's only one globally accessible instance in your game, making it easy for any part of your code to access it.
    *   **Key Components:**
        *   `eventDictionary`: A `Dictionary` where the keys are `AnalyticsEventType` (e.g., `LevelCompleted`) and the values are `Action<AnalyticsEventData>` delegates. Each delegate acts as a "channel" for a specific event type.
        *   `Awake()`: Implements the Singleton pattern, ensuring only one instance and preventing destruction on scene loads (`DontDestroyOnLoad`).
        *   `OnDestroy()`: Crucially cleans up the event dictionary to prevent memory leaks if the application quits or the instance is manually destroyed.
        *   `Subscribe(type, listener)`: Allows any component to register a method (`listener`) to be called when an event of a specific `type` is published.
        *   `Unsubscribe(type, listener)`: Removes a registered listener. This is **critically important** to prevent `NullReferenceException` errors when subscribed GameObjects are destroyed, and to avoid memory leaks. It should typically be called in `OnDisable()` or `OnDestroy()` of the subscribing component.
        *   `Publish(type, data)`: The core method to trigger an event. When called, it looks up the `type` in the `eventDictionary` and invokes all registered `listener` methods for that event type, passing the `AnalyticsEventData`.
    *   **Benefit:**
        *   **Decoupling:** Publishers (e.g., `GameManager`) don't need to know anything about who is listening or what happens to the data. Listeners (e.g., `AnalyticsLogger`, `AchievementTracker`) don't need to know who published the event. This reduces dependencies and makes your code more modular and easier to maintain.
        *   **Centralization:** All analytics event management is handled in one place.
        *   **Flexibility:** You can easily add new event types or new listeners without modifying existing code that publishes or other existing listeners.

4.  **Publishers (`GameManager`):**
    *   **Purpose:** These are the components in your game that detect when an event worthy of analytics tracking occurs.
    *   **Implementation:** They simply call `AnalyticsEventSystem.Instance.Publish()` with the appropriate `AnalyticsEventType` and `AnalyticsEventData`.
    *   **Benefit:** Game logic remains clean and focused on gameplay, without being cluttered by analytics integration details.

5.  **Listeners (`AnalyticsLogger`, `AchievementTracker`):**
    *   **Purpose:** These are the components that are interested in specific analytics events and react to them.
    *   **Implementation:**
        *   In their `OnEnable()` methods, they call `AnalyticsEventSystem.Instance.Subscribe()` for the events they care about, passing a method to handle that event.
        *   In their `OnDisable()` methods, they call `AnalyticsEventSystem.Instance.Unsubscribe()` to prevent issues when the GameObject is inactive or destroyed.
        *   Their event handler methods (e.g., `OnAnalyticsEvent`, `OnLevelCompleted`) receive the `AnalyticsEventData` and perform their specific logic (e.g., send to analytics SDK, update achievements, display UI).
    *   **Benefit:**
        *   **Modularity:** Different listeners can handle events in different ways (logging, achievements, leaderboards, marketing campaigns) without interfering with each other.
        *   **Scalability:** You can add new analytics features or change your analytics provider by simply creating or modifying a listener script, leaving game logic untouched.

This pattern makes your analytics integration robust, maintainable, and highly flexible for any Unity project.