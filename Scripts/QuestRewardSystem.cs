// Unity Design Pattern Example: QuestRewardSystem
// This script demonstrates the QuestRewardSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **QuestRewardSystem** design pattern in Unity using C#. The core idea is to decouple the *definition* of rewards from the *logic* that applies them to the player. This makes the system highly flexible, extensible, and easy to manage via ScriptableObjects in the Unity Editor.

**Key Components of the Pattern:**

1.  **`PlayerManager`**: The recipient of the rewards. It manages player stats (XP, Gold) and inventory.
2.  **`RewardSO` (Abstract ScriptableObject)**: The base class for all reward types. It defines the `ApplyReward` method, which concrete reward types must implement.
3.  **Concrete Reward ScriptableObjects**: Classes like `ExperienceRewardSO`, `CurrencyRewardSO`, and `ItemRewardSO` that inherit from `RewardSO`. Each defines a specific reward and how it's applied to the `PlayerManager`.
4.  **`QuestDefinitionSO` (ScriptableObject)**: Defines a quest. It holds a list of `RewardSO` assets. When a quest is completed, it iterates through this list and tells each `RewardSO` to apply itself.
5.  **`QuestManager` (MonoBehaviour)**: Simulates quest completion. It orchestrates the process, typically by calling a method on a `QuestDefinitionSO` to grant its rewards.

---

### **1. `PlayerManager.cs`**
This script manages the player's core attributes like experience, gold, and inventory. It's the central point where all rewards are ultimately applied.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents the player in the game, managing their stats and inventory.
/// This acts as the recipient for all rewards, providing methods for
/// adding experience, gold, and items.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField, Tooltip("Current experience points of the player.")]
    private int _currentExperience = 0;
    [SerializeField, Tooltip("Current in-game currency (gold) of the player.")]
    private int _currentGold = 0;

    [Header("Player Inventory")]
    [SerializeField, Tooltip("List of items currently in the player's inventory.")]
    private List<InventoryItem> _inventory = new List<InventoryItem>();

    // Public properties to access player stats
    public int CurrentExperience => _currentExperience;
    public int CurrentGold => _currentGold;
    public IReadOnlyList<InventoryItem> Inventory => _inventory; // Read-only access to inventory

    void Awake()
    {
        Debug.Log("PlayerManager Initialized.");
    }

    /// <summary>
    /// Adds experience to the player.
    /// </summary>
    /// <param name="amount">The amount of experience to add. Must be positive.</param>
    public void AddExperience(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to add negative experience. Please use positive values.", this);
            return;
        }
        _currentExperience += amount;
        Debug.Log($"Player gained {amount} XP. Total XP: {_currentExperience}", this);
        // TODO: In a real game, you would trigger UI updates, level-up checks, etc.
    }

    /// <summary>
    /// Adds gold to the player's currency.
    /// </summary>
    /// <param name="amount">The amount of gold to add. Must be positive.</param>
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to add negative gold. Please use positive values.", this);
            return;
        }
        _currentGold += amount;
        Debug.Log($"Player gained {amount} Gold. Total Gold: {_currentGold}", this);
        // TODO: In a real game, you would trigger UI updates.
    }

    /// <summary>
    /// Adds an item to the player's inventory. Handles stacking for existing items.
    /// </summary>
    /// <param name="item">The ItemSO to add.</param>
    /// <param name="quantity">The quantity of the item to add. Must be positive.</param>
    public void AddItem(ItemSO item, int quantity)
    {
        if (item == null)
        {
            Debug.LogError("Attempted to add a null item to inventory.", this);
            return;
        }
        if (quantity <= 0)
        {
            Debug.LogWarning($"Attempted to add 0 or negative quantity of item '{item.itemName}'. No item will be added.", this);
            return;
        }

        // Simple stacking logic: find existing item and add quantity, or add new entry
        bool itemFound = false;
        foreach (var invItem in _inventory)
        {
            if (invItem.item == item)
            {
                invItem.quantity += quantity;
                itemFound = true;
                break;
            }
        }

        if (!itemFound)
        {
            _inventory.Add(new InventoryItem { item = item, quantity = quantity });
        }

        Debug.Log($"Player received {quantity}x {item.itemName}. Current inventory size: {_inventory.Count}", this);
        // TODO: In a real game, you would trigger UI updates for the inventory.
    }

    /// <summary>
    /// A simple serializable struct to represent an item stack in the inventory.
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public ItemSO item;     // Reference to the item definition
        public int quantity;    // How many of this item the player has
    }
}
```

---

### **2. `ItemSO.cs`**
A basic ScriptableObject to define generic items that can be given as rewards.

```csharp
using UnityEngine;

/// <summary>
/// A simple ScriptableObject representing a generic item in the game.
/// This serves as a data definition for items that can be rewarded or used.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "QuestRewardSystem/Item")]
public class ItemSO : ScriptableObject
{
    [Tooltip("The display name of the item.")]
    public string itemName = "New Item";
    [TextArea(2, 5), Tooltip("A brief description of the item.")]
    public string description = "A generic item.";
    [Tooltip("Optional: Icon for UI display.")]
    public Sprite icon; 
    [Tooltip("Optional: A value for selling, etc.")]
    public int value = 1; 

    public enum ItemType { Generic, Weapon, Armor, Consumable, QuestItem }
    [Tooltip("The type of item, useful for categorization.")]
    public ItemType itemType = ItemType.Generic;

    // Optional: Ensure the asset name matches the item name for consistency
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(name))
        {
            itemName = name;
        }
    }
}
```

---

### **3. `RewardSO.cs`**
The abstract base class for all reward types. This is a ScriptableObject, which is key to making rewards data-driven and assignable in the Unity Editor.

```csharp
using UnityEngine;

/// <summary>
/// Abstract base class for all reward types in the QuestRewardSystem.
/// This is a ScriptableObject, allowing reward definitions to be created
/// as assets in the Unity Editor.
/// </summary>
/// <remarks>
/// The 'QuestRewardSystem' pattern decouples the *definition* of a reward
/// from its *application*. This abstract class defines the contract for any reward type,
/// ensuring that all rewards can be applied to a PlayerManager in a consistent way.
/// </remarks>
public abstract class RewardSO : ScriptableObject
{
    [Tooltip("A general description of what this reward grants, for editor clarity.")]
    public string rewardDescription = "Grants a reward.";

    /// <summary>
    /// Applies this specific reward to the given player.
    /// Each concrete reward type (e.g., Experience, Item, Gold) will implement this
    /// method differently to modify the player's state (experience, inventory, currency).
    /// </summary>
    /// <param name="player">The PlayerManager instance to apply the reward to.</param>
    public abstract void ApplyReward(PlayerManager player);
}
```

---

### **4. `ExperienceRewardSO.cs`**
A concrete implementation of `RewardSO` for granting experience points.

```csharp
using UnityEngine;

/// <summary>
/// Concrete reward type: Grants experience points to the player.
/// Inherits from RewardSO, making it a data-driven reward asset that can
/// be configured in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewExperienceReward", menuName = "QuestRewardSystem/Rewards/Experience Reward")]
public class ExperienceRewardSO : RewardSO
{
    [Tooltip("The amount of experience points to grant.")]
    [Min(0)] public int amount = 100;

    /// <summary>
    /// Applies the experience reward by calling the PlayerManager's AddExperience method.
    /// </summary>
    /// <param name="player">The PlayerManager instance to apply the reward to.</param>
    public override void ApplyReward(PlayerManager player)
    {
        if (player == null)
        {
            Debug.LogError($"Cannot apply Experience Reward: PlayerManager is null for '{this.name}'.", this);
            return;
        }
        
        player.AddExperience(amount);
        Debug.Log($"Applied Experience Reward: {amount} XP to {player.name}. (Via {this.name})", this);
    }
}
```

---

### **5. `CurrencyRewardSO.cs`**
A concrete implementation of `RewardSO` for granting in-game currency (gold).

```csharp
using UnityEngine;

/// <summary>
/// Concrete reward type: Grants in-game currency (gold) to the player.
/// Inherits from RewardSO, making it a data-driven reward asset that can
/// be configured in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewCurrencyReward", menuName = "QuestRewardSystem/Rewards/Currency Reward")]
public class CurrencyRewardSO : RewardSO
{
    [Tooltip("The amount of currency (gold) to grant.")]
    [Min(0)] public int amount = 50;

    /// <summary>
    /// Applies the currency reward by calling the PlayerManager's AddGold method.
    /// </summary>
    /// <param name="player">The PlayerManager instance to apply the reward to.</param>
    public override void ApplyReward(PlayerManager player)
    {
        if (player == null)
        {
            Debug.LogError($"Cannot apply Currency Reward: PlayerManager is null for '{this.name}'.", this);
            return;
        }

        player.AddGold(amount);
        Debug.Log($"Applied Currency Reward: {amount} Gold to {player.name}. (Via {this.name})", this);
    }
}
```

---

### **6. `ItemRewardSO.cs`**
A concrete implementation of `RewardSO` for granting items to the player's inventory.

```csharp
using UnityEngine;

/// <summary>
/// Concrete reward type: Grants one or more items to the player's inventory.
/// Inherits from RewardSO, making it a data-driven reward asset that can
/// be configured in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewItemReward", menuName = "QuestRewardSystem/Rewards/Item Reward")]
public class ItemRewardSO : RewardSO
{
    [Tooltip("The specific ItemSO asset to grant.")]
    public ItemSO item;
    [Tooltip("The quantity of the item to grant.")]
    [Min(1)] public int quantity = 1;

    /// <summary>
    /// Applies the item reward by calling the PlayerManager's AddItem method.
    /// </summary>
    /// <param name="player">The PlayerManager instance to apply the reward to.</param>
    public override void ApplyReward(PlayerManager player)
    {
        if (player == null)
        {
            Debug.LogError($"Cannot apply Item Reward: PlayerManager is null for '{this.name}'.", this);
            return;
        }
        if (item == null)
        {
            Debug.LogError($"Cannot apply Item Reward: ItemSO is null for '{this.name}'. Please assign an item.", this);
            return;
        }
        if (quantity <= 0)
        {
            Debug.LogWarning($"Item Reward '{this.name}' for '{item.itemName}' has a quantity of {quantity}. No item will be added.", this);
            return;
        }

        player.AddItem(item, quantity);
        Debug.Log($"Applied Item Reward: {quantity}x {item.itemName} to {player.name}. (Via {this.name})", this);
    }
}
```

---

### **7. `QuestDefinitionSO.cs`**
This ScriptableObject defines a quest and the list of `RewardSO` assets associated with its completion. This is the central piece that links quests to their rewards.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a quest and the rewards associated with its completion.
/// This is a ScriptableObject, allowing quests to be data-driven assets
/// that can be created and configured directly in the Unity Editor.
/// </summary>
/// <remarks>
/// This ScriptableObject acts as the "Quest" in "QuestRewardSystem".
/// It holds a list of generic RewardSO objects. This design choice
/// effectively decouples the quest's definition from the specific logic
/// of each individual reward type. A quest doesn't care *how* a reward
/// is applied, only *what* rewards it offers.
/// </remarks>
[CreateAssetMenu(fileName = "NewQuestDefinition", menuName = "QuestRewardSystem/Quest Definition")]
public class QuestDefinitionSO : ScriptableObject
{
    [Header("Quest Information")]
    [Tooltip("The display name of the quest.")]
    public string questName = "New Quest";
    [TextArea(3, 10), Tooltip("A detailed description of the quest objectives.")]
    public string description = "A description of the quest objective.";

    [Header("Quest Rewards")]
    [Tooltip("List of all RewardSO assets granted upon quest completion.")]
    public List<RewardSO> rewards = new List<RewardSO>();

    /// <summary>
    /// Grants all rewards defined in this quest to the specified player.
    /// This method iterates through the list of `RewardSO` assets and calls
    /// their individual `ApplyReward` methods. Each `RewardSO` then handles
    /// the specific logic of modifying the player's state.
    /// </summary>
    /// <param name="player">The PlayerManager instance to receive the rewards.</param>
    public void GrantRewards(PlayerManager player)
    {
        if (player == null)
        {
            Debug.LogError($"Cannot grant rewards for quest '{questName}': PlayerManager is null.", this);
            return;
        }

        Debug.Log($"--- Granting Rewards for Quest: '{questName}' ---", this);
        if (rewards.Count == 0)
        {
            Debug.LogWarning($"Quest '{questName}' has no rewards defined. No rewards will be granted.", this);
            return;
        }

        foreach (RewardSO reward in rewards)
        {
            if (reward == null)
            {
                Debug.LogWarning($"Quest '{questName}' has a null reward entry in its list. Skipping this reward.", this);
                continue;
            }
            // This is the core polymorphic call: each reward object knows how to apply itself.
            reward.ApplyReward(player); 
        }
        Debug.Log($"--- Finished Granting Rewards for Quest: '{questName}' ---", this);
    }
}
```

---

### **8. `QuestManager.cs`**
This MonoBehaviour serves as a high-level system that might track quest progress and trigger reward granting upon completion. It demonstrates how a game system would interact with the `QuestRewardSystem`.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages quest completion and triggers the reward system.
/// This MonoBehaviour demonstrates how a game system would interact
/// with the QuestRewardSystem to grant rewards upon quest completion.
/// </summary>
/// <remarks>
/// In a real game, this class might manage a list of active quests,
/// check completion conditions, and then call CompleteQuest when appropriate.
/// For this example, it simply completes a predefined list of quests on Start.
/// </remarks>
public class QuestManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the player manager who will receive rewards. Auto-finds if not set.")]
    [SerializeField] private PlayerManager _playerManager;

    [Header("Test Quests")]
    [Tooltip("Drag the QuestDefinition ScriptableObjects here to test their rewards.")]
    [SerializeField] private List<QuestDefinitionSO> _questsToComplete = new List<QuestDefinitionSO>();

    private void Awake()
    {
        // Attempt to find PlayerManager if not assigned in Inspector
        if (_playerManager == null)
        {
            _playerManager = FindObjectOfType<PlayerManager>();
            if (_playerManager == null)
            {
                Debug.LogError("QuestManager: No PlayerManager found in scene! Rewards cannot be applied.", this);
            }
        }
    }

    /// <summary>
    /// Public method to simulate completing a quest. This would be called
    /// by your game's quest tracking logic.
    /// </summary>
    /// <param name="quest">The QuestDefinitionSO that has been completed.</param>
    public void CompleteQuest(QuestDefinitionSO quest)
    {
        if (quest == null)
        {
            Debug.LogError("QuestManager: Attempted to complete a null quest definition.", this);
            return;
        }

        if (_playerManager == null)
        {
            Debug.LogError($"QuestManager: Cannot complete quest '{quest.questName}'. PlayerManager is null.", this);
            return;
        }

        Debug.Log($"QuestManager: Quest '{quest.questName}' completed! Initiating reward granting...", this);

        // The core interaction with the QuestRewardSystem pattern:
        // We tell the QuestDefinition to grant its rewards.
        // The QuestDefinition then handles iterating through its RewardSOs
        // and applying them to the PlayerManager.
        quest.GrantRewards(_playerManager);
    }

    /// <summary>
    /// Simple test to complete all quests in the `_questsToComplete` list when the game starts.
    /// This is purely for demonstration purposes to show the system in action.
    /// </summary>
    void Start()
    {
        if (_playerManager == null)
        {
            Debug.LogError("QuestManager: PlayerManager is null, cannot run demo quests.", this);
            return;
        }

        Debug.Log("\n--- QuestRewardSystem Demo Start ---");
        Debug.Log($"Player Initial State: XP={_playerManager.CurrentExperience}, Gold={_playerManager.CurrentGold}, Items={_playerManager.Inventory.Count}");

        // Iterate and complete each predefined test quest
        foreach (QuestDefinitionSO quest in _questsToComplete)
        {
            if (quest != null)
            {
                CompleteQuest(quest);
                Debug.Log($"Player Current State After '{quest.questName}': XP={_playerManager.CurrentExperience}, Gold={_playerManager.CurrentGold}");
                if (_playerManager.Inventory.Count > 0)
                {
                    Debug.Log("    Current Inventory:");
                    foreach(var item in _playerManager.Inventory)
                    {
                        Debug.Log($"        - {item.quantity}x {item.item.itemName}");
                    }
                }
                else
                {
                    Debug.Log("    Inventory is empty.");
                }
                Debug.Log("-------------------------------------\n");
            }
            else
            {
                Debug.LogWarning("QuestManager: A null quest definition was found in the '_questsToComplete' list. Skipping.", this);
            }
        }
        Debug.Log("--- QuestRewardSystem Demo End ---\n");
    }

    // Example of how you might trigger quest completion from a UI Button or other event:
    // public void OnCompleteQuestButton(QuestDefinitionSO questToComplete)
    // {
    //     CompleteQuest(questToComplete);
    // }
}
```

---

### **How to Use This Example in Unity:**

1.  **Create an Empty GameObject:**
    *   In your Unity project, create a new empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty).
    *   Rename it to `GameManager`.

2.  **Attach Scripts to `GameManager`:**
    *   Drag and drop `PlayerManager.cs` onto the `GameManager` in the Hierarchy.
    *   Drag and drop `QuestManager.cs` onto the `GameManager`.

3.  **Create Folders for ScriptableObjects:**
    *   In your Project window, create a new folder structure, for example: `Assets/ScriptableObjects/Items`, `Assets/ScriptableObjects/Rewards`, `Assets/ScriptableObjects/Quests`. This helps keep your assets organized.

4.  **Create Item Assets:**
    *   Right-click in `Assets/ScriptableObjects/Items` -> Create -> QuestRewardSystem -> Item.
        *   Name it `Item_Sword`. Set `itemName = Sword`, `description = A sharp, gleaming blade.`, `itemType = Weapon`.
        *   Create another: `Item_HealthPotion`. Set `itemName = Health Potion`, `description = Restores a small amount of health.`, `itemType = Consumable`.

5.  **Create Reward Assets:**
    *   Right-click in `Assets/ScriptableObjects/Rewards` -> Create -> QuestRewardSystem -> Rewards -> Experience Reward.
        *   Name it `Reward_XP100`. Set `amount = 100`.
    *   Right-click -> Create -> QuestRewardSystem -> Rewards -> Currency Reward.
        *   Name it `Reward_Gold50`. Set `amount = 50`.
    *   Right-click -> Create -> QuestRewardSystem -> Rewards -> Item Reward.
        *   Name it `Reward_Sword`. Drag `Item_Sword` into the `Item` field. Set `quantity = 1`.
    *   Right-click -> Create -> QuestRewardSystem -> Rewards -> Item Reward.
        *   Name it `Reward_HealthPotions3`. Drag `Item_HealthPotion` into the `Item` field. Set `quantity = 3`.

6.  **Create Quest Definition Assets:**
    *   Right-click in `Assets/ScriptableObjects/Quests` -> Create -> QuestRewardSystem -> Quest Definition.
        *   Name it `QuestDef_KillSlime`.
        *   Set `questName = Kill the Pesky Slime`, `description = Locate and defeat the slime causing trouble in the forest.`
        *   In its Inspector, expand the `Rewards` list.
        *   Set Size to `2`.
        *   Drag `Reward_XP100` into Element 0.
        *   Drag `Reward_Gold50` into Element 1.
    *   Create another: `QuestDef_FindTreasure`.
        *   Set `questName = Find the Hidden Treasure`, `description = Search the ancient ruins for a legendary treasure chest.`
        *   Expand the `Rewards` list.
        *   Set Size to `3`.
        *   Drag `Reward_XP100` into Element 0.
        *   Drag `Reward_Sword` into Element 1.
        *   Drag `Reward_HealthPotions3` into Element 2.

7.  **Assign Quests to `QuestManager`:**
    *   Select the `GameManager` object in your Hierarchy.
    *   In the Inspector, find the `Quest Manager` component.
    *   Expand `Test Quests`.
    *   Set its Size to `2`.
    *   Drag `QuestDef_KillSlime` from your Project window into Element 0.
    *   Drag `QuestDef_FindTreasure` from your Project window into Element 1.

8.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Open your Console window (`Window -> General -> Console`).
    *   You will see detailed logs showing the player's initial state, then the rewards being applied for each quest in sequence, and finally the player's updated state.
    *   You can also select the `GameManager` object while the game is running to observe the `PlayerManager`'s `Current Experience`, `Current Gold`, and `Inventory` update in real-time in the Inspector.

This setup demonstrates a fully functional, data-driven Quest Reward System that is easy to extend with new reward types without modifying existing quest definitions.