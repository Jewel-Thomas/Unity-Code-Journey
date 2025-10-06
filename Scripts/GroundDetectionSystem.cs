// Unity Design Pattern Example: GroundDetectionSystem
// This script demonstrates the GroundDetectionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'GroundDetectionSystem' is a design pattern in game development (more of a system architecture pattern than a traditional GoF pattern) that centralizes and encapsulates all logic related to determining if a character or object is on the ground. This makes ground detection modular, reusable, and easy to configure for different entities in a game.

### GroundDetectionSystem Pattern Explained:

1.  **Encapsulation:** All ground detection parameters (layers, distances, offsets, detection method) and logic are contained within a single component. This means other systems (e.g., player controller, AI, physics) don't need to know *how* ground detection is performed, only *if* the object is grounded.
2.  **Reusability:** The system can be attached to any `GameObject` that requires ground detection (player character, enemies, movable objects) without code duplication.
3.  **Configurability:** Designers and developers can easily tweak detection parameters through the Unity Inspector, allowing for different detection behaviors (e.g., precise raycast for a robot, a wider spherecast for a rounded character) without modifying code.
4.  **Modularity:** It provides a clear, concise API (e.g., `IsGrounded`, `GetGroundNormal`) for other scripts to query the ground state.
5.  **Strategy-like Implementation:** By offering different `GroundDetectionMethod` options (Raycast, SphereCast, BoxCast), the system employs elements of the Strategy pattern, allowing the core ground detection *behavior* to be swapped at runtime or design time.

### Real-World Use Case: Player Character Controller

A common use case is within a player character controller. Instead of embedding complex `Physics.Raycast` or `Physics.SphereCast` calls directly into the character's movement script, the `GroundDetectionSystem` provides a clean `IsGrounded` boolean and `CurrentGroundNormal` vector. This simplifies the character controller, making it more readable and maintainable.

---

### `GroundDetectionSystem.cs` - Complete C# Unity Script

This script provides a robust and configurable ground detection system. Attach it to your character or object, configure its settings in the Inspector, and then query its public properties from other scripts.

```csharp
using UnityEngine;

/// <summary>
/// Defines the different methods available for ground detection.
/// </summary>
public enum GroundDetectionMethod
{
    /// <summary>
    /// Uses Physics.Raycast for precise line-based detection. Good for sharp-edged characters.
    /// </summary>
    Raycast,
    /// <summary>
    /// Uses Physics.SphereCast for detection with a spherical shape. Good for rounded characters.
    /// </summary>
    SphereCast,
    /// <summary>
    /// Uses Physics.BoxCast for detection with a box shape. Good for rectangular or flat-based characters.
    /// </summary>
    BoxCast
}

/// <summary>
/// The GroundDetectionSystem provides a modular and configurable way to detect if a GameObject
/// is on the ground in a Unity project.
///
/// This system encapsulates all ground detection logic, making it reusable and easy to manage.
/// It supports various detection methods (Raycast, SphereCast, BoxCast) and allows for detailed
/// configuration via the Inspector.
/// </summary>
[DisallowMultipleComponent] // Only one GroundDetectionSystem per GameObject
public class GroundDetectionSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("The method used to detect ground.")]
    [SerializeField] private GroundDetectionMethod detectionMethod = GroundDetectionMethod.SphereCast;
    
    [Tooltip("Layers that are considered ground. Only colliders on these layers will be detected.")]
    [SerializeField] private LayerMask groundLayer;
    
    [Tooltip("Local position offset from the GameObject's pivot (transform.position) " +
             "for the origin of the detection cast. " +
             "For SphereCast/BoxCast, this is the center of the sphere/box.")]
    [SerializeField] private Vector3 castOriginOffset = new Vector3(0f, -0.9f, 0f);
    
    [Tooltip("Maximum distance the cast will travel downwards from its origin. " +
             "This defines how far below the character the system looks for ground.")]
    [SerializeField] private float maxCastDistance = 0.2f;
    
    [Range(0f, 90f)]
    [Tooltip("The maximum angle (in degrees) a surface can have relative to Vector3.up " +
             "to be considered walkable ground. Surfaces steeper than this are ignored.")]
    [SerializeField] private float maxWalkableSlopeAngle = 45f;

    [Header("SphereCast / BoxCast Specific Settings")]
    [Tooltip("Radius for SphereCast. Only applies when Detection Method is SphereCast.")]
    [SerializeField] private float sphereCastRadius = 0.4f;
    
    [Tooltip("Half Extents for BoxCast (half the size of the box along each axis). " +
             "Only applies when Detection Method is BoxCast.")]
    [SerializeField] private Vector3 boxCastHalfExtents = new Vector3(0.4f, 0.1f, 0.4f);

    [Header("Debug Settings")]
    [Tooltip("If true, draws debug visuals in the editor to show detection casts.")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private Color groundedColor = Color.green;
    [SerializeField] private Color notGroundedColor = Color.red;
    [SerializeField] private Color groundNormalColor = Color.blue;

    // --- Private / Internal State ---
    private RaycastHit _groundHit;          // Stores information about the last successful ground hit.
    private bool _isGrounded;               // True if the object is currently on walkable ground.
    private Vector3 _currentGroundNormal = Vector3.up; // The normal of the surface currently being stood on.

    // --- Public Properties (Read-Only) ---
    /// <summary>
    /// Gets a value indicating whether the object is currently detected as being on walkable ground.
    /// </summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>
    /// Gets the RaycastHit information from the last successful ground detection.
    /// Use this to get details like hit point, collider, etc. Only valid if <see cref="IsGrounded"/> is true.
    /// </summary>
    public RaycastHit GroundHit => _groundHit;

    /// <summary>
    /// Gets the normal vector of the surface currently being stood on.
    /// Defaults to Vector3.up if not grounded or on perfectly flat ground.
    /// </summary>
    public Vector3 CurrentGroundNormal => _currentGroundNormal;

    /// <summary>
    /// Gets a value indicating whether the object is currently on a slope.
    /// This is true if grounded and the ground normal is not perfectly vertical (Vector3.up).
    /// </summary>
    public bool IsOnSlope => _isGrounded && Vector3.Angle(Vector3.up, _currentGroundNormal) > 0.01f; // Use a small epsilon for float comparison.

    /// <summary>
    /// Called every fixed framerate frame, useful for physics operations.
    /// We perform ground detection here to ensure it aligns with physics updates.
    /// </summary>
    private void FixedUpdate()
    {
        PerformGroundDetection();
    }

    /// <summary>
    /// Performs the ground detection using the configured method.
    /// This method updates the internal state variables (_isGrounded, _groundHit, _currentGroundNormal).
    /// </summary>
    private void PerformGroundDetection()
    {
        // Calculate the world position where the detection cast will originate.
        Vector3 origin = transform.position + castOriginOffset;
        Vector3 direction = Vector3.down; // Always cast downwards.

        // Reset state before detection.
        _isGrounded = false;
        _currentGroundNormal = Vector3.up; // Default to flat ground or not grounded.
        _groundHit = default; // Clear previous hit info.

        bool hitDetected = false;

        // Perform the chosen detection method.
        switch (detectionMethod)
        {
            case GroundDetectionMethod.Raycast:
                hitDetected = Physics.Raycast(origin, direction, out _groundHit, maxCastDistance, groundLayer, QueryTriggerInteraction.Ignore);
                break;

            case GroundDetectionMethod.SphereCast:
                hitDetected = Physics.SphereCast(origin, sphereCastRadius, direction, out _groundHit, maxCastDistance, groundLayer, QueryTriggerInteraction.Ignore);
                break;

            case GroundDetectionMethod.BoxCast:
                // BoxCast requires rotation; transform.rotation aligns the box with the object's orientation.
                hitDetected = Physics.BoxCast(origin, boxCastHalfExtents, direction, out _groundHit, transform.rotation, maxCastDistance, groundLayer, QueryTriggerInteraction.Ignore);
                break;
        }

        // If a collider was hit, check if it's considered "walkable ground".
        if (hitDetected)
        {
            // Calculate the angle between the hit surface's normal and the global up direction.
            // If the angle is within the maxWalkableSlopeAngle, it's considered ground.
            float angle = Vector3.Angle(Vector3.up, _groundHit.normal);
            if (angle <= maxWalkableSlopeAngle)
            {
                _isGrounded = true;
                _currentGroundNormal = _groundHit.normal;
            }
            // If the angle is too steep, it's considered a wall or too steep a slope, not "grounded".
        }
    }

    /// <summary>
    /// Helper method to get the world position where the detection cast originates.
    /// Useful for other scripts needing to know the exact detection point.
    /// </summary>
    public Vector3 GetDetectionOriginWorldPosition()
    {
        return transform.position + castOriginOffset;
    }

    /// <summary>
    /// Draws debug visuals in the editor to visualize the ground detection.
    /// This helps in setting up parameters correctly.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        // Set up common visualization parameters.
        Vector3 origin = GetDetectionOriginWorldPosition();
        Vector3 direction = Vector3.down;
        Color gizmoColor = _isGrounded ? groundedColor : notGroundedColor;

        Gizmos.color = gizmoColor;

        // Draw the origin point of the cast.
        Gizmos.DrawWireSphere(origin, 0.05f);

        // Draw the detection cast based on the chosen method.
        switch (detectionMethod)
        {
            case GroundDetectionMethod.Raycast:
                Gizmos.DrawLine(origin, origin + direction * maxCastDistance);
                if (_isGrounded)
                {
                    Gizmos.DrawSphere(_groundHit.point, 0.05f); // Draw point of contact
                }
                break;

            case GroundDetectionMethod.SphereCast:
                // Draw starting sphere
                Gizmos.DrawWireSphere(origin, sphereCastRadius);
                // Draw path of the cast
                Gizmos.DrawLine(origin, origin + direction * maxCastDistance);
                // Draw ending sphere (if not grounded) or hit sphere (if grounded)
                if (_isGrounded)
                {
                    // Draw a sphere at the point where the SphereCast hit, adjusted by its normal
                    // to show the shape that made contact.
                    // The hit.point is the first point of contact, not the center of the sphere.
                    // To show the center of the sphere at hit time: hit.point + hit.normal * sphereCastRadius
                    Gizmos.DrawWireSphere(_groundHit.point + _groundHit.normal * sphereCastRadius, sphereCastRadius);
                    Gizmos.DrawSphere(_groundHit.point, 0.05f); // Draw point of contact
                }
                else
                {
                    // Draw the sphere at its maximum travel distance
                    Gizmos.DrawWireSphere(origin + direction * maxCastDistance, sphereCastRadius);
                }
                break;

            case GroundDetectionMethod.BoxCast:
                // For BoxCast, we use Gizmos.matrix to correctly draw rotated boxes.
                // Draw the starting box.
                Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxCastHalfExtents * 2); // size is halfExtents * 2
                Gizmos.matrix = Matrix4x4.identity; // Reset matrix

                // Draw the ending box (if not grounded) or show hit point.
                if (_isGrounded)
                {
                    // For BoxCast, _groundHit.point is the first point of contact.
                    // Visualizing the entire box at the hit point is complex; just draw the point.
                    Gizmos.DrawSphere(_groundHit.point, 0.05f); // Draw point of contact
                }
                else
                {
                    // Draw the box at its maximum travel distance.
                    Gizmos.matrix = Matrix4x4.TRS(origin + direction * maxCastDistance, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, boxCastHalfExtents * 2);
                    Gizmos.matrix = Matrix4x4.identity;
                }
                break;
        }

        // If grounded, draw the ground normal vector.
        if (_isGrounded)
        {
            Gizmos.color = groundNormalColor;
            Gizmos.DrawRay(_groundHit.point, _currentGroundNormal * 0.5f); // Draw normal from hit point
        }
    }
}
```

---

### Example Usage in a Character Controller Script (`PlayerController.cs`)

This example demonstrates how another script (like a `PlayerController`) would interact with the `GroundDetectionSystem`.

1.  **Create a new C# script** called `PlayerController.cs`.
2.  **Attach both `GroundDetectionSystem.cs` and `PlayerController.cs`** to your player GameObject.
3.  **Set up `GroundDetectionSystem` in the Inspector:**
    *   Set `Ground Layer` to include your ground/platform layers.
    *   Adjust `Cast Origin Offset`, `Max Cast Distance`, `Max Walkable Slope Angle`, and specific cast parameters based on your character's size and desired detection behavior.
    *   Enable `Draw Debug Gizmos` to visualize the detection in the Scene view.
4.  **Drag the `GroundDetectionSystem` component** from your player GameObject into the `Ground Detector` slot of the `PlayerController` in the Inspector.

```csharp
using UnityEngine;

/// <summary>
/// Example PlayerController script demonstrating how to use the GroundDetectionSystem.
/// This script relies on a GroundDetectionSystem component being present on the same GameObject
/// or referenced from another GameObject.
/// </summary>
[RequireComponent(typeof(GroundDetectionSystem))] // Ensures GroundDetectionSystem is present
public class PlayerController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the GroundDetectionSystem component.")]
    [SerializeField] private GroundDetectionSystem groundDetector;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravity = -20f; // Custom gravity
    [SerializeField] private float slopeSlideSpeed = 5f;

    private CharacterController _characterController;
    private Vector3 _velocity;

    private void Awake()
    {
        // Get references to components.
        _characterController = GetComponent<CharacterController>();
        if (groundDetector == null)
        {
            groundDetector = GetComponent<GroundDetectionSystem>();
            if (groundDetector == null)
            {
                Debug.LogError("PlayerController requires a GroundDetectionSystem component!", this);
                enabled = false; // Disable script if dependency is missing.
                return;
            }
        }
    }

    private void Update()
    {
        // --- Horizontal Movement ---
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        
        // Normalize movement input to prevent faster diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Apply movement speed
        Vector3 horizontalVelocity = moveDirection * moveSpeed;

        // Adjust horizontal movement to align with ground slope (if grounded)
        if (groundDetector.IsGrounded)
        {
            // Project desired horizontal movement onto the ground plane.
            // This allows the character to move along slopes naturally.
            Vector3 groundNormal = groundDetector.CurrentGroundNormal;
            Vector3 projectedMove = Vector3.ProjectOnPlane(horizontalVelocity, groundNormal).normalized * horizontalVelocity.magnitude;
            horizontalVelocity = projectedMove;

            // Handle sliding down steep slopes (optional)
            if (groundDetector.IsOnSlope && Vector3.Angle(Vector3.up, groundNormal) > 0.01f)
            {
                // Add a force pushing down the slope
                Vector3 slideDirection = Vector3.Cross(Vector3.Cross(Vector3.up, groundNormal), groundNormal);
                horizontalVelocity += slideDirection.normalized * slopeSlideSpeed;
            }
        }

        _velocity.x = horizontalVelocity.x;
        _velocity.z = horizontalVelocity.z;


        // --- Vertical Movement (Gravity & Jump) ---
        // Check if grounded using the GroundDetectionSystem.
        if (groundDetector.IsGrounded)
        {
            // If grounded, reset vertical velocity and allow jumping.
            // A small negative velocity keeps the character "snapped" to the ground.
            _velocity.y = -2f; 

            if (Input.GetButtonDown("Jump"))
            {
                _velocity.y = jumpForce;
            }
        }
        else
        {
            // If not grounded, apply gravity.
            _velocity.y += gravity * Time.deltaTime;
        }

        // --- Apply Movement to CharacterController ---
        _characterController.Move(_velocity * Time.deltaTime);
    }
}
```