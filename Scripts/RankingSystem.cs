// Unity Design Pattern Example: RankingSystem
// This script demonstrates the RankingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a practical **RankingSystem** design pattern in Unity. It manages a list of player scores, allows adding/updating them, retrieves sorted lists, and persists the data using JSON.

The core idea of this "RankingSystem" pattern (it's more of an architectural component than a classical GoF pattern) is to centralize the logic for managing a collection of ranked items. It handles:
1.  **Data Representation:** How an individual ranked item (e.g., `PlayerScoreEntry`) is stored.
2.  **Collection Management:** How multiple ranked items are stored, added, updated, and retrieved.
3.  **Sorting Logic:** How items are ordered (e.g., highest score first).
4.  **Persistence:** How the data is saved and loaded across game sessions.
5.  **Notification:** How other parts of the game (e.g., UI) are informed of changes.

---

### **`RankingSystem.cs`**

To use this:
1.  Create a new C# script named `RankingSystem.cs` in your Unity project.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., named `GameManager` or `RankingManager`).
4.  Attach the `RankingSystem` script to this GameObject.
5.  The system will automatically initialize, load rankings (if any), and save them when the application quits.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For sorting and querying
using System.IO;   // For file operations
using UnityEngine.Events; // For custom Unity events

/// <summary>
/// Represents a single entry in the ranking system.
/// Implements IComparable to define default sorting behavior (higher score first).
/// </summary>
[System.Serializable] // Makes this class serializable by Unity's JsonUtility
public class PlayerScoreEntry : IComparable<PlayerScoreEntry>
{
    public string PlayerName;
    public int Score;
    public DateTime EntryDate; // When the score was recorded

    public PlayerScoreEntry(string playerName, int score)
    {
        PlayerName = playerName;
        Score = score;
        EntryDate = DateTime.Now; // Record the current time
    }

    /// <summary>
    /// Compares this entry with another for sorting purposes.
    /// Default sort order: Higher scores come first.
    /// If scores are equal, earlier entries (older dates) come first.
    /// </summary>
    /// <param name="other">The other PlayerScoreEntry to compare with.</param>
    /// <returns>
    /// A negative integer if this instance precedes the other.
    /// A positive integer if this instance follows the other.
    /// Zero if this instance has the same position in the sort order as the other.
    /// </returns>
    public int CompareTo(PlayerScoreEntry other)
    {
        if (other == null) return 1; // This instance is greater than a null object.

        // Primary sort: Descending by score (higher score is better)
        int scoreComparison = other.Score.CompareTo(Score);
        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        // Secondary sort: Ascending by date (earlier entry date is better for ties)
        return EntryDate.CompareTo(other.EntryDate);
    }

    public override string ToString()
    {
        return $"{PlayerName}: {Score} (Recorded: {EntryDate.ToShortDateString()} {EntryDate.ToShortTimeString()})";
    }
}

/// <summary>
/// A wrapper class required for JsonUtility to serialize a List<T>.
/// JsonUtility cannot directly serialize root-level Lists or arrays.
/// </summary>
[System.Serializable]
public class ScoreDataWrapper
{
    public List<PlayerScoreEntry> playerScores = new List<PlayerScoreEntry>();
}

/// <summary>
/// The core RankingSystem pattern implementation.
/// Manages player scores, provides sorting, persistence, and event notifications.
/// </summary>
public class RankingSystem : MonoBehaviour
{
    // Singleton pattern for easy global access to the RankingSystem
    public static RankingSystem Instance { get; private set; }

    [Tooltip("The maximum number of scores to keep in the ranking. Set to 0 for unlimited.")]
    [SerializeField] private int maxRankingEntries = 10;

    [Tooltip("The file name for saving and loading ranking data.")]
    [SerializeField] private string saveFileName = "player_rankings.json";

    // Internal list to store all player score entries.
    private List<PlayerScoreEntry> _playerScores = new List<PlayerScoreEntry>();

    // Full path to the save file.
    private string _saveFilePath;

    // Event invoked when the ranking data changes (e.g., score added/updated, cleared).
    // Other scripts can subscribe to this to update UI or trigger other logic.
    public UnityEvent OnRankingChanged = new UnityEvent();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and loads existing scores.
    /// </summary>
    private void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the RankingSystem alive across scenes
        }
        else
        {
            // If another instance already exists, destroy this one.
            Destroy(gameObject);
            return;
        }

        // Determine the save file path
        _saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log($"RankingSystem initialized. Save path: {_saveFilePath}");

        LoadScores(); // Load scores when the system starts
    }

    /// <summary>
    /// Called when the application quits or the GameObject is destroyed.
    /// Ensures scores are saved before the application closes.
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveScores();
    }

    /// <summary>
    /// Adds a new score or updates an existing player's score if the new score is higher.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="score">The score achieved by the player.</param>
    public void AddOrUpdateScore(string playerName, int score)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("Player name cannot be empty or null.");
            return;
        }

        PlayerScoreEntry existingEntry = _playerScores.Find(e => e.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        if (existingEntry != null)
        {
            // Player exists: Update score only if the new score is higher
            if (score > existingEntry.Score)
            {
                existingEntry.Score = score;
                existingEntry.EntryDate = DateTime.Now; // Update date to reflect new best score
                Debug.Log($"Updated score for {playerName} to {score}.");
                SortAndMaintainRankings();
                OnRankingChanged.Invoke(); // Notify listeners of change
                SaveScores();
            }
            else
            {
                Debug.Log($"Score {score} for {playerName} is not higher than existing {existingEntry.Score}. No update needed.");
            }
        }
        else
        {
            // New player: Add a new entry
            PlayerScoreEntry newEntry = new PlayerScoreEntry(playerName, score);
            _playerScores.Add(newEntry);
            Debug.Log($"Added new score for {playerName}: {score}.");
            SortAndMaintainRankings();
            OnRankingChanged.Invoke(); // Notify listeners of change
            SaveScores();
        }
    }

    /// <summary>
    /// Retrieves a read-only list of the top 'count' scores.
    /// The list is already sorted in descending order of score.
    /// </summary>
    /// <param name="count">The maximum number of top scores to retrieve.</param>
    /// <returns>An IReadOnlyList of PlayerScoreEntry objects.</returns>
    public IReadOnlyList<PlayerScoreEntry> GetTopScores(int count)
    {
        // Ensure the internal list is sorted before returning.
        // It should already be sorted by SortAndMaintainRankings, but this adds robustness.
        // We also create a new list to prevent external modification of our internal list.
        return _playerScores.Take(count).ToList().AsReadOnly();
    }

    /// <summary>
    /// Retrieves a read-only list of all current scores.
    /// The list is already sorted in descending order of score.
    /// </summary>
    /// <returns>An IReadOnlyList of all PlayerScoreEntry objects.</returns>
    public IReadOnlyList<PlayerScoreEntry> GetAllScores()
    {
        // Similar to GetTopScores, return a read-only copy.
        return _playerScores.AsReadOnly();
    }

    /// <summary>
    /// Clears all scores from the ranking system.
    /// </summary>
    public void ClearScores()
    {
        _playerScores.Clear();
        Debug.Log("All scores cleared.");
        OnRankingChanged.Invoke(); // Notify listeners of change
        SaveScores();
    }

    /// <summary>
    /// Sorts the internal list of scores and prunes it to maxRankingEntries if specified.
    /// </summary>
    private void SortAndMaintainRankings()
    {
        // Use the CompareTo implementation of PlayerScoreEntry for sorting
        _playerScores.Sort();

        // If a max number of entries is set, prune the list
        if (maxRankingEntries > 0 && _playerScores.Count > maxRankingEntries)
        {
            // Remove entries beyond the max limit. Since it's sorted, these are the lowest scores.
            _playerScores.RemoveRange(maxRankingEntries, _playerScores.Count - maxRankingEntries);
            Debug.Log($"Ranking pruned to {maxRankingEntries} entries.");
        }
    }

    /// <summary>
    /// Loads player scores from the save file.
    /// Uses Unity's JsonUtility for simple JSON serialization.
    /// </summary>
    private void LoadScores()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(_saveFilePath);
                ScoreDataWrapper data = JsonUtility.FromJson<ScoreDataWrapper>(json);
                if (data != null && data.playerScores != null)
                {
                    _playerScores = data.playerScores;
                    SortAndMaintainRankings(); // Ensure loaded scores are sorted
                    Debug.Log($"Successfully loaded {_playerScores.Count} scores from {_saveFilePath}");
                    OnRankingChanged.Invoke(); // Notify listeners that data has been loaded
                }
                else
                {
                    Debug.LogWarning($"Ranking file {_saveFilePath} was empty or malformed. Starting with empty rankings.");
                    _playerScores = new List<PlayerScoreEntry>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load ranking data from {_saveFilePath}: {e.Message}");
                _playerScores = new List<PlayerScoreEntry>(); // Clear list on error
            }
        }
        else
        {
            Debug.Log($"No ranking save file found at {_saveFilePath}. Starting with empty rankings.");
            _playerScores = new List<PlayerScoreEntry>();
        }
    }

    /// <summary>
    /// Saves the current player scores to the save file.
    /// Uses Unity's JsonUtility for simple JSON serialization.
    /// </summary>
    private void SaveScores()
    {
        try
        {
            ScoreDataWrapper data = new ScoreDataWrapper { playerScores = _playerScores };
            string json = JsonUtility.ToJson(data, true); // true for pretty print
            File.WriteAllText(_saveFilePath, json);
            // Debug.Log($"Successfully saved {_playerScores.Count} scores to {_saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save ranking data to {_saveFilePath}: {e.Message}");
        }
    }
}


/*
=====================================================================================
EXAMPLE USAGE:
=====================================================================================

To use the RankingSystem from another script (e.g., a Game Controller or a UI Manager):

1.  **Ensure RankingSystem is in your scene:**
    Make sure you have an empty GameObject (e.g., "GameManager") with the `RankingSystem.cs` script attached.

2.  **Accessing the system:**
    You can access the `RankingSystem` via its `Instance` property from any script.

Example `GameLogic.cs` script for adding scores:
-------------------------------------------------
*/
/*
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private void Start()
    {
        // Example of adding or updating scores
        Debug.Log("--- Adding/Updating Scores ---");
        RankingSystem.Instance.AddOrUpdateScore("Alice", 1500);
        RankingSystem.Instance.AddOrUpdateScore("Bob", 2000);
        RankingSystem.Instance.AddOrUpdateScore("Charlie", 1200);
        RankingSystem.Instance.AddOrUpdateScore("Alice", 1700); // Alice's score improves
        RankingSystem.Instance.AddOrUpdateScore("David", 2500);
        RankingSystem.Instance.AddOrUpdateScore("Charlie", 1300); // Charlie's score improves
        RankingSystem.Instance.AddOrUpdateScore("Eve", 1000);
        RankingSystem.Instance.AddOrUpdateScore("Bob", 1900); // Bob's score doesn't improve (1900 < 2000)
        RankingSystem.Instance.AddOrUpdateScore("Frank", 3000);
        RankingSystem.Instance.AddOrUpdateScore("Grace", 1800);
        RankingSystem.Instance.AddOrUpdateScore("Harry", 2200);
        RankingSystem.Instance.AddOrUpdateScore("Ivy", 1600);
        RankingSystem.Instance.AddOrUpdateScore("Jack", 2800);

        // You can also add a small delay to simulate game progression or different score submissions
        Invoke("AddMoreScoresDelayed", 2f);
    }

    private void AddMoreScoresDelayed()
    {
        Debug.Log("\n--- Adding More Scores (Delayed) ---");
        RankingSystem.Instance.AddOrUpdateScore("Liam", 3500);
        RankingSystem.Instance.AddOrUpdateScore("Mia", 2100);
        RankingSystem.Instance.AddOrUpdateScore("Nora", 1950);
        RankingSystem.Instance.AddOrUpdateScore("David", 2700); // David's score improves
        RankingSystem.Instance.AddOrUpdateScore("Frank", 3100); // Frank's score improves
    }

    // You could call AddOrUpdateScore after a game round finishes, like this:
    public void OnGameRoundEnd(string playerName, int finalScore)
    {
        RankingSystem.Instance.AddOrUpdateScore(playerName, finalScore);
        Debug.Log($"Game round ended. {playerName} scored {finalScore}. Ranking updated.");
    }
}
*/

/*
Example `UIManager.cs` script for displaying scores and listening to changes:
-------------------------------------------------------------------------
This example assumes you have some UI Text elements or similar to display the ranks.
You would typically update a UI List (e.g., using a ScrollView and prefabs) here.
For simplicity, this just logs to console.
*/
/*
using UnityEngine;
using UnityEngine.UI; // If you're updating UI Text directly
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    // Public reference to a UI Text component (if you want to display directly)
    // [SerializeField] private Text rankingDisplayText; 

    private void OnEnable()
    {
        // Subscribe to the OnRankingChanged event
        if (RankingSystem.Instance != null)
        {
            RankingSystem.Instance.OnRankingChanged.AddListener(UpdateRankingDisplay);
            // Initial display in case scores were loaded before this script enabled
            UpdateRankingDisplay(); 
        }
        else
        {
            Debug.LogError("RankingSystem.Instance is null. Make sure it's in the scene and initialized.");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks when this object is disabled or destroyed
        if (RankingSystem.Instance != null)
        {
            RankingSystem.Instance.OnRankingChanged.RemoveListener(UpdateRankingDisplay);
        }
    }

    /// <summary>
    /// This method is called whenever the ranking system's data changes.
    /// It retrieves the latest scores and updates the UI (or logs to console here).
    /// </summary>
    public void UpdateRankingDisplay()
    {
        Debug.Log("\n--- RANKING DISPLAY UPDATED ---");

        // Get the top 5 scores
        IReadOnlyList<PlayerScoreEntry> topScores = RankingSystem.Instance.GetTopScores(5);

        if (topScores.Count == 0)
        {
            Debug.Log("No scores recorded yet.");
            // if (rankingDisplayText != null) rankingDisplayText.text = "No scores yet.";
            return;
        }

        string displayString = "--- Top Scores ---\n";
        for (int i = 0; i < topScores.Count; i++)
        {
            displayString += $"{i + 1}. {topScores[i].PlayerName}: {topScores[i].Score}\n";
        }

        Debug.Log(displayString);
        // if (rankingDisplayText != null) rankingDisplayText.text = displayString;

        // Example of getting ALL scores (not just top N)
        // IReadOnlyList<PlayerScoreEntry> allScores = RankingSystem.Instance.GetAllScores();
        // Debug.Log("\n--- All Scores ---");
        // foreach (var scoreEntry in allScores)
        // {
        //     Debug.Log($"{scoreEntry.PlayerName}: {scoreEntry.Score}");
        // }
    }

    // Example function to clear all scores (e.g., from an admin panel or reset button)
    public void OnClickClearScores()
    {
        if (RankingSystem.Instance != null)
        {
            RankingSystem.Instance.ClearScores();
            Debug.Log("Scores cleared by UI button.");
        }
    }
}
*/
```