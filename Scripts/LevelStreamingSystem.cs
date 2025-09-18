// Unity Design Pattern Example: LevelStreamingSystem
// This script demonstrates the LevelStreamingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Level Streaming System is a design pattern used in games (especially open-world or large-scale games) to manage memory and performance by dynamically loading and unloading parts of the game world (scenes, chunks, areas) based on the player's proximity, progress, or other triggers. Instead of loading the entire game world at startup, only relevant sections are active, reducing memory footprint and improving frame rates.

This example provides a practical, "drop-in" system for Unity, demonstrating how to achieve this using additive scene loading.

---

### Level Streaming System in Unity

**Core Idea:**
When the player (or camera) enters a defined streaming radius around a level chunk, that chunk's corresponding Unity scene is loaded additively. When the player moves far enough away, the scene is unloaded.

**Components:**

1.  **`StreamableLevelChunkData` (ScriptableObject):**
    *   Defines a single streamable part of your game world.
    *   Holds the `sceneName` to load.
    *   Holds `chunkBounds` (an `Bounds` object) to represent its physical location and size in the world, which is used for proximity checks.

2.  **`LevelStreamingSystem` (MonoBehaviour):**
    *   The central manager.
    *   References a `playerTransform` to track the player's position.
    *   Holds a list of `StreamableLevelChunkData` assets.
    *   Defines `streamingRadius` (when to load) and `unloadThresholdRadius` (when to unload).
    *   Periodically checks player position against all chunk bounds.
    *   Uses `SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)` and `SceneManager.UnloadSceneAsync(...)` to manage scenes.
    *   Keeps track of currently loaded and loading/unloading scenes to prevent duplicates.

---

### **1. `StreamableLevelChunkData.cs`**

This ScriptableObject defines a single streamable chunk of your level.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject representing a single streamable level chunk.
/// This asset defines a specific Unity scene to be loaded/unloaded
/// and its physical bounds in the world for proximity checks.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelChunkData", menuName = "Level Streaming/Streamable Level Chunk Data", order = 1)]
public class StreamableLevelChunkData : ScriptableObject
{
    [Tooltip("The exact name of the Unity scene asset for this chunk.")]
    public string sceneName;

    [Tooltip("The physical bounds of this chunk in world space. " +
             "Used to determine player proximity for loading/unloading.")]
    public Bounds chunkBounds;

    [Tooltip("Editor-only: Color to visualize chunk bounds in the Scene View.")]
    public Color editorGizmoColor = Color.blue;

    /// <summary>
    /// Checks if a given position is within the loading radius of this chunk.
    /// </summary>
    /// <param name="position">The world position to check against.</param>
    /// <param name="radius">The radius to consider for loading.</param>
    /// <returns>True if the position is within the loading radius, false otherwise.</returns>
    public bool IsWithinLoadingRadius(Vector3 position, float radius)
    {
        // Calculate the distance from the position to the closest point on the chunk's bounds.
        // This handles cases where the player might be inside the bounds or outside.
        Vector3 closestPoint = chunkBounds.ClosestPoint(position);
        float dist = Vector3.Distance(position, closestPoint);
        return dist <= radius;
    }

    /// <summary>
    /// Checks if a given position is outside the unloading radius of this chunk.
    /// </summary>
    /// <param name="position">The world position to check against.</param>
    /// <param name="radius">The radius to consider for unloading.</param>
    /// <returns>True if the position is outside the unloading radius, false otherwise.</returns>
    public bool IsOutsideUnloadingRadius(Vector3 position, float radius)
    {
        // Same logic as loading, but checking if the distance is greater than the radius.
        Vector3 closestPoint = chunkBounds.ClosestPoint(position);
        float dist = Vector3.Distance(position, closestPoint);
        return dist > radius;
    }

    /// <summary>
    /// Draws the chunk's bounds in the editor for visual setup.
    /// </summary>
    public void DrawGizmos()
    {
        Gizmos.color = editorGizmoColor;
        Gizmos.DrawWireCube(chunkBounds.center, chunkBounds.size);
    }
}

```

---

### **2. `LevelStreamingSystem.cs`**

This MonoBehaviour is the core manager that sits in your persistent main scene.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements the Level Streaming System design pattern.
/// This system dynamically loads and unloads Unity scenes (level chunks)
/// based on the player's proximity to defined chunk bounds.
/// </summary>
public class LevelStreamingSystem : MonoBehaviour
{
    [Header("Player Tracking")]
    [Tooltip("The transform of the player or camera that dictates scene loading/unloading.")]
    public Transform playerTransform;

    [Header("Level Chunks")]
    [Tooltip("A list of ScriptableObjects defining all streamable level chunks.")]
    public List<StreamableLevelChunkData> streamableLevelChunks = new List<StreamableLevelChunkData>();

    [Header("Streaming Settings")]
    [Tooltip("The radius around the player within which level chunks will be loaded.")]
    [Range(10f, 500f)]
    public float streamingRadius = 100f;

    [Tooltip("The radius around the player beyond which loaded level chunks will be unloaded. " +
             "Should be larger than streamingRadius to prevent flickering.")]
    [Range(10f, 550f)]
    public float unloadThresholdRadius = 150f;

    [Tooltip("How often (in seconds) the system checks for scene streaming.")]
    [Range(0.1f, 5f)]
    public float streamingUpdateInterval = 1.0f;

    [Header("Debug")]
    [Tooltip("Enable logging of streaming events to the console.")]
    public bool logStreamingEvents = true;

    // Internal state management
    private Dictionary<string, Scene> loadedScenes = new Dictionary<string, Scene>();
    private HashSet<string> scenesCurrentlyLoading = new HashSet<string>();
    private HashSet<string> scenesCurrentlyUnloading = new HashSet<string>();
    private float lastStreamCheckTime;

    private void Awake()
    {
        // Validate settings
        if (playerTransform == null)
        {
            Debug.LogError("LevelStreamingSystem: Player Transform is not assigned! " +
                           "Please assign the player or camera transform.", this);
            enabled = false; // Disable component if essential setup is missing
            return;
        }

        if (unloadThresholdRadius <= streamingRadius)
        {
            Debug.LogWarning("LevelStreamingSystem: Unload Threshold Radius should be greater than Streaming Radius " +
                             "to prevent rapid loading/unloading near the boundary. Adjusting.", this);
            unloadThresholdRadius = streamingRadius * 1.2f; // Ensure a buffer
        }

        // Initialize any scenes that should be loaded at startup if they are considered "central"
        // For simplicity, this example relies on the first check to load initial scenes.
        // A more complex system might have 'always loaded' chunks or an initial load based on player spawn.
    }

    private void Update()
    {
        // Only perform checks at the specified interval to save performance
        if (Time.time >= lastStreamCheckTime + streamingUpdateInterval)
        {
            PerformStreamingCheck();
            lastStreamCheckTime = Time.time;
        }
    }

    /// <summary>
    /// Iterates through all defined level chunks and determines if they need to be loaded or unloaded.
    /// </summary>
    private void PerformStreamingCheck()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        foreach (StreamableLevelChunkData chunk in streamableLevelChunks)
        {
            // --- Loading Logic ---
            // If the chunk is within streaming range, not currently loaded, and not already loading
            if (chunk.IsWithinLoadingRadius(playerPos, streamingRadius) &&
                !loadedScenes.ContainsKey(chunk.sceneName) &&
                !scenesCurrentlyLoading.Contains(chunk.sceneName))
            {
                StartCoroutine(LoadLevelChunkAsync(chunk));
            }

            // --- Unloading Logic ---
            // If the chunk is outside the unload threshold, is currently loaded, and not already unloading
            else if (chunk.IsOutsideUnloadingRadius(playerPos, unloadThresholdRadius) &&
                     loadedScenes.ContainsKey(chunk.sceneName) &&
                     !scenesCurrentlyUnloading.Contains(chunk.sceneName))
            {
                StartCoroutine(UnloadLevelChunkAsync(chunk));
            }
        }
    }

    /// <summary>
    /// Asynchronously loads a level chunk's scene additively.
    /// </summary>
    /// <param name="chunkData">The StreamableLevelChunkData to load.</param>
    private IEnumerator LoadLevelChunkAsync(StreamableLevelChunkData chunkData)
    {
        if (logStreamingEvents)
        {
            Debug.Log($"LevelStreamingSystem: Starting to load scene: {chunkData.sceneName}", this);
        }

        scenesCurrentlyLoading.Add(chunkData.sceneName);

        // Load the scene additively so it merges with the existing scene hierarchy
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(chunkData.sceneName, LoadSceneMode.Additive);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            // You could add loading progress UI here if needed
            yield return null;
        }

        if (asyncLoad.isDone)
        {
            Scene loadedScene = SceneManager.GetSceneByName(chunkData.sceneName);
            if (loadedScene.isLoaded)
            {
                loadedScenes[chunkData.sceneName] = loadedScene;
                if (logStreamingEvents)
                {
                    Debug.Log($"LevelStreamingSystem: Successfully loaded scene: {chunkData.sceneName}", this);
                }
            }
            else
            {
                Debug.LogError($"LevelStreamingSystem: Failed to retrieve loaded scene object for {chunkData.sceneName}. " +
                               $"SceneManager might not have registered it as loaded.", this);
            }
        }
        else
        {
            Debug.LogError($"LevelStreamingSystem: Loading operation for {chunkData.sceneName} failed unexpectedly.", this);
        }

        scenesCurrentlyLoading.Remove(chunkData.sceneName);
    }

    /// <summary>
    /// Asynchronously unloads a previously loaded level chunk's scene.
    /// </summary>
    /// <param name="chunkData">The StreamableLevelChunkData to unload.</param>
    private IEnumerator UnloadLevelChunkAsync(StreamableLevelChunkData chunkData)
    {
        // Ensure the scene is actually in our loadedScenes dictionary and not already unloading
        if (!loadedScenes.ContainsKey(chunkData.sceneName) || scenesCurrentlyUnloading.Contains(chunkData.sceneName))
        {
            yield break;
        }

        if (logStreamingEvents)
        {
            Debug.Log($"LevelStreamingSystem: Starting to unload scene: {chunkData.sceneName}", this);
        }

        scenesCurrentlyUnloading.Add(chunkData.sceneName);

        // Unity's SceneManager.UnloadSceneAsync accepts either scene name or Scene object
        // Using the Scene object we stored for robustness
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(loadedScenes[chunkData.sceneName]);

        // Wait until the scene is fully unloaded
        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        if (asyncUnload.isDone)
        {
            loadedScenes.Remove(chunkData.sceneName);
            if (logStreamingEvents)
            {
                Debug.Log($"LevelStreamingSystem: Successfully unloaded scene: {chunkData.sceneName}", this);
            }
        }
        else
        {
            Debug.LogError($"LevelStreamingSystem: Unloading operation for {chunkData.sceneName} failed unexpectedly.", this);
        }

        scenesCurrentlyUnloading.Remove(chunkData.sceneName);
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize streaming radii and chunk bounds.
    /// Helpful for debugging and setup.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            // Draw streaming radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, streamingRadius);

            // Draw unload threshold radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, unloadThresholdRadius);
        }

        // Draw individual chunk bounds
        if (streamableLevelChunks != null)
        {
            foreach (StreamableLevelChunkData chunk in streamableLevelChunks)
            {
                if (chunk != null)
                {
                    chunk.DrawGizmos();
                }
            }
        }
    }
}
```

---

### **Example Usage in Unity:**

Here's how to set this up in a new Unity project:

1.  **Create a New Unity Project** (or open an existing one).

2.  **Create a "Scenes" Folder:** In your Project window, create a folder named `Scenes`.

3.  **Create Your Level Chunk Scenes:**
    *   Go to `File > New Scene`. Save it as `Scenes/MainPersistentScene`. This will be your main scene that always stays loaded.
    *   Create 3-4 more new scenes. Name them `Scenes/Chunk_01`, `Scenes/Chunk_02`, `Scenes/Chunk_03`, etc.
    *   **Crucially:** Add some unique content to each chunk scene (e.g., a differently colored cube, text, a unique object) so you can visually confirm when they load/unload.
    *   **Important:** Go to `File > Build Settings...` and drag all your `Chunk_XX` scenes into the "Scenes In Build" list. The `MainPersistentScene` can also be added, but the streaming system doesn't need it there to load it additively. **If scenes are not in Build Settings, `LoadSceneAsync` will fail.**

4.  **Create a Player Object:**
    *   In your `MainPersistentScene`, create an empty GameObject named `Player`.
    *   Add a `CharacterController` component (or any component that allows movement).
    *   Add a simple script to `Player` for movement (e.g., using `Input.GetAxis("Horizontal")`, `Input.GetAxis("Vertical")`). This will allow you to move and trigger streaming. A simple capsule with a camera parented is also good.

5.  **Set up `StreamableLevelChunkData` Assets:**
    *   In your Project window, right-click `Create > Level Streaming > Streamable Level Chunk Data`.
    *   Create one of these ScriptableObjects for each `Chunk_XX` scene you made. Name them `Chunk_01_Data`, `Chunk_02_Data`, etc.
    *   **For each `Chunk_XX_Data` asset:**
        *   Drag its corresponding `Chunk_XX` scene from the `Scenes` folder into the `Scene Name` field.
        *   Adjust the `Chunk Bounds` (Center and Size) to accurately represent the physical area your objects in that scene will occupy. You can visualize this in the editor using `OnDrawGizmos()`. *This is the most critical step for spatial streaming.* For example, if Chunk_01 has content centered at (0,0,0) with a 50x50x50 size, set its bounds accordingly. Chunk_02 might be centered at (100,0,0) with 50x50x50 size, etc.

6.  **Create the `LevelStreamingSystem` GameObject:**
    *   In your `MainPersistentScene`, create an empty GameObject named `_LevelStreamingSystem`.
    *   Attach the `LevelStreamingSystem.cs` script to it.

7.  **Configure `_LevelStreamingSystem` Component:**
    *   Drag your `Player` GameObject into the `Player Transform` field.
    *   Drag all your `Chunk_XX_Data` ScriptableObjects into the `Streamable Level Chunks` list.
    *   Adjust `Streaming Radius` and `Unload Threshold Radius` as needed. (e.g., 100 for streaming, 150 for unloading).
    *   Make sure `Log Streaming Events` is checked for debugging.

8.  **Position Your Player:**
    *   Place your `Player` object in `MainPersistentScene` such that it's initially within the `streamingRadius` of *at least one* `Chunk_XX_Data`'s bounds (e.g., `Chunk_01_Data`). This will ensure the first chunk loads immediately.

9.  **Run the Scene:**
    *   Play `MainPersistentScene`.
    *   Observe the Console for loading/unloading messages.
    *   Move your player around. As you enter the `streamingRadius` of other chunks, they should appear. As you leave the `unloadThresholdRadius` of previously loaded chunks, they should disappear.

**Important Considerations for Real Projects:**

*   **Scene Content:** Ensure streamed scenes only contain objects relevant to that chunk. Avoid global managers or persistent objects in streamed scenes.
*   **Scene Setup:** When a scene is loaded additively, its root GameObjects become children of the currently active scene. You might need to move them to a dedicated parent or use `SceneManager.SetSceneDirty` if you want to save changes to the loaded scene from the main scene.
*   **Initial Load:** For the very first load, you might want a "start chunk" to be loaded by default, or for the system to aggressively load all chunks within range on `Awake`. This example handles it on the first `PerformStreamingCheck`.
*   **Unloading Dependencies:** If objects in your main scene reference objects in a streamed scene, those references will become null when the streamed scene unloads. Design your system to handle this (e.g., using events, ID-based lookups, or ensuring such references only exist within the streamed scenes themselves).
*   **Optimization:** For very large worlds with many chunks, `PerformStreamingCheck` iterating through all chunks every `streamingUpdateInterval` might become slow. You could optimize this using spatial partitioning structures like a Quadtree or Octree to quickly find relevant chunks.
*   **Loading Screens:** For large chunks, `asyncLoad.progress` can be used to display a loading bar.
*   **Networked Games:** Level streaming in multiplayer games adds complexity. Server needs to manage which chunks are loaded for which client, and synchronize relevant game states.

This complete example provides a solid foundation for implementing a Level Streaming System in your Unity projects, emphasizing both educational clarity and practical applicability.