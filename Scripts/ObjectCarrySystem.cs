// Unity Design Pattern Example: ObjectCarrySystem
// This script demonstrates the ObjectCarrySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ObjectCarrySystem' design pattern in Unity (while not a formal Gang of Four pattern, it's a common and practical gameplay system) refers to the architecture for allowing a character to pick up, carry, and drop/throw objects. It emphasizes **separation of concerns** by dividing responsibilities between the object doing the carrying (e.g., a player character) and the objects that *can be carried*.

This example provides two scripts:
1.  **`ObjectCarrySystem.cs`**: This script is attached to the character that *performs* the carrying (e.g., Player). It handles input, detects carryable objects, manages the state of the carried object (parenting, physics), and applies forces for throwing.
2.  **`CarryableObject.cs`**: This is a simple marker script attached to any `GameObject` that *can be carried*. It primarily ensures the object has the necessary physics components (`Rigidbody`, `Collider`) and provides a simple way for the `ObjectCarrySystem` to identify it.

---

### Key Concepts & Design Principles Demonstrated:

*   **Component-Based Architecture:** Both features are implemented as reusable `MonoBehaviour` components.
*   **Separation of Concerns:**
    *   `ObjectCarrySystem` focuses on the *interaction logic* (pickup input, finding objects, manipulating physics, parenting).
    *   `CarryableObject` focuses on *identifying* an object as carryable and ensuring it has the correct base components. It doesn't need to know *how* it's carried.
*   **Loose Coupling:** The `ObjectCarrySystem` interacts with `CarryableObject` instances through their common component type, not specific implementations.
*   **State Management:** The `ObjectCarrySystem` manages whether an object is currently being carried (`_carriedObject`).
*   **Physics Interaction:** Correctly handling `Rigidbody` and `Collider` properties (`isKinematic`, `enabled`, `SetParent`, `AddForce`) during pickup, carry, and drop/throw states.
*   **Unity Best Practices:** `[SerializeField]`, `LayerMask`, `OnDrawGizmos`, `RequireComponent`, clear comments, and appropriate `Update()` usage.

---

### `ObjectCarrySystem.cs`

This script should be attached to your player character or an object that can pick up items. It requires a `Rigidbody` on the same GameObject or a parent GameObject for accurate force application if the player itself moves while carrying.

```csharp
using UnityEngine;
using System.Collections; // Required for coroutines if used, though not directly in this example

/// <summary>
/// Implements the 'ObjectCarrySystem' design pattern, allowing a character to pick up,
/// carry, and drop/throw objects in a Unity game.
/// This script handles the interaction logic from the player's perspective.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures the player has a Rigidbody for physics interactions
public class ObjectCarrySystem : MonoBehaviour
{
    [Header("Carry Settings")]
    [Tooltip("The Transform where the carried object will be parented and positioned.")]
    [SerializeField] private Transform _carryPoint;

    [Tooltip("The maximum distance from the player to detect carryable objects.")]
    [SerializeField] private float _pickupRange = 2.0f;

    [Tooltip("The force applied when throwing the object.")]
    [SerializeField] private float _throwForce = 15.0f;

    [Tooltip("The LayerMask used to filter which objects can be picked up. " +
             "Ensure your carryable objects are on this layer.")]
    [SerializeField] private LayerMask _carryableLayer;

    [Header("Input Settings")]
    [Tooltip("Key to press to pick up or drop an object.")]
    [SerializeField] private KeyCode _pickupDropKey = KeyCode.E;

    [Tooltip("Key to press to throw an object.")]
    [SerializeField] private KeyCode _throwKey = KeyCode.Q;

    // Internal references for the currently carried object and its components
    private CarryableObject _carriedObject = null;
    private Rigidbody _carriedRigidbody = null;
    private Collider _carriedCollider = null;

    // Reference to the player's Rigidbody, useful for physics interactions
    private Rigidbody _playerRigidbody;

    private void Awake()
    {
        _playerRigidbody = GetComponent<Rigidbody>();
        if (_playerRigidbody == null)
        {
            Debug.LogError("ObjectCarrySystem requires a Rigidbody on the GameObject or a parent. Please add one.", this);
            enabled = false; // Disable script if Rigidbody is missing
            return;
        }

        if (_carryPoint == null)
        {
            Debug.LogWarning("Carry Point not assigned. Creating a default one slightly in front of the player.", this);
            GameObject defaultCarryPoint = new GameObject("DefaultCarryPoint");
            defaultCarryPoint.transform.SetParent(transform);
            defaultCarryPoint.transform.localPosition = Vector3.forward * 0.5f; // Slightly in front
            _carryPoint = defaultCarryPoint.transform;
        }
    }

    private void Update()
    {
        // Handle Pickup/Drop input
        if (Input.GetKeyDown(_pickupDropKey))
        {
            if (_carriedObject != null)
            {
                // If an object is carried, drop it
                DropObject();
            }
            else
            {
                // If no object is carried, attempt to pick one up
                AttemptPickup();
            }
        }

        // Handle Throw input
        if (Input.GetKeyDown(_throwKey) && _carriedObject != null)
        {
            ThrowObject();
        }
    }

    /// <summary>
    /// Attempts to find and pick up the closest CarryableObject within the pickup range.
    /// </summary>
    private void AttemptPickup()
    {
        // Use OverlapSphere to find all colliders within the pickup range on the specified layer.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _pickupRange, _carryableLayer);
        
        CarryableObject closestCarryable = null;
        float minDistance = float.MaxValue;

        // Iterate through found colliders to find a CarryableObject
        foreach (var hitCollider in hitColliders)
        {
            // Try to get the CarryableObject component from the hit collider's GameObject
            CarryableObject carryable = hitCollider.GetComponent<CarryableObject>();
            if (carryable != null && !carryable.IsCarried) // Ensure it's not already being carried by someone else
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestCarryable = carryable;
                }
            }
        }

        // If a carryable object was found, pick it up
        if (closestCarryable != null)
        {
            PickUpObject(closestCarryable);
        }
    }

    /// <summary>
    /// Performs the actions required to pick up a given CarryableObject.
    /// This includes parenting, disabling physics, and updating internal references.
    /// </summary>
    /// <param name="objToCarry">The CarryableObject to pick up.</param>
    private void PickUpObject(CarryableObject objToCarry)
    {
        if (objToCarry == null) return;

        // Get Rigidbody and Collider components from the object to carry
        Rigidbody rb = objToCarry.GetComponent<Rigidbody>();
        Collider col = objToCarry.GetComponent<Collider>();

        if (rb == null || col == null)
        {
            Debug.LogError($"Carryable object '{objToCarry.name}' is missing Rigidbody or Collider component. Cannot pick up.", objToCarry);
            return;
        }

        _carriedObject = objToCarry;
        _carriedRigidbody = rb;
        _carriedCollider = col;

        // 1. Disable physics on the carried object
        _carriedRigidbody.isKinematic = true; // Makes it immune to forces
        _carriedRigidbody.velocity = Vector3.zero;
        _carriedRigidbody.angularVelocity = Vector3.zero;

        // 2. Parent the object to the carry point and reset its local transform
        _carriedObject.transform.SetParent(_carryPoint);
        _carriedObject.transform.localPosition = Vector3.zero;
        _carriedObject.transform.localRotation = Quaternion.identity;
        // Optional: Reset local scale if you want the object to have a consistent size when carried
        // _carriedObject.transform.localScale = Vector3.one; 

        // 3. Disable the object's collider to prevent it from interacting with the environment while carried
        // This prevents collision issues with the player or environment.
        _carriedCollider.enabled = false;

        // Inform the CarryableObject that it's being carried (optional, for its internal state)
        _carriedObject.OnEnableCarry();

        Debug.Log($"Picked up: {_carriedObject.name}", _carriedObject);
    }

    /// <summary>
    /// Releases the currently carried object, re-enabling its physics and unparenting it.
    /// </summary>
    private void DropObject()
    {
        if (_carriedObject == null) return;

        // 1. Re-enable the object's collider
        _carriedCollider.enabled = true;

        // 2. Re-enable physics on the object
        _carriedRigidbody.isKinematic = false;

        // 3. Unparent the object
        _carriedObject.transform.SetParent(null);

        // Inform the CarryableObject that it's no longer carried
        _carriedObject.OnDisableCarry();

        Debug.Log($"Dropped: {_carriedObject.name}", _carriedObject);

        // Clear references
        _carriedObject = null;
        _carriedRigidbody = null;
        _carriedCollider = null;
    }

    /// <summary>
    /// Throws the currently carried object forward with a specified force.
    /// This essentially performs a drop and then applies an impulse force.
    /// </summary>
    private void ThrowObject()
    {
        if (_carriedObject == null) return;

        // Store a reference to the rigidbody before dropping, as DropObject will nullify _carriedRigidbody
        Rigidbody rbToThrow = _carriedRigidbody; 

        // First, perform the standard drop actions
        DropObject();

        // Then, apply a force to the now unparented and physics-enabled object
        if (rbToThrow != null)
        {
            // Apply force in the direction of the carry point (which is usually forward relative to player)
            rbToThrow.AddForce(_carryPoint.forward * _throwForce, ForceMode.Impulse);
            Debug.Log($"Threw: {rbToThrow.name} with force {_throwForce}", rbToThrow);
        }
    }

    /// <summary>
    /// Draws a debug sphere in the editor to visualize the pickup range.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _pickupRange);

        if (_carryPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_carryPoint.position, 0.1f); // Show carry point
            Gizmos.DrawRay(_carryPoint.position, _carryPoint.forward * 0.5f); // Show forward direction
        }
    }
}

```

---

### `CarryableObject.cs`

This script should be attached to any `GameObject` you want to be pick-up-able. It requires both a `Rigidbody` and a `Collider` on the same `GameObject`.

```csharp
using UnityEngine;

/// <summary>
/// A marker component for objects that can be picked up and carried by the ObjectCarrySystem.
/// This script ensures the necessary physics components are present and provides a simple
/// API for the carrying system to interact with its state.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures the object has a Rigidbody
[RequireComponent(typeof(Collider))]  // Ensures the object has a Collider
public class CarryableObject : MonoBehaviour
{
    // A simple flag to indicate if this object is currently being carried.
    // This can be useful for other systems that need to query the object's state.
    private bool _isCarried = false;
    public bool IsCarried => _isCarried; // Public read-only property

    // Internal references to components
    private Rigidbody _rigidbody;
    private Collider _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        // Basic validation: ensure Rigidbody and Collider are not null
        if (_rigidbody == null)
        {
            Debug.LogError($"CarryableObject '{name}' is missing a Rigidbody!", this);
            enabled = false; // Disable component if essential parts are missing
        }
        if (_collider == null)
        {
            Debug.LogError($"CarryableObject '{name}' is missing a Collider!", this);
            enabled = false; // Disable component if essential parts are missing
        }

        // Ensure the Rigidbody is initially non-kinematic so it can be affected by physics
        _rigidbody.isKinematic = false;
    }

    /// <summary>
    /// Called by the ObjectCarrySystem when this object is picked up.
    /// Updates the internal state.
    /// </summary>
    public void OnEnableCarry()
    {
        _isCarried = true;
        // Optionally, add visual/audio feedback here (e.g., play pickup sound)
        // Debug.Log($"{name} was picked up.");
    }

    /// <summary>
    /// Called by the ObjectCarrySystem when this object is dropped or thrown.
    /// Updates the internal state.
    /// </summary>
    public void OnDisableCarry()
    {
        _isCarried = false;
        // Optionally, add visual/audio feedback here (e.g., play drop sound)
        // Debug.Log($"{name} was dropped.");
    }

    // You can add more object-specific logic here, like:
    // - OnCollisionEnter methods for specific interactions after being dropped/thrown.
    // - Properties for weight, value, or other game-specific attributes.
}

```

---

### Example Usage in Unity

To implement this system in your Unity project:

1.  **Create your Player Character:**
    *   Create a 3D object (e.g., a Capsule, Cube) for your player.
    *   Add a `Rigidbody` component to it. Set `Is Kinematic` to `false` and potentially freeze rotation if your character movement handles rotation separately.
    *   Add a `Capsule Collider` (or appropriate collider).
    *   **Attach the `ObjectCarrySystem.cs` script** to this player `GameObject`.
    *   **Create a Carry Point:**
        *   Right-click on your Player `GameObject` in the Hierarchy and select "Create Empty". Rename it to `CarryPoint`.
        *   Position this `CarryPoint` `GameObject` slightly in front and perhaps a bit above the player's center (e.g., `localPosition` `(0, 0.5, 0.7)`). This will be where the carried object visually appears.
        *   Drag this `CarryPoint` `GameObject` from the Hierarchy into the `_Carry Point` field of the `Object Carry System` component on your player.
    *   **Configure `ObjectCarrySystem`:**
        *   Adjust `_Pickup Range`, `_Throw Force` as needed.
        *   Set the `_Carryable Layer`. This is crucial!

2.  **Create Carryable Objects:**
    *   Create some 3D objects (e.g., Cubes, Spheres) that you want to be able to pick up.
    *   Add a `Rigidbody` component to each. Set `Is Kinematic` to `false`.
    *   Add a `Box Collider` (or appropriate collider) to each.
    *   **Create a new Layer** for your carryable objects:
        *   Go to `Layers` dropdown in the Unity Editor (top right, above Inspector).
        *   Click `Add Layer...`.
        *   Add a new layer, e.g., `Carryable`.
        *   Select your carryable `GameObject`s and change their `Layer` dropdown to `Carryable`.
    *   Go back to your Player's `Object Carry System` component and **set the `_Carryable Layer` field** to the `Carryable` layer you just created.
    *   **Attach the `CarryableObject.cs` script** to each of these objects.

3.  **Run the Scene:**
    *   Move your player near a carryable object.
    *   Press the `E` key (default `_PickupDropKey`) to pick it up.
    *   Move around while carrying it.
    *   Press `E` again to drop it.
    *   Press `Q` (default `_ThrowKey`) to throw it.

This setup provides a complete and functional object carrying system, demonstrating a practical application of common game development patterns and Unity best practices.