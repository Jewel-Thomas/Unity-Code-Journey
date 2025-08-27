// Unity Design Pattern Example: ScriptableObjects
// This script demonstrates the ScriptableObjects pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the ScriptableObjects design pattern in Unity using a common real-world scenario: defining game items.

You will find three C# scripts below:

1.  **`GameItem.cs`**: The core `ScriptableObject` that defines properties and general behavior for a game item.
2.  **`ConsumableItem.cs`**: An inherited `ScriptableObject` that specializes `GameItem` for consumables, demonstrating polymorphism.
3.  **`InventoryManager.cs`**: A `MonoBehaviour` that consumes and interacts with `GameItem` (and its derived types) instances, representing, for example, a player's inventory.

---

### 1. `GameItem.cs`

This script defines the base `ScriptableObject` for any item in your game.

```csharp
using UnityEngine;
// System.Collections is not strictly needed for this base class but often useful for complex SOs.

/// <summary>
/// <para><b>ScriptableObjects Design Pattern: Base Game Item Definition</b></para>
/// <para>
/// ScriptableObjects are data containers that can be saved as assets in the Unity Editor.
/// They do not need to be attached to a GameObject and are not Components.
/// They are ideal for:
/// <list type="bullet">
/// <item>Storing shared data (e.g., item definitions, character stats, enemy types).</item>
/// <item>Reducing memory footprint by sharing references to the same data asset.</item>
/// <item>Separating data from logic (MonoBehaviours).</item>
/// <item>Providing a flexible and extensible way to define game content through assets.</item>
/// <item>Easy serialization and persistence in the Unity Editor.</item>
/// </list>
/// </para>
/// <para>
/// This `GameItem` class serves as a blueprint for all items in our game.
/// We can create multiple instances of this ScriptableObject (e.g., "SwordItem", "ShieldItem", "PotionItem")
/// as actual `.asset` files in the Unity project, each holding unique data.
/// </para>
/// </summary>
// The [CreateAssetMenu] attribute allows you to create instances of this ScriptableObject
// directly from the Unity Editor's 'Assets/Create' menu.
// fileName: The default name for the new asset when created.
// menuName: The path in the 'Assets/Create' menu (e.g., "Scriptable Objects/Game Item").
[CreateAssetMenu(fileName = "NewGameItem", menuName = "Scriptable Objects/Game Item")]
public class GameItem : ScriptableObject
{
    // --- Core Data Fields ---
    // These fields define the data that each GameItem asset will hold.
    [Tooltip("The unique identifier for this item.")]
    public string ItemID = System.Guid.NewGuid().ToString(); // A simple way to generate a unique ID
    [Tooltip("The display name of the item.")]
    public string ItemName = "New Item";
    [Tooltip("A detailed description of the item.")]
    [TextArea(3, 10)] // Makes the string field a multi-line text area in the Inspector for better editing.
    public string Description = "A generic item.";
    [Tooltip("The icon sprite for the item, displayed in UI.")]
    public Sprite Icon; // Visual representation
    [Tooltip("How much the item is worth (e.g., for selling).")]
    public int Value = 1;
    [Tooltip("How heavy the item is (e.g., for inventory limits).")]
    public float Weight = 0.1f;

    // An enum for item categorization, demonstrating more complex data types within SOs.
    [Tooltip("The type or category of the item.")]
    public ItemType Type = ItemType.Generic;

    // --- ScriptableObject-specific Behavior ---
    // You can also add methods to ScriptableObjects to encapsulate behavior related to their data.
    // This allows items to have inherent actions, which can then be called by other game systems.
    /// <summary>
    /// Defines the default action when this item is used.
    /// Derived classes can override this method for specific item behaviors.
    /// </summary>
    public virtual void Use()
    {
        Debug.Log($"<color=green>Using {ItemName}.</color> It does something generic or has no specific active effect.");
        // In a real game, this might play a sound, trigger an animation, etc.
        // It generally *would not* modify the player's stats directly here,
        // but rather signal to a PlayerManager or InventoryManager to do so.
    }

    // You can also add other utility methods here, e.g., to get a formatted description.
    public string GetFormattedDescription()
    {
        return $"<b>{ItemName}</b>\n<i>Type: {Type}</i>\nValue: {Value}g | Weight: {Weight}kg\n\n{Description}";
    }
}

/// <summary>
/// Example enum for different item categories.
/// </summary>
public enum ItemType
{
    Generic,
    Weapon,
    Armor,
    Potion,
    Consumable,
    QuestItem,
    CraftingMaterial
}
```

---

### 2. `ConsumableItem.cs`

This script demonstrates inheritance with ScriptableObjects, allowing for specialized item types.

```csharp
using UnityEngine;

/// <summary>
/// <para><b>ScriptableObjects Design Pattern: Derived Item Example</b></para>
/// <para>
/// This class inherits from `GameItem` to create a more specific type of item: a consumable.
/// This demonstrates how ScriptableObjects can participate in inheritance hierarchies,
/// allowing for polymorphic behavior and organized item definitions.
/// </para>
/// </summary>
// We provide a different menuName and fileName for this specialized item type.
[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Scriptable Objects/Consumable Item")]
public class ConsumableItem : GameItem
{
    // Additional data specific to consumable items.
    [Header("Consumable Properties")]
    [Tooltip("Amount of health restored when consumed.")]
    public int healthRestored = 20;
    [Tooltip("Amount of mana restored when consumed.")]
    public int manaRestored = 0;
    [Tooltip("Whether the item is consumed upon use (e.g., a potion vs. a reusable buff scroll).")]
    public bool destroyOnUse = true;

    /// <summary>
    /// Overrides the base `Use` method to provide specific behavior for a consumable.
    /// </summary>
    public override void Use()
    {
        // Calling base.Use() is optional; you can completely replace the behavior if needed.
        // base.Use(); 

        Debug.Log($"<color=yellow>Using {ItemName}!</color> Restored {healthRestored} HP and {manaRestored} MP.");

        // In a real game, you would interact with a Player or Character component here
        // to apply the effects.
        // Example: PlayerStats.Instance.RestoreHealth(healthRestored);
        // Example: PlayerStats.Instance.RestoreMana(manaRestored);

        if (destroyOnUse)
        {
            Debug.Log($"{ItemName} was consumed and removed from inventory.");
            // In an actual inventory system, you would signal the inventory to remove this item.
            // Example: InventoryManager.Instance.RemoveItem(this);
        }
    }
}
```

---

### 3. `InventoryManager.cs`

This `MonoBehaviour` shows how a typical game object would interact with and utilize `ScriptableObject` assets.

```csharp
using UnityEngine;
using System.Collections.Generic; // Needed for List<GameItem>

/// <summary>
/// <para><b>ScriptableObjects Design Pattern: Consumer MonoBehaviour</b></para>
/// <para>
/// This MonoBehaviour demonstrates how to consume and interact with `ScriptableObject` assets.
/// In a real game, this might be an Inventory system, a Character's equipped items,
/// a crafting recipe manager, or any other system that needs access to defined game data.
/// </para>
/// <para>
/// It holds references to `GameItem` (and its derived types) ScriptableObject assets,
/// allowing you to assign these assets directly in the Unity Inspector.
/// </para>
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region Public Fields

    [Header("Currently Selected Item")]
    [Tooltip("Assign a GameItem or ConsumableItem ScriptableObject asset here from the Project window.")]
    // This public field allows you to assign a 'GameItem' (or any class inheriting from it)
    // ScriptableObject asset directly in the Unity Inspector.
    // This MonoBehaviour instance then holds a *reference* to that asset.
    public GameItem currentlySelectedItem;

    [Header("Inventory List (Example)")]
    [Tooltip("Drag and drop multiple GameItem or ConsumableItem assets here to simulate an inventory.")]
    // A list to demonstrate holding multiple ScriptableObjects, useful for an inventory system.
    public List<GameItem> inventoryItems = new List<GameItem>();

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        Debug.Log("<color=cyan>--- InventoryManager Initializing ---</color>");

        // --- Demonstrate usage of the 'currentlySelectedItem' ---
        if (currentlySelectedItem != null)
        {
            Debug.Log($"Currently selected item: <color=white><b>{currentlySelectedItem.ItemName}</b></color>");
            Debug.Log($"Description: {currentlySelectedItem.Description}");
            Debug.Log($"Type: {currentlySelectedItem.Type}");
            Debug.Log($"Value: {currentlySelectedItem.Value} coins");
            Debug.Log($"Weight: {currentlySelectedItem.Weight} kg");

            // Calling a method on the ScriptableObject itself.
            // This demonstrates how SOs can encapsulate behavior along with data.
            currentlySelectedItem.Use();
        }
        else
        {
            Debug.LogWarning("No GameItem assigned to 'currentlySelectedItem' in InventoryManager. Please assign one in the Inspector.");
        }

        // --- Demonstrate iterating through an 'inventory' list ---
        if (inventoryItems.Count > 0)
        {
            Debug.Log($"\n<color=yellow>--- Inventory Contents ({inventoryItems.Count} items) ---</color>");
            foreach (var item in inventoryItems)
            {
                Debug.Log($"  - {item.ItemName} (Type: {item.Type})");
                // You could also call item.Use() here for each item, or display its icon, etc.
            }
            Debug.Log($"<color=yellow>--- End Inventory Contents ---</color>");
        }
        else
        {
            Debug.Log("Inventory list is empty. Add some items in the Inspector!");
        }

        Debug.Log("<color=cyan>--- InventoryManager Setup Complete ---</color>\n");
    }

    void Update()
    {
        // Example: Press 'U' to use the currently selected item.
        if (Input.GetKeyDown(KeyCode.U))
        {
            UseSelectedItem();
        }

        // Example: Press 'C' to demonstrate changing an item's value (WARNING: this modifies the asset!)
        if (Input.GetKeyDown(KeyCode.C))
        {
            // IMPORTANT NOTE: Changes made to a ScriptableObject asset at runtime
            // are PERMANENT and will be saved to the asset file in the Project.
            // This affects ALL MonoBehaviour instances that reference this same asset.
            // If you need *instance-specific* modifications that don't affect the original asset,
            // you should create a runtime copy using `Instantiate(originalScriptableObject)`.
            ChangeItemValue(Random.Range(10, 100));
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Triggers the 'Use' method of the currently selected item.
    /// </summary>
    public void UseSelectedItem()
    {
        if (currentlySelectedItem != null)
        {
            currentlySelectedItem.Use();
        }
        else
        {
            Debug.LogWarning("Cannot use item: No item is currently selected.");
        }
    }

    /// <summary>
    /// Changes the value of the currently selected item.
    /// WARNING: This modifies the original ScriptableObject asset file!
    /// </summary>
    /// <param name="newValue">The new value to set for the item.</param>
    public void ChangeItemValue(int newValue)
    {
        if (currentlySelectedItem != null)
        {
            Debug.Log($"Attempting to change <b>{currentlySelectedItem.ItemName}</b>'s value from {currentlySelectedItem.Value} to {newValue}.");
            currentlySelectedItem.Value = newValue; // This permanently changes the asset!
            Debug.Log($"<b>{currentlySelectedItem.ItemName}</b>'s value is now {currentlySelectedItem.Value}. This change is saved to the asset!");
        }
        else
        {
            Debug.LogWarning("No item selected to change value.");
        }
    }

    /// <summary>
    /// Adds an item to the inventory list.
    /// </summary>
    /// <param name="itemToAdd">The GameItem ScriptableObject to add.</param>
    public void AddItemToInventory(GameItem itemToAdd)
    {
        if (itemToAdd != null)
        {
            inventoryItems.Add(itemToAdd);
            Debug.Log($"Added <color=lime>{itemToAdd.ItemName}</color> to inventory.");
        }
    }

    #endregion
}
```

---

### How to Use This Example in Unity:

1.  **Create C# Scripts**:
    *   Create a new C# script named `GameItem.cs` and copy the contents of the first script into it.
    *   Create a new C# script named `ConsumableItem.cs` and copy the contents of the second script into it.
    *   Create a new C# script named `InventoryManager.cs` and copy the contents of the third script into it.

2.  **Create ScriptableObject Assets**:
    *   In your Unity Project window, right-click (or go to `Assets -> Create`).
    *   You will now see a new submenu: `Scriptable Objects`.
    *   Select `Scriptable Objects -> Game Item`. Name it `SwordItem`.
    *   Repeat, selecting `Scriptable Objects -> Game Item`. Name it `ShieldItem`.
    *   Repeat, selecting `Scriptable Objects -> Consumable Item`. Name it `HealthPotion`.

3.  **Configure ScriptableObject Assets**:
    *   Select `SwordItem` in your Project window. In the Inspector, fill in its details (e.g., Name: "Mighty Sword", Description: "A legendary blade.", Value: 100, Type: Weapon). You can optionally assign a `Sprite` if you have one.
    *   Select `ShieldItem`. Fill in its details (e.g., Name: "Wooden Shield", Description: "A basic shield.", Value: 50, Type: Armor).
    *   Select `HealthPotion`. Fill in its details (e.g., Name: "Greater Health Potion", Description: "Restores a good amount of health.", Value: 25, Type: Potion). Notice it has extra fields (`healthRestored`, `manaRestored`) specific to `ConsumableItem`. Set `healthRestored` to, say, 50.

4.  **Create a GameObject to Consume the Assets**:
    *   In your Hierarchy window, right-click and select `Create Empty`. Name it `GameManager` or `InventorySystem`.
    *   Select this new GameObject. In the Inspector, click `Add Component` and search for `Inventory Manager` to attach the script.

5.  **Assign ScriptableObjects to the `InventoryManager`**:
    *   With the `GameManager` (or `InventorySystem`) GameObject still selected in the Hierarchy, look at its `Inventory Manager` component in the Inspector.
    *   You will see a field `Currently Selected Item` and a `Inventory Items` list.
    *   **For `Currently Selected Item`**: Drag the `HealthPotion` asset from your Project window into this field.
    *   **For `Inventory Items` list**: Expand the `Inventory Items` list, set its size to 3. Then, drag `SwordItem`, `ShieldItem`, and `HealthPotion` into the respective element slots. (You can drag the same `HealthPotion` asset here again, demonstrating how multiple references point to the *same* data.)

6.  **Run the Scene**:
    *   Press the Play button in the Unity Editor.
    *   Observe the Console window. You will see `Debug.Log` messages from the `InventoryManager` displaying the details of the selected item and its inventory.
    *   Press the 'U' key (configured in `Update` in `InventoryManager.cs`) to call the `Use()` method of the `HealthPotion`. Notice the specialized message from `ConsumableItem`.
    *   Press the 'C' key. This will randomly change the `Value` of the `HealthPotion`. Exit Play Mode. You will notice that the `Value` of your `HealthPotion` asset in the Project window has **permanently changed**, demonstrating that ScriptableObjects are assets that persist data changes even outside of Play Mode.

This setup clearly illustrates how ScriptableObjects serve as powerful, reusable data containers that separate data definitions from runtime logic, improving organization, memory efficiency, and workflow in Unity projects.