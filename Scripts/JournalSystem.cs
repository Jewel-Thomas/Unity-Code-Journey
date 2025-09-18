// Unity Design Pattern Example: JournalSystem
// This script demonstrates the JournalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **JournalSystem** design pattern (also known as Event Log or Audit Trail) is used to record a chronological sequence of events, actions, or state changes within an application. It provides an immutable, append-only log that can be queried and analyzed later.

In Unity, this pattern is incredibly useful for:
*   **Debugging:** Tracking the flow of events and state changes to diagnose issues.
*   **Analytics:** Recording player behavior, game progress, and interactions for data analysis.
*   **Quest/Progression Tracking:** Logging quest updates, achievement triggers, and story events.
*   **Save/Load Systems:** Storing a history of crucial events that might need to be replayed or understood.
*   **Player Feedback:** Allowing players to review their past actions or game events.

This example provides a complete, practical implementation of a JournalSystem for Unity, ready to be dropped into your project.

---

### **How to Use This Example in Unity:**

1.  **Create a C# Script:** In your Unity project, create a new C# script named `JournalSystem.cs`.
2.  **Copy and Paste:** Copy the entire code block below and paste it into your new `JournalSystem.cs` file, replacing its default content.
3.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., name it `_GameManagers` or `JournalManager`).
4.  **Attach Script:** Drag and drop the `JournalSystem.cs` script onto this new GameObject.
5.  **Run Your Game:** The `JournalSystem` will automatically initialize, load any previous journal entries, and be ready to receive new logs.
6.  **Log Events:** From any other script, call `JournalSystem.Instance.Log(...)` to add entries to the journal. The `GameEventsLogger` script below demonstrates how to do this.
7.  **Review in Console:** New entries will be logged to Unity's console.
8.  **Check Persistence:** Stop and restart your game. The journal should load the entries from the previous session (saved to `Application.persistentDataPath`).

---

### **`JournalSystem.cs`**

This script contains all the necessary classes and the `JournalSystem` MonoBehaviour to manage your game's event log.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO; // For file operations
using System.Linq; // For LINQ operations like .TakeLast()

// =================================================================================================
// 1. JournalEntry Classes: Define the structure of a single entry in the journal.
// =================================================================================================

/// <summary>
/// Represents a single key-value pair for contextual data within a journal entry.
/// This structure is used to allow JsonUtility to easily serialize dictionary-like data.
/// </summary>
[Serializable]
public class JournalContextParameter
{
    public string key;
    public string value; // Keeping value as string for simplicity with JsonUtility.
                         // Complex types would need to be serialized into this string (e.g., as JSON).

    public JournalContextParameter(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

/// <summary>
/// Represents a single, immutable event logged in the game's journal.
/// Each entry includes a timestamp, an event type, a descriptive message,
/// and optional contextual data.
/// </summary>
[Serializable]
public class JournalEntry
{
    // Timestamp when the event occurred. Crucial for chronological logging.
    public DateTime timestamp;

    // A string category for the event (e.g., "PlayerAction", "QuestUpdate", "SystemEvent").
    // Using a string offers flexibility; an enum could be used for strict types.
    public string eventType;

    // A descriptive message explaining what happened.
    public string message;

    // Optional list of key-value pairs providing additional context to the event.
    public List<JournalContextParameter> contextData;

    /// <summary>
    /// Constructor for a new journal entry.
    /// </summary>
    /// <param name="eventType">The category of the event.</param>
    /// <param name="message">A description of the event.</param>
    /// <param name="contextData">Optional dictionary of additional data.</param>
    public JournalEntry(string eventType, string message, Dictionary<string, string> context = null)
    {
        this.timestamp = DateTime.Now; // Automatically set the creation time
        this.eventType = eventType;
        this.message = message;
        this.contextData = new List<JournalContextParameter>();

        if (context != null)
        {
            foreach (var kvp in context)
            {
                this.contextData.Add(new JournalContextParameter(kvp.Key, kvp.Value));
            }
        }
    }

    /// <summary>
    /// Provides a formatted string representation of the journal entry for display or logging.
    /// </summary>
    public string ToDisplayString()
    {
        string contextString = "";
        if (contextData != null && contextData.Count > 0)
        {
            contextString = " (Context: ";
            foreach (var param in contextData)
            {
                contextString += $"{param.key}: {param.value}; ";
            }
            contextString = contextString.TrimEnd(';', ' ') + ")";
        }
        return $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {eventType}: {message}{contextString}";
    }
}

/// <summary>
/// A wrapper class required by Unity's JsonUtility to serialize and deserialize
/// a List of custom serializable objects. JsonUtility cannot directly serialize List<T>.
/// </summary>
[Serializable]
public class JournalDataWrapper
{
    public List<JournalEntry> entries;

    public JournalDataWrapper()
    {
        entries = new List<JournalEntry>();
    }
}


// =================================================================================================
// 2. JournalSystem MonoBehaviour: Manages the collection of journal entries and persistence.
// =================================================================================================

/// <summary>
/// The core JournalSystem manager. Implemented as a Singleton for easy global access.
/// It collects, stores, and allows querying of journal entries, and handles persistence.
/// </summary>
public class JournalSystem : MonoBehaviour
{
    // Singleton instance for easy global access (e.g., JournalSystem.Instance.Log(...)).
    public static JournalSystem Instance { get; private set; }

    [Header("Journal Settings")]
    [Tooltip("The name of the file to save/load journal data (e.g., 'game_journal.json').")]
    [SerializeField] private string journalFileName = "game_journal.json";
    
    [Tooltip("Maximum number of entries to keep in memory. Older entries will be discarded " +
             "when adding new ones or after loading, to prevent excessive memory usage.")]
    [SerializeField] private int maxEntriesInMemory = 1000;
    
    [Tooltip("If true, the journal will automatically save all entries to disk when the application quits.")]
    [SerializeField] private bool autoSaveOnQuit = true;
    
    [Tooltip("If true, a Debug.Log message will be printed for every journal entry added.")]
    [SerializeField] private bool logToConsole = true;

    // The internal list that holds all current journal entries.
    private List<JournalEntry> _journalEntries = new List<JournalEntry>();
    
    // Public read-only property to expose the journal entries without allowing external modification.
    public IReadOnlyList<JournalEntry> AllEntries => _journalEntries.AsReadOnly();

    // =============================================================================================
    // Unity Lifecycle Methods
    // =============================================================================================

    private void Awake()
    {
        // Enforce the Singleton pattern:
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Destroy(gameObject);
            return;
        }
        
        // This is the first or only instance, so assign it.
        Instance = this;
        // Keep this GameObject alive across scene loads to maintain the journal state.
        DontDestroyOnLoad(gameObject);

        // Attempt to load any previously saved journal entries at startup.
        LoadJournal();
        Debug.Log($"[JournalSystem] Initialized. Ready to log events. Max entries: {maxEntriesInMemory}.");
    }

    private void OnApplicationQuit()
    {
        // Automatically save the journal when the game closes if auto-save is enabled.
        if (autoSaveOnQuit)
        {
            SaveJournal();
        }
    }

    // =============================================================================================
    // Public API for Logging and Retrieving Entries
    // =============================================================================================

    /// <summary>
    /// Logs a new entry into the game journal. This is the primary method for adding events.
    /// The entry is timestamped automatically and added to the chronological log.
    /// </summary>
    /// <param name="eventType">A string categorizing the event (e.g., "PlayerAction", "QuestUpdate", "SystemEvent").</param>
    /// <param name="message">A descriptive message for the event (e.g., "Player picked up item", "Quest completed").</param>
    /// <param name="context">Optional: A dictionary of key-value pairs providing additional context to the event.</param>
    public void Log(string eventType, string message, Dictionary<string, string> context = null)
    {
        // Create a new JournalEntry object with the current time.
        JournalEntry newEntry = new JournalEntry(eventType, message, context);
        _journalEntries.Add(newEntry);

        // Enforce the maximum number of entries allowed in memory.
        // If the list exceeds the limit, remove the oldest entry.
        if (_journalEntries.Count > maxEntriesInMemory)
        {
            _journalEntries.RemoveAt(0); // Remove the oldest entry from the start of the list.
        }

        // Log to Unity Console for immediate feedback during development.
        if (logToConsole)
        {
            Debug.Log($"[Journal] {newEntry.ToDisplayString()}");
        }
    }

    /// <summary>
    /// Retrieves a read-only list of all journal entries currently in memory,
    /// ordered chronologically from oldest to newest.
    /// </summary>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of <see cref="JournalEntry"/>.</returns>
    public IReadOnlyList<JournalEntry> GetEntries()
    {
        return _journalEntries.AsReadOnly();
    }

    /// <summary>
    /// Retrieves a read-only list of journal entries that match a specific event type.
    /// The comparison is case-insensitive.
    /// </summary>
    /// <param name="eventType">The type of event to filter by.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of filtered <see cref="JournalEntry"/>.</returns>
    public IReadOnlyList<JournalEntry> GetEntriesByType(string eventType)
    {
        return _journalEntries.FindAll(e => e.eventType.Equals(eventType, StringComparison.OrdinalIgnoreCase)).AsReadOnly();
    }

    /// <summary>
    /// Retrieves a read-only list of journal entries that occurred within a specified time range.
    /// </summary>
    /// <param name="startTime">The start of the time range (inclusive).</param>
    /// <param name="endTime">The end of the time range (inclusive).</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of filtered <see cref="JournalEntry"/>.</returns>
    public IReadOnlyList<JournalEntry> GetEntriesInTimeRange(DateTime startTime, DateTime endTime)
    {
        return _journalEntries.FindAll(e => e.timestamp >= startTime && e.timestamp <= endTime).AsReadOnly();
    }

    /// <summary>
    /// Clears all entries from the journal currently in memory.
    /// Note: This does not affect the saved journal file until <see cref="SaveJournal"/> is called.
    /// </summary>
    public void ClearJournal()
    {
        _journalEntries.Clear();
        Debug.Log("[JournalSystem] All in-memory entries cleared.");
    }

    // =============================================================================================
    // Persistence Methods (Save/Load)
    // =============================================================================================

    /// <summary>
    /// Constructs the full file path for the journal save file.
    /// Uses Application.persistentDataPath, which is a platform-independent directory
    /// for persistent data (e.g., C:\Users\<user>\AppData\LocalLow\<company>\<product>\ on Windows).
    /// </summary>
    /// <returns>The full path to the journal file.</returns>
    private string GetJournalFilePath()
    {
        return Path.Combine(Application.persistentDataPath, journalFileName);
    }

    /// <summary>
    /// Saves the current journal entries to a JSON file.
    /// This method can be called manually at key points (e.g., level transitions, game exit).
    /// </summary>
    public void SaveJournal()
    {
        string filePath = GetJournalFilePath();
        try
        {
            // Wrap the list in a serializable class for JsonUtility.
            JournalDataWrapper wrapper = new JournalDataWrapper { entries = _journalEntries };
            
            // Serialize the wrapper to a JSON string. 'true' enables pretty-printing.
            string json = JsonUtility.ToJson(wrapper, true);
            
            // Write the JSON string to the specified file path.
            File.WriteAllText(filePath, json);
            Debug.Log($"[JournalSystem] Saved {journalFileName} to {filePath}. Total entries: {_journalEntries.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[JournalSystem] Failed to save journal to {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Loads journal entries from a JSON file. This is typically called at game startup.
    /// If the file doesn't exist, the journal remains empty.
    /// </summary>
    public void LoadJournal()
    {
        string filePath = GetJournalFilePath();
        if (File.Exists(filePath))
        {
            try
            {
                // Read the entire JSON string from the file.
                string json = File.ReadAllText(filePath);
                
                // Deserialize the JSON string back into the wrapper object.
                JournalDataWrapper wrapper = JsonUtility.FromJson<JournalDataWrapper>(json);
                
                // Assign the loaded entries, ensuring it's never null.
                _journalEntries = wrapper.entries ?? new List<JournalEntry>();
                Debug.Log($"[JournalSystem] Loaded {journalFileName} from {filePath}. Total entries: {_journalEntries.Count}");

                // Apply max entries limit after loading as well, in case the saved file
                // contains more entries than currently allowed in memory.
                if (_journalEntries.Count > maxEntriesInMemory)
                {
                    Debug.LogWarning($"[JournalSystem] Loaded journal has more entries ({_journalEntries.Count}) than max allowed ({maxEntriesInMemory}). Trimming oldest entries.");
                    // Remove the oldest entries until the limit is met.
                    _journalEntries.RemoveRange(0, _journalEntries.Count - maxEntriesInMemory);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JournalSystem] Failed to load journal from {filePath}: {e.Message}");
                // If loading fails, ensure the journal is in a valid (empty) state.
                _journalEntries = new List<JournalEntry>();
            }
        }
        else
        {
            Debug.Log($"[JournalSystem] No existing journal file found at {filePath}. Starting with an empty journal.");
            _journalEntries = new List<JournalEntry>();
        }
    }
}
```

---

### **`GameEventsLogger.cs` (Example Usage)**

This script demonstrates how other parts of your game would interact with the `JournalSystem`.
You can attach this script to any GameObject in your scene to see the example logs in action.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For DateTime

/// <summary>
/// This script demonstrates how to use the JournalSystem to log various game events
/// and retrieve entries for display or analysis.
/// Attach this script to any GameObject in your scene to see it in action.
/// </summary>
public class GameEventsLogger : MonoBehaviour
{
    private void Start()
    {
        // Ensure the JournalSystem is initialized before trying to use it.
        // If JournalSystem is a MonoBehaviour on a GameObject with DontDestroyOnLoad,
        // it should already be ready by the time other Start methods run.
        if (JournalSystem.Instance == null)
        {
            Debug.LogError("JournalSystem is not initialized. Make sure it's present in your scene.");
            return;
        }

        Debug.Log("--- Demonstrating JournalSystem Logging ---");

        // 1. Example: Logging a player action
        // This could be called from a player controller script.
        JournalSystem.Instance.Log(
            "PlayerAction", // Event Type
            "Player picked up an item.", // Message
            new Dictionary<string, string> { 
                { "ItemName", "Health Potion" }, 
                { "ItemID", "HP001" },
                { "Location", "Forest Glade" }
            } // Context Data
        );

        // 2. Example: Logging a quest update
        // This could be called from a quest manager script.
        JournalSystem.Instance.Log(
            "QuestUpdate",
            "Quest 'The Lost Relic' progress.",
            new Dictionary<string, string> { 
                { "QuestID", "QR002" }, 
                { "Progress", "2/5 artifacts found" },
                { "Target", "Artifact_GoblinCave" }
            }
        );

        // 3. Example: Logging a system event
        // This could be called from a game settings manager or AI system.
        JournalSystem.Instance.Log(
            "SystemEvent",
            "Game difficulty changed.",
            new Dictionary<string, string> { { "OldDifficulty", "Normal" }, { "NewDifficulty", "Hard" } }
        );

        // 4. Example: Logging an achievement unlock (could be tied to a separate achievement system)
        JournalSystem.Instance.Log(
            "Achievement",
            "Achievement unlocked: First Blood.",
            new Dictionary<string, string> { { "AchievementID", "AC001" }, { "Reward", "50 Gold" } }
        );

        // Simulate some time passing or more events happening later.
        // We'll use Invoke for demonstration purposes.
        Invoke(nameof(LogMoreEventsAndQuery), 2f);
    }

    private void LogMoreEventsAndQuery()
    {
        Debug.Log("\n--- Logging More Events After a Delay ---");

        // More player actions
        JournalSystem.Instance.Log("PlayerAction", "Player entered combat zone.", new Dictionary<string, string> { { "Zone", "Goblin Caves" } });
        JournalSystem.Instance.Log("PlayerAction", "Player defeated a monster.", new Dictionary<string, string> { { "MonsterType", "Goblin" }, { "ExperienceGained", "15" } });
        
        // Quest completion
        JournalSystem.Instance.Log("QuestUpdate", "Quest 'The Lost Relic' completed!", new Dictionary<string, string> { { "QuestID", "QR002" }, { "Reward", "Ancient Staff" } });

        Debug.Log("\n--- Displaying All Journal Entries ---");
        // Retrieve and print all entries.
        foreach (var entry in JournalSystem.Instance.GetEntries())
        {
            Debug.Log(entry.ToDisplayString());
        }

        Debug.Log("\n--- Displaying Only 'QuestUpdate' Entries ---");
        // Retrieve and print only entries of a specific type.
        foreach (var entry in JournalSystem.Instance.GetEntriesByType("QuestUpdate"))
        {
            Debug.Log(entry.ToDisplayString());
        }

        Debug.Log("\n--- Displaying Entries in the Last 5 Seconds (approx) ---");
        // Retrieve entries within a time range.
        DateTime fiveSecondsAgo = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
        foreach (var entry in JournalSystem.Instance.GetEntriesInTimeRange(fiveSecondsAgo, DateTime.Now))
        {
            Debug.Log(entry.ToDisplayString());
        }

        // Demonstrate manual save (the system also auto-saves on quit if configured)
        Debug.Log("\n--- Manually Saving Journal ---");
        JournalSystem.Instance.SaveJournal();

        // Optional: Clear journal (for next run or specific game states)
        // JournalSystem.Instance.ClearJournal();
    }
}
```