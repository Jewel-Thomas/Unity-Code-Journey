// Unity Design Pattern Example: CameraCollisionSystem
// This script demonstrates the CameraCollisionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script provides a practical implementation of the `CameraCollisionSystem` design pattern in Unity. This pattern is crucial for third-person cameras to prevent them from clipping through environmental objects while maintaining a smooth, desired view of the target.

### CameraCollisionSystem Design Pattern Explained

**Purpose:**
The primary goal of the Camera Collision System is to dynamically adjust a camera's position to avoid obstacles that come between the camera and its target (e.g., the player character). Instead of clipping through walls or terrain, the camera will smoothly move closer to the target until the obstacle is cleared. When no obstacles are present, the camera returns to its ideal, desired position.

**Key Components & Logic:**

1.  **Camera Rig (Pivot):** A dedicated GameObject (often empty) acts as the camera's pivot point. This rig usually follows the target's position and handles all camera rotation based on player input. The actual `Camera` component is typically a child of this rig.
2.  **Desired Offset:** An `offset` vector defines the ideal position of the camera relative to its target (e.g., behind and above the player).
3.  **Collision Detection:**
    *   A raycast or, more robustly, a `SphereCast` is performed from the target's position towards the camera's *desired* ideal position.
    *   The `SphereCast` uses a small radius (`collisionBuffer`) to simulate the camera's physical volume, preventing its edges from clipping.
    *   If the cast hits an obstacle (`collisionLayerMask`) before reaching the ideal position, a collision is detected.
4.  **Position Adjustment:**
    *   When a collision occurs, the camera's position is moved along the collision ray/direction to the point of impact, backing off slightly by the `collisionBuffer` to avoid immediate contact.
    *   A `minCollisionDistance` ensures the camera doesn't get *too* close to the target, even in tight spaces.
    *   When no collision is detected, the camera moves back to its `desired offset`.
5.  **Smoothing:** `Vector3.SmoothDamp` and `Quaternion.Slerp` are used to ensure all camera movements (position and rotation) are fluid and natural, avoiding sudden jumps.
6.  **LateUpdate:** All camera logic is placed in `LateUpdate()`. This is crucial because `LateUpdate()` runs *after* all other `Update()` methods (including player movement), ensuring the camera always reacts to the target's final position for the current frame.

---

### Complete C# Unity Script: `ThirdPersonCameraCollision.cs`

```csharp
using UnityEngine;

/// <summary>
/// Implements the Camera Collision System pattern for a third-person camera in Unity.
/// This script handles camera following, rotation, and collision detection to prevent
/// the camera from clipping through environmental objects.
///
/// Design Pattern: Camera Collision System
/// Purpose: Adjusts the camera's position dynamically to avoid obstacles between the camera and its target,
///          while maintaining a desired distance when no obstacles are present.
/// </summary>
/// <remarks>
/// How to Use in Unity:
/// 1. Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty).
///    Rename it to "CameraRig" or "ThirdPersonCameraPivot".
/// 2. Add a standard Unity Camera as a child of this "CameraRig" GameObject.
///    (e.g., right-click "CameraRig" -> Camera). Set its Local Position to (0,0,0) and Local Rotation to (0,0,0).
/// 3. Attach this `ThirdPersonCameraCollision` script to the "CameraRig" GameObject.
/// 4. In the Inspector for the "CameraRig":
///    a. Assign your player's `Transform` to the `Target` field.
///    b. The `Camera Transform` field should automatically link to its child Camera.
///    c. Adjust the `Offset` vector (e.g., X=0, Y=2, Z=-5) to define the camera's default position
///       relative to the target. Negative Z means behind the target.
///    d. Set the `Collision Layer Mask` to include all layers that the camera should collide with
///       (e.g., "Default", "Environment", "Walls"). IMPORTANT: Exclude the player's layer itself,
///       otherwise the camera will try to push away from the player!
///    e. Adjust `Min Collision Distance`, `Collision Buffer`, `Position Smooth Time`,
///       `Rotation Smooth Speed`, and `Mouse Sensitivity` to fit your game's feel.
/// 5. Ensure your player character has a Collider (e.g., Capsule Collider) and is on a layer NOT included in the `collisionLayerMask`.
/// </remarks>
public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("Target and Offset")]
    [Tooltip("The Transform the camera will follow (e.g., the player character).")]
    public Transform target;

    [Tooltip("The ideal local offset of the camera from the target when no collisions occur. This defines the camera's 'default' view.")]
    public Vector3 offset = new Vector3(0f, 2f, -5f);

    [Tooltip("The actual camera Transform. If left null, it tries to find a Camera component in children on Awake.")]
    public Transform cameraTransform;

    [Header("Collision Settings")]
    [Tooltip("Layers that the camera will collide with to prevent clipping. Make sure to exclude the player's layer!")]
    public LayerMask collisionLayerMask;

    [Tooltip("The minimum distance the camera can get to the target during a collision. Prevents it from getting too close.")]
    public float minCollisionDistance = 1.0f;

    [Tooltip("Radius of the sphere used for collision detection. A small buffer helps prevent camera edges from clipping.")]
    public float collisionBuffer = 0.3f; // Small buffer for SphereCast radius

    [Header("Camera Movement & Rotation")]
    [Tooltip("How smoothly the camera's position adjusts to its target or collision-adjusted spot.")]
    public float positionSmoothTime = 0.1f;

    [Tooltip("How smoothly the camera's rotation adjusts based on input.")]
    public float rotationSmoothSpeed = 5.0f; // Uses Lerp, not SmoothDamp for rotation

    [Tooltip("Speed multiplier for horizontal camera rotation (Mouse X input).")]
    public float mouseXSensitivity = 100f;

    [Tooltip("Speed multiplier for vertical camera rotation (Mouse Y input).")]
    public float mouseYSensitivity = 100f;

    [Tooltip("If true, moving the mouse up will move the camera down (inverted Y-axis).")]
    public bool invertYAxis = false;

    [Tooltip("Minimum vertical angle (in degrees) the camera can look down.")]
    [Range(-89f, 89f)]
    public float minYAngle = -60f;

    [Tooltip("Maximum vertical angle (in degrees) the camera can look up.")]
    [Range(-89f, 89f)]
    public float maxYAngle = 80f;

    // Private fields for internal state management
    private float _currentX = 0f; // Current horizontal rotation angle of the camera rig
    private float _currentY = 0f; // Current vertical rotation angle of the camera rig
    private Vector3 _positionSmoothVelocity = Vector3.zero; // Velocity reference for Vector3.SmoothDamp
    private Vector3 _currentCameraOffset; // The dynamically adjusted offset from the target, considering collisions

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used for initial setup before Start, ensuring critical components are available.
    /// </summary>
    void Awake()
    {
        // If cameraTransform is not explicitly assigned in the Inspector,
        // try to find a Camera component in the children of this GameObject (the CameraRig).
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError("No Camera Transform assigned and no Camera found in children of " + gameObject.name + ". Disabling script.", this);
                enabled = false; // Disable script if no camera is found
                return;
            }
        }

        // Initialize current rotation angles based on the CameraRig's initial rotation.
        // This prevents a sudden "snap" if the rig isn't starting at (0,0,0) Euler angles.
        Vector3 eulerAngles = transform.eulerAngles;
        _currentX = eulerAngles.y; // Yaw
        _currentY = eulerAngles.x; // Pitch
        
        // Normalize the X angle if it's over 180 or under -180 to fit typical angle ranges.
        // This helps with clamping later.
        if (_currentY > 180) _currentY -= 360;
        if (_currentY < -180) _currentY += 360;
        _currentY = Mathf.Clamp(_currentY, minYAngle, maxYAngle); // Apply initial clamp

        // Lock the cursor to the center of the screen and hide it for typical third-person controls.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize the current camera offset to the ideal offset.
        // It will be adjusted in LateUpdate if collisions occur.
        _currentCameraOffset = offset;
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled, just before any Update methods.
    /// Used for checks that rely on other Awake calls being completed.
    /// </summary>
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera Target not assigned to " + gameObject.name + ". Disabling script.", this);
            enabled = false; // Disable script if no target is set
            return;
        }
    }

    /// <summary>
    /// LateUpdate is called once per frame, after all Update functions have been called.
    /// This is the ideal place for camera logic, as all player movement and other object
    /// updates will have already completed for the current frame, ensuring the camera
    /// reacts to the final state of objects.
    /// </summary>
    void LateUpdate()
    {
        // Basic safety check: if target or cameraTransform are somehow null, do nothing.
        if (target == null || cameraTransform == null)
        {
            return;
        }

        // --- Step 1: Handle Camera Rotation Input ---
        // This rotates the 'CameraRig' (this GameObject) around the target.
        HandleCameraRotation();

        // --- Step 2: Position the Camera Rig at the Target's Location ---
        // The CameraRig itself acts as the pivot point directly at the target's center.
        transform.position = target.position;

        // --- Step 3: Calculate the Desired Camera Position (without collision adjustment) ---
        // This is the ideal position the camera *wants* to be at, based on the rig's rotation and the defined offset.
        Vector3 idealLocalOffset = offset;
        Vector3 desiredCameraWorldPosition = transform.position + transform.rotation * idealLocalOffset;

        // --- Step 4: Perform Collision Detection and Adjust Camera Position ---
        // Initialize _currentCameraOffset to the ideal offset. This will be overwritten if a collision occurs.
        _currentCameraOffset = idealLocalOffset; 

        // Define the origin and direction for the collision check.
        // The SphereCast starts from the target's center and goes towards the desired camera position.
        Vector3 collisionOrigin = target.position;
        Vector3 collisionDirection = (desiredCameraWorldPosition - collisionOrigin).normalized;
        float maxCollisionDistance = Vector3.Distance(desiredCameraWorldPosition, collisionOrigin);

        RaycastHit hit;
        // Use Physics.SphereCast for robust collision detection.
        // It casts a sphere (with radius `collisionBuffer`) along a ray, which helps prevent
        // the camera's 'volume' from clipping, not just its central point.
        if (Physics.SphereCast(collisionOrigin, collisionBuffer, collisionDirection, out hit, maxCollisionDistance, collisionLayerMask))
        {
            // Collision detected! Adjust the camera's offset to bring it closer to the target.
            // The adjusted distance is based on the hit point, backing off by the `collisionBuffer`.
            float adjustedDistance = hit.distance - collisionBuffer;

            // Ensure the camera doesn't get closer than the `minCollisionDistance`.
            if (adjustedDistance < minCollisionDistance)
            {
                adjustedDistance = minCollisionDistance;
            }

            // Update the _currentCameraOffset based on the calculated adjusted distance.
            _currentCameraOffset = collisionDirection * adjustedDistance;
        }
        // If no collision is detected, or the collision is beyond the ideal position,
        // _currentCameraOffset remains the idealLocalOffset, effectively placing the camera
        // at its desired non-colliding position.

        // --- Step 5: Smoothly Apply the Camera's Final Position ---
        // The final camera position is the target's position plus the (potentially collision-adjusted) offset.
        // Vector3.SmoothDamp provides smooth, critically damped motion.
        Vector3 finalCameraPosition = target.position + _currentCameraOffset;
        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, finalCameraPosition, ref _positionSmoothVelocity, positionSmoothTime);
    }

    /// <summary>
    /// Handles camera rotation based on mouse input.
    /// Rotates the CameraRig (this GameObject) around the target.
    /// </summary>
    private void HandleCameraRotation()
    {
        // Get raw mouse input axes.
        // For a more modern approach, consider Unity's new Input System.
        float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity * Time.deltaTime;

        // Update horizontal rotation (yaw) of the CameraRig.
        _currentX += mouseX;

        // Update vertical rotation (pitch) of the CameraRig, respecting the invertYAxis setting.
        if (invertYAxis)
        {
            _currentY += mouseY;
        }
        else
        {
            _currentY -= mouseY;
        }

        // Clamp vertical rotation to predefined min/max angles to prevent the camera from flipping over.
        _currentY = Mathf.Clamp(_currentY, minYAngle, maxYAngle);

        // Calculate the target rotation for the CameraRig based on the accumulated angles.
        Quaternion targetRotation = Quaternion.Euler(_currentY, _currentX, 0);

        // Smoothly rotate the CameraRig towards the target rotation using Quaternion.Slerp.
        // Slerp provides spherical interpolation for rotations.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// OnDrawGizmos is called for rendering gizmos in the editor.
    /// Useful for visualizing the camera's logic, target, and collision checks.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw gizmos if target and cameraTransform are assigned.
        if (target == null || cameraTransform == null) return;

        // Draw a cyan sphere at the target position to mark it.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, 0.2f);
        
        // Draw a yellow line from the target to the camera rig's position (the pivot point).
        // This helps visualize where the camera is rotating around.
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position, transform.position);

        // --- Visualize Desired Camera Position (before collision) ---
        // Calculate the ideal desired world position of the camera based on the rig's rotation and default offset.
        Vector3 idealDesiredLocalOffset = offset;
        Vector3 idealDesiredWorldPosition = transform.position + transform.rotation * idealDesiredLocalOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position, idealDesiredWorldPosition); // Line from target to ideal position
        Gizmos.DrawWireSphere(idealDesiredWorldPosition, 0.1f); // Sphere at ideal position

        // --- Visualize Collision SphereCast Path ---
        Vector3 collisionOrigin = target.position;
        Vector3 collisionDirection = (idealDesiredWorldPosition - collisionOrigin).normalized;
        float maxCollisionDistance = Vector3.Distance(idealDesiredWorldPosition, collisionOrigin);
        
        // Draw a red ray representing the path of the SphereCast.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(collisionOrigin, collisionDirection * maxCollisionDistance);
        
        // Draw a wire sphere at the camera's *actual* current position to show its collision buffer.
        Gizmos.DrawWireSphere(cameraTransform.position, collisionBuffer); 

        // --- Visualize Actual Camera Position (after collision adjustment) ---
        // Draw a green sphere at the camera's actual current world position.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(cameraTransform.position, 0.15f);

        // Draw a blue line from the target to the camera's actual current position.
        // This is the final line of sight.
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(target.position, cameraTransform.position);
    }
}
```