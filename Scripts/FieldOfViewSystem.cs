// Unity Design Pattern Example: FieldOfViewSystem
// This script demonstrates the FieldOfViewSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **FieldOfViewSystem** design pattern in Unity. The core idea is to centralize the logic for determining what an "observer" can see within its defined field of view, making the perception system reusable and decoupled from specific observer behaviors.

We'll create three main scripts:
1.  **`FieldOfViewSystem`**: A static utility class containing the pure logic for FOV calculations (distance, angle, obstruction). This is the "system" part of the pattern.
2.  **`FieldOfViewObserver`**: A MonoBehaviour component that an entity (like an AI agent) attaches to. It acts as the "observer," configuring its FOV parameters and periodically querying the `FieldOfViewSystem`. It exposes visible targets via a public property and an event.
3.  **`DetectableTarget`**: A simple MonoBehaviour component to mark GameObjects as potential targets for the `FieldOfViewSystem`.
4.  **`TargetDetectionDemo`**: An example MonoBehaviour demonstrating how to use `FieldOfViewObserver` by subscribing to its events.

---

### 1. `FieldOfViewSystem.cs` (The Core Logic)

This static class contains the reusable calculations for determining visibility. It doesn't need to be attached to a GameObject and doesn't hold any state, making it a pure utility.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The FieldOfViewSystem is a static utility class that encapsulates the core logic
/// for determining which targets are visible to an observer within a defined field of view.
/// This centralizes the FOV calculations, making them reusable and testable.
/// It takes observer parameters and a list of potential targets, and returns
/// a filtered list of truly visible targets.
/// </summary>
public static class FieldOfViewSystem
{
    /// <summary>
    /// Calculates and returns a list of transforms that are within the observer's
    /// view radius, view angle, and are not obstructed by obstacles.
    /// This method embodies the 'system' part of the FieldOfViewSystem pattern.
    /// </summary>
    /// <param name="observerTransform">The transform of the entity observing.</param>
    /// <param name="viewRadius">The maximum distance the observer can see.</param>
    /// <param name="viewAngle">The half-angle of the view cone in degrees (e.g., 45 for a 90-degree cone).</param>
    /// <param name="targetLayer">A LayerMask specifying which layers contain potential targets.</param>
    /// <param name="obstacleLayer">A LayerMask specifying which layers contain objects that obstruct vision.</param>
    /// <returns>A list of Transforms representing the targets currently visible to the observer.</returns>
    public static List<Transform> GetVisibleTargets(
        Transform observerTransform,
        float viewRadius,
        float viewAngle, // This is the HALF angle, so for 90-degree FOV, pass 45.
        LayerMask targetLayer,
        LayerMask obstacleLayer)
    {
        List<Transform> visibleTargets = new List<Transform>();

        // 1. Find all potential targets within the viewRadius using an overlap sphere.
        // This is a performance optimization to quickly filter out distant objects.
        Collider[] targetsInViewRadius = Physics.OverlapSphere(observerTransform.position, viewRadius, targetLayer);

        foreach (Collider targetCollider in targetsInViewRadius)
        {
            Transform target = targetCollider.transform;
            // Calculate the direction vector from the observer to the target.
            Vector3 directionToTarget = (target.position - observerTransform.position).normalized;

            // 2. Check if the target is within the viewAngle (cone).
            // Vector3.Angle returns the angle between two vectors.
            if (Vector3.Angle(observerTransform.forward, directionToTarget) < viewAngle)
            {
                float distanceToTarget = Vector3.Distance(observerTransform.position, target.position);

                // 3. Check for obstructions using a Raycast.
                // Cast a ray from the observer's position towards the target.
                // We add a small vertical offset to the origin to simulate "eye-level" and prevent
                // the ray from hitting the observer's own collider if it's on the ground or has a large base.
                Vector3 raycastOrigin = observerTransform.position + Vector3.up * 0.5f; // Adjust offset as needed for your observer's model.
                
                // Physics.Raycast returns true if it hits an obstacle.
                // If it hits *something* before reaching the target, and that something is on the obstacle layer,
                // then the target is obstructed.
                if (!Physics.Raycast(raycastOrigin, directionToTarget, distanceToTarget, obstacleLayer))
                {
                    // No obstruction detected, the target is truly visible!
                    visibleTargets.Add(target);
                }
            }
        }
        return visibleTargets;
    }
}
```

---

### 2. `FieldOfViewObserver.cs` (The Observer Component)

This MonoBehaviour is attached to any GameObject that needs to "observe." It sets its own FOV parameters and uses the `FieldOfViewSystem` to get visible targets. It provides an event for other scripts to react to changes in visible targets.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // Required for Action events

/// <summary>
/// The FieldOfViewObserver component represents an entity that observes its surroundings.
/// It defines its own field of view parameters (radius, angle) and periodically
/// queries the static FieldOfViewSystem to determine which targets are visible.
/// It exposes the list of visible targets and an event for other scripts to react to changes.
/// This acts as the consumer of the FieldOfViewSystem.
/// </summary>
public class FieldOfViewObserver : MonoBehaviour
{
    [Header("FOV Settings")]
    [Tooltip("The maximum radius (distance) the observer can see.")]
    public float viewRadius = 10f;

    [Tooltip("The half-angle of the field of view cone in degrees (e.g., 45 for a 90-degree total cone).")]
    [Range(0, 180)] // Angle can be from 0 (line) to 180 (full hemisphere)
    public float viewAngle = 45f;

    [Tooltip("How often the FOV system updates its target detection (in seconds). Lower values are more responsive but use more CPU.")]
    public float updateDelay = 0.2f;

    [Header("Layer Masks")]
    [Tooltip("Specifies which Unity Layer(s) contain GameObjects that this observer should consider as targets.")]
    public LayerMask targetLayer;

    [Tooltip("Specifies which Unity Layer(s) contain GameObjects that can obstruct the observer's view.")]
    public LayerMask obstacleLayer;

    [Header("Debug Visualization")]
    [Tooltip("Color for the FOV cone and radius in the editor.")]
    public Color fovColor = new Color(1, 0, 0, 0.1f); // Transparent Red
    [Tooltip("Color for lines connecting the observer to currently visible targets.")]
    public Color visibleTargetColor = Color.green;
    // Removed obstructedTargetColor as it's not directly used in visibleTargets list.

    // Public property to access the current list of visible targets.
    // Read-only from outside to ensure consistent updates via the observer logic.
    public List<Transform> VisibleTargets { get; private set; } = new List<Transform>();

    // An event that fires whenever the list of visible targets changes.
    // Other scripts can subscribe to this to react to perception changes.
    public event Action<List<Transform>> OnVisibleTargetsChanged;

    private Transform _observerTransform;
    private List<Transform> _previousVisibleTargets = new List<Transform>(); // To detect changes for event invocation

    void Awake()
    {
        _observerTransform = transform;
    }

    void Start()
    {
        // Start the coroutine to periodically find targets.
        // Using a coroutine with a delay is more efficient than checking every frame in Update().
        StartCoroutine(FindTargetsWithDelay(updateDelay));
    }

    /// <summary>
    /// Coroutine to repeatedly call the target finding logic after a specified delay.
    /// </summary>
    /// <param name="delay">The time in seconds between each FOV check.</param>
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay); // Wait for the specified delay
            FindVisibleTargets(); // Perform the FOV check
        }
    }

    /// <summary>
    /// Invokes the FieldOfViewSystem to get visible targets and updates the internal list.
    /// If the list of visible targets has changed, it triggers the OnVisibleTargetsChanged event.
    /// </summary>
    void FindVisibleTargets()
    {
        // Call the static FieldOfViewSystem to get the current list of visible targets.
        List<Transform> currentVisibleTargets = FieldOfViewSystem.GetVisibleTargets(
            _observerTransform,
            viewRadius,
            viewAngle,
            targetLayer,
            obstacleLayer
        );

        // Check if the current list of visible targets is different from the previous one.
        // This prevents unnecessary event invocations if nothing has changed.
        if (!AreListsEqual(currentVisibleTargets, _previousVisibleTargets))
        {
            VisibleTargets = currentVisibleTargets; // Update the public property
            OnVisibleTargetsChanged?.Invoke(VisibleTargets); // Invoke the event with the new list
            _previousVisibleTargets = new List<Transform>(currentVisibleTargets); // Update previous for next comparison
        }
    }

    /// <summary>
    /// Helper method to compare two lists of Transforms.
    /// Returns true if both lists contain the same Transforms, regardless of order.
    /// </summary>
    private bool AreListsEqual(List<Transform> listA, List<Transform> listB)
    {
        if (listA.Count != listB.Count)
        {
            return false;
        }

        // Using HashSet for efficient comparison when order doesn't matter.
        // This avoids O(N^2) complexity if listB.Contains() was used in a loop.
        HashSet<Transform> setA = new HashSet<Transform>(listA);
        foreach (Transform itemB in listB)
        {
            if (!setA.Contains(itemB))
            {
                return false;
            }
        }
        return true;
    }

    // --- Editor Visualization (Gizmos) ---
    // OnDrawGizmos is called for rendering gizmos in the editor (even when not playing).
    void OnDrawGizmos()
    {
        if (_observerTransform == null)
        {
            _observerTransform = transform;
        }

        // Draw the view radius sphere
        Gizmos.color = new Color(fovColor.r, fovColor.g, fovColor.b, 0.1f); // More transparent
        Gizmos.DrawSphere(_observerTransform.position, viewRadius);

        // Draw the view cone
        Gizmos.color = fovColor;
        Vector3 forward = _observerTransform.forward;
        // Calculate the direction vectors for the left and right edges of the cone
        Vector3 leftLimit = Quaternion.Euler(0, -viewAngle, 0) * forward * viewRadius;
        Vector3 rightLimit = Quaternion.Euler(0, viewAngle, 0) * forward * viewRadius;

        // Draw rays for the cone's edges and center
        Gizmos.DrawRay(_observerTransform.position, leftLimit);
        Gizmos.DrawRay(_observerTransform.position, rightLimit);
        Gizmos.DrawRay(_observerTransform.position, forward * viewRadius); // Center ray

        // Draw an arc at the end of the cone for better visual representation
        DrawConeArc(_observerTransform.position, _observerTransform.forward, viewRadius, viewAngle, fovColor);

        // Draw lines to currently visible targets (if any)
        if (VisibleTargets != null)
        {
            Gizmos.color = visibleTargetColor;
            foreach (Transform target in VisibleTargets)
            {
                if (target != null) // Ensure target still exists
                {
                    // Draw a line from the observer's "eye level" to the target
                    Gizmos.DrawLine(_observerTransform.position + Vector3.up * 0.5f, target.position);
                    Gizmos.DrawSphere(target.position, 0.2f); // Small sphere at target position
                }
            }
        }
    }

    /// <summary>
    /// Helper function to draw an arc representing the outer edge of the FOV cone in Gizmos.
    /// </summary>
    void DrawConeArc(Vector3 origin, Vector3 direction, float radius, float angle, Color color)
    {
        Gizmos.color = color;
        int segments = 20; // Number of segments to approximate the arc

        // Calculate the starting direction for the arc (leftmost edge of the cone)
        Vector3 startDirection = Quaternion.Euler(0, -angle, 0) * direction;
        Vector3 previousPoint = origin + startDirection * radius;

        // Draw lines between segments to form the arc
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = Mathf.Lerp(-angle, angle, (float)i / segments);
            Vector3 currentDirection = Quaternion.Euler(0, currentAngle, 0) * direction;
            Vector3 currentPoint = origin + currentDirection * radius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}
```

---

### 3. `DetectableTarget.cs` (The Target Component)

This simple component marks a GameObject as something that can be detected by the `FieldOfViewSystem`. Its presence (or the GameObject's layer/tag) is what the system looks for.

```csharp
using UnityEngine;

/// <summary>
/// This component simply marks a GameObject as a potential target that can be
/// detected by the FieldOfViewSystem.
/// Any GameObject with this script (and on the correct layer) will be considered
/// a 'target' for observers.
/// </summary>
public class DetectableTarget : MonoBehaviour
{
    [Tooltip("Optional: A visual indicator for the target in the editor.")]
    public Color debugColor = Color.blue;

    void OnDrawGizmos()
    {
        Gizmos.color = debugColor;
        // Draw a small wire sphere to easily identify targets in the editor.
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
```

---

### 4. `TargetDetectionDemo.cs` (Example Usage)

This script shows how another component would interact with the `FieldOfViewObserver` by subscribing to its `OnVisibleTargetsChanged` event.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script serves as a practical example of how to use the FieldOfViewObserver.
/// It demonstrates subscribing to the observer's event to react when visible targets change.
/// Attach this script to the same GameObject that has the FieldOfViewObserver component.
/// </summary>
[RequireComponent(typeof(FieldOfViewObserver))] // Ensures FieldOfViewObserver is present
public class TargetDetectionDemo : MonoBehaviour
{
    private FieldOfViewObserver _fovObserver;

    void Awake()
    {
        // Get a reference to the FieldOfViewObserver component on this GameObject.
        _fovObserver = GetComponent<FieldOfViewObserver>();
        if (_fovObserver == null)
        {
            Debug.LogError("TargetDetectionDemo requires a FieldOfViewObserver component on the same GameObject!");
            enabled = false; // Disable this script if the observer is missing.
        }
    }

    void OnEnable()
    {
        // Subscribe to the OnVisibleTargetsChanged event when this script is enabled.
        // This is crucial for reacting to changes in the observer's perception.
        if (_fovObserver != null)
        {
            _fovObserver.OnVisibleTargetsChanged += HandleVisibleTargetsChanged;
            Debug.Log("[FOV Demo] Subscribed to OnVisibleTargetsChanged event.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the event when this script is disabled to prevent memory leaks
        // and ensure the event doesn't try to call a null object if this script is destroyed.
        if (_fovObserver != null)
        {
            _fovObserver.OnVisibleTargetsChanged -= HandleVisibleTargetsChanged;
            Debug.Log("[FOV Demo] Unsubscribed from OnVisibleTargetsChanged event.");
        }
    }

    /// <summary>
    /// This method is called whenever the FieldOfViewObserver detects a change
    /// in its list of visible targets. It's the primary way to react to FOV changes.
    /// </summary>
    /// <param name="visibleTargets">The current list of transforms visible to the observer.</param>
    private void HandleVisibleTargetsChanged(List<Transform> visibleTargets)
    {
        Debug.Log($"[FOV Demo] Visible targets changed! Current count: {visibleTargets.Count}");

        if (visibleTargets.Count > 0)
        {
            string targetNames = "";
            foreach (Transform target in visibleTargets)
            {
                targetNames += target.name + ", ";
                // EXAMPLE: This is where you would implement game logic based on detection.
                // - An AI might decide to pursue or attack this target.
                // - A UI element might highlight this target.
                // - A stealth game might trigger an "alert" state.
                // Debug.Log($"[FOV Demo] -> Observed: {target.name}");
            }
            Debug.Log($"[FOV Demo] Currently seeing: {targetNames.TrimEnd(',', ' ')}");
        }
        else
        {
            Debug.Log("[FOV Demo] No targets currently visible.");
        }
    }

    // You could also poll the _fovObserver.VisibleTargets list directly in Update(),
    // but using the event is generally more efficient and reactive for significant changes.
    // private void Update()
    // {
    //     if (_fovObserver.VisibleTargets.Count > 0)
    //     {
    //         // Debug.Log($"[FOV Demo - Update] Still seeing {_fovObserver.VisibleTargets[0].name}");
    //     }
    // }
}
```

---

### How to Use in Unity (Step-by-Step Guide)

1.  **Create C# Scripts:**
    *   Create a new C# script named `FieldOfViewSystem` and copy the code from section 1 into it.
    *   Create a new C# script named `FieldOfViewObserver` and copy the code from section 2 into it.
    *   Create a new C# script named `DetectableTarget` and copy the code from section 3 into it.
    *   Create a new C# script named `TargetDetectionDemo` and copy the code from section 4 into it.

2.  **Set Up Layers:**
    *   Go to `Edit > Project Settings > Tags and Layers`.
    *   In the "Layers" section, add two new User Layers (e.g., at index 8 and 9):
        *   Layer 8: `Targets`
        *   Layer 9: `Obstacles`

3.  **Create the Observer GameObject:**
    *   In your scene, create an empty GameObject (e.g., right-click in Hierarchy -> `Create Empty`). Name it `AI_Observer`.
    *   Add the `FieldOfViewObserver` component to `AI_Observer`.
    *   Optionally, add the `TargetDetectionDemo` component to `AI_Observer` to see the console output.
    *   **In the Inspector for `AI_Observer` (FieldOfViewObserver component):**
        *   Adjust `View Radius` (e.g., 15) and `View Angle` (e.g., 60 for a 120-degree total FOV).
        *   Set `Target Layer` to your newly created `Targets` layer.
        *   Set `Obstacle Layer` to your newly created `Obstacles` layer.
        *   You can also adjust `Update Delay` and debug colors.
    *   Position `AI_Observer` in your scene (e.g., (0, 1, 0)).

4.  **Create Target GameObjects:**
    *   Create several Cube, Sphere, or Capsule GameObjects (e.g., `Create 3D Object > Cube`). Name them `Target_1`, `Target_2`, `Player`, etc.
    *   Add the `DetectableTarget` component to each of these GameObjects.
    *   **For each target GameObject:**
        *   In the Inspector, change its `Layer` dropdown to the `Targets` layer.
    *   Place these targets at various distances and angles around your `AI_Observer`.

5.  **Create Obstacle GameObjects:**
    *   Create a Cube GameObject (e.g., `Create 3D Object > Cube`). Name it `Wall`.
    *   In the Inspector for `Wall`, change its `Layer` dropdown to the `Obstacles` layer.
    *   Scale it up (e.g., (5, 3, 0.5)) and place it between your `AI_Observer` and some of your `Targets`.

6.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Observe the scene view: You should see the `AI_Observer`'s FOV cone and radius. Green lines will connect the observer to any visible targets.
    *   Check the Console window: The `TargetDetectionDemo` script will log messages when visible targets enter or leave the FOV.
    *   **Experiment:**
        *   Move `AI_Observer`, `Targets`, or `Walls`.
        *   Rotate `AI_Observer` to change its facing direction.
        *   Change `View Radius` or `View Angle` in the Inspector at runtime to see immediate effects.

This setup provides a complete, functional, and educational example of the FieldOfViewSystem pattern in Unity, demonstrating how to decouple perception logic from specific AI behaviors while maintaining good performance and visual debugging.