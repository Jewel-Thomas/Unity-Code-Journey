// Unity Design Pattern Example: SplineBasedMovement
// This script demonstrates the SplineBasedMovement pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SplineBasedMovement' design pattern in Unity allows GameObjects (characters, enemies, cameras, projectiles, etc.) to traverse a predefined path or curve, often referred to as a "spline." This pattern is incredibly useful for creating guided movements, patrol routes, cinematic camera shots, or any scenario where an object needs to follow a specific, non-linear trajectory.

### Core Components of the SplineBasedMovement Pattern:

1.  **Spline/Path Definition:** This is the most crucial part. It defines the series of points and potentially the mathematical interpolation method (e.g., linear, Bezier, Catmull-Rom) that creates the continuous curve.
2.  **Mover Component:** This component is attached to the GameObject that needs to move. It takes the spline definition, calculates the object's position along the path based on its current progress, speed, and desired movement behavior (looping, ping-pong, one-shot).
3.  **Position Calculation:** A method within the mover that, given a normalized time (usually 0 to 1) along the spline, returns the corresponding world-space position. For smoother curves, this involves complex interpolation mathematics.
4.  **Direction Calculation (Optional but Recommended):** A method that, given a normalized time, returns the forward direction along the spline at that point. This allows the moving object to orient itself correctly.

This example provides a `SplineMover` script that demonstrates this pattern using a list of `Vector3` points for spline definition and linear interpolation with arc-length parameterization for movement, ensuring consistent speed along the path.

---

### Complete C# Unity Example: `SplineMover.cs`

To use this script:
1.  Create a new C# script named `SplineMover` in your Unity project.
2.  Copy and paste the code below into the script.
3.  Attach this script to any GameObject you want to move along a spline (e.g., an empty GameObject, a character model, a camera).
4.  In the Inspector, populate the `Spline Points` list with `Vector3` coordinates. These points are relative to the GameObject's `transform.position`.
5.  Configure `Move Speed`, `Movement Mode`, and `Look Forward` as desired.
6.  Run the scene!

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<Vector3>

/// <summary>
/// Implements the 'SplineBasedMovement' design pattern in Unity.
/// This script allows a GameObject to move along a series of defined 3D points
/// forming a path (spline) in the game world. The movement can loop, ping-pong,
/// or stop after one pass.
/// </summary>
[HelpURL("https://github.com/YourGitHub/UnityDesignPatterns/wiki/SplineBasedMovement")] // Example help link
public class SplineMover : MonoBehaviour
{
    // =====================================================================================
    // Spline Definition:
    // This section defines the path the object will follow.
    // For simplicity, this example uses a list of Vector3 points and linear interpolation
    // between them. For smoother paths, this could be extended to use Bezier or Catmull-Rom
    // spline algorithms.
    // =====================================================================================
    [Header("Spline Configuration")]
    [Tooltip("The local points that define the spline path. These are relative to this GameObject's position.")]
    public List<Vector3> splinePoints = new List<Vector3>();

    [Tooltip("An additional offset applied to the entire spline path. Useful for fine-tuning the path's world position.")]
    public Vector3 splineOffset = Vector3.zero;

    // =====================================================================================
    // Movement Parameters:
    // These settings control how the object moves along the defined spline.
    // =====================================================================================
    [Header("Movement Configuration")]
    [Tooltip("The speed at which the object moves along the spline, in units per second.")]
    [SerializeField]
    private float moveSpeed = 1.0f;

    /// <summary>
    /// Defines the behavior when the object reaches the end of the spline.
    /// - Loop: Restarts from the beginning.
    /// - PingPong: Reverses direction.
    /// - Once: Stops at the end.
    /// </summary>
    public enum MovementMode { Loop, PingPong, Once }
    [Tooltip("How the object behaves when it reaches the end of the spline.")]
    public MovementMode movementMode = MovementMode.Loop;

    [Tooltip("If true, the object will rotate to face the direction of movement.")]
    public bool lookForward = true;

    [Tooltip("The forward axis of the model to align with the movement direction (e.g., Vector3.forward for Z-forward models).")]
    public Vector3 lookForwardAxis = Vector3.forward;

    // =====================================================================================
    // Internal State Variables:
    // These variables manage the current state of the mover along the spline.
    // =====================================================================================
    private float currentPathTime = 0.0f; // Normalized time (0 to 1) along the *entire* path length
    private bool movingForward = true;    // Used specifically for PingPong movement mode
    private float _splineTotalLength = -1f; // Cached total length of the spline for performance

    // =====================================================================================
    // Editor Visualization:
    // These settings control how the spline is drawn in the Unity editor using Gizmos.
    // =====================================================================================
    [Header("Editor Visualization")]
    [Tooltip("Color of the spline gizmos in the editor.")]
    public Color gizmoColor = Color.cyan;
    [Tooltip("Radius of the spheres drawn at each spline point.")]
    public float gizmoSphereRadius = 0.1f;
    [Tooltip("Number of segments to draw between each pair of spline points for a smoother visual approximation of the path.")]
    public int gizmoLineSegments = 20;

    // =====================================================================================
    // MonoBehaviour Lifecycle Methods:
    // Standard Unity callbacks for initialization and frame-by-frame updates.
    // =====================================================================================

    void Start()
    {
        // Basic validation to ensure the spline can be used.
        if (splinePoints == null || splinePoints.Count < 2)
        {
            Debug.LogWarning("SplineMover requires at least 2 spline points to define a path. Disabling script.", this);
            enabled = false; // Disable the script if not enough points are defined.
            return;
        }

        // Initialize the object's position to the start of the spline.
        transform.position = GetPositionOnSpline(0.0f);
        // Calculate and cache the total length of the spline once at start.
        // This is important for ensuring consistent speed regardless of segment lengths.
        _splineTotalLength = CalculateSplineTotalLength();
    }

    void Update()
    {
        // Early exit if the script has been disabled or spline is invalid.
        if (!enabled || splinePoints == null || splinePoints.Count < 2)
        {
            return;
        }

        MoveAlongSpline();
    }

    // =====================================================================================
    // Core Movement Logic:
    // These methods handle the actual movement and path traversal.
    // =====================================================================================

    /// <summary>
    /// Updates the object's position and rotation along the spline based on `moveSpeed` and `Time.deltaTime`.
    /// </summary>
    private void MoveAlongSpline()
    {
        if (_splineTotalLength <= 0.0f)
        {
            // If the spline has no length (e.g., all points are identical), prevent movement.
            return;
        }

        // Calculate the normalized step along the path.
        // `moveSpeed` is in units/second. We divide by `_splineTotalLength` to convert
        // it into a normalized (0-1) change per second, consistent with `currentPathTime`.
        float normalizedStep = (moveSpeed / _splineTotalLength) * Time.deltaTime;

        if (movingForward)
        {
            currentPathTime += normalizedStep;
            // Check if we've reached or passed the end of the path.
            if (currentPathTime >= 1.0f)
            {
                HandlePathEnd();
            }
        }
        else // Moving backward (PingPong mode)
        {
            currentPathTime -= normalizedStep;
            // Check if we've reached or passed the start of the path.
            if (currentPathTime <= 0.0f)
            {
                HandlePathEnd();
            }
        }

        // Get the interpolated position and direction at the current time.
        Vector3 newPosition = GetPositionOnSpline(currentPathTime);
        Vector3 lookDirection = GetDirectionOnSpline(currentPathTime);

        // Apply the new position to the GameObject.
        transform.position = newPosition;

        // Apply rotation if 'lookForward' is enabled and there's a valid direction.
        if (lookForward && lookDirection.sqrMagnitude > 0.0001f) // Check sqrMagnitude for performance
        {
            // Create a rotation that makes the object's forward axis (lookForwardAxis)
            // point in the calculated lookDirection.
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up) * Quaternion.FromToRotation(lookForwardAxis, Vector3.forward);
        }
    }

    /// <summary>
    /// Handles the behavior when the object reaches the end (or start, in PingPong) of the spline,
    /// based on the `movementMode` setting.
    /// </summary>
    private void HandlePathEnd()
    {
        switch (movementMode)
        {
            case MovementMode.Loop:
                currentPathTime = 0.0f; // Reset to the beginning of the path.
                break;
            case MovementMode.PingPong:
                movingForward = !movingForward; // Reverse the direction of movement.
                // Clamp time to ensure it's exactly 0 or 1 at the turning point.
                currentPathTime = Mathf.Clamp01(currentPathTime);
                break;
            case MovementMode.Once:
                currentPathTime = 1.0f; // Ensure it stays at the end point.
                enabled = false;        // Disable the script to stop further movement.
                break;
        }
    }

    // =====================================================================================
    // Spline Calculation Methods:
    // These methods provide the core functionality for querying the spline's properties.
    // =====================================================================================

    /// <summary>
    /// Calculates a position along the spline based on a normalized time value (t).
    /// This implementation uses linear interpolation between control points,
    /// parameterized by arc length to ensure constant speed along the path segments.
    /// For smoother curves, this method would be replaced with Bezier, Catmull-Rom, etc.
    /// </summary>
    /// <param name="t">Normalized time (0 to 1) along the entire spline.</param>
    /// <returns>The interpolated position in world space.</returns>
    public Vector3 GetPositionOnSpline(float t)
    {
        // Handle invalid spline or edge cases where t is out of bounds.
        if (splinePoints == null || splinePoints.Count < 2 || _splineTotalLength <= 0.0f)
        {
            // If invalid, return the current transform position or the first point.
            return GetWorldSplinePoint(0);
        }

        // Clamp 't' to ensure it stays within the valid range [0, 1].
        t = Mathf.Clamp01(t);

        // Determine the target length along the spline corresponding to 't'.
        float targetLength = t * _splineTotalLength;
        float currentAccumulatedLength = 0f;

        // Iterate through each segment of the spline to find which segment 't' falls into.
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Vector3 p1 = GetWorldSplinePoint(i);
            Vector3 p2 = GetWorldSplinePoint(i + 1);
            float segmentLength = Vector3.Distance(p1, p2);

            // Check if the target length falls within the current segment.
            // Or if it's the last segment, assume it must be in this one.
            if (targetLength <= currentAccumulatedLength + segmentLength + float.Epsilon || i == splinePoints.Count - 2)
            {
                // Calculate the progress (0-1) within this specific segment.
                float segmentProgress = (targetLength - currentAccumulatedLength) / segmentLength;
                // Linearly interpolate between the two points of this segment.
                return Vector3.Lerp(p1, p2, segmentProgress);
            }
            currentAccumulatedLength += segmentLength;
        }

        // Fallback: If somehow t=1.0 and loop didn't catch, return the last point.
        return GetWorldSplinePoint(splinePoints.Count - 1);
    }

    /// <summary>
    /// Calculates the forward direction along the spline at a given normalized time.
    /// It does this by sampling a point slightly ahead (or behind, if at the very end)
    /// and computing the vector between the current and sampled point.
    /// </summary>
    /// <param name="t">Normalized time (0 to 1) along the entire spline.</param>
    /// <returns>The normalized direction vector in world space.</returns>
    public Vector3 GetDirectionOnSpline(float t)
    {
        if (splinePoints == null || splinePoints.Count < 2 || _splineTotalLength <= 0.0f)
        {
            return Vector3.forward; // Default direction if spline is invalid.
        }

        const float sampleDelta = 0.01f; // A small step to sample for direction.

        Vector3 currentPos = GetPositionOnSpline(t);
        Vector3 nextPos;

        // If we are very close to the end of the path, sample slightly *behind* to get a valid direction.
        if (t >= 1.0f - sampleDelta / 2f)
        {
            nextPos = currentPos; // 'nextPos' becomes the current position
            currentPos = GetPositionOnSpline(Mathf.Max(0f, t - sampleDelta)); // 'currentPos' samples just before
        }
        else // Otherwise, sample slightly *ahead* to get the forward direction.
        {
            nextPos = GetPositionOnSpline(Mathf.Min(1f, t + sampleDelta));
        }

        Vector3 direction = nextPos - currentPos;

        // Return normalized direction, or a default forward if the direction is negligible.
        if (direction.sqrMagnitude > 0.0001f) // Use sqrMagnitude for performance.
        {
            return direction.normalized;
        }
        return Vector3.forward; // Default if no discernible direction (e.g., consecutive points are identical).
    }

    /// <summary>
    /// Calculates the total cumulative length of all segments in the spline.
    /// This is used for arc-length parameterization to ensure constant speed.
    /// </summary>
    /// <returns>The total length of the spline path.</returns>
    private float CalculateSplineTotalLength()
    {
        if (splinePoints == null || splinePoints.Count < 2)
        {
            return 0f; // A spline needs at least 2 points to have length.
        }

        float totalLength = 0f;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            // Sum up the distance between consecutive world spline points.
            totalLength += Vector3.Distance(GetWorldSplinePoint(i), GetWorldSplinePoint(i + 1));
        }
        return totalLength;
    }

    /// <summary>
    /// Converts a local spline point (defined in `splinePoints` list) to its world space equivalent.
    /// It applies the `splineOffset` and considers the `SplineMover`'s own `transform.position`.
    /// </summary>
    /// <param name="index">The index of the spline point in the `splinePoints` list.</param>
    /// <returns>The world space position of the spline point.</returns>
    private Vector3 GetWorldSplinePoint(int index)
    {
        // Spline points are defined relative to the SplineMover's transform.
        // Add transform.position to make them world coordinates, then apply splineOffset.
        return transform.position + splinePoints[index] + splineOffset;
    }

    // =====================================================================================
    // Editor Visualization (Gizmos):
    // These methods draw visual aids in the scene view to help design and inspect the spline.
    // =====================================================================================

    void OnDrawGizmos()
    {
        // Only draw gizmos if there are enough points defined.
        if (splinePoints == null || splinePoints.Count < 2)
        {
            return;
        }

        Gizmos.color = gizmoColor;

        // Draw a sphere at each defined spline point.
        for (int i = 0; i < splinePoints.Count; i++)
        {
            Gizmos.DrawSphere(GetWorldSplinePoint(i), gizmoSphereRadius);
        }

        // Draw straight lines connecting the raw spline points.
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(GetWorldSplinePoint(i), GetWorldSplinePoint(i + 1));
        }

        // Draw a smoother approximation of the path using `GetPositionOnSpline`
        // to show how the object will actually move.
        if (splinePoints.Count >= 2 && gizmoLineSegments > 0)
        {
            Gizmos.color = gizmoColor * 0.7f; // Slightly dimmer color for the interpolated path.
            Vector3 previousPosition = GetPositionOnSpline(0f);
            // Iterate many times to draw a smooth line over the entire path length (0-1).
            for (int i = 1; i <= gizmoLineSegments * (splinePoints.Count - 1); i++)
            {
                float t = (float)i / (gizmoLineSegments * (splinePoints.Count - 1));
                Vector3 currentPosition = GetPositionOnSpline(t);
                Gizmos.DrawLine(previousPosition, currentPosition);
                previousPosition = currentPosition;
            }
        }
    }
}
```

---

### Example Usage and Setup in Unity

1.  **Create a Mover GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> "Create Empty"). Name it `PathFollower`.
    *   You can add a visual representation to it, like a simple Cube (right-click `PathFollower` -> "3D Object" -> "Cube") or any model you wish to move. Reset the Cube's position relative to `PathFollower` if needed.

2.  **Attach the `SplineMover` Script:**
    *   Select the `PathFollower` GameObject.
    *   Drag and drop the `SplineMover.cs` script onto it in the Inspector, or click "Add Component" and search for `SplineMover`.

3.  **Define the Spline Path:**
    *   In the Inspector, with `PathFollower` selected, locate the `Spline Mover` component.
    *   Under "Spline Configuration," you'll see "Spline Points."
    *   Increase the `Size` of the `Spline Points` list (e.g., to 4 for a simple square).
    *   Enter `Vector3` coordinates for each element. **These points are relative to the `PathFollower` GameObject's current position.**
        *   **Example for a square path relative to `PathFollower`'s initial position:**
            *   Element 0: `(0, 0, 0)`
            *   Element 1: `(5, 0, 0)`
            *   Element 2: `(5, 0, 5)`
            *   Element 3: `(0, 0, 5)`
        *   You'll see the Gizmos appear in the Scene view, showing your path.

4.  **Configure Movement:**
    *   **Move Speed:** Set this to your desired speed (e.g., `2.0` units per second).
    *   **Movement Mode:** Choose `Loop` (repeats the path), `PingPong` (moves back and forth), or `Once` (stops at the end).
    *   **Look Forward:** Check this if you want your GameObject to rotate and face the direction it's moving.
    *   **Look Forward Axis:** If `Look Forward` is enabled, and your model's "forward" isn't along the Z-axis (e.g., a 2D sprite might use `Vector3.up`), adjust this. For most 3D models, `Vector3.forward` is correct.

5.  **Run the Scene:**
    *   Press the Play button. Your `PathFollower` GameObject (or its child visual) will now move along the defined spline path.

### Extending for Smoother Splines (Advanced)

The current `GetPositionOnSpline` method uses linear interpolation, creating sharp corners at each control point. For smoother, more natural-looking paths, you would replace or augment the `GetPositionOnSpline` (and possibly `GetDirectionOnSpline`) logic with a more advanced spline algorithm:

*   **Bezier Curves:** Offer precise control with anchor points and tangent handles.
*   **Catmull-Rom Splines:** Pass through all control points and provide a smooth curve, often used for camera paths or simple rail systems. They usually require 4 points to define a segment (two segment points and two "tension" points outside the segment).

To implement these, you would:
1.  **Change `GetPositionOnSpline(float t)`:** Modify this method to use the mathematical formula for the chosen spline type. You would need to determine which set of 4 control points (for Catmull-Rom) or anchor/tangent points (for Bezier) corresponds to the `t` value and then apply the respective interpolation formula.
2.  **Adjust `GetDirectionOnSpline(float t)`:** For true spline curves, the direction can often be derived more accurately by taking the derivative of the spline function. Alternatively, the sampling approach used in this example remains a good approximation.
3.  **Consider a dedicated `Spline` data class:** For complex splines, it's often beneficial to encapsulate the spline points and interpolation logic in a separate `Spline` class or ScriptableObject, allowing multiple `SplineMover` instances to reference the same path.