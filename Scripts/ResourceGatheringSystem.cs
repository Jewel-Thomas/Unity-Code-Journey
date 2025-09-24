// Unity Design Pattern Example: ResourceGatheringSystem
// This script demonstrates the ResourceGatheringSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Resource Gathering System** design pattern. It provides a modular and extensible way to define resources, manage a player's inventory, and create interactive resource nodes that can be gathered from.

The pattern breaks down the system into the following key components:

1.  **ResourceDefinition (ScriptableObject):** Defines the types of resources in the game. Using ScriptableObjects allows designers to create new resources directly in the Unity Editor without touching code, promoting extensibility.
2.  **ResourceAmount (Struct):** A simple structure to pair a `ResourceDefinition` with a specific quantity.
3.  **PlayerInventory (MonoBehaviour):** Manages the collection and consumption of resources. It acts as a central repository for all gathered items and provides methods for adding, removing, and checking resource availability. It also uses events for UI updates.
4.  **IResourceGatherable (Interface):** Defines a contract for any game object that can provide resources. This decouples the gatherer from the specific implementation of the resource source (e.g., a tree, a rock, a mine).
5.  **ResourceNode (MonoBehaviour):** A concrete implementation of `IResourceGatherable`. Represents a physical object in the world from which resources can be gathered.
6.  **PlayerGatherer (MonoBehaviour):** Represents the entity (e.g., player character) that interacts with `IResourceGatherable` objects to collect resources. It orchestrates the gathering process over time and adds collected items to the `PlayerInventory`.

This single script (`ResourceGatheringSystemExample.cs`) contains all the necessary classes and interfaces, ready to be dropped into a Unity project.

---

```csharp
// ResourceGatheringSystemExample.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Required for LINQ extensions like .FirstOrDefault()

// ====================================================================================================
// SECTION 1: RESOURCE DEFINITION
// This section defines what a 'resource' is in our game.
// Using a ScriptableObject allows us to define different resource types as assets in Unity.
// This makes the system highly extensible without modifying code.
// ====================================================================================================

/// <summary>
/// A ScriptableObject defining a specific type of resource in the game (e.g., Wood, Stone, Gold).
/// This allows designers to create new resource types directly in the Unity Editor.
/// Benefits:
/// - **Extensibility:** Easily add new resource types without changing code.
/// - **Data-Driven:** Resource properties are managed as assets.
/// - **Designer-Friendly:** Non-programmers can create and configure resources.
/// </summary>
[CreateAssetMenu(fileName = "NewResourceDefinition", menuName = "Resource System/Resource Definition")]
public class ResourceDefinition : ScriptableObject
{
    [Tooltip("The display name of the resource.")]
    public string resourceName = "New Resource";

    [Tooltip("An icon to represent this resource in UI.")]
    public Sprite icon;

    [Tooltip("A color to optionally display this resource (e.g., in debug logs or UI).")]
    public Color displayColor = Color.white;

    public override string ToString()
    {
        return resourceName;
    }
}

/// <summary>
/// A serializable struct to pair a ResourceDefinition with a specific quantity.
/// Useful for specifying resource costs, rewards, or current inventory amounts in the Inspector.
/// </summary>
[System.Serializable]
public struct ResourceAmount
{
    [Tooltip("The type of resource.")]
    public ResourceDefinition resource;

    [Tooltip("The quantity of the resource.")]
    public int amount;

    public ResourceAmount(ResourceDefinition resource, int amount)
    {
        this.resource = resource;
        this.amount = amount;
    }

    public override string ToString()
    {
        return $"{amount} {resource.resourceName}";
    }
}

// ====================================================================================================
// SECTION 2: PLAYER INVENTORY
// This section handles storing and managing resources collected by the player or an entity.
// It acts as the central repository for all gathered resources.
// ====================================================================================================

/// <summary>
/// Manages the player's collected resources. This component can be attached to the player GameObject
/// or a dedicated GameManager. It uses a dictionary for efficient lookup and modification of resource amounts.
/// Design Pattern aspect: This is the central 'Storage' or 'Inventory' part of the system.
/// It encapsulates resource management logic, keeping it separate from gathering or consumption.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // A dictionary to store the current count of each resource type.
    // Key: ResourceDefinition (the type of resource), Value: int (the current amount).
    private Dictionary<ResourceDefinition, int> _resources = new Dictionary<ResourceDefinition, int>();

    // Initial resources for testing or starting the game. Can be set in the Inspector.
    [SerializeField]
    [Tooltip("Initial resources to start with for testing or game setup.")]
    private List<ResourceAmount> _initialResources = new List<ResourceAmount>();

    // Event fired when any single resource amount changes. Useful for updating specific UI elements.
    public event Action<ResourceDefinition, int> OnResourceChanged;
    // Event fired when multiple resources change (e.g., after gathering a batch or a complex crafting operation).
    // Can be used to trigger a full UI refresh.
    public event Action OnAllResourcesChanged;

    void Awake()
    {
        // Initialize inventory with any predefined initial resources.
        foreach (var item in _initialResources)
        {
            if (item.resource != null)
            {
                _resources[item.resource] = item.amount;
            }
        }
        Debug.Log("PlayerInventory Initialized.");
        DisplayInventory(); // Show initial state in console
    }

    /// <summary>
    /// Adds a specified amount of a resource to the inventory.
    /// </summary>
    /// <param name="resource">The type of resource to add.</param>
    /// <param name="amount">The quantity to add.</param>
    public void AddResources(ResourceDefinition resource, int amount)
    {
        if (resource == null || amount <= 0)
        {
            Debug.LogWarning("Attempted to add null resource or non-positive amount.");
            return;
        }

        if (_resources.ContainsKey(resource))
        {
            _resources[resource] += amount;
        }
        else
        {
            _resources.Add(resource, amount);
        }
        Debug.Log($"<color=green>Added {amount} {resource.resourceName}. Total: {_resources[resource]}</color>");
        OnResourceChanged?.Invoke(resource, _resources[resource]); // Notify listeners about specific resource change
        OnAllResourcesChanged?.Invoke(); // Notify listeners about general inventory change
    }

    /// <summary>
    /// Removes a specified amount of a resource from the inventory.
    /// Returns true if removal was successful (sufficient resources existed), false otherwise.
    /// </summary>
    /// <param name="resource">The type of resource to remove.</param>
    /// <param name="amount">The quantity to remove.</param>
    /// <returns>True if resources were successfully removed, false if not enough resources.</returns>
    public bool RemoveResources(ResourceDefinition resource, int amount)
    {
        if (resource == null || amount <= 0)
        {
            Debug.LogWarning("Attempted to remove null resource or non-positive amount.");
            return false;
        }

        if (_resources.ContainsKey(resource) && _resources[resource] >= amount)
        {
            _resources[resource] -= amount;
            Debug.Log($"<color=red>Removed {amount} {resource.resourceName}. Total: {_resources[resource]}</color>");
            OnResourceChanged?.Invoke(resource, _resources[resource]); // Notify listeners
            OnAllResourcesChanged?.Invoke();
            return true;
        }
        Debug.LogWarning($"Attempted to remove {amount} {resource.resourceName} but only had {GetResourceAmount(resource)}.");
        return false;
    }

    /// <summary>
    /// Checks if the inventory contains at least the specified amount of a resource.
    /// </summary>
    /// <param name="resource">The type of resource to check.</param>
    /// <param name="amount">The minimum quantity required.</param>
    /// <returns>True if enough resources are available, false otherwise.</returns>
    public bool HasResources(ResourceDefinition resource, int amount)
    {
        if (resource == null) return false;
        return _resources.TryGetValue(resource, out int currentAmount) && currentAmount >= amount;
    }

    /// <summary>
    /// Gets the current amount of a specific resource in the inventory.
    /// </summary>
    /// <param name="resource">The type of resource to query.</param>
    /// <returns>The current amount of the resource, or 0 if not present.</returns>
    public int GetResourceAmount(ResourceDefinition resource)
    {
        if (resource == null) return 0;
        return _resources.TryGetValue(resource, out int amount) ? amount : 0;
    }

    /// <summary>
    /// Logs the current state of the inventory to the console for debugging.
    /// </summary>
    public void DisplayInventory()
    {
        Debug.Log("--- Player Inventory ---");
        if (_resources.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }
        foreach (var pair in _resources)
        {
            // Use ColorUtility for more robust color logging
            string hexColor = ColorUtility.ToHtmlStringRGB(pair.Key.displayColor);
            Debug.Log($"<color=#{hexColor}>{pair.Key.resourceName}: {pair.Value}</color>");
        }
        Debug.Log("------------------------");
    }
}

// ====================================================================================================
// SECTION 3: RESOURCE GATHERING INTERFACE & NODE
// This section defines how resources can be gathered from game objects.
// An interface ensures that any gatherable object adheres to a common contract.
// ResourceNode is a concrete example of a gatherable object.
// Design Pattern aspect: IResourceGatherable is the 'Resource Source' abstraction.
// It allows different types of resource providers (trees, rocks, mines) to be treated uniformly
// by the gatherer, showcasing polymorphism and decoupling.
// ====================================================================================================

/// <summary>
/// Interface for any object in the game world that can provide resources.
/// This decouples the gatherer from the specific implementation of the resource source.
/// </summary>
public interface IResourceGatherable
{
    /// <summary>
    /// Indicates if this resource node can currently be gathered from.
    /// </summary>
    bool CanGather { get; }

    /// <summary>
    /// The time (in seconds) it takes to perform one gathering action on this node.
    /// </summary>
    float GatherTime { get; }

    /// <summary>
    /// Performs a gathering action, reducing the node's resources and returning the gathered items.
    /// </summary>
    /// <param name="gathererStrength">An optional parameter representing the strength or efficiency of the gatherer.</param>
    /// <returns>A list of resources and their amounts that were successfully gathered in this action.</returns>
    List<ResourceAmount> GatherResources(int gathererStrength);

    /// <summary>
    /// Returns a world position suitable for a gatherer to stand at or target.
    /// </summary>
    Vector3 GetGatherPosition();

    /// <summary>
    /// Event fired when the gatherable node is depleted (runs out of resources).
    /// </summary>
    event Action OnDepleted;
}

/// <summary>
/// A concrete implementation of an IResourceGatherable. Represents a node in the world
/// (e.g., a tree, a rock) that provides a specific resource.
/// </summary>
public class ResourceNode : MonoBehaviour, IResourceGatherable
{
    [Header("Resource Settings")]
    [Tooltip("The type of resource this node provides.")]
    [SerializeField] private ResourceDefinition _resourceType;

    [Tooltip("The amount of resource gathered per single successful gathering action.")]
    [SerializeField] private int _amountPerGather = 1;

    [Tooltip("The total amount of resource this node contains before it is depleted.")]
    [SerializeField] private int _totalResourceAmount = 10;

    [Tooltip("The time (in seconds) it takes to gather from this node once.")]
    [SerializeField] private float _gatherTime = 1.5f;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("GameObject to deactivate or destroy when the node is depleted. If null, defaults to this GameObject.")]
    [SerializeField] private GameObject _visualGameObject;

    private int _currentResourceAmount;
    public bool CanGather => _currentResourceAmount > 0;
    public float GatherTime => _gatherTime;

    public event Action OnDepleted; // Implementation of IResourceGatherable event

    void Awake()
    {
        _currentResourceAmount = _totalResourceAmount;
        if (_visualGameObject == null)
        {
            _visualGameObject = gameObject; // Default to self if not set
        }
        UpdateVisuals(); // Set initial visual state
        Debug.Log($"Resource Node '{(_resourceType != null ? _resourceType.resourceName : "UNDEFINED")}' initialized with {_currentResourceAmount} resources.");
    }

    /// <summary>
    /// Performs a gathering action on this node.
    /// </summary>
    /// <param name="gathererStrength">The strength/efficiency of the gatherer.</param>
    /// <returns>A list of resources gathered. Returns an empty list if unable to gather.</returns>
    public List<ResourceAmount> GatherResources(int gathererStrength = 1)
    {
        if (!CanGather || _resourceType == null)
        {
            Debug.LogWarning($"Tried to gather from depleted or undefined node: {gameObject.name}");
            return new List<ResourceAmount>();
        }

        // Calculate actual amount to gather, considering node's remaining resources and gatherer strength.
        // For simplicity, gathererStrength just multiplies the base amount per gather.
        int effectiveAmountPerGather = _amountPerGather * gathererStrength;
        int actualAmountToGather = Mathf.Min(effectiveAmountPerGather, _currentResourceAmount);

        _currentResourceAmount -= actualAmountToGather;
        Debug.Log($"Gathered {actualAmountToGather} {_resourceType.resourceName} from {gameObject.name}. Remaining: {_currentResourceAmount}");

        UpdateVisuals(); // Update visual state (e.g., deactivate if depleted)

        if (_currentResourceAmount <= 0)
        {
            Debug.Log($"<color=red>Resource node '{gameObject.name}' depleted!</color>");
            OnDepleted?.Invoke(); // Notify subscribers (e.g., the PlayerGatherer)
        }

        // Return a list containing the resources gathered in this single action.
        return new List<ResourceAmount> { new ResourceAmount(_resourceType, actualAmountToGather) };
    }

    /// <summary>
    /// Provides a position where a gatherer might stand or aim.
    /// </summary>
    public Vector3 GetGatherPosition()
    {
        return transform.position; // Can be customized (e.g., return a child transform's position)
    }

    /// <summary>
    /// Updates the visual state of the resource node based on its remaining resources.
    /// </summary>
    private void UpdateVisuals()
    {
        if (_visualGameObject != null)
        {
            _visualGameObject.SetActive(CanGather); // Deactivate visuals if node is depleted
            // Optional: Implement visual changes based on depletion percentage (e.g., fading, different model)
            // Example:
            // Renderer renderer = _visualGameObject.GetComponent<Renderer>();
            // if (renderer != null && _totalResourceAmount > 0)
            // {
            //    float depletionRatio = (float)_currentResourceAmount / _totalResourceAmount;
            //    renderer.material.color = Color.Lerp(Color.gray, _resourceType.displayColor, depletionRatio);
            // }
        }
    }

    // Unity Editor Gizmos for visual debugging
    void OnDrawGizmos()
    {
        if (_resourceType != null)
        {
            Gizmos.color = _resourceType.displayColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            #if UNITY_EDITOR
            // Display resource info in the scene view
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{_resourceType.resourceName} ({_currentResourceAmount}/{_totalResourceAmount})");
            #endif
        }
    }
}


// ====================================================================================================
// SECTION 4: PLAYER GATHERER
// This section demonstrates how a player or AI agent interacts with gatherable resources.
// It orchestrates the gathering process by finding nodes, initiating gathering,
// and adding resources to the player's inventory.
// Design Pattern aspect: This is the 'Gatherer' or 'Client' of the Resource Gathering System.
// It interacts with IResourceGatherable objects, effectively driving the gathering process
// and integrating with the PlayerInventory.
// ====================================================================================================

/// <summary>
/// This component represents the player character's ability to gather resources.
/// It detects nearby gatherable nodes and manages the gathering process over time.
/// </summary>
[RequireComponent(typeof(PlayerInventory))] // PlayerGatherer needs a PlayerInventory on the same GameObject
public class PlayerGatherer : MonoBehaviour
{
    [Header("Gatherer Settings")]
    [Tooltip("Reference to the player's inventory where gathered resources will be stored.")]
    [SerializeField] private PlayerInventory _playerInventory;

    [Tooltip("The range within which the player can detect and gather from resource nodes.")]
    [SerializeField] private float _gatherRange = 3f;

    [Tooltip("The strength or efficiency of the gatherer. Affects amount gathered per action.")]
    [SerializeField] private int _gathererStrength = 1;

    [Tooltip("The layer(s) on which resource nodes are located. Crucial for physics detection.")]
    [SerializeField] private LayerMask _resourceNodeLayer;

    private IResourceGatherable _currentTargetNode;
    private Coroutine _gatheringCoroutine;

    void Start()
    {
        // Automatically find PlayerInventory if not assigned in the Inspector.
        if (_playerInventory == null)
        {
            _playerInventory = GetComponent<PlayerInventory>();
            if (_playerInventory == null)
            {
                Debug.LogError("PlayerGatherer requires a PlayerInventory component on the same GameObject or a reference to one.", this);
                enabled = false; // Disable this component if essential dependencies are missing
            }
        }
    }

    void Update()
    {
        // Example: Detect nearest gatherable node and start gathering on 'E' press.
        if (Input.GetKeyDown(KeyCode.E))
        {
            IResourceGatherable nearestNode = FindNearestGatherableNode();

            if (nearestNode != null && nearestNode.CanGather)
            {
                // If we're already gathering from a different node, stop that first.
                if (_currentTargetNode != null && _currentTargetNode != nearestNode)
                {
                    StopGathering();
                }

                // If not currently gathering or targeting the same node, start a new gathering process.
                if (_gatheringCoroutine == null)
                {
                    _currentTargetNode = nearestNode;
                    Debug.Log($"<color=blue>Starting to gather from: {nearestNode.GetGatherPosition()} ({nearestNode.GatherTime}s per cycle)</color>");
                    _gatheringCoroutine = StartCoroutine(GatheringProcess(_currentTargetNode));
                }
            }
            else if (nearestNode == null)
            {
                Debug.Log("No gatherable resource node found in range.");
                StopGathering(); // Stop any ongoing gathering if no target is found
            }
            else if (!nearestNode.CanGather)
            {
                Debug.Log($"Target resource node '{((MonoBehaviour)nearestNode).name}' is depleted.");
                StopGathering(); // Stop if the found target is depleted
            }
        }

        // Example: Stop gathering on 'Q' press
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StopGathering();
        }
    }

    /// <summary>
    /// Finds the nearest gatherable resource node within the gather range using Physics.OverlapSphere.
    /// </summary>
    /// <returns>The nearest IResourceGatherable node, or null if none found.</returns>
    private IResourceGatherable FindNearestGatherableNode()
    {
        // Use OverlapSphere to find all colliders within the gather range on the specified layer.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _gatherRange, _resourceNodeLayer);
        IResourceGatherable nearestGatherable = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            // Try to get the IResourceGatherable component from the collider's GameObject.
            if (hitCollider.TryGetComponent(out IResourceGatherable gatherable))
            {
                if (gatherable.CanGather) // Only consider nodes that still have resources
                {
                    float distance = Vector3.Distance(transform.position, gatherable.GetGatherPosition());
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestGatherable = gatherable;
                    }
                }
            }
        }
        return nearestGatherable;
    }

    /// <summary>
    /// Coroutine that simulates the continuous gathering process from a resource node.
    /// This runs for as long as the node has resources and the gathering isn't explicitly stopped.
    /// </summary>
    /// <param name="targetNode">The IResourceGatherable node to gather from.</param>
    private IEnumerator GatheringProcess(IResourceGatherable targetNode)
    {
        // Subscribe to the node's depletion event to automatically stop gathering.
        targetNode.OnDepleted += OnTargetNodeDepleted;

        while (targetNode != null && targetNode.CanGather)
        {
            // Wait for the specific gather time of the current node.
            yield return new WaitForSeconds(targetNode.GatherTime);

            // Perform the gather action on the node. The node itself handles reducing its internal count.
            List<ResourceAmount> gatheredResources = targetNode.GatherResources(_gathererStrength);

            // Add the gathered resources to the player's inventory.
            foreach (var item in gatheredResources)
            {
                if (item.resource != null && item.amount > 0)
                {
                    _playerInventory.AddResources(item.resource, item.amount);
                }
            }

            // The loop condition (targetNode.CanGather) will naturally check if the node is depleted
            // after the last gather cycle. The OnDepleted event also stops the coroutine preemptively.
        }

        Debug.Log($"<color=yellow>Gathering from {((MonoBehaviour)targetNode).name} finished or stopped.</color>");
        StopGathering(); // Ensure cleanup if the coroutine exits naturally (e.g., node depleted)
    }

    /// <summary>
    /// Callback method for when the currently targeted resource node runs out of resources.
    /// </summary>
    private void OnTargetNodeDepleted()
    {
        Debug.Log("Target node was depleted. Stopping gathering.");
        StopGathering();
    }

    /// <summary>
    /// Stops any ongoing gathering process and cleans up references and event subscriptions.
    /// </summary>
    public void StopGathering()
    {
        if (_gatheringCoroutine != null)
        {
            StopCoroutine(_gatheringCoroutine);
            _gatheringCoroutine = null;
            Debug.Log("<color=yellow>Gathering process stopped by player or depletion.</color>");
        }

        // Always unsubscribe from events to prevent memory leaks, especially important if targetNode might be destroyed.
        if (_currentTargetNode != null)
        {
            _currentTargetNode.OnDepleted -= OnTargetNodeDepleted;
            _currentTargetNode = null; // Clear target reference
        }
    }

    /// <summary>
    /// Ensures the gathering coroutine stops if the GameObject is disabled or destroyed.
    /// </summary>
    void OnDisable()
    {
        StopGathering();
    }

    /// <summary>
    /// Draws a sphere in the Unity Editor scene view to visualize the gather range.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _gatherRange);
    }
}


// ====================================================================================================
// EXAMPLE USAGE IN UNITY EDITOR (Please read carefully):
//
// This section provides step-by-step instructions on how to set up and use this Resource Gathering System
// in your Unity project.
//
// 1.  Create a new C# script named 'ResourceGatheringSystemExample.cs' in your Unity project.
//     Copy and paste all the code above into this script.
//
// 2.  Create ResourceDefinition ScriptableObjects:
//     - Go to 'Assets -> Create -> Resource System -> Resource Definition'.
//     - Create at least two instances: e.g., 'WoodResource' and 'StoneResource'.
//     - For 'WoodResource':
//         - Set 'Resource Name' to "Wood".
//         - Optionally set 'Icon' and 'Display Color' (e.g., a brown color).
//     - For 'StoneResource':
//         - Set 'Resource Name' to "Stone".
//         - Optionally set 'Icon' and 'Display Color' (e.g., a gray color).
//     - You can create more for Gold, Iron, etc.
//
// 3.  Setup a dedicated Layer for Resource Nodes:
//     - Go to 'Edit -> Project Settings -> Tags and Layers'.
//     - Under 'Layers', find an empty 'User Layer' (e.g., User Layer 8).
//     - Name it 'ResourceNodes'. This is crucial for the PlayerGatherer's detection.
//
// 4.  Setup the Player GameObject:
//     - Create an empty GameObject in your scene named 'Player'.
//     - Add the 'PlayerGatherer' component to it.
//     - Add the 'PlayerInventory' component to it (PlayerGatherer will automatically find it).
//     - In the 'PlayerGatherer' component:
//         - Set 'Gather Range' (e.g., 5). This is the radius for finding nodes.
//         - Set 'Gatherer Strength' (e.g., 1). This multiplies the 'Amount Per Gather' from nodes.
//         - Crucially, select the 'Resource Node Layer' you created earlier ('ResourceNodes') in the dropdown.
//     - In the 'PlayerInventory' component:
//         - You can add some 'Initial Resources' for testing (e.g., add 'WoodResource' with amount 5).
//
// 5.  Setup Resource Nodes (e.g., a Tree and a Rock):
//
//     a) For a Tree (Wood Resource):
//        - Create an empty GameObject in your scene named 'Tree1'.
//        - Add the 'ResourceNode' component to it.
//        - In the 'ResourceNode' component:
//            - Drag your 'WoodResource' ScriptableObject into the 'Resource Type' field.
//            - Set 'Amount Per Gather' (e.g., 2).
//            - Set 'Total Resource Amount' (e.g., 10).
//            - Set 'Gather Time' (e.g., 2.0 seconds).
//            - (Optional) Create a 3D Cube (GameObject -> 3D Object -> Cube) as a child of Tree1, rename it "Visual",
//              scale it to resemble a tree trunk (e.g., Y: 2, X/Z: 0.5), and drag it into the 'Visual GameObject' field.
//        - **VERY IMPORTANT:** Select the 'Tree1' GameObject and assign the 'ResourceNodes' Layer to it
//          using the dropdown at the top of the Inspector (next to 'Tag').
//
//     b) For a Rock (Stone Resource):
//        - Create an empty GameObject named 'Rock1'.
//        - Add the 'ResourceNode' component.
//        - In the 'ResourceNode' component:
//            - Drag your 'StoneResource' ScriptableObject into 'Resource Type'.
//            - Set its amounts and gather time (e.g., 1 per gather, 15 total, 3.0 gather time).
//            - (Optional) Create a 3D Sphere as a child, rename it "Visual", scale it, and assign to 'Visual GameObject'.
//        - **VERY IMPORTANT:** Assign the 'ResourceNodes' Layer to the 'Rock1' GameObject.
//
// 6.  Run the Scene:
//     - Position your 'Player' GameObject near 'Tree1' or 'Rock1' (within the yellow Gizmo sphere of PlayerGatherer).
//     - Press 'Play'.
//     - In the Game view, observe the scene.
//     - Press 'E' to start gathering from the nearest node.
//         - Observe the console logs for gathering progress and inventory updates.
//         - You'll see "Added X Wood. Total: Y" messages.
//     - Press 'Q' to stop gathering at any time.
//     - When a node is depleted (its 'Total Resource Amount' reaches 0), the logs will indicate it,
//       and its 'Visual GameObject' (if set) will deactivate, showing it's no longer gatherable.
//     - You can move your Player and target a different node with 'E'.
//     - Use the 'Display Inventory' button on the 'PlayerInventory' component (in Inspector while playing) to see current resources.
//
// This setup provides a complete, working example of the Resource Gathering System pattern.
// It leverages ScriptableObjects for flexible resource definitions, a PlayerInventory for centralized storage,
// an IResourceGatherable interface for abstraction, and concrete ResourceNodes and a PlayerGatherer
// component for interaction, demonstrating practical Unity development best practices.
// ====================================================================================================

```