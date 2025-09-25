// Unity Design Pattern Example: SeasonPassSystem
// This script demonstrates the SeasonPassSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust and practical implementation of a 'Season Pass System' in Unity. While not a classical GoF (Gang of Four) design pattern, the "SeasonPassSystem" represents a common **System Architecture** in game development that incorporates several standard design patterns for modularity, extensibility, and maintainability.

**Key Design Patterns Utilized:**

1.  **Data-Driven Design (Scriptable Objects):** All season pass configuration (tiers, rewards, dates) is defined in Scriptable Objects, allowing designers to create and modify seasons without touching code.
2.  **Strategy Pattern:** `SeasonPassReward` acts as an abstract base class, and concrete reward types (e.g., `CurrencyReward`, `ItemReward`) implement their specific `Claim()` logic. This makes adding new reward types very easy.
3.  **Observer Pattern:** The `SeasonPassManager` uses C# events (`OnXPChanged`, `OnTierLeveledUp`, `OnRewardClaimed`, etc.) to notify interested UI elements or other systems about changes, promoting loose coupling.
4.  **Singleton Pattern (Optional, but common for Managers):** The `SeasonPassManager` is implemented as a static instance for easy global access, ensuring only one instance exists.
5.  **State Management:** The `PlayerSeasonProgress` class encapsulates the player's current state within a season, which is then persisted.

---

### **1. `SeasonPassRewards.cs`**
(Base class and concrete reward implementations)

This file defines the abstract base for any reward given by the season pass and provides concrete examples of common reward types.

```csharp
// SeasonPassRewards.cs
using UnityEngine;
using System; // For potentially more complex reward logic

/// <summary>
/// Base class for all season pass rewards.
/// This uses the **Strategy Pattern**: different reward types
/// implement a common interface (the abstract Claim() method) but encapsulate their specific logic.
/// This allows for easy extension with new reward types without modifying the core SeasonPassManager.
/// </summary>
public abstract class SeasonPassReward : ScriptableObject
{
    [Header("Reward General Info")]
    public string RewardName = "Unnamed Reward";
    public Sprite RewardIcon;
    [TextArea(3, 5)] public string Description = "A reward from the season pass.";

    /// <summary>
    /// The core method to be implemented by all concrete reward types.
    /// This is where the actual game logic for granting the reward happens.
    /// </summary>
    public abstract void Claim();
}

/// <summary>
/// Concrete implementation of a reward: Grants in-game currency.
/// </summary>
[CreateAssetMenu(fileName = "CurrencyReward", menuName = "Season Pass/Rewards/Currency Reward")]
public class CurrencyReward : SeasonPassReward
{
    [Header("Currency Reward Details")]
    public int Amount = 100;
    public string CurrencyType = "SoftCurrency"; // e.g., "Gold", "Gems", "Tickets"

    public override void Claim()
    {
        Debug.Log($"Claimed {Amount} {CurrencyType}. Granting to player...");
        // --- REAL WORLD USAGE ---
        // In a real game, you would interact with a PlayerInventory, EconomyManager,
        // or a central PlayerData system here.
        // Example: PlayerData.Instance.AddCurrency(CurrencyType, Amount);
        // Example: EventManager.Trigger(new CurrencyGainedEvent(CurrencyType, Amount));
        // For this example, we just log to the console.
        // --- END REAL WORLD USAGE ---
    }
}

/// <summary>
/// Concrete implementation of a reward: Grants an item.
/// </summary>
[CreateAssetMenu(fileName = "ItemReward", menuName = "Season Pass/Rewards/Item Reward")]
public class ItemReward : SeasonPassReward
{
    [Header("Item Reward Details")]
    public string ItemID = "ITEM_001"; // Or a direct reference to an Item ScriptableObject
    public int Quantity = 1;

    public override void Claim()
    {
        Debug.Log($"Claimed {Quantity} x {ItemID}. Adding to player inventory...");
        // --- REAL WORLD USAGE ---
        // Example: PlayerInventory.Instance.AddItem(ItemID, Quantity);
        // --- END REAL WORLD USAGE ---
    }
}

/// <summary>
/// Concrete implementation of a reward: Unlocks a cosmetic item.
/// </summary>
[CreateAssetMenu(fileName = "CosmeticReward", menuName = "Season Pass/Rewards/Cosmetic Reward")]
public class CosmeticReward : SeasonPassReward
{
    [Header("Cosmetic Reward Details")]
    public string CosmeticID = "SKIN_001"; // Or a direct reference to a Cosmetic ScriptableObject

    public override void Claim()
    {
        Debug.Log($"Claimed cosmetic item: {CosmeticID}. Unlocking for player...");
        // --- REAL WORLD USAGE ---
        // Example: PlayerCustomizationManager.Instance.UnlockCosmetic(CosmeticID);
        // --- END REAL WORLD USAGE ---
    }
}
```

---

### **2. `SeasonPassConfig.cs`**
(Tier and Season Pass Configuration)

This file defines the structure for a single tier within a season pass and the overall configuration for an entire season using Scriptable Objects. This is central to the **Data-Driven Design** aspect.

```csharp
// SeasonPassConfig.cs
using UnityEngine;
using System.Collections.Generic;
using System; // For DateTime

/// <summary>
/// Represents a single tier (level) within the season pass.
/// It defines the XP required to reach this tier and the rewards available for both
/// the free and premium tracks at this level.
/// </summary>
[Serializable] // Make it serializable so it can be embedded in ScriptableObjects
public class SeasonPassTier
{
    [Tooltip("The amount of XP required to reach this tier.")]
    public int RequiredXP;

    [Tooltip("List of rewards for players on the free track at this tier.")]
    public List<SeasonPassReward> FreeRewards = new List<SeasonPassReward>();

    [Tooltip("List of rewards for players on the premium track at this tier.")]
    public List<SeasonPassReward> PremiumRewards = new List<SeasonPassReward>();
}

/// <summary>
/// A ScriptableObject that defines the complete configuration for a single season pass.
/// This allows game designers to create and manage multiple seasons entirely from the Unity editor
/// without writing or changing any code. This is a core part of the **Data-Driven Design** pattern.
/// </summary>
[CreateAssetMenu(fileName = "SeasonPassConfig", menuName = "Season Pass/Season Pass Configuration")]
public class SeasonPassConfig : ScriptableObject
{
    [Header("Season General Details")]
    [Tooltip("A unique identifier for this season. Used for saving/loading player progress.")]
    public string SeasonID = "SEASON_001";
    public string DisplayName = "Season One: The Grand Adventure";
    [TextArea(3, 8)] public string Description = "Embark on an epic journey, conquer challenges, and earn exclusive rewards!";

    [Header("Season Duration")]
    [Tooltip("Start date of the season in YYYY-MM-DD format (e.g., 2023-01-01).")]
    public string StartDateString = "2023-01-01";
    [Tooltip("End date of the season in YYYY-MM-DD format (e.g., 2023-03-31).")]
    public string EndDateString = "2023-03-31";

    [Header("Premium Pass Details")]
    [Tooltip("The cost to unlock the premium track of this season pass.")]
    public int PremiumPassCost = 999; // Cost in a virtual currency (e.g., "Gems")

    [Header("Season Tiers")]
    [Tooltip("A list of all tiers in this season pass, ordered by Required XP.")]
    public List<SeasonPassTier> Tiers = new List<SeasonPassTier>();

    // --- Runtime Properties ---
    private DateTime _startDate;
    private DateTime _endDate;
    private bool _datesParsed = false;

    /// <summary>
    /// Gets the parsed start date of the season.
    /// </summary>
    public DateTime StartDate
    {
        get
        {
            if (!_datesParsed) ParseDates();
            return _startDate;
        }
    }

    /// <summary>
    /// Gets the parsed end date of the season.
    /// </summary>
    public DateTime EndDate
    {
        get
        {
            if (!_datesParsed) ParseDates();
            return _endDate;
        }
    }

    /// <summary>
    /// Parses the string dates into DateTime objects.
    /// </summary>
    private void ParseDates()
    {
        if (DateTime.TryParse(StartDateString, out DateTime start))
        {
            _startDate = start;
        }
        else
        {
            Debug.LogError($"Failed to parse StartDateString: '{StartDateString}' for SeasonID: '{SeasonID}'. Using MinValue.", this);
            _startDate = DateTime.MinValue;
        }

        if (DateTime.TryParse(EndDateString, out DateTime end))
        {
            // Add one day and subtract one tick to make the end date inclusive of the entire day
            _endDate = end.AddDays(1).AddTicks(-1);
        }
        else
        {
            Debug.LogError($"Failed to parse EndDateString: '{EndDateString}' for SeasonID: '{SeasonID}'. Using MaxValue.", this);
            _endDate = DateTime.MaxValue;
        }
        _datesParsed = true;
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the inspector.
    /// Useful for validating or pre-processing data.
    /// </summary>
    private void OnValidate()
    {
        _datesParsed = false; // Force re-parsing if dates are changed
        ParseDates();

        // Ensure XP is strictly increasing for tiers and rewards are assigned.
        for (int i = 0; i < Tiers.Count; i++)
        {
            // Tier 0 should usually start at 0 XP.
            if (i == 0 && Tiers[i].RequiredXP != 0)
            {
                Debug.LogWarning($"SeasonPassConfig '{name}': Tier 0 RequiredXP should typically be 0. Correcting.", this);
                Tiers[i].RequiredXP = 0;
            }
            // Ensure subsequent tiers have strictly higher XP requirements.
            else if (i > 0 && Tiers[i].RequiredXP <= Tiers[i - 1].RequiredXP)
            {
                Debug.LogWarning($"SeasonPassConfig '{name}': Tier {i} has RequiredXP ({Tiers[i].RequiredXP}) not greater than previous tier's XP ({Tiers[i-1].RequiredXP}). Adjusting automatically.", this);
                Tiers[i].RequiredXP = Tiers[i - 1].RequiredXP + 1; // Auto-correct for convenience
            }
        }
    }
}
```

---

### **3. `PlayerSeasonProgress.cs`**
(Player Progress Data Structure)

This file defines a serializable class that holds a player's individual progress for a specific season. This is crucial for persistence.

```csharp
// PlayerSeasonProgress.cs
using System;
using System.Collections.Generic;

/// <summary>
/// Serializable class to hold a player's progress for a specific season.
/// This structure is designed for easy JSON serialization/deserialization,
/// making it compatible with `JsonUtility.ToJson`/`FromJson`.
/// </summary>
[Serializable]
public class PlayerSeasonProgress
{
    public string SeasonID; // The ID of the season this progress belongs to
    public int CurrentXP;
    public int CurrentTierIndex; // Derived from CurrentXP, but cached for convenience and often used directly by UI
    public bool IsPremiumUnlocked;

    // A list of booleans for each tier, indicating if the free reward for that tier has been claimed.
    // The index of the list corresponds to the tier index (0-based).
    public List<bool> ClaimedFreeRewards;

    // Same as above, but for premium rewards.
    public List<bool> ClaimedPremiumRewards;

    /// <summary>
    /// Constructor for new player season progress.
    /// </summary>
    /// <param name="seasonID">The ID of the season.</param>
    /// <param name="totalTiers">The total number of tiers in this season pass.</param>
    public PlayerSeasonProgress(string seasonID, int totalTiers)
    {
        SeasonID = seasonID;
        CurrentXP = 0;
        CurrentTierIndex = 0;
        IsPremiumUnlocked = false;
        // Initialize lists with 'false' for all tiers, meaning no rewards are claimed yet.
        ClaimedFreeRewards = new List<bool>(new bool[totalTiers]);
        ClaimedPremiumRewards = new List<bool>(new bool[totalTiers]);
    }

    /// <summary>
    /// Ensures the claimed reward lists match the current season's tier count.
    /// This is important if a season config changes (e.g., new tiers added)
    /// after a player has already saved progress for that season.
    /// </summary>
    /// <param name="totalTiers">The current total number of tiers in the season.</param>
    public void InitializeClaimedRewardLists(int totalTiers)
    {
        // Add `false` entries if the season now has more tiers than previously saved progress.
        while (ClaimedFreeRewards.Count < totalTiers)
        {
            ClaimedFreeRewards.Add(false);
        }
        while (ClaimedPremiumRewards.Count < totalTiers)
        {
            ClaimedPremiumRewards.Add(false);
        }

        // --- OPTIONAL: Handle fewer tiers in new config ---
        // If tiers were removed from the season config, we might have extra `true`s
        // which might indicate rewards for tiers that no longer exist.
        // If strict trimming is needed (e.g., to prevent claiming non-existent rewards), uncomment:
        // if (ClaimedFreeRewards.Count > totalTiers) ClaimedFreeRewards.RemoveRange(totalTiers, ClaimedFreeRewards.Count - totalTiers);
        // if (ClaimedPremiumRewards.Count > totalTiers) ClaimedPremiumRewards.RemoveRange(totalTiers, ClaimedPremiumRewards.Count - totalTiers);
        // Typically, this isn't necessary as the SeasonPassManager will only access valid tier indices.
    }
}
```

---

### **4. `SeasonPassManager.cs`**
(The Core Season Pass System Manager)

This is the central hub for the season pass system. It's a MonoBehaviour that manages player progression, handles interactions with the season configuration, processes XP, and facilitates reward claiming. It uses the **Observer Pattern** for UI updates and a **Singleton-like** access pattern.

```csharp
// SeasonPassManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .FirstOrDefault()

/// <summary>
/// The central manager for handling the season pass system.
/// This MonoBehaviour acts as the main interface for other game systems and UI to interact
/// with the season pass. It incorporates:
/// - **Singleton Pattern (optional but common):** Provides a global access point.
/// - **Observer Pattern:** Uses events to notify subscribers (e.g., UI) of changes.
/// - **Data Persistence:** Manages saving and loading player progress.
/// </summary>
public class SeasonPassManager : MonoBehaviour
{
    // --- Singleton Pattern (Ensures a single instance exists globally) ---
    public static SeasonPassManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("The ScriptableObject defining the current season pass.")]
    public SeasonPassConfig CurrentSeasonConfig;
    [Tooltip("Base key used for PlayerPrefs or other save systems. SeasonID will be appended.")]
    public string SaveKeyBase = "SeasonPassProgress_";

    // --- Private Player Data (Managed internally) ---
    private PlayerSeasonProgress _playerProgress;
    private string _currentSeasonSaveKey; // Dynamic save key including season ID

    // --- Events (Observer Pattern: For UI and other systems to subscribe to) ---
    public event Action<int> OnXPChanged; // Invoked when player XP changes (value is current XP)
    public event Action<int> OnTierLeveledUp; // Invoked when player reaches a new tier (value is new tier index)
    public event Action<int, bool, SeasonPassReward> OnRewardClaimed; // tierIndex, isPremium, reward
    public event Action OnPremiumPassUnlocked; // Invoked when the premium pass is unlocked
    public event Action OnSeasonPassInitialized; // Invoked after the season pass manager is fully initialized
    public event Action<bool> OnSeasonActivityChanged; // Invoked when the season's active status changes

    // --- Public Properties (Read-only access to current state) ---
    public int CurrentXP => _playerProgress?.CurrentXP ?? 0;
    public int CurrentTierIndex => _playerProgress?.CurrentTierIndex ?? 0;
    public bool IsPremiumUnlocked => _playerProgress?.IsPremiumUnlocked ?? false;
    public int TotalTiers => CurrentSeasonConfig?.Tiers.Count ?? 0;
    public bool IsSeasonActive { get; private set; } = false; // Is the current season within its start/end dates?

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SeasonPassManager already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally persist across scenes if needed, remove if manager is scene-specific.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // For demonstration, initialize with a config directly.
        // In a real game, you might load this based on game state, current date,
        // or from a GameDataManager that manages multiple active seasons.
        if (CurrentSeasonConfig != null)
        {
            InitializeSeasonPass(CurrentSeasonConfig);
        }
        else
        {
            Debug.LogError("SeasonPassManager requires a 'CurrentSeasonConfig' to be assigned in the Inspector!", this);
        }
    }

    private void Update()
    {
        // Periodically check season activity.
        // In a production game, this might be optimized to only check once a day,
        // or on specific game events (e.g., game launch, menu open).
        CheckSeasonActivity();
    }

    /// <summary>
    /// Initializes the Season Pass system with a given configuration.
    /// This should be called once when the game starts, or when a new season becomes active.
    /// </summary>
    /// <param name="config">The SeasonPassConfig ScriptableObject for the current season.</param>
    public void InitializeSeasonPass(SeasonPassConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Cannot initialize Season Pass: Provided config is null!");
            return;
        }

        CurrentSeasonConfig = config;
        // Construct the unique save key for this specific season.
        _currentSeasonSaveKey = SaveKeyBase + CurrentSeasonConfig.SeasonID;

        LoadProgress(); // Attempt to load existing player progress for THIS season

        // If no progress found (or loaded progress was for a different season), create a new one.
        if (_playerProgress == null || _playerProgress.SeasonID != CurrentSeasonConfig.SeasonID)
        {
            Debug.Log($"No existing progress found or incompatible progress for Season ID: '{CurrentSeasonConfig.SeasonID}'. Creating new progress.");
            _playerProgress = new PlayerSeasonProgress(CurrentSeasonConfig.SeasonID, TotalTiers);
        }
        else
        {
            Debug.Log($"Loaded existing progress for Season ID: '{CurrentSeasonConfig.SeasonID}'.");
            // Ensure claimed reward lists are correctly sized for the current season's tier count
            _playerProgress.InitializeClaimedRewardLists(TotalTiers);
        }

        // Validate player's current tier based on their loaded/new XP
        UpdateCurrentTierFromXP();

        // Check if the season is currently active based on dates.
        CheckSeasonActivity();
        Debug.Log($"Season Pass '{CurrentSeasonConfig.DisplayName}' initialized. Active: {IsSeasonActive}");

        OnSeasonPassInitialized?.Invoke(); // Notify subscribers that initialization is complete
    }

    /// <summary>
    /// Checks if the current season is active based on its start and end dates.
    /// Triggers `OnSeasonActivityChanged` event if the status changes.
    /// </summary>
    private void CheckSeasonActivity()
    {
        if (CurrentSeasonConfig == null)
        {
            if (IsSeasonActive) // If it was active and now config is null, it became inactive
            {
                IsSeasonActive = false;
                OnSeasonActivityChanged?.Invoke(false);
            }
            return;
        }

        DateTime now = DateTime.UtcNow; // Using UTC for consistency across time zones
        bool newActivityStatus = now >= CurrentSeasonConfig.StartDate && now <= CurrentSeasonConfig.EndDate;

        if (newActivityStatus != IsSeasonActive)
        {
            IsSeasonActive = newActivityStatus;
            OnSeasonActivityChanged?.Invoke(IsSeasonActive);
            Debug.Log($"Season activity changed to: {IsSeasonActive}");
        }
    }


    /// <summary>
    /// Adds experience points to the player's current season pass progress.
    /// </summary>
    /// <param name="amount">The amount of XP to add. Must be positive.</param>
    public void AddXP(int amount)
    {
        if (!IsSeasonActive)
        {
            Debug.LogWarning("Cannot add XP: The current season is not active.");
            return;
        }
        if (_playerProgress == null || CurrentSeasonConfig == null)
        {
            Debug.LogError("Season Pass not initialized. Cannot add XP.");
            return;
        }
        if (amount <= 0)
        {
            Debug.LogWarning($"Attempted to add non-positive XP amount: {amount}. XP must be > 0.");
            return;
        }

        _playerProgress.CurrentXP += amount;
        Debug.Log($"XP added: {amount}. Current XP: {_playerProgress.CurrentXP}");

        OnXPChanged?.Invoke(_playerProgress.CurrentXP); // Notify XP change

        UpdateCurrentTierFromXP(); // Check if player leveled up
        SaveProgress(); // Persist changes
    }

    /// <summary>
    /// Updates the player's current tier based on their accumulated XP.
    /// Triggers `OnTierLeveledUp` event if the player progresses to a new tier.
    /// </summary>
    private void UpdateCurrentTierFromXP()
    {
        if (CurrentSeasonConfig == null || CurrentSeasonConfig.Tiers.Count == 0) return;

        int newTierIndex = _playerProgress.CurrentTierIndex;

        // Iterate through tiers from the current tier onwards to find the highest tier
        // the player's XP qualifies for. This is optimized as XP only increases.
        for (int i = _playerProgress.CurrentTierIndex; i < CurrentSeasonConfig.Tiers.Count; i++)
        {
            if (_playerProgress.CurrentXP >= CurrentSeasonConfig.Tiers[i].RequiredXP)
            {
                newTierIndex = i; // Player has reached this tier
            }
            else
            {
                // Since tiers are ordered by RequiredXP, we can stop early if current XP is insufficient.
                break;
            }
        }

        if (newTierIndex > _playerProgress.CurrentTierIndex)
        {
            Debug.Log($"Player leveled up from Tier {_playerProgress.CurrentTierIndex + 1} to Tier {newTierIndex + 1}!");
            _playerProgress.CurrentTierIndex = newTierIndex;
            OnTierLeveledUp?.Invoke(_playerProgress.CurrentTierIndex); // Notify tier change
        }
        // If XP could be lost, you might handle newTierIndex < _playerProgress.CurrentTierIndex here.
        // For standard season passes, XP only goes up.
    }

    /// <summary>
    /// Unlocks the premium track of the season pass.
    /// In a real game, this would involve a currency check and deduction.
    /// </summary>
    public void UnlockPremiumPass()
    {
        if (!IsSeasonActive)
        {
            Debug.LogWarning("Cannot unlock premium pass: The current season is not active.");
            return;
        }
        if (_playerProgress == null || CurrentSeasonConfig == null)
        {
            Debug.LogError("Season Pass not initialized. Cannot unlock premium pass.");
            return;
        }
        if (_playerProgress.IsPremiumUnlocked)
        {
            Debug.Log("Premium pass is already unlocked for this season.");
            return;
        }

        // --- MOCK CURRENCY CHECK & DEDUCTION (Replace with your actual economy system) ---
        Debug.Log($"Attempting to unlock premium pass for {CurrentSeasonConfig.PremiumPassCost} currency...");
        // Example: if (PlayerCurrencyManager.Instance.TryDeduct(CurrentSeasonConfig.PremiumPassCost)) { ... }
        // else { Debug.Log("Not enough currency!"); return; }
        // For this example, we assume success:
        // --- END MOCK ---

        _playerProgress.IsPremiumUnlocked = true;
        Debug.Log("Premium pass unlocked successfully!");
        OnPremiumPassUnlocked?.Invoke(); // Notify premium pass unlock
        SaveProgress(); // Persist changes
    }

    /// <summary>
    /// Checks if a specific reward at a given tier index can be claimed.
    /// </summary>
    /// <param name="tierIndex">The 0-based index of the tier.</param>
    /// <param name="isPremium">True for premium reward track, false for free reward track.</param>
    /// <returns>True if the reward can be claimed, false otherwise.</returns>
    public bool CanClaimReward(int tierIndex, bool isPremium)
    {
        if (!IsSeasonActive) return false;
        if (_playerProgress == null || CurrentSeasonConfig == null || tierIndex < 0 || tierIndex >= TotalTiers)
        {
            Debug.LogWarning($"Invalid state or tier index ({tierIndex}). Season not initialized or index out of bounds.");
            return false;
        }

        // Player must have reached or passed this tier.
        if (tierIndex > CurrentTierIndex)
        {
            return false;
        }

        // If claiming a premium reward, the player must have unlocked the premium pass.
        if (isPremium && !_playerProgress.IsPremiumUnlocked)
        {
            return false;
        }

        // Get the specific tier configuration.
        SeasonPassTier tier = CurrentSeasonConfig.Tiers[tierIndex];

        // Check if there's actually a reward defined for this track at this tier.
        // And check if it's already claimed.
        if (isPremium)
        {
            return tier.PremiumRewards.Any() && !_playerProgress.ClaimedPremiumRewards[tierIndex];
        }
        else
        {
            return tier.FreeRewards.Any() && !_playerProgress.ClaimedFreeRewards[tierIndex];
        }
    }

    /// <summary>
    /// Claims a specific reward at a given tier index.
    /// This method performs the actual reward granting logic via the `SeasonPassReward.Claim()` method
    /// (an example of the **Strategy Pattern** in action).
    /// </summary>
    /// <param name="tierIndex">The 0-based index of the tier.</param>
    /// <param name="isPremium">True for premium reward track, false for free reward track.</param>
    /// <returns>True if the reward was successfully claimed, false otherwise.</returns>
    public bool ClaimReward(int tierIndex, bool isPremium)
    {
        if (!CanClaimReward(tierIndex, isPremium))
        {
            Debug.LogWarning($"Cannot claim {(isPremium ? "premium" : "free")} reward at tier {tierIndex + 1}. Conditions not met.");
            return false;
        }

        SeasonPassReward rewardToClaim = null;
        if (isPremium)
        {
            // Assuming for simplicity that each tier track has one primary reward in the list.
            // If multiple rewards per tier/track are allowed, you would iterate and claim all.
            rewardToClaim = CurrentSeasonConfig.Tiers[tierIndex].PremiumRewards.FirstOrDefault();
            if (rewardToClaim != null)
            {
                _playerProgress.ClaimedPremiumRewards[tierIndex] = true;
            }
        }
        else
        {
            rewardToClaim = CurrentSeasonConfig.Tiers[tierIndex].FreeRewards.FirstOrDefault();
            if (rewardToClaim != null)
            {
                _playerProgress.ClaimedFreeRewards[tierIndex] = true;
            }
        }

        if (rewardToClaim != null)
        {
            rewardToClaim.Claim(); // Execute the specific reward logic (Strategy Pattern)
            Debug.Log($"Successfully claimed {(isPremium ? "premium" : "free")} reward '{rewardToClaim.RewardName}' at Tier {tierIndex + 1}.");
            OnRewardClaimed?.Invoke(tierIndex, isPremium, rewardToClaim); // Notify reward claimed
            SaveProgress(); // Persist changes
            return true;
        }
        else
        {
            Debug.LogError($"No {(isPremium ? "premium" : "free")} reward found at tier {tierIndex + 1} to claim in the config!");
            return false;
        }
    }

    /// <summary>
    /// Gets the current claim status for a reward at a specific tier.
    /// </summary>
    /// <param name="tierIndex">The 0-based index of the tier.</param>
    /// <param name="isPremium">True for premium track, false for free track.</param>
    /// <returns>True if claimed, false if not.</returns>
    public bool GetRewardClaimStatus(int tierIndex, bool isPremium)
    {
        if (_playerProgress == null || tierIndex < 0 || tierIndex >= TotalTiers) return false;

        if (isPremium)
        {
            return _playerProgress.ClaimedPremiumRewards[tierIndex];
        }
        else
        {
            return _playerProgress.ClaimedFreeRewards[tierIndex];
        }
    }

    /// <summary>
    /// Retrieves the SeasonPassTier configuration for a given index.
    /// </summary>
    /// <param name="tierIndex">The 0-based index of the tier.</param>
    /// <returns>The SeasonPassTier object or null if invalid index.</returns>
    public SeasonPassTier GetTierConfig(int tierIndex)
    {
        if (CurrentSeasonConfig != null && tierIndex >= 0 && tierIndex < TotalTiers)
        {
            return CurrentSeasonConfig.Tiers[tierIndex];
        }
        return null;
    }

    // --- Persistence (Using PlayerPrefs for simplicity in this example) ---
    // In a real game, this would typically integrate with a more robust save system (e.g., file I/O, cloud saves).
    private void SaveProgress()
    {
        if (_playerProgress == null || CurrentSeasonConfig == null) return;
        string json = JsonUtility.ToJson(_playerProgress);
        PlayerPrefs.SetString(_currentSeasonSaveKey, json);
        PlayerPrefs.Save(); // Ensures data is written to disk immediately
        Debug.Log($"Season Pass progress for '{CurrentSeasonConfig.SeasonID}' saved.");
    }

    private void LoadProgress()
    {
        if (CurrentSeasonConfig == null)
        {
            _playerProgress = null;
            return;
        }

        if (PlayerPrefs.HasKey(_currentSeasonSaveKey))
        {
            string json = PlayerPrefs.GetString(_currentSeasonSaveKey);
            _playerProgress = JsonUtility.FromJson<PlayerSeasonProgress>(json);

            // Important: After loading, ensure the loaded progress matches the CURRENT season's ID.
            // If it's a new season (or the config was swapped), we treat it as no progress for the current season.
            if (_playerProgress.SeasonID != CurrentSeasonConfig.SeasonID)
            {
                Debug.LogWarning($"Loaded progress belongs to a different season ('{_playerProgress.SeasonID}') than current ('{CurrentSeasonConfig.SeasonID}'). Discarding old progress for this context.");
                _playerProgress = null; // Discard incompatible progress
                return;
            }

            // Ensure claimed reward lists are correctly sized for the current season's tiers.
            _playerProgress.InitializeClaimedRewardLists(TotalTiers);

            Debug.Log($"Season Pass progress for '{CurrentSeasonConfig.SeasonID}' loaded.");
        }
        else
        {
            Debug.Log($"No saved progress found for Season ID: '{CurrentSeasonConfig.SeasonID}'.");
            _playerProgress = null; // No progress exists, will be created on initialization.
        }
    }

    /// <summary>
    /// Clears the saved progress for the current season. Useful for debugging or season resets.
    /// </summary>
    public void ClearProgress()
    {
        if (CurrentSeasonConfig == null) return;

        if (PlayerPrefs.HasKey(_currentSeasonSaveKey))
        {
            PlayerPrefs.DeleteKey(_currentSeasonSaveKey);
            PlayerPrefs.Save();
            Debug.Log($"Season Pass progress for '{CurrentSeasonConfig.SeasonID}' cleared.");
        }

        // Re-initialize player progress after clearing
        _playerProgress = new PlayerSeasonProgress(CurrentSeasonConfig.SeasonID, TotalTiers);
        UpdateCurrentTierFromXP(); // Reset tier
        OnXPChanged?.Invoke(_playerProgress.CurrentXP); // Notify UI
        OnTierLeveledUp?.Invoke(_playerProgress.CurrentTierIndex); // Notify UI
        OnPremiumPassUnlocked?.Invoke(); // Notify UI (will show as locked again)
    }
}
```

---

### **5. `SeasonPassTestUI.cs`**
(Example UI Integration and Test Script)

This script demonstrates how a UI or any other game system would interact with the `SeasonPassManager`. It subscribes to events for updates and provides methods to trigger actions.

```csharp
// SeasonPassTestUI.cs
using UnityEngine;
using UnityEngine.UI; // For UI elements
using System.Text; // For StringBuilder for log display

/// <summary>
/// A simple MonoBehaviour to demonstrate interaction with the SeasonPassManager.
/// This script simulates a basic UI for a season pass, showing how to:
/// - Subscribe to `SeasonPassManager` events (Observer Pattern).
/// - Call manager methods (AddXP, UnlockPremiumPass, ClaimReward).
/// - Update UI elements based on the season pass state.
/// </summary>
public class SeasonPassTestUI : MonoBehaviour
{
    [Header("UI References")]
    public Text currentXPText;
    public Text currentTierText;
    public Text premiumStatusText;
    public Text seasonStatusText;
    public Button addXPButton;
    public Button unlockPremiumButton;
    public Button claimRewardButtonFree;
    public Button claimRewardButtonPremium;
    public Button clearProgressButton; // For debugging/testing
    public Text debugMessageText; // For displaying runtime logs in the UI

    private StringBuilder debugLog = new StringBuilder();
    private int _logLineCount = 0;
    private const int MAX_LOG_LINES = 10; // Limit log output to keep UI clean

    void Start()
    {
        // Ensure the SeasonPassManager exists and is initialized.
        if (SeasonPassManager.Instance == null)
        {
            LogMessage("SeasonPassManager not found. Please ensure it's in the scene and initialized.");
            SetAllUIInteractable(false);
            return;
        }

        // --- Subscribe to SeasonPassManager Events (Observer Pattern) ---
        SeasonPassManager.Instance.OnXPChanged += UpdateUI;
        SeasonPassManager.Instance.OnTierLeveledUp += (tier) => {
            UpdateUI(SeasonPassManager.Instance.CurrentXP);
            LogMessage($"Player Leveled Up to Tier {tier + 1}!");
        };
        SeasonPassManager.Instance.OnPremiumPassUnlocked += () => {
            UpdateUI(SeasonPassManager.Instance.CurrentXP);
            LogMessage("Premium Pass Unlocked!");
        };
        SeasonPassManager.Instance.OnRewardClaimed += (tier, isPremium, reward) => {
            UpdateUI(SeasonPassManager.Instance.CurrentXP);
            LogMessage($"Claimed {(isPremium ? "Premium" : "Free")} Reward '{reward.RewardName}' at Tier {tier + 1}.");
        };
        SeasonPassManager.Instance.OnSeasonPassInitialized += () => {
            UpdateUI(SeasonPassManager.Instance.CurrentXP);
            LogMessage($"Season Pass '{SeasonPassManager.Instance.CurrentSeasonConfig.DisplayName}' Initialized.");
        };
        SeasonPassManager.Instance.OnSeasonActivityChanged += (isActive) => {
            LogMessage($"Season Activity: {isActive}");
            UpdateUI(SeasonPassManager.Instance.CurrentXP);
        };


        // --- Assign UI Button Actions ---
        addXPButton?.onClick.AddListener(() => SeasonPassManager.Instance.AddXP(50));
        unlockPremiumButton?.onClick.AddListener(SeasonPassManager.Instance.UnlockPremiumPass);
        claimRewardButtonFree?.onClick.AddListener(() => TryClaimReward(SeasonPassManager.Instance.CurrentTierIndex, false));
        claimRewardButtonPremium?.onClick.AddListener(() => TryClaimReward(SeasonPassManager.Instance.CurrentTierIndex, true));
        clearProgressButton?.onClick.AddListener(() => { SeasonPassManager.Instance.ClearProgress(); UpdateUI(0); LogMessage("Season Pass Progress Cleared!"); });


        // Initial UI update to reflect current state
        UpdateUI(SeasonPassManager.Instance.CurrentXP);
        LogMessage("SeasonPassTestUI started and subscribed to events.");
    }

    void OnDestroy()
    {
        // --- Unsubscribe from events to prevent memory leaks ---
        if (SeasonPassManager.Instance != null)
        {
            SeasonPassManager.Instance.OnXPChanged -= UpdateUI;
            SeasonPassManager.Instance.OnTierLeveledUp -= (tier) => { UpdateUI(SeasonPassManager.Instance.CurrentXP); };
            SeasonPassManager.Instance.OnPremiumPassUnlocked -= () => { UpdateUI(SeasonPassManager.Instance.CurrentXP); };
            SeasonPassManager.Instance.OnRewardClaimed -= (tier, isPremium, reward) => { UpdateUI(SeasonPassManager.Instance.CurrentXP); };
            SeasonPassManager.Instance.OnSeasonPassInitialized -= () => { UpdateUI(SeasonPassManager.Instance.CurrentXP); };
            SeasonPassManager.Instance.OnSeasonActivityChanged -= (isActive) => { UpdateUI(SeasonPassManager.Instance.CurrentXP); };
        }

        // --- Remove all button listeners ---
        addXPButton?.onClick.RemoveAllListeners();
        unlockPremiumButton?.onClick.RemoveAllListeners();
        claimRewardButtonFree?.onClick.RemoveAllListeners();
        claimRewardButtonPremium?.onClick.RemoveAllListeners();
        clearProgressButton?.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Updates all relevant UI text fields and button interactability based on the current season pass state.
    /// This method is typically called by the `SeasonPassManager` events.
    /// </summary>
    /// <param name="currentXP">The player's current XP (passed by OnXPChanged event).</param>
    void UpdateUI(int currentXP)
    {
        // Handle cases where manager or config might not be ready
        if (SeasonPassManager.Instance == null || SeasonPassManager.Instance.CurrentSeasonConfig == null)
        {
            if (currentXPText != null) currentXPText.text = "XP: N/A";
            if (currentTierText != null) currentTierText.text = "Tier: N/A";
            if (premiumStatusText != null) premiumStatusText.text = "Premium: N/A";
            if (seasonStatusText != null) seasonStatusText.text = "Season: Not Initialized / Inactive";
            SetAllUIInteractable(false);
            return;
        }

        SeasonPassManager manager = SeasonPassManager.Instance;
        SeasonPassConfig config = manager.CurrentSeasonConfig;

        // --- Update Text Fields ---
        if (currentXPText != null)
        {
            int nextTierXP = manager.CurrentTierIndex < manager.TotalTiers - 1
                ? config.Tiers[manager.CurrentTierIndex + 1].RequiredXP
                : manager.CurrentXP; // If max tier, show current XP as max.
            currentXPText.text = $"XP: {manager.CurrentXP} / {nextTierXP} (to next tier)";
        }
        if (currentTierText != null)
        {
            currentTierText.text = $"Tier: {manager.CurrentTierIndex + 1} / {manager.TotalTiers}";
        }
        if (premiumStatusText != null)
        {
            premiumStatusText.text = manager.IsPremiumUnlocked ? "Premium: Unlocked" : $"Premium: Locked (Cost: {config.PremiumPassCost})";
        }
        if (seasonStatusText != null)
        {
            seasonStatusText.text = manager.IsSeasonActive ? "Season: Active" : "Season: Inactive";
        }

        // --- Update Button Interactability ---
        bool canInteractWithPass = manager.IsSeasonActive;
        SetAllUIInteractable(canInteractWithPass); // Enable/disable based on season activity

        if (unlockPremiumButton != null)
        {
            unlockPremiumButton.interactable = canInteractWithPass && !manager.IsPremiumUnlocked;
        }

        if (claimRewardButtonFree != null)
        {
            // Can claim free reward if season is active, player reached tier, and reward not claimed
            claimRewardButtonFree.interactable = canInteractWithPass && manager.CanClaimReward(manager.CurrentTierIndex, false);
            Text buttonText = claimRewardButtonFree.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                // Show current tier + 1 for user-friendly display (0-based vs 1-based)
                buttonText.text = $"Claim Free Reward (Tier {manager.CurrentTierIndex + 1})";
            }
        }
        if (claimRewardButtonPremium != null)
        {
            // Can claim premium reward if season is active, player reached tier, premium unlocked, and reward not claimed
            claimRewardButtonPremium.interactable = canInteractWithPass && manager.CanClaimReward(manager.CurrentTierIndex, true);
            Text buttonText = claimRewardButtonPremium.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Claim Premium Reward (Tier {manager.CurrentTierIndex + 1})";
            }
        }
    }

    /// <summary>
    /// Helper to set interactability of core buttons.
    /// </summary>
    /// <param name="interactable">True to enable, false to disable.</param>
    void SetAllUIInteractable(bool interactable)
    {
        if (addXPButton != null) addXPButton.interactable = interactable;
        // Other buttons like unlockPremiumButton and claimReward buttons have specific logic in UpdateUI
    }

    /// <summary>
    /// Attempts to claim a reward for the current tier.
    /// </summary>
    /// <param name="tierIndex">The 0-based index of the tier to claim from.</param>
    /// <param name="isPremium">True for premium track, false for free track.</param>
    void TryClaimReward(int tierIndex, bool isPremium)
    {
        if (SeasonPassManager.Instance.ClaimReward(tierIndex, isPremium))
        {
            // The UpdateUI method will be automatically called by the OnRewardClaimed event.
        }
        else
        {
            LogMessage($"Failed to claim {(isPremium ? "Premium" : "Free")} Reward for Tier {tierIndex + 1}. Check conditions.");
        }
    }

    /// <summary>
    /// Adds a message to the UI debug log and to Unity's console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogMessage(string message)
    {
        if (debugMessageText != null)
        {
            // Prepend new messages and trim oldest ones if log exceeds max lines
            if (_logLineCount >= MAX_LOG_LINES)
            {
                int firstNewline = debugLog.ToString().IndexOf('\n');
                if (firstNewline != -1)
                {
                    debugLog.Remove(0, firstNewline + 1);
                    _logLineCount--;
                }
            }
            debugLog.AppendLine(message);
            _logLineCount++;
            debugMessageText.text = debugLog.ToString();
            Debug.Log($"[SeasonPassTestUI] {message}");
        }
        else
        {
            Debug.LogWarning($"[SeasonPassTestUI] Debug Message Text not assigned. Message: {message}");
        }
    }
}

/*
    === HOW TO SET UP THIS EXAMPLE IN UNITY ===

    1.  **Create C# Scripts:**
        -   Create a new C# Script named `SeasonPassRewards.cs` and paste the code from section 1.
        -   Create a new C# Script named `SeasonPassConfig.cs` and paste the code from section 2.
        -   Create a new C# Script named `PlayerSeasonProgress.cs` and paste the code from section 3.
        -   Create a new C# Script named `SeasonPassManager.cs` and paste the code from section 4.
        -   Create a new C# Script named `SeasonPassTestUI.cs` and paste the code from section 5.

    2.  **Create a Season Pass Configuration (ScriptableObject):**
        -   In the Unity Project window, right-click -> Create -> Season Pass -> Season Pass Configuration.
        -   Name it `MySeasonPassConfig`.
        -   Select `MySeasonPassConfig` in the Project window and populate its fields in the Inspector:
            -   `Season ID`: `SEASON_LAUNCH`
            -   `Display Name`: `Launch Season`
            -   `Start Date String`: e.g., `2023-01-01`
            -   `End Date String`: e.g., `2024-12-31` (Make sure it's active for today's date for testing)
            -   `Premium Pass Cost`: `1000` (Arbitrary currency value)
            -   **Tiers:** Expand the `Tiers` list. Add a few elements:
                -   **Element 0 (Tier 1):** `Required XP = 0`
                    -   `Free Rewards`: Add 1 item. Right-click in Project -> Create -> Season Pass -> Rewards -> Currency Reward. Set `Amount = 100`, `Currency Type = Gold`. Drag this new CurrencyReward SO into the `Free Rewards` slot.
                    -   `Premium Rewards`: Add 1 item. Right-click in Project -> Create -> Season Pass -> Rewards -> Item Reward. Set `ItemID = StarterChest`, `Quantity = 1`. Drag this into the `Premium Rewards` slot.
                -   **Element 1 (Tier 2):** `Required XP = 100`
                    -   `Free Rewards`: Add 1 item. Create a new `CurrencyReward`, set `Amount = 200`, `Currency Type = Gold`.
                    -   `Premium Rewards`: Add 1 item. Create a new `CosmeticReward`, set `CosmeticID = UniqueHat`.
                -   **Element 2 (Tier 3):** `Required XP = 250`
                    -   ...and so on. You can create different reward types for each tier.

    3.  **Setup the `SeasonPassManager` GameObject:**
        -   In your Unity Scene Hierarchy, right-click -> Create Empty. Name it `SeasonPassManager`.
        -   Drag the `SeasonPassManager.cs` script onto this new GameObject.
        -   In the Inspector for `SeasonPassManager`, drag your `MySeasonPassConfig` ScriptableObject from the Project window into the `Current Season Config` field.

    4.  **Setup the UI (`Canvas` and `SeasonPassTestUI` GameObject):**
        -   In your Scene Hierarchy, right-click -> UI -> Canvas. This will create a Canvas and an EventSystem.
        -   Inside the Canvas, create the following UI elements (Right-click on Canvas -> UI -> ...):
            -   `Text` for `Current XP`: Name it `XP_Text`.
            -   `Text` for `Current Tier`: Name it `Tier_Text`.
            -   `Text` for `Premium Status`: Name it `Premium_Status_Text`.
            -   `Text` for `Season Status`: Name it `Season_Status_Text`.
            -   `Button` for `Add XP`: Name it `AddXP_Button`. Set its text to "Add 50 XP".
            -   `Button` for `Unlock Premium`: Name it `UnlockPremium_Button`. Set its text to "Unlock Premium Pass".
            -   `Button` for `Claim Free Reward`: Name it `ClaimFree_Button`. Set its text to "Claim Free Reward".
            -   `Button` for `Claim Premium Reward`: Name it `ClaimPremium_Button`. Set its text to "Claim Premium Reward".
            -   `Button` for `Clear Progress`: Name it `ClearProgress_Button`. Set its text to "Clear Progress".
            -   `Text` for `Debug Messages`: Name it `Debug_Log_Text`. Adjust its Rect Transform to be multi-line.
        -   Create another Empty GameObject in the scene: Name it `SeasonPassTestUI`.
        -   Drag the `SeasonPassTestUI.cs` script onto this new GameObject.
        -   In the Inspector for `SeasonPassTestUI`, drag the UI elements you just created (from the Canvas) into their corresponding public fields (e.g., `XP_Text` to `Current XP Text`).

    5.  **Run the Scene:**
        -   Play your Unity scene.
        -   Observe the UI elements updating.
        -   Interact with the buttons:
            -   Click "Add 50 XP" multiple times. Watch XP and Tier update.
            -   Click "Unlock Premium Pass".
            -   Click "Claim Free Reward" and "Claim Premium Reward" for your current tier.
            -   Watch the debug log for messages.
            -   Click "Clear Progress" to reset the season pass progress for the current season.

    This setup provides a fully functional and understandable example of a 'SeasonPassSystem' in Unity, ready for further expansion and integration into your game.
*/
```