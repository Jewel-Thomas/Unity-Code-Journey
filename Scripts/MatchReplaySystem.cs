// Unity Design Pattern Example: MatchReplaySystem
// This script demonstrates the MatchReplaySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Match Replay System is a design pattern used in games to record gameplay events and then play them back. This is useful for features like killcams, post-match analysis, tutorial demonstrations, or even debugging.

The core idea is to:
1.  **Record:** During live gameplay, capture significant game state changes or player actions as "replay events" and store them in a sequential list, usually with a timestamp.
2.  **Store:** Save the list of events (a "replay") for later use. This often involves serialization to disk.
3.  **Playback:** Load a saved replay and step through the events in order, re-creating the game state at each timestamp.

This example provides a complete, self-contained C# script for Unity that demonstrates this pattern.

**Key Components of the Match Replay System:**

*   **`MatchReplayManager` (MonoBehaviour):** The central orchestrator. It manages the current replay state (Idle, Recording, PlayingBack), records events, and executes events during playback. It also handles saving and loading replays.
*   **`ReplayEvent` (Abstract Base Class):** Defines the common properties (like `Timestamp`) and behavior for all replayable actions.
*   **Concrete `ReplayEvent` Classes:** Derived from `ReplayEvent`, these classes hold specific data for different types of events (e.g., `PlayerMoveEvent`, `EnemySpawnEvent`, `PlayerDamageEvent`).
*   **`ReplayData` (ScriptableObject):** A container for a list of `ReplayEvent`s, representing a complete replay. Using a `ScriptableObject` allows replays to be saved as assets within the Unity project or dynamically loaded.
*   **`ReplayableObject` (Base Class):** An optional base class for game objects that need to be explicitly managed (moved, spawned, etc.) by the replay system during playback. It provides a unique ID.
*   **Serialization Helpers:** Since `JsonUtility` (Unity's built-in JSON serializer) doesn't directly support polymorphic lists (a list of base class objects where actual instances are derived classes), we use `ReplayEventWrapper` and `ReplayDataSerializable` to properly save and load the different types of `ReplayEvent`s to/from JSON files on disk.

---

**How to Use This Script in Unity:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a New C# Script:** Name it `MatchReplaySystem.cs`.
3.  **Copy and Paste:** Replace the entire content of `MatchReplaySystem.cs` with the code provided below.
4.  **Create an Empty GameObject:** In your scene, create an empty GameObject (e.g., `_ReplayManager`).
5.  **Attach the Script:** Drag and drop the `MatchReplaySystem.cs` script onto the `_ReplayManager` GameObject.
6.  **Setup Prefabs:**
    *   Create a simple `Player` GameObject (e.g., a Cube with a `Rigidbody`).
    *   Attach the `ReplayablePlayer.cs` script to this `Player` Cube.
    *   Attach the `SimplePlayerController.cs` script to this `Player` Cube.
    *   Drag this `Player` GameObject into your Project window to create a prefab (e.g., `Assets/Prefabs/PlayerPrefab.prefab`).
    *   Create a simple `Enemy` GameObject (e.g., a Sphere, make sure it has a Collider if you want it to interact).
    *   Drag this `Enemy` GameObject into your Project window to create a prefab (e.g., `Assets/Prefabs/EnemyPrefab.prefab`).
7.  **Configure the `_ReplayManager`:**
    *   Select the `_ReplayManager` GameObject in the Hierarchy.
    *   In the Inspector, assign your `PlayerPrefab` and `EnemyPrefab` to the corresponding slots in the `Match Replay Manager` component.
    *   Set a `Replay File Name` (e.g., "MyFirstReplay").
8.  **Run and Test:**
    *   Press Play in the Editor.
    *   **To Record:** Click the "Start Recording" button in the Inspector of the `_ReplayManager`.
    *   **Play the Game:** Use WASD keys to move your player. Observe the debug logs for events.
    *   **To Stop Recording:** Click the "Stop Recording" button. The replay will be saved to disk.
    *   **To Playback:** Click the "Start Playback" button. The player will be spawned and will move according to the recorded actions. Enemies will spawn.
    *   **To Stop Playback:** Click the "Stop Playback" button.

This setup will allow you to record your player's movement and simulate enemy spawning/player damage, then play it all back.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // For OrderBy

/// <summary>
/// This script demonstrates the 'Match Replay System' design pattern in Unity.
/// It allows recording game events (like player movement, enemy spawns, damage)
/// and playing them back later.
///
/// The entire system is contained within this single file as requested,
/// with internal classes and namespaces for organization.
/// </summary>

#region MatchReplayManager - The Orchestrator

/// <summary>
/// The central component for managing replay recording and playback.
/// This MonoBehaviour should be attached to a GameObject in your scene.
/// </summary>
public class MatchReplayManager : MonoBehaviour
{
    /// <summary>
    /// Defines the current state of the replay system.
    /// </summary>
    public enum ReplayState { Idle, Recording, PlayingBack }

    [Header("Replay System Settings")]
    [Tooltip("The current state of the replay system.")]
    [SerializeField] private ReplayState currentState = ReplayState.Idle;

    [Tooltip("Name of the replay file to save/load (e.g., 'MyMatchReplay').")]
    [SerializeField] private string replayFileName = "DefaultMatchReplay";

    [Tooltip("The path where replay files will be saved relative to Application.persistentDataPath.")]
    [SerializeField] private string replayFileFolder = "Replays";

    [Header("Playback Settings")]
    [Tooltip("Speed multiplier for playback. 1.0 is normal speed.")]
    [SerializeField] private float playbackSpeed = 1.0f;

    [Tooltip("Prefab for the replayable player character.")]
    [SerializeField] private GameObject playerPrefab;
    [Tooltip("Prefab for the replayable enemy character.")]
    [SerializeField] private GameObject enemyPrefab;

    // Internal data for recording and playback
    private MatchReplaySystem.ReplayData currentReplayData;
    private float recordingStartTime;
    private float playbackStartTime;
    private int currentPlaybackEventIndex;

    // Dictionary to keep track of spawned ReplayableObjects during playback by their unique ID.
    private Dictionary<string, MatchReplaySystem.ReplayableObjects.ReplayableObject> activeReplayObjects = new Dictionary<string, MatchReplaySystem.ReplayableObjects.ReplayableObject>();

    /// <summary>
    /// Gets the current replay state.
    /// </summary>
    public ReplayState CurrentState => currentState;

    /// <summary>
    /// Get the full path for replay files.
    /// </summary>
    private string ReplayFilePath => Path.Combine(Application.persistentDataPath, replayFileFolder, replayFileName + ".json");

    void Awake()
    {
        // Ensure the replay folder exists
        string folderPath = Path.Combine(Application.persistentDataPath, replayFileFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Create an empty ReplayData ScriptableObject to hold events during runtime.
        // This is primarily for internal management. When saving/loading, we use JSON.
        currentReplayData = ScriptableObject.CreateInstance<MatchReplaySystem.ReplayData>();
    }

    void Update()
    {
        switch (currentState)
        {
            case ReplayState.Recording:
                // Update the duration of the current recording.
                currentReplayData.ReplayDuration = Time.time - recordingStartTime;
                break;

            case ReplayState.PlayingBack:
                HandlePlayback();
                break;
        }
    }

    /// <summary>
    /// Starts the replay recording process.
    /// Clears any existing replay data and resets the timer.
    /// </summary>
    [ContextMenu("Start Recording")]
    public void StartRecording()
    {
        if (currentState != ReplayState.Idle)
        {
            Debug.LogWarning("Cannot start recording. System is not idle.");
            return;
        }

        Debug.Log("Starting replay recording...");
        currentState = ReplayState.Recording;
        currentReplayData.ClearEvents();
        recordingStartTime = Time.time;

        // Optionally, clear existing player/enemies or reset the scene for a fresh recording start.
        ClearReplayObjects();
    }

    /// <summary>
    /// Stops the recording process and saves the collected events to a file.
    /// </summary>
    [ContextMenu("Stop Recording")]
    public void StopRecording()
    {
        if (currentState != ReplayState.Recording)
        {
            Debug.LogWarning("Not currently recording. Cannot stop recording.");
            return;
        }

        Debug.Log("Stopping replay recording...");
        currentState = ReplayState.Idle;
        currentReplayData.ReplayDuration = Time.time - recordingStartTime; // Final duration

        // Sort events by timestamp to ensure correct order for playback.
        currentReplayData.SortEvents();

        SaveReplayToFile(replayFileName);
        Debug.Log($"Recording stopped. Total events: {currentReplayData.Events.Count}. Duration: {currentReplayData.ReplayDuration:F2}s.");
    }

    /// <summary>
    /// Records a new event into the current replay data.
    /// Events are timestamped relative to the start of the recording.
    /// </summary>
    /// <param name="newEvent">The ReplayEvent to record.</param>
    public void RecordEvent(MatchReplaySystem.Events.ReplayEvent newEvent)
    {
        if (currentState == ReplayState.Recording)
        {
            newEvent.Timestamp = Time.time - recordingStartTime;
            currentReplayData.AddEvent(newEvent);
        }
    }

    /// <summary>
    /// Starts the replay playback process from the beginning of the loaded replay data.
    /// Loads a replay from file if not already loaded.
    /// </summary>
    [ContextMenu("Start Playback")]
    public void StartPlayback()
    {
        if (currentState != ReplayState.Idle)
        {
            Debug.LogWarning("Cannot start playback. System is not idle.");
            return;
        }

        Debug.Log("Starting replay playback...");

        // Attempt to load the replay if not already loaded or if current data is empty.
        if (currentReplayData.Events.Count == 0 || string.IsNullOrEmpty(currentReplayData.name) || currentReplayData.name != replayFileName)
        {
            if (!LoadReplayFromFile(replayFileName))
            {
                Debug.LogError($"Failed to load replay '{replayFileName}'. Cannot start playback.");
                return;
            }
        }

        if (currentReplayData.Events.Count == 0)
        {
            Debug.LogWarning("No events to play back in the loaded replay.");
            return;
        }

        currentState = ReplayState.PlayingBack;
        playbackStartTime = Time.time;
        currentPlaybackEventIndex = 0;

        // Clear existing objects before playback to ensure a clean slate.
        ClearReplayObjects();

        Debug.Log($"Playback started. Playing {currentReplayData.Events.Count} events over {currentReplayData.ReplayDuration:F2}s.");
    }

    /// <summary>
    /// Stops the playback process.
    /// </summary>
    [ContextMenu("Stop Playback")]
    public void StopPlayback()
    {
        if (currentState != ReplayState.PlayingBack)
        {
            Debug.LogWarning("Not currently playing back. Cannot stop playback.");
            return;
        }

        Debug.Log("Stopping replay playback.");
        currentState = ReplayState.Idle;
        // Optionally, destroy all replay-controlled objects or return them to a default state.
        ClearReplayObjects();
    }

    /// <summary>
    /// Handles the continuous processing of events during playback.
    /// </summary>
    private void HandlePlayback()
    {
        float currentPlaybackTime = (Time.time - playbackStartTime) * playbackSpeed;

        // Check if playback is finished
        if (currentPlaybackEventIndex >= currentReplayData.Events.Count && currentPlaybackTime >= currentReplayData.ReplayDuration)
        {
            StopPlayback();
            Debug.Log("Replay finished!");
            return;
        }

        // Process all events whose timestamp is less than or equal to the current playback time.
        while (currentPlaybackEventIndex < currentReplayData.Events.Count &&
               currentReplayData.Events[currentPlaybackEventIndex].Timestamp <= currentPlaybackTime)
        {
            MatchReplaySystem.Events.ReplayEvent currentEvent = currentReplayData.Events[currentPlaybackEventIndex];

            // Use pattern matching to handle different event types
            switch (currentEvent)
            {
                case MatchReplaySystem.Events.PlayerMoveEvent playerMove:
                    HandlePlayerMoveEvent(playerMove);
                    break;
                case MatchReplaySystem.Events.EnemySpawnEvent enemySpawn:
                    HandleEnemySpawnEvent(enemySpawn);
                    break;
                case MatchReplaySystem.Events.PlayerDamageEvent playerDamage:
                    HandlePlayerDamageEvent(playerDamage);
                    break;
                    // Add cases for other event types here
            }

            currentPlaybackEventIndex++;
        }
    }

    /// <summary>
    /// Executes a PlayerMoveEvent during playback.
    /// </summary>
    private void HandlePlayerMoveEvent(MatchReplaySystem.Events.PlayerMoveEvent playerMove)
    {
        if (activeReplayObjects.TryGetValue(playerMove.PlayerId, out MatchReplaySystem.ReplayableObjects.ReplayableObject obj))
        {
            obj.transform.position = playerMove.Position;
            obj.transform.rotation = playerMove.Rotation;
        }
        else
        {
            // If the player doesn't exist yet (e.g., first move event before spawn, or was destroyed), spawn it.
            // In a real game, initial player spawn would be its own event.
            if (playerPrefab != null)
            {
                GameObject newPlayerGo = Instantiate(playerPrefab, playerMove.Position, playerMove.Rotation);
                MatchReplaySystem.ReplayableObjects.ReplayableObject newPlayer = newPlayerGo.GetComponent<MatchReplaySystem.ReplayableObjects.ReplayableObject>();
                if (newPlayer != null)
                {
                    newPlayer.Initialize(playerMove.PlayerId); // Ensure the ID is set
                    activeReplayObjects.Add(playerMove.PlayerId, newPlayer);
                    Debug.Log($"[Replay] Initializing/Spawning Player {playerMove.PlayerId} at {playerMove.Position}");
                }
            }
        }
    }

    /// <summary>
    /// Executes an EnemySpawnEvent during playback.
    /// </summary>
    private void HandleEnemySpawnEvent(MatchReplaySystem.Events.EnemySpawnEvent enemySpawn)
    {
        if (!activeReplayObjects.ContainsKey(enemySpawn.EnemyId) && enemyPrefab != null)
        {
            GameObject newEnemyGo = Instantiate(enemyPrefab, enemySpawn.Position, enemySpawn.Rotation);
            MatchReplaySystem.ReplayableObjects.ReplayableObject newEnemy = newEnemyGo.GetComponent<MatchReplaySystem.ReplayableObjects.ReplayableObject>();
            if (newEnemy != null)
            {
                newEnemy.Initialize(enemySpawn.EnemyId);
                activeReplayObjects.Add(enemySpawn.EnemyId, newEnemy);
                Debug.Log($"[Replay] Spawning Enemy {enemySpawn.EnemyId} (Type: {enemySpawn.EnemyType}) at {enemySpawn.Position}");
            }
        }
    }

    /// <summary>
    /// Executes a PlayerDamageEvent during playback.
    /// </summary>
    private void HandlePlayerDamageEvent(MatchReplaySystem.Events.PlayerDamageEvent playerDamage)
    {
        if (activeReplayObjects.TryGetValue(playerDamage.TargetPlayerId, out MatchReplaySystem.ReplayableObjects.ReplayableObject obj))
        {
            // In a real game, this might trigger a visual damage effect, reduce health, etc.
            Debug.Log($"[Replay] Player {playerDamage.TargetPlayerId} took {playerDamage.DamageAmount} damage.");
        }
    }

    /// <summary>
    /// Destroys all currently active replayable objects.
    /// Called when stopping playback or before starting a new recording/playback session.
    /// </summary>
    private void ClearReplayObjects()
    {
        foreach (var obj in activeReplayObjects.Values)
        {
            if (obj != null && obj.gameObject != null)
            {
                Destroy(obj.gameObject);
            }
        }
        activeReplayObjects.Clear();
    }

    /// <summary>
    /// Saves the current replay data to a JSON file on disk.
    /// </summary>
    /// <param name="filename">The name of the file (without extension).</param>
    private void SaveReplayToFile(string filename)
    {
        try
        {
            // Use the serialization helper to convert polymorphic events to a serializable structure.
            MatchReplaySystem.Serialization.ReplayDataSerializable serializableData = new MatchReplaySystem.Serialization.ReplayDataSerializable
            {
                ReplayDuration = currentReplayData.ReplayDuration
            };

            foreach (var evt in currentReplayData.Events)
            {
                serializableData.Events.Add(new MatchReplaySystem.Serialization.ReplayEventWrapper
                {
                    EventType = evt.GetType().AssemblyQualifiedName, // Use AssemblyQualifiedName for robust type resolution
                    EventJson = JsonUtility.ToJson(evt)
                });
            }

            string json = JsonUtility.ToJson(serializableData, true); // 'true' for pretty print
            File.WriteAllText(ReplayFilePath, json);
            Debug.Log($"Replay saved to: {ReplayFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save replay to {ReplayFilePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads replay data from a JSON file on disk into currentReplayData.
    /// </summary>
    /// <param name="filename">The name of the file (without extension).</param>
    /// <returns>True if loading was successful, false otherwise.</returns>
    [ContextMenu("Load Replay from File")]
    private bool LoadReplayFromFile(string filename)
    {
        if (!File.Exists(ReplayFilePath))
        {
            Debug.LogWarning($"Replay file not found: {ReplayFilePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(ReplayFilePath);
            MatchReplaySystem.Serialization.ReplayDataSerializable serializableData = JsonUtility.FromJson<MatchReplaySystem.Serialization.ReplayDataSerializable>(json);

            currentReplayData.ClearEvents();
            currentReplayData.ReplayDuration = serializableData.ReplayDuration;
            currentReplayData.name = filename; // Set the name to match the loaded file

            foreach (var wrapper in serializableData.Events)
            {
                Type eventType = Type.GetType(wrapper.EventType);
                if (eventType != null && typeof(MatchReplaySystem.Events.ReplayEvent).IsAssignableFrom(eventType))
                {
                    MatchReplaySystem.Events.ReplayEvent evt = (MatchReplaySystem.Events.ReplayEvent)JsonUtility.FromJson(wrapper.EventJson, eventType);
                    currentReplayData.AddEvent(evt);
                }
                else
                {
                    Debug.LogWarning($"Could not load event type: {wrapper.EventType}");
                }
            }
            Debug.Log($"Replay loaded from: {ReplayFilePath}. Events: {currentReplayData.Events.Count}, Duration: {currentReplayData.ReplayDuration:F2}s.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load replay from {ReplayFilePath}: {ex.Message}");
            return false;
        }
    }

    void OnApplicationQuit()
    {
        // Clean up the dynamically created ScriptableObject
        if (currentReplayData != null)
        {
            Destroy(currentReplayData);
        }
    }
}

#endregion

namespace MatchReplaySystem
{
    #region ReplayData - ScriptableObject for Replay Container

    /// <summary>
    /// A ScriptableObject to hold a list of ReplayEvents and the total duration.
    /// This acts as the container for a single match replay.
    /// Using [SerializeReference] for the list of events enables polymorphic serialization
    /// in the Unity Editor and asset pipeline (Unity 2019.3+).
    /// For file-based JSON serialization, a custom wrapper is used (see ReplayDataSerializable).
    /// </summary>
    // [CreateAssetMenu(fileName = "NewReplayData", menuName = "Replay System/Replay Data", order = 1)] // Can create as asset if needed
    public class ReplayData : ScriptableObject
    {
        [Tooltip("List of all recorded events in this replay.")]
        [SerializeReference] // Enables polymorphic serialization for derived classes in the Inspector/Asset save.
        private List<Events.ReplayEvent> events = new List<Events.ReplayEvent>();

        [Tooltip("The total duration of the recorded match in seconds.")]
        public float ReplayDuration;

        /// <summary>
        /// Gets the list of replay events.
        /// </summary>
        public List<Events.ReplayEvent> Events => events;

        /// <summary>
        /// Adds a new event to the replay data.
        /// </summary>
        /// <param name="newEvent">The event to add.</param>
        public void AddEvent(Events.ReplayEvent newEvent)
        {
            events.Add(newEvent);
        }

        /// <summary>
        /// Clears all events from the replay data.
        /// </summary>
        public void ClearEvents()
        {
            events.Clear();
            ReplayDuration = 0;
        }

        /// <summary>
        /// Sorts the events by timestamp in ascending order.
        /// Essential for correct playback sequence.
        /// </summary>
        public void SortEvents()
        {
            events = events.OrderBy(e => e.Timestamp).ToList();
        }
    }

    #endregion

    #region ReplayEvents - Base Class and Concrete Event Implementations

    namespace Events
    {
        /// <summary>
        /// The abstract base class for all replayable events.
        /// All concrete event types must inherit from this and be serializable.
        /// </summary>
        [Serializable]
        public abstract class ReplayEvent
        {
            [Tooltip("The time in seconds (relative to recording start) when this event occurred.")]
            public float Timestamp;
        }

        /// <summary>
        /// Represents a player movement event.
        /// </summary>
        [Serializable]
        public class PlayerMoveEvent : ReplayEvent
        {
            public string PlayerId;
            public Vector3 Position;
            public Quaternion Rotation;

            public PlayerMoveEvent(float timestamp, string playerId, Vector3 position, Quaternion rotation)
            {
                Timestamp = timestamp;
                PlayerId = playerId;
                Position = position;
                Rotation = rotation;
            }
        }

        /// <summary>
        /// Represents an enemy spawning event.
        /// </summary>
        [Serializable]
        public class EnemySpawnEvent : ReplayEvent
        {
            public string EnemyId;
            public string EnemyType; // e.g., "Goblin", "Orc", etc.
            public Vector3 Position;
            public Quaternion Rotation;

            public EnemySpawnEvent(float timestamp, string enemyId, string enemyType, Vector3 position, Quaternion rotation)
            {
                Timestamp = timestamp;
                EnemyId = enemyId;
                EnemyType = enemyType;
                Position = position;
                Rotation = rotation;
            }
        }

        /// <summary>
        /// Represents a player taking damage event.
        /// </summary>
        [Serializable]
        public class PlayerDamageEvent : ReplayEvent
        {
            public string TargetPlayerId;
            public int DamageAmount;
            public string SourceEntityId; // Optional: who dealt the damage

            public PlayerDamageEvent(float timestamp, string targetPlayerId, int damageAmount, string sourceEntityId = null)
            {
                Timestamp = timestamp;
                TargetPlayerId = targetPlayerId;
                DamageAmount = damageAmount;
                SourceEntityId = sourceEntityId;
            }
        }

        // --- Example of other potential replay events ---
        /*
        [Serializable]
        public class ProjectileFiredEvent : ReplayEvent
        {
            public string ShooterId;
            public Vector3 StartPosition;
            public Vector3 Direction;
            public float Speed;
            public ProjectileFiredEvent(float timestamp, string shooterId, Vector3 startPosition, Vector3 direction, float speed)
            {
                Timestamp = timestamp;
                ShooterId = shooterId;
                StartPosition = startPosition;
                Direction = direction;
                Speed = speed;
            }
        }

        [Serializable]
        public class ObjectDestroyedEvent : ReplayEvent
        {
            public string ObjectId;
            public string Reason;
            public ObjectDestroyedEvent(float timestamp, string objectId, string reason)
            {
                Timestamp = timestamp;
                ObjectId = objectId;
                Reason = reason;
            }
        }
        */
    }

    #endregion

    #region Serialization - Helpers for JSON File I/O with Polymorphism

    namespace Serialization
    {
        /// <summary>
        /// A wrapper class used for serializing individual ReplayEvents to JSON.
        /// It stores the full type name of the event and its JSON representation.
        /// This is a workaround for JsonUtility's lack of direct polymorphic serialization.
        /// </summary>
        [Serializable]
        public class ReplayEventWrapper
        {
            public string EventType; // Stores the AssemblyQualifiedName of the event type
            public string EventJson; // Stores the JSON string of the actual event data
        }

        /// <summary>
        /// A serializable container for a list of ReplayEventWrappers and replay duration.
        /// This structure is used when saving/loading replay data to/from disk via JsonUtility.
        /// </summary>
        [Serializable]
        public class ReplayDataSerializable
        {
            public List<ReplayEventWrapper> Events = new List<ReplayEventWrapper>();
            public float ReplayDuration;
        }
    }

    #endregion

    #region ReplayableObjects - Base Class for GameObjects Managed by Replay System

    namespace ReplayableObjects
    {
        /// <summary>
        /// Base class for any GameObject whose state needs to be managed or recorded by the replay system.
        /// Provides a unique ID for tracking.
        /// </summary>
        public class ReplayableObject : MonoBehaviour
        {
            [Tooltip("A unique identifier for this replayable object.")]
            [SerializeField] private string replayableId;

            public string ReplayableId => replayableId;

            /// <summary>
            /// Initializes the replayable object with a unique ID.
            /// Should be called during instantiation or Awake().
            /// </summary>
            /// <param name="id">Optional. A specific ID to use. If null, a new GUID is generated.</param>
            public void Initialize(string id = null)
            {
                if (string.IsNullOrEmpty(replayableId)) // Only set if not already assigned in Inspector or by another call
                {
                    replayableId = id ?? Guid.NewGuid().ToString();
                    Debug.Log($"ReplayableObject '{gameObject.name}' initialized with ID: {replayableId}");
                }
            }

            // In a real project, Awake() or Start() could call Initialize()
            // to ensure every instance has an ID.
            protected virtual void Awake()
            {
                // Ensure ID exists, especially if object is placed in scene and not spawned by replay.
                Initialize();
            }
        }

        /// <summary>
        /// Example of a replayable player character.
        /// Its movement will be recorded and replayed.
        /// </summary>
        [RequireComponent(typeof(Rigidbody))]
        public class ReplayablePlayer : ReplayableObject
        {
            [Tooltip("How often (in seconds) the player's position should be recorded.")]
            [SerializeField] private float recordInterval = 0.1f;
            private float lastRecordTime;

            private MatchReplayManager replayManager;

            protected override void Awake()
            {
                base.Awake(); // Call base ReplayableObject Awake to initialize ID
                replayManager = FindObjectOfType<MatchReplayManager>();
                if (replayManager == null)
                {
                    Debug.LogError("MatchReplayManager not found in scene!");
                    enabled = false;
                }
            }

            void Update()
            {
                // Only record if the replay manager is in recording state and it's time to record.
                if (replayManager != null && replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    if (Time.time - lastRecordTime >= recordInterval)
                    {
                        RecordPlayerMove();
                        lastRecordTime = Time.time;
                    }
                }
            }

            /// <summary>
            /// Records a PlayerMoveEvent with the current position and rotation.
            /// </summary>
            private void RecordPlayerMove()
            {
                if (replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    Events.PlayerMoveEvent moveEvent = new Events.PlayerMoveEvent(
                        0, // Timestamp will be set by the manager
                        ReplayableId,
                        transform.position,
                        transform.rotation
                    );
                    replayManager.RecordEvent(moveEvent);
                }
            }

            /// <summary>
            /// Example method to simulate damage taken and record it.
            /// </summary>
            /// <param name="amount"></param>
            /// <param name="sourceId"></param>
            public void TakeDamage(int amount, string sourceId = "Unknown")
            {
                Debug.Log($"{ReplayableId} took {amount} damage from {sourceId}!");
                if (replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    Events.PlayerDamageEvent damageEvent = new Events.PlayerDamageEvent(
                        0, // Timestamp will be set by the manager
                        ReplayableId,
                        amount,
                        sourceId
                    );
                    replayManager.RecordEvent(damageEvent);
                }
                // In a real game, this would apply actual damage, play effects, etc.
            }
        }

        /// <summary>
        /// Example of a replayable enemy character.
        /// This class primarily exists to hold the ReplayableObject component and ID.
        /// Its spawning is triggered by `EnemySpawnEvent` during playback.
        /// During recording, its AI could trigger `PlayerDamageEvent`s.
        /// </summary>
        public class ReplayableEnemy : ReplayableObject
        {
            // Add any enemy-specific logic here, e.g., AI that records damage events.
            private MatchReplayManager replayManager;

            protected override void Awake()
            {
                base.Awake();
                replayManager = FindObjectOfType<MatchReplayManager>();
                if (replayManager == null)
                {
                    Debug.LogError("MatchReplayManager not found in scene!");
                    enabled = false;
                }
            }

            void Start()
            {
                // Example: During recording, an enemy might randomly try to damage the player.
                // For a simple demo, let's just show how to record an event.
                if (replayManager != null && replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    // Simulate an enemy dealing damage after a delay
                    Invoke(nameof(SimulateDamageToPlayer), UnityEngine.Random.Range(2f, 5f));
                }
            }

            private void SimulateDamageToPlayer()
            {
                // Find a player to damage (simplistic, assuming one player for demo)
                ReplayablePlayer player = FindObjectOfType<ReplayablePlayer>();
                if (player != null && replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    player.TakeDamage(10, ReplayableId); // Enemy deals 10 damage
                }
                // Reschedule for continuous damage, or destroy the enemy
                if (replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    Invoke(nameof(SimulateDamageToPlayer), UnityEngine.Random.Range(3f, 7f));
                }
            }
        }
    }

    #endregion

    #region Controllers - Example Player Controller for Generating Events

    namespace Controllers
    {
        /// <summary>
        /// A simple player controller that records movement events
        /// when the MatchReplayManager is in recording mode.
        /// </summary>
        [RequireComponent(typeof(Rigidbody), typeof(ReplayableObjects.ReplayablePlayer))]
        public class SimplePlayerController : MonoBehaviour
        {
            [Header("Player Movement")]
            [SerializeField] private float moveSpeed = 5f;
            [SerializeField] private float rotationSpeed = 180f;

            private Rigidbody rb;
            private MatchReplayManager replayManager;
            private ReplayableObjects.ReplayablePlayer replayablePlayer;

            void Awake()
            {
                rb = GetComponent<Rigidbody>();
                replayablePlayer = GetComponent<ReplayableObjects.ReplayablePlayer>();
                replayManager = FindObjectOfType<MatchReplayManager>();

                if (replayManager == null)
                {
                    Debug.LogError("MatchReplayManager not found in scene. Player controller disabled.");
                    enabled = false;
                    return;
                }
                if (replayablePlayer == null)
                {
                    Debug.LogError("ReplayablePlayer component not found. Player controller disabled.");
                    enabled = false;
                }
            }

            void FixedUpdate()
            {
                // Only allow input control if not currently playing back a replay.
                if (replayManager.CurrentState != MatchReplayManager.ReplayState.PlayingBack)
                {
                    HandleMovementInput();
                    HandleEnemySpawns(); // For demonstration purposes, player can trigger enemy spawns
                }
                else
                {
                    // If in playback, disable direct input control
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            private void HandleMovementInput()
            {
                float horizontal = Input.GetAxis("Horizontal"); // A/D keys
                float vertical = Input.GetAxis("Vertical");     // W/S keys

                Vector3 moveDirection = transform.forward * vertical;
                rb.velocity = moveDirection * moveSpeed + new Vector3(0, rb.velocity.y, 0);

                transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.fixedDeltaTime);
            }

            private void HandleEnemySpawns()
            {
                // Simple demonstration: Press Space to spawn an enemy during recording.
                if (Input.GetKeyDown(KeyCode.Space) && replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
                {
                    string enemyId = Guid.NewGuid().ToString();
                    Vector3 spawnPos = transform.position + transform.forward * 3f + Vector3.up * 0.5f;
                    replayManager.RecordEvent(new Events.EnemySpawnEvent(
                        0, // Timestamp set by manager
                        enemyId,
                        "BasicEnemy",
                        spawnPos,
                        Quaternion.identity
                    ));
                    Debug.Log($"Recorded Enemy Spawn Event: {enemyId} at {spawnPos}");
                }
            }
        }
    }

    #endregion
}

/*
/// <summary>
/// EXAMPLE USAGE IN OTHER SCRIPTS:
///
/// To record an event from another script (e.g., a Health system or a projectile):
///
/// public class MyHealthSystem : MonoBehaviour
/// {
///     private MatchReplayManager replayManager;
///     private MatchReplaySystem.ReplayableObjects.ReplayableObject replayableObject;
///
///     void Awake()
///     {
///         replayManager = FindObjectOfType<MatchReplayManager>();
///         replayableObject = GetComponent<MatchReplaySystem.ReplayableObjects.ReplayableObject>();
///     }
///
///     public void TakeDamage(int amount, string sourceId)
///     {
///         // Logic to apply damage...
///
///         // If recording, create and record a damage event
///         if (replayManager != null && replayManager.CurrentState == MatchReplayManager.ReplayState.Recording)
///         {
///             MatchReplaySystem.Events.PlayerDamageEvent damageEvent = new MatchReplaySystem.Events.PlayerDamageEvent(
///                 0, // Timestamp will be set by the manager
///                 replayableObject.ReplayableId,
///                 amount,
///                 sourceId
///             );
///             replayManager.RecordEvent(damageEvent);
///         }
///     }
/// }
///
/// To manually start/stop recording/playback from another UI manager or game state script:
///
/// public class UIManager : MonoBehaviour
/// {
///     private MatchReplayManager replayManager;
///
///     void Start()
///     {
///         replayManager = FindObjectOfType<MatchReplayManager>();
///     }
///
///     public void OnStartRecordingButtonClicked()
///     {
///         replayManager?.StartRecording();
///     }
///
///     public void OnStopRecordingButtonClicked()
///     {
///         replayManager?.StopRecording();
///     }
///
///     public void OnStartPlaybackButtonClicked()
///     {
///         replayManager?.StartPlayback();
///     }
///
///     public void OnStopPlaybackButtonClicked()
///     {
///         replayManager?.StopPlayback();
///     }
/// }
/// </summary>
*/
```