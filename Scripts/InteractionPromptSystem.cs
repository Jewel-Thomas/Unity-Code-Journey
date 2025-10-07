// Unity Design Pattern Example: InteractionPromptSystem
// This script demonstrates the InteractionPromptSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'InteractionPromptSystem' design pattern in Unity helps decouple the logic of detecting interactable objects, displaying an interaction prompt to the player, and executing the interaction itself. This makes your game code more modular, maintainable, and scalable.

Here's a complete, practical C# Unity example demonstrating this pattern, including all necessary scripts and detailed setup instructions.

---

### Understanding the Pattern Components:

1.  **`IInteractable` Interface:**
    *   Defines a contract for any object in the scene that can be interacted with.
    *   Specifies properties for the interaction prompt message and whether the object is currently interactable.
    *   Defines the `Interact()` method that will be called when the player interacts.

2.  **`Interactor` Component:**
    *   Attached to the player character.
    *   Responsible for detecting `IInteractable` objects within a certain range (e.g., using raycasts or sphere casts).
    *   Manages the state of the currently focused interactable.
    *   Triggers the `Interact()` method on the focused object when the player presses the interaction key.
    *   Communicates with the `InteractionPromptManager` to display/hide the UI prompt.

3.  **`InteractionPromptUI` Component:**
    *   A simple UI script responsible for displaying the actual text prompt on the screen.
    *   It contains methods to show and hide the prompt with a given message.

4.  **`InteractionPromptManager` (Singleton):**
    *   A central, globally accessible system that orchestrates the `InteractionPromptUI`.
    *   It decouples the `Interactor` from directly managing the UI, allowing any part of the game to request prompt display or hiding.
    *   Typically implemented as a Singleton for easy access.

---

### Project Setup and Scripts

**1. `IInteractable.cs`**
This interface defines what an interactable object needs to provide.

```csharp
// FILE: IInteractable.cs
using UnityEngine;

/// <summary>
/// This interface defines the contract for any object that can be interacted with.
/// It's the 'Interactable' part of the InteractionPromptSystem, allowing different
/// game objects to expose their interaction capabilities in a standardized way.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Gets the text message to display in the prompt UI (e.g., "Press E to open").
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Gets a value indicating whether the object is currently interactable.
    /// This allows objects to dynamically enable/disable interactions (e.g., a locked door).
    /// </summary>
    bool CanInteract { get; }

    /// <summary>
    /// The method that gets called when the player interacts with the object.
    /// The 'Interactor' is passed as an argument, allowing the interactable
    /// to potentially react differently based on who is interacting (e.g., for multiplayer).
    /// </summary>
    /// <param name="interactor">The Interactor component that initiated the interaction.</param>
    void Interact(Interactor interactor);
}
```

**2. `InteractionPromptUI.cs`**
This script manages the visual display of the prompt text.

```csharp
// FILE: InteractionPromptUI.cs
using UnityEngine;
using TMPro; // Required for TextMeshPro. If using legacy UI Text, change this.

/// <summary>
/// This class manages the visual display of the interaction prompt UI element.
/// It's the 'UI Prompt' part of the InteractionPromptSystem, responsible for
/// presenting interaction messages to the player.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component that will display the prompt message.")]
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        // Ensure the prompt text is hidden initially when the UI loads.
        HidePrompt();
    }

    /// <summary>
    /// Displays the interaction prompt with the given message.
    /// The UI element will become active and show the provided text.
    /// </summary>
    /// <param name="message">The text message to display (e.g., "Press E to Open").</param>
    public void ShowPrompt(string message)
    {
        if (promptText == null)
        {
            Debug.LogWarning("InteractionPromptUI: Prompt TextMeshProUGUI is not assigned! Please assign it in the Inspector.", this);
            return;
        }

        promptText.text = message;
        promptText.gameObject.SetActive(true); // Make sure the UI element is visible.
    }

    /// <summary>
    /// Hides the interaction prompt.
    /// The UI element will become inactive.
    /// </summary>
    public void HidePrompt()
    {
        if (promptText == null)
        {
            // Already warned in ShowPrompt, no need to spam if it's consistently missing.
            return;
        }

        promptText.gameObject.SetActive(false); // Hide the UI element.
    }
}
```

**3. `InteractionPromptManager.cs`**
This central singleton manages the `InteractionPromptUI`.

```csharp
// FILE: InteractionPromptManager.cs
using UnityEngine;

/// <summary>
/// This class acts as a central manager for the interaction prompt UI.
/// It follows the Singleton pattern to ensure only one instance exists and
/// provides easy global access to show/hide the prompt from any script.
/// It's the 'Interaction Manager' part of the InteractionPromptSystem,
/// specifically handling the UI display aspect.
/// </summary>
public class InteractionPromptManager : MonoBehaviour
{
    // Static instance of the manager, allowing global access (Singleton pattern).
    public static InteractionPromptManager Instance { get; private set; }

    [Tooltip("Reference to the InteractionPromptUI component that displays the prompt message.")]
    [SerializeField] private InteractionPromptUI promptUI;

    private void Awake()
    {
        // Singleton pattern implementation:
        // Ensure that only one instance of the manager exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InteractionPromptManager: Another instance already exists! Destroying this duplicate.", this);
            Destroy(gameObject); // Destroy this new instance if one already exists.
            return;
        }
        Instance = this; // Set this instance as the singleton.

        // Optional: Make the manager persist across scene loads. Remove if not needed.
        DontDestroyOnLoad(gameObject);

        // Ensure the UI reference is set.
        if (promptUI == null)
        {
            Debug.LogError("InteractionPromptManager: Prompt UI reference is missing! Please assign it in the Inspector.", this);
        }
    }

    /// <summary>
    /// Public method to display an interaction prompt.
    /// This is typically called by the Interactor when an interactable object is detected.
    /// </summary>
    /// <param name="message">The message string to display on the prompt.</param>
    public void DisplayPrompt(string message)
    {
        if (promptUI != null)
        {
            promptUI.ShowPrompt(message);
        }
    }

    /// <summary>
    /// Public method to hide the interaction prompt.
    /// This is typically called by the Interactor when no interactable object is detected
    /// or after an interaction has occurred.
    /// </summary>
    public void ClearPrompt()
    {
        if (promptUI != null)
        {
            promptUI.HidePrompt();
        }
    }
}
```

**4. `Interactor.cs`**
This script is attached to the player and handles detection and interaction logic.

```csharp
// FILE: Interactor.cs
using UnityEngine;

/// <summary>
/// This component is attached to the player and handles detecting interactable objects
/// within range and initiating interactions. It's the 'Player (Interactor)' part of the system.
/// It uses a sphere cast to find IInteractable objects and communicates with the
/// InteractionPromptManager to display relevant UI prompts.
/// </summary>
public class Interactor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The maximum distance within which the player can interact with objects.")]
    [SerializeField] private float interactionRange = 3f;
    [Tooltip("The radius of the sphere used for detecting interactable objects. A larger radius is more forgiving.")]
    [SerializeField] private float interactionSphereRadius = 0.5f;
    [Tooltip("The LayerMask to filter which layers contain interactable objects. Only objects on these layers will be considered.")]
    [SerializeField] private LayerMask interactableLayer;
    [Tooltip("The key (e.g., E, F) the player needs to press to initiate an interaction.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    // The currently focused interactable object. This is the object that the player
    // is looking at/close enough to interact with.
    private IInteractable _currentInteractable;

    private void Update()
    {
        // Determine the origin and direction for the interaction ray/sphere cast.
        // For FPS/TPS, casting from the main camera's position forward is common.
        // For other games, it might be from the character's forward vector.
        Vector3 sphereCastOrigin = Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.up * 0.5f;
        Vector3 forwardDirection = Camera.main != null ? Camera.main.transform.forward : transform.forward;

        RaycastHit hit;
        // Perform a sphere cast to detect interactable objects. Sphere casts are more
        // forgiving than simple raycasts, allowing interaction slightly off-center.
        if (Physics.SphereCast(sphereCastOrigin, interactionSphereRadius, forwardDirection, out hit, interactionRange, interactableLayer))
        {
            // Try to get the IInteractable component from the object that was hit.
            IInteractable detectedInteractable = hit.collider.GetComponent<IInteractable>();

            if (detectedInteractable != null && detectedInteractable.CanInteract)
            {
                // If a new interactable is detected or the previously focused one is different:
                if (detectedInteractable != _currentInteractable)
                {
                    _currentInteractable = detectedInteractable;
                    // Ask the InteractionPromptManager to display the prompt message for the new interactable.
                    InteractionPromptManager.Instance.DisplayPrompt(_currentInteractable.InteractionPrompt);
                }

                // If the interact key is pressed AND an interactable is currently focused:
                if (Input.GetKeyDown(interactKey))
                {
                    // Trigger the interaction on the currently focused object.
                    _currentInteractable.Interact(this);
                    // After interaction, clear the prompt and focus immediately.
                    // This handles cases where the interactable object is destroyed
                    // or its state changes (e.g., a door becomes locked).
                    InteractionPromptManager.Instance.ClearPrompt();
                    _currentInteractable = null;
                }
            }
            else // Hit something, but it's not interactable or cannot interact (e.g., locked door).
            {
                ClearCurrentInteractable();
            }
        }
        else // No interactable object detected within range by the sphere cast.
        {
            ClearCurrentInteractable();
        }
    }

    /// <summary>
    /// Helper method to clear the currently focused interactable and hide the prompt.
    /// </summary>
    private void ClearCurrentInteractable()
    {
        if (_currentInteractable != null)
        {
            InteractionPromptManager.Instance.ClearPrompt();
            _currentInteractable = null;
        }
    }

    // Optional: Draw the interaction sphere in the editor for debugging purposes.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 sphereCastOrigin = Camera.main != null ? Camera.main.transform.position : transform.position + Vector3.up * 0.5f;
        Vector3 forwardDirection = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        
        // Draw the sphere at the end of the interaction range.
        Gizmos.DrawWireSphere(sphereCastOrigin + forwardDirection * interactionRange, interactionSphereRadius);
        // Draw a line representing the sphere cast direction and range.
        Gizmos.DrawLine(sphereCastOrigin, sphereCastOrigin + forwardDirection * interactionRange);
    }
}
```

**5. `DoorInteractable.cs` (Example Interactable)**
An example implementation of an `IInteractable` for a door.

```csharp
// FILE: DoorInteractable.cs
using UnityEngine;

/// <summary>
/// An example implementation of the IInteractable interface for a door object.
/// This door can be opened and closed, and its interaction prompt changes accordingly.
/// It also demonstrates a 'locked' state where interaction is disabled.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [Tooltip("The message displayed when looking at the door to open it.")]
    [SerializeField] private string openPrompt = "Press E to Open Door";
    [Tooltip("The message displayed when looking at the door to close it.")]
    [SerializeField] private string closePrompt = "Press E to Close Door";
    [Tooltip("The local Euler angles for the door when it is open.")]
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
    [Tooltip("The local Euler angles for the door when it is closed.")]
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [Tooltip("How fast the door rotates between open and closed states.")]
    [SerializeField] private float rotationSpeed = 2f;
    [Tooltip("Is the door currently locked? A locked door cannot be interacted with.")]
    [SerializeField] private bool _isLocked = false;
    [Tooltip("Message to show if the door is locked when attempting interaction.")]
    [SerializeField] private string lockedMessage = "Door is Locked!";

    private bool _isOpen = false; // Current state of the door.
    private Quaternion _targetRotation; // The rotation the door is moving towards.

    /// <summary>
    /// Implements IInteractable.InteractionPrompt.
    /// Returns the appropriate prompt message based on the door's state (open/closed/locked).
    /// </summary>
    public string InteractionPrompt
    {
        get
        {
            if (_isLocked) return lockedMessage;
            return _isOpen ? closePrompt : openPrompt;
        }
    }

    /// <summary>
    /// Implements IInteractable.CanInteract.
    /// Returns true if the door is not locked, otherwise false.
    /// </summary>
    public bool CanInteract
    {
        get { return !_isLocked; }
    }

    private void Start()
    {
        // Initialize the door to its closed state at the start of the scene.
        transform.localRotation = Quaternion.Euler(closedRotation);
        _targetRotation = Quaternion.Euler(closedRotation);
    }

    private void Update()
    {
        // Smoothly interpolate the door's current rotation towards its target rotation.
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, Time.deltaTime * rotationSpeed);
    }

    /// <summary>
    /// Implements IInteractable.Interact.
    /// Toggles the door's open/closed state if it's not locked.
    /// </summary>
    /// <param name="interactor">The Interactor component that performed the interaction.</param>
    public void Interact(Interactor interactor)
    {
        if (!CanInteract)
        {
            // Provide feedback if interaction failed due to the door being locked.
            Debug.Log($"Door '{gameObject.name}' is locked and cannot be interacted with by {interactor.gameObject.name}.", this);
            return;
        }

        // Toggle the door's state and update the target rotation.
        _isOpen = !_isOpen;
        _targetRotation = _isOpen ? Quaternion.Euler(openRotation) : Quaternion.Euler(closedRotation);
        Debug.Log($"Door '{gameObject.name}' was {_isOpen}ed by {interactor.gameObject.name}.", this);

        // Example: If a door could only be opened once, you might set _isLocked = true here.
        // For this example, it can be opened/closed repeatedly.
    }

    /// <summary>
    /// Public method to toggle the lock state of the door.
    /// This can be called by other game systems (e.g., player uses a key to unlock).
    /// </summary>
    /// <param name="locked">True to lock the door, false to unlock it.</param>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        if (locked) Debug.Log($"Door '{gameObject.name}' is now LOCKED.", this);
        else Debug.Log($"Door '{gameObject.name}' is now UNLOCKED.", this);
    }
}
```

**6. `PickupInteractable.cs` (Example Interactable)**
Another example for picking up items.

```csharp
// FILE: PickupInteractable.cs
using UnityEngine;

/// <summary>
/// An example implementation of the IInteractable interface for an item that can be picked up.
/// This item disappears after being interacted with once.
/// </summary>
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [Tooltip("The message displayed when looking at the item to pick it up.")]
    [SerializeField] private string pickupPrompt = "Press E to Pick Up";
    [Tooltip("The name of the item, useful for debugging or inventory systems.")]
    [SerializeField] private string itemName = "Generic Item";

    private bool _isPickedUp = false; // Flag to ensure item is picked up only once.

    /// <summary>
    /// Implements IInteractable.InteractionPrompt.
    /// Returns the prompt message for picking up the item.
    /// </summary>
    public string InteractionPrompt => pickupPrompt;

    /// <summary>
    /// Implements IInteractable.CanInteract.
    /// Returns true if the item has not been picked up yet, otherwise false.
    /// </summary>
    public bool CanInteract => !_isPickedUp;

    /// <summary>
    /// Implements IInteractable.Interact.
    /// Marks the item as picked up and destroys the GameObject.
    /// </summary>
    /// <param name="interactor">The Interactor component that performed the interaction.</param>
    public void Interact(Interactor interactor)
    {
        if (!CanInteract)
        {
            Debug.Log($"Item '{itemName}' is already picked up!", this);
            return;
        }

        _isPickedUp = true; // Mark as picked up.
        Debug.Log($"{interactor.gameObject.name} picked up '{itemName}'.", this);

        // --- Real-world use cases here: ---
        // - Add item to player's inventory system.
        // - Play a pickup sound effect.
        // - Trigger visual effects.

        // For this example, we simply destroy the GameObject after pickup.
        Destroy(gameObject);

        // After destroying, the Interactor will automatically clear the prompt
        // in its next Update cycle as the object will no longer be detected.
        // However, explicitly clearing it here can provide immediate feedback.
        InteractionPromptManager.Instance.ClearPrompt();
    }
}
```

---

### Unity Editor Setup Instructions

Follow these steps to set up the InteractionPromptSystem in your Unity project:

**1. Project Preparation:**
   *   Open your Unity project.
   *   **Import TextMeshPro:** Go to `Window > TextMeshPro > Import TMP Essential Resources`. This is required for `TextMeshProUGUI`.

**2. Create the UI Canvas and Prompt:**
   *   In the Hierarchy, right-click -> `UI` -> `Canvas`. Name it **"InteractionCanvas"**.
   *   Ensure its `Render Mode` in the Inspector is `Screen Space - Overlay` (usually default).
   *   Right-click on **"InteractionCanvas"** -> `UI` -> `Text - TextMeshPro`. Name it **"PromptText"**.
   *   **Adjust "PromptText" Rect Transform:**
        *   `Pos X: 0`, `Pos Y: -150` (or your desired position on screen)
        *   `Width: 300`, `Height: 50`
   *   **Adjust "PromptText" Text (TMP) Component:**
        *   Set `Text` to "Press E to Interact" (this will be overridden by the system).
        *   `Font Size: 24` (or desired size).
        *   `Alignment: Center, Middle`.
        *   `Color: White` (or desired color).
        *   `Enable Word Wrap` (if needed, or `Overflow` for simple text).

**3. Set up the Interaction System Manager:**
   *   Create an Empty GameObject in the Hierarchy, name it **"InteractionSystem"**.
   *   Attach the `InteractionPromptManager.cs` script to the **"InteractionSystem"** GameObject.
   *   Attach the `InteractionPromptUI.cs` script to the **"InteractionCanvas"** GameObject.
   *   In the Inspector, on **"InteractionCanvas"** (where `InteractionPromptUI` is):
        *   Drag the **"PromptText"** (TextMeshProUGUI) into the `Prompt Text` field of the `InteractionPromptUI` component.
   *   In the Inspector, on **"InteractionSystem"** (where `InteractionPromptManager` is):
        *   Drag the **"InteractionCanvas"** GameObject (which has `InteractionPromptUI` script) into the `Prompt UI` field of the `InteractionPromptManager` component.

**4. Prepare the Player (Interactor):**
   *   Select your Player character GameObject (e.g., a `Capsule`, `FPSController`, etc.). Ensure it has a Collider and a Rigidbody/CharacterController if it's moving.
   *   Attach the `Interactor.cs` script to your Player GameObject.
   *   **Configure `Interactor` settings in the Inspector:**
        *   `Interaction Range`, `Interaction Sphere Radius`, `Interact Key` (default `E` is fine).
        *   **`Interactable Layer`**:
            *   Go to `Edit` -> `Project Settings` -> `Tags and Layers`.
            *   Click on "Layers" dropdown and add a new User Layer (e.g., "Interactable").
            *   Select this newly created "Interactable" layer in the `Interactable Layer` dropdown on your `Interactor` component.
   *   **Important:** Ensure your player's camera (if it's a first-person or third-person camera that determines interaction direction) is tagged as **"MainCamera"**. You can set this in the Inspector of your Camera GameObject.

**5. Create Example Interactable Objects:**

   *   **Door Example:**
        *   Create an Empty GameObject, name it **"Door_Parent"**. Position this where the door's hinge should be.
        *   Create a 3D Cube as a child of **"Door_Parent"**, name it **"Door_Mesh"**.
        *   Scale and position **"Door_Mesh"** relative to **"Door_Parent"** so it looks like a door rotating around the parent's pivot. (e.g., if parent is at (0,0,0), child mesh could be at (0.5,0,0) with scale (1,2,0.1)).
        *   Add a `Box Collider` to **"Door_Mesh"** (if it doesn't have one). Ensure `Is Trigger` is **OFF**.
        *   Set the **"Door_Mesh"** GameObject's `Layer` to the **"Interactable"** layer you created earlier.
        *   Attach the `DoorInteractable.cs` script to the **"Door_Parent"** GameObject (or "Door_Mesh" if you prefer to rotate the mesh directly and adjust open/closed rotations). *For this script, it's simplest if `DoorInteractable` is on the actual mesh that rotates.* So, attach it to **"Door_Mesh"**.
        *   Adjust `DoorInteractable` settings: `Open Prompt`, `Close Prompt`, `Open Rotation` (e.g., `Y:90`), `Closed Rotation` (e.g., `Y:0`), `Rotation Speed`, `Is Locked`, `Locked Message`.

   *   **Pickup Item Example:**
        *   Create a 3D Cube GameObject, name it **"PickupItem"**.
        *   Add a `Box Collider` to it. Ensure `Is Trigger` is **OFF**.
        *   Set the **"PickupItem"** GameObject's `Layer` to the **"Interactable"** layer.
        *   Attach the `PickupInteractable.cs` script to the **"PickupItem"** GameObject.
        *   Adjust `PickupInteractable` settings: `Pickup Prompt`, `Item Name`.

**6. Run the Scene:**
   *   Place your Player character and the interactable objects in the scene so they are reachable.
   *   Run the game. When your player looks at or approaches an interactable object, the prompt should appear on the UI.
   *   Press 'E' (or your assigned interact key) to interact. Observe the door opening/closing and the item being picked up (and destroyed).

---

This complete setup provides a fully functional and extensible Interaction Prompt System using the described design pattern. You can easily create new types of interactable objects by simply implementing the `IInteractable` interface.