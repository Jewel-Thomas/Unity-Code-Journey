// Unity Design Pattern Example: PersistentWorldSystem
// This script demonstrates the PersistentWorldSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Persistent World System design pattern in Unity focuses on creating a game world whose state can be saved and loaded, allowing players to pick up exactly where they left off. This involves identifying savable entities, collecting their relevant data, serializing that data to storage, and then deserializing and applying it back to the entities when the game loads.

This example provides a robust, commented C# script that demonstrates this pattern. It includes:
1.  **`PersistentWorldSystem`**: A central manager responsible for orchestrating the saving and loading process.
2.  **`ISavable` Interface**: Defines the contract for any `MonoBehaviour` that wishes to have its state saved and loaded.
3.  **`SavableObjectData`**: A serializable struct that holds the core data for a single savable object (position, rotation, active state, and a custom data string).
4.  **`PersistentWorldData`**: A serializable class that acts as the container for all `SavableObjectData` objects, representing the entire world state.
5.  **`ExampleSavableObject`**: A concrete example of a `MonoBehaviour` that implements `ISavable`, demonstrating how to save and load its own specific state.

---

### `PersistentWorldSystem.cs`

This single file contains the entire system, ready to be dropped into a Unity project.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq; // For easier data manipulation like ToDictionary
using UnityEngine.SceneManagement; // To scan the current active scene

// =====================================================================================
// PERSISTENT WORLD SYSTEM DESIGN PATTERN
// =====================================================================================
//
// Overview:
// The Persistent World System pattern aims to save and load the state of various objects
// in your game world, ensuring that when a player returns, the world is as they left it.
//
// Key Components:
// 1. ISavable Interface: Defines what an object needs to do to be savable.
//    - GetSavableID(): Provides a unique identifier for the object.
//    - GetSavableData(): Returns a serializable data object representing its state.
//    - ApplySavableData(SavableObjectData data): Applies the loaded state to the object.
//
// 2. SavableObjectData: A serializable data structure that holds the state of a single ISavable object.
//    It should include common properties (like transform) and a way to store custom data.
//
// 3. PersistentWorldData: A serializable container for all SavableObjectData objects,
//    representing the entire saved world state.
//
// 4. PersistentWorldSystem (Manager): A central singleton that:
//    - Scans the scene for all ISavable objects.
//    - Maintains a registry (ID -> ISavable object).
//    - Orchestrates the saving process (collects all data, serializes to file).
//    - Orchestrates the loading process (deserializes file, applies data to registered objects).
//
// How it Works:
// 1. Initialization: The PersistentWorldSystem scans the current scene for all objects
//    that implement the ISavable interface and registers them using their unique ID.
//    Objects that need to be savable MUST have a unique ID that persists across game sessions.
//    For scene objects, this ID is typically generated once and stored in a [SerializeField] field.
//    For dynamically spawned objects, they would generate an ID and register themselves upon creation.
//
// 2. Saving: When `SaveWorld()` is called:
//    - The manager iterates through all registered ISavable objects.
//    - For each, it calls `GetSavableData()` to retrieve their current state.
//    - All collected `SavableObjectData` are put into a `PersistentWorldData` container.
//    - The `PersistentWorldData` container is then serialized (e.g., to JSON) and saved to a file
//      (typically in `Application.persistentDataPath`).
//
// 3. Loading: When `LoadWorld()` is called:
//    - The manager reads the save file and deserializes it into a `PersistentWorldData` object.
//    - It then iterates through all currently registered ISavable objects in the scene.
//    - For each registered object, it looks up its ID in the loaded `PersistentWorldData`.
//    - If found, it calls `ApplySavableData()` on the object, passing the loaded state data.
//    - Crucially, this system needs a strategy for objects that were saved but are no longer
//      in the scene (e.g., destroyed dynamic objects) or objects that need to be dynamically
//      spawned based on save data (not fully covered in this minimal example, but key for robust systems).
//      This example focuses on applying state to *existing* scene objects.
//
// Best Practices:
// - Use `Application.persistentDataPath` for save files.
// - Use `JsonUtility` for serialization for its simplicity and human readability.
// - Ensure IDs are truly unique and persistent. Guids are great for this.
// - Handle error cases (file not found, deserialization errors).
// - Be mindful of performance for very large worlds with many savable objects.
//
// =====================================================================================

/// <summary>
/// Interface for any object that wants to be part of the Persistent World System.
/// </summary>
public interface ISavable
{
    // A unique identifier for this savable object. Must be persistent across sessions.
    string GetSavableID();

    // Collects the current state of the object into a SavableObjectData struct.
    SavableObjectData GetSavableData();

    // Applies the given state data to the object.
    void ApplySavableData(SavableObjectData data);

    // Called when the savable object is initialized or becomes active.
    void RegisterSavable();

    // Called when the savable object is deactivated or destroyed.
    void DeregisterSavable();
}

/// <summary>
/// Serializable struct to hold common data for any savable object.
/// Custom data can be stored as a JSON string within CustomDataJson.
/// </summary>
[System.Serializable]
public struct SavableObjectData
{
    public string Id;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool IsActive;
    public string CustomDataJson; // For component-specific data (e.g., health, inventory)
}

/// <summary>
/// Top-level serializable class that holds all savable object data for the entire world.
/// This is what gets saved to and loaded from the file.
/// </summary>
[System.Serializable]
public class PersistentWorldData
{
    public List<SavableObjectData> SavableObjects;

    public PersistentWorldData()
    {
        SavableObjects = new List<SavableObjectData>();
    }
}

/// <summary>
/// The main manager for the Persistent World System.
/// This is a singleton MonoBehaviour that orchestrates saving and loading.
/// </summary>
public class PersistentWorldSystem : MonoBehaviour
{
    // Singleton instance for easy access from other scripts.
    public static PersistentWorldSystem Instance { get; private set; }

    [Header("System Settings")]
    [Tooltip("The filename for the save data, stored in Application.persistentDataPath.")]
    [SerializeField] private string _saveFileName = "world_save.json";

    // Internal dictionary to keep track of all ISavable objects in the current scene.
    // Key: Savable ID, Value: ISavable instance.
    private Dictionary<string, ISavable> _registeredSavables = new Dictionary<string, ISavable>();

    // Full path to the save file.
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, _saveFileName);

    void Awake()
    {
        // Implement singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PersistentWorldSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the manager alive across scene loads.
        }
    }

    void Start()
    {
        // Initial scan for savable objects in the currently active scene.
        ScanForSavablesInActiveScene();

        // Optional: Automatically load on start if a save file exists.
        // Uncomment the line below if you want this behavior.
        // LoadWorld(); 
    }

    /// <summary>
    /// Registers an ISavable object with the system.
    /// Called by ISavable objects themselves (e.g., in their OnEnable).
    /// </summary>
    /// <param name="savable">The ISavable component to register.</param>
    public void RegisterSavable(ISavable savable)
    {
        string id = savable.GetSavableID();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError($"ISavable object {((MonoBehaviour)savable).name} attempted to register with an empty or null ID. It will not be saved/loaded.", (MonoBehaviour)savable);
            return;
        }

        if (_registeredSavables.ContainsKey(id))
        {
            // This can happen if an object is duplicated or IDs are not truly unique.
            // Or if an object is enabled/disabled multiple times without proper deregistration.
            Debug.LogWarning($"Duplicate Savable ID '{id}' found for object '{((MonoBehaviour)savable).name}'. " +
                             $"Existing object: '{((MonoBehaviour)_registeredSavables[id]).name}'. " +
                             $"The new object will override the old one if it's a different instance, or this is a harmless re-registration.", (MonoBehaviour)savable);
            // Optionally, we could prevent re-registration or force an error here.
            // For now, allow re-registration to update the reference if it's the same object.
            _registeredSavables[id] = savable; 
        }
        else
        {
            _registeredSavables.Add(id, savable);
            // Debug.Log($"Registered savable: {id} ({((MonoBehaviour)savable).name})");
        }
    }

    /// <summary>
    /// Deregisters an ISavable object from the system.
    /// Called by ISavable objects themselves (e.g., in their OnDisable or OnDestroy).
    /// </summary>
    /// <param name="savable">The ISavable component to deregister.</param>
    public void DeregisterSavable(ISavable savable)
    {
        string id = savable.GetSavableID();
        if (_registeredSavables.ContainsKey(id))
        {
            _registeredSavables.Remove(id);
            // Debug.Log($"Deregistered savable: {id} ({((MonoBehaviour)savable).name})");
        }
    }

    /// <summary>
    /// Scans the currently active scene for all MonoBehaviour components that implement ISavable
    /// and registers them with the system.
    /// </summary>
    public void ScanForSavablesInActiveScene()
    {
        // Clear existing registrations to ensure we only have objects from the current scene
        // or to handle scenarios where objects might have been destroyed/recreated.
        _registeredSavables.Clear(); 
        
        // FindObjectsOfType is expensive, use sparingly.
        // For production, consider using a manual registration system or specific scene object managers.
        ISavable[] savables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISavable>().ToArray();
        
        // Register all found savables. Their RegisterSavable() methods will call this manager's RegisterSavable.
        foreach (ISavable savable in savables)
        {
            savable.RegisterSavable(); // This calls back to PersistentWorldSystem.RegisterSavable(this)
        }
        Debug.Log($"Scanned and found {_registeredSavables.Count} savable objects in scene '{SceneManager.GetActiveScene().name}'.");
    }

    /// <summary>
    /// Saves the current state of all registered ISavable objects to a file.
    /// </summary>
    public void SaveWorld()
    {
        PersistentWorldData worldData = new PersistentWorldData();

        foreach (var pair in _registeredSavables)
        {
            ISavable savable = pair.Value;
            SavableObjectData data = savable.GetSavableData();
            worldData.SavableObjects.Add(data);
        }

        try
        {
            string json = JsonUtility.ToJson(worldData, true); // true for pretty printing
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"World saved successfully to: {SaveFilePath}. Total objects: {worldData.SavableObjects.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save world data: {e.Message}");
        }
    }

    /// <summary>
    /// Loads the world state from the save file and applies it to registered ISavable objects.
    /// </summary>
    public void LoadWorld()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning($"No save file found at: {SaveFilePath}. Starting fresh.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            PersistentWorldData worldData = JsonUtility.FromJson<PersistentWorldData>(json);

            if (worldData == null || worldData.SavableObjects == null)
            {
                Debug.LogError("Failed to deserialize world data or no objects found in save file.");
                return;
            }

            // Create a dictionary for quick lookup of loaded data by ID.
            Dictionary<string, SavableObjectData> loadedDataLookup = worldData.SavableObjects
                .ToDictionary(data => data.Id, data => data);

            int appliedCount = 0;
            int notFoundCount = 0;

            foreach (var pair in _registeredSavables)
            {
                string id = pair.Key;
                ISavable savable = pair.Value;

                if (loadedDataLookup.TryGetValue(id, out SavableObjectData loadedSavableData))
                {
                    savable.ApplySavableData(loadedSavableData);
                    appliedCount++;
                }
                else
                {
                    // This means an object exists in the current scene but was NOT in the save file.
                    // It will retain its default state. This is often desired.
                    // Debug.LogWarning($"Object with ID '{id}' ({((MonoBehaviour)savable).name}) found in scene but not in save file. Retaining default state.");
                    notFoundCount++;
                }
            }
            
            // Handle objects that were in the save file but are NOT currently in the scene.
            // This is crucial for a complete persistent world.
            // For example, if a dynamic enemy was saved but is not currently spawned.
            // This would typically involve a "PrefabManager" or similar system that
            // instantiates missing objects based on their ID and potentially a prefab reference.
            // Example placeholder:
            // foreach (var loadedSavableData in worldData.SavableObjects)
            // {
            //     if (!_registeredSavables.ContainsKey(loadedSavableData.Id))
            //     {
            //         Debug.Log($"Object with ID '{loadedSavableData.Id}' was in save file but not in scene. " +
            //                   $"Consider spawning it here if it's a dynamic object (e.g., using a PrefabManager).");
            //         // Example: PrefabManager.Instance.SpawnObject(loadedSavableData.PrefabId, loadedSavableData);
            //     }
            // }

            Debug.Log($"World loaded successfully. Applied state to {appliedCount} objects. " +
                      $"{notFoundCount} existing objects not found in save data (retained default state).");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load world data: {e.Message}");
        }
    }

    /// <summary>
    /// Clears the save file, effectively resetting the world state.
    /// </summary>
    public void ClearSaveData()
    {
        if (File.Exists(SaveFilePath))
        {
            try
            {
                File.Delete(SaveFilePath);
                Debug.Log($"Save file deleted: {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }
        else
        {
            Debug.Log("No save file to delete.");
        }
    }

    // Example of how you might call save/load from a UI button or other game event.
    // Make sure to assign these public methods to UI events in the Inspector or call them from other scripts.
    // Example usage:
    // public void OnSaveButtonClicked() { PersistentWorldSystem.Instance.SaveWorld(); }
    // public void OnLoadButtonClicked() { PersistentWorldSystem.Instance.LoadWorld(); }
    // public void OnClearSaveButtonClicked() { PersistentWorldSystem.Instance.ClearSaveData(); }
}


/// <summary>
/// An example MonoBehaviour that demonstrates how to implement the ISavable interface.
/// This object will save and load its position, rotation, and active state,
/// plus a custom color and a simple integer counter.
/// </summary>
public class ExampleSavableObject : MonoBehaviour, ISavable
{
    // The unique ID for this specific object.
    // [SerializeField] makes it visible in the Inspector and persists across editor sessions.
    // If empty on Awake, a new GUID is generated. This ensures persistence for scene objects.
    [SerializeField] private string _savableID = "";

    [Header("Example Savable Properties")]
    public Color customColor = Color.white;
    public int interactionCount = 0;

    // --- ISavable Implementation ---

    public string GetSavableID()
    {
        return _savableID;
    }

    public SavableObjectData GetSavableData()
    {
        // Create a data struct to hold common properties.
        SavableObjectData data = new SavableObjectData
        {
            Id = _savableID,
            Position = transform.position,
            Rotation = transform.rotation,
            IsActive = gameObject.activeSelf
        };

        // Create a custom data object for properties specific to THIS savable type.
        // This is then serialized to JSON and stored in CustomDataJson.
        SavableCustomExampleData customData = new SavableCustomExampleData
        {
            SavedColor = customColor,
            SavedInteractionCount = interactionCount
        };
        data.CustomDataJson = JsonUtility.ToJson(customData);

        return data;
    }

    public void ApplySavableData(SavableObjectData data)
    {
        // Apply common properties.
        transform.position = data.Position;
        transform.rotation = data.Rotation;
        gameObject.SetActive(data.IsActive);

        // Deserialize and apply custom properties specific to this object type.
        if (!string.IsNullOrEmpty(data.CustomDataJson))
        {
            try
            {
                SavableCustomExampleData customData = JsonUtility.FromJson<SavableCustomExampleData>(data.CustomDataJson);
                customColor = customData.SavedColor;
                interactionCount = customData.SavedInteractionCount;

                // Apply the loaded color to the material for visual feedback
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = customColor;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize custom data for object '{_savableID}': {e.Message}");
            }
        }
        Debug.Log($"Applied state to '{gameObject.name}' (ID: {_savableID}). Pos: {transform.position}, Active: {gameObject.activeSelf}, Color: {customColor}, Interactions: {interactionCount}");
    }

    public void RegisterSavable()
    {
        PersistentWorldSystem.Instance?.RegisterSavable(this);
    }

    public void DeregisterSavable()
    {
        PersistentWorldSystem.Instance?.DeregisterSavable(this);
    }

    // --- MonoBehaviour Lifecycle ---

    void Awake()
    {
        // If no ID is assigned in the Inspector, generate a new one.
        // This ensures every savable object has a unique, persistent ID.
        if (string.IsNullOrEmpty(_savableID))
        {
            _savableID = Guid.NewGuid().ToString();
            Debug.Log($"Generated new ID for {gameObject.name}: {_savableID}");
        }

        // Apply initial color for demonstration purposes if not loaded.
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = customColor;
        }
    }

    void OnEnable()
    {
        // Register this object with the PersistentWorldSystem when it becomes active.
        RegisterSavable();
    }

    void OnDisable()
    {
        // Deregister this object when it becomes inactive.
        DeregisterSavable();
    }

    // --- Custom Logic for Demonstration ---

    void OnMouseDown()
    {
        // Example interaction: change color and increment counter on click.
        customColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        interactionCount++;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = customColor;
        }

        Debug.Log($"Object '{gameObject.name}' (ID: {_savableID}) clicked! New color: {customColor}, Interactions: {interactionCount}");
    }
}

/// <summary>
/// A custom serializable class to hold additional data specific to ExampleSavableObject.
/// This will be serialized into the CustomDataJson field of SavableObjectData.
/// </summary>
[System.Serializable]
public class SavableCustomExampleData
{
    public Color SavedColor;
    public int SavedInteractionCount;
}

```

---

### How to Use in Unity:

1.  **Create the Script**:
    *   Create a new C# script in your Unity project named `PersistentWorldSystem.cs`.
    *   Copy and paste the entire code above into this new script.

2.  **Setup the PersistentWorldSystem Manager**:
    *   Create an empty GameObject in your scene (e.g., `_GameManager`).
    *   Attach the `PersistentWorldSystem` script to this GameObject.
    *   Make sure this GameObject is present in any scene where you want persistence. Since it uses `DontDestroyOnLoad`, it will persist across scenes if placed in your initial scene.

3.  **Create Savable Objects**:
    *   Create a 3D Object (e.g., a `Cube`) in your scene.
    *   Rename it (e.g., `SavableCube1`).
    *   Attach the `ExampleSavableObject` script to it.
    *   **Crucial**: Observe the `_savableID` field in the Inspector of `SavableCube1`. When you first run the game, if it's empty, a GUID will be automatically generated and assigned. This ID will persist even if you close and reopen Unity, ensuring the system can uniquely identify this specific object across game sessions.
    *   Duplicate this cube a few times (e.g., `SavableCube2`, `SavableCube3`) and move them to different positions. Each will get its own unique ID on first run.

4.  **Test the System**:
    *   **Play the scene.**
    *   **Interact with the cubes**: Click on them to change their color and increment their internal counter. Move them around. Deactivate some using the Inspector or `gameObject.SetActive(false)` in a script.
    *   **Save**: Stop playing. Go back to the `_GameManager` GameObject with the `PersistentWorldSystem` script. In the Inspector, you'll see public methods for `SaveWorld()`, `LoadWorld()`, and `ClearSaveData()`.
    *   Right-click on the `PersistentWorldSystem` component header in the Inspector and select `Save World` from the context menu (or expose these as actual UI buttons in a real game).
    *   **Modify further**: While still in edit mode (or even in play mode, if you want to test loading without saving again), change the positions/colors of the cubes manually to something different than what was just saved.
    *   **Load**: Right-click on the `PersistentWorldSystem` component header and select `Load World`.
    *   **Observe**: The cubes should revert to their state (position, rotation, color, interaction count, active state) exactly as they were when you last saved the world.
    *   **Clear Save**: If you want to start fresh, right-click and select `Clear Save Data`.

### Example UI Integration (Optional):

You'd typically have a simple UI to trigger these actions. Here's how you might set it up:

1.  **Create a UI Canvas**: `GameObject -> UI -> Canvas`.
2.  **Create Buttons**: Add three `UI -> Button` elements to the Canvas.
    *   Rename them to "Save Button", "Load Button", "Clear Save Button".
    *   Change their text to "Save", "Load", "Clear Save".
3.  **Assign Functions to Buttons**:
    *   Select the "Save Button". In the Inspector, find the `Button` component. Under `On Click ()` list, click the `+` icon.
    *   Drag your `_GameManager` GameObject (with `PersistentWorldSystem` attached) into the "None (Object)" slot.
    *   From the "No Function" dropdown, select `PersistentWorldSystem -> SaveWorld()`.
    *   Repeat for "Load Button" (assign `LoadWorld()`) and "Clear Save Button" (assign `ClearSaveData()`).
4.  Now, when you play the game, you can click these UI buttons to save and load your world state.

This complete example provides a solid foundation for implementing persistence in your Unity games, adhering to best practices and offering detailed explanations for educational purposes.