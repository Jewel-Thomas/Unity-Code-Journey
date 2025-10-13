// Unity Design Pattern Example: OutfitChangeSystem
// This script demonstrates the OutfitChangeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'OutfitChangeSystem' design pattern in Unity, while not a formal GoF pattern, represents a common system in games for dynamically swapping character visual components (clothing, armor, accessories). It typically involves managing different mesh renderers, materials, and associated data to change a character's appearance in real-time.

This example demonstrates a practical implementation using several core Unity features and design principles:

1.  **Data-Driven Design (ScriptableObject):** Item definitions are stored in `ScriptableObject` assets, separating data from logic.
2.  **Component-Based Architecture (MonoBehaviour):** The `OutfitManager` is a `MonoBehaviour` attached to the character, encapsulating outfit change logic.
3.  **Event System (UnityEvent):** `OnOutfitChanged` allows other systems (UI, stats, etc.) to react to outfit changes.
4.  **Clear Mapping:** A system to map abstract item IDs to concrete `SkinnedMeshRenderer` components on the character model.
5.  **State Management:** The `OutfitManager` keeps track of which items are currently equipped in each slot.

---

### Project Setup in Unity:

1.  **Create a C# Script:** Name it `OutfitChangeSystem.cs` and paste the code below.
2.  **Create a Character Model:**
    *   Import or create a 3D character model. This model should ideally have a base body mesh and then *separate child GameObjects*, each containing a `SkinnedMeshRenderer` for different equipable items (e.g., a "Default_Torso_Mesh", "Leather_Armor_Torso_Mesh", "Cloth_Pants_Mesh", "Plate_Pants_Mesh", etc.).
    *   All these `SkinnedMeshRenderer`s should share the same `Animator` (if your character is animated) and bone hierarchy as the base body.
    *   **Crucially, ensure all these individual `SkinnedMeshRenderer`s are initially DISABLED.** The `OutfitManager` will enable them.
3.  **Attach `OutfitManager`:** Drag the `OutfitChangeSystem.cs` script onto the root `GameObject` of your character model.
4.  **Create Outfit Item Data:**
    *   In the Unity Project window, right-click -> `Create` -> `Outfit System` -> `Outfit Item Data`.
    *   Create several of these `ScriptableObject` assets (e.g., "LeatherChestpiece", "FancyHelmet", "Jeans", "Boots").
    *   For each, fill in its `ItemID` (must be unique, e.g., "LeatherChest_01"), `ItemName`, `SlotType`, and optionally an `Icon`.
5.  **Configure `OutfitManager`:**
    *   Select your character model in the Hierarchy.
    *   In the Inspector, locate the `OutfitManager` component.
    *   Expand the `Equipped Items` section.
    *   Drag your created `OutfitItemData` assets into the respective slots if you want a default outfit.
    *   Expand the `Renderer Mappings` section.
    *   Increase the size of the list.
    *   For each `OutfitItemData` you created, create an entry here:
        *   Copy the `ItemID` from your `OutfitItemData` asset.
        *   Drag the corresponding `SkinnedMeshRenderer` (from your character model's children) into the `Target Renderer` slot.
        *   *Example:* For `OutfitItemData` with `ItemID` "LeatherChest_01", drag the `SkinnedMeshRenderer` of your "Leather_Armor_Torso_Mesh" GameObject into its `Target Renderer` slot.
6.  **Add a `TestOutfitChanger` (Optional, for quick demo):**
    *   Create an empty `GameObject` in your scene, name it `OutfitTester`.
    *   Attach the `TestOutfitChanger.cs` script (provided below) to it.
    *   Drag your character's `OutfitManager` into the `Outfit Manager` field.
    *   Drag your created `OutfitItemData` assets into the corresponding item fields (`HelmetItem`, `ChestItem`, `PantsItem`).
7.  **Run the Scene:** Press play and use the keys (or UI buttons if you implement them) to change the character's outfit.

---

### OutfitChangeSystem.cs

```csharp
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; // For dictionary lookup safety

namespace OutfitSystem
{
    // =========================================================================================
    // 1. OutfitSlotType Enum
    //    Defines the available slots on a character where items can be equipped.
    //    Expand this enum to include all the body parts you want to support (e.g., Shoulders, Gloves, Boots, etc.)
    // =========================================================================================
    public enum OutfitSlotType
    {
        None,
        Head,
        Torso,
        Legs,
        Feet,
        Hands,
        Accessory1,
        Accessory2,
        // Add more as needed
    }

    // =========================================================================================
    // 2. OutfitItemData ScriptableObject
    //    This ScriptableObject represents the data for a single equipable item.
    //    Using ScriptableObjects allows us to define items as assets in the Unity editor,
    //    decoupling their data from runtime logic.
    // =========================================================================================
    [CreateAssetMenu(fileName = "NewOutfitItem", menuName = "Outfit System/Outfit Item Data", order = 1)]
    public class OutfitItemData : ScriptableObject
    {
        [Tooltip("A unique identifier for this item. Used to link to actual renderers.")]
        public string ItemID;

        [Tooltip("The display name of the item.")]
        public string ItemName;

        [Tooltip("The slot type this item occupies.")]
        public OutfitSlotType SlotType;

        [Tooltip("An icon to display in UI (optional).")]
        public Sprite Icon;

        // You can add more item-specific data here, such as:
        // public int ArmorRating;
        // public List<StatModifier> StatModifiers;
        // public Material itemMaterialOverride; // If you want to change materials dynamically
    }

    // =========================================================================================
    // 3. RendererMapping Struct
    //    A helper struct to link an OutfitItemData's ItemID to an actual SkinnedMeshRenderer
    //    component on the character model. This is crucial for the OutfitManager to know
    //    which visual component to enable/disable for a given item.
    // =========================================================================================
    [System.Serializable]
    public struct RendererMapping
    {
        [Tooltip("The unique ID of the OutfitItemData asset this renderer represents.")]
        public string ItemID;

        [Tooltip("The SkinnedMeshRenderer component on the character model that corresponds to this item.")]
        public SkinnedMeshRenderer TargetRenderer;
    }

    // =========================================================================================
    // 4. OutfitManager MonoBehaviour
    //    This is the core component that manages the equipping and unequipping of outfit items
    //    on a character. It handles the visual representation by enabling/disabling
    //    SkinnedMeshRenderers and manages the character's current outfit state.
    // =========================================================================================
    [DisallowMultipleComponent] // Ensures only one OutfitManager exists on a GameObject
    public class OutfitManager : MonoBehaviour
    {
        [Header("Default Equipped Items (Optional)")]
        [Tooltip("Items to be equipped by default when the game starts.")]
        [SerializeField] private List<OutfitItemData> _defaultEquippedItems = new List<OutfitItemData>();

        [Header("Renderer Mappings")]
        [Tooltip("Links OutfitItemData ItemIDs to actual SkinnedMeshRenderer components on this character.")]
        [SerializeField] private List<RendererMapping> _rendererMappings = new List<RendererMapping>();

        // Dictionary to quickly map an ItemID to its SkinnedMeshRenderer
        private Dictionary<string, SkinnedMeshRenderer> _itemRendererMap = new Dictionary<string, SkinnedMeshRenderer>();

        // Dictionary to track which item is currently equipped in each slot
        private Dictionary<OutfitSlotType, OutfitItemData> _equippedItems = new Dictionary<OutfitSlotType, OutfitItemData>();

        // Event fired when the outfit changes. Useful for UI, stat updates, etc.
        public UnityEvent<OutfitSlotType, OutfitItemData> OnOutfitChanged = new UnityEvent<OutfitSlotType, OutfitItemData>();
        public UnityEvent<OutfitSlotType> OnOutfitUnequipped = new UnityEvent<OutfitSlotType>();

        // Public accessor to get the currently equipped item in a slot
        public OutfitItemData GetEquippedItem(OutfitSlotType slotType)
        {
            _equippedItems.TryGetValue(slotType, out OutfitItemData item);
            return item;
        }

        private void Awake()
        {
            InitializeRendererMappings();
        }

        private void Start()
        {
            // Equip default items after all mappings are set up
            foreach (OutfitItemData item in _defaultEquippedItems)
            {
                if (item != null)
                {
                    EquipItem(item, true); // Suppress event for initial load if desired
                }
            }
            // Trigger events for all initially equipped items
            foreach (var kvp in _equippedItems)
            {
                OnOutfitChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Initializes the internal dictionary that maps ItemIDs to SkinnedMeshRenderers.
        /// Disables all renderers initially.
        /// </summary>
        private void InitializeRendererMappings()
        {
            _itemRendererMap.Clear();
            foreach (var mapping in _rendererMappings)
            {
                if (string.IsNullOrEmpty(mapping.ItemID))
                {
                    Debug.LogWarning($"OutfitManager on {gameObject.name}: Renderer mapping has an empty ItemID. Skipping.");
                    continue;
                }
                if (mapping.TargetRenderer == null)
                {
                    Debug.LogWarning($"OutfitManager on {gameObject.name}: Renderer mapping for ItemID '{mapping.ItemID}' has a null TargetRenderer. Skipping.");
                    continue;
                }

                // Check for duplicate ItemIDs to prevent dictionary errors
                if (_itemRendererMap.ContainsKey(mapping.ItemID))
                {
                    Debug.LogWarning($"OutfitManager on {gameObject.name}: Duplicate ItemID '{mapping.ItemID}' found in renderer mappings. The first one will be used.");
                    continue;
                }

                _itemRendererMap.Add(mapping.ItemID, mapping.TargetRenderer);
                // Ensure all equipable renderers are disabled initially
                mapping.TargetRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Equips a new outfit item onto the character.
        /// This will unequip any existing item in the same slot.
        /// </summary>
        /// <param name="newItemData">The OutfitItemData to equip.</param>
        /// <param name="silent">If true, suppresses the OnOutfitChanged event for this equip.</param>
        public void EquipItem(OutfitItemData newItemData, bool silent = false)
        {
            if (newItemData == null)
            {
                Debug.LogWarning($"OutfitManager on {gameObject.name}: Attempted to equip a null item.");
                return;
            }
            if (string.IsNullOrEmpty(newItemData.ItemID))
            {
                Debug.LogError($"OutfitManager on {gameObject.name}: OutfitItemData '{newItemData.name}' has an empty ItemID. Cannot equip.");
                return;
            }

            OutfitSlotType targetSlot = newItemData.SlotType;

            // 1. Unequip any existing item in the target slot
            if (_equippedItems.ContainsKey(targetSlot))
            {
                OutfitItemData currentlyEquipped = _equippedItems[targetSlot];
                if (currentlyEquipped == newItemData) // Trying to equip the same item
                {
                    // Debug.Log($"OutfitManager on {gameObject.name}: Item '{newItemData.name}' is already equipped in slot {targetSlot}.");
                    return; // No change needed
                }
                UnequipItem(targetSlot, silent);
            }

            // 2. Find the SkinnedMeshRenderer for the new item
            if (_itemRendererMap.TryGetValue(newItemData.ItemID, out SkinnedMeshRenderer targetRenderer))
            {
                // 3. Enable the new item's renderer
                targetRenderer.enabled = true;
                _equippedItems[targetSlot] = newItemData;

                // 4. Invoke event if not silent
                if (!silent)
                {
                    OnOutfitChanged?.Invoke(targetSlot, newItemData);
                    Debug.Log($"OutfitManager on {gameObject.name}: Equipped '{newItemData.ItemName}' in slot {targetSlot}.");
                }
            }
            else
            {
                Debug.LogWarning($"OutfitManager on {gameObject.name}: No renderer mapping found for ItemID '{newItemData.ItemID}'. Item '{newItemData.name}' not equipped.");
            }
        }

        /// <summary>
        /// Unequips the item in a specific slot.
        /// </summary>
        /// <param name="slotType">The slot from which to unequip the item.</param>
        /// <param name="silent">If true, suppresses the OnOutfitChanged/OnOutfitUnequipped event for this unequip.</param>
        public void UnequipItem(OutfitSlotType slotType, bool silent = false)
        {
            if (_equippedItems.TryGetValue(slotType, out OutfitItemData equippedItem))
            {
                // 1. Find the SkinnedMeshRenderer for the currently equipped item
                if (_itemRendererMap.TryGetValue(equippedItem.ItemID, out SkinnedMeshRenderer targetRenderer))
                {
                    // 2. Disable the renderer
                    targetRenderer.enabled = false;
                    _equippedItems.Remove(slotType);

                    // 3. Invoke events if not silent
                    if (!silent)
                    {
                        OnOutfitUnequipped?.Invoke(slotType);
                        OnOutfitChanged?.Invoke(slotType, null); // Indicate no item in this slot
                        Debug.Log($"OutfitManager on {gameObject.name}: Unequipped '{equippedItem.ItemName}' from slot {slotType}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"OutfitManager on {gameObject.name}: No renderer mapping found for ItemID '{equippedItem.ItemID}' during unequip. Item '{equippedItem.name}' removed from _equippedItems, but visual may persist.");
                    _equippedItems.Remove(slotType); // Still remove from internal state
                    if (!silent)
                    {
                        OnOutfitUnequipped?.Invoke(slotType);
                        OnOutfitChanged?.Invoke(slotType, null);
                    }
                }
            }
            else
            {
                // Debug.Log($"OutfitManager on {gameObject.name}: No item equipped in slot {slotType} to unequip.");
            }
        }

        /// <summary>
        /// Unequips all items from the character.
        /// </summary>
        public void UnequipAll()
        {
            // Iterate over a copy of the keys to avoid modifying collection during iteration
            foreach (OutfitSlotType slot in _equippedItems.Keys.ToList())
            {
                UnequipItem(slot);
            }
            Debug.Log($"OutfitManager on {gameObject.name}: Unequipped all items.");
        }

        /// <summary>
        /// Internal method to check if a specific item (by ItemID) is currently active (enabled) on the character.
        /// </summary>
        /// <param name="itemID">The unique ID of the item to check.</param>
        /// <returns>True if the item's renderer is found and enabled, false otherwise.</returns>
        public bool IsItemRendererActive(string itemID)
        {
            if (_itemRendererMap.TryGetValue(itemID, out SkinnedMeshRenderer renderer))
            {
                return renderer.enabled;
            }
            return false;
        }

        // Optional: Method to refresh all renderers (e.g., if character model changes)
        public void RefreshOutfitRenderers()
        {
            // Disable all renderers first
            foreach (var renderer in _itemRendererMap.Values)
            {
                renderer.enabled = false;
            }

            // Re-enable only the currently equipped ones
            foreach (var kvp in _equippedItems)
            {
                if (_itemRendererMap.TryGetValue(kvp.Value.ItemID, out SkinnedMeshRenderer renderer))
                {
                    renderer.enabled = true;
                }
                else
                {
                    Debug.LogWarning($"OutfitManager on {gameObject.name}: Equipped item '{kvp.Value.name}' (ID: {kvp.Value.ItemID}) has no corresponding renderer mapping. This item will not be visible.");
                }
            }
            Debug.Log($"OutfitManager on {gameObject.name}: Renderers refreshed.");
        }

        // --- Example of how other systems might listen to outfit changes ---
        // (These methods would typically be in a separate UI or StatManager script)
        public void HandleOutfitChangeUI(OutfitSlotType slot, OutfitItemData newItem)
        {
            string itemName = (newItem != null) ? newItem.ItemName : "Nothing";
            Debug.Log($"<color=cyan>UI Update:</color> Slot '{slot}' now has: {itemName}");
            // Example: Update an inventory UI slot icon and text
            // if (newItem != null && inventoryUISlot != null) {
            //     inventoryUISlot.SetIcon(newItem.Icon);
            //     inventoryUISlot.SetText(newItem.ItemName);
            // } else { /* clear slot */ }
        }

        public void HandleOutfitChangeStats(OutfitSlotType slot, OutfitItemData newItem)
        {
            // Debug.Log($"<color=green>Stat System Update:</color> Recalculating stats due to change in slot: {slot}");
            // Example:
            // if (newItem != null) {
            //     characterStats.AddModifiers(newItem.StatModifiers);
            // } else {
            //     characterStats.RemoveModifiers(oldItem.StatModifiers);
            // }
            // characterStats.RecalculateStats();
        }
    }
}
```

---

### TestOutfitChanger.cs (Example Usage)

This script demonstrates how to interact with the `OutfitManager` in your scene.

```csharp
using UnityEngine;
using OutfitSystem; // Make sure to use the correct namespace

public class TestOutfitChanger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your character's OutfitManager here.")]
    public OutfitManager OutfitManager;

    [Header("Outfit Items for Testing")]
    public OutfitItemData HelmetItem;
    public OutfitItemData ChestItem;
    public OutfitItemData PantsItem;
    public OutfitItemData AnotherChestItem; // To demonstrate swapping

    void Start()
    {
        if (OutfitManager == null)
        {
            Debug.LogError("OutfitManager not assigned to TestOutfitChanger!", this);
            enabled = false;
            return;
        }

        // Optionally subscribe to the outfit change event here for testing purposes
        OutfitManager.OnOutfitChanged.AddListener(OnOutfitChangedCallback);
        OutfitManager.OnOutfitUnequipped.AddListener(OnOutfitUnequippedCallback);

        // Example: Force equip a default item at start, overriding initial setup if needed
        // OutfitManager.EquipItem(ChestItem);
    }

    void OnDestroy()
    {
        // Always remember to unsubscribe from events to prevent memory leaks
        if (OutfitManager != null)
        {
            OutfitManager.OnOutfitChanged.RemoveListener(OnOutfitChangedCallback);
            OutfitManager.OnOutfitUnequipped.RemoveListener(OnOutfitUnequippedCallback);
        }
    }

    void Update()
    {
        // Example: Equip items with key presses
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OutfitManager.EquipItem(HelmetItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OutfitManager.EquipItem(ChestItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OutfitManager.EquipItem(PantsItem);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Demonstrate swapping an item in the same slot
            OutfitManager.EquipItem(AnotherChestItem);
        }

        // Example: Unequip items
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OutfitManager.UnequipItem(OutfitSlotType.Head);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            OutfitManager.UnequipItem(OutfitSlotType.Torso);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            OutfitManager.UnequipItem(OutfitSlotType.Legs);
        }

        // Example: Unequip all
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OutfitManager.UnequipAll();
        }
    }

    // Callback function for when an outfit item changes
    void OnOutfitChangedCallback(OutfitSlotType slot, OutfitItemData newItem)
    {
        if (newItem != null)
        {
            Debug.Log($"<color=yellow>[TestOutfitChanger]</color> Outfit changed! Slot: {slot}, New Item: {newItem.ItemName} (ID: {newItem.ItemID})");
        }
        else
        {
            Debug.Log($"<color=yellow>[TestOutfitChanger]</color> Outfit changed! Slot: {slot}, Item Unequipped.");
        }

        // You could also call the OutfitManager's internal UI/Stat handlers here for demonstration
        // OutfitManager.HandleOutfitChangeUI(slot, newItem);
        // OutfitManager.HandleOutfitChangeStats(slot, newItem);
    }

    void OnOutfitUnequippedCallback(OutfitSlotType slot)
    {
        Debug.Log($"<color=yellow>[TestOutfitChanger]</color> Item explicitly unequipped from slot: {slot}.");
    }
}
```