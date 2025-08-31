// Unity Design Pattern Example: ServicePattern
// This script demonstrates the ServicePattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The Service Pattern is a design pattern used to decouple the service interface from its concrete implementation. It allows clients to use a service without knowing the underlying implementation details, promoting flexibility, testability, and maintainability.

In Unity, this pattern is incredibly useful for managing systems like:
*   **Save/Load Systems:** Swap between PlayerPrefs, JSON files, or custom binary formats.
*   **Audio Systems:** Switch between different audio engines or handle varying complexity.
*   **Analytics Systems:** Use different analytics providers without changing client code.
*   **Input Systems:** Abstract keyboard, gamepad, or touch input behind a common interface.

---

## Service Pattern Unity Example: Save/Load System

This example demonstrates a Save/Load system using the Service Pattern. We'll create:
1.  An `ISaveLoadService` interface.
2.  Two concrete implementations: `PlayerPrefsSaveLoadService` and `JsonFileSaveLoadService`.
3.  A `ServiceLocator` to register and retrieve services.
4.  A `ServicePatternInitializer` to set up which service is active.
5.  A `ServicePatternClient` to demonstrate using the service without knowing its implementation.

### How to Use This Script in Unity:

1.  Create a new C# script named `ServicePatternExample.cs`.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., named "GameManager").
4.  Attach the `ServicePatternInitializer` component to this GameObject.
5.  In the Inspector for `ServicePatternInitializer`, choose which `SaveLoadImplementation` you want to use (e.g., `PlayerPrefs` or `JsonFile`).
6.  Create another empty GameObject (e.g., named "ClientTest").
7.  Attach the `ServicePatternClient` component to this GameObject.
8.  Run the scene. Check the Unity Console to see the save and load operations. You can change the `SaveLoadImplementation` on the `ServicePatternInitializer` and re-run to see it use a different backend without altering the `ServicePatternClient`.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO; // Required for File operations for JSON saving
using System.Linq; // Required for LINQ operations for JSON saving

// =====================================================================================
// 1. Service Interface: Defines the contract for the Save/Load service.
//    Clients will interact with this interface, not the concrete implementations.
// =====================================================================================
public interface ISaveLoadService
{
    // Saves data of a generic type T under a given key.
    void SaveData<T>(string key, T data);

    // Loads data of a generic type T from a given key.
    // Returns default(T) if the key does not exist or data cannot be deserialized.
    T LoadData<T>(string key);

    // Checks if data exists for a given key.
    bool HasKey(string key);

    // Deletes data associated with a given key.
    void DeleteKey(string key);

    // Clears all saved data.
    void ClearAllData();
}

// =====================================================================================
// 2. Concrete Service Implementation: Uses Unity's PlayerPrefs for saving.
//    Simple for small data, but not suitable for complex or large structures.
// =====================================================================================
public class PlayerPrefsSaveLoadService : ISaveLoadService
{
    public void SaveData<T>(string key, T data)
    {
        // PlayerPrefs only directly supports int, float, string.
        // For other types, we need to serialize them to a string (e.g., JSON).
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save(); // Ensure data is written to disk immediately
        Debug.Log($"PlayerPrefsSaveLoadService: Saved data for key '{key}'. Data: {json}");
    }

    public T LoadData<T>(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            Debug.Log($"PlayerPrefsSaveLoadService: Loaded JSON for key '{key}': {json}");
            return JsonUtility.FromJson<T>(json);
        }
        Debug.LogWarning($"PlayerPrefsSaveLoadService: No data found for key '{key}'. Returning default.");
        return default(T);
    }

    public bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public void DeleteKey(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"PlayerPrefsSaveLoadService: Deleted key '{key}'.");
        }
    }

    public void ClearAllData()
    {
        // PlayerPrefs.DeleteAll() deletes ALL PlayerPrefs, which might include unrelated game settings.
        // For a more controlled approach, one might prefix keys or keep track of them.
        // For this example, we'll use DeleteAll for simplicity.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefsSaveLoadService: Cleared ALL PlayerPrefs data.");
    }
}

// =====================================================================================
// 2. Concrete Service Implementation: Uses JSON files for saving.
//    More robust for complex data structures and larger amounts of data.
// =====================================================================================
public class JsonFileSaveLoadService : ISaveLoadService
{
    // Define the folder where save files will be stored.
    // Application.persistentDataPath is a platform-independent path for persistent data.
    private string saveFolderPath => Application.persistentDataPath + "/SaveData/";

    public JsonFileSaveLoadService()
    {
        // Ensure the save directory exists when the service is instantiated.
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"JsonFileSaveLoadService: Created save directory at: {saveFolderPath}");
        }
    }

    private string GetFilePath(string key)
    {
        // Use a simple hash or sanitize the key to ensure it's a valid filename.
        // For simplicity, we'll just use the key directly with .json extension.
        // In a real project, consider GUIDs or more robust key-to-filename mappings.
        return Path.Combine(saveFolderPath, $"{key}.json");
    }

    public void SaveData<T>(string key, T data)
    {
        string filePath = GetFilePath(key);
        string json = JsonUtility.ToJson(data, true); // 'true' for pretty printing
        File.WriteAllText(filePath, json);
        Debug.Log($"JsonFileSaveLoadService: Saved data for key '{key}' to '{filePath}'. Data: {json}");
    }

    public T LoadData<T>(string key)
    {
        string filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Debug.Log($"JsonFileSaveLoadService: Loaded JSON for key '{key}' from '{filePath}': {json}");
            return JsonUtility.FromJson<T>(json);
        }
        Debug.LogWarning($"JsonFileSaveLoadService: No data file found for key '{key}' at '{filePath}'. Returning default.");
        return default(T);
    }

    public bool HasKey(string key)
    {
        return File.Exists(GetFilePath(key));
    }

    public void DeleteKey(string key)
    {
        string filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"JsonFileSaveLoadService: Deleted file for key '{key}' at '{filePath}'.");
        }
    }

    public void ClearAllData()
    {
        if (Directory.Exists(saveFolderPath))
        {
            Directory.Delete(saveFolderPath, true); // 'true' to delete directory and its contents
            Debug.Log($"JsonFileSaveLoadService: Cleared all data by deleting directory: {saveFolderPath}");
            // Re-create the directory so subsequent saves still work
            Directory.CreateDirectory(saveFolderPath);
        }
    }
}

// =====================================================================================
// 3. Service Locator: A central static class to register and retrieve services.
//    This allows easy access to services from any part of the application.
//    A dictionary maps interface types to their concrete instances.
// =====================================================================================
public static class ServiceLocator
{
    // Dictionary to store registered services. Keys are the interface types,
    // values are the actual service instances.
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    // Registers a service implementation for a given interface type.
    // TInterface: The interface type (e.g., ISaveLoadService).
    // TImplementation: The concrete class that implements the interface.
    public static void RegisterService<TInterface>(TInterface service) where TInterface : class
    {
        Type interfaceType = typeof(TInterface);
        if (_services.ContainsKey(interfaceType))
        {
            Debug.LogWarning($"ServiceLocator: Service of type {interfaceType.Name} already registered. Overwriting.");
            _services[interfaceType] = service;
        }
        else
        {
            _services.Add(interfaceType, service);
        }
        Debug.Log($"ServiceLocator: Registered service {service.GetType().Name} for interface {interfaceType.Name}.");
    }

    // Retrieves a registered service by its interface type.
    // TInterface: The interface type of the service to retrieve.
    public static TInterface GetService<TInterface>() where TInterface : class
    {
        Type interfaceType = typeof(TInterface);
        if (_services.TryGetValue(interfaceType, out object service))
        {
            return (TInterface)service;
        }
        Debug.LogError($"ServiceLocator: Service of type {interfaceType.Name} not registered.");
        return null;
    }

    // Unregisters a service. Useful for testing or changing services dynamically.
    public static void UnregisterService<TInterface>() where TInterface : class
    {
        Type interfaceType = typeof(TInterface);
        if (_services.Remove(interfaceType))
        {
            Debug.Log($"ServiceLocator: Unregistered service for interface {interfaceType.Name}.");
        }
        else
        {
            Debug.LogWarning($"ServiceLocator: No service registered for interface {interfaceType.Name} to unregister.");
        }
    }

    // Clears all registered services.
    public static void ClearAllServices()
    {
        _services.Clear();
        Debug.Log("ServiceLocator: All services cleared.");
    }
}

// =====================================================================================
// Example Data Structure: What we're actually going to save and load.
// Needs to be Serializable for JsonUtility.
// =====================================================================================
[Serializable]
public class PlayerProfile
{
    public string playerName;
    public int level;
    public float health;
    public List<string> inventory;

    public PlayerProfile(string name, int lvl, float hp, List<string> items)
    {
        playerName = name;
        level = lvl;
        health = hp;
        inventory = items;
    }

    public override string ToString()
    {
        return $"Player: {playerName}, Level: {level}, Health: {health}, Inventory: [{string.Join(", ", inventory)}]";
    }
}

// =====================================================================================
// 4. Service Initializer: A MonoBehaviour that sets up and registers the
//    chosen service implementation at the start of the application.
//    This is where you decide which concrete service to use.
// =====================================================================================
public class ServicePatternInitializer : MonoBehaviour
{
    // Enum to choose which SaveLoad implementation to use via the Inspector.
    public enum SaveLoadImplementation { PlayerPrefs, JsonFile }

    [Tooltip("Choose which SaveLoad service implementation to use.")]
    [SerializeField] private SaveLoadImplementation _saveLoadType = SaveLoadImplementation.PlayerPrefs;

    void Awake()
    {
        Debug.Log($"ServicePatternInitializer: Initializing with {_saveLoadType} service.");
        // Clear any previously registered services to ensure a clean state
        ServiceLocator.ClearAllServices();

        ISaveLoadService saveLoadService = null;

        // Instantiate the chosen concrete service and register it.
        switch (_saveLoadType)
        {
            case SaveLoadImplementation.PlayerPrefs:
                saveLoadService = new PlayerPrefsSaveLoadService();
                break;
            case SaveLoadImplementation.JsonFile:
                saveLoadService = new JsonFileSaveLoadService();
                break;
            default:
                Debug.LogError("ServicePatternInitializer: Unknown SaveLoad implementation selected.");
                return;
        }

        // Register the instantiated service with the Service Locator.
        // The Service Locator now holds an instance of the concrete service,
        // but clients will only request it via its interface (ISaveLoadService).
        ServiceLocator.RegisterService<ISaveLoadService>(saveLoadService);
    }
}

// =====================================================================================
// 5. Service Client: A MonoBehaviour that uses the Save/Load service.
//    It only knows about the ISaveLoadService interface, not the specific implementation.
// =====================================================================================
public class ServicePatternClient : MonoBehaviour
{
    private ISaveLoadService _saveLoadService;
    private const string PROFILE_KEY = "PlayerProfileData";

    void Start()
    {
        // Retrieve the service from the Service Locator.
        // The client doesn't care if it's PlayerPrefs, JSON file, or anything else.
        _saveLoadService = ServiceLocator.GetService<ISaveLoadService>();

        if (_saveLoadService == null)
        {
            Debug.LogError("ServicePatternClient: SaveLoadService not found! Ensure ServicePatternInitializer is in the scene and runs first.");
            return;
        }

        Debug.Log("--- ServicePatternClient: Demonstrating Save/Load ---");

        // Step 1: Attempt to load an existing profile
        PlayerProfile loadedProfile = _saveLoadService.LoadData<PlayerProfile>(PROFILE_KEY);

        if (loadedProfile == null || string.IsNullOrEmpty(loadedProfile.playerName))
        {
            Debug.Log("ServicePatternClient: No existing profile found or failed to load. Creating a new one.");
            // Step 2: Create a new profile if none exists
            PlayerProfile newProfile = new PlayerProfile(
                "HeroName",
                1,
                100.0f,
                new List<string> { "Sword", "Shield", "Potion" }
            );
            _saveLoadService.SaveData(PROFILE_KEY, newProfile);
            Debug.Log($"ServicePatternClient: New profile saved: {newProfile}");

            // Load it back immediately to verify
            loadedProfile = _saveLoadService.LoadData<PlayerProfile>(PROFILE_KEY);
            if (loadedProfile != null)
            {
                Debug.Log($"ServicePatternClient: Verified newly saved profile: {loadedProfile}");
            }
        }
        else
        {
            Debug.Log($"ServicePatternClient: Loaded existing profile: {loadedProfile}");

            // Step 3: Modify the profile and save again
            loadedProfile.level++;
            loadedProfile.health -= 10.0f;
            loadedProfile.inventory.Add("New Item " + loadedProfile.level);

            _saveLoadService.SaveData(PROFILE_KEY, loadedProfile);
            Debug.Log($"ServicePatternClient: Modified and saved profile: {loadedProfile}");

            // Step 4: Load again to confirm changes
            PlayerProfile updatedProfile = _saveLoadService.LoadData<PlayerProfile>(PROFILE_KEY);
            Debug.Log($"ServicePatternClient: Confirmed updated profile after reload: {updatedProfile}");
        }

        // Step 5: Check if a specific key exists
        bool hasKey = _saveLoadService.HasKey(PROFILE_KEY);
        Debug.Log($"ServicePatternClient: Does key '{PROFILE_KEY}' exist? {hasKey}");

        // Example: Delete the profile after 5 seconds to demonstrate deletion
        // and allow for a fresh start on next run if desired.
        // Invoke("DeleteProfile", 5.0f);
    }

    private void DeleteProfile()
    {
        if (_saveLoadService != null && _saveLoadService.HasKey(PROFILE_KEY))
        {
            _saveLoadService.DeleteKey(PROFILE_KEY);
            Debug.Log($"ServicePatternClient: Profile '{PROFILE_KEY}' deleted after delay.");
            bool hasKeyAfterDelete = _saveLoadService.HasKey(PROFILE_KEY);
            Debug.Log($"ServicePatternClient: Does key '{PROFILE_KEY}' exist after delete? {hasKeyAfterDelete}");
        }
    }

    // Optional: Demonstrate clearing all data (use with caution!)
    // private void ClearAllDataExample()
    // {
    //     if (_saveLoadService != null)
    //     {
    //         _saveLoadService.ClearAllData();
    //         Debug.Log("ServicePatternClient: All save data cleared.");
    //     }
    // }
}

```