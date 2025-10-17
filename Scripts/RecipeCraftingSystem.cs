// Unity Design Pattern Example: RecipeCraftingSystem
// This script demonstrates the RecipeCraftingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RecipeCraftingSystem' design pattern provides a structured way to manage crafting recipes, ingredients, and the crafting process within a game. It separates concerns into distinct components: Items, Recipes, an Inventory, and a Crafting Manager.

This example provides a complete, practical C# Unity implementation.

---

**How to Use This Example in Unity:**

1.  **Create Folders:**
    *   In your Unity Project window, create a folder named `Scripts`.
    *   Inside `Scripts`, create another folder named `RecipeCraftingSystem`.
    *   Inside `RecipeCraftingSystem`, create sub-folders: `Items`, `Recipes`, `Managers`.

2.  **Create C# Scripts:**
    *   Copy each of the following C# code blocks into new C# scripts with the exact filenames provided.
    *   Place `Item.cs`, `Ingredient.cs`, `CraftingRecipe.cs` in the `RecipeCraftingSystem/Items` and `RecipeCraftingSystem/Recipes` folders respectively.
    *   Place `InventorySystem.cs` and `CraftingManager.cs` in the `RecipeCraftingSystem/Managers` folder.
    *   Place `CraftingDemo.cs` (optional, for quick testing) in the `RecipeCraftingSystem/Managers` or root `Scripts` folder.

3.  **Create ScriptableObject Assets:**
    *   **Create Items:**
        *   Right-click in your Project window -> `Create` -> `Recipe Crafting System` -> `Item`.
        *   Name them: `Wood`, `Stone`, `IronOre`, `IronIngot`, `Sword`, `HealthPotion`, `Bread`, `Flour`, `Water`.
        *   Fill in unique IDs (e.g., Wood=1, Stone=2, etc.), names, and assign a placeholder `Sprite` (e.g., a default UI sprite, or just leave it null for this demo).
    *   **Create Recipes:**
        *   Right-click in your Project window -> `Create` -> `Recipe Crafting System` -> `Crafting Recipe`.
        *   Name them: `Recipe_IronIngot`, `Recipe_Sword`, `Recipe_HealthPotion`, `Recipe_Bread`.
        *   **`Recipe_IronIngot`:**
            *   Output Item: `IronIngot` (Quantity: 1)
            *   Ingredients:
                *   `IronOre` (Quantity: 2)
        *   **`Recipe_Sword`:**
            *   Output Item: `Sword` (Quantity: 1)
            *   Ingredients:
                *   `IronIngot` (Quantity: 3)
                *   `Wood` (Quantity: 1)
        *   **`Recipe_HealthPotion`:**
            *   Output Item: `HealthPotion` (Quantity: 1)
            *   Ingredients:
                *   `Water` (Quantity: 1)
                *   `Flour` (Quantity: 1)
        *   **`Recipe_Bread`:**
            *   Output Item: `Bread` (Quantity: 1)
            *   Ingredients:
                *   `Flour` (Quantity: 3)
                *   `Water` (Quantity: 1)

4.  **Setup Scene:**
    *   Create an Empty GameObject in your scene named `CraftingSystem`.
    *   Add the `InventorySystem` component to `CraftingSystem`.
        *   In the Inspector for `InventorySystem`, fill in some initial items (e.g., 5 Wood, 5 Stone, 10 IronOre, 2 Flour, 2 Water).
    *   Add the `CraftingManager` component to `CraftingSystem`.
        *   In the Inspector for `CraftingManager`, assign all the `CraftingRecipe` assets you created to the `All Recipes` list.
    *   (Optional but recommended for testing) Add the `CraftingDemo` component to `CraftingSystem`.

5.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   If you added `CraftingDemo`, check the Console for output demonstrating crafting attempts and inventory changes. You can modify `CraftingDemo` to try different recipes.

---

### 1. `Item.cs` (ScriptableObject)
Represents a generic item in the game, whether it's an ingredient or a crafted product.

```csharp
using UnityEngine;

namespace RecipeCraftingSystem.Items
{
    /// <summary>
    /// Represents a generic item in the game. This is a ScriptableObject,
    /// allowing items to be defined as assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Recipe Crafting System/Item", order = 0)]
    public class Item : ScriptableObject
    {
        [Tooltip("A unique identifier for this item.")]
        public int id;

        [Tooltip("The display name of the item.")]
        public string itemName = "New Item";

        [Tooltip("The icon used to represent this item in UI.")]
        public Sprite icon;

        [Tooltip("A brief description of the item.")]
        [TextArea(3, 5)]
        public string description = "A generic item.";

        // You can add more properties here like weight, stackability, rarity, etc.

        /// <summary>
        /// Provides a readable string representation of the item.
        /// </summary>
        /// <returns>The item's name.</returns>
        public override string ToString()
        {
            return itemName;
        }
    }
}
```

### 2. `Ingredient.cs` (Serializable Struct)
A helper struct used within recipes to define an item and its required quantity.

```csharp
using UnityEngine;
using RecipeCraftingSystem.Items;
using System; // Required for [Serializable]

namespace RecipeCraftingSystem.Recipes
{
    /// <summary>
    /// Represents an ingredient required for a recipe, specifying the item and its quantity.
    /// This struct is marked [Serializable] so it can be embedded in ScriptableObjects
    /// and show up in the Unity Inspector.
    /// </summary>
    [Serializable]
    public struct Ingredient
    {
        [Tooltip("The Item asset that serves as an ingredient.")]
        public Item item;

        [Tooltip("The quantity of the item required for the recipe.")]
        [Min(1)] // Ensures quantity is at least 1
        public int quantity;

        /// <summary>
        /// Provides a readable string representation of the ingredient.
        /// </summary>
        /// <returns>A string like "2x Wood".</returns>
        public override string ToString()
        {
            return $"{quantity}x {item.itemName}";
        }
    }
}
```

### 3. `CraftingRecipe.cs` (ScriptableObject)
Defines a recipe, specifying input ingredients and the output item.

```csharp
using UnityEngine;
using System.Collections.Generic;
using RecipeCraftingSystem.Items;

namespace RecipeCraftingSystem.Recipes
{
    /// <summary>
    /// Defines a crafting recipe. This is a ScriptableObject, allowing recipes
    /// to be defined as assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe Crafting System/Crafting Recipe", order = 1)]
    public class CraftingRecipe : ScriptableObject
    {
        [Tooltip("The display name of the recipe.")]
        public string recipeName = "New Recipe";

        [Tooltip("A list of ingredients required to craft this item.")]
        public List<Ingredient> ingredients = new List<Ingredient>();

        [Tooltip("The item produced by this recipe.")]
        public Item outputItem;

        [Tooltip("The quantity of the output item produced.")]
        [Min(1)] // Ensures at least one item is produced
        public int outputQuantity = 1;

        /// <summary>
        /// Provides a readable string representation of the recipe.
        /// </summary>
        /// <returns>A string like "Recipe: 3x IronIngot + 1x Wood -> 1x Sword".</returns>
        public override string ToString()
        {
            string ingredientList = "";
            foreach (var ing in ingredients)
            {
                ingredientList += $"{ing.quantity}x {ing.item.itemName} + ";
            }
            // Remove the last " + "
            if (ingredientList.Length > 3)
            {
                ingredientList = ingredientList.Substring(0, ingredientList.Length - 3);
            }
            
            return $"Recipe: {ingredientList} -> {outputQuantity}x {outputItem.itemName}";
        }
    }
}
```

### 4. `InventorySystem.cs` (MonoBehaviour)
Manages the player's inventory, handling item storage, addition, removal, and quantity checks.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for Action
using RecipeCraftingSystem.Items;

namespace RecipeCraftingSystem.Managers
{
    /// <summary>
    /// Manages the player's inventory. This MonoBehaviour can be attached to a GameObject
    /// in the scene, and it provides methods for adding, removing, and checking items.
    /// It uses a Dictionary to store item quantities efficiently.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        // Singleton pattern for easy access throughout the game.
        public static InventorySystem Instance { get; private set; }

        [Header("Initial Inventory (for testing)")]
        [Tooltip("Items and quantities to start with for demonstration purposes.")]
        [SerializeField] private List<Ingredient> initialItems = new List<Ingredient>();

        // Stores the current items and their quantities in the inventory.
        // Using Item ScriptableObject as the key provides direct reference and type safety.
        private Dictionary<Item, int> itemQuantities = new Dictionary<Item, int>();

        // Event triggered whenever the inventory changes, useful for UI updates.
        public event Action<Item, int, int> OnInventoryChanged; // Item, old quantity, new quantity
        public event Action<Item, int> OnItemAdded; // Item, quantity added
        public event Action<Item, int> OnItemRemoved; // Item, quantity removed

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("InventorySystem: Another instance already exists. Destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeInventory();
        }

        /// <summary>
        /// Initializes the inventory with the predefined initial items.
        /// </summary>
        private void InitializeInventory()
        {
            foreach (var itemEntry in initialItems)
            {
                if (itemEntry.item != null)
                {
                    AddItem(itemEntry.item, itemEntry.quantity, false); // Add without triggering full change event yet
                }
            }
            Debug.Log($"InventorySystem: Initialized with {itemQuantities.Count} unique items.");
            // After all initial items are added, trigger change for all, or individually
            // For simplicity, we just log and let individual adds handle their events.
        }

        /// <summary>
        /// Adds a specified quantity of an item to the inventory.
        /// </summary>
        /// <param name="item">The Item ScriptableObject to add.</param>
        /// <param name="quantity">The amount of the item to add.</param>
        /// <param name="triggerEvents">Whether to trigger OnInventoryChanged events.</param>
        public void AddItem(Item item, int quantity, bool triggerEvents = true)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"InventorySystem: Attempted to add invalid item ({item?.itemName ?? "NULL"}) or quantity ({quantity}).");
                return;
            }

            int oldQuantity = 0;
            if (itemQuantities.ContainsKey(item))
            {
                oldQuantity = itemQuantities[item];
                itemQuantities[item] += quantity;
            }
            else
            {
                itemQuantities.Add(item, quantity);
            }

            Debug.Log($"InventorySystem: Added {quantity}x {item.itemName}. New total: {itemQuantities[item]}.");

            if (triggerEvents)
            {
                OnInventoryChanged?.Invoke(item, oldQuantity, itemQuantities[item]);
                OnItemAdded?.Invoke(item, quantity);
            }
        }

        /// <summary>
        /// Removes a specified quantity of an item from the inventory.
        /// </summary>
        /// <param name="item">The Item ScriptableObject to remove.</param>
        /// <param name="quantity">The amount of the item to remove.</param>
        /// <param name="triggerEvents">Whether to trigger OnInventoryChanged events.</param>
        /// <returns>True if items were successfully removed, false otherwise (e.g., not enough items).</returns>
        public bool RemoveItem(Item item, int quantity, bool triggerEvents = true)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"InventorySystem: Attempted to remove invalid item ({item?.itemName ?? "NULL"}) or quantity ({quantity}).");
                return false;
            }

            if (itemQuantities.TryGetValue(item, out int currentQuantity))
            {
                if (currentQuantity >= quantity)
                {
                    itemQuantities[item] -= quantity;
                    Debug.Log($"InventorySystem: Removed {quantity}x {item.itemName}. New total: {itemQuantities[item]}.");

                    if (itemQuantities[item] == 0)
                    {
                        itemQuantities.Remove(item);
                        Debug.Log($"InventorySystem: {item.itemName} completely removed from inventory.");
                    }

                    if (triggerEvents)
                    {
                        OnInventoryChanged?.Invoke(item, currentQuantity, itemQuantities.GetValueOrDefault(item, 0));
                        OnItemRemoved?.Invoke(item, quantity);
                    }
                    return true;
                }
                else
                {
                    Debug.Log($"InventorySystem: Not enough {item.itemName} to remove. Have {currentQuantity}, need {quantity}.");
                    return false;
                }
            }
            else
            {
                Debug.Log($"InventorySystem: Item {item.itemName} not found in inventory.");
                return false;
            }
        }

        /// <summary>
        /// Checks if the inventory contains at least the specified quantity of an item.
        /// </summary>
        /// <param name="item">The Item ScriptableObject to check.</param>
        /// <param name="quantity">The required amount of the item.</param>
        /// <returns>True if the inventory has enough, false otherwise.</returns>
        public bool HasItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            return itemQuantities.TryGetValue(item, out int currentQuantity) && currentQuantity >= quantity;
        }

        /// <summary>
        /// Gets the current quantity of a specific item in the inventory.
        /// </summary>
        /// <param name="item">The Item ScriptableObject to query.</param>
        /// <returns>The quantity of the item, or 0 if not present.</returns>
        public int GetItemQuantity(Item item)
        {
            if (item == null) return 0;
            return itemQuantities.GetValueOrDefault(item, 0);
        }

        /// <summary>
        /// Returns a read-only dictionary of the current inventory contents.
        /// </summary>
        public IReadOnlyDictionary<Item, int> GetInventoryContents()
        {
            return itemQuantities;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
```

### 5. `CraftingManager.cs` (MonoBehaviour)
The core crafting logic. It interfaces with the `InventorySystem` to check ingredients, consume them, and produce output.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for Action
using System.Linq; // Required for LINQ operations like .All()
using RecipeCraftingSystem.Items;
using RecipeCraftingSystem.Recipes;

namespace RecipeCraftingSystem.Managers
{
    /// <summary>
    /// Manages all crafting operations. It holds a list of available recipes
    /// and uses the InventorySystem to perform crafting attempts.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        // Singleton pattern for easy access
        public static CraftingManager Instance { get; private set; }

        [Header("Dependencies")]
        [Tooltip("Reference to the InventorySystem in the scene.")]
        [SerializeField] private InventorySystem inventory;

        [Header("Recipes")]
        [Tooltip("All available crafting recipes in the game.")]
        public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        // Events to notify other systems (e.g., UI) about crafting outcomes.
        public event Action<CraftingRecipe, Item, int> OnCraftingSuccess; // Recipe, OutputItem, OutputQuantity
        public event Action<CraftingRecipe, string> OnCraftingFailure; // Recipe, FailureReason

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CraftingManager: Another instance already exists. Destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure inventory reference is set, especially if using a static Instance for InventorySystem
            if (inventory == null)
            {
                inventory = InventorySystem.Instance;
                if (inventory == null)
                {
                    Debug.LogError("CraftingManager: InventorySystem not found in scene!", this);
                }
            }
        }

        /// <summary>
        /// Attempts to craft an item based on the provided recipe.
        /// </summary>
        /// <param name="recipe">The CraftingRecipe to attempt to craft.</param>
        /// <returns>True if crafting was successful, false otherwise.</returns>
        public bool TryCraft(CraftingRecipe recipe)
        {
            if (recipe == null)
            {
                Debug.LogError("CraftingManager: Attempted to craft with a null recipe!");
                OnCraftingFailure?.Invoke(null, "Invalid recipe provided.");
                return false;
            }
            if (inventory == null)
            {
                Debug.LogError("CraftingManager: InventorySystem reference is missing!");
                OnCraftingFailure?.Invoke(recipe, "Inventory system not found.");
                return false;
            }

            Debug.Log($"CraftingManager: Attempting to craft {recipe.recipeName}...");

            // 1. Check if the player has all required ingredients.
            if (!CanCraft(recipe))
            {
                string reason = GetMissingIngredientsString(recipe);
                Debug.Log($"CraftingManager: Failed to craft {recipe.recipeName}. Reason: {reason}");
                OnCraftingFailure?.Invoke(recipe, reason);
                return false;
            }

            // 2. Consume ingredients.
            // It's important to consume all ingredients before adding the output,
            // to prevent edge cases where output is also an ingredient and causes issues.
            foreach (var ingredient in recipe.ingredients)
            {
                // We already checked CanCraft, so this should always succeed.
                inventory.RemoveItem(ingredient.item, ingredient.quantity);
                Debug.Log($"CraftingManager: Consumed {ingredient.quantity}x {ingredient.item.itemName}.");
            }

            // 3. Add the crafted item to the inventory.
            inventory.AddItem(recipe.outputItem, recipe.outputQuantity);
            Debug.Log($"CraftingManager: Successfully crafted {recipe.outputQuantity}x {recipe.outputItem.itemName}!");

            // 4. Trigger success event.
            OnCraftingSuccess?.Invoke(recipe, recipe.outputItem, recipe.outputQuantity);
            return true;
        }

        /// <summary>
        /// Checks if the player has all the necessary ingredients to craft a given recipe.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <returns>True if all ingredients are available in sufficient quantities, false otherwise.</returns>
        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null || inventory == null) return false;

            // Use LINQ's .All() to check if all ingredients are present.
            return recipe.ingredients.All(ingredient =>
                inventory.HasItem(ingredient.item, ingredient.quantity));
        }

        /// <summary>
        /// Generates a string listing all missing ingredients for a given recipe.
        /// </summary>
        /// <param name="recipe">The recipe to check.</param>
        /// <returns>A formatted string of missing ingredients, or an empty string if none are missing.</returns>
        public string GetMissingIngredientsString(CraftingRecipe recipe)
        {
            if (recipe == null || inventory == null) return "Invalid recipe or inventory.";

            List<string> missing = new List<string>();
            foreach (var ingredient in recipe.ingredients)
            {
                int currentCount = inventory.GetItemQuantity(ingredient.item);
                if (currentCount < ingredient.quantity)
                {
                    missing.Add($"{ingredient.item.itemName} (Need: {ingredient.quantity}, Have: {currentCount})");
                }
            }

            if (missing.Count > 0)
            {
                return "Missing: " + string.Join(", ", missing);
            }
            return "No missing ingredients.";
        }

        /// <summary>
        /// Returns a read-only list of all recipes known by the manager.
        /// </summary>
        public IReadOnlyList<CraftingRecipe> GetAllRecipes()
        {
            return allRecipes;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
```

### 6. `CraftingDemo.cs` (Optional MonoBehaviour for testing)
A simple script to demonstrate how to use the `InventorySystem` and `CraftingManager`. Attach this to the same GameObject as `CraftingManager` and `InventorySystem`.

```csharp
using UnityEngine;
using RecipeCraftingSystem.Items;
using RecipeCraftingSystem.Recipes;
using RecipeCraftingSystem.Managers;
using System.Linq; // For .FirstOrDefault()

namespace RecipeCraftingSystem.Demo
{
    /// <summary>
    /// A simple demonstration script to show how to interact with the
    /// InventorySystem and CraftingManager.
    /// Attach this to a GameObject in your scene along with InventorySystem and CraftingManager.
    /// </summary>
    public class CraftingDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private CraftingManager craftingManager;

        [Header("Demo Settings")]
        [Tooltip("The recipe you want to try crafting at the start.")]
        [SerializeField] private CraftingRecipe recipeToTry;

        [Tooltip("An item to add to inventory for testing purposes.")]
        [SerializeField] private Item itemToAddManually;
        [SerializeField] private int quantityToAddManually = 5;


        void Start()
        {
            // Ensure references are set, especially for singletons
            if (inventorySystem == null)
                inventorySystem = InventorySystem.Instance;
            if (craftingManager == null)
                craftingManager = CraftingManager.Instance;

            if (inventorySystem == null || craftingManager == null)
            {
                Debug.LogError("CraftingDemo: InventorySystem or CraftingManager not found! Please set them up in the scene.", this);
                return;
            }

            // Subscribe to events for feedback
            inventorySystem.OnInventoryChanged += OnInventoryChanged;
            craftingManager.OnCraftingSuccess += OnCraftingSuccess;
            craftingManager.OnCraftingFailure += OnCraftingFailure;

            Debug.Log("--- Crafting Demo Started ---");
            PrintCurrentInventory();

            // --- Demo Scenario 1: Try crafting a specific recipe ---
            if (recipeToTry != null)
            {
                Debug.Log($"\nAttempting to craft: {recipeToTry.recipeName}");
                craftingManager.TryCraft(recipeToTry);
                PrintCurrentInventory();
            }
            else
            {
                Debug.Log("\nNo specific recipe set to try. Attempting to craft 'Sword' recipe.");
                // Find a recipe by name for dynamic testing
                CraftingRecipe swordRecipe = craftingManager.GetAllRecipes()
                                                .FirstOrDefault(r => r.recipeName == "Recipe_Sword");
                if (swordRecipe != null)
                {
                    Debug.Log($"Attempting to craft: {swordRecipe.recipeName}");
                    craftingManager.TryCraft(swordRecipe);
                    PrintCurrentInventory();
                }
                else
                {
                    Debug.LogWarning("CraftingDemo: 'Recipe_Sword' not found in CraftingManager's recipes list.");
                }
            }


            // --- Demo Scenario 2: Add more items and try crafting again ---
            if (itemToAddManually != null)
            {
                Debug.Log($"\nAdding {quantityToAddManually}x {itemToAddManually.itemName} manually.");
                inventorySystem.AddItem(itemToAddManually, quantityToAddManually);
                PrintCurrentInventory();

                // Try crafting the same recipe again after adding items
                if (recipeToTry != null)
                {
                    Debug.Log($"\nAttempting to craft {recipeToTry.recipeName} again after adding items.");
                    craftingManager.TryCraft(recipeToTry);
                    PrintCurrentInventory();
                }
            }

            Debug.Log("--- Crafting Demo Finished ---");
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= OnInventoryChanged;
            }
            if (craftingManager != null)
            {
                craftingManager.OnCraftingSuccess -= OnCraftingSuccess;
                craftingManager.OnCraftingFailure -= OnCraftingFailure;
            }
        }

        private void OnInventoryChanged(Item item, int oldQuantity, int newQuantity)
        {
            Debug.Log($"[UI Update] Inventory changed for {item.itemName}: {oldQuantity} -> {newQuantity}");
            // In a real game, this would update a UI element like an inventory slot.
        }

        private void OnCraftingSuccess(CraftingRecipe recipe, Item outputItem, int outputQuantity)
        {
            Debug.Log($"[UI Update] CRAFT SUCCESS! Crafted {outputQuantity}x {outputItem.itemName} from recipe: {recipe.recipeName}");
            // In a real game, this would show a success message, play sound/animation, etc.
        }

        private void OnCraftingFailure(CraftingRecipe recipe, string reason)
        {
            string recipeName = recipe != null ? recipe.recipeName : "UNKNOWN RECIPE";
            Debug.LogWarning($"[UI Update] CRAFT FAILED for {recipeName}. Reason: {reason}");
            // In a real game, this would show an error message to the player.
        }

        private void PrintCurrentInventory()
        {
            Debug.Log("\n--- Current Inventory Contents ---");
            if (inventorySystem.GetInventoryContents().Count == 0)
            {
                Debug.Log("Inventory is empty.");
                return;
            }

            foreach (var kvp in inventorySystem.GetInventoryContents())
            {
                Debug.Log($" - {kvp.Key.itemName}: {kvp.Value}");
            }
            Debug.Log("----------------------------------");
        }
    }
}
```