// Unity Design Pattern Example: LootChestSystem
// This script demonstrates the LootChestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates a robust and practical **Loot Chest System** design pattern. It focuses on modularity, reusability, and editor-friendliness using Scriptable Objects and clear separation of concerns.

The core idea of the "Loot Chest System" as a design pattern here is to:
1.  **Decouple Loot Data from Loot Chest Logic:** `LootItem` and `LootTable` are Scriptable Objects, making them reusable assets.
2.  **Centralize Loot Generation Logic:** The `LootTable` handles the complexities of weighted random drops.
3.  **Abstract Interaction:** The `LootChest` MonoBehaviour handles game-world interaction and delegates loot generation to its assigned `LootTable`.
4.  **Support Extensibility:** New item types can be added by inheriting `LootItem` without modifying core chest logic.

---

### Project Structure (Recommended)

To keep your Unity project organized, create these folders:

```
Assets/
├── LootChestSystem/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── LootItem.cs
│   │   │   ├── LootDrop.cs
│   │   │   ├── LootResult.cs
│   │   │   └── LootTable.cs
│   │   ├── Gameplay/
│   │   │   ├── LootChest.cs
│   │   │   └── PlayerInventory.cs
│   │   └── Items/ (Optional: For specific item types)
│   │       ├── PotionItem.cs
│   │       └── WeaponItem.cs
│   ├── LootItems/ (Folder to store your actual ScriptableObject item assets)
│   ├── LootTables/ (Folder to store your actual ScriptableObject loot table assets)
│   └── Prefabs/ (Optional: For chest prefabs, UI prefabs)
```

---

### 1. `LootItem.cs` (Core/LootItem.cs)

This is the base class for all items that can be looted. It's a `ScriptableObject`, meaning you can create instances of items directly in the Unity Editor as assets.

```csharp
using UnityEngine;

namespace LootChestSystem.Core
{
    /// <summary>
    /// Represents a base item that can be found in a loot chest.
    /// This is a ScriptableObject, allowing you to create item definitions
    /// as assets in the Unity Editor, making them reusable and modular.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootItem", menuName = "Loot System/Loot Item")]
    public class LootItem : ScriptableObject
    {
        [Header("Item Details")]
        public string itemName = "New Item";
        public Sprite itemIcon;
        [TextArea]
        public string itemDescription = "A generic item.";
        public int maxStackSize = 1; // Maximum stack size in inventory (e.g., 99 for potions, 1 for swords)

        /// <summary>
        /// Virtual method for using the item. Derived classes can override this
        /// to provide specific functionality (e.g., consuming a potion, equipping a weapon).
        /// </summary>
        public virtual void Use()
        {
            Debug.Log($"Using {itemName}. (Base functionality)", this);
            // In a real game, this would trigger specific game logic.
        }
    }
}
```

### 2. `PotionItem.cs` & `WeaponItem.cs` (Items/)

Examples of specific item types inheriting from `LootItem`. This demonstrates the extensibility of the system.

```csharp
using UnityEngine;
using LootChestSystem.Core; // Important: Use the namespace of LootItem

namespace LootChestSystem.Items
{
    /// <summary>
    /// An example derived class for a Potion item.
    /// It adds specific properties and overrides the Use method.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPotionItem", menuName = "Loot System/Potion Item")]
    public class PotionItem : LootItem
    {
        [Header("Potion Properties")]
        public int healthRestored = 25;
        public GameObject particlesOnUse; // Example: Particle effect

        public override void Use()
        {
            base.Use(); // Call base method if needed
            Debug.Log($"Using {itemName}: Restored {healthRestored} health!", this);
            // Example: Trigger health restoration logic for player
            // PlayerHealth.Instance.RestoreHealth(healthRestored);

            if (particlesOnUse != null)
            {
                // Instantiate particles at player position, etc.
                // Instantiate(particlesOnUse, Player.Instance.transform.position, Quaternion.identity);
            }
        }
    }
}
```

```csharp
using UnityEngine;
using LootChestSystem.Core; // Important: Use the namespace of LootItem

namespace LootChestSystem.Items
{
    /// <summary>
    /// An example derived class for a Weapon item.
    /// It adds specific properties and overrides the Use method for equipping.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponItem", menuName = "Loot System/Weapon Item")]
    public class WeaponItem : LootItem
    {
        [Header("Weapon Properties")]
        public int damage = 10;
        public float attackSpeed = 1.0f;
        public GameObject weaponPrefab; // Visual model for the weapon

        public override void Use()
        {
            base.Use(); // Call base method if needed
            Debug.Log($"Equipping {itemName}: Damage {damage}, Speed {attackSpeed}.", this);
            // Example: Equip weapon logic
            // PlayerEquipment.Instance.EquipWeapon(this);
            // Instantiate(weaponPrefab, Player.Instance.HandTransform);
        }
    }
}
```

### 3. `LootDrop.cs` (Core/LootDrop.cs)

A simple serializable struct that defines a single potential item drop within a `LootTable`.

```csharp
using UnityEngine; // For Range attribute and Debug.LogWarning
using System; // For Serializable

namespace LootChestSystem.Core
{
    /// <summary>
    /// Represents a single potential item drop within a LootTable.
    /// It holds the item, its drop chance, and the quantity range.
    /// Marked as [Serializable] so it can be edited in the Inspector within a list.
    /// </summary>
    [Serializable]
    public struct LootDrop
    {
        [Tooltip("The LootItem ScriptableObject that can be dropped.")]
        public LootItem item;

        [Tooltip("The percentage chance (0-100) this item has to drop, relative to other items in the table.")]
        [Range(0, 100)]
        public float dropChance;

        [Tooltip("The minimum quantity of this item that will drop if selected.")]
        public int minQuantity;

        [Tooltip("The maximum quantity of this item that will drop if selected.")]
        public int maxQuantity;

        /// <summary>
        /// Called by the Unity Editor when values are changed.
        /// Ensures quantities are valid and warns about null items.
        /// </summary>
        public void OnValidate()
        {
            if (minQuantity < 0) minQuantity = 0;
            if (maxQuantity < minQuantity) maxQuantity = minQuantity;
            if (item == null) Debug.LogWarning("LootDrop has a null item!", item);
        }
    }
}
```

### 4. `LootResult.cs` (Core/LootResult.cs)

A simple struct to hold the actual item and quantity that was generated by a `LootTable`.

```csharp
using LootChestSystem.Core; // Important: Use the namespace of LootItem

namespace LootChestSystem.Core
{
    /// <summary>
    /// A simple struct to represent an item and its quantity that was successfully generated as loot.
    /// </summary>
    public struct LootResult
    {
        public LootItem item;
        public int quantity;

        public LootResult(LootItem item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }
}
```

### 5. `LootTable.cs` (Core/LootTable.cs)

This is a `ScriptableObject` that defines a collection of `LootDrop` items and contains the core logic for randomly selecting loot based on their chances. This centralizes the "loot generation strategy".

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ methods like .Sum()

namespace LootChestSystem.Core
{
    /// <summary>
    /// Defines a collection of potential loot drops and the logic to select them.
    /// This is a ScriptableObject, allowing different loot tables to be created
    /// as assets and assigned to various loot sources (chests, enemies, quest rewards).
    /// This pattern uses Composition: LootChest has a LootTable.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Loot System/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("List of items that can potentially drop from this loot table, along with their chances and quantities.")]
        [SerializeField]
        private List<LootDrop> potentialDrops = new List<LootDrop>();

        /// <summary>
        /// Called when the script is loaded or a value is changed in the inspector.
        /// Ensures all contained LootDrop entries are valid.
        /// </summary>
        private void OnValidate()
        {
            foreach (var drop in potentialDrops)
            {
                // Call OnValidate on each struct to ensure its internal values are correct
                drop.OnValidate();
            }
        }

        /// <summary>
        /// Generates a list of random loot items based on their drop chances and quantities.
        /// This method encapsulates the "Strategy" for how loot is generated from this table.
        /// </summary>
        /// <param name="numRolls">How many times to "roll" on the loot table. Each roll can yield an item.</param>
        /// <returns>A list of <see cref="LootResult"/>, containing the item and its quantity.</returns>
        public List<LootResult> GenerateLoot(int numRolls = 1)
        {
            List<LootResult> generatedLoot = new List<LootResult>();

            if (potentialDrops == null || potentialDrops.Count == 0)
            {
                Debug.LogWarning($"LootTable '{name}' is empty. No loot will be generated.", this);
                return generatedLoot;
            }

            // Calculate the total weight of all items to normalize drop chances.
            // If dropChance is treated as a percentage, totalWeight should ideally be 100.
            // But this system works even if chances sum to more/less than 100,
            // as it treats them as relative weights.
            float totalWeight = potentialDrops.Sum(drop => drop.dropChance);

            if (totalWeight <= 0)
            {
                Debug.LogWarning($"LootTable '{name}' has zero or negative total drop chances. No loot will be generated.", this);
                return generatedLoot;
            }

            for (int i = 0; i < numRolls; i++)
            {
                // Generate a random point within the total weight
                float randomPoint = Random.Range(0f, totalWeight);

                foreach (LootDrop drop in potentialDrops)
                {
                    if (drop.item == null) continue; // Skip if the item reference is null

                    // If the random point falls within this item's chance range
                    if (randomPoint < drop.dropChance)
                    {
                        // Item selected! Determine its quantity.
                        int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1); // +1 because max is exclusive

                        if (quantity > 0)
                        {
                            generatedLoot.Add(new LootResult(drop.item, quantity));
                        }
                        break; // Stop after finding an item for this roll
                    }
                    randomPoint -= drop.dropChance; // Subtract this item's chance and move to the next segment
                }
            }
            return generatedLoot;
        }
    }
}
```

### 6. `PlayerInventory.cs` (Gameplay/PlayerInventory.cs)

A simple MonoBehaviour representing the player's inventory. In a real game, this would be more complex (e.g., using UI updates, dedicated inventory slots), but here it serves to demonstrate where loot goes. A basic Singleton pattern is used for easy access.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ methods

using LootChestSystem.Core; // Important: Use the namespace of LootItem

namespace LootChestSystem.Gameplay
{
    /// <summary>
    /// A simple example Player Inventory MonoBehaviour to demonstrate receiving items.
    /// In a real game, this would likely be more complex, potentially a Singleton or an object
    /// managed by a dedicated game manager, and would include UI updates.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        // Simple dictionary to hold items and their quantities.
        // In a real game, you might use a list of custom 'InventorySlot' objects.
        private Dictionary<LootItem, int> inventory = new Dictionary<LootItem, int>();

        // Public getter for external systems to inspect the inventory.
        public IReadOnlyDictionary<LootItem, int> InventoryItems => inventory;

        // Simple Singleton pattern for easy access from other scripts (e.g., LootChest).
        public static PlayerInventory Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Ensure only one instance exists
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep inventory across scenes
            }
        }

        /// <summary>
        /// Adds an item to the player's inventory.
        /// This is the method that a LootChest will call to deliver its contents.
        /// </summary>
        /// <param name="item">The <see cref="LootItem"/> to add.</param>
        /// <param name="quantity">The quantity of the item to add.</param>
        public void AddItem(LootItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("Attempted to add null item or non-positive quantity to inventory.");
                return;
            }

            if (inventory.ContainsKey(item))
            {
                inventory[item] += quantity;
            }
            else
            {
                inventory.Add(item, quantity);
            }

            Debug.Log($"[Inventory] Added {quantity} x {item.itemName}. Current total: {inventory[item]}", item);
            // TODO: Trigger UI update events here if you had an inventory UI.
        }

        /// <summary>
        /// Removes an item from the player's inventory.
        /// </summary>
        /// <param name="item">The <see cref="LootItem"/> to remove.</param>
        /// <param name="quantity">The quantity of the item to remove.</param>
        /// <returns>True if items were successfully removed, false otherwise.</returns>
        public bool RemoveItem(LootItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            if (inventory.ContainsKey(item))
            {
                if (inventory[item] >= quantity)
                {
                    inventory[item] -= quantity;
                    if (inventory[item] <= 0)
                    {
                        inventory.Remove(item);
                    }
                    Debug.Log($"[Inventory] Removed {quantity} x {item.itemName}.", item);
                    // TODO: Trigger UI update events here if you had an inventory UI.
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[Inventory] Not enough {item.itemName} to remove {quantity}. Has {inventory[item]}.", item);
                    return false;
                }
            }
            Debug.LogWarning($"[Inventory] Item {item.itemName} not found in inventory.", item);
            return false;
        }

        /// <summary>
        /// Debug method to display current inventory contents in the console.
        /// </summary>
        public void DisplayInventory()
        {
            Debug.Log("--- Player Inventory ---");
            if (inventory.Count == 0)
            {
                Debug.Log("Inventory is empty.");
                return;
            }
            foreach (var entry in inventory)
            {
                Debug.Log($"- {entry.Key.itemName}: {entry.Value}");
            }
            Debug.Log("------------------------");
        }
    }
}
```

### 7. `LootChest.cs` (Gameplay/LootChest.cs)

This MonoBehaviour is the interactive game object that represents a chest in the world. It uses composition by holding a reference to a `LootTable` ScriptableObject. It handles the interaction, animation, and delegates the actual loot generation to the `LootTable`.

```csharp
using UnityEngine;
using System.Collections.Generic;

using LootChestSystem.Core; // Important: Use the namespace of LootTable and LootResult

namespace LootChestSystem.Gameplay
{
    /// <summary>
    /// Represents an interactive loot chest in the game world.
    /// This MonoBehaviour orchestrates the opening process and delegates loot generation
    /// to a <see cref="LootTable"/> ScriptableObject.
    /// This class follows the Composition principle: a LootChest *has a* LootTable.
    /// </summary>
    public class LootChest : MonoBehaviour
    {
        [Header("Loot Configuration")]
        [Tooltip("The LootTable ScriptableObject defining what items this chest can drop.")]
        [SerializeField]
        private LootTable lootTable;

        [Tooltip("Number of times to 'roll' on the loot table when opening the chest. " +
                 "Each roll has a chance to yield an item.")]
        [SerializeField]
        private int numberOfLootRolls = 1;

        [Tooltip("Whether the chest has already been opened. Prevents re-opening.")]
        [SerializeField]
        private bool hasBeenOpened = false;

        [Header("Visual & Audio")]
        [Tooltip("Optional: Animator component on the chest for opening animations.")]
        [SerializeField]
        private Animator chestAnimator;
        [Tooltip("Name of the trigger parameter in the Animator to play the open animation.")]
        [SerializeField]
        private string openTriggerName = "Open";

        [Tooltip("Optional: AudioSource component for playing opening sounds.")]
        [SerializeField]
        private AudioSource chestAudioSource;
        [Tooltip("The AudioClip to play when the chest is opened.")]
        [SerializeField]
        private AudioClip openSound;

        // You could add a UnityEvent here to allow other systems to react to the chest opening
        // public UnityEvent OnChestOpened;

        /// <summary>
        /// Public method to interact with and open the chest.
        /// This method acts as the entry point for players or other game systems to open the chest.
        /// </summary>
        public void OpenChest()
        {
            if (hasBeenOpened)
            {
                Debug.Log($"Chest '{name}' has already been opened.", this);
                return;
            }

            if (lootTable == null)
            {
                Debug.LogError($"LootChest '{name}' has no LootTable assigned! Cannot open.", this);
                return;
            }

            Debug.Log($"[LootChest] Opening '{name}'...", this);

            // 1. Mark the chest as opened to prevent future interactions.
            hasBeenOpened = true;

            // 2. Trigger visual and audio effects.
            PlayOpenEffects();

            // 3. Delegate to the LootTable to generate the actual loot.
            // This decouples the chest's interaction logic from the complex loot generation logic.
            List<LootResult> generatedLoot = lootTable.GenerateLoot(numberOfLootRolls);

            // 4. Distribute the generated loot to the player's inventory or other recipient.
            DistributeLoot(generatedLoot);

            // Optional: Disable collider, interaction components, or destroy the object after opening.
            // For example, if it's a one-time use chest.
            GetComponent<Collider>()?.enabled = false;
            // Optionally, destroy this GameObject after a delay (e.g., after animation finishes)
            // Destroy(gameObject, 5f);
        }

        /// <summary>
        /// Plays the chest opening animations and sounds if they are assigned.
        /// </summary>
        private void PlayOpenEffects()
        {
            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(openTriggerName);
            }

            if (chestAudioSource != null && openSound != null)
            {
                chestAudioSource.PlayOneShot(openSound);
            }
        }

        /// <summary>
        /// Distributes the generated loot items to the player's inventory.
        /// This method provides the integration point with the game's inventory system.
        /// </summary>
        /// <param name="loot">A list of <see cref="LootResult"/> objects to give to the player.</param>
        private void DistributeLoot(List<LootResult> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                Debug.Log($"[LootChest] '{name}' was opened but yielded no loot.", this);
                return;
            }

            Debug.Log($"[LootChest] Loot from '{name}':");
            foreach (LootResult result in loot)
            {
                Debug.Log($" - {result.quantity} x {result.item.itemName}", result.item);

                // Attempt to add to PlayerInventory.
                // This relies on PlayerInventory being accessible (e.g., via Singleton).
                if (PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.AddItem(result.item, result.quantity);
                }
                else
                {
                    Debug.LogWarning($"[LootChest] PlayerInventory.Instance not found! Loot ({result.quantity}x {result.item.itemName}) not added to inventory. " +
                                     "Ensure a PlayerInventory GameObject exists in the scene.", this);
                    // Fallback: Optionally instantiate physical item pickups if no inventory system is available.
                }
            }

            // Display the updated inventory in the console for debugging purposes.
            PlayerInventory.Instance?.DisplayInventory();
            // OnChestOpened?.Invoke(); // Example of triggering a UnityEvent
        }

        /// <summary>
        /// Example of how the chest might be triggered by a player interaction.
        /// This uses Unity's trigger system. A more advanced system might use Raycasting
        /// or a dedicated interaction manager.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Assuming the player has the tag "Player" and a Collider (set to Is Trigger) and Rigidbody.
            if (other.CompareTag("Player") && !hasBeenOpened)
            {
                Debug.Log($"Player is near '{name}'. Press 'E' to open (or call OpenChest directly).", this);
                // For a more robust system, a UI prompt might appear, and a player script
                // would then call OpenChest() when 'E' is pressed.
                // For this example, we'll simulate a direct open if no other interaction is set up.
                // OpenChest(); // Uncomment this line for immediate opening on trigger.
            }
        }
    }
}
```

---

### How to Implement and Use in Unity

1.  **Create Folders:** Set up the recommended folder structure (`Assets/LootChestSystem/Scripts/Core`, etc.).
2.  **Place Scripts:** Copy all the C# code into their respective `.cs` files in the correct folders.
3.  **Create Player Inventory:**
    *   In your Unity scene, create an empty GameObject (e.g., named `PlayerInventoryManager`).
    *   Add the `PlayerInventory.cs` component to it. This will make it a Singleton, accessible throughout your game.
4.  **Create Loot Item Assets:**
    *   Navigate to your `Assets/LootChestSystem/LootItems` folder.
    *   Right-click in the Project window -> `Create -> Loot System -> Loot Item`.
    *   Also, create `Potion Item` and `Weapon Item` assets.
    *   Name them appropriately (e.g., `HealthPotion`, `SwordOfTruth`).
    *   Fill in their details in the Inspector (item name, icon, description, specific properties like health restored or damage).
5.  **Create Loot Table Assets:**
    *   Navigate to your `Assets/LootChestSystem/LootTables` folder.
    *   Right-click in the Project window -> `Create -> Loot System -> Loot Table`.
    *   Name it (e.g., `ForestChestLoot`, `BossDropTable`).
    *   In the Inspector for your `LootTable` asset, expand the `Potential Drops` list.
    *   Add elements to the list. For each element:
        *   Drag and drop one of your created `LootItem` assets into the `Item` field.
        *   Set the `Drop Chance` (e.g., 50 for common, 10 for rare).
        *   Set `Min Quantity` and `Max Quantity`.
        *   Experiment with different item types and chances.
6.  **Create a Loot Chest in Scene:**
    *   Create an empty GameObject in your scene (e.g., `MyLootChest`).
    *   Add the `LootChest.cs` component to it.
    *   In the Inspector for `MyLootChest`:
        *   Drag and drop one of your created `LootTable` assets into the `Loot Table` field.
        *   Set `Number Of Loot Rolls` (e.g., 1 for a single item, 3 for multiple chances).
        *   **Optional:** Add a visual model (e.g., a 3D chest model) as a child of `MyLootChest`.
        *   **Optional:** Add an `Animator` component (if your model has animations) and assign it to the `Chest Animator` field. Make sure your Animator Controller has an "Open" trigger parameter.
        *   **Optional:** Add an `AudioSource` component and assign it to the `Chest Audio Source` field, then drag an `AudioClip` into the `Open Sound` field.
        *   **For Interaction via Trigger:** Add a `BoxCollider` component to `MyLootChest`. Check "Is Trigger". Adjust its size to encompass the chest.
7.  **Setup Your Player (for trigger interaction):**
    *   Ensure your Player GameObject has:
        *   A `Collider` (e.g., `CapsuleCollider`).
        *   A `Rigidbody` (required for trigger events).
        *   The **Tag** "Player" (or whatever tag you check in `LootChest.OnTriggerEnter`).
8.  **Run the Scene:**
    *   Play your scene.
    *   Move your player into the `MyLootChest`'s trigger area.
    *   Observe the Console window: You'll see messages about the chest opening, what loot was generated, and items being added to the `PlayerInventory`. You can also call `PlayerInventory.Instance.DisplayInventory()` from anywhere for a current inventory snapshot.

This setup provides a complete, flexible, and extensible Loot Chest System that is ready for use and modification in your Unity projects.