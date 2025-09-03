// Unity Design Pattern Example: PlayerPrefsManager
// This script demonstrates the PlayerPrefsManager pattern in Unity
// Generated automatically - ready to use in your Unity project

Here's a complete, practical C# Unity example for the 'PlayerPrefsManager' design pattern. This script is designed to be dropped directly into a Unity project.

---

```csharp
using UnityEngine;
using System; // Required for System.DateTime
using System.Globalization; // Required for DateTimeStyles.RoundtripKind

/// <summary>
/// A static manager class for abstracting and centralizing all interactions with Unity's PlayerPrefs.
/// This pattern promotes type safety, provides default values, centralizes key definitions,
/// and makes PlayerPrefs usage more robust and maintainable.
/// </summary>
/// <remarks>
/// PlayerPrefs are stored in a platform-dependent way:
/// - Windows: Registry (HKCU\Software\[CompanyName]\[ProductName])
/// - macOS: Plist files (~/Library/Preferences/[bundle identifier].plist)
/// - Linux: INI files (~/.config/unity3d/[CompanyName]/[ProductName]/prefs)
/// - iOS/Android: XML files / SharedPreferences
///
/// **Why use a PlayerPrefsManager?**
/// 1.  **Type Safety:** PlayerPrefs only natively supports `int`, `float`, `string`. This manager provides `bool`, `DateTime`, etc. by converting them.
/// 2.  **Default Values:** Every `Get` method includes a default value, eliminating the need for `HasKey` checks and providing clean fallback behavior.
/// 3.  **Centralized Keys:** All PlayerPrefs keys are defined as `const string` constants in one place, preventing typos and simplifying refactoring.
/// 4.  **Encapsulation:** All PlayerPrefs logic is contained, keeping game logic clean and focused on game mechanics.
/// 5.  **Maintainability:** Easier to modify, debug, and understand how save data is handled.
/// 6.  **Future-proofing:** If you decide to switch from PlayerPrefs to another saving system (e.g., JSON files, database), you only need to modify this manager class, not every script that interacts with saved data.
/// </remarks>
public static class PlayerPrefsManager
{
    // =================================================================================
    // MARK: - PlayerPrefs Keys Definition
    // All keys used for PlayerPrefs should be defined here as constants.
    // This practice is crucial for preventing typos and allows for easy refactoring
    // of keys across your entire project if needed.
    // Use a clear naming convention, e.g., PP_CATEGORY_SETTINGNAME, to keep them organized.
    // =================================================================================

    #region -- Game Configuration Keys --
    public const string PP_AUDIO_MASTER_VOLUME = "Audio_MasterVolume";
    public const string PP_AUDIO_SFX_VOLUME = "Audio_SfxVolume";
    public const string PP_AUDIO_MUSIC_VOLUME = "Audio_MusicVolume";
    public const string PP_AUDIO_VIBRATION_ENABLED = "Audio_VibrationEnabled"; // Stored as int (0 for false, 1 for true)

    public const string PP_GRAPHICS_QUALITY_LEVEL = "Graphics_QualityLevel";
    public const string PP_GRAPHICS_FULLSCREEN = "Graphics_Fullscreen"; // Stored as int (0 for false, 1 for true)
    #endregion

    #region -- Game Progress & Player Data Keys --
    public const string PP_PLAYER_NAME = "Player_Name";
    public const string PP_PLAYER_LEVEL = "Player_CurrentLevel";
    public const string PP_PLAYER_EXPERIENCE = "Player_ExperiencePoints";
    public const string PP_PLAYER_COINS = "Player_TotalCoins";
    public const string PP_PLAYER_HIGHSCORE = "Player_Highscore";
    public const string PP_PLAYER_TUTORIAL_COMPLETED = "Player_TutorialCompleted"; // Stored as int (0 for false, 1 for true)
    public const string PP_GAME_LAST_PLAYED_DATE = "Game_LastPlayedDate"; // Stored as string (ISO 8601 format)
    #endregion

    // =================================================================================
    // MARK: - Core PlayerPrefs Methods
    // These methods provide a type-safe wrapper around Unity's PlayerPrefs functions.
    // Each 'Get' method includes a default value, ensuring no unexpected behavior
    // (like returning 0 for a missing int or null for a missing string) if a key doesn't exist.
    // =================================================================================

    #region -- Integers (int) --

    /// <summary>
    /// Sets an integer value for the specified key in PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference (use constants from PlayerPrefsManager).</param>
    /// <param name="value">The integer value to store.</param>
    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    /// <summary>
    /// Gets an integer value for the specified key from PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <returns>The integer value associated with the key, or the defaultValue if the key is not found.</returns>
    public static int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    /// <summary>
    /// Increments an integer value by a specified amount and returns the new value.
    /// If the key doesn't exist, it initializes with `initialValueIfNew` before incrementing.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="increment">The amount to increment by (can be negative for decrement).</param>
    /// <param name="initialValueIfNew">The value to use if the key does not exist before applying the increment.</param>
    /// <returns>The new, incremented integer value.</returns>
    public static int IncrementInt(string key, int increment = 1, int initialValueIfNew = 0)
    {
        int currentValue = GetInt(key, initialValueIfNew);
        currentValue += increment;
        SetInt(key, currentValue);
        return currentValue;
    }

    #endregion

    #region -- Floats (float) --

    /// <summary>
    /// Sets a float value for the specified key in PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="value">The float value to store.</param>
    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    /// <summary>
    /// Gets a float value for the specified key from PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <returns>The float value associated with the key, or the defaultValue if the key is not found.</returns>
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    #endregion

    #region -- Strings (string) --

    /// <summary>
    /// Sets a string value for the specified key in PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="value">The string value to store.</param>
    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    /// <summary>
    /// Gets a string value for the specified key from PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <returns>The string value associated with the key, or the defaultValue if the key is not found.</returns>
    public static string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    #endregion

    #region -- Booleans (bool) --
    // PlayerPrefs does not natively support bools. We store them as integers: 0 for false, 1 for true.

    /// <summary>
    /// Sets a boolean value for the specified key in PlayerPrefs.
    /// Stored as an integer: 0 for false, 1 for true.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="value">The boolean value to store.</param>
    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    /// <summary>
    /// Gets a boolean value for the specified key from PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <returns>The boolean value associated with the key, or the defaultValue if the key is not found.</returns>
    public static bool GetBool(string key, bool defaultValue = false)
    {
        // If the key doesn't exist, GetInt returns the mapped default value.
        // We then check if the retrieved int is 1 (true).
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    #endregion

    #region -- DateTimes (System.DateTime) --
    // PlayerPrefs does not natively support DateTime. We store them as ISO 8601 formatted strings
    // for robust parsing and timezone handling.

    /// <summary>
    /// Sets a DateTime value for the specified key in PlayerPrefs.
    /// Stored as an ISO 8601 formatted string (e.g., "2023-10-27T10:30:00.0000000Z").
    /// Converts to UTC before storing to prevent timezone issues across different machines/regions.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="value">The DateTime value to store.</param>
    public static void SetDateTime(string key, DateTime value)
    {
        PlayerPrefs.SetString(key, value.ToUniversalTime().ToString("o")); // "o" is the round-trip format specifier (ISO 8601)
    }

    /// <summary>
    /// Gets a DateTime value for the specified key from PlayerPrefs.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <param name="defaultValue">The value to return if the key does not exist or parsing fails.</param>
    /// <returns>The DateTime value associated with the key, or the defaultValue if the key is not found or invalid.
    /// The returned DateTime will be converted back to local time.</returns>
    public static DateTime GetDateTime(string key, DateTime defaultValue)
    {
        string dateTimeString = PlayerPrefs.GetString(key, string.Empty);
        if (!string.IsNullOrEmpty(dateTimeString))
        {
            // Try to parse using the round-trip format, respecting the original DateTimeKind.
            if (DateTime.TryParseExact(dateTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedDateTime))
            {
                return parsedDateTime.ToLocalTime(); // Convert back to local time for typical application use
            }
            Debug.LogWarning($"PlayerPrefsManager: Failed to parse DateTime for key '{key}'. Returning default value.");
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets a DateTime value for the specified key from PlayerPrefs.
    /// Returns `DateTime.MinValue` if the key is not found or parsing fails.
    /// </summary>
    /// <param name="key">The unique key to identify this preference.</param>
    /// <returns>The DateTime value associated with the key, or `DateTime.MinValue` if the key is not found or invalid.
    /// The returned DateTime will be converted back to local time.</returns>
    public static DateTime GetDateTime(string key)
    {
        return GetDateTime(key, DateTime.MinValue);
    }

    #endregion

    #region -- Generic PlayerPrefs Operations --

    /// <summary>
    /// Checks if a specific key exists in PlayerPrefs.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// Deletes a specific key and its corresponding value from PlayerPrefs.
    /// </summary>
    /// <param name="key">The key to delete.</param>
    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        Debug.Log($"PlayerPrefsManager: Deleted key '{key}'. Remember to call Save() to persist changes.");
    }

    /// <summary>
    /// Deletes all keys and values from PlayerPrefs.
    /// **USE WITH EXTREME CAUTION**, as this will wipe all saved data for your application.
    /// </summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefsManager: All PlayerPrefs data has been deleted. Remember to call Save() to persist changes.");
    }

    /// <summary>
    /// Writes all modified PlayerPrefs to disk.
    /// PlayerPrefs are not automatically saved to disk when modified; they are usually cached in memory.
    /// It's a good practice to call this method after making multiple changes or before the application quits/pauses.
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefsManager: PlayerPrefs data saved to disk.");
    }

    #endregion

    // =================================================================================
    // MARK: - Example Usage in Comments
    // This section demonstrates how to use the PlayerPrefsManager in your game scripts.
    // To see it in action, copy the 'MyGameSettings' class below into a new C# script
    // (e.g., 'MyGameSettings.cs'), attach it to a GameObject in your scene, and run the game.
    // =================================================================================

    /*
    // --- Example Monobehaviour Script (e.g., MyGameSettings.cs) ---

    using UnityEngine;
    using System; // For DateTime

    public class MyGameSettings : MonoBehaviour
    {
        // Example variables to hold current game settings in memory
        [Header("Current Game Settings (In-Memory)")]
        [SerializeField] private float masterVolume;
        [SerializeField] private int currentLevel;
        [SerializeField] private bool tutorialCompleted;
        [SerializeField] private string playerName;
        [SerializeField] private DateTime lastPlayedDate;
        [SerializeField] private int totalCoins;

        void Awake()
        {
            // Load settings when the game starts or when this component becomes active.
            // This ensures your in-memory variables reflect the saved state.
            LoadAllGameSettings();
        }

        void OnApplicationQuit()
        {
            // It's critical to save PlayerPrefs changes, especially when the application exits.
            // PlayerPrefs.Save() is not guaranteed to be called automatically on all platforms.
            SaveAllGameSettings();
        }

        /// <summary>
        /// Loads all game settings from the PlayerPrefsManager into in-memory variables.
        /// </summary>
        public void LoadAllGameSettings()
        {
            Debug.Log("<color=cyan>--- Loading Game Settings ---</color>");

            // --- Audio Settings ---
            // Get Master Volume, default to 0.7f if not previously set
            masterVolume = PlayerPrefsManager.GetFloat(PlayerPrefsManager.PP_AUDIO_MASTER_VOLUME, 0.7f);
            Debug.Log($"Loaded Master Volume: {masterVolume}");

            // Get SFX Volume, default to 1.0f
            float sfxVolume = PlayerPrefsManager.GetFloat(PlayerPrefsManager.PP_AUDIO_SFX_VOLUME, 1.0f);
            Debug.Log($"Loaded SFX Volume: {sfxVolume}");

            // Get Vibration Enabled status, default to true
            bool vibration = PlayerPrefsManager.GetBool(PlayerPrefsManager.PP_AUDIO_VIBRATION_ENABLED, true);
            Debug.Log($"Loaded Vibration Enabled: {vibration}");

            // --- Game Progress ---
            // Get Current Level, default to 1
            currentLevel = PlayerPrefsManager.GetInt(PlayerPrefsManager.PP_PLAYER_LEVEL, 1);
            Debug.Log($"Loaded Current Level: {currentLevel}");

            // Get Total Coins, default to 0
            totalCoins = PlayerPrefsManager.GetInt(PlayerPrefsManager.PP_PLAYER_COINS, 0);
            Debug.Log($"Loaded Total Coins: {totalCoins}");

            // Get Tutorial Completion status, default to false
            tutorialCompleted = PlayerPrefsManager.GetBool(PlayerPrefsManager.PP_PLAYER_TUTORIAL_COMPLETED, false);
            Debug.Log($"Loaded Tutorial Completed: {tutorialCompleted}");

            // --- Player Info ---
            // Get Player Name, default to "New Player"
            playerName = PlayerPrefsManager.GetString(PlayerPrefsManager.PP_PLAYER_NAME, "New Player");
            Debug.Log($"Loaded Player Name: {playerName}");

            // Get Last Played Date, default to DateTime.MinValue if not found/invalid
            lastPlayedDate = PlayerPrefsManager.GetDateTime(PlayerPrefsManager.PP_GAME_LAST_PLAYED_DATE, DateTime.MinValue);
            Debug.Log($"Loaded Last Played Date: {lastPlayedDate}");
            
            Debug.Log("<color=cyan>--- Settings Loading Complete ---</color>");
        }

        /// <summary>
        /// Saves all current in-memory game settings to the PlayerPrefsManager.
        /// </summary>
        public void SaveAllGameSettings()
        {
            Debug.Log("<color=green>--- Saving Game Settings ---</color>");

            // --- Update and Save current states ---
            PlayerPrefsManager.SetFloat(PlayerPrefsManager.PP_AUDIO_MASTER_VOLUME, masterVolume);
            PlayerPrefsManager.SetInt(PlayerPrefsManager.PP_PLAYER_LEVEL, currentLevel);
            PlayerPrefsManager.SetBool(PlayerPrefsManager.PP_PLAYER_TUTORIAL_COMPLETED, tutorialCompleted);
            PlayerPrefsManager.SetString(PlayerPrefsManager.PP_PLAYER_NAME, playerName);
            PlayerPrefsManager.SetInt(PlayerPrefsManager.PP_PLAYER_COINS, totalCoins);
            
            // Update the 'last played' date to now
            PlayerPrefsManager.SetDateTime(PlayerPrefsManager.PP_GAME_LAST_PLAYED_DATE, DateTime.Now);

            // Call Save() to ensure all changes are written to disk.
            // This is essential after making any modifications you want to persist.
            PlayerPrefsManager.Save();
            Debug.Log("<color=green>--- Settings Saving Complete ---</color>");
        }

        /// <summary>
        /// Example method to simulate a player completing the tutorial.
        /// </summary>
        [ContextMenu("Complete Tutorial")] // Adds a button to the inspector for easy testing
        public void SetTutorialCompleted()
        {
            tutorialCompleted = true; // Update in-memory state
            PlayerPrefsManager.SetBool(PlayerPrefsManager.PP_PLAYER_TUTORIAL_COMPLETED, true);
            PlayerPrefsManager.Save(); // Save immediately for critical changes
            Debug.Log("Tutorial marked as completed and saved.");
        }

        /// <summary>
        /// Example method to change the master volume.
        /// </summary>
        /// <param name="newVolume">The new volume level (0.0 to 1.0).</param>
        [ContextMenu("Set Master Volume to 0.5")]
        public void SetMasterVolume(float newVolume = 0.5f)
        {
            masterVolume = Mathf.Clamp01(newVolume); // Ensure volume is between 0 and 1
            PlayerPrefsManager.SetFloat(PlayerPrefsManager.PP_AUDIO_MASTER_VOLUME, masterVolume);
            PlayerPrefsManager.Save();
            Debug.Log($"Master Volume set to: {masterVolume} and saved.");
        }

        /// <summary>
        /// Example method to change the player's name.
        /// </summary>
        /// <param name="newName">The new name for the player.</param>
        [ContextMenu("Change Player Name to 'Hero'")]
        public void ChangePlayerName(string newName = "Hero")
        {
            playerName = newName;
            PlayerPrefsManager.SetString(PlayerPrefsManager.PP_PLAYER_NAME, playerName);
            PlayerPrefsManager.Save();
            Debug.Log($"Player name changed to: '{playerName}' and saved.");
        }

        /// <summary>
        /// Example method to add coins to the player's total.
        /// </summary>
        [ContextMenu("Add 100 Coins")]
        public void AddCoins(int amount = 100)
        {
            totalCoins = PlayerPrefsManager.IncrementInt(PlayerPrefsManager.PP_PLAYER_COINS, amount);
            PlayerPrefsManager.Save();
            Debug.Log($"Added {amount} coins. Total coins: {totalCoins}. Saved.");
        }


        /// <summary>
        /// Resets all PlayerPrefs for the application.
        /// </summary>
        [ContextMenu("!!! DANGER: Delete All PlayerPrefs !!!")]
        public void ResetAllPlayerPrefs()
        {
            PlayerPrefsManager.DeleteAll();
            PlayerPrefsManager.Save(); // Ensure the deletion is written to disk
            Debug.LogWarning("All PlayerPrefs have been deleted and saved. Reloading default settings.");
            LoadAllGameSettings(); // Reload defaults after clearing
        }
    }
    */
}
```