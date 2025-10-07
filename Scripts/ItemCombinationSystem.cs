// Unity Design Pattern Example: ItemCombinationSystem
// This script demonstrates the ItemCombinationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Item Combination System** design pattern, which is used to manage and process recipes for combining various in-game items into new ones. It's a fundamental system for crafting, alchemy, or puzzle mechanics in many games.

The pattern consists of:
1.  **Item Data (`ItemData` ScriptableObject):** Defines the properties of individual items.
2.  **Combination Recipe (`CombinationRecipe` ScriptableObject):** Defines what input items are needed to produce a specific output item.
3.  **Combination System (`ItemCombinationSystem` MonoBehaviour):** The core logic that processes a set of input items, checks against all known recipes, and returns a combination result.

This setup uses `ScriptableObject` assets for item and recipe definitions, allowing you to create and manage your game data directly in the Unity Editor without writing new code for each item or recipe.

---

**How to Use This Script in Your Unity Project:**

1.  **Create C# Script:** Create a new C# script in your Unity project called `ItemCombinationSystem.cs` and copy the entire code below into it.
2.  **Create Item Assets:**
    *   In the Unity Editor, go to `Assets -> Create -> Item Combination System -> Item Data`.
    *   Create several `ItemData` assets (e.g., "Wood", "Stone", "IronOre", "Sword").
    *   Fill in their `DisplayName`, `Description`, and assign an `Icon` (optional). The `ItemId` will be auto-generated but can be manually set for easier reference.
3.  **Create Recipe Assets:**
    *   Go to `Assets -> Create -> Item Combination System -> Combination Recipe`.
    *   Create several `CombinationRecipe` assets (e.g., "CraftSwordRecipe").
    *   **Required Items:** Drag your `ItemData` assets (e.g., "IronOre", "IronOre", "Wood") into the `Required Items` list. The system counts quantities, so if you need two "IronOre", drag it twice.
    *   **Output Item:** Drag your `ItemData` asset (e.g., "Sword") into the `Output Item` slot.
    *   **Output Quantity:** Set the quantity produced (e.g., 1).
4.  **Setup Combination System in Scene:**
    *   Create an empty GameObject in your scene (e.g., "GameManager").
    *   Add the `ItemCombinationSystem` component to this GameObject.
    *   **All Recipes:** Drag all your created `CombinationRecipe` assets from your Project window into the `All Recipes` list on the `ItemCombinationSystem` component.
5.  **Call from your Game Logic:**
    *   In your player inventory, crafting UI, or any other script that needs to attempt a combination, get a reference to `ItemCombinationSystem.Instance`.
    *   Prepare a `List<ItemData>` of the items the player wants to combine.
    *   Call `ItemCombinationSystem.Instance.TryCombine(yourItemsList)`.
    *   Handle the `CombinationResult` to remove input items from the player's inventory and add the output item if successful.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like GroupBy, ToDictionary, Select

// =====================================================================================================================
// ItemData ScriptableObject
// Purpose: Defines the properties of a generic item in the game.
// Benefits: Allows creating item assets in the Unity Editor without scripting each item.
// =====================================================================================================================
/// <summary>
/// Represents a generic item in the game.
/// This is a ScriptableObject, allowing us to create item definitions
/// as assets in the Unity Editor without attaching them to GameObjects.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Item Combination System/Item Data", order = 1)]
public class ItemData : ScriptableObject
{
    // A unique identifier for the item. Good for serialization, lookup, and ensuring uniqueness.
    // Automatically generates a GUID if not set, but can be manually overridden in the Editor.
    public string ItemId => _itemId;
    [Tooltip("Unique identifier for this item. Auto-generated GUID by default, but can be manually set (e.g., 'wood_log').")]
    [SerializeField] private string _itemId = System.Guid.NewGuid().ToString();

    // The name displayed to the player in UI.
    public string DisplayName => _displayName;
    [Tooltip("The name of the item displayed to the player (e.g., 'Wooden Log').")]
    [SerializeField] private string _displayName = "New Item";

    // A description of the item for UI tooltips or lore.
    public string Description => _description;
    [Tooltip("A brief description of the item.")]
    [SerializeField] [TextArea(3, 5)] private string _description = "A generic item.";

    // An icon to represent the item in UI.
    public Sprite Icon => _icon;
    [Tooltip("The sprite icon for this item in the UI.")]
    [SerializeField] private Sprite _icon;

    /// <summary>
    /// Overrides ToString for easier debugging in the Unity console.
    /// </summary>
    public override string ToString()
    {
        return $"ItemData (ID: {ItemId}, Name: {DisplayName})";
    }

    /// <summary>
    /// Custom Equals method to compare ItemData objects based on their ItemId.
    /// This is important if you store ItemData in collections and need value equality.
    /// </summary>
    public override bool Equals(object other)
    {
        if (other == null || !(other is ItemData otherItem))
            return false;
        // Items are considered equal if their unique ItemIds match.
        return this.ItemId == otherItem.ItemId;
    }

    /// <summary>
    /// Custom GetHashCode method, required when overriding Equals.
    /// Uses the ItemId's hash code for consistent behavior in hash-based collections.
    /// </summary>
    public override int GetHashCode()
    {
        return ItemId.GetHashCode();
    }
}

// =====================================================================================================================
// CombinationRecipe ScriptableObject
// Purpose: Defines a specific recipe: a set of input items yields an output item.
// Benefits: Allows creating recipe assets in the Unity Editor without scripting each recipe.
// =====================================================================================================================
/// <summary>
/// Defines a single combination recipe: a set of input items yields an output item.
/// This is a ScriptableObject, allowing us to define recipes as assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewCombinationRecipe", menuName = "Item Combination System/Combination Recipe", order = 2)]
public class CombinationRecipe : ScriptableObject
{
    // The list of ItemData objects required for this combination.
    // The system will count the number of times each unique ItemData appears here
    // to determine quantities (e.g., two 'Wood' entries means two 'Wood' are needed).
    [Tooltip("The exact items (and their quantities) required for this recipe. Drag ItemData assets here.")]
    [SerializeField] private List<ItemData> _requiredItems = new List<ItemData>();
    public IReadOnlyList<ItemData> RequiredItems => _requiredItems;

    // The ItemData asset that is produced when this recipe is successfully combined.
    [Tooltip("The item that is produced when this recipe is successfully combined.")]
    [SerializeField] private ItemData _outputItem;
    public ItemData OutputItem => _outputItem;

    // The quantity of the output item produced by this recipe.
    [Tooltip("The quantity of the output item produced.")]
    [SerializeField] private int _outputQuantity = 1;
    public int OutputQuantity => _outputQuantity;

    /// <summary>
    /// Generates a frequency map (dictionary) of required items from the recipe's list.
    /// This map makes it efficient to compare against a player's attempted combination items.
    /// Example: If _requiredItems contains [Wood, Wood, Stone], the map will be { "Wood_ID": 2, "Stone_ID": 1 }.
    /// </summary>
    /// <returns>A dictionary mapping ItemId strings to their required quantities.</returns>
    public Dictionary<string, int> GetRequiredItemsFrequencyMap()
    {
        // Group items by their unique ItemId and count occurrences.
        return _requiredItems.GroupBy(item => item.ItemId)
                             .ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>
    /// Overrides ToString for easier debugging in the Unity console.
    /// </summary>
    public override string ToString()
    {
        string inputs = string.Join(", ", _requiredItems.Select(item => item.DisplayName));
        return $"Recipe: ({inputs}) -> {OutputItem?.DisplayName ?? "None"} (x{OutputQuantity})";
    }
}

// =====================================================================================================================
// CombinationResult Struct
// Purpose: A simple data structure to return the outcome of a combination attempt.
// =====================================================================================================================
/// <summary>
/// A struct to hold the result of an item combination attempt.
/// </summary>
public struct CombinationResult
{
    // True if a combination was successful, false otherwise.
    public bool Success { get; }
    // The ItemData of the item produced, if successful. Null otherwise.
    public ItemData OutputItem { get; }
    // The quantity of the output item produced, if successful. 0 otherwise.
    public int OutputQuantity { get; }

    public CombinationResult(bool success, ItemData outputItem, int outputQuantity)
    {
        Success = success;
        OutputItem = outputItem;
        OutputQuantity = outputQuantity;
    }

    // A static property for a failed combination result.
    // This provides a consistent way to represent "no combination found" without creating new objects.
    public static CombinationResult NoCombination => new CombinationResult(false, null, 0);
}

// =====================================================================================================================
// ItemCombinationSystem MonoBehaviour
// Purpose: The core logic that manages recipes and processes combination attempts.
// Benefits: Centralizes combination logic, making it easy to add new recipes and query them.
// =====================================================================================================================
/// <summary>
/// The core Item Combination System.
/// This MonoBehaviour manages a collection of combination recipes and provides the logic
/// to attempt combining a given set of items.
/// It acts as the 'Service' or 'Manager' in the ItemCombinationSystem design pattern.
/// </summary>
public class ItemCombinationSystem : MonoBehaviour
{
    // A list of all known combination recipes in the game.
    // These should be created as ScriptableObject assets in the Unity Editor
    // and dragged into this list in the Inspector.
    [Tooltip("Drag all combination recipes (ScriptableObject assets) here to make them known to the system.")]
    [SerializeField] private List<CombinationRecipe> _allRecipes = new List<CombinationRecipe>();

    // Implements a simple Singleton pattern for easy global access to the system.
    // Ensures there's only one instance of the ItemCombinationSystem throughout the game.
    public static ItemCombinationSystem Instance { get; private set; }

    private void Awake()
    {
        // Singleton enforcement: If an instance already exists and it's not this one,
        // destroy this GameObject to prevent duplicates.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ItemCombinationSystem instances found! Destroying duplicate.");
            Destroy(this.gameObject);
        }
        else
        {
            // Set this instance as the singleton.
            Instance = this;
            // Optionally: uncomment the line below if this system should persist across scene loads.
            // DontDestroyOnLoad(this.gameObject);
        }

        // Validate recipes on Awake to catch potential setup errors early.
        ValidateRecipes();
    }

    /// <summary>
    /// Performs basic validation on the loaded recipes to ensure they are properly configured.
    /// </summary>
    private void ValidateRecipes()
    {
        for (int i = 0; i < _allRecipes.Count; i++)
        {
            CombinationRecipe recipe = _allRecipes[i];
            if (recipe == null)
            {
                Debug.LogWarning($"ItemCombinationSystem has a null recipe entry at index {i}. Please remove it.");
                continue;
            }
            if (recipe.RequiredItems == null || recipe.RequiredItems.Count == 0)
            {
                Debug.LogWarning($"Recipe '{recipe.name}' has no required items. It will never be craftable.");
            }
            if (recipe.OutputItem == null)
            {
                Debug.LogWarning($"Recipe '{recipe.name}' has no output item. It will produce nothing.");
            }
        }
    }

    /// <summary>
    /// Attempts to combine a given list of items.
    /// This method iterates through all known recipes and checks if any recipe's requirements
    /// match the provided input items (considering item types and quantities, ignoring order).
    /// </summary>
    /// <param name="itemsToCombine">The list of ItemData objects the player is attempting to combine.</param>
    /// <returns>
    /// A <see cref="CombinationResult"/> struct containing:
    /// - Success: True if a combination was found, false otherwise.
    /// - OutputItem: The ItemData produced, if successful. Null otherwise.
    /// - OutputQuantity: The quantity of the output item, if successful. 0 otherwise.
    /// </returns>
    public CombinationResult TryCombine(List<ItemData> itemsToCombine)
    {
        // Handle edge cases: no items provided for combination.
        if (itemsToCombine == null || itemsToCombine.Count == 0)
        {
            Debug.LogWarning("ItemCombinationSystem: No items provided for combination.");
            return CombinationResult.NoCombination;
        }

        // Generate a frequency map for the items the player is trying to combine.
        // This allows us to efficiently compare against recipe requirements, ignoring the order of items.
        // Example: If itemsToCombine is [Wood, Stone, Wood], the map will be { "Wood_ID": 2, "Stone_ID": 1 }.
        Dictionary<string, int> inputItemsFrequency = itemsToCombine
            .GroupBy(item => item.ItemId) // Group by unique ItemId
            .ToDictionary(group => group.Key, group => group.Count()); // Count occurrences of each unique item

        // Iterate through each registered recipe to find a match.
        foreach (CombinationRecipe recipe in _allRecipes)
        {
            // Skip invalid or incomplete recipes.
            if (recipe == null || recipe.OutputItem == null || recipe.RequiredItems == null || recipe.RequiredItems.Count == 0)
            {
                continue;
            }

            // Get the frequency map for the current recipe's required items.
            Dictionary<string, int> recipeRequiredFrequency = recipe.GetRequiredItemsFrequencyMap();

            // --- Combination Match Logic ---
            // 1. Quick check: Do the total number of items match?
            // This is a crucial early exit if the counts don't match, preventing combinations with extra items.
            if (itemsToCombine.Count != recipe.RequiredItems.Count)
            {
                continue;
            }

            // 2. Do the number of *unique* item types match?
            if (inputItemsFrequency.Count != recipeRequiredFrequency.Count)
            {
                continue;
            }

            // 3. Detailed check: Does every required item in the recipe exist in the input items
            //    with the exact required quantity?
            bool match = true;
            foreach (var requiredPair in recipeRequiredFrequency)
            {
                string requiredItemId = requiredPair.Key;
                int requiredCount = requiredPair.Value;

                // If the input items don't contain the required item, or the quantities don't match,
                // then this recipe is not a match.
                if (!inputItemsFrequency.TryGetValue(requiredItemId, out int inputCount) || inputCount != requiredCount)
                {
                    match = false;
                    break; // No need to check further items for this recipe.
                }
            }

            // If 'match' is still true after checking all required items, we found a recipe!
            if (match)
            {
                Debug.Log($"Successfully combined items: {string.Join(", ", itemsToCombine.Select(i => i.DisplayName))} " +
                          $"-> {recipe.OutputItem.DisplayName} (x{recipe.OutputQuantity}) using recipe: {recipe.name}");
                return new CombinationResult(true, recipe.OutputItem, recipe.OutputQuantity);
            }
        }

        // If the loop completes, no recipe matched the input items.
        Debug.Log("No combination found for the given items.");
        return CombinationResult.NoCombination;
    }
}


// =====================================================================================================================
// Example Usage (for demonstration purposes)
// How another script (e.g., an InventoryManager or UI controller) would interact with the system.
// =====================================================================================================================
/*
 * // ExampleCraftingUI.cs
 * using UnityEngine;
 * using System.Collections.Generic;
 * using UnityEngine.UI; // If using UI elements
 * 
 * public class ExampleCraftingUI : MonoBehaviour
 * {
 *     // Assign these ItemData assets in the Inspector to simulate player's inventory selections
 *     [Header("Simulated Player Inventory Selections")]
 *     [SerializeField] private List<ItemData> _selectedItemsForCrafting = new List<ItemData>();
 * 
 *     [Header("UI References (Optional)")]
 *     [SerializeField] private Text _resultText;
 *     [SerializeField] private Button _combineButton;
 * 
 *     private void Start()
 *     {
 *         // Ensure the ItemCombinationSystem is in the scene.
 *         if (ItemCombinationSystem.Instance == null)
 *         {
 *             Debug.LogError("ItemCombinationSystem is not found in the scene! Please add it to a GameObject.");
 *             this.enabled = false; // Disable this script if the system isn't present
 *             return;
 *         }
 * 
 *         if (_combineButton != null)
 *         {
 *             _combineButton.onClick.AddListener(OnCombineButtonClicked);
 *         }
 *         UpdateResultText("Select items and click combine.");
 *     }
 * 
 *     private void OnCombineButtonClicked()
 *     {
 *         AttemptCombination();
 *     }
 * 
 *     public void AttemptCombination()
 *     {
 *         if (ItemCombinationSystem.Instance == null)
 *         {
 *             UpdateResultText("Error: Combination System not available.");
 *             return;
 *         }
 * 
 *         Debug.Log($"Attempting to combine {string.Join(", ", _selectedItemsForCrafting.Select(i => i.DisplayName))}");
 * 
 *         // 1. Call the combination system
 *         CombinationResult result = ItemCombinationSystem.Instance.TryCombine(_selectedItemsForCrafting);
 * 
 *         // 2. Process the result
 *         if (result.Success)
 *         {
 *             Debug.Log($"Crafted: {result.OutputItem.DisplayName} x{result.OutputQuantity}");
 *             UpdateResultText($"Success! Crafted: {result.OutputItem.DisplayName} x{result.OutputQuantity}");
 * 
 *             // In a real game, you would now:
 *             // - Remove _selectedItemsForCrafting from the player's inventory.
 *             // - Add result.OutputItem (x result.OutputQuantity) to the player's inventory.
 *             // - Clear _selectedItemsForCrafting or update UI.
 *             // Example: InventoryManager.Instance.RemoveItems(_selectedItemsForCrafting);
 *             //          InventoryManager.Instance.AddItem(result.OutputItem, result.OutputQuantity);
 *             //          _selectedItemsForCrafting.Clear(); // For this example, reset selected items
 *         }
 *         else
 *         {
 *             Debug.Log("Combination failed: No matching recipe found.");
 *             UpdateResultText("Combination failed: No matching recipe.");
 *         }
 *     }
 * 
 *     private void UpdateResultText(string message)
 *     {
 *         if (_resultText != null)
 *         {
 *             _resultText.text = message;
 *         }
 *     }
 * 
 *     // Example of adding items to the selection list dynamically (e.g., from UI button clicks)
 *     public void AddItemToSelection(ItemData item)
 *     {
 *         _selectedItemsForCrafting.Add(item);
 *         Debug.Log($"Added {item.DisplayName} to selection. Current selection: {string.Join(", ", _selectedItemsForCrafting.Select(i => i.DisplayName))}");
 *     }
 * 
 *     public void ClearSelection()
 *     {
 *         _selectedItemsForCrafting.Clear();
 *         Debug.Log("Cleared item selection.");
 *         UpdateResultText("Selection cleared. Select new items.");
 *     }
 * }
 */
```