// Unity Design Pattern Example: GameAnalyticsSystem
// This script demonstrates the GameAnalyticsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **GameAnalyticsSystem** design pattern. This pattern provides a flexible and maintainable way to integrate multiple analytics services into your game without coupling your game logic directly to any specific analytics SDK.

**Key Concepts of the GameAnalyticsSystem Pattern:**

1.  **IAnalyticsProvider Interface:** Defines a common contract for all analytics services. Any class that implements this interface can be treated as an analytics provider.
2.  **Concrete Analytics Providers:** Implementations of `IAnalyticsProvider` for specific services (e.g., `DebugLogAnalyticsProvider`, `DummyRemoteAnalyticsProvider`, Google Analytics, Firebase, etc.).
3.  **GameAnalyticsManager (Singleton):** A central manager that acts as the public interface for your game to log events. It maintains a list of registered `IAnalyticsProvider` instances and dispatches events to all of them.

---

### GameAnalyticsSystem.cs

Save this entire code block as a single C# script named `GameAnalyticsSystem.cs` in your Unity project's Assets folder.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // Required for IEnumerator (coroutine) in DummyRemoteAnalyticsProvider

// =====================================================================================
// DESIGN PATTERN: Game Analytics System
// =====================================================================================
//
// GOAL: Provide a unified interface for logging game analytics events, abstracting away
//       the specific analytics providers (e.g., Google Analytics, Firebase, custom backend,
//       local debugging logs).
//
// BENEFITS:
// 1.  **Decoupling:** Game code doesn't need to know about specific analytics SDKs.
//     It just calls GameAnalyticsManager.Instance.TrackEvent(). This means you can swap
//     out or add new analytics providers without touching core game logic.
// 2.  **Flexibility:** Easily add, remove, or swap analytics providers. Want to add a new
//     provider? Implement IAnalyticsProvider and register it.
// 3.  **Centralization:** All analytics calls go through one central point, making
//     management, configuration, and debugging easier.
// 4.  **Testing & Development:** During development, you can use a `DebugLogAnalyticsProvider`
//     to see events in the Unity console without sending data to live services.
// 5.  **Performance Control:** Potentially allows for throttling or batching events
//     at the `GameAnalyticsManager` level before dispatching to providers.
//
// COMPONENTS:
// 1.  **IAnalyticsProvider:** An interface defining the contract for any analytics
//     service. This ensures all providers have common methods for tracking events, screens, etc.
// 2.  **Concrete Analytics Providers:** Implementations of IAnalyticsProvider for specific
//     services (e.g., DebugLogAnalyticsProvider, DummyRemoteAnalyticsProvider).
// 3.  **GameAnalyticsManager (Singleton):** The central hub. It maintains a list of
//     registered IAnalyticsProvider instances and dispatches incoming analytics calls
//     to all of them. It is implemented as a MonoBehaviour Singleton for easy
//     access and Unity lifecycle management.
//
// HOW TO USE THIS EXAMPLE:
// 1.  Create an empty GameObject in your scene (e.g., "AnalyticsManager").
// 2.  Attach the `GameAnalyticsManager` script to this GameObject.
// 3.  In the Inspector for "AnalyticsManager", enable or disable the desired providers
//     (e.g., DebugLog Analytics Provider, Dummy Remote Analytics Provider).
// 4.  Attach the `ExampleGameEventGenerator` script to any other GameObject in your
//     scene (e.g., "GameLogic").
// 5.  Run the scene. You will see analytics events being logged to the console by
//     the enabled providers. Use the Spacebar and 'S' key to trigger more events.
//
// =====================================================================================

#region IAnalyticsProvider Interface
/// <summary>
/// Defines the contract for any analytics service provider.
/// All concrete analytics implementations must adhere to this interface.
/// </summary>
public interface IAnalyticsProvider
{
    /// <summary>
    /// Initializes the analytics provider. Called once when the GameAnalyticsManager starts.
    /// This is where you would initialize SDKs, set API keys, etc.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Tracks a game event with a given name and optional data.
    /// </summary>
    /// <param name="eventName">The name of the event (e.g., "Level_Started", "Item_Collected").</param>
    /// <param name="eventData">Optional dictionary of key-value pairs providing more context
    /// (e.g., {"level": 1, "score": 100, "item_type": "Coin"}).</param>
    void TrackEvent(string eventName, Dictionary<string, object> eventData = null);

    /// <summary>
    /// Tracks a screen or scene change.
    /// </summary>
    /// <param name="screenName">The name of the screen/scene (e.g., "MainMenu", "Level_1_Gameplay").</param>
    void TrackScreen(string screenName);
}
#endregion

#region Concrete Analytics Providers

/// <summary>
/// An analytics provider that simply logs events to Unity's Debug.Log.
/// This is invaluable for development and testing without hitting external services.
/// </summary>
public class DebugLogAnalyticsProvider : IAnalyticsProvider
{
    private const string TAG = "[DebugLogAnalyticsProvider]";

    public void Initialize()
    {
        Debug.Log($"{TAG} Initialized.");
    }

    public void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
    {
        string dataString = "";
        if (eventData != null && eventData.Count > 0)
        {
            List<string> dataItems = new List<string>();
            foreach (var kvp in eventData)
            {
                dataItems.Add($"{kvp.Key}={kvp.Value}");
            }
            dataString = " | Data: " + string.Join(", ", dataItems);
        }

        Debug.Log($"{TAG} Event Tracked: '{eventName}'{dataString}");
    }

    public void TrackScreen(string screenName)
    {
        Debug.Log($"{TAG} Screen Tracked: '{screenName}'");
    }
}

/// <summary>
/// A dummy analytics provider that simulates sending data to a remote service.
/// It introduces a delay to mimic network latency for asynchronous operations.
/// This demonstrates how a real provider might handle web requests.
/// </summary>
public class DummyRemoteAnalyticsProvider : IAnalyticsProvider
{
    private const string TAG = "[DummyRemoteAnalyticsProvider]";
    private MonoBehaviour _coroutineHost; // Needed to start coroutines for async operations

    /// <summary>
    /// Constructor requires a MonoBehaviour to start coroutines.
    /// The GameAnalyticsManager will pass itself as the host.
    /// </summary>
    /// <param name="coroutineHost">A MonoBehaviour instance to run coroutines.</param>
    public DummyRemoteAnalyticsProvider(MonoBehaviour coroutineHost)
    {
        _coroutineHost = coroutineHost ?? throw new ArgumentNullException(nameof(coroutineHost));
    }

    public void Initialize()
    {
        Debug.Log($"{TAG} Initialized. Will simulate remote calls with delays.");
    }

    public void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
    {
        _coroutineHost.StartCoroutine(SimulateRemoteCall(
            $"Sending event '{eventName}' to remote server...",
            $"Event '{eventName}' sent successfully. Data: {FormatEventData(eventData)}"));
    }

    public void TrackScreen(string screenName)
    {
        _coroutineHost.StartCoroutine(SimulateRemoteCall(
            $"Sending screen view '{screenName}' to remote server...",
            $"Screen '{screenName}' view sent successfully."));
    }

    /// <summary>
    /// Coroutine to simulate network latency and a remote API call.
    /// </summary>
    private IEnumerator SimulateRemoteCall(string startMessage, string successMessage)
    {
        Debug.Log($"{TAG} {startMessage}");
        // Simulate network latency or server processing time
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
        Debug.Log($"{TAG} {successMessage}");
    }

    /// <summary>
    /// Helper to format event data dictionary into a readable string for logging.
    /// </summary>
    private string FormatEventData(Dictionary<string, object> eventData)
    {
        if (eventData == null || eventData.Count == 0) return "{}";
        List<string> dataItems = new List<string>();
        foreach (var kvp in eventData)
        {
            dataItems.Add($"\"{kvp.Key}\": \"{kvp.Value}\"");
        }
        return "{" + string.Join(", ", dataItems) + "}";
    }
}

// Example of how other providers (e.g., Google Analytics, Firebase) would fit in:
/*
/// <summary>
/// Placeholder for a Google Analytics provider.
/// In a real scenario, this would integrate with the Google Analytics SDK.
/// </summary>
public class GoogleAnalyticsProvider : IAnalyticsProvider
{
    private const string TAG = "[GoogleAnalyticsProvider]";

    public void Initialize()
    {
        Debug.Log($"{TAG} Initializing Google Analytics SDK...");
        // TODO: Call Google Analytics SDK initialization methods here (e.g., GoogleAnalytics.Initialize())
    }

    public void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
    {
        Debug.Log($"{TAG} Tracking Google Analytics event: '{eventName}' with data: {FormatEventData(eventData)}");
        // TODO: Call Google Analytics SDK event tracking methods here
        // Example: GoogleAnalytics.LogEvent(eventName, eventData);
    }

    public void TrackScreen(string screenName)
    {
        Debug.Log($"{TAG} Tracking Google Analytics screen: '{screenName}'");
        // TODO: Call Google Analytics SDK screen tracking methods here
        // Example: GoogleAnalytics.LogScreen(screenName);
    }

    private string FormatEventData(Dictionary<string, object> eventData)
    {
        // Similar formatting as above
        if (eventData == null || eventData.Count == 0) return "{}";
        List<string> dataItems = new List<string>();
        foreach (var kvp in eventData)
        {
            dataItems.Add($"\"{kvp.Key}\": \"{kvp.Value}\"");
        }
        return "{" + string.Join(", ", dataItems) + "}";
    }
}
*/

#endregion

#region GameAnalyticsManager Singleton

/// <summary>
/// The central hub for all game analytics.
/// This is a MonoBehaviour Singleton, providing easy global access and Unity lifecycle integration.
/// It dispatches analytics events to all registered IAnalyticsProvider instances.
/// </summary>
public class GameAnalyticsManager : MonoBehaviour
{
    // ===================================
    // Singleton Implementation
    // ===================================
    private static GameAnalyticsManager _instance;
    public static GameAnalyticsManager Instance
    {
        get
        {
            // If the instance hasn't been set yet (e.g., first access)
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<GameAnalyticsManager>();

                // If no instance exists in the scene, create a new GameObject and attach the manager
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GameAnalyticsManager");
                    _instance = obj.AddComponent<GameAnalyticsManager>();
                    Debug.LogWarning("[GameAnalyticsManager] No existing instance found, created a new one. " +
                                     "Consider placing a GameAnalyticsManager GameObject in your scene to configure its providers via Inspector.");
                }

                // Ensure the instance persists across scene loads
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // ===================================
    // Provider Management
    // ===================================
    // The list of analytics providers that this manager will dispatch events to.
    private List<IAnalyticsProvider> _providers = new List<IAnalyticsProvider>();

    [Header("Enabled Analytics Providers")]
    [Tooltip("Enable / Disable the Debug.Log analytics provider for development purposes.")]
    [SerializeField] private bool _enableDebugLogProvider = true;

    [Tooltip("Enable / Disable a dummy provider that simulates remote calls with network latency.")]
    [SerializeField] private bool _enableDummyRemoteProvider = false;

    // You can add more [SerializeField] booleans or configuration fields here
    // for other specific analytics providers you might want to integrate, e.g.:
    // [Tooltip("Enable / Disable Google Analytics integration.")]
    // [SerializeField] private bool _enableGoogleAnalyticsProvider = false;
    // [Tooltip("Your Google Analytics Tracking ID (e.g., UA-XXXXXXXXX-Y).")]
    // [SerializeField] private string _googleAnalyticsTrackingID = "YOUR_GA_ID";


    private void Awake()
    {
        // Enforce the Singleton pattern: if another instance already exists, destroy this one.
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[GameAnalyticsManager] Duplicate instance detected on '{gameObject.name}'. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton and ensure it survives scene loads.
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize and register all desired analytics providers.
        InitializeAnalyticsProviders();
    }

    /// <summary>
    /// Initializes and registers all chosen analytics providers based on inspector settings.
    /// This method is called automatically during Awake.
    /// </summary>
    private void InitializeAnalyticsProviders()
    {
        _providers.Clear(); // Clear any existing providers (e.g., for re-initialization in editor)

        // Add providers based on Inspector flags
        if (_enableDebugLogProvider)
        {
            AddProvider(new DebugLogAnalyticsProvider());
        }

        if (_enableDummyRemoteProvider)
        {
            // DummyRemoteAnalyticsProvider needs a MonoBehaviour to run coroutines
            AddProvider(new DummyRemoteAnalyticsProvider(this));
        }

        // --- Add other providers here based on your project's needs and configuration ---
        /*
        if (_enableGoogleAnalyticsProvider)
        {
            // You might pass configuration details like _googleAnalyticsTrackingID here
            // Example: AddProvider(new GoogleAnalyticsProvider(_googleAnalyticsTrackingID));
            AddProvider(new GoogleAnalyticsProvider());
        }
        */

        // Call Initialize() on all registered providers to set them up.
        foreach (var provider in _providers)
        {
            provider.Initialize();
        }

        Debug.Log($"[GameAnalyticsManager] Initialized with {_providers.Count} analytics providers.");
    }

    /// <summary>
    /// Dynamically adds a new analytics provider to the system.
    /// Useful for adding custom providers at runtime or integrating third-party SDKs
    /// that are initialized later.
    /// </summary>
    /// <param name="provider">The IAnalyticsProvider instance to add.</param>
    public void AddProvider(IAnalyticsProvider provider)
    {
        if (provider == null)
        {
            Debug.LogError("[GameAnalyticsManager] Attempted to add a null analytics provider.");
            return;
        }
        if (!_providers.Contains(provider))
        {
            _providers.Add(provider);
            Debug.Log($"[GameAnalyticsManager] Provider added: {provider.GetType().Name}");
            // Optionally initialize the provider immediately if added dynamically after manager's Awake
            // provider.Initialize();
        }
        else
        {
            Debug.LogWarning($"[GameAnalyticsManager] Provider {provider.GetType().Name} already added.");
        }
    }

    /// <summary>
    /// Dynamically removes an analytics provider from the system.
    /// This might be useful if a provider becomes obsolete or needs to be temporarily disabled.
    /// </summary>
    /// <param name="provider">The IAnalyticsProvider instance to remove.</param>
    public void RemoveProvider(IAnalyticsProvider provider)
    {
        if (provider == null)
        {
            Debug.LogError("[GameAnalyticsManager] Attempted to remove a null analytics provider.");
            return;
        }
        if (_providers.Remove(provider))
        {
            Debug.Log($"[GameAnalyticsManager] Provider removed: {provider.GetType().Name}");
        }
        else
        {
            Debug.LogWarning($"[GameAnalyticsManager] Provider {provider.GetType().Name} was not found for removal.");
        }
    }


    // ===================================
    // Public Analytics API
    // ===================================
    // These are the methods your game code should call to log analytics.

    /// <summary>
    /// Tracks a game event across all registered analytics providers.
    /// This is the primary method game code should call for most actions.
    /// </summary>
    /// <param name="eventName">The name of the event (e.g., "Player_Died", "PowerUp_Used").</param>
    /// <param name="eventData">Optional dictionary of key-value pairs providing more context
    /// (e.g., {"reason": "fell_off_cliff", "power_up_type": "shield"}).</param>
    public void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("[GameAnalyticsManager] Attempted to track an event with a null or empty name. Event not tracked.");
            return;
        }

        // Dispatch the event to all registered providers
        foreach (var provider in _providers)
        {
            try
            {
                provider.TrackEvent(eventName, eventData);
            }
            catch (Exception ex)
            {
                // Crucial: Catch exceptions from individual providers.
                // This prevents one broken analytics SDK/provider from stopping
                // other analytics or crashing the game.
                Debug.LogError($"[GameAnalyticsManager] Error tracking event '{eventName}' with {provider.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tracks a screen or scene change across all registered analytics providers.
    /// Useful for understanding player navigation paths.
    /// </summary>
    /// <param name="screenName">The name of the screen/scene (e.g., "MainMenu", "GameSettings", "Level_3").</param>
    public void TrackScreen(string screenName)
    {
        if (string.IsNullOrEmpty(screenName))
        {
            Debug.LogError("[GameAnalyticsManager] Attempted to track a screen with a null or empty name. Screen not tracked.");
            return;
        }

        // Dispatch the screen view to all registered providers
        foreach (var provider in _providers)
        {
            try
            {
                provider.TrackScreen(screenName);
            }
            catch (Exception ex)
            {
                // Catch exceptions from individual providers.
                Debug.LogError($"[GameAnalyticsManager] Error tracking screen '{screenName}' with {provider.GetType().Name}: {ex.Message}");
            }
        }
    }
}

#endregion

#region Example Usage

/// <summary>
/// An example MonoBehaviour demonstrating how game code would interact with the GameAnalyticsManager.
/// This script simulates various game events and calls the analytics manager to track them.
/// Attach this script to any GameObject in your scene to see it in action.
/// </summary>
public class ExampleGameEventGenerator : MonoBehaviour
{
    private float _timer;
    private int _levelCount = 0;
    private int _itemCollectedCount = 0;

    void Start()
    {
        // Always track initial game state or scene load
        GameAnalyticsManager.Instance.TrackScreen("MainMenu_Scene");

        // Simulate a "Game Started" event with initial parameters
        GameAnalyticsManager.Instance.TrackEvent("Game_Started", new Dictionary<string, object>
        {
            { "platform", Application.platform.ToString() },
            { "app_version", Application.version },
            { "build_id", "v1.0.0-beta" } // Example custom build ID
        });

        Debug.Log("[ExampleGameEventGenerator] Initial events sent upon Start.");
        Debug.Log("[ExampleGameEventGenerator] Press SPACE to collect an item.");
        Debug.Log("[ExampleGameEventGenerator] Press 'S' to visit the Shop screen.");
    }

    void Update()
    {
        _timer += Time.deltaTime;

        // Simulate a "Level Started" event every 5-10 seconds
        if (_timer >= UnityEngine.Random.Range(5f, 10f))
        {
            _timer = 0;
            _levelCount++;
            string difficulty = _levelCount % 2 == 0 ? "Hard" : "Easy";

            GameAnalyticsManager.Instance.TrackEvent("Level_Started", new Dictionary<string, object>
            {
                { "level_number", _levelCount },
                { "difficulty", difficulty },
                { "first_attempt", _levelCount == 1 ? true : false }
            });

            Debug.Log($"[ExampleGameEventGenerator] Level {_levelCount} started (Difficulty: {difficulty}).");

            // Simulate level completion after a short "gameplay" delay
            StartCoroutine(SimulateLevelComplete(_levelCount));
        }

        // Simulate collecting an item when the Spacebar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _itemCollectedCount++;
            string itemType = UnityEngine.Random.value > 0.5f ? "Coin" : "Gem";
            int itemValue = itemType == "Coin" ? 1 : 10;

            GameAnalyticsManager.Instance.TrackEvent("Item_Collected", new Dictionary<string, object>
            {
                { "item_type", itemType },
                { "item_value", itemValue },
                { "total_items_collected", _itemCollectedCount },
                { "current_level", _levelCount > 0 ? _levelCount : 1 } // If no level started, assume level 1
            });

            Debug.Log($"[ExampleGameEventGenerator] Player collected: {itemType} (Value: {itemValue}). Total collected: {_itemCollectedCount}");
        }

        // Simulate navigating to a new screen (e.g., shop) when 'S' is pressed
        if (Input.GetKeyDown(KeyCode.S))
        {
            GameAnalyticsManager.Instance.TrackScreen("Shop_Screen");
            Debug.Log("[ExampleGameEventGenerator] Player navigated to Shop_Screen.");
        }
    }

    /// <summary>
    /// Simulates gameplay and then logs a "Level_Completed" event.
    /// </summary>
    private IEnumerator SimulateLevelComplete(int level)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f)); // Simulate time spent in level

        bool won = UnityEngine.Random.value > 0.2f; // 80% chance to win the level
        int enemiesKilled = UnityEngine.Random.Range(5, 20);
        int timeTaken = (int)UnityEngine.Random.Range(30, 120); // Time in seconds

        string outcome = won ? "Won" : "Lost";
        int score = won ? enemiesKilled * 100 + timeTaken * 5 + (level * 1000) : 0;
        int retries = UnityEngine.Random.Range(0, 3); // Example of tracking retries for this level

        GameAnalyticsManager.Instance.TrackEvent("Level_Completed", new Dictionary<string, object>
        {
            { "level_number", level },
            { "outcome", outcome },
            { "score_earned", score },
            { "enemies_killed", enemiesKilled },
            { "time_taken_seconds", timeTaken },
            { "retries_on_level", retries },
            { "is_first_completion", level == 1 && won } // True if level 1 won, false otherwise
        });

        Debug.Log($"[ExampleGameEventGenerator] Level {level} {outcome}! Score: {score}.");
    }
}
#endregion
```

---

### How to Set Up in Unity:

1.  **Create the Script:** In your Unity project, create a new C# script (e.g., `Assets/Scripts/GameAnalyticsSystem.cs`) and copy-paste the entire code above into it.
2.  **Create AnalyticsManager GameObject:**
    *   In your scene (e.g., `SampleScene`), create an empty GameObject.
    *   Rename it to `AnalyticsManager`.
    *   Drag the `GameAnalyticsManager` component (from the `GameAnalyticsSystem.cs` script) onto this `AnalyticsManager` GameObject in the Inspector.
3.  **Configure Providers:**
    *   Select the `AnalyticsManager` GameObject.
    *   In the Inspector, you'll see the `Game Analytics Manager` component.
    *   Under "Enabled Analytics Providers", you can toggle:
        *   `Enable Debug Log Provider`: If checked, events will be printed to Unity's console. Great for debugging.
        *   `Enable Dummy Remote Provider`: If checked, events will simulate being sent to a remote server with a delay.
    *   Enable both for this example to see how multiple providers receive the same event.
4.  **Create Event Generator GameObject:**
    *   Create another empty GameObject in your scene.
    *   Rename it to `GameLogic` (or anything descriptive).
    *   Drag the `ExampleGameEventGenerator` component (also from the `GameAnalyticsSystem.cs` script) onto this `GameLogic` GameObject.
5.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Observe the Unity Console. You will see messages from both `DebugLogAnalyticsProvider` and `DummyRemoteAnalyticsProvider` as events are triggered.
    *   **Press `SPACEBAR`** to trigger "Item_Collected" events.
    *   **Press `S` key** to trigger "Shop_Screen" navigation events.
    *   "Level_Started" and "Level_Completed" events will fire automatically over time.

This setup will immediately demonstrate the GameAnalyticsSystem pattern in action, showing how a single call (`GameAnalyticsManager.Instance.TrackEvent(...)`) dispatches data to multiple, decoupled analytics providers.