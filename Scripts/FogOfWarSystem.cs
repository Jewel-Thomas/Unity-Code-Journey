// Unity Design Pattern Example: FogOfWarSystem
// This script demonstrates the FogOfWarSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Fog of War System is a classic design pattern in strategy games, where only areas currently observed by the player's units are fully visible. Other areas might be "shrouded" (terrain visible, but no enemy units/details), or "hidden" (completely black/unexplored). This pattern aims to manage visibility efficiently and visually represent it in the game world.

Here's a complete, practical C# Unity example demonstrating the FogOfWarSystem pattern.

---

**Project Setup in Unity:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a Folder Structure:**
    *   `Assets/Scripts/FogOfWar`
    *   `Assets/Materials/FogOfWar`
3.  **Create Materials:**
    *   In `Assets/Materials/FogOfWar`, create three new Materials:
        *   `HiddenMaterial`: Set its shader to `Standard` or `Universal Render Pipeline/Lit` if using URP. Change its Base Map color to black, and set its Render Mode to `Fade` or `Transparent` to ensure alpha works.
        *   `ShroudedMaterial`: Base Map color to a dark grey (e.g., R: 0.2, G: 0.2, B: 0.2), Render Mode `Fade` or `Transparent`.
        *   `VisibleMaterial`: Base Map color to white, Render Mode `Fade` or `Transparent`. (This will be the default material for objects when visible).
    *   In `Assets/Materials/FogOfWar`, create one more Material for the `FogPlane`:
        *   `FogPlaneMaterial`: Set its shader to `Unlit/Texture`. Drag and drop this onto the `Fog Plane` GameObject later. (Alternatively, you can use a custom shader for more control over the fog appearance, but Unlit/Texture is simplest for this example).

4.  **Create 3 C# Scripts:**
    *   `Assets/Scripts/FogOfWar/FogOfWarSystem.cs`
    *   `Assets/Scripts/FogOfWar/VisionSource.cs`
    *   `Assets/Scripts/FogOfWar/FogOfWarAffectedObject.cs`

---

**1. `FogOfWarSystem.cs` (The Manager)**

This script is the core of the system. It manages the visibility grid, calculates the state of each cell, and updates a visual fog texture.

```csharp
using UnityEngine;
using System.Collections.Generic; // For HashSet
using System.Linq; // For LINQ operations if needed, though not heavily used here

/// <summary>
/// Defines the visibility state of a grid cell.
/// - Hidden: Completely obscured, never explored or currently not observed.
/// - Shrouded: Explored but not currently observed. Terrain visible, but no dynamic objects (enemies, resources).
/// - Visible: Currently observed by a vision source. Everything is visible.
/// </summary>
public enum FogState
{
    Hidden,
    Shrouded,
    Visible
}

/// <summary>
/// The central manager for the Fog of War system.
/// Implements a Singleton pattern to be easily accessible throughout the game.
/// Manages the visibility grid, processes vision sources, and updates the visual fog.
/// </summary>
public class FogOfWarSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static FogOfWarSystem Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Width of the Fog of War grid in cells.")]
    public int gridWidth = 100;
    [Tooltip("Height of the Fog of War grid in cells.")]
    public int gridHeight = 100;
    [Tooltip("Size of each grid cell in Unity world units.")]
    public float cellSize = 1f;
    [Tooltip("World position for the bottom-left corner of the grid.")]
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Fog Texture Settings")]
    [Tooltip("Resolution of the texture used to visualize the fog.")]
    public int fogTextureResolution = 256;
    [Tooltip("The plane or quad in the world that will display the fog texture.")]
    public Renderer fogPlaneRenderer;
    [Tooltip("Material used for objects that are Hidden.")]
    public Material hiddenMaterial;
    [Tooltip("Material used for objects that are Shrouded.")]
    public Material shroudedMaterial;

    // --- Internal Data ---
    // The main grid storing the current visibility state of each cell.
    private FogState[,] visibilityGrid;
    // A separate grid to track if a cell has ever been explored (for Shrouded state persistence).
    private bool[,] exploredGrid;
    // Collection of all active VisionSources in the game.
    private HashSet<VisionSource> visionSources = new HashSet<VisionSource>();

    // Texture used to render the fog overlay.
    private Texture2D fogTexture;
    private Color[] fogPixels; // Cached pixel array for efficient texture updates

    private int lastUpdatedFrame = -1; // To prevent multiple updates in the same frame if called externally

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple FogOfWarSystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeFogOfWar();
    }

    private void Start()
    {
        // Initial update after all VisionSources might have registered in their Start/OnEnable.
        UpdateFog();
    }

    private void Update()
    {
        // For dynamic games, update every frame or on a timed interval.
        // For this example, we'll update every frame for simplicity.
        // In a real game, you might optimize this to update only when sources move.
        if (lastUpdatedFrame != Time.frameCount)
        {
            UpdateFog();
            lastUpdatedFrame = Time.frameCount;
        }
    }

    /// <summary>
    /// Initializes the Fog of War system, setting up grids and textures.
    /// </summary>
    private void InitializeFogOfWar()
    {
        // Validate settings
        if (gridWidth <= 0 || gridHeight <= 0 || cellSize <= 0 || fogTextureResolution <= 0)
        {
            Debug.LogError("FogOfWarSystem: Grid or texture resolution settings are invalid. Please check values.");
            enabled = false;
            return;
        }
        if (fogPlaneRenderer == null || fogPlaneRenderer.sharedMaterial == null)
        {
            Debug.LogError("FogOfWarSystem: Fog Plane Renderer or its Material is not assigned. Please assign it in the Inspector.");
            enabled = false;
            return;
        }
        if (hiddenMaterial == null || shroudedMaterial == null)
        {
            Debug.LogError("FogOfWarSystem: Hidden/Shrouded Materials are not assigned. Please assign them in the Inspector.");
            enabled = false;
            return;
        }

        visibilityGrid = new FogState[gridWidth, gridHeight];
        exploredGrid = new bool[gridWidth, gridHeight];
        fogTexture = new Texture2D(fogTextureResolution, fogTextureResolution, TextureFormat.R8, false);
        fogTexture.filterMode = FilterMode.Bilinear; // Smooth texture scaling
        fogTexture.wrapMode = TextureWrapMode.Clamp; // Prevent edge artifacts
        fogPixels = new Color[fogTextureResolution * fogTextureResolution];

        // Initially, everything is hidden and unexplored.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                visibilityGrid[x, y] = FogState.Hidden;
                exploredGrid[x, y] = false;
            }
        }

        // Assign the texture to the fog plane's material
        fogPlaneRenderer.sharedMaterial.mainTexture = fogTexture;

        // Set the fog plane's scale to match the grid size
        // Assuming the fogPlaneRenderer is a Quad or Plane with pivot at center and 1x1 scale default size
        // You might need to adjust this depending on your plane's default size/pivot.
        float totalGridWidth = gridWidth * cellSize;
        float totalGridHeight = gridHeight * cellSize;
        fogPlaneRenderer.transform.localScale = new Vector3(totalGridWidth, totalGridHeight, 1f);
        fogPlaneRenderer.transform.position = gridOrigin + new Vector3(totalGridWidth / 2f, 0f, totalGridHeight / 2f); // Centered over the grid
        fogPlaneRenderer.transform.rotation = Quaternion.Euler(90, 0, 0); // Assuming plane is flat on XZ
    }

    /// <summary>
    /// Registers a VisionSource with the system. Called by VisionSource.OnEnable().
    /// </summary>
    /// <param name="source">The VisionSource to register.</param>
    public void RegisterVisionSource(VisionSource source)
    {
        if (visionSources.Add(source))
        {
            // Debug.Log($"Registered VisionSource: {source.name}");
            // Trigger an update immediately to reflect the new source
            UpdateFog();
        }
    }

    /// <summary>
    /// Unregisters a VisionSource from the system. Called by VisionSource.OnDisable().
    /// </summary>
    /// <param name="source">The VisionSource to unregister.</param>
    public void UnregisterVisionSource(VisionSource source)
    {
        if (visionSources.Remove(source))
        {
            // Debug.Log($"Unregistered VisionSource: {source.name}");
            // Trigger an update immediately as a source has been removed
            UpdateFog();
        }
    }

    /// <summary>
    /// Main method to recalculate and update the Fog of War.
    /// This is called periodically or when significant changes occur (e.g., source moves, added/removed).
    /// </summary>
    public void UpdateFog()
    {
        // 1. Prepare temporary grids for current frame's vision calculation
        bool[,] currentFrameVisible = new bool[gridWidth, gridHeight];
        bool[,] currentFrameShrouded = new bool[gridWidth, gridHeight];

        // 2. Iterate through all active VisionSources to determine current frame's visibility
        foreach (VisionSource source in visionSources)
        {
            Vector2Int sourceGridPos = WorldToGrid(source.transform.position);

            // Calculate clear vision area
            MarkVisionArea(sourceGridPos, source.clearVisionRange, currentFrameVisible, true);

            // Calculate shroud vision area (if greater than clear vision)
            if (source.shroudVisionRange > source.clearVisionRange)
            {
                MarkVisionArea(sourceGridPos, source.shroudVisionRange, currentFrameShrouded, false);
            }
        }

        // 3. Update the main visibilityGrid and exploredGrid based on current frame's vision
        //    and previous exploration data.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (currentFrameVisible[x, y])
                {
                    visibilityGrid[x, y] = FogState.Visible;
                    exploredGrid[x, y] = true; // Mark as explored when it becomes visible
                }
                else if (currentFrameShrouded[x, y])
                {
                    visibilityGrid[x, y] = FogState.Shrouded;
                    // Note: Shrouded by current vision doesn't make it *freshly* explored,
                    // but it contributes to the 'exploredGrid' if it ever became Visible.
                    // This is for cases where shroud range extends beyond clear vision,
                    // and we want those areas to show terrain, not just turn black when source moves away.
                }
                else // Not currently observed
                {
                    if (exploredGrid[x, y])
                    {
                        visibilityGrid[x, y] = FogState.Shrouded; // Was explored, now shrouded
                    }
                    else
                    {
                        visibilityGrid[x, y] = FogState.Hidden; // Never explored
                    }
                }
            }
        }

        // 4. Update the visual fog texture based on the new visibilityGrid.
        UpdateFogTexture();
    }

    /// <summary>
    /// Helper method to mark cells within a vision range.
    /// Uses a square check for simplicity, then a circle distance check for accuracy.
    /// </summary>
    /// <param name="centerGridPos">The center of the vision area in grid coordinates.</param>
    /// <param name="range">The vision range in world units.</param>
    /// <param name="targetGrid">The boolean grid to mark (e.g., currentFrameVisible, currentFrameShrouded).</param>
    /// <param name="isClearVision">True if marking clear vision, false for shroud (affects how overlapping vision is handled).</param>
    private void MarkVisionArea(Vector2Int centerGridPos, float range, bool[,] targetGrid, bool isClearVision)
    {
        // Convert world range to grid units
        int gridRange = Mathf.CeilToInt(range / cellSize);
        float rangeSq = range * range; // Use squared distance for comparison optimization

        // Iterate a square region around the source's grid position
        for (int x = centerGridPos.x - gridRange; x <= centerGridPos.x + gridRange; x++)
        {
            for (int y = centerGridPos.y - gridRange; y <= centerGridPos.y + gridRange; y++)
            {
                // Clamp coordinates to grid boundaries
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;

                // Calculate world position of the cell center
                Vector3 cellWorldPos = GridToWorld(new Vector2Int(x, y));
                // Calculate distance from source to cell center
                float distSq = (new Vector3(centerGridPos.x * cellSize, 0, centerGridPos.y * cellSize) + gridOrigin - cellWorldPos).sqrMagnitude;
                // Note: The above distance calculation needs to be relative to the *source's* world position, not grid position.
                // Re-calculating using the source's actual world position.
                Vector3 sourceWorldPos = GridToWorld(centerGridPos); // Using the grid center for simplicity, or source.transform.position.y for proper 3D distance

                // Corrected distance calculation: from source's actual world position to the cell's world center
                distSq = (sourceWorldPos - cellWorldPos).sqrMagnitude;


                if (distSq <= rangeSq)
                {
                    if (isClearVision)
                    {
                        targetGrid[x, y] = true;
                    }
                    else
                    {
                        // If marking shroud, ensure it doesn't overwrite a clear vision already set for this frame.
                        // This logic is important if shroud range calculation is done after clear vision calculation for a source.
                        // For this implementation, currentFrameVisible is checked first in the main loop.
                        // So here, we just mark shroud.
                        targetGrid[x, y] = true;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Updates the `fogTexture` based on the `visibilityGrid`.
    /// Each pixel in the fog texture corresponds to a grid cell.
    /// </summary>
    private void UpdateFogTexture()
    {
        // Map grid cells to texture pixels.
        // The texture resolution might be different from the grid dimensions.
        // We'll average the visibility for corresponding texture blocks.

        float cellToTexRatioX = (float)gridWidth / fogTextureResolution;
        float cellToTexRatioY = (float)gridHeight / fogTextureResolution;

        for (int texY = 0; texY < fogTextureResolution; texY++)
        {
            for (int texX = 0; texX < fogTextureResolution; texX++)
            {
                int gridX = Mathf.FloorToInt(texX * cellToTexRatioX);
                int gridY = Mathf.FloorToInt(texY * cellToTexRatioY);

                Color pixelColor;
                switch (visibilityGrid[gridX, gridY])
                {
                    case FogState.Visible:
                        pixelColor = new Color(0, 0, 0, 0); // Transparent black for visible
                        break;
                    case FogState.Shrouded:
                        pixelColor = new Color(0.2f, 0.2f, 0.2f, 0.7f); // Dark semi-transparent for shrouded
                        break;
                    case FogState.Hidden:
                    default:
                        pixelColor = new Color(0, 0, 0, 1); // Opaque black for hidden
                        break;
                }
                fogPixels[texY * fogTextureResolution + texX] = pixelColor;
            }
        }

        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
    }

    // --- Helper Methods ---

    /// <summary>
    /// Converts a world position to its corresponding grid coordinates.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <returns>A Vector2Int representing grid (x, y) coordinates.</returns>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int gridX = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int gridY = Mathf.FloorToInt((worldPos.z - gridOrigin.z) / cellSize); // Assuming Z-up for grid Y

        return new Vector2Int(
            Mathf.Clamp(gridX, 0, gridWidth - 1),
            Mathf.Clamp(gridY, 0, gridHeight - 1)
        );
    }

    /// <summary>
    /// Converts grid coordinates to the world position of the cell's center.
    /// </summary>
    /// <param name="gridPos">Grid coordinates.</param>
    /// <returns>World position of the cell center.</returns>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float worldX = gridOrigin.x + (gridPos.x * cellSize) + (cellSize / 2f);
        float worldZ = gridOrigin.z + (gridPos.y * cellSize) + (cellSize / 2f);
        return new Vector3(worldX, gridOrigin.y, worldZ); // Assuming grid is on the XZ plane
    }

    /// <summary>
    /// Gets the current visibility state of a specific world position.
    /// </summary>
    /// <param name="worldPos">The world position to check.</param>
    /// <returns>The FogState at that position.</returns>
    public FogState GetFogState(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGrid(worldPos);
        return visibilityGrid[gridPos.x, gridPos.y];
    }

    // --- Editor Gizmos for Visualization ---
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && visibilityGrid != null)
        {
            // Draw grid bounds
            Vector3 startCorner = gridOrigin;
            Vector3 endCorner = gridOrigin + new Vector3(gridWidth * cellSize, 0, gridHeight * cellSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startCorner, new Vector3(endCorner.x, startCorner.y, startCorner.z));
            Gizmos.DrawLine(startCorner, new Vector3(startCorner.x, startCorner.y, endCorner.z));
            Gizmos.DrawLine(new Vector3(endCorner.x, startCorner.y, startCorner.z), endCorner);
            Gizmos.DrawLine(new Vector3(startCorner.x, startCorner.y, endCorner.z), endCorner);


            // Visualize grid states (optional, can be performance heavy for large grids)
            // if (cellSize > 0.5f) // Only draw for larger cells to avoid clutter
            // {
            //     for (int x = 0; x < gridWidth; x++)
            //     {
            //         for (int y = 0; y < gridHeight; y++)
            //         {
            //             Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
            //             switch (visibilityGrid[x, y])
            //             {
            //                 case FogState.Visible:
            //                     Gizmos.color = new Color(0, 1, 0, 0.2f); // Green, semi-transparent
            //                     break;
            //                 case FogState.Shrouded:
            //                     Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // Orange, semi-transparent
            //                     break;
            //                 case FogState.Hidden:
            //                 default:
            //                     Gizmos.color = new Color(0, 0, 0, 0.5f); // Black, semi-transparent
            //                     break;
            //             }
            //             Gizmos.DrawCube(cellCenter + Vector3.up * 0.1f, new Vector3(cellSize, 0.2f, cellSize));
            //         }
            //     }
            // }
        }
    }
}
```

---

**2. `VisionSource.cs` (The Vision Generator)**

This component is added to any GameObject that should generate vision (e.g., player units, buildings).

```csharp
using UnityEngine;

/// <summary>
/// Represents an entity that generates visibility for the FogOfWarSystem.
/// Attach this script to any GameObject that should be a 'vision source'.
/// </summary>
public class VisionSource : MonoBehaviour
{
    [Tooltip("The radius (in world units) within which this source provides clear vision.")]
    public float clearVisionRange = 5f;
    [Tooltip("The radius (in world units) within which this source reveals shrouded areas (terrain). Must be >= Clear Vision Range.")]
    public float shroudVisionRange = 8f;

    private void Awake()
    {
        // Ensure shroudVisionRange is always at least clearVisionRange
        if (shroudVisionRange < clearVisionRange)
        {
            Debug.LogWarning($"VisionSource '{name}': Shroud Vision Range ({shroudVisionRange}) cannot be less than Clear Vision Range ({clearVisionRange}). Adjusting shroudVisionRange to match clearVisionRange.", this);
            shroudVisionRange = clearVisionRange;
        }
    }

    private void OnEnable()
    {
        // Register this source with the FogOfWarSystem when it becomes active.
        if (FogOfWarSystem.Instance != null)
        {
            FogOfWarSystem.Instance.RegisterVisionSource(this);
        }
        else
        {
            Debug.LogError("VisionSource: FogOfWarSystem.Instance is not found. Make sure the FogOfWarSystem GameObject is in the scene.", this);
        }
    }

    private void OnDisable()
    {
        // Unregister this source from the FogOfWarSystem when it becomes inactive.
        if (FogOfWarSystem.Instance != null)
        {
            FogOfWarSystem.Instance.UnregisterVisionSource(this);
        }
    }

    // Optional: Draw vision ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, clearVisionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shroudVisionRange);
    }
}
```

---

**3. `FogOfWarAffectedObject.cs` (The Responder)**

This component is added to any GameObject that should react to the Fog of War (e.g., enemy units, resource nodes, terrain tiles).

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This component makes a GameObject react to the Fog of War System.
/// It adjusts its rendering and collision based on the FogState of its position.
/// </summary>
public class FogOfWarAffectedObject : MonoBehaviour
{
    [Tooltip("Material to use when the object is fully visible.")]
    public Material visibleMaterial;
    [Tooltip("Material to use when the object is shrouded (explored but not currently seen).")]
    public Material shroudedMaterial;
    [Tooltip("Material to use when the object is hidden (unexplored/unseen).")]
    public Material hiddenMaterial;

    [Tooltip("If true, the object's colliders will be disabled when not visible.")]
    public bool disableCollidersWhenHidden = true;
    [Tooltip("If true, the object's renderers will be disabled when hidden.")]
    public bool disableRenderersWhenHidden = true;
    [Tooltip("If true, the object's renderers will be disabled when shrouded.")]
    public bool disableRenderersWhenShrouded = false; // Usually, terrain is visible when shrouded

    private List<Renderer> _renderers;
    private List<Collider> _colliders;
    private FogState _lastFogState = FogState.Hidden; // Cache last known state to optimize updates

    private void Awake()
    {
        // Get all renderers and colliders on this GameObject and its children
        _renderers = GetComponentsInChildren<Renderer>().ToList();
        _colliders = GetComponentsInChildren<Collider>().ToList();

        // If no specific materials are provided, try to use the ones from FogOfWarSystem
        if (hiddenMaterial == null && FogOfWarSystem.Instance != null)
            hiddenMaterial = FogOfWarSystem.Instance.hiddenMaterial;
        if (shroudedMaterial == null && FogOfWarSystem.Instance != null)
            shroudedMaterial = FogOfWarSystem.Instance.shroudedMaterial;
        // visibleMaterial usually comes from the object itself, but can be set if needed
    }

    private void Update()
    {
        if (FogOfWarSystem.Instance == null)
        {
            Debug.LogWarning("FogOfWarAffectedObject: FogOfWarSystem.Instance is not found. Object will not react to Fog of War.", this);
            return;
        }

        // Get current visibility state from the system
        FogState currentFogState = FogOfWarSystem.Instance.GetFogState(transform.position);

        // Only update if the state has changed
        if (currentFogState != _lastFogState)
        {
            ApplyVisibilityState(currentFogState);
            _lastFogState = currentFogState;
        }
    }

    /// <summary>
    /// Applies the visual and physical changes based on the given FogState.
    /// </summary>
    /// <param name="state">The new FogState for this object.</param>
    private void ApplyVisibilityState(FogState state)
    {
        switch (state)
        {
            case FogState.Visible:
                SetRenderersEnabled(true);
                SetCollidersEnabled(true);
                ApplyMaterial(_renderers, visibleMaterial);
                break;
            case FogState.Shrouded:
                SetRenderersEnabled(!disableRenderersWhenShrouded);
                SetCollidersEnabled(!disableCollidersWhenHidden); // Colliders usually disabled in shroud
                ApplyMaterial(_renderers, shroudedMaterial);
                break;
            case FogState.Hidden:
            default:
                SetRenderersEnabled(!disableRenderersWhenHidden);
                SetCollidersEnabled(!disableCollidersWhenHidden);
                ApplyMaterial(_renderers, hiddenMaterial);
                break;
        }
    }

    private void SetRenderersEnabled(bool enabledState)
    {
        foreach (Renderer r in _renderers)
        {
            if (r != null) r.enabled = enabledState;
        }
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        foreach (Collider c in _colliders)
        {
            if (c != null) c.enabled = enabledState;
        }
    }

    private void ApplyMaterial(List<Renderer> targetRenderers, Material materialToApply)
    {
        if (materialToApply == null) return;

        foreach (Renderer r in targetRenderers)
        {
            if (r != null)
            {
                // Ensure sharedMaterial is not modified directly if it's a project asset.
                // Instantiating a new material is safer if properties (like color) might be changed per object.
                // For this example, we assume we want to apply a single shared material.
                r.sharedMaterial = materialToApply;
            }
        }
    }
}
```

---

**Example Usage and Scene Setup:**

1.  **Create a New Scene.**
2.  **Terrain/Base:** Create a simple 3D environment (e.g., using Unity's built-in 3D Objects: Plane, or a basic Terrain). This will represent your game map. Scale it up (e.g., 100x100 units).
    *   Add `FogOfWarAffectedObject` to your terrain. Set its `disableRenderersWhenShrouded` to `false` so terrain is visible in shroud. Assign your `HiddenMaterial`, `ShroudedMaterial`, `VisibleMaterial`.

3.  **FogOfWarSystem GameObject:**
    *   Create an Empty GameObject named `FogOfWarSystem`.
    *   Attach the `FogOfWarSystem.cs` script to it.
    *   **Configure `FogOfWarSystem` in the Inspector:**
        *   `Grid Width`: e.g., `100` (matches your terrain size)
        *   `Grid Height`: e.g., `100`
        *   `Cell Size`: e.g., `1`
        *   `Grid Origin`: `0, 0, 0` (or the bottom-left corner of your map)
        *   `Fog Texture Resolution`: e.g., `256` (higher for smoother fog, lower for performance)
        *   **`Fog Plane Renderer`:** This is crucial!
            *   Create a 3D Object -> Quad (or Plane, but Quad is easier for a flat overlay). Name it `Fog Plane`.
            *   Move the `Fog Plane` slightly above your ground (e.g., Y=0.1).
            *   Assign the `FogPlaneMaterial` you created earlier to this `Fog Plane`.
            *   Drag the `Fog Plane` GameObject from the Hierarchy into the `Fog Plane Renderer` slot on the `FogOfWarSystem` component.
        *   Assign `HiddenMaterial` and `ShroudedMaterial` you created.

4.  **Vision Sources:**
    *   Create a 3D Object -> Cube (or Sphere) named `PlayerUnit`. Position it somewhere on the map.
    *   Attach the `VisionSource.cs` script to `PlayerUnit`.
    *   **Configure `PlayerUnit` in the Inspector:**
        *   `Clear Vision Range`: e.g., `10`
        *   `Shroud Vision Range`: e.g., `15`
    *   *Optional:* Add a simple `Rigidbody` and `PlayerController` script (e.g., using arrow keys) to `PlayerUnit` so you can move it around and observe the fog changing.

5.  **Fog-Affected Objects (Enemies, Resources, etc.):**
    *   Create another 3D Object -> Sphere named `EnemyUnit`. Place it some distance away from the `PlayerUnit`.
    *   Attach the `FogOfWarAffectedObject.cs` script to `EnemyUnit`.
    *   **Configure `EnemyUnit` in the Inspector:**
        *   Assign `HiddenMaterial`, `ShroudedMaterial`, `VisibleMaterial`.
        *   Set `Disable Renderers When Hidden` to `true`.
        *   Set `Disable Renderers When Shrouded` to `true` (enemies should not be visible in shrouded areas).
        *   Set `Disable Colliders When Hidden` to `true`.
    *   Duplicate `EnemyUnit` a few times and place them in different areas, some within player vision, some in shroud, some completely hidden.
    *   Create a simple 3D Object -> Cylinder named `ResourceNode`. Place it on the map.
    *   Attach the `FogOfWarAffectedObject.cs` script to `ResourceNode`.
    *   **Configure `ResourceNode` in the Inspector:**
        *   Assign `HiddenMaterial`, `ShroudedMaterial`, `VisibleMaterial`.
        *   `Disable Renderers When Hidden`: `true`.
        *   `Disable Renderers When Shrouded`: `false` (often, resources are visible in shrouded areas, but not interactive).
        *   `Disable Colliders When Hidden`: `true`.

6.  **Camera:** Make sure your camera is looking down at the map.

**Run the scene!**

*   You should see the `Fog Plane` covering the entire map.
*   Areas around your `PlayerUnit` should be clear (transparent fog).
*   Areas that were explored but are no longer in clear vision should be semi-transparent (shrouded fog).
*   Unexplored areas should be completely opaque.
*   `EnemyUnit`s should only appear when your `PlayerUnit` has `clearVisionRange` over them.
*   `ResourceNode`s and `Terrain` should appear in `shrouded` areas but look different than in `visible` areas.

---

This complete example provides a robust foundation for a Fog of War system in Unity. It demonstrates the pattern by separating concerns into a manager, vision generators, and vision responders, making it educational and practical for real-world game development.