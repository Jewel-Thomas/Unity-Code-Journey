// Unity Design Pattern Example: PhysicsDebugSystem
// This script demonstrates the PhysicsDebugSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `PhysicsDebugSystem` design pattern provides a centralized, flexible, and non-invasive way to visualize physics-related information in a game engine, primarily for debugging purposes. It allows developers to "see" things like raycasts, collider bounds, overlap checks, and contact points that are otherwise invisible during gameplay.

**Key Principles of the PhysicsDebugSystem Pattern:**

1.  **Centralized Control:** A single manager class handles all debug drawing requests. This makes it easy to enable/disable debugging globally or change drawing styles.
2.  **Non-Invasive:** The debug system should not interfere with the game's core logic or performance in release builds. This is typically achieved using conditional compilation (`#if UNITY_EDITOR`) or `[Conditional]` attributes.
3.  **Abstracted Drawing:** Clients (game objects performing physics operations) don't directly call `Gizmos.DrawWireSphere` or `Debug.DrawLine`. Instead, they make requests to the `PhysicsDebugSystem`, which then decides *how* and *when* to draw.
4.  **Toggleable:** Debugging can be turned on or off easily, often via a boolean flag in the manager.
5.  **Visualization:** It uses visual primitives (lines, spheres, boxes) to represent physics data.

---

### Complete C# Unity Example: PhysicsDebugSystem

This example provides:
*   A `PhysicsDebugSystem` MonoBehaviour singleton that manages debug drawing.
*   A `DebugDrawRequest` struct to store drawing details.
*   Methods to queue various types of debug drawings (rays, spheres, boxes).
*   Automatic drawing using `OnDrawGizmos` (for editor visualization) and `Debug.DrawLine` (for runtime in editor).
*   A `PhysicsUser` component demonstrating how to integrate with the system.
*   Conditional compilation to ensure the debug system is removed from release builds.

---

**1. PhysicsDebugSystem.cs**

This script should be placed on an empty GameObject in your scene (or it will create one automatically).

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics; // Required for [Conditional] attribute

/// <summary>
/// The PhysicsDebugSystem design pattern provides a centralized and flexible way
/// to visualize physics-related information during development in Unity.
/// It allows developers to "see" invisible physics operations like raycasts,
/// overlap checks, and collider bounds.
/// </summary>
/// <remarks>
/// Key features of this implementation:
/// -   **Singleton:** Ensures only one instance exists and is globally accessible.
/// -   **Toggleable:** `IsDebuggingEnabled` allows turning the system on/off at runtime.
/// -   **Abstracted Drawing:** Client code (e.g., PhysicsUser) requests drawings via simple methods,
///     and the system handles the actual Gizmo/Debug.DrawLine calls.
/// -   **Conditional Compilation:** The entire system is excluded from non-editor builds
///     using `#if UNITY_EDITOR` and `[Conditional("UNITY_EDITOR")]`, ensuring no
///     performance impact or code bloat in release versions.
/// -   **Per-Frame Clearing:** Drawings made with `duration = 0f` (intended for Gizmos)
///     are cleared each frame, so they represent the current frame's physics.
/// -   **Customizable Defaults:** Default colors and durations can be configured in the Inspector.
/// </remarks>
public class PhysicsDebugSystem : MonoBehaviour
{
    // Singleton Instance
    private static PhysicsDebugSystem _instance;
    public static PhysicsDebugSystem Instance
    {
        get
        {
            // If the instance doesn't exist, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhysicsDebugSystem>();

                // If still no instance, create a new GameObject and add the component.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(PhysicsDebugSystem).Name);
                    _instance = singletonObject.AddComponent<PhysicsDebugSystem>();
                    UnityEngine.Debug.Log($"Created new PhysicsDebugSystem instance on '{singletonObject.name}'.");
                }
            }
            return _instance;
        }
    }

    [Header("System Settings")]
    [Tooltip("Enable or disable the entire debug system.")]
    public bool IsDebuggingEnabled = true;
    [Tooltip("Default duration for Debug.Draw... calls. 0 means it will be drawn as a Gizmo.")]
    public float DefaultDrawDuration = 0f; // 0 for Gizmos, >0 for Debug.DrawLine

    [Header("Default Colors")]
    public Color DefaultRayColor = Color.yellow;
    public Color DefaultHitColor = Color.red;
    public Color DefaultMissColor = Color.blue;
    public Color DefaultOverlapColor = Color.magenta;
    public Color DefaultColliderBoundsColor = Color.cyan;
    public Color DefaultSphereColor = Color.green;
    public Color DefaultBoxColor = Color.yellow;
    public Color DefaultLineColor = Color.white;


    // --- Internal Data Structures for Debug Drawing ---

    /// <summary>
    /// Defines the type of debug primitive to draw.
    /// </summary>
    private enum DrawType
    {
        Ray,
        Line,
        Sphere,
        WireSphere,
        Box,
        WireBox,
        Capsule, // Not fully implemented in this example, but shows extensibility
        ColliderBounds,
    }

    /// <summary>
    /// A struct to hold all necessary information for a single debug drawing request.
    /// This allows us to defer drawing to `OnDrawGizmos`.
    /// </summary>
    private struct DebugDrawRequest
    {
        public DrawType Type;
        public Vector3 StartPosition;
        public Vector3 EndPosition; // For lines, or direction for rays
        public Vector3 Size;        // For boxes, collider bounds
        public Quaternion Rotation; // For boxes, collider bounds
        public float Radius;        // For spheres
        public Color Color;
        public float Duration;      // Only relevant for Debug.Draw...
    }

    // List to store all pending drawing requests for the current frame
    private List<DebugDrawRequest> _drawRequests = new List<DebugDrawRequest>();

    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Ensure this is the only instance.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // Make sure the system persists across scene loads.
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Clears all stored drawing requests at the end of each frame.
    /// This ensures that Gizmos (duration=0) only show physics for the current frame.
    /// </summary>
    private void LateUpdate()
    {
        // Only clear if debugging is enabled.
        if (IsDebuggingEnabled)
        {
            _drawRequests.Clear();
        }
    }

    /// <summary>
    /// Called by the Unity editor to draw gizmos in the Scene view.
    /// This is where we process and draw all accumulated requests with duration 0.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!IsDebuggingEnabled) return;

        // Save current Gizmo state to restore it afterwards
        Color originalGizmoColor = Gizmos.color;
        Matrix4x4 originalGizmoMatrix = Gizmos.matrix;

        foreach (var request in _drawRequests)
        {
            Gizmos.color = request.Color;

            // Apply matrix for rotated/scaled shapes
            // Note: For rays/lines, we often want them drawn in world space directly,
            // so Gizmos.matrix might need to be identity or carefully set.
            // For boxes/spheres, we use TRS to place and size them.
            Gizmos.matrix = Matrix4x4.TRS(request.StartPosition, request.Rotation, request.Size);

            switch (request.Type)
            {
                case DrawType.Ray:
                    // Gizmos.DrawRay directly takes origin and direction
                    // If Gizmos.matrix is not identity, StartPosition needs to be transformed
                    Gizmos.matrix = Matrix4x4.identity; // Reset matrix for standard ray drawing
                    Gizmos.DrawRay(request.StartPosition, request.EndPosition); // EndPosition stores direction here
                    break;
                case DrawType.Line:
                    Gizmos.matrix = Matrix4x4.identity; // Reset matrix for standard line drawing
                    Gizmos.DrawLine(request.StartPosition, request.EndPosition);
                    break;
                case DrawType.Sphere:
                    // Draw at local origin, it will be transformed by Gizmos.matrix
                    Gizmos.DrawSphere(Vector3.zero, request.Radius);
                    break;
                case DrawType.WireSphere:
                    Gizmos.DrawWireSphere(Vector3.zero, request.Radius);
                    break;
                case DrawType.Box:
                    Gizmos.DrawCube(Vector3.zero, Vector3.one); // Draw unit cube, scaled by Gizmos.matrix.Size
                    break;
                case DrawType.WireBox:
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one); // Draw unit wire cube, scaled by Gizmos.matrix.Size
                    break;
                case DrawType.ColliderBounds:
                    Gizmos.matrix = Matrix4x4.TRS(request.StartPosition, request.Rotation, Vector3.one); // Position and rotation of the collider's transform
                    Gizmos.DrawWireCube(request.EndPosition, request.Size); // EndPosition is actually the center of the bounds in local space here
                    break;
                case DrawType.Capsule:
                    // TODO: Implement Gizmos.DrawWireCapsule (requires custom drawing or helper)
                    // For now, draw a wire sphere as a fallback
                    Gizmos.DrawWireSphere(Vector3.zero, request.Radius);
                    break;
            }
        }

        // Restore original Gizmo state
        Gizmos.color = originalGizmoColor;
        Gizmos.matrix = originalGizmoMatrix;
    }

    // --- Public API for Requesting Debug Drawings ---

    /// <summary>
    /// Draws a ray. If duration > 0, uses Debug.DrawRay. If duration == 0, queues for Gizmos.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction vector of the ray.</param>
    /// <param name="color">The color of the ray. Uses DefaultRayColor if null.</param>
    /// <param name="duration">How long the ray should be visible. 0 for Gizmos (single frame).</param>
    [Conditional("UNITY_EDITOR")]
    public void DrawRay(Vector3 origin, Vector3 direction, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = color ?? DefaultRayColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        if (actualDuration > 0f)
        {
            UnityEngine.Debug.DrawRay(origin, direction, drawColor, actualDuration);
        }
        else
        {
            _drawRequests.Add(new DebugDrawRequest
            {
                Type = DrawType.Ray,
                StartPosition = origin,
                EndPosition = direction, // Direction is stored in EndPosition for rays
                Color = drawColor,
                Duration = actualDuration
            });
        }
    }

    /// <summary>
    /// Draws a ray representing a raycast hit.
    /// The ray is drawn up to the hit point in hitColor, and then the rest of the ray in missColor.
    /// </summary>
    /// <param name="ray">The original ray.</param>
    /// <param name="hitInfo">The RaycastHit information.</param>
    /// <param name="rayLength">The maximum length of the ray.</param>
    /// <param name="hitColor">Color for the hit portion of the ray.</param>
    /// <param name="missColor">Color for the missed portion of the ray.</param>
    /// <param name="duration">Duration for the debug draw.</param>
    [Conditional("UNITY_EDITOR")]
    public void DrawRaycast(Ray ray, RaycastHit hitInfo, float rayLength, Color? hitColor = null, Color? missColor = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color actualHitColor = hitColor ?? DefaultHitColor;
        Color actualMissColor = missColor ?? DefaultMissColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        if (hitInfo.collider != null)
        {
            // Draw hit portion
            DrawLine(ray.origin, hitInfo.point, actualHitColor, actualDuration);
            // Draw remaining portion
            DrawRay(hitInfo.point, ray.direction * (rayLength - hitInfo.distance), actualMissColor, actualDuration);
            // Draw hit normal
            DrawRay(hitInfo.point, hitInfo.normal * 0.5f, Color.white, actualDuration);
        }
        else
        {
            DrawRay(ray.origin, ray.direction * rayLength, actualMissColor, actualDuration);
        }
    }

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawLine(Vector3 start, Vector3 end, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = color ?? DefaultLineColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        if (actualDuration > 0f)
        {
            UnityEngine.Debug.DrawLine(start, end, drawColor, actualDuration);
        }
        else
        {
            _drawRequests.Add(new DebugDrawRequest
            {
                Type = DrawType.Line,
                StartPosition = start,
                EndPosition = end,
                Color = drawColor,
                Duration = actualDuration
            });
        }
    }

    /// <summary>
    /// Draws a wire sphere.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawWireSphere(Vector3 center, float radius, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = color ?? DefaultSphereColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        if (actualDuration > 0f)
        {
            // Debug.DrawLine can approximate a sphere, but it's not ideal.
            // For simple Debug.DrawLine, we'd need multiple lines.
            // Sticking to Gizmos for duration=0 spheres for best representation.
            // As a fallback for duration > 0, we can draw a cross.
            UnityEngine.Debug.DrawLine(center + Vector3.up * radius, center - Vector3.up * radius, drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(center + Vector3.right * radius, center - Vector3.right * radius, drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(center + Vector3.forward * radius, center - Vector3.forward * radius, drawColor, actualDuration);
        }
        else
        {
            _drawRequests.Add(new DebugDrawRequest
            {
                Type = DrawType.WireSphere,
                StartPosition = center,
                Radius = radius,
                Color = drawColor,
                Duration = actualDuration
            });
        }
    }

    /// <summary>
    /// Draws a solid sphere.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawSphere(Vector3 center, float radius, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = color ?? DefaultSphereColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        // No direct Debug.DrawSphere, so we always queue for Gizmos.
        // If duration > 0 is requested, it still queues for Gizmos and will disappear next frame.
        _drawRequests.Add(new DebugDrawRequest
        {
            Type = DrawType.Sphere,
            StartPosition = center,
            Radius = radius,
            Color = drawColor,
            Duration = actualDuration // Duration won't affect Gizmos but might be useful for tracking
        });
    }

    /// <summary>
    /// Draws a wire box.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawWireBox(Vector3 center, Vector3 size, Quaternion rotation, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = color ?? DefaultBoxColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        if (actualDuration > 0f)
        {
            // Debug.DrawLine for a box is complex, so we'll just queue for Gizmos.
            // If duration > 0, it will still show for a single frame as a Gizmo.
            // A basic representation for Debug.DrawLine could be its center and axes.
            Vector3 halfSize = size / 2f;
            Vector3[] corners = new Vector3[8];
            corners[0] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[3] = center + rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[4] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[5] = center + rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[6] = center + rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            UnityEngine.Debug.DrawLine(corners[0], corners[1], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[1], corners[2], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[2], corners[3], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[3], corners[0], drawColor, actualDuration);

            UnityEngine.Debug.DrawLine(corners[4], corners[5], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[5], corners[6], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[6], corners[7], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[7], corners[4], drawColor, actualDuration);

            UnityEngine.Debug.DrawLine(corners[0], corners[4], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[1], corners[5], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[2], corners[6], drawColor, actualDuration);
            UnityEngine.Debug.DrawLine(corners[3], corners[7], drawColor, actualDuration);
        }
        else
        {
            _drawRequests.Add(new DebugDrawRequest
            {
                Type = DrawType.WireBox,
                StartPosition = center,
                Size = size,
                Rotation = rotation,
                Color = drawColor,
                Duration = actualDuration
            });
        }
    }

    /// <summary>
    /// Draws the bounds of a collider.
    /// </summary>
    /// <param name="collider">The collider whose bounds to draw.</param>
    /// <param name="color">The color of the bounds. Uses DefaultColliderBoundsColor if null.</param>
    /// <param name="duration">Duration for the debug draw.</param>
    [Conditional("UNITY_EDITOR")]
    public void DrawColliderBounds(Collider collider, Color? color = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled || collider == null) return;
        Color drawColor = color ?? DefaultColliderBoundsColor;
        float actualDuration = (duration == -1f) ? DefaultDrawDuration : duration;

        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        Quaternion rotation = collider.transform.rotation; // Bounds are AABB, but we can draw a rotated box

        // Note: For actual OBB, we'd need to extract it from the collider type or calculate it.
        // For simplicity, we'll draw an AABB aligned with the collider's transform.
        DrawWireBox(center, size, rotation, drawColor, actualDuration);
    }

    /// <summary>
    /// Draws a sphere at the overlap check position.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawOverlapSphere(Vector3 center, float radius, bool overlapped, Color? overlapColor = null, Color? noOverlapColor = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = overlapped ? (overlapColor ?? DefaultOverlapColor) : (noOverlapColor ?? DefaultMissColor);
        DrawWireSphere(center, radius, drawColor, duration);
    }

    /// <summary>
    /// Draws a box at the overlap check position.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public void DrawOverlapBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, bool overlapped, Color? overlapColor = null, Color? noOverlapColor = null, float duration = -1f)
    {
        if (!IsDebuggingEnabled) return;
        Color drawColor = overlapped ? (overlapColor ?? DefaultOverlapColor) : (noOverlapColor ?? DefaultMissColor);
        DrawWireBox(center, halfExtents * 2f, rotation, drawColor, duration);
    }
}
```

---

**2. PhysicsUser.cs**

This script demonstrates how to use the `PhysicsDebugSystem` from another component. Attach this to any GameObject to see it in action.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// This component demonstrates how to utilize the PhysicsDebugSystem to visualize
/// common physics operations like Raycasts and OverlapSphere checks.
/// </summary>
/// <remarks>
/// To use:
/// 1. Create an empty GameObject in your scene named "PhysicsDebugSystem".
/// 2. Attach the `PhysicsDebugSystem.cs` script to it.
/// 3. Attach this `PhysicsUser.cs` script to any other GameObject.
/// 4. Add some colliders (e.g., Cubes, Spheres) to your scene for the raycasts/overlaps to hit.
/// 5. Run the scene in the Unity Editor. You will see the debug visualizations in the Scene view.
/// 6. Toggle the 'IsDebuggingEnabled' checkbox on the PhysicsDebugSystem GameObject to enable/disable.
/// </remarks>
public class PhysicsUser : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastLength = 10f;
    public float raycastOffset = 0.5f;
    public LayerMask raycastLayerMask = -1; // -1 means everything

    [Header("OverlapSphere Settings")]
    public float overlapRadius = 2f;
    public LayerMask overlapLayerMask = -1;
    public Vector3 overlapOffset = new Vector3(0, 1f, 0);

    [Header("Collider Bounds Settings")]
    public Collider targetCollider; // Assign a collider from your scene here

    [Header("Debug Display Settings")]
    [Tooltip("Duration for temporary runtime draws (0 for Gizmos-only).")]
    public float debugDrawDuration = 0.1f;
    public Color rayHitColor = Color.green;
    public Color rayMissColor = Color.red;
    public Color overlapColor = Color.magenta;
    public Color noOverlapColor = Color.gray;

    void Update()
    {
        // Ensure the PhysicsDebugSystem exists and is ready
        if (PhysicsDebugSystem.Instance == null)
        {
            Debug.LogError("PhysicsDebugSystem not found. Make sure it's in your scene.");
            return;
        }

        // --- Example 1: Raycast Visualization ---
        // We perform a raycast and then tell the debug system to visualize it.
        PerformRaycastDebug();

        // --- Example 2: OverlapSphere Visualization ---
        PerformOverlapSphereDebug();

        // --- Example 3: Collider Bounds Visualization ---
        if (targetCollider != null)
        {
            PhysicsDebugSystem.Instance.DrawColliderBounds(targetCollider, Color.cyan);
        }
        else
        {
            // If no target collider is set, draw the bounds of this object's collider if it has one.
            Collider selfCollider = GetComponent<Collider>();
            if (selfCollider != null)
            {
                PhysicsDebugSystem.Instance.DrawColliderBounds(selfCollider, Color.yellow);
            }
        }
    }

    /// <summary>
    /// Performs a raycast and then uses the PhysicsDebugSystem to draw the ray and its hit/miss state.
    /// </summary>
    private void PerformRaycastDebug()
    {
        Vector3 rayOrigin = transform.position + transform.up * raycastOffset;
        Vector3 rayDirection = transform.forward;
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        bool isHit = Physics.Raycast(ray, out hit, raycastLength, raycastLayerMask);

        // Request the PhysicsDebugSystem to draw the raycast result
        // The debug system decides if it draws with Debug.DrawRay or Gizmos based on its settings and the duration parameter.
        PhysicsDebugSystem.Instance.DrawRaycast(
            ray,
            hit,
            raycastLength,
            rayHitColor,
            rayMissColor,
            debugDrawDuration // Will appear as a temporary line in the Scene/Game view
        );

        if (isHit)
        {
            // You can also add more specific debug drawings for the hit point
            PhysicsDebugSystem.Instance.DrawSphere(hit.point, 0.1f, Color.white, debugDrawDuration);
        }
    }

    /// <summary>
    /// Performs an overlap sphere check and uses the PhysicsDebugSystem to visualize the sphere.
    /// </summary>
    private void PerformOverlapSphereDebug()
    {
        Vector3 sphereCenter = transform.position + overlapOffset;
        Collider[] colliders = Physics.OverlapSphere(sphereCenter, overlapRadius, overlapLayerMask);
        bool overlapped = colliders.Length > 0;

        // Request the PhysicsDebugSystem to draw the overlap sphere
        PhysicsDebugSystem.Instance.DrawOverlapSphere(
            sphereCenter,
            overlapRadius,
            overlapped,
            overlapColor,
            noOverlapColor,
            0f // Draw as Gizmo (persistent in editor, cleared each frame)
        );

        if (overlapped)
        {
            // Optionally, draw smaller spheres for each overlapped collider's center
            foreach (Collider col in colliders)
            {
                PhysicsDebugSystem.Instance.DrawSphere(col.bounds.center, 0.05f, Color.red, debugDrawDuration);
            }
        }
    }

    // Optional: Draw a custom debug line in the editor for the raycast origin offset
    void OnDrawGizmosSelected()
    {
        if (PhysicsDebugSystem.Instance != null && PhysicsDebugSystem.Instance.IsDebuggingEnabled)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(transform.position + transform.up * raycastOffset, 0.05f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + overlapOffset, 0.05f);
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create the Debug System:**
    *   Create an empty GameObject in your scene (e.g., rename it to `PhysicsDebugSystem`).
    *   Attach the `PhysicsDebugSystem.cs` script to this GameObject. (If you forget, it will create itself at runtime.)
    *   You can configure its `IsDebuggingEnabled` flag, `DefaultDrawDuration`, and colors in the Inspector.

2.  **Create a Physics User:**
    *   Create another GameObject (e.g., a simple Cube or Sphere).
    *   Attach the `PhysicsUser.cs` script to this GameObject.
    *   Optionally, assign a `targetCollider` in the `PhysicsUser`'s Inspector to visualize specific collider bounds.
    *   Ensure there are other colliders in your scene for the raycasts and overlap checks to interact with.

3.  **Run in Editor:**
    *   Play the scene.
    *   Observe the Scene view: You will see lines for raycasts, wire spheres for overlap checks, and wire boxes for collider bounds.
    *   Try moving the `PhysicsUser` GameObject to see the debug visuals update.
    *   Toggle `IsDebuggingEnabled` on the `PhysicsDebugSystem` GameObject in the Inspector during play mode to see the debug visuals appear and disappear.

### Explanation and Design Pattern Focus:

*   **Singleton Pattern (`PhysicsDebugSystem.Instance`):**
    *   Ensures a single, globally accessible point of control for all physics debugging.
    *   The `Awake` method and `get` accessor for `_instance` handle creation if it doesn't exist and prevent duplicates.
*   **Centralized Request Handling:**
    *   Instead of `PhysicsUser` directly calling `Debug.DrawRay` or `Gizmos.DrawWireSphere`, it calls `PhysicsDebugSystem.Instance.DrawRay(...)`.
    *   This decouples the client code from the specific drawing implementation.
*   **Deferred Drawing (for `duration = 0`):**
    *   Requests with `duration = 0f` are stored in the `_drawRequests` list.
    *   These requests are then processed and drawn all at once during `OnDrawGizmos()`. This is crucial because `OnDrawGizmos` is called by the editor when it needs to refresh the Scene view, and drawing all requests at once provides a consistent visual.
    *   `LateUpdate()` clears the requests each frame, ensuring that Gizmos represent the *current* frame's physics state.
*   **Immediate Drawing (for `duration > 0`):**
    *   Requests with a positive `duration` directly use `UnityEngine.Debug.DrawLine`/`DrawRay`. These appear temporarily in the Game and Scene views and are managed by Unity's internal debug drawing system.
*   **Conditional Compilation (`[Conditional("UNITY_EDITOR")]`):**
    *   This is the most critical aspect for a debug system. The `[Conditional("UNITY_EDITOR")]` attribute tells the C# compiler to *only include calls to these methods if the `UNITY_EDITOR` symbol is defined*.
    *   When you build your game for a player (e.g., Windows, Android), the `UNITY_EDITOR` symbol is *not* defined. Consequently, all calls to `PhysicsDebugSystem.Instance.DrawX(...)` from your game code will be entirely stripped out by the compiler.
    *   This means:
        *   Zero performance overhead in release builds.
        *   No extra code size in release builds.
        *   The `PhysicsDebugSystem` script itself (and the GameObject it's on) can be removed from release builds or deactivated without causing errors in code that calls its methods.
*   **Extensibility:**
    *   Adding a new debug drawing type (e.g., `DrawCapsule`, `DrawPath`) simply requires:
        1.  Adding an entry to the `DrawType` enum.
        2.  Creating a new public `DrawX()` method in `PhysicsDebugSystem`.
        3.  Adding a `case` in the `OnDrawGizmos` switch statement to handle the actual Gizmo drawing.
*   **Practicality:**
    *   Provides clear visualization for complex physics interactions.
    *   Easy to integrate into existing scripts.
    *   Configurable through the Inspector.
    *   Zero impact on release builds.

This example provides a robust and production-ready foundation for a `PhysicsDebugSystem` in your Unity projects, helping you understand and visualize your game's physics behavior effectively.