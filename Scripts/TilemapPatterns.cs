// Unity Design Pattern Example: TilemapPatterns
// This script demonstrates the TilemapPatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TilemapPatterns' design pattern in Unity focuses on creating reusable, modular definitions of tile arrangements that can be easily applied to a `Tilemap`. Instead of placing individual tiles for every structure (like a corner, a specific wall segment, or a small room), you define these structures once as a "pattern" and then "stamp" them onto your Tilemap.

This pattern is especially useful for:
*   **Procedural Generation:** Quickly placing pre-defined room layouts, path segments, or decorative elements.
*   **Level Design:** Creating a library of common architectural features or environmental details that can be rapidly deployed.
*   **Encapsulation:** Grouping related tiles and their positions into a single, manageable unit, reducing errors and increasing consistency.
*   **Modularity & Reusability:** Patterns can be shared across different scenes or projects and modified independently.

### Core Components of the TilemapPatterns Pattern:

1.  **`PatternTileData` (Data Structure):** A simple struct that holds information for a single tile within a pattern: the `TileBase` asset and its `Vector3Int` position relative to the pattern's origin.
2.  **`TilemapPatternDefinition` (ScriptableObject):** This is the core of the pattern. It's a `ScriptableObject` that encapsulates an entire pattern. It contains a list of `PatternTileData` entries and defines a `pivotOffset` which determines where the pattern's "origin" aligns when placed on a Tilemap. It also provides a method to `ApplyPattern` to a given `Tilemap`.
3.  **`PatternPlacer` (MonoBehaviour):** A component that utilizes `TilemapPatternDefinition` assets. It allows designers or other scripts to select a defined pattern and apply it to a `Tilemap` at a specified position. This acts as the "client" or "user" of the pattern definitions.

---

### File Structure:

You will need three C# files in your Unity project:

1.  `PatternTileData.cs`
2.  `TilemapPatternDefinition.cs`
3.  `PatternPlacer.cs`

---

### 1. `PatternTileData.cs`

This struct represents a single tile entry within a `TilemapPatternDefinition`. It stores the actual `TileBase` and its position relative to the pattern's origin.

```csharp
// PatternTileData.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System; // For System.Serializable

/// <summary>
/// Represents a single tile within a Tilemap Pattern.
/// Stores the TileBase asset and its local position relative to the pattern's origin.
/// </summary>
[Serializable] // Make it serializable so it can be shown in the Inspector
public struct PatternTileData
{
    [Tooltip("The actual TileBase asset to be placed.")]
    public TileBase tile;

    [Tooltip("The position of this tile relative to the pattern's pivot point (0,0,0).")]
    public Vector3Int localPosition;

    // Additional properties could be added here if patterns require more complex tile data,
    // e.g., TileFlags, Color, Matrix4x4 transform, custom data for the tile.
    // public Color color;
    // public Matrix4x4 transform;
}
```

---

### 2. `TilemapPatternDefinition.cs`

This `ScriptableObject` is the heart of the pattern. It defines a complete tile arrangement and provides the logic to apply it to a `Tilemap`.

```csharp
// TilemapPatternDefinition.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that defines a reusable pattern of tiles for Unity's Tilemap system.
/// This acts as the 'Pattern Definition' in the TilemapPatterns design pattern.
/// </summary>
[CreateAssetMenu(fileName = "NewTilemapPattern", menuName = "Tilemap Patterns/Tilemap Pattern Definition", order = 1)]
public class TilemapPatternDefinition : ScriptableObject
{
    [Tooltip("A descriptive name for this pattern, useful for identification in lists.")]
    public string patternName = "New Pattern";

    [Tooltip("The collection of tiles that make up this pattern, along with their positions " +
             "relative to the pattern's pivot point (0,0,0).")]
    public List<PatternTileData> patternTiles = new List<PatternTileData>();

    [Tooltip("The pivot point for the pattern. When placing the pattern on the Tilemap, " +
             "this local position within the pattern will align with the target world cell position.")]
    public Vector3Int pivotOffset = Vector3Int.zero;

    /// <summary>
    /// Applies this pattern to the given Tilemap at the specified world cell position.
    /// The pattern's pivotOffset determines how the pattern aligns with the worldCellPosition.
    /// </summary>
    /// <param name="targetTilemap">The Unity Tilemap component to apply the pattern to.</param>
    /// <param name="worldCellPosition">The world cell coordinates where the pattern's pivot should be placed.</param>
    public void ApplyPattern(Tilemap targetTilemap, Vector3Int worldCellPosition)
    {
        if (targetTilemap == null)
        {
            Debug.LogError($"[{patternName}] Target Tilemap is null. Cannot apply pattern.", this);
            return;
        }

        if (patternTiles == null || patternTiles.Count == 0)
        {
            Debug.LogWarning($"[{patternName}] Pattern '{patternName}' has no tiles defined. Nothing to place.", this);
            return;
        }

        // Iterate through each tile defined in the pattern
        foreach (var tileData in patternTiles)
        {
            // Calculate the actual world cell position for each tile.
            // This is done by taking the target world position (where the pattern's pivot goes),
            // adding the tile's local position, and then subtracting the pattern's pivot offset.
            // Subtracting the pivotOffset shifts the entire pattern so its 'pivotOffset'
            // effectively becomes the worldCellPosition.
            Vector3Int actualTilePosition = worldCellPosition + tileData.localPosition - pivotOffset;

            // Set the tile on the target Tilemap
            targetTilemap.SetTile(actualTilePosition, tileData.tile);
        }

        Debug.Log($"[{patternName}] Pattern '{patternName}' applied at world cell {worldCellPosition}.");
    }

    // --- Optional Extensions ---
    // You could add methods here to:
    // 1. Rotate or flip the pattern before applying (by manipulating patternTiles' localPosition).
    //    This would involve creating a temporary List<PatternTileData> with transformed positions.
    // 2. Clear an area before placing the pattern.
    // 3. Check if the pattern can be placed (e.g., collision detection with existing tiles).
    // 4. Preview the pattern in the editor (more advanced editor scripting).
}
```

---

### 3. `PatternPlacer.cs`

This `MonoBehaviour` acts as an example client for the `TilemapPatternDefinition`s. It allows you to assign a `Tilemap` and various `TilemapPatternDefinition` assets, then place them in the scene via the Inspector, on Start, or via user input.

```csharp
// PatternPlacer.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// MonoBehaviour responsible for placing defined Tilemap Patterns onto a Unity Tilemap.
/// This acts as the 'Client' or 'User' of the TilemapPatterns design pattern.
/// It demonstrates how to utilize the TilemapPatternDefinition ScriptableObjects.
/// </summary>
public class PatternPlacer : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [Tooltip("The Unity Tilemap component to which patterns will be applied.")]
    public Tilemap targetTilemap;

    [Header("Pattern Definitions")]
    [Tooltip("A list of Tilemap Pattern Definitions that can be placed by this placer.")]
    public List<TilemapPatternDefinition> availablePatterns = new List<TilemapPatternDefinition>();

    [Header("Placement Settings")]
    [Tooltip("The index of the pattern to place from the 'availablePatterns' list. " +
             "Use right-click in Play Mode to cycle through patterns.")]
    [Range(0, 99)] // Assuming a reasonable maximum number of patterns for the slider
    public int patternIndexToPlace = 0;

    [Tooltip("The world cell position where the chosen pattern will be placed. " +
             "This aligns with the pattern's pivotOffset.")]
    public Vector3Int placePosition = Vector3Int.zero;

    [Tooltip("If true, the selected pattern will be placed automatically when the scene starts.")]
    public bool placeOnStart = false;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main; // Cache the main camera for performance
    }

    void Start()
    {
        if (placeOnStart)
        {
            PlaceSelectedPattern();
        }
    }

    void Update()
    {
        // Example: Dynamic placement via mouse click
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found. Please ensure your camera is tagged 'MainCamera'.");
                return;
            }

            if (targetTilemap != null && availablePatterns.Count > 0)
            {
                // Convert mouse screen position to world position
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                // Convert world position to cell position (integer grid coordinates)
                Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

                // For 2D tilemaps, the Z-component is usually irrelevant unless using isometric or 3D setups.
                cellPosition.z = 0;

                // Update the inspector variable for debugging and persistence (if needed)
                placePosition = cellPosition;

                PlaceSelectedPattern();
            }
        }

        // Example: Cycle through available patterns with right mouse button for quick testing
        if (Input.GetMouseButtonDown(1)) // Right mouse button click
        {
            if (availablePatterns.Count > 1)
            {
                patternIndexToPlace = (patternIndexToPlace + 1) % availablePatterns.Count;
                Debug.Log($"Switched to pattern: {availablePatterns[patternIndexToPlace].patternName}");
            }
            else if (availablePatterns.Count == 1)
            {
                Debug.LogWarning("Only one pattern available. Cannot cycle.");
            }
            else
            {
                Debug.LogWarning("No patterns available to cycle through.");
            }
        }
    }

    /// <summary>
    /// Public method to place the currently selected pattern at the specified 'placePosition'.
    /// This method can be called from other scripts, UI buttons, or the Inspector Context Menu.
    /// </summary>
    [ContextMenu("Place Selected Pattern Now")] // Adds a button to the Inspector for easy testing
    public void PlaceSelectedPattern()
    {
        // Basic validation
        if (targetTilemap == null)
        {
            Debug.LogError("Target Tilemap is not assigned. Please assign a Tilemap in the Inspector.");
            return;
        }

        if (availablePatterns == null || availablePatterns.Count == 0)
        {
            Debug.LogWarning("No Tilemap Pattern Definitions assigned. Please assign patterns to 'availablePatterns'.");
            return;
        }

        // Validate the pattern index
        if (patternIndexToPlace < 0 || patternIndexToPlace >= availablePatterns.Count)
        {
            Debug.LogWarning($"Pattern index {patternIndexToPlace} is out of bounds (0-{availablePatterns.Count - 1}). " +
                             $"Using pattern at index 0 instead.", this);
            patternIndexToPlace = 0; // Default to the first pattern
        }

        TilemapPatternDefinition selectedPattern = availablePatterns[patternIndexToPlace];

        if (selectedPattern != null)
        {
            // The core usage of the TilemapPatterns pattern:
            // Call the ApplyPattern method on the selected TilemapPatternDefinition.
            selectedPattern.ApplyPattern(targetTilemap, placePosition);
        }
        else
        {
            Debug.LogError($"Selected pattern at index {patternIndexToPlace} is null in the 'availablePatterns' list. " +
                           $"Please ensure all slots in the list refer to valid assets.", this);
        }
    }
}
```

---

### How to Use This Example in Unity:

Follow these steps to set up and run the example:

1.  **Create a New Unity Project:**
    *   Choose a 2D Core template for simplicity.

2.  **Install 2D Tilemap Editor:**
    *   Go to `Window > Package Manager`.
    *   Select "Unity Registry".
    *   Search for "2D Tilemap Editor" and install it.

3.  **Create a Tilemap:**
    *   In the Hierarchy, right-click -> `2D Object > Tilemap > Rectangular`.
    *   Rename the created `Grid` GameObject to `GameGrid` and its child `Tilemap` GameObject to `GameTilemap`.

4.  **Create Tile Assets:**
    *   In your Project window (e.g., in a `Assets/Tiles` folder), right-click -> `Create > 2D > Tiles > Tile`.
    *   Create at least two `Tile` assets, e.g., `GroundTile` and `WallTile`.
    *   For each tile, you'll need to assign a sprite. You can either:
        *   Drag any square image from your project into the `Sprite` field of the Tile asset.
        *   Or, create simple colored sprites: `Right-click > Create > 2D > Sprites > Square`. Set their color and then assign them to the `Tile` assets.

5.  **Create C# Scripts:**
    *   Create the three C# files (`PatternTileData.cs`, `TilemapPatternDefinition.cs`, `PatternPlacer.cs`) in your project (e.g., in `Assets/Scripts`). Copy and paste the code provided above into them.

6.  **Create Tilemap Pattern Definitions (ScriptableObjects):**
    *   In your Project window (e.g., in `Assets/Patterns` folder), right-click -> `Create > Tilemap Patterns > Tilemap Pattern Definition`.
    *   Create at least two patterns, for example: `L_Shape_Wall` and `Small_Room_Floor`.

    *   **Configure `L_Shape_Wall` Pattern:**
        *   Select the `L_Shape_Wall` asset.
        *   Set `Pattern Name`: `L-Shape Wall`
        *   Expand `Pattern Tiles` and set its `Size` to `5`.
        *   Fill in the tiles using your `WallTile` asset and their `Local Position` values:
            *   Element 0: `Tile = WallTile`, `Local Position = (0,0,0)`
            *   Element 1: `Tile = WallTile`, `Local Position = (1,0,0)`
            *   Element 2: `Tile = WallTile`, `Local Position = (2,0,0)`
            *   Element 3: `Tile = WallTile`, `Local Position = (0,1,0)`
            *   Element 4: `Tile = WallTile`, `Local Position = (0,2,0)`
        *   Set `Pivot Offset` to `(0,0,0)`.

    *   **Configure `Small_Room_Floor` Pattern:**
        *   Select the `Small_Room_Floor` asset.
        *   Set `Pattern Name`: `Small Room Floor (3x3)`
        *   Expand `Pattern Tiles` and set its `Size` to `9`.
        *   Fill in the tiles using your `GroundTile` asset for a 3x3 floor:
            *   Element 0: `Tile = GroundTile`, `Local Position = (0,0,0)`
            *   Element 1: `Tile = GroundTile`, `Local Position = (1,0,0)`
            *   Element 2: `Tile = GroundTile`, `Local Position = (2,0,0)`
            *   Element 3: `Tile = GroundTile`, `Local Position = (0,1,0)`
            *   ...and so on, up to `(2,2,0)`.
        *   Set `Pivot Offset` to `(1,1,0)` to center the pivot.

7.  **Create a Pattern Placer GameObject:**
    *   In the Hierarchy, right-click -> `Create Empty`.
    *   Rename it `TilemapManager`.
    *   Add the `PatternPlacer` component to `TilemapManager` (`Add Component` button in Inspector, search for `PatternPlacer`).

8.  **Configure `PatternPlacer` Component:**
    *   Select the `TilemapManager` GameObject.
    *   Drag your `GameTilemap` from the Hierarchy into the `Target Tilemap` slot of the `Pattern Placer` component.
    *   Drag your `L_Shape_Wall` and `Small_Room_Floor` `TilemapPatternDefinition` assets from the Project window into the `Available Patterns` list.
    *   Set `Pattern Index To Place` to `0` (for the `L_Shape_Wall`).
    *   Set `Place On Start` to `true`.
    *   Set `Place Position` to `(0,0,0)`.

9.  **Run the Scene:**
    *   Press the Play button.
    *   The `L_Shape_Wall` pattern should appear on your `GameTilemap` at `(0,0,0)`.
    *   **While the game is running:**
        *   **Click the left mouse button anywhere** in the Game view to place the *current* pattern at that cell position.
        *   **Click the right mouse button** to cycle through the `Available Patterns`. Observe the `patternIndexToPlace` change in the Inspector.
        *   You can also manually change `Pattern Index To Place` and `Place Position` in the Inspector while in Play Mode, then click the `Place Selected Pattern Now` button in the `Pattern Placer` component's Inspector to place it.

This setup demonstrates how to define reusable `TilemapPatternDefinition` assets and then programmatically (or via designer input) apply them to your `Tilemap`, making your level design or procedural generation more efficient and organized.