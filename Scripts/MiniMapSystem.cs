// Unity Design Pattern Example: MiniMapSystem
// This script demonstrates the MiniMapSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates a practical 'MiniMapSystem' design pattern, encapsulating the logic for rendering a top-down view of the game world and displaying dynamic points of interest (POIs) on a UI minimap.

The core idea is to have a central `MiniMapManager` responsible for the minimap camera, rendering, and managing minimap indicators. Game objects that wish to appear on the minimap implement an `IMiniMapObject` interface and register themselves with the manager using a `MinimapObjectRegisterer` component.

---

### **1. `IMiniMapObject.cs`**
This interface defines what any game object needs to provide to be represented on the minimap.

```csharp
// IMiniMapObject.cs
using UnityEngine;

/// <summary>
/// Interface for any game object that wishes to be displayed on the minimap.
/// </summary>
/// <remarks>
/// This allows the MiniMapManager to abstractly interact with different types of world objects
/// without needing to know their specific implementations, adhering to the Liskov Substitution Principle.
/// </remarks>
public interface IMiniMapObject
{
    /// <summary>
    /// Gets the world transform of the object.
    /// Used by the MiniMapManager to determine the object's position and rotation on the minimap.
    /// </summary>
    Transform WorldTransform { get; }

    /// <summary>
    /// Gets the UI Prefab that should be instantiated to represent this object on the minimap.
    /// This prefab should typically contain a RectTransform and an Image component.
    /// </summary>
    GameObject MinimapIndicatorPrefab { get; }

    /// <summary>
    /// Gets the color to apply to the minimap indicator (e.g., for player, enemies, objectives).
    /// </summary>
    Color IndicatorColor { get; }

    /// <summary>
    /// Gets a value indicating whether this object's minimap indicator should currently be active/visible.
    /// </summary>
    bool IsActiveOnMinimap { get; }

    /// <summary>
    /// Gets a value indicating whether the minimap indicator should rotate to match the world object's rotation.
    /// </summary>
    bool RotateWithObject { get; }
}

```

---

### **2. `MinimapObjectRegisterer.cs`**
This component is attached to world game objects (e.g., Player, Enemies, Collectibles) to make them appear on the minimap. It implements `IMiniMapObject` and handles automatic registration/unregistration with the `MiniMapManager`.

```csharp
// MinimapObjectRegisterer.cs
using UnityEngine;

/// <summary>
/// Component to attach to world game objects that should appear on the minimap.
/// It implements the IMiniMapObject interface and handles registration/unregistration
/// with the MiniMapManager automatically.
/// </summary>
/// <remarks>
/// This acts as an adapter, allowing any MonoBehaviour to easily become an IMiniMapObject.
/// </remarks>
public class MinimapObjectRegisterer : MonoBehaviour, IMiniMapObject
{
    [Tooltip("The UI Prefab to use as the indicator on the minimap. This should be a UI element (e.g., an Image).")]
    [SerializeField]
    private GameObject _minimapIndicatorUIPrefab;

    [Tooltip("The color tint for this object's minimap indicator.")]
    [SerializeField]
    private Color _indicatorColor = Color.white;

    [Tooltip("If true, the minimap indicator will rotate to match the Y-axis rotation of this world object.")]
    [SerializeField]
    private bool _rotateWithObject = true;

    // --- IMiniMapObject Implementation ---
    public Transform WorldTransform => transform;
    public GameObject MinimapIndicatorPrefab => _minimapIndicatorUIPrefab;
    public Color IndicatorColor => _indicatorColor;
    public bool IsActiveOnMinimap => gameObject.activeInHierarchy;
    public bool RotateWithObject => _rotateWithObject;
    // ------------------------------------

    /// <summary>
    /// Called when the component is enabled. Registers this object with the MiniMapManager.
    /// </summary>
    private void OnEnable()
    {
        // Ensure the manager exists before trying to register.
        // Using `?.` (null conditional operator) for safety.
        MiniMapManager.Instance?.RegisterMinimapObject(this);
    }

    /// <summary>
    /// Called when the component is disabled or destroyed. Unregisters this object from the MiniMapManager.
    /// </summary>
    private void OnDisable()
    {
        // Ensure the manager exists before trying to unregister.
        MiniMapManager.Instance?.UnregisterMinimapObject(this);
    }
}

```

---

### **3. `MiniMapManager.cs`**
This is the core of the MiniMapSystem. It's a Singleton that manages the minimap camera, the `RenderTexture` output, the UI `RawImage` display, and the dynamic positioning/rotation/coloring of all minimap indicators.

```csharp
// MiniMapManager.cs
using UnityEngine;
using UnityEngine.UI; // Required for RawImage, RectTransform, Image components
using System.Collections.Generic; // Required for Dictionary

/// <summary>
/// The central manager for the Minimap System.
/// This class orchestrates the minimap camera, render texture, UI display,
/// and the dynamic positioning of minimap indicators for registered objects.
/// </summary>
/// <remarks>
/// This manager follows a Singleton pattern for easy access throughout the game.
/// It also acts as a "Facade" for the complex minimap logic, providing a simple
/// registration API for other game objects.
/// </remarks>
public class MiniMapManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static MiniMapManager Instance { get; private set; }

    /// <summary>
    /// Ensures only one instance of MiniMapManager exists.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple MiniMapManager instances found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make this object persist across scene loads
        // DontDestroyOnLoad(gameObject);
    }
    // -------------------------

    [Header("Minimap Camera Settings")]
    [Tooltip("The dedicated camera for rendering the minimap view.")]
    [SerializeField]
    private Camera _minimapCamera;

    [Tooltip("The height at which the minimap camera should be positioned above the player.")]
    [SerializeField]
    private float _minimapCameraHeight = 50f;

    [Tooltip("The orthographic size of the minimap camera. Controls the zoom level (smaller value = more zoomed in).")]
    [SerializeField]
    private float _minimapOrthographicSize = 25f;

    [Tooltip("The Transform of the player or main target the minimap camera should follow.")]
    [SerializeField]
    private Transform _playerWorldTransform;

    [Header("Minimap UI Settings")]
    [Tooltip("The RenderTexture that the minimap camera will render to.")]
    [SerializeField]
    private RenderTexture _minimapRenderTexture;

    [Tooltip("The UI RawImage component that will display the minimap RenderTexture.")]
    [SerializeField]
    private RawImage _minimapUIRawImage;

    [Tooltip("The RectTransform under which all minimap indicators (UI Image elements) will be spawned.")]
    [SerializeField]
    private RectTransform _minimapIndicatorsParent;

    [Header("Default Indicator Prefabs (Optional)")]
    [Tooltip("Default UI prefab for player indicator if not specified by MinimapObjectRegisterer.")]
    [SerializeField]
    private GameObject _defaultPlayerIndicatorPrefab;

    [Tooltip("Default UI prefab for general POI indicators if not specified by MinimapObjectRegisterer.")]
    [SerializeField]
    private GameObject _defaultPoiIndicatorPrefab;

    // Dictionary to keep track of registered IMiniMapObject instances and their corresponding UI RectTransform indicators.
    // This allows efficient lookup and management.
    private readonly Dictionary<IMiniMapObject, RectTransform> _registeredObjects = new Dictionary<IMiniMapObject, RectTransform>();

    /// <summary>
    /// Called once after Awake. Initializes the minimap camera and UI.
    /// </summary>
    private void Start()
    {
        if (_minimapCamera == null)
        {
            Debug.LogError("Minimap Camera is not assigned in MiniMapManager!", this);
            enabled = false;
            return;
        }
        if (_minimapRenderTexture == null)
        {
            Debug.LogError("Minimap RenderTexture is not assigned in MiniMapManager!", this);
            enabled = false;
            return;
        }
        if (_minimapUIRawImage == null)
        {
            Debug.LogError("Minimap UI RawImage is not assigned in MiniMapManager!", this);
            enabled = false;
            return;
        }
        if (_minimapIndicatorsParent == null)
        {
            Debug.LogError("Minimap Indicators Parent RectTransform is not assigned in MiniMapManager! Assign the RawImage's RectTransform or a dedicated parent.", this);
            enabled = false;
            return;
        }

        // Set the target texture for the minimap camera.
        _minimapCamera.targetTexture = _minimapRenderTexture;
        // Assign the render texture to the UI RawImage to display it.
        _minimapUIRawImage.texture = _minimapRenderTexture;

        // Ensure the minimap camera is orthographic and looks straight down.
        _minimapCamera.orthographic = true;
        _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look down along Y-axis
        _minimapCamera.orthographicSize = _minimapOrthographicSize;
        _minimapCamera.cullingMask = _minimapCamera.cullingMask & ~(1 << LayerMask.NameToLayer("MinimapObjects")); // Exclude a layer specifically for world-space minimap icons if you were using that method
                                                                                                                // For this example, we render world to texture, then overlay UI icons.
    }

    /// <summary>
    /// LateUpdate is called once per frame, after all Update functions have been called.
    /// This is ideal for camera logic and updates that depend on object movements.
    /// </summary>
    private void LateUpdate()
    {
        if (_minimapCamera == null || _playerWorldTransform == null)
        {
            return;
        }

        // 1. Update Minimap Camera Position
        // Center the minimap camera on the player's X and Z coordinates, maintaining its fixed height.
        Vector3 playerPos = _playerWorldTransform.position;
        _minimapCamera.transform.position = new Vector3(playerPos.x, _minimapCameraHeight, playerPos.z);
        _minimapCamera.orthographicSize = _minimapOrthographicSize; // Apply zoom level

        // 2. Update Minimap Indicators
        UpdateAllMinimapIndicators();
    }

    /// <summary>
    /// Registers an IMiniMapObject with the manager, creating its UI indicator.
    /// </summary>
    /// <param name="minimapObject">The world object to register.</param>
    public void RegisterMinimapObject(IMiniMapObject minimapObject)
    {
        if (minimapObject == null || _registeredObjects.ContainsKey(minimapObject))
        {
            return; // Object already registered or null
        }

        // Determine which prefab to use. Prioritize the object's specific prefab, then defaults.
        GameObject indicatorPrefab = minimapObject.MinimapIndicatorPrefab;
        if (indicatorPrefab == null)
        {
            // Try to assign default player indicator if player transform matches
            if (minimapObject.WorldTransform == _playerWorldTransform && _defaultPlayerIndicatorPrefab != null)
            {
                indicatorPrefab = _defaultPlayerIndicatorPrefab;
            }
            // Otherwise, use default POI indicator
            else if (_defaultPoiIndicatorPrefab != null)
            {
                indicatorPrefab = _defaultPoiIndicatorPrefab;
            }
            else
            {
                Debug.LogWarning($"MinimapObjectRegisterer on {minimapObject.WorldTransform.name} has no indicator prefab, and no default assigned in MiniMapManager.", minimapObject.WorldTransform);
                return; // Cannot create indicator without a prefab.
            }
        }
        
        // Instantiate the UI indicator prefab as a child of the minimap UI area.
        GameObject indicatorGO = Instantiate(indicatorPrefab, _minimapIndicatorsParent);
        RectTransform indicatorRect = indicatorGO.GetComponent<RectTransform>();

        if (indicatorRect == null)
        {
            Debug.LogError($"Minimap indicator prefab '{indicatorPrefab.name}' for {minimapObject.WorldTransform.name} must have a RectTransform component!", minimapObject.WorldTransform);
            Destroy(indicatorGO);
            return;
        }

        // Add to dictionary for tracking
        _registeredObjects.Add(minimapObject, indicatorRect);
        UpdateMinimapIndicator(minimapObject, indicatorRect); // Initial update
    }

    /// <summary>
    /// Unregisters an IMiniMapObject from the manager, destroying its UI indicator.
    /// </summary>
    /// <param name="minimapObject">The world object to unregister.</param>
    public void UnregisterMinimapObject(IMiniMapObject minimapObject)
    {
        if (minimapObject == null || !_registeredObjects.ContainsKey(minimapObject))
        {
            return; // Object not registered or null
        }

        // Destroy the UI indicator GameObject
        RectTransform indicatorRect = _registeredObjects[minimapObject];
        if (indicatorRect != null)
        {
            Destroy(indicatorRect.gameObject);
        }

        // Remove from tracking dictionary
        _registeredObjects.Remove(minimapObject);
    }

    /// <summary>
    /// Iterates through all registered minimap objects and updates their UI indicators.
    /// </summary>
    private void UpdateAllMinimapIndicators()
    {
        // Get the world dimensions covered by the orthographic camera.
        float mapWidthWorld = _minimapCamera.orthographicSize * _minimapCamera.aspect * 2;
        float mapHeightWorld = _minimapCamera.orthographicSize * 2;

        // Get the pixel dimensions of the UI RawImage (the minimap display area).
        // Assumes the RawImage's RectTransform is also the parent for indicators.
        Vector2 minimapUIAreaSize = _minimapUIRawImage.rectTransform.rect.size;

        foreach (var entry in _registeredObjects)
        {
            IMiniMapObject worldObject = entry.Key;
            RectTransform indicatorRect = entry.Value;

            // Handle visibility based on IsActiveOnMinimap property
            if (!worldObject.IsActiveOnMinimap)
            {
                if (indicatorRect.gameObject.activeSelf) indicatorRect.gameObject.SetActive(false);
                continue;
            }
            else
            {
                if (!indicatorRect.gameObject.activeSelf) indicatorRect.gameObject.SetActive(true);
            }

            // --- Coordinate Transformation: World Position to UI Position ---
            
            // 1. Calculate world position relative to the minimap camera's center.
            // We project the 3D world XZ plane onto the 2D UI XY plane.
            Vector3 relativeWorldPos = worldObject.WorldTransform.position - _minimapCamera.transform.position;

            // 2. Normalize position within the camera's view (from -0.5 to 0.5, where 0,0 is camera center).
            float normalizedX = relativeWorldPos.x / mapWidthWorld;
            float normalizedY = relativeWorldPos.z / mapHeightWorld; // World Z-axis maps to UI Y-axis

            // 3. Convert normalized position to UI local position within the minimap area.
            // Assumes the UI RectTransform pivot is at (0.5, 0.5) (center).
            float uiX = normalizedX * minimapUIAreaSize.x;
            float uiY = normalizedY * minimapUIAreaSize.y;

            indicatorRect.anchoredPosition = new Vector2(uiX, uiY);

            // --- Indicator Rotation ---
            if (worldObject.RotateWithObject)
            {
                // Get the Y-axis rotation of the world object.
                // UI rotations usually increase clockwise for positive Z, so we negate.
                float worldRotationY = worldObject.WorldTransform.eulerAngles.y;
                indicatorRect.localRotation = Quaternion.Euler(0, 0, -worldRotationY);
            }
            else
            {
                // Keep indicator upright if not rotating with the object.
                indicatorRect.localRotation = Quaternion.identity;
            }

            // --- Indicator Color ---
            // Apply color tint if the indicator has an Image component.
            Image indicatorImage = indicatorRect.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = worldObject.IndicatorColor;
            }
        }
    }

    /// <summary>
    /// Updates a single minimap indicator's position, rotation, and color.
    /// This is called internally after registration and periodically during LateUpdate.
    /// </summary>
    /// <param name="worldObject">The world object whose indicator needs updating.</param>
    /// <param name="indicatorRect">The RectTransform of the UI indicator.</param>
    private void UpdateMinimapIndicator(IMiniMapObject worldObject, RectTransform indicatorRect)
    {
        // This method can be optimized or removed if UpdateAllMinimapIndicators is sufficient.
        // For now, it simply calls the logic that would be performed in the full update loop.
        // In a more complex system, this might allow individual updates without iterating all.
        // For simplicity and to avoid code duplication in this example, we keep the main logic
        // in UpdateAllMinimapIndicators and use this for initial setup.
    }
}
```

---

### **How to Implement and Use in Unity:**

**1. Create Layers (Optional but Recommended):**
   *   Go to `Edit > Project Settings > Tags and Layers`.
   *   Add a new `User Layer` (e.g., Layer 8) and name it `MinimapObjects`. (While our example uses UI icons, this layer can be useful for culling if you wanted minimap camera to *only* render specific 3D minimap representations directly.)

**2. Create MiniMapManager GameObject:**
   *   Create an empty GameObject in your scene, name it `MiniMapSystem`.
   *   Attach the `MiniMapManager.cs` script to it.

**3. Setup Minimap Camera:**
   *   As a child of `MiniMapSystem`, create a new Camera GameObject, name it `Minimap Camera`.
   *   Set its `Projection` to `Orthographic`.
   *   Set its `Clear Flags` to `Solid Color` (or `Skybox` if your world has one), choose a background color.
   *   Set its `Culling Mask` to render *most* things you want on the minimap background (e.g., "Default" terrain, buildings) but potentially exclude player/enemies if you only want their icons to be UI elements.
   *   Drag this `Minimap Camera` into the `_minimapCamera` slot of the `MiniMapManager` component.

**4. Create RenderTexture:**
   *   In your Project window, right-click `Assets > Create > Render Texture`.
   *   Name it `MinimapRenderTexture` (or similar).
   *   Set its `Size` (e.g., 512x512) and `Depth Buffer` (e.g., 16-bit).
   *   Drag this `MinimapRenderTexture` into the `_minimapRenderTexture` slot of the `MiniMapManager` component and also into the `Target Texture` slot of the `Minimap Camera`.

**5. Create Minimap UI:**
   *   Create a Canvas (`GameObject > UI > Canvas`). Set its `Render Mode` to `Screen Space - Overlay` or `Screen Space - Camera` for best results.
   *   As a child of the Canvas, create a `Raw Image` (`GameObject > UI > Raw Image`). Name it `Minimap Display`.
   *   Adjust its `Rect Transform` to position and size your minimap on the screen (e.g., Anchor Presets to top-right corner).
   *   Drag this `Minimap Display` (RawImage) into the `_minimapUIRawImage` slot of the `MiniMapManager` component.
   *   Drag its `RectTransform` into the `_minimapIndicatorsParent` slot of the `MiniMapManager` component (or create an empty child GameObject under `Minimap Display` and drag its `RectTransform` there if you need a dedicated container for icons).

**6. Create Minimap Indicator Prefabs:**
   *   In your Project window, create a new UI Image prefab:
      *   Right-click `Assets > Create > UI > Image`.
      *   Configure the `Image` (e.g., set `Source Image` to a white circle, triangle, or an arrow sprite).
      *   Set its `Rect Transform` `Width` and `Height` (e.g., 16x16 or 32x32 pixels).
      *   Ensure its `Pivot` is (0.5, 0.5) (center).
      *   Drag this Image GameObject from the Hierarchy into your Project window to create a prefab (e.g., `MinimapPlayerIcon`, `MinimapEnemyIcon`).
   *   Drag your `MinimapPlayerIcon` prefab to the `_defaultPlayerIndicatorPrefab` slot of `MiniMapManager`.
   *   Drag your `MinimapEnemyIcon` prefab to the `_defaultPoiIndicatorPrefab` slot of `MiniMapManager`.

**7. Assign Player Transform:**
   *   Drag your player GameObject's `Transform` into the `_playerWorldTransform` slot of the `MiniMapManager`.

**8. Attach MinimapObjectRegisterer to Game Objects:**
   *   Select your Player GameObject. Add the `MinimapObjectRegisterer.cs` component.
      *   You can leave `Minimap Indicator UI Prefab` empty to use the default player prefab set in the `MiniMapManager`.
      *   Set `Indicator Color` (e.g., Blue for player).
      *   Enable `Rotate With Object` for the player.
   *   Select an Enemy GameObject. Add the `MinimapObjectRegisterer.cs` component.
      *   You can leave `Minimap Indicator UI Prefab` empty to use the default POI prefab.
      *   Set `Indicator Color` (e.g., Red for enemies).
      *   Disable `Rotate With Object` if you want enemies to always face up on the minimap.
   *   Repeat for any other POIs (collectibles, quest givers, etc.), adjusting prefab, color, and rotation as needed.

**Example Hierarchy (after setup):**

```
Canvas (Screen Space - Overlay)
├── Minimap Display (RawImage)
│   └── (Dynamic UI Indicators will be children here at runtime)
│
MiniMapSystem (GameObject)
├── MiniMapManager (Script)
│   ├── _minimapCamera: Minimap Camera
│   ├── _minimapRenderTexture: MinimapRenderTexture (Asset)
│   ├── _minimapUIRawImage: Minimap Display (RawImage)
│   ├── _minimapIndicatorsParent: Minimap Display (RectTransform)
│   ├── _playerWorldTransform: Player (Transform)
│   └── ... (Other settings like defaults and sizes)
└── Minimap Camera (Camera)
    ├── Target Texture: MinimapRenderTexture (Asset)
    └── ... (Other camera settings)

Player (GameObject)
├── MinimapObjectRegisterer (Script)
│   ├── _minimapIndicatorUIPrefab: (null, uses default from manager)
│   ├── _indicatorColor: Blue
│   └── _rotateWithObject: True
└── ... (Other Player components)

Enemy (GameObject)
├── MinimapObjectRegisterer (Script)
│   ├── _minimapIndicatorUIPrefab: (null, uses default from manager)
│   ├── _indicatorColor: Red
│   └── _rotateWithObject: False
└── ... (Other Enemy components)
```