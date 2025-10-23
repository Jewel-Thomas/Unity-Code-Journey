// Unity Design Pattern Example: SymmetryBuildingSystem
// This script demonstrates the SymmetryBuildingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `SymmetryBuildingSystem` design pattern in Unity provides a robust way to build structures where every action (like placing an object) is automatically mirrored across a predefined symmetry plane. This ensures that the created structure remains symmetrical without requiring manual duplication and positioning of mirrored objects.

**Core Idea of the Pattern:**

1.  **Centralized Control:** A dedicated `SymmetryBuildingSystem` component acts as the sole coordinator for all symmetrical building actions. This central point manages the placement, removal, and modification of both primary and mirrored objects.
2.  **Encapsulation of Symmetry Logic:** The complex mathematical calculations required to determine a mirrored position are encapsulated within the `SymmetryBuildingSystem`. This keeps the higher-level building logic clean and easy to use.
3.  **Consistency and Enforcement:** By having the system automatically handle the mirrored counterpart, consistency is guaranteed. Developers or players cannot accidentally place an object without its symmetrical pair (or remove one without its pair), enforcing the symmetry rule.
4.  **Flexibility:** The system can be configured to use different symmetry axes and planes, allowing for various types of symmetrical structures (e.g., left-right, front-back, up-down).
5.  **Extensibility:** New building blocks, interaction methods, or advanced features (like radial symmetry or multi-plane symmetry) can be added without altering the core symmetrical placement logic.

---

Here's a complete, practical C# Unity example demonstrating the `SymmetryBuildingSystem` pattern:

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For easier removal from lists

/// <summary>
/// Defines the axis of the symmetry plane relative to the symmetryPlaneTransform's local axes.
/// </summary>
public enum SymmetryAxis
{
    X, // Use symmetryPlaneTransform.right as the normal vector for the plane
    Y, // Use symmetryPlaneTransform.up as the normal vector for the plane
    Z  // Use symmetryPlaneTransform.forward as the normal vector for the plane
}

/// <summary>
/// A complete Unity example of the 'SymmetryBuildingSystem' design pattern.
/// This system allows you to place objects in a scene, and it automatically
/// places a symmetrical counterpart based on a predefined symmetry plane.
///
/// Pattern Core Idea:
/// The SymmetryBuildingSystem acts as a central coordinator for all symmetrical
/// building actions. When a 'primary' object is placed, the system automatically
/// calculates and places its 'mirrored' counterpart, ensuring consistency and
/// enforcing the symmetry rule. The details of symmetry calculation are
/// encapsulated within the system.
/// </summary>
public class SymmetryBuildingSystem : MonoBehaviour
{
    [Header("Symmetry Setup")]
    [Tooltip("The Transform that defines the position and orientation of the symmetry plane.")]
    public Transform symmetryPlaneTransform;

    [Tooltip("Which local axis of the symmetryPlaneTransform defines the normal of the symmetry plane. " +
             "E.g., if X is chosen, the plane is perpendicular to the SymmetryPlane's local X-axis (YZ plane).")]
    public SymmetryAxis symmetryAxis = SymmetryAxis.X;

    [Header("Building Blocks")]
    [Tooltip("An array of GameObjects that can be placed as building blocks. " +
             "These should be prefabs.")]
    public GameObject[] blockPrefabs;

    [Tooltip("The material to use for the translucent ghost preview blocks. " +
             "This material should have its Rendering Mode set to 'Fade' to allow transparency.")]
    public Material ghostBlockMaterial;

    [Header("Placement Settings")]
    [Tooltip("The size of the grid to snap placed blocks to. Set to 0 or less to disable snapping.")]
    public float gridSize = 1f;

    [Tooltip("The LayerMask to use for raycasting when trying to find a valid placement position " +
             "(e.g., a 'Ground' layer).")]
    public LayerMask placementLayer;

    [Tooltip("The maximum distance for the raycast to find a placement position.")]
    public float maxPlacementRaycastDistance = 100f;

    // Internal state variables for block type selection and ghost blocks
    private int currentBlockTypeIndex = 0;
    private GameObject ghostPrimaryBlock;
    private GameObject ghostMirroredBlock;

    /// <summary>
    /// Stores information about each pair of symmetrically placed blocks.
    /// This is crucial for managing the built structure (e.g., removal, modification, saving).
    /// </summary>
    [System.Serializable]
    public class PlacedBlockEntry
    {
        public GameObject primaryBlock;
        public GameObject mirroredBlock;
        public int blockTypeIndex;
        // Store the local position relative to the SymmetryBuildingSystem's transform.
        // This allows moving/rotating the entire built structure easily.
        public Vector3 localPositionOfPrimary; 
    }

    // A list to keep track of all placed block pairs
    private List<PlacedBlockEntry> placedBlocks = new List<PlacedBlockEntry>();

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        InitializeGhostBlocks();
    }

    void Update()
    {
        // Handle input for selecting different block types (e.g., 1, 2, 3 keys)
        HandleBlockTypeSelection();

        // Update the position and visibility of the translucent ghost blocks for preview
        UpdateGhostBlocks();

        // Handle block placement input (Left mouse button)
        if (Input.GetMouseButtonDown(0)) 
        {
            TryPlaceBlock();
        }

        // Handle block removal input (Right mouse button)
        if (Input.GetMouseButtonDown(1)) 
        {
            TryRemoveBlock();
        }
    }

    /// <summary>
    /// Cleans up instantiated ghost blocks when the script is destroyed.
    /// </summary>
    void OnDestroy()
    {
        if (ghostPrimaryBlock != null) Destroy(ghostPrimaryBlock);
        if (ghostMirroredBlock != null) Destroy(ghostMirroredBlock);
    }

    // --- Initialization and Cleanup ---

    /// <summary>
    /// Initializes the ghost blocks used for placement preview.
    /// Creates two instances of the current block prefab, assigns the ghost material,
    /// and parents them to this system's transform for organization.
    /// </summary>
    private void InitializeGhostBlocks()
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("SymmetryBuildingSystem: No block prefabs assigned! Please assign block prefabs in the Inspector.");
            enabled = false; // Disable the script if no prefabs are available
            return;
        }

        // Ensure the currentBlockTypeIndex is valid
        if (currentBlockTypeIndex < 0 || currentBlockTypeIndex >= blockPrefabs.Length)
        {
            currentBlockTypeIndex = 0;
        }

        // Create the primary ghost block instance
        ghostPrimaryBlock = Instantiate(blockPrefabs[currentBlockTypeIndex]);
        SetGhostMaterial(ghostPrimaryBlock);
        ghostPrimaryBlock.SetActive(false); // Start inactive
        ghostPrimaryBlock.transform.SetParent(this.transform); // Parent for cleaner hierarchy and easier cleanup

        // Create the mirrored ghost block instance
        ghostMirroredBlock = Instantiate(blockPrefabs[currentBlockTypeIndex]);
        SetGhostMaterial(ghostMirroredBlock);
        ghostMirroredBlock.SetActive(false); // Start inactive
        ghostMirroredBlock.transform.SetParent(this.transform); 
    }

    /// <summary>
    /// Applies the `ghostBlockMaterial` to all Renderer components within a given GameObject.
    /// </summary>
    /// <param name="obj">The GameObject whose renderers should be updated.</param>
    private void SetGhostMaterial(GameObject obj)
    {
        if (ghostBlockMaterial == null)
        {
            Debug.LogWarning("SymmetryBuildingSystem: Ghost block material not assigned. Ghost blocks will use their default material.");
            return;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = ghostBlockMaterial;
        }
    }

    // --- Input Handling ---

    /// <summary>
    /// Handles input for selecting different block types.
    /// Uses number keys (1, 2, 3...) to correspond to blockPrefabs array indices.
    /// </summary>
    private void HandleBlockTypeSelection()
    {
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) // Alpha1, Alpha2, etc. correspond to keys 1, 2, ...
            {
                SetCurrentBlockType(i);
                break;
            }
        }
    }

    /// <summary>
    /// Sets the current block type to be placed and updates the ghost blocks to reflect the change.
    /// </summary>
    /// <param name="index">The index of the desired block prefab in the `blockPrefabs` array.</param>
    public void SetCurrentBlockType(int index)
    {
        if (index >= 0 && index < blockPrefabs.Length)
        {
            if (currentBlockTypeIndex != index) // Only update if the type has changed
            {
                currentBlockTypeIndex = index;
                Debug.Log($"Selected block type: {blockPrefabs[currentBlockTypeIndex].name}");

                // Re-initialize ghost blocks to show the new block type preview
                if (ghostPrimaryBlock != null) Destroy(ghostPrimaryBlock);
                if (ghostMirroredBlock != null) Destroy(ghostMirroredBlock);
                InitializeGhostBlocks(); 
            }
        }
        else
        {
            Debug.LogWarning($"SymmetryBuildingSystem: Block type index {index} is out of bounds.");
        }
    }

    /// <summary>
    /// Attempts to place a block (and its symmetrical counterpart) based on a mouse click.
    /// A raycast from the mouse position determines the placement location on the `placementLayer`.
    /// </summary>
    private void TryPlaceBlock()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPlacementRaycastDistance, placementLayer))
        {
            // Calculate the world position for the primary block, snapped to the grid.
            // Offset by half the grid size along the normal to place the block on top of the surface, not embedded.
            Vector3 placementWorldPosition = SnapToGrid(hit.point + hit.normal * (gridSize / 2f)); 
            PlaceBlock(placementWorldPosition, currentBlockTypeIndex);
        }
    }

    /// <summary>
    /// Attempts to remove a block (and its symmetrical counterpart) based on a mouse click.
    /// A raycast from the mouse position determines which block was clicked.
    /// </summary>
    private void TryRemoveBlock()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // For removal, we don't need a specific layer mask; we want to hit any block
        if (Physics.Raycast(ray, out hit, maxPlacementRaycastDistance)) 
        {
            // Find if the hit object is a primary or mirrored block managed by this system
            GameObject hitObject = hit.collider.gameObject;
            RemoveBlock(hitObject);
        }
    }

    // --- Core Symmetry Building System Logic ---

    /// <summary>
    /// Calculates the world position of a mirrored point relative to the symmetry plane.
    /// This method encapsulates the core symmetry calculation logic.
    /// </summary>
    /// <param name="originalWorldPosition">The world position of the original point to mirror.</param>
    /// <returns>The world position of the mirrored point.</returns>
    private Vector3 CalculateMirroredPosition(Vector3 originalWorldPosition)
    {
        if (symmetryPlaneTransform == null)
        {
            Debug.LogError("SymmetryBuildingSystem: Symmetry Plane Transform is not assigned! Cannot calculate mirrored position.");
            return originalWorldPosition; // Return original if setup is invalid
        }

        // Determine the plane's normal vector based on the selected `SymmetryAxis`
        Vector3 planeNormal = Vector3.zero;
        switch (symmetryAxis)
        {
            case SymmetryAxis.X: planeNormal = symmetryPlaneTransform.right; break;
            case SymmetryAxis.Y: planeNormal = symmetryPlaneTransform.up; break;
            case SymmetryAxis.Z: planeNormal = symmetryPlaneTransform.forward; break;
        }

        // The origin of the symmetry plane in world space
        Vector3 planeOrigin = symmetryPlaneTransform.position;

        // Vector from a point on the plane to the original point
        Vector3 vectorFromPlaneToPoint = originalWorldPosition - planeOrigin;

        // Calculate the distance from the point to the plane along the normal.
        // This is the projection of 'vectorFromPlaneToPoint' onto 'planeNormal'.
        float distance = Vector3.Dot(vectorFromPlaneToPoint, planeNormal);

        // The mirrored point is found by subtracting twice this distance along the normal.
        // This effectively "crosses" the plane and goes the same distance on the other side.
        Vector3 mirroredWorldPosition = originalWorldPosition - 2 * distance * planeNormal;

        return mirroredWorldPosition;
    }

    /// <summary>
    /// Places a new block at a specified world position and automatically
    /// places its symmetrical counterpart. This is the core method demonstrating
    /// the SymmetryBuildingSystem pattern in action.
    /// </summary>
    /// <param name="worldPosition">The desired world position for the primary block.</param>
    /// <param name="blockTypeIndex">The index of the block prefab from the `blockPrefabs` array to use.</param>
    public void PlaceBlock(Vector3 worldPosition, int blockTypeIndex)
    {
        if (blockPrefabs == null || blockTypeIndex < 0 || blockTypeIndex >= blockPrefabs.Length)
        {
            Debug.LogError($"SymmetryBuildingSystem: Invalid block type index {blockTypeIndex}. Cannot place block.");
            return;
        }
        if (symmetryPlaneTransform == null)
        {
            Debug.LogError("SymmetryBuildingSystem: Symmetry Plane Transform is not assigned! Cannot place symmetrical blocks.");
            return;
        }

        // 1. Calculate the world position for the mirrored block using the encapsulated logic
        Vector3 mirroredWorldPosition = CalculateMirroredPosition(worldPosition);

        // 2. Instantiate the primary block at the calculated world position
        GameObject primaryBlock = Instantiate(blockPrefabs[blockTypeIndex], worldPosition, Quaternion.identity);
        // Parent the block to this system's transform for better organization in the Hierarchy.
        // This also allows moving the entire built structure by moving this GameObject.
        primaryBlock.transform.SetParent(this.transform); 

        // 3. Instantiate the mirrored block at its calculated symmetrical world position
        // For simple blocks, Quaternion.identity is often sufficient. For complex objects with
        // specific orientations or non-symmetrical meshes, a mirrored rotation might be needed.
        // E.g., Quaternion mirroredRotation = Quaternion.LookRotation(Vector3.Reflect(primaryBlock.transform.forward, planeNormal));
        GameObject mirroredBlock = Instantiate(blockPrefabs[blockTypeIndex], mirroredWorldPosition, Quaternion.identity);
        mirroredBlock.transform.SetParent(this.transform); 

        // 4. Store the entry for future management (e.g., undo, removal, saving/loading)
        placedBlocks.Add(new PlacedBlockEntry
        {
            primaryBlock = primaryBlock,
            mirroredBlock = mirroredBlock,
            blockTypeIndex = blockTypeIndex,
            // Store the primary block's local position relative to this system's transform
            localPositionOfPrimary = this.transform.InverseTransformPoint(worldPosition) 
        });

        Debug.Log($"Placed {blockPrefabs[blockTypeIndex].name} at {worldPosition} and its mirror at {mirroredWorldPosition}");
    }

    /// <summary>
    /// Removes a block and its symmetrical counterpart if found in the `placedBlocks` list.
    /// This ensures that symmetry is maintained even during removal operations.
    /// </summary>
    /// <param name="blockToDestroy">The GameObject (which could be either the primary or the mirrored block) to remove.</param>
    public void RemoveBlock(GameObject blockToDestroy)
    {
        // Use LINQ to find the PlacedBlockEntry that contains the GameObject to be destroyed.
        // This is efficient for small to medium lists. For very large structures, a Dictionary
        // mapping GameObject to PlacedBlockEntry might be more performant.
        PlacedBlockEntry entryToRemove = placedBlocks.FirstOrDefault(
            entry => entry.primaryBlock == blockToDestroy || entry.mirroredBlock == blockToDestroy
        );

        if (entryToRemove != null)
        {
            // Destroy both the primary and the mirrored blocks to maintain symmetry
            if (entryToRemove.primaryBlock != null) Destroy(entryToRemove.primaryBlock);
            if (entryToRemove.mirroredBlock != null) Destroy(entryToRemove.mirroredBlock);

            // Remove the entry from our tracking list
            placedBlocks.Remove(entryToRemove);
            Debug.Log($"Removed block and its symmetrical counterpart.");
        }
        else
        {
            // If the clicked object is not managed by this system, provide feedback.
            Debug.Log($"Clicked on a block not managed by this system or already removed: {blockToDestroy.name}");
        }
    }

    // --- Utility Methods ---

    /// <summary>
    /// Snaps a world position to the defined grid.
    /// </summary>
    /// <param name="worldPosition">The original world position.</param>
    /// <returns>The world position snapped to the nearest grid point.</returns>
    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        if (gridSize <= 0) return worldPosition; // No snapping if grid size is zero or negative

        float snappedX = Mathf.Round(worldPosition.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(worldPosition.y / gridSize) * gridSize;
        float snappedZ = Mathf.Round(worldPosition.z / gridSize) * gridSize;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    /// <summary>
    /// Updates the position and visibility of the ghost blocks (primary and mirrored)
    /// to provide a visual preview of where blocks will be placed.
    /// </summary>
    private void UpdateGhostBlocks()
    {
        if (ghostPrimaryBlock == null || ghostMirroredBlock == null || blockPrefabs.Length == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPlacementRaycastDistance, placementLayer))
        {
            // Calculate primary ghost position (snapped to grid, offset from hit surface)
            Vector3 primaryGhostWorldPosition = SnapToGrid(hit.point + hit.normal * (gridSize / 2f));
            ghostPrimaryBlock.transform.position = primaryGhostWorldPosition;

            // Calculate mirrored ghost position using the same symmetry logic
            Vector3 mirroredGhostWorldPosition = CalculateMirroredPosition(primaryGhostWorldPosition);
            ghostMirroredBlock.transform.position = mirroredGhostWorldPosition;

            // Ensure both ghost blocks are active and visible
            if (!ghostPrimaryBlock.activeSelf) ghostPrimaryBlock.SetActive(true);
            if (!ghostMirroredBlock.activeSelf) ghostMirroredBlock.SetActive(true);
        }
        else
        {
            // Deactivate ghosts if the raycast doesn't hit anything on the placement layer
            if (ghostPrimaryBlock.activeSelf) ghostPrimaryBlock.SetActive(false);
            if (ghostMirroredBlock.activeSelf) ghostMirroredBlock.SetActive(false);
        }
    }

    // --- Gizmos for Visualization in Editor ---

    void OnDrawGizmos()
    {
        if (symmetryPlaneTransform != null)
        {
            // Set Gizmo color for the plane
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f); // Blue, semi-transparent

            Vector3 planeNormal = Vector3.zero;
            Vector3 planeTangent1 = Vector3.zero; // Vectors defining the plane's surface
            Vector3 planeTangent2 = Vector3.zero;

            // Determine the plane's normal and two perpendicular tangent vectors based on `symmetryAxis`
            switch (symmetryAxis)
            {
                case SymmetryAxis.X: // Normal is along symmetryPlaneTransform's local Right (X)
                    planeNormal = symmetryPlaneTransform.right;
                    planeTangent1 = symmetryPlaneTransform.up;
                    planeTangent2 = symmetryPlaneTransform.forward;
                    break;
                case SymmetryAxis.Y: // Normal is along symmetryPlaneTransform's local Up (Y)
                    planeNormal = symmetryPlaneTransform.up;
                    planeTangent1 = symmetryPlaneTransform.right;
                    planeTangent2 = symmetryPlaneTransform.forward;
                    break;
                case SymmetryAxis.Z: // Normal is along symmetryPlaneTransform's local Forward (Z)
                    planeNormal = symmetryPlaneTransform.forward;
                    planeTangent1 = symmetryPlaneTransform.right;
                    planeTangent2 = symmetryPlaneTransform.up;
                    break;
            }

            // Draw a square representation of the symmetry plane in the editor
            float planeSize = 10f; // Size of the drawn gizmo plane
            Vector3 p1 = symmetryPlaneTransform.position + planeTangent1 * planeSize / 2f + planeTangent2 * planeSize / 2f;
            Vector3 p2 = symmetryPlaneTransform.position - planeTangent1 * planeSize / 2f + planeTangent2 * planeSize / 2f;
            Vector3 p3 = symmetryPlaneTransform.position - planeTangent1 * planeSize / 2f - planeTangent2 * planeSize / 2f;
            Vector3 p4 = symmetryPlaneTransform.position + planeTangent1 * planeSize / 2f - planeTangent2 * planeSize / 2f;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
            Gizmos.DrawLine(p1, p3); // Diagonal for better visibility

            // Draw the normal vector of the symmetry plane (in red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(symmetryPlaneTransform.position, symmetryPlaneTransform.position + planeNormal * 2f);
            Gizmos.DrawSphere(symmetryPlaneTransform.position + planeNormal * 2f, 0.1f); // Arrowhead for normal
        }
    }
}

/*
 * --- Example Usage and Setup Guide in Unity ---
 *
 * Follow these steps to set up and use the SymmetryBuildingSystem in your Unity project:
 *
 * 1.  Create a new Unity Project or open an existing one.
 *
 * 2.  Create C# Script:
 *     - In the Project window, right-click -> Create -> C# Script, name it "SymmetryBuildingSystem".
 *     - Copy and paste the entire code above into this script.
 *
 * 3.  Create Building Block Prefabs:
 *     - In the Hierarchy, right-click -> 3D Object -> Cube.
 *     - Rename it to "Block_Cube".
 *     - Drag "Block_Cube" from the Hierarchy into your Project window (e.g., into a "Prefabs" folder) to create a prefab.
 *     - Delete "Block_Cube" from the Hierarchy (we will instantiate it via script).
 *     - (Optional) Repeat this for other shapes (Sphere, Cylinder) to have multiple block types.
 *
 * 4.  Create a Ground Plane:
 *     - In the Hierarchy, right-click -> 3D Object -> Plane.
 *     - Rename it "Ground".
 *     - Ensure it has a Collider component (Plane already does).
 *     - Create a new Layer: Go to the Layer dropdown in the Inspector -> Add Layer...
 *       Add a new layer, e.g., "GroundLayer".
 *     - Assign the "Ground" GameObject to the "GroundLayer" layer.
 *
 * 5.  Create a Symmetry Plane GameObject:
 *     - In the Hierarchy, right-click -> Create Empty.
 *     - Rename it "SymmetryPlane".
 *     - Position it at (0,0,0) or anywhere you want your symmetry axis to be. This transform defines the plane's origin and orientation.
 *     - For a common vertical mirror plane (e.g., mirroring across the YZ plane at X=0):
 *       - Set its Position to (0,0,0) and Rotation to (0,0,0).
 *       - In the `SymmetryBuildingSystem` component, you would choose `SymmetryAxis.X`. The Gizmo will show a red arrow along the X-axis, indicating the plane's normal.
 *     - For a horizontal mirror plane (e.g., mirroring across the XZ plane at Y=0, like a floor):
 *       - Set its Position to (0,0,0) and Rotation to (0,0,0).
 *       - In the `SymmetryBuildingSystem` component, you would choose `SymmetryAxis.Y`. The Gizmo will show a red arrow along the Y-axis.
 *
 * 6.  Create the SymmetryBuildingSystem GameObject:
 *     - In the Hierarchy, right-click -> Create Empty.
 *     - Rename it "BuildingSystem".
 *     - Drag the "SymmetryBuildingSystem" script onto this "BuildingSystem" GameObject in the Hierarchy.
 *
 * 7.  Configure the SymmetryBuildingSystem Component:
 *     - Select the "BuildingSystem" GameObject in the Hierarchy.
 *     - In its Inspector, you'll see the "Symmetry Building System" component.
 *     - Drag the "SymmetryPlane" GameObject from the Hierarchy into the "Symmetry Plane Transform" slot.
 *     - Set "Symmetry Axis": Choose `X`, `Y`, or `Z` based on how you set up your "SymmetryPlane" (see step 5).
 *     - Set "Block Prefabs":
 *       - Set the "Size" to the number of different block types you have (e.g., 1 for just "Block_Cube").
 *       - Drag your "Block_Cube" prefab (and any others) from the Project window into the "Element 0" slot (and subsequent elements).
 *     - Create a Ghost Block Material:
 *       - In the Project window, right-click -> Create -> Material. Name it "GhostMaterial".
 *       - In the Inspector for "GhostMaterial", change its "Rendering Mode" to "Fade".
 *       - Set its Albedo color to a translucent color (e.g., light blue with low Alpha value, like 60-100).
 *       - Drag "GhostMaterial" into the "Ghost Block Material" slot on the "BuildingSystem" component.
 *     - Set "Placement Layer": Select "GroundLayer" from the dropdown.
 *
 * 8.  Run the Scene:
 *     - Press Play.
 *     - Move your mouse over the "Ground" plane. You should see two translucent "ghost" blocks appearing symmetrically.
 *     - Left-click to place a block. A primary block and its mirrored counterpart will be instantiated.
 *     - Right-click to remove a block. If you click on either a primary or mirrored block, both will be removed.
 *     - Press number keys (1, 2, 3...) to switch between different block types if you've added more prefabs.
 *
 * This setup provides a robust and clear demonstration of the SymmetryBuildingSystem pattern,
 * making it ready for integration into more complex game environments like level editors or base-building games.
 */
```