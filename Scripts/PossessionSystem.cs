// Unity Design Pattern Example: PossessionSystem
// This script demonstrates the PossessionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Possession System' design pattern in game development, particularly in Unity, is used to decouple the "controller" (who is issuing commands) from the "pawn" (the object being controlled). This allows a single controller (e.g., the player) to take control of various different types of game objects, each with their own unique movement and interaction logic, without the controller needing to know the specifics of how to drive each one.

This example demonstrates a complete Possession System where a player's "spirit" (the controller) can float around and then possess different "characters" (the pawns). When a character is possessed, the player's input drives that character's specific movement logic. When unpossessed, the player regains control of the spirit, and the character reverts to an idle or AI state.

---

### Key Components of the Possession System:

1.  **`IPossessable` (Interface):**
    *   Defines the contract for any game object that can be possessed.
    *   It specifies methods like `Possess(Possessor newPossessor)` and `Unpossess()`, and a property `IsPossessed`.
    *   This is crucial for decoupling: the `Possessor` only interacts with this interface, not concrete character types.

2.  **`Possessor` (Abstract Base Class):**
    *   An abstract class that defines the core logic for any entity capable of possessing an `IPossessable`.
    *   It maintains a reference to the `CurrentPossessable`.
    *   Handles the state transitions (unpossessing current, then possessing new).

3.  **`SpiritController` (Concrete Possessor):**
    *   A concrete implementation of `Possessor` that represents the player's controllable entity.
    *   It handles player input to move itself when unpossessed, find nearby `IPossessable` targets, and initiate/release possession.
    *   When possessing, its own movement is disabled, and control shifts to the `IPossessable`.

4.  **`CharacterPossessable` (Concrete Possessable):**
    *   A concrete implementation of `IPossessable`.
    *   Contains the specific movement logic for a character.
    *   When `Possess()` is called, it enables player input handling (via `FixedUpdate`).
    *   When `Unpossess()` is called, it disables player input and could potentially re-enable AI or idle behavior.

5.  **`CameraFollow` (Helper Script):**
    *   A utility script to smoothly follow a target object.
    *   The `SpiritController` updates the `CameraFollow`'s target to either itself or the currently possessed `IPossessable`, providing dynamic camera control.

---

### Project Structure and Code:

Create these C# scripts in your Unity project and copy the respective code into them.

#### 1. `IPossessable.cs`

```csharp
using UnityEngine;
using System; // For Action delegate

/// <summary>
/// Defines the contract for any object that can be possessed by a <see cref="Possessor"/>.
/// This interface ensures decoupling, allowing the Possessor to interact with any possessable
/// without knowing its specific implementation details.
/// </summary>
public interface IPossessable
{
    /// <summary>
    /// Gets the GameObject associated with this possessable object.
    /// Useful for the Possessor to interact with its physical presence (e.g., transform, collisions).
    /// </summary>
    /// <returns>The GameObject this component is attached to.</returns>
    GameObject GetGameObject();

    /// <summary>
    /// Called when this object is possessed by a new Possessor.
    /// Implementations should enable player input, disable AI, or change behavior accordingly.
    /// </summary>
    /// <param name="newPossessor">The Possessor instance taking control.</param>
    void Possess(Possessor newPossessor);

    /// <summary>
    /// Called when this object is unpossessed.
    /// Implementations should disable player input, re-enable AI, or revert to idle behavior.
    /// </summary>
    void Unpossess();

    /// <summary>
    /// Gets a value indicating whether this object is currently possessed.
    /// </summary>
    bool IsPossessed { get; }

    /// <summary>
    /// An event that is invoked when the possession state of this object changes.
    /// The boolean parameter indicates the new state (true if possessed, false if unpossessed).
    /// </summary>
    event Action<bool> OnPossessionStateChanged;
}
```

#### 2. `Possessor.cs`

```csharp
using UnityEngine;

/// <summary>
/// Abstract base class for any entity that can possess an <see cref="IPossessable"/> object.
/// This acts as the 'Controller' in the PossessionSystem pattern.
/// It maintains a reference to the currently possessed object and provides a mechanism
/// for concrete Possessor implementations (like a player or AI) to manage possession.
/// </summary>
public abstract class Possessor : MonoBehaviour
{
    /// <summary>
    /// Gets the currently possessed object. This property is protected so derived classes
    /// can set it, but external classes can only read it.
    /// </summary>
    public IPossessable CurrentPossessable { get; protected set; }

    // --- Design Pattern Explanation ---
    // The Possessor acts as the 'Controller' that delegates its actions to the 'Pawn' (IPossessable).
    // It doesn't know the concrete type of the Pawn, only that it implements IPossessable.
    // This provides high decoupling: a single Possessor can control a Character, a Vehicle,
    // a Turret, or any other object that implements IPossessable, without modification.
    // The IPossessable itself handles how it is controlled.

    /// <summary>
    /// Attempts to possess the given target object.
    /// This method manages the state transition: unpossessing the current object (if any)
    /// and then possessing the new target.
    /// </summary>
    /// <param name="target">The IPossessable object to possess.</param>
    protected virtual void AttemptPossess(IPossessable target)
    {
        if (target == null || target.IsPossessed)
        {
            Debug.LogWarning($"Attempted to possess null or already possessed target: {target?.GetGameObject().name ?? "NULL"}");
            return;
        }

        // 1. Unpossess current object if one is already possessed
        if (CurrentPossessable != null)
        {
            CurrentPossessable.Unpossess();
            Debug.Log($"Unpossessed {CurrentPossessable.GetGameObject().name}");
        }

        // 2. Possess the new target
        CurrentPossessable = target;
        CurrentPossessable.Possess(this); // Pass this Possessor instance to the IPossessable
        Debug.Log($"Possessed {CurrentPossessable.GetGameObject().name}");
    }

    /// <summary>
    /// Unpossesses the currently controlled object.
    /// This method reverts the system to an unpossessed state.
    /// </summary>
    protected virtual void AttemptUnpossess()
    {
        if (CurrentPossessable == null)
        {
            Debug.LogWarning("Attempted to unpossess, but no object is currently possessed.");
            return;
        }

        // 1. Call Unpossess on the current object
        CurrentPossessable.Unpossess();
        Debug.Log($"Unpossessed {CurrentPossessable.GetGameObject().name}");

        // 2. Clear the reference
        CurrentPossessable = null;
    }

    /// <summary>
    /// Helper method to find the nearest IPossessable object within a given range.
    /// </summary>
    /// <param name="origin">The center point for the search.</param>
    /// <param name="range">The maximum distance to search.</param>
    /// <param name="possessableLayer">The LayerMask to filter for possessable objects.</param>
    /// <returns>The nearest IPossessable, or null if none found.</returns>
    protected IPossessable FindNearestPossessable(Vector3 origin, float range, LayerMask possessableLayer)
    {
        Collider[] hitColliders = Physics.OverlapSphere(origin, range, possessableLayer);
        IPossessable nearestPossessable = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out IPossessable possessable))
            {
                // Ensure we don't try to possess ourselves if we also implement IPossessable,
                // or an object that is already possessed by someone else.
                if (possessable.GetGameObject() == this.gameObject || possessable.IsPossessed)
                {
                    continue;
                }

                float distance = Vector3.Distance(origin, possessable.GetGameObject().transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPossessable = possessable;
                }
            }
        }
        return nearestPossessable;
    }

    /// <summary>
    /// Draws the possession range in the editor for debugging.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // For derived classes to implement if they have a range to visualize.
    }
}
```

#### 3. `SpiritController.cs`

```csharp
using UnityEngine;

/// <summary>
/// Represents the player's 'spirit' or unembodied controller.
/// This is a concrete implementation of a <see cref="Possessor"/>.
/// The SpiritController can move freely in the world and, upon interaction,
/// can possess nearby <see cref="IPossessable"/> objects.
/// When possessed, the SpiritController's own movement is disabled, and it
/// essentially 'rides' along with the possessed object (for logical coherence,
/// though its physical presence isn't driving the possessed object).
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensure Rigidbody for physics movement
public class SpiritController : Possessor
{
    [Header("Spirit Movement")]
    [SerializeField, Tooltip("Speed at which the spirit moves when unpossessed.")]
    private float spiritMoveSpeed = 7f;
    [SerializeField, Tooltip("Force applied for spirit movement.")]
    private float moveForce = 100f;

    [Header("Possession Settings")]
    [SerializeField, Tooltip("The maximum distance from which the spirit can possess an object.")]
    private float possessRange = 5f;
    [SerializeField, Tooltip("The layer(s) that possessable objects are on.")]
    private LayerMask possessableLayer;
    [SerializeField, Tooltip("Input button to initiate possession.")]
    private KeyCode possessKey = KeyCode.E;
    [SerializeField, Tooltip("Input button to unpossess the current object.")]
    private KeyCode unpossessKey = KeyCode.Q;

    [Header("Camera Settings")]
    [SerializeField, Tooltip("Reference to the camera controller.")]
    public CameraFollow cameraFollow; // Made public for CharacterPossessable to access
    [SerializeField, Tooltip("Offset for the camera when the spirit is unpossessed.")]
    private Vector3 unpossessedCameraOffset = new Vector3(0, 5, -10);
    [SerializeField, Tooltip("Offset for the camera when possessing an object.")]
    private Vector3 possessedCameraOffset = new Vector3(0, 3, -7);

    private Rigidbody rb;
    private bool canMoveSpirit = true;

    // --- Design Pattern Explanation ---
    // This class is the primary 'Client' of the PossessionSystem pattern.
    // It initiates the possession/unpossession process and manages its own state
    // (moving freely vs. controlling a pawn). It leverages the IPossessable interface
    // to interact with any target without knowing its specific movement or interaction logic.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("SpiritController requires a Rigidbody component.", this);
            enabled = false;
            return;
        }

        if (cameraFollow == null)
        {
            // Try to find it if not assigned in editor
            cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow == null)
            {
                Debug.LogWarning("CameraFollow script not assigned and not found in scene. Camera will not follow.", this);
            }
        }
    }

    private void Start()
    {
        // Set initial camera target to the spirit
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform, unpossessedCameraOffset);
        }
    }

    private void Update()
    {
        HandlePossessionInput();
    }

    private void FixedUpdate()
    {
        // Only move the spirit if it's not possessing anything and allowed to move
        if (CurrentPossessable == null && canMoveSpirit)
        {
            HandleSpiritMovement();
        }
        else if (CurrentPossessable != null)
        {
            // If possessing, make the spirit's physical body follow the possessed object.
            // This is useful if the camera is attached to the spirit, or for spatial reference.
            // Or just keep the spirit stationary and manage camera separately (as we do).
            // For this example, we'll make the spirit's Transform follow the possessed object's.
            transform.position = CurrentPossessable.GetGameObject().transform.position;
            transform.rotation = CurrentPossessable.GetGameObject().transform.rotation;
        }
    }

    /// <summary>
    /// Handles player input for possessing and unpossessing objects.
    /// </summary>
    private void HandlePossessionInput()
    {
        if (Input.GetKeyDown(possessKey))
        {
            if (CurrentPossessable == null) // Only try to possess if not already possessing
            {
                IPossessable target = FindNearestPossessable(transform.position, possessRange, possessableLayer);
                if (target != null)
                {
                    // Call the base class's possession logic
                    AttemptPossess(target);
                    // Disable spirit movement and update camera
                    canMoveSpirit = false;
                    rb.isKinematic = true; // Stop spirit from interacting physically (prevents it from being pushed)
                    if (cameraFollow != null)
                    {
                        cameraFollow.SetTarget(CurrentPossessable.GetGameObject().transform, possessedCameraOffset);
                    }
                }
                else
                {
                    Debug.Log("No possessable object found in range.");
                }
            }
        }
        else if (Input.GetKeyDown(unpossessKey))
        {
            if (CurrentPossessable != null) // Only unpossess if currently possessing
            {
                // Call the base class's unpossession logic
                AttemptUnpossess();
                // Re-enable spirit movement and update camera
                canMoveSpirit = true;
                rb.isKinematic = false; // Allow spirit to move physically again
                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(transform, unpossessedCameraOffset);
                }
            }
        }
    }

    /// <summary>
    /// Handles the SpiritController's own movement when unpossessed.
    /// </summary>
    private void HandleSpiritMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // Use camera's forward direction to align movement relative to camera view
            if (cameraFollow != null && cameraFollow.Target != null)
            {
                Transform cameraTransform = cameraFollow.transform;
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;

                forward.y = 0; // Keep movement horizontal on the XZ plane
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * vertical + right * horizontal).normalized;
            }

            // Apply force for movement
            rb.AddForce(moveDirection * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

            // Limit max speed
            Vector3 flatVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (flatVel.magnitude > spiritMoveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * spiritMoveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

            // Optional: Rotate spirit to face direction of movement
            if (moveDirection.magnitude > 0.1f && rb.velocity.magnitude > 0.1f) // Only rotate if moving significantly
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(rb.velocity.x, 0, rb.velocity.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        else
        {
            // Reduce velocity when no input to stop sliding
            rb.velocity = new Vector3(rb.velocity.x * 0.9f, rb.velocity.y, rb.velocity.z * 0.9f);
        }
    }

    private const float rotationSpeed = 10f; // Added for spirit rotation, matching character

    /// <summary>
    /// Draws the possession range in the editor for debugging.
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Call base for any common gizmos
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, possessRange);
        if (CurrentPossessable != null && CurrentPossessable.GetGameObject() != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, CurrentPossessable.GetGameObject().transform.position);
            Gizmos.DrawSphere(CurrentPossessable.GetGameObject().transform.position, 0.5f);
        }
    }
}
```

#### 4. `CharacterPossessable.cs`

```csharp
using UnityEngine;
using System; // For Action delegate

/// <summary>
/// A concrete implementation of <see cref="IPossessable"/> representing a character.
/// This component provides the logic for how a character behaves when possessed by
/// a <see cref="Possessor"/> (e.g., player input) and when unpossessed (e.g., idle animation, simple AI).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterPossessable : MonoBehaviour, IPossessable
{
    [Header("Character Settings")]
    [SerializeField, Tooltip("Movement speed of the character when possessed.")]
    private float moveSpeed = 5f;
    [SerializeField, Tooltip("Force applied for character movement.")]
    private float moveForce = 150f;
    [SerializeField, Tooltip("Rotation speed for the character.")]
    private float rotationSpeed = 10f;

    [Header("Visuals")]
    [SerializeField, Tooltip("Material to apply when possessed.")]
    private Material possessedMaterial;
    [SerializeField, Tooltip("Material to apply when unpossessed.")]
    private Material unpossessedMaterial;

    // --- IPossessable Implementation ---
    private Possessor currentPossessor;
    public bool IsPossessed { get; private set; }
    public event Action<bool> OnPossessionStateChanged;

    private Rigidbody rb;
    private Renderer characterRenderer; // To change material for visual feedback

    // --- Design Pattern Explanation ---
    // This class is the 'Pawn' in the PossessionSystem pattern.
    // It's entirely responsible for *how* it is controlled. The Possessor only calls
    // Possess() and Unpossess(), but doesn't dictate movement or specific actions.
    // This allows different IPossessable types (e.g., Character, Vehicle, Turret)
    // to have entirely different control schemes while being controlled by the same Possessor.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
        Debug.LogError("CharacterPossessable requires a Rigidbody component.", this);
            enabled = false;
            return;
        }
        rb.freezeRotation = true; // Prevent unwanted rotation from physics

        characterRenderer = GetComponentInChildren<Renderer>();
        if (characterRenderer == null)
        {
            Debug.LogWarning("No Renderer found on CharacterPossessable or its children for material changes.", this);
        }
    }

    private void Start()
    {
        SetPossessionVisuals(false); // Start unpossessed
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    /// <summary>
    /// Activates player control for this character and disables any AI.
    /// </summary>
    /// <param name="newPossessor">The Possessor taking control.</param>
    public void Possess(Possessor newPossessor)
    {
        if (IsPossessed) return; // Already possessed

        IsPossessed = true;
        currentPossessor = newPossessor;
        Debug.Log($"Character '{gameObject.name}' is now possessed by {newPossessor.gameObject.name}.");

        // Example: Disable AI script if present
        // AIController ai = GetComponent<AIController>();
        // if (ai != null) ai.enabled = false;

        // Ensure physics body is active for player control
        rb.isKinematic = false;

        SetPossessionVisuals(true);
        OnPossessionStateChanged?.Invoke(true);
    }

    /// <summary>
    /// Deactivates player control and re-enables any AI or idle behavior.
    /// </summary>
    public void Unpossess()
    {
        if (!IsPossessed) return; // Not possessed

        IsPossessed = false;
        currentPossessor = null;
        Debug.Log($"Character '{gameObject.name}' is now unpossessed.");

        // Example: Re-enable AI script
        // AIController ai = GetComponent<AIController>();
        // if (ai != null) ai.enabled = true;

        // Stop movement immediately on unpossess
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Optional: Make character kinematic if it should stand still and not react to physics,
        // or re-enable AI to take over.
        // rb.isKinematic = true;

        SetPossessionVisuals(false);
        OnPossessionStateChanged?.Invoke(false);
    }

    private void FixedUpdate()
    {
        if (IsPossessed)
        {
            HandlePlayerMovement();
        }
        // else: Can implement basic AI or idle behavior here for unpossessed state
    }

    /// <summary>
    /// Handles player input for character movement when possessed.
    /// </summary>
    private void HandlePlayerMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // Use camera's forward direction for movement, assuming a SpiritController with CameraFollow
            if (currentPossessor is SpiritController spirit && spirit.cameraFollow != null && spirit.cameraFollow.Target != null)
            {
                Transform cameraTransform = spirit.cameraFollow.transform;
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;

                forward.y = 0; // Keep movement horizontal on the XZ plane
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * vertical + right * horizontal).normalized;
            }

            // Apply force for movement
            rb.AddForce(moveDirection * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

            // Limit max speed
            Vector3 flatVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

            // Rotate character to face movement direction
            if (moveDirection.magnitude > 0.1f && rb.velocity.magnitude > 0.1f) // Only rotate if moving significantly
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(rb.velocity.x, 0, rb.velocity.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        else
        {
            // Reduce velocity when no input to stop sliding
            rb.velocity = new Vector3(rb.velocity.x * 0.9f, rb.velocity.y, rb.velocity.z * 0.9f);
        }
    }

    /// <summary>
    /// Applies the appropriate material based on the possession state for visual feedback.
    /// </summary>
    /// <param name="possessed">True if possessed, false if unpossessed.</param>
    private void SetPossessionVisuals(bool possessed)
    {
        if (characterRenderer != null)
        {
            characterRenderer.material = possessed ? possessedMaterial : unpossessedMaterial;
        }
    }
}
```

#### 5. `CameraFollow.cs`

```csharp
using UnityEngine;

/// <summary>
/// A simple camera script to follow a target with an offset.
/// This decouples camera control from the Possessor and Possessable objects,
/// making it a utility that simply receives a target to follow.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField, Tooltip("The speed at which the camera moves to catch up to the target.")]
    private float smoothSpeed = 0.125f;

    [Header("Look At Settings")]
    [SerializeField, Tooltip("How quickly the camera rotates to look at the target.")]
    private float lookAtSpeed = 5f;
    [SerializeField, Tooltip("Additional offset for the camera's look-at point relative to the target.")]
    private Vector3 lookAtOffset = Vector3.up * 1.5f;

    // The target Transform the camera should follow.
    public Transform Target { get; private set; }
    // The desired position offset from the target.
    public Vector3 Offset { get; private set; }

    /// <summary>
    /// Sets a new target for the camera to follow with a specific offset.
    /// </summary>
    /// <param name="newTarget">The new Transform to follow.</param>
    /// <param name="newOffset">The offset from the new target.</param>
    public void SetTarget(Transform newTarget, Vector3 newOffset)
    {
        Target = newTarget;
        Offset = newOffset;
        // Immediately snap to the new target's position when setting, to avoid jarring transitions
        if (Target != null)
        {
            transform.position = Target.position + Target.rotation * Offset;
            // Also snap rotation to look at target
            Vector3 lookPosition = Target.position + lookAtOffset;
            transform.rotation = Quaternion.LookRotation((lookPosition - transform.position).normalized);
        }
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            // If no target, perhaps maintain current position or revert to default
            return;
        }

        // Calculate desired position with offset relative to target's rotation
        Vector3 desiredPosition = Target.position + Target.rotation * Offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Calculate desired look-at point
        Vector3 lookPosition = Target.position + lookAtOffset;

        // Smoothly rotate to look at the target
        Quaternion targetRotation = Quaternion.LookRotation((lookPosition - transform.position).normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookAtSpeed);
    }
}
```

---

### How to Set Up in Unity:

1.  **Create a New Unity Project.**
2.  **Create the C# scripts** listed above and copy their contents.
3.  **Create a 3D Scene:**
    *   **Ground Plane:** Create a 3D Plane (GameObject -> 3D Object -> Plane).
    *   **Camera Setup:**
        *   Create an empty GameObject named **"MainCameraContainer"**.
        *   Make the existing **"Main Camera"** a child of "MainCameraContainer".
        *   Add the `CameraFollow.cs` script to the "MainCameraContainer".
        *   Adjust "Main Camera" position/rotation relative to its container to point downwards/forward (e.g., Local Position: `(0, 0, 0)`, Local Rotation: `(30, 0, 0)` or similar).
    *   **Spirit Controller (Player):**
        *   Create an empty GameObject named **"PlayerSpirit"**.
        *   Add the `SpiritController.cs` script to it.
        *   Add a `Rigidbody` component to "PlayerSpirit" (Constraints -> Freeze Rotation X, Y, Z).
        *   Add a `Sphere Collider` to "PlayerSpirit" (Is Trigger: `true`, Radius: `0.5` or `1`). This is for visual debugging/interaction.
        *   In the `SpiritController` component:
            *   Drag the **"MainCameraContainer"** (which has the `CameraFollow` script) into the `Camera Follow` slot.
            *   Set `Possessable Layer` to a new Layer (e.g., **"Possessable"**).
            *   Adjust `Spirit Move Speed`, `Possess Range`, etc., as desired.
            *   Set `Unpossessed Camera Offset` (e.g., `(0, 10, -10)`).
            *   Set `Possessed Camera Offset` (e.g., `(0, 3, -7)`).
    *   **Possessable Characters (Pawns):**
        *   Create a 3D Cube (GameObject -> 3D Object -> Cube) named **"Character_01"**.
        *   Add a `Rigidbody` component to it (Constraints -> Freeze Rotation X, Y, Z).
        *   Add the `CharacterPossessable.cs` script to it.
        *   **Important:** Set **"Character_01"'s Layer** to **"Possessable"** (the one you will define and link in `SpiritController`).
        *   Create two new Materials: **"PossessedMaterial"** (e.g., bright green, emissive) and **"UnpossessedMaterial"** (e.g., dull grey).
        *   Drag these materials into the respective slots in the `CharacterPossessable` component.
        *   Duplicate "Character_01" a few times and place them around the scene (e.g., "Character_02", "Character_03").
    *   **Layers Setup:**
        *   Go to **Edit -> Project Settings -> Tags and Layers**.
        *   Add a new Layer, name it **"Possessable"**.
        *   Assign this layer to all your `Character_XX` objects.
        *   In the `SpiritController` component, make sure the `Possessable Layer` dropdown correctly selects your **"Possessable"** layer.

4.  **Run the scene!**
    *   Use **WASD** to move the `PlayerSpirit`.
    *   Press **'E'** to possess the nearest character.
    *   Use **WASD** to move the possessed character.
    *   Press **'Q'** to unpossess, returning control to the `PlayerSpirit`.
    *   Notice how the camera follows the currently controlled entity, and the character's material changes.

---

### Extending the System:

*   **New `IPossessable` types:** Create `VehiclePossessable`, `TurretPossessable`, or `EnvironmentalObjectPossessable` (e.g., a boulder you can roll). Each will have its own unique movement/interaction logic defined within its `FixedUpdate`/`Update` when `IsPossessed` is true.
*   **AI Integration:** Implement AI scripts that enable themselves when `IsPossessed` is false and disable when true, allowing NPCs to have autonomous behavior when not controlled by the player.
*   **Different `Possessor` types:** Create an `AIPossessor` that could take control of an enemy unit for strategic purposes, or a `NetworkPossessor` for multiplayer control.
*   **Visual/Audio Feedback:** Add particle effects, sounds, or UI elements to visually and audibly communicate possession/unpossession events.
*   **Enhanced Camera System:** Improve `CameraFollow` with features like collision avoidance, dynamic FOV, or different camera modes based on the possessed object type.

This example provides a robust foundation for building complex possession mechanics in your Unity games, adhering to solid design principles for maintainability and extensibility.