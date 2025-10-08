// Unity Design Pattern Example: KickbackSystem
// This script demonstrates the KickbackSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "KickbackSystem" design pattern, while not a universally standardized GoF pattern, typically refers to a system where a core process or entity, upon completing an action or reaching a significant state, "kicks back" or publishes its results/notifications to various interested, decoupled subscribers. This is a practical application of the **Observer Pattern** (or Publish-Subscribe pattern) within a specific context where a central system orchestrates an action and then broadcasts its outcome.

### Key Components of a KickbackSystem:

1.  **The Kickback Provider (Subject):** This is the core system that performs an action. Once the action is complete (or a significant event occurs), it broadcasts a "kickback" notification. It does not directly manage or know about its subscribers.
2.  **The Kickback Mechanism:** This is how the provider communicates. In C#, this is often implemented using events and delegates (`Action<T>`).
3.  **The Kickback Receivers (Observers):** These are independent systems or components that are interested in the provider's notifications. They subscribe to the provider's kickback event and react accordingly when a notification is received.

### Benefits:

*   **Decoupling:** The provider system doesn't need to know anything about the concrete receiver systems. It just broadcasts a generic event. This makes the system highly modular and easier to maintain.
*   **Extensibility:** New receiver systems can be added or removed without modifying the core provider system. You just create a new subscriber and register it.
*   **Flexibility:** Multiple receivers can react to the same kickback event in different ways, allowing for complex interactions from a single source.

---

## C# Unity Example: Quest System Kickback

This example demonstrates a `QuestSystem` as the **Kickback Provider**. When a quest is completed, it "kicks back" a `QuestCompleted` event. Various **Kickback Receivers** (`XPSystem`, `InventorySystem`, `AchievementSystem`, `UIManager`) subscribe to this event to perform their respective actions (award XP, give items, check achievements, display UI) without any direct dependencies on each other or the `QuestSystem` itself beyond the event subscription.

### How to Use This Script in Unity:

1.  **Create a new C# script** in your Unity project, name it `KickbackSystem` (or any name you prefer).
2.  **Copy and paste** the entire code below into the new script.
3.  **Create an Empty GameObject** in your scene (e.g., named "GameManagers").
4.  **Attach the following components** to the "GameManagers" GameObject:
    *   `QuestSystem`
    *   `XPSystem`
    *   `InventorySystem`
    *   `AchievementSystem`
    *   `UIManager`
5.  **Create another Empty GameObject** (e.g., named "TestTrigger").
6.  **Attach the `TestQuestTrigger` component** to the "TestTrigger" GameObject.
7.  **Run the scene** and press `1`, `2`, or `3` on your keyboard to complete quests and observe the kickback system in action in the Unity Console.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for LINQ extension methods like FirstOrDefault

/// <summary>
/// This script demonstrates the 'KickbackSystem' design pattern in Unity.
/// It's essentially an application of the Observer/Publish-Subscribe pattern,
/// where a central 'provider' system performs an action and then 'kicks back'
/// a notification (via an event) to all interested, decoupled 'receiver' systems.
/// </summary>

// --- 1. Quest Data Structure ---
/// <summary>
/// Represents a single quest with basic properties.
/// This is the data that will be "kicked back" to receivers.
/// </summary>
[System.Serializable]
public class Quest
{
    public string Id;
    public string Name;
    public string Description;
    public int XPReward;
    public List<string> ItemRewards; // Example: list of item IDs or names
    public bool IsCompleted;

    public Quest(string id, string name, string description, int xpReward, List<string> itemRewards)
    {
        Id = id;
        Name = name;
        Description = description;
        XPReward = xpReward;
        ItemRewards = itemRewards ?? new List<string>();
        IsCompleted = false;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        // In a real game, you might save the completion state here
        // or trigger other internal quest-specific logic.
    }
}

// --- 2. QuestSystem: The Kickback Provider (Subject) ---
/// <summary>
/// The central 'QuestSystem' acts as the **Kickback Provider**.
/// It manages quests and, upon quest completion, "kicks back" a notification
/// (via an event) to all interested **Kickback Receivers**.
/// </summary>
public class QuestSystem : MonoBehaviour
{
    // A simple Singleton pattern to make the QuestSystem easily accessible globally.
    // In larger projects, consider a dedicated service locator or dependency injection.
    public static QuestSystem Instance { get; private set; }

    // --- The Core Kickback Mechanism ---
    /// <summary>
    /// This is the 'kickback' event. It's a C# event using the Action delegate.
    /// Any system interested in knowing when a quest is completed will subscribe to this event.
    /// When invoked, it passes the completed Quest object as an argument, providing
    /// the necessary data for receivers to react.
    /// </summary>
    public event Action<Quest> OnQuestCompleted;

    [Header("Quest Data")]
    // [SerializeField] allows you to see and edit the list in the Unity Inspector.
    [SerializeField] private List<Quest> availableQuests = new List<Quest>();

    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple QuestSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Make the QuestSystem persist across scene loads.
        // Remove if you want a new QuestSystem instance per scene.
        DontDestroyOnLoad(gameObject);
        Debug.Log("QuestSystem initialized and ready to provide kickbacks.");
    }

    private void Start()
    {
        // Initialize with some dummy quests for demonstration if none are set in Inspector.
        if (availableQuests.Count == 0)
        {
            Debug.Log("QuestSystem: Initializing dummy quests.");
            availableQuests.Add(new Quest(
                "Q001", "The First Steps", "Find 3 shiny pebbles in the forest.",
                100, new List<string> { "Small Health Potion", "5 Gold Coins" }
            ));
            availableQuests.Add(new Quest(
                "Q002", "Mushroom Gatherer", "Collect 5 red mushrooms.",
                150, new List<string> { "Medium Mana Potion", "10 Gold Coins" }
            ));
            availableQuests.Add(new Quest(
                "Q003", "The Lost Artifact", "Retrieve the ancient relic from the goblin caves.",
                500, new List<string> { "Rare Sword", "50 Gold Coins" }
            ));
        }
    }

    /// <summary>
    /// Attempts to complete a quest by its ID.
    /// If the quest is found and not already completed, it marks it complete
    /// and, crucially, triggers the 'kickback' event to all subscribers.
    /// </summary>
    /// <param name="questId">The unique ID of the quest to complete.</param>
    public void CompleteQuest(string questId)
    {
        Quest questToComplete = availableQuests.FirstOrDefault(q => q.Id == questId);

        if (questToComplete == null)
        {
            Debug.LogWarning($"QuestSystem: Quest with ID '{questId}' not found. No kickback.");
            return;
        }

        if (questToComplete.IsCompleted)
        {
            Debug.Log($"QuestSystem: Quest '{questToComplete.Name}' is already completed. No kickback initiated.");
            return;
        }

        questToComplete.MarkCompleted();

        // --- THE KICKBACK HAPPENS HERE! ---
        // This line invokes the 'OnQuestCompleted' event, notifying all subscribers.
        // The '?' (null-conditional operator) ensures that the event is only invoked
        // if there are actual subscribers, preventing a NullReferenceException if no one is listening.
        OnQuestCompleted?.Invoke(questToComplete);

        Debug.Log($"<color=green>QuestSystem:</color> Quest '{questToComplete.Name}' (ID: {questId}) completed and **KICKBACK EVENT FIRED!**");
    }

    /// <summary>
    /// Provides a read-only list of all quests managed by the system.
    /// </summary>
    public IReadOnlyList<Quest> GetAvailableQuests()
    {
        return availableQuests.AsReadOnly();
    }
}

// --- 3. XPSystem: A Kickback Receiver (Observer) ---
/// <summary>
/// A system responsible for managing player experience points.
/// It acts as a **Kickback Receiver** by subscribing to the QuestSystem's
/// kickback event to award XP upon quest completion.
/// </summary>
public class XPSystem : MonoBehaviour
{
    private int currentXP = 0;
    private int level = 1;
    [SerializeField] private int xpToNextLevel = 1000;

    void OnEnable()
    {
        // --- Subscriber Registration ---
        // In OnEnable, we subscribe to the QuestSystem's OnQuestCompleted event.
        // This is where the XPSystem registers its interest in receiving the 'kickback'.
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted += HandleQuestCompleted;
            Debug.Log($"XPSystem: Subscribed to QuestSystem.OnQuestCompleted.");
        }
        else
        {
            Debug.LogError("XPSystem: QuestSystem.Instance is null. Cannot subscribe. Make sure QuestSystem initializes first.");
        }
    }

    void OnDisable()
    {
        // --- Subscriber Deregistration ---
        // In OnDisable, we always unsubscribe to prevent memory leaks,
        // especially important if this GameObject is destroyed or disabled.
        // Failing to unsubscribe can lead to "dangling references."
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted -= HandleQuestCompleted;
            Debug.Log($"XPSystem: Unsubscribed from QuestSystem.OnQuestCompleted.");
        }
    }

    /// <summary>
    /// This method is called by the QuestSystem when it 'kicks back' a completed quest notification.
    /// It processes the information and awards XP to the player.
    /// This method is effectively the 'reaction' to the kickback.
    /// </summary>
    /// <param name="completedQuest">The quest that was just completed, passed by the provider.</param>
    private void HandleQuestCompleted(Quest completedQuest)
    {
        currentXP += completedQuest.XPReward;
        Debug.Log($"<color=blue>XPSystem:</color> Awarded {completedQuest.XPReward} XP for '{completedQuest.Name}'. Current XP: {currentXP}.");
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        if (currentXP >= xpToNextLevel)
        {
            level++;
            currentXP -= xpToNextLevel; // Carry over excess XP
            xpToNextLevel = (int)(xpToNextLevel * 1.2f); // Example: increase XP needed for next level
            Debug.Log($"<color=blue>XPSystem:</color> Leveled up to Level {level}! XP to next level: {xpToNextLevel}.");
        }
    }
}

// --- 4. InventorySystem: A Kickback Receiver (Observer) ---
/// <summary>
/// A system responsible for managing player inventory.
/// It acts as a **Kickback Receiver** by subscribing to the QuestSystem's
/// kickback event to add reward items upon quest completion.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    private Dictionary<string, int> inventory = new Dictionary<string, int>();

    void OnEnable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted += HandleQuestCompleted;
            Debug.Log($"InventorySystem: Subscribed to QuestSystem.OnQuestCompleted.");
        }
        else
        {
            Debug.LogError("InventorySystem: QuestSystem.Instance is null. Cannot subscribe.");
        }
    }

    void OnDisable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted -= HandleQuestCompleted;
            Debug.Log($"InventorySystem: Unsubscribed from QuestSystem.OnQuestCompleted.");
        }
    }

    /// <summary>
    /// This method is called by the QuestSystem when it 'kicks back' a completed quest notification.
    /// It processes the information and adds reward items to the inventory.
    /// </summary>
    /// <param name="completedQuest">The quest that was just completed.</param>
    private void HandleQuestCompleted(Quest completedQuest)
    {
        Debug.Log($"<color=magenta>InventorySystem:</color> Processing rewards for '{completedQuest.Name}'.");
        foreach (string item in completedQuest.ItemRewards)
        {
            AddItemToInventory(item);
        }
        DisplayInventory();
    }

    private void AddItemToInventory(string itemName, int quantity = 1)
    {
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] += quantity;
        }
        else
        {
            inventory.Add(itemName, quantity);
        }
        // In a real game, you might use a proper ItemData object instead of just string names.
    }

    private void DisplayInventory()
    {
        string inventoryContent = "Current Inventory: ";
        if (inventory.Count == 0)
        {
            inventoryContent += "Empty.";
        }
        else
        {
            foreach (var item in inventory)
            {
                inventoryContent += $"{item.Key} ({item.Value}), ";
            }
            inventoryContent = inventoryContent.TrimEnd(' ', ','); // Remove trailing comma and space
        }
        Debug.Log($"<color=magenta>{inventoryContent}</color>");
    }
}

// --- 5. AchievementSystem: A Kickback Receiver (Observer) ---
/// <summary>
/// A system for tracking and unlocking achievements.
/// It acts as a **Kickback Receiver** by subscribing to the QuestSystem's
/// kickback event to check for quest-related achievements.
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    private HashSet<string> unlockedAchievements = new HashSet<string>();
    private int completedQuestsCount = 0;

    void OnEnable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted += HandleQuestCompleted;
            Debug.Log($"AchievementSystem: Subscribed to QuestSystem.OnQuestCompleted.");
        }
        else
        {
            Debug.LogError("AchievementSystem: QuestSystem.Instance is null. Cannot subscribe.");
        }
    }

    void OnDisable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted -= HandleQuestCompleted;
            Debug.Log($"AchievementSystem: Unsubscribed from QuestSystem.OnQuestCompleted.");
        }
    }

    /// <summary>
    /// This method is called by the QuestSystem when it 'kicks back' a completed quest notification.
    /// It processes the information and checks if any achievements should be unlocked.
    /// </summary>
    /// <param name="completedQuest">The quest that was just completed.</param>
    private void HandleQuestCompleted(Quest completedQuest)
    {
        completedQuestsCount++;
        Debug.Log($"<color=orange>AchievementSystem:</color> Quest '{completedQuest.Name}' completed. Total quests completed: {completedQuestsCount}.");

        // Example achievement: "First Quest!"
        if (completedQuestsCount == 1)
        {
            UnlockAchievement("ACH_FIRST_QUEST", "First Quest!", "Complete your very first quest.");
        }

        // Example achievement: "Quest Master" (e.g., complete 3 quests)
        if (completedQuestsCount == 3)
        {
            UnlockAchievement("ACH_QUEST_MASTER", "Quest Master", "Complete 3 quests.");
        }

        // Example achievement: based on specific quest ID
        if (completedQuest.Id == "Q003")
        {
             UnlockAchievement("ACH_LOST_ARTIFACT", "Artifact Recovered", "Recovered the lost artifact from the goblin caves.");
        }
    }

    private void UnlockAchievement(string id, string name, string description)
    {
        if (!unlockedAchievements.Contains(id))
        {
            unlockedAchievements.Add(id);
            Debug.Log($"<color=green>Achievement Unlocked!</color> '{name}': {description}");
            // In a real game, you'd likely update UI, play sound, save achievement state, etc.
        }
        else
        {
            Debug.Log($"<color=grey>AchievementSystem:</color> Achievement '{name}' already unlocked.");
        }
    }
}

// --- 6. UIManager: A Kickback Receiver (Observer) ---
/// <summary>
/// A simple UI manager (for console logging in this example) that displays messages.
/// It acts as a **Kickback Receiver** by subscribing to the QuestSystem's
/// kickback event to display quest completion notifications.
/// </summary>
public class UIManager : MonoBehaviour
{
    void OnEnable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted += HandleQuestCompleted;
            Debug.Log($"UIManager: Subscribed to QuestSystem.OnQuestCompleted.");
        }
        else
        {
            Debug.LogError("UIManager: QuestSystem.Instance is null. Cannot subscribe.");
        }
    }

    void OnDisable()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestCompleted -= HandleQuestCompleted;
            Debug.Log($"UIManager: Unsubscribed from QuestSystem.OnQuestCompleted.");
        }
    }

    /// <summary>
    /// This method is called by the QuestSystem when it 'kicks back' a completed quest notification.
    /// It processes the information and displays a UI message (to console in this demo).
    /// </summary>
    /// <param name="completedQuest">The quest that was just completed.</param>
    private void HandleQuestCompleted(Quest completedQuest)
    {
        Debug.Log($"<color=cyan>UI Notification:</color> Quest Completed! <b>'{completedQuest.Name}'</b>! Great job!");
        // In a real game, this would update a Canvas UI element, play an animation,
        // show a popup, or add an entry to a quest log.
    }
}


// --- Example Usage / Test Trigger ---
/// <summary>
/// A simple MonoBehaviour to demonstrate triggering quest completions.
/// Attach this to any GameObject in your scene to test the KickbackSystem.
/// </summary>
public class TestQuestTrigger : MonoBehaviour
{
    void Update()
    {
        // Check if QuestSystem is available before trying to complete quests.
        if (QuestSystem.Instance == null)
        {
            Debug.LogError("TestQuestTrigger: QuestSystem.Instance is null. Make sure QuestSystem is present and initialized in the scene.");
            return;
        }

        // Press '1' to complete the first quest (Q001)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("<color=yellow>--- Test Trigger: Attempting to complete Quest Q001 ---</color>");
            QuestSystem.Instance.CompleteQuest("Q001");
        }
        // Press '2' to complete the second quest (Q002)
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("<color=yellow>--- Test Trigger: Attempting to complete Quest Q002 ---</color>");
            QuestSystem.Instance.CompleteQuest("Q002");
        }
        // Press '3' to complete the third quest (Q003)
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("<color=yellow>--- Test Trigger: Attempting to complete Quest Q003 ---</color>");
            QuestSystem.Instance.CompleteQuest("Q003");
        }
        // Press 'R' to attempt to complete a non-existent quest (demonstrates error handling)
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("<color=yellow>--- Test Trigger: Attempting to complete a non-existent quest ---</color>");
            QuestSystem.Instance.CompleteQuest("NON_EXISTENT_QUEST");
        }
    }
}
```