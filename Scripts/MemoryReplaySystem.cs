// Unity Design Pattern Example: MemoryReplaySystem
// This script demonstrates the MemoryReplaySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Memory Replay System is a design pattern often used in areas like Reinforcement Learning, AI training, debugging, and advanced undo/redo systems. It involves storing past experiences (called "memories" or "states") in a buffer, and then later sampling and "replaying" those memories to learn from them, analyze them, or reconstruct past events.

**Core Components of a Memory Replay System:**

1.  **Memory (Experience):** A data structure that encapsulates a single snapshot of the system's state or an action taken, along with relevant context (e.g., position, velocity, input, time).
2.  **Memory Buffer (Replay Buffer):** A collection (often a fixed-size circular buffer) that stores multiple `Memory` objects. When the buffer is full, new memories overwrite the oldest ones.
3.  **Recorder:** The component responsible for observing the system, creating `Memory` objects, and adding them to the `Memory Buffer`.
4.  **Replayer:** The component responsible for retrieving `Memory` objects from the `Memory Buffer` (either sequentially or by random sampling) and applying them to reconstruct or simulate past events.

---

### Real-World Use Case: Player Movement Replay & Debugging

In this Unity example, we'll implement a Memory Replay System to record a player's movement, rotation, and input over time. Later, we can "replay" these memories using a "ghost" object to visualize the player's path, analyze their actions, or even help debug issues.

---

### Unity Setup Instructions:

1.  **Create an Empty GameObject:** Name it `MemoryReplayManager`.
2.  **Attach `MemoryReplaySystem.cs`:** Drag and drop the `MemoryReplaySystem.cs` script onto the `MemoryReplayManager` GameObject.
3.  **Create a Player Object:**
    *   Create a 3D Cube (or any simple primitive). Name it `Player`.
    *   Add a `Rigidbody` component to it (uncheck "Use Gravity" for simpler movement demo, or adjust movement force if gravity is on).
    *   Add a `PlayerMovement.cs` script (provided below) to control the player.
    *   Add a `PlayerMemoryRecorder.cs` script to the `Player` object.
    *   **Crucially:** In the Inspector for `PlayerMemoryRecorder` on the `Player` object, drag the `MemoryReplayManager` GameObject into the `Replay System` slot.
4.  **Create a Ghost Prefab:**
    *   Create another 3D Cube. Name it `GhostPlayer`.
    *   Change its material color to something distinct (e.g., blue, green, or semi-transparent) so it's clearly different from the original player.
    *   Drag the `GhostPlayer` from the Hierarchy into your Project window to create a prefab.
    *   Delete the `GhostPlayer` from the Hierarchy.
5.  **Create another Empty GameObject:** Name it `ReplayController`.
6.  **Attach `PlayerMemoryReplayer.cs`:** Drag and drop the `PlayerMemoryReplayer.cs` script onto the `ReplayController` GameObject.
7.  **Crucially:** In the Inspector for `PlayerMemoryReplayer` on the `ReplayController` object:
    *   Drag the `MemoryReplayManager` GameObject into the `Replay System` slot.
    *   Drag the `GhostPlayer` prefab from your Project window into the `Ghost Player Prefab` slot.
8.  **Run the Scene:**
    *   Use WASD to move the player around.
    *   Press `R` to **Start Recording** (a message will appear in the console).
    *   Keep moving the player for a while.
    *   Press `R` again to **Stop Recording**.
    *   Press `P` to **Start Replay**. A ghost object will appear and move along the path your player took.
    *   Press `P` again to **Stop Replay**.
    *   Press `C` to **Clear All Memories**.

---

### The C# Unity Scripts:

---

#### 1. `MemoryReplaySystem.cs` (Central Manager)

This script manages the buffer of memories. It's designed as a singleton for easy access.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Clear method

/// <summary>
/// Represents a single 'memory' or 'experience' of the player's state.
/// This struct holds all relevant data points needed to reconstruct or analyze a past moment.
/// </summary>
[System.Serializable] // Make it visible in the inspector for debugging if needed
public struct PlayerMemory
{
    public Vector3 position;       // Player's position at the time of recording
    public Quaternion rotation;    // Player's rotation at the time of recording
    public Vector2 inputDirection; // Normalized input direction (e.g., WASD)
    public bool jumped;            // Whether the player jumped at this moment
    public float timestamp;        // The time (since game start) when this memory was recorded

    public PlayerMemory(Vector3 pos, Quaternion rot, Vector2 input, bool jump, float time)
    {
        position = pos;
        rotation = rot;
        inputDirection = input;
        jumped = jump;
        timestamp = time;
    }

    public override string ToString()
    {
        return $"Time: {timestamp:F2}, Pos: {position}, Rot: {rotation.eulerAngles}, Input: {inputDirection}, Jump: {jumped}";
    }
}

/// <summary>
/// The core Memory Replay System. Manages a circular buffer of PlayerMemory objects.
/// This acts as the central hub for recording and retrieving memories.
/// </summary>
public class MemoryReplaySystem : MonoBehaviour
{
    // Singleton instance to allow easy access from other scripts
    public static MemoryReplaySystem Instance { get; private set; }

    [Header("Buffer Configuration")]
    [Tooltip("The maximum number of memories to store in the buffer.")]
    [SerializeField] private int bufferSize = 1000;

    // The actual buffer storing the memories. Using a List to simplify circular buffer logic.
    private List<PlayerMemory> _memories;
    
    // Index for the next memory to be overwritten in the circular buffer.
    private int _currentMemoryIndex = 0;
    
    // The actual number of memories currently stored (can be less than bufferSize if not full).
    public int CurrentMemoryCount { get; private set; }

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple MemoryReplaySystem instances found. Destroying duplicate.", this);
            Destroy(this);
        }
        else
        {
            Instance = this;
            InitializeBuffer();
        }
    }

    /// <summary>
    /// Initializes the memory buffer.
    /// </summary>
    private void InitializeBuffer()
    {
        _memories = new List<PlayerMemory>(bufferSize);
        CurrentMemoryCount = 0;
        _currentMemoryIndex = 0;
        Debug.Log($"MemoryReplaySystem initialized with buffer size: {bufferSize}");
    }

    /// <summary>
    /// Adds a new memory to the replay buffer.
    /// Implements a circular buffer: if full, overwrites the oldest memory.
    /// </summary>
    /// <param name="memory">The PlayerMemory object to add.</param>
    public void AddMemory(PlayerMemory memory)
    {
        if (_memories.Count < bufferSize)
        {
            // Buffer is not yet full, just add the new memory
            _memories.Add(memory);
            CurrentMemoryCount = _memories.Count;
        }
        else
        {
            // Buffer is full, overwrite the memory at the current index
            _memories[_currentMemoryIndex] = memory;
            CurrentMemoryCount = bufferSize; // Ensure it stays at max capacity
        }

        // Move to the next index, wrapping around if we reach the end of the buffer
        _currentMemoryIndex = (_currentMemoryIndex + 1) % bufferSize;
    }

    /// <summary>
    /// Retrieves a memory from the buffer at a specific index.
    /// </summary>
    /// <param name="index">The index of the memory to retrieve.</param>
    /// <returns>The PlayerMemory at the specified index.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
    public PlayerMemory GetMemoryAt(int index)
    {
        if (index < 0 || index >= CurrentMemoryCount)
        {
            throw new System.IndexOutOfRangeException($"Memory index {index} is out of bounds. Current memory count: {CurrentMemoryCount}");
        }

        // When the buffer is full (CurrentMemoryCount == bufferSize),
        // the actual memory at 'logical' index 0 might be at a shifted physical index
        // due to the circular nature.
        // We need to calculate the actual physical index in the List.
        if (CurrentMemoryCount == bufferSize)
        {
            // If the buffer is full, the logical index '0' corresponds to the element
            // right after the 'currentMemoryIndex' (which points to the *next* write position).
            // So, the oldest element is at _currentMemoryIndex, and the newest is just before it.
            // A logical index `i` maps to `(_currentMemoryIndex + i) % bufferSize`.
            return _memories[(_currentMemoryIndex + index) % bufferSize];
        }
        else
        {
            // If the buffer is not full, indices are straightforward.
            return _memories[index];
        }
    }

    /// <summary>
    /// Retrieves a random memory from the buffer. Useful for AI training.
    /// </summary>
    /// <returns>A randomly selected PlayerMemory.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    public PlayerMemory GetRandomMemory()
    {
        if (CurrentMemoryCount == 0)
        {
            throw new System.InvalidOperationException("Cannot get random memory from an empty buffer.");
        }
        int randomIndex = Random.Range(0, CurrentMemoryCount);
        return GetMemoryAt(randomIndex); // Use GetMemoryAt to handle circular buffer logic
    }

    /// <summary>
    /// Clears all memories from the buffer.
    /// </summary>
    public void ClearMemories()
    {
        _memories.Clear();
        CurrentMemoryCount = 0;
        _currentMemoryIndex = 0;
        Debug.Log("MemoryReplaySystem: All memories cleared.");
    }
}
```

---

#### 2. `PlayerMovement.cs` (Example Player Controller)

A simple script to make the player move so there's something to record.

```csharp
using UnityEngine;

/// <summary>
/// Simple player movement script for demonstration purposes.
/// Allows movement with WASD and jumping with Space.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpForce = 5.0f;

    private Rigidbody _rb;
    private bool _isGrounded; // Simplified ground check

    public Vector2 CurrentInputDirection { get; private set; }
    public bool HasJumpedThisFrame { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb.useGravity == false)
        {
            Debug.LogWarning("Rigidbody on PlayerMovement has 'Use Gravity' unchecked. Player won't fall without external force.", this);
        }
    }

    void Update()
    {
        // Reset jump flag at the start of the frame
        HasJumpedThisFrame = false;

        // Get input
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        CurrentInputDirection = new Vector2(moveHorizontal, moveVertical).normalized;

        // Movement relative to camera/world forward
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        _rb.MovePosition(_rb.position + movement * moveSpeed * Time.deltaTime);

        // Jump input
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            HasJumpedThisFrame = true;
            _isGrounded = false; // Assume no longer grounded immediately after jump
        }
    }

    // Simplified ground detection for demo
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Make sure your ground object has tag "Ground"
        {
            _isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }
}
```

---

#### 3. `PlayerMemoryRecorder.cs` (The Recorder)

This script observes the player and adds `PlayerMemory` objects to the `MemoryReplaySystem`.

```csharp
using UnityEngine;

/// <summary>
/// The Recorder component of the Memory Replay System.
/// This script observes a player's actions and state, creating `PlayerMemory` objects
/// and adding them to the central `MemoryReplaySystem`.
/// </summary>
[RequireComponent(typeof(PlayerMovement))] // Ensures player movement script is present
public class PlayerMemoryRecorder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the central MemoryReplaySystem.")]
    [SerializeField] private MemoryReplaySystem replaySystem;

    [Header("Recording Settings")]
    [Tooltip("How often to record player state, in seconds.")]
    [SerializeField] private float recordInterval = 0.1f;

    private PlayerMovement _playerMovement;
    private float _lastRecordTime;
    private bool _isRecording = false;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (replaySystem == null)
        {
            // Try to find the system if not assigned in Inspector
            replaySystem = FindObjectOfType<MemoryReplaySystem>();
            if (replaySystem == null)
            {
                Debug.LogError("PlayerMemoryRecorder: MemoryReplaySystem not found in scene or not assigned!", this);
                enabled = false; // Disable this script if no system is available
                return;
            }
        }
    }

    private void Update()
    {
        // Toggle recording with 'R' key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRecording();
        }

        if (_isRecording)
        {
            // Only record if enough time has passed since the last recording
            if (Time.time - _lastRecordTime >= recordInterval)
            {
                RecordCurrentState();
                _lastRecordTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Toggles the recording state.
    /// </summary>
    public void ToggleRecording()
    {
        _isRecording = !_isRecording;
        if (_isRecording)
        {
            Debug.Log("PlayerMemoryRecorder: Started recording player memories.");
            _lastRecordTime = Time.time; // Reset timer on start
        }
        else
        {
            Debug.Log($"PlayerMemoryRecorder: Stopped recording. Total memories: {replaySystem.CurrentMemoryCount}");
        }
    }

    /// <summary>
    /// Captures the player's current state and creates a new PlayerMemory.
    /// Adds the memory to the MemoryReplaySystem.
    /// </summary>
    private void RecordCurrentState()
    {
        // Create a new PlayerMemory object from the current player's state
        PlayerMemory memory = new PlayerMemory(
            transform.position,
            transform.rotation,
            _playerMovement.CurrentInputDirection,
            _playerMovement.HasJumpedThisFrame,
            Time.time
        );

        // Add the created memory to the central replay system
        replaySystem.AddMemory(memory);
        // Debug.Log($"Recorded memory: {memory}");
    }

    /// <summary>
    /// Public method to manually start recording (e.g., from a UI button).
    /// </summary>
    public void StartRecording() => _isRecording = true;

    /// <summary>
    /// Public method to manually stop recording (e.g., from a UI button).
    /// </summary>
    public void StopRecording() => _isRecording = false;

    public bool IsRecording => _isRecording;
}
```

---

#### 4. `PlayerMemoryReplayer.cs` (The Replayer)

This script samples memories from the `MemoryReplaySystem` and applies them to a "ghost" object to visualize the replay.

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// The Replayer component of the Memory Replay System.
/// This script retrieves `PlayerMemory` objects from the central `MemoryReplaySystem`
/// and applies them to a 'ghost' object to visually replay past events.
/// </summary>
public class PlayerMemoryReplayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the central MemoryReplaySystem.")]
    [SerializeField] private MemoryReplaySystem replaySystem;

    [Tooltip("Prefab for the ghost player that will replay the memories.")]
    [SerializeField] private GameObject ghostPlayerPrefab;

    [Header("Replay Settings")]
    [Tooltip("Multiplier for replay speed. 1 = real-time, 0.5 = half speed, 2 = double speed.")]
    [SerializeField] private float replaySpeed = 1.0f;

    private GameObject _currentGhostInstance; // The instantiated ghost object
    private Coroutine _replayCoroutine;       // Reference to the running replay coroutine
    private bool _isReplaying = false;

    private void Awake()
    {
        if (replaySystem == null)
        {
            // Try to find the system if not assigned in Inspector
            replaySystem = FindObjectOfType<MemoryReplaySystem>();
            if (replaySystem == null)
            {
                Debug.LogError("PlayerMemoryReplayer: MemoryReplaySystem not found in scene or not assigned!", this);
                enabled = false; // Disable this script if no system is available
                return;
            }
        }
        if (ghostPlayerPrefab == null)
        {
            Debug.LogError("PlayerMemoryReplayer: Ghost Player Prefab not assigned!", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Toggle replay with 'P' key
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleReplay();
        }
        // Clear memories with 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            replaySystem.ClearMemories();
        }
    }

    /// <summary>
    /// Toggles the replay state. If replaying, it stops. If not replaying, it starts.
    /// </summary>
    public void ToggleReplay()
    {
        if (_isReplaying)
        {
            StopReplay();
        }
        else
        {
            StartReplay();
        }
    }

    /// <summary>
    /// Initiates the replay process. Spawns a ghost object and starts a coroutine
    /// to iterate through the recorded memories.
    /// </summary>
    public void StartReplay()
    {
        if (replaySystem.CurrentMemoryCount == 0)
        {
            Debug.LogWarning("PlayerMemoryReplayer: No memories to replay!");
            return;
        }

        if (_isReplaying)
        {
            Debug.LogWarning("PlayerMemoryReplayer: Already replaying. Stopping current replay before starting new one.");
            StopReplay(); // Stop any existing replay before starting a new one
        }

        Debug.Log($"PlayerMemoryReplayer: Starting replay of {replaySystem.CurrentMemoryCount} memories at {replaySpeed}x speed.");

        // Instantiate the ghost player at the location of the first memory
        PlayerMemory firstMemory = replaySystem.GetMemoryAt(0);
        _currentGhostInstance = Instantiate(ghostPlayerPrefab, firstMemory.position, firstMemory.rotation);
        _currentGhostInstance.name = "Ghost Player (Replay)";

        _isReplaying = true;
        _replayCoroutine = StartCoroutine(ReplaySequence());
    }

    /// <summary>
    /// Stops the current replay, destroying the ghost object and stopping the coroutine.
    /// </summary>
    public void StopReplay()
    {
        if (_replayCoroutine != null)
        {
            StopCoroutine(_replayCoroutine);
            _replayCoroutine = null;
        }

        if (_currentGhostInstance != null)
        {
            Destroy(_currentGhostInstance);
            _currentGhostInstance = null;
        }

        _isReplaying = false;
        Debug.Log("PlayerMemoryReplayer: Replay stopped.");
    }

    /// <summary>
    /// Coroutine that iterates through stored memories and applies them to the ghost player.
    /// </summary>
    private IEnumerator ReplaySequence()
    {
        float startTime = Time.time;
        float lastMemoryTime = replaySystem.GetMemoryAt(0).timestamp;

        // Loop through all stored memories
        for (int i = 0; i < replaySystem.CurrentMemoryCount; i++)
        {
            PlayerMemory memory = replaySystem.GetMemoryAt(i);

            // Calculate the time difference from the previous memory
            // This ensures replay speed is accurate based on recording intervals
            float timeDiff = memory.timestamp - lastMemoryTime;
            float waitTime = timeDiff / replaySpeed; // Adjust for replay speed

            // Apply the memory's state to the ghost player
            if (_currentGhostInstance != null)
            {
                _currentGhostInstance.transform.position = memory.position;
                _currentGhostInstance.transform.rotation = memory.rotation;
                // You could also visualize inputDirection or jumped state here (e.g., change color)
            }

            lastMemoryTime = memory.timestamp;

            // Wait for the calculated time before processing the next memory
            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("PlayerMemoryReplayer: Replay finished.");
        StopReplay(); // Automatically stop replay when all memories are played
    }

    public bool IsReplaying => _isReplaying;
}
```