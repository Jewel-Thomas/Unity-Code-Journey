// Unity Design Pattern Example: UnitSelectionSystem
// This script demonstrates the UnitSelectionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a complete `UnitSelectionSystem` design pattern for Unity, allowing players to select units via single clicks (with multi-select options) and drag-box selection, similar to RTS games.

It follows Unity best practices, includes clear explanations, and is designed to be easily integrated into any Unity project.

---

### **1. `SelectableUnit.cs` (Component for Units)**

This script marks a GameObject as selectable and handles its visual feedback when selected or deselected.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// Represents a single selectable unit in the game.
/// This component should be attached to any GameObject that the player can select.
/// </summary>
[RequireComponent(typeof(Collider))] // Units must have a Collider for raycasting
[RequireComponent(typeof(Renderer))] // Units must have a Renderer to change visual state
public class SelectableUnit : MonoBehaviour
{
    [Tooltip("The color of the unit when it is selected.")]
    [SerializeField] private Color _selectedColor = Color.green;

    private Renderer _renderer;
    private Material _originalMaterial; // Store original material to revert on deselect
    private bool _isSelected;

    /// <summary>
    /// Gets a value indicating whether this unit is currently selected.
    /// </summary>
    public bool IsSelected => _isSelected;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        // Store a copy of the material to avoid modifying the original asset directly
        // This is important if multiple units share the same material asset.
        _originalMaterial = _renderer.material; 
    }

    void OnEnable()
    {
        // Register this unit with the selection manager when it becomes active.
        UnitSelectionManager.Instance?.RegisterUnit(this);
        SetSelected(false); // Ensure unit starts in deselected state
    }

    void OnDisable()
    {
        // Deregister this unit from the selection manager when it becomes inactive.
        UnitSelectionManager.Instance?.DeregisterUnit(this);
    }

    void OnDestroy()
    {
        // Clean up the instantiated material to prevent memory leaks
        if (_renderer != null && _renderer.material != _originalMaterial)
        {
            Destroy(_renderer.material);
        }
    }

    /// <summary>
    /// Sets the selection state of the unit.
    /// </summary>
    /// <param name="selected">True to select the unit, false to deselect.</param>
    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return; // No change needed

        _isSelected = selected;

        if (_isSelected)
        {
            // Apply a new material instance or change properties for visual feedback.
            // Creating a new material instance is safer if the material is shared.
            Material selectedMaterial = new Material(_originalMaterial);
            selectedMaterial.color = _selectedColor;
            _renderer.material = selectedMaterial;
            Debug.Log($"Unit '{name}' selected!");
        }
        else
        {
            // Revert to the original material/color.
            if (_renderer.material != _originalMaterial)
            {
                Destroy(_renderer.material); // Destroy the temporary material instance
                _renderer.material = _originalMaterial; // Revert to the original
            }
            Debug.Log($"Unit '{name}' deselected!");
        }
    }
}
```

---

### **2. `UnitSelectionManager.cs` (The Core System)**

This script manages all selection logic, including input handling (clicks, drag-box), raycasting, and maintaining the list of currently selected units.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel; // For ReadOnlyCollection

/// <summary>
/// The core Unit Selection System manager.
/// Handles player input for unit selection (single click, multi-click, drag box).
/// </summary>
public class UnitSelectionManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global access point to the UnitSelectionManager instance.
    public static UnitSelectionManager Instance { get; private set; }

    [Header("Selection Settings")]
    [Tooltip("The LayerMask that selectable units belong to. Important for efficient raycasting.")]
    [SerializeField] private LayerMask _selectableLayer;
    [Tooltip("The color of the selection box drawn on screen.")]
    [SerializeField] private Color _selectionBoxColor = Color.green;

    // --- Internal State ---
    private List<SelectableUnit> _selectedUnits = new List<SelectableUnit>();
    private List<SelectableUnit> _allSelectableUnits = new List<SelectableUnit>(); // All active selectable units in the scene.

    private bool _isDragging = false;
    private Vector2 _dragStartPosition;

    private Texture2D _selectionBoxTexture; // Used for drawing the selection box via OnGUI

    // --- Public Access ---
    /// <summary>
    /// Gets a read-only list of all currently selected units.
    /// </summary>
    public ReadOnlyCollection<SelectableUnit> SelectedUnits => _selectedUnits.AsReadOnly();

    // --- Unity Lifecycle Methods ---
    void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
        }

        // Initialize the 1x1 texture for drawing the selection box.
        // This is a common technique for drawing solid-color rectangles with OnGUI.
        _selectionBoxTexture = new Texture2D(1, 1);
        _selectionBoxTexture.SetPixel(0, 0, Color.white);
        _selectionBoxTexture.Apply();
    }

    void OnDestroy()
    {
        // Clean up the texture when the manager is destroyed.
        if (Instance == this)
        {
            Instance = null;
            if (_selectionBoxTexture != null)
            {
                Destroy(_selectionBoxTexture);
            }
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// This is where the selection box is drawn while dragging.
    /// </summary>
    void OnGUI()
    {
        if (_isDragging)
        {
            // Get the rectangle representing the current drag area in GUI coordinates.
            Rect selectionRect = GetSelectionRectangle(_dragStartPosition, Input.mousePosition);

            // Set the color for the GUI drawing operation.
            GUI.color = _selectionBoxColor;

            // Draw the selection box using the 1x1 white texture, tinted by GUI.color.
            GUI.DrawTexture(selectionRect, _selectionBoxTexture);
        }
    }

    // --- Public Unit Registration/Deregistration ---
    /// <summary>
    /// Registers a selectable unit with the manager. Called by SelectableUnit.OnEnable.
    /// </summary>
    /// <param name="unit">The unit to register.</param>
    public void RegisterUnit(SelectableUnit unit)
    {
        if (unit != null && !_allSelectableUnits.Contains(unit))
        {
            _allSelectableUnits.Add(unit);
            Debug.Log($"Registered unit: {unit.name}. Total units: {_allSelectableUnits.Count}");
        }
    }

    /// <summary>
    /// Deregisters a selectable unit from the manager. Called by SelectableUnit.OnDisable.
    /// </summary>
    /// <param name="unit">The unit to deregister.</param>
    public void DeregisterUnit(SelectableUnit unit)
    {
        if (unit != null)
        {
            _allSelectableUnits.Remove(unit);
            // If the unit was selected, remove it from the selected list and deselect it.
            if (_selectedUnits.Contains(unit))
            {
                _selectedUnits.Remove(unit);
                unit.SetSelected(false); // Ensure visual state is updated
            }
            Debug.Log($"Deregistered unit: {unit.name}. Total units: {_allSelectableUnits.Count}");
        }
    }

    // --- Core Selection Logic ---
    /// <summary>
    /// Handles all mouse input for selection.
    /// </summary>
    private void HandleInput()
    {
        // --- Left Mouse Button Down (Start Drag or Single Click) ---
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _dragStartPosition = Input.mousePosition; // Store the starting position for drag
        }

        // --- Left Mouse Button Up (End Drag or Complete Single Click) ---
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false; // Reset dragging flag

            // Calculate the distance the mouse moved from the start of the click.
            float dragDistance = Vector2.Distance(_dragStartPosition, Input.mousePosition);

            // If the mouse moved significantly, it was a drag selection.
            if (dragDistance > 5f) // Threshold to distinguish click from drag
            {
                // Perform box selection.
                Rect selectionRect = GetSelectionRectangle(_dragStartPosition, Input.mousePosition);
                PerformBoxSelection(selectionRect);
            }
            else // Otherwise, it was a single click selection.
            {
                // Check if Shift or Ctrl is held for multi-selection.
                bool appendToSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl);
                PerformRaycastSelection(Input.mousePosition, appendToSelection);
            }
        }

        // --- Right Mouse Button Down (Example for commanding selected units) ---
        // This is outside the 'selection' pattern but shows how to use selected units.
        if (Input.GetMouseButtonDown(1))
        {
            if (_selectedUnits.Count > 0)
            {
                Debug.Log($"Commanding {_selectedUnits.Count} selected units to new position/target!");
                // Example: Raycast to find a target position or enemy to command units to.
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // For each selected unit, tell it to move to hit.point
                    foreach (SelectableUnit unit in _selectedUnits)
                    {
                        // Example: unit.GetComponent<UnitMovement>().MoveTo(hit.point);
                        Debug.Log($"{unit.name} received command to move to {hit.point}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Performs a raycast from the camera through the screen point to select a single unit.
    /// </summary>
    /// <param name="screenPoint">The screen position of the mouse click.</param>
    /// <param name="appendToSelection">If true, adds to current selection; otherwise, clears and selects.</param>
    private void PerformRaycastSelection(Vector2 screenPoint, bool appendToSelection)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _selectableLayer))
        {
            // If the ray hits a selectable unit
            SelectableUnit unit = hit.collider.GetComponent<SelectableUnit>();
            if (unit != null)
            {
                if (appendToSelection)
                {
                    // If append is true, toggle selection state for the clicked unit.
                    if (unit.IsSelected)
                    {
                        RemoveUnitFromSelection(unit);
                    }
                    else
                    {
                        AddUnitToSelection(unit);
                    }
                }
                else
                {
                    // If not appending, clear existing selection and select only this unit.
                    ClearSelection();
                    AddUnitToSelection(unit);
                }
            }
            else // Hit something on selectableLayer but it's not a SelectableUnit (e.g., ground)
            {
                if (!appendToSelection)
                {
                    ClearSelection();
                }
            }
        }
        else // No unit hit
        {
            if (!appendToSelection)
            {
                ClearSelection(); // Clicking on empty space deselects all.
            }
        }
    }

    /// <summary>
    /// Performs a box selection by checking which units are within the screen rectangle.
    /// </summary>
    /// <param name="selectionRect">The screen rectangle defining the selection area.</param>
    private void PerformBoxSelection(Rect selectionRect)
    {
        ClearSelection(); // Box selection always clears previous selection.

        // Iterate through all known selectable units.
        foreach (SelectableUnit unit in _allSelectableUnits)
        {
            if (unit == null) continue; // Skip if unit was destroyed

            // Convert the unit's world position to screen coordinates.
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);

            // Check if the unit's screen position is within the selection rectangle.
            // Note: Rect.Contains expects screen Y to increase upwards, while OnGUI/Input.mousePosition
            // Y increases downwards (from top-left). GetSelectionRectangle handles this conversion.
            if (selectionRect.Contains(screenPos, true))
            {
                AddUnitToSelection(unit);
            }
        }
    }

    /// <summary>
    /// Adds a unit to the currently selected units list and updates its visual state.
    /// </summary>
    /// <param name="unit">The unit to add to selection.</param>
    private void AddUnitToSelection(SelectableUnit unit)
    {
        if (unit != null && !_selectedUnits.Contains(unit))
        {
            _selectedUnits.Add(unit);
            unit.SetSelected(true);
        }
    }

    /// <summary>
    /// Removes a unit from the currently selected units list and updates its visual state.
    /// </summary>
    /// <param name="unit">The unit to remove from selection.</param>
    private void RemoveUnitFromSelection(SelectableUnit unit)
    {
        if (unit != null && _selectedUnits.Contains(unit))
        {
            _selectedUnits.Remove(unit);
            unit.SetSelected(false);
        }
    }

    /// <summary>
    /// Clears all currently selected units.
    /// </summary>
    public void ClearSelection()
    {
        // Iterate through a copy of the list to avoid modifying it while iterating.
        foreach (SelectableUnit unit in new List<SelectableUnit>(_selectedUnits))
        {
            unit?.SetSelected(false); // Deselect each unit
        }
        _selectedUnits.Clear(); // Clear the list
    }

    /// <summary>
    /// Calculates a proper Rect object from two mouse positions for GUI drawing.
    /// Handles cases where drag can go in any direction (top-left to bottom-right, etc.).
    /// IMPORTANT: Converts Input.mousePosition (bottom-left origin) to GUI coordinates (top-left origin).
    /// </summary>
    /// <param name="startPos">The mouse position when dragging started.</param>
    /// <param name="endPos">The current mouse position.</param>
    /// <returns>A Rect representing the selection area in GUI coordinates.</returns>
    private Rect GetSelectionRectangle(Vector2 startPos, Vector2 endPos)
    {
        // Convert y-coordinates from Input.mousePosition (bottom-left origin)
        // to GUI coordinates (top-left origin)
        startPos.y = Screen.height - startPos.y;
        endPos.y = Screen.height - endPos.y;

        // Calculate min/max for x and y to handle dragging in any direction
        float minX = Mathf.Min(startPos.x, endPos.x);
        float maxX = Mathf.Max(startPos.x, endPos.x);
        float minY = Mathf.Min(startPos.y, endPos.y);
        float maxY = Mathf.Max(startPos.y, endPos.y);

        // Return the Rect
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}
```

---

### **Example Usage in Unity Project**

To make this system work in your Unity project, follow these steps:

**1. Create the `UnitSelectionManager` GameObject:**
   *   Create an empty GameObject in your scene (e.g., by right-clicking in the Hierarchy and selecting `Create Empty`).
   *   Rename it to `UnitSelectionManager`.
   *   Attach the `UnitSelectionManager.cs` script to this GameObject.

**2. Configure the `UnitSelectionManager`:**
   *   In the Inspector for `UnitSelectionManager`, you'll see fields:
      *   **Selectable Layer:** This is crucial. Create a new Layer (e.g., "Units") in `Tags & Layers` dropdown at the top of the Unity editor. Assign this layer to all your selectable units. Then, select this layer in the `Selectable Layer` dropdown on the `UnitSelectionManager`.
      *   **Selection Box Color:** Choose your desired color for the drag selection box.

**3. Prepare your Selectable Units:**
   *   Create some 3D objects in your scene (e.g., `GameObject -> 3D Object -> Cube` or `Sphere`).
   *   Rename them (e.g., `Unit_01`, `Unit_02`).
   *   **Add Components:**
      *   Ensure each unit has a **Collider** (e.g., `Box Collider` for a cube, `Sphere Collider` for a sphere). This is essential for raycasting.
      *   Ensure each unit has a **Renderer** (e.g., `Mesh Renderer`). This is needed for the script to change its color.
      *   Attach the `SelectableUnit.cs` script to each unit.
   *   **Configure `SelectableUnit`:**
      *   In the Inspector for `SelectableUnit`, set the `Selected Color` if you want something different from the default green.
   *   **Assign Layer:** Select all your unit GameObjects. In the Inspector, next to their name, click the `Layer` dropdown and assign them to the "Units" layer (or whatever layer you set in `UnitSelectionManager`).

**4. Ensure Main Camera is Tagged:**
   *   Make sure your `Main Camera` GameObject in the scene has the `MainCamera` tag assigned to it (Unity usually does this by default for the primary camera). This is needed for `Camera.main` to work.

**5. Play and Test:**
   *   Run your scene.
   *   **Single Select:** Left-click on a unit to select it. Click on another unit to deselect the first and select the new one.
   *   **Deselect All:** Left-click on empty ground to deselect all units.
   *   **Multi-Select (Add/Remove):** Hold `Shift` or `Ctrl` and left-click on units to add them to or remove them from the current selection.
   *   **Box Select:** Left-click and drag the mouse to draw a selection box. All units whose center point falls within the box will be selected. This clears any previous selection.
   *   **Command Selected Units (Example):** With units selected, right-click on the ground. You'll see debug logs indicating that the selected units received a command to move to that point.

---

This setup provides a robust and educational foundation for understanding and implementing the `UnitSelectionSystem` pattern in your Unity games. You can extend it further with more sophisticated visual feedback (outlines, UI elements), advanced selection logic (e.g., filtering unit types), and integration with a command system for selected units.