// Unity Design Pattern Example: HeatmapSystem
// This script demonstrates the HeatmapSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The HeatmapSystem design pattern focuses on visualizing data intensity over a spatial area. In Unity, this often means tracking events (player movement, enemy deaths, item pickups, damage zones) across a game world and displaying their frequency or magnitude using a color gradient.

This example provides a complete, practical `HeatmapSystem` script for Unity.

**Key Concepts Demonstrated:**

1.  **Grid-based Data Storage:** Uses a 2D array to represent the game world as a grid, storing an intensity value for each cell.
2.  **World-to-Grid Mapping:** Converts `Vector3` world coordinates into `int` grid coordinates.
3.  **Runtime Texture Generation:** Dynamically creates and updates a `Texture2D` based on the grid data.
4.  **Visualization:** Applies the generated texture to a world-space quad (`MeshRenderer`) to display the heatmap.
5.  **Color Gradient:** Maps intensity values to a customizable color range (e.g., blue for low, red for high).
6.  **Modular Design:** The `HeatmapSystem` manages the data and visualization, while a separate script (like `HeatmapPlayerTracker` in the example usage) provides the data points.

---

### **1. `HeatmapSystem.cs`**

This is the core script. It manages the heatmap data, generates the visualization texture, and applies it to a quad in the scene.

```csharp
using UnityEngine;
using System.Collections;
using System.Linq; // For .Max()

/// <summary>
/// HeatmapSystem Design Pattern Implementation for Unity.
/// 
/// This system tracks "heat points" across a defined grid in world space
/// and visualizes their intensity by generating and updating a texture
/// applied to a quad mesh.
/// </summary>
/// <remarks>
/// How it works:
/// 1.  **Grid Definition:** You define a `gridWidth`, `gridHeight`, and `cellSize` in Unity units.
///     This creates a logical grid over a portion of your game world.
/// 2.  **Data Storage:** An `int[,]` array (`_heatGrid`) stores the intensity value for each cell.
///     When `AddHeatPoint` is called, it converts the world position to grid coordinates
///     and increments the value in the corresponding grid cell.
/// 3.  **Visualization:** A `Texture2D` (`_heatmapTexture`) is created at runtime.
///     Its pixels are colored based on the intensity values in `_heatGrid`.
///     The color is interpolated between `minHeatColor` and `maxHeatColor`.
/// 4.  **Display:** The generated `_heatmapTexture` is applied to a dynamically
///     generated quad mesh in the world, which is scaled to match the grid's dimensions.
/// 5.  **Dynamic Update:** The heatmap texture can be regenerated periodically
///     (`updateInterval`) or on demand (`UpdateHeatmapDisplay()`) to reflect changes in data.
/// 
/// Usage:
/// - Attach this script to an empty GameObject in your scene.
/// - Configure `gridWidth`, `gridHeight`, `cellSize`, and colors in the Inspector.
/// - Ensure the `HeatmapSystem` GameObject is positioned where you want the bottom-left
///   corner of your heatmap quad to be in world space.
/// - Call `AddHeatPoint(Vector3 worldPosition, int intensity)` from other scripts
///   (e.g., a player controller, enemy AI, item manager) to add data to the heatmap.
/// - The heatmap will automatically update its display at the specified interval.
/// </remarks>
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class HeatmapSystem : MonoBehaviour
{
    [Header("Heatmap Grid Settings")]
    [Tooltip("Width of the heatmap grid in cells.")]
    [SerializeField] private int gridWidth = 100;
    [Tooltip("Height of the heatmap grid in cells.")]
    [SerializeField] private int gridHeight = 100;
    [Tooltip("Size of each grid cell in Unity world units.")]
    [SerializeField] private float cellSize = 1.0f;

    [Header("Visualization Settings")]
    [Tooltip("Minimum color for low heat intensity.")]
    [SerializeField] private Color minHeatColor = new Color(0, 0, 1, 0.5f); // Blue, semi-transparent
    [Tooltip("Maximum color for high heat intensity.")]
    [SerializeField] private Color maxHeatColor = new Color(1, 0, 0, 0.75f); // Red, more opaque
    [Tooltip("How often the heatmap texture is regenerated and displayed (in seconds). Set to 0 for manual updates.")]
    [SerializeField] private float updateInterval = 1.0f;

    // Private internal state
    private int[,] _heatGrid;
    private Texture2D _heatmapTexture;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    // Cache to prevent recalculations
    private Vector3 _heatmapOrigin;
    private float _worldWidth;
    private float _worldHeight;

    private Coroutine _updateCoroutine;

    /// <summary>
    /// Gets the current intensity value at a specific world position.
    /// </summary>
    /// <param name="worldPosition">The world position to query.</param>
    /// <returns>The intensity value at the corresponding grid cell.</returns>
    public int GetHeatValue(Vector3 worldPosition)
    {
        Vector2Int gridCoords = WorldToGridCoordinates(worldPosition);
        if (IsValidGridCoordinate(gridCoords.x, gridCoords.y))
        {
            return _heatGrid[gridCoords.x, gridCoords.y];
        }
        return 0; // Or throw an error, depending on desired behavior
    }

    void Awake()
    {
        // Get or add required components
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();

        // Initialize grid and display elements
        InitializeHeatmap();
    }

    void Start()
    {
        // Initial display update
        UpdateHeatmapDisplay();

        // Start periodic updates if an interval is set
        if (updateInterval > 0)
        {
            _updateCoroutine = StartCoroutine(PeriodicUpdate());
        }
    }

    /// <summary>
    /// Initializes or re-initializes the heatmap system.
    /// This will reset all grid data and regenerate the display.
    /// </summary>
    public void InitializeHeatmap()
    {
        _heatmapOrigin = transform.position;
        _worldWidth = gridWidth * cellSize;
        _worldHeight = gridHeight * cellSize;

        // Initialize the internal heat grid
        _heatGrid = new int[gridWidth, gridHeight];
        ClearHeatmap(); // Sets all values to 0

        // Setup the Mesh (quad) for display
        GenerateHeatmapQuad();

        // Setup the Texture2D
        if (_heatmapTexture == null || _heatmapTexture.width != gridWidth || _heatmapTexture.height != gridHeight)
        {
            _heatmapTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
            _heatmapTexture.filterMode = FilterMode.Bilinear; // Smooth texture
            _heatmapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Apply a default material (Unlit/Color or Standard)
        // If you have a specific material, assign it in the Inspector.
        if (_meshRenderer.material == null || !_meshRenderer.material.name.Contains("HeatmapMaterial"))
        {
            // Create a new material or use a default one
            Material material = new Material(Shader.Find("Unlit/Transparent Color"));
            material.name = "HeatmapMaterial";
            material.color = Color.white; // This is the tint color, texture will provide actual color
            _meshRenderer.material = material;
        }

        // Assign the texture to the material
        _meshRenderer.material.mainTexture = _heatmapTexture;
    }

    /// <summary>
    /// Adds a heat point at a specific world position, increasing the intensity
    /// of the corresponding grid cell.
    /// </summary>
    /// <param name="worldPosition">The world coordinates of the heat point.</param>
    /// <param name="intensity">The amount of heat to add to the cell (defaults to 1).</param>
    public void AddHeatPoint(Vector3 worldPosition, int intensity = 1)
    {
        Vector2Int gridCoords = WorldToGridCoordinates(worldPosition);
        IncrementHeatGridCell(gridCoords.x, gridCoords.y, intensity);
    }

    /// <summary>
    /// Resets all heat values in the grid to zero and updates the display.
    /// </summary>
    public void ClearHeatmap()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _heatGrid[x, y] = 0;
            }
        }
        UpdateHeatmapDisplay(); // Immediately reflect the cleared state
    }

    /// <summary>
    /// Manually triggers the regeneration and display update of the heatmap.
    /// </summary>
    public void UpdateHeatmapDisplay()
    {
        GenerateHeatmapTexture();
    }

    // --- Private Helper Methods ---

    private IEnumerator PeriodicUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            UpdateHeatmapDisplay();
        }
    }

    /// <summary>
    /// Generates a quad mesh programmatically to display the heatmap.
    /// </summary>
    private void GenerateHeatmapQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HeatmapQuad";

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        // Vertices for a quad in the XZ plane (Y up)
        // The quad's bottom-left corner will be at the GameObject's transform.position
        // (0,0) (world space origin of heatmap)
        // (width, 0)
        // (0, height)
        // (width, height)
        vertices[0] = Vector3.zero;                                        // Bottom-left
        vertices[1] = new Vector3(_worldWidth, 0, 0);                      // Bottom-right
        vertices[2] = new Vector3(0, 0, _worldHeight);                     // Top-left
        vertices[3] = new Vector3(_worldWidth, 0, _worldHeight);           // Top-right

        // UV coordinates (standard for a quad)
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        // Triangles to form the quad
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // Good practice for lighting
        mesh.RecalculateBounds();

        _meshFilter.mesh = mesh;
    }

    /// <summary>
    /// Converts a world position to grid coordinates (0-indexed).
    /// </summary>
    private Vector2Int WorldToGridCoordinates(Vector3 worldPosition)
    {
        // Calculate offset from the heatmap's origin
        Vector3 localPosition = worldPosition - _heatmapOrigin;

        // Convert to grid coordinates
        int x = Mathf.FloorToInt(localPosition.x / cellSize);
        int y = Mathf.FloorToInt(localPosition.z / cellSize); // Assuming heatmap is on XZ plane

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Increments the heat value of a specific grid cell, clamping coordinates.
    /// </summary>
    private void IncrementHeatGridCell(int x, int y, int intensity)
    {
        if (IsValidGridCoordinate(x, y))
        {
            _heatGrid[x, y] += intensity;
        }
    }

    /// <summary>
    /// Checks if the given grid coordinates are within the valid range.
    /// </summary>
    private bool IsValidGridCoordinate(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// Generates the heatmap texture based on the current heat grid data.
    /// </summary>
    private void GenerateHeatmapTexture()
    {
        if (_heatmapTexture == null || _heatGrid == null) return;

        // Find the maximum heat value to normalize colors
        int maxHeatValue = 0;
        if (gridWidth > 0 && gridHeight > 0)
        {
            // Using Linq for simplicity, but a manual loop is also fine
            maxHeatValue = _heatGrid.Cast<int>().Max();
        }
        
        // Prevent division by zero if all values are 0
        if (maxHeatValue == 0)
        {
            maxHeatValue = 1; // Treat as 1 to still show min color, or clear it completely
        }

        // Loop through each cell and set the corresponding pixel color
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float normalizedValue = (float)_heatGrid[x, y] / maxHeatValue;
                Color pixelColor = Color.Lerp(minHeatColor, maxHeatColor, normalizedValue);
                _heatmapTexture.SetPixel(x, y, pixelColor);
            }
        }

        _heatmapTexture.Apply(); // Apply all changes to the texture
        
        // Ensure the material is using the updated texture
        if (_meshRenderer.material.mainTexture != _heatmapTexture)
        {
             _meshRenderer.material.mainTexture = _heatmapTexture;
        }
    }

    void OnValidate()
    {
        // Ensure grid dimensions are positive
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.1f, cellSize); // Cell size must be positive
        updateInterval = Mathf.Max(0f, updateInterval); // Update interval cannot be negative

        // If the application is running, and parameters change, reinitialize
        // This is primarily for editor changes during play mode.
        if (Application.isPlaying && _heatGrid != null &&
            (_heatGrid.GetLength(0) != gridWidth || _heatGrid.GetLength(1) != gridHeight))
        {
            InitializeHeatmap();
            UpdateHeatmapDisplay();
        }
    }

    void OnDestroy()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
        }
        // Clean up generated mesh and texture if necessary
        if (_meshFilter != null && _meshFilter.mesh != null && _meshFilter.mesh.name == "HeatmapQuad")
        {
            // Destroy the generated mesh
            Destroy(_meshFilter.mesh);
        }
        if (_heatmapTexture != null)
        {
            // Destroy the generated texture
            Destroy(_heatmapTexture);
        }
    }
}
```

---

### **2. Example Usage: `HeatmapPlayerTracker.cs`**

This script demonstrates how another component would interact with the `HeatmapSystem` to provide data. Here, it simulates player movement and adds heat points to the system.

```csharp
using UnityEngine;

/// <summary>
/// Example usage script to demonstrate how to interact with HeatmapSystem.
/// This script simulates a 'player' moving around and adds heat points
/// to the HeatmapSystem at regular intervals.
/// </summary>
[RequireComponent(typeof(HeatmapSystem))]
public class HeatmapPlayerTracker : MonoBehaviour
{
    [Tooltip("Reference to the HeatmapSystem script.")]
    [SerializeField] private HeatmapSystem heatmapSystem; // Will be auto-assigned by RequireComponent
    
    [Header("Player Tracking Settings")]
    [Tooltip("Interval (in seconds) at which the player's position is added to the heatmap.")]
    [SerializeField] private float trackingInterval = 0.5f;
    [Tooltip("The intensity value to add for each tracked point.")]
    [SerializeField] private int trackingIntensity = 1;
    [Tooltip("Speed at which the simulated player moves.")]
    [SerializeField] private float playerSpeed = 5f;
    [Tooltip("Radius within which the player is constrained.")]
    [SerializeField] private float movementRadius = 40f;

    private float _timeSinceLastTrack;
    private Vector3 _targetPosition;

    void Awake()
    {
        // Get the HeatmapSystem component on the same GameObject
        // This is guaranteed to exist due to [RequireComponent]
        if (heatmapSystem == null)
        {
            heatmapSystem = GetComponent<HeatmapSystem>();
        }
        _targetPosition = GetRandomPointInRadius(transform.position, movementRadius);
    }

    void Update()
    {
        // Simulate player movement
        SimulatePlayerMovement();

        // Add heat point at intervals
        _timeSinceLastTrack += Time.deltaTime;
        if (_timeSinceLastTrack >= trackingInterval)
        {
            heatmapSystem.AddHeatPoint(transform.position, trackingIntensity);
            _timeSinceLastTrack = 0f;
        }

        // Example: Clear heatmap on key press
        if (Input.GetKeyDown(KeyCode.R))
        {
            heatmapSystem.ClearHeatmap();
            Debug.Log("Heatmap Cleared!");
        }
    }

    /// <summary>
    /// Simulates basic player movement towards a random target within a radius.
    /// </summary>
    private void SimulatePlayerMovement()
    {
        // Move towards the target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, playerSpeed * Time.deltaTime);

        // If reached target, find a new random target
        if (Vector3.Distance(transform.position, _targetPosition) < 0.5f)
        {
            _targetPosition = GetRandomPointInRadius(Vector3.zero, movementRadius);
        }
    }

    /// <summary>
    /// Gets a random point within a specified radius from an origin.
    /// </summary>
    private Vector3 GetRandomPointInRadius(Vector3 origin, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return origin + new Vector3(randomCircle.x, 0, randomCircle.y); // Assuming XZ plane
    }

    void OnDrawGizmosSelected()
    {
        // Draw the movement boundary
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Vector3.zero, movementRadius);

        // Draw the current target position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetPosition, 1f);
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., named "HeatmapManager").
2.  **Attach `HeatmapSystem.cs`:** Drag and drop the `HeatmapSystem.cs` script onto the "HeatmapManager" GameObject.
3.  **Attach `HeatmapPlayerTracker.cs`:** Drag and drop the `HeatmapPlayerTracker.cs` script onto the *same* "HeatmapManager" GameObject.
4.  **Position the HeatmapManager:** The `transform.position` of the "HeatmapManager" GameObject will be the **bottom-left corner** of your heatmap quad in world space. For example, if you set its position to `(0, 0, 0)`, the heatmap will span from `(0,0,0)` to `(gridWidth * cellSize, 0, gridHeight * cellSize)`.
5.  **Configure in Inspector:**
    *   **Heatmap System:**
        *   `Grid Width`, `Grid Height`: E.g., `100` each.
        *   `Cell Size`: E.g., `1.0` (meaning each grid cell is 1x1 Unity unit).
        *   `Min Heat Color`, `Max Heat Color`: Adjust to your preference. `Min Heat Color` should typically be more transparent to show the scene underneath.
        *   `Update Interval`: E.g., `0.5` seconds (how often the heatmap visualization refreshes).
    *   **Heatmap Player Tracker:**
        *   `Tracking Interval`: E.g., `0.1` seconds (how often the simulated player adds a heat point).
        *   `Tracking Intensity`: E.g., `1`.
        *   `Player Speed`: E.g., `10`.
        *   `Movement Radius`: E.g., `40` (the simulated player will move within a 40-unit radius from `Vector3.zero`).
6.  **Run the Scene:** Press Play. You will see a transparent quad appear. As the simulated player moves around, areas it visits more frequently will gradually change color, showing the heatmap effect. Press 'R' to clear the heatmap data.

This setup creates a fully functional heatmap system that is easy to integrate and extend for various data visualization needs in your Unity projects.