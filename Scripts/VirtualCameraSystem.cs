// Unity Design Pattern Example: VirtualCameraSystem
// This script demonstrates the VirtualCameraSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical C# Unity implementation of the 'VirtualCameraSystem' design pattern. It includes a central manager, an interface for virtual cameras, and several concrete virtual camera types, demonstrating smooth transitions and priority-based activation.

### `VirtualCameraSystem.cs`

This single script file contains all the necessary classes for the pattern. You can create a new C# script named `VirtualCameraSystem.cs` in your Unity project and paste the entire content below into it.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for LINQ extension methods like Where, OrderByDescending, FirstOrDefault

/// <summary>
/// C# Unity Example for the 'VirtualCameraSystem' Design Pattern.
///
/// This pattern allows for multiple "virtual cameras" (configurations for the main Unity Camera)
/// to exist in a scene. A central 'VirtualCameraSystem' then manages these virtual cameras,
/// deciding which one is currently "active" based on rules (e.g., priority) and applying its
/// settings to the *single* main Unity Camera.
///
/// This provides a flexible and modular way to manage camera behavior, enabling easy switching
/// between different camera perspectives (e.g., third-person, first-person, cutscene, fixed view, overhead)
/// without constantly instantiating/destroying camera objects or writing complex state machines
/// directly on the main camera.
///
/// Key Components:
/// 1. IVirtualCamera: An interface defining the contract for any virtual camera.
/// 2. VirtualCameraBase: An abstract MonoBehaviour that implements IVirtualCamera and handles
///    common logic like registration/deregistration with the system.
/// 3. Concrete Virtual Cameras (e.g., FollowVirtualCamera, StaticVirtualCamera, OrbitVirtualCamera):
///    Specific implementations that define how the main camera should behave (position, rotation, FOV).
/// 4. VirtualCameraSystem: The central manager (Singleton) that keeps track of all registered
///    virtual cameras, determines the active one, and applies its settings to the main Unity Camera.
/// </summary>

// --- 1. Define the 'Virtual Camera' Abstraction ---

/// <summary>
/// Interface for a Virtual Camera. All concrete virtual cameras should implement this.
/// This defines the contract for what a virtual camera can do and what properties it must expose.
/// </summary>
public interface IVirtualCamera
{
    /// <summary>
    /// Gets or sets the priority of this virtual camera.
    /// Higher priority cameras take precedence when multiple are active and eligible to drive the main camera.
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this virtual camera is currently considered "active"
    /// and eligible to drive the main Unity camera. This is often controlled by game logic.
    /// The VirtualCameraSystem will then select the highest priority 'IsActive' camera.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Applies the virtual camera's settings (position, rotation, FOV, etc.) to the main Unity camera.
    /// This method is called by the VirtualCameraSystem in its LateUpdate.
    /// </summary>
    /// <param name="mainCamera">The actual Unity Camera component to modify.</param>
    /// <param name="blendWeight">A normalized weight (0-1) to blend settings.
    /// 1 means fully apply this camera's settings. Used during transitions.</param>
    void ApplySettings(Camera mainCamera, float blendWeight = 1f);

    /// <summary>
    /// Called when this virtual camera becomes the active driver of the main Unity camera.
    /// Useful for camera-specific setup (e.g., locking cursor, playing a sound, enabling input).
    /// </summary>
    void OnActivated();

    /// <summary>
    /// Called when this virtual camera ceases to be the active driver of the main Unity camera.
    /// Useful for camera-specific cleanup (e.g., unlocking cursor, stopping sounds).
    /// </summary>
    void OnDeactivated();
}

// --- 2. Implement Concrete Virtual Cameras ---

/// <summary>
/// Base class for concrete MonoBehaviour-based Virtual Cameras.
/// This abstract class handles common functionality like automatic registration/deregistration
/// with the VirtualCameraSystem, and provides default implementations for activation/deactivation callbacks.
/// All specific virtual camera types will inherit from this.
/// </summary>
public abstract class VirtualCameraBase : MonoBehaviour, IVirtualCamera
{
    [Tooltip("The priority of this virtual camera. Higher values take precedence.")]
    [SerializeField] private int _priority = 0;
    public int Priority { get => _priority; set => _priority = value; }

    [Tooltip("Is this camera currently active and eligible to drive the main Unity camera?")]
    [SerializeField] private bool _isActive = false;
    public bool IsActive { get => _isActive; set => _isActive = value; }

    /// <summary>
    /// When this MonoBehaviour is enabled, it automatically registers itself with the VirtualCameraSystem.
    /// This makes it known to the system and eligible to become the active camera.
    /// </summary>
    protected virtual void OnEnable()
    {
        // Check if the singleton instance exists to avoid errors during scene loading/unloading.
        VirtualCameraSystem.Instance?.Register(this);
    }

    /// <summary>
    /// When this MonoBehaviour is disabled or destroyed, it automatically deregisters itself
    /// from the VirtualCameraSystem. This prevents the system from trying to control a non-existent camera.
    /// </summary>
    protected virtual void OnDisable()
    {
        VirtualCameraSystem.Instance?.Deregister(this);
    }

    /// <summary>
    /// Abstract method that concrete virtual cameras must implement.
    /// This is where the logic for positioning, rotating, and setting the FOV of the main camera goes.
    /// </summary>
    /// <param name="mainCamera">The actual Unity Camera component to modify.</param>
    /// <param name="blendWeight">A normalized weight (0-1) to blend settings. 1 means full influence.</param>
    public abstract void ApplySettings(Camera mainCamera, float blendWeight = 1f);

    /// <summary>
    /// Default implementation for activation. Can be overridden in derived classes for specific camera logic.
    /// </summary>
    public virtual void OnActivated()
    {
        Debug.Log($"<color=cyan>Virtual Camera '{name}' ACTIVATED!</color>");
        // Example: You might trigger a visual effect or UI overlay here.
    }

    /// <summary>
    /// Default implementation for deactivation. Can be overridden in derived classes for specific camera logic.
    /// </summary>
    public virtual void OnDeactivated()
    {
        Debug.Log($"<color=magenta>Virtual Camera '{name}' DEACTIVATED!</color>");
        // Example: You might stop a sound effect or hide a UI element here.
    }
}

/// <summary>
/// A concrete virtual camera that acts as a simple third-person follower.
/// It follows a target with a fixed offset and smoothly looks at it.
/// </summary>
public class FollowVirtualCamera : VirtualCameraBase
{
    [Tooltip("The transform this camera should follow and look at.")]
    [SerializeField] private Transform _targetToFollow;
    [Tooltip("The offset from the target's position.")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 3, -7);
    [Tooltip("How smoothly the camera follows the target's position.")]
    [SerializeField] private float _followDamping = 5f;
    [Tooltip("How smoothly the camera rotates to look at the target.")]
    [SerializeField] private float _lookAtDamping = 5f;
    [Tooltip("The field of view for this camera.")]
    [SerializeField] private float _fieldOfView = 60f;

    // Public setter to allow other scripts to change the target dynamically.
    public Transform TargetToFollow { get => _targetToFollow; set => _targetToFollow = value; }

    /// <summary>
    /// Implements the core logic for this specific camera type.
    /// It calculates the desired position and rotation based on the target and applies it to the main camera.
    /// </summary>
    /// <param name="mainCamera">The actual Unity Camera component to modify.</param>
    /// <param name="blendWeight">Weight (0-1) for blending during transitions.</param>
    public override void ApplySettings(Camera mainCamera, float blendWeight = 1f)
    {
        if (_targetToFollow == null) return; // Cannot apply settings without a target.

        // Calculate the desired world position of the camera by offsetting it from the target
        // and transforming that offset by the target's rotation (so the offset moves with the target).
        Vector3 desiredPosition = _targetToFollow.position + _targetToFollow.TransformDirection(_offset);

        // Calculate the desired rotation to look at the target's position.
        Quaternion desiredRotation = Quaternion.LookRotation(_targetToFollow.position - desiredPosition);

        // Apply blending and smoothing (using Lerp/Slerp towards the desired values)
        // The `blendWeight` influences how strongly this camera's settings are applied.
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, Time.deltaTime * _followDamping * blendWeight);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, desiredRotation, Time.deltaTime * _lookAtDamping * blendWeight);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, _fieldOfView, Time.deltaTime * _followDamping * blendWeight);
    }
}

/// <summary>
/// A concrete virtual camera that stays in a fixed position and rotation,
/// typically used for cutscenes, security cameras, or fixed viewpoints.
/// It can optionally look at a specific target.
/// </summary>
public class StaticVirtualCamera : VirtualCameraBase
{
    [Tooltip("The fixed world position for this camera.")]
    [SerializeField] private Vector3 _fixedPosition = new Vector3(0, 5, -10);
    [Tooltip("The fixed Euler angles for this camera's rotation (pitch, yaw, roll).")]
    [SerializeField] private Vector3 _fixedRotation = new Vector3(20, 0, 0); // Euler angles
    [Tooltip("The field of view for this camera.")]
    [SerializeField] private float _fieldOfView = 60f;

    [Tooltip("Optional: If set, the camera will continuously look at this target, overriding _fixedRotation.")]
    [SerializeField] private Transform _lookAtTarget;

    /// <summary>
    /// Implements the core logic for this specific camera type.
    /// It sets the main camera's position and rotation to fixed values, with optional target-looking.
    /// </summary>
    /// <param name="mainCamera">The actual Unity Camera component to modify.</param>
    /// <param name="blendWeight">Weight (0-1) for blending during transitions.</param>
    public override void ApplySettings(Camera mainCamera, float blendWeight = 1f)
    {
        // Calculate desired rotation. If a target is provided, look at it; otherwise, use fixed rotation.
        Quaternion desiredRotation;
        if (_lookAtTarget != null)
        {
            desiredRotation = Quaternion.LookRotation(_lookAtTarget.position - _fixedPosition);
        }
        else
        {
            desiredRotation = Quaternion.Euler(_fixedRotation);
        }

        // Apply blending and smoothing. Using the VirtualCameraSystem's global transition speed for static cams.
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, _fixedPosition, Time.deltaTime * VirtualCameraSystem.Instance.TransitionSpeed * blendWeight);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, desiredRotation, Time.deltaTime * VirtualCameraSystem.Instance.TransitionSpeed * blendWeight);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, _fieldOfView, Time.deltaTime * VirtualCameraSystem.Instance.TransitionSpeed * blendWeight);
    }

    /// <summary>
    /// Overrides the base activation logic to add specific behavior for a static camera.
    /// </summary>
    public override void OnActivated()
    {
        base.OnActivated(); // Call the base implementation first for general logging.
        Debug.Log($"Static camera '{name}' is now active at {_fixedPosition}.");
        // Example: Play a specific ambient sound for this fixed view, or enable specific UI elements.
    }
}

/// <summary>
/// A concrete virtual camera that allows orbiting around a target using mouse input.
/// Similar to a FreeLook camera found in game engines like Unity's Cinemachine.
/// </summary>
public class OrbitVirtualCamera : VirtualCameraBase
{
    [Tooltip("The transform this camera should orbit around.")]
    [SerializeField] private Transform _targetToOrbit;
    [Tooltip("The distance from the target.")]
    [SerializeField] private float _distance = 5f;
    [Tooltip("The height offset from the target.")]
    [SerializeField] private float _height = 2f;
    [Tooltip("The current horizontal angle (yaw) around the target.")]
    [SerializeField] private float _currentYaw = 0f;
    [Tooltip("The current vertical angle (pitch) around the target.")]
    [SerializeField] private float _currentPitch = 20f;
    [Tooltip("Limits for the vertical pitch angle.")]
    [SerializeField] private Vector2 _pitchLimits = new Vector2(-60f, 80f); // Min/Max pitch in degrees
    [Tooltip("How quickly the camera responds to mouse input for orbiting.")]
    [SerializeField] private float _orbitSensitivity = 200f;
    [Tooltip("How smoothly the camera moves to its desired position/rotation.")]
    [SerializeField] private float _damping = 8f;
    [Tooltip("The field of view for this camera.")]
    [SerializeField] private float _fieldOfView = 60f;

    // Constants for Unity's old Input Manager axes.
    private const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";

    public Transform TargetToOrbit { get => _targetToOrbit; set => _targetToOrbit = value; }

    /// <summary>
    /// Implements the core logic for this specific camera type.
    /// It processes mouse input to orbit around a target and applies the resulting
    /// position and rotation to the main camera.
    /// </summary>
    /// <param name="mainCamera">The actual Unity Camera component to modify.</param>
    /// <param name="blendWeight">Weight (0-1) for blending during transitions.</param>
    public override void ApplySettings(Camera mainCamera, float blendWeight = 1f)
    {
        if (_targetToOrbit == null) return;

        // Only process input if this camera is the active one or has significant influence during a blend.
        // This prevents multiple orbit cameras from fighting for input.
        if (IsActive || blendWeight > 0.5f) // The threshold can be adjusted
        {
            // Process mouse input to update yaw and pitch angles.
            // Mouse X controls yaw (horizontal rotation), Mouse Y controls pitch (vertical rotation).
            _currentYaw += Input.GetAxis(MouseXInput) * _orbitSensitivity * Time.deltaTime;
            _currentPitch -= Input.GetAxis(MouseYInput) * _orbitSensitivity * Time.deltaTime; // Negative because Unity's Y-axis input is inverted for camera pitch.
            _currentPitch = Mathf.Clamp(_currentPitch, _pitchLimits.x, _pitchLimits.y); // Clamp pitch to limits.
        }

        // Calculate the desired rotation based on the current yaw and pitch.
        Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);

        // Determine the target's position, including a height offset.
        Vector3 targetPosition = _targetToOrbit.position + Vector3.up * _height;

        // Calculate the desired camera position by rotating an offset vector (0,0,-distance)
        // by the calculated rotation and adding it to the target's position.
        Vector3 desiredPosition = targetPosition + rotation * new Vector3(0, 0, -_distance);

        // Apply blending and smoothing (using Lerp/Slerp towards the desired values)
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, Time.deltaTime * _damping * blendWeight);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, rotation, Time.deltaTime * _damping * blendWeight);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, _fieldOfView, Time.deltaTime * _damping * blendWeight);
    }

    /// <summary>
    /// Overrides OnActivated to lock the cursor when this camera becomes active,
    /// which is typical for orbiting cameras to provide seamless mouse control.
    /// </summary>
    public override void OnActivated()
    {
        base.OnActivated();
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor to center of screen.
        Cursor.visible = false; // Hide the cursor.
    }

    /// <summary>
    /// Overrides OnDeactivated to unlock and show the cursor when this camera is no longer active.
    /// </summary>
    public override void OnDeactivated()
    {
        base.OnDeactivated();
        Cursor.lockState = CursorLockMode.None; // Unlock cursor.
        Cursor.visible = true; // Show the cursor.
    }
}


// --- 3. Create the `VirtualCameraSystem` (The Manager) ---

/// <summary>
/// The central manager for the Virtual Camera System.
/// This MonoBehaviour manages all registered IVirtualCamera instances,
/// determines which one should be active based on priority and IsActive status,
/// and smoothly drives the main Unity Camera.
/// </summary>
public class VirtualCameraSystem : MonoBehaviour
{
    // Singleton pattern for easy global access from other scripts (e.g., PlayerController, individual VirtualCameras).
    // Ensures there's only one instance of the camera system throughout the game.
    public static VirtualCameraSystem Instance { get; private set; }

    [Tooltip("The actual main Unity Camera component that this system will control. If not set, it tries to find Camera.main.")]
    [SerializeField] private Camera _mainCamera;
    [Tooltip("The speed at which the main camera transitions between different virtual cameras (how fast it blends).")]
    [SerializeField] private float _transitionSpeed = 8f;
    public float TransitionSpeed => _transitionSpeed; // Public getter for blend speeds in individual cameras.

    // A list to hold all virtual cameras that have registered themselves with this system.
    private List<IVirtualCamera> _registeredCameras = new List<IVirtualCamera>();

    // The currently active virtual camera that is driving the main Unity camera.
    private IVirtualCamera _currentActiveCamera;
    // The previously active virtual camera, used for smooth blending during transitions.
    private IVirtualCamera _previousActiveCamera;
    // A timer used to track the progress of the blend between cameras (0 to 1).
    private float _blendTimer;

    // Small epsilon value for float comparisons to account for floating point inaccuracies.
    private const float BLEND_THRESHOLD = 0.01f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern to ensure only one instance exists.
    /// Also initializes the main camera reference.
    /// </summary>
    void Awake()
    {
        // Enforce Singleton: If another instance already exists, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this; // Set this instance as the singleton.

        // If _mainCamera is not assigned in the Inspector, try to find the one tagged "MainCamera".
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("VirtualCameraSystem: Main Camera not found or assigned! Please assign it manually in the Inspector or ensure your main camera GameObject is tagged 'MainCamera'.", this);
            }
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Clears the singleton instance.
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers a virtual camera with the system. This camera will then be considered for activation.
    /// </summary>
    /// <param name="virtualCamera">The IVirtualCamera instance to register.</param>
    public void Register(IVirtualCamera virtualCamera)
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning("Attempted to register a null virtual camera.");
            return;
        }

        // Only add if not already present in the list.
        if (!_registeredCameras.Contains(virtualCamera))
        {
            _registeredCameras.Add(virtualCamera);
            Debug.Log($"Registered Virtual Camera: {((MonoBehaviour)virtualCamera).name}");
        }
    }

    /// <summary>
    /// Deregisters a virtual camera from the system. It will no longer be managed.
    /// </summary>
    /// <param name="virtualCamera">The IVirtualCamera instance to deregister.</param>
    public void Deregister(IVirtualCamera virtualCamera)
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning("Attempted to deregister a null virtual camera.");
            return;
        }

        if (_registeredCameras.Remove(virtualCamera))
        {
            Debug.Log($"Deregistered Virtual Camera: {((MonoBehaviour)virtualCamera).name}");
            // If the deregistered camera was the currently active one, we need to force a re-evaluation
            // to find a new active camera in the next LateUpdate.
            if (_currentActiveCamera == virtualCamera)
            {
                _currentActiveCamera = null;
            }
        }
    }

    /// <summary>
    /// LateUpdate is called once per frame, after all Update functions have been called.
    /// This is crucial for camera logic to ensure all other game objects have moved first,
    /// preventing camera jitter.
    /// </summary>
    void LateUpdate()
    {
        if (_mainCamera == null) return; // Cannot control camera if not assigned.

        // 1. Determine the highest priority active camera from all registered cameras.
        //    - Filter for cameras where 'IsActive' is true.
        //    - Order them by 'Priority' in descending order (highest priority first).
        //    - Take the first one (or null if no active cameras are found).
        IVirtualCamera newActiveCamera = _registeredCameras
            .Where(cam => cam.IsActive)
            .OrderByDescending(cam => cam.Priority)
            .FirstOrDefault();

        // 2. Handle camera transitions: Check if the active camera has changed.
        if (newActiveCamera != _currentActiveCamera)
        {
            // A new camera wants to become active, or the previous one is no longer eligible.
            if (_currentActiveCamera != null)
            {
                // Notify the old active camera that it's being deactivated.
                _currentActiveCamera.OnDeactivated();
                // Store the old camera for blending purposes.
                _previousActiveCamera = _currentActiveCamera;
            }
            else
            {
                // If there was no previous active camera, we don't need to blend from anything.
                _previousActiveCamera = null;
            }

            // Set the new current active camera.
            _currentActiveCamera = newActiveCamera;
            // Reset the blend timer to start a new transition.
            _blendTimer = 0f;

            if (_currentActiveCamera != null)
            {
                // Notify the new active camera that it's being activated.
                _currentActiveCamera.OnActivated();
            }
            else
            {
                // No active camera found, potentially inform the user or revert to a default state.
                Debug.LogWarning("VirtualCameraSystem: No active virtual camera found to drive the main camera!");
                // Optionally, disable the main camera or set it to a default position/FOV here.
            }
        }

        // 3. Apply settings from the active camera (with blending if a transition is in progress).
        if (_currentActiveCamera != null)
        {
            // Increment blend timer based on transition speed, clamped between 0 and 1.
            _blendTimer += Time.deltaTime * _transitionSpeed;
            float currentBlendWeight = Mathf.Clamp01(_blendTimer);

            // If we are still actively blending from a _previousActiveCamera (blendWeight < 1)
            // and the previous camera actually exists.
            if (_previousActiveCamera != null && currentBlendWeight < 1f - BLEND_THRESHOLD)
            {
                // Apply a portion of the previous camera's settings.
                _previousActiveCamera.ApplySettings(_mainCamera, 1f - currentBlendWeight);
                // Then overlay/blend the current camera's settings.
                _currentActiveCamera.ApplySettings(_mainCamera, currentBlendWeight);
            }
            else
            {
                // Blending is complete (or wasn't needed), apply current camera's settings fully.
                _currentActiveCamera.ApplySettings(_mainCamera);
                // Clear the previous camera reference as blending is done.
                _previousActiveCamera = null;
            }
        }
    }

    /// <summary>
    /// Public method to programmatically set a specific virtual camera as the active one.
    /// This is useful for game events (e.g., player enters a cutscene, objective starts).
    /// </summary>
    /// <param name="cameraToActivate">The IVirtualCamera instance to activate.</param>
    /// <param name="forceActivation">
    /// If true, this camera will be given a very high priority temporarily to ensure it takes over,
    /// overriding other cameras' priorities. Use with caution for cinematic or critical moments.
    /// Its original priority is *not* restored in this simple example. A more advanced system
    /// would save and restore original priorities.
    /// </param>
    public void ActivateCamera(IVirtualCamera cameraToActivate, bool forceActivation = false)
    {
        // Basic validation.
        if (cameraToActivate == null || !_registeredCameras.Contains(cameraToActivate))
        {
            Debug.LogWarning($"VirtualCameraSystem: Attempted to activate unregistered or null camera.");
            return;
        }

        // Deactivate all other cameras that are currently active.
        // This ensures only the requested camera (or the highest priority one if others remain active)
        // will be chosen by the system.
        foreach (var cam in _registeredCameras)
        {
            if (cam != cameraToActivate) // Don't deactivate the one we want to activate.
            {
                cam.IsActive = false;
            }
        }

        // Set the requested camera to active.
        cameraToActivate.IsActive = true;

        if (forceActivation)
        {
            // Boost its priority to ensure it is chosen as the absolute active camera.
            cameraToActivate.Priority = int.MaxValue;
        }

        Debug.Log($"VirtualCameraSystem: Requested activation of camera '{((MonoBehaviour)cameraToActivate).name}' (Force: {forceActivation}).");
    }
}


// --- 4. Example Player Controller (to demonstrate camera following) ---

/// <summary>
/// Simple player controller to move a character around and provide input
/// for switching between different virtual cameras.
/// This is just for demonstration purposes to show the camera system in action.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 180f;

    void Update()
    {
        // Basic player movement using horizontal and vertical input axes.
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        // Move the player relative to its own local space.
        transform.Translate(moveDirection * _moveSpeed * Time.deltaTime, Space.Self);

        // Rotate the player to face the direction of movement.
        // This is a simple rotation; in a real game, you might align to camera forward, etc.
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // --- Example: Switching cameras with keyboard input ---
        // These calls demonstrate how game logic can interact with the VirtualCameraSystem.

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Find and activate the FollowVirtualCamera.
            // Using FindObjectOfType is generally not recommended for performance in shipping games,
            // but is acceptable for a simple demonstration.
            // In a real project, you'd likely have references to your specific cameras (e.g., via a list, an enum, or public fields).
            var cam1 = FindObjectOfType<FollowVirtualCamera>();
            if (cam1 != null)
            {
                VirtualCameraSystem.Instance.ActivateCamera(cam1);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Find and activate the first StaticVirtualCamera found.
            var cam2 = FindObjectsOfType<StaticVirtualCamera>().FirstOrDefault();
            if (cam2 != null)
            {
                VirtualCameraSystem.Instance.ActivateCamera(cam2);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Find and activate the first OrbitVirtualCamera found.
            var cam3 = FindObjectsOfType<OrbitVirtualCamera>().FirstOrDefault();
            if (cam3 != null)
            {
                VirtualCameraSystem.Instance.ActivateCamera(cam3);
            }
        }
    }
}
```

---

### How to Implement and Use in Unity

Follow these steps to set up the example in your Unity project:

1.  **Create the Script:**
    *   In your Unity project, right-click in the Project window -> `Create` -> `C# Script`.
    *   Name it `VirtualCameraSystem`.
    *   Open the newly created script and paste the *entire* code block provided above, overwriting any default content. Save the script.

2.  **Scene Setup:**

    *   **Main Camera:**
        *   Ensure you have a `Main Camera` in your scene (Unity typically adds one by default).
        *   Verify that its GameObject is tagged `MainCamera` (you can check and set this in the Inspector at the top). This is the camera that the `VirtualCameraSystem` will actually control.

    *   **VirtualCameraSystem GameObject:**
        *   Create an empty GameObject: Right-click in the Hierarchy -> `Create Empty`.
        *   Rename it to `_GameManagers` (or `VirtualCameraManager`).
        *   Drag the `VirtualCameraSystem.cs` script from your Project window onto this `_GameManagers` GameObject in the Hierarchy to add it as a component.
        *   In the Inspector of `_GameManagers`, locate the `Virtual Camera System` component.
        *   Drag your `Main Camera` from the Hierarchy into the `Main Camera` slot of the `Virtual Camera System` component.
        *   Adjust `Transition Speed` as desired (e.g., `8` to `15` for a smooth, noticeable blend).

    *   **Player GameObject:**
        *   Create a simple player character: Right-click in the Hierarchy -> `3D Object` -> `Capsule`.
        *   Rename it to `Player`.
        *   Drag the `VirtualCameraSystem.cs` script from your Project window (yes, the same script file contains `PlayerController`) onto the `Player` GameObject to add the `Player Controller` component.

    *   **Create Virtual Camera Instances:**
        *   **FollowVirtualCamera (e.g., for general gameplay):**
            *   Create an empty GameObject: Right-click in the Hierarchy -> `Create Empty`.
            *   Rename it to `FollowCam_Gameplay`.
            *   Drag the `VirtualCameraSystem.cs` script onto this GameObject to add the `Follow Virtual Camera` component.
            *   In its Inspector, drag your `Player` GameObject into the `Target To Follow` slot.
            *   Set its `Priority` (e.g., `100`).
            *   **Crucially, check the `Is Active` checkbox** if you want this to be the initial camera when you start the game.

        *   **StaticVirtualCamera (e.g., for a cutscene or fixed view):**
            *   Create an empty GameObject: Right-click in the Hierarchy -> `Create Empty`.
            *   Rename it to `StaticCam_Cutscene`.
            *   Drag the `VirtualCameraSystem.cs` script onto this GameObject to add the `Static Virtual Camera` component.
            *   Adjust its `Fixed Position` and `Fixed Rotation` to a desired static viewpoint in your scene. For example, `Position: (0, 5, -10)`, `Rotation: (20, 0, 0)`.
            *   Optionally, drag your `Player` GameObject into the `Look At Target` slot if you want it to point at the player.
            *   Set its `Priority` (e.g., `50`). **Ensure `Is Active` is unchecked** by default.

        *   **OrbitVirtualCamera (e.g., for exploring an item or character):**
            *   Create an empty GameObject: Right-click in the Hierarchy -> `Create Empty`.
            *   Rename it to `OrbitCam_Inspect`.
            *   Drag the `VirtualCameraSystem.cs` script onto this GameObject to add the `Orbit Virtual Camera` component.
            *   Drag your `Player` GameObject into the `Target To Orbit` slot.
            *   Set its `Priority` (e.g., `75`). **Ensure `Is Active` is unchecked** by default.

    *   **Add some visual context:** Add a `3D Object` -> `Plane` at `(0,0,0)` for the player to stand on, and maybe a few cubes around to give depth to the camera views.

3.  **Run the Scene:**

    *   Press the `Play` button in Unity.
    *   The `VirtualCameraSystem` will automatically detect your `FollowCam_Gameplay` (if its `Is Active` is checked and it has the highest priority among active cameras) and use it to control the main camera.
    *   Use **WASD** to move the `Player` capsule. The camera will follow it.
    *   Press **1** on your keyboard to activate the `FollowCam_Gameplay`.
    *   Press **2** on your keyboard to activate the `StaticCam_Cutscene`. Observe the smooth transition to the fixed viewpoint.
    *   Press **3** on your keyboard to activate the `OrbitCam_Inspect`. Now, move your **mouse** to orbit around the `Player`. Notice how the mouse cursor is locked and hidden, then restored when switching away.

This setup demonstrates a practical and flexible camera system for your Unity projects, allowing you to easily define, manage, and transition between various camera behaviors.