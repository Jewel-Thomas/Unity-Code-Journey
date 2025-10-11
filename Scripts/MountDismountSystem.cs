// Unity Design Pattern Example: MountDismountSystem
// This script demonstrates the MountDismountSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the `MountDismountSystem` design pattern. It's designed to be practical, educational, and ready to drop into your Unity project.

The core idea of the Mount/Dismount pattern is to cleanly separate the responsibilities of:
1.  **The Mountable:** An entity that *can be mounted* (e.g., a player, an item). It needs to know how to react when it's mounted or dismounted.
2.  **The Mount:** An entity that *can be mounted upon* (e.g., a vehicle, a horse, a weapon slot). It needs to manage what's currently mounted on it and provide attachment points.
3.  **The System:** A central orchestrator that manages the act of mounting and dismounting, ensuring both parties are updated correctly and enforcing rules.

---

### How to Use This Example in Unity:

1.  **Create a New C# Script:** Name it `MountDismountSystemExample` and copy the entire code below into it.
2.  **Create a Player GameObject:**
    *   Create an empty GameObject, name it `Player`.
    *   Add a `Capsule Collider` (or any collider that fits your character).
    *   Add a `Rigidbody` (set `Is Kinematic` to `true` if you're not using physics-based movement for this example, or set constraints).
    *   Add a `Sphere Collider` and set `Is Trigger` to `true`. Adjust its radius (`~3-5`) to define the detection range for mounts.
    *   Attach the `PlayerCharacter` script component to it.
    *   Optionally, add a `Cube` as a child to visually represent the player.
3.  **Create a Vehicle GameObject:**
    *   Create an empty GameObject, name it `Vehicle`.
    *   Add a `Box Collider` (or any collider that represents your vehicle).
    *   Attach the `VehicleMount` script component to it.
    *   Optionally, add a `Cube` as a child to visually represent the vehicle.
4.  **Create a Mount Point for the Vehicle:**
    *   Create an empty GameObject as a child of `Vehicle`, name it `MountPoint`.
    *   Position this `MountPoint` where you want the player to sit/stand when mounted.
    *   In the `Vehicle`'s Inspector, drag the `MountPoint` GameObject into the `Mount Point` field of the `VehicleMount` script.
5.  **Run the Scene:**
    *   Move the `Player` close to the `Vehicle`.
    *   Press the **'E'** key to mount.
    *   Press the **'E'** key again to dismount.
    *   Observe the player's position, hierarchy in the Inspector, and console messages.

---

### MountDismountSystemExample.cs

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for lists if needed in a more complex system

/// <summary>
/// INTERFACE: IMountable
/// Defines the contract for any GameObject that can be mounted onto something else.
/// </summary>
/// <remarks>
/// This interface ensures that any object wishing to be mounted provides:
/// - A reference to its own GameObject for scene manipulation (e.g., parenting, positioning).
/// - A way to track its current mount.
/// - Callbacks for when it successfully mounts or dismounts.
/// </remarks>
public interface IMountable
{
    GameObject MountableGameObject { get; }
    IMount CurrentMount { get; set; }

    /// <summary>
    /// Called when this object is successfully mounted onto an IMount.
    /// </summary>
    /// <param name="mount">The IMount it was mounted upon.</param>
    void OnMount(IMount mount);

    /// <summary>
    /// Called when this object is successfully dismounted from an IMount.
    /// </summary>
    /// <param name="mount">The IMount it was dismounted from.</param>
    void OnDismount(IMount mount);
}

/// <summary>
/// INTERFACE: IMount
/// Defines the contract for any GameObject that can be mounted upon.
/// </summary>
/// <remarks>
/// This interface ensures that any object wishing to act as a mount provides:
/// - A reference to its own GameObject.
/// - A designated Transform where mountables should attach (the 'mount point').
/// - A way to track the current rider (if any).
/// - Methods to determine if a mountable *can* mount, and to perform the actual mount/dismount actions.
/// </remarks>
public interface IMount
{
    GameObject MountGameObject { get; }
    Transform GetMountPoint();
    IMountable CurrentRider { get; }

    /// <summary>
    /// Determines if a specific IMountable can currently mount this IMount.
    /// </summary>
    /// <param name="mountable">The IMountable attempting to mount.</param>
    /// <returns>True if mounting is allowed, false otherwise.</returns>
    bool CanMount(IMountable mountable);

    /// <summary>
    /// Handles the internal logic for this mount when an IMountable is attached.
    /// This typically involves parenting and setting state.
    /// </summary>
    /// <param name="mountable">The IMountable being attached.</param>
    /// <returns>True if the mount operation was successful from the mount's perspective.</returns>
    bool Mount(IMountable mountable);

    /// <summary>
    /// Handles the internal logic for this mount when an IMountable is detached.
    /// This typically involves unparenting and resetting state.
    /// </summary>
    /// <param name="mountable">The IMountable being detached.</param>
    /// <returns>True if the dismount operation was successful from the mount's perspective.</returns>
    bool Dismount(IMountable mountable);
}

/// <summary>
/// CORE SYSTEM: MountDismountSystem
/// A static utility class that orchestrates the mounting and dismounting process.
/// It acts as the central point of control, ensuring both the IMountable and IMount
/// interfaces are interacted with correctly.
/// </summary>
/// <remarks>
/// By centralizing the logic here, we prevent either the mountable or the mount
/// from having direct knowledge of each other's specific implementations, adhering
/// to the Single Responsibility Principle and promoting loose coupling.
/// </remarks>
public static class MountDismountSystem
{
    /// <summary>
    /// Attempts to mount an IMountable onto an IMount.
    /// This is the primary method to initiate a mount action.
    /// </summary>
    /// <param name="mountable">The object attempting to mount.</param>
    /// <param name="mount">The object being mounted upon.</param>
    /// <returns>True if the mount operation was entirely successful, false otherwise.</returns>
    public static bool TryMount(IMountable mountable, IMount mount)
    {
        if (mountable == null || mount == null)
        {
            Debug.LogError("MountDismountSystem: Mountable or Mount is null.");
            return false;
        }

        // 1. Check if the mountable is already mounted
        if (mountable.CurrentMount != null)
        {
            Debug.LogWarning($"{mountable.MountableGameObject.name} is already mounted on {mountable.CurrentMount.MountGameObject.name}. Dismount first.");
            return false;
        }

        // 2. Check if the mount can accept this mountable
        if (!mount.CanMount(mountable))
        {
            Debug.LogWarning($"{mountable.MountableGameObject.name} cannot mount {mount.MountGameObject.name}. (Reason: {mount.MountGameObject.name} already has a rider or other restriction).");
            return false;
        }

        // 3. Perform the actual mount operation on the mount
        if (!mount.Mount(mountable))
        {
            Debug.LogError($"MountDismountSystem: {mount.MountGameObject.name} failed to internally mount {mountable.MountableGameObject.name}.");
            return false;
        }

        // 4. Update the mountable's state
        mountable.CurrentMount = mount;
        mountable.OnMount(mount);

        Debug.Log($"MountDismountSystem: {mountable.MountableGameObject.name} successfully mounted {mount.MountGameObject.name}.");
        return true;
    }

    /// <summary>
    /// Attempts to dismount an IMountable from its current IMount.
    /// This is the primary method to initiate a dismount action.
    /// </summary>
    /// <param name="mountable">The object attempting to dismount.</param>
    /// <returns>True if the dismount operation was entirely successful, false otherwise.</returns>
    public static bool TryDismount(IMountable mountable)
    {
        if (mountable == null)
        {
            Debug.LogError("MountDismountSystem: Mountable is null.");
            return false;
        }

        // 1. Check if the mountable is actually mounted
        if (mountable.CurrentMount == null)
        {
            Debug.LogWarning($"{mountable.MountableGameObject.name} is not currently mounted.");
            return false;
        }

        IMount mount = mountable.CurrentMount;

        // 2. Perform the actual dismount operation on the mount
        if (!mount.Dismount(mountable))
        {
            Debug.LogError($"MountDismountSystem: {mount.MountGameObject.name} failed to internally dismount {mountable.MountableGameObject.name}.");
            return false;
        }

        // 3. Update the mountable's state
        mountable.CurrentMount = null;
        mountable.OnDismount(mount);

        Debug.Log($"MountDismountSystem: {mountable.MountableGameObject.name} successfully dismounted from {mount.MountGameObject.name}.");
        return true;
    }
}

/// <summary>
/// CONCRETE IMPLEMENTATION: PlayerCharacter
/// An example MonoBehaviour that acts as an IMountable.
/// Represents a player or character that can ride vehicles.
/// </summary>
[RequireComponent(typeof(Collider))] // Player needs a collider to detect vehicles
public class PlayerCharacter : MonoBehaviour, IMountable
{
    [Header("Mountable Properties")]
    [SerializeField] private float _dismountOffset = 1.5f; // Distance from mount point to dismount
    [SerializeField] private float _interactionDistance = 5f; // Max distance to interact with a mount

    private IMount _currentMount;
    private IMount _nearbyMount; // Reference to a mount detected in range

    // IMountable Implementation
    public GameObject MountableGameObject => gameObject;
    public IMount CurrentMount
    {
        get => _currentMount;
        set => _currentMount = value;
    }

    void Update()
    {
        // Example input for mounting/dismounting
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentMount == null)
            {
                // Try to mount if not currently mounted and a mount is nearby
                if (_nearbyMount != null)
                {
                    MountDismountSystem.TryMount(this, _nearbyMount);
                }
                else
                {
                    Debug.Log("No mount nearby to interact with.");
                }
            }
            else
            {
                // Try to dismount if currently mounted
                MountDismountSystem.TryDismount(this);
            }
        }

        // Basic movement for demonstration (when not mounted)
        if (_currentMount == null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            transform.Translate(new Vector3(horizontal, 0, vertical) * Time.deltaTime * 5f, Space.World);
        }
    }

    /// <summary>
    /// Callback when this character is mounted.
    /// Here, we might disable player input, change animation state, etc.
    /// </summary>
    /// <param name="mount">The mount it was attached to.</param>
    public void OnMount(IMount mount)
    {
        Debug.Log($"PlayerCharacter: {gameObject.name} has been mounted on {mount.MountGameObject.name}. Input disabled.");
        // Example: Disable player-specific movement/input controls
        // this.enabled = false; // Or disable a specific input script
        // Play 'sit' animation
    }

    /// <summary>
    /// Callback when this character is dismounted.
    /// Here, we might re-enable player input, change animation state, reposition.
    /// </summary>
    /// <param name="mount">The mount it was detached from.</param>
    public void OnDismount(IMount mount)
    {
        Debug.Log($"PlayerCharacter: {gameObject.name} has been dismounted from {mount.MountGameObject.name}. Input enabled.");
        // Example: Re-enable player-specific movement/input controls
        // this.enabled = true; // Or enable a specific input script

        // Reposition player slightly away from the mount point
        Vector3 dismountPosition = mount.GetMountPoint().position + transform.forward * _dismountOffset;
        transform.position = dismountPosition;
        transform.rotation = mount.MountGameObject.transform.rotation; // Align player rotation with mount
        // Play 'stand' animation
    }

    // --- Proximity Detection for Mounts ---
    private void OnTriggerEnter(Collider other)
    {
        IMount potentialMount = other.GetComponent<IMount>();
        if (potentialMount != null && _nearbyMount == null)
        {
            _nearbyMount = potentialMount;
            Debug.Log($"Player detected nearby mount: {_nearbyMount.MountGameObject.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IMount exitedMount = other.GetComponent<IMount>();
        if (exitedMount != null && _nearbyMount == exitedMount)
        {
            _nearbyMount = null;
            Debug.Log($"Player exited range of mount: {exitedMount.MountGameObject.name}");
        }
    }
}

/// <summary>
/// CONCRETE IMPLEMENTATION: VehicleMount
/// An example MonoBehaviour that acts as an IMount.
/// Represents a vehicle or object that a player can ride.
/// </summary>
public class VehicleMount : MonoBehaviour, IMount
{
    [Header("Mount Properties")]
    [Tooltip("The Transform where the mountable object will be positioned when mounted.")]
    [SerializeField] private Transform _mountPoint;

    private IMountable _currentRider;

    // IMount Implementation
    public GameObject MountGameObject => gameObject;
    public Transform GetMountPoint() => _mountPoint;
    public IMountable CurrentRider => _currentRider;

    void Awake()
    {
        if (_mountPoint == null)
        {
            Debug.LogError($"VehicleMount: {_mountPoint.name} has no Mount Point assigned! Mounting will not work correctly.", this);
            // Consider creating a default mount point if none is assigned
            GameObject defaultMountPoint = new GameObject("DefaultMountPoint");
            defaultMountPoint.transform.SetParent(transform);
            defaultMountPoint.transform.localPosition = Vector3.up; // Example default position
            _mountPoint = defaultMountPoint.transform;
        }
    }

    /// <summary>
    /// Determines if this vehicle can accept the given mountable.
    /// </summary>
    /// <param name="mountable">The mountable attempting to mount.</param>
    /// <returns>True if the vehicle is empty, false otherwise.</returns>
    public bool CanMount(IMountable mountable)
    {
        return _currentRider == null; // Only one rider allowed at a time for this vehicle
    }

    /// <summary>
    /// Handles the parenting and positioning of the mountable when it attaches.
    /// </summary>
    /// <param name="mountable">The mountable being attached.</param>
    /// <returns>True if the attachment was successful.</returns>
    public bool Mount(IMountable mountable)
    {
        if (!CanMount(mountable))
        {
            Debug.LogWarning($"VehicleMount: {mountable.MountableGameObject.name} cannot mount {gameObject.name}. Already occupied.");
            return false;
        }

        // Set the mountable's parent to the mount point
        mountable.MountableGameObject.transform.SetParent(_mountPoint);
        // Reset local position and rotation to fit the mount point
        mountable.MountableGameObject.transform.localPosition = Vector3.zero;
        mountable.MountableGameObject.transform.localRotation = Quaternion.identity;

        _currentRider = mountable;
        Debug.Log($"VehicleMount: {mountable.MountableGameObject.name} is now positioned on {gameObject.name}.");
        return true;
    }

    /// <summary>
    /// Handles unparenting the mountable when it detaches.
    /// </summary>
    /// <param name="mountable">The mountable being detached.</param>
    /// <returns>True if the detachment was successful.</returns>
    public bool Dismount(IMountable mountable)
    {
        if (_currentRider != mountable)
        {
            Debug.LogWarning($"VehicleMount: {mountable.MountableGameObject.name} is not the current rider of {gameObject.name}. Cannot dismount.");
            return false;
        }

        // Unparent the mountable from the mount point
        mountable.MountableGameObject.transform.SetParent(null);

        _currentRider = null;
        Debug.Log($"VehicleMount: {mountable.MountableGameObject.name} has been unparented from {gameObject.name}.");
        return true;
    }
}
```