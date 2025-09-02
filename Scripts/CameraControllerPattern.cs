// Unity Design Pattern Example: CameraControllerPattern
// This script demonstrates the CameraControllerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The CameraControllerPattern is a design pattern used in game development to separate the logic for camera movement and behavior from the camera itself and the objects it observes. This promotes modularity, reusability, and testability, allowing you to easily swap different camera control schemes without modifying the core camera or the target.

### Key Components of the CameraControllerPattern:

1.  **`ICameraController` (Interface):** Defines the common contract that all camera control behaviors must adhere to. It typically includes methods for initializing the controller and updating the camera's position/rotation.
2.  **`BaseCameraController` (Abstract Class):** An optional abstract base class (often a `MonoBehaviour` in Unity) that implements `ICameraController`. It can provide common properties (like `camera` and `target`) and methods, reducing code duplication in concrete implementations.
3.  **Concrete `CameraController`s (Implementations):** These are specific camera behaviors (e.g., `FollowTargetCameraController`, `OrbitCameraController`, `FixedCameraController`). Each implements the `ICameraController` interface and defines its unique camera logic.
4.  **`CameraManager` (or `CameraBrain` / `CameraSystem`):** This is the central orchestrator. It holds a reference to the actual Unity `Camera` and manages an array or list of `ICameraController` instances. Its primary responsibilities include:
    *   Initializing and providing the camera and target to the active controller.
    *   Calling the `UpdateCamera` method on the currently active controller (typically in `LateUpdate`).
    *   Providing methods to switch between different camera controllers at runtime.

### Advantages:

*   **Modularity:** Easily swap camera behaviors (e.g., switch from a follow camera to an orbit camera for a cutscene) without touching the Camera component itself.
*   **Reusability:** Individual camera controllers can be reused in different parts of the game or in entirely different projects.
*   **Flexibility:** New camera behaviors can be added easily by creating a new `ICameraController` implementation without altering existing code.
*   **Testability:** Each camera controller's logic can be tested in isolation.

---

### Complete C# Unity Example: `CameraControllerPattern.cs`

This script provides a `CameraManager` that orchestrates different `BaseCameraController` components attached to the same GameObject. It includes two concrete examples: `FollowTargetCameraController` and `OrbitCameraController`.

To use this, create an empty GameObject in your scene, attach the `CameraManager` script, then attach the specific `CameraController` scripts you want to use (e.g., `FollowTargetCameraController`, `OrbitCameraController`) to the *same* GameObject.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List if needed, but Array.IndexOf will work for now

/// <summary>
/// ICameraController Interface:
/// Defines the contract for all camera controller implementations.
/// This ensures that any class intending to control the camera must provide
/// methods for initialization and updating the camera's state.
/// </summary>
public interface ICameraController
{
    /// <summary>
    /// Initializes the camera controller with the camera to control and its target.
    /// </summary>
    /// <param name="cameraToControl">The Unity Camera component that this controller will manipulate.</param>
    /// <param name="target">The Transform that the camera will observe or follow.</param>
    void Initialize(Camera cameraToControl, Transform target);

    /// <summary>
    /// Updates the camera's position, rotation, or other properties based on its specific logic.
    /// This method is typically called by a CameraManager in LateUpdate.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame, useful for frame-rate independent movement.</param>
    void UpdateCamera(float deltaTime);

    /// <summary>
    /// Called when this controller becomes the active controller.
    /// Used for enabling specific logic, setting up state, etc.
    /// </summary>
    void OnActivate();

    /// <summary>
    /// Called when this controller is no longer the active controller.
    /// Used for disabling specific logic, cleaning up state, etc.
    /// </summary>
    void OnDeactivate();
}

/// <summary>
/// BaseCameraController Abstract Class:
/// Provides a common base for all concrete camera controllers in Unity.
/// By inheriting from MonoBehaviour and implementing ICameraController,
/// concrete controllers can be attached as components to GameObjects and
/// share common properties like the controlled camera and target.
/// </summary>
[DisallowMultipleComponent] // Prevents multiple instances of the *same* controller type on one GO
public abstract class BaseCameraController : MonoBehaviour, ICameraController
{
    // Protected references to the camera and target, accessible by derived classes.
    protected Camera _camera;
    protected Transform _target;

    /// <summary>
    /// Virtual Initialize method allows derived classes to add their own initialization logic
    /// while still ensuring the base properties (_camera, _target) are set.
    /// </summary>
    /// <param name="cameraToControl">The Unity Camera component.</param>
    /// <param name="target">The Transform being observed.</param>
    public virtual void Initialize(Camera cameraToControl, Transform target)
    {
        _camera = cameraToControl;
        _target = target;
        // Optionally, ensure the component is disabled until activated by the CameraManager.
        this.enabled = false; 
    }

    /// <summary>
    /// Abstract method that must be implemented by all concrete camera controllers.
    /// This is where the specific camera movement logic will reside.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public abstract void UpdateCamera(float deltaTime);

    /// <summary>
    /// Default OnActivate behavior: simply enables this MonoBehaviour component.
    /// Derived classes can override this for additional activation logic.
    /// </summary>
    public virtual void OnActivate()
    {
        this.enabled = true;
        // Debug.Log($"Activated: {GetType().Name}"); // Uncomment for debugging
    }

    /// <summary>
    /// Default OnDeactivate behavior: simply disables this MonoBehaviour component.
    /// Derived classes can override this for additional deactivation logic.
    /// </summary>
    public virtual void OnDeactivate()
    {
        this.enabled = false;
        // Debug.Log($"Deactivated: {GetType().Name}"); // Uncomment for debugging
    }
}

/// <summary>
/// FollowTargetCameraController:
/// A concrete implementation of a camera controller that follows a target
/// with a specified offset and smoothly rotates to look at it.
/// </summary>
[AddComponentMenu("Camera Controllers/Follow Target Camera")] // Adds to component menu
public class FollowTargetCameraController : BaseCameraController
{
    [Header("Follow Settings")]
    [Tooltip("The offset from the target's position.")]
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    [Tooltip("How quickly the camera moves towards its desired position.")]
    [Range(1f, 20f)] public float followSpeed = 5f;
    [Tooltip("How quickly the camera rotates to look at the target.")]
    [Range(1f, 20f)] public float lookAtSpeed = 5f;

    /// <summary>
    /// Updates the camera's position and rotation to follow and look at the target.
    /// This method is called by the CameraManager when this controller is active.
    /// </summary>
    /// <param name="deltaTime">Time since the last frame.</param>
    public override void UpdateCamera(float deltaTime)
    {
        // Early exit if essential components are missing.
        if (_camera == null || _target == null) return;

        // Calculate the desired position based on the target's position and rotation.
        // Multiplying by target.rotation ensures the offset is relative to the target's forward direction.
        Vector3 desiredPosition = _target.position + _target.rotation * offset;

        // Smoothly move the camera's position towards the desired position.
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, desiredPosition, followSpeed * deltaTime);

        // Calculate the desired rotation to look at the target's position.
        Quaternion desiredRotation = Quaternion.LookRotation(_target.position - _camera.transform.position);
        
        // Smoothly rotate the camera towards the desired rotation.
        _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, desiredRotation, lookAtSpeed * deltaTime);
    }
}

/// <summary>
/// OrbitCameraController:
/// A concrete implementation of a camera controller that orbits around a target
/// based on mouse input. The player can use the right mouse button to rotate the camera.
/// </summary>
[AddComponentMenu("Camera Controllers/Orbit Camera")] // Adds to component menu
public class OrbitCameraController : BaseCameraController
{
    [Header("Orbit Settings")]
    [Tooltip("The distance from the target.")]
    public float distance = 5f;
    [Tooltip("The height offset from the target's pivot.")]
    public float height = 2f;
    [Tooltip("Sensitivity for mouse input when orbiting horizontally.")]
    [Range(0.1f, 10f)] public float horizontalSensitivity = 3f;
    [Tooltip("Sensitivity for mouse input when orbiting vertically.")]
    [Range(0.1f, 10f)] public float verticalSensitivity = 3f;
    [Tooltip("Minimum pitch angle (vertical rotation).")]
    [Range(-90f, 0f)] public float minPitch = -80f; // Look down
    [Tooltip("Maximum pitch angle (vertical rotation).")]
    [Range(0f, 90f)] public float maxPitch = 80f;   // Look up

    private float _currentYaw = 0f;    // Horizontal rotation around the target (Y-axis)
    private float _currentPitch = 0f;  // Vertical rotation (X-axis)

    /// <summary>
    /// Initializes the orbit camera. It sets up the initial yaw and pitch based
    /// on the camera's starting orientation relative to the target.
    /// </summary>
    public override void Initialize(Camera cameraToControl, Transform target)
    {
        base.Initialize(cameraToControl, target); // Call base initialization

        if (_camera != null && _target != null)
        {
            // Calculate initial yaw/pitch based on current camera position relative to target
            Vector3 toTarget = _target.position - _camera.transform.position;
            Quaternion initialRotation = Quaternion.LookRotation(toTarget.normalized);
            
            // Extract Euler angles and adjust for Unity's coordinate system
            Vector3 euler = initialRotation.eulerAngles;
            
            // Adjust pitch (x-axis rotation) for angles > 180 (e.g., 270 becomes -90)
            _currentPitch = euler.x > 180 ? euler.x - 360 : euler.x;
            _currentYaw = euler.y;

            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
        }
    }

    /// <summary>
    /// Updates the camera's position and rotation for orbiting.
    /// This method responds to mouse input (right-click) for rotation.
    /// </summary>
    /// <param name="deltaTime">Time since the last frame.</param>
    public override void UpdateCamera(float deltaTime)
    {
        // Early exit if essential components are missing.
        if (_camera == null || _target == null) return;

        // Only orbit when the right mouse button is held down.
        if (Input.GetMouseButton(1))
        {
            // Update yaw (horizontal rotation) based on mouse X input.
            _currentYaw += Input.GetAxis("Mouse X") * horizontalSensitivity;
            // Update pitch (vertical rotation) based on mouse Y input (inverted for natural feel).
            _currentPitch -= Input.GetAxis("Mouse Y") * verticalSensitivity;
            
            // Clamp pitch to prevent the camera from going upside down or too far up/down.
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
        }
        
        // Calculate the rotation quaternion based on current pitch and yaw.
        Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
        
        // Calculate the desired position:
        // Start from target, move back by distance in the direction of the rotation,
        // then add the height offset.
        Vector3 position = _target.position - (rotation * Vector3.forward * distance) + Vector3.up * height;

        // Apply the calculated position and rotation to the camera.
        _camera.transform.position = position;
        _camera.transform.rotation = rotation;
    }
}

/// <summary>
/// CameraManager:
/// The central component that orchestrates which ICameraController is active.
/// It holds references to the main camera, the target, and all available
/// camera controllers attached to this GameObject. It's responsible for:
/// 1. Initializing all controllers.
/// 2. Switching between controllers.
/// 3. Calling the active controller's UpdateCamera method in LateUpdate.
/// </summary>
[AddComponentMenu("Camera Controllers/Camera Manager")] // Adds to component menu
[DefaultExecutionOrder(-100)] // Ensures this runs before other scripts that might need camera state
public class CameraManager : MonoBehaviour
{
    [Header("Core Setup")]
    [Tooltip("The camera this manager will control. Usually the Main Camera in the scene.")]
    public Camera mainCamera;
    [Tooltip("The primary target Transform for camera controllers (e.g., player character).")]
    public Transform cameraTarget;

    [Header("Controllers")]
    [Tooltip("List of all BaseCameraController components found on this GameObject.")]
    // This array is populated automatically in Awake, but shown in Inspector for overview.
    public BaseCameraController[] cameraControllers;

    [Tooltip("The index of the currently active camera controller.")]
    [SerializeField] private int _activeControllerIndex = 0;
    private ICameraController _activeController;

    /// <summary>
    /// Public property to access the currently active camera controller.
    /// </summary>
    public ICameraController ActiveController => _activeController;

    void Awake()
    {
        // 1. Validate Camera Setup
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Try to find the main camera if not assigned
            if (mainCamera == null)
            {
                Debug.LogError("CameraManager: No main camera found or assigned. Please assign a Camera to control or ensure one is tagged 'MainCamera'. Disabling CameraManager.", this);
                enabled = false;
                return;
            }
        }

        // 2. Validate Target Setup
        if (cameraTarget == null)
        {
            Debug.LogWarning("CameraManager: No camera target assigned. Some controllers may not function correctly.", this);
            // We won't disable the manager, as some controllers might not need a target.
        }

        // 3. Initialize all attached camera controllers
        InitializeControllers();
        
        // 4. Set the initial active controller
        SetController(_activeControllerIndex);
    }

    /// <summary>
    /// Gathers all BaseCameraController components attached to this GameObject,
    /// initializes them, and ensures they are initially deactivated.
    /// </summary>
    private void InitializeControllers()
    {
        // Get all components that derive from BaseCameraController on this GameObject.
        cameraControllers = GetComponents<BaseCameraController>();

        if (cameraControllers == null || cameraControllers.Length == 0)
        {
            Debug.LogError("CameraManager: No BaseCameraController components found on this GameObject. Please add some camera controller scripts (e.g., FollowTargetCameraController, OrbitCameraController) to this GameObject.", this);
            enabled = false;
            return;
        }

        // Initialize each controller with the main camera and target, then deactivate it.
        // Only the active controller will be enabled by SetController().
        foreach (var controller in cameraControllers)
        {
            controller.Initialize(mainCamera, cameraTarget);
            controller.OnDeactivate(); // Ensure they are all off initially
        }
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// This is the ideal place for camera logic to ensure all game objects
    /// have completed their movement for the current frame.
    /// </summary>
    void LateUpdate()
    {
        // If an active controller exists, instruct it to update the camera.
        _activeController?.UpdateCamera(Time.deltaTime);
    }

    /// <summary>
    /// Switches the active camera controller using its index in the `cameraControllers` array.
    /// </summary>
    /// <param name="index">The zero-based index of the controller to activate.</param>
    public void SetController(int index)
    {
        if (index < 0 || index >= cameraControllers.Length)
        {
            Debug.LogWarning($"CameraManager: Controller index {index} out of bounds. No controller switch performed.", this);
            return;
        }

        // Deactivate the currently active controller if one exists.
        _activeController?.OnDeactivate();

        // Set the new active controller.
        _activeControllerIndex = index;
        _activeController = cameraControllers[_activeControllerIndex];

        // Activate the new controller.
        _activeController.OnActivate();
        Debug.Log($"CameraManager: Switched to controller: {_activeController.GetType().Name}", this);
    }

    /// <summary>
    /// Switches the active camera controller using a direct reference to an ICameraController instance.
    /// </summary>
    /// <param name="newController">The ICameraController instance to set as active.</param>
    public void SetController(ICameraController newController)
    {
        if (newController == null)
        {
            Debug.LogWarning("CameraManager: Cannot set null controller.", this);
            return;
        }

        // Find the index of the provided controller in our array.
        int index = System.Array.IndexOf(cameraControllers, newController as BaseCameraController);
        if (index == -1)
        {
            Debug.LogWarning($"CameraManager: The provided controller '{newController.GetType().Name}' is not managed by this CameraManager. Ensure it's attached to the same GameObject.", this);
            return;
        }
        
        // Use the index-based setter to perform the switch.
        SetController(index);
    }

    /// <summary>
    /// Example usage: Switch controllers using keyboard input.
    /// This method demonstrates how to integrate controller switching into your game logic.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Press '1' to activate the first controller
        {
            SetController(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Press '2' to activate the second controller
        {
            SetController(1);
        }
        // Add more KeyCode checks for more controllers as needed.
    }

    // You can also add methods to get specific controllers by type if needed, e.g.:
    // public T GetController<T>() where T : BaseCameraController
    // {
    //     foreach (var controller in cameraControllers)
    //     {
    //         if (controller is T specificController)
    //         {
    //             return specificController;
    //         }
    //     }
    //     return null;
    // }
}
```

---

### How to Use in Unity (Step-by-Step):

1.  **Create a C# Script:** Create a new C# script named `CameraControllerPattern` (or simply copy-paste the entire code above into one file) and save it in your Unity project's `Assets` folder.
2.  **Create a Camera Rig:** In your Unity scene, create an Empty GameObject (right-click in Hierarchy -> Create Empty). Rename it to `CameraRig` (or `CameraManager`). This GameObject will be responsible for managing your camera.
3.  **Attach `CameraManager`:** Drag and drop the `CameraControllerPattern` script onto the `CameraRig` GameObject in the Hierarchy, or add it as a component via the Inspector (`Add Component -> Camera Manager`).
4.  **Attach Concrete Controllers:** Now, add the specific camera controller scripts to the *same* `CameraRig` GameObject:
    *   `Add Component -> Camera Controllers -> Follow Target Camera`
    *   `Add Component -> Camera Controllers -> Orbit Camera`
    You will see these appear as separate components below the `CameraManager`.
5.  **Assign Camera and Target:**
    *   In the Inspector, select your `CameraRig` GameObject.
    *   Drag your `Main Camera` (from the Hierarchy) to the `Main Camera` slot in the `CameraManager` component.
    *   Create a simple `Cube` or `Sphere` (GameObject -> 3D Object -> Cube) in your scene. This will be your player stand-in. Rename it "Player".
    *   Drag the "Player" GameObject (from the Hierarchy) to the `Camera Target` slot in the `CameraManager` component.
6.  **Adjust Controller Settings:** Select `CameraRig` again. You'll see the `Follow Target Camera` and `Orbit Camera` components. Adjust their public parameters (e.g., `offset`, `followSpeed`, `distance`, `height`, `sensitivity`) directly in the Inspector to fine-tune their behavior.
7.  **Run the Scene:**
    *   Start your game.
    *   The `FollowTargetCameraController` will be active by default (index 0).
    *   Move your "Player" Cube around (e.g., by creating a simple script to move it with arrow keys) and observe the camera following.
    *   Press `1` on your keyboard to explicitly activate the `FollowTargetCameraController`.
    *   Press `2` on your keyboard to activate the `OrbitCameraController`. While `OrbitCameraController` is active, hold down the **Right Mouse Button** and move your mouse to orbit around the "Player".
    *   You can easily add more controllers (e.g., a fixed camera for a specific view) and extend the `CameraManager.Update()` method to switch to them with different key presses.

This setup creates a flexible and organized camera system that adheres to the CameraControllerPattern, making your camera management much more robust and adaptable.