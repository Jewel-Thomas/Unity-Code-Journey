// Unity Design Pattern Example: TradeRouteSystem
// This script demonstrates the TradeRouteSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TradeRouteSystem' is not a standard, universally recognized GoF (Gang of Four) design pattern. Instead, it's an architectural pattern or system design commonly found in simulation, strategy, or RPG games where resource management, transportation, and economic interactions are central.

This pattern focuses on:
1.  **Graph Representation:** Modeling the game world as a network of interconnected locations (nodes) and pathways (routes).
2.  **Centralized Management:** A dedicated manager class oversees the entire network, handles route finding, dispatches trade units, and orchestrates resource transfers.
3.  **Decoupled Components:** Nodes, routes, and trade units are separate entities, each with specific responsibilities, interacting primarily through the central manager.
4.  **Simulation of Flow:** Managing the movement of goods, resources, or agents along these routes over time.

This example provides a complete, self-contained C# Unity script demonstrating this 'TradeRouteSystem' pattern.

---

**How to Use This Script in Unity:**

1.  **Create a New C# Script:** Name it `TradeRouteSystem.cs` and paste the entire code below into it.
2.  **Create an Empty GameObject:** Name it `TradeRouteManager`. Add the `TradeRouteManager` component to it.
3.  **Create Node GameObjects:**
    *   Create several empty GameObjects (e.g., `Node A`, `Node B`, `Node C`).
    *   Position them in your scene.
    *   Add the `TradeNode` component to each.
    *   Give each node a unique `Node Name`.
    *   In the Inspector for each `TradeNode`, populate its `Initial Resources` with some `ResourceType` and `Amount`.
    *   Crucially, drag and drop other `TradeNode` GameObjects into the `Connected Nodes` list of each node to define the direct routes. This helps the manager build the graph.
4.  **Create Vessel GameObjects:**
    *   Create a few more empty GameObjects (e.g., `Trader 1`, `Trader 2`). You can add a visible mesh (like a Cube) as a child for visualization.
    *   Add the `TradeVessel` component to each.
    *   Give each vessel a `Vessel Name`, `Cargo Capacity`, and `Move Speed`.
5.  **Assign Vessels to Manager:** In the `TradeRouteManager` GameObject's Inspector, drag all your `TradeVessel` GameObjects into the `Available Vessels` list.
6.  **Run the Scene:** The `TradeRouteManager` will automatically register all nodes and vessels, build the trade network, and in its `Start()` method, initiate example trades. You'll see vessels moving between nodes, and resource counts will update.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ queries like .FirstOrDefault(), .Where()

// This script defines the 'TradeRouteSystem' pattern, including:
// - SerializableDictionary: A helper for Unity to serialize Dictionaries.
// - ResourceType: An enum for different types of resources.
// - ResourceAmount: A simple serializable struct/class for resource quantities.
// - TradeNode: Represents a location in the trade network.
// - TradeVessel: Represents a unit that transports resources.
// - TradeRouteManager: The central system managing nodes, routes, and trades.

namespace TradeRouteSystem
{
    // =====================================================================================
    // Helper: Serializable Dictionary for Unity Inspector
    // Unity does not natively serialize Dictionaries. This generic class helps with that.
    // =====================================================================================
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        // Save the dictionary to lists
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                _keys.Add(pair.Key);
                _values.Add(pair.Value);
            }
        }

        // Load the dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();
            if (_keys.Count != _values.Count)
            {
                Debug.LogError("Tried to deserialize a SerializableDictionary, but the amount of keys (" + _keys.Count + ") and values (" + _values.Count + ") was not equal. Skipping.");
                return;
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                // Handle potential duplicate keys if necessary, or simply overwrite
                if (this.ContainsKey(_keys[i]))
                {
                    Debug.LogWarning($"Duplicate key found during deserialization: {_keys[i]}. Overwriting existing value.");
                    this[_keys[i]] = _values[i];
                }
                else
                {
                    this.Add(_keys[i], _values[i]);
                }
            }
        }
    }

    // =====================================================================================
    // Resource Definition
    // Defines the types of resources that can be traded.
    // =====================================================================================
    public enum ResourceType
    {
        Wood,
        Stone,
        Iron,
        Gold,
        Food,
        Water
    }

    // A simple struct/class to hold a resource type and its amount.
    [Serializable]
    public class ResourceAmount
    {
        public ResourceType type;
        public int amount;

        public ResourceAmount(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }

        public override string ToString()
        {
            return $"{amount} {type}";
        }
    }

    // =====================================================================================
    // TradeNode Component
    // Represents a single location (e.g., city, outpost) in the trade network.
    // It manages its own inventory and connections to other nodes.
    // =====================================================================================
    public class TradeNode : MonoBehaviour
    {
        [Tooltip("A unique identifier for this trade node.")]
        [SerializeField] private string _nodeName = "New Node";
        public string NodeName => _nodeName;

        [Tooltip("The initial resources available at this node when the game starts.")]
        [SerializeField] private List<ResourceAmount> _initialResources = new List<ResourceAmount>();

        [Tooltip("Nodes directly connected to this node, forming routes.")]
        [SerializeField] private List<TradeNode> _connectedNodes = new List<TradeNode>();
        public IReadOnlyList<TradeNode> ConnectedNodes => _connectedNodes;

        // Runtime inventory of resources at this node.
        // Using a SerializableDictionary allows initial values to be set in the inspector
        // while providing dictionary functionality at runtime.
        private SerializableDictionary<ResourceType, int> _currentResources = new SerializableDictionary<ResourceType, int>();
        public IReadOnlyDictionary<ResourceType, int> CurrentResources => _currentResources;

        public event Action<TradeNode> OnResourcesChanged;

        private void Awake()
        {
            // Initialize current resources from initial resources
            foreach (var res in _initialResources)
            {
                _currentResources[res.type] = res.amount;
            }
        }

        private void OnEnable()
        {
            // Register this node with the TradeRouteManager when enabled.
            // This ensures the manager knows about all active nodes.
            TradeRouteManager.Instance?.RegisterNode(this);
        }

        private void OnDisable()
        {
            // Unregister this node from the TradeRouteManager if it's disabled or destroyed.
            TradeRouteManager.Instance?.UnregisterNode(this);
        }

        // Tries to deduct resources from this node.
        // Returns true if successful, false if not enough resources.
        public bool DeductResources(ResourceType type, int amount)
        {
            if (!_currentResources.ContainsKey(type) || _currentResources[type] < amount)
            {
                Debug.LogWarning($"Node '{NodeName}': Not enough {type} to deduct {amount}. Has {_currentResources.GetValueOrDefault(type, 0)}.");
                return false;
            }

            _currentResources[type] -= amount;
            Debug.Log($"Node '{NodeName}': Deducted {amount} {type}. Remaining: {_currentResources[type]}");
            OnResourcesChanged?.Invoke(this);
            return true;
        }

        // Adds resources to this node.
        public void AddResources(ResourceType type, int amount)
        {
            if (!_currentResources.ContainsKey(type))
            {
                _currentResources[type] = 0;
            }
            _currentResources[type] += amount;
            Debug.Log($"Node '{NodeName}': Added {amount} {type}. Total: {_currentResources[type]}");
            OnResourcesChanged?.Invoke(this);
        }

        public int GetResourceAmount(ResourceType type)
        {
            return _currentResources.GetValueOrDefault(type, 0);
        }

        // Visualizes connections in the Unity editor.
        private void OnDrawGizmos()
        {
            // Draw a sphere for the node
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.5f);

            // Draw lines to connected nodes
            Gizmos.color = Color.cyan;
            if (_connectedNodes != null)
            {
                foreach (TradeNode connectedNode in _connectedNodes)
                {
                    if (connectedNode != null)
                    {
                        Gizmos.DrawLine(transform.position, connectedNode.transform.position);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"TradeNode({NodeName})";
        }
    }

    // =====================================================================================
    // TradeVessel Component
    // Represents a unit (e.g., ship, caravan) that moves between nodes to transport resources.
    // It receives orders from the TradeRouteManager and executes them.
    // =====================================================================================
    public class TradeVessel : MonoBehaviour
    {
        [Tooltip("A unique identifier for this vessel.")]
        [SerializeField] private string _vesselName = "New Vessel";
        public string VesselName => _vesselName;

        [Tooltip("Maximum amount of resources this vessel can carry.")]
        [SerializeField] private int _cargoCapacity = 100;
        public int CargoCapacity => _cargoCapacity;

        [Tooltip("Movement speed of the vessel (units per second).")]
        [SerializeField] private float _moveSpeed = 5f;

        // Current cargo held by the vessel.
        private SerializableDictionary<ResourceType, int> _currentCargo = new SerializableDictionary<ResourceType, int>();
        public IReadOnlyDictionary<ResourceType, int> CurrentCargo => _currentCargo;

        private List<TradeNode> _currentPath; // The list of nodes forming the route
        private int _currentWaypointIndex; // Index of the next node to move towards
        private TradeNode _sourceNode;
        private TradeNode _destinationNode;
        private ResourceType _tradeResourceType;
        private int _tradeQuantity;

        private bool _isTrading = false;
        public bool IsTrading => _isTrading;

        // Callback when a trade journey is completed.
        public event Action<TradeVessel, TradeNode, TradeNode, ResourceType, int> OnTradeJourneyCompleted;

        public int CurrentCargoWeight
        {
            get
            {
                return _currentCargo.Values.Sum();
            }
        }

        private void OnEnable()
        {
            // Register this vessel with the manager (optional, manager can also discover them)
            TradeRouteManager.Instance?.RegisterVessel(this);
        }

        private void OnDisable()
        {
            TradeRouteManager.Instance?.UnregisterVessel(this);
        }

        private void Update()
        {
            if (!_isTrading || _currentPath == null || _currentPath.Count == 0)
            {
                return;
            }

            MoveAlongPath();
        }

        // Initiates a trade journey for this vessel.
        // The path, resource type, and quantity are provided by the TradeRouteManager.
        public void StartTradeJourney(TradeNode source, TradeNode destination, List<TradeNode> path, ResourceType resourceType, int quantity)
        {
            if (_isTrading)
            {
                Debug.LogWarning($"Vessel '{VesselName}' is already trading. Cannot start new journey.");
                return;
            }
            if (quantity > CargoCapacity)
            {
                Debug.LogError($"Vessel '{VesselName}' cannot carry {quantity} {resourceType}. Capacity is {CargoCapacity}.");
                return;
            }
            if (path == null || path.Count < 2)
            {
                Debug.LogError($"Vessel '{VesselName}': Invalid path provided for trade.");
                return;
            }

            _sourceNode = source;
            _destinationNode = destination;
            _currentPath = path;
            _tradeResourceType = resourceType;
            _tradeQuantity = quantity;
            _currentWaypointIndex = 0; // Start at the first node in the path (source)
            transform.position = _currentPath[_currentWaypointIndex].transform.position; // Snap to source
            _isTrading = true;

            _currentCargo.Clear();
            _currentCargo[_tradeResourceType] = _tradeQuantity;

            Debug.Log($"Vessel '{VesselName}' started trade: {quantity} {resourceType} from {_sourceNode.NodeName} to {_destinationNode.NodeName}.");
            Debug.Log($"Initial cargo: {quantity} {resourceType}");
        }

        // Simulates movement along the assigned path.
        private void MoveAlongPath()
        {
            // If we've reached the final destination
            if (_currentWaypointIndex >= _currentPath.Count - 1)
            {
                CompleteTradeJourney();
                return;
            }

            TradeNode currentTargetNode = _currentPath[_currentWaypointIndex + 1];
            Vector3 targetPosition = currentTargetNode.transform.position;

            // Move towards the next waypoint
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

            // If we've reached the current target node (waypoint)
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                _currentWaypointIndex++;
                Debug.Log($"Vessel '{VesselName}' arrived at waypoint: {currentTargetNode.NodeName}. Next: {_currentPath.ElementAtOrDefault(_currentWaypointIndex + 1)?.NodeName ?? "Final Destination"}");

                // If this is the final destination, complete the trade
                if (_currentWaypointIndex >= _currentPath.Count - 1)
                {
                    CompleteTradeJourney();
                }
            }
        }

        // Handles the completion of a trade journey.
        private void CompleteTradeJourney()
        {
            _isTrading = false;
            Debug.Log($"Vessel '{VesselName}' completed trade journey from {_sourceNode.NodeName} to {_destinationNode.NodeName}.");

            // Clear cargo visually (the actual resource transfer happens via the manager's callback)
            _currentCargo.Clear();

            // Notify the manager that the journey is complete
            OnTradeJourneyCompleted?.Invoke(this, _sourceNode, _destinationNode, _tradeResourceType, _tradeQuantity);

            // Reset for next trade
            _currentPath = null;
            _currentWaypointIndex = 0;
            _sourceNode = null;
            _destinationNode = null;
            _tradeResourceType = default;
            _tradeQuantity = 0;
        }

        public override string ToString()
        {
            return $"TradeVessel({VesselName})";
        }

        // Visualizes the vessel and its current path in the editor.
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(transform.position, Vector3.one * 0.7f);

            if (_isTrading && _currentPath != null && _currentPath.Count > 1)
            {
                Gizmos.color = Color.yellow;
                for (int i = _currentWaypointIndex; i < _currentPath.Count - 1; i++)
                {
                    if (_currentPath[i] != null && _currentPath[i + 1] != null)
                    {
                        Gizmos.DrawLine(_currentPath[i].transform.position, _currentPath[i + 1].transform.position);
                        Gizmos.DrawSphere(_currentPath[i].transform.position, 0.3f); // Mark waypoints
                    }
                }
                Gizmos.DrawSphere(_currentPath.Last().transform.position, 0.3f); // Mark destination
            }
        }
    }

    // =====================================================================================
    // TradeRouteManager Component (Singleton)
    // The central component of the TradeRouteSystem pattern.
    // It manages the network of nodes, finds routes, dispatches vessels,
    // and orchestrates resource transfers.
    // =====================================================================================
    public class TradeRouteManager : MonoBehaviour
    {
        // Singleton pattern for easy access from other parts of the game.
        public static TradeRouteManager Instance { get; private set; }

        [Tooltip("All available trade vessels that the manager can dispatch.")]
        [SerializeField] private List<TradeVessel> _availableVessels = new List<TradeVessel>();

        // Internal list of all registered trade nodes.
        private List<TradeNode> _allTradeNodes = new List<TradeNode>();

        // Adjacency list representing the trade network graph.
        // Key: A TradeNode, Value: List of TradeNodes directly connected to the key node.
        private Dictionary<TradeNode, List<TradeNode>> _adjacencyList = new Dictionary<TradeNode, List<TradeNode>>();

        // List of vessels currently performing a trade journey.
        private List<TradeVessel> _activeTrades = new List<TradeVessel>();

        private void Awake()
        {
            // Enforce Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple TradeRouteManager instances found. Destroying duplicate.");
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Ensure all nodes and vessels are registered and graph is built after Awake
            // (Nodes and Vessels register themselves in OnEnable, but this ensures manager has latest data)
            RefreshNetwork();

            // --- Example Usage: Initiating Trades ---
            Debug.Log("\n--- Initiating Example Trades ---");

            // Find specific nodes by name for the example
            TradeNode nodeA = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node A");
            TradeNode nodeB = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node B");
            TradeNode nodeC = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node C");
            TradeNode nodeD = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node D");

            if (nodeA == null || nodeB == null || nodeC == null || nodeD == null)
            {
                Debug.LogError("Could not find all example nodes (Node A, B, C, D). Please ensure they are created and named correctly.");
                return;
            }

            // Example 1: Basic trade
            InitiateTrade(nodeA, nodeB, ResourceType.Wood, 50);

            // Example 2: Another trade with different resource, slightly delayed to show multiple trades
            Invoke(nameof(InitiateSecondExampleTrade), 5f); // Delay for better visualization

            // Example 3: Trade that requires pathfinding through an intermediate node
            Invoke(nameof(InitiateThirdExampleTrade), 10f);
        }

        private void InitiateSecondExampleTrade()
        {
            TradeNode nodeB = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node B");
            TradeNode nodeC = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node C");
            if (nodeB != null && nodeC != null)
            {
                InitiateTrade(nodeB, nodeC, ResourceType.Gold, 20);
            }
        }

        private void InitiateThirdExampleTrade()
        {
            TradeNode nodeA = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node A");
            TradeNode nodeD = _allTradeNodes.FirstOrDefault(n => n.NodeName == "Node D");
            if (nodeA != null && nodeD != null)
            {
                // This trade might need to go A -> B -> D or A -> C -> D depending on how you connect them.
                InitiateTrade(nodeA, nodeD, ResourceType.Iron, 30);
            }
        }


        // Rebuilds the network graph based on currently active nodes.
        public void RefreshNetwork()
        {
            // Clear existing data
            _allTradeNodes.Clear();
            _adjacencyList.Clear();

            // Discover all active TradeNodes in the scene
            _allTradeNodes.AddRange(FindObjectsOfType<TradeNode>());
            Debug.Log($"Found {_allTradeNodes.Count} TradeNodes.");

            // Build the adjacency list (graph)
            foreach (TradeNode node in _allTradeNodes)
            {
                _adjacencyList[node] = new List<TradeNode>();
                foreach (TradeNode connectedNode in node.ConnectedNodes)
                {
                    if (_allTradeNodes.Contains(connectedNode) && connectedNode != node) // Ensure connected node is also active and not self
                    {
                        _adjacencyList[node].Add(connectedNode);
                    }
                }
            }

            Debug.Log("Trade network graph built.");
            LogNetworkStatus();

            // Ensure vessels are correctly subscribed
            foreach (var vessel in _availableVessels)
            {
                if (vessel != null)
                {
                    vessel.OnTradeJourneyCompleted -= HandleTradeJourneyCompleted; // Unsubscribe to prevent duplicates
                    vessel.OnTradeJourneyCompleted += HandleTradeJourneyCompleted; // Subscribe
                }
            }
        }

        // Registers a TradeNode with the manager. Called by TradeNode.OnEnable().
        public void RegisterNode(TradeNode node)
        {
            if (!_allTradeNodes.Contains(node))
            {
                _allTradeNodes.Add(node);
                // Rebuild graph incrementally or fully refresh if needed
                // For simplicity, we'll rely on RefreshNetwork for full rebuild on Start,
                // or you could call RefreshNetwork() here if nodes are added dynamically during runtime.
            }
        }

        // Unregisters a TradeNode from the manager. Called by TradeNode.OnDisable().
        public void UnregisterNode(TradeNode node)
        {
            _allTradeNodes.Remove(node);
            _adjacencyList.Remove(node);
            // Remove node from other nodes' adjacency lists if it was a connection
            foreach (var kvp in _adjacencyList)
            {
                kvp.Value.Remove(node);
            }
        }

        // Registers a TradeVessel with the manager. Called by TradeVessel.OnEnable().
        public void RegisterVessel(TradeVessel vessel)
        {
            if (!_availableVessels.Contains(vessel))
            {
                _availableVessels.Add(vessel);
                vessel.OnTradeJourneyCompleted -= HandleTradeJourneyCompleted; // Defensive
                vessel.OnTradeJourneyCompleted += HandleTradeJourneyCompleted;
            }
        }

        // Unregisters a TradeVessel from the manager. Called by TradeVessel.OnDisable().
        public void UnregisterVessel(TradeVessel vessel)
        {
            _availableVessels.Remove(vessel);
            _activeTrades.Remove(vessel);
            if (vessel != null)
            {
                vessel.OnTradeJourneyCompleted -= HandleTradeJourneyCompleted;
            }
        }

        // Initiates a trade operation between two nodes.
        // This is the primary public interface for requesting a trade.
        public bool InitiateTrade(TradeNode source, TradeNode destination, ResourceType resourceType, int quantity)
        {
            if (source == null || destination == null)
            {
                Debug.LogError("Source or Destination node is null for trade initiation.");
                return false;
            }
            if (source == destination)
            {
                Debug.LogWarning($"Cannot trade from {source.NodeName} to itself.");
                return false;
            }
            if (quantity <= 0)
            {
                Debug.LogWarning("Trade quantity must be greater than zero.");
                return false;
            }

            Debug.Log($"Attempting trade: {quantity} {resourceType} from {source.NodeName} to {destination.NodeName}");

            // 1. Check if source has enough resources
            if (source.GetResourceAmount(resourceType) < quantity)
            {
                Debug.LogWarning($"Trade failed: {source.NodeName} does not have enough {resourceType}. (Needed {quantity}, Has {source.GetResourceAmount(resourceType)})");
                return false;
            }

            // 2. Find an available vessel
            TradeVessel availableVessel = _availableVessels.FirstOrDefault(v => !v.IsTrading && v.CargoCapacity >= quantity);
            if (availableVessel == null)
            {
                Debug.LogWarning($"Trade failed: No available vessel with capacity for {quantity} {resourceType}.");
                return false;
            }

            // 3. Find a path from source to destination
            List<TradeNode> path = FindPath(source, destination);
            if (path == null || path.Count < 2)
            {
                Debug.LogWarning($"Trade failed: No valid path found from {source.NodeName} to {destination.NodeName}.");
                return false;
            }

            // All checks passed, proceed with trade
            Debug.Log($"Trade approved. Path: {string.Join(" -> ", path.Select(n => n.NodeName))}");

            // Deduct resources from the source node immediately (optimistic deduction)
            // If the vessel is destroyed or trade fails mid-journey, you'd need a rollback mechanism.
            source.DeductResources(resourceType, quantity);

            // Assign the vessel to this trade
            availableVessel.StartTradeJourney(source, destination, path, resourceType, quantity);
            _activeTrades.Add(availableVessel);

            Debug.Log($"Trade of {quantity} {resourceType} from {source.NodeName} to {destination.NodeName} initiated with vessel '{availableVessel.VesselName}'.");
            return true;
        }

        // Pathfinding algorithm (Breadth-First Search for unweighted paths)
        private List<TradeNode> FindPath(TradeNode startNode, TradeNode endNode)
        {
            if (!_adjacencyList.ContainsKey(startNode) || !_adjacencyList.ContainsKey(endNode))
            {
                Debug.LogError("Pathfinding: Start or end node not in adjacency list.");
                return null;
            }

            Queue<TradeNode> queue = new Queue<TradeNode>();
            Dictionary<TradeNode, TradeNode> parents = new Dictionary<TradeNode, TradeNode>();
            HashSet<TradeNode> visited = new HashSet<TradeNode>();

            queue.Enqueue(startNode);
            visited.Add(startNode);
            parents[startNode] = null; // Start node has no parent

            while (queue.Count > 0)
            {
                TradeNode currentNode = queue.Dequeue();

                if (currentNode == endNode)
                {
                    // Path found, reconstruct it
                    List<TradeNode> path = new List<TradeNode>();
                    while (currentNode != null)
                    {
                        path.Add(currentNode);
                        currentNode = parents[currentNode];
                    }
                    path.Reverse(); // Path is from end to start, reverse it
                    return path;
                }

                if (_adjacencyList.TryGetValue(currentNode, out List<TradeNode> neighbors))
                {
                    foreach (TradeNode neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            parents[neighbor] = currentNode;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // No path found
            return null;
        }

        // Callback handler for when a TradeVessel completes its journey.
        private void HandleTradeJourneyCompleted(TradeVessel vessel, TradeNode source, TradeNode destination, ResourceType resourceType, int quantity)
        {
            _activeTrades.Remove(vessel);

            // Add resources to the destination node.
            destination.AddResources(resourceType, quantity);

            Debug.Log($"Trade '{resourceType}' completed by '{vessel.VesselName}': {quantity} {resourceType} delivered to {destination.NodeName}.");
        }

        // Helper to log the current status of the network for debugging.
        private void LogNetworkStatus()
        {
            Debug.Log("--- Trade Network Status ---");
            Debug.Log($"Total Nodes: {_allTradeNodes.Count}");
            foreach (var node in _allTradeNodes)
            {
                string connections = "None";
                if (_adjacencyList.ContainsKey(node) && _adjacencyList[node].Count > 0)
                {
                    connections = string.Join(", ", _adjacencyList[node].Select(n => n.NodeName));
                }
                Debug.Log($"- Node '{node.NodeName}' (Connections: {connections})");
            }
            Debug.Log($"Total Vessels: {_availableVessels.Count}");
            foreach (var vessel in _availableVessels)
            {
                Debug.Log($"- Vessel '{vessel.VesselName}' (Capacity: {vessel.CargoCapacity})");
            }
            Debug.Log("---------------------------");
        }

        // Visualize the entire network in the editor.
        private void OnDrawGizmos()
        {
            if (Instance == null) return; // Only draw if manager is active
            if (_adjacencyList == null || _adjacencyList.Count == 0) return;

            Gizmos.color = Color.gray;
            foreach (var entry in _adjacencyList)
            {
                TradeNode node = entry.Key;
                List<TradeNode> connections = entry.Value;

                if (node == null) continue;

                foreach (TradeNode connectedNode in connections)
                {
                    if (connectedNode == null) continue;
                    // Draw a line for each one-way connection (Gizmos.DrawLine is bidirectional for visualization)
                    Gizmos.DrawLine(node.transform.position, connectedNode.transform.position);
                }
            }
        }
    }
}
```