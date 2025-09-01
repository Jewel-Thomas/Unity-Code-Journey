// Unity Design Pattern Example: AssetBundleManagement
// This script demonstrates the AssetBundleManagement pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity script demonstrates the **AssetBundleManagement design pattern**. It provides a robust, centralized, and asynchronous system for loading, caching, and unloading AssetBundles and their contained assets.

The pattern helps in:
*   **Decoupling:** Your game logic doesn't directly interact with low-level AssetBundle APIs.
*   **Performance:** Caching prevents redundant loading, and asynchronous operations keep the game responsive.
*   **Memory Management:** Centralized control allows for systematic unloading of bundles when no longer needed.
*   **Scalability:** Easily handle a large number of assets and their dependencies.
*   **Maintainability:** Changes to how AssetBundles are loaded (e.g., from local storage vs. CDN) are confined to the manager.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO; // For Path.Combine
using UnityEngine.Networking; // For UnityWebRequestAssetBundle to load bundles

// =====================================================================================
// AssetBundleManagement Design Pattern Example
// =====================================================================================
// This script demonstrates a practical implementation of the AssetBundleManagement
// design pattern in Unity. It provides a robust, centralized system for loading,
// caching, and unloading AssetBundles and their contained assets.
//
// Key features:
// - Singleton pattern for easy access from anywhere.
// - Asynchronous loading for non-blocking operations using Unity's Coroutines.
// - Caching of loaded AssetBundles to prevent redundant loading.
// - Dependency resolution using the AssetBundleManifest.
// - Flexible path resolution for different deployment scenarios (StreamingAssets, PersistentDataPath).
// - Error handling with informative debug messages.
// - Clear separation of concerns.
//
// How to use this script:
// 1. Create an empty GameObject in your scene, name it "AssetBundleManager".
// 2. Attach this script to the "AssetBundleManager" GameObject.
// 3. **Build AssetBundles for your project.**
//    For this example, we assume bundles are built into
//    'Assets/StreamingAssets/AssetBundles/[Platform]' folder.
//    
//    To build asset bundles:
//    a. Create a folder named "AssetBundles" in your Unity Project view (e.g., `Assets/AssetBundles_Raw`).
//    b. Select assets you want to put into bundles (e.g., a Prefab, a Texture, a Material).
//    c. In the Inspector for the selected asset, at the bottom, there's a dropdown
//       menu labeled "AssetBundle". Assign a new or existing bundle name (e.g., "mybundle", "anotherbundle").
//    d. Create an Editor script (e.g., `Assets/Editor/BuildAssetBundles.cs`):
//       ```csharp
//       // Assets/Editor/BuildAssetBundles.cs
//       using UnityEditor;
//       using System.IO;
//       
//       public class BuildAssetBundles
//       {
//           [MenuItem("Assets/Build AssetBundles")]
//           static void BuildAllAssetBundles()
//           {
//               // Define the output directory for AssetBundles.
//               // AssetBundles are platform-specific, so they are placed in a subfolder
//               // corresponding to the active build target (e.g., StandaloneWindows).
//               string assetBundleOutputDirectory = Path.Combine(Application.streamingAssetsPath, "AssetBundles");
//               
//               // Ensure the directory exists.
//               if(!Directory.Exists(Application.streamingAssetsPath))
//               {
//                   Directory.CreateDirectory(Application.streamingAssetsPath);
//               }
//               if(!Directory.Exists(assetBundleOutputDirectory))
//               {
//                   Directory.CreateDirectory(assetBundleOutputDirectory);
//               }
//               
//               // Build the AssetBundles. BuildAssetBundleOptions.None is typical.
//               // Other options include ChunkBasedCompression, UncompressedAssetBundle, etc.
//               BuildPipeline.BuildAssetBundles(assetBundleOutputDirectory, 
//                                               BuildAssetBundleOptions.None, 
//                                               EditorUserBuildSettings.activeBuildTarget);
//                                               
//               Debug.Log("Asset Bundles built to: " + assetBundleOutputDirectory);
//           }
//       }
//       ```
//    e. Go to "Assets" -> "Build AssetBundles" in the Unity Editor menu.
//       This will create the bundles and a manifest file (e.g., "StandaloneWindows")
//       in `Assets/StreamingAssets/AssetBundles/[Platform]`.
//
// Example Usage (in another script, e.g., attached to a GameInitializer GameObject):
// ```csharp
// using UnityEngine;
// using System.Collections;
// 
// public class GameInitializer : MonoBehaviour
// {
//     [Header("Asset Bundle Names")]
//     public string mainBundleName = "mybundle"; // Example bundle name
//     public string mainAssetName = "MyPrefab"; // Example asset name within 'mybundle'
//     public string textureBundleName = "anotherbundle"; // Another example bundle
//     public string textureAssetName = "MyTexture"; // Another example asset
// 
//     void Start()
//     {
//         StartCoroutine(InitializeAndLoadAssets());
//     }
// 
//     IEnumerator InitializeAndLoadAssets()
//     {
//         Debug.Log("AssetBundleManager: Initializing...");
//         // Step 1: Initialize the manager. This loads the AssetBundleManifest,
//         // which is critical for dependency tracking.
//         yield return AssetBundleManager.Instance.Initialize();
//         Debug.Log("AssetBundleManager: Initialization complete.");
// 
//         if (!AssetBundleManager.Instance.IsInitialized)
//         {
//             Debug.LogError("AssetBundleManager failed to initialize. Cannot load assets.");
//             yield break;
//         }
// 
//         // --- Example 1: Load a prefab from 'mybundle' ---
//         Debug.Log($"AssetBundleManager: Loading '{mainAssetName}' from '{mainBundleName}'...");
//         // LoadAssetAsync will automatically load the bundle and its dependencies if not already loaded.
//         yield return AssetBundleManager.Instance.LoadAssetAsync<GameObject>(mainBundleName, mainAssetName);
//         
//         // Retrieve the loaded asset from the manager's cache.
//         GameObject myPrefab = AssetBundleManager.Instance.GetAsset<GameObject>(mainBundleName, mainAssetName);
// 
//         if (myPrefab != null)
//         {
//             Debug.Log("AssetBundleManager: Instantiating MyPrefab...");
//             Instantiate(myPrefab);
//         }
//         else
//         {
//             Debug.LogError($"AssetBundleManager: Failed to load {mainAssetName}. Check bundle name, asset name, and type.");
//         }
// 
//         // --- Example 2: Load a texture from 'anotherbundle' ---
//         Debug.Log($"AssetBundleManager: Loading '{textureAssetName}' from '{textureBundleName}'...");
//         yield return AssetBundleManager.Instance.LoadAssetAsync<Texture2D>(textureBundleName, textureAssetName);
//         Texture2D myTexture = AssetBundleManager.Instance.GetAsset<Texture2D>(textureBundleName, textureAssetName);
// 
//         if (myTexture != null)
//         {
//             Debug.Log($"AssetBundleManager: '{myTexture.name}' texture loaded successfully.");
//             // You could assign this texture to a Renderer, UI Image, etc.
//             // For example, if you have a RawImage component:
//             // RawImage image = FindObjectOfType<RawImage>();
//             // if (image != null) image.texture = myTexture;
//         }
//         else
//         {
//             Debug.LogError($"AssetBundleManager: Failed to load {textureAssetName}. Check bundle name, asset name, and type.");
//         }
//         
//         // --- Example 3: Unload a specific bundle ---
//         // IMPORTANT: In a production game, you'd typically implement a more sophisticated
//         // reference counting system for AssetBundles. This ensures bundles are only
//         // unloaded when NO assets from them are in use or referenced in memory.
//         // For this example, we demonstrate explicit unloading.
//         Debug.Log($"AssetBundleManager: Unloading '{mainBundleName}'...");
//         AssetBundleManager.Instance.UnloadAssetBundle(mainBundleName);
//         
//         // After unloading, GetAsset("mybundle", ...) will return null for assets from that bundle.
//         // Instantiated GameObjects or directly referenced Textures will remain in memory
//         // unless 'unloadAllLoadedObjects' is true (use with extreme caution!).
//         
//         // If you try to load 'MyPrefab' again, the bundle will be reloaded.
//         Debug.Log($"AssetBundleManager: Loading '{mainAssetName}' from '{mainBundleName}' again (should trigger reload)...");
//         yield return AssetBundleManager.Instance.LoadAssetAsync<GameObject>(mainBundleName, mainAssetName);
//         myPrefab = AssetBundleManager.Instance.GetAsset<GameObject>(mainBundleName, mainAssetName);
//         if (myPrefab != null) { Debug.Log("AssetBundleManager: MyPrefab re-loaded and available."); }
//         
//         // --- Example 4: Unload all bundles ---
//         // This is typically done on scene transitions or at game shutdown.
//         Debug.Log("AssetBundleManager: Unloading all bundles...");
//         AssetBundleManager.Instance.UnloadAllAssetBundles(true); // 'true' means also unload all objects that came from these bundles.
//         Debug.Log("AssetBundleManager: All bundles unloaded.");
// 
//         // After this, any attempts to GetAsset will return null, and future LoadAssetAsync
//         // calls will fully reload bundles and assets.
//     }
// }
// ```

/// <summary>
/// Manages the loading, caching, and unloading of Unity AssetBundles.
/// Implements the Singleton pattern for easy global access.
/// </summary>
public class AssetBundleManager : MonoBehaviour
{
    // =====================================================================================
    // Singleton Instance
    // =====================================================================================
    // Provides a globally accessible single instance of the AssetBundleManager.
    // This makes it easy to call its methods from any other script without needing
    // to find or pass references.
    private static AssetBundleManager _instance;
    public static AssetBundleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene.
                _instance = FindObjectOfType<AssetBundleManager>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and attach the script.
                    GameObject singletonObject = new GameObject("AssetBundleManager");
                    _instance = singletonObject.AddComponent<AssetBundleManager>();
                    // Make sure the manager persists across scene loads.
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    // =====================================================================================
    // Configuration & State
    // =====================================================================================

    [Header("AssetBundle Configuration")]
    [Tooltip("The base folder name where AssetBundles are located within StreamingAssets or PersistentDataPath.")]
    public string assetBundleBaseFolder = "AssetBundles";

    // A dictionary to cache currently loaded AssetBundle objects.
    // Key: AssetBundle name (string, e.g., "mybundle")
    // Value: The loaded AssetBundle instance
    private Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();

    // A dictionary to cache specific assets loaded from bundles.
    // This prevents re-loading the same asset from a bundle if it's requested multiple times
    // while the bundle is still loaded.
    // Key: Combination of AssetBundle name and Asset name (e.g., "mybundle_MyPrefab")
    // Value: The loaded UnityEngine.Object (e.g., GameObject, Texture2D)
    private Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();

    // The AssetBundleManifest contains crucial information about all bundles,
    // especially their dependencies on each other. It's essential for correct loading.
    private AssetBundleManifest _assetBundleManifest;

    // A flag to indicate if the AssetBundleManager has successfully loaded its manifest
    // and is ready to load AssetBundles.
    public bool IsInitialized { get; private set; } = false;

    // The name of the platform-specific folder where bundles are located (e.g., "StandaloneWindows", "Android").
    // This is determined at runtime based on Application.platform.
    private string _platformFolder;

    // =====================================================================================
    // Initialization
    // =====================================================================================

    // Called when the script instance is being loaded. Ensures the singleton pattern.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If another instance already exists, destroy this one to maintain uniqueness.
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Keep the manager alive across scene changes.
        
        // Determine the correct platform folder name for bundle paths.
        _platformFolder = GetPlatformFolderForAssetBundles(Application.platform);
        Debug.Log($"AssetBundleManager: Initializing for platform: {_platformFolder}");
    }

    /// <summary>
    /// Asynchronously initializes the AssetBundleManager by loading the main AssetBundleManifest.
    /// This is a critical first step and MUST be called and completed before any
    /// other AssetBundle or asset loading operations can begin.
    /// The manifest provides dependency information.
    /// </summary>
    /// <returns>An IEnumerator for use with StartCoroutine.</returns>
    public IEnumerator Initialize()
    {
        if (IsInitialized)
        {
            Debug.LogWarning("AssetBundleManager: Manager is already initialized.");
            yield break; // Already initialized, nothing to do.
        }

        // The AssetBundleManifest itself is always contained within a special AssetBundle
        // that is named after the platform (e.g., "StandaloneWindows", "Android").
        string manifestBundleName = _platformFolder;
        string manifestPath = GetAssetBundlePath(manifestBundleName);

        Debug.Log($"AssetBundleManager: Attempting to load AssetBundleManifest from: {manifestPath}");

        // Use UnityWebRequestAssetBundle for consistent loading, especially for StreamingAssets
        // on Android or if bundles were to be loaded from remote URLs.
        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(manifestPath))
        {
            yield return request.SendWebRequest(); // Send the web request to load the bundle.

            // Check for network or protocol errors.
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"AssetBundleManager: Failed to load AssetBundleManifest: {request.error} at path {manifestPath}");
                IsInitialized = false;
                yield break;
            }

            // Get the loaded AssetBundle from the request.
            AssetBundle manifestBundle = DownloadHandlerAssetBundle.GetContent(request);
            if (manifestBundle == null)
            {
                Debug.LogError($"AssetBundleManager: Could not get AssetBundle content for manifest from {manifestPath}. " +
                               $"Make sure the manifest bundle '{manifestBundleName}' exists.");
                IsInitialized = false;
                yield break;
            }

            // The actual AssetBundleManifest object is an asset *within* this special manifest bundle.
            // It's always named "AssetBundleManifest".
            AssetBundleRequest assetRequest = manifestBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            yield return assetRequest; // Wait for the asset to load.

            _assetBundleManifest = assetRequest.asset as AssetBundleManifest;
            if (_assetBundleManifest == null)
            {
                Debug.LogError($"AssetBundleManager: Failed to load AssetBundleManifest object from bundle. " +
                               $"Is 'AssetBundleManifest' asset present in the '{_platformFolder}' bundle?");
                // If the manifest object itself can't be found, unload the bundle.
                manifestBundle.Unload(true); 
                IsInitialized = false;
                yield break;
            }

            // Once the _assetBundleManifest object is extracted, we no longer need the manifest bundle itself.
            // 'false' means it unloads the bundle data but keeps any loaded objects (like _assetBundleManifest) in memory.
            manifestBundle.Unload(false);
            
            IsInitialized = true;
            Debug.Log("AssetBundleManager: AssetBundleManager successfully initialized with AssetBundleManifest.");
        }
    }

    // =====================================================================================
    // AssetBundle Loading & Management
    // =====================================================================================

    /// <summary>
    /// Asynchronously loads a specific AssetBundle and all its dependencies.
    /// If the bundle (or its dependencies) are already loaded and cached,
    /// this method will return immediately for those bundles.
    /// This method is primarily an internal helper, but can be called directly if
    /// you only need to ensure a bundle is loaded without loading a specific asset from it.
    /// </summary>
    /// <param name="bundleName">The name of the AssetBundle to load (e.g., "mybundle").</param>
    /// <returns>An IEnumerator for use with StartCoroutine.</returns>
    public IEnumerator LoadAssetBundleAsync(string bundleName)
    {
        if (!IsInitialized)
        {
            Debug.LogError("AssetBundleManager: Manager is not initialized. Call Initialize() first.");
            yield break;
        }

        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError("AssetBundleManager: Bundle name cannot be null or empty.");
            yield break;
        }

        // If the bundle is already in our cache, it means it's loaded.
        // No need to load it again, just return.
        if (_loadedAssetBundles.ContainsKey(bundleName))
        {
            // Debug.Log($"AssetBundleManager: AssetBundle '{bundleName}' is already loaded.");
            yield break;
        }

        // --- Step 1: Load Dependencies ---
        // Retrieve all direct dependencies for the requested bundle from the manifest.
        string[] dependencies = _assetBundleManifest.GetAllDependencies(bundleName);
        if (dependencies.Length > 0)
        {
            // Debug.Log($"AssetBundleManager: Loading dependencies for '{bundleName}': {string.Join(", ", dependencies)}");
        }

        foreach (string dependency in dependencies)
        {
            // Recursively call LoadAssetBundleAsync for each dependency.
            // The caching mechanism at the start of this method prevents redundant loads
            // if a dependency is shared by multiple bundles.
            yield return LoadAssetBundleAsync(dependency);
        }

        // --- Step 2: Load the Requested Bundle itself ---
        string bundlePath = GetAssetBundlePath(bundleName);
        Debug.Log($"AssetBundleManager: Loading AssetBundle '{bundleName}' from: {bundlePath}");

        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"AssetBundleManager: Failed to load AssetBundle '{bundleName}': {request.error} at path {bundlePath}");
                // In a production system, you might want to handle this more gracefully,
                // e.g., retry, or notify the user.
                yield break;
            }

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle == null)
            {
                Debug.LogError($"AssetBundleManager: Could not get AssetBundle content for '{bundleName}' from {bundlePath}.");
                yield break;
            }

            // Add the successfully loaded bundle to our cache.
            _loadedAssetBundles[bundleName] = bundle;
            Debug.Log($"AssetBundleManager: AssetBundle '{bundleName}' loaded successfully.");
        }
    }

    /// <summary>
    /// Unloads a specific AssetBundle from memory. This will also clear any assets
    /// loaded from this bundle from the manager's asset cache.
    /// </summary>
    /// <param name="bundleName">The name of the AssetBundle to unload.</param>
    /// <param name="unloadAllLoadedObjects">
    /// If 'true', all assets that were loaded from this bundle and are currently in memory
    /// (e.g., textures, meshes, audio clips) will be unloaded and destroyed.
    /// Use with extreme caution, as this can destroy assets that are currently in use
    /// by existing GameObjects in your scene.
    /// If 'false', only the AssetBundle data itself is unloaded, and objects previously
    /// loaded from it remain in memory (until Unity's garbage collection or explicit destruction).
    /// </param>
    public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogWarning("AssetBundleManager: Cannot unload bundle: bundle name is null or empty.");
            return;
        }

        // Check if the bundle is actually in our loaded cache.
        if (_loadedAssetBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            Debug.Log($"AssetBundleManager: Unloading AssetBundle '{bundleName}' (unloadAllLoadedObjects: {unloadAllLoadedObjects}).");
            bundle.Unload(unloadAllLoadedObjects); // Perform the actual Unity unload operation.
            _loadedAssetBundles.Remove(bundleName); // Remove it from our cache.

            // Also clear any specific assets from this bundle from our _loadedAssets cache.
            // We need to iterate and create a temporary list because we can't modify the dictionary
            // while iterating over it.
            List<string> assetsToRemove = new List<string>();
            foreach (var key in _loadedAssets.Keys)
            {
                if (key.StartsWith($"{bundleName}_")) // Asset cache key format: "bundleName_assetName"
                {
                    assetsToRemove.Add(key);
                }
            }
            foreach (var key in assetsToRemove)
            {
                _loadedAssets.Remove(key);
            }
            Debug.Log($"AssetBundleManager: AssetBundle '{bundleName}' unloaded and associated assets cleared from cache.");
        }
        else
        {
            Debug.LogWarning($"AssetBundleManager: Attempted to unload AssetBundle '{bundleName}' but it was not found in cache.");
        }
        
        // --- IMPORTANT NOTE on Dependency Unloading ---
        // A truly robust AssetBundle management system often includes reference counting.
        // When a bundle is unloaded, its dependencies' reference counts would decrease.
        // If a dependency's reference count reaches zero (meaning no other loaded bundles
        // or active requests depend on it), that dependency could then also be unloaded.
        // This example keeps it simpler by only unloading the explicitly requested bundle.
    }

    /// <summary>
    /// Unloads all currently loaded AssetBundles from memory.
    /// This also clears the entire asset cache.
    /// </summary>
    /// <param name="unloadAllLoadedObjects">
    /// If 'true', all assets loaded from *any* bundle that are still in memory will be unloaded.
    /// Use with extreme caution, typically only when transitioning scenes or at game exit,
    /// as it can destroy many active game resources.
    /// </param>
    public void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
    {
        Debug.Log($"AssetBundleManager: Unloading all AssetBundles (unloadAllLoadedObjects: {unloadAllLoadedObjects}).");
        foreach (var pair in _loadedAssetBundles)
        {
            pair.Value.Unload(unloadAllLoadedObjects); // Unload each bundle.
        }
        _loadedAssetBundles.Clear(); // Clear the bundle cache.
        _loadedAssets.Clear();       // Clear all cached assets.
        Debug.Log("AssetBundleManager: All AssetBundles unloaded.");
    }

    // =====================================================================================
    // Asset Loading & Retrieval
    // =====================================================================================

    /// <summary>
    /// Asynchronously loads a specific asset of a given type from an AssetBundle.
    /// This method first ensures the target AssetBundle (and its dependencies) is loaded,
    /// then loads the specified asset from it. The loaded asset is then cached.
    /// </summary>
    /// <typeparam name="T">The type of the asset to load (e.g., GameObject, Texture2D, Material).</typeparam>
    /// <param name="bundleName">The name of the AssetBundle containing the asset.</param>
    /// <param name="assetName">The exact name of the asset to load (as defined in Unity).</param>
    /// <returns>An IEnumerator for use with StartCoroutine.</returns>
    public IEnumerator LoadAssetAsync<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (!IsInitialized)
        {
            Debug.LogError("AssetBundleManager: Manager is not initialized. Call Initialize() first.");
            yield break;
        }

        if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("AssetBundleManager: Bundle name and asset name cannot be null or empty.");
            yield break;
        }

        // Create a unique key for the asset cache.
        string assetCacheKey = $"{bundleName}_{assetName}";
        if (_loadedAssets.ContainsKey(assetCacheKey))
        {
            // Debug.Log($"AssetBundleManager: Asset '{assetName}' from bundle '{bundleName}' already in asset cache.");
            yield break; // Asset is already loaded and cached, no need to re-load.
        }

        // Ensure the AssetBundle itself is loaded. This call will handle dependencies and caching.
        yield return LoadAssetBundleAsync(bundleName);

        // After LoadAssetBundleAsync, check if the bundle is indeed loaded.
        if (!_loadedAssetBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            Debug.LogError($"AssetBundleManager: AssetBundle '{bundleName}' was not loaded, cannot load asset '{assetName}'. " +
                           "Check for previous errors during bundle loading.");
            yield break;
        }

        Debug.Log($"AssetBundleManager: Loading asset '{assetName}' from bundle '{bundleName}'...");
        AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
        yield return request; // Wait for the asset to finish loading.

        T asset = request.asset as T;
        if (asset != null)
        {
            _loadedAssets[assetCacheKey] = asset; // Add the loaded asset to our cache.
            Debug.Log($"AssetBundleManager: Asset '{assetName}' loaded successfully from bundle '{bundleName}'.");
        }
        else
        {
            Debug.LogError($"AssetBundleManager: Failed to load asset '{assetName}' of type '{typeof(T).Name}' from bundle '{bundleName}'. " +
                           $"Check asset name, type, and ensure it exists in the bundle. " +
                           $"Available assets in '{bundleName}': {string.Join(", ", bundle.GetAllAssetNames())}");
        }
    }

    /// <summary>
    /// Retrieves a previously loaded asset from the manager's internal cache.
    /// This method does NOT initiate any loading; it only returns assets that have
    /// already been successfully loaded via `LoadAssetAsync`.
    /// </summary>
    /// <typeparam name="T">The expected type of the asset.</typeparam>
    /// <param name="bundleName">The name of the AssetBundle the asset originated from.</param>
    /// <param name="assetName">The name of the asset.</param>
    /// <returns>The loaded asset of type T, or null if the asset is not found in the cache.</returns>
    public T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("AssetBundleManager: Bundle name and asset name cannot be null or empty.");
            return null;
        }

        string assetCacheKey = $"{bundleName}_{assetName}";
        if (_loadedAssets.TryGetValue(assetCacheKey, out UnityEngine.Object asset))
        {
            return asset as T;
        }
        Debug.LogWarning($"AssetBundleManager: Asset '{assetName}' from bundle '{bundleName}' not found in cache. Was it loaded using LoadAssetAsync?");
        return null;
    }

    // =====================================================================================
    // Path Resolution Helpers
    // =====================================================================================

    /// <summary>
    /// Constructs the full, platform-specific path to an AssetBundle.
    /// This example primarily targets AssetBundles located in `Application.streamingAssetsPath`.
    /// In a more complex scenario, you might check `Application.persistentDataPath` first
    /// for updated bundles (downloaded from a server), and then fall back to StreamingAssets.
    /// </summary>
    /// <param name="bundleName">The name of the AssetBundle (e.g., "mybundle" or "standalonewindows").</param>
    /// <returns>The full path (including "file://" prefix where necessary) to the AssetBundle.</returns>
    private string GetAssetBundlePath(string bundleName)
    {
        // Construct the path within StreamingAssets.
        // Example: Application.streamingAssetsPath/AssetBundles/StandaloneWindows/mybundle
        string streamingAssetsBundlePath = Path.Combine(Application.streamingAssetsPath, assetBundleBaseFolder, _platformFolder, bundleName);
        
        // UnityWebRequestAssetBundle.GetAssetBundle handles paths differently on various platforms.
        // For Android, StreamingAssets are inside the APK/JAR, so a direct file path won't work
        // for `File.Exists` or `new WWW()`. `UnityWebRequest` handles this correctly internally.
        // For other platforms, it typically expects a "file://" prefix for local files.

        #if UNITY_ANDROID
            // On Android, UnityWebRequest automatically understands paths within StreamingAssets.
            return streamingAssetsBundlePath; 
        #elif UNITY_IOS || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            // On other platforms, it's generally good practice to prepend "file://" for local files.
            // UnityWebRequest can often handle bare paths, but this is safer for consistency.
            return "file://" + streamingAssetsBundlePath;
        #else
            // Fallback for any other platforms. May require adjustment.
            Debug.LogWarning($"AssetBundleManager: Untested platform '{Application.platform}' for AssetBundle pathing. " +
                             "Using direct path. If loading fails, consider adjusting GetAssetBundlePath.");
            return streamingAssetsBundlePath;
        #endif
    }

    /// <summary>
    /// Determines the correct platform-specific folder name for AssetBundles
    /// based on the current Unity RuntimePlatform.
    /// This name must match the folder created by `BuildPipeline.BuildAssetBundles`.
    /// </summary>
    /// <param name="platform">The current RuntimePlatform (e.g., RuntimePlatform.Android).</param>
    /// <returns>The platform-specific folder name (e.g., "Android", "StandaloneWindows").</returns>
    private string GetPlatformFolderForAssetBundles(RuntimePlatform platform)
    {
        switch (platform)
        {
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IOS:
                return "iOS";
            case RuntimePlatform.WebGLPlayer:
                return "WebGL";
            // Standalone platforms: Windows, macOS, Linux typically share "Standalone" prefix.
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return "StandaloneWindows";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "StandaloneOSX";
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                return "StandaloneLinux";
            // Add more cases for other platforms as needed (e.g., Switch, PS4, XboxOne)
            default:
                Debug.LogWarning($"AssetBundleManager: Unknown platform '{platform}', using 'Standalone' as default for AssetBundles. " +
                                 "Ensure AssetBundles are built for this platform name or add a specific case.");
                return "Standalone"; // Generic fallback, might not work depending on build settings.
        }
    }
}
```