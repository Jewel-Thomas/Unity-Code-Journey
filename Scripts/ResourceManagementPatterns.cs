// Unity Design Pattern Example: ResourceManagementPatterns
// This script demonstrates the ResourceManagementPatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

The Resource Management Patterns in Unity encompass various strategies for efficiently loading, caching, and releasing assets (textures, audio, prefabs, scriptable objects, etc.). This is crucial for optimizing memory usage, preventing freezes (due to synchronous loading), and ensuring a smooth user experience, especially in games with many assets or dynamic content.

This example demonstrates a common approach using a centralized `ResourceManager` that employs the following patterns/strategies:

1.  **Service Locator/Singleton**: A single, globally accessible instance (`ResourceManager.Instance`) provides a consistent API for all resource operations.
2.  **Asynchronous Loading**: Uses `Resources.LoadAsync` to prevent frame rate drops when loading assets, notifying clients via callbacks upon completion.
3.  **Caching**: Stores loaded assets in memory (`_resourceCache`) to avoid repeated disk I/O for frequently accessed resources.
4.  **Reference Counting**: Tracks how many "clients" are currently using a specific resource. A resource is only eligible for unloading when its reference count drops to zero. This helps prevent assets from being unloaded while still in use.
5.  **Abstraction**: Provides a generic `LoadResourceAsync<T>` method, abstracting away the underlying loading mechanism (`Resources.LoadAsync`).

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a resource entry within the ResourceManager's cache.
/// Tracks the actual asset, its type, and how many times it has been acquired (referenced).
/// </summary>
internal class ResourceEntry
{
    public UnityEngine.Object Asset { get; private set; }
    public int ReferenceCount { get; private set; }
    public Type AssetType { get; private set; }
    public string Path { get; private set; } // Added for easier internal management

    public ResourceEntry(string path, UnityEngine.Object asset, Type type)
    {
        Path = path;
        Asset = asset;
        AssetType = type;
        ReferenceCount = 0; // Will be incremented upon first acquisition
    }

    /// <summary>
    /// Increments the reference count, indicating another client is using this resource.
    /// </summary>
    public void Acquire()
    {
        ReferenceCount++;
        // Debug.Log($"Acquired resource: {Path}. Ref count: {ReferenceCount}");
    }

    /// <summary>
    /// Decrements the reference count, indicating a client has finished using this resource.
    /// </summary>
    public void Release()
    {
        if (ReferenceCount > 0)
        {
            ReferenceCount--;
            // Debug.Log($"Released resource: {Path}. Ref count: {ReferenceCount}");
        }
        else
        {
            Debug.LogWarning($"Attempted to release resource '{Path}' but its reference count was already zero.");
        }
    }
}


/// <summary>
/// A centralized Resource Manager implementing various Resource Management Patterns.
/// It acts as a Service Locator/Singleton, handles asynchronous loading, caching,
/// and reference counting for assets loaded from the Unity Resources folder.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures there's only one instance of the ResourceManager throughout the application.
    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<ResourceManager>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the ResourceManager to it
                    GameObject singletonObject = new GameObject("ResourceManager");
                    _instance = singletonObject.AddComponent<ResourceManager>();
                    Debug.Log("ResourceManager created automatically.");
                }

                // Make sure the ResourceManager persists across scene loads
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // --- Caching ---
    // Stores loaded assets. Key: resource path, Value: ResourceEntry containing the asset and its metadata.
    private Dictionary<string, ResourceEntry> _resourceCache = new Dictionary<string, ResourceEntry>();

    // --- Asynchronous Loading Tracking ---
    // Stores ongoing asynchronous loading operations to prevent redundant loads and manage callbacks.
    // Key: resource path, Value: A list of callbacks waiting for this resource to load.
    private Dictionary<string, List<Action<UnityEngine.Object>>> _loadingCallbacks = new Dictionary<string, List<Action<UnityEngine.Object>>>();
    
    // Tracks current loading coroutines to prevent starting multiple loads for the same path
    private Dictionary<string, Coroutine> _activeLoadingCoroutines = new Dictionary<string, Coroutine>();


    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // If another instance already exists, destroy this duplicate
            Debug.LogWarning("Duplicate ResourceManager instance found, destroying this one.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null; // Clear the singleton reference if this instance is destroyed
        }
        // It's good practice to unload all resources when the manager is destroyed,
        // typically at application exit or a specific cleanup phase.
        UnloadAllResources(true);
    }

    // --- Public API for Resource Management ---

    /// <summary>
    /// Asynchronously loads a resource of a specific type from the "Resources" folder.
    /// If the resource is already cached, it's returned immediately.
    /// If it's currently loading, the callback is added to the waiting list.
    /// Otherwise, a new asynchronous load operation is started.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load (e.g., Texture2D, GameObject, AudioClip).</typeparam>
    /// <param name="path">The path to the resource within a "Resources" folder (e.g., "Textures/MyTexture").</param>
    /// <param name="onComplete">Callback action invoked when the resource has finished loading or is retrieved from cache.</param>
    public void LoadResourceAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("ResourceManager: Attempted to load resource with an empty or null path.");
            onComplete?.Invoke(null);
            return;
        }

        // 1. Check if resource is already in cache
        if (_resourceCache.TryGetValue(path, out ResourceEntry entry))
        {
            if (entry.Asset != null)
            {
                // Resource found in cache and is not null. Increment ref count and return.
                entry.Acquire();
                // Ensure the type matches
                if (entry.Asset is T typedAsset)
                {
                    onComplete?.Invoke(typedAsset);
                    return;
                }
                else
                {
                    Debug.LogWarning($"ResourceManager: Cached asset '{path}' is type '{entry.Asset.GetType().Name}', but requested type was '{typeof(T).Name}'. Attempting to load again.");
                    _resourceCache.Remove(path); // Remove the mismatched entry
                }
            }
            else
            {
                // Asset was null in cache (e.g., failed to load previously), remove it to try again
                Debug.LogWarning($"ResourceManager: Cached asset '{path}' was null. Re-attempting load.");
                _resourceCache.Remove(path);
            }
        }

        // 2. Check if resource is currently loading
        if (_loadingCallbacks.TryGetValue(path, out var callbacks))
        {
            // Resource is already loading, add this new callback to the list
            callbacks.Add(obj => onComplete?.Invoke(obj as T));
            // Debug.Log($"ResourceManager: Added callback for '{path}', already loading.");
            return;
        }

        // 3. Resource is not in cache and not currently loading, start a new load operation
        callbacks = new List<Action<UnityEngine.Object>>();
        callbacks.Add(obj => onComplete?.Invoke(obj as T));
        _loadingCallbacks.Add(path, callbacks);

        // Start the actual asynchronous loading coroutine
        Coroutine loadingCoroutine = StartCoroutine(ProcessLoadRequest(path, typeof(T), callbacks));
        _activeLoadingCoroutines.Add(path, loadingCoroutine);

        // Debug.Log($"ResourceManager: Started async load for '{path}'.");
    }

    /// <summary>
    /// Decrements the reference count for a given resource.
    /// Once the reference count reaches zero, the resource becomes eligible for unloading.
    /// Note: This does NOT immediately unload the resource from memory, but marks it as unused.
    /// </summary>
    /// <param name="path">The path to the resource that is no longer needed by a client.</param>
    public void ReleaseResource(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("ResourceManager: Attempted to release resource with an empty or null path.");
            return;
        }

        if (_resourceCache.TryGetValue(path, out ResourceEntry entry))
        {
            entry.Release();
            // A common pattern is to trigger a cleanup method periodically or on scene transitions
            // to actually unload assets with a ref count of 0.
            // For this example, we'll demonstrate explicit cleanup later.
        }
        else
        {
            Debug.LogWarning($"ResourceManager: Attempted to release resource '{path}' which was not found in cache.");
        }
    }

    /// <summary>
    /// Synchronously retrieves a resource if it's already in the cache and has an active reference count.
    /// This method is primarily for quick checks; for loading, use LoadResourceAsync.
    /// </summary>
    /// <typeparam name="T">The expected type of the resource.</typeparam>
    /// <param name="path">The path to the resource.</param>
    /// <returns>The loaded resource if found and type matches, otherwise null.</returns>
    public T GetLoadedResource<T>(string path) where T : UnityEngine.Object
    {
        if (_resourceCache.TryGetValue(path, out ResourceEntry entry))
        {
            if (entry.Asset != null && entry.ReferenceCount > 0 && entry.Asset is T typedAsset)
            {
                return typedAsset;
            }
        }
        return null;
    }

    /// <summary>
    /// Unloads all resources from the cache whose reference count is zero.
    /// This is where the actual `Resources.UnloadAsset` or `Destroy` call happens.
    /// </summary>
    /// <param name="forceUnloadAll">If true, unloads all resources regardless of their reference count.
    /// Use with caution, typically on application shutdown or specific full cleanup points.</param>
    public void UnloadAllResources(bool forceUnloadAll = false)
    {
        Debug.Log($"ResourceManager: Starting cleanup (Force all: {forceUnloadAll}). Cache size before: {_resourceCache.Count}");

        // Cancel any active loading operations
        foreach(var kvp in _activeLoadingCoroutines)
        {
            StopCoroutine(kvp.Value);
        }
        _activeLoadingCoroutines.Clear();
        _loadingCallbacks.Clear(); // Clear all pending callbacks

        List<string> pathsToUnload = new List<string>();
        foreach (var kvp in _resourceCache)
        {
            string path = kvp.Key;
            ResourceEntry entry = kvp.Value;

            if (forceUnloadAll || entry.ReferenceCount <= 0)
            {
                pathsToUnload.Add(path);
                // The actual unloading mechanism depends on the asset type and how it was loaded.
                // For Resources.Load, we use Resources.UnloadAsset.
                if (entry.Asset != null)
                {
                    // For instantiated GameObjects (prefabs), UnloadAsset doesn't destroy the instances.
                    // It only unloads the *asset data*. Instances must be destroyed manually.
                    if (entry.AssetType == typeof(GameObject) || entry.Asset is GameObject)
                    {
                        // Special handling for prefabs: Resources.UnloadAsset doesn't destroy instances.
                        // You'd typically manage instances separately (e.g., using an Object Pool).
                        // Here, we just unload the prefab asset data.
                        Debug.LogWarning($"ResourceManager: Unloading GameObject asset '{path}'. Note: This does not destroy active instances derived from it.");
                    }
                    Resources.UnloadAsset(entry.Asset);
                    Debug.Log($"ResourceManager: Unloaded asset '{path}'.");
                }
            }
        }

        // Remove unloaded resources from the cache
        foreach (string path in pathsToUnload)
        {
            _resourceCache.Remove(path);
        }
        
        // This attempts to unload *all* unused assets loaded via Resources API.
        // Can be memory intensive, use sparingly.
        if (forceUnloadAll)
        {
            Resources.UnloadUnusedAssets().completed += (asyncOp) =>
            {
                Debug.Log("ResourceManager: Resources.UnloadUnusedAssets completed.");
            };
        }

        Debug.Log($"ResourceManager: Cleanup finished. Cache size after: {_resourceCache.Count}");
    }


    // --- Internal Asynchronous Loading Process ---

    /// <summary>
    /// Coroutine that handles the actual asynchronous loading of a resource using Unity's Resources.LoadAsync.
    /// Once loaded, it updates the cache and invokes all waiting callbacks.
    /// </summary>
    private IEnumerator ProcessLoadRequest(string path, Type type, List<Action<UnityEngine.Object>> callbacks)
    {
        ResourceRequest request = Resources.LoadAsync(path, type);

        yield return request; // Wait for the asynchronous load to complete

        UnityEngine.Object loadedAsset = request.asset;

        // Ensure the coroutine reference is removed once it completes
        _activeLoadingCoroutines.Remove(path);

        if (loadedAsset != null)
        {
            // Resource loaded successfully, add to cache
            ResourceEntry newEntry = new ResourceEntry(path, loadedAsset, type);
            _resourceCache[path] = newEntry; // Add or update cache
            newEntry.Acquire(); // First acquisition for the newly loaded asset
            
            // Invoke all waiting callbacks
            foreach (var callback in callbacks)
            {
                callback?.Invoke(loadedAsset);
            }
            // Debug.Log($"ResourceManager: Loaded '{path}' successfully.");
        }
        else
        {
            // Resource failed to load
            Debug.LogError($"ResourceManager: Failed to load resource at path: {path}");
            
            // Invoke all waiting callbacks with null to indicate failure
            foreach (var callback in callbacks)
            {
                callback?.Invoke(null);
            }
            // Remove from cache or prevent future attempts for this path until explicitly requested again
            // or if a null entry was added, remove it.
            if (_resourceCache.ContainsKey(path)) _resourceCache.Remove(path);
        }

        // Clear callbacks for this path after processing
        _loadingCallbacks.Remove(path);
    }


    // --- Example Usage (Demonstrates the pattern) ---

    // Define some constant paths for our example resources
    private const string TEXTURE_PATH = "Textures/ExampleTexture"; // Create a 'Resources/Textures' folder and put a texture there
    private const string PREFAB_PATH = "Prefabs/ExampleCube";      // Create a 'Resources/Prefabs' folder and put a simple cube prefab there
    private const string AUDIO_PATH = "Audio/ExampleSound";        // Create a 'Resources/Audio' folder and put an audio clip there


    void Start()
    {
        Debug.Log("--- ResourceManager Demo Started ---");

        // --- Demo 1: Loading a Texture ---
        Debug.Log("\n--- DEMO 1: Loading Texture ---");
        LoadResourceAsync<Texture2D>(TEXTURE_PATH, OnTextureLoaded);

        // Try loading the same texture again immediately. It should be cached or join the pending callbacks.
        LoadResourceAsync<Texture2D>(TEXTURE_PATH, (tex) =>
        {
            if (tex != null)
            {
                Debug.Log($"Second request for '{TEXTURE_PATH}' completed. Texture size: {tex.width}x{tex.height}.");
                // Release the resource after usage
                ReleaseResource(TEXTURE_PATH);
            }
            else
            {
                Debug.LogError($"Second request for '{TEXTURE_PATH}' failed.");
            }
        });

        // --- Demo 2: Loading a Prefab and Instantiating ---
        Debug.Log("\n--- DEMO 2: Loading Prefab ---");
        LoadResourceAsync<GameObject>(PREFAB_PATH, OnPrefabLoaded);

        // --- Demo 3: Loading an Audio Clip ---
        Debug.Log("\n--- DEMO 3: Loading Audio Clip ---");
        LoadResourceAsync<AudioClip>(AUDIO_PATH, OnAudioClipLoaded);

        // --- Demo 4: Attempt to load a non-existent resource ---
        Debug.Log("\n--- DEMO 4: Loading Non-Existent Resource ---");
        LoadResourceAsync<Texture2D>("NonExistent/Path", (tex) =>
        {
            if (tex == null)
            {
                Debug.LogError("Successfully handled loading a non-existent texture. Callback received null.");
            }
            else
            {
                Debug.LogError("Error: Non-existent texture surprisingly loaded!");
            }
        });

        // Schedule a cleanup after some time
        Invoke("PerformCleanup", 5f);
    }

    private void OnTextureLoaded(Texture2D texture)
    {
        if (texture != null)
        {
            Debug.Log($"Texture '{texture.name}' ({TEXTURE_PATH}) loaded successfully! Size: {texture.width}x{texture.height}");
            // Example: Apply texture to a material
            // Renderer rend = GetComponent<Renderer>();
            // if (rend != null) rend.material.mainTexture = texture;
            // else Debug.Log("No renderer found to apply texture.");

            // Release the resource when done with its initial use
            ReleaseResource(TEXTURE_PATH);
        }
        else
        {
            Debug.LogError($"Failed to load texture at '{TEXTURE_PATH}'.");
        }
    }

    private void OnPrefabLoaded(GameObject prefab)
    {
        if (prefab != null)
        {
            Debug.Log($"Prefab '{prefab.name}' ({PREFAB_PATH}) loaded successfully!");
            // Example: Instantiate the prefab
            GameObject instantiatedObject = Instantiate(prefab, Vector3.up * 2, Quaternion.identity);
            instantiatedObject.name = "Instantiated_" + prefab.name;
            Debug.Log($"Instantiated '{instantiatedObject.name}'.");

            // Important: Releasing the *prefab asset* doesn't destroy instantiated game objects.
            // You must manage the lifecycle of instantiated objects separately (e.g., destroy them).
            Destroy(instantiatedObject, 4f); // Destroy instance after 4 seconds
            ReleaseResource(PREFAB_PATH); // Release the *asset data* reference
        }
        else
        {
            Debug.LogError($"Failed to load prefab at '{PREFAB_PATH}'.");
        }
    }

    private void OnAudioClipLoaded(AudioClip audioClip)
    {
        if (audioClip != null)
        {
            Debug.Log($"Audio Clip '{audioClip.name}' ({AUDIO_PATH}) loaded successfully! Length: {audioClip.length}s");
            // Example: Play the audio clip
            // AudioSource source = gameObject.GetComponent<AudioSource>();
            // if (source == null) source = gameObject.AddComponent<AudioSource>();
            // source.clip = audioClip;
            // source.Play();

            // Release the resource after usage
            ReleaseResource(AUDIO_PATH);
        }
        else
        {
            Debug.LogError($"Failed to load audio clip at '{AUDIO_PATH}'.");
        }
    }

    private void PerformCleanup()
    {
        Debug.Log("\n--- DEMO: Performing Cleanup ---");
        UnloadAllResources(); // Unload resources with zero reference count
    }
}
```

### How to Use This Example in Unity:

1.  **Create a C# Script**:
    *   In your Unity project, go to `Assets/Scripts` (or any preferred folder).
    *   Right-click -> Create -> C# Script, name it `ResourceManager`.
    *   Copy and paste the entire code above into this new script.

2.  **Create Resources Folders**:
    *   In the `Assets` folder (or any subfolder), create a folder named `Resources`. (e.g., `Assets/Resources`).
    *   Inside `Resources`, create the following subfolders:
        *   `Assets/Resources/Textures`
        *   `Assets/Resources/Prefabs`
        *   `Assets/Resources/Audio`

3.  **Populate Resources Folders**:
    *   **Texture**: Find any image file (e.g., `PNG`, `JPG`), drag it into `Assets/Resources/Textures`, and rename it to `ExampleTexture`.
    *   **Prefab**:
        *   Create a simple 3D object: Right-click in Hierarchy -> 3D Object -> Cube.
        *   Drag this `Cube` from the Hierarchy into `Assets/Resources/Prefabs`. This will create a prefab.
        *   Rename the prefab in `Assets/Resources/Prefabs` to `ExampleCube`.
        *   Delete the `Cube` from your Hierarchy (the prefab asset remains).
    *   **Audio**: Find any audio file (e.g., `WAV`, `MP3`), drag it into `Assets/Resources/Audio`, and rename it to `ExampleSound`.

4.  **Add to Scene**:
    *   Create an empty GameObject in your scene: Right-click in Hierarchy -> Create Empty.
    *   Rename this GameObject to `_GameManager` (or anything you prefer).
    *   Drag the `ResourceManager` script from your Project window onto this `_GameManager` GameObject in the Hierarchy.

5.  **Run the Scene**:
    *   Play your Unity scene.
    *   Observe the Console window for detailed logs illustrating:
        *   When resources are requested, loaded, and cached.
        *   Reference count changes (Acquire/Release).
        *   Prefab instantiation.
        *   Handling of non-existent resources.
        *   The cleanup process.

### Explanation of Resource Management Patterns in this Example:

*   **Service Locator/Singleton**: The `ResourceManager.Instance` static property provides a global, easy-to-access entry point for all resource-related operations. Any script in your game can call `ResourceManager.Instance.LoadResourceAsync(...)` without needing a direct reference. `DontDestroyOnLoad` ensures it persists across scene changes.

*   **Asynchronous Loading (`Resources.LoadAsync`)**: Instead of blocking the main thread, `Resources.LoadAsync` starts loading in the background. The `ProcessLoadRequest` coroutine waits for this operation to complete (`yield return request;`) and then invokes the provided `onComplete` callback. This prevents your game from freezing while large assets are being loaded, crucial for smooth gameplay.

*   **Caching (`_resourceCache`)**:
    *   When `LoadResourceAsync` is called, it first checks `_resourceCache`.
    *   If the asset is already loaded and in the cache, it's returned immediately (and its reference count incremented), avoiding redundant disk I/O. This makes subsequent accesses to the same resource much faster.
    *   If the asset is *currently loading*, the new request's callback is simply added to a list (`_loadingCallbacks`) associated with that path. Once the asset finishes loading, all waiting callbacks are invoked.

*   **Reference Counting (`ResourceEntry.ReferenceCount`)**:
    *   Each time `LoadResourceAsync` successfully provides an asset (either by loading it or retrieving it from cache), the `Acquire()` method on its `ResourceEntry` is called, incrementing `ReferenceCount`.
    *   When a client is finished with a resource, it calls `ReleaseResource(path)`, which decrements `ReferenceCount`.
    *   The `UnloadAllResources()` method only performs `Resources.UnloadAsset` on assets whose `ReferenceCount` is zero (or less, though it should ideally not go below zero). This ensures assets are not prematurely removed from memory if other parts of the game still need them.
    *   **Important Note on `GameObject` Prefabs**: When you load a `GameObject` prefab, `Resources.UnloadAsset` only unloads the *prefab's asset data*. It *does not* destroy any GameObjects that have been instantiated from that prefab. You must manage the lifecycle of instantiated GameObjects separately (e.g., using `Destroy(gameObject)` or object pooling).

*   **Abstraction**: The `LoadResourceAsync<T>(string path, Action<T> onComplete)` method provides a clean, generic interface for clients. The client doesn't need to know the specifics of `Resources.LoadAsync`, how caching works, or how reference counts are managed. It just requests a resource by path and type, and receives it via a callback.

This example provides a robust foundation for managing resources in your Unity projects, helping you build more performant and memory-efficient games. For larger projects, consider Unity's Addressable Asset System, which offers more advanced features like asset bundles, content delivery networks, and further abstraction, building upon many of these same core resource management principles.