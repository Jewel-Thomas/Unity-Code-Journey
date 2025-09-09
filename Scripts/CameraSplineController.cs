// Unity Design Pattern Example: CameraSplineController
// This script demonstrates the CameraSplineController pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# script implements the **Camera Spline Controller** design pattern in Unity.

### Camera Spline Controller Design Pattern Explained

**Intent:** The Camera Spline Controller pattern aims to provide a robust, reusable, and easily configurable system for moving a camera along a predefined path (a spline or curve) in a game or application. It decouples the camera's movement logic from the game's core logic, making cinematic sequences, guided tours, or fixed-path gameplay cameras much easier to implement and manage.

**Key Components:**

1.  **Spline Definition:** A series of control points (typically `Transform` objects in Unity) that define the shape of the camera's path. The controller interpolates between these points to create a smooth curve. In this example, we use a Catmull-Rom spline for its smoothness and property of passing through all control points (except the first and last two, which act as tangents).
2.  **Camera Controller:** A dedicated component (`MonoBehaviour` script) that:
    *   **Manages the Camera:** Holds a reference to the camera it controls.
    *   **Calculates Path:** Uses the spline's control points to calculate the camera's exact position and (optionally) rotation at any given point along the path.
    *   **Controls Playback:** Provides methods to start, stop, pause, and reset the camera's movement, as well as adjusting parameters like speed/duration and looping behavior.
    *   **Optional Look-At Target:** Allows the camera to dynamically track an object while moving along the spline.

**Benefits:**

*   **Modularity:** The camera movement logic is encapsulated within a single, self-contained component.
*   **Reusability:** This script can be dropped into any scene and configured for different camera paths without code changes.
*   **Design-Time Visualisation:** Using `OnDrawGizmos`, developers can visually lay out and preview the camera path directly in the Unity editor.
*   **Decoupling:** Game logic doesn't need to know *how* the camera moves, only *when* to tell it to start or stop. This simplifies game code.
*   **Flexibility:** Easily extensible to support different spline types (Bezier, Linear), easing functions, or advanced camera behaviors (e.g., dynamic FOV changes).

---

### `CameraSplineController.cs` Script

This script allows you to define a path using empty GameObjects as control points, assign a camera, and then smoothly move that camera along the path. It includes editor visualization (Gizmos) to make path creation intuitive.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implements the Camera Spline Controller design pattern in Unity.
/// This script manages a camera's movement along a defined Catmull-Rom spline.
/// </summary>
/// <remarks>
/// The Camera Spline Controller decouples the camera's path definition
/// from its movement logic, offering a reusable and configurable system
/// for cinematic sequences, guided tours, or fixed-path gameplay cameras.
///
/// **Setup Instructions:**
/// 1. Create a new empty GameObject in your scene (e.g., "CameraSplineManager").
/// 2. Attach this `CameraSplineController` script to it.
/// 3. Create at least 4 empty GameObjects in your scene. These will be your
///    control points. Position them to define your desired camera path.
///    (e.g., "Point A", "Point B", "Point C", "Point D", etc.).
/// 4. Drag these control point GameObjects into the `Control Points` list
///    in the Inspector of the "CameraSplineManager" GameObject.
///    Ensure you have at least 4 points for the Catmull-Rom spline to work.
/// 5. Assign your main camera (or any specific camera) to the `Target Camera`
///    field in the Inspector. If left null, it defaults to `Camera.main`.
/// 6. (Optional) Assign a `Look At Target` Transform if you want the camera
///    to always face a specific object while moving.
/// 7. Configure `Travel Duration`, `Loop Spline`, and `Auto Start On Awake`
///    settings as needed.
/// 8. Observe the spline path drawn with Gizmos in the Scene view.
/// </remarks>
public class CameraSplineController : MonoBehaviour
{
    [Header("Spline Setup")]
    [Tooltip("The camera that will move along the spline. If null, Camera.main will be used.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("A list of Transforms representing the control points of the Catmull-Rom spline. " +
             "You need at least 4 points for the spline to be properly defined.")]
    [SerializeField] private List<Transform> controlPoints = new List<Transform>();

    [Tooltip("An optional Transform for the camera to continuously look at while moving. " +
             "If null, the camera's rotation will not be explicitly controlled by the spline.")]
    [SerializeField] private Transform lookAtTarget;

    [Header("Movement Settings")]
    [Tooltip("The total time in seconds it takes for the camera to traverse the entire spline.")]
    [SerializeField] [Range(0.1f, 120f)] private float travelDuration = 10f; // Time to traverse the entire spline

    [Tooltip("If true, the camera will loop its movement from the end back to the beginning.")]
    [SerializeField] private bool loopSpline = false;

    [Tooltip("If true, the spline movement will start automatically when the script awakes.")]
    [SerializeField] private bool autoStartOnAwake = false;

    // Internal state variables
    private float currentSplineProgress = 0f; // Normalized progress (0 to 1) along the entire spline
    private Coroutine movementCoroutine;       // Reference to the active movement coroutine

    [Header("Gizmo Settings")]
    [Tooltip("Color of the spline path drawn in the Scene view.")]
    [SerializeField] private Color splineColor = Color.green;

    [Tooltip("Color of the control points drawn in the Scene view.")]
    [SerializeField] private Color controlPointColor = Color.red;

    [Tooltip("Radius of the spheres drawn at each control point.")]
    [SerializeField] [Range(0.1f, 1f)] private float gizmoSphereRadius = 0.3f;

    [Tooltip("Number of segments used to draw the spline for visualization. Higher values mean smoother lines.")]
    [SerializeField] [Range(10, 200)] private int gizmoSegments = 50; // Number of segments per spline section to draw

    /// <summary>
    /// Gets a value indicating whether the camera is currently moving along the spline.
    /// </summary>
    public bool IsMoving => movementCoroutine != null;

    /// <summary>
    /// Gets the current normalized progress of the camera along the spline (0 to 1).
    /// </summary>
    public float CurrentSplineProgress => currentSplineProgress;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used for initial setup and validation.
    /// </summary>
    private void Awake()
    {
        // Auto-assign Camera.main if targetCamera is not set
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("CameraSplineController requires a target Camera. Please assign one " +
                               "or ensure a main camera exists in the scene.", this);
                enabled = false; // Disable the script if no camera found
                return;
            }
        }

        // Catmull-Rom spline requires at least 4 control points
        if (controlPoints.Count < 4)
        {
            Debug.LogWarning("CameraSplineController requires at least 4 control points for a smooth Catmull-Rom spline. " +
                             "Add more points in the Inspector.", this);
            enabled = false; // Disable script if not enough points
            return;
        }

        if (autoStartOnAwake)
        {
            StartSplineMovement();
        }
    }

    /// <summary>
    /// OnDrawGizmos is called by Unity to draw gizmos in the Scene view.
    /// This method visualizes the spline path and control points.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Do not draw if there are too few points
        if (controlPoints == null || controlPoints.Count < 2) return;

        // Draw control point spheres
        Gizmos.color = controlPointColor;
        foreach (Transform point in controlPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, gizmoSphereRadius);
            }
        }

        // Need at least 4 points to draw Catmull-Rom spline segments
        if (controlPoints.Count < 4) return;

        // Draw the spline path
        Gizmos.color = splineColor;
        // Iterate through each segment that can be drawn (P1 to P2 using P0, P1, P2, P3)
        for (int i = 0; i < controlPoints.Count - 3; i++)
        {
            // Get the four control points for the current segment
            Vector3 p0 = controlPoints[i].position;
            Vector3 p1 = controlPoints[i + 1].position;
            Vector3 p2 = controlPoints[i + 2].position;
            Vector3 p3 = controlPoints[i + 3].position;

            Vector3 previousPoint = p1; // Start drawing from the actual start of the segment (P1)
            for (int j = 1; j <= gizmoSegments; j++)
            {
                float t = (float)j / gizmoSegments; // Normalized value (0 to 1) for the current segment
                Vector3 currentPoint = GetCatmullRomPosition(t, p0, p1, p2, p3);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
    }

    /// <summary>
    /// Public API: Starts the camera's movement along the defined spline.
    /// If already moving, it stops the current movement and restarts from the beginning.
    /// </summary>
    public void StartSplineMovement()
    {
        if (controlPoints.Count < 4 || targetCamera == null)
        {
            Debug.LogWarning("Cannot start spline movement: insufficient control points or no target camera assigned.", this);
            return;
        }

        StopSplineMovement(); // Stop any existing movement before starting a new one
        currentSplineProgress = 0f; // Reset progress to the beginning of the spline
        movementCoroutine = StartCoroutine(MoveCameraAlongSpline());
    }

    /// <summary>
    /// Public API: Stops the camera's current movement along the spline.
    /// </summary>
    public void StopSplineMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }

    /// <summary>
    /// Public API: Sets the camera's position and rotation directly to a specific point on the spline.
    /// Can be used to jump to a specific part of the path or manually control progress.
    /// </summary>
    /// <param name="progress">Normalized progress along the entire spline (0 to 1).</param>
    public void SetCameraToSplinePosition(float progress)
    {
        if (targetCamera == null || controlPoints.Count < 4) return;

        progress = Mathf.Clamp01(progress); // Ensure progress is between 0 and 1
        currentSplineProgress = progress;

        // Calculate the segment index and the local 't' value within that segment.
        // For N control points, there are (N - 3) drawable Catmull-Rom segments (e.g., 4 points define 1 segment).
        float totalDrawableSegments = controlPoints.Count - 3;
        int segmentIndex = Mathf.FloorToInt(progress * totalDrawableSegments);
        float segmentT = (progress * totalDrawableSegments) - segmentIndex;

        // Clamp segment index to ensure it's within the valid range for the control points
        segmentIndex = Mathf.Clamp(segmentIndex, 0, controlPoints.Count - 4);

        // Get the four control points for the current segment
        Vector3 p0 = controlPoints[segmentIndex].position;
        Vector3 p1 = controlPoints[segmentIndex + 1].position;
        Vector3 p2 = controlPoints[segmentIndex + 2].position;
        Vector3 p3 = controlPoints[segmentIndex + 3].position;

        // Set camera position using the Catmull-Rom interpolation
        targetCamera.transform.position = GetCatmullRomPosition(segmentT, p0, p1, p2, p3);

        // If a lookAtTarget is specified, make the camera look at it
        if (lookAtTarget != null)
        {
            targetCamera.transform.LookAt(lookAtTarget);
        }
        // Optional: If no lookAtTarget, you could calculate a forward direction based on the spline tangent here.
        // For simplicity, we leave the rotation as is if no target, assuming a default orientation or external control.
    }

    /// <summary>
    /// Public API: Sets the total duration for the camera to traverse the entire spline.
    /// </summary>
    /// <param name="newDuration">The new duration in seconds (must be positive).</param>
    public void SetTravelDuration(float newDuration)
    {
        travelDuration = Mathf.Max(0.1f, newDuration); // Ensure duration is not zero or negative
    }

    /// <summary>
    /// Public API: Sets whether the camera should loop its movement along the spline.
    /// </summary>
    /// <param name="loop">True to loop, false otherwise.</param>
    public void SetLooping(bool loop)
    {
        loopSpline = loop;
    }

    /// <summary>
    /// Public API: Sets an optional target for the camera to always look at while moving along the spline.
    /// </summary>
    /// <param name="target">The Transform of the object to look at, or null to disable.</param>
    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
    }

    /// <summary>
    /// Coroutine responsible for moving the camera along the spline over time.
    /// </summary>
    private IEnumerator MoveCameraAlongSpline()
    {
        float startTime = Time.time;
        float normalizedTime = 0f;

        do
        {
            // Calculate normalized time based on the desired travel duration
            normalizedTime = (Time.time - startTime) / travelDuration;

            // Update the camera's position and rotation
            SetCameraToSplinePosition(normalizedTime);

            yield return null; // Wait for the next frame
        }
        while (normalizedTime < 1f); // Continue until the end of the spline is reached

        // After completing one full pass:
        if (loopSpline)
        {
            // If looping, restart the coroutine
            movementCoroutine = StartCoroutine(MoveCameraAlongSpline());
        }
        else
        {
            // If not looping, ensure the camera lands precisely at the end point and clear the coroutine reference
            SetCameraToSplinePosition(1f);
            movementCoroutine = null; // Movement finished
        }
    }

    /// <summary>
    /// Calculates a point on a Catmull-Rom spline segment given four control points.
    /// Catmull-Rom ensures the path passes through P1 and P2, with P0 and P3 acting as tangents.
    /// </summary>
    /// <param name="t">Normalized distance along the segment (0 to 1).</param>
    /// <param name="p0">The first control point (before the start of the segment).</param>
    /// <param name="p1">The start point of the current segment.</param>
    /// <param name="p2">The end point of the current segment.</param>
    /// <param name="p3">The fourth control point (after the end of the segment).</param>
    /// <returns>The interpolated position on the spline.</returns>
    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Clamp t to ensure it's within the valid range [0, 1] for the segment
        t = Mathf.Clamp01(t);

        // Calculate powers of t
        float t2 = t * t;
        float t3 = t2 * t;

        // Catmull-Rom spline formula coefficients:
        // P(t) = 0.5 * ( (2 * P1) + (-P0 + P2) * t + (2 * P0 - 5 * P1 + 4 * P2 - P3) * t^2 + (-P0 + 3 * P1 - 3 * P2 + P3) * t^3 )
        return 0.5f * (
            (2.0f * p1) +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
        );
    }
}
```