// Unity Design Pattern Example: RuntimeDataSerialization
// This script demonstrates the RuntimeDataSerialization pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RuntimeDataSerialization' pattern focuses on saving and loading the *runtime state* of objects within your game. Instead of directly serializing `MonoBehaviour` instances (which is generally problematic in Unity due to scene references and component coupling), you define plain data structures (Plain Old C# Objects - POCOs) that hold the relevant state. An orchestrating manager then converts runtime objects into these serializable data structures and vice-versa.

This pattern is essential for:
*   **Game Save/Load Systems:** Saving player progress, world state.
*   **Level Editors:** Saving dynamically placed objects.
*   **Procedural Content Generation:** Saving generated worlds or items.
*   **Network Synchronization:** Sending object state across a network.

## RuntimeDataSerialization Example in Unity

This example demonstrates the pattern by allowing you to save and load the positions, health, and other simple properties of dynamically created `Player` and `Enemy` objects in your scene.

**How to Use This Script:**

1.  **Create a New Unity Project** or open an existing one.
2.  **Create a New C# Script** named `RuntimeDataSerializationExample.cs` (or similar) and copy the entire code below into it.
3.  **Create an Empty GameObject** in your scene (e.g., named "GameManager").
4.  **Attach the `RuntimeDataSerializationManager` component** from the script to this "GameManager" object.
5.  **Create two Prefabs:**
    *   An empty GameObject named "PlayerPrefab".
    *   An empty GameObject named "EnemyPrefab".
6.  **Attach the `PlayerController` component** (from the script) to your "PlayerPrefab".
7.  **Attach the `EnemyController` component** (from the script) to your "EnemyPrefab".
8.  **In the Inspector** for the "GameManager" (which has `RuntimeDataSerializationManager`):
    *   Locate the "Serializable Prefabs" list.
    *   Add two entries:
        *   **Entry 0:** Set `ObjectType` to "Player", Drag your "PlayerPrefab" into the `Prefab` slot.
        *   **Entry 1:** Set `ObjectType` to "Enemy", Drag your "EnemyPrefab" into the `Prefab` slot.
9.  **Run the Game:**
    *   You'll see buttons for "Create Player", "Create Enemy", "Randomize All", "Save Game", and "Load Game".
    *   Click "Create Player" and "Create Enemy" a few times.
    *   Click "Randomize All" to change their positions and health.
    *   Click "Save Game".
    *   Stop the game, then run it again.
    *   Click "Load Game". You should see your previously saved objects restored with their correct states.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // For .Where(), .ToList()

/// <summary>
/// This script demonstrates the Runtime Data Serialization design pattern in Unity.
/// It provides a framework for saving and loading the dynamic state of game objects
/// without directly serializing MonoBehaviours, ensuring a robust and flexible save system.
///
/// Pattern Components:
/// 1.  ISerializableRuntimeData: An interface for runtime objects that can be serialized.
/// 2.  SerializableGameObjectData: A plain data class (POCO) to hold the state of a single game object.
/// 3.  GameSaveData: A top-level container for all serializable data, including global game state.
/// 4.  RuntimeDataSerializationManager: The central manager responsible for orchestrating
///     the serialization (runtime object -> data) and deserialization (data -> runtime object) process.
/// 5.  Concrete Implementations (PlayerController, EnemyController): Examples of runtime objects
///     that implement ISerializableRuntimeData.
/// </summary>

// --- 1. Data Structures for Serialization ---

/// <summary>
/// A simple serializable struct to represent a Vector3.
/// Unity's JsonUtility cannot directly serialize Vector3 as a top-level object,
/// but it works fine when embedded in another [Serializable] class/struct.
/// This explicit wrapper ensures compatibility and clarity.
/// </summary>
[System.Serializable]
public struct Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3Data(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    // Implicit conversion from Vector3Data to Vector3
    public static implicit operator Vector3(Vector3Data vData)
    {
        return new Vector3(vData.x, vData.y, vData.z);
    }

    // Implicit conversion from Vector3 to Vector3Data
    public static implicit operator Vector3Data(Vector3 v)
    {
        return new Vector3Data(v.x, v.y, v.z);
    }
}

/// <summary>
/// This is a Plain Old C# Object (POCO) that holds the serializable state
/// of a single game object. It's crucial that this class contains ONLY
/// data and no references to Unity components, GameObjects, or scene objects.
/// This makes it safe for JSON serialization and deserialization.
/// </summary>
[System.Serializable]
public class SerializableGameObjectData
{
    public string uniqueId;     // A unique identifier for the object (e.g., GUID).
    public string objectType;   // A string to identify the type of object (e.g., "Player", "Enemy")
                                // used by the manager to know which prefab to instantiate.
    public Vector3Data position; // The object's position.
    public int health;          // Example: object's health.
    public string objectName;   // Example: object's name.
    // Add any other properties you need to save for a specific object type.
}

/// <summary>
/// The top-level data container that will be serialized to a file (e.g., JSON).
/// It holds a list of all individual SerializableGameObjectData objects,
/// and can also include global game state like current level, time, player score, etc.
/// </summary>
[System.Serializable]
public class GameSaveData
{
    public List<SerializableGameObjectData> serializedObjects = new List<SerializableGameObjectData>();
    public int gameTimeInSeconds; // Example: Global game time.
    public string currentSceneName; // Example: Which scene was active.
    // Add any other global game state variables here.
}

// --- 2. Interface for Runtime Objects ---

/// <summary>
/// This interface defines the contract for any MonoBehaviour that wants to
/// participate in the RuntimeDataSerialization process.
/// Objects implementing this interface know how to convert their runtime state
/// into a serializable data object and how to restore their state from one.
/// </summary>
public interface ISerializableRuntimeData
{
    /// <summary>
    /// Returns a unique identifier for this specific runtime object.
    /// This ID is used to match saved data with existing runtime objects or to
    /// create new ones if they don't exist in the scene.
    /// </summary>
    string GetUniqueIdentifier();

    /// <summary>
    /// Converts the current runtime state of this object into a
    /// SerializableGameObjectData object.
    /// </summary>
    SerializableGameObjectData GetSerializableData();

    /// <summary>
    /// Restores the runtime state of this object using data from a
    /// SerializableGameObjectData object.
    /// </summary>
    /// <param name="data">The data to restore from.</param>
    void RestoreFromSerializableData(SerializableGameObjectData data);
}

// --- 3. Concrete Example Objects (Player & Enemy) ---

/// <summary>
/// A simple example of a player character that can be serialized.
/// Implements ISerializableRuntimeData to interact with the SerializationManager.
/// </summary>
public class PlayerController : MonoBehaviour, ISerializableRuntimeData
{
    [SerializeField] private string _uniqueId; // Should be set once (e.g., in Awake)
    [SerializeField] private int _health = 100;

    private const string PLAYER_TYPE = "Player"; // Constant type for this object

    void Awake()
    {
        // For a player, the ID might be static or persistent across sessions.
        // If it's the *only* player, a fixed ID like "Player1" is fine.
        // If there could be multiple players (e.g., in a multiplayer game),
        // you'd use a GUID or a system-generated ID.
        if (string.IsNullOrEmpty(_uniqueId))
        {
            _uniqueId = "MainPlayer"; // Fixed ID for a primary player
        }
    }

    void OnEnable()
    {
        // Register with the manager so its state can be saved/loaded.
        RuntimeDataSerializationManager.Instance?.Register(this);
    }

    void OnDisable()
    {
        // Unregister when disabled or destroyed to prevent null references.
        RuntimeDataSerializationManager.Instance?.Unregister(this);
    }

    // --- ISerializableRuntimeData Implementation ---

    public string GetUniqueIdentifier() => _uniqueId;

    public SerializableGameObjectData GetSerializableData()
    {
        // Create a new data object and populate it with the current state.
        return new SerializableGameObjectData
        {
            uniqueId = _uniqueId,
            objectType = PLAYER_TYPE,
            position = transform.position,
            health = _health,
            objectName = gameObject.name
        };
    }

    public void RestoreFromSerializableData(SerializableGameObjectData data)
    {
        // Apply the loaded data back to this runtime object.
        if (data.uniqueId != _uniqueId || data.objectType != PLAYER_TYPE)
        {
            Debug.LogWarning($"Attempted to restore player with mismatching ID/Type: {data.uniqueId} ({data.objectType}) vs {_uniqueId} ({PLAYER_TYPE})");
            return;
        }

        transform.position = data.position;
        _health = data.health;
        gameObject.name = data.objectName;
        Debug.Log($"Player '{_uniqueId}' restored: Pos={transform.position}, Health={_health}");
    }

    // --- Demo Specific Methods ---
    public void RandomizeState()
    {
        transform.position = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
        _health = UnityEngine.Random.Range(50, 200);
        Debug.Log($"Player '{_uniqueId}' randomized to Pos={transform.position}, Health={_health}");
    }
}

/// <summary>
/// A simple example of an enemy character that can be serialized.
/// Implements ISerializableRuntimeData.
/// </summary>
public class EnemyController : MonoBehaviour, ISerializableRuntimeData
{
    [SerializeField] private string _uniqueId; // Each enemy needs a unique ID
    [SerializeField] private int _health = 50;

    private const string ENEMY_TYPE = "Enemy"; // Constant type for this object

    void Awake()
    {
        // For dynamically created enemies, a GUID is a good choice for unique IDs.
        // This ensures that even if you have multiple enemies of the same type,
        // their save data can be uniquely identified.
        if (string.IsNullOrEmpty(_uniqueId))
        {
            _uniqueId = Guid.NewGuid().ToString();
            gameObject.name = $"{ENEMY_TYPE}_{_uniqueId.Substring(0, 4)}"; // Give it a unique-ish name
        }
    }

    void OnEnable()
    {
        RuntimeDataSerializationManager.Instance?.Register(this);
    }

    void OnDisable()
    {
        RuntimeDataSerializationManager.Instance?.Unregister(this);
    }

    // --- ISerializableRuntimeData Implementation ---

    public string GetUniqueIdentifier() => _uniqueId;

    public SerializableGameObjectData GetSerializableData()
    {
        return new SerializableGameObjectData
        {
            uniqueId = _uniqueId,
            objectType = ENEMY_TYPE,
            position = transform.position,
            health = _health,
            objectName = gameObject.name
        };
    }

    public void RestoreFromSerializableData(SerializableGameObjectData data)
    {
        if (data.uniqueId != _uniqueId || data.objectType != ENEMY_TYPE)
        {
            Debug.LogWarning($"Attempted to restore enemy with mismatching ID/Type: {data.uniqueId} ({data.objectType}) vs {_uniqueId} ({ENEMY_TYPE})");
            return;
        }

        transform.position = data.position;
        _health = data.health;
        gameObject.name = data.objectName;
        Debug.Log($"Enemy '{_uniqueId}' restored: Pos={transform.position}, Health={_health}");
    }

    // --- Demo Specific Methods ---
    public void RandomizeState()
    {
        transform.position = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
        _health = UnityEngine.Random.Range(10, 100);
        Debug.Log($"Enemy '{_uniqueId}' randomized to Pos={transform.position}, Health={_health}");
    }
}

// --- 4. The Serialization Manager ---

/// <summary>
/// A helper class to map an object type string to its corresponding prefab.
/// Used by the SerializationManager in the Inspector to configure which prefabs
/// should be instantiated when loading saved data for new objects.
/// </summary>
[System.Serializable]
public class PrefabMapping
{
    public string objectType; // e.g., "Player", "Enemy"
    public GameObject prefab;
}

/// <summary>
/// The central manager for Runtime Data Serialization.
/// It orchestrates saving and loading by interacting with ISerializableRuntimeData objects
/// and handling the conversion to/from the plain data structures.
/// </summary>
public class RuntimeDataSerializationManager : MonoBehaviour
{
    // Singleton pattern for easy access from other scripts.
    public static RuntimeDataSerializationManager Instance { get; private set; }

    [Header("Serialization Settings")]
    [Tooltip("List of prefabs for different serializable object types. Used during loading.")]
    [SerializeField] private List<PrefabMapping> serializablePrefabs = new List<PrefabMapping>();

    private Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();

    // A dictionary to keep track of all currently active ISerializableRuntimeData objects in the scene.
    private Dictionary<string, ISerializableRuntimeData> _registeredObjects = new Dictionary<string, ISerializableRuntimeData>();

    private const string SAVE_FILE_NAME = "game_save.json";

    void Awake()
    {
        // Singleton initialization.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep manager across scenes.
            InitializePrefabMap();
        }
    }

    private void InitializePrefabMap()
    {
        _prefabMap.Clear();
        foreach (var mapping in serializablePrefabs)
        {
            if (string.IsNullOrEmpty(mapping.objectType))
            {
                Debug.LogError("Prefab mapping has an empty ObjectType. Please fix it in the Inspector.");
                continue;
            }
            if (mapping.prefab == null)
            {
                Debug.LogError($"Prefab mapping for type '{mapping.objectType}' has no prefab assigned.");
                continue;
            }

            if (_prefabMap.ContainsKey(mapping.objectType))
            {
                Debug.LogWarning($"Duplicate ObjectType '{mapping.objectType}' found in serializable prefabs. The first one will be used.");
                continue;
            }
            _prefabMap.Add(mapping.objectType, mapping.prefab);
        }
        Debug.Log($"Initialized prefab map with {_prefabMap.Count} entries.");
    }

    /// <summary>
    /// Registers an ISerializableRuntimeData object with the manager.
    /// This object's state will be included in save operations.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    public void Register(ISerializableRuntimeData obj)
    {
        string uniqueId = obj.GetUniqueIdentifier();
        if (_registeredObjects.ContainsKey(uniqueId))
        {
            // This can happen if an object is enabled, then disabled, then re-enabled
            // without being fully destroyed, or if two objects have the same ID (error!).
            Debug.LogWarning($"Object with ID '{uniqueId}' already registered. Overwriting. This might indicate a bug or duplicate ID.");
            _registeredObjects[uniqueId] = obj;
        }
        else
        {
            _registeredObjects.Add(uniqueId, obj);
        }
        Debug.Log($"Registered object: {uniqueId}");
    }

    /// <summary>
    /// Unregisters an ISerializableRuntimeData object from the manager.
    /// Its state will no longer be included in save operations.
    /// </summary>
    /// <param name="obj">The object to unregister.</param>
    public void Unregister(ISerializableRuntimeData obj)
    {
        string uniqueId = obj.GetUniqueIdentifier();
        if (_registeredObjects.Remove(uniqueId))
        {
            Debug.Log($"Unregistered object: {uniqueId}");
        }
    }

    /// <summary>
    /// Collects the state of all registered objects, serializes it to JSON,
    /// and saves it to a file.
    /// </summary>
    /// <param name="filename">The name of the file to save to.</param>
    public void SaveGame(string filename = SAVE_FILE_NAME)
    {
        GameSaveData saveData = new GameSaveData();

        // Populate global game state (example)
        saveData.gameTimeInSeconds = (int)Time.timeSinceLevelLoad;
        saveData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Collect data from all registered ISerializableRuntimeData objects.
        foreach (var obj in _registeredObjects.Values)
        {
            saveData.serializedObjects.Add(obj.GetSerializableData());
        }

        // Convert the GameSaveData object to a JSON string.
        string json = JsonUtility.ToJson(saveData, true); // 'true' for pretty printing.

        // Determine the save path and write the JSON to a file.
        string filePath = GetSaveFilePath(filename);
        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Game saved successfully to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game to {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Loads game data from a specified file, deserializes it,
    /// and restores the state of game objects in the scene.
    /// </summary>
    /// <param name="filename">The name of the file to load from.</param>
    public void LoadGame(string filename = SAVE_FILE_NAME)
    {
        string filePath = GetSaveFilePath(filename);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Save file not found at: {filePath}");
            return;
        }

        try
        {
            // Read the JSON string from the file.
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON string back into a GameSaveData object.
            GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(json);

            if (loadedData == null)
            {
                Debug.LogError($"Failed to deserialize save data from: {filePath}");
                return;
            }

            Debug.Log($"Game loaded from: {filePath}. Restoring {loadedData.serializedObjects.Count} objects...");

            // Restore global game state (example)
            Debug.Log($"Loaded game time: {loadedData.gameTimeInSeconds}s, Scene: {loadedData.currentSceneName}");

            // --- Strategy for loading and restoring objects ---
            // 1. Keep track of objects that have been restored or created.
            HashSet<string> restoredObjectIds = new HashSet<string>();

            foreach (var objData in loadedData.serializedObjects)
            {
                ISerializableRuntimeData runtimeObj;
                if (_registeredObjects.TryGetValue(objData.uniqueId, out runtimeObj))
                {
                    // Case 1: Object with this ID already exists in the scene.
                    // Restore its state.
                    runtimeObj.RestoreFromSerializableData(objData);
                    restoredObjectIds.Add(objData.uniqueId);
                }
                else
                {
                    // Case 2: Object with this ID does NOT exist in the scene.
                    // We need to instantiate it from a prefab and then restore its state.
                    if (_prefabMap.TryGetValue(objData.objectType, out GameObject prefab))
                    {
                        GameObject newGO = Instantiate(prefab, objData.position, Quaternion.identity);
                        ISerializableRuntimeData newRuntimeObj = newGO.GetComponent<ISerializableRuntimeData>();

                        if (newRuntimeObj != null)
                        {
                            // Important: Manually set the unique ID BEFORE restoring,
                            // if the ID is initialized in Awake and isn't auto-set by the component.
                            // For this example, Player/Enemy Awake sets its ID if it's empty,
                            // but if you have a component that's always assigned an ID on creation,
                            // this might need a public setter or an init method.
                            // For our example, if the prefab's script has a default empty _uniqueId,
                            // it will use the Awake logic. If you serialize an existing ID,
                            // you may need to ensure it's applied here.
                            // For simplicity, we'll let Awake handle initial ID,
                            // and then Restore will apply the rest of the data including name/pos.
                            // If _uniqueId was a public property, you could do: newRuntimeObj.UniqueId = objData.uniqueId;
                            
                            // A temporary override to ensure the loaded ID is used for the *new* object
                            // if its Awake generated a fresh GUID. This is a common point of complexity.
                            // In this specific example, EnemyController generates a new GUID in Awake
                            // if _uniqueId is null, but if we're loading, we want to force the loaded ID.
                            // This might require a public setter or a more robust init in the component.
                            // For the sake of this demo, we'll assume the Awake logic (if empty, generate)
                            // is fine, and then Restore will apply the rest. If _uniqueId was read-only
                            // and set by Guid.NewGuid() *always*, this would fail.
                            
                            // Let's ensure the uniqueId is set on the new component if it needs to be.
                            // This is a bit of a hack specific to how Enemy/PlayerController are written.
                            if (newRuntimeObj is EnemyController enemy)
                            {
                                enemy.SetUniqueIdForLoading(objData.uniqueId);
                            }
                            else if (newRuntimeObj is PlayerController player)
                            {
                                player.SetUniqueIdForLoading(objData.uniqueId); // Assuming we added this method
                            }
                            
                            newRuntimeObj.RestoreFromSerializableData(objData);
                            restoredObjectIds.Add(objData.uniqueId);
                        }
                        else
                        {
                            Debug.LogError($"Prefab '{prefab.name}' for type '{objData.objectType}' does not have an ISerializableRuntimeData component.");
                            Destroy(newGO); // Clean up if component is missing.
                        }
                    }
                    else
                    {
                        Debug.LogError($"No prefab found for object type: {objData.objectType}. Cannot instantiate object with ID: {objData.uniqueId}");
                    }
                }
            }

            // Case 3: Destroy objects that were in the scene but not in the save data.
            // This prevents "ghost" objects from previous sessions.
            List<ISerializableRuntimeData> objectsToDestroy = new List<ISerializableRuntimeData>();
            foreach (var registeredObj in _registeredObjects.Values)
            {
                if (!restoredObjectIds.Contains(registeredObj.GetUniqueIdentifier()))
                {
                    objectsToDestroy.Add(registeredObj);
                }
            }

            foreach (var obj in objectsToDestroy)
            {
                Debug.Log($"Destroying object '{obj.GetUniqueIdentifier()}' because it was not found in save data.");
                // Unregister first to avoid issues during Destroy.
                Unregister(obj); // Explicitly unregister
                Destroy(obj as MonoBehaviour).gameObject; // Destroy the GameObject associated with the component.
            }

            Debug.Log("Game load complete.");

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Gets the full path to the save file in a persistent location.
    /// </summary>
    private string GetSaveFilePath(string filename)
    {
        // Application.persistentDataPath is a reliable place for saving user data.
        return Path.Combine(Application.persistentDataPath, filename);
    }

    // --- Demo UI for testing ---
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Label("Runtime Data Serialization");

        if (GUILayout.Button("Create Player"))
        {
            CreatePrefabInstance("Player");
        }
        if (GUILayout.Button("Create Enemy"))
        {
            CreatePrefabInstance("Enemy");
        }
        if (GUILayout.Button("Randomize All"))
        {
            RandomizeAllObjects();
        }
        if (GUILayout.Button("Save Game"))
        {
            SaveGame();
        }
        if (GUILayout.Button("Load Game"))
        {
            LoadGame();
        }
        GUILayout.EndArea();
    }

    private void CreatePrefabInstance(string objectType)
    {
        if (_prefabMap.TryGetValue(objectType, out GameObject prefab))
        {
            GameObject newInstance = Instantiate(prefab, new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0), Quaternion.identity);
            newInstance.name = $"{objectType}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            Debug.Log($"Created new {objectType}: {newInstance.name}");
        }
        else
        {
            Debug.LogError($"No prefab mapped for type: {objectType}");
        }
    }

    private void RandomizeAllObjects()
    {
        foreach (var obj in _registeredObjects.Values)
        {
            if (obj is PlayerController player)
            {
                player.RandomizeState();
            }
            else if (obj is EnemyController enemy)
            {
                enemy.RandomizeState();
            }
        }
    }
}

// --- Additional methods for PlayerController and EnemyController for safe ID handling during loading ---
// It's a common issue where Awake/Start sets an ID, but on load you need to force a specific ID.
// This is one way to handle it, making the ID mutable specifically for the loading process.
public partial class PlayerController
{
    // Make this internal or protected if used within a larger assembly or inheritance hierarchy
    public void SetUniqueIdForLoading(string id)
    {
        _uniqueId = id;
    }
}

public partial class EnemyController
{
    // Make this internal or protected if used within a larger assembly or inheritance hierarchy
    public void SetUniqueIdForLoading(string id)
    {
        _uniqueId = id;
    }
}
```