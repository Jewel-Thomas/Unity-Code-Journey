// Unity Design Pattern Example: InventorySystemPattern
// This script demonstrates the InventorySystemPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The "Inventory System Pattern" in game development isn't a single GoF (Gang of Four) design pattern, but rather an architectural solution that often combines several patterns to create a robust, modular, and extensible item management system.

This example demonstrates a practical inventory system in Unity by incorporating:

1.  **Scriptable Objects:** For defining unique item data (ItemData, ConsumableItemData), separating data from logic. This promotes data-driven design, reusability, and easy content creation.
2.  **Singleton:** For the `InventoryManager`, ensuring a single, globally accessible instance that manages all inventory operations.
3.  **Observer/Publish-Subscribe:** Using C# events (`OnInventoryChanged`) to notify UI elements or other systems whenever the inventory's state changes. This decouples the inventory logic from its display.
4.  **Inheritance/Polymorphism:** Allowing different types of items (e.g., `ConsumableItemData`) to inherit from a base `ItemData` and provide specific functionality (`Use` method overrides).

---

### **How to Use This Example in Unity:**

1.  **Create C# Scripts:**
    *   Create five new C# scripts in your Unity project (e.g., in a `Scripts` folder):
        *   `ItemData.cs`
        *   `InventorySlot.cs`
        *   `InventoryManager.cs`
        *   `ConsumableItemData.cs`
        *   `InventoryUIUpdater.cs`
        *   `ExampleInventoryUsage.cs`
    *   Copy and paste the code for each script into its respective file.

2.  **Create Inventory Manager GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., rename it to `_InventoryManager`).
    *   Attach the `InventoryManager.cs` script to this GameObject.

3.  **Create UI Updater GameObject (Optional but Recommended):**
    *   Create another empty GameObject (e.g., rename it to `_InventoryUIUpdater`).
    *   Attach the `InventoryUIUpdater.cs` script to this GameObject. This will log inventory changes to the console.

4.  **Create Example Usage GameObject:**
    *   Create an empty GameObject (e.g., rename it to `_ExampleInventoryUsage`).
    *   Attach the `ExampleInventoryUsage.cs` script to this GameObject.

5.  **Create Item ScriptableObject Assets:**
    *   In your Project window, right-click -> Create -> Inventory -> Item Data.
    *   Rename it to `SwordData`. Set `ItemName` to "Sword", `MaxStackSize` to 1. Optionally assign an icon.
    *   Right-click -> Create -> Inventory -> Item Data.
    *   Rename it to `CoinData`. Set `ItemName` to "Coin", `MaxStackSize` to 99. Optionally assign an icon.
    *   Right-click -> Create -> Inventory -> Consumable Item Data.
    *   Rename it to `HealthPotionData`. Set `ItemName` to "Health Potion", `HealthRestored` to 25. (Note: `MaxStackSize` defaults to 5 in `ConsumableItemData`'s constructor).
    *   Right-click -> Create -> Inventory -> Consumable Item Data.
    *   Rename it to `ManaPotionData`. Set `ItemName` to "Mana Potion", `ManaRestored` to 20.

6.  **Assign Test Items:**
    *   Select the `_ExampleInventoryUsage` GameObject in your scene.
    *   Drag and drop the `HealthPotionData`, `SwordData`, `ManaPotionData`, and `CoinData` assets from your Project window into the corresponding slots (`healthPotion`, `sword`, `manaPotion`, `coin`) in the Inspector.

7.  **Run and Test:**
    *   Play the scene.
    *   Select the `_ExampleInventoryUsage` GameObject.
    *   In the Inspector, you'll see buttons created by `[ContextMenu]`. Click them to add, remove, and use items.
    *   Observe the Console output from `InventoryUIUpdater` and the `Debug.Log` messages from the `InventoryManager` and `ItemData` classes, demonstrating the system's behavior.

---

### **1. `ItemData.cs`**
This ScriptableObject defines the base properties and behavior for any item.

```csharp
using UnityEngine;
using System; // For Guid

/// <summary>
/// [InventorySystemPattern] - ItemData (ScriptableObject)
///
/// This ScriptableObject serves as the definition for any item in the game.
/// Using ScriptableObjects allows us to:
/// 1. Create unique item instances (assets) in the Unity Editor.
/// 2. Decouple item data from runtime game objects, making items reusable and data-driven.
/// 3. Store common properties like name, description, icon, and stackability.
///
/// This is a core component of a flexible inventory system, enabling data-driven item definitions.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Identification")]
    [Tooltip("Unique ID for this item. Can be a GUID or a simple string/int. Generated on asset creation.")]
    // Using Guid.NewGuid() here ensures a unique ID when a new asset is created.
    // Be aware that duplicating the asset will duplicate the GUID; for a production system,
    // you might want a custom editor to generate new GUIDs on duplication or a more robust ID system.
    public string ItemID = Guid.NewGuid().ToString();
    public string ItemName = "New Item";
    [TextArea(3, 5)]
    public string Description = "A generic item.";

    [Header("Visuals")]
    public Sprite Icon;

    [Header("Inventory Properties")]
    [Tooltip("Maximum number of this item that can be stacked in a single inventory slot. Default is 1 for non-stackable.")]
    public int MaxStackSize = 1; // Default to 1 for non-stackable items (e.g., equipment)

    /// <summary>
    /// [InventorySystemPattern] - Item Usage (Polymorphism)
    ///
    /// This virtual method defines what happens when an item is "used".
    /// Subclasses (e.g., ConsumableItemData, EquipmentItemData) can override this
    /// to provide specific functionality without the InventoryManager needing to know
    /// the exact type of item (loose coupling).
    ///
    /// The InventoryManager will call this method.
    /// </summary>
    /// <param name="user">The GameObject that is using this item (e.g., the player).</param>
    /// <returns>True if the item was successfully used, false otherwise.</returns>
    public virtual bool Use(GameObject user)
    {
        if (user == null)
        {
            Debug.LogError($"[InventorySystemPattern] Cannot use {ItemName}: user is null.");
            return false;
        }

        Debug.Log($"[InventorySystemPattern] {ItemName} was used by {user.name}. (Base ItemData usage: No specific effect)");
        // Base item might just play a generic sound, show a message, or do nothing.
        // Derived classes will implement specific effects like restoring health, equipping, etc.
        return true; // Assume success for base item, it just means it "could" be used.
    }

    /// <summary>
    /// Overrides ToString for easier debugging in console or UI.
    /// </summary>
    public override string ToString()
    {
        return $"[ItemData] ID: {ItemID}, Name: {ItemName}";
    }
}
```

---

### **2. `InventorySlot.cs`**
A simple serializable class representing a single container in the inventory.

```csharp
using UnityEngine;
using System; // For [Serializable]

/// <summary>
/// [InventorySystemPattern] - InventorySlot
///
/// A plain C# class that represents a single slot within the inventory.
/// It holds a reference to the ItemData and the quantity of that item.
///
/// Marked as [Serializable] so it can be viewed and edited in the Unity Inspector
/// when used within a MonoBehaviour or ScriptableObject (e.g., in InventoryManager).
/// This allows for easy debugging and initial setup within the editor.
/// </summary>
[Serializable]
public class InventorySlot
{
    // [SerializeField] is used here to make private fields visible in the Inspector
    // without making them public, adhering to encapsulation best practices.
    [SerializeField]
    private ItemData _itemData;
    [SerializeField]
    private int _quantity;

    /// <summary>
    /// Reference to the ItemData stored in this slot.
    /// </summary>
    public ItemData ItemData => _itemData;

    /// <summary>
    /// Current quantity of the item in this slot.
    /// </summary>
    public int Quantity => _quantity;

    /// <summary>
    /// Constructor for creating a new inventory slot.
    /// </summary>
    /// <param name="item">The ItemData to store in the slot.</param>
    /// <param name="quantity">The initial quantity of the item.</param>
    public InventorySlot(ItemData item, int quantity)
    {
        SetSlot(item, quantity);
    }

    /// <summary>
    /// Checks if the slot is empty (no item or zero quantity).
    /// </summary>
    public bool IsEmpty => _itemData == null || _quantity <= 0;

    /// <summary>
    /// Adds a specified quantity to the slot.
    /// Handles max stack size defined by the ItemData.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The quantity that was unable to be added (overflow).</returns>
    public int AddQuantity(int amount)
    {
        if (_itemData == null) return amount; // Cannot add to an empty slot without defining an item first.
        if (amount <= 0) return 0;

        int spaceAvailable = _itemData.MaxStackSize - _quantity;
        if (spaceAvailable >= amount)
        {
            _quantity += amount;
            return 0; // All added successfully
        }
        else
        {
            _quantity += spaceAvailable;
            return amount - spaceAvailable; // Return the remainder that couldn't fit
        }
    }

    /// <summary>
    /// Removes a specified quantity from the slot.
    /// </summary>
    /// <param name="amount">The amount to remove.</param>
    /// <returns>True if removal was successful (enough items were present), false otherwise.</returns>
    public bool RemoveQuantity(int amount)
    {
        if (_itemData == null || amount <= 0 || _quantity < amount)
        {
            return false;
        }

        _quantity -= amount;
        if (_quantity <= 0)
        {
            _itemData = null; // Clear the slot if quantity drops to zero or less
            _quantity = 0;
        }
        return true;
    }

    /// <summary>
    /// Sets the item data and quantity, effectively filling or replacing the slot's content.
    /// </summary>
    /// <param name="item">The ItemData to set. Can be null to clear the slot.</param>
    /// <param name="quantity">The quantity to set. Should be 0 if item is null.</param>
    public void SetSlot(ItemData item, int quantity)
    {
        _itemData = item;
        _quantity = item != null ? Mathf.Clamp(quantity, 0, item.MaxStackSize) : 0;
        if (_itemData == null) _quantity = 0; // Ensure quantity is 0 if item is null
    }

    /// <summary>
    /// Clears the slot, setting item to null and quantity to 0.
    /// </summary>
    public void ClearSlot()
    {
        _itemData = null;
        _quantity = 0;
    }
}
```

---

### **3. `InventoryManager.cs`**
The core manager, implementing the Singleton and Observer patterns.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action

/// <summary>
/// [InventorySystemPattern] - InventoryManager (Singleton & Observer Subject)
///
/// This class serves as the central manager for the player's inventory.
/// It implements the Singleton pattern to ensure there's only one instance
/// throughout the game, providing a global point of access for all inventory operations.
///
/// Key features:
/// - Manages a collection of InventorySlots.
/// - Provides methods for adding, removing, checking, and using items.
/// - Uses a C# Event (OnInventoryChanged) to notify other systems (e.g., UI)
///   whenever the inventory state changes, following the Observer pattern. This
///   decouples the inventory logic from its presentation.
/// - Designed to be robust and handle item stacking.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // [InventorySystemPattern] - Singleton Implementation
    // Ensures only one instance of the InventoryManager exists.
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    [Tooltip("The initial number of inventory slots.")]
    [SerializeField] private int _inventorySize = 16;

    // [InventorySystemPattern] - Inventory Storage
    // Using a List of InventorySlot objects to manage individual slots.
    // This allows for fixed-size inventories and easier UI mapping.
    [SerializeField]
    private List<InventorySlot> _inventorySlots = new List<InventorySlot>();

    // [InventorySystemPattern] - Event System (Observer Pattern)
    // An event that other classes can subscribe to.
    // This notifies all listeners whenever the inventory's state changes,
    // allowing UIs or other game systems to react.
    public event Action OnInventoryChanged;

    private void Awake()
    {
        // [InventorySystemPattern] - Singleton Enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[InventorySystemPattern] Duplicate InventoryManager detected. Destroying this duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make it persistent across scene loads:
            // DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
    }

    /// <summary>
    /// Initializes the inventory with empty slots based on _inventorySize.
    /// </summary>
    private void InitializeInventory()
    {
        _inventorySlots.Clear();
        for (int i = 0; i < _inventorySize; i++)
        {
            _inventorySlots.Add(new InventorySlot(null, 0)); // Add empty slots
        }
        Debug.Log($"[InventorySystemPattern] Inventory initialized with {_inventorySize} slots.");
        OnInventoryChanged?.Invoke(); // Notify on initialization as well
    }

    /// <summary>
    /// [InventorySystemPattern] - Add Item Functionality
    /// Attempts to add a specified quantity of an item to the inventory.
    /// Handles stacking with existing items and finding empty slots.
    /// </summary>
    /// <param name="itemData">The ItemData to add.</param>
    /// <param name="quantity">The quantity to add (defaults to 1).</param>
    /// <returns>True if at least one item was added, false if no items could be added.</returns>
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("[InventorySystemPattern] Attempted to add null item or non-positive quantity.");
            return false;
        }

        bool itemAdded = false;
        int quantityRemaining = quantity;

        // 1. Try to stack with existing items (if stackable)
        if (itemData.MaxStackSize > 1)
        {
            foreach (InventorySlot slot in _inventorySlots)
            {
                // If the slot has the same item and isn't full
                if (slot.ItemData == itemData && slot.Quantity < itemData.MaxStackSize)
                {
                    quantityRemaining = slot.AddQuantity(quantityRemaining); // Add to slot, get remaining
                    itemAdded = true;
                    if (quantityRemaining == 0) break; // All quantity added
                }
            }
        }

        // 2. If there's still quantity remaining, find empty slots
        if (quantityRemaining > 0)
        {
            foreach (InventorySlot slot in _inventorySlots)
            {
                if (slot.IsEmpty)
                {
                    // Add up to MaxStackSize to the empty slot
                    int quantityToAdd = Mathf.Min(quantityRemaining, itemData.MaxStackSize);
                    slot.SetSlot(itemData, quantityToAdd);
                    quantityRemaining -= quantityToAdd;
                    itemAdded = true;
                    if (quantityRemaining == 0) break; // All quantity added
                }
            }
        }

        if (itemAdded)
        {
            Debug.Log($"[InventorySystemPattern] Added {quantity - quantityRemaining}x {itemData.ItemName}. Remaining: {quantityRemaining}.");
            OnInventoryChanged?.Invoke(); // Notify listeners of the change
        }
        else
        {
            Debug.Log($"[InventorySystemPattern] Could not add {itemData.ItemName}. Inventory full or no space for stacking.");
        }
        return itemAdded;
    }

    /// <summary>
    /// [InventorySystemPattern] - Remove Item Functionality
    /// Attempts to remove a specified quantity of an item from the inventory.
    /// Removes from multiple slots if necessary.
    /// </summary>
    /// <param name="itemData">The ItemData to remove.</param>
    /// <param name="quantity">The quantity to remove (defaults to 1).</param>
    /// <returns>True if all specified items were removed, false otherwise.</returns>
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("[InventorySystemPattern] Attempted to remove null item or non-positive quantity.");
            return false;
        }

        // First, check if we actually have enough of the item
        if (GetItemQuantity(itemData) < quantity)
        {
            Debug.Log($"[InventorySystemPattern] Not enough {itemData.ItemName} to remove {quantity}. (Have: {GetItemQuantity(itemData)})");
            return false;
        }

        int quantityToRemove = quantity;
        for (int i = _inventorySlots.Count - 1; i >= 0; i--) // Iterate backwards to safely remove/clear slots
        {
            InventorySlot slot = _inventorySlots[i];
            if (slot.ItemData == itemData)
            {
                if (slot.Quantity >= quantityToRemove)
                {
                    slot.RemoveQuantity(quantityToRemove);
                    quantityToRemove = 0;
                    break; // All removed
                }
                else
                {
                    quantityToRemove -= slot.Quantity;
                    slot.ClearSlot(); // Remove all from this slot
                }
            }
        }

        if (quantityToRemove == 0)
        {
            Debug.Log($"[InventorySystemPattern] Removed {quantity}x {itemData.ItemName}.");
            OnInventoryChanged?.Invoke(); // Notify listeners
            return true;
        }
        else
        {
            Debug.LogError("[InventorySystemPattern] Logic error in RemoveItem: Should not reach here if GetItemQuantity check passed.");
            return false;
        }
    }

    /// <summary>
    /// [InventorySystemPattern] - Item Lookup/Check
    /// Checks if the inventory contains a specified quantity of an item.
    /// </summary>
    /// <param name="itemData">The ItemData to check for.</param>
    /// <param name="quantity">The minimum quantity required (defaults to 1).</param>
    /// <returns>True if the item (and quantity) is found, false otherwise.</returns>
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0) return false;
        return GetItemQuantity(itemData) >= quantity;
    }

    /// <summary>
    /// Gets the total quantity of a specific item across all slots.
    /// </summary>
    /// <param name="itemData">The ItemData to count.</param>
    /// <returns>The total quantity of the item.</returns>
    public int GetItemQuantity(ItemData itemData)
    {
        if (itemData == null) return 0;
        int count = 0;
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (slot.ItemData == itemData)
            {
                count += slot.Quantity;
            }
        }
        return count;
    }

    /// <summary>
    /// [InventorySystemPattern] - Use Item Functionality (Polymorphic Call)
    /// Attempts to use an item from a specific inventory slot.
    /// The actual effect is handled by the item's own `Use` method.
    /// If the item is a `ConsumableItemData` (derived type), one quantity will be removed.
    /// This demonstrates how the InventoryManager can interact with item-specific logic.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to use the item from.</param>
    /// <param name="user">The GameObject that is using this item (e.g., the player).</param>
    /// <returns>True if the item was successfully used, false otherwise.</returns>
    public bool UseItem(int slotIndex, GameObject user)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Count)
        {
            Debug.LogWarning($"[InventorySystemPattern] Invalid slot index: {slotIndex}.");
            return false;
        }

        InventorySlot slot = _inventorySlots[slotIndex];
        if (slot.IsEmpty)
        {
            Debug.Log($"[InventorySystemPattern] Slot {slotIndex} is empty. Cannot use item.");
            return false;
        }

        ItemData itemToUse = slot.ItemData;
        bool wasUsed = itemToUse.Use(user); // Calls the virtual Use method (polymorphism)

        if (wasUsed)
        {
            // [InventorySystemPattern] - Item Consumption Logic
            // If the item successfully used and is of a `ConsumableItemData` type, decrease its quantity.
            // This is a common pattern: the InventoryManager decides how to handle an item AFTER it's used,
            // based on its type or properties.
            if (itemToUse is ConsumableItemData)
            {
                slot.RemoveQuantity(1);
                Debug.Log($"[InventorySystemPattern] Used and consumed 1x {itemToUse.ItemName} from slot {slotIndex}.");
            }
            else
            {
                Debug.Log($"[InventorySystemPattern] Used {itemToUse.ItemName} from slot {slotIndex}. (Not consumable, not removed)");
            }

            OnInventoryChanged?.Invoke(); // Notify listeners as the inventory state might have changed (e.g., item count decreased)
            return true;
        }
        else
        {
            Debug.LogWarning($"[InventorySystemPattern] Failed to use {itemToUse.ItemName} from slot {slotIndex}.");
            return false;
        }
    }

    /// <summary>
    /// Gets a read-only list of inventory slots.
    /// Useful for UI to display inventory contents without directly modifying it.
    /// </summary>
    public IReadOnlyList<InventorySlot> GetInventorySlots()
    {
        return _inventorySlots;
    }

    /// <summary>
    /// Resizes the inventory, adding or removing slots as needed.
    /// Handles removing items if slots are lost due to shrinking.
    /// </summary>
    /// <param name="newSize">The new desired size of the inventory.</param>
    public void ResizeInventory(int newSize)
    {
        if (newSize < 0)
        {
            Debug.LogError("[InventorySystemPattern] Inventory size cannot be negative.");
            return;
        }

        if (newSize == _inventorySize)
        {
            Debug.Log("[InventorySystemPattern] Inventory size is already " + newSize + ". No resize needed.");
            return;
        }

        Debug.Log($"[InventorySystemPattern] Resizing inventory from {_inventorySize} to {newSize}.");

        // If new size is smaller, handle potential item loss
        if (newSize < _inventorySize)
        {
            // Simple removal for this example. A real game might drop items on the ground
            // or transfer them to another inventory if slots are lost.
            for (int i = _inventorySize - 1; i >= newSize; i--)
            {
                if (!_inventorySlots[i].IsEmpty)
                {
                    Debug.LogWarning($"[InventorySystemPattern] Item {_inventorySlots[i].ItemData.ItemName} " +
                                     $"({_inventorySlots[i].Quantity}) lost from slot {i} due to inventory resize to {newSize}.");
                }
                _inventorySlots.RemoveAt(i);
            }
        }
        // If new size is larger, add empty slots
        else
        {
            for (int i = _inventorySize; i < newSize; i++)
            {
                _inventorySlots.Add(new InventorySlot(null, 0));
            }
        }

        _inventorySize = newSize;
        OnInventoryChanged?.Invoke(); // Notify listeners of the size change
        Debug.Log($"[InventorySystemPattern] Inventory resized to {newSize} slots.");
    }
}
```

---

### **4. `ConsumableItemData.cs`**
An example of extending `ItemData` to create a specific item type.

```csharp
using UnityEngine;

/// <summary>
/// [InventorySystemPattern] - ConsumableItemData (Derived ScriptableObject)
///
/// This class extends ItemData to represent consumable items like potions, food, etc.
/// It overrides the Use method to provide specific consumption logic.
///
/// This demonstrates how to extend the item system to handle different item types
/// using inheritance and polymorphism with Scriptable Objects, making the system
/// highly extensible without modifying the core InventoryManager logic for each new item type.
/// </summary>
[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Inventory/Consumable Item Data")]
public class ConsumableItemData : ItemData
{
    [Header("Consumable Properties")]
    [Tooltip("Amount of health restored by this consumable.")]
    public int HealthRestored = 20;
    [Tooltip("Amount of mana restored by this consumable.")]
    public int ManaRestored = 0;

    /// <summary>
    /// Constructor for ConsumableItemData.
    /// Sets a default MaxStackSize for consumables (overriding the ItemData default of 1).
    /// </summary>
    public ConsumableItemData()
    {
        MaxStackSize = 5; // Consumables typically stack
    }

    /// <summary>
    /// Overrides the base Use method to apply consumable-specific effects.
    /// This is where the item's unique functionality is defined.
    /// </summary>
    /// <param name="user">The GameObject that is using this item.</param>
    /// <returns>True if the item was successfully used, false otherwise.</returns>
    public override bool Use(GameObject user)
    {
        if (user == null)
        {
            Debug.LogError($"[InventorySystemPattern] Cannot use {ItemName}: user is null.");
            return false;
        }

        // Example: Apply effects to the user.
        // In a real game, 'user' would likely have a Health/Mana component
        // that these values would interact with (e.g., user.GetComponent<PlayerHealth>().Restore(HealthRestored);).
        // For this example, we just log the effect.
        string effects = "";
        if (HealthRestored > 0)
        {
            effects += $"Restored {HealthRestored} HP. ";
            // Example: user.GetComponent<PlayerHealth>()?.RestoreHealth(HealthRestored);
        }
        if (ManaRestored > 0)
        {
            effects += $"Restored {ManaRestored} MP. ";
            // Example: user.GetComponent<PlayerMana>()?.RestoreMana(ManaRestored);
        }

        Debug.Log($"[InventorySystemPattern] {ItemName} (Consumable) used by {user.name}. {effects}");

        // Call base.Use() if some base functionality should also occur, though often not needed for specific consumables.
        // base.Use(user);

        return true; // Indicate that the item was successfully used
    }
}
```

---

### **5. `InventoryUIUpdater.cs`**
An example observer that reacts to inventory changes.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// [InventorySystemPattern] - InventoryUIUpdater (Observer Example)
///
/// This class demonstrates how a UI component (or any other system)
/// can observe and react to changes in the InventoryManager.
///
/// It subscribes to the InventoryManager's OnInventoryChanged event.
/// When the event is triggered, it updates its display (in this case, logs the inventory
/// contents to the console). This adheres to the Observer pattern: InventoryManager
/// is the Subject, InventoryUIUpdater is an Observer. This decoupling means the
/// InventoryManager doesn't need to know anything about the UI.
/// </summary>
public class InventoryUIUpdater : MonoBehaviour
{
    void OnEnable()
    {
        // [InventorySystemPattern] - Subscribe to Event
        // It's crucial to check if the InventoryManager.Instance is not null before subscribing,
        // especially if this script might awake before the InventoryManager.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryDisplay;
            Debug.Log("[InventorySystemPattern] InventoryUIUpdater subscribed to OnInventoryChanged.");
            // Also call once on enable to show initial state
            UpdateInventoryDisplay();
        }
        else
        {
            Debug.LogWarning("[InventorySystemPattern] InventoryManager instance not found on OnEnable. " +
                             "Ensure InventoryManager initializes before InventoryUIUpdater. " +
                             "Delayed subscription or execution order might be needed in complex scenarios.");
        }
    }

    void OnDisable()
    {
        // [InventorySystemPattern] - Unsubscribe from Event
        // Always unsubscribe to prevent memory leaks and null reference exceptions
        // if the InventoryManager is destroyed before this object, or if this object
        // is disabled/destroyed.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryDisplay;
            Debug.Log("[InventorySystemPattern] InventoryUIUpdater unsubscribed from OnInventoryChanged.");
        }
    }

    /// <summary>
    /// [InventorySystemPattern] - Event Handler
    /// This method is called by the InventoryManager when the inventory changes.
    /// In a real UI, this would iterate through slots and update visual elements
    /// (e.g., slot icons, quantity texts). For this example, it just logs to the console.
    /// </summary>
    private void UpdateInventoryDisplay()
    {
        Debug.Log("--- [InventorySystemPattern] Inventory Display Updated ---");
        // Access the inventory slots via the Singleton instance.
        IReadOnlyList<InventorySlot> slots = InventoryManager.Instance.GetInventorySlots();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty)
            {
                Debug.Log($"Slot {i:00}: Empty");
            }
            else
            {
                Debug.Log($"Slot {i:00}: {slot.ItemData.ItemName} x{slot.Quantity}");
                // In a real UI, you would update a UI image, text, etc.
                // For example:
                // UIManager.UpdateSlotIcon(i, slot.ItemData.Icon);
                // UIManager.UpdateSlotText(i, slot.Quantity.ToString());
            }
        }
        Debug.Log("-------------------------------------------------");
    }
}
```

---

### **6. `ExampleInventoryUsage.cs`**
A demonstration script to simulate player interaction.

```csharp
using UnityEngine;

/// <summary>
/// [InventorySystemPattern] - ExampleInventoryUsage
///
/// This script demonstrates how other game systems (e.g., player controller, item pickups, shop interactions)
/// would interact with the InventoryManager's public API.
///
/// It uses a few placeholder ItemData ScriptableObjects (assigned in the Inspector)
/// to simulate adding, removing, and using items via button presses in the editor's Inspector
/// using `[ContextMenu]` attributes. This provides a quick way to test the inventory system.
/// </summary>
public class ExampleInventoryUsage : MonoBehaviour
{
    [Header("Test Items (Assign in Inspector)")]
    public ItemData healthPotion;   // Standard ItemData
    public ItemData sword;          // Standard ItemData (non-stackable equipment example)
    public ConsumableItemData manaPotion; // Specific ConsumableItemData (derived type)
    public ItemData coin;           // Stackable ItemData

    [Header("Inventory Interaction (Press buttons in Inspector)")]
    [Tooltip("Set the index of the slot you want to interact with for 'Use Item' context menu.")]
    public int testSlotIndex = 0; // For using item from a specific slot

    [ContextMenu("Add Health Potion (x1)")]
    public void AddHealthPotion()
    {
        // Always check if the manager exists before trying to use it.
        // This is good practice for Singletons.
        if (InventoryManager.Instance != null && healthPotion != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to add 1x {healthPotion.ItemName}...");
            InventoryManager.Instance.AddItem(healthPotion);
        }
    }

    [ContextMenu("Add Health Potion (x5)")]
    public void AddHealthPotionFive()
    {
        if (InventoryManager.Instance != null && healthPotion != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to add 5x {healthPotion.ItemName}...");
            InventoryManager.Instance.AddItem(healthPotion, 5);
        }
    }

    [ContextMenu("Add Sword")]
    public void AddSword()
    {
        if (InventoryManager.Instance != null && sword != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to add 1x {sword.ItemName}...");
            InventoryManager.Instance.AddItem(sword);
        }
    }

    [ContextMenu("Add Mana Potion (x1)")]
    public void AddManaPotion()
    {
        if (InventoryManager.Instance != null && manaPotion != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to add 1x {manaPotion.ItemName}...");
            InventoryManager.Instance.AddItem(manaPotion);
        }
    }

    [ContextMenu("Add Coins (x10)")]
    public void AddCoins()
    {
        if (InventoryManager.Instance != null && coin != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to add 10x {coin.ItemName}...");
            InventoryManager.Instance.AddItem(coin, 10);
        }
    }

    [ContextMenu("Remove Health Potion (x1)")]
    public void RemoveHealthPotion()
    {
        if (InventoryManager.Instance != null && healthPotion != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to remove 1x {healthPotion.ItemName}...");
            InventoryManager.Instance.RemoveItem(healthPotion);
        }
    }

    [ContextMenu("Use Item From Test Slot")]
    public void UseItemFromTestSlot()
    {
        if (InventoryManager.Instance != null)
        {
            Debug.Log($"[ExampleUsage] Attempting to use item from slot {testSlotIndex}...");
            // Pass 'this.gameObject' as the user for the item's Use method,
            // so items can interact with the entity that used them (e.g., restore player health).
            InventoryManager.Instance.UseItem(testSlotIndex, this.gameObject);
        }
    }

    [ContextMenu("Check for Sword")]
    public void CheckForSword()
    {
        if (InventoryManager.Instance != null && sword != null)
        {
            if (InventoryManager.Instance.HasItem(sword))
            {
                Debug.Log($"[InventorySystemPattern] Player has a {sword.ItemName}.");
            }
            else
            {
                Debug.Log($"[InventorySystemPattern] Player does NOT have a {sword.ItemName}.");
            }
        }
    }

    [ContextMenu("Check Total Coins")]
    public void CheckTotalCoins()
    {
        if (InventoryManager.Instance != null && coin != null)
        {
            int totalCoins = InventoryManager.Instance.GetItemQuantity(coin);
            Debug.Log($"[InventorySystemPattern] Player has {totalCoins} {coin.ItemName}(s).");
        }
    }

    [ContextMenu("Resize Inventory to 5 Slots")]
    public void ResizeToFive()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResizeInventory(5);
        }
    }

    [ContextMenu("Resize Inventory to 20 Slots")]
    public void ResizeToTwenty()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResizeInventory(20);
        }
    }
}
```