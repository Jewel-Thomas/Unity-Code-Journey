// Unity Design Pattern Example: SaveLoadSystem
// This script demonstrates the SaveLoadSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SaveLoadSystem' design pattern in Unity allows you to manage the persistence of game data efficiently. It centralizes the saving and loading logic, making it easier for various game objects (entities) to contribute their state to a save file and restore it later.

This example provides a complete, self-contained C# Unity script that demonstrates this pattern.

### How the SaveLoadSystem Pattern Works:

1.  **`SaveLoadManager` (The Central Hub):**
    *   A Singleton `MonoBehaviour` that orchestrates the entire save/load process.
    *   It maintains a list of all active `ISaveable` entities in the scene.
    *   Handles file I/O (reading from and writing to `Application.persistentDataPath`).
    *   Uses `JsonUtility` for serializing (converting to JSON) and deserializing (converting from JSON) game data.
    *   Provides public methods like `SaveGame()`, `LoadGame()`, and `NewGame()`.

2.  **`ISaveable` (The Contract):**
    *   An interface that defines the contract for any object that wishes to be saved and loaded.
    *   **`Id`**: A unique string identifier for the entity. This is crucial for matching the correct saved data to the correct object when loading. It's often set manually in the inspector or auto-generated as a GUID.
    *   **`CaptureState()`**: A method called by the `SaveLoadManager` to ask the entity for its current state. The entity converts its relevant data into a serializable format (in this example, a JSON string).
    *   **`RestoreState(string stateJson)`**: A method called by the `SaveLoadManager` to provide the entity with its previously saved state. The entity takes the JSON string, deserializes it, and applies the saved data to itself.

3.  **`SaveableEntity` (The Base Implementation - Optional but Recommended):**
    *   An abstract `MonoBehaviour` class that implements `ISaveable`.
    *   It handles the common logic for `ISaveable` objects: generating/managing the unique `Id` and automatically registering/unregistering with the `SaveLoadManager` when enabled/disabled.
    *   Concrete saveable objects will inherit from this base class.

4.  **`GameData` (The Data Container):**
    *   A `[Serializable]` class that acts as a wrapper for all the game's saved data.
    *   It primarily holds a dictionary (`Dictionary<string, string>`) where keys are the `Id`s of `ISaveable` entities, and values are the JSON strings representing their individual states.
    *   A custom `List<StateEntry>` is used internally to allow `JsonUtility` to properly serialize/deserialize the dictionary, as `JsonUtility` has limitations with direct `Dictionary` serialization.

5.  **Concrete `ISaveable` Implementations (e.g., `Player`, `Item`, `Enemy`):**
    *   These are your actual game objects that need to persist data.
    *   They inherit from `SaveableEntity` (or implement `ISaveable` directly).
    *   They define their own specific `[Serializable]` data structures (e.g., `PlayerProgressData`, `SaveableTransformData`) to hold their unique state.
    *   They implement `CaptureState()` to convert their current data into their specific data structure, then to a JSON string.
    *   They implement `RestoreState()` to take a JSON string, deserialize it into their specific data structure, and apply it.

This pattern promotes modularity, making it easy to add new saveable objects without significantly altering the core save/load logic.

---

### `SaveLoadSystem.cs`

This single script file contains all the necessary classes and interfaces to implement the SaveLoadSystem pattern.

**To use this script:**

1.  Create a new C# script in your Unity project called `SaveLoadSystem.cs`.
2.  Copy and paste the entire code below into `SaveLoadSystem.cs`.
3.  Create an empty GameObject in your scene, name it `SaveLoadManager`, and attach the `SaveLoadSystem` component to it.
4.  For any GameObject you want to make saveable (e.g., your player, important items):
    *   Attach one of the concrete `Saveable` scripts (like `ExampleSaveablePlayer` or `ExampleSaveableTransform`) or create your own by inheriting from `SaveableEntity`.
    *   Ensure each `SaveableEntity` has a unique ID in the Inspector. If left empty, it will generate a GUID on `Awake()`.
5.  To trigger a save/load:
    *   Call `SaveLoadManager.Instance.SaveGame()` when the player quits, checkpoints, etc.
    *   Call `SaveLoadManager.Instance.LoadGame()` when starting the game or loading a save slot.
    *   Call `SaveLoadManager.Instance.NewGame()` to clear existing save data and start fresh.

---

```csharp
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// Region for Core Save/Load System Components
#region Core Save/Load System Components

/// <summary>
/// Interface for any object that can be saved and loaded by the SaveLoadManager.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Gets a unique string identifier for this saveable entity.
    /// This ID is used by the SaveLoadManager to link saved data to the correct object.
    /// It should be persistent across game sessions and unique within the scene.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Captures the current state of the entity and returns it as a JSON string.
    /// The entity is responsible for serializing its relevant data into a string format.
    /// </summary>
    /// <returns>A JSON string representing the entity's current state.</returns>
    string CaptureState();

    /// <summary>
    /// Restores the entity's state from a given JSON string.
    /// The entity is responsible for deserializing the string and applying the data.
    /// </summary>
    /// <param name="stateJson">A JSON string containing the previously saved state of the entity.</param>
    void RestoreState(string stateJson);
}

/// <summary>
/// Abstract base class for MonoBehaviour scripts that implement ISaveable.
/// Provides common functionality like unique ID management and automatic registration/unregistration
/// with the SaveLoadManager.
/// </summary>
[DisallowMultipleComponent] // Prevents multiple SaveableEntity components on the same GameObject
public abstract class SaveableEntity : MonoBehaviour, ISaveable
{
    // [SerializeField] allows this private field to be set in the Unity Inspector.
    // It's crucial for the Id to be unique and stable. Designers can set it manually,
    // or it will generate a GUID if left empty.
    [SerializeField]
    private string _id = "";

    /// <summary>
    /// The unique identifier for this saveable entity.
    /// If left empty in the inspector, a GUID will be generated on Awake.
    /// </summary>
    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id))
            {
                // Generate a new GUID if the ID is not set.
                // This makes it unique but won't be saved if you save the scene without
                // applying changes. For robust usage, set it manually or save the scene
                // after it's auto-generated.
                _id = Guid.NewGuid().ToString();
            }
            return _id;
        }
    }

    /// <summary>
    /// Automatically registers this SaveableEntity with the SaveLoadManager when enabled.
    /// This ensures the manager knows about all active saveable objects in the scene.
    /// </summary>
    protected virtual void OnEnable()
    {
        SaveLoadManager.RegisterSaveable(this);
    }

    /// <summary>
    /// Automatically unregisters this SaveableEntity from the SaveLoadManager when disabled.
    /// This prevents the manager from trying to save/load non-existent or inactive objects.
    /// </summary>
    protected virtual void OnDisable()
    {
        SaveLoadManager.UnregisterSaveable(this);
    }

    // Abstract methods that must be implemented by concrete SaveableEntity classes.
    public abstract string CaptureState();
    public abstract void RestoreState(string stateJson);
}

/// <summary>
/// A serializable class representing a single key-value pair for state data.
/// This is used to allow JsonUtility to serialize a Dictionary<string, string>,
/// which it doesn't support directly.
/// </summary>
[Serializable]
public class StateEntry
{
    public string id;          // The unique ID of the ISaveable entity
    public string stateJson;   // The JSON string representing the entity's state

    public StateEntry(string id, string stateJson)
    {
        this.id = id;
        this.stateJson = stateJson;
    }
}

/// <summary>
/// The main container for all game data that needs to be saved.
/// It holds a collection of StateEntry objects, each representing the state
/// of an individual ISaveable entity.
/// </summary>
[Serializable]
public class GameData
{
    // JsonUtility cannot directly serialize Dictionary<TKey, TValue>.
    // Therefore, we use a List of custom StateEntry objects to store the data.
    public List<StateEntry> stateEntries = new List<StateEntry>();

    public GameData() { } // Default constructor for deserialization

    /// <summary>
    /// Prepares the internal list of state entries for serialization from a dictionary.
    /// This should be called before converting GameData to JSON.
    /// </summary>
    /// <param name="statesDictionary">The dictionary of states to serialize.</param>
    public void PrepareForSerialization(Dictionary<string, string> statesDictionary)
    {
        stateEntries.Clear();
        foreach (var kvp in statesDictionary)
        {
            stateEntries.Add(new StateEntry(kvp.Key, kvp.Value));
        }
    }

    /// <summary>
    /// Converts the internal list of state entries back into a dictionary after deserialization.
    /// This should be called after converting JSON to GameData.
    /// </summary>
    /// <returns>A dictionary containing the deserialized states.</returns>
    public Dictionary<string, string> GetStatesDictionary()
    {
        Dictionary<string, string> states = new Dictionary<string, string>();
        foreach (var entry in stateEntries)
        {
            states[entry.id] = entry.stateJson;
        }
        return states;
    }
}

/// <summary>
/// The central manager for saving and loading game data.
/// It's a Singleton MonoBehaviour, accessible from anywhere.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    // Singleton instance for easy global access
    public static SaveLoadManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField]
    private string _saveFileName = "game_save.json"; // Name of the save file
    [SerializeField]
    private bool _encryptSaveFile = false; // Example: Add simple encryption (not implemented here)

    // A list of all ISaveable entities currently registered with the manager.
    private static List<ISaveable> _saveables = new List<ISaveable>();

    // Path where the save file will be stored (cross-platform persistent data path).
    private string _savePath;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton and sets up the save path.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy duplicate instances to ensure only one Singleton exists.
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Keep this object alive across scene loads.
        DontDestroyOnLoad(gameObject);

        // Determine the save file path. Application.persistentDataPath is platform-independent.
        _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);
        Debug.Log($"SaveLoadManager initialized. Save Path: {_savePath}");
    }

    /// <summary>
    /// Registers an ISaveable entity with the manager.
    /// Called by SaveableEntity.OnEnable().
    /// </summary>
    /// <param name="saveable">The ISaveable entity to register.</param>
    public static void RegisterSaveable(ISaveable saveable)
    {
        if (!_saveables.Contains(saveable))
        {
            _saveables.Add(saveable);
            // Debug.Log($"Registered saveable: {saveable.Id}");
        }
    }

    /// <summary>
    /// Unregisters an ISaveable entity from the manager.
    /// Called by SaveableEntity.OnDisable().
    /// </summary>
    /// <param name="saveable">The ISaveable entity to unregister.</param>
    public static void UnregisterSaveable(ISaveable saveable)
    {
        if (_saveables.Contains(saveable))
        {
            _saveables.Remove(saveable);
            // Debug.Log($"Unregistered saveable: {saveable.Id}");
        }
    }

    /// <summary>
    /// Gathers state from all registered ISaveable entities and saves it to a file.
    /// </summary>
    public void SaveGame()
    {
        // Create a new GameData object to hold all the captured states.
        GameData gameData = new GameData();
        Dictionary<string, string> allStates = new Dictionary<string, string>();

        // Iterate through all registered ISaveable entities and capture their state.
        foreach (var saveable in _saveables)
        {
            if (string.IsNullOrEmpty(saveable.Id))
            {
                Debug.LogWarning($"Skipping save for entity with empty ID: {((MonoBehaviour)saveable).name}");
                continue;
            }
            allStates[saveable.Id] = saveable.CaptureState();
            // Debug.Log($"Captured state for {saveable.Id}: {allStates[saveable.Id]}");
        }

        // Prepare the GameData object for JsonUtility serialization.
        gameData.PrepareForSerialization(allStates);

        // Convert the GameData object to a JSON string.
        string json = JsonUtility.ToJson(gameData, true); // 'true' for pretty printing

        try
        {
            // Write the JSON string to the save file.
            File.WriteAllText(_savePath, json);
            Debug.Log($"Game saved successfully to {_savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game to {_savePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Loads game data from the save file and restores the state of all ISaveable entities.
    /// </summary>
    public void LoadGame()
    {
        if (!File.Exists(_savePath))
        {
            Debug.LogWarning($"No save file found at {_savePath}. Starting new game or no data to load.");
            return;
        }

        try
        {
            // Read the JSON string from the save file.
            string json = File.ReadAllText(_savePath);

            // Deserialize the JSON string back into a GameData object.
            GameData gameData = JsonUtility.FromJson<GameData>(json);

            // Get the states dictionary from the deserialized GameData.
            Dictionary<string, string> allStates = gameData.GetStatesDictionary();

            // Iterate through all registered ISaveable entities and restore their state.
            foreach (var saveable in _saveables)
            {
                if (allStates.TryGetValue(saveable.Id, out string stateJson))
                {
                    saveable.RestoreState(stateJson);
                    // Debug.Log($"Restored state for {saveable.Id}: {stateJson}");
                }
                else
                {
                    Debug.LogWarning($"No saved state found for entity: {saveable.Id} ({((MonoBehaviour)saveable).name}). Keeping current state.");
                }
            }
            Debug.Log($"Game loaded successfully from {_savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from {_savePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Deletes the current save file, effectively starting a new game.
    /// </summary>
    public void NewGame()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                File.Delete(_savePath);
                Debug.Log($"Save file deleted: {_savePath}. Starting a new game.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file at {_savePath}: {e.Message}");
            }
        }
        else
        {
            Debug.Log("No save file found to delete. Starting a new game.");
        }

        // Optionally, reset all saveable entities to their default state after deleting save file.
        // This might involve reloading the scene or manually resetting each entity.
        // For this example, we'll just log.
        // You might want to reload the current scene or instantiate new player, etc.
    }

    /// <summary>
    /// Returns true if a save file currently exists.
    /// </summary>
    public bool HasSaveGame()
    {
        return File.Exists(_savePath);
    }
}

#endregion // Core Save/Load System Components

// Region for Example Saveable Entities
#region Example Saveable Entities

/// <summary>
/// A serializable struct to hold the state of an ExampleSaveablePlayer.
/// Structs are generally good for small data bundles.
/// </summary>
[Serializable]
public struct PlayerProgressData
{
    public int score;
    public int level;
    public string playerName;
    public float health;
}

/// <summary>
/// An example implementation of a saveable player MonoBehaviour.
/// Inherits from SaveableEntity to get ID management and auto-registration.
/// </summary>
public class ExampleSaveablePlayer : SaveableEntity
{
    [Header("Player Data")]
    public int _score = 0;
    public int _level = 1;
    public string _playerName = "Hero";
    public float _health = 100f;

    // A simple test method to simulate player actions
    public void AddScore(int amount)
    {
        _score += amount;
        Debug.Log($"{_playerName} scored {amount}! Total score: {_score}");
    }

    public void TakeDamage(float amount)
    {
        _health -= amount;
        Debug.Log($"{_playerName} took {amount} damage! Health: {_health}");
    }

    /// <summary>
    /// Overrides CaptureState to save player-specific data.
    /// </summary>
    /// <returns>A JSON string representing the player's state.</returns>
    public override string CaptureState()
    {
        PlayerProgressData data = new PlayerProgressData
        {
            score = _score,
            level = _level,
            playerName = _playerName,
            health = _health
        };
        // Use JsonUtility to convert the struct to a JSON string.
        return JsonUtility.ToJson(data);
    }

    /// <summary>
    /// Overrides RestoreState to load player-specific data.
    /// </summary>
    /// <param name="stateJson">The JSON string containing the player's saved state.</param>
    public override void RestoreState(string stateJson)
    {
        // Use JsonUtility to convert the JSON string back to the struct.
        PlayerProgressData data = JsonUtility.FromJson<PlayerProgressData>(stateJson);
        _score = data.score;
        _level = data.level;
        _playerName = data.playerName;
        _health = data.health;
        Debug.Log($"Player '{_playerName}' ({Id}) state restored: Score={_score}, Level={_level}, Health={_health}");
    }

    // Example of how the player might interact with the system (for testing purposes)
    private void Start()
    {
        Debug.Log($"ExampleSaveablePlayer '{_playerName}' ({Id}) initialized.");
        // Simulate some initial actions
        if (_score == 0 && _level == 1) // Only do this if it's a fresh start before any load
        {
            AddScore(100);
            TakeDamage(10);
        }
    }
}

/// <summary>
/// A serializable struct to hold the transform state of a GameObject.
/// </summary>
[Serializable]
public struct SaveableTransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
}

/// <summary>
/// An example implementation of a saveable GameObject's transform.
/// This allows saving and loading a GameObject's position, rotation, and scale.
/// </summary>
public class ExampleSaveableTransform : SaveableEntity
{
    [Header("Transform Settings")]
    [Tooltip("If true, the transform will reset to its initial position on Awake before loading.")]
    public bool resetOnAwake = true;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    protected override void OnEnable()
    {
        base.OnEnable(); // Call base class OnEnable for registration
        if (resetOnAwake)
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }
    }

    /// <summary>
    /// Overrides CaptureState to save the GameObject's transform data.
    /// </summary>
    /// <returns>A JSON string representing the transform's state.</returns>
    public override string CaptureState()
    {
        SaveableTransformData data = new SaveableTransformData
        {
            position = transform.position,
            rotation = transform.rotation,
            localScale = transform.localScale
        };
        return JsonUtility.ToJson(data);
    }

    /// <summary>
    /// Overrides RestoreState to load the GameObject's transform data.
    /// </summary>
    /// <param name="stateJson">The JSON string containing the transform's saved state.</param>
    public override void RestoreState(string stateJson)
    {
        SaveableTransformData data = JsonUtility.FromJson<SaveableTransformData>(stateJson);
        transform.position = data.position;
        transform.rotation = data.rotation;
        transform.localScale = data.localScale;
        Debug.Log($"Transform ({Id}) state restored: Pos={transform.position}, Rot={transform.rotation.eulerAngles}, Scale={transform.localScale}");
    }

    // Example of how the transform might be moved for testing
    private void Update()
    {
        // Simple movement for testing purposes
        if (Input.GetKey(KeyCode.W)) transform.Translate(Vector3.forward * Time.deltaTime * 2);
        if (Input.GetKey(KeyCode.S)) transform.Translate(Vector3.back * Time.deltaTime * 2);
        if (Input.GetKey(KeyCode.A)) transform.Rotate(Vector3.up, -90 * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) transform.Rotate(Vector3.up, 90 * Time.deltaTime);
    }
}

#endregion // Example Saveable Entities

// Region for Editor UI and Test Scene Manager (for demonstration purposes)
#region Editor UI and Test Scene Manager

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Custom editor for SaveableEntity to provide a button for generating a GUID.
/// This makes it easier for developers to assign unique IDs in the Inspector.
/// </summary>
[CustomEditor(typeof(SaveableEntity), true)] // true makes it apply to derived classes too
public class SaveableEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SaveableEntity saveable = (SaveableEntity)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Saveable ID:", saveable.Id, EditorStyles.boldLabel);

        if (string.IsNullOrEmpty(saveable.Id))
        {
            if (GUILayout.Button("Generate Unique ID (GUID)"))
            {
                // Use reflection to access the private _id field
                var idField = typeof(SaveableEntity).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null)
                {
                    string newId = Guid.NewGuid().ToString();
                    idField.SetValue(saveable, newId);
                    EditorUtility.SetDirty(saveable); // Mark object as dirty to save changes
                    Debug.Log($"Generated new ID for {saveable.name}: {newId}");
                }
            }
        }
    }
}
#endif

/// <summary>
/// A simple MonoBehaviour to demonstrate the usage of the SaveLoadManager
/// with UI buttons in a test scene.
/// </summary>
public class TestSceneSaveLoadManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab; // Assign your player prefab here
    private ExampleSaveablePlayer _currentPlayer;

    void Start()
    {
        // Optionally instantiate a player if none exists.
        // For demonstration, let's assume a player is already in the scene.
        _currentPlayer = FindObjectOfType<ExampleSaveablePlayer>();
        if (_currentPlayer == null && _playerPrefab != null)
        {
            _currentPlayer = Instantiate(_playerPrefab).GetComponent<ExampleSaveablePlayer>();
            Debug.Log("Instantiated player for testing.");
        }
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 20;
        GUI.skin.box.fontSize = 20;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Box("Save/Load Controls");

        if (GUILayout.Button("Save Game"))
        {
            SaveLoadManager.Instance.SaveGame();
        }

        if (GUILayout.Button("Load Game"))
        {
            SaveLoadManager.Instance.LoadGame();
        }

        if (GUILayout.Button("New Game"))
        {
            SaveLoadManager.Instance.NewGame();
            // Optionally, reset player/scene state after new game.
            // For example, if you have a default scene to load:
            // UnityEngine.SceneManagement.SceneManager.LoadScene(
            //     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            // Or if you want to reset saveables manually:
            // FindObjectsOfType<ISaveable>().ToList().ForEach(s => s.RestoreState(s.CaptureDefaultState()));
        }

        GUILayout.EndArea();
    }

    // Example of calling these methods from other scripts:
    /*
    // To save progress from a game script:
    void PlayerReachedCheckpoint()
    {
        SaveLoadManager.Instance.SaveGame();
        Debug.Log("Checkpoint saved!");
    }

    // To load on game start:
    void GameStart()
    {
        if (SaveLoadManager.Instance.HasSaveGame())
        {
            SaveLoadManager.Instance.LoadGame();
            Debug.Log("Game loaded from save.");
        }
        else
        {
            Debug.Log("No save game found. Starting new game.");
        }
    }
    */
}

#endregion // Editor UI and Test Scene Manager
```