// Unity Design Pattern Example: ResourceNodeSystem
// This script demonstrates the ResourceNodeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ResourceNodeSystem' design pattern is common in games where players collect resources from the environment, like crafting, base-building, or survival games. It typically involves:

1.  **Resource Types:** Defining different kinds of resources.
2.  **Resource Nodes:** Game objects in the world that hold a specific type and quantity of a resource, often harvestable.
3.  **Harvester:** The entity (usually the player or an AI unit) that interacts with resource nodes to extract resources.
4.  **Inventory/Storage:** Where harvested resources are stored.
5.  **Events/Messaging:** To communicate changes (e.g., resource harvested, node depleted).

This example demonstrates a complete, practical C# Unity implementation of the ResourceNodeSystem, including a `ResourceNode`, a `PlayerHarvester`, and a simple UI to display collected resources.

---

### **ResourceNodeSystem.cs** (Single Script for Easy Drop-in)

To use this, create a new C# script named `ResourceNodeSystem.cs` in your Unity project, copy the entire content below, and save it.

**Setup Instructions:**

1.  **Create an Empty GameObject** in your scene. Name it `GameManager` (or similar) and attach this `ResourceNodeSystem.cs` script to it.
2.  **Player Harvester:**
    *   Create a simple 3D object (e.g., a Capsule, Cube) for your player. Add a `Rigidbody` (if you want physics movement) and a `Capsule Collider`.
    *   Attach the `PlayerHarvester` component (from this script) to your player GameObject.
    *   Set the `Move Speed`, `Interaction Range`, and `Harvest Interval` in the Inspector.
    *   Crucially, set the `Resource Node Layer` to a layer dedicated to resource nodes (e.g., create a layer called "ResourceNodes").
3.  **Resource Nodes:**
    *   Create several 3D objects (e.g., Spheres for "Stone", Cylinders for "Wood").
    *   Add a `Collider` (e.g., Sphere Collider, Box Collider) to each, marking it as **`Is Trigger`**.
    *   Attach the `ResourceNode` component (from this script) to each resource node GameObject.
    *   **Assign properties in the Inspector:**
        *   `Resource Type`: Choose `Wood`, `Stone`, or `Gold`.
        *   `Max Quantity`: E.g., 100 for a large tree.
        *   `Harvest Amount Per Interaction`: E.g., 10 wood per chop.
        *   `Respawn Time`: E.g., 30 seconds for a tree to grow back.
        *   `Depletion Threshold`: E.g., 0. If current quantity drops below this, it's considered visually depleted.
        *   **Visuals:** Assign different materials or child GameObjects for `Active Visual` and `Depleted Visual` to show its state.
    *   **Crucially, set the Layer of each Resource Node GameObject to the "ResourceNodes" layer** (the same one you set on the `PlayerHarvester`).
4.  **UI Setup (Optional but Recommended):**
    *   Create a UI Canvas (`GameObject -> UI -> Canvas`).
    *   Inside the Canvas, create a few UI Text elements (`GameObject -> UI -> Text`).
    *   Rename them to `WoodCountText`, `StoneCountText`, `GoldCountText`.
    *   Create an Empty GameObject under the Canvas (e.g., `ResourceDisplay`).
    *   Attach the `ResourceDisplayUI` component (from this script) to this `ResourceDisplay` GameObject.
    *   Drag and drop your `WoodCountText`, `StoneCountText`, `GoldCountText` into the corresponding fields in the `ResourceDisplayUI` component in the Inspector.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // Required for UI elements like Text

/// <summary>
/// ResourceType Enum: Defines the different types of resources available in the game.
/// This is the core identifier for any resource in the system.
/// </summary>
public enum ResourceType
{
    Wood,
    Stone,
    Gold,
    // Add more resource types as needed
}

/// <summary>
/// HarvestResult Struct: Represents the outcome of a harvesting attempt.
/// Provides detailed information about what was harvested.
/// </summary>
public struct HarvestResult
{
    public ResourceType ResourceType;
    public int AmountHarvested;
    public bool NodeDepleted; // Indicates if the resource node became depleted after this harvest.

    public HarvestResult(ResourceType type, int amount, bool depleted)
    {
        ResourceType = type;
        AmountHarvested = amount;
        NodeDepleted = depleted;
    }
}

/// <summary>
/// ResourceNode Class: Represents a harvestable resource in the game world.
/// This is the 'Node' part of the ResourceNodeSystem pattern.
/// Players or harvesters interact with these nodes to gather resources.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure there's a collider for interaction
public class ResourceNode : MonoBehaviour
{
    [Header("Resource Node Configuration")]
    [Tooltip("The type of resource this node provides.")]
    [SerializeField] private ResourceType _resourceType = ResourceType.Wood;
    public ResourceType Type => _resourceType;

    [Tooltip("The maximum quantity of resources this node can hold.")]
    [SerializeField] private int _maxQuantity = 100;
    public int MaxQuantity => _maxQuantity;

    [Tooltip("The current quantity of resources remaining in this node.")]
    [SerializeField] private int _currentQuantity;
    public int CurrentQuantity => _currentQuantity;

    [Tooltip("Amount of resource harvested per interaction.")]
    [SerializeField] private int _harvestAmountPerInteraction = 10;
    public int HarvestAmountPerInteraction => _harvestAmountPerInteraction;

    [Tooltip("Time in seconds for the resource node to respawn after depletion.")]
    [SerializeField] private float _respawnTime = 30f;
    public float RespawnTime => _respawnTime;

    [Tooltip("Threshold below which the node is considered 'depleted' visually/functionally.")]
    [SerializeField] private int _depletionThreshold = 0;

    [Header("Visuals")]
    [Tooltip("GameObject to show when the node is active and harvestable.")]
    [SerializeField] private GameObject _activeVisual;
    [Tooltip("GameObject to show when the node is depleted or respawning.")]
    [SerializeField] private GameObject _depletedVisual;

    private bool _isRespawning = false;

    // --- Events for loose coupling and communication ---
    // OnResourceHarvested: Global event triggered when ANY resource is harvested from ANY node.
    // Useful for UI updates, achievements, or global resource tracking.
    public static event Action<ResourceType, int> OnResourceHarvested;

    // OnNodeDepleted: Local event triggered when THIS specific node becomes depleted.
    // Useful for visual effects, sound, or specific logic related to this node.
    public event Action<ResourceNode> OnNodeDepleted;

    // OnNodeRespawned: Local event triggered when THIS specific node respawns.
    public event Action<ResourceNode> OnNodeRespawned;

    private void Awake()
    {
        // Initialize current quantity to max quantity at start
        _currentQuantity = _maxQuantity;
        UpdateVisuals();

        // Ensure the collider is set to 'Is Trigger' for interaction detection.
        // Also, it's good practice to assign a specific layer to ResourceNodes
        // so harvesters can efficiently filter them.
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Collider on ResourceNode '{gameObject.name}' is not marked as 'Is Trigger'. " +
                             "It's recommended for interaction detection.");
            // Optionally, force it: col.isTrigger = true;
        }
    }

    /// <summary>
    /// Attempts to harvest resources from this node.
    /// This is the primary interaction method for harvesters.
    /// </summary>
    /// <param name="requestedAmount">The amount of resource the harvester tries to take.</param>
    /// <returns>A HarvestResult struct detailing the actual amount harvested and node status.</returns>
    public HarvestResult Harvest(int requestedAmount)
    {
        if (_isRespawning || _currentQuantity <= _depletionThreshold)
        {
            // Node is depleted or respawning, cannot harvest
            return new HarvestResult(_resourceType, 0, _currentQuantity <= _depletionThreshold);
        }

        // Calculate actual amount to harvest, considering remaining quantity
        int actualAmountHarvested = Mathf.Min(requestedAmount, _currentQuantity);
        _currentQuantity -= actualAmountHarvested;

        bool nodeBecameDepleted = _currentQuantity <= _depletionThreshold;

        // --- Event Invocation ---
        // Notify global listeners that resources were harvested
        OnResourceHarvested?.Invoke(_resourceType, actualAmountHarvested);

        if (nodeBecameDepleted)
        {
            // Notify local listeners that this specific node is depleted
            OnNodeDepleted?.Invoke(this);
            StartRespawnCoroutine(); // Start the respawn process
        }

        UpdateVisuals(); // Update visual state based on new quantity

        return new HarvestResult(_resourceType, actualAmountHarvested, nodeBecameDepleted);
    }

    /// <summary>
    /// Coroutine to handle the respawn logic for the resource node.
    /// </summary>
    private IEnumerator RespawnCoroutine()
    {
        _isRespawning = true;
        Debug.Log($"Resource Node '{_resourceType}' at '{gameObject.name}' depleted. Respawning in {_respawnTime} seconds...");
        yield return new WaitForSeconds(_respawnTime);

        // Reset quantity and state
        _currentQuantity = _maxQuantity;
        _isRespawning = false;
        UpdateVisuals();

        // Notify local listeners that this node has respawned
        OnNodeRespawned?.Invoke(this);
        Debug.Log($"Resource Node '{_resourceType}' at '{gameObject.name}' respawned!");
    }

    /// <summary>
    /// Updates the visual representation of the resource node based on its current state.
    /// </summary>
    private void UpdateVisuals()
    {
        bool showDepleted = _currentQuantity <= _depletionThreshold || _isRespawning;

        if (_activeVisual != null) _activeVisual.SetActive(!showDepleted);
        if (_depletedVisual != null) _depletedVisual.SetActive(showDepleted);

        // Optionally, change material or tint color
        // Example: If using a single mesh and just changing material
        // Renderer nodeRenderer = GetComponent<Renderer>();
        // if (nodeRenderer != null)
        // {
        //     nodeRenderer.material = showDepleted ? _depletedMaterial : _activeMaterial;
        // }
    }

    // Example of a visual change for depletion (optional, depends on your setup)
    private void OnEnable()
    {
        // Subscribe to local depletion event to handle visual changes if not using direct UpdateVisuals
        // OnNodeDepleted += HandleNodeDepletedVisuals;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        // OnNodeDepleted -= HandleNodeDepletedVisuals;
    }

    // private void HandleNodeDepletedVisuals(ResourceNode node)
    // {
    //     // This could be another way to trigger visual updates,
    //     // separate from the UpdateVisuals() called directly after harvest.
    //     // UpdateVisuals(); // Or specific logic for depletion
    // }

    // OnValidate is called in the editor when the script is loaded or a value is changed in the Inspector.
    private void OnValidate()
    {
        if (_currentQuantity > _maxQuantity)
        {
            _currentQuantity = _maxQuantity;
        }
        if (_depletionThreshold < 0)
        {
            _depletionThreshold = 0;
        }
        if (_harvestAmountPerInteraction <= 0)
        {
            _harvestAmountPerInteraction = 1; // Must harvest at least 1
        }
        if (_respawnTime < 0)
        {
            _respawnTime = 0;
        }

        // Initialize current quantity if not set (first time adding script or after reset)
        if (_currentQuantity == 0 && !Application.isPlaying)
        {
            _currentQuantity = _maxQuantity;
        }

        // Ensure visuals are set up if not null
        if (_activeVisual == null && transform.childCount > 0)
        {
            // Try to auto-assign the first child as active visual if no specific visuals are set
            // For a simple setup where the node itself is the visual, you might not use _activeVisual.
            // If you have a child that represents the node, consider assigning it here.
            // For this example, we'll assume direct assignment in Inspector is preferred.
        }
        if (_depletedVisual != null && _activeVisual != null && _depletedVisual == _activeVisual)
        {
            Debug.LogWarning($"ResourceNode '{gameObject.name}': Active and Depleted visuals are the same. " +
                             "Consider using different GameObjects for distinct visual states.");
        }
    }
}


/// <summary>
/// PlayerHarvester Class: Manages player interaction with resource nodes and their inventory.
/// This is the 'Harvester' part of the ResourceNodeSystem pattern.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Requires Rigidbody for physics interaction (movement, collision)
[RequireComponent(typeof(Collider))] // Requires Collider to detect nodes (e.g., trigger collision)
public class PlayerHarvester : MonoBehaviour
{
    [Header("Player Harvester Configuration")]
    [Tooltip("Movement speed of the player.")]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("How close the player needs to be to a resource node to interact.")]
    [SerializeField] private float _interactionRange = 2f;

    [Tooltip("Interval in seconds between harvest attempts when holding the harvest button.")]
    [SerializeField] private float _harvestInterval = 0.5f;

    [Tooltip("Layer on which Resource Nodes are placed for efficient detection.")]
    [SerializeField] private LayerMask _resourceNodeLayer;

    private Dictionary<ResourceType, int> _inventory = new Dictionary<ResourceType, int>();
    private float _lastHarvestTime;
    private ResourceNode _currentClosestNode; // Track the closest node for interaction

    // --- Events ---
    // OnPlayerInventoryChanged: Global event for when the player's inventory changes.
    // Useful for UI updates.
    public static event Action<ResourceType, int> OnPlayerInventoryChanged;

    private void Start()
    {
        // Initialize inventory with 0 for all resource types
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _inventory[type] = 0;
            OnPlayerInventoryChanged?.Invoke(type, _inventory[type]); // Notify UI of initial state
        }
        Debug.Log("Player Harvester initialized. Press 'E' to harvest when near a resource node.");
    }

    private void Update()
    {
        HandleMovement();
        DetectClosestResourceNode();

        // Check for harvest input
        if (Input.GetKey(KeyCode.E) && Time.time - _lastHarvestTime >= _harvestInterval)
        {
            TryHarvest();
            _lastHarvestTime = Time.time;
        }
    }

    /// <summary>
    /// Handles player movement using WASD keys.
    /// </summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        transform.position += moveDirection * _moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Detects the closest harvestable resource node within interaction range.
    /// Uses an OverlapSphere for efficient range detection.
    /// </summary>
    private void DetectClosestResourceNode()
    {
        _currentClosestNode = null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _interactionRange, _resourceNodeLayer);
        float minDistance = _interactionRange + 1f;

        foreach (var hitCollider in hitColliders)
        {
            ResourceNode node = hitCollider.GetComponent<ResourceNode>();
            if (node != null)
            {
                float distance = Vector3.Distance(transform.position, node.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    _currentClosestNode = node;
                }
            }
        }

        // Optional: Provide visual feedback for the currently targeted node
        // Debug.Log(_currentClosestNode != null ? $"Closest node: {_currentClosestNode.name}" : "No node in range.");
    }

    /// <summary>
    /// Attempts to harvest from the currently detected closest resource node.
    /// </summary>
    private void TryHarvest()
    {
        if (_currentClosestNode == null)
        {
            Debug.Log("No resource node in range to harvest from.");
            return;
        }

        if (_currentClosestNode.CurrentQuantity <= _currentClosestNode.DepletionThreshold)
        {
            Debug.Log($"Node '{_currentClosestNode.name}' is depleted or respawning.");
            return;
        }

        // Request to harvest the node's defined harvest amount per interaction
        HarvestResult result = _currentClosestNode.Harvest(_currentClosestNode.HarvestAmountPerInteraction);

        if (result.AmountHarvested > 0)
        {
            AddResourcesToInventory(result.ResourceType, result.AmountHarvested);
            Debug.Log($"Harvested {result.AmountHarvested} {result.ResourceType} from {_currentClosestNode.name}. " +
                      $"Node remaining: {_currentClosestNode.CurrentQuantity}/{_currentClosestNode.MaxQuantity}.");
        }
        else
        {
            Debug.Log($"Failed to harvest from {_currentClosestNode.name}. Node depleted? {result.NodeDepleted}");
        }
    }

    /// <summary>
    /// Adds harvested resources to the player's inventory and notifies UI.
    /// </summary>
    /// <param name="type">The type of resource.</param>
    /// <param name="amount">The amount to add.</param>
    private void AddResourcesToInventory(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        if (_inventory.ContainsKey(type))
        {
            _inventory[type] += amount;
        }
        else
        {
            _inventory[type] = amount;
        }

        // Notify UI or other systems about inventory change
        OnPlayerInventoryChanged?.Invoke(type, _inventory[type]);
    }

    /// <summary>
    /// Retrieves the current amount of a specific resource in the player's inventory.
    /// </summary>
    /// <param name="type">The type of resource to check.</param>
    /// <returns>The amount of the resource, or 0 if not found.</returns>
    public int GetResourceAmount(ResourceType type)
    {
        _inventory.TryGetValue(type, out int amount);
        return amount;
    }

    // Draw interaction range in editor for easier debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactionRange);
    }
}

/// <summary>
/// ResourceDisplayUI Class: A simple UI component to display the player's collected resources.
/// Listens to the PlayerHarvester's inventory change events.
/// </summary>
public class ResourceDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text element to display Wood count.")]
    [SerializeField] private Text _woodCountText;
    [Tooltip("Text element to display Stone count.")]
    [SerializeField] private Text _stoneCountText;
    [Tooltip("Text element to display Gold count.")]
    [SerializeField] private Text _goldCountText;
    // Add more Text fields for other resource types

    private void OnEnable()
    {
        // Subscribe to the global player inventory changed event
        PlayerHarvester.OnPlayerInventoryChanged += UpdateResourceText;
        // Optionally subscribe to global resource harvested event if you want a separate display
        // ResourceNode.OnResourceHarvested += HandleGlobalHarvested;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks when the object is destroyed or disabled
        PlayerHarvester.OnPlayerInventoryChanged -= UpdateResourceText;
        // ResourceNode.OnResourceHarvested -= HandleGlobalHarvested;
    }

    /// <summary>
    /// Updates the corresponding UI Text element when a resource amount changes.
    /// This method is called by the PlayerHarvester's OnPlayerInventoryChanged event.
    /// </summary>
    /// <param name="type">The type of resource that changed.</param>
    /// <param name="amount">The new amount of that resource.</param>
    private void UpdateResourceText(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Wood:
                if (_woodCountText != null) _woodCountText.text = $"Wood: {amount}";
                break;
            case ResourceType.Stone:
                if (_stoneCountText != null) _stoneCountText.text = $"Stone: {amount}";
                break;
            case ResourceType.Gold:
                if (_goldCountText != null) _goldCountText.text = $"Gold: {amount}";
                break;
                // Add more cases for other resource types
        }
    }

    // Example for handling global harvest notification (optional)
    // private void HandleGlobalHarvested(ResourceType type, int amount)
    // {
    //     Debug.Log($"Global Event: {amount} {type} harvested somewhere!");
    // }
}
```