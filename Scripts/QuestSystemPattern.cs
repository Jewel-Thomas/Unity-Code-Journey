// Unity Design Pattern Example: QuestSystemPattern
// This script demonstrates the QuestSystemPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The `QuestSystemPattern` design pattern in Unity aims to create a flexible, scalable, and data-driven quest system. It leverages several common design patterns like **Strategy**, **Observer**, **Factory Method**, and **Scriptable Objects** to achieve this.

This example provides a complete, practical implementation ready to be dropped into a Unity project. It demonstrates:

1.  **Data-Driven Quests & Objectives:** Using `ScriptableObject` assets for quest and objective definitions.
2.  **Runtime State Management:** Separate classes for the runtime state of quests and objectives.
3.  **Flexible Objective Types:** Easily extendable for different kinds of objectives (kill, collect, talk, etc.).
4.  **Event-Driven Progress:** Using a static `GameEvents` class to decouple game actions from quest progress updates.
5.  **Centralized Quest Management:** A `QuestManager` singleton handles all quest lifecycle events.
6.  **Player Inventory & Rewards:** Basic mockups for demonstrating quest rewards.

---

### How the QuestSystemPattern Works:

*   **`ScriptableObject` Definitions (Data Layer):**
    *   `QuestSO`: Defines a quest's name, description, rewards, and a list of `QuestObjectiveSO`s. These are assets you create in the Unity editor.
    *   `QuestObjectiveSO` (abstract base): Defines common properties for all objective types.
    *   `CollectObjectiveSO`, `KillObjectiveSO`, `TalkObjectiveSO` (concrete): Inherit from `QuestObjectiveSO` and add specific data (e.g., item to collect, enemy to kill).
    *   These `ScriptableObject`s act as **factories** to create their runtime counterparts.
*   **Runtime Classes (Logic & State Layer):**
    *   `RuntimeQuest`: An instance created from a `QuestSO` when a player accepts a quest. It tracks the quest's current status and holds a list of `RuntimeQuestObjective`s.
    *   `RuntimeQuestObjective` (abstract base): An instance created from a `QuestObjectiveSO`. It tracks the current progress towards that objective (e.g., 2/5 items collected).
    *   `RuntimeCollectObjective`, `RuntimeKillObjective`, `RuntimeTalkObjective` (concrete): Inherit from `RuntimeQuestObjective` and implement the specific logic for how their progress is updated and checked. These use the **Strategy Pattern** to define different ways to complete an objective.
*   **`QuestManager` (System Layer):**
    *   A `MonoBehaviour` **Singleton** that acts as the central hub for the quest system.
    *   Manages lists of available, active, completed, and failed quests.
    *   Subscribes to global `GameEvents` (e.g., `OnEnemyKilled`, `OnItemCollected`) and dispatches these events to active `RuntimeQuest`s to update their objectives. This uses the **Observer Pattern**.
    *   Provides methods for accepting, completing, and failing quests.
*   **`GameEvents` (Event Hub):**
    *   A static class with `public static event Action` delegates. This is a simple, effective way to broadcast events across different parts of the game without tight coupling.
*   **Player & Inventory Mockups:** Simple classes (`Player`, `ItemSO`, `PlayerInventory`) are included to demonstrate quest rewards.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuestSystemPattern
{
    // --- Core Enums and Delegates ---

    /// <summary>
    /// Represents the current status of a quest.
    /// </summary>
    public enum QuestStatus
    {
        Available,  // Quest is ready to be accepted
        Active,     // Quest has been accepted and is in progress
        Completed,  // All objectives are met, but rewards not yet claimed
        Failed,     // Quest conditions were not met (e.g., time limit, specific failure condition)
        Rewarded    // Quest is fully completed and rewards have been given
    }

    /// <summary>
    /// Delegates for quest and objective events.
    /// </summary>
    public static class QuestEvents
    {
        public static event Action<RuntimeQuest> OnQuestAccepted;
        public static event Action<RuntimeQuest> OnQuestCompleted; // When all objectives are met
        public static event Action<RuntimeQuest> OnQuestRewarded;  // When rewards are claimed
        public static event Action<RuntimeQuest> OnQuestFailed;
        public static event Action<RuntimeQuestObjective> OnObjectiveProgressChanged;
        public static event Action<RuntimeQuestObjective> OnObjectiveCompleted;

        // Helper methods to invoke events safely
        public static void InvokeQuestAccepted(RuntimeQuest quest) => OnQuestAccepted?.Invoke(quest);
        public static void InvokeQuestCompleted(RuntimeQuest quest) => OnQuestCompleted?.Invoke(quest);
        public static void InvokeQuestRewarded(RuntimeQuest quest) => OnQuestRewarded?.Invoke(quest);
        public static void InvokeQuestFailed(RuntimeQuest quest) => OnQuestFailed?.Invoke(quest);
        public static void InvokeObjectiveProgressChanged(RuntimeQuestObjective objective) => OnObjectiveProgressChanged?.Invoke(objective);
        public static void InvokeObjectiveCompleted(RuntimeQuestObjective objective) => OnObjectiveCompleted?.Invoke(objective);
    }

    // --- Item/Player Mockup (for rewards) ---
    // These are simplified for the example. In a real game, these would be more complex.

    /// <summary>
    /// A simple ScriptableObject to represent an item.
    /// Used for quest rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "QuestSystem/Item")]
    public class ItemSO : ScriptableObject
    {
        public string ItemName;
        public Sprite Icon; // Optional: visual representation
        public int Value;   // Optional: for currency or selling
    }

    /// <summary>
    /// A mockup of a player's inventory.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        private Dictionary<ItemSO, int> items = new Dictionary<ItemSO, int>();
        public int Gold { get; private set; } = 0;
        public int Experience { get; private set; } = 0;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public void AddItem(ItemSO item, int count)
        {
            if (items.ContainsKey(item))
            {
                items[item] += count;
            }
            else
            {
                items.Add(item, count);
            }
            Debug.Log($"Added {count}x {item.ItemName}. Total: {items[item]}");
            // In a real game, notify UI
        }

        public bool HasItem(ItemSO item, int count)
        {
            return items.ContainsKey(item) && items[item] >= count;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            Debug.Log($"Added {amount} Gold. Total: {Gold}");
        }

        public void AddExperience(int amount)
        {
            Experience += amount;
            Debug.Log($"Added {amount} XP. Total: {Experience}");
        }

        public void DebugLogInventory()
        {
            Debug.Log("--- Inventory ---");
            foreach (var kvp in items)
            {
                Debug.Log($"- {kvp.Key.ItemName}: {kvp.Value}");
            }
            Debug.Log($"- Gold: {Gold}");
            Debug.Log($"- XP: {Experience}");
            Debug.Log("-----------------");
        }
    }

    /// <summary>
    /// A mockup of the player character, primarily for event interaction.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        public Transform CurrentTarget { get; set; } // For simulating interaction with NPCs/enemies

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // Example method for player action
        public void PerformAttack()
        {
            if (CurrentTarget != null)
            {
                Debug.Log($"Player attacks {CurrentTarget.name}");
                // Simulate enemy death after attack
                if (CurrentTarget.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    enemy.TakeDamage(); // In a real game, this would be more complex
                }
            }
            else
            {
                Debug.Log("Player attacks thin air.");
            }
        }

        public void CollectItem(ItemSO item, int amount = 1)
        {
            Debug.Log($"Player collected {amount}x {item.ItemName}");
            // Notify quest system about collected item
            GameEvents.InvokeOnItemCollected(item, amount);
        }
    }

    // --- GameEvents Static Class ---
    /// <summary>
    /// A central static class for game-wide events that the Quest System listens to.
    /// This decouples the quest system from direct game logic.
    /// Uses the Observer Pattern.
    /// </summary>
    public static class GameEvents
    {
        // Event for when an enemy is killed
        public static event Action<string> OnEnemyKilled;
        public static void InvokeOnEnemyKilled(string enemyType) => OnEnemyKilled?.Invoke(enemyType);

        // Event for when an item is collected
        public static event Action<ItemSO, int> OnItemCollected;
        public static void InvokeOnItemCollected(ItemSO item, int amount) => OnItemCollected?.Invoke(item, amount);

        // Event for when an NPC is interacted with
        public static event Action<string> OnNPCInteracted;
        public static void InvokeOnNPCInteracted(string npcID) => OnNPCInteracted?.Invoke(npcID);
    }

    // --- Base Quest Objective Definition (ScriptableObject) ---

    /// <summary>
    /// Base class for defining a Quest Objective's data.
    /// Each concrete objective type will inherit from this.
    /// Uses the Factory Method Pattern to create runtime instances.
    /// </summary>
    public abstract class QuestObjectiveSO : ScriptableObject
    {
        [Header("Objective Definition")]
        public string ObjectiveText;
        public bool IsOptional;

        /// <summary>
        /// Factory method to create a runtime instance of this objective.
        /// </summary>
        /// <returns>A new RuntimeQuestObjective instance.</returns>
        public abstract RuntimeQuestObjective CreateRuntimeObjectiveInstance();
    }

    // --- Concrete Quest Objective Definitions (ScriptableObjects) ---

    /// <summary>
    /// Defines a 'Kill X Enemies' objective.
    /// </summary>
    [CreateAssetMenu(fileName = "KillObjective", menuName = "QuestSystem/Objectives/Kill")]
    public class KillObjectiveSO : QuestObjectiveSO
    {
        [Header("Kill Specifics")]
        public string TargetEnemyType; // e.g., "Goblin", "Slime"
        public int AmountToKill;

        public override RuntimeQuestObjective CreateRuntimeObjectiveInstance()
        {
            return new RuntimeKillObjective(this);
        }
    }

    /// <summary>
    /// Defines a 'Collect X Items' objective.
    /// </summary>
    [CreateAssetMenu(fileName = "CollectObjective", menuName = "QuestSystem/Objectives/Collect")]
    public class CollectObjectiveSO : QuestObjectiveSO
    {
        [Header("Collect Specifics")]
        public ItemSO ItemRequired;
        public int AmountRequired;

        public override RuntimeQuestObjective CreateRuntimeObjectiveInstance()
        {
            return new RuntimeCollectObjective(this);
        }
    }

    /// <summary>
    /// Defines a 'Talk to NPC' objective.
    /// </summary>
    [CreateAssetMenu(fileName = "TalkObjective", menuName = "QuestSystem/Objectives/Talk")]
    public class TalkObjectiveSO : QuestObjectiveSO
    {
        [Header("Talk Specifics")]
        public string TargetNPC_ID; // Unique identifier for the NPC

        public override RuntimeQuestObjective CreateRuntimeObjectiveInstance()
        {
            return new RuntimeTalkObjective(this);
        }
    }


    // --- Base Runtime Quest Objective ---

    /// <summary>
    /// Base class for a runtime quest objective instance.
    /// This object tracks the player's progress towards completing the objective.
    /// Uses the Strategy Pattern for specific objective logic.
    /// </summary>
    public abstract class RuntimeQuestObjective
    {
        public QuestObjectiveSO Definition { get; private set; }
        public string ObjectiveText => Definition.ObjectiveText;
        public bool IsOptional => Definition.IsOptional;

        public int CurrentProgress { get; protected set; }
        public bool IsCompleted { get; protected set; }

        public RuntimeQuestObjective(QuestObjectiveSO definition)
        {
            Definition = definition;
            CurrentProgress = 0;
            IsCompleted = false;
        }

        /// <summary>
        /// Attempts to advance the objective based on a game event.
        /// This is the core 'strategy' method.
        /// </summary>
        /// <param name="eventArgs">Arguments relevant to the game event.</param>
        /// <returns>True if progress was made, false otherwise.</returns>
        public abstract bool TryAdvanceProgress(params object[] eventArgs);

        /// <summary>
        /// Sets the objective as completed, regardless of current progress.
        /// </summary>
        public void ForceComplete()
        {
            if (!IsCompleted)
            {
                CurrentProgress = GetRequiredProgress(); // Set to max for consistency
                IsCompleted = true;
                Debug.Log($"Objective '{ObjectiveText}' force-completed.");
                QuestEvents.InvokeObjectiveCompleted(this);
                QuestEvents.InvokeObjectiveProgressChanged(this); // To update UI
            }
        }

        /// <summary>
        /// Checks if the objective's conditions are met.
        /// </summary>
        /// <returns>True if completed, false otherwise.</returns>
        protected virtual bool CheckCompletion()
        {
            return CurrentProgress >= GetRequiredProgress();
        }

        /// <summary>
        /// Returns the required progress for this objective type.
        /// </summary>
        public abstract int GetRequiredProgress();
    }

    // --- Concrete Runtime Quest Objectives ---

    /// <summary>
    /// Runtime instance for a 'Kill X Enemies' objective.
    /// </summary>
    public class RuntimeKillObjective : RuntimeQuestObjective
    {
        private KillObjectiveSO killDefinition => (KillObjectiveSO)Definition;

        public string TargetEnemyType => killDefinition.TargetEnemyType;
        public int AmountToKill => killDefinition.AmountToKill;

        public RuntimeKillObjective(KillObjectiveSO definition) : base(definition) { }

        public override bool TryAdvanceProgress(params object[] eventArgs)
        {
            if (IsCompleted) return false;

            if (eventArgs.Length > 0 && eventArgs[0] is string killedEnemyType)
            {
                if (killedEnemyType == TargetEnemyType)
                {
                    CurrentProgress++;
                    Debug.Log($"Kill objective '{ObjectiveText}' progress: {CurrentProgress}/{AmountToKill}");
                    QuestEvents.InvokeObjectiveProgressChanged(this);
                    if (CheckCompletion())
                    {
                        IsCompleted = true;
                        Debug.Log($"Objective '{ObjectiveText}' completed!");
                        QuestEvents.InvokeObjectiveCompleted(this);
                    }
                    return true;
                }
            }
            return false;
        }

        public override int GetRequiredProgress() => AmountToKill;
    }

    /// <summary>
    /// Runtime instance for a 'Collect X Items' objective.
    /// </summary>
    public class RuntimeCollectObjective : RuntimeQuestObjective
    {
        private CollectObjectiveSO collectDefinition => (CollectObjectiveSO)Definition;

        public ItemSO ItemRequired => collectDefinition.ItemRequired;
        public int AmountRequired => collectDefinition.AmountRequired;

        public RuntimeCollectObjective(CollectObjectiveSO definition) : base(definition) { }

        public override bool TryAdvanceProgress(params object[] eventArgs)
        {
            if (IsCompleted) return false;

            if (eventArgs.Length >= 2 && eventArgs[0] is ItemSO collectedItem && eventArgs[1] is int collectedAmount)
            {
                if (collectedItem == ItemRequired)
                {
                    CurrentProgress += collectedAmount;
                    CurrentProgress = Mathf.Min(CurrentProgress, AmountRequired); // Cap progress at required amount
                    Debug.Log($"Collect objective '{ObjectiveText}' progress: {CurrentProgress}/{AmountRequired}");
                    QuestEvents.InvokeObjectiveProgressChanged(this);
                    if (CheckCompletion())
                    {
                        IsCompleted = true;
                        Debug.Log($"Objective '{ObjectiveText}' completed!");
                        QuestEvents.InvokeObjectiveCompleted(this);
                    }
                    return true;
                }
            }
            return false;
        }

        public override int GetRequiredProgress() => AmountRequired;
    }

    /// <summary>
    /// Runtime instance for a 'Talk to NPC' objective.
    /// </summary>
    public class RuntimeTalkObjective : RuntimeQuestObjective
    {
        private TalkObjectiveSO talkDefinition => (TalkObjectiveSO)Definition;

        public string TargetNPC_ID => talkDefinition.TargetNPC_ID;

        public RuntimeTalkObjective(TalkObjectiveSO definition) : base(definition) { }

        public override bool TryAdvanceProgress(params object[] eventArgs)
        {
            if (IsCompleted) return false;

            if (eventArgs.Length > 0 && eventArgs[0] is string interactedNPC_ID)
            {
                if (interactedNPC_ID == TargetNPC_ID)
                {
                    CurrentProgress = 1; // Talking is usually a single step objective
                    Debug.Log($"Talk objective '{ObjectiveText}' progress: Completed.");
                    QuestEvents.InvokeObjectiveProgressChanged(this);
                    if (CheckCompletion())
                    {
                        IsCompleted = true;
                        Debug.Log($"Objective '{ObjectiveText}' completed!");
                        QuestEvents.InvokeObjectiveCompleted(this);
                    }
                    return true;
                }
            }
            return false;
        }

        public override int GetRequiredProgress() => 1; // Always 1 for a talk objective
    }


    // --- Quest Definition (ScriptableObject) ---

    /// <summary>
    /// Defines a Quest. This is a ScriptableObject asset.
    /// It contains the quest's metadata and a list of objective definitions.
    /// Uses the Factory Method Pattern to create runtime instances.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "QuestSystem/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Header("Quest Definition")]
        public string QuestName;
        [TextArea] public string Description;
        public int QuestID = -1; // Unique identifier for the quest

        [Header("Objectives")]
        public List<QuestObjectiveSO> Objectives = new List<QuestObjectiveSO>();

        [Header("Rewards")]
        public int RewardExperience;
        public int RewardGold;
        public List<ItemSO> RewardItems = new List<ItemSO>();
        public List<int> RewardItemAmounts = new List<int>(); // Corresponds to RewardItems

        void OnValidate()
        {
            // Ensure item amounts list matches item list size
            while (RewardItemAmounts.Count < RewardItems.Count)
            {
                RewardItemAmounts.Add(1);
            }
            while (RewardItemAmounts.Count > RewardItems.Count)
            {
                RewardItemAmounts.RemoveAt(RewardItemAmounts.Count - 1);
            }

            if (QuestID == -1) // Assign a unique ID if not set
            {
                QuestID = GetHashCode(); 
            }
        }

        /// <summary>
        /// Factory method to create a runtime instance of this quest.
        /// </summary>
        /// <returns>A new RuntimeQuest instance.</returns>
        public RuntimeQuest CreateRuntimeQuestInstance()
        {
            return new RuntimeQuest(this);
        }
    }


    // --- Runtime Quest Class ---

    /// <summary>
    /// Represents a quest in the game world that a player has accepted.
    /// It manages the status and progress of its objectives.
    /// </summary>
    public class RuntimeQuest
    {
        public QuestSO Definition { get; private set; }
        public string Name => Definition.QuestName;
        public string Description => Definition.Description;
        public int QuestID => Definition.QuestID;

        public QuestStatus CurrentStatus { get; private set; }
        public List<RuntimeQuestObjective> RuntimeObjectives { get; private set; } = new List<RuntimeQuestObjective>();

        public RuntimeQuest(QuestSO definition)
        {
            Definition = definition;
            CurrentStatus = QuestStatus.Available; // Initially available, will be set to Active by QuestManager
            InitializeObjectives();
        }

        private void InitializeObjectives()
        {
            foreach (var objectiveSO in Definition.Objectives)
            {
                RuntimeObjectives.Add(objectiveSO.CreateRuntimeObjectiveInstance());
            }
        }

        /// <summary>
        /// Sets the quest to active. Called by QuestManager.
        /// </summary>
        public void Activate()
        {
            if (CurrentStatus == QuestStatus.Available)
            {
                CurrentStatus = QuestStatus.Active;
                Debug.Log($"Quest '{Name}' activated!");
                QuestEvents.InvokeQuestAccepted(this);
            }
        }

        /// <summary>
        /// Checks if the quest can be completed (all non-optional objectives are met).
        /// </summary>
        /// <returns>True if all required objectives are completed, false otherwise.</returns>
        public bool CheckCompletionConditions()
        {
            // A quest is completable if all non-optional objectives are completed
            // and all optional objectives are either completed or ignored.
            return RuntimeObjectives.All(obj => obj.IsCompleted || obj.IsOptional);
        }

        /// <summary>
        /// Attempts to complete the quest. This method should only be called by QuestManager
        /// after `CheckCompletionConditions()` returns true.
        /// </summary>
        public void CompleteQuest()
        {
            if (CurrentStatus == QuestStatus.Active && CheckCompletionConditions())
            {
                CurrentStatus = QuestStatus.Completed;
                Debug.Log($"Quest '{Name}' completed!");
                QuestEvents.InvokeQuestCompleted(this);
            }
        }

        /// <summary>
        /// Applies rewards for the quest. This should be called once the quest is completed
        /// and the player chooses to claim rewards (e.g., from a Quest Giver).
        /// </summary>
        public void GiveRewards()
        {
            if (CurrentStatus == QuestStatus.Completed)
            {
                if (PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.AddExperience(Definition.RewardExperience);
                    PlayerInventory.Instance.AddGold(Definition.RewardGold);
                    for (int i = 0; i < Definition.RewardItems.Count; i++)
                    {
                        if (Definition.RewardItems[i] != null && i < Definition.RewardItemAmounts.Count)
                        {
                            PlayerInventory.Instance.AddItem(Definition.RewardItems[i], Definition.RewardItemAmounts[i]);
                        }
                    }
                }
                CurrentStatus = QuestStatus.Rewarded;
                Debug.Log($"Rewards for '{Name}' claimed!");
                QuestEvents.InvokeQuestRewarded(this);
            }
        }

        /// <summary>
        /// Fails the quest.
        /// </summary>
        public void FailQuest()
        {
            if (CurrentStatus == QuestStatus.Active)
            {
                CurrentStatus = QuestStatus.Failed;
                Debug.Log($"Quest '{Name}' failed!");
                QuestEvents.InvokeQuestFailed(this);
            }
        }

        /// <summary>
        /// Attempts to advance any active objective for this quest based on an event.
        /// </summary>
        /// <param name="eventType">A string identifying the type of game event (e.g., "Kill", "Collect", "Talk").</param>
        /// <param name="eventArgs">Specific arguments for the event.</param>
        public void TryAdvanceObjective(string eventType, params object[] eventArgs)
        {
            if (CurrentStatus != QuestStatus.Active) return;

            bool progressMade = false;
            foreach (var objective in RuntimeObjectives)
            {
                if (!objective.IsCompleted)
                {
                    // This is where specific objective types handle specific events
                    if (objective is RuntimeKillObjective killObj && eventType == "Kill")
                    {
                        if (killObj.TryAdvanceProgress(eventArgs)) progressMade = true;
                    }
                    else if (objective is RuntimeCollectObjective collectObj && eventType == "Collect")
                    {
                        if (collectObj.TryAdvanceProgress(eventArgs)) progressMade = true;
                    }
                    else if (objective is RuntimeTalkObjective talkObj && eventType == "Talk")
                    {
                        if (talkObj.TryAdvanceProgress(eventArgs)) progressMade = true;
                    }
                    // Add more objective types here
                }
            }

            if (progressMade && CheckCompletionConditions())
            {
                CompleteQuest();
            }
        }
    }


    // --- Quest Manager (MonoBehaviour Singleton) ---

    /// <summary>
    /// The central system for managing all quests.
    /// It's a MonoBehaviour Singleton, providing easy global access.
    /// It subscribes to GameEvents to update quest progress.
    /// Uses the Singleton and Observer Patterns.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Debug - Drag Available Quests Here")]
        public List<QuestSO> availableQuestDefinitions; // Quests the player can accept from the start (or from quest givers)

        private Dictionary<int, RuntimeQuest> activeQuests = new Dictionary<int, RuntimeQuest>();
        private Dictionary<int, RuntimeQuest> completedQuests = new Dictionary<int, RuntimeQuest>();
        private Dictionary<int, RuntimeQuest> failedQuests = new Dictionary<int, RuntimeQuest>();
        private Dictionary<int, RuntimeQuest> rewardedQuests = new Dictionary<int, RuntimeQuest>(); // Quests that have given rewards

        // Public lists for UI/debug access
        public IReadOnlyCollection<RuntimeQuest> ActiveQuests => activeQuests.Values;
        public IReadOnlyCollection<RuntimeQuest> CompletedQuests => completedQuests.Values;
        public IReadOnlyCollection<RuntimeQuest> FailedQuests => failedQuests.Values;
        public IReadOnlyCollection<RuntimeQuest> RewardedQuests => rewardedQuests.Values;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep QuestManager across scenes
            }
        }

        void OnEnable()
        {
            // Subscribe to game events to update quest progress
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnItemCollected += HandleItemCollected;
            GameEvents.OnNPCInteracted += HandleNPCInteracted;

            // Subscribe to quest events for internal management (moving quests between lists)
            QuestEvents.OnQuestCompleted += HandleQuestCompleted;
            QuestEvents.OnQuestRewarded += HandleQuestRewarded;
            QuestEvents.OnQuestFailed += HandleQuestFailed;
        }

        void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnItemCollected -= HandleItemCollected;
            GameEvents.OnNPCInteracted -= HandleNPCInteracted;

            QuestEvents.OnQuestCompleted -= HandleQuestCompleted;
            QuestEvents.OnQuestRewarded -= HandleQuestRewarded;
            QuestEvents.OnQuestFailed -= HandleQuestFailed;
        }

        void Start()
        {
            // Automatically make quests in the 'availableQuestDefinitions' list available
            foreach (var questSO in availableQuestDefinitions)
            {
                // In a real game, you might manage a separate list of truly 'available' quests
                // and use an NPC to trigger 'AcceptQuest'. For this example, we assume
                // these are quests ready to be accepted by the player.
            }
        }

        /// <summary>
        /// Accepts a quest, creating a runtime instance and adding it to active quests.
        /// </summary>
        public void AcceptQuest(QuestSO questDefinition)
        {
            if (questDefinition == null)
            {
                Debug.LogError("Attempted to accept a null quest definition.");
                return;
            }
            if (activeQuests.ContainsKey(questDefinition.QuestID))
            {
                Debug.LogWarning($"Quest '{questDefinition.QuestName}' is already active.");
                return;
            }
            if (completedQuests.ContainsKey(questDefinition.QuestID) || rewardedQuests.ContainsKey(questDefinition.QuestID))
            {
                Debug.LogWarning($"Quest '{questDefinition.QuestName}' has already been completed or rewarded.");
                return;
            }

            RuntimeQuest newQuest = questDefinition.CreateRuntimeQuestInstance();
            activeQuests.Add(newQuest.QuestID, newQuest);
            newQuest.Activate(); // Set its status to active
            Debug.Log($"Quest '{newQuest.Name}' accepted and added to active quests.");
        }

        /// <summary>
        /// Attempts to claim rewards for a completed quest.
        /// </summary>
        public void ClaimQuestRewards(RuntimeQuest quest)
        {
            if (quest == null) return;

            if (quest.CurrentStatus == QuestStatus.Completed)
            {
                quest.GiveRewards();
                // QuestEvents.OnQuestRewarded will handle moving it to rewardedQuests
            }
            else
            {
                Debug.LogWarning($"Cannot claim rewards for quest '{quest.Name}'. Status is {quest.CurrentStatus}, expected {QuestStatus.Completed}.");
            }
        }

        /// <summary>
        /// Retrieves an active quest by its ID.
        /// </summary>
        public RuntimeQuest GetActiveQuest(int questID)
        {
            activeQuests.TryGetValue(questID, out RuntimeQuest quest);
            return quest;
        }

        // --- Event Handlers for Game Events ---
        private void HandleEnemyKilled(string enemyType)
        {
            foreach (var quest in activeQuests.Values.ToList()) // ToList() to prevent modification during iteration
            {
                quest.TryAdvanceObjective("Kill", enemyType);
            }
        }

        private void HandleItemCollected(ItemSO item, int amount)
        {
            foreach (var quest in activeQuests.Values.ToList())
            {
                quest.TryAdvanceObjective("Collect", item, amount);
            }
        }

        private void HandleNPCInteracted(string npcID)
        {
            foreach (var quest in activeQuests.Values.ToList())
            {
                quest.TryAdvanceObjective("Talk", npcID);
            }
        }

        // --- Event Handlers for Quest Events (Internal Management) ---
        private void HandleQuestCompleted(RuntimeQuest quest)
        {
            if (activeQuests.Remove(quest.QuestID))
            {
                completedQuests.Add(quest.QuestID, quest);
                Debug.Log($"QuestManager: Moved '{quest.Name}' from Active to Completed.");
            }
        }

        private void HandleQuestRewarded(RuntimeQuest quest)
        {
            if (completedQuests.Remove(quest.QuestID))
            {
                rewardedQuests.Add(quest.QuestID, quest);
                Debug.Log($"QuestManager: Moved '{quest.Name}' from Completed to Rewarded.");
            }
        }

        private void HandleQuestFailed(RuntimeQuest quest)
        {
            if (activeQuests.Remove(quest.QuestID))
            {
                failedQuests.Add(quest.QuestID, quest);
                Debug.Log($"QuestManager: Moved '{quest.Name}' from Active to Failed.");
            }
        }

        public void DebugLogAllQuests()
        {
            Debug.Log("\n--- Current Quest State ---");
            Debug.Log("Active Quests:");
            foreach (var q in ActiveQuests)
            {
                Debug.Log($"- {q.Name} ({q.CurrentStatus})");
                foreach (var obj in q.RuntimeObjectives)
                {
                    Debug.Log($"  - {obj.ObjectiveText}: {obj.CurrentProgress}/{obj.GetRequiredProgress()} (Completed: {obj.IsCompleted})");
                }
            }
            Debug.Log("Completed Quests (Awaiting Rewards):");
            foreach (var q in CompletedQuests) Debug.Log($"- {q.Name}");
            Debug.Log("Rewarded Quests:");
            foreach (var q in RewardedQuests) Debug.Log($"- {q.Name}");
            Debug.Log("Failed Quests:");
            foreach (var q in FailedQuests) Debug.Log($"- {q.Name}");
            Debug.Log("---------------------------");
        }
    }

    // --- Example Quest Giver (MonoBehaviour) ---

    /// <summary>
    /// A simple component to simulate an NPC that gives quests.
    /// It demonstrates how to interact with the QuestManager.
    /// </summary>
    public class QuestGiver : MonoBehaviour
    {
        public string NPC_ID = "QuestGiver_001"; // Unique ID for this NPC
        public QuestSO QuestToGive;
        public QuestSO QuestToClaim; // If this NPC also claims rewards for a specific quest

        public void Interact()
        {
            Debug.Log($"Player interacted with {gameObject.name} ({NPC_ID}).");
            GameEvents.InvokeOnNPCInteracted(NPC_ID); // Notify any 'Talk' objectives

            if (QuestToGive != null && !QuestManager.Instance.ActiveQuests.Any(q => q.QuestID == QuestToGive.QuestID) &&
                                        !QuestManager.Instance.CompletedQuests.Any(q => q.QuestID == QuestToGive.QuestID) &&
                                        !QuestManager.Instance.RewardedQuests.Any(q => q.QuestID == QuestToGive.QuestID))
            {
                QuestManager.Instance.AcceptQuest(QuestToGive);
                QuestToGive = null; // Don't give it again for this example
            }
            else if (QuestToClaim != null && QuestManager.Instance.CompletedQuests.Any(q => q.QuestID == QuestToClaim.QuestID))
            {
                RuntimeQuest questToClaim = QuestManager.Instance.CompletedQuests.FirstOrDefault(q => q.QuestID == QuestToClaim.QuestID);
                if (questToClaim != null)
                {
                    QuestManager.Instance.ClaimQuestRewards(questToClaim);
                    QuestToClaim = null; // Rewards claimed
                }
            }
            else
            {
                Debug.Log("QuestGiver: Nothing new to give or claim at the moment.");
            }
        }
    }

    // --- Example Enemy (MonoBehaviour) ---

    /// <summary>
    /// A simple enemy that can be killed to advance 'Kill' objectives.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public string EnemyType = "Goblin"; // e.g., "Goblin", "Slime"
        public int Health = 1;

        public void TakeDamage(int damage = 1)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Die();
            }
            else
            {
                Debug.Log($"{gameObject.name} took {damage} damage. Health: {Health}");
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} ({EnemyType}) was killed!");
            GameEvents.InvokeOnEnemyKilled(EnemyType); // Notify quest system
            Destroy(gameObject);
        }
    }

    // --- Demo Script ---

    /// <summary>
    /// A simple MonoBehaviour to demonstrate the Quest System.
    /// Attach this to an empty GameObject in your scene.
    /// </summary>
    public class QuestSystemDemo : MonoBehaviour
    {
        [Header("References")]
        public QuestGiver questGiver;
        public Enemy goblinEnemyPrefab;
        public ItemSO potionItem;

        [Header("Spawn Points")]
        public Transform enemySpawnPoint;

        private Enemy spawnedGoblin;

        void Start()
        {
            if (QuestManager.Instance == null)
            {
                new GameObject("QuestManager").AddComponent<QuestManager>();
            }
            if (PlayerInventory.Instance == null)
            {
                new GameObject("PlayerInventory").AddComponent<PlayerInventory>();
            }
            if (Player.Instance == null)
            {
                new GameObject("Player").AddComponent<Player>();
            }

            // Ensure our specific quest giver is in the scene or create one
            if (questGiver == null)
            {
                Debug.LogError("QuestGiver not assigned! Please create an empty GameObject, add QuestGiver component and assign QuestSO assets to it, then drag it here.");
                return;
            }

            Debug.Log("QuestSystemDemo Started. Press keys for actions:");
            Debug.Log("P: Player interacts with Quest Giver");
            Debug.Log("K: Player attacks current target (simulates enemy kill)");
            Debug.Log("C: Player collects 1 Potion");
            Debug.Log("Q: Debug Log all quests");
            Debug.Log("I: Debug Log Player Inventory");
            Debug.Log("S: Spawn a Goblin (if goblinEnemyPrefab is assigned)");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) // Interact with Quest Giver
            {
                questGiver?.Interact();
            }
            if (Input.GetKeyDown(KeyCode.K)) // Kill enemy
            {
                if (Player.Instance != null)
                {
                    if (spawnedGoblin != null)
                    {
                        Player.Instance.CurrentTarget = spawnedGoblin.transform;
                        Player.Instance.PerformAttack();
                    }
                    else
                    {
                        Debug.Log("No goblin to attack. Press 'S' to spawn one.");
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.C)) // Collect item
            {
                if (Player.Instance != null && potionItem != null)
                {
                    Player.Instance.CollectItem(potionItem);
                }
                else
                {
                    Debug.LogWarning("Potion Item or Player not set for collection.");
                }
            }
            if (Input.GetKeyDown(KeyCode.Q)) // Debug quests
            {
                QuestManager.Instance?.DebugLogAllQuests();
            }
            if (Input.GetKeyDown(KeyCode.I)) // Debug inventory
            {
                PlayerInventory.Instance?.DebugLogInventory();
            }
            if (Input.GetKeyDown(KeyCode.S)) // Spawn Enemy
            {
                if (goblinEnemyPrefab != null && enemySpawnPoint != null)
                {
                    if (spawnedGoblin == null)
                    {
                        spawnedGoblin = Instantiate(goblinEnemyPrefab, enemySpawnPoint.position, Quaternion.identity);
                        Debug.Log("Goblin spawned!");
                    }
                    else
                    {
                        Debug.Log("Goblin already exists. Kill it first!");
                    }
                }
                else
                {
                    Debug.LogWarning("Goblin Enemy Prefab or Spawn Point not assigned!");
                }
            }
        }
    }
}
```

---

### **How to Use This in Unity:**

1.  **Create a C# Script:** Create a new C# script named `QuestSystemPattern.cs` in your Unity project (e.g., in an `Assets/Scripts/QuestSystem` folder).
2.  **Paste the Code:** Copy and paste *all* the code provided above into `QuestSystemPattern.cs`, replacing any default content.
3.  **Create Folders (Optional but Recommended):**
    *   `Assets/Resources/Quests`
    *   `Assets/Resources/Objectives`
    *   `Assets/Resources/Items`
4.  **Create Item Assets:**
    *   Right-click in the `Assets/Resources/Items` folder -> `Create` -> `QuestSystem` -> `Item`.
    *   Name it `PotionItem`. You can optionally assign an icon.
5.  **Create Objective Assets:**
    *   Right-click in `Assets/Resources/Objectives` -> `Create` -> `QuestSystem` -> `Objectives` -> `Kill`.
        *   Name it `KillGoblinObjective`.
        *   Set `ObjectiveText`: "Slay 2 Goblins".
        *   Set `TargetEnemyType`: "Goblin".
        *   Set `AmountToKill`: 2.
    *   Right-click in `Assets/Resources/Objectives` -> `Create` -> `QuestSystem` -> `Objectives` -> `Collect`.
        *   Name it `CollectPotionObjective`.
        *   Set `ObjectiveText`: "Collect 3 Potions".
        *   Drag `PotionItem` (from `Assets/Resources/Items`) into `ItemRequired`.
        *   Set `AmountRequired`: 3.
    *   Right-click in `Assets/Resources/Objectives` -> `Create` -> `QuestSystem` -> `Objectives` -> `Talk`.
        *   Name it `TalkToGiverObjective`.
        *   Set `ObjectiveText`: "Talk to the Quest Giver".
        *   Set `TargetNPC_ID`: "QuestGiver_001" (This ID must match the `QuestGiver` GameObject's `NPC_ID`).
6.  **Create Quest Asset:**
    *   Right-click in `Assets/Resources/Quests` -> `Create` -> `QuestSystem` -> `Quest`.
    *   Name it `TheFirstQuest`.
    *   Set `QuestName`: "The First Quest".
    *   Set `Description`: "Your adventure begins! Talk to the Quest Giver, kill some goblins, and collect potions."
    *   **Drag your objective assets** into the `Objectives` list: `TalkToGiverObjective`, `KillGoblinObjective`, `CollectPotionObjective`.
    *   Set `RewardExperience`: 100.
    *   Set `RewardGold`: 50.
    *   Add `PotionItem` to `RewardItems` and set its amount to 5.
7.  **Create Scene Objects:**
    *   **Main Game Manager:** Create an empty GameObject in your scene, name it `_GameManagers`. Add the `QuestSystemDemo` component to it.
    *   **Quest Giver:** Create another empty GameObject, name it `QuestGiverNPC`. Add the `QuestGiver` component to it.
        *   Set its `NPC_ID` to `QuestGiver_001`.
        *   Drag `TheFirstQuest` (from `Assets/Resources/Quests`) into `QuestToGive`.
        *   Drag `TheFirstQuest` into `QuestToClaim` (this means this NPC will also be where you claim rewards).
    *   **Enemy Prefab:** Create a 3D Cube (or any simple model), name it `Goblin_Prefab`. Add the `Enemy` component to it.
        *   Set its `EnemyType` to `Goblin`.
        *   Drag `Goblin_Prefab` into the `Goblin Enemy Prefab` slot on your `_GameManagers` (QuestSystemDemo component).
    *   **Spawn Point:** Create an empty GameObject, name it `EnemySpawnPoint`. Position it somewhere convenient in your scene.
        *   Drag `EnemySpawnPoint` into the `Enemy Spawn Point` slot on your `_GameManagers` (QuestSystemDemo component).
8.  **Link References:**
    *   On the `_GameManagers` GameObject (with `QuestSystemDemo`):
        *   Drag `QuestGiverNPC` into the `Quest Giver` slot.
        *   Drag `PotionItem` (from `Assets/Resources/Items`) into the `Potion Item` slot.
9.  **Run the Scene:** Press Play in the Unity editor.

---

### **Demonstration Steps in Play Mode:**

1.  **Initial State:**
    *   The `QuestSystemDemo` will initialize `QuestManager`, `PlayerInventory`, and `Player` (if they don't exist).
    *   `QuestManager` will be active.
    *   Press `Q` to debug quests: You'll see no active quests.
    *   Press `I` to debug inventory: You'll see 0 gold, 0 XP, and no items.

2.  **Accept Quest:**
    *   Press `P` (Interact with Quest Giver).
    *   The Quest Giver will accept `The First Quest`.
    *   Check the console: "Quest 'The First Quest' activated!"
    *   Press `Q`: You'll now see "The First Quest" under "Active Quests" with its three objectives. The "Talk to the Quest Giver" objective should already be completed (1/1) because you just interacted with him.

3.  **Advance Kill Objective:**
    *   Press `S` to spawn a Goblin.
    *   Press `K` twice to "kill" the goblin (each `K` reduces its health by 1, and it needs 2 health).
    *   Check the console: "Goblin_Prefab was killed!", "Kill objective 'Slay 2 Goblins' progress: 1/2", etc.
    *   Press `S` again to spawn another Goblin.
    *   Press `K` twice again to kill the second goblin.
    *   Check the console: "Kill objective 'Slay 2 Goblins' progress: 2/2", "Objective 'Slay 2 Goblins' completed!".
    *   Press `Q`: You'll see the Kill Objective is now completed.

4.  **Advance Collect Objective:**
    *   Press `C` three times to "collect" 3 potions.
    *   Check the console: "Collect objective 'Collect 3 Potions' progress: 1/3", etc.
    *   After the third `C`, you'll see "Objective 'Collect 3 Potions' completed!".
    *   Check the console: "Quest 'The First Quest' completed!". The `QuestManager` automatically detects all conditions are met.
    *   Press `Q`: "The First Quest" should now be under "Completed Quests (Awaiting Rewards)".

5.  **Claim Rewards:**
    *   Press `P` again (Interact with Quest Giver).
    *   The Quest Giver will now offer to claim rewards for `The First Quest`.
    *   Check the console: "Rewards for 'The First Quest' claimed!", "Added 100 XP.", "Added 50 Gold.", "Added 5x PotionItem.".
    *   Press `Q`: "The First Quest" should now be under "Rewarded Quests".
    *   Press `I`: Your inventory will show the new gold, XP, and potions.

This example provides a robust foundation for a full quest system, demonstrating core design patterns and Unity best practices.