// Unity Design Pattern Example: RaycastInteractionSystem
// This script demonstrates the RaycastInteractionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **RaycastInteractionSystem** design pattern. This pattern decouples the concerns of raycasting and input handling from the objects that can be interacted with.

**Core Idea of RaycastInteractionSystem:**

A central "System" (e.g., `RaycastInteractionSystem`) is responsible for:
1.  **Raycasting:** Continuously casting a ray into the scene (e.g., from the camera's center).
2.  **Detection:** Identifying if the ray hits an object that implements a specific `IInteractable` interface.
3.  **State Management:** Tracking which `IInteractable` object is currently being hovered over.
4.  **Event Dispatching:** Notifying the `IInteractable` objects about events like `OnRayEnter`, `OnRayStay`, `OnRayExit`, and `OnInteract`.

Any object that wishes to be interactable simply needs to implement the `IInteractable` interface and attach the corresponding script.

---

### **1. The `IInteractable` Interface**

This interface defines the contract for any object that can be interacted with by the `RaycastInteractionSystem`.

**File: `IInteractable.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Interface for objects that can be interacted with by the RaycastInteractionSystem.
/// </summary>
/// <remarks>
/// Any GameObject component implementing this interface can be detected and notified
/// by the central RaycastInteractionSystem. This promotes decoupling and reusability.
/// </remarks>
public interface IInteractable
{
    /// <summary>
    /// Called by the RaycastInteractionSystem when the raycast first enters this interactable object.
    /// </summary>
    /// <param name="hit">The RaycastHit information for the entry point.</param>
    void OnRayEnter(RaycastHit hit);

    /// <summary>
    /// Called by the RaycastInteractionSystem every frame the raycast stays on this interactable object.
    /// </summary>
    /// <param name="hit">The current RaycastHit information.</param>
    void OnRayStay(RaycastHit hit);

    /// <summary>
    /// Called by the RaycastInteractionSystem when the raycast exits this interactable object.
    /// </summary>
    void OnRayExit();

    /// <summary>
    /// Called by the RaycastInteractionSystem when the interaction input is triggered
    /// while the raycast is currently on this object.
    /// </summary>
    /// <param name="hit">The RaycastHit information at the moment of interaction.</param>
    void OnInteract(RaycastHit hit);
}

```

---

### **2. The `RaycastInteractionSystem` (The System)**

This script is the core of the pattern. It performs the raycasting, manages the state of hovered interactables, and dispatches events.

**File: `RaycastInteractionSystem.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Unity Input System
using System.Collections.Generic; // Not strictly needed for this basic version but often useful

/// <summary>
/// RaycastInteractionSystem: A central system for detecting and interacting with IInteractable objects via raycasting.
/// This script should typically be attached to the main camera or a dedicated GameObject that performs the raycasting.
/// </summary>
/// <remarks>
/// This pattern decouples the raycasting logic from the interactable objects themselves.
/// The system is responsible for detecting interactions, and IInteractable objects are responsible
/// for defining *how* they respond to these interactions.
///
/// How it works:
/// 1.  **Raycasting:** Every frame, a ray is cast from the camera's center (or a specified origin).
/// 2.  **Detection:** If the ray hits a collider, it checks if that collider's GameObject (or its parents)
///     has an attached component that implements the `IInteractable` interface.
/// 3.  **State Management:** It keeps track of the currently hovered `IInteractable` object.
/// 4.  **Event Dispatching:**
///     -   `OnRayEnter()`: Called when the ray first hits an `IInteractable`.
///     -   `OnRayStay()`: Called every frame the ray remains on an `IInteractable`.
///     -   `OnRayExit()`: Called when the ray leaves an `IInteractable`.
///     -   `OnInteract()`: Called when a specified interaction input (e.g., mouse click) occurs
///         while the ray is on an `IInteractable`.
///
/// Benefits:
/// -   **Decoupling:** Interaction logic is separated from raycasting logic.
/// -   **Scalability:** Easily add new types of interactable objects by implementing `IInteractable`.
/// -   **Centralized Control:** All raycasting and interaction input handling is in one place.
/// -   **Flexibility:** Can be adapted for different ray origins (camera, player character, custom transform)
///     and input methods (mouse, touch, gamepad).
/// </remarks>
[RequireComponent(typeof(Camera))] // Ensures this GameObject has a Camera component
public class RaycastInteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("The layer(s) that interactable objects belong to. Only objects on these layers will be considered.")]
    [SerializeField] private LayerMask interactionLayer;
    [Tooltip("The maximum distance the raycast will travel from the camera.")]
    [SerializeField] private float maxInteractionDistance = 100f;
    [Tooltip("If true, a debug ray will be drawn in the editor for visualization.")]
    [SerializeField] private bool showDebugRay = true;

    [Header("Interaction Input")]
    [Tooltip("Assign an Input Action Property (from an Input Action Asset) to trigger interaction. " +
             "If unassigned, the 'Interaction Button Name' (old Input System) will be used as a fallback.")]
    [SerializeField] private InputActionProperty interactionInputAction;
    [Tooltip("If using the old Input System (or 'Interaction Input Action' is not set), specify the button name " +
             "(e.g., 'Fire1' for left mouse click, 'Jump' for spacebar).")]
    [SerializeField] private string interactionButtonName = "Fire1"; // Fallback for old Input System

    private Camera _camera;
    private IInteractable _currentInteractable; // The object currently being hovered by the ray
    private IInteractable _previousInteractable; // The object that was hovered in the previous frame

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("RaycastInteractionSystem requires a Camera component on the same GameObject.", this);
            enabled = false; // Disable the script if no camera is found
            return;
        }

        // Initialize and enable the input action if using the new Input System
        if (interactionInputAction != null && interactionInputAction.action != null)
        {
            interactionInputAction.action.Enable();
        }
    }

    private void OnEnable()
    {
        // Re-enable input action if it was disabled (e.g., script disabled/re-enabled)
        if (interactionInputAction != null && interactionInputAction.action != null)
        {
            interactionInputAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Disable input action to prevent memory leaks or unwanted input when the script is not active
        if (interactionInputAction != null && interactionInputAction.action != null)
        {
            interactionInputAction.action.Disable();
        }

        // Ensure OnRayExit is called if we disable the system while hovering something
        if (_currentInteractable != null)
        {
            _currentInteractable.OnRayExit();
            _currentInteractable = null;
            _previousInteractable = null;
        }
    }

    private void Update()
    {
        PerformRaycast();
        CheckForInteractionInput();
    }

    // --- Core Logic ---

    /// <summary>
    /// Performs the raycast from the camera's center and manages the state transitions
    /// for IInteractable objects (Enter, Stay, Exit).
    /// </summary>
    private void PerformRaycast()
    {
        // Create a ray from the center of the camera's viewport
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        RaycastHit hit;
        IInteractable hitInteractable = null;

        // Perform the raycast
        if (Physics.Raycast(ray, out hit, maxInteractionDistance, interactionLayer))
        {
            // If we hit something, try to get an IInteractable component from it.
            // We search on the hit object and its parents to allow for complex GameObject hierarchies
            // where the collider might be on a child, but the interactable logic on the parent.
            hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        // --- State Management for IInteractable Events ---

        if (hitInteractable != null) // We hit an interactable object
        {
            if (_currentInteractable == null) // Case 1: First time entering an interactable object
            {
                _currentInteractable = hitInteractable;
                _currentInteractable.OnRayEnter(hit);
            }
            else if (_currentInteractable == hitInteractable) // Case 2: Still hovering the same interactable object
            {
                _currentInteractable.OnRayStay(hit);
            }
            else // Case 3: Hovering a *new* interactable object (transition from one to another)
            {
                _previousInteractable = _currentInteractable;
                _previousInteractable.OnRayExit(); // Notify the old object that the ray has exited it

                _currentInteractable = hitInteractable;
                _currentInteractable.OnRayEnter(hit); // Notify the new object that the ray has entered it
            }
        }
        else // No interactable object hit (or hit something not interactable)
        {
            if (_currentInteractable != null) // Case 4: We were previously hovering an interactable, but now we're not
            {
                _previousInteractable = _currentInteractable;
                _previousInteractable.OnRayExit(); // Notify the previous object that the ray has exited it

                _currentInteractable = null; // Reset current interactable
            }
            // If _currentInteractable is already null, do nothing (ray is not on any interactable).
        }
    }

    /// <summary>
    /// Checks for interaction input and calls OnInteract on the currently hovered object if an input is detected.
    /// </summary>
    private void CheckForInteractionInput()
    {
        // Only check for interaction input if an interactable object is currently being hovered
        if (_currentInteractable != null)
        {
            bool interactionTriggered = false;

            // Priority 1: New Input System check (if an action is assigned)
            if (interactionInputAction != null && interactionInputAction.action != null && interactionInputAction.action.enabled)
            {
                if (interactionInputAction.action.WasPressedThisFrame())
                {
                    interactionTriggered = true;
                }
            }
            // Priority 2: Old Input System fallback (if no new input action is assigned or enabled)
            else if (!string.IsNullOrEmpty(interactionButtonName))
            {
                if (Input.GetButtonDown(interactionButtonName))
                {
                    interactionTriggered = true;
                }
            }

            // If an interaction input was triggered, call OnInteract on the current object
            if (interactionTriggered)
            {
                // Re-perform a quick raycast to get accurate hit information for OnInteract
                Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, maxInteractionDistance, interactionLayer))
                {
                    _currentInteractable.OnInteract(hitInfo);
                }
            }
        }
    }

    // --- Debugging ---

    /// <summary>
    /// Draws a debug ray in the editor's Scene view to visualize the raycast.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showDebugRay && _camera != null)
        {
            // Set the color for the debug ray
            Gizmos.color = Color.red;
            // Draw a ray from the camera's center, extending up to maxInteractionDistance
            Gizmos.DrawRay(_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)).origin,
                            _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)).direction * maxInteractionDistance);
        }
    }
}
```

---

### **3. Example `IInteractable` Implementation (`SimpleHighlightInteractable`)**

This is a concrete example of an object that can be interacted with. It changes color on hover and logs a message on click.

**File: `SimpleHighlightInteractable.cs`**

```csharp
using UnityEngine;

/// <summary>
/// An example implementation of IInteractable that visually responds to raycast events.
/// It changes the material color when the raycast enters, stays, or exits,
/// and logs a message to the console on interaction (e.g., mouse click).
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensures this GameObject has a Renderer component to change color
[RequireComponent(typeof(Collider))] // Ensures this GameObject has a Collider to be hit by raycasts
public class SimpleHighlightInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("The default color of the object when not hovered.")]
    [SerializeField] private Color defaultColor = Color.white;
    [Tooltip("The color the object changes to when the raycast hovers over it.")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("The color the object briefly changes to when an interaction occurs.")]
    [SerializeField] private Color pressedColor = Color.red;

    private Renderer _renderer;
    private Color _initialMaterialColor; // To store the color found at Awake

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            // Store the initial color of the material.
            // This allows the defaultColor field to be overridden by the material's actual color.
            _initialMaterialColor = _renderer.material.color;
        }
    }

    /// <summary>
    /// Called when the raycast first enters this interactable object.
    /// Changes the object's color to highlightColor.
    /// </summary>
    /// <param name="hit">The RaycastHit information.</param>
    public void OnRayEnter(RaycastHit hit)
    {
        Debug.Log($"Ray entered {gameObject.name} at {hit.point}.");
        if (_renderer != null)
        {
            _renderer.material.color = highlightColor;
        }
    }

    /// <summary>
    /// Called every frame the raycast stays on this interactable object.
    /// Currently, it does nothing visually, but could be used for continuous feedback.
    /// </summary>
    /// <param name="hit">The current RaycastHit information.</param>
    public void OnRayStay(RaycastHit hit)
    {
        // Example: Could play a subtle animation, update UI, etc.
        // Debug.Log($"Ray staying on {gameObject.name}"); // Uncomment for detailed debug
    }

    /// <summary>
    /// Called when the raycast exits this interactable object.
    /// Restores the object's color to its defaultColor.
    /// </summary>
    public void OnRayExit()
    {
        Debug.Log($"Ray exited {gameObject.name}.");
        if (_renderer != null)
        {
            _renderer.material.color = _initialMaterialColor; // Restore to the color found at Awake
        }
    }

    /// <summary>
    /// Called when the interaction input is triggered while the raycast is on this object.
    /// Logs a message, briefly changes color to pressedColor, and then restores highlight.
    /// </summary>
    /// <param name="hit">The RaycastHit information at the moment of interaction.</param>
    public void OnInteract(RaycastHit hit)
    {
        Debug.Log($"<color=green>Interacted with {gameObject.name} at {hit.point}!</color>");
        if (_renderer != null)
        {
            // Briefly show pressed color, then restore to highlight color
            _renderer.material.color = pressedColor;
            Invoke(nameof(RestoreHighlightColor), 0.15f); // Restore highlight after a short delay
        }

        // --- Example of further actions on interact ---
        // MyCustomEventManager.Instance.TriggerInteractionEvent(this.gameObject);
        // Play a sound effect: AudioManager.Instance.PlaySFX("ButtonPress");
        // Open a door: GetComponent<Door>().Open();
        // Pick up an item: Inventory.Instance.AddItem(this.gameObject.GetComponent<CollectableItem>());
    }

    /// <summary>
    /// Helper method to restore the highlight color after a brief 'pressed' state.
    /// </summary>
    private void RestoreHighlightColor()
    {
        if (_renderer != null)
        {
            _renderer.material.color = highlightColor; // Go back to highlight color
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create Scripts:**
    *   Create a new C# script named `IInteractable.cs` and paste the content from section 1.
    *   Create a new C# script named `RaycastInteractionSystem.cs` and paste the content from section 2.
    *   Create a new C# script named `SimpleHighlightInteractable.cs` and paste the content from section 3.

2.  **Configure Layers:**
    *   Go to `Edit > Project Settings > Tags and Layers`.
    *   Under "Layers", add a new User Layer (e.g., `Interactable`).
    *   (Optional but recommended for clean projects) Add another layer called `UI` if you have screen-space UI elements that should block raycasts but not be interactable by this system.

3.  **Setup `RaycastInteractionSystem`:**
    *   Select your `Main Camera` GameObject in the Hierarchy.
    *   Add the `RaycastInteractionSystem` component to it (`Add Component` -> search for "RaycastInteractionSystem").
    *   **Inspector Settings for `RaycastInteractionSystem`:**
        *   **Interaction Layer:** Select the `Interactable` layer you created.
        *   **Max Interaction Distance:** Set a reasonable value (e.g., `50` or `100`).
        *   **Show Debug Ray:** Check this to see the ray in the Scene view during play mode.

        *   **Input System (Choose one):**
            *   **New Input System (Recommended):** If you're using Unity's new Input System, create an `Input Action Asset` (`Assets > Create > Input Actions`). Define an Action (e.g., `Interact`) and bind it to a desired input (e.g., `Left Mouse Button`). Drag this `Input Action` to the `Interaction Input Action` field on your `RaycastInteractionSystem` component.
            *   **Old Input System (Fallback):** If you leave `Interaction Input Action` unassigned, the system will fall back to using `Input.GetButtonDown(interactionButtonName)`. The default `interactionButtonName` "Fire1" corresponds to the left mouse button.

4.  **Setup Interactable Objects:**
    *   Create some 3D objects in your scene (e.g., `GameObject > 3D Object > Cube`).
    *   Ensure each object has a **Collider** component (e.g., `Box Collider`, `Mesh Collider`).
    *   Set the **Layer** of each of these objects to `Interactable` (the layer you created).
    *   Add the `SimpleHighlightInteractable` component to each of these objects.
    *   (Optional) Adjust the `Default Color`, `Highlight Color`, and `Pressed Color` on the `SimpleHighlightInteractable` component for each object.

5.  **Run the Scene:**
    *   Play the scene.
    *   Move your mouse over the interactable objects. They should change to their `Highlight Color`.
    *   Click the interaction button (e.g., Left Mouse Button). The object should briefly flash `Pressed Color` and then return to `Highlight Color`, and a log message will appear in the console.
    *   Move your mouse off an object, and it will return to its `Default Color`.

This complete setup provides a robust and extensible interaction system for your Unity projects.