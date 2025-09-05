// Unity Design Pattern Example: AdvancedSaveSystem
// This script demonstrates the AdvancedSaveSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script demonstrates a practical and extensible "AdvancedSaveSystem" design pattern. It focuses on modularity, data encapsulation, multiple save slots, and versioning, making it suitable for real-world Unity projects.

**Key Design Pattern Concepts Implemented:**

1.  **ISaveable Interface:** Defines a contract for any object that can be saved. This promotes loose coupling.
2.  **Data-Only Classes (DTOs):** Separate serializable classes (`PlayerData`, `WorldObjectData`, `InventoryData`) hold the raw data, distinct from the `MonoBehaviour` logic. This keeps the save data clean and easily versionable.
3.  **Centralized SaveManager (Singleton):** A single point of control for saving, loading, and managing save files. It orchestrates the process by finding `ISaveable` objects and managing the serialization/deserialization.
4.  **Generic Save File Structure (`GameSaveData`):** A root object that holds a version number and a dictionary mapping `ISaveable` unique IDs to their serialized JSON strings. This allows the system to save any `ISaveable` without the `SaveManager` needing to know its specific data type beforehand.
5.  **Multiple Save Slots:** Achieved by varying the file name based on a slot index.
6.  **Versioning:** The `GameSaveData.Version` field allows for future compatibility if save data structures change.

---

**`AdvancedSaveSystem.cs`**

To use this:
1.  Create a new C# script named `AdvancedSaveSystem` in your Unity project.
2.  Copy and paste the entire code below into it.
3.  Follow the "How to use this AdvancedSaveSystem in Unity" instructions at the bottom of the script.

```csharp
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Required for OfType<ISaveable>() and ToDictionary()

// =====================================================================================
// 1. ISaveable Interface
//    Any MonoBehaviour that needs its state saved and loaded must implement this.
// =====================================================================================
public interface ISaveable
{
    // A unique identifier for this object in the scene/world.
    // This ID is crucial for the SaveManager to match saved data with runtime objects.
    // Ensure it's stable and unique across all ISaveable instances.
    string GetUniqueId();

    // Returns an object containing all data that needs to be saved for this entity.
    // The SaveManager will serialize this object into a JSON string.
    object Save();

    // Receives the raw JSON string representing this entity's saved data.
    // The ISaveable implementation is responsible for deserializing it into its
    // specific data type and applying the loaded values.
    void Load(string jsonString);
}

// =====================================================================================
// 2. Example Save Data Structures (Data Transfer Objects - DTOs)
//    These are simple C# classes/structs that hold the actual data for each ISaveable.
//    They must be marked [Serializable] for Unity's JsonUtility to work.
//    Avoid putting logic here; they are purely for data.
// =====================================================================================

[Serializable]
public class PlayerData
{
    public float Health;
    public Vector3 Position;
    public int Score;
    public List<string> EquippedItems; // Example: List of item IDs/names
}

[Serializable]
public class WorldObjectData
{
    public Vector3 Position;
    public bool IsActive; // e.g., if it's a collected item, it might become inactive
    public string ObjectType; // Useful for spawning if the object doesn't exist at load time
}

// --- Auxiliary struct for InventoryData's Dictionary serialization workaround ---
// JsonUtility does not directly serialize Dictionary<TKey, TValue>.
// This struct provides a workaround by converting dictionary entries into a serializable list.
[Serializable]
public struct ItemCountEntry
{
    public string Key;
    public int Value;
}

[Serializable]
public class InventoryData
{
    public List<string> ItemIds; // Example: A simple list of collected item IDs
    public List<ItemCountEntry> SerializableItemCounts; // Stores dictionary data in a JsonUtility-friendly format
}


// =====================================================================================
// 3. The Root Game Save Data Structure
//    This class encapsulates all the saved data for a single game save slot.
// =====================================================================================
[Serializable]
public class GameSaveData
{
    public int Version = 1; // Allows for future data structure changes and migration logic

    // A dictionary where the key is the ISaveable's unique ID and the value
    // is the JSON string representation of its specific SaveData object (e.g., PlayerData JSON).
    // This makes the system flexible, as SaveManager doesn't need to know concrete SaveData types.
    public Dictionary<string, string> SaveableData = new Dictionary<string, string>();

    // Optional: Add global game state data directly here if it doesn't belong to an ISaveable.
    // public DateTime LastSaveTime;
    // public string CurrentSceneName;
}


// =====================================================================================
// 4. The SaveManager (Singleton MonoBehaviour)
//    This is the core of the AdvancedSaveSystem. It handles saving, loading,
//    and deleting save files, orchestrating the interaction with ISaveable objects.
// =====================================================================================
public class SaveManager : MonoBehaviour
{
    // Singleton pattern for easy, global access
    public static SaveManager Instance { get; private set; }

    // --- Configuration ---
    [Tooltip("The base file name for save slots. e.g., 'save_slot_0.json', 'save_slot_1.json'")]
    public string saveFileNameBase = "game_save_slot_";
    [Tooltip("The file extension for save files.")]
    public string saveFileExtension = ".json";
    [Tooltip("Enable detailed logging for save/load operations.")]
    public bool enableDetailedLogging = true;

    // The full path to the directory where save files will be stored.
    // Application.persistentDataPath is recommended for platform-independent persistence.
    private string SaveDirectoryPath => Application.persistentDataPath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one to enforce singleton.
            Destroy(gameObject);
        }
        else
        {
            // Set this as the singleton instance and prevent its destruction across scenes.
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Log("SaveManager initialized.");
            Log($"Save Directory: {SaveDirectoryPath}");
        }
    }

    /// <summary>
    /// Saves the current game state to a specified slot.
    /// Finds all ISaveable objects in the scene, retrieves their data, and writes to a file.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot (e.g., 0 for slot 0, 1 for slot 1).</param>
    [ContextMenu("Save Game Slot 0")] // Editor convenience for testing
    public void SaveGameSlot0() => SaveGame(0);
    [ContextMenu("Save Game Slot 1")]
    public void SaveGameSlot1() => SaveGame(1);

    public void SaveGame(int slotIndex)
    {
        Log($"Attempting to save game to slot {slotIndex}...");

        GameSaveData gameSaveData = new GameSaveData();
        gameSaveData.Version = 1; // Always set to current version when saving.

        // Find all ISaveable objects in the current scene.
        // For very large scenes or frequent saves, consider a registration system
        // (where ISaveables register themselves on OnEnable/OnDisable) to avoid
        // the performance cost of FindObjectsOfType.
        ISaveable[] saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        Log($"Found {saveables.Length} ISaveable objects to process for saving.");

        foreach (ISaveable saveable in saveables)
        {
            string uniqueId = saveable.GetUniqueId();
            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogWarning($"ISaveable object {saveable.GetType().Name} on GameObject " +
                                 $"{(saveable as MonoBehaviour)?.gameObject.name} has an empty or null Unique ID. Skipping this object's save.");
                continue;
            }

            try
            {
                object dataToSave = saveable.Save();
                if (dataToSave == null)
                {
                    Debug.LogWarning($"ISaveable '{uniqueId}' returned null data. Skipping save for this object.");
                    continue;
                }

                // Serialize the specific save data object into a JSON string.
                // 'true' for pretty print makes the JSON file human-readable.
                string jsonData = JsonUtility.ToJson(dataToSave, true);
                gameSaveData.SaveableData[uniqueId] = jsonData;
                LogDetail($"Saved data for '{uniqueId}'. Type: {dataToSave.GetType().Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save data for ISaveable '{uniqueId}': {e.Message}\n{e.StackTrace}");
            }
        }

        string filePath = GetSaveFilePath(slotIndex);
        string jsonToSave = JsonUtility.ToJson(gameSaveData, true); // Serialize the root save data

        try
        {
            Directory.CreateDirectory(SaveDirectoryPath); // Ensure the save directory exists
            File.WriteAllText(filePath, jsonToSave);
            Log($"Game saved successfully to {filePath}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write save file to {filePath}: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Loads the game state from a specified slot.
    /// Reads data from a file, deserializes it, and applies it to ISaveable objects in the scene.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    [ContextMenu("Load Game Slot 0")] // Editor convenience
    public void LoadGameSlot0() => LoadGame(0);
    [ContextMenu("Load Game Slot 1")]
    public void LoadGameSlot1() => LoadGame(1);

    public void LoadGame(int slotIndex)
    {
        Log($"Attempting to load game from slot {slotIndex}...");

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file found at {filePath} for slot {slotIndex}. Cannot load.");
            return;
        }

        try
        {
            string jsonFromFile = File.ReadAllText(filePath);
            GameSaveData loadedGameSaveData = JsonUtility.FromJson<GameSaveData>(jsonFromFile);

            // --- Versioning Logic ---
            // If your save data structures change in future game updates, this is where you'd
            // implement migration logic to convert older save data formats to the current one.
            if (loadedGameSaveData.Version < 1) // Example: if we ever introduce version 2+
            {
                Debug.LogWarning($"Loading an old save file (Version {loadedGameSaveData.Version}). " +
                                 "Consider implementing data migration logic for older versions.");
                // Example: loadedGameSaveData = MigrateSaveDataToCurrentVersion(loadedGameSaveData);
            }

            ISaveable[] saveablesInScene = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
            Log($"Found {saveablesInScene.Length} ISaveable objects in scene for loading.");

            // Create a temporary map for quick lookup of ISaveable objects by their unique ID.
            Dictionary<string, ISaveable> saveableMap = saveablesInScene.ToDictionary(s => s.GetUniqueId(), s => s);

            foreach (var entry in loadedGameSaveData.SaveableData)
            {
                string uniqueId = entry.Key;
                string jsonSaveData = entry.Value;

                if (saveableMap.TryGetValue(uniqueId, out ISaveable saveable))
                {
                    try
                    {
                        saveable.Load(jsonSaveData);
                        LogDetail($"Loaded data for '{uniqueId}'.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load data for ISaveable '{uniqueId}': {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    // This scenario means an object existed in the save file but is not in the current scene.
                    // Depending on game design, you might want to:
                    // 1. Log a warning (default behavior).
                    // 2. Dynamically spawn the object (requires knowing its prefab/type from the saved data).
                    // 3. Mark the saved data as 'unused' or 'destroyed' for items that should not respawn.
                    Debug.LogWarning($"No ISaveable object with Unique ID '{uniqueId}' found in the scene to load data onto. " +
                                     "This object might have been removed, destroyed, or needs to be spawned dynamically.");
                    // Example for dynamic spawning (requires more setup in WorldObjectData to include a prefab ID):
                    // if (jsonSaveData contains type/prefab information) { SpawnAndLoadObject(jsonSaveData); }
                }
            }

            Log($"Game loaded successfully from {filePath}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from {filePath}: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Deletes the save file for a given slot.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot to delete.</param>
    [ContextMenu("Delete Game Slot 0")] // Editor convenience
    public void DeleteGameSlot0() => DeleteSave(0);
    [ContextMenu("Delete Game Slot 1")]
    public void DeleteGameSlot1() => DeleteSave(1);

    public void DeleteSave(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Log($"Save file for slot {slotIndex} deleted successfully from {filePath}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file {filePath}: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Log($"No save file found at {filePath} for slot {slotIndex} to delete.");
        }
    }

    /// <summary>
    /// Checks if a save file exists for a given slot.
    /// </summary>
    /// <param name="slotIndex">The index of the save slot.</param>
    /// <returns>True if a save file exists, false otherwise.</returns>
    public bool DoesSaveExist(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    /// <summary>
    /// Helper method to construct the full file path for a save slot.
    /// </summary>
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(SaveDirectoryPath, $"{saveFileNameBase}{slotIndex}{saveFileExtension}");
    }

    /// <summary>
    /// Logs a message to the Unity console with a SaveManager prefix.
    /// </summary>
    private void Log(string message)
    {
        Debug.Log($"[SaveManager] {message}");
    }

    /// <summary>
    /// Logs a detailed message, only if enableDetailedLogging is true.
    /// </summary>
    private void LogDetail(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[SaveManager Detail] {message}");
        }
    }
}


// =====================================================================================
// 5. Example ISaveable Components
//    These are MonoBehaviour scripts that implement ISaveable to demonstrate usage.
// =====================================================================================

/// <summary>
/// Attaching this script to your player GameObject makes it saveable.
/// </summary>
public class Player : MonoBehaviour, ISaveable
{
    // Important: This ID MUST be unique among all ISaveable objects in your game,
    // especially for objects that persist across scenes or are never destroyed.
    public string uniqueId = "Player_Main"; // A constant ID for the main player

    [Header("Player Data (Runtime)")]
    public float health = 100f;
    public int score = 0;
    public Vector3 playerPosition; // Stores position explicitly for saving/loading
    public List<string> equippedItems = new List<string> { "BasicSword", "LeatherShield" };

    void Start()
    {
        // Initialize playerPosition with the current transform position.
        // During load, transform.position will be set, so this keeps them in sync.
        playerPosition = transform.position;
    }

    void Update()
    {
        // Example: Simulate player actions changing state
        if (Input.GetKeyDown(KeyCode.H)) // Simulate taking damage
        {
            health = Mathf.Max(0, health - 10);
            Debug.Log($"Player takes damage. Health: {health}");
        }
        if (Input.GetKeyDown(KeyCode.S)) // Simulate scoring points
        {
            score += 10;
            Debug.Log($"Player scores. Score: {score}");
        }
        if (Input.GetKeyDown(KeyCode.M)) // Simulate movement
        {
            transform.position += new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
            Debug.Log($"Player moved to: {transform.position}");
        }
        // Always update playerPosition from transform for saving accuracy
        playerPosition = transform.position;
    }

    public string GetUniqueId() => uniqueId;

    public object Save()
    {
        // Package current runtime state into a PlayerData object for serialization.
        return new PlayerData
        {
            Health = health,
            Position = playerPosition,
            Score = score,
            EquippedItems = new List<string>(equippedItems) // Create new list to avoid reference issues
        };
    }

    public void Load(string jsonString)
    {
        // Deserialize the JSON string back into PlayerData and apply to this object's runtime properties.
        PlayerData data = JsonUtility.FromJson<PlayerData>(jsonString);
        health = data.Health;
        playerPosition = data.Position;
        transform.position = data.Position; // Apply the loaded position to the GameObject's transform
        score = data.Score;
        equippedItems = new List<string>(data.EquippedItems); // Create new list from loaded data
        Debug.Log($"Player '{uniqueId}' loaded: Health={health}, Score={score}, Position={playerPosition}");
    }
}

/// <summary>
/// Attaching this script to a world object (e.g., a collectible, a door, a lever) makes it saveable.
/// Uses a GUID for unique IDs to handle multiple instances robustly.
/// </summary>
public class WorldObject : MonoBehaviour, ISaveable
{
    // For WorldObjects that are unique instances or dynamically spawned, a GUID is a robust unique ID.
    // We generate it only once if it's empty to ensure stability for scene objects.
    public string uniqueId = ""; // Leave empty in Inspector to auto-generate

    [Header("World Object Data (Runtime)")]
    public string objectType = "DefaultObject"; // Useful for identifying what type of object this is
    public bool isActiveState = true;

    void OnValidate()
    {
        // Ensure uniqueId is assigned in the editor if it's empty.
        // This is important for objects placed directly in the scene to get a stable ID.
        if (string.IsNullOrEmpty(uniqueId) || uniqueId == "00000000-0000-0000-0000-000000000000") // Check for empty GUID too
        {
            uniqueId = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this); // Mark as dirty so the change gets saved to the scene file
        }
    }

    void OnMouseDown() // Example: Simulate interaction changing state
    {
        isActiveState = !isActiveState;
        gameObject.SetActive(isActiveState);
        Debug.Log($"WorldObject '{uniqueId}' changed state. Active: {isActiveState}");
    }

    public string GetUniqueId() => uniqueId;

    public object Save()
    {
        return new WorldObjectData
        {
            Position = transform.position,
            IsActive = isActiveState,
            ObjectType = objectType
        };
    }

    public void Load(string jsonString)
    {
        WorldObjectData data = JsonUtility.FromJson<WorldObjectData>(jsonString);
        transform.position = data.Position;
        isActiveState = data.IsActive;
        gameObject.SetActive(data.IsActive); // Apply active state
        objectType = data.ObjectType; // Re-assign object type if needed
        Debug.Log($"WorldObject '{uniqueId}' loaded: Active={isActiveState}, Position={transform.position}");
    }
}

/// <summary>
/// A simple inventory example demonstrating another ISaveable type,
/// including a workaround for Dictionary serialization with JsonUtility.
/// </summary>
public class PlayerInventory : MonoBehaviour, ISaveable
{
    public string uniqueId = "Player_Inventory"; // Unique ID for the player's inventory

    [Header("Inventory Data (Runtime)")]
    public List<string> itemIds = new List<string>(); // Simple list for items
    public Dictionary<string, int> itemCounts = new Dictionary<string, int>(); // Runtime dictionary for counts

    void Start()
    {
        // Example initial inventory if it's empty
        if (itemIds.Count == 0 && itemCounts.Count == 0)
        {
            itemIds.Add("Potion");
            itemIds.Add("Key");
            itemCounts["Potion"] = 2;
            itemCounts["Key"] = 1;
            itemCounts["Gold"] = 50;
        }
    }

    void Update()
    {
        // Example: Add item on key press
        if (Input.GetKeyDown(KeyCode.A))
        {
            string newItem = "GoldCoin";
            itemIds.Add(newItem); // Add to the list
            itemCounts.TryGetValue(newItem, out int currentCount);
            itemCounts[newItem] = currentCount + 1; // Update count in dictionary
            Debug.Log($"Added {newItem}. Inventory now has {itemCounts[newItem]} {newItem}(s). Total items: {itemIds.Count}");
        }
        // Example: Use/Remove an item on key press
        if (Input.GetKeyDown(KeyCode.R))
        {
            string removeItem = "Potion";
            if (itemCounts.ContainsKey(removeItem) && itemCounts[removeItem] > 0)
            {
                itemCounts[removeItem]--;
                itemIds.Remove(removeItem); // Remove one instance from the list
                Debug.Log($"Used {removeItem}. Inventory now has {itemCounts[removeItem]} {removeItem}(s). Total items: {itemIds.Count}");
            }
            else
            {
                Debug.Log($"No {removeItem} to use.");
            }
        }
    }

    public string GetUniqueId() => uniqueId;

    public object Save()
    {
        // Convert the runtime dictionary into a serializable list of ItemCountEntry objects
        // for JsonUtility compatibility.
        List<ItemCountEntry> countsList = new List<ItemCountEntry>();
        foreach (var pair in itemCounts)
        {
            countsList.Add(new ItemCountEntry { Key = pair.Key, Value = pair.Value });
        }

        return new InventoryData
        {
            ItemIds = new List<string>(itemIds), // Deep copy the list
            SerializableItemCounts = countsList
        };
    }

    public void Load(string jsonString)
    {
        InventoryData data = JsonUtility.FromJson<InventoryData>(jsonString);
        itemIds = new List<string>(data.ItemIds); // Deep copy the list

        // Rebuild the runtime dictionary from the loaded serializable list
        itemCounts.Clear();
        foreach (var entry in data.SerializableItemCounts)
        {
            itemCounts[entry.Key] = entry.Value;
        }

        Debug.Log($"Player Inventory '{uniqueId}' loaded. Items in list: {string.Join(", ", itemIds)}. Counts: {string.Join(", ", itemCounts.Select(kv => $"{kv.Key}: {kv.Value}"))}");
    }
}


/*
=====================================================================================
--- How to use this AdvancedSaveSystem in Unity ---
=====================================================================================

1.  **Create SaveManager GameObject:**
    *   Create an empty GameObject in your very first scene (e.g., "Managers").
    *   Attach the `SaveManager.cs` script to it.
    *   The `SaveManager` GameObject will automatically persist across scenes due to `DontDestroyOnLoad`.
    *   You can customize `saveFileNameBase`, `saveFileExtension`, and `enableDetailedLogging` in the Inspector.

2.  **Make Your Game Objects Saveable:**
    *   **Player:** Create a 3D object (e.g., a Cube) and name it "Player". Attach the `Player.cs` script to it.
        *   Ensure its `uniqueId` is set (e.g., "Player_Main").
    *   **World Objects:** Create a few other 3D objects (e.g., Spheres, Cylinders). Attach the `WorldObject.cs` script to each.
        *   Their `uniqueId` will automatically generate a GUID in `OnValidate()` if left empty. Move and click on them in play mode to change their state.
    *   **Player Inventory:** Create an empty GameObject as a child of your Player (e.g., "InventoryManager"). Attach the `PlayerInventory.cs` script to it.
        *   Ensure its `uniqueId` is set (e.g., "Player_Inventory").

3.  **Trigger Save/Load Operations:**
    *   For testing, you can use the `[ContextMenu]` attributes directly on the `SaveManager` component in the Inspector to Save, Load, or Delete slots 0 and 1.
    *   For in-game use, create a separate script (e.g., `GameUIHandler.cs`) and attach it to a UI canvas or an input manager.

    **Example `GameUIHandler.cs` script:**
    ```csharp
    using UnityEngine;
    using UnityEngine.UI; // If using UI Buttons

    public class GameUIHandler : MonoBehaviour
    {
        public int currentSaveSlot = 0; // Can be linked to UI dropdown/buttons

        // Optional: UI Buttons for save/load
        public Button saveButton;
        public Button loadButton;
        public Button deleteButton;

        void Start()
        {
            // Optional: Link UI buttons if assigned
            saveButton?.onClick.AddListener(() => SaveGame());
            loadButton?.onClick.AddListener(() => LoadGame());
            deleteButton?.onClick.AddListener(() => DeleteSave());
        }

        void Update()
        {
            // Example keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
            if (Input.GetKeyDown(KeyCode.F10)) // Use with caution!
            {
                DeleteSave();
            }
        }

        public void SaveGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame(currentSaveSlot);
            }
            else
            {
                Debug.LogError("SaveManager not found!");
            }
        }

        public void LoadGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame(currentSaveSlot);
            }
            else
            {
                Debug.LogError("SaveManager not found!");
            }
        }

        public void DeleteSave()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSave(currentSaveSlot);
            }
            else
            {
                Debug.LogError("SaveManager not found!");
            }
        }
    }
    ```

4.  **Testing the System:**
    *   Run your game in the Unity Editor.
    *   Interact with the Player (move with M, take damage with H, score with S) and World Objects (click them to toggle active state). Add items to inventory (A, R).
    *   Press F5 (or use your UI/Inspector context menu) to save the game.
    *   Stop the game, then change some object states manually in the Editor, or restart the Editor, or simply restart Play Mode.
    *   Run the game again.
    *   Press F9 (or use your UI/Inspector context menu) to load the game.
    *   Observe that the Player's position, health, score, equipped items, World Objects' positions and active states, and Player Inventory items are restored to their saved state.
    *   You can find the generated JSON save files in `Application.persistentDataPath` (e.g., on Windows: `C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\`).

---
**Advanced Considerations & Extensions:**

*   **Performance Optimization for `FindObjectsOfType`:** For games with hundreds or thousands of `ISaveable` objects, `FindObjectsOfType` can be a performance bottleneck during save/load.
    *   **Solution:** Implement a registration system where `ISaveable` objects register themselves with the `SaveManager` in `OnEnable()` and unregister in `OnDisable()`. The `SaveManager` would then iterate its internal `List<ISaveable>` instead of searching the entire scene.
*   **Dynamic Object Management (Spawning/Destroying):**
    *   If objects can be created or destroyed during gameplay (e.g., enemies, collected items), the `SaveManager.LoadGame` method needs to handle data for objects not currently in the scene. This typically involves:
        1.  Adding a `prefabId` or `type` field to your `SaveData` classes (e.g., `WorldObjectData`).
        2.  During loading, if an `ISaveable` with a specific ID is *not* found in the scene, the `SaveManager` can use the `prefabId` from the loaded data to instantiate the correct prefab.
        3.  Conversely, objects that exist in the scene but *not* in the save data might need to be destroyed (e.g., an enemy that was killed).
*   **Scene Loading Integration:** For multi-scene games, you'll need to decide when to load scenes relative to `LoadGame()`. You might save the `CurrentSceneName` in `GameSaveData` and load that scene before applying data.
*   **Serialization Library:** While `JsonUtility` is convenient, it has limitations (e.g., poor Dictionary support, no private field serialization without `[SerializeField]`, no polymorphism without custom converters). For very complex data structures, consider using a more powerful library like **Newtonsoft.Json** (JSON.NET), which offers greater flexibility.
*   **Asynchronous Operations:** For very large save files, consider performing file I/O and JSON serialization/deserialization on a separate thread to prevent frame rate drops.
*   **Error Handling & User Feedback:** Provide robust error handling and clear user feedback (e.g., "Game Saved!", "Load Failed: File Missing!") through UI elements.
*   **Encryption and Compression:** For sensitive save data or to reduce file size, you can add encryption and/or compression layers before writing the JSON string to disk.
*   **Version Migration Logic:** The `GameSaveData.Version` field is crucial. When you update your game and change data structures (e.g., add new fields to `PlayerData`), you'll need to write custom migration code within `LoadGame()` (or a separate migration function) to gracefully handle loading older save file versions.
*/
```