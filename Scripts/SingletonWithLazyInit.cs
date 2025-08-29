// Unity Design Pattern Example: SingletonWithLazyInit
// This script demonstrates the SingletonWithLazyInit pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the `SingletonWithLazyInit` design pattern in Unity. This pattern ensures that only one instance of a class exists throughout the application's lifetime, provides a global point of access to it, and *creates* that instance only when it's first requested (lazy initialization).

The script consists of two main parts:
1.  **`SingletonWithLazyInit<T>`**: A generic base class that implements the core Singleton logic for any `MonoBehaviour`.
2.  **`GameSettingsManager`**: A concrete example class that inherits from `SingletonWithLazyInit<GameSettingsManager>` to manage game settings.

---

### `SingletonWithLazyInit.cs`

This script defines the generic base class for your Singletons.

```csharp
using UnityEngine;
using System; // Required for EventArgs, Action, etc., though not strictly used in this base class, good for derived classes.

/// <summary>
/// A generic base class for implementing the Singleton pattern with lazy initialization in Unity.
/// This ensures that only one instance of the inheriting MonoBehaviour exists, provides a global
/// point of access to it, and creates the instance only when it's first requested (lazy initialization).
/// The instance will persist across scene loads using DontDestroyOnLoad.
/// </summary>
/// <typeparam name="T">The type of the MonoBehaviour that will be a Singleton.</typeparam>
public abstract class SingletonWithLazyInit<T> : MonoBehaviour where T : MonoBehaviour
{
    // The private static instance of the Singleton.
    // This will hold the single instance of the class.
    private static T _instance;

    // A private static lock object to ensure thread safety when accessing _instance.
    // While Unity primarily runs on a single thread, it's good practice for robust singletons.
    private static readonly object _lock = new object();

    // A flag to prevent the singleton from being recreated when the application is quitting.
    // This is important because Unity might call OnDestroy on the singleton,
    // and then some other script might try to access Instance during the shutdown process,
    // leading to a new instance being created on a "destroyed" GameObject, causing errors.
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// Provides the global access point to the Singleton instance.
    /// If the instance doesn't exist, it will be found in the scene or created.
    /// </summary>
    public static T Instance
    {
        get
        {
            // If the application is quitting, return null to prevent creating a ghost object.
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Returning null.");
                return null;
            }

            // Lock to ensure thread safety during instance retrieval/creation.
            lock (_lock)
            {
                // If the instance is null, try to find or create it.
                if (_instance == null)
                {
                    // 1. Try to find an existing instance in the scene.
                    _instance = (T)FindObjectOfType(typeof(T));

                    // Check if multiple instances already exist. This can happen if an editor places multiple.
                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError($"[Singleton] Something went wrong - there should never be more than 1 singleton of type {typeof(T).Name}. Reopening the scene might fix it.");
                        return _instance; // Return the first one found.
                    }

                    // 2. If no instance found, create a new GameObject and add the component.
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).Name + " (Singleton)";

                        Debug.Log($"[Singleton] An instance of {typeof(T)} was created because one was not found. Name: {singletonObject.name}");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] An instance of {typeof(T)} found in the scene. Name: {_instance.gameObject.name}");
                    }

                    // Ensure the instance persists across scene loads.
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where the core Singleton logic for handling duplicate instances and persistence resides.
    /// </summary>
    protected virtual void Awake()
    {
        // If _instance is null, this is the first (or the one created by 'Instance' getter)
        // Set this object as the singleton instance.
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[Singleton] Initializing instance of {typeof(T).Name}: {gameObject.name}.");
            // Call the virtual hook for derived classes to perform their specific initialization.
            OnAwakeSingleton();
        }
        // If _instance is not null AND it's not THIS object, then this object is a duplicate.
        // Destroy this duplicate to ensure only one instance exists.
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Destroying duplicate instance of {typeof(T).Name}: {gameObject.name}. Original: {_instance.gameObject.name}");
            Destroy(gameObject);
        }
        // If _instance is not null AND it IS this object, it means it's already the correct singleton
        // (e.g., after a scene load, DontDestroyOnLoad kept it, and Awake is called again).
        // No action needed in this case, it's already properly set up.
    }

    /// <summary>
    /// A virtual method that derived classes can override to perform their specific initialization
    /// once the singleton is confirmed and properly set up. This is called from Awake.
    /// </summary>
    protected virtual void OnAwakeSingleton()
    {
        // Default implementation does nothing. Override in derived classes.
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// This sets the applicationIsQuitting flag to prevent the singleton from being recreated
    /// when the application is shutting down.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
        Debug.Log($"[Singleton] Application quitting, marking {typeof(T).Name} as destroyed.");
    }
}
```

---

### `GameSettingsManager.cs`

This script is a practical example of a concrete Singleton, inheriting from `SingletonWithLazyInit<T>`. It simulates a manager that holds game settings.

```csharp
using UnityEngine;

/// <summary>
/// Example concrete implementation of a SingletonWithLazyInit.
/// This class manages game settings like volume, player name, etc.
/// It automatically creates itself if not present and persists across scenes.
/// </summary>
public class GameSettingsManager : SingletonWithLazyInit<GameSettingsManager>
{
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float _musicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 0.8f;

    [Header("Player Settings")]
    [SerializeField] private string _playerName = "Player1";
    [SerializeField] private bool _invertYAxis = false;

    // Public properties to access settings. Read-only for simplicity.
    public float MasterVolume => _masterVolume;
    public float MusicVolume => _musicVolume;
    public float SfxVolume => _sfxVolume;
    public string PlayerName => _playerName;
    public bool InvertYAxis => _invertYAxis;

    /// <summary>
    /// This method is called from the base Singleton's Awake method
    /// after the instance has been confirmed as the one and only singleton.
    /// Use this for specific initialization logic for GameSettingsManager.
    /// </summary>
    protected override void OnAwakeSingleton()
    {
        Debug.Log($"[GameSettingsManager] Initializing settings for player '{_playerName}'.");
        LoadSettings(); // Load saved settings when the manager is initialized.
        ApplySettings(); // Apply initial settings.
    }

    /// <summary>
    /// Simulates loading settings from a persistent storage (e.g., PlayerPrefs, JSON file).
    /// </summary>
    public void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", _masterVolume);
        _musicVolume = PlayerPrefs.GetFloat("MusicVolume", _musicVolume);
        _sfxVolume = PlayerPrefs.GetFloat("SfxVolume", _sfxVolume);
        _playerName = PlayerPrefs.GetString("PlayerName", _playerName);
        _invertYAxis = PlayerPrefs.GetInt("InvertYAxis", _invertYAxis ? 1 : 0) == 1;

        Debug.Log("[GameSettingsManager] Settings loaded.");
        Debug.Log($"  Master Volume: {_masterVolume}, Music Volume: {_musicVolume}, SFX Volume: {_sfxVolume}");
        Debug.Log($"  Player Name: {_playerName}, Invert Y Axis: {_invertYAxis}");
    }

    /// <summary>
    /// Simulates saving current settings to persistent storage.
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
        PlayerPrefs.SetFloat("SfxVolume", _sfxVolume);
        PlayerPrefs.SetString("PlayerName", _playerName);
        PlayerPrefs.SetInt("InvertYAxis", _invertYAxis ? 1 : 0);
        PlayerPrefs.Save(); // Don't forget to save PlayerPrefs!

        Debug.Log("[GameSettingsManager] Settings saved.");
    }

    /// <summary>
    /// Applies the current settings (e.g., sets actual audio levels, configures input).
    /// </summary>
    public void ApplySettings()
    {
        AudioListener.volume = _masterVolume; // Example: apply master volume to global audio listener.
        // Imagine other settings being applied here (e.g., input manager sensitivity, UI updates).
        Debug.Log($"[GameSettingsManager] Settings applied. AudioListener volume set to: {AudioListener.volume}");
    }

    /// <summary>
    /// Example method to change a setting.
    /// </summary>
    /// <param name="newVolume">The new master volume (0.0 to 1.0).</param>
    public void SetMasterVolume(float newVolume)
    {
        _masterVolume = Mathf.Clamp01(newVolume);
        ApplySettings(); // Apply changes immediately.
        Debug.Log($"[GameSettingsManager] Master Volume set to: {_masterVolume}");
    }

    /// <summary>
    /// Example method to change player name.
    /// </summary>
    /// <param name="name">The new player name.</param>
    public void SetPlayerName(string name)
    {
        _playerName = name;
        Debug.Log($"[GameSettingsManager] Player Name set to: {_playerName}");
    }
}
```

---

### Example Usage (in another script)

To demonstrate how to use `GameSettingsManager`, create a new script called `GameSettingsTester.cs` and attach it to any GameObject in your scene.

```csharp
using UnityEngine;

/// <summary>
/// This script demonstrates how to access and use the GameSettingsManager Singleton.
/// </summary>
public class GameSettingsTester : MonoBehaviour
{
    void Start()
    {
        Debug.Log("--- GameSettingsTester Start ---");

        // Access the Singleton instance.
        // If GameSettingsManager doesn't exist in the scene, it will be created automatically.
        // This is the "lazy initialization" part.
        GameSettingsManager settings = GameSettingsManager.Instance;

        if (settings != null)
        {
            Debug.Log($"[GameSettingsTester] Current Player Name: {settings.PlayerName}");
            Debug.Log($"[GameSettingsTester] Current Master Volume: {settings.MasterVolume}");
            Debug.Log($"[GameSettingsTester] Current SFX Volume: {settings.SfxVolume}");

            // Modify a setting
            settings.SetMasterVolume(0.5f);
            settings.SetPlayerName("HeroPlayer");

            Debug.Log($"[GameSettingsTester] Updated Master Volume: {settings.MasterVolume}");
            Debug.Log($"[GameSettingsTester] Updated Player Name: {settings.PlayerName}");

            // Save settings (e.g., when player exits menu or game)
            settings.SaveSettings();

            // Simulate loading into a new scene or restarting the game
            // (settings would persist, and you'd call LoadSettings() again)
            Debug.Log("[GameSettingsTester] Simulating game restart/scene load...");
            // Even if you null out the local reference, the singleton still exists globally.
            settings = null; 

            // Accessing it again retrieves the *same* persistent instance.
            settings = GameSettingsManager.Instance;
            Debug.Log($"[GameSettingsTester] After 'restart', Player Name: {settings.PlayerName}"); // Should be "HeroPlayer"
            Debug.Log($"[GameSettingsTester] After 'restart', Master Volume: {settings.MasterVolume}"); // Should be 0.5f
            
            // PlayerPrefs stores it, so the next run it will also be these values.
            // You can also delete PlayerPrefs using PlayerPrefs.DeleteAll();
        }
        else
        {
            Debug.LogError("[GameSettingsTester] GameSettingsManager instance is null. Singleton failed to initialize.");
        }

        Debug.Log("--- GameSettingsTester End ---");
    }

    // You can also test in Update if needed, but Start is sufficient for initialization.
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         // Example: Toggle Y-axis inversion
    //         // GameSettingsManager.Instance.SetInvertYAxis(!GameSettingsManager.Instance.InvertYAxis);
    //         // GameSettingsManager.Instance.SaveSettings();
    //     }
    // }
}
```

---

### How to Use in Unity:

1.  **Create `SingletonWithLazyInit.cs`**: In your Unity project, create a new C# script named `SingletonWithLazyInit.cs` and copy the code for the `SingletonWithLazyInit<T>` class into it.
2.  **Create `GameSettingsManager.cs`**: Create another C# script named `GameSettingsManager.cs` and copy the code for the `GameSettingsManager` class into it.
3.  **Create `GameSettingsTester.cs`**: Create a third C# script named `GameSettingsTester.cs` and copy the example usage code into it.
4.  **Attach `GameSettingsTester`**: In any scene, create an empty GameObject (e.g., name it "Tester") and attach the `GameSettingsTester` script to it.
5.  **Run the Scene**: Play the scene. Observe the Console window:
    *   You'll see messages indicating `GameSettingsManager` being initialized (if it wasn't already in the scene).
    *   It will load default/saved settings, then apply changes made by `GameSettingsTester`, and finally save them.
    *   If you stop and restart the game, `GameSettingsManager` will reload the previously saved settings (e.g., "HeroPlayer", Master Volume 0.5f).
    *   You can optionally drag the `GameSettingsManager.cs` script onto a GameObject in your scene *manually* before running. The singleton logic will detect it and use that instance. If you then run the `GameSettingsTester`, it will still work as expected. If you *duplicate* the `GameSettingsManager` GameObject in the scene, the `Awake` logic will destroy the duplicate, ensuring only one persists.

This setup provides a robust, lazy-initialized, and persistent Singleton solution for your Unity projects, making it easy to manage global services and data.