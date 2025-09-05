// Unity Design Pattern Example: AdvancedCameraController
// This script demonstrates the AdvancedCameraController pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates an **Advanced Camera Controller** using the **Strategy Design Pattern**.

The core idea is to separate different camera behaviors (e.g., "orbiting around a target," "fixed behind the target," "first-person view") into individual "strategy" classes. The main `AdvancedCameraController` then acts as a context that holds a reference to the currently active strategy and delegates all camera update logic to it. This makes the camera system highly modular, extensible, and easy to manage, as you can dynamically switch between different camera behaviors without modifying the core controller logic.

---

### **AdvancedCameraController.cs**

To use this, create a new C# script named `AdvancedCameraController.cs` in your Unity project, copy the code below into it, and then follow the **Example Usage** instructions.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<T>
using System; // For [Serializable]

// ========================================================================================
// 1. ICameraStrategy Interface (The Strategy Pattern Interface)
//    Defines the contract for all camera behaviors.
// ========================================================================================
/// <summary>
/// Defines the interface for all camera movement and control strategies.
/// Each concrete camera behavior (e.g., orbit, first-person, fixed) will implement this.
/// </summary>
public interface ICameraStrategy
{
    /// <summary>
    /// Called once when the strategy is first initialized by the AdvancedCameraController.
    /// Use this to set up permanent references like the camera transform and target.
    /// </summary>
    /// <param name="cameraTransform">The transform of the camera GameObject.</param>
    /// <param name="target">The target transform the camera should follow/look at.</param>
    void Initialize(Transform cameraTransform, Transform target);

    /// <summary>
    /// Called when this strategy becomes the active camera strategy.
    /// Use this for setup specific to activation (e.g., resetting internal states, playing sounds).
    /// </summary>
    void Enter();

    /// <summary>
    /// Called when this strategy is no longer the active camera strategy.
    /// Use this for cleanup specific to deactivation (e.g., saving state, stopping effects).
    /// </summary>
    void Exit();

    /// <summary>
    /// Called every LateUpdate by the AdvancedCameraController to update the camera's position and rotation.
    /// All camera movement and input logic should reside here.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    void UpdateCamera(float deltaTime);
}


// ========================================================================================
// 2. Concrete Camera Strategies (Implementations of ICameraStrategy)
//    These classes define specific camera behaviors.
// ========================================================================================

/// <summary>
/// Implements a common third-person orbit camera with player input for rotation and zoom,
/// and basic collision avoidance to prevent the camera from going through walls.
/// </summary>
[System.Serializable] // Allows Unity to serialize this class in the Inspector
public class OrbitCameraStrategy : ICameraStrategy
{
    // References that will be initialized by the controller
    private Transform _cameraTransform;
    private Transform _target;

    [Header("Orbit Settings")]
    [Tooltip("Distance from the target's pivot point.")]
    public float Distance = 5.0f;
    [Tooltip("Vertical offset from the target's origin, determining the pivot height.")]
    public float HeightOffset = 1.5f;
    [Tooltip("How smoothly the camera interpolates to its desired position.")]
    public float PositionSmoothSpeed = 10.0f;
    [Tooltip("How smoothly the camera interpolates to its desired rotation.")]
    public float RotationSmoothSpeed = 10.0f;

    [Header("Input Sensitivity")]
    [Tooltip("How fast the camera rotates horizontally based on 'Mouse X' input.")]
    public float MouseXSensitivity = 3.0f;
    [Tooltip("How fast the camera rotates vertically based on 'Mouse Y' input.")]
    public float MouseYSensitivity = 3.0f;
    [Tooltip("Scroll sensitivity for zooming in and out.")]
    public float ZoomSensitivity = 5.0f;

    [Header("Angle & Distance Limits")]
    [Tooltip("Minimum vertical angle (pitch) the camera can look.")]
    public float MinPitch = -45.0f;
    [Tooltip("Maximum vertical angle (pitch) the camera can look.")]
    public float MaxPitch = 80.0f;
    [Tooltip("Minimum zoom distance from the target.")]
    public float MinDistance = 1.0f;
    [Tooltip("Maximum zoom distance from the target.")]
    public float MaxDistance = 10.0f;

    [Header("Collision Handling")]
    [Tooltip("Radius of the spherecast used to detect obstacles between the target and camera.")]
    public float CollisionRadius = 0.3f;
    [Tooltip("Layers the camera should collide with (e.g., 'Default', 'Environment').")]
    public LayerMask CollisionLayers = 1; // Default to everything
    [Tooltip("How far to pull the camera back from an obstruction.")]
    public float CollisionOffset = 0.2f;

    // Internal state variables for camera rotation and distance
    private float _currentYaw = 0.0f;       // Horizontal rotation
    private float _currentPitch = 0.0f;     // Vertical rotation
    private float _currentDistance;         // Current zoom distance

    /// <inheritdoc/>
    public void Initialize(Transform cameraTransform, Transform target)
    {
        _cameraTransform = cameraTransform;
        _target = target;
        _currentDistance = Distance; // Start with the default distance
        // Initialize pitch and yaw from the current camera orientation if possible,
        // otherwise default to a neutral view.
        // For an orbit camera, often it's better to start relative to the target's forward.
        _currentYaw = _target.eulerAngles.y;
        _currentPitch = 0; 
    }

    /// <inheritdoc/>
    public void Enter()
    {
        Debug.Log("OrbitCameraStrategy entered.");
        // Optional: Perform any resets or initializations when this strategy becomes active.
        // For example, if you wanted to snap to a specific view or animate the transition.
        // We ensure _currentDistance is up-to-date with the public Distance setting.
        _currentDistance = Distance; 
    }

    /// <inheritdoc/>
    public void Exit()
    {
        Debug.Log("OrbitCameraStrategy exited.");
        // Optional: Cleanup or save state when this strategy is deactivated.
    }

    /// <inheritdoc/>
    public void UpdateCamera(float deltaTime)
    {
        if (_target == null) return;

        // --- 1. Handle User Input ---
        // Mouse input for rotation
        _currentYaw += Input.GetAxis("Mouse X") * MouseXSensitivity;
        _currentPitch -= Input.GetAxis("Mouse Y") * MouseYSensitivity; // Invert Y for natural look-up/down

        // Clamp vertical rotation (pitch) to prevent flipping
        _currentPitch = Mathf.Clamp(_currentPitch, MinPitch, MaxPitch);

        // Mouse scroll wheel for zooming
        _currentDistance -= Input.GetAxis("Mouse ScrollWheel") * ZoomSensitivity;
        _currentDistance = Mathf.Clamp(_currentDistance, MinDistance, MaxDistance);

        // --- 2. Calculate Desired Camera Position and Rotation ---
        // The point around which the camera will orbit (target's position + vertical offset)
        Vector3 pivotPoint = _target.position + Vector3.up * HeightOffset;

        // Calculate the desired rotation based on current pitch and yaw
        Quaternion desiredRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);

        // Calculate the initial desired camera position before collision detection
        Vector3 initialDesiredPosition = pivotPoint - (desiredRotation * Vector3.forward * _currentDistance);

        // --- 3. Collision Handling (Prevent camera from going through obstacles) ---
        RaycastHit hit;
        Vector3 collisionDirection = (initialDesiredPosition - pivotPoint).normalized;
        float actualDistance = _currentDistance;

        // Perform a SphereCast from the pivot towards the desired camera position
        if (Physics.SphereCast(pivotPoint, CollisionRadius, collisionDirection, out hit, _currentDistance, CollisionLayers))
        {
            // If an obstacle is hit, adjust the camera's distance to be just in front of it
            actualDistance = Mathf.Max(MinDistance, hit.distance - CollisionOffset);
        }
        
        // Re-calculate the final desired position using the collision-adjusted distance
        Vector3 finalDesiredPosition = pivotPoint - (desiredRotation * Vector3.forward * actualDistance);

        // --- 4. Smoothly Move and Rotate the Camera ---
        _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, finalDesiredPosition, deltaTime * PositionSmoothSpeed);
        _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, desiredRotation, deltaTime * RotationSmoothSpeed);
    }
}

/// <summary>
/// Implements a simpler camera strategy that stays directly behind the target
/// at a fixed local offset and smoothly follows its movement and rotation.
/// </summary>
[System.Serializable]
public class FixedBehindCameraStrategy : ICameraStrategy
{
    private Transform _cameraTransform;
    private Transform _target;

    [Header("Fixed Behind Settings")]
    [Tooltip("The camera's offset from the target's local space (e.g., (0, 2, -5) for behind and slightly up).")]
    public Vector3 LocalOffset = new Vector3(0, 2, -5); // Behind, up relative to target

    [Tooltip("How smoothly the camera interpolates to its desired position.")]
    public float PositionSmoothSpeed = 8.0f;
    [Tooltip("How smoothly the camera interpolates to its desired rotation.")]
    public float RotationSmoothSpeed = 8.0f;
    [Tooltip("An additional vertical offset for the camera's 'look at' point, making it look slightly above the target's origin.")]
    public float LookAtHeightOffset = 1.0f;


    /// <inheritdoc/>
    public void Initialize(Transform cameraTransform, Transform target)
    {
        _cameraTransform = cameraTransform;
        _target = target;
    }

    /// <inheritdoc/>
    public void Enter()
    {
        Debug.Log("FixedBehindCameraStrategy entered.");
        // Optional: Snap the camera to the initial desired position/rotation immediately
        // or start a slow transition. For simplicity, we'll let LateUpdate handle smoothing.
    }

    /// <inheritdoc/>
    public void Exit()
    {
        Debug.Log("FixedBehindCameraStrategy exited.");
    }

    /// <inheritdoc/>
    public void UpdateCamera(float deltaTime)
    {
        if (_target == null) return;

        // Calculate the desired position based on the target's current position and local offset
        Vector3 desiredPosition = _target.TransformPoint(LocalOffset);

        // Calculate the desired rotation to look at the target (optionally adjusted height)
        Vector3 lookAtPoint = _target.position + Vector3.up * LookAtHeightOffset;
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - desiredPosition);

        // Smoothly move and rotate the camera
        _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, desiredPosition, deltaTime * PositionSmoothSpeed);
        _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, desiredRotation, deltaTime * RotationSmoothSpeed);
    }
}


// ========================================================================================
// 3. AdvancedCameraController (The Context in the Strategy Pattern)
//    This MonoBehaviour manages the active camera strategy and delegates updates.
// ========================================================================================
/// <summary>
/// The main camera controller that orchestrates different camera behaviors
/// using the Strategy Design Pattern. It holds a collection of strategies
/// and delegates its `LateUpdate` call to the currently active one.
/// </summary>
[RequireComponent(typeof(Camera))] // Ensures this GameObject has a Camera component
public class AdvancedCameraController : MonoBehaviour
{
    [Tooltip("The Transform that the camera will follow or look at (e.g., the player character).")]
    public Transform Target;

    [Tooltip("A list of all available camera strategies. Use the Inspector to add and configure them.")]
    [SerializeReference] // Crucial for Unity to serialize interface types in the Inspector
    public List<ICameraStrategy> AvailableStrategies = new List<ICameraStrategy>();

    [Tooltip("The currently active camera strategy. This is set automatically on Awake or can be changed at runtime.")]
    [SerializeReference] // Crucial for Unity to serialize interface types in the Inspector
    private ICameraStrategy _activeStrategy;

    // Public property to safely get and set the active strategy.
    // Handles calling Exit on the old strategy and Enter on the new one.
    public ICameraStrategy ActiveStrategy
    {
        get => _activeStrategy;
        set
        {
            if (_activeStrategy == value) return; // No change needed

            _activeStrategy?.Exit(); // Call Exit on the previously active strategy
            _activeStrategy = value;
            // The strategies are initialized once in Awake.
            // Here we just need to activate the new one.
            _activeStrategy?.Enter(); // Call Enter on the new strategy
            Debug.Log($"Camera strategy switched to: {(_activeStrategy?.GetType().Name ?? "None")}");
        }
    }

    private void Awake()
    {
        // Basic validation: ensure a Camera component exists on this GameObject
        if (GetComponent<Camera>() == null)
        {
            Debug.LogError("AdvancedCameraController requires a Camera component on the same GameObject.", this);
            enabled = false; // Disable the script if no camera is found
            return;
        }

        // Initialize all available strategies.
        // This sets up their permanent references (cameraTransform, target) once.
        foreach (var strategy in AvailableStrategies)
        {
            if (strategy != null)
            {
                strategy.Initialize(this.transform, Target);
            }
            else
            {
                Debug.LogWarning("An empty (null) strategy was found in AvailableStrategies list. Please remove it.", this);
            }
        }

        // Set the initial active strategy:
        // 1. If a strategy was explicitly assigned to _activeStrategy in the Inspector, use that.
        // 2. Otherwise, use the first strategy in the AvailableStrategies list.
        if (_activeStrategy == null && AvailableStrategies.Count > 0)
        {
            // If no active strategy was pre-assigned, default to the first in the list
            _activeStrategy = AvailableStrategies[0];
        }

        // Ensure the initial active strategy (if any) is properly entered.
        if (_activeStrategy != null)
        {
            _activeStrategy.Enter();
        }
        else
        {
            Debug.LogWarning("No camera strategies found or set in AdvancedCameraController. Camera will not move.", this);
            enabled = false; // Disable if no strategy is available
        }
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// This is the ideal place for camera logic to ensure it tracks objects
    /// after they have finished their movement for the current frame.
    /// </summary>
    private void LateUpdate()
    {
        // Delegate the camera update logic to the currently active strategy.
        _activeStrategy?.UpdateCamera(Time.deltaTime);

        // --- Example: Dynamic Strategy Switching with Input (for demonstration) ---
        // Press '1' to activate the OrbitCameraStrategy
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Find the OrbitCameraStrategy instance from the list and set it as active
            ICameraStrategy orbitStrat = AvailableStrategies.Find(s => s is OrbitCameraStrategy);
            if (orbitStrat != null && orbitStrat != ActiveStrategy)
            {
                ActiveStrategy = orbitStrat;
            }
        }
        // Press '2' to activate the FixedBehindCameraStrategy
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Find the FixedBehindCameraStrategy instance from the list and set it as active
            ICameraStrategy fixedStrat = AvailableStrategies.Find(s => s is FixedBehindCameraStrategy);
            if (fixedStrat != null && fixedStrat != ActiveStrategy)
            {
                ActiveStrategy = fixedStrat;
            }
        }
    }

    /// <summary>
    /// Public method to programmatically set the active strategy by type.
    /// </summary>
    /// <typeparam name="T">The type of the camera strategy to activate.</typeparam>
    public void SetStrategy<T>() where T : class, ICameraStrategy
    {
        ICameraStrategy newStrategy = AvailableStrategies.Find(s => s is T);
        if (newStrategy != null)
        {
            ActiveStrategy = newStrategy;
        }
        else
        {
            Debug.LogError($"Strategy of type {typeof(T).Name} not found in AvailableStrategies list.");
        }
    }

    /// <summary>
    /// Draws gizmos in the editor for the active strategy (if it's an OrbitCameraStrategy)
    /// to visualize collision detection parameters.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_activeStrategy is OrbitCameraStrategy orbitStrategy && Target != null)
        {
            // Visualize the collision sphere cast path
            Gizmos.color = Color.yellow;
            Vector3 pivotPoint = Target.position + Vector3.up * orbitStrategy.HeightOffset;
            Gizmos.DrawWireSphere(pivotPoint, 0.1f); // Pivot point
            
            // This requires the current yaw/pitch which is internal to the strategy,
            // so we can't perfectly draw the *exact* runtime path, but we can draw the general concept.
            // For a precise visualization, the strategy would need to expose its current calculated desired position.
            // Let's draw a simplified ray for the current distance.
            Vector3 camForward = Quaternion.Euler(orbitStrategy.MinPitch, orbitStrategy._currentYaw, 0) * Vector3.forward; // Using MinPitch for visualization
            Gizmos.DrawLine(pivotPoint, pivotPoint - camForward * orbitStrategy.Distance);

            // Draw sphere at nominal camera position
            Vector3 nominalCamPos = pivotPoint - camForward * orbitStrategy.Distance;
            Gizmos.DrawWireSphere(nominalCamPos, orbitStrategy.CollisionRadius);
        }
    }
}
```

---

### **Explanation of the AdvancedCameraController Pattern**

1.  **`ICameraStrategy` (The Strategy Interface):**
    *   This interface defines a common contract for all camera behaviors. Any class that implements `ICameraStrategy` must provide `Initialize`, `Enter`, `Exit`, and `UpdateCamera` methods.
    *   This abstraction allows the `AdvancedCameraController` to work with *any* camera behavior without knowing its specific implementation details.

2.  **`OrbitCameraStrategy` and `FixedBehindCameraStrategy` (Concrete Strategies):**
    *   These are concrete implementations of `ICameraStrategy`. Each one encapsulates a distinct way the camera should behave.
    *   **`OrbitCameraStrategy`**: Implements a typical third-person camera that orbits around a target, responds to mouse input for rotation and zoom, and includes basic collision detection (using `SphereCast`) to prevent the camera from going through environment objects. It handles smooth movement using `Vector3.Lerp` and `Quaternion.Slerp`.
    *   **`FixedBehindCameraStrategy`**: A simpler strategy that keeps the camera at a constant local offset behind the target, following its position and rotation smoothly.
    *   `[System.Serializable]` is used on these classes so they can be configured directly in the Unity Inspector when embedded within the `AdvancedCameraController` script.

3.  **`AdvancedCameraController` (The Context):**
    *   This is a `MonoBehaviour` script that you attach to your main `Camera` GameObject.
    *   **`Target`**: A reference to the GameObject the camera should follow (e.g., your player character).
    *   **`AvailableStrategies`**: A `List` of `ICameraStrategy` objects. You populate this list in the Inspector with instances of `OrbitCameraStrategy`, `FixedBehindCameraStrategy`, or any other custom strategies you create.
    *   **`[SerializeReference]`**: This attribute is crucial! It tells Unity's serialization system to correctly save and load instances of interface types (like `ICameraStrategy`) or abstract classes in the Inspector. Without it, you wouldn't be able to assign concrete strategy classes to the `AvailableStrategies` list or the `_activeStrategy` field directly in the Inspector.
    *   **`ActiveStrategy` Property**: This public property allows you to dynamically change the active camera behavior at runtime. When a new strategy is set, it automatically calls `Exit()` on the old strategy and `Enter()` on the new one, allowing for clean transitions and setup/teardown logic.
    *   **`Awake()`**: Initializes all strategies in the `AvailableStrategies` list with the camera's transform and the target. It also sets an initial active strategy (either one pre-assigned in the Inspector or the first one in the list).
    *   **`LateUpdate()`**: This is the heart of the delegation. Instead of implementing camera logic directly, it simply calls `UpdateCamera(Time.deltaTime)` on the `_activeStrategy`. Camera logic should be in `LateUpdate` to ensure all other game objects have completed their `Update` phase for the current frame, preventing camera jitter.
    *   **Strategy Switching Example**: The `LateUpdate` includes example input (`Alpha1`, `Alpha2`) to demonstrate how you can switch between different camera strategies dynamically.

### **Benefits of this Pattern**

*   **Modularity**: Each camera behavior is self-contained in its own class, making it easy to understand, debug, and modify without affecting other behaviors.
*   **Extensibility**: Adding new camera behaviors is as simple as creating a new class that implements `ICameraStrategy` and adding it to the `AvailableStrategies` list in the Inspector. No modifications to the `AdvancedCameraController` itself are needed.
*   **Flexibility**: You can dynamically switch camera behaviors at runtime based on game events (e.g., entering combat, aiming, cutscenes, entering a vehicle).
*   **Reusability**: Individual `ICameraStrategy` implementations can potentially be reused across different projects or camera setups.
*   **Testability**: Each strategy can be tested in isolation.

---

### **Example Usage in Unity**

1.  **Create a Camera GameObject**:
    *   In your Unity scene, create an empty GameObject (e.g., `GameObject -> Create Empty`).
    *   Rename it to `Main Camera` (if you don't already have one, or disable your default main camera).
    *   Add a `Camera` component to this `Main Camera` GameObject (`Add Component -> Camera`).

2.  **Create a Target Object**:
    *   Create a simple GameObject to be your camera's target (e.g., `GameObject -> 3D Object -> Cube`).
    *   Rename it `Player` and give it some distinct position in the scene.

3.  **Attach the Script**:
    *   Attach the `AdvancedCameraController.cs` script to your `Main Camera` GameObject (`Add Component -> Advanced Camera Controller`).

4.  **Configure the Controller in the Inspector**:
    *   **`Target`**: Drag your `Player` GameObject from the Hierarchy into the `Target` slot of the `AdvancedCameraController` component.
    *   **`Available Strategies`**:
        *   Click the `+` button to add a new strategy.
        *   In the dropdown that appears, navigate to `Script -> Orbit Camera Strategy` and select it.
        *   Configure its properties (e.g., `Distance`, `Sensitivity`, `Collision Layers`).
        *   Click the `+` button again.
        *   Select `Script -> Fixed Behind Camera Strategy`.
        *   Configure its properties (e.g., `Local Offset`).

5.  **Run the Scene**:
    *   Press Play. The camera should start with the `OrbitCameraStrategy` (as it's the first in the list).
    *   Use your mouse to orbit around the `Player` and the scroll wheel to zoom.
    *   **Press `1` on your keyboard** to switch to the `OrbitCameraStrategy` (if not already active).
    *   **Press `2` on your keyboard** to switch to the `FixedBehindCameraStrategy`. Observe how the camera behavior changes instantly.

You now have a flexible and extensible camera system based on the Strategy pattern, ready for advanced game development!