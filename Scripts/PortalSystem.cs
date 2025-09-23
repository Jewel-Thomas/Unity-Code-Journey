// Unity Design Pattern Example: PortalSystem
// This script demonstrates the PortalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the 'PortalSystem' design pattern. This pattern, common in game development, allows objects to instantaneously move between two designated points (portals) in a game world, often maintaining their physical properties.

The provided script, `Portal.cs`, is designed to be attached to a GameObject and configured in the Unity Editor. It handles object detection, teleportation logic, and prevents immediate re-teleportation through a cooldown mechanism.

---

### `Portal.cs` - The PortalSystem Implementation

This script represents a single portal. To create a working portal system, you will typically create two or more GameObjects, each with this `Portal` script attached, and link them in the Inspector.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for HashSet

/// <summary>
/// Represents a single portal in a Portal System.
/// This script manages the detection of 'travelers' and initiates their teleportation
/// to a linked portal, maintaining their velocity and orientation if configured.
/// </summary>
/// <remarks>
/// The Portal System design pattern, in a game context, allows objects to move
/// instantaneously between two distinct points (portals) in a game world.
/// This implementation focuses on creating individual, configurable portals that
/// work in pairs or chains.
/// </remarks>
[RequireComponent(typeof(Collider))] // Ensure there's a collider for trigger detection
public class Portal : MonoBehaviour
{
    [Header("Portal Configuration")]
    [Tooltip("The portal this one is linked to. An object entering THIS portal will exit via the LINKED portal.")]
    [SerializeField] private Portal _linkedPortal;

    [Tooltip("Optional: A specific transform defining where objects exit *this* portal. " +
             "If null, this portal's own transform will be used as its exit point. " +
             "This is used by the linked portal when it teleports an object HERE.")]
    [SerializeField] private Transform _teleportExitPoint;

    [Tooltip("The layers of GameObjects that can use this portal.")]
    [SerializeField] private LayerMask _travelerLayer;

    [Header("Teleportation Behavior")]
    [Tooltip("Time in seconds before an object can re-enter *any* portal after exiting through this portal's link. " +
             "This prevents immediate re-teleportation loops.")]
    [SerializeField] private float _teleportCooldown = 1.0f;

    [Tooltip("If true, the traveler's velocity will be maintained and adjusted to the new orientation.")]
    [SerializeField] private bool _maintainVelocity = true;

    [Tooltip("If true, the traveler's rotation/orientation will be maintained and adjusted to the new portal's orientation.")]
    [SerializeField] private bool _maintainOrientation = true;

    [Header("Editor Visualization")]
    [Tooltip("Color of the Gizmo sphere in the editor.")]
    [SerializeField] private Color _gizmoColor = Color.cyan;

    [Tooltip("Radius of the Gizmo sphere in the editor.")]
    [SerializeField] private float _gizmoRadius = 1.0f;

    // --- Internal State ---
    // A HashSet to track GameObjects that have recently been teleported.
    // This is crucial for preventing them from immediately re-entering the
    // linked portal (or the current one if it's a loop) before they've
    // properly exited the trigger volume and had time to settle.
    private HashSet<GameObject> _teleportingObjects = new HashSet<GameObject>();

    // Reference to the portal's collider, set in Awake.
    private Collider _portalCollider;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used to initialize references and validate setup.
    /// </summary>
    private void Awake()
    {
        _portalCollider = GetComponent<Collider>();
        if (_portalCollider == null)
        {
            Debug.LogError($"Portal '{name}' is missing a Collider component!", this);
            enabled = false; // Disable script if no collider
            return;
        }

        // Ensure the collider is a trigger, as we're using OnTriggerEnter/Exit
        _portalCollider.isTrigger = true;

        if (_linkedPortal == null)
        {
            Debug.LogWarning($"Portal '{name}' has no linked portal assigned! It will not function.", this);
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// This is where we detect potential travelers and initiate teleportation.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // 1. Basic Checks: Is there a linked portal and is this object a valid traveler?
        if (_linkedPortal == null)
        {
            // No linked portal means this portal cannot teleport anything.
            return;
        }

        // Check if the object's layer is in our allowed traveler layers.
        if (!IsTraveler(other.gameObject))
        {
            return;
        }

        // 2. Cooldown Checks: Prevent immediate re-teleportation.
        //    a) Check if the object is currently in cooldown for THIS portal.
        //       (e.g., if it just entered, got teleported, and immediately re-entered this same trigger)
        if (_teleportingObjects.Contains(other.gameObject))
        {
            // Debug.Log($"'{other.gameObject.name}' is in cooldown for '{name}'. Aborting teleport.", this);
            return;
        }

        //    b) Check if the object is currently in cooldown for the LINKED portal.
        //       This is crucial: if an object just *exited* the linked portal,
        //       it shouldn't be immediately re-teleported *back* into this portal (if they're close).
        if (_linkedPortal._teleportingObjects.Contains(other.gameObject))
        {
            // Debug.Log($"'{other.gameObject.name}' is in cooldown for linked portal '{_linkedPortal.name}'. Aborting teleport.", this);
            return;
        }

        // If all checks pass, attempt to teleport the object.
        TeleportTraveler(other.transform);
    }

    /// <summary>
    /// Checks if a given GameObject is on one of the specified traveler layers.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if the object is a traveler, false otherwise.</returns>
    private bool IsTraveler(GameObject obj)
    {
        return (_travelerLayer.value & (1 << obj.layer)) > 0;
    }

    /// <summary>
    /// Performs the actual teleportation of the traveler to the linked portal.
    /// This involves calculating new position, rotation, and optionally adjusting velocity.
    /// </summary>
    /// <param name="travelerTransform">The transform of the object to teleport.</param>
    private void TeleportTraveler(Transform travelerTransform)
    {
        // Add the traveler to the cooldown set for both this portal and the linked portal.
        // This ensures it won't be immediately re-teleported from either end.
        _teleportingObjects.Add(travelerTransform.gameObject);
        _linkedPortal._teleportingObjects.Add(travelerTransform.gameObject);

        // --- Calculate New Position ---
        // 1. Get the traveler's position relative to this portal's transform.
        // This provides the local offset (e.g., 1 unit in front, 0.5 units to the right).
        Vector3 relativePos = transform.InverseTransformPoint(travelerTransform.position);

        // 2. Determine the target exit point (either the linked portal itself or its specific exit transform).
        // The linked portal provides its preferred exit point.
        Transform targetExitPoint = _linkedPortal.GetTeleportExitPoint();

        // 3. Transform the relative position into the target exit point's local space.
        // This correctly places the object at the same relative offset from the linked portal,
        // respecting the linked portal's orientation.
        Vector3 newPosition = targetExitPoint.TransformPoint(relativePos);

        // --- Calculate New Rotation ---
        Quaternion newRotation = travelerTransform.rotation; // Default to current rotation

        if (_maintainOrientation)
        {
            // 1. Get the traveler's rotation relative to this portal's transform.
            Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * travelerTransform.rotation;

            // 2. Apply this relative rotation to the linked portal's exit orientation.
            // This re-orients the traveler based on the linked portal's facing direction.
            newRotation = targetExitPoint.rotation * relativeRot;
        }

        // --- Apply Teleportation ---
        travelerTransform.position = newPosition;
        travelerTransform.rotation = newRotation;

        // --- Handle Rigidbody Physics (if applicable) ---
        Rigidbody travelerRigidbody = travelerTransform.GetComponent<Rigidbody>();
        if (travelerRigidbody != null)
        {
            if (_maintainVelocity)
            {
                // 1. Get the traveler's velocity relative to this portal's forward direction.
                Vector3 relativeVelocity = transform.InverseTransformDirection(travelerRigidbody.velocity);

                // 2. Apply this relative velocity to the linked portal's exit direction.
                travelerRigidbody.velocity = targetExitPoint.TransformDirection(relativeVelocity);
            }
            else
            {
                // If not maintaining velocity, optionally stop it.
                travelerRigidbody.velocity = Vector3.zero;
            }

            if (_maintainOrientation)
            {
                // 1. Get the traveler's angular velocity relative to this portal's orientation.
                Vector3 relativeAngularVelocity = transform.InverseTransformDirection(travelerRigidbody.angularVelocity);

                // 2. Apply this relative angular velocity to the linked portal's exit orientation.
                travelerRigidbody.angularVelocity = targetExitPoint.TransformDirection(relativeAngularVelocity);
            }
            else
            {
                // If not maintaining angular velocity, optionally stop it.
                travelerRigidbody.angularVelocity = Vector3.zero;
            }
            // Clear any forces that might have built up, to ensure a clean teleport.
            travelerRigidbody.isKinematic = true; // Temporarily make kinematic to prevent physics glitches
            travelerRigidbody.isKinematic = false; // Restore physics simulation
        }
        else
        {
            // If the traveler has no Rigidbody, it's just a simple position/rotation change.
            // (e.g., a camera, a non-physics object, or a character controller that moves manually).
        }

        // Start cooldown coroutine for the traveler. This coroutine will remove the object
        // from the cooldown sets of both portals after the specified duration.
        StartCoroutine(RemoveTravelerFromCooldown(travelerTransform.gameObject));

        Debug.Log($"Teleported '{travelerTransform.name}' from '{name}' to '{_linkedPortal.name}'.", this);
    }

    /// <summary>
    /// Returns the appropriate Transform to be used as the exit point for teleportation.
    /// Prioritizes a custom `_teleportExitPoint` if set, otherwise uses the portal's own transform.
    /// This method is called by the *linked* portal when an object teleports *to* this portal.
    /// </summary>
    /// <returns>The Transform defining the exit location and orientation for this portal.</returns>
    public Transform GetTeleportExitPoint()
    {
        return _teleportExitPoint != null ? _teleportExitPoint : transform;
    }

    /// <summary>
    /// Coroutine to remove a GameObject from the cooldown sets of both the entry and exit portals
    /// after a specified delay. This allows the object to potentially re-enter a portal after some time.
    /// </summary>
    /// <param name="traveler">The GameObject to remove from cooldown.</param>
    private IEnumerator RemoveTravelerFromCooldown(GameObject traveler)
    {
        yield return new WaitForSeconds(_teleportCooldown);

        // Remove from *this* portal's cooldown set.
        _teleportingObjects.Remove(traveler);

        // Also remove from the *linked* portal's cooldown set.
        // This is crucial for bidirectional portals, ensuring the object can immediately
        // re-enter the other portal if desired.
        if (_linkedPortal != null)
        {
            _linkedPortal._teleportingObjects.Remove(traveler);
        }

        // Debug.Log($"'{traveler.name}' removed from portal cooldown for '{name}' and '{_linkedPortal?.name ?? "null link"}'.", this);
    }

    /// <summary>
    /// Draw Gizmos in the editor to visualize the portal's position and orientation.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw the portal's position as a sphere
        Gizmos.color = _gizmoColor;
        Gizmos.DrawSphere(transform.position, _gizmoRadius);

        // Draw an arrow representing the portal's forward direction (exit direction by default)
        Vector3 arrowDirection = transform.forward * _gizmoRadius * 1.5f;
        Gizmos.DrawRay(transform.position, arrowDirection);
        Gizmos.DrawSphere(transform.position + arrowDirection, _gizmoRadius * 0.2f); // Arrowhead

        // Draw a line to the linked portal
        if (_linkedPortal != null)
        {
            Gizmos.color = Color.magenta; // Different color for the link
            Gizmos.DrawLine(transform.position, _linkedPortal.transform.position);

            // Draw the linked portal's specific exit point if it has one
            if (_linkedPortal._teleportExitPoint != null)
            {
                Gizmos.color = Color.yellow; // Highlight the specific exit point
                Gizmos.DrawWireSphere(_linkedPortal._teleportExitPoint.position, _gizmoRadius * 0.8f);
                Gizmos.DrawRay(_linkedPortal._teleportExitPoint.position, _linkedPortal._teleportExitPoint.forward * _gizmoRadius);
            }
        }
    }

    /// <summary>
    /// Draw Gizmos when the object is selected.
    /// This allows for more detailed visualization, like the trigger collider bounds.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_portalCollider != null)
        {
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.3f); // Semi-transparent

            // Draw the collider bounds to visualize the trigger area
            if (_portalCollider is BoxCollider box)
            {
                // Need to use Gizmos.matrix for correctly scaled and rotated box colliders
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity; // Reset matrix
            }
            else if (_portalCollider is SphereCollider sphere)
            {
                // For sphere colliders, scale is applied to radius
                Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
            }
            // Add other collider types as needed (e.g., CapsuleCollider)
        }
    }
}
```

---

### Example Usage in Unity Editor

Here's how to set up and use the `Portal.cs` script in your Unity project:

**1. Create the Portal GameObjects:**

*   In your scene, create two empty GameObjects. Name them `PortalA` and `PortalB`.
*   You can also add a visual mesh (e.g., a simple Quad or a 3D model) as a child of each portal GameObject to make them visible. Rotate them to face in a desired direction (e.g., `transform.forward` will be the "front" of the portal).

**2. Configure Each Portal GameObject:**

For both `PortalA` and `PortalB`:

*   **Add Component:** Add the `Portal` script to the GameObject.
*   **Add Collider:** Add a `Box Collider` (or `Sphere Collider` / `Capsule Collider`) component.
    *   **Crucially, set `Is Trigger` to `true`**. (The `Portal.cs` script will also ensure this in `Awake`, but it's good practice to set it manually).
    *   Adjust the `Size` of the collider to define the active area of your portal (e.g., X=2, Y=3, Z=0.5 for a rectangular portal). The 'Z' direction typically represents the depth of the portal.

**3. Link the Portals:**

*   **For `PortalA`'s Inspector:**
    *   Drag the `PortalB` GameObject from your Hierarchy into `PortalA`'s `Linked Portal` slot.
    *   Set `Traveler Layer` to the layer your player or objects that should use the portal are on (e.g., "Default" or a custom "Player" layer).
    *   Adjust `Teleport Cooldown` (e.g., `1.0` second) to prevent rapid re-teleportation.
    *   Configure `Maintain Velocity` and `Maintain Orientation` based on your desired physics behavior.

*   **For `PortalB`'s Inspector:**
    *   Drag the `PortalA` GameObject from your Hierarchy into `PortalB`'s `Linked Portal` slot.
    *   Set `Traveler Layer` to the same layer as `PortalA`.
    *   Adjust `Teleport Cooldown`, `Maintain Velocity`, and `Maintain Orientation` as desired.

**4. (Optional) Define Custom Exit Points:**

*   For `PortalA`, if you want objects to exit `PortalA` at a *specific point and orientation* different from `PortalA`'s main transform:
    *   Create an empty GameObject as a child of `PortalA`. Name it something like `PortalA_ExitPoint`.
    *   Position and rotate `PortalA_ExitPoint` where you want objects to appear and face when they exit `PortalA`.
    *   Drag this `PortalA_ExitPoint` GameObject into `PortalA`'s `Teleport Exit Point` slot in the Inspector.
*   Repeat the same process for `PortalB` if needed (e.g., `PortalB_ExitPoint` for `PortalB`'s `Teleport Exit Point` slot).

**5. Create a "Traveler" Object:**

*   Create a 3D Object (e.g., a Cube, Sphere, or your Player character model). Name it "Traveler" or "Player".
*   **Add a `Rigidbody` component** to this object. This is essential for `OnTriggerEnter` to work with physics objects and for maintaining velocity/angular velocity.
*   Ensure its `Layer` matches the `Traveler Layer` you set on your portals (e.g., "Default").
*   Position your "Traveler" object in front of `PortalA`.

**6. Run the Scene!**

*   Move your "Traveler" object into `PortalA`. It should instantaneously teleport to `PortalB`.
*   Move it into `PortalB`, and it should teleport back to `PortalA`.
*   Experiment with the `Maintain Velocity` and `Maintain Orientation` settings.

---

### Key Concepts of the PortalSystem Pattern Demonstrated:

1.  **Portal Representation:** Each `Portal` script instance acts as a distinct portal.
2.  **Linked Portals:** Portals are explicitly linked (`_linkedPortal` field) to define their entry-exit relationships. This allows for one-way or two-way portal pairs.
3.  **Traveler Detection:** `OnTriggerEnter` is used to detect when a valid "traveler" (an object on the specified `_travelerLayer` with a `Rigidbody` or `CharacterController`) enters the portal's trigger volume.
4.  **Teleportation Logic:** The `TeleportTraveler` method calculates the new position and rotation for the traveler based on its position/rotation relative to the entry portal and the orientation of the exit portal.
5.  **Physics Handling:** If the traveler has a `Rigidbody`, its `velocity` and `angularVelocity` are transformed and applied to maintain consistent motion across the portal.
6.  **Cooldown Mechanism:** The `_teleportingObjects` `HashSet` and `_teleportCooldown` prevent an object from being immediately re-teleported (either by the same portal it just entered, or by the linked portal it just exited), avoiding infinite loops or rapid flickering.
7.  **Custom Exit Points:** The optional `_teleportExitPoint` allows fine-grained control over exactly where an object appears and which way it faces after teleporting, separate from the main portal transform.
8.  **Editor Visualization:** `OnDrawGizmos` and `OnDrawGizmosSelected` provide clear visual feedback in the Unity Editor, showing portal locations, their forward directions, and their links.

This example provides a robust and flexible foundation for implementing portal mechanics in a wide range of Unity games.