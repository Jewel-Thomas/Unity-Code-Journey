// Unity Design Pattern Example: MountSystem
// This script demonstrates the MountSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MountSystem' design pattern, while not a standard Gang of Four pattern, is a common and practical design for games where entities (like players or NPCs) can attach themselves to and control other entities (like vehicles, animals, or even static positions). It promotes a clean separation of concerns between the "mounter" and the "mountable" and provides a robust mechanism for managing their relationship, state, and control flow.

This example provides a complete, production-ready C# Unity script demonstrating the MountSystem pattern.

**Key Design Pattern Principles Demonstrated:**

*   **Interfaces (`IMounter`, `IMountable`):** Define clear contracts for what an object needs to *do* to be a mounter or a mountable. This promotes loose coupling; any class implementing these interfaces can participate in the system without the core logic needing to know its concrete type.
*   **Encapsulation:** Each component (`CharacterMounter`, `VehicleMountable`) manages its own internal state and specific logic (e.g., enabling/disabling its own controllers, handling its rigidbody).
*   **Events (`System.Action`):** Provide a flexible way for other systems (like UI, sound, or animation) to react to mounting/unmounting actions without creating direct dependencies on the specific `CharacterMounter` or `VehicleMountable` classes.
*   **Separation of Concerns:**
    *   `CharacterMounter` focuses solely on its role as a Mounter.
    *   `VehicleMountable` focuses solely on its role as a Mountable.
    *   `PlayerInputController` handles player input and interaction logic.
    *   `VehicleController` handles vehicle-specific movement logic.
    *   `MountPoint` simply defines a location.
    This makes the system modular, easier to understand, and maintain.
*   **Practical Unity Application:** Leverages `MonoBehaviour` for components, `Rigidbody` for physics, `Transform` parenting for visual attachment, `LayerMask` and `OverlapSphere` for interaction detection, and common Unity editor workflows.

---

## `MountSystemExample.cs`

To use this example:

1.  **Create a C# Script:** Create a new C# script named `MountSystemExample.cs` in your Unity project.
2.  **Copy & Paste:** Copy the entire code block below and paste it into the new `MountSystemExample.cs` file, replacing its default content.
3.  **Follow Setup Instructions:** Refer to the "Example Usage in the Unity Editor" section at the end of the script for detailed steps on how to set up your GameObjects in the Unity editor.

```csharp
using UnityEngine;
using System; // For Action
using System.Collections.Generic; // For generic lists if needed, but not directly for this pattern core.

/// <summary>
/// INTERFACE: IMounter
/// Defines the contract for any object that can mount other objects.
/// This interface ensures that any Mounter has a consistent way to initiate
/// and respond to mounting and unmounting actions.
/// </summary>
public interface IMounter
{
    /// <summary>
    /// A reference to the GameObject this Mounter component is attached to.
    /// Useful for debugging and general GameObject interactions.
    /// </summary>
    GameObject MounterGameObject { get; }

    /// <summary>
    /// The IMountable object currently mounted by this Mounter.
    /// Will be null if the Mounter is not currently mounting anything.
    /// </summary>
    IMountable CurrentMounted { get; }

    /// <summary>
    /// True if this Mounter is currently mounting an object; otherwise, false.
    /// </summary>
    bool IsMounting { get; }

    /// <summary>
    /// Attempts to mount the specified target IMountable.
    /// This method orchestrates the mounting process, checking conditions and
    /// informing the target mountable.
    /// </summary>
    /// <param name="target">The IMountable to attempt to mount.</param>
    /// <returns>True if the mounting process was successful, false otherwise.</returns>
    bool TryMount(IMountable target);

    /// <summary>
    /// Attempts to unmount from the currently mounted object.
    /// This method orchestrates the unmounting process, restoring the Mounter's
    /// state and informing the mounted object.
    /// </summary>
    /// <returns>True if the unmounting process was successful, false otherwise.</returns>
    bool TryUnmount();

    /// <summary>
    /// Event fired when this Mounter successfully mounts an object.
    /// Subscribers can react to a new mount (e.g., play animation, change UI).
    /// </summary>
    event Action<IMountable> OnMounted;

    /// <summary>
    /// Event fired when this Mounter successfully unmounts from an object.
    /// Subscribers can react to an unmount (e.g., stop animation, restore UI).
    /// </summary>
    event Action<IMountable> OnUnmounted;
}

/// <summary>
/// INTERFACE: IMountable
/// Defines the contract for any object that can be mounted by other objects.
/// This interface ensures that any Mountable has a consistent way to report its
/// mount status, provide a mount point, and accept mount/unmount commands.
/// </summary>
public interface IMountable
{
    /// <summary>
    /// A reference to the GameObject this Mountable component is attached to.
    /// </summary>
    GameObject MountableGameObject { get; }

    /// <summary>
    /// The IMounter object currently mounting this Mountable.
    /// Will be null if the Mountable is not currently mounted.
    /// </summary>
    IMounter CurrentMounter { get; }

    /// <summary>
    /// True if this Mountable is currently mounted by an object; otherwise, false.
    /// </summary>
    bool IsMounted { get; }

    /// <summary>
    /// Returns the Transform that defines the position and orientation where a Mounter
    /// should be placed when mounted. This is crucial for visual and logical attachment.
    /// </summary>
    /// <returns>The Transform representing the mount point.</returns>
    Transform GetMountPoint();

    /// <summary>
    /// Allows a potential Mounter to check if this Mountable can be mounted by them.
    /// This method encapsulates custom logic for mount eligibility (e.g., check Mounter type,
    /// capacity, player reputation, quest status).
    /// </summary>
    /// <param name="potentialMounter">The IMounter attempting to mount.</param>
    /// <returns>True if the Mounter can mount this object, false otherwise.</returns>
    bool CanBeMountedBy(IMounter potentialMounter);

    /// <summary>
    /// Internal method called by an IMounter to signal that it is attempting to mount this object.
    /// The Mountable uses this to update its internal state and respond to the mounting action.
    /// </summary>
    /// <param name="mounter">The IMounter that is mounting this object.</param>
    /// <returns>True if the mount was successfully processed and accepted, false otherwise.</returns>
    bool ReceiveMountCommand(IMounter mounter);

    /// <summary>
    /// Internal method called by an IMounter to signal that it is attempting to unmount from this object.
    /// The Mountable uses this to update its internal state and respond to the unmounting action.
    /// </summary>
    /// <param name="mounter">The IMounter that is unmounting from this object.</param>
    /// <returns>True if the unmount was successfully processed and accepted, false otherwise.</returns>
    bool ReceiveUnmountCommand(IMounter mounter);

    /// <summary>
    /// Event fired when this Mountable is successfully mounted by an IMounter.
    /// Subscribers can react to being mounted (e.g., change vehicle state, play sounds).
    /// </summary>
    event Action<IMounter> OnMountedBy;

    /// <summary>
    /// Event fired when this Mountable is successfully unmounted by an IMounter.
    /// Subscribers can react to being unmounted (e.g., restore vehicle state, disable mounted controls).
    /// </summary>
    event Action<IMounter> OnUnmountedBy;
}


/// <summary>
/// HELPER COMPONENT: MountPoint
/// A simple component to mark a GameObject as a designated mount point on a mountable object.
/// This allows designers to easily place and configure exactly where the mounter will sit
/// (position and rotation) relative to the mountable object.
/// </summary>
public class MountPoint : MonoBehaviour
{
    // No specific logic needed here. Its primary purpose is to hold a Transform
    // and provide a clear visual indicator in the editor for where a Mounter will be placed.
    [Tooltip("The position and rotation where a mounter will be placed.")]
    public Transform PointTransform => transform;

    // Optional: Draw a small gizmo in the editor to visualize the mount point.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f); // Forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 0.2f); // Right direction
    }
}


/// <summary>
/// CONCRETE IMPLEMENTATION: CharacterMounter
/// An example MonoBehaviour that implements the IMounter interface.
/// Represents a character (e.g., player, NPC) that has the ability to mount vehicles or creatures.
/// It manages the character's state changes when mounting/unmounting, such as physics and control.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Characters typically have a Rigidbody for physics interaction.
public class CharacterMounter : MonoBehaviour, IMounter
{
    [Header("Mounter Settings")]
    [Tooltip("The Mounter's Rigidbody. It will be set to kinematic when mounted to prevent physics conflicts.")]
    [SerializeField] private Rigidbody _rigidbody;

    [Tooltip("Any MonoBehaviour components that control the character's movement (e.g., PlayerMovement, CharacterController). " +
             "These will be disabled when mounted and re-enabled when unmounted, allowing the mounted object to take control.")]
    [SerializeField] private MonoBehaviour[] _characterMovementComponents;

    [Tooltip("Offset applied to the character's local position when unmounting. This helps prevent " +
             "immediate re-collision or the character falling into the mountable upon detachment.")]
    [SerializeField] private Vector3 _unmountOffset = new Vector3(0, 0.5f, -1.0f); // Move a bit up and back from mount point

    // IMounter Properties
    public GameObject MounterGameObject => gameObject;
    public IMountable CurrentMounted { get; private set; }
    public bool IsMounting => CurrentMounted != null;

    // IMounter Events
    public event Action<IMountable> OnMounted;
    public event Action<IMountable> OnUnmounted;

    // Internal state variables to restore the Mounter's original transform and physics state upon unmounting.
    private Transform _originalParent;
    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private bool _rigidbodyWasKinematic; // Stores the Rigidbody's kinematic state before mounting

    private void Awake()
    {
        // Auto-assign Rigidbody if not set in Inspector.
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
        if (_rigidbody == null)
        {
            Debug.LogError($"CharacterMounter on {gameObject.name} requires a Rigidbody component to be assigned or present on the GameObject.");
            enabled = false; // Disable script if essential component is missing.
            return;
        }

        // Store original rigidbody state for restoration upon unmounting.
        _rigidbodyWasKinematic = _rigidbody.isKinematic;
    }

    /// <summary>
    /// Attempts to mount the given IMountable target.
    /// This method orchestrates the mounting process by performing checks,
    /// updating the Mounter's state, attaching it to the Mountable, and
    /// notifying both parties.
    /// </summary>
    /// <param name="target">The IMountable to attempt to mount.</param>
    /// <returns>True if mounting was successful, false otherwise.</returns>
    public bool TryMount(IMountable target)
    {
        // --- Pre-mount checks ---
        if (IsMounting)
        {
            Debug.LogWarning($"{MounterGameObject.name} is already mounting {CurrentMounted.MountableGameObject.name}. Cannot mount {target.MountableGameObject.name}.");
            return false;
        }

        if (target == null)
        {
            Debug.LogError($"{MounterGameObject.name} attempted to mount a null target.");
            return false;
        }

        if (!target.CanBeMountedBy(this))
        {
            Debug.Log($"{MounterGameObject.name} cannot mount {target.MountableGameObject.name}. (Mountable denied access based on its rules).");
            return false;
        }

        if (target.IsMounted)
        {
            Debug.Log($"{target.MountableGameObject.name} is already mounted by {target.CurrentMounter.MounterGameObject.name}. Cannot mount.");
            return false;
        }

        // --- Core Mounting Logic ---
        // 1. Tell the Mountable to accept the mount command. The Mountable has final say.
        if (!target.ReceiveMountCommand(this))
        {
            Debug.Log($"{target.MountableGameObject.name} rejected mount command from {MounterGameObject.name}. Mounting failed.");
            return false;
        }

        // 2. Update Mounter's internal state.
        CurrentMounted = target;

        // 3. Store original transform information (parent, local position/rotation)
        //    so the Mounter can return to its original state upon unmounting.
        _originalParent = transform.parent;
        _originalLocalPosition = transform.localPosition;
        _originalLocalRotation = transform.localRotation;

        // 4. Position and parent the Mounter to the Mountable's designated mount point.
        Transform mountPoint = target.GetMountPoint();
        if (mountPoint == null)
        {
            Debug.LogError($"{target.MountableGameObject.name} has no mount point defined! Unmounting to clean up.");
            TryUnmountInternal(false); // Clean up the partially mounted state.
            return false;
        }

        transform.SetParent(mountPoint);
        transform.localPosition = Vector3.zero;     // Snap to mount point's local origin.
        transform.localRotation = Quaternion.identity; // Align with mount point's local rotation.

        // 5. Disable the character's own movement/physics components and set its Rigidbody to kinematic.
        //    This prevents the character's physics from interfering with the mounted object's movement
        //    and allows the mounted object to fully control the Mounter's position.
        SetCharacterControlActive(false);
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true; // Make Rigidbody kinematic to ignore physics forces.
            _rigidbody.velocity = Vector3.zero; // Stop any current movement.
            _rigidbody.angularVelocity = Vector3.zero; // Stop any current rotation.
        }

        Debug.Log($"{MounterGameObject.name} successfully mounted {target.MountableGameObject.name}.");

        // 6. Fire the OnMounted event to notify any subscribers.
        OnMounted?.Invoke(target);
        return true;
    }

    /// <summary>
    /// Attempts to unmount from the currently mounted object.
    /// This method orchestrates the unmounting process, restoring the Mounter's
    /// previous state, detaching it from the Mountable, and notifying both parties.
    /// </summary>
    /// <returns>True if unmounting was successful, false otherwise.</returns>
    public bool TryUnmount()
    {
        if (!IsMounting)
        {
            Debug.LogWarning($"{MounterGameObject.name} is not currently mounting anything. Cannot unmount.");
            return false;
        }

        // Call internal unmount method, ensuring the mountable is notified.
        return TryUnmountInternal(true);
    }

    /// <summary>
    /// Internal helper method for unmounting. This allows skipping the mountable interaction
    /// in scenarios like error cleanup during a failed mount attempt.
    /// </summary>
    /// <param name="notifyMountable">If true, the mounted object will be told to unmount.
    /// Set to false for internal cleanup if the mountable couldn't be mounted in the first place.</param>
    /// <returns>True if unmounting was successful, false otherwise.</returns>
    private bool TryUnmountInternal(bool notifyMountable)
    {
        // Keep a reference to the mounted object before clearing CurrentMounted.
        IMountable mounted = CurrentMounted;

        // 1. If requested, tell the Mountable to accept the unmount command.
        //    The Mountable might deny unmount for various reasons (e.g., in combat).
        if (notifyMountable && mounted != null && !mounted.ReceiveUnmountCommand(this))
        {
            Debug.LogWarning($"{mounted.MountableGameObject.name} rejected unmount command from {MounterGameObject.name}. Unmounting failed on Mounter side.");
            return false; // Mountable denied unmount.
        }

        // 2. Restore the Mounter's original transform (parent, local position, local rotation).
        transform.SetParent(_originalParent);
        // Apply an offset to the local position to prevent immediate re-collision or falling into the mount.
        transform.localPosition = _originalLocalPosition + _unmountOffset;
        transform.localRotation = _originalLocalRotation;

        // 3. Re-enable the character's own movement/physics components and restore its Rigidbody's kinematic state.
        SetCharacterControlActive(true);
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = _rigidbodyWasKinematic; // Restore original kinematic state.
        }

        // 4. Clear the Mounter's internal state.
        CurrentMounted = null;
        _originalParent = null; // Clear reference to avoid stale data.

        Debug.Log($"{MounterGameObject.name} successfully unmounted from {mounted?.MountableGameObject.name ?? "unknown mountable"}.");

        // 5. Fire the OnUnmounted event to notify any subscribers.
        OnUnmounted?.Invoke(mounted);
        return true;
    }

    /// <summary>
    /// Helper method to enable or disable the character's movement control components.
    /// This is crucial for switching control between the character and the mounted object.
    /// </summary>
    /// <param name="active">True to enable components, false to disable them.</param>
    private void SetCharacterControlActive(bool active)
    {
        foreach (MonoBehaviour component in _characterMovementComponents)
        {
            if (component != null)
            {
                component.enabled = active;
            }
        }
    }
}


/// <summary>
/// CONCRETE IMPLEMENTATION: VehicleMountable
/// An example MonoBehaviour that implements the IMountable interface.
/// Represents a vehicle, creature, or any object that can be mounted by a character.
/// It manages the vehicle's state changes when mounted/unmounted, such as control input.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Vehicles typically have a Rigidbody for physics interaction.
public class VehicleMountable : MonoBehaviour, IMountable
{
    [Header("Mountable Settings")]
    [Tooltip("The MountPoint component that defines where a Mounter will be positioned when mounted.")]
    [SerializeField] private MountPoint _mountPoint;

    [Tooltip("Any MonoBehaviour components that control the vehicle's movement (e.g., CarController, AIController). " +
             "These will typically be disabled when a character is mounted, as control is transferred.")]
    [SerializeField] private MonoBehaviour[] _vehicleControlComponents;

    [Tooltip("Optional: A specific controller script to ENABLE when a character is mounted. " +
             "This allows for different control schemes when the vehicle is driven by a player vs. AI or unmounted.")]
    [SerializeField] private MonoBehaviour _mountedDriverController;

    // IMountable Properties
    public GameObject MountableGameObject => gameObject;
    public IMounter CurrentMounter { get; private set; }
    public bool IsMounted => CurrentMounter != null;

    // IMountable Events
    public event Action<IMounter> OnMountedBy;
    public event Action<IMounter> OnUnmountedBy;

    private void Awake()
    {
        if (_mountPoint == null)
        {
            Debug.LogError($"VehicleMountable on {gameObject.name} requires a MountPoint component assigned in the Inspector. Please create a child GameObject with a MountPoint script.");
            enabled = false;
            return;
        }

        // Ensure default controls are enabled initially, and the mounted driver controller is disabled.
        SetVehicleControlActive(true);
        if (_mountedDriverController != null)
        {
            _mountedDriverController.enabled = false;
        }
    }

    /// <summary>
    /// Returns the Transform of the designated mount point for this Mountable.
    /// </summary>
    /// <returns>The MountPoint's Transform.</returns>
    public Transform GetMountPoint()
    {
        return _mountPoint != null ? _mountPoint.PointTransform : transform;
    }

    /// <summary>
    /// Checks if this Mountable can be mounted by the given potential Mounter.
    /// This method is designed to be extensible for custom game logic.
    /// </summary>
    /// <param name="potentialMounter">The IMounter attempting to mount.</param>
    /// <returns>True if the Mounter can mount this object, false otherwise.</returns>
    public bool CanBeMountedBy(IMounter potentialMounter)
    {
        // Example logic: Only allow mounting if this vehicle is not already mounted.
        if (IsMounted)
        {
            Debug.Log($"{gameObject.name} is already mounted by {CurrentMounter.MounterGameObject.name}.");
            return false;
        }

        // Add more complex eligibility checks here if needed:
        // - Is the potentialMounter the correct type (e.g., player only)?
        // - Does the potentialMounter have required skills/items?
        // - Is the vehicle damaged?
        // - Is the vehicle locked?

        return true; // By default, if not already mounted, allow it.
    }

    /// <summary>
    /// Receives the mount command from an IMounter. Updates the Mountable's internal state,
    /// and manages its control components (disabling default, enabling mounted).
    /// </summary>
    /// <param name="mounter">The IMounter that is mounting this object.</param>
    /// <returns>True if the mount was successfully processed, false otherwise.</returns>
    public bool ReceiveMountCommand(IMounter mounter)
    {
        // Double-check: ensure it's not already mounted before accepting.
        if (IsMounted)
        {
            Debug.LogWarning($"{MountableGameObject.name} is already mounted. Denying mount command from {mounter.MounterGameObject.name}.");
            return false;
        }

        CurrentMounter = mounter;

        // Disable default vehicle controls (e.g., AI control, or basic unmounted movement).
        SetVehicleControlActive(false);

        // Enable the specific controller designed for when a player is mounted, if one is provided.
        if (_mountedDriverController != null)
        {
            _mountedDriverController.enabled = true;
        }

        Debug.Log($"{MountableGameObject.name} successfully accepted mount by {mounter.MounterGameObject.name}.");

        // Fire the OnMountedBy event to notify any subscribers.
        OnMountedBy?.Invoke(mounter);
        return true;
    }

    /// <summary>
    /// Receives the unmount command from an IMounter. Updates the Mountable's internal state,
    /// and restores its control components (disabling mounted, enabling default).
    /// </summary>
    /// <param name="mounter">The IMounter that is unmounting from this object.</param>
    /// <returns>True if the unmount was successfully processed, false otherwise.</returns>
    public bool ReceiveUnmountCommand(IMounter mounter)
    {
        // Double-check: ensure the correct mounter is requesting to unmount.
        if (!IsMounted || CurrentMounter != mounter)
        {
            Debug.LogWarning($"{MountableGameObject.name} is not mounted by {mounter.MounterGameObject.name}. Denying unmount command.");
            return false;
        }

        CurrentMounter = null;

        // Disable the mounted driver controller if it was enabled.
        if (_mountedDriverController != null)
        {
            _mountedDriverController.enabled = false;
        }

        // Re-enable default vehicle controls (e.g., AI takes over, or basic unmounted movement is restored).
        SetVehicleControlActive(true);

        Debug.Log($"{MountableGameObject.name} successfully accepted unmount by {mounter.MounterGameObject.name}.");

        // Fire the OnUnmountedBy event to notify any subscribers.
        OnUnmountedBy?.Invoke(mounter);
        return true;
    }

    /// <summary>
    /// Helper method to enable or disable the default vehicle control components.
    /// This is used to switch between unmounted/AI control and player control.
    /// </summary>
    /// <param name="active">True to enable components, false to disable them.</param>
    private void SetVehicleControlActive(bool active)
    {
        foreach (MonoBehaviour component in _vehicleControlComponents)
        {
            if (component != null)
            {
                component.enabled = active;
            }
        }
    }
}


/// <summary>
/// EXAMPLE: PlayerInputController
/// This script simulates a player character's input handling, including movement
/// when unmounted and interaction with nearby mountables.
/// It works in conjunction with CharacterMounter.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player characters typically need a Rigidbody.
public class PlayerInputController : MonoBehaviour
{
    [Header("Player Control Settings")]
    [Tooltip("The CharacterMounter component on this GameObject.")]
    [SerializeField] private CharacterMounter _characterMounter;

    [Tooltip("The speed at which the character moves when not mounted.")]
    [SerializeField] private float _characterMoveSpeed = 5f;

    [Tooltip("The layer(s) that mountable objects reside on, for proximity detection (e.g., 'Mountables' layer).")]
    [SerializeField] private LayerMask _mountableLayer;

    [Tooltip("The radius for detecting nearby mountables for interaction.")]
    [SerializeField] private float _detectionRadius = 2f;

    private Rigidbody _playerRigidbody;
    private IMountable _nearbyMountable; // The closest mountable currently in range.

    private void Awake()
    {
        // Auto-assign CharacterMounter if not set in Inspector.
        if (_characterMounter == null)
        {
            _characterMounter = GetComponent<CharacterMounter>();
            if (_characterMounter == null)
            {
                Debug.LogError("PlayerInputController requires a CharacterMounter component on the same GameObject or assigned.");
                enabled = false;
                return;
            }
        }
        // Auto-assign Rigidbody if not set in Inspector.
        _playerRigidbody = GetComponent<Rigidbody>();
        if (_playerRigidbody == null)
        {
            Debug.LogError("PlayerInputController requires a Rigidbody component on the same GameObject.");
            enabled = false;
        }
    }

    private void Update()
    {
        // Only handle direct character movement if this script is enabled (meaning character is not mounted).
        HandleMovementInput();
        HandleMountInteractInput();
        DetectNearbyMountables();

        // Optional: Display debug information or UI prompt if a mountable is nearby.
        if (_nearbyMountable != null && !_characterMounter.IsMounting)
        {
            Debug.DrawLine(transform.position, _nearbyMountable.MountableGameObject.transform.position, Color.yellow);
            // In a real game, you might show a UI prompt like "Press 'E' to Mount [Vehicle Name]".
        }
    }

    /// <summary>
    /// Handles player input for character movement when not mounted.
    /// This method is only active when the CharacterMounter enables this script.
    /// </summary>
    private void HandleMovementInput()
    {
        // The 'enabled' state of this script is controlled by CharacterMounter.
        // If this script is enabled, it means the character is NOT mounted,
        // so its direct movement controls should be active.
        if (enabled)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

            if (moveDirection.magnitude > 0.1f)
            {
                // Simple Rigidbody-based movement.
                _playerRigidbody.MovePosition(_playerRigidbody.position + moveDirection * _characterMoveSpeed * Time.deltaTime);
                // Rotate character to face movement direction.
                transform.rotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z));
            }
            else
            {
                // If no input, stop movement to prevent sliding (useful for physics-based movement).
                _playerRigidbody.velocity = Vector3.zero;
                _playerRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Handles player input for mounting/unmounting actions.
    /// Uses the 'E' key as an example interaction button.
    /// </summary>
    private void HandleMountInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Example interact key
        {
            if (_characterMounter.IsMounting)
            {
                // If currently mounted, try to unmount.
                _characterMounter.TryUnmount();
            }
            else
            {
                // If not mounted, and a mountable is detected, try to mount it.
                if (_nearbyMountable != null)
                {
                    _characterMounter.TryMount(_nearbyMountable);
                }
                else
                {
                    Debug.Log("No mountable detected in range to interact with.");
                }
            }
        }
    }

    /// <summary>
    /// Detects the closest IMountable object within the specified detection radius.
    /// Uses Physics.OverlapSphere to find potential mountables.
    /// </summary>
    private void DetectNearbyMountables()
    {
        // OverlapSphere finds all colliders within a sphere.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _detectionRadius, _mountableLayer);
        _nearbyMountable = null; // Reset the closest mountable.
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            // Try to get the IMountable component from the collider's GameObject or its parent.
            // This handles cases where the collider is on a child GameObject of the actual mountable.
            IMountable mountable = hitCollider.GetComponentInParent<IMountable>();
            if (mountable != null)
            {
                // Check if this mountable is closer than the previously found one.
                float distance = Vector3.Distance(transform.position, mountable.MountableGameObject.transform.position);
                if (distance < closestDistance)
                {
                    _nearbyMountable = mountable;
                    closestDistance = distance;
                }
            }
        }
    }

    /// <summary>
    /// Draws a gizmo in the editor to visualize the detection radius.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}


/// <summary>
/// EXAMPLE: VehicleController
/// This script simulates the control of a vehicle. It's intended to be enabled
/// only when a player is mounted, typically by the VehicleMountable component.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Vehicles typically need a Rigidbody.
public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Control Settings")]
    [Tooltip("The speed at which the vehicle moves.")]
    [SerializeField] private float _vehicleMoveSpeed = 10f;

    [Tooltip("The rotation speed of the vehicle.")]
    [SerializeField] private float _vehicleRotationSpeed = 100f;

    private Rigidbody _vehicleRigidbody;

    private void Awake()
    {
        _vehicleRigidbody = GetComponent<Rigidbody>();
        if (_vehicleRigidbody == null)
        {
            Debug.LogError("VehicleController requires a Rigidbody component on the same GameObject.");
            enabled = false;
        }

        // Initially disable this controller. It will be enabled by VehicleMountable
        // when a Mounter successfully mounts this vehicle.
        enabled = false;
    }

    private void FixedUpdate()
    {
        // These input checks will only be processed when this component is enabled.
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrows for steering.
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down Arrows for acceleration/braking.

        // Forward/backward movement for the vehicle.
        Vector3 moveDirection = transform.forward * vertical * _vehicleMoveSpeed * Time.fixedDeltaTime;
        _vehicleRigidbody.MovePosition(_vehicleRigidbody.position + moveDirection);

        // Rotation (steering) for the vehicle.
        Quaternion turnRotation = Quaternion.Euler(0f, horizontal * _vehicleRotationSpeed * Time.fixedDeltaTime, 0f);
        _vehicleRigidbody.MoveRotation(_vehicleRigidbody.rotation * turnRotation);
    }
}

// --- Example Usage in the Unity Editor ---
/*
*   How to set up your scene in Unity to demonstrate the MountSystem:
*
*   1.  **Create a C# Script:**
*       -   Create a new C# script named `MountSystemExample.cs` in your project (e.g., in a 'Scripts' folder).
*       -   Copy and paste the ENTIRE code block above into this script, replacing any default content.
*
*   2.  **Create a 'Mountables' Layer:**
*       -   Go to `Edit > Project Settings > Tags & Layers`.
*       -   In the `Layers` section, find an empty "User Layer" slot (e.g., User Layer 8).
*       -   Rename it to `Mountables`.
*
*   3.  **Create Player GameObject:**
*       -   Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty). Name it `Player`.
*       -   Add a `Rigidbody` component to `Player`.
*           -   In the Rigidbody component, set `Collision Detection` to `Continuous` (recommended for character physics).
*           -   Freeze `Rotation X` and `Rotation Z` to prevent the player from tipping over.
*       -   Add a `Capsule Collider` component to `Player`.
*           -   Adjust its `Center` and `Height` to fit your player model (e.g., Center Y: 1, Height: 2).
*       -   Add a visual representation (optional): Right-click `Player` -> 3D Object -> `Cube`.
*           -   Rename the Cube to `Visual` and set its `Scale` to `(1, 2, 1)`. This will serve as your player model.
*       -   Add the `CharacterMounter` script to `Player`.
*           -   In the Inspector, drag `Player`'s `Rigidbody` into the `_rigidbody` slot of `CharacterMounter` (it should auto-fill if `CharacterMounter` is added after Rigidbody).
*       -   Add the `PlayerInputController` script to `Player`.
*           -   In the Inspector, `_characterMounter` should auto-fill.
*           -   For `_mountableLayer`, select `Mountables` from the dropdown.
*           -   Adjust `_characterMoveSpeed` and `_detectionRadius` as desired (e.g., 5 for speed, 2 for radius).
*
*   4.  **Create Vehicle GameObject:**
*       -   Create an empty GameObject. Name it `Vehicle_Horse`.
*       -   Set its `Layer` to `Mountables` (select from the dropdown at the top of the Inspector).
*       -   Add a `Rigidbody` component to `Vehicle_Horse`.
*           -   Freeze `Rotation X` and `Rotation Z` (common for vehicles).
*       -   Add a `Box Collider` component to `Vehicle_Horse`.
*           -   Adjust its `Size` and `Center` to fit your vehicle model (e.g., Size X: 1, Y: 1.5, Z: 3, Center Y: 0.75).
*       -   Add a visual representation (optional): Right-click `Vehicle_Horse` -> 3D Object -> `Capsule`.
*           -   Rename it `Visual`, set its `Scale` to `(1.5, 1, 3)`, and `Rotation Y` to `90`.
*       -   Add the `VehicleMountable` script to `Vehicle_Horse`.
*       -   Add the `VehicleController` script to `Vehicle_Horse`.
*       -   **Create a child MountPoint:**
*           -   Right-click `Vehicle_Horse` -> Create Empty. Name it `MountPoint`.
*           -   Position this `MountPoint` child GameObject exactly where you want the player character to sit on the vehicle (e.g., Local Position Y: 1, Z: 0.5).
*           -   Add the `MountPoint` script to this `MountPoint` child GameObject.
*       -   **Configure `VehicleMountable` (on `Vehicle_Horse`):**
*           -   Drag the `MountPoint` child GameObject into the `_mountPoint` slot.
*           -   Drag the `VehicleController` script (from `Vehicle_Horse` itself) into the `_mountedDriverController` slot. (This means the `VehicleController` will be active ONLY when a player is mounted). If you had AI controls, you'd put them in `_vehicleControlComponents`.
*
*   5.  **Add a Floor:**
*       -   Create a 3D Object -> `Plane` to give your player and vehicle something to stand on.
*
*   6.  **Run the Scene:**
*       -   Play the scene.
*       -   Use WASD to move the player character.
*       -   Move the player character close to the `Vehicle_Horse`.
*       -   Press 'E' to mount the vehicle. You should see the player snap to the `MountPoint` on the vehicle.
*       -   Now, use WASD to control the vehicle.
*       -   Press 'E' again to unmount. The player should detach from the vehicle (with a slight offset) and regain control.
*
*   This setup provides a complete, interactive demonstration of the MountSystem pattern in Unity.
*/
```