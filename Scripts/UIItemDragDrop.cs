// Unity Design Pattern Example: UIItemDragDrop
// This script demonstrates the UIItemDragDrop pattern in Unity
// Generated automatically - ready to use in your Unity project

The UI Item Drag & Drop pattern is fundamental for many interactive UI experiences in games, such as inventory management, crafting, equipment systems, and puzzle mechanics. This example provides a robust, commented C# solution for Unity, demonstrating how to create draggable UI elements and drop targets (slots) that can accept, reject, or swap items.

This solution focuses on:
*   **Draggable UI Item (`DraggableUIItem.cs`):** The component attached to the UI element you want to drag. It handles the visual movement and temporary state during the drag.
*   **Drop Slot (`UIDropSlot.cs`):** The component attached to the UI element that acts as a target for dropped items. It manages item placement, swapping, and visual feedback for hover states.

---

### **Unity Setup Instructions:**

To make this example work in your Unity project:

1.  **Create a New Scene** or open an existing one.
2.  **Create a Canvas:** Right-click in the Hierarchy -> UI -> Canvas.
    *   Set its **Render Mode** to "Screen Space - Camera" and assign your `Main Camera` for consistent behavior.
    *   Ensure the Canvas has a `Graphic Raycaster` component (it's added by default).
3.  **Create an Event System:** Right-click in the Hierarchy -> UI -> Event System (if one isn't already present).
4.  **Create UI Slots:**
    *   Inside the Canvas, right-click -> UI -> Image. Name it "Slot (1)".
    *   Duplicate this Image several times (e.g., "Slot (2)", "Slot (3)") to create multiple drop targets.
    *   Position and size them as desired (e.g., in a grid).
    *   **Add the `UIDropSlot.cs` script to each "Slot" GameObject.**
    *   (Optional but Recommended) Assign the Image component of the slot to the `_slotImage` field in the Inspector if it's not automatically found. Adjust `_highlightColor` and `_defaultColor`.
5.  **Create Draggable UI Items:**
    *   Inside the Canvas (or initially within a Slot, or a shop panel), right-click -> UI -> Image. Name it "Item A".
    *   Give it a distinct source image (e.g., a simple icon).
    *   Duplicate this Image several times (e.g., "Item B", "Item C").
    *   **Add the `DraggableUIItem.cs` script to each "Item" GameObject.**
    *   (Optional) Set the `ItemName` property in the Inspector for debugging purposes.
    *   **Crucially, each `DraggableUIItem` GameObject must also have a `CanvasGroup` component.** The `DraggableUIItem.cs` script will automatically add one if it's missing during `Awake()`.
6.  **Initial Placement:** You can place some `DraggableUIItem`s directly into `UIDropSlot`s in the editor by parenting them. The `UIDropSlot` script will detect them in `Awake()` and correctly set its `ContainedItem` reference. (Alternatively, the `UIDropSlot` can start empty).
7.  **Run the Scene:** You should now be able to drag items between slots.

---

### **`DraggableUIItem.cs`**

```csharp
using UnityEngine;
using UnityEngine.EventSystems; // Required for event interfaces
using UnityEngine.UI; // Required for Image, CanvasGroup

/// <summary>
/// DraggableUIItem: Implements the drag behavior for a UI element.
/// This script makes any UI element (e.g., Image, Panel, Button) draggable
/// and provides visual feedback during the drag operation. It also handles
/// returning the item to its original position if it's not dropped on a valid target.
/// </summary>
[RequireComponent(typeof(RectTransform))] // Ensures it has a RectTransform
[RequireComponent(typeof(CanvasGroup))]   // Ensures it has a CanvasGroup for raycast blocking
public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // --- Public Properties (Managed by UIDropSlot or other external logic) ---
    /// <summary>
    /// The transform of the UI slot or container that currently logically owns this item.
    /// This is set by the UIDropSlot when the item is successfully placed.
    /// It's different from the _originalParentDuringDrag which is purely for snapping back.
    /// </summary>
    public Transform CurrentParentSlot { get; private set; }

    // --- Private Internal State ---
    private Transform _originalParentDuringDrag;        // The item's parent before drag began. Used for snapping back.
    private Vector3 _originalLocalPositionDuringDrag;   // The item's local position before drag began. Used for snapping back.
    private CanvasGroup _canvasGroup;                   // Used to disable raycasting during drag.
    private RectTransform _rectTransform;               // Cached RectTransform for position manipulation.
    private Transform _canvasTransform;                 // Cached Canvas transform to ensure item is on top during drag.

    // --- Example Item Data ---
    [Tooltip("A simple string identifier for the item. In a real project, this would be an ItemData ScriptableObject or similar.")]
    public string ItemName = "Generic Item";

    // --- MonoBehaviour Lifecycle Methods ---
    void Awake()
    {
        // Get or add CanvasGroup. Essential for allowing raycasts to hit objects underneath the dragged item.
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _rectTransform = GetComponent<RectTransform>();

        // Find the root Canvas transform. This is where the item will be temporarily parented during drag
        // to ensure it renders on top of all other UI elements.
        _canvasTransform = GetComponentInParent<Canvas>()?.transform;
        if (_canvasTransform == null)
        {
            Debug.LogError("DraggableUIItem must be a child of a Canvas!", this);
            enabled = false; // Disable if no Canvas is found
        }
    }

    // --- IBeginDragHandler Implementation ---
    /// <summary>
    /// Called once when a drag operation is initiated.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Store the item's original parent and local position. If the drag doesn't
        // result in a successful drop, the item will return to this position.
        _originalParentDuringDrag = transform.parent;
        _originalLocalPositionDuringDrag = transform.localPosition;

        // Temporarily change the item's parent to the Canvas. This ensures it's
        // rendered on top of all other UI elements while being dragged.
        transform.SetParent(_canvasTransform);
        transform.SetAsLastSibling(); // Ensure it's rendered on top of other dragged items

        // Disable raycast blocking. This allows pointer events to pass through this
        // item and hit UI elements (like UIDropSlots) underneath it.
        _canvasGroup.blocksRaycasts = false;

        // Optional: Reduce item's alpha for visual feedback during drag.
        _canvasGroup.alpha = 0.6f;

        // Debug.Log($"Started dragging: {ItemName}");
    }

    // --- IDragHandler Implementation ---
    /// <summary>
    /// Called every frame while the drag operation is active.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Update the item's position to follow the mouse cursor.
        // Using RectTransformUtility.ScreenPointToLocalPointInRectangle for accurate
        // positioning within different Canvas render modes.
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasTransform as RectTransform, // The rectangle to compare against (Canvas)
                eventData.position,                // Current screen position of the pointer
                eventData.pressEventCamera,        // Camera used to generate the event (can be null for Screen Space Overlay)
                out localPoint))                   // Output: local position within the Canvas RectTransform
        {
            _rectTransform.localPosition = localPoint;
        }
    }

    // --- IEndDragHandler Implementation ---
    /// <summary>
    /// Called once when the drag operation ends (e.g., mouse button is released).
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // Re-enable raycast blocking and reset alpha.
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // If the item's parent is still the Canvas (meaning no UIDropSlot successfully
        // re-parented it), then it was not dropped on a valid target. Revert its position.
        if (transform.parent == _canvasTransform)
        {
            // Debug.Log($"Dropped '{ItemName}' on invalid area. Returning to original position.");
            // Explicitly call PlacedInSlot to manage its logical state even when returning.
            PlacedInSlot(_originalParentDuringDrag); 
            transform.localPosition = _originalLocalPositionDuringDrag; // Ensure it returns to its exact local spot
        }
        // Else, a UIDropSlot successfully took ownership and called PlacedInSlot,
        // so its parent and CurrentParentSlot are already updated.
    }

    // --- Public Methods for External Control (e.g., by UIDropSlot) ---

    /// <summary>
    /// Called by a UIDropSlot when this item is successfully placed into it.
    /// Updates its logical parent and physical position.
    /// </summary>
    /// <param name="newSlotTransform">The transform of the new slot.</param>
    public void PlacedInSlot(Transform newSlotTransform)
    {
        CurrentParentSlot = newSlotTransform; // Update logical parent
        transform.SetParent(newSlotTransform);
        transform.localPosition = Vector3.zero; // Center it in the new slot
        // Debug.Log($"{ItemName} successfully placed in slot: {newSlotTransform.name}");
    }

    /// <summary>
    /// Called by a UIDropSlot when this item is removed from it (e.g., swapped out).
    /// Clears its logical parent reference.
    /// </summary>
    public void RemovedFromSlot()
    {
        CurrentParentSlot = null; // No longer logically associated with a specific slot
        // Debug.Log($"{ItemName} removed from its slot reference.");
    }
}
```

---

### **`UIDropSlot.cs`**

```csharp
using UnityEngine;
using UnityEngine.EventSystems; // Required for event interfaces
using UnityEngine.UI; // Required for Image

/// <summary>
/// UIDropSlot: Defines an area where DraggableUIItems can be dropped.
/// This script handles the logic for accepting dropped items, managing item swaps
/// between slots, and providing visual feedback (highlighting) when an item
/// is hovered over it.
/// </summary>
[RequireComponent(typeof(RectTransform))] // Ensures it has a RectTransform
[RequireComponent(typeof(Image))]         // Ensures it has an Image component for visual feedback
public class UIDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // --- Editor-Configurable Properties ---
    [Tooltip("Reference to the Image component of this slot. Used for visual hover feedback.")]
    [SerializeField] private Image _slotImage;
    [Tooltip("Color to show when a draggable item is hovering over this slot.")]
    [SerializeField] private Color _highlightColor = new Color(0.8f, 1f, 0.8f, 1f); // Light green for hover
    [Tooltip("Default color of the slot when no item is hovering.")]
    [SerializeField] private Color _defaultColor = new Color(1f, 1f, 1f, 0.5f); // White, semi-transparent by default

    // --- Public Properties (Read-Only) ---
    /// <summary>
    /// The DraggableUIItem currently occupying this slot. Null if the slot is empty.
    /// </summary>
    public DraggableUIItem ContainedItem { get; private set; }

    // --- MonoBehaviour Lifecycle Methods ---
    void Awake()
    {
        // Get or add Image component. It's required for visual feedback.
        if (_slotImage == null)
        {
            _slotImage = GetComponent<Image>();
        }
        if (_slotImage != null)
        {
            _defaultColor = _slotImage.color; // Initialize default color from editor value
        }
        else
        {
            Debug.LogWarning($"UIDropSlot '{gameObject.name}' is missing an Image component. Visual feedback will not work.", this);
        }

        // Check if an item is already a child in the editor and assign it.
        // This makes it easy to set up initial inventory states in the editor.
        if (transform.childCount > 0)
        {
            DraggableUIItem initialItem = transform.GetChild(0).GetComponent<DraggableUIItem>();
            if (initialItem != null)
            {
                ContainedItem = initialItem;
                initialItem.PlacedInSlot(transform); // Ensure item knows its parent
            }
        }
    }

    // --- IDropHandler Implementation ---
    /// <summary>
    /// Called when a draggable item is released over this slot.
    /// </summary>
    /// <param name="eventData">Pointer event data containing information about the dragged object.</param>
    public void OnDrop(PointerEventData eventData)
    {
        // Reset slot's visual highlight immediately after drop.
        ResetSlotVisual();

        // Attempt to get the DraggableUIItem component from the object that was dragged.
        DraggableUIItem droppedItem = eventData.pointerDrag?.GetComponent<DraggableUIItem>();

        if (droppedItem == null)
        {
            // Debug.Log($"Dropped object '{eventData.pointerDrag?.name}' is not a DraggableUIItem. Ignoring drop.");
            return; // Only process drops from DraggableUIItems.
        }

        // --- Core Drop Logic: Handle empty slot, occupied slot (swap), or self-drop ---

        // Case 1: The slot is currently empty.
        if (ContainedItem == null)
        {
            // Debug.Log($"'{droppedItem.ItemName}' dropped into empty slot: {gameObject.name}.");
            PlaceItemInThisSlot(droppedItem);
        }
        // Case 2: The slot is occupied by another item, and it's not the same item being dropped onto itself.
        else if (ContainedItem != droppedItem)
        {
            // Debug.Log($"'{droppedItem.ItemName}' dropped onto occupied slot: {gameObject.name} (contains '{ContainedItem.ItemName}'). Attempting swap.");
            SwapItems(droppedItem, ContainedItem);
        }
        // Case 3: The item was dropped back onto the same slot it originated from (self-drop).
        else
        {
            // Debug.Log($"'{droppedItem.ItemName}' dropped onto its own slot: {gameObject.name}. No change, just re-parenting if needed.");
            // Ensure it's correctly parented and positioned, even if it's already here.
            droppedItem.PlacedInSlot(transform);
            // We don't need to call RemovedFromSlot on this item because it's still logically in this slot.
        }
    }

    // --- IPointerEnterHandler Implementation ---
    /// <summary>
    /// Called when the mouse pointer (while dragging) enters this slot's area.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only highlight if a draggable item is actually being dragged.
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<DraggableUIItem>() != null)
        {
            if (_slotImage != null)
            {
                _slotImage.color = _highlightColor;
            }
        }
    }

    // --- IPointerExitHandler Implementation ---
    /// <summary>
    /// Called when the mouse pointer (while dragging) exits this slot's area.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset highlight color.
        ResetSlotVisual();
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Places a DraggableUIItem into this slot, updating its parent and logical state.
    /// Handles clearing the item's previous slot reference if it came from one.
    /// </summary>
    /// <param name="itemToPlace">The DraggableUIItem to place.</param>
    private void PlaceItemInThisSlot(DraggableUIItem itemToPlace)
    {
        // If the item was previously in another slot, clear its reference there.
        if (itemToPlace.CurrentParentSlot != null)
        {
            UIDropSlot oldSlot = itemToPlace.CurrentParentSlot.GetComponent<UIDropSlot>();
            if (oldSlot != null && oldSlot.ContainedItem == itemToPlace)
            {
                oldSlot.ContainedItem = null;
                itemToPlace.RemovedFromSlot(); // Notify the item it's no longer in its old slot.
            }
        }

        // Update this slot's reference and the item's physical parent/position.
        ContainedItem = itemToPlace;
        itemToPlace.PlacedInSlot(transform); // Notifies item of its new logical parent and sets physical parent/position.
    }

    /// <summary>
    /// Swaps two DraggableUIItems: the incoming one with the one already in this slot.
    /// Handles updating their parents and logical slot references.
    /// </summary>
    /// <param name="incomingItem">The item being dropped onto this slot.</param>
    /// <param name="itemAlreadyInSlot">The item currently occupying this slot.</param>
    private void SwapItems(DraggableUIItem incomingItem, DraggableUIItem itemAlreadyInSlot)
    {
        // Get the slot where the incoming item originated.
        UIDropSlot originalSlotOfIncomingItem = null;
        if (incomingItem.CurrentParentSlot != null)
        {
            originalSlotOfIncomingItem = incomingItem.CurrentParentSlot.GetComponent<UIDropSlot>();
        }

        // --- Step 1: Place the incoming item into this current slot ---
        // This call will update this slot's ContainedItem and the incomingItem's
        // CurrentParentSlot and physical parent. It also clears the incomingItem's
        // reference from its original slot if it came from one.
        PlaceItemInThisSlot(incomingItem);

        // --- Step 2: Handle the item that was originally in this slot ('itemAlreadyInSlot') ---
        if (originalSlotOfIncomingItem != null && originalSlotOfIncomingItem != this)
        {
            // If the incoming item came from another *valid* UIDropSlot, move 'itemAlreadyInSlot' into that original slot.
            // Temporarily clear the old slot's ContainedItem if it refers to the incoming item, to prevent conflicts.
            if (originalSlotOfIncomingItem.ContainedItem == incomingItem)
            {
                originalSlotOfIncomingItem.ContainedItem = null;
            }
            originalSlotOfIncomingItem.PlaceItemInThisSlot(itemAlreadyInSlot);
        }
        else
        {
            // The incoming item did not originate from a UIDropSlot, or it was dragged onto itself (handled in OnDrop),
            // or the original slot was somehow this one.
            // 'itemAlreadyInSlot' needs to go somewhere. We'll send it back to its own last known logical slot.
            // If it had no logical slot, it will go to the current slot's parent for visual consistency,
            // but its CurrentParentSlot will be nullified.

            itemAlreadyInSlot.RemovedFromSlot(); // Mark it as no longer in THIS slot.

            if (itemAlreadyInSlot.CurrentParentSlot != null)
            {
                // Send it back to its own *previous* logical slot.
                UIDropSlot ownPreviousLogicalSlot = itemAlreadyInSlot.CurrentParentSlot.GetComponent<UIDropSlot>();
                if (ownPreviousLogicalSlot != null)
                {
                    // If its own previous slot is empty or contained itself (which it just left), place it there.
                    if (ownPreviousLogicalSlot.ContainedItem == null || ownPreviousLogicalSlot.ContainedItem == itemAlreadyInSlot)
                    {
                        ownPreviousLogicalSlot.PlaceItemInThisSlot(itemAlreadyInSlot);
                    }
                    else
                    {
                        // Its own previous slot is now occupied by something else.
                        // Place it back to its own previous *physical* parent, but without a logical slot reference.
                        // This handles cases where two concurrent drags might occupy the same target slot.
                        itemAlreadyInSlot.transform.SetParent(itemAlreadyInSlot.CurrentParentSlot);
                        itemAlreadyInSlot.transform.localPosition = Vector3.zero;
                        Debug.LogWarning($"Could not place '{itemAlreadyInSlot.ItemName}' back into its original slot '{itemAlreadyInSlot.CurrentParentSlot.name}' as it's now occupied.", this);
                    }
                }
                else
                {
                    // Its previous logical parent was not a UIDropSlot. Place it back to that transform.
                    itemAlreadyInSlot.transform.SetParent(itemAlreadyInSlot.CurrentParentSlot);
                    itemAlreadyInSlot.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                // The item had no CurrentParentSlot (e.g., it was a fresh item from a shop or not associated with any slot).
                // Just place it somewhere visible, like the parent of this slot.
                itemAlreadyInSlot.transform.SetParent(this.transform.parent);
                itemAlreadyInSlot.transform.localPosition = Vector3.zero;
                // Debug.Log($"'{itemAlreadyInSlot.ItemName}' had no previous slot and was replaced. Placed it under parent of this slot.");
            }
        }
    }

    /// <summary>
    /// Resets the slot's visual appearance to its default state.
    /// </summary>
    private void ResetSlotVisual()
    {
        if (_slotImage != null)
        {
            _slotImage.color = _defaultColor;
        }
    }
}
```