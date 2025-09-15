// Unity Design Pattern Example: GameEventRecorder
// This script demonstrates the GameEventRecorder pattern in Unity
// Generated automatically - ready to use in your Unity project

The GameEventRecorder design pattern provides a centralized, decoupled way to record significant events that occur during gameplay. This is incredibly useful for debugging, analytics, replay systems, and auditing game state changes without tightly coupling various game systems.

Here's a complete, practical C# Unity example demonstrating the GameEventRecorder pattern.

---

### Understanding the GameEventRecorder Pattern

**Purpose:**
To log or "record" various in-game events (e.g., player takes damage, item collected, level loaded, enemy spawned) in a structured manner.

**Benefits:**
1.  **Debugging:** Review a sequence of events to understand how a bug occurred.
2.  **Analytics:** Collect data on player behavior, game balance, or specific interactions.
3.  **Replay Systems:** Store events to reconstruct and replay a game session.
4.  **Decoupling:** Game systems don't need to know *who* is listening to events, only that they should be recorded. The recorder is a single point of entry for logging.
5.  **Auditing/History:** Maintain a history of significant game state changes.

**Core Components:**
1.  **`GameEvent` (Data Structure):** A struct or class that defines what constitutes an event (timestamp, type, description, relevant data).
2.  **`GameEventRecorder` (Manager):** A central object (often a `ScriptableObject` or a Singleton `MonoBehaviour`) responsible for:
    *   Receiving `GameEvent` objects from various parts of the game.
    *   Storing these events in a list or other collection.
    *   Providing methods to query, filter, or retrieve the recorded events.
    *   Potentially handling persistence (saving/loading events).

---

### Unity Implementation

We'll create three scripts:
1.  `GameEvent.cs`: The data structure for an individual event.
2.  `GameEventRecorder.cs`: The central ScriptableObject recorder.
3.  `PlayerHealth.cs`: An example MonoBehaviour that records health-related events.
4.  `GameEventDebugDisplay.cs`: An example MonoBehaviour that reads and displays recorded events.

---

### 1. `GameEvent.cs`

This struct defines the information associated with each recorded event.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single event that occurred in the game.
/// This struct holds all the relevant data for an event, making it easy to log and analyze.
/// </summary>
[Serializable] // Mark as serializable for potential saving/loading if needed
public struct GameEvent
{
    // --- Core Event Data ---
    public DateTime Timestamp { get; private set; }
    public string EventType { get; private set; }
    public string Description { get; private set; }

    // --- Optional Context Data ---
    // A reference to the GameObject or Component involved in the event.
    // Useful for linking events back to specific entities in the scene.
    public string ContextObjectName { get; private set; }
    public int ContextObjectID { get; private set; } // For unique identification even if object name changes

    // --- Custom Data (Flexible Key-Value Pairs) ---
    // A dictionary for any additional, specific data an event might need.
    // E.g., damage amount, item ID, player score, position data.
    public Dictionary<string, string> CustomData { get; private set; }

    /// <summary>
    /// Constructor for a GameEvent.
    /// </summary>
    /// <param name="eventType">A categorization of the event (e.g., "PlayerDamage", "ItemCollected").</param>
    /// <param name="description">A human-readable description of what happened.</param>
    /// <param name="contextObject">An optional GameObject or Component related to the event.</param>
    /// <param name="customData">Optional dictionary for additional key-value data specific to the event.</param>
    public GameEvent(string eventType, string description, Object contextObject = null, Dictionary<string, string> customData = null)
    {
        Timestamp = DateTime.Now; // Record the exact time the event occurred
        EventType = eventType;
        Description = description;

        // Populate context object data if provided
        if (contextObject != null)
        {
            ContextObjectName = contextObject.name;
            // Use GetInstanceID for a unique identifier for runtime objects
            ContextObjectID = contextObject.GetInstanceID(); 
        }
        else
        {
            ContextObjectName = "N/A";
            ContextObjectID = 0;
        }

        // Initialize custom data, ensuring it's never null
        CustomData = customData ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Provides a user-friendly string representation of the event.
    /// </summary>
    public override string ToString()
    {
        string customDataString = CustomData.Count > 0 
            ? " (" + string.Join(", ", CustomData.Select(kv => $"{kv.Key}: {kv.Value}")) + ")" 
            : "";

        return $"{Timestamp:HH:mm:ss.fff} [{EventType}] {Description} [Obj: {ContextObjectName}, ID: {ContextObjectID}]{customDataString}";
    }
}
```

---

### 2. `GameEventRecorder.cs`

This `ScriptableObject` acts as the central recorder. Using a `ScriptableObject` is great for persistent manager-like objects in Unity, as it can be created once in the project and referenced from anywhere without needing to be in a scene.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Select and .Where extensions

/// <summary>
/// The central GameEventRecorder. This ScriptableObject acts as a persistent,
/// global manager for recording game events.
///
/// It implements the GameEventRecorder design pattern, providing a decoupled
/// way for various game systems to log events without needing to know
/// about each other or how the events are consumed.
/// </summary>
[CreateAssetMenu(fileName = "GameEventRecorder", menuName = "Game/Game Event Recorder")]
public class GameEventRecorder : ScriptableObject
{
    // --- Singleton Instance ---
    // Provides a static, globally accessible instance of the GameEventRecorder.
    // This allows any script to easily record or query events.
    // It assumes the ScriptableObject asset will be created in a Resources folder.
    private static GameEventRecorder _instance;
    public static GameEventRecorder Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to load the asset from any Resources folder
                _instance = Resources.Load<GameEventRecorder>("GameEventRecorder");

                if (_instance == null)
                {
                    // If not found, log an error and suggest creating it.
                    Debug.LogError("GameEventRecorder ScriptableObject not found in Resources folder. " +
                                   "Please create one via Assets -> Create -> Game -> Game Event Recorder " +
                                   "and name it 'GameEventRecorder' then place it in a 'Resources' folder.");
                }
                else
                {
                    // Initialize the list when loaded if it's null (e.g., after a script reload)
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }

    // --- Recorded Events Storage ---
    [Tooltip("The list of all recorded game events.")]
    private List<GameEvent> _recordedEvents = new List<GameEvent>();

    [Tooltip("The maximum number of events to store. Oldest events are removed when this limit is exceeded.")]
    [SerializeField] private int _maxEventsToStore = 100;

    /// <summary>
    /// Initializes the event list. Called when the ScriptableObject is loaded or created.
    /// </summary>
    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Ensure the list is always initialized to prevent NullReferenceExceptions
        if (_recordedEvents == null)
        {
            _recordedEvents = new List<GameEvent>();
        }
    }

    /// <summary>
    /// Records a new game event. This is the primary method for logging events.
    /// </summary>
    /// <param name="eventType">The type or category of the event.</param>
    /// <param name="description">A descriptive message for the event.</param>
    /// <param name="contextObject">An optional Unity Object related to the event (e.g., GameObject, MonoBehaviour).</param>
    /// <param name="customData">Optional dictionary of additional key-value data.</param>
    public void RecordEvent(string eventType, string description, Object contextObject = null, Dictionary<string, string> customData = null)
    {
        GameEvent newEvent = new GameEvent(eventType, description, contextObject, customData);
        _recordedEvents.Add(newEvent);

        // Keep the list size within the specified limit
        if (_recordedEvents.Count > _maxEventsToStore)
        {
            _recordedEvents.RemoveAt(0); // Remove the oldest event
        }

        // Optional: Debug log the event for immediate feedback in the console
        // Debug.Log($"Event Recorded: {newEvent.ToString()}");
    }

    /// <summary>
    /// Retrieves all currently recorded game events.
    /// Returns a read-only copy to prevent external modification of the internal list.
    /// </summary>
    public IReadOnlyList<GameEvent> GetAllEvents()
    {
        return _recordedEvents.AsReadOnly();
    }

    /// <summary>
    /// Retrieves all recorded game events of a specific type.
    /// </summary>
    /// <param name="eventType">The type of events to filter by.</param>
    public IReadOnlyList<GameEvent> GetEventsByType(string eventType)
    {
        return _recordedEvents.Where(e => e.EventType == eventType).ToList().AsReadOnly();
    }

    /// <summary>
    /// Clears all recorded events from the recorder.
    /// </summary>
    public void ClearEvents()
    {
        _recordedEvents.Clear();
        Debug.Log("Game Event Recorder: All events cleared.");
    }

    /// <summary>
    /// Sets the maximum number of events to store.
    /// </summary>
    /// <param name="maxCount">The new maximum event count.</param>
    public void SetMaxEventsToStore(int maxCount)
    {
        if (maxCount < 0)
        {
            Debug.LogWarning("Max events to store cannot be negative. Setting to 0.");
            _maxEventsToStore = 0;
        }
        else
        {
            _maxEventsToStore = maxCount;
        }

        // If reducing the limit, trim the current list
        while (_recordedEvents.Count > _maxEventsToStore)
        {
            _recordedEvents.RemoveAt(0);
        }
    }
}
```

---

### 3. `PlayerHealth.cs`

An example `MonoBehaviour` that demonstrates how a game system uses the `GameEventRecorder` to log its own events.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

/// <summary>
/// Example MonoBehaviour demonstrating how a game system uses the GameEventRecorder.
/// This PlayerHealth component records events whenever health changes.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    void Awake()
    {
        _currentHealth = _maxHealth;
        // Record initial state
        GameEventRecorder.Instance.RecordEvent(
            "PlayerHealth", 
            "Player spawned with full health.", 
            this, // 'this' refers to this MonoBehaviour as the context object
            new Dictionary<string, string> { { "Health", _currentHealth.ToString() } }
        );
    }

    /// <summary>
    /// Applies damage to the player and records a "PlayerDamage" event.
    /// </summary>
    /// <param name="amount">The amount of damage taken.</param>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        _currentHealth -= amount;
        if (_currentHealth < 0) _currentHealth = 0;

        Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {_currentHealth}");

        // --- GameEventRecorder Usage ---
        // Record the event using the static Instance of the GameEventRecorder
        GameEventRecorder.Instance.RecordEvent(
            "PlayerDamage",                                     // Event Type
            $"{gameObject.name} took {amount} damage.",         // Description
            this,                                               // Context Object (this PlayerHealth component)
            new Dictionary<string, string>                      // Custom Data
            {
                { "DamageAmount", amount.ToString() },
                { "CurrentHealth", _currentHealth.ToString() }
            }
        );

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the player and records a "PlayerHeal" event.
    /// </summary>
    /// <param name="amount">The amount of health restored.</param>
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        _currentHealth += amount;
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

        Debug.Log($"{gameObject.name} healed {amount}. Current Health: {_currentHealth}");

        // --- GameEventRecorder Usage ---
        GameEventRecorder.Instance.RecordEvent(
            "PlayerHeal",
            $"{gameObject.name} healed {amount} health.",
            this,
            new Dictionary<string, string>
            {
                { "HealAmount", amount.ToString() },
                { "CurrentHealth", _currentHealth.ToString() }
            }
        );
    }

    /// <summary>
    /// Handles player death and records a "PlayerDeath" event.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!");

        // --- GameEventRecorder Usage ---
        GameEventRecorder.Instance.RecordEvent(
            "PlayerDeath",
            $"{gameObject.name} died!",
            this,
            new Dictionary<string, string>
            {
                { "FinalHealth", _currentHealth.ToString() } // Should be 0
            }
        );

        // In a real game, you might disable the player, show a game over screen, etc.
        gameObject.SetActive(false); 
    }

    // --- For Demonstration: Simulate health changes ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) // 'D' for Damage
        {
            TakeDamage(10);
        }
        if (Input.GetKeyDown(KeyCode.H)) // 'H' for Heal
        {
            Heal(5);
        }
        if (Input.GetKeyDown(KeyCode.K)) // 'K' for Kill
        {
            TakeDamage(_currentHealth); // Instant death
        }
    }
}
```

---

### 4. `GameEventDebugDisplay.cs`

An example `MonoBehaviour` that demonstrates how another system (e.g., a debug UI, analytics sender, or replay system) can retrieve and use the recorded events.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro if you're using it
using System.Text;
using System.Collections.Generic; // Required for List

/// <summary>
/// Example MonoBehaviour that demonstrates how to retrieve and display
/// recorded events from the GameEventRecorder. This could be a debug UI,
/// an analytics sender, or a replay system.
/// </summary>
public class GameEventDebugDisplay : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component to display the events.")]
    [SerializeField] private TextMeshProUGUI _displayOutput;

    [Tooltip("How many of the latest events to display.")]
    [SerializeField] private int _eventsToDisplay = 10;

    [Tooltip("How often to refresh the event display (in seconds).")]
    [SerializeField] private float _refreshInterval = 1.0f;
    private float _timeSinceLastRefresh;

    private readonly StringBuilder _stringBuilder = new StringBuilder();

    void Awake()
    {
        if (_displayOutput == null)
        {
            Debug.LogError("GameEventDebugDisplay requires a TextMeshProUGUI component assigned to '_displayOutput'.");
            enabled = false; // Disable this script if no display is set
        }
    }

    void Start()
    {
        RefreshDisplay(); // Initial display refresh
    }

    void Update()
    {
        _timeSinceLastRefresh += Time.deltaTime;
        if (_timeSinceLastRefresh >= _refreshInterval)
        {
            RefreshDisplay();
            _timeSinceLastRefresh = 0f;
        }

        // Optional: Clear events with a key press for testing
        if (Input.GetKeyDown(KeyCode.C)) // 'C' for Clear
        {
            GameEventRecorder.Instance.ClearEvents();
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Retrieves the latest events from the GameEventRecorder and updates the display.
    /// </summary>
    public void RefreshDisplay()
    {
        if (_displayOutput == null) return;

        _stringBuilder.Clear();
        _stringBuilder.AppendLine("--- GAME EVENT LOG ---");
        _stringBuilder.AppendLine($"Showing latest {_eventsToDisplay} events.");
        _stringBuilder.AppendLine("-----------------------");

        IReadOnlyList<GameEvent> allEvents = GameEventRecorder.Instance.GetAllEvents();

        // Display the latest events
        int startIndex = Mathf.Max(0, allEvents.Count - _eventsToDisplay);
        for (int i = startIndex; i < allEvents.Count; i++)
        {
            _stringBuilder.AppendLine(allEvents[i].ToString());
        }

        _displayOutput.text = _stringBuilder.ToString();
    }
}
```

---

### How to Set Up and Use in Unity:

1.  **Create the Scripts:**
    *   Create a new C# script named `GameEvent.cs` and paste the `GameEvent` struct code.
    *   Create a new C# script named `GameEventRecorder.cs` and paste the `GameEventRecorder` class code.
    *   Create a new C# script named `PlayerHealth.cs` and paste the `PlayerHealth` class code.
    *   Create a new C# script named `GameEventDebugDisplay.cs` and paste the `GameEventDebugDisplay` class code.

2.  **Create the `GameEventRecorder` Asset:**
    *   In your Unity Project window, right-click (or go to `Assets -> Create`).
    *   Navigate to `Game -> Game Event Recorder`.
    *   **IMPORTANT:** Name this new asset `GameEventRecorder`.
    *   **IMPORTANT:** Drag this `GameEventRecorder.asset` file into a folder named `Resources` (if you don't have one, create it by right-clicking in the Project window -> `Create -> Folder` and name it `Resources`). The static `Instance` property relies on loading this asset from a `Resources` folder.

3.  **Set up the Player GameObject:**
    *   Create an empty GameObject in your scene (e.g., named "Player").
    *   Attach the `PlayerHealth.cs` script to it.

4.  **Set up the Debug UI (Optional but Recommended):**
    *   Create a UI Canvas (`GameObject -> UI -> Canvas`).
    *   Inside the Canvas, create a TextMeshPro Text (`GameObject -> UI -> Text - TextMeshPro`).
        *   If prompted, import the TMP Essentials.
        *   Adjust its size and position on the Canvas so it's visible.
        *   You might want to make it a scroll view if you expect many events.
    *   Create an empty GameObject in your scene (e.g., named "EventLogger").
    *   Attach the `GameEventDebugDisplay.cs` script to "EventLogger".
    *   Drag your TextMeshProUGUI component from the Canvas onto the `_displayOutput` field of the `GameEventDebugDisplay` script in the Inspector.

5.  **Run the Game:**
    *   Play the scene.
    *   Press `D` to damage the player, `H` to heal, `K` to kill.
    *   Observe the TextMeshPro UI updating with the recorded events.
    *   Press `C` to clear the event log.

This setup provides a complete, working example of the GameEventRecorder pattern, demonstrating how to record events from game systems and how to retrieve and display them for debugging or other purposes.