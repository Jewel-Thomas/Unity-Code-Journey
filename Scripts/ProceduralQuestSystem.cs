// Unity Design Pattern Example: ProceduralQuestSystem
// This script demonstrates the ProceduralQuestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example provides a complete and practical implementation of a **Procedural Quest System** design pattern. This pattern focuses on generating quests dynamically at runtime based on defined rules and world context, rather than relying on manually pre-authored quests.

The system is broken down into several key components:

1.  **Enums**: Define quest states and objective types.
2.  **`QuestObjective`**: A single task within a quest (e.g., "Kill 5 Goblins").
3.  **`Quest`**: The complete quest definition, comprising multiple objectives, state, and rewards.
4.  **`QuestGenerationContext` (ScriptableObject)**: A designer-friendly asset that stores the "ingredients" for quest generation (e.g., available enemy types, item types, reward ranges).
5.  **`QuestGenerator` (Static Class)**: The core logic that combines the context with generation rules to create new `Quest` instances.
6.  **`QuestManager` (MonoBehaviour)**: The central hub that manages active quests, processes game events to update quest progress, and handles quest completion/failure. It also uses events for decoupling.

---

**1. Create the C# Script:**

Create a new C# script in your Unity project, name it `QuestSystem.cs`, and paste the following code into it:

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for LINQ operations like .All() and .Where()

// =====================================================================================================================
// PROCEDURAL QUEST SYSTEM DESIGN PATTERN EXAMPLE
// =====================================================================================================================
// This example demonstrates how to build a system that dynamically generates quests
// at runtime, rather than relying solely on pre-defined, manually authored quests.
// It leverages ScriptableObjects for configuration and events for decoupling.
// =====================================================================================================================

// --- 1. Enums ---
/// <summary>
/// Defines the possible states of a quest throughout its lifecycle.
/// </summary>
public enum QuestState
{
    NotStarted, // Quest has been generated but not yet accepted by the player
    Active,     // Quest is currently being pursued by the player
    Completed,  // Quest objectives met and rewards claimed
    Failed      // Quest objectives not met, or time ran out, or player choice led to failure
}

/// <summary>
/// Defines the types of objectives a quest can have.
/// This enum can be extensively expanded to support various gameplay mechanics.
/// </summary>
public enum QuestObjectiveType
{
    KillEnemies,        // Defeat a specified number of certain enemy types
    CollectItems,       // Gather a specified number of certain item types
    VisitLocation,      // Reach a specific point or area in the game world
    InteractWithObject  // Activate or use a specific object in the environment
    // TODO: Add more types like EscortNPC, TalkToNPC, FindItem, DeliverItem, etc.
}

// --- 2. Quest Objective Data Structure ---
/// <summary>
/// Represents a single goal or task within a quest.
/// Marked as [System.Serializable] so it can be viewed and edited in the Unity Inspector
/// when embedded within a MonoBehaviour or ScriptableObject.
/// </summary>
[System.Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;  // What kind of task this is (e.g., KillEnemies)
    public string targetName;        // The specific entity/item/location (e.g., "Goblin", "Mushroom")
    public int requiredAmount;       // How many of the target are needed
    public int currentAmount;        // Current progress towards the required amount

    /// <summary>
    /// Checks if the objective has been completed.
    /// </summary>
    public bool IsCompleted => currentAmount >= requiredAmount;

    /// <summary>
    /// Provides a user-friendly description of the objective's current progress.
    /// </summary>
    public string GetProgressDescription()
    {
        switch (type)
        {
            case QuestObjectiveType.KillEnemies:
                // Adds 's' for plural if requiredAmount > 1
                return $"Kill {currentAmount}/{requiredAmount} {targetName}{(requiredAmount > 1 && targetName.ToLower() != "slime" ? "s" : "")}";
            case QuestObjectiveType.CollectItems:
                return $"Collect {currentAmount}/{requiredAmount} {targetName}{(requiredAmount > 1 ? "s" : "")}";
            case QuestObjectiveType.VisitLocation:
            case QuestObjectiveType.InteractWithObject:
                // These are usually 1-time objectives, so progress is binary
                return $"{ (IsCompleted ? "Visited" : "Visit")} the {targetName}"; // "Visited the Ancient Ruin" or "Visit the Ancient Ruin"
            default:
                return "Unknown Objective";
        }
    }

    /// <summary>
    /// Increments the current amount for the objective, capping at the required amount.
    /// </summary>
    /// <param name="amount">The quantity to add to the current progress.</param>
    /// <returns>True if progress was successfully made (i.e., not already completed), false otherwise.</returns>
    public bool AddProgress(int amount)
    {
        if (IsCompleted) return false; // Cannot add progress to an already completed objective

        int oldAmount = currentAmount;
        currentAmount = Mathf.Min(currentAmount + amount, requiredAmount); // Ensure currentAmount doesn't exceed requiredAmount
        return currentAmount > oldAmount; // Returns true if any progress was actually added
    }

    /// <summary>
    /// Resets the objective's progress to zero. Useful for failed quests or retries.
    /// </summary>
    public void ResetProgress()
    {
        currentAmount = 0;
    }
}

// --- 3. Quest Data Structure ---
/// <summary>
/// Represents a complete quest. It holds all the details including objectives,
/// current state, and rewards.
/// Marked as [System.Serializable] to allow it to be displayed in the Inspector
/// when part of another Unity component.
/// </summary>
[System.Serializable]
public class Quest
{
    public string id;                   // A unique identifier, typically a GUID for procedural quests
    public string title;                // The quest's display name
    [TextArea(3, 5)]                    // Makes the description field multi-line in the Inspector
    public string description;          // Detailed explanation of the quest
    public List<QuestObjective> objectives; // A list of tasks to complete this quest
    public QuestState state;            // Current state of the quest

    // Simple rewards for demonstration; could be a list of items, currency, etc.
    public int goldReward;
    public int xpReward;

    /// <summary>
    /// Checks if all objectives of the quest have been completed.
    /// </summary>
    public bool IsCompleted => objectives != null && objectives.All(o => o.IsCompleted);

    /// <summary>
    /// Checks if the quest is currently active.
    /// </summary>
    public bool IsActive => state == QuestState.Active;

    /// <summary>
    /// Constructor for creating a new Quest instance.
    /// </summary>
    public Quest(string id, string title, string description, List<QuestObjective> objectives, int goldReward, int xpReward)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        // Ensure objectives list is initialized, even if null is passed
        this.objectives = objectives ?? new List<QuestObjective>();
        this.goldReward = goldReward;
        this.xpReward = xpReward;
        this.state = QuestState.NotStarted; // New quests always start as NotStarted
    }

    /// <summary>
    /// Activates the quest, changing its state to Active. This typically happens when a player accepts the quest.
    /// </summary>
    public void Activate()
    {
        if (state == QuestState.NotStarted)
        {
            state = QuestState.Active;
            Debug.Log($"Quest '{title}' has been activated!");
        }
    }

    /// <summary>
    /// Attempts to complete the quest if it's active and all objectives are met.
    /// This method also applies the quest's rewards.
    /// </summary>
    /// <returns>True if the quest was successfully completed, false otherwise.</returns>
    public bool TryComplete()
    {
        if (state == QuestState.Active && IsCompleted)
        {
            state = QuestState.Completed;
            Debug.Log($"Quest '{title}' completed! Gained {goldReward} gold and {xpReward} XP.");
            // In a real game, you would interact with player inventory/stats here:
            // PlayerStats.Instance.AddGold(goldReward);
            // PlayerStats.Instance.AddXP(xpReward);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks the quest as failed.
    /// </summary>
    public void Fail()
    {
        if (state == QuestState.Active)
        {
            state = QuestState.Failed;
            Debug.Log($"Quest '{title}' failed!");
            // In a real game, you might apply penalties or specific game state changes here
        }
    }
}

// --- 4. Quest Generation Context (ScriptableObject) ---
/// <summary>
/// A ScriptableObject that serves as a configurable data source for the QuestGenerator.
/// Designers can create and populate instances of this asset in the Unity Editor
/// to define the various elements and parameters for procedural quest generation.
/// This decouples quest content from code, making it highly flexible.
/// </summary>
[CreateAssetMenu(fileName = "QuestGenerationContext", menuName = "Quest System/Quest Generation Context", order = 1)]
public class QuestGenerationContext : ScriptableObject
{
    [Header("Enemy Types for Kill Quests")]
    public List<string> availableEnemyTypes = new List<string> { "Goblin", "Orc", "Slime", "Bandit" };
    [Range(1, 15)] public int minKillAmount = 3;
    [Range(1, 15)] public int maxKillAmount = 8;

    [Header("Item Types for Collect Quests")]
    public List<string> availableItemTypes = new List<string> { "Mushroom", "Wolf Pelt", "Iron Ore", "Crystal Shard" };
    [Range(1, 10)] public int minCollectAmount = 2;
    [Range(1, 10)] public int maxCollectAmount = 6;

    [Header("Locations for Visit Quests")]
    public List<string> availableLocations = new List<string> { "Ancient Ruin", "Whispering Forest", "Dark Cave", "Forgotten Shrine" };

    [Header("Objects for Interact Quests")]
    public List<string> availableInteractiveObjects = new List<string> { "Mysterious Altar", "Broken Cart", "Crystal Pedestal" };

    [Header("Quest Reward Ranges")]
    public int minGoldReward = 50;
    public int maxGoldReward = 200;
    public int minXPReward = 100;
    public int maxXPReward = 500;
}

// --- 5. Quest Generator (Static Class) ---
/// <summary>
/// A static utility class responsible for creating new Quest instances procedurally.
/// It uses the provided QuestGenerationContext to make random, yet constrained, choices
/// about quest types, targets, amounts, and rewards.
/// </summary>
public static class QuestGenerator
{
    /// <summary>
    /// Generates a single random quest based on the configured context.
    /// </summary>
    /// <param name="context">The QuestGenerationContext containing the pool of quest elements.</param>
    /// <returns>A newly created Quest instance, or null if generation failed (e.g., empty context lists).</returns>
    public static Quest GenerateRandomQuest(QuestGenerationContext context)
    {
        if (context == null)
        {
            Debug.LogError("QuestGenerationContext is null. Cannot generate quest.");
            return null;
        }

        string questId = Guid.NewGuid().ToString(); // Generate a unique ID for the quest
        string title = "Generic Quest";
        string description = "An unknown task needs your attention.";
        List<QuestObjective> objectives = new List<QuestObjective>();
        int goldReward = UnityEngine.Random.Range(context.minGoldReward, context.maxGoldReward + 1);
        int xpReward = UnityEngine.Random.Range(context.minXPReward, context.maxXPReward + 1);

        // Randomly pick one of the defined objective types
        QuestObjectiveType randomObjectiveType = (QuestObjectiveType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(QuestObjectiveType)).Length);

        // Based on the chosen type, populate the objective details
        switch (randomObjectiveType)
        {
            case QuestObjectiveType.KillEnemies:
                if (context.availableEnemyTypes.Count > 0)
                {
                    string enemy = context.availableEnemyTypes[UnityEngine.Random.Range(0, context.availableEnemyTypes.Count)];
                    int amount = UnityEngine.Random.Range(context.minKillAmount, context.maxKillAmount + 1);
                    objectives.Add(new QuestObjective { type = QuestObjectiveType.KillEnemies, targetName = enemy, requiredAmount = amount, currentAmount = 0 });
                    title = $"Bounty: Eliminate {amount} {enemy}{(amount > 1 && enemy.ToLower() != "slime" ? "s" : "")}";
                    description = $"The villagers are terrorized by {enemy}{(amount > 1 && enemy.ToLower() != "slime" ? "s" : "")}. Bring peace to the lands.";
                }
                break;
            case QuestObjectiveType.CollectItems:
                if (context.availableItemTypes.Count > 0)
                {
                    string item = context.availableItemTypes[UnityEngine.Random.Range(0, context.availableItemTypes.Count)];
                    int amount = UnityEngine.Random.Range(context.minCollectAmount, context.maxCollectAmount + 1);
                    objectives.Add(new QuestObjective { type = QuestObjectiveType.CollectItems, targetName = item, requiredAmount = amount, currentAmount = 0 });
                    title = $"Gathering Mission: {item}{(amount > 1 ? "s" : "")}";
                    description = $"Collect {amount} {item}{(amount > 1 ? "s" : "")} for the local alchemist. They are essential for new potions.";
                }
                break;
            case QuestObjectiveType.VisitLocation:
                if (context.availableLocations.Count > 0)
                {
                    string location = context.availableLocations[UnityEngine.Random.Range(0, context.availableLocations.Count)];
                    objectives.Add(new QuestObjective { type = QuestObjectiveType.VisitLocation, targetName = location, requiredAmount = 1, currentAmount = 0 });
                    title = $"Expedition: Explore the {location}";
                    description = $"Journey to the mysterious {location} and discover its secrets. Report back upon arrival.";
                }
                break;
            case QuestObjectiveType.InteractWithObject:
                if (context.availableInteractiveObjects.Count > 0)
                {
                    string obj = context.availableInteractiveObjects[UnityEngine.Random.Range(0, context.availableInteractiveObjects.Count)];
                    objectives.Add(new QuestObjective { type = QuestObjectiveType.InteractWithObject, targetName = obj, requiredAmount = 1, currentAmount = 0 });
                    title = $"Investigation: The {obj}";
                    description = $"An ancient artifact, the {obj}, holds dormant power. Interact with it to activate it.";
                }
                break;
            default:
                Debug.LogWarning("QuestGenerator encountered an unhandled QuestObjectiveType.");
                break;
        }

        // If no objectives were added (e.g., due to empty lists in context), return null
        if (objectives.Count == 0)
        {
            Debug.LogError("Failed to generate any objectives. Check QuestGenerationContext configuration for empty lists.");
            return null;
        }

        return new Quest(questId, title, description, objectives, goldReward, xpReward);
    }
}


// --- 6. Quest Manager (MonoBehaviour) ---
/// <summary>
/// The central manager for all quests in the game. It acts as a singleton for easy access
/// and orchestrates the entire quest lifecycle: generation, activation, progress tracking,
/// completion, and failure.
/// This component is the primary interface for other game systems to interact with the
/// Procedural Quest System.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // Singleton pattern for global access
    public static QuestManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private QuestGenerationContext questGenerationContext; // The ScriptableObject defining quest elements
    [SerializeField] private int maxActiveQuests = 3;                       // Limit on how many quests a player can have simultaneously

    [Header("Quest Data (Runtime)")]
    [Tooltip("List of quests currently accepted and in progress.")]
    public List<Quest> activeQuests = new List<Quest>();
    [Tooltip("List of quests that have been successfully completed.")]
    public List<Quest> completedQuests = new List<Quest>();
    [Tooltip("List of quests that have been failed.")]
    public List<Quest> failedQuests = new List<Quest>();

    // --- Events for Decoupling ---
    // Other game systems (UI, PlayerController, GameState) can subscribe to these events
    // to react to quest changes without direct dependencies on the QuestManager's internal logic.
    public event Action<Quest> OnQuestGenerated;          // Fired when a new quest is generated and accepted
    public event Action<Quest, QuestObjective> OnQuestProgressUpdated; // Fired when an objective's progress changes
    public event Action<Quest> OnQuestCompleted;          // Fired when a quest transitions to 'Completed'
    public event Action<Quest> OnQuestFailed;             // Fired when a quest transitions to 'Failed'


    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement the singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple QuestManagers found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make the QuestManager persist across scene loads
            DontDestroyOnLoad(gameObject);
        }

        // Ensure lists are initialized (though [SerializeField] usually handles this)
        activeQuests ??= new List<Quest>();
        completedQuests ??= new List<Quest>();
        failedQuests ??= new List<Quest>();
    }

    private void Start()
    {
        // Example: Generate some initial quests when the game starts
        Debug.Log("QuestManager Initializing: Generating first quests...");
        GenerateAndAcceptNewQuest();
        GenerateAndAcceptNewQuest();
        GenerateAndAcceptNewQuest();
    }

    // --- Event Subscriptions/Unsubscriptions ---
    private void OnEnable()
    {
        OnQuestGenerated += HandleQuestGenerated;
        OnQuestProgressUpdated += HandleQuestProgressUpdated;
        OnQuestCompleted += HandleQuestCompleted;
        OnQuestFailed += HandleQuestFailed; // Added for completeness
    }

    private void OnDisable()
    {
        OnQuestGenerated -= HandleQuestGenerated;
        OnQuestProgressUpdated -= HandleQuestProgressUpdated;
        OnQuestCompleted -= HandleQuestCompleted;
        OnQuestFailed -= HandleQuestFailed;
    }

    // --- Core Quest Management Methods ---

    /// <summary>
    /// Generates a new quest using the QuestGenerator and automatically accepts it (activates it).
    /// </summary>
    /// <returns>The newly generated and accepted Quest, or null if generation failed or max active quests reached.</returns>
    public Quest GenerateAndAcceptNewQuest()
    {
        if (activeQuests.Count >= maxActiveQuests)
        {
            Debug.LogWarning($"Cannot accept new quests. Maximum active quests ({maxActiveQuests}) reached.");
            return null;
        }
        if (questGenerationContext == null)
        {
            Debug.LogError("QuestGenerationContext is not assigned. Cannot generate quests.");
            return null;
        }

        Quest newQuest = QuestGenerator.GenerateRandomQuest(questGenerationContext);
        if (newQuest != null)
        {
            activeQuests.Add(newQuest); // Add to our list of active quests
            newQuest.Activate();        // Set its state to Active
            OnQuestGenerated?.Invoke(newQuest); // Notify subscribers
            Debug.Log($"[QuestManager] Successfully generated and accepted new quest: '{newQuest.title}'");
            return newQuest;
        }
        else
        {
            Debug.LogError("[QuestManager] Failed to generate a new quest.");
            return null;
        }
    }

    /// <summary>
    /// Reports progress for a specific type of objective and target.
    /// This method is designed to be called by other game systems (e.g., an enemy script on death,
    /// an item script on pickup, a player controller on entering a location).
    /// It iterates through all active quests to find any matching objectives and updates them.
    /// This allows a single game action to potentially contribute to multiple quests.
    /// </summary>
    /// <param name="type">The type of objective being progressed (e.g., KillEnemies).</param>
    /// <param name="targetName">The specific target involved (e.g., "Goblin", "Mushroom").</param>
    /// <param name="amount">The quantity of progress to add (default is 1).</param>
    public void ReportProgress(QuestObjectiveType type, string targetName, int amount = 1)
    {
        bool anyProgressMade = false;
        // Iterate through active quests to find objectives that match the reported event
        foreach (Quest quest in activeQuests)
        {
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type == type && objective.targetName == targetName && !objective.IsCompleted)
                {
                    if (objective.AddProgress(amount))
                    {
                        Debug.Log($"[QuestManager] Quest '{quest.title}' progress: {objective.GetProgressDescription()}");
                        OnQuestProgressUpdated?.Invoke(quest, objective); // Notify subscribers
                        anyProgressMade = true;
                    }
                }
            }
        }

        // If any progress was made, check if any quests have now become complete
        if (anyProgressMade)
        {
            CheckQuestsForCompletion();
        }
    }

    /// <summary>
    /// A convenience method for one-time objectives (like VisitLocation or InteractWithObject),
    /// where `amount` isn't strictly necessary or is always 1.
    /// </summary>
    public void ReportCompletion(QuestObjectiveType type, string targetName)
    {
        ReportProgress(type, targetName, 1000000); // Pass a very large number to guarantee completion if matched
    }

    /// <summary>
    /// Iterates through all currently active quests and checks if any have met all their objectives.
    /// If so, it completes the quest and moves it to the 'completedQuests' list.
    /// </summary>
    public void CheckQuestsForCompletion()
    {
        // Use LINQ to find all quests that are active and have all objectives completed
        List<Quest> questsToComplete = activeQuests.Where(q => q.IsCompleted).ToList();

        foreach (Quest quest in questsToComplete)
        {
            if (quest.TryComplete()) // Attempt to complete the quest (applies rewards)
            {
                activeQuests.Remove(quest);    // Remove from active list
                completedQuests.Add(quest);    // Add to completed list
                OnQuestCompleted?.Invoke(quest); // Notify subscribers
            }
        }
    }

    /// <summary>
    /// Manually fails a specific quest by its ID.
    /// </summary>
    /// <param name="questId">The unique ID of the quest to fail.</param>
    /// <returns>True if the quest was found and failed, false otherwise.</returns>
    public bool FailQuest(string questId)
    {
        Quest quest = activeQuests.FirstOrDefault(q => q.id == questId);
        if (quest != null)
        {
            quest.Fail(); // Mark the quest as failed
            activeQuests.Remove(quest); // Remove from active list
            failedQuests.Add(quest);    // Add to failed list
            OnQuestFailed?.Invoke(quest); // Notify subscribers
            Debug.Log($"[QuestManager] Quest '{quest.title}' (ID: {questId}) has been failed.");
            return true;
        }
        Debug.LogWarning($"[QuestManager] Quest with ID '{questId}' not found among active quests to fail.");
        return false;
    }

    /// <summary>
    /// Retrieves an active quest by its unique ID.
    /// </summary>
    public Quest GetActiveQuest(string questId)
    {
        return activeQuests.FirstOrDefault(q => q.id == questId);
    }

    /// <summary>
    /// Retrieves all currently active quests.
    /// </summary>
    public List<Quest> GetAllActiveQuests()
    {
        return new List<Quest>(activeQuests); // Return a copy to prevent external modification
    }

    // --- Event Handler Methods (for demonstration/logging) ---
    private void HandleQuestGenerated(Quest quest)
    {
        Debug.Log($"[Event Handler] Quest '{quest.title}' was generated. Objectives:");
        foreach (var obj in quest.objectives)
        {
            Debug.Log($"  - {obj.GetProgressDescription()}");
        }
    }

    private void HandleQuestProgressUpdated(Quest quest, QuestObjective objective)
    {
        Debug.Log($"[Event Handler] Quest '{quest.title}' objective '{objective.targetName}' updated: {objective.GetProgressDescription()}");
    }

    private void HandleQuestCompleted(Quest quest)
    {
        Debug.Log($"[Event Handler] Quest '{quest.title}' has been COMPLETED! Rewards: {quest.goldReward} Gold, {quest.xpReward} XP.");
    }

    private void HandleQuestFailed(Quest quest)
    {
        Debug.Log($"[Event Handler] Quest '{quest.title}' has been FAILED.");
    }

    // --- Example Usage / Simulation in Inspector (Context Menu) ---
    // These methods provide easy ways to test the system directly from the Unity Editor's Inspector.
    // In a real game, 'ReportProgress' would be called by actual game logic (e.g., enemy death scripts).

    [ContextMenu("Simulate: Kill a Goblin")]
    public void SimulateKillGoblin()
    {
        Debug.Log("\n--- SIMULATING: Player killed a Goblin ---");
        ReportProgress(QuestObjectiveType.KillEnemies, "Goblin");
    }

    [ContextMenu("Simulate: Collect a Mushroom")]
    public void SimulateCollectMushroom()
    {
        Debug.Log("\n--- SIMULATING: Player collected a Mushroom ---");
        ReportProgress(QuestObjectiveType.CollectItems, "Mushroom");
    }

    [ContextMenu("Simulate: Visit Ancient Ruin")]
    public void SimulateVisitAncientRuin()
    {
        Debug.Log("\n--- SIMULATING: Player visited Ancient Ruin ---");
        ReportCompletion(QuestObjectiveType.VisitLocation, "Ancient Ruin");
    }

    [ContextMenu("Simulate: Interact with Mysterious Altar")]
    public void SimulateInteractWithAltar()
    {
        Debug.Log("\n--- SIMULATING: Player interacted with Mysterious Altar ---");
        ReportCompletion(QuestObjectiveType.InteractWithObject, "Mysterious Altar");
    }

    [ContextMenu("Simulate: Generate Another Quest")]
    public void SimulateGenerateAnotherQuest()
    {
        Debug.Log("\n--- SIMULATING: Generating and accepting another random quest ---");
        GenerateAndAcceptNewQuest();
    }

    [ContextMenu("Simulate: Fail First Active Quest")]
    public void SimulateFailFirstActiveQuest()
    {
        if (activeQuests.Count > 0)
        {
            Debug.Log("\n--- SIMULATING: Failing the first active quest ---");
            FailQuest(activeQuests[0].id);
        }
        else
        {
            Debug.LogWarning("No active quests to fail.");
        }
    }
}
```

---

**2. How to Set Up and Use in Unity:**

1.  **Create Script:** Save the code as `QuestSystem.cs` in your `Assets` folder.
2.  **Create QuestGenerationContext:**
    *   In the Unity Editor, go to `Assets -> Create -> Quest System -> Quest Generation Context`.
    *   Name the new asset `MyGameQuestContext` (or any descriptive name).
    *   Select `MyGameQuestContext` in the Project window. In the Inspector, you'll see fields to customize:
        *   `Available Enemy Types`: Add names like "Goblin", "Orc", "Dragon".
        *   `Min/Max Kill Amount`: Set the range for how many enemies to kill.
        *   `Available Item Types`: Add names like "Mushroom", "Wolf Pelt", "Gem".
        *   `Min/Max Collect Amount`: Set the range for how many items to collect.
        *   `Available Locations`: Add names like "Ancient Ruin", "Dark Forest", "Sky Temple".
        *   `Available Interactive Objects`: Add names like "Mysterious Altar", "Lost Relic", "Ancient Door".
        *   `Min/Max Gold/XP Reward`: Define the range for rewards.
    *   This is your **designer-configurable data** for procedural quest generation.
3.  **Create QuestManager GameObject:**
    *   In your scene, create an empty GameObject (e.g., right-click in Hierarchy -> `Create Empty`).
    *   Rename it to `_QuestManager`.
    *   Drag the `QuestSystem.cs` script onto this `_QuestManager` GameObject in the Inspector. This will attach the `QuestManager` component.
4.  **Assign Context to Manager:**
    *   Select the `_QuestManager` GameObject.
    *   In the Inspector, locate the `Quest Manager` component.
    *   Drag your `MyGameQuestContext` asset (from step 2) into the `Quest Generation Context` slot.
    *   Set `Max Active Quests` to your desired limit (e.g., 3).
5.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe the Console window. You should see messages indicating quests being generated and activated in `Start()`.
    *   Select the `_QuestManager` GameObject in the Hierarchy while in Play Mode.
    *   In the Inspector, right-click on the `Quest Manager` component's header (or click the `...` menu on the component) to access the `[ContextMenu]` methods.
    *   Click on "Simulate: Kill a Goblin", "Simulate: Collect a Mushroom", etc. to see quests progress and complete in real-time. The `activeQuests` list will update, and completed quests will move to `completedQuests`.

---

**3. Integrating with Your Game (Example Usage in Other Scripts):**

To make this system truly practical, your game's mechanics need to report events to the `QuestManager`.

**Example: Enemy Script**

```csharp
// Attach this script to your enemy prefabs/GameObjects
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public string enemyTypeName = "Goblin"; // Set this in the Inspector for each enemy type

    public void TakeDamage(int damage)
    {
        // ... health reduction logic ...
        if ( /* enemy health <= 0 */ true ) // Simplified for example
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{enemyTypeName} died!");
        if (QuestManager.Instance != null)
        {
            // Report to the QuestManager that an enemy of this type was killed
            QuestManager.Instance.ReportProgress(QuestObjectiveType.KillEnemies, enemyTypeName);
        }
        Destroy(gameObject); // Remove enemy from scene
    }
}
```

**Example: Collectible Item Script**

```csharp
// Attach this script to your collectible item prefabs/GameObjects
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public string itemTypeName = "Mushroom"; // Set this in the Inspector for each item type

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Assuming your player has the tag "Player"
        {
            Debug.Log($"Player collected a {itemTypeName}!");
            if (QuestManager.Instance != null)
            {
                // Report to the QuestManager that an item of this type was collected
                QuestManager.Instance.ReportProgress(QuestObjectiveType.CollectItems, itemTypeName);
            }
            Destroy(gameObject); // Remove item from scene
        }
    }
}
```

**Example: Location Trigger Script**

```csharp
// Attach this script to a trigger collider around a specific location
using UnityEngine;

public class LocationDiscoveryTrigger : MonoBehaviour
{
    public string locationName = "Ancient Ruin"; // Set this in the Inspector
    private bool visited = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !visited)
        {
            Debug.Log($"Player entered {locationName}!");
            if (QuestManager.Instance != null)
            {
                // Report to the QuestManager that the player visited this location
                QuestManager.Instance.ReportCompletion(QuestObjectiveType.VisitLocation, locationName);
                visited = true; // Mark as visited to prevent re-reporting
            }
        }
    }
}
```

**Example: Basic UI Display (Optional, requires Unity UI components)**

```csharp
// Attach to a UI Canvas GameObject with Text components for quest display
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Linq;

public class QuestUIDisplay : MonoBehaviour
{
    public Text currentQuestTitleText;
    public Text currentQuestDescriptionText;
    public Transform objectivesPanel; // Parent GameObject for objective UI elements
    public GameObject objectiveUIPrefab; // A prefab with a Text component for individual objectives

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestGenerated += UpdateUI;
            QuestManager.Instance.OnQuestProgressUpdated += (quest, obj) => UpdateUI(quest); // Update UI for specific quest
            QuestManager.Instance.OnQuestCompleted += UpdateUI;
            QuestManager.Instance.OnQuestFailed += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestGenerated -= UpdateUI;
            QuestManager.Instance.OnQuestProgressUpdated -= (quest, obj) => UpdateUI(quest);
            QuestManager.Instance.OnQuestCompleted -= UpdateUI;
            QuestManager.Instance.OnQuestFailed -= UpdateUI;
        }
    }

    void Start()
    {
        UpdateUI(null); // Initial UI update
    }

    // Displays the first active quest (can be extended to show all)
    private void UpdateUI(Quest updatedQuest = null)
    {
        // Clear previous objective UI elements
        foreach (Transform child in objectivesPanel)
        {
            Destroy(child.gameObject);
        }

        if (QuestManager.Instance.activeQuests.Any())
        {
            Quest displayedQuest = updatedQuest ?? QuestManager.Instance.activeQuests[0]; // Prioritize updated quest, else first active

            currentQuestTitleText.text = displayedQuest.title;
            currentQuestDescriptionText.text = displayedQuest.description;

            foreach (var objective in displayedQuest.objectives)
            {
                GameObject objUI = Instantiate(objectiveUIPrefab, objectivesPanel);
                Text objText = objUI.GetComponent<Text>();
                if (objText != null)
                {
                    objText.text = objective.GetProgressDescription();
                    objText.color = objective.IsCompleted ? Color.green : Color.white; // Green for completed, white for active
                }
            }
        }
        else
        {
            currentQuestTitleText.text = "No Active Quests";
            currentQuestDescriptionText.text = "Generate a new quest or complete existing ones!";
        }
    }
}
```

---

This example provides a robust, educational, and practical foundation for implementing a procedural quest system in your Unity projects. It highlights key design patterns like **Singleton**, **Observer (Events)**, and **Strategy (QuestGenerationContext + QuestGenerator)**, making it easily extensible and maintainable.