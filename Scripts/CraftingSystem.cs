// Unity Design Pattern Example: CraftingSystem
// This script demonstrates the CraftingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **Crafting System design pattern**, making it practical and educational. It includes data definitions using ScriptableObjects, core logic, inventory management, and a simple demonstration script.

---

### Project Setup in Unity

1.  Create a new Unity project.
2.  Create a folder structure:
    *   `Assets/Scripts/CraftingSystem/`
    *   `Assets/Scripts/Demo/`
    *   `Assets/ScriptableObjects/Items/`
    *   `Assets/ScriptableObjects/Recipes/`
3.  Place the following C# scripts into `Assets/Scripts/CraftingSystem/`.
4.  Place `CraftingDemo.cs` into `Assets/Scripts/Demo/`.
5.  Create a new empty GameObject in your scene named `GameManager`.
6.  Attach the `InventoryManager.cs` and `CraftingManager.cs` scripts to the `GameManager` GameObject.
7.  Create another empty GameObject in your scene named `CraftingDemo`.
8.  Attach the `CraftingDemo.cs` script to the `CraftingDemo` GameObject.

---

### 1. `Item.cs` (ScriptableObject - Data Definition)

This defines a base item. All items in your game (resources, tools, etc.) will be instances of this ScriptableObject.

```csharp
using UnityEngine;
using System;

namespace CraftingSystem
{
    // ItemType enum can be used to categorize items for various purposes (e.g., filtering in UI)
    public enum ItemType
    {
        Resource,
        Tool,
        Consumable,
        Equipment,
        Craftable
    }

    /// <summary>
    /// Represents a generic item in the game. This is a ScriptableObject,
    /// allowing us to define item data assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Crafting System/Item")]
    public class Item : ScriptableObject
    {
        [Header("Item Core Data")]
        [Tooltip("A unique identifier for this item.")]
        public string id;

        [Tooltip("The display name of the item.")]
        public string displayName;

        [Tooltip("An icon to represent the item in UI.")]
        public Sprite icon;

        [Tooltip("A brief description of the item.")]
        [TextArea]
        public string description;

        [Tooltip("The type of this item.")]
        public ItemType itemType = ItemType.Resource;

        [Tooltip("The maximum quantity of this item that can stack in an inventory slot.")]
        public int maxStackSize = 99;

        // You can add more properties here as needed, e.g., weight, rarity, sell value, etc.

        private void OnValidate()
        {
            // Ensure ID is set and unique if possible (more robust validation would check against all existing items)
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                Debug.LogWarning($"Item '{displayName}' had no ID, a new one was generated: {id}");
            }
        }
    }
}
```

---

### 2. `CraftingRecipe.cs` (ScriptableObject - Data Definition)

This defines what ingredients are needed to craft a specific output item. Each recipe is an instance of this ScriptableObject.

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace CraftingSystem
{
    /// <summary>
    /// Represents an ingredient required for a crafting recipe.
    /// </summary>
    [System.Serializable] // Make it serializable so it appears in the inspector
    public class RecipeIngredient
    {
        public Item item;
        [Min(1)] public int quantity;
    }

    /// <summary>
    /// Defines a crafting recipe, specifying required ingredients and the output item.
    /// This is a ScriptableObject, allowing us to define recipe data assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting System/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Recipe Definition")]
        [Tooltip("A user-friendly name for this recipe.")]
        public string recipeName;

        [Tooltip("The list of items required as ingredients.")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

        [Header("Output Item")]
        [Tooltip("The item that will be produced by crafting this recipe.")]
        public Item outputItem;

        [Tooltip("The quantity of the output item produced.")]
        [Min(1)] public int outputQuantity = 1;

        /// <summary>
        /// Provides a summary string for debugging or UI display.
        /// </summary>
        public string GetRecipeSummary()
        {
            if (outputItem == null) return "Invalid Recipe (No Output Item)";

            string summary = $"{recipeName} (Crafts {outputQuantity}x {outputItem.displayName}):\n";
            foreach (var ingredient in ingredients)
            {
                if (ingredient.item != null)
                {
                    summary += $"  - {ingredient.quantity}x {ingredient.item.displayName}\n";
                }
                else
                {
                    summary += "  - Missing Ingredient Item!\n";
                }
            }
            return summary;
        }
    }
}
```

---

### 3. `InventoryManager.cs` (Core System - MonoBehaviour)

Manages the player's items. The `CraftingManager` will use this to check for ingredients and add/remove items.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CraftingSystem
{
    /// <summary>
    /// Manages the player's inventory, storing items and their quantities.
    /// This acts as a central repository for items, allowing other systems
    /// (like the CraftingManager) to interact with the player's possessions.
    /// Implemented as a Singleton for easy global access.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        // Singleton pattern implementation
        public static InventoryManager Instance { get; private set; }

        // Dictionary to store the actual inventory contents: Item -> Quantity
        private Dictionary<Item, int> _inventoryContents = new Dictionary<Item, int>();

        // Event to notify other systems when the inventory changes.
        // Useful for updating UI, enabling/disabling crafting buttons, etc.
        public event Action OnInventoryChanged;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of InventoryManager found, destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optionally, if you want this to persist across scenes:
            // DontDestroyOnLoad(gameObject);

            Debug.Log("InventoryManager initialized.");
        }

        /// <summary>
        /// Adds a specified quantity of an item to the inventory.
        /// </summary>
        /// <param name="item">The item ScriptableObject to add.</param>
        /// <param name="quantity">The amount of the item to add. Must be positive.</param>
        public void AddItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"Attempted to add invalid item or quantity: Item={(item != null ? item.displayName : "NULL")}, Quantity={quantity}");
                return;
            }

            if (_inventoryContents.ContainsKey(item))
            {
                _inventoryContents[item] += quantity;
            }
            else
            {
                _inventoryContents.Add(item, quantity);
            }

            Debug.Log($"Added {quantity}x {item.displayName} to inventory. Total: {_inventoryContents[item]}");
            OnInventoryChanged?.Invoke(); // Notify listeners
        }

        /// <summary>
        /// Removes a specified quantity of an item from the inventory.
        /// </summary>
        /// <param name="item">The item ScriptableObject to remove.</param>
        /// <param name="quantity">The amount of the item to remove. Must be positive.</param>
        /// <returns>True if items were successfully removed, false otherwise (e.g., not enough items).</returns>
        public bool RemoveItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"Attempted to remove invalid item or quantity: Item={(item != null ? item.displayName : "NULL")}, Quantity={quantity}");
                return false;
            }

            if (_inventoryContents.TryGetValue(item, out int currentQuantity))
            {
                if (currentQuantity >= quantity)
                {
                    _inventoryContents[item] -= quantity;
                    if (_inventoryContents[item] == 0)
                    {
                        _inventoryContents.Remove(item); // Remove entry if quantity drops to zero
                    }
                    Debug.Log($"Removed {quantity}x {item.displayName} from inventory. Remaining: {GetItemQuantity(item)}");
                    OnInventoryChanged?.Invoke(); // Notify listeners
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Not enough {item.displayName} to remove {quantity}. Only {currentQuantity} available.");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"Attempted to remove {item.displayName}, but it's not in inventory.");
                return false;
            }
        }

        /// <summary>
        /// Checks if the inventory contains at least the specified quantity of an item.
        /// </summary>
        /// <param name="item">The item ScriptableObject to check for.</param>
        /// <param name="quantity">The required quantity.</param>
        /// <returns>True if the required quantity is present, false otherwise.</returns>
        public bool HasItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            return _inventoryContents.TryGetValue(item, out int currentQuantity) && currentQuantity >= quantity;
        }

        /// <summary>
        /// Gets the current quantity of a specific item in the inventory.
        /// </summary>
        /// <param name="item">The item ScriptableObject to query.</param>
        /// <returns>The quantity of the item, or 0 if not present.</returns>
        public int GetItemQuantity(Item item)
        {
            if (item == null) return 0;
            return _inventoryContents.TryGetValue(item, out int quantity) ? quantity : 0;
        }

        /// <summary>
        /// Returns a read-only dictionary of the current inventory contents.
        /// </summary>
        public IReadOnlyDictionary<Item, int> GetInventoryContents()
        {
            return _inventoryContents;
        }

        // --- Editor Context Menu for Debugging ---
        // These methods can be called directly from the Unity Editor's inspector
        // when InventoryManager GameObject is selected, useful for testing.
        [ContextMenu("Debug: Add 5 Wood")]
        private void DebugAddWood()
        {
            // You would normally reference specific Item SOs here
            // For this example, let's assume you have a 'Wood' item asset
            // In a real project, you'd load it from Resources or a public field.
            Item woodItem = Resources.Load<Item>("ScriptableObjects/Items/Wood"); // Example path
            if (woodItem != null) AddItem(woodItem, 5);
            else Debug.LogError("Wood item not found at 'Resources/ScriptableObjects/Items/Wood'");
        }

        [ContextMenu("Debug: Add 3 Stone")]
        private void DebugAddStone()
        {
            Item stoneItem = Resources.Load<Item>("ScriptableObjects/Items/Stone"); // Example path
            if (stoneItem != null) AddItem(stoneItem, 3);
            else Debug.LogError("Stone item not found at 'Resources/ScriptableObjects/Items/Stone'");
        }

        [ContextMenu("Debug: Clear Inventory")]
        private void DebugClearInventory()
        {
            _inventoryContents.Clear();
            Debug.Log("Inventory cleared.");
            OnInventoryChanged?.Invoke();
        }

        // --- Example: Save/Load functionality (simplified) ---
        // In a real game, you would serialize _inventoryContents to a file or database.
        public void SaveInventory()
        {
            // Simplified: just logs
            Debug.Log("Saving Inventory...");
            foreach (var entry in _inventoryContents)
            {
                Debug.Log($"  - {entry.Value}x {entry.Key.displayName} (ID: {entry.Key.id})");
            }
        }

        public void LoadInventory()
        {
            // Simplified: just logs and clears
            Debug.Log("Loading Inventory (clearing current)...");
            _inventoryContents.Clear();
            // In a real scenario, you'd deserialize data and populate _inventoryContents
            OnInventoryChanged?.Invoke();
        }
    }
}
```

---

### 4. `CraftingManager.cs` (Core System - MonoBehaviour)

This is the heart of the Crafting System pattern. It holds all available recipes and processes crafting requests, interacting with the `InventoryManager`.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CraftingSystem
{
    /// <summary>
    /// The central manager for all crafting operations. This class defines the "Crafting System" pattern.
    /// It holds a collection of all known crafting recipes and provides methods to check
    /// if a recipe can be crafted, and to perform the crafting process.
    /// It interacts with the InventoryManager to consume ingredients and produce output items.
    /// Implemented as a Singleton for easy global access.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        // Singleton pattern implementation
        public static CraftingManager Instance { get; private set; }

        [Header("Recipe Data")]
        [Tooltip("List of all available crafting recipes in the game.")]
        [SerializeField] private List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        // Reference to the InventoryManager, which handles item storage.
        private InventoryManager _inventoryManager;

        // Events for external systems to subscribe to, allowing for UI updates or other game logic.
        public event Action<CraftingRecipe> OnCraftingSuccess;
        public event Action<CraftingRecipe, string> OnCraftingFailed;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of CraftingManager found, destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if this manager needs to persist across scenes

            Debug.Log("CraftingManager initialized.");
        }

        private void Start()
        {
            // Get reference to the InventoryManager
            _inventoryManager = InventoryManager.Instance;
            if (_inventoryManager == null)
            {
                Debug.LogError("InventoryManager not found! Crafting System cannot function without it.");
                enabled = false; // Disable this component if inventory manager is missing
            }
            else
            {
                // Optionally, subscribe to inventory changes to re-evaluate craftable recipes for UI.
                // _inventoryManager.OnInventoryChanged += ReevaluateCraftableRecipes;
            }

            Debug.Log($"Loaded {allRecipes.Count} crafting recipes.");
        }

        /// <summary>
        /// Checks if the player currently has all the required ingredients to craft a given recipe.
        /// </summary>
        /// <param name="recipe">The CraftingRecipe to check.</param>
        /// <returns>True if all ingredients are present in the inventory, false otherwise.</returns>
        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null)
            {
                Debug.LogWarning("Attempted to check a null recipe.");
                return false;
            }
            if (_inventoryManager == null)
            {
                Debug.LogError("InventoryManager is null, cannot check crafting ability.");
                return false;
            }

            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.item == null)
                {
                    Debug.LogError($"Recipe '{recipe.recipeName}' has a null ingredient item. Please fix the recipe asset.");
                    return false; // Invalid recipe definition
                }

                if (!_inventoryManager.HasItem(ingredient.item, ingredient.quantity))
                {
                    // Debug.Log($"Cannot craft '{recipe.recipeName}': Not enough {ingredient.item.displayName}. Required: {ingredient.quantity}, Have: {_inventoryManager.GetItemQuantity(ingredient.item)}");
                    return false; // Missing or insufficient quantity of an ingredient
                }
            }
            return true; // All ingredients are present
        }

        /// <summary>
        /// Attempts to craft a given recipe. If successful, consumes ingredients and adds the output item to inventory.
        /// Notifies listeners via OnCraftingSuccess or OnCraftingFailed events.
        /// </summary>
        /// <param name="recipe">The CraftingRecipe to craft.</param>
        /// <returns>True if crafting was successful, false otherwise.</returns>
        public bool Craft(CraftingRecipe recipe)
        {
            if (recipe == null)
            {
                OnCraftingFailed?.Invoke(null, "Recipe is null.");
                return false;
            }
            if (_inventoryManager == null)
            {
                OnCraftingFailed?.Invoke(recipe, "InventoryManager is not available.");
                Debug.LogError("InventoryManager is null, cannot craft.");
                return false;
            }
            if (recipe.outputItem == null)
            {
                OnCraftingFailed?.Invoke(recipe, "Recipe has no output item defined.");
                Debug.LogError($"Recipe '{recipe.recipeName}' has no output item. Please fix the recipe asset.");
                return false;
            }


            // First, check if all ingredients are available
            if (!CanCraft(recipe))
            {
                // A more detailed error message could be generated inside CanCraft
                OnCraftingFailed?.Invoke(recipe, "Not enough ingredients to craft.");
                Debug.LogWarning($"Failed to craft '{recipe.recipeName}': Not enough ingredients.");
                return false;
            }

            // If all checks pass, proceed to consume ingredients and produce output.
            // It's good practice to consume all ingredients before adding the output
            // to ensure atomicity of the operation. If consuming fails for any reason
            // (shouldn't happen after CanCraft, but good for robust error handling),
            // you might want to roll back. For this simple demo, we assume success.

            // 1. Consume Ingredients
            foreach (var ingredient in recipe.ingredients)
            {
                if (!_inventoryManager.RemoveItem(ingredient.item, ingredient.quantity))
                {
                    // This scenario should ideally not be reached if CanCraft() passed.
                    // If it does, it indicates a race condition or a bug.
                    Debug.LogError($"Critical Error: Failed to remove {ingredient.quantity}x {ingredient.item.displayName} for recipe {recipe.recipeName} after CanCraft check passed.");
                    // In a real system, you might try to re-add previously removed items here (rollback)
                    OnCraftingFailed?.Invoke(recipe, "Internal error: Failed to consume ingredients.");
                    return false;
                }
            }

            // 2. Add Output Item
            _inventoryManager.AddItem(recipe.outputItem, recipe.outputQuantity);

            Debug.Log($"Successfully crafted {recipe.outputQuantity}x {recipe.outputItem.displayName} using recipe '{recipe.recipeName}'.");
            OnCraftingSuccess?.Invoke(recipe); // Notify listeners of success
            return true;
        }

        /// <summary>
        /// Returns a read-only list of all known crafting recipes.
        /// </summary>
        public IReadOnlyList<CraftingRecipe> GetAllRecipes()
        {
            return allRecipes;
        }

        /// <summary>
        /// Returns a list of recipes that the player currently has the ingredients for.
        /// This is useful for dynamically updating crafting UIs.
        /// </summary>
        public List<CraftingRecipe> GetCraftableRecipes()
        {
            List<CraftingRecipe> craftable = new List<CraftingRecipe>();
            foreach (var recipe in allRecipes)
            {
                if (CanCraft(recipe))
                {
                    craftable.Add(recipe);
                }
            }
            return craftable;
        }

        // --- Example: Adding/Removing recipes dynamically (e.g., unlocking new recipes) ---
        public void AddRecipe(CraftingRecipe newRecipe)
        {
            if (newRecipe != null && !allRecipes.Contains(newRecipe))
            {
                allRecipes.Add(newRecipe);
                Debug.Log($"New recipe '{newRecipe.recipeName}' added to the system.");
            }
        }

        public void RemoveRecipe(CraftingRecipe recipeToRemove)
        {
            if (recipeToRemove != null && allRecipes.Remove(recipeToRemove))
            {
                Debug.Log($"Recipe '{recipeToRemove.recipeName}' removed from the system.");
            }
        }
    }
}
```

---

### 5. `CraftingDemo.cs` (Demonstration Script)

This script simulates player interaction, showing how to set up items and recipes, add items to inventory, and initiate crafting.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CraftingSystem
{
    /// <summary>
    /// This script serves as a demonstration of the Crafting System in action.
    /// It simulates initial inventory, attempts to craft items, and logs outcomes.
    /// It also shows how a UI or other game systems would interact with
    /// the CraftingManager and InventoryManager.
    /// </summary>
    public class CraftingDemo : MonoBehaviour
    {
        [Header("Demo Setup")]
        [Tooltip("List of items to add to inventory at the start of the demo.")]
        public List<RecipeIngredient> initialInventoryItems = new List<RecipeIngredient>();

        [Tooltip("Recipes that the demo will attempt to craft.")]
        public List<CraftingRecipe> recipesToAttempt = new List<CraftingRecipe>();

        private InventoryManager _inventoryManager;
        private CraftingManager _craftingManager;

        void Start()
        {
            // 1. Get references to the managers
            _inventoryManager = InventoryManager.Instance;
            _craftingManager = CraftingManager.Instance;

            if (_inventoryManager == null || _craftingManager == null)
            {
                Debug.LogError("CraftingDemo: InventoryManager or CraftingManager not found. Make sure they are in the scene.");
                enabled = false;
                return;
            }

            // 2. Subscribe to events for feedback
            _inventoryManager.OnInventoryChanged += OnInventoryChanged;
            _craftingManager.OnCraftingSuccess += OnCraftingSuccess;
            _craftingManager.OnCraftingFailed += OnCraftingFailed;

            Debug.Log("--- Crafting Demo Started ---");

            // 3. Populate initial inventory
            PopulateInitialInventory();

            // 4. List all available recipes (from CraftingManager)
            Debug.Log("\n--- All Known Recipes ---");
            foreach (var recipe in _craftingManager.GetAllRecipes())
            {
                Debug.Log(recipe.GetRecipeSummary());
            }

            // 5. Simulate crafting attempts
            Invoke("PerformCraftingAttempts", 2f); // Delay to allow initial logs to settle
        }

        void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks, especially important for singletons.
            if (_inventoryManager != null)
            {
                _inventoryManager.OnInventoryChanged -= OnInventoryChanged;
            }
            if (_craftingManager != null)
            {
                _craftingManager.OnCraftingSuccess -= OnCraftingSuccess;
                _craftingManager.OnCraftingFailed -= OnCraftingFailed;
            }
        }

        /// <summary>
        /// Adds predefined items to the player's inventory.
        /// </summary>
        private void PopulateInitialInventory()
        {
            Debug.Log("\n--- Initializing Inventory ---");
            foreach (var itemEntry in initialInventoryItems)
            {
                if (itemEntry.item != null)
                {
                    _inventoryManager.AddItem(itemEntry.item, itemEntry.quantity);
                }
                else
                {
                    Debug.LogWarning("Initial inventory item is null in CraftingDemo configuration.");
                }
            }
            DisplayCurrentInventory();
        }

        /// <summary>
        /// Performs the crafting attempts configured in the inspector.
        /// </summary>
        private void PerformCraftingAttempts()
        {
            Debug.Log("\n--- Starting Crafting Attempts ---");
            foreach (var recipe in recipesToAttempt)
            {
                if (recipe == null)
                {
                    Debug.LogWarning("Attempted to craft a null recipe in demo setup.");
                    continue;
                }
                Debug.Log($"\nAttempting to craft: {recipe.recipeName} (Output: {recipe.outputItem?.displayName})");
                _craftingManager.Craft(recipe);
                // Display inventory after each attempt to show changes
                DisplayCurrentInventory();
                // In a real UI, you might refresh the list of craftable items here
                // Debug.Log("Current craftable recipes: " + _craftingManager.GetCraftableRecipes().Count);
            }

            Debug.Log("\n--- Crafting Demo Finished ---");
        }

        /// <summary>
        /// Event handler for successful crafting.
        /// </summary>
        /// <param name="recipe">The recipe that was successfully crafted.</param>
        private void OnCraftingSuccess(CraftingRecipe recipe)
        {
            Debug.Log($"<color=green>SUCCESS:</color> Crafted {recipe.outputQuantity}x {recipe.outputItem.displayName}.");
            DisplayCurrentInventory(); // Always show inventory after changes
        }

        /// <summary>
        /// Event handler for failed crafting.
        /// </summary>
        /// <param name="recipe">The recipe that failed to craft.</param>
        /// <param name="reason">The reason for the failure.</param>
        private void OnCraftingFailed(CraftingRecipe recipe, string reason)
        {
            string recipeName = recipe != null ? recipe.recipeName : "Unknown Recipe";
            Debug.LogWarning($"<color=red>FAILED:</color> To craft '{recipeName}'. Reason: {reason}");
            DisplayCurrentInventory();
        }

        /// <summary>
        /// Event handler for inventory changes.
        /// </summary>
        private void OnInventoryChanged()
        {
            Debug.Log("Inventory changed! (Refreshing UI/Crafting options might happen here)");
            // For demo purposes, we don't display inventory on every change
            // as it would spam the console. We call it explicitly when needed.
            // In a real game, this would trigger a UI update.
        }

        /// <summary>
        /// Logs the current contents of the inventory to the console.
        /// </summary>
        private void DisplayCurrentInventory()
        {
            Debug.Log("--- Current Inventory ---");
            var contents = _inventoryManager.GetInventoryContents();
            if (contents.Count == 0)
            {
                Debug.Log("Inventory is empty.");
                return;
            }
            foreach (var entry in contents)
            {
                Debug.Log($"- {entry.Value}x {entry.Key.displayName}");
            }
            Debug.Log("-------------------------");
        }

        // --- Context Menu for editor testing ---
        [ContextMenu("Debug: Trigger Crafting Attempts Now")]
        private void TriggerCraftingAttempts()
        {
            PerformCraftingAttempts();
        }

        [ContextMenu("Debug: Display Inventory Now")]
        private void DisplayInventoryDebug()
        {
            DisplayCurrentInventory();
        }
    }
}
```

---

### **How to Use in Unity Editor:**

1.  **Create Item ScriptableObjects:**
    *   Go to `Assets/ScriptableObjects/Items/`.
    *   Right-click -> `Create` -> `Crafting System` -> `Item`.
    *   Create a few items, e.g., `Wood`, `Stone`, `Pickaxe`.
    *   Fill in their `ID`, `Display Name`, `Icon` (optional), etc.

2.  **Create Crafting Recipe ScriptableObjects:**
    *   Go to `Assets/ScriptableObjects/Recipes/`.
    *   Right-click -> `Create` -> `Crafting System` -> `Crafting Recipe`.
    *   Create a recipe, e.g., `WoodenPickaxeRecipe`.
    *   Fill in:
        *   `Recipe Name`: "Wooden Pickaxe"
        *   `Ingredients`:
            *   Add an element: `Item` = `Wood`, `Quantity` = `3`
            *   Add another element: `Item` = `Stone`, `Quantity` = `2` (Example, adjust as needed)
        *   `Output Item`: Select your `Pickaxe` Item SO.
        *   `Output Quantity`: `1`

3.  **Configure `GameManager`:**
    *   Select the `GameManager` GameObject in your scene.
    *   The `InventoryManager` and `CraftingManager` components should be attached. No specific configuration needed here unless you want to use the `[ContextMenu]` debugging options.

4.  **Configure `CraftingDemo`:**
    *   Select the `CraftingDemo` GameObject in your scene.
    *   **`Initial Inventory Items`**: Add some items you want the player to start with. For example, add `Wood` (quantity 10) and `Stone` (quantity 10).
    *   **`Recipes To Attempt`**: Add the `WoodenPickaxeRecipe` (and any other recipes you create) to this list.

5.  **Run the Scene:**
    *   Play the scene.
    *   Observe the Unity Console. You will see logs detailing the inventory initialization, crafting attempts, successes, and failures, demonstrating the entire system.

---

### **Explanation of the Crafting System Pattern:**

This example implements the Crafting System as a set of interconnected components, primarily using a **Manager (Singleton)** pattern for global access to core systems and **ScriptableObjects** for data definition.

1.  **Separation of Concerns:**
    *   **Data (What):** `Item.cs` and `CraftingRecipe.cs` (ScriptableObjects) define *what* items are and *what* recipes require and produce. They contain no game logic, only data. This makes your game data easy to manage, create, and modify in the Unity Editor without touching code.
    *   **Inventory (Where):** `InventoryManager.cs` handles *where* items are stored. It's responsible for adding, removing, and querying item quantities. It doesn't know about crafting, only item storage.
    *   **Crafting Logic (How):** `CraftingManager.cs` is the core of the pattern. It knows *how* to craft. It holds all available recipes, checks inventory for ingredients (delegating to `InventoryManager`), consumes ingredients, and adds the crafted output (again, delegating to `InventoryManager`). It doesn't know *how* to store items, only how to orchestrate the crafting process.
    *   **User Interface/Interaction (When):** `CraftingDemo.cs` (which would be a UI or player input system in a real game) decides *when* crafting happens. It gets recipes from `CraftingManager`, sends crafting requests, and listens to events from `CraftingManager` and `InventoryManager` to update the display or react to outcomes.

2.  **Manager/Singleton Pattern (`CraftingManager`, `InventoryManager`):**
    *   Both managers are implemented as Singletons. This ensures there's only one instance of each throughout the game, providing a global access point for other scripts. This is practical for core systems like Inventory and Crafting, which most parts of the game need to interact with.
    *   They use `public static Instance` property for easy access (e.g., `CraftingManager.Instance.Craft(myRecipe)`).

3.  **Events/Delegates (`OnInventoryChanged`, `OnCraftingSuccess`, `OnCraftingFailed`):**
    *   The managers use C# events (`Action<T>`) to communicate changes or outcomes without directly knowing or relying on the specific scripts that need to react.
    *   For example, when `InventoryManager.AddItem()` is called, it `Invoke()`s `OnInventoryChanged`. A UI script (`CraftingDemo` in this case) can subscribe to this event and refresh its display without the `InventoryManager` needing to know anything about the UI. This promotes loose coupling.

4.  **ScriptableObjects for Data:**
    *   `Item` and `CraftingRecipe` are `ScriptableObject`s. This is a best practice in Unity for defining immutable data assets.
    *   **Benefits:**
        *   **Data-Driven:** Easily create thousands of items and recipes directly in the editor.
        *   **Memory Efficiency:** One `ScriptableObject` instance can be referenced by many `MonoBehaviour`s, saving memory compared to creating new `Item` objects in memory for every stack in the inventory.
        *   **Reusability:** The same `Item` ScriptableObject can be an ingredient in multiple recipes, an item in the player's inventory, and a drop from an enemy, all referencing the single data asset.
        *   **Editor Workflow:** `[CreateAssetMenu]` allows easy creation, and `[SerializeField]` lists allow easy assignment of these assets to manager components.

5.  **Robustness and Error Handling:**
    *   Checks for null references (`item == null`, `recipe == null`).
    *   Checks for valid quantities (`quantity <= 0`).
    *   `CanCraft()` method explicitly checks if ingredients are available *before* attempting to remove them, preventing partial crafting or negative inventory.
    *   Warning and error logs provide feedback during development.

### **Benefits of this Pattern:**

*   **Scalability:** Easily add hundreds of items and recipes without changing core code. Just create new ScriptableObject assets.
*   **Maintainability:** Changes to item data, recipe definitions, or inventory/crafting logic are localized to their respective classes.
*   **Flexibility:** Different UIs or game mechanics can interact with the core systems using the provided public methods and events.
*   **Designer-Friendly:** Game designers can balance recipes and create items directly in the Unity Editor without programmer intervention.
*   **Clear Responsibilities:** Each component has a single, well-defined role.

This example provides a solid foundation for building a robust and expandable crafting system in your Unity projects.