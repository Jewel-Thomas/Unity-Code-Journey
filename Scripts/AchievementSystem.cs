// Unity Design Pattern Example: AchievementSystem
// This script demonstrates the AchievementSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script provides a complete and practical implementation of the **Achievement System design pattern** for Unity projects. It focuses on clarity, reusability, and adherence to Unity best practices, making it suitable for educational purposes and real-world game development.

The Achievement System pattern centralizes the management of achievements, decoupling the achievement logic from the core game mechanics. Game systems simply report events or progress to the Achievement System, which then handles checking conditions, unlocking achievements, and notifying relevant parts of the game (e.g., UI).

---

## `AchievementSystem.cs`

To use this script:
1.  Create an empty GameObject in your first scene (e.g., "AchievementManager").
2.  Attach this `AchievementSystem.cs` script to it.
3.  In the Inspector, populate the `Achievement Definitions` list with your desired achievements.
4.  Refer to the "Example Usage" section in the comments below for how to integrate it with your game logic and UI.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extension methods like .ToList(), .Where()

/// <summary>
/// Represents a single achievement instance with its current state.
/// This class is internal to the AchievementSystem and stores runtime data.
/// It is marked [System.Serializable] to allow its state to be easily saved/loaded
/// using serialization if a more complex persistence method (e.g., JSON) were used.
/// For this example, individual properties are saved via PlayerPrefs.
/// </summary>
[System.Serializable]
public class Achievement
{
    // Unique identifier for this achievement. Used for lookup and saving.
    public string ID { get; private set; }
    // Display name of the achievement.
    public string Name { get; private set; }
    // Detailed description of how to unlock the achievement.
    public string Description { get; private set; }
    // The target value needed to unlock this achievement (e.g., 10 kills, 1 completed tutorial).
    public int TargetValue { get; private set; }
    // Current progress towards the TargetValue.
    public int CurrentProgress { get; private set; }
    // True if the achievement has been unlocked, false otherwise.
    public bool IsUnlocked { get; private set; }
    // Optional: Sprite for displaying the achievement icon in UI.
    public Sprite Icon { get; private set; }

    /// <summary>
    /// Constructor for creating an Achievement instance from a definition.
    /// Initial state is locked with 0 progress.
    /// </summary>
    public Achievement(string id, string name, string description, int targetValue, Sprite icon = null)
    {
        ID = id;
        Name = name;
        Description = description;
        TargetValue = targetValue;
        CurrentProgress = 0;
        IsUnlocked = false;
        Icon = icon;
    }

    /// <summary>
    /// Increments the current progress of the achievement.
    /// This method only updates the internal state; the AchievementSystem will handle
    /// checking for unlock conditions and notifications.
    /// </summary>
    /// <param name="amount">The amount to increment progress by (default is 1).</param>
    public void IncrementProgress(int amount = 1)
    {
        // Ensure progress doesn't exceed the target value.
        CurrentProgress = Mathf.Min(CurrentProgress + amount, TargetValue);
    }

    /// <summary>
    /// Sets the current progress of the achievement to an absolute value.
    /// This is useful for achievements like "Reach Level X" or "Collect Y Gold".
    /// </summary>
    /// <param name="value">The absolute progress value to set.</param>
    public void SetProgress(int value)
    {
        // Clamp progress between 0 and TargetValue.
        CurrentProgress = Mathf.Clamp(value, 0, TargetValue);
    }

    /// <summary>
    /// Marks the achievement as unlocked.
    /// This method should primarily be called by the AchievementSystem itself.
    /// </summary>
    public void Unlock()
    {
        if (!IsUnlocked)
        {
            IsUnlocked = true;
            CurrentProgress = TargetValue; // Ensure progress is full when unlocked
        }
    }

    /// <summary>
    /// Loads the saved state into this achievement instance.
    /// This method should primarily be called by the AchievementSystem during initialization.
    /// </summary>
    /// <param name="savedProgress">The previously saved current progress.</param>
    /// <param name="savedIsUnlocked">The previously saved unlocked status.</param>
    public void LoadState(int savedProgress, bool savedIsUnlocked)
    {
        CurrentProgress = savedProgress;
        IsUnlocked = savedIsUnlocked;
    }
}

/// <summary>
/// A serializable class used in the Unity Inspector to define the initial properties
/// of an achievement. This allows designers to easily set up achievements without
/// writing code.
/// </summary>
[System.Serializable]
public class AchievementDefinition
{
    // Unique identifier for the achievement. Must be unique across all achievements.
    public string ID;
    // The display name of the achievement.
    public string Name;
    // A detailed description of the achievement. Using TextArea for better Inspector editing.
    [TextArea(3, 5)]
    public string Description;
    // The target value for unlocking this achievement (e.g., 1 for "Complete Tutorial", 100 for "Collect 100 Coins").
    public int TargetValue;
    // Optional: An icon to visually represent the achievement in the UI.
    public Sprite Icon;
}

/// <summary>
/// The core Achievement System manager.
/// Implements the Singleton pattern to provide easy, global access to achievement functionalities.
/// It handles achievement definitions, runtime state, progress tracking, unlocking, persistence,
/// and notifying other systems about achievement unlocks.
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a static instance for global access.
    public static AchievementSystem Instance { get; private set; }

    // --- Achievement Definitions ---
    // A list of AchievementDefinition objects, populated via the Unity Inspector.
    // These define all the achievements available in the game.
    [SerializeField]
    private List<AchievementDefinition> _achievementDefinitions = new List<AchievementDefinition>();

    // --- Runtime Achievement State ---
    // A dictionary to hold the actual Achievement instances and their current runtime states.
    // Keyed by the achievement's unique ID for efficient lookup.
    private Dictionary<string, Achievement> _achievements = new Dictionary<string, Achievement>();

    // --- Event System ---
    // An event that other systems can subscribe to, to be notified when an achievement is unlocked.
    // This decouples the UI or other game systems from the achievement logic.
    public static event Action<Achievement> OnAchievementUnlocked;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton, persists the GameObject across scenes, and sets up achievements.
    /// </summary>
    private void Awake()
    {
        // Enforce singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("AchievementSystem: Multiple instances found! Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Keep this GameObject alive across scene changes so achievement progress is retained.
        DontDestroyOnLoad(gameObject);

        InitializeAchievements();
    }

    /// <summary>
    /// Called when the application is quitting.
    /// Ensures all achievement data is saved before the application closes.
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveAchievements();
    }

    // --- Core Achievement System Logic ---

    /// <summary>
    /// Initializes the achievement system. This involves creating Achievement instances
    /// from the definitions and loading any previously saved progress.
    /// </summary>
    private void InitializeAchievements()
    {
        _achievements.Clear(); // Clear any existing achievements (e.g., in editor play mode restarts).

        // Create runtime Achievement objects from their definitions.
        foreach (var def in _achievementDefinitions)
        {
            if (_achievements.ContainsKey(def.ID))
            {
                Debug.LogError($"AchievementSystem: Duplicate achievement ID found: {def.ID}. Please ensure all IDs are unique.", this);
                continue;
            }
            Achievement newAchievement = new Achievement(def.ID, def.Name, def.Description, def.TargetValue, def.Icon);
            _achievements.Add(def.ID, newAchievement);
        }

        LoadAchievements(); // Load saved states for all achievements.
        Debug.Log($"AchievementSystem Initialized. Loaded {_achievements.Count} achievements.");
    }

    /// <summary>
    /// Updates the progress of a specific achievement by a given amount.
    /// This is the primary method game logic should call to report progress.
    /// </summary>
    /// <param name="achievementID">The unique ID of the achievement to update.</param>
    /// <param name="amount">The amount to add to the current progress (default is 1).</param>
    public void UpdateAchievementProgress(string achievementID, int amount = 1)
    {
        if (!_achievements.TryGetValue(achievementID, out Achievement achievement))
        {
            Debug.LogWarning($"AchievementSystem: Attempted to update progress for unknown achievement ID: '{achievementID}'", this);
            return;
        }

        if (achievement.IsUnlocked)
        {
            // No need to update progress for an already unlocked achievement.
            // Debug.Log($"Achievement '{achievement.Name}' is already unlocked. No progress update needed.");
            return;
        }

        // Update the internal progress of the achievement object.
        achievement.IncrementProgress(amount);

        Debug.Log($"Achievement '{achievement.Name}' progress: {achievement.CurrentProgress}/{achievement.TargetValue}");
        CheckAndUnlockAchievement(achievementID); // Check if conditions are now met.
        SaveAchievements(); // Save state after every update. Consider batching saves for performance in production.
    }

    /// <summary>
    /// Sets the progress of a specific achievement to an absolute value.
    /// Useful for achievements tied to specific game state values (e.g., player level, total score).
    /// </summary>
    /// <param name="achievementID">The unique ID of the achievement to set progress for.</param>
    /// <param name="value">The absolute progress value to set.</param>
    public void SetAchievementProgress(string achievementID, int value)
    {
        if (!_achievements.TryGetValue(achievementID, out Achievement achievement))
        {
            Debug.LogWarning($"AchievementSystem: Attempted to set progress for unknown achievement ID: '{achievementID}'", this);
            return;
        }

        if (achievement.IsUnlocked)
        {
            // Debug.Log($"Achievement '{achievement.Name}' is already unlocked. No progress update needed.");
            return;
        }

        // Set the internal progress of the achievement object.
        achievement.SetProgress(value);

        Debug.Log($"Achievement '{achievement.Name}' progress set to: {achievement.CurrentProgress}/{achievement.TargetValue}");
        CheckAndUnlockAchievement(achievementID); // Check if conditions are now met.
        SaveAchievements(); // Save state after every update.
    }


    /// <summary>
    /// Internal method to check if an achievement's unlock conditions are met and, if so, unlock it.
    /// This also triggers the OnAchievementUnlocked event.
    /// </summary>
    /// <param name="achievementID">The ID of the achievement to check.</param>
    private void CheckAndUnlockAchievement(string achievementID)
    {
        if (!_achievements.TryGetValue(achievementID, out Achievement achievement))
        {
            // This should ideally not happen if called internally after a valid lookup.
            Debug.LogWarning($"AchievementSystem: Attempted to check and unlock for unknown achievement ID: '{achievementID}'", this);
            return;
        }

        // Check if the achievement is not already unlocked and if its current progress meets the target.
        if (!achievement.IsUnlocked && achievement.CurrentProgress >= achievement.TargetValue)
        {
            achievement.Unlock(); // Mark the achievement as unlocked.
            Debug.Log($"<color=green>ACHIEVEMENT UNLOCKED: {achievement.Name}!</color>");
            OnAchievementUnlocked?.Invoke(achievement); // Notify subscribers (e.g., UI)
            SaveAchievements(); // Save immediately after unlocking to prevent data loss.
        }
    }

    /// <summary>
    /// Forcefully unlocks an achievement, bypassing any progress checks.
    /// Useful for debugging, cheat codes, or special game events.
    /// </summary>
    /// <param name="achievementID">The unique ID of the achievement to unlock.</param>
    public void ForceUnlockAchievement(string achievementID)
    {
        if (!_achievements.TryGetValue(achievementID, out Achievement achievement))
        {
            Debug.LogWarning($"AchievementSystem: Attempted to force unlock unknown achievement ID: '{achievementID}'", this);
            return;
        }

        if (!achievement.IsUnlocked)
        {
            achievement.Unlock();
            Debug.Log($"<color=green>ACHIEVEMENT FORCE UNLOCKED: {achievement.Name}!</color>");
            OnAchievementUnlocked?.Invoke(achievement); // Notify subscribers
            SaveAchievements();
        }
        else
        {
            Debug.Log($"Achievement '{achievement.Name}' was already unlocked. No action taken.");
        }
    }

    // --- Public Getters for UI and other systems ---

    /// <summary>
    /// Retrieves a specific achievement by its ID.
    /// </summary>
    /// <param name="achievementID">The ID of the achievement.</param>
    /// <returns>The Achievement object if found, otherwise null.</returns>
    public Achievement GetAchievement(string achievementID)
    {
        _achievements.TryGetValue(achievementID, out Achievement achievement);
        return achievement;
    }

    /// <summary>
    /// Retrieves a list of all achievements defined in the system.
    /// </summary>
    public List<Achievement> GetAllAchievements()
    {
        return _achievements.Values.ToList();
    }

    /// <summary>
    /// Retrieves a list of all achievements that have been unlocked.
    /// </summary>
    public List<Achievement> GetUnlockedAchievements()
    {
        return _achievements.Values.Where(a => a.IsUnlocked).ToList();
    }

    /// <summary>
    /// Retrieves a list of all achievements that are currently locked.
    /// </summary>
    public List<Achievement> GetLockedAchievements()
    {
        return _achievements.Values.Where(a => !a.IsUnlocked).ToList();
    }

    // --- Persistence (using Unity's PlayerPrefs for simplicity) ---
    // PlayerPrefs is simple but not suitable for large amounts of data or secure storage.
    // For production, consider JSON, XML, or binary serialization to a file, or a dedicated save system.
    private const string ACHIEVEMENT_SAVE_PREFIX = "Achievement_"; // Prefix for PlayerPrefs keys

    /// <summary>
    /// Saves the current state (progress and unlocked status) of all achievements to PlayerPrefs.
    /// </summary>
    private void SaveAchievements()
    {
        foreach (var achievement in _achievements.Values)
        {
            // Store progress as an int
            PlayerPrefs.SetInt($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Progress", achievement.CurrentProgress);
            // Store unlocked status as an int (1 for true, 0 for false)
            PlayerPrefs.SetInt($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Unlocked", achievement.IsUnlocked ? 1 : 0);
        }
        PlayerPrefs.Save(); // Ensures all PlayerPrefs changes are written to disk.
        // Debug.Log("Achievements saved."); // Uncomment for verbose saving logs.
    }

    /// <summary>
    /// Loads the saved state of all achievements from PlayerPrefs.
    /// </summary>
    private void LoadAchievements()
    {
        foreach (var achievement in _achievements.Values)
        {
            // Retrieve saved progress, defaulting to 0 if not found.
            int savedProgress = PlayerPrefs.GetInt($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Progress", 0);
            // Retrieve saved unlocked status, defaulting to 0 (false) if not found.
            bool savedUnlocked = PlayerPrefs.GetInt($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Unlocked", 0) == 1;

            achievement.LoadState(savedProgress, savedUnlocked);

            // Edge case: If an achievement was marked as unlocked but its progress wasn't at target (e.g., from a bug or old save),
            // ensure its progress is correctly set to TargetValue.
            if (savedUnlocked && achievement.CurrentProgress < achievement.TargetValue)
            {
                achievement.SetProgress(achievement.TargetValue);
            }
        }
        // Debug.Log("Achievements loaded."); // Uncomment for verbose loading logs.
    }

    // --- Debug/Reset Functionality ---

    /// <summary>
    /// Resets all achievements to their initial, locked state with zero progress.
    /// Also clears their entries from PlayerPrefs.
    /// Useful for testing or implementing a "New Game" option.
    /// </summary>
    public void ResetAllAchievements()
    {
        foreach (var achievement in _achievements.Values)
        {
            achievement.LoadState(0, false); // Reset internal state
            // Delete corresponding PlayerPrefs entries
            PlayerPrefs.DeleteKey($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Progress");
            PlayerPrefs.DeleteKey($"{ACHIEVEMENT_SAVE_PREFIX}{achievement.ID}_Unlocked");
        }
        PlayerPrefs.Save(); // Ensure deletions are written to disk.

        // Optionally, invoke the event with a null achievement or a dedicated reset event
        // to signal UI to refresh its display of achievements.
        OnAchievementUnlocked?.Invoke(null); 
        Debug.Log("<color=red>All achievements have been reset!</color>");
    }
}


/*
/// <summary>
/// --- EXAMPLE USAGE ---
///
/// This section demonstrates how to set up and interact with the AchievementSystem
/// within your Unity project.
/// </summary>
*/

/*
/// <summary>
/// 1. Setting Up Achievements in the Unity Editor:
///
/// a. Create an Empty GameObject in your first scene (e.g., "AchievementManager").
/// b. Attach the `AchievementSystem.cs` script to this GameObject.
/// c. In the Inspector, you'll see a field called "Achievement Definitions".
/// d. Expand this list and click the '+' button to add new achievements.
/// e. For each achievement, fill in:
///    - ID: A unique string identifier (e.g., "FIRST_KILL", "COMPLETED_TUTORIAL", "COLLECT_100_COINS").
///    - Name: The display name (e.g., "First Blood", "Master Apprentice", "Coin Hoarder").
///    - Description: What the player needs to do (e.g., "Defeat your first enemy.", "Complete the game's tutorial.", "Collect 100 gold coins.").
///    - Target Value: The number for progress-based achievements (e.g., 1 for "First Blood", 100 for "Coin Hoarder").
///    - Icon: (Optional) Assign a Sprite for the achievement's icon.
///
/// Example Definitions:
/// - ID: FIRST_KILL, Name: First Blood, Description: Defeat your first enemy., Target Value: 1
/// - ID: TEN_KILLS, Name: Slayer, Description: Defeat 10 enemies., Target Value: 10
/// - ID: COMPLETE_TUTORIAL, Name: Apprentice, Description: Complete the game tutorial., Target Value: 1
/// - ID: COLLECT_50_COINS, Name: Coin Collector, Description: Collect 50 coins., Target Value: 50
/// - ID: REACH_LEVEL_5, Name: Seasoned Adventurer, Description: Reach player level 5., Target Value: 5
/// </summary>
*/

/*
/// <summary>
/// 2. Integrating with Game Logic:
///
/// Your game's systems will call `AchievementSystem.Instance.UpdateAchievementProgress()`
/// or `AchievementSystem.Instance.SetAchievementProgress()` whenever a relevant event occurs.
///
/// Example: Player Kills an Enemy
/// </summary>
public class Enemy : MonoBehaviour
{
    public void Die()
    {
        Debug.Log("Enemy died!");
        // Notify the AchievementSystem about the kill.
        // It will automatically check if "FIRST_KILL" or "TEN_KILLS" etc., should be unlocked.
        AchievementSystem.Instance.UpdateAchievementProgress("FIRST_KILL");
        AchievementSystem.Instance.UpdateAchievementProgress("TEN_KILLS");
        AchievementSystem.Instance.UpdateAchievementProgress("KILL_100_ENEMIES"); // Assuming you have such an achievement
        
        // Destroy(gameObject); // Or disable, pool, etc.
    }
}

/// Example: Player Collects Coins
public class Coin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player collected a coin!");
            // Notify the AchievementSystem that a coin was collected.
            AchievementSystem.Instance.UpdateAchievementProgress("COLLECT_50_COINS");
            
            // Destroy(gameObject);
        }
    }
}

/// Example: Player Completes Tutorial
public class TutorialManager : MonoBehaviour
{
    public void CompleteTutorial()
    {
        Debug.Log("Tutorial completed!");
        // Unlock the tutorial achievement.
        AchievementSystem.Instance.UpdateAchievementProgress("COMPLETE_TUTORIAL"); // TargetValue is 1, so this will unlock it.
    }
}

/// Example: Player Reaches a Certain Level (using SetAchievementProgress)
public class PlayerStats : MonoBehaviour
{
    public int currentLevel = 0;

    public void GainLevel()
    {
        currentLevel++;
        Debug.Log($"Player reached level {currentLevel}!");
        // Set the achievement progress to the current level.
        // The AchievementSystem will check if "REACH_LEVEL_5" (or other level-based achievements)
        // should be unlocked.
        AchievementSystem.Instance.SetAchievementProgress("REACH_LEVEL_5", currentLevel);
        AchievementSystem.Instance.SetAchievementProgress("REACH_LEVEL_10", currentLevel);
    }
}
*/

/*
/// <summary>
/// 3. Displaying Achievements in UI:
///
/// Create a simple UI element (e.g., a notification popup, an achievement list screen)
/// and subscribe to the `OnAchievementUnlocked` event to react to unlocks.
/// </summary>
public class AchievementUI : MonoBehaviour
{
    public GameObject achievementUnlockedPopupPrefab; // Assign a UI popup prefab in Inspector
    public Transform popupParent; // Parent transform for the popup (e.g., Canvas)

    private void OnEnable()
    {
        // Subscribe to the achievement unlocked event.
        AchievementSystem.OnAchievementUnlocked += OnAchievementUnlockedHandler;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks.
        AchievementSystem.OnAchievementUnlocked -= OnAchievementUnlockedHandler;
    }

    /// <summary>
    /// Event handler for when an achievement is unlocked.
    /// </summary>
    /// <param name="achievement">The achievement that was unlocked. Null if reset was called.</param>
    private void OnAchievementUnlockedHandler(Achievement achievement)
    {
        if (achievement == null)
        {
            Debug.Log("Achievement System Reset - UI should refresh achievement list.");
            // Refresh your entire achievement list UI here if needed, as all might have changed.
            RefreshAchievementListUI(); 
            return;
        }

        Debug.Log($"UI: Received event for Unlocked Achievement: {achievement.Name}");

        // Example: Show a temporary popup notification.
        if (achievementUnlockedPopupPrefab != null && popupParent != null)
        {
            GameObject popupGO = Instantiate(achievementUnlockedPopupPrefab, popupParent);
            // Assuming your popup prefab has a script like 'AchievementPopupDisplay'
            // that can take an Achievement object and display its name, description, icon.
            AchievementPopupDisplay display = popupGO.GetComponent<AchievementPopupDisplay>();
            if (display != null)
            {
                display.Setup(achievement);
            }
            else
            {
                Debug.LogWarning("AchievementSystem: Popup prefab missing 'AchievementPopupDisplay' script.");
            }
            // Optionally, destroy popup after a delay, or manage it with an animation.
            Destroy(popupGO, 5f); 
        }

        // Also, refresh your main achievement list UI if it's open.
        RefreshAchievementListUI();
    }

    /// <summary>
    /// Example method to refresh an achievement list UI.
    /// In a real project, this would populate scroll views, etc.
    /// </summary>
    public void RefreshAchievementListUI()
    {
        if (AchievementSystem.Instance == null) return;

        Debug.Log("UI: Refreshing achievement list display...");
        List<Achievement> allAchievements = AchievementSystem.Instance.GetAllAchievements();
        foreach (var achievement in allAchievements)
        {
            Debug.Log($"  - {achievement.Name} ({achievement.CurrentProgress}/{achievement.TargetValue}) - {(achievement.IsUnlocked ? "Unlocked" : "Locked")}");
            // Your UI logic to update entries for each achievement would go here.
            // E.g., find a UI element for this achievement and update its text/progress bar/icon.
        }
    }

    /// <summary>
    /// Example button callback for testing the reset functionality.
    /// </summary>
    public void OnResetAchievementsButtonClicked()
    {
        AchievementSystem.Instance.ResetAllAchievements();
    }
}

// Example script for the achievement popup display (would be on your prefab)
public class AchievementPopupDisplay : MonoBehaviour
{
    public TMPro.TextMeshProUGUI titleText; // Requires TextMeshPro, import from Window -> TextMeshPro -> Import TMP Essential Resources
    public TMPro.TextMeshProUGUI descriptionText;
    public UnityEngine.UI.Image iconImage;

    public void Setup(Achievement achievement)
    {
        if (titleText != null) titleText.text = achievement.Name;
        if (descriptionText != null) descriptionText.text = achievement.Description;
        if (iconImage != null && achievement.Icon != null) iconImage.sprite = achievement.Icon;
        else if (iconImage != null) iconImage.gameObject.SetActive(false); // Hide if no icon
    }
}
*/
```