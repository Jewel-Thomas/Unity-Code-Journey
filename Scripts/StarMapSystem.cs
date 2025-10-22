// Unity Design Pattern Example: StarMapSystem
// This script demonstrates the StarMapSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a practical implementation of a 'StarMapSystem' design pattern in Unity using C#. While 'StarMapSystem' isn't a traditional GoF (Gang of Four) pattern, it represents a common architectural pattern for managing a specific domain (like a star map) within a game.

**Core Concepts Demonstrated:**

1.  **Singleton Pattern:** The `StarMapSystem` class is implemented as a Singleton, ensuring there's only one instance responsible for managing the star map throughout the game.
2.  **Service/Manager Pattern:** `StarMapSystem` acts as a central service, providing a clear, cohesive API for all star map-related operations (e.g., discovering stars, jumping between systems).
3.  **Data Model:** The `StarSystem` class is a plain C# class that encapsulates the data for a single star system.
4.  **Event Bus (Observer Pattern):** `StarMapSystemEvents` is a static class using `UnityEvent`s to notify other parts of the game about changes in the star map state (e.g., a new star is discovered, the player jumps). This promotes loose coupling between the map logic and its consumers (like UI or other game systems).
5.  **Encapsulation:** The internal representation and logic of the star map are hidden within `StarMapSystem`, exposed only through its public API.

---

### How to Use This Script in Unity:

1.  **Create a C# Script:** Create a new C# script in your Unity project (e.g., `StarMapSystem_Example.cs`) and copy-paste the entire code below into it.
2.  **Create an Empty GameObject:** In your scene, create an empty GameObject (e.g., `_GameSystems`).
3.  **Attach StarMapSystem:** Drag and drop the `StarMapSystem_Example.cs` script onto the `_GameSystems` GameObject. The `StarMapSystem` component will be added. (The `StarMapDisplayUI` and `StarMapTestClient` components will also be added to the same GameObject by default, which is fine for this example).
4.  **Run the Scene:** Play the scene. Observe the Unity console for detailed logs from the `StarMapSystem`, `StarMapDisplayUI`, and `StarMapTestClient` demonstrating the pattern in action.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like .Where() and .Select()
using UnityEngine.Events; // Required for UnityEvent

// Ensure all classes are within a namespace to prevent naming conflicts
namespace StarMapSystemExample
{
    // --- Star Map System Design Pattern Explanation ---
    //
    // This example demonstrates a 'StarMapSystem' architectural pattern.
    // It's designed to centralize all logic and data related to a galactic star map,
    // providing a robust and flexible way for other game components to interact with it.
    //
    // Key Principles & Design Patterns Applied:
    // ----------------------------------------
    // 1.  Singleton Pattern: Ensures only one instance of the StarMapSystem exists,
    //     making it globally accessible without passing references everywhere. This is
    //     ideal for a core game system like a map manager.
    // 2.  Service/Manager Pattern: The StarMapSystem class acts as a dedicated service
    //     responsible solely for star map operations and data. It encapsulates the
    //     map's complexity.
    // 3.  Data Model (StarSystem): A simple, clear class to represent individual star
    //     systems, holding their unique properties and state.
    // 4.  Event Bus (Observer Pattern): The StarMapSystemEvents static class acts as
    //     an event dispatcher. Other game systems (e.g., UI, AI, game progression)
    //     can subscribe to these events (e.g., OnStarDiscovered, OnPlayerJumped) to react
    //     to map changes without directly knowing or depending on the StarMapSystem's internal
    //     implementation. This promotes loose coupling and modularity.
    // 5.  Clear API: Provides a well-defined set of public methods to interact with the map,
    //     hiding internal data structures and logic.
    // 6.  Unity Best Practices: Uses MonoBehaviour, UnityEvents, proper logging, and
    //     `DontDestroyOnLoad` for persistence.

    // --- 1. Data Model: StarSystem ---
    /// <summary>
    /// Represents a single star system in the galaxy.
    /// This is the core data model for each star, encapsulating its properties and runtime state.
    /// </summary>
    [Serializable] // Allows Unity to serialize instances of this class if used in a list/array in a MonoBehaviour or ScriptableObject
    public class StarSystem
    {
        public string ID { get; private set; } // Unique identifier for the system
        public string Name { get; private set; } // Display name
        public Vector3 Position { get; private set; } // Position in 3D space (can be world coordinates or map coordinates)
        public bool IsDiscovered { get; private set; } // Tracks if the player has visited/scanned this system

        // List of IDs of other star systems that can be directly jumped to from this system.
        // In a more complex game, jump lanes might have additional properties (cost, restrictions).
        public List<string> NeighborIDs { get; private set; }

        /// <summary>
        /// Constructor for creating a new StarSystem instance.
        /// </summary>
        /// <param name="id">The unique ID for this star system.</param>
        /// <param name="name">The display name of the system.</param>
        /// <param name="position">The 3D position of the system.</param>
        /// <param name="neighborIDs">Optional: A list of IDs of direct neighbor systems.</param>
        public StarSystem(string id, string name, Vector3 position, List<string> neighborIDs = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("StarSystem ID cannot be null or empty during creation.");
                return;
            }
            ID = id;
            Name = name;
            Position = position;
            IsDiscovered = false; // By default, systems are undiscovered until the player interacts with them
            NeighborIDs = neighborIDs ?? new List<string>(); // Ensure NeighborIDs is never null
        }

        /// <summary>
        /// Marks this star system as discovered.
        /// This method encapsulates the logic for changing the discovery state.
        /// </summary>
        public void MarkAsDiscovered()
        {
            if (!IsDiscovered) // Only process if it's a new discovery
            {
                IsDiscovered = true;
                Debug.Log($"Star System '{Name}' ({ID}) has been newly discovered!");
                // In a real game, this might trigger visual effects, lore unlocks, or rewards specific to this star.
            }
        }

        public override string ToString()
        {
            return $"[{ID}] {Name} (Pos: {Position}) - Discovered: {IsDiscovered}";
        }
    }

    // --- 2. Event Bus: StarMapSystemEvents ---
    /// <summary>
    /// A static class that serves as an event bus for the StarMapSystem.
    /// It centralizes all events related to the star map, allowing other systems
    /// to subscribe and react to changes without direct dependencies on the
    /// StarMapSystem's internal implementation. This is a core part of the Observer pattern.
    /// </summary>
    public static class StarMapSystemEvents
    {
        // Event fired when a new star system is discovered.
        // Listeners will receive the discovered StarSystem object.
        public static UnityEvent<StarSystem> OnStarDiscovered = new UnityEvent<StarSystem>();

        // Event fired when the player successfully jumps to a new star system.
        // Listeners will receive the old (from) system and the new (to) system.
        public static UnityEvent<StarSystem, StarSystem> OnPlayerJumped = new UnityEvent<StarSystem, StarSystem>();

        // Event fired when an attempt to jump to a system fails.
        // Listeners will receive the ID of the target system and a reason for failure.
        public static UnityEvent<string, string> OnJumpFailed = new UnityEvent<string, string>();
    }


    // --- 3. The StarMapSystem Manager (Singleton MonoBehaviour) ---
    /// <summary>
    /// The central manager for the entire Star Map System.
    /// Implemented as a Singleton, it provides a single, globally accessible
    /// point of control and data for all star map related operations.
    /// It manages all <see cref="StarSystem"/> objects, tracks the player's
    /// current location, and handles map interactions like discovery and jumping.
    /// </summary>
    public class StarMapSystem : MonoBehaviour
    {
        // Public static property to access the single instance of the StarMapSystem.
        // This is the core of the Singleton pattern.
        public static StarMapSystem Instance { get; private set; }

        // Private dictionary to store all star systems, keyed by their unique ID for fast lookup.
        private Dictionary<string, StarSystem> _starSystems;

        // The star system where the player is currently located.
        public StarSystem CurrentSystem { get; private set; }

        [Header("Initial Setup")]
        [Tooltip("The ID of the star system where the player starts the game.")]
        [SerializeField] private string _startingSystemID = "Sol";

        private void Awake()
        {
            // Singleton enforcement: Ensure only one instance of StarMapSystem exists.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StarMapSystem: An instance already exists. Destroying duplicate GameObject.", this);
                Destroy(this.gameObject); // Destroy this duplicate GameObject
                return;
            }
            Instance = this; // Set this instance as the Singleton
            // Ensure this GameObject persists across scene loads, typical for core game managers.
            DontDestroyOnLoad(this.gameObject);

            _starSystems = new Dictionary<string, StarSystem>();

            // Initialize the map with sample data.
            // In a real project, this would involve loading from persistent storage,
            // ScriptableObjects, or procedural generation.
            InitializeMap();

            // Set the player's initial location based on the configured starting system ID.
            SetInitialPlayerLocation();
        }

        /// <summary>
        /// Populates the star map with initial data.
        /// This method serves as a placeholder for how map data would be loaded.
        /// </summary>
        private void InitializeMap()
        {
            Debug.Log("StarMapSystem: Initializing map data...");

            // --- Define Sample Star Systems ---
            // Each StarSystem is created with an ID, Name, 3D Position, and a list of Neighbor IDs.
            StarSystem sol = new StarSystem("Sol", "Sol System", new Vector3(0, 0, 0),
                new List<string> { "AlphaC", "Sirius" });
            StarSystem alphaCentauri = new StarSystem("AlphaC", "Alpha Centauri", new Vector3(4.3f, 0, 0),
                new List<string> { "Sol", "BarnardS" });
            StarSystem sirius = new StarSystem("Sirius", "Sirius", new Vector3(-8.6f, 2.5f, 0),
                new List<string> { "Sol", "Procyon" });
            StarSystem procyon = new StarSystem("Procyon", "Procyon", new Vector3(-11.4f, -1.0f, 0),
                new List<string> { "Sirius" });
            StarSystem barnardsStar = new StarSystem("BarnardS", "Barnard's Star", new Vector3(5.9f, 0, 7.5f),
                new List<string> { "AlphaC" });

            // --- Add Systems to the Map ---
            AddStarSystem(sol);
            AddStarSystem(alphaCentauri);
            AddStarSystem(sirius);
            AddStarSystem(procyon);
            AddStarSystem(barnardsStar);

            Debug.Log($"StarMapSystem: Map initialized with { _starSystems.Count } systems.");
        }

        /// <summary>
        /// Helper method to add a star system to the internal dictionary.
        /// </summary>
        private void AddStarSystem(StarSystem system)
        {
            if (system == null || string.IsNullOrEmpty(system.ID))
            {
                Debug.LogError("StarMapSystem: Attempted to add a null or invalid star system.");
                return;
            }
            if (_starSystems.ContainsKey(system.ID))
            {
                Debug.LogWarning($"StarMapSystem: Attempted to add duplicate system ID: {system.ID}. Ignoring.");
                return;
            }
            _starSystems.Add(system.ID, system);
        }

        /// <summary>
        /// Sets the player's initial location in the star map.
        /// </summary>
        private void SetInitialPlayerLocation()
        {
            if (_starSystems.ContainsKey(_startingSystemID))
            {
                CurrentSystem = _starSystems[_startingSystemID];
                // The starting system is always considered discovered.
                CurrentSystem.MarkAsDiscovered();
                Debug.Log($"StarMapSystem: Player starts in: {CurrentSystem.Name} ({CurrentSystem.ID})");
                // Inform listeners that the player has 'spawned' in a system
                StarMapSystemEvents.OnPlayerJumped.Invoke(null, CurrentSystem); // oldSystem is null for initial spawn
            }
            else
            {
                Debug.LogError($"StarMapSystem: Starting system '{_startingSystemID}' not found in map data! Please check '_startingSystemID' in Inspector.");
                // Fallback to the first available system if the specified one isn't found
                if (_starSystems.Any())
                {
                    CurrentSystem = _starSystems.Values.First();
                    CurrentSystem.MarkAsDiscovered();
                    Debug.LogWarning($"StarMapSystem: Falling back to start in: {CurrentSystem.Name} ({CurrentSystem.ID})");
                    StarMapSystemEvents.OnPlayerJumped.Invoke(null, CurrentSystem);
                }
                else
                {
                    Debug.LogError("StarMapSystem: No star systems were initialized at all! Map is empty.");
                }
            }
        }

        // --- Public API for interacting with the Star Map ---
        // These methods provide the interface for other game components to query and modify the map state.

        /// <summary>
        /// Retrieves a <see cref="StarSystem"/> object by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the star system to retrieve.</param>
        /// <returns>The <see cref="StarSystem"/> object if found; otherwise, null.</returns>
        public StarSystem GetStarSystem(string id)
        {
            _starSystems.TryGetValue(id, out var system);
            return system;
        }

        /// <summary>
        /// Retrieves a read-only list of all star systems currently in the map.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of all <see cref="StarSystem"/> objects.</returns>
        public IReadOnlyList<StarSystem> GetAllStarSystems()
        {
            return _starSystems.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves a read-only list of all star systems that have been discovered by the player.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of discovered <see cref="StarSystem"/> objects.</returns>
        public IReadOnlyList<StarSystem> GetDiscoveredStarSystems()
        {
            return _starSystems.Values.Where(s => s.IsDiscovered).ToList().AsReadOnly();
        }

        /// <summary>
        /// Attempts to mark a star system as discovered.
        /// If the system is already discovered or not found, no action is taken.
        /// Invokes <see cref="StarMapSystemEvents.OnStarDiscovered"/> if a new discovery occurs.
        /// </summary>
        /// <param name="id">The ID of the star system to discover.</param>
        /// <returns>True if the system was newly discovered; false if already discovered or not found.</returns>
        public bool DiscoverStar(string id)
        {
            if (_starSystems.TryGetValue(id, out var system))
            {
                if (!system.IsDiscovered)
                {
                    system.MarkAsDiscovered(); // Change internal state
                    StarMapSystemEvents.OnStarDiscovered.Invoke(system); // Notify listeners
                    return true;
                }
                else
                {
                    Debug.Log($"StarMapSystem: System '{system.Name}' ({id}) was already discovered.");
                }
            }
            else
            {
                Debug.LogWarning($"StarMapSystem: Attempted to discover unknown system ID: '{id}'.");
            }
            return false;
        }

        /// <summary>
        /// Attempts to move the player's ship to a new star system.
        /// This operation involves several checks:
        /// 1. The target system must exist.
        /// 2. The target system must already be discovered.
        /// 3. The target system must be a direct neighbor of the current system.
        /// Invokes <see cref="StarMapSystemEvents.OnPlayerJumped"/> on success,
        /// or <see cref="StarMapSystemEvents.OnJumpFailed"/> on failure.
        /// </summary>
        /// <param name="targetSystemId">The ID of the star system to jump to.</param>
        /// <returns>True if the jump was successful; false otherwise.</returns>
        public bool JumpToStar(string targetSystemId)
        {
            if (CurrentSystem == null)
            {
                string reason = "Player's current system is not set!";
                Debug.LogError($"StarMapSystem: Cannot jump. {reason}");
                StarMapSystemEvents.OnJumpFailed.Invoke(targetSystemId, reason);
                return false;
            }

            // 1. Check if target system exists
            if (!_starSystems.TryGetValue(targetSystemId, out var targetSystem))
            {
                string reason = $"Target system '{targetSystemId}' not found.";
                Debug.LogWarning($"StarMapSystem: Cannot jump. {reason}");
                StarMapSystemEvents.OnJumpFailed.Invoke(targetSystemId, reason);
                return false;
            }

            // 2. Check if target system is discovered
            if (!targetSystem.IsDiscovered)
            {
                string reason = $"Target system '{targetSystem.Name}' has not been discovered yet.";
                Debug.LogWarning($"StarMapSystem: Cannot jump. {reason}");
                StarMapSystemEvents.OnJumpFailed.Invoke(targetSystemId, reason);
                return false;
            }

            // 3. Check if target system is a direct neighbor of the current system
            if (!CurrentSystem.NeighborIDs.Contains(targetSystemId))
            {
                string reason = $"'{targetSystem.Name}' is not a direct neighbor of '{CurrentSystem.Name}'.";
                Debug.LogWarning($"StarMapSystem: Cannot jump. {reason}");
                StarMapSystemEvents.OnJumpFailed.Invoke(targetSystemId, reason);
                return false;
            }

            // If all checks pass, perform the jump
            StarSystem oldSystem = CurrentSystem;
            CurrentSystem = targetSystem; // Update player's current system
            Debug.Log($"StarMapSystem: Player successfully jumped from '{oldSystem.Name}' to '{CurrentSystem.Name}'.");
            StarMapSystemEvents.OnPlayerJumped.Invoke(oldSystem, CurrentSystem); // Notify listeners
            return true;
        }

        /// <summary>
        /// Gets a read-only list of star systems that are direct neighbors of a given system.
        /// </summary>
        /// <param name="systemId">The ID of the system to find neighbors for.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of neighbor <see cref="StarSystem"/> objects.
        /// Returns an empty list if the system is not found or has no neighbors.</returns>
        public IReadOnlyList<StarSystem> GetNeighbors(string systemId)
        {
            if (_starSystems.TryGetValue(systemId, out var system))
            {
                List<StarSystem> neighbors = new List<StarSystem>();
                foreach (string neighborID in system.NeighborIDs)
                {
                    if (_starSystems.TryGetValue(neighborID, out var neighborSystem))
                    {
                        neighbors.Add(neighborSystem);
                    }
                    else
                    {
                        Debug.LogWarning($"StarMapSystem: Neighbor ID '{neighborID}' for system '{systemId}' points to an unknown system.");
                    }
                }
                return neighbors.AsReadOnly();
            }
            Debug.LogWarning($"StarMapSystem: System '{systemId}' not found when requesting neighbors.");
            return new List<StarSystem>().AsReadOnly(); // Return empty list if system not found
        }

        /// <summary>
        /// Gets a read-only list of star systems that are direct neighbors of the
        /// <see cref="CurrentSystem"/> AND have already been discovered by the player.
        /// This is useful for UI elements that show available jump targets.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of accessible neighbor <see cref="StarSystem"/> objects.</returns>
        public IReadOnlyList<StarSystem> GetAccessibleNeighborSystems()
        {
            if (CurrentSystem == null) return new List<StarSystem>().AsReadOnly();

            return GetNeighbors(CurrentSystem.ID) // Get all direct neighbors
                   .Where(s => s.IsDiscovered)   // Filter to only include discovered ones
                   .ToList()
                   .AsReadOnly();
        }
    }


    // --- 4. Example Usage: StarMapDisplayUI (An Observer/Consumer Component) ---
    /// <summary>
    /// This MonoBehaviour acts as a simple UI display for the Star Map System.
    /// It demonstrates the Observer pattern by subscribing to events from
    /// <see cref="StarMapSystemEvents"/> and updating its display accordingly.
    /// In a real game, this would update actual UI elements (TextMeshPro, Image, etc.).
    /// </summary>
    [RequireComponent(typeof(StarMapSystem))] // Requires StarMapSystem to be on the same GameObject or handles it explicitly
    public class StarMapDisplayUI : MonoBehaviour
    {
        // In a real project, you would reference UI elements here:
        // [SerializeField] private TextMeshProUGUI currentSystemNameText;
        // [SerializeField] private TextMeshProUGUI discoveredSystemsListText;
        // [SerializeField] private GameObject jumpButtonTemplate; // For dynamic jump buttons

        private void OnEnable()
        {
            // Subscribe to events from the StarMapSystemEvents bus.
            // This allows the UI to react to map changes without directly polling the StarMapSystem.
            StarMapSystemEvents.OnStarDiscovered.AddListener(HandleStarDiscovered);
            StarMapSystemEvents.OnPlayerJumped.AddListener(HandlePlayerJumped);
            StarMapSystemEvents.OnJumpFailed.AddListener(HandleJumpFailed);
            Debug.Log("StarMapDisplayUI: Subscribed to StarMapSystemEvents.");
        }

        private void OnDisable()
        {
            // Always unsubscribe from events to prevent memory leaks, especially when the GameObject is destroyed.
            StarMapSystemEvents.OnStarDiscovered.RemoveListener(HandleStarDiscovered);
            StarMapSystemEvents.OnPlayerJumped.RemoveListener(HandlePlayerJumped);
            StarMapSystemEvents.OnJumpFailed.RemoveListener(HandleJumpFailed);
            Debug.Log("StarMapDisplayUI: Unsubscribed from StarMapSystemEvents.");
        }

        private void Start()
        {
            // Initial UI update when the scene starts (after StarMapSystem has initialized).
            // Small delay to ensure StarMapSystem has completed its Awake()
            Invoke(nameof(UpdateDisplay), 0.1f);
        }

        // --- Event Handlers ---
        // These methods are called automatically when the corresponding events are invoked.

        private void HandleStarDiscovered(StarSystem system)
        {
            Debug.Log($"<color=green>UI Event:</color> New Star Discovered! '{system.Name}'. Updating map display.");
            UpdateDisplay(); // Refresh UI to show the newly discovered star
        }

        private void HandlePlayerJumped(StarSystem oldSystem, StarSystem newSystem)
        {
            string oldSystemName = oldSystem?.Name ?? "[Initial Spawn]";
            Debug.Log($"<color=cyan>UI Event:</color> Player Jumped from '{oldSystemName}' to '{newSystem.Name}'. Updating map display.");
            UpdateDisplay(); // Refresh UI to show new current system and accessible neighbors
        }

        private void HandleJumpFailed(string targetId, string reason)
        {
            Debug.LogWarning($"<color=red>UI Event:</color> Jump to '{targetId}' failed! Reason: {reason}. Displaying error message to player.");
            // In a real game, you'd show a UI message here (e.g., "Cannot jump: Not enough fuel!").
        }

        /// <summary>
        /// Updates all relevant UI elements based on the current state of the StarMapSystem.
        /// </summary>
        private void UpdateDisplay()
        {
            // Always check for the Singleton instance, especially if this script's Awake/Start might run before it.
            if (StarMapSystem.Instance == null)
            {
                Debug.LogError("StarMapSystem instance not found for UI update. Is it in the scene and initialized?");
                return;
            }

            Debug.Log("\n--- Star Map UI Display Update ---");
            Debug.Log($"Current System: <color=yellow>{StarMapSystem.Instance.CurrentSystem?.Name ?? "Unknown"}</color>");

            // Display discovered systems
            string discoveredList = "Discovered Systems:\n";
            var discoveredSystems = StarMapSystem.Instance.GetDiscoveredStarSystems();
            if (discoveredSystems.Any())
            {
                foreach (var system in discoveredSystems)
                {
                    discoveredList += $"- {system.Name} ({system.ID})\n";
                }
            }
            else
            {
                discoveredList += "  (None yet, besides current)\n";
            }
            Debug.Log(discoveredList);

            // Display accessible neighbor systems (potential jump targets)
            string accessibleNeighbors = "Accessible Neighbor Systems (Jump Targets):\n";
            var accessibleNeighborSystems = StarMapSystem.Instance.GetAccessibleNeighborSystems();
            if (accessibleNeighborSystems.Any())
            {
                foreach (var system in accessibleNeighborSystems)
                {
                    accessibleNeighbors += $"- {system.Name} ({system.ID})\n";
                }
            }
            else
            {
                accessibleNeighbors += "  (None accessible from here)\n";
            }
            Debug.Log(accessibleNeighbors);
            Debug.Log("----------------------------------\n");

            // Example of how you would update actual UI TextMeshProUGUI elements:
            // if (currentSystemNameText != null) currentSystemNameText.text = $"Current: {StarMapSystem.Instance.CurrentSystem?.Name ?? "N/A"}";
            // if (discoveredSystemsListText != null) discoveredSystemsListText.text = discoveredList;
            // You might also dynamically create buttons for each accessible neighbor to allow jumping.
        }


        // --- Example Methods for UI Button Interaction (called by editor buttons or other game logic) ---

        /// <summary>
        /// Public method that could be linked to a UI button to simulate discovering a star system.
        /// This directly interacts with the StarMapSystem's API.
        /// </summary>
        /// <param name="systemID">The ID of the system to attempt to discover.</param>
        public void TryDiscoverSystem(string systemID)
        {
            if (StarMapSystem.Instance != null)
            {
                Debug.Log($"<color=magenta>UI Action:</color> Player attempting to discover '{systemID}'...");
                StarMapSystem.Instance.DiscoverStar(systemID);
            }
        }

        /// <summary>
        /// Public method that could be linked to a UI button to simulate a player attempting to jump to a star system.
        /// This directly interacts with the StarMapSystem's API.
        /// </summary>
        /// <param name="systemID">The ID of the system to attempt to jump to.</param>
        public void TryJumpToSystem(string systemID)
        {
            if (StarMapSystem.Instance != null)
            {
                string currentSystemName = StarMapSystem.Instance.CurrentSystem?.Name ?? "Unknown";
                Debug.Log($"<color=magenta>UI Action:</color> Player attempting to jump to '{systemID}' from '{currentSystemName}'...");
                StarMapSystem.Instance.JumpToStar(systemID);
            }
        }
    }


    // --- 5. Example Usage: StarMapTestClient (A Script for Automated Testing/Demonstration) ---
    /// <summary>
    /// This script provides an automated sequence of actions to demonstrate and test
    /// the functionality of the <see cref="StarMapSystem"/>. It simulates various
    /// player interactions over time.
    /// Attach this script to any GameObject in your scene to see it in action.
    /// </summary>
    [RequireComponent(typeof(StarMapSystem))] // Ensures StarMapSystem is present on the same GameObject
    public class StarMapTestClient : MonoBehaviour
    {
        private void Start()
        {
            // Start the test sequence in a Coroutine to allow for delays between actions.
            StartCoroutine(TestStarMapSequence());
        }

        private System.Collections.IEnumerator TestStarMapSequence()
        {
            // Give the StarMapSystem's Awake() method a moment to complete and initialize.
            yield return new WaitForSeconds(0.5f);

            Debug.Log("\n--- <color=orange>Star Map Test Client Starting</color> ---");

            if (StarMapSystem.Instance == null)
            {
                Debug.LogError("StarMapSystem instance not found! Make sure it's in the scene and initialized before tests.");
                yield break;
            }

            // --- Phase 1: Initial State (Player starts at Sol, which is discovered) ---
            Debug.Log($"<color=lime>Test:</color> Current System: {StarMapSystem.Instance.CurrentSystem.Name}");
            Debug.Log($"<color=lime>Test:</color> Discovered Systems: {string.Join(", ", StarMapSystem.Instance.GetDiscoveredStarSystems().Select(s => s.Name))}");
            Debug.Log($"<color=lime>Test:</color> Accessible Neighbors: {string.Join(", ", StarMapSystem.Instance.GetAccessibleNeighborSystems().Select(s => s.Name))}");
            yield return new WaitForSeconds(1.5f);

            // --- Phase 2: Attempting Invalid Jumps ---
            Debug.Log("\n--- <color=orange>Test: Attempting Invalid Jumps</color> ---");

            // Attempt to jump to an undiscovered system (Procyon is neighbor of Sirius, which is neighbor of Sol)
            Debug.Log("<color=lime>Test:</color> Trying to jump to Procyon (undiscovered)...");
            StarMapSystem.Instance.JumpToStar("Procyon");
            yield return new WaitForSeconds(1.5f);

            // Attempt to jump to a discovered system that is NOT a direct neighbor
            Debug.Log("<color=lime>Test:</color> Discovering Alpha Centauri...");
            StarMapSystem.Instance.DiscoverStar("AlphaC"); // Discover Alpha Centauri first
            yield return new WaitForSeconds(1.0f);
            Debug.Log("<color=lime>Test:</color> Trying to jump to Barnard's Star (discovered, but not neighbor of Sol)...");
            StarMapSystem.Instance.DiscoverStar("BarnardS"); // Discover Barnard's Star too, for later
            yield return new WaitForSeconds(1.0f);
            StarMapSystem.Instance.JumpToStar("BarnardS");
            yield return new WaitForSeconds(1.5f);

            // Attempt to jump to a system that doesn't exist
            Debug.Log("<color=lime>Test:</color> Trying to jump to 'UnknownSystem' (does not exist)...");
            StarMapSystem.Instance.JumpToStar("UnknownSystem");
            yield return new WaitForSeconds(1.5f);

            // --- Phase 3: Discovering and Successfully Jumping ---
            Debug.Log("\n--- <color=orange>Test: Discovering and Jumping Sequence</color> ---");

            // From Sol, jump to Alpha Centauri
            Debug.Log("<color=lime>Test:</color> Jumping to Alpha Centauri (already discovered)...");
            StarMapSystem.Instance.JumpToStar("AlphaC");
            yield return new WaitForSeconds(1.5f);

            // From Alpha Centauri, jump to Barnard's Star
            Debug.Log("<color=lime>Test:</color> Jumping to Barnard's Star (already discovered)...");
            StarMapSystem.Instance.JumpToStar("BarnardS");
            yield return new WaitForSeconds(1.5f);

            // From Barnard's Star, jump back to Alpha Centauri
            Debug.Log("<color=lime>Test:</color> Jumping back to Alpha Centauri...");
            StarMapSystem.Instance.JumpToStar("AlphaC");
            yield return new WaitForSeconds(1.5f);

            // From Alpha Centauri, jump back to Sol
            Debug.Log("<color=lime>Test:</color> Jumping back to Sol...");
            StarMapSystem.Instance.JumpToStar("Sol");
            yield return new WaitForSeconds(1.5f);

            // Discover Sirius and Procyon, then jump there
            Debug.Log("<color=lime>Test:</color> Discovering Sirius...");
            StarMapSystem.Instance.DiscoverStar("Sirius");
            yield return new WaitForSeconds(1.0f);
            Debug.Log("<color=lime>Test:</color> Jumping to Sirius...");
            StarMapSystem.Instance.JumpToStar("Sirius");
            yield return new WaitForSeconds(1.5f);

            Debug.Log("<color=lime>Test:</color> Discovering Procyon...");
            StarMapSystem.Instance.DiscoverStar("Procyon");
            yield return new WaitForSeconds(1.0f);
            Debug.Log("<color=lime>Test:</color> Jumping to Procyon...");
            StarMapSystem.Instance.JumpToStar("Procyon");
            yield return new WaitForSeconds(1.5f);


            // --- Phase 4: Final State ---
            Debug.Log("\n--- <color=orange>Star Map Test Client Finished</color> ---");
            Debug.Log($"<color=lime>Final State:</color> Current System: {StarMapSystem.Instance.CurrentSystem.Name}");
            Debug.Log($"<color=lime>Final State:</color> All Discovered Systems: {string.Join(", ", StarMapSystem.Instance.GetDiscoveredStarSystems().Select(s => s.Name))}");
            Debug.Log("Check Console output for detailed event logs and UI updates from StarMapDisplayUI during the test sequence.");
        }
    }
}
```