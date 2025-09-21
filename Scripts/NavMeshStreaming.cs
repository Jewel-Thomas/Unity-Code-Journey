// Unity Design Pattern Example: NavMeshStreaming
// This script demonstrates the NavMeshStreaming pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a practical and educational example of the **NavMeshStreaming** design pattern. It dynamically loads and unloads baked NavMesh data assets based on a player's position, optimizing memory usage and performance in large open-world or tiled environments.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMesh, NavMeshData, and NavMesh.NavMeshDataInstance
using System.Collections;
using System.Collections.Generic; // For Dictionary and HashSet

/// <summary>
/// Implements the NavMeshStreaming design pattern in Unity.
/// This script dynamically loads and unloads baked NavMesh data assets
/// based on a player's position, optimizing memory usage and performance
/// in large open-world or tiled environments.
/// </summary>
/// <remarks>
/// The 'NavMeshStreaming' pattern addresses the challenge of having a single,
/// massive NavMesh for an entire game world, which can lead to:
/// 1.  **High Memory Usage:** Loading the entire NavMesh into RAM.
/// 2.  **Long Load Times:** Initial baking and runtime loading of a huge mesh.
/// 3.  **Performance Issues:** Complex pathfinding queries on a giant mesh.
///
/// Instead, the world is divided into smaller, manageable "chunks" or "tiles."
/// Each chunk's NavMesh is baked separately and saved as a <see cref="NavMeshData"/> asset.
/// This script then loads these assets into the Unity NavMesh system
/// only when the player (or an agent) is within a certain distance, and unloads
/// them when they move out of range.
///
/// **How to Use (Example Setup):**
///
/// **1. Prepare Your Scene Geometry:**
///    *   Divide your large game world into logical "chunks" or "tiles." For example, if `chunkSize` is 50,
///        you might have game objects like "Terrain_Chunk_0_0", "Terrain_Chunk_1_0", "Terrain_Chunk_-1_1", etc.,
///        each covering a 50x50 world area.
///    *   Ensure the geometry for each chunk is contained within its respective GameObject.
///
/// **2. Bake NavMesh Data for Each Chunk:**
///    *   Create a folder `Assets/Resources/NavMeshes` (or whatever you set `navMeshDataFolder` to).
///    *   For *each* chunk GameObject (e.g., "Terrain_Chunk_0_0"):
///        a.  Add a <see cref="NavMeshSurface"/> component to it (requires `AI Navigation` package from Package Manager).
///        b.  **Crucially:** Configure the <see cref="NavMeshSurface"/> for the specific chunk:
///            *   Set `Collect Objects` to `Volume`.
///            *   Adjust `Center` and `Size` of the <see cref="NavMeshSurface"/> to perfectly encapsulate that chunk's geometry.
///                *   Example: For chunk (0,0) (world coords X:0-50, Z:0-50), `Center` would be (25, 0, 25), `Size` would be (50, 100, 50).
///                *   Example: For chunk (1,0) (world coords X:50-100, Z:0-50), `Center` would be (75, 0, 25), `Size` would be (50, 100, 50).
///                *   **Important:** The <see cref="NavMeshSurface"/> component's transform and volume define the origin and extent of the *baked* NavMesh data. The `worldOffset` parameter in <see cref="NavMesh.AddNavMeshData"/> then shifts this data to its correct world location.
///            *   Adjust `Agent Type` and other baking settings as needed.
///        c.  Click the `Bake` button on the <see cref="NavMeshSurface"/> component.
///        d.  In the save dialog, save the <see cref="NavMeshData"/> asset into your `Assets/Resources/NavMeshes` folder.
///        e.  **Naming Convention:** Name the asset according to its chunk coordinates.
///            For chunk (X, Z) with `chunkSize`, name it `NavMesh_Chunk_X_Z.asset`.
///            Example: `NavMesh_Chunk_0_0.asset`, `NavMesh_Chunk_1_0.asset`, `NavMesh_Chunk_-1_2.asset`.
///            This naming convention is critical for the script to find and load the correct assets.
///        f.  After baking, you can disable or remove the <see cref="NavMeshSurface"/> component from the GameObject if desired,
///            as we only need the baked `.asset` file at runtime.
///
/// **3. Set up the NavMeshStreamer in your scene:**
///    *   Create an empty GameObject in your scene (e.g., "NavMesh Streamer").
///    *   Attach this `NavMeshStreamer.cs` script to it.
///    *   Assign your `Player` (or the GameObject with the <see cref="NavMeshAgent"/> that needs navigation) to the `Player Transform` field.
///    *   Set `Chunk Size` to match the size you used when dividing your world and baking.
///    *   Adjust `Load Distance Chunks` to control how many chunks in each direction around the player should be loaded.
///        (e.g., 2 means 5x5 chunks will be active: current + 2 in each X/Z direction).
///    *   Ensure `Nav Mesh Data Folder` matches the path within `Resources` where your `.asset` files are stored.
///    *   Adjust `Refresh Interval` to control how often the script checks player position.
///
/// **4. Add NavMeshAgents:**
///    *   Any <see cref="NavMeshAgent"/>s in your scene will automatically use the currently loaded NavMesh data.
///    *   When an agent tries to pathfind across an unloaded chunk boundary, it will likely fail or get stuck
///        until that chunk's NavMesh is loaded.
/// </remarks>
public class NavMeshStreamer : MonoBehaviour
{
    // --- Configuration Parameters ---
    [Header("Streaming Settings")]
    [Tooltip("The transform of the player or main character whose position dictates NavMesh loading.")]
    public Transform playerTransform;

    [Tooltip("The size of each NavMesh chunk in world units (e.g., 50x50 means a chunk covers 50 units on X and Z). " +
             "This MUST match the 'Size' used when baking individual NavMeshSurface components for each chunk.")]
    public float chunkSize = 50f;

    [Tooltip("How many chunks in each direction (X and Z) around the player's current chunk should be loaded. " +
             "A value of 1 means a 3x3 grid (current + 1 in each dir), 2 means a 5x5 grid, etc.")]
    public int loadDistanceChunks = 2; // e.g., 2 means a 5x5 grid (player chunk + 2 on each side)

    [Tooltip("The path within the 'Resources' folder where NavMesh data assets are stored. " +
             "E.g., 'NavMeshes' if assets are in 'Assets/Resources/NavMeshes/'.")]
    public string navMeshDataFolder = "NavMeshes";

    [Tooltip("The refresh interval in seconds for checking player position and updating loaded NavMeshes.")]
    public float refreshInterval = 1.0f;

    // --- Internal State ---
    // Stores currently loaded NavMesh data instances and their unique handles.
    // Key: Chunk coordinates (Vector3Int representing grid cell X, Y, Z - though Y is often ignored for 2D grids).
    // Value: The NavMesh.NavMeshDataInstance handle returned by NavMesh.AddNavMeshData().
    private Dictionary<Vector3Int, NavMesh.NavMeshDataInstance> loadedNavMeshes = new Dictionary<Vector3Int, NavMesh.NavMeshDataInstance>();

    // To prevent checking player position every frame, we use a timer.
    private float _nextRefreshTime;

    // The last chunk coordinates the player was in. Used to detect when a refresh is needed.
    private Vector3Int _lastPlayerChunkCoords;

    // --- Unity Lifecycle Methods ---
    void Start()
    {
        // Basic validation: ensure the player transform is assigned.
        if (playerTransform == null)
        {
            Debug.LogError("NavMeshStreamer: Player Transform is not assigned! Please assign a transform to track.", this);
            enabled = false; // Disable the script if critical dependency is missing.
            return;
        }

        // Initialize internal state.
        _lastPlayerChunkCoords = GetChunkCoordinates(playerTransform.position);
        _nextRefreshTime = Time.time + refreshInterval;

        // Perform an initial load of NavMeshes around the starting player position.
        RefreshNavMeshes();
    }

    void Update()
    {
        // Check for refresh only at the specified interval.
        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + refreshInterval; // Set next refresh time.
            Vector3Int currentPlayerChunkCoords = GetChunkCoordinates(playerTransform.position);

            // Only refresh the loaded NavMeshes if the player has moved into a new chunk
            // or if it's the very first refresh (loadedNavMeshes.Count == 0 ensures initial call happens).
            if (currentPlayerChunkCoords != _lastPlayerChunkCoords || loadedNavMeshes.Count == 0)
            {
                _lastPlayerChunkCoords = currentPlayerChunkCoords; // Update last known chunk.
                RefreshNavMeshes(); // Trigger the loading/unloading logic.
            }
        }
    }

    void OnDisable()
    {
        // Crucial: Clean up all loaded NavMeshes when the streamer is disabled or destroyed.
        // Failing to do so can leave orphaned NavMesh data in memory, leading to unexpected behavior or leaks.
        UnloadAllNavMeshes();
    }

    // --- Core Logic Methods ---

    /// <summary>
    /// Converts a world position into its corresponding NavMesh chunk grid coordinates.
    /// These coordinates are integer-based, representing grid cells (e.g., (0,0,0) for the origin chunk, (1,0,0) for the next chunk on X).
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <returns>A <see cref="Vector3Int"/> representing the chunk grid coordinates.</returns>
    private Vector3Int GetChunkCoordinates(Vector3 worldPosition)
    {
        // Integer division (using Mathf.FloorToInt for correct handling of negative numbers)
        // determines which chunk grid cell a world position falls into.
        // For example, if chunkSize is 50:
        // worldPosition.x = 49.9 -> x = 0
        // worldPosition.x = 50.1 -> x = 1
        // worldPosition.x = -0.1 -> x = -1 (due to FloorToInt)
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int z = Mathf.FloorToInt(worldPosition.z / chunkSize);
        // The Y coordinate is typically ignored for a 2D NavMesh streaming grid on the XZ plane.
        // If your game has vertical streaming (e.g., multi-layered levels), you would need to
        // consider the Y-axis similarly for chunking.
        return new Vector3Int(x, 0, z); // Y is set to 0 as it's usually not chunked vertically.
    }

    /// <summary>
    /// Determines which NavMesh chunks should be loaded based on the player's current position and load distance.
    /// It then loads any missing chunks and unloads any out-of-range chunks.
    /// </summary>
    private void RefreshNavMeshes()
    {
        Vector3Int currentPlayerChunk = GetChunkCoordinates(playerTransform.position);
        HashSet<Vector3Int> chunksToKeepLoaded = new HashSet<Vector3Int>();

        // Calculate all chunk coordinates that should be within the active loading range.
        // This creates a square grid of (2 * loadDistanceChunks + 1) by (2 * loadDistanceChunks + 1) chunks.
        for (int xOffset = -loadDistanceChunks; xOffset <= loadDistanceChunks; xOffset++)
        {
            for (int zOffset = -loadDistanceChunks; zOffset <= loadDistanceChunks; zOffset++)
            {
                Vector3Int targetChunkCoords = new Vector3Int(
                    currentPlayerChunk.x + xOffset,
                    0, // Y offset is typically 0 for a flat grid streaming
                    currentPlayerChunk.z + zOffset
                );
                chunksToKeepLoaded.Add(targetChunkCoords);
            }
        }

        // --- Step 1: Unload Chunks that are no longer needed ---
        List<Vector3Int> chunksToUnload = new List<Vector3Int>();
        // Iterate through all currently loaded NavMeshes.
        foreach (var entry in loadedNavMeshes)
        {
            // If a loaded chunk's coordinates are NOT in the `chunksToKeepLoaded` set, mark it for unloading.
            if (!chunksToKeepLoaded.Contains(entry.Key))
            {
                chunksToUnload.Add(entry.Key);
            }
        }

        // Perform the unloading for all marked chunks.
        foreach (Vector3Int chunkCoords in chunksToUnload)
        {
            UnloadChunk(chunkCoords);
        }

        // --- Step 2: Load Chunks that are needed but not yet loaded ---
        // Iterate through all chunks that *should* be loaded.
        foreach (Vector3Int chunkCoords in chunksToKeepLoaded)
        {
            // If a required chunk is NOT currently in the `loadedNavMeshes` dictionary, load it.
            if (!loadedNavMeshes.ContainsKey(chunkCoords))
            {
                LoadChunk(chunkCoords);
            }
        }
        
        // Optional debug logging (uncomment to see runtime output):
        // Debug.Log($"NavMeshStreamer: Refreshed. Currently loaded {loadedNavMeshes.Count} chunks. " +
        //           $"Player in chunk: {currentPlayerChunk.x},{currentPlayerChunk.z}");
    }

    /// <summary>
    /// Loads a <see cref="NavMeshData"/> asset for a given chunk coordinate and adds it to the Unity NavMesh system.
    /// </summary>
    /// <param name="chunkCoords">The integer coordinates (X, Z) of the chunk to load.</param>
    private void LoadChunk(Vector3Int chunkCoords)
    {
        // Construct the resource path for the NavMesh data asset using the defined naming convention.
        // Example path: "NavMeshes/NavMesh_Chunk_0_0"
        string path = $"{navMeshDataFolder}/NavMesh_Chunk_{chunkCoords.x}_{chunkCoords.z}";

        // Attempt to load the NavMeshData asset from the Resources folder.
        // IMPORTANT: Resources.Load is synchronous and can cause hitches for large assets.
        // For production, consider using Addressables for asynchronous loading and better asset management.
        NavMeshData navMeshData = Resources.Load<NavMeshData>(path);

        if (navMeshData != null)
        {
            // Calculate the world position offset for this chunk.
            // When NavMesh.AddNavMeshData is called, the baked data (which was baked relative to its NavMeshSurface's origin)
            // is placed in the world. We need to offset it to match the actual world position of this chunk.
            // For instance, chunk (1,0) starts at world X=chunkSize, Z=0.
            Vector3 worldOffset = new Vector3(chunkCoords.x * chunkSize, 0, chunkCoords.z * chunkSize);
            
            // Add the NavMeshData to the global NavMesh system.
            // The NavMesh.AddNavMeshData function returns a unique handle (NavMesh.NavMeshDataInstance)
            // which is essential for later removing this specific NavMesh data.
            NavMesh.NavMeshDataInstance instance = NavMesh.AddNavMeshData(navMeshData, worldOffset, Quaternion.identity);
            
            // Store the instance handle along with its chunk coordinates for tracking.
            loadedNavMeshes.Add(chunkCoords, instance);
            // Debug.Log($"NavMeshStreamer: Loaded NavMesh '{path}' at world offset {worldOffset}.");
        }
        else
        {
            // Log a warning if a NavMesh asset is expected but not found.
            Debug.LogWarning($"NavMeshStreamer: Could not load NavMesh data for chunk [{chunkCoords.x},{chunkCoords.z}] " +
                             $"at path: '{path}'. Ensure the asset exists in 'Resources/{navMeshDataFolder}/' " +
                             $"and follows the naming convention 'NavMesh_Chunk_X_Z.asset'.", this);
        }
    }

    /// <summary>
    /// Unloads a NavMesh data asset from the Unity NavMesh system using its instance handle.
    /// </summary>
    /// <param name="chunkCoords">The integer coordinates (X, Z) of the chunk to unload.</param>
    private void UnloadChunk(Vector3Int chunkCoords)
    {
        // Try to retrieve the NavMesh.NavMeshDataInstance handle for the given chunk.
        if (loadedNavMeshes.TryGetValue(chunkCoords, out NavMesh.NavMeshDataInstance instance))
        {
            // Remove the NavMesh data from the global NavMesh system.
            NavMesh.RemoveNavMeshData(instance);
            // Remove the entry from our tracking dictionary.
            loadedNavMeshes.Remove(chunkCoords);
            // Debug.Log($"NavMeshStreamer: Unloaded NavMesh for chunk: [{chunkCoords.x},{chunkCoords.z}].");
        }
    }

    /// <summary>
    /// Unloads all currently loaded NavMesh data. This is typically called when the streamer component
    /// is disabled or destroyed, ensuring a clean state and preventing memory leaks.
    /// </summary>
    private void UnloadAllNavMeshes()
    {
        // Iterate through all stored instance handles and remove them from the NavMesh system.
        foreach (var entry in loadedNavMeshes.Values)
        {
            NavMesh.RemoveNavMeshData(entry);
        }
        // Clear the dictionary as all entries are now unloaded.
        loadedNavMeshes.Clear();
        // Debug.Log("NavMeshStreamer: Unloaded all NavMeshes.");
    }

    // --- Editor Visualization (Gizmos) ---
    /// <summary>
    /// Draws helpful Gizmos in the Unity editor to visualize the loaded NavMesh chunks
    /// and the player's current chunk and load radius.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw Gizmos if the playerTransform is assigned to avoid errors.
        if (playerTransform == null) return;

        // --- In Play Mode ---
        if (Application.isPlaying)
        {
            // Draw a boundary for each *currently loaded* NavMesh chunk.
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, semi-transparent
            foreach (Vector3Int chunkCoords in loadedNavMeshes.Keys)
            {
                // Calculate the center of the chunk for drawing.
                Vector3 center = new Vector3(chunkCoords.x * chunkSize + chunkSize / 2f, 0, chunkCoords.z * chunkSize + chunkSize / 2f);
                Gizmos.DrawCube(center, new Vector3(chunkSize, 1f, chunkSize)); // Draw a flat cube representing the chunk.
            }

            // Draw the player's current chunk boundary.
            Gizmos.color = new Color(1, 1, 0, 0.5f); // Yellow, semi-transparent
            Vector3Int currentPlayerChunk = GetChunkCoordinates(playerTransform.position);
            Vector3 playerChunkCenter = new Vector3(currentPlayerChunk.x * chunkSize + chunkSize / 2f, 0, currentPlayerChunk.z * chunkSize + chunkSize / 2f);
            Gizmos.DrawCube(playerChunkCenter, new Vector3(chunkSize, 1.5f, chunkSize)); // Slightly taller for emphasis.

            // Draw the overall "refresh radius" visually (outermost loaded chunk boundaries).
            if (loadedNavMeshes.Count > 0)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // Orange, very transparent
                float totalLoadAreaSize = (loadDistanceChunks * 2 + 1) * chunkSize; // Total size of the square area.
                Gizmos.DrawWireCube(playerChunkCenter, new Vector3(totalLoadAreaSize, 2f, totalLoadAreaSize));
            }
        }
        // --- In Editor Mode (when not playing) ---
        else
        {
            // Draw a preview of the player's current chunk and load radius in editor mode
            // This helps visualize the streaming area even before running the game.
            Gizmos.color = new Color(1, 1, 0, 0.5f); // Yellow
            Vector3Int previewChunk = GetChunkCoordinates(playerTransform.position);
            Vector3 previewChunkCenter = new Vector3(previewChunk.x * chunkSize + chunkSize / 2f, 0, previewChunk.z * chunkSize + chunkSize / 2f);
            Gizmos.DrawCube(previewChunkCenter, new Vector3(chunkSize, 1.5f, chunkSize));

            Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // Orange, transparent
            float previewTotalLoadAreaSize = (loadDistanceChunks * 2 + 1) * chunkSize;
            Gizmos.DrawWireCube(previewChunkCenter, new Vector3(previewTotalLoadAreaSize, 2f, previewTotalLoadAreaSize));
        }
    }
}
```