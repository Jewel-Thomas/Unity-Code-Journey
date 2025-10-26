// Unity Design Pattern Example: UISafeAreaManager
// This script demonstrates the UISafeAreaManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **UISafeAreaManager design pattern**, a robust way to adapt your UI to different screen aspect ratios and device specific safe areas (like notches, camera cutouts, or rounded corners on mobile devices).

The pattern consists of two main parts:

1.  **`UISafeAreaManager` (Singleton):** A central manager that monitors the device's safe area and notifies other UI elements when it changes.
2.  **`SafeAreaPanelAdapter` (Adapter):** A component attached to individual UI elements (e.g., panels) that subscribes to the manager's events and adjusts its `RectTransform` to fit within the reported safe area.

---

## 1. `UISafeAreaManager.cs`

This script is the core manager. It uses the Singleton pattern to ensure only one instance exists. It continuously checks `Screen.safeArea` and broadcasts changes to interested listeners.

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// UISafeAreaManager: A Singleton manager that detects and provides the screen's safe area.
/// This manager is responsible for checking Unity's `Screen.safeArea` property
/// and notifying all subscribed UI elements whenever the safe area changes (e.g., on device orientation change).
/// It ensures that UI content avoids device-specific cutouts like notches or rounded corners.
/// </summary>
public class UISafeAreaManager : MonoBehaviour
{
    // === Singleton Implementation ===
    // A private static reference to the single instance of the manager.
    private static UISafeAreaManager _instance;

    /// <summary>
    /// Public static property to access the singleton instance.
    /// If no instance exists in the scene, it creates one.
    /// This ensures that other scripts can easily get a reference to the manager.
    /// </summary>
    public static UISafeAreaManager Instance
    {
        get
        {
            // If the instance doesn't exist, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<UISafeAreaManager>();

                // If still no instance, create a new GameObject and add the component.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(UISafeAreaManager).Name);
                    _instance = singletonObject.AddComponent<UISafeAreaManager>();
                    // Make sure the new GameObject is not destroyed when loading new scenes.
                    DontDestroyOnLoad(singletonObject);
                    Debug.Log($"UISafeAreaManager: A new instance was created on GameObject '{singletonObject.name}'.");
                }
            }
            return _instance;
        }
    }

    // === Public Properties and Events ===

    /// <summary>
    /// The current safe area rectangle in screen pixel coordinates.
    /// This property is read-only and is updated whenever `Screen.safeArea` reports a change.
    /// UI elements can query this directly or subscribe to `OnSafeAreaChanged`.
    /// </summary>
    public Rect CurrentSafeArea { get; private set; }

    /// <summary>
    /// An event fired when the safe area of the screen changes.
    /// UI elements (like `SafeAreaPanelAdapter`) should subscribe to this event
    /// to update their layout dynamically.
    /// The `Rect` parameter passed with the event is the new safe area in screen pixel coordinates.
    /// </summary>
    public event Action<Rect> OnSafeAreaChanged;

    // === Private Members ===

    // Stores the safe area from the previous frame to detect changes.
    private Rect _lastSafeArea;

    // === MonoBehaviour Lifecycle Methods ===

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to enforce the singleton pattern and initialize the safe area.
    /// </summary>
    private void Awake()
    {
        // Enforce singleton: if an instance already exists and it's not this one, destroy this duplicate.
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("UISafeAreaManager: Destroying duplicate instance. Only one manager should exist.");
            Destroy(gameObject);
            return;
        }

        // Set this instance as the active singleton.
        _instance = this;
        // Prevent this GameObject from being destroyed when loading new scenes.
        DontDestroyOnLoad(gameObject);

        // Initialize the safe area immediately upon startup.
        UpdateSafeArea();
    }

    /// <summary>
    /// Called once per frame.
    /// Continuously checks for changes in `Screen.safeArea` to react to
    /// device orientation changes or other dynamic screen adjustments.
    /// </summary>
    private void Update()
    {
        // Compare the current `Screen.safeArea` with the last known safe area.
        // If they differ, it means the safe area has changed.
        if (Screen.safeArea != _lastSafeArea)
        {
            UpdateSafeArea();
        }
    }

    // === Core Logic Methods ===

    /// <summary>
    /// Retrieves the current `Screen.safeArea`, updates internal properties,
    /// and invokes the `OnSafeAreaChanged` event if a change is detected.
    /// </summary>
    private void UpdateSafeArea()
    {
        // Get the current safe area rectangle directly from Unity's `Screen` API.
        Rect safeArea = Screen.safeArea;

        // Fallback: In some rare cases (e.g., editor startup, specific device states),
        // `Screen.safeArea` might report zero width/height. In such cases,
        // we default to the full screen to prevent UI from collapsing.
        if (safeArea.width == 0 || safeArea.height == 0)
        {
            safeArea = new Rect(0, 0, Screen.width, Screen.height);
            Debug.LogWarning("UISafeAreaManager: Screen.safeArea reported as zero/invalid. Defaulting to full screen.");
        }

        // Only update and notify if the safe area has genuinely changed from the last known state.
        if (safeArea != _lastSafeArea)
        {
            CurrentSafeArea = safeArea;   // Update the publicly accessible property.
            _lastSafeArea = safeArea;     // Store this as the new 'last known' safe area.

            Debug.Log($"UISafeAreaManager: Safe Area changed to: {CurrentSafeArea} (in screen pixel coordinates). Notifying subscribers.");

            // Invoke the event, passing the new safe area. The '?' ensures it only
            // runs if there are actual subscribers, preventing NullReferenceExceptions.
            OnSafeAreaChanged?.Invoke(CurrentSafeArea);
        }
    }
}

```

---

## 2. `SafeAreaPanelAdapter.cs`

This script is an adapter that you attach to any UI `RectTransform` that you want to constrain to the safe area. It listens for changes from the `UISafeAreaManager` and adjusts its own `RectTransform` anchors accordingly.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI components, specifically RectTransform
using System; // Required for Action delegate

/// <summary>
/// SafeAreaPanelAdapter: Adapts a UI RectTransform to respect the device's safe area.
/// This script should be attached to any UI panel, background, or element that needs
/// to be constrained within the screen's safe area (e.g., to avoid notches, cutouts, etc.).
/// It subscribes to the UISafeAreaManager's OnSafeAreaChanged event and dynamically
/// adjusts its RectTransform's anchor points to fit the reported safe area.
/// </summary>
[RequireComponent(typeof(RectTransform))] // Ensures this component is always on a GameObject with a RectTransform.
public class SafeAreaPanelAdapter : MonoBehaviour
{
    [Tooltip("If true, the panel will log its safe area adjustments to the console for debugging.")]
    public bool debugMode = false;

    // Reference to the RectTransform component of this GameObject.
    private RectTransform _rectTransform;

    // === MonoBehaviour Lifecycle Methods ===

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to get a reference to the RectTransform component.
    /// </summary>
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Used to subscribe to the UISafeAreaManager's safe area change event.
    /// Also immediately applies the current safe area to ensure correct positioning on startup/enable.
    /// </summary>
    private void OnEnable()
    {
        // Ensure the UISafeAreaManager instance exists before attempting to subscribe.
        if (UISafeAreaManager.Instance != null)
        {
            // Subscribe to the event. This means `ApplySafeArea` will be called
            // whenever the UISafeAreaManager detects a change.
            UISafeAreaManager.Instance.OnSafeAreaChanged += ApplySafeArea;

            // Immediately apply the current safe area. This is crucial for when
            // this component is enabled *after* the UISafeAreaManager has already
            // initialized or fired its first event.
            ApplySafeArea(UISafeAreaManager.Instance.CurrentSafeArea);
        }
        else
        {
            Debug.LogError("SafeAreaPanelAdapter: UISafeAreaManager.Instance is null. " +
                           "Please ensure UISafeAreaManager is present in the scene and initialized before any SafeAreaPanelAdapter is enabled.");
        }
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// Used to unsubscribe from the event to prevent memory leaks and ensure
    /// the manager doesn't try to call `ApplySafeArea` on a disabled/destroyed object.
    /// </summary>
    private void OnDisable()
    {
        // Only unsubscribe if the manager instance still exists.
        // It might be null if the manager was destroyed first (e.g., on application quit).
        if (UISafeAreaManager.Instance != null)
        {
            UISafeAreaManager.Instance.OnSafeAreaChanged -= ApplySafeArea;
        }
    }

    // === Core Logic Methods ===

    /// <summary>
    /// Applies the provided safe area rectangle (in screen pixel coordinates)
    /// to this RectTransform's anchor points.
    /// This method is typically invoked by the `UISafeAreaManager.OnSafeAreaChanged` event.
    /// </summary>
    /// <param name="safeArea">The safe area rectangle in raw screen pixel coordinates.</param>
    private void ApplySafeArea(Rect safeArea)
    {
        // If RectTransform reference is lost (shouldn't happen with RequireComponent, but as a safeguard).
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                Debug.LogError($"{gameObject.name}: RectTransform not found. Cannot apply safe area.", this);
                return;
            }
        }

        // Get the current full screen dimensions in pixels.
        // Screen.width and Screen.height provide the total resolution of the device.
        Vector2 screenResolution = new Vector2(Screen.width, Screen.height);

        // Calculate normalized anchor positions (values between 0 and 1) based on the safe area.
        // `anchorMin` represents the bottom-left corner of the safe area as a fraction of the screen.
        // `anchorMax` represents the top-right corner of the safe area as a fraction of the screen.
        Vector2 anchorMin = safeArea.position / screenResolution;
        Vector2 anchorMax = (safeArea.position + safeArea.size) / screenResolution;

        // Apply the calculated anchors to the RectTransform.
        // Setting anchors this way ensures the UI element automatically stretches and
        // positions itself to fill the safe area, regardless of the CanvasScaler's
        // scale factor (e.g., "Scale With Screen Size").
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        // Reset offset values to zero. This makes the UI element perfectly fit the
        // area defined by its new anchors. If `offsetMin` or `offsetMax` were non-zero,
        // they would add additional padding/margins relative to the safe area boundaries.
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        if (debugMode)
        {
            Debug.Log($"{gameObject.name}: Applied Safe Area. RectTransform AnchorMin: {_rectTransform.anchorMin}, AnchorMax: {_rectTransform.anchorMax}. Raw Safe Area: {safeArea}");
        }
    }
}

```

---

## How to Set Up and Use in Unity

Follow these steps to integrate the `UISafeAreaManager` into your Unity project:

### 1. Create the `UISafeAreaManager` GameObject

1.  In your Unity project, right-click in the Hierarchy window.
2.  Select `Create Empty`.
3.  Rename the new GameObject to `_UISafeAreaManager` (the underscore helps keep it at the top of your hierarchy).
4.  Drag and drop the `UISafeAreaManager.cs` script onto this `_UISafeAreaManager` GameObject.
    *   **Explanation:** This script will become your central manager. Because it uses `DontDestroyOnLoad` and the Singleton pattern, it will persist across scenes and ensure only one instance is ever active.

### 2. Create Your UI Canvas

1.  In the Hierarchy, right-click and select `UI` -> `Canvas`.
2.  Select your new Canvas.
3.  In the Inspector, make sure its `Render Mode` is set to `Screen Space - Overlay` or `Screen Space - Camera` as appropriate for your project.
4.  For `Canvas Scaler`, a common setting is `UI Scale Mode` to `Scale With Screen Size` and a `Reference Resolution` (e.g., `1920x1080`). The `SafeAreaPanelAdapter` handles different scalings by working with normalized anchors.

### 3. Create a UI Panel to Adapt

1.  Inside your Canvas GameObject, right-click and select `UI` -> `Panel`.
2.  Rename this Panel to something descriptive, e.g., `Safe_UI_Panel`.
3.  Select the `Safe_UI_Panel` in the Hierarchy.
4.  **Important:** In its `RectTransform` component, ensure the `Anchors` are set to stretch the full screen. You can do this by clicking the `Anchor Presets` button (the square with the cross in the middle) and holding `Alt` (or `Option` on Mac) while clicking the bottom-right stretch preset. This will set `Anchor Min` to `(0,0)` and `Anchor Max` to `(1,1)`, and `Left`, `Top`, `Right`, `Bottom` (offsetMin/offsetMax) to `0`.
    *   **Explanation:** While `SafeAreaPanelAdapter` will overwrite these, starting with full-screen anchors gives it a clear base to work from.

### 4. Attach the `SafeAreaPanelAdapter`

1.  Drag and drop the `SafeAreaPanelAdapter.cs` script onto your `Safe_UI_Panel` (or any UI element you want to constrain).
2.  You can optionally tick the `Debug Mode` checkbox on the `SafeAreaPanelAdapter` component to see log messages about the applied safe area in the Console.

### Testing and Verification

*   **In the Editor:**
    *   Go to `Game` view.
    *   Change the aspect ratio using the dropdown (e.g., `iPhone 12 Pro Max` or `iPhone X`). You should see your `Safe_UI_Panel` adjust to fit within the simulated safe area of that device.
    *   The Unity editor's `Screen.safeArea` simulation is quite good.
*   **On Device (Mobile recommended):**
    *   Build and run your application on a mobile device with a notch or camera cutout (e.g., iPhone X, Samsung S20, etc.).
    *   Observe how the `Safe_UI_Panel` automatically shrinks and positions itself to avoid the intrusive device features.
    *   Rotate the device. The `UISafeAreaManager` will detect the orientation change and the `Safe_UI_Panel` will readjust accordingly.

This complete example provides a robust and flexible solution for managing UI safe areas in Unity, following best practices for design patterns and Unity development.