// Unity Design Pattern Example: LandscapeStreaming
// This script demonstrates the LandscapeStreaming pattern in Unity
// Generated automatically - ready to use in your Unity project

The `LandscapeStreaming` design pattern is a fundamental technique for managing large, open game worlds in Unity (and other engines). It addresses the challenge of rendering vast environments without consuming excessive memory or performance by dynamically loading and unloading portions of the world based on the player's position.

Here's a complete, practical C# Unity example demonstrating this pattern.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For Dictionary and HashSet

// The LandscapeStreaming design pattern is used to dynamically load and unload
// portions of a large game world (the "landscape" or "terrain") based on the
// player's proximity. This is crucial for managing memory and improving performance
// in open-world games by ensuring only relevant parts of the world are active
// at any given time.

// How it works:
// 1. The world is conceptually divided into smaller, manageable "chunks" or "tiles".
// 2. A central system (this script) continuously tracks the player's position.
// 3. Based on the player's current chunk coordinates and a predefined "streaming distance",
//    it determines which chunks should currently be active (loaded/instantiated/visible).
// 4. Chunks that fall within this streaming distance are activated (if not already).
// 5. Chunks that move outside this streaming distance are deactivated (unloaded/destroyed).
// 6. This entire process typically runs periodically (e.g., every 0.5-1 second), not every frame,
//    to balance responsiveness with performance overhead.

// Benefits:
// - Reduces memory usage significantly by not loading the entire world into RAM.
// - Improves performance by decreasing the number of active GameObjects, physics calculations,
//   rendering calls, and script updates.
// - Allows for the creation of much larger, more detailed, and seamless worlds than
//   would otherwise be possible.

// Drawbacks:
// - Can introduce "pop-in" if chunk loading/unloading isn't handled smoothly (e.g.,
//   without asynchronous loading, fading, or clever procedural generation).
// - Requires careful chunk coordinate management and a robust system for loading/saving chunk data.
// - Initial setup and configuration can be complex depending on the world's complexity.

public class LandscapeStreamer : MonoBehaviour
{
    [Header("Player & Chunks")]
    [Tooltip("The Transform of the player or main camera that determines the center of the active chunk grid.")]
    public Transform playerTransform;

    [Tooltip("The prefab to use for each landscape chunk. This should be a GameObject " +
             "representing a piece of your world, scaled appropriately.")]
    public GameObject chunkPrefab;

    [Tooltip("The size (width and depth) of each individual chunk in world units. " +
             "e.g., a value of 100 means each chunk occupies a 100x100 unit square.")]
    public float chunkSize = 100f;

    [Tooltip("The number of chunks to keep active around the player in each cardinal direction (X and Z). " +
             "For example, if set to '1', it will load a 3x3 grid (player's chunk + 1 chunk on each side). " +
             "If set to '2', it loads a 5x5 grid (player's chunk + 2 chunks on each side).")]
    public int streamingDistanceChunks = 2; // Results in a (2*N+1) x (2*N+1) grid, so (2*2+1)x(2*2+1) = 5x5 grid.

    [Tooltip("How often (in seconds) the system should check the player's position and update " +
             "which chunks are active. A higher value reduces CPU usage but might cause " +
             "chunks to appear or disappear with a slight delay.")]
    public float updateIntervalSeconds = 1.0f;

    [Tooltip("An optional parent Transform for all instantiated chunks. This helps keep the " +
             "Unity Hierarchy organized, especially with many chunks.")]
    public Transform chunksParent;

    // A dictionary to efficiently keep track of currently active chunks.
    // Key: Vector2Int representing the chunk's grid coordinates (x, z).
    // Value: The GameObject instance of the chunk that has been instantiated.
    private Dictionary<Vector2Int, GameObject> _activeChunks = new Dictionary<Vector2Int, GameObject>();

    // Stores the chunk coordinates where the player was last located.
    // Used to detect when the player moves to a new chunk, which often triggers
    // a necessary world update sooner than the updateIntervalSeconds.
    private Vector2Int _lastPlayerChunkCoords;

    // A flag to prevent multiple instances of the streaming coroutine from running
    // simultaneously, which could lead to unpredictable behavior.
    private bool _isUpdating = false;

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // If no custom chunksParent is assigned in the Inspector, create a new GameObject
        // to serve as the parent. This keeps the Hierarchy clean by grouping all instantiated chunks.
        if (chunksParent == null)
        {
            GameObject parentGO = new GameObject("LandscapeChunks");
            chunksParent = parentGO.transform;
        }

        // Perform basic validation to ensure essential components are assigned.
        // If critical references are missing, log an error and disable the script to prevent runtime errors.
        if (playerTransform == null)
        {
            Debug.LogError("LandscapeStreamer: Player Transform is not assigned! Please assign the player's Transform in the Inspector.", this);
            enabled = false; // Disable this component if it can't function.
            return;
        }
        if (chunkPrefab == null)
        {
            Debug.LogError("LandscapeStreamer: Chunk Prefab is not assigned! Please assign a GameObject prefab for chunks in the Inspector.", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Begin the continuous chunk streaming process when the game starts.
        StartStreaming();
    }

    // --- Core Streaming Logic Methods ---

    /// <summary>
    /// Initiates the chunk streaming coroutine.
    /// </summary>
    public void StartStreaming()
    {
        // Only start the coroutine if it's not already running.
        if (!_isUpdating)
        {
            _isUpdating = true;
            StartCoroutine(StreamChunksCoroutine());
        }
    }

    /// <summary>
    /// Halts the chunk streaming coroutine and cleans up.
    /// </summary>
    public void StopStreaming()
    {
        // Only stop if the coroutine is currently active.
        if (_isUpdating)
        {
            _isUpdating = false;
            // Stop all coroutines on this specific GameObject to ensure the streaming one is halted.
            StopAllCoroutines();
            Debug.Log("Landscape streaming stopped.");
        }
    }

    /// <summary>
    /// The main coroutine that periodically checks the player's position and triggers
    /// the activation/deactivation of landscape chunks.
    /// </summary>
    private IEnumerator StreamChunksCoroutine()
    {
        // Perform an initial update immediately when the game starts or streaming begins,
        // so that chunks around the player are loaded without delay.
        UpdateChunks();

        // Loop indefinitely as long as streaming is active.
        while (_isUpdating)
        {
            // Pause the coroutine for the specified interval. This prevents
            // the system from checking for chunk updates every single frame,
            // saving CPU resources.
            yield return new WaitForSeconds(updateIntervalSeconds);

            // After the delay, perform the chunk update logic.
            UpdateChunks();
        }
    }

    /// <summary>
    /// This method is the core logic for deciding which chunks should be active.
    /// It calculates the required chunks based on player position and streaming distance,
    /// then activates new ones and deactivates old ones.
    /// </summary>
    private void UpdateChunks()
    {
        // 1. Determine the player's current chunk coordinates.
        Vector2Int playerCurrentChunkCoords = CalculateChunkCoordinates(playerTransform.position);

        // 2. Optimization: Check if the player has moved to a *new* chunk.
        // If the player is still within the same chunk as the last update, and we already
        // have chunks loaded, we can often skip the expensive chunk recalculation.
        // The `_activeChunks.Count > 0` condition ensures it always runs on the very first call.
        if (playerCurrentChunkCoords == _lastPlayerChunkCoords && _activeChunks.Count > 0)
        {
            return; // No change in player's chunk, so no need to update the entire grid.
        }

        // If the player moved to a new chunk or it's the initial load, update the last known position.
        _lastPlayerChunkCoords = playerCurrentChunkCoords;

        // 3. Identify chunks that *should* be active.
        // Use a HashSet for efficient lookups (O(1) average time complexity) to check
        // if a chunk is supposed to be active.
        HashSet<Vector2Int> chunksToKeepActive = new HashSet<Vector2Int>();

        // Iterate through a square grid of chunks centered on the player's current chunk.
        // `streamingDistanceChunks` defines the radius of this grid.
        for (int xOffset = -streamingDistanceChunks; xOffset <= streamingDistanceChunks; xOffset++)
        {
            for (int zOffset = -streamingDistanceChunks; zOffset <= streamingDistanceChunks; zOffset++)
            {
                // Calculate the world grid coordinates for the current chunk in the loop.
                Vector2Int currentChunkCoords = new Vector2Int(
                    playerCurrentChunkCoords.x + xOffset,
                    playerCurrentChunkCoords.y + zOffset // In a 3D world, Vector2Int.y often maps to the Z-axis.
                );

                // Add these coordinates to our set of chunks that must be active.
                chunksToKeepActive.Add(currentChunkCoords);

                // If this chunk is not yet present in our `_activeChunks` dictionary, it means
                // it needs to be created or loaded.
                if (!_activeChunks.ContainsKey(currentChunkCoords))
                {
                    ActivateChunk(currentChunkCoords);
                }
            }
        }

        // 4. Identify chunks that *should no longer* be active.
        // Create a temporary list to hold coordinates of chunks that are currently loaded
        // but are now outside the streaming distance. We collect them first to avoid
        // modifying the `_activeChunks` dictionary while iterating over it.
        List<Vector2Int> chunksToDeactivate = new List<Vector2Int>();
        foreach (Vector2Int activeChunkCoords in _activeChunks.Keys)
        {
            // If an active chunk's coordinates are NOT found in our `chunksToKeepActive` set,
            // it means the player has moved too far away from it.
            if (!chunksToKeepActive.Contains(activeChunkCoords))
            {
                chunksToDeactivate.Add(activeChunkCoords);
            }
        }

        // 5. Deactivate (destroy) the identified chunks.
        foreach (Vector2Int chunkCoords in chunksToDeactivate)
        {
            // Retrieve the GameObject instance associated with these coordinates.
            // It's safe to assume it exists because we just got the key from `_activeChunks.Keys`.
            GameObject chunkObject = _activeChunks[chunkCoords];
            DeactivateChunk(chunkCoords, chunkObject);
        }
    }

    /// <summary>
    /// Handles the process of making a chunk active (e.g., instantiating its prefab).
    /// </summary>
    /// <param name="chunkCoords">The grid coordinates (x, z) of the chunk to activate.</param>
    private void ActivateChunk(Vector2Int chunkCoords)
    {
        // Calculate the exact world position for the center of this chunk.
        Vector3 worldPosition = CalculateChunkWorldPosition(chunkCoords);

        // Instantiate the chunk prefab at the calculated world position.
        // In a more advanced real-world scenario, this might involve:
        // - Loading an AssetBundle or Addressable asset asynchronously.
        // - Loading a specific sub-scene using `SceneManager.LoadSceneAsync`.
        // - Triggering a procedural generation routine for the chunk's content.
        GameObject newChunk = Instantiate(chunkPrefab, worldPosition, Quaternion.identity);

        // Assign a descriptive name for easier debugging in the Unity Hierarchy.
        newChunk.name = $"Chunk_{chunkCoords.x}_{chunkCoords.y}";

        // Parent the new chunk GameObject to the designated `chunksParent` for organization.
        if (chunksParent != null)
        {
            newChunk.transform.SetParent(chunksParent);
        }

        // Add the newly created chunk to our dictionary of currently active chunks.
        _activeChunks.Add(chunkCoords, newChunk);

        // Optional: Debug log for visibility in the Console.
        // Debug.Log($"Activated Chunk: {newChunk.name} at {worldPosition}", newChunk);

        // In a real game, you might trigger events or call methods on the chunk itself here,
        // e.g., `newChunk.GetComponent<ChunkController>()?.Initialize(chunkCoords);`
    }

    /// <summary>
    /// Handles the process of making a chunk inactive (e.g., destroying its GameObject).
    /// </summary>
    /// <param name="chunkCoords">The grid coordinates (x, z) of the chunk to deactivate.</param>
    /// <param name="chunkObject">The GameObject instance of the chunk to deactivate.</param>
    private void DeactivateChunk(Vector2Int chunkCoords, GameObject chunkObject)
    {
        // Remove the chunk from our dictionary of active chunks.
        _activeChunks.Remove(chunkCoords);

        // Destroy the GameObject.
        // For larger projects and performance optimization, instead of immediate destruction,
        // you would typically:
        // 1. Deactivate the GameObject (`chunkObject.SetActive(false)`) and return it to an
        //    Object Pool for reuse, reducing garbage collection and instantiation overhead.
        // 2. Unload the scene if the chunk is managed as a separate additive scene.
        // 3. Trigger a mechanism to save any dynamic changes that occurred within the chunk
        //    (e.g., player-built structures, moved items).
        Destroy(chunkObject);

        // Optional: Debug log for visibility.
        // Debug.Log($"Deactivated Chunk: {chunkObject.name}");

        // Similar to activation, you might trigger cleanup events on the chunk itself,
        // e.g., `chunkObject.GetComponent<ChunkController>()?.OnChunkDeactivated();`
    }

    // --- Helper Methods for Coordinate Conversion ---

    /// <summary>
    /// Converts a world space position (Vector3) into grid-based chunk coordinates (Vector2Int).
    /// </summary>
    /// <param name="worldPosition">The world space position (e.g., player's Transform.position).</param>
    /// <returns>A Vector2Int where x is the chunk's X-coordinate and y is the chunk's Z-coordinate.</returns>
    private Vector2Int CalculateChunkCoordinates(Vector3 worldPosition)
    {
        // Using `Mathf.FloorToInt` is crucial here. It correctly handles negative coordinates.
        // For example, if chunkSize=100:
        // - A world position of (50, Y, 50) falls into chunk (0, 0).
        // - A world position of (101, Y, 101) falls into chunk (1, 1).
        // - A world position of (-50, Y, -50) correctly falls into chunk (-1, -1), not (0, 0).
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int z = Mathf.FloorToInt(worldPosition.z / chunkSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Converts grid-based chunk coordinates (Vector2Int) into the world position of the chunk's center.
    /// </summary>
    /// <param name="chunkCoords">The grid coordinates (x, z) of the chunk.</param>
    /// <returns>A Vector3 representing the world space position of the chunk's center.</returns>
    private Vector3 CalculateChunkWorldPosition(Vector2Int chunkCoords)
    {
        // Calculate the world coordinates of the chunk's bottom-left corner.
        float xPos = chunkCoords.x * chunkSize;
        float zPos = chunkCoords.y * chunkSize; // Vector2Int.y is used for the 3D world's Z-axis.

        // To get the center of the chunk, add half the `chunkSize` to both X and Z.
        // The Y-coordinate (height) is assumed to be 0 for a flat terrain; adjust this
        // if your chunks have specific ground heights or an initial offset.
        return new Vector3(xPos + chunkSize * 0.5f, 0f, zPos + chunkSize * 0.5f);
    }

    // --- Editor-only Debugging for Visualization ---

    /// <summary>
    /// Draws visual helpers in the Scene view during editor play mode to show active chunks.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw gizmos if the application is playing and we have active chunks to visualize.
        if (Application.isPlaying && _activeChunks != null && _activeChunks.Count > 0)
        {
            // Draw all currently active chunks in green.
            Gizmos.color = Color.green;
            foreach (var kvp in _activeChunks)
            {
                // Calculate the center of each chunk for drawing.
                Vector3 center = CalculateChunkWorldPosition(kvp.Key);
                // Draw a wireframe cube to represent the chunk boundary.
                // The Y-height of 1f is arbitrary, just for visualization.
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1f, chunkSize));
            }

            // If a playerTransform is assigned, highlight the chunk the player is currently in.
            if (playerTransform != null)
            {
                Gizmos.color = Color.yellow; // Use a distinct color for the player's current chunk.
                Vector2Int playerCurrentChunkCoords = CalculateChunkCoordinates(playerTransform.position);
                Vector3 playerChunkCenter = CalculateChunkWorldPosition(playerCurrentChunkCoords);
                // Draw a slightly taller wireframe cube for emphasis.
                Gizmos.DrawWireCube(playerChunkCenter, new Vector3(chunkSize, 2f, chunkSize));
            }
        }
    }
}

/*
 * --- EXAMPLE USAGE IN UNITY PROJECT ---
 *
 * Follow these steps to set up and run the LandscapeStreaming example in your Unity project:
 *
 * 1.  Create a new Unity Project or open an existing one.
 *
 * 2.  **Create the C# Script:**
 *     - Create a new C# script named `LandscapeStreamer.cs` (or copy the entire code above).
 *     - Place it in your project's `Assets` folder (e.g., `Assets/Scripts/`).
 *
 * 3.  **Create a 'Player' GameObject:**
 *     - In the Unity Hierarchy window, right-click -> 3D Object -> Cube.
 *     - Name this new GameObject "Player".
 *     - Position it at `(0, 0.5, 0)` in the Inspector.
 *     - To make it movable, you can add a simple movement script (e.g., WASD keys) or just
 *       manually move its Transform using the Unity editor tools while in Play mode.
 *     - (Optional) For a more realistic test, you could use Unity's `CharacterController` or a simple Rigidbody setup.
 *
 * 4.  **Create a 'Chunk Prefab':**
 *     - In the Hierarchy, right-click -> 3D Object -> Cube.
 *     - Name this new GameObject "Chunk_Prefab_Template".
 *     - In the Inspector, adjust its `Scale` to match your desired `chunkSize`.
 *       If `chunkSize` in the script is 100, set the scale to `(100, 1, 100)`.
 *       (The '1' for Y-scale is just for a flat visual; your actual chunks can have complex geometry).
 *     - You can add materials, textures, or more detailed models to this Cube to make it look like terrain.
 *     - Drag this "Chunk_Prefab_Template" GameObject from the Hierarchy into your Project tab
 *       (e.g., `Assets/Prefabs/`) to create a Prefab.
 *     - After creating the Prefab, delete the "Chunk_Prefab_Template" GameObject from the Hierarchy.
 *
 * 5.  **Create the 'LandscapeStreamer' GameObject:**
 *     - In the Hierarchy, right-click -> Create Empty.
 *     - Name this new GameObject "LandscapeStreamer".
 *     - Drag the `LandscapeStreamer.cs` script (from step 2) onto this "LandscapeStreamer" GameObject in the Hierarchy.
 *
 * 6.  **Configure the `LandscapeStreamer` component in the Inspector:**
 *     - **Player Transform:** Drag your "Player" GameObject from the Hierarchy into this slot.
 *     - **Chunk Prefab:** Drag your "Chunk_Prefab_Template" Prefab from the Project tab into this slot.
 *     - **Chunk Size:** Set this to your desired chunk dimension (e.g., `100`). This should match the scale you set for your prefab.
 *     - **Streaming Distance Chunks:** Set this value (e.g., `2`). This will make the system load a `(2*2+1)x(2*2+1) = 5x5` grid of chunks around the player.
 *     - **Update Interval Seconds:** Set how often the system checks for updates (e.g., `0.5` seconds).
 *     - **Chunks Parent:** (Optional) You can leave this empty, and the script will create a parent GameObject named "LandscapeChunks" automatically.
 *
 * 7.  **Run the Scene:**
 *     - Press the Play button in the Unity editor.
 *     - You should immediately see a grid of your "Chunk_Prefab_Template" cubes appear around your "Player" GameObject.
 *     - Move your "Player" GameObject in the Scene view (or using your movement script).
 *     - As the player moves, you will observe chunks outside the streaming distance disappearing (being destroyed), and new chunks appearing (being instantiated) at the edge of the streaming area.
 *     - In the Scene view, `OnDrawGizmos` will draw green wireframe cubes for all active chunks, and a yellow wireframe cube highlighting the chunk your player is currently occupying.
 *
 * --- Further Improvements and Real-World Considerations ---
 *
 * This example provides a solid foundation. For a production-ready game, consider these enhancements:
 *
 * -   **Object Pooling:** Instead of `Instantiate()` and `Destroy()`, use an object pooling system for chunks. This significantly reduces garbage collection overhead and improves performance, especially with frequent chunk activation/deactivation.
 * -   **Asynchronous Loading:** For very complex chunks (e.g., large models, many sub-objects, or procedural generation), loading them on the main thread can cause frame rate hitches. Implement asynchronous loading using Unity's `Addressables` system, `AssetBundles`, `SceneManager.LoadSceneAsync`, or C# Tasks/Job System for procedural generation.
 * -   **Level of Detail (LOD):** Implement different levels of detail for chunks. Chunks closer to the player could have high-resolution models and textures, while distant chunks could use simpler meshes and lower-res assets to optimize rendering.
 * -   **Visual Transitions:** To avoid "pop-in" of new chunks, implement smooth visual transitions like fading in/out, or apply a fog effect to hide the loading boundaries.
 * -   **Chunk Content Management:** Beyond just terrain, chunks might contain NPCs, interactive objects, foliage, or even entire sub-scenes. The `ActivateChunk` and `DeactivateChunk` methods would need to be extended to properly manage the lifecycle of these contents (e.g., enabling/disabling AI, activating scripts, loading their own data).
 * -   **World Origin Shifting:** For truly massive open worlds (thousands of kilometers), floating-point precision issues can arise far from the world origin. Implement a "World Origin Shifting" system that periodically resets the world's origin to the player's position, shifting all objects accordingly.
 * -   **Saving/Loading Chunk State:** If players can modify chunks (e.g., build structures, dig holes), you'll need a robust system to save the state of a chunk when it deactivates and load it back when it activates.
 * -   **Seamless Generation:** For procedural terrains, techniques like Marching Cubes or Perlin noise can be used to generate terrain geometry on the fly, often on a separate thread using Unity's Job System, to ensure a seamless visual experience.
 */
```