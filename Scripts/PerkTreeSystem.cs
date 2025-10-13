// Unity Design Pattern Example: PerkTreeSystem
// This script demonstrates the PerkTreeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Perk Tree System is a design pattern used in games to manage player progression through a tree-like structure of abilities or "perks." Players unlock perks by spending resources (e.g., perk points) and often have to meet prerequisites (e.g., unlock a previous perk in the branch).

This C# Unity example demonstrates the Perk Tree System using:
1.  **`PerkDefinition` (ScriptableObject):** To define the immutable data for each perk (ID, name, cost, prerequisites).
2.  **`PerkTreeManager` (MonoBehaviour):** The central component that manages the player's perk points and unlocked perks, handles unlock logic, and notifies other systems of changes.
3.  **`PlayerStats` (MonoBehaviour):** A simple example component representing player attributes that can be modified by perks.
4.  **`PerkEffectApplier` (MonoBehaviour):** A component that subscribes to the `PerkTreeManager`'s events and applies the actual effects of unlocked perks to the `PlayerStats`.

---

### **How to Use This Script in Unity:**

1.  **Create a C# Script:** Create a new C# script in your Unity project named `PerkTreeSystemExample`.
2.  **Copy and Paste:** Replace the entire content of the new script with the code provided below.
3.  **Create Manager GameObject:**
    *   Create an empty GameObject in your scene (e.g., named `GameManager`).
    *   Add the `PerkTreeManager` component to this `GameManager` GameObject.
    *   Add the `PlayerStats` component to this `GameManager` GameObject.
    *   Add the `PerkEffectApplier` component to this `GameManager` GameObject.
4.  **Create Perk Definitions (ScriptableObjects):**
    *   In your Project window, right-click -> Create -> Perk System -> Perk Definition.
    *   Create several of these, e.g., `Perk_Health1`, `Perk_Damage1`, `Perk_Health2`, `Perk_Damage2`, `Perk_Armor1`.
    *   **Configure each PerkDefinition:**
        *   Give it a unique `ID` (e.g., `health_boost_1`, `damage_boost_1`). This is crucial for internal logic.
        *   Set `Display Name`, `Description`, and `Cost`.
        *   For `Prerequisites`, drag and drop other `PerkDefinition` assets into the list. For example, `Perk_Health2` might have `Perk_Health1` as a prerequisite.
5.  **Assign References (Optional but Recommended):**
    *   Select your `GameManager` GameObject.
    *   In the `PerkEffectApplier` component, drag the `GameManager` GameObject itself into the `Perk Tree Manager` and `Player Stats` fields to assign their references. (It will auto-find them if not assigned, but explicit assignment is safer).
6.  **Run the Scene:** Observe the debug logs showing perk point changes, unlock attempts, and perk effects being applied.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // For custom events in the inspector

// This entire code block should be saved as a single C# script file, e.g., "PerkTreeSystemExample.cs"

/// <summary>
/// 1. PerkDefinition (ScriptableObject)
/// Represents a single perk within the perk tree.
/// This is a ScriptableObject, meaning it's a data asset that can be created
/// in the Unity Editor and reused across different perk trees or game instances.
/// It defines the *what* of a perk, not its state.
/// </summary>
[CreateAssetMenu(fileName = "NewPerk", menuName = "Perk System/Perk Definition", order = 1)]
public class PerkDefinition : ScriptableObject
{
    [Header("Perk Core Data")]
    [Tooltip("A unique identifier for this perk. Crucial for saving/loading and internal logic.")]
    public string id;
    [Tooltip("The name displayed to the player.")]
    public string displayName;
    [TextArea(3, 6)]
    [Tooltip("A detailed description of what the perk does.")]
    public string description;
    [Tooltip("The cost in perk points to unlock this perk.")]
    public int cost;

    [Header("Perk Tree Structure")]
    [Tooltip("A list of other perks that must be unlocked before this perk can be unlocked.")]
    public List<PerkDefinition> prerequisites;

    // You could also add visual elements here like an icon, sprite, or 3D model path
    // public Sprite icon;
}


/// <summary>
/// 2. PerkTreeManager (MonoBehaviour)
/// This is the central system that manages the player's perk points,
/// keeps track of unlocked perks, and handles the logic for unlocking new perks.
/// It uses a MonoBehaviour so it can exist in the scene and be easily accessed.
/// </summary>
public class PerkTreeManager : MonoBehaviour
{
    // Singleton pattern for easy access from anywhere.
    // In a larger project, consider a more robust service locator or dependency injection.
    public static PerkTreeManager Instance { get; private set; }

    [Header("Player Perk State")]
    [SerializeField]
    [Tooltip("The current number of perk points the player has.")]
    private int _currentPerkPoints = 0;
    public int CurrentPerkPoints => _currentPerkPoints; // Public read-only access

    [Tooltip("A list of IDs of all perks currently unlocked by the player.")]
    // Using HashSet for efficient O(1) lookups for `Contains` checks.
    private HashSet<string> _unlockedPerkIds = new HashSet<string>();
    // Expose as an IReadOnlyCollection to prevent external modification.
    public IReadOnlyCollection<string> UnlockedPerkIds => _unlockedPerkIds;

    [Header("Events")]
    [Tooltip("Event fired when a perk is successfully unlocked.")]
    public UnityEvent<PerkDefinition> OnPerkUnlocked;
    [Tooltip("Event fired when the player's perk points change.")]
    public UnityEvent OnPerkPointsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PerkTreeManager instances found! Destroying duplicate.", this);
            Destroy(this);
            return;
        }
        Instance = this;

        // Initialize events if they haven't been in the inspector (good practice)
        if (OnPerkUnlocked == null) OnPerkUnlocked = new UnityEvent<PerkDefinition>();
        if (OnPerkPointsChanged == null) OnPerkPointsChanged = new UnityEvent();

        Debug.Log("PerkTreeManager initialized.");
        Debug.Log($"Initial Perk Points: {_currentPerkPoints}");
    }

    /// <summary>
    /// Adds perk points to the player's pool.
    /// </summary>
    /// <param name="amount">The number of perk points to add. Must be positive.</param>
    public void AddPerkPoints(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Attempted to add non-positive perk points.", this);
            return;
        }

        _currentPerkPoints += amount;
        OnPerkPointsChanged?.Invoke(); // Notify listeners that points have changed
        Debug.Log($"Added {amount} perk points. Total: {_currentPerkPoints}", this);
    }

    /// <summary>
    /// Checks if a given perk can currently be unlocked by the player.
    /// </summary>
    /// <param name="perk">The PerkDefinition to check.</param>
    /// <returns>True if the perk can be unlocked, false otherwise.</returns>
    public bool CanUnlockPerk(PerkDefinition perk)
    {
        if (perk == null)
        {
            Debug.LogError("Attempted to check null perk definition.", this);
            return false;
        }

        // 1. Is it already unlocked?
        if (_unlockedPerkIds.Contains(perk.id))
        {
            // Debug.Log($"Perk '{perk.displayName}' (ID: {perk.id}) is already unlocked.", this);
            return false;
        }

        // 2. Does the player have enough perk points?
        if (_currentPerkPoints < perk.cost)
        {
            // Debug.Log($"Not enough perk points for '{perk.displayName}'. Needed: {perk.cost}, Have: {_currentPerkPoints}", this);
            return false;
        }

        // 3. Are all prerequisites met?
        foreach (var prereq in perk.prerequisites)
        {
            if (prereq == null)
            {
                Debug.LogError($"Perk '{perk.displayName}' has a null prerequisite. Please fix in editor!", this);
                continue; // Skip and check other prereqs, or you might want to return false immediately.
            }
            if (!_unlockedPerkIds.Contains(prereq.id))
            {
                // Debug.Log($"Prerequisite '{prereq.displayName}' not met for '{perk.displayName}'.", this);
                return false;
            }
        }

        return true; // All conditions met!
    }

    /// <summary>
    /// Attempts to unlock a perk. If successful, consumes perk points and adds the perk to the unlocked list.
    /// </summary>
    /// <param name="perk">The PerkDefinition to unlock.</param>
    /// <returns>True if the perk was successfully unlocked, false otherwise.</returns>
    public bool TryUnlockPerk(PerkDefinition perk)
    {
        if (perk == null)
        {
            Debug.LogError("Attempted to unlock a null perk definition.", this);
            return false;
        }

        if (CanUnlockPerk(perk))
        {
            _currentPerkPoints -= perk.cost;
            _unlockedPerkIds.Add(perk.id);

            OnPerkUnlocked?.Invoke(perk);       // Notify listeners that a perk was unlocked
            OnPerkPointsChanged?.Invoke();      // Notify listeners that points have changed

            Debug.Log($"Successfully unlocked perk: '{perk.displayName}' (ID: {perk.id}). Remaining points: {_currentPerkPoints}", this);
            return true;
        }
        else
        {
            Debug.Log($"Failed to unlock perk: '{perk.displayName}' (ID: {perk.id}). Check conditions.", this);
            return false;
        }
    }

    /// <summary>
    /// Checks if a specific perk is currently unlocked.
    /// </summary>
    /// <param name="perk">The PerkDefinition to check.</param>
    /// <returns>True if the perk is unlocked, false otherwise.</returns>
    public bool IsPerkUnlocked(PerkDefinition perk)
    {
        if (perk == null) return false;
        return _unlockedPerkIds.Contains(perk.id);
    }

    // --- Example Usage Methods (Can be called from UI buttons, game events, etc.) ---
    public void SimulateAddPerkPoints(int amount)
    {
        AddPerkPoints(amount);
    }

    // For demonstration, let's create some example perks to unlock via Inspector buttons.
    [Header("Test Perks (Drag PerkDefinitions here in Inspector)")]
    public PerkDefinition testPerk1;
    public PerkDefinition testPerk2;
    public PerkDefinition testPerk3;

    public void SimulateUnlockTestPerk1() { TryUnlockPerk(testPerk1); }
    public void SimulateUnlockTestPerk2() { TryUnlockPerk(testPerk2); }
    public void SimulateUnlockTestPerk3() { TryUnlockPerk(testPerk3); }


    // --- Persistence (Saving/Loading) ---
    // In a real game, you would need to save and load the _currentPerkPoints
    // and the _unlockedPerkIds.
    //
    // Example Save:
    // public PerkTreeSaveData GetSaveData()
    // {
    //     return new PerkTreeSaveData
    //     {
    //         perkPoints = _currentPerkPoints,
    //         unlockedPerkIds = new List<string>(_unlockedPerkIds)
    //     };
    // }
    //
    // Example Load:
    // public void LoadSaveData(PerkTreeSaveData data)
    // {
    //     _currentPerkPoints = data.perkPoints;
    //     _unlockedPerkIds = new HashSet<string>(data.unlockedPerkIds);
    //     OnPerkPointsChanged?.Invoke(); // Notify systems that state has changed
    //     foreach (string perkId in _unlockedPerkIds)
    //     {
    //         // You might want to re-apply effects for already unlocked perks on load
    //         // or ensure other systems are correctly initialized based on the loaded state.
    //     }
    // }
    //
    // [System.Serializable]
    // public class PerkTreeSaveData
    // {
    //     public int perkPoints;
    //     public List<string> unlockedPerkIds;
    // }
}


/// <summary>
/// 3. PlayerStats (MonoBehaviour - Example)
/// A dummy component representing player attributes that can be modified by perks.
/// In a real game, this would be a more complex system managing health, damage, etc.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Player Attributes")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float baseDamage = 10f;
    public float movementSpeed = 5f;
    public int bonusArmor = 0;

    // Multipliers for demonstration
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;

    public void Start()
    {
        ReportStats();
    }

    public void ApplyHealthBoost(float percentage)
    {
        healthMultiplier += percentage;
        maxHealth *= (1 + percentage); // Example: increase max health
        currentHealth = maxHealth; // Heal to new max health
        Debug.Log($"PlayerStats: Health Multiplier increased by {percentage * 100}%. New Max Health: {maxHealth}", this);
    }

    public void ApplyDamageBoost(float percentage)
    {
        damageMultiplier += percentage;
        Debug.Log($"PlayerStats: Damage Multiplier increased by {percentage * 100}%. Current total damage multiplier: {damageMultiplier}", this);
    }

    public void ApplyArmorBoost(int amount)
    {
        bonusArmor += amount;
        Debug.Log($"PlayerStats: Bonus Armor increased by {amount}. Current total bonus armor: {bonusArmor}", this);
    }

    public void ReportStats()
    {
        Debug.Log($"--- Player Current Stats ---", this);
        Debug.Log($"  Effective Max Health: {maxHealth * healthMultiplier:F2}", this);
        Debug.Log($"  Effective Damage: {baseDamage * damageMultiplier:F2}", this);
        Debug.Log($"  Bonus Armor: {bonusArmor}", this);
        Debug.Log($"----------------------------", this);
    }
}


/// <summary>
/// 4. PerkEffectApplier (MonoBehaviour - Example)
/// This component demonstrates how other parts of your game would react to perks being unlocked.
/// It subscribes to the PerkTreeManager's `OnPerkUnlocked` event and applies specific effects
/// to the `PlayerStats` based on the unlocked perk's ID.
/// </summary>
public class PerkEffectApplier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PerkTreeManager in the scene.")]
    public PerkTreeManager perkTreeManager;
    [Tooltip("Reference to the PlayerStats component that perks will modify.")]
    public PlayerStats playerStats;

    void Awake()
    {
        // Attempt to find references if not assigned in the Inspector
        if (perkTreeManager == null)
        {
            perkTreeManager = FindObjectOfType<PerkTreeManager>();
            if (perkTreeManager == null)
            {
                Debug.LogError("PerkEffectApplier: PerkTreeManager not found in scene!", this);
                enabled = false; // Disable this component if manager isn't found
                return;
            }
        }
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("PerkEffectApplier: PlayerStats not found in scene!", this);
                enabled = false; // Disable this component if player stats isn't found
                return;
            }
        }
    }

    void OnEnable()
    {
        // Subscribe to the event when this component is enabled
        if (perkTreeManager != null)
        {
            perkTreeManager.OnPerkUnlocked.AddListener(HandlePerkUnlocked);
            perkTreeManager.OnPerkPointsChanged.AddListener(HandlePerkPointsChanged);
            Debug.Log("PerkEffectApplier: Subscribed to PerkTreeManager events.", this);
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the event when this component is disabled or destroyed
        if (perkTreeManager != null)
        {
            perkTreeManager.OnPerkUnlocked.RemoveListener(HandlePerkUnlocked);
            perkTreeManager.OnPerkPointsChanged.RemoveListener(HandlePerkPointsChanged);
            Debug.Log("PerkEffectApplier: Unsubscribed from PerkTreeManager events.", this);
        }
    }

    /// <summary>
    /// Callback method executed when the PerkTreeManager's OnPerkUnlocked event fires.
    /// This is where the actual game effect of the perk is applied.
    /// </summary>
    /// <param name="perk">The PerkDefinition of the newly unlocked perk.</param>
    private void HandlePerkUnlocked(PerkDefinition perk)
    {
        Debug.Log($"PerkEffectApplier: Applying effects for perk '{perk.displayName}' (ID: {perk.id}).", this);

        // Use a switch statement or a dictionary/strategy pattern for more complex scenarios
        // to map perk IDs to specific actions.
        switch (perk.id)
        {
            case "health_boost_1":
                playerStats.ApplyHealthBoost(0.1f); // +10% Health Multiplier
                break;
            case "damage_boost_1":
                playerStats.ApplyDamageBoost(0.15f); // +15% Damage Multiplier
                break;
            case "health_boost_2":
                playerStats.ApplyHealthBoost(0.2f); // Another +20% Health Multiplier
                break;
            case "damage_boost_2":
                playerStats.ApplyDamageBoost(0.25f); // Another +25% Damage Multiplier
                break;
            case "armor_boost_1":
                playerStats.ApplyArmorBoost(5); // +5 Bonus Armor
                break;
            // Add more cases for other perk IDs
            default:
                Debug.LogWarning($"PerkEffectApplier: No specific effect defined for perk ID: {perk.id}.", this);
                break;
        }

        playerStats.ReportStats(); // Report current stats after applying perk effect
    }

    private void HandlePerkPointsChanged()
    {
        Debug.Log($"PerkEffectApplier: Perk points changed. Current: {perkTreeManager.CurrentPerkPoints}", this);
        // This could trigger UI updates for perk point display
    }
}
```