// Unity Design Pattern Example: ThirdPersonCameraSystem
// This script demonstrates the ThirdPersonCameraSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **ThirdPersonCameraSystem** design pattern. This pattern encapsulates all the logic required for a third-person camera, including following a target, handling user input for rotation and zoom, and preventing the camera from clipping through obstacles.

The script is designed to be highly configurable via the Unity Inspector and uses best practices like `LateUpdate` for camera logic, `SmoothDamp` for fluid movement, and `SphereCast` for robust collision detection.

```csharp
using UnityEngine;
using System.Collections; // Often useful, but not strictly required for this specific script
using System.Collections.Generic; // Often useful, but not strictly required for this specific script

/// <summary>
/// ThirdPersonCameraSystem Design Pattern Implementation.
///
/// This script demonstrates a common pattern for managing a third-person camera
/// that follows a target, allowing user input for rotation and zoom,
/// and includes collision detection to prevent the camera from clipping through obstacles.
///
/// The pattern encapsulates all camera-related logic (following, input, collision, smoothing)
/// into a single, cohesive system, making it easy to manage and integrate.
/// </summary>
[AddComponentMenu("Camera Systems/Third Person Camera System")] // Makes it easier to find in the Add Component menu
public class ThirdPersonCameraSystem : MonoBehaviour
{
    [Header("Target and Offset")]
    [Tooltip("The Transform the camera should follow (e.g., the player character).")]
    [SerializeField] private Transform target;
    [Tooltip("The initial offset from the target's position. This defines the starting distance and angle.")]
    [SerializeField] private Vector3 initialOffset = new Vector3(0f, 2f, -5f);
    [Tooltip("Adjusts the height offset for where the camera looks at the target. " +
             "Useful for looking at the character's head/chest instead of their feet.")]
    [SerializeField] private float lookAtHeightOffset = 1.5f;

    [Header("Rotation Settings")]
    [Tooltip("Speed at which the camera rotates horizontally (Yaw) with mouse input.")]
    [SerializeField] private float rotationSpeedX = 150f;
    [Tooltip("Speed at which the camera rotates vertically (Pitch) with mouse input.")]
    [SerializeField] private float rotationSpeedY = 150f;
    [Range(-90, 0)]
    [Tooltip("Minimum vertical angle (pitch) the camera can go down to (e.g., looking down).")]
    [SerializeField] private float minPitch = -60f;
    [Range(0, 90)]
    [Tooltip("Maximum vertical angle (pitch) the camera can go up to (e.g., looking up).")]
    [SerializeField] private float maxPitch = 80f;

    [Header("Zoom Settings")]
    [Tooltip("Speed at which the camera zooms in/out with the mouse scroll wheel.")]
    [SerializeField] private float zoomSpeed = 10f;
    [Tooltip("Minimum distance the camera can zoom in to the target.")]
    [SerializeField] private float minZoomDistance = 2f;
    [Tooltip("Maximum distance the camera can zoom out from the target.")]
    [SerializeField] private float maxZoomDistance = 10f;

    [Header("Smoothing Settings")]
    [Tooltip("Time taken for the camera to smoothly follow the target's position. Lower values mean snappier movement.")]
    [SerializeField] private float positionSmoothTime = 0.15f;
    [Tooltip("Time taken for the camera to smoothly rotate to its desired orientation. Lower values mean snappier rotation.")]
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [Tooltip("Time taken for the camera zoom to smoothly change. Lower values mean snappier zoom.")]
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Collision Settings")]
    [Tooltip("Layers the camera should collide with to prevent clipping through obstacles (e.g., 'Default', 'Environment').")]
    [SerializeField] private LayerMask cameraCollisionLayerMask;
    [Tooltip("The radius of the sphere used for collision detection. " +
             "This represents the camera's 'physical size' for collision.")]
    [SerializeField] private float collisionSphereRadius = 0.3f;
    [Tooltip("Distance to keep the camera away from colliding objects. " +
             "Prevents the camera from touching or slightly going into walls.")]
    [SerializeField] private float collisionBufferDistance = 0.1f;

    // --- Private Internal State ---
    private float currentYaw;          // Stores the current horizontal rotation angle
    private float currentPitch;        // Stores the current vertical rotation angle
    private float currentZoomDistance; // Stores the current distance from the target

    // Variables for SmoothDamp functions to maintain continuity across frames
    private Vector3 currentPositionVelocity;
    private float currentZoomVelocity;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// This is used to initialize the camera's state.
    /// </summary>
    private void Awake()
    {
        // Essential check: If no target is assigned, log an error and disable the script.
        if (target == null)
        {
            Debug.LogError("ThirdPersonCameraSystem: Target is not assigned. Please assign a target Transform in the Inspector.", this);
            enabled = false; // Disable the script if it can't function
            return;
        }

        // --- Initialize currentYaw, currentPitch, and currentZoomDistance ---
        // We calculate these based on the `initialOffset` to provide a consistent starting camera view.
        // This ensures the camera's behavior is predictable regardless of its initial placement in the editor.

        // 1. Calculate the initial direction vector from the target's look-at point to the camera's desired initial position.
        Vector3 targetLookAtPosition = target.position + Vector3.up * lookAtHeightOffset;
        Vector3 initialCameraPosition = target.position + initialOffset;
        Vector3 initialDirectionFromTarget = (initialCameraPosition - targetLookAtPosition).normalized;

        // 2. Create a Quaternion that represents the camera looking *from* `initialCameraPosition` *towards* `targetLookAtPosition`.
        Quaternion initialCameraLookRotation = Quaternion.LookRotation(-initialDirectionFromTarget); // Look towards the target

        // 3. Extract the Euler angles (Yaw and Pitch) from this Quaternion.
        //    Euler angles need careful handling, especially for pitch which can wrap from 0-360.
        Vector3 eulerAngles = initialCameraLookRotation.eulerAngles;
        currentYaw = eulerAngles.y;

        // Normalize pitch to be between -90 and 90 degrees for easier clamping.
        if (eulerAngles.x > 180f)
        {
            currentPitch = eulerAngles.x - 360f;
        }
        else
        {
            currentPitch = eulerAngles.x;
        }
        
        // Ensure the initial pitch is within the defined limits.
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // 4. Initialize zoom distance from the magnitude of the `initialOffset`.
        currentZoomDistance = initialOffset.magnitude;
        // Ensure initial zoom is within the defined limits.
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);

        // Immediately set the camera to this initial position and rotation to prevent a 'jump'
        // on the first frame if the editor camera position differs from `initialOffset`.
        transform.position = initialCameraPosition;
        transform.LookAt(targetLookAtPosition);
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called for all GameObjects.
    /// This is the ideal place for camera logic to ensure all target movement
    /// (e.g., player character movement) has been completed for the current frame before the camera updates.
    /// </summary>
    private void LateUpdate()
    {
        // Safety check in case the target is destroyed during runtime.
        if (target == null) return;

        HandleInput();
        CalculateCameraPositionAndRotation();
    }

    /// <summary>
    /// Processes user input for camera rotation (mouse X/Y) and zoom (mouse scroll wheel).
    /// </summary>
    private void HandleInput()
    {
        // --- Rotation Input ---
        // Mouse X controls Yaw (horizontal rotation around the Y-axis).
        currentYaw += Input.GetAxis("Mouse X") * rotationSpeedX * Time.deltaTime;
        // Mouse Y controls Pitch (vertical rotation around the X-axis).
        // Negative sign inverts mouse Y, which is common for camera control.
        currentPitch -= Input.GetAxis("Mouse Y") * rotationSpeedY * Time.deltaTime; 

        // Clamp pitch to prevent the camera from flipping upside down or looking too far up/down.
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // --- Zoom Input ---
        // Mouse ScrollWheel controls zoom distance.
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        // Smoothly adjust the current zoom distance based on scroll input.
        currentZoomDistance = Mathf.SmoothDamp(currentZoomDistance, currentZoomDistance - scrollInput * zoomSpeed, ref currentZoomVelocity, zoomSmoothTime);
        // Clamp the zoom distance to stay within min/max limits.
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
    }

    /// <summary>
    /// Calculates the desired camera position and rotation, applies collision detection,
    /// and then smoothly moves and rotates the actual camera transform.
    /// </summary>
    private void CalculateCameraPositionAndRotation()
    {
        // 1. Determine the target point the camera should look at.
        Vector3 targetLookAtPosition = target.position + Vector3.up * lookAtHeightOffset;

        // 2. Calculate the desired raw position *before* any collision adjustments.
        //    This uses the currentYaw, currentPitch, and currentZoomDistance to find the ideal spot.
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        // We apply the rotation to a vector pointing directly behind the target at the current zoom distance.
        Vector3 desiredPositionRaw = targetLookAtPosition + desiredRotation * new Vector3(0, 0, -currentZoomDistance);

        // --- 3. Collision Detection ---
        Vector3 collisionAdjustedPosition = desiredPositionRaw; // Start with the raw desired position
        RaycastHit hit;
        
        // Perform a SphereCast from the target's look-at point towards the desired camera position.
        // SphereCasts are more robust than Raycasts for cameras as they account for the camera's volume,
        // preventing the 'edge' of the camera from clipping.
        Vector3 rayDirection = (desiredPositionRaw - targetLookAtPosition).normalized;
        float rayDistance = Vector3.Distance(desiredPositionRaw, targetLookAtPosition);

        if (Physics.SphereCast(targetLookAtPosition, collisionSphereRadius, rayDirection, out hit, rayDistance, cameraCollisionLayerMask))
        {
            // If an obstacle is hit, adjust the camera's position to be just before the hit point.
            // We subtract a `collisionBufferDistance` to keep the camera slightly off the obstacle.
            float collisionDistance = hit.distance - collisionBufferDistance;
            
            // Ensure the adjusted distance is not less than the minimum zoom distance,
            // to prevent the camera from being forced too close to the target.
            collisionAdjustedPosition = targetLookAtPosition + rayDirection * Mathf.Max(collisionDistance, minZoomDistance);
        }

        // --- 4. Smoothly Update Camera Position ---
        // Use SmoothDamp to gradually move the camera's transform.position towards the
        // collision-adjusted desired position, creating a fluid following effect.
        transform.position = Vector3.SmoothDamp(transform.position, collisionAdjustedPosition, ref currentPositionVelocity, positionSmoothTime);

        // --- 5. Smoothly Update Camera Rotation ---
        // Calculate the rotation required to make the camera look at the target.
        Quaternion targetLookRotation = Quaternion.LookRotation(targetLookAtPosition - transform.position);

        // Use Quaternion.Slerp (spherical linear interpolation) for smooth rotation.
        // Time.deltaTime / rotationSmoothTime controls the interpolation speed.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetLookRotation, Time.deltaTime / rotationSmoothTime);
    }

    /// <summary>
    /// OnDrawGizmos is called for drawing gizmos in the editor.
    /// This helps visualize the camera's behavior and important points.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (target == null) return;

        // Draw a line from the target to the current camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position, transform.position);

        // Draw a sphere at the target's look-at point
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position + Vector3.up * lookAtHeightOffset, 0.2f);

        // Draw the collision sphere at the current camera position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionSphereRadius);

        // If playing, visualize the desired raw position before collision for debugging purposes
        if (Application.isPlaying)
        {
            Vector3 targetLookAtPosition = target.position + Vector3.up * lookAtHeightOffset;
            Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 desiredPositionRaw = targetLookAtPosition + desiredRotation * new Vector3(0, 0, -currentZoomDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(desiredPositionRaw, collisionSphereRadius / 2);
        }
    }
}

/*
/// --- Example Usage in Unity Editor ---

To implement this ThirdPersonCameraSystem in your Unity project, follow these steps:

1.  **Prepare your Player/Target:**
    *   Create a 3D Object (e.g., a Capsule, Cube, or your actual player model). Rename it "Player".
    *   **Crucially**, ensure your "Player" GameObject has a Rigidbody (if it moves using physics) or a CharacterController (if it moves without physics directly).
    *   Add a simple script to "Player" to make it move (e.g., using `Input.GetAxis("Horizontal")` and `Input.GetAxis("Vertical")`) so you can test camera following.

2.  **Set up the Camera GameObject:**
    *   Delete the default "Main Camera" if you want this system to fully control your primary camera.
    *   Create an Empty GameObject in your scene (Right-click in Hierarchy -> Create Empty). Rename it "ThirdPersonCameraRig".
    *   Reset its Transform (Right-click on the Transform component -> Reset) to place it at (0,0,0).
    *   Add a Camera component to "ThirdPersonCameraRig" (Select "ThirdPersonCameraRig" -> Add Component -> search for "Camera"). This Camera component *is* your main camera.

3.  **Attach the Script:**
    *   Create a new C# Script asset named "ThirdPersonCameraSystem" in your Project window.
    *   Copy and paste the entire code above into this new script file, overwriting its contents.
    *   Drag and drop the "ThirdPersonCameraSystem" script from your Project window onto your "ThirdPersonCameraRig" GameObject in the Hierarchy.

4.  **Configure the Script in the Inspector:**
    *   Select your "ThirdPersonCameraRig" GameObject in the Hierarchy. Its Inspector panel will show the "Third Person Camera System" component.
    *   **Target:** Drag your "Player" GameObject from the Hierarchy into the 'Target' field.
    *   **Initial Offset:** Adjust this (e.g., X=0, Y=2, Z=-5) to set the starting distance and height for the camera relative to the player. The script will derive the initial rotation and zoom from this.
    *   **Rotation Speed X/Y:** Adjust these values (e.g., 150) to change how fast the camera rotates horizontally and vertically with mouse movement.
    *   **Min/Max Pitch:** Set limits for how far the camera can look up and down (e.g., -60 for looking down, 80 for looking up).
    *   **Zoom Speed:** Adjust how fast the camera zooms in/out with the mouse scroll wheel (e.g., 10).
    *   **Min/Max Zoom Distance:** Define the closest (e.g., 2) and farthest (e.g., 10) the camera can zoom from the target.
    *   **Smoothing Settings:** Experiment with `Position Smooth Time` (e.g., 0.15), `Rotation Smooth Time` (e.g., 0.1), and `Zoom Smooth Time` (e.g., 0.1) to get the desired feel for camera responsiveness. Higher values mean slower, more delayed movement.
    *   **Camera Collision Layer Mask:**
        *   Go to the Unity Editor's top menu -> `Layers` -> `Edit Layers...`.
        *   Add a new User Layer, for example, "CameraCollision".
        *   Select objects in your scene (walls, trees, buildings, terrain) that the camera *should* collide with. In their Inspector, set their `Layer` dropdown to "CameraCollision".
        *   Back on the "ThirdPersonCameraSystem" component, click the dropdown next to 'Camera Collision Layer Mask' and check the "CameraCollision" layer.
    *   **Collision Sphere Radius:** This represents the camera's "size" for collision purposes (e.g., 0.3).
    *   **Collision Buffer Distance:** This is the minimum distance the camera will maintain from a colliding object (e.g., 0.1).

5.  **Test Your Scene:**
    *   Run the game (Play button).
    *   Move your player character around. The camera should smoothly follow.
    *   Move your mouse to rotate the camera around the player.
    *   Use the mouse scroll wheel to zoom in and out.
    *   Move the player behind an obstacle (like a wall or a large cube you've set to the "CameraCollision" layer). The camera should gracefully move closer to the player to avoid clipping through the obstacle.

This setup provides a robust, configurable, and educational example of the ThirdPersonCameraSystem design pattern in Unity.
*/
```