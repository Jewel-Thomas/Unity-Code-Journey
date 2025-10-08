// Unity Design Pattern Example: LeaderboardsSystem
// This script demonstrates the LeaderboardsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a 'LeaderboardsSystem' pattern in Unity using C#. The core idea is to have a centralized manager (`LeaderboardsManager`) that handles multiple distinct leaderboards, abstracts away data storage (simulated with `PlayerPrefs` and JSON), and provides an asynchronous API for score submission and retrieval.

**Key Features of the Pattern Demonstrated:**

1.  **Singleton Manager:** `LeaderboardsManager` uses the Singleton pattern for easy global access.
2.  **Multiple Leaderboards:** Supports different leaderboards identified by unique IDs (e.g., "HighScores", "Level1FastestTimes").
3.  **Asynchronous Operations:** Uses `async/await` to simulate network latency, which is typical for real leaderboard interactions.
4.  **Data Persistence:** Saves and loads leaderboard data using `PlayerPrefs` and JSON serialization (using Newtonsoft.Json-for-Unity for robustness).
5.  **Score Entry Structure:** A `ScoreEntry` class holds player name, score, timestamp, and rank, with built-in sorting logic.
6.  **Decoupling:** The `LeaderboardsManager` handles all data logic, allowing UI or game logic scripts to interact with it via a clean API without needing to know implementation details.

---

### Setup Instructions:

1.  **Create a C# Script:** Create a new C# script in your Unity project called `LeaderboardsSystem.cs`.
2.  **Install Newtonsoft.Json-for-Unity:**
    *   Go to `Window > Package Manager`.
    *   Click the `+` icon in the top-left corner.
    *   Select `Add package by name...`.
    *   Enter `com.unity.nuget.newtonsoft.json` and click `Add`.
    *   *Alternatively:* You can download the package from the GitHub repository (`jilleJr/Newtonsoft.Json-for-Unity`) and import it.
3.  **Paste the Code:** Copy and paste the entire code below into your `LeaderboardsSystem.cs` file.
4.  **Create Manager GameObject:** In your first Unity scene (e.g., your "Splash" or "MainMenu" scene), create an empty GameObject. Rename it to `LeaderboardsManager`.
5.  **Attach Script:** Drag and drop the `LeaderboardsSystem.cs` script onto the `LeaderboardsManager` GameObject.
6.  **Test Script:** To see it in action, create another empty GameObject (e.g., `GameManager`) and a new C# script (e.g., `GameController.cs`). Paste the "EXAMPLE USAGE" section (at the bottom of the `LeaderboardsManager` script, **excluding** the commented-out `LeaderboardsManager` class itself) into `GameController.cs`, uncomment it, and attach it to your `GameManager` GameObject.
7.  **Run:** Play your scene. Watch the Console for output demonstrating score submission and retrieval. You can also modify the `GameController`'s inspector fields to test different scores/players/leaderboards.

---

### `LeaderboardsSystem.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Required for async/await operations

// IMPORTANT: This example uses Newtonsoft.Json-for-Unity for robust JSON serialization.
// To use it, you need to install it:
// 1. Go to Window > Package Manager.
// 2. Click the '+' icon in the top-left corner.
// 3. Select 'Add package by name...'.
// 4. Enter 'com.unity.nuget.newtonsoft.json' and click 'Add'.
//    Alternatively, download from GitHub: https://github.com/jilleJr/Newtonsoft.Json-for-Unity
using Newtonsoft.Json;

/// <summary>
/// Represents a single score entry on a leaderboard.
/// Implements IComparable to allow sorting by score (descending) and then timestamp (ascending) for ties.
/// </summary>
[Serializable]
public class ScoreEntry : IComparable<ScoreEntry>
{
    // Properties are public set for JSON deserialization, but private set for creation
    public string PlayerName { get; private set; }
    public long Score { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int Rank { get; set; } // Rank is assigned dynamically when retrieving/sorting

    // Parameterless constructor required for JSON deserialization
    public ScoreEntry() { }

    /// <summary>
    /// Creates a new score entry.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="score">The score achieved.</param>
    public ScoreEntry(string playerName, long score)
    {
        // Default to "Anonymous" if player name is empty or whitespace
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Anonymous";
        }
        PlayerName = playerName;
        Score = score;
        Timestamp = DateTime.UtcNow; // Record the submission time (UTC is best for consistency)
    }

    /// <summary>
    /// Compares this score entry with another for sorting.
    /// Primary sort: Score (descending).
    /// Secondary sort: Timestamp (ascending) for ties (earlier submission wins ties).
    /// </summary>
    public int CompareTo(ScoreEntry other)
    {
        if (other == null) return 1; // Null is always "less" (i.e., this one comes after)

        // Compare scores in descending order (higher score first)
        int scoreComparison = other.Score.CompareTo(this.Score);
        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        // If scores are equal, compare timestamps in ascending order (earlier submission first)
        return this.Timestamp.CompareTo(other.Timestamp);
    }

    /// <summary>
    /// Provides a user-friendly string representation of the score entry.
    /// </summary>
    public override string ToString()
    {
        return $"#{Rank} {PlayerName}: {Score} ({Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss})";
    }
}

/// <summary>
/// Represents the data for a single leaderboard.
/// Manages a list of ScoreEntry objects and handles their sorting.
/// </summary>
[Serializable]
public class LeaderboardData
{
    // The list of scores for this specific leaderboard.
    // Public set for JSON deserialization, but managed internally otherwise.
    public List<ScoreEntry> Scores { get; set; } = new List<ScoreEntry>();

    // Parameterless constructor required for JSON deserialization
    public LeaderboardData() { }

    /// <summary>
    /// Adds a new score entry to this leaderboard and re-sorts the entire list.
    /// </summary>
    /// <param name="newEntry">The ScoreEntry to add.</param>
    public void AddScore(ScoreEntry newEntry)
    {
        Scores.Add(newEntry);
        SortAndRankScores(); // Re-sort and re-rank after adding
    }

    /// <summary>
    /// Sorts the internal list of scores and assigns ranks based on the sorting order.
    /// This ensures the list is always ordered correctly and ranks are up-to-date.
    /// </summary>
    private void SortAndRankScores()
    {
        Scores.Sort(); // Uses the ScoreEntry.CompareTo method for sorting

        // After sorting, iterate to assign ranks
        for (int i = 0; i < Scores.Count; i++)
        {
            Scores[i].Rank = i + 1; // Ranks are 1-based
        }
    }

    /// <summary>
    /// Retrieves a specified number of top scores from the leaderboard.
    /// </summary>
    /// <param name="count">The maximum number of scores to retrieve.</param>
    /// <returns>A list of the top ScoreEntry objects.</returns>
    public List<ScoreEntry> GetTopScores(int count)
    {
        SortAndRankScores(); // Ensure the list is sorted and ranked before retrieving
        return Scores.Take(count).ToList(); // Use LINQ to get the top 'count' entries
    }

    /// <summary>
    /// Retrieves a specific player's score entry from this leaderboard.
    /// </summary>
    /// <param name="playerName">The name of the player to search for.</param>
    /// <returns>The ScoreEntry of the player, or null if not found.</returns>
    public ScoreEntry GetPlayerScoreEntry(string playerName)
    {
        SortAndRankScores(); // Ensure the list is sorted and ranked
        // Use OrdinalIgnoreCase for case-insensitive player name matching
        return Scores.FirstOrDefault(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// The central manager for all leaderboards in the game.
/// Implements the Singleton pattern to ensure only one instance exists throughout the game lifecycle.
/// This manager handles:
/// - Initializing and persisting leaderboard data (simulated with PlayerPrefs).
/// - Submitting new scores to specific leaderboards.
/// - Retrieving scores from specific leaderboards.
/// - Simulating network latency for asynchronous operations.
/// </summary>
public class LeaderboardsManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // The static instance property ensures easy global access to the manager.
    public static LeaderboardsManager Instance { get; private set; }

    // --- Internal Data Structure ---
    // A dictionary to hold multiple distinct leaderboards. Each leaderboard is identified by a string ID.
    // This is the core of the 'LeaderboardsSystem' pattern, allowing games to have "HighScores",
    // "FastestTimes_Level1", "MostKills_Weekly", etc., all managed centrally.
    private Dictionary<string, LeaderboardData> _leaderboards = new Dictionary<string, LeaderboardData>();

    // --- Persistence Configuration ---
    // Key used to store/retrieve all leaderboard data from Unity's PlayerPrefs.
    private const string LeaderboardsPlayerPrefsKey = "GameLeaderboardsData";

    [Tooltip("Delay in milliseconds to simulate network latency for asynchronous operations.")]
    [SerializeField]
    private int _simulatedNetworkDelayMs = 500; // Default to 0.5 seconds of simulated latency

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton pattern and loads existing leaderboard data.
    /// </summary>
    void Awake()
    {
        // Enforce Singleton: If another instance already exists, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set this instance as the Singleton.
        Instance = this;
        // Keep this GameObject alive across scene loads, as it's a critical manager.
        DontDestroyOnLoad(gameObject);

        // Load all leaderboards from PlayerPrefs when the game starts.
        LoadLeaderboards();
        Debug.Log("[LeaderboardsManager] Initialized and loaded leaderboards.");
    }

    /// <summary>
    /// Submits a new score to a specified leaderboard.
    /// This method is asynchronous to simulate a real-world scenario where score submission
    /// might involve network requests to a backend server.
    /// </summary>
    /// <param name="leaderboardId">The unique identifier for the leaderboard (e.g., "GlobalHighScores", "Level1Times").</param>
    /// <param name="playerName">The name of the player submitting the score.</param>
    /// <param name="score">The score value to submit.</param>
    /// <returns>A Task<bool> representing the asynchronous operation, returning true if submission was successful.</returns>
    public async Task<bool> SubmitScoreAsync(string leaderboardId, string playerName, long score)
    {
        // Simulate network latency (e.g., waiting for server response)
        await Task.Delay(_simulatedNetworkDelayMs);

        if (string.IsNullOrWhiteSpace(leaderboardId))
        {
            Debug.LogError("[LeaderboardsManager] Leaderboard ID cannot be null or empty for score submission.");
            return false;
        }

        // If the leaderboard doesn't exist yet, create it.
        if (!_leaderboards.ContainsKey(leaderboardId))
        {
            _leaderboards.Add(leaderboardId, new LeaderboardData());
            Debug.Log($"[LeaderboardsManager] Created new leaderboard: '{leaderboardId}'.");
        }

        // Create a new score entry and add it to the specific leaderboard.
        ScoreEntry newEntry = new ScoreEntry(playerName, score);
        _leaderboards[leaderboardId].AddScore(newEntry);

        SaveLeaderboards(); // Persist the updated data immediately.

        Debug.Log($"[LeaderboardsManager] Submitted score to '{leaderboardId}': {playerName} - {score}.");
        return true;
    }

    /// <summary>
    /// Retrieves a list of top scores from a specified leaderboard.
    /// This method is asynchronous to simulate fetching data from a backend server.
    /// </summary>
    /// <param name="leaderboardId">The unique identifier for the leaderboard.</param>
    /// <param name="count">The maximum number of top scores to retrieve.</param>
    /// <returns>A Task<List<ScoreEntry>> representing the asynchronous operation, returning a list of scores.</returns>
    public async Task<List<ScoreEntry>> GetLeaderboardScoresAsync(string leaderboardId, int count)
    {
        // Simulate network latency.
        await Task.Delay(_simulatedNetworkDelayMs);

        if (string.IsNullOrWhiteSpace(leaderboardId))
        {
            Debug.LogError("[LeaderboardsManager] Leaderboard ID cannot be null or empty for score retrieval.");
            return new List<ScoreEntry>();
        }

        // Try to get the LeaderboardData for the specified ID.
        if (_leaderboards.TryGetValue(leaderboardId, out LeaderboardData data))
        {
            return data.GetTopScores(count); // Return the top 'count' scores.
        }
        else
        {
            Debug.LogWarning($"[LeaderboardsManager] Leaderboard '{leaderboardId}' not found or has no scores.");
            return new List<ScoreEntry>(); // Return an empty list if the leaderboard doesn't exist.
        }
    }

    /// <summary>
    /// Retrieves a specific player's score entry from a specified leaderboard.
    /// </summary>
    /// <param name="leaderboardId">The unique identifier for the leaderboard.</param>
    /// <param name="playerName">The name of the player whose score to retrieve.</param>
    /// <returns>A Task<ScoreEntry> representing the asynchronous operation, returning the player's score entry or null if not found.</returns>
    public async Task<ScoreEntry> GetPlayerScoreEntryAsync(string leaderboardId, string playerName)
    {
        // Simulate network latency.
        await Task.Delay(_simulatedNetworkDelayMs);

        if (string.IsNullOrWhiteSpace(leaderboardId) || string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("[LeaderboardsManager] Leaderboard ID and Player Name cannot be null or empty for player score retrieval.");
            return null;
        }

        if (_leaderboards.TryGetValue(leaderboardId, out LeaderboardData data))
        {
            return data.GetPlayerScoreEntry(playerName);
        }
        else
        {
            Debug.LogWarning($"[LeaderboardsManager] Leaderboard '{leaderboardId}' not found or has no scores.");
            return null;
        }
    }

    /// <summary>
    /// Clears all scores from a specific leaderboard.
    /// This is typically used for administrative purposes or during development/testing.
    /// </summary>
    /// <param name="leaderboardId">The unique identifier for the leaderboard to clear.</param>
    public async Task ClearLeaderboardAsync(string leaderboardId)
    {
        await Task.Delay(_simulatedNetworkDelayMs); // Simulate latency

        if (_leaderboards.ContainsKey(leaderboardId))
        {
            _leaderboards[leaderboardId] = new LeaderboardData(); // Replace with a new empty leaderboard
            SaveLeaderboards(); // Persist the change
            Debug.Log($"[LeaderboardsManager] Leaderboard '{leaderboardId}' cleared.");
        }
        else
        {
            Debug.LogWarning($"[LeaderboardsManager] Leaderboard '{leaderboardId}' not found. Cannot clear.");
        }
    }

    /// <summary>
    /// Completely clears all leaderboards and their data from persistence.
    /// Use with extreme caution, as this will reset all leaderboard progress.
    /// </summary>
    public async Task ClearAllLeaderboardsAsync()
    {
        await Task.Delay(_simulatedNetworkDelayMs); // Simulate latency
        _leaderboards.Clear(); // Clear the in-memory dictionary
        PlayerPrefs.DeleteKey(LeaderboardsPlayerPrefsKey); // Delete the data from PlayerPrefs
        PlayerPrefs.Save(); // Ensure changes are written to disk
        Debug.Log("[LeaderboardsManager] All leaderboards data cleared from persistence.");
    }

    /// <summary>
    /// Saves all current leaderboard data to Unity's PlayerPrefs as a JSON string.
    /// In a real production game, this would typically involve saving to a secure backend database (e.g., PlayFab, custom server).
    /// PlayerPrefs is used here for simplicity and local persistence demonstration.
    /// </summary>
    private void SaveLeaderboards()
    {
        try
        {
            // Serialize the entire dictionary of leaderboards into a JSON string.
            // Formatting.Indented makes the JSON human-readable, good for debugging.
            string jsonData = JsonConvert.SerializeObject(_leaderboards, Formatting.Indented);
            PlayerPrefs.SetString(LeaderboardsPlayerPrefsKey, jsonData);
            PlayerPrefs.Save(); // Explicitly save to disk (important after SetString)
            // Debug.Log("[LeaderboardsManager] Leaderboards saved successfully."); // Can be verbose if called often
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardsManager] Failed to save leaderboards: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads all leaderboard data from Unity's PlayerPrefs.
    /// If no data is found, initializes an empty set of leaderboards.
    /// Handles potential deserialization errors.
    /// </summary>
    private void LoadLeaderboards()
    {
        if (PlayerPrefs.HasKey(LeaderboardsPlayerPrefsKey))
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(LeaderboardsPlayerPrefsKey);
                // Deserialize the JSON string back into our dictionary of leaderboards.
                _leaderboards = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(jsonData);

                // IMPORTANT: After deserialization, ensure lists are not null and scores are re-sorted/re-ranked.
                // This handles cases where data might have been corrupted or the ScoreEntry structure changed.
                foreach (var kvp in _leaderboards)
                {
                    if (kvp.Value.Scores == null)
                    {
                        kvp.Value.Scores = new List<ScoreEntry>();
                    }
                    // Re-sort and re-rank to ensure consistency and assign correct ranks after loading
                    kvp.Value.Scores.Sort(); // Uses ScoreEntry.CompareTo
                    for (int i = 0; i < kvp.Value.Scores.Count; i++)
                    {
                        kvp.Value.Scores[i].Rank = i + 1;
                    }
                }
                Debug.Log("[LeaderboardsManager] Leaderboards loaded successfully from PlayerPrefs.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardsManager] Failed to load leaderboards: {ex.Message}. Starting with empty leaderboards.");
                _leaderboards = new Dictionary<string, LeaderboardData>(); // Reset if loading fails
            }
        }
        else
        {
            Debug.Log("[LeaderboardsManager] No existing leaderboard data found in PlayerPrefs. Starting fresh.");
            _leaderboards = new Dictionary<string, LeaderboardData>(); // Initialize empty if no data exists
        }
    }
}

/*
// --- EXAMPLE USAGE IN ANOTHER SCRIPT (e.g., 'GameController.cs') ---
// Attach this script to any GameObject in your scene (e.g., a "GameManager" GameObject)
// Ensure 'using System.Threading.Tasks;' is at the top of your script.

using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks; // Required for async/await

public class GameController : MonoBehaviour
{
    [Header("Leaderboard Settings")]
    [SerializeField] private string _leaderboardIdHighScores = "HighScores";
    [SerializeField] private string _leaderboardIdLevelTimes = "Level1FastestTimes";
    [SerializeField] private int _scoresToDisplay = 5;

    [Header("Test Data (can be changed in Inspector)")]
    [SerializeField] private string _testPlayerName = "HeroPlayer";
    [SerializeField] private long _testHighScore = 12345;
    [SerializeField] private long _testLevelTimeMs = 30000; // 30 seconds

    // --- Unity Lifecycle Methods ---
    void Start()
    {
        // Always check if the LeaderboardsManager instance is available before using it.
        // It should be created in the first scene and marked with DontDestroyOnLoad.
        if (LeaderboardsManager.Instance == null)
        {
            Debug.LogError("LeaderboardsManager not found in scene! Make sure it's on a GameObject in your first scene.");
            return;
        }

        Debug.Log("--- GameController Starting Leaderboard Operations ---");

        // Example Sequence:
        // 1. Submit some scores to different leaderboards
        SimulateScoreSubmissions();

        // 2. Display the leaderboards
        Invoke("DisplayAllLeaderboards", 2f); // Add a small delay for clarity after submissions
    }

    // --- Core Interaction Methods ---

    /// <summary>
    /// Simulates game events that lead to score submissions.
    /// </summary>
    async void SimulateScoreSubmissions()
    {
        // Submit scores to the "HighScores" leaderboard
        Debug.Log("\n--- Submitting High Scores ---");
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, _testPlayerName, _testHighScore);
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Warrior", 10000);
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Mage", 15000);
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, _testPlayerName, _testHighScore + 500); // Player submits a new higher score
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Rogue", 8000);
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Cleric", 11000);
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Warrior", 10500); // Warrior beats their old score
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdHighScores, "Newbie", 5000);

        // Submit scores to the "Level1FastestTimes" leaderboard
        Debug.Log("\n--- Submitting Level 1 Times ---");
        // For times, lower score (time) is usually better, but our CompareTo sorts descending for score, so submit as negative for now
        // Or, more correctly, you might store "time in ms" and implement a separate sorting logic for time leaderboards.
        // For simplicity, let's treat it as a high score for now, but in a real system, you'd handle "min-score-wins" explicitly.
        // For this example, let's treat lower time as higher "score" for submission, so sorting works.
        // A better design would be a LeaderboardType enum (Score_HighIsBetter, Time_LowIsBetter).
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdLevelTimes, _testPlayerName, 60000 - _testLevelTimeMs); // Example: 60s max, player gets 30s
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdLevelTimes, "Speedster", 60000 - 15000); // 15s
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdLevelTimes, "Runner", 60000 - 25000); // 25s
        await LeaderboardsManager.Instance.SubmitScoreAsync(_leaderboardIdLevelTimes, _testPlayerName, 60000 - (_testLevelTimeMs - 5000)); // Player improves time
    }

    /// <summary>
    /// Retrieves and displays scores for various leaderboards.
    /// </summary>
    async void DisplayAllLeaderboards()
    {
        Debug.Log("\n--- Displaying Leaderboards ---");

        // Display High Scores
        await DisplayLeaderboard(_leaderboardIdHighScores);

        // Display Level 1 Times
        await DisplayLeaderboard(_leaderboardIdLevelTimes);

        // Retrieve and display a specific player's entry
        await DisplayPlayerScore(_leaderboardIdHighScores, _testPlayerName);
        await DisplayPlayerScore(_leaderboardIdLevelTimes, "Speedster");

        // Example of clearing a leaderboard (uncomment to use)
        // await ClearSpecificLeaderboard(_leaderboardIdHighScores);
        // await ClearAllLeaderboards(); // Use with extreme caution!
    }

    /// <summary>
    /// Helper method to retrieve and print scores for a given leaderboard ID.
    /// </summary>
    /// <param name="leaderboardId">The ID of the leaderboard to display.</param>
    async Task DisplayLeaderboard(string leaderboardId)
    {
        Debug.Log($"\n--- Top {_scoresToDisplay} Scores for '{leaderboardId}' ---");
        List<ScoreEntry> scores = await LeaderboardsManager.Instance.GetLeaderboardScoresAsync(leaderboardId, _scoresToDisplay);

        if (scores.Any())
        {
            foreach (var entry in scores)
            {
                Debug.Log(entry.ToString()); // Uses the overridden ToString() for formatted output
            }
        }
        else
        {
            Debug.Log($"No scores available for leaderboard '{leaderboardId}'.");
        }
    }

    /// <summary>
    /// Helper method to retrieve and print a specific player's score for a given leaderboard.
    /// </summary>
    async Task DisplayPlayerScore(string leaderboardId, string playerName)
    {
        Debug.Log($"\n--- Player '{playerName}' on '{leaderboardId}' ---");
        ScoreEntry playerEntry = await LeaderboardsManager.Instance.GetPlayerScoreEntryAsync(leaderboardId, playerName);

        if (playerEntry != null)
        {
            Debug.Log(playerEntry.ToString());
        }
        else
        {
            Debug.Log($"Player '{playerName}' not found on leaderboard '{leaderboardId}'.");
        }
    }

    /// <summary>
    /// Helper method to clear a specific leaderboard.
    /// </summary>
    async Task ClearSpecificLeaderboard(string leaderboardId)
    {
        Debug.Log($"\n--- Clearing Leaderboard: '{leaderboardId}' ---");
        await LeaderboardsManager.Instance.ClearLeaderboardAsync(leaderboardId);
        await DisplayLeaderboard(leaderboardId); // Show that it's empty now
    }

    /// <summary>
    /// Helper method to clear ALL leaderboards.
    /// </summary>
    async Task ClearAllLeaderboards()
    {
        Debug.Log("\n--- Clearing ALL Leaderboards ---");
        await LeaderboardsManager.Instance.ClearAllLeaderboardsAsync();
        // You might want to re-display or refresh UI after this
        Debug.Log("All leaderboards have been cleared.");
    }

    // --- Public Methods for UI/Game Logic (e.g., connected to Buttons) ---

    // Example of how you might wire this up to UI buttons:
    public void OnClickSubmitHighScore()
    {
        // In a real game, you'd get the player name and score from UI input fields or game state.
        SubmitScore(_leaderboardIdHighScores, _testPlayerName, _testHighScore + UnityEngine.Random.Range(0, 5000));
    }

    public void OnClickDisplayHighScores()
    {
        DisplayLeaderboard(_leaderboardIdHighScores);
    }

    public void OnClickClearHighScores()
    {
        ClearSpecificLeaderboard(_leaderboardIdHighScores);
    }

    // Generic submission method that can be called from other game logic
    public async void SubmitScore(string leaderboardId, string playerName, long score)
    {
        if (LeaderboardsManager.Instance == null) return;
        bool success = await LeaderboardsManager.Instance.SubmitScoreAsync(leaderboardId, playerName, score);
        if (success)
        {
            Debug.Log($"UI: Score '{score}' for '{playerName}' submitted to '{leaderboardId}'.");
            // Optionally, refresh UI here
            // DisplayLeaderboard(leaderboardId);
        }
        else
        {
            Debug.LogError($"UI: Failed to submit score to '{leaderboardId}'.");
        }
    }
}
*/
```