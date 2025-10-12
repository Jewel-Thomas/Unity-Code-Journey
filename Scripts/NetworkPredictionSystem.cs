// Unity Design Pattern Example: NetworkPredictionSystem
// This script demonstrates the NetworkPredictionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `NetworkPredictionSystem` is a design pattern commonly used in networked games, especially for fast-paced action where perceived latency is a critical issue. It allows a client to immediately react to player input by *predicting* what the game state will be, making the game feel responsive. Simultaneously, the client sends its input to the authoritative server. When the server processes the input and sends back its true, authoritative state, the client *reconciles* its predicted state with the server's truth. If there's a discrepancy, the client corrects its local state and re-simulates any subsequent predicted inputs from that corrected point.

This example provides a complete C# Unity script demonstrating this pattern with a simple player movement scenario.

---

### NetworkPredictionSystem Design Pattern Explained

1.  **Client-Side Prediction:**
    *   The client, immediately upon receiving input (e.g., WASD keys pressed), locally simulates the effect of that input on its own game state (e.g., moves the player character).
    *   This provides instant feedback to the player, masking network latency.
    *   The client needs to store these inputs and the predicted states for each game "tick" or "frame".

2.  **Input Buffering and Sending:**
    *   The client continuously generates inputs and buffers them.
    *   These buffered inputs are periodically sent to the server. Due to network latency, the server will receive these inputs with a delay.

3.  **Authoritative Server Simulation:**
    *   The server receives client inputs (delayed) and processes them against its own, authoritative game state.
    *   The server's state is the "truth" of the game world.
    *   Periodically, the server sends snapshots of its authoritative game state (including the tick for which the state is valid) back to the clients. These also experience network latency.

4.  **Server Reconciliation (on Client):**
    *   When the client receives an authoritative state snapshot from the server, it compares this state to its own *previously predicted state for that same server tick*.
    *   **If there's a match:** Great! The client's prediction was accurate. It can safely discard older buffered inputs and states.
    *   **If there's a discrepancy (misprediction):**
        *   The client "rolls back" its local game state to the authoritative server state received.
        *   It then "re-simulates" all player inputs that occurred *after* that authoritative server tick, applying them one by one to the rolled-back state.
        *   This corrects the client's position to align with the server's truth, while still applying all inputs the player has made since that server tick. This might cause a visible "snap" or "jerk" if the discrepancy was large.

5.  **Interpolation (Optional but Recommended for Smoothness):**
    *   While not explicitly implemented in this basic example, in a real game, you would often interpolate between the previous reconciled state and the current predicted state to smooth out the visual corrections during reconciliation, making snaps less jarring.

---

### Complete C# Unity Example Script

This script can be dropped directly into a Unity project. It simulates both a client and a server within the same application, along with configurable network latency, to demonstrate the prediction and reconciliation process.

**To use this example:**

1.  Create a new C# script named `NetworkPredictionSystem` in your Unity project and paste the code below into it.
2.  Create two simple 3D Cube GameObjects (or any other visual) in your scene.
3.  Drag these two cubes into your Project window to make them prefabs. Name one `ClientPrefab` and the other `ServerPrefab`.
4.  Create an empty GameObject in your scene and name it `PredictionManager`.
5.  Attach the `NetworkPredictionSystem` script to the `PredictionManager` GameObject.
6.  In the Inspector, drag your `ClientPrefab` into the `Client Prefab` slot and your `ServerPrefab` into the `Server Prefab` slot on the `NetworkPredictionSystem` component.
7.  Adjust `Network Latency Ms` (e.g., to 200ms or 300ms) to clearly observe the prediction and reconciliation effects.
8.  Run the scene.
9.  Use **WASD** keys to move the blue client-predicted player.
10. Observe how the blue cube moves instantly (prediction), while the red cube (server-authoritative) moves with a delay. When latency is high, you'll see the blue cube occasionally "snap" or "correct" its position to match the server's authoritative path.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Required for LINQ extensions like .Where() and .ToDictionary()

/// <summary>
/// Represents player input for a specific game tick.
/// This structure should be lightweight and easily serializable for network transmission.
/// </summary>
public struct PlayerInput
{
    public int Tick;             // The tick at which this input was generated/processed.
    public Vector3 MoveDirection; // The direction of movement (normalized).

    public PlayerInput(int tick, Vector3 moveDirection)
    {
        Tick = tick;
        MoveDirection = moveDirection;
    }

    public override string ToString()
    {
        return $"Input [T:{Tick}, Dir:{MoveDirection}]";
    }
}

/// <summary>
/// Represents the state of a player at a given game tick.
/// This structure must contain all relevant properties that need to be predicted and reconciled.
/// It should also be lightweight and easily serializable.
/// </summary>
public struct PredictedPlayerState
{
    public int Tick;
    public Vector3 Position;
    public Vector3 Velocity; // For more complex physics/movement, could include rotation, health, etc.

    public PredictedPlayerState(int tick, Vector3 position, Vector3 velocity)
    {
        Tick = tick;
        Position = position;
        Velocity = velocity;
    }

    /// <summary>
    /// Checks if two states are approximately equal. Essential for reconciliation to detect discrepancies.
    /// Using a small epsilon for floating-point comparisons is crucial.
    /// </summary>
    public bool ApproximatelyEquals(PredictedPlayerState other)
    {
        // Compare positions, potentially velocities within a small epsilon
        return Tick == other.Tick &&
               Vector3.Distance(Position, other.Position) < 0.001f; // Threshold for position difference
               // (Optional) Add checks for other properties like Velocity if they're predicted.
    }

    public override string ToString()
    {
        return $"State [T:{Tick}, Pos:{Position.x:F2}, Vel:{Velocity.magnitude:F2}]";
    }
}

/// <summary>
/// Simulates the authoritative game server.
/// It processes client inputs and maintains the true, authoritative game state.
/// </summary>
public class SimulatedServer
{
    private PredictedPlayerState _authoritativeState;
    private int _currentServerTick;
    private float _playerSpeed;
    private float _fixedDeltaTime; // The fixed time step for server tick calculations.

    // A queue for inputs received from the client, simulating network delay for input.
    // In a real game, this would involve network packet reception and deserialization.
    private Queue<PlayerInput> _pendingClientInputs = new Queue<PlayerInput>();

    public SimulatedServer(Vector3 initialPosition, float playerSpeed, float fixedDeltaTime)
    {
        _authoritativeState = new PredictedPlayerState(0, initialPosition, Vector3.zero);
        _currentServerTick = 0;
        _playerSpeed = playerSpeed;
        _fixedDeltaTime = fixedDeltaTime;
        Debug.Log($"<color=red>Server:</color> Initialized at Position: {initialPosition}");
    }

    /// <summary>
    /// Processes a batch of inputs from the client.
    /// In a real network, this would be triggered by a network message.
    /// </summary>
    public void ReceiveClientInputs(List<PlayerInput> inputs)
    {
        foreach (var input in inputs)
        {
            _pendingClientInputs.Enqueue(input);
        }
    }

    /// <summary>
    /// Advances the server's authoritative game state by one tick.
    /// This method is called repeatedly by `FixedUpdate` in the `NetworkPredictionSystem`.
    /// </summary>
    public void ServerTick()
    {
        _currentServerTick++;

        // Process any pending client inputs whose tick is relevant to or before the current server tick.
        // In a more complex system, inputs might be processed against a specific buffered state.
        while (_pendingClientInputs.Any() && _pendingClientInputs.Peek().Tick <= _currentServerTick)
        {
            PlayerInput input = _pendingClientInputs.Dequeue();
            ApplyInputToState(ref _authoritativeState, input, _fixedDeltaTime);
            // The server's authoritative state should represent the current server tick.
            // If processing a client's historical input, the state still advances.
        }

        // Ensure the authoritative state's tick always matches the current server tick.
        // This is important for reconciliation on the client, as the server snapshot's tick
        // tells the client what historical state to compare against.
        _authoritativeState.Tick = _currentServerTick;
    }

    /// <summary>
    /// Gets the current authoritative state of the player.
    /// This state is sent to clients for reconciliation.
    /// </summary>
    public PredictedPlayerState GetAuthoritativeState()
    {
        return _authoritativeState;
    }

    /// <summary>
    /// Applies an input to a player state. This core movement logic is shared
    /// between the client (for prediction) and the server (for authoritative simulation)
    /// to ensure consistency.
    /// </summary>
    private void ApplyInputToState(ref PredictedPlayerState state, PlayerInput input, float deltaTime)
    {
        Vector3 movement = input.MoveDirection * _playerSpeed * deltaTime;
        state.Position += movement;
        state.Velocity = movement / deltaTime; // Calculate velocity based on movement
    }
}

/// <summary>
/// Manages client-side prediction and server reconciliation for a player.
/// This runs on the client machine.
/// </summary>
public class PredictedClient
{
    private PredictedPlayerState _currentPredictedState;
    private int _currentClientTick;
    private int _lastAuthoritativeServerTick; // The latest tick for which we received and reconciled a server update.
    private float _playerSpeed;
    private float _fixedDeltaTime; // The fixed time step for client prediction calculations.

    // Buffers for prediction and reconciliation:
    // 1. _clientInputsSentToServer: Stores historical inputs that have been sent to the server.
    //    Needed for re-simulation during reconciliation.
    private Dictionary<int, PlayerInput> _clientInputsSentToServer = new Dictionary<int, PlayerInput>();
    // 2. _clientPredictedStates: Stores historical predicted states corresponding to each input tick.
    //    Needed to compare against incoming authoritative server states.
    private Dictionary<int, PredictedPlayerState> _clientPredictedStates = new Dictionary<int, PredictedPlayerState>();

    public PredictedClient(Vector3 initialPosition, float playerSpeed, float fixedDeltaTime)
    {
        _currentPredictedState = new PredictedPlayerState(0, initialPosition, Vector3.zero);
        _currentClientTick = 0;
        _lastAuthoritativeServerTick = -1; // No server updates processed yet.
        _playerSpeed = playerSpeed;
        _fixedDeltaTime = fixedDeltaTime;
        Debug.Log($"<color=blue>Client:</color> Initialized at Position: {initialPosition}");
    }

    /// <summary>
    /// Generates player input for the current client tick based on user input (e.g., keyboard).
    /// This input is immediately used for local prediction and then sent to the server.
    /// </summary>
    public PlayerInput GenerateInput()
    {
        _currentClientTick++; // Advance client's local tick counter.
        Vector3 moveDirection = Vector3.zero;

        // Capture user input
        if (Input.GetKey(KeyCode.A)) moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D)) moveDirection += Vector3.right;
        if (Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection += Vector3.back;

        moveDirection.Normalize(); // Ensure consistent speed for diagonal movement.

        PlayerInput input = new PlayerInput(_currentClientTick, moveDirection);
        _clientInputsSentToServer[input.Tick] = input; // Store input for potential re-simulation.
        return input;
    }

    /// <summary>
    /// Locally predicts the player's movement based on the generated input.
    /// This provides immediate feedback to the player.
    /// </summary>
    public void PredictLocally(PlayerInput input)
    {
        ApplyInputToState(ref _currentPredictedState, input, _fixedDeltaTime);
        _currentPredictedState.Tick = input.Tick; // Update predicted state's tick to match the input.
        _clientPredictedStates[input.Tick] = _currentPredictedState; // Store this predicted state for reconciliation.
    }

    /// <summary>
    /// Reconciles the client's predicted state with the authoritative state received from the server.
    /// This is the core of the Network Prediction System.
    /// </summary>
    /// <param name="authoritativeState">The true state received from the server.</param>
    /// <returns>True if a discrepancy was found and reconciliation occurred, false otherwise.</returns>
    public bool Reconcile(PredictedPlayerState authoritativeState)
    {
        // Only reconcile if this authoritative state is newer than what we've already processed.
        if (authoritativeState.Tick <= _lastAuthoritativeServerTick)
        {
            return false;
        }

        _lastAuthoritativeServerTick = authoritativeState.Tick;
        bool discrepancyFound = false;

        // Try to retrieve the client's predicted state for the server's authoritative tick.
        if (_clientPredictedStates.TryGetValue(authoritativeState.Tick, out PredictedPlayerState predictedStateAtServerTick))
        {
            // Compare the predicted state with the authoritative state.
            if (!predictedStateAtServerTick.ApproximatelyEquals(authoritativeState))
            {
                discrepancyFound = true;
                Debug.LogWarning($"<color=blue>Client:</color> Discrepancy found at tick {authoritativeState.Tick}! " +
                                 $"Client Predicted: {predictedStateAtServerTick.Position.x:F2} " +
                                 $"Server Authoritative: {authoritativeState.Position.x:F2}");

                // --- Rollback and Re-simulate ---
                // 1. Rollback: Set the client's current predicted state to the authoritative server state.
                _currentPredictedState = authoritativeState;

                // 2. Re-simulate: Apply all subsequent client inputs (from the tick *after* the authoritative state
                //    up to the current client tick) on top of the corrected state.
                for (int tickToReSimulate = authoritativeState.Tick + 1; tickToReSimulate <= _currentClientTick; tickToReSimulate++)
                {
                    if (_clientInputsSentToServer.TryGetValue(tickToReSimulate, out PlayerInput inputToReSimulate))
                    {
                        ApplyInputToState(ref _currentPredictedState, inputToReSimulate, _fixedDeltaTime);
                        _currentPredictedState.Tick = inputToReSimulate.Tick;
                        // Update the historical predicted state with the newly re-simulated state.
                        _clientPredictedStates[inputToReSimulate.Tick] = _currentPredictedState;
                    }
                    else
                    {
                        // This case indicates a potential issue (e.g., input was not buffered or lost).
                        Debug.LogError($"<color=blue>Client:</color> Missing input for re-simulation at tick {tickToReSimulate}! Cannot fully reconcile.");
                        break;
                    }
                }
                Debug.Log($"<color=blue>Client:</color> Reconciled. New predicted position: {_currentPredictedState.Position.x:F2}");
            }
        }
        else
        {
            // If the client doesn't have a predicted state for the server's tick, it might mean:
            //   - The server's update is significantly older than the client's current prediction (rare, or client joined late).
            //   - The client has missed many frames/ticks and its buffer is empty for that tick.
            // In such cases, the safest approach is to directly apply the authoritative state.
            Debug.LogWarning($"<color=blue>Client:</color> Missing predicted state for server tick {authoritativeState.Tick}. Directly applying server state.");
            _currentPredictedState = authoritativeState;
            discrepancyFound = true;
        }

        // Clean up old buffered inputs and states to prevent memory accumulation.
        CleanupBuffers(authoritativeState.Tick);
        return discrepancyFound;
    }

    /// <summary>
    /// Gets the client's current predicted player state. This is what the player sees.
    /// </summary>
    public PredictedPlayerState GetPredictedState()
    {
        return _currentPredictedState;
    }

    /// <summary>
    /// Applies an input to a player state (shared logic).
    /// </summary>
    private void ApplyInputToState(ref PredictedPlayerState state, PlayerInput input, float deltaTime)
    {
        Vector3 movement = input.MoveDirection * _playerSpeed * deltaTime;
        state.Position += movement;
        state.Velocity = movement / deltaTime;
    }

    /// <summary>
    /// Removes old inputs and predicted states from buffers.
    /// Any data older than the last reconciled tick is no longer needed.
    /// </summary>
    private void CleanupBuffers(int upToTick)
    {
        // Remove inputs and states that are older than or equal to the reconciled tick.
        _clientInputsSentToServer = _clientInputsSentToServer
            .Where(pair => pair.Key >= upToTick)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        _clientPredictedStates = _clientPredictedStates
            .Where(pair => pair.Key >= upToTick)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}

/// <summary>
/// Main Unity component demonstrating the Network Prediction System pattern.
/// It simulates both a client and a server within the same application,
/// managing their ticks, input sending, and state reconciliation with simulated network latency.
/// </summary>
public class NetworkPredictionSystem : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Tooltip("The shared tick rate (Hz) for both client and server simulations.")]
    [Range(10, 120)]
    public float SharedTickRate = 60f;
    [Tooltip("Simulated round-trip network latency in milliseconds. Affects how old server updates are.")]
    [Range(0, 500)]
    public float NetworkLatencyMs = 100f;
    [Tooltip("The speed at which the player moves in units per second.")]
    public float PlayerSpeed = 5f;

    [Header("Visuals")]
    [Tooltip("Prefab for the client-predicted player (blue). Shows client-side prediction.")]
    public GameObject ClientPrefab;
    [Tooltip("Prefab for the server-authoritative player (red). Shows the true server position.")]
    public GameObject ServerPrefab;

    private SimulatedServer _server;
    private PredictedClient _client;

    private GameObject _clientVisual; // Visual representation of the client's predicted player
    private GameObject _serverVisual; // Visual representation of the server's authoritative player

    private float _fixedDeltaTime; // Calculated from SharedTickRate, used for all simulation steps

    // Queues to simulate network packet travel time for inputs (client to server) and states (server to client).
    // Each item is a tuple: (data, simulatedArrivalTime)
    private Queue<(List<PlayerInput> inputs, float arrivalTime)> _clientToServerQueue = new Queue<(List<PlayerInput>, float)>();
    private Queue<(PredictedPlayerState state, float arrivalTime)> _serverToClientQueue = new Queue<(PredictedPlayerState, float)>();

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        if (ClientPrefab == null || ServerPrefab == null)
        {
            Debug.LogError("ClientPrefab and ServerPrefab must be assigned in the Inspector.");
            enabled = false;
            return;
        }

        // Calculate the fixed delta time based on the desired tick rate.
        _fixedDeltaTime = 1f / SharedTickRate;
        // Set Unity's FixedUpdate rate to match our simulation tick rate for consistency.
        Time.fixedDeltaTime = _fixedDeltaTime;

        // Initialize client and server at slightly different starting positions for visual clarity.
        Vector3 initialClientPos = new Vector3(-2, 0.5f, 0);
        Vector3 initialServerPos = new Vector3(2, 0.5f, 0);

        _server = new SimulatedServer(initialServerPos, PlayerSpeed, _fixedDeltaTime);
        _client = new PredictedClient(initialClientPos, PlayerSpeed, _fixedDeltaTime);

        // Instantiate and configure visual game objects.
        _clientVisual = Instantiate(ClientPrefab, initialClientPos, Quaternion.identity);
        _clientVisual.name = "Client Predicted Player (Blue)";
        // Ensure the visual has a Material property to change color.
        Renderer clientRenderer = _clientVisual.GetComponent<Renderer>();
        if (clientRenderer != null) clientRenderer.material.color = Color.blue;
        else Debug.LogWarning("ClientPrefab has no Renderer component. Cannot set color.");


        _serverVisual = Instantiate(ServerPrefab, initialServerPos, Quaternion.identity);
        _serverVisual.name = "Server Authoritative Player (Red)";
        Renderer serverRenderer = _serverVisual.GetComponent<Renderer>();
        if (serverRenderer != null) serverRenderer.material.color = Color.red;
        else Debug.LogWarning("ServerPrefab has no Renderer component. Cannot set color.");

        // Add kinematic Rigidbodies to visuals. This allows them to interact with physics
        // if desired, but setting isKinematic=true ensures their movement is controlled
        // solely by script (transform.position) and not Unity's physics engine.
        if (_clientVisual.GetComponent<Rigidbody>() == null) _clientVisual.AddComponent<Rigidbody>().isKinematic = true;
        if (_serverVisual.GetComponent<Rigidbody>() == null) _serverVisual.AddComponent<Rigidbody>().isKinematic = true;
    }

    /// <summary>
    /// Update is called once per frame. Used here for smoothing visual updates.
    /// While game logic runs in FixedUpdate, visual objects can be updated here
    /// for smoother animation between fixed ticks, avoiding choppy movement.
    /// </summary>
    void Update()
    {
        // Update visual positions based on the current predicted/authoritative states.
        // This makes movement appear smoother even if game logic runs at a lower fixed tick rate.
        _clientVisual.transform.position = _client.GetPredictedState().Position;
        _serverVisual.transform.position = _server.GetAuthoritativeState().Position;
    }

    /// <summary>
    /// FixedUpdate is called at a fixed interval defined by Time.fixedDeltaTime.
    /// This is where all deterministic game logic, including client prediction and server simulation, should run.
    /// </summary>
    void FixedUpdate()
    {
        // 1. Advance Client Tick:
        //    Generate input, predict locally, and enqueue input for server.
        ClientTick();

        // 2. Advance Server Tick:
        //    Process any arrived client inputs, advance authoritative state, and enqueue state for client.
        ServerTick();

        // 3. Process Network Queues:
        //    Deliver simulated network messages and trigger client reconciliation.
        ProcessNetworkQueues();
    }

    // --- Simulation Logic ---

    /// <summary>
    /// Executes one client-side tick. This involves:
    /// 1. Generating player input.
    /// 2. Applying this input immediately for local prediction.
    /// 3. Enqueueing the input to be sent to the simulated server with latency.
    /// </summary>
    private void ClientTick()
    {
        PlayerInput input = _client.GenerateInput(); // Client generates its input for the current tick.
        _client.PredictLocally(input);              // Client immediately applies the input for local prediction.

        // Simulate sending this input to the server.
        // In a real network, inputs might be batched over several ticks before sending.
        // Here, for simplicity, we send one input per tick wrapped in a list.
        // We use NetworkLatencyMs / 2000f because NetworkLatencyMs is RTT, so half is one-way trip.
        _clientToServerQueue.Enqueue((new List<PlayerInput> { input }, Time.time + NetworkLatencyMs / 2000f));
    }

    /// <summary>
    /// Executes one server-side tick. This involves:
    /// 1. Processing any client inputs that have "arrived" at the server.
    /// 2. Advancing the server's authoritative game state.
    /// 3. Enqueueing the updated authoritative state to be sent back to the client with latency.
    /// </summary>
    private void ServerTick()
    {
        // Deliver client inputs that have reached the server based on simulated latency.
        while (_clientToServerQueue.Any() && _clientToServerQueue.Peek().arrivalTime <= Time.time)
        {
            var (inputs, _) = _clientToServerQueue.Dequeue();
            _server.ReceiveClientInputs(inputs);
        }

        // Advance the server's authoritative state by one tick.
        _server.ServerTick();

        // Simulate sending the authoritative state back to the client.
        PredictedPlayerState authoritativeState = _server.GetAuthoritativeState();
        _serverToClientQueue.Enqueue((authoritativeState, Time.time + NetworkLatencyMs / 2000f));
    }

    /// <summary>
    /// Processes incoming simulated network messages for the client.
    /// This primarily involves delivering authoritative states from the server to the client for reconciliation.
    /// </summary>
    private void ProcessNetworkQueues()
    {
        // Deliver authoritative server states that have reached the client based on simulated latency.
        while (_serverToClientQueue.Any() && _serverToClientQueue.Peek().arrivalTime <= Time.time)
        {
            var (state, _) = _serverToClientQueue.Dequeue();
            _client.Reconcile(state); // Client attempts to reconcile its state with the server's truth.
        }
    }

    // --- Helper Methods (for demonstrating the pattern and debugging) ---

    /// <summary>
    /// Displays debugging information on the screen.
    /// </summary>
    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.color = Color.black; // Make text visible against light background
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label($"Shared Tick Rate: {SharedTickRate} Hz");
        GUILayout.Label($"Simulated Latency: {NetworkLatencyMs} ms");
        GUILayout.Label($"Client Predicted Pos: {_client.GetPredictedState().Position.x:F2}");
        GUILayout.Label($"Server Authoritative Pos: {_server.GetAuthoritativeState().Position.x:F2}");
        GUILayout.Label($"Client Tick: {_client.GetPredictedState().Tick}");
        GUILayout.Label($"Server Tick: {_server.GetAuthoritativeState().Tick}");
        GUILayout.EndArea();

        GUI.skin.label.fontSize = 15;
        GUI.color = Color.white; // White text for instructions at the bottom
        GUI.Label(new Rect(10, Screen.height - 30, 500, 25), "Use WASD to move the Client Predicted Player (Blue).");
    }
}
```