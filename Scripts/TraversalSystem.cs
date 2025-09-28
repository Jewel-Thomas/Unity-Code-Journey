// Unity Design Pattern Example: TraversalSystem
// This script demonstrates the TraversalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TraversalSystem' design pattern, while not one of the canonical Gang of Four patterns, represents a common architectural approach in software development, particularly in game development. It's essentially a system designed to traverse a collection of interconnected elements (often referred to as a graph or tree structure) and perform operations on those elements.

In Unity, this often translates to navigating game world entities like waypoints, nodes in a dialogue tree, or parts of a complex object hierarchy. The TraversalSystem combines principles from patterns like **Strategy** (for defining different traversal algorithms) and **Iterator** (for sequentially accessing elements) to create a flexible and extensible way to move through and interact with a data structure.

### Core Components of the TraversalSystem Pattern in this Example:

1.  **`ITraversableNode` (Element Interface):** Defines what it means for an object to be part of the traversable structure. It provides a common interface for accessing node properties and its connections to other nodes.
2.  **`ITraversalStrategy` (Strategy Interface):** Defines the contract for different traversal algorithms (e.g., Depth-First, Breadth-First). Concrete strategies implement this interface to provide specific ways of navigating the nodes.
3.  **Concrete `TraversalStrategy` Implementations:** Classes like `DepthFirstTraversalStrategy` and `BreadthFirstTraversalStrategy` provide the actual algorithms.
4.  **Concrete `ITraversableNode` Implementation (`Waypoint`):** A Unity `MonoBehaviour` that represents a node in our game world. It holds references to other `Waypoint` objects, forming a graph.
5.  **`WaypointTraversalManager` (Context/Client):** A Unity `MonoBehaviour` that orchestrates the traversal. It holds a reference to an `ITraversalStrategy` and uses it to initiate traversals, passing along an `Action` (a delegate) that defines what operation should be performed at each visited node.

This structure allows you to:
*   **Decouple Traversal Logic from Node Data:** Waypoints don't need to know *how* they are traversed.
*   **Easily Swap Traversal Algorithms:** Change from Depth-First to Breadth-First with a single line of code, without modifying the Waypoints or the traversal execution logic.
*   **Define Dynamic Operations:** Perform different actions at each node (logging, activating objects, moving agents) without altering the traversal strategy itself.

---

### Complete C# Unity Example Script

Below is a single C# script ready to be dropped into a Unity project. It defines the TraversalSystem components and demonstrates their usage for navigating a network of `Waypoint` objects.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For List, Stack, Queue, HashSet
using System.Linq; // For LINQ extension methods like .Where()

// Define a namespace for the traversal system components to keep them organized.
namespace UnityTraversalExample
{
    // =====================================================================================
    // TRAVERSAL SYSTEM INTERFACES
    // These interfaces define the contracts for what can be traversed and how it is traversed.
    // =====================================================================================

    /// <summary>
    /// ITraversableNode: The Element Interface
    /// This interface defines what it means for an object to be a 'node' within our traversal system.
    /// Any object that needs to be traversable must implement this interface.
    /// This provides a common contract, decoupling the traversal logic from concrete node types.
    /// </summary>
    public interface ITraversableNode
    {
        // A unique identifier for the node, useful for debugging and tracking.
        string NodeID { get; }

        // The position of the node in the world, useful for Unity-specific operations (e.g., Gizmos).
        Vector3 Position { get; }

        // Returns a collection of other ITraversableNodes that are connected to this node.
        // This is crucial for navigating through the graph/network.
        IEnumerable<ITraversableNode> GetConnections();
    }

    /// <summary>
    /// ITraversalStrategy: The Strategy Interface
    /// This interface defines the contract for different traversal algorithms (strategies).
    /// By implementing this, we can easily swap out how the graph is traversed (e.g., Depth-First, Breadth-First)
    /// without changing the core TraversalManager or the nodes themselves. This is a key aspect of the Strategy Pattern.
    /// </summary>
    public interface ITraversalStrategy
    {
        /// <summary>
        /// Executes the traversal algorithm starting from a given node.
        /// It takes an 'Action' delegate, which defines what operation should be performed
        /// each time a node is visited during the traversal. This makes the traversal
        /// highly flexible, as the operation can be anything (logging, moving an agent,
        /// activating an object, checking conditions, etc.).
        /// </summary>
        /// <param name="startNode">The node from which to begin the traversal.</param>
        /// <param name="onNodeVisited">An action to perform on each node that is visited.</param>
        void ExecuteTraversal(ITraversableNode startNode, Action<ITraversableNode> onNodeVisited);
    }

    // =====================================================================================
    // CONCRETE TRAVERSAL STRATEGIES
    // These classes implement ITraversalStrategy, providing specific algorithms.
    // =====================================================================================

    /// <summary>
    /// DepthFirstTraversalStrategy: Concrete Strategy
    /// Implements a Depth-First Traversal (DFT) algorithm.
    /// DFT explores as far as possible along each branch before backtracking.
    /// It typically uses a Stack data structure for its LIFO (Last-In, First-Out) behavior.
    /// </summary>
    public class DepthFirstTraversalStrategy : ITraversalStrategy
    {
        public void ExecuteTraversal(ITraversableNode startNode, Action<ITraversableNode> onNodeVisited)
        {
            if (startNode == null)
            {
                Debug.LogError("DepthFirstTraversalStrategy: Start node cannot be null.");
                return;
            }

            // A HashSet is used to keep track of visited nodes. This is crucial for:
            // 1. Preventing infinite loops in graphs with cycles.
            // 2. Ensuring each node is processed only once.
            HashSet<ITraversableNode> visited = new HashSet<ITraversableNode>();
            
            // A Stack is used for Depth-First Search. When we pop a node, we visit it,
            // then push its unvisited neighbors onto the stack, effectively diving deeper.
            Stack<ITraversableNode> stack = new Stack<ITraversableNode>();

            stack.Push(startNode); // Start the traversal from the initial node.
            visited.Add(startNode); // Mark the starting node as visited.

            while (stack.Count > 0)
            {
                ITraversableNode currentNode = stack.Pop(); // Get the next node to visit (most recently added).
                onNodeVisited?.Invoke(currentNode);          // Perform the specified action on the current node.

                // Iterate through the connections of the current node.
                // For DFS, we typically add neighbors to the stack to visit them later.
                // The order of adding neighbors can affect the exact DFS path, but all reachable nodes will be visited.
                foreach (ITraversableNode neighbor in currentNode.GetConnections())
                {
                    if (neighbor == null) continue; // Skip null connections to prevent errors.

                    // Only add and visit neighbors that haven't been visited yet.
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor); // Mark as visited *before* pushing to stack to prevent re-adding.
                        stack.Push(neighbor);  // Add to stack to explore its path later.
                    }
                }
            }
        }
    }

    /// <summary>
    /// BreadthFirstTraversalStrategy: Concrete Strategy
    /// Implements a Breadth-First Traversal (BFT) algorithm.
    /// BFT explores all of the neighbor nodes at the present depth before moving on to nodes at the next depth level.
    /// It typically uses a Queue data structure for its FIFO (First-In, First-Out) behavior.
    /// </summary>
    public class BreadthFirstTraversalStrategy : ITraversalStrategy
    {
        public void ExecuteTraversal(ITraversableNode startNode, Action<ITraversableNode> onNodeVisited)
        {
            if (startNode == null)
            {
                Debug.LogError("BreadthFirstTraversalStrategy: Start node cannot be null.");
                return;
            }

            // A HashSet to keep track of visited nodes, similar to DFS, for cycle prevention and uniqueness.
            HashSet<ITraversableNode> visited = new HashSet<ITraversableNode>();
            
            // A Queue is used for Breadth-First Search. Nodes are processed level by level.
            Queue<ITraversableNode> queue = new Queue<ITraversableNode>();

            queue.Enqueue(startNode); // Start the traversal from the initial node.
            visited.Add(startNode);   // Mark the starting node as visited.

            while (queue.Count > 0)
            {
                ITraversableNode currentNode = queue.Dequeue(); // Get the next node to visit (earliest added).
                onNodeVisited?.Invoke(currentNode);              // Perform the specified action on the current node.

                // Iterate through the connections of the current node.
                // For BFS, we add neighbors to the queue to explore them after all nodes at the current level.
                foreach (ITraversableNode neighbor in currentNode.GetConnections())
                {
                    if (neighbor == null) continue; // Skip null connections.

                    // Only add and visit neighbors that haven't been visited yet.
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);   // Mark as visited *before* enqueuing.
                        queue.Enqueue(neighbor); // Add to queue to explore later.
                    }
                }
            }
        }
    }

    // =====================================================================================
    // CONCRETE TRAVERSABLE NODE: WAYPOINT
    // This is a Unity-specific implementation of ITraversableNode, representing a point in the game world.
    // =====================================================================================

    /// <summary>
    /// Waypoint: Concrete Element
    /// This MonoBehaviour acts as a node in our traversable graph. Agents or game logic can use these waypoints
    /// for navigation, pathfinding, or marking points of interest.
    /// </summary>
    [SelectionBase] // Makes selecting the parent object easier in the Scene view
    public class Waypoint : MonoBehaviour, ITraversableNode
    {
        // Public list of connected Waypoints. These will be assigned in the Unity Editor.
        [Tooltip("List of other Waypoints connected to this one. Drag and drop Waypoint GameObjects here.")]
        public List<Waypoint> connections = new List<Waypoint>();

        // Implement ITraversableNode properties.
        public string NodeID => gameObject.name;
        public Vector3 Position => transform.position;

        // Implement GetConnections to return the list of connected Waypoints as ITraversableNodes.
        public IEnumerable<ITraversableNode> GetConnections()
        {
            // Filter out any null connections (e.g., if a connected Waypoint was deleted or unassigned).
            return connections.Where(c => c != null).ToList();
        }

        // --- Editor Visualizations (Gizmos) ---
        // These methods help visualize the waypoints and their connections directly in the Unity editor,
        // making it easier to design and debug your graph.
        private void OnDrawGizmos()
        {
            // Draw a small sphere to represent the Waypoint itself.
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.3f);

            // Draw lines to connected Waypoints.
            if (connections != null)
            {
                Gizmos.color = Color.cyan;
                foreach (Waypoint connection in connections)
                {
                    if (connection != null) // Ensure the connection is not null before drawing.
                    {
                        Gizmos.DrawLine(transform.position, connection.transform.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // When this Waypoint is selected in the editor, draw a larger sphere and highlight connections.
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.5f);

            if (connections != null)
            {
                Gizmos.color = Color.green;
                foreach (Waypoint connection in connections)
                {
                    if (connection != null)
                    {
                        Gizmos.DrawLine(transform.position, connection.transform.position);
                        // Draw a simple arrow to indicate potential direction (visual aid only).
                        DrawArrow(transform.position, connection.transform.position, Color.green);
                    }
                }
            }
        }

        // Helper method to draw a simple arrow head for Gizmos.
        private void DrawArrow(Vector3 start, Vector3 end, Color color)
        {
            Gizmos.color = color;
            Vector3 direction = (end - start).normalized;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
            Gizmos.DrawLine(end, end + right * 0.5f);
            Gizmos.DrawLine(end, end + left * 0.5f);
        }
    }

    // =====================================================================================
    // TRAVERSAL SYSTEM MANAGER (CONTEXT/CLIENT)
    // This class orchestrates the traversal process within Unity.
    // =====================================================================================

    /// <summary>
    /// WaypointTraversalManager: The Context/Client
    /// This MonoBehaviour acts as the 'client' that uses the TraversalSystem.
    /// It holds a reference to an ITraversalStrategy and uses it to perform traversals
    /// based on the selected algorithm and desired starting point.
    /// This is the main component you'll add to an empty GameObject in your scene.
    /// </summary>
    public class WaypointTraversalManager : MonoBehaviour
    {
        [Header("Traversal Settings")]
        [Tooltip("The starting Waypoint for the traversal. Drag a Waypoint GameObject here.")]
        public Waypoint startingWaypoint;
        [Tooltip("Select the desired traversal algorithm (Depth-First or Breadth-First).")]
        public TraversalType traversalType = TraversalType.BreadthFirst;

        // Enum to easily select the traversal type in the Inspector.
        public enum TraversalType { DepthFirst, BreadthFirst }

        private ITraversalStrategy _currentStrategy; // The currently selected traversal strategy.

        void Awake()
        {
            // Initialize the traversal strategy based on the Inspector selection when the game starts.
            SetTraversalStrategy(traversalType);
        }

        // This method allows you to dynamically change the traversal strategy at runtime.
        public void SetTraversalStrategy(TraversalType type)
        {
            switch (type)
            {
                case TraversalType.DepthFirst:
                    _currentStrategy = new DepthFirstTraversalStrategy();
                    break;
                case TraversalType.BreadthFirst:
                    _currentStrategy = new BreadthFirstTraversalStrategy();
                    break;
                default:
                    Debug.LogWarning($"WaypointTraversalManager: Unknown traversal type '{type}', defaulting to BreadthFirst.");
                    _currentStrategy = new BreadthFirstTraversalStrategy();
                    break;
            }
            Debug.Log($"Traversal strategy set to: {_currentStrategy.GetType().Name}");
        }

        /// <summary>
        /// Example usage: Initiates a traversal that simply prints information about each visited waypoint.
        /// This method can be called from other scripts, via a UI button, or using the ContextMenu in the editor.
        /// </summary>
        [ContextMenu("Start Traversal: Print Node Info")]
        public void StartInfoTraversal()
        {
            if (!ValidateTraversalSetup()) return;

            Debug.Log($"--- Starting Traversal: {traversalType} from '{startingWaypoint.NodeID}' (Print Info) ---");

            // Define the action (operation) to perform at each visited node.
            // This is a simple lambda expression that logs the node's ID and position.
            // This 'Action' is passed to the strategy, which executes it for every visited node.
            Action<ITraversableNode> printInfoAction = (node) =>
            {
                Debug.Log($"Visited Node: {node.NodeID} at {node.Position}");
            };

            // Execute the traversal using the currently selected strategy and the defined action.
            _currentStrategy.ExecuteTraversal(startingWaypoint, printInfoAction);

            Debug.Log("--- Traversal Complete ---");
        }

        /// <summary>
        /// Example usage: Initiates a traversal that toggles the renderer component
        /// of each visited waypoint. Demonstrates a more interactive game action.
        /// </summary>
        [ContextMenu("Start Traversal: Toggle Renderers")]
        public void StartToggleRendererTraversal()
        {
            if (!ValidateTraversalSetup()) return;

            Debug.Log($"--- Starting Traversal: {traversalType} from '{startingWaypoint.NodeID}' (Toggle Renderers) ---");

            // Define a more complex action: toggle the renderer of the waypoint GameObject.
            // We need to cast back to 'Waypoint' (our concrete implementation) to access
            // MonoBehaviour-specific components like Renderer. This cast is safe because
            // all ITraversableNode instances in this example are Waypoints.
            Action<ITraversableNode> toggleRendererAction = (node) =>
            {
                if (node is Waypoint waypointNode)
                {
                    Renderer rend = waypointNode.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.enabled = !rend.enabled; // Toggle visibility.
                        Debug.Log($"Toggled renderer for '{waypointNode.NodeID}'. New state: {rend.enabled}");
                    }
                }
            };

            // Execute the traversal.
            _currentStrategy.ExecuteTraversal(startingWaypoint, toggleRendererAction);
            Debug.Log("--- Traversal Complete ---");
        }

        // Helper method to validate that the traversal system is properly set up
        // before attempting to start a traversal.
        private bool ValidateTraversalSetup()
        {
            if (startingWaypoint == null)
            {
                Debug.LogError("WaypointTraversalManager: Starting Waypoint is not set. Please assign one in the Inspector.");
                return false;
            }
            if (_currentStrategy == null)
            {
                // This typically shouldn't happen if Awake() runs correctly.
                Debug.LogError("WaypointTraversalManager: Traversal strategy is not initialized. Check Awake() method.");
                return false;
            }
            return true;
        }

        // --- Editor Utility Methods (Context Menus and Input) ---
        // These methods provide easy ways to test and demonstrate the traversal system
        // directly from the Unity Editor or during runtime with keyboard input.

        [ContextMenu("Set Strategy: Depth-First")]
        public void SetStrategyDepthFirst() { SetTraversalStrategy(TraversalType.DepthFirst); }

        [ContextMenu("Set Strategy: Breadth-First")]
        public void SetStrategyBreadthFirst() { SetTraversalStrategy(TraversalType.BreadthFirst); }

        // Example: Update method to change strategy via key press (for runtime testing).
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                SetTraversalStrategy(TraversalType.DepthFirst);
                StartInfoTraversal();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                SetTraversalStrategy(TraversalType.BreadthFirst);
                StartInfoTraversal();
            }
            if (Input.GetKeyDown(KeyCode.T)) // 'T' for Traverse (using current strategy)
            {
                StartInfoTraversal();
            }
            if (Input.GetKeyDown(KeyCode.R)) // 'R' for Render (using current strategy)
            {
                StartToggleRendererTraversal();
            }
        }
    }
}
```

---

### How to Use This Example in Unity:

1.  **Create a New C# Script:** In your Unity project, create a new C# script named `TraversalSystemExample.cs` (or any name you prefer, as long as it matches the file name).
2.  **Copy and Paste:** Copy the entire code block above and paste it into your new `TraversalSystemExample.cs` script, overwriting any default content.
3.  **Create Waypoints:**
    *   Create several empty GameObjects in your scene (e.g., `Waypoint A`, `Waypoint B`, `Waypoint C`, `Waypoint D`).
    *   Add a `Waypoint` component (from `UnityTraversalExample` namespace) to each of these GameObjects.
    *   Position them distinctively in your scene.
    *   In the Inspector for each `Waypoint`, drag and drop other `Waypoint` GameObjects into their `Connections` list to create a network. For example:
        *   **Waypoint A:** Connects to B, C
        *   **Waypoint B:** Connects to A, D
        *   **Waypoint C:** Connects to A, D
        *   **Waypoint D:** Connects to B, C
    *   (Optional but Recommended): Add a `Sphere` Mesh and a `Renderer` component to each `Waypoint` GameObject to make them visible and interactable with `StartToggleRendererTraversal`.
4.  **Create Traversal Manager:**
    *   Create an empty GameObject in your scene (e.g., `Traversal Manager`).
    *   Add the `WaypointTraversalManager` component (from `UnityTraversalExample` namespace) to this GameObject.
5.  **Configure Traversal Manager:**
    *   In the Inspector for the `Traversal Manager` GameObject:
        *   Drag one of your `Waypoint` GameObjects (e.g., `Waypoint A`) into the `Starting Waypoint` field.
        *   Choose your desired `Traversal Type` (e.g., `BreadthFirst` or `DepthFirst`).
6.  **Run and Test:**
    *   Press Play in the Unity Editor.
    *   Select the `Traversal Manager` GameObject in the Hierarchy.
    *   In the Inspector, you'll see several `[ContextMenu]` buttons:
        *   `Start Traversal: Print Node Info`: Click this to see the traversal order printed in the console.
        *   `Start Traversal: Toggle Renderers`: If your Waypoints have `Renderer` components, click this to see their visibility toggle as they are visited.
        *   You can also use the `Set Strategy: Depth-First` and `Set Strategy: Breadth-First` buttons to change the algorithm.
    *   Alternatively, while in Play mode, press `D` for Depth-First, `B` for Breadth-First, `T` to run the Info Traversal, or `R` to run the Toggle Renderer Traversal. Observe the console output and scene changes.

This example provides a robust and flexible foundation for any graph or tree traversal needs in your Unity projects, clearly demonstrating the benefits of the TraversalSystem pattern.