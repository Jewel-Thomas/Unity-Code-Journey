// Unity Design Pattern Example: XPBoostSystem
// This script demonstrates the XPBoostSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **XP Boost System** design pattern in Unity, which is essentially a central service for managing various XP multipliers and additive bonuses that can be applied to a player's base XP gain. This pattern promotes modularity, making it easy to add new types of boosts, manage their durations, and ensure consistent XP calculation across the game.

---

### XP Boost System Design Pattern Explanation

**Purpose:**
To provide a flexible and centralized way to manage various sources of experience point (XP) boosts (e.g., temporary potions, permanent perks, subscription bonuses) and apply them consistently when a player gains XP.

**Core Components:**

1.  **`XPBoostDefinition` (ScriptableObject):**
    *   **Role:** Defines the *blueprint* or *type* of an XP boost. This is immutable data, meaning it describes what a "2x XP Potion" or "Premium Subscription Bonus" *is*.
    *   **Details:** Contains properties like `boostID` (unique identifier), `displayName`, `multiplier` (e.g., 0.5 for +50%), and `durationSeconds` (0 for permanent).
    *   **Benefit:** Allows designers to create and configure different boost types directly in the Unity Editor without touching code, promoting data-driven design.

2.  **`XPBoostInstance` (Plain C# Class):**
    *   **Role:** Represents an *active instance* of an `XPBoostDefinition`. This holds the dynamic state of a boost currently affecting the player.
    *   **Details:** Contains a reference to its `XPBoostDefinition` and `timeRemaining` for temporary boosts.
    *   **Benefit:** Separates the static definition of a boost from its dynamic runtime state (e.g., how much time is left).

3.  **`XPBoostSystem` (MonoBehaviour Singleton):**
    *   **Role:** The central manager for all XP boost-related logic. It's a `Singleton` so it can be easily accessed from any part of the game.
    *   **Details:**
        *   Maintains a list of `_activeBoosts` (`List<XPBoostInstance>`).
        *   Provides methods to `AddBoost` and `RemoveBoost`.
        *   The core method `CalculateFinalXP(float baseXP)` takes a base XP value and applies all currently active boost multipliers to return the final boosted XP.
        *   Handles the expiration of temporary boosts in its `Update` method.
        *   Includes an `OnActiveBoostsChanged` event for UI or other systems to react to boost state changes.
    *   **Benefit:** Encapsulates all boost management logic, ensuring that XP calculation is consistent and that boost effects are correctly applied and managed (e.g., durations). Other systems only need to call `XPBoostSystem.Instance.CalculateFinalXP()` without worrying about *how* boosts are applied.

---

### File Structure and Setup in Unity

You will need to create **five** C# scripts and place them in your Unity project.

1.  **`XPBoostDefinition.cs`**: Define boost types.
2.  **`XPBoostInstance.cs`**: Manage active boost states.
3.  **`XPBoostSystem.cs`**: The core singleton manager.
4.  **`PlayerXPManager.cs`**: An example of a system consuming the XPBoostSystem.
5.  **`TestXPBooster.cs`**: An example script to add/remove boosts and gain XP for testing.

**Steps to use:**

1.  Create the five C# scripts as provided below.
2.  In Unity, create an empty GameObject in your scene (e.g., "GameManager").
3.  Attach the `XPBoostSystem.cs` script to the "GameManager" GameObject.
4.  Attach the `PlayerXPManager.cs` script to the "GameManager" GameObject (or a separate "Player" GameObject).
5.  Attach the `TestXPBooster.cs` script to any GameObject in your scene (e.g., "TestInput").
6.  Create some `XPBoostDefinition` ScriptableObjects:
    *   Go to `Assets/Create/XP System/XP Boost Definition`.
    *   Create a few, for example:
        *   **2x_Potion:** `boostID="Potion2xXP"`, `displayName="2x XP Potion"`, `multiplier=1.0` (for 100% boost -> 2x total), `durationSeconds=60`.
        *   **Premium_Sub:** `boostID="PremiumSubscription"`, `displayName="Premium Subscription"`, `multiplier=0.5` (for 50% boost), `durationSeconds=0` (permanent).
        *   **Event_Bonus:** `boostID="WeekendEvent"`, `displayName="Weekend Event Bonus"`, `multiplier=0.25` (for 25% boost), `durationSeconds=300`.
7.  Drag these created `XPBoostDefinition` assets into the `TestXPBooster` script's `Boost Definitions` slots in the Inspector.
8.  Run the scene. Use the buttons on the `TestXPBooster` component in the Inspector to add/remove boosts and gain XP. Observe the console output.

---

### 1. `XPBoostDefinition.cs`

```csharp
using UnityEngine;

/// <summary>
/// Defines the blueprint for an XP boost.
/// This is a ScriptableObject, allowing designers to create and configure
/// different boost types directly in the Unity Editor without code changes.
/// </summary>
[CreateAssetMenu(fileName = "NewXPBoost", menuName = "XP System/XP Boost Definition")]
public class XPBoostDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this boost type (e.g., 'PremiumSubscription', 'Potion2xXP'). " +
             "Used to identify and manage active boosts.")]
    public string boostID;

    [Tooltip("User-friendly name for displaying in UI.")]
    public string displayName;

    [Tooltip("The multiplier for XP gain. E.g., 0.5 for +50% XP, 1.0 for +100% XP (double XP).")]
    [Range(0f, 10f)] // Boosts are typically positive, limit for sanity
    public float multiplier = 0.5f;

    [Tooltip("Duration of the boost in seconds. Set to 0 for a permanent boost.")]
    public float durationSeconds = 0f;

    /// <summary>
    /// Called when the scriptable object is loaded or a value is changed in the editor.
    /// Ensures the boostID is set if not already defined.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(boostID))
        {
            boostID = name; // Use asset name as default ID
            #if UNITY_EDITOR
            // To prevent runtime issues if ID is not unique
            UnityEditor.EditorUtility.SetDirty(this); 
            #endif
        }
    }
}
```

---

### 2. `XPBoostInstance.cs`

```csharp
using System; // For TimeSpan

/// <summary>
/// Represents an active instance of an XP boost in the game.
/// This class holds the dynamic state of a boost (e.g., remaining time)
/// and a reference to its static definition.
/// </summary>
[System.Serializable] // Make it visible in the Inspector for debugging purposes
public class XPBoostInstance
{
    [Tooltip("Reference to the immutable definition of this boost.")]
    public XPBoostDefinition definition;

    [Tooltip("How much time is left for this specific boost instance. Only relevant for temporary boosts.")]
    public float timeRemaining;

    /// <summary>
    /// Returns true if this boost is permanent (durationSeconds <= 0).
    /// </summary>
    public bool IsPermanent => definition.durationSeconds <= 0f;

    /// <summary>
    /// Returns true if this boost is currently active (either permanent or has time remaining).
    /// </summary>
    public bool IsActive => IsPermanent || timeRemaining > 0f;

    /// <summary>
    /// Constructor for a new XPBoostInstance.
    /// Initializes timeRemaining based on the definition's duration.
    /// </summary>
    /// <param name="def">The XPBoostDefinition this instance is based on.</param>
    public XPBoostInstance(XPBoostDefinition def)
    {
        definition = def;
        timeRemaining = def.durationSeconds;
    }

    /// <summary>
    /// Formats the remaining time into a human-readable string.
    /// </summary>
    /// <returns>A formatted string of the remaining time.</returns>
    public string GetRemainingTimeFormatted()
    {
        if (IsPermanent) return "Permanent";
        if (timeRemaining <= 0) return "Expired";

        TimeSpan span = TimeSpan.FromSeconds(timeRemaining);
        if (span.TotalHours >= 1) return $"{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00} remaining";
        if (span.TotalMinutes >= 1) return $"{span.Minutes:00}:{span.Seconds:00} remaining";
        return $"{span.Seconds:00}s remaining";
    }
}
```

---

### 3. `XPBoostSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like FirstOrDefault, Sum
using System; // For Action event

/// <summary>
/// The central manager for all XP boost related logic.
/// This is a Singleton, ensuring there's only one instance throughout the game,
/// providing easy and consistent access to XP boost management and calculation.
/// </summary>
public class XPBoostSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static XPBoostSystem Instance { get; private set; }

    // List of currently active XP boost instances.
    // [SerializeField] makes it visible in the Inspector for debugging.
    [SerializeField] private List<XPBoostInstance> _activeBoosts = new List<XPBoostInstance>();

    // Event that other systems can subscribe to when the list of active boosts changes.
    // Useful for updating UI elements (e.g., "Active Boosts" panel).
    public event Action OnActiveBoostsChanged;

    /// <summary>
    /// Initializes the Singleton instance.
    /// Ensures only one XPBoostSystem exists and persists across scene loads.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple XPBoostSystem instances found. Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: If you want the boost system to persist across scene changes.
            DontDestroyOnLoad(gameObject); 
            Debug.Log("XPBoostSystem initialized.");
        }
    }

    /// <summary>
    /// Called once per frame. Handles decrementing the duration of temporary boosts
    /// and removing expired boosts from the active list.
    /// </summary>
    private void Update()
    {
        if (_activeBoosts.Count == 0) return;

        List<XPBoostInstance> boostsToRemove = new List<XPBoostInstance>();
        bool changed = false;

        // Iterate through active boosts to update their remaining time.
        // Identify expired boosts for removal.
        foreach (var boost in _activeBoosts)
        {
            if (!boost.IsPermanent) // Only process temporary boosts
            {
                boost.timeRemaining -= Time.deltaTime;
                if (boost.timeRemaining <= 0)
                {
                    boostsToRemove.Add(boost);
                    changed = true; // Mark that a change occurred
                }
            }
        }

        // Remove all identified expired boosts.
        foreach (var expiredBoost in boostsToRemove)
        {
            _activeBoosts.Remove(expiredBoost);
            Debug.Log($"XP Boost '{expiredBoost.definition.displayName}' ({expiredBoost.definition.boostID}) has expired.");
        }

        // If any boosts were added, removed, or expired, notify subscribers.
        if (changed)
        {
            OnActiveBoostsChanged?.Invoke(); 
        }
    }

    // --- Public API for Managing Boosts ---

    /// <summary>
    /// Adds a new XP boost to the system or refreshes an existing one.
    /// If a boost with the same ID already exists, its duration is refreshed.
    /// Different boost IDs stack their multipliers.
    /// </summary>
    /// <param name="definition">The ScriptableObject definition of the XP boost.</param>
    public void AddBoost(XPBoostDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("Attempted to add a null XPBoostDefinition.");
            return;
        }

        // Try to find an existing instance of this boost definition
        XPBoostInstance existingBoost = _activeBoosts.FirstOrDefault(b => b.definition.boostID == definition.boostID);

        if (existingBoost != null)
        {
            // If the boost already exists, refresh its duration.
            existingBoost.timeRemaining = definition.durationSeconds;
            Debug.Log($"XP Boost '{definition.displayName}' ({definition.boostID}) duration refreshed to {definition.durationSeconds}s.");
        }
        else
        {
            // If it's a new boost, create an instance and add it.
            XPBoostInstance newBoost = new XPBoostInstance(definition);
            _activeBoosts.Add(newBoost);
            Debug.Log($"XP Boost '{definition.displayName}' ({definition.boostID}) added. Multiplier: +{definition.multiplier * 100}%, " +
                      (definition.durationSeconds > 0 ? $"Duration: {definition.durationSeconds}s." : "Permanent."));
        }

        OnActiveBoostsChanged?.Invoke(); // Notify listeners about the change
    }

    /// <summary>
    /// Removes an active XP boost by its unique ID.
    /// </summary>
    /// <param name="boostID">The unique identifier of the boost to remove.</param>
    public void RemoveBoost(string boostID)
    {
        XPBoostInstance boostToRemove = _activeBoosts.FirstOrDefault(b => b.definition.boostID == boostID);
        if (boostToRemove != null)
        {
            _activeBoosts.Remove(boostToRemove);
            Debug.Log($"XP Boost '{boostToRemove.definition.displayName}' ({boostToRemove.definition.boostID}) manually removed.");
            OnActiveBoostsChanged?.Invoke(); // Notify listeners
        }
        else
        {
            Debug.LogWarning($"Attempted to remove XP boost with ID '{boostID}', but it was not found.");
        }
    }

    /// <summary>
    /// Calculates the final XP value after applying all active boosts.
    /// This is the core calculation method.
    /// </summary>
    /// <param name="baseXP">The initial base XP value before boosts.</param>
    /// <returns>The final boosted XP value.</returns>
    public float CalculateFinalXP(float baseXP)
    {
        if (baseXP < 0)
        {
            Debug.LogWarning($"Base XP cannot be negative. Clamping to 0. Given: {baseXP}");
            baseXP = 0;
        }

        // Sum up all active multipliers from the boost definitions.
        // For example, if boost A gives +50% (0.5) and boost B gives +20% (0.2),
        // totalMultiplierBonus will be 0.7.
        float totalMultiplierBonus = _activeBoosts.Sum(b => b.definition.multiplier);

        // Apply the total multiplier to the base XP.
        // Formula: Final XP = Base XP * (1 + Sum of Multiplier Bonuses)
        // E.g., 100 base XP * (1 + 0.7) = 170 final XP.
        float finalXP = baseXP * (1f + totalMultiplierBonus);

        return finalXP;
    }

    /// <summary>
    /// Gets a read-only list of currently active boost instances.
    /// Useful for displaying active boosts in UI.
    /// </summary>
    /// <returns>A read-only list of active XPBoostInstance objects.</returns>
    public IReadOnlyList<XPBoostInstance> GetActiveBoosts()
    {
        return _activeBoosts;
    }

    /// <summary>
    /// Utility method to get the current total XP multiplier bonus percentage.
    /// </summary>
    /// <returns>The total multiplier bonus, e.g., 0.7 for +70% XP.</returns>
    public float GetCurrentTotalMultiplierBonus()
    {
        return _activeBoosts.Sum(b => b.definition.multiplier);
    }

    // --- Editor Helper for Inspector Display / Debugging ---
    [ContextMenu("Log Current Boost State")]
    private void LogBoostState()
    {
        if (_activeBoosts.Count == 0)
        {
            Debug.Log("--- No active XP boosts ---");
            return;
        }

        Debug.Log("--- Current Active XP Boosts ---");
        foreach (var boost in _activeBoosts)
        {
            Debug.Log($"ID: {boost.definition.boostID}, Display: {boost.definition.displayName}, " +
                      $"Multiplier: +{boost.definition.multiplier * 100:F0}%, " +
                      $"Remaining: {boost.GetRemainingTimeFormatted()}");
        }
        Debug.Log($"Total Multiplier Bonus: +{GetCurrentTotalMultiplierBonus() * 100:F0}%");
        Debug.Log("---------------------------------");
    }
}
```

---

### 4. `PlayerXPManager.cs` (Example Consumer)

This script demonstrates how another system (e.g., a player's XP system) would interact with the `XPBoostSystem` to gain XP and react to boost changes.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example Player XP Manager that uses the XPBoostSystem.
/// This script manages the player's current XP and uses the XPBoostSystem
/// to calculate final XP gains after applying all active boosts.
/// It also subscribes to boost changes for UI updates (demonstrated via logs).
/// </summary>
public class PlayerXPManager : MonoBehaviour
{
    [SerializeField] private float currentXP = 0f;
    [SerializeField] private int playerLevel = 1;

    public float CurrentXP => currentXP;
    public int PlayerLevel => playerLevel;

    private void OnEnable()
    {
        // Subscribe to the event when boosts are added, removed, or expire.
        // This allows the UI or other systems to react.
        if (XPBoostSystem.Instance != null)
        {
            XPBoostSystem.Instance.OnActiveBoostsChanged += UpdateBoostDisplay;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks when the object is destroyed.
        if (XPBoostSystem.Instance != null)
        {
            XPBoostSystem.Instance.OnActiveBoostsChanged -= UpdateBoostDisplay;
        }
    }

    /// <summary>
    /// Simulates gaining XP, applying all active boosts via XPBoostSystem.
    /// </summary>
    /// <param name="baseAmount">The base amount of XP to gain before boosts.</param>
    public void GainXP(float baseAmount)
    {
        if (XPBoostSystem.Instance == null)
        {
            Debug.LogError("XPBoostSystem is not initialized. Cannot gain XP.", this);
            return;
        }

        // --- Core usage of XPBoostSystem ---
        float finalXP = XPBoostSystem.Instance.CalculateFinalXP(baseAmount);
        // -----------------------------------

        currentXP += finalXP;
        Debug.Log($"Player gained {baseAmount} base XP. After boosts (+{XPBoostSystem.Instance.GetCurrentTotalMultiplierBonus() * 100:F0}%): " +
                  $"{finalXP:F2} final XP. Total XP: {currentXP:F2}");

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        // Simple leveling system for demonstration
        float xpToNextLevel = GetXPRequiredForLevel(playerLevel + 1);
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel; // Carry over excess XP
            playerLevel++;
            Debug.Log($"Player Leveled Up! New Level: {playerLevel}. Remaining XP: {currentXP:F2}");
            xpToNextLevel = GetXPRequiredForLevel(playerLevel + 1); // Update required XP for next level
        }
    }

    private float GetXPRequiredForLevel(int level)
    {
        // Simple exponential scaling for XP requirements
        return 100f * Mathf.Pow(1.1f, level - 1);
    }

    /// <summary>
    /// Callback method for when active boosts change.
    /// In a real game, this would update UI elements. Here, it logs to console.
    /// </summary>
    private void UpdateBoostDisplay()
    {
        IReadOnlyList<XPBoostInstance> activeBoosts = XPBoostSystem.Instance.GetActiveBoosts();
        if (activeBoosts.Count == 0)
        {
            Debug.Log("[PlayerXPManager] All XP boosts cleared or expired.");
            return;
        }

        string boostSummary = "[PlayerXPManager] Active Boosts Updated:";
        foreach (var boost in activeBoosts)
        {
            boostSummary += $"\n- {boost.definition.displayName} (+{boost.definition.multiplier * 100:F0}%, {boost.GetRemainingTimeFormatted()})";
        }
        boostSummary += $"\nTotal XP Multiplier Bonus: +{XPBoostSystem.Instance.GetCurrentTotalMultiplierBonus() * 100:F0}%";
        Debug.Log(boostSummary);
    }

    [ContextMenu("Gain 50 Base XP")]
    public void TestGainXP()
    {
        GainXP(50f);
    }
}
```

---

### 5. `TestXPBooster.cs` (Example Test Script)

This script provides buttons in the Unity Inspector to easily test adding, removing, and gaining XP with boosts.

```csharp
using UnityEngine;

/// <summary>
/// A simple script to demonstrate adding/removing boosts and gaining XP
/// using the XPBoostSystem and PlayerXPManager.
/// Attach this to any GameObject in your scene and assign XPBoostDefinition ScriptableObjects.
/// </summary>
public class TestXPBooster : MonoBehaviour
{
    [Header("XP Boost Definitions for Testing")]
    [Tooltip("Drag your XPBoostDefinition ScriptableObjects here to test adding them.")]
    public XPBoostDefinition[] boostDefinitions;

    [Header("Test Actions")]
    public float baseXPToGain = 50f;

    /// <summary>
    /// Adds a specific boost definition to the system.
    /// Called via Inspector button.
    /// </summary>
    public void AddBoost(int index)
    {
        if (index < 0 || index >= boostDefinitions.Length || boostDefinitions[index] == null)
        {
            Debug.LogWarning("Invalid boost definition index or definition is null.");
            return;
        }

        if (XPBoostSystem.Instance != null)
        {
            XPBoostSystem.Instance.AddBoost(boostDefinitions[index]);
        }
        else
        {
            Debug.LogError("XPBoostSystem instance not found.");
        }
    }

    /// <summary>
    /// Removes a specific boost by its ID.
    /// Called via Inspector button.
    /// </summary>
    public void RemoveBoost(string boostID)
    {
        if (string.IsNullOrEmpty(boostID))
        {
            Debug.LogWarning("Boost ID cannot be empty.");
            return;
        }

        if (XPBoostSystem.Instance != null)
        {
            XPBoostSystem.Instance.RemoveBoost(boostID);
        }
        else
        {
            Debug.LogError("XPBoostSystem instance not found.");
        }
    }

    /// <summary>
    /// Triggers XP gain in the PlayerXPManager.
    /// Called via Inspector button.
    /// </summary>
    public void GainXP()
    {
        PlayerXPManager playerXPManager = FindObjectOfType<PlayerXPManager>();
        if (playerXPManager != null)
        {
            playerXPManager.GainXP(baseXPToGain);
        }
        else
        {
            Debug.LogError("PlayerXPManager not found in scene. Please add it to a GameObject.");
        }
    }

    /// <summary>
    /// Logs the current state of boosts using the XPBoostSystem's internal debug method.
    /// Called via Inspector button.
    /// </summary>
    public void LogCurrentBoostState()
    {
        if (XPBoostSystem.Instance != null)
        {
            // We can directly call the ContextMenu method here, or wrap it in a public method.
            // For simplicity, let's just make a public method to call it.
            XPBoostSystem.Instance.SendMessage("LogBoostState", SendMessageOptions.DontRequireReceiver);
            // Alternatively, if LogBoostState was public: XPBoostSystem.Instance.LogBoostState();
        }
        else
        {
            Debug.LogError("XPBoostSystem instance not found.");
        }
    }

    /// <summary>
    /// Helper for the Inspector to draw buttons easily.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(TestXPBooster))]
    public class TestXPBoosterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TestXPBooster testScript = (TestXPBooster)target;

            GUILayout.Space(10);
            GUILayout.Label("Add Boosts (based on Boost Definitions array)", UnityEditor.EditorStyles.boldLabel);
            for (int i = 0; i < testScript.boostDefinitions.Length; i++)
            {
                XPBoostDefinition def = testScript.boostDefinitions[i];
                if (def != null)
                {
                    if (GUILayout.Button($"Add '{def.displayName}' ({def.boostID})"))
                    {
                        testScript.AddBoost(i);
                    }
                }
                else
                {
                    GUILayout.Label($"Slot {i}: (Empty Definition)");
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Remove Boosts (by ID)", UnityEditor.EditorStyles.boldLabel);
            for (int i = 0; i < testScript.boostDefinitions.Length; i++)
            {
                XPBoostDefinition def = testScript.boostDefinitions[i];
                if (def != null)
                {
                    if (GUILayout.Button($"Remove '{def.displayName}' ({def.boostID})"))
                    {
                        testScript.RemoveBoost(def.boostID);
                    }
                }
            }
            
            GUILayout.Space(20);
            if (GUILayout.Button($"Gain {testScript.baseXPToGain} Base XP"))
            {
                testScript.GainXP();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Log All Active Boosts State"))
            {
                testScript.LogCurrentBoostState();
            }
        }
    }
}
```