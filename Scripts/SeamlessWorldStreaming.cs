// Unity Design Pattern Example: SeamlessWorldStreaming
// This script demonstrates the SeamlessWorldStreaming pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Seamless World Streaming' design pattern is crucial for creating large open-world games in Unity. It allows you to load and unload portions of your game world (often called "chunks" or "tiles") dynamically as the player moves, preventing memory overload and eliminating visible loading screens.

This C# Unity script provides a complete and practical implementation of this pattern using Unity's additive scene loading capabilities.

---

### SeamlessWorldStreamer.cs

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SeamlessWorldStreamer: Implements the Seamless World Streaming design pattern in Unity.
///
/// This pattern dynamically loads and unloads parts of the game world (called 'chunks' or 'scenes')
/// based on the player's position, making the transition between different areas smooth and
/// eliminating visible loading screens. It's crucial for large open-world games.
///
/// How the Seamless World Streaming Pattern Works:
/// 1.  **Chunks as Additive Scenes**: Each distinct part of your game world is created as a separate Unity scene.
///     These scenes contain all the necessary geometry, lighting, props, and any specific scripts for that area.
///     They are loaded additively into your main game scene, meaning they coexist with the main scene without replacing it.
/// 2.  **Player/Observer Tracking**: The system continuously monitors the position of a designated observer, typically the player character or the main camera.
/// 3.  **Chunk Grid**: The entire game world is conceptually divided into a uniform grid of 'chunks'. Each chunk is identified by its unique grid coordinates (e.g., (0,0), (1,0), (-1,-1)).
/// 4.  **Render Distance (Active Area)**: A configurable radius (measured in chunks) around the player defines the 'active' area. All chunks within this radius are considered "in-range" and should be loaded and visible to the player.
/// 5.  **Dynamic Loading/Unloading Logic**:
///     -   **Loading**: When the player moves into a new chunk, the system calculates which chunks are now within the render distance but are not currently loaded. It then initiates their asynchronous loading.
///     -   **Unloading**: Simultaneously, it identifies previously loaded chunks that are now outside the render distance. These chunks are marked for asynchronous unloading.
/// 6.  **Asynchronous Operations**: All loading and unloading operations are performed asynchronously (`SceneManager.LoadSceneAsync`, `SceneManager.UnloadSceneAsync`). This is critical to prevent the game from freezing or stuttering, ensuring a truly 'seamless' experience.
/// 7.  **Operation Management**: A central manager (this `SeamlessWorldStreamer` script) keeps track of currently loaded scenes, manages queues for pending load/unload operations, and limits the number of concurrent operations to maintain performance.
///
/// Practical Use Cases:
/// -   Large open-world games (MMORPGs, survival games, expansive adventure titles).
/// -   Games with procedural generation where new chunks are created and streamed on the fly.
/// -   Architectural or scientific visualizations that require navigating vast environments.
///
/// Advantages:
/// -   **Optimized Memory Usage**: Only relevant parts of the world are kept in memory, significantly reducing the game's memory footprint.
/// -   **Scalability**: Enables the creation of extremely large game worlds without hitting Unity's scene size limits or performance bottlenecks associated with huge monolithic scenes.
/// -   **Seamless Experience**: Provides a continuous gameplay experience without disruptive loading screens, enhancing player immersion.
///
/// Disadvantages:
/// -   **Complex Setup**: Requires careful organization and setup of chunk scenes, including proper naming conventions and placement in the Build Settings.
/// -   **Potential for Hitches**: If chunk assets are very large or loading/unloading logic is not optimized, subtle framerate drops can occur.
/// -   **Debugging Complexity**: Dynamic scene changes can make debugging more challenging.
///
///
/// ### How to use this script in your Unity project:
///
/// **1. Main Scene Setup:**
///    a. Create a new Unity scene (e.g., "MainScene").
///    b. Add a `Player` GameObject (e.g., a Capsule, character controller, or any GameObject with a `Transform` that represents the observer). Ensure it has a way to move.
///    c. Create an empty GameObject in "MainScene" and name it something like "WorldStreamer".
///    d. Attach this `SeamlessWorldStreamer.cs` script to the "WorldStreamer" GameObject.
///
/// **2. Configure the Streamer Component:**
///    a. In the Inspector for "WorldStreamer", drag your `Player` GameObject into the `Player Transform` field.
///    b. Set `Chunk Size`: This defines the real-world size (e.g., 100 for 100x100 units) of each chunk.
///    c. Set `Render Distance Chunks`: This determines how many chunks around the player will be loaded. A value of `2` means a 5x5 grid of chunks (player's chunk + 2 in each cardinal direction).
///    d. Adjust `Update Interval Seconds`, `Max Concurrent Loads`, and `Max Concurrent Unloads` for performance.
///
/// **3. Create your Chunk Scenes:**
///    a. In your Project window, create multiple new scenes. Name them according to the pattern: `chunkSceneNamesPrefix` + `_X_Y`.
///       For example, if `chunkSceneNamesPrefix` is "Chunk_", create scenes like:
///       - "Chunk_0_0" (for the chunk at grid coordinates (0,0))
///       - "Chunk_1_0" (for the chunk at grid coordinates (1,0))
///       - "Chunk_-1_1" (for the chunk at grid coordinates (-1,1))
///       - etc.
///    b. Open each chunk scene and add some visual content (e.g., a Quad, Cube, or complex prefabs) to represent that part of the world.
///    c. **Crucially, position the content within each chunk scene correctly**: The origin (0,0,0) of the chunk scene should correspond to the bottom-left corner (min-X, min-Z) of that chunk's world-space area. For example:
///       - In "Chunk_0_0", place content around `(0, 0, 0)` to `(chunkSize, 0, chunkSize)`.
///       - In "Chunk_1_0", place content around `(chunkSize, 0, 0)` to `(2*chunkSize, 0, chunkSize)`.
///       - In "Chunk_X_Y", place its content such that its bounding box roughly spans from
///         `(X * chunkSize, 0, Y * chunkSize)` to `((X+1) * chunkSize, 0, (Y+1) * chunkSize)`.
///       This ensures that when loaded additively, the chunks align seamlessly in the world.
///
/// **4. Add Chunk Scenes to Build Settings:**
///    a. Go to `File > Build Settings...`
///    b. Drag all your "Chunk_X_Y" scenes (and your "MainScene") into the "Scenes In Build" list.
///       **This step is absolutely critical! `SceneManager.LoadSceneAsync` can only load scenes that are included in the build.**
///
/// Once set up, run your game. As the player moves, you should see chunks load and unload dynamically around them. The debug gizmos (visible in the Scene view while the game is running) will show you which chunks are loaded, loading, and unloading.
/// </summary>
public class SeamlessWorldStreamer : MonoBehaviour
{
    [Header("Player/Observer Settings")]
    [Tooltip("The Transform of the player or camera that dictates which chunks should be loaded.")]
    public Transform playerTransform;

    [Header("Chunk Settings")]
    [Tooltip("The prefix used for your chunk scene names (e.g., 'Chunk_' for scenes like 'Chunk_0_0').")]
    public string chunkSceneNamesPrefix = "Chunk_";
    [Tooltip("The size of each chunk in world units (e.g., 100 for 100x100 unit chunks).")]
    public float chunkSize = 100f;
    [Tooltip("How many chunks in each cardinal direction (X and Z) from the player should be loaded." +
             " A value of 1 means a 3x3 grid (player's chunk + 1 on each side). A value of 2 means a 5x5 grid.")]
    public int renderDistanceChunks = 2;

    [Header("Performance Settings")]
    [Tooltip("How often, in seconds, the system checks for new chunks to load/unload.")]
    public float updateIntervalSeconds = 1.0f;
    [Tooltip("The maximum number of concurrent asynchronous loading operations. Prevents performance spikes.")]
    public int maxConcurrentLoads = 3;
    [Tooltip("The maximum number of concurrent asynchronous unloading operations. Prevents performance spikes.")]
    public int maxConcurrentUnloads = 3;

    [Header("Debug Settings")]
    [Tooltip("Color to draw debug gizmos for loaded chunks in the editor.")]
    public Color debugGizmoColor = Color.cyan;

    // Internal state: Tracks which chunks are currently loaded and their coordinates.
    private HashSet<Vector2Int> _currentLoadedChunks = new HashSet<Vector2Int>();
    // Stores the player's last known chunk coordinates to detect when they cross a chunk boundary.
    private Vector2Int _lastPlayerChunkCoords = Vector2Int.zero;
    // Timer to control the frequency of chunk updates.
    private float _lastUpdateTime;

    // Queues for managing pending load/unload operations asynchronously.
    private Queue<Vector2Int> _chunksToLoadQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> _chunksToUnloadQueue = new Queue<Vector2Int>();

    // Lists to hold active AsyncOperation objects for currently running loads/unloads.
    private List<AsyncOperation> _activeLoadingOperations = new List<AsyncOperation>();
    private List<AsyncOperation> _activeUnloadingOperations = new List<AsyncOperation>();

    private void Awake()
    {
        // Basic validation: Ensure the playerTransform is assigned.
        if (playerTransform == null)
        {
            Debug.LogError("SeamlessWorldStreamer: playerTransform is not assigned! Please assign the player's Transform in the Inspector.", this);
            enabled = false; // Disable component if essential setup is missing
            return;
        }

        // Initialize the last player chunk coordinates based on the player's starting position.
        _lastPlayerChunkCoords = GetChunkCoordinatesFromWorldPosition(playerTransform.position);
        // Immediately update the chunk state to load the initial chunks around the player.
        UpdateChunkState(_lastPlayerChunkCoords);
        _lastUpdateTime = Time.time; // Initialize the update timer.
        // Start the coroutine that continuously processes chunk loading and unloading operations.
        StartCoroutine(ProcessChunkOperations());
    }

    private void Update()
    {
        // Periodically check player position to see if they've moved to a new chunk.
        // This avoids checking every single frame, optimizing performance.
        if (Time.time - _lastUpdateTime >= updateIntervalSeconds)
        {
            Vector2Int currentPlayerChunkCoords = GetChunkCoordinatesFromWorldPosition(playerTransform.position);

            // If the player has moved into a different chunk, update the world state.
            if (currentPlayerChunkCoords != _lastPlayerChunkCoords)
            {
                Debug.Log($"Player moved from chunk {_lastPlayerChunkCoords} to {currentPlayerChunkCoords}");
                _lastPlayerChunkCoords = currentPlayerChunkCoords;
                UpdateChunkState(currentPlayerChunkCoords);
            }
            _lastUpdateTime = Time.time; // Reset the timer.
        }
    }

    /// <summary>
    /// Coroutine to process loading and unloading operations asynchronously.
    /// This runs continuously, managing the operation queues and ensuring that
    /// `maxConcurrentLoads`/`maxConcurrentUnloads` limits are respected.
    /// </summary>
    private IEnumerator ProcessChunkOperations()
    {
        while (true) // Infinite loop for continuous streaming
        {
            // Clean up any loading operations that have completed.
            _activeLoadingOperations.RemoveAll(op => op.isDone);
            // Clean up any unloading operations that have completed.
            _activeUnloadingOperations.RemoveAll(op => op.isDone);

            // Start new loading operations if there are chunks in the queue and we haven't hit the concurrent limit.
            while (_chunksToLoadQueue.Count > 0 && _activeLoadingOperations.Count < maxConcurrentLoads)
            {
                Vector2Int chunkCoords = _chunksToLoadQueue.Dequeue();
                LoadChunkAsync(chunkCoords); // Initiate the async load.
            }

            // Start new unloading operations if there are chunks in the queue and we haven't hit the concurrent limit.
            while (_chunksToUnloadQueue.Count > 0 && _activeUnloadingOperations.Count < maxConcurrentUnloads)
            {
                Vector2Int chunkCoords = _chunksToUnloadQueue.Dequeue();
                UnloadChunkAsync(chunkCoords); // Initiate the async unload.
            }

            yield return null; // Wait for the next frame before checking again.
        }
    }

    /// <summary>
    /// Determines which chunks should be loaded or unloaded based on the player's current position
    /// and the defined render distance. It populates the `_chunksToLoadQueue` and `_chunksToUnloadQueue`.
    /// </summary>
    /// <param name="playerChunkCoords">The grid coordinates of the chunk the player is currently in.</param>
    private void UpdateChunkState(Vector2Int playerChunkCoords)
    {
        // 1. Identify all chunks that *should* be loaded based on the player's current position and render distance.
        HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        for (int x = -renderDistanceChunks; x <= renderDistanceChunks; x++)
        {
            for (int z = -renderDistanceChunks; z <= renderDistanceChunks; z++)
            {
                requiredChunks.Add(new Vector2Int(playerChunkCoords.x + x, playerChunkCoords.y + z));
            }
        }

        // 2. Enqueue chunks for loading.
        // Iterate through all required chunks. If a chunk is not currently loaded AND not already in the load queue, add it.
        foreach (Vector2Int chunkCoords in requiredChunks)
        {
            if (!_currentLoadedChunks.Contains(chunkCoords) && !_chunksToLoadQueue.Contains(chunkCoords))
            {
                _chunksToLoadQueue.Enqueue(chunkCoords);
                Debug.Log($"Queued chunk for loading: {chunkCoords}");
            }
        }

        // 3. Enqueue chunks for unloading.
        // Create a temporary list to hold chunks identified for unloading to avoid modifying _currentLoadedChunks during iteration.
        List<Vector2Int> chunksToUnloadNow = new List<Vector2Int>();
        // Iterate through all currently loaded chunks.
        foreach (Vector2Int chunkCoords in _currentLoadedChunks)
        {
            // If a loaded chunk is no longer required AND not already in the unload queue, mark it for unloading.
            if (!requiredChunks.Contains(chunkCoords) && !_chunksToUnloadQueue.Contains(chunkCoords))
            {
                chunksToUnloadNow.Add(chunkCoords);
            }
        }
        // Add marked chunks to the unload queue.
        foreach (Vector2Int chunkCoords in chunksToUnloadNow)
        {
            _chunksToUnloadQueue.Enqueue(chunkCoords);
            Debug.Log($"Queued chunk for unloading: {chunkCoords}");
        }
    }

    /// <summary>
    /// Initiates an asynchronous loading operation for a specific chunk scene.
    /// </summary>
    private void LoadChunkAsync(Vector2Int chunkCoords)
    {
        string sceneName = GetChunkSceneName(chunkCoords);

        // Before loading, check if the chunk is already in our loaded set or actively being loaded.
        if (_currentLoadedChunks.Contains(chunkCoords) || _activeLoadingOperations.Any(op => op.assetPath == sceneName))
        {
            // This might happen if a chunk was queued, but then became loaded by another mechanism
            // or if the player quickly moved back and forth.
            // Ensure it's in our _currentLoadedChunks set if it's indeed loaded.
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                _currentLoadedChunks.Add(chunkCoords); // Add it just in case
            }
            return;
        }

        // Check if the scene exists in the Build Settings. This is a crucial validation.
        // SceneUtility.GetBuildIndexByScenePath works for asset paths (like "Assets/Scenes/Chunk_0_0.unity")
        // or by scene name if it's unique. For this pattern, scene name is usually unique enough.
        // We'll rely on the simpler SceneManager.GetSceneByName().isLoaded check for loaded state
        // and a general BuildIndex check for existence for robustness.
        // NOTE: For scene names like "Chunk_0_0", SceneUtility.GetBuildIndexByScenePath might not work directly
        // if the path isn't "Assets/MyScenes/Chunk_0_0.unity". Using SceneManager.GetSceneByPath is similar.
        // A more reliable check for existence *in build settings* is to iterate `EditorBuildSettings.scenes`
        // in editor, but for runtime, we rely on LoadSceneAsync to fail gracefully.
        // For simplicity, we assume `LoadSceneAsync` will log its own error if the scene isn't found,
        // but a custom pre-check can be added if needed.

        Debug.Log($"Starting async load for scene: {sceneName} (Chunk: {chunkCoords})");
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        _activeLoadingOperations.Add(op); // Add to active operations list
        op.completed += (asyncOp) => OnChunkLoaded(chunkCoords, sceneName, op); // Attach a callback
    }

    /// <summary>
    /// Callback method invoked when an asynchronous chunk scene loading operation completes.
    /// </summary>
    private void OnChunkLoaded(Vector2Int chunkCoords, string sceneName, AsyncOperation op)
    {
        _currentLoadedChunks.Add(chunkCoords); // Mark the chunk as loaded in our internal set.
        
        // Retrieve the loaded scene to perform any post-load setup if necessary.
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            // Optionally, you might want to set the loaded scene as the active scene,
            // though for additive streaming, this is often not strictly required
            // as objects in the new scene are active immediately.
            // SceneManager.SetActiveScene(loadedScene); 
            Debug.Log($"Successfully loaded scene: {sceneName}. Total loaded chunks: {_currentLoadedChunks.Count}");
        }
        else
        {
            Debug.LogError($"SeamlessWorldStreamer: Failed to retrieve or validate scene '{sceneName}' after load completion for chunk {chunkCoords}.");
        }
    }

    /// <summary>
    /// Initiates an asynchronous unloading operation for a specific chunk scene.
    /// </summary>
    private void UnloadChunkAsync(Vector2Int chunkCoords)
    {
        string sceneName = GetChunkSceneName(chunkCoords);

        // Before unloading, check if the chunk is actually loaded or actively being unloaded.
        if (!_currentLoadedChunks.Contains(chunkCoords) || _activeUnloadingOperations.Any(op => op.assetPath == sceneName))
        {
            // If it's not in _currentLoadedChunks, then it's effectively already unloaded from our perspective.
            // Ensure it's removed from our set for consistency.
            _currentLoadedChunks.Remove(chunkCoords);
            return;
        }

        Debug.Log($"Starting async unload for scene: {sceneName} (Chunk: {chunkCoords})");
        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
        _activeUnloadingOperations.Add(op); // Add to active operations list
        op.completed += (asyncOp) => OnChunkUnloaded(chunkCoords, sceneName, op); // Attach a callback
    }

    /// <summary>
    /// Callback method invoked when an asynchronous chunk scene unloading operation completes.
    /// </summary>
    private void OnChunkUnloaded(Vector2Int chunkCoords, string sceneName, AsyncOperation op)
    {
        _currentLoadedChunks.Remove(chunkCoords); // Remove the chunk from our internal loaded set.
        Debug.Log($"Successfully unloaded scene: {sceneName}. Total loaded chunks: {_currentLoadedChunks.Count}");
    }

    /// <summary>
    /// Converts a world space position (Vector3) into its corresponding chunk grid coordinates (Vector2Int).
    /// This uses simple floor division, assuming chunks are aligned with the X and Z axes.
    /// </summary>
    /// <param name="worldPos">The world space position (typically the player's position).</param>
    /// <returns>A Vector2Int representing the chunk's grid coordinates (X, Y).</returns>
    private Vector2Int GetChunkCoordinatesFromWorldPosition(Vector3 worldPos)
    {
        // FloorToInt ensures that positive and negative coordinates behave as expected for grid cells.
        // E.g., for chunkSize=100:
        // worldPos.x = 50  => chunkX = 0
        // worldPos.x = 150 => chunkX = 1
        // worldPos.x = -50 => chunkX = -1
        // worldPos.x = -150 => chunkX = -2
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(worldPos.z / chunkSize); // Using Z for the 'Y' coordinate of our 2D grid
        return new Vector2Int(chunkX, chunkZ);
    }

    /// <summary>
    /// Generates the expected scene name for a given chunk coordinate, based on the prefix.
    /// Example: For (0,0) and prefix "Chunk_", returns "Chunk_0_0".
    /// </summary>
    private string GetChunkSceneName(Vector2Int chunkCoords)
    {
        return $"{chunkSceneNamesPrefix}{chunkCoords.x}_{chunkCoords.y}";
    }

    /// <summary>
    /// Checks if a chunk is considered currently loaded by our streamer.
    /// It primarily checks our internal `_currentLoadedChunks` set.
    /// </summary>
    private bool IsChunkLoaded(Vector2Int chunkCoords)
    {
        // First, check our internal hash set for efficiency.
        if (_currentLoadedChunks.Contains(chunkCoords))
        {
            // As a robustness check, we can also ask the SceneManager,
            // in case a scene was unloaded externally or an initial state mismatch.
            string sceneName = GetChunkSceneName(chunkCoords);
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded && scene.IsValid();
        }
        return false;
    }

    /// <summary>
    /// Draws debug gizmos in the editor to visually represent the chunks.
    /// This helps in understanding the streaming logic and verifying setup.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (playerTransform == null || chunkSize <= 0) return;

        // Get the current chunk the player is in for central reference.
        Vector2Int playerChunkCoords = GetChunkCoordinatesFromWorldPosition(playerTransform.position);

        // Draw the player's current chunk with a distinct color (yellow).
        DrawChunkGizmo(playerChunkCoords, Color.yellow);

        // Draw all currently loaded chunks.
        foreach (Vector2Int chunkCoords in _currentLoadedChunks)
        {
            // Avoid redrawing the player's current chunk, as it's already yellow.
            if (chunkCoords != playerChunkCoords)
            {
                DrawChunkGizmo(chunkCoords, debugGizmoColor);
            }
        }

        // Draw all chunks that are currently in the loading queue (light green).
        if (_chunksToLoadQueue.Count > 0)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.white, 0.5f); // Soft green
            foreach (Vector2Int chunkCoords in _chunksToLoadQueue)
            {
                DrawChunkGizmo(chunkCoords, Gizmos.color);
            }
        }

        // Draw all chunks that are currently in the unloading queue (light red).
        if (_chunksToUnloadQueue.Count > 0)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.white, 0.5f); // Soft red
            foreach (Vector2Int chunkCoords in _chunksToUnloadQueue)
            {
                DrawChunkGizmo(chunkCoords, Gizmos.color);
            }
        }
    }

    /// <summary>
    /// Helper method to draw a wire cube representing a chunk in the editor.
    /// </summary>
    private void DrawChunkGizmo(Vector2Int chunkCoords, Color color)
    {
        // Calculate the world-space center of the chunk.
        // Chunk (X,Y) starts at (X*chunkSize, Y*chunkSize) and ends at ((X+1)*chunkSize, (Y+1)*chunkSize).
        // The center is then (X*chunkSize + chunkSize/2, Y*chunkSize + chunkSize/2).
        Vector3 chunkCenter = new Vector3(
            chunkCoords.x * chunkSize + chunkSize / 2f,
            0f, // Assuming a flat world or drawing gizmo at Y=0. Adjust if your chunks have varying heights.
            chunkCoords.y * chunkSize + chunkSize / 2f
        );

        Gizmos.color = color;
        // Draw a wire cube outline for the chunk. The height (Y) of the cube is arbitrary for visualization.
        Gizmos.DrawWireCube(chunkCenter, new Vector3(chunkSize, 1f, chunkSize));
    }
}
```