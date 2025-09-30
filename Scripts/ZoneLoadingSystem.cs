// Unity Design Pattern Example: ZoneLoadingSystem
// This script demonstrates the ZoneLoadingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `ZoneLoadingSystem` design pattern in Unity is used to efficiently manage game content (like scenes, prefabs, or specific game objects) by loading and unloading them dynamically based on the player's proximity to defined "zones" in the game world. This pattern helps optimize memory usage, reduce initial load times, and improve performance by ensuring only relevant content is active at any given moment.

Here's a complete C# Unity example demonstrating this pattern. It consists of two main scripts:

1.  **`ZoneLoadingManager.cs`**: The central singleton that orchestrates scene loading and unloading, handles asynchronous operations, and uses a reference counting system for robust management of shared content and overlapping zones.
2.  **`LoadingZone.cs`**: A component placed on game objects that define a specific zone, triggering load/unload requests to the manager when the player enters or exits.

---

### 1. `ZoneLoadingManager.cs`

This script is the core of the system. It's a singleton, meaning there's only one instance of it throughout the game, providing a global access point for zones to request scene changes. It uses reference counting to handle situations where multiple zones might require the same scene, ensuring the scene is only unloaded when *all* zones referencing it have been exited. A configurable `unloadDelay` prevents scenes from rapidly flickering if the player briefly touches a zone boundary.

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// --- ZoneLoadingSystem Design Pattern ---
//
// Goal: Efficiently load and unload game content (e.g., scenes, prefabs)
// based on the player's proximity to defined "zones" in the game world.
// This prevents overwhelming memory with content far from the player
// and reduces initial load times by breaking the world into smaller, manageable chunks.
//
// Key Components:
// 1.  ZoneLoadingManager (Singleton):
//     - Centralized control for loading and unloading scenes.
//     - Uses a reference counting system to handle overlapping zones
//       or shared content, ensuring a scene is only loaded/unloaded
//       when its reference count (number of active zones requesting it)
//       goes from 0 to 1 (load) or 1 to 0 (unload).
//     - Manages asynchronous loading/unloading operations using Coroutines.
//     - Implements a configurable delay before unloading to prevent
//       "flickering" if the player rapidly enters and exits a zone.
//
// 2.  LoadingZone (MonoBehaviour):
//     - Placed on GameObjects representing a physical "zone" in the game world.
//     - Requires a Collider (set to Is Trigger) to detect player entry/exit.
//     - Configurable with a list of scene names to load when the player enters
//       and to eventually unload when the player leaves.
//     - Communicates with the ZoneLoadingManager to initiate load/unload requests.
//
// How it works:
// - A player (with a specific tag and Rigidbody) enters a LoadingZone's trigger.
// - The LoadingZone notifies the ZoneLoadingManager to increment reference counts
//   for its associated scenes. If a scene's reference count becomes 1, the manager
//   starts loading it asynchronously.
// - When the player leaves a LoadingZone's trigger, the LoadingZone notifies
//   the ZoneLoadingManager to decrement reference counts.
// - If a scene's reference count drops to 0, the manager initiates a delayed
//   asynchronous unload for that scene. The delay ensures that if the player
//   re-enters the zone (or another zone requesting the same scene) quickly,
//   the unload operation can be cancelled.
//
// Benefits:
// - Memory Optimization: Only necessary parts of the world are in memory.
// - Performance: Reduces hitching by loading/unloading scenes asynchronously.
// - Modularity: Game world can be built in smaller, independent scenes.
// - Scalability: Easily add new zones and content without re-architecting.
//
/// <summary>
/// Manages the loading and unloading of additive scenes based on zone requests.
/// Implements a singleton pattern for easy global access.
/// Uses reference counting to handle overlapping zones and shared scenes.
/// Provides delayed unloading to prevent rapid scene cycling.
/// </summary>
public class ZoneLoadingManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Public static property to access the single instance of ZoneLoadingManager.
    public static ZoneLoadingManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Delay in seconds before an unused scene is unloaded. " +
             "Prevents flickering if player quickly re-enters a zone.")]
    [SerializeField] private float unloadDelay = 2.0f;

    // --- Internal State ---
    // Tracks how many active zones are requesting a specific scene.
    // A scene is loaded when its count goes from 0 to 1.
    // A scene is queued for unload when its count goes from 1 to 0.
    private Dictionary<string, int> _sceneReferenceCounts = new Dictionary<string, int>();

    // Stores active unload coroutines for scenes. Used to cancel delayed unloads
    // if a scene becomes referenced again before the delay completes.
    private Dictionary<string, Coroutine> _unloadCoroutines = new Dictionary<string, Coroutine>();

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the singleton.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this duplicate.
            Debug.LogWarning("ZoneLoadingManager: Multiple instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            // Set this instance as the singleton and prevent it from being destroyed on scene loads.
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
    }

    /// <summary>
    /// Requests scenes to be loaded. Increments reference counts.
    /// If a scene's reference count becomes 1, it starts loading asynchronously.
    /// Cancels any pending unload operations for the scene.
    /// </summary>
    /// <param name="sceneNames">List of scene names to load.</param>
    public void RequestLoadScenes(List<string> sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            // Get current reference count, default to 0 if not found.
            _sceneReferenceCounts.TryGetValue(sceneName, out int currentCount);
            // Increment the reference count.
            _sceneReferenceCounts[sceneName] = currentCount + 1;

            // If this is the first time this scene is being requested by an active zone,
            // or if its reference count just went from 0 to 1.
            if (currentCount == 0)
            {
                // If there's a pending unload for this scene, cancel it.
                if (_unloadCoroutines.ContainsKey(sceneName))
                {
                    StopCoroutine(_unloadCoroutines[sceneName]);
                    _unloadCoroutines.Remove(sceneName);
                    Debug.Log($"ZoneLoadingManager: Cancelled pending unload for '{sceneName}' as it's now referenced again.");
                }

                // Start loading the scene asynchronously.
                StartCoroutine(LoadSceneAsync(sceneName));
            }
            else
            {
                // Scene is already referenced by other zones or is currently loading.
                // Just ensure no unload is pending. This is a safeguard; the `currentCount == 0`
                // check above should mostly handle this.
                 if (_unloadCoroutines.ContainsKey(sceneName))
                {
                    StopCoroutine(_unloadCoroutines[sceneName]);
                    _unloadCoroutines.Remove(sceneName);
                    Debug.Log($"ZoneLoadingManager: Cancelled pending unload for '{sceneName}' (ref count: {_sceneReferenceCounts[sceneName]}) as it's still needed.");
                }
            }
        }
    }

    /// <summary>
    /// Requests scenes to be unloaded. Decrements reference counts.
    /// If a scene's reference count drops to 0, it starts a delayed unload asynchronously.
    /// </summary>
    /// <param name="sceneNames">List of scene names to unload.</param>
    public void RequestUnloadScenes(List<string> sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            // Get current reference count.
            _sceneReferenceCounts.TryGetValue(sceneName, out int currentCount);

            if (currentCount > 0)
            {
                // Decrement the reference count.
                _sceneReferenceCounts[sceneName] = currentCount - 1;

                // If the scene is no longer referenced by any active zone (count is now 0).
                if (_sceneReferenceCounts[sceneName] == 0)
                {
                    // If an unload is already pending, stop it and start a new one to reset the delay.
                    // This is important if player rapidly enters/exits different zones that reference the same scene.
                    if (_unloadCoroutines.ContainsKey(sceneName))
                    {
                        StopCoroutine(_unloadCoroutines[sceneName]);
                        _unloadCoroutines.Remove(sceneName);
                        Debug.Log($"ZoneLoadingManager: Resetting unload delay for '{sceneName}'.");
                    }
                    
                    // Start the delayed unload. Store the coroutine reference to allow cancellation.
                    Coroutine unloadCoroutine = StartCoroutine(DelayedUnloadSceneAsync(sceneName));
                    _unloadCoroutines[sceneName] = unloadCoroutine;
                }
            }
            else
            {
                // This scenario indicates an imbalance (e.g., Unload called without a preceding Load).
                // Or the scene was never loaded/managed by this system.
                Debug.LogWarning($"ZoneLoadingManager: Attempted to unload '{sceneName}' which had a reference count of 0. " +
                                 "This might indicate an issue with load/unload requests or scene was never managed by this system.");
            }
        }
    }

    /// <summary>
    /// Asynchronously loads a single additive scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Check if the scene is already loaded to prevent redundant operations.
        // This can happen if the scene was loaded by other means or if logic is slightly off.
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.Log($"ZoneLoadingManager: Scene '{sceneName}' is already loaded. Skipping load operation.");
            yield break;
        }

        Debug.Log($"ZoneLoadingManager: Loading scene '{sceneName}'...");
        // Start the asynchronous scene loading operation in Additive mode.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Wait until the scene is fully loaded.
        while (!asyncLoad.isDone)
        {
            // You can update a loading bar here using asyncLoad.progress if needed
            yield return null;
        }

        Debug.Log($"ZoneLoadingManager: Scene '{sceneName}' loaded successfully.");
    }

    /// <summary>
    /// Asynchronously unloads a single additive scene after a delay.
    /// Includes checks to ensure the scene is still unreferenced and loaded before unloading.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unload.</param>
    private IEnumerator DelayedUnloadSceneAsync(string sceneName)
    {
        yield return new WaitForSeconds(unloadDelay);

        // --- Re-check conditions after the delay ---
        // It's crucial to check again because the player might have re-entered a zone
        // (or another zone requiring this scene) during the unload delay.
        
        _sceneReferenceCounts.TryGetValue(sceneName, out int currentCount);

        // Condition 1: Is the scene still unreferenced (count is 0)?
        // Condition 2: Is the scene actually loaded (it might have been unloaded by another system)?
        if (currentCount == 0 && SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.Log($"ZoneLoadingManager: Unloading scene '{sceneName}' after delay...");
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            // Wait until the scene is fully unloaded.
            while (asyncUnload != null && !asyncUnload.isDone)
            {
                yield return null;
            }
            Debug.Log($"ZoneLoadingManager: Scene '{sceneName}' unloaded successfully.");
        }
        else if (currentCount > 0)
        {
            Debug.Log($"ZoneLoadingManager: Unload of '{sceneName}' cancelled after delay because it became referenced again. Current count: {currentCount}.");
        }
        else
        {
             Debug.Log($"ZoneLoadingManager: Unload of '{sceneName}' cancelled/skipped as it's no longer loaded or reference count is not 0 (e.g. was already unloaded by another system).");
        }

        // Clean up the unload coroutine entry regardless of whether it was unloaded or cancelled.
        _unloadCoroutines.Remove(sceneName);
    }

    /// <summary>
    /// Public method to get the current reference count for a scene.
    /// Useful for debugging or external systems that need to know scene status.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The current reference count for the scene, or 0 if not managed.</returns>
    public int GetSceneReferenceCount(string sceneName)
    {
        _sceneReferenceCounts.TryGetValue(sceneName, out int count);
        return count;
    }

    /// <summary>
    /// Optional: Forcefully unloads all scenes currently managed by the system.
    /// This can be useful for cleanup on game exit or when starting a new game.
    /// </summary>
    public void ForceUnloadAllManagedScenes()
    {
        foreach (var entry in _sceneReferenceCounts)
        {
            string sceneName = entry.Key;
            // Stop any pending unload coroutines for this scene
            if (_unloadCoroutines.ContainsKey(sceneName))
            {
                StopCoroutine(_unloadCoroutines[sceneName]);
                _unloadCoroutines.Remove(sceneName);
            }

            // Unload if currently loaded
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                Debug.Log($"ZoneLoadingManager: Forcibly unloading '{sceneName}'.");
                // Fire and forget, as we are typically exiting or transitioning.
                SceneManager.UnloadSceneAsync(sceneName); 
            }
        }
        _sceneReferenceCounts.Clear(); // Reset all counts
        _unloadCoroutines.Clear(); // Clear all pending unloads
        Debug.Log("ZoneLoadingManager: All managed scenes have been forcibly unloaded and reference counts reset.");
    }
}
```

---

### 2. `LoadingZone.cs`

This script is attached to GameObjects that define the boundaries of a loading zone. It requires a `Collider` set as a trigger. When an object with the specified `playerTag` enters or exits its trigger, it calls the `ZoneLoadingManager` to request loading or unloading of its associated scenes.

```csharp
using UnityEngine;
using System.Collections.Generic;
// Required for UnityEditor.Handles.Label which is used in OnDrawGizmos for better editor visualization.
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Represents a loading zone in the game world. When a GameObject with the specified
/// player tag enters this zone's trigger, it requests the ZoneLoadingManager to load
/// a list of associated scenes. When the player leaves, it requests unloading.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures this GameObject has a Collider component.
public class LoadingZone : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("The tag of the player GameObject that will trigger scene loading/unloading.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("List of scene names to load when the player enters this zone.")]
    [SerializeField] private List<string> scenesToManage = new List<string>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the collider is a trigger and provides warnings if setup is incorrect.
    /// </summary>
    private void Awake()
    {
        // Ensure the collider is set to 'Is Trigger' for OnTriggerEnter/Exit to work.
        Collider zoneCollider = GetComponent<Collider>();
        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"LoadingZone on '{gameObject.name}' has a Collider that is not a trigger. Setting it to trigger. " +
                             "Please ensure the collider is correctly configured in the Inspector.", this);
            zoneCollider.isTrigger = true;
        }

        // For trigger events to fire correctly, at least one of the colliding objects
        // must have a Rigidbody. Typically, the player character will have one.
        // This zone itself does not strictly require a Rigidbody if it's stationary.
    }

    /// <summary>
    /// Called when another collider enters this trigger collider.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the designated player tag.
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"LoadingZone: Player entered '{gameObject.name}'. Requesting scene loads for: {string.Join(", ", scenesToManage)}.");
            // Request the ZoneLoadingManager to load the associated scenes.
            ZoneLoadingManager.Instance.RequestLoadScenes(scenesToManage);
        }
    }

    /// <summary>
    /// Called when another collider exits this trigger collider.
    /// </summary>
    /// <param name="other">The Collider that exited the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object has the designated player tag.
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"LoadingZone: Player exited '{gameObject.name}'. Requesting scene unloads for: {string.Join(", ", scenesToManage)}.");
            // Request the ZoneLoadingManager to unload the associated scenes.
            ZoneLoadingManager.Instance.RequestUnloadScenes(scenesToManage);
        }
    }

    /// <summary>
    /// Draws a gizmo in the editor to visualize the zone boundaries and associated scene names.
    /// </summary>
    private void OnDrawGizmos()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null) return;

        // Set gizmo color for the zone's visual representation.
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, semi-transparent
        
        // Store current matrix and apply zone's transform for drawing.
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        // Draw the appropriate collider shape.
        if (zoneCollider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0, 1, 0, 0.8f); // Opaque green for wireframe
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (zoneCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0, 1, 0, 0.8f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (zoneCollider is CapsuleCollider capsule)
        {
            // Drawing a perfect capsule gizmo is more complex. Drawing a wire sphere as an approximation.
            Gizmos.DrawWireSphere(capsule.center, capsule.radius); 
        }

        // Restore original gizmo matrix.
        Gizmos.matrix = originalMatrix;

        // Display associated scene names above the zone in the editor.
        if (scenesToManage.Count > 0)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 20;

            string sceneList = string.Join("\n", scenesToManage);
            #if UNITY_EDITOR // UnityEditor namespace content should only be compiled in the editor.
            // Handles.Label is a UnityEditor function for drawing text in the Scene view.
            UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneCollider.bounds.extents.y + 0.5f), $"Zone:\n{sceneList}", style);
            #endif
        }
    }
}
```

---

### Example Usage in Unity Project

Follow these steps to set up and test the ZoneLoadingSystem in your Unity project:

1.  **CREATE THE ZONE LOADING SYSTEM MANAGER:**
    *   Create an empty GameObject in your primary scene (e.g., named "ZoneLoadingSystem").
    *   Attach the `ZoneLoadingManager.cs` script to it.
    *   (Optional) Adjust the `Unload Delay` value in the Inspector (default is 2 seconds).

2.  **PREPARE YOUR SCENES:**
    *   Create several new Unity scenes (e.g., "Zone1_Content", "Zone2_Content", "Zone_SharedContent").
    *   Add some unique content to each of these scenes (e.g., a simple cube or text) so you can visually confirm when they are loaded/unloaded. Position them at (0,0,0) in their respective scenes, as additive scenes typically merge their contents into the current scene's coordinate system.
    *   **IMPORTANT:** Go to `File > Build Settings...` and drag all your content scenes (e.g., "Zone1_Content", "Zone2_Content", "Zone_SharedContent") into the "Scenes In Build" list. They do NOT need to be loaded at startup; they just need to be in the build list to be loadable by name. Your main scene (e.g., "GameScene") should also be in this list, usually at index 0.

3.  **CREATE YOUR PLAYER:**
    *   Create a simple player GameObject (e.g., a "Cube" or "Capsule").
    *   Set its `Tag` to "Player" in the Inspector (you might need to add this tag first: `Tags & Layers` dropdown -> `Add Tag...`).
    *   Add a `CharacterController` or a `Collider` (e.g., `CapsuleCollider`) and a `Rigidbody` component to your player.
        *   For the `Rigidbody`, ensure "Is Kinematic" is checked if you're using a `CharacterController` or don't want physics to directly move the player. Triggers require *at least one* of the colliding objects to have a `Rigidbody`.
    *   (Optional) Add a simple movement script to your player so you can move it around (e.g., using `Input.GetAxis("Horizontal")` and `Input.GetAxis("Vertical")`).

4.  **SET UP LOADING ZONES:**
    *   In your main scene, create an empty GameObject (e.g., "Zone1").
    *   Add a `Collider` component to it (e.g., `BoxCollider` or `SphereCollider`).
    *   Set the Collider's `Is Trigger` property to `true` in the Inspector. Scale it to cover the desired area for Zone1.
    *   Attach the `LoadingZone.cs` script to "Zone1".
    *   In the `LoadingZone` script's Inspector:
        *   Ensure `Player Tag` is set to "Player".
        *   In the `Scenes To Manage` list, add the name(s) of the scene(s) you want to load when entering this zone. For "Zone1", you might add "Zone1_Content".
    *   Repeat steps a-e for "Zone2" (e.g., adding "Zone2_Content" to its `Scenes To Manage` list). Position Zone2 so it's distinct from Zone1.
    *   **Example of shared content and overlapping zones:**
        *   Create a third zone, "ZoneShared", and add "Zone_SharedContent" to its `Scenes To Manage` list. Position this zone to overlap partially with "Zone1" and "Zone2".
        *   Alternatively, you could add "Zone_SharedContent" to *both* "Zone1" and "Zone2"'s `Scenes To Manage` lists. Observe the console output and the reference counting logic:
            *   Player enters Zone1: "Zone1_Content" and "Zone_SharedContent" load. `Zone_SharedContent` ref count is 1.
            *   Player enters Zone2 (overlapping Zone1): "Zone2_Content" loads. `Zone_SharedContent` ref count increments to 2 (it's already loaded, so no re-load).
            *   Player leaves Zone1: "Zone1_Content" gets queued for unload. `Zone_SharedContent` ref count decrements to 1. Since it's still > 0, it will *not* be unloaded.
            *   Player leaves Zone2: "Zone2_Content" gets queued for unload. `Zone_SharedContent` ref count decrements to 0. It now gets queued for unload after the `unloadDelay`.

5.  **RUN THE GAME:**
    *   Play your main scene.
    *   Move your player character.
    *   Observe the Console window: You will see messages indicating when scenes are requested, loaded, unloaded, or cancelled.
    *   Visually confirm the content (cubes, text) from your additive scenes appearing and disappearing as you enter and exit the zones.

This setup provides a clear, practical demonstration of the ZoneLoadingSystem pattern, handling asynchronous operations, overlapping zones, and shared content efficiently, making it suitable for managing large game worlds.