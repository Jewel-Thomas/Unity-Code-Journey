// Unity Design Pattern Example: InteractionSystem
// This script demonstrates the InteractionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Interaction System design pattern**. It provides a flexible and extensible way for a player (or any `Interactor`) to interact with various objects in the game world.

**Key Components of the Interaction System Pattern:**

1.  **`IInteractable` Interface:** Defines the contract for any object that can be interacted with. This promotes polymorphism, allowing the `Interactor` to treat all interactable objects uniformly.
2.  **`Interactor` Class:** The active entity (e.g., player, AI) that detects and initiates interactions. It typically uses raycasts or trigger colliders to find `IInteractable` objects and then calls their `Interact()` method.
3.  **Concrete `Interactable` Implementations:** Specific game objects (like doors, items, NPCs) that implement the `IInteractable` interface and define their unique interaction logic.
4.  **`InteractionSystemUI` (Optional but Practical):** A component that listens to the `Interactor`'s events and provides visual feedback to the player, such as displaying an interaction prompt.

---

### How to Use This Example:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `InteractionSystem.cs`.
2.  **Copy & Paste:** Copy the entire code block below and paste it into `InteractionSystem.cs`.
3.  **Setup the Player/Camera:**
    *   Create an empty GameObject (e.g., "PlayerInteractionPoint") or use your existing player camera.
    *   Attach the `Interactor` script to this GameObject.
    *   Configure its `Interaction Distance`, `Interactable Layer Mask`, and `Interaction Key` in the Inspector.
    *   For the `Raycast Origin` field, drag the GameObject where the raycast should originate from (e.g., your camera or an "eye" bone).
4.  **Create Interactable Objects:**
    *   Create some 3D objects in your scene (e.g., a Cube for a door, a Sphere for an item).
    *   Add a `Collider` component to each (e.g., Box Collider, Sphere Collider). Make sure they are *not* set to "Is Trigger" unless you are using a trigger-based detection system (this example uses raycasting, so regular colliders are fine).
    *   Assign them to the `Interactable` Layer (or whatever layer you specified in the `Interactor`'s `Interactable Layer Mask`).
    *   Attach one of the example `Interactable` scripts (`DoorInteractable`, `ItemPickupInteractable`, `DialogueInteractable`) to these objects.
    *   Configure their specific properties in the Inspector. For `DoorInteractable`, you'll need to drag the "door" transform, and optionally set up open/closed positions/rotations.
5.  **Setup UI (Optional but Recommended):**
    *   Create a Canvas (if you don't have one).
    *   Inside the Canvas, create a UI Text element (TextMeshPro Text is generally preferred, but this example uses `UnityEngine.UI.Text` for simplicity). Name it something like "InteractionPromptText".
    *   Create an empty GameObject in your Canvas (e.g., "InteractionUIRoot") and attach the `InteractionSystemUI` script to it.
    *   Drag your "InteractionPromptText" UI element into the `Prompt Text` field of the `InteractionSystemUI` component in the Inspector.

Now, when you run the game, moving your player/camera will allow the `Interactor` to detect and interact with the configured objects!

---

```csharp
using UnityEngine;
using System; // For Action delegate
using System.Collections.Generic; // For List
using UnityEngine.UI; // For UI.Text (optional, but good for prompt display)

// This script contains a complete InteractionSystem pattern implementation for Unity.
// It includes an IInteractable interface, an Interactor component, example Interactables,
// and a simple UI component to display interaction prompts.

/// <summary>
/// INTERACTION SYSTEM DESIGN PATTERN
///
/// This pattern separates the concerns of:
/// 1. What an object *can* do (IInteractable)
/// 2. What an entity *does* to interact (Interactor)
/// 3. Specific implementation of an interaction (Concrete Interactables)
/// 4. Visual feedback for interaction (InteractionSystemUI)
///
/// Benefits:
/// - **Decoupling:** The Interactor doesn't need to know the specific type of object it's interacting with,
///   only that it implements IInteractable.
/// - **Extensibility:** Easily add new types of interactable objects by simply implementing the IInteractable interface.
/// - **Maintainability:** Changes to how interactions are detected or displayed don't require changes to individual interactable objects.
/// - **Flexibility:** Multiple Interactors can exist (player, AI), and multiple types of Interactables.
/// </summary>

#region 1. IInteractable Interface

/// <summary>
/// Interface for any object that can be interacted with in the game world.
/// All interactable objects must implement this interface.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Performs the interaction logic for this object.
    /// </summary>
    /// <param name="interactor">The Interactor that initiated this interaction.</param>
    void Interact(Interactor interactor);

    /// <summary>
    /// Returns the text to be displayed as an interaction prompt (e.g., "Press E to Open").
    /// </summary>
    /// <returns>The interaction prompt string.</returns>
    string GetInteractionPrompt();

    /// <summary>
    /// Called when the Interactor starts looking at this object.
    /// Use this for visual feedback like highlighting the object.
    /// </summary>
    void OnHighlight();

    /// <summary>
    /// Called when the Interactor stops looking at this object.
    /// Use this to remove visual feedback like de-highlighting.
    /// </summary>
    void OnUnHighlight();
}

#endregion

#region 2. Interactor Component

/// <summary>
/// The Interactor component is responsible for detecting interactable objects
/// and initiating interactions based on player input.
/// It uses raycasting to find IInteractable objects in front of it.
/// </summary>
public class Interactor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The origin point for the raycast. If null, uses this GameObject's transform.")]
    [SerializeField] private Transform raycastOrigin;
    [Tooltip("The direction of the raycast from the origin. (e.g., Camera.main.transform.forward)")]
    [SerializeField] private Vector3 raycastDirection = Vector3.forward;
    [Tooltip("Maximum distance to detect interactable objects.")]
    [SerializeField] private float interactionDistance = 3f;
    [Tooltip("LayerMask to filter which layers contain interactable objects.")]
    [SerializeField] private LayerMask interactableLayerMask;
    [Tooltip("The Unity input key used to trigger an interaction.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    // Internal state tracking
    private IInteractable _currentInteractable = null;
    private GameObject _currentInteractableGameObject = null; // To check if the GameObject itself changed

    // Events for UI and other systems to subscribe to
    public static event Action<IInteractable> OnInteractableFound;
    public static event Action OnInteractableLost;
    public static event Action<IInteractable, Interactor> OnInteractionPerformed;

    private void Awake()
    {
        // If no specific raycast origin is set, use the GameObject this script is attached to.
        if (raycastOrigin == null)
        {
            raycastOrigin = this.transform;
        }
    }

    void Update()
    {
        // Get the actual raycast direction. If raycastOrigin is a camera, use its forward.
        // Otherwise, use the specified raycastDirection relative to the origin's forward.
        Vector3 actualRaycastDirection = raycastOrigin.forward;
        if (raycastDirection != Vector3.forward) // Allow custom directions if not default forward
        {
             actualRaycastDirection = raycastOrigin.TransformDirection(raycastDirection);
        }

        // Perform a raycast to detect interactable objects.
        RaycastHit hit;
        if (Physics.Raycast(raycastOrigin.position, actualRaycastDirection, out hit, interactionDistance, interactableLayerMask))
        {
            // We hit something. Check if it's an IInteractable.
            IInteractable detectedInteractable = hit.collider.GetComponent<IInteractable>();

            if (detectedInteractable != null)
            {
                // We found an interactable object.
                if (_currentInteractable != detectedInteractable)
                {
                    // This is a *new* interactable object we haven't highlighted yet.
                    // First, un-highlight the previously highlighted object (if any).
                    _currentInteractable?.OnUnHighlight();
                    OnInteractableLost?.Invoke(); // Notify UI that previous interactable is lost

                    // Now, set the new current interactable and highlight it.
                    _currentInteractable = detectedInteractable;
                    _currentInteractableGameObject = hit.collider.gameObject;
                    _currentInteractable.OnHighlight();
                    OnInteractableFound?.Invoke(_currentInteractable); // Notify UI about new interactable
                    Debug.Log($"<color=cyan>Interactor:</color> Found interactable: {_currentInteractableGameObject.name}");
                }

                // If the interaction key is pressed, perform the interaction.
                if (Input.GetKeyDown(interactionKey))
                {
                    Debug.Log($"<color=cyan>Interactor:</color> Interacted with: {_currentInteractableGameObject.name}");
                    _currentInteractable.Interact(this);
                    OnInteractionPerformed?.Invoke(_currentInteractable, this); // Notify about interaction
                }
            }
            else
            {
                // We hit something on the interactable layer, but it's not an IInteractable.
                // Or, we hit a different interactable that replaced the previous one.
                ClearCurrentInteractable();
            }
        }
        else
        {
            // Raycast hit nothing, or hit something not on the interactable layer.
            ClearCurrentInteractable();
        }
    }

    /// <summary>
    /// Clears the current interactable, un-highlights it, and notifies subscribers.
    /// </summary>
    private void ClearCurrentInteractable()
    {
        if (_currentInteractable != null)
        {
            // We no longer see the previous interactable. Un-highlight it.
            _currentInteractable.OnUnHighlight();
            OnInteractableLost?.Invoke(); // Notify UI that interactable is lost
            Debug.Log($"<color=cyan>Interactor:</color> Lost interactable: {_currentInteractableGameObject.name}");
            _currentInteractable = null;
            _currentInteractableGameObject = null;
        }
    }

    // Visual aid for the raycast in the editor.
    private void OnDrawGizmosSelected()
    {
        if (raycastOrigin != null)
        {
            Gizmos.color = Color.red;
            Vector3 actualRaycastDirection = raycastOrigin.forward;
            if (raycastDirection != Vector3.forward)
            {
                 actualRaycastDirection = raycastOrigin.TransformDirection(raycastDirection);
            }
            Gizmos.DrawRay(raycastOrigin.position, actualRaycastDirection * interactionDistance);
            Gizmos.DrawSphere(raycastOrigin.position + actualRaycastDirection * interactionDistance, 0.05f);
        }
    }
}

#endregion

#region 3. Example Concrete Interactable Implementations

/// <summary>
/// An example interactable representing a door that can be opened and closed.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [Tooltip("The Transform of the actual door object to animate.")]
    [SerializeField] private Transform doorTransform;
    [Tooltip("The local position of the door when open.")]
    [SerializeField] private Vector3 openPosition;
    [Tooltip("The local position of the door when closed.")]
    [SerializeField] private Vector3 closedPosition;
    [Tooltip("The local rotation of the door when open.")]
    [SerializeField] private Quaternion openRotation;
    [Tooltip("The local rotation of the door when closed.")]
    [SerializeField] private Quaternion closedRotation;
    [Tooltip("Speed at which the door animates.")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private Color highlightColor = Color.yellow;

    private bool _isOpen = false;
    private Material _originalMaterial;
    private Renderer _renderer;

    private void Awake()
    {
        if (doorTransform == null)
        {
            Debug.LogError($"<color=red>DoorInteractable:</color> 'Door Transform' not assigned on {gameObject.name}. Disabling script.", this);
            enabled = false;
            return;
        }
        _renderer = doorTransform.GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material; // Store original material
        }
        else
        {
            Debug.LogWarning($"<color=orange>DoorInteractable:</color> No Renderer found on doorTransform {doorTransform.name}. Highlighting will not work.", this);
        }

        // Initialize door to closed state
        doorTransform.localPosition = closedPosition;
        doorTransform.localRotation = closedRotation;
    }

    public void Interact(Interactor interactor)
    {
        _isOpen = !_isOpen; // Toggle door state
        Debug.Log($"<color=green>DoorInteractable:</color> {gameObject.name} was {_isOpen} by {interactor.name}.");
        // Simple animation using Lerp towards target position/rotation
        StopAllCoroutines(); // Stop any ongoing animation
        StartCoroutine(AnimateDoor());
    }

    public string GetInteractionPrompt()
    {
        return _isOpen ? "Close Door (E)" : "Open Door (E)";
    }

    public void OnHighlight()
    {
        Debug.Log($"<color=green>DoorInteractable:</color> Highlighted {gameObject.name}");
        if (_renderer != null)
        {
            // Create a new material instance for highlighting
            _renderer.material = new Material(_originalMaterial);
            _renderer.material.color = highlightColor;
        }
    }

    public void OnUnHighlight()
    {
        Debug.Log($"<color=green>DoorInteractable:</color> Unhighlighted {gameObject.name}");
        if (_renderer != null)
        {
            _renderer.material = _originalMaterial; // Restore original material
        }
    }

    private System.Collections.IEnumerator AnimateDoor()
    {
        Vector3 targetPos = _isOpen ? openPosition : closedPosition;
        Quaternion targetRot = _isOpen ? openRotation : closedRotation;
        Vector3 startPos = doorTransform.localPosition;
        Quaternion startRot = doorTransform.localRotation;

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * animationSpeed;
            doorTransform.localPosition = Vector3.Lerp(startPos, targetPos, timer);
            doorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, timer);
            yield return null;
        }
        doorTransform.localPosition = targetPos; // Ensure it snaps to final position
        doorTransform.localRotation = targetRot; // Ensure it snaps to final rotation
    }
}

/// <summary>
/// An example interactable representing a pickup item.
/// </summary>
public class ItemPickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [Tooltip("The name of the item that will be displayed when picked up.")]
    [SerializeField] private string itemName = "Generic Item";
    [SerializeField] private Color highlightColor = Color.green;

    private Material _originalMaterial;
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
        else
        {
            Debug.LogWarning($"<color=orange>ItemPickupInteractable:</color> No Renderer found on {gameObject.name}. Highlighting will not work.", this);
        }
    }

    public void Interact(Interactor interactor)
    {
        Debug.Log($"<color=green>ItemPickupInteractable:</color> {interactor.name} picked up {itemName}.");
        // In a real game, you would add the item to an inventory, play a sound, etc.
        Destroy(gameObject); // Remove the item from the scene
    }

    public string GetInteractionPrompt()
    {
        return $"Pick up {itemName} (E)";
    }

    public void OnHighlight()
    {
        Debug.Log($"<color=green>ItemPickupInteractable:</color> Highlighted {itemName}");
        if (_renderer != null)
        {
            _renderer.material = new Material(_originalMaterial);
            _renderer.material.color = highlightColor;
        }
    }

    public void OnUnHighlight()
    {
        Debug.Log($"<color=green>ItemPickupInteractable:</color> Unhighlighted {itemName}");
        if (_renderer != null)
        {
            _renderer.material = _originalMaterial;
        }
    }
}

/// <summary>
/// An example interactable representing an NPC or object for dialogue.
/// </summary>
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue Settings")]
    [Tooltip("An array of dialogue lines that will be displayed.")]
    [SerializeField] private string[] dialogueLines = { "Hello there!", "How are you today?", "Nice weather, isn't it?" };
    [SerializeField] private Color highlightColor = Color.blue;

    private int _currentDialogueIndex = 0;
    private Material _originalMaterial;
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material;
        }
        else
        {
            Debug.LogWarning($"<color=orange>DialogueInteractable:</color> No Renderer found on {gameObject.name}. Highlighting will not work.", this);
        }
    }

    public void Interact(Interactor interactor)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"<color=yellow>DialogueInteractable:</color> No dialogue lines set for {gameObject.name}.");
            return;
        }

        string currentLine = dialogueLines[_currentDialogueIndex];
        Debug.Log($"<color=green>DialogueInteractable:</color> {gameObject.name} says: \"{currentLine}\" (to {interactor.name})");

        // In a real game, this would trigger a UI dialogue box
        // For this example, we'll just cycle through the lines in the console.
        _currentDialogueIndex = (_currentDialogueIndex + 1) % dialogueLines.Length;
    }

    public string GetInteractionPrompt()
    {
        return "Talk (E)";
    }

    public void OnHighlight()
    {
        Debug.Log($"<color=green>DialogueInteractable:</color> Highlighted {gameObject.name}");
        if (_renderer != null)
        {
            _renderer.material = new Material(_originalMaterial);
            _renderer.material.color = highlightColor;
        }
    }

    public void OnUnHighlight()
    {
        Debug.Log($"<color=green>DialogueInteractable:</color> Unhighlighted {gameObject.name}");
        if (_renderer != null)
        {
            _renderer.material = _originalMaterial;
        }
    }
}

#endregion

#region 4. InteractionSystemUI (Optional but Recommended)

/// <summary>
/// This component listens to events from the Interactor and updates a UI Text element
/// to show the interaction prompt.
/// </summary>
public class InteractionSystemUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Text component used to display interaction prompts.")]
    [SerializeField] private Text promptText; // Or TextMeshProUGUI if using TMPro

    private void Awake()
    {
        if (promptText == null)
        {
            Debug.LogError($"<color=red>InteractionSystemUI:</color> Prompt Text not assigned. Disabling script.", this);
            enabled = false;
            return;
        }
        // Initially hide the prompt
        promptText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Subscribe to Interactor events
        Interactor.OnInteractableFound += ShowPrompt;
        Interactor.OnInteractableLost += HidePrompt;
    }

    private void OnDisable()
    {
        // Unsubscribe from Interactor events to prevent memory leaks
        Interactor.OnInteractableFound -= ShowPrompt;
        Interactor.OnInteractableLost -= HidePrompt;
    }

    /// <summary>
    /// Displays the interaction prompt using the text from the IInteractable.
    /// </summary>
    /// <param name="interactable">The interactable object currently in focus.</param>
    private void ShowPrompt(IInteractable interactable)
    {
        promptText.text = interactable.GetInteractionPrompt();
        promptText.gameObject.SetActive(true);
        Debug.Log($"<color=magenta>InteractionSystemUI:</color> Showing prompt: {interactable.GetInteractionPrompt()}");
    }

    /// <summary>
    /// Hides the interaction prompt.
    /// </summary>
    private void HidePrompt()
    {
        promptText.gameObject.SetActive(false);
        Debug.Log($"<color=magenta>InteractionSystemUI:</color> Hiding prompt.");
    }
}

#endregion
```