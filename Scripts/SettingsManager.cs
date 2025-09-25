// Unity Design Pattern Example: SettingsManager
// This script demonstrates the SettingsManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **SettingsManager design pattern** using the Singleton pattern for global access. It focuses on managing common game settings like volume, graphics quality, and control sensitivity, persisting them using `PlayerPrefs`.

This script is designed to be easily integrated into any Unity project. Just create a C# script named `SettingsManager.cs` and paste the code below. Then, create an empty GameObject in your scene (preferably in your initial scene or a persistent "Managers" scene) and attach this script to it. The `DontDestroyOnLoad` call will ensure it persists across scene changes.

---

### `SettingsManager.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
///     The SettingsManager is a Singleton pattern implementation responsible for
///     managing all game settings, providing a centralized access point,
///     persistence through PlayerPrefs, and an event system for reactive updates.
/// </summary>
/// <remarks>
///     **Design Pattern:** Singleton, Manager, Observer (via events)
///     
///     **Purpose:**
///     1.  **Centralized Access:** Provides a single, global point of access for all game settings.
///         Other scripts don't need to know where settings are stored or how they are managed;
///         they just interact with the SettingsManager.Instance.
///     2.  **Persistence:** Handles saving and loading settings to and from persistent storage
///         (e.g., Unity's PlayerPrefs). Settings are loaded on game start and saved on quit
///         or whenever a setting is changed.
///     3.  **Event-Driven Updates:** Notifies other game systems when a setting has changed.
///         This decouples the UI (which might change a setting) from the game logic (which
///         needs to react to that change, like adjusting volume).
///     4.  **Default Values:** Ensures that settings always have a sensible default value
///         if they haven't been set by the user yet.
/// </remarks>
public class SettingsManager : MonoBehaviour
{
    // =====================================================================================
    // Singleton Implementation
    // Ensures only one instance of SettingsManager exists throughout the application.
    // =====================================================================================
    private static SettingsManager _instance;

    /// <summary>
    ///     Gets the singleton instance of the SettingsManager.
    ///     Access this from any script using `SettingsManager.Instance`.
    /// </summary>
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<SettingsManager>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject(typeof(SettingsManager).Name);
                    _instance = singletonObject.AddComponent<SettingsManager>();
                }
            }
            return _instance;
        }
    }

    // =====================================================================================
    // Events
    // Other systems can subscribe to these events to react to setting changes.
    // =====================================================================================

    /// <summary>
    ///     Event fired whenever any setting is changed.
    ///     Subscribers receive the key (string) of the changed setting and its new value (object).
    /// </summary>
    public event Action<string, object> OnSettingChanged;

    // =====================================================================================
    // Internal Settings Cache
    // Stores the current state of settings in memory for quick access.
    // These dictionaries are populated on load and updated on set.
    // =====================================================================================
    private readonly Dictionary<string, float> _floatSettings = new();
    private readonly Dictionary<string, int> _intSettings = new();
    private readonly Dictionary<string, bool> _boolSettings = new();
    private readonly Dictionary<string, string> _stringSettings = new();

    // =====================================================================================
    // Unity Lifecycle Methods
    // =====================================================================================

    private void Awake()
    {
        // Singleton enforcement
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }

        _instance = this;
        // Keep the manager alive across scene changes
        DontDestroyOnLoad(gameObject);

        // Load all settings from PlayerPrefs (or apply defaults) when the manager starts
        LoadAllSettings();
    }

    private void OnApplicationQuit()
    {
        // PlayerPrefs.Save() is called after each Set() for immediate persistence.
        // This call acts as a final safeguard to ensure all pending writes are flushed.
        PlayerPrefs.Save();
        Debug.Log("SettingsManager: All settings saved on application quit.");
    }

    // =====================================================================================
    // Public Getters for Settings
    // Provide type-safe access to retrieve setting values.
    // =====================================================================================

    /// <summary>
    ///     Retrieves a float setting by its key.
    /// </summary>
    /// <param name="key">The key identifying the setting (e.g., SettingsKeys.MasterVolume).</param>
    /// <returns>The current float value of the setting.</returns>
    public float GetFloat(string key)
    {
        if (_floatSettings.TryGetValue(key, out float value))
        {
            return value;
        }
        Debug.LogWarning($"SettingsManager: Float setting with key '{key}' not found in cache. Returning default 0.");
        return 0f; // Should not happen if LoadAllSettings correctly initialized
    }

    /// <summary>
    ///     Retrieves an integer setting by its key.
    /// </summary>
    /// <param name="key">The key identifying the setting (e.g., SettingsKeys.GraphicsQuality).</param>
    /// <returns>The current integer value of the setting.</returns>
    public int GetInt(string key)
    {
        if (_intSettings.TryGetValue(key, out int value))
        {
            return value;
        }
        Debug.LogWarning($"SettingsManager: Int setting with key '{key}' not found in cache. Returning default 0.");
        return 0; // Should not happen if LoadAllSettings correctly initialized
    }

    /// <summary>
    ///     Retrieves a boolean setting by its key.
    /// </summary>
    /// <param name="key">The key identifying the setting (e.g., SettingsKeys.InvertYAxis).</param>
    /// <returns>The current boolean value of the setting.</returns>
    public bool GetBool(string key)
    {
        if (_boolSettings.TryGetValue(key, out bool value))
        {
            return value;
        }
        Debug.LogWarning($"SettingsManager: Bool setting with key '{key}' not found in cache. Returning default false.");
        return false; // Should not happen if LoadAllSettings correctly initialized
    }

    /// <summary>
    ///     Retrieves a string setting by its key.
    /// </summary>
    /// <param name="key">The key identifying the setting (e.g., SettingsKeys.Language).</param>
    /// <returns>The current string value of the setting.</returns>
    public string GetString(string key)
    {
        if (_stringSettings.TryGetValue(key, out string value))
        {
            return value;
        }
        Debug.LogWarning($"SettingsManager: String setting with key '{key}' not found in cache. Returning default empty string.");
        return string.Empty; // Should not happen if LoadAllSettings correctly initialized
    }

    // =====================================================================================
    // Public Setters for Settings
    // Update setting values, persist them, and notify subscribers.
    // =====================================================================================

    /// <summary>
    ///     Sets a float setting, updates its cached value, persists it, and notifies listeners.
    /// </summary>
    /// <param name="key">The key identifying the setting.</param>
    /// <param name="value">The new float value to set.</param>
    public void SetFloat(string key, float value)
    {
        if (!_floatSettings.ContainsKey(key) || _floatSettings[key] != value)
        {
            _floatSettings[key] = value;
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save(); // Save immediately for persistence
            OnSettingChanged?.Invoke(key, value);
            Debug.Log($"SettingsManager: Set float '{key}' to {value}");
        }
    }

    /// <summary>
    ///     Sets an integer setting, updates its cached value, persists it, and notifies listeners.
    /// </summary>
    /// <param name="key">The key identifying the setting.</param>
    /// <param name="value">The new integer value to set.</param>
    public void SetInt(string key, int value)
    {
        if (!_intSettings.ContainsKey(key) || _intSettings[key] != value)
        {
            _intSettings[key] = value;
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            OnSettingChanged?.Invoke(key, value);
            Debug.Log($"SettingsManager: Set int '{key}' to {value}");
        }
    }

    /// <summary>
    ///     Sets a boolean setting, updates its cached value, persists it, and notifies listeners.
    ///     Booleans are stored as 0 or 1 in PlayerPrefs.
    /// </summary>
    /// <param name="key">The key identifying the setting.</param>
    /// <param name="value">The new boolean value to set.</param>
    public void SetBool(string key, bool value)
    {
        if (!_boolSettings.ContainsKey(key) || _boolSettings[key] != value)
        {
            _boolSettings[key] = value;
            PlayerPrefs.SetInt(key, value ? 1 : 0); // Convert bool to int for PlayerPrefs
            PlayerPrefs.Save();
            OnSettingChanged?.Invoke(key, value);
            Debug.Log($"SettingsManager: Set bool '{key}' to {value}");
        }
    }

    /// <summary>
    ///     Sets a string setting, updates its cached value, persists it, and notifies listeners.
    /// </summary>
    /// <param name="key">The key identifying the setting.</param>
    /// <param name="value">The new string value to set.</param>
    public void SetString(string key, string value)
    {
        if (!_stringSettings.ContainsKey(key) || _stringSettings[key] != value)
        {
            _stringSettings[key] = value;
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            OnSettingChanged?.Invoke(key, value);
            Debug.Log($"SettingsManager: Set string '{key}' to '{value}'");
        }
    }

    // =====================================================================================
    // Helper Methods
    // For loading all settings, getting default values, and resetting.
    // =====================================================================================

    /// <summary>
    ///     Loads all known settings from PlayerPrefs into the internal cache.
    ///     If a setting is not found in PlayerPrefs, its default value is used.
    /// </summary>
    private void LoadAllSettings()
    {
        Debug.Log("SettingsManager: Loading all settings...");

        // Load Float Settings
        foreach (string key in SettingsKeys.FloatKeys)
        {
            _floatSettings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : GetDefaultFloat(key);
            Debug.Log($"   Loaded Float '{key}': {_floatSettings[key]} (Source: {(PlayerPrefs.HasKey(key) ? "PlayerPrefs" : "Default")})");
        }

        // Load Int Settings
        foreach (string key in SettingsKeys.IntKeys)
        {
            _intSettings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : GetDefaultInt(key);
            Debug.Log($"   Loaded Int '{key}': {_intSettings[key]} (Source: {(PlayerPrefs.HasKey(key) ? "PlayerPrefs" : "Default")})");
        }

        // Load Bool Settings (stored as int 0 or 1)
        foreach (string key in SettingsKeys.BoolKeys)
        {
            _boolSettings[key] = PlayerPrefs.HasKey(key) ? (PlayerPrefs.GetInt(key) == 1) : GetDefaultBool(key);
            Debug.Log($"   Loaded Bool '{key}': {_boolSettings[key]} (Source: {(PlayerPrefs.HasKey(key) ? "PlayerPrefs" : "Default")})");
        }

        // Load String Settings
        foreach (string key in SettingsKeys.StringKeys)
        {
            _stringSettings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : GetDefaultString(key);
            Debug.Log($"   Loaded String '{key}': '{_stringSettings[key]}' (Source: {(PlayerPrefs.HasKey(key) ? "PlayerPrefs" : "Default")})");
        }

        Debug.Log("SettingsManager: All settings loaded.");
    }

    /// <summary>
    ///     Resets all settings to their default values and clears them from PlayerPrefs.
    /// </summary>
    public void ResetToDefaults()
    {
        Debug.Log("SettingsManager: Resetting all settings to defaults...");

        // Clear all known settings from PlayerPrefs
        foreach (string key in SettingsKeys.FloatKeys) PlayerPrefs.DeleteKey(key);
        foreach (string key in SettingsKeys.IntKeys) PlayerPrefs.DeleteKey(key);
        foreach (string key in SettingsKeys.BoolKeys) PlayerPrefs.DeleteKey(key);
        foreach (string key in SettingsKeys.StringKeys) PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save(); // Ensure deletions are saved

        // Reload all settings, which will now pull default values
        LoadAllSettings();

        // Notify subscribers that various settings have changed (implicitly by LoadAllSettings)
        // For a more explicit notification, one could iterate through all loaded defaults and invoke OnSettingChanged.
        Debug.Log("SettingsManager: All settings reset to defaults.");
    }

    // =====================================================================================
    // Default Value Definitions
    // Defines the initial values for settings if not found in PlayerPrefs.
    // =====================================================================================

    private float GetDefaultFloat(string key)
    {
        return key switch
        {
            SettingsKeys.MasterVolume => 0.75f,
            SettingsKeys.MusicVolume => 0.6f,
            SettingsKeys.SfxVolume => 0.8f,
            SettingsKeys.MouseSensitivity => 0.5f,
            _ => 0f,
        };
    }

    private int GetDefaultInt(string key)
    {
        return key switch
        {
            SettingsKeys.GraphicsQuality => 2, // 0: Low, 1: Medium, 2: High
            _ => 0,
        };
    }

    private bool GetDefaultBool(string key)
    {
        return key switch
        {
            SettingsKeys.InvertYAxis => false,
            _ => false,
        };
    }

    private string GetDefaultString(string key)
    {
        return key switch
        {
            SettingsKeys.Language => "en", // Default to English
            _ => string.Empty,
        };
    }

    // =====================================================================================
    // Setting Keys Definition (Best Practice)
    // Using a static class with const strings avoids "magic strings" and provides
    // a clear, centralized list of all available setting keys.
    // =====================================================================================

    /// <summary>
    ///     A static class containing constant string keys for all game settings.
    ///     Using these constants prevents typos and makes code more readable.
    /// </summary>
    public static class SettingsKeys
    {
        // Float Settings
        public const string MasterVolume = "MasterVolume";
        public const string MusicVolume = "MusicVolume";
        public const string SfxVolume = "SFXVolume";
        public const string MouseSensitivity = "MouseSensitivity";

        // Integer Settings
        public const string GraphicsQuality = "GraphicsQuality"; // Example: 0=Low, 1=Medium, 2=High

        // Boolean Settings
        public const string InvertYAxis = "InvertYAxis";
        public const string FullscreenMode = "FullscreenMode";

        // String Settings
        public const string Language = "Language"; // Example: "en", "es", "fr"

        /// <summary>
        ///     Helper list of all float setting keys for easy iteration (e.g., during loading/resetting).
        /// </summary>
        public static readonly List<string> FloatKeys = new()
        {
            MasterVolume, MusicVolume, SfxVolume, MouseSensitivity
        };

        /// <summary>
        ///     Helper list of all int setting keys.
        /// </summary>
        public static readonly List<string> IntKeys = new()
        {
            GraphicsQuality
        };

        /// <summary>
        ///     Helper list of all bool setting keys.
        /// </summary>
        public static readonly List<string> BoolKeys = new()
        {
            InvertYAxis, FullscreenMode
        };

        /// <summary>
        ///     Helper list of all string setting keys.
        /// </summary>
        public static readonly List<string> StringKeys = new()
        {
            Language
        };
    }
}


// =====================================================================================
// Example Usage (How to implement it in other scripts)
// =====================================================================================

/*
/// <summary>
///     Example script demonstrating how to interact with the SettingsManager.
///     Attach this to any GameObject to test.
/// </summary>
public class SettingsExampleUsage : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("--- SettingsManager Example Usage ---");

        // --- 1. Getting Settings ---
        float currentMasterVolume = SettingsManager.Instance.GetFloat(SettingsManager.SettingsKeys.MasterVolume);
        int currentGraphicsQuality = SettingsManager.Instance.GetInt(SettingsManager.SettingsKeys.GraphicsQuality);
        bool isInvertY = SettingsManager.Instance.GetBool(SettingsManager.SettingsKeys.InvertYAxis);
        string currentLanguage = SettingsManager.Instance.GetString(SettingsManager.SettingsKeys.Language);

        Debug.Log($"Current Master Volume: {currentMasterVolume}");
        Debug.Log($"Current Graphics Quality: {currentGraphicsQuality} (0=Low, 1=Medium, 2=High)");
        Debug.Log($"Invert Y-Axis: {isInvertY}");
        Debug.Log($"Current Language: {currentLanguage}");

        // --- 2. Subscribing to Setting Changes ---
        // This is crucial for other game systems to react dynamically.
        SettingsManager.Instance.OnSettingChanged += HandleSettingChanged;

        // --- 3. Setting New Values ---
        // You might do this from UI elements (sliders, toggles, dropdowns) or game logic.
        Debug.Log("\n--- Changing Settings ---");
        SettingsManager.Instance.SetFloat(SettingsManager.SettingsKeys.MasterVolume, 0.5f);
        SettingsManager.Instance.SetInt(SettingsManager.SettingsKeys.GraphicsQuality, 1); // Medium
        SettingsManager.Instance.SetBool(SettingsManager.SettingsKeys.InvertYAxis, true);
        SettingsManager.Instance.SetString(SettingsManager.SettingsKeys.Language, "es");

        // Setting a value that is already the current value will not trigger an event or save.
        SettingsManager.Instance.SetFloat(SettingsManager.SettingsKeys.MasterVolume, 0.5f); // No output expected

        // --- 4. Verifying Changes ---
        Debug.Log("\n--- Verifying Changes ---");
        Debug.Log($"New Master Volume: {SettingsManager.Instance.GetFloat(SettingsManager.SettingsKeys.MasterVolume)}");
        Debug.Log($"New Graphics Quality: {SettingsManager.Instance.GetInt(SettingsManager.SettingsKeys.GraphicsQuality)}");
        Debug.Log($"New Invert Y-Axis: {SettingsManager.Instance.GetBool(SettingsManager.SettingsKeys.InvertYAxis)}");
        Debug.Log($"New Language: {SettingsManager.Instance.GetString(SettingsManager.SettingsKeys.Language)}");

        // --- 5. Resetting to Defaults (Optional) ---
        // This could be a button in your options menu.
        // Debug.Log("\n--- Resetting Settings to Defaults ---");
        // SettingsManager.Instance.ResetToDefaults();
        // Debug.Log($"Master Volume after Reset: {SettingsManager.Instance.GetFloat(SettingsManager.SettingsKeys.MasterVolume)}");
    }

    /// <summary>
    ///     Handler for the OnSettingChanged event.
    ///     This method would contain the logic to update specific game systems.
    /// </summary>
    /// <param name="key">The key of the setting that changed.</param>
    /// <param name="newValue">The new value of the setting (requires casting).</param>
    private void HandleSettingChanged(string key, object newValue)
    {
        Debug.Log($"<color=cyan>Setting Changed Event: '{key}' now has value '{newValue}'</color>");

        switch (key)
        {
            case SettingsManager.SettingsKeys.MasterVolume:
                float newVolume = (float)newValue;
                AudioListener.volume = newVolume; // Example: Update global audio volume
                Debug.Log($"   -> AudioListener volume updated to: {newVolume}");
                break;
            case SettingsManager.SettingsKeys.GraphicsQuality:
                int newQuality = (int)newValue;
                QualitySettings.SetQualityLevel(newQuality); // Example: Update graphics quality
                Debug.Log($"   -> Graphics Quality updated to level: {newQuality}");
                break;
            case SettingsManager.SettingsKeys.InvertYAxis:
                bool invert = (bool)newValue;
                // Example: Update player controller input logic
                Debug.Log($"   -> Player controller Y-axis inversion set to: {invert}");
                break;
            case SettingsManager.SettingsKeys.Language:
                string newLang = (string)newValue;
                // Example: Update UI texts to the new language
                Debug.Log($"   -> UI Language updated to: {newLang}");
                break;
            default:
                Debug.Log($"   -> No specific handler for '{key}'.");
                break;
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks, especially with singletons.
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingChanged -= HandleSettingChanged;
        }
    }
}
*/