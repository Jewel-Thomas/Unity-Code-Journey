// Unity Design Pattern Example: ExperienceSystem
// This script demonstrates the ExperienceSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of an "Experience System" design pattern. It manages a player's experience points (XP), handles leveling up based on a configurable XP curve, and uses events to notify other game systems (like UI, ability managers, or stat systems) about changes.

This pattern promotes **decoupling**, allowing different parts of your game to react to XP and level changes without needing direct references to each other.

---

### How to Use This Script:

1.  **Create a New C# Script:** In your Unity project, create a new C# script named `ExperienceSystem.cs`.
2.  **Copy and Paste:** Replace the default content of the new script with the code provided below.
3.  **Attach to a GameObject:**
    *   Create an empty GameObject in your scene (e.g., name it `PlayerManager` or `GameManager`).
    *   Attach the `ExperienceSystem.cs` script to this GameObject.
4.  **Configure in Inspector:**
    *   Select the GameObject with the `ExperienceSystem` script.
    *   In the Inspector, you can adjust:
        *   **Base XP Requirement:** The XP needed for Level 1 to Level 2.
        *   **XP Per Level Increase:** How much more XP is needed for each subsequent level.
        *   **Max Level:** Set to 0 or less for no level cap.
5.  **Implement Example Usage:**
    *   Refer to the example scripts at the bottom (commented out in the main file) to see how to create a UI Manager, Enemy script, Ability Manager, or Debugger that interacts with the `ExperienceSystem`.
    *   Create separate C# scripts for these examples (e.g., `UIManager.cs`, `Enemy.cs`, `AbilityManager.cs`, `ExperienceDebugger.cs`) and attach them to appropriate GameObjects in your scene (e.g., `UIManager` to a Canvas, `Enemy` to enemy prefabs, `AbilityManager` to your player).
    *   Uncomment the example code in those *new* scripts.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // Not strictly needed for the core system, but good practice for general Unity scripts.

/// <summary>
/// Represents a comprehensive Experience System for a player or entity in a Unity game.
/// This system manages current experience points (XP), calculates required XP for level-ups,
/// handles the leveling process, and provides events for other systems to react to changes.
/// </summary>
[DisallowMultipleComponent] // Ensures only one ExperienceSystem can be on a GameObject.
public class ExperienceSystem : MonoBehaviour
{
    // --- Singleton Pattern (Optional but common for player-centric systems) ---
    // Provides easy access to the ExperienceSystem from anywhere in the game.
    // Be mindful of its potential drawbacks in larger projects (e.g., testing, dependency management).
    public static ExperienceSystem Instance { get; private set; }

    [Header("Current Stats")]
    [SerializeField]
    [Tooltip("The current experience points accumulated within the current level.")]
    private int _currentXP = 0;
    /// <summary>
    /// Gets the current experience points.
    /// </summary>
    public int CurrentXP => _currentXP;

    [SerializeField]
    [Tooltip("The current level.")]
    private int _currentLevel = 1; // Start at level 1
    /// <summary>
    /// Gets the current level.
    /// </summary>
    public int CurrentLevel => _currentLevel;

    [Header("Experience Curve Configuration")]
    [SerializeField]
    [Tooltip("The base experience points required to level up from Level 1 to Level 2.")]
    private int _baseXPRequirement = 100;

    [SerializeField]
    [Tooltip("The amount of additional XP required for each subsequent level beyond the base requirement. " +
             "Example: L1->L2 needs BaseXP. L2->L3 needs BaseXP + (1 * XPIncrease). L3->L4 needs BaseXP + (2 * XPIncrease).")]
    private int _xpPerLevelIncrease = 50;

    [SerializeField]
    [Tooltip("The maximum level this system can reach. Set to 0 or less for no cap.")]
    private int _maxLevel = 50;
    /// <summary>
    /// Gets the maximum level configured for this experience system.
    /// </summary>
    public int MaxLevel => _maxLevel;


    // --- Calculated Properties ---

    /// <summary>
    /// Gets the total experience points required to reach the next level from the current level.
    /// Returns 0 if already at maximum level.
    /// </summary>
    public int XPToNextLevel
    {
        get
        {
            // If already at max level and maxLevel is set, no more XP is needed.
            if (_maxLevel > 0 && _currentLevel >= _maxLevel)
            {
                return 0;
            }
            // Formula: Base XP + (Current Level * XP increase per level)
            // Example for L1->L2 (_currentLevel = 1): _baseXPRequirement + (1 * _xpPerLevelIncrease)
            // Example for L2->L3 (_currentLevel = 2): _baseXPRequirement + (2 * _xpPerLevelIncrease)
            // This makes the XP requirement linearly scale with the current level.
            return _baseXPRequirement + (_currentLevel * _xpPerLevelIncrease);
        }
    }

    /// <summary>
    /// Gets the progress towards the next level as a percentage (0.0 to 1.0).
    /// Returns 1.0f if at max level or if XPToNextLevel is 0.
    /// </summary>
    public float ProgressToNextLevel
    {
        get
        {
            if (XPToNextLevel <= 0) // Avoid division by zero, especially at max level.
            {
                return 1.0f;
            }
            return (float)_currentXP / XPToNextLevel;
        }
    }

    // --- Events ---
    // These events allow other systems (like UI, abilities, or stat managers) to react
    // to changes in the experience system without direct coupling, adhering to the
    // "Experience System" design pattern's core principle of decoupling.

    /// <summary>
    /// Event fired whenever current XP changes (e.g., XP gained, level-up occurred, reset).
    /// Parameters: (currentXP, xpToNextLevel, progressToNextLevel)
    /// Useful for updating UI experience bars.
    /// </summary>
    public event Action<int, int, float> OnXPChanged;

    /// <summary>
    /// Event fired when the level increases.
    /// Parameters: (newLevel)
    /// Useful for unlocking abilities, showing level-up effects, or updating character stats.
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// Event fired when the maximum level is reached.
    /// Parameters: (maxLevel)
    /// </summary>
    public event Action<int> OnMaxLevelReached;

    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ExperienceSystem instances found! Destroying duplicate.", this);
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make this object persist across scene loads if it's for the player.
            // DontDestroyOnLoad(this.gameObject);
        }
    }

    // --- Public API for Interaction ---

    /// <summary>
    /// Adds experience points to the system. Handles level-ups if enough XP is accumulated.
    /// This is the primary method for granting XP to the player/entity.
    /// </summary>
    /// <param name="amount">The amount of XP to add. Must be non-negative.</param>
    public void AddXP(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to add negative XP. Use a separate RemoveXP method if needed.", this);
            return;
        }

        // If at max level (and maxLevel is set), no more XP can be added to progress levels.
        if (_maxLevel > 0 && _currentLevel >= _maxLevel)
        {
            Debug.Log($"Player is already at max level ({_maxLevel}). Cannot gain more XP to level up.", this);
            // Still trigger XP changed, as we might want to show a "maxed out" bar,
            // or perhaps for a system that counts "overflow" XP post-max level.
            OnXPChanged?.Invoke(_currentXP, 0, 1.0f); // XPToNextLevel is 0, progress is 100%
            return;
        }

        _currentXP += amount;
        Debug.Log($"Gained {amount} XP. Current XP: {_currentXP} (before level check)", this);

        // Loop to handle multiple level-ups from a large XP gain (e.g., completing a major quest).
        // The loop continues as long as there's a next level to reach and enough XP.
        while (_maxLevel == 0 || _currentLevel < _maxLevel)
        {
            int requiredXPForCurrentLevelUp = XPToNextLevel; // Get XP needed for current level to advance.

            if (_currentXP >= requiredXPForCurrentLevelUp && requiredXPForCurrentLevelUp > 0)
            {
                _currentXP -= requiredXPForCurrentLevelUp; // Subtract XP required for the current level.
                _currentLevel++;                             // Increment level.

                Debug.Log($"Leveled up to Level {_currentLevel}! Remaining XP: {_currentXP}. Next level needs: {XPToNextLevel}", this);
                OnLevelUp?.Invoke(_currentLevel); // Notify subscribers of the level-up.

                // If max level reached after this level up, stop further processing.
                if (_maxLevel > 0 && _currentLevel >= _maxLevel)
                {
                    _currentXP = 0; // Typically, XP resets to 0 or is capped when max level is reached.
                    OnMaxLevelReached?.Invoke(_maxLevel);
                    Debug.Log($"Reached max level {_maxLevel}.", this);
                    break; // Exit loop, no further levels possible.
                }
            }
            else
            {
                // Not enough XP for the current level-up, so stop looping.
                break;
            }
        }

        // Always invoke OnXPChanged to update UI elements, even if no level-up occurred.
        // XPToNextLevel might be 0 if max level was reached in the loop.
        OnXPChanged?.Invoke(_currentXP, XPToNextLevel, ProgressToNextLevel);
    }

    /// <summary>
    /// Resets the experience system to its initial state (Level 1, 0 XP).
    /// Useful for starting a new game or for debugging purposes.
    /// </summary>
    public void ResetExperience()
    {
        _currentXP = 0;
        _currentLevel = 1;
        Debug.Log("Experience system reset to Level 1, 0 XP.", this);

        // Notify subscribers of the reset.
        OnLevelUp?.Invoke(_currentLevel); // Often, a reset counts as a level change.
        OnXPChanged?.Invoke(_currentXP, XPToNextLevel, ProgressToNextLevel);
    }

    /// <summary>
    /// Sets the experience system to a specific level and XP.
    /// Useful for loading game data, character creation, or advanced debugging.
    /// </summary>
    /// <param name="level">The target level to set. Will be capped by MaxLevel.</param>
    /// <param name="xp">The current XP within that level. Will be capped by XPToNextLevel for the given level.</param>
    public void SetExperience(int level, int xp)
    {
        if (level < 1) level = 1; // Minimum level is 1.
        
        _currentLevel = level;
        _currentXP = xp;

        // If setting to or beyond max level, cap it.
        if (_maxLevel > 0 && _currentLevel >= _maxLevel)
        {
            _currentLevel = _maxLevel;
            _currentXP = 0; // At max level, current XP is typically reset or considered irrelevant for level progression.
            Debug.Log($"Experience set to MAX Level {_currentLevel}.", this);
            OnMaxLevelReached?.Invoke(_maxLevel);
        }
        else
        {
            // Ensure XP doesn't exceed what's needed for the current level to the next.
            if (_currentXP >= XPToNextLevel)
            {
                // If the provided XP is too high for the given level, cap it just below the next level.
                // This assumes the intention is to set the 'current' state, not to trigger a level up.
                _currentXP = XPToNextLevel - 1; 
            }
            if (_currentXP < 0) _currentXP = 0; // Ensure XP is not negative.
            Debug.Log($"Experience set to Level {CurrentLevel}, XP {CurrentXP}/{XPToNextLevel}", this);
        }
        
        // Notify subscribers of the change.
        OnLevelUp?.Invoke(CurrentLevel); 
        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel, ProgressToNextLevel); 
    }

    // --- Persistence (Example - not fully implemented for brevity) ---
    // In a real game, you would typically save and load _currentXP and _currentLevel
    // using methods like PlayerPrefs, JSON serialization, or a dedicated save system.

    // public void SaveExperience()
    // {
    //     PlayerPrefs.SetInt("PlayerCurrentXP", _currentXP);
    //     PlayerPrefs.SetInt("PlayerCurrentLevel", _currentLevel);
    //     PlayerPrefs.Save(); // Ensures data is written to disk.
    //     Debug.Log("Experience saved!");
    // }

    // public void LoadExperience()
    // {
    //     _currentXP = PlayerPrefs.GetInt("PlayerCurrentXP", 0); // Default to 0 XP if not found.
    //     _currentLevel = PlayerPrefs.GetInt("PlayerCurrentLevel", 1); // Default to Level 1 if not found.
        
    //     // After loading, ensure all derived values and UI are updated.
    //     OnLevelUp?.Invoke(_currentLevel); 
    //     OnXPChanged?.Invoke(_currentXP, XPToNextLevel, ProgressToNextLevel);
    //     Debug.Log($"Experience loaded: Level {_currentLevel}, XP {_currentXP}");
    // }
}


/*
/// ====================================================================================
/// EXAMPLE USAGE: How other scripts would interact with the ExperienceSystem
///
/// To use these examples:
/// 1. Create a new C# script for each example (e.g., UIManager.cs, Enemy.cs).
/// 2. Copy the content of each example into its respective new script.
/// 3. Attach the scripts to appropriate GameObjects in your scene.
/// 4. Ensure you have the necessary UI elements (Text, Slider) for the UIManager example.
/// ====================================================================================
*/

// --- EXAMPLE 1: UI Manager for an Experience Bar ---
// Attach this script to a GameObject that manages your UI (e.g., Canvas, UIManager).
// Requires Unity UI components (Text, Slider) to be set up in your scene and referenced.

// using UnityEngine;
// using UnityEngine.UI; // Required for UI elements like Text and Slider.

// public class UIManager : MonoBehaviour
// {
//     [SerializeField] private Text levelText; // Assign a UI Text element in the Inspector.
//     [SerializeField] private Text xpText;    // Assign a UI Text element in the Inspector.
//     [SerializeField] private Slider xpSlider; // Assign a UI Slider element in the Inspector.

//     private void Start()
//     {
//         // Check if the ExperienceSystem instance exists in the scene.
//         if (ExperienceSystem.Instance == null)
//         {
//             Debug.LogError("UIManager: ExperienceSystem not found! Make sure it's present in the scene.", this);
//             return;
//         }

//         // Subscribe to events from the ExperienceSystem to receive updates.
//         ExperienceSystem.Instance.OnXPChanged += UpdateExperienceUI;
//         ExperienceSystem.Instance.OnLevelUp += UpdateLevelUI;
//         ExperienceSystem.Instance.OnMaxLevelReached += ShowMaxLevelMessage;

//         // Immediately update UI with current stats when starting, in case of scene load or initialization.
//         UpdateLevelUI(ExperienceSystem.Instance.CurrentLevel);
//         UpdateExperienceUI(ExperienceSystem.Instance.CurrentXP, ExperienceSystem.Instance.XPToNextLevel, ExperienceSystem.Instance.ProgressToNextLevel);
//     }

//     private void OnDestroy()
//     {
//         // Unsubscribe from events to prevent memory leaks, especially important if this object
//         // might be destroyed while the ExperienceSystem persists (e.g., DontDestroyOnLoad).
//         if (ExperienceSystem.Instance != null)
//         {
//             ExperienceSystem.Instance.OnXPChanged -= UpdateExperienceUI;
//             ExperienceSystem.Instance.OnLevelUp -= UpdateLevelUI;
//             ExperienceSystem.Instance.OnMaxLevelReached -= ShowMaxLevelMessage;
//         }
//     }

//     /// <summary>
//     /// Updates the experience bar and XP text in the UI. Called when OnXPChanged event fires.
//     /// </summary>
//     private void UpdateExperienceUI(int currentXP, int xpToNextLevel, float progress)
//     {
//         if (xpText != null)
//         {
//             xpText.text = $"XP: {currentXP} / {xpToNextLevel}";
//         }
//         if (xpSlider != null)
//         {
//             // Set slider's max value to the XP needed for the next level.
//             // Ensure it's at least 1 to prevent issues if xpToNextLevel is 0 (at max level).
//             xpSlider.maxValue = xpToNextLevel > 0 ? xpToNextLevel : 1; 
//             xpSlider.value = currentXP;
//             
//             // Alternative: use progress (0.0 to 1.0) directly if slider max is always 1.
//             // xpSlider.value = progress;
//             // xpSlider.maxValue = 1;
//         }

//         // Special display for max level.
//         if (xpToNextLevel == 0 && ExperienceSystem.Instance.CurrentLevel == ExperienceSystem.Instance.MaxLevel && ExperienceSystem.Instance.MaxLevel > 0)
//         {
//              if (xpText != null) xpText.text = "MAX XP";
//              if (xpSlider != null) xpSlider.value = xpSlider.maxValue; // Ensure slider is full.
//         }
//     }

//     /// <summary>
//     /// Updates the level text in the UI. Called when OnLevelUp event fires.
//     /// </summary>
//     private void UpdateLevelUI(int newLevel)
//     {
//         if (levelText != null)
//         {
//             levelText.text = $"Level: {newLevel}";
//         }
//         Debug.Log($"UI Manager: Player reached Level {newLevel}!");
//         // You could also trigger a level-up animation, sound effect, or special UI pop-up here.
//     }

//     /// <summary>
//     /// Displays a message when the maximum level is reached. Called when OnMaxLevelReached event fires.
//     /// </summary>
//     private void ShowMaxLevelMessage(int maxLevel)
//     {
//         Debug.Log($"UI Manager: Congratulations! You've reached the maximum level: {maxLevel}!");
//         // Display a special "Max Level" graphic or message on the UI.
//     }
// }


// --- EXAMPLE 2: Enemy script that grants XP on defeat ---
// Attach this script to an Enemy GameObject. When the enemy is "defeated", it grants XP.

// using UnityEngine;

// public class Enemy : MonoBehaviour
// {
//     [SerializeField] private int xpOnDefeat = 25; // XP this enemy grants when defeated.

//     /// <summary>
//     /// Simulates the enemy being defeated and grants XP to the player.
//     /// In a real game, this would be called by a combat system or player interaction.
//     /// </summary>
//     public void DefeatEnemy()
//     {
//         Debug.Log($"{gameObject.name} was defeated, granting {xpOnDefeat} XP.");
        
//         // Grant XP via the ExperienceSystem. This is the primary way to interact with the system.
//         if (ExperienceSystem.Instance != null)
//         {
//             ExperienceSystem.Instance.AddXP(xpOnDefeat);
//         }
//         else
//         {
//             Debug.LogError("Enemy: ExperienceSystem not found! Cannot grant XP.", this);
//         }

//         // Destroy the enemy GameObject or disable it after defeat.
//         Destroy(gameObject);
//     }

//     private void Start()
//     {
//         // For demonstration, an enemy might defeat itself after a short delay.
//         // In a real game, this would typically be triggered by player actions.
//         // Invoke("DefeatEnemy", 3f); // Example: Enemy self-destructs after 3 seconds.
//     }
// }

// --- EXAMPLE 3: Ability Manager (unlocking abilities based on level) ---
// Attach this to your player character or an AbilityManager GameObject.

// using UnityEngine;
// using System.Collections.Generic; // Required for List.

// public class AbilityManager : MonoBehaviour
// {
//     // A structure to define an ability and its unlock requirements.
//     [System.Serializable] // Makes struct visible and editable in the Inspector.
//     public struct AbilityUnlockData
//     {
//         public string abilityName;
//         public int requiredLevel;
//         [HideInInspector] public bool isUnlocked; // Internal state, not directly set in Inspector.
//     }

//     [SerializeField] private List<AbilityUnlockData> abilities = new List<AbilityUnlockData>();

//     private void Start()
//     {
//         if (ExperienceSystem.Instance == null)
//         {
//             Debug.LogError("AbilityManager: ExperienceSystem not found! Make sure it's present in the scene.", this);
//             return;
//         }

//         // Subscribe to the OnLevelUp event to check for new abilities whenever the player levels up.
//         ExperienceSystem.Instance.OnLevelUp += CheckForNewAbilities;

//         // Important for game loading: Initialize abilities based on the player's current level
//         // in case abilities were already unlocked in a previous session.
//         CheckForNewAbilities(ExperienceSystem.Instance.CurrentLevel); 
//     }

//     private void OnDestroy()
//     {
//         // Unsubscribe to prevent memory leaks.
//         if (ExperienceSystem.Instance != null)
//         {
//             ExperienceSystem.Instance.OnLevelUp -= CheckForNewAbilities;
//         }
//     }

//     /// <summary>
//     /// Checks if any new abilities can be unlocked based on the current level.
//     /// Called when the player levels up or when the game starts/loads.
//     /// </summary>
//     /// <param name="currentLevel">The player's current level.</param>
//     private void CheckForNewAbilities(int currentLevel)
//     {
//         // Iterate through abilities to see if their unlock conditions are met.
//         for (int i = 0; i < abilities.Count; i++)
//         {
//             // Create a temporary copy to modify the struct within the loop.
//             AbilityUnlockData ability = abilities[i]; 
            
//             if (!ability.isUnlocked && currentLevel >= ability.requiredLevel)
//             {
//                 ability.isUnlocked = true; // Mark as unlocked.
//                 UnlockAbility(ability.abilityName); // Trigger the actual unlock logic.
//                 abilities[i] = ability; // Assign the modified struct back to the list.
//             }
//         }
//     }

//     /// <summary>
//     /// Placeholder for actual ability unlocking logic.
//     /// </summary>
//     /// <param name="name">The name of the ability to unlock.</param>
//     private void UnlockAbility(string name)
//     {
//         Debug.Log($"AbilityManager: Unlocked new ability: {name}!");
//         // Add actual ability logic here:
//         // - Enable a component on the player.
//         // - Add the ability to a list of active skills.
//         // - Show a notification to the player.
//         // - Instantiate a new ability GameObject.
//     }
// }

// --- EXAMPLE 4: Debug/Test Script ---
// Attach this to any GameObject to easily test the ExperienceSystem with key presses.

// using UnityEngine;

// public class ExperienceDebugger : MonoBehaviour
// {
//     [SerializeField] private int xpToAdd = 10; // Amount of XP to add per key press.
//     [SerializeField] private int levelToSet = 5; // Level to set for testing.
//     [SerializeField] private int xpWithinLevelToSet = 0; // XP within that level.

//     void Update()
//     {
//         if (ExperienceSystem.Instance == null)
//         {
//             Debug.LogError("ExperienceDebugger: ExperienceSystem not found!");
//             return;
//         }

//         // Press Space to add XP.
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             ExperienceSystem.Instance.AddXP(xpToAdd);
//         }

//         // Press R to reset experience.
//         if (Input.GetKeyDown(KeyCode.R))
//         {
//             ExperienceSystem.Instance.ResetExperience();
//         }

//         // Press S to set a specific level and XP.
//         if (Input.GetKeyDown(KeyCode.S))
//         {
//             ExperienceSystem.Instance.SetExperience(levelToSet, xpWithinLevelToSet);
//         }
//     }
// }
```