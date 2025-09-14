// Unity Design Pattern Example: FactionsSystem
// This script demonstrates the FactionsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The FactionsSystem design pattern is crucial in many games for managing relationships between different groups of entities (factions). It dictates how entities from one faction interact with entities from another (e.g., hostile, friendly, neutral).

This example provides a complete, practical implementation in C# for Unity, following best practices and including detailed explanations.

---

### **FactionsSystem Design Pattern Explained**

**Core Idea:**
To establish a centralized, easily configurable system that determines the relationship between any two defined factions in a game. This allows game logic (like AI behavior, spell targeting, or quest progression) to query these relationships rather than hardcoding them.

**Key Components:**

1.  **`FactionDefinition` (ScriptableObject):**
    *   Represents a distinct group (e.g., "Player", "Enemy", "Townsfolk", "Orcs").
    *   Being a `ScriptableObject`, designers can create these as assets in the Unity Editor, making the system data-driven and flexible.

2.  **`FactionRelationshipType` (Enum):**
    *   Defines the possible states of interaction between factions (e.g., `Hostile`, `Neutral`, `Friendly`, `Allied`). This enum can be expanded based on game needs.

3.  **`FactionsSystemConfig` (ScriptableObject):**
    *   A central configuration asset that holds all initial, default relationships between factions.
    *   It's loaded by the `FactionsSystem` static class, making it a "global settings" object.

4.  **`FactionsSystem` (Static Class):**
    *   The core manager of the system.
    *   It provides the public API to query (`GetRelationship`) and dynamically change (`SetRelationship`) faction relationships at runtime.
    *   It internally uses a dictionary for efficient lookup of relationships.
    *   It initializes itself by loading the `FactionsSystemConfig`.

5.  **`IFactionMember` (Interface):**
    *   An interface that any game entity (player, NPC, monster, even a destructible object) can implement to declare its faction.

6.  **`FactionMember` (MonoBehaviour Component):**
    *   A concrete implementation of `IFactionMember` that can be attached to any `GameObject`. Designers simply assign a `FactionDefinition` asset to this component in the Inspector.

---

### **Unity Project Setup Instructions**

To use this FactionsSystem in your Unity project:

1.  **Create Folders:** Create a new folder (e.g., `Assets/FactionsSystem`) and place all the following C# scripts inside it.

2.  **Create FactionDefinition Assets:**
    *   In the Project window, right-click -> Create -> Factions System -> Faction Definition.
    *   Create at least three: `PlayerFaction`, `EnemyFaction`, `NeutralFaction`.
    *   Select each asset and give it a unique `DisplayName` (e.g., "Player Faction", "Enemy Horde", "Neutral Civilians").

3.  **Create FactionsSystemConfig Asset:**
    *   Right-click in the Project window -> Create -> Factions System -> Factions System Config.
    *   **Crucially:** Rename this asset to **`FactionsSystemConfig`** and place it inside a `Resources` folder (e.g., `Assets/Resources/FactionsSystemConfig.asset`). The `FactionsSystem` static class expects to find it there.
    *   **Configure Initial Relationships:**
        *   Select the `FactionsSystemConfig` asset.
        *   Set a `DefaultRelationship` (e.g., `Neutral`). This is used if no specific relationship is defined.
        *   Expand "Initial Faction Relationships".
        *   Add entries by clicking the '+' button:
            *   **Entry 0:** `Faction A = PlayerFaction`, `Faction B = EnemyFaction`, `Relationship = Hostile`
            *   **Entry 1:** `Faction A = PlayerFaction`, `Faction B = NeutralFaction`, `Relationship = Friendly`
            *   **Entry 2:** `Faction A = EnemyFaction`, `Faction B = NeutralFaction`, `Relationship = Hostile`
            *   (Add more as needed for your game factions, e.g., "Orcs" vs "Goblins")

4.  **Create GameObjects with `FactionMember` Components:**
    *   In your scene, create an empty GameObject, rename it "PlayerCharacter".
    *   Add the `FactionMember` component to it (Add Component -> Factions System -> Faction Member).
    *   Assign your `PlayerFaction` asset to its `_faction` field in the Inspector.
    *   Repeat for "EnemyCharacter" (assign `EnemyFaction`) and "NeutralNPC" (assign `NeutralFaction`).

5.  **Create and Configure `FactionInteractionDemo`:**
    *   Create an empty GameObject in your scene, rename it "FactionsDemo".
    *   Attach the `FactionInteractionDemo.cs` script to it.
    *   In the Inspector, drag your "PlayerCharacter" GameObject to `My Faction Member`.
    *   Drag your "EnemyCharacter" GameObject to `Other Faction Member`.
    *   For the runtime change demo, assign `factionForChangeA` (e.g., `NeutralFaction`) and `factionForChangeB` (e.g., `EnemyFaction`) from your Project window. Set `New Relationship Type` (e.g., `Friendly`).

6.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe the console output to see how the FactionsSystem initializes, queries relationships, changes them at runtime, and then resets them.

---

### **C# Scripts**

```csharp
// 1. FactionDefinition.cs
using UnityEngine;

/// <summary>
/// Represents a specific faction in the game.
/// This is a ScriptableObject, allowing designers to create and configure factions as assets.
/// Each FactionDefinition asset defines a unique faction (e.g., "Player", "Orcs", "Goblins").
/// </summary>
[CreateAssetMenu(fileName = "NewFaction", menuName = "Factions System/Faction Definition")]
public class FactionDefinition : ScriptableObject
{
    [Tooltip("A unique identifier for this faction (derived from asset name).")]
    // Using the asset's name as a unique ID. It's good practice for ScriptableObjects to have unique names.
    public string FactionID => name; 

    [Tooltip("A human-readable display name for the faction.")]
    public string DisplayName = "New Faction";

    /// <summary>
    /// Overrides ToString for better debugging output, showing the display name.
    /// </summary>
    public override string ToString()
    {
        return DisplayName;
    }

    // You can extend this class with more properties relevant to a faction, such as:
    // - Faction color
    // - Default behavior flags for NPCs within this faction
    // - An icon for UI display
}
```

```csharp
// 2. FactionRelationshipType.cs
/// <summary>
/// Defines the possible types of relationships between two factions.
/// This enum dictates how entities belonging to these factions should interact.
/// </summary>
public enum FactionRelationshipType
{
    Friendly,   // Entities will assist each other, no aggression.
    Neutral,    // Entities will ignore each other unless provoked.
    Hostile,    // Entities will actively seek to attack each other.
    Allied      // A stronger form of Friendly, implying active cooperation and shared goals.
}
```

```csharp
// 3. FactionRelationshipEntry.cs
using UnityEngine;
using System; // Required for [Serializable]

/// <summary>
/// A serializable struct used for defining a relationship between two specific factions.
/// This struct is primarily used in the FactionsSystemConfig ScriptableObject
/// to allow designers to set up initial relationships directly in the Unity Editor Inspector.
/// </summary>
[Serializable]
public struct FactionRelationshipEntry
{
    [Tooltip("The first faction in the relationship pair.")]
    public FactionDefinition FactionA;

    [Tooltip("The second faction in the relationship pair.")]
    public FactionDefinition FactionB;

    [Tooltip("The type of relationship between Faction A and Faction B.")]
    public FactionRelationshipType Relationship;

    /// <summary>
    /// Provides a readable string representation for debugging and editor display.
    /// </summary>
    public override string ToString()
    {
        string factionAName = FactionA != null ? FactionA.name : "NULL";
        string factionBName = FactionB != null ? FactionB.name : "NULL";
        return $"{factionAName} <-> {factionBName} : {Relationship}";
    }
}
```

```csharp
// 4. FactionsSystemConfig.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This ScriptableObject acts as the global configuration for the Factions System.
/// It stores the default relationship type and a list of all initial, predefined
/// relationships between factions in the game.
/// It's designed to be a singleton-like asset that the static FactionsSystem class loads
/// from a Resources folder.
/// </summary>
[CreateAssetMenu(fileName = "FactionsSystemConfig", menuName = "Factions System/Factions System Config")]
public class FactionsSystemConfig : ScriptableObject
{
    [Header("Default Relationship")]
    [Tooltip("The relationship type used when no specific relationship is defined between two factions.")]
    public FactionRelationshipType DefaultRelationship = FactionRelationshipType.Neutral;

    [Header("Initial Faction Relationships")]
    [Tooltip("Define the initial relationships between different factions. " +
             "These will be loaded at game start. Relationships are assumed symmetrical.")]
    public List<FactionRelationshipEntry> InitialRelationships = new List<FactionRelationshipEntry>();

    // Static instance for easy access from the FactionsSystem static class.
    // This allows the system to load its configuration from a specific asset.
    private static FactionsSystemConfig _instance;
    public static FactionsSystemConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load the configuration asset from the Resources folder.
                // It must be named "FactionsSystemConfig" and reside in any "Resources" folder.
                _instance = Resources.Load<FactionsSystemConfig>("FactionsSystemConfig");

                // If not found, log an error to guide the developer.
                if (_instance == null)
                {
                    Debug.LogError("FactionsSystemConfig.asset not found in any Resources folder! " +
                                   "Please create one via 'Create -> Factions System -> Factions System Config' " +
                                   "and name it 'FactionsSystemConfig.asset', placing it in 'Assets/Resources/'.");
                }
            }
            return _instance;
        }
    }
}
```

```csharp
// 5. FactionsSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System; // Required for ValueTuple

/// <summary>
/// The central static class for the Factions System. This is the main API
/// for game logic to interact with faction relationships.
/// It manages loading initial relationships from FactionsSystemConfig and
/// provides methods to query and dynamically change relationships at runtime.
/// </summary>
public static class FactionsSystem
{
    // A dictionary to store runtime relationships.
    // The key is a ValueTuple of two FactionDefinitions. We store relationships
    // in both (FactionA, FactionB) and (FactionB, FactionA) directions for symmetric
    // and efficient lookups, avoiding the need to normalize the key during queries.
    private static Dictionary<ValueTuple<FactionDefinition, FactionDefinition>, FactionRelationshipType> _runtimeRelationships;

    // A flag to ensure the system is initialized only once.
    private static bool _isInitialized = false;

    /// <summary>
    /// Initializes the FactionsSystem by loading the configuration and setting up initial relationships.
    /// This method is idempotent (safe to call multiple times) and should be called
    /// once at game start, or it will be lazily initialized on the first relationship query.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return; // Prevent re-initialization

        Debug.Log("[FactionsSystem] Initializing...");

        _runtimeRelationships = new Dictionary<ValueTuple<FactionDefinition, FactionDefinition>, FactionRelationshipType>();
        FactionsSystemConfig config = FactionsSystemConfig.Instance; // Access the singleton config

        if (config == null)
        {
            Debug.LogError("[FactionsSystem] Failed to load FactionsSystemConfig. Relationships will not be initialized.");
            return;
        }

        // Process all initial relationships defined in the FactionsSystemConfig.
        foreach (var entry in config.InitialRelationships)
        {
            if (entry.FactionA == null || entry.FactionB == null)
            {
                Debug.LogWarning($"[FactionsSystem] Skipping malformed relationship entry (missing FactionDefinition). Entry: {entry}");
                continue;
            }
            
            // Add the relationship to the runtime dictionary in both directions
            // to ensure symmetric lookups later without extra processing.
            SetRelationshipInternal(entry.FactionA, entry.FactionB, entry.Relationship);
            SetRelationshipInternal(entry.FactionB, entry.FactionA, entry.Relationship);
        }

        _isInitialized = true;
        Debug.Log($"[FactionsSystem] Initialized with {_runtimeRelationships.Count / 2} unique relationship entries.");
    }

    /// <summary>
    /// Retrieves the relationship type between two specified factions.
    /// If no explicit relationship is defined, the DefaultRelationship from FactionsSystemConfig is returned.
    /// </summary>
    /// <param name="factionA">The first faction.</param>
    /// <param name="factionB">The second faction.</param>
    /// <returns>The resolved FactionRelationshipType between the two factions.</returns>
    public static FactionRelationshipType GetRelationship(FactionDefinition factionA, FactionDefinition factionB)
    {
        // Lazy initialization: if not yet initialized, do it now.
        if (!_isInitialized) Initialize();

        if (factionA == null || factionB == null)
        {
            Debug.LogError("[FactionsSystem] Cannot get relationship: One or both FactionDefinitions are null.");
            // Return a safe default for invalid input.
            return FactionsSystemConfig.Instance != null ? FactionsSystemConfig.Instance.DefaultRelationship : FactionRelationshipType.Neutral;
        }

        // A faction is always considered Friendly with itself.
        if (factionA == factionB)
        {
            return FactionRelationshipType.Friendly;
        }

        // Create the key for dictionary lookup.
        ValueTuple<FactionDefinition, FactionDefinition> key = (factionA, factionB);

        // Try to get the relationship from the runtime dictionary.
        if (_runtimeRelationships.TryGetValue(key, out FactionRelationshipType relationship))
        {
            return relationship;
        }

        // If no specific relationship is found, return the default relationship from config.
        return FactionsSystemConfig.Instance != null ? FactionsSystemConfig.Instance.DefaultRelationship : FactionRelationshipType.Neutral;
    }

    /// <summary>
    /// Sets or updates the relationship between two factions at runtime.
    /// This change is temporary for the current game session and does not
    /// modify the FactionsSystemConfig ScriptableObject asset.
    /// </summary>
    /// <param name="factionA">The first faction.</param>
    /// <param name="factionB">The second faction.</param>
    /// <param name="relationshipType">The new relationship type to establish.</param>
    public static void SetRelationship(FactionDefinition factionA, FactionDefinition factionB, FactionRelationshipType relationshipType)
    {
        // Lazy initialization.
        if (!_isInitialized) Initialize();

        if (factionA == null || factionB == null)
        {
            Debug.LogError("[FactionsSystem] Cannot set relationship: One or both FactionDefinitions are null.");
            return;
        }

        // Set the relationship in both directions to maintain symmetry in the dictionary.
        SetRelationshipInternal(factionA, factionB, relationshipType);
        SetRelationshipInternal(factionB, factionA, relationshipType);
        Debug.Log($"[FactionsSystem] Runtime: Relationship between {factionA.DisplayName} and {factionB.DisplayName} set to {relationshipType}.");
    }

    /// <summary>
    /// Internal helper method to set a single-direction relationship in the dictionary.
    /// This is used by Initialize and SetRelationship to ensure consistency.
    /// </summary>
    private static void SetRelationshipInternal(FactionDefinition factionA, FactionDefinition factionB, FactionRelationshipType relationshipType)
    {
        ValueTuple<FactionDefinition, FactionDefinition> key = (factionA, factionB);
        _runtimeRelationships[key] = relationshipType;
    }

    /// <summary>
    /// Resets all runtime relationships to their initial state as defined in the FactionsSystemConfig.
    /// This effectively reloads the configuration and discards any runtime changes.
    /// </summary>
    public static void ResetRelationships()
    {
        Debug.Log("[FactionsSystem] Resetting all runtime relationships...");
        _isInitialized = false; // Force re-initialization on next access
        Initialize(); // Re-initialize to reload from config
        Debug.Log("[FactionsSystem] Relationships reset to initial configuration.");
    }
}
```

```csharp
// 6. IFactionMember.cs
using UnityEngine;

/// <summary>
/// Interface for any game object that belongs to a faction.
/// Entities like player characters, NPCs, enemies, or even interactive environmental
/// objects can implement this interface to participate in the FactionsSystem.
/// </summary>
public interface IFactionMember
{
    /// <summary>
    /// Gets the FactionDefinition that this member belongs to.
    /// </summary>
    /// <returns>The FactionDefinition of this member.</returns>
    FactionDefinition GetFaction();
}
```

```csharp
// 7. FactionMember.cs
using UnityEngine;

/// <summary>
/// A MonoBehaviour component that assigns a FactionDefinition to a GameObject.
/// Attaching this component to a GameObject makes it a part of the FactionsSystem,
/// allowing its faction to be queried by other entities or game logic.
/// </summary>
[AddComponentMenu("Factions System/Faction Member")]
public class FactionMember : MonoBehaviour, IFactionMember
{
    [Tooltip("The faction this GameObject belongs to.")]
    [SerializeField] // Makes a private field editable in the Inspector.
    private FactionDefinition _faction;

    /// <summary>
    /// Returns the FactionDefinition of this member.
    /// Implements the IFactionMember interface.
    /// </summary>
    public FactionDefinition GetFaction()
    {
        if (_faction == null)
        {
            Debug.LogError($"FactionMember on '{gameObject.name}' has no FactionDefinition assigned!", this);
        }
        return _faction;
    }

    /// <summary>
    /// Allows setting the faction dynamically at runtime if the entity changes allegiance.
    /// </summary>
    /// <param name="newFaction">The new FactionDefinition for this member.</param>
    public void SetFaction(FactionDefinition newFaction)
    {
        _faction = newFaction;
        Debug.Log($"{gameObject.name} has changed faction to: {newFaction.DisplayName}");
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// Useful for validating assigned fields and providing warnings.
    /// </summary>
    private void OnValidate()
    {
        if (_faction == null)
        {
            // Optionally, provide a warning in the Unity Editor if no faction is assigned.
            // Debug.LogWarning($"FactionMember on '{gameObject.name}' has no FactionDefinition assigned!", this);
        }
    }
}
```

```csharp
// 8. FactionInteractionDemo.cs
using UnityEngine;

/// <summary>
/// A demonstration script to showcase how to use the FactionsSystem in a Unity project.
/// Attach this to an empty GameObject in your scene to see the system in action.
/// Remember to set up FactionDefinition assets and a FactionsSystemConfig asset first.
/// </summary>
public class FactionInteractionDemo : MonoBehaviour
{
    [Header("Demo Faction Members (from scene)")]
    [Tooltip("Reference to a GameObject with a FactionMember component (e.g., PlayerCharacter).")]
    public FactionMember myFactionMember;

    [Tooltip("Reference to another GameObject with a FactionMember component (e.g., EnemyCharacter).")]
    public FactionMember otherFactionMember;

    [Header("Runtime Relationship Change Demo (using assets)")]
    [Tooltip("Faction A asset for demonstrating a runtime relationship change.")]
    public FactionDefinition factionForChangeA;
    [Tooltip("Faction B asset for demonstrating a runtime relationship change.")]
    public FactionDefinition factionForChangeB;
    [Tooltip("The new relationship type to set between Faction A and Faction B.")]
    public FactionRelationshipType newRelationshipType = FactionRelationshipType.Hostile;

    void Start()
    {
        // It's good practice to explicitly initialize the FactionsSystem once at the start
        // of your game, although it also initializes lazily on its first access.
        FactionsSystem.Initialize();

        Debug.Log("--- FactionsSystem Demo Start ---");

        if (myFactionMember == null || otherFactionMember == null)
        {
            Debug.LogError("Please assign 'My Faction Member' and 'Other Faction Member' in the Inspector " +
                           "for the demo to properly function.", this);
            return;
        }

        // --- Demo 1: Checking Initial Relationships ---
        Debug.Log("\n--- Demo 1: Checking Initial Relationships ---");
        CheckRelationship(myFactionMember, otherFactionMember);

        // --- Demo 2: Demonstrating Runtime Relationship Change ---
        Debug.Log("\n--- Demo 2: Changing Relationships at Runtime ---");
        if (factionForChangeA != null && factionForChangeB != null)
        {
            Debug.Log($"Attempting to change relationship between {factionForChangeA.DisplayName} " +
                      $"and {factionForChangeB.DisplayName} to {newRelationshipType}...");
            
            // This is how you would dynamically change a relationship in game.
            FactionsSystem.SetRelationship(factionForChangeA, factionForChangeB, newRelationshipType);
            
            Debug.Log($"New relationship after change: {FactionsSystem.GetRelationship(factionForChangeA, factionForChangeB)}");
        }
        else
        {
            Debug.LogWarning("Faction definitions for runtime change not assigned. Skipping this part of Demo 2.");
        }

        // --- Demo 3: Simulating Game Logic based on Relationship ---
        Debug.Log("\n--- Demo 3: Game Logic based on Relationship ---");
        
        // Example: An AI entity deciding its action based on relationship with another.
        FactionDefinition myFaction = myFactionMember.GetFaction();
        FactionDefinition otherFaction = otherFactionMember.GetFaction();

        if (myFaction != null && otherFaction != null)
        {
            FactionRelationshipType relationship = FactionsSystem.GetRelationship(myFaction, otherFaction);
            Debug.Log($"Current relationship between {myFaction.DisplayName} and {otherFaction.DisplayName}: {relationship}");

            // Example game decision based on the determined relationship:
            switch (relationship)
            {
                case FactionRelationshipType.Hostile:
                    Debug.Log($"ACTION: {myFactionMember.gameObject.name} ({myFaction.DisplayName}) will attack {otherFactionMember.gameObject.name} ({otherFaction.DisplayName})!");
                    // Trigger attack animation, AI combat state, etc.
                    break;
                case FactionRelationshipType.Friendly:
                case FactionRelationshipType.Allied:
                    Debug.Log($"ACTION: {myFactionMember.gameObject.name} ({myFaction.DisplayName}) will assist {otherFactionMember.gameObject.name} ({otherFaction.DisplayName})!");
                    // Trigger healing, support spell, follow behavior, etc.
                    break;
                case FactionRelationshipType.Neutral:
                    Debug.Log($"ACTION: {myFactionMember.gameObject.name} ({myFaction.DisplayName}) will ignore {otherFactionMember.gameObject.name} ({otherFaction.DisplayName}).");
                    // Trigger ignore behavior, pathfinding around, etc.
                    break;
            }
        }


        // --- Demo 4: Resetting Relationships to Initial State ---
        Debug.Log("\n--- Demo 4: Resetting Relationships ---");
        FactionsSystem.ResetRelationships(); // All runtime changes are reverted.

        // Verify reset state for the factions we changed earlier.
        if (factionForChangeA != null && factionForChangeB != null)
        {
            Debug.Log($"Relationship between {factionForChangeA.DisplayName} and {factionForChangeB.DisplayName} " +
                      $"after reset: {FactionsSystem.GetRelationship(factionForChangeA, factionForChangeB)} (should be initial config)");
        }
        // Verify original demo relationship is also reset
        CheckRelationship(myFactionMember, otherFactionMember);


        Debug.Log("--- FactionsSystem Demo End ---");
    }

    /// <summary>
    /// Helper method to demonstrate how to query and log a relationship between two IFactionMembers.
    /// This pattern would be used by AI, targeting systems, etc.
    /// </summary>
    private void CheckRelationship(IFactionMember memberA, IFactionMember memberB)
    {
        FactionDefinition factionA = memberA.GetFaction();
        FactionDefinition factionB = memberB.GetFaction();

        if (factionA == null || factionB == null)
        {
            Debug.LogError($"Cannot check relationship: one or both members have no assigned faction. " +
                           $"Member A: '{memberA.GetType().Name}', Member B: '{memberB.GetType().Name}'");
            return;
        }

        FactionRelationshipType relationship = FactionsSystem.GetRelationship(factionA, factionB);
        Debug.Log($"Relationship between '{memberA.gameObject.name}' ({factionA.DisplayName}) and " +
                  $"'{memberB.gameObject.name}' ({factionB.DisplayName}) is: {relationship}");
    }


    /*
    // Example of how you might use this in a continuous update loop (e.g., for AI decisions).
    // Uncomment this method to see continuous checks.
    private void Update()
    {
        if (myFactionMember != null && otherFactionMember != null)
        {
            FactionDefinition myFaction = myFactionMember.GetFaction();
            FactionDefinition targetFaction = otherFactionMember.GetFaction();

            if (myFaction != null && targetFaction != null)
            {
                FactionRelationshipType relationship = FactionsSystem.GetRelationship(myFaction, targetFaction);

                // Perform actions based on the relationship
                if (relationship == FactionRelationshipType.Hostile)
                {
                    // Debug.Log($"{myFactionMember.gameObject.name} is hostile towards {otherFactionMember.gameObject.name}!");
                    // Trigger attack behavior
                }
                else if (relationship == FactionRelationshipType.Friendly || relationship == FactionRelationshipType.Allied)
                {
                    // Debug.Log($"{myFactionMember.gameObject.name} is friendly/allied with {otherFactionMember.gameObject.name}.");
                    // Trigger support behavior
                }
                else // Neutral
                {
                    // Debug.Log($"{myFactionMember.gameObject.name} is neutral towards {otherFactionMember.gameObject.name}.");
                    // Trigger ignore/passive behavior
                }
            }
        }
    }
    */
}
```