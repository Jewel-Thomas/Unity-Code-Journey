// Unity Design Pattern Example: PressurePlateSystem
// This script demonstrates the PressurePlateSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PressurePlateSystem' design pattern in Unity decouples the mechanism that detects an event (the pressure plate) from the actions that are performed in response (e.g., opening a door, activating a light). This is achieved through an interface, allowing any GameObject that implements this interface to be controlled by the pressure plate without the plate needing to know the specific type of object it's interacting with.

This example provides:
1.  **`IPressureActuatable` Interface**: Defines the contract for objects that can be activated/deactivated.
2.  **`PressurePlateController` Script**: The core logic for the pressure plate, detecting objects, managing its state, and notifying its targets.
3.  **`DoorController` Script**: An example implementation of `IPressureActuatable` to demonstrate how a door would respond.
4.  **Detailed Comments**: Explaining the pattern, its implementation, and Unity best practices.

---

### **1. `IPressureActuatable.cs` (Interface Definition)**

This interface defines the contract that any object must adhere to if it wants to be controlled by a `PressurePlateController`.

```csharp
using UnityEngine;

// PressurePlateSystem Design Pattern: IPressureActuatable Interface
// This interface defines the contract for any object that can be "actuated"
// (activated or deactivated) by a pressure plate.
// By using an interface, the PressurePlateController doesn't need to know
// the specific type of object it's interacting with (e.g., a door, a light, a platform).
// It simply calls Activate() or Deactivate() on any component that implements this interface.
public interface IPressureActuatable
{
    /// <summary>
    /// Called when the pressure plate is pressed or activated.
    /// </summary>
    void Activate();

    /// <summary>
    /// Called when the pressure plate is released or deactivated.
    /// </summary>
    void Deactivate();
}

```

---

### **2. `PressurePlateController.cs` (The Pressure Plate Logic)**

This script manages the pressure plate's state, detects objects, and notifies all linked `IPressureActuatable` targets.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Used for .Any() and .Count() with filters

// PressurePlateSystem Design Pattern: PressurePlateController
// This script acts as the "trigger" or "initiator" in the pattern.
// It detects when objects enter/exit its trigger zone and, based on
// configurable rules, transitions between a "pressed" and "released" state.
// When its state changes, it notifies all registered IPressureActuatable targets
// to perform their respective actions.
[RequireComponent(typeof(Collider))] // Ensure there's a collider for trigger detection
public class PressurePlateController : MonoBehaviour
{
    [Header("Plate Configuration")]
    [Tooltip("List of GameObjects that contain IPressureActuatable components. " +
             "These are the objects that will respond to the plate.")]
    [SerializeField] private List<GameObject> actuatableTargets = new List<GameObject>();

    [Tooltip("Optional: A visual representation of the plate that moves when pressed.")]
    [SerializeField] private Transform plateVisual;

    [Tooltip("How much the plate visual moves down when pressed.")]
    [SerializeField] private Vector3 pressedOffset = new Vector3(0, -0.1f, 0);

    [Tooltip("Speed at which the plate visual animates up/down.")]
    [SerializeField] private float plateAnimationSpeed = 5f;

    [Header("Activation Rules")]
    [Tooltip("Optional: If specified, only objects with these tags can activate the plate. Leave empty for any object.")]
    [SerializeField] private string[] activatorTags;

    [Tooltip("The number of valid activators required on the plate to trigger activation.")]
    [SerializeField] private int requiredActivators = 1;

    [Tooltip("Delay in seconds before activating targets after the condition is met.")]
    [SerializeField] private float activationDelay = 0.5f;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip pressSound;
    [SerializeField] private AudioClip releaseSound;

    private AudioSource _audioSource;
    private List<IPressureActuatable> _parsedTargets = new List<IPressureActuatable>();
    private HashSet<Collider> _currentlyOnPlate = new HashSet<Collider>(); // Tracks valid colliders on the plate
    private bool _isPressed = false;
    private Coroutine _activationCoroutine;
    private Vector3 _plateInitialLocalPosition;

    // Unity Lifecycle Method: Awake
    // Initializes the plate, parses targets, and sets up audio.
    void Awake()
    {
        // Ensure the collider is set to be a trigger.
        Collider plateCollider = GetComponent<Collider>();
        if (!plateCollider.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} is not a trigger. Setting it to trigger.", this);
            plateCollider.isTrigger = true;
        }

        // Get or add an AudioSource component for playing sounds.
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        // Parse GameObjects into actual IPressureActuatable references.
        // This allows us to work with interfaces directly and avoid repeated GetComponent calls.
        foreach (GameObject targetGO in actuatableTargets)
        {
            if (targetGO != null)
            {
                IPressureActuatable actuatable = targetGO.GetComponent<IPressureActuatable>();
                if (actuatable != null)
                {
                    _parsedTargets.Add(actuatable);
                }
                else
                {
                    Debug.LogWarning($"GameObject '{targetGO.name}' assigned to PressurePlateController " +
                                     $"does not have a component implementing IPressureActuatable.", this);
                }
            }
        }

        // Store the initial local position of the plate visual for animation.
        if (plateVisual != null)
        {
            _plateInitialLocalPosition = plateVisual.localPosition;
        }
    }

    // Unity Lifecycle Method: OnTriggerEnter
    // Called when another collider enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is a valid activator based on tags.
        if (IsValidActivator(other))
        {
            _currentlyOnPlate.Add(other); // Add to the set of objects on the plate
            CheckPlateState(); // Re-evaluate the plate's state
        }
    }

    // Unity Lifecycle Method: OnTriggerExit
    // Called when another collider exits the trigger.
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object was a valid activator.
        if (IsValidActivator(other))
        {
            _currentlyOnPlate.Remove(other); // Remove from the set
            CheckPlateState(); // Re-evaluate the plate's state
        }
    }

    // Checks if a collider is considered a valid activator based on the configured tags.
    private bool IsValidActivator(Collider other)
    {
        // It's crucial for objects interacting with triggers to have a Rigidbody.
        // If the collider doesn't have a Rigidbody (and isn't kinematic), OnTriggerEnter/Exit
        // might not be consistently called for non-kinematic Rigidbodies.
        if (other.attachedRigidbody == null || other.isTrigger) 
        {
             // If a Rigidbody is missing and it's not a trigger collider itself,
             // it might not behave as expected with trigger interactions.
             // We allow trigger colliders without Rigidbodies (e.g., another trigger plate)
             // but warn for non-trigger colliders without Rigidbodies.
            if (!other.isTrigger)
            {
                Debug.LogWarning($"Collider '{other.name}' on '{other.gameObject.name}' entered/exited " +
                                 $"pressure plate '{gameObject.name}' but has no Rigidbody. " +
                                 $"Trigger events might be unreliable. Consider adding a Rigidbody.", other.gameObject);
            }
        }
        
        // If no specific activator tags are set, any valid collider can activate it.
        if (activatorTags == null || activatorTags.Length == 0)
        {
            return true;
        }

        // Otherwise, check if the object's tag is in the list of valid activator tags.
        return activatorTags.Any(tag => other.CompareTag(tag));
    }

    // Evaluates the current number of valid activators on the plate
    // and determines if the plate should be pressed or released.
    private void CheckPlateState()
    {
        // Filter the currently on plate set to only count valid activators based on current tags.
        // This handles cases where tags might change at runtime (though rare).
        int validActivatorCount = _currentlyOnPlate.Count(collider => IsValidActivator(collider));

        if (validActivatorCount >= requiredActivators && !_isPressed)
        {
            // Condition met and plate not yet pressed -> Start pressing sequence
            StartActivationProcess(true);
        }
        else if (validActivatorCount < requiredActivators && _isPressed)
        {
            // Condition no longer met and plate is pressed -> Start releasing sequence
            StartActivationProcess(false);
        }
    }

    // Initiates the activation or deactivation process, managing the delay.
    private void StartActivationProcess(bool activate)
    {
        // Stop any ongoing activation/deactivation coroutine to prevent conflicts.
        if (_activationCoroutine != null)
        {
            StopCoroutine(_activationCoroutine);
        }
        _activationCoroutine = StartCoroutine(ActivationCoroutine(activate));
    }

    // Coroutine to handle the activation/deactivation delay and notify targets.
    private IEnumerator ActivationCoroutine(bool activate)
    {
        // Wait for the specified delay before executing actions.
        if (activationDelay > 0)
        {
            yield return new WaitForSeconds(activationDelay);
        }

        _isPressed = activate; // Update the plate's internal state

        // Notify all registered IPressureActuatable targets.
        foreach (IPressureActuatable target in _parsedTargets)
        {
            if (target != null) // Basic null check
            {
                if (activate)
                {
                    target.Activate(); // Call Activate on the target
                }
                else
                {
                    target.Deactivate(); // Call Deactivate on the target
                }
            }
        }

        // Play sound feedback.
        if (_audioSource != null)
        {
            if (activate && pressSound != null)
            {
                _audioSource.PlayOneShot(pressSound);
            }
            else if (!activate && releaseSound != null)
            {
                _audioSource.PlayOneShot(releaseSound);
            }
        }

        // Animate the plate visual.
        if (plateVisual != null)
        {
            StartCoroutine(MovePlateVisual(activate));
        }
    }

    // Coroutine to smoothly move the plate visual up or down.
    private IEnumerator MovePlateVisual(bool pressed)
    {
        Vector3 targetPosition = pressed ? _plateInitialLocalPosition + pressedOffset : _plateInitialLocalPosition;
        float startTime = Time.time;
        Vector3 startPosition = plateVisual.localPosition;
        float distance = Vector3.Distance(startPosition, targetPosition);

        // Avoid division by zero if start and target are the same (no movement).
        if (distance < 0.001f) yield break;

        float duration = distance / plateAnimationSpeed;
        if (duration == 0) yield break; // Avoid division by zero if speed is very high or distance is zero

        while (plateVisual.localPosition != targetPosition)
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            plateVisual.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
    }

    // Optional: Draw Gizmos for better visualization in the editor.
    private void OnDrawGizmos()
    {
        // Draw the trigger collider bounds in the editor.
        Collider plateCollider = GetComponent<Collider>();
        if (plateCollider != null)
        {
            Gizmos.color = _isPressed ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix; // Apply object's transform
            if (plateCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (plateCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (plateCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center + Vector3.up * (capsule.height / 2 - capsule.radius), capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - Vector3.up * (capsule.height / 2 - capsule.radius), capsule.radius);
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height - capsule.radius * 2, capsule.radius * 2));
            }
            Gizmos.matrix = Matrix4x4.identity; // Reset Gizmo matrix

            // Draw lines to linked targets
            Gizmos.color = Color.yellow;
            foreach (GameObject targetGO in actuatableTargets)
            {
                if (targetGO != null)
                {
                    Gizmos.DrawLine(transform.position, targetGO.transform.position);
                    Gizmos.DrawSphere(targetGO.transform.position, 0.2f);
                }
            }
        }
    }
}
```

---

### **3. `DoorController.cs` (Example IPressureActuatable Implementation)**

This script is an example of a target that responds to the `PressurePlateController`. It implements the `IPressureActuatable` interface to open and close a door.

```csharp
using UnityEngine;
using System.Collections;

// PressurePlateSystem Design Pattern: Example Actuatable Target (Door)
// This script demonstrates how an object can implement the IPressureActuatable interface
// to respond to commands from a PressurePlateController.
// When Activate() is called, the door opens; when Deactivate() is called, it closes.
public class DoorController : MonoBehaviour, IPressureActuatable
{
    [Header("Door Configuration")]
    [Tooltip("The position offset from the door's initial position when it's open.")]
    [SerializeField] private Vector3 openPositionOffset = new Vector3(0, 3f, 0);

    [Tooltip("Speed at which the door moves between open and closed states.")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private AudioSource _audioSource;
    private Vector3 _initialPosition;
    private bool _isOpen = false;
    private Coroutine _doorMovementCoroutine;

    // Unity Lifecycle Method: Awake
    // Stores the door's initial position and gets/adds an AudioSource.
    void Awake()
    {
        _initialPosition = transform.position;

        // Get or add an AudioSource component for playing sounds.
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Implements IPressureActuatable.Activate().
    /// Called by the PressurePlateController to open the door.
    /// </summary>
    public void Activate()
    {
        if (!_isOpen) // Only open if not already open
        {
            // Stop any ongoing movement to prevent conflicts.
            if (_doorMovementCoroutine != null)
            {
                StopCoroutine(_doorMovementCoroutine);
            }
            _doorMovementCoroutine = StartCoroutine(MoveDoor(true));
        }
    }

    /// <summary>
    /// Implements IPressureActuatable.Deactivate().
    /// Called by the PressurePlateController to close the door.
    /// </summary>
    public void Deactivate()
    {
        if (_isOpen) // Only close if not already closed
        {
            // Stop any ongoing movement to prevent conflicts.
            if (_doorMovementCoroutine != null)
            {
                StopCoroutine(_doorMovementCoroutine);
            }
            _doorMovementCoroutine = StartCoroutine(MoveDoor(false));
        }
    }

    // Coroutine to smoothly move the door to its open or closed position.
    private IEnumerator MoveDoor(bool open)
    {
        _isOpen = open; // Update the door's internal state

        Vector3 targetPosition = open ? _initialPosition + openPositionOffset : _initialPosition;
        float startTime = Time.time;
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);

        // Play sound feedback.
        if (_audioSource != null)
        {
            if (open && openSound != null)
            {
                _audioSource.PlayOneShot(openSound);
            }
            else if (!open && closeSound != null)
            {
                _audioSource.PlayOneShot(closeSound);
            }
        }

        // Avoid division by zero if start and target are the same (no movement).
        if (distance < 0.001f) yield break;

        float duration = distance / moveSpeed;
        if (duration == 0) yield break; // Avoid division by zero if speed is very high or distance is zero

        while (transform.position != targetPosition)
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
    }
}
```

---

### **How to Implement and Use in Unity:**

1.  **Create Scripts**:
    *   Create a C# script named `IPressureActuatable.cs` and paste the `IPressureActuatable` interface code into it.
    *   Create a C# script named `PressurePlateController.cs` and paste the `PressurePlateController` class code into it.
    *   Create a C# script named `DoorController.cs` and paste the `DoorController` class code into it.

2.  **Setup the Door (Actuatable Target)**:
    *   Create a 3D Cube (GameObject -> 3D Object -> Cube) in your scene. Name it "Door".
    *   Position it where you want the door to be (e.g., X=0, Y=1.5, Z=5). Adjust its scale if needed (e.g., X=1, Y=3, Z=0.2).
    *   Add the `DoorController` component to the "Door" GameObject.
    *   In the `DoorController`'s Inspector:
        *   `Open Position Offset`: Set this to `(0, 3, 0)` to make it move 3 units up when open.
        *   `Move Speed`: Set to `2`.
        *   (Optional) Add `AudioClips` for `Open Sound` and `Close Sound`.

3.  **Setup the Pressure Plate**:
    *   Create another 3D Cube (GameObject -> 3D Object -> Cube) in your scene. Name it "PressurePlate_Base". This will be the static base.
    *   Create a child 3D Cube under "PressurePlate_Base". Name it "PressurePlate_Visual". This will be the part that animates down.
        *   Position "PressurePlate_Visual" slightly above "PressurePlate_Base" (e.g., local Y=0.1). Scale it smaller (e.g., X=0.8, Y=0.2, Z=0.8).
    *   Select "PressurePlate_Base".
    *   Add the `PressurePlateController` component to "PressurePlate_Base".
    *   Ensure the `Box Collider` on "PressurePlate_Base" is marked `Is Trigger`. Resize the collider to encompass the desired activation area.
    *   In the `PressurePlateController`'s Inspector:
        *   **`Actuatable Targets`**: Drag the "Door" GameObject from your Hierarchy into the `Actuatable Targets` list.
        *   **`Plate Visual`**: Drag the "PressurePlate_Visual" GameObject from the Hierarchy into this slot.
        *   **`Pressed Offset`**: Set to `(0, -0.1, 0)`.
        *   **`Activator Tags`**: Click the plus icon and add `"Player"` (or whatever tag your player character uses). If you want crates to activate it, add `"Crate"` as well.
        *   **`Required Activators`**: Set to `1`.
        *   (Optional) Add `AudioClips` for `Press Sound` and `Release Sound`.

4.  **Setup the Player (Activator)**:
    *   Ensure your Player character has a `Rigidbody` component (required for `OnTriggerEnter`/`Exit` events with non-kinematic colliders).
    *   Make sure your Player character has the tag specified in `PressurePlateController`'s `Activator Tags` (e.g., "Player"). If you don't have a "Player" tag, go to Unity's Tag Manager (Edit -> Project Settings -> Tags and Layers) to add it.
    *   Give your Player character a `Collider` (e.g., `CapsuleCollider`).

5.  **Test**:
    *   Run the scene.
    *   Move your player character onto the "PressurePlate\_Base". The "PressurePlate\_Visual" should move down, and the "Door" should open.
    *   Move your player character off the pressure plate. The "PressurePlate\_Visual" should move up, and the "Door" should close.

This setup provides a robust and extensible system where you can easily add more `IPressureActuatable` components (e.g., `LightController`, `PlatformMover`) and link them to the same or different pressure plates without modifying the `PressurePlateController` itself.