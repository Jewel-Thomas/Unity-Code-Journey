// Unity Design Pattern Example: ItemDurabilitySystem
// This script demonstrates the ItemDurabilitySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ItemDurabilitySystem' design pattern in Unity focuses on creating a robust, reusable, and modular way to manage the wear and tear of items within a game. It encapsulates all durability-related logic into a dedicated component, allowing any item to become "durable" simply by attaching this component. This approach promotes:

1.  **Encapsulation:** All durability state (`currentDurability`, `maxDurability`) and behavior (`DecreaseDurability`, `RepairDurability`, `IsBroken`) are self-contained.
2.  **Reusability:** The same `ItemDurability` script can be used for swords, shields, tools, armor, or any other game object that needs durability.
3.  **Modularity:** Durability management is separated from other item concerns (e.g., how an item deals damage, its inventory slot, its visual model).
4.  **Decoupling (via Events):** Other systems (e.g., UI, player controllers, repair stations) can interact with an item's durability and react to changes without direct knowledge of its internal workings, typically using events (Observer Pattern).

This example demonstrates the core `ItemDurability` component and two example consumer scripts: `PlayerController` (which decreases durability) and `RepairStation` (which repairs it).

---

## 1. Core Component: `ItemDurability.cs`

This is the heart of the system. It manages an item's durability value, provides methods to modify it, and broadcasts events when its state changes.

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// The ItemDurabilitySystem pattern encapsulates the logic and state
/// for an item's durability, making it a reusable and modular component.
///
/// It promotes separation of concerns by isolating durability management
/// from other item-specific logic (e.g., damage, inventory, visual effects).
///
/// This component can be attached to any GameObject that represents an item
/// requiring durability tracking (e.g., a sword, armor, tool, consumable).
///
/// Key aspects of the pattern demonstrated here:
/// 1.  **Encapsulation:** Durability logic (decrease, repair, check broken state)
///     is self-contained within this component.
/// 2.  **Reusability:** Any item can gain durability simply by attaching this script.
/// 3.  **Modularity:** Durability can be managed independently of other item features.
/// 4.  **Events (Observer Pattern):** Notifies other systems (e.g., UI, player inventory,
///     visual effects) when durability changes or an item breaks,
///     without direct coupling. This is crucial for UI updates, sound effects,
///     gameplay rule changes (e.g., cannot use broken item), etc.
/// </summary>
public class ItemDurability : MonoBehaviour
{
    // --- Public Properties (Read-only access to internal state) ---
    public float CurrentDurability => _currentDurability;
    public float MaxDurability => _maxDurability;

    /// <summary>
    /// Returns the durability as a normalized value (0.0 to 1.0).
    /// Useful for UI elements like durability bars.
    /// </summary>
    public float NormalizedDurability
    {
        get
        {
            if (_maxDurability <= 0) return 0f; // Prevent division by zero
            return _currentDurability / _maxDurability;
        }
    }

    /// <summary>
    /// Checks if the item's durability has reached zero or below.
    /// </summary>
    public bool IsBroken => _currentDurability <= 0;

    // --- Events (Observer Pattern implementation) ---
    /// <summary>
    /// Invoked whenever the durability changes (decreases or repairs).
    /// Parameters: currentDurability, maxDurability, normalizedDurability.
    /// Subscribers can use this to update UI, play sounds, etc.
    /// </summary>
    public event Action<float, float, float> OnDurabilityChanged;

    /// <summary>
    /// Invoked specifically when the item's durability reaches 0 or less,
    /// transitioning from a usable state to a broken state.
    /// Subscribers can react to item breakage (e.g., unequip, play sound, show message).
    /// </summary>
    public event Action OnItemBroken;

    /// <summary>
    /// Invoked specifically when a broken item is repaired back to a usable state (durability > 0).
    /// Subscribers can react to an item becoming usable again (e.g., re-enable UI, play repair sound).
    /// </summary>
    public event Action OnItemRepairedFromBroken;

    // --- Private Fields (Managed internally, exposed to Inspector) ---
    [Tooltip("The maximum durability this item can have.")]
    [SerializeField]
    private float _maxDurability = 100f;

    [Tooltip("The current durability of the item. Clamped between 0 and MaxDurability.")]
    [SerializeField]
    private float _currentDurability;

    // Internal flag to track if the item was broken in the previous state check.
    // Used to correctly trigger OnItemBroken / OnItemRepairedFromBroken only on state transitions.
    private bool _wasBrokenLastCheck = false;

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        // Initialize current durability if not set or ensure it's within bounds.
        // This makes it robust even if designers forget to set _currentDurability in Inspector.
        if (_currentDurability <= 0 && _maxDurability > 0)
        {
            _currentDurability = _maxDurability;
        }
        else
        {
            _currentDurability = Mathf.Clamp(_currentDurability, 0, _maxDurability);
        }

        // Initialize the broken state tracker based on the item's initial durability.
        _wasBrokenLastCheck = IsBroken;

        // Optionally, invoke OnDurabilityChanged on start to ensure any UI is up-to-date.
        OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability, NormalizedDurability);
    }

    // --- Public Methods (Interactions with the durability system) ---

    /// <summary>
    /// Decreases the item's current durability by a specified amount.
    /// This is typically called when an item is used, takes damage, or performs an action.
    /// </summary>
    /// <param name="amount">The amount to decrease durability by. Must be positive.</param>
    public void DecreaseDurability(float amount)
    {
        // --- Input Validation ---
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to decrease durability by a negative amount ({amount}) on {gameObject.name}. Use RepairDurability instead.", this);
            return;
        }

        if (IsBroken)
        {
            // Item is already broken, cannot decrease durability further.
            // Avoids redundant operations and ensures state consistency.
            // Debug.Log($"{gameObject.name} is already broken, cannot decrease durability.");
            return;
        }

        // --- Durability Modification ---
        _currentDurability -= amount;
        _currentDurability = Mathf.Max(0, _currentDurability); // Ensure durability never goes below 0.

        Debug.Log($"{gameObject.name} durability decreased by {amount}. Current: {_currentDurability}/{_maxDurability}");

        // --- Event Notification ---
        OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability, NormalizedDurability);

        // --- State Transition Check (Broken) ---
        // If the item just became broken and wasn't broken before, trigger the OnItemBroken event.
        if (IsBroken && !_wasBrokenLastCheck)
        {
            OnItemBroken?.Invoke();
            Debug.Log($"!!! {gameObject.name} IS NOW BROKEN !!!", this);
            _wasBrokenLastCheck = true; // Update the broken state tracker.
        }
    }

    /// <summary>
    /// Repairs the item's current durability by a specified amount.
    /// This is typically called by a repair station, a crafting mechanic, or a special ability.
    /// </summary>
    /// <param name="amount">The amount to increase durability by. Must be positive.</param>
    public void RepairDurability(float amount)
    {
        // --- Input Validation ---
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to repair durability by a negative amount ({amount}) on {gameObject.name}. Use DecreaseDurability instead.", this);
            return;
        }

        if (_currentDurability >= _maxDurability)
        {
            // Item is already at max durability, cannot repair further.
            Debug.Log($"{gameObject.name} is already at max durability, cannot repair further.", this);
            return;
        }

        bool wasBrokenBeforeRepair = IsBroken; // Store state *before* repair for transition check.

        // --- Durability Modification ---
        _currentDurability += amount;
        _currentDurability = Mathf.Min(_maxDurability, _currentDurability); // Ensure durability never exceeds MaxDurability.

        Debug.Log($"{gameObject.name} durability repaired by {amount}. Current: {_currentDurability}/{_maxDurability}");

        // --- Event Notification ---
        OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability, NormalizedDurability);

        // --- State Transition Check (Repaired from Broken) ---
        // If the item *was* broken and is now *not* broken, trigger the OnItemRepairedFromBroken event.
        if (wasBrokenBeforeRepair && !IsBroken)
        {
            OnItemRepairedFromBroken?.Invoke();
            Debug.Log($"--- {gameObject.name} HAS BEEN REPAIRED FROM BROKEN STATE! ---", this);
            _wasBrokenLastCheck = false; // Update the broken state tracker.
        }
    }

    /// <summary>
    /// Fully repairs the item to its maximum durability.
    /// Convenience method for a complete repair action.
    /// </summary>
    public void RepairFully()
    {
        RepairDurability(_maxDurability - _currentDurability); // Repair by the exact amount needed to reach max.
    }
}
```

---

## 2. Example Usage: `PlayerController.cs` (Decreasing Durability)

This script simulates a player interacting with an equipped item, demonstrating how to decrease its durability and react to its events.

```csharp
using UnityEngine;

/// <summary>
/// Example of a PlayerController that interacts with an ItemDurability component.
/// This script demonstrates how a 'user' of an item (e.g., a player) would
/// decrease the durability of an equipped item.
///
/// It also shows how to subscribe to the durability events to react to changes,
/// such as logging a message or updating a UI element.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Tooltip("The ItemDurability component of the currently equipped item.")]
    [SerializeField]
    private ItemDurability equippedItemDurability;

    [Tooltip("The amount of durability to decrease when the item is 'used'.")]
    [SerializeField]
    private float useDurabilityLoss = 5f;

    [Tooltip("The key to press to 'use' the equipped item.")]
    public KeyCode useItemKey = KeyCode.Space;

    void Start()
    {
        // Initial setup and event subscription.
        if (equippedItemDurability == null)
        {
            Debug.LogError("PlayerController: No equipped item's ItemDurability component assigned! Please assign one in the Inspector.", this);
            return;
        }

        // Subscribe to events from the equipped item's durability component.
        // This allows the PlayerController to react to durability changes
        // without constantly polling the ItemDurability component.
        equippedItemDurability.OnDurabilityChanged += HandleDurabilityChanged;
        equippedItemDurability.OnItemBroken += HandleItemBroken;
        equippedItemDurability.OnItemRepairedFromBroken += HandleItemRepaired;

        Debug.Log($"PlayerController: Ready to use item '{equippedItemDurability.gameObject.name}'. Initial Durability: {equippedItemDurability.CurrentDurability}/{equippedItemDurability.MaxDurability}", this);
    }

    void OnDestroy()
    {
        // IMPORTANT: Unsubscribe from events to prevent memory leaks,
        // especially crucial if items are frequently equipped/unequipped or destroyed.
        if (equippedItemDurability != null)
        {
            equippedItemDurability.OnDurabilityChanged -= HandleDurabilityChanged;
            equippedItemDurability.OnItemBroken -= HandleItemBroken;
            equippedItemDurability.OnItemRepairedFromBroken -= HandleItemRepaired;
        }
    }

    void Update()
    {
        // Simulate item usage based on player input.
        if (Input.GetKeyDown(useItemKey))
        {
            UseEquippedItem();
        }
    }

    /// <summary>
    /// Simulates using the equipped item, which decreases its durability.
    /// This method demonstrates how a user component interacts with the ItemDurability.
    /// </summary>
    public void UseEquippedItem()
    {
        if (equippedItemDurability == null)
        {
            Debug.LogWarning("PlayerController: No item equipped to use.", this);
            return;
        }

        if (equippedItemDurability.IsBroken)
        {
            Debug.Log($"PlayerController: Cannot use {equippedItemDurability.gameObject.name} because it is broken! It needs repair.", this);
            return;
        }

        Debug.Log($"PlayerController: Using {equippedItemDurability.gameObject.name}...", this);
        equippedItemDurability.DecreaseDurability(useDurabilityLoss);
    }

    // --- Event Handlers (Reactions to ItemDurability events) ---

    private void HandleDurabilityChanged(float currentDurability, float maxDurability, float normalizedDurability)
    {
        Debug.Log($"PlayerController: Equipped item durability changed! Current: {currentDurability:F1}/{maxDurability:F1} ({normalizedDurability:P0})", this);
        // In a real game, you would typically update a UI durability bar or text here.
        // E.g., UIManager.Instance.UpdateDurabilityBar(normalizedDurability);
    }

    private void HandleItemBroken()
    {
        Debug.Log($"PlayerController: Oh no! My {equippedItemDurability.gameObject.name} just broke!", this);
        // In a real game, you might:
        // - Unequip the item automatically
        // - Play a broken sound/animation
        // - Display a prominent message to the player ("Your sword shattered!")
        // - Change the item's visual state (e.g., cracked texture, particle effects)
        // - Prevent further use by setting a flag or disabling collision/interaction components.
    }

    private void HandleItemRepaired()
    {
        Debug.Log($"PlayerController: Great! My {equippedItemDurability.gameObject.name} is now repaired and usable again!", this);
        // In a real game, you might:
        // - Re-enable item use
        // - Update UI status to show it's fixed
        // - Play a repair sound/animation
        // - Restore its original visual state
    }

    /// <summary>
    /// Public method to dynamically equip a new item. This demonstrates how the
    /// ItemDurabilitySystem allows for flexible item management.
    /// </summary>
    /// <param name="newItemDurability">The ItemDurability component of the new item to equip.</param>
    public void EquipItem(ItemDurability newItemDurability)
    {
        // Unsubscribe from events of the previously equipped item.
        if (equippedItemDurability != null)
        {
            equippedItemDurability.OnDurabilityChanged -= HandleDurabilityChanged;
            equippedItemDurability.OnItemBroken -= HandleItemBroken;
            equippedItemDurability.OnItemRepairedFromBroken -= HandleItemRepaired;
            Debug.Log($"PlayerController: Unequipped old item: {equippedItemDurability.gameObject.name}", this);
        }

        equippedItemDurability = newItemDurability;

        // Subscribe to events of the newly equipped item.
        if (equippedItemDurability != null)
        {
            equippedItemDurability.OnDurabilityChanged += HandleDurabilityChanged;
            equippedItemDurability.OnItemBroken += HandleItemBroken;
            equippedItemDurability.OnItemRepairedFromBroken += HandleItemRepaired;
            Debug.Log($"PlayerController: Equipped new item: {equippedItemDurability.gameObject.name}. Current Durability: {equippedItemDurability.CurrentDurability}/{equippedItemDurability.MaxDurability}", this);
            // Trigger an initial event for the new item to update any UI right away.
            equippedItemDurability.OnDurabilityChanged?.Invoke(equippedItemDurability.CurrentDurability, equippedItemDurability.MaxDurability, equippedItemDurability.NormalizedDurability);
        }
        else
        {
            Debug.LogWarning("PlayerController: Attempted to equip a null item.", this);
        }
    }
}
```

---

## 3. Example Usage: `RepairStation.cs` (Repairing Durability)

This script simulates an entity that can repair an item, demonstrating how an external system interacts with the `ItemDurability` component.

```csharp
using UnityEngine;

/// <summary>
/// Example of a RepairStation that interacts with an ItemDurability component.
/// This script demonstrates how an external entity (like a repair shop,
/// a crafting bench, or a special ability) can repair an item's durability.
///
/// It directly calls the public RepairDurability method on the target item.
/// </summary>
public class RepairStation : MonoBehaviour
{
    [Tooltip("The ItemDurability component of the item that this station will repair. Can be set dynamically.")]
    [SerializeField]
    private ItemDurability itemToRepair;

    [Tooltip("The amount of durability to restore per repair action.")]
    [SerializeField]
    private float repairAmount = 25f;

    [Tooltip("Key to press to initiate a repair.")]
    public KeyCode repairKey = KeyCode.R;

    void Start()
    {
        if (itemToRepair == null)
        {
            Debug.LogWarning("RepairStation: No item to repair assigned initially. Assign one dynamically or in Inspector if needed.", this);
        }
        else
        {
            Debug.Log($"RepairStation: Ready to repair {itemToRepair.gameObject.name}.", this);
        }
    }

    void Update()
    {
        // Simulate repair action based on player input.
        if (Input.GetKeyDown(repairKey))
        {
            PerformRepair();
        }
    }

    /// <summary>
    /// Performs a repair action on the assigned item.
    /// This method directly interacts with the ItemDurability component's public API.
    /// </summary>
    public void PerformRepair()
    {
        if (itemToRepair == null)
        {
            Debug.LogWarning("RepairStation: No item to repair currently assigned.", this);
            return;
        }

        Debug.Log($"RepairStation: Attempting to repair {itemToRepair.gameObject.name} by {repairAmount}...", this);
        itemToRepair.RepairDurability(repairAmount);
    }

    /// <summary>
    /// Fully repairs the assigned item.
    /// </summary>
    public void PerformFullRepair()
    {
        if (itemToRepair == null)
        {
            Debug.LogWarning("RepairStation: No item to repair currently assigned for full repair.", this);
            return;
        }

        Debug.Log($"RepairStation: Performing full repair on {itemToRepair.gameObject.name}...", this);
        itemToRepair.RepairFully();
    }

    /// <summary>
    /// Sets the item that this station will repair.
    /// This could be called by a player script when they interact with the station,
    /// passing their currently equipped or selected item. This demonstrates the
    /// dynamic nature of the ItemDurabilitySystem.
    /// </summary>
    /// <param name="newItem">The ItemDurability component of the item to be repaired.</param>
    public void SetItemToRepair(ItemDurability newItem)
    {
        itemToRepair = newItem;
        if (itemToRepair != null)
        {
            Debug.Log($"RepairStation: Item to repair set to {itemToRepair.gameObject.name}.", this);
        }
        else
        {
            Debug.Log("RepairStation: Item to repair cleared.", this);
        }
    }
}
```

---

## How to Set Up in Unity

Follow these steps to get the example working immediately in your Unity project:

1.  **Create C# Scripts:**
    *   In your Unity project's `Assets` folder, create a new C# Script named `ItemDurability.cs` and paste the `ItemDurability` code into it.
    *   Create another C# Script named `PlayerController.cs` and paste the `PlayerController` code into it.
    *   Create a third C# Script named `RepairStation.cs` and paste the `RepairStation` code into it.

2.  **Create the Item GameObject:**
    *   In the Hierarchy window, right-click -> `Create Empty`. Name it "Sword".
    *   Select the "Sword" GameObject. In the Inspector window, click "Add Component" and search for "ItemDurability". Add it.
    *   In the `ItemDurability` component, set its `Max Durability` to `100` (or any value you prefer). You can leave `Current Durability` as `0` for it to automatically initialize to `Max Durability` on Awake.

3.  **Create the Player GameObject:**
    *   In the Hierarchy, right-click -> `Create Empty`. Name it "Player".
    *   Select the "Player" GameObject. In the Inspector, click "Add Component" and search for "PlayerController". Add it.
    *   **Crucially:** Drag the "Sword" GameObject from the Hierarchy into the `Equipped Item Durability` slot of the `PlayerController` component.
    *   Set `Use Durability Loss` to `10` (or any value).
    *   Leave `Use Item Key` as `Space` (or choose another key).

4.  **Create the Repair Station GameObject:**
    *   In the Hierarchy, right-click -> `Create Empty`. Name it "RepairShop".
    *   Select the "RepairShop" GameObject. In the Inspector, click "Add Component" and search for "RepairStation". Add it.
    *   **Crucially:** Drag the "Sword" GameObject from the Hierarchy into the `Item To Repair` slot of the `RepairStation` component.
    *   Set `Repair Amount` to `25` (or any value).
    *   Leave `Repair Key` as `R` (or choose another key).

5.  **Run the Scene:**
    *   Press the **Play** button in the Unity Editor.
    *   Open the **Console** window (`Window > General > Console`) to see the logs.
    *   Press the `Space` key repeatedly. You will see logs indicating the sword's durability decreasing.
    *   Keep pressing `Space` until the sword breaks. A "!!! IS NOW BROKEN !!!" message will appear, and `PlayerController` will react.
    *   Try pressing `Space` again. You'll see a message that the item cannot be used because it's broken.
    *   Press the `R` key. You'll see logs indicating the sword's durability being repaired.
    *   If the item was broken, after the first `R` press, you'll see a "--- HAS BEEN REPAIRED FROM BROKEN STATE! ---" message, and `PlayerController` will react.
    *   You can now use `Space` again to decrease durability or `R` to repair it.

---

## Conclusion and Further Enhancements

This example provides a complete and practical foundation for an Item Durability System. It highlights how a component-based approach combined with the Observer pattern (using events) leads to highly modular, reusable, and easily extensible game systems.

**Potential Enhancements:**

*   **UI Integration:** Implement a simple UI text or slider to visually represent the item's durability, updating via the `OnDurabilityChanged` event.
*   **Visual/Audio Feedback:** Play sound effects or particle effects when durability decreases, when an item breaks, or when it's repaired.
*   **Durability Tiers/Effects:**
    *   Introduce thresholds (e.g., "low durability," "critical durability") that trigger different visual effects (cracked textures, smoke) or gameplay penalties (reduced damage, slower attack speed).
    *   These thresholds could also trigger specific events from `ItemDurability`.
*   **Different Damage Types:** Extend `DecreaseDurability` to take a "damage type" parameter, allowing certain items to be more or less resistant to specific types of durability damage.
*   **Item Types:** Create an `Item` base class or interface that includes a reference to `ItemDurability` (if present), allowing for a more unified item management system.
*   **Inventory System:** Integrate this with an inventory system where `PlayerController.EquipItem()` would be called when an item is moved to an equipped slot.
*   **Serialization:** Ensure `_currentDurability` and `_maxDurability` are saved and loaded correctly if you implement a save/load system.
*   **Repair Cost:** Add a cost to repairing items in the `RepairStation` (e.g., requires currency or specific materials).