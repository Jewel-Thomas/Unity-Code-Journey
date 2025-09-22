// Unity Design Pattern Example: PlayerProgressionSystem
// This script demonstrates the PlayerProgressionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Player Progression System design pattern in games focuses on managing a player's journey through the game, typically involving experience points (XP), levels, stat improvements, skill unlocks, and rewards. While not a classical GoF design pattern, it describes a common architectural approach to building such systems in a robust, extensible, and data-driven manner.

This example will demonstrate a practical implementation using Unity's ScriptableObjects for data definition and a central MonoBehaviour for managing the player's progression state.

---

### Key Components of this Player Progression System:

1.  **`StatDataSO` (ScriptableObject):** Defines a fundamental player statistic (e.g., Health, Attack).
2.  **`SkillDataSO` (ScriptableObject):** Defines a specific skill or ability the player can unlock and potentially upgrade.
3.  **`LevelDataSO` (ScriptableObject):** Defines the requirements and rewards for reaching a specific player level.
    *   Includes `StatBonus` struct for applying stat modifications.
4.  **`ProgressionConfigSO` (ScriptableObject):** The main configuration asset that holds an ordered list of all `LevelDataSO` assets.
5.  **`PlayerProgressionSystem` (MonoBehaviour, Singleton):** The core manager.
    *   Tracks current XP, level, skill points, and player stats.
    *   Provides methods to gain XP, unlock/upgrade skills.
    *   Notifies other systems (UI, game logic) about progression changes using C# Events.
6.  **`ProgressionUIExample` (MonoBehaviour):** A simple UI script demonstrating how to subscribe to events and display progression data.
7.  **`ProgressionTriggerExample` (MonoBehaviour):** A script that simulates actions causing XP gain or skill unlocks.

---

### Setup in Unity:

1.  Create a new Unity project or open an existing one.
2.  Create folders: `Scripts`, `ScriptableObjects`, `UI`.
3.  Place the C# scripts into their respective folders (or just `Scripts`).
4.  **Create ScriptableObjects:**
    *   Right-click in your Project window -> `Create` -> `Progression` -> `Stat Data` (e.g., "Health", "Attack", "Defense").
    *   Right-click -> `Create` -> `Progression` -> `Skill Data` (e.g., "Fireball", "Heal", "Dash").
    *   Right-click -> `Create` -> `Progression` -> `Level Data` (e.g., "Level 1", "Level 2", "Level 3").
        *   Fill in the `Required XP`, `Skill Points Awarded`, `Stat Bonuses` (drag your `StatDataSO` assets here), and `Unlocked Skills` (drag your `SkillDataSO` assets here).
    *   Right-click -> `Create` -> `Progression` -> `Progression Config` (e.g., "DefaultProgression").
        *   Drag your `LevelDataSO` assets into the `Level Definitions` list **in ascending order** (Level 1, Level 2, Level 3...).
        *   Drag your `StatDataSO` assets into `Initial Stats` and set their starting values.
5.  **Create an Empty GameObject** in your scene (e.g., "ProgressionManager").
6.  Attach the `PlayerProgressionSystem.cs` script to it.
7.  Drag your "DefaultProgression" `ProgressionConfigSO` into the `Progression Config` field of the `PlayerProgressionSystem` component.
8.  **Create a simple UI Canvas:**
    *   Add a Canvas (UI -> Canvas).
    *   Add Text elements for Level, XP, Skill Points, and individual Stats.
    *   Add Buttons for "Gain 50 XP" and "Unlock Fireball".
    *   Attach the `ProgressionUIExample.cs` script to a suitable GameObject within the Canvas or the Canvas itself. Drag the Text and Button references to the script's fields.
9.  **Create another Empty GameObject** (e.g., "GameLogic") and attach `ProgressionTriggerExample.cs` to it. Drag the relevant `SkillDataSO` to its field if you want to test unlocking a specific skill.

---

### `StatDataSO.cs`

```csharp
using UnityEngine;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// ScriptableObject defining a core player statistic (e.g., Health, Attack, Defense).
    /// This allows us to refer to stats by a common object rather than just strings,
    /// enabling easier referencing and preventing typos.
    /// </summary>
    [CreateAssetMenu(fileName = "StatData_", menuName = "Progression/Stat Data", order = 1)]
    public class StatDataSO : ScriptableObject
    {
        [Tooltip("A unique identifier for this stat.")]
        public string statID;

        [Tooltip("The display name for this stat.")]
        public string statName;

        // You could add an icon here, or a default value if not set in ProgressionConfigSO
        // public Sprite icon;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(statID))
            {
                statID = name; // Auto-populate ID if not set, useful for initial setup
            }
        }
    }
}
```

### `SkillDataSO.cs`

```csharp
using UnityEngine;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// ScriptableObject defining a specific skill or ability the player can learn.
    /// This could be extended to include skill levels, effects, prerequisites, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData_", menuName = "Progression/Skill Data", order = 2)]
    public class SkillDataSO : ScriptableObject
    {
        [Tooltip("A unique identifier for this skill.")]
        public string skillID;

        [Tooltip("The display name of the skill.")]
        public string skillName;

        [Tooltip("A brief description of the skill.")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("The maximum level this skill can reach (1 for unlevelable skills).")]
        public int maxLevel = 1;

        [Tooltip("The number of skill points required to learn/upgrade this skill once.")]
        public int costPerLevel = 1;

        [Tooltip("Visual icon for the skill.")]
        public Sprite icon;

        // Example: Effects could be defined here using a custom struct/class
        // public List<SkillEffect> effects;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(skillID))
            {
                skillID = name; // Auto-populate ID
            }
            if (maxLevel < 1) maxLevel = 1;
            if (costPerLevel < 0) costPerLevel = 0;
        }
    }
}
```

### `LevelDataSO.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// Struct to define a bonus applied to a specific stat.
    /// Used within LevelDataSO to specify stat increases upon leveling up.
    /// </summary>
    [Serializable]
    public struct StatBonus
    {
        public StatDataSO stat;
        public float value;
    }

    /// <summary>
    /// ScriptableObject defining the requirements and rewards for a specific player level.
    /// Each LevelDataSO represents a single level.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Progression/Level Data", order = 3)]
    public class LevelDataSO : ScriptableObject
    {
        [Tooltip("The specific level this data defines (e.g., 1, 2, 3).")]
        public int level;

        [Tooltip("The total experience points required to reach this level from level 0.")]
        public int requiredXP;

        [Tooltip("The number of skill points awarded upon reaching this level.")]
        public int skillPointsAwarded;

        [Tooltip("List of stat bonuses applied when the player reaches this level.")]
        public List<StatBonus> statBonuses = new List<StatBonus>();

        [Tooltip("List of skills automatically unlocked when the player reaches this level.")]
        public List<SkillDataSO> unlockedSkills = new List<SkillDataSO>();

        private void OnValidate()
        {
            if (level < 1) level = 1;
            if (requiredXP < 0) requiredXP = 0;
            if (skillPointsAwarded < 0) skillPointsAwarded = 0;
        }
    }
}
```

### `ProgressionConfigSO.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// Struct to define an initial stat value for a player.
    /// Used within ProgressionConfigSO for setting up starting player stats.
    /// </summary>
    [System.Serializable]
    public struct InitialStat
    {
        public StatDataSO stat;
        public float value;
    }

    /// <summary>
    /// ScriptableObject acting as the central configuration for the entire player progression system.
    /// It holds references to all level definitions and initial player settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionConfig_", menuName = "Progression/Progression Config", order = 0)]
    public class ProgressionConfigSO : ScriptableObject
    {
        [Tooltip("A sorted list of all level definitions, from Level 1 upwards.")]
        public List<LevelDataSO> levelDefinitions = new List<LevelDataSO>();

        [Tooltip("The starting amount of skill points the player has.")]
        public int startingSkillPoints = 0;

        [Tooltip("The initial values for various player stats.")]
        public List<InitialStat> initialStats = new List<InitialStat>();

        /// <summary>
        /// Retrieves the LevelDataSO for a specific level.
        /// </summary>
        /// <param name="level">The level to retrieve data for.</param>
        /// <returns>The LevelDataSO for the specified level, or null if not found.</returns>
        public LevelDataSO GetLevelData(int level)
        {
            if (level <= 0 || level > levelDefinitions.Count)
            {
                return null;
            }
            // Levels are 1-indexed, list is 0-indexed
            return levelDefinitions[level - 1];
        }

        /// <summary>
        /// Gets the total number of defined levels.
        /// </summary>
        public int MaxLevel => levelDefinitions.Count;

        private void OnValidate()
        {
            // Optional: Sort level definitions by level number for consistency
            // if (levelDefinitions != null && levelDefinitions.Count > 0)
            // {
            //     levelDefinitions.Sort((a, b) => a.level.CompareTo(b.level));
            // }
            if (startingSkillPoints < 0) startingSkillPoints = 0;
        }
    }
}
```

### `PlayerProgressionSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// The core Player Progression System manager.
    /// This MonoBehaviour handles all player progression logic, including XP, leveling,
    /// skill points, stats, and skill unlocks. It uses a Singleton pattern for easy access
    /// from other parts of the game.
    /// </summary>
    public class PlayerProgressionSystem : MonoBehaviour
    {
        // --- Singleton Pattern ---
        public static PlayerProgressionSystem Instance { get; private set; }

        // --- Configuration ---
        [Tooltip("The ScriptableObject containing all progression settings (levels, initial stats).")]
        [SerializeField] private ProgressionConfigSO progressionConfig;

        // --- Current Player State ---
        private int _currentXP;
        private int _currentLevel;
        private int _currentSkillPoints;
        // Dictionary to store current effective stat values (StatDataSO.statID, value)
        private Dictionary<string, float> _playerStats = new Dictionary<string, float>();
        // Dictionary to store unlocked skills and their current level (SkillDataSO.skillID, skillLevel)
        private Dictionary<string, int> _unlockedSkills = new Dictionary<string, int>();

        // --- Events for UI and other systems to subscribe to ---
        public static event Action<int, int> OnXPChanged;                 // Current XP, XP needed for next level
        public static event Action<int> OnLevelUp;                        // New level reached
        public static event Action<StatDataSO, float> OnStatChanged;      // Stat that changed, new value
        public static event Action<SkillDataSO, int> OnSkillUnlocked;     // Skill unlocked, level unlocked at (always 1 for first unlock)
        public static event Action<SkillDataSO, int> OnSkillLeveledUp;   // Skill leveled up, new skill level
        public static event Action<int> OnSkillPointsChanged;             // New total skill points

        // --- Public Properties to access current state ---
        public int CurrentXP => _currentXP;
        public int CurrentLevel => _currentLevel;
        public int CurrentSkillPoints => _currentSkillPoints;
        public int MaxLevel => progressionConfig.MaxLevel;

        /// <summary>
        /// Returns the XP required to reach the next level.
        /// Returns 0 if already at max level.
        /// </summary>
        public int XPToNextLevel
        {
            get
            {
                if (_currentLevel >= MaxLevel) return 0;
                LevelDataSO nextLevelData = progressionConfig.GetLevelData(_currentLevel + 1);
                return nextLevelData != null ? nextLevelData.requiredXP - _currentXP : 0;
            }
        }

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            // Enforce Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PlayerProgressionSystem instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes

            InitializeProgression();
        }

        /// <summary>
        /// Initializes the player's progression state based on the ProgressionConfigSO.
        /// Called on Awake, or can be called explicitly (e.g., after loading a save game).
        /// </summary>
        private void InitializeProgression()
        {
            if (progressionConfig == null)
            {
                Debug.LogError("ProgressionConfigSO is not assigned to PlayerProgressionSystem!");
                return;
            }

            // Reset state
            _currentXP = 0;
            _currentLevel = 1; // Start at Level 1
            _currentSkillPoints = progressionConfig.startingSkillPoints;
            _playerStats.Clear();
            _unlockedSkills.Clear();

            // Apply initial stats from config
            foreach (var initialStat in progressionConfig.initialStats)
            {
                if (initialStat.stat != null)
                {
                    _playerStats[initialStat.stat.statID] = initialStat.value;
                    OnStatChanged?.Invoke(initialStat.stat, initialStat.value);
                }
            }

            // Trigger initial events for UI update
            OnXPChanged?.Invoke(_currentXP, XPToNextLevel);
            OnLevelUp?.Invoke(_currentLevel); // Notify current level
            OnSkillPointsChanged?.Invoke(_currentSkillPoints);

            Debug.Log($"Progression System Initialized: Level {_currentLevel}, XP {_currentXP}, Skill Points {_currentSkillPoints}");
        }

        // --- Core Progression Methods ---

        /// <summary>
        /// Awards experience points to the player.
        /// Checks for level-ups and applies associated rewards.
        /// </summary>
        /// <param name="amount">The amount of XP to gain.</param>
        public void GainXP(int amount)
        {
            if (amount <= 0) return;

            _currentXP += amount;
            Debug.Log($"Gained {amount} XP. Total XP: {_currentXP}");

            // Check for level ups
            CheckForLevelUp();

            OnXPChanged?.Invoke(_currentXP, XPToNextLevel);
        }

        /// <summary>
        /// Internal method to check if the player has enough XP to level up.
        /// Applies all level-up rewards if a new level is reached.
        /// </summary>
        private void CheckForLevelUp()
        {
            bool leveledUpThisCheck = false;
            while (_currentLevel < MaxLevel)
            {
                LevelDataSO nextLevelData = progressionConfig.GetLevelData(_currentLevel + 1);

                if (nextLevelData == null || _currentXP < nextLevelData.requiredXP)
                {
                    break; // Not enough XP for the next level or no more levels defined
                }

                // Level Up!
                _currentLevel++;
                leveledUpThisCheck = true;
                Debug.Log($"Player Leveled Up to Level {_currentLevel}!");

                // Award Skill Points
                _currentSkillPoints += nextLevelData.skillPointsAwarded;
                OnSkillPointsChanged?.Invoke(_currentSkillPoints);
                Debug.Log($"Awarded {nextLevelData.skillPointsAwarded} skill points. Total: {_currentSkillPoints}");

                // Apply Stat Bonuses
                foreach (var bonus in nextLevelData.statBonuses)
                {
                    if (bonus.stat != null)
                    {
                        ApplyStatBonus(bonus.stat, bonus.value);
                        Debug.Log($"Stat '{bonus.stat.statName}' increased by {bonus.value}. New value: {GetStatValue(bonus.stat)}");
                    }
                }

                // Unlock Skills
                foreach (var skill in nextLevelData.unlockedSkills)
                {
                    if (skill != null && !_unlockedSkills.ContainsKey(skill.skillID))
                    {
                        _unlockedSkills[skill.skillID] = 1; // Unlock at level 1
                        OnSkillUnlocked?.Invoke(skill, 1);
                        Debug.Log($"Skill '{skill.skillName}' unlocked automatically at Level {_currentLevel}.");
                    }
                }

                OnLevelUp?.Invoke(_currentLevel);
            }

            if (leveledUpThisCheck)
            {
                OnXPChanged?.Invoke(_currentXP, XPToNextLevel); // Update XP display as XP needed might change
            }
        }

        /// <summary>
        /// Attempts to unlock a skill for the player.
        /// </summary>
        /// <param name="skill">The SkillDataSO to unlock.</param>
        /// <returns>True if the skill was successfully unlocked, false otherwise.</returns>
        public bool UnlockSkill(SkillDataSO skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("Attempted to unlock a null skill.");
                return false;
            }

            if (_unlockedSkills.ContainsKey(skill.skillID))
            {
                Debug.Log($"Skill '{skill.skillName}' is already unlocked.");
                // Potentially call UpgradeSkill here if skill has levels
                return false;
            }

            if (_currentSkillPoints < skill.costPerLevel)
            {
                Debug.Log($"Not enough skill points to unlock '{skill.skillName}'. Cost: {skill.costPerLevel}, Current: {_currentSkillPoints}");
                return false;
            }

            // TODO: Add more complex logic like prerequisites here

            _currentSkillPoints -= skill.costPerLevel;
            _unlockedSkills[skill.skillID] = 1; // Unlock at level 1
            OnSkillUnlocked?.Invoke(skill, 1);
            OnSkillPointsChanged?.Invoke(_currentSkillPoints);

            // Apply any immediate effects of the skill (e.g., stat bonuses)
            // For this example, we'll just log it.
            Debug.Log($"Skill '{skill.skillName}' unlocked! Remaining Skill Points: {_currentSkillPoints}");

            return true;
        }

        /// <summary>
        /// Attempts to upgrade an already unlocked skill.
        /// </summary>
        /// <param name="skill">The SkillDataSO to upgrade.</param>
        /// <returns>True if the skill was successfully upgraded, false otherwise.</returns>
        public bool UpgradeSkill(SkillDataSO skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("Attempted to upgrade a null skill.");
                return false;
            }

            if (!_unlockedSkills.ContainsKey(skill.skillID))
            {
                Debug.Log($"Skill '{skill.skillName}' is not yet unlocked. Cannot upgrade.");
                return false;
            }

            int currentSkillLevel = _unlockedSkills[skill.skillID];
            if (currentSkillLevel >= skill.maxLevel)
            {
                Debug.Log($"Skill '{skill.skillName}' is already at max level ({skill.maxLevel}).");
                return false;
            }

            if (_currentSkillPoints < skill.costPerLevel)
            {
                Debug.Log($"Not enough skill points to upgrade '{skill.skillName}'. Cost: {skill.costPerLevel}, Current: {_currentSkillPoints}");
                return false;
            }

            _currentSkillPoints -= skill.costPerLevel;
            _unlockedSkills[skill.skillID] = currentSkillLevel + 1; // Increment skill level
            OnSkillLeveledUp?.Invoke(skill, currentSkillLevel + 1);
            OnSkillPointsChanged?.Invoke(_currentSkillPoints);

            // Apply any effects of the skill upgrade (e.g., increased stat bonuses)
            Debug.Log($"Skill '{skill.skillName}' upgraded to level {_unlockedSkills[skill.skillID]}! Remaining Skill Points: {_currentSkillPoints}");
            return true;
        }

        /// <summary>
        /// Applies a stat bonus to the player's stats.
        /// This is an internal method, exposed to be used by level-ups and skills.
        /// </summary>
        /// <param name="stat">The StatDataSO to modify.</param>
        /// <param name="value">The amount to add to the stat.</param>
        private void ApplyStatBonus(StatDataSO stat, float value)
        {
            if (stat == null) return;

            if (!_playerStats.ContainsKey(stat.statID))
            {
                // If stat doesn't exist yet, initialize it with 0 + value
                _playerStats[stat.statID] = value;
            }
            else
            {
                _playerStats[stat.statID] += value;
            }
            OnStatChanged?.Invoke(stat, _playerStats[stat.statID]);
        }

        // --- Public Getters ---

        /// <summary>
        /// Retrieves the current value of a specific player stat.
        /// </summary>
        /// <param name="stat">The StatDataSO to query.</param>
        /// <returns>The current value of the stat, or 0 if the stat is not found.</returns>
        public float GetStatValue(StatDataSO stat)
        {
            if (stat == null || !_playerStats.ContainsKey(stat.statID))
            {
                return 0f;
            }
            return _playerStats[stat.statID];
        }

        /// <summary>
        /// Checks if a skill is currently unlocked.
        /// </summary>
        /// <param name="skill">The SkillDataSO to check.</param>
        /// <returns>True if the skill is unlocked, false otherwise.</returns>
        public bool IsSkillUnlocked(SkillDataSO skill)
        {
            if (skill == null) return false;
            return _unlockedSkills.ContainsKey(skill.skillID);
        }

        /// <summary>
        /// Returns the current level of an unlocked skill.
        /// </summary>
        /// <param name="skill">The SkillDataSO to query.</param>
        /// <returns>The level of the skill, or 0 if not unlocked.</returns>
        public int GetSkillLevel(SkillDataSO skill)
        {
            if (skill == null || !_unlockedSkills.ContainsKey(skill.skillID))
            {
                return 0;
            }
            return _unlockedSkills[skill.skillID];
        }

        /// <summary>
        /// Gets a read-only dictionary of all player stats and their values.
        /// </summary>
        public IReadOnlyDictionary<string, float> GetAllPlayerStats() => _playerStats;

        /// <summary>
        /// Gets a read-only dictionary of all unlocked skills and their levels.
        /// </summary>
        public IReadOnlyDictionary<string, int> GetUnlockedSkills() => _unlockedSkills;


        // --- Save/Load Placeholder ---
        // For a full implementation, you would serialize _currentXP, _currentLevel,
        // _currentSkillPoints, _playerStats, and _unlockedSkills using JsonUtility,
        // binary serialization, or a custom save system.
        [ContextMenu("Save Progression (Placeholder)")]
        public void SaveProgression()
        {
            // Example: Convert to a serializable data class
            // var saveData = new PlayerProgressionData
            // {
            //     xp = _currentXP,
            //     level = _currentLevel,
            //     skillPoints = _currentSkillPoints,
            //     stats = _playerStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            //     unlockedSkills = _unlockedSkills.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            // };
            // string json = JsonUtility.ToJson(saveData);
            // PlayerPrefs.SetString("PlayerProgression", json);
            Debug.Log("Progression Saved (Placeholder): Current state is logged, not actually persisted.");
            Debug.Log($"XP: {_currentXP}, Level: {_currentLevel}, SP: {_currentSkillPoints}");
            Debug.Log($"Stats: {string.Join(", ", _playerStats.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            Debug.Log($"Skills: {string.Join(", ", _unlockedSkills.Select(kv => $"{kv.Key} (Lvl {kv.Value})"))}");
        }

        [ContextMenu("Load Progression (Placeholder)")]
        public void LoadProgression()
        {
            // string json = PlayerPrefs.GetString("PlayerProgression");
            // if (!string.IsNullOrEmpty(json))
            // {
            //     PlayerProgressionData loadedData = JsonUtility.FromJson<PlayerProgressionData>(json);
            //     _currentXP = loadedData.xp;
            //     _currentLevel = loadedData.level;
            //     _currentSkillPoints = loadedData.skillPoints;
            //     _playerStats = loadedData.stats;
            //     _unlockedSkills = loadedData.unlockedSkills;

            //     // Re-trigger events to update UI etc.
            //     OnXPChanged?.Invoke(_currentXP, XPToNextLevel);
            //     OnLevelUp?.Invoke(_currentLevel);
            //     OnSkillPointsChanged?.Invoke(_currentSkillPoints);
            //     foreach (var stat in _playerStats) OnStatChanged?.Invoke(GetStatDataSO(stat.Key), stat.Value); // Needs a helper to convert ID back to SO
            //     foreach (var skill in _unlockedSkills) OnSkillUnlocked?.Invoke(GetSkillDataSO(skill.Key), skill.Value); // Needs a helper

            //     Debug.Log("Progression Loaded (Placeholder).");
            // }
            // else
            // {
            //     Debug.Log("No saved progression found. Initializing new game.");
            //     InitializeProgression();
            // }
            Debug.LogWarning("Load Progression is a placeholder. It currently re-initializes the system, not loads from save.");
            InitializeProgression(); // For example, just restart with initial values
        }

        // Helper method to get StatDataSO from ID (would need a global registry or lookup in ProgressionConfigSO)
        // private StatDataSO GetStatDataSO(string statID) { /* Implementation needed */ return null; }
        // private SkillDataSO GetSkillDataSO(string skillID) { /* Implementation needed */ return null; }

        // [System.Serializable] // Example data structure for serialization
        // private class PlayerProgressionData
        // {
        //     public int xp;
        //     public int level;
        //     public int skillPoints;
        //     public Dictionary<string, float> stats;
        //     public Dictionary<string, int> unlockedSkills;
        // }
    }
}
```

### `ProgressionUIExample.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PlayerProgressionSystem
{
    /// <summary>
    /// Example UI script that subscribes to PlayerProgressionSystem events
    /// to update UI elements (text, buttons) showing player progress.
    /// </summary>
    public class ProgressionUIExample : MonoBehaviour
    {
        [Header("UI Text Fields")]
        public Text levelText;
        public Text xpText;
        public Text skillPointsText;

        [Header("Stat Text Fields")]
        public List<StatUIText> statTexts; // List of stat and its corresponding Text field

        [Header("Skill UI (Optional)")]
        public Text unlockedSkillsText; // A single text field to list all unlocked skills
        public GameObject skillUnlockPanel; // Panel to show skills that can be unlocked
        public GameObject skillButtonPrefab; // Prefab for skill buttons (needs a Button component and a Text child)

        [Header("Debug Buttons (Optional)")]
        public Button gainXpButton;
        public int xpGainAmount = 50;

        // Internal mapping for quick stat UI updates
        private Dictionary<StatDataSO, Text> _statTextMap = new Dictionary<StatDataSO, Text>();

        [Serializable]
        public class StatUIText
        {
            public StatDataSO stat;
            public Text displayTxt;
        }

        private void OnEnable()
        {
            // Subscribe to progression events
            PlayerProgressionSystem.OnXPChanged += UpdateXPUI;
            PlayerProgressionSystem.OnLevelUp += UpdateLevelUI;
            PlayerProgressionSystem.OnSkillPointsChanged += UpdateSkillPointsUI;
            PlayerProgressionSystem.OnStatChanged += UpdateSpecificStatUI;
            PlayerProgressionSystem.OnSkillUnlocked += HandleSkillUnlocked;
            PlayerProgressionSystem.OnSkillLeveledUp += HandleSkillLeveledUp;

            // Setup debug buttons
            if (gainXpButton != null)
            {
                gainXpButton.onClick.AddListener(() => PlayerProgressionSystem.Instance.GainXP(xpGainAmount));
            }

            // Populate stat text map for efficient updates
            foreach (var statUI in statTexts)
            {
                if (statUI.stat != null && statUI.displayTxt != null)
                {
                    _statTextMap[statUI.stat] = statUI.displayTxt;
                }
            }

            // Initial UI update if system is already initialized
            if (PlayerProgressionSystem.Instance != null)
            {
                UpdateLevelUI(PlayerProgressionSystem.Instance.CurrentLevel);
                UpdateXPUI(PlayerProgressionSystem.Instance.CurrentXP, PlayerProgressionSystem.Instance.XPToNextLevel);
                UpdateSkillPointsUI(PlayerProgressionSystem.Instance.CurrentSkillPoints);

                foreach (var statEntry in _statTextMap)
                {
                    UpdateSpecificStatUI(statEntry.Key, PlayerProgressionSystem.Instance.GetStatValue(statEntry.Key));
                }
                UpdateUnlockedSkillsList(); // Initial list of unlocked skills
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from progression events to prevent memory leaks
            PlayerProgressionSystem.OnXPChanged -= UpdateXPUI;
            PlayerProgressionSystem.OnLevelUp -= UpdateLevelUI;
            PlayerProgressionSystem.OnSkillPointsChanged -= UpdateSkillPointsUI;
            PlayerProgressionSystem.OnStatChanged -= UpdateSpecificStatUI;
            PlayerProgressionSystem.OnSkillUnlocked -= HandleSkillUnlocked;
            PlayerProgressionSystem.OnSkillLeveledUp -= HandleSkillLeveledUp;

            if (gainXpButton != null)
            {
                gainXpButton.onClick.RemoveAllListeners();
            }
        }

        private void UpdateLevelUI(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"Level: {newLevel}";
            }
        }

        private void UpdateXPUI(int currentXP, int xpToNextLevel)
        {
            if (xpText != null)
            {
                if (PlayerProgressionSystem.Instance.CurrentLevel >= PlayerProgressionSystem.Instance.MaxLevel)
                {
                    xpText.text = $"XP: {currentXP} (MAX LEVEL)";
                }
                else
                {
                    xpText.text = $"XP: {currentXP} / {currentXP + xpToNextLevel}";
                }
            }
        }

        private void UpdateSkillPointsUI(int newSkillPoints)
        {
            if (skillPointsText != null)
            {
                skillPointsText.text = $"Skill Points: {newSkillPoints}";
            }
        }

        private void UpdateSpecificStatUI(StatDataSO stat, float newValue)
        {
            if (_statTextMap.TryGetValue(stat, out Text textComponent))
            {
                textComponent.text = $"{stat.statName}: {newValue:F0}"; // Format to integer for display
            }
        }

        private void HandleSkillUnlocked(SkillDataSO skill, int level)
        {
            Debug.Log($"UI: Skill '{skill.skillName}' (Level {level}) Unlocked!");
            UpdateUnlockedSkillsList();
            UpdateUnlockableSkillsPanel();
        }

        private void HandleSkillLeveledUp(SkillDataSO skill, int newLevel)
        {
            Debug.Log($"UI: Skill '{skill.skillName}' upgraded to Level {newLevel}!");
            UpdateUnlockedSkillsList();
            UpdateUnlockableSkillsPanel();
        }

        private void UpdateUnlockedSkillsList()
        {
            if (unlockedSkillsText == null) return;

            string skillsList = "Unlocked Skills:\n";
            var unlockedSkills = PlayerProgressionSystem.Instance.GetUnlockedSkills();
            if (unlockedSkills.Count == 0)
            {
                skillsList += "None";
            }
            else
            {
                foreach (var kvp in unlockedSkills)
                {
                    // For a real game, you'd probably need to resolve the SkillDataSO from the ID
                    // For this example, assuming we have a way to get the name (e.g., a lookup or cached SO)
                    // Simplified: just show the ID and level
                    skillsList += $"- {kvp.Key} (Lvl {kvp.Value})\n";
                }
            }
            unlockedSkillsText.text = skillsList;
        }

        // Example: Dynamically creating buttons for skills that can be unlocked/upgraded
        public void UpdateUnlockableSkillsPanel()
        {
            if (skillUnlockPanel == null || skillButtonPrefab == null) return;

            // Clear existing buttons
            foreach (Transform child in skillUnlockPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Find all SkillDataSOs in the project (this is for example purposes; in a real game
            // you might have a curated list in a ProgressionConfigSO or similar)
            SkillDataSO[] allSkills = Resources.LoadAll<SkillDataSO>("");

            foreach (SkillDataSO skill in allSkills)
            {
                bool isUnlocked = PlayerProgressionSystem.Instance.IsSkillUnlocked(skill);
                int currentSkillLevel = PlayerProgressionSystem.Instance.GetSkillLevel(skill);

                // Option 1: Display all skills and their status
                // Option 2: Only display skills that can be unlocked/upgraded
                // For this example, we'll focus on unlockable/upgradable skills
                if (isUnlocked && currentSkillLevel >= skill.maxLevel) continue; // Skip maxed out skills

                GameObject skillButtonGO = Instantiate(skillButtonPrefab, skillUnlockPanel.transform);
                Button button = skillButtonGO.GetComponent<Button>();
                Text buttonText = skillButtonGO.GetComponentInChildren<Text>();

                if (buttonText != null)
                {
                    if (!isUnlocked)
                    {
                        buttonText.text = $"Unlock {skill.skillName} ({skill.costPerLevel} SP)";
                        button.interactable = PlayerProgressionSystem.Instance.CurrentSkillPoints >= skill.costPerLevel;
                        button.onClick.AddListener(() => {
                            PlayerProgressionSystem.Instance.UnlockSkill(skill);
                            UpdateUnlockableSkillsPanel(); // Refresh panel after attempt
                        });
                    }
                    else // Skill is unlocked, but not maxed
                    {
                        buttonText.text = $"Upgrade {skill.skillName} Lvl {currentSkillLevel + 1} ({skill.costPerLevel} SP)";
                        button.interactable = PlayerProgressionSystem.Instance.CurrentSkillPoints >= skill.costPerLevel;
                        button.onClick.AddListener(() => {
                            PlayerProgressionSystem.Instance.UpgradeSkill(skill);
                            UpdateUnlockableSkillsPanel(); // Refresh panel after attempt
                        });
                    }
                }
            }
        }
    }
}
```

### `ProgressionTriggerExample.cs`

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button

namespace PlayerProgressionSystem
{
    /// <summary>
    /// An example script to demonstrate how game logic can interact with the PlayerProgressionSystem.
    /// It provides simple buttons to gain XP and try to unlock a specific skill.
    /// </summary>
    public class ProgressionTriggerExample : MonoBehaviour
    {
        [Header("XP Gain")]
        [Tooltip("Button to trigger XP gain.")]
        public Button gainXPButton;
        [Tooltip("Amount of XP to gain when the button is pressed.")]
        public int xpAmount = 50;

        [Header("Skill Unlocking")]
        [Tooltip("The SkillDataSO to attempt to unlock/upgrade with the button.")]
        public SkillDataSO targetSkill;
        [Tooltip("Button to trigger skill unlock/upgrade.")]
        public Button unlockSkillButton;


        private void Start()
        {
            if (gainXPButton != null)
            {
                gainXPButton.onClick.AddListener(OnGainXPButtonClick);
            }

            if (unlockSkillButton != null)
            {
                unlockSkillButton.onClick.AddListener(OnUnlockSkillButtonClick);
            }

            // Ensure the ProgressionUIExample updates its skill panel if it's active
            ProgressionUIExample uiExample = FindObjectOfType<ProgressionUIExample>();
            if (uiExample != null)
            {
                uiExample.UpdateUnlockableSkillsPanel();
            }
        }

        private void OnDestroy()
        {
            if (gainXPButton != null)
            {
                gainXPButton.onClick.RemoveListener(OnGainXPButtonClick);
            }

            if (unlockSkillButton != null)
            {
                unlockSkillButton.onClick.RemoveListener(OnUnlockSkillButtonClick);
            }
        }

        private void OnGainXPButtonClick()
        {
            if (PlayerProgressionSystem.Instance != null)
            {
                PlayerProgressionSystem.Instance.GainXP(xpAmount);
                Debug.Log($"Triggered XP gain: {xpAmount}");
            }
            else
            {
                Debug.LogError("PlayerProgressionSystem.Instance is not available!");
            }
        }

        private void OnUnlockSkillButtonClick()
        {
            if (PlayerProgressionSystem.Instance != null && targetSkill != null)
            {
                if (!PlayerProgressionSystem.Instance.IsSkillUnlocked(targetSkill))
                {
                    PlayerProgressionSystem.Instance.UnlockSkill(targetSkill);
                }
                else if (PlayerProgressionSystem.Instance.GetSkillLevel(targetSkill) < targetSkill.maxLevel)
                {
                    PlayerProgressionSystem.Instance.UpgradeSkill(targetSkill);
                }
                else
                {
                    Debug.Log($"Skill '{targetSkill.skillName}' is already at max level.");
                }

                // Update UI after attempting skill action
                ProgressionUIExample uiExample = FindObjectOfType<ProgressionUIExample>();
                if (uiExample != null)
                {
                    uiExample.UpdateUnlockableSkillsPanel();
                }
            }
            else
            {
                Debug.LogError("PlayerProgressionSystem.Instance or targetSkill is not available!");
            }
        }

        // Example: Gaining XP when an enemy is defeated (called from an Enemy script)
        public void OnEnemyDefeated(int enemyXPValue)
        {
            if (PlayerProgressionSystem.Instance != null)
            {
                PlayerProgressionSystem.Instance.GainXP(enemyXPValue);
                Debug.Log($"Gained {enemyXPValue} XP from enemy defeat.");
            }
        }

        // Example: Unlocking a skill from a quest reward
        public void OnQuestCompleted(SkillDataSO rewardSkill)
        {
            if (PlayerProgressionSystem.Instance != null && rewardSkill != null)
            {
                PlayerProgressionSystem.Instance.UnlockSkill(rewardSkill);
                Debug.Log($"Unlocked skill '{rewardSkill.skillName}' from quest reward.");
            }
        }
    }
}
```

---

### Example Usage in Comments:

**How to get player level from another script:**

```csharp
// In any other script:
if (PlayerProgressionSystem.Instance != null)
{
    int currentLevel = PlayerProgressionSystem.Instance.CurrentLevel;
    Debug.Log("Player's current level: " + currentLevel);
}
```

**How to gain XP from game events (e.g., enemy killed, quest completed):**

```csharp
// In an Enemy script when it dies:
public int xpValue = 25;
private void OnDeath()
{
    if (PlayerProgressionSystem.Instance != null)
    {
        PlayerProgressionSystem.Instance.GainXP(xpValue);
        Debug.Log("Player gained " + xpValue + " XP from killing enemy!");
    }
    // ... other death logic ...
}

// Or in a Quest Manager script:
public SkillDataSO rewardSkill; // Assign in Inspector
public int questXP = 100;
private void OnQuestCompletion()
{
    if (PlayerProgressionSystem.Instance != null)
    {
        PlayerProgressionSystem.Instance.GainXP(questXP);
        PlayerProgressionSystem.Instance.UnlockSkill(rewardSkill);
        Debug.Log("Player completed quest and gained " + questXP + " XP and unlocked " + rewardSkill.skillName);
    }
}
```

**How to react to a player leveling up (e.g., show a 'Level Up!' animation):**

```csharp
// In a UI Manager or VFX Manager script:
private void OnEnable()
{
    PlayerProgressionSystem.OnLevelUp += HandlePlayerLevelUp;
}

private void OnDisable()
{
    PlayerProgressionSystem.OnLevelUp -= HandlePlayerLevelUp;
}

private void HandlePlayerLevelUp(int newLevel)
{
    Debug.Log("Player has leveled up! New level: " + newLevel);
    // Trigger Level Up animation
    // Play sound effect
    // Show a congratulatory message
}
```

**How to check a player's stat (e.g., for damage calculation):**

```csharp
// In a Combat System script:
public StatDataSO attackStat; // Assign your "Attack" StatDataSO in the Inspector

public float CalculateDamage()
{
    if (PlayerProgressionSystem.Instance != null)
    {
        float playerAttack = PlayerProgressionSystem.Instance.GetStatValue(attackStat);
        // Add weapon damage, etc.
        float totalDamage = playerAttack + 10; // Example: 10 base weapon damage
        return totalDamage;
    }
    return 0;
}
```

This complete example provides a robust foundation for a player progression system in Unity, emphasizing a data-driven approach and loose coupling using events.