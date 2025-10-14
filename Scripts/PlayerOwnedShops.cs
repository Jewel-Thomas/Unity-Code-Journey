// Unity Design Pattern Example: PlayerOwnedShops
// This script demonstrates the PlayerOwnedShops pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PlayerOwnedShops' design pattern empowers players to become entrepreneurs within a game, allowing them to acquire, manage, and profit from their own businesses. This adds a layer of economic simulation and strategic depth to the gameplay.

Below is a complete C# Unity example that demonstrates this pattern. It includes separate classes for item data, inventory slots, the player's personal inventory, individual player-owned shops, and a central manager to orchestrate everything.

---

### PlayerOwnedShopsManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Guid

// --- 1. ShopItemData: ScriptableObject for defining items ---
// This represents the *definition* or blueprint of an item that can be bought or sold.
// Using a ScriptableObject allows you to create these assets directly in the Unity Editor,
// making it easy for designers to add new items without touching code.
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item Data", order = 1)]
public class ShopItemData : ScriptableObject
{
    public string itemName = "Default Item";
    public string itemDescription = "A generic item.";
    public Sprite itemIcon; // Visual representation for UI

    [Tooltip("The base cost for the player to acquire this item (e.g., from a supplier or crafting).")]
    public float basePurchasePrice = 10f;

    [Tooltip("The base price at which the shop will attempt to sell this item to NPCs or other players.")]
    public float baseSellPrice = 15f;

    [Tooltip("The rarity or demand of the item, influencing its chance of being sold in daily operations.")]
    [Range(0.0f, 1.0f)] // A range from 0 (no demand) to 1 (high demand)
    public float demandFactor = 0.5f; 
}


// --- 2. ShopInventorySlot: Serializable class for item instances within a shop ---
// Represents a specific item and its quantity within *any* inventory (player's or shop's).
// Marked [Serializable] so it can be stored in Lists/Arrays within MonoBehaviours or ScriptableObjects
// and shown in the Unity Inspector.
[Serializable]
public class ShopInventorySlot
{
    public ShopItemData itemData; // Reference to the item's definition
    public int quantity;          // How many units of this item
    public float currentSellPrice; // This could dynamically change based on shop upgrades, market events, etc.

    public ShopInventorySlot(ShopItemData data, int qty)
    {
        itemData = data;
        quantity = qty;
        currentSellPrice = data.baseSellPrice; // Initialize with the item's base sell price
    }
}


// --- 3. PlayerInventory: Manages the player's personal money and items ---
// This class encapsulates all logic related to the player's personal economic resources.
// It is kept as a plain C# class for better encapsulation and to avoid GameObject overhead
// for something that is purely data and logic. It's managed by the PlayerOwnedShopsManager.
[Serializable] // Make it serializable to be visible in the PlayerOwnedShopsManager inspector
public class PlayerInventory
{
    public float currentMoney = 1000f; // Starting money for the player

    // Dictionary to store player's personal items: ShopItemData (key) -> quantity (value).
    // Dictionaries are not directly serializable by Unity's inspector without custom property drawers.
    // For this example, we manage it programmatically and provide a public getter.
    private Dictionary<ShopItemData, int> items = new Dictionary<ShopItemData, int>();

    public Dictionary<ShopItemData, int> Items => items; // Public getter for read-only access to the dictionary

    // --- Money Management ---
    public bool CanAfford(float amount) => currentMoney >= amount;

    public void SpendMoney(float amount)
    {
        if (CanAfford(amount))
        {
            currentMoney -= amount;
            Debug.Log($"Player spent ${amount:F2}. Remaining: ${currentMoney:F2}");
        }
        else
        {
            Debug.LogWarning($"Player cannot afford to spend ${amount:F2}. Current money: ${currentMoney:F2}");
        }
    }

    public void GainMoney(float amount)
    {
        currentMoney += amount;
        Debug.Log($"Player gained ${amount:F2}. Current money: ${currentMoney:F2}");
    }

    // --- Item Management ---
    public void AddItem(ShopItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        if (items.ContainsKey(item))
        {
            items[item] += quantity;
        }
        else
        {
            items.Add(item, quantity);
        }
        Debug.Log($"Player added {quantity}x {item.itemName}. Player now has {items[item]}x {item.itemName}.");
    }

    public bool RemoveItem(ShopItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        if (items.ContainsKey(item) && items[item] >= quantity)
        {
            items[item] -= quantity;
            if (items[item] == 0)
            {
                items.Remove(item); // Remove item entry if quantity drops to 0
            }
            Debug.Log($"Player removed {quantity}x {item.itemName}. Player now has {(items.ContainsKey(item) ? items[item] : 0)}x {item.itemName}.");
            return true;
        }
        Debug.LogWarning($"Player does not have enough {item.itemName} to remove {quantity}. Has: {(items.ContainsKey(item) ? items[item] : 0)}");
        return false;
    }

    public int GetItemQuantity(ShopItemData item)
    {
        if (item == null || !items.ContainsKey(item)) return 0;
        return items[item];
    }

    // --- Debugging/Utility ---
    public void PrintInventory()
    {
        string inv = "--- Player Inventory Status ---\n";
        inv += $"Money: ${currentMoney:F2}\n";
        if (items.Count == 0)
        {
            inv += "  (Empty Personal Items)\n";
        }
        else
        {
            foreach (var pair in items)
            {
                inv += $"  - {pair.Key.itemName}: {pair.Value} units\n";
            }
        }
        Debug.Log(inv);
    }
}


// --- 4. PlayerOwnedShop: Represents a single shop owned by the player ---
// This is the core component of the pattern, encapsulating all data and logic for an individual shop.
// It's a [Serializable] class, not a MonoBehaviour, allowing the central manager to hold many
// instances without creating a GameObject for each.
[Serializable]
public class PlayerOwnedShop
{
    public string shopID; // Unique identifier for this shop (e.g., using GUID)
    public string shopName;
    public string ownerPlayerID = "Player_01"; // Identifies which player owns this shop (useful for multi-player)
    public float currentShopBalance; // Money earned *by this specific shop*, distinct from player's personal money

    [Tooltip("Maximum number of different item types this shop can hold in its inventory.")]
    public int maxInventorySlots = 10;
    
    [Tooltip("The daily fixed cost to operate this shop (e.g., rent, staff wages).")]
    public float dailyOperatingCost = 50f;

    [Tooltip("Base percentage chance for each item in stock to sell one unit per day.")]
    [Range(0.01f, 1.0f)]
    public float baseDailySaleChance = 0.3f; // 30% base chance per item type to sell one unit

    // The shop's current inventory of items for sale
    [SerializeField] // Make this list visible and editable in the Inspector
    private List<ShopInventorySlot> inventory = new List<ShopInventorySlot>();

    public List<ShopInventorySlot> Inventory => inventory; // Public getter for the inventory list

    // Constructor for creating a new shop instance
    public PlayerOwnedShop(string name, string id = "")
    {
        shopName = name;
        shopID = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id; // Generate unique ID if not provided
        currentShopBalance = 0f;
    }

    // --- Shop Inventory Management ---

    /// <summary>
    /// Attempts to add an item to the shop's inventory. Used when the player stocks the shop.
    /// </summary>
    /// <param name="itemData">The item definition.</param>
    /// <param name="quantity">The amount to add.</param>
    /// <returns>True if items were added, false otherwise (e.g., inventory full).</returns>
    public bool AddItemToStock(ShopItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning($"[{shopName}] Invalid item data or quantity provided for stocking.");
            return false;
        }

        ShopInventorySlot existingSlot = inventory.Find(slot => slot.itemData == itemData);

        if (existingSlot != null)
        {
            existingSlot.quantity += quantity;
            Debug.Log($"[{shopName}] Stocked {quantity}x {itemData.itemName}. Total: {existingSlot.quantity}.");
            return true;
        }
        else
        {
            // If it's a new item type, check if there's space for a new slot
            if (inventory.Count >= maxInventorySlots)
            {
                Debug.LogWarning($"[{shopName}] Inventory full. Cannot add new item type {itemData.itemName}.");
                return false;
            }
            inventory.Add(new ShopInventorySlot(itemData, quantity));
            Debug.Log($"[{shopName}] Added new item {itemData.itemName} with {quantity} units to stock.");
            return true;
        }
    }

    /// <summary>
    /// Removes an item from the shop's inventory. Used when an item is sold or manually removed by player.
    /// </summary>
    /// <param name="itemData">The item definition.</param>
    /// <param name="quantity">The amount to remove.</param>
    /// <returns>True if items were removed, false otherwise (e.g., not enough stock).</returns>
    public bool RemoveItemFromStock(ShopItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;

        ShopInventorySlot existingSlot = inventory.Find(slot => slot.itemData == itemData);

        if (existingSlot != null && existingSlot.quantity >= quantity)
        {
            existingSlot.quantity -= quantity;
            if (existingSlot.quantity <= 0)
            {
                inventory.Remove(existingSlot); // Remove slot if quantity drops to 0
                Debug.Log($"[{shopName}] Removed all {itemData.itemName} from stock.");
            }
            else
            {
                Debug.Log($"[{shopName}] Removed {quantity}x {itemData.itemName} from stock. Remaining: {existingSlot.quantity}.");
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"[{shopName}] Not enough {itemData.itemName} in stock to remove {quantity}. Has: {(existingSlot != null ? existingSlot.quantity : 0)}.");
            return false;
        }
    }

    // --- Daily Shop Operations ---

    /// <summary>
    /// Simulates a single day's operation for the shop.
    /// Calculates sales, applies daily operating costs, and updates the shop's balance.
    /// This is the core automated part of the 'Player Owned Shops' pattern.
    /// </summary>
    public void ProcessDailyOperation()
    {
        Debug.Log($"\n--- [{shopName}] Daily Operation (ID: {shopID}) ---");

        float dailySalesRevenue = 0f;
        List<ShopInventorySlot> itemsToRemove = new List<ShopInventorySlot>();

        foreach (ShopInventorySlot slot in inventory)
        {
            // The actual chance to sell is influenced by the base chance and the item's demand factor.
            // Higher demandFactor means higher chance of selling.
            float effectiveSaleChance = baseDailySaleChance * slot.itemData.demandFactor;

            if (UnityEngine.Random.value < effectiveSaleChance) // Check if a sale occurs for this item type
            {
                // Determine how many units are sold (e.g., 1 to a few, capped by current stock)
                int quantitySold = UnityEngine.Random.Range(1, Math.Min(slot.quantity + 1, 3)); 
                quantitySold = Math.Min(quantitySold, slot.quantity); // Ensure we don't sell more than available

                if (quantitySold > 0)
                {
                    dailySalesRevenue += quantitySold * slot.currentSellPrice;
                    slot.quantity -= quantitySold;
                    Debug.Log($"[{shopName}] Sold {quantitySold}x {slot.itemData.itemName} for ${quantitySold * slot.currentSellPrice:F2}.");
                }
            }

            if (slot.quantity <= 0)
            {
                itemsToRemove.Add(slot); // Mark out-of-stock items for removal after the loop
            }
        }

        // Remove any items that went out of stock during the daily operation
        foreach (ShopInventorySlot slot in itemsToRemove)
        {
            inventory.Remove(slot);
        }

        // Apply financial changes
        currentShopBalance += dailySalesRevenue; // Add revenue
        currentShopBalance -= dailyOperatingCost; // Subtract fixed daily cost

        Debug.Log($"[{shopName}] Daily Revenue: ${dailySalesRevenue:F2}");
        Debug.Log($"[{shopName}] Daily Operating Cost: ${dailyOperatingCost:F2}");
        Debug.Log($"[{shopName}] Net Daily Change: ${dailySalesRevenue - dailyOperatingCost:F2}");
        Debug.Log($"[{shopName}] New Shop Balance: ${currentShopBalance:F2}");
    }

    /// <summary>
    /// Allows the player to collect the accumulated earnings from this shop, transferring them
    /// from the shop's balance to the player's personal money.
    /// </summary>
    /// <returns>The amount collected.</returns>
    public float CollectEarnings()
    {
        float collected = currentShopBalance;
        currentShopBalance = 0f; // Reset shop balance after collection
        Debug.Log($"[{shopName}] Player collected ${collected:F2} in earnings.");
        return collected;
    }

    // --- Debugging/Utility ---
    public void PrintShopStatus()
    {
        string status = $"--- Shop: {shopName} (ID: {shopID}) ---\n";
        status += $"  Balance: ${currentShopBalance:F2}\n";
        status += $"  Daily Operating Cost: ${dailyOperatingCost:F2}\n";
        status += "  Inventory:\n";
        if (inventory.Count == 0)
        {
            status += "    (Empty Shop Inventory)\n";
        }
        else
        {
            foreach (var slot in inventory)
            {
                status += $"    - {slot.itemData.itemName}: {slot.quantity} units @ ${slot.currentSellPrice:F2} each (Demand: {slot.itemData.demandFactor:P0})\n";
            }
        }
        Debug.Log(status);
    }
}


// --- 5. PlayerOwnedShopsManager: Central manager for all player-owned shops and player interactions ---
// This MonoBehaviour acts as the entry point and orchestrator for the Player Owned Shops system.
// It sits in the scene and manages the collection of PlayerOwnedShop instances and the PlayerInventory.
public class PlayerOwnedShopsManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField]
    private PlayerInventory playerInventory = new PlayerInventory(); // The player's personal inventory and money

    [Header("Player Owned Shops")]
    [SerializeField]
    private List<PlayerOwnedShop> ownedShops = new List<PlayerOwnedShop>(); // List of all shops owned by the player

    // Public access to player inventory and shops (read-only to prevent external modification of the list itself)
    public PlayerInventory PlayerInventory => playerInventory;
    public IReadOnlyList<PlayerOwnedShop> OwnedShops => ownedShops;

    // --- Example Shop Item Data (assign these ScriptableObjects in the Inspector) ---
    // These references are used for demonstration purposes, allowing easy interaction with specific items.
    [Header("Example Shop Item Data (for demo - assign in Inspector)")]
    [SerializeField] private ShopItemData exampleItem1;
    [SerializeField] private ShopItemData exampleItem2;
    [SerializeField] private ShopItemData exampleItem3;

    void Awake()
    {
        // Initialize player inventory if it's null (e.g., if created via constructor or loaded from save)
        if (playerInventory == null)
        {
            playerInventory = new PlayerInventory();
        }

        // For demonstration, add some initial shops if none are set up in the Inspector
        if (ownedShops.Count == 0)
        {
            Debug.Log("No shops found in Inspector. Initializing example shops...");
            AddShop(new PlayerOwnedShop("The Grand Emporium"));
            AddShop(new PlayerOwnedShop("Mystic Potions & Scrolls"));
        }

        // For demonstration, add some initial items to the player's personal inventory
        // (Ensures player has items to stock their shops with)
        if (exampleItem1 != null && playerInventory.GetItemQuantity(exampleItem1) == 0) playerInventory.AddItem(exampleItem1, 5);
        if (exampleItem2 != null && playerInventory.GetItemQuantity(exampleItem2) == 0) playerInventory.AddItem(exampleItem2, 3);
        // Note: exampleItem3 is intentionally not added to player inventory initially to demonstrate buying it.
    }

    void Start()
    {
        Debug.Log("\n--- PlayerOwnedShopsManager Initialized ---");
        playerInventory.PrintInventory();
        foreach (var shop in ownedShops)
        {
            shop.PrintShopStatus();
        }
        Debug.Log("\n--- CONTROLS ---");
        Debug.Log("Press 'D' to simulate a day's operations for all shops.");
        Debug.Log("Press 'S' to stock the first shop with items from player inventory (or buy them first).");
        Debug.Log("Press 'C' to collect profits from the first shop.");
        Debug.Log("Press 'P' to print player inventory and all shop statuses.");
        Debug.Log("Press 'B' to make player buy an item FROM their own shop (example interaction).");
        Debug.Log("Press 'L' to make player sell an item TO their own shop (example interaction).");
    }

    void Update()
    {
        // --- Input-driven Player Actions ---

        // Simulate a day for all owned shops
        if (Input.GetKeyDown(KeyCode.D))
        {
            SimulateDayForOwnedShops();
        }

        // Example player action: Stock shop
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (ownedShops.Count > 0 && exampleItem1 != null && exampleItem2 != null && exampleItem3 != null)
            {
                PlayerOwnedShop firstShop = ownedShops[0];
                Debug.Log($"\n--- Player attempts to stock '{firstShop.shopName}' ---");

                // Stock exampleItem1 (player already has some, let's move 3 units)
                PlayerSellItemToOwnShop(firstShop, exampleItem1, 3); // Player 'sells' to their shop

                // Stock exampleItem3 (player doesn't have it initially, let's simulate player buying it from a 'supplier' then stocking)
                float costToBuyItem3 = exampleItem3.basePurchasePrice * 2; // Price to acquire 2 units of item3
                if (playerInventory.CanAfford(costToBuyItem3))
                {
                    playerInventory.SpendMoney(costToBuyItem3); // Player buys from external source
                    playerInventory.AddItem(exampleItem3, 2); // Player now has them
                    PlayerSellItemToOwnShop(firstShop, exampleItem3, 2); // Player 'sells' them to their shop
                }
                else
                {
                    Debug.LogWarning($"Player cannot afford to buy and stock {exampleItem3.itemName}. Needed: ${costToBuyItem3:F2}");
                }
                
                firstShop.PrintShopStatus();
                playerInventory.PrintInventory();
            }
            else
            {
                Debug.LogWarning("No shops or example items available to demonstrate stocking.");
            }
        }

        // Example player action: Collect profits from a shop
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (ownedShops.Count > 0)
            {
                PlayerOwnedShop firstShop = ownedShops[0];
                float collectedAmount = firstShop.CollectEarnings();
                playerInventory.GainMoney(collectedAmount); // Transfer collected money to player's personal inventory
                Debug.Log($"Player collected ${collectedAmount:F2} from '{firstShop.shopName}'.");
                playerInventory.PrintInventory();
                firstShop.PrintShopStatus();
            }
            else
            {
                Debug.LogWarning("No shops to collect profits from.");
            }
        }

        // Example player action: Player buys item from their own shop
        if (Input.GetKeyDown(KeyCode.B))
        {
             if (ownedShops.Count > 0 && exampleItem1 != null)
            {
                PlayerOwnedShop firstShop = ownedShops[0];
                Debug.Log($"\n--- Player attempts to buy {exampleItem1.itemName} from '{firstShop.shopName}' ---");
                PlayerBuyItemFromOwnShop(firstShop, exampleItem1, 1);
            }
            else
            {
                Debug.LogWarning("No shops or example items available for player to buy from own shop.");
            }
        }

        // Example player action: Player sells item to their own shop (another way to stock)
        if (Input.GetKeyDown(KeyCode.L)) // L for "Load" (into shop)
        {
            if (ownedShops.Count > 0 && exampleItem2 != null)
            {
                PlayerOwnedShop firstShop = ownedShops[0];
                Debug.Log($"\n--- Player attempts to sell {exampleItem2.itemName} to '{firstShop.shopName}' ---");
                PlayerSellItemToOwnShop(firstShop, exampleItem2, 2);
            }
            else
            {
                Debug.LogWarning("No shops or example items available for player to sell to own shop.");
            }
        }

        // Print current status of player and all shops
        if (Input.GetKeyDown(KeyCode.P))
        {
            playerInventory.PrintInventory();
            foreach (var shop in ownedShops)
            {
                shop.PrintShopStatus();
            }
        }
    }

    // --- Shop Management Methods ---

    /// <summary>
    /// Adds a new PlayerOwnedShop instance to the player's ownership.
    /// </summary>
    /// <param name="newShop">The PlayerOwnedShop object to add.</param>
    public void AddShop(PlayerOwnedShop newShop)
    {
        if (newShop == null)
        {
            Debug.LogError("Cannot add a null shop.");
            return;
        }
        if (ownedShops.Exists(s => s.shopID == newShop.shopID))
        {
            Debug.LogWarning($"Shop with ID {newShop.shopID} already exists. Not adding duplicate.");
            return;
        }
        ownedShops.Add(newShop);
        Debug.Log($"Player now owns a new shop: {newShop.shopName} (ID: {newShop.shopID})");
    }

    /// <summary>
    /// Removes a shop from the player's ownership by its unique ID.
    /// </summary>
    /// <param name="shopID">The ID of the shop to remove.</param>
    public void RemoveShop(string shopID)
    {
        PlayerOwnedShop shopToRemove = ownedShops.Find(s => s.shopID == shopID);
        if (shopToRemove != null)
        {
            ownedShops.Remove(shopToRemove);
            Debug.Log($"Shop '{shopToRemove.shopName}' (ID: {shopID}) has been removed from player ownership.");
        }
        else
        {
            Debug.LogWarning($"Shop with ID {shopID} not found to remove.");
        }
    }

    /// <summary>
    /// Retrieves a PlayerOwnedShop instance by its unique ID.
    /// </summary>
    /// <param name="shopID">The ID of the shop to retrieve.</param>
    /// <returns>The PlayerOwnedShop object, or null if not found.</returns>
    public PlayerOwnedShop GetShop(string shopID)
    {
        return ownedShops.Find(s => s.shopID == shopID);
    }

    /// <summary>
    /// Iterates through all owned shops and triggers their daily operation simulation.
    /// This is typically called once per game day or upon a specific event.
    /// </summary>
    public void SimulateDayForOwnedShops()
    {
        Debug.Log("\n=============================================");
        Debug.Log("--- SIMULATING A NEW DAY FOR ALL OWNED SHOPS ---");
        Debug.Log("=============================================");

        foreach (var shop in ownedShops)
        {
            shop.ProcessDailyOperation();
        }
        Debug.Log("\n--- DAY SIMULATION COMPLETE ---");
        playerInventory.PrintInventory(); // Show player money after all shops operate
    }

    // --- Player Interaction Methods (These facilitate player actions with their shops) ---

    /// <summary>
    /// Simulates the player buying an item *from* one of their own shops.
    /// This means the shop earns money, and the player spends money and gains the item.
    /// </summary>
    /// <param name="targetShop">The shop the player is buying from.</param>
    /// <param name="itemData">The item definition.</param>
    /// <param name="quantity">The quantity to buy.</param>
    public void PlayerBuyItemFromOwnShop(PlayerOwnedShop targetShop, ShopItemData itemData, int quantity)
    {
        if (targetShop == null || itemData == null || quantity <= 0) return;

        ShopInventorySlot shopSlot = targetShop.Inventory.Find(s => s.itemData == itemData);
        if (shopSlot == null || shopSlot.quantity < quantity)
        {
            Debug.LogWarning($"Player cannot buy {quantity}x {itemData.itemName} from '{targetShop.shopName}'. Not enough stock.");
            return;
        }

        float price = shopSlot.currentSellPrice * quantity;
        if (playerInventory.CanAfford(price))
        {
            playerInventory.SpendMoney(price);
            targetShop.RemoveItemFromStock(itemData, quantity);
            playerInventory.AddItem(itemData, quantity);
            targetShop.currentShopBalance += price; // The shop earns money from this sale
            Debug.Log($"Player successfully bought {quantity}x {itemData.itemName} from '{targetShop.shopName}' for ${price:F2}.");
            playerInventory.PrintInventory();
            targetShop.PrintShopStatus();
        }
        else
        {
            Debug.LogWarning($"Player cannot afford to buy {quantity}x {itemData.itemName}. Needs ${price:F2}. Has ${playerInventory.currentMoney:F2}.");
        }
    }

    /// <summary>
    /// Simulates the player selling an item *to* one of their own shops.
    /// This means the player gains money, and the shop spends money to acquire the item for its stock.
    /// This is a common way for players to "stock" their shops.
    /// </summary>
    /// <param name="targetShop">The shop the player is selling to.</param>
    /// <param name="itemData">The item definition.</param>
    /// <param name="quantity">The quantity to sell.</param>
    public void PlayerSellItemToOwnShop(PlayerOwnedShop targetShop, ShopItemData itemData, int quantity)
    {
        if (targetShop == null || itemData == null || quantity <= 0) return;

        if (playerInventory.GetItemQuantity(itemData) < quantity)
        {
            Debug.LogWarning($"Player does not have {quantity}x {itemData.itemName} to sell to '{targetShop.shopName}'.");
            return false; // Not enough items in player's inventory
        }

        // For simplicity, the shop pays the item's base purchase price to the player.
        // In a real game, this might be a lower "buy-back" price or a dynamic market price.
        float price = itemData.basePurchasePrice * quantity;

        if (targetShop.currentShopBalance >= price) // Shop must have enough money to buy from player
        {
            playerInventory.RemoveItem(itemData, quantity); // Player gives up items
            targetShop.AddItemToStock(itemData, quantity);  // Shop gains items
            targetShop.currentShopBalance -= price;         // Shop pays money
            playerInventory.GainMoney(price);               // Player gains money
            Debug.Log($"Player successfully sold {quantity}x {itemData.itemName} to '{targetShop.shopName}' for ${price:F2}.");
            playerInventory.PrintInventory();
            targetShop.PrintShopStatus();
            return true;
        }
        else
        {
            Debug.LogWarning($"'{targetShop.shopName}' does not have enough funds (${targetShop.currentShopBalance:F2}) to buy {quantity}x {itemData.itemName} for ${price:F2}.");
            return false;
        }
    }
}

/*
--- How to Use This Example in Unity ---

1.  **Create C# Script:**
    *   Create a new C# script named `PlayerOwnedShopsManager.cs` in your Unity project.
    *   Copy and paste the entire code block above into this script.

2.  **Create Shop Item Data (ScriptableObjects):**
    *   In your Project window, right-click (or go to `Assets -> Create`).
    *   Navigate to `Shop -> Shop Item Data`.
    *   Create at least 3-5 different `ShopItemData` assets (e.g., "Health Potion", "Mana Crystal", "Iron Ore", "Magic Scroll", "Gold Bar").
    *   For each asset, fill in its properties in the Inspector:
        *   `itemName` (e.g., "Health Potion")
        *   `itemDescription`
        *   `itemIcon` (optional, drag a `Sprite` here)
        *   `basePurchasePrice` (what it costs the player to acquire it to stock their shop)
        *   `baseSellPrice` (what the shop tries to sell it for to customers)
        *   `demandFactor` (how likely it is to sell daily, 0.0 to 1.0)

3.  **Setup the Manager in Scene:**
    *   Create an empty GameObject in your scene (e.g., rename it "GameManager").
    *   Attach the `PlayerOwnedShopsManager.cs` script to this "GameManager" GameObject.

4.  **Assign Example Items to the Manager:**
    *   In the Inspector for the "GameManager" object, locate the `Player Owned Shops Manager` component.
    *   Drag and drop the `ShopItemData` assets you created in step 2 into the `Example Item 1`, `Example Item 2`, and `Example Item 3` fields. These are used for demo interaction.

5.  **Run the Scene:**
    *   Play the Unity scene.
    *   Observe the `Debug.Log` output in your Console window. It will show the initial player inventory and shop statuses.

6.  **Interact with the System (using keyboard inputs while the game is running):**
    *   **D:** **Simulate Day.** Triggers `SimulateDayForOwnedShops()`. Each owned shop will process sales, incur costs, and update its balance.
    *   **S:** **Stock First Shop.** Triggers an example of the player stocking the first owned shop with `Example Item 1` (3 units) and `Example Item 3` (2 units). This demonstrates the flow of items from the player's personal inventory to the shop's inventory, potentially involving the player buying items first if they don't possess them.
    *   **C:** **Collect Profits.** Triggers `CollectEarnings()` for the first owned shop. The shop's accumulated balance is transferred to the player's personal money.
    *   **B:** **Player Buy From Own Shop.** Triggers `PlayerBuyItemFromOwnShop()` for the first shop with `Example Item 1`. This shows the player acting as a customer of their own shop.
    *   **L:** **Player Sell To Own Shop.** Triggers `PlayerSellItemToOwnShop()` for the first shop with `Example Item 2`. This is another way for the player to stock their shop, getting paid by the shop.
    *   **P:** **Print Status.** Prints the current status of the player's inventory (money and items) and all owned shops (balance and inventory) to the Console.

---

### Explanation of the PlayerOwnedShops Design Pattern

The PlayerOwnedShops pattern is designed to give players control over in-game businesses, transforming them from mere consumers into economic actors. It typically involves several key components working together:

**1. `ShopItemData` (The Item Catalog):**
*   **Role:** Defines the properties of any item that can be traded (e.g., name, base price, demand). It's a `ScriptableObject` because item definitions are static data, independent of any specific instance of an item in an inventory. This makes it highly reusable and easy for designers to create new items.
*   **How it works:** You create these assets in the Unity Editor, and all shop inventories and player inventories reference these definitions.

**2. `ShopInventorySlot` (The Inventory Slot):**
*   **Role:** Represents an actual instance of an item *within* an inventory. It pairs a `ShopItemData` reference with a `quantity` and potentially instance-specific details like `currentSellPrice`.
*   **How it works:** Used by both `PlayerInventory` (conceptually) and `PlayerOwnedShop` to manage their holdings. It's `[Serializable]` to be visible in the Inspector within `List`s.

**3. `PlayerInventory` (Player's Personal Capital & Resources):**
*   **Role:** Manages the player character's personal finances (`currentMoney`) and the items they physically carry (`items`). This is crucial for distinguishing between the player's personal wealth and the wealth of their businesses.
*   **How it works:** Contains methods for gaining/spending money and adding/removing items. It's a plain C# class, instantiated and managed by the `PlayerOwnedShopsManager`, avoiding unnecessary `GameObject`s.

**4. `PlayerOwnedShop` (The Shop Instance):**
*   **Role:** This is the heart of the pattern. Each instance of `PlayerOwnedShop` represents a single business owned by the player. It encapsulates all the specific data for that shop (name, ID, balance, inventory, operating costs) and the core logic for its daily operations.
*   **How it works:**
    *   It has its own `currentShopBalance`, distinct from the player's money. Profits accumulate here.
    *   Its `inventory` stores `ShopInventorySlot`s of items it has for sale.
    *   `ProcessDailyOperation()` simulates sales (based on item demand and a random chance) and subtracts `dailyOperatingCost`.
    *   `CollectEarnings()` allows the player to transfer profits from the shop's balance to their personal inventory.
*   **Design Choice:** It's a `[Serializable]` class, not a `MonoBehaviour`, to allow the `PlayerOwnedShopsManager` to easily hold a collection of them without each shop needing its own `GameObject`.

**5. `PlayerOwnedShopsManager` (The Shop Management System):**
*   **Role:** The central `MonoBehaviour` in the scene. It acts as the orchestrator, managing the collection of all `PlayerOwnedShop` instances and the `PlayerInventory`. It handles player input related to shops and triggers the daily operations.
*   **How it works:**
    *   Holds a `List<PlayerOwnedShop>` and a `PlayerInventory` instance.
    *   Provides methods to `AddShop`, `RemoveShop`, and `GetShop`.
    *   The `SimulateDayForOwnedShops()` method iterates through all owned shops and calls their `ProcessDailyOperation()`.
    *   Facilitates direct player interactions like `PlayerBuyItemFromOwnShop()` and `PlayerSellItemToOwnShop()`.

**Workflow of the Pattern:**

1.  **Setup:** Designers create `ShopItemData` assets. The `PlayerOwnedShopsManager` `MonoBehaviour` is placed in the scene, and `PlayerOwnedShop` instances are created (either in the Inspector or programmatically). The `PlayerInventory` is initialized.
2.  **Player Management:** The player interacts with the `PlayerOwnedShopsManager` to:
    *   **Acquire New Shops:** Expand their business empire.
    *   **Stock Shops:** Move items from their personal `PlayerInventory` into a `PlayerOwnedShop`'s `inventory`. This usually costs the player money (if buying from external suppliers) or uses items they've gathered.
    *   **Collect Profits:** Transfer money from a `PlayerOwnedShop`'s `currentShopBalance` to their `PlayerInventory.currentMoney`.
    *   (Optionally) **Upgrade Shops:** Improve `maxInventorySlots`, reduce `dailyOperatingCost`, or adjust `currentSellPrice` for items.
3.  **Automated Operation:** At regular intervals (e.g., every game day, triggered by the `PlayerOwnedShopsManager`), each `PlayerOwnedShop` automatically:
    *   Attempts to sell items from its `inventory` to simulated customers (NPCs).
    *   Incurs its `dailyOperatingCost`.
    *   Updates its `currentShopBalance` based on sales revenue minus costs.

**Benefits of This Pattern:**

*   **Modularity and Encapsulation:** Each shop is a self-contained unit, and player inventory is separate.
*   **Extensibility:** Easy to add new item types (via `ShopItemData` ScriptableObjects) or new shop features/upgrades without altering core logic.
*   **Scalability:** The `PlayerOwnedShopsManager` can efficiently handle a large number of `PlayerOwnedShop` instances.
*   **Clear Separation of Concerns:** Each class has a distinct responsibility, leading to cleaner, more maintainable code.
*   **Engaging Gameplay:** Provides a sandbox for players to engage with economic systems, offering a sense of ownership and progression beyond typical combat or questing.