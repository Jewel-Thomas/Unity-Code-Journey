// Unity Design Pattern Example: SaveLoadWithJSON
// This script demonstrates the SaveLoadWithJSON pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **SaveLoadWithJSON** design pattern in Unity. It provides a robust, easy-to-use system for saving and loading various types of game data using Unity's built-in `JsonUtility`.

The pattern works by:
1.  **Defining a `[System.Serializable]` data class (`GameData`)**: This class holds all the information you want to save. Unity's `JsonUtility` can only serialize fields/properties of types that are either primitive, Unity-specific (`Vector3`, `Quaternion`, `Color`, etc.), or custom classes/structs marked with `[System.Serializable]`.
2.  **Creating a `SaveLoadManager` (Singleton)**: This manager handles the logic for converting the `GameData` object to a JSON string, writing it to a file, reading it back, and converting the JSON string back into a `GameData` object. It uses `Application.persistentDataPath` to ensure cross-platform compatibility for save files.

---

### How to Use This Script in Your Unity Project:

1.  **Create a C# Script**: In your Unity project, create a new C# script named `SaveLoadManager.cs`.
2.  **Copy and Paste**: Copy the entire code below and paste it into your `SaveLoadManager.cs` file, replacing its default content.
3.  **Create an Empty GameObject**: In your scene, create an empty GameObject (e.g., named `_GameManager` or `_SaveLoad`).
4.  **Attach Script**: Drag and drop the `SaveLoadManager.cs` script onto this new GameObject.
5.  **Run**: When you run your game, the `SaveLoadManager` will initialize. It will try to load existing data or create new default data.
6.  **Call Save/Load Methods**: From any other script in your game, you can access the `SaveLoadManager` instance and call its methods to save, load, or modify your game data. Refer to the "Example Usage" section at the end of the script for details.

---

```csharp
using UnityEngine;
using System.IO; // Required for File operations (File.WriteAllText, File.ReadAllText, File.Exists, File.Delete)
using System.Collections.Generic; // Required for List<T>
using System; // Required for System.DateTime

// ====================================================================================================
// SECTION 1: GameData Classes
// These classes define the structure of the data that will be saved and loaded.
// They must be marked with [System.Serializable] for JsonUtility to be able to convert them
// to/from JSON. All fields you want to save must be public.
// ====================================================================================================

/// <summary>
/// Represents a single item in the player's inventory.
/// Marked as Serializable to be included in the GameData class and converted to JSON.
/// </summary>
[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public int quantity;
    public string iconPath; // Example: path to a sprite resource for the item's icon

    public InventoryItem(string name, int qty, string path = "")
    {
        itemName = name;
        quantity = qty;
        iconPath = path;
    }
}

/// <summary>
/// Represents the progress of a single quest.
/// Marked as Serializable to be included in the GameData class and converted to JSON.
/// </summary>
[System.Serializable]
public class QuestProgress
{
    public int questID;
    public string questName;
    public bool isCompleted;
    public int currentObjectiveIndex; // For multi-step quests, tracks current step

    public QuestProgress(int id, string name, bool completed = false, int objIndex = 0)
    {
        questID = id;
        questName = name;
        isCompleted = completed;
        currentObjectiveIndex = objIndex;
    }
}

/// <summary>
/// The main container class for all game data that needs to be saved.
/// This is the 'Save Data' object. All fields must be public.
/// Note: JsonUtility has limitations: it cannot serialize Dictionaries directly.
/// If you need dictionaries, you typically convert them to a List of custom [System.Serializable]
/// KeyValuePair-like objects before serialization. For simplicity, this example avoids dictionaries.
/// </summary>
[System.Serializable]
public class GameData
{
    // --- Player Data ---
    public int playerHealth = 100;
    public Vector3 playerPosition = Vector3.zero; // Unity struct, natively supported
    public Quaternion playerRotation = Quaternion.identity; // Unity struct, natively supported
    public string playerName = "Adventurer";
    public int playerLevel = 1;
    public float experiencePoints = 0f;

    // --- Inventory Data (using a custom serializable class list) ---
    public List<InventoryItem> inventory = new List<InventoryItem>();

    // --- Game Settings ---
    public float masterVolume = 0.75f;
    public bool invertedYAxis = false;
    public string graphicsQuality = "High";

    // --- Quest Progress (using a custom serializable class list) ---
    public List<QuestProgress> activeQuests = new List<QuestProgress>();
    public List<int> completedQuestIDs = new List<int>(); // Simple list for completed quest IDs

    // --- World State ---
    public List<string> openedDoors = new List<string>(); // List of unique door IDs that have been opened

    // --- Game Metadata ---
    public System.DateTime lastSaveTime; // Stores the last time the game was saved
    public string gameVersion; // Useful for managing save data across different game versions

    /// <summary>
    /// Constructor for GameData. Initializes default values for a new game.
    /// </summary>
    public GameData()
    {
        // Set up initial player state
        playerHealth = 100;
        playerPosition = new Vector3(0, 1, 0); // Start at a specific position
        playerRotation = Quaternion.identity;
        playerLevel = 1;
        experiencePoints = 0f;
        playerName = "New Hero";

        // Set up initial inventory
        inventory.Add(new InventoryItem("Basic Sword", 1, "Icons/SwordIcon"));
        inventory.Add(new InventoryItem("Health Potion", 3, "Icons/PotionIcon"));

        // Set up initial quests
        activeQuests.Add(new QuestProgress(1, "The First Step", false, 0));
        activeQuests.Add(new QuestProgress(2, "Gather Resources", false, 0));

        // Set up initial game settings
        masterVolume = 0.75f;
        invertedYAxis = false;
        graphicsQuality = "High";

        // Set initial metadata
        lastSaveTime = System.DateTime.Now;
        gameVersion = Application.version; // Use Unity's reported application version
    }

    /// <summary>
    /// Updates dynamic data that should be fresh before saving.
    /// In a real game, a GameManager or PlayerController would provide these values.
    /// </summary>
    public void UpdateDynamicData(Vector3 currentPos, Quaternion currentRot, int currentHealth, float currentXP, List<InventoryItem> currentInventory, List<QuestProgress> currentActiveQuests)
    {
        playerPosition = currentPos;
        playerRotation = currentRot;
        playerHealth = currentHealth;
        experiencePoints = currentXP;
        inventory = new List<InventoryItem>(currentInventory); // Create a new list to avoid reference issues
        activeQuests = new List<QuestProgress>(currentActiveQuests); // Copy current active quests

        lastSaveTime = System.DateTime.Now;
        gameVersion = Application.version; // Ensure game version is up-to-date in save
    }
}

// ====================================================================================================
// SECTION 2: SaveLoadManager
// This MonoBehaviour acts as the central point for saving, loading, and managing game data.
// It implements the Singleton pattern for easy access from anywhere in the game.
// ====================================================================================================

/// <summary>
/// Manages saving and loading game data to/from a JSON file.
/// This class uses the Singleton pattern to ensure only one instance exists throughout the game.
/// It utilizes Unity's JsonUtility for serialization and deserialization.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    // Singleton instance for easy access from other scripts
    public static SaveLoadManager Instance { get; private set; }

    [Header("Save File Settings")]
    [Tooltip("The name of the save file (e.g., 'gamesave.json').")]
    public string saveFileName = "gamesave.json";

    // The full path where the save file will be stored
    private string saveFilePath;

    // The current game data loaded into memory. Other scripts modify this object.
    public GameData currentSaveData { get; private set; }

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton pattern and determines the save file path.
    /// </summary>
    void Awake()
    {
        // Implement the singleton pattern
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one
            Destroy(gameObject);
            return;
        }
        Instance = this; // Set this instance as the singleton
        DontDestroyOnLoad(gameObject); // Keep this GameObject alive across scene loads

        // Determine the save file path
        // Application.persistentDataPath provides a reliable, cross-platform directory
        // for storing persistent data (e.g., on Windows: AppData/LocalLow/[Company]/[Product]/)
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

        Debug.Log($"SaveLoadManager initialized. Save file path: {saveFilePath}");

        // Attempt to load existing game data, or create a new one if no save is found.
        LoadGame(true);
    }

    // --- Public Save/Load/Delete Methods ---

    /// <summary>
    /// Saves the current game data (held in `currentSaveData`) to a JSON file.
    /// </summary>
    public void SaveGame()
    {
        if (currentSaveData == null)
        {
            Debug.LogError("SaveLoadManager: No GameData instance to save! Call LoadGame(true) or GetOrCreateNewSaveData() first.");
            return;
        }

        try
        {
            // Convert the GameData object to a JSON string.
            // 'prettyPrint: true' makes the JSON file human-readable, which is great for debugging.
            // For release builds, you might set it to false to save disk space and slightly improve performance.
            string json = JsonUtility.ToJson(currentSaveData, true);

            // Write the JSON string to the specified file path.
            File.WriteAllText(saveFilePath, json);

            Debug.Log($"SaveLoadManager: Game saved successfully to {saveFilePath}");
        }
        catch (System.Exception e)
        {
            // Log any errors that occur during the save process.
            Debug.LogError($"SaveLoadManager: Failed to save game to {saveFilePath}. Error: {e.Message}");
        }
    }

    /// <summary>
    /// Loads game data from the JSON save file into `currentSaveData`.
    /// </summary>
    /// <param name="createNewIfNotFound">If true, a new default GameData will be created
    /// if no save file exists or if loading fails.</param>
    /// <returns>True if data was successfully loaded or created, false if an error occurred
    /// and `createNewIfNotFound` was false.</returns>
    public bool LoadGame(bool createNewIfNotFound = false)
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                // Read the entire JSON string from the file.
                string json = File.ReadAllText(saveFilePath);

                // Convert the JSON string back into a GameData object.
                // If currentSaveData is null, JsonUtility.FromJson creates a new object.
                // If it were not null, JsonUtility.FromJsonOverwrite(json, currentSaveData)
                // could update an existing object, but FromJson is generally safer for full loads.
                currentSaveData = JsonUtility.FromJson<GameData>(json);

                // It's good practice to check for null after deserialization, though FromJson
                // usually returns a new object or throws an exception on invalid JSON.
                if (currentSaveData == null)
                {
                    throw new System.Exception("Deserialized GameData is null, indicating an issue with the JSON content.");
                }

                Debug.Log($"SaveLoadManager: Game loaded successfully from {saveFilePath}. Last Save Time: {currentSaveData.lastSaveTime}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveLoadManager: Failed to load game from {saveFilePath}. Error: {e.Message}. StackTrace: {e.StackTrace}");
                // If loading fails, and we are allowed to, create a new default game.
                if (createNewIfNotFound)
                {
                    InitializeNewGameData();
                    Debug.Log("SaveLoadManager: Created new default game data due to load failure.");
                    return true;
                }
                currentSaveData = null; // Ensure no partial or corrupted data is kept in memory.
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"SaveLoadManager: No save file found at {saveFilePath}.");
            if (createNewIfNotFound)
            {
                InitializeNewGameData();
                Debug.Log("SaveLoadManager: Created new default game data.");
                return true;
            }
            currentSaveData = null; // No save file, so no data is loaded.
            return false;
        }
    }

    /// <summary>
    /// Deletes the existing save file.
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                // After deleting, reset the in-memory data to a new default state.
                currentSaveData = InitializeNewGameData();
                Debug.Log($"SaveLoadManager: Save file deleted successfully from {saveFilePath}. Current in-memory data reset.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveLoadManager: Failed to delete save file at {saveFilePath}. Error: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"SaveLoadManager: No save file found to delete at {saveFilePath}.");
        }
    }

    /// <summary>
    /// Checks if a save file currently exists on disk.
    /// </summary>
    /// <returns>True if a save file exists, false otherwise.</returns>
    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }

    /// <summary>
    /// Provides access to the current GameData instance.
    /// If no data is currently loaded (e.g., after a failed load or initial startup without a save),
    /// it will create a new default `GameData` instance to ensure `currentSaveData` is never null.
    /// This method is useful for ensuring you always have a valid `GameData` object to work with.
    /// </summary>
    /// <returns>The current or a newly created default `GameData` instance.</returns>
    public GameData GetOrCreateNewSaveData()
    {
        if (currentSaveData == null)
        {
            InitializeNewGameData();
            Debug.Log("SaveLoadManager: GetOrCreateNewSaveData created a new default GameData instance because none was loaded.");
        }
        return currentSaveData;
    }

    /// <summary>
    /// Initializes a new GameData object with default values and assigns it to `currentSaveData`.
    /// This is typically called when starting a new game or after deleting a save.
    /// </summary>
    /// <returns>The newly created GameData object.</returns>
    private GameData InitializeNewGameData()
    {
        currentSaveData = new GameData();
        return currentSaveData;
    }
}


// ====================================================================================================
// SECTION 3: Example Usage (in comments)
// This section demonstrates how other scripts in your game would interact with the SaveLoadManager.
// You would put these calls in your GameManager, PlayerController, UI Button handlers, etc.
// ====================================================================================================

/*
// Example of how to use the SaveLoadManager from another script (e.g., a GameManager, PlayerController, or UI Button callback)

public class MyGameManager : MonoBehaviour
{
    // A reference to your player GameObject (e.g., found by tag or assigned in Inspector)
    public GameObject playerGameObject;

    // A list simulating the player's current in-game inventory, which would be managed by an InventorySystem
    public List<InventoryItem> playerCurrentInventory = new List<InventoryItem>();
    // A list simulating the player's current active quests, managed by a QuestSystem
    public List<QuestProgress> playerActiveQuests = new List<QuestProgress>();


    void Start()
    {
        // On game start, ensure the SaveLoadManager has initialized.
        // It typically loads data or creates a new one in its Awake method.
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("MyGameManager: SaveLoadManager instance not found! Make sure it's in the scene.");
            return;
        }

        // Initialize example current in-game data, simulating what a real game would have.
        // In a real game, these would come from your active game state objects (Player, InventorySystem, QuestSystem).
        playerCurrentInventory.Add(new InventoryItem("Bow", 1, "Icons/BowIcon"));
        playerActiveQuests.Add(new QuestProgress(3, "Find the Lost Artifact", false, 0));

        // After loading, apply the loaded data to the game world.
        // This is crucial for reflecting the save state in the running game.
        ApplyLoadedDataToGame();
    }

    /// <summary>
    /// Updates the in-memory GameData object with the *current* state of the game
    /// before saving. This must be called before SaveGame().
    /// </summary>
    public void UpdateCurrentGameDataInMemory()
    {
        GameData data = SaveLoadManager.Instance.GetOrCreateNewSaveData();

        // Example: Update player position, rotation, health, and XP from the actual game objects
        if (playerGameObject != null)
        {
            PlayerController player = playerGameObject.GetComponent<PlayerController>(); // Assuming a PlayerController script
            if (player != null)
            {
                data.UpdateDynamicData(
                    playerGameObject.transform.position,
                    playerGameObject.transform.rotation,
                    player.currentHealth, // Get actual health from player
                    player.experiencePoints, // Get actual XP from player
                    playerCurrentInventory,  // Get current inventory from inventory system
                    playerActiveQuests       // Get current active quests from quest system
                );
            }
            else
            {
                // If no PlayerController, just update basic transform info
                data.UpdateDynamicData(
                    playerGameObject.transform.position,
                    playerGameObject.transform.rotation,
                    data.playerHealth, // Keep old health if no controller
                    data.experiencePoints, // Keep old XP if no controller
                    playerCurrentInventory,
                    playerActiveQuests
                );
            }
        }
        else
        {
            Debug.LogWarning("MyGameManager: Player GameObject not assigned, cannot update player position/rotation for save.");
            // Even if player is not found, update other data that doesn't rely on it
            data.UpdateDynamicData(
                data.playerPosition,
                data.playerRotation,
                data.playerHealth,
                data.experiencePoints,
                playerCurrentInventory,
                playerActiveQuests
            );
        }

        // Example: Update other dynamic data not tied to playerGameObject directly
        // data.masterVolume = AudioListener.volume; // If you manage volume centrally
        // data.openedDoors.Add("Door_CaveEntrance_01"); // Mark a door as opened
    }

    /// <summary>
    /// Applies the loaded data from `SaveLoadManager.Instance.currentSaveData`
    /// to the active game objects and systems in the scene.
    /// This should be called after a successful LoadGame().
    /// </summary>
    public void ApplyLoadedDataToGame()
    {
        GameData loadedData = SaveLoadManager.Instance.GetOrCreateNewSaveData();

        Debug.Log($"Applying Loaded Data: Health={loadedData.playerHealth}, Pos={loadedData.playerPosition}, Volume={loadedData.masterVolume}, LastSave={loadedData.lastSaveTime}");

        // Example: Apply player data
        if (playerGameObject != null)
        {
            playerGameObject.transform.position = loadedData.playerPosition;
            playerGameObject.transform.rotation = loadedData.playerRotation;
            PlayerController player = playerGameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetHealth(loadedData.playerHealth); // Set player's health
                player.SetExperience(loadedData.experiencePoints); // Set player's XP
            }
            // Update player name display, etc.
        }

        // Example: Apply inventory data
        playerCurrentInventory.Clear();
        foreach (InventoryItem item in loadedData.inventory)
        {
            playerCurrentInventory.Add(item); // Add items to the in-game inventory system
            Debug.Log($"Loaded Inventory Item: {item.itemName} x{item.quantity}");
        }
        // Update UI to reflect inventory changes

        // Example: Apply quest data
        playerActiveQuests.Clear();
        foreach (QuestProgress quest in loadedData.activeQuests)
        {
            playerActiveQuests.Add(quest); // Add active quests to the in-game quest system
            Debug.Log($"Loaded Active Quest: {quest.questName} (Completed: {quest.isCompleted})");
        }
        // Update UI to reflect active quests

        // Example: Apply game settings
        AudioListener.volume = loadedData.masterVolume;
        // Apply graphics quality, inverted Y-axis, etc.

        // Example: Apply world state (e.g., open specific doors)
        foreach (string doorID in loadedData.openedDoors)
        {
            Debug.Log($"Loaded Opened Door ID: {doorID}");
            // Find door GameObject by ID and set it to open state
            // DoorManager.Instance.OpenDoor(doorID);
        }
    }


    // --- UI Button Callbacks (or similar event-driven calls) ---

    public void OnClickSaveButton()
    {
        Debug.Log("Save button clicked!");
        UpdateCurrentGameDataInMemory(); // Ensure in-memory data is up-to-date
        SaveLoadManager.Instance.SaveGame();
    }

    public void OnClickLoadButton()
    {
        Debug.Log("Load button clicked!");
        bool success = SaveLoadManager.Instance.LoadGame();
        if (success)
        {
            ApplyLoadedDataToGame(); // Apply the loaded data to the game world
            Debug.Log("Game successfully loaded and applied to world.");
        }
        else
        {
            Debug.LogWarning("Failed to load game. Current game state remains (or new default data created if specified).");
        }
    }

    public void OnClickNewGameButton()
    {
        Debug.Log("New Game button clicked!");
        SaveLoadManager.Instance.DeleteSave(); // Optionally delete old save
        SaveLoadManager.Instance.GetOrCreateNewSaveData(); // This will create a new default GameData
        ApplyLoadedDataToGame(); // Apply the new default data to the game world
        Debug.Log("Started a new game with default data.");
    }

    public void OnClickDeleteSaveButton()
    {
        Debug.Log("Delete Save button clicked!");
        SaveLoadManager.Instance.DeleteSave();
        // After deleting, you might want to force a "New Game" state,
        // which InitializeNewGameData() does internally when called by DeleteSave().
        ApplyLoadedDataToGame(); // Apply the newly reset default data to the game world.
    }

    public void OnCheckForSaveButton()
    {
        if (SaveLoadManager.Instance.HasSaveFile())
        {
            Debug.Log("A save file exists.");
        }
        else
        {
            Debug.Log("No save file found.");
        }
    }

    // --- Example of modifying data directly ---
    public void IncreasePlayerHealth(int amount)
    {
        GameData data = SaveLoadManager.Instance.GetOrCreateNewSaveData();
        data.playerHealth += amount;
        Debug.Log($"Player health increased to: {data.playerHealth}");
    }

    public void ChangePlayerName(string newName)
    {
        GameData data = SaveLoadManager.Instance.GetOrCreateNewSaveData();
        data.playerName = newName;
        Debug.Log($"Player name changed to: {data.playerName}");
    }

    public void AddItem(string itemName, int quantity = 1)
    {
        GameData data = SaveLoadManager.Instance.GetOrCreateNewSaveData();
        InventoryItem existingItem = data.inventory.Find(item => item.itemName == itemName);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            data.inventory.Add(new InventoryItem(itemName, quantity));
        }
        Debug.Log($"Added {quantity}x {itemName}. Inventory count: {data.inventory.Count}");
    }
}

// You might have a simple PlayerController to simulate player state
public class PlayerController : MonoBehaviour
{
    public int currentHealth = 100;
    public float experiencePoints = 0f;

    public void SetHealth(int health)
    {
        currentHealth = health;
        Debug.Log($"PlayerController: Health set to {currentHealth}");
    }

    public void SetExperience(float xp)
    {
        experiencePoints = xp;
        Debug.Log($"PlayerController: XP set to {experiencePoints}");
    }

    // Simulate player movement for position updates
    void Update()
    {
        // For demonstration, let's make the player move slightly over time
        // in a real game, this would be actual player movement.
        transform.position = new Vector3(Mathf.Sin(Time.time * 0.5f) * 5, 1, Mathf.Cos(Time.time * 0.5f) * 5);
    }
}
*/
```