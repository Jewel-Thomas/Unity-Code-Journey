// Unity Design Pattern Example: WorldEventSystem
// This script demonstrates the WorldEventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'WorldEventSystem' design pattern, often referred to as a global event bus or message bus, is a powerful way to decouple different parts of your game logic. Instead of objects directly communicating with each other, they broadcast events to a central system, and other objects register to listen for specific events from that system.

This promotes:
*   **Decoupling**: Objects don't need direct references to each other. A UI element doesn't need to know about the Player class, only that an "ItemCollected" event occurred.
*   **Maintainability**: Changes to one system are less likely to break others.
*   **Scalability**: Easier to add new features or listeners without modifying existing code.
*   **Testability**: Individual components can be tested more easily in isolation.

Below is a complete Unity C# example demonstrating the WorldEventSystem pattern. It includes a central `WorldEventManager` and example components that publish and subscribe to events.

---

### File Structure for Unity Project:

1.  `Scripts/WorldEventSystem/WorldEventData.cs`
2.  `Scripts/WorldEventSystem/WorldEventManager.cs`
3.  `Scripts/Events/ItemCollectedEventData.cs`
4.  `Scripts/Events/GameStartedEventData.cs`
5.  `Scripts/Events/PlayerDiedEventData.cs`
6.  `Scripts/GameLogic/PlayerController.cs`
7.  `Scripts/GameLogic/UIManager.cs`
8.  `Scripts/GameLogic/GameInitializer.cs`
9.  `Scripts/GameLogic/EnemySpawner.cs`

---

### 1. `WorldEventData.cs` (Base Event Data)

This script defines the base class for all event data. All specific event data classes will inherit from this, allowing the `WorldEventManager` to work with a common type.

```csharp
// Scripts/WorldEventSystem/WorldEventData.cs
using System;
using UnityEngine;

namespace WorldEventSystem
{
    /// <summary>
    /// Base class for all event data objects in the World Event System.
    /// All specific event data classes should inherit from this.
    /// </summary>
    [Serializable] // Allows serialization in the Inspector if needed, though not directly used by the system itself.
    public abstract class WorldEventData
    {
        // A timestamp can be useful for debugging or chronological processing.
        public readonly DateTime Timestamp;

        protected WorldEventData()
        {
            Timestamp = DateTime.UtcNow; // Record when the event data was created.
        }

        // You can add common properties or methods here that all events might need.
        // For example, an event source ID or a method to log event details.
        public virtual string GetDebugInfo()
        {
            return $"Event occurred at: {Timestamp.ToShortTimeString()}";
        }
    }
}
```

---

### 2. `WorldEventManager.cs` (The Core System)

This is the heart of the WorldEventSystem. It's a singleton MonoBehaviour that manages all event subscriptions and publications.

```csharp
// Scripts/WorldEventSystem/WorldEventManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEventSystem
{
    /// <summary>
    /// The central World Event Manager.
    /// This is a singleton MonoBehaviour that provides a global message bus
    /// for publishing and subscribing to various game events.
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        // --- Singleton Implementation ---
        private static WorldEventManager _instance;
        public static WorldEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldEventManager>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(WorldEventManager).Name);
                        _instance = singletonObject.AddComponent<WorldEventManager>();
                    }
                }
                return _instance;
            }
        }

        // Dictionary to store event names (strings) mapped to a collection of subscriber methods (Actions).
        // Each Action takes a WorldEventData object, allowing us to pass specific event information.
        private readonly Dictionary<string, Action<WorldEventData>> _eventDictionary = new Dictionary<string, Action<WorldEventData>>();

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // If another instance already exists, destroy this one.
                Debug.LogWarning("Multiple WorldEventManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            // Make sure the event manager persists across scene loads.
            DontDestroyOnLoad(gameObject);

            Debug.Log($"WorldEventManager initialized on GameObject: {gameObject.name}");
        }

        private void OnDestroy()
        {
            // Clean up event dictionary to prevent memory leaks, especially if the manager is explicitly destroyed.
            // In a typical persistent singleton, this might only happen on application quit.
            if (_eventDictionary != null)
            {
                _eventDictionary.Clear();
            }
            if (_instance == this)
            {
                _instance = null; // Clear the static reference.
            }
            Debug.Log("WorldEventManager destroyed.");
        }

        // --- Public Event Methods ---

        /// <summary>
        /// Subscribes a listener method to a specific event.
        /// When an event with the given name is published, the listener will be invoked.
        /// </summary>
        /// <param name="eventName">The unique string identifier for the event.</param>
        /// <param name="listener">The method (Action<WorldEventData>) to be called when the event is published.</param>
        public void Subscribe(string eventName, Action<WorldEventData> listener)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("Attempted to subscribe to an event with a null or empty name.");
                return;
            }
            if (listener == null)
            {
                Debug.LogError($"Attempted to subscribe to event '{eventName}' with a null listener.");
                return;
            }

            if (_eventDictionary.ContainsKey(eventName))
            {
                _eventDictionary[eventName] += listener; // Add listener to existing event.
                // Debug.Log($"Subscribed listener to event: {eventName}");
            }
            else
            {
                _eventDictionary.Add(eventName, listener); // Create new event entry.
                // Debug.Log($"Created new event entry and subscribed listener to event: {eventName}");
            }
        }

        /// <summary>
        /// Unsubscribes a listener method from a specific event.
        /// It's crucial to unsubscribe to prevent memory leaks and unexpected behavior,
        /// especially when objects are destroyed or disabled.
        /// </summary>
        /// <param name="eventName">The unique string identifier for the event.</param>
        /// <param name="listener">The method (Action<WorldEventData>) to be unsubscribed.</param>
        public void Unsubscribe(string eventName, Action<WorldEventData> listener)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("Attempted to unsubscribe from an event with a null or empty name.");
                return;
            }
            if (listener == null)
            {
                Debug.LogError($"Attempted to unsubscribe from event '{eventName}' with a null listener.");
                return;
            }
            if (_instance == null || !_eventDictionary.ContainsKey(eventName))
            {
                // If the manager is destroyed or event doesn't exist, no need to unsubscribe.
                return;
            }

            _eventDictionary[eventName] -= listener; // Remove listener.
            // Debug.Log($"Unsubscribed listener from event: {eventName}");

            // Optional: If no more listeners for this event, remove its entry from the dictionary.
            if (_eventDictionary[eventName] == null)
            {
                _eventDictionary.Remove(eventName);
                // Debug.Log($"Removed event entry for: {eventName} (no more listeners).");
            }
        }

        /// <summary>
        /// Publishes an event, invoking all registered listeners for that event.
        /// </summary>
        /// <param name="eventName">The unique string identifier for the event.</param>
        /// <param name="eventData">An object containing data relevant to the event. Must inherit from WorldEventData.</param>
        public void Publish(string eventName, WorldEventData eventData)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("Attempted to publish an event with a null or empty name.");
                return;
            }
            if (eventData == null)
            {
                Debug.LogWarning($"Event '{eventName}' published with null event data. Consider using an empty WorldEventData if no data is truly needed.");
                // It's generally better to pass at least an empty WorldEventData instance than null.
                // However, the system technically supports null data.
            }

            Action<WorldEventData> thisEvent;
            if (_eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                // Using a try-catch block to ensure that one faulty listener doesn't stop others.
                if (thisEvent != null)
                {
                    Delegate[] invocationList = thisEvent.GetInvocationList();
                    foreach (Delegate handler in invocationList)
                    {
                        try
                        {
                            (handler as Action<WorldEventData>)?.Invoke(eventData);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error invoking event listener for '{eventName}': {e.Message}\n{e.StackTrace}");
                        }
                    }
                }
                // Debug.Log($"Event published: {eventName} with data: {eventData?.GetDebugInfo()}");
            }
            else
            {
                // Debug.LogWarning($"Event '{eventName}' published but no listeners are registered for it.");
            }
        }
    }
}
```

---

### 3. `ItemCollectedEventData.cs` (Example Event Data)

```csharp
// Scripts/Events/ItemCollectedEventData.cs
using UnityEngine;
using WorldEventSystem; // Make sure to include the namespace for WorldEventData

namespace GameEvents
{
    /// <summary>
    /// Event data for when an item is collected by the player.
    /// </summary>
    public class ItemCollectedEventData : WorldEventData
    {
        public readonly string ItemName;
        public readonly int ScoreValue;
        public readonly Vector3 CollectPosition;

        public ItemCollectedEventData(string itemName, int scoreValue, Vector3 collectPosition)
        {
            ItemName = itemName;
            ScoreValue = scoreValue;
            CollectPosition = collectPosition;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Item Collected: {ItemName}, Value: {ScoreValue} at {CollectPosition}.";
        }
    }
}
```

---

### 4. `GameStartedEventData.cs` (Example Event Data)

```csharp
// Scripts/Events/GameStartedEventData.cs
using WorldEventSystem;

namespace GameEvents
{
    /// <summary>
    /// Event data for when the game officially starts.
    /// </summary>
    public class GameStartedEventData : WorldEventData
    {
        public readonly int InitialPlayerLives;
        public readonly string GameMode;

        public GameStartedEventData(int initialLives, string gameMode)
        {
            InitialPlayerLives = initialLives;
            GameMode = gameMode;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Game Started! Mode: {GameMode}, Lives: {InitialPlayerLives}.";
        }
    }
}
```

---

### 5. `PlayerDiedEventData.cs` (Example Event Data)

```csharp
// Scripts/Events/PlayerDiedEventData.cs
using WorldEventSystem;

namespace GameEvents
{
    /// <summary>
    /// Event data for when the player character dies.
    /// </summary>
    public class PlayerDiedEventData : WorldEventData
    {
        public readonly int FinalScore;
        public readonly string CauseOfDeath;

        public PlayerDiedEventData(int finalScore, string causeOfDeath)
        {
            FinalScore = finalScore;
            CauseOfDeath = causeOfDeath;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Player Died! Final Score: {FinalScore}, Cause: {CauseOfDeath}.";
        }
    }
}
```

---

### 6. `PlayerController.cs` (Example Publisher)

This component simulates player actions that trigger events.

```csharp
// Scripts/GameLogic/PlayerController.cs
using UnityEngine;
using WorldEventSystem; // Access the WorldEventManager
using GameEvents;       // Access specific event data

/// <summary>
/// Simulates a player controller that publishes events.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public string playerName = "Hero";
    public int currentScore = 0;

    [Tooltip("Press this key to simulate collecting an item.")]
    public KeyCode collectItemKey = KeyCode.E;
    [Tooltip("Press this key to simulate the player dying.")]
    public KeyCode playerDiedKey = KeyCode.K;

    void Update()
    {
        // --- Simulate Item Collection ---
        if (Input.GetKeyDown(collectItemKey))
        {
            string itemName = $"Coin_{Random.Range(1, 100)}";
            int scoreValue = Random.Range(10, 50);
            currentScore += scoreValue;

            // Create event data
            ItemCollectedEventData data = new ItemCollectedEventData(itemName, scoreValue, transform.position);

            // Publish the event!
            WorldEventManager.Instance.Publish("ItemCollected", data);
            Debug.Log($"<color=green>[{playerName}]</color> Published 'ItemCollected' for {itemName}. New Score: {currentScore}");
        }

        // --- Simulate Player Death ---
        if (Input.GetKeyDown(playerDiedKey))
        {
            // Create event data
            PlayerDiedEventData data = new PlayerDiedEventData(currentScore, "Fell into a pit");

            // Publish the event!
            WorldEventManager.Instance.Publish("PlayerDied", data);
            Debug.Log($"<color=red>[{playerName}]</color> Published 'PlayerDied'. Final Score: {currentScore}");
            Destroy(gameObject); // Player is dead, remove them.
        }
    }

    // Example of a player starting a level or similar (could also be GameInitializer)
    public void StartLevel(string levelName)
    {
        Debug.Log($"Player started level: {levelName}");
        // No event published here, just demonstrating it's a component that *could* publish.
    }

    // Example Usage in other scripts:
    /*
    // To publish an event from any script:
    // WorldEventManager.Instance.Publish("YourEventName", new YourEventData(...));
    */
}
```

---

### 7. `UIManager.cs` (Example Subscriber)

This component listens for events and updates the UI accordingly.

```csharp
// Scripts/GameLogic/UIManager.cs
using UnityEngine;
using TMPro; // Assuming TextMeshPro for UI text, requires importing TMP Essential Resources
using WorldEventSystem; // Access the WorldEventManager
using GameEvents;       // Access specific event data

/// <summary>
/// Manages UI elements and subscribes to relevant WorldEvents to update the display.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statusText;
    public GameObject gameOverPanel;

    private int _currentScore = 0;

    void Awake()
    {
        if (scoreText == null) Debug.LogError("Score Text is not assigned in UIManager!");
        if (statusText == null) Debug.LogError("Status Text is not assigned in UIManager!");
        if (gameOverPanel == null) Debug.LogError("Game Over Panel is not assigned in UIManager!");

        UpdateScoreText();
        UpdateStatusText("Waiting for game start...");
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void OnEnable()
    {
        // --- Subscribe to Events ---
        WorldEventManager.Instance.Subscribe("ItemCollected", OnItemCollected);
        WorldEventManager.Instance.Subscribe("GameStarted", OnGameStarted);
        WorldEventManager.Instance.Subscribe("PlayerDied", OnPlayerDied);

        Debug.Log("<color=cyan>[UI Manager]</color> Subscribed to events: ItemCollected, GameStarted, PlayerDied.");
    }

    void OnDisable()
    {
        // --- Unsubscribe from Events ---
        // IMPORTANT: Always unsubscribe when your MonoBehaviour is disabled or destroyed
        // to prevent memory leaks and 'Missing Reference' errors.
        if (WorldEventManager.Instance != null) // Check if manager still exists
        {
            WorldEventManager.Instance.Unsubscribe("ItemCollected", OnItemCollected);
            WorldEventManager.Instance.Unsubscribe("GameStarted", OnGameStarted);
            WorldEventManager.Instance.Unsubscribe("PlayerDied", OnPlayerDied);
            Debug.Log("<color=cyan>[UI Manager]</color> Unsubscribed from events.");
        }
    }

    // --- Event Handlers ---

    /// <summary>
    /// Handles the 'ItemCollected' event.
    /// </summary>
    /// <param name="eventData">The raw event data, which needs to be cast.</param>
    private void OnItemCollected(WorldEventData eventData)
    {
        // Cast the base WorldEventData to the specific type.
        // It's good practice to check if the cast is successful, though for known events it's usually safe.
        if (eventData is ItemCollectedEventData itemData)
        {
            _currentScore += itemData.ScoreValue;
            UpdateScoreText();
            UpdateStatusText($"Collected: {itemData.ItemName} (+{itemData.ScoreValue})");
            Debug.Log($"<color=cyan>[UI Manager]</color> Handled ItemCollected: {itemData.ItemName}. Current Score: {_currentScore}");
        }
    }

    /// <summary>
    /// Handles the 'GameStarted' event.
    /// </summary>
    /// <param name="eventData">The raw event data, which needs to be cast.</param>
    private void OnGameStarted(WorldEventData eventData)
    {
        if (eventData is GameStartedEventData gameData)
        {
            _currentScore = 0; // Reset score for a new game.
            UpdateScoreText();
            UpdateStatusText($"Game Started! Mode: {gameData.GameMode}, Lives: {gameData.InitialPlayerLives}");
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            Debug.Log($"<color=cyan>[UI Manager]</color> Handled GameStarted: {gameData.GetDebugInfo()}");
        }
    }

    /// <summary>
    /// Handles the 'PlayerDied' event.
    /// </summary>
    /// <param name="eventData">The raw event data, which needs to be cast.</param>
    private void OnPlayerDied(WorldEventData eventData)
    {
        if (eventData is PlayerDiedEventData playerDiedData)
        {
            UpdateStatusText($"GAME OVER! You died because: {playerDiedData.CauseOfDeath}. Final Score: {playerDiedData.FinalScore}");
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            Debug.Log($"<color=cyan>[UI Manager]</color> Handled PlayerDied: {playerDiedData.GetDebugInfo()}");
        }
    }

    // --- Helper Methods for UI Updates ---
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    // Example Usage in other scripts:
    /*
    // To subscribe to an event:
    // WorldEventManager.Instance.Subscribe("YourEventName", YourEventHandlerMethod);
    // YourEventHandlerMethod must match the signature: void YourEventHandlerMethod(WorldEventData eventData)

    // To unsubscribe (very important!):
    // WorldEventManager.Instance.Unsubscribe("YourEventName", YourEventHandlerMethod);
    */
}
```

---

### 8. `GameInitializer.cs` (Example Publisher)

This script kicks off the game by publishing an event.

```csharp
// Scripts/GameLogic/GameInitializer.cs
using UnityEngine;
using WorldEventSystem;
using GameEvents;

/// <summary>
/// A simple script to initialize the game and publish the 'GameStarted' event.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Game Start Settings")]
    public int initialPlayerLives = 3;
    public string gameMode = "Adventure";
    public float delayBeforeStart = 1f;

    void Start()
    {
        // Use Invoke to delay the event publication slightly, giving other components time to Awaken/OnEnable.
        Invoke(nameof(PublishGameStartedEvent), delayBeforeStart);
    }

    private void PublishGameStartedEvent()
    {
        // Create event data
        GameStartedEventData data = new GameStartedEventData(initialPlayerLives, gameMode);

        // Publish the event!
        WorldEventManager.Instance.Publish("GameStarted", data);
        Debug.Log($"<color=blue>[Game Initializer]</color> Published 'GameStarted' event.");
    }
}
```

---

### 9. `EnemySpawner.cs` (Another Example Subscriber)

This component listens for game start and player death to manage enemy spawning.

```csharp
// Scripts/GameLogic/EnemySpawner.cs
using UnityEngine;
using WorldEventSystem;
using GameEvents;

/// <summary>
/// Simulates an enemy spawner that reacts to game events.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public int maxEnemies = 5;

    private int _currentEnemies = 0;
    private bool _isSpawning = false;

    void OnEnable()
    {
        WorldEventManager.Instance.Subscribe("GameStarted", OnGameStarted);
        WorldEventManager.Instance.Subscribe("PlayerDied", OnPlayerDied);
        Debug.Log("<color=magenta>[Enemy Spawner]</color> Subscribed to events: GameStarted, PlayerDied.");
    }

    void OnDisable()
    {
        if (WorldEventManager.Instance != null)
        {
            WorldEventManager.Instance.Unsubscribe("GameStarted", OnGameStarted);
            WorldEventManager.Instance.Unsubscribe("PlayerDied", OnPlayerDied);
            Debug.Log("<color=magenta>[Enemy Spawner]</color> Unsubscribed from events.");
        }
        CancelInvoke(nameof(SpawnEnemy)); // Stop any pending invokes if disabled
    }

    private void OnGameStarted(WorldEventData eventData)
    {
        if (eventData is GameStartedEventData)
        {
            Debug.Log("<color=magenta>[Enemy Spawner]</color> Game Started event received. Starting enemy spawning.");
            _isSpawning = true;
            _currentEnemies = 0; // Reset enemy count
            InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval); // Start spawning
        }
    }

    private void OnPlayerDied(WorldEventData eventData)
    {
        if (eventData is PlayerDiedEventData)
        {
            Debug.Log("<color=magenta>[Enemy Spawner]</color> Player Died event received. Stopping enemy spawning.");
            _isSpawning = false;
            CancelInvoke(nameof(SpawnEnemy)); // Stop spawning immediately
            // Optionally, clear existing enemies
        }
    }

    private void SpawnEnemy()
    {
        if (!_isSpawning || _currentEnemies >= maxEnemies)
        {
            return;
        }

        if (enemyPrefab != null)
        {
            // Simple spawn at spawner's position
            Instantiate(enemyPrefab, transform.position + Random.insideUnitSphere * 5f, Quaternion.identity);
            _currentEnemies++;
            Debug.Log($"<color=magenta>[Enemy Spawner]</color> Spawned an enemy. Total: {_currentEnemies}");
        }
    }
}
```

---

### How to Set Up in Unity:

1.  **Create Folders**: In your Unity project's `Assets` folder, create a `Scripts` folder, and inside that, `WorldEventSystem`, `Events`, and `GameLogic`. Place the respective `.cs` files into these folders.
2.  **TextMeshPro (if not already)**: If you haven't used TextMeshPro before, Unity will prompt you to import its essential resources when you open a scene with a TMP component (like `UIManager` uses). Agree to this.
3.  **Create `WorldEventManager` GameObject**:
    *   Create an empty GameObject in your scene (e.g., named "GlobalManagers").
    *   Add the `WorldEventManager` component to it. This will ensure the singleton is initialized.
4.  **Create UI Canvas**:
    *   Go to `GameObject -> UI -> Canvas`.
    *   Inside the Canvas, create two `TextMeshPro - Text` objects (rename them `ScoreText` and `StatusText`). Position them on the screen.
    *   Create a `Panel` (rename `GameOverPanel`), make it partially transparent, add a `TextMeshPro - Text` child to it (e.g., "GAME OVER"). Make sure the `GameOverPanel` is initially inactive in the Inspector.
5.  **Create `UIManager` GameObject**:
    *   Create an empty GameObject (e.g., "UIManager").
    *   Add the `UIManager` component to it.
    *   Drag your `ScoreText`, `StatusText`, and `GameOverPanel` UI elements from the Canvas into the respective slots in the `UIManager` component's Inspector.
6.  **Create `Player` GameObject**:
    *   Create a Cube or Capsule (rename "Player").
    *   Add the `PlayerController` component to it.
7.  **Create `GameInitializer` GameObject**:
    *   Create an empty GameObject (e.g., "GameInitializer").
    *   Add the `GameInitializer` component to it.
8.  **Create `EnemySpawner` GameObject**:
    *   Create an empty GameObject (e.g., "EnemySpawner").
    *   Add the `EnemySpawner` component to it.
    *   Create a simple `Cube` in the scene, make it a prefab (drag it from hierarchy to `Assets` folder), then delete it from the scene.
    *   Drag your `Enemy` prefab into the `Enemy Prefab` slot of the `EnemySpawner` component.
9.  **Run the Scene**:
    *   Observe the Console logs.
    *   Press the `E` key (default for `PlayerController`) to simulate collecting items. The score and status in the UI will update.
    *   Press the `K` key (default for `PlayerController`) to simulate the player dying. The `GameOverPanel` will appear, and enemy spawning will stop.

This setup provides a fully functional and educational example of the WorldEventSystem pattern in Unity, ready to be expanded upon for more complex game logic.