// Unity Design Pattern Example: ShopInventorySystem
// This script demonstrates the ShopInventorySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust 'ShopInventorySystem' pattern in Unity. It's designed to be modular, extensible, and easy to integrate into your projects. The system separates concerns into Item Data, core Inventory logic, Player-specific inventory, Shop-specific inventory, and a central Transaction Manager.

---

### **Shop Inventory System Design Pattern Explanation**

The "Shop Inventory System" isn't a formal GoF (Gang of Four) design pattern, but rather an architectural pattern commonly used in games. It focuses on structuring the various components involved in item storage, management, and transactions (buying/selling) between a player and a shop.

**Core Principles:**

1.  **Data-Driven Items (ItemData):** Items are defined as ScriptableObjects, allowing game designers to create and configure new items without modifying code. This promotes flexibility and content creation.
2.  **Generic Inventory (Inventory):** A reusable core class that manages a collection of items (and their quantities). This class is independent of who owns the inventory (player, shop, chest) and provides fundamental operations like adding, removing, and querying items.
3.  **Specific Inventory Owners (PlayerInventory, ShopInventory):** MonoBehaviours that encapsulate a generic `Inventory` instance and add owner-specific properties (e.g., player currency, shop currency, initial stock). They act as facades or wrappers around the generic inventory for external interaction.
4.  **Transaction Manager (ShopManager):** A central service responsible for orchestrating buying and selling operations. It acts as the "controller" in the MVC sense for transactions, ensuring all business rules (e.g., enough money, enough stock) are met and performing the actual item and currency transfers between the player and the shop. This decouples the player and shop from direct transaction logic.
5.  **Events for UI/System Updates:** Using C# events (`Action`, `Action<T>`) allows UI elements or other game systems to react dynamically when inventories or currencies change, promoting loose coupling.

---

### **C# Unity Code Implementation**

Here are the complete C# scripts. Follow the setup instructions below to get them running in Unity.

#### 1. `ItemData.cs` (ScriptableObject for Item Definition)

This defines the properties of a single item type.

```csharp
using UnityEngine;

/// <summary>
/// Scriptable Object representing a single type of item in the game.
/// This allows us to define item properties like name, cost, description, etc.,
/// independently of specific instances in the game world or inventory.
/// Using ScriptableObjects makes items easily configurable by game designers
/// without touching code, and they can be created as assets in the Unity project.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Shop/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Item Info")]
    public string ItemName = "New Item";
    [TextArea(3, 5)]
    public string Description = "A generic item.";
    public Sprite Icon; // Visual representation of the item, for UI.

    [Header("Gameplay Properties")]
    public float BaseCost = 10f; // The base cost to buy from shop (or sell price to shop).
                                 // This can be adjusted by game balance or shop modifiers.
    public int MaxStackSize = 1; // How many of this item can stack in a single inventory slot.
                                 // 1 for non-stackable items like equipment, >1 for consumables like potions.

    /// <summary>
    /// Provides a unique identifier for the item.
    /// In a real game, this might be a GUID, an integer ID from a database,
    /// or a string ID ensuring uniqueness. For simplicity, we use the asset's name.
    /// </summary>
    public string ID => name; 
}
```

#### 2. `InventorySlot.cs` (Struct for Item + Quantity)

A simple struct to hold an item reference and its current quantity within an inventory.

```csharp
using System; // For [Serializable]

/// <summary>
/// Represents a single slot within an inventory, holding an ItemData and its quantity.
/// This struct is used by the generic Inventory class to manage individual stacks or items.
/// Marking it [Serializable] allows Unity to display and save instances of this struct
/// within MonoBehaviour fields (e.g., for initial stock setup in the Inspector).
/// </summary>
[Serializable]
public struct InventorySlot
{
    public ItemData Item;
    public int Quantity;

    public InventorySlot(ItemData item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }
}
```

#### 3. `Inventory.cs` (Core Inventory Logic)

This is the heart of the inventory system. It's a plain C# class, making it reusable by any component (Player, Shop, Chest, etc.) without coupling it to a GameObject.

```csharp
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like Sum and Where

/// <summary>
/// A core inventory system, managing a collection of ItemData and their quantities.
/// This class is designed to be reusable for player inventories, shop inventories, chests, etc.
/// It's a plain C# class, meaning it doesn't derive from MonoBehaviour, making it flexible
/// and easily embedded into other components.
/// Marking it [Serializable] allows Unity to display and save instances of this class
/// if it's used as a field within a MonoBehaviour.
/// </summary>
[Serializable]
public class Inventory
{
    // The actual storage for inventory items.
    // We use a List<InventorySlot> to maintain order (useful for UI display)
    // and easily iterate through items.
    // For very large inventories with extremely frequent lookups by ID,
    // a Dictionary<string, InventorySlot> might offer faster access.
    private List<InventorySlot> _slots = new List<InventorySlot>();

    /// <summary>
    /// Event fired when the inventory changes (item added, removed, quantity changed).
    /// This allows UI systems, saving systems, or other game logic to react to updates.
    /// </summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// Gets a read-only list of the current inventory slots.
    /// This prevents external classes from directly modifying the internal _slots list.
    /// </summary>
    public IReadOnlyList<InventorySlot> Slots => _slots;

    /// <summary>
    /// Adds an item to the inventory, stacking it with existing items if possible
    /// and if the item's MaxStackSize allows. Creates new slots if needed.
    /// </summary>
    /// <param name="itemToAdd">The ItemData to add.</param>
    /// <param name="quantity">The amount of the item to add. Must be positive.</param>
    /// <returns>The quantity that was successfully added (might be less than requested if item is null or quantity invalid).</returns>
    public int AddItem(ItemData itemToAdd, int quantity = 1)
    {
        if (itemToAdd == null || quantity <= 0)
        {
            Debug.LogWarning("Attempted to add null item or non-positive quantity.");
            return 0;
        }

        int addedCount = 0;
        int remainingToAdd = quantity;

        // First, try to add to existing stacks (only if item is stackable)
        if (itemToAdd.MaxStackSize > 1)
        {
            // Iterate through existing slots to find matching, non-full stacks
            for (int i = 0; i < _slots.Count; i++)
            {
                InventorySlot slot = _slots[i];
                if (slot.Item == itemToAdd && slot.Quantity < itemToAdd.MaxStackSize)
                {
                    int canAdd = itemToAdd.MaxStackSize - slot.Quantity; // Space remaining in this stack
                    int toTransfer = Math.Min(remainingToAdd, canAdd);   // How much we can add to this slot

                    slot.Quantity += toTransfer;
                    _slots[i] = slot; // Update the struct in the list
                    
                    remainingToAdd -= toTransfer;
                    addedCount += toTransfer;

                    if (remainingToAdd == 0) break; // All requested quantity has been added
                }
            }
        }

        // If there's still quantity to add, create new slots
        while (remainingToAdd > 0)
        {
            int toTransfer = Math.Min(remainingToAdd, itemToAdd.MaxStackSize); // Fill new slot up to MaxStackSize
            _slots.Add(new InventorySlot(itemToAdd, toTransfer));
            remainingToAdd -= toTransfer;
            addedCount += toTransfer;
        }

        if (addedCount > 0)
        {
            OnInventoryChanged?.Invoke(); // Notify listeners that inventory has changed
        }
        return addedCount;
    }

    /// <summary>
    /// Removes an item from the inventory. It will remove from multiple stacks if necessary.
    /// </summary>
    /// <param name="itemToRemove">The ItemData to remove.</param>
    /// <param name="quantity">The amount of the item to remove. Must be positive.</param>
    /// <returns>True if the specified quantity of the item was successfully removed, false otherwise.</returns>
    public bool RemoveItem(ItemData itemToRemove, int quantity = 1)
    {
        if (itemToRemove == null || quantity <= 0)
        {
            Debug.LogWarning("Attempted to remove null item or non-positive quantity.");
            return false;
        }

        // First, check if there's enough total quantity to remove
        if (GetItemQuantity(itemToRemove) < quantity)
        {
            Debug.Log($"Not enough {itemToRemove.ItemName} in inventory. Have {GetItemQuantity(itemToRemove)}, need {quantity}.");
            return false; // Not enough items to remove
        }

        int removedCount = 0;
        // Iterate backward to safely remove elements from the list while iterating.
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = _slots[i];
            if (slot.Item == itemToRemove)
            {
                int toRemoveFromThisSlot = Math.Min(quantity - removedCount, slot.Quantity); // How much to take from current slot

                slot.Quantity -= toRemoveFromThisSlot;
                removedCount += toRemoveFromThisSlot;

                // If the slot is now empty, remove it from the list entirely
                if (slot.Quantity <= 0)
                {
                    _slots.RemoveAt(i);
                }
                else
                {
                    // Otherwise, update the slot in the list with its new quantity
                    _slots[i] = slot;
                }
            }
            if (removedCount >= quantity)
            {
                break; // All requested quantity has been removed
            }
        }

        if (removedCount > 0)
        {
            OnInventoryChanged?.Invoke(); // Notify listeners
            return true;
        }
        return false; // Should theoretically not be reached if initial HasItem check passes.
    }

    /// <summary>
    /// Checks if the inventory contains a specific item with at least the specified quantity.
    /// </summary>
    /// <param name="item">The ItemData to check for.</param>
    /// <param name="quantity">The minimum quantity required.</param>
    /// <returns>True if the inventory has enough of the item, false otherwise.</returns>
    public bool HasItem(ItemData item, int quantity = 1)
    {
        return GetItemQuantity(item) >= quantity;
    }

    /// <summary>
    /// Gets the total quantity of a specific item across all stacks/slots in the inventory.
    /// </summary>
    /// <param name="item">The ItemData to count.</param>
    /// <returns>The total quantity of the item.</returns>
    public int GetItemQuantity(ItemData item)
    {
        if (item == null) return 0;
        // Use LINQ to sum quantities of all matching items.
        return _slots.Where(s => s.Item == item).Sum(s => s.Quantity);
    }
}
```

#### 4. `PlayerInventory.cs` (Player-specific Inventory and Currency)

A MonoBehaviour that represents the player's personal inventory and currency. It wraps an `Inventory` instance.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For IReadOnlyList

/// <summary>
/// Manages the player's items and currency.
/// This MonoBehaviour component integrates the generic Inventory class with player-specific needs,
/// such as managing gold and providing player-centric events.
/// It acts as a facade over the underlying generic Inventory.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Player Currency")]
    [SerializeField] private float _currentGold = 100f; // Player's starting gold.
    public float CurrentGold => _currentGold; // Public getter for current gold.

    /// <summary>
    /// Event fired when player's gold changes. Passes the new total gold.
    /// </summary>
    public event Action<float> OnCurrencyChanged;

    /// <summary>
    /// Event fired when player's item inventory changes (add, remove, quantity update).
    /// </summary>
    public event Action OnPlayerInventoryChanged;

    [Header("Player Items")]
    // The actual item management logic is handled by an instance of the generic Inventory class.
    // [SerializeField] makes it visible in the Inspector for debugging and initial setup if desired.
    [SerializeField]
    private Inventory _inventory = new Inventory();

    void Awake()
    {
        // Subscribe to the generic inventory's change event and re-broadcast it.
        // This allows other player-specific systems (like UI) to listen to a single event.
        _inventory.OnInventoryChanged += () => OnPlayerInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Adds gold to the player's currency.
    /// </summary>
    /// <param name="amount">The positive amount of gold to add.</param>
    public void AddCurrency(float amount)
    {
        if (amount < 0) return;
        _currentGold += amount;
        OnCurrencyChanged?.Invoke(_currentGold); // Notify listeners
        Debug.Log($"Player gained {amount} gold. Total: {_currentGold}");
    }

    /// <summary>
    /// Removes gold from the player's currency.
    /// </summary>
    /// <param name="amount">The positive amount of gold to remove.</param>
    /// <returns>True if gold was successfully removed, false if not enough gold.</returns>
    public bool RemoveCurrency(float amount)
    {
        if (amount < 0) return false;
        if (_currentGold >= amount)
        {
            _currentGold -= amount;
            OnCurrencyChanged?.Invoke(_currentGold); // Notify listeners
            Debug.Log($"Player lost {amount} gold. Total: {_currentGold}");
            return true;
        }
        Debug.LogWarning($"Player tried to remove {amount} gold but only has {_currentGold}. Insufficient funds.");
        return false;
    }

    /// <summary>
    /// Checks if the player can afford a certain amount of gold.
    /// </summary>
    /// <param name="amount">The amount to check.</param>
    /// <returns>True if the player has enough gold, false otherwise.</returns>
    public bool CanAfford(float amount)
    {
        return _currentGold >= amount;
    }

    // --- Wrapper methods for the underlying Inventory to expose its functionality ---
    // These methods simply delegate to the internal _inventory instance.
    public int AddItem(ItemData item, int quantity = 1)
    {
        return _inventory.AddItem(item, quantity);
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        return _inventory.RemoveItem(item, quantity);
    }

    public bool HasItem(ItemData item, int quantity = 1)
    {
        return _inventory.HasItem(item, quantity);
    }

    public int GetItemQuantity(ItemData item)
    {
        return _inventory.GetItemQuantity(item);
    }

    /// <summary>
    /// Provides a read-only list of the player's current inventory slots.
    /// </summary>
    public IReadOnlyList<InventorySlot> GetInventorySlots()
    {
        return _inventory.Slots;
    }
}
```

#### 5. `ShopInventory.cs` (Shop-specific Inventory and Currency)

A MonoBehaviour that represents a shop's inventory and currency. It also wraps an `Inventory` instance and allows defining initial stock in the Inspector.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For [Serializable]

/// <summary>
/// Manages a shop's items for sale and its own currency reserves.
/// This MonoBehaviour component integrates the generic Inventory class with shop-specific needs,
/// such as defining initial stock and providing shop-centric events.
/// It acts as a facade over the underlying generic Inventory.
/// </summary>
public class ShopInventory : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private float _shopGold = 1000f; // Shop's starting gold (for buying items from the player).
    public float ShopGold => _shopGold; // Public getter for shop's current gold.

    /// <summary>
    /// Event fired when shop's gold changes. Passes the new total gold.
    /// </summary>
    public event Action<float> OnShopCurrencyChanged;

    /// <summary>
    /// Event fired when shop's item inventory changes (add, remove, quantity update).
    /// </summary>
    public event Action OnShopInventoryChanged;

    [Header("Initial Shop Stock")]
    // Use a list of InventorySlot to define the shop's starting items in the Inspector.
    // These items will be loaded into the runtime inventory upon Awake.
    [SerializeField] private List<InventorySlot> _initialStock = new List<InventorySlot>();

    // The actual item management logic is handled by an instance of the generic Inventory class.
    [SerializeField]
    private Inventory _inventory = new Inventory();

    void Awake()
    {
        // Populate the shop's runtime inventory from the inspector-defined initial stock.
        foreach (var slot in _initialStock)
        {
            if (slot.Item != null && slot.Quantity > 0)
            {
                _inventory.AddItem(slot.Item, slot.Quantity);
            }
        }
        // Subscribe to the generic inventory's change event and re-broadcast it.
        _inventory.OnInventoryChanged += () => OnShopInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Adds gold to the shop's currency.
    /// </summary>
    /// <param name="amount">The positive amount of gold to add.</param>
    public void AddCurrency(float amount)
    {
        if (amount < 0) return;
        _shopGold += amount;
        OnShopCurrencyChanged?.Invoke(_shopGold); // Notify listeners
        Debug.Log($"Shop gained {amount} gold. Total: {_shopGold}");
    }

    /// <summary>
    /// Removes gold from the shop's currency.
    /// </summary>
    /// <param name="amount">The positive amount of gold to remove.</param>
    /// <returns>True if gold was successfully removed, false if not enough gold.</returns>
    public bool RemoveCurrency(float amount)
    {
        if (amount < 0) return false;
        if (_shopGold >= amount)
        {
            _shopGold -= amount;
            OnShopCurrencyChanged?.Invoke(_shopGold); // Notify listeners
            Debug.Log($"Shop lost {amount} gold. Total: {_shopGold}");
            return true;
        }
        Debug.LogWarning($"Shop tried to remove {amount} gold but only has {_shopGold}. Insufficient funds.");
        return false;
    }

    /// <summary>
    /// Checks if the shop can afford a certain amount of gold.
    /// </summary>
    /// <param name="amount">The amount to check.</param>
    /// <returns>True if the shop has enough gold, false otherwise.</returns>
    public bool CanAfford(float amount)
    {
        return _shopGold >= amount;
    }

    // --- Wrapper methods for the underlying Inventory to expose its functionality ---
    // These methods simply delegate to the internal _inventory instance.
    public int AddItem(ItemData item, int quantity = 1)
    {
        return _inventory.AddItem(item, quantity);
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        return _inventory.RemoveItem(item, quantity);
    }

    public bool HasItem(ItemData item, int quantity = 1)
    {
        return _inventory.HasItem(item, quantity);
    }

    public int GetItemQuantity(ItemData item)
    {
        return _inventory.GetItemQuantity(item);
    }

    /// <summary>
    /// Provides a read-only list of the shop's current inventory slots.
    /// </summary>
    public IReadOnlyList<InventorySlot> GetInventorySlots()
    {
        return _inventory.Slots;
    }
}
```

#### 6. `ShopManager.cs` (Central Transaction Handler)

This is the central component responsible for handling all buying and selling logic, ensuring all conditions are met before transactions occur. It decouples the player and shop inventories from direct interaction.

```csharp
using UnityEngine;

/// <summary>
/// The central manager for facilitating buying and selling transactions
/// between a PlayerInventory and a ShopInventory.
/// This component embodies the 'ShopInventorySystem' pattern by providing a unified
/// interface for all trade operations, ensuring consistency and handling all rules.
/// It acts as the orchestrator for all transactions, abstracting the complex logic
/// from the individual inventory components.
/// Implemented as a Singleton for easy global access.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // Singleton pattern: Provides a global point of access to the ShopManager instance.
    public static ShopManager Instance { get; private set; }

    void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ShopManager instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make it persist across scene loads if desired for a continuous game flow.
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Initiates a purchase transaction: Player buys an item from the Shop.
    /// This method encapsulates all the rules and steps for a successful purchase.
    /// </summary>
    /// <param name="playerInventory">Reference to the PlayerInventory component.</param>
    /// <param name="shopInventory">Reference to the ShopInventory component.</param>
    /// <param name="itemToBuy">The ItemData the player wants to buy.</param>
    /// <param name="quantity">The quantity of the item to buy (default is 1).</param>
    /// <returns>True if the purchase was successful, false otherwise.</returns>
    public bool BuyItem(PlayerInventory playerInventory, ShopInventory shopInventory, ItemData itemToBuy, int quantity = 1)
    {
        // Input validation
        if (playerInventory == null || shopInventory == null || itemToBuy == null || quantity <= 0)
        {
            Debug.LogError("BuyItem: Invalid parameters provided. Check player, shop, item or quantity.");
            return false;
        }

        // Calculate total cost
        float totalCost = itemToBuy.BaseCost * quantity;

        // 1. Pre-condition Check: Does the shop have enough of the item?
        if (!shopInventory.HasItem(itemToBuy, quantity))
        {
            Debug.Log($"Failed to buy {quantity} {itemToBuy.ItemName}. Shop doesn't have enough. Available: {shopInventory.GetItemQuantity(itemToBuy)}");
            return false;
        }

        // 2. Pre-condition Check: Can the player afford the item?
        if (!playerInventory.CanAfford(totalCost))
        {
            Debug.Log($"Failed to buy {quantity} {itemToBuy.ItemName}. Player cannot afford {totalCost} gold. Player gold: {playerInventory.CurrentGold}");
            return false;
        }

        // --- All conditions met, proceed with the transaction ---

        // 3. Currency Transfer: Remove gold from player, add to shop.
        if (!playerInventory.RemoveCurrency(totalCost))
        {
            Debug.LogError($"Error removing {totalCost} gold from player during buy transaction. This shouldn't happen after CanAfford check.");
            return false;
        }
        shopInventory.AddCurrency(totalCost); // Shop receives the money

        // 4. Item Transfer: Remove item from shop, add to player.
        if (!shopInventory.RemoveItem(itemToBuy, quantity))
        {
            Debug.LogError($"Error removing {quantity} {itemToBuy.ItemName} from shop during buy transaction. This shouldn't happen after HasItem check.");
            // Potentially revert currency if this fails, depending on desired robustness.
            playerInventory.AddCurrency(totalCost); // Revert gold
            return false;
        }
        playerInventory.AddItem(itemToBuy, quantity);

        Debug.Log($"Player successfully bought {quantity} {itemToBuy.ItemName} for {totalCost} gold. (Player: {playerInventory.CurrentGold}, Shop: {shopInventory.ShopGold})");
        return true;
    }

    /// <summary>
    /// Initiates a sell transaction: Player sells an item to the Shop.
    /// This method encapsulates all the rules and steps for a successful sale.
    /// </summary>
    /// <param name="playerInventory">Reference to the PlayerInventory component.</param>
    /// <param name="shopInventory">Reference to the ShopInventory component.</param>
    /// <param name="itemToSell">The ItemData the player wants to sell.</param>
    /// <param name="quantity">The quantity of the item to sell (default is 1).</param>
    /// <returns>True if the sale was successful, false otherwise.</returns>
    public bool SellItem(PlayerInventory playerInventory, ShopInventory shopInventory, ItemData itemToSell, int quantity = 1)
    {
        // Input validation
        if (playerInventory == null || shopInventory == null || itemToSell == null || quantity <= 0)
        {
            Debug.LogError("SellItem: Invalid parameters provided. Check player, shop, item or quantity.");
            return false;
        }

        // For simplicity, selling price is also BaseCost. In a real game, it might be
        // itemToSell.BaseCost * shopSellModifier * globalEconomyModifier.
        float totalRevenue = itemToSell.BaseCost * quantity;

        // 1. Pre-condition Check: Does the player have enough of the item to sell?
        if (!playerInventory.HasItem(itemToSell, quantity))
        {
            Debug.Log($"Failed to sell {quantity} {itemToSell.ItemName}. Player doesn't have enough. Player has: {playerInventory.GetItemQuantity(itemToSell)}");
            return false;
        }

        // 2. Pre-condition Check: Can the shop afford to buy the item?
        if (!shopInventory.CanAfford(totalRevenue))
        {
            Debug.Log($"Failed to sell {quantity} {itemToSell.ItemName}. Shop cannot afford to buy for {totalRevenue} gold. Shop gold: {shopInventory.ShopGold}");
            return false;
        }

        // --- All conditions met, proceed with the transaction ---

        // 3. Item Transfer: Remove item from player, add to shop.
        if (!playerInventory.RemoveItem(itemToSell, quantity))
        {
            Debug.LogError($"Error removing {quantity} {itemToSell.ItemName} from player during sell transaction. This shouldn't happen after HasItem check.");
            return false;
        }
        shopInventory.AddItem(itemToSell, quantity);

        // 4. Currency Transfer: Remove gold from shop, add to player.
        if (!shopInventory.RemoveCurrency(totalRevenue))
        {
            Debug.LogError($"Error removing {totalRevenue} gold from shop during sell transaction. This shouldn't happen after CanAfford check.");
            // Potentially revert item transfer if this fails.
            playerInventory.AddItem(itemToSell, quantity); // Revert item
            return false;
        }
        playerInventory.AddCurrency(totalRevenue); // Player receives the money

        Debug.Log($"Player successfully sold {quantity} {itemToSell.ItemName} for {totalRevenue} gold. (Player: {playerInventory.CurrentGold}, Shop: {shopInventory.ShopGold})");
        return true;
    }
}
```

#### 7. `GameManager.cs` (Example Usage & Scene Setup)

This script demonstrates how to set up the player and shop, and then simulate various buy/sell transactions. It also shows how to subscribe to events.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This class serves as an example of how to set up and interact with the
/// ShopInventorySystem components in a Unity scene.
/// It initializes a player and a shop, and then simulates some transactions
/// to demonstrate the system's functionality.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Dependencies (Assign in Inspector)")]
    [SerializeField] private PlayerInventory _playerInventory;
    [SerializeField] private ShopInventory _shopInventory;
    // ShopManager is a singleton, so it can be found automatically, but explicit assignment is clearer.
    [SerializeField] private ShopManager _shopManager;

    [Header("Initial Player Items (for testing)")]
    // Populate this list in the Inspector to give the player starting items.
    [SerializeField] private List<InventorySlot> _initialPlayerItems = new List<InventorySlot>();

    [Header("Items available for testing transactions")]
    // Assign some ItemData Scriptable Objects here in the Inspector to test buy/sell scenarios.
    [SerializeField] private ItemData _testItem1;
    [SerializeField] private ItemData _testItem2;
    [SerializeField] private ItemData _testItem3;


    void Start()
    {
        // --- Dependency checks and initialization ---
        if (_shopManager == null)
        {
            _shopManager = ShopManager.Instance; // Try to get the singleton instance
            if (_shopManager == null)
            {
                Debug.LogError("ShopManager not found! Please ensure a GameObject with ShopManager.cs exists in the scene.");
                return;
            }
        }

        if (_playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned! Please assign the Player GameObject with PlayerInventory.cs in the Inspector.");
            return;
        }

        if (_shopInventory == null)
        {
            Debug.LogError("ShopInventory not assigned! Please assign the Shop GameObject with ShopInventory.cs in the Inspector.");
            return;
        }

        // Initialize player's starting items from inspector-defined list.
        foreach (var slot in _initialPlayerItems)
        {
            if (slot.Item != null && slot.Quantity > 0)
            {
                _playerInventory.AddItem(slot.Item, slot.Quantity);
            }
        }

        // --- Subscribe to events for logging / UI updates ---
        // In a real game, UI components would subscribe to these to update dynamically.
        _playerInventory.OnCurrencyChanged += OnPlayerCurrencyChanged;
        _playerInventory.OnPlayerInventoryChanged += OnPlayerItemsChanged;
        _shopInventory.OnShopCurrencyChanged += OnShopCurrencyChanged;
        _shopInventory.OnShopInventoryChanged += OnShopItemsChanged;

        Debug.Log("--- ShopInventorySystem Initialized ---");
        LogCurrentStates(); // Log initial state to console

        // --- Demonstrate Transactions after a short delay for clarity ---
        Invoke(nameof(SimulateTransactions), 2f);
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks or null reference exceptions
        // if the subscribed objects are destroyed but the publisher persists.
        if (_playerInventory != null)
        {
            _playerInventory.OnCurrencyChanged -= OnPlayerCurrencyChanged;
            _playerInventory.OnPlayerInventoryChanged -= OnPlayerItemsChanged;
        }
        if (_shopInventory != null)
        {
            _shopInventory.OnShopCurrencyChanged -= OnShopCurrencyChanged;
            _shopInventory.OnShopInventoryChanged -= OnShopItemsChanged;
        }
    }

    /// <summary>
    /// Simulates a series of buy and sell transactions to demonstrate the system.
    /// </summary>
    private void SimulateTransactions()
    {
        Debug.Log("\n--- Simulating Transactions ---");

        // Example 1: Player successfully buys an item.
        if (_testItem1 != null)
        {
            Debug.Log($"\nAttempting to BUY {_testItem1.ItemName} (x1) from shop.");
            _shopManager.BuyItem(_playerInventory, _shopInventory, _testItem1, 1);
            LogCurrentStates();
        }

        // Example 2: Player tries to buy more than the shop has in stock.
        if (_testItem2 != null)
        {
            Debug.Log($"\nAttempting to BUY {_testItem2.ItemName} (x100 - more than shop likely has).");
            _shopManager.BuyItem(_playerInventory, _shopInventory, _testItem2, 100);
            LogCurrentStates();
        }

        // Example 3: Player tries to buy an item they cannot afford.
        if (_testItem3 != null)
        {
            // Temporarily make test item 3 very expensive for this specific test case.
            float originalCost = _testItem3.BaseCost;
            _testItem3.BaseCost = 1000f; 
            Debug.Log($"\nAttempting to BUY {_testItem3.ItemName} (x1) - very expensive, player might not afford ({_testItem3.BaseCost} gold).");
            _shopManager.BuyItem(_playerInventory, _shopInventory, _testItem3, 1);
            _testItem3.BaseCost = originalCost; // Revert cost for subsequent tests if any.
            LogCurrentStates();
        }

        // Example 4: Player successfully sells an item they have.
        if (_testItem1 != null)
        {
            // Ensure player has the item to sell for this demonstration.
            if (!_playerInventory.HasItem(_testItem1, 1))
            {
                 Debug.Log($"Player doesn't have {_testItem1.ItemName} to sell, giving them one for demonstration.");
                 _playerInventory.AddItem(_testItem1, 1);
                 LogCurrentStates();
            }

            Debug.Log($"\nAttempting to SELL {_testItem1.ItemName} (x1) to shop.");
            _shopManager.SellItem(_playerInventory, _shopInventory, _testItem1, 1);
            LogCurrentStates();
        }

        // Example 5: Player tries to sell an item they don't possess.
        if (_testItem3 != null)
        {
            Debug.Log($"\nAttempting to SELL {_testItem3.ItemName} (x1) which player likely doesn't have.");
            _shopManager.SellItem(_playerInventory, _shopInventory, _testItem3, 1);
            LogCurrentStates();
        }
    }

    /// <summary>
    /// Logs the current state of player and shop inventories and currencies to the console.
    /// </summary>
    private void LogCurrentStates()
    {
        Debug.Log("\n--- Current States ---");
        Debug.Log($"Player Gold: {_playerInventory.CurrentGold:F2}"); // Format to 2 decimal places
        Debug.Log("Player Inventory:");
        LogInventory(_playerInventory.GetInventorySlots());

        Debug.Log($"Shop Gold: {_shopInventory.ShopGold:F2}");
        Debug.Log("Shop Inventory:");
        LogInventory(_shopInventory.GetInventorySlots());
        Debug.Log("----------------------");
    }

    /// <summary>
    /// Helper to log the contents of an inventory.
    /// </summary>
    private void LogInventory(IReadOnlyList<InventorySlot> slots)
    {
        if (slots.Count == 0)
        {
            Debug.Log("- Empty -");
            return;
        }
        foreach (var slot in slots)
        {
            Debug.Log($"- {slot.Item.ItemName} x{slot.Quantity}");
        }
    }

    // --- Event Handlers (for console logging, but would update UI in a real game) ---
    private void OnPlayerCurrencyChanged(float newGold)
    {
        Debug.Log($"[EVENT] Player Currency Changed: {newGold:F2}");
    }

    private void OnPlayerItemsChanged()
    {
        Debug.Log("[EVENT] Player Inventory Changed! (Re-logging full inventory for demo)");
        // In a real game, this would trigger specific UI updates rather than a full log.
        // LogInventory(_playerInventory.GetInventorySlots());
    }

    private void OnShopCurrencyChanged(float newGold)
    {
        Debug.Log($"[EVENT] Shop Currency Changed: {newGold:F2}");
    }

    private void OnShopItemsChanged()
    {
        Debug.Log("[EVENT] Shop Inventory Changed! (Re-logging full inventory for demo)");
        // LogInventory(_shopInventory.GetInventorySlots());
    }
}
```

---

### **Unity Setup Instructions**

1.  **Create C# Scripts:**
    *   In your Unity project's `Assets` folder, create a new folder (e.g., `Scripts`).
    *   Inside `Scripts`, create new C# scripts with the exact names:
        *   `ItemData.cs`
        *   `InventorySlot.cs`
        *   `Inventory.cs`
        *   `PlayerInventory.cs`
        *   `ShopInventory.cs`
        *   `ShopManager.cs`
        *   `GameManager.cs`
    *   Copy and paste the corresponding code into each script file.

2.  **Create ItemData Assets:**
    *   Create another folder (e.g., `Items`) in `Assets`.
    *   Right-click in the `Items` folder -> `Create` -> `Shop` -> `Item Data`.
    *   Create at least three `ItemData` assets (e.g., `HealthPotion`, `IronSword`, `MagicRobe`).
    *   Select each `ItemData` asset in the Project window and fill in its properties in the Inspector:
        *   `Item Name`: e.g., "Health Potion"
        *   `Description`: (some text)
        *   `Base Cost`: e.g., 25 for potion, 100 for sword
        *   `Max Stack Size`: e.g., 5 for potion, 1 for sword/robe (non-stackable)
        *   (Optional) Assign a simple `Sprite` for the `Icon`.

3.  **Create GameObjects in Scene:**
    *   In your Unity scene, create an empty GameObject named `Player`.
        *   Attach the `PlayerInventory.cs` script to it.
    *   Create an empty GameObject named `Shop`.
        *   Attach the `ShopInventory.cs` script to it.
        *   In the Inspector for `ShopInventory`, under `Initial Shop Stock`, add some items by dragging your `ItemData` assets from the `Items` folder and setting quantities (e.g., `HealthPotion x 5`, `IronSword x 2`).
    *   Create an empty GameObject named `ShopManagerObject`.
        *   Attach the `ShopManager.cs` script to it. (This will become your singleton manager).
    *   Create an empty GameObject named `Game Manager`.
        *   Attach the `GameManager.cs` script to it.
        *   In the Inspector for `GameManager`:
            *   Drag the `Player` GameObject into the `_playerInventory` field.
            *   Drag the `Shop` GameObject into the `_shopInventory` field.
            *   Drag the `ShopManagerObject` GameObject into the `_shopManager` field.
            *   Assign your created `ItemData` assets (e.g., `HealthPotion`, `IronSword`, `MagicRobe`) to `_testItem1`, `_testItem2`, `_testItem3` to be used in the simulated transactions.
            *   (Optional) Add some `_initialPlayerItems` if you want the player to start with items they can sell.

4.  **Run the Scene:**
    *   Play the Unity scene.
    *   Open the Console window (`Window` -> `General` -> `Console`).
    *   Observe the `Debug.Log` messages:
        *   Initial states of player and shop inventories and gold.
        *   Logs for each simulated transaction attempt (buy/sell).
        *   Event messages whenever currency or inventory contents change.
    *   While the scene is running, select the `Player` and `Shop` GameObjects in the Hierarchy and observe their respective `PlayerInventory` and `ShopInventory` components in the Inspector. You will see their `CurrentGold`/`ShopGold` and `_inventory` contents update in real-time.

This comprehensive setup provides a functional, educational, and practical example of a ShopInventorySystem in Unity, ready for further expansion and integration into your game's UI and logic.