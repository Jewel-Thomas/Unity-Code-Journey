// Unity Design Pattern Example: MatchmakingSystem
// This script demonstrates the MatchmakingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Matchmaking System design pattern is crucial in games that require players to be grouped together for multiplayer sessions. It manages a pool of waiting players, applies specific criteria (like skill level, game mode, number of players), and forms matches when those criteria are met. This pattern decouples the process of player queuing from the actual game session management, making the system more modular and scalable.

Here's a complete, practical C# Unity example of a MatchmakingSystem, ready to be dropped into your project.

---

### 1. `MatchmakingPlayer.cs` (Data Structure)

This struct represents a player waiting in the matchmaking queue.

```csharp
using System;
using UnityEngine;

/// <summary>
/// Represents a player participating in the matchmaking system.
/// </summary>
[Serializable]
public struct MatchmakingPlayer
{
    public string PlayerId;         // Unique identifier for the player
    public int SkillRating;         // A numerical representation of player skill
    public string DesiredGameMode;  // The game mode the player wants to play (e.g., "TeamDeathmatch", "FreeForAll")
    public int MinPlayersDesired;   // Minimum players this player wants in a match
    public int MaxPlayersDesired;   // Maximum players this player wants in a match
    public float EnqueueTime;       // The time (in Unity's Time.time) when the player joined the queue

    public MatchmakingPlayer(string id, int skill, string gameMode, int minPlayers, int maxPlayers)
    {
        PlayerId = id;
        SkillRating = skill;
        DesiredGameMode = gameMode;
        MinPlayersDesired = minPlayers;
        MaxPlayersDesired = maxPlayers;
        EnqueueTime = Time.time; // Record when the player entered the queue
    }

    public override string ToString()
    {
        return $"[PlayerId: {PlayerId}, Skill: {SkillRating}, Mode: {DesiredGameMode}, QueueTime: {Time.time - EnqueueTime:F1}s]";
    }
}
```

---

### 2. `Match.cs` (Data Structure)

This class represents a formed match, containing the players and the game mode.

```csharp
using System.Collections.Generic;

/// <summary>
/// Represents a successfully formed match, containing the participating players and chosen game mode.
/// </summary>
public class Match
{
    public string MatchId { get; private set; }
    public List<MatchmakingPlayer> Players { get; private set; }
    public string GameMode { get; private set; }

    public Match(string gameMode, List<MatchmakingPlayer> players)
    {
        MatchId = System.Guid.NewGuid().ToString(); // Generate a unique ID for the match
        GameMode = gameMode;
        Players = players;
    }

    public override string ToString()
    {
        string playerList = "";
        foreach (var player in Players)
        {
            playerList += $"{player.PlayerId} (Skill:{player.SkillRating}), ";
        }
        return $"Match ID: {MatchId}, Mode: {GameMode}, Players: [{playerList.TrimEnd(',', ' ')}]";
    }
}
```

---

### 3. `MatchmakingSystem.cs` (Core Logic)

This is the main Unity script that implements the MatchmakingSystem pattern.

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Used for LINQ queries like .RemoveAll()

/// <summary>
/// The MatchmakingSystem design pattern manages a queue of players and forms matches based on predefined criteria.
///
/// Pattern Components:
/// 1.  **Player Data:** (MatchmakingPlayer struct) Represents a player with relevant data (ID, skill, desired mode).
/// 2.  **Match Data:** (Match class) Represents a formed group of players ready for a game session.
/// 3.  **Queue:** (List<MatchmakingPlayer>) Stores players currently waiting for a match.
/// 4.  **Matching Logic:** The core algorithm that processes the queue, applies criteria, and creates matches.
/// 5.  **Events/Callbacks:** Notifies other systems when a match is found or queue status changes.
/// 6.  **Periodic Processing:** (Coroutine) The mechanism to periodically run the matching logic without blocking the main thread.
///
/// This implementation uses a Singleton pattern for easy global access to the matchmaking system.
/// </summary>
public class MatchmakingSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    public static MatchmakingSystem Instance { get; private set; }

    // --- Configuration Parameters ---
    [Header("Matchmaking Configuration")]
    [Tooltip("The minimum number of players required to form a match.")]
    [SerializeField] private int minPlayersPerMatch = 2; // e.g., for a 2v2 match, set to 4
    [Tooltip("The maximum number of players allowed in a match.")]
    [SerializeField] private int maxPlayersPerMatch = 4; // e.g., for a 2v2 match, set to 4

    [Tooltip("The maximum acceptable skill difference between players in the same match.")]
    [SerializeField] private int skillRatingTolerance = 50; // Max difference in skill rating

    [Tooltip("How often (in seconds) the system attempts to find new matches.")]
    [SerializeField] private float matchmakingInterval = 5f; // How often to run the matching algorithm

    [Tooltip("How long (in seconds) a player waits before skill tolerance starts to widen.")]
    [SerializeField] private float toleranceWidenDelay = 30f;
    [Tooltip("How much the skill tolerance widens per second after the delay.")]
    [SerializeField] private int skillToleranceWidenRate = 1;

    // --- Internal State ---
    private List<MatchmakingPlayer> playerQueue = new List<MatchmakingPlayer>();
    private Coroutine matchmakingCoroutine;

    // --- Events ---
    /// <summary>
    /// Event fired when a new match has been successfully formed.
    /// Subscribers can use this to launch a game session or notify players.
    /// </summary>
    public event Action<Match> OnMatchFound;

    /// <summary>
    /// Event fired when a player successfully joins the matchmaking queue.
    /// </summary>
    public event Action<MatchmakingPlayer> OnPlayerJoinedQueue;

    /// <summary>
    /// Event fired when a player leaves the matchmaking queue (either by match found or cancellation).
    /// </summary>
    public event Action<MatchmakingPlayer> OnPlayerLeftQueue;


    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MatchmakingSystem: Another instance found, destroying this one. Ensure only one MatchmakingSystem exists.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the matchmaking system alive across scene changes
        }
    }

    private void OnEnable()
    {
        // Start the matchmaking process when the component becomes active
        if (matchmakingCoroutine != null)
        {
            StopCoroutine(matchmakingCoroutine);
        }
        matchmakingCoroutine = StartCoroutine(MatchmakingLoop());
        Debug.Log("MatchmakingSystem started.");
    }

    private void OnDisable()
    {
        // Stop the matchmaking process when the component is disabled or destroyed
        if (matchmakingCoroutine != null)
        {
            StopCoroutine(matchmakingCoroutine);
            matchmakingCoroutine = null;
        }
        Debug.Log("MatchmakingSystem stopped.");
    }

    // --- Public API for Interaction ---

    /// <summary>
    /// Adds a player to the matchmaking queue.
    /// </summary>
    /// <param name="player">The MatchmakingPlayer object to add.</param>
    public void EnqueuePlayer(MatchmakingPlayer player)
    {
        if (playerQueue.Any(p => p.PlayerId == player.PlayerId))
        {
            Debug.LogWarning($"Player {player.PlayerId} is already in the queue.");
            return;
        }

        playerQueue.Add(player);
        Debug.Log($"Player {player.PlayerId} joined the queue. Current queue size: {playerQueue.Count}");
        OnPlayerJoinedQueue?.Invoke(player);
    }

    /// <summary>
    /// Removes a player from the matchmaking queue.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to remove.</param>
    public void DequeuePlayer(string playerId)
    {
        MatchmakingPlayer? playerToRemove = playerQueue.FirstOrDefault(p => p.PlayerId == playerId);

        if (playerToRemove.HasValue)
        {
            playerQueue.RemoveAll(p => p.PlayerId == playerId); // Use RemoveAll for safety, though FirstOrDefault should ensure only one.
            Debug.Log($"Player {playerId} left the queue. Current queue size: {playerQueue.Count}");
            OnPlayerLeftQueue?.Invoke(playerToRemove.Value);
        }
        else
        {
            Debug.LogWarning($"Player {playerId} not found in the queue.");
        }
    }

    /// <summary>
    /// Returns the current number of players waiting in the queue.
    /// </summary>
    public int GetQueueSize()
    {
        return playerQueue.Count;
    }

    // --- Matchmaking Logic (Internal) ---

    /// <summary>
    /// The main coroutine that periodically attempts to find matches.
    /// </summary>
    private IEnumerator MatchmakingLoop()
    {
        while (true) // Loop indefinitely while the system is enabled
        {
            yield return new WaitForSeconds(matchmakingInterval); // Wait for the configured interval
            TryFindMatches(); // Attempt to find and form matches
        }
    }

    /// <summary>
    /// The core logic for finding and forming matches from the current queue.
    /// It iterates through the queue, groups compatible players, and creates matches.
    /// </summary>
    private void TryFindMatches()
    {
        if (playerQueue.Count < minPlayersPerMatch)
        {
            // Debug.Log($"Not enough players in queue ({playerQueue.Count}) to form a match (min {minPlayersPerMatch}).");
            return;
        }

        Debug.Log($"Attempting to find matches. Queue size: {playerQueue.Count}");

        // Sort players by queue time (oldest first) to prioritize those who have waited longer.
        // This also helps ensure players don't get stuck at the back of the queue.
        playerQueue = playerQueue.OrderBy(p => p.EnqueueTime).ToList();

        // Use a list to store players that have been matched so we can remove them later.
        List<MatchmakingPlayer> matchedPlayersInThisCycle = new List<MatchmakingPlayer>();

        // Iterate through game modes to try and form matches for each mode.
        // This makes the system flexible for multiple game types.
        var distinctGameModes = playerQueue.Select(p => p.DesiredGameMode).Distinct().ToList();

        foreach (var mode in distinctGameModes)
        {
            // Get players for the current game mode who haven't been matched yet.
            List<MatchmakingPlayer> modeSpecificQueue = playerQueue
                .Where(p => p.DesiredGameMode == mode && !matchedPlayersInThisCycle.Contains(p))
                .ToList();

            // Continue forming matches for this mode until no more can be made
            while (modeSpecificQueue.Count >= minPlayersPerMatch)
            {
                // Prioritize players who have been waiting longest
                modeSpecificQueue = modeSpecificQueue.OrderBy(p => p.EnqueueTime).ToList();

                List<MatchmakingPlayer> potentialMatch = new List<MatchmakingPlayer>();
                potentialMatch.Add(modeSpecificQueue[0]); // Start with the longest-waiting player

                int currentSkillTolerance = skillRatingTolerance;

                // Adjust skill tolerance based on queue time
                float timeInQueue = Time.time - potentialMatch[0].EnqueueTime;
                if (timeInQueue > toleranceWidenDelay)
                {
                    currentSkillTolerance += (int)((timeInQueue - toleranceWidenDelay) * skillToleranceWidenRate);
                }

                // Find other compatible players for the match
                for (int i = 1; i < modeSpecificQueue.Count; i++)
                {
                    MatchmakingPlayer candidate = modeSpecificQueue[i];

                    // Check if candidate is compatible with existing players in potentialMatch
                    // For simplicity, we'll check against the first player added (the longest waiting one).
                    // A more complex system might check against the average skill or all players.
                    if (Mathf.Abs(potentialMatch[0].SkillRating - candidate.SkillRating) <= currentSkillTolerance)
                    {
                        potentialMatch.Add(candidate);
                        if (potentialMatch.Count >= minPlayersPerMatch)
                        {
                            // If we have enough players, check if we've hit max or if adding more would exceed skill tolerance too much.
                            // For simplicity, we will form the match once min players are met, up to max players.
                            if (potentialMatch.Count >= maxPlayersPerMatch)
                            {
                                break; // We have enough or too many, stop adding.
                            }
                        }
                    }
                }

                // Check if a valid match was formed
                if (potentialMatch.Count >= minPlayersPerMatch && potentialMatch.Count <= maxPlayersPerMatch)
                {
                    // Calculate average skill of the formed match (optional, for logging or further balancing)
                    int avgSkill = (int)potentialMatch.Average(p => p.SkillRating);
                    Debug.Log($"Match found for mode '{mode}' with {potentialMatch.Count} players (Avg Skill: {avgSkill}).");

                    Match newMatch = new Match(mode, potentialMatch);
                    OnMatchFound?.Invoke(newMatch); // Notify subscribers

                    // Mark players as matched so they are removed from the queue
                    foreach (var player in potentialMatch)
                    {
                        matchedPlayersInThisCycle.Add(player);
                        OnPlayerLeftQueue?.Invoke(player); // Player is leaving queue because match found
                    }

                    // Remove matched players from the current mode-specific queue
                    modeSpecificQueue.RemoveAll(p => potentialMatch.Contains(p));
                }
                else
                {
                    // No match found starting with this player (or not enough compatible players).
                    // Move on to the next longest-waiting player in the mode-specific queue.
                    // To avoid infinite loops, remove the first player and try again with the next.
                    // This means the first player might need to wait longer or tolerance needs to widen more.
                    modeSpecificQueue.RemoveAt(0);
                }
            }
        }

        // Finally, remove all players that were matched in this cycle from the main queue
        playerQueue.RemoveAll(p => matchedPlayersInThisCycle.Contains(p));
    }

    /// <summary>
    /// Returns a read-only list of current players in the queue.
    /// </summary>
    public IReadOnlyList<MatchmakingPlayer> GetCurrentQueue()
    {
        return playerQueue.AsReadOnly();
    }
}
```

---

### 4. `MatchmakingTester.cs` (Example Usage)

This script demonstrates how to interact with the `MatchmakingSystem`. Attach it to an empty GameObject in your scene.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This script demonstrates how to use the MatchmakingSystem.
/// Attach this to an empty GameObject in your scene.
/// </summary>
public class MatchmakingTester : MonoBehaviour
{
    [Header("Test Player Configuration")]
    [Tooltip("Number of test players to generate automatically.")]
    [SerializeField] private int numberOfTestPlayers = 10;
    [Tooltip("Minimum skill rating for generated players.")]
    [SerializeField] private int minSkill = 500;
    [Tooltip("Maximum skill rating for generated players.")]
    [SerializeField] private int maxSkill = 1500;

    [Tooltip("Desired game modes for test players.")]
    [SerializeField] private string[] gameModes = { "TeamDeathmatch", "CaptureTheFlag", "FreeForAll" };

    [Tooltip("Automatically enqueue players when scene starts.")]
    [SerializeField] private bool autoEnqueueOnStart = true;
    [Tooltip("Interval (in seconds) to add new players after initial enqueue.")]
    [SerializeField] private float addPlayerInterval = 5f;

    private int nextPlayerId = 1;
    private List<string> enqueuedPlayerIds = new List<string>();


    void Start()
    {
        if (MatchmakingSystem.Instance == null)
        {
            Debug.LogError("MatchmakingSystem instance not found! Make sure it exists in the scene and is set up correctly.");
            enabled = false; // Disable this tester if system is not found
            return;
        }

        // Subscribe to events from the MatchmakingSystem
        MatchmakingSystem.Instance.OnMatchFound += HandleMatchFound;
        MatchmakingSystem.Instance.OnPlayerJoinedQueue += HandlePlayerJoinedQueue;
        MatchmakingSystem.Instance.OnPlayerLeftQueue += HandlePlayerLeftQueue;

        if (autoEnqueueOnStart)
        {
            // Enqueue initial set of players
            for (int i = 0; i < numberOfTestPlayers; i++)
            {
                EnqueueRandomPlayer();
            }
            // Start a coroutine to add more players over time
            StartCoroutine(AddPlayersOverTime());
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when this object is destroyed
        if (MatchmakingSystem.Instance != null)
        {
            MatchmakingSystem.Instance.OnMatchFound -= HandleMatchFound;
            MatchmakingSystem.Instance.OnPlayerJoinedQueue -= HandlePlayerJoinedQueue;
            MatchmakingSystem.Instance.OnPlayerLeftQueue -= HandlePlayerLeftQueue;
        }
    }

    /// <summary>
    /// Event handler for when a match is found by the MatchmakingSystem.
    /// </summary>
    /// <param name="match">The formed Match object.</param>
    private void HandleMatchFound(Match match)
    {
        Debug.Log($"<color=green>MATCH FOUND!</color> {match}");

        foreach (var player in match.Players)
        {
            enqueuedPlayerIds.Remove(player.PlayerId); // Remove from our tracked list
        }

        // In a real game, you would now load a game scene, send players to a server, etc.
        // For this example, we just log it.
    }

    /// <summary>
    /// Event handler for when a player joins the queue.
    /// </summary>
    private void HandlePlayerJoinedQueue(MatchmakingPlayer player)
    {
        Debug.Log($"<color=blue>Player Joined Queue:</color> {player}");
    }

    /// <summary>
    /// Event handler for when a player leaves the queue (either by match found or cancellation).
    /// </summary>
    private void HandlePlayerLeftQueue(MatchmakingPlayer player)
    {
        Debug.Log($"<color=orange>Player Left Queue:</color> {player.PlayerId}");
    }

    /// <summary>
    /// Creates and enqueues a random player with varied skill and game mode.
    /// </summary>
    [ContextMenu("Enqueue Random Player")] // Adds a context menu item in the inspector
    public void EnqueueRandomPlayer()
    {
        string playerId = $"Player_{nextPlayerId++}";
        int skill = Random.Range(minSkill, maxSkill + 1);
        string mode = gameModes[Random.Range(0, gameModes.Length)];

        // Min/Max players desired can be dynamic or fixed based on game.
        // For this example, we use the system's min/max players as the player's preference.
        MatchmakingPlayer newPlayer = new MatchmakingPlayer(playerId, skill, mode, 2, 4); 
        MatchmakingSystem.Instance.EnqueuePlayer(newPlayer);
        enqueuedPlayerIds.Add(playerId);
    }

    /// <summary>
    /// Removes a random player currently in the queue.
    /// </summary>
    [ContextMenu("Dequeue Random Player")]
    public void DequeueRandomPlayer()
    {
        if (enqueuedPlayerIds.Count > 0)
        {
            string playerIdToDequeue = enqueuedPlayerIds[Random.Range(0, enqueuedPlayerIds.Count)];
            MatchmakingSystem.Instance.DequeuePlayer(playerIdToDequeue);
            enqueuedPlayerIds.Remove(playerIdToDequeue);
        }
        else
        {
            Debug.LogWarning("No players in queue to dequeue.");
        }
    }

    /// <summary>
    /// Coroutine to add players periodically after initial enqueue.
    /// </summary>
    private IEnumerator AddPlayersOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(addPlayerInterval);
            EnqueueRandomPlayer();
        }
    }

    // --- Editor GUI for easy testing ---
    void OnGUI()
    {
        GUI.color = Color.cyan;
        GUI.Box(new Rect(10, 10, 250, 150), "Matchmaking Tester");

        GUI.color = Color.white;
        if (GUI.Button(new Rect(20, 40, 230, 30), "Enqueue Random Player"))
        {
            EnqueueRandomPlayer();
        }
        if (GUI.Button(new Rect(20, 75, 230, 30), "Dequeue Random Player"))
        {
            DequeueRandomPlayer();
        }

        GUI.Label(new Rect(20, 110, 230, 20), $"Queue Size: {MatchmakingSystem.Instance?.GetQueueSize() ?? 0}");
        GUI.Label(new Rect(20, 130, 230, 20), $"Players Tracked: {enqueuedPlayerIds.Count}");
    }
}
```

---

### How to Use in Unity:

1.  **Create C# Scripts:**
    *   Create a C# script named `MatchmakingPlayer.cs` and paste the content from section 1.
    *   Create a C# script named `Match.cs` and paste the content from section 2.
    *   Create a C# script named `MatchmakingSystem.cs` and paste the content from section 3.
    *   Create a C# script named `MatchmakingTester.cs` and paste the content from section 4.

2.  **Create Matchmaking GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., named `_GameManagers`).
    *   Attach the `MatchmakingSystem.cs` script to this `_GameManagers` GameObject. This will make it a Singleton, accessible globally.

3.  **Configure MatchmakingSystem:**
    *   Select the `_GameManagers` GameObject.
    *   In the Inspector, adjust the `MatchmakingSystem`'s parameters:
        *   `Min Players Per Match`: e.g., `2` (for 1v1) or `4` (for 2v2).
        *   `Max Players Per Match`: e.g., `2` or `4`.
        *   `Skill Rating Tolerance`: e.g., `50`.
        *   `Matchmaking Interval`: e.g., `3` seconds.
        *   `Tolerance Widen Delay`: e.g., `30` seconds (how long before skill tolerance starts to expand).
        *   `Skill Tolerance Widen Rate`: e.g., `1` (how much skill tolerance widens per second after the delay).

4.  **Create MatchmakingTester GameObject:**
    *   Create another empty GameObject (e.g., named `MatchmakingTester`).
    *   Attach the `MatchmakingTester.cs` script to this GameObject.

5.  **Configure MatchmakingTester:**
    *   Select the `MatchmakingTester` GameObject.
    *   Adjust its parameters in the Inspector:
        *   `Number Of Test Players`: e.g., `10`.
        *   `Min Skill`, `Max Skill`: Define the range for test player skills.
        *   `Game Modes`: Add different game modes like "TeamDeathmatch", "CaptureTheFlag", "FreeForAll".
        *   `Auto Enqueue On Start`: Keep this `true` for automatic testing.
        *   `Add Player Interval`: How often new test players are added.

6.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Observe the Console window. You'll see players joining the queue, the system attempting to find matches, and eventually, "MATCH FOUND!" messages when criteria are met. The OnGUI buttons also allow for manual testing.

This setup provides a robust and educational example of the MatchmakingSystem design pattern in Unity, demonstrating how to manage players, define matching rules, and notify other parts of your game when matches are ready.