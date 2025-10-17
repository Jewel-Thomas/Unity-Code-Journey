// Unity Design Pattern Example: RealTimeEconomySystem
// This script demonstrates the RealTimeEconomySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RealTimeEconomySystem' design pattern, while not a classical GoF pattern, is a common architectural need in games, especially those with simulation or management elements (like city builders, RTS games, or RPGs with crafting/trading). It focuses on managing various in-game resources, their real-time generation (production) and consumption, and discrete transactions.

This example implements a robust `RealTimeEconomySystem` using a central `EconomyManager` that acts as a singleton. It uses the **Observer Pattern** for notifications (e.g., UI updates when resources change) and an **Inversion of Control** approach where the manager drives the updates of registered participants, rather than participants managing their own timers.

---

### Key Components of the RealTimeEconomySystem:

1.  **`ResourceType` Enum:** Defines all the distinct resources in your game (e.g., Gold, Wood, Food).
2.  **`EconomyManager` (Singleton MonoBehaviour):**
    *   The central authority for the entire economy.
    *   Holds the current quantities of all resources.
    *   Provides methods to add, remove, and query resources.
    *   Manages a list of `IEconomyParticipant` objects (producers, consumers).
    *   Fires events (`OnResourceChanged`, `OnInsufficientFunds`, `OnTransactionAttempted`) for other systems (like UI) to subscribe to.
    *   Its `Update()` method drives the real-time logic for all participants.
3.  **`IEconomyParticipant` Interface:**
    *   Defines a contract for any game object that interacts with the `EconomyManager` in a time-based manner.
    *   `Initialize()`: To get a reference to the `EconomyManager`.
    *   `UpdateEconomy()`: Called by the `EconomyManager` to update the participant's economic logic.
4.  **`ResourceProducer` (MonoBehaviour, implements `IEconomyParticipant`):**
    *   An example participant that generates a specific resource at a defined rate over time.
    *   Registers itself with the `EconomyManager` when enabled.
5.  **`ResourceConsumer` (MonoBehaviour, implements `IEconomyParticipant`):**
    *   An example participant that consumes a specific resource at a defined rate over time.
    *   Registers itself with the `EconomyManager` when enabled.
    *   Handles cases where insufficient resources are available.
6.  **`TransactionDefinition` (Serializable Class):**
    *   Represents a set of resource costs and/or gains for a single action (e.g., building a house, buying an item).
    *   Includes logic to check affordability and apply the transaction.
7.  **`EconomyDisplay` (Example UI Component):**
    *   Subscribes to `EconomyManager` events to demonstrate how UI can react to resource changes.
8.  **`TransactionTrigger` (Example Action Component):**
    *   Demonstrates how a user action (like a button click) or a timed event can trigger a `TransactionDefinition` through the `EconomyManager`.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // For ReadOnlyDictionary
using System.Linq; // For LINQ operations

// --- 1. ResourceType Enum ---
/// <summary>
/// Defines all available resource types in the game economy.
/// Extend this enum to add more resources (e.g., Iron, Mana, Population).
/// </summary>
public enum ResourceType
{
    Gold,
    Wood,
    Stone,
    Food,
    Energy,
    // Add more resource types here as needed for your game
}

// --- 2. TransactionDefinition (Represents a cost or gain) ---
/// <summary>
/// A class representing a specific economic transaction,
/// detailing resource costs and potential gains.
/// This is highly versatile and can be used for:
/// - Building structures (costs Wood, Stone, Gold)
/// - Crafting items (costs various materials)
/// - Buying items (costs Gold)
/// - Selling items (gains Gold)
/// - Training units (costs Food, Gold)
/// </summary>
[System.Serializable] // Make it visible in Inspector if used as a field, though Dictionary editing requires custom tools.
public class TransactionDefinition
{
    // These dictionaries store the resource types and their quantities for costs and gains.
    // Using [SerializeField] allows Unity to save/load these in simple cases, but for complex
    // Inspector editing of Dictionaries, a custom editor or ScriptableObject approach is better.
    [SerializeField] private Dictionary<ResourceType, int> _costs;
    [SerializeField] private Dictionary<ResourceType, int> _gains;

    /// <summary>
    /// Gets a read-only dictionary of resources required for this transaction.
    /// This prevents external code from accidentally modifying the transaction's costs.
    /// </summary>
    public ReadOnlyDictionary<ResourceType, int> Costs => new ReadOnlyDictionary<ResourceType, int>(_costs ?? new Dictionary<ResourceType, int>());

    /// <summary>
    /// Gets a read-only dictionary of resources gained from this transaction.
    /// This prevents external code from accidentally modifying the transaction's gains.
    /// </summary>
    public ReadOnlyDictionary<ResourceType, int> Gains => new ReadOnlyDictionary<ResourceType, int>(_gains ?? new Dictionary<ResourceType, int>());

    /// <summary>
    /// Constructor for creating a TransactionDefinition.
    /// </summary>
    /// <param name="costs">A dictionary of resources to be consumed by the transaction.</param>
    /// <param name="gains">A dictionary of resources to be produced by the transaction.</param>
    public TransactionDefinition(Dictionary<ResourceType, int> costs = null, Dictionary<ResourceType, int> gains = null)
    {
        _costs = costs ?? new Dictionary<ResourceType, int>();
        _gains = gains ?? new Dictionary<ResourceType, int>();
    }

    /// <summary>
    /// Checks if the provided EconomyManager has sufficient resources to cover all costs of this transaction.
    /// </summary>
    /// <param name="economyManager">The central EconomyManager instance to check against.</param>
    /// <returns>True if all costs can be afforded, false otherwise.</returns>
    public bool CanAfford(EconomyManager economyManager)
    {
        if (economyManager == null)
        {
            Debug.LogError("CanAfford called with a null EconomyManager.");
            return false;
        }

        foreach (var costEntry in _costs)
        {
            if (economyManager.GetResourceQuantity(costEntry.Key) < costEntry.Value)
            {
                return false; // Not enough of this specific resource
            }
        }
        return true; // All costs can be covered
    }

    /// <summary>
    /// Attempts to apply the transaction's costs and gains to the EconomyManager.
    /// It first verifies if the transaction can be afforded.
    /// </summary>
    /// <param name="economyManager">The central EconomyManager instance to apply changes to.</param>
    /// <returns>True if the transaction was successfully applied, false if not affordable.</returns>
    public bool TryApply(EconomyManager economyManager)
    {
        if (economyManager == null)
        {
            Debug.LogError("TryApply called with a null EconomyManager.");
            return false;
        }

        // First, check if all costs can be afforded. This ensures atomicity (all or nothing).
        if (!CanAfford(economyManager))
        {
            // The EconomyManager's RemoveResource method will fire OnInsufficientFunds events if needed.
            return false;
        }

        // Deduct costs (if CanAfford passed, these removals should succeed)
        foreach (var costEntry in _costs)
        {
            economyManager.RemoveResource(costEntry.Key, costEntry.Value);
        }

        // Add gains
        foreach (var gainEntry in _gains)
        {
            economyManager.AddResource(gainEntry.Key, gainEntry.Value);
        }

        return true; // Transaction successfully applied
    }

    /// <summary>
    /// Creates a user-friendly string description of the transaction.
    /// Useful for displaying costs/gains in UI tooltips or logs.
    /// </summary>
    public string GetDescription()
    {
        var descriptionParts = new List<string>();

        if (_costs != null && _costs.Count > 0)
        {
            descriptionParts.Add("Costs: " + string.Join(", ", _costs.Select(kv => $"{kv.Value} {kv.Key}")));
        }
        if (_gains != null && _gains.Count > 0)
        {
            descriptionParts.Add("Gains: " + string.Join(", ", _gains.Select(kv => $"{kv.Value} {kv.Key}")));
        }

        if (descriptionParts.Count == 0) return "No economic effect.";
        return string.Join(" | ", descriptionParts);
    }
}

// --- 3. IEconomyParticipant Interface ---
/// <summary>
/// Interface for any game entity that interacts with the economy manager
/// in a time-based or persistent manner. This includes producers (mines, farms)
/// and consumers (upkeep for buildings, population food consumption).
/// By implementing this interface, an object can be managed by the EconomyManager.
/// </summary>
public interface IEconomyParticipant
{
    /// <summary>
    /// Initializes the participant with a reference to the central EconomyManager.
    /// This method is called by the EconomyManager when the participant is registered.
    /// This is a form of dependency injection, ensuring the participant has the necessary reference.
    /// </summary>
    /// <param name="manager">The central EconomyManager instance.</param>
    void Initialize(EconomyManager manager);

    /// <summary>
    /// Updates the participant's economic logic based on elapsed time.
    /// This method is called by the EconomyManager's main Update loop, centralizing
    /// all real-time economic calculations.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, typically Time.deltaTime.</param>
    void UpdateEconomy(float deltaTime);
}


// --- 4. EconomyManager (Singleton MonoBehaviour) ---
/// <summary>
/// The central hub for managing all economic activities in the game.
/// This class follows the Singleton pattern, ensuring there's only one
/// instance controlling the game's economy at any given time.
/// It tracks resource quantities, handles transactions, and orchestrates
/// real-time production and consumption from registered IEconomyParticipants.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    // Singleton instance property for global access.
    public static EconomyManager Instance { get; private set; }

    [Header("Initial Resources")]
    [Tooltip("Define starting quantities for resources. Only applies on first load.")]
    [SerializeField]
    private List<InitialResource> _initialResources = new List<InitialResource>();

    // Helper class for Inspector serialization of initial resources.
    [System.2Serializable]
    private class InitialResource
    {
        public ResourceType Type;
        public int Quantity;
    }

    // Internal dictionary to store current quantities of all resources.
    private Dictionary<ResourceType, int> _currentResources;

    // List of all active IEconomyParticipant entities (producers, consumers)
    // that the EconomyManager will manage and update in real-time.
    private List<IEconomyParticipant> _participants = new List<IEconomyParticipant>();

    // --- Events for UI and other game systems to subscribe to ---

    /// <summary>
    /// Event fired whenever a resource's quantity changes.
    /// Useful for updating UI displays, triggering sound effects, or visual feedback.
    /// Parameters: ResourceType, newQuantity, oldQuantity.
    /// </summary>
    public event Action<ResourceType, int, int> OnResourceChanged;

    /// <summary>
    /// Event fired when an attempt to remove resources fails due to insufficient funds.
    /// Can be used to display warnings to the player, play an "error" sound, etc.
    /// Parameters: ResourceType, attemptedRemovalQuantity.
    /// </summary>
    public event Action<ResourceType, int> OnInsufficientFunds;

    /// <summary>
    /// Event fired when any transaction (via TryApplyTransaction) is attempted.
    /// Useful for logging, analytics, or debugging transaction flows.
    /// Parameters: TransactionDefinition, success.
    /// </summary>
    public event Action<TransactionDefinition, bool> OnTransactionAttempted;


    private void Awake()
    {
        // Enforce the Singleton pattern:
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple EconomyManager instances found! Destroying duplicate.");
            Destroy(gameObject); // Destroy this duplicate GameObject.
            return;
        }
        Instance = this; // Set this instance as the Singleton.
        DontDestroyOnLoad(gameObject); // Persist across scene loads.

        InitializeEconomy();
    }

    /// <summary>
    /// Initializes the economy, setting up initial resource quantities.
    /// Called once when the manager is first created.
    /// </summary>
    private void InitializeEconomy()
    {
        _currentResources = new Dictionary<ResourceType, int>();
        // Ensure all possible resource types are present in the dictionary, even if their quantity is 0.
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            _currentResources[type] = 0;
        }

        // Apply initial resources configured in the Unity Inspector.
        foreach (var initialRes in _initialResources)
        {
            _currentResources[initialRes.Type] = initialRes.Quantity;
            Debug.Log($"Economy: Initialized {initialRes.Type} with {initialRes.Quantity}.");
            // Notify subscribers about the initial resource states.
            OnResourceChanged?.Invoke(initialRes.Type, initialRes.Quantity, 0);
        }
    }

    /// <summary>
    /// The main update loop for real-time economic processing.
    /// This method iterates through all registered IEconomyParticipants
    /// and delegates their time-based economic logic.
    /// </summary>
    private void Update()
    {
        // Iterate over a copy of the participants list to prevent "Collection was modified" errors
        // if participants register/unregister themselves during the loop (e.g., a building is destroyed).
        foreach (var participant in _participants.ToList())
        {
            participant.UpdateEconomy(Time.deltaTime);
        }
    }

    /// <summary>
    /// Registers an IEconomyParticipant with the manager.
    /// Once registered, the participant's UpdateEconomy method will be called every frame.
    /// </summary>
    /// <param name="participant">The entity (e.g., a ResourceProducer or ResourceConsumer) to register.</param>
    public void RegisterParticipant(IEconomyParticipant participant)
    {
        if (participant == null)
        {
            Debug.LogError("Attempted to register a null economy participant.");
            return;
        }
        if (!_participants.Contains(participant))
        {
            _participants.Add(participant);
            participant.Initialize(this); // Inject the EconomyManager reference into the participant.
            Debug.Log($"EconomyManager: Registered participant '{participant.GetType().Name}'.");
        }
        else
        {
            Debug.LogWarning($"EconomyManager: Participant '{participant.GetType().Name}' is already registered.");
        }
    }

    /// <summary>
    /// Unregisters an IEconomyParticipant from the manager.
    /// This stops the participant's UpdateEconomy method from being called.
    /// Important for cleanup when objects are destroyed or disabled.
    /// </summary>
    /// <param name="participant">The entity to unregister.</param>
    public void UnregisterParticipant(IEconomyParticipant participant)
    {
        if (participant == null)
        {
            Debug.LogError("Attempted to unregister a null economy participant.");
            return;
        }
        if (_participants.Contains(participant))
        {
            _participants.Remove(participant);
            Debug.Log($"EconomyManager: Unregistered participant '{participant.GetType().Name}'.");
        }
        else
        {
            Debug.LogWarning($"EconomyManager: Participant '{participant.GetType().Name}' was not registered, cannot unregister.");
        }
    }

    /// <summary>
    /// Adds a specified quantity of a resource to the economy.
    /// Fires the OnResourceChanged event upon successful addition.
    /// </summary>
    /// <param name="type">The type of resource to add.</param>
    /// <param name="quantity">The amount to add. Must be a positive value.</param>
    public void AddResource(ResourceType type, int quantity)
    {
        if (quantity < 0)
        {
            Debug.LogWarning($"EconomyManager: Attempted to add a negative quantity ({quantity}) of {type}. Use RemoveResource instead.");
            return;
        }

        int oldQuantity = _currentResources[type];
        _currentResources[type] += quantity;
        OnResourceChanged?.Invoke(type, _currentResources[type], oldQuantity); // Notify subscribers
        // Debug.Log($"Economy: Added {quantity} {type}. New total: {_currentResources[type]}");
    }

    /// <summary>
    /// Attempts to remove a specified quantity of a resource from the economy.
    /// Fires OnResourceChanged if successful, or OnInsufficientFunds if not enough resources.
    /// </summary>
    /// <param name="type">The type of resource to remove.</param>
    /// <param name="quantity">The amount to remove. Must be a positive value.</param>
    /// <returns>True if resources were successfully removed, false if insufficient.</returns>
    public bool RemoveResource(ResourceType type, int quantity)
    {
        if (quantity < 0)
        {
            Debug.LogWarning($"EconomyManager: Attempted to remove a negative quantity ({quantity}) of {type}. Use AddResource instead.");
            return false;
        }

        if (_currentResources[type] >= quantity)
        {
            int oldQuantity = _currentResources[type];
            _currentResources[type] -= quantity;
            OnResourceChanged?.Invoke(type, _currentResources[type], oldQuantity); // Notify subscribers
            // Debug.Log($"Economy: Removed {quantity} {type}. New total: {_currentResources[type]}");
            return true;
        }
        else
        {
            OnInsufficientFunds?.Invoke(type, quantity); // Notify subscribers of failure
            // Debug.LogWarning($"EconomyManager: Insufficient {type} to remove {quantity}. Current: {_currentResources[type]}");
            return false;
        }
    }

    /// <summary>
    /// Gets the current quantity of a specific resource.
    /// </summary>
    /// <param name="type">The type of resource to query.</param>
    /// <returns>The current quantity of the resource. Returns 0 if the type is not tracked (shouldn't happen with proper initialization).</returns>
    public int GetResourceQuantity(ResourceType type)
    {
        return _currentResources.ContainsKey(type) ? _currentResources[type] : 0;
    }

    /// <summary>
    /// Checks if the current resources held by the EconomyManager can cover the costs
    /// specified in a given TransactionDefinition.
    /// </summary>
    /// <param name="transaction">The TransactionDefinition to check.</param>
    /// <returns>True if all costs can be afforded, false otherwise.</returns>
    public bool CanAfford(TransactionDefinition transaction)
    {
        if (transaction == null)
        {
            Debug.LogError("CanAfford called with a null TransactionDefinition.");
            return false;
        }
        return transaction.CanAfford(this);
    }

    /// <summary>
    /// Attempts to apply a TransactionDefinition (deducts costs, adds gains).
    /// This method uses the TransactionDefinition's internal logic for atomic application.
    /// Fires the OnTransactionAttempted event.
    /// </summary>
    /// <param name="transaction">The TransactionDefinition to apply.</param>
    /// <returns>True if the transaction was successful (affordable and applied), false if not.</returns>
    public bool TryApplyTransaction(TransactionDefinition transaction)
    {
        if (transaction == null)
        {
            Debug.LogError("Attempted to apply a null transaction.");
            OnTransactionAttempted?.Invoke(null, false); // Notify of failed attempt with null transaction
            return false;
        }

        bool success = transaction.TryApply(this);
        OnTransactionAttempted?.Invoke(transaction, success); // Notify all subscribers of the transaction attempt
        return success;
    }

    // --- Persistence (Example - real implementation would interface with a dedicated save/load system) ---
    /// <summary>
    /// Example method for saving the current resource state.
    /// In a real game, this would be part of a larger save/load system (e.g., serializing to JSON, binary, etc.).
    /// </summary>
    /// <returns>A dictionary representing the current resource quantities.</returns>
    public Dictionary<ResourceType, int> SaveEconomyState()
    {
        Debug.Log("EconomyManager: Saving current economy state.");
        return new Dictionary<ResourceType, int>(_currentResources); // Return a copy of the current state.
    }

    /// <summary>
    /// Example method for loading resource state into the manager.
    /// In a real game, this would be part of a larger save/load system.
    /// </summary>
    /// <param name="savedState">A dictionary containing the resource quantities to load.</param>
    public void LoadEconomyState(Dictionary<ResourceType, int> savedState)
    {
        if (savedState == null)
        {
            Debug.LogWarning("EconomyManager: Attempted to load a null economy state.");
            return;
        }

        Debug.Log("EconomyManager: Loading economy state.");

        // Clear existing resources and reset to 0 for all types.
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            _currentResources[type] = 0;
        }

        // Apply the loaded state, notifying of changes.
        foreach (var entry in savedState)
        {
            int oldQuantity = _currentResources.ContainsKey(entry.Key) ? _currentResources[entry.Key] : 0;
            _currentResources[entry.Key] = entry.Value;
            OnResourceChanged?.Invoke(entry.Key, entry.Value, oldQuantity); // Notify UI of all changes
        }
    }
}


// --- 5. ResourceProducer (Example IEconomyParticipant) ---
/// <summary>
/// A MonoBehaviour component that acts as a real-time producer of a specific resource.
/// Examples: A mine produces Stone, a farm produces Food, a lumber mill produces Wood.
/// It implements IEconomyParticipant and registers itself with the EconomyManager.
/// </summary>
public class ResourceProducer : MonoBehaviour, IEconomyParticipant
{
    [Header("Producer Settings")]
    [Tooltip("The type of resource this producer generates.")]
    [SerializeField] private ResourceType _producesType;
    [Tooltip("The rate at which this resource is produced per second (e.g., 1.5 means 1.5 units per second).")]
    [SerializeField] private float _productionRatePerSecond = 1f;

    private EconomyManager _economyManager;
    private float _timeSinceLastProduction; // Accumulates time to handle fractional production rates.

    public ResourceType ProducesType => _producesType;
    public float ProductionRatePerSecond => _productionRatePerSecond;

    private void OnEnable()
    {
        // Register with the EconomyManager when this component becomes active.
        // This ensures the manager starts calling UpdateEconomy on this producer.
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.RegisterParticipant(this);
        }
        else
        {
            Debug.LogError($"{name}: EconomyManager.Instance is null. Ensure EconomyManager is in the scene and initialized before any producers are enabled.");
        }
    }

    private void OnDisable()
    {
        // Unregister from the EconomyManager when this component is disabled or destroyed.
        // This prevents memory leaks and ensures the manager doesn't try to update a non-existent object.
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.UnregisterParticipant(this);
        }
    }

    /// <summary>
    /// Called by the EconomyManager to provide this producer with its reference.
    /// </summary>
    /// <param name="manager">The central EconomyManager instance.</param>
    public void Initialize(EconomyManager manager)
    {
        _economyManager = manager;
        _timeSinceLastProduction = 0f; // Reset timer upon initialization.
        Debug.Log($"{name}: Initialized as a producer of {_producesType} at {_productionRatePerSecond} units/sec.");
    }

    /// <summary>
    /// Called by the EconomyManager every frame to update production logic.
    /// It calculates how many units should have been produced based on the elapsed time and rate.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update (from EconomyManager's Update).</param>
    public void UpdateEconomy(float deltaTime)
    {
        if (_economyManager == null) return; // Should not happen if Initialize was called correctly.

        _timeSinceLastProduction += deltaTime;

        // Calculate how many *whole* units should have been produced since the last check.
        int unitsToProduce = Mathf.FloorToInt(_timeSinceLastProduction * _productionRatePerSecond);

        if (unitsToProduce > 0)
        {
            _economyManager.AddResource(_producesType, unitsToProduce);
            // Deduct the time equivalent for the units that were actually produced.
            // This preserves fractional production progress.
            _timeSinceLastProduction -= unitsToProduce / _productionRatePerSecond;
            // Ensure accumulated time doesn't go negative due to floating point inaccuracies.
            _timeSinceLastProduction = Mathf.Max(0f, _timeSinceLastProduction);
        }
    }
}

// --- 6. ResourceConsumer (Example IEconomyParticipant) ---
/// <summary>
/// A MonoBehaviour component that acts as a real-time consumer of a specific resource.
/// Examples: A city consumes Food, a factory consumes Energy, a barracks consumes Gold for upkeep.
/// It implements IEconomyParticipant and registers itself with the EconomyManager.
/// </summary>
public class ResourceConsumer : MonoBehaviour, IEconomyParticipant
{
    [Header("Consumer Settings")]
    [Tooltip("The type of resource this consumer needs.")]
    [SerializeField] private ResourceType _consumesType;
    [Tooltip("The rate at which this resource is consumed per second (e.g., 0.5 means 0.5 units per second).")]
    [SerializeField] private float _consumptionRatePerSecond = 1f;

    private EconomyManager _economyManager;
    private float _timeSinceLastConsumption; // Accumulates time to handle fractional consumption rates.

    public ResourceType ConsumesType => _consumesType;
    public float ConsumptionRatePerSecond => _consumptionRatePerSecond;

    private void OnEnable()
    {
        // Register with the EconomyManager when this component becomes active.
        // This ensures the manager starts calling UpdateEconomy on this consumer.
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.RegisterParticipant(this);
        }
        else
        {
            Debug.LogError($"{name}: EconomyManager.Instance is null. Ensure EconomyManager is in the scene and initialized before any consumers are enabled.");
        }
    }

    private void OnDisable()
    {
        // Unregister from the EconomyManager when this component is disabled or destroyed.
        // This prevents memory leaks and ensures the manager doesn't try to update a non-existent object.
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.UnregisterParticipant(this);
        }
    }

    /// <summary>
    /// Called by the EconomyManager to provide this consumer with its reference.
    /// </summary>
    /// <param name="manager">The central EconomyManager instance.</param>
    public void Initialize(EconomyManager manager)
    {
        _economyManager = manager;
        _timeSinceLastConsumption = 0f; // Reset timer upon initialization.
        Debug.Log($"{name}: Initialized as a consumer of {_consumesType} at {_consumptionRatePerSecond} units/sec.");
    }

    /// <summary>
    /// Called by the EconomyManager every frame to update consumption logic.
    /// It calculates how many units should have been consumed based on the elapsed time and rate.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update (from EconomyManager's Update).</param>
    public void UpdateEconomy(float deltaTime)
    {
        if (_economyManager == null) return; // Should not happen if Initialize was called correctly.

        _timeSinceLastConsumption += deltaTime;

        // Calculate how many *whole* units should be consumed.
        int unitsToConsume = Mathf.FloorToInt(_timeSinceLastConsumption * _consumptionRatePerSecond);

        if (unitsToConsume > 0)
        {
            // Attempt to remove the calculated quantity.
            if (_economyManager.RemoveResource(_consumesType, unitsToConsume))
            {
                // If consumption was successful, deduct the time equivalent.
                _timeSinceLastConsumption -= unitsToConsume / _consumptionRatePerSecond;
                _timeSinceLastConsumption = Mathf.Max(0f, _timeSinceLastConsumption);
            }
            else
            {
                // If consumption failed (e.g., insufficient resources), we have a choice:
                // 1. Let _timeSinceLastConsumption accumulate: This means when resources become available,
                //    the consumer will try to consume multiple units at once to catch up.
                // 2. Reset _timeSinceLastConsumption = 0f: This pauses consumption until resources are
                //    available again, effectively preventing "backlogged" consumption.
                // For this example, we let it accumulate, as this models a continuous need.
                Debug.LogWarning($"{name}: Ran out of {_consumesType} to consume. Current: {_economyManager.GetResourceQuantity(_consumesType)}. Real-time consumption will resume when resources are available.");
            }
        }
    }
}


// --- 7. Example UI Display (for demonstrating events) ---
/// <summary>
/// A simple MonoBehaviour to demonstrate how to subscribe to EconomyManager events
/// and update a display (in this case, console logs).
/// In a real game, this would update UI Text elements (e.g., using TextMeshPro).
/// Attach this script to any GameObject in your scene.
/// </summary>
public class EconomyDisplay : MonoBehaviour
{
    // A local cache of resource quantities to detect changes and report old vs new values.
    private Dictionary<ResourceType, int> _lastKnownQuantities = new Dictionary<ResourceType, int>();

    void OnEnable()
    {
        // Subscribe to relevant EconomyManager events when this component becomes active.
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnResourceChanged += HandleResourceChanged;
            EconomyManager.Instance.OnInsufficientFunds += HandleInsufficientFunds;
            EconomyManager.Instance.OnTransactionAttempted += HandleTransactionAttempted;

            // Display initial state of all resources.
            Debug.Log("--- EconomyDisplay: Initial Economy State ---");
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int quantity = EconomyManager.Instance.GetResourceQuantity(type);
                _lastKnownQuantities[type] = quantity; // Cache initial quantity.
                Debug.Log($"Initial {type}: {quantity}");
            }
            Debug.Log("-------------------------------------------");
        }
        else
        {
            Debug.LogError("EconomyDisplay: EconomyManager.Instance is null. Cannot subscribe to events.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe from events when this component is disabled or destroyed.
        // This is crucial to prevent memory leaks (preventing the manager from holding a reference
        // to a destroyed object, which would cause a NullReferenceException when it tries to invoke the event).
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnResourceChanged -= HandleResourceChanged;
            EconomyManager.Instance.OnInsufficientFunds -= HandleInsufficientFunds;
            EconomyManager.Instance.OnTransactionAttempted -= HandleTransactionAttempted;
        }
    }

    /// <summary>
    /// Event handler for when a resource's quantity changes.
    /// </summary>
    private void HandleResourceChanged(ResourceType type, int newQuantity, int oldQuantity)
    {
        int change = newQuantity - oldQuantity;
        Debug.Log($"[EconomyDisplay] {type}: {oldQuantity} -> {newQuantity} ({(change > 0 ? "+" : "")}{change})");
        _lastKnownQuantities[type] = newQuantity; // Update cached quantity.
    }

    /// <summary>
    /// Event handler for when a resource removal fails due to insufficient funds.
    /// </summary>
    private void HandleInsufficientFunds(ResourceType type, int attemptedQuantity)
    {
        Debug.LogWarning($"[EconomyDisplay ALERT] Not enough {type}! Tried to use {attemptedQuantity}, but only had {_lastKnownQuantities[type]}.");
    }

    /// <summary>
    /// Event handler for when any transaction is attempted, useful for logging.
    /// </summary>
    private void HandleTransactionAttempted(TransactionDefinition transaction, bool success)
    {
        Debug.Log($"[EconomyDisplay LOG] Transaction attempt: '{transaction?.GetDescription() ?? "N/A"}' - {(success ? "SUCCESS" : "FAILED")}");
    }
}


// --- 8. Example Trigger for Manual Transactions (e.g., a button click) ---
/// <summary>
/// A component to simulate user interaction or event-driven transactions.
/// This could represent a "Build" button, "Buy" button, "Train Unit" button, etc.
/// Attach this script to a GameObject, configure its transaction in the Inspector,
/// and use the Context Menu or a UI button to trigger it.
/// </summary>
public class TransactionTrigger : MonoBehaviour
{
    [Header("Transaction Settings")]
    [Tooltip("The transaction to attempt when triggered.")]
    [SerializeField] private TransactionDefinition _transaction;

    [Tooltip("If greater than 0, the transaction will automatically trigger at this interval.")]
    [SerializeField] private float _autoTriggerInterval = 0f;

    private float _timer;

    private void Start()
    {
        // Ensure the transaction definition is not null.
        if (_transaction == null)
        {
            _transaction = new TransactionDefinition();
        }
    }

    void Update()
    {
        // Handle automatic triggering if enabled.
        if (_autoTriggerInterval > 0)
        {
            _timer += Time.deltaTime;
            if (_timer >= _autoTriggerInterval)
            {
                TryTriggerTransaction();
                _timer = 0f; // Reset timer for the next interval.
            }
        }
    }

    /// <summary>
    /// Public method to manually trigger the configured transaction.
    /// This can be hooked up to a Unity UI Button's OnClick event or called from other scripts.
    /// A ContextMenu attribute allows triggering directly from the Unity Editor's Inspector.
    /// </summary>
    [ContextMenu("Trigger Transaction Now")]
    public void TryTriggerTransaction()
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("TransactionTrigger: EconomyManager.Instance is null. Cannot perform transaction.");
            return;
        }

        Debug.Log($"TransactionTrigger: Attempting to apply transaction: {_transaction.GetDescription()}");
        bool success = EconomyManager.Instance.TryApplyTransaction(_transaction);

        if (success)
        {
            Debug.Log("TransactionTrigger: Transaction successful!");
        }
        else
        {
            Debug.Log("TransactionTrigger: Transaction failed (likely insufficient resources).");
        }
    }

    // --- Editor Context Menu Helpers for setting up example transactions quickly ---
    // These methods create specific TransactionDefinition objects.
    // In a real project, you might use ScriptableObjects for more complex, reusable definitions.

    [ContextMenu("Setup Example Buy 5 Wood for 10 Gold")]
    private void SetupBuyWoodTransaction()
    {
        _transaction = new TransactionDefinition(
            costs: new Dictionary<ResourceType, int> { { ResourceType.Gold, 10 } },
            gains: new Dictionary<ResourceType, int> { { ResourceType.Wood, 5 } }
        );
        Debug.Log("TransactionTrigger: Configured 'Buy 5 Wood' transaction.");
    }

    [ContextMenu("Setup Example Build House (Cost: 20 Wood, 10 Stone, 5 Gold)")]
    private void SetupBuildHouseTransaction()
    {
        _transaction = new TransactionDefinition(
            costs: new Dictionary<ResourceType, int>
            {
                { ResourceType.Wood, 20 },
                { ResourceType.Stone, 10 },
                { ResourceType.Gold, 5 }
            }
        );
        Debug.Log("TransactionTrigger: Configured 'Build House' transaction.");
    }

    [ContextMenu("Setup Example Harvest Food (Gain: 10 Food)")]
    private void SetupHarvestFoodTransaction()
    {
        _transaction = new TransactionDefinition(
            gains: new Dictionary<ResourceType, int> { { ResourceType.Food, 10 } }
        );
        Debug.Log("TransactionTrigger: Configured 'Harvest Food' transaction.");
    }
}
```

---

### How to Use This in Your Unity Project:

1.  **Create a C# Script:** Save the entire code block above as `EconomySystem.cs` in your Unity project's Assets folder.
2.  **Create the EconomyManager:**
    *   Create an empty GameObject in your scene (e.g., `_GameManager`).
    *   Add the `EconomyManager` component to this GameObject.
    *   In the Inspector, you can set `Initial Resources` for your game (e.g., 100 Gold, 50 Wood).
3.  **Add an EconomyDisplay (Optional but Recommended):**
    *   Create another empty GameObject (e.g., `_EconomyDisplay`).
    *   Add the `EconomyDisplay` component to it.
    *   Run the scene. You will see console logs showing initial resource states and real-time updates.
4.  **Create Resource Producers:**
    *   Create an empty GameObject (e.g., `LumberMill`).
    *   Add the `ResourceProducer` component to it.
    *   In the Inspector, set `Produces Type` to `Wood` and `Production Rate Per Second` to `1.5` (or any value).
    *   Repeat for other resources (e.g., `StoneMine` producing `Stone`).
5.  **Create Resource Consumers:**
    *   Create an empty GameObject (e.g., `CityPopulation`).
    *   Add the `ResourceConsumer` component to it.
    *   In the Inspector, set `Consumes Type` to `Food` and `Consumption Rate Per Second` to `0.5`.
    *   Repeat for other consumption needs (e.g., `PowerPlant` consuming `Energy`).
6.  **Create Transaction Triggers:**
    *   Create an empty GameObject (e.g., `UI_BuildButton_House`).
    *   Add the `TransactionTrigger` component to it.
    *   In the Inspector, you'll see a `Transaction Definition` field.
        *   Right-click on the `TransactionTrigger` component header in the Inspector.
        *   Choose one of the "Setup Example..." options (e.g., `Setup Example Build House`). This will populate the transaction costs/gains.
        *   You can also set an `Auto Trigger Interval` (e.g., `5` seconds) to have it automatically try the transaction.
    *   Run the scene. Observe the console logs for production, consumption, and transaction attempts. You can also manually trigger transactions using the "Trigger Transaction Now" context menu item.

This setup provides a complete, modular, and extensible real-time economy system for your Unity projects!