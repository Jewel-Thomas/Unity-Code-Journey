// Unity Design Pattern Example: DataTableSystem
// This script demonstrates the DataTableSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The DataTableSystem design pattern is a powerful way to manage large sets of static game data (items, enemies, quests, configurations, etc.) in a structured and efficient manner. It separates your game's data from its logic, making it easier to balance, update, and manage game content without changing code.

This C# Unity example demonstrates a practical implementation of the DataTableSystem using:
1.  **CSV files** as the primary data source (common for game design).
2.  **ScriptableObjects** to represent and hold the loaded data in Unity.
3.  **Generics** for a flexible, reusable system.
4.  **Error Handling** for robust data loading.

---

### **Understanding the DataTableSystem Pattern**

1.  **Data Rows (e.g., `ItemData`):** These are simple C# classes or structs that define the schema for a single entry in your data table. Each property corresponds to a column in your data source (e.g., CSV). They typically implement an `IDataRow` interface to ensure they have a unique identifier.
2.  **Base Data Table (`BaseDataTable<T>`):** This is an abstract generic `ScriptableObject` that provides the core functionality for any data table. It holds a dictionary (`Dictionary<int, T>`) for fast lookups by ID and defines common methods like `GetById`, `GetAll`, and `FindByCondition`.
3.  **Concrete Data Table (e.g., `ItemDataTable`):** This class inherits from `BaseDataTable<T>` and specializes it for a specific type of data (e.g., `ItemData`). It's responsible for *how* its specific data type is parsed from the raw data source (e.g., parsing a CSV string into `ItemData` objects). Being a `ScriptableObject`, you can create assets for these tables in your Unity project.
4.  **Data Source (CSV `TextAsset`):** A plain text file (like a CSV) that contains your game data. It's referenced by the Concrete Data Table (e.g., `ItemDataTable`) as a `TextAsset`.
5.  **Initialization/Usage (e.g., `DataTableExample` MonoBehaviour):** A component that loads the `TextAsset`, passes its content to the `ScriptableObject` data table for parsing and population, and then uses the data table's methods to retrieve specific game data.

**Benefits:**
*   **Data-Driven Design:** Game logic is separated from game data.
*   **Easy Iteration:** Designers can modify data (e.g., item stats, enemy values) in a CSV without requiring a code change or recompilation.
*   **Performance:** Data is loaded once into a fast lookup structure (dictionary) at runtime.
*   **Scalability:** Easily add new data types or expand existing ones.
*   **Readability:** Clear structure for game data.

---

### **How to Use This Example in Unity:**

1.  **Create a C# Script:** Create a new C# script in your Unity project (e.g., `DataTableSystem.cs`) and copy all the code below into it.
2.  **Create a CSV File:**
    *   Create a new text file named `ItemData.csv` (or any name you prefer) in your project.
    *   Copy and paste the following content into `ItemData.csv`:
        ```csv
        ID,Name,Description,Type,Value,IconPath
        1001,Sword,A sharp blade.,Weapon,50,Icons/Items/SwordIcon
        1002,Shield,Protects against attacks.,Armor,30,Icons/Items/ShieldIcon
        1003,Potion,Restores health.,Consumable,20,Icons/Items/PotionIcon
        1004,Ring of Power,Grants great strength.,Accessory,100,Icons/Items/RingIcon
        1005,Bread,A simple food item.,Food,5,Icons/Items/BreadIcon
        ```
3.  **Place CSV in Resources:** Move `ItemData.csv` into a `Resources` folder (e.g., `Assets/Resources/Data/ItemData.csv`). If you don't have a `Resources` folder, create one.
4.  **Create ItemDataTable Asset:**
    *   In Unity, right-click in your Project window -> `Create` -> `Data Tables` -> `Item Data Table`.
    *   Name the new asset `ItemDataTableAsset`.
    *   In the Inspector for `ItemDataTableAsset`, drag your `ItemData.csv` (from the `Resources/Data` folder) into the `Csv File` slot.
5.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., `GameManager`).
6.  **Add `DataTableExample` Component:** Add the `DataTableExample` script component to your `GameManager` GameObject.
7.  **Assign ItemDataTable Asset:** In the Inspector for the `DataTableExample` component, drag your `ItemDataTableAsset` (created in step 4) into the `Item Data Table Asset` slot.
8.  **Run the Scene:** Play your Unity scene. You will see detailed logs in the Console demonstrating the data loading and retrieval.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like Where, FirstOrDefault, etc.
using System.IO;   // For string processing if needed, though mostly using TextAsset.text
using System;      // For general purpose types and exceptions

/// <summary>
/// INTERFACE: IDataRow
/// Defines the contract for any data entry that can be stored in a DataTable.
/// All data entries must have a unique integer ID.
/// </summary>
public interface IDataRow
{
    int Id { get; }
}

/// <summary>
/// CONCRETE CLASS: ItemData
/// Represents a single row of item data in our game.
/// Implements IDataRow, providing specific properties for an item.
/// </summary>
[System.Serializable] // Makes this class serializable, useful for debugging or other Unity features.
public class ItemData : IDataRow
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Type { get; private set; }
    public int Value { get; private set; }
    public string IconPath { get; private set; } // Path to an icon in Resources, e.g., "Icons/Items/SwordIcon"

    // Constructor to easily create ItemData objects.
    public ItemData(int id, string name, string description, string type, int value, string iconPath)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        Value = value;
        IconPath = iconPath;
    }

    public override string ToString()
    {
        return $"[ItemData] ID: {Id}, Name: {Name}, Type: {Type}, Value: {Value}";
    }
}

/// <summary>
/// ABSTRACT BASE CLASS: BaseDataTable<T>
/// A generic ScriptableObject base class for all data tables.
/// It provides common functionality for loading, storing, and accessing data.
/// T must be a class that implements IDataRow.
/// </summary>
/// <typeparam name="T">The type of data row this table will hold (e.g., ItemData).</typeparam>
public abstract class BaseDataTable<T> : ScriptableObject where T : class, IDataRow
{
    // Dictionary to store data for fast lookups by ID.
    // [System.NonSerialized] prevents Unity from trying to serialize this dictionary,
    // as it's populated at runtime from the CSV TextAsset.
    [System.NonSerialized]
    protected Dictionary<int, T> dataMap = new Dictionary<int, T>();

    // Property to check if the data table has been loaded.
    public bool IsLoaded => dataMap.Count > 0;

    /// <summary>
    /// Abstract method to parse the raw CSV content and populate the dataMap.
    /// Derived classes must implement this to define how their specific data type is parsed.
    /// </summary>
    /// <param name="csvContent">The raw string content of the CSV file.</param>
    protected abstract void ParseCsvAndPopulate(string csvContent);

    /// <summary>
    /// Initializes the data table by loading data from the provided TextAsset.
    /// This method should be called once, typically at game start or when the table is first needed.
    /// </summary>
    /// <param name="csvTextAsset">The TextAsset containing the CSV data.</param>
    public void Initialize(TextAsset csvTextAsset)
    {
        if (IsLoaded)
        {
            Debug.LogWarning($"DataTable '{name}' already loaded. Re-initializing.");
            dataMap.Clear(); // Clear existing data if re-initializing.
        }

        if (csvTextAsset == null)
        {
            Debug.LogError($"DataTable '{name}' initialization failed: CSV TextAsset is null.");
            return;
        }

        try
        {
            ParseCsvAndPopulate(csvTextAsset.text);
            Debug.Log($"DataTable '{name}' loaded successfully with {dataMap.Count} entries.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing DataTable '{name}' from CSV '{csvTextAsset.name}': {ex.Message}\n{ex.StackTrace}");
            dataMap.Clear(); // Ensure map is empty on failure.
        }
    }

    /// <summary>
    /// Retrieves a data row by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the data row.</param>
    /// <returns>The data row of type T, or null if not found.</returns>
    public T GetById(int id)
    {
        if (!IsLoaded)
        {
            Debug.LogWarning($"DataTable '{name}' not loaded. Cannot retrieve data for ID: {id}.");
            return null;
        }

        if (dataMap.TryGetValue(id, out T data))
        {
            return data;
        }
        Debug.LogWarning($"Data with ID '{id}' not found in '{name}'.");
        return null;
    }

    /// <summary>
    /// Retrieves all data rows in the table.
    /// </summary>
    /// <returns>An IEnumerable of all data rows.</returns>
    public IEnumerable<T> GetAll()
    {
        if (!IsLoaded)
        {
            Debug.LogWarning($"DataTable '{name}' not loaded. Returning empty collection.");
            return Enumerable.Empty<T>();
        }
        return dataMap.Values;
    }

    /// <summary>
    /// Finds data rows that satisfy a given predicate (condition).
    /// </summary>
    /// <param name="predicate">A function that defines the search condition.</param>
    /// <returns>An IEnumerable of data rows that match the condition.</returns>
    public IEnumerable<T> FindByCondition(Func<T, bool> predicate)
    {
        if (!IsLoaded)
        {
            Debug.LogWarning($"DataTable '{name}' not loaded. Returning empty collection.");
            return Enumerable.Empty<T>();
        }
        return dataMap.Values.Where(predicate);
    }
}


/// <summary>
/// CONCRETE CLASS: ItemDataTable
/// Specializes BaseDataTable for ItemData.
/// This is a ScriptableObject asset that holds the loaded ItemData.
/// </summary>
[CreateAssetMenu(fileName = "NewItemDataTable", menuName = "Data Tables/Item Data Table")]
public class ItemDataTable : BaseDataTable<ItemData>
{
    // Public TextAsset field to assign the CSV file directly in the Unity Inspector.
    // This makes it easy to link your data file to your ScriptableObject.
    public TextAsset csvFile;

    /// <summary>
    /// Concrete implementation of parsing for ItemData.
    /// Reads the CSV content, parses each line, and populates the dataMap.
    /// </summary>
    /// <param name="csvContent">The raw string content of the CSV file.</param>
    protected override void ParseCsvAndPopulate(string csvContent)
    {
        dataMap.Clear(); // Ensure clean slate on parse.

        // Split the CSV content into lines.
        // StringSplitOptions.RemoveEmptyEntries prevents processing blank lines.
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1) // Expect at least a header and one data row.
        {
            Debug.LogError($"ItemDataTable '{name}': CSV file is empty or only contains headers.");
            return;
        }

        // The first line is typically the header, containing column names.
        // We'll skip it for data parsing but could use it for validation.
        string headerLine = lines[0];
        string[] headers = headerLine.Split(','); // Simple split, assumes no commas in values.

        // Basic header validation (optional but good practice)
        if (headers.Length < 6 ||
            headers[0] != "ID" || headers[1] != "Name" || headers[2] != "Description" ||
            headers[3] != "Type" || headers[4] != "Value" || headers[5] != "IconPath")
        {
            Debug.LogWarning($"ItemDataTable '{name}': CSV header mismatch or incomplete. Expected: ID,Name,Description,Type,Value,IconPath. Found: {headerLine}");
            // Proceed anyway, but be aware of potential issues. Or throw an exception.
        }

        // Iterate through each data line (skipping the header).
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');

            // Basic validation: ensure we have enough columns.
            if (values.Length < 6)
            {
                Debug.LogWarning($"ItemDataTable '{name}': Skipping malformed line {i + 1}: '{line}' (not enough columns).");
                continue;
            }

            try
            {
                // Parse values into appropriate types.
                int id = int.Parse(values[0]);
                string itemName = values[1];
                string description = values[2];
                string itemType = values[3];
                int itemValue = int.Parse(values[4]);
                string iconPath = values[5];

                // Create a new ItemData object.
                ItemData item = new ItemData(id, itemName, description, itemType, itemValue, iconPath);

                // Add to the dictionary. Check for duplicate IDs.
                if (dataMap.ContainsKey(id))
                {
                    Debug.LogWarning($"ItemDataTable '{name}': Duplicate ID '{id}' found for item '{itemName}'. Overwriting previous entry.");
                    dataMap[id] = item; // Overwrite or handle as error.
                }
                else
                {
                    dataMap.Add(id, item);
                }
            }
            catch (FormatException fe)
            {
                Debug.LogError($"ItemDataTable '{name}': Parsing error on line {i + 1}: '{line}'. Format exception: {fe.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ItemDataTable '{name}': An unexpected error occurred on line {i + 1}: '{line}'. Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper method to initialize this specific data table from its assigned `csvFile` TextAsset.
    /// This is typically called by a manager or an example MonoBehaviour.
    /// </summary>
    public void Initialize()
    {
        Initialize(csvFile);
    }
}


/// <summary>
/// MONOBEHAVIOUR: DataTableExample
/// An example MonoBehaviour to demonstrate how to use the DataTableSystem.
/// It loads the ItemDataTable and performs various data retrieval operations.
/// </summary>
public class DataTableExample : MonoBehaviour
{
    // Assign your ItemDataTable ScriptableObject asset in the Unity Inspector.
    public ItemDataTable itemDataTableAsset;

    void Awake()
    {
        if (itemDataTableAsset == null)
        {
            Debug.LogError("Item Data Table Asset not assigned in DataTableExample. Please assign the ScriptableObject asset in the Inspector.");
            return;
        }

        // 1. Initialize the DataTable.
        // This is crucial. It loads the CSV data into the dictionary for runtime use.
        // It's good practice to do this once, e.g., at game start or scene load.
        itemDataTableAsset.Initialize();

        Debug.Log("--- DataTableSystem Example Usage ---");

        // 2. Retrieve a specific item by ID.
        int itemIdToFind = 1001;
        ItemData sword = itemDataTableAsset.GetById(itemIdToFind);
        if (sword != null)
        {
            Debug.Log($"Found Item by ID {itemIdToFind}: {sword.Name}, Description: {sword.Description}, Value: {sword.Value}");
            // Simulate loading an icon for the item
            // This would typically involve Resources.Load<Sprite>(sword.IconPath);
            Debug.Log($"   (Icon path for '{sword.Name}': {sword.IconPath})");
        }
        else
        {
            Debug.LogWarning($"Item with ID {itemIdToFind} not found.");
        }

        // Try to get a non-existent item.
        ItemData nonExistentItem = itemDataTableAsset.GetById(9999);
        if (nonExistentItem == null)
        {
            Debug.Log("Confirmed: Item with ID 9999 does not exist (as expected).");
        }

        // 3. Get all items.
        Debug.Log("\n--- All Items ---");
        foreach (ItemData item in itemDataTableAsset.GetAll())
        {
            Debug.Log($"- {item.Id}: {item.Name} ({item.Type})");
        }

        // 4. Find items by condition (e.g., all weapons).
        Debug.Log("\n--- Weapons ---");
        IEnumerable<ItemData> weapons = itemDataTableAsset.FindByCondition(item => item.Type == "Weapon");
        foreach (ItemData weapon in weapons)
        {
            Debug.Log($"- Weapon: {weapon.Name} (Value: {weapon.Value})");
        }

        // 5. Find items by condition (e.g., items with value > 40).
        Debug.Log("\n--- High-Value Items (Value > 40) ---");
        IEnumerable<ItemData> highValueItems = itemDataTableAsset.FindByCondition(item => item.Value > 40);
        foreach (ItemData item in highValueItems)
        {
            Debug.Log($"- High-Value: {item.Name} (Value: {item.Value})");
        }

        // 6. Find a specific item by name (using LINQ on GetAll or FindByCondition).
        string itemNameQuery = "Potion";
        ItemData potion = itemDataTableAsset.GetAll().FirstOrDefault(item => item.Name == itemNameQuery);
        if (potion != null)
        {
            Debug.Log($"\nFound '{itemNameQuery}' by name: {potion.Description}");
        }
        else
        {
            Debug.LogWarning($"\nItem named '{itemNameQuery}' not found.");
        }

        Debug.Log("\n--- DataTableSystem Example End ---");
    }

    // Optional: If you need to reload data during development (e.g., after CSV changes).
    // In a production game, data is typically loaded once.
    [ContextMenu("Reload Item Data Table")]
    void ReloadItemDataTable()
    {
        if (itemDataTableAsset != null)
        {
            itemDataTableAsset.Initialize();
            Debug.Log("Item Data Table reloaded via Context Menu.");
            // Re-run example logic if needed for testing reload.
            // Awake(); // Be careful if Awake has other side effects.
        }
        else
        {
            Debug.LogError("Item Data Table Asset not assigned.");
        }
    }
}
```