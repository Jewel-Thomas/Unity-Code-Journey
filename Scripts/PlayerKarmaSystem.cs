// Unity Design Pattern Example: PlayerKarmaSystem
// This script demonstrates the PlayerKarmaSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'PlayerKarmaSystem' pattern in Unity. This system centralizes the management of a player's moral alignment (karma), allowing other game systems to query the player's karma state and react to changes via events.

**Key Design Patterns Used:**

1.  **Singleton:** Ensures there is only one instance of the `PlayerKarmaSystem` throughout the game, providing a global access point.
2.  **Observer (Event/Delegate):** Other scripts can "subscribe" to `OnKarmaChanged` and `OnKarmaTierChanged` events. This decouples the KarmaSystem from scripts that react to karma changes, making the system more modular and easier to extend.
3.  **State Machine (Implicit):** The `KarmaTier` system acts as an implicit state machine, where the player's "karma state" transitions between different tiers based on their `currentKarma` value.

---

### **1. `PlayerKarmaSystem.cs` (The Core System Script)**

This script should be attached to an empty GameObject in your first scene (e.g., named "GameManager" or "KarmaSystem").

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ's OrderBy method

/// <summary>
/// Represents a specific tier or level of karma, defined by a lower boundary.
/// This struct allows easy configuration of karma tiers directly in the Unity Inspector.
/// </summary>
[System.Serializable] // Makes this struct editable in the Unity Inspector
public struct KarmaTier : IEquatable<KarmaTier>
{
    public string tierName; // E.g., "Saint", "Neutral", "Evil"
    [Tooltip("The minimum karma value required to be in this tier. Tiers should be sorted by this threshold.")]
    public float threshold; 
    public Color displayColor; // Optional: Color for UI representation of this tier

    public KarmaTier(string name, float thresh, Color color)
    {
        tierName = name;
        threshold = thresh;
        displayColor = color;
    }

    // Implement IEquatable for proper comparison of KarmaTier structs
    public bool Equals(KarmaTier other)
    {
        return tierName == other.tierName &&
               Mathf.Approximately(threshold, other.threshold) && // Use Approximately for float comparison
               displayColor == other.displayColor;
    }

    public override bool Equals(object obj)
    {
        return obj is KarmaTier other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Simple hash code for a struct
        return HashCode.Combine(tierName, threshold, displayColor);
    }
}

/// <summary>
/// The central Player Karma System, implementing the Singleton pattern.
/// This system tracks the player's moral standing (karma) and notifies other systems of changes.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g., "GameManager").
/// 2. Attach this `PlayerKarmaSystem.cs` script to that GameObject.
/// 3. Configure `Min Karma`, `Max Karma`, and `Karma Tiers` in the Inspector.
///    Ensure `Karma Tiers` are logically ordered by their `threshold` (lowest to highest).
/// 4. Other scripts can access the system via `PlayerKarmaSystem.Instance`.
/// 5. Other scripts can subscribe to `PlayerKarmaSystem.OnKarmaChanged` and
///    `PlayerKarmaSystem.OnKarmaTierChanged` to react to karma updates.
/// </summary>
public class PlayerKarmaSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Provides a global point of access to the single instance of the Karma System.
    public static PlayerKarmaSystem Instance { get; private set; }

    [Header("Karma Settings")]
    [Tooltip("The player's current karma value. Can be set in Inspector for initial testing.")]
    [SerializeField]
    private float currentKarma = 0f;

    [Tooltip("The minimum possible karma value. Karma cannot go below this.")]
    [SerializeField]
    private float minKarma = -100f;

    [Tooltip("The maximum possible karma value. Karma cannot go above this.")]
    [SerializeField]
    private float maxKarma = 100f;

    [Tooltip("Defines the different karma tiers (e.g., Good, Neutral, Evil). " +
             "Ensure tiers are sorted by threshold in ascending order for correct functionality." +
             "The system will automatically sort them on Awake and OnValidate.")]
    [SerializeField]
    private List<KarmaTier> karmaTiers = new List<KarmaTier>()
    {
        // Default example tiers - customize as needed
        new KarmaTier("Evil", -75f, new Color(0.8f, 0.2f, 0.2f)),    // Dark Red
        new KarmaTier("Bad", -25f, new Color(1.0f, 0.5f, 0.0f)),     // Orange
        new KarmaTier("Neutral", 0f, new Color(0.6f, 0.6f, 0.6f)),   // Gray
        new KarmaTier("Good", 25f, new Color(0.5f, 0.8f, 0.2f)),     // Light Green
        new KarmaTier("Saint", 75f, new Color(0.2f, 0.7f, 0.2f))     // Dark Green
    };

    // --- Events for Observers (Loose Coupling) ---
    // These events allow other scripts to react when karma changes without direct dependencies.
    /// <summary>
    /// Event fired when the player's karma value changes.
    /// Parameters: (newKarmaValue, oldKarmaValue).
    /// </summary>
    public static event Action<float, float> OnKarmaChanged;

    /// <summary>
    /// Event fired when the player's karma tier changes.
    /// Parameters: (newKarmaTier, oldKarmaTier).
    /// </summary>
    public static event Action<KarmaTier, KarmaTier> OnKarmaTierChanged;

    private KarmaTier _currentTier; // Internally tracks the player's current karma tier.

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Implement Singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PlayerKarmaSystem: Duplicate instance found, destroying this one. " +
                             "Ensure only one PlayerKarmaSystem exists in the scene.");
            Destroy(gameObject);
            return;
        }

        // Otherwise, set this as the singleton instance.
        Instance = this;

        // Make this object persist across scene loads. This is common for game managers.
        DontDestroyOnLoad(gameObject);

        // Ensure karma tiers are sorted by threshold for correct `GetKarmaTier` logic.
        karmaTiers = karmaTiers.OrderBy(t => t.threshold).ToList();

        // Initialize the internal current tier based on the initial karma value.
        _currentTier = GetKarmaTierForValue(currentKarma);
        Debug.Log($"PlayerKarmaSystem initialized. Initial Karma: {currentKarma}, Tier: {_currentTier.tierName}");
    }

    // OnValidate is called in the editor when the script is loaded or a value is changed in the Inspector.
    private void OnValidate()
    {
        // Ensure currentKarma stays within defined bounds when changed in the Inspector.
        currentKarma = Mathf.Clamp(currentKarma, minKarma, maxKarma);

        // Ensure minKarma is logically less than or equal to maxKarma.
        if (minKarma > maxKarma)
        {
            Debug.LogWarning("PlayerKarmaSystem: minKarma cannot be greater than maxKarma. Adjusting minKarma.");
            minKarma = maxKarma;
        }

        // Re-sort tiers in the editor to help maintain correct order.
        if (karmaTiers != null && karmaTiers.Any())
        {
            karmaTiers = karmaTiers.OrderBy(t => t.threshold).ToList();
        }
    }

    // --- Public API for Karma Manipulation and Querying ---

    /// <summary>
    /// Adds or subtracts karma from the player's current karma value.
    /// The karma value is clamped between `minKarma` and `maxKarma`.
    /// Fires `OnKarmaChanged` and `OnKarmaTierChanged` events if applicable.
    /// </summary>
    /// <param name="amount">The amount of karma to add (positive for good, negative for evil).</param>
    /// <param name="reason">An optional string describing why karma changed (e.g., "Helped NPC", "Stole Item").</param>
    public void AddKarma(float amount, string reason = "")
    {
        SetKarma(currentKarma + amount, reason);
    }

    /// <summary>
    /// Sets the player's karma to a specific value.
    /// The karma value is clamped between `minKarma` and `maxKarma`.
    /// Fires `OnKarmaChanged` and `OnKarmaTierChanged` events if applicable.
    /// </summary>
    /// <param name="value">The new karma value.</param>
    /// <param name="reason">An optional string describing why karma changed.</param>
    public void SetKarma(float value, string reason = "")
    {
        float oldKarma = currentKarma;
        currentKarma = Mathf.Clamp(value, minKarma, maxKarma);

        // Only proceed if karma actually changed to avoid unnecessary event firing.
        if (Mathf.Approximately(currentKarma, oldKarma))
        {
            return;
        }

        // Notify all subscribers that the karma value has changed.
        OnKarmaChanged?.Invoke(currentKarma, oldKarma);
        Debug.Log($"Karma changed to {currentKarma} (from {oldKarma}). Reason: {reason}");

        // Check if the karma tier has also changed.
        KarmaTier newTier = GetKarmaTierForValue(currentKarma);
        if (!newTier.Equals(_currentTier)) // Compare structs for equality
        {
            KarmaTier oldTier = _currentTier;
            _currentTier = newTier;
            // Notify all subscribers that the karma tier has changed.
            OnKarmaTierChanged?.Invoke(newTier, oldTier);
            Debug.Log($"Karma Tier changed from {oldTier.tierName} to {newTier.tierName}!");
        }

        // --- Persistence Hint ---
        // In a real game, you would likely save the karma state here or trigger a save system.
        // Example: GameSaveSystem.Instance.SavePlayerKarma(currentKarma);
    }


    /// <summary>
    /// Retrieves the player's current karma value.
    /// </summary>
    public float GetCurrentKarma()
    {
        return currentKarma;
    }

    /// <summary>
    /// Retrieves the player's current karma tier.
    /// </summary>
    public KarmaTier GetCurrentKarmaTier()
    {
        // Return the internally tracked tier. It's updated by AddKarma/SetKarma.
        return _currentTier; 
    }

    /// <summary>
    /// Gets the current karma as a normalized value between 0 and 1.
    /// Useful for UI elements like progress bars, where 0 is minKarma and 1 is maxKarma.
    /// </summary>
    public float GetKarmaPercentage()
    {
        return Mathf.InverseLerp(minKarma, maxKarma, currentKarma);
    }

    /// <summary>
    /// Determines the KarmaTier for a given karma value.
    /// It iterates through the sorted tiers and returns the highest tier
    /// whose threshold is less than or equal to the current karma.
    /// </summary>
    /// <param name="karmaValue">The karma value to check.</param>
    /// <returns>The KarmaTier corresponding to the given karma value.</returns>
    private KarmaTier GetKarmaTierForValue(float karmaValue)
    {
        // Iterate backwards through the sorted tiers (highest threshold first)
        // to find the highest tier whose threshold is met or exceeded.
        for (int i = karmaTiers.Count - 1; i >= 0; i--)
        {
            if (karmaValue >= karmaTiers[i].threshold)
            {
                return karmaTiers[i];
            }
        }
        
        // If karma is below all defined thresholds, return the lowest tier.
        // This case should generally be covered by having a lowest tier with a threshold <= minKarma.
        if (karmaTiers.Count > 0)
        {
            return karmaTiers[0];
        }

        // Fallback: If no tiers are defined, return a default "Undefined" tier.
        return new KarmaTier("Undefined", minKarma, Color.clear);
    }
}

/*
// ==================================================================================
// --- EXAMPLE USAGE SCRIPTS (How other parts of your game would interact) ---
// ==================================================================================

// To use these examples:
// 1. Create new C# scripts in Unity.
// 2. Copy and paste the content of each example into its respective script.
// 3. Attach `KarmaUIUpdater.cs` to a UI GameObject (e.g., a Text or Image).
// 4. Attach `PlayerActionHandler.cs` to your Player GameObject or a Game Manager.
// 5. Wire up the Inspector references for the UI Updater.
// 6. You can then call methods like `PlayerActionHandler.PerformGoodDeed()` from UI buttons,
//    other game logic, or debugging tools.


// --- EXAMPLE 1: KarmaUIUpdater.cs ---
// This script demonstrates how to display karma information in the UI and react to karma changes.
// Attach this to a GameObject that has Text and/or Image components for displaying karma.

using UnityEngine;
using UnityEngine.UI; // Required for UI Text and Image components

public class KarmaUIUpdater : MonoBehaviour
{
    [Header("UI References")]
    public Text karmaValueText;      // Displays the numerical karma value
    public Text karmaTierText;       // Displays the current karma tier name
    public Image karmaProgressBar;   // Optional: an Image for a progress bar (e.g., fill amount)

    private void OnEnable()
    {
        // Subscribe to karma change events when this script becomes active.
        // This ensures the UI updates whenever the karma system notifies of a change.
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.OnKarmaChanged += UpdateKarmaUI;
            PlayerKarmaSystem.OnKarmaTierChanged += UpdateKarmaTierUI;

            // Perform an initial UI update to display the current karma state immediately.
            UpdateKarmaUI(PlayerKarmaSystem.Instance.GetCurrentKarma(), 0f); // 0f as dummy old value
            UpdateKarmaTierUI(PlayerKarmaSystem.Instance.GetCurrentKarmaTier(), new KarmaTier()); // Dummy old tier
        }
        else
        {
            Debug.LogError("KarmaUIUpdater: PlayerKarmaSystem instance not found. " +
                           "Ensure PlayerKarmaSystem is initialized before KarmaUIUpdater.");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events when this script is disabled or destroyed.
        // This is crucial to prevent potential memory leaks or errors if the KarmaSystem
        // tries to invoke an event on a destroyed object.
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.OnKarmaChanged -= UpdateKarmaUI;
            PlayerKarmaSystem.OnKarmaTierChanged -= UpdateKarmaTierUI;
        }
    }

    /// <summary>
    /// Callback function for `OnKarmaChanged` event. Updates the karma value display.
    /// </summary>
    private void UpdateKarmaUI(float newKarma, float oldKarma)
    {
        if (karmaValueText != null)
        {
            karmaValueText.text = $"Karma: {newKarma:F0}"; // Display as integer, or F1 for one decimal place
        }
        if (karmaProgressBar != null)
        {
            karmaProgressBar.fillAmount = PlayerKarmaSystem.Instance.GetKarmaPercentage();
            // Optionally, change the progress bar color based on the current tier.
            karmaProgressBar.color = PlayerKarmaSystem.Instance.GetCurrentKarmaTier().displayColor;
        }
        Debug.Log($"UI Updated: Karma value from {oldKarma:F0} to {newKarma:F0}");
    }

    /// <summary>
    /// Callback function for `OnKarmaTierChanged` event. Updates the karma tier display.
    /// </summary>
    private void UpdateKarmaTierUI(KarmaTier newTier, KarmaTier oldTier)
    {
        if (karmaTierText != null)
        {
            karmaTierText.text = $"Tier: {newTier.tierName}";
            karmaTierText.color = newTier.displayColor;
        }
        Debug.Log($"UI Updated: Karma tier changed from {oldTier.tierName} to {newTier.tierName}");
    }
}


// --- EXAMPLE 2: PlayerActionHandler.cs ---
// This script demonstrates how other game logic (like player actions or quest outcomes)
// would interact with the PlayerKarmaSystem to modify karma.
// Attach this to your Player GameObject or a dedicated Game Manager.

using UnityEngine;

public class PlayerActionHandler : MonoBehaviour
{
    [Header("Karma Modifiers")]
    public float goodDeedAmount = 10f;
    public float badDeedAmount = -15f;
    public float majorGoodDeedAmount = 30f;
    public float majorBadDeedAmount = -30f;

    /// <summary>
    /// Example: Player performs a minor good deed.
    /// </summary>
    public void PerformGoodDeed()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.Instance.AddKarma(goodDeedAmount, "Helped a lost villager");
            Debug.Log($"Player performed a good deed (+{goodDeedAmount} Karma).");
        }
    }

    /// <summary>
    /// Example: Player performs a minor bad deed.
    /// </summary>
    public void PerformBadDeed()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.Instance.AddKarma(badDeedAmount, "Stole from a merchant");
            Debug.Log($"Player performed a bad deed ({badDeedAmount} Karma).");
        }
    }

    /// <summary>
    /// Example: Player performs a major good deed (e.g., saving a village).
    /// </summary>
    public void PerformMajorGoodDeed()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.Instance.AddKarma(majorGoodDeedAmount, "Saved the village from bandits");
            Debug.Log($"Player performed a major good deed (+{majorGoodDeedAmount} Karma).");
        }
    }

    /// <summary>
    /// Example: Player performs a major evil act (e.g., betraying an ally).
    /// Demonstrates using `SetKarma` for a specific outcome.
    /// </summary>
    public void PerformMajorBadDeed()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.Instance.AddKarma(majorBadDeedAmount, "Betrayed a loyal companion");
            Debug.Log($"Player performed a major bad deed ({majorBadDeedAmount} Karma).");
        }
    }

    /// <summary>
    /// Example: A debug or cheat action to reset karma to neutral.
    /// </summary>
    public void ResetKarmaToNeutral()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            PlayerKarmaSystem.Instance.SetKarma(0f, "Karma reset to neutral (debug)");
            Debug.Log("Karma reset to neutral.");
        }
    }

    /// <summary>
    /// Example: Checking karma before a dialogue option or quest becomes available.
    /// This shows how other systems can query the karma state.
    /// </summary>
    public void CheckKarmaForDialogue()
    {
        if (PlayerKarmaSystem.Instance != null)
        {
            KarmaTier currentTier = PlayerKarmaSystem.Instance.GetCurrentKarmaTier();
            float currentKarma = PlayerKarmaSystem.Instance.GetCurrentKarma();

            Debug.Log($"Current Karma: {currentKarma:F0}, Tier: {currentTier.tierName}");

            if (currentTier.tierName == "Saint" || currentTier.tierName == "Good")
            {
                Debug.Log("NPC offers a special 'Hero's Quest' due to your noble standing!");
            }
            else if (currentTier.tierName == "Evil" || currentTier.tierName == "Bad")
            {
                Debug.Log("NPC refuses to speak with you: 'Away, foul villain!'");
            }
            else // Neutral
            {
                Debug.Log("NPC offers a standard quest: 'Hello, stranger.'");
            }
        }
    }

    // You could also add Input handling here for testing:
    /*
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { PerformGoodDeed(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { PerformBadDeed(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { PerformMajorGoodDeed(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { PerformMajorBadDeed(); }
        if (Input.GetKeyDown(KeyCode.R)) { ResetKarmaToNeutral(); }
        if (Input.GetKeyDown(KeyCode.C)) { CheckKarmaForDialogue(); }
    }
    */
}
*/
```