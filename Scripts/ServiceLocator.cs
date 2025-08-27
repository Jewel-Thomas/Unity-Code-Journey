// Unity Design Pattern Example: ServiceLocator
// This script demonstrates the ServiceLocator pattern in Unity
// Generated automatically - ready to use in your Unity project

The ServiceLocator pattern is a design pattern used to encapsulate the process of obtaining services with a strong abstraction layer. It acts as a centralized registry where services (objects providing specific functionalities) can register themselves and be retrieved by other parts of the application without knowing their concrete implementations.

This example provides a complete, practical demonstration of the ServiceLocator pattern in Unity, including interfaces, concrete service implementations, the locator itself, and a Unity `MonoBehaviour` for initializing and registering services.

---

### How to Use This Script in Unity:

1.  **Create a new C# script** in your Unity project (e.g., `ServiceLocatorExample.cs`).
2.  **Copy and paste** the entire code below into the new script.
3.  **Create an empty GameObject** in your scene (e.g., named "ServiceInitializer").
4.  **Attach the `ServiceInitializer` component** (from the `ServiceLocatorExample.cs` script) to this GameObject.
5.  **Run the scene.** Observe the Console output to see the services being registered and used.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

// --- 1. Define Service Interfaces ---
// These interfaces define the contracts that our services will adhere to.
// Using interfaces is crucial for decoupling: client code depends only on the interface,
// not on the concrete implementation. This allows us to easily swap implementations later.

/// <summary>
/// Interface for a logging service.
/// </summary>
public interface ILoggerService
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
}

/// <summary>
/// Interface for a game saving and loading service.
/// </summary>
public interface ISaveLoadService
{
    void SaveData(string key, string data);
    string LoadData(string key);
    bool HasData(string key);
    void DeleteData(string key);
}


// --- 2. Implement Concrete Services ---
// These classes provide the actual implementation for the service interfaces.
// They contain the specific logic for logging, saving, etc.

/// <summary>
/// Concrete implementation of ILoggerService that logs messages to Unity's console.
/// </summary>
public class ConsoleLoggerService : ILoggerService
{
    public void Log(string message)
    {
        Debug.Log($"[Logger] {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[Logger Warning] {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[Logger Error] {message}");
    }
}

/// <summary>
/// Concrete implementation of ISaveLoadService using Unity's PlayerPrefs.
/// </summary>
public class PlayerPrefsSaveLoadService : ISaveLoadService
{
    public void SaveData(string key, string data)
    {
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save(); // Ensure data is written to disk immediately
        Debug.Log($"[SaveLoad] Saved data for key '{key}': {data}");
    }

    public string LoadData(string key)
    {
        string data = PlayerPrefs.GetString(key, string.Empty);
        Debug.Log($"[SaveLoad] Loaded data for key '{key}': {data}");
        return data;
    }

    public bool HasData(string key)
    {
        bool hasData = PlayerPrefs.HasKey(key);
        Debug.Log($"[SaveLoad] Checking for key '{key}': {hasData}");
        return hasData;
    }

    public void DeleteData(string key)
    {
        PlayerPrefs.DeleteKey(key);
        Debug.Log($"[SaveLoad] Deleted data for key '{key}'");
    }
}


// --- 3. The ServiceLocator Itself ---
// This is the core of the pattern. It's a static class, making it globally accessible.
// It holds a dictionary mapping service interfaces (Types) to their concrete instances.

/// <summary>
/// The static ServiceLocator class.
/// Provides a centralized registry for services, allowing other objects to
/// retrieve service instances without knowing their concrete implementations.
/// </summary>
public static class ServiceLocator
{
    // A dictionary to store registered services.
    // The key is the Type of the service interface (e.g., typeof(ILoggerService)).
    // The value is the actual service instance (e.g., an instance of ConsoleLoggerService).
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    /// <summary>
    /// Registers a service instance with the locator.
    /// </summary>
    /// <typeparam name="TService">The type of the service interface being registered.</typeparam>
    /// <param name="serviceInstance">The concrete instance of the service to register.</param>
    /// <exception cref="InvalidOperationException">Thrown if a service of the same type is already registered.</exception>
    public static void RegisterService<TService>(TService serviceInstance)
    {
        // Check if the service is already registered to prevent duplicates or unexpected behavior.
        if (_services.ContainsKey(typeof(TService)))
        {
            Debug.LogWarning($"[ServiceLocator] Service of type {typeof(TService).Name} is already registered. Overwriting.");
            _services[typeof(TService)] = serviceInstance; // Or throw an exception if strict one-instance policy is desired
        }
        else
        {
            _services.Add(typeof(TService), serviceInstance);
            Debug.Log($"[ServiceLocator] Registered service: {typeof(TService).Name}");
        }
    }

    /// <summary>
    /// Retrieves a service instance from the locator.
    /// </summary>
    /// <typeparam name="TService">The type of the service interface to retrieve.</typeparam>
    /// <returns>The registered instance of the service.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the requested service is not registered.</exception>
    public static TService GetService<TService>()
    {
        // Check if the service is registered before attempting to retrieve it.
        if (!_services.TryGetValue(typeof(TService), out object serviceInstance))
        {
            // If the service is not found, throw an exception to indicate a programming error.
            throw new InvalidOperationException($"[ServiceLocator] Service of type {typeof(TService).Name} not registered.");
        }

        // Cast the generic object to the specific service type and return it.
        return (TService)serviceInstance;
    }

    /// <summary>
    /// Unregisters a service from the locator.
    /// </summary>
    /// <typeparam name="TService">The type of the service interface to unregister.</typeparam>
    public static void UnregisterService<TService>()
    {
        if (_services.Remove(typeof(TService)))
        {
            Debug.Log($"[ServiceLocator] Unregistered service: {typeof(TService).Name}");
        }
        else
        {
            Debug.LogWarning($"[ServiceLocator] Attempted to unregister service {typeof(TService).Name} but it was not found.");
        }
    }

    /// <summary>
    /// Clears all registered services. Useful for testing or scene transitions.
    /// </summary>
    public static void ClearServices()
    {
        _services.Clear();
        Debug.Log("[ServiceLocator] All services cleared.");
    }
}


// --- 4. Service Initializer (Unity MonoBehaviour) ---
// This MonoBehaviour will run early in the game lifecycle (Awake) to
// instantiate our concrete services and register them with the ServiceLocator.
// It acts as the "composition root" for our services.

/// <summary>
/// A Unity MonoBehaviour responsible for initializing and registering all application services
/// with the ServiceLocator at game startup.
/// </summary>
public class ServiceInitializer : MonoBehaviour
{
    // Make sure this GameObject persists across scenes if services are needed globally.
    // However, for this example, we'll keep it simple and assume it's set up per scene
    // or loaded once.
    [SerializeField]
    private bool _dontDestroyOnLoad = true;

    void Awake()
    {
        // OPTIONAL: Ensure this initializer instance is a singleton and persists.
        // If you have a more sophisticated scene management, you might handle this differently.
        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[ServiceInitializer] Set to DontDestroyOnLoad.");
        }

        Debug.Log("--- Initializing Services ---");

        // Clear any previously registered services, especially useful during editor play/stop cycles
        // or when moving between scenes if this is set to DontDestroyOnLoad.
        ServiceLocator.ClearServices(); 

        // 1. Instantiate Concrete Services
        // For simple services, we can directly create new instances.
        // For services that might need other dependencies or be MonoBehaviours,
        // you would instantiate them differently (e.g., AddComponent<T>).
        var logger = new ConsoleLoggerService();
        var saveLoad = new PlayerPrefsSaveLoadService();

        // 2. Register Services with the ServiceLocator
        ServiceLocator.RegisterService<ILoggerService>(logger);
        ServiceLocator.RegisterService<ISaveLoadService>(saveLoad);

        Debug.Log("--- Services Initialized and Registered ---");

        // Demonstrate immediate usage to confirm they work
        TestServiceUsage();
    }

    /// <summary>
    /// Demonstrates how a client script would retrieve and use services.
    /// This method would typically be in other game scripts (e.g., PlayerController, GameManager).
    /// </summary>
    private void TestServiceUsage()
    {
        Debug.Log("\n--- Demonstrating Service Usage ---");

        // Retrieve the logger service
        ILoggerService myLogger = ServiceLocator.GetService<ILoggerService>();
        myLogger.Log("This message is logged via the ServiceLocator!");
        myLogger.LogWarning("This is a warning from the ServiceLocator.");
        myLogger.LogError("An error occurred via ServiceLocator!");

        // Retrieve the save/load service
        ISaveLoadService mySaveLoad = ServiceLocator.GetService<ISaveLoadService>();

        string gameKey = "PlayerName";
        string playerData = "HeroPlayer1";

        // Test saving
        mySaveLoad.SaveData(gameKey, playerData);

        // Test loading
        string loadedData = mySaveLoad.LoadData(gameKey);
        myLogger.Log($"Retrieved player data: {loadedData}");

        // Test non-existent data
        string missingKey = "NonExistentScore";
        if (!mySaveLoad.HasData(missingKey))
        {
            myLogger.LogWarning($"Key '{missingKey}' does not exist, as expected.");
        }

        // Test deleting data
        mySaveLoad.DeleteData(gameKey);
        if (!mySaveLoad.HasData(gameKey))
        {
            myLogger.Log($"Player data for '{gameKey}' deleted successfully.");
        }
        
        Debug.Log("--- Service Usage Demonstration Complete ---\n");
    }

    // Optional: Unregister services on destroy if they hold significant resources
    // or need to be cleaned up specifically. For static services like this,
    // it's often not strictly necessary unless you're reloading scenes without DontDestroyOnLoad.
    void OnDestroy()
    {
        // ServiceLocator.UnregisterService<ILoggerService>();
        // ServiceLocator.UnregisterService<ISaveLoadService>();
        // Debug.Log("Services unregistered on destroy.");
    }
}

/*
/// --- Example Client Script (Illustrative - you would put this in a separate file) ---
/// This shows how any other script in your game can access services.
///

// To use this, create a new C# script named `GameClientExample.cs`
// and attach it to any GameObject in your scene.

// using UnityEngine;

// public class GameClientExample : MonoBehaviour
// {
//     void Start()
//     {
//         // Get the logger service from the locator
//         ILoggerService logger = ServiceLocator.GetService<ILoggerService>();
//         logger.Log("GameClientExample: Start method executed.");

//         // Get the save/load service
//         ISaveLoadService saveLoad = ServiceLocator.GetService<ISaveLoadService>();

//         string levelKey = "CurrentLevel";
//         // Try to load current level data
//         if (saveLoad.HasData(levelKey))
//         {
//             string loadedLevel = saveLoad.LoadData(levelKey);
//             logger.Log($"GameClientExample: Loaded current level: {loadedLevel}");
//         }
//         else
//         {
//             logger.Log("GameClientExample: No current level data found. Setting default.");
//             saveLoad.SaveData(levelKey, "Level1-Forest");
//         }
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.L))
//         {
//             ILoggerService logger = ServiceLocator.GetService<ILoggerService>();
//             logger.Log("GameClientExample: 'L' key pressed. Logging via service.");
//         }

//         if (Input.GetKeyDown(KeyCode.S))
//         {
//             ISaveLoadService saveLoad = ServiceLocator.GetService<ISaveLoadService>();
//             // Example: Save player's score
//             int currentScore = UnityEngine.Random.Range(100, 1000);
//             saveLoad.SaveData("PlayerScore", currentScore.ToString());
//             ILoggerService logger = ServiceLocator.GetService<ILoggerService>();
//             logger.Log($"GameClientExample: Player score {currentScore} saved.");
//         }
//     }
// }

*/

```

### Explanation of the ServiceLocator Pattern:

1.  **Service Interfaces (`ILoggerService`, `ISaveLoadService`):**
    *   **Purpose:** Define the contract for a service. This is the most crucial part for decoupling. Any client (another script) that needs logging functionality only knows about `ILoggerService`, not `ConsoleLoggerService`.
    *   **Benefit:** Allows you to swap out implementations without changing client code. For example, you could replace `ConsoleLoggerService` with a `FileLoggerService` or a `NetworkLoggerService` without modifying any script that *uses* `ILoggerService`.

2.  **Concrete Service Implementations (`ConsoleLoggerService`, `PlayerPrefsSaveLoadService`):**
    *   **Purpose:** These classes provide the actual logic for the service interfaces. They implement the methods defined in their respective interfaces.
    *   **Example:** `ConsoleLoggerService` uses `Debug.Log`, while `PlayerPrefsSaveLoadService` uses `PlayerPrefs`.

3.  **The `ServiceLocator` Class (Static):**
    *   **Purpose:** This is the central registry. It holds references to all active service instances.
    *   **`_services` Dictionary:** Stores services. The key is the `Type` of the *interface* (e.g., `typeof(ILoggerService)`), and the value is the *instance* of the concrete service (e.g., `new ConsoleLoggerService()`).
    *   **`RegisterService<TService>(TService serviceInstance)`:**
        *   Adds a service instance to the dictionary.
        *   The `TService` type parameter ensures that the registered instance matches the interface type.
        *   Includes a warning if a service is re-registered (can be changed to throw an error for stricter control).
    *   **`GetService<TService>()`:**
        *   Retrieves a service instance from the dictionary.
        *   If the service isn't found, it throws an `InvalidOperationException`. This is a good practice as it makes a missing dependency obvious during development.
    *   **`UnregisterService<TService>()` / `ClearServices()`:** Provide ways to remove services, which can be useful for managing resources during scene transitions or for testing purposes.

4.  **`ServiceInitializer` (`MonoBehaviour`):**
    *   **Purpose:** This Unity script acts as the "composition root" or "bootstrap" for your services. It's responsible for:
        *   Creating concrete instances of your services.
        *   Registering these instances with the `ServiceLocator`.
    *   **`Awake()` Method:** This is a good place for initialization because it runs early in the GameObject lifecycle, ensuring services are available before most other scripts try to use them.
    *   **`DontDestroyOnLoad`:** If services are meant to be global and persist across scene loads (like a logger or a persistent save system), using `DontDestroyOnLoad(gameObject)` on the initializer ensures it and its registered services remain active.
    *   **`TestServiceUsage()`:** Demonstrates how client code would retrieve and use services.

### Advantages of ServiceLocator:

*   **Decoupling:** Client code depends only on interfaces, not concrete implementations, making it easier to swap out services.
*   **Centralized Access:** Services can be accessed from anywhere in the application without needing direct references or complex dependency passing.
*   **Simplicity:** For smaller projects or teams, it can be simpler to set up than a full-fledged Dependency Injection (DI) framework.
*   **Runtime Swapping:** Can easily swap service implementations at runtime (e.g., a "mock" logger for testing vs. a "production" logger).

### Disadvantages of ServiceLocator:

*   **Hidden Dependencies:** It can be hard to tell what services a class needs just by looking at its constructor or method signatures, as dependencies are "pulled" from the locator rather than being "pushed" in. This can make the dependency graph less obvious.
*   **"God Object" Tendency:** The ServiceLocator itself can become a "God object" if too many services are registered and retrieved, leading to a single point of failure or complexity.
*   **Testability Challenges:** While implementations can be swapped for testing, mocking the locator itself can be tricky, and it can encourage tight coupling to the locator rather than to the actual service interfaces.
*   **Violates Dependency Inversion Principle (DIP):** It still ties clients to the `ServiceLocator` itself, rather than purely to their abstract dependencies.
*   **Lifetime Management:** Managing the lifecycle of registered services (when they are created, destroyed, or reset) can become complex in larger applications, especially without a dedicated DI framework.

ServiceLocator is a useful pattern when you need a straightforward way to provide global access to services without direct coupling, especially in Unity where static access is common. However, be mindful of its downsides, and for very large or complex projects, a more robust Dependency Injection framework might be considered.