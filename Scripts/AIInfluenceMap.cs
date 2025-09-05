// Unity Design Pattern Example: AIInfluenceMap
// This script demonstrates the AIInfluenceMap pattern in Unity
// Generated automatically - ready to use in your Unity project

The AI Influence Map is a powerful design pattern in game AI, particularly useful for strategy games, RTS, and open-world environments. It provides AI agents with a high-level understanding of the game world by representing various "influences" (danger, attraction, resource availability, cover, etc.) on a grid. Agents can then query this map to make informed decisions about movement, targeting, and behavior.

This C# Unity example demonstrates a practical implementation of an AI Influence Map.

**Key Components:**

1.  **`InfluenceType` Enum:** Categorizes different kinds of influence (e.g., enemy threat, objective attraction).
2.  **`InfluenceSource.cs`:** A `MonoBehaviour` that you attach to game objects (like players, enemies, or objectives) that emit influence. It defines the type, radius, and strength of the influence.
3.  **`InfluenceMapManager.cs`:** The core manager that:
    *   Maintains a 2D grid (`float[,]`) to store influence values.
    *   Registers and unregisters `InfluenceSource` objects.
    *   Periodically recalculates the influence values across the entire map, decaying influence with distance.
    *   Provides methods for AI agents to query influence at specific world positions or find "best" positions.
    *   Visualizes the map in the Unity editor using Gizmos, showing areas of positive (attraction) and negative (danger) influence.
4.  **`AIController.cs`:** An example AI agent that demonstrates how to query the `InfluenceMapManager` to make decisions, such as moving towards attractive areas or away from dangerous ones.

---

### C# Unity Scripts

Create three C# scripts named `InfluenceType.cs`, `InfluenceSource.cs`, `InfluenceMapManager.cs`, and `AIController.cs` in your Unity project.

**1. `InfluenceType.cs` (Enum Definition)**
*(Note: While `InfluenceType` could be defined within `InfluenceMapManager`, separating enums into their own file is good practice for larger projects if they are used by multiple distinct classes.)*

```csharp
// InfluenceType.cs
using UnityEngine; // Included for good measure, though not strictly needed for just an enum.

/// <summary>
/// Enum to categorize different types of influence.
/// This allows the map to potentially track multiple layers or interpret values differently.
/// For this example, we're combining them into a single map where positive is good, negative is bad.
/// </summary>
public enum InfluenceType
{
    Threat_Enemy,       // Danger from enemies (typically negative strength)
    Threat_Friendly,    // Danger from friendly fire (less common, but possible, negative strength)
    Objective_Primary,  // Attraction for primary objectives (positive strength)
    Objective_Secondary, // Attraction for secondary objectives (positive strength)
    Resource_Gatherable, // Attraction for resources (positive strength)
    Cover_Positive,     // Positive influence for cover (good defensive spot, positive strength)
    Cover_Negative,     // Negative influence for lack of cover (bad offensive spot, negative strength)
    SafeZone,           // Explicitly safe areas (strong positive strength)
    DangerZone          // Explicitly dangerous areas (strong negative strength)
}
```

**2. `InfluenceSource.cs`**

```csharp
// InfluenceSource.cs
using UnityEngine;

/// <summary>
/// Represents a single point source of influence on the map.
/// This component should be attached to GameObjects that emit influence (e.g., players, enemies, objectives).
/// </summary>
public class InfluenceSource : MonoBehaviour
{
    [Header("Influence Properties")]
    [Tooltip("The type of influence this source emits. Affects its meaning, but for this example, " +
             "all types contribute to a single 'desirability' map based on strength.")]
    public InfluenceType influenceType = InfluenceType.Threat_Enemy;

    [Range(0.1f, 50f)]
    [Tooltip("How far the influence spreads from this source in world units.")]
    public float influenceRadius = 5f;

    [Range(-10f, 10f)]
    [Tooltip("The base strength of the influence. Positive values make areas more attractive/safe, " +
             "negative values make them more dangerous/undesirable.")]
    public float influenceStrength = 5f;

    [Header("Visualization")]
    [Tooltip("Color for gizmo visualization of this source's radius.")]
    public Color gizmoColor = Color.red;

    private InfluenceMapManager _mapManager; // Reference to the influence map manager

    /// <summary>
    /// When the object becomes active, register itself with the InfluenceMapManager.
    /// </summary>
    void OnEnable()
    {
        // Find the InfluenceMapManager in the scene.
        // It's designed as a singleton, so we can access its static Instance property.
        _mapManager = InfluenceMapManager.Instance;
        if (_mapManager != null)
        {
            _mapManager.RegisterSource(this);
        }
        else
        {
            Debug.LogWarning($"InfluenceMapManager not found in scene for {name}. InfluenceSource will not function.");
        }
    }

    /// <summary>
    /// When the object becomes inactive or is destroyed, unregister itself from the InfluenceMapManager.
    /// </summary>
    void OnDisable()
    {
        if (_mapManager != null)
        {
            _mapManager.UnregisterSource(this);
        }
    }

    /// <summary>
    /// Call this method if the source's world position or its influence properties (radius, strength) change
    /// to notify the map manager that a recalculation might be needed.
    /// In a real game, you might optimize this to only update if a significant change occurs
    /// or let the manager poll active sources.
    /// </summary>
    public void NotifyMapManagerOfChange()
    {
        if (_mapManager != null)
        {
            _mapManager.MarkMapDirty();
        }
    }

    /// <summary>
    /// Draws a wire sphere in the editor to visualize the influence radius of this source.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, influenceRadius);
    }
}
```

**3. `InfluenceMapManager.cs`**

```csharp
// InfluenceMapManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For debugging and potential LINQ queries (not heavily used here)
using UnityEditor; // Used for Selection.Contains in OnDrawGizmos to draw only when selected.

/// <summary>
/// This is the core Influence Map Manager. It holds the grid data and manages influence calculations.
/// It's designed as a Singleton for easy access from other scripts (e.g., AIController).
/// </summary>
public class InfluenceMapManager : MonoBehaviour
{
    // Singleton pattern for easy access throughout the game.
    public static InfluenceMapManager Instance { get; private set; }

    [Header("Map Settings")]
    [Tooltip("The width of the map grid in cells.")]
    public int mapWidth = 50;
    [Tooltip("The height of the map grid in cells.")]
    public int mapHeight = 50;
    [Tooltip("The size of each cell in Unity world units.")]
    public float cellSize = 1f;
    [Tooltip("The world position of the bottom-left corner of the map grid.")]
    public Vector3 mapOrigin = Vector3.zero;

    [Header("Influence Decay")]
    [Tooltip("Controls the decay curve. 1 for linear decay, >1 for faster initial decay, <1 for slower initial decay.")]
    [Range(0.1f, 2f)]
    public float decayRate = 1f;

    [Header("Update Settings")]
    [Tooltip("How often the influence map is recalculated (in seconds). Set to 0 for every frame (less efficient).")]
    public float updateInterval = 0.5f;
    [Tooltip("Clamps influence values to prevent them from becoming too extreme.")]
    public float minInfluence = -100f;
    public float maxInfluence = 100f;

    [Header("Visualization")]
    [Tooltip("Enable/disable drawing influence map gizmos in the editor.")]
    public bool drawGizmos = true;
    [Tooltip("Color for cells with zero influence.")]
    public Color neutralColor = Color.gray;
    [Tooltip("Color for cells with positive influence (attraction/safety).")]
    public Color positiveColor = Color.green;
    [Tooltip("Color for cells with negative influence (danger/undesirability).")]
    public Color negativeColor = Color.red;
    [Range(0f, 1f)]
    [Tooltip("Transparency for the influence visualization cubes.")]
    public float gizmoAlpha = 0.5f;
    [Tooltip("Scales the height of the influence visualization cubes for better visibility. " +
             "Influence value determines relative height within this scale.")]
    public float gizmoHeightScale = 0.2f;
    [Tooltip("If true, gizmos are only drawn when the InfluenceMapManager GameObject is selected in the editor. " +
             "Useful for large maps to reduce editor clutter/performance impact.")]
    public bool drawGizmosOnlyWhenSelected = false;

    // The 2D array storing influence values for each cell.
    private float[,] _influenceValues;
    // List of all active influence sources currently affecting the map.
    private List<InfluenceSource> _influenceSources = new List<InfluenceSource>();

    private float _lastUpdateTime; // Tracks when the map was last updated.
    private bool _isMapDirty = true; // Flag to indicate if the map needs recalculation due to changes.

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances.
        }
        else
        {
            Instance = this; // Set this as the singleton instance.
            InitializeMap(); // Set up the influence grid.
        }
    }

    void Update()
    {
        // Recalculate the map if it's dirty or if the update interval has passed.
        if (_isMapDirty || (updateInterval > 0 && Time.time >= _lastUpdateTime + updateInterval))
        {
            RecalculateInfluenceMap();
            _lastUpdateTime = Time.time;
            _isMapDirty = false;
        }
    }

    // --- Map Initialization ---

    /// <summary>
    /// Initializes the 2D array that stores influence values and clears it.
    /// </summary>
    private void InitializeMap()
    {
        _influenceValues = new float[mapWidth, mapHeight];
        ClearMap(); // Set all values to zero initially.
    }

    // --- Influence Source Management ---

    /// <summary>
    /// Registers an InfluenceSource with the manager, ensuring it contributes to the map.
    /// </summary>
    /// <param name="source">The InfluenceSource to register.</param>
    public void RegisterSource(InfluenceSource source)
    {
        if (!_influenceSources.Contains(source))
        {
            _influenceSources.Add(source);
            MarkMapDirty(); // Map needs recalculation when a new source is added.
        }
    }

    /// <summary>
    /// Unregisters an InfluenceSource, removing its contribution from the map.
    /// </summary>
    /// <param name="source">The InfluenceSource to unregister.</param>
    public void UnregisterSource(InfluenceSource source)
    {
        if (_influenceSources.Remove(source))
        {
            MarkMapDirty(); // Map needs recalculation when a source is removed.
        }
    }

    // --- Map Operations ---

    /// <summary>
    /// Flags the map for recalculation during the next update cycle.
    /// Call this when an influence source is added, removed, moved, or its properties change.
    /// </summary>
    public void MarkMapDirty()
    {
        _isMapDirty = true;
    }

    /// <summary>
    /// Resets all influence values on the map to zero.
    /// </summary>
    public void ClearMap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                _influenceValues[x, y] = 0f;
            }
        }
        // No need to MarkMapDirty here, as RecalculateInfluenceMap calls ClearMap and then updates.
        // If ClearMap was called independently to truly empty the map, then it would need to mark dirty.
    }

    /// <summary>
    /// Recalculates the entire influence map based on all active sources.
    /// This can be computationally intensive for very large maps or many sources,
    /// but is simple and effective for demonstration purposes.
    /// </summary>
    private void RecalculateInfluenceMap()
    {
        ClearMap(); // Start with a fresh, empty map

        foreach (InfluenceSource source in _influenceSources)
        {
            ApplyInfluenceFromSource(source);
        }
    }

    /// <summary>
    /// Applies the influence from a single source to the map's cells.
    /// Influence decays with distance from the source.
    /// </summary>
    private void ApplyInfluenceFromSource(InfluenceSource source)
    {
        // Convert source's world position to grid coordinates.
        Vector2Int sourceGridPos = WorldToGridPosition(source.transform.position);

        // Calculate the effective radius in grid cells.
        int gridRadius = Mathf.CeilToInt(source.influenceRadius / cellSize);

        // Iterate over a square region around the source's grid position.
        // This is an optimization to avoid iterating the entire map for each source.
        for (int x = sourceGridPos.x - gridRadius; x <= sourceGridPos.x + gridRadius; x++)
        {
            for (int y = sourceGridPos.y - gridRadius; y <= sourceGridPos.y + gridRadius; y++)
            {
                // Ensure the cell is within the map bounds.
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    Vector3 cellWorldCenter = GridToWorldPosition(x, y);
                    float distance = Vector3.Distance(source.transform.position, cellWorldCenter);

                    // If the cell is outside the source's radius, it receives no influence.
                    if (distance > source.influenceRadius)
                    {
                        continue;
                    }

                    // Calculate influence decay based on distance.
                    // Simple linear decay: influence reduces linearly from full strength at source to 0 at radius edge.
                    // Using Mathf.Pow for decayRate allows more control over the decay curve.
                    float normalizedDistance = distance / source.influenceRadius;
                    float decayFactor = 1f - Mathf.Pow(normalizedDistance, decayRate);

                    // Apply influence, clamped to min/max to prevent extreme values.
                    _influenceValues[x, y] = Mathf.Clamp(
                        _influenceValues[x, y] + (source.influenceStrength * decayFactor),
                        minInfluence, maxInfluence
                    );
                }
            }
        }
    }

    /// <summary>
    /// Retrieves the influence value at a specific world position.
    /// </summary>
    /// <param name="worldPosition">The world coordinates to query.</param>
    /// <returns>The influence value at that cell, or 0 if outside map bounds.</returns>
    public float GetInfluenceAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);

        if (gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight)
        {
            return _influenceValues[gridPos.x, gridPos.y];
        }
        return 0f; // Return neutral influence (0) if queried position is outside map bounds.
    }

    /// <summary>
    /// Finds the cell with the highest or lowest influence within a specified world radius around a center point.
    /// Useful for AI to find optimal positions (e.g., safest spot, best attack position).
    /// </summary>
    /// <param name="centerWorldPosition">The center of the search area.</param>
    /// <param name="searchRadius">The radius of the search area in world units.</param>
    /// <param name="maximizeInfluence">If true, returns position with highest influence; otherwise, lowest.</param>
    /// <returns>The world position of the best cell, or the centerWorldPosition if no valid cells are found
    /// (e.g., search radius entirely outside map).</returns>
    public Vector3 GetBestPositionInRadius(Vector3 centerWorldPosition, float searchRadius, bool maximizeInfluence)
    {
        Vector2Int centerGridPos = WorldToGridPosition(centerWorldPosition);
        int gridSearchRadius = Mathf.CeilToInt(searchRadius / cellSize);

        Vector3 bestWorldPos = centerWorldPosition; // Default to current position
        float bestInfluence = maximizeInfluence ? float.MinValue : float.MaxValue;
        bool foundValidPosition = false;

        // Iterate over a square region defined by the search radius in grid cells.
        for (int x = centerGridPos.x - gridSearchRadius; x <= centerGridPos.x + gridSearchRadius; x++)
        {
            for (int y = centerGridPos.y - gridSearchRadius; y <= centerGridPos.y + gridSearchRadius; y++)
            {
                // Ensure the cell is within the map bounds.
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    Vector3 cellWorldCenter = GridToWorldPosition(x, y);

                    // Check if the cell is actually within the circular search radius (not just the square bounds).
                    if (Vector3.Distance(centerWorldPosition, cellWorldCenter) > searchRadius)
                    {
                        continue;
                    }

                    float currentInfluence = _influenceValues[x, y];

                    // Determine if this cell's influence is "better" than the current best.
                    if ((maximizeInfluence && currentInfluence > bestInfluence) ||
                        (!maximizeInfluence && currentInfluence < bestInfluence))
                    {
                        bestInfluence = currentInfluence;
                        bestWorldPos = cellWorldCenter;
                        foundValidPosition = true;
                    }
                }
            }
        }

        // If no valid cell was found within the search area (e.g., search radius entirely outside map),
        // return the original center position, indicating no better alternative was found.
        return foundValidPosition ? bestWorldPos : centerWorldPosition;
    }


    // --- Coordinate Conversion Utilities ---

    /// <summary>
    /// Converts a world position to its corresponding grid coordinates (x, y).
    /// Clamps the result to ensure it's within map bounds.
    /// </summary>
    /// <param name="worldPosition">The world coordinates.</param>
    /// <returns>A Vector2Int representing the grid (column, row) coordinates.</returns>
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        // Calculate grid coordinates relative to the map origin.
        int x = Mathf.FloorToInt((worldPosition.x - mapOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.z - mapOrigin.z) / cellSize); // Assuming Z is the 'Y' axis for the 2D grid.

        // Clamp to ensure coordinates are within the map boundaries.
        x = Mathf.Clamp(x, 0, mapWidth - 1);
        y = Mathf.Clamp(y, 0, mapHeight - 1);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates (x, y) to the world position of the cell's center.
    /// </summary>
    /// <param name="x">The grid column index.</param>
    /// <param name="y">The grid row index.</param>
    /// <returns>A Vector3 representing the world center of the cell.</returns>
    public Vector3 GridToWorldPosition(int x, int y)
    {
        // Calculate world position, adding half a cell size to get to the center.
        float worldX = mapOrigin.x + (x * cellSize) + (cellSize * 0.5f);
        float worldZ = mapOrigin.z + (y * cellSize) + (cellSize * 0.5f);
        // Assuming the map is flat on the XZ plane. Y-coordinate (height) is kept at mapOrigin.y.
        // In a complex game, you might raycast or query terrain/NavMesh for the actual ground height.
        return new Vector3(worldX, mapOrigin.y, worldZ);
    }

    // --- Debug Visualization (Gizmos) ---

    /// <summary>
    /// Called by Unity to draw gizmos in the editor, even when not selected.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw if enabled, map data exists, and if not restricted to 'selected only'.
        if (!drawGizmos || _influenceValues == null || (drawGizmosOnlyWhenSelected && !Selection.Contains(gameObject)))
        {
            return;
        }
        DrawInfluenceGizmos();
    }

    /// <summary>
    /// Called by Unity to draw gizmos in the editor, only when the GameObject is selected.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw always if 'drawGizmosOnlyWhenSelected' is false, otherwise only if selected.
        if (!drawGizmos || _influenceValues == null || (drawGizmosOnlyWhenSelected && !Selection.Contains(gameObject)))
        {
            return;
        }
        DrawInfluenceGizmos();
    }


    /// <summary>
    /// Renders the influence map as colored cubes in the Unity editor's Scene view.
    /// Color and height of cubes reflect the influence value.
    /// </summary>
    private void DrawInfluenceGizmos()
    {
        // Draw map boundaries for context.
        Gizmos.color = Color.blue;
        Vector3 mapCorner0 = mapOrigin;
        Vector3 mapCorner1 = mapOrigin + new Vector3(mapWidth * cellSize, 0, 0);
        Vector3 mapCorner2 = mapOrigin + new Vector3(mapWidth * cellSize, 0, mapHeight * cellSize);
        Vector3 mapCorner3 = mapOrigin + new Vector3(0, 0, mapHeight * cellSize);

        Gizmos.DrawLine(mapCorner0, mapCorner1);
        Gizmos.DrawLine(mapCorner1, mapCorner2);
        Gizmos.DrawLine(mapCorner2, mapCorner3);
        Gizmos.DrawLine(mapCorner3, mapCorner0);

        // Draw each cell as a colored cube.
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float influence = _influenceValues[x, y];
                Vector3 cellCenter = GridToWorldPosition(x, y);
                Vector3 cubeSize = new Vector3(cellSize, gizmoHeightScale, cellSize);

                // Interpolate color based on influence value.
                // Positive influence fades from neutral to positiveColor.
                // Negative influence fades from neutral to negativeColor.
                Color influenceColor;
                if (influence > 0)
                {
                    influenceColor = Color.Lerp(neutralColor, positiveColor, influence / maxInfluence);
                }
                else
                {
                    influenceColor = Color.Lerp(neutralColor, negativeColor, influence / minInfluence);
                }
                influenceColor.a = gizmoAlpha; // Apply transparency.

                Gizmos.color = influenceColor;

                // Draw cube slightly offset upwards based on influence value to visualize height.
                // This makes areas with stronger influence stand out more visually.
                float normalizedInfluenceHeight = influence / maxInfluence; // Normalize influence for height scaling
                if (influence < 0) normalizedInfluenceHeight = influence / Mathf.Abs(minInfluence); // For negative influences

                Gizmos.DrawCube(cellCenter + Vector3.up * (normalizedInfluenceHeight * gizmoHeightScale * 0.5f), cubeSize);
            }
        }
    }
}
```

**4. `AIController.cs`**

```csharp
// AIController.cs
using UnityEngine;

/// <summary>
/// An example AI Controller that utilizes the Influence Map to make movement decisions.
/// This script would be attached to an AI agent (e.g., an enemy or a friendly unit).
/// </summary>
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("The movement speed of the AI agent.")]
    public float moveSpeed = 3f;
    [Tooltip("How often the AI agent re-evaluates its target position based on the influence map (in seconds).")]
    public float decisionInterval = 1f;
    [Tooltip("The radius around the AI within which it searches for the 'best' position on the influence map.")]
    public float searchRadius = 10f;
    [Tooltip("If true, the AI seeks areas with high positive influence (attraction/safety). " +
             "If false, it seeks areas with low (more negative) influence (avoiding danger).")]
    public bool seekPositiveInfluence = true;

    private InfluenceMapManager _influenceMapManager; // Reference to the singleton InfluenceMapManager.
    private Vector3 _targetPosition; // The current target position the AI is moving towards.
    private float _lastDecisionTime; // Timestamp of the last decision made.

    void Start()
    {
        // Get the singleton instance of the InfluenceMapManager.
        _influenceMapManager = InfluenceMapManager.Instance;
        if (_influenceMapManager == null)
        {
            Debug.LogError("AIController: InfluenceMapManager not found in scene! Disabling AI.");
            enabled = false; // Disable this component if the manager is missing.
            return;
        }

        _targetPosition = transform.position; // Initialize target to current position.
        _lastDecisionTime = Time.time; // Set initial decision time.
    }

    void Update()
    {
        if (_influenceMapManager == null) return; // Ensure manager exists before proceeding.

        // Make a new decision periodically based on the `decisionInterval`.
        if (Time.time >= _lastDecisionTime + decisionInterval)
        {
            MakeDecision();
            _lastDecisionTime = Time.time;
        }

        // Move the AI agent towards its current target position.
        MoveToTarget();
    }

    /// <summary>
    /// Queries the Influence Map to find a new target position based on desired influence.
    /// </summary>
    private void MakeDecision()
    {
        // Use the InfluenceMapManager to find the best position within the AI's search radius.
        // `seekPositiveInfluence` determines if it's looking for the highest or lowest influence.
        _targetPosition = _influenceMapManager.GetBestPositionInRadius(
            transform.position,
            searchRadius,
            seekPositiveInfluence
        );

        // Debug output to show the AI's decision.
        float influenceAtTarget = _influenceMapManager.GetInfluenceAtWorldPosition(_targetPosition);
        Debug.Log($"{name} AI decided to move to {_targetPosition} (Influence: {influenceAtTarget}). " +
                  $"Current position influence: {_influenceMapManager.GetInfluenceAtWorldPosition(transform.position)}");
    }

    /// <summary>
    /// Moves the AI agent towards its calculated target position.
    /// </summary>
    private void MoveToTarget()
    {
        // Only move if the AI is not already very close to its target.
        if (Vector3.Distance(transform.position, _targetPosition) > 0.1f)
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize the AI's search radius and its current target.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (_influenceMapManager == null) return;

        // Draw the AI's search radius as a cyan wire sphere.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        // Draw a yellow line from the AI's current position to its target position.
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, _targetPosition);
        Gizmos.DrawWireSphere(_targetPosition, 0.2f); // Mark the target spot with a small sphere.
    }
}
```

---

### How to Use in Unity (Example Setup)

Follow these steps to set up and observe the Influence Map in your Unity project:

1.  **Create an Empty GameObject for the Manager:**
    *   Right-click in the Hierarchy -> "Create Empty".
    *   Rename it to `InfluenceMapManager`.
    *   Drag and drop the `InfluenceMapManager.cs` script onto this GameObject.

2.  **Configure the `InfluenceMapManager`:**
    *   In the Inspector for `InfluenceMapManager`:
        *   **Map Settings:**
            *   `Map Width` & `Map Height`: Set these based on your scene's playable area (e.g., `50`x`50`).
            *   `Cell Size`: `1` is a good starting point (each cell is 1x1 Unity unit).
            *   `Map Origin`: This is the world coordinate of the bottom-left corner of your map. For a map centered at (0,0,0) and 50 units wide, you'd set it to `(-25, 0, -25)`. (The Y-component here represents vertical height, which you might adjust to be at ground level).
        *   **Influence Decay:** `Decay Rate` of `1` for linear decay, experiment with `0.5` to `2.0`.
        *   **Update Settings:** `Update Interval` of `0.5` seconds is a good balance for dynamic maps without constant recalculation.
        *   **Visualization:** Enable `Draw Gizmos` and adjust `Gizmo Alpha` (e.g., `0.5`), `Positive Color` (e.g., Green), `Negative Color` (e.g., Red), and `Neutral Color` (e.g., Gray). `Gizmo Height Scale` (e.g., `0.2`) makes the cubes more visible. Consider enabling `Draw Gizmos Only When Selected` for performance on large maps.

3.  **Create Influence Sources (e.g., Player, Enemies, Objectives):**
    *   Create several 3D objects in your scene (e.g., `GameObject -> 3D Object -> Cube`).
    *   **Rename them:** "Player", "Enemy_A", "Enemy_B", "Objective_Flag", etc.
    *   **Attach `InfluenceSource.cs`:** Drag and drop the `InfluenceSource.cs` script onto each of these GameObjects.
    *   **Configure each `InfluenceSource`:**
        *   **`Player` Example:** `InfluenceType = SafeZone`, `Influence Radius = 8`, `Influence Strength = 8` (Positive influence, makes area around player attractive). Set `Gizmo Color` to Blue.
        *   **`Enemy_A` Example:** `InfluenceType = Threat_Enemy`, `Influence Radius = 10`, `Influence Strength = -10` (Negative influence, makes area dangerous). Set `Gizmo Color` to Red.
        *   **`Objective_Flag` Example:** `InfluenceType = Objective_Primary`, `Influence Radius = 6`, `Influence Strength = 10` (Strong positive influence, makes area highly attractive). Set `Gizmo Color` to Green.
    *   Position these objects in your scene. You will immediately see the influence map gizmos update (if `Draw Gizmos` is enabled on the `InfluenceMapManager`).

4.  **Create an AI Agent:**
    *   Create another 3D object (e.g., `GameObject -> 3D Object -> Capsule`).
    *   Rename it to "AI_Unit".
    *   Drag and drop the `AIController.cs` script onto this GameObject.
    *   **Configure `AIController` properties:**
        *   `Move Speed`: `3`
        *   `Decision Interval`: `1` (The AI will re-evaluate its target every second).
        *   `Search Radius`: `10` (The AI looks for the best spot within 10 units of itself).
        *   `Seek Positive Influence`:
            *   Set to `true` if this AI should move towards "good" areas (like objectives or safe zones).
            *   Set to `false` if this AI should move away from "bad" areas (like enemies or danger zones). For an enemy avoiding your player, you'd make your player a positive source and the enemy AI `seekPositiveInfluence = false` (meaning it seeks the least positive, or most negative, influence).

5.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Observe how the "AI_Unit" moves. It will actively navigate based on the combined influence generated by your `InfluenceSource` objects.
    *   Try moving your "Player" or "Enemy_A" objects around during runtime. You'll see the influence map recalculate, and the "AI_Unit" will react dynamically to the changing environment!

This setup provides a complete and interactive demonstration of the AI Influence Map pattern in Unity, allowing you to visualize and experiment with its core mechanics.