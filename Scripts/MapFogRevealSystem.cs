// Unity Design Pattern Example: MapFogRevealSystem
// This script demonstrates the MapFogRevealSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `MapFogRevealSystem` design pattern in Unity provides a way to obscure parts of your game world (the "fog") and progressively reveal them as the player or other designated entities explore. This is commonly seen in real-time strategy games or adventure games where the map starts shrouded in mystery.

This example provides a complete, production-ready C# script for Unity, demonstrating the pattern. It uses a `Texture2D` as the fog overlay, dynamically updating its alpha channel to reveal the underlying map.

---

### **MapFogRevealSystem Design Pattern Explained**

The core components of this pattern are:

1.  **Fog Layer:** A visual element (in this case, a `RawImage` with a dynamic `Texture2D`) that sits on top of your game world, completely obscuring it initially.
2.  **Fog State Management:** An internal data structure (like a `Color[]` array in this example, mirroring the pixels of the `Texture2D`) that tracks the current opacity of each part of the fog.
3.  **Revealer Entities:** Game objects (e.g., player, scout units) that have the ability to "reveal" the fog. These entities typically have a position and a reveal radius.
4.  **Reveal Logic:** The system determines which parts of the fog are within the reveal radius of active revealers and updates their opacity in the internal fog state.
5.  **Visual Update:** The internal fog state is periodically applied back to the visual fog layer (the `Texture2D`) to reflect changes on screen.
6.  **Optional: Fog Fade-in:** A mechanism for fog to slowly return to obscurity if no revealer is present in an area for a certain period, creating dynamic fog of war.

---

### **C# Unity Implementation: `MapFogRevealSystem`**

This script manages the fog and its revealing logic.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for RawImage
using System.Collections.Generic; // Required for List

namespace MapFogRevealSystem
{
    /// <summary>
    /// The MapFogRevealSystem is a core manager that handles the dynamic fog of war.
    /// It works by managing a texture's alpha channel, making parts of it transparent
    /// based on the positions of registered FogRevealer components.
    ///
    /// Design Pattern: Singleton (for easy global access) and Manager.
    /// It acts as the 'Subject' in an Observer-like pattern for FogRevealers,
    /// although revealers actively register/unregister rather than being passively observed.
    /// </summary>
    public class MapFogRevealSystem : MonoBehaviour
    {
        // --- Singleton Instance ---
        public static MapFogRevealSystem Instance { get; private set; }

        // --- Configuration Parameters ---

        [Header("Fog Texture Settings")]
        [Tooltip("The RawImage UI component that will display the fog texture. " +
                 "It should cover the area where fog is needed (e.g., full screen, minimap).")]
        [SerializeField] private RawImage fogRawImage;

        [Tooltip("The resolution of the internal fog texture. Higher resolution means " +
                 "smoother fog reveal but higher performance cost.")]
        [SerializeField] private int fogTextureResolution = 256; // e.g., 128, 256, 512

        [Tooltip("The world size (width and height) that the fog texture covers. " +
                 "This defines the mapping from world coordinates to texture coordinates.")]
        [SerializeField] private Vector2 mapWorldSize = new Vector2(100f, 100f); // e.g., 100x100 units

        [Header("Reveal Properties")]
        [Tooltip("How much a single update step reduces the fog's opacity in a revealed area. " +
                 "Lower values make revealing smoother but slower.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float revealStrength = 0.1f;

        [Tooltip("How quickly fog will return to obscurity if no revealer is present. " +
                 "Set to 0 for permanent reveals. Higher values make fog return faster.")]
        [Range(0f, 1f)]
        [SerializeField] private float fogFadeSpeed = 0.05f;

        // --- Internal State ---
        private Texture2D _fogTexture;      // The texture actively modified and displayed.
        private Color[] _fogColors;         // An array of colors representing the pixels of _fogTexture.
                                            // Modified in CPU, then applied to _fogTexture.
        private List<FogRevealer> _revealers = new List<FogRevealer>(); // List of active revealer entities.

        private Vector2 _worldToTextureRatio; // Pre-calculated ratio for world to texture coordinate conversion.

        // --- Unity Lifecycle Methods ---

        private void Awake()
        {
            // Enforce Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeFog();
        }

        private void Update()
        {
            UpdateFogState();
        }

        private void OnDestroy()
        {
            // Clean up the dynamic texture when the system is destroyed
            if (_fogTexture != null)
            {
                Destroy(_fogTexture);
            }
        }

        // --- Public Methods for Revealer Management ---

        /// <summary>
        /// Registers a FogRevealer component with the system.
        /// This revealer's position will be used to clear the fog.
        /// </summary>
        /// <param name="revealer">The FogRevealer instance to register.</param>
        public void RegisterRevealer(FogRevealer revealer)
        {
            if (!_revealers.Contains(revealer))
            {
                _revealers.Add(revealer);
            }
        }

        /// <summary>
        /// Unregisters a FogRevealer component from the system.
        /// It will no longer contribute to clearing the fog.
        /// </summary>
        /// <param name="revealer">The FogRevealer instance to unregister.</param>
        public void UnregisterRevealer(FogRevealer revealer)
        {
            _revealers.Remove(revealer);
        }

        // --- Private Internal Methods ---

        /// <summary>
        /// Initializes the fog texture and internal color array.
        /// Sets the initial state to fully opaque (black, or any desired fog color).
        /// </summary>
        private void InitializeFog()
        {
            // Create a new Texture2D that will serve as our dynamic fog layer
            _fogTexture = new Texture2D(fogTextureResolution, fogTextureResolution, TextureFormat.RGBA32, false);
            _fogTexture.filterMode = FilterMode.Bilinear; // Smooth texture scaling
            _fogTexture.wrapMode = TextureWrapMode.Clamp; // Prevent edge artifacts

            // Initialize the internal color array
            _fogColors = new Color[fogTextureResolution * fogTextureResolution];

            // Fill the texture with opaque black (or desired fog color)
            for (int i = 0; i < _fogColors.Length; i++)
            {
                _fogColors[i] = Color.black; // Starts as fully opaque black
            }

            // Apply the initial colors to the texture
            _fogTexture.SetPixels(_fogColors);
            _fogTexture.Apply();

            // Assign the created texture to the RawImage UI component
            if (fogRawImage != null)
            {
                fogRawImage.texture = _fogTexture;
            }
            else
            {
                Debug.LogError("MapFogRevealSystem: RawImage reference is missing! " +
                               "Please assign a RawImage component in the Inspector.", this);
            }

            // Calculate the ratio for converting world coordinates to texture coordinates
            _worldToTextureRatio = new Vector2(fogTextureResolution / mapWorldSize.x,
                                               fogTextureResolution / mapWorldSize.y);
        }

        /// <summary>
        /// Iterates through all pixels, fading back fog or clearing it based on revealers.
        /// This is the core logic that updates the internal _fogColors array.
        /// </summary>
        private void UpdateFogState()
        {
            // Loop through each pixel in the fog texture
            for (int y = 0; y < fogTextureResolution; y++)
            {
                for (int x = 0; x < fogTextureResolution; x++)
                {
                    int pixelIndex = y * fogTextureResolution + x;
                    Color currentColor = _fogColors[pixelIndex];

                    // --- Step 1: Fade fog back in if fogFadeSpeed > 0 ---
                    if (fogFadeSpeed > 0f)
                    {
                        // Increase alpha (make it more opaque, bring fog back)
                        currentColor.a = Mathf.Min(currentColor.a + fogFadeSpeed * Time.deltaTime, 1f);
                    }

                    // --- Step 2: Check for revealers ---
                    Vector3 pixelWorldPos = TextureToWorldCoord(x, y);
                    bool isRevealedByAny = false;

                    foreach (var revealer in _revealers)
                    {
                        if (revealer == null) continue; // Handle potential null revealers if destroyed

                        // Calculate distance from revealer to current pixel's world position
                        float dist = Vector3.Distance(revealer.transform.position, pixelWorldPos);

                        // If the pixel is within the revealer's radius, reveal it
                        if (dist < revealer.RevealRadius)
                        {
                            // Reduce alpha (make it more transparent, clear fog)
                            // Use revealStrength and also distance-based falloff for smoother edges
                            float revealAmount = revealStrength * (1f - (dist / revealer.RevealRadius));
                            currentColor.a = Mathf.Max(currentColor.a - revealAmount * Time.deltaTime, 0f);
                            isRevealedByAny = true;
                            // If it's fully revealed (alpha 0) we can break early for this pixel
                            if (currentColor.a <= 0.01f) break;
                        }
                    }

                    // Update the pixel color in our array
                    _fogColors[pixelIndex] = currentColor;
                }
            }

            // --- Step 3: Apply changes to the GPU texture ---
            // This is the most performance-intensive step. For very high resolutions
            // or frequent updates, consider optimizing by only updating changed regions
            // or using compute shaders. For typical use cases, this is acceptable.
            _fogTexture.SetPixels(_fogColors);
            _fogTexture.Apply();
        }

        /// <summary>
        /// Converts a world position to texture pixel coordinates.
        /// Assumes world (0,0) maps to the center of the mapWorldSize,
        /// and texture (0,0) is bottom-left.
        /// </summary>
        /// <param name="worldPos">The world coordinate to convert.</param>
        /// <returns>A Vector2 representing pixel coordinates (x,y).</returns>
        private Vector2 WorldToTextureCoord(Vector3 worldPos)
        {
            // Adjust worldPos relative to the center of the mapWorldSize
            // If mapWorldSize is (100,100), then world ranges from -50 to 50.
            // We want to map this to 0 to mapWorldSize.
            float relativeX = worldPos.x + mapWorldSize.x / 2f;
            float relativeY = worldPos.z + mapWorldSize.y / 2f; // Assuming Y in world is Z in 2D top-down

            // Convert to texture coordinates
            int texX = Mathf.FloorToInt(relativeX * _worldToTextureRatio.x);
            int texY = Mathf.FloorToInt(relativeY * _worldToTextureRatio.y);

            // Clamp to ensure it's within texture bounds
            texX = Mathf.Clamp(texX, 0, fogTextureResolution - 1);
            texY = Mathf.Clamp(texY, 0, fogTextureResolution - 1);

            return new Vector2(texX, texY);
        }

        /// <summary>
        /// Converts texture pixel coordinates back to a world position.
        /// Useful for checking distances.
        /// </summary>
        /// <param name="texX">Texture X coordinate.</param>
        /// <param name="texY">Texture Y coordinate.</param>
        /// <returns>A Vector3 representing the world position of the pixel's center.</returns>
        private Vector3 TextureToWorldCoord(int texX, int texY)
        {
            // Convert texture coordinates (0 to resolution) back to relative world coordinates (0 to mapWorldSize)
            float relativeX = (texX + 0.5f) / _worldToTextureRatio.x; // +0.5 to get center of pixel
            float relativeY = (texY + 0.5f) / _worldToTextureRatio.y; // +0.5 to get center of pixel

            // Adjust back from relative (0 to mapWorldSize) to actual world coordinates (e.g., -mapWorldSize/2 to mapWorldSize/2)
            float worldX = relativeX - mapWorldSize.x / 2f;
            float worldZ = relativeY - mapWorldSize.y / 2f; // Assuming Y in world is Z in 2D top-down

            return new Vector3(worldX, 0, worldZ); // Assuming flat plane at Y=0
        }
    }
}
```

---

### **C# Unity Implementation: `FogRevealer`**

This script marks a `GameObject` as capable of revealing fog.

```csharp
using UnityEngine;

namespace MapFogRevealSystem
{
    /// <summary>
    /// The FogRevealer component marks a GameObject as an entity that can reveal the fog.
    /// It automatically registers and unregisters itself with the MapFogRevealSystem.
    ///
    /// Design Pattern: Component (for attaching to any GameObject).
    /// It acts as the 'Observer' or rather, the 'Active Entity' that triggers updates
    /// in the MapFogRevealSystem.
    /// </summary>
    public class FogRevealer : MonoBehaviour
    {
        [Tooltip("The radius around this object within which fog will be cleared.")]
        [SerializeField] private float revealRadius = 10f;

        /// <summary>
        /// Public accessor for the reveal radius.
        /// </summary>
        public float RevealRadius => revealRadius;

        // --- Unity Lifecycle Methods ---

        private void OnEnable()
        {
            // Register this revealer with the MapFogRevealSystem when it becomes active
            if (MapFogRevealSystem.Instance != null)
            {
                MapFogRevealSystem.Instance.RegisterRevealer(this);
            }
            else
            {
                Debug.LogWarning("FogRevealer: MapFogRevealSystem.Instance is null. " +
                                 "Ensure MapFogRevealSystem is initialized before any FogRevealer.", this);
            }
        }

        private void OnDisable()
        {
            // Unregister this revealer from the MapFogRevealSystem when it becomes inactive
            // This prevents errors if the revealer is destroyed before the system
            if (MapFogRevealSystem.Instance != null)
            {
                MapFogRevealSystem.Instance.UnregisterRevealer(this);
            }
        }

        // --- Optional: Gizmos for Visualization ---
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
            Gizmos.DrawSphere(transform.position, revealRadius);
        }
    }
}
```

---

### **How to Use in Unity (Example Usage)**

Follow these steps to set up the Map Fog Reveal System in your Unity project:

1.  **Create Folders:**
    *   Create a folder named `Scripts` in your `Assets` directory.
    *   Inside `Scripts`, create another folder `MapFogRevealSystem`.
    *   Place both `MapFogRevealSystem.cs` and `FogRevealer.cs` into the `Assets/Scripts/MapFogRevealSystem` folder.

2.  **Set up the UI Canvas for the Fog:**
    *   In your Unity scene, go to `GameObject > UI > Canvas`. This will create a `Canvas` and an `EventSystem`.
    *   Select the `Canvas` in the Hierarchy. In the Inspector, set its `Render Mode` to `Screen Space - Overlay` (or `Screen Space - Camera` if you have a specific camera setup).
    *   Right-click on the `Canvas` in the Hierarchy, then go to `UI > Raw Image`. Name it `FogOverlay`.
    *   Select the `FogOverlay` Raw Image. In its Rect Transform, set `Anchor Presets` to the stretch option (Ctrl + Alt + Shift + stretch icon) to make it cover the entire screen. Set all `Left`, `Top`, `Right`, `Bottom` values to `0`.
    *   Ensure its `Color` is set to `(255, 255, 255, 255)` (white, fully opaque) initially. The script will dynamically manage its texture.

3.  **Create the Map Fog Reveal Manager:**
    *   Create an empty GameObject in your scene: `GameObject > Create Empty`. Name it `MapFogRevealManager`.
    *   Select `MapFogRevealManager`. In the Inspector, click `Add Component` and search for `MapFogRevealSystem`. Add the script.

4.  **Configure the `MapFogRevealSystem` Component:**
    *   With `MapFogRevealManager` selected, drag the `FogOverlay` `Raw Image` from your Hierarchy into the `Fog Raw Image` slot in the `MapFogRevealSystem` component.
    *   Set `Fog Texture Resolution`: A good starting point is `256`. Adjust higher for smoother fog but more performance impact.
    *   Set `Map World Size`: This should match the dimensions of your game world. For example, if your playable area is 100x100 Unity units (e.g., from X=-50 to X=50, and Z=-50 to Z=50 for a top-down game), set this to `(100, 100)`.
    *   Adjust `Reveal Strength` and `Fog Fade Speed` to your liking. Set `Fog Fade Speed` to `0` for permanently revealed fog.

5.  **Add a `FogRevealer` to your Player/Entities:**
    *   Select your player character or any moving game object you want to reveal the fog (e.g., a "Player" GameObject).
    *   Click `Add Component` and search for `FogRevealer`. Add the script.
    *   Set `Reveal Radius`: This determines how far around the object the fog will be cleared. For a top-down view, this relates to the world's XZ plane distance.

6.  **Test the System:**
    *   Run your game. The `FogOverlay` `Raw Image` should initially be fully opaque (black).
    *   As your player (or any object with a `FogRevealer` component) moves, you should see the fog clearing in a circle around it.
    *   If `Fog Fade Speed` is greater than 0, the fog will slowly return in areas your revealer has left.

---

### **Practical Considerations & Best Practices:**

*   **Performance:**
    *   The current implementation updates the entire `_fogTexture` every frame using `SetPixels` and `Apply()`. For very large `fogTextureResolution` (e.g., 1024x1024 or higher) and many `FogRevealer` objects, this can be CPU-intensive.
    *   **Optimization 1 (Update Region):** Only update the regions of the texture that have actually changed. This requires tracking dirty rectangles.
    *   **Optimization 2 (Update Frequency):** Update the fog texture less frequently (e.g., in a coroutine every 0.1 seconds instead of `Update()`).
    *   **Optimization 3 (Shaders):** For advanced, high-performance fog, consider using a Compute Shader to handle the pixel calculations on the GPU. This is significantly more complex to set up.
*   **Coordinate System:** The `WorldToTextureCoord` and `TextureToWorldCoord` methods assume a 2D top-down game where:
    *   World `X` maps to texture `X`.
    *   World `Z` maps to texture `Y`.
    *   The `Y` (up/down) coordinate in the world is ignored for fog calculations, assuming your map is flat.
    *   The world origin `(0,0,0)` is assumed to be the center of your `mapWorldSize`. Adjust these methods if your game's coordinate system differs (e.g., side-scroller using Y-axis).
*   **Visual Style:** You can change the fog's initial color from black to any other color by modifying `_fogColors[i] = Color.black;` in `InitializeFog()`. You could also use a different texture instead of a solid color for the opaque fog.
*   **Multiple Fog Layers:** For more complex effects (e.g., persistent fog of war vs. temporary line-of-sight), you might implement multiple `MapFogRevealSystem` instances or extend this one to manage different alpha channels or textures.
*   **Memory Usage:** A `Texture2D` of 256x256 pixels (RGBA32) uses `256 * 256 * 4 bytes = 262,144 bytes` (approx 0.25 MB). This is generally negligible. A 1024x1024 texture would be 4MB.

This complete example provides a robust foundation for implementing a dynamic map fog reveal system in your Unity projects, emphasizing clarity, practicality, and extensibility.