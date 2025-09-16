// Unity Design Pattern Example: GridBasedBuildingSystem
// This script demonstrates the GridBasedBuildingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Grid-Based Building System** design pattern. This pattern is fundamental for games like city builders, tower defense, or strategy games where objects are placed on a discrete grid.

The pattern separates concerns:
1.  **Building Definition (`BuildingAsset`):** A ScriptableObject that defines the *type* of building (its prefab, size, cost, etc.). This makes it data-driven and easy to add new building types without changing code.
2.  **Placed Building Instance (`PlacedBuilding`):** A MonoBehaviour attached to an instantiated building prefab, linking it back to its `BuildingAsset` and storing its grid origin.
3.  **Grid Tile Data (`GridTile`):** A simple class that represents the state of a single cell on the grid (e.g., whether it's occupied, and by which building).
4.  **Grid Management System (`GridBuildingSystem`):** The core MonoBehaviour that manages the entire grid state, handles world-to-grid conversions, placement logic (checking for validity), and removal.
5.  **Input and Visualization:** Handles mouse input for placing/removing and displays a "ghost" building for visual feedback during placement.

---

**1. `BuildingAsset.cs` (ScriptableObject for Building Definitions)**

This ScriptableObject defines the properties of a building type. It's a data container, not an instance.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// BuildingAsset is a ScriptableObject that defines a type of building.
/// It contains data like the building's prefab, its size on the grid, and other metadata.
/// This separates building data from the core building system logic, making it highly modular.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingAsset", menuName = "Building/Building Asset")]
public class BuildingAsset : ScriptableObject
{
    [Header("Building Data")]
    public string buildingName = "New Building"; // Display name of the building
    public GameObject prefab; // The actual 3D model/prefab to instantiate for this building
    public Vector2Int size = Vector2Int.one; // The dimensions of the building in grid units (e.g., 1x1, 2x2)

    [Header("UI/Gameplay Data (Optional)")]
    public Sprite icon; // Icon for UI display
    public int cost = 100; // Cost to place this building
    [TextArea] public string description = "A basic building."; // Description for UI

    /// <summary>
    /// Calculates all grid positions that this building would occupy, given its bottom-left origin.
    /// Assumes a flat grid where 'size.x' extends along the X-axis and 'size.y' extends along the Z-axis.
    /// </summary>
    /// <param name="origin">The bottom-left grid coordinate where the building starts.</param>
    /// <returns>A list of Vector3Int representing all grid cells occupied by the building.</returns>
    public List<Vector3Int> GetOccupiedGridPositions(Vector3Int origin)
    {
        List<Vector3Int> occupiedPositions = new List<Vector3Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++) // Using 'size.y' for Z-depth on a flat grid
            {
                occupiedPositions.Add(origin + new Vector3Int(x, 0, z)); // Y is 0 for flat grid
            }
        }
        return occupiedPositions;
    }
}
```

---

**2. `PlacedBuilding.cs` (Component for Instantiated Buildings)**

This MonoBehaviour is attached to each actual building GameObject that is instantiated in the scene. It stores a reference to its `BuildingAsset` and its grid origin, allowing us to query its properties and the cells it occupies.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// PlacedBuilding is a MonoBehaviour attached to an actual instantiated building in the scene.
/// It acts as a bridge, linking the runtime GameObject instance back to its
/// BuildingAsset definition and its specific grid placement data.
/// </summary>
public class PlacedBuilding : MonoBehaviour
{
    // Public properties to access building data
    public BuildingAsset Asset { get; private set; }
    public Vector3Int GridOrigin { get; private set; } // The bottom-left grid coordinate this building occupies.

    private List<Vector3Int> _occupiedGridPositions; // Cache of all grid positions this building takes up.

    /// <summary>
    /// Initializes this placed building instance. This should be called immediately after instantiation.
    /// </summary>
    /// <param name="asset">The BuildingAsset definition for this building.</param>
    /// <param name="gridOrigin">The grid coordinate where this building was placed (its bottom-left origin).</param>
    public void Initialize(BuildingAsset asset, Vector3Int gridOrigin)
    {
        Asset = asset;
        GridOrigin = gridOrigin;
        // Pre-calculate and cache the occupied grid positions for efficiency
        _occupiedGridPositions = Asset.GetOccupiedGridPositions(GridOrigin);
    }

    /// <summary>
    /// Returns a list of all grid positions this building currently occupies.
    /// </summary>
    public List<Vector3Int> GetOccupiedGridPositions()
    {
        return _occupiedGridPositions;
    }

    // You might add methods here for building specific actions, e.g.,
    // public void DoBuildingAction() { /* ... */ }
    // public int GetHealth() { /* ... */ }
}
```

---

**3. `GridTile.cs` (Data Structure for Grid Cells)**

A simple class (not a MonoBehaviour) that represents the state of a single grid cell. It tells us if the cell is occupied and, if so, which `PlacedBuilding` occupies it.

```csharp
/// <summary>
/// GridTile represents a single cell within the grid.
/// It stores information about the tile's state, specifically whether it's occupied
/// and a reference to the PlacedBuilding that occupies it.
/// </summary>
public class GridTile
{
    public bool IsOccupied { get; private set; } // True if the tile is currently occupied
    public PlacedBuilding OccupyingBuilding { get; private set; } // Reference to the building occupying this tile (null if not occupied)

    /// <summary>
    /// Marks this tile as occupied by a specific building.
    /// </summary>
    /// <param name="building">The PlacedBuilding instance that is now occupying this tile.</param>
    public void Occupy(PlacedBuilding building)
    {
        IsOccupied = true;
        OccupyingBuilding = building;
    }

    /// <summary>
    /// Clears this tile, making it available for new buildings.
    /// </summary>
    public void Clear()
    {
        IsOccupied = false;
        OccupyingBuilding = null;
    }
}
```

---

**4. `GridBuildingSystem.cs` (The Core System)**

This MonoBehaviour manages the overall grid, handles building placement and removal logic, and interacts with user input. It's often implemented as a Singleton for easy access throughout the game.

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System.Collections; // For IEnumerator if needed (not directly used here)

/// <summary>
/// GridBuildingSystem is the core manager for all grid-based building logic.
/// It implements the Singleton pattern for easy global access.
/// This system handles:
/// - Grid initialization and data storage.
/// - Converting between world coordinates and grid coordinates.
/// - Checking if a building can be placed at a location (validity).
/// - Placing buildings (instantiating prefabs, updating grid data).
/// - Removing buildings (destroying prefabs, clearing grid data).
/// - Displaying a ghost building for visual placement feedback.
/// - Handling mouse input for placement and removal.
/// </summary>
public class GridBuildingSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global point of access to the GridBuildingSystem instance.
    public static GridBuildingSystem Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private Vector2Int _gridSize = new Vector2Int(50, 50); // The total dimensions of the grid (X and Z)
    [SerializeField] private float _cellSize = 1.0f; // The real-world size (width/depth) of a single grid cell
    [SerializeField] private Vector3 _gridOriginWorldPosition = Vector3.zero; // The world position of the grid's (0,0,0) coordinate

    [Header("Placement Settings")]
    [SerializeField] private LayerMask _placementLayer; // A Unity LayerMask to specify which layers the placement raycast should hit (e.g., "Ground").
    [SerializeField] private Material _validPlacementMaterial; // Material for the ghost building when placement is valid.
    [SerializeField] private Material _invalidPlacementMaterial; // Material for the ghost building when placement is invalid.
    [SerializeField] private float _placementHeightOffset = 0.05f; // Small vertical offset to prevent Z-fighting with the ground.

    // --- Internal Grid Data ---
    // A dictionary to store GridTile data. This is efficient for sparse grids
    // where not every cell needs an explicit GridTile object from the start.
    private Dictionary<Vector3Int, GridTile> _gridData;

    // --- Current Placement State ---
    private BuildingAsset _selectedBuildingAsset; // The BuildingAsset currently chosen for placement.
    private GameObject _ghostBuildingInstance; // The visual preview (ghost) of the building being placed.
    private Renderer[] _ghostBuildingRenderers; // All renderers on the ghost building to change its material.

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Enforce Singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate
        }
        else
        {
            Instance = this; // Set this instance as the Singleton
        }

        InitializeGrid(); // Prepare the grid data structure
    }

    private void Update()
    {
        HandleBuildingPlacementInput(); // Process user input for building
    }

    // --- Public API for Interaction ---
    // These methods can be called by UI elements or other game logic to interact with the building system.

    /// <summary>
    /// Selects a specific BuildingAsset for placement. This will activate the ghost building visualization.
    /// Call this when a player clicks a UI button to choose a building.
    /// </summary>
    /// <param name="asset">The BuildingAsset to be placed.</param>
    public void SelectBuildingToPlace(BuildingAsset asset)
    {
        _selectedBuildingAsset = asset;
        DestroyGhostBuilding(); // Remove any existing ghost first

        if (_selectedBuildingAsset != null)
        {
            // Instantiate the ghost building based on the selected asset's prefab
            _ghostBuildingInstance = Instantiate(_selectedBuildingAsset.prefab);
            _ghostBuildingInstance.name = "Ghost Building (Preview)";
            _ghostBuildingInstance.transform.parent = transform; // Parent to this system for scene organization

            // Get all renderers for material switching
            _ghostBuildingRenderers = _ghostBuildingInstance.GetComponentsInChildren<Renderer>();
            SetGhostMaterial(_invalidPlacementMaterial); // Start with invalid material
            
            // Disable colliders and other components on the ghost building to prevent interaction
            foreach (var collider in _ghostBuildingInstance.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            // You might also want to remove physics components like Rigidbodies
        }
    }

    /// <summary>
    /// Clears the currently selected building asset, effectively exiting placement mode.
    /// Call this when a player cancels placement (e.g., presses 'C' or right-clicks without a building selected).
    /// </summary>
    public void DeselectBuilding()
    {
        _selectedBuildingAsset = null;
        DestroyGhostBuilding(); // Hide and destroy the ghost building
    }

    /// <summary>
    /// Attempts to place the currently selected building asset at a given grid position.
    /// </summary>
    /// <param name="gridPosition">The bottom-left grid coordinate where the building's origin will be.</param>
    /// <returns>True if the building was successfully placed, false otherwise (e.g., invalid location).</returns>
    public bool PlaceBuilding(Vector3Int gridPosition)
    {
        if (_selectedBuildingAsset == null)
        {
            Debug.LogWarning("GridBuildingSystem: No building asset selected for placement.");
            return false;
        }

        // First, check if placement is valid at this location
        if (CanPlaceBuilding(_selectedBuildingAsset, gridPosition))
        {
            // 1. Instantiate the actual building prefab
            GameObject buildingGO = Instantiate(_selectedBuildingAsset.prefab);
            // Position it in the world, adding a slight offset to avoid Z-fighting
            buildingGO.transform.position = GetWorldPositionFromGrid(gridPosition) + new Vector3(0, _placementHeightOffset, 0);
            buildingGO.transform.parent = transform; // Parent for scene organization
            buildingGO.name = $"{_selectedBuildingAsset.buildingName} ({gridPosition.x},{gridPosition.z})";

            // 2. Add or get the PlacedBuilding component and initialize it
            PlacedBuilding placedBuilding = buildingGO.GetComponent<PlacedBuilding>();
            if (placedBuilding == null)
            {
                placedBuilding = buildingGO.AddComponent<PlacedBuilding>();
            }
            placedBuilding.Initialize(_selectedBuildingAsset, gridPosition);

            // 3. Update the internal grid data to mark occupied tiles
            foreach (Vector3Int occupiedPos in placedBuilding.GetOccupiedGridPositions())
            {
                if (!_gridData.ContainsKey(occupiedPos))
                {
                    _gridData[occupiedPos] = new GridTile(); // Create new GridTile if it doesn't exist
                }
                _gridData[occupiedPos].Occupy(placedBuilding); // Mark the tile as occupied
            }

            Debug.Log($"GridBuildingSystem: Placed {_selectedBuildingAsset.buildingName} at grid {gridPosition}");
            DeselectBuilding(); // After successful placement, exit placement mode
            return true;
        }
        else
        {
            Debug.Log("GridBuildingSystem: Cannot place building here. Grid is occupied or out of bounds.");
            return false;
        }
    }

    /// <summary>
    /// Removes a building from the grid at a specific grid position (which should be the building's origin).
    /// </summary>
    /// <param name="gridPosition">The grid origin of the building to remove.</param>
    /// <returns>True if a building was found and removed, false otherwise.</returns>
    public bool RemoveBuilding(Vector3Int gridPosition)
    {
        // Check if the given grid position is occupied and has a building
        if (_gridData.TryGetValue(gridPosition, out GridTile tile) && tile.IsOccupied && tile.OccupyingBuilding != null)
        {
            PlacedBuilding buildingToRemove = tile.OccupyingBuilding;
            
            // 1. Clear all grid tiles that were occupied by this building
            foreach (Vector3Int occupiedPos in buildingToRemove.GetOccupiedGridPositions())
            {
                // Ensure we only clear tiles truly owned by this specific building instance
                if (_gridData.TryGetValue(occupiedPos, out GridTile occupiedTile) && occupiedTile.OccupyingBuilding == buildingToRemove)
                {
                    occupiedTile.Clear();
                }
            }

            // 2. Destroy the actual GameObject instance from the scene
            Destroy(buildingToRemove.gameObject);
            Debug.Log($"GridBuildingSystem: Removed building at grid {gridPosition}");
            return true;
        }
        else
        {
            Debug.Log("GridBuildingSystem: No building found at this grid position to remove.");
            return false;
        }
    }

    /// <summary>
    /// Checks if a given building asset can be placed at a specific grid position.
    /// This is crucial for valid placement logic.
    /// </summary>
    /// <param name="buildingAsset">The BuildingAsset to check for placement.</param>
    /// <param name="gridPosition">The intended bottom-left grid origin for the building.</param>
    /// <returns>True if the building can be placed, false if it's out of bounds or conflicts with other buildings.</returns>
    public bool CanPlaceBuilding(BuildingAsset buildingAsset, Vector3Int gridPosition)
    {
        if (buildingAsset == null) return false;

        // Get all grid cells this building would occupy
        List<Vector3Int> occupiedPositions = buildingAsset.GetOccupiedGridPositions(gridPosition);

        foreach (Vector3Int pos in occupiedPositions)
        {
            // Check bounds: Ensure placement is within the defined grid size
            if (pos.x < 0 || pos.x >= _gridSize.x || pos.z < 0 || pos.z >= _gridSize.y) // Using Z for depth
            {
                return false; // Out of grid bounds
            }

            // Check occupancy: See if any of the required tiles are already occupied
            if (_gridData.TryGetValue(pos, out GridTile tile) && tile.IsOccupied)
            {
                return false; // Tile is already occupied by another building
            }
        }
        return true; // All checks passed, placement is valid
    }

    // --- Grid Utility Methods ---

    /// <summary>
    /// Converts a world space position to its corresponding grid coordinate.
    /// Assumes a flat grid where X and Z axes are horizontal and Y is vertical.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <returns>The Vector3Int grid coordinate.</returns>
    public Vector3Int GetGridPositionFromWorld(Vector3 worldPosition)
    {
        // Adjust the world position relative to the grid's origin
        Vector3 localPos = worldPosition - _gridOriginWorldPosition;

        // Calculate grid coordinates by dividing by cell size and flooring the result
        int x = Mathf.FloorToInt(localPos.x / _cellSize);
        int z = Mathf.FloorToInt(localPos.z / _cellSize); // Using Z for depth on a flat grid

        return new Vector3Int(x, 0, z); // Y is always 0 for a 2D-like grid in 3D space
    }

    /// <summary>
    /// Converts a grid coordinate to its corresponding world position (typically the center of the cell).
    /// </summary>
    /// <param name="gridPosition">The grid coordinate to convert.</param>
    /// <returns>The Vector3 world position, centered in the cell.</returns>
    public Vector3 GetWorldPositionFromGrid(Vector3Int gridPosition)
    {
        // Calculate world position based on grid coordinates and cell size
        // Add 0.5 * _cellSize to center the object within the grid cell
        float worldX = gridPosition.x * _cellSize + _cellSize * 0.5f;
        float worldZ = gridPosition.z * _cellSize + _cellSize * 0.5f; // Using Z for depth

        // Add the grid's world origin to get the final world position
        return new Vector3(worldX, 0, worldZ) + _gridOriginWorldPosition;
    }

    /// <summary>
    /// Retrieves the GridTile data for a specific grid position.
    /// </summary>
    /// <param name="gridPosition">The grid coordinate to query.</param>
    /// <returns>The GridTile object if it exists and is stored, otherwise null.</returns>
    public GridTile GetGridTile(Vector3Int gridPosition)
    {
        _gridData.TryGetValue(gridPosition, out GridTile tile);
        return tile; // Will return null if the key (gridPosition) is not found
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Initializes the internal grid data structure.
    /// For a sparse grid (using Dictionary), this simply creates the dictionary.
    /// For a dense grid (e.g., using a 2D array), this would pre-populate all cells.
    /// </summary>
    private void InitializeGrid()
    {
        _gridData = new Dictionary<Vector3Int, GridTile>();
        // Tiles are only added to _gridData when a building occupies them,
        // or if explicitly queried and a new tile needs to be created.
    }

    /// <summary>
    /// Handles mouse input for building placement and removal.
    /// </summary>
    private void HandleBuildingPlacementInput()
    {
        // If no building is selected, there's no ghost or placement logic to run.
        if (_selectedBuildingAsset == null)
        {
            DestroyGhostBuilding(); // Ensure ghost is hidden if we exit placement mode
            return;
        }

        // Raycast from the mouse position into the game world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check if the ray hits the designated placement layer (e.g., the ground plane)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _placementLayer))
        {
            Vector3 mouseWorldPosition = hit.point;
            Vector3Int gridPos = GetGridPositionFromWorld(mouseWorldPosition);

            // Update the ghost building's position and visual feedback (material)
            UpdateGhostBuildingPositionAndMaterial(gridPos);

            // Left-click (Mouse0) to place the selected building
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBuilding(gridPos);
            }
        }
        else
        {
            // If the mouse is not over the placement layer, hide the ghost building
            if (_ghostBuildingInstance != null)
            {
                _ghostBuildingInstance.SetActive(false);
            }
        }

        // Right-click (Mouse1) to cancel placement mode OR remove an existing building
        if (Input.GetMouseButtonDown(1))
        {
            if (_selectedBuildingAsset != null)
            {
                DeselectBuilding(); // If in placement mode, cancel it
            }
            else
            {
                // If not in placement mode, try to remove a building at the clicked spot
                RaycastHit removeHit;
                if (Physics.Raycast(ray, out removeHit, Mathf.Infinity, _placementLayer))
                {
                    Vector3Int removeGridPos = GetGridPositionFromWorld(removeHit.point);
                    
                    // Find the building at this spot. We need its origin for RemoveBuilding.
                    // If the clicked tile is occupied, its OccupyingBuilding refers to the PlacedBuilding instance.
                    // We then use that instance's GridOrigin to ensure we remove the whole building correctly.
                    if (_gridData.TryGetValue(removeGridPos, out GridTile tileToClear) && tileToClear.IsOccupied && tileToClear.OccupyingBuilding != null)
                    {
                        RemoveBuilding(tileToClear.OccupyingBuilding.GridOrigin);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the position of the ghost building and changes its material
    /// based on whether the current grid position is valid for placement.
    /// </summary>
    /// <param name="gridPos">The current grid coordinate under the mouse.</param>
    private void UpdateGhostBuildingPositionAndMaterial(Vector3Int gridPos)
    {
        if (_ghostBuildingInstance == null) return;

        _ghostBuildingInstance.SetActive(true); // Ensure ghost is visible

        // Position the ghost building at the world center of the potential grid cell
        Vector3 worldPos = GetWorldPositionFromGrid(gridPos);
        _ghostBuildingInstance.transform.position = worldPos + new Vector3(0, _placementHeightOffset, 0);

        // Check if placement is valid at this position and set the ghost's material accordingly
        bool canPlace = CanPlaceBuilding(_selectedBuildingAsset, gridPos);
        SetGhostMaterial(canPlace ? _validPlacementMaterial : _invalidPlacementMaterial);
    }

    /// <summary>
    /// Applies a material to all renderers of the ghost building.
    /// </summary>
    /// <param name="mat">The material to apply (e.g., valid/invalid material).</param>
    private void SetGhostMaterial(Material mat)
    {
        if (_ghostBuildingRenderers == null) return;

        foreach (Renderer rend in _ghostBuildingRenderers)
        {
            rend.material = mat;
        }
    }

    /// <summary>
    /// Destroys the ghost building instance and clears its references.
    /// </summary>
    private void DestroyGhostBuilding()
    {
        if (_ghostBuildingInstance != null)
        {
            Destroy(_ghostBuildingInstance);
            _ghostBuildingInstance = null;
            _ghostBuildingRenderers = null;
        }
    }

    // --- Gizmos for Visualization (Editor Only) ---
    // These methods help visualize the grid and occupied cells in the Unity editor.

    private void OnDrawGizmos()
    {
        // Only draw gizmos if the grid data has been initialized
        if (_gridData == null || _cellSize <= 0) return;

        // Draw the main grid lines
        Gizmos.color = Color.grey;
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int z = 0; z < _gridSize.y; z++)
            {
                Vector3Int gridCoord = new Vector3Int(x, 0, z);
                Vector3 worldPos = GetWorldPositionFromGrid(gridCoord);
                Gizmos.DrawWireCube(worldPos, new Vector3(_cellSize, 0.01f, _cellSize)); // Draw a flat wire cube

                // Highlight occupied tiles in red
                if (_gridData.TryGetValue(gridCoord, out GridTile tile) && tile.IsOccupied)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(worldPos + Vector3.up * 0.1f, new Vector3(_cellSize * 0.8f, 0.05f, _cellSize * 0.8f));
                    Gizmos.color = Color.grey; // Reset color for the next wireframe
                }
            }
        }

        // Draw a selection highlight for the ghost building
        if (_selectedBuildingAsset != null && _ghostBuildingInstance != null)
        {
            // Determine color based on placement validity
            Gizmos.color = CanPlaceBuilding(_selectedBuildingAsset, GetGridPositionFromWorld(_ghostBuildingInstance.transform.position)) ? Color.green : Color.red;
            
            Vector3 ghostWorldPos = _ghostBuildingInstance.transform.position;
            // Adjust back for the height offset used during rendering to draw gizmo at base
            ghostWorldPos.y -= _placementHeightOffset; 
            
            // Draw a cube representing the ghost building's footprint
            Gizmos.DrawCube(ghostWorldPos, new Vector3(_selectedBuildingAsset.size.x * _cellSize, 0.1f, _selectedBuildingAsset.size.y * _cellSize));
        }
    }
}
```

---

**5. `BuildingSelector.cs` (Example UI/Input for Testing)**

This simple script provides basic keyboard input to select different `BuildingAsset` ScriptableObjects for placement. You would typically replace this with a more robust UI system.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// BuildingSelector is an example script to demonstrate how to interact with the GridBuildingSystem.
/// It allows selecting different building types using number keys and cancelling placement.
/// In a real game, this would be integrated with a more sophisticated UI system.
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    [Header("Available Buildings")]
    // List of all BuildingAsset ScriptableObjects the player can choose from.
    [SerializeField] private List<BuildingAsset> _availableBuildings;
    private int _selectedBuildingIndex = -1; // Index of the currently selected building (-1 means none)

    void Update()
    {
        // --- Input for Selecting Buildings ---
        // Press '1', '2', etc., to select a building from the list.
        // This is a simple placeholder; a real UI would use buttons.
        if (Input.GetKeyDown(KeyCode.Alpha1) && _availableBuildings.Count > 0)
        {
            SelectBuilding(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && _availableBuildings.Count > 1)
        {
            SelectBuilding(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && _availableBuildings.Count > 2)
        {
            SelectBuilding(2);
        }
        // Add more 'Alpha' key checks for more building types as needed.

        // --- Input for Cancelling Placement ---
        // Press 'C' to clear the current building selection and exit placement mode.
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (GridBuildingSystem.Instance != null)
            {
                GridBuildingSystem.Instance.DeselectBuilding();
                _selectedBuildingIndex = -1;
                Debug.Log("BuildingSelector: Placement mode cancelled.");
            }
            else
            {
                Debug.LogError("BuildingSelector: GridBuildingSystem.Instance not found!");
            }
        }
    }

    /// <summary>
    /// Selects a building by its index in the _availableBuildings list.
    /// This tells the GridBuildingSystem which building to prepare for placement.
    /// </summary>
    /// <param name="index">The index of the building asset in the _availableBuildings list.</param>
    private void SelectBuilding(int index)
    {
        if (GridBuildingSystem.Instance == null)
        {
            Debug.LogError("BuildingSelector: GridBuildingSystem.Instance not found! Make sure GridBuildingSystem is in the scene.");
            return;
        }

        if (index >= 0 && index < _availableBuildings.Count)
        {
            _selectedBuildingIndex = index;
            BuildingAsset selectedAsset = _availableBuildings[_selectedBuildingIndex];
            GridBuildingSystem.Instance.SelectBuildingToPlace(selectedAsset);
            Debug.Log($"BuildingSelector: Selected for placement: {selectedAsset.buildingName}");
        }
        else
        {
            Debug.LogWarning("BuildingSelector: Invalid building selection index.");
        }
    }
}
```

---

### **How to Use in a Unity Project (Step-by-Step Guide):**

1.  **Create C# Scripts:**
    *   Create a new C# script named `BuildingAsset.cs` and paste the `BuildingAsset` code.
    *   Create a new C# script named `PlacedBuilding.cs` and paste the `PlacedBuilding` code.
    *   Create a new C# script named `GridTile.cs` and paste the `GridTile` code.
    *   Create a new C# script named `GridBuildingSystem.cs` and paste the `GridBuildingSystem` code.
    *   Create a new C# script named `BuildingSelector.cs` and paste the `BuildingSelector` code.

2.  **Create Materials for Placement Feedback:**
    *   In your Project window, Right-click -> Create -> Material.
    *   Name one `ValidPlacementMaterial`. In its Inspector, set its **Rendering Mode** to `Fade` or `Transparent`. Set its **Albedo** color to a translucent green (e.g., RGBA: `0, 1, 0, 0.5`).
    *   Name the other `InvalidPlacementMaterial`. Set its **Rendering Mode** to `Fade` or `Transparent`. Set its **Albedo** color to a translucent red (e.g., RGBA: `1, 0, 0, 0.5`).

3.  **Prepare the Scene:**
    *   **Ground Plane:** Create a 3D Plane (GameObject -> 3D Object -> Plane). This will be the surface for building. Scale it if necessary (e.g., Scale X: 5, Z: 5 for a larger area).
    *   **Placement Layer:** Select the Plane. In the Inspector, click the "Layer" dropdown (top right), then "Add Layer...". Create a new layer, e.g., "Ground". Assign this "Ground" layer to your Plane.
    *   **Main Camera:** Position your Main Camera to look down at the plane (e.g., Position: `X:25, Y:30, Z:25`, Rotation: `X:45, Y:-45, Z:0`).

4.  **Set up `GridBuildingSystem` GameObject:**
    *   Create an Empty GameObject (GameObject -> Create Empty). Name it `GridBuildingManager`.
    *   Attach the `GridBuildingSystem.cs` script to `GridBuildingManager`.
    *   In the Inspector for `GridBuildingManager`:
        *   **Grid Size:** Set to `X:50, Y:50` (or match your plane size, e.g., 50x50 units for a 5x5 scaled plane with default 1 unit cell size).
        *   **Cell Size:** `1.0` (default).
        *   **Grid Origin World Position:** `(0,0,0)` (if your plane's center is at 0,0,0, its bottom-left grid corner will be at -25, -25 in world space for a 50x50 grid with 1 unit cell size, so `_gridOriginWorldPosition` could be set to `(-25, 0, -25)` or the plane's world position if using a single plane. For simplicity, we assume `(0,0,0)` and raycast takes care of it.)
        *   **Placement Layer:** Select `Ground` from the dropdown.
        *   **Valid Placement Material:** Drag your `ValidPlacementMaterial` here.
        *   **Invalid Placement Material:** Drag your `InvalidPlacementMaterial` here.

5.  **Create Building Prefabs and `BuildingAsset` ScriptableObjects:**
    *   Create a new folder in Project window, e.g., `Assets/Buildings`.
    *   **Building Prefab 1 (e.g., Small House):**
        *   Create a 3D Cube (GameObject -> 3D Object -> Cube). Scale it to `(1,1,1)`.
        *   Drag this Cube from the Hierarchy into `Assets/Buildings` to create a prefab. Rename it, e.g., `SmallHouse_Prefab`.
        *   **IMPORTANT:** Select the `SmallHouse_Prefab` in the Project window. In its Inspector, click "Add Component" and add the `PlacedBuilding` script. This script *must* be on the prefab for the system to work.
    *   **Building Asset 1 (for Small House):**
        *   In `Assets/Buildings`, Right-click -> Create -> Building -> Building Asset.
        *   Name it `BuildingAsset_SmallHouse`.
        *   In its Inspector:
            *   **Building Name:** `Small House`
            *   **Prefab:** Drag `SmallHouse_Prefab` into this slot.
            *   **Size:** `X:1, Y:1` (as it's a 1x1 grid unit building).
    *   **Building Prefab 2 (e.g., Large Building):**
        *   Create another 3D Cube. Scale it to `(2,1,2)`.
        *   Drag it to `Assets/Buildings` to make a prefab. Rename it `LargeBuilding_Prefab`.
        *   Add the `PlacedBuilding` script to this prefab as well.
    *   **Building Asset 2 (for Large Building):**
        *   Right-click -> Create -> Building -> Building Asset.
        *   Name it `BuildingAsset_LargeBuilding`.
        *   In its Inspector:
            *   **Building Name:** `Large Building`
            *   **Prefab:** Drag `LargeBuilding_Prefab` into this slot.
            *   **Size:** `X:2, Y:2` (as it's a 2x2 grid unit building).

6.  **Set up `BuildingSelector` GameObject:**
    *   Create an Empty GameObject. Name it `GameManager`.
    *   Attach the `BuildingSelector.cs` script to `GameManager`.
    *   In the Inspector for `GameManager`:
        *   Expand `_Available Buildings`.
        *   Set its `Size` to 2 (or how many `BuildingAsset`s you created).
        *   Drag `BuildingAsset_SmallHouse` to Element 0.
        *   Drag `BuildingAsset_LargeBuilding` to Element 1.

7.  **Run the Scene:**
    *   Press Play.
    *   Press `1` on your keyboard to select the `Small House`. You should see a translucent green or red ghost building under your mouse cursor.
    *   Move your mouse. The ghost changes color based on valid placement.
    *   Left-click to place the building. The ghost disappears.
    *   Press `2` to select the `Large Building` and try placing it. Notice it takes up a 2x2 area.
    *   Right-click on an empty spot while in placement mode to cancel placement.
    *   Right-click on an *already placed* building to remove it.
    *   Press `C` to cancel placement mode (useful if you selected a building but don't want to place anything).
    *   Observe the Gizmos in the Scene view (when not playing) to see the grid layout and occupied cells.

This comprehensive example provides a fully functional, commented, and practical grid-based building system for Unity, adhering to design patterns and best practices.