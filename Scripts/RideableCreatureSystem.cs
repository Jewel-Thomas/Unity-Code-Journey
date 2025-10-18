// Unity Design Pattern Example: RideableCreatureSystem
// This script demonstrates the RideableCreatureSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the 'RideableCreatureSystem' design pattern, focusing on clear interfaces, loose coupling, and practical application. It allows a player character (rider) to mount and dismount various rideable creatures, transferring control between the two.

The example adheres to the following principles:

1.  **Interface Segregation Principle (ISP):** Distinct interfaces (`IRider`, `IRideable`) define separate contracts.
2.  **Dependency Inversion Principle (DIP):** High-level modules (like `PlayerRider`) depend on abstractions (`IRideable`) rather than concrete implementations.
3.  **Loose Coupling:** Components interact via interfaces, making the system flexible and extensible.
4.  **Extensibility:** Easily add new types of riders or rideable creatures by implementing the respective interfaces.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For finding nearby creatures with OverlapSphere

// =====================================================================================
// PART 1: Interfaces - Defining the 'RideableCreatureSystem' Pattern
// =====================================================================================

/// <summary>
/// IRider Interface:
/// Defines the common contract for any entity that can ride a creature.
/// This allows different types of riders (player, AI, etc.) to interact with rideable creatures
/// in a consistent manner without needing to know their concrete implementation.
/// </summary>
public interface IRider
{
    /// <summary>
    /// Returns the Transform of the rider. This Transform will be parented to the creature's mount point.
    /// </summary>
    Transform GetRiderTransform();

    /// <summary>
    /// Called when the rider successfully mounts a creature.
    /// The rider should disable its own movement/input and prepare to control the creature (or just sit passively).
    /// </summary>
    /// <param name="rideable">The creature that was mounted.</param>
    void StartRiding(IRideable rideable);

    /// <summary>
    /// Called when the rider successfully dismounts a creature.
    /// The rider should re-enable its own movement/input and return to its normal state.
    /// </summary>
    /// <param name="rideable">The creature that was dismounted.</param>
    void StopRiding(IRideable rideable);

    /// <summary>
    /// Provides read-only access to the currently ridden creature.
    /// Returns null if the rider is not currently riding.
    /// </summary>
    IRideable CurrentRideable { get; }

    /// <summary>
    /// Indicates whether this rider is currently riding a creature.
    /// </summary>
    bool IsRiding { get; }
}

/// <summary>
/// IRideable Interface:
/// Defines the common contract for any creature or object that can be ridden.
/// This allows different types of rideable objects (horses, dragons, vehicles) to be mounted
/// by various riders, again promoting consistency and loose coupling.
/// </summary>
public interface IRideable
{
    /// <summary>
    /// Returns the Transform where the rider should be parented to when mounted.
    /// This is typically an empty child GameObject positioned where the rider will sit.
    /// </summary>
    Transform GetMountPoint();

    /// <summary>
    /// Called when a rider attempts to mount this creature.
    /// The creature should accept the rider, adjust its state (e.g., become "ridden"),
    /// and prepare to receive input from the rider.
    /// </summary>
    /// <param name="rider">The rider attempting to mount.</param>
    void Mount(IRider rider);

    /// <summary>
    /// Called when a rider attempts to dismount this creature.
    /// The creature should release the rider, revert its state, and potentially stop taking rider input.
    /// </summary>
    /// <param name="rider">The rider attempting to dismount.</param>
    void Dismount(IRider rider);

    /// <summary>
    /// Provides read-only access to the currently riding entity.
    /// Returns null if the creature is not currently ridden.
    /// </summary>
    IRider CurrentRider { get; }

    /// <summary>
    /// Indicates whether this creature is currently being ridden.
    /// </summary>
    bool IsRidden { get; }

    /// <summary>
    /// Allows the rider to send movement input to the creature.
    /// This decouples the creature's movement logic from the specific rider's input source.
    /// </summary>
    /// <param name="input">A Vector2 representing horizontal (X) and vertical (Y) input axes.</param>
    void ReceiveRiderInput(Vector2 input);
}

// =====================================================================================
// PART 2: Concrete Implementations - Bringing the Pattern to Life
// =====================================================================================

/// <summary>
/// RideableCreature:
/// A concrete implementation of an IRideable creature in the game world.
/// This class handles its own movement when ridden, manages the rider's position,
/// and provides visual cues for its state.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Creatures need physics for movement
public class RideableCreature : MonoBehaviour, IRideable
{
    [Header("Creature Settings")]
    [Tooltip("The Transform where the rider will sit. Should be an empty child GameObject.")]
    [SerializeField] private Transform mountPoint;
    [Tooltip("Offset from the creature's position where the rider will land when dismounting.")]
    [SerializeField] private Vector3 dismountOffset = new Vector3(1f, 0, 1f);
    [Tooltip("The speed at which the creature moves when ridden.")]
    [SerializeField] private float creatureMoveSpeed = 7f;
    [Tooltip("The speed at which the creature rotates when ridden.")]
    [SerializeField] private float creatureRotateSpeed = 100f;

    [Header("Debug/Visuals")]
    [Tooltip("Optional: A visual representation for the creature (e.g., a Cube or 3D model).")]
    [SerializeField] private GameObject creatureModel;

    // --- IRideable Interface Properties ---
    private IRider _currentRider;
    public IRider CurrentRider => _currentRider;
    public bool IsRidden => _currentRider != null;
    // ------------------------------------

    private Rigidbody _rigidbody;
    private Vector2 _riderInput; // Stores input received from the rider

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("RideableCreature requires a Rigidbody component. Disabling script.", this);
            enabled = false;
            return;
        }
        _rigidbody.freezeRotation = true; // Prevent unwanted rotation from physics engine
        _rigidbody.isKinematic = false; // Ensure it's not kinematic by default
    }

    void Start()
    {
        if (mountPoint == null)
        {
            Debug.LogError($"Mount Point is not assigned for {gameObject.name}. Disabling script.", this);
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        // Only move the creature if it's currently ridden and receiving input.
        if (IsRidden)
        {
            HandleCreatureMovement(_riderInput);
        }
        // Optional: If not ridden, could implement creature AI movement here.
        // For this example, it stands still when unridden.

        _riderInput = Vector2.zero; // Reset input after applying it to prevent continuous movement
    }

    // --- IRideable Implementation ---

    public Transform GetMountPoint()
    {
        return mountPoint;
    }

    public void Mount(IRider rider)
    {
        if (IsRidden)
        {
            Debug.LogWarning($"{gameObject.name} is already ridden by {_currentRider.GetRiderTransform().name}. Cannot mount again.", this);
            rider.StopRiding(this); // Inform the attempting rider to abort
            return;
        }

        _currentRider = rider;
        Transform riderTransform = rider.GetRiderTransform();

        // Parent the rider to the mount point to make it move with the creature.
        riderTransform.SetParent(mountPoint);
        // Position the rider precisely at the mount point.
        riderTransform.localPosition = Vector3.zero;
        riderTransform.localRotation = Quaternion.identity;

        Debug.Log($"{riderTransform.name} successfully mounted {gameObject.name}.", this);

        // Optional: Trigger creature animation (e.g., "start riding", "ridden idle")
        // Example: if (creatureAnimator != null) creatureAnimator.SetBool("IsRidden", true);
        if (creatureModel != null)
        {
            Debug.Log($"Creature model {creatureModel.name} now in 'ridden' state (simulated).");
        }
    }

    public void Dismount(IRider rider)
    {
        if (_currentRider != rider)
        {
            Debug.LogWarning($"{rider.GetRiderTransform().name} tried to dismount {gameObject.name} but is not the current rider.", this);
            return;
        }

        Transform riderTransform = rider.GetRiderTransform();

        // Unparent the rider from the mount point.
        riderTransform.SetParent(null);
        // Position the rider slightly away from the creature at a safe dismount point.
        // TransformDirection converts local dismountOffset to world space relative to creature's current rotation.
        riderTransform.position = transform.position + transform.TransformDirection(dismountOffset);
        riderTransform.rotation = transform.rotation; // Keep rider facing same direction as creature

        _currentRider = null; // Clear the rider reference.

        Debug.Log($"{riderTransform.name} dismounted from {gameObject.name}.", this);

        // Optional: Trigger creature animation (e.g., "dismounted", "idle")
        // Example: if (creatureAnimator != null) creatureAnimator.SetBool("IsRidden", false);
        if (creatureModel != null)
        {
            Debug.Log($"Creature model {creatureModel.name} now in 'idle' state (simulated).");
        }
    }

    public void ReceiveRiderInput(Vector2 input)
    {
        // Store the input. It will be processed in FixedUpdate to ensure physics-based movement is smooth.
        _riderInput = input;
    }

    // --- Creature specific movement logic ---

    /// <summary>
    /// Handles the creature's movement based on rider input, applied in FixedUpdate.
    /// </summary>
    /// <param name="input">The Vector2 input from the rider (X for turn, Y for forward/backward).</param>
    private void HandleCreatureMovement(Vector2 input)
    {
        // Forward/Backward movement
        Vector3 moveDirection = transform.forward * input.y;
        _rigidbody.MovePosition(_rigidbody.position + moveDirection * creatureMoveSpeed * Time.fixedDeltaTime);

        // Rotation
        float turn = input.x * creatureRotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);

        // Optional: Trigger creature movement animations based on input
        // Example: if (creatureAnimator != null) { creatureAnimator.SetFloat("MoveSpeed", input.y); creatureAnimator.SetFloat("TurnAmount", input.x); }
    }

    // --- Debugging and Editor Visuals ---

    void OnDrawGizmos()
    {
        // Visualize the mount point
        if (mountPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(mountPoint.position, 0.1f); // Sphere at mount point
            Gizmos.DrawRay(mountPoint.position, mountPoint.forward * 0.5f); // Arrow indicating forward
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(mountPoint.position, 0.2f); // Wire sphere around mount point
        }
        // Visualize the dismount point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(dismountOffset), 0.2f);
    }
}


/// <summary>
/// PlayerRider:
/// A concrete implementation of an IRider, representing the player character.
/// It handles player input, detects nearby rideable creatures, and initiates
/// the mounting/dismounting process.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player needs physics for movement
public class PlayerRider : MonoBehaviour, IRider
{
    [Header("Player Settings")]
    [Tooltip("The speed at which the player moves when not riding.")]
    [SerializeField] private float playerMoveSpeed = 5f;
    [Tooltip("The speed at which the player rotates when not riding.")]
    [SerializeField] private float playerRotateSpeed = 150f;
    [Tooltip("The maximum distance from which the player can interact (mount/dismount) with a creature.")]
    [SerializeField] private float interactionDistance = 2.0f;
    [Tooltip("The key used to interact (mount/dismount).")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Debug/Visuals")]
    [Tooltip("Optional: A visual representation for the player (e.g., a Capsule or 3D model).")]
    [SerializeField] private GameObject playerModel;

    // --- IRider Interface Properties ---
    private IRideable _currentRideable;
    public IRideable CurrentRideable => _currentRideable;
    public bool IsRiding => _currentRideable != null;
    // ------------------------------------

    private Rigidbody _rigidbody;
    private Collider _playerCollider; // To toggle when player is parented to a creature

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("PlayerRider requires a Rigidbody component. Disabling script.", this);
            enabled = false;
            return;
        }
        _rigidbody.freezeRotation = true; // Prevent player from toppling over
        _rigidbody.isKinematic = false; // Ensure it's not kinematic by default

        _playerCollider = GetComponent<Collider>(); // Get the collider, assumed to be present
        if (_playerCollider == null)
        {
            Debug.LogWarning("PlayerRider is missing a Collider component, some functionality may not work as expected.", this);
        }
    }

    void Update()
    {
        // Handle interaction input. This is done in Update for immediate response.
        if (Input.GetKeyDown(interactKey))
        {
            if (IsRiding)
            {
                // If currently riding, attempt to dismount from the current creature.
                StopRiding(_currentRideable);
            }
            else
            {
                // If not riding, look for a nearby creature to mount.
                FindAndMountCreature();
            }
        }
    }

    void FixedUpdate()
    {
        // Physics-based movement is handled in FixedUpdate.
        if (!IsRiding)
        {
            HandlePlayerMovement();
        }
        else
        {
            // If riding, send player input to the creature instead of moving the player directly.
            SendInputToRideableCreature();
        }
    }

    // --- IRider Implementation ---

    public Transform GetRiderTransform()
    {
        return transform; // The player's own transform is what needs to be positioned
    }

    public void StartRiding(IRideable rideable)
    {
        if (IsRiding)
        {
            Debug.LogWarning($"{gameObject.name} is already riding {_currentRideable.GetMountPoint().parent.name}. Cannot mount another.", this);
            return;
        }

        _currentRideable = rideable;

        // Inform the creature that this rider is mounting.
        _currentRideable.Mount(this);

        // Disable player's own movement and physics when riding.
        _rigidbody.isKinematic = true; // Make player kinematic so it doesn't fall through the creature
        if (_playerCollider != null) _playerCollider.enabled = false; // Disable collider to prevent conflicts

        // Hide or change player model/animation to a 'sitting' state.
        if (playerModel != null)
        {
            playerModel.SetActive(false); // Simple hide, can be replaced with animation logic
            Debug.Log($"Player model {playerModel.name} hidden during riding (simulated).");
        }

        Debug.Log($"{gameObject.name} started riding {_currentRideable.GetMountPoint().parent.name}.", this);

        // Optional: Change camera follow target to the creature.
        // Example: Camera.main.GetComponent<CameraFollow>()?.SetTarget(_currentRideable.GetMountPoint().parent);
    }

    public void StopRiding(IRideable rideable)
    {
        if (!IsRiding || _currentRideable != rideable)
        {
            Debug.LogWarning($"{gameObject.name} tried to stop riding {rideable.GetMountPoint().parent.name} but is not riding it.", this);
            return;
        }

        // Inform the creature that the rider is dismounting.
        _currentRideable.Dismount(this);

        // Re-enable player's own movement and physics.
        _rigidbody.isKinematic = false;
        if (_playerCollider != null) _playerCollider.enabled = true;

        // Show or restore player model/animation.
        if (playerModel != null)
        {
            playerModel.SetActive(true);
            Debug.Log($"Player model {playerModel.name} shown after dismounting (simulated).");
        }

        _currentRideable = null; // Clear the rideable reference.

        Debug.Log($"{gameObject.name} stopped riding {rideable.GetMountPoint().parent.name}.", this);

        // Optional: Change camera follow target back to the player.
        // Example: Camera.main.GetComponent<CameraFollow>()?.SetTarget(transform);
    }

    // --- Player specific logic (when not riding) ---

    /// <summary>
    /// Handles the player's direct movement when not riding a creature.
    /// </summary>
    private void HandlePlayerMovement()
    {
        float moveInput = Input.GetAxis("Vertical"); // W/S or Up/Down arrows
        float turnInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows

        // Apply forward/backward movement
        Vector3 moveDirection = transform.forward * moveInput;
        _rigidbody.MovePosition(_rigidbody.position + moveDirection * playerMoveSpeed * Time.fixedDeltaTime);

        // Apply rotation
        float turn = turnInput * playerRotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);

        // Optional: Player movement animations.
        // Example: if (playerAnimator != null) { playerAnimator.SetFloat("MoveSpeed", moveInput); playerAnimator.SetFloat("TurnAmount", turnInput); }
    }

    /// <summary>
    /// Sends player input (WASD) to the creature currently being ridden.
    /// </summary>
    private void SendInputToRideableCreature()
    {
        if (_currentRideable != null)
        {
            float moveInput = Input.GetAxis("Vertical");
            float turnInput = Input.GetAxis("Horizontal");
            _currentRideable.ReceiveRiderInput(new Vector2(turnInput, moveInput));
        }
    }

    /// <summary>
    /// Finds the closest IRideable creature within interactionDistance and attempts to mount it.
    /// </summary>
    private void FindAndMountCreature()
    {
        // Use an OverlapSphere to detect nearby colliders.
        // In a real game, you might use a dedicated trigger collider or a raycast for more precise interaction.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionDistance);
        IRideable closestRideable = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            // Try to get IRideable from the collider's GameObject or its parent.
            // This accounts for creatures having child colliders or visual models.
            IRideable rideable = hitCollider.GetComponentInParent<IRideable>();
            
            // Ensure it's a valid rideable creature and not already ridden.
            if (rideable != null && !rideable.IsRidden)
            {
                float dist = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestRideable = rideable;
                }
            }
        }

        if (closestRideable != null)
        {
            // If a rideable creature is found, initiate the riding process.
            StartRiding(closestRideable);
        }
        else
        {
            Debug.Log("No unridden rideable creature found within interaction distance.", this);
        }
    }

    // --- Debugging and Editor Visuals ---

    void OnDrawGizmosSelected()
    {
        // Visualize the interaction distance in the editor.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}


// =====================================================================================
// PART 3: Utility - Camera Follow for better testing
// =====================================================================================

/// <summary>
/// CameraFollow:
/// A simple script to make the camera follow a target, adjusting its position smoothly.
/// Essential for a playable example where the camera needs to switch between following
/// the player or the creature.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -8);
    [SerializeField] private float smoothSpeed = 0.125f;

    /// <summary>
    /// Sets a new target for the camera to follow.
    /// </summary>
    /// <param name="newTarget">The Transform of the new object to follow.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target == null) return; // Do nothing if there's no target

        // Calculate the desired position based on target's position and rotation
        Vector3 desiredPosition = target.position + target.rotation * offset;
        // Smoothly interpolate between current position and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Make the camera look at the target (slightly above its center)
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}


/*
=====================================================================================
EXAMPLE USAGE IN UNITY PROJECT
=====================================================================================

To use this 'RideableCreatureSystem' in your Unity project, follow these steps:

1.  **Create a New Unity Project** (or open an existing one).

2.  **Create a New C# Script:**
    *   Create a single C# script file, name it `RideableCreatureSystem.cs`.
    *   Copy and paste the ENTIRE content of this Gist into that single file.
    *   (Optional but Recommended for larger projects): For better organization, you could split the interfaces (`IRider`, `IRideable`) into separate `.cs` files and place `RideableCreature.cs`, `PlayerRider.cs`, and `CameraFollow.cs` each into their own `.cs` files. This example combines them for simplicity as a single, ready-to-drop-in demonstration.

3.  **Setup the Scene:**

    *   **Ground:**
        *   Right-click in the Hierarchy -> 3D Object -> Plane.
        *   Scale it up (e.g., X:10, Z:10) for a larger play area.

    *   **Creature (e.g., a "Horse" or "Dragon"):**
        *   Right-click in Hierarchy -> Create Empty. Name it `Creature_Horse`.
        *   Add Component: `RideableCreature` script to `Creature_Horse`.
        *   Add a **visual** for the creature: Right-click `Creature_Horse` -> 3D Object -> Cube. Position and scale it (e.g., Y:1.5, Z:2) to resemble an animal. Drag this Cube GameObject into the `Creature Model` field of the `RideableCreature` component.
        *   Ensure `Creature_Horse` has a `Rigidbody` component (the `[RequireComponent]` attribute should add it automatically, or add it manually).
        *   Create another Empty GameObject as a child of `Creature_Horse`. Name it `MountPoint`. Position this `MountPoint` where you want the rider to sit (e.g., Y:1.5, Z:0.5 relative to `Creature_Horse`'s center). Drag this `MountPoint` into the `Mount Point` field of the `RideableCreature` component.
        *   Adjust `Dismount Offset` in the `RideableCreature` component as desired (e.g., `(1,0,1)` will dismount the rider to the right and slightly behind the creature).

    *   **Player Character:**
        *   Right-click in Hierarchy -> Create Empty. Name it `Player`.
        *   Add Component: `PlayerRider` script to `Player`.
        *   Add a **visual** for the player: Right-click `Player` -> 3D Object -> Capsule. Position it (e.g., Y:1) so it stands on the ground. Drag this Capsule GameObject into the `Player Model` field of the `PlayerRider` component.
        *   Ensure `Player` has a `Rigidbody` component (the `[RequireComponent]` attribute should add it automatically, or add it manually).
        *   Ensure `Player` has a `Collider` component (e.g., a `Capsule Collider`, which comes with the Capsule visual by default). This is needed for collision detection during `FindAndMountCreature()`.

    *   **Camera:**
        *   Select your `Main Camera` in the Hierarchy.
        *   Add Component: `CameraFollow` script to the `Main Camera`.
        *   Drag the `Player` GameObject from the Hierarchy into the `Target` field of the `CameraFollow` component.
        *   Adjust the `Offset` (e.g., `(0, 5, -8)`) and `Smooth Speed` to get a good third-person view.

4.  **Run the Game:**
    *   Press the Play button in the Unity Editor.
    *   You can move your Player character using the **WASD** keys (or Arrow keys).
    *   Walk the Player close to the `Creature_Horse` (within the `Interaction Distance`, which is visualized by a cyan wire sphere when the `Player` GameObject is selected in the Editor).
    *   Press the **'E' key** (or your chosen `Interact Key`) to mount the creature.
    *   Once mounted, the Player's visual model will disappear (or change state as per your `playerModel` setup), and the **WASD** input will now control the `Creature_Horse`.
    *   Press the **'E' key** again to dismount. The Player will reappear at the `Dismount Offset` relative to the creature and regain control.

This setup provides a complete, functional demonstration of the 'RideableCreatureSystem' pattern, ready for immediate use and further expansion in your Unity projects.
*/
```