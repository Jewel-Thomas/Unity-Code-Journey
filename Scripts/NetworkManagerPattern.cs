// Unity Design Pattern Example: NetworkManagerPattern
// This script demonstrates the NetworkManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'NetworkManagerPattern' is a fundamental design pattern in game development, especially when dealing with online functionalities. It centralizes all network-related logic into a single, dedicated class. This promotes modularity, reusability, and makes it easier to manage, test, and extend network features.

**Why use the NetworkManagerPattern?**

*   **Centralization:** All network code (connection, disconnection, sending/receiving data, player management, error handling) lives in one place.
*   **Decoupling:** Game logic doesn't need to know the intricate details of network protocols. It just asks the `NetworkManager` to send or receive data.
*   **Reusability:** The `NetworkManager` can be easily reused across different parts of your game or even different projects.
*   **Maintainability:** Changes or updates to network functionality only need to be made in one script.
*   **Testability:** You can easily mock or simulate network behavior within the `NetworkManager` for testing purposes.
*   **Singleton Access:** Often implemented as a Singleton, allowing any part of the game to easily access network functions.

Below is a complete Unity C# script demonstrating the NetworkManagerPattern. It simulates network operations using `Debug.Log` and coroutines to represent asynchronous tasks, making it practical for understanding the structure without requiring a full networking backend.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///     The NetworkManagerPattern centralizes all network-related logic into a single, dedicated class.
///     This promotes modularity, reusability, and makes it easier to manage, test, and extend network features.
///
///     This implementation uses the Singleton pattern to provide easy global access to the NetworkManager instance.
///     It simulates network operations (connecting, disconnecting, sending messages, managing players)
///     using Debug.Log and coroutines for asynchronous behavior, demonstrating the pattern's structure
///     without requiring an actual networking backend (like Unity Netcode for GameObjects, Mirror, Photon, etc.).
/// </summary>
public class NetworkManager : MonoBehaviour
{
    // ====================================================================================================
    // 1. Singleton Instance
    //    Provides a globally accessible single instance of the NetworkManager.
    // ====================================================================================================

    /// <summary>
    /// The static instance of the NetworkManager, accessible from anywhere.
    /// </summary>
    public static NetworkManager Instance { get; private set; }

    // ====================================================================================================
    // 2. Events/Callbacks
    //    Allows other scripts to subscribe to network events without directly polling or interacting
    //    with the NetworkManager's internal state. This is a key part of the pattern for decoupling.
    // ====================================================================================================

    /// <summary>
    /// Event fired when the client successfully connects to the server.
    /// </summary>
    public event Action OnConnectedToServer;

    /// <summary>
    /// Event fired when the client disconnects from the server (either intentionally or due to an error).
    /// </summary>
    public event Action OnDisconnectedFromServer;

    /// <summary>
    /// Event fired when a message is received from the server.
    /// Passes the received message string.
    /// </summary>
    public event Action<string> OnMessageReceived;

    /// <summary>
    /// Event fired when a new player joins the game.
    /// Passes the ID of the joined player.
    /// </summary>
    public event Action<int> OnPlayerJoined;

    /// <summary>
    /// Event fired when an existing player leaves the game.
    /// Passes the ID of the left player.
    /// </summary>
    public event Action<int> OnPlayerLeft;

    /// <summary>
    /// Event fired when there's a network error.
    /// Passes a descriptive error message.
    /// </summary>
    public event Action<string> OnNetworkError;

    // ====================================================================================================
    // 3. Configuration & Internal State
    //    Private fields to hold the NetworkManager's current state and configurable settings.
    // ====================================================================================================

    [Header("Network Settings (Simulated)")]
    [SerializeField] private string serverAddress = "127.0.0.1"; // The address of the server to connect to.
    [SerializeField] private int serverPort = 7777;             // The port of the server to connect to.
    [SerializeField] private float connectionDelay = 2.0f;      // Simulated delay for connection.

    private bool _isConnected = false;                  // Current connection status.
    private int _localPlayerId = -1;                    // The ID assigned to the local player by the server.
    private List<int> _connectedPlayerIds = new List<int>(); // List of all currently connected player IDs.

    // ====================================================================================================
    // 4. Public Properties
    //    Read-only access to important network state variables.
    // ====================================================================================================

    /// <summary>
    /// Indicates whether the client is currently connected to the server.
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// The unique ID assigned to the local player by the server. Returns -1 if not connected.
    /// </summary>
    public int LocalPlayerId => _localPlayerId;

    /// <summary>
    /// A read-only list of IDs for all players currently connected to the server (including local player).
    /// </summary>
    public IReadOnlyList<int> ConnectedPlayerIds => _connectedPlayerIds.AsReadOnly();

    // ====================================================================================================
    // 5. Unity Lifecycle Methods
    //    Used for initialization and cleanup.
    // ====================================================================================================

    private void Awake()
    {
        // Singleton enforcement:
        // If an instance already exists and it's not this one, destroy this GameObject
        // to ensure only one NetworkManager is active.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NetworkManager: Another instance of NetworkManager found. Destroying this duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            // If no instance exists, set this as the instance.
            Instance = this;
            // Make sure the NetworkManager persists across scene loads.
            DontDestroyOnLoad(gameObject);
            Debug.Log($"NetworkManager: Initialized. Server target: {serverAddress}:{serverPort}");
        }
    }

    private void OnDestroy()
    {
        // When the NetworkManager is destroyed, clear the static instance if it was this one.
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("NetworkManager: Instance destroyed.");
        }
        // Ensure we unsubscribe from events to prevent memory leaks if other objects still hold references.
        // In a real scenario, this is less critical for a singleton that persists, but good practice.
        OnConnectedToServer = null;
        OnDisconnectedFromServer = null;
        OnMessageReceived = null;
        OnPlayerJoined = null;
        OnPlayerLeft = null;
        OnNetworkError = null;

        // Disconnect if still connected.
        if (_isConnected)
        {
            DisconnectFromServer();
        }
    }

    // ====================================================================================================
    // 6. Public API Methods (Core Network Operations)
    //    These methods provide the main interface for other scripts to interact with network functionality.
    // ====================================================================================================

    /// <summary>
    /// Initiates a connection attempt to the configured server.
    /// </summary>
    public void ConnectToServer()
    {
        if (_isConnected)
        {
            Debug.LogWarning("NetworkManager: Already connected to server.");
            return;
        }

        Debug.Log($"NetworkManager: Attempting to connect to {serverAddress}:{serverPort}...");
        StartCoroutine(SimulateConnectionProcess());
    }

    /// <summary>
    /// Disconnects the client from the server.
    /// </summary>
    public void DisconnectFromServer()
    {
        if (!_isConnected)
        {
            Debug.LogWarning("NetworkManager: Not connected to server.");
            return;
        }

        Debug.Log("NetworkManager: Disconnecting from server...");
        StopAllCoroutines(); // Stop any pending connection attempts or simulations.

        // Simulate actual disconnection logic
        _isConnected = false;
        _localPlayerId = -1;
        _connectedPlayerIds.Clear();

        Debug.Log("NetworkManager: Disconnected from server.");
        OnDisconnectedFromServer?.Invoke(); // Notify subscribers
    }

    /// <summary>
    /// Sends a message string to the server.
    /// In a real game, this would involve serialization and sending via a network protocol.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public void SendMessageToServer(string message)
    {
        if (!_isConnected)
        {
            Debug.LogWarning("NetworkManager: Cannot send message, not connected to server.");
            OnNetworkError?.Invoke("Attempted to send message while disconnected.");
            return;
        }

        Debug.Log($"NetworkManager: Sending message to server: '{message}' (Local Player ID: {_localPlayerId})");
        // In a real network setup, this would call into the underlying networking library
        // to send the message to the server.
        // Example: netcodeManager.SendServerRpc(message);
    }

    // ====================================================================================================
    // 7. Internal/Simulation Methods
    //    These methods handle the actual (simulated) network operations and state changes.
    //    In a real project, these would interact with a networking library (e.g., Netcode for GameObjects).
    // ====================================================================================================

    /// <summary>
    /// Simulates the connection process with a delay.
    /// </summary>
    private IEnumerator SimulateConnectionProcess()
    {
        yield return new WaitForSeconds(connectionDelay);

        // Simulate successful connection
        _isConnected = true;
        _localPlayerId = UnityEngine.Random.Range(1000, 9999); // Assign a random player ID
        _connectedPlayerIds.Add(_localPlayerId); // Add self to connected players

        Debug.Log($"NetworkManager: Successfully connected to server! Local Player ID: {_localPlayerId}");
        OnConnectedToServer?.Invoke(); // Notify subscribers

        // Simulate some initial players joining
        StartCoroutine(SimulateInitialPlayerActivity());
    }

    /// <summary>
    /// Simulates initial player joins shortly after connection.
    /// </summary>
    private IEnumerator SimulateInitialPlayerActivity()
    {
        yield return new WaitForSeconds(1.0f); // Small delay after connection

        // Simulate other players joining
        SimulatePlayerJoin(UnityEngine.Random.Range(10000, 19999));
        yield return new WaitForSeconds(0.5f);
        SimulatePlayerJoin(UnityEngine.Random.Range(20000, 29999));

        // Start a routine to simulate incoming messages periodically
        StartCoroutine(SimulateIncomingMessagesRoutine());
    }

    /// <summary>
    /// Simulates an incoming message from the server (e.g., a chat message, game state update).
    /// This would be triggered by the networking library receiving data.
    /// </summary>
    /// <param name="message">The simulated message content.</param>
    public void SimulateIncomingMessage(string message)
    {
        if (_isConnected)
        {
            Debug.Log($"NetworkManager: Received message from server: '{message}'");
            OnMessageReceived?.Invoke(message); // Notify subscribers
        }
    }

    /// <summary>
    /// Simulates a player joining the game.
    /// This would typically be triggered by a network event indicating a new client connected.
    /// </summary>
    /// <param name="playerId">The ID of the player who joined.</param>
    public void SimulatePlayerJoin(int playerId)
    {
        if (_isConnected && !_connectedPlayerIds.Contains(playerId))
        {
            _connectedPlayerIds.Add(playerId);
            Debug.Log($"NetworkManager: Player {playerId} joined the game. Total players: {_connectedPlayerIds.Count}");
            OnPlayerJoined?.Invoke(playerId); // Notify subscribers
        }
    }

    /// <summary>
    /// Simulates a player leaving the game.
    /// This would typically be triggered by a network event indicating a client disconnected.
    /// </summary>
    /// <param name="playerId">The ID of the player who left.</param>
    public void SimulatePlayerLeave(int playerId)
    {
        if (_isConnected && _connectedPlayerIds.Remove(playerId))
        {
            Debug.Log($"NetworkManager: Player {playerId} left the game. Total players: {_connectedPlayerIds.Count}");
            OnPlayerLeft?.Invoke(playerId); // Notify subscribers
        }
    }

    /// <summary>
    /// Continuously simulates incoming messages for demonstration purposes.
    /// </summary>
    private IEnumerator SimulateIncomingMessagesRoutine()
    {
        int messageCount = 0;
        while (_isConnected)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 7f));
            messageCount++;
            SimulateIncomingMessage($"Server broadcast: Message #{messageCount} from server.");

            // Occasionally simulate a player joining/leaving
            if (messageCount % 5 == 0 && _connectedPlayerIds.Count < 5)
            {
                SimulatePlayerJoin(UnityEngine.Random.Range(30000, 39999));
            }
            else if (messageCount % 7 == 0 && _connectedPlayerIds.Count > 1)
            {
                // Leave out the local player
                int playerToLeave = _connectedPlayerIds[UnityEngine.Random.Range(1, _connectedPlayerIds.Count)];
                SimulatePlayerLeave(playerToLeave);
            }
        }
    }
}

/*
// ====================================================================================================
// Example Usage: How other scripts would interact with the NetworkManager
// ====================================================================================================

// You would typically create a UI Manager script, a Game State script, or a Player Controller
// that subscribes to events and calls methods on the NetworkManager.

/// <summary>
/// This is an example script demonstrating how other parts of your game would
/// interact with the NetworkManager.
/// </summary>
public class GameClient : MonoBehaviour
{
    private void Start()
    {
        // IMPORTANT: Ensure a NetworkManager GameObject exists in your scene
        // with the NetworkManager script attached, or it will be created automatically
        // but not configured via the Inspector.

        // Subscribe to events from the NetworkManager
        NetworkManager.Instance.OnConnectedToServer += HandleConnected;
        NetworkManager.Instance.OnDisconnectedFromServer += HandleDisconnected;
        NetworkManager.Instance.OnMessageReceived += HandleMessageReceived;
        NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft;
        NetworkManager.Instance.OnNetworkError += HandleNetworkError;

        Debug.Log("GameClient: Subscribed to NetworkManager events.");

        // Optionally, connect automatically on start
        // NetworkManager.Instance.ConnectToServer();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks, especially if this GameClient
        // might be destroyed while the NetworkManager persists (which it does via DontDestroyOnLoad).
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectedToServer -= HandleConnected;
            NetworkManager.Instance.OnDisconnectedFromServer -= HandleDisconnected;
            NetworkManager.Instance.OnMessageReceived -= HandleMessageReceived;
            NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
            NetworkManager.Instance.OnNetworkError -= HandleNetworkError;
            Debug.Log("GameClient: Unsubscribed from NetworkManager events.");
        }
    }

    // --- Event Handlers ---
    private void HandleConnected()
    {
        Debug.Log($"GameClient: Successfully connected! My Player ID: {NetworkManager.Instance.LocalPlayerId}");
        Debug.Log($"GameClient: Currently {NetworkManager.Instance.ConnectedPlayerIds.Count} players connected.");
        // Example: Now that we're connected, maybe request player data or join a specific lobby.
        NetworkManager.Instance.SendMessageToServer("Hello, server! I'm ready to play.");
    }

    private void HandleDisconnected()
    {
        Debug.Log("GameClient: Disconnected from server.");
        // Example: Show a "Disconnected" message on the UI.
    }

    private void HandleMessageReceived(string message)
    {
        Debug.Log($"GameClient: Received server message: {message}");
        // Example: Display message in a chat window.
    }

    private void HandlePlayerJoined(int playerId)
    {
        Debug.Log($"GameClient: Player {playerId} has joined. Total players now: {NetworkManager.Instance.ConnectedPlayerIds.Count}");
        // Example: Update a scoreboard or player list UI.
    }

    private void HandlePlayerLeft(int playerId)
    {
        Debug.Log($"GameClient: Player {playerId} has left. Total players now: {NetworkManager.Instance.ConnectedPlayerIds.Count}");
        // Example: Remove player from scoreboard.
    }

    private void HandleNetworkError(string errorMessage)
    {
        Debug.LogError($"GameClient: Network Error occurred: {errorMessage}");
        // Example: Display error message to the user, log to file.
    }

    // --- UI Interaction Examples (simulate button clicks) ---
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!NetworkManager.Instance.IsConnected)
            {
                NetworkManager.Instance.ConnectToServer();
            }
            else
            {
                Debug.Log("GameClient: Already connected.");
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (NetworkManager.Instance.IsConnected)
            {
                NetworkManager.Instance.DisconnectFromServer();
            }
            else
            {
                Debug.Log("GameClient: Not connected to disconnect.");
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (NetworkManager.Instance.IsConnected)
            {
                NetworkManager.Instance.SendMessageToServer($"Ping from local player {NetworkManager.Instance.LocalPlayerId} at {Time.time}");
            }
            else
            {
                Debug.LogWarning("GameClient: Cannot send message, not connected.");
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
             // Simulate an external event triggering a message from the server
            NetworkManager.Instance.SimulateIncomingMessage("Simulated server event: Game state updated!");
        }
    }
}
*/
```

---

**How to Use This Script in Unity:**

1.  **Create a C# Script:** In your Unity project, right-click in the Project window, go to `Create > C# Script`, and name it `NetworkManager`.
2.  **Copy and Paste:** Open the newly created script and replace its content with the `NetworkManager` class code provided above.
3.  **Create an Empty GameObject:** In your Hierarchy, right-click, then go to `Create Empty`. Name it `NetworkManagerObject`.
4.  **Attach the Script:** Drag and drop the `NetworkManager` script from your Project window onto the `NetworkManagerObject` in the Hierarchy.
5.  **Run the Scene:** Play your scene.
    *   Observe the `Debug.Log` messages in the Console window.
    *   The `NetworkManager` will initialize and persist across scene loads (due to `DontDestroyOnLoad`).
    *   You can adjust `Server Address`, `Server Port`, and `Connection Delay` in the Inspector for the `NetworkManagerObject`.

**To see the `GameClient` example in action:**

1.  **Uncomment `GameClient`:** Uncomment the entire `GameClient` class at the bottom of the provided script.
2.  **Create another C# Script:** Name it `GameClient` and copy the uncommented `GameClient` class into it.
3.  **Create an Empty GameObject:** Name it `GameClientObject`.
4.  **Attach `GameClient` Script:** Drag and drop the `GameClient` script onto `GameClientObject`.
5.  **Run the Scene:**
    *   The `GameClient` will subscribe to events.
    *   Press **C** to connect.
    *   Press **D** to disconnect.
    *   Press **S** to send a simulated message to the server.
    *   Press **R** to simulate an incoming message from the server.
    *   Observe the console for detailed output of simulated network events.

This setup provides a robust and educational foundation for understanding and implementing the NetworkManagerPattern in your Unity projects. When you integrate a real networking solution (like Unity Netcode, Mirror, Photon, etc.), you would replace the "Simulate..." methods with actual calls to that library, while keeping the public API (`ConnectToServer`, `SendMessageToServer`, and the event system) largely the same.