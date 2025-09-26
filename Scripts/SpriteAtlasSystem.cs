// Unity Design Pattern Example: SpriteAtlasSystem
// This script demonstrates the SpriteAtlasSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'SpriteAtlasSystem' design pattern in Unity using a `SpriteAtlasManager` singleton. This pattern centralizes the loading, caching, and retrieval of sprites from Sprite Atlases, providing a robust and efficient way to manage visual assets in your game.

### SpriteAtlasSystem Design Pattern Explained

**Purpose:**
The SpriteAtlasSystem aims to provide a centralized, efficient, and flexible way to manage sprites that are packed into Unity's `SpriteAtlas` assets. It addresses common challenges like:
1.  **Performance:** Loading individual sprites or even entire atlases repeatedly can be costly. Caching loaded assets improves performance significantly.
2.  **Memory Management:** Loading many atlases or individual sprites can consume large amounts of memory. This system allows for controlled loading and unloading.
3.  **Decoupling:** GameObjects or UI elements don't directly reference sprite assets. Instead, they request sprites by name from the manager, making asset changes (like repacking an atlas or renaming a sprite) less disruptive to code.
4.  **Consistency:** Ensures all parts of the application retrieve sprites through a consistent interface.

**Key Components:**
*   **Singleton Manager (`SpriteAtlasManager`):** A single, globally accessible instance responsible for all atlas and sprite operations.
*   **Atlas Cache:** A dictionary (`Dictionary<string, SpriteAtlas>`) to store references to already loaded `SpriteAtlas` objects, keyed by their path or name. This prevents redundant atlas loading from disk.
*   **Sprite Cache:** A dictionary (`Dictionary<string, Sprite>`) to store references to individual `Sprite` objects that have been extracted from atlases. This prevents repeated `atlas.GetSprite()` calls, which can be slightly slower than a direct dictionary lookup.
*   **Loading Mechanism:** Methods to load atlases (e.g., using `Resources.Load` or `Addressables`).
*   **Retrieval Method (`GetSprite`):** The primary public interface for requesting a sprite by its atlas path and sprite name.

**Benefits:**
*   **Reduced Load Times:** Sprites and atlases are loaded only once.
*   **Optimized Memory:** Controlled loading/unloading of atlases.
*   **Easier Maintenance:** Changes to asset organization (e.g., moving an atlas) require updates in fewer places.
*   **Scalability:** Well-suited for projects with many sprites and complex UI.

---

### `SpriteAtlasManager.cs` Script

```csharp
using UnityEngine;
using UnityEngine.U2D; // Required for SpriteAtlas
using System.Collections.Generic; // Required for Dictionaries
using System.Threading.Tasks; // For potential async operations (discussed in comments)

/// <summary>
///     The SpriteAtlasManager implements the 'SpriteAtlasSystem' design pattern in Unity.
///     It centralizes the loading, caching, and management of Sprite Atlases and individual Sprites.
///
///     This pattern aims to:
///     1.  **Decouple:** Remove direct references to individual sprites from GameObjects,
///         making it easier to update or replace sprite assets without modifying numerous GameObjects.
///     2.  **Optimize Performance:** Cache loaded SpriteAtlases and individual Sprites to prevent
///         repeated costly disk I/O operations (e.g., `Resources.Load` or Addressables loads).
///     3.  **Memory Management:** Provide a controlled way to load and unload atlases,
///         reducing memory footprint when assets are no longer needed.
///     4.  **Consistency:** Ensure a single, consistent way of accessing sprites across the entire application.
///
///     **Implementation Details:**
///     -   **Singleton:** Ensures only one instance of the manager exists globally, providing
///         a single point of access.
///     -   **Caching:** Uses Dictionaries to store references to `SpriteAtlas` objects and
///         individual `Sprite` objects. The keys are typically strings representing paths or names.
///     -   **Unity's SpriteAtlas API:** Leverages `UnityEngine.U2D.SpriteAtlas` and its `GetSprite()` method.
///     -   **Asset Loading:** This example primarily uses `Resources.Load`. For production-grade games,
///         Unity's Addressable Asset System is highly recommended for its advanced features
///         (asynchronous loading, remote content, reference counting), which is discussed conceptually below.
/// </summary>
public class SpriteAtlasManager : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static SpriteAtlasManager _instance;

    /// <summary>
    ///     Provides the global access point to the SpriteAtlasManager instance.
    ///     This is a common way to implement a Singleton in Unity, ensuring only one
    ///     instance exists and creating it if necessary.
    /// </summary>
    public static SpriteAtlasManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<SpriteAtlasManager>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject(typeof(SpriteAtlasManager).Name);
                    _instance = singletonObject.AddComponent<SpriteAtlasManager>();
                }

                // Ensure the manager persists across scene loads to maintain its state (cached atlases).
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // --- Caching Dictionaries ---
    // Stores loaded SpriteAtlases, keyed by their resource path (e.g., "Atlases/UIAtlas").
    // This prevents repeated disk I/O for the same atlas.
    private readonly Dictionary<string, SpriteAtlas> _atlasCache = new Dictionary<string, SpriteAtlas>();

    // Stores individual sprites extracted from atlases, keyed by "{atlasPath}/{spriteName}".
    // This prevents repeated calls to atlas.GetSprite() which can be slightly less performant
    // than a direct dictionary lookup, especially if an atlas contains many sprites.
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    // --- Initialization and Cleanup ---
    private void Awake()
    {
        // If another instance already exists (e.g., from a previous scene load), destroy this duplicate.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set this instance as the global singleton.
        _instance = this;
        // Keep this GameObject alive across scene loads.
        DontDestroyOnLoad(gameObject);

        Debug.Log("SpriteAtlasManager initialized.");
    }

    private void OnDestroy()
    {
        // Clear the static instance reference if this manager GameObject is destroyed.
        if (_instance == this)
        {
            _instance = null;
            Debug.Log("SpriteAtlasManager destroyed. Cache references cleared.");
            // Note: Actual memory release for Resources.Load assets requires manual calls to Resources.UnloadAsset
            // or implicitly happens when the scene that loaded them is unloaded (if not DontDestroyOnLoad).
            // For Addressables, you would explicitly release asset handles here.
        }
    }

    /// <summary>
    ///     Loads a SpriteAtlas from a 'Resources' folder if it hasn't been loaded yet
    ///     and caches it for future use. This is an internal helper method.
    /// </summary>
    /// <param name="atlasPath">The path to the SpriteAtlas asset within a Resources folder
    ///                           (e.g., "Atlases/MyGameAtlas"). Do NOT include the file extension (.spriteatlas).</param>
    /// <returns>The loaded SpriteAtlas, or null if not found.</returns>
    private SpriteAtlas LoadAtlasInternal(string atlasPath)
    {
        if (string.IsNullOrEmpty(atlasPath))
        {
            Debug.LogError("SpriteAtlasManager: Cannot load atlas, atlasPath is null or empty.");
            return null;
        }

        // 1. Check if the atlas is already in our cache.
        if (_atlasCache.TryGetValue(atlasPath, out SpriteAtlas atlas))
        {
            return atlas; // Return cached atlas immediately.
        }

        // 2. If not in cache, load it from the Resources folder.
        //    'Resources.Load' is synchronous and can cause hitches for large assets.
        //    For production, consider Addressables.
        atlas = Resources.Load<SpriteAtlas>(atlasPath);

        if (atlas == null)
        {
            Debug.LogError($"SpriteAtlasManager: Failed to load SpriteAtlas at path: '{atlasPath}'. " +
                           "Make sure the atlas is placed in a 'Resources' folder and the path is correct " +
                           " (e.g., 'Assets/Resources/Atlases/MyAtlas.spriteatlas' means path is 'Atlases/MyAtlas').");
            return null;
        }

        // 3. Add the newly loaded atlas to the cache.
        _atlasCache.Add(atlasPath, atlas);
        Debug.Log($"SpriteAtlasManager: Loaded and cached atlas: '{atlasPath}'");
        return atlas;
    }

    /// <summary>
    ///     Retrieves a specific Sprite from a SpriteAtlas.
    ///     This is the primary public method for clients to get sprites.
    ///     It handles loading the atlas (if not already loaded) and then finding
    ///     and caching the individual sprite for optimal performance.
    /// </summary>
    /// <param name="atlasPath">The path to the SpriteAtlas asset within a Resources folder
    ///                           (e.g., "Atlases/UIAtlas"). Do NOT include the file extension.</param>
    /// <param name="spriteName">The name of the sprite *within* the atlas (typically the original
    ///                           filename of the sprite asset before it was packed).</param>
    /// <returns>The requested Sprite, or null if the atlas or sprite could not be found.</returns>
    public Sprite GetSprite(string atlasPath, string spriteName)
    {
        if (string.IsNullOrEmpty(atlasPath) || string.IsNullOrEmpty(spriteName))
        {
            Debug.LogError("SpriteAtlasManager: Cannot get sprite. atlasPath or spriteName is null or empty.");
            return null;
        }

        // Create a unique key for the individual sprite cache (e.g., "Atlases/UIAtlas/my_icon_01").
        string spriteCacheKey = $"{atlasPath}/{spriteName}";

        // 1. Check the individual sprite cache first. This is the fastest lookup.
        if (_spriteCache.TryGetValue(spriteCacheKey, out Sprite sprite))
        {
            return sprite; // Return cached sprite immediately.
        }

        // 2. If the sprite is not cached, get the containing atlas.
        SpriteAtlas atlas;
        if (!_atlasCache.TryGetValue(atlasPath, out atlas))
        {
            // Atlas is not in cache, attempt to load it using our internal method.
            atlas = LoadAtlasInternal(atlasPath);
            if (atlas == null)
            {
                // Atlas loading failed, so we can't get the sprite.
                Debug.LogError($"SpriteAtlasManager: Could not get sprite '{spriteName}' because atlas '{atlasPath}' could not be loaded.");
                return null;
            }
        }

        // 3. Now that we have a valid atlas (either from cache or newly loaded),
        //    request the specific sprite from the atlas.
        sprite = atlas.GetSprite(spriteName);

        if (sprite == null)
        {
            Debug.LogWarning($"SpriteAtlasManager: Sprite '{spriteName}' not found in atlas '{atlasPath}'. " +
                             "Ensure the sprite name is correct and it is packed within the specified atlas.");
            return null;
        }

        // 4. Cache the found individual sprite for future requests.
        _spriteCache.Add(spriteCacheKey, sprite);
        // Debug.Log($"SpriteAtlasManager: Found and cached sprite '{spriteName}' from atlas '{atlasPath}'.");
        return sprite;
    }

    /// <summary>
    ///     Attempts to unload a specific SpriteAtlas and remove all its contained sprites from the cache.
    ///     This is important for memory management, especially when an atlas is no longer needed
    ///     (e.g., after leaving a specific game section).
    /// </summary>
    /// <param name="atlasPath">The path to the SpriteAtlas asset to unload.</param>
    public void UnloadAtlas(string atlasPath)
    {
        if (string.IsNullOrEmpty(atlasPath))
        {
            Debug.LogWarning("SpriteAtlasManager: Cannot unload atlas, atlasPath is null or empty.");
            return;
        }

        if (_atlasCache.TryGetValue(atlasPath, out SpriteAtlas atlas))
        {
            // 1. Remove the atlas from its cache.
            _atlasCache.Remove(atlasPath);

            // 2. Remove all individual sprites associated with this atlas from the sprite cache.
            //    We iterate and collect keys to avoid modifying the dictionary while iterating.
            List<string> spritesToRemove = new List<string>();
            foreach (var kvp in _spriteCache)
            {
                if (kvp.Key.StartsWith($"{atlasPath}/"))
                {
                    spritesToRemove.Add(kvp.Key);
                }
            }
            foreach (string key in spritesToRemove)
            {
                _spriteCache.Remove(key);
            }

            // 3. For Resources.Load, attempt to unload the specific asset.
            //    Note: Unity's Resources.UnloadAsset() might not always fully free memory
            //    if other parts of the application still hold direct references to it.
            Resources.UnloadAsset(atlas);
            Debug.Log($"SpriteAtlasManager: Unloaded atlas '{atlasPath}' and its cached sprites. " +
                      "Memory might not be immediately freed by Resources.UnloadAsset(). " +
                      "Consider Resources.UnloadUnusedAssets() for more aggressive, but slower, cleanup.");
        }
        else
        {
            Debug.LogWarning($"SpriteAtlasManager: Atlas '{atlasPath}' not found in cache, nothing to unload.");
        }
    }

    /// <summary>
    ///     Clears all cached SpriteAtlases and individual Sprites.
    ///     Useful for a comprehensive memory cleanup, e.g., when transitioning between major game sections
    ///     or returning to a main menu.
    /// </summary>
    public void ClearAllCaches()
    {
        // For each cached atlas, attempt to unload it using Unity's API.
        foreach (var kvp in _atlasCache)
        {
            Resources.UnloadAsset(kvp.Value);
        }
        _atlasCache.Clear(); // Clear dictionary references.
        _spriteCache.Clear(); // Clear dictionary references.

        Debug.Log("SpriteAtlasManager: All atlas and sprite caches cleared. Resources.UnloadAsset called on cached atlases.");
        
        // Optional: Call Resources.UnloadUnusedAssets() for a more aggressive memory sweep.
        // This will unload *any* assets in memory that no longer have references.
        // It can be a performance-heavy operation, so use it judiciously (e.g., during loading screens).
        // Resources.UnloadUnusedAssets();
    }


    // --- ADVANCED CONSIDERATIONS: ASYNC LOADING AND ADDRESSABLES ---
    // The current example uses synchronous Resources.Load, which can cause frame hitches for very large atlases
    // or when loading many atlases simultaneously.
    // For production-grade games, especially with many or large atlases, asynchronous loading
    // and Unity's Addressable Asset System are highly recommended.

    /*
    /// <summary>
    ///     (Conceptual Example for Async Loading with Addressables - Not fully implemented here)
    ///     This would be the preferred method for loading atlases in a production environment.
    ///     Requires the Addressables package to be installed and atlases marked as Addressable.
    /// </summary>
    /// <param name="addressableKey">The addressable key (path or label) of the SpriteAtlas.</param>
    /// <returns>An async task that resolves to the loaded SpriteAtlas.</returns>
    public async Task<SpriteAtlas> LoadAtlasAsync(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError("SpriteAtlasManager: Cannot load atlas async, key is null or empty.");
            return null;
        }

        if (_atlasCache.TryGetValue(addressableKey, out SpriteAtlas atlas))
        {
            return atlas; // Atlas already cached.
        }

        // Example using Addressables (requires 'using UnityEngine.AddressableAssets;' and 'using UnityEngine.ResourceManagement.AsyncOperations;'):
        // AsyncOperationHandle<SpriteAtlas> handle = Addressables.LoadAssetAsync<SpriteAtlas>(addressableKey);
        // await handle.Task; // Wait for the loading to complete.
        // atlas = handle.Result;

        // if (atlas == null)
        // {
        //     Debug.LogError($"SpriteAtlasManager: Failed to load SpriteAtlas via Addressables: {addressableKey}");
        //     return null;
        // }

        // _atlasCache.Add(addressableKey, atlas);
        // Debug.Log($"SpriteAtlasManager: Loaded and cached atlas async: {addressableKey}");
        // return atlas;

        Debug.LogWarning("SpriteAtlasManager: Async loading with Addressables is conceptual. " +
                         "Please install Addressables package and uncomment the implementation.");
        return await Task.FromResult<SpriteAtlas>(null); // Placeholder return for the example.
    }

    /// <summary>
    ///     (Conceptual Example for Async Sprite Retrieval with Addressables - Not fully implemented)
    ///     This would be used in conjunction with `LoadAtlasAsync`.
    /// </summary>
    /// <param name="atlasKey">The addressable key for the atlas.</param>
    /// <param name="spriteName">The name of the sprite within the atlas.</param>
    /// <returns>An async task that resolves to the requested Sprite.</returns>
    public async Task<Sprite> GetSpriteAsync(string atlasKey, string spriteName)
    {
        string spriteCacheKey = $"{atlasKey}/{spriteName}";
        if (_spriteCache.TryGetValue(spriteCacheKey, out Sprite sprite))
        {
            return sprite; // Sprite already cached.
        }

        // Use the async atlas loader.
        SpriteAtlas atlas = await LoadAtlasAsync(atlasKey);
        if (atlas == null)
        {
            return null;
        }

        // Once the atlas is loaded, getting the sprite from it is usually synchronous.
        sprite = atlas.GetSprite(spriteName);

        if (sprite == null)
        {
            Debug.LogWarning($"SpriteAtlasManager: Sprite '{spriteName}' not found in atlas '{atlasKey}'.");
        }
        else
        {
            _spriteCache.Add(spriteCacheKey, sprite); // Cache the sprite.
        }

        return sprite;
    }

    /// <summary>
    ///     (Conceptual Example for Addressables Cleanup - Not fully implemented)
    ///     When using Addressables, it's crucial to release asset handles to manage memory effectively.
    /// </summary>
    public void ReleaseAddressableAtlas(string addressableKey)
    {
        if (_atlasCache.TryGetValue(addressableKey, out SpriteAtlas atlas))
        {
            // Addressables.Release(atlas); // This releases the Addressables asset handle
            _atlasCache.Remove(addressableKey);
            // Also remove related sprites from _spriteCache here
            // (Similar logic as in UnloadAtlas but adapted for Addressables handles)
        }
    }
    */
}
```

---

### Example Usage: How to Integrate into Your Unity Project

This section provides steps and a sample script to demonstrate how to use the `SpriteAtlasManager`.

#### 1. Prepare Your Unity Project

*   **Create Sprite Atlases:**
    *   In Unity, go to `Assets > Create > 2D > Sprite Atlas`.
    *   Name your atlas (e.g., `UIAtlas`).
    *   In the Inspector of the `SpriteAtlas` asset, drag and drop your individual sprite textures (e.g., icons, UI elements) into the "Objects for Packing" list.
    *   Click "Pack Preview" (or "Pack") to generate the atlas texture.

*   **Place Atlases in a `Resources` Folder:**
    *   Create a folder named `Resources` anywhere in your `Assets` folder (e.g., `Assets/Resources`).
    *   Optionally, create a subfolder within `Resources` for better organization (e.g., `Assets/Resources/Atlases`).
    *   Move your `UIAtlas.spriteatlas` file into this `Assets/Resources/Atlases` folder.
    *   The `atlasPath` you'll use in code will then be `"Atlases/UIAtlas"`. Remember: **no file extension**.
    *   **Crucially:** Ensure the individual sprites you packed into the atlas also have distinct names. The `spriteName` you request will be the original name of the sprite asset.

#### 2. Create an Example Component (`MyIconDisplay.cs`)

This script will be attached to a GameObject and use the `SpriteAtlasManager` to set its `SpriteRenderer`'s sprite.

```csharp
using UnityEngine;

/// <summary>
///     Example component demonstrating how to use the SpriteAtlasManager to set and change sprites.
///     Attach this script to a GameObject that has a SpriteRenderer component.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))] // Ensures the GameObject has a SpriteRenderer
public class MyIconDisplay : MonoBehaviour
{
    [Tooltip("The path to the SpriteAtlas in a Resources folder (e.g., 'Atlases/UIAtlas'). Do NOT include extension.")]
    public string atlasPath = "Atlases/UIAtlas"; // Default path, adjust in Inspector

    [Tooltip("The name of the sprite within the atlas (e.g., 'coin_icon'). This is the original asset name.")]
    public string spriteName = "default_icon"; // Default sprite name, adjust in Inspector

    private SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("MyIconDisplay requires a SpriteRenderer component, but none was found. Disabling script.");
            enabled = false; // Disable the script if its dependency is missing.
        }
    }

    void Start()
    {
        // Request the initial sprite from the SpriteAtlasManager.
        // This implicitly triggers the atlas loading and sprite caching if not already done.
        SetIcon(spriteName); 
    }

    /// <summary>
    ///     Sets the SpriteRenderer's sprite using a sprite retrieved from the SpriteAtlasManager.
    /// </summary>
    /// <param name="newSpriteName">The name of the sprite to retrieve from the specified atlas.</param>
    public void SetIcon(string newSpriteName)
    {
        Sprite newSprite = SpriteAtlasManager.Instance.GetSprite(atlasPath, newSpriteName);

        if (newSprite != null)
        {
            _spriteRenderer.sprite = newSprite;
            Debug.Log($"MyIconDisplay: Successfully set sprite to '{newSpriteName}' from atlas '{atlasPath}'.");
        }
        else
        {
            Debug.LogWarning($"MyIconDisplay: Failed to set sprite '{newSpriteName}' from atlas '{atlasPath}'. " +
                             "Check logs for more details (atlas/sprite not found).");
        }
    }

    // Example of how you might trigger dynamic sprite changes (e.g., from a button click or game event)
    [ContextMenu("Change Sprite to AnotherIcon")] // Adds an option to the Inspector context menu
    public void ChangeToAnotherIcon()
    {
        // Make sure "another_icon" exists in your UIAtlas
        SetIcon("another_icon"); 
    }

    [ContextMenu("Unload UIAtlas and Clear Caches")]
    public void UnloadAndClear()
    {
        Debug.Log("Initiating unload and clear for UIAtlas.");
        SpriteAtlasManager.Instance.UnloadAtlas(atlasPath);
        // Optionally clear all caches, but usually UnloadAtlas is sufficient for specific atlases
        // SpriteAtlasManager.Instance.ClearAllCaches(); 
        _spriteRenderer.sprite = null; // Clear the current sprite
    }
}
```

#### 3. Set Up Your Scene

1.  Create a new C# script named `SpriteAtlasManager.cs` and paste the first block of code into it.
2.  Create a new C# script named `MyIconDisplay.cs` and paste the second block of code into it.
3.  In your Unity scene, create an empty GameObject (e.g., named "IconDisplay").
4.  Add a `SpriteRenderer` component to this "IconDisplay" GameObject.
5.  Add the `MyIconDisplay.cs` script to the same "IconDisplay" GameObject.
6.  In the Inspector for "IconDisplay", configure the `MyIconDisplay` component:
    *   Set `Atlas Path` to the path of your atlas (e.g., `"Atlases/UIAtlas"`).
    *   Set `Sprite Name` to the name of one of the sprites packed in that atlas (e.g., `"coin_icon"` or whatever you named your individual sprite asset).
7.  Run the scene. The `SpriteRenderer` on "IconDisplay" should now display the specified sprite.
8.  You can change the `Sprite Name` in the Inspector while the game is running to see how the sprite dynamically updates, leveraging the manager's caching. You can also use the right-click context menu on `MyIconDisplay` in the Inspector to call `ChangeToAnotherIcon` or `UnloadAndClear`.

This complete example provides a robust foundation for managing sprites using the SpriteAtlasSystem pattern in your Unity projects. Remember to adapt `atlasPath` and `spriteName` to match your actual asset names and folder structure.