// Unity Design Pattern Example: WorldPersistenceSystem
// This script demonstrates the WorldPersistenceSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **World Persistence System** design pattern in Unity. This pattern centralizes the saving and loading of game state, decoupling persistent objects from the persistence mechanism itself.

**Key Components & How it Works:**

1.  **`WorldPersistenceSystem` (Singleton MonoBehaviour):**
    *   The central hub for all persistence operations.
    *   Implemented as a Singleton (`Instance`) for easy global access.
    *   Manages a collection of all objects that implement the `IPersistent` interface.
    *   Provides `SaveGame()` and `LoadGame()` methods.
    *   Handles file I/O (using `JsonUtility` for serialization).
    *   Can manage scene transitions (loading a saved scene) and dynamic object instantiation upon loading.
    *   Generates unique IDs for new persistent objects.

2.  **`IPersistent` Interface:**
    *   Defines the contract for any `MonoBehaviour` that wants its state saved and loaded.
    *   Methods: `GetPersistentID()`, `SaveData()`, `LoadData()`, `GetGameObject()`, `ShouldInstantiateFromPrefab()`, `GetPrefabPath()`.
    *   This decouples the "what to save" from the "how to save."

3.  **`GameObjectPersistentData` (Serializable Class):**
    *   A generic data container for an individual persistent `GameObject`.
    *   Stores common data like `persistentID`, `position`, `rotation`, `scale`, and `isActive`.
    *   Includes `specificComponentJson` to hold type-specific data (e.g., player health, item status) as a JSON string, allowing individual components to define their own data structure.
    *   Includes `prefabPath` for dynamic instantiation.

4.  **`GameSaveData` (Serializable Class):**
    *   The top-level container that holds the entire game's save state.
    *   Contains a list of `GameObjectPersistentData` for all persistent objects.
    *   Can also hold global game state (e.g., `currentSceneName`, `playerScore`).

5.  **`PersistentObject` (Abstract Base Class):**
    *   A `MonoBehaviour` base class that implements the `IPersistent` interface partially.
    *   Provides common persistence logic, like registering/unregistering with `WorldPersistenceSystem` in `OnEnable`/`OnDestroy`.
    *   Has a `persistentID` field (crucial for re-identifying objects across save/load).
    *   Includes fields for prefab path and whether it should be instantiated.
    *   `PopulateGameObjectData()` helper for common `GameObject` data.
    *   Derived classes must implement `SaveData()` and `LoadData()`.

6.  **Concrete `IPersistent` Implementations (e.g., `PersistentPlayer`, `PersistentCollectible`):**
    *   Derived from `PersistentObject`.
    *   Override `SaveData()` to collect their specific state into a `[System.Serializable]` POCO (Plain Old C# Object) and serialize it to JSON for `specificComponentJson`.
    *   Override `LoadData()` to deserialize `specificComponentJson` and apply the loaded state.

7.  **Editor Utility (Optional but Recommended):**
    *   A custom editor for `PersistentObject` (and its derived classes) to easily generate unique `persistentID`s in the Inspector.

---

### **How to Use This in Your Unity Project:**

1.  **Create a C# Script:** Create a new C# script in your Unity project named `WorldPersistenceSystem.cs`.
2.  **Copy and Paste:** Copy the entire code block below and paste it into your `WorldPersistenceSystem.cs` file.
3.  **Create Prefab Folder (Optional but Recommended):** Create a `Resources` folder in your project (e.g., `Assets/Resources`). Inside it, you might have `Assets/Resources/Prefabs`.
4.  **Setup WorldPersistenceSystem GameObject:**
    *   Create an empty GameObject in your scene (e.g., named "GameManager").
    *   Attach the `WorldPersistenceSystem` script to it.
    *   **Drag any prefabs** you intend to dynamically spawn and persist into the `Persistent Prefabs` array in the Inspector of the `WorldPersistenceSystem` component.
5.  **Make Your Game Objects Persistent:**
    *   For any `GameObject` you want to save/load, attach a script that derives from `PersistentObject` (e.g., `PersistentPlayer`, `PersistentCollectible`).
    *   **Crucially:** Assign a **unique** `Persistent ID` to each instance of your `PersistentObject` in the Inspector. You can use the "Generate New Persistent ID" button provided by the custom editor.
    *   If a persistent object can be dynamically instantiated (e.g., an enemy that spawns), set `Should Instantiate From Prefab` to `true` and provide the `Prefab Path` (e.g., "Prefabs/EnemyPrefab" if your prefab is at `Assets/Resources/Prefabs/EnemyPrefab.prefab`, or just the prefab name if it's in the `Persistent Prefabs` array of the `WorldPersistenceSystem`).
6.  **Trigger Save/Load:**
    *   Call `WorldPersistenceSystem.Instance.SaveGame()` when you want to save.
    *   Call `WorldPersistenceSystem.Instance.LoadGame()` when you want to load.
    *   You might do this from UI buttons, a game manager, or on scene transitions.
7.  **Run Your Game:** Observe how objects' positions, states, and specific data are saved and restored across game sessions.

---

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO; // For file operations
using System.Linq; // For LINQ queries

#if UNITY_EDITOR
using UnityEditor; // Required for the custom editor script
#endif

// --- 1. IPersistent Interface ---
#region IPersistent Interface
/// <summary>
/// The contract for any object that wants its state to be saved and loaded
/// by the WorldPersistenceSystem.
/// </summary>
public interface IPersistent
{
    /// <summary>
    /// Returns a unique identifier for this persistent object.
    /// This ID must be unique across all persistent objects in the entire game world.
    /// </summary>
    string GetPersistentID();

    /// <summary>
    /// Returns the GameObject associated with this persistent component.
    /// </summary>
    GameObject GetGameObject();

    /// <summary>
    /// Gathers all relevant data from the object and returns it in a serializable format.
    /// </summary>
    GameObjectPersistentData SaveData();

    /// <summary>
    /// Applies the provided data to restore the object's state.
    /// </summary>
    /// <param name="data">The data to load.</param>
    void LoadData(GameObjectPersistentData data);

    /// <summary>
    /// Indicates whether this object should be instantiated from a prefab if it's not found
    /// in the scene during a load operation. Typically true for dynamically spawned objects.
    /// </summary>
    bool ShouldInstantiateFromPrefab();

    /// <summary>
    /// Returns the path to the prefab (e.g., "Prefabs/PlayerPrefab") if
    /// <see cref="ShouldInstantiateFromPrefab"/> is true.
    /// This path is used to load the prefab from the Resources folder or lookup in persistent prefabs list.
    /// </summary>
    string GetPrefabPath();
}
#endregion

// --- 2. Data Structures for Persistence ---
#region Persistence Data Structures
/// <summary>
/// Top-level data structure holding the entire game's save state.
/// </summary>
[System.Serializable]
public class GameSaveData
{
    public string saveID; // A unique identifier for this save (e.g., timestamp)
    public List<GameObjectPersistentData> persistentObjectsData = new List<GameObjectPersistentData>();
    public string currentSceneName; // The name of the scene that was active when saved.

    // Add any global game state here (e.g., player score, game time, weather state)
    public int playerScore;
    public float gameTimeElapsed;
    // ... potentially other global manager data
}

/// <summary>
/// Generic data structure for storing the persistent state of a single GameObject.
/// </summary>
[System.Serializable] // Use System.Serializable for JsonUtility to work
public class GameObjectPersistentData
{
    public string persistentID; // Unique ID to identify the object
    public string prefabPath;   // Asset path or lookup name for prefab (if it needs to be instantiated)
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public bool isActive;       // Whether the object was active in the saved game
    public string specificComponentJson; // JSON string for specific component data (e.g., player health, item collected state)
    public string objectTypeTag; // A tag to help identify object type during loading/debugging (e.g., "Player", "Collectible")
}

/// <summary>
/// Specific data structure for Player component state (example).
/// </summary>
[System.Serializable]
public class PlayerSpecificData
{
    public int health;
    public int score;
    public List<string> inventoryItemIDs = new List<string>(); // Example inventory
}

/// <summary>
/// Specific data structure for Collectible component state (example).
/// </summary>
[System.Serializable]
public class CollectibleSpecificData
{
    public string itemType; // e.g., "Coin", "HealthPotion"
    public int value;      // e.g., 10 for coin, 25 for health
}
#endregion

// --- 3. WorldPersistenceSystem (Singleton Manager) ---
#region WorldPersistenceSystem
/// <summary>
/// WorldPersistenceSystem: A central manager for saving and loading the game world's state.
/// This system implements the World Persistence System design pattern, providing a single point
/// of control for managing the persistent state of various game objects.
/// </summary>
/// <remarks>
/// Key features:
/// - Singleton pattern for easy global access.
/// - Manages registration and unregistration of IPersistent objects.
/// - Handles serialization and deserialization of game data to a file (JSON).
/// - Provides methods to save and load the entire game state.
/// - Supports both scene-placed and dynamically spawned persistent objects.
/// - Demonstrates unique ID generation and mapping.
/// </remarks>
public class WorldPersistenceSystem : MonoBehaviour
{
    // --- Singleton Setup ---
    public static WorldPersistenceSystem Instance { get; private set; }

    [Header("Persistence Settings")]
    [Tooltip("The name of the save file (e.g., 'gamesave.json').")]
    public string saveFileName = "gamesave.json";

    [Tooltip("List of prefabs that can be dynamically spawned and persisted. " +
             "These prefabs must have an IPersistent component.")]
    public GameObject[] persistentPrefabs;

    private Dictionary<string, GameObject> _prefabLookup; // For quick lookup of prefabs by name/path

    // --- Internal State ---
    // A dictionary to keep track of all currently registered persistent objects in the scene.
    // Key: PersistentID, Value: IPersistent object reference
    private Dictionary<string, IPersistent> _registeredPersistentObjects = new Dictionary<string, IPersistent>();

    // --- Unity Lifecycle ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WorldPersistenceSystem: Duplicate instance found. Destroying new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Ensure the system persists across scene loads

        InitializePrefabLookup();
        Debug.Log("WorldPersistenceSystem initialized and ready for persistence operations.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public Registration Methods ---

    /// <summary>
    /// Registers an IPersistent object with the system.
    /// This should be called by IPersistent objects in their OnEnable or Awake method.
    /// </summary>
    /// <param name="persistentObject">The object implementing IPersistent.</param>
    public void RegisterPersistentObject(IPersistent persistentObject)
    {
        string id = persistentObject.GetPersistentID();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError($"WorldPersistenceSystem: Persistent object {persistentObject.GetGameObject().name} has no PersistentID. It will not be saved.", persistentObject.GetGameObject());
            return;
        }

        if (_registeredPersistentObjects.ContainsKey(id))
        {
            // If the same object is being re-registered (e.g., OnEnable called multiple times),
            // we just ensure the reference is up-to-date.
            if (_registeredPersistentObjects[id].GetGameObject() != persistentObject.GetGameObject())
            {
                // Critical error: Two different objects have the same PersistentID!
                Debug.LogError($"WorldPersistenceSystem: Duplicate PersistentID '{id}' found for objects '{_registeredPersistentObjects[id].GetGameObject().name}' and '{persistentObject.GetGameObject().name}'. Please ensure unique IDs!", persistentObject.GetGameObject());
            }
        }
        else
        {
            _registeredPersistentObjects.Add(id, persistentObject);
            // Debug.Log($"Registered persistent object: {id} ({persistentObject.GetGameObject().name})");
        }
    }

    /// <summary>
    /// Unregisters an IPersistent object from the system.
    /// This should be called by IPersistent objects in their OnDestroy method.
    /// (Not OnDisable, as an object might just be deactivated, not removed from the world).
    /// </summary>
    /// <param name="persistentObject">The object implementing IPersistent.</param>
    public void UnregisterPersistentObject(IPersistent persistentObject)
    {
        string id = persistentObject.GetPersistentID();
        if (_registeredPersistentObjects.ContainsKey(id))
        {
            _registeredPersistentObjects.Remove(id);
            // Debug.Log($"Unregistered persistent object: {id} ({persistentObject.GetGameObject().name})");
        }
    }

    // --- Save/Load Functionality ---

    /// <summary>
    /// Saves the current game state to a file.
    /// Collects data from all registered IPersistent objects and serializes it.
    /// </summary>
    public void SaveGame()
    {
        GameSaveData saveData = new GameSaveData
        {
            saveID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss"), // Unique ID for this save
            currentSceneName = SceneManager.GetActiveScene().name,
            // Example: Collect global game state (if GameManager or similar exists)
            // playerScore = GameManager.Instance.Score,
            // gameTimeElapsed = Time.timeSinceLevelLoad, // For demonstration, in real game update a dedicated timer
        };

        foreach (var entry in _registeredPersistentObjects)
        {
            IPersistent persistentObject = entry.Value;
            if (persistentObject != null && persistentObject.GetGameObject() != null)
            {
                saveData.persistentObjectsData.Add(persistentObject.SaveData());
            }
        }

        string json = JsonUtility.ToJson(saveData, true); // true for pretty printing JSON
        string filePath = GetSaveFilePath();

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Game saved successfully to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game to {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Loads a game state from a file.
    /// Deserializes data and applies it to existing and newly spawned persistent objects.
    /// This method handles loading objects that are in the current scene or objects that need to be instantiated.
    /// </summary>
    public void LoadGame()
    {
        string filePath = GetSaveFilePath();

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file found at: {filePath}. Cannot load game.");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("Failed to deserialize save data. Save file might be corrupted.");
                return;
            }

            Debug.Log($"Loading game from: {filePath} (Save ID: {saveData.saveID})");

            // --- Step 1: Handle Scene Loading if necessary ---
            // If the saved scene is different from the current one, load it first.
            if (SceneManager.GetActiveScene().name != saveData.currentSceneName)
            {
                Debug.Log($"Switching to scene '{saveData.currentSceneName}' before applying game state...");
                StartCoroutine(LoadSceneAndApplyState(saveData.currentSceneName, saveData));
                return; // The rest of the loading will happen after the scene is loaded
            }

            // If already in the correct scene, apply the state directly
            ApplyLoadedState(saveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game from {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Coroutine to load a scene asynchronously and then apply the game state.
    /// </summary>
    private IEnumerator LoadSceneAndApplyState(string sceneName, GameSaveData saveData)
    {
        // Clear registered objects before scene unload to prevent stale references
        // All objects in the current scene will be destroyed.
        _registeredPersistentObjects.Clear();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the new scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Now that the new scene is loaded and its IPersistent objects have
        // registered themselves (in their Awake/OnEnable), we can apply the saved state.
        ApplyLoadedState(saveData);
    }

    /// <summary>
    /// Applies the loaded GameSaveData to the currently registered objects and instantiates missing ones.
    /// </summary>
    /// <param name="saveData">The deserialized GameSaveData.</param>
    private void ApplyLoadedState(GameSaveData saveData)
    {
        // Example: Apply global game state
        // GameManager.Instance.Score = saveData.playerScore;
        // Update game timers/managers with saveData.gameTimeElapsed;

        // Create a temporary dictionary for quick lookup of loaded data by ID
        Dictionary<string, GameObjectPersistentData> loadedDataLookup = saveData.persistentObjectsData.ToDictionary(d => d.persistentID);

        // --- Step 2: Apply data to existing IPersistent objects in the current scene ---
        // Iterate through objects currently registered (meaning they exist in the scene)
        List<string> handledIDs = new List<string>(); // Keep track of IDs processed
        foreach (var entry in _registeredPersistentObjects.Values.ToList()) // ToList() to allow safe modification during iteration
        {
            string id = entry.GetPersistentID();
            if (loadedDataLookup.TryGetValue(id, out GameObjectPersistentData data))
            {
                // Apply common GameObject data (position, rotation, scale, active state)
                entry.GetGameObject().transform.position = data.position;
                entry.GetGameObject().transform.rotation = data.rotation;
                entry.GetGameObject().transform.localScale = data.scale;
                entry.GetGameObject().SetActive(data.isActive); // Set active state before loading component data

                entry.LoadData(data); // Call component-specific LoadData
                handledIDs.Add(id);
                // Debug.Log($"Loaded data for existing object: {id} ({entry.GetGameObject().name})");
            }
            else
            {
                // Object exists in the current scene but was NOT in the save data.
                // This could mean it was new, or destroyed in the previous save.
                // Decision: For this example, we'll deactivate objects not in the save,
                // assuming they were destroyed in the previous game session.
                // You might choose to destroy them instead, or leave them if they represent new content.
                Debug.Log($"Persistent object '{id}' ({entry.GetGameObject().name}) in scene but not in save data. Deactivating it.");
                entry.GetGameObject().SetActive(false);
            }
        }

        // --- Step 3: Instantiate missing IPersistent objects from prefabs ---
        // Iterate through loaded data to find objects that were not found in the current scene
        foreach (GameObjectPersistentData data in saveData.persistentObjectsData)
        {
            if (!handledIDs.Contains(data.persistentID)) // If this object's data hasn't been applied yet
            {
                // This means the object was not found in the scene, and potentially needs to be spawned.
                if (data.prefabPath != null) // Check if prefabPath was saved
                {
                    GameObject prefab = GetPersistentPrefab(data.prefabPath);
                    if (prefab != null)
                    {
                        GameObject instance = Instantiate(prefab, data.position, data.rotation);
                        IPersistent persistentInstance = instance.GetComponent<IPersistent>();

                        if (persistentInstance != null)
                        {
                            // After instantiation, the new object's Awake/OnEnable will register itself automatically.
                            // We now apply its specific data and common GameObject transform/active state.
                            instance.transform.localScale = data.scale; // Scale not always handled by LoadData
                            instance.SetActive(data.isActive); // Set active state first

                            persistentInstance.LoadData(data); // Apply component-specific data
                            // Debug.Log($"Instantiated and loaded object: {data.persistentID} ({instance.name}) from prefab {data.prefabPath}");
                        }
                        else
                        {
                            Debug.LogError($"Instantiated prefab '{data.prefabPath}' for ID '{data.persistentID}' but it lacks an IPersistent component. Persistence will not work for it.");
                            Destroy(instance); // Clean up
                        }
                    }
                    else
                    {
                        Debug.LogError($"Could not find prefab at path '{data.prefabPath}' for persistent ID '{data.persistentID}'. Object cannot be instantiated.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Persistent data for ID '{data.persistentID}' has no prefab path and no matching object in scene. Skipping instantiation.");
                }
            }
        }
        Debug.Log("Game state applied successfully.");
    }

    /// <summary>
    /// Gets the full path to the save file.
    /// Uses Application.persistentDataPath, which is platform-independent.
    /// </summary>
    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    /// <summary>
    /// Initializes a lookup dictionary for persistent prefabs.
    /// This allows us to find a prefab by its name or a specific identifier string.
    /// </summary>
    private void InitializePrefabLookup()
    {
        _prefabLookup = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in persistentPrefabs)
        {
            if (prefab != null)
            {
                // Use the prefab's name as the key. For robust systems, a custom ID component on prefabs might be better.
                if (_prefabLookup.ContainsKey(prefab.name))
                {
                    Debug.LogWarning($"Duplicate prefab name '{prefab.name}' in 'Persistent Prefabs' list. Only the first will be used for lookup.");
                    continue;
                }
                _prefabLookup.Add(prefab.name, prefab);
            }
        }
    }

    /// <summary>
    /// Retrieves a persistent prefab by its path/name.
    /// It first checks the `persistentPrefabs` list by name, then attempts to load from `Resources`.
    /// </summary>
    /// <param name="pathOrName">The name of the prefab (for lookup in `persistentPrefabs`)
    /// or its path within a `Resources` folder (e.g., "Prefabs/PlayerPrefab").</param>
    /// <returns>The GameObject prefab, or null if not found or if it lacks an IPersistent component.</returns>
    private GameObject GetPersistentPrefab(string pathOrName)
    {
        GameObject prefab = null;

        // 1. Try lookup in the editor-assigned `persistentPrefabs` list
        if (_prefabLookup.TryGetValue(pathOrName, out prefab))
        {
            if (prefab.GetComponent<IPersistent>() == null)
            {
                Debug.LogError($"Prefab '{pathOrName}' found in `persistentPrefabs` list but lacks an IPersistent component.");
                return null;
            }
            return prefab;
        }

        // 2. Fallback: Try loading from Resources (e.g., "Assets/Resources/Prefabs/PlayerPrefab.prefab" -> "Prefabs/PlayerPrefab")
        prefab = Resources.Load<GameObject>(pathOrName);
        if (prefab != null)
        {
            if (prefab.GetComponent<IPersistent>() == null)
            {
                Debug.LogError($"Prefab '{pathOrName}' loaded from Resources but does not have an IPersistent component. It cannot be used for persistence.");
                return null;
            }
            return prefab;
        }

        Debug.LogError($"Prefab '{pathOrName}' not found in `persistentPrefabs` list or in `Resources` folder. Check the prefab path/name and the `WorldPersistenceSystem` setup.");
        return null;
    }


    /// <summary>
    /// Generates a unique ID for new persistent objects at runtime or editor-time.
    /// Uses GUID for robustness.
    /// </summary>
    /// <returns>A new unique ID string.</returns>
    public string GenerateNewPersistentID()
    {
        return System.Guid.NewGuid().ToString();
    }
}
#endregion

// --- 4. PersistentObject (Base Class for IPersistent MonoBehaviours) ---
#region PersistentObject Base Class
/// <summary>
/// Abstract base class for MonoBehaviour components that implement IPersistent.
/// Handles common registration/unregistration logic and provides fields for ID and prefab info.
/// </summary>
public abstract class PersistentObject : MonoBehaviour, IPersistent
{
    [Tooltip("A unique identifier for this object within the game world. " +
             "MUST be unique across all persistent objects. Use the button below to generate.")]
    [SerializeField]
    protected string persistentID = ""; // Initialized empty; should be set by user or editor script

    [Tooltip("If true, this object can be dynamically instantiated from a prefab during loading " +
             "if it doesn't exist in the scene. Typically true for spawned objects, false for scene-placed.")]
    [SerializeField]
    protected bool _shouldInstantiateFromPrefab = false;

    [Tooltip("The path to the prefab in a Resources folder (e.g., 'Prefabs/Player') " +
             "or the name of a prefab in the WorldPersistenceSystem's 'Persistent Prefabs' list. " +
             "Required if 'Should Instantiate From Prefab' is true.")]
    [SerializeField]
    protected string _prefabPath; // Path relative to Resources folder or just the prefab name for lookup


    protected virtual void OnEnable()
    {
        // Register with the persistence system when the object becomes active.
        // WorldPersistenceSystem.Instance might be null if this object wakes up before the singleton.
        // It's safer to register in Start or ensure execution order. For this example, a null check is fine.
        WorldPersistenceSystem.Instance?.RegisterPersistentObject(this);
    }

    protected virtual void OnDestroy()
    {
        // Only unregister if the WorldPersistenceSystem still exists.
        // During application quit, singletons might be destroyed in arbitrary order,
        // so WorldPersistenceSystem.Instance could already be null.
        if (WorldPersistenceSystem.Instance != null)
        {
            WorldPersistenceSystem.Instance.UnregisterPersistentObject(this);
        }
    }

    public string GetPersistentID() => persistentID;
    public GameObject GetGameObject() => gameObject;
    public bool ShouldInstantiateFromPrefab() => _shouldInstantiateFromPrefab;
    public string GetPrefabPath() => _prefabPath;

    /// <summary>
    /// Abstract method to save specific component data.
    /// Concrete implementations must override this to serialize their unique state.
    /// </summary>
    public abstract GameObjectPersistentData SaveData();

    /// <summary>
    /// Abstract method to load specific component data.
    /// Concrete implementations must override this to deserialize and apply their unique state.
    /// </summary>
    public abstract void LoadData(GameObjectPersistentData data);

    /// <summary>
    /// Helper method to populate common GameObject data into the persistent data structure.
    /// Derived classes can call this to create the base `GameObjectPersistentData` object.
    /// </summary>
    /// <param name="objectTypeTag">A string tag to identify the type of object (e.g., "Player", "Enemy").</param>
    /// <param name="specificComponentJson">A JSON string containing component-specific data.</param>
    protected GameObjectPersistentData PopulateGameObjectData(string objectTypeTag, string specificComponentJson)
    {
        return new GameObjectPersistentData
        {
            persistentID = GetPersistentID(),
            prefabPath = ShouldInstantiateFromPrefab() ? GetPrefabPath() : null, // Only store prefab path if needed
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            isActive = gameObject.activeSelf, // Save active state
            specificComponentJson = specificComponentJson,
            objectTypeTag = objectTypeTag
        };
    }
}
#endregion

// --- 5. Concrete IPersistent Implementations (Examples) ---
#region Example Persistent Objects
/// <summary>
/// Example of a persistent player character.
/// Saves and loads its transform, health, and score.
/// </summary>
public class PersistentPlayer : PersistentObject
{
    [Header("Player Specific Data")]
    public int health = 100;
    public int score = 0;
    public List<string> inventoryItemIDs = new List<string>(); // Simple example inventory

    void Start()
    {
        // Example: If player gets health/score updates, these would happen here.
        // For demonstration, just log current values.
        Debug.Log($"<color=cyan>Player initialized:</color> ID={persistentID}, Health={health}, Score={score}, InventoryCount={inventoryItemIDs.Count}");
    }

    // Example methods to change player state
    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health < 0) health = 0;
        Debug.Log($"<color=orange>Player {persistentID} took {amount} damage. Health: {health}</color>");
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log($"<color=green>Player {persistentID} gained {amount} score. Total Score: {score}</color>");
    }

    public void AddItemToInventory(string itemID)
    {
        inventoryItemIDs.Add(itemID);
        Debug.Log($"<color=yellow>Player {persistentID} picked up item: {itemID}. Inventory count: {inventoryItemIDs.Count}</color>");
    }

    /// <summary>
    /// Saves the player's specific state (health, score, inventory) and common GameObject data.
    /// </summary>
    public override GameObjectPersistentData SaveData()
    {
        PlayerSpecificData playerSpecificData = new PlayerSpecificData
        {
            health = this.health,
            score = this.score,
            inventoryItemIDs = new List<string>(this.inventoryItemIDs) // Create a new list for deep copy
        };

        string jsonSpecific = JsonUtility.ToJson(playerSpecificData);
        return PopulateGameObjectData("Player", jsonSpecific);
    }

    /// <summary>
    /// Loads the player's specific state (health, score, inventory) and common GameObject data.
    /// </summary>
    public override void LoadData(GameObjectPersistentData data)
    {
        // Common GameObject data (position, rotation, scale, active state) is handled by WorldPersistenceSystem.ApplyLoadedState
        // We only need to apply our component-specific data here.

        if (!string.IsNullOrEmpty(data.specificComponentJson))
        {
            PlayerSpecificData playerSpecificData = JsonUtility.FromJson<PlayerSpecificData>(data.specificComponentJson);
            this.health = playerSpecificData.health;
            this.score = playerSpecificData.score;
            this.inventoryItemIDs = new List<string>(playerSpecificData.inventoryItemIDs); // Deep copy
        }

        Debug.Log($"<color=cyan>Player {persistentID} loaded:</color> Health={health}, Score={score}, InventoryCount={inventoryItemIDs.Count}");
    }
}

/// <summary>
/// Example of a persistent collectible item (e.g., a coin, a health potion).
/// Saves and loads its transform and whether it has been collected (isActive).
/// </summary>
public class PersistentCollectible : PersistentObject
{
    [Header("Collectible Specific Data")]
    public string itemType = "Coin";
    public int value = 1;

    void Start()
    {
        Debug.Log($"<color=magenta>Collectible {persistentID} initialized:</color> Type={itemType}, Value={value}, Active={gameObject.activeSelf}");
    }

    /// <summary>
    /// Simulates collecting the item.
    /// Deactivates the GameObject, which will be saved as 'isActive = false'.
    /// </summary>
    public void Collect()
    {
        if (gameObject.activeSelf) // Only collect if active
        {
            Debug.Log($"<color=green>Collectible {persistentID} ({itemType}) collected!</color>");
            gameObject.SetActive(false); // Deactivating will be saved as isActive=false

            // Optionally, notify player or score manager
            FindObjectOfType<PersistentPlayer>()?.AddScore(value);
        }
    }

    /// <summary>
    /// Saves the collectible's specific state (itemType, value) and common GameObject data.
    /// </summary>
    public override GameObjectPersistentData SaveData()
    {
        CollectibleSpecificData collectibleSpecificData = new CollectibleSpecificData
        {
            itemType = this.itemType,
            value = this.value
        };

        string jsonSpecific = JsonUtility.ToJson(collectibleSpecificData);
        return PopulateGameObjectData("Collectible", jsonSpecific);
    }

    /// <summary>
    /// Loads the collectible's specific state (itemType, value) and common GameObject data.
    /// </summary>
    public override void LoadData(GameObjectPersistentData data)
    {
        // Common GameObject data is handled by WorldPersistenceSystem.ApplyLoadedState
        // We only need to apply our component-specific data here.

        if (!string.IsNullOrEmpty(data.specificComponentJson))
        {
            CollectibleSpecificData collectibleSpecificData = JsonUtility.FromJson<CollectibleSpecificData>(data.specificComponentJson);
            this.itemType = collectibleSpecificData.itemType;
            this.value = collectibleSpecificData.value;
        }

        Debug.Log($"<color=magenta>Collectible {persistentID} loaded:</color> Type={itemType}, Value={value}, Active={gameObject.activeSelf}");
    }
}
#endregion

// --- 6. Editor Utility (Optional but Recommended) ---
#region Editor Utilities
#if UNITY_EDITOR
/// <summary>
/// Custom Editor for PersistentObject to add a "Generate New Persistent ID" button.
/// This makes assigning unique IDs easier in the Unity Inspector.
/// </summary>
[CustomEditor(typeof(PersistentObject), true)] // true to apply to derived classes
public class PersistentObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default fields first

        PersistentObject myTarget = (PersistentObject)target;

        // Find the 'persistentID' field using SerializedProperty
        SerializedProperty persistentIDProp = serializedObject.FindProperty("persistentID");

        // Display a button to generate a new GUID
        if (GUILayout.Button("Generate New Persistent ID"))
        {
            persistentIDProp.stringValue = WorldPersistenceSystem.Instance?.GenerateNewPersistentID() ?? System.Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties(); // Apply changes
            EditorUtility.SetDirty(myTarget); // Mark as dirty to ensure changes are saved
        }

        // Small warning for empty IDs
        if (string.IsNullOrEmpty(persistentIDProp.stringValue))
        {
            EditorGUILayout.HelpBox("Persistent ID is empty! Please generate a new ID to ensure this object can be saved/loaded correctly.", MessageType.Warning);
        }

        // Display a warning if prefabPath is required but missing
        SerializedProperty shouldInstantiateProp = serializedObject.FindProperty("_shouldInstantiateFromPrefab");
        SerializedProperty prefabPathProp = serializedObject.FindProperty("_prefabPath");

        if (shouldInstantiateProp.boolValue && string.IsNullOrEmpty(prefabPathProp.stringValue))
        {
            EditorGUILayout.HelpBox(" 'Should Instantiate From Prefab' is true, but 'Prefab Path' is empty. This object cannot be instantiated upon loading.", MessageType.Error);
        }
    }
}
#endif
#endregion

```