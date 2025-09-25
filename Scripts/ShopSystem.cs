// Unity Design Pattern Example: ShopSystem
// This script demonstrates the ShopSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a complete and practical C# Unity implementation of the 'ShopSystem' design pattern. It includes separate components for items, player inventory, and the shop logic itself, all communicating through events to ensure a flexible and scalable architecture.

**Key Design Pattern Elements Demonstrated:**

*   **Encapsulation:** The `ShopManager` encapsulates all the business logic for buying and selling. `PlayerInventory` encapsulates player-specific data and operations.
*   **Separation of Concerns:**
    *   `ShopItem` (ScriptableObject) defines item data.
    *   `PlayerInventory` manages player currency and owned items.
    *   `ShopManager` orchestrates transactions between the player and the shop.
    *   (Conceptual) UI elements would only interact with the `ShopManager` and subscribe to its events, without needing to know internal logic.
*   **Observer Pattern (via UnityEvents):** The `ShopManager` uses `UnityEvent`s to notify other systems (like UI) when transactions occur, player currency changes, or errors happen. This decouples the shop logic from its presentation.
*   **Data-Driven Design:** `ShopItem`s are `ScriptableObject`s, allowing game designers to create and configure items directly in the Unity Editor without touching code.

---

### **1. `ShopItem.cs` (ScriptableObject for Item Definitions)**

This ScriptableObject defines the properties of an item that can be bought or sold in the shop. It's a data container, making items easy to create and manage in the Unity Editor.

```csharp
using UnityEngine;

/// <summary>
/// A ScriptableObject representing an item that can be sold in a shop.
/// This allows designers to create items as assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop System/Shop Item")]
public class ShopItem : ScriptableObject
{
    // Unique identifier for the item. Can be used for saving/loading or lookup.
    public string ItemID => _itemID;
    [SerializeField] private string _itemID = System.Guid.NewGuid().ToString(); // Auto-generate GUID

    // Display name of the item.
    public string ItemName => _itemName;
    [SerializeField] private string _itemName = "New Item";

    // Description of the item.
    public string Description => _description;
    [TextArea(3, 5)]
    [SerializeField] private string _description = "A description for the item.";

    // The price at which the shop sells this item to the player.
    public int Price => _price;
    [SerializeField] private int _price = 100;

    // The price at which the player can sell this item back to the shop.
    // Often a fraction of the buy price, or 0 if not sellable.
    public int SellPrice => _sellPrice;
    [SerializeField] private int _sellPrice = 50;

    // Icon for displaying the item in UI.
    public Sprite Icon => _icon;
    [SerializeField] private Sprite _icon;

    // Determines if the player can sell this item back to the shop.
    public bool CanBeSoldBack => _canBeSoldBack;
    [SerializeField] private bool _canBeSoldBack = true;

    // Optional: Max stack size for this item if inventory supports stacking.
    // For this example, we'll keep it simple and assume unique items or multiple entries for stacks.
    // public int MaxStackSize => _maxStackSize;
    // [SerializeField] private int _maxStackSize = 1;

    // --- Editor-only functionality for GUID generation ---
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure the ItemID is unique and set when a new asset is created or duplicated.
        if (string.IsNullOrEmpty(_itemID) || UnityEditor.AssetDatabase.IsSubAsset(_itemID))
        {
            _itemID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this); // Mark asset dirty to save changes
        }
    }

    // This method can be used to manually generate a new GUID if needed, e.g., after duplication.
    public void GenerateNewItemID()
    {
        _itemID = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
```

---

### **2. `PlayerInventory.cs` (Manages Player's Assets)**

This `MonoBehaviour` manages the player's currency and their collection of `ShopItem`s. It's a central place for player-specific economic data.

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent

/// <summary>
/// Manages the player's currency and owned items.
/// This acts as the player's wallet and backpack.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // Singleton pattern for easy access throughout the game.
    // In a larger project, consider a Service Locator or Dependency Injection.
    public static PlayerInventory Instance { get; private set; }

    // Current amount of currency the player has.
    public int Currency => _currency;
    [SerializeField] private int _currency = 1000; // Starting currency

    // List of items currently owned by the player.
    // For simplicity, we store ShopItem directly. For stacks, use a Dictionary<ShopItem, int>.
    public List<ShopItem> OwnedItems => _ownedItems;
    private List<ShopItem> _ownedItems = new List<ShopItem>();

    // --- Events to notify other systems (e.g., UI) of changes ---
    public UnityEvent<int> OnCurrencyChanged = new UnityEvent<int>();
    public UnityEvent OnInventoryChanged = new UnityEvent();

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, DontDestroyOnLoad(gameObject); if this needs to persist across scenes.
        }
    }

    private void Start()
    {
        // Notify initial currency state
        OnCurrencyChanged.Invoke(_currency);
        // Add some example items for testing
        // You would typically load these from a save file
        // Example: AddInitialItemsForTesting();
    }

    /// <summary>
    /// Adds currency to the player's wallet.
    /// </summary>
    /// <param name="amount">The amount of currency to add.</param>
    public void AddCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to add negative currency. Use RemoveCurrency instead.");
            return;
        }
        _currency += amount;
        OnCurrencyChanged.Invoke(_currency);
        Debug.Log($"Added {amount} currency. Total: {_currency}");
    }

    /// <summary>
    /// Removes currency from the player's wallet.
    /// </summary>
    /// <param name="amount">The amount of currency to remove.</param>
    /// <returns>True if currency was successfully removed, false if not enough currency.</returns>
    public bool RemoveCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to remove negative currency. Use AddCurrency instead.");
            return false;
        }
        if (_currency >= amount)
        {
            _currency -= amount;
            OnCurrencyChanged.Invoke(_currency);
            Debug.Log($"Removed {amount} currency. Total: {_currency}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough currency. Tried to remove {amount}, but only have {_currency}.");
            return false;
        }
    }

    /// <summary>
    /// Checks if the player has enough currency for a given amount.
    /// </summary>
    /// <param name="amount">The required currency amount.</param>
    /// <returns>True if the player has enough currency, false otherwise.</returns>
    public bool HasEnoughCurrency(int amount)
    {
        return _currency >= amount;
    }

    /// <summary>
    /// Adds an item to the player's inventory.
    /// </summary>
    /// <param name="item">The ShopItem to add.</param>
    public void AddItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogError("Attempted to add a null item to inventory.");
            return;
        }
        _ownedItems.Add(item);
        OnInventoryChanged.Invoke();
        Debug.Log($"Added item: {item.ItemName} to inventory.");
    }

    /// <summary>
    /// Removes an item from the player's inventory.
    /// </summary>
    /// <param name="item">The ShopItem to remove.</param>
    /// <returns>True if the item was found and removed, false otherwise.</returns>
    public bool RemoveItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogError("Attempted to remove a null item from inventory.");
            return false;
        }
        bool removed = _ownedItems.Remove(item);
        if (removed)
        {
            OnInventoryChanged.Invoke();
            Debug.Log($"Removed item: {item.ItemName} from inventory.");
        }
        else
        {
            Debug.LogWarning($"Item: {item.ItemName} not found in inventory.");
        }
        return removed;
    }

    /// <summary>
    /// Checks if the player owns a specific item.
    /// </summary>
    /// <param name="item">The ShopItem to check for.</param>
    /// <returns>True if the player owns the item, false otherwise.</returns>
    public bool HasItem(ShopItem item)
    {
        return _ownedItems.Contains(item);
    }
}
```

---

### **3. `ShopManager.cs` (The Core ShopSystem Logic)**

This `MonoBehaviour` is the heart of the shop system. It handles all the business logic for buying and selling items, interacting with the `PlayerInventory` and notifying other parts of the game via events.

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent

/// <summary>
/// The central component of the Shop System design pattern.
/// This class manages the items available for sale, processes transactions (buy/sell),
/// and interacts with the PlayerInventory. It uses UnityEvents to notify other
/// systems (e.g., UI) about transaction outcomes.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // Singleton pattern for easy access.
    public static ShopManager Instance { get; private set; }

    [Header("Shop Configuration")]
    [Tooltip("The list of items this specific shop offers for sale.")]
    [SerializeField] private List<ShopItem> _availableShopItems = new List<ShopItem>();

    [Tooltip("Reference to the player's inventory system. Will try to find if not set.")]
    [SerializeField] private PlayerInventory _playerInventory;

    // --- Events for UI and other systems to subscribe to ---
    // Notifies when a transaction is successful, includes a message.
    public UnityEvent<string> OnShopTransactionSuccess = new UnityEvent<string>();
    // Notifies when a transaction fails, includes an error message.
    public UnityEvent<string> OnShopTransactionFailed = new UnityEvent<string>();
    // Notifies when the list of available shop items changes (e.g., item sells out, new stock)
    public UnityEvent OnAvailableShopItemsChanged = new UnityEvent();

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Ensure player inventory reference is set.
        if (_playerInventory == null)
        {
            _playerInventory = FindObjectOfType<PlayerInventory>();
            if (_playerInventory == null)
            {
                Debug.LogError("ShopManager could not find PlayerInventory in the scene! " +
                               "Please create a GameObject with PlayerInventory script, or assign it manually.");
                enabled = false; // Disable shop if no inventory
                return;
            }
        }

        Debug.Log($"ShopManager initialized with {_availableShopItems.Count} items.");
        OnAvailableShopItemsChanged.Invoke(); // Notify initial shop state
    }

    /// <summary>
    /// Gets a read-only list of items currently available for sale in this shop.
    /// </summary>
    /// <returns>A list of ShopItem objects.</returns>
    public IReadOnlyList<ShopItem> GetAvailableShopItems()
    {
        return _availableShopItems.AsReadOnly();
    }

    /// <summary>
    /// Processes a request for the player to buy an item from the shop.
    /// </summary>
    /// <param name="itemToBuy">The ShopItem the player wishes to purchase.</param>
    public void BuyItem(ShopItem itemToBuy)
    {
        if (itemToBuy == null)
        {
            OnShopTransactionFailed.Invoke("Invalid item specified.");
            Debug.LogError("Attempted to buy a null item.");
            return;
        }

        // 1. Check if the item is actually for sale in THIS shop.
        if (!_availableShopItems.Contains(itemToBuy))
        {
            OnShopTransactionFailed.Invoke($"'{itemToBuy.ItemName}' is not available in this shop.");
            Debug.LogWarning($"Shop does not sell item: {itemToBuy.ItemName}");
            return;
        }

        // 2. Check if the player has enough currency.
        if (!_playerInventory.HasEnoughCurrency(itemToBuy.Price))
        {
            OnShopTransactionFailed.Invoke($"Not enough currency to buy '{itemToBuy.ItemName}'. Price: {itemToBuy.Price}");
            Debug.Log($"Player tried to buy {itemToBuy.ItemName} for {itemToBuy.Price} but only has {_playerInventory.Currency}");
            return;
        }

        // 3. Process the transaction.
        // Remove currency first to ensure atomicity (player can't exploit by spending more than they have).
        if (_playerInventory.RemoveCurrency(itemToBuy.Price))
        {
            _playerInventory.AddItem(itemToBuy); // Add item to player's inventory

            // Optional: If items are unique/limited stock, remove from shop's available items
            // _availableShopItems.Remove(itemToBuy); 
            // OnAvailableShopItemsChanged.Invoke(); // Notify UI if shop stock changed

            OnShopTransactionSuccess.Invoke($"Successfully bought '{itemToBuy.ItemName}' for {itemToBuy.Price}!");
            Debug.Log($"Player bought {itemToBuy.ItemName} for {itemToBuy.Price}. Remaining currency: {_playerInventory.Currency}");
        }
        else
        {
            // This case should ideally not be reached due to HasEnoughCurrency check,
            // but acts as a safeguard.
            OnShopTransactionFailed.Invoke($"Failed to process purchase of '{itemToBuy.ItemName}'.");
            Debug.LogError("Unexpected error during currency removal for purchase.");
        }
    }

    /// <summary>
    /// Processes a request for the player to sell an item to the shop.
    /// </summary>
    /// <param name="itemToSell">The ShopItem the player wishes to sell.</param>
    public void SellItem(ShopItem itemToSell)
    {
        if (itemToSell == null)
        {
            OnShopTransactionFailed.Invoke("Invalid item specified.");
            Debug.LogError("Attempted to sell a null item.");
            return;
        }

        // 1. Check if the item can actually be sold back to the shop.
        if (!itemToSell.CanBeSoldBack)
        {
            OnShopTransactionFailed.Invoke($"'{itemToSell.ItemName}' cannot be sold back to the shop.");
            Debug.LogWarning($"Item {itemToSell.ItemName} cannot be sold back.");
            return;
        }

        // 2. Check if the player actually owns the item.
        if (!_playerInventory.HasItem(itemToSell))
        {
            OnShopTransactionFailed.Invoke($"You do not own '{itemToSell.ItemName}' to sell.");
            Debug.Log($"Player tried to sell {itemToSell.ItemName} but does not own it.");
            return;
        }

        // 3. Process the transaction.
        if (_playerInventory.RemoveItem(itemToSell)) // Remove item from player's inventory first
        {
            _playerInventory.AddCurrency(itemToSell.SellPrice); // Add currency to player

            // Optional: If the shop 'buys back' items, potentially add it to shop's available items
            // _availableShopItems.Add(itemToSell);
            // OnAvailableShopItemsChanged.Invoke(); // Notify UI if shop stock changed

            OnShopTransactionSuccess.Invoke($"Successfully sold '{itemToSell.ItemName}' for {itemToSell.SellPrice}!");
            Debug.Log($"Player sold {itemToSell.ItemName} for {itemToSell.SellPrice}. Remaining currency: {_playerInventory.Currency}");
        }
        else
        {
            // This case should ideally not be reached due to HasItem check,
            // but acts as a safeguard.
            OnShopTransactionFailed.Invoke($"Failed to process sale of '{itemToSell.ItemName}'.");
            Debug.LogError("Unexpected error during item removal for sale.");
        }
    }
}
```

---

### **Example Usage (How a UI would interact)**

This is not a runnable script but shows how you would connect UI elements (buttons, text) to the ShopSystem.

```csharp
/*
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming you are using TextMeshPro for UI text

/// <summary>
/// This is a conceptual example of a UI script that interacts with the ShopSystem.
/// You would attach this to your UI panel that displays shop items.
/// </summary>
public class ShopDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopItemUIPrefab; // A prefab for displaying a single shop item
    public Transform shopContentParent; // Parent transform for shop items (e.g., a ScrollView content)
    public Transform playerInventoryContentParent; // Parent transform for player inventory items

    public TextMeshProUGUI playerCurrencyText;
    public TextMeshProUGUI statusMessageText; // For displaying transaction success/failure

    private ShopManager _shopManager;
    private PlayerInventory _playerInventory;

    private void OnEnable()
    {
        // Get references to the singletons
        _shopManager = ShopManager.Instance;
        _playerInventory = PlayerInventory.Instance;

        if (_shopManager == null || _playerInventory == null)
        {
            Debug.LogError("ShopManager or PlayerInventory not found! Ensure they exist in the scene.");
            gameObject.SetActive(false); // Disable UI if dependencies are missing
            return;
        }

        // Subscribe to events
        _shopManager.OnShopTransactionSuccess.AddListener(OnTransactionSuccess);
        _shopManager.OnShopTransactionFailed.AddListener(OnTransactionFailed);
        _shopManager.OnAvailableShopItemsChanged.AddListener(RefreshShopUI);
        _playerInventory.OnCurrencyChanged.AddListener(UpdateCurrencyUI);
        _playerInventory.OnInventoryChanged.AddListener(RefreshPlayerInventoryUI);

        // Initial UI setup
        RefreshShopUI();
        RefreshPlayerInventoryUI();
        UpdateCurrencyUI(_playerInventory.Currency);
        statusMessageText.text = ""; // Clear initial status
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_shopManager != null)
        {
            _shopManager.OnShopTransactionSuccess.RemoveListener(OnTransactionSuccess);
            _shopManager.OnShopTransactionFailed.RemoveListener(OnTransactionFailed);
            _shopManager.OnAvailableShopItemsChanged.RemoveListener(RefreshShopUI);
        }
        if (_playerInventory != null)
        {
            _playerInventory.OnCurrencyChanged.RemoveListener(UpdateCurrencyUI);
            _playerInventory.OnInventoryChanged.RemoveListener(RefreshPlayerInventoryUI);
        }
    }

    private void RefreshShopUI()
    {
        // Clear existing items
        foreach (Transform child in shopContentParent)
        {
            Destroy(child.gameObject);
        }

        // Populate shop items
        foreach (ShopItem item in _shopManager.GetAvailableShopItems())
        {
            GameObject itemUI = Instantiate(shopItemUIPrefab, shopContentParent);
            // Assuming your itemUIPrefab has components to display item details
            // Example:
            // itemUI.GetComponentInChildren<TextMeshProUGUI>("ItemNameText").text = item.ItemName;
            // itemUI.GetComponentInChildren<TextMeshProUGUI>("PriceText").text = item.Price.ToString();
            // itemUI.GetComponentInChildren<Image>("ItemIcon").sprite = item.Icon;

            // Add button listener for buying
            Button buyButton = itemUI.GetComponentInChildren<Button>("BuyButton"); // Assuming a button named "BuyButton"
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => _shopManager.BuyItem(item));
            }
        }
    }

    private void RefreshPlayerInventoryUI()
    {
        // Clear existing items
        foreach (Transform child in playerInventoryContentParent)
        {
            Destroy(child.gameObject);
        }

        // Populate player's owned items
        foreach (ShopItem item in _playerInventory.OwnedItems)
        {
            GameObject itemUI = Instantiate(shopItemUIPrefab, playerInventoryContentParent); // Re-using same prefab for simplicity
            // Assuming your itemUIPrefab has components to display item details
            // itemUI.GetComponentInChildren<TextMeshProUGUI>("ItemNameText").text = item.ItemName;
            // itemUI.GetComponentInChildren<TextMeshProUGUI>("PriceText").text = item.SellPrice.ToString(); // Show sell price
            // itemUI.GetComponentInChildren<Image>("ItemIcon").sprite = item.Icon;

            // Add button listener for selling
            Button sellButton = itemUI.GetComponentInChildren<Button>("SellButton"); // Assuming a button named "SellButton"
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(() => _shopManager.SellItem(item));
                sellButton.interactable = item.CanBeSoldBack; // Only allow selling if item is sellable
            }
        }
    }

    private void UpdateCurrencyUI(int newCurrency)
    {
        if (playerCurrencyText != null)
        {
            playerCurrencyText.text = $"Currency: {newCurrency}";
        }
    }

    private void OnTransactionSuccess(string message)
    {
        Debug.Log($"Shop Transaction Success: {message}");
        statusMessageText.text = message;
        // Optionally, hide message after a delay
        // Invoke("ClearStatusMessage", 3f);
    }

    private void OnTransactionFailed(string errorMessage)
    {
        Debug.LogWarning($"Shop Transaction Failed: {errorMessage}");
        statusMessageText.text = $"Error: {errorMessage}";
        // Optionally, hide message after a delay
        // Invoke("ClearStatusMessage", 3f);
    }

    private void ClearStatusMessage()
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = "";
        }
    }

    // Example of how to open/close the shop UI
    public void OpenShop()
    {
        gameObject.SetActive(true);
        RefreshShopUI(); // Ensure it's up-to-date when opened
        RefreshPlayerInventoryUI();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }
}
*/
```

---

### **How to Set Up in Unity:**

1.  **Create C# Scripts:**
    *   Save the `ShopItem.cs`, `PlayerInventory.cs`, and `ShopManager.cs` code into separate C# script files in your Unity project (e.g., in a `Scripts/ShopSystem` folder).

2.  **Create ShopItem Assets:**
    *   In the Unity Editor, go to `Assets -> Create -> Shop System -> Shop Item`.
    *   Create a few `ShopItem` assets (e.g., "Health Potion", "Iron Sword", "Magic Orb").
    *   Fill in their `Item Name`, `Description`, `Price`, `Sell Price`, and assign an `Icon` (any `Sprite` will do for testing). Set `Can Be Sold Back` as desired.

3.  **Create `PlayerInventory` GameObject:**
    *   Create an empty GameObject in your scene (e.g., named "PlayerManager").
    *   Attach the `PlayerInventory.cs` script to it.
    *   Adjust the initial `Currency` value in the Inspector if needed.

4.  **Create `ShopManager` GameObject:**
    *   Create another empty GameObject (e.g., named "ShopSystem").
    *   Attach the `ShopManager.cs` script to it.
    *   **Drag and Drop Items:** In the Inspector for `ShopSystem`, you'll see a list called `Available Shop Items`. Drag and drop the `ShopItem` assets you created in step 2 into this list. These are the items this shop will sell.
    *   The `Player Inventory` field should auto-populate if `PlayerInventory` is present in the scene, otherwise, drag your "PlayerManager" GameObject (or the one with `PlayerInventory`) here.

5.  **(Optional) Implement UI:**
    *   Create a UI Canvas.
    *   Design your shop UI (buttons, text fields for item names, prices, player currency, etc.).
    *   Create a prefab for a single shop item display that includes buttons for "Buy" and "Sell".
    *   Create a new C# script (e.g., `ShopDisplayUI.cs` as shown in the commented example) and attach it to your main shop UI panel. Connect the UI elements and set up the button listeners as demonstrated.
    *   Remember to activate/deactivate your shop UI panel to simulate opening/closing the shop.

**How it works together:**

*   When the game starts, `PlayerInventory` and `ShopManager` initialize.
*   The `ShopDisplayUI` (if implemented) would call `ShopManager.Instance.GetAvailableShopItems()` to populate its display.
*   When a player clicks a "Buy" button in the UI, it calls `ShopManager.Instance.BuyItem(selectedItem)`.
*   `ShopManager` performs all necessary checks (currency, item availability).
*   If successful, `ShopManager` tells `PlayerInventory` to `RemoveCurrency()` and `AddItem()`.
*   Both `ShopManager` and `PlayerInventory` then `Invoke()` their respective `UnityEvent`s (`OnShopTransactionSuccess`, `OnCurrencyChanged`, `OnInventoryChanged`).
*   The `ShopDisplayUI` (or any other subscriber) listens to these events and updates its display accordingly (e.g., refreshes currency text, redraws inventory, shows success message).
*   Selling works similarly, but in reverse.

This setup provides a robust, extensible, and easy-to-use shop system for your Unity projects, adhering to common design patterns and best practices.