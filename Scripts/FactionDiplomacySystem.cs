// Unity Design Pattern Example: FactionDiplomacySystem
// This script demonstrates the FactionDiplomacySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Faction Diplomacy System** design pattern. It provides a centralized manager for all faction relationships, allowing various game systems to query and modify diplomatic statuses between entities.

The system is designed to be practical, extensible, and easy to integrate into Unity projects.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // For LINQ operations

namespace GameSystems.Diplomacy
{
    /// <summary>
    /// Represents the various diplomatic states a Faction can have towards another.
    /// </summary>
    public enum DiplomacyStatus
    {
        AtWar,       // Actively engaged in hostilities
        Hostile,     // Negative relations, potential for conflict
        Neutral,     // Indifferent, no strong feelings either way
        Friendly,    // Positive relations, potential for cooperation
        Allied       // Strong positive relations, mutual support
    }

    /// <summary>
    /// [SCRIPTABLE OBJECT] Represents a single Faction in the game world.
    /// ScriptableObjects are used here to define factions as reusable data assets.
    /// This allows designers to create and configure factions directly in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFaction", menuName = "Game Systems/Diplomacy/Faction")]
    public class Faction : ScriptableObject
    {
        [Tooltip("A unique identifier name for the faction.")]
        public string FactionName = "New Faction";

        [Tooltip("A descriptive color for the faction (e.g., for UI representation).")]
        public Color FactionColor = Color.white;

        [Tooltip("A brief description of the faction.")]
        [TextArea(3, 5)]
        public string Description = "A newly created faction.";

        // You can add more properties here, e.g., default starting diplomacy values,
        // leader names, territories, etc.

        public override string ToString()
        {
            return FactionName;
        }

        // It's good practice to override GetHashCode and Equals for ScriptableObjects
        // if they are used as dictionary keys or in collections where identity matters.
        // For simplicity, we'll rely on Unity's default InstanceID comparison in FactionPairKey.
    }

    /// <summary>
    /// Represents the relationship details between two factions.
    /// This struct holds the current diplomacy score and derives the status from it.
    /// </summary>
    [Serializable] // Make it serializable so it can be viewed in the Inspector if needed,
                   // although it's primarily managed internally.
    public struct FactionRelationship
    {
        public Faction FactionA;
        public Faction FactionB;

        [Tooltip("The raw numerical value representing the relationship. Range: -100 to 100.")]
        [Range(-100, 100)]
        public int DiplomacyScore; // -100 (worst) to 100 (best)

        /// <summary>
        /// Gets the current diplomatic status based on the DiplomacyScore.
        /// This is a derived property, so it always reflects the score.
        /// </summary>
        public DiplomacyStatus CurrentStatus
        {
            get { return GetStatusFromScore(DiplomacyScore); }
        }

        public FactionRelationship(Faction faction1, Faction faction2, int initialScore)
        {
            // Ensure consistent ordering for FactionA and FactionB based on InstanceID
            // This isn't strictly necessary for the Relationship struct itself,
            // but it's good practice for clarity. The FactionPairKey handles the symmetry.
            if (faction1.GetInstanceID() < faction2.GetInstanceID())
            {
                FactionA = faction1;
                FactionB = faction2;
            }
            else
            {
                FactionA = faction2;
                FactionB = faction1;
            }
            DiplomacyScore = initialScore;
        }

        /// <summary>
        /// Determines the DiplomacyStatus based on a given score.
        /// This defines the thresholds for each status level.
        /// </summary>
        /// <param name="score">The diplomacy score.</param>
        /// <returns>The corresponding DiplomacyStatus.</returns>
        public static DiplomacyStatus GetStatusFromScore(int score)
        {
            if (score <= -75) return DiplomacyStatus.AtWar;
            if (score <= -25) return DiplomacyStatus.Hostile;
            if (score < 25) return DiplomacyStatus.Neutral; // Scores -24 to 24 are Neutral
            if (score < 75) return DiplomacyStatus.Friendly;
            return DiplomacyStatus.Allied; // Scores 75 to 100 are Allied
        }
    }

    /// <summary>
    /// [HELPER STRUCT] Used as a key for the relationships dictionary.
    /// This ensures that the relationship between Faction A and Faction B is considered
    /// the same as Faction B and Faction A, regardless of the order they are passed in.
    /// </summary>
    public struct FactionPairKey : IEquatable<FactionPairKey>
    {
        public Faction Faction1;
        public Faction Faction2;

        public FactionPairKey(Faction f1, Faction f2)
        {
            // Ensure consistent internal ordering using GetInstanceID() for reliable hashing and comparison.
            // This prevents duplicate entries for (A,B) and (B,A).
            if (f1.GetInstanceID() < f2.GetInstanceID())
            {
                Faction1 = f1;
                Faction2 = f2;
            }
            else
            {
                Faction1 = f2;
                Faction2 = f1;
            }
        }

        public bool Equals(FactionPairKey other)
        {
            return (Faction1 == other.Faction1 && Faction2 == other.Faction2);
            // Due to our constructor's ordering, we don't need to check (F1==O2 && F2==O1)
        }

        public override bool Equals(object obj)
        {
            if (obj is FactionPairKey other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // A simple symmetric hash code using XOR.
            // This works because Faction1 and Faction2 are always consistently ordered.
            return Faction1.GetInstanceID() ^ Faction2.GetInstanceID();
        }
    }

    /// <summary>
    /// [SINGLETON / MANAGER] The core FactionDiplomacySystem.
    /// This MonoBehaviour manages all faction definitions and their relationships.
    /// It provides an API for other game systems to query and modify diplomatic states.
    /// This embodies the "Faction Diplomacy System" design pattern by centralizing
    /// all diplomacy logic and data.
    /// </summary>
    public class FactionDiplomacySystem : MonoBehaviour
    {
        // --- Singleton Instance ---
        public static FactionDiplomacySystem Instance { get; private set; }

        // --- Editor Configurable Data ---
        [Tooltip("List of all factions participating in diplomacy. Drag your Faction ScriptableObjects here.")]
        [SerializeField] private List<Faction> allFactions = new List<Faction>();

        [Tooltip("The initial diplomacy score for newly formed relationships.")]
        [SerializeField] private int initialDiplomacyScore = 0; // Default to Neutral

        // --- Internal Data Structures ---
        // Stores all relationships. FactionPairKey handles the symmetry (A,B same as B,A).
        private Dictionary<FactionPairKey, FactionRelationship> relationships;

        // --- Events ---
        /// <summary>
        /// Event fired when the diplomatic status between two factions changes.
        /// Parameters: (Faction faction1, Faction faction2, DiplomacyStatus newStatus)
        /// Other systems can subscribe to this to react to changes (e.g., UI updates, AI behavior changes).
        /// </summary>
        public event Action<Faction, Faction, DiplomacyStatus> OnRelationshipChanged;

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            // Enforce Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FactionDiplomacySystem already exists. Destroying duplicate.", this);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Persist across scene loads
                InitializeDiplomacySystem();
            }
        }

        /// <summary>
        /// Initializes the diplomacy system by creating all initial relationships.
        /// </summary>
        private void InitializeDiplomacySystem()
        {
            relationships = new Dictionary<FactionPairKey, FactionRelationship>();

            if (allFactions == null || allFactions.Count < 2)
            {
                Debug.LogWarning("FactionDiplomacySystem needs at least two Faction ScriptableObjects configured in 'All Factions' list to establish relationships.", this);
                return;
            }

            // Create initial neutral relationships between all unique pairs of factions
            for (int i = 0; i < allFactions.Count; i++)
            {
                for (int j = i + 1; j < allFactions.Count; j++) // Start j from i+1 to avoid self-relationships and duplicates
                {
                    Faction f1 = allFactions[i];
                    Faction f2 = allFactions[j];

                    FactionPairKey key = new FactionPairKey(f1, f2);
                    FactionRelationship newRelationship = new FactionRelationship(f1, f2, initialDiplomacyScore);
                    relationships.Add(key, newRelationship);

                    Debug.Log($"Initialized relationship between {f1.FactionName} and {f2.FactionName} to {newRelationship.CurrentStatus} (Score: {newRelationship.DiplomacyScore}).");
                }
            }
        }

        // --- Public API: Querying Relationships ---

        /// <summary>
        /// Gets the current diplomatic status between two specified factions.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <returns>The DiplomacyStatus between them. Returns Neutral if no relationship exists (should not happen if initialized correctly).</returns>
        public DiplomacyStatus GetDiplomacyStatus(Faction faction1, Faction faction2)
        {
            if (faction1 == faction2) return DiplomacyStatus.Allied; // A faction is always allied with itself.

            FactionPairKey key = new FactionPairKey(faction1, faction2);
            if (relationships.TryGetValue(key, out FactionRelationship relationship))
            {
                return relationship.CurrentStatus;
            }

            Debug.LogWarning($"Relationship between {faction1.FactionName} and {faction2.FactionName} not found. Returning Neutral.", this);
            return DiplomacyStatus.Neutral;
        }

        /// <summary>
        /// Gets the raw diplomacy score between two specified factions.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <returns>The diplomacy score. Returns 0 if no relationship exists.</returns>
        public int GetDiplomacyScore(Faction faction1, Faction faction2)
        {
            if (faction1 == faction2) return 100; // Max score for self-relationship

            FactionPairKey key = new FactionPairKey(faction1, faction2);
            if (relationships.TryGetValue(key, out FactionRelationship relationship))
            {
                return relationship.DiplomacyScore;
            }

            Debug.LogWarning($"Relationship between {faction1.FactionName} and {faction2.FactionName} not found. Returning 0.", this);
            return 0;
        }

        /// <summary>
        /// Gets all factions currently configured in the system.
        /// </summary>
        public IReadOnlyList<Faction> GetAllFactions()
        {
            return allFactions.AsReadOnly();
        }

        /// <summary>
        /// Gets all relationships where a specific faction is involved.
        /// </summary>
        /// <param name="faction">The faction to query for.</param>
        /// <returns>A list of FactionRelationship structs involving the given faction.</returns>
        public List<FactionRelationship> GetRelationshipsForFaction(Faction faction)
        {
            List<FactionRelationship> factionRelationships = new List<FactionRelationship>();
            foreach (var pair in relationships)
            {
                if (pair.Key.Faction1 == faction || pair.Key.Faction2 == faction)
                {
                    factionRelationships.Add(pair.Value);
                }
            }
            return factionRelationships;
        }

        // --- Public API: Modifying Relationships ---

        /// <summary>
        /// Adjusts the diplomacy score between two factions by a specified amount.
        /// The score is clamped between -100 and 100.
        /// Fires OnRelationshipChanged event if the status changes.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <param name="amount">The amount to add to the diplomacy score (can be negative).</param>
        public void AdjustDiplomacyScore(Faction faction1, Faction faction2, int amount)
        {
            if (faction1 == faction2) return; // Cannot adjust relationship with oneself

            FactionPairKey key = new FactionPairKey(faction1, faction2);

            if (relationships.TryGetValue(key, out FactionRelationship currentRelationship))
            {
                DiplomacyStatus oldStatus = currentRelationship.CurrentStatus;

                currentRelationship.DiplomacyScore = Mathf.Clamp(currentRelationship.DiplomacyScore + amount, -100, 100);
                relationships[key] = currentRelationship; // Update the struct in the dictionary

                if (currentRelationship.CurrentStatus != oldStatus)
                {
                    Debug.Log($"Diplomacy status changed between {faction1.FactionName} and {faction2.FactionName}: {oldStatus} -> {currentRelationship.CurrentStatus} (Score: {currentRelationship.DiplomacyScore})");
                    OnRelationshipChanged?.Invoke(faction1, faction2, currentRelationship.CurrentStatus);
                }
                else
                {
                    Debug.Log($"Diplomacy score adjusted between {faction1.FactionName} and {faction2.FactionName}: {currentRelationship.DiplomacyScore} (Status: {currentRelationship.CurrentStatus})");
                }
            }
            else
            {
                Debug.LogWarning($"Attempted to adjust relationship for non-existent pair: {faction1.FactionName} and {faction2.FactionName}.", this);
            }
        }

        /// <summary>
        /// Sets the diplomacy score between two factions to a specific value.
        /// Score is clamped between -100 and 100.
        /// Fires OnRelationshipChanged event if the status changes.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <param name="newScore">The new diplomacy score.</param>
        public void SetDiplomacyScore(Faction faction1, Faction faction2, int newScore)
        {
            if (faction1 == faction2) return;

            FactionPairKey key = new FactionPairKey(faction1, faction2);

            if (relationships.TryGetValue(key, out FactionRelationship currentRelationship))
            {
                DiplomacyStatus oldStatus = currentRelationship.CurrentStatus;

                currentRelationship.DiplomacyScore = Mathf.Clamp(newScore, -100, 100);
                relationships[key] = currentRelationship;

                if (currentRelationship.CurrentStatus != oldStatus)
                {
                    Debug.Log($"Diplomacy status set between {faction1.FactionName} and {faction2.FactionName}: {oldStatus} -> {currentRelationship.CurrentStatus} (Score: {currentRelationship.DiplomacyScore})");
                    OnRelationshipChanged?.Invoke(faction1, faction2, currentRelationship.CurrentStatus);
                }
                else
                {
                    Debug.Log($"Diplomacy score set between {faction1.FactionName} and {faction2.FactionName}: {currentRelationship.DiplomacyScore} (Status: {currentRelationship.CurrentStatus})");
                }
            }
            else
            {
                Debug.LogWarning($"Attempted to set diplomacy score for non-existent pair: {faction1.FactionName} and {faction2.FactionName}.", this);
            }
        }

        // --- High-Level Diplomatic Actions ---

        /// <summary>
        /// Declares war between two factions. Sets score to -100.
        /// </summary>
        /// <param name="aggressor">The faction initiating the war.</param>
        /// <param name="target">The faction being declared upon.</param>
        public void DeclareWar(Faction aggressor, Faction target)
        {
            if (GetDiplomacyStatus(aggressor, target) == DiplomacyStatus.AtWar)
            {
                Debug.Log($"{aggressor.FactionName} and {target.FactionName} are already at war.");
                return;
            }
            Debug.Log($"!!! {aggressor.FactionName} declares WAR on {target.FactionName} !!!");
            SetDiplomacyScore(aggressor, target, -100);
        }

        /// <summary>
        /// Offers peace between two factions, moving them to Neutral status.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        public void OfferPeace(Faction faction1, Faction faction2)
        {
            if (GetDiplomacyStatus(faction1, faction2) == DiplomacyStatus.Neutral)
            {
                Debug.Log($"{faction1.FactionName} and {faction2.FactionName} are already Neutral.");
                return;
            }
            Debug.Log($"--- {faction1.FactionName} and {faction2.FactionName} agree to PEACE ---");
            SetDiplomacyScore(faction1, faction2, 0); // Set to neutral score
        }

        /// <summary>
        /// Forms an alliance between two factions, setting score to 100.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        public void FormAlliance(Faction faction1, Faction faction2)
        {
            if (GetDiplomacyStatus(faction1, faction2) == DiplomacyStatus.Allied)
            {
                Debug.Log($"{faction1.FactionName} and {faction2.FactionName} are already Allied.");
                return;
            }
            Debug.Log($"*** {faction1.FactionName} and {faction2.FactionName} form an ALLIANCE ***");
            SetDiplomacyScore(faction1, faction2, 100); // Set to max friendly score
        }

        /// <summary>
        /// Simulates an event that improves relations between two factions.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <param name="value">The amount by which to improve relations.</param>
        public void ImproveRelations(Faction faction1, Faction faction2, int value = 10)
        {
            Debug.Log($"Improving relations between {faction1.FactionName} and {faction2.FactionName} by {value}.");
            AdjustDiplomacyScore(faction1, faction2, value);
        }

        /// <summary>
        /// Simulates an event that worsens relations between two factions.
        /// </summary>
        /// <param name="faction1">The first faction.</param>
        /// <param name="faction2">The second faction.</param>
        /// <param name="value">The amount by which to worsen relations.</param>
        public void WorsenRelations(Faction faction1, Faction faction2, int value = 10)
        {
            Debug.Log($"Worsening relations between {faction1.FactionName} and {faction2.FactionName} by {value}.");
            AdjustDiplomacyScore(faction1, faction2, -value);
        }

        // --- Debugging / Example Usage ---
        [ContextMenu("Log All Faction Relationships")]
        public void LogAllRelationships()
        {
            Debug.Log("--- Current Faction Relationships ---");
            if (relationships == null || relationships.Count == 0)
            {
                Debug.Log("No relationships initialized. Ensure factions are assigned and system is awake.");
                return;
            }

            foreach (var entry in relationships)
            {
                FactionRelationship rel = entry.Value;
                Debug.Log($"[Diplomacy] {rel.FactionA.FactionName} <-> {rel.FactionB.FactionName}: Status = {rel.CurrentStatus} (Score: {rel.DiplomacyScore})");
            }
            Debug.Log("------------------------------------");
        }
    }

    /// <summary>
    /// [EXAMPLE USAGE] This class demonstrates how other systems would interact with the FactionDiplomacySystem.
    /// Attach this to a GameObject alongside the FactionDiplomacySystem to see it in action.
    /// </summary>
    public class DiplomacyExampleController : MonoBehaviour
    {
        [Header("Factions for Example Interaction")]
        public Faction FactionA;
        public Faction FactionB;
        public Faction FactionC;

        private void Start()
        {
            if (FactionDiplomacySystem.Instance == null)
            {
                Debug.LogError("FactionDiplomacySystem instance not found. Make sure it's present in the scene and initialized.");
                return;
            }

            // Subscribe to the relationship changed event
            FactionDiplomacySystem.Instance.OnRelationshipChanged += HandleRelationshipChange;

            Debug.Log("--- Diplomacy Example Started ---");

            // --- Example Scenarios ---
            if (FactionA != null && FactionB != null)
            {
                Debug.Log($"Initial status {FactionA.FactionName} <-> {FactionB.FactionName}: {FactionDiplomacySystem.Instance.GetDiplomacyStatus(FactionA, FactionB)}");

                // Scenario 1: Improve relations
                Debug.Log("\n--- Scenario 1: Improve relations between A and B ---");
                FactionDiplomacySystem.Instance.ImproveRelations(FactionA, FactionB, 30); // Should become Friendly if Neutral
                FactionDiplomacySystem.Instance.ImproveRelations(FactionA, FactionB, 30); // Should become Allied if Friendly

                // Scenario 2: Worsen relations
                Debug.Log("\n--- Scenario 2: Worsen relations between A and B ---");
                FactionDiplomacySystem.Instance.WorsenRelations(FactionA, FactionB, 40); // Should drop from Allied/Friendly
                FactionDiplomacySystem.Instance.WorsenRelations(FactionA, FactionB, 40); // Should become Hostile/AtWar

                // Scenario 3: Declare war
                Debug.Log("\n--- Scenario 3: Declare War ---");
                FactionDiplomacySystem.Instance.DeclareWar(FactionA, FactionB);

                // Scenario 4: Offer Peace
                Debug.Log("\n--- Scenario 4: Offer Peace ---");
                FactionDiplomacySystem.Instance.OfferPeace(FactionA, FactionB);

                // Scenario 5: Form Alliance (requires a third faction for a more interesting example)
                if (FactionC != null)
                {
                    Debug.Log($"\n--- Scenario 5: {FactionA.FactionName} forms Alliance with {FactionC.FactionName} ---");
                    Debug.Log($"Initial status {FactionA.FactionName} <-> {FactionC.FactionName}: {FactionDiplomacySystem.Instance.GetDiplomacyStatus(FactionA, FactionC)}");
                    FactionDiplomacySystem.Instance.FormAlliance(FactionA, FactionC);
                }
            }
            else
            {
                Debug.LogWarning("Please assign Faction A, B (and C) ScriptableObjects in the Inspector for the example to run.", this);
            }

            Debug.Log("\n--- Example scenarios complete. Check console for logs. ---");
            FactionDiplomacySystem.Instance.LogAllRelationships();
        }

        private void OnDestroy()
        {
            // Unsubscribe from the event to prevent memory leaks when this object is destroyed
            if (FactionDiplomacySystem.Instance != null)
            {
                FactionDiplomacySystem.Instance.OnRelationshipChanged -= HandleRelationshipChange;
            }
        }

        /// <summary>
        /// Event handler for relationship changes. This is where other systems would react.
        /// </summary>
        private void HandleRelationshipChange(Faction faction1, Faction faction2, DiplomacyStatus newStatus)
        {
            Debug.Log($"[Event Listener] Relationship between {faction1.FactionName} and {faction2.FactionName} changed to: {newStatus}");

            // Example reactions:
            // - Update UI elements showing faction relations
            // - Trigger AI behavior changes (e.g., cease fire, attack, send trade envoy)
            // - Spawn special events (e.g., border skirmishes if hostile, joint ventures if allied)
            // - Play sound effects or visual cues
        }
    }
}
```

## How to use this Faction Diplomacy System in Unity:

1.  **Create C# Scripts:**
    *   Save the entire code block above into a single C# file named `FactionDiplomacySystem.cs` (or two files if you prefer `Faction.cs` separate from the main system, but for "complete, working script" one file is fine).
    *   Make sure it's inside your Unity project's `Assets` folder (e.g., `Assets/Scripts/GameSystems/Diplomacy/FactionDiplomacySystem.cs`).

2.  **Create Faction ScriptableObjects:**
    *   In the Unity Editor, right-click in the Project window -> `Create` -> `Game Systems/Diplomacy` -> `Faction`.
    *   Create at least three `Faction` assets (e.g., "HumanEmpire", "OrcHorde", "ElvenKingdom").
    *   Select each Faction asset and customize its `Faction Name` and `Faction Color` in the Inspector.

3.  **Setup the `FactionDiplomacySystem` in your Scene:**
    *   Create an empty GameObject in your scene (e.g., named "GameManagers").
    *   Add the `FactionDiplomacySystem` component to this GameObject.
    *   In the Inspector, for the `FactionDiplomacySystem` component:
        *   Locate the "All Factions" list.
        *   Drag and drop your created `Faction` ScriptableObjects (HumanEmpire, OrcHorde, ElvenKingdom) into this list. Make sure there are at least two.

4.  **Setup the `DiplomacyExampleController` (for demonstration):**
    *   Add the `DiplomacyExampleController` component to the *same* GameObject as the `FactionDiplomacySystem` (or a separate GameObject).
    *   In the Inspector, for the `DiplomacyExampleController` component:
        *   Drag and drop your `Faction` ScriptableObjects into the `Faction A`, `Faction B`, and `Faction C` fields to define which factions the example will interact with.

5.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Observe the Console window. You'll see:
        *   Initialization logs for all faction relationships.
        *   Logs from `DiplomacyExampleController` demonstrating various diplomatic actions (improving, worsening relations, declaring war, peace, alliance).
        *   Logs from the `OnRelationshipChanged` event handler, showing how other systems can react to diplomatic shifts.
        *   A final log of all current relationships.

## Explanation of the Faction Diplomacy System Pattern:

This example showcases a common "Manager" or "System" design pattern, specifically tailored for managing diplomatic relationships.

*   **Centralized Control (`FactionDiplomacySystem`):**
    *   Instead of each faction managing its own relationships, a single `FactionDiplomacySystem` MonoBehaviour acts as the central authority. This prevents redundant logic, ensures data consistency, and simplifies queries.
    *   It's implemented as a Singleton (`Instance`) for easy global access from any part of your game.
*   **Data-Driven Factions (`Faction` ScriptableObject):**
    *   `Faction` is a `ScriptableObject`, making factions reusable data assets. Designers can create and configure new factions directly in the Unity Editor without touching code.
    *   This separates data (faction definition) from logic (diplomacy management).
*   **Relationship Representation (`FactionRelationship` struct):**
    *   A simple `struct` encapsulates the core data of a relationship: the two involved factions and a `DiplomacyScore`.
    *   It also provides a derived `CurrentStatus` (enum) based on the score, making it easy to understand the relationship state.
*   **Symmetric Relationship Key (`FactionPairKey` struct):**
    *   A custom `FactionPairKey` is used as the key in the internal `relationships` Dictionary. This is crucial for correctly representing symmetric relationships (A's relationship with B is the same as B's with A). It ensures that `(FactionA, FactionB)` and `(FactionB, FactionA)` map to the same entry.
*   **Clear API:**
    *   The `FactionDiplomacySystem` exposes public methods (`GetDiplomacyStatus`, `AdjustDiplomacyScore`, `DeclareWar`, `OfferPeace`, `FormAlliance`, etc.) that provide a clean, high-level interface for other game systems to interact with diplomacy.
*   **Event-Driven Reactions:**
    *   The `OnRelationshipChanged` event allows other systems (UI, AI, story events) to subscribe and react automatically when diplomatic statuses change, promoting a decoupled architecture. Instead of continuously polling for changes, systems are notified when something relevant happens.

This pattern makes your game's diplomacy robust, scalable, and easy to debug, as all related logic and data reside in a well-defined and accessible system.