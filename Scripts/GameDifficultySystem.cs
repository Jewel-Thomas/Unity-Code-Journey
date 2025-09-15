// Unity Design Pattern Example: GameDifficultySystem
// This script demonstrates the GameDifficultySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Game Difficulty System design pattern provides a centralized way to manage and retrieve game difficulty settings. Instead of scattering difficulty-related magic numbers throughout your codebase, this pattern funnels all difficulty-specific configurations through a single manager. Other game components (enemies, player, UI, environmental hazards) can then query this manager to adapt their behavior based on the current difficulty level.

This example uses a combination of:
1.  **`DifficultyLevel` Enum:** Defines the discrete difficulty levels (e.g., Easy, Normal, Hard).
2.  **`DifficultyPreset` ScriptableObject:** Holds all configurable parameters for a *specific* difficulty level. This allows designers to easily create and tweak difficulty settings directly in the Unity Editor without touching code.
3.  **`GameDifficultyManager` (Singleton MonoBehaviour):** The central hub. It stores the current difficulty, provides methods to change it, and offers query methods for other systems to get difficulty-adjusted values. It also uses a C# event to notify subscribers when the difficulty changes.
4.  **Example Game Elements:** Simple `MonoBehaviour` classes (Player, Enemy, UI) that demonstrate how to interact with the `GameDifficultyManager` to adjust their behavior or display information.

---

To use this system:
1.  Save the code below as `GameDifficultySystem.cs` in your Unity project.
2.  In Unity, right-click in your Project window -> Create -> Game Difficulty -> Difficulty Preset. Create multiple presets (e.g., "Easy Preset", "Normal Preset", "Hard Preset").
3.  Fill in the values for each `DifficultyPreset` in the Inspector (e.g., Easy: Health Mult = 0.8, Damage Mult = 0.7; Hard: Health Mult = 1.5, Damage Mult = 1.2).
4.  Create an empty GameObject in your scene, name it "GameDifficultyManager", and attach the `GameDifficultyManager` script to it.
5.  Drag your created `DifficultyPreset` assets into the `Difficulty Presets` list in the `GameDifficultyManager`'s Inspector. Ensure they are assigned to their corresponding `DifficultyLevel` in the list (e.g., Index 0 for Easy, Index 1 for Normal, etc.).
6.  The `Initial Difficulty` field in the `GameDifficultyManager` Inspector determines what difficulty the game starts with.
7.  Add the `ExamplePlayerController`, `ExampleEnemyController`, or `ExampleDifficultyUI` components to other GameObjects in your scene to see them react to difficulty changes.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // Required for Dictionary
using System.Linq; // Required for Linq methods like .FirstOrDefault()

namespace GameDifficultySystem
{
    // #region 1. DifficultyLevel Enum
    // ====================================================================================================
    // 1. DifficultyLevel Enum
    // This enum defines the distinct difficulty levels available in the game.
    // It's used to identify specific difficulty presets and to set the game's current difficulty.
    // ====================================================================================================
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert // Example of adding a new level
    }
    // #endregion

    // #region 2. DifficultyPreset ScriptableObject
    // ====================================================================================================
    // 2. DifficultyPreset ScriptableObject
    // This ScriptableObject holds all the configurable parameters for a *single* difficulty level.
    // By using ScriptableObjects, designers can create and tweak difficulty settings directly in
    // the Unity Editor without touching code, promoting data-driven design.
    // ====================================================================================================
    [CreateAssetMenu(fileName = "NewDifficultyPreset", menuName = "Game Difficulty/Difficulty Preset")]
    public class DifficultyPreset : ScriptableObject
    {
        [Header("Preset Identification")]
        [Tooltip("The specific difficulty level this preset corresponds to.")]
        public DifficultyLevel difficultyLevel;

        [Tooltip("A display name for this difficulty level, useful for UI.")]
        public string difficultyName = "Default Difficulty";

        [Header("Gameplay Adjustments")]
        [Tooltip("Multiplier for enemy health. e.g., 1.0 = normal, 0.8 = 20% less health, 1.5 = 50% more health.")]
        [Range(0.1f, 5.0f)] public float enemyHealthMultiplier = 1.0f;

        [Tooltip("Multiplier for damage dealt by enemies. e.g., 1.0 = normal, 0.5 = half damage, 2.0 = double damage.")]
        [Range(0.1f, 5.0f)] public float enemyDamageMultiplier = 1.0f;

        [Tooltip("Multiplier for damage dealt by the player. e.g., 1.0 = normal, 1.2 = 20% more damage, 0.7 = 30% less damage.")]
        [Range(0.1f, 5.0f)] public float playerDamageMultiplier = 1.0f;

        [Tooltip("Multiplier for resources (gold, XP) gained by the player. e.g., 1.0 = normal, 1.5 = 50% more resources.")]
        [Range(0.1f, 5.0f)] public float resourceGainMultiplier = 1.0f;

        [Tooltip("Multiplier for player's special ability cooldowns. e.g., 1.0 = normal, 0.8 = 20% faster cooldowns, 1.2 = 20% slower cooldowns.")]
        [Range(0.1f, 2.0f)] public float specialAbilityCooldownMultiplier = 1.0f;

        [Tooltip("Base score awarded for completing levels or tasks. Not a multiplier, but a direct value.")]
        public int baseScoreAward = 1000;

        // You can add many more difficulty-specific parameters here:
        // public float environmentalHazardDamage = 10f;
        // public int numberOfEnemiesSpawned = 5;
        // public bool enableHardcoreModeFeatures = false;
        // ...and so on.

        private void OnValidate()
        {
            // Ensures the display name is updated if the enum is changed and the name isn't manually set.
            if (string.IsNullOrEmpty(difficultyName) || difficultyName == "Default Difficulty")
            {
                difficultyName = difficultyLevel.ToString();
            }
        }
    }
    // #endregion

    // #region 3. GameDifficultyManager (Singleton MonoBehaviour)
    // ====================================================================================================
    // 3. GameDifficultyManager (Singleton MonoBehaviour)
    // This is the core of the Game Difficulty System. It's a Singleton, meaning there's only one
    // instance throughout the game, providing a global access point for difficulty settings.
    // It manages the current difficulty level, stores all available difficulty presets, and
    // notifies other systems when the difficulty changes.
    // ====================================================================================================
    public class GameDifficultyManager : MonoBehaviour
    {
        // Static instance of the GameDifficultyManager, implementing the Singleton pattern.
        private static GameDifficultyManager _instance;

        // Public static property to access the Singleton instance.
        // This ensures lazy instantiation and thread safety.
        public static GameDifficultyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameDifficultyManager>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(GameDifficultyManager).Name);
                        _instance = singletonObject.AddComponent<GameDifficultyManager>();
                        Debug.LogWarning($"GameDifficultyManager: No instance found in scene, creating new one. " +
                                         $"Consider adding a GameDifficultyManager GameObject to your scene setup.");
                    }
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [Tooltip("The difficulty level the game will start with.")]
        [SerializeField] private DifficultyLevel initialDifficulty = DifficultyLevel.Normal;

        [Tooltip("List of all available DifficultyPreset ScriptableObjects. " +
                 "Ensure each DifficultyLevel has a corresponding preset here.")]
        [SerializeField] private List<DifficultyPreset> difficultyPresets = new List<DifficultyPreset>();

        // Private fields to store the current difficulty state.
        private DifficultyLevel _currentDifficultyLevel;
        private DifficultyPreset _currentPreset;

        // Dictionary for efficient lookup of presets by DifficultyLevel.
        private Dictionary<DifficultyLevel, DifficultyPreset> _presetMap;

        // C# Event: Invoked whenever the difficulty level changes.
        // Other game systems can subscribe to this event to react to difficulty changes in real-time.
        public static event Action<DifficultyLevel> OnDifficultyChanged;

        // ================================================================================================
        // Unity Lifecycle Methods
        // ================================================================================================

        private void Awake()
        {
            // Enforce Singleton pattern: ensure only one instance exists.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene loads.

            InitializePresets();
            // Set initial difficulty when the manager awakes.
            SetDifficulty(initialDifficulty, true);
        }

        // ================================================================================================
        // Private Initialization Methods
        // ================================================================================================

        // Populates the _presetMap dictionary for quick lookup.
        private void InitializePresets()
        {
            _presetMap = new Dictionary<DifficultyLevel, DifficultyPreset>();
            foreach (var preset in difficultyPresets)
            {
                if (preset == null)
                {
                    Debug.LogError("GameDifficultyManager: A null Difficulty Preset was found in the list. Please check your configuration.");
                    continue;
                }
                if (_presetMap.ContainsKey(preset.difficultyLevel))
                {
                    Debug.LogWarning($"GameDifficultyManager: Duplicate preset found for DifficultyLevel '{preset.difficultyLevel}'. " +
                                     $"The last one added will be used. Please ensure unique presets per level.");
                    _presetMap[preset.difficultyLevel] = preset; // Overwrite if duplicate
                }
                else
                {
                    _presetMap.Add(preset.difficultyLevel, preset);
                }
            }
        }

        // ================================================================================================
        // Public API for Difficulty Management
        // ================================================================================================

        /// <summary>
        /// Sets the current game difficulty to the specified level.
        /// Notifies all subscribers about the change.
        /// </summary>
        /// <param name="newLevel">The target DifficultyLevel.</param>
        /// <param name="forceUpdate">If true, the event will be fired even if the difficulty level hasn't actually changed.</param>
        public void SetDifficulty(DifficultyLevel newLevel, bool forceUpdate = false)
        {
            if (_currentDifficultyLevel == newLevel && !forceUpdate)
            {
                Debug.Log($"Difficulty is already set to {newLevel}. No change made.");
                return;
            }

            if (_presetMap == null || _presetMap.Count == 0)
            {
                Debug.LogError("GameDifficultyManager: Presets not initialized! Call InitializePresets() first.");
                InitializePresets(); // Attempt to initialize if somehow missed
                if (_presetMap.Count == 0)
                {
                    Debug.LogError("GameDifficultyManager: Failed to initialize presets. Cannot set difficulty.");
                    return;
                }
            }

            if (_presetMap.TryGetValue(newLevel, out DifficultyPreset foundPreset))
            {
                _currentDifficultyLevel = newLevel;
                _currentPreset = foundPreset;
                Debug.Log($"Game difficulty set to: {_currentDifficultyLevel} ({_currentPreset.difficultyName})");

                // Invoke the event, notifying all subscribed game elements.
                OnDifficultyChanged?.Invoke(_currentDifficultyLevel);
            }
            else
            {
                Debug.LogError($"GameDifficultyManager: No preset found for DifficultyLevel '{newLevel}'. " +
                               "Please ensure a corresponding DifficultyPreset ScriptableObject is assigned in the Inspector.");
            }
        }

        /// <summary>
        /// Returns the currently active DifficultyLevel.
        /// </summary>
        public DifficultyLevel GetCurrentDifficulty()
        {
            return _currentDifficultyLevel;
        }

        /// <summary>
        /// Returns the DifficultyPreset associated with the current difficulty level.
        /// </summary>
        public DifficultyPreset GetCurrentPreset()
        {
            if (_currentPreset == null)
            {
                Debug.LogError("GameDifficultyManager: Current preset is null. Ensure difficulty is set correctly.");
                SetDifficulty(initialDifficulty, true); // Attempt to reset if it's null
            }
            return _currentPreset;
        }

        /// <summary>
        /// Returns a specific DifficultyPreset for a given DifficultyLevel.
        /// Useful if you need to preview settings for a different difficulty without changing the current one.
        /// </summary>
        public DifficultyPreset GetDifficultyPreset(DifficultyLevel level)
        {
            if (_presetMap.TryGetValue(level, out DifficultyPreset preset))
            {
                return preset;
            }
            Debug.LogError($"GameDifficultyManager: No preset found for DifficultyLevel '{level}'. Returning current preset.");
            return GetCurrentPreset(); // Fallback to current preset
        }

        // ================================================================================================
        // Public API for Querying Difficulty-Adjusted Values (main practical use)
        // These methods abstract away the complexity of getting values from the current preset.
        // Game elements just ask "What's the enemy health multiplier?" not "Give me the current preset then its enemy health multiplier."
        // ================================================================================================

        /// <summary>
        /// Gets the enemy health multiplier for the current difficulty.
        /// </summary>
        public float GetEnemyHealthMultiplier() => GetCurrentPreset().enemyHealthMultiplier;

        /// <summary>
        /// Gets the enemy damage multiplier for the current difficulty.
        /// </summary>
        public float GetEnemyDamageMultiplier() => GetCurrentPreset().enemyDamageMultiplier;

        /// <summary>
        /// Gets the player damage multiplier for the current difficulty.
        /// </summary>
        public float GetPlayerDamageMultiplier() => GetCurrentPreset().playerDamageMultiplier;

        /// <summary>
        /// Gets the resource gain multiplier for the current difficulty.
        /// </summary>
        public float GetResourceGainMultiplier() => GetCurrentPreset().resourceGainMultiplier;

        /// <summary>
        /// Gets the special ability cooldown multiplier for the current difficulty.
        /// </summary>
        public float GetSpecialAbilityCooldownMultiplier() => GetCurrentPreset().specialAbilityCooldownMultiplier;

        /// <summary>
        /// Gets the base score award for the current difficulty.
        /// </summary>
        public int GetBaseScoreAward() => GetCurrentPreset().baseScoreAward;

        /// <summary>
        /// Gets the display name of the current difficulty level.
        /// </summary>
        public string GetCurrentDifficultyName() => GetCurrentPreset().difficultyName;

        // You can add more query methods here for any parameter you've added to DifficultyPreset.
    }
    // #endregion

    // #region 4. Example Game Elements (Demonstrates Usage)
    // ====================================================================================================
    // 4. Example Game Elements (Demonstrates Usage)
    // These are simple MonoBehaviour scripts that show how other parts of your game would
    // interact with the GameDifficultyManager.
    // They subscribe to difficulty changes and retrieve difficulty-adjusted values.
    // ====================================================================================================

    /// <summary>
    /// Base class for example game elements to handle difficulty subscription.
    /// </summary>
    public abstract class ExampleGameElement : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            // Subscribe to the difficulty changed event when this object is enabled.
            GameDifficultyManager.OnDifficultyChanged += OnDifficultyChanged;
            // Optionally, get initial settings immediately if manager is already awake.
            if (GameDifficultyManager.Instance != null && GameDifficultyManager.Instance.GetCurrentPreset() != null)
            {
                OnDifficultyChanged(GameDifficultyManager.Instance.GetCurrentDifficulty());
            }
        }

        protected virtual void OnDisable()
        {
            // Unsubscribe from the event when this object is disabled to prevent memory leaks.
            GameDifficultyManager.OnDifficultyChanged -= OnDifficultyChanged;
        }

        // This method will be called whenever the game difficulty changes.
        protected abstract void OnDifficultyChanged(DifficultyLevel newLevel);
    }

    /// <summary>
    /// Example Player Controller component that adapts to game difficulty.
    /// </summary>
    public class ExamplePlayerController : ExampleGameElement
    {
        [Header("Player Settings")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float baseSpecialCooldown = 5f;

        private float _currentHealth;
        private float _currentDamage;
        private float _currentSpecialCooldown;

        public float CurrentDamage => _currentDamage;
        public float CurrentHealth => _currentHealth;

        protected override void OnDifficultyChanged(DifficultyLevel newLevel)
        {
            // Get adjusted values from the GameDifficultyManager
            float playerDamageMultiplier = GameDifficultyManager.Instance.GetPlayerDamageMultiplier();
            float specialCooldownMultiplier = GameDifficultyManager.Instance.GetSpecialAbilityCooldownMultiplier();
            string difficultyName = GameDifficultyManager.Instance.GetCurrentDifficultyName();

            // Apply adjustments
            _currentDamage = baseDamage * playerDamageMultiplier;
            _currentSpecialCooldown = baseSpecialCooldown * specialCooldownMultiplier; // Note: Multiplier here typically means longer/shorter, not just higher.
                                                                                     // For cooldowns, a multiplier < 1 means faster (e.g., 0.8 = 20% faster).

            Debug.Log($"<color=cyan>Player:</color> Difficulty set to {difficultyName}. " +
                      $"Effective Damage: {_currentDamage:F1} (Base: {baseDamage} x {playerDamageMultiplier:F1}), " +
                      $"Special Cooldown: {_currentSpecialCooldown:F1}s (Base: {baseSpecialCooldown} x {specialCooldownMultiplier:F1}).");

            // Example: Adjust starting health based on difficulty, though it's not directly in the preset for simplicity
            // Could add a 'playerStartingHealthMultiplier' to DifficultyPreset if needed.
            // For now, let's just make it harder to represent:
            if (newLevel == DifficultyLevel.Easy)
            {
                _currentHealth = baseHealth * 1.2f; // More health on easy
            }
            else if (newLevel == DifficultyLevel.Hard || newLevel == DifficultyLevel.Expert)
            {
                _currentHealth = baseHealth * 0.8f; // Less health on hard
            }
            else
            {
                _currentHealth = baseHealth;
            }
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            Debug.Log($"<color=cyan>Player:</color> Took {amount:F1} damage. Remaining Health: {_currentHealth:F1}");
            if (_currentHealth <= 0)
            {
                Debug.Log("<color=red>Player DEFEATED!</color>");
                // Handle game over logic
            }
        }

        public void DealDamage(ExampleEnemyController targetEnemy)
        {
            if (targetEnemy != null)
            {
                Debug.Log($"<color=cyan>Player:</color> Attacking enemy for {_currentDamage:F1} damage.");
                targetEnemy.TakeDamage(_currentDamage);
            }
        }
    }

    /// <summary>
    /// Example Enemy Controller component that adapts to game difficulty.
    /// </summary>
    public class ExampleEnemyController : ExampleGameElement
    {
        [Header("Enemy Settings")]
        [SerializeField] private float baseHealth = 50f;
        [SerializeField] private float baseDamage = 5f;

        private float _currentHealth;
        private float _currentDamage;

        public float CurrentDamage => _currentDamage;

        protected override void OnDifficultyChanged(DifficultyLevel newLevel)
        {
            // Get adjusted values from the GameDifficultyManager
            float enemyHealthMultiplier = GameDifficultyManager.Instance.GetEnemyHealthMultiplier();
            float enemyDamageMultiplier = GameDifficultyManager.Instance.GetEnemyDamageMultiplier();
            string difficultyName = GameDifficultyManager.Instance.GetCurrentDifficultyName();

            // Apply adjustments
            _currentHealth = baseHealth * enemyHealthMultiplier;
            _currentDamage = baseDamage * enemyDamageMultiplier;

            Debug.Log($"<color=red>Enemy:</color> Difficulty set to {difficultyName}. " +
                      $"Effective Health: {_currentHealth:F1} (Base: {baseHealth} x {enemyHealthMultiplier:F1}), " +
                      $"Effective Damage: {_currentDamage:F1} (Base: {baseDamage} x {enemyDamageMultiplier:F1}).");
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            Debug.Log($"<color=red>Enemy:</color> Took {amount:F1} damage. Remaining Health: {_currentHealth:F1}");
            if (_currentHealth <= 0)
            {
                Debug.Log("<color=green>Enemy DEFEATED!</color>");
                Destroy(gameObject); // Example: Destroy enemy when health depleted
            }
        }

        public void AttackPlayer(ExamplePlayerController player)
        {
            if (player != null)
            {
                Debug.Log($"<color=red>Enemy:</color> Attacking player for {_currentDamage:F1} damage.");
                player.TakeDamage(_currentDamage);
            }
        }
    }


    /// <summary>
    /// Example UI component to display and change difficulty.
    /// This would typically use Unity UI elements (buttons, text).
    /// </summary>
    public class ExampleDifficultyUI : ExampleGameElement
    {
        [Header("UI References (Example - replace with actual Unity UI components)")]
        [SerializeField] private TMPro.TextMeshProUGUI difficultyText; // Requires TextMeshPro, add 'using TMPro;'
        [SerializeField] private UnityEngine.UI.Button easyButton;
        [SerializeField] private UnityEngine.UI.Button normalButton;
        [SerializeField] private UnityEngine.UI.Button hardButton;
        [SerializeField] private UnityEngine.UI.Button expertButton;


        private void Start()
        {
            // Attach button listeners (assuming you have actual UI Buttons in your scene)
            easyButton?.onClick.AddListener(() => SetDifficulty(DifficultyLevel.Easy));
            normalButton?.onClick.AddListener(() => SetDifficulty(DifficultyLevel.Normal));
            hardButton?.onClick.AddListener(() => SetDifficulty(DifficultyLevel.Hard));
            expertButton?.onClick.AddListener(() => SetDifficulty(DifficultyLevel.Expert));

            // Ensure initial UI state is correct
            if (GameDifficultyManager.Instance != null && GameDifficultyManager.Instance.GetCurrentPreset() != null)
            {
                UpdateDifficultyText(GameDifficultyManager.Instance.GetCurrentDifficultyName());
            }
        }

        protected override void OnDifficultyChanged(DifficultyLevel newLevel)
        {
            string difficultyName = GameDifficultyManager.Instance.GetCurrentDifficultyName();
            UpdateDifficultyText(difficultyName);
            Debug.Log($"<color=purple>UI:</color> Displaying current difficulty: {difficultyName}");
        }

        private void UpdateDifficultyText(string difficultyName)
        {
            if (difficultyText != null)
            {
                difficultyText.text = $"Current Difficulty: {difficultyName}";
            }
            else
            {
                Debug.LogWarning("ExampleDifficultyUI: difficultyText is not assigned. Cannot update UI text.");
            }
        }

        private void SetDifficulty(DifficultyLevel level)
        {
            GameDifficultyManager.Instance.SetDifficulty(level);
        }

        // Clean up listeners when the object is destroyed
        private void OnDestroy()
        {
            easyButton?.onClick.RemoveAllListeners();
            normalButton?.onClick.RemoveAllListeners();
            hardButton?.onClick.RemoveAllListeners();
            expertButton?.onClick.RemoveAllListeners();
        }
    }
    // #endregion
}
```