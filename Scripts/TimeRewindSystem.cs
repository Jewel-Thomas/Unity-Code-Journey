// Unity Design Pattern Example: TimeRewindSystem
// This script demonstrates the TimeRewindSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements the 'Time Rewind System' design pattern in Unity. This pattern allows for recording the state of game objects over time and then playing back or rewinding those states, effectively creating a temporal manipulation system often seen in games.

**Key Concepts of the Time Rewind System:**

1.  **State Snapshotting:** At regular intervals, the crucial data (position, rotation, etc.) of designated "rewindable" objects is saved into a historical buffer.
2.  **History Buffer:** Each rewindable object maintains its own list (or a circular buffer for optimization) of these historical states.
3.  **Global Time Cursor:** A central manager keeps a "global time index" that points to a specific moment in the shared timeline.
4.  **Rewind/Fast-Forward:** When the system is in rewind mode, instead of recording new states, the global time cursor moves backward or forward, and each rewindable object applies the state corresponding to that cursor from its history.
5.  **Timeline Branching:** If the user rewinds and then resumes normal play, any "future" states that were recorded *after* the point where the rewind stopped are discarded. New states are then recorded, effectively creating a new timeline branch.
6.  **Physics Handling:** For objects with Rigidbodies, their physics simulation (velocity, angular velocity, collisions) must be paused or disabled during rewind to prevent unwanted interactions.

---

**1. `IRewindable.cs`**
This interface defines the contract for any object that can be part of the Time Rewind System.

```csharp
using UnityEngine; // Required for MonoBehaviour casting if needed in manager, but generally not for interface itself.

/// <summary>
/// Interface for any object that can be rewound in time.
/// Implement this interface on your MonoBehaviour to make it reversible.
/// </summary>
public interface IRewindable
{
    /// <summary>
    /// Records the current state of the object and adds it to its history.
    /// The manager will ensure this is called at appropriate intervals.
    /// </summary>
    void RecordState();

    /// <summary>
    /// Applies a specific historical state to the object.
    /// The manager will provide the correct index based on the global time cursor.
    /// </summary>
    /// <param name="historyIndex">The index of the state to apply in its history buffer.</param>
    void ApplyState(int historyIndex);

    /// <summary>
    /// Returns the total number of states currently recorded for this object.
    /// Used by the manager to synchronize the global time index.
    /// </summary>
    int GetHistoryCount();

    /// <summary>
    /// Clears all recorded states that occurred after the specified index.
    /// This is used when new history is recorded after a rewind, effectively branching the timeline.
    /// </summary>
    /// <param name="index">The index *before* which states are preserved. States from (index + 1) onwards are removed.</param>
    void ClearFutureStates(int index);

    /// <summary>
    /// Called by the TimeRewindManager when rewind mode starts.
    /// Objects should typically disable normal update logic (e.g., movement scripts, physics simulation).
    /// </summary>
    void OnRewindModeStart();

    /// <summary>
    /// Called by the TimeRewindManager when rewind mode stops.
    /// Objects should re-enable normal update logic and resume normal behavior.
    /// </summary>
    void OnRewindModeEnd();
}

```

---

**2. `TimeRewindManager.cs`**
This is the core singleton component that orchestrates the entire system.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action, not strictly used here but good practice for events.

/// <summary>
/// The central manager for the Time Rewind System.
/// This MonoBehaviour should be placed once in your scene.
/// It orchestrates recording and applying states for all registered IRewindable objects.
/// </summary>
public class TimeRewindManager : MonoBehaviour
{
    // Singleton instance for easy access from other scripts
    public static TimeRewindManager Instance { get; private set; }

    [Header("Rewind Settings")]
    [Tooltip("The maximum number of states to store for each rewound object.")]
    [SerializeField] private int _historyCapacity = 500; // Roughly 10 seconds at 50 FixedUpdates/sec
    public int HistoryCapacity => _historyCapacity;

    [Tooltip("How often, in seconds, to record object states. Should ideally match Time.fixedDeltaTime for physics.")]
    [SerializeField] private float _recordInterval = 0.02f; // Matches FixedUpdate default
    public float RecordInterval => _recordInterval;

    [Tooltip("The speed multiplier for rewinding and fast-forwarding.")]
    [SerializeField] private float _rewindSpeedMultiplier = 2.0f;

    [Header("Debug/Runtime Info")]
    [Tooltip("Is the system currently in rewind/fast-forward mode?")]
    [SerializeField] private bool _isRewinding = false;
    public bool IsRewinding => _isRewinding;

    [Tooltip("The current global time index being applied across all objects.")]
    [SerializeField] private int _globalTimeIndex; // Points to the state currently being viewed/applied
    public int GlobalTimeIndex => _globalTimeIndex;

    private List<IRewindable> _reversibleObjects = new List<IRewindable>();
    private float _recordTimer; // Tracks time until next state recording

    // Tracks the maximum history count among all registered objects.
    // This is used to ensure _globalTimeIndex doesn't go beyond the shortest history,
    // which prevents errors if objects are registered at different times or have slightly different history sizes.
    private int _maxConsistentHistoryCount = 0; 

    void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TimeRewindManager: Multiple instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Warn if record interval doesn't match Fixed Timestep for physics consistency
        if (Mathf.Approximately(Time.fixedDeltaTime, _recordInterval) == false)
        {
            Debug.LogWarning($"TimeRewindManager: Record interval ({_recordInterval}s) does not match Fixed Timestep ({Time.fixedDeltaTime}s). " +
                             "For best consistency, especially with physics, these should be the same. " +
                             "Consider changing Fixed Timestep in Project Settings -> Time.");
        }
    }

    void OnDestroy()
    {
        // Clean up singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers an IRewindable object with the manager.
    /// This object will then have its state recorded and applied by the system.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    public void Register(IRewindable obj)
    {
        if (!_reversibleObjects.Contains(obj))
        {
            _reversibleObjects.Add(obj);
            // Record initial state immediately upon registration to ensure it has a starting point.
            obj.RecordState();
            Debug.Log($"TimeRewindManager: Registered {((MonoBehaviour)obj).name}");
            UpdateMaxConsistentHistoryCount(); // Recalculate max history after registration
        }
    }

    /// <summary>
    /// Unregisters an IRewindable object from the manager.
    /// It will no longer be affected by the time rewind system.
    /// </summary>
    /// <param name="obj">The object to unregister.</param>
    public void Unregister(IRewindable obj)
    {
        _reversibleObjects.Remove(obj);
        Debug.Log($"TimeRewindManager: Unregistered {((MonoBehaviour)obj).name}");
        UpdateMaxConsistentHistoryCount(); // Recalculate max history after unregistration
    }

    /// <summary>
    /// FixedUpdate is used for physics and consistent timing, making it ideal for state recording and application.
    /// </summary>
    void FixedUpdate()
    {
        if (_isRewinding)
        {
            // Handle rewind/fast-forward input and update the global time index
            float step = Time.fixedDeltaTime * _rewindSpeedMultiplier;
            if (Input.GetKey(KeyCode.R)) // Rewind
            {
                _globalTimeIndex = Mathf.FloorToInt(Mathf.Max(0, _globalTimeIndex - (step / _recordInterval)));
            }
            else if (Input.GetKey(KeyCode.F)) // Fast-Forward
            {
                // Clamp fast-forward to the maximum available history for all objects
                _globalTimeIndex = Mathf.FloorToInt(Mathf.Min(_maxConsistentHistoryCount - 1, _globalTimeIndex + (step / _recordInterval)));
            }
            // If neither R nor F is pressed, _globalTimeIndex remains unchanged, effectively pausing playback at that historical point.

            // Apply the state at the current global time index to all registered objects
            ApplyStatesToObjects();
        }
        else
        {
            // Normal play mode: record states periodically
            _recordTimer += Time.fixedDeltaTime;
            if (_recordTimer >= _recordInterval)
            {
                RecordStatesFromObjects();
                _recordTimer = 0f; // Reset timer for the next recording
            }
        }
    }

    /// <summary>
    /// Update is used for input that doesn't need to be tied to physics frames.
    /// </summary>
    void Update()
    {
        HandleRewindToggleInput();
    }

    /// <summary>
    /// Toggles the rewind mode on/off based on user input.
    /// </summary>
    private void HandleRewindToggleInput()
    {
        if (Input.GetKeyDown(KeyCode.T)) // 'T' for Toggle Rewind
        {
            if (!_isRewinding)
            {
                StartRewindMode();
            }
            else
            {
                StopRewindMode();
            }
        }
    }

    /// <summary>
    /// Instructs all registered objects to record their current state.
    /// Handles timeline branching if recording resumes after a rewind.
    /// </summary>
    private void RecordStatesFromObjects()
    {
        // If _globalTimeIndex is not at the end of the current timeline, it means we rewound
        // and are now recording new history. This requires discarding the "future" states.
        if (_globalTimeIndex < _maxConsistentHistoryCount - 1)
        {
            foreach (var obj in _reversibleObjects)
            {
                obj.ClearFutureStates(_globalTimeIndex);
            }
            // Update _maxConsistentHistoryCount to reflect the trimmed history.
            // It will be _globalTimeIndex + 1 because ClearFutureStates keeps up to _globalTimeIndex.
            _maxConsistentHistoryCount = _globalTimeIndex + 1; 
        }

        // Record a new state for each object
        foreach (var obj in _reversibleObjects)
        {
            obj.RecordState();
        }

        // Advance _globalTimeIndex to the newly recorded state (which is now the "present")
        // And update the consistent history count for proper clamping.
        _globalTimeIndex = _maxConsistentHistoryCount; 
        UpdateMaxConsistentHistoryCount();
    }

    /// <summary>
    /// Instructs all registered objects to apply the state corresponding to the _globalTimeIndex.
    /// </summary>
    private void ApplyStatesToObjects()
    {
        foreach (var obj in _reversibleObjects)
        {
            // Ensure the object has enough history for the current _globalTimeIndex
            // Clamping is necessary as some objects might have shorter histories (e.g., just spawned).
            int objectHistoryCount = obj.GetHistoryCount();
            if (objectHistoryCount > 0)
            {
                int clampedIndex = Mathf.Min(_globalTimeIndex, objectHistoryCount - 1);
                obj.ApplyState(clampedIndex);
            }
        }
    }

    /// <summary>
    /// Updates _maxConsistentHistoryCount by finding the minimum history count among all registered objects.
    /// This ensures _globalTimeIndex never points to a state that doesn't exist for all objects.
    /// </summary>
    private void UpdateMaxConsistentHistoryCount()
    {
        int minHistoryCount = _historyCapacity; // Start high, will be reduced by actual history counts
        if (_reversibleObjects.Count == 0)
        {
            _maxConsistentHistoryCount = 0;
            return;
        }

        foreach (var obj in _reversibleObjects)
        {
            minHistoryCount = Mathf.Min(minHistoryCount, obj.GetHistoryCount());
        }
        _maxConsistentHistoryCount = minHistoryCount;
    }

    /// <summary>
    /// Initiates rewind mode. Stops normal recording and prepares objects for state application.
    /// </summary>
    public void StartRewindMode()
    {
        if (_reversibleObjects.Count == 0)
        {
            Debug.LogWarning("TimeRewindManager: No reversible objects registered. Cannot start rewind mode.");
            return;
        }

        _isRewinding = true;
        // Set _globalTimeIndex to the last recorded state to start from the "present" moment.
        _globalTimeIndex = _maxConsistentHistoryCount - 1;
        _recordTimer = 0f; // Reset recording timer, as we are no longer recording.

        // Notify all objects that rewind mode has started
        foreach (var obj in _reversibleObjects)
        {
            obj.OnRewindModeStart();
        }
        Debug.Log("TimeRewindManager: Rewind mode STARTED. Press R to rewind, F to fast-forward. Release to pause.");
    }

    /// <summary>
    /// Stops rewind mode and returns to normal play. Objects resume their normal behavior.
    /// </summary>
    public void StopRewindMode()
    {
        _isRewinding = false;
        // When stopping rewind, the _globalTimeIndex remains at the last viewed historical point.
        // The `RecordStatesFromObjects` method will handle clearing future states if new recording starts from here.

        // Notify all objects that rewind mode has stopped
        foreach (var obj in _reversibleObjects)
        {
            obj.OnRewindModeEnd();
        }
        Debug.Log("TimeRewindManager: Rewind mode STOPPED. Resuming normal gameplay.");
    }
}
```

---

**3. `RewindableTransform.cs`**
A concrete example implementation of `IRewindable` that saves and applies the `Transform`'s position, rotation, and active state.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A concrete example of an IRewindable object.
/// This script makes a GameObject's Transform (position, rotation, active state) reversible.
/// Attach this to any GameObject you want to be able to rewind.
/// </summary>
public class RewindableTransform : MonoBehaviour, IRewindable
{
    // --- State Struct ---
    /// <summary>
    /// Represents the state of a Transform at a specific point in time.
    /// Using a struct for value-type semantics and efficiency in storage.
    /// </summary>
    private struct TransformState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsActive; // Example: can also store active state of the GameObject
    }

    // Stores the history of recorded TransformStates for this object.
    [Tooltip("The history of recorded TransformStates for this object.")]
    [SerializeField] private List<TransformState> _history = new List<TransformState>();

    private Rigidbody _rigidbody; // Optional: reference to Rigidbody for handling physics objects
    private bool _originalIsKinematic; // Stores original Rigidbody setting before rewind
    private bool _originalDetectCollisions; // Stores original Rigidbody collision setting

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        // Register with the manager when this component is enabled.
        // This makes the object part of the Time Rewind System.
        if (TimeRewindManager.Instance != null)
        {
            TimeRewindManager.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning($"RewindableTransform on '{name}': No TimeRewindManager found in scene. " +
                             "This object will not be reversible. Please add a TimeRewindManager GameObject.");
            enabled = false; // Disable this component if no manager is present
        }
    }

    void OnDisable()
    {
        // Unregister from the manager when this component is disabled or the GameObject is destroyed.
        if (TimeRewindManager.Instance != null)
        {
            TimeRewindManager.Instance.Unregister(this);
        }
    }

    /// <summary>
    /// Records the current position, rotation, and active state of the GameObject.
    /// </summary>
    public void RecordState()
    {
        // Create a new state snapshot
        TransformState currentState = new TransformState
        {
            Position = transform.position,
            Rotation = transform.rotation,
            IsActive = gameObject.activeSelf
        };

        // Add to history list.
        _history.Add(currentState);

        // Ensure the history capacity is respected by removing the oldest state if over capacity.
        if (_history.Count > TimeRewindManager.Instance.HistoryCapacity)
        {
            _history.RemoveAt(0); // Remove the oldest state
        }
    }

    /// <summary>
    /// Applies a specific historical state to the GameObject's transform.
    /// </summary>
    /// <param name="historyIndex">The index of the state to apply from this object's history.</param>
    public void ApplyState(int historyIndex)
    {
        // Validate the index to prevent errors if history is shorter than expected
        if (historyIndex >= 0 && historyIndex < _history.Count)
        {
            TransformState stateToApply = _history[historyIndex];
            transform.position = stateToApply.Position;
            transform.rotation = stateToApply.Rotation;
            gameObject.SetActive(stateToApply.IsActive);

            // For Rigidbody objects, stop their physics simulation to prevent interference
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // This warning can be useful during debugging if history counts are inconsistent.
            // In a robust system, the manager's _maxConsistentHistoryCount should prevent _globalTimeIndex
            // from going past the minimum history available.
            // Debug.LogWarning($"RewindableTransform ({name}): Attempted to apply state at invalid index {historyIndex}. History count: {_history.Count}");
        }
    }

    /// <summary>
    /// Returns the number of recorded states in this object's history.
    /// </summary>
    public int GetHistoryCount()
    {
        return _history.Count;
    }

    /// <summary>
    /// Clears any recorded states that are considered "future" relative to the given index.
    /// This happens when the game resumes normal play after a rewind, creating a new timeline.
    /// </summary>
    /// <param name="index">The index *before* which states are preserved. States from (index + 1) onwards are removed.</param>
    public void ClearFutureStates(int index)
    {
        // If the provided index is valid and there are states after it, remove them.
        if (index >= 0 && index < _history.Count - 1)
        {
            int statesToRemove = _history.Count - (index + 1);
            if (statesToRemove > 0)
            {
                _history.RemoveRange(index + 1, statesToRemove);
            }
        }
    }

    /// <summary>
    /// Called when rewind mode starts.
    /// For physics objects, this disables their simulation to allow direct transform manipulation.
    /// </summary>
    public void OnRewindModeStart()
    {
        if (_rigidbody != null)
        {
            _originalIsKinematic = _rigidbody.isKinematic; // Save original state
            _originalDetectCollisions = _rigidbody.detectCollisions; // Save original collision state

            _rigidbody.isKinematic = true; // Disable physics simulation
            _rigidbody.detectCollisions = false; // Optionally disable collisions
        }
    }

    /// <summary>
    /// Called when rewind mode stops.
    /// Re-enables physics simulation for physics objects.
    /// </summary>
    public void OnRewindModeEnd()
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = _originalIsKinematic; // Restore original state
            _rigidbody.detectCollisions = _originalDetectCollisions; // Restore original collision state
            // If the object was active and not kinematic originally, apply an impulse or similar if needed
            // to "kickstart" physics after being paused. For now, it will just resume with zero velocity.
        }
    }
}
```

---

**4. `SimpleMover.cs` (Example for Demonstration)**
This script provides basic movement for a GameObject so you can observe the rewind effect.

```csharp
using UnityEngine;

/// <summary>
/// A simple script to make a GameObject move and rotate.
/// Used to demonstrate the Time Rewind System.
/// </summary>
public class SimpleMover : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private float _rotateSpeed = 50.0f;
    [SerializeField] private Vector3 _moveDirection = Vector3.forward;

    void Update()
    {
        // Only allow movement if the TimeRewindManager exists and is NOT in rewind mode.
        // This prevents the object from moving on its own while you're trying to rewind it.
        if (TimeRewindManager.Instance == null || !TimeRewindManager.Instance.IsRewinding)
        {
            // Move the object
            transform.Translate(_moveDirection * _moveSpeed * Time.deltaTime, Space.Self);

            // Rotate the object
            transform.Rotate(Vector3.up * _rotateSpeed * Time.deltaTime, Space.Self);

            // Optional: Reverse direction when reaching a boundary (for interesting demo)
            if (transform.position.z > 5 || transform.position.z < -5)
            {
                _moveDirection *= -1;
            }
        }
    }
}
```

---

### **How to Use in Unity (Example Setup):**

1.  **Create an Empty GameObject for the Manager:**
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
    *   Rename this GameObject to "TimeRewindManager".
    *   Attach the `TimeRewindManager.cs` script to it.
    *   In the Inspector, you can adjust `History Capacity` (how many states to store), `Record Interval` (how often to save states), and `Rewind Speed Multiplier`. For physics consistency, ensure `Record Interval` matches `Project Settings -> Time -> Fixed Timestep` (default 0.02s).

2.  **Create a Rewindable Object (e.g., a moving cube):**
    *   Create a 3D Cube (GameObject -> 3D Object -> Cube).
    *   Attach the `RewindableTransform.cs` script to this Cube.
    *   Attach the `SimpleMover.cs` script to this Cube. (You can also add a `Rigidbody` if you want to test physics interaction, the system will handle pausing it during rewind).

3.  **Run the Scene:**
    *   The Cube will start moving and rotating automatically via the `SimpleMover` script.
    *   **Press `T` (Toggle):** This will activate the rewind mode. The Cube's normal movement will stop.
    *   **While in rewind mode (after pressing `T`):**
        *   **Press and hold `R`:** The Cube will move backward through its recorded history.
        *   **Press and hold `F`:** The Cube will move forward through its recorded history.
        *   **Release `R` or `F`:** The Cube will pause at that specific moment in time.
    *   **Press `T` again:** This will exit rewind mode. The Cube will resume its normal movement from its *current* historical position. Notice that if you had rewound, any "future" states are now discarded, and the Cube starts a new timeline branch from that point.

This setup provides a complete, practical, and educational demonstration of the Time Rewind System design pattern in Unity. You can extend `RewindableTransform` to save other properties (e.g., scale, color, custom script variables) or create new `IRewindable` implementations for different types of objects (e.g., `RewindableHealth` for health changes).