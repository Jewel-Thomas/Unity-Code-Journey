// Unity Design Pattern Example: JournalQuestIntegration
// This script demonstrates the JournalQuestIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **JournalQuestIntegration** design pattern in Unity. This pattern focuses on how a core game system (like a Quest System) interacts with a display/information system (like a Player Journal) to provide a cohesive and up-to-date experience for the player.

**Core Idea of JournalQuestIntegration:**

1.  **Quest Management System:** Handles the logic, state, and progression of all quests. It's the source of truth for quest data.
2.  **Journal Display System:** Primarily responsible for presenting quest information (active, completed, lore, etc.) to the player. It should be a viewer of the quest data, not the manager of it.
3.  **Integration (Observer Pattern):** The Quest Management System uses events or callbacks to notify the Journal Display System whenever a quest's state changes. The Journal Display System subscribes to these events and updates its internal representation and, consequently, the UI. This loose coupling ensures that both systems can evolve independently while maintaining a strong, reactive connection.

---

To use this code:

1.  Create a new C# script named `JournalQuestIntegration` in your Unity project.
2.  Copy and paste the entire code below into the script.
3.  Create an `Editor` folder in your project, and inside it, create a new C# script named `QuestSOEditor`. Copy the `QuestSOEditor` code provided into it. This will make the Quest Scriptable Object creation easier.
4.  Create some `QuestSO` Scriptable Objects:
    *   Right-click in your Project window -> Create -> Quest System -> Quest.
    *   Fill in Quest ID, Name, Description, and add Objectives.
5.  Create an empty GameObject in your scene named `Managers`.
6.  Attach the `QuestManager` component to `Managers`.
7.  Attach the `JournalManager` component to `Managers`.
8.  Assign your created `QuestSO` assets to the `Quest Manager`'s `Available Quests` list in the Inspector.
9.  Drag the `UI_Journal_Panel` prefab from the `_Prefabs` folder into the scene if you want a visual journal display (see `JournalUIController` at the bottom for an example).
10. Create empty GameObjects for `Quest Giver` and `Objective Trigger`. Attach the `QuestGiver` and `ObjectiveCompleter` scripts respectively. Assign a `Quest ID` and `Objective ID` in their Inspectors.
11. Run the scene and observe the Debug.Log output from the Journal and Quest Managers.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// --- Enums ---
/// <summary>
/// Represents the current state of a quest.
/// </summary>
public enum QuestState
{
    NotStarted,
    Active,
    Completed,
    Failed
}

/// <summary>
/// Represents the type of an entry in the player's journal.
/// </summary>
public enum JournalEntryType
{
    Quest,
    Lore,
    Achievement
}

// --- Structs & Classes for Data Definitions ---

/// <summary>
/// Defines a single objective for a quest. This is part of the static QuestSO.
/// </summary>
[System.Serializable]
public class QuestObjective
{
    public string objectiveID; // Unique ID for this objective within the quest
    public string description;
    public int requiredProgress;
    public bool canBeCompletedMultipleTimes; // e.g., collect 5 items, each item increments progress

    public QuestObjective(string id, string desc, int reqProgress, bool multiple)
    {
        objectiveID = id;
        description = desc;
        requiredProgress = reqProgress;
        canBeCompletedMultipleTimes = multiple;
    }
}

/// <summary>
/// Represents the player's current progress on a specific objective.
/// This is part of the player's dynamic quest status.
/// </summary>
[System.Serializable]
public class PlayerObjectiveStatus
{
    public string objectiveID; // Matches the ID in QuestObjective
    public int currentProgress;
    public bool isCompleted;

    public PlayerObjectiveStatus(string id)
    {
        objectiveID = id;
        currentProgress = 0;
        isCompleted = false;
    }

    public PlayerObjectiveStatus(QuestObjective objective)
    {
        objectiveID = objective.objectiveID;
        currentProgress = 0;
        isCompleted = false;
    }

    /// <summary>
    /// Updates the progress for this objective.
    /// Returns true if the objective was just completed.
    /// </summary>
    public bool UpdateProgress(int amount)
    {
        if (isCompleted) return false;

        currentProgress += amount;
        if (currentProgress >= QuestManager.Instance.GetQuestObjective(objectiveID)?.requiredProgress)
        {
            isCompleted = true;
            return true;
        }
        return false;
    }
}

/// <summary>
/// Represents the player's dynamic status for a single quest.
/// This tracks progress, state, and objective completion.
/// </summary>
[System.Serializable]
public class PlayerQuestStatus
{
    public string questID;
    public QuestState currentState;
    public List<PlayerObjectiveStatus> objectiveStatuses;

    public PlayerQuestStatus(string id, QuestSO questSO)
    {
        questID = id;
        currentState = QuestState.NotStarted;
        objectiveStatuses = new List<PlayerObjectiveStatus>();
        foreach (var obj in questSO.objectives)
        {
            objectiveStatuses.Add(new PlayerObjectiveStatus(obj));
        }
    }

    /// <summary>
    /// Checks if all objectives for this quest are completed.
    /// </summary>
    public bool AreAllObjectivesCompleted()
    {
        return objectiveStatuses.All(o => o.isCompleted);
    }
}

/// <summary>
/// Represents a single entry in the player's journal.
/// This can be a quest, a lore piece, an achievement, etc.
/// </summary>
[System.Serializable]
public class JournalEntry
{
    public JournalEntryType entryType;
    public string id; // Unique ID for this entry (e.g., questID, loreID)
    public string title;
    public string description;
    public string currentStatusText; // For quests: "Active", "Completed", "Failed"

    // Quest-specific data, only relevant if entryType is Quest
    public QuestState questState;
    public List<PlayerObjectiveStatus> questObjectives;

    public JournalEntry(JournalEntryType type, string entryId, string entryTitle, string entryDescription, string status = "", QuestState state = QuestState.NotStarted, List<PlayerObjectiveStatus> objectives = null)
    {
        entryType = type;
        id = entryId;
        title = entryTitle;
        description = entryDescription;
        currentStatusText = status;
        questState = state;
        questObjectives = objectives ?? new List<PlayerObjectiveStatus>();
    }
}


// --- Scriptable Objects (Quest Definitions) ---

/// <summary>
/// A ScriptableObject defining a quest. This is static data, not player-specific progress.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest", order = 1)]
public class QuestSO : ScriptableObject
{
    [Header("Quest Details")]
    public string questID; // Unique identifier for the quest
    public string questName;
    [TextArea(3, 10)]
    public string description;

    [Header("Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    // You could add rewards, prerequisites, follow-up quests here.
}


// --- Quest Management System ---

/// <summary>
/// Manages all quests in the game, their definitions, and the player's progress.
/// This is the core 'source of truth' for quest data.
/// It uses the Singleton pattern for easy global access.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Definitions")]
    [SerializeField] private List<QuestSO> availableQuests = new List<QuestSO>();

    // Player's current quest progress and status
    private Dictionary<string, PlayerQuestStatus> playerQuests = new Dictionary<string, PlayerQuestStatus>();
    private Dictionary<string, QuestSO> questDefinitions = new Dictionary<string, QuestSO>();

    // --- Events for Journal Integration ---
    // These events are the core of the 'Integration' part of the pattern.
    // The JournalManager (or any other system) subscribes to these.

    /// <summary>
    /// Event fired when a quest's status changes (started, updated, completed, failed).
    /// Provides the QuestID and the updated PlayerQuestStatus.
    /// </summary>
    public event Action<string, PlayerQuestStatus> OnQuestUpdated;

    /// <summary>
    /// Event fired specifically when a quest is first started.
    /// </summary>
    public event Action<string, PlayerQuestStatus> OnQuestStarted;

    /// <summary>
    /// Event fired specifically when a quest is completed.
    /// </summary>
    public event Action<string, PlayerQuestStatus> OnQuestCompleted;

    /// <summary>
    /// Event fired specifically when a quest is failed.
    /// </summary>
    public event Action<string, PlayerQuestStatus> OnQuestFailed;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeQuests();
    }

    /// <summary>
    /// Loads all available QuestSO definitions into a dictionary for quick lookup.
    /// </summary>
    private void InitializeQuests()
    {
        foreach (var questSO in availableQuests)
        {
            if (questDefinitions.ContainsKey(questSO.questID))
            {
                Debug.LogWarning($"Duplicate QuestID found: {questSO.questID}. Skipping {questSO.questName}.");
                continue;
            }
            questDefinitions.Add(questSO.questID, questSO);
        }
        Debug.Log($"QuestManager initialized with {questDefinitions.Count} quests.");
    }

    /// <summary>
    /// Starts a quest for the player.
    /// </summary>
    public bool StartQuest(string questID)
    {
        if (!questDefinitions.TryGetValue(questID, out QuestSO questSO))
        {
            Debug.LogError($"Attempted to start non-existent quest: {questID}");
            return false;
        }

        if (playerQuests.ContainsKey(questID) && playerQuests[questID].currentState != QuestState.NotStarted)
        {
            Debug.LogWarning($"Quest '{questID}' is already {playerQuests[questID].currentState}. Cannot start again.");
            return false;
        }

        PlayerQuestStatus newQuestStatus = new PlayerQuestStatus(questID, questSO)
        {
            currentState = QuestState.Active
        };
        playerQuests[questID] = newQuestStatus;

        Debug.Log($"Quest '{questSO.questName}' STARTED.");

        // Notify subscribers (e.g., JournalManager) that a quest has started/updated
        OnQuestStarted?.Invoke(questID, newQuestStatus);
        OnQuestUpdated?.Invoke(questID, newQuestStatus); // Also notify general update
        return true;
    }

    /// <summary>
    /// Marks an objective as completed or progresses it by a certain amount.
    /// </summary>
    public bool CompleteObjective(string questID, string objectiveID, int progressAmount = 1)
    {
        if (!playerQuests.TryGetValue(questID, out PlayerQuestStatus questStatus))
        {
            Debug.LogWarning($"Attempted to update objective for non-active quest: {questID}");
            return false;
        }

        if (questStatus.currentState != QuestState.Active)
        {
            Debug.LogWarning($"Quest '{questID}' is not active ({questStatus.currentState}). Cannot complete objective.");
            return false;
        }

        PlayerObjectiveStatus objStatus = questStatus.objectiveStatuses.FirstOrDefault(o => o.objectiveID == objectiveID);
        if (objStatus == null)
        {
            Debug.LogWarning($"Objective '{objectiveID}' not found in quest '{questID}'.");
            return false;
        }

        if (objStatus.isCompleted && !GetQuestObjective(objectiveID, questID).canBeCompletedMultipleTimes)
        {
            Debug.Log($"Objective '{objectiveID}' in quest '{questID}' is already completed.");
            return false;
        }

        bool wasJustCompleted = objStatus.UpdateProgress(progressAmount);

        string objName = questDefinitions[questID].objectives.FirstOrDefault(o => o.objectiveID == objectiveID)?.description ?? objectiveID;
        Debug.Log($"Objective '{objName}' for quest '{questStatus.questID}' progress: {objStatus.currentProgress}/{GetQuestObjective(objectiveID, questID)?.requiredProgress}. {(wasJustCompleted ? "Completed!" : "")}");

        // Check if all objectives are completed after this update
        if (questStatus.AreAllObjectivesCompleted())
        {
            CompleteQuest(questID); // Automatically complete the quest
            return true;
        }

        // Notify subscribers (e.g., JournalManager) that a quest's status has been updated
        OnQuestUpdated?.Invoke(questID, questStatus);
        return true;
    }

    /// <summary>
    /// Manually completes a quest. Typically called when all objectives are met, or via a specific event.
    /// </summary>
    public bool CompleteQuest(string questID)
    {
        if (!playerQuests.TryGetValue(questID, out PlayerQuestStatus questStatus))
        {
            Debug.LogWarning($"Attempted to complete non-active quest: {questID}");
            return false;
        }

        if (questStatus.currentState != QuestState.Active)
        {
            Debug.LogWarning($"Quest '{questID}' is not active ({questStatus.currentState}). Cannot complete.");
            return false;
        }

        questStatus.currentState = QuestState.Completed;
        Debug.Log($"Quest '{questDefinitions[questID].questName}' COMPLETED!");

        // Notify subscribers
        OnQuestCompleted?.Invoke(questID, questStatus);
        OnQuestUpdated?.Invoke(questID, questStatus); // Also notify general update
        return true;
    }

    /// <summary>
    /// Fails a quest.
    /// </summary>
    public bool FailQuest(string questID)
    {
        if (!playerQuests.TryGetValue(questID, out PlayerQuestStatus questStatus))
        {
            Debug.LogWarning($"Attempted to fail non-active quest: {questID}");
            return false;
        }

        if (questStatus.currentState != QuestState.Active)
        {
            Debug.LogWarning($"Quest '{questID}' is not active ({questStatus.currentState}). Cannot fail.");
            return false;
        }

        questStatus.currentState = QuestState.Failed;
        Debug.Log($"Quest '{questDefinitions[questID].questName}' FAILED!");

        // Notify subscribers
        OnQuestFailed?.Invoke(questID, questStatus);
        OnQuestUpdated?.Invoke(questID, questStatus); // Also notify general update
        return true;
    }

    /// <summary>
    /// Retrieves the current status of a player's quest.
    /// </summary>
    public PlayerQuestStatus GetQuestStatus(string questID)
    {
        playerQuests.TryGetValue(questID, out PlayerQuestStatus status);
        return status;
    }

    /// <summary>
    /// Retrieves the static definition of a quest.
    /// </summary>
    public QuestSO GetQuestDefinition(string questID)
    {
        questDefinitions.TryGetValue(questID, out QuestSO definition);
        return definition;
    }

    /// <summary>
    /// Retrieves a specific objective definition from a quest.
    /// </summary>
    public QuestObjective GetQuestObjective(string objectiveID, string questID = null)
    {
        if (questID != null && questDefinitions.TryGetValue(questID, out QuestSO questSO))
        {
            return questSO.objectives.FirstOrDefault(o => o.objectiveID == objectiveID);
        }
        // Fallback for when questID isn't provided (e.g., if objectiveID is globally unique or context implies quest)
        // This might require iterating through all quest definitions, which can be slow.
        // It's better to provide questID when possible.
        foreach (var quest in questDefinitions.Values)
        {
            var obj = quest.objectives.FirstOrDefault(o => o.objectiveID == objectiveID);
            if (obj != null) return obj;
        }
        return null;
    }

    /// <summary>
    /// Gets all quests that are currently active for the player.
    /// </summary>
    public List<PlayerQuestStatus> GetActiveQuests()
    {
        return playerQuests.Values.Where(q => q.currentState == QuestState.Active).ToList();
    }

    /// <summary>
    /// Gets all quests that are completed or failed for the player.
    /// </summary>
    public List<PlayerQuestStatus> GetInactiveQuests()
    {
        return playerQuests.Values.Where(q => q.currentState == QuestState.Completed || q.currentState == QuestState.Failed).ToList();
    }
}


// --- Journal Management System ---

/// <summary>
/// Manages the player's journal, which displays information from various game systems,
/// primarily quests, but can also include lore, achievements, etc.
/// It observes the QuestManager for updates to display relevant quest information.
/// Uses the Singleton pattern.
/// </summary>
public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    [Header("Journal Settings")]
    [SerializeField] private bool includeCompletedQuests = true;
    [SerializeField] private bool includeFailedQuests = true;

    private Dictionary<string, JournalEntry> journalEntries = new Dictionary<string, JournalEntry>();

    // Event for UI to subscribe to for journal updates
    public event Action OnJournalUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // --- Core of JournalQuestIntegration: Subscribing to QuestManager events ---
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            QuestManager.Instance.OnQuestFailed += HandleQuestFailed;
            Debug.Log("JournalManager subscribed to QuestManager events.");
        }
        else
        {
            Debug.LogError("QuestManager not found! JournalManager cannot subscribe to quest events.");
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            QuestManager.Instance.OnQuestFailed -= HandleQuestFailed;
            Debug.Log("JournalManager unsubscribed from QuestManager events.");
        }
    }

    /// <summary>
    /// Handles the OnQuestStarted event from QuestManager.
    /// Creates or updates a journal entry for the newly started quest.
    /// </summary>
    private void HandleQuestStarted(string questID, PlayerQuestStatus questStatus)
    {
        QuestSO questDef = QuestManager.Instance.GetQuestDefinition(questID);
        if (questDef == null) return;

        Debug.Log($"JournalManager: Quest '{questDef.questName}' started. Adding/Updating journal entry.");

        JournalEntry newEntry = new JournalEntry(
            JournalEntryType.Quest,
            questID,
            questDef.questName,
            questDef.description,
            questStatus.currentState.ToString(),
            questStatus.currentState,
            questStatus.objectiveStatuses
        );

        journalEntries[questID] = newEntry; // Add or replace the entry
        OnJournalUpdated?.Invoke(); // Notify UI to refresh
    }

    /// <summary>
    /// Handles the OnQuestUpdated event from QuestManager.
    /// Updates an existing journal entry with the latest quest status.
    /// </summary>
    private void HandleQuestUpdated(string questID, PlayerQuestStatus questStatus)
    {
        if (journalEntries.TryGetValue(questID, out JournalEntry entry))
        {
            entry.questState = questStatus.currentState;
            entry.currentStatusText = questStatus.currentState.ToString();
            entry.questObjectives = questStatus.objectiveStatuses; // Update objective progress

            Debug.Log($"JournalManager: Quest '{entry.title}' updated. Current state: {entry.questState}. Objectives updated.");
            OnJournalUpdated?.Invoke(); // Notify UI to refresh
        }
        else
        {
            // If the entry doesn't exist but an update came, it might be a quest started without OnQuestStarted firing
            // (e.g., if JournalManager was initialized after quest started). Call HandleQuestStarted to create it.
            HandleQuestStarted(questID, questStatus);
        }
    }

    /// <summary>
    /// Handles the OnQuestCompleted event from QuestManager.
    /// Updates the journal entry and potentially moves it to a 'completed' section.
    /// </summary>
    private void HandleQuestCompleted(string questID, PlayerQuestStatus questStatus)
    {
        HandleQuestUpdated(questID, questStatus); // Update the entry
        Debug.Log($"JournalManager: Quest '{questID}' completed. Updated journal entry.");
        if (!includeCompletedQuests && journalEntries.ContainsKey(questID))
        {
            journalEntries.Remove(questID); // Remove from active view if not showing completed
        }
        OnJournalUpdated?.Invoke();
    }

    /// <summary>
    /// Handles the OnQuestFailed event from QuestManager.
    /// Updates the journal entry and potentially moves it to a 'failed' section.
    /// </summary>
    private void HandleQuestFailed(string questID, PlayerQuestStatus questStatus)
    {
        HandleQuestUpdated(questID, questStatus); // Update the entry
        Debug.Log($"JournalManager: Quest '{questID}' failed. Updated journal entry.");
        if (!includeFailedQuests && journalEntries.ContainsKey(questID))
        {
            journalEntries.Remove(questID); // Remove from active view if not showing failed
        }
        OnJournalUpdated?.Invoke();
    }

    /// <summary>
    /// Adds a non-quest-related entry to the journal (e.g., a lore piece).
    /// </summary>
    public void AddLoreEntry(string loreID, string title, string description)
    {
        if (journalEntries.ContainsKey(loreID))
        {
            Debug.LogWarning($"Lore entry with ID '{loreID}' already exists. Updating existing entry.");
            journalEntries[loreID].title = title;
            journalEntries[loreID].description = description;
            journalEntries[loreID].currentStatusText = "Unlocked";
        }
        else
        {
            JournalEntry newLoreEntry = new JournalEntry(JournalEntryType.Lore, loreID, title, description, "Unlocked");
            journalEntries.Add(loreID, newLoreEntry);
            Debug.Log($"JournalManager: Added new lore entry: {title}");
        }
        OnJournalUpdated?.Invoke();
    }

    /// <summary>
    /// Retrieves all entries currently held by the journal manager.
    /// </summary>
    public List<JournalEntry> GetAllJournalEntries()
    {
        return journalEntries.Values.ToList();
    }

    /// <summary>
    /// Retrieves specific quest entries based on their state for UI display.
    /// </summary>
    public List<JournalEntry> GetQuestEntriesByState(QuestState state)
    {
        return journalEntries.Values
            .Where(e => e.entryType == JournalEntryType.Quest && e.questState == state)
            .ToList();
    }

    /// <summary>
    /// Retrieves active quest entries.
    /// </summary>
    public List<JournalEntry> GetActiveQuestEntries()
    {
        return GetQuestEntriesByState(QuestState.Active);
    }

    /// <summary>
    /// Retrieves completed quest entries.
    /// </summary>
    public List<JournalEntry> GetCompletedQuestEntries()
    {
        return GetQuestEntriesByState(QuestState.Completed);
    }

    /// <summary>
    /// Retrieves failed quest entries.
    /// </summary>
    public List<JournalEntry> GetFailedQuestEntries()
    {
        return GetQuestEntriesByState(QuestState.Failed);
    }
}

// --- Example Usage / Integrators ---

/// <summary>
/// An example component that starts a quest when triggered (e.g., player interacts with an NPC).
/// </summary>
public class QuestGiver : MonoBehaviour
{
    [SerializeField] private string questToStartID = "DefaultQuest";
    [SerializeField] private KeyCode activateKey = KeyCode.E;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(activateKey))
        {
            Debug.Log($"Player interacted with {gameObject.name}. Attempting to start quest: {questToStartID}");
            QuestManager.Instance?.StartQuest(questToStartID);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}

/// <summary>
/// An example component that completes a specific objective when triggered (e.g., player picks up an item).
/// </summary>
public class ObjectiveCompleter : MonoBehaviour
{
    [SerializeField] private string questID = "DefaultQuest";
    [SerializeField] private string objectiveID = "DefaultObjective";
    [SerializeField] private int progressAmount = 1;
    [SerializeField] private bool destroyOnComplete = true; // For single-use triggers

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered {gameObject.name}. Attempting to complete objective: {objectiveID} for quest: {questID}");
            if (QuestManager.Instance?.CompleteObjective(questID, objectiveID, progressAmount) == true)
            {
                if (destroyOnComplete)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}


// --- Optional: Basic UI Display for Journal (Requires Unity UI namespace) ---
// To make this fully functional, create a Canvas, then a Panel with a ScrollView.
// Inside the ScrollView's Content, add a Text (for title) and another Text (for description).
// Make a prefab of the "JournalEntryUI" to be instantiated.

// This part needs `using UnityEngine.UI;` and `TMPro` if you use TextMeshPro.
// I'll provide a placeholder example using Debug.Log to keep it within the single file.

/*
// Uncomment this section if you want a basic UI implementation.
// Make sure to add 'using UnityEngine.UI;' at the top of the file.
// For TextMeshPro, add 'using TMPro;' and change `Text` to `TMP_Text`.

using UnityEngine.UI;

public class JournalUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject journalPanel; // The main UI panel for the journal
    public Transform contentParent; // Parent transform for dynamic quest entry UI elements
    public GameObject journalEntryUIPrefab; // Prefab for displaying a single journal entry

    private List<GameObject> activeEntryUIs = new List<GameObject>();

    void Start()
    {
        if (journalPanel != null) journalPanel.SetActive(false); // Start with journal closed
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.OnJournalUpdated += RefreshJournalUI;
            Debug.Log("JournalUIController subscribed to JournalManager updates.");
        }
    }

    void OnDestroy()
    {
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.OnJournalUpdated -= RefreshJournalUI;
            Debug.Log("JournalUIController unsubscribed from JournalManager updates.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) // Toggle journal with 'J' key
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        if (journalPanel != null)
        {
            bool isActive = !journalPanel.activeSelf;
            journalPanel.SetActive(isActive);
            if (isActive)
            {
                RefreshJournalUI();
                Debug.Log("Journal opened.");
            }
            else
            {
                Debug.Log("Journal closed.");
            }
        }
    }

    public void RefreshJournalUI()
    {
        // Clear old entries
        foreach (var uiObj in activeEntryUIs)
        {
            Destroy(uiObj);
        }
        activeEntryUIs.Clear();

        if (JournalManager.Instance == null || journalEntryUIPrefab == null || contentParent == null)
        {
            Debug.LogError("JournalUIController not fully set up.");
            return;
        }

        List<JournalEntry> entries = JournalManager.Instance.GetAllJournalEntries();

        // Sort entries (e.g., active quests first, then completed, then lore)
        entries = entries
            .OrderByDescending(e => e.entryType == JournalEntryType.Quest && e.questState == QuestState.Active)
            .ThenByDescending(e => e.entryType == JournalEntryType.Quest && e.questState == QuestState.Completed)
            .ThenBy(e => e.title)
            .ToList();

        foreach (var entry in entries)
        {
            GameObject entryUI = Instantiate(journalEntryUIPrefab, contentParent);
            activeEntryUIs.Add(entryUI);

            // Find UI components within the prefab (e.g., Text, TMP_Text)
            Text titleText = entryUI.transform.Find("TitleText")?.GetComponent<Text>();
            Text descriptionText = entryUI.transform.Find("DescriptionText")?.GetComponent<Text>();
            Text statusText = entryUI.transform.Find("StatusText")?.GetComponent<Text>();
            // Add more specific UI elements for objectives if needed

            if (titleText != null) titleText.text = entry.title;
            if (descriptionText != null) descriptionText.text = entry.description;
            if (statusText != null)
            {
                statusText.text = $"Status: {entry.currentStatusText}";
                // Color based on status
                if (entry.entryType == JournalEntryType.Quest)
                {
                    switch (entry.questState)
                    {
                        case QuestState.Active: statusText.color = Color.yellow; break;
                        case QuestState.Completed: statusText.color = Color.green; break;
                        case QuestState.Failed: statusText.color = Color.red; break;
                        default: statusText.color = Color.white; break;
                    }
                }
                else
                {
                    statusText.color = Color.cyan;
                }
            }

            // For quest entries, display objectives
            if (entry.entryType == JournalEntryType.Quest && entry.questObjectives.Any())
            {
                string objectiveDetails = "";
                foreach (var obj in entry.questObjectives)
                {
                    QuestObjective staticObj = QuestManager.Instance.GetQuestObjective(obj.objectiveID, entry.id);
                    if (staticObj != null)
                    {
                        objectiveDetails += $"- {staticObj.description} ({obj.currentProgress}/{staticObj.requiredProgress}) {(obj.isCompleted ? "[X]" : "[ ]")}\n";
                    }
                }
                if (descriptionText != null) descriptionText.text += "\n\nObjectives:\n" + objectiveDetails;
            }
        }
        Debug.Log($"Journal UI refreshed. Displaying {activeEntryUIs.Count} entries.");
    }
}
*/
```

---

**Editor Script for QuestSO (Place in an `Editor` folder):**

This script makes it easier to create `QuestSO` assets in the Unity Editor.

```csharp
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(QuestSO))]
public class QuestSOEditor : Editor
{
    private QuestSO questSO;
    private string newObjectiveDescription = "";
    private string newObjectiveID = "";
    private int newObjectiveRequiredProgress = 1;
    private bool newObjectiveCanBeCompletedMultipleTimes = false;

    private void OnEnable()
    {
        questSO = (QuestSO)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw default fields first

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add New Objective", EditorStyles.boldLabel);

        newObjectiveID = EditorGUILayout.TextField("Objective ID", newObjectiveID);
        newObjectiveDescription = EditorGUILayout.TextField("Description", newObjectiveDescription);
        newObjectiveRequiredProgress = EditorGUILayout.IntField("Required Progress", newObjectiveRequiredProgress);
        newObjectiveCanBeCompletedMultipleTimes = EditorGUILayout.Toggle("Can Be Completed Multiple Times", newObjectiveCanBeCompletedMultipleTimes);

        if (GUILayout.Button("Add Objective"))
        {
            if (string.IsNullOrWhiteSpace(newObjectiveID) || string.IsNullOrWhiteSpace(newObjectiveDescription))
            {
                Debug.LogWarning("Objective ID and Description cannot be empty.");
            }
            else if (questSO.objectives.Any(obj => obj.objectiveID == newObjectiveID))
            {
                Debug.LogWarning($"Objective with ID '{newObjectiveID}' already exists in this quest.");
            }
            else
            {
                questSO.objectives.Add(new QuestObjective(newObjectiveID, newObjectiveDescription, newObjectiveRequiredProgress, newObjectiveCanBeCompletedMultipleTimes));
                EditorUtility.SetDirty(questSO); // Mark the asset as dirty to save changes
                newObjectiveID = ""; // Clear for next input
                newObjectiveDescription = "";
                newObjectiveRequiredProgress = 1;
                newObjectiveCanBeCompletedMultipleTimes = false;
            }
        }

        EditorGUILayout.Space();

        if (questSO.objectives.Any())
        {
            EditorGUILayout.LabelField("Current Objectives", EditorStyles.boldLabel);
            for (int i = 0; i < questSO.objectives.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"ID: {questSO.objectives[i].objectiveID}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Description: {questSO.objectives[i].description}");
                EditorGUILayout.LabelField($"Required Progress: {questSO.objectives[i].requiredProgress}");
                EditorGUILayout.LabelField($"Multiple Times: {questSO.objectives[i].canBeCompletedMultipleTimes}");
                if (GUILayout.Button("Remove Objective"))
                {
                    questSO.objectives.RemoveAt(i);
                    EditorUtility.SetDirty(questSO);
                    break; // Exit loop after removing to avoid index issues
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }
    }
}
```