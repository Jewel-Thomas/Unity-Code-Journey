// Unity Design Pattern Example: QuestBranchingSystem
// This script demonstrates the QuestBranchingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Quest Branching System** design pattern. This pattern allows quests to have multiple paths and outcomes based on player choices and in-game conditions, leading to dynamic and non-linear storytelling.

The system is built on:
1.  **`QuestDefinition` (ScriptableObject):** The blueprint for a quest, configured by designers in the Unity Editor. It defines objectives, prerequisites, choices, and consequences.
2.  **`QuestObjective` (Abstract Class & Derived Types):** Represents individual tasks within a quest. Different objective types (e.g., kill enemies, fetch items, talk to NPCs) extend this base.
3.  **`QuestChoice` (Serializable Class):** Defines player decisions that influence branching, setting global flags or unlocking/failing other quests.
4.  **`QuestRuntimeState` (Class):** Manages the specific progress of a quest for a player, cloning the `QuestDefinition`'s objectives to track their completion status.
5.  **`QuestManager` (Singleton MonoBehaviour):** The central hub that manages all quest definitions, tracks player progress (`QuestRuntimeState` instances), handles global flags, evaluates quest eligibility, processes choices, and triggers follow-up quests.

---

### Key Concepts of the Quest Branching System:

*   **ScriptableObject-driven Content:** `QuestDefinition` allows game designers to create and link quests without touching code, promoting flexible content creation.
*   **Decoupled Runtime State:** `QuestRuntimeState` separates a quest's static definition from a player's dynamic progress, enabling multiple playthroughs or save files to manage quests independently.
*   **Global Flags:** A core mechanism for branching. Player choices or critical in-game events set "global flags" (simple string identifiers).
*   **Prerequisites:** Quests can only become available if certain `PrerequisiteQuestIDs` are completed and `PrerequisiteFlags` are set. This allows you to define complex dependencies and chains.
*   **Explicit Choices (`QuestChoice`):** Some quests can present players with explicit dialogue or action choices. Making a choice directly influences global flags and can unlock or fail other quests immediately.
*   **Dynamic Follow-Up:** Based on which flags are set, different follow-up quests can become available, or a single quest might adapt its content.

---

### Project Setup and How to Use in Unity:

1.  **Create C# Script:** Save the entire code block below as `QuestManager.cs` in your Unity project (e.g., in a `Scripts/Questing` folder).
2.  **Create QuestManager GameObject:** In a Unity scene, create an empty GameObject and name it `QuestManager`.
3.  **Attach Script:** Drag the `QuestManager.cs` script onto the `QuestManager` GameObject.
4.  **Create Quest Definitions:**
    *   In your Project window, right-click -> `Create` -> `Questing` -> `Quest Definition`.
    *   Create several of these (e.g., `Quest_Pendant`, `Quest_AnyaBranch`, `Quest_MerchantBranch`, `Quest_TownFate_AnyaPath`, `Quest_TownFate_MerchantPath`).
5.  **Configure Quest Definitions (Unity Inspector):**
    *   **Quest_Pendant:**
        *   `Quest ID`: `QUEST_PENDANT`
        *   `Quest Name`: "The Missing Pendant"
        *   `Description`: "Find Anya and locate her lost pendant in the Old Mine."
        *   `Objectives`:
            *   Add Element (Type: `SimpleCompletionObjective`)
                *   `Objective ID`: `OBJ_TALK_ANYA`
                *   `Description`: "Talk to Anya."
            *   Add Element (Type: `FetchItemObjective`)
                *   `Objective ID`: `OBJ_FIND_PENDANT`
                *   `Description`: "Find the ancient pendant."
                *   `Item ID To Fetch`: `ANCIENT_PENDANT`
                *   `Required Quantity`: `1`
        *   `Branching Choices`:
            *   **Choice 0:**
                *   `Choice Text`: "Return the pendant to Anya."
                *   `Consequence Flags To Set`: `PendantReturnedToAnya`
                *   `Quests To Unlock`: `QUEST_ANYAS_GRATITUDE`
            *   **Choice 1:**
                *   `Choice Text`: "Sell the pendant to the shady merchant."
                *   `Consequence Flags To Set`: `PendantSoldToMerchant`
                *   `Quests To Unlock`: `QUEST_MERCHANTS_SECRET`
        *   `Rewards`: `Experience Points: 100`
        *   `Flags To Set On Completion`: `PendantQuestCompleted`

    *   **Quest_AnyaBranch:**
        *   `Quest ID`: `QUEST_ANYAS_GRATITUDE`
        *   `Quest Name`: "Anya's Gratitude"
        *   `Description`: "Deliver a message for Anya as thanks for returning her pendant."
        *   `Prerequisite Flags`: `PendantReturnedToAnya`
        *   `Objectives`:
            *   Add Element (Type: `SimpleCompletionObjective`)
                *   `Objective ID`: `OBJ_DELIVER_ANYA_MESSAGE`
                *   `Description`: "Deliver Anya's message to the Elder."
        *   `Default Follow Up Quest IDs`: `QUEST_TOWN_FATE_ANYA_PATH`
        *   `Flags To Set On Completion`: `AnyaBranchCompleted`
        *   `Rewards`: `Experience Points: 150`

    *   **Quest_MerchantBranch:**
        *   `Quest ID`: `QUEST_MERCHANTS_SECRET`
        *   `Quest Name`: "Merchant's Secret"
        *   `Description`: "Help the shady merchant with his dubious task after selling him the pendant."
        *   `Prerequisite Flags`: `PendantSoldToMerchant`
        *   `Objectives`:
            *   Add Element (Type: `SimpleCompletionObjective`)
                *   `Objective ID`: `OBJ_DO_DUBIOUS_TASK`
                *   `Description`: "Perform the merchant's task."
        *   `Default Follow Up Quest IDs`: `QUEST_TOWN_FATE_MERCHANT_PATH`
        *   `Flags To Set On Completion`: `MerchantBranchCompleted`
        *   `Rewards`: `Experience Points: 150`, `Item IDs: ShadyCoin`

    *   **Quest_TownFate_AnyaPath:**
        *   `Quest ID`: `QUEST_TOWN_FATE_ANYA_PATH`
        *   `Quest Name`: "The Town's Fate (Anya's Path)"
        *   `Description`: "The town flourishes due to your good deeds, influenced by Anya."
        *   `Prerequisite Flags`: `AnyaBranchCompleted`
        *   `Objectives`: (e.g., "Witness the town's prosperity.")
        *   `Rewards`: `Experience Points: 200`, `Item IDs: TownBlessing`

    *   **Quest_TownFate_MerchantPath:**
        *   `Quest ID`: `QUEST_TOWN_FATE_MERCHANT_PATH`
        *   `Quest Name`: "The Town's Fate (Merchant's Path)"
        *   `Description`: "The town struggles under the merchant's growing influence."
        *   `Prerequisite Flags`: `MerchantBranchCompleted`
        *   `Objectives`: (e.g., "Witness the town's plight.")
        *   `Rewards`: `Experience Points: 200`, `Item IDs: BlackMarketLicense`

6.  **Assign Quest Definitions:** Select the `QuestManager` GameObject in the Hierarchy. In its Inspector, drag all your created `QuestDefinition` ScriptableObjects into the `All Quest Definitions` list.
7.  **Run and Test:** Play the scene. Use the "Debug" section in the `QuestManager` Inspector (while in Play mode) to simulate quest progression, objective completion, and choices. Watch the console for output confirming the branching logic.

    *   **Scenario 1 (Anya's Path):**
        1.  `Debug Quest ID To Start`: `QUEST_PENDANT` -> `Start Debug Quest`
        2.  `Debug Quest ID To Complete Objective`: `QUEST_PENDANT`, `Debug Objective ID To Complete`: `OBJ_TALK_ANYA` -> `Complete Debug Objective`
        3.  `Debug Item ID To Simulate Pickup`: `ANCIENT_PENDANT`, `Debug Item Quantity To Simulate Pickup`: `1` -> `Simulate Item Pickup` (This completes QUEST_PENDANT)
        4.  `Debug Quest ID To Make Choice`: `QUEST_PENDANT`, `Debug Choice Index`: `0` -> `Make Debug Choice` (This sets `PendantReturnedToAnya` and starts `QUEST_ANYAS_GRATITUDE`)
        5.  `Debug Quest ID To Complete Objective`: `QUEST_ANYAS_GRATITUDE`, `Debug Objective ID To Complete`: `OBJ_DELIVER_ANYA_MESSAGE` -> `Complete Debug Objective` (This sets `AnyaBranchCompleted` and starts `QUEST_TOWN_FATE_ANYA_PATH`)
        6.  Use `Print All Flags` and `Print All Quests Status` to see the results.

    *   **Scenario 2 (Merchant's Path):**
        1.  Stop and restart Unity Play mode to clear flags and quest states.
        2.  Repeat steps 1-3 from Scenario 1.
        3.  `Debug Quest ID To Make Choice`: `QUEST_PENDANT`, `Debug Choice Index`: `1` -> `Make Debug Choice` (This sets `PendantSoldToMerchant` and starts `QUEST_MERCHANTS_SECRET`)
        4.  `Debug Quest ID To Complete Objective`: `QUEST_MERCHANTS_SECRET`, `Debug Objective ID To Complete`: `OBJ_DO_DUBIOUS_TASK` -> `Complete Debug Objective` (This sets `MerchantBranchCompleted` and starts `QUEST_TOWN_FATE_MERCHANT_PATH`)
        5.  Use `Print All Flags` and `Print All Quests Status` to see the results.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// --- 1. QuestObjective Base Class and Derived Types ---
// This abstract class defines the fundamental structure of a quest objective.
// It uses the Strategy pattern, where different objective types (e.g., Kill, Fetch, Talk)
// are concrete implementations of this abstract base.
[Serializable]
public abstract class QuestObjective
{
    // Unique identifier for the objective within a quest. Used for referencing.
    public string ObjectiveID;
    public string Description;
    // Internal state of the objective. Protected set allows derived classes to change it.
    public bool IsCompleted { get; protected set; } = false;

    // Event to notify when this objective completes.
    [NonSerialized] // Don't serialize events, they get hooked up at runtime.
    public Action<QuestObjective> OnObjectiveCompleted;

    // Initializes the objective, typically resetting its state for a new quest instance.
    public virtual void Initialize()
    {
        IsCompleted = false;
        // In a real game, you might subscribe to game events here (e.g., enemy killed, item picked up).
        // For this example, objectives are completed manually or through simple conditions.
    }

    // Abstract method to evaluate if the objective's conditions are met.
    // This would be called by the QuestManager or related systems periodically or on relevant events.
    public abstract void EvaluateProgress();

    // Marks the objective as completed and triggers the OnObjectiveCompleted event.
    protected void MarkCompleted()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;
            Debug.Log($"Objective '{Description}' completed!");
            OnObjectiveCompleted?.Invoke(this);
        }
    }
}

// Concrete objective type: Simple "do something" objective, completed manually by game logic.
[Serializable]
public class SimpleCompletionObjective : QuestObjective
{
    // Simple objectives are often marked complete directly by external game logic
    // (e.g., after a cutscene, an NPC interaction, reaching a specific location).
    public override void EvaluateProgress()
    {
        // This objective typically has no internal progress logic;
        // it waits for an external system to call CompleteManually().
        // If already completed, do nothing.
    }

    // Public method to allow external systems (like QuestManager) to manually complete this objective.
    public void CompleteManually()
    {
        MarkCompleted();
    }
}

// Another concrete objective type: Fetch Item objective.
// In a real game, this would listen to an inventory manager or item pickup event.
[Serializable]
public class FetchItemObjective : QuestObjective
{
    public string ItemIDToFetch;
    public int RequiredQuantity;
    private int _currentQuantity; // Tracks how many items the player has collected for this objective

    public override void Initialize()
    {
        base.Initialize();
        _currentQuantity = 0;
        // In a real game, you'd subscribe to an InventoryManager.OnItemAdded event here.
        // For this demo, we'll simulate picking up an item through QuestManager.SimulateAddItem.
    }

    public override void EvaluateProgress()
    {
        if (IsCompleted) return;

        // In a real game, you'd check the player's actual inventory for ItemIDToFetch.
        // For now, _currentQuantity is updated via AddItem method.
        if (_currentQuantity >= RequiredQuantity)
        {
            MarkCompleted();
        }
    }

    // Simulates picking up an item relevant to this objective.
    public void AddItem(string itemID, int quantity)
    {
        if (IsCompleted) return;
        if (itemID == ItemIDToFetch)
        {
            _currentQuantity += quantity;
            Debug.Log($"Picked up {quantity}x {itemID}. Current: {_currentQuantity}/{RequiredQuantity} for objective '{Description}'.");
            EvaluateProgress(); // Check if completed after adding item
        }
    }
}

// --- 2. QuestChoice Class ---
// Represents a branching decision the player can make within a quest.
[Serializable]
public class QuestChoice
{
    public string ChoiceText;
    [Tooltip("Global flags to set when this choice is made. These flags can influence future quest availability.")]
    public List<string> ConsequenceFlagsToSet = new List<string>();
    [Tooltip("IDs of quests that become available/unlock after this choice (if their prerequisites are met).")]
    public List<string> QuestsToUnlock = new List<string>();
    [Tooltip("IDs of quests that become failed after this choice (e.g., mutually exclusive quests).")]
    public List<string> QuestsToFail = new List<string>();
}

// --- 3. QuestRewards (Simple Struct) ---
[Serializable]
public struct QuestRewards
{
    public int ExperiencePoints;
    public List<string> ItemIDs; // For simplicity, just a list of item IDs
    // Add more rewards as needed (gold, reputation, etc.)
}

// --- 4. QuestDefinition (ScriptableObject) ---
// This is the blueprint for a quest. Designers create these in the Unity Editor.
// It holds all the static, unchangeable data about a quest.
[CreateAssetMenu(fileName = "NewQuest", menuName = "Questing/Quest Definition", order = 1)]
public class QuestDefinition : ScriptableObject
{
    public string QuestID; // Unique identifier for the quest (e.g., "QUEST_MISSING_PENDANT")
    public string QuestName;
    [TextArea]
    public string Description;

    // [SerializeReference] allows Unity to serialize polymorphic fields.
    // This means we can have a List<QuestObjective> and populate it with
    // instances of SimpleCompletionObjective, FetchItemObjective, etc., in the Inspector.
    [SerializeReference]
    [Tooltip("The list of objectives required to complete this quest.")]
    public List<QuestObjective> Objectives = new List<QuestObjective>();

    [Header("Quest Branching & Prerequisites")]
    [Tooltip("Other quests that must be in a 'Completed' state for this quest to be eligible.")]
    public List<string> PrerequisiteQuestIDs = new List<string>();
    [Tooltip("Global flags that must be set for this quest to be eligible (AND logic).")]
    public List<string> PrerequisiteFlags = new List<string>();

    [Tooltip("Choices presented to the player upon completing this quest or at a decision point, leading to different branches.")]
    public List<QuestChoice> BranchingChoices = new List<QuestChoice>();
    [Tooltip("Quests that become available after this quest completes, if no specific branching choice was made/applied.")]
    public List<string> DefaultFollowUpQuestIDs = new List<string>();

    [Header("Consequences")]
    [Tooltip("Global flags to set when this quest successfully completes.")]
    public List<string> FlagsToSetOnCompletion = new List<string>();
    [Tooltip("Global flags to set when this quest fails.")]
    public List<string> FlagsToSetOnFailure = new List<string>();

    public QuestRewards Rewards;

    // Ensures QuestID is set (defaults to filename) and ObjectiveIDs are unique.
    void OnValidate()
    {
        if (string.IsNullOrEmpty(QuestID))
        {
            QuestID = name; // Default QuestID to filename if not set
        }
        foreach (var obj in Objectives)
        {
            if (string.IsNullOrEmpty(obj.ObjectiveID))
            {
                obj.ObjectiveID = Guid.NewGuid().ToString(); // Assign a unique ID if not set
            }
        }
    }
}

// --- 5. QuestRuntimeState Class ---
// This class holds the player-specific, runtime state of a quest.
// It references a QuestDefinition for its static data, but tracks dynamic progress unique to a player.
public enum QuestStatus { NotStarted, Active, Completed, Failed }

public class QuestRuntimeState
{
    public QuestDefinition Definition { get; private set; }
    public QuestStatus CurrentStatus { get; private set; }
    public List<QuestObjective> ObjectiveInstances { get; private set; } // Actual instances for player's progress

    // Events for UI updates or other game systems to react to quest state changes.
    public event Action<QuestRuntimeState> OnStatusChanged;
    public event Action<QuestRuntimeState, QuestObjective> OnObjectiveCompleted;

    public QuestRuntimeState(QuestDefinition definition)
    {
        Definition = definition;
        CurrentStatus = QuestStatus.NotStarted;
        ObjectiveInstances = new List<QuestObjective>();

        // Create deep copies of objectives so their state is unique to this runtime quest instance.
        // [SerializeReference] helps with this by allowing proper cloning of derived types.
        // For more complex nested objects within objectives, a dedicated cloning utility
        // or a serialize/deserialize approach might be more robust.
        foreach (var objDef in definition.Objectives)
        {
            QuestObjective clonedObjective;
            // Type-check and instantiate the correct derived objective type.
            if (objDef is SimpleCompletionObjective simpleObj)
            {
                clonedObjective = new SimpleCompletionObjective
                {
                    ObjectiveID = simpleObj.ObjectiveID,
                    Description = simpleObj.Description
                };
            }
            else if (objDef is FetchItemObjective fetchObj)
            {
                clonedObjective = new FetchItemObjective
                {
                    ObjectiveID = fetchObj.ObjectiveID,
                    Description = fetchObj.Description,
                    ItemIDToFetch = fetchObj.ItemIDToFetch,
                    RequiredQuantity = fetchObj.RequiredQuantity
                };
            }
            else
            {
                Debug.LogError($"Unsupported QuestObjective type encountered during cloning: {objDef.GetType()}. Falling back to SimpleCompletionObjective.");
                clonedObjective = new SimpleCompletionObjective
                {
                    ObjectiveID = objDef.ObjectiveID,
                    Description = objDef.Description
                };
            }

            // Subscribe to the cloned objective's completion event.
            clonedObjective.OnObjectiveCompleted += HandleObjectiveCompleted;
            ObjectiveInstances.Add(clonedObjective);
        }
    }

    public void StartQuest()
    {
        if (CurrentStatus == QuestStatus.NotStarted)
        {
            CurrentStatus = QuestStatus.Active;
            Debug.Log($"Quest '{Definition.QuestName}' started!");
            foreach (var obj in ObjectiveInstances)
            {
                obj.Initialize(); // Initialize each objective for this quest instance.
            }
            OnStatusChanged?.Invoke(this);
        }
    }

    public void FailQuest()
    {
        if (CurrentStatus == QuestStatus.Active)
        {
            CurrentStatus = QuestStatus.Failed;
            Debug.Log($"Quest '{Definition.QuestName}' failed!");
            OnStatusChanged?.Invoke(this);
        }
    }

    // Internal handler for when an objective within this quest completes.
    private void HandleObjectiveCompleted(QuestObjective objective)
    {
        OnObjectiveCompleted?.Invoke(this, objective);
        CheckQuestCompletion(); // Check if all objectives are now done.
    }

    // Attempts to mark a specific objective as complete.
    // This is useful for objectives like SimpleCompletionObjective that are externally triggered.
    public void CompleteObjective(string objectiveID)
    {
        if (CurrentStatus != QuestStatus.Active) return;

        var objective = ObjectiveInstances.FirstOrDefault(o => o.ObjectiveID == objectiveID);
        if (objective != null && !objective.IsCompleted)
        {
            if (objective is SimpleCompletionObjective simpleObj)
            {
                simpleObj.CompleteManually();
            }
            else
            {
                Debug.LogWarning($"Objective '{objectiveID}' for quest '{Definition.QuestName}' cannot be manually completed this way (type: {objective.GetType().Name}). Only SimpleCompletionObjectives can be force-completed.");
            }
        }
        else if (objective == null)
        {
            Debug.LogWarning($"Objective '{objectiveID}' not found in quest '{Definition.QuestName}'.");
        }
    }
    
    // Specifically for FetchItemObjective: Simulates adding an item to the player's inventory.
    public void AddItemToObjective(string itemID, int quantity)
    {
        if (CurrentStatus != QuestStatus.Active) return;
        foreach (var obj in ObjectiveInstances)
        {
            if (obj is FetchItemObjective fetchObj && fetchObj.ItemIDToFetch == itemID && !fetchObj.IsCompleted)
            {
                fetchObj.AddItem(itemID, quantity);
                // The objective itself calls EvaluateProgress, which then calls CheckQuestCompletion if needed.
                break; // Assuming one objective per item for simplicity.
            }
        }
    }

    // Checks if all objectives for this quest are completed. If so, updates quest status.
    public void CheckQuestCompletion()
    {
        if (CurrentStatus != QuestStatus.Active) return;

        bool allObjectivesComplete = ObjectiveInstances.All(obj => obj.IsCompleted);

        if (allObjectivesComplete)
        {
            CurrentStatus = QuestStatus.Completed;
            Debug.Log($"Quest '{Definition.QuestName}' completed!");
            OnStatusChanged?.Invoke(this); // Notify QuestManager
        }
    }
}


// --- 6. QuestManager (Singleton MonoBehaviour) ---
// The central hub for all quest logic, managing definitions, player progress, and branching.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Definitions")]
    [Tooltip("Drag all your QuestDefinition ScriptableObjects here. Ensure their QuestIDs are unique.")]
    public List<QuestDefinition> AllQuestDefinitions;

    // Internal maps for quick lookup of quest blueprints and player's active quests.
    private Dictionary<string, QuestDefinition> _questDefinitionsMap = new Dictionary<string, QuestDefinition>();
    private Dictionary<string, QuestRuntimeState> _playerQuests = new Dictionary<string, QuestRuntimeState>();
    private HashSet<string> _globalFlags = new HashSet<string>(); // Global flags/decisions that persist across the game.

    // Events for external systems (like UI, NPC logic) to subscribe to and react to quest changes.
    public event Action<QuestRuntimeState> OnQuestStarted;
    public event Action<QuestRuntimeState> OnQuestCompleted;
    public event Action<QuestRuntimeState> OnQuestFailed;
    public event Action<string> OnGlobalFlagChanged; // For when a flag is set/unset

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Enforce singleton pattern: destroy duplicate instances.
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Persist QuestManager across scenes.
            // Remove if you want QuestManager to reset on scene load.
            DontDestroyOnLoad(gameObject); 
            Initialize();
        }
    }

    private void Initialize()
    {
        _questDefinitionsMap.Clear();
        foreach (var questDef in AllQuestDefinitions)
        {
            if (_questDefinitionsMap.ContainsKey(questDef.QuestID))
            {
                Debug.LogWarning($"Duplicate QuestID '{questDef.QuestID}' found for '{questDef.QuestName}'. Please ensure all quest IDs are unique.");
                continue;
            }
            _questDefinitionsMap.Add(questDef.QuestID, questDef);
        }

        Debug.Log($"QuestManager initialized with {AllQuestDefinitions.Count} quest definitions.");

        // For a real game, _playerQuests and _globalFlags would be loaded from a save file here.
        // For this example, we start fresh on Awake.
    }

    // --- Public API for Quest Management ---

    /// <summary>
    /// Attempts to start a quest for the player.
    /// It checks prerequisites and creates a new runtime state for the quest.
    /// </summary>
    /// <param name="questID">The ID of the quest to start.</param>
    /// <returns>True if the quest was successfully started or is already active/completed, false otherwise.</returns>
    public bool StartQuest(string questID)
    {
        // 1. Check if quest is already known to the player and its current status.
        if (_playerQuests.TryGetValue(questID, out var questState))
        {
            if (questState.CurrentStatus == QuestStatus.Active || questState.CurrentStatus == QuestStatus.Completed)
            {
                // If already active or completed, no need to start again.
                Debug.Log($"Quest '{questID}' is already {questState.CurrentStatus}.");
                return true;
            }
            if (questState.CurrentStatus == QuestStatus.Failed)
            {
                // If failed, cannot restart with this method. (Design choice: could allow resets).
                Debug.Log($"Quest '{questID}' has already failed. Cannot restart.");
                return false;
            }
        }

        // 2. Get the quest definition blueprint.
        if (!_questDefinitionsMap.TryGetValue(questID, out var questDef))
        {
            Debug.LogError($"QuestDefinition for ID '{questID}' not found in QuestManager's definitions list.");
            return false;
        }

        // 3. Check if the quest is eligible to start based on its prerequisites (branching logic).
        if (!CheckQuestEligibility(questDef))
        {
            Debug.Log($"Quest '{questID}' is not eligible to start (prerequisites not met).");
            return false;
        }

        // 4. Create a new runtime state for the player's quest.
        var newQuestState = new QuestRuntimeState(questDef);
        _playerQuests[questID] = newQuestState; // Add to player's active quests.

        // 5. Subscribe to its events to react to status and objective changes.
        newQuestState.OnStatusChanged += HandleQuestStatusChanged;
        newQuestState.OnObjectiveCompleted += HandleObjectiveCompleted;

        // 6. Start the quest.
        newQuestState.StartQuest();
        OnQuestStarted?.Invoke(newQuestState); // Notify external systems.
        return true;
    }

    /// <summary>
    /// Marks a specific objective within an active quest as complete.
    /// This is typically called by game logic (e.g., an NPC script, a trigger collider).
    /// </summary>
    /// <param name="questID">The ID of the quest the objective belongs to.</param>
    /// <param name="objectiveID">The ID of the objective to complete.</param>
    public void CompleteObjective(string questID, string objectiveID)
    {
        if (_playerQuests.TryGetValue(questID, out var questState))
        {
            if (questState.CurrentStatus == QuestStatus.Active)
            {
                questState.CompleteObjective(objectiveID);
            }
            else
            {
                Debug.LogWarning($"Quest '{questID}' is not active, cannot complete objective '{objectiveID}'. Current status: {questState.CurrentStatus}");
            }
        }
        else
        {
            Debug.LogError($"Quest '{questID}' not found in player's active quests. Cannot complete objective '{objectiveID}'.");
        }
    }
    
    /// <summary>
    /// Simulates adding an item to the player's inventory, which can progress FetchItemObjectives
    /// in any currently active quest.
    /// </summary>
    /// <param name="itemID">The ID of the item being picked up.</param>
    /// <param name="quantity">The quantity of the item.</param>
    public void SimulateAddItem(string itemID, int quantity)
    {
        foreach (var questState in _playerQuests.Values)
        {
            if (questState.CurrentStatus == QuestStatus.Active)
            {
                questState.AddItemToObjective(itemID, quantity);
            }
        }
    }

    /// <summary>
    /// Processes a player's choice for a quest, triggering branching logic.
    /// This will set global flags, unlock/fail other quests, and often completes the quest
    /// that presented the choice.
    /// </summary>
    /// <param name="questID">The ID of the quest where the choice is made.</param>
    /// <param name="choiceIndex">The 0-based index of the chosen option in the quest's BranchingChoices list.</param>
    public void MakeChoice(string questID, int choiceIndex)
    {
        if (!_playerQuests.TryGetValue(questID, out var questState))
        {
            Debug.LogError($"Quest '{questID}' not found in player's active quests for making a choice.");
            return;
        }

        if (questState.Definition.BranchingChoices == null || choiceIndex < 0 || choiceIndex >= questState.Definition.BranchingChoices.Count)
        {
            Debug.LogError($"Invalid choice index {choiceIndex} for quest '{questID}'. Quest has {questState.Definition.BranchingChoices.Count} choices.");
            return;
        }

        QuestChoice chosen = questState.Definition.BranchingChoices[choiceIndex];
        Debug.Log($"Player chose: '{chosen.ChoiceText}' for quest '{questID}'");

        // Apply consequence flags (core branching mechanism)
        foreach (var flag in chosen.ConsequenceFlagsToSet)
        {
            SetGlobalFlag(flag, true);
        }

        // Unlock specific quests (by trying to start them, they'll only start if eligible)
        foreach (var unlockQuestID in chosen.QuestsToUnlock)
        {
            StartQuest(unlockQuestID);
        }

        // Fail specific quests (mutually exclusive paths)
        foreach (var failQuestID in chosen.QuestsToFail)
        {
            MarkQuestAsFailed(failQuestID); // Directly mark as failed.
        }
        
        // After making a choice, the "decision" quest implicitly completes itself.
        // This is a common pattern for quests that serve as decision points rather than task lists.
        MarkQuestAsCompleted(questID); 
    }

    /// <summary>
    /// Gets the current status of a specific quest.
    /// </summary>
    public QuestStatus GetQuestStatus(string questID)
    {
        if (_playerQuests.TryGetValue(questID, out var questState))
        {
            return questState.CurrentStatus;
        }
        // If not found in player quests, check if definition exists (meaning it's not yet started by player)
        if (_questDefinitionsMap.ContainsKey(questID))
        {
            return QuestStatus.NotStarted;
        }
        return QuestStatus.NotStarted; // Or a specific 'NotFound' status if you distinguish
    }

    /// <summary>
    /// Checks if a global flag is currently set.
    /// These flags track player decisions and world states.
    /// </summary>
    public bool HasGlobalFlag(string flag)
    {
        return _globalFlags.Contains(flag);
    }

    /// <summary>
    /// Sets or unsets a global flag. Notifies subscribers of changes.
    /// </summary>
    /// <param name="flag">The string identifier for the flag.</param>
    /// <param name="value">True to set the flag, false to unset it.</param>
    public void SetGlobalFlag(string flag, bool value)
    {
        bool changed = false;
        if (value)
        {
            if (_globalFlags.Add(flag)) // Add returns true if element was new
            {
                changed = true;
                Debug.Log($"Global flag '{flag}' set.");
            }
        }
        else
        {
            if (_globalFlags.Remove(flag)) // Remove returns true if element was found and removed
            {
                changed = true;
                Debug.Log($"Global flag '{flag}' unset.");
            }
        }
        if (changed)
        {
            OnGlobalFlagChanged?.Invoke(flag);
            // After a flag changes, it might make other quests eligible.
            // A comprehensive system might re-evaluate all 'NotStarted' quests here.
            // For this example, we rely on explicit StartQuest calls or follow-up chains.
        }
    }

    // --- Internal Event Handlers ---

    // Handles changes in a quest's runtime status.
    private void HandleQuestStatusChanged(QuestRuntimeState questState)
    {
        switch (questState.CurrentStatus)
        {
            case QuestStatus.Completed:
                ApplyQuestCompletion(questState.Definition.QuestID);
                break;
            case QuestStatus.Failed:
                ApplyQuestFailure(questState.Definition.QuestID);
                break;
            // Handle other status changes if needed (e.g., 'Active' could trigger UI updates)
        }
    }

    // Handles an objective being completed within a quest.
    private void HandleObjectiveCompleted(QuestRuntimeState questState, QuestObjective objective)
    {
        Debug.Log($"Objective '{objective.Description}' for quest '{questState.Definition.QuestName}' completed. All objectives complete: {questState.ObjectiveInstances.All(o => o.IsCompleted)}");
    }

    // --- Core Branching Logic ---

    /// <summary>
    /// Checks if a quest is eligible to be started based on its defined prerequisites and global flags.
    /// This is the heart of the branching system, determining which paths are available.
    /// </summary>
    private bool CheckQuestEligibility(QuestDefinition questDef)
    {
        // Check prerequisite quests: ALL specified quests must be completed.
        foreach (var prereqID in questDef.PrerequisiteQuestIDs)
        {
            if (!_playerQuests.TryGetValue(prereqID, out var prereqQuestState) || prereqQuestState.CurrentStatus != QuestStatus.Completed)
            {
                Debug.Log($"Quest '{questDef.QuestName}' (ID: {questDef.QuestID}) is missing prerequisite quest '{prereqID}' (must be completed).");
                return false;
            }
        }

        // Check prerequisite flags: ALL specified flags must be set.
        foreach (var flag in questDef.PrerequisiteFlags)
        {
            if (!_globalFlags.Contains(flag))
            {
                Debug.Log($"Quest '{questDef.QuestName}' (ID: {questDef.QuestID}) is missing prerequisite flag '{flag}'.");
                return false;
            }
        }

        return true; // All prerequisites met, quest is eligible.
    }

    /// <summary>
    /// Applies completion logic for a quest (rewards, flags, and default follow-up quests).
    /// </summary>
    private void ApplyQuestCompletion(string questID)
    {
        if (_playerQuests.TryGetValue(questID, out var questState) && questState.CurrentStatus == QuestStatus.Completed)
        {
            Debug.Log($"Applying completion logic for quest: '{questState.Definition.QuestName}' (ID: {questID})");

            // Apply rewards (simple debug print for now)
            if (questState.Definition.Rewards.ExperiencePoints > 0)
                Debug.Log($"Received {questState.Definition.Rewards.ExperiencePoints} XP.");
            foreach (var itemID in questState.Definition.Rewards.ItemIDs)
            {
                Debug.Log($"Received item: {itemID}");
            }

            // Set global flags defined for completion (further influences branching).
            foreach (var flag in questState.Definition.FlagsToSetOnCompletion)
            {
                SetGlobalFlag(flag, true);
            }

            // Trigger default follow-up quests.
            // This is typically for linear chains or if no branching choices were presented/made.
            // If the quest had branching choices, 'MakeChoice' already handled unlocking specific follow-ups.
            if (questState.Definition.BranchingChoices.Count == 0) // Only trigger defaults if no explicit choices were defined
            {
                foreach (var followUpID in questState.Definition.DefaultFollowUpQuestIDs)
                {
                    StartQuest(followUpID); // Attempt to start follow-up, it will check eligibility.
                }
            }

            OnQuestCompleted?.Invoke(questState); // Notify external systems.
        }
    }

    /// <summary>
    /// Applies failure logic for a quest.
    /// </summary>
    private void ApplyQuestFailure(string questID)
    {
        if (_playerQuests.TryGetValue(questID, out var questState) && questState.CurrentStatus == QuestStatus.Failed)
        {
            Debug.Log($"Applying failure logic for quest: '{questState.Definition.QuestName}' (ID: {questID})");

            // Set global flags defined for failure scenarios.
            foreach (var flag in questState.Definition.FlagsToSetOnFailure)
            {
                SetGlobalFlag(flag, true);
            }

            // Optionally, trigger specific follow-up quests for failure scenarios.
            // For example, failing a quest might open a "redeem" quest.
            // This is achieved by having other quest definitions check for `FlagsToSetOnFailure` in their prerequisites.

            OnQuestFailed?.Invoke(questState); // Notify external systems.
        }
    }

    // Public method to explicitly mark a quest as failed (e.g., time limit expired, critical NPC died).
    public void MarkQuestAsFailed(string questID)
    {
        if (_playerQuests.TryGetValue(questID, out var questState))
        {
            if (questState.CurrentStatus != QuestStatus.Failed && questState.CurrentStatus != QuestStatus.Completed)
            {
                questState.FailQuest(); // Change runtime state to Failed
                ApplyQuestFailure(questID); // Apply consequences
            }
            else
            {
                Debug.LogWarning($"Quest '{questID}' is already {questState.CurrentStatus}. Cannot mark as failed.");
            }
        }
        else
        {
            // If quest isn't active, but we want to fail it anyway (e.g., pre-emptively fail due to an event)
            if (_questDefinitionsMap.TryGetValue(questID, out var questDef))
            {
                Debug.Log($"Quest '{questID}' was not active, marking as failed proactively.");
                var newQuestState = new QuestRuntimeState(questDef);
                _playerQuests[questID] = newQuestState;
                newQuestState.FailQuest();
                ApplyQuestFailure(questID);
            }
            else
            {
                Debug.LogError($"Quest '{questID}' not found in definitions. Cannot mark as failed.");
            }
        }
    }


    // --- Utility Methods (Example for saving/loading) ---
    // In a real game, these would handle persistent storage.
    public void SaveGame()
    {
        Debug.Log("Saving QuestManager state...");
        // Example: You would serialize _playerQuests (QuestID, CurrentStatus, ObjectiveStates)
        // and _globalFlags to JSON, XML, or a binary format.
        // For instance, using JsonUtility:
        // var saveData = new QuestSaveData(); // A custom serializable class for your save data
        // saveData.playerQuestStates = _playerQuests.Values.Select(q => new QuestStateDTO(q)).ToList();
        // saveData.globalFlags = _globalFlags.ToList();
        // string json = JsonUtility.ToJson(saveData);
        // System.IO.File.WriteAllText("quests_save.json", json);
    }

    public void LoadGame()
    {
        Debug.Log("Loading QuestManager state...");
        // Example: Deserialize from a save file and reconstruct the state.
        // Clear current state
        _playerQuests.Clear();
        _globalFlags.Clear();

        // Then, load from file:
        // if (System.IO.File.Exists("quests_save.json"))
        // {
        //     string json = System.IO.File.ReadAllText("quests_save.json");
        //     QuestSaveData loadedData = JsonUtility.FromJson<QuestSaveData>(json);
        //     foreach (var dto in loadedData.playerQuestStates)
        //     {
        //         if (_questDefinitionsMap.TryGetValue(dto.QuestId, out var def))
        //         {
        //             var loadedQuestState = new QuestRuntimeState(def);
        //             loadedQuestState.LoadFromDTO(dto); // Custom method to apply loaded objective states
        //             _playerQuests[dto.QuestId] = loadedQuestState;
        //             loadedQuestState.OnStatusChanged += HandleQuestStatusChanged;
        //             loadedQuestState.OnObjectiveCompleted += HandleObjectiveCompleted;
        //         }
        //     }
        //     foreach (var flag in loadedData.globalFlags)
        //     {
        //         _globalFlags.Add(flag);
        //     }
        // }
        // For this demo, just resetting means all quests are 'NotStarted' and no flags set.
        Debug.Log("QuestManager state reset (simulated load).");
    }

    // --- Debugging / Testing UI (for the example) ---
    [Header("Debug Controls (Play Mode Only)")]
    public string DebugQuestIDToStart;
    public string DebugQuestIDToCompleteObjective;
    public string DebugObjectiveIDToComplete;
    public string DebugQuestIDToMakeChoice;
    [Tooltip("0-based index of the choice in the quest's BranchingChoices list.")]
    public int DebugChoiceIndex;
    public string DebugItemIDToSimulatePickup;
    public int DebugItemQuantityToSimulatePickup;
    public string DebugFlagToSet;

    [ContextMenu("Start Debug Quest")]
    void StartDebugQuest() => StartQuest(DebugQuestIDToStart);

    [ContextMenu("Complete Debug Objective")]
    void CompleteDebugObjective() => CompleteObjective(DebugQuestIDToCompleteObjective, DebugObjectiveIDToComplete);

    [ContextMenu("Make Debug Choice")]
    void MakeDebugChoice() => MakeChoice(DebugQuestIDToMakeChoice, DebugChoiceIndex);

    [ContextMenu("Simulate Item Pickup")]
    void SimulateItemPickup() => SimulateAddItem(DebugItemIDToSimulatePickup, DebugItemQuantityToSimulatePickup);
    
    [ContextMenu("Set Debug Flag")]
    void SetDebugFlag() => SetGlobalFlag(DebugFlagToSet, true);

    [ContextMenu("Print All Flags")]
    void PrintAllFlags()
    {
        Debug.Log("--- Current Global Flags ---");
        if (_globalFlags.Count == 0) Debug.Log("No flags set.");
        foreach (var flag in _globalFlags)
        {
            Debug.Log($"- {flag}");
        }
        Debug.Log("----------------------------");
    }

    [ContextMenu("Print All Quests Status")]
    void PrintAllQuestsStatus()
    {
        Debug.Log("--- Current Player Quests Status ---");
        if (_playerQuests.Count == 0) Debug.Log("No quests in progress.");
        foreach (var quest in _playerQuests.Values)
        {
            Debug.Log($"- {quest.Definition.QuestName} (ID: {quest.Definition.QuestID}) Status: {quest.CurrentStatus}");
            foreach (var obj in quest.ObjectiveInstances)
            {
                Debug.Log($"  - Objective: {obj.Description} (Completed: {obj.IsCompleted})");
                if (obj is FetchItemObjective fetchObj)
                {
                    Debug.Log($"    - Item: {fetchObj.ItemIDToFetch}, Current: {fetchObj._currentQuantity}/{fetchObj.RequiredQuantity}");
                }
            }
        }
        Debug.Log("------------------------------------");

        Debug.Log("--- All Quest Eligibility (Not Started Definitions) ---");
        foreach (var questDef in AllQuestDefinitions)
        {
            // Only show eligibility for quests that the player hasn't started yet.
            if (!_playerQuests.ContainsKey(questDef.QuestID)) 
            {
                 Debug.Log($"- {questDef.QuestName} (ID: {questDef.QuestID}) - Eligible: {CheckQuestEligibility(questDef)}");
            }
        }
        Debug.Log("-------------------------------------------");
    }
}
```