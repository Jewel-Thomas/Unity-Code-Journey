// Unity Design Pattern Example: DependencyGraph
// This script demonstrates the DependencyGraph pattern in Unity
// Generated automatically - ready to use in your Unity project

The Dependency Graph pattern is a powerful way to manage complex dependencies between tasks, systems, or data. In Unity, this pattern is particularly useful for:
*   **Initialization Order:** Ensuring certain game systems or components initialize before others.
*   **Resource Loading:** Loading assets in the correct sequence (e.g., textures before materials, materials before models).
*   **Quest Systems:** Defining prerequisites for quests.
*   **AI Behavior Trees (advanced):** Managing complex behavior flows.
*   **Build Pipelines:** Ordering compilation or asset generation steps.

This example provides a complete C# Unity implementation of a generic Dependency Graph, including topological sorting (to get a valid execution order) and cycle detection (to prevent infinite loops).

---

### Understanding the DependencyGraph Pattern

A Dependency Graph is a directed graph where:
*   **Nodes:** Represent individual items (e.g., a game system, a quest, an asset).
*   **Edges (Dependencies):** Represent that one node *depends on* another. An edge from A to B means "A depends on B" (B must be processed/initialized before A).

**Key Operations:**
1.  **Add Node:** Introduce a new item to the graph.
2.  **Add Dependency:** Define an edge between two nodes.
3.  **Topological Sort:** Arrange the nodes in a linear order such that if node A depends on node B, then B appears before A in the list. This gives a valid execution or processing order.
4.  **Cycle Detection:** Identify if there's a circular dependency (e.g., A depends on B, B depends on C, and C depends on A). Cycles prevent a valid topological sort from being generated, as there's no way to resolve the order.

---

### Practical Unity Use Case: Game System Initialization Order

We'll demonstrate the Dependency Graph by managing the initialization order of various `GameSystem` components (e.g., `AudioManager`, `UIManager`, `InputManager`). Some systems might rely on others being ready first.

**Example Scenario:**
*   `UIManager` needs `LocalizationManager` (to display localized text) and `InputManager` (to handle UI interactions).
*   `SaveLoadManager` needs `LocalizationManager` (to save/load localized settings).
*   `AudioManager` has no initial dependencies.

---

### 1. `DependencyGraph.cs` (Core Pattern Implementation)

This script implements the generic `DependencyGraph` class, along with methods for adding nodes, adding dependencies, performing a topological sort, and detecting cycles.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine; // Only for Debug.Log, can be removed if used in non-Unity contexts

/// <summary>
/// Implements the Dependency Graph design pattern.
/// This generic class manages dependencies between objects of type T,
/// allowing for topological sorting to determine a valid processing order
/// and detecting circular dependencies (cycles).
/// </summary>
/// <typeparam name="T">The type of items (nodes) in the graph.
/// It's recommended that T implements Equals() and GetHashCode() properly
/// if it's a value type or a custom reference type that needs value-based equality.</typeparam>
public class DependencyGraph<T>
{
    // Adjacency list: Key (dependent) -> List of Values (dependencies).
    // If A depends on B and C, then _dependencies[A] = {B, C}.
    // This means A needs B and C to be processed/initialized BEFORE A.
    private Dictionary<T, HashSet<T>> _dependencies;

    // Reverse adjacency list: Key (dependency) -> List of Values (dependents).
    // If A depends on B, then _reverseDependencies[B] = {A}.
    // This helps in efficiently finding nodes that need a specific dependency,
    // which is crucial for algorithms like Kahn's for topological sort.
    private Dictionary<T, HashSet<T>> _reverseDependencies;

    // Stores all unique nodes currently in the graph.
    private HashSet<T> _allNodes;

    /// <summary>
    /// Initializes a new instance of the DependencyGraph.
    /// </summary>
    public DependencyGraph()
    {
        _dependencies = new Dictionary<T, HashSet<T>>();
        _reverseDependencies = new Dictionary<T, HashSet<T>>();
        _allNodes = new HashSet<T>();
    }

    /// <summary>
    /// Adds a node to the graph. If the node already exists, nothing happens.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(T node)
    {
        if (_allNodes.Add(node)) // Add returns true if the element was added (i.e., it was new)
        {
            if (!_dependencies.ContainsKey(node))
            {
                _dependencies.Add(node, new HashSet<T>());
            }
            if (!_reverseDependencies.ContainsKey(node))
            {
                _reverseDependencies.Add(node, new HashSet<T>());
            }
        }
    }

    /// <summary>
    /// Adds a dependency relationship: 'dependent' needs 'dependency' to be processed first.
    /// Both 'dependent' and 'dependency' nodes are automatically added to the graph if they don't exist.
    /// </summary>
    /// <param name="dependent">The node that needs 'dependency'.</param>
    /// <param name="dependency">The node that 'dependent' relies on.</param>
    /// <returns>True if the dependency was added successfully, false if it already existed.</returns>
    public bool AddDependency(T dependent, T dependency)
    {
        // Ensure both nodes exist in the graph
        AddNode(dependent);
        AddNode(dependency);

        // Add to forward dependencies: dependent needs dependency
        if (!_dependencies[dependent].Add(dependency))
        {
            // Dependency already existed
            return false;
        }

        // Add to reverse dependencies: dependency is needed by dependent
        _reverseDependencies[dependency].Add(dependent);
        return true;
    }

    /// <summary>
    /// Removes a node and all its associated dependencies from the graph.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveNode(T node)
    {
        if (!_allNodes.Contains(node)) return;

        _allNodes.Remove(node);
        _dependencies.Remove(node);
        _reverseDependencies.Remove(node);

        // Remove node from other nodes' dependency lists
        foreach (var entry in _dependencies)
        {
            entry.Value.Remove(node);
        }
        foreach (var entry in _reverseDependencies)
        {
            entry.Value.Remove(node);
        }
    }

    /// <summary>
    /// Removes a specific dependency relationship.
    /// </summary>
    /// <param name="dependent">The dependent node.</param>
    /// <param name="dependency">The node it depends on.</param>
    /// <returns>True if the dependency was removed, false if it didn't exist.</returns>
    public bool RemoveDependency(T dependent, T dependency)
    {
        if (_dependencies.TryGetValue(dependent, out var deps))
        {
            if (deps.Remove(dependency))
            {
                if (_reverseDependencies.TryGetValue(dependency, out var reverseDeps))
                {
                    reverseDeps.Remove(dependent);
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a given node exists in the graph.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node exists, false otherwise.</returns>
    public bool HasNode(T node)
    {
        return _allNodes.Contains(node);
    }

    /// <summary>
    /// Gets all direct dependencies of a given node (what it needs).
    /// </summary>
    /// <param name="node">The node to query.</param>
    /// <returns>A read-only collection of direct dependencies, or an empty collection if the node doesn't exist or has no dependencies.</returns>
    public IReadOnlyCollection<T> GetDependencies(T node)
    {
        if (_dependencies.TryGetValue(node, out var deps))
        {
            return deps;
        }
        return new HashSet<T>();
    }

    /// <summary>
    /// Gets all nodes that directly depend on the given node (who needs it).
    /// </summary>
    /// <param name="node">The node to query.</param>
    /// <returns>A read-only collection of direct dependents, or an empty collection if the node doesn't exist or no nodes depend on it.</returns>
    public IReadOnlyCollection<T> GetDependents(T node)
    {
        if (_reverseDependencies.TryGetValue(node, out var dependents))
        {
            return dependents;
        }
        return new HashSet<T>();
    }

    /// <summary>
    /// Performs a topological sort on the graph using Kahn's algorithm.
    /// This method finds a valid processing order for all nodes based on their dependencies.
    /// </summary>
    /// <returns>
    /// A tuple containing:
    ///   - A List<T> representing the topologically sorted order of nodes.
    ///   - A boolean indicating if a cycle was detected (true if cycle exists, false otherwise).
    ///     If a cycle is detected, the returned list might be incomplete.
    /// </returns>
    public (List<T> sortedList, bool hasCycle) TopologicalSort()
    {
        List<T> sortedList = new List<T>();
        
        // Calculate in-degrees for all nodes.
        // In-degree for a node 'A' is the number of nodes 'B' such that A depends on B.
        // Or, more accurately for Kahn's, it's the number of nodes that 'A' *depends on*.
        // This means, how many incoming edges does A have FROM its dependencies?
        // Our _dependencies map: Key (dependent) -> List of Values (dependencies).
        // So, inDegrees[dependent] counts the size of _dependencies[dependent].
        Dictionary<T, int> inDegrees = new Dictionary<T, int>();
        foreach (T node in _allNodes)
        {
            inDegrees[node] = _dependencies[node].Count;
        }

        // Initialize a queue with all nodes that have an in-degree of 0.
        // These are the nodes that have no prerequisites and can be processed first.
        Queue<T> queue = new Queue<T>();
        foreach (T node in _allNodes)
        {
            if (inDegrees[node] == 0)
            {
                queue.Enqueue(node);
            }
        }

        // Process nodes until the queue is empty
        while (queue.Count > 0)
        {
            T currentNode = queue.Dequeue();
            sortedList.Add(currentNode);

            // For each node that CURRENTLY depends on currentNode:
            // (i.e., each node in _reverseDependencies[currentNode])
            foreach (T dependentNode in _reverseDependencies[currentNode])
            {
                // Decrement its in-degree because currentNode is now processed.
                inDegrees[dependentNode]--;

                // If its in-degree becomes 0, it means all of its direct dependencies
                // have now been processed, so it can be added to the queue.
                if (inDegrees[dependentNode] == 0)
                {
                    queue.Enqueue(dependentNode);
                }
            }
        }

        // Check for cycles: If the number of nodes in the sorted list
        // is less than the total number of nodes in the graph,
        // it means there's at least one cycle (some nodes could not be processed).
        bool hasCycle = sortedList.Count < _allNodes.Count;

        return (sortedList, hasCycle);
    }

    /// <summary>
    /// Clears all nodes and dependencies, resetting the graph to an empty state.
    /// </summary>
    public void Clear()
    {
        _dependencies.Clear();
        _reverseDependencies.Clear();
        _allNodes.Clear();
    }
}
```

---

### 2. `GameSystem.cs` (Example Nodes)

This abstract base class and its concrete implementations represent the "nodes" in our dependency graph – the game systems that need to be initialized.

```csharp
using UnityEngine;

/// <summary>
/// Abstract base class for a game system that needs initialization.
/// This serves as the 'node' type for our DependencyGraph.
/// </summary>
public abstract class GameSystem
{
    public string Name { get; protected set; }

    // Unique ID for each system instance, useful for debugging and equality if names aren't unique
    public System.Guid Id { get; private set; } = System.Guid.NewGuid();

    public GameSystem()
    {
        // Default constructor for derived classes
    }

    /// <summary>
    /// Initializes the game system. Derived classes must implement this.
    /// </summary>
    public abstract void Initialize();

    public override string ToString()
    {
        return $"GameSystem: {Name} ({Id.ToString().Substring(0, 8)}...)";
    }

    // Important for Dictionary/HashSet if two instances with the same Name should be treated as equal.
    // For this example, we treat distinct instances as distinct nodes, so default object equality is fine.
    // If you wanted to treat systems by name, you'd override Equals and GetHashCode based on Name.
    /*
    public override bool Equals(object obj)
    {
        if (obj is GameSystem other)
        {
            return Name == other.Name;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
    */
}

/// <summary>
/// Concrete implementation of a GameSystem: AudioManager.
/// </summary>
public class AudioManager : GameSystem
{
    public AudioManager() { Name = "Audio Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=cyan>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}

/// <summary>
/// Concrete implementation of a GameSystem: InputManager.
/// </summary>
public class InputManager : GameSystem
{
    public InputManager() { Name = "Input Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=green>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}

/// <summary>
/// Concrete implementation of a GameSystem: UIManager.
/// </summary>
public class UIManager : GameSystem
{
    public UIManager() { Name = "UI Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=magenta>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}

/// <summary>
/// Concrete implementation of a GameSystem: SaveLoadManager.
/// </summary>
public class SaveLoadManager : GameSystem
{
    public SaveLoadManager() { Name = "Save/Load Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=yellow>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}

/// <summary>
/// Concrete implementation of a GameSystem: LocalizationManager.
/// </summary>
public class LocalizationManager : GameSystem
{
    public LocalizationManager() { Name = "Localization Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=blue>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}

/// <summary>
/// Concrete implementation of a GameSystem: AnalyticsManager.
/// </summary>
public class AnalyticsManager : GameSystem
{
    public AnalyticsManager() { Name = "Analytics Manager"; }
    public override void Initialize()
    {
        Debug.Log($"<color=orange>[{Name}] Initializing...</color>");
        // Simulate some initialization work
    }
}
```

---

### 3. `DependencyGraphExample.cs` (Unity MonoBehaviour Demonstrator)

This script is a `MonoBehaviour` that you can attach to any GameObject in a Unity scene. It will set up an example dependency graph using our `GameSystem` classes and then demonstrate its functionality.

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour example demonstrating the DependencyGraph pattern in Unity.
/// This script sets up a graph of GameSystem dependencies, performs a topological sort
/// to determine the correct initialization order, and handles cycle detection.
/// </summary>
public class DependencyGraphExample : MonoBehaviour
{
    // The instance of our generic DependencyGraph
    private DependencyGraph<GameSystem> _systemGraph;

    void Awake()
    {
        Debug.Log("<color=white><b>--- Dependency Graph Example Started ---</b></color>");

        _systemGraph = new DependencyGraph<GameSystem>();

        // 1. Create instances of our GameSystem nodes
        var audioManager = new AudioManager();
        var inputManager = new InputManager();
        var uiManager = new UIManager();
        var saveLoadManager = new SaveLoadManager();
        var localizationManager = new LocalizationManager();
        var analyticsManager = new AnalyticsManager();

        // 2. Add all systems to the graph.
        // (AddDependency will also add nodes, but explicit AddNode can make it clearer)
        _systemGraph.AddNode(audioManager);
        _systemGraph.AddNode(inputManager);
        _systemGraph.AddNode(uiManager);
        _systemGraph.AddNode(saveLoadManager);
        _systemGraph.AddNode(localizationManager);
        _systemGraph.AddNode(analyticsManager);

        Debug.Log("\n<color=white><b>--- Scenario 1: Valid Dependencies ---</b></color>");
        Debug.Log("Defining dependencies:");

        // 3. Define dependencies:
        // AddDependency(dependent, dependency) means 'dependent' needs 'dependency' ready first.
        
        // UIManager needs Localization and Input
        Debug.Log($"- {uiManager.Name} depends on {localizationManager.Name}");
        _systemGraph.AddDependency(uiManager, localizationManager);
        Debug.Log($"- {uiManager.Name} depends on {inputManager.Name}");
        _systemGraph.AddDependency(uiManager, inputManager);

        // SaveLoadManager needs Localization and Analytics
        Debug.Log($"- {saveLoadManager.Name} depends on {localizationManager.Name}");
        _systemGraph.AddDependency(saveLoadManager, localizationManager);
        Debug.Log($"- {saveLoadManager.Name} depends on {analyticsManager.Name}");
        _systemGraph.AddDependency(saveLoadManager, analyticsManager);

        // AudioManager has no dependencies
        // InputManager has no dependencies
        // LocalizationManager has no dependencies
        // AnalyticsManager has no dependencies

        // 4. Perform Topological Sort to get initialization order
        InitializeGameSystems(_systemGraph);

        // --- Demonstrate Cycle Detection ---
        Debug.Log("\n<color=white><b>--- Scenario 2: Introducing a Cycle ---</b></color>");
        Debug.Log("Now, let's create a circular dependency:");
        Debug.Log($"- {localizationManager.Name} depends on {uiManager.Name}");

        // Create a cycle: LocalizationManager now depends on UIManager, but UIManager already depends on LocalizationManager.
        _systemGraph.AddDependency(localizationManager, uiManager);

        // Try to initialize again with the cycle
        InitializeGameSystems(_systemGraph);

        // --- Demonstrate Clearing the Graph and a new valid scenario ---
        Debug.Log("\n<color=white><b>--- Scenario 3: Clearing Graph & New Setup ---</b></color>");
        Debug.Log("Clearing the graph and setting up new, simpler dependencies.");

        _systemGraph.Clear();
        // Re-add nodes as Clear() removes everything
        _systemGraph.AddNode(audioManager);
        _systemGraph.AddNode(inputManager);
        _systemGraph.AddNode(uiManager);

        // New dependencies: UI -> Input, Audio -> Input
        Debug.Log($"- {uiManager.Name} depends on {inputManager.Name}");
        _systemGraph.AddDependency(uiManager, inputManager);
        Debug.Log($"- {audioManager.Name} depends on {inputManager.Name}");
        _systemGraph.AddDependency(audioManager, inputManager);
        
        InitializeGameSystems(_systemGraph);

        Debug.Log("\n<color=white><b>--- Dependency Graph Example Finished ---</b></color>");
    }

    /// <summary>
    /// Helper method to perform the topological sort and initialize systems.
    /// </summary>
    /// <param name="graph">The DependencyGraph to sort and process.</param>
    private void InitializeGameSystems(DependencyGraph<GameSystem> graph)
    {
        Debug.Log("\nAttempting to initialize game systems...");

        (List<GameSystem> sortedSystems, bool hasCycle) = graph.TopologicalSort();

        if (hasCycle)
        {
            Debug.LogError("<color=red><b>ERROR: Circular dependency detected!</b></color>");
            Debug.LogError("Cannot determine a valid initialization order. Please check your system dependencies.");
        }
        else
        {
            Debug.Log("<color=green><b>Successfully determined initialization order:</b></color>");
            foreach (var system in sortedSystems)
            {
                system.Initialize();
            }
            Debug.Log("<color=green>All systems initialized in correct order.</color>");
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `DependencyGraph.cs` and paste the content of the first code block into it.
    *   Create a new C# script named `GameSystem.cs` and paste the content of the second code block (all classes) into it.
    *   Create a new C# script named `DependencyGraphExample.cs` and paste the content of the third code block into it.
2.  **Attach to GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., name it "DependencyGraphDemonstrator").
    *   Drag and drop the `DependencyGraphExample.cs` script onto this GameObject in the Inspector.
3.  **Run the Scene:**
    *   Play your Unity scene.
    *   Open the Console window (`Window > General > Console`) to see the output. You will observe the systems initializing in the correct order, then an error when a cycle is introduced, and finally a new valid order after the graph is reset.

---

### Key Takeaways for Unity Developers:

*   **Modularity:** The `DependencyGraph<T>` class is generic and completely decoupled from `GameSystem`. You can use it for any `T` (e.g., `ScriptableObject` assets, `Quest` classes, `BuildTask` objects).
*   **Initialization Order:** This pattern provides a robust way to manage complex initialization sequences, preventing subtle bugs that arise from components trying to access uninitialized dependencies.
*   **Cycle Detection:** Crucially, it tells you when your design has a logical flaw (a circular dependency), which is much better than runtime errors or deadlocks.
*   **Scalability:** As your project grows, manually managing dependencies becomes impossible. A dependency graph keeps this manageable and verifiable.
*   **Debugging:** The topological sort output explicitly shows the order, making it easier to debug initialization issues.
*   **Alternative Uses:**
    *   **Asset Bundles Loading:** If Bundle A contains a Material that uses a Texture from Bundle B, then Bundle B must be loaded before Bundle A.
    *   **Quest Chains:** Quest A must be completed before Quest B, and Quest B before Quest C.
    *   **Complex Editor Tools:** Steps in a custom build or asset processing pipeline might depend on previous steps.

This example provides a solid foundation for implementing the Dependency Graph pattern in various Unity contexts, making your projects more robust and easier to manage.