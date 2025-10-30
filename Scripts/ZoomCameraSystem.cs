// Unity Design Pattern Example: ZoomCameraSystem
// This script demonstrates the ZoomCameraSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a comprehensive and practical implementation of a 'ZoomCameraSystem' design pattern. While 'ZoomCameraSystem' isn't a named GoF (Gang of Four) pattern, it leverages several common design patterns like **Singleton**, **Observer**, and aspects of **Command** to create a robust and extensible camera management system.

The script allows you to control camera zoom for both 2D (Orthographic) and 3D (Perspective) cameras, handles user input (mouse scroll wheel), provides smooth transitions, and allows other systems to react to zoom changes.

```csharp
using UnityEngine;
using System; // Required for the Action delegate
using System.Collections; // Required for Coroutines in example usage comments

/// <summary>
/// A centralized system for managing camera zoom in Unity,
/// supporting both orthographic (2D) and perspective (3D) cameras.
///
/// Design Patterns Demonstrated:
/// 1.  Singleton: Ensures a single instance of ZoomCameraSystem exists,
///     providing a global access point for camera control.
/// 2.  Observer: Uses an event (`OnZoomLevelChanged`) to notify other
///     scripts when the camera's zoom state changes, allowing for decoupled
///     reactions (e.g., UI scaling, LOD adjustments).
/// 3.  Command (Implied): Public methods like `ZoomIn()`, `ZoomOut()`,
///     and `SetZoomLevel()` act as commands that other systems can issue
///     to control the camera without knowing its internal implementation details.
/// </summary>
public class ZoomCameraSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    // This private static field holds the single instance of the ZoomCameraSystem.
    private static ZoomCameraSystem _instance;

    /// <summary>
    /// Public static property to access the single instance of the ZoomCameraSystem.
    /// If no instance exists in the scene, it creates one on a new GameObject.
    /// </summary>
    public static ZoomCameraSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Attempt to find an existing instance in the scene.
                _instance = FindObjectOfType<ZoomCameraSystem>();
                if (_instance == null)
                {
                    // If no instance is found, create a new GameObject and attach the script.
                    GameObject singletonObject = new GameObject(typeof(ZoomCameraSystem).Name);
                    _instance = singletonObject.AddComponent<ZoomCameraSystem>();
                    // Make the GameObject persistent across scene loads by default.
                    // This is common for managers that need to exist throughout the game.
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    // --- Public Events (Observer Pattern) ---
    /// <summary>
    /// Event triggered when the camera's desired zoom level changes.
    /// Other scripts can subscribe to this event to react to zoom changes (e.g., update UI, change LODs).
    /// The float parameter represents the new target orthographic size (2D) or Field of View (3D).
    /// </summary>
    public static event Action<float> OnZoomLevelChanged;

    // --- Inspector Fields ---
    [Header("Camera Configuration")]
    [Tooltip("The camera this system will control. If left unassigned, it defaults to Camera.main.")]
    public Camera targetCamera;

    [Header("Orthographic (2D) Zoom Settings")]
    [Tooltip("Minimum (most zoomed-in) orthographic size. Smaller value = closer view.")]
    [Range(0.1f, 50f)] public float minOrthographicSize = 2f;
    [Tooltip("Maximum (most zoomed-out) orthographic size. Larger value = wider view.")]
    [Range(0.1f, 50f)] public float maxOrthographicSize = 10f;
    [Tooltip("The sensitivity of orthographic zoom per unit of mouse scroll input.")]
    [Range(0.01f, 5f)] public float orthographicZoomSensitivity = 1f;
    [Tooltip("The fixed amount the orthographic size changes per call to ZoomIn()/ZoomOut().")]
    [Range(0.01f, 5f)] public float orthographicZoomStep = 0.5f;


    [Header("Perspective (3D) Zoom Settings")]
    [Tooltip("Minimum (most zoomed-in) Field of View. Smaller value = closer view.")]
    [Range(1f, 179f)] public float minFieldOfView = 30f;
    [Tooltip("Maximum (most zoomed-out) Field of View. Larger value = wider view.")]
    [Range(1f, 179f)] public float maxFieldOfView = 60f;
    [Tooltip("The sensitivity of perspective zoom per unit of mouse scroll input.")]
    [Range(0.1f, 20f)] public float fieldOfViewZoomSensitivity = 5f;
    [Tooltip("The fixed amount the Field of View changes per call to ZoomIn()/ZoomOut().")]
    [Range(0.1f, 20f)] public float fieldOfViewZoomStep = 2.5f;


    [Header("Zoom Smoothing")]
    [Tooltip("How quickly the camera smoothly transitions to the target zoom level. Higher value = snappier transition.")]
    [Range(1f, 20f)] public float zoomLerpSpeed = 5f;

    // --- Private / Internal State ---
    private float _currentDesiredZoomValue; // The target orthographicSize or fieldOfView that the camera lerps towards.
    private float _initialCameraValue;      // Stores the camera's zoom value at Awake for ResetZoom().
    private bool _isOrthographic;           // True if the targetCamera is orthographic, false if perspective.

    // --- Public Properties ---
    /// <summary>
    /// Gets the current desired zoom level (orthographic size for 2D, or field of view for 3D).
    /// This is the target value that the camera is smoothly moving towards, not its instantaneous value.
    /// </summary>
    public float CurrentZoomLevel => _currentDesiredZoomValue;

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Enforce the Singleton pattern: If another instance already exists, destroy this one.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // The DontDestroyOnLoad call is already handled in the Instance getter.
        // Uncomment the line below if you want to ensure it's always persistent regardless of initial creation method.
        // DontDestroyOnLoad(gameObject);

        // If no target camera is assigned in the Inspector, try to find Camera.main.
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("ZoomCameraSystem: No target camera assigned and Camera.main not found! Please assign a camera to the 'Target Camera' field in the Inspector.");
                enabled = false; // Disable the script if no camera is found to prevent null reference errors.
                return;
            }
        }

        // Determine if the camera is orthographic or perspective.
        _isOrthographic = targetCamera.orthographic;

        // Store the initial zoom value and set the current desired zoom, clamped within bounds.
        if (_isOrthographic)
        {
            _initialCameraValue = targetCamera.orthographicSize;
            _currentDesiredZoomValue = targetCamera.orthographicSize;
            _currentDesiredZoomValue = Mathf.Clamp(_currentDesiredZoomValue, minOrthographicSize, maxOrthographicSize);
            targetCamera.orthographicSize = _currentDesiredZoomValue; // Apply initial clamp immediately.
        }
        else // Perspective camera
        {
            _initialCameraValue = targetCamera.fieldOfView;
            _currentDesiredZoomValue = targetCamera.fieldOfView;
            _currentDesiredZoomValue = Mathf.Clamp(_currentDesiredZoomValue, minFieldOfView, maxFieldOfView);
            targetCamera.fieldOfView = _currentDesiredZoomValue; // Apply initial clamp immediately.
        }
    }

    private void Update()
    {
        // Skip update if no camera is assigned or the script is disabled.
        if (targetCamera == null || !enabled) return;

        HandleInput();          // Process user input for zooming.
        ApplyZoomSmoothing();   // Smoothly move the camera towards the desired zoom level.
    }

    // --- Input Handling ---
    /// <summary>
    /// Processes mouse scroll wheel input to change the desired zoom level.
    /// </summary>
    private void HandleInput()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        // Check for significant scroll input to avoid tiny fluctuations.
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            float zoomChangeAmount;
            if (_isOrthographic)
            {
                // For orthographic, positive scroll (up) means zoom in, so decrease size.
                zoomChangeAmount = scrollDelta * orthographicZoomSensitivity;
            }
            else // Perspective
            {
                // For perspective, positive scroll (up) means zoom in, so decrease FOV.
                zoomChangeAmount = scrollDelta * fieldOfViewZoomSensitivity;
            }

            // Update the desired zoom level. We subtract because positive scrollDelta should
            // generally lead to 'zooming in', which means a smaller orthographicSize or FOV.
            SetZoomLevel(_currentDesiredZoomValue - zoomChangeAmount);
        }
    }

    // --- Core Zoom Logic (Public Interface for Control) ---
    // These methods provide the interface for other parts of the game to control zoom,
    // acting as 'commands' that abstract the underlying camera type and zoom mechanism.

    /// <summary>
    /// Zooms the camera in by a predefined step amount.
    /// This decreases the target orthographic size (2D) or Field of View (3D).
    /// </summary>
    public void ZoomIn()
    {
        float step = _isOrthographic ? orthographicZoomStep : fieldOfViewZoomStep;
        SetZoomLevel(_currentDesiredZoomValue - step);
    }

    /// <summary>
    /// Zooms the camera out by a predefined step amount.
    /// This increases the target orthographic size (2D) or Field of View (3D).
    /// </summary>
    public void ZoomOut()
    {
        float step = _isOrthographic ? orthographicZoomStep : fieldOfViewZoomStep;
        SetZoomLevel(_currentDesiredZoomValue + step);
    }

    /// <summary>
    /// Sets the camera to a specific zoom level, clamping it within min/max bounds.
    /// This is the primary method for programmatically controlling the target zoom value.
    /// </summary>
    /// <param name="newZoomLevel">The desired orthographic size (for 2D) or Field of View (for 3D).</param>
    public void SetZoomLevel(float newZoomLevel)
    {
        if (_isOrthographic)
        {
            _currentDesiredZoomValue = Mathf.Clamp(newZoomLevel, minOrthographicSize, maxOrthographicSize);
        }
        else // Perspective
        {
            _currentDesiredZoomValue = Mathf.Clamp(newZoomLevel, minFieldOfView, maxFieldOfView);
        }
        // Notify any subscribers that the desired zoom level has changed.
        OnZoomLevelChanged?.Invoke(_currentDesiredZoomValue);
    }

    /// <summary>
    /// Resets the camera zoom to its initial value (the value it had when `Awake` was called).
    /// </summary>
    public void ResetZoom()
    {
        SetZoomLevel(_initialCameraValue);
    }

    // --- Smoothing Logic ---
    /// <summary>
    /// Smoothly interpolates the camera's actual zoom value towards the desired target zoom value.
    /// </summary>
    private void ApplyZoomSmoothing()
    {
        if (_isOrthographic)
        {
            // Only update if there's a significant difference to avoid constant assignments and ensure smooth stop.
            if (!Mathf.Approximately(targetCamera.orthographicSize, _currentDesiredZoomValue))
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, _currentDesiredZoomValue, Time.deltaTime * zoomLerpSpeed);
            }
        }
        else // Perspective
        {
            if (!Mathf.Approximately(targetCamera.fieldOfView, _currentDesiredZoomValue))
            {
                targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, _currentDesiredZoomValue, Time.deltaTime * zoomLerpSpeed);
            }
        }
    }

    // --- Gizmos for Visualization (Editor-only helper) ---
    private void OnDrawGizmos()
    {
        if (targetCamera == null) return;

        Gizmos.color = Color.yellow;
        Vector3 cameraPos = targetCamera.transform.position;
        Gizmos.DrawWireSphere(cameraPos, 0.5f); // Draw a sphere at the camera's position

        if (_isOrthographic)
        {
            // Visualize the min/max orthographic size range with wire cubes.
            // Note: Gizmos.DrawWireCube does not perfectly account for camera's aspect ratio in this simple form.
            // These are approximations to show the general range of zoom.
            Gizmos.color = Color.cyan; // Represents max zoom (furthest out, largest size)
            Gizmos.DrawWireCube(cameraPos, new Vector3(maxOrthographicSize * 2 * targetCamera.aspect, maxOrthographicSize * 2, 0));
            Gizmos.color = Color.red; // Represents min zoom (closest in, smallest size)
            Gizmos.DrawWireCube(cameraPos, new Vector3(minOrthographicSize * 2 * targetCamera.aspect, minOrthographicSize * 2, 0));
        }
        else // Perspective camera
        {
            // For perspective cameras, visualizing FOV with simple Gizmos is less direct.
            // We'll draw a line in the camera's forward direction to indicate its orientation.
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(cameraPos, cameraPos + targetCamera.transform.forward * 2f);
            // For more precise perspective visualization, consider using Gizmos.matrix and Gizmos.DrawFrustum.
        }
    }
}

/*
 * --- HOW TO USE THIS ZOOM CAMERA SYSTEM IN UNITY ---
 *
 * 1.  **Create an Empty GameObject**: In your Unity scene, create a new empty GameObject (e.g., named "CameraSystem").
 *
 * 2.  **Attach the Script**: Drag and drop this `ZoomCameraSystem.cs` script onto the "CameraSystem" GameObject.
 *
 * 3.  **Configure in the Inspector**:
 *     -   **Target Camera**: Assign your primary Camera object (e.g., "Main Camera") to this field.
 *         If left empty, the system will automatically try to use `Camera.main` at runtime.
 *     -   **Orthographic (2D) Zoom Settings**:
 *         -   `Min Orthographic Size`: Set the smallest `orthographicSize` value (most zoomed-in view).
 *         -   `Max Orthographic Size`: Set the largest `orthographicSize` value (most zoomed-out view).
 *         -   `Orthographic Zoom Sensitivity`: Controls how much the `orthographicSize` changes per mouse scroll 'tick'.
 *         -   `Orthographic Zoom Step`: The fixed amount of `orthographicSize` change when `ZoomIn()` or `ZoomOut()` is called programmatically.
 *     -   **Perspective (3D) Zoom Settings**:
 *         -   `Min Field of View`: Set the smallest `fieldOfView` value (most zoomed-in view).
 *         -   `Max Field of View`: Set the largest `fieldOfView` value (most zoomed-out view).
 *         -   `Field Of View Zoom Sensitivity`: Controls how much the `fieldOfView` changes per mouse scroll 'tick'.
 *         -   `Field Of View Zoom Step`: The fixed amount of `fieldOfView` change when `ZoomIn()` or `ZoomOut()` is called programmatically.
 *     -   **Zoom Smoothing**:
 *         -   `Zoom Lerp Speed`: Adjust this value for smoother (lower value) or snappier (higher value) transitions between zoom levels.
 *
 * 4.  **Camera Projection**: Ensure your actual Camera component's 'Projection' property is set correctly:
 *     -   'Orthographic' for 2D games (the system will use the Orthographic settings).
 *     -   'Perspective' for 3D games (the system will use the Perspective settings).
 *
 * 5.  **Run the Scene**: Start your game. You should now be able to use the **Mouse Scroll Wheel** to smoothly zoom your camera in and out within the defined limits.
 *
 * --- EXAMPLE USAGE IN OTHER SCRIPTS (Client Code) ---
 *
 * Here are examples of how other scripts can interact with the `ZoomCameraSystem`.
 *
 * ```csharp
 * // Example 1: A simple UI button that resets the camera zoom.
 * // Attach this script to a UI Button GameObject and assign its OnClick() event to 'OnResetButtonClick()'.
 * public class ZoomResetButton : MonoBehaviour
 * {
 *     public void OnResetButtonClick()
 *     {
 *         // Access the Singleton instance of ZoomCameraSystem and call its ResetZoom method.
 *         if (ZoomCameraSystem.Instance != null)
 *         {
 *             ZoomCameraSystem.Instance.ResetZoom();
 *             Debug.Log("Camera zoom reset to initial level.");
 *         }
 *     }
 * }
 * ```
 *
 * ```csharp
 * // Example 2: A game manager or another system that reacts to camera zoom changes (Observer Pattern).
 * // This could be used to adjust game element visibility, UI scaling, or trigger special effects.
 * public class GameEventHandler : MonoBehaviour
 * {
 *     [Header("Zoom Reaction Logic")]
 *     [Tooltip("The orthographic size or FOV threshold for triggering detailed objects.")]
 *     public float detailThreshold = 5f; // Example: for orthographic, show details if size < 5.
 *     [Tooltip("A GameObject that becomes active/inactive based on zoom level.")]
 *     public GameObject detailedObjects;
 *
 *     void OnEnable()
 *     {
 *         // Subscribe to the OnZoomLevelChanged event when this script becomes active.
 *         // This enables it to receive notifications whenever the zoom level changes.
 *         ZoomCameraSystem.OnZoomLevelChanged += HandleZoomChanged;
 *         Debug.Log("GameEventHandler: Subscribed to ZoomCameraSystem.OnZoomLevelChanged.");
 *     }
 *
 *     void OnDisable()
 *     {
 *         // Crucially, unsubscribe from the event when this script is disabled or destroyed.
 *         // This prevents memory leaks and ensures the script doesn't try to access the system
 *         // when it's no longer valid.
 *         ZoomCameraSystem.OnZoomLevelChanged -= HandleZoomChanged;
 *         Debug.Log("GameEventHandler: Unsubscribed from ZoomCameraSystem.OnZoomLevelChanged.");
 *     }
 *
 *     /// <summary>
 *     /// This method is called by the ZoomCameraSystem whenever the desired zoom level changes.
 *     /// </summary>
 *     /// <param name="newZoomLevel">The new target orthographic size or field of view.</param>
 *     private void HandleZoomChanged(float newZoomLevel)
 *     {
 *         Debug.Log($"GameEventHandler: Camera zoom level changed to: {newZoomLevel}");
 *
 *         // Example reaction: Adjust visibility of detailed objects.
 *         // If the camera is zoomed in sufficiently (newZoomLevel is smaller than threshold), show details.
 *         if (detailedObjects != null)
 *         {
 *             bool shouldShowDetails = newZoomLevel < detailThreshold;
 *             if (detailedObjects.activeSelf != shouldShowDetails)
 *             {
 *                 detailedObjects.SetActive(shouldShowDetails);
 *                 Debug.Log(shouldShowDetails ? "Showing detailed objects." : "Hiding detailed objects.");
 *             }
 *         }
 *     }
 *
 *     // Example 3: Programmatically controlling zoom from another script (e.g., triggered by game events).
 *     void Start()
 *     {
 *         // Always check if the Instance is available, especially if the system might not be
 *         // initialized yet or might be destroyed.
 *         if (ZoomCameraSystem.Instance != null)
 *         {
 *             // You can call ZoomIn() or ZoomOut() directly for fixed step changes:
 *             // ZoomCameraSystem.Instance.ZoomIn();
 *
 *             // Or set a specific zoom level after a delay using a Coroutine:
 *             // StartCoroutine(DelayedSetSpecificZoom());
 *         }
 *     }
 *
 *     // An example Coroutine to demonstrate delayed, programmatic zoom changes.
 *     IEnumerator DelayedSetSpecificZoom()
 *     {
 *         yield return new WaitForSeconds(3f); // Wait for 3 seconds
 *
 *         // Set to a specific orthographic size (e.g., 5 units for a 2D camera).
 *         // The system will automatically clamp this value if it's out of bounds.
 *         ZoomCameraSystem.Instance.SetZoomLevel(5f);
 *         Debug.Log("Programmatically set zoom to 5f.");
 *
 *         yield return new WaitForSeconds(3f);
 *
 *         // Or set to a specific Field of View (e.g., 40 degrees for a 3D camera).
 *         ZoomCameraSystem.Instance.SetZoomLevel(40f);
 *         Debug.Log("Programmatically set zoom to 40f.");
 *     }
 * }
 * ```
 */
```