// Unity Design Pattern Example: LineOfSightSystem
// This script demonstrates the LineOfSightSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **LineOfSightSystem design pattern** in Unity. This pattern provides a reusable and encapsulated component for GameObjects that need to detect and react to other objects within their vision, considering factors like range, field of view, and physical obstacles.

The `LineOfSightController` script acts as the core of this system. It continuously checks for targets based on configurable parameters and notifies other scripts via events when targets enter, exit, or stay within its line of sight.

---

### C# Unity Script: `LineOfSightController.cs`

To use this:
1.  Create a new C# script named `LineOfSightController` in your Unity project.
2.  Copy and paste the code below into the script.
3.  Follow the **EXAMPLE USAGE** instructions provided in the comments at the bottom of the script.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent

/// <summary>
///     The LineOfSightSystem design pattern encapsulates the logic for detecting if an
///     object (the "seer") can see another object (the "target") within a specified
///     range and field of view, while also considering obstacles.
///
///     This script implements a flexible LineOfSightController that can be attached
///     to any GameObject that needs to "see" other objects. It provides events
///     for when targets enter, exit, or stay within sight.
///
///     **Key Components of the LineOfSightSystem Pattern:**
///     1.  **Seer (This GameObject):** The entity performing the sight check.
///     2.  **Target(s):** The entities being looked for (filtered by LayerMask).
///     3.  **Vision Parameters:** Configurable range, field of view angle, and check interval.
///     4.  **Obstacle Detection:** Uses Raycasting to ensure line of sight isn't blocked by physical objects (filtered by LayerMask).
///     5.  **Event-Driven Communication:** Notifies other scripts (e.g., AI behaviors) when the state of targets in sight changes (enter, exit, stay).
///     6.  **Optimization:** Uses non-allocating physics calls and interval checks to reduce performance overhead.
/// </summary>
[DisallowMultipleComponent] // Ensures only one LineOfSightController can be attached to a GameObject.
public class LineOfSightController : MonoBehaviour
{
    [Header("Vision Parameters")]
    [Tooltip("The maximum distance this object can see.")]
    [SerializeField]
    private float visionRange = 10f;

    [Tooltip("The field of view angle (in degrees) from the forward direction. 0 means only straight ahead, 360 means full omnidirectional vision.")]
    [Range(0, 360)]
    [SerializeField]
    private float fieldOfViewAngle = 90f;

    [Tooltip("How often (in seconds) the system checks for targets. Lower values mean more frequent checks but higher performance cost.")]
    [SerializeField]
    private float checkInterval = 0.2f;

    [Header("Layer Masks")]
    [Tooltip("Which layers contain potential targets that this object should try to see.")]
    [SerializeField]
    private LayerMask targetLayerMask;

    [Tooltip("Which layers contain obstacles that block line of sight (e.g., walls, terrain).")]
    [SerializeField]
    private LayerMask obstacleLayerMask;

    [Header("Debug")]
    [Tooltip("If true, draws visual aids in the editor for vision range and FOV.")]
    [SerializeField]
    private bool debugDrawGizmos = true;

    // Internal state management
    private float _lastCheckTime;
    // Using HashSet for efficient add/remove/contains operations when tracking multiple targets.
    private HashSet<Transform> _targetsInSight = new HashSet<Transform>();
    // Pre-allocated array to avoid garbage collection during Physics.OverlapSphereNonAlloc calls.
    private Collider[] _overlapSphereColliders; 

    // --- Public Events for other scripts to subscribe to ---
    [Header("Events")]
    [Tooltip("Invoked when a target first enters the line of sight.")]
    public UnityEvent<Transform> OnTargetEnterSight = new UnityEvent<Transform>();

    [Tooltip("Invoked when a target exits the line of sight (either moved out of range/FOV or got blocked by an obstacle).")]
    public UnityEvent<Transform> OnTargetExitSight = new UnityEvent<Transform>();

    [Tooltip("Invoked continuously for targets that remain in line of sight during each check interval.")]
    public UnityEvent<Transform> OnTargetStayInSight = new UnityEvent<Transform>();

    /// <summary>
    /// Property to get the current list of targets in sight.
    /// Returns a copy of the internal hash set to prevent external modification of the state.
    /// </summary>
    public List<Transform> CurrentTargetsInSight
    {
        get { return new List<Transform>(_targetsInSight); }
    }

    /// <summary>
    /// Checks if a specific target is currently within this object's line of sight
    /// based on the last `CheckForTargets()` execution.
    /// </summary>
    /// <param name="target">The transform of the target to check.</param>
    /// <returns>True if the target is in sight, false otherwise.</returns>
    public bool IsTargetCurrentlyInSight(Transform target)
    {
        return _targetsInSight.Contains(target);
    }

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Used here for input validation and initial array allocation.
    /// </summary>
    private void OnValidate()
    {
        if (visionRange < 0) visionRange = 0;
        if (fieldOfViewAngle < 0) fieldOfViewAngle = 0;
        if (fieldOfViewAngle > 360) fieldOfViewAngle = 360; // Max FOV is full circle
        if (checkInterval <= 0) checkInterval = 0.01f; // Minimum interval to prevent zero division

        // Initialize or resize the collider array.
        // A common practice is to start with a reasonable size (e.g., 32 or 64)
        // and let it grow if needed, or pick a size based on expected max targets.
        if (_overlapSphereColliders == null || _overlapSphereColliders.Length < 32) 
        {
            _overlapSphereColliders = new Collider[32]; 
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to ensure the collider array is initialized at runtime.
    /// </summary>
    private void Awake()
    {
        // Ensure array is initialized at runtime.
        if (_overlapSphereColliders == null)
        {
            _overlapSphereColliders = new Collider[32]; 
        }
    }

    /// <summary>
    /// Called once per frame. Handles the timed execution of sight checks.
    /// </summary>
    private void Update()
    {
        // Perform sight checks only at specified intervals to optimize performance.
        if (Time.time >= _lastCheckTime + checkInterval)
        {
            _lastCheckTime = Time.time;
            CheckForTargets();
        }
    }

    /// <summary>
    /// The core logic of the LineOfSightSystem. This method:
    /// 1.  Finds all potential targets within the vision range using a physics overlap sphere.
    /// 2.  Filters these potential targets by field of view (FOV) and checks for obstacles using raycasts.
    /// 3.  Manages the internal state of targets that are currently in sight.
    /// 4.  Fires `OnTargetEnterSight`, `OnTargetExitSight`, and `OnTargetStayInSight` events as appropriate.
    /// </summary>
    private void CheckForTargets()
    {
        // Temporarily store targets found to be in sight during this specific check cycle.
        HashSet<Transform> currentFrameSeenTargets = new HashSet<Transform>();

        // Step 1: Find all colliders within the vision range on the 'targetLayerMask'.
        // Using Physics.OverlapSphereNonAlloc significantly reduces garbage collection.
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, visionRange, _overlapSphereColliders, targetLayerMask);

        // Step 2: Iterate through potential targets and perform detailed sight checks.
        for (int i = 0; i < numColliders; i++)
        {
            Transform potentialTarget = _overlapSphereColliders[i].transform;

            // Important: Don't check against self.
            if (potentialTarget == transform)
            {
                continue;
            }
            
            // Perform the detailed line of sight check (FOV, raycast).
            if (IsTargetInSight(potentialTarget))
            {
                currentFrameSeenTargets.Add(potentialTarget);
            }
        }

        // Step 3: Compare current frame's seen targets with previously seen targets to fire events.

        // Identify targets that were previously seen but are no longer in sight.
        List<Transform> targetsThatExited = new List<Transform>();
        foreach (Transform previouslySeenTarget in _targetsInSight)
        {
            // If a target we thought was in sight is not in the current frame's seen targets, it has exited.
            if (!currentFrameSeenTargets.Contains(previouslySeenTarget))
            {
                targetsThatExited.Add(previouslySeenTarget);
            }
        }

        // Fire OnTargetExitSight events and remove them from our tracked set.
        foreach (Transform target in targetsThatExited)
        {
            _targetsInSight.Remove(target);
            OnTargetExitSight?.Invoke(target);
        }

        // Identify targets that are newly seen or continue to be seen.
        foreach (Transform currentTarget in currentFrameSeenTargets)
        {
            if (!_targetsInSight.Contains(currentTarget))
            {
                // This target is new; it just entered sight.
                _targetsInSight.Add(currentTarget);
                OnTargetEnterSight?.Invoke(currentTarget);
            }
            else
            {
                // This target was already in sight and remains so; fire a continuous event.
                OnTargetStayInSight?.Invoke(currentTarget);
            }
        }
    }

    /// <summary>
    /// Determines if a specific target is currently within this object's line of sight,
    /// considering range, field of view, and physical obstacles.
    /// This is the core individual sight check logic.
    /// </summary>
    /// <param name="target">The transform of the target to check.</param>
    /// <returns>True if the target is currently visible, false otherwise.</returns>
    public bool IsTargetInSight(Transform target)
    {
        // Basic safety check.
        if (target == null) return false;

        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // 1. Check if target is within the maximum vision range.
        if (distanceToTarget > visionRange)
        {
            return false;
        }

        // 2. Check if target is within the field of view angle.
        // We divide by 2 because the angle spreads out from the forward vector.
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > fieldOfViewAngle / 2f)
        {
            return false;
        }

        // 3. Check for obstacles using a Raycast.
        // Offset the ray origin slightly forward to prevent hitting this object's own collider
        // if it has one and is on the obstacle layer (e.g., a character controller).
        Vector3 rayOrigin = transform.position + transform.forward * 0.1f; 
        Vector3 rayDirection = (target.position - rayOrigin).normalized;

        RaycastHit hit;
        // The raycast distance is slightly less than distanceToTarget to prevent
        // hitting the target's own collider if it also happens to be on the obstacleLayerMask.
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, distanceToTarget - 0.1f, obstacleLayerMask))
        {
            // If the ray hits something, check if what it hit IS the target.
            // If it's NOT the target, then an obstacle is blocking the view.
            if (hit.collider.transform != target)
            {
                return false; // Obstacle detected
            }
        }
        
        // If all checks pass (within range, within FOV, no obstacles), the target is in sight.
        return true;
    }

    // --- Debug Visualization in Editor ---

    /// <summary>
    /// Draws visual debugging aids in the Unity editor's Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!debugDrawGizmos) return;

        // Draw vision range sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange); 

        // Draw FOV arcs (left and right boundaries of the cone)
        Vector3 forward = transform.forward;
        Vector3 leftArcDirection = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * forward;
        Vector3 rightArcDirection = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftArcDirection * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightArcDirection * visionRange);

        // Draw the arc of the FOV at the maximum range
        DrawGizmoArc(transform.position, forward, visionRange, fieldOfViewAngle, Color.cyan);

        // Draw lines to currently seen targets
        Gizmos.color = Color.green;
        foreach (Transform target in _targetsInSight)
        {
            if (target != null) // Ensure target hasn't been destroyed
            {
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }

    /// <summary>
    /// Helper method to draw an arc in the Gizmos.
    /// </summary>
    private void DrawGizmoArc(Vector3 center, Vector3 forward, float radius, float angle, Color color)
    {
        Gizmos.color = color;
        Vector3 startDirection = Quaternion.Euler(0, -angle / 2, 0) * forward;
        Vector3 lastPoint = center + startDirection * radius;
        int segments = 20;
        float angleStep = angle / segments;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 currentDirection = Quaternion.Euler(0, -angle / 2 + i * angleStep, 0) * forward;
            Vector3 currentPoint = center + currentDirection * radius;
            Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }
}

/*
/// EXAMPLE USAGE:
///
/// This section explains how to set up and use the LineOfSightController in a Unity project.
///
/// 1.  **Setup the Scene and Layers in Unity Editor:**
///     a.  **Create a Seer GameObject:**
///         -   Create an empty GameObject (e.g., "Guard_A").
///         -   Add a `LineOfSightController` component to it.
///         -   Position "Guard_A" in your scene.
///         -   Configure its `Vision Range`, `Field Of View Angle`, and `Check Interval` in the Inspector.
///
///     b.  **Define Layers:**
///         -   Go to `Layers` dropdown in the Unity editor (top right, near Inspector).
///         -   Click `Edit Layers...`.
///         -   Add two new User Layers, for example: `Player` and `Obstacle`.
///
///     c.  **Assign Layer Masks in LineOfSightController:**
///         -   In the `LineOfSightController` component on "Guard_A":
///             -   Set `Target Layer Mask` to `Player`.
///             -   Set `Obstacle Layer Mask` to `Obstacle`.
///             -   Optionally, enable `Debug Draw Gizmos` to visualize the vision cone in the Scene view.
///
///     d.  **Create a Target GameObject:**
///         -   Create a Cube or Capsule (e.g., "Player").
///         -   Ensure it has a Collider component (e.g., BoxCollider, CapsuleCollider).
///         -   Assign it to the `Player` Layer.
///
///     e.  **Create Obstacle GameObjects:**
///         -   Create some Cube GameObjects (e.g., "Wall_1", "Wall_2").
///         -   Ensure they have Collider components.
///         -   Assign them to the `Obstacle` Layer.
///
/// 2.  **Example Script for AI Reaction (e.g., `GuardAI.cs`):**
///     -   Create a new C# script named `GuardAI`.
///     -   Attach this script to the same "Guard_A" GameObject that has the `LineOfSightController`.
///     -   Copy and paste the following code into `GuardAI.cs`:
///
///     ```csharp
///     using UnityEngine;
///     using System.Collections.Generic;
///
///     public class GuardAI : MonoBehaviour
///     {
///         [SerializeField] private LineOfSightController lineOfSightController;
///         [SerializeField] private float pursuitSpeed = 3f;
///         [SerializeField] private float rotationSpeed = 5f;
///
///         private Transform currentTarget; // The target currently being pursued
///
///         void Awake()
///         {
///             // Get a reference to the LineOfSightController component on this GameObject.
///             // It's good practice to assign this in the Inspector if possible, but this provides a fallback.
///             if (lineOfSightController == null)
///             {
///                 lineOfSightController = GetComponent<LineOfSightController>();
///             }
///
///             if (lineOfSightController == null)
///             {
///                 Debug.LogError("GuardAI requires a LineOfSightController component on the same GameObject!", this);
///                 enabled = false; // Disable this script if LineOfSightController is missing.
///                 return;
///             }
///
///             // Subscribe to the events provided by the LineOfSightController.
///             // This is how the AI "listens" for vision changes without implementing vision logic itself.
///             lineOfSightController.OnTargetEnterSight.AddListener(HandleTargetEnterSight);
///             lineOfSightController.OnTargetExitSight.AddListener(HandleTargetExitSight);
///             lineOfSightController.OnTargetStayInSight.AddListener(HandleTargetStayInSight);
///
///             Debug.Log($"{gameObject.name}: GuardAI initialized. Listening for line of sight events.", this);
///         }
///
///         private void OnDestroy()
///         {
///             // IMPORTANT: Unsubscribe from events to prevent memory leaks, especially when objects are destroyed.
///             if (lineOfSightController != null)
///             {
///                 lineOfSightController.OnTargetEnterSight.RemoveListener(HandleTargetEnterSight);
///                 lineOfSightController.OnTargetExitSight.RemoveListener(HandleTargetExitSight);
///                 lineOfSightController.OnTargetStayInSight.RemoveListener(HandleTargetStayInSight);
///             }
///         }
///
///         void Update()
///         {
///             if (currentTarget != null)
///             {
///                 // Example behavior: If a target is seen, rotate towards it and move.
///                 Debug.Log($"{gameObject.name}: Pursuing target: {currentTarget.name}", this);
///                 
///                 // Calculate direction to target (ignoring Y-axis for ground-based movement)
///                 Vector3 directionToTarget = (currentTarget.position - transform.position);
///                 directionToTarget.y = 0; // Keep rotation horizontal
///                 directionToTarget.Normalize();
///
///                 // Rotate towards target
///                 if (directionToTarget != Vector3.zero) // Avoid LookRotation(Vector3.zero) error
///                 {
///                     Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
///                     transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
///                 }
///
///                 // Move towards target
///                 transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, pursuitSpeed * Time.deltaTime);
///             }
///             else
///             {
///                 // Example behavior: If no target, perhaps patrol or stand still.
///                 // Debug.Log($"{gameObject.name}: No target currently in sight. Patrolling...", this);
///             }
///         }
///
///         // --- Event Handlers ---
///         // These methods are called automatically by the LineOfSightController when events occur.
///
///         private void HandleTargetEnterSight(Transform target)
///         {
///             Debug.Log($"<color=red>{gameObject.name} DETECTED {target.name}!</color> Starting pursuit.", this);
///             // Set the first detected target as the current target to pursue.
///             // You could implement more complex target selection logic here (e.g., closest, highest threat).
///             if (currentTarget == null) 
///             {
///                 currentTarget = target;
///             }
///         }
///
///         private void HandleTargetExitSight(Transform target)
///         {
///             Debug.Log($"<color=green>{gameObject.name} LOST SIGHT OF {target.name}.</color> Stopping pursuit if it was our target.", this);
///             // If the target that exited was the one we were pursuing, clear it.
///             if (currentTarget == target)
///             {
///                 currentTarget = null; 
///             }
///         }
///
///         private void HandleTargetStayInSight(Transform target)
///         {
///             // This event fires every 'checkInterval' while a target is continuously in sight.
///             // Useful for continuous actions like aiming, updating UI, playing sounds, etc.
///             // Debug.Log($"{gameObject.name} keeping eyes on {target.name}. Distance: {Vector3.Distance(transform.position, target.position):F2}");
///         }
///     }
///     ```
///
/// 3.  **Run the Scene:**
///     -   Enter Play Mode in Unity.
///     -   Move the "Player" GameObject around the "Guard_A".
///     -   Observe the "Guard_A"'s behavior:
///         -   It will rotate and move towards the "Player" when the "Player" enters its vision cone and range.
///         -   It will stop pursuing and potentially look for new targets when the "Player" moves out of range, out of its FOV, or goes behind an "Obstacle".
///         -   Check the Console window for the debug logs to see the events firing.
///
/// This example showcases how the LineOfSightSystem component provides a robust, reusable,
/// and encapsulated solution for vision detection. Other AI scripts (like `GuardAI`) can
/// simply subscribe to its events, allowing for clean separation of concerns and easier
/// development of complex AI behaviors.
*/
```