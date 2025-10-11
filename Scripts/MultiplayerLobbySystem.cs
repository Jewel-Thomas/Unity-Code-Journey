// Unity Design Pattern Example: MultiplayerLobbySystem
// This script demonstrates the MultiplayerLobbySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# script demonstrates the **Multiplayer Lobby System** design pattern in Unity. It provides a foundational structure for managing players, their readiness status, and the overall state of a game lobby, crucial for any multiplayer game.

The core idea of the pattern is to centralize lobby management logic, decouple it from UI and networking specifics using events, and clearly define player and lobby states.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .FirstOrDefault(), .Any(), .All()

// =====================================================================================
// 1. LobbyPlayer Data Structure
//    Represents a single player within the lobby.
// =====================================================================================
/// <summary>
/// Represents a player's data within the multiplayer lobby.
/// This class is serializable so its state can be viewed in the Unity Inspector for debugging.
/// </summary>
[Serializable]
public class LobbyPlayer
{
    public string Id;          // Unique identifier for the player (e.g., network ID, UUID)
    public string Name;        // Display name of the player
    public bool IsHost;        // True if this player is the lobby host
    public bool IsReady;       // True if this player is ready to start the game

    /// <summary>
    /// Initializes a new instance of the LobbyPlayer class.
    /// </summary>
    /// <param name="id">The unique identifier for the player.</param>
    /// <param name="name">The display name for the player.</param>
    /// <param name="isHost">True if this player is the host of the lobby.</param>
    public LobbyPlayer(string id, string name, bool isHost = false)
    {
        Id = id;
        Name = name;
        IsHost = isHost;
        IsReady = false; // Players typically start in an unready state
    }

    public override string ToString()
    {
        return $"[{Id}] {Name} (Host: {IsHost}, Ready: {IsReady})";
    }
}

// =====================================================================================
// 2. LobbyState Enum
//    Defines the possible states the lobby can be in.
// =====================================================================================
/// <summary>
/// Defines the various states the multiplayer lobby can be in.
/// </summary>
public enum LobbyState
{
    /// <summary>No players, or not enough players, or not all players ready.</summary>
    WaitingForPlayers,
    /// <summary>Enough players are present and all required players are ready.</summary>
    ReadyToStart,
    /// <summary>The game has started, and the lobby is no longer active for joining/leaving/readying.</summary>
    InGame,
    // Add other states as needed, e.g., 'ConnectingToGame', 'Disconnected'
}

// =====================================================================================
// 3. MultiplayerLobbySystem (The Core Lobby Manager)
//    This MonoBehaviour acts as the central hub for all lobby-related logic.
// =====================================================================================
/// <summary>
/// The central manager for the multiplayer lobby system.
/// This class implements the core logic for player management, state transitions,
/// and event broadcasting, following the Multiplayer Lobby System design pattern.
/// It uses a Singleton pattern for easy global access.
/// </summary>
public class MultiplayerLobbySystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Ensures only one instance of the LobbySystem exists and provides a global access point.
    public static MultiplayerLobbySystem Instance { get; private set; }

    // --- Configuration Variables (Adjustable in Unity Inspector) ---
    [Header("Lobby Settings")]
    [Tooltip("Minimum number of players required before the game can potentially start.")]
    [SerializeField] private int _minPlayersToStart = 2;

    [Tooltip("Maximum number of players allowed in the lobby.")]
    [SerializeField] private int _maxPlayersInLobby = 4;

    // --- Internal Lobby State ---
    private List<LobbyPlayer> _players = new List<LobbyPlayer>();
    /// <summary>
    /// Provides a read-only list of all players currently in the lobby.
    /// </summary>
    public IReadOnlyList<LobbyPlayer> Players => _players.AsReadOnly(); // Public read-only access to prevent external modification

    private LobbyState _currentLobbyState = LobbyState.WaitingForPlayers;
    /// <summary>
    /// Gets the current state of the lobby.
    /// </summary>
    public LobbyState CurrentLobbyState => _currentLobbyState;

    private LobbyPlayer _hostPlayer;
    /// <summary>
    /// Gets the current host player of the lobby. Null if no players are in the lobby.
    /// </summary>
    public LobbyPlayer HostPlayer => _hostPlayer;

    // --- Events (Key for Decoupling: Observer Pattern) ---
    // These events allow other parts of the application (e.g., UI, NetworkManager)
    // to react to changes in the lobby without directly coupling to this class.
    [Header("Lobby Events (Subscribe from UI/Network scripts)")]
    public static event Action<LobbyPlayer> OnPlayerJoined;           // Fired when a new player successfully joins.
    public static event Action<string> OnPlayerLeft;                  // Fired when a player leaves (sends player ID).
    public static event Action<LobbyPlayer> OnPlayerReadyStatusChanged; // Fired when a player's ready status changes.
    public static event Action<LobbyPlayer> OnHostChanged;            // Fired when the lobby host changes.
    public static event Action<LobbyState> OnLobbyStateChanged;      // Fired when the overall lobby state changes.
    public static event Action OnGameStarting;                        // Fired when the host initiates game start.

    // =====================================================================================
    // MonoBehaviour Lifecycle Methods
    // =====================================================================================
    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple MultiplayerLobbySystem instances detected! Destroying duplicate.");
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            // Optional: Keep the lobby manager alive across scene loads if the lobby state
            // needs to persist (e.g., transitioning from a lobby scene to a game scene).
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clean up singleton reference when this object is destroyed.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =====================================================================================
    // Public API for Lobby Actions
    // These methods are typically called by a NetworkManager or LocalPlayerInput based on
    // network messages or UI interactions.
    // =====================================================================================

    /// <summary>
    /// Attempts to add a new player to the lobby.
    /// This method would typically be called by the NetworkManager when a client connects
    /// and is authorized to join the lobby.
    /// </summary>
    /// <param name="playerId">A unique identifier for the player (e.g., their network ID).</param>
    /// <param name="playerName">The display name for the player.</param>
    /// <returns>True if the player joined successfully, false otherwise (e.g., lobby full, player already exists).</returns>
    public bool JoinLobby(string playerId, string playerName)
    {
        // 1. Check if the lobby is full
        if (_players.Count >= _maxPlayersInLobby)
        {
            Debug.LogWarning($"Lobby is full. Cannot add player {playerName} ({playerId}). Max players: {_maxPlayersInLobby}.");
            return false;
        }

        // 2. Check if the player is already in the lobby
        if (_players.Any(p => p.Id == playerId))
        {
            Debug.LogWarning($"Player {playerName} ({playerId}) is already in the lobby.");
            return false;
        }

        // 3. Create new player and add to the list
        bool isFirstPlayer = _players.Count == 0;
        LobbyPlayer newPlayer = new LobbyPlayer(playerId, playerName, isFirstPlayer);
        _players.Add(newPlayer);

        Debug.Log($"<color=lime>{newPlayer.Name} ({newPlayer.Id}) joined the lobby.</color>");

        // 4. Assign host if this is the first player
        if (isFirstPlayer)
        {
            _hostPlayer = newPlayer;
            Debug.Log($"<color=yellow>{newPlayer.Name} ({newPlayer.Id}) is now the lobby host.</color>");
            // OnHostChanged is not invoked here if it's the very first player, as the concept of host changing
            // implies a previous host. It's set during player creation.
        }

        // 5. Notify subscribers about the new player and potentially the lobby state change
        OnPlayerJoined?.Invoke(newPlayer);
        CheckLobbyStateChange(); // Re-evaluate lobby state after a player joins
        return true;
    }

    /// <summary>
    /// Removes a player from the lobby.
    /// This would typically be called by the NetworkManager when a client disconnects,
    /// or by a local player choosing to leave the lobby.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to remove.</param>
    public void LeaveLobby(string playerId)
    {
        LobbyPlayer playerToLeave = _players.FirstOrDefault(p => p.Id == playerId);
        if (playerToLeave == null)
        {
            Debug.LogWarning($"Player with ID {playerId} not found in lobby to leave.");
            return;
        }

        // 1. Remove the player from the list
        _players.Remove(playerToLeave);
        Debug.Log($"<color=orange>{playerToLeave.Name} ({playerToLeave.Id}) left the lobby.</color>");
        
        // 2. Notify subscribers that a player has left
        OnPlayerLeft?.Invoke(playerId);

        // 3. Handle host reassignment if the host left
        if (playerToLeave.IsHost && _players.Count > 0)
        {
            _hostPlayer = _players[0]; // Assign the first player in the list as the new host
            _hostPlayer.IsHost = true; // Update their 'IsHost' property
            Debug.Log($"<color=yellow>Host {playerToLeave.Name} left. New host is {_hostPlayer.Name} ({_hostPlayer.Id}).</color>");
            OnHostChanged?.Invoke(_hostPlayer); // Notify about host change
        }
        else if (_players.Count == 0)
        {
            _hostPlayer = null; // No players left, no host.
        }
        
        // 4. Re-evaluate lobby state after a player leaves
        CheckLobbyStateChange();
    }

    /// <summary>
    /// Sets a player's ready status.
    /// This would typically be called by the NetworkManager when a player signals
    /// their readiness (e.g., clicks a "Ready" button in the UI).
    /// </summary>
    /// <param name="playerId">The unique ID of the player whose status is changing.</param>
    /// <param name="isReady">True to set the player as ready, false to set as unready.</param>
    public void SetPlayerReady(string playerId, bool isReady)
    {
        LobbyPlayer player = _players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
        {
            Debug.LogWarning($"Player with ID {playerId} not found in lobby to set ready status.");
            return;
        }

        // Avoid unnecessary updates if status hasn't changed
        if (player.IsReady == isReady)
        {
            Debug.Log($"Player {player.Name} ({player.Id}) is already {(isReady ? "ready" : "not ready")}. No change.");
            return;
        }

        player.IsReady = isReady;
        Debug.Log($"<color={(isReady ? "aqua" : "silver")}>Player {player.Name} ({player.Id}) is now {(isReady ? "READY" : "NOT READY")}.</color>");
        
        // Notify subscribers about the player's ready status change
        OnPlayerReadyStatusChanged?.Invoke(player);
        CheckLobbyStateChange(); // Re-evaluate lobby state after a player's ready status changes
    }

    /// <summary>
    /// Attempts to start the game. This action is typically restricted to the host.
    /// It should only proceed if the lobby is in a 'ReadyToStart' state.
    /// </summary>
    /// <param name="hostId">The unique ID of the player attempting to start the game. Must match the current host's ID.</param>
    public void StartGame(string hostId)
    {
        // 1. Verify if the calling player is the host
        if (_hostPlayer == null || _hostPlayer.Id != hostId)
        {
            Debug.LogWarning($"Player {hostId} is not the host and cannot start the game.");
            return;
        }

        // 2. Verify the lobby state allows starting the game
        if (_currentLobbyState != LobbyState.ReadyToStart)
        {
            Debug.LogWarning($"Cannot start game in current lobby state ({_currentLobbyState}). " +
                             $"Ensure enough players are present and all required players are ready.");
            return;
        }

        Debug.Log($"<color=green>Host {_hostPlayer.Name} ({_hostPlayer.Id}) initiating game start!</color>");
        
        // 3. Transition lobby state to InGame
        ChangeLobbyState(LobbyState.InGame);
        
        // 4. Notify subscribers that the game is starting.
        // This event would typically trigger scene loading, network session setup, etc.
        OnGameStarting?.Invoke();
    }

    // =====================================================================================
    // Private Helper Methods
    // These methods encapsulate internal logic and state transitions.
    // =====================================================================================

    /// <summary>
    /// Changes the internal lobby state and broadcasts an event if the state has changed.
    /// </summary>
    /// <param name="newState">The new LobbyState to transition to.</param>
    private void ChangeLobbyState(LobbyState newState)
    {
        if (_currentLobbyState == newState) return; // No change needed

        Debug.Log($"<color=cyan>Lobby State Changed: {_currentLobbyState} -> {newState}</color>");
        _currentLobbyState = newState;
        OnLobbyStateChanged?.Invoke(_currentLobbyState); // Notify subscribers
    }

    /// <summary>
    /// Evaluates current lobby conditions (player count, readiness) and updates the
    /// overall <see cref="LobbyState"/> accordingly.
    /// This is called after any significant event like a player joining, leaving, or changing ready status.
    /// </summary>
    private void CheckLobbyStateChange()
    {
        // If there are no players, we are always waiting.
        if (_players.Count == 0)
        {
            ChangeLobbyState(LobbyState.WaitingForPlayers);
            return;
        }

        // Check if we have enough players to meet the minimum requirement.
        bool hasEnoughPlayers = _players.Count >= _minPlayersToStart;

        // Check if all players (who are currently in the lobby) are ready.
        // This assumes that if _minPlayersToStart is met, all current players must be ready.
        bool allPlayersReady = _players.All(p => p.IsReady);

        if (hasEnoughPlayers && allPlayersReady)
        {
            ChangeLobbyState(LobbyState.ReadyToStart);
        }
        else
        {
            // If not enough players, or enough players but not all are ready,
            // the lobby is still in a waiting state.
            ChangeLobbyState(LobbyState.WaitingForPlayers);
        }
    }

    // =====================================================================================
    // Editor-only Testing Methods (using [ContextMenu])
    // These methods allow for quick testing and debugging directly from the Unity Inspector.
    // Right-click on the MultiplayerLobbySystem component in the Inspector to see these options.
    // =====================================================================================
    [ContextMenu("Test: Add Player 1 (Host)")]
    private void TestAddPlayer1() => JoinLobby("p1_id", "PlayerOne");

    [ContextMenu("Test: Add Player 2")]
    private void TestAddPlayer2() => JoinLobby("p2_id", "PlayerTwo");

    [ContextMenu("Test: Add Player 3")]
    private void TestAddPlayer3() => JoinLobby("p3_id", "PlayerThree");

    [ContextMenu("Test: Add Player 4")]
    private void TestAddPlayer4() => JoinLobby("p4_id", "PlayerFour");

    [ContextMenu("Test: Player 1 Ready")]
    private void TestPlayer1Ready() => SetPlayerReady("p1_id", true);

    [ContextMenu("Test: Player 2 Ready")]
    private void TestPlayer2Ready() => SetPlayerReady("p2_id", true);

    [ContextMenu("Test: Player 3 Ready")]
    private void TestPlayer3Ready() => SetPlayerReady("p3_id", true);

    [ContextMenu("Test: Player 4 Ready")]
    private void TestPlayer4Ready() => SetPlayerReady("p4_id", true);

    [ContextMenu("Test: Player 1 Unready")]
    private void TestPlayer1Unready() => SetPlayerReady("p1_id", false);

    [ContextMenu("Test: Player 2 Unready")]
    private void TestPlayer2Unready() => SetPlayerReady("p2_id", false);

    [ContextMenu("Test: Player 3 Unready")]
    private void TestPlayer3Unready() => SetPlayerReady("p3_id", false);

    [ContextMenu("Test: Player 4 Unready")]
    private void TestPlayer4Unready() => SetPlayerReady("p4_id", false);

    [ContextMenu("Test: Player 2 Leave")]
    private void TestPlayer2Leave() => LeaveLobby("p2_id");

    [ContextMenu("Test: Host (Player 1) Start Game")]
    private void TestHostStartGame()
    {
        if (_hostPlayer != null)
        {
            StartGame(_hostPlayer.Id);
        }
        else
        {
            Debug.LogWarning("No host found to start game for testing.");
        }
    }
}


/* =====================================================================================
 * EXAMPLE USAGE: How to integrate this MultiplayerLobbySystem into your Unity project.
 *
 * 1. Create an Empty GameObject in your scene (e.g., named "LobbyManager").
 * 2. Attach the 'MultiplayerLobbySystem.cs' script to this GameObject.
 * 3. Configure '_minPlayersToStart' and '_maxPlayersInLobby' in the Inspector if needed.
 *
 * The `MultiplayerLobbySystem` uses a Singleton pattern, meaning you can access its
 * `Instance` property from anywhere in your code (e.g., `MultiplayerLobbySystem.Instance.JoinLobby(...)`).
 *
 * Below are examples of how other scripts (like UI or Network managers) would interact
 * with this LobbySystem using its public methods and events.
 * ===================================================================================== */

// =====================================================================================
// Example: LobbyUIController (A script that manages your lobby's visual interface)
// = =====================================================================================
/// <summary>
/// This is an example of a UI script that would subscribe to the LobbySystem's events
/// to update the visual representation of the lobby (e.g., player list, ready buttons).
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    // [SerializeField] private GameObject playerEntryPrefab; // Assume a UI prefab for each player
    // [SerializeField] private Transform playerListParent;   // The parent transform for player entries
    // [SerializeField] private Button readyButton;
    // [SerializeField] private Button startButton;
    // [SerializeField] private Text lobbyStateText;

    private Dictionary<string, GameObject> _playerUIs = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        // Subscribe to LobbySystem events to react to changes
        MultiplayerLobbySystem.OnPlayerJoined += HandlePlayerJoined;
        MultiplayerLobbySystem.OnPlayerLeft += HandlePlayerLeft;
        MultiplayerLobbySystem.OnPlayerReadyStatusChanged += HandlePlayerReadyStatusChanged;
        MultiplayerLobbySystem.OnHostChanged += HandleHostChanged;
        MultiplayerLobbySystem.OnLobbyStateChanged += HandleLobbyStateChanged;
        MultiplayerLobbySystem.OnGameStarting += HandleGameStarting;

        // Initialize UI based on current lobby state if the system is already running
        if (MultiplayerLobbySystem.Instance != null)
        {
            UpdateAllPlayerUIs(MultiplayerLobbySystem.Instance.Players);
            UpdateLobbyStateUI(MultiplayerLobbySystem.Instance.CurrentLobbyState);
            UpdateStartButton(MultiplayerLobbySystem.Instance.CurrentLobbyState, MultiplayerLobbySystem.Instance.HostPlayer?.Id == "local_player_id");
            UpdateReadyButton("local_player_id"); // Assuming a local player exists
        }

        // Example: Setup UI button listeners (assuming a local player with ID "local_player_id")
        // readyButton.onClick.AddListener(() =>
        // {
        //     LobbyPlayer localPlayer = MultiplayerLobbySystem.Instance.Players.FirstOrDefault(p => p.Id == "local_player_id");
        //     if (localPlayer != null)
        //     {
        //         MultiplayerLobbySystem.Instance.SetPlayerReady("local_player_id", !localPlayer.IsReady);
        //     }
        // });
        // startButton.onClick.AddListener(() => MultiplayerLobbySystem.Instance.StartGame("local_player_id"));
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and unexpected behavior
        MultiplayerLobbySystem.OnPlayerJoined -= HandlePlayerJoined;
        MultiplayerLobbySystem.OnPlayerLeft -= HandlePlayerLeft;
        MultiplayerLobbySystem.OnPlayerReadyStatusChanged -= HandlePlayerReadyStatusChanged;
        MultiplayerLobbySystem.OnHostChanged -= HandleHostChanged;
        MultiplayerLobbySystem.OnLobbyStateChanged -= HandleLobbyStateChanged;
        MultiplayerLobbySystem.OnGameStarting -= HandleGameStarting;

        // Example: Clean up UI button listeners
        // readyButton.onClick.RemoveAllListeners();
        // startButton.onClick.RemoveAllListeners();
    }

    private void HandlePlayerJoined(LobbyPlayer player)
    {
        Debug.Log($"UI: Player {player.Name} joined. Updating UI.");
        // Create a new UI entry for the player and add it to _playerUIs dictionary
        // Example: GameObject playerUI = Instantiate(playerEntryPrefab, playerListParent);
        // playerUI.GetComponent<PlayerEntryUI>().Initialize(player);
        // _playerUIs[player.Id] = playerUI;
        UpdateUIForPlayer(player);
        UpdateReadyButton("local_player_id"); // Check local player ready status
    }

    private void HandlePlayerLeft(string playerId)
    {
        Debug.Log($"UI: Player {playerId} left. Removing UI.");
        // Destroy the corresponding UI entry
        if (_playerUIs.TryGetValue(playerId, out GameObject playerUI))
        {
            // Destroy(playerUI);
            _playerUIs.Remove(playerId);
        }
        UpdateReadyButton("local_player_id");
    }

    private void HandlePlayerReadyStatusChanged(LobbyPlayer player)
    {
        Debug.Log($"UI: Player {player.Name} ready status changed. Updating UI.");
        // Update the readiness indicator on the player's UI entry
        // _playerUIs[player.Id].GetComponent<PlayerEntryUI>().UpdateReadyStatus(player.IsReady);
        UpdateUIForPlayer(player);
        UpdateReadyButton("local_player_id"); // Update local player's ready button text/state
    }

    private void HandleHostChanged(LobbyPlayer newHost)
    {
        Debug.Log($"UI: Host changed to {newHost.Name}. Updating UI.");
        // Update host indicators on all player UI entries
        foreach (var entry in _playerUIs.Values)
        {
            // entry.GetComponent<PlayerEntryUI>().UpdateHostStatus(entry.GetComponent<PlayerEntryUI>().PlayerId == newHost.Id);
        }
        // Update start button visibility/interactability for the new host
        UpdateStartButton(MultiplayerLobbySystem.Instance.CurrentLobbyState, newHost.Id == "local_player_id");
    }

    private void HandleLobbyStateChanged(LobbyState newState)
    {
        Debug.Log($"UI: Lobby state changed to {newState}. Updating UI.");
        // Update a text field showing the lobby state
        // lobbyStateText.text = $"Lobby State: {newState}";
        UpdateStartButton(newState, MultiplayerLobbySystem.Instance.HostPlayer?.Id == "local_player_id");
        UpdateReadyButton("local_player_id");
    }

    private void HandleGameStarting()
    {
        Debug.Log("UI: Game is starting! Transitioning UI.");
        // Disable lobby UI, show loading screen, etc.
        // gameObject.SetActive(false); // Hide the entire lobby UI
        // SceneManager.LoadScene("GameScene"); // Example: Initiate scene load
    }

    // Helper to update individual player UI (conceptually)
    private void UpdateUIForPlayer(LobbyPlayer player)
    {
        // This is where you would update the visual elements for a single player.
        // E.g., change color of name, show/hide ready icon, show/hide host icon.
        Debug.Log($"UI Update for {player}");
    }

    // Helper to refresh all player UIs (e.g., on initial load)
    private void UpdateAllPlayerUIs(IReadOnlyList<LobbyPlayer> players)
    {
        // Clear existing UIs
        foreach (var ui in _playerUIs.Values) { /* Destroy(ui); */ }
        _playerUIs.Clear();

        // Recreate UIs for all current players
        foreach (var player in players)
        {
            HandlePlayerJoined(player); // Re-use the join handler
        }
    }

    // Helper to manage start button visibility/interactability
    private void UpdateStartButton(LobbyState state, bool isLocalPlayerHost)
    {
        // Start button should only be visible and interactable for the host
        // AND when the lobby is in the 'ReadyToStart' state.
        // startButton.gameObject.SetActive(isLocalPlayerHost);
        // startButton.interactable = (isLocalPlayerHost && state == LobbyState.ReadyToStart);
        Debug.Log($"UI: Start Button visible: {isLocalPlayerHost}, interactable: {(isLocalPlayerHost && state == LobbyState.ReadyToStart)}");
    }

    // Helper to manage ready button text/interactability
    private void UpdateReadyButton(string localPlayerId)
    {
        LobbyPlayer localPlayer = MultiplayerLobbySystem.Instance.Players.FirstOrDefault(p => p.Id == localPlayerId);
        if (localPlayer != null)
        {
            // readyButton.gameObject.SetActive(true);
            // readyButton.GetComponentInChildren<Text>().text = localPlayer.IsReady ? "Unready" : "Ready";
            // readyButton.interactable = (MultiplayerLobbySystem.Instance.CurrentLobbyState != LobbyState.InGame);
             Debug.Log($"UI: Ready Button for {localPlayer.Name}. Text: {(localPlayer.IsReady ? "Unready" : "Ready")}");
        }
        else
        {
            // readyButton.gameObject.SetActive(false);
            Debug.Log($"UI: Ready Button hidden as local player not in lobby.");
        }
    }
}

// =====================================================================================
// Example: NetworkManagerFacade (A script that handles network communication)
// =====================================================================================
/// <summary>
/// This is an example of a NetworkManager (or a facade over a specific networking solution)
/// that would translate network events into calls to the LobbySystem, and also react
/// to LobbySystem events to send network messages.
/// </summary>
public class NetworkManagerFacade : MonoBehaviour
{
    // Assume a network framework like Mirror, Netcode for GameObjects, Photon, etc.
    // This example simulates network messages locally.

    private string _localPlayerNetworkId = "local_player_id_123"; // This would come from network identity
    private string _localPlayerName = "LocalPlayer";

    private void Start()
    {
        // Simulate a local player joining the lobby on startup.
        // In a real network game, this would happen after connecting to a relay/server.
        Debug.Log("Network: Simulating local player connection and joining lobby...");
        MultiplayerLobbySystem.Instance.JoinLobby(_localPlayerNetworkId, _localPlayerName);
    }

    // Example of a network message handler for a remote player joining
    public void ReceiveNetworkPlayerJoined(string remotePlayerId, string remotePlayerName)
    {
        Debug.Log($"Network: Received message - remote player {remotePlayerName} joined.");
        MultiplayerLobbySystem.Instance.JoinLobby(remotePlayerId, remotePlayerName);
    }

    // Example of a network message handler for a remote player leaving
    public void ReceiveNetworkPlayerLeft(string remotePlayerId)
    {
        Debug.Log($"Network: Received message - remote player {remotePlayerId} left.");
        MultiplayerLobbySystem.Instance.LeaveLobby(remotePlayerId);
    }

    // Example of a network message handler for a remote player changing ready status
    public void ReceiveNetworkPlayerReadyStatus(string remotePlayerId, bool isReady)
    {
        Debug.Log($"Network: Received message - remote player {remotePlayerId} ready status: {isReady}.");
        MultiplayerLobbySystem.Instance.SetPlayerReady(remotePlayerId, isReady);
    }

    // Example of how to send network messages when a local action occurs
    private void OnEnable()
    {
        MultiplayerLobbySystem.OnPlayerReadyStatusChanged += HandleLocalPlayerReadyStatusChanged;
        MultiplayerLobbySystem.OnGameStarting += HandleGameStarting;
    }

    private void OnDisable()
    {
        MultiplayerLobbySystem.OnPlayerReadyStatusChanged -= HandleLocalPlayerReadyStatusChanged;
        MultiplayerLobbySystem.OnGameStarting -= HandleGameStarting;
    }

    private void HandleLocalPlayerReadyStatusChanged(LobbyPlayer player)
    {
        if (player.Id == _localPlayerNetworkId)
        {
            Debug.Log($"Network: Local player ({player.Name}) ready status changed to {player.IsReady}. " +
                      $"Would send network message to server/other clients.");
            // Example: networkManager.Send("PlayerReadyStatus", player.Id, player.IsReady);
        }
    }

    private void HandleGameStarting()
    {
        Debug.Log("Network: Game is starting! Would initiate network scene load or session setup.");
        // Example: networkManager.LoadScene("GameScene");
    }

    // Example: Simulating a remote player interaction (e.g. from an RPC or server push)
    [ContextMenu("Simulate: Remote Player 2 Joins")]
    private void SimulateRemotePlayer2Joins()
    {
        ReceiveNetworkPlayerJoined("remote_p2_id", "RemotePlayer2");
    }

    [ContextMenu("Simulate: Remote Player 2 Ready")]
    private void SimulateRemotePlayer2Ready()
    {
        ReceiveNetworkPlayerReadyStatus("remote_p2_id", true);
    }

    [ContextMenu("Simulate: Local Player Ready")]
    private void SimulateLocalPlayerReady()
    {
        MultiplayerLobbySystem.Instance.SetPlayerReady(_localPlayerNetworkId, true);
    }
}
```