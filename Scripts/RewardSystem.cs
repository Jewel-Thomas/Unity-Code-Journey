// Unity Design Pattern Example: RewardSystem
// This script demonstrates the RewardSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The RewardSystem design pattern is crucial for managing and distributing various in-game rewards like currency, items, experience, or special bonuses. It centralizes the logic for how rewards are defined, processed, and given to the player, making the system extensible and easy to maintain.

This example provides a complete, practical C# Unity script that implements the RewardSystem pattern. It includes:

1.  **`RewardType` Enum**: Defines different categories of rewards.
2.  **`Reward` Struct**: Represents a single reward instance (type, amount, item ID).
3.  **`RewardDefinition` ScriptableObject**: Allows you to create predefined bundles of rewards in the Unity Editor (e.g., "Daily Bonus," "Quest Complete Reward").
4.  **`IRewardProcessor` Interface**: Defines how any type of reward should be processed.
5.  **Concrete Reward Processors**: Implement `IRewardProcessor` for specific reward types (e.g., `CoinRewardProcessor`, `ItemRewardProcessor`), handling the actual logic of adding rewards to the player's state.
6.  **`PlayerData` MonoBehaviour**: A mock class representing the player's inventory and stats, which the reward processors interact with.
7.  **`RewardSystem` MonoBehaviour**: The central hub that manages all `IRewardProcessor` instances and provides a public method (`GrantReward`) to distribute rewards based on a `RewardDefinition`. It's implemented as a singleton for easy access.

**To use this script in your Unity project:**

1.  Create a new C# script named `RewardSystem.cs` and copy all the content below into it.
2.  Create an empty GameObject in your scene named `Reward System`.
3.  Attach the `RewardSystem.cs` script to the `Reward System` GameObject.
4.  Create another empty GameObject in your scene named `Player Data`.
5.  Attach the `RewardSystem.PlayerData.cs` script (which is part of the `RewardSystem.cs` file below) to the `Player Data` GameObject.
6.  In the Unity Editor, right-click in your Project window -> Create -> Reward System -> Reward Definition. Create a few different `RewardDefinition` ScriptableObjects (e.g., "DailyLoginReward", "LevelUpReward"). Fill them with various `RewardType`s, amounts, and item IDs.
7.  You can now call `RewardSystem.Instance.GrantReward(yourRewardDefinition)` from any other script in your game to give rewards to the player.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

// This single file contains all necessary components for the RewardSystem pattern.
// In a larger project, interfaces, structs, enums, and individual processor classes
// would typically reside in separate, appropriately named files for better organization.

namespace DesignPatterns.RewardSystem
{
    /// <summary>
    /// Represents different categories of rewards.
    /// This enum makes the system extensible for new reward types.
    /// </summary>
    public enum RewardType
    {
        Coin,
        Experience,
        Item,
        PremiumCurrency,
        Booster, // Example of adding a new reward type
        AchievementPoints
    }

    /// <summary>
    /// A simple struct to define a single reward instance.
    /// Contains the type, a generic amount, and an optional ItemID for item rewards.
    /// </summary>
    [System.Serializable] // Make it serializable so it can be used in the Inspector for ScriptableObjects.
    public struct Reward
    {
        public RewardType Type;
        public int Amount; // Used for Coin, Experience, PremiumCurrency, etc.
        public string ItemID; // Used for Item rewards (e.g., "Sword_01", "Potion_Health")

        public Reward(RewardType type, int amount, string itemID = null)
        {
            Type = type;
            Amount = amount;
            ItemID = itemID;
        }

        public override string ToString()
        {
            if (Type == RewardType.Item)
            {
                return $"{Amount}x {ItemID} ({Type})";
            }
            return $"{Amount} {Type}";
        }
    }

    /// <summary>
    /// ScriptableObject for defining bundles of rewards.
    /// This allows designers to create and configure reward packages directly in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRewardDefinition", menuName = "Reward System/Reward Definition")]
    public class RewardDefinition : ScriptableObject
    {
        public string RewardName = "New Reward Bundle";
        [Tooltip("List of individual rewards included in this bundle.")]
        public List<Reward> Rewards = new List<Reward>();

        public void LogRewards()
        {
            Debug.Log($"<color=cyan>--- Granting Reward: {RewardName} ---</color>");
            foreach (var reward in Rewards)
            {
                Debug.Log($"  - {reward}");
            }
            Debug.Log($"<color=cyan>-------------------------------</color>");
        }
    }

    /// <summary>
    /// Interface for any class that can process a specific type of reward.
    /// This is the core of the RewardSystem pattern's extensibility.
    /// New reward types only require a new processor implementing this interface.
    /// </summary>
    public interface IRewardProcessor
    {
        bool CanProcess(RewardType type);
        void ProcessReward(Reward reward);
    }

    /// <summary>
    /// A mock PlayerData MonoBehaviour to simulate player state (inventory, stats).
    /// Reward processors will interact with this class to update the player's game state.
    /// In a real game, this would likely be part of a more comprehensive PlayerManager or PlayerInventory system.
    /// </summary>
    public class PlayerData : MonoBehaviour
    {
        [Header("Player Stats")]
        public int Coins = 0;
        public int Experience = 0;
        public int PremiumCurrency = 0;
        public List<string> Inventory = new List<string>();

        // Event for UI updates or other systems to react to changes
        public event Action OnPlayerStatsChanged;

        void Awake()
        {
            Debug.Log("<color=green>PlayerData Initialized.</color>");
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Debug.Log($"<color=green>Added {amount} Coins. Total: {Coins}</color>");
            OnPlayerStatsChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
            Debug.Log($"<color=green>Added {amount} XP. Total: {Experience}</color>");
            OnPlayerStatsChanged?.Invoke();
        }

        public void AddItem(string itemID, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemID) || quantity <= 0) return;
            for (int i = 0; i < quantity; i++)
            {
                Inventory.Add(itemID);
            }
            Debug.Log($"<color=green>Added {quantity}x {itemID} to Inventory.</color> Inventory Size: {Inventory.Count}");
            OnPlayerStatsChanged?.Invoke();
        }

        public void AddPremiumCurrency(int amount)
        {
            if (amount <= 0) return;
            PremiumCurrency += amount;
            Debug.Log($"<color=green>Added {amount} Premium Currency. Total: {PremiumCurrency}</color>");
            OnPlayerStatsChanged?.Invoke();
        }

        public void AddBooster(string boosterID, int quantity = 1)
        {
            if (string.IsNullOrEmpty(boosterID) || quantity <= 0) return;
            // In a real game, you might have a dedicated BoosterManager or a dictionary for boosters
            for (int i = 0; i < quantity; i++)
            {
                Inventory.Add($"Booster_{boosterID}"); // Just add to generic inventory for this example
            }
            Debug.Log($"<color=green>Added {quantity}x {boosterID} Booster.</color>");
            OnPlayerStatsChanged?.Invoke();
        }
        
        public void AddAchievementPoints(int amount)
        {
            if (amount <= 0) return;
            // In a real game, you would interact with an AchievementManager
            Debug.Log($"<color=green>Added {amount} Achievement Points. (Not directly stored in PlayerData in this example, usually an external system)</color>");
            OnPlayerStatsChanged?.Invoke();
        }


        // Optional: A method to display current stats (e.g., for debugging UI)
        public string GetPlayerStatsSummary()
        {
            return $"Coins: {Coins}\nXP: {Experience}\nPremium: {PremiumCurrency}\nItems: {string.Join(", ", Inventory)}";
        }
    }

    /// <summary>
    /// Concrete processor for Coin rewards.
    /// </summary>
    public class CoinRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData;

        public CoinRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.Coin;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddCoins(reward.Amount);
        }
    }

    /// <summary>
    /// Concrete processor for Experience rewards.
    /// </summary>
    public class ExperienceRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData;

        public ExperienceRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.Experience;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddExperience(reward.Amount);
        }
    }

    /// <summary>
    /// Concrete processor for Item rewards.
    /// </summary>
    public class ItemRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData;

        public ItemRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.Item;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddItem(reward.ItemID, reward.Amount); // Amount here means quantity of the item
        }
    }

    /// <summary>
    /// Concrete processor for Premium Currency rewards.
    /// </summary>
    public class PremiumCurrencyRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData;

        public PremiumCurrencyRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.PremiumCurrency;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddPremiumCurrency(reward.Amount);
        }
    }

    /// <summary>
    /// Example of a new concrete processor for Booster rewards.
    /// Demonstrates the extensibility of the system.
    /// </summary>
    public class BoosterRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData;

        public BoosterRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.Booster;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddBooster(reward.ItemID, reward.Amount); // ItemID for booster type, Amount for quantity
        }
    }
    
    /// <summary>
    /// Example of a new concrete processor for Achievement Points.
    /// This might interact with an AchievementManager rather than directly PlayerData.
    /// </summary>
    public class AchievementPointsRewardProcessor : IRewardProcessor
    {
        private PlayerData _playerData; // Still passed for consistency, but might not be directly used for storage.

        public AchievementPointsRewardProcessor(PlayerData playerData)
        {
            _playerData = playerData;
        }

        public bool CanProcess(RewardType type) => type == RewardType.AchievementPoints;

        public void ProcessReward(Reward reward)
        {
            _playerData.AddAchievementPoints(reward.Amount); // This will log the action.
        }
    }


    /// <summary>
    /// The central RewardSystem MonoBehaviour.
    /// Manages a collection of IRewardProcessor instances and provides a public
    /// method to grant rewards based on a RewardDefinition.
    /// Implemented as a basic singleton for easy global access.
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        public static RewardSystem Instance { get; private set; }

        private List<IRewardProcessor> _processors = new List<IRewardProcessor>();
        private PlayerData _playerData;

        void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple RewardSystem instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep the RewardSystem alive across scenes.

            InitializeRewardSystem();
        }

        /// <summary>
        /// Initializes the RewardSystem by finding PlayerData and registering all concrete reward processors.
        /// </summary>
        private void InitializeRewardSystem()
        {
            // Find the PlayerData instance in the scene.
            // In a real game, this might be injected or retrieved from a GameManager.
            _playerData = FindObjectOfType<PlayerData>();
            if (_playerData == null)
            {
                Debug.LogError("RewardSystem requires a PlayerData instance in the scene! Please add one.");
                return;
            }

            // Register all concrete reward processors.
            // When a new reward type is added, a new processor is created and registered here.
            RegisterProcessor(new CoinRewardProcessor(_playerData));
            RegisterProcessor(new ExperienceRewardProcessor(_playerData));
            RegisterProcessor(new ItemRewardProcessor(_playerData));
            RegisterProcessor(new PremiumCurrencyRewardProcessor(_playerData));
            RegisterProcessor(new BoosterRewardProcessor(_playerData)); // Register the new booster processor
            RegisterProcessor(new AchievementPointsRewardProcessor(_playerData)); // Register achievement points processor

            Debug.Log($"<color=blue>RewardSystem Initialized with {_processors.Count} processors.</color>");
        }

        /// <summary>
        /// Registers an IRewardProcessor with the system.
        /// </summary>
        /// <param name="processor">The processor to register.</param>
        public void RegisterProcessor(IRewardProcessor processor)
        {
            _processors.Add(processor);
        }

        /// <summary>
        /// The main method to grant a bundle of rewards defined by a RewardDefinition ScriptableObject.
        /// </summary>
        /// <param name="definition">The RewardDefinition containing the rewards to grant.</param>
        public void GrantReward(RewardDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("Attempted to grant a null RewardDefinition.");
                return;
            }

            if (_playerData == null)
            {
                Debug.LogError("RewardSystem cannot grant rewards: PlayerData is not found.");
                return;
            }

            definition.LogRewards(); // Log the definition for clarity

            foreach (var reward in definition.Rewards)
            {
                bool processed = false;
                foreach (var processor in _processors)
                {
                    if (processor.CanProcess(reward.Type))
                    {
                        processor.ProcessReward(reward);
                        processed = true;
                        break; // Found the correct processor, move to the next reward
                    }
                }

                if (!processed)
                {
                    Debug.LogWarning($"No processor found for reward type: {reward.Type} (Reward: {reward})");
                }
            }
        }
    }
}

/*
 * ====================================================================================================
 * EXAMPLE USAGE: How to trigger rewards from other scripts (e.g., a button, quest completion, etc.)
 * ====================================================================================================
 */

// EXAMPLE SCRIPT: This would be a separate MonoBehaviour in your project.
// Attach this to a GameObject and assign your RewardDefinition ScriptableObjects in the Inspector.

/*
using UnityEngine;
using DesignPatterns.RewardSystem; // Make sure to include the namespace

public class RewardTriggerExample : MonoBehaviour
{
    [Header("Reward Definitions (assign in Inspector)")]
    public RewardDefinition dailyLoginReward;
    public RewardDefinition questCompletionReward;
    public RewardDefinition levelUpReward;

    void Start()
    {
        // Subscribe to PlayerData changes to log current stats
        if (RewardSystem.Instance != null && RewardSystem.Instance.GetComponent<PlayerData>() != null)
        {
            RewardSystem.Instance.GetComponent<PlayerData>().OnPlayerStatsChanged += LogCurrentPlayerStats;
            LogCurrentPlayerStats(); // Log initial stats
        }
        else
        {
            Debug.LogError("RewardSystem or PlayerData not found on Start.");
        }
    }

    void OnDestroy()
    {
        if (RewardSystem.Instance != null && RewardSystem.Instance.GetComponent<PlayerData>() != null)
        {
            RewardSystem.Instance.GetComponent<PlayerData>().OnPlayerStatsChanged -= LogCurrentPlayerStats;
        }
    }

    void LogCurrentPlayerStats()
    {
        if (RewardSystem.Instance != null && RewardSystem.Instance.GetComponent<PlayerData>() != null)
        {
            Debug.Log($"<color=magenta>--- Current Player Stats ---</color>\n{RewardSystem.Instance.GetComponent<PlayerData>().GetPlayerStatsSummary()}");
            Debug.Log($"<color=magenta>--------------------------</color>");
        }
    }

    // Call this method when a daily login event occurs (e.g., from a UI button or timer)
    public void GrantDailyLoginBonus()
    {
        if (dailyLoginReward != null && RewardSystem.Instance != null)
        {
            Debug.Log("Triggering Daily Login Bonus!");
            RewardSystem.Instance.GrantReward(dailyLoginReward);
        }
        else
        {
            Debug.LogError("Daily Login Reward Definition or RewardSystem not assigned/found.");
        }
    }

    // Call this method when a quest is completed
    public void GrantQuestCompletionReward()
    {
        if (questCompletionReward != null && RewardSystem.Instance != null)
        {
            Debug.Log("Triggering Quest Completion Reward!");
            RewardSystem.Instance.GrantReward(questCompletionReward);
        }
        else
        {
            Debug.LogError("Quest Completion Reward Definition or RewardSystem not assigned/found.");
        }
    }

    // Call this method when the player levels up
    public void GrantLevelUpReward()
    {
        if (levelUpReward != null && RewardSystem.Instance != null)
        {
            Debug.Log("Triggering Level Up Reward!");
            RewardSystem.Instance.GrantReward(levelUpReward);
        }
        else
        {
            Debug.LogError("Level Up Reward Definition or RewardSystem not assigned/found.");
        }
    }

    // Example of a simple UI to trigger rewards (create a UI Canvas, then Buttons)
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Label("Reward System Controls:");

        if (GUILayout.Button("Grant Daily Login"))
        {
            GrantDailyLoginBonus();
        }
        if (GUILayout.Button("Grant Quest Reward"))
        {
            GrantQuestCompletionReward();
        }
        if (GUILayout.Button("Grant Level Up"))
        {
            GrantLevelUpReward();
        }
        GUILayout.EndArea();
    }
}
*/
```