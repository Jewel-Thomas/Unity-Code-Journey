// Unity Design Pattern Example: PluginLoaderSystem
// This script demonstrates the PluginLoaderSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the PluginLoaderSystem design pattern in Unity. This pattern allows you to create a modular architecture where different parts of your game (plugins) can be developed, registered, and managed independently by a central system.

**Core Concepts:**

1.  **`IPlugin` Interface:** Defines a common contract that all plugins must adhere to. This ensures that the `PluginLoader` can interact with any plugin in a standardized way.
2.  **Concrete Plugin Implementations:** Actual game systems (e.g., Achievement System, Localization System, Sound System) that implement the `IPlugin` interface. These are typically `MonoBehaviour` scripts in Unity.
3.  **`PluginLoader` System:** A central `MonoBehaviour` that discovers, registers, and provides access to all available plugins. It often acts as a Singleton for global access.

---

### **Setup Instructions (How to use this in Unity):**

1.  **Create C# Scripts:** Create new C# scripts in your Unity project, one for each class below, and paste the respective code into them.
    *   `IPlugin.cs`
    *   `PluginLoader.cs`
    *   `AchievementPlugin.cs`
    *   `LocalizationPlugin.cs`
    *   `SoundPlugin.cs`
2.  **Create PluginLoader GameObject:**
    *   In an empty Unity scene, create an empty GameObject.
    *   Rename it to `PluginLoader`.
    *   Attach the `PluginLoader.cs` script to this GameObject.
3.  **Create Plugin GameObjects:**
    *   Create three more empty GameObjects in the scene.
    *   Rename them to `Achievements`, `Localization`, and `Sound`.
    *   Attach `AchievementPlugin.cs` to the `Achievements` GameObject.
    *   Attach `LocalizationPlugin.cs` to the `Localization` GameObject.
    *   Attach `SoundPlugin.cs` to the `Sound` GameObject.
    *   (Optional: For the `Sound` GameObject, you might want to add an `AudioSource` component manually or let the plugin create one, and assign a `_backgroundMusic` AudioClip in its inspector if you have one.)
4.  **Run the Scene:** Play the scene. Observe the Unity console for messages demonstrating plugin discovery, initialization, and execution.

---

### **1. `IPlugin.cs`**
This defines the interface that all our plugins must implement.

```csharp
using UnityEngine; // Included for consistency, though not strictly required for interfaces
using System;      // For common types

namespace PluginLoaderSystem
{
    /// <summary>
    /// The core interface for any plugin within the PluginLoaderSystem.
    /// All concrete plugin implementations must adhere to this contract.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// A unique name for the plugin. This name is used by the PluginLoader
        /// to identify and retrieve specific plugins.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// Initializes the plugin. This method is called by the PluginLoader
        /// after the plugin has been discovered and registered.
        /// Use this for plugin-specific setup like loading data,
        /// configuring settings, or connecting to other systems.
        /// </summary>
        void InitializePlugin();

        /// <summary>
        /// Executes the main logic or a primary action of the plugin.
        /// This method serves as a generic entry point for the PluginLoader
        /// to tell a plugin to "do its thing."
        /// The actual implementation will vary greatly depending on the plugin's purpose.
        /// </summary>
        void ExecutePluginLogic();
    }
}
```

### **2. `PluginLoader.cs`**
This is the central component that finds, registers, and manages all plugins. It uses the Singleton pattern for easy global access.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extension methods like .OfType<T>()

namespace PluginLoaderSystem
{
    /// <summary>
    /// The central Plugin Loader System.
    /// This MonoBehaviour is responsible for discovering, registering, and managing
    /// all available plugins in the scene that implement the IPlugin interface.
    /// It acts as a Singleton for easy global access from any other script.
    /// </summary>
    public class PluginLoader : MonoBehaviour
    {
        // --- Singleton Pattern Implementation ---
        private static PluginLoader _instance;
        /// <summary>
        /// Provides a static access point to the single instance of the PluginLoader.
        /// If no instance exists in the scene, one will be created automatically.
        /// </summary>
        public static PluginLoader Instance
        {
            get
            {
                // If the instance is null, try to find it in the scene.
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PluginLoader>();

                    // If still no instance, create a new GameObject and add the component.
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(PluginLoader).Name);
                        _instance = singletonObject.AddComponent<PluginLoader>();
                        Debug.LogWarning("[PluginLoader] No PluginLoader found in scene. A new one was created automatically. " +
                                         "Consider adding a PluginLoader GameObject manually for explicit control.");
                    }
                }
                return _instance;
            }
        }

        [Header("Plugin Settings")]
        [Tooltip("If true, plugins will be automatically discovered and initialized during Awake().")]
        [SerializeField] private bool _autoLoadOnAwake = true;

        /// <summary>
        /// A dictionary to store all discovered and registered plugins.
        /// Plugins are mapped by their unique 'PluginName'.
        /// Using a dictionary allows for quick lookup by name.
        /// </summary>
        private Dictionary<string, IPlugin> _registeredPlugins = new Dictionary<string, IPlugin>();

        /// <summary>
        /// Public read-only access to the registered plugins.
        /// </summary>
        public IReadOnlyDictionary<string, IPlugin> RegisteredPlugins => _registeredPlugins;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Used here to enforce the Singleton pattern and trigger initial plugin loading.
        /// </summary>
        private void Awake()
        {
            // Enforce Singleton: If another instance already exists, destroy this one.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            // Make this GameObject persist across scene loads (optional, but common for managers).
            DontDestroyOnLoad(gameObject);

            // Automatically load plugins if enabled.
            if (_autoLoadOnAwake)
            {
                LoadAllPluginsInScene();
            }
        }

        /// <summary>
        /// This method is responsible for discovering all components in the scene
        /// that implement the <see cref="IPlugin"/> interface.
        /// It then registers them in the internal dictionary and calls their <see cref="IPlugin.InitializePlugin"/> method.
        /// This method can be called manually at any time to re-scan for plugins.
        /// </summary>
        [ContextMenu("Load All Plugins Now")] // Adds a convenient button in the Inspector
        public void LoadAllPluginsInScene()
        {
            Debug.Log("[PluginLoader] Starting plugin discovery...");

            // Clear any previously registered plugins before re-loading to avoid duplicates
            // and handle cases where plugins might have been added/removed.
            _registeredPlugins.Clear();

            // --- Plugin Discovery Mechanism ---
            // Find all MonoBehaviours in the entire scene (including inactive GameObjects).
            // This is a common way to find components, but can be performance-intensive
            // in very large scenes. For optimization, consider:
            // 1. Tagging plugin GameObjects and using GameObject.FindGameObjectsWithTag.
            // 2. Having plugins be children of a specific "Plugins" GameObject and searching only that hierarchy.
            // 3. Loading plugins from specific Prefabs in Resources folders.
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);

            // Filter the found MonoBehaviours to only include those that implement IPlugin.
            var scenePlugins = allMonoBehaviours.OfType<IPlugin>().ToList();

            if (scenePlugins.Count == 0)
            {
                Debug.LogWarning("[PluginLoader] No IPlugin implementations found in the current scene.");
                return;
            }

            // Register and initialize each discovered plugin.
            foreach (var plugin in scenePlugins)
            {
                // Ensure plugin names are unique.
                if (_registeredPlugins.ContainsKey(plugin.PluginName))
                {
                    // Log an error and skip duplicate plugins to prevent issues.
                    Debug.LogError($"[PluginLoader] A plugin with the name '{plugin.PluginName}' already exists. " +
                                   $"Duplicate plugin from GameObject '{((MonoBehaviour)plugin).name}' ignored. " +
                                   $"Plugin names must be unique to avoid conflicts.");
                    continue;
                }

                _registeredPlugins.Add(plugin.PluginName, plugin); // Register the plugin
                plugin.InitializePlugin(); // Call its initialization method
                Debug.Log($"[PluginLoader] Registered and Initialized plugin: '{plugin.PluginName}' " +
                          $"from GameObject '{((MonoBehaviour)plugin).name}'.");
            }

            Debug.Log($"[PluginLoader] Total plugins loaded: {_registeredPlugins.Count}");
        }

        /// <summary>
        /// Retrieves a specific plugin by its unique name.
        /// </summary>
        /// <param name="pluginName">The unique name of the plugin to retrieve.</param>
        /// <returns>The <see cref="IPlugin"/> instance if found; otherwise, returns null.</returns>
        public IPlugin GetPlugin(string pluginName)
        {
            if (_registeredPlugins.TryGetValue(pluginName, out IPlugin plugin))
            {
                return plugin;
            }
            Debug.LogWarning($"[PluginLoader] Plugin with name '{pluginName}' not found in registered plugins.");
            return null;
        }

        /// <summary>
        /// Executes the primary logic of a specific plugin identified by its name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin whose logic is to be executed.</param>
        public void ExecutePlugin(string pluginName)
        {
            IPlugin plugin = GetPlugin(pluginName);
            if (plugin != null)
            {
                plugin.ExecutePluginLogic();
            }
            // Warning already logged by GetPlugin if not found.
        }

        /// <summary>
        /// Iterates through all currently registered plugins and calls their
        /// <see cref="IPlugin.ExecutePluginLogic"/> method.
        /// </summary>
        public void ExecuteAllPlugins()
        {
            if (_registeredPlugins.Count == 0)
            {
                Debug.LogWarning("[PluginLoader] No plugins registered to execute.");
                return;
            }

            Debug.Log("\n--- PluginLoader: Executing all registered plugins ---");
            foreach (var pluginEntry in _registeredPlugins)
            {
                pluginEntry.Value.ExecutePluginLogic();
            }
            Debug.Log("--- PluginLoader: Finished executing all registered plugins ---\n");
        }

        /// <summary>
        /// Demonstrates how to use the PluginLoader system.
        /// This method runs automatically after plugins are loaded (if auto-load is enabled).
        /// In a real project, other scripts would call PluginLoader.Instance.GetPlugin()
        /// or PluginLoader.Instance.ExecutePlugin() as needed.
        /// </summary>
        private void Start()
        {
            if (_autoLoadOnAwake) // Only run demo if auto-load was active for a complete flow
            {
                Debug.Log("\n--- PluginLoader Demo Start ---");

                // --- Example 1: Get a specific plugin and call its custom method ---
                // We know 'Localization Plugin' is a LocalizationPlugin, so we can cast it
                // to access its specific methods.
                if (GetPlugin("Localization Plugin") is LocalizationPlugin localizationPlugin)
                {
                    Debug.Log($"Attempting to use '{localizationPlugin.PluginName}'...");
                    localizationPlugin.SetLanguage("es_ES");
                    string greeting = localizationPlugin.GetTranslatedText("greeting");
                    Debug.Log($"Localized greeting: {greeting}");
                    localizationPlugin.SetLanguage("en_US");
                    string welcome = localizationPlugin.GetTranslatedText("welcome");
                    Debug.Log($"Localized welcome: {welcome}");
                    localizationPlugin.GetTranslatedText("non_existent_key"); // Test fallback
                }

                // --- Example 2: Get another specific plugin and execute its generic logic ---
                IPlugin achievementPlugin = GetPlugin("Achievement Plugin");
                if (achievementPlugin != null)
                {
                    Debug.Log($"Attempting to execute '{achievementPlugin.PluginName}' via generic IPlugin method...");
                    achievementPlugin.ExecutePluginLogic(); // This will increment/check achievements
                    // We can also cast it to AchievementPlugin to call its specific methods if needed
                    // (AchievementPlugin castedPlugin = achievementPlugin as AchievementPlugin; castedPlugin?.UnlockAchievement("FIRST_KILL");)
                }

                // --- Example 3: Try to get a non-existent plugin (expect a warning) ---
                GetPlugin("NonExistentPlugin");

                // --- Example 4: Execute the generic logic of ALL registered plugins ---
                ExecuteAllPlugins();

                // --- Example 5: Use Sound Plugin's specific functionality ---
                if (GetPlugin("Sound Plugin") is SoundPlugin soundPlugin)
                {
                    Debug.Log($"Attempting to use '{soundPlugin.PluginName}'...");
                    soundPlugin.SetMasterVolume(0.5f);
                    // To play music/SFX, you'd typically pass an AudioClip reference.
                    // For this demo, its ExecutePluginLogic will try to play its default background music.
                    soundPlugin.ExecutePluginLogic(); // This will try to play background music
                    // In a real scenario, you'd have AudioClips assigned in the editor or loaded via Resources.
                    // soundPlugin.PlaySoundEffect(myExplosionSound, 0.7f);
                }

                Debug.Log("--- PluginLoader Demo End ---\n");
            }
            else
            {
                Debug.Log("[PluginLoader] Auto-load on Awake is disabled. You need to manually call " +
                          "LoadAllPluginsInScene() or use the Context Menu option to load plugins.");
            }
        }
    }
}
```

### **3. `AchievementPlugin.cs`**
An example plugin for an achievement system.

```csharp
using UnityEngine;

namespace PluginLoaderSystem
{
    /// <summary>
    /// An example concrete implementation of the IPlugin interface: Achievement System.
    /// This script would manage player achievements, tracking progress, and unlocking rewards.
    /// </summary>
    public class AchievementPlugin : MonoBehaviour, IPlugin
    {
        [Header("Plugin Configuration")]
        [Tooltip("The unique name for this plugin.")]
        [SerializeField] private string _pluginName = "Achievement Plugin";
        public string PluginName => _pluginName; // Exposes the unique plugin name

        [Header("Achievement Data")]
        [Tooltip("Number of achievements currently unlocked.")]
        [SerializeField] private int _achievementsUnlocked = 0;

        /// <summary>
        /// Called by the PluginLoader when this plugin is discovered and registered.
        /// Use this to load saved achievement data, set up initial state, etc.
        /// </summary>
        public void InitializePlugin()
        {
            Debug.Log($"[{PluginName}] Initializing. Currently {_achievementsUnlocked} achievements unlocked.");
            // Example: Load achievement data from player preferences or a save file
            // _achievementsUnlocked = PlayerPrefs.GetInt("AchievementsUnlocked", 0);
        }

        /// <summary>
        /// Executes the main logic of the achievement plugin.
        /// In a real game, this might be called periodically, or on specific game events,
        /// to check if new achievements have been earned.
        /// </summary>
        public void ExecutePluginLogic()
        {
            Debug.Log($"[{PluginName}] Executing logic: Checking for new achievements. " +
                      $"Currently have {_achievementsUnlocked} achievements unlocked.");

            // Simulate a chance to unlock an achievement
            if (Random.value > 0.7f) // 30% chance to unlock
            {
                _achievementsUnlocked++;
                Debug.Log($"[{PluginName}] !!! New achievement unlocked! Total: {_achievementsUnlocked}");
                // Example: Trigger UI notification, save data
                // PlayerPrefs.SetInt("AchievementsUnlocked", _achievementsUnlocked);
            }
        }

        // --- Plugin-specific methods (accessible after casting from IPlugin) ---

        /// <summary>
        /// Unlocks a specific achievement.
        /// </summary>
        /// <param name="achievementId">The unique ID of the achievement to unlock.</param>
        public void UnlockAchievement(string achievementId)
        {
            Debug.Log($"[{PluginName}] Manually unlocking achievement: {achievementId}");
            _achievementsUnlocked++;
            // Logic to mark achievement as unlocked, trigger UI, grant rewards, etc.
        }

        /// <summary>
        /// Returns the current count of unlocked achievements.
        /// </summary>
        public int GetAchievementsUnlockedCount()
        {
            return _achievementsUnlocked;
        }
    }
}
```

### **4. `LocalizationPlugin.cs`**
An example plugin for a localization (language) system.

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace PluginLoaderSystem
{
    /// <summary>
    /// Another example concrete implementation of the IPlugin interface: Localization System.
    /// This script manages game language settings and provides translated text strings.
    /// </summary>
    public class LocalizationPlugin : MonoBehaviour, IPlugin
    {
        [Header("Plugin Configuration")]
        [Tooltip("The unique name for this plugin.")]
        [SerializeField] private string _pluginName = "Localization Plugin";
        public string PluginName => _pluginName; // Exposes the unique plugin name

        [Header("Localization Settings")]
        [Tooltip("The currently active language code (e.g., 'en_US', 'es_ES').")]
        [SerializeField] private string _currentLanguage = "en_US";

        /// <summary>
        /// A nested dictionary to store translations: LanguageCode -> (Key -> TranslatedText).
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Called by the PluginLoader when this plugin is discovered and registered.
        /// Use this to load localization data (e.g., from JSON, CSV files) and set the initial language.
        /// </summary>
        public void InitializePlugin()
        {
            Debug.Log($"[{PluginName}] Initializing. Current language: {_currentLanguage}.");
            // Example: Load localization data from external files here
            LoadMockTranslations(); // Load some dummy data for the example
            // _currentLanguage = PlayerPrefs.GetString("SelectedLanguage", "en_US");
        }

        /// <summary>
        /// Executes the main logic of the localization plugin.
        /// For a localization system, this method might not have a direct "action" to perform,
        /// as its primary role is to provide data on demand. It can be used for logging or checks.
        /// </summary>
        public void ExecutePluginLogic()
        {
            Debug.Log($"[{PluginName}] Executing logic: Current language is '{_currentLanguage}'. " +
                      "This plugin primarily provides data, not continuous execution.");
            // Example: Check if language data needs to be reloaded, or update UI elements with current language.
        }

        // --- Plugin-specific methods (accessible after casting from IPlugin) ---

        /// <summary>
        /// Loads mock translation data for demonstration purposes.
        /// In a real project, this would load from files.
        /// </summary>
        private void LoadMockTranslations()
        {
            _translations["en_US"] = new Dictionary<string, string>
            {
                {"greeting", "Hello, World!"},
                {"welcome", "Welcome to the game!"},
                {"player_name", "Player"}
            };
            _translations["es_ES"] = new Dictionary<string, string>
            {
                {"greeting", "¡Hola, Mundo!"},
                {"welcome", "¡Bienvenido al juego!"},
                {"player_name", "Jugador"}
            };
            Debug.Log($"[{PluginName}] Mock translations loaded for {_translations.Keys.Count} languages.");
        }

        /// <summary>
        /// Retrieves the translated text for a given key in the current language.
        /// </summary>
        /// <param name="key">The key for the desired text string.</param>
        /// <returns>The translated text, or the key itself as a fallback with a warning if not found.</returns>
        public string GetTranslatedText(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var langDict))
            {
                if (langDict.TryGetValue(key, out string translatedText))
                {
                    return translatedText;
                }
            }
            Debug.LogWarning($"[{PluginName}] No translation found for key '{key}' in language '{_currentLanguage}'. " +
                             "Returning key as fallback.");
            return $"[{key}]"; // Return key itself as fallback
        }

        /// <summary>
        /// Sets the active language for the game.
        /// </summary>
        /// <param name="langCode">The language code to set (e.g., "en_US", "es_ES").</param>
        public void SetLanguage(string langCode)
        {
            if (_translations.ContainsKey(langCode))
            {
                _currentLanguage = langCode;
                Debug.Log($"[{PluginName}] Language set to: {_currentLanguage}");
                // Example: Trigger UI update events here if necessary
                // PlayerPrefs.SetString("SelectedLanguage", _currentLanguage);
            }
            else
            {
                Debug.LogError($"[{PluginName}] Language '{langCode}' not supported or translations not loaded.");
            }
        }
    }
}
```

### **5. `SoundPlugin.cs`**
An example plugin for a sound/audio system.

```csharp
using UnityEngine;

namespace PluginLoaderSystem
{
    /// <summary>
    /// A third example concrete implementation of the IPlugin interface: Sound System.
    /// This script manages background music, sound effects, and audio settings.
    /// </summary>
    [RequireComponent(typeof(AudioSource))] // Ensures an AudioSource is present on the GameObject
    public class SoundPlugin : MonoBehaviour, IPlugin
    {
        [Header("Plugin Configuration")]
        [Tooltip("The unique name for this plugin.")]
        [SerializeField] private string _pluginName = "Sound Plugin";
        public string PluginName => _pluginName; // Exposes the unique plugin name

        [Header("Audio Settings")]
        [Tooltip("The global master volume for all sounds played through this plugin.")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 0.8f;

        [Tooltip("The default background music clip to play.")]
        [SerializeField] private AudioClip _backgroundMusic;

        private AudioSource _audioSource; // Reference to the AudioSource component

        /// <summary>
        /// Called by the PluginLoader when this plugin is discovered and registered.
        /// Used to set up the AudioSource component and load initial volume settings.
        /// </summary>
        public void InitializePlugin()
        {
            // Get the AudioSource component. [RequireComponent] ensures it exists.
            _audioSource = GetComponent<AudioSource>();
            _audioSource.volume = _masterVolume;
            _audioSource.loop = true;          // Loop background music by default
            _audioSource.playOnAwake = false;  // We'll control playback programmatically

            Debug.Log($"[{PluginName}] Initializing. Master volume: {_masterVolume}.");
            // Example: Load volume settings from player preferences
            // _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        }

        /// <summary>
        /// Executes the main logic of the sound plugin.
        /// This could be used for periodic checks, like ensuring music is playing if it should be,
        /// or reacting to global game state changes related to audio.
        /// </summary>
        public void ExecutePluginLogic()
        {
            Debug.Log($"[{PluginName}] Executing logic: Checking sound settings and background music state.");
            // Example: If background music is assigned and not currently playing, start it.
            if (_backgroundMusic != null && !_audioSource.isPlaying)
            {
                PlayBackgroundMusic(_backgroundMusic);
            }
        }

        // --- Plugin-specific methods (accessible after casting from IPlugin) ---

        /// <summary>
        /// Plays a one-shot sound effect.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="volume">The volume for this specific sound effect (will be scaled by master volume).</param>
        public void PlaySoundEffect(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                // PlayOneShot allows multiple clips to overlap without stopping the current one.
                _audioSource.PlayOneShot(clip, volume * _masterVolume);
                Debug.Log($"[{PluginName}] Playing sound effect: {clip.name}");
            }
            else
            {
                Debug.LogWarning($"[{PluginName}] Attempted to play a null sound effect clip.");
            }
        }

        /// <summary>
        /// Starts or resumes playing background music.
        /// If a new clip is provided, it replaces the current one.
        /// </summary>
        /// <param name="musicClip">The audio clip to use as background music.</param>
        public void PlayBackgroundMusic(AudioClip musicClip)
        {
            if (musicClip == null)
            {
                Debug.LogWarning($"[{PluginName}] Cannot play null background music clip.");
                return;
            }

            // If a different clip is provided, or nothing is playing, set and play.
            if (_audioSource.clip != musicClip || !_audioSource.isPlaying)
            {
                _audioSource.clip = musicClip;
                _audioSource.Play();
                Debug.Log($"[{PluginName}] Playing background music: {musicClip.name}");
            }
            else if (_audioSource.clip == musicClip && _audioSource.isPlaying)
            {
                Debug.Log($"[{PluginName}] Background music '{musicClip.name}' is already playing.");
            }
            // If the same clip is set but paused, resume it
            else if (_audioSource.clip == musicClip && !_audioSource.isPlaying)
            {
                _audioSource.Play();
                Debug.Log($"[{PluginName}] Resuming background music: {musicClip.name}");
            }
        }

        /// <summary>
        /// Stops the currently playing background music.
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
                Debug.Log($"[{PluginName}] Stopping background music.");
            }
        }

        /// <summary>
        /// Sets the master volume for all sounds managed by this plugin.
        /// </summary>
        /// <param name="volume">The new master volume (0.0 to 1.0).</param>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume); // Ensure volume is between 0 and 1
            if (_audioSource != null)
            {
                _audioSource.volume = _masterVolume;
            }
            Debug.Log($"[{PluginName}] Master volume set to: {_masterVolume}");
            // Example: Save new volume setting
            // PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        }
    }
}
```