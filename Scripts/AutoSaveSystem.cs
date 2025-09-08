// Unity Design Pattern Example: AutoSaveSystem
// This script demonstrates the AutoSaveSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AutoSaveSystem' design pattern in Unity focuses on automatically saving crucial game data at specified intervals or events, ensuring player progress isn't lost. This example provides a robust, practical implementation using a centralized manager, a clear interface for savable objects, and Unity's `JsonUtility` for data persistence.

**Key Concepts Demonstrated:**

1.  **Singleton Pattern:** The `AutoSaveManager` is implemented as a singleton for easy global access.
2.  **`ISavable` Interface:** Defines a contract for any game object or component that wants its state to be saved and loaded.
3.  **Data Transfer Objects (DTOs):** Serializable classes (`SavableData` and its derivatives) represent the actual data to be saved, separating data from behavior.
4.  **Polymorphic Serialization with `JsonUtility`:** A common challenge with `JsonUtility` is handling derived classes. This example shows a practical way to achieve this by storing type names and JSON strings separately.
5.  **Automatic and Manual Saving:** Supports timed auto-saves, saves on application quit, and exposed methods for manual triggers.
6.  **Persistence Across Scenes:** The `AutoSaveManager` is persistent across scene loads using `DontDestroyOnLoad`.
7.  **`Application.persistentDataPath`:** Correctly uses the platform-independent path for saving files.

---

### **How to Use This Script in Your Unity Project:**

1.  **Create a C# Script:** In your Unity project, create a new C# script named `AutoSaveSystem.cs` (or any name you prefer, but the class name should match).
2.  **Copy and Paste:** Copy the entire code block below and paste it into your new `AutoSaveSystem.cs` file, replacing its default content.
3.  **Create an Empty GameObject:** In your first scene (or a "Bootstrapping" scene), create an empty GameObject and name it `AutoSaveManager`.
4.  **Attach the Script:** Drag and drop the `AutoSaveSystem.cs` script onto the `AutoSaveManager` GameObject.
5.  **Configure in Inspector:**
    *   Adjust `Auto Save Interval` (e.g., 60 seconds).
    *   Adjust `Save File Name` (e.g., "game_save.json").
6.  **Make Savable Components:**
    *   For any `MonoBehaviour` you want to save (e.g., `PlayerController`, `CollectableItem`), make it implement the `ISavable` interface.
    *   Create a corresponding serializable `SavableData` class that inherits from `SavableData` and holds the fields you want to save.
    *   Implement `GetSavableId()`, `SaveState()`, and `LoadState()` methods.
    *   **Crucially:** Ensure your `GetSavableId()` returns a *unique* string for each *instance* of a savable object that exists in the scene and needs its state preserved. For static scene objects, a hardcoded unique ID is fine. For dynamically spawned objects, you might generate a GUID on creation and store it.
7.  **Register/Unregister:** In the `Awake()` or `Start()` method of your `ISavable` components, call `AutoSaveManager.Instance.RegisterSavable(this);`. In `OnDestroy()`, call `AutoSaveManager.Instance.UnregisterSavable(this);`.
8.  **Test:** Run your game, make some changes to your savable objects, quit the editor/application, and then run it again. Your changes should be loaded. You can also manually trigger saves/loads using the public methods or test UI.

---

### **`AutoSaveSystem.cs`**

```csharp
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// ================================================================================================
// INTERFACES & BASE CLASSES FOR SAVABLE DATA
// ================================================================================================

/// <summary>
/// Interface for any MonoBehaviour that wants to be saved and loaded by the AutoSaveSystem.
/// </summary>
public interface ISavable
{
    /// <summary>
    /// Returns a unique identifier for this savable object.
    /// This ID is used to match saved data with the correct object instance upon loading.
    /// It must be unique across all savable objects in the game.
    /// </summary>
    string GetSavableId();

    /// <summary>
    /// Gathers the current state of the object into a SavableData object.
    /// </summary>
    /// <returns>A concrete SavableData object containing the object's current state.</returns>
    SavableData SaveState();

    /// <summary>
    /// Applies the loaded state from a SavableData object to this object.
    /// </summary>
    /// <param name="data">The loaded SavableData object containing the state to apply.</param>
    void LoadState(SavableData data);
}

/// <summary>
/// Base abstract class for all savable data objects.
/// These objects are plain C# objects (POCOs) that hold the state of an ISavable MonoBehaviour.
/// They must be marked [System.Serializable] for JsonUtility to work.
/// </summary>
[System.Serializable]
public abstract class SavableData
{
    // The unique ID of the ISavable object that this data belongs to.
    public string savableId;

    protected SavableData(string id)
    {
        savableId = id;
    }
}

/// <summary>
/// A wrapper class used by the AutoSaveManager to serialize and deserialize individual
/// SavableData objects, allowing for polymorphic saving with JsonUtility.
/// Each SerializedSavableData stores the type name of the original SavableData
/// and its JSON string representation.
/// </summary>
[System.Serializable]
public class SerializedSavableData
{
    public string typeName; // The full type name of the actual SavableData object (e.g., "PlayerSavableData")
    public string jsonData; // The JSON string of the actual SavableData object

    public SerializedSavableData(string typeName, string jsonData)
    {
        this.typeName = typeName;
        this.jsonData = jsonData;
    }
}

/// <summary>
/// The main container for all game data that will be saved to a file.
/// This single object is what gets converted to/from JSON.
/// </summary>
[System.Serializable]
public class SaveDataWrapper
{
    public List<SerializedSavableData> savableObjectsData = new List<SerializedSavableData>();
    // You could add other global game state here, e.g., current scene name, game version, etc.
    public string saveTimestamp;
}

// ================================================================================================
// AUTO SAVE MANAGER (Singleton)
// ================================================================================================

/// <summary>
/// The central manager for the AutoSaveSystem pattern.
/// It's responsible for orchestrating saving and loading of game data.
/// Implemented as a persistent singleton.
/// </summary>
public class AutoSaveManager : MonoBehaviour
{
    // Singleton Instance
    public static AutoSaveManager Instance { get; private set; }

    [Header("Save Settings")]
    [Tooltip("Time interval in seconds for automatic saving.")]
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    [Tooltip("The name of the file where game data will be saved.")]
    [SerializeField] private string saveFileName = "game_save.json";

    // Internal list of all currently registered ISavable objects in the scene.
    // Using a Dictionary for quick lookup by ID, which is crucial for loading.
    private Dictionary<string, ISavable> registeredSavables = new Dictionary<string, ISavable>();

    // Path where the save file will be stored.
    private string saveFilePath;

    private Coroutine autoSaveCoroutine;

    // ============================================================================================
    // UNITY LIFECYCLE METHODS
    // ============================================================================================

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the manager alive across scene changes
            InitializeManager();
        }
        else
        {
            Debug.LogWarning("AutoSaveManager already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Start the auto-save coroutine
        StartAutoSave();
        // Attempt to load the game state when the manager starts
        LoadGame();
    }

    private void OnApplicationQuit()
    {
        // Ensure data is saved when the application quits
        Debug.Log("Application quitting. Triggering final save.");
        SaveGame();
    }

    private void OnDestroy()
    {
        // Clean up the singleton instance if this manager is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============================================================================================
    // INITIALIZATION & CONFIGURATION
    // ============================================================================================

    /// <summary>
    /// Initializes the save file path.
    /// </summary>
    private void InitializeManager()
    {
        // Use Application.persistentDataPath for cross-platform save data storage
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log($"AutoSaveManager initialized. Save path: {saveFilePath}");
    }

    // ============================================================================================
    // SAVABLE OBJECT REGISTRATION
    // ============================================================================================

    /// <summary>
    /// Registers an ISavable object with the AutoSaveManager.
    /// This object will then be included in future save operations.
    /// </summary>
    /// <param name="savable">The ISavable object to register.</param>
    public void RegisterSavable(ISavable savable)
    {
        if (savable == null)
        {
            Debug.LogError("Attempted to register a null ISavable object.");
            return;
        }

        string id = savable.GetSavableId();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError($"ISavable object {savable.GetType().Name} has a null or empty ID and cannot be registered.");
            return;
        }

        if (registeredSavables.ContainsKey(id))
        {
            Debug.LogWarning($"ISavable object with ID '{id}' is already registered. Overwriting with new instance. " +
                             $"Ensure IDs are unique, or unregister previous instance first. Object Type: {savable.GetType().Name}");
            registeredSavables[id] = savable; // Update the reference if a new instance with the same ID registers
        }
        else
        {
            registeredSavables.Add(id, savable);
            // Debug.Log($"Registered savable: {id} ({savable.GetType().Name})");
        }
    }

    /// <summary>
    /// Unregisters an ISavable object from the AutoSaveManager.
    /// This object will no longer be included in save operations.
    /// </summary>
    /// <param name="savable">The ISavable object to unregister.</param>
    public void UnregisterSavable(ISavable savable)
    {
        if (savable == null)
        {
            // Debug.LogWarning("Attempted to unregister a null ISavable object. This can happen if the object was destroyed.");
            return;
        }

        string id = savable.GetSavableId();
        if (string.IsNullOrEmpty(id))
        {
            // Debug.LogWarning($"ISavable object {savable.GetType().Name} has a null or empty ID and cannot be unregistered correctly.");
            return;
        }

        if (registeredSavables.ContainsKey(id))
        {
            registeredSavables.Remove(id);
            // Debug.Log($"Unregistered savable: {id}");
        }
    }

    // ============================================================================================
    // SAVE LOGIC
    // ============================================================================================

    /// <summary>
    /// Collects data from all registered ISavable objects and saves it to a file.
    /// </summary>
    [ContextMenu("Save Game Manually")] // Allows direct execution from the Inspector
    public void SaveGame()
    {
        if (registeredSavables.Count == 0)
        {
            Debug.Log("No savable objects registered. Skipping save operation.");
            return;
        }

        SaveDataWrapper saveData = new SaveDataWrapper();
        saveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var entry in registeredSavables)
        {
            ISavable savable = entry.Value;
            if (savable == null || savable.GetSavableId() != entry.Key)
            {
                Debug.LogWarning($"Skipping null or invalid savable with ID '{entry.Key}'. It might have been destroyed or its ID changed.");
                continue;
            }

            try
            {
                SavableData savableObjectData = savable.SaveState();
                if (savableObjectData != null)
                {
                    // Serialize the specific SavableData object to JSON
                    string json = JsonUtility.ToJson(savableObjectData);
                    // Wrap it with its type name
                    saveData.savableObjectsData.Add(new SerializedSavableData(savableObjectData.GetType().FullName, json));
                }
                else
                {
                    Debug.LogWarning($"Savable object '{savable.GetSavableId()}' returned null data for saving. Skipping.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving state for object '{savable.GetSavableId()}': {e.Message}");
            }
        }

        string fullJson = JsonUtility.ToJson(saveData, true); // true for pretty printing
        try
        {
            File.WriteAllText(saveFilePath, fullJson);
            Debug.Log($"Game saved successfully! ({registeredSavables.Count} objects) to: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write save file to {saveFilePath}: {e.Message}");
        }
    }

    // ============================================================================================
    // LOAD LOGIC
    // ============================================================================================

    /// <summary>
    /// Loads game data from the save file and applies it to registered ISavable objects.
    /// </summary>
    [ContextMenu("Load Game Manually")] // Allows direct execution from the Inspector
    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning($"Save file not found at {saveFilePath}. Starting fresh.");
            return;
        }

        try
        {
            string fullJson = File.ReadAllText(saveFilePath);
            SaveDataWrapper saveData = JsonUtility.FromJson<SaveDataWrapper>(fullJson);

            if (saveData == null || saveData.savableObjectsData == null)
            {
                Debug.LogError("Save data is empty or corrupted. Cannot load.");
                return;
            }

            Debug.Log($"Loading game from {saveFilePath} (saved at: {saveData.saveTimestamp})... ");
            int loadedCount = 0;

            foreach (var serializedData in saveData.savableObjectsData)
            {
                if (string.IsNullOrEmpty(serializedData.typeName) || string.IsNullOrEmpty(serializedData.jsonData))
                {
                    Debug.LogWarning("Skipping invalid serialized data entry (empty type or data).");
                    continue;
                }

                try
                {
                    // Get the actual type of the SavableData using its stored typeName
                    Type dataType = Type.GetType(serializedData.typeName);
                    if (dataType == null)
                    {
                        Debug.LogError($"Could not find type '{serializedData.typeName}' during loading. Make sure the type exists and is accessible.");
                        continue;
                    }

                    // Deserialize the specific SavableData object
                    SavableData specificData = JsonUtility.FromJson(serializedData.jsonData, dataType) as SavableData;

                    if (specificData != null && registeredSavables.TryGetValue(specificData.savableId, out ISavable savableObject))
                    {
                        savableObject.LoadState(specificData);
                        loadedCount++;
                        // Debug.Log($"Loaded state for: {specificData.savableId}");
                    }
                    else
                    {
                        // This might happen if an object saved previously no longer exists in the scene
                        Debug.LogWarning($"Could not find registered ISavable object with ID '{specificData?.savableId ?? "Unknown"}' to load data for. " +
                                         $"Data type: {serializedData.typeName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error deserializing or loading data for type '{serializedData.typeName}' (ID: {serializedData.jsonData}): {e.Message}");
                }
            }
            Debug.Log($"Game loaded successfully! Applied state to {loadedCount} objects.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read or parse save file: {e.Message}");
        }
    }

    // ============================================================================================
    // AUTO-SAVE COROUTINE
    // ============================================================================================

    /// <summary>
    /// Starts the auto-save coroutine, which periodically saves the game.
    /// </summary>
    public void StartAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }
        autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
        Debug.Log($"Auto-save started with interval: {autoSaveInterval} seconds.");
    }

    /// <summary>
    /// Stops the currently running auto-save coroutine.
    /// </summary>
    public void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
            Debug.Log("Auto-save stopped.");
        }
    }

    /// <summary>
    /// Coroutine that periodically calls the SaveGame method.
    /// </summary>
    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            Debug.Log($"Auto-saving game at {DateTime.Now.ToShortTimeString()}...");
            SaveGame();
        }
    }
}


// ================================================================================================
// EXAMPLE USAGE: PLAYER COMPONENT
// ================================================================================================

/// <summary>
/// Example concrete SavableData for a player's state.
/// </summary>
[System.Serializable]
public class PlayerSavableData : SavableData
{
    public float posX, posY, posZ; // Player position
    public int health;             // Player health
    public int score;              // Player score

    public PlayerSavableData(string id, Vector3 position, int health, int score) : base(id)
    {
        posX = position.x;
        posY = position.y;
        posZ = position.z;
        this.health = health;
        this.score = score;
    }
}

/// <summary>
/// Example PlayerController that implements ISavable.
/// Attach this script to your Player GameObject.
/// </summary>
public class PlayerController : MonoBehaviour, ISavable
{
    [Header("Player Settings")]
    [Tooltip("Unique ID for this player instance. MUST be unique if multiple players.")]
    [SerializeField] private string playerSaveId = "Player_Main";
    [SerializeField] private int health = 100;
    [SerializeField] private int score = 0;
    [SerializeField] private float moveSpeed = 5f;

    public string GetSavableId() => playerSaveId;

    void Awake()
    {
        // Register this player with the AutoSaveManager
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.RegisterSavable(this);
        }
    }

    void OnDestroy()
    {
        // Unregister this player when it's destroyed
        if (AutoSaveManager.Instance != null && !gameObject.scene.isLoaded) // Only unregister if object is truly being destroyed, not just scene unload
        {
            AutoSaveManager.Instance.UnregisterSavable(this);
        }
    }

    void Update()
    {
        // Simple player movement for demonstration
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Example: Increase score or decrease health based on some input for testing
        if (Input.GetKeyDown(KeyCode.H)) { health = Mathf.Max(0, health - 10); Debug.Log($"Player Health: {health}"); }
        if (Input.GetKeyDown(KeyCode.S)) { score += 10; Debug.Log($"Player Score: {score}"); }
        if (Input.GetKeyDown(KeyCode.R)) { ResetPlayerState(); Debug.Log("Player state reset!"); }
    }

    /// <summary>
    /// Gathers player state into a PlayerSavableData object.
    /// </summary>
    public SavableData SaveState()
    {
        // Return a new PlayerSavableData instance with current values
        return new PlayerSavableData(playerSaveId, transform.position, health, score);
    }

    /// <summary>
    /// Applies loaded player state from a PlayerSavableData object.
    /// </summary>
    /// <param name="data">The loaded data, cast to PlayerSavableData.</param>
    public void LoadState(SavableData data)
    {
        // Ensure the loaded data is of the correct type
        if (data is PlayerSavableData playerData)
        {
            transform.position = new Vector3(playerData.posX, playerData.posY, playerData.posZ);
            health = playerData.health;
            score = playerData.score;
            Debug.Log($"Loaded Player State for '{playerSaveId}': Pos={transform.position}, Health={health}, Score={score}");
        }
        else
        {
            Debug.LogError($"Mismatch data type for PlayerController '{playerSaveId}'. Expected PlayerSavableData, got {data?.GetType().Name ?? "null"}.");
        }
    }

    private void ResetPlayerState()
    {
        transform.position = new Vector3(0, 0.5f, 0); // Example initial position
        health = 100;
        score = 0;
    }
}


// ================================================================================================
// EXAMPLE USAGE: SAVABLE CUBE (OBJECT IN SCENE)
// ================================================================================================

/// <summary>
/// Example concrete SavableData for a Cube's state.
/// </summary>
[System.Serializable]
public class CubeSavableData : SavableData
{
    public float posX, posY, posZ; // Position
    public float rotX, rotY, rotZ, rotW; // Rotation
    public float colorR, colorG, colorB, colorA; // Color
    public string customName; // Custom name for the cube

    public CubeSavableData(string id, Vector3 position, Quaternion rotation, Color color, string name) : base(id)
    {
        posX = position.x; posY = position.y; posZ = position.z;
        rotX = rotation.x; rotY = rotation.y; rotZ = rotation.z; rotW = rotation.w;
        colorR = color.r; colorG = color.g; colorB = color.b; colorA = color.a;
        customName = name;
    }
}

/// <summary>
/// Example SavableCube component. Attach this to a 3D Cube GameObject.
/// It will save its position, rotation, color, and a custom name.
/// </summary>
public class SavableCube : MonoBehaviour, ISavable
{
    [Header("Cube Settings")]
    [Tooltip("Unique ID for this specific cube instance. MUST be unique across all SavableCubes.")]
    [SerializeField] private string cubeSaveId;
    [SerializeField] private string customName = "Default Cube";

    private MeshRenderer meshRenderer;

    void Awake()
    {
        // Ensure a unique ID is set, or generate one if not provided (for runtime created objects)
        if (string.IsNullOrEmpty(cubeSaveId))
        {
            // For editor-placed objects, it's better to assign manually or use object name if unique.
            // For dynamically spawned objects, a GUID is a good choice.
            // Example: cubeSaveId = "Cube_" + Guid.NewGuid().ToString();
            cubeSaveId = "SavableCube_" + GetInstanceID(); // Using instance ID for simple uniqueness in scene
            Debug.LogWarning($"Cube '{gameObject.name}' had no save ID. Assigned '{cubeSaveId}'. Consider setting unique IDs in the inspector for static objects.");
        }

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("SavableCube requires a MeshRenderer component.", this);
            enabled = false;
            return;
        }

        // Register this cube with the AutoSaveManager
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.RegisterSavable(this);
        }
    }

    void OnDestroy()
    {
        // Unregister this cube when it's destroyed
        if (AutoSaveManager.Instance != null && !gameObject.scene.isLoaded)
        {
            AutoSaveManager.Instance.UnregisterSavable(this);
        }
    }

    void Update()
    {
        // Example: Rotate the cube and change its color over time for visual feedback/testing
        transform.Rotate(Vector3.up * 10f * Time.deltaTime);
        // Change color randomly on mouse click for testing
        if (Input.GetMouseButtonDown(0))
        {
            meshRenderer.material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            Debug.Log($"Cube '{cubeSaveId}' color changed to: {meshRenderer.material.color}");
        }
    }

    public string GetSavableId() => cubeSaveId;

    public SavableData SaveState()
    {
        // Return a new CubeSavableData instance with current values
        return new CubeSavableData(cubeSaveId, transform.position, transform.rotation, meshRenderer.material.color, customName);
    }

    public void LoadState(SavableData data)
    {
        // Ensure the loaded data is of the correct type
        if (data is CubeSavableData cubeData)
        {
            transform.position = new Vector3(cubeData.posX, cubeData.posY, cubeData.posZ);
            transform.rotation = new Quaternion(cubeData.rotX, cubeData.rotY, cubeData.rotZ, cubeData.rotW);
            meshRenderer.material.color = new Color(cubeData.colorR, cubeData.colorG, cubeData.colorB, cubeData.colorA);
            customName = cubeData.customName;
            Debug.Log($"Loaded Cube State for '{cubeSaveId}': Pos={transform.position}, Color={meshRenderer.material.color}, Name='{customName}'");
        }
        else
        {
            Debug.LogError($"Mismatch data type for SavableCube '{cubeSaveId}'. Expected CubeSavableData, got {data?.GetType().Name ?? "null"}.");
        }
    }
}


// ================================================================================================
// EXAMPLE USAGE: UI FOR MANUAL SAVE/LOAD (OPTIONAL)
// ================================================================================================

/*
/// <summary>
/// Simple UI to trigger manual Save/Load from a Button.
/// Attach this to a UI Canvas and hook up buttons.
/// </summary>
public class SaveLoadTestUI : MonoBehaviour
{
    public void OnSaveButtonClicked()
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.SaveGame();
        }
    }

    public void OnLoadButtonClicked()
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.LoadGame();
        }
    }
}
*/
```