// Unity Design Pattern Example: DataReplicationSystem
// This script demonstrates the DataReplicationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `DataReplicationSystem` design pattern, in a Unity context, focuses on establishing a single, authoritative source of data and providing a robust, decoupled mechanism for various "consumers" or "replicas" to receive and synchronize with that data. It's particularly useful for:

1.  **Centralized Game State:** Managing player stats, game settings, or inventory from a single, reliable point.
2.  **UI Synchronization:** Ensuring all UI elements displaying specific data are always up-to-date with the core game state.
3.  **Decoupling:** Allowing different parts of your game (e.g., UI, AI, save system) to react to data changes without direct dependencies on the source logic.
4.  **Preparing for Networking:** The principles form a strong foundation for networked game state synchronization.

This example will demonstrate a local Data Replication System using a `ScriptableObject` as the authoritative data source, and various `MonoBehaviour`s as consumers and modifiers.

---

### **Core Components of the DataReplicationSystem:**

1.  **Replicable Data (Struct/Class):** The actual data payload that needs to be replicated. It should be `[System.Serializable]` so Unity can work with it.
2.  **Authoritative Data Source (ScriptableObject):** This is the "hub" or "publisher." It holds the *single, true copy* of the data. It provides an event or callback mechanism to notify all subscribers when the data changes and methods to update its internal state.
3.  **Data Consumers (MonoBehaviour):** These are the "subscribers." They listen for data change notifications from the authoritative source and update their local state (e.g., UI elements, internal variables) to match the replicated data.
4.  **Data Modifiers (MonoBehaviour/Game Logic):** These are components or systems that change the authoritative data. When they do, they use the Authoritative Data Source's methods, which in turn trigger the replication event.

---

### **Example Scenario: Player Statistics**

We will create a system where:
*   `PlayerStats` (a struct) holds the player's health, score, and level.
*   `PlayerStatsReplicator` (a `ScriptableObject`) is the central, authoritative source for `PlayerStats`.
*   `PlayerStatsUIReplicator` (a `MonoBehaviour`) subscribes to `PlayerStatsReplicator` and updates UI Text elements when stats change.
*   `PlayerStatsModifier` (a `MonoBehaviour`) periodically modifies the `PlayerStatsReplicator` to simulate gameplay (player taking damage, gaining score, leveling up).

---

### **Step-by-Step Implementation:**

**1. Create the `PlayerStats` Data Structure**
This struct will represent the actual data payload that gets replicated.

```csharp
// PlayerStats.cs
using System;
using UnityEngine;

namespace DataReplicationSystemExample
{
    /// <summary>
    /// The core data structure for player statistics.
    /// This is the data payload that will be replicated.
    /// Marked as [Serializable] so Unity can display it in the Inspector and persist it.
    /// Using a struct ensures that when it's passed via an event, a copy is made,
    /// reinforcing the "replication" concept where consumers get their own instance of the data.
    /// </summary>
    [Serializable]
    public struct PlayerStats
    {
        [Tooltip("Current health of the player.")]
        public int health;
        [Tooltip("Player's current score.")]
        public int score;
        [Tooltip("Player's current level.")]
        public int level;

        public PlayerStats(int health, int score, int level)
        {
            this.health = health;
            this.score = score;
            this.level = level;
        }

        // Helper method to represent stats as a string
        public override string ToString()
        {
            return $"Health: {health}\nScore: {score}\nLevel: {level}";
        }
    }
}
```

**2. Create the `PlayerStatsReplicator` (Authoritative Data Source)**
This `ScriptableObject` acts as the central hub. It holds the authoritative `PlayerStats` and provides an event for subscribers.

```csharp
// PlayerStatsReplicator.cs
using UnityEngine;
using System; // Required for Action
using DataReplicationSystemExample; // Namespace for PlayerStats

namespace DataReplicationSystemExample
{
    /// <summary>
    /// The Authoritative Data Source for PlayerStats.
    /// This is a ScriptableObject, allowing it to exist as an asset in the project,
    /// independent of any single scene or GameObject. This makes it a perfect
    /// central hub for game-wide data.
    ///
    /// It holds the 'true' current state of PlayerStats and provides an event
    /// that other systems can subscribe to for updates.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStatsReplicator", menuName = "Data Replication/Player Stats Replicator")]
    public class PlayerStatsReplicator : ScriptableObject
    {
        [Tooltip("The authoritative, current player statistics.")]
        [SerializeField] private PlayerStats _currentStats = new PlayerStats(100, 0, 1);

        /// <summary>
        /// Event fired when the authoritative PlayerStats data has been updated and replicated.
        /// Subscribers should listen to this event to receive the latest PlayerStats.
        /// The event passes a PlayerStats struct, ensuring a copy of the data is sent
        /// to each subscriber, maintaining data integrity.
        /// </summary>
        public event Action<PlayerStats> OnStatsReplicated;

        /// <summary>
        /// Provides read-only access to the current authoritative player stats.
        /// Consumers can read this directly, but should primarily react to OnStatsReplicated events
        /// to ensure they act on *changes*.
        /// </summary>
        public PlayerStats CurrentStats => _currentStats;

        /// <summary>
        /// Updates the authoritative PlayerStats with new values and triggers the replication process.
        /// This is the primary method for modifying the central data.
        /// </summary>
        /// <param name="newHealth">New health value.</param>
        /// <param name="newScore">New score value.</param>
        /// <param name="newLevel">New level value.</param>
        public void UpdateStats(int newHealth, int newScore, int newLevel)
        {
            _currentStats.health = newHealth;
            _currentStats.score = newScore;
            _currentStats.level = newLevel;
            ReplicateData(); // Trigger replication after updating
        }

        /// <summary>
        /// Updates the authoritative PlayerStats with a new PlayerStats object and triggers replication.
        /// </summary>
        /// <param name="newStats">The new PlayerStats object.</param>
        public void UpdateStats(PlayerStats newStats)
        {
            _currentStats = newStats;
            ReplicateData(); // Trigger replication after updating
        }

        /// <summary>
        /// Explicitly triggers the replication event to notify all subscribers
        /// about the current state of the data. This is called automatically by UpdateStats,
        /// but can be called manually if internal changes occur without using UpdateStats.
        /// </summary>
        public void ReplicateData()
        {
            // The '?.Invoke(_currentStats)' syntax safely calls the event only if there are subscribers.
            // When _currentStats (a struct) is passed, a copy is implicitly made,
            // so subscribers receive their own replicated instance of the data.
            OnStatsReplicated?.Invoke(_currentStats);
            Debug.Log($"[DataReplicationSystem] Player Stats Replicated: {_currentStats.ToString().Replace("\n", ", ")}");
        }

        /// <summary>
        /// Resets the player stats to their initial values and triggers replication.
        /// Useful for game restarts or editor functionality.
        /// </summary>
        public void ResetStats()
        {
            _currentStats = new PlayerStats(100, 0, 1);
            ReplicateData();
        }

        // Called when the ScriptableObject is enabled (e.g., when the game starts or in editor if selected)
        // You might want to trigger initial replication here, or reset data in editor mode.
        private void OnEnable()
        {
            // For example, to ensure initial data is always replicated on game start
            // or after editor changes. Be careful not to cause infinite loops or
            // unwanted behavior if other OnEnable methods also call UpdateStats.
            // ReplicateData(); 
        }

        // Called when the ScriptableObject is disabled
        private void OnDisable()
        {
            // Clear all event subscribers to prevent memory leaks,
            // especially important for ScriptableObjects that persist across scene loads.
            if (OnStatsReplicated != null)
            {
                foreach (Delegate handler in OnStatsReplicated.GetInvocationList())
                {
                    OnStatsReplicated -= (Action<PlayerStats>)handler;
                }
            }
        }
    }
}
```

**3. Create the `PlayerStatsUIReplicator` (Data Consumer/Replica)**
This `MonoBehaviour` will display the replicated `PlayerStats` on UI Text elements.

```csharp
// PlayerStatsUIReplicator.cs
using UnityEngine;
using TMPro; // Assuming you're using TextMeshPro for UI
using DataReplicationSystemExample; // Namespace for PlayerStatsReplicator

namespace DataReplicationSystemExample
{
    /// <summary>
    /// A Data Consumer/Replica component that subscribes to the PlayerStatsReplicator.
    /// It updates UI Text elements to reflect the latest player statistics received
    /// from the authoritative source.
    /// </summary>
    public class PlayerStatsUIReplicator : MonoBehaviour
    {
        [Tooltip("Reference to the authoritative PlayerStatsReplicator ScriptableObject.")]
        [SerializeField] private PlayerStatsReplicator _playerStatsReplicator;

        [Header("UI Elements")]
        [Tooltip("TextMeshProUGUI for displaying player health.")]
        [SerializeField] private TextMeshProUGUI _healthText;
        [Tooltip("TextMeshProUGUI for displaying player score.")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [Tooltip("TextMeshProUGUI for displaying player level.")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [Tooltip("TextMeshProUGUI for displaying all player stats combined.")]
        [SerializeField] private TextMeshProUGUI _fullStatsText;

        /// <summary>
        /// Called when the GameObject becomes enabled and active.
        /// This is where the component subscribes to the replication event.
        /// It also immediately updates the UI to ensure it reflects the current state
        /// when it's first activated or re-activated.
        /// </summary>
        void OnEnable()
        {
            if (_playerStatsReplicator != null)
            {
                // Subscribe to the event: when OnStatsReplicated is invoked, UpdateUI will be called.
                _playerStatsReplicator.OnStatsReplicated += UpdateUI;
                
                // Immediately update UI with the current stats in case they changed
                // while this component was disabled, or for initial display.
                UpdateUI(_playerStatsReplicator.CurrentStats);
            }
            else
            {
                Debug.LogError("[DataReplicationSystem] PlayerStatsReplicator is not assigned on " + gameObject.name + ". UI will not update.", this);
                enabled = false; // Disable this component if the replicator is missing
            }
        }

        /// <summary>
        /// Called when the GameObject becomes disabled or inactive.
        /// It's crucial to unsubscribe from events here to prevent memory leaks
        /// and ensure the component doesn't try to update UI when it's not active.
        /// </summary>
        void OnDisable()
        {
            if (_playerStatsReplicator != null)
            {
                // Unsubscribe from the event.
                _playerStatsReplicator.OnStatsReplicated -= UpdateUI;
            }
        }

        /// <summary>
        /// This method is the callback for the OnStatsReplicated event.
        /// It receives the latest PlayerStats and updates the UI elements accordingly.
        /// </summary>
        /// <param name="newStats">The newly replicated PlayerStats data.</param>
        private void UpdateUI(PlayerStats newStats)
        {
            if (_healthText != null) _healthText.text = $"Health: {newStats.health}";
            if (_scoreText != null) _scoreText.text = $"Score: {newStats.score}";
            if (_levelText != null) _levelText.text = $"Level: {newStats.level}";
            if (_fullStatsText != null) _fullStatsText.text = $"Current Stats:\n{newStats}";

            Debug.Log($"[DataReplicationSystem] UI (on {gameObject.name}) Updated with: {newStats.ToString().Replace("\n", ", ")}");
        }
    }
}
```

**4. Create the `PlayerStatsModifier` (Data Modifier)**
This `MonoBehaviour` simulates game logic that modifies the authoritative `PlayerStatsReplicator`.

```csharp
// PlayerStatsModifier.cs
using UnityEngine;
using DataReplicationSystemExample; // Namespace for PlayerStatsReplicator and PlayerStats

namespace DataReplicationSystemExample
{
    /// <summary>
    /// A Data Modifier component that simulates game logic affecting player stats.
    /// It periodically updates the authoritative PlayerStatsReplicator, which in turn
    /// triggers the data replication to all subscribed consumers.
    /// </summary>
    public class PlayerStatsModifier : MonoBehaviour
    {
        [Tooltip("Reference to the authoritative PlayerStatsReplicator ScriptableObject.")]
        [SerializeField] private PlayerStatsReplicator _playerStatsReplicator;

        [Header("Simulation Settings")]
        [Tooltip("How often (in seconds) the stats are automatically modified.")]
        [SerializeField] private float _updateInterval = 2f;
        [Tooltip("Amount of health to change (e.g., -5 for damage, +10 for heal).")]
        [SerializeField] private int _healthChange = -5;
        [Tooltip("Amount of score gained per automatic update.")]
        [SerializeField] private int _scoreGain = 10;
        [Tooltip("Score required to level up from the current level.")]
        [SerializeField] private int _levelUpScoreThreshold = 50;
        [Tooltip("Max health for the player (will cap healing).")]
        [SerializeField] private int _maxHealth = 100;

        private float _timer;
        private int _currentLevelUpThreshold;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the timer and the level-up threshold based on current stats.
        /// </summary>
        void Start()
        {
            if (_playerStatsReplicator == null)
            {
                Debug.LogError("[DataReplicationSystem] PlayerStatsReplicator is not assigned on " + gameObject.name + ". Modifier will not function.", this);
                enabled = false; // Disable this component if the replicator is missing
                return;
            }
            _timer = _updateInterval;
            // Initialize the threshold based on the current level of the replicated stats.
            _currentLevelUpThreshold = _playerStatsReplicator.CurrentStats.level * _levelUpScoreThreshold;
        }

        /// <summary>
        /// Called once per frame. Handles periodic stat modifications and input-based updates.
        /// </summary>
        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                ModifyStats(); // Automatically modify stats
                _timer = _updateInterval;
            }

            // Example: An external event (like a button press) triggering an immediate update.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[DataReplicationSystem] Spacebar pressed: Healing player!");
                PlayerStats current = _playerStatsReplicator.CurrentStats;
                // Directly call UpdateStats on the authoritative source.
                // This will also trigger the replication event.
                _playerStatsReplicator.UpdateStats(
                    Mathf.Min(_maxHealth, current.health + 20), // Heal, but don't exceed max health
                    current.score,
                    current.level
                );
            }
        }

        /// <summary>
        /// Applies simulated changes to player stats and updates the authoritative source.
        /// </summary>
        private void ModifyStats()
        {
            PlayerStats currentStats = _playerStatsReplicator.CurrentStats;

            // Calculate new stats
            int newHealth = Mathf.Min(_maxHealth, Mathf.Max(0, currentStats.health + _healthChange)); // Cap health between 0 and maxHealth
            int newScore = currentStats.score + _scoreGain;
            int newLevel = currentStats.level;

            // Check for level up condition
            if (newScore >= _currentLevelUpThreshold)
            {
                newLevel++;
                _currentLevelUpThreshold = newLevel * _levelUpScoreThreshold; // Increase threshold for the next level
                Debug.Log($"[DataReplicationSystem] Player Leveled Up to {newLevel}!");
            }

            // Update the authoritative data source.
            // This call internally triggers the OnStatsReplicated event,
            // notifying all subscribed consumers (like the UI).
            _playerStatsReplicator.UpdateStats(newHealth, newScore, newLevel);
        }

        /// <summary>
        /// Public method to reset game stats, potentially called from a UI button or another system.
        /// </summary>
        public void ResetGameStats()
        {
            _playerStatsReplicator.ResetStats();
            // Recalculate level up threshold based on the reset level
            _currentLevelUpThreshold = _playerStatsReplicator.CurrentStats.level * _levelUpScoreThreshold;
            Debug.Log("[DataReplicationSystem] Player Stats Reset by external call!");
        }
    }
}
```

---

### **Unity Setup Instructions:**

To get this example running in your Unity project:

1.  **Create Folders:**
    *   Create a folder named `Scripts` in your Project window.
    *   Inside `Scripts`, create another folder named `DataReplicationSystemExample`.
    *   Place all four C# scripts (`PlayerStats.cs`, `PlayerStatsReplicator.cs`, `PlayerStatsUIReplicator.cs`, `PlayerStatsModifier.cs`) into the `DataReplicationSystemExample` folder.

2.  **Create the Authoritative Data Source (ScriptableObject):**
    *   In the Project window, navigate to your `DataReplicationSystemExample` folder.
    *   Right-click -> Create -> Data Replication -> **Player Stats Replicator**.
    *   Name this new asset `GlobalPlayerStats`. This is your single source of truth for player stats.

3.  **Setup UI Display:**
    *   In your scene, create a new **Canvas** (GameObject -> UI -> Canvas).
    *   Make sure you have **TextMeshPro** imported (if not, Unity will prompt you when you create a TextMeshPro UI element).
    *   On the Canvas, create several TextMeshPro - Text (UI) elements. For example, four of them.
        *   Position them clearly on the canvas (e.g., Health, Score, Level, and one for Full Stats).
    *   Create an empty GameObject in the scene (e.g., `PlayerStatsDisplay`).
    *   Add the `PlayerStatsUIReplicator` component to this `PlayerStatsDisplay` GameObject.
    *   In the Inspector of `PlayerStatsDisplay`:
        *   Drag the `GlobalPlayerStats` asset (from your Project window) into the `_playerStatsReplicator` field.
        *   Drag your created TextMeshPro UI elements into the `_healthText`, `_scoreText`, `_levelText`, and `_fullStatsText` fields respectively.

4.  **Setup the Data Modifier:**
    *   Create an empty GameObject in the scene (e.g., `GameManager`).
    *   Add the `PlayerStatsModifier` component to this `GameManager` GameObject.
    *   In the Inspector of `GameManager`:
        *   Drag the `GlobalPlayerStats` asset (from your Project window) into the `_playerStatsReplicator` field.
        *   You can adjust the `_updateInterval`, `_healthChange`, `_scoreGain`, `_levelUpScoreThreshold`, and `_maxHealth` values to see how they affect the simulation.

5.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   You should immediately see the UI elements update with the initial player stats.
    *   Every `_updateInterval` seconds, the stats will change, and the UI will automatically update to reflect the new values.
    *   Press the **Spacebar** to see an immediate healing event that also updates the UI.
    *   Watch the Console for debug messages confirming replication and UI updates.

---

### **How this Demonstrates the DataReplicationSystem Pattern:**

*   **Authoritative Source:** The `GlobalPlayerStats` `ScriptableObject` is the single source of truth for `PlayerStats`. All modifications happen through its methods (`UpdateStats`, `ResetStats`).
*   **Data Replication Trigger:** Calling `UpdateStats` or `ReplicateData` on the `PlayerStatsReplicator` explicitly triggers the `OnStatsReplicated` event. This is the "replication" step – broadcasting the latest state.
*   **Replicated Data:** The `PlayerStats` `struct` is passed by value to event subscribers. This means each `PlayerStatsUIReplicator` (and any other consumer) receives its *own copy* of the current data, ensuring consistency and preventing accidental modification of the authoritative source by a consumer.
*   **Consumers/Replicas:** The `PlayerStatsUIReplicator` instances are passive listeners. They don't pull data; they react to pushes from the `PlayerStatsReplicator`, updating their local representation (the UI) accordingly.
*   **Decoupling:** The `PlayerStatsUIReplicator` doesn't need to know *who* or *how* the stats are being changed; it only needs to know *that* they changed. Similarly, the `PlayerStatsModifier` doesn't need to know *what* UI elements exist; it just updates the central data. This reduces dependencies and makes the system more modular.
*   **Scalability:** You could add more consumers (e.g., a `PlayerStatsSaveSystem` that writes `PlayerStats` to disk on `OnStatsReplicated`, or an `AnalyticsReporter` that sends updated stats to a server) without modifying the `PlayerStatsReplicator` or `PlayerStatsModifier`.

This example provides a robust, decoupled, and highly maintainable way to manage and synchronize important game data within your Unity projects.