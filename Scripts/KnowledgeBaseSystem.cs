// Unity Design Pattern Example: KnowledgeBaseSystem
// This script demonstrates the KnowledgeBaseSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Knowledge Base System** design pattern in Unity.

**Concept:**
A Knowledge Base System is designed to store, manage, and reason with a collection of knowledge. It typically consists of:
1.  **Knowledge Representation:** How knowledge is structured and stored (facts, rules, concepts).
2.  **Inference Engine:** A mechanism to deduce new knowledge or make decisions based on existing knowledge and rules.
3.  **Working Memory:** The current state or set of facts relevant to the ongoing reasoning process.

**Real-world Use Case in Unity:**
We'll implement an **NPC Behavior Decision System**.
*   **Facts:** Conditions about the game world (e.g., "playerIsNear", "npcHealthLow", "hasTarget").
*   **Rules:** Define how these facts lead to specific NPC behaviors (e.g., "IF playerIsNear AND npcHasAmmo THEN behavior_attack").
*   **Inference:** The system will process rules based on current facts to determine the NPC's next action.

---

### Instructions to Use in Unity:

1.  **Create a Folder:** In your Unity Project window, create a folder named `KnowledgeBaseSystem`.
2.  **Create C# Scripts:**
    *   Create a C# script named `KnowledgeFact.cs` and paste the content from the "KnowledgeFact.cs" section below.
    *   Create a C# script named `KnowledgeRule.cs` and paste the content from the "KnowledgeRule.cs" section below.
    *   Create a C# script named `KnowledgeBaseSystem.cs` and paste the content from the "KnowledgeBaseSystem.cs" section below.
3.  **Create Knowledge Assets:**
    *   In your Project window, right-click -> `Create` -> `KnowledgeBaseSystem` -> `Knowledge Fact`. Name it something like `Fact_PlayerNear`. Set its `Key` to "playerIsNear" and `Value` to "true". Repeat for other facts you need (e.g., `Fact_NPCHasAmmo`, `Fact_NPCHealthLow`, `Fact_CanSeePlayer`, `Fact_CoverAvailable`).
    *   Right-click -> `Create` -> `KnowledgeBaseSystem` -> `Knowledge Rule`. Name it `Rule_AttackPlayer`.
4.  **Configure Rules:**
    *   Select `Rule_AttackPlayer`.
    *   In its Inspector, expand `Antecedent Facts` and add two elements:
        *   Element 0: `Key = playerIsNear`, `Value = true`
        *   Element 1: `Key = npcHasAmmo`, `Value = true`
    *   Expand `Consequent Facts` and add one element:
        *   Element 0: `Key = npcBehavior`, `Value = Attack` (You can create a `KnowledgeFact` asset for this, or just input the key/value directly here as it creates transient facts).
    *   Create more rules similarly (e.g., `Rule_Flee`, `Rule_SeekCover`).
        *   `Rule_Flee`: Antecedent: `playerIsNear=true`, `npcHasAmmo=false`. Consequent: `npcBehavior=Flee`.
        *   `Rule_SeekCover`: Antecedent: `npcHealthLow=true`, `coverAvailable=true`. Consequent: `npcBehavior=SeekCover`.
5.  **Add `KnowledgeBaseSystem` to a GameObject:**
    *   Create an empty GameObject in your scene (e.g., named `GameManager`).
    *   Add the `KnowledgeBaseSystem.cs` component to this GameObject.
6.  **Assign Initial Facts and Rules:**
    *   In the Inspector of the `KnowledgeBaseSystem` component, assign any `KnowledgeFact` assets to `Initial Permanent Facts` that should always be true (e.g., `Fact_NPCHasAmmo`).
    *   Assign all your created `KnowledgeRule` assets to the `Rules` list.
7.  **Run the Scene:** You can now interact with the system from other scripts (see example usage in `KnowledgeBaseSystem.cs` comments). Watch the Console for debug output.

---

### KnowledgeFact.cs

```csharp
using UnityEngine;
using System.Collections.Generic; // Not strictly needed for this SO, but often useful.

namespace KnowledgeBaseSystem
{
    /// <summary>
    /// KnowledgeFact ScriptableObject
    /// Represents a single piece of knowledge or a fact within the knowledge base.
    /// These are asset-based facts that can be pre-defined in the Unity editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewKnowledgeFact", menuName = "KnowledgeBaseSystem/Knowledge Fact", order = 1)]
    public class KnowledgeFact : ScriptableObject
    {
        // The unique identifier for this fact (e.g., "playerIsNear", "npcHealthLow").
        [Tooltip("The unique identifier for this fact (e.g., 'playerIsNear', 'npcHealthLow').")]
        public string Key;

        // The value associated with this fact (e.g., "true", "50", "Attack").
        [Tooltip("The value associated with this fact (e.g., 'true', '50', 'Attack').")]
        public string Value;

        // Determines if this fact is a core, permanent piece of knowledge,
        // or a transient fact that can be easily asserted/retracted.
        [Tooltip("If true, this fact is considered permanent and won't be cleared on memory reset.")]
        public bool IsPermanent = false;

        /// <summary>
        /// Provides a readable representation of the fact.
        /// </summary>
        public override string ToString()
        {
            return $"{Key}: {Value} ({(IsPermanent ? "Permanent" : "Transient")})";
        }
    }

    /// <summary>
    /// Represents a simple key-value pair for facts, used internally by rules
    /// or for asserting facts directly without needing a ScriptableObject asset.
    /// </summary>
    [System.Serializable]
    public class FactKeyValuePair
    {
        public string Key;
        public string Value;

        public FactKeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Key} = {Value}";
        }
    }
}
```

---

### KnowledgeRule.cs

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace KnowledgeBaseSystem
{
    /// <summary>
    /// KnowledgeRule ScriptableObject
    /// Defines a rule for the inference engine.
    /// A rule consists of an antecedent (conditions) and a consequent (what to assert if conditions are met).
    /// This allows us to define "if-then" logic as re-usable assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewKnowledgeRule", menuName = "KnowledgeBaseSystem/Knowledge Rule", order = 2)]
    public class KnowledgeRule : ScriptableObject
    {
        [Tooltip("A descriptive name for the rule, useful for debugging.")]
        public string RuleName = "New Rule";

        [Tooltip("The list of facts that must be true for this rule to 'fire'.")]
        public List<FactKeyValuePair> AntecedentFacts = new List<FactKeyValuePair>();

        [Tooltip("The list of facts that will be asserted if the antecedent conditions are met.")]
        public List<FactKeyValuePair> ConsequentFacts = new List<FactKeyValuePair>();

        /// <summary>
        /// Provides a readable representation of the rule.
        /// </summary>
        public override string ToString()
        {
            return $"Rule: {RuleName}\n  IF: {string.Join(" AND ", AntecedentFacts)}\n  THEN: {string.Join(" AND ", ConsequentFacts)}";
        }
    }
}
```

---

### KnowledgeBaseSystem.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ queries like Any(), All(), Select()

namespace KnowledgeBaseSystem
{
    /// <summary>
    /// KnowledgeBaseSystem MonoBehaviour
    /// This is the core component that manages the knowledge base.
    /// It handles storing facts, processing rules (inference), and querying knowledge.
    /// </summary>
    public class KnowledgeBaseSystem : MonoBehaviour
    {
        // Singleton pattern for easy access from anywhere in the game.
        // In a larger project, consider a dependency injection system or a more robust singleton.
        public static KnowledgeBaseSystem Instance { get; private set; }

        [Header("Knowledge Representation")]
        [Tooltip("Facts that are loaded at the start and persist throughout (e.g., NPC has ammo).")]
        [SerializeField] private List<KnowledgeFact> _initialPermanentFacts = new List<KnowledgeFact>();

        [Tooltip("Rules that the inference engine will use to deduce new facts.")]
        [SerializeField] private List<KnowledgeRule> _rules = new List<KnowledgeRule>();

        // Working Memory: Stores the current set of known facts.
        // Key: Fact Key (string), Value: Fact Value (string)
        private Dictionary<string, string> _currentFacts = new Dictionary<string, string>();

        // Keep track of which facts were asserted as permanent vs. temporary.
        // This is crucial for 'ResetWorkingMemory' to only clear temporary facts.
        private HashSet<string> _permanentFactKeys = new HashSet<string>();

        [Header("Inference Settings")]
        [Tooltip("Maximum iterations for the inference engine to prevent infinite loops.")]
        [SerializeField] private int _maxInferenceIterations = 10;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the singleton and loads initial facts.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("KnowledgeBaseSystem: Multiple instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeKnowledgeBase();
        }

        /// <summary>
        /// Initializes the knowledge base by loading all permanent facts.
        /// </summary>
        private void InitializeKnowledgeBase()
        {
            _currentFacts.Clear();
            _permanentFactKeys.Clear();

            Debug.Log("KnowledgeBaseSystem: Initializing with permanent facts.");
            foreach (var fact in _initialPermanentFacts)
            {
                AssertFact(fact.Key, fact.Value, fact.IsPermanent);
            }
            Debug.Log($"KnowledgeBaseSystem: Loaded {_currentFacts.Count} initial permanent facts.");
            DebugWorkingMemory();
        }

        /// <summary>
        /// Asserts a fact into the knowledge base. If the fact already exists, its value is updated.
        /// This is the primary way to add new knowledge to the system.
        /// </summary>
        /// <param name="key">The unique identifier of the fact.</param>
        /// <param name="value">The value associated with the fact.</param>
        /// <param name="isPermanent">If true, this fact will persist even after ResetWorkingMemory().</param>
        public void AssertFact(string key, string value, bool isPermanent = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("KnowledgeBaseSystem: Attempted to assert fact with null or empty key.");
                return;
            }

            if (_currentFacts.TryGetValue(key, out string existingValue))
            {
                if (existingValue != value)
                {
                    Debug.Log($"KnowledgeBaseSystem: Updating fact '{key}' from '{existingValue}' to '{value}'.");
                    _currentFacts[key] = value;
                }
                else
                {
                    // Fact exists with the same value, no change needed.
                    // Debug.Log($"KnowledgeBaseSystem: Fact '{key}' already exists with value '{value}'. No update needed.");
                }
            }
            else
            {
                _currentFacts.Add(key, value);
                Debug.Log($"KnowledgeBaseSystem: Asserted new fact '{key}' = '{value}'.");
            }

            if (isPermanent)
            {
                _permanentFactKeys.Add(key);
            }
            else
            {
                // If a fact was previously permanent but is now asserted as non-permanent, remove from permanent set.
                // This allows overriding permanent facts with temporary ones if desired, but typically permanent facts aren't re-asserted non-permanently.
                _permanentFactKeys.Remove(key);
            }
        }

        /// <summary>
        /// Removes a fact from the knowledge base, but only if it's not a permanent fact.
        /// </summary>
        /// <param name="key">The key of the fact to retract.</param>
        /// <returns>True if the fact was retracted, false otherwise (e.g., fact not found or is permanent).</returns>
        public bool RetractFact(string key)
        {
            if (_permanentFactKeys.Contains(key))
            {
                Debug.LogWarning($"KnowledgeBaseSystem: Cannot retract permanent fact '{key}'.");
                return false;
            }

            if (_currentFacts.Remove(key))
            {
                Debug.Log($"KnowledgeBaseSystem: Retracted fact '{key}'.");
                return true;
            }
            Debug.LogWarning($"KnowledgeBaseSystem: Attempted to retract non-existent fact '{key}'.");
            return false;
        }

        /// <summary>
        /// Queries the knowledge base for a specific fact.
        /// </summary>
        /// <param name="key">The key of the fact to query.</param>
        /// <returns>The value of the fact if found, otherwise null.</returns>
        public string QueryFact(string key)
        {
            _currentFacts.TryGetValue(key, out string value);
            return value;
        }

        /// <summary>
        /// Checks if a fact with the given key exists in the knowledge base.
        /// </summary>
        /// <param name="key">The key of the fact to check.</param>
        /// <returns>True if the fact exists, false otherwise.</returns>
        public bool HasFact(string key)
        {
            return _currentFacts.ContainsKey(key);
        }

        /// <summary>
        /// Checks if a fact with the given key and value exists in the knowledge base.
        /// </summary>
        /// <param name="key">The key of the fact.</param>
        /// <param name="value">The value to match.</param>
        /// <returns>True if the fact exists and its value matches, false otherwise.</returns>
        public bool HasFact(string key, string value)
        {
            return _currentFacts.TryGetValue(key, out string storedValue) && storedValue == value;
        }

        /// <summary>
        /// Clears all temporary facts from the working memory,
        /// leaving only the facts marked as permanent.
        /// Useful for resetting context for a new decision cycle.
        /// </summary>
        public void ResetWorkingMemory()
        {
            Debug.Log("KnowledgeBaseSystem: Resetting working memory (clearing transient facts).");
            var factsToRemove = _currentFacts.Keys.Where(key => !_permanentFactKeys.Contains(key)).ToList();
            foreach (var key in factsToRemove)
            {
                _currentFacts.Remove(key);
            }
            Debug.Log($"KnowledgeBaseSystem: Working memory reset. Remaining permanent facts: {_currentFacts.Count}.");
            DebugWorkingMemory();
        }

        /// <summary>
        /// The Inference Engine: Applies rules to deduce new facts.
        /// This uses a simple forward-chaining approach.
        /// It iterates through rules, checking antecedents against current facts,
        /// and asserting consequents if conditions are met, until no new facts are inferred
        /// in a complete pass or the max iteration limit is reached.
        /// </summary>
        /// <returns>True if any new facts were inferred during the process, false otherwise.</returns>
        public bool Infer()
        {
            Debug.Log("KnowledgeBaseSystem: Initiating inference cycle.");
            bool factsInferredThisCycle = false;
            int iterationCount = 0;

            // Loop until no new facts are inferred in a full pass, or max iterations reached.
            while (iterationCount < _maxInferenceIterations)
            {
                bool newFactsAssertedInPass = false;
                foreach (var rule in _rules)
                {
                    // Check if all antecedent facts for the rule are present and match values.
                    bool antecedentsMet = rule.AntecedentFacts.All(
                        antecedent => HasFact(antecedent.Key, antecedent.Value)
                    );

                    if (antecedentsMet)
                    {
                        // Antecedents met, now assert the consequent facts if they aren't already true.
                        foreach (var consequent in rule.ConsequentFacts)
                        {
                            if (!HasFact(consequent.Key, consequent.Value))
                            {
                                AssertFact(consequent.Key, consequent.Value); // Consequent facts are typically transient
                                newFactsAssertedInPass = true;
                                factsInferredThisCycle = true;
                                Debug.Log($"  Rule Fired: '{rule.RuleName}' -> Asserted '{consequent.Key}' = '{consequent.Value}'.");
                            }
                        }
                    }
                }

                if (!newFactsAssertedInPass)
                {
                    // No new facts were asserted in this pass, so we've reached a stable state.
                    break;
                }
                iterationCount++;
            }

            if (iterationCount >= _maxInferenceIterations)
            {
                Debug.LogWarning($"KnowledgeBaseSystem: Inference reached max iterations ({_maxInferenceIterations}). Possible infinite loop or complex rule set.");
            }
            else
            {
                Debug.Log($"KnowledgeBaseSystem: Inference completed in {iterationCount} passes.");
            }
            DebugWorkingMemory();
            return factsInferredThisCycle;
        }

        /// <summary>
        /// For debugging: Prints all current facts in the working memory to the console.
        /// </summary>
        public void DebugWorkingMemory()
        {
            Debug.Log("--- Current Knowledge Base Facts ---");
            if (_currentFacts.Count == 0)
            {
                Debug.Log("  (No facts currently asserted)");
                return;
            }

            foreach (var fact in _currentFacts)
            {
                Debug.Log($"  {fact.Key}: {fact.Value} ({(_permanentFactKeys.Contains(fact.Key) ? "Permanent" : "Transient")})");
            }
            Debug.Log("------------------------------------");
        }


        // --- EXAMPLE USAGE ---
        // You can call these methods from other scripts or a test script.

        /// <summary>
        /// Example method to demonstrate asserting facts and inferring conclusions.
        /// This could be triggered by game events or an NPC's update loop.
        /// </summary>
        [ContextMenu("Simulate NPC Decision Cycle")]
        public void SimulateNPCDecisionCycle()
        {
            Debug.Log("\n--- Starting NPC Decision Cycle Simulation ---");

            // 1. Reset working memory (clear previous temporary observations/decisions)
            ResetWorkingMemory();

            // 2. Assert current observations (temporary facts about the world)
            Debug.Log("\nAsserting current observations:");
            AssertFact("playerIsNear", "true");
            AssertFact("canSeePlayer", "true");
            AssertFact("npcHealthLow", "false");
            AssertFact("coverAvailable", "false");
            AssertFact("npcHasAmmo", "true", false); // Even if initial was permanent, we might override it temporarily.

            DebugWorkingMemory();

            // 3. Run the inference engine to deduce behavior
            Debug.Log("\nRunning Inference Engine:");
            Infer();

            // 4. Query for the determined behavior
            string behavior = QueryFact("npcBehavior");
            if (!string.IsNullOrEmpty(behavior))
            {
                Debug.Log($"\nNPC Decision: The determined behavior is: {behavior}");
            }
            else
            {
                Debug.Log("\nNPC Decision: No specific behavior could be determined from current facts and rules.");
            }

            // --- Scenario 2: Different conditions ---
            Debug.Log("\n--- Simulating Different Scenario (NPC Low Health) ---");
            ResetWorkingMemory(); // Start fresh with permanent facts

            AssertFact("playerIsNear", "true");
            AssertFact("npcHealthLow", "true"); // Now low health
            AssertFact("coverAvailable", "true"); // And cover is available
            AssertFact("npcHasAmmo", "true");

            DebugWorkingMemory();
            Infer();

            behavior = QueryFact("npcBehavior");
            if (!string.IsNullOrEmpty(behavior))
            {
                Debug.Log($"\nNPC Decision: The determined behavior is: {behavior}");
            }
            else
            {
                Debug.Log("\nNPC Decision: No specific behavior could be determined from current facts and rules.");
            }

            Debug.Log("\n--- NPC Decision Cycle Simulation Complete ---");
        }

        /// <summary>
        /// Example of how another script might interact with the KnowledgeBaseSystem.
        /// This could be an NPC AI script or a game manager.
        /// </summary>
        public class NPCControllerExample : MonoBehaviour
        {
            void Start()
            {
                // Ensure the system is initialized
                if (KnowledgeBaseSystem.Instance == null)
                {
                    Debug.LogError("KnowledgeBaseSystem instance not found!");
                    return;
                }

                // Simulate an event: Player enters NPC's detection range
                KnowledgeBaseSystem.Instance.AssertFact("playerIsNear", "true");
                Debug.Log("NPCControllerExample: Player detected!");

                // Let the system infer based on new facts
                KnowledgeBaseSystem.Instance.Infer();

                // Check what behavior was decided
                string currentBehavior = KnowledgeBaseSystem.Instance.QueryFact("npcBehavior");
                if (currentBehavior != null)
                {
                    Debug.Log($"NPCControllerExample: My inferred behavior is: {currentBehavior}");
                    // Based on 'currentBehavior', trigger animations, pathfinding, etc.
                }

                // Simulate another event: NPC runs out of ammo
                KnowledgeBaseSystem.Instance.AssertFact("npcHasAmmo", "false");
                Debug.Log("NPCControllerExample: Ran out of ammo!");

                KnowledgeBaseSystem.Instance.Infer(); // Re-infer
                currentBehavior = KnowledgeBaseSystem.Instance.QueryFact("npcBehavior");
                if (currentBehavior != null)
                {
                    Debug.Log($"NPCControllerExample: My new inferred behavior is: {currentBehavior}");
                }

                // Reset for a new cycle
                KnowledgeBaseSystem.Instance.ResetWorkingMemory();
                Debug.Log("NPCControllerExample: Resetting context.");
            }
        }
    }
}
```