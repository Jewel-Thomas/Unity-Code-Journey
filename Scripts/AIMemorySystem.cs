// Unity Design Pattern Example: AIMemorySystem
// This script demonstrates the AIMemorySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The AIMemorySystem design pattern is crucial for creating intelligent and believable AI in games. It provides a structured way for an AI agent to store, retrieve, and manage information about its environment, other agents, and past events.

This pattern typically involves:
1.  **Memory Entries**: Individual pieces of information (e.g., "saw player at X," "heard sound at Y").
2.  **Memory System**: A central component responsible for adding, retrieving, and managing the lifecycle of memories (e.g., decaying old memories, forgetting irrelevant ones).
3.  **Memory Queries**: Mechanisms to efficiently search and filter memories based on specific criteria.

Let's create a complete Unity example.

---

### `AIMemorySystem.cs` (The Core System)

This script will define the `Memory` structure, the `MemoryQuery` structure, and the `AIMemorySystem` MonoBehaviour responsible for managing all memories.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ queries like Where, OrderByDescending, Take

/// <summary>
/// Defines different types of memories an AI agent can have.
/// This enum makes memory categorization clear and extensible.
/// </summary>
public enum MemoryType
{
    Unknown,
    SawEnemy,
    HeardSound,
    VisitedLocation,
    PickedUpItem,
    SpottedAlly,
    PlayerLostSight,
    DamageTaken
}

/// <summary>
/// Represents a single piece of information stored in the AI's memory.
/// Each memory has a type, a target (what it's about), a location, a timestamp,
/// and a significance that can decay over time.
/// </summary>
[System.Serializable] // Makes Memory visible in the Inspector for debugging purposes (if stored directly)
public class Memory
{
    public Guid Id { get; private set; } // Unique identifier for this specific memory instance
    public MemoryType Type { get; private set; } // Categorization of the memory
    public GameObject Target { get; private set; } // The GameObject this memory pertains to (e.g., the enemy seen)
    public Vector3 Location { get; private set; } // The world position associated with the memory
    public string Description { get; private set; } // A detailed description or note
    public float Timestamp { get; private set; } // When the memory was created (Time.time)
    public float Significance { get; set; } // How important or relevant this memory is (0.0 to 1.0)

    /// <summary>
    /// Constructor for creating a new Memory object.
    /// </summary>
    /// <param name="type">The category of the memory.</param>
    /// <param name="target">The GameObject relevant to this memory (can be null).</param>
    /// <param name="location">The world position where the memory occurred.</param>
    /// <param name="description">A detailed description.</param>
    /// <param name="initialSignificance">The initial importance of this memory (clamped between 0 and 1).</param>
    public Memory(MemoryType type, GameObject target, Vector3 location, string description, float initialSignificance = 1.0f)
    {
        Id = Guid.NewGuid(); // Assign a unique ID
        Type = type;
        Target = target;
        Location = location;
        Description = description;
        Timestamp = Time.time; // Record the current time
        Significance = Mathf.Clamp01(initialSignificance); // Ensure significance is between 0 and 1
    }

    /// <summary>
    /// Provides a human-readable string representation of the memory.
    /// </summary>
    public override string ToString()
    {
        return $"[Memory: {Type}] Target: {Target?.name ?? "N/A"}, Loc: {Location}, Desc: '{Description}', Sig: {Significance:F2}, Age: {Time.time - Timestamp:F2}s";
    }
}

/// <summary>
/// Defines criteria for querying the AI's memory system.
/// This allows flexible retrieval of memories based on various filters.
/// </summary>
public struct MemoryQuery
{
    public MemoryType? Type { get; set; } // Optional: Filter by specific memory type
    public GameObject Target { get; set; } // Optional: Filter by the GameObject the memory pertains to
    public float MinSignificance { get; set; } // Minimum significance required
    public float MaxAge { get; set; } // Maximum age (in seconds) of memories to retrieve (Time.time - Timestamp)
    public int MaxResults { get; set; } // Maximum number of memories to return
    public bool SortBySignificance { get; set; } // Whether to sort results by significance (highest first)

    /// <summary>
    /// Constructor for a new MemoryQuery.
    /// </summary>
    /// <param name="type">Optional specific memory type.</param>
    /// <param name="target">Optional specific GameObject target.</param>
    /// <param name="minSignificance">Minimum significance threshold (default: 0).</param>
    /// <param name="maxAge">Maximum age in seconds (default: float.MaxValue for no age limit).</param>
    /// <param name="maxResults">Maximum number of results to return (default: int.MaxValue for no limit).</param>
    /// <param name="sortBySignificance">Whether to sort by significance (default: true).</param>
    public MemoryQuery(MemoryType? type = null, GameObject target = null, float minSignificance = 0f, float maxAge = float.MaxValue, int maxResults = int.MaxValue, bool sortBySignificance = true)
    {
        Type = type;
        Target = target;
        MinSignificance = minSignificance;
        MaxAge = maxAge;
        MaxResults = maxResults;
        SortBySignificance = sortBySignificance;
    }
}

/// <summary>
/// The core AIMemorySystem MonoBehaviour.
/// This component manages a collection of memories for an AI agent,
/// handling their creation, retrieval, decay, and eventual forgetting.
/// </summary>
public class AIMemorySystem : MonoBehaviour
{
    [Header("Memory Settings")]
    [Tooltip("How much significance a memory loses per second.")]
    [SerializeField] private float memoryDecayRatePerSecond = 0.05f;

    [Tooltip("Memories with significance below this threshold will be forgotten.")]
    [SerializeField] private float minSignificanceToForget = 0.01f;

    [Tooltip("How often (in seconds) the memory system processes decay and forgetting.")]
    [SerializeField] private float memoryProcessingInterval = 1.0f;

    private List<Memory> _memories = new List<Memory>(); // Internal storage for all memories
    private float _lastMemoryProcessingTime; // Timestamp of the last time memories were processed

    public IReadOnlyList<Memory> AllMemories => _memories; // Public read-only access to all current memories for inspection

    private void Awake()
    {
        _lastMemoryProcessingTime = Time.time;
    }

    private void Update()
    {
        // Process memory decay and forgetting periodically, not every frame, for efficiency.
        if (Time.time >= _lastMemoryProcessingTime + memoryProcessingInterval)
        {
            ProcessMemoryDecayAndForgetting();
            _lastMemoryProcessingTime = Time.time;
        }
    }

    /// <summary>
    /// Adds a new memory to the system.
    /// If an existing memory of the same type and target is highly similar,
    /// its significance might be boosted instead of adding a new one (optional, not implemented here for simplicity).
    /// </summary>
    /// <param name="memory">The Memory object to add.</param>
    public void AddMemory(Memory memory)
    {
        _memories.Add(memory);
        // Debug.Log($"[AIMemorySystem] Added memory: {memory}");
    }

    /// <summary>
    /// Retrieves memories based on the provided query criteria.
    /// Uses LINQ for efficient filtering, sorting, and limiting results.
    /// </summary>
    /// <param name="query">The MemoryQuery defining the retrieval criteria.</param>
    /// <returns>A list of memories matching the query, sorted and limited as specified.</returns>
    public List<Memory> RetrieveMemories(MemoryQuery query)
    {
        // Start with all memories
        IEnumerable<Memory> results = _memories;

        // Apply filters
        if (query.Type.HasValue)
        {
            results = results.Where(m => m.Type == query.Type.Value);
        }
        if (query.Target != null)
        {
            results = results.Where(m => m.Target == query.Target);
        }

        results = results.Where(m => m.Significance >= query.MinSignificance);
        results = results.Where(m => (Time.time - m.Timestamp) <= query.MaxAge);

        // Apply sorting
        if (query.SortBySignificance)
        {
            results = results.OrderByDescending(m => m.Significance);
        }
        else // Default sort by recency if not sorting by significance
        {
            results = results.OrderByDescending(m => m.Timestamp);
        }

        // Apply result limit
        if (query.MaxResults != int.MaxValue)
        {
            results = results.Take(query.MaxResults);
        }

        return results.ToList();
    }

    /// <summary>
    /// Forcibly removes a specific memory by its unique ID.
    /// </summary>
    /// <param name="memoryId">The unique ID of the memory to forget.</param>
    /// <returns>True if the memory was found and removed, false otherwise.</returns>
    public bool ForgetMemory(Guid memoryId)
    {
        int initialCount = _memories.Count;
        _memories.RemoveAll(m => m.Id == memoryId);
        return _memories.Count < initialCount;
    }

    /// <summary>
    /// Forcibly removes all memories from the system.
    /// </summary>
    public void ClearAllMemories()
    {
        _memories.Clear();
        Debug.Log("[AIMemorySystem] All memories cleared.");
    }

    /// <summary>
    /// Internal method to reduce the significance of memories over time
    /// and remove those that fall below a certain significance threshold.
    /// This is called periodically by the Update loop.
    /// </summary>
    private void ProcessMemoryDecayAndForgetting()
    {
        if (_memories.Count == 0) return;

        // Calculate the time elapsed since the last processing
        float deltaTime = Time.time - _lastMemoryProcessingTime;
        if (deltaTime <= 0) deltaTime = memoryProcessingInterval; // Fallback in case of very fast calls

        List<Memory> memoriesToKeep = new List<Memory>();

        foreach (var memory in _memories)
        {
            // Reduce significance based on decay rate and elapsed time
            memory.Significance -= memoryDecayRatePerSecond * deltaTime;

            // Clamp significance to ensure it doesn't go below zero
            memory.Significance = Mathf.Max(0f, memory.Significance);

            // If significance is above the threshold, keep the memory
            if (memory.Significance >= minSignificanceToForget)
            {
                memoriesToKeep.Add(memory);
            }
            // else { Debug.Log($"[AIMemorySystem] Forgetting memory due to decay: {memory.Description}"); }
        }

        // Replace the old list with the new, filtered list
        _memories = memoriesToKeep;
    }
}
```

---

### `AIDemonstration.cs` (Example Usage)

This script will show how to use the `AIMemorySystem` by simulating an AI agent's perception and decision-making based on memories.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for LINQ operations on the results

/// <summary>
/// This script demonstrates the usage of the AIMemorySystem.
/// Attach this to an AI agent GameObject alongside the AIMemorySystem script.
/// It simulates various events, adds memories, and then queries them.
/// </summary>
[RequireComponent(typeof(AIMemorySystem))] // Ensures an AIMemorySystem is present on the same GameObject
public class AIDemonstration : MonoBehaviour
{
    private AIMemorySystem _memorySystem; // Reference to the AI's memory system

    [Header("Demo Settings")]
    [Tooltip("A reference to the Player GameObject to simulate interaction.")]
    [SerializeField] private GameObject playerGameObject;
    [Tooltip("A reference to another friendly AI GameObject.")]
    [SerializeField] private GameObject allyGameObject;
    [Tooltip("A simulated item pickup location.")]
    [SerializeField] private Vector3 itemPickupLocation = new Vector3(5, 0, 5);

    [Tooltip("How often the AI will try to 'perceive' and update its memories.")]
    [SerializeField] private float perceptionInterval = 2f;
    private float _lastPerceptionTime;

    [Tooltip("How often the AI will try to 'make decisions' based on its memories.")]
    [SerializeField] private float decisionInterval = 5f;
    private float _lastDecisionTime;

    void Awake()
    {
        _memorySystem = GetComponent<AIMemorySystem>();
        if (_memorySystem == null)
        {
            Debug.LogError("AIMemorySystem not found on this GameObject. Please add it.");
            enabled = false; // Disable this script if the memory system isn't found
            return;
        }

        _lastPerceptionTime = Time.time;
        _lastDecisionTime = Time.time;

        if (playerGameObject == null)
        {
            playerGameObject = GameObject.FindWithTag("Player"); // Try to find a player by tag
            if (playerGameObject == null)
            {
                Debug.LogWarning("Player GameObject not set and not found by tag 'Player'. Some demo features will be limited.");
            }
        }
        if (allyGameObject == null)
        {
            // Create a dummy ally if not set, for demonstration purposes
            allyGameObject = new GameObject("Ally_Demo");
            allyGameObject.transform.position = new Vector3(-5, 0, 0);
            Debug.Log("Created a dummy 'Ally_Demo' GameObject for demonstration.");
        }
    }

    void Update()
    {
        // Simulate continuous perception
        if (Time.time >= _lastPerceptionTime + perceptionInterval)
        {
            SimulatePerception();
            _lastPerceptionTime = Time.time;
        }

        // Simulate periodic decision making based on current memories
        if (Time.time >= _lastDecisionTime + decisionInterval)
        {
            MakeDecisionBasedOnMemories();
            _lastDecisionTime = Time.time;
        }

        // --- Manual Memory Addition/Query for Debugging (Optional) ---
        // Press 'E' to simulate hearing a sound
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 soundOrigin = transform.position + transform.forward * 5f + Vector3.up * 1f;
            _memorySystem.AddMemory(new Memory(MemoryType.HeardSound, null, soundOrigin, "Loud unknown sound ahead!", 0.8f));
            Debug.Log("DEMO: Simulated 'heard sound' event.");
        }

        // Press 'Q' to query for the most significant memory
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("DEMO: Querying for most significant memory...");
            MemoryQuery query = new MemoryQuery(minSignificance: 0.1f, maxResults: 1, sortBySignificance: true);
            List<Memory> results = _memorySystem.RetrieveMemories(query);
            if (results.Any())
            {
                Debug.Log($"DEMO: Most significant memory: {results.First()}");
            }
            else
            {
                Debug.Log("DEMO: No significant memories found.");
            }
        }
    }

    /// <summary>
    /// Simulates the AI perceiving its environment and adding relevant memories.
    /// </summary>
    private void SimulatePerception()
    {
        // Example 1: Simulate seeing the player
        if (playerGameObject != null && Vector3.Distance(transform.position, playerGameObject.transform.position) < 10f)
        {
            // Add or update a "SawEnemy" memory
            // For a more advanced system, you might check if a similar memory exists
            // and just boost its significance instead of adding a new one.
            Memory newMemory = new Memory(MemoryType.SawEnemy, playerGameObject, playerGameObject.transform.position, "Spotted the player!", 0.9f);
            _memorySystem.AddMemory(newMemory);
            Debug.Log($"DEMO: Simulated 'Saw Enemy' (Player at {playerGameObject.transform.position})");
        }
        else if (playerGameObject != null && Vector3.Distance(transform.position, playerGameObject.transform.position) > 15f)
        {
             // If player is far away, add a "PlayerLostSight" memory
             Memory newMemory = new Memory(MemoryType.PlayerLostSight, playerGameObject, transform.position, "Lost sight of the player.", 0.6f);
             _memorySystem.AddMemory(newMemory);
             Debug.Log($"DEMO: Simulated 'Player Lost Sight'.");
        }


        // Example 2: Simulate visiting a specific location
        if (Vector3.Distance(transform.position, itemPickupLocation) < 2f)
        {
            _memorySystem.AddMemory(new Memory(MemoryType.VisitedLocation, null, itemPickupLocation, "Reached the item pickup spot.", 0.7f));
            // Move AI away from location to prevent spamming memory
            transform.position += Vector3.right * 1f;
            Debug.Log($"DEMO: Simulated 'Visited Location' (Item pickup at {itemPickupLocation})");
        }


        // Example 3: Simulate spotting an ally
        if (allyGameObject != null && Vector3.Distance(transform.position, allyGameObject.transform.position) < 8f)
        {
            _memorySystem.AddMemory(new Memory(MemoryType.SpottedAlly, allyGameObject, allyGameObject.transform.position, "Saw an ally nearby.", 0.5f));
            Debug.Log($"DEMO: Simulated 'Spotted Ally' ({allyGameObject.name} at {allyGameObject.transform.position})");
        }
    }

    /// <summary>
    /// Simulates the AI making a decision based on its current memories.
    /// This is where the AI's logic would interact with the memory system.
    /// </summary>
    private void MakeDecisionBasedOnMemories()
    {
        Debug.Log("\n--- DEMO: AI Making Decision ---");

        // Query 1: Find the most recent enemy sighting
        MemoryQuery enemyQuery = new MemoryQuery(MemoryType.SawEnemy, maxResults: 1, sortBySignificance: true, minSignificance: 0.1f);
        List<Memory> enemySightings = _memorySystem.RetrieveMemories(enemyQuery);

        if (enemySightings.Any())
        {
            Memory lastEnemySighting = enemySightings.First();
            Debug.Log($"AI Decision: Recalled seeing enemy at {lastEnemySighting.Location} {Time.time - lastEnemySighting.Timestamp:F1}s ago. Current significance: {lastEnemySighting.Significance:F2}");
            // In a real game, the AI might now pathfind to this location, or engage combat.
        }
        else
        {
            Debug.Log("AI Decision: No recent enemy sightings. Feeling safe for now.");
        }

        // Query 2: Check for any uninvestigated sounds
        MemoryQuery soundQuery = new MemoryQuery(MemoryType.HeardSound, maxAge: 10f, maxResults: 1, minSignificance: 0.5f);
        List<Memory> soundsHeard = _memorySystem.RetrieveMemories(soundQuery);

        if (soundsHeard.Any())
        {
            Memory significantSound = soundsHeard.First();
            Debug.Log($"AI Decision: Heard a significant sound at {significantSound.Location}. Investigating...");
            // AI would move towards sound location.
        }

        // Query 3: Check if the player was recently lost sight of
        MemoryQuery playerLostQuery = new MemoryQuery(MemoryType.PlayerLostSight, playerGameObject, maxAge: 5f, maxResults: 1, minSignificance: 0.2f);
        List<Memory> playerLostMemories = _memorySystem.RetrieveMemories(playerLostQuery);
        if (playerLostMemories.Any())
        {
            Debug.Log($"AI Decision: Player recently lost sight of. Searching last known area around {playerLostMemories.First().Location}...");
        }

        // Query 4: List all currently active memories for debugging
        Debug.Log("AI Decision: Current active memories:");
        foreach (var mem in _memorySystem.AllMemories.OrderByDescending(m => m.Significance))
        {
            Debug.Log($"  - {mem}");
        }
        Debug.Log("--- End AI Decision ---\n");
    }

    /// <summary>
    /// Draws spheres in the scene view to visualize memory locations.
    /// This is useful for debugging memory-based AI behavior.
    /// </summary>
    void OnDrawGizmos()
    {
        if (_memorySystem == null || _memorySystem.AllMemories == null) return;

        foreach (var memory in _memorySystem.AllMemories)
        {
            switch (memory.Type)
            {
                case MemoryType.SawEnemy:
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(memory.Location, 0.5f * memory.Significance);
                    Gizmos.DrawWireCube(memory.Location, Vector3.one * (0.8f * memory.Significance));
                    break;
                case MemoryType.HeardSound:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(memory.Location, 1f * memory.Significance);
                    break;
                case MemoryType.VisitedLocation:
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(memory.Location, Vector3.one * 0.5f);
                    break;
                case MemoryType.SpottedAlly:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(memory.Location, 0.4f * memory.Significance);
                    break;
                case MemoryType.PlayerLostSight:
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(memory.Location, 0.7f * memory.Significance);
                    break;
                default:
                    Gizmos.color = Color.grey;
                    Gizmos.DrawWireSphere(memory.Location, 0.3f);
                    break;
            }

            // Draw text label in editor for more info
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(memory.Location + Vector3.up * (1f + memory.Significance), $"{memory.Type.ToString()} (Sig: {memory.Significance:F2})");
            #endif
        }

        // Draw the item pickup location
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(itemPickupLocation, Vector3.one);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(itemPickupLocation + Vector3.up * 1.5f, "Item Pickup Spot");
        #endif
    }
}
```

---

### How to Use in Unity:

1.  **Create a C# Script:**
    *   In your Unity Project window, right-click -> `Create` -> `C# Script`.
    *   Name it `AIMemorySystem`. Copy and paste the first C# code block into it.
2.  **Create another C# Script:**
    *   Right-click -> `Create` -> `C# Script`.
    *   Name it `AIDemonstration`. Copy and paste the second C# code block into it.
3.  **Create an AI Agent GameObject:**
    *   In your Unity Hierarchy window, right-click -> `Create Empty`.
    *   Name it `AI_Agent`.
    *   Add a `Capsule` or `Cube` to it so you can see it in the scene (`Right-click AI_Agent -> 3D Object -> Capsule`).
    *   Position it somewhere in the scene (e.g., `0, 0, 0`).
4.  **Attach Scripts:**
    *   Select `AI_Agent` in the Hierarchy.
    *   Drag and drop both `AIMemorySystem` and `AIDemonstration` scripts onto it in the Inspector.
5.  **Create a Player GameObject:**
    *   Right-click -> `3D Object` -> `Cube`.
    *   Name it `Player`.
    *   **Crucially**, set its `Tag` to `Player` in the Inspector (select the `Player` object, then in the Inspector, click the `Tag` dropdown, select `Add Tag...`, create a new tag named `Player`, then re-select `Player` object and assign the new `Player` tag to it).
    *   Position it away from the `AI_Agent` (e.g., `10, 0, 10`).
6.  **Configure AIDemonstration:**
    *   Select `AI_Agent`.
    *   In the `AIDemonstration` component, drag your `Player` GameObject from the Hierarchy into the `Player Game Object` slot.
    *   Optionally, create another `Cube` named `Ally` and drag it into the `Ally Game Object` slot. If you don't, the demo will create a dummy one.
    *   Adjust `Perception Interval` and `Decision Interval` as desired.
7.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe the Console window for memory additions, queries, and AI decisions.
    *   Move the `Player` object closer to the `AI_Agent` to trigger "Saw Enemy" memories.
    *   Move the `AI_Agent` to the `Item Pickup Spot` (you can see it in the Scene view `OnDrawGizmos` if `AIDemonstration` is selected) to trigger "Visited Location" memories.
    *   Press 'E' to simulate hearing a sound, and 'Q' to query for the most significant memory.
    *   Watch the `OnDrawGizmos` in the Scene view (when `AI_Agent` is selected) for visual representations of memories.

This setup provides a fully working and interactive example of the AIMemorySystem pattern in Unity.