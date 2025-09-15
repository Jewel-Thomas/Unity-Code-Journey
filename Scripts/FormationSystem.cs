// Unity Design Pattern Example: FormationSystem
// This script demonstrates the FormationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The FormationSystem design pattern is used to arrange a group of units (e.g., characters, objects) into a specific geometric configuration relative to a central point or a leader. This pattern is highly useful in strategy games, RTS, RPGs, or any scenario where you need to dynamically control the spatial arrangement of multiple entities.

This example provides a complete, practical implementation in C# for Unity. It includes:
1.  **`IFormation` Interface**: Defines the contract for any formation type.
2.  **Concrete Formation Classes**: Implement `IFormation` for various shapes like Line, Circle, Grid, and V-Shape. These classes encapsulate the logic for calculating unit positions within their respective formations.
3.  **`FormationType` Enum**: Allows easy selection of the desired formation in the Unity editor.
4.  **`FormationController` MonoBehaviour**: The main component that manages a collection of units, selects a formation type, and applies the calculated positions. It provides editor-friendly parameters and Gizmos for visualization.

---

## FormationSystem.cs

To use this script:
1.  Create an empty GameObject in your Unity scene (e.g., "FormationManager").
2.  Attach this `FormationController.cs` script to it.
3.  Assign a `Unit Prefab` (a simple Cube or Sphere will do) to the `FormationController`.
4.  Set the `Number Of Units` you want to spawn.
5.  Adjust `Formation Type` and its related parameters in the Inspector to see the formation change in real-time.
6.  The `OnDrawGizmos` method will draw the planned unit positions in the Scene view even without units spawned, helping with setup.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // Required for [Serializable]

// =====================================================================================
// FORMATION SYSTEM DESIGN PATTERN
// =====================================================================================
// Intent:
// To define a system for arranging multiple units (e.g., characters, enemies, objects)
// into various geometric configurations relative to a central point or a leader.
// This pattern encapsulates the logic for different formation types, making it easy
// to switch formations and apply them to a group of units.
//
// Components:
// 1.  IFormation: An interface or abstract base class that defines the contract
//     for all formation types (e.g., a method to get a unit's local position).
// 2.  Concrete Formation Classes: Implementations of IFormation for specific
//     formation shapes (e.g., LineFormation, CircleFormation, GridFormation, VFormation).
//     Each class encapsulates the specific logic for calculating positions.
// 3.  FormationType Enum: An enumeration to easily select between different
//     available formation types in the inspector.
// 4.  FormationController: A MonoBehaviour that holds a collection of units,
//     selects the current formation, and applies the calculated positions
//     to the units. It acts as the client of the IFormation interface.
//
// Advantages:
// -   **Extensibility:** Easily add new formation types without modifying
//     existing code (Open/Closed Principle).
// -   **Flexibility:** Switch formations at runtime with minimal effort.
// -   **Separation of Concerns:** Formation logic is separated from unit behavior
//     and the controller logic.
// -   **Maintainability:** Each formation type's logic is self-contained.
// -   **Readability:** The code is clearer because the intent of different
//     formation calculations is explicitly named.
//
// Usage:
// Attach the FormationController to an empty GameObject.
// Assign a Unit Prefab.
// Configure the number of units and choose a FormationType.
// The FormationController will then calculate and apply the positions to the units.
// =====================================================================================

/// <summary>
/// Defines the contract for any formation type.
/// Concrete formation classes will implement this interface to provide their
/// specific logic for calculating a unit's local position within the formation.
/// </summary>
public interface IFormation
{
    /// <summary>
    /// Calculates the local position for a specific unit within the formation.
    /// The returned position is relative to the formation's origin (0,0,0).
    /// </summary>
    /// <param name="unitIndex">The 0-based index of the unit.</param>
    /// <param name="totalUnits">The total number of units in the formation.</param>
    /// <returns>A Vector3 representing the unit's local offset from the formation's origin.</returns>
    Vector3 GetUnitLocalPosition(int unitIndex, int totalUnits);
}

/// <summary>
/// Concrete implementation of IFormation for a straight line formation.
/// Units are spaced evenly along the X-axis.
/// </summary>
[Serializable] // Though not directly serialized as a field, good practice if it were.
public class LineFormation : IFormation
{
    private float _spacing;

    public LineFormation(float spacing)
    {
        _spacing = spacing;
    }

    public Vector3 GetUnitLocalPosition(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 0) return Vector3.zero;

        // Calculate the offset from the center of the line.
        // E.g., for 5 units, indices 0,1,2,3,4. Center is at index 2.
        // (0 - 2) * spacing = -2 * spacing
        // (1 - 2) * spacing = -1 * spacing
        // (2 - 2) * spacing = 0 * spacing
        // (3 - 2) * spacing = 1 * spacing
        // (4 - 2) * spacing = 2 * spacing
        float offset = (unitIndex - (totalUnits - 1) / 2.0f) * _spacing;
        return new Vector3(offset, 0, 0); // Arranged along the X-axis
    }
}

/// <summary>
/// Concrete implementation of IFormation for a circular formation.
/// Units are spaced evenly around the circumference of a circle.
/// </summary>
[Serializable]
public class CircleFormation : IFormation
{
    private float _radius;
    private float _startAngle; // In degrees, for rotation of the circle

    public CircleFormation(float radius, float startAngle)
    {
        _radius = radius;
        _startAngle = startAngle;
    }

    public Vector3 GetUnitLocalPosition(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 0) return Vector3.zero;
        if (totalUnits == 1) return Vector3.zero; // Single unit at the center for a circle

        // Calculate the angle for the current unit
        float angleStep = 360f / totalUnits;
        float currentAngle = _startAngle + unitIndex * angleStep;

        // Convert angle to radians for trigonometric functions
        float angleRad = currentAngle * Mathf.Deg2Rad;

        // Calculate X and Z positions on the circle
        float x = Mathf.Cos(angleRad) * _radius;
        float z = Mathf.Sin(angleRad) * _radius;

        return new Vector3(x, 0, z);
    }
}

/// <summary>
/// Concrete implementation of IFormation for a grid formation.
/// Units are arranged in rows and columns.
/// </summary>
[Serializable]
public class GridFormation : IFormation
{
    private float _spacingX;
    private float _spacingY; // Using Y for Z-depth in Unity's XZ plane
    private int _unitsPerRow;

    public GridFormation(float spacingX, float spacingY, int unitsPerRow)
    {
        _spacingX = spacingX;
        _spacingY = spacingY;
        _unitsPerRow = Mathf.Max(1, unitsPerRow); // Ensure at least 1 unit per row
    }

    public Vector3 GetUnitLocalPosition(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 0) return Vector3.zero;

        // Calculate row and column for the current unit
        int row = unitIndex / _unitsPerRow;
        int col = unitIndex % _unitsPerRow;

        // Determine actual units in the last row for centering
        int actualUnitsInCurrentRow = _unitsPerRow;
        if (row == totalUnits / _unitsPerRow && totalUnits % _unitsPerRow != 0)
        {
            actualUnitsInCurrentRow = totalUnits % _unitsPerRow;
        }

        // Calculate total grid dimensions for centering
        int totalRows = Mathf.CeilToInt((float)totalUnits / _unitsPerRow);

        // Calculate X and Z offsets, centering the grid around (0,0)
        float offsetX = (col - (actualUnitsInCurrentRow - 1) / 2.0f) * _spacingX;
        float offsetZ = (row - (totalRows - 1) / 2.0f) * _spacingY;

        return new Vector3(offsetX, 0, offsetZ);
    }
}

/// <summary>
/// Concrete implementation of IFormation for a V-shaped formation.
/// Units spread outwards from a central point, forming a 'V'.
/// </summary>
[Serializable]
public class VFormation : IFormation
{
    private float _spacing; // Distance between units along the V's arms
    private float _spreadAngle; // Total angle of the V (e.g., 60 degrees)

    public VFormation(float spacing, float spreadAngle)
    {
        _spacing = spacing;
        _spreadAngle = spreadAngle;
    }

    public Vector3 GetUnitLocalPosition(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 0) return Vector3.zero;

        // Handle single unit case (at the tip)
        if (totalUnits == 1) return Vector3.zero;

        // Determine the number of 'levels' or 'rows' in the V, starting from the tip
        // If totalUnits = 1, levels = 0
        // If totalUnits = 2 (1 tip, 1 arm unit), levels = 1
        // If totalUnits = 3 (1 tip, 2 arm units), levels = 1
        // If totalUnits = 4 (1 tip, 3 arm units, last arm gets 2 units), levels = 2
        // Generally, each level after the tip has 2 units.
        int numPairedUnits = totalUnits - 1; // Units excluding the tip (if it exists)
        int numLevels = Mathf.CeilToInt(numPairedUnits / 2f); // Number of pairs of units

        // Calculate the maximum Z-depth for centering the V
        float maxZDepth = numLevels * _spacing;
        float centerZOffset = maxZDepth / 2f; // Shift to center the V's Z-axis at 0

        // Handle the tip unit (index 0) separately
        if (unitIndex == 0)
        {
            return new Vector3(0, 0, -centerZOffset);
        }
        else
        {
            // For other units, calculate their position on the arms
            // armLevel: which 'row' or 'level' of units it is on (1 for first pair, 2 for second, etc.)
            int armLevel = (unitIndex + 1) / 2; // e.g., unit 1,2 -> armLevel 1; unit 3,4 -> armLevel 2
            bool isLeftArm = unitIndex % 2 != 0; // Odd indices go to the left arm, Even to the right arm

            // Calculate distance along the arm (Z-depth relative to tip)
            float armOffsetZ = armLevel * _spacing;

            // Calculate horizontal spread based on Z-depth and half the V-spread angle
            float halfSpreadRad = _spreadAngle * 0.5f * Mathf.Deg2Rad;
            float armOffsetX = armOffsetZ * Mathf.Tan(halfSpreadRad);

            // Apply horizontal offset based on which arm it is
            Vector3 localPos = new Vector3(isLeftArm ? -armOffsetX : armOffsetX, 0, armOffsetZ);

            // Apply the centering offset
            return localPos - new Vector3(0, 0, centerZOffset);
        }
    }
}


/// <summary>
/// Enum to easily select different formation types in the Unity Inspector.
/// </summary>
public enum FormationType
{
    Line,
    Circle,
    Grid,
    VShape
}

/// <summary>
/// The main MonoBehaviour that orchestrates the Formation System.
/// It holds a collection of units, selects a formation type, and applies
/// the calculated positions to the units.
/// </summary>
public class FormationController : MonoBehaviour
{
    [Header("Unit Configuration")]
    [Tooltip("Prefab to instantiate for each unit in the formation.")]
    public GameObject unitPrefab;
    [Tooltip("The total number of units in the formation.")]
    [Range(1, 100)] public int numberOfUnits = 10;

    [Header("Formation Type")]
    [Tooltip("Select the desired formation shape.")]
    public FormationType formationType = FormationType.Line;

    [Header("General Formation Parameters")]
    [Tooltip("Spacing between units for Line, Grid, and V-Shape formations.")]
    public float spacing = 2f;

    [Header("Circle Formation Parameters")]
    [Tooltip("Radius of the circle for Circle formation.")]
    public float circleRadius = 5f;
    [Tooltip("Starting angle (in degrees) for the first unit in Circle formation.")]
    public float circleStartAngle = 0f;

    [Header("Grid Formation Parameters")]
    [Tooltip("Horizontal spacing between units in a grid.")]
    public float gridSpacingX = 2f;
    [Tooltip("Vertical (Z-depth) spacing between rows in a grid.")]
    public float gridSpacingY = 2f;
    [Tooltip("Number of units per row in a grid.")]
    [Range(1, 20)] public int gridUnitsPerRow = 5;

    [Header("V-Shape Formation Parameters")]
    [Tooltip("The total angle of the 'V' shape (in degrees).")]
    [Range(10, 170)] public float vFormationSpreadAngle = 60f;

    // List to hold the actual unit transforms
    private List<Transform> _units = new List<Transform>();
    private IFormation _currentFormation; // The currently active formation strategy

    void Awake()
    {
        // Instantiate units and apply formation on game start
        PopulateUnits();
        ApplyFormation();
    }

    void OnValidate()
    {
        // OnValidate is called in the editor when script is loaded or a value is changed.
        // This allows real-time updates of the formation in the scene view.
        if (Application.isPlaying)
        {
            // Only update formation when playing if units already exist
            if (_units != null && _units.Count > 0)
            {
                ApplyFormation();
            }
        }
        else
        {
            // In editor mode, always try to apply formation for Gizmos or potential editor scripts.
            // If units aren't populated yet, Gizmos will still use the logic to draw.
            ApplyFormation();
        }
    }

    /// <summary>
    /// Instantiates the specified number of units using the unitPrefab
    /// and adds them to the _units list. Destroys existing units first.
    /// </summary>
    private void PopulateUnits()
    {
        // Clear existing units
        foreach (Transform unit in _units)
        {
            if (unit != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(unit.gameObject);
                }
                else
                {
                    // If in editor, use DestroyImmediate for immediate cleanup.
                    // This is generally used for editor tools, be cautious in runtime code.
                    DestroyImmediate(unit.gameObject);
                }
            }
        }
        _units.Clear();

        if (unitPrefab == null)
        {
            Debug.LogWarning("Unit Prefab is not assigned on FormationController. Cannot populate units.");
            return;
        }

        // Instantiate new units
        for (int i = 0; i < numberOfUnits; i++)
        {
            GameObject unitGO = Instantiate(unitPrefab, transform);
            unitGO.name = $"Unit_{i}";
            _units.Add(unitGO.transform);
        }
    }

    /// <summary>
    /// Creates the appropriate IFormation instance based on the selected `formationType`
    /// and then updates the positions of all units in the formation.
    /// </summary>
    public void ApplyFormation()
    {
        // Ensure units are populated, especially important if called from OnValidate
        // and units haven't been created yet (e.g., initial script load in editor).
        if (_units.Count != numberOfUnits)
        {
            PopulateUnits();
        }

        if (_units.Count == 0) return;

        // Choose the concrete formation strategy based on the enum
        switch (formationType)
        {
            case FormationType.Line:
                _currentFormation = new LineFormation(spacing);
                break;
            case FormationType.Circle:
                _currentFormation = new CircleFormation(circleRadius, circleStartAngle);
                break;
            case FormationType.Grid:
                _currentFormation = new GridFormation(gridSpacingX, gridSpacingY, gridUnitsPerRow);
                break;
            case FormationType.VShape:
                _currentFormation = new VFormation(spacing, vFormationSpreadAngle);
                break;
            default:
                _currentFormation = new LineFormation(spacing); // Default to Line
                Debug.LogWarning("Unknown formation type selected. Defaulting to Line Formation.");
                break;
        }

        // Apply the chosen formation to all units
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] != null)
            {
                // Get the local position from the current formation strategy
                Vector3 localPos = _currentFormation.GetUnitLocalPosition(i, _units.Count);
                _units[i].localPosition = localPos;
            }
        }
    }

    /// <summary>
    /// Draws Gizmos in the Scene view to visualize the formation.
    /// This is extremely helpful for debugging and setting up formations in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        // Don't draw if unitPrefab is not assigned or no units
        if (unitPrefab == null || numberOfUnits == 0) return;

        // Ensure a current formation strategy is set, even if not in play mode
        // This allows Gizmos to be drawn correctly when just editing in the inspector.
        if (_currentFormation == null)
        {
            // Temporarily create a formation instance to draw gizmos without affecting game state
            IFormation tempFormation = null;
            switch (formationType)
            {
                case FormationType.Line:
                    tempFormation = new LineFormation(spacing);
                    break;
                case FormationType.Circle:
                    tempFormation = new CircleFormation(circleRadius, circleStartAngle);
                    break;
                case FormationType.Grid:
                    tempFormation = new GridFormation(gridSpacingX, gridSpacingY, gridUnitsPerRow);
                    break;
                case FormationType.VShape:
                    tempFormation = new VFormation(spacing, vFormationSpreadAngle);
                    break;
            }
            if (tempFormation == null) return; // Should not happen
            _currentFormation = tempFormation;
        }


        // Store original Gizmos color to restore later
        Color originalColor = Gizmos.color;
        Gizmos.color = new Color(0, 1, 1, 0.75f); // Cyan color for visibility

        // Get the mesh from the unit prefab to draw realistic gizmos
        MeshFilter prefabMeshFilter = unitPrefab.GetComponent<MeshFilter>();
        Mesh prefabMesh = prefabMeshFilter != null ? prefabMeshFilter.sharedMesh : null;

        // Draw the local positions for each unit
        for (int i = 0; i < numberOfUnits; i++)
        {
            Vector3 localPos = _currentFormation.GetUnitLocalPosition(i, numberOfUnits);
            // Convert local position to world position
            Vector3 worldPos = transform.TransformPoint(localPos);

            if (prefabMesh != null)
            {
                // Draw a wire mesh if a mesh is available
                Gizmos.DrawWireMesh(prefabMesh, worldPos, transform.rotation, unitPrefab.transform.localScale);
            }
            else
            {
                // Otherwise, draw a sphere as a placeholder
                Gizmos.DrawWireSphere(worldPos, 0.5f);
            }
        }

        Gizmos.color = originalColor; // Restore original Gizmos color
    }
}
```