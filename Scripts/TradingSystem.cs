// Unity Design Pattern Example: TradingSystem
// This script demonstrates the TradingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **TradingSystem** design pattern. It provides a central `TradingSystem` class that acts as a **Facade**, simplifying the complex process of buying and selling items between a player and a shop.

The example uses:
*   **`ScriptableObject`** for defining `Currency` and `TradableItem` data, making it easy for designers to create and manage game assets.
*   **Serializable `Inventory` class** to manage item quantities and currency amounts for both the player and the shop.
*   **Events (`Action`)** to notify other parts of the game (e.g., UI) when a trade is completed.
*   **Detailed logging** for clear understanding of the trade flow.

---

### **Project Setup in Unity:**

1.  **Create Folder Structure:** In your Unity project's `Assets` folder, create a new folder: `Scripts/TradingSystem`.
2.  **Create C# Scripts:** Create the following C# scripts inside the `Scripts/TradingSystem` folder and copy the code into them:
    *   `Currency.cs`
    *   `TradableItem.cs`
    *   `Inventory.cs`
    *   `TradingSystem.cs`
3.  **Create ScriptableObjects:**
    *   Right-click in your Project window (`Assets` folder or a subfolder like `Assets/Data` if you prefer).
    *   Go to `Create -> Trading System -> Currency`. Name it `Gold`.
    *   Go to `Create -> Trading System -> Tradable Item`. Name it `HealthPotion`.
        *   Set its `Base Price` to `50`.
        *   Drag your `Gold` Currency ScriptableObject into the `Currency Type` field.
    *   Go to `Create -> Trading System -> Tradable Item`. Name it `MagicSword`.
        *   Set its `Base Price` to `250`.
        *   Drag your `Gold` Currency ScriptableObject into the `Currency Type` field.
4.  **Create Game Manager:**
    *   Create an empty GameObject in your scene (e.g., `Hierarchy -> Create Empty`). Name it `GameManager`.
    *   Select the `GameManager` GameObject. In the Inspector, click `Add Component` and search for `Trading System` to add the `TradingSystem.cs` script to it.
5.  **Configure TradingSystem Component:**
    *   In the `GameManager`'s Inspector, configure the `TradingSystem` component:
        *   **Player Inventory:**
            *   Expand `Initial Items`. Add 2 elements.
                *   Element 0: Drag `HealthPotion` into `Item`, set `Quantity` to `1`.
                *   Element 1: Drag `MagicSword` into `Item`, set `Quantity` to `1`.
            *   Expand `Initial Currencies`. Add 1 element.
                *   Element 0: Drag `Gold` into `Currency`, set `Amount` to `500`.
        *   **Shop Inventory:**
            *   Expand `Initial Items`. Add 2 elements.
                *   Element 0: Drag `HealthPotion` into `Item`, set `Quantity` to `5`.
                *   Element 1: Drag `MagicSword` into `Item`, set `Quantity` to `2`.
            *   Expand `Initial Currencies`. Add 1 element.
                *   Element 0: Drag `Gold` into `Currency`, set `Amount` to `1000`.
        *   **Demo Fields:**
            *   Drag `HealthPotion` into `Demo Item 1`.
            *   Drag `MagicSword` into `Demo Item 2`.
            *   Drag `Gold` into `Demo Currency`.
6.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Observe the Console window for initial inventory states.
    *   While the game is running, select the `GameManager` in the Hierarchy.
    *   In the Inspector for the `TradingSystem` component, click the three dots (`...`) next to the component name, then select `Perform Sample Trades`.
    *   Watch the Console window for detailed logs of trade attempts, successes, failures, and event notifications.

---

### **1. `Currency.cs`**

This `ScriptableObject` defines different types of in-game currencies.

```csharp
// Currency.cs
using UnityEngine;

namespace TradingSystem
{
    /// <summary>
    /// ScriptableObject representing a type of currency in the game.
    /// This allows designers to easily create and manage different currencies (e.g., Gold, Gems, Tokens).
    /// </summary>
    [CreateAssetMenu(fileName = "NewCurrency", menuName = "Trading System/Currency", order = 1)]
    public class Currency : ScriptableObject
    {
        [Tooltip("A unique identifier for this currency.")]
        public string id = System.Guid.NewGuid().ToString();

        [Tooltip("The display name of the currency (e.g., 'Gold Coins', 'Magic Dust').")]
        public string currencyName = "New Currency";

        [Tooltip("Optional: An icon to represent this currency in the UI.")]
        public Sprite icon;

        /// <summary>
        /// Ensures the ID is unique when created or validated in the editor.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
        }
    }
}
```

---

### **2. `TradableItem.cs`**

This `ScriptableObject` defines an item that can be bought or sold within the trading system.

```csharp
// TradableItem.cs
using UnityEngine;

namespace TradingSystem
{
    /// <summary>
    /// ScriptableObject representing an item that can be traded in the game.
    /// This allows designers to easily create and manage various items (e.g., Potions, Swords, Resources).
    /// Each item has a base price and specifies which currency it uses.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTradableItem", menuName = "Trading System/Tradable Item", order = 2)]
    public class TradableItem : ScriptableObject
    {
        [Tooltip("A unique identifier for this item.")]
        public string id = System.Guid.NewGuid().ToString();

        [Tooltip("The display name of the item (e.g., 'Health Potion', 'Iron Sword').")]
        public string itemName = "New Tradable Item";

        [Tooltip("A brief description of the item.")]
        [TextArea]
        public string description = "A generic tradable item.";

        [Tooltip("The base price of the item when buying or selling.")]
        public float basePrice = 10.0f;

        [Tooltip("The type of currency used for this item's transactions.")]
        public Currency currencyType;

        [Tooltip("Optional: An icon to represent this item in the UI.")]
        public Sprite icon;

        /// <summary>
        /// Ensures the ID is unique and checks for common configuration errors during editor validation.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
            if (currencyType == null)
            {
                Debug.LogWarning($"Tradable Item '{itemName}' is missing a Currency Type. Please assign one.", this);
            }
            if (basePrice < 0)
            {
                basePrice = 0;
            }
        }
    }
}
```

---

### **3. `Inventory.cs`**

This class manages the items and currencies for any participant in the trading system (player, shop, etc.). It's `[System.Serializable]` so it can be embedded directly into a `MonoBehaviour` and configured in the Inspector.

```csharp
// Inventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TradingSystem
{
    /// <summary>
    /// A simple struct used for initial setup of items in the Unity Inspector.
    /// </summary>
    [Serializable]
    public struct InventoryItemData
    {
        public TradableItem item;
        public int quantity;
    }

    /// <summary>
    /// A simple struct used for initial setup of currencies in the Unity Inspector.
    /// </summary>
    [Serializable]
    public struct InventoryCurrencyData
    {
        public Currency currency;
        public float amount;
    }

    /// <summary>
    /// Represents an inventory for a participant in the trading system (e.g., Player, Shop).
    /// Manages item quantities and currency amounts using internal dictionaries for efficient lookup.
    /// Provides methods for adding, removing, and checking for items/currencies.
    /// </summary>
    [Serializable] // Make Inventory class serializable to appear in Inspector if part of another MonoBehaviour
    public class Inventory
    {
        // Internal dictionaries for quick lookup and management of items and currencies.
        private Dictionary<TradableItem, int> _itemQuantities = new Dictionary<TradableItem, int>();
        private Dictionary<Currency, float> _currencyAmounts = new Dictionary<Currency, float>();

        // Public properties to get read-only access to the inventory contents (encapsulation).
        public IReadOnlyDictionary<TradableItem, int> ItemQuantities => _itemQuantities;
        public IReadOnlyDictionary<Currency, float> CurrencyAmounts => _currencyAmounts;

        // --- Inspector Initialization Fields ---
        // These lists are used for initial setup in the Unity Inspector.
        // They will be converted into the internal Dictionaries when Initialize() is called.
        [SerializeField] private List<InventoryItemData> initialItems = new List<InventoryItemData>();
        [SerializeField] private List<InventoryCurrencyData> initialCurrencies = new List<InventoryCurrencyData>();

        /// <summary>
        /// Initializes the inventory with items and currencies specified in the inspector.
        /// This method should be called once, typically during Awake, to populate the internal dictionaries.
        /// </summary>
        public void Initialize()
        {
            _itemQuantities.Clear();
            foreach (var data in initialItems)
            {
                if (data.item != null && data.quantity > 0)
                {
                    _itemQuantities[data.item] = data.quantity;
                }
            }

            _currencyAmounts.Clear();
            foreach (var data in initialCurrencies)
            {
                if (data.currency != null && data.amount >= 0) // Allow 0 initial currency
                {
                    _currencyAmounts[data.currency] = data.amount;
                }
            }
            Debug.Log($"Inventory Initialized with {initialItems.Count} unique items and {initialCurrencies.Count} unique currencies from inspector setup.");
        }

        /// <summary>
        /// Adds a specified quantity of an item to the inventory.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="quantity">The amount to add.</param>
        public void AddItem(TradableItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"Attempted to add invalid item or quantity: Item={item?.itemName ?? "NULL"}, Quantity={quantity}");
                return;
            }

            if (_itemQuantities.ContainsKey(item))
            {
                _itemQuantities[item] += quantity;
            }
            else
            {
                _itemQuantities.Add(item, quantity);
            }
            Debug.Log($"Added {quantity}x {item.itemName} to inventory. New quantity: {_itemQuantities[item]}");
        }

        /// <summary>
        /// Removes a specified quantity of an item from the inventory.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="quantity">The amount to remove.</param>
        /// <returns>True if items were successfully removed, false otherwise (e.g., not enough items).</returns>
        public bool RemoveItem(TradableItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"Attempted to remove invalid item or quantity: Item={item?.itemName ?? "NULL"}, Quantity={quantity}");
                return false;
            }

            if (_itemQuantities.TryGetValue(item, out int currentQuantity))
            {
                if (currentQuantity >= quantity)
                {
                    _itemQuantities[item] -= quantity;
                    if (_itemQuantities[item] == 0)
                    {
                        _itemQuantities.Remove(item);
                    }
                    Debug.Log($"Removed {quantity}x {item.itemName} from inventory. Remaining: {_itemQuantities.GetValueOrDefault(item, 0)}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Attempted to remove {quantity}x {item.itemName}, but only {currentQuantity} available.");
                    return false;
                }
            }
            Debug.LogWarning($"Attempted to remove {quantity}x {item.itemName}, but item not found in inventory.");
            return false;
        }

        /// <summary>
        /// Checks if the inventory has a sufficient quantity of a specific item.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="requiredQuantity">The required quantity.</param>
        /// <returns>True if the inventory has the required quantity, false otherwise.</returns>
        public bool HasItem(TradableItem item, int requiredQuantity)
        {
            if (item == null || requiredQuantity <= 0) return false;
            return _itemQuantities.GetValueOrDefault(item, 0) >= requiredQuantity;
        }

        /// <summary>
        /// Gets the current quantity of a specific item in the inventory.
        /// </summary>
        /// <param name="item">The item to get the quantity for.</param>
        /// <returns>The quantity of the item, or 0 if not found.</returns>
        public int GetItemQuantity(TradableItem item)
        {
            return _itemQuantities.GetValueOrDefault(item, 0);
        }

        /// <summary>
        /// Adds a specified amount of currency to the inventory.
        /// </summary>
        /// <param name="currency">The currency type to add.</param>
        /// <param name="amount">The amount to add.</param>
        public void AddCurrency(Currency currency, float amount)
        {
            if (currency == null || amount < 0) // Allow adding 0, but not negative
            {
                Debug.LogWarning($"Attempted to add invalid currency or amount: Currency={currency?.currencyName ?? "NULL"}, Amount={amount}");
                return;
            }

            if (_currencyAmounts.ContainsKey(currency))
            {
                _currencyAmounts[currency] += amount;
            }
            else
            {
                _currencyAmounts.Add(currency, amount);
            }
            Debug.Log($"Added {amount:F2} {currency.currencyName}. New total: {_currencyAmounts[currency]:F2}");
        }

        /// <summary>
        /// Removes a specified amount of currency from the inventory.
        /// </summary>
        /// <param name="currency">The currency type to remove.</param>
        /// <param name="amount">The amount to remove.</param>
        /// <returns>True if currency was successfully removed, false otherwise (e.g., not enough currency).</returns>
        public bool RemoveCurrency(Currency currency, float amount)
        {
            if (currency == null || amount < 0) // Allow removing 0, but not negative
            {
                Debug.LogWarning($"Attempted to remove invalid currency or amount: Currency={currency?.currencyName ?? "NULL"}, Amount={amount}");
                return false;
            }

            if (_currencyAmounts.TryGetValue(currency, out float currentAmount))
            {
                if (currentAmount >= amount)
                {
                    _currencyAmounts[currency] -= amount;
                    Debug.Log($"Removed {amount:F2} {currency.currencyName}. Remaining: {_currencyAmounts[currency]:F2}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Attempted to remove {amount:F2} {currency.currencyName}, but only {currentAmount:F2} available.");
                    return false;
                }
            }
            Debug.LogWarning($"Attempted to remove {amount:F2} {currency.currencyName}, but currency type not found in inventory.");
            return false;
        }

        /// <summary>
        /// Checks if the inventory has a sufficient amount of a specific currency.
        /// </summary>
        /// <param name="currency">The currency type to check for.</param>
        /// <param name="requiredAmount">The required amount.</param>
        /// <returns>True if the inventory has the required amount, false otherwise.</returns>
        public bool HasCurrency(Currency currency, float requiredAmount)
        {
            if (currency == null || requiredAmount < 0) return false;
            return _currencyAmounts.GetValueOrDefault(currency, 0f) >= requiredAmount;
        }

        /// <summary>
        /// Gets the current amount of a specific currency in the inventory.
        /// </summary>
        /// <param name="currency">The currency type to get the amount for.</param>
        /// <returns>The amount of the currency, or 0 if not found.</returns>
        public float GetCurrencyAmount(Currency currency)
        {
            return _currencyAmounts.GetValueOrDefault(currency, 0f);
        }

        /// <summary>
        /// Debug method to print inventory contents to the console.
        /// Useful for inspecting inventory state at various points in time.
        /// </summary>
        /// <param name="inventoryName">A descriptive name to identify the inventory in logs (e.g., "Player", "Shop").</param>
        public void PrintInventory(string inventoryName)
        {
            Debug.Log($"--- {inventoryName} Inventory Contents ---");
            Debug.Log("Items:");
            if (_itemQuantities.Count == 0)
            {
                Debug.Log("- No items -");
            }
            foreach (var pair in _itemQuantities)
            {
                Debug.Log($"- {pair.Key.itemName}: {pair.Value}x");
            }

            Debug.Log("Currencies:");
            if (_currencyAmounts.Count == 0)
            {
                Debug.Log("- No currencies -");
            }
            foreach (var pair in _currencyAmounts)
            {
                // Use F2 for currency to avoid floating point precision issues in display
                Debug.Log($"- {pair.Key.currencyName}: {pair.Value:F2}");
            }
            Debug.Log("-------------------------------------");
        }
    }
}
```

---

### **4. `TradingSystem.cs`**

This is the main component that orchestrates all trading logic, acting as the **Facade** for the entire system. It handles player-shop interactions, manages inventory updates, and dispatches events.

```csharp
// TradingSystem.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TradingSystem
{
    /// <summary>
    /// Represents detailed information about a completed trade.
    /// This struct is passed via the OnTradeCompleted event to provide context
    /// for UI updates, analytics, or other game systems reacting to a trade.
    /// </summary>
    public struct TradeInfo
    {
        public TradableItem Item;           // The item that was traded
        public int Quantity;                // The quantity of the item traded
        public float PricePerUnit;          // The price per unit at the time of trade
        public Currency CurrencyUsed;       // The currency involved in the trade
        public Inventory BuyerInventory;    // Reference to the inventory that received the item
        public Inventory SellerInventory;   // Reference to the inventory that gave up the item
        public TradeType Type;              // Whether it was a Buy or Sell operation from the player's perspective
    }

    /// <summary>
    /// Defines the type of trade that occurred, from the player's perspective.
    /// </summary>
    public enum TradeType
    {
        Buy, // Player bought from seller (shop)
        Sell // Player sold to buyer (shop)
    }

    /// <summary>
    /// The core Facade for the Trading System design pattern.
    /// This MonoBehaviour orchestrates all buying and selling operations between a player and a shop.
    /// It provides a simplified interface for complex trading logic, handling inventory updates,
    /// currency transfers, and event notifications, effectively hiding the underlying complexity.
    /// </summary>
    public class TradingSystem : MonoBehaviour
    {
        // --- Core Inventories ---
        // Serialized fields to allow assigning initial inventory contents directly in the Unity Inspector.
        [Tooltip("The player's inventory, managed by this system.")]
        [SerializeField] private Inventory playerInventory = new Inventory();

        [Tooltip("The shop's inventory, managed by this system.")]
        [SerializeField] private Inventory shopInventory = new Inventory();

        // --- Events ---
        /// <summary>
        /// Event fired when any trade (buy or sell from player's perspective) is successfully completed.
        /// Other systems (e.g., UI, achievement systems, analytics) can subscribe to this event
        /// to react to trade activities without needing to know the internal mechanics.
        /// This demonstrates the Observer pattern being used for loose coupling.
        /// </summary>
        public static event Action<TradeInfo> OnTradeCompleted;

        // --- Accessors for other systems ---
        // Public read-only properties to access the managed inventories.
        public Inventory PlayerInventory => playerInventory;
        public Inventory ShopInventory => shopInventory;

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            // Initialize inventories from inspector-defined data.
            // This ensures internal Dictionaries are populated at game start.
            playerInventory.Initialize();
            shopInventory.Initialize();

            Debug.Log("<color=cyan>TradingSystem Initialized.</color>");
            playerInventory.PrintInventory("Player Initial");
            shopInventory.PrintInventory("Shop Initial");
        }

        /// <summary>
        /// Subscribes the event handler when the object becomes enabled.
        /// </summary>
        private void OnEnable()
        {
            OnTradeCompleted += HandleTradeCompleted;
        }

        /// <summary>
        /// Unsubscribes the event handler when the object becomes disabled to prevent memory leaks.
        /// </summary>
        private void OnDisable()
        {
            OnTradeCompleted -= HandleTradeCompleted;
        }

        // --- Public Trading Operations (Facade Methods) ---

        /// <summary>
        /// Initiates a "buy" transaction where the player attempts to buy an item from the shop.
        /// This method encapsulates all the checks and operations required for a purchase.
        /// </summary>
        /// <param name="itemToBuy">The item the player wants to purchase.</param>
        /// <param name="quantity">The amount of the item to purchase.</param>
        /// <returns>True if the transaction was successful, false otherwise.</returns>
        public bool BuyItem(TradableItem itemToBuy, int quantity)
        {
            if (itemToBuy == null)
            {
                Debug.LogError("Attempted to buy a null item.");
                return false;
            }
            if (quantity <= 0)
            {
                Debug.LogError($"Attempted to buy item '{itemToBuy.itemName}' with invalid quantity: {quantity}.");
                return false;
            }

            Debug.Log($"Attempting to BUY {quantity}x {itemToBuy.itemName} from shop...");

            // 1. Calculate total cost and identify currency
            float totalPrice = itemToBuy.basePrice * quantity;
            Currency requiredCurrency = itemToBuy.currencyType;

            // 2. Pre-check: Does the player have enough currency?
            if (!playerInventory.HasCurrency(requiredCurrency, totalPrice))
            {
                Debug.LogWarning($"Player cannot afford {quantity}x {itemToBuy.itemName}. Requires {totalPrice:F2} {requiredCurrency.currencyName}, but has {playerInventory.GetCurrencyAmount(requiredCurrency):F2}.");
                return false;
            }

            // 3. Pre-check: Does the shop have enough items in stock?
            if (!shopInventory.HasItem(itemToBuy, quantity))
            {
                Debug.LogWarning($"Shop does not have {quantity}x {itemToBuy.itemName}. Available: {shopInventory.GetItemQuantity(itemToBuy)}.");
                return false;
            }

            // 4. Perform the transaction: Transfer items and currency
            // This sequence ensures an atomic-like operation; all or nothing.
            bool currencyRemovedFromPlayer = playerInventory.RemoveCurrency(requiredCurrency, totalPrice);
            bool currencyAddedToShop = shopInventory.AddCurrency(requiredCurrency, totalPrice);

            bool itemRemovedFromShop = shopInventory.RemoveItem(itemToBuy, quantity);
            bool itemAddedToPlayer = playerInventory.AddItem(itemToBuy, quantity);

            // Basic rollback mechanism if any step failed (should ideally not happen if pre-checks pass)
            if (!currencyRemovedFromPlayer || !currencyAddedToShop || !itemRemovedFromShop || !itemAddedToPlayer)
            {
                Debug.LogError($"<color=red>Critical Error during buy transaction for {itemToBuy.itemName}. Rolling back...</color>");
                // In a robust system, you'd meticulously reverse each step. For this demo, we rely on pre-checks.
                // If any part failed, it likely means a logic error or unexpected state, so we log and fail.
                return false;
            }

            Debug.Log($"<color=green>SUCCESS: Player bought {quantity}x {itemToBuy.itemName} for {totalPrice:F2} {requiredCurrency.currencyName}.</color>");

            // 5. Notify subscribers of the successful trade
            // The facade (TradingSystem) emits an event, allowing other systems to react.
            OnTradeCompleted?.Invoke(new TradeInfo
            {
                Item = itemToBuy,
                Quantity = quantity,
                PricePerUnit = itemToBuy.basePrice,
                CurrencyUsed = requiredCurrency,
                BuyerInventory = playerInventory,
                SellerInventory = shopInventory,
                Type = TradeType.Buy
            });

            return true;
        }

        /// <summary>
        /// Initiates a "sell" transaction where the player attempts to sell an item to the shop.
        /// The shop effectively "buys" the item from the player.
        /// This method encapsulates all the checks and operations required for selling.
        /// </summary>
        /// <param name="itemToSell">The item the player wants to sell.</param>
        /// <param name="quantity">The amount of the item to sell.</param>
        /// <returns>True if the transaction was successful, false otherwise.</returns>
        public bool SellItem(TradableItem itemToSell, int quantity)
        {
            if (itemToSell == null)
            {
                Debug.LogError("Attempted to sell a null item.");
                return false;
            }
            if (quantity <= 0)
            {
                Debug.LogError($"Attempted to sell item '{itemToSell.itemName}' with invalid quantity: {quantity}.");
                return false;
            }

            Debug.Log($"Attempting to SELL {quantity}x {itemToSell.itemName} to shop...");

            // 1. Calculate total value received by player (shop pays)
            // For simplicity, using basePrice for selling too. In a real system, shop might buy for less.
            float totalValue = itemToSell.basePrice * quantity;
            Currency currencyToReceive = itemToSell.currencyType;

            // 2. Pre-check: Does the player have enough items to sell?
            if (!playerInventory.HasItem(itemToSell, quantity))
            {
                Debug.LogWarning($"Player does not have {quantity}x {itemToSell.itemName} to sell. Available: {playerInventory.GetItemQuantity(itemToSell)}.");
                return false;
            }

            // 3. Pre-check: Can the shop afford to buy the item?
            if (!shopInventory.HasCurrency(currencyToReceive, totalValue))
            {
                Debug.LogWarning($"Shop cannot afford to buy {quantity}x {itemToSell.itemName}. Requires {totalValue:F2} {currencyToReceive.currencyName}, but has {shopInventory.GetCurrencyAmount(currencyToReceive):F2}.");
                return false;
            }

            // 4. Perform the transaction: Transfer items and currency
            bool itemRemovedFromPlayer = playerInventory.RemoveItem(itemToSell, quantity);
            bool itemAddedToShop = shopInventory.AddItem(itemToSell, quantity);

            bool currencyRemovedFromShop = shopInventory.RemoveCurrency(currencyToReceive, totalValue);
            bool currencyAddedToPlayer = playerInventory.AddCurrency(currencyToReceive, totalValue);

            // Basic rollback mechanism
            if (!itemRemovedFromPlayer || !itemAddedToShop || !currencyRemovedFromShop || !currencyAddedToPlayer)
            {
                Debug.LogError($"<color=red>Critical Error during sell transaction for {itemToSell.itemName}. Rolling back...</color>");
                return false;
            }

            Debug.Log($"<color=green>SUCCESS: Player sold {quantity}x {itemToSell.itemName} for {totalValue:F2} {currencyToReceive.currencyName}.</color>");

            // 5. Notify subscribers of the successful trade
            OnTradeCompleted?.Invoke(new TradeInfo
            {
                Item = itemToSell,
                Quantity = quantity,
                PricePerUnit = itemToSell.basePrice,
                CurrencyUsed = currencyToReceive,
                BuyerInventory = shopInventory,     // In a sell, the shop is the buyer
                SellerInventory = playerInventory,  // In a sell, the player is the seller
                Type = TradeType.Sell
            });

            return true;
        }

        // --- Demo Usage (for demonstration purposes) ---
        // These fields are assigned in the Inspector to facilitate testing the sample trades.
        [Header("Demo - Assign in Inspector for testing")]
        [SerializeField] private TradableItem demoItem1;
        [SerializeField] private TradableItem demoItem2;
        [SerializeField] private Currency demoCurrency;

        /// <summary>
        /// This method demonstrates how to use the TradingSystem by performing a series of sample trades.
        /// In a real game, these calls would be triggered by UI buttons, player interactions with shopkeepers, etc.
        /// This method can be invoked via a button in the Inspector's context menu (right-click on component header).
        /// </summary>
        [ContextMenu("Perform Sample Trades")]
        public void PerformSampleTrades()
        {
            Debug.Log("\n--- Performing Sample Trades ---");

            if (demoItem1 == null || demoItem2 == null || demoCurrency == null)
            {
                Debug.LogError("Please assign Demo Items and Currency in the inspector to run sample trades.");
                return;
            }
            
            // Print initial states for clear comparison
            playerInventory.PrintInventory("Player (Before Trades)");
            shopInventory.PrintInventory("Shop (Before Trades)");

            // --- Test Case 1: Player successfully buys an item ---
            Debug.Log("\n--- Test Case 1: Player buys 2x " + demoItem1.itemName + " ---");
            BuyItem(demoItem1, 2);
            playerInventory.PrintInventory("Player (After Buy 1)");
            shopInventory.PrintInventory("Shop (After Buy 1)");

            // --- Test Case 2: Player tries to buy more than shop has (should fail) ---
            Debug.Log("\n--- Test Case 2: Player tries to buy 100x " + demoItem1.itemName + " (should fail - shop out of stock) ---");
            BuyItem(demoItem1, 100);
            playerInventory.PrintInventory("Player (After Failed Buy 2)");
            shopInventory.PrintInventory("Shop (After Failed Buy 2)");

            // --- Test Case 3: Player successfully sells an item ---
            Debug.Log("\n--- Test Case 3: Player sells 1x " + demoItem2.itemName + " ---");
            SellItem(demoItem2, 1);
            playerInventory.PrintInventory("Player (After Sell 3)");
            shopInventory.PrintInventory("Shop (After Sell 3)");
            
            // --- Test Case 4: Player tries to sell an item they don't have (should fail) ---
            Debug.Log("\n--- Test Case 4: Player tries to sell 5x " + demoItem1.itemName + " (should fail - player out of stock) ---");
            SellItem(demoItem1, 5);
            playerInventory.PrintInventory("Player (After Failed Sell 4)");
            shopInventory.PrintInventory("Shop (After Failed Sell 4)");

            // --- Test Case 5: Player tries to buy with insufficient funds (should fail) ---
            Debug.Log("\n--- Test Case 5: Player tries to buy 5x " + demoItem2.itemName + " (should fail - insufficient funds) ---");
            // Temporarily remove almost all player's money for this specific test case
            playerInventory.RemoveCurrency(demoCurrency, playerInventory.GetCurrencyAmount(demoCurrency) - 1.0f); 
            BuyItem(demoItem2, 5); // This should now fail due to lack of money
            // Re-add some money for subsequent tests if any, or just to restore state
            playerInventory.AddCurrency(demoCurrency, 500f);

            Debug.Log("\n--- Sample Trades Completed ---");
            playerInventory.PrintInventory("Player (Final State)");
            shopInventory.PrintInventory("Shop (Final State)");
        }

        /// <summary>
        /// A simple listener for the OnTradeCompleted event to demonstrate its usage.
        /// In a real application, a UI Manager, Analytics Manager, or other game systems
        /// would subscribe here to update displays, log data, or trigger other game events.
        /// This showcases the Observer pattern: the TradingSystem doesn't care who listens,
        /// it just broadcasts that a trade happened.
        /// </summary>
        /// <param name="tradeInfo">The structured information about the completed trade.</param>
        private void HandleTradeCompleted(TradeInfo tradeInfo)
        {
            Debug.Log($"<color=lime>Event Received: Trade {tradeInfo.Type} completed!</color>");
            Debug.Log($"- Item: {tradeInfo.Item.itemName}, Quantity: {tradeInfo.Quantity}");
            Debug.Log($"- Total Price: {tradeInfo.PricePerUnit * tradeInfo.Quantity:F2} {tradeInfo.CurrencyUsed.currencyName}");
            Debug.Log($"- Buyer: {(tradeInfo.BuyerInventory == PlayerInventory ? "Player" : "Shop")}, Seller: {(tradeInfo.SellerInventory == PlayerInventory ? "Player" : "Shop")}");
            // Example: Here you would typically call methods on your UIManager to update the player's inventory UI,
            // or a SoundManager to play a trade sound, or an AnalyticsManager to log the trade.
            // UIManager.Instance.UpdatePlayerInventoryDisplay(tradeInfo.BuyerInventory);
            // SoundManager.Instance.PlaySound("TradeComplete");
        }
    }
}
```