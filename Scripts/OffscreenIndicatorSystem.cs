// Unity Design Pattern Example: OffscreenIndicatorSystem
// This script demonstrates the OffscreenIndicatorSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **Offscreen Indicator System** design pattern. This pattern is commonly used in games to guide players towards important objectives, enemies, or points of interest that are currently outside the camera's view.

The system consists of three main parts:
1.  **`OffscreenIndicatorManager`**: A singleton that manages all off-screen indicators, updating their positions and visibility.
2.  **`OffscreenTarget`**: A component added to any game object that needs an off-screen indicator. It registers and unregisters itself with the manager.
3.  **`OffscreenIndicatorUI`**: A script on the indicator UI prefab itself, responsible for setting its visual properties (icon, color, rotation).

---

## **OffscreenIndicatorSystem.cs**

To use this system, copy the following code into a C# script file named `OffscreenIndicatorSystem.cs` in your Unity project.

**Example Usage & Setup Instructions:**

1.  **Create a UI Canvas**:
    *   Right-click in the Hierarchy -> UI -> Canvas.
    *   Set its `Render Mode` to `Screen Space - Overlay`.
    *   Set `UI Scale Mode` to `Scale With Screen Size` (recommended for responsive UI).

2.  **Create the Indicator Prefab**:
    *   Right-click on your Canvas in the Hierarchy -> UI -> Image. Name it `OffscreenIndicator`.
    *   **Appearance**: Give this Image a suitable sprite (e.g., an arrow pointing upwards) or set its color.
    *   **Structure for Arrow + Icon (Optional but Recommended)**:
        *   If you want both an arrow and a custom icon:
            *   Make the `OffscreenIndicator` Image the **arrow** graphic. Set its sprite to an arrow pointing UP.
            *   Right-click `OffscreenIndicator` -> UI -> Image. Name it `Icon`.
            *   Position `Icon` as a child, e.g., slightly in front of or behind the arrow, or centered on it. This `Icon` will display target-specific sprites.
    *   **Add `OffscreenIndicatorUI` script**: Select the `OffscreenIndicator` GameObject (the parent, not the child `Icon` image). Add Component -> Search for `OffscreenIndicatorUI`.
    *   **Link References**: Drag the `OffscreenIndicator` Image to the `Arrow Image` slot on the `OffscreenIndicatorUI` script. If you created an `Icon` child Image, drag that to the `Icon Image` slot.
    *   **Create Prefab**: Drag the `OffscreenIndicator` GameObject from the Hierarchy into your Project window (e.g., in a `Prefabs` folder) to create a prefab.
    *   Delete the `OffscreenIndicator` GameObject from the Hierarchy (the instance, not the prefab).

3.  **Create the Manager GameObject**:
    *   Create an Empty GameObject in your scene. Name it `OffscreenIndicatorManager`.
    *   Add Component -> Search for `OffscreenIndicatorManager`.
    *   **Link References**:
        *   Drag your Canvas (from step 1) to the `Canvas Rect Transform` slot.
        *   Drag your `OffscreenIndicator` prefab (from step 2) to the `Indicator Prefab` slot.
        *   Optionally, assign your `Main Camera` to the `Target Camera` slot (it defaults to `Camera.main` if left empty).
        *   Adjust `Screen Edge Padding` as desired.

4.  **Add `OffscreenTarget` to Game Objects**:
    *   Select any GameObject in your scene that you want to have an off-screen indicator (e.g., an enemy, an objective, a collectible).
    *   Add Component -> Search for `OffscreenTarget`.
    *   **Customize**:
        *   You can assign a custom `Indicator Icon` (Sprite) for this specific target.
        *   Set an `Indicator Color` for the arrow and icon.

5.  **Run the Scene**:
    *   When you run the scene, if any `OffscreenTarget` objects are outside the camera's view, an indicator will appear on the edge of the screen, pointing towards them. When they come into view, the indicator will disappear.

---

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
///     The Offscreen Indicator System allows you to display UI indicators on the screen edge,
///     pointing towards game objects that are currently off-screen.
///
///     It follows a common design pattern:
///     1.  OffscreenIndicatorManager (Singleton): Manages all indicators, handles their creation,
///         positioning, rotation, and visibility based on target positions.
///     2.  OffscreenTarget (Component): A script attached to any 3D GameObject that needs
///         an off-screen indicator. It registers itself with the manager.
///     3.  OffscreenIndicatorUI (UI Component): A script on the UI prefab that represents
///         a single indicator. It handles the visual updates (sprite, color, rotation).
/// </summary>

// --- 1. OffscreenIndicatorManager ---
// Handles the creation, positioning, and management of all off-screen indicators.
// This is a Singleton to ensure there's only one manager responsible for the UI.
public class OffscreenIndicatorManager : MonoBehaviour
{
    // Singleton instance for easy access throughout the application.
    public static OffscreenIndicatorManager Instance { get; private set; }

    [Header("UI Setup")]
    [Tooltip("The Canvas RectTransform to parent the indicators to. Usually a Screen Space - Overlay Canvas.")]
    [SerializeField] private RectTransform _canvasRectTransform;
    [Tooltip("Prefab for the off-screen indicator UI element.")]
    [SerializeField] private OffscreenIndicatorUI _indicatorPrefab;

    [Header("Indicator Behavior")]
    [Tooltip("Padding from the screen edge for the indicators, in pixels.")]
    [SerializeField] private float _screenEdgePadding = 50f;
    [Tooltip("The camera used for determining on/off-screen status and target positions. " +
             "Defaults to Camera.main if not explicitly set.")]
    [SerializeField] private Camera _targetCamera; 

    // Dictionary to keep track of active indicators mapped to their 3D game object targets.
    private Dictionary<OffscreenTarget, OffscreenIndicatorUI> _activeIndicators = new Dictionary<OffscreenTarget, OffscreenIndicatorUI>();

    private void Awake()
    {
        // Enforce Singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances.
        }
        else
        {
            Instance = this; // Assign this instance as the singleton.
            // If the target camera is not explicitly set, try to find the main camera.
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }
    }

    private void Update()
    {
        // Basic error checking to ensure essential components are assigned.
        if (_canvasRectTransform == null || _indicatorPrefab == null || _targetCamera == null)
        {
            Debug.LogWarning("OffscreenIndicatorManager: Missing Canvas, Indicator Prefab, or Target Camera reference. " +
                             "Please assign them in the Inspector to enable indicators.", this);
            return;
        }

        // Create a temporary list to store targets that need to be unregistered,
        // to avoid modifying the dictionary while iterating.
        List<OffscreenTarget> targetsToRemove = new List<OffscreenTarget>();

        // Iterate through all registered targets and update their indicators.
        foreach (var entry in _activeIndicators)
        {
            OffscreenTarget target = entry.Key;
            OffscreenIndicatorUI indicator = entry.Value;

            // Handle cases where a target GameObject might have been destroyed.
            if (target == null)
            {
                targetsToRemove.Add(entry.Key); // Mark for removal.
                continue;
            }

            UpdateIndicatorPosition(target, indicator);
        }

        // Remove any marked targets and their indicators after the loop.
        foreach (var target in targetsToRemove)
        {
            UnregisterTarget(target);
        }
    }

    /// <summary>
    /// Registers a new off-screen target with the manager.
    /// An indicator UI element will be instantiated and associated with this target.
    /// </summary>
    /// <param name="target">The OffscreenTarget component attached to the 3D GameObject.</param>
    public void RegisterTarget(OffscreenTarget target)
    {
        // Prevent registering null targets or targets that are already registered.
        if (target == null || _activeIndicators.ContainsKey(target))
        {
            return;
        }

        // Instantiate the indicator prefab and parent it to the specified Canvas.
        OffscreenIndicatorUI newIndicator = Instantiate(_indicatorPrefab, _canvasRectTransform);
        // Initialize the indicator's visual properties based on the target's settings.
        newIndicator.Setup(target.IndicatorIcon, target.IndicatorColor);
        _activeIndicators.Add(target, newIndicator); // Add to our tracking dictionary.
    }

    /// <summary>
    /// Unregisters an off-screen target from the manager.
    /// Its associated indicator UI element will be destroyed.
    /// </summary>
    /// <param name="target">The OffscreenTarget component to unregister.</param>
    public void UnregisterTarget(OffscreenTarget target)
    {
        // Prevent unregistering null targets or targets not currently registered.
        if (target == null || !_activeIndicators.ContainsKey(target))
        {
            return;
        }

        // Destroy the UI indicator GameObject and remove it from the dictionary.
        Destroy(_activeIndicators[target].gameObject);
        _activeIndicators.Remove(target);
    }

    /// <summary>
    /// Calculates and sets the position and rotation of a given indicator based on its target's position.
    /// </summary>
    private void UpdateIndicatorPosition(OffscreenTarget target, OffscreenIndicatorUI indicator)
    {
        // 1. Get the target's position in screen space (pixel coordinates).
        Vector3 screenPoint = _targetCamera.WorldToScreenPoint(target.transform.position);

        // 2. Determine if the target is behind the camera.
        bool isBehindCamera = screenPoint.z < 0;

        // If behind, mirror the X and Y coordinates. This makes the indicator point
        // to the opposite side of the screen, indicating the target is "behind" you.
        if (isBehindCamera)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        // 3. Determine if the target is off-screen (outside the screen bounds).
        // It's off-screen if any part of it is beyond the 0 to Screen.width/height range.
        bool isOffscreen = screenPoint.x < 0 || screenPoint.x > Screen.width ||
                           screenPoint.y < 0 || screenPoint.y > Screen.height;

        // 4. Set the indicator's visibility.
        // It's visible only if the target is off-screen.
        indicator.SetVisibility(isOffscreen);

        if (isOffscreen)
        {
            // 5. Calculate the direction vector from the screen center to the target's screen position.
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 directionFromCenter = (Vector2)screenPoint - screenCenter;

            // 6. Calculate the angle of this direction vector in radians.
            float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x);

            // 7. Calculate the padded screen half-dimensions. These define the rectangle
            //    on which the indicator will sit, away from the true screen edge.
            float halfScreenWidth = Screen.width / 2f - _screenEdgePadding;
            float halfScreenHeight = Screen.height / 2f - _screenEdgePadding;

            // 8. Find the intersection point of the ray (from screenCenter in directionFromCenter)
            //    with the padded screen rectangle. This determines the indicator's position on the edge.
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);

            float indicatorX, indicatorY;

            // Determine if the ray is more horizontal or vertical to check which screen edge it hits first.
            if (Mathf.Abs(cosAngle) > Mathf.Abs(sinAngle)) // Ray is more horizontal than vertical
            {
                // Calculate intersection with left/right edge.
                indicatorX = Mathf.Sign(cosAngle) * halfScreenWidth;
                indicatorY = indicatorX * (sinAngle / cosAngle);

                // If the calculated Y-coordinate exceeds the vertical bounds, it means the ray
                // actually hit the top/bottom edge first. Re-calculate based on that.
                if (Mathf.Abs(indicatorY) > halfScreenHeight)
                {
                    indicatorY = Mathf.Sign(sinAngle) * halfScreenHeight;
                    indicatorX = indicatorY * (cosAngle / sinAngle);
                }
            }
            else // Ray is more vertical than horizontal
            {
                // Calculate intersection with top/bottom edge.
                indicatorY = Mathf.Sign(sinAngle) * halfScreenHeight;
                indicatorX = indicatorY * (cosAngle / sinAngle);

                // If the calculated X-coordinate exceeds the horizontal bounds, it means the ray
                // actually hit the left/right edge first. Re-calculate based on that.
                if (Mathf.Abs(indicatorX) > halfScreenWidth)
                {
                    indicatorX = Mathf.Sign(cosAngle) * halfScreenWidth;
                    indicatorY = indicatorX * (sinAngle / cosAngle);
                }
            }

            // 9. Convert these screen-center-relative coordinates to Canvas local position.
            //    This assumes the UI Canvas is set to Screen Space - Overlay with its pivot at the center (0.5, 0.5).
            Vector2 indicatorLocalPos = new Vector2(indicatorX, indicatorY);

            // 10. Calculate the rotation for the indicator.
            //     Atan2 returns an angle from the positive X-axis.
            //     We subtract 90 degrees because most arrow sprites in Unity are oriented
            //     with their "up" (Y+) pointing visually upwards, so an angle of 0 points upwards.
            //     If your arrow sprite points right (X+) by default, you would use `angle * Mathf.Rad2Deg` directly.
            float rotationZ = angle * Mathf.Rad2Deg - 90f;

            // 11. Apply position, rotation, and potentially updated icon/color to the indicator UI.
            indicator.SetPositionAndRotation(indicatorLocalPos, rotationZ);
            indicator.SetColor(target.IndicatorColor); // Update color in case target changed it dynamically
            indicator.SetIcon(target.IndicatorIcon);   // Update icon in case target changed it dynamically
        }
    }
}

// --- 2. OffscreenTarget ---
// Component to be added to any GameObject that needs an off-screen indicator.
// It automatically registers and unregisters itself with the OffscreenIndicatorManager.
public class OffscreenTarget : MonoBehaviour
{
    [Tooltip("The sprite to display as the indicator's custom icon (optional).")]
    [SerializeField] private Sprite _indicatorIcon;
    [Tooltip("The color of the indicator's arrow and/or icon.")]
    [SerializeField] private Color _indicatorColor = Color.white;

    // Public properties allow the manager to access the target's desired icon and color.
    public Sprite IndicatorIcon => _indicatorIcon;
    public Color IndicatorColor => _indicatorColor;

    private void OnEnable()
    {
        // Attempt to register this target with the manager when it becomes active in the scene.
        if (OffscreenIndicatorManager.Instance != null)
        {
            OffscreenIndicatorManager.Instance.RegisterTarget(this);
        }
        else
        {
            Debug.LogWarning($"OffscreenTarget on {gameObject.name}: OffscreenIndicatorManager not found in scene. " +
                             "Ensure the manager GameObject exists and is active. Indicator will not be displayed.", this);
        }
    }

    private void OnDisable()
    {
        // Attempt to unregister this target from the manager when it becomes inactive or is destroyed.
        if (OffscreenIndicatorManager.Instance != null)
        {
            OffscreenIndicatorManager.Instance.UnregisterTarget(this);
        }
    }

    /// <summary>
    /// Dynamically changes the indicator's icon and color for this target.
    /// The manager will pick up these changes in its next Update cycle.
    /// </summary>
    /// <param name="icon">The new sprite for the icon (pass null to hide icon).</param>
    /// <param name="color">The new color for the indicator.</param>
    public void SetCustomIndicator(Sprite icon, Color color)
    {
        _indicatorIcon = icon;
        _indicatorColor = color;
        // The manager's Update() method will automatically refresh the indicator's appearance.
    }
}

// --- 3. OffscreenIndicatorUI ---
// Represents the UI element for a single off-screen indicator.
// This script is meant to be part of the indicator prefab.
public class OffscreenIndicatorUI : MonoBehaviour
{
    // References to the UI components within the prefab.
    [Tooltip("The Image component for the main arrow graphic.")]
    [SerializeField] private Image _arrowImage;
    [Tooltip("The Image component for a custom icon (optional, can be hidden if not used).")]
    [SerializeField] private Image _iconImage;

    private RectTransform _rectTransform; // Reference to this GameObject's RectTransform.

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("OffscreenIndicatorUI: RectTransform not found on this GameObject. " +
                           "This script must be on a UI element.", this);
            enabled = false; // Disable the script if an essential component is missing.
        }
    }

    /// <summary>
    /// Initializes the indicator with a specific icon and color.
    /// Called once when the indicator is created by the manager.
    /// </summary>
    /// <param name="icon">The sprite for the indicator's icon.</param>
    /// <param name="color">The color for the indicator's arrow/icon.</param>
    public void Setup(Sprite icon, Color color)
    {
        SetIcon(icon);
        SetColor(color);
    }

    /// <summary>
    /// Sets the visibility of the entire indicator UI element.
    /// </summary>
    /// <param name="isVisible">True to show, false to hide.</param>
    public void SetVisibility(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// Sets the position and rotation of the indicator on the UI Canvas.
    /// Position is relative to the Canvas's RectTransform (usually center for Screen Space - Overlay).
    /// </summary>
    /// <param name="position">The local position (anchoredPosition) on the Canvas.</param>
    /// <param name="rotationZ">The Z-axis rotation in degrees for the arrow.</param>
    public void SetPositionAndRotation(Vector2 position, float rotationZ)
    {
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = position;
        }

        // Apply rotation specifically to the arrow image.
        // This allows the optional icon to remain upright while the arrow rotates.
        if (_arrowImage != null)
        {
            _arrowImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotationZ);
        }
    }

    /// <summary>
    /// Sets the color of the arrow image and the icon image (if available).
    /// </summary>
    /// <param name="color">The color to apply.</param>
    public void SetColor(Color color)
    {
        if (_arrowImage != null)
        {
            _arrowImage.color = color;
        }
        if (_iconImage != null)
        {
            _iconImage.color = color; // Optionally apply color to icon, or separate this for distinct coloring.
        }
    }

    /// <summary>
    /// Sets the sprite for the custom icon. Hides the icon if a null sprite is provided.
    /// </summary>
    /// <param name="icon">The sprite to use for the icon.</param>
    public void SetIcon(Sprite icon)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            // Only show the icon GameObject if a sprite is assigned to it.
            _iconImage.gameObject.SetActive(icon != null);
        }
    }
}
```