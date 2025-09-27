// Unity Design Pattern Example: TextureStreaming
// This script demonstrates the TextureStreaming pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a **Texture Streaming** pattern in Unity by implementing a system for **dynamic texture asset loading and swapping** based on distance to the camera. While Unity has a built-in "Texture Streaming" feature for mipmaps, this pattern addresses a common need to load entirely different texture assets (e.g., a low-resolution diffuse map vs. a full PBR texture set) to optimize memory and visual quality in your game.

This approach provides more fine-grained control over which specific texture assets are loaded into memory at any given time, making it highly valuable for projects with many unique high-resolution assets, like open-world games.

---

### **Understanding the Texture Streaming Design Pattern (in this context)**

The core idea is to manage the `Texture2D` assets used by objects in your scene:

1.  **Low-Resolution Default:** Objects initially use a low-resolution texture to minimize initial memory footprint and loading times.
2.  **Distance-Based Trigger:** As the player (or camera) approaches an object, a manager component detects this distance.
3.  **Asynchronous High-Resolution Load:** When an object is close enough, the system asynchronously loads its higher-resolution texture asset into memory.
4.  **Texture Swap:** Once the high-resolution texture is loaded, it's applied to the object's material, replacing the low-resolution one.
5.  **Unloading (Optional but Crucial):** When objects move far away, their high-resolution textures can be unloaded (or reverted to low-res) to free up VRAM and system memory. This example provides a basic caching mechanism and hints at more robust unloading strategies.

---

### **C# Unity Scripts**

You'll need three parts for this example:

1.  **`TextureQuality.cs`**: An enum to define different quality levels.
2.  **`StreamableTextureComponent.cs`**: A component to attach to GameObjects that need streaming. It holds paths to the different texture assets.
3.  **`TextureStreamingManager.cs`**: A singleton manager that orchestrates the streaming process, checking distances, triggering loads, and caching textures.

---

#### 1. `TextureQuality.cs`

```csharp
// TextureQuality.cs
using UnityEngine;

/// <summary>
/// Defines the different quality levels for streamable textures.
/// </summary>
public enum TextureQuality
{
    /// <summary>
    /// Represents the lowest quality texture (e.g., small diffuse map).
    /// </summary>
    Low,
    /// <summary>
    /// Represents a medium quality texture (optional, not fully implemented in this example but can be extended).
    /// </summary>
    Medium,
    /// <summary>
    /// Represents the highest quality texture (e.g., full PBR texture set).
    /// </summary>
    High
}
```

#### 2. `StreamableTextureComponent.cs`

```csharp
// StreamableTextureComponent.cs
using UnityEngine;
using System.Collections;
using System; // For Action delegate

/// <summary>
/// This component is attached to a GameObject that should have its texture quality
/// dynamically managed by the TextureStreamingManager.
/// It holds references or paths to different quality versions of its main texture.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class StreamableTextureComponent : MonoBehaviour
{
    [Tooltip("The path to the low-resolution texture in a Resources folder (e.g., 'Textures/my_object_low').")]
    public string lowResTexturePath;
    [Tooltip("The path to the high-resolution texture in a Resources folder (e.g., 'Textures/my_object_high').")]
    public string highResTexturePath;

    // A placeholder texture to use if no texture is assigned or while loading.
    // Recommended to have a small, easily loadable placeholder.
    [Tooltip("Optional: A placeholder texture to use if paths are invalid or during loading.")]
    public Texture2D placeholderTexture;

    private Renderer _renderer;
    private TextureQuality _currentAppliedQuality = TextureQuality.Low;
    private Texture2D _currentAppliedTexture; // The actual texture currently applied
    private Coroutine _loadingCoroutine; // To manage asynchronous loading

    // Public property to expose the current applied quality
    public TextureQuality CurrentAppliedQuality => _currentAppliedQuality;

    /// <summary>
    /// Returns the appropriate texture path based on the requested quality.
    /// </summary>
    /// <param name="quality">The desired texture quality.</param>
    /// <returns>The string path to the texture in Resources.</returns>
    public string GetTexturePathForQuality(TextureQuality quality)
    {
        switch (quality)
        {
            case TextureQuality.Low: return lowResTexturePath;
            case TextureQuality.High: return highResTexturePath;
            default: return lowResTexturePath; // Default to low if no specific path
        }
    }

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"StreamableTextureComponent on {gameObject.name} requires a Renderer.", this);
            enabled = false;
            return;
        }

        // Initially try to apply the low-res texture or placeholder synchronously
        // so the object isn't blank until the manager runs.
        if (!string.IsNullOrEmpty(lowResTexturePath))
        {
            Texture2D initialLowRes = Resources.Load<Texture2D>(lowResTexturePath);
            if (initialLowRes != null)
            {
                ApplyTexture(initialLowRes, TextureQuality.Low);
            }
            else
            {
                Debug.LogWarning($"StreamableTextureComponent on {gameObject.name}: Low-res texture not found at path '{lowResTexturePath}'. Using placeholder if available.", this);
                ApplyTexture(placeholderTexture, TextureQuality.Low);
            }
        }
        else
        {
            ApplyTexture(placeholderTexture, TextureQuality.Low);
        }
    }

    void OnEnable()
    {
        // Register this component with the TextureStreamingManager when it becomes active.
        TextureStreamingManager.Instance?.RegisterStreamableTexture(this);
    }

    void OnDisable()
    {
        // Unregister from the manager when disabled to prevent errors and optimize processing.
        TextureStreamingManager.Instance?.UnregisterStreamableTexture(this);
        // Stop any ongoing loading coroutine to prevent race conditions or unnecessary loads.
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
        }
    }

    /// <summary>
    /// Requests a specific texture quality for this component.
    /// This method is typically called by the TextureStreamingManager.
    /// </summary>
    /// <param name="targetQuality">The desired texture quality to apply.</param>
    public void RequestTextureQuality(TextureQuality targetQuality)
    {
        // If the target quality is already applied, there's nothing to do.
        if (_currentAppliedQuality == targetQuality)
        {
            return;
        }

        // If a texture is currently being loaded, stop that operation
        // as a new quality has been requested.
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
        }

        // Start a new coroutine to load and apply the requested texture asynchronously.
        _loadingCoroutine = StartCoroutine(LoadAndApplyTexture(targetQuality));
    }

    /// <summary>
    /// Asynchronously loads the target texture using the TextureStreamingManager's caching system
    /// and then applies it to the renderer's material.
    /// </summary>
    /// <param name="targetQuality">The quality level of the texture to load.</param>
    private IEnumerator LoadAndApplyTexture(TextureQuality targetQuality)
    {
        string texturePath = GetTexturePathForQuality(targetQuality);

        if (string.IsNullOrEmpty(texturePath))
        {
            Debug.LogWarning($"StreamableTextureComponent on {gameObject.name}: No texture path defined for {targetQuality} quality. Maintaining current quality.", this);
            _loadingCoroutine = null;
            yield break;
        }

        // Delegate the actual texture loading and caching to the TextureStreamingManager.
        // This ensures textures are loaded efficiently and shared if multiple components need them.
        yield return TextureStreamingManager.Instance.GetOrLoadTextureAsync(texturePath,
            (loadedTexture) =>
            {
                if (loadedTexture != null)
                {
                    ApplyTexture(loadedTexture, targetQuality);
                }
                else
                {
                    Debug.LogError($"StreamableTextureComponent on {gameObject.name}: Failed to load texture at path '{texturePath}' for {targetQuality} quality. Reverting to placeholder.", this);
                    ApplyTexture(placeholderTexture, _currentAppliedQuality); // Fallback
                }
                _loadingCoroutine = null; // Mark loading as complete.
            });
    }

    /// <summary>
    /// Applies the given texture to the Renderer's main material and updates the component's state.
    /// </summary>
    /// <param name="texture">The Texture2D asset to apply.</param>
    /// <param name="quality">The quality level of the texture being applied.</param>
    private void ApplyTexture(Texture2D texture, TextureQuality quality)
    {
        if (_renderer != null && _renderer.sharedMaterial != null)
        {
            // IMPORTANT: Using .material creates a new material instance if one doesn't exist for this renderer.
            // This is generally desired when modifying a material unique to an object.
            // If you intend to share materials across many objects, you might need a more complex system
            // (e.g., using MaterialPropertyBlock or managing shared material instances).
            _renderer.material.mainTexture = texture;
            _currentAppliedTexture = texture;
            _currentAppliedQuality = quality;
            // Debug.Log($"Applied {quality} texture '{texture?.name ?? "NULL"}' to {gameObject.name}");
        }
        else
        {
            Debug.LogError($"StreamableTextureComponent on {gameObject.name}: Renderer or its material is null, cannot apply texture.", this);
        }
    }
}
```

#### 3. `TextureStreamingManager.cs`

```csharp
// TextureStreamingManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action delegate

/// <summary>
/// The TextureStreamingManager is a singleton that orchestrates the dynamic loading
/// and swapping of texture assets based on various criteria (e.g., distance to camera).
/// This mimics a 'Texture Streaming' pattern by swapping entire texture assets
/// rather than just managing mipmaps (which Unity's built-in system handles automatically).
/// It also caches loaded textures to avoid redundant loading.
/// </summary>
public class TextureStreamingManager : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static TextureStreamingManager Instance { get; private set; }

    [Header("Streaming Settings")]
    [Tooltip("The camera used for distance calculations. Defaults to Camera.main.")]
    public Camera streamingCamera;
    [Tooltip("Distance threshold (in Unity units) beyond which only low-res textures are active.")]
    public float lowResDistanceThreshold = 50f;
    [Tooltip("Distance threshold (in Unity units) within which high-res textures are requested.")]
    public float highResDistanceThreshold = 10f;
    [Tooltip("How often (in seconds) the manager checks for texture quality updates for all registered objects.")]
    public float updateInterval = 1.0f;

    // List of all StreamableTextureComponents currently managed by this system.
    private List<StreamableTextureComponent> _streamableTextures = new List<StreamableTextureComponent>();
    // Cache to store loaded Texture2D assets, preventing multiple loads of the same texture.
    private Dictionary<string, Texture2D> _loadedTextureCache = new Dictionary<string, Texture2D>();
    private Coroutine _updateLoopCoroutine; // Coroutine for the periodic update loop.

    void Awake()
    {
        // Implement the Singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Ensure the manager persists across scene loads.
        DontDestroyOnLoad(gameObject);

        // Auto-assign Camera.main if no camera is explicitly set.
        if (streamingCamera == null)
        {
            streamingCamera = Camera.main;
            if (streamingCamera == null)
            {
                Debug.LogError("TextureStreamingManager: No streaming camera assigned and Camera.main is not found. Please assign one in the Inspector.", this);
                enabled = false; // Disable if no camera to stream against.
                return;
            }
        }

        // Basic validation for distance thresholds.
        if (highResDistanceThreshold >= lowResDistanceThreshold)
        {
            Debug.LogWarning("TextureStreamingManager: highResDistanceThreshold should be less than lowResDistanceThreshold. Adjusting automatically.", this);
            highResDistanceThreshold = lowResDistanceThreshold * 0.5f; // Automatically set a reasonable default.
        }
    }

    void OnEnable()
    {
        // Start the periodic update loop when the manager is enabled.
        _updateLoopCoroutine = StartCoroutine(StreamingUpdateLoop());
    }

    void OnDisable()
    {
        // Stop the update loop when the manager is disabled.
        if (_updateLoopCoroutine != null)
        {
            StopCoroutine(_updateLoopCoroutine);
            _updateLoopCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that periodically checks and updates texture quality for registered components.
    /// This spreads the work over frames, avoiding a single frame hitch.
    /// </summary>
    private IEnumerator StreamingUpdateLoop()
    {
        while (true)
        {
            if (streamingCamera != null)
            {
                UpdateTextureQualities();
            }
            yield return new WaitForSeconds(updateInterval); // Wait for the next check interval.
        }
    }

    /// <summary>
    /// Iterates through all registered streamable textures and requests their appropriate quality
    /// based on their distance from the streaming camera.
    /// </summary>
    private void UpdateTextureQualities()
    {
        if (_streamableTextures.Count == 0) return;

        Vector3 cameraPos = streamingCamera.transform.position;

        // Iterate through a copy of the list to safely remove/add items during iteration
        // if components are destroyed or enabled/disabled dynamically.
        List<StreamableTextureComponent> currentStreamableTextures = new List<StreamableTextureComponent>(_streamableTextures);

        foreach (var streamableTexture in currentStreamableTextures)
        {
            // Skip if the component is null (destroyed), disabled, or its GameObject is inactive.
            if (streamableTexture == null || !streamableTexture.enabled || !streamableTexture.gameObject.activeInHierarchy)
            {
                // This component is no longer valid, unregister it.
                // This handles cases where objects might be destroyed without OnDisable being called reliably.
                UnregisterStreamableTexture(streamableTexture);
                continue;
            }

            float distance = Vector3.Distance(cameraPos, streamableTexture.transform.position);
            TextureQuality targetQuality = DetermineTargetQuality(distance);

            // Request the texture quality change. The component itself handles the loading logic.
            streamableTexture.RequestTextureQuality(targetQuality);
        }

        // IMPORTANT NOTE ON UNLOADING:
        // `Resources.UnloadUnusedAssets()` can be very expensive and cause significant frame rate drops.
        // It should generally NOT be called frequently within an `Update` loop or a frequent coroutine.
        // For a full production system, a more robust memory management strategy would involve:
        // 1. Reference counting for textures in `_loadedTextureCache` to know when a texture is truly unused.
        // 2. Triggering `Resources.UnloadUnusedAssets()` strategically (e.g., during scene transitions,
        //    when memory pressure is high, or when the player is in a 'loading' state).
        // 3. Using `AssetBundles` instead of `Resources` for finer control over asset lifecycle.
        // For this example, it's commented out to avoid performance issues in a simple demo.
        // If you uncomment, be aware of its performance implications.
        // Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// Determines the target texture quality based on the given distance to the camera.
    /// </summary>
    /// <param name="distance">The distance from the object to the streaming camera.</param>
    /// <returns>The desired TextureQuality.</returns>
    private TextureQuality DetermineTargetQuality(float distance)
    {
        if (distance <= highResDistanceThreshold)
        {
            return TextureQuality.High;
        }
        else if (distance <= lowResDistanceThreshold)
        {
            // If you had a Medium quality, it would go here.
            return TextureQuality.Low;
        }
        else
        {
            return TextureQuality.Low;
        }
    }

    /// <summary>
    /// Registers a StreamableTextureComponent with the manager, allowing it to be streamed.
    /// </summary>
    /// <param name="component">The StreamableTextureComponent to register.</param>
    public void RegisterStreamableTexture(StreamableTextureComponent component)
    {
        if (!_streamableTextures.Contains(component))
        {
            _streamableTextures.Add(component);
            // Immediately request the correct quality upon registration for objects already in the scene.
            if (streamingCamera != null && component != null)
            {
                float distance = Vector3.Distance(streamingCamera.transform.position, component.transform.position);
                component.RequestTextureQuality(DetermineTargetQuality(distance));
            }
        }
    }

    /// <summary>
    /// Unregisters a StreamableTextureComponent from the manager.
    /// </summary>
    /// <param name="component">The StreamableTextureComponent to unregister.</param>
    public void UnregisterStreamableTexture(StreamableTextureComponent component)
    {
        _streamableTextures.Remove(component);
    }

    /// <summary>
    /// Gets a texture from the internal cache or loads it asynchronously from Unity's Resources system.
    /// This method is crucial for efficiently managing Texture2D assets.
    /// </summary>
    /// <param name="path">The path to the texture in Resources (e.g., "Textures/my_texture").</param>
    /// <param name="onComplete">A callback action that is invoked with the loaded Texture2D (or null if failed).</param>
    public IEnumerator GetOrLoadTextureAsync(string path, Action<Texture2D> onComplete)
    {
        if (string.IsNullOrEmpty(path))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // Check if the texture is already in our cache.
        if (_loadedTextureCache.TryGetValue(path, out Texture2D cachedTexture))
        {
            onComplete?.Invoke(cachedTexture); // Return the cached texture immediately.
            yield break;
        }

        // Texture not in cache, load it asynchronously from Resources.
        Debug.Log($"TextureStreamingManager: Loading texture asynchronously from Resources: {path}");
        ResourceRequest request = Resources.LoadAsync<Texture2D>(path);
        yield return request; // Wait for the asynchronous load to complete.

        Texture2D loadedTexture = request.asset as Texture2D;
        if (loadedTexture != null)
        {
            // Add the successfully loaded texture to the cache.
            _loadedTextureCache[path] = loadedTexture;
        }
        else
        {
            Debug.LogError($"TextureStreamingManager: Failed to load texture asynchronously from Resources at path: {path}");
        }

        onComplete?.Invoke(loadedTexture); // Invoke the callback with the result.
    }

    /// <summary>
    /// Clears all texture references from the internal cache.
    /// Note: This does NOT immediately free memory. It only allows Unity to garbage collect
    /// these textures when `Resources.UnloadUnusedAssets()` is called, provided no other
    /// active objects reference them.
    /// </summary>
    public void ClearTextureCache()
    {
        _loadedTextureCache.Clear();
        Debug.Log("TextureStreamingManager: Texture cache cleared. Remember to call Resources.UnloadUnusedAssets() strategically to free memory.");
    }

    void OnDestroy()
    {
        // Clean up the singleton instance reference.
        if (Instance == this)
        {
            Instance = null;
        }
        // It's a good practice to clear the cache on shutdown.
        _loadedTextureCache.Clear();
        // `Resources.UnloadUnusedAssets()` might be called here, but be mindful if the manager
        // is `DontDestroyOnLoad` and you're only closing the application, not transitioning scenes.
    }
}
```

---

### **How to Use This Example in Your Unity Project**

Follow these steps to get the Texture Streaming pattern working in your Unity project:

1.  **Create C# Scripts:**
    *   Create three new C# scripts in your Unity project (e.g., in a `Scripts` folder): `TextureQuality.cs`, `StreamableTextureComponent.cs`, and `TextureStreamingManager.cs`.
    *   Copy and paste the code for each script into its respective file.

2.  **Prepare Textures in a `Resources` Folder:**
    *   In your Unity Project window, right-click -> Create -> Folder. Name it `Resources`.
    *   Inside `Resources`, create another folder, e.g., `Textures`.
    *   **Find or create two versions of a texture:**
        *   A **low-resolution** version (e.g., `my_object_texture_low_res.png`, 128x128 pixels).
        *   A **high-resolution** version (e.g., `my_object_texture_high_res.png`, 1024x1024 pixels).
    *   Drag and drop these textures into your `Resources/Textures` folder.
    *   *(Optional but recommended)* Select your high-resolution texture in the Project window and in the Inspector, ensure 'Generate Mip Maps' is checked. 'Streaming Mip Maps' can also be checked if you want Unity's built-in system to work alongside this.

3.  **Create the `TextureStreamingManager` GameObject:**
    *   In your scene (Hierarchy window), right-click -> Create Empty.
    *   Rename this new GameObject to `TextureStreamingManager`.
    *   Drag the `TextureStreamingManager.cs` script onto this GameObject in the Inspector.
    *   In the Inspector for `TextureStreamingManager`:
        *   Adjust `Low Res Distance Threshold` (e.g., `50` units). Objects beyond this distance will use low-res.
        *   Adjust `High Res Distance Threshold` (e.g., `10` units). Objects within this distance will request high-res.
        *   `Update Interval` (e.g., `1.0` seconds) controls how often the manager checks distances.
        *   *(Optional)* If your main camera is not tagged "MainCamera" or you're using a specific camera, drag that camera to the `Streaming Camera` field.

4.  **Create Streamable GameObjects:**
    *   Create a 3D Object in your scene (e.g., Right-click in Hierarchy -> 3D Object -> Cube).
    *   Rename it appropriately (e.g., `StreamableCube1`).
    *   Drag the `StreamableTextureComponent.cs` script onto this Cube GameObject.
    *   In the Inspector for the `StreamableTextureComponent`:
        *   **`Low Res Texture Path`**: Type `Textures/my_object_texture_low_res` (no file extension).
        *   **`High Res Texture Path`**: Type `Textures/my_object_texture_high_res` (no file extension).
        *   *(Optional)* Drag a small placeholder texture to the `Placeholder Texture` field if you have one.
    *   Duplicate this GameObject (`Ctrl+D` or `Cmd+D`) a few times.
    *   Place these duplicated objects at various distances from your `Main Camera` (or the camera assigned to the manager).

5.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe how the textures on the objects change as you move your `Main Camera` closer to or farther away from them.
    *   Objects far away will display their low-resolution texture.
    *   As you move within the `High Res Distance Threshold`, the manager will trigger an asynchronous load of the high-resolution texture, and it will be swapped onto the object.
    *   You can monitor the Console window for messages indicating when textures are loaded or if any issues occur.

---

### **Key Considerations for Production Use**

*   **Asset Bundles vs. Resources:** This example uses `Resources.LoadAsync` for simplicity. For large-scale projects, **Asset Bundles** are the recommended way to manage assets, as they provide much finer control over loading, unloading, and dependency management. Adapting `TextureStreamingManager` to use Asset Bundles would involve using `AssetBundle.LoadAssetAsync<Texture2D>` instead of `Resources.LoadAsync`.
*   **Unloading:** The provided `TextureStreamingManager` currently caches loaded textures but doesn't implement a sophisticated unloading strategy (`Resources.UnloadUnusedAssets()` is commented out due to its performance impact). A production system would likely need:
    *   **Reference Counting:** Track how many `StreamableTextureComponent` instances are currently using a specific texture in the cache. Only unload a texture when its reference count drops to zero.
    *   **Memory Budgeting:** Trigger `Resources.UnloadUnusedAssets()` (or specific Asset Bundle unloading) when VRAM usage approaches a set limit.
    *   **Deferred Unloading:** Don't unload immediately when an object moves out of range, but perhaps after a short delay, to prevent "pop-in" if the object quickly comes back into range.
*   **Performance Optimization:**
    *   For very large numbers of streamable objects, iterating through `_streamableTextures` every `updateInterval` might become a bottleneck. Consider:
        *   **Chunked Updates:** Process only a small batch of objects per `updateInterval` instead of all of them.
        *   **Spatial Partitioning:** Use an Octree or a similar structure to only check objects within a relevant radius around the camera.
        *   **Visibility Culling:** Use `OnBecameVisible` and `OnBecameInvisible` callbacks on `StreamableTextureComponent` to inform the manager that an object might need attention, rather than relying solely on distance.
*   **Material Instancing:** The `_renderer.material.mainTexture = texture;` line creates an *instance* of the material for that specific renderer if it wasn't already an instance. If you have many objects sharing the same base material and you want to reduce draw calls, you might consider using `MaterialPropertyBlock`s to change only the texture without instancing the entire material. However, for unique texture swapping, material instancing is often acceptable.