// Unity Design Pattern Example: EventReplaySystem
// This script demonstrates the EventReplaySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Event Replay System is a powerful design pattern used to record a sequence of events (actions, state changes) in a system and then replay them later to reconstruct the system's state or actions. This is incredibly useful in game development for features like:

*   **Killcams/Replays:** Showing a player's last moments or impressive plays.
*   **Spectator Modes:** Allowing players to watch a match as it unfolds or after it has finished.
*   **Debugging:** Reproducing bugs by replaying user input or game states.
*   **Undo/Redo Systems:** Reverting or re-applying actions.
*   **AI Training/Demonstration:** Showing AI behavior.
*   **Network Synchronization:** Sending events over a network and replaying them on clients to ensure consistent state.

---

### **Core Components of the Event Replay System Pattern:**

1.  **Event (or Command):**
    *   A data structure (class or struct) that represents a single, atomic action or state change that occurred in the system.
    *   It should contain all necessary information to re-apply that action or state change.
    *   Often includes a timestamp or sequence number to maintain chronological order.
    *   Should be serializable if events need to be saved/loaded.
    *   **In this example:** `GameEvent`, `PlayerMovedEvent`, `PlayerJumpedEvent`, `PlayerAttackedEvent`.

2.  **Event Store (or Event Log/Journal):**
    *   A central repository responsible for recording and storing all published events.
    *   Events are typically stored in the order they occurred.
    *   This is the "source of truth" for the system's history.
    *   **In this example:** `EventStore`.

3.  **Event Publisher (or Emitter):**
    *   The part of the system that generates events when specific actions or state changes happen.
    *   It doesn't directly act on the event's consequences, but rather creates the event and hands it to the `EventStore`.
    *   **In this example:** The `EventReplayManager` acts as the publisher, creating `PlayerMovedEvent`, `PlayerJumpedEvent`, `PlayerAttackedEvent` based on player input.

4.  **Event Replayer (or Projector/Consumer):**
    *   A component responsible for reading events from the `EventStore` and "re-applying" them to a target system.
    *   It effectively reconstructs the past state or actions by sequentially processing events.
    *   **In this example:** The `ReplayEventsCoroutine` method within `EventReplayManager` acts as the replayer, feeding events to a `ReplayablePlayer` instance.

5.  **Replayable Entity:**
    *   The object or system that can *receive* and *process* events to simulate actions or reconstruct its state.
    *   It defines methods that correspond to the event types it can handle.
    *   **In this example:** `ReplayablePlayer` with `SimulateMove`, `SimulateJump`, `SimulateAttack` methods.

---

### **Example Use Case: Player Action Recording and Replay in Unity**

This example demonstrates recording a player's movement, jumps, and attacks, then replaying those actions on a separate instance of the player.

### **How to Use This Script in Unity:**

1.  **Create a new C# script** named `EventReplaySystem.cs` in your Unity project.
2.  **Copy and paste** the entire code below into this new script.
3.  **Create a new 3D Cube GameObject** in your scene (GameObject -> 3D Object -> Cube).
4.  **Add a `Rigidbody` component** to the Cube (if it doesn't have one). This is needed for physics-based actions like jumping.
5.  **Add the `ReplayablePlayer` component** to this Cube.
6.  **Drag this Cube GameObject from the Hierarchy into your Project tab** to create a Prefab (e.g., name it `ReplayablePlayerPrefab`). You can then delete the Cube from the scene.
7.  **Create an Empty GameObject** in your scene (GameObject -> Create Empty) and name it `EventReplayManager`.
8.  **Add the `EventReplayManager` component** to the `EventReplayManager` GameObject.
9.  In the Inspector for `EventReplayManager`, **drag your `ReplayablePlayerPrefab`** from the Project tab into the `Replayable Player Prefab` slot.
10. **Run the scene!**
    *   You will see UI buttons in the top-left corner.
    *   Click "Start Recording". A player cube will appear.
    *   Use **WASD** to move, **Spacebar** to jump, and **Left Mouse Click** to attack (player turns red briefly).
    *   Click "Stop Recording".
    *   Click "Start Replay". A *second* player cube will appear and automatically perform the exact actions you just did.
    *   You can adjust the `Replay Speed` in the Inspector while replaying.
    *   Click "Clear Events" to remove the recorded history.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Used for .Any() and .ToList()

// --- 1. Define the Base Event Interface/Class ---
// An abstract base class for all game events.
// It includes a timestamp to maintain the order and timing of events, crucial for replay.
// [System.Serializable] allows these events to be serialized by Unity, which is
// important if you wanted to save and load recorded events to/from a file.
[System.Serializable]
public abstract class GameEvent
{
    public float Timestamp { get; private set; } // Time relative to the start of recording.

    // Constructor to set the timestamp
    protected GameEvent(float timestamp)
    {
        Timestamp = timestamp;
    }

    // Default constructor for serialization purposes (Unity sometimes requires it)
    protected GameEvent() { }
}

// --- 2. Concrete Event Implementations ---
// These classes represent specific actions or state changes in our game.
// They inherit from GameEvent and add specific data relevant to their action.

[System.Serializable]
public class PlayerMovedEvent : GameEvent
{
    public Vector3 Position;
    public Quaternion Rotation;

    public PlayerMovedEvent(float timestamp, Vector3 position, Quaternion rotation) : base(timestamp)
    {
        Position = position;
        Rotation = rotation;
    }
}

[System.Serializable]
public class PlayerJumpedEvent : GameEvent
{
    public PlayerJumpedEvent(float timestamp) : base(timestamp) { }
}

[System.Serializable]
public class PlayerAttackedEvent : GameEvent
{
    public PlayerAttackedEvent(float timestamp) : base(timestamp) { }
}

// --- 3. Event Store ---
// A central repository for all recorded events. This is the 'log' or 'journal'
// of everything that happened. Events are added in chronological order.
public class EventStore
{
    private List<GameEvent> _events = new List<GameEvent>();

    // Provides a read-only view of all recorded events.
    public IReadOnlyList<GameEvent> AllEvents => _events.AsReadOnly();

    // Records a new event, adding it to the end of the list.
    public void RecordEvent(GameEvent gameEvent)
    {
        _events.Add(gameEvent);
        // Debug.Log($"Recorded event: {gameEvent.GetType().Name} at {gameEvent.Timestamp:F2}");
    }

    // Clears all stored events, effectively resetting the history.
    public void ClearEvents()
    {
        _events.Clear();
        Debug.Log("Event store cleared.");
    }
}

// --- 4. Replayable Entity ---
// This is the component attached to game objects whose actions can be recorded
// and replayed. It provides methods to "simulate" or "apply" events,
// effectively making the object react as if the action just happened.
public class ReplayablePlayer : MonoBehaviour
{
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _attackDuration = 0.5f;

    private Rigidbody _rb;
    private MeshRenderer _meshRenderer;
    private Color _originalColor;
    private Coroutine _attackCoroutine;

    // IsControlledByInput: A flag to determine if this player instance should
    // respond to direct user input (for recording) or purely to events (for replay).
    public bool IsControlledByInput { get; set; }

    void Awake()
    {
        // Ensure Rigidbody is present for physics-based actions like jumping.
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent unwanted rotation
        }

        // Ensure MeshRenderer is present for visual feedback (e.g., attack color).
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (_meshRenderer == null)
        {
            // If no MeshRenderer, create a simple cube for visualization.
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one;
            _meshRenderer = cube.GetComponent<MeshRenderer>();
        }
        _originalColor = _meshRenderer.material.color;
    }

    // Update is kept simple here. The EventReplayManager handles direct input
    // and event generation for the 'live' player. This player component focuses
    // on simulating actions when told to.
    void Update()
    {
        // No direct input handling here; the manager feeds events or commands.
    }

    // --- Methods for Replay / Simulation ---
    // These methods apply the event data to the player, reconstructing its state/action.

    // Simulates movement to a specific position and rotation.
    // For replay, directly setting transform is often more precise than physics.
    public void SimulateMove(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    // Simulates a jump action using the Rigidbody.
    public void SimulateJump()
    {
        if (_rb != null)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z); // Reset vertical velocity
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    // Simulates an attack action, with a visual feedback.
    public void SimulateAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine); // Stop any ongoing attack visual
        }
        _attackCoroutine = StartCoroutine(AttackVisual());
    }

    // Coroutine for showing a visual cue during an attack.
    private IEnumerator AttackVisual()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = Color.red; // Change color to red
        }
        yield return new WaitForSeconds(_attackDuration);
        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = _originalColor; // Revert to original color
        }
        _attackCoroutine = null;
    }
}

// --- 5. Event Replay Manager (The Orchestrator) ---
// This MonoBehaviour ties everything together. It handles:
// - User interface (buttons for record/replay)
// - Instantiating and managing 'live' and 'replay' player instances
// - Recording player actions as events into the EventStore
// - Replaying events from the EventStore onto a replay player
public class EventReplayManager : MonoBehaviour
{
    [Tooltip("Prefab of the player character to record/replay.")]
    public GameObject replayablePlayerPrefab;

    [Tooltip("Speed multiplier for replaying events.")]
    [Range(0.1f, 5.0f)]
    public float replaySpeed = 1.0f;

    private EventStore _eventStore = new EventStore(); // The central log of events
    private ReplayablePlayer _livePlayerInstance; // The player controlled by the user
    private ReplayablePlayer _replayPlayerInstance; // The player instance used for replay

    private bool _isRecording = false;
    private bool _isReplaying = false;

    private float _recordingStartTime; // Timestamp when recording began
    private Coroutine _replayCoroutine; // Reference to the active replay coroutine

    // Called once when the script instance is being loaded.
    void Start()
    {
        CreateGroundPlane(); // Provide a simple ground for players to move on
    }

    // OnGUI is used here for a simple debug UI with buttons.
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 300)); // Define a UI area

        // UI for starting recording
        if (!_isRecording && !_isReplaying)
        {
            if (GUILayout.Button("Start Recording"))
            {
                StartRecording();
            }
        }
        // UI when recording is active
        else if (_isRecording)
        {
            if (GUILayout.Button("Stop Recording"))
            {
                StopRecording();
            }
            GUILayout.Label($"Recording... ({Time.time - _recordingStartTime:F2}s)");
        }
        // UI when replaying is active
        else if (_isReplaying)
        {
            if (GUILayout.Button("Stop Replay"))
            {
                StopReplay();
            }
            GUILayout.Label($"Replaying... (Speed: {replaySpeed:F1}x)");
        }

        // UI for starting replay or clearing events (only available when not recording/replaying)
        if (!_isRecording && !_isReplaying && _eventStore.AllEvents.Any())
        {
            if (GUILayout.Button("Start Replay"))
            {
                StartReplay();
            }
            if (GUILayout.Button("Clear Events"))
            {
                _eventStore.ClearEvents();
            }
            GUILayout.Label($"Stored Events: {_eventStore.AllEvents.Count}");
        }

        GUILayout.EndArea();
    }

    // Update is called once per frame. Used here for recording events based on live input.
    void Update()
    {
        if (_isRecording && _livePlayerInstance != null)
        {
            // Handle player movement input and apply it directly to the live player.
            // This also calculates the player's position/rotation for recording.
            HandleLivePlayerMovement();

            // Record a PlayerMovedEvent every frame with the current state of the live player.
            // The timestamp is relative to when recording started.
            _eventStore.RecordEvent(new PlayerMovedEvent(
                Time.time - _recordingStartTime,
                _livePlayerInstance.transform.position,
                _livePlayerInstance.transform.rotation
            ));

            // Check for other actions (Jump, Attack) and record them as events.
            // Also trigger the visual/physics effect on the live player immediately.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _livePlayerInstance.SimulateJump();
                _eventStore.RecordEvent(new PlayerJumpedEvent(Time.time - _recordingStartTime));
            }
            if (Input.GetMouseButtonDown(0)) // Left mouse click
            {
                _livePlayerInstance.SimulateAttack();
                _eventStore.RecordEvent(new PlayerAttackedEvent(Time.time - _recordingStartTime));
            }
        }
    }

    // Helper method to create a simple ground plane in the scene.
    private void CreateGroundPlane()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(10, 1, 10);
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.name = "Ground";
        if (ground.GetComponent<Collider>() == null) ground.AddComponent<BoxCollider>();
        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }


    // --- Recording Logic ---

    // Initializes the recording process.
    private void StartRecording()
    {
        if (_isRecording || _isReplaying) return; // Prevent starting if already busy

        Debug.Log("Starting Recording...");
        _eventStore.ClearEvents(); // Clear previous events
        _recordingStartTime = Time.time; // Mark the start time for relative timestamps

        // Instantiate a new player for the user to control during recording.
        if (_livePlayerInstance != null) Destroy(_livePlayerInstance.gameObject); // Clean up old player
        GameObject playerGO = Instantiate(replayablePlayerPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        _livePlayerInstance = playerGO.GetComponent<ReplayablePlayer>();
        if (_livePlayerInstance == null)
        {
            Debug.LogError("ReplayablePlayer component not found on prefab! Cannot record.");
            return;
        }
        _livePlayerInstance.IsControlledByInput = true; // Flag it as user-controlled
        _livePlayerInstance.name = "Live Player"; // Give it a distinct name
        _isRecording = true;
    }

    // Stops the recording process.
    private void StopRecording()
    {
        if (!_isRecording) return;

        Debug.Log($"Stopped Recording. Total events recorded: {_eventStore.AllEvents.Count}");
        _isRecording = false;
        if (_livePlayerInstance != null)
        {
            Destroy(_livePlayerInstance.gameObject); // Remove the live player
            _livePlayerInstance = null;
        }
    }

    // Handles WASD input to move the live player.
    private void HandleLivePlayerMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // Rotate player to face the movement direction
            _livePlayerInstance.transform.rotation = Quaternion.Slerp(
                _livePlayerInstance.transform.rotation,
                Quaternion.LookRotation(moveDirection),
                Time.deltaTime * 10f
            );

            // Move player in the world space
            _livePlayerInstance.transform.Translate(moveDirection * 5f * Time.deltaTime, Space.World);
        }
    }


    // --- Replay Logic ---

    // Initializes the replay process.
    private void StartReplay()
    {
        if (_isRecording || _isReplaying || !_eventStore.AllEvents.Any()) return; // Prevent if busy or no events

        Debug.Log("Starting Replay...");
        _isReplaying = true;

        // Instantiate a new player instance specifically for replaying events.
        if (_replayPlayerInstance != null) Destroy(_replayPlayerInstance.gameObject); // Clean up old replay player
        GameObject replayPlayerGO = Instantiate(replayablePlayerPrefab, Vector3.up * 0.5f, Quaternion.identity);
        _replayPlayerInstance = replayPlayerGO.GetComponent<ReplayablePlayer>();
        if (_replayPlayerInstance == null)
        {
            Debug.LogError("ReplayablePlayer component not found on prefab for replay! Cannot replay.");
            StopReplay();
            return;
        }
        _replayPlayerInstance.IsControlledByInput = false; // Important: replay player ignores user input
        _replayPlayerInstance.name = "Replay Player"; // Distinct name

        // Start the coroutine that steps through and applies events.
        _replayCoroutine = StartCoroutine(ReplayEventsCoroutine(_eventStore.AllEvents.ToList()));
    }

    // Stops the replay process.
    private void StopReplay()
    {
        if (!_isReplaying) return;

        Debug.Log("Stopped Replay.");
        _isReplaying = false;
        if (_replayCoroutine != null)
        {
            StopCoroutine(_replayCoroutine); // Stop the coroutine
            _replayCoroutine = null;
        }
        if (_replayPlayerInstance != null)
        {
            Destroy(_replayPlayerInstance.gameObject); // Remove the replay player
            _replayPlayerInstance = null;
        }
    }

    // Coroutine that iterates through recorded events and applies them over time.
    private IEnumerator ReplayEventsCoroutine(List<GameEvent> eventsToReplay)
    {
        if (eventsToReplay == null || eventsToReplay.Count == 0 || _replayPlayerInstance == null)
        {
            StopReplay();
            yield break;
        }

        float replayTime = 0f; // Our internal clock for the replay
        int eventIndex = 0;

        // Adjust replay clock to be relative to the first event's timestamp.
        // This ensures replay starts from "time 0" of the recorded sequence,
        // even if the first recorded event's timestamp was not 0 (e.g., if recording started after game began).
        float firstEventTimestamp = eventsToReplay[0].Timestamp;

        while (eventIndex < eventsToReplay.Count && _isReplaying)
        {
            GameEvent currentEvent = eventsToReplay[eventIndex];

            // Calculate the target time for this event in the replay sequence.
            float targetReplayTime = currentEvent.Timestamp - firstEventTimestamp;

            // Wait until our replay clock catches up to this event's target time.
            while (replayTime < targetReplayTime && _isReplaying)
            {
                replayTime += Time.deltaTime * replaySpeed; // Advance replay clock
                yield return null; // Wait for the next frame
            }

            // Apply the event to the replay player.
            ApplyEventToReplayPlayer(currentEvent);
            eventIndex++;
        }

        Debug.Log("Replay finished.");
        StopReplay(); // Automatically stop replay once all events are processed
    }

    // Dispatches the given GameEvent to the appropriate simulation method on the replay player.
    private void ApplyEventToReplayPlayer(GameEvent gameEvent)
    {
        if (_replayPlayerInstance == null) return;

        // Use pattern matching (C# 7.0+) to handle different event types.
        switch (gameEvent)
        {
            case PlayerMovedEvent moveEvent:
                _replayPlayerInstance.SimulateMove(moveEvent.Position, moveEvent.Rotation);
                break;
            case PlayerJumpedEvent jumpEvent:
                _replayPlayerInstance.SimulateJump();
                break;
            case PlayerAttackedEvent attackEvent:
                _replayPlayerInstance.SimulateAttack();
                break;
            default:
                Debug.LogWarning($"Unhandled event type during replay: {gameEvent.GetType().Name}");
                break;
        }
    }
}
```