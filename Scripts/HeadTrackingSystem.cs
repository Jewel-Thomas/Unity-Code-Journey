// Unity Design Pattern Example: HeadTrackingSystem
// This script demonstrates the HeadTrackingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The HeadTrackingSystem design pattern in Unity provides a robust, extensible way to manage and access head tracking data. It abstracts the source of the tracking (e.g., VR headset, webcam, simulation) behind a unified interface, allowing other parts of your application to simply request "the current head pose" without caring about the underlying implementation.

This approach offers several benefits:
1.  **Flexibility:** Easily swap between different head tracking sources (VR, simulation, future custom solutions) without modifying consuming code.
2.  **Modularity:** Each tracking source is a self-contained module.
3.  **Testability:** Simulate head movement for testing without needing physical hardware.
4.  **Centralization:** All head tracking logic and current pose data are managed in one place.
5.  **Extensibility:** Add new tracking sources by simply implementing the `IHeadTracker` interface.

---

### HeadTrackingSystem Pattern Breakdown:

1.  **`HeadPose` Struct:** A simple data structure to hold the current position and rotation of the head.
2.  **`IHeadTracker` Interface:** Defines the contract for any class that wants to provide head tracking data. It specifies methods like `GetCurrentHeadPose()`, `Initialize()`, `Deinitialize()`, and a property `IsTracking`.
3.  **Concrete `IHeadTracker` Implementations:**
    *   **`SimulatedHeadTracker`:** An example implementation that provides head tracking data based on keyboard input (WASD/QE) for development and debugging.
    *   **`VRHeadTracker`:** An example implementation that fetches actual head tracking data from a connected VR headset using Unity's XR input system.
    *   (You could add `WebcamHeadTracker`, `ARKitHeadTracker`, etc., by implementing `IHeadTracker`).
4.  **`HeadTrackingSystem` (Singleton):** This is the core manager.
    *   It's a **Singleton** to ensure there's only one instance globally accessible.
    *   It holds references to various `IHeadTracker` implementations.
    *   It has a method (`SetTrackingMode`) to switch between different active trackers.
    *   It exposes a `CurrentHeadPose` property and an `OnHeadPoseUpdated` event, allowing other scripts to get the latest head data or react to changes.
    *   It handles the polling of the active tracker and decides when to broadcast updates based on configured thresholds.

---

### How to use this script in your Unity project:

1.  Create a new C# script named `HeadTrackingSystem.cs` in your Unity project.
2.  Copy and paste the entire code below into this file.
3.  Create an empty GameObject in your scene (e.g., named `_Managers` or `HeadTrackingSystem`).
4.  Add the `HeadTrackingSystem` component to this GameObject.
5.  Also, add the `VRHeadTracker` and `SimulatedHeadTracker` components to the *same* GameObject.
6.  In the `HeadTrackingSystem` Inspector, you will see fields for `Vr Tracker` and `Simulated Tracker`. Drag the respective components from the same GameObject onto these fields.
7.  Set the `Initial Tracking Mode` in the Inspector (e.g., `Simulated` for testing in editor, `VR` for a VR build).
8.  Now, any other script can access the head tracking data using `HeadTrackingSystem.Instance.CurrentHeadPose` or subscribe to `HeadTrackingSystem.Instance.OnHeadPoseUpdated`.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .FirstOrDefault()
using UnityEngine.XR; // Required for XR input and tracking

/// <summary>
/// HeadPose struct: Represents the position and rotation of the head.
/// </summary>
public struct HeadPose
{
    public Vector3 Position;
    public Quaternion Rotation;

    public HeadPose(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    /// <summary>
    /// A default, uninitialized head pose.
    /// </summary>
    public static HeadPose Default => new HeadPose(Vector3.zero, Quaternion.identity);

    /// <summary>
    /// Checks if this HeadPose has significantly changed compared to another.
    /// </summary>
    /// <param name="other">The other HeadPose to compare against.</param>
    /// <param name="posThreshold">Minimum distance change to be considered significant.</param>
    /// <param name="rotThreshold">Minimum angle change (degrees) to be considered significant.</param>
    /// <returns>True if the pose has changed beyond the given thresholds, false otherwise.</returns>
    public bool HasChanged(HeadPose other, float posThreshold, float rotThreshold)
    {
        return Vector3.Distance(Position, other.Position) > posThreshold ||
               Quaternion.Angle(Rotation, other.Rotation) > rotThreshold;
    }

    public override string ToString()
    {
        return $"Pos: {Position}, Rot: {Rotation.eulerAngles}";
    }
}

/// <summary>
/// IHeadTracker interface: Defines the contract for any head tracking source.
/// This is the core of the HeadTrackingSystem pattern, allowing different
/// tracking implementations to be used interchangeably.
/// </summary>
public interface IHeadTracker
{
    /// <summary>
    /// Gets whether the tracker is currently active and providing valid data.
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// Retrieves the current head pose from this tracking source.
    /// </summary>
    /// <returns>The latest HeadPose data.</returns>
    HeadPose GetCurrentHeadPose();

    /// <summary>
    /// Called when this tracker becomes the active tracker in the system.
    /// Use this for setup (e.g., enabling XR, initializing devices).
    /// </summary>
    void Initialize();

    /// <summary>
    /// Called when this tracker is no longer the active tracker.
    /// Use this for cleanup (e.g., disabling XR, releasing resources).
    /// </summary>
    void Deinitialize();
}

/// <summary>
/// SimulatedHeadTracker: An IHeadTracker implementation for development and testing.
/// Provides head tracking data based on keyboard input.
/// </summary>
[AddComponentMenu("Head Tracking System/Simulated Head Tracker")]
public class SimulatedHeadTracker : MonoBehaviour, IHeadTracker
{
    [Header("Simulated Head Tracker Settings")]
    [Tooltip("Movement speed for WASD/Arrow keys.")]
    [SerializeField] private float moveSpeed = 1f;
    [Tooltip("Rotation speed for Q/E keys.")]
    [SerializeField] private float rotateSpeed = 90f;

    private Vector3 currentPosition = Vector3.zero;
    private Quaternion currentRotation = Quaternion.identity;

    // Simulated tracker is always "tracking" when active.
    public bool IsTracking => true;

    /// <summary>
    /// Gets the current simulated head pose, updated by keyboard input.
    /// </summary>
    public HeadPose GetCurrentHeadPose()
    {
        HandleInput();
        return new HeadPose(currentPosition, currentRotation);
    }

    /// <summary>
    /// Processes keyboard input to update the simulated head position and rotation.
    /// </summary>
    private void HandleInput()
    {
        if (!enabled) return; // Only process input if component is enabled

        // Positional movement (WASD / Arrows for XZ, Space/Ctrl for Y)
        float inputX = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float inputY = 0f;
        float inputZ = Input.GetAxis("Vertical");   // W/S or Up/Down

        if (Input.GetKey(KeyCode.Space)) inputY = 1f;
        if (Input.GetKey(KeyCode.LeftControl)) inputY = -1f;

        Vector3 moveDelta = new Vector3(inputX, inputY, inputZ) * moveSpeed * Time.deltaTime;
        currentPosition += currentRotation * moveDelta; // Move relative to current facing

        // Rotational movement (Q/E for yaw)
        float rotateYaw = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateYaw = -1f;
        if (Input.GetKey(KeyCode.E)) rotateYaw = 1f;

        Quaternion yawDelta = Quaternion.Euler(0, rotateYaw * rotateSpeed * Time.deltaTime, 0);
        currentRotation = currentRotation * yawDelta;
    }

    /// <summary>
    /// Initializes the simulated tracker, resetting pose to default.
    /// </summary>
    public void Initialize()
    {
        Debug.Log("SimulatedHeadTracker Initialized. Use WASD/Arrows for movement, Q/E for rotation.");
        currentPosition = Vector3.zero;
        currentRotation = Quaternion.identity;
    }

    /// <summary>
    /// Deinitializes the simulated tracker.
    /// </summary>
    public void Deinitialize()
    {
        Debug.Log("SimulatedHeadTracker Deinitialized.");
    }
}

/// <summary>
/// VRHeadTracker: An IHeadTracker implementation that fetches head tracking data
/// from a connected VR headset using Unity's XR Input System.
/// </summary>
[AddComponentMenu("Head Tracking System/VR Head Tracker")]
public class VRHeadTracker : MonoBehaviour, IHeadTracker
{
    private bool _isTracking = false;
    private List<InputDevice> inputDevices = new List<InputDevice>(); // Cache for XR Input Devices

    // Indicates if VR tracking is currently active and available.
    public bool IsTracking => _isTracking;

    /// <summary>
    /// Retrieves the current head pose from the VR headset.
    /// Prioritizes the newer InputDevices API, falls back to InputTracking if needed.
    /// </summary>
    public HeadPose GetCurrentHeadPose()
    {
        if (!_isTracking) return HeadPose.Default;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        // Try to get head tracking data from XR Input Devices API (recommended)
        InputDevice headDevice = FindHeadDevice();
        if (headDevice.isValid)
        {
            if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                return new HeadPose(position, rotation);
            }
        }

        // Fallback: If InputDevices didn't work or for older XR setups, use InputTracking (deprecated but still functional)
        if (XRSettings.enabled)
        {
            // InputTracking provides the local position/rotation of the head relative to the tracking origin.
            return new HeadPose(InputTracking.GetLocalPosition(XRNode.Head), InputTracking.GetLocalRotation(XRNode.Head));
        }

        return HeadPose.Default;
    }

    /// <summary>
    /// Finds the primary head-mounted device (HMD) among connected XR input devices.
    /// </summary>
    private InputDevice FindHeadDevice()
    {
        inputDevices.Clear(); // Clear the list before populating
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, inputDevices);
        return inputDevices.FirstOrDefault(); // Return the first one found, or a default invalid device.
    }

    /// <summary>
    /// Initializes the VR tracker, checking if XR is enabled and active.
    /// </summary>
    public void Initialize()
    {
        Debug.Log("VRHeadTracker Initialized. Checking XR status...");
        // Check if XR is generally enabled and an XR device is active.
        _isTracking = XRSettings.enabled && XRSettings.isDeviceActive;

        if (_isTracking)
        {
            Debug.Log($"VRHeadTracker: XR device active: {XRSettings.loadedDeviceName}. Tracking will commence.");
        }
        else
        {
            Debug.LogWarning("VRHeadTracker: XR is not enabled or no device is active. No VR tracking data will be available. Please ensure Project Settings -> XR Plug-in Management is configured correctly.");
        }
    }

    /// <summary>
    /// Deinitializes the VR tracker.
    /// </summary>
    public void Deinitialize()
    {
        Debug.Log("VRHeadTracker Deinitialized.");
        _isTracking = false;
    }
}

/// <summary>
/// HeadTrackingSystem: The central manager (Singleton) for head tracking.
/// It provides a unified interface to access head pose data, abstracting the
/// underlying tracking mechanism. It allows switching between different
/// IHeadTracker implementations and notifies subscribers of pose updates.
/// </summary>
[AddComponentMenu("Head Tracking System/Head Tracking System")]
public class HeadTrackingSystem : MonoBehaviour
{
    // Singleton pattern: provides a global access point to the system.
    public static HeadTrackingSystem Instance { get; private set; }

    /// <summary>
    /// Defines the available head tracking modes.
    /// </summary>
    public enum HeadTrackerMode { None, VR, Simulated }

    [Header("Configuration")]
    [Tooltip("The initial head tracking mode to use when the system starts.")]
    [SerializeField] private HeadTrackerMode initialTrackingMode = HeadTrackerMode.Simulated;
    [Tooltip("How frequently (in seconds) the system checks for head pose updates. Set to 0 for every frame.")]
    [SerializeField] private float updateInterval = 0f;
    [Tooltip("Minimum distance (in meters) the head position must change to trigger an OnHeadPoseUpdated event.")]
    [SerializeField] private float positionChangeThreshold = 0.001f; // 1mm
    [Tooltip("Minimum angle (in degrees) the head rotation must change to trigger an OnHeadPoseUpdated event.")]
    [SerializeField] private float rotationChangeThreshold = 0.1f;   // 0.1 degrees

    [Header("Tracker References")]
    [Tooltip("Reference to the VRHeadTracker component on this GameObject.")]
    [SerializeField] private VRHeadTracker vrTracker;
    [Tooltip("Reference to the SimulatedHeadTracker component on this GameObject.")]
    [SerializeField] private SimulatedHeadTracker simulatedTracker;

    private IHeadTracker currentTracker;
    private HeadPose _currentHeadPose = HeadPose.Default;
    private float lastUpdateTime;

    /// <summary>
    /// Gets the current head pose (position and rotation).
    /// This property provides direct access to the latest known head pose.
    /// </summary>
    public HeadPose CurrentHeadPose
    {
        get { return _currentHeadPose; }
        private set { _currentHeadPose = value; }
    }

    /// <summary>
    /// Event triggered whenever the head pose is updated and has changed significantly.
    /// Subscribers can use this to react to head movements.
    /// </summary>
    public event Action<HeadPose> OnHeadPoseUpdated;

    private void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple HeadTrackingSystem instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Optionally, make the system persist across scene loads.
        // Uncomment the line below if you want the HeadTrackingSystem to live throughout your application.
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Ensure tracker references are set, attempting to GetComponent if not assigned in Inspector.
        if (vrTracker == null) vrTracker = GetComponent<VRHeadTracker>();
        if (simulatedTracker == null) simulatedTracker = GetComponent<SimulatedHeadTracker>();

        if (vrTracker == null) Debug.LogError("HeadTrackingSystem: VRHeadTracker component not found on this GameObject. VR tracking will not be available.");
        if (simulatedTracker == null) Debug.LogError("HeadTrackingSystem: SimulatedHeadTracker component not found on this GameObject. Simulated tracking will not be available.");

        // Set the initial tracking mode.
        SetTrackingMode(initialTrackingMode);
        lastUpdateTime = Time.time;
    }

    private void OnDisable()
    {
        // Deinitialize the current tracker when the system is disabled.
        if (currentTracker != null)
        {
            currentTracker.Deinitialize();
        }
    }

    private void OnDestroy()
    {
        // Clear the singleton instance reference if this instance is destroyed.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // If no tracker is active or tracking, reset the current pose and return.
        if (currentTracker == null || !currentTracker.IsTracking)
        {
            // Only update if current pose is not already default to avoid unnecessary event invokes.
            if (_currentHeadPose.Position != HeadPose.Default.Position || _currentHeadPose.Rotation != HeadPose.Default.Rotation)
            {
                 CurrentHeadPose = HeadPose.Default; // Reset if no valid tracker
                 OnHeadPoseUpdated?.Invoke(CurrentHeadPose);
            }
            return;
        }

        // Check if enough time has passed for an update, or if updateInterval is 0 (every frame).
        if (updateInterval <= 0f || Time.time - lastUpdateTime >= updateInterval)
        {
            HeadPose newPose = currentTracker.GetCurrentHeadPose();

            // Only update and notify if the pose has changed significantly based on thresholds.
            if (newPose.HasChanged(CurrentHeadPose, positionChangeThreshold, rotationChangeThreshold))
            {
                CurrentHeadPose = newPose;
                OnHeadPoseUpdated?.Invoke(CurrentHeadPose);
            }
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Changes the active head tracking mode.
    /// This method manages the lifecycle of the IHeadTracker implementations.
    /// </summary>
    /// <param name="mode">The desired HeadTrackerMode.</param>
    public void SetTrackingMode(HeadTrackerMode mode)
    {
        // Deinitialize the previously active tracker, if any.
        if (currentTracker != null)
        {
            currentTracker.Deinitialize();
        }

        // Ensure all MonoBehaviour-based trackers are disabled first to avoid multiple updates.
        if (vrTracker != null) vrTracker.enabled = false;
        if (simulatedTracker != null) simulatedTracker.enabled = false;

        // Select and initialize the new active tracker based on the chosen mode.
        switch (mode)
        {
            case HeadTrackerMode.VR:
                if (vrTracker != null)
                {
                    currentTracker = vrTracker;
                    vrTracker.enabled = true; // Enable the MonoBehaviour component
                    Debug.Log($"HeadTrackingSystem: Switched to VR Head Tracker. XR Enabled: {XRSettings.enabled}, Device Active: {XRSettings.isDeviceActive}");
                }
                else
                {
                    Debug.LogWarning("VRHeadTracker not available. Falling back to Simulated mode.");
                    currentTracker = simulatedTracker; // Fallback
                    if (simulatedTracker != null) simulatedTracker.enabled = true;
                }
                break;
            case HeadTrackerMode.Simulated:
                if (simulatedTracker != null)
                {
                    currentTracker = simulatedTracker;
                    simulatedTracker.enabled = true; // Enable the MonoBehaviour component
                    Debug.Log("HeadTrackingSystem: Switched to Simulated Head Tracker.");
                }
                else
                {
                    Debug.LogError("SimulatedHeadTracker not available. Cannot switch to Simulated mode.");
                    currentTracker = null;
                }
                break;
            case HeadTrackerMode.None:
            default:
                currentTracker = null;
                Debug.Log("HeadTrackingSystem: No head tracking mode selected (None).");
                break;
        }

        // Initialize the newly selected tracker.
        if (currentTracker != null)
        {
            currentTracker.Initialize();
        }

        // Force an initial pose update and event notification after mode change.
        if (currentTracker != null && currentTracker.IsTracking)
        {
            CurrentHeadPose = currentTracker.GetCurrentHeadPose();
            Debug.Log($"Initial Head Pose: {CurrentHeadPose}");
        }
        else
        {
            CurrentHeadPose = HeadPose.Default;
            Debug.Log("HeadTrackingSystem: No active tracker, current pose reset to default.");
        }
        OnHeadPoseUpdated?.Invoke(CurrentHeadPose); // Notify subscribers of initial state or reset
    }

    /// <summary>
    /// Provides direct access to the HeadPose. Use CurrentHeadPose property for most cases.
    /// This method is primarily for consistency with the IHeadTracker interface's GetCurrentHeadPose naming.
    /// </summary>
    /// <returns>The current HeadPose.</returns>
    public HeadPose GetHeadPose()
    {
        return CurrentHeadPose;
    }
}

/*
/// --- EXAMPLE USAGE IN ANOTHER SCRIPT ---
///
/// You would create a separate script (e.g., 'PlayerHeadController.cs')
/// and attach it to a GameObject (e.g., your player character's head or camera).

using UnityEngine;

public class PlayerHeadController : MonoBehaviour
{
    [Tooltip("Apply head position updates to this transform.")]
    [SerializeField] private Transform headTarget;
    [Tooltip("Offset to apply to the head position from the tracking system.")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [Tooltip("Offset to apply to the head rotation from the tracking system.")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    void OnEnable()
    {
        // Check if the HeadTrackingSystem instance exists before subscribing
        if (HeadTrackingSystem.Instance != null)
        {
            HeadTrackingSystem.Instance.OnHeadPoseUpdated += OnHeadPoseUpdated;
            Debug.Log("PlayerHeadController subscribed to HeadPose updates.");
        }
        else
        {
            Debug.LogWarning("HeadTrackingSystem instance not found. PlayerHeadController will not receive head pose updates.");
        }

        if (headTarget == null)
        {
            headTarget = this.transform; // Default to this GameObject's transform
            Debug.LogWarning("PlayerHeadController: Head Target not assigned. Defaulting to this GameObject's transform.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks or errors
        if (HeadTrackingSystem.Instance != null)
        {
            HeadTrackingSystem.Instance.OnHeadPoseUpdated -= OnHeadPoseUpdated;
            Debug.Log("PlayerHeadController unsubscribed from HeadPose updates.");
        }
    }

    /// <summary>
    /// Callback method invoked when the HeadTrackingSystem reports a new head pose.
    /// </summary>
    /// <param name="newPose">The latest HeadPose data.</param>
    private void OnHeadPoseUpdated(HeadPose newPose)
    {
        if (headTarget != null)
        {
            // Apply the new position and rotation, incorporating any offsets.
            headTarget.localPosition = newPose.Position + positionOffset;
            headTarget.localRotation = newPose.Rotation * Quaternion.Euler(rotationOffset);

            // Example: Debugging the pose
            // Debug.Log($"Player Head Updated: Pos={headTarget.localPosition}, Rot={headTarget.localRotation.eulerAngles}");
        }
    }

    /// <summary>
    /// You could also get the pose on demand (e.g., in LateUpdate for camera).
    /// </summary>
    void LateUpdate()
    {
        // If not subscribed to events, you could poll the system:
        // if (HeadTrackingSystem.Instance != null && HeadTrackingSystem.Instance.CurrentHeadPose.HasChanged(...))
        // {
        //     HeadPose currentPose = HeadTrackingSystem.Instance.CurrentHeadPose;
        //     headTarget.localPosition = currentPose.Position + positionOffset;
        //     headTarget.localRotation = currentPose.Rotation * Quaternion.Euler(rotationOffset);
        // }
    }

    // Example of dynamically changing the tracking mode at runtime
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && HeadTrackingSystem.Instance != null)
        {
            HeadTrackingSystem.Instance.SetTrackingMode(HeadTrackingSystem.HeadTrackerMode.Simulated);
            Debug.Log("Switched to Simulated Head Tracking Mode via F1.");
        }
        if (Input.GetKeyDown(KeyCode.F2) && HeadTrackingSystem.Instance != null)
        {
            HeadTrackingSystem.Instance.SetTrackingMode(HeadTrackingSystem.HeadTrackerMode.VR);
            Debug.Log("Switched to VR Head Tracking Mode via F2.");
        }
    }
}
*/
```