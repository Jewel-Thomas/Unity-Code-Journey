// Unity Design Pattern Example: GridBasedInventory
// This script demonstrates the GridBasedInventory pattern in Unity
// Generated automatically - ready to use in your Unity project

The GridBasedInventory pattern is a common and practical design for games where players manage items within a limited, grid-based space (like an RPG backpack or a crafting station). This pattern focuses on efficiently managing item placement, checking for available space, and handling item removal within a defined 2D grid.

Here's a complete C# Unity example demonstrating this pattern.

---

**1. Create the C# Script:**

Create a new C# script named `GridBasedInventory` in your Unity project.

**2. Copy and Paste the Code:**

Replace the default content of `GridBasedInventory.cs` with the following code:

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// This script demonstrates the 'GridBasedInventory' design pattern in Unity.
///
/// Pattern Overview:
/// The GridBasedInventory pattern manages items within a fixed-size 2D grid.
/// Each item has a size (width and height) and occupies multiple cells in the grid.
/// The pattern provides logic for:
/// 1. Defining items with their properties (name, icon, size).
/// 2. Storing instances of these items in specific grid positions.
/// 3. Checking if an item can fit at a given position.
/// 4. Adding and removing items from the inventory.
/// 5. Providing an event for UI updates when the inventory changes.
///
/// Key Components:
/// - InventoryItemData (ScriptableObject): Defines the static properties of an item type.
/// - InventoryItem (Serializable class): Represents an *instance* of an item placed in the inventory,
///   including its position and a reference to its InventoryItemData.
/// - GridBasedInventory (MonoBehaviour): Manages the 2D grid, placement logic, and item storage.
///
/// How it works:
/// - The inventory internally uses a 2D array (InventoryItem[,]) to represent the grid.
///   Each cell in this array either holds a reference to the InventoryItem occupying it, or null if empty.
///   Note: If an item is 2x2, all four cells it occupies will point to the *same* InventoryItem instance.
///   This allows easily identifying which item is at any given cell.
/// - When placing an item, the system checks if all required cells are within bounds and currently empty.
/// - When an item is successfully placed, all cells it occupies are updated to point to the new InventoryItem instance.
/// - An event (OnInventoryChanged) is triggered for UI or other systems to react.
/// </summary>

#region InventoryItemData
/// <summary>
/// A ScriptableObject to define the static properties of an inventory item.
/// This allows creating reusable item types as assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Inventory Item Data", order = 1)]
public class InventoryItemData : ScriptableObject
{
    public string id = Guid.NewGuid().ToString(); // Unique identifier for the item type
    public string itemName = "New Item";
    [TextArea(3, 5)] public string description = "A generic item.";
    public Sprite itemIcon;
    public int width = 1; // How many grid cells wide the item is
    public int height = 1; // How many grid cells tall the item is

    [Tooltip("If true, the item can be rotated (e.g., a 1x2 item becomes 2x1). " +
             "This example does not implement rotation, but it's a common extension.")]
    public bool isRotatable = false; // Example property, rotation logic not implemented in this version.

    // Basic validation
    private void OnValidate()
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;
    }
}
#endregion

#region InventoryItem
/// <summary>
/// Represents an instance of an item currently placed in the inventory.
/// This class is [Serializable] so Unity can save it as part of a list
/// within the GridBasedInventory MonoBehaviour.
/// </summary>
[Serializable]
public class InventoryItem
{
    public InventoryItemData itemData; // Reference to the static item data
    public int x; // Top-left X coordinate in the grid
    public int y; // Top-left Y coordinate in the grid
    // public int rotationState; // Example for rotation (0, 90, 180, 270 degrees) - not used in this example.

    public InventoryItem(InventoryItemData data, int gridX, int gridY)
    {
        itemData = data;
        x = gridX;
        y = gridY;
    }

    // Convenience properties to get actual width/height based on potential rotation (if implemented)
    public int GetCurrentWidth()
    {
        // For this example, no rotation, so it's always itemData.width
        return itemData.width;
    }

    public int GetCurrentHeight()
    {
        // For this example, no rotation, so it's always itemData.height
        return itemData.height;
    }
}
#endregion

/// <summary>
/// The core MonoBehaviour for managing the grid-based inventory.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class GridBasedInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int inventoryWidth = 5;
    [SerializeField] private int inventoryHeight = 5;

    // The 2D array representing the inventory grid.
    // Each cell stores a reference to the InventoryItem occupying it, or null if empty.
    private InventoryItem[,] grid;

    // A list of all item instances currently placed in the inventory.
    // Useful for quick iteration, saving/loading, or UI representation.
    [SerializeField] private List<InventoryItem> placedItems = new List<InventoryItem>();

    // Event for other systems (e.g., UI) to subscribe to,
    // notifying them when the inventory content changes.
    public event Action OnInventoryChanged;

    public int InventoryWidth => inventoryWidth;
    public int InventoryHeight => inventoryHeight;
    public IReadOnlyList<InventoryItem> PlacedItems => placedItems; // Public read-only access to placed items

    private void Awake()
    {
        InitializeInventory();
    }

    /// <summary>
    /// Initializes the internal grid structure.
    /// </summary>
    private void InitializeInventory()
    {
        if (inventoryWidth <= 0) inventoryWidth = 1;
        if (inventoryHeight <= 0) inventoryHeight = 1;

        grid = new InventoryItem[inventoryWidth, inventoryHeight];

        // Re-place any previously serialized items (e.g., after Play mode stop/start or scene load)
        // This ensures the internal `grid` state matches `placedItems`.
        List<InventoryItem> itemsToReAdd = new List<InventoryItem>(placedItems);
        placedItems.Clear(); // Clear and re-add to validate positions and update grid
        foreach (InventoryItem item in itemsToReAdd)
        {
            // Note: If an item was serialized at an invalid position, it won't be re-added here.
            // This implicitly cleans up invalid states.
            TryAddItem(item.itemData, item.x, item.y, out _);
        }
        
        Debug.Log($"Inventory initialized: {inventoryWidth}x{inventoryHeight} grid.");
    }

    /// <summary>
    /// Checks if a given item (based on its data) can be placed at a specific top-left coordinate (targetX, targetY).
    /// It verifies bounds and checks if all cells the item would occupy are currently empty.
    /// </summary>
    /// <param name="itemData">The static data of the item to check.</param>
    /// <param name="targetX">The target top-left X coordinate.</param>
    /// <param name="targetY">The target top-left Y coordinate.</param>
    /// <returns>True if the item can be placed, false otherwise.</returns>
    public bool CanAddItem(InventoryItemData itemData, int targetX, int targetY)
    {
        if (itemData == null)
        {
            Debug.LogWarning("CanAddItem: itemData is null.");
            return false;
        }

        int itemWidth = itemData.width;
        int itemHeight = itemData.height;

        // 1. Check if the item is within inventory bounds
        if (targetX < 0 || targetY < 0 ||
            targetX + itemWidth > inventoryWidth ||
            targetY + itemHeight > inventoryHeight)
        {
            // Debug.Log($"CanAddItem: Item {itemData.itemName} ({itemWidth}x{itemHeight}) at ({targetX},{targetY}) is out of bounds.");
            return false;
        }

        // 2. Check if all cells the item would occupy are empty
        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                if (grid[targetX + x, targetY + y] != null)
                {
                    // Debug.Log($"CanAddItem: Cell ({targetX + x},{targetY + y}) is already occupied by {grid[targetX + x, targetY + y].itemData.itemName}.");
                    return false; // Cell is already occupied
                }
            }
        }
        return true; // All checks passed, item can be placed
    }

    /// <summary>
    /// Attempts to add an item to the inventory at the specified coordinates.
    /// </summary>
    /// <param name="itemData">The static data of the item to add.</param>
    /// <param name="targetX">The target top-left X coordinate.</param>
    /// <param name="targetY">The target top-left Y coordinate.</param>
    /// <param name="placedItem">Output parameter: The InventoryItem instance that was placed, or null if failed.</param>
    /// <returns>True if the item was successfully added, false otherwise.</returns>
    public bool TryAddItem(InventoryItemData itemData, int targetX, int targetY, out InventoryItem placedItem)
    {
        placedItem = null;
        if (itemData == null)
        {
            Debug.LogWarning("TryAddItem: itemData is null, cannot add.");
            return false;
        }

        if (!CanAddItem(itemData, targetX, targetY))
        {
            // Debug.Log($"TryAddItem: Cannot add {itemData.itemName} at ({targetX},{targetY}). Space occupied or out of bounds.");
            return false;
        }

        // Create a new instance of InventoryItem for this placement
        InventoryItem newItem = new InventoryItem(itemData, targetX, targetY);
        placedItems.Add(newItem);

        // Mark all cells occupied by this new item instance
        for (int x = 0; x < newItem.GetCurrentWidth(); x++)
        {
            for (int y = 0; y < newItem.GetCurrentHeight(); y++)
            {
                grid[targetX + x, targetY + y] = newItem;
            }
        }

        placedItem = newItem;
        Debug.Log($"Successfully added {itemData.itemName} ({newItem.GetCurrentWidth()}x{newItem.GetCurrentHeight()}) at ({targetX},{targetY}).");
        OnInventoryChanged?.Invoke(); // Notify subscribers that the inventory has changed
        return true;
    }

    /// <summary>
    /// Removes a specific InventoryItem instance from the inventory.
    /// It clears the grid cells it occupied and removes it from the placedItems list.
    /// </summary>
    /// <param name="itemToRemove">The specific InventoryItem instance to remove.</param>
    /// <returns>True if the item was found and removed, false otherwise.</returns>
    public bool RemoveItem(InventoryItem itemToRemove)
    {
        if (itemToRemove == null || !placedItems.Contains(itemToRemove))
        {
            Debug.LogWarning("RemoveItem: Item is null or not found in inventory.");
            return false;
        }

        // Clear all grid cells occupied by this item
        int itemX = itemToRemove.x;
        int itemY = itemToRemove.y;
        int itemWidth = itemToRemove.GetCurrentWidth();
        int itemHeight = itemToRemove.GetCurrentHeight();

        for (int x = 0; x < itemWidth; x++)
        {
            for (int y = 0; y < itemHeight; y++)
            {
                // Ensure we only clear cells that *actually* point to this item instance
                // (Important in case of overlapping logic errors, though CanAddItem should prevent this).
                if (itemX + x >= 0 && itemX + x < inventoryWidth &&
                    itemY + y >= 0 && itemY + y < inventoryHeight &&
                    grid[itemX + x, itemY + y] == itemToRemove)
                {
                    grid[itemX + x, itemY + y] = null;
                }
            }
        }

        placedItems.Remove(itemToRemove);
        Debug.Log($"Removed {itemToRemove.itemData.itemName} from inventory.");
        OnInventoryChanged?.Invoke(); // Notify subscribers
        return true;
    }

    /// <summary>
    /// Gets the InventoryItem instance at a specific grid coordinate.
    /// All cells occupied by an item will return the same item instance.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The InventoryItem at (x,y), or null if the cell is empty or out of bounds.</returns>
    public InventoryItem GetItemAt(int x, int y)
    {
        if (x < 0 || x >= inventoryWidth || y < 0 || y >= inventoryHeight)
        {
            return null; // Out of bounds
        }
        return grid[x, y];
    }

    /// <summary>
    /// Finds the first available empty slot (top-left coordinate) where an item can fit.
    /// This is a basic search, more advanced inventories might use a packing algorithm.
    /// </summary>
    /// <param name="itemData">The item data to find space for.</param>
    /// <param name="foundX">Output: The X coordinate of the found slot.</param>
    /// <param name="foundY">Output: The Y coordinate of the found slot.</param>
    /// <returns>True if an empty slot was found, false otherwise.</returns>
    public bool GetEmptySlot(InventoryItemData itemData, out int foundX, out int foundY)
    {
        foundX = -1;
        foundY = -1;

        if (itemData == null) return false;

        for (int y = 0; y <= inventoryHeight - itemData.height; y++)
        {
            for (int x = 0; x <= inventoryWidth - itemData.width; x++)
            {
                if (CanAddItem(itemData, x, y))
                {
                    foundX = x;
                    foundY = y;
                    return true;
                }
            }
        }
        return false;
    }

    // --- DEBUGGING / VISUALIZATION ---
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (drawGizmos && grid != null)
        {
            Gizmos.color = Color.grey;
            // Draw grid lines
            for (int x = 0; x <= inventoryWidth; x++)
            {
                Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, inventoryHeight, 0));
            }
            for (int y = 0; y <= inventoryHeight; y++)
            {
                Gizmos.DrawLine(new Vector3(0, y, 0), new Vector3(inventoryWidth, y, 0));
            }

            // Draw occupied item areas
            foreach (InventoryItem item in placedItems)
            {
                if (item != null && item.itemData != null)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent green
                    Gizmos.DrawCube(
                        new Vector3(item.x + item.GetCurrentWidth() / 2f, item.y + item.GetCurrentHeight() / 2f, 0.1f),
                        new Vector3(item.GetCurrentWidth(), item.GetCurrentHeight(), 0.1f)
                    );

                    Gizmos.color = Color.white;
                    if (item.itemData.itemIcon != null)
                    {
                        // Draw a simple icon in the center if available
                        Gizmos.DrawIcon(
                            new Vector3(item.x + item.GetCurrentWidth() / 2f, item.y + item.GetCurrentHeight() / 2f, 0f),
                            item.itemData.itemIcon.name,
                            true
                        );
                    }
                    else
                    {
                        // Fallback text for debugging
                        // Unity does not have a native Gizmos.DrawText.
                        // You'd typically use a Handles.Label for Editor UI, or actual UI elements for runtime.
                        // For a simple visual, we can just outline or change color.
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(
                            new Vector3(item.x + item.GetCurrentWidth() / 2f, item.y + item.GetCurrentHeight() / 2f, 0.1f),
                            new Vector3(item.GetCurrentWidth(), item.GetCurrentHeight(), 0.1f)
                        );
                    }
                }
            }
        }
    }
}
```

---

**3. Example Usage in Unity:**

Follow these steps to set up and test the inventory in your Unity project:

**A. Create Inventory Item Data Assets:**

1.  In your Unity Project window, right-click -> `Create` -> `Inventory` -> `Inventory Item Data`.
2.  Name the new asset something like "HealthPotionData".
3.  In the Inspector for "HealthPotionData":
    *   Set `Item Name`: "Health Potion"
    *   Set `Width`: `1`
    *   Set `Height`: `1`
    *   (Optional) Assign a `Sprite` to `Item Icon` for visual debugging.
4.  Repeat the process to create a few more items:
    *   **"LongSwordData"**: `Width: 1`, `Height: 3`
    *   **"ShieldData"**: `Width: 2`, `Height: 2`
    *   **"LargePouchData"**: `Width: 3`, `Height: 2`

**B. Set up the Inventory GameObject:**

1.  In your Unity Hierarchy, create an empty GameObject (right-click -> `Create Empty`).
2.  Name it "PlayerInventory".
3.  Drag the `GridBasedInventory` script onto the "PlayerInventory" GameObject.
4.  In the Inspector for "PlayerInventory":
    *   Set `Inventory Width`: `5`
    *   Set `Inventory Height`: `5`
    *   Ensure `Draw Gizmos` is checked to see the grid in the Scene view.

**C. Create a Test Script (InventoryDebugger):**

Create a new C# script named `InventoryDebugger` and attach it to the "PlayerInventory" GameObject (or any other GameObject in your scene). This script will interact with `GridBasedInventory`.

```csharp
using UnityEngine;
using System.Collections.Generic;

public class InventoryDebugger : MonoBehaviour
{
    [SerializeField] private GridBasedInventory playerInventory;

    [Header("Items to Test Add")]
    [SerializeField] private InventoryItemData healthPotionData;
    [SerializeField] private InventoryItemData longSwordData;
    [SerializeField] private InventoryItemData shieldData;
    [SerializeField] private InventoryItemData largePouchData;
    [SerializeField] private InventoryItemData unplaceableItemData; // For testing failed placements

    private InventoryItem placedHealthPotion; // Keep reference to remove specific instance later
    private InventoryItem placedLongSword;
    private InventoryItem placedShield;
    private InventoryItem placedLargePouch;

    void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponent<GridBasedInventory>();
            if (playerInventory == null)
            {
                Debug.LogError("InventoryDebugger requires a GridBasedInventory component on the same GameObject or assigned in inspector.");
                enabled = false;
                return;
            }
        }

        // Subscribe to inventory changes to log them
        playerInventory.OnInventoryChanged += LogInventoryState;
        Debug.Log("Inventory Debugger ready. Press keys to interact.");
        LogInventoryState(); // Initial state
    }

    void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= LogInventoryState;
        }
    }

    void Update()
    {
        // Add Items
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryAddAtSpecificPos(healthPotionData, 0, 0, out placedHealthPotion);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryAddAtSpecificPos(longSwordData, 1, 0, out placedLongSword);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryAddAtSpecificPos(shieldData, 1, 3, out placedShield);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TryAddAtSpecificPos(largePouchData, 3, 0, out placedLargePouch);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TryAddUsingAutoSlot(healthPotionData); // Try to auto-place another potion
        if (Input.GetKeyDown(KeyCode.Alpha6)) TryAddUsingAutoSlot(unplaceableItemData); // Item might be too big or no space

        // Remove Items
        if (Input.GetKeyDown(KeyCode.Q)) RemoveSpecificItem(placedHealthPotion);
        if (Input.GetKeyDown(KeyCode.W)) RemoveSpecificItem(placedLongSword);
        if (Input.GetKeyDown(KeyCode.E)) RemoveSpecificItem(placedShield);
        if (Input.GetKeyDown(KeyCode.R)) RemoveSpecificItem(placedLargePouch);
        if (Input.GetKeyDown(KeyCode.T)) RemoveRandomItem(); // Remove any random item

        // Debug info
        if (Input.GetKeyDown(KeyCode.Space)) LogInventoryState();
    }

    void TryAddAtSpecificPos(InventoryItemData itemData, int x, int y, out InventoryItem placedItemRef)
    {
        if (itemData == null) { Debug.LogWarning("Item data is null, cannot add."); placedItemRef = null; return; }
        Debug.Log($"Attempting to add {itemData.itemName} at ({x},{y})...");
        if (playerInventory.TryAddItem(itemData, x, y, out placedItemRef))
        {
            Debug.Log($"Successfully added {itemData.itemName} at ({x},{y}).");
        }
        else
        {
            Debug.Log($"Failed to add {itemData.itemName} at ({x},{y}).");
        }
    }

    void TryAddUsingAutoSlot(InventoryItemData itemData)
    {
        if (itemData == null) { Debug.LogWarning("Item data is null, cannot add."); return; }
        Debug.Log($"Attempting to add {itemData.itemName} using auto-slotting...");
        if (playerInventory.GetEmptySlot(itemData, out int x, out int y))
        {
            Debug.Log($"Found empty slot for {itemData.itemName} at ({x},{y}).");
            if (playerInventory.TryAddItem(itemData, x, y, out InventoryItem placedItem))
            {
                Debug.Log($"Successfully auto-added {itemData.itemName} at ({x},{y}).");
                // Store reference if needed, e.g., if it's a potion we want to 'use'
                if (itemData == healthPotionData) placedHealthPotion = placedItem;
            }
            else
            {
                Debug.Log($"Failed to auto-add {itemData.itemName} at ({x},{y}) after finding slot.");
            }
        }
        else
        {
            Debug.Log($"No empty slot found for {itemData.itemName}.");
        }
    }

    void RemoveSpecificItem(InventoryItem itemToRemove)
    {
        if (itemToRemove == null) { Debug.LogWarning("No item instance to remove."); return; }
        Debug.Log($"Attempting to remove {itemToRemove.itemData.itemName}...");
        if (playerInventory.RemoveItem(itemToRemove))
        {
            Debug.Log($"{itemToRemove.itemData.itemName} removed successfully.");
            // Nullify the reference so we don't try to remove it again
            if (itemToRemove == placedHealthPotion) placedHealthPotion = null;
            else if (itemToRemove == placedLongSword) placedLongSword = null;
            else if (itemToRemove == placedShield) placedShield = null;
            else if (itemToRemove == placedLargePouch) placedLargePouch = null;
        }
        else
        {
            Debug.Log($"Failed to remove {itemToRemove.itemData.itemName}.");
        }
    }

    void RemoveRandomItem()
    {
        if (playerInventory.PlacedItems.Count == 0)
        {
            Debug.Log("Inventory is empty, nothing to remove randomly.");
            return;
        }

        int randomIndex = Random.Range(0, playerInventory.PlacedItems.Count);
        InventoryItem itemToRemove = playerInventory.PlacedItems[randomIndex];
        Debug.Log($"Attempting to remove random item: {itemToRemove.itemData.itemName}...");
        RemoveSpecificItem(itemToRemove);
    }

    void LogInventoryState()
    {
        Debug.Log("--- Inventory State ---");
        if (playerInventory.PlacedItems.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }

        string state = "Items in inventory:\n";
        foreach (InventoryItem item in playerInventory.PlacedItems)
        {
            state += $"- {item.itemData.itemName} ({item.itemData.width}x{item.itemData.height}) at ({item.x},{item.y})\n";
        }
        Debug.Log(state);
    }
}
```

**D. Assign Item Data in the Inspector:**

1.  Select your "PlayerInventory" GameObject in the Hierarchy.
2.  In the Inspector, locate the `Inventory Debugger` component.
3.  Drag and drop the `InventoryItemData` assets you created earlier (`HealthPotionData`, `LongSwordData`, `ShieldData`, `LargePouchData`) into their corresponding fields in the `Inventory Debugger`.
4.  For `Unplaceable Item Data`, create a new `Inventory Item Data` asset, perhaps named "HugeItemData", and set its `Width` to `10` and `Height` to `10` (larger than your 5x5 inventory). Drag this asset into the field.

**E. Run the Scene:**

1.  Press Play in the Unity Editor.
2.  Open the Console window (`Window` -> `General` -> `Console`).
3.  Observe the debug messages and the visual grid in the Scene view (you might need to zoom in and position the camera to see the grid around (0,0,0)).

**F. Interact with the Inventory (While in Play Mode):**

*   Press `1`: Add Health Potion (1x1) at (0,0)
*   Press `2`: Add Long Sword (1x3) at (1,0)
*   Press `3`: Add Shield (2x2) at (1,3)
*   Press `4`: Add Large Pouch (3x2) at (3,0)
*   Press `5`: Try to auto-add another Health Potion (should find the next available 1x1 slot)
*   Press `6`: Try to auto-add a Huge Item (should fail due to no space)

*   Press `Q`: Remove the first Health Potion
*   Press `W`: Remove the Long Sword
*   Press `E`: Remove the Shield
*   Press `R`: Remove the Large Pouch
*   Press `T`: Remove a random item from the inventory

*   Press `Space`: Log the current state of the inventory to the console.

You'll see messages in the Console confirming successful or failed placements/removals, and the `OnDrawGizmos` will update the grid visualization in the Scene view.

---

This example provides a robust foundation for a grid-based inventory system. You can extend it further by:
*   Implementing item rotation.
*   Adding drag-and-drop functionality for a UI.
*   Creating a proper UI system to visualize the grid and items using `GridLayoutGroup` and UI elements.
*   Integrating with a saving/loading system.
*   Adding item stacking for stackable items.