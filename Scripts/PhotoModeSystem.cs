// Unity Design Pattern Example: PhotoModeSystem
// This script demonstrates the PhotoModeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **'PhotoModeSystem'** design pattern. It demonstrates how to manage different states, control the camera, handle UI visibility, pause game time, and capture screenshots, all while ensuring proper decoupling and adherence to Unity best practices.

The pattern centralizes the logic for entering, being active in, and exiting photo mode, allowing other game systems to react to these changes through events without direct dependencies.

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.IO;        // Required for file path operations (screenshots)
using System.Collections; // Required for Coroutines (e.g., for transitions and screenshot capture)

/// <summary>
/// The PhotoModeSystem is a centralized manager for all Photo Mode functionalities.
/// It handles state transitions, camera control, UI visibility, game pausing,
/// and screenshot capture.
///
/// This script implements the PhotoModeSystem design pattern by:
/// 1.  **Centralized Control:** A single singleton instance manages all photo mode aspects.
/// 2.  **State Management:** Uses an enum to define distinct states (Inactive, Entering, Active, Exiting)
///     and transitions between them.
/// 3.  **Decoupling with Events:** Uses UnityEvents to notify other game systems (like UI, PlayerController)
///     when Photo Mode activates or deactivates, allowing them to react accordingly without direct dependencies.
/// 4.  **Camera Management:** Takes control of the main camera, swaps it with a dedicated photo mode camera,
///     and provides free-look controls.
/// 5.  **UI Management:** Hides regular game UI and shows photo mode specific UI when active,
///     with an option to hide photo mode UI for clean screenshots.
/// 6.  **Game State Control:** Pauses or slows down game time.
/// 7.  **Screenshot Functionality:** Captures and saves screenshots.
/// </summary>
public class PhotoModeSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global access point to the PhotoModeSystem instance.
    public static PhotoModeSystem Instance { get; private set; }

    // --- Inspector Settings ---

    [Header("Core Settings")]
    [Tooltip("The key to toggle Photo Mode on/off.")]
    [SerializeField] private KeyCode togglePhotoModeKey = KeyCode.P;
    [Tooltip("The key to hide/show the Photo Mode UI panel.")]
    [SerializeField] private KeyCode togglePhotoModeUIKey = KeyCode.H;
    [Tooltip("The key to take a screenshot.")]
    [SerializeField] private KeyCode takeScreenshotKey = KeyCode.K;
    [Tooltip("The time scale when photo mode is active. Set to 0 for full pause.")]
    [Range(0f, 1f)]
    [SerializeField] private float photoModeTimeScale = 0f;

    [Header("Camera Control")]
    [Tooltip("The dedicated camera rig or GameObject that will take over during photo mode.")]
    [SerializeField] private GameObject photoModeCameraRig;
    [Tooltip("The speed at which the camera moves with WASD/QE.")]
    [SerializeField] private float cameraMoveSpeed = 5f;
    [Tooltip("The speed at which the camera rotates with mouse input (when right-click is held).")]
    [SerializeField] private float cameraRotateSpeed = 2f;
    [Tooltip("The speed at which the camera zooms (changes FOV) with the scroll wheel.")]
    [SerializeField] private float cameraZoomSpeed = 5f;
    [Tooltip("Minimum Field of View for zooming.")]
    [SerializeField] private float minFOV = 10f;
    [Tooltip("Maximum Field of View for zooming.")]
    [SerializeField] private float maxFOV = 90f;

    [Header("UI References")]
    [Tooltip("The UI panel that contains all Photo Mode specific controls and elements.")]
    [SerializeField] private GameObject photoModeUIPanel;
    [Tooltip("An array of main game UI panels that should be hidden when Photo Mode is active.")]
    [SerializeField] private GameObject[] gameUIPanelsToHide;

    [Header("Events")]
    [Tooltip("Event fired when Photo Mode is activated. Other systems can subscribe to this.")]
    public UnityEvent OnPhotoModeActivated = new UnityEvent();
    [Tooltip("Event fired when Photo Mode is deactivated. Other systems can subscribe to this.")]
    public UnityEvent OnPhotoModeDeactivated = new UnityEvent();

    // --- Internal State Variables ---
    private PhotoModeState currentState = PhotoModeState.Inactive;
    private Camera mainGameCamera; // Reference to the game's primary camera (e.g., player camera)
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalCameraFOV;
    private float originalTimeScale;
    private bool photoModeUIVisible = true; // Tracks the visibility of the photo mode UI itself

    // Defines the different states of the Photo Mode system
    private enum PhotoModeState
    {
        Inactive,      // Game is running normally, Photo Mode is off
        Entering,      // Transitioning into photo mode (e.g., pausing game, hiding UI)
        Active,        // Photo mode is fully active, camera is controllable, UI is visible/toggleable
        Exiting        // Transitioning out of photo mode (e.g., restoring game, showing UI)
    }

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Enforce the singleton pattern: only one instance can exist.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PhotoModeSystem instances found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the manager persistent across scene loads.

        // Validate essential references.
        if (photoModeCameraRig == null)
        {
            Debug.LogError("PhotoModeSystem: Photo Mode Camera Rig is not assigned! Disabling script.", this);
            enabled = false; // Disable this script if a critical reference is missing.
            return;
        }

        // Initially hide the photo mode camera and its UI.
        photoModeCameraRig.SetActive(false);
        if (photoModeUIPanel != null) photoModeUIPanel.SetActive(false);

        // Find the main game camera. It's crucial for it to be tagged "MainCamera".
        mainGameCamera = Camera.main;
        if (mainGameCamera == null)
        {
            Debug.LogError("PhotoModeSystem: No main camera found! Please ensure your primary game camera is tagged 'MainCamera'. Disabling script.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // Ensure the system starts in an inactive state.
        TransitionToState(PhotoModeState.Inactive);
    }

    private void Update()
    {
        // Allow toggling Photo Mode from any state (except during transitions).
        if (Input.GetKeyDown(togglePhotoModeKey))
        {
            if (currentState == PhotoModeState.Inactive)
            {
                EnterPhotoMode();
            }
            else if (currentState == PhotoModeState.Active)
            {
                ExitPhotoMode();
            }
        }

        // Only process photo mode specific input and camera controls when active.
        if (currentState == PhotoModeState.Active)
        {
            HandlePhotoModeInput();
            HandleCameraMovement();
            HandleCameraRotation();
            HandleCameraZoom();
        }
    }

    // --- Public API for Photo Mode Control ---

    /// <summary>
    /// Initiates the process of activating Photo Mode.
    /// This method can be called by UI buttons or other game logic.
    /// </summary>
    public void EnterPhotoMode()
    {
        if (currentState == PhotoModeState.Inactive)
        {
            TransitionToState(PhotoModeState.Entering);
            StartCoroutine(ActivatePhotoModeRoutine());
        }
        else
        {
            Debug.LogWarning($"Attempted to enter Photo Mode while in {currentState} state. Must be Inactive.");
        }
    }

    /// <summary>
    /// Initiates the process of deactivating Photo Mode.
    /// This method can be called by UI buttons or other game logic.
    /// </summary>
    public void ExitPhotoMode()
    {
        if (currentState == PhotoModeState.Active)
        {
            TransitionToState(PhotoModeState.Exiting);
            StartCoroutine(DeactivatePhotoModeRoutine());
        }
        else
        {
            Debug.LogWarning($"Attempted to exit Photo Mode while in {currentState} state. Must be Active.");
        }
    }

    /// <summary>
    /// Captures a screenshot and saves it to a designated folder.
    /// The Photo Mode UI is temporarily hidden for a clean shot.
    /// </summary>
    public void TakePicture()
    {
        if (currentState != PhotoModeState.Active)
        {
            Debug.LogWarning("Can only take pictures when Photo Mode is active.");
            return;
        }

        // Temporarily hide the Photo Mode UI for a clean screenshot.
        bool wasUIVisible = photoModeUIPanel != null && photoModeUIPanel.activeSelf;
        if (wasUIVisible)
        {
            photoModeUIPanel.SetActive(false);
        }

        StartCoroutine(CaptureScreenshotRoutine(wasUIVisible));
    }

    // --- Internal Coroutines for Smooth State Transitions ---

    private IEnumerator ActivatePhotoModeRoutine()
    {
        Debug.Log("Activating Photo Mode...");

        // 1. Store original game state for restoration later.
        originalTimeScale = Time.timeScale;
        originalCameraPosition = mainGameCamera.transform.position;
        originalCameraRotation = mainGameCamera.transform.rotation;
        originalCameraFOV = mainGameCamera.fieldOfView;

        // 2. Adjust game state for photo mode.
        Time.timeScale = photoModeTimeScale; // Pause or slow down the game.
        OnPhotoModeActivated?.Invoke();       // Notify subscribers that photo mode is active.

        // 3. Hide all specified main game UI panels.
        foreach (var uiPanel in gameUIPanelsToHide)
        {
            if (uiPanel != null) uiPanel.SetActive(false);
        }

        // 4. Position the photo mode camera rig to match the main game camera, then activate it.
        photoModeCameraRig.transform.position = originalCameraPosition;
        photoModeCameraRig.transform.rotation = originalCameraRotation;
        
        // Ensure the Camera component within the rig also has the correct FOV initially.
        Camera pmCam = photoModeCameraRig.GetComponentInChildren<Camera>();
        if (pmCam != null)
        {
            pmCam.fieldOfView = originalCameraFOV;
        }

        mainGameCamera.enabled = false;     // Disable the original game camera.
        photoModeCameraRig.SetActive(true); // Enable the photo mode camera rig.

        // 5. Show the photo mode UI panel.
        if (photoModeUIPanel != null)
        {
            photoModeUIPanel.SetActive(true);
            photoModeUIVisible = true; // Ensure internal flag matches.
        }

        // Yield for one frame to ensure all systems have updated before transitioning to Active.
        yield return null; 

        TransitionToState(PhotoModeState.Active);
        Debug.Log("Photo Mode is now Active.");
    }

    private IEnumerator DeactivatePhotoModeRoutine()
    {
        Debug.Log("Deactivating Photo Mode...");

        // 1. Restore the game's original time scale.
        Time.timeScale = originalTimeScale;
        OnPhotoModeDeactivated?.Invoke(); // Notify subscribers that photo mode is deactivated.

        // 2. Hide the photo mode UI and camera.
        if (photoModeUIPanel != null) photoModeUIPanel.SetActive(false);
        photoModeCameraRig.SetActive(false);

        // 3. Restore the main game camera's original state and re-enable it.
        mainGameCamera.transform.position = originalCameraPosition;
        mainGameCamera.transform.rotation = originalCameraRotation;
        mainGameCamera.fieldOfView = originalCameraFOV;
        mainGameCamera.enabled = true;

        // 4. Restore all specified main game UI panels.
        foreach (var uiPanel in gameUIPanelsToHide)
        {
            if (uiPanel != null) uiPanel.SetActive(true);
        }

        // Yield for one frame for visual consistency.
        yield return null; 

        TransitionToState(PhotoModeState.Inactive);
        Debug.Log("Photo Mode is now Inactive.");
    }

    private IEnumerator CaptureScreenshotRoutine(bool restoreUIAfterCapture)
    {
        // Define screenshot save path.
        // Application.persistentDataPath is a reliable cross-platform path for user data.
        string folderPath = Path.Combine(Application.persistentDataPath, "Screenshots");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Create a unique file name with a timestamp.
        string fileName = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string filePath = Path.Combine(folderPath, fileName);

        Debug.Log($"Capturing screenshot to: {filePath}");

        // Wait for the end of the frame to ensure all rendering is complete before capturing.
        yield return new WaitForEndOfFrame();

        ScreenCapture.CaptureScreenshot(filePath);

        Debug.Log($"Screenshot saved: {fileName}");

        // If the UI was visible before the screenshot, restore its visibility.
        if (restoreUIAfterCapture)
        {
            if (photoModeUIPanel != null)
            {
                photoModeUIPanel.SetActive(true);
            }
        }
    }

    // --- Internal Input Handling and Camera Control ---

    private void HandlePhotoModeInput()
    {
        // Toggle Photo Mode UI visibility.
        if (Input.GetKeyDown(togglePhotoModeUIKey))
        {
            photoModeUIVisible = !photoModeUIVisible;
            if (photoModeUIPanel != null)
            {
                photoModeUIPanel.SetActive(photoModeUIVisible);
            }
        }

        // Take Screenshot.
        if (Input.GetKeyDown(takeScreenshotKey))
        {
            TakePicture();
        }
    }

    private void HandleCameraMovement()
    {
        // Get input for horizontal, vertical, and depth movement.
        float horizontal = Input.GetAxis("Horizontal"); // A/D keys or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");     // W/S keys or Up/Down arrows
        float depth = 0f;                               // Q/E keys for up/down movement
        if (Input.GetKey(KeyCode.Q)) depth = -1f;
        if (Input.GetKey(KeyCode.E)) depth = 1f;

        Vector3 moveDirection = new Vector3(horizontal, depth, vertical);
        // Use Time.unscaledDeltaTime because Time.timeScale might be 0 (game paused).
        photoModeCameraRig.transform.Translate(moveDirection * cameraMoveSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    private void HandleCameraRotation()
    {
        // Rotate only when the right mouse button is held down (common for free-look cameras).
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Rotate around the Y-axis (yaw) for horizontal mouse movement.
            // Using Space.World prevents unwanted tilting when pitching.
            photoModeCameraRig.transform.Rotate(Vector3.up, mouseX * cameraRotateSpeed, Space.World);

            // Rotate around the X-axis (pitch) for vertical mouse movement.
            // We need to clamp pitch to prevent the camera from flipping upside down.
            Vector3 currentEuler = photoModeCameraRig.transform.localEulerAngles;
            float newPitch = currentEuler.x - mouseY * cameraRotateSpeed; // Subtract for inverted Y-axis usually.
            
            // Normalize the angle to make clamping consistent (e.g., -180 to 180).
            newPitch = NormalizeAngle(newPitch);
            // Clamp the pitch to prevent extreme rotations.
            newPitch = Mathf.Clamp(newPitch, -89f, 89f); 
            
            photoModeCameraRig.transform.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
        }
    }

    private void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Camera pmCam = photoModeCameraRig.GetComponentInChildren<Camera>();
            if (pmCam != null)
            {
                // Adjust FOV based on scroll input and clamp it within defined limits.
                pmCam.fieldOfView = Mathf.Clamp(pmCam.fieldOfView - scroll * cameraZoomSpeed, minFOV, maxFOV);
            }
        }
    }

    // --- State Management ---

    /// <summary>
    /// Manages the state transitions of the Photo Mode system.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void TransitionToState(PhotoModeState newState)
    {
        if (currentState == newState) return; // No state change if already in target state.

        Debug.Log($"Photo Mode State Change: {currentState} -> {newState}");
        currentState = newState;

        // Additional state-specific logic could be added here if needed,
        // though most transition logic is handled by the coroutines.
    }

    // --- Utility Methods ---

    /// <summary>
    /// Normalizes an angle to be within the range of -180 to 180 degrees.
    /// Useful for consistent angle clamping.
    /// </summary>
    /// <param name="angle">The angle to normalize.</param>
    /// <returns>The normalized angle.</returns>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    // --- Debugging and Editor Integration ---

    // A simple OnGUI display for showing the current Photo Mode state and controls.
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Photo Mode: {currentState}");
        if (currentState == PhotoModeState.Active)
        {
            GUI.Label(new Rect(10, 30, 300, 20), $"[P] Toggle Photo Mode");
            GUI.Label(new Rect(10, 50, 300, 20), $"[WASD/QE] Move Camera");
            GUI.Label(new Rect(10, 70, 300, 20), $"[Right Click + Mouse] Rotate Camera");
            GUI.Label(new Rect(10, 90, 300, 20), $"[Scroll Wheel] Zoom FOV");
            GUI.Label(new Rect(10, 110, 300, 20), $"[H] Toggle UI");
            GUI.Label(new Rect(10, 130, 300, 20), $"[K] Take Screenshot");
            GUI.Label(new Rect(10, 150, 300, 20), $"UI Visible: {photoModeUIVisible}");
            GUI.Label(new Rect(10, 170, 300, 20), $"Screenshots saved to: {Application.persistentDataPath}/Screenshots");
        }
    }
}
```

---

### **How to Implement and Use in Unity (Example Usage):**

Follow these steps to integrate the `PhotoModeSystem` into your Unity project:

1.  **Create a Manager GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> `Create Empty`).
    *   Rename it to `_GameManagers` or `PhotoModeManager`.

2.  **Attach the Script:**
    *   Drag and drop the `PhotoModeSystem.cs` script onto the `_GameManagers` GameObject in the Hierarchy or Inspector.

3.  **Ensure Main Camera Tag:**
    *   Select your primary game camera (e.g., your player's first-person camera or a third-person camera).
    *   In the Inspector, ensure its `Tag` property is set to `MainCamera`. This is crucial for the system to find and store its original state.

4.  **Create the Photo Mode Camera Rig:**
    *   Create another empty GameObject in your scene (e.g., `PhotoModeCameraRig`).
    *   Add a `Camera` component to this `PhotoModeCameraRig` GameObject (or a child of it if you prefer a more complex rig). This camera will be enabled and controlled when Photo Mode is active.
    *   **Drag** this `PhotoModeCameraRig` GameObject from the Hierarchy into the `Photo Mode Camera Rig` slot in the `PhotoModeSystem` component's Inspector.

5.  **Prepare UI Elements:**
    *   **Main Game UI Panel(s):** Identify the GameObject(s) that represent your normal game UI (e.g., a `Canvas` panel displaying health, score, mini-map).
        *   Drag these GameObjects into the `Game UI Panels To Hide` array in the `PhotoModeSystem` Inspector. The system will `SetActive(false)` these when Photo Mode starts and `SetActive(true)` when it ends.
    *   **Photo Mode UI Panel:** Create a `Canvas` (or a specific panel within an existing `Canvas`) dedicated to Photo Mode controls (e.g., buttons for "Take Photo," sliders for FOV, filter options, "Exit Photo Mode" button).
        *   Ensure this `PhotoModeUIPanel` GameObject is initially **disabled** in the Hierarchy.
        *   **Drag** this `PhotoModeUIPanel` GameObject into the `Photo Mode UI Panel` slot in the `PhotoModeSystem` Inspector.

6.  **Configure Settings (Inspector):**
    *   Adjust `togglePhotoModeKey` (default `P`), `togglePhotoModeUIKey` (default `H`), `takeScreenshotKey` (default `K`) to your preference.
    *   Set `photoModeTimeScale` (default `0`) to fully pause the game or use a small value for slow-motion.
    *   Tweak `cameraMoveSpeed`, `cameraRotateSpeed`, `cameraZoomSpeed`, `minFOV`, and `maxFOV` for desired camera feel.

7.  **Example Listener Script (Optional, but Recommended for Decoupling):**
    *   You might have a `PlayerController` or `GameUIManager` script that needs to react to Photo Mode.
    *   Create a script like `PlayerInputDisabler.cs`:

    ```csharp
    using UnityEngine;

    public class PlayerInputDisabler : MonoBehaviour
    {
        // Reference to your player controller script
        [SerializeField] private MonoBehaviour playerController; // Or a specific type like MyPlayerController

        void OnEnable()
        {
            // Subscribe to Photo Mode events
            if (PhotoModeSystem.Instance != null)
            {
                PhotoModeSystem.Instance.OnPhotoModeActivated.AddListener(DisablePlayerInput);
                PhotoModeSystem.Instance.OnPhotoModeDeactivated.AddListener(EnablePlayerInput);
            }
        }

        void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            if (PhotoModeSystem.Instance != null)
            {
                PhotoModeSystem.Instance.OnPhotoModeActivated.RemoveListener(DisablePlayerInput);
                PhotoModeSystem.Instance.OnPhotoModeDeactivated.RemoveListener(EnablePlayerInput);
            }
        }

        private void DisablePlayerInput()
        {
            if (playerController != null)
            {
                Debug.Log("Player input disabled due to Photo Mode.");
                playerController.enabled = false; // Disable the player's movement script
            }
        }

        private void EnablePlayerInput()
        {
            if (playerController != null)
            {
                Debug.Log("Player input re-enabled after Photo Mode.");
                playerController.enabled = true; // Re-enable the player's movement script
            }
        }
    }
    ```
    *   Attach this `PlayerInputDisabler.cs` script to your Player GameObject and drag your actual player controller script into its `Player Controller` slot.

8.  **Run the Scene:**
    *   Press Play in Unity.
    *   Press the `P` key (default) to toggle Photo Mode.
    *   Once in Photo Mode, use `WASD` for horizontal movement, `Q` and `E` for vertical movement, hold `Right-Click` and move the mouse to rotate, and use the `Scroll Wheel` to zoom (change FOV).
    *   Press `H` to hide/show the Photo Mode UI.
    *   Press `K` to take a screenshot. Screenshots will be saved to your `Application.persistentDataPath`/Screenshots folder (e.g., `C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\Screenshots` on Windows).

This setup provides a fully functional and extensible Photo Mode system for your Unity project!