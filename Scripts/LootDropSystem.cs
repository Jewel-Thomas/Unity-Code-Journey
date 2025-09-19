// Unity Design Pattern Example: LootDropSystem
// This script demonstrates the LootDropSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'LootDropSystem' design pattern in Unity is used to manage and determine which items (loot) are dropped by enemies, opened from chests, or awarded for quests. It typically involves defining loot items, assigning probabilities or weights to their drops, and providing a mechanism to randomly select items based on these chances.

This example provides a complete, practical C# Unity script that implements a flexible Loot Drop System using ScriptableObjects for defining loot items and loot tables, and a MonoBehaviour for demonstrating the dropping process.

---

### How the LootDropSystem Pattern Works in this Example:

1.  **`BaseLootItem` (Abstract ScriptableObject):** This is the foundation for all droppable items. It defines common properties like name, icon, and description. Using an abstract base allows for different types of loot items to be created (e.g., `ConsumableLootItem`, `EquipmentLootItem`) while still being handled uniformly by the loot system.
2.  **Concrete Loot Items (e.g., `ConsumableLootItem`, `EquipmentLootItem`):** These inherit from `BaseLootItem` and add specific properties relevant to their type (e.g., `healAmount` for a potion, `attackBonus` for a weapon). These are created as individual assets in the Unity Editor.
3.  **`LootDrop` (Serializable Struct):** This struct links a `BaseLootItem` with its `dropWeight`. The `dropWeight` determines how likely this specific item is to be chosen relative to other items in the same loot table.
4.  **`LootTable` (ScriptableObject):** This is the core of the drop logic. It holds a list of `LootDrop` entries. When `GetRandomDrop()` is called, it calculates the total weight of all items and then uses a weighted random selection algorithm to pick one `BaseLootItem` from its list. This allows you to create different loot tables for different enemies, zones, or chest types.
5.  **`LootDropper` (MonoBehaviour):** This component demonstrates how to use the `LootTable`. You attach it to a GameObject (e.g., an enemy, a chest) and assign a `LootTable` asset. When its `DropLoot()` method is called, it requests a random item from its assigned `LootTable`. For this example, it simply logs the dropped item; in a real game, this would involve adding the item to an inventory, spawning a physical item, etc.

---

### Ready-to-use C# Script: `LootSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like Sum

namespace LootSystem
{
    // =========================================================================
    // 1. BaseLootItem: The abstract base class for all items that can be dropped.
    //    It defines common properties and serves as a generic type for the loot system.
    // =========================================================================
    [CreateAssetMenu(fileName = "NewLootItem", menuName = "Loot System/Base Loot Item")]
    public abstract class BaseLootItem : ScriptableObject
    {
        [Header("Base Item Properties")]
        public string itemName = "New Item";
        public Sprite itemIcon;
        [TextArea(3, 5)]
        public string itemDescription = "A generic item.";
        
        // You could add an ID system here if needed for serialization/networking
        // public int itemID;

        // Abstract method example: you could force derived classes to implement
        // specific behavior, though for a drop system, just defining the item
        // is often enough. The 'use' logic would be in an inventory/player system.
        // public abstract void UseItem();
    }

    // =========================================================================
    // 2. Concrete Loot Item Examples: Derived classes for specific item types.
    //    These demonstrate how you can extend BaseLootItem with unique properties.
    // =========================================================================

    [CreateAssetMenu(fileName = "NewConsumable", menuName = "Loot System/Consumable Item")]
    public class ConsumableLootItem : BaseLootItem
    {
        [Header("Consumable Properties")]
        public int healAmount = 0;
        public float buffDuration = 0f;
        public ConsumableType consumableType = ConsumableType.Potion;

        public enum ConsumableType { Potion, Food, Scroll, Elixir }
    }

    [CreateAssetMenu(fileName = "NewEquipment", menuName = "Loot System/Equipment Item")]
    public class EquipmentLootItem : BaseLootItem
    {
        [Header("Equipment Properties")]
        public int attackBonus = 0;
        public int defenseBonus = 0;
        public int durability = 100;
        public EquipmentSlot equipmentSlot = EquipmentSlot.Weapon;

        public enum EquipmentSlot { Weapon, Armor, Helmet, Boots, Ring, Amulet }
    }

    // You can add more item types like QuestItemLootItem, CraftingMaterialLootItem, etc.

    // =========================================================================
    // 3. LootDrop: A serializable struct that links a LootItem with its drop weight.
    //    This is used within LootTable to define potential drops and their chances.
    // =========================================================================
    [System.Serializable]
    public struct LootDrop
    {
        [Tooltip("The actual item ScriptableObject that can be dropped.")]
        public BaseLootItem item;
        [Tooltip("The weight of this item in the loot table. Higher weight = higher chance.")]
        [Range(0, 100)] // Using a range for visual clarity in inspector, can be higher
        public int dropWeight;
    }

    // =========================================================================
    // 4. LootTable: A ScriptableObject that defines a collection of potential drops.
    //    It contains the core logic for selecting a random item based on weights.
    // =========================================================================
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Loot System/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Header("Loot Table Settings")]
        public string tableName = "Default Loot Table";
        [Tooltip("List of all possible items that can drop from this table, along with their weights.")]
        public List<LootDrop> possibleDrops = new List<LootDrop>();

        /// <summary>
        /// Retrieves a random item from the loot table based on defined drop weights.
        /// </summary>
        /// <returns>A BaseLootItem that has been randomly selected, or null if the table is empty.</returns>
        public BaseLootItem GetRandomDrop()
        {
            if (possibleDrops == null || possibleDrops.Count == 0)
            {
                Debug.LogWarning($"Loot Table '{tableName}' is empty or null. No item dropped.");
                return null;
            }

            // Calculate the total weight of all items in the table.
            // This is crucial for proportional chance distribution.
            int totalWeight = possibleDrops.Sum(drop => drop.dropWeight);

            // If for some reason all weights are zero (or negative), return null to avoid division by zero.
            if (totalWeight <= 0)
            {
                Debug.LogWarning($"Loot Table '{tableName}' has a total weight of zero or less. No item dropped.");
                return null;
            }

            // Generate a random number between 0 (inclusive) and totalWeight (exclusive).
            int randomNumber = Random.Range(0, totalWeight);

            // Iterate through the items, subtracting each item's weight from the random number.
            // The first item that makes the randomNumber zero or less is the chosen item.
            foreach (var drop in possibleDrops)
            {
                randomNumber -= drop.dropWeight;
                if (randomNumber < 0)
                {
                    return drop.item; // This item has been selected!
                }
            }

            // Fallback: This should theoretically not be reached if totalWeight > 0.
            // It means something went wrong in the random number or weight calculation.
            Debug.LogError($"Loot Table '{tableName}' failed to select an item. This should not happen if total weight is positive.");
            return null;
        }
    }

    // =========================================================================
    // 5. LootDropper: A MonoBehaviour component to demonstrate using the LootTable.
    //    Attach this to an enemy, a chest, or any object that should drop loot.
    // =========================================================================
    public class LootDropper : MonoBehaviour
    {
        [Header("Loot Dropper Settings")]
        [Tooltip("The LootTable asset to use for this dropper. Define probabilities here.")]
        public LootTable lootTable;
        [Tooltip("Optional: The position where the physical loot object would appear.")]
        public Transform dropPoint;
        [Tooltip("Optional: If true, will drop loot on Start() for testing purposes.")]
        public bool dropOnStart = false;

        private void Start()
        {
            if (dropOnStart)
            {
                Debug.Log($"Dropping loot from {gameObject.name} on Start...");
                DropLoot();
            }
        }

        /// <summary>
        /// Triggers the loot dropping process.
        /// </summary>
        public void DropLoot()
        {
            if (lootTable == null)
            {
                Debug.LogError($"No LootTable assigned to {gameObject.name}. Cannot drop loot.");
                return;
            }

            // Get a random item from the assigned loot table.
            BaseLootItem droppedItem = lootTable.GetRandomDrop();

            if (droppedItem != null)
            {
                // Here's where you'd integrate with your game's inventory or spawning system.
                // For this example, we'll just log the item and its details.
                Debug.Log($"<color=green>'{gameObject.name}' dropped:</color> <color=yellow>{droppedItem.itemName}</color>!");
                Debug.Log($"Description: {droppedItem.itemDescription}");

                // Example of how to handle different item types
                if (droppedItem is ConsumableLootItem consumable)
                {
                    Debug.Log($"  - Type: Consumable ({consumable.consumableType}), Heals: {consumable.healAmount}");
                }
                else if (droppedItem is EquipmentLootItem equipment)
                {
                    Debug.Log($"  - Type: Equipment ({equipment.equipmentSlot}), Attack Bonus: {equipment.attackBonus}, Defense Bonus: {equipment.defenseBonus}");
                }
                else
                {
                    Debug.Log($"  - Type: Generic BaseLootItem");
                }

                // In a real game, you might:
                // 1. Add the item to the player's inventory: InventoryManager.Instance.AddItem(droppedItem);
                // 2. Instantiate a physical item in the world: Instantiate(droppedItem.physicalPrefab, dropPoint.position, Quaternion.identity);
                // 3. Trigger a UI notification.
            }
            else
            {
                Debug.Log($"'{gameObject.name}' dropped nothing from '{lootTable.tableName}'.");
            }
        }

        // Example: You could trigger this from a UI button or an event.
        // For editor demonstration:
        [ContextMenu("Simulate Drop Loot")]
        private void SimulateDropLoot()
        {
            DropLoot();
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create the Script:**
    *   Save the code above as `LootSystem.cs` in your Unity project (e.g., in `Assets/Scripts/LootSystem/`).

2.  **Create Loot Item Assets:**
    *   In the Unity Editor, go to `Assets -> Create -> Loot System/Consumable Item`.
        *   Name it "HealthPotion", set `healAmount` to 25.
    *   Go to `Assets -> Create -> Loot System/Equipment Item`.
        *   Name it "BasicSword", set `attackBonus` to 10, `equipmentSlot` to Weapon.
    *   Create a few more items to have variety.

3.  **Create Loot Table Assets:**
    *   Go to `Assets -> Create -> Loot System/Loot Table`.
        *   Name it "GoblinLootTable".
        *   In its inspector, expand `Possible Drops`.
        *   Add a few elements (e.g., 3).
        *   Drag your "HealthPotion" item into one `Item` slot and set `Drop Weight` to 50.
        *   Drag your "BasicSword" item into another `Item` slot and set `Drop Weight` to 10.
        *   (Optional) Add a `null` item with `Drop Weight` of 40 to represent a chance to drop nothing, if desired.
    *   Create another one named "BossLootTable" with different, perhaps rarer, items and higher weights for good drops.

4.  **Create a Loot Dropper GameObject:**
    *   In your scene, create an empty GameObject (e.g., "GoblinEnemy").
    *   Add the `LootDropper` component to it.
    *   Drag your "GoblinLootTable" asset into the `Loot Table` slot of the `LootDropper` component.
    *   Check `Drop On Start` if you want it to drop loot when the game starts, or leave it unchecked to trigger manually.

5.  **Run and Test:**
    *   Run your scene. If `Drop On Start` was checked, you'll see loot details in the console.
    *   Select your "GoblinEnemy" GameObject in the Hierarchy during Play Mode.
    *   In the Inspector, you'll see a `Simulate Drop Loot` button (thanks to `[ContextMenu]`). Click it multiple times to observe different drops according to your weights.

This setup provides a robust and easily extensible system for managing loot drops in your Unity projects, adhering to common design patterns and Unity best practices.