// Unity Design Pattern Example: ReputationSystem
// This script demonstrates the ReputationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **ReputationSystem** design pattern in Unity. It provides a flexible, data-driven system for tracking the reputation of various entities (e.g., factions, NPCs, players) and mapping reputation points to descriptive tiers.

The system is composed of:
1.  **`ReputationTier`**: A simple struct to define a reputation level (e.g., "Hated", "Neutral", "Friendly") and the minimum points required to reach it.
2.  **`ReputationConfig`**: A `ScriptableObject` that acts as the data source for reputation tiers. This allows designers to easily define and modify tiers without changing code.
3.  **`ReputationManager`**: A `MonoBehaviour` singleton that manages all reputation logic. It tracks the current reputation points for different entities, provides methods to modify reputation, and notifies listeners when reputation or tiers change.

---

### **1. ReputationTier.cs**

This struct defines a single reputation tier with a name and a minimum point threshold.

```csharp
using System;
using UnityEngine;

/// <summary>
/// Represents a single reputation tier within the system.
/// This struct holds the display name of the tier and the minimum
/// reputation points required to be considered part of this tier.
/// </summary>
[Serializable]
public struct ReputationTier
{
    [Tooltip("The display name for this reputation tier (e.g., 'Hated', 'Neutral', 'Friendly').")]
    public string tierName;

    [Tooltip("The minimum reputation points required to be in this tier. " +
             "Tiers should be ordered from lowest min points to highest.")]
    public int minReputationPoints;

    public ReputationTier(string name, int minPoints)
    {
        tierName = name;
        minReputationPoints = minPoints;
    }
}
```

---

### **2. ReputationConfig.cs**

This `ScriptableObject` holds a list of `ReputationTier` objects. It's the central data asset for defining all reputation levels in your game.

```csharp
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For OrderByDescending

/// <summary>
/// A ScriptableObject that defines all reputation tiers for the game.
/// This allows designers to easily configure reputation levels without
/// modifying code. It should be created via 'Create/Reputation System/Reputation Config'.
/// </summary>
[CreateAssetMenu(fileName = "ReputationConfig", menuName = "Reputation System/Reputation Config", order = 1)]
public class ReputationConfig : ScriptableObject
{
    [Tooltip("List of all reputation tiers, ordered from lowest to highest minimum points. " +
             "The system will automatically sort them if they are not.")]
    [SerializeField]
    private List<ReputationTier> reputationTiers = new List<ReputationTier>();

    private List<ReputationTier> _sortedTiers; // Cache for sorted tiers

    /// <summary>
    /// Gets the list of reputation tiers, ensuring they are sorted by minReputationPoints
    /// in ascending order. This sorting is crucial for correct tier lookup.
    /// </summary>
    public List<ReputationTier> GetSortedTiers()
    {
        if (_sortedTiers == null || _sortedTiers.Count != reputationTiers.Count || _sortedTiers.Any(t => !reputationTiers.Contains(t)))
        {
            // Only re-sort if the original list changed or cache is invalid
            _sortedTiers = reputationTiers.OrderBy(tier => tier.minReputationPoints).ToList();
        }
        return _sortedTiers;
    }

    /// <summary>
    /// Finds the appropriate ReputationTier for a given amount of reputation points.
    /// It iterates through the sorted tiers and returns the highest tier whose
    /// minimum points are less than or equal to the provided reputation points.
    /// </summary>
    /// <param name="points">The current reputation points.</param>
    /// <returns>The ReputationTier corresponding to the points, or the lowest tier if none match.</returns>
    public ReputationTier GetTierForPoints(int points)
    {
        // Get the sorted list of tiers.
        List<ReputationTier> sortedTiers = GetSortedTiers();

        // If no tiers are defined, return a default/empty tier.
        if (sortedTiers == null || sortedTiers.Count == 0)
        {
            Debug.LogWarning("ReputationConfig has no tiers defined. Returning default tier.");
            return new ReputationTier("Undefined", 0);
        }

        // Iterate from the highest point threshold down to find the correct tier.
        // This ensures we get the *highest* tier the player qualifies for.
        for (int i = sortedTiers.Count - 1; i >= 0; i--)
        {
            if (points >= sortedTiers[i].minReputationPoints)
            {
                return sortedTiers[i];
            }
        }

        // If no tier matches (e.g., points are below the minimum of the lowest tier),
        // return the lowest defined tier.
        return sortedTiers[0];
    }

    // Called when the scriptable object is loaded or modified in the editor
    private void OnValidate()
    {
        // Clear the sorted cache to force a re-sort next time GetSortedTiers is called.
        // This is important if tiers are manually reordered in the inspector.
        _sortedTiers = null; 
    }
}
```

---

### **3. ReputationManager.cs**

This is the core manager class. It's a `MonoBehaviour` singleton responsible for tracking all entities' reputations, updating them, and notifying other parts of the game about changes.

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent

/// <summary>
/// The central manager for the ReputationSystem design pattern.
/// This MonoBehaviour singleton tracks the reputation of various entities (e.g., Factions, NPCs, Player).
/// It uses a ReputationConfig ScriptableObject to define reputation tiers.
/// It provides methods to modify reputation and events to notify listeners of changes.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static ReputationManager _instance;
    public static ReputationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ReputationManager>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("ReputationManager");
                    _instance = singletonObject.AddComponent<ReputationManager>();
                    DontDestroyOnLoad(singletonObject); // Persist across scenes
                }
            }
            return _instance;
        }
    }

    // --- Configuration ---
    [Tooltip("The ScriptableObject containing all reputation tier definitions.")]
    [SerializeField]
    private ReputationConfig reputationConfig;

    [Tooltip("The initial reputation points given to a new entity if not specified.")]
    [SerializeField]
    private int defaultInitialReputation = 0;

    // --- Internal State ---
    // Dictionary to store current reputation points for each entity (e.g., "FactionA", "Player").
    // Key: Entity ID (string), Value: Current reputation points (int).
    private Dictionary<string, int> _entityReputations = new Dictionary<string, int>();

    // --- Events ---
    // Event triggered when an entity's reputation points change.
    // Parameters: string entityID, int newReputationPoints
    public UnityEvent<string, int> OnReputationPointsChanged = new UnityEvent<string, int>();

    // Event triggered when an entity's reputation tier changes.
    // Parameters: string entityID, ReputationTier oldTier, ReputationTier newTier
    public UnityEvent<string, ReputationTier, ReputationTier> OnReputationTierChanged = new UnityEvent<string, ReputationTier, ReputationTier>();

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Ensure only one instance of the manager exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Basic validation
        if (reputationConfig == null)
        {
            Debug.LogError("ReputationManager: ReputationConfig is not assigned! " +
                           "Please create a ReputationConfig ScriptableObject (Create/Reputation System/Reputation Config) " +
                           "and assign it in the Inspector.");
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Initializes an entity with a starting reputation.
    /// If the entity already exists, its reputation will be set to the initial value.
    /// </summary>
    /// <param name="entityID">The unique identifier for the entity (e.g., "Goblin Faction", "Player").</param>
    /// <param name="initialReputation">The starting reputation points for this entity.</param>
    public void InitializeEntity(string entityID, int initialReputation = -999999) // Using a sentinel value
    {
        if (reputationConfig == null)
        {
            Debug.LogError("ReputationManager: Cannot initialize entity, ReputationConfig is null.");
            return;
        }

        int actualInitialRep = (initialReputation == -999999) ? defaultInitialReputation : initialReputation;

        if (!_entityReputations.ContainsKey(entityID))
        {
            _entityReputations.Add(entityID, actualInitialRep);
            Debug.Log($"ReputationManager: Initialized entity '{entityID}' with {actualInitialRep} points.");
            // Fire events for initial state
            OnReputationPointsChanged.Invoke(entityID, actualInitialRep);
            OnReputationTierChanged.Invoke(entityID, reputationConfig.GetTierForPoints(actualInitialRep), reputationConfig.GetTierForPoints(actualInitialRep));
        }
        else
        {
            SetReputation(entityID, actualInitialRep);
        }
    }

    /// <summary>
    /// Adds reputation points to a specific entity.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <param name="amount">The amount of reputation points to gain.</param>
    public void GainReputation(string entityID, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"ReputationManager: Called GainReputation with a negative amount ({amount}). Use LoseReputation instead.");
            LoseReputation(entityID, -amount);
            return;
        }
        UpdateReputation(entityID, amount);
    }

    /// <summary>
    /// Subtracts reputation points from a specific entity.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <param name="amount">The amount of reputation points to lose.</param>
    public void LoseReputation(string entityID, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"ReputationManager: Called LoseReputation with a negative amount ({amount}). Use GainReputation instead.");
            GainReputation(entityID, -amount);
            return;
        }
        UpdateReputation(entityID, -amount);
    }

    /// <summary>
    /// Directly sets the reputation points for an entity.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <param name="newReputation">The new total reputation points for the entity.</param>
    public void SetReputation(string entityID, int newReputation)
    {
        if (!_entityReputations.ContainsKey(entityID))
        {
            Debug.LogWarning($"ReputationManager: Entity '{entityID}' not found. Initializing with {newReputation} points.");
            InitializeEntity(entityID, newReputation);
            return;
        }

        int oldReputation = _entityReputations[entityID];
        ReputationTier oldTier = reputationConfig.GetTierForPoints(oldReputation);

        _entityReputations[entityID] = newReputation;
        Debug.Log($"ReputationManager: Entity '{entityID}' reputation set to {newReputation}.");

        ReputationTier newTier = reputationConfig.GetTierForPoints(newReputation);

        // Notify listeners
        OnReputationPointsChanged.Invoke(entityID, newReputation);
        if (!oldTier.Equals(newTier)) // Assuming ReputationTier has a meaningful Equals or check names
        {
            OnReputationTierChanged.Invoke(entityID, oldTier, newTier);
        }
    }

    /// <summary>
    /// Gets the current reputation points for a specific entity.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <returns>The current reputation points, or 0 if the entity is not tracked.</returns>
    public int GetReputation(string entityID)
    {
        if (_entityReputations.TryGetValue(entityID, out int reputation))
        {
            return reputation;
        }
        Debug.LogWarning($"ReputationManager: Entity '{entityID}' not found. Returning 0 reputation.");
        return 0;
    }

    /// <summary>
    /// Gets the current reputation tier for a specific entity.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <returns>The current ReputationTier, or a default "Undefined" tier if not found.</returns>
    public ReputationTier GetReputationTier(string entityID)
    {
        if (reputationConfig == null)
        {
            Debug.LogError("ReputationManager: Cannot get tier, ReputationConfig is null.");
            return new ReputationTier("Error", 0);
        }

        if (_entityReputations.TryGetValue(entityID, out int reputation))
        {
            return reputationConfig.GetTierForPoints(reputation);
        }
        Debug.LogWarning($"ReputationManager: Entity '{entityID}' not found. Returning lowest tier.");
        // If entity not found, return the lowest tier defined or a default.
        return reputationConfig.GetSortedTiers().Count > 0 ? reputationConfig.GetSortedTiers()[0] : new ReputationTier("Undefined", 0);
    }

    // --- Private Helper ---

    /// <summary>
    /// Internal method to update an entity's reputation points and trigger events.
    /// </summary>
    /// <param name="entityID">The unique identifier of the entity.</param>
    /// <param name="delta">The change in reputation points (can be positive or negative).</param>
    private void UpdateReputation(string entityID, int delta)
    {
        if (!_entityReputations.ContainsKey(entityID))
        {
            Debug.LogWarning($"ReputationManager: Entity '{entityID}' not found. Initializing with default reputation and applying delta.");
            InitializeEntity(entityID, defaultInitialReputation + delta); // Initialize and apply delta
            return;
        }

        int oldReputation = _entityReputations[entityID];
        ReputationTier oldTier = reputationConfig.GetTierForPoints(oldReputation);

        _entityReputations[entityID] += delta;
        int newReputation = _entityReputations[entityID];
        Debug.Log($"ReputationManager: Entity '{entityID}' reputation changed by {delta}. New points: {newReputation}.");

        ReputationTier newTier = reputationConfig.GetTierForPoints(newReputation);

        // Notify listeners
        OnReputationPointsChanged.Invoke(entityID, newReputation);
        if (!oldTier.Equals(newTier)) // Check if tier has actually changed
        {
            OnReputationTierChanged.Invoke(entityID, oldTier, newTier);
        }
    }

    // --- Persistence (Conceptual) ---
    // For a real project, you would need methods here to Save and Load
    // the _entityReputations dictionary, perhaps using JSON, BinaryFormatter,
    // or Unity's built-in serialization (e.g., by converting to a list of serializable structs).
    //
    // Example (conceptual):
    /*
    [Serializable]
    private struct EntityReputationData
    {
        public string entityID;
        public int reputationPoints;
    }

    public string SaveReputationData()
    {
        List<EntityReputationData> data = new List<EntityReputationData>();
        foreach (var entry in _entityReputations)
        {
            data.Add(new EntityReputationData { entityID = entry.Key, reputationPoints = entry.Value });
        }
        return JsonUtility.ToJson(new { reputations = data }); // Wrap in an object for proper JSON array serialization
    }

    public void LoadReputationData(string json)
    {
        _entityReputations.Clear();
        // Parse JSON back into dictionary and trigger events for loaded states
        // This would involve more complex parsing if using JsonUtility,
        // or a custom JSON library like Newtonsoft.Json.
    }
    */
}
```

---

### **How to Use in Unity:**

1.  **Create ReputationConfig:**
    *   In your Unity Project window, right-click -> `Create` -> `Reputation System` -> `Reputation Config`.
    *   Name it something like "GameReputationConfig".
    *   Select this new `ReputationConfig` asset.
    *   In the Inspector, expand `Reputation Tiers`. Add new tiers and define their `Tier Name` and `Min Reputation Points`. **Order them from lowest `Min Reputation Points` to highest.**

    *Example Configuration:*
    *   **Tier 0:** Name: "Hated", Min Points: -500
    *   **Tier 1:** Name: "Disliked", Min Points: -100
    *   **Tier 2:** Name: "Neutral", Min Points: 0
    *   **Tier 3:** Name: "Friendly", Min Points: 100
    *   **Tier 4:** Name: "Honored", Min Points: 500

2.  **Add ReputationManager to Scene:**
    *   Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> `Create Empty`).
    *   Name it "ReputationManager".
    *   Add the `ReputationManager` script to this GameObject.
    *   In the Inspector for the "ReputationManager" GameObject, drag your "GameReputationConfig" asset into the `Reputation Config` field.

3.  **Example Usage in Another Script:**

    ```csharp
    using UnityEngine;

    public class GameEventHandler : MonoBehaviour
    {
        private const string PLAYER_FACTION_ID = "Player";
        private const string GOBLIN_FACTION_ID = "Goblin Tribe";
        private const string ELF_FACTION_ID = "Silverwood Elves";

        void Start()
        {
            // Initialize entities (factions) if they don't exist
            // This is good practice to ensure they are tracked from the start
            ReputationManager.Instance.InitializeEntity(PLAYER_FACTION_ID, 0); // Player starts neutral
            ReputationManager.Instance.InitializeEntity(GOBLIN_FACTION_ID, -50); // Goblins start disliked
            ReputationManager.Instance.InitializeEntity(ELF_FACTION_ID, 20); // Elves start slightly friendly

            // Subscribe to events (optional, but very powerful for UI/game logic)
            ReputationManager.Instance.OnReputationPointsChanged.AddListener(OnReputationPointsChanged);
            ReputationManager.Instance.OnReputationTierChanged.AddListener(OnReputationTierChanged);

            Debug.Log($"--- Initial Reputation ---");
            LogFactionStatus(GOBLIN_FACTION_ID);
            LogFactionStatus(ELF_FACTION_ID);
        }

        void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks when this object is destroyed
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.OnReputationPointsChanged.RemoveListener(OnReputationPointsChanged);
                ReputationManager.Instance.OnReputationTierChanged.RemoveListener(OnReputationTierChanged);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G)) // Gain reputation with Goblins
            {
                ReputationManager.Instance.GainReputation(GOBLIN_FACTION_ID, 20);
                Debug.Log("Player helped a Goblin! Gained 20 reputation with Goblin Tribe.");
            }
            if (Input.GetKeyDown(KeyCode.L)) // Lose reputation with Elves
            {
                ReputationManager.Instance.LoseReputation(ELF_FACTION_ID, 30);
                Debug.Log("Player chopped down an ancient tree! Lost 30 reputation with Silverwood Elves.");
            }
            if (Input.GetKeyDown(KeyCode.S)) // Set player reputation directly
            {
                // This might be used for specific quest outcomes or cheats
                ReputationManager.Instance.SetReputation(PLAYER_FACTION_ID, 150);
                Debug.Log("Player performed a legendary deed! Player reputation set to 150.");
            }
            if (Input.GetKeyDown(KeyCode.C)) // Check current status
            {
                Debug.Log("--- Current Status Check ---");
                LogFactionStatus(GOBLIN_FACTION_ID);
                LogFactionStatus(ELF_FACTION_ID);
                LogFactionStatus(PLAYER_FACTION_ID);
            }
        }

        /// <summary>
        /// Event listener for when reputation points change.
        /// </summary>
        private void OnReputationPointsChanged(string entityID, int newPoints)
        {
            Debug.Log($"EVENT: {entityID} reputation points changed to {newPoints}.");
            // Update UI element, play a sound, etc.
        }

        /// <summary>
        /// Event listener for when reputation tier changes.
        /// </summary>
        private void OnReputationTierChanged(string entityID, ReputationTier oldTier, ReputationTier newTier)
        {
            Debug.Log($"EVENT: {entityID} reputation tier changed from '{oldTier.tierName}' to '{newTier.tierName}'!");
            // Display a notification, unlock content, change NPC behavior, etc.
        }

        /// <summary>
        /// Helper to log an entity's current reputation status.
        /// </summary>
        private void LogFactionStatus(string factionID)
        {
            int currentPoints = ReputationManager.Instance.GetReputation(factionID);
            ReputationTier currentTier = ReputationManager.Instance.GetReputationTier(factionID);
            Debug.Log($"{factionID}: {currentPoints} points ({currentTier.tierName})");
        }
    }
    ```

    *   Create an empty GameObject in your scene (e.g., "GameEvents").
    *   Attach the `GameEventHandler` script to it.
    *   Run the game and press 'G', 'L', 'S', 'C' to see the reputation system in action in the console.

---

This complete example provides a robust and extensible foundation for a reputation system in your Unity projects, adhering to common design patterns and best practices.