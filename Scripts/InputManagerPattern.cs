// Unity Design Pattern Example: InputManagerPattern
// This script demonstrates the InputManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'InputManagerPattern' is a design pattern used in game development to centralize and abstract input handling. Instead of individual game objects directly polling Unity's `Input` class (e.g., `Input.GetKeyDown(KeyCode.Space)`), they subscribe to events or query an `InputManager` for high-level actions (e.g., "Jump Pressed", "Move Input").

**Benefits of the InputManagerPattern:**

1.  **Decoupling:** Game logic is decoupled from specific input keys. A `PlayerController` doesn't care *how* a jump is triggered, only that it *is* triggered.
2.  **Reusability:** The input mapping logic is in one place, making it easy to reuse across multiple characters or even different games.
3.  **Configurability:** Key bindings can be easily changed in one central location (e.g., through a Unity Inspector or even dynamically through an in-game options menu) without altering game logic.
4.  **Flexibility:** Easily extendable to support different input devices (keyboard, mouse, gamepad, touch) or multiple control schemes.
5.  **Testability:** Input can be simulated more easily for testing purposes.
6.  **Maintainability:** Easier to manage and debug input-related issues.

This example provides a robust and practical `InputManager` for Unity using a **Singleton pattern** for global access and **C# events** for notifying listeners.

---

### **InputManager.cs**

To use this script:
1.  Create an empty GameObject in your Unity scene (e.g., named "InputManager").
2.  Attach this `InputManager.cs` script to it.
3.  Configure the `Key Bindings` in the Inspector for your desired actions.
4.  Ensure this GameObject persists across scenes if you want the InputManager to be always available (using `DontDestroyOnLoad` as implemented).

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
///     The InputManagerPattern is a design pattern that centralizes and abstracts input handling
///     in game development. Instead of individual game objects directly polling Unity's Input class,
///     they subscribe to events or query an InputManager for high-level actions (e.g., "Jump Pressed", "Move Input").
///
///     This script implements the InputManagerPattern using:
///     1.  A **Singleton Pattern**: Ensures only one instance of the InputManager exists and provides global access.
///     2.  **C# Events**: Decouples the input source from the game logic. Game objects subscribe to these events
///         to react to specific actions, rather than checking raw key presses.
///     3.  **Configurable Key Bindings**: Allows easy modification of input keys via the Unity Inspector.
/// </summary>
public class InputManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // A static reference to the InputManager instance, making it globally accessible.
    // 'private set' ensures it can only be set internally by the InputManager itself.
    public static InputManager Instance { get; private set; }

    // --- Action Events ---
    // These events provide a high-level abstraction for game actions.
    // Game objects subscribe to these events to react to input without knowing the specific key bindings.

    [Header("Discrete Action Events (Button Presses)")]
    // Event triggered when the 'Jump' key is pressed down.
    public event Action OnJumpPressed;
    // Event triggered when the 'Jump' key is released.
    public event Action OnJumpReleased; // Example for a "key up" event

    // Event triggered when the 'Interact' key is pressed down.
    public event Action OnInteractPressed;

    // Event triggered when the 'Attack' key is pressed down.
    public event Action OnAttackPressed;

    // Event triggered when the 'Pause' key is pressed down.
    public event Action OnPausePressed;


    [Header("Continuous Action Events (Axis Input)")]
    // Event triggered every frame, providing the current 2D movement input.
    // The Vector2 represents (Horizontal, Vertical) input, usually normalized.
    public event Action<Vector2> OnMoveInput;

    // Event triggered every frame, providing the current 2D look input (e.g., mouse delta).
    // The Vector2 represents (Mouse X, Mouse Y) or (Gamepad Right Stick X, Y).
    public event Action<Vector2> OnLookInput;


    // --- Configurable Key Bindings ---
    // These fields allow developers to assign specific KeyCodes to game actions
    // directly from the Unity Inspector, making them easily remappable.
    [Header("Key Bindings")]
    [Tooltip("The KeyCode used for the 'Jump' action.")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [Tooltip("The KeyCode used for the 'Interact' action.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("The KeyCode used for the 'Attack' action.")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // Mouse Left Click
    [Tooltip("The KeyCode used for the 'Pause' action.")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;


    // --- Internal State (Optional: for polling current state, though events are preferred for the pattern) ---
    // Storing the current state of continuous inputs.
    private Vector2 currentMoveInput;
    private Vector2 currentLookInput;


    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where the Singleton pattern is initialized.
    /// </summary>
    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy this duplicate.
        // This ensures there's only one InputManager active in the scene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // Set this instance as the singleton.
            Instance = this;
            // Make this GameObject persist across scene changes.
            // This is crucial for a global manager that should always be available.
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Called every frame. Used to poll Unity's raw input and trigger our custom events.
    /// </summary>
    private void Update()
    {
        PollDiscreteInput();
        PollContinuousInput();
    }

    /// <summary>
    /// Checks for discrete (button-like) input actions (e.g., key down, key up)
    /// and invokes the corresponding events.
    /// </summary>
    private void PollDiscreteInput()
    {
        // Jump Action
        if (Input.GetKeyDown(jumpKey))
        {
            // The '?.Invoke()' syntax ensures the event is only invoked if there are subscribers.
            OnJumpPressed?.Invoke();
        }
        if (Input.GetKeyUp(jumpKey))
        {
            OnJumpReleased?.Invoke();
        }

        // Interact Action
        if (Input.GetKeyDown(interactKey))
        {
            OnInteractPressed?.Invoke();
        }

        // Attack Action
        if (Input.GetKeyDown(attackKey))
        {
            OnAttackPressed?.Invoke();
        }

        // Pause Action
        if (Input.GetKeyDown(pauseKey))
        {
            OnPausePressed?.Invoke();
        }
    }

    /// <summary>
    /// Checks for continuous (axis-like) input actions (e.g., movement, looking)
    /// and invokes the corresponding events.
    /// </summary>
    private void PollContinuousInput()
    {
        // Movement Input (WASD or Arrow Keys, Gamepad Left Stick)
        // Input.GetAxisRaw returns -1, 0, or 1, suitable for direct movement.
        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float verticalMove = Input.GetAxisRaw("Vertical");
        currentMoveInput = new Vector2(horizontalMove, verticalMove).normalized; // Normalize for consistent speed in all directions
        
        // Invoke OnMoveInput event, even if input is (0,0), so listeners know when movement stops.
        OnMoveInput?.Invoke(currentMoveInput);

        // Look Input (Mouse Delta or Gamepad Right Stick)
        // Input.GetAxis for "Mouse X" and "Mouse Y" provides delta movement.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        currentLookInput = new Vector2(mouseX, mouseY);

        // Only invoke OnLookInput if there's actual mouse/look movement.
        // This prevents unnecessary event calls if the mouse isn't moving.
        if (mouseX != 0 || mouseY != 0)
        {
            OnLookInput?.Invoke(currentLookInput);
        }
    }

    // --- Public Getters for current state (Optional: for polling if events are not suitable) ---
    // While event-driven is preferred for the InputManagerPattern, sometimes direct polling
    // of the current state is useful (e.g., for physics calculations in FixedUpdate).
    public Vector2 GetCurrentMoveInput() => currentMoveInput;
    public Vector2 GetCurrentLookInput() => currentLookInput;
    public bool IsJumpKeyHeld() => Input.GetKey(jumpKey);
    public bool IsInteractKeyHeld() => Input.GetKey(interactKey);
}


/*
/// <summary>
/// EXAMPLE USAGE: PlayerController.cs
///
/// This script demonstrates how a game object (e.g., a player character)
/// would subscribe to and react to events published by the InputManager.
/// </summary>
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float rotationSpeed = 200f; // For camera/player rotation

    private Rigidbody rb;
    private bool isGrounded = true; // Simplified ground check for demonstration

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component.");
            enabled = false; // Disable script if no Rigidbody
        }
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// This is the ideal place to subscribe to InputManager events.
    /// </summary>
    void OnEnable()
    {
        // Ensure InputManager instance exists before subscribing.
        // This is important if this script enables before InputManager's Awake runs
        // (though DontDestroyOnLoad helps manage this across scenes).
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpPressed += HandleJump;
            InputManager.Instance.OnMoveInput += HandleMove;
            InputManager.Instance.OnLookInput += HandleLook;
            InputManager.Instance.OnInteractPressed += HandleInteract;
            InputManager.Instance.OnPausePressed += HandlePause;
            Debug.Log("PlayerController subscribed to InputManager events.");
        }
        else
        {
            Debug.LogError("InputManager not found! Ensure it's in the scene and set up correctly.");
        }
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// It is CRUCIAL to unsubscribe from events here to prevent memory leaks.
    /// If not unsubscribed, the InputManager would hold a reference to this object,
    /// preventing it from being garbage collected, even if it's destroyed.
    /// </summary>
    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnJumpPressed -= HandleJump;
            InputManager.Instance.OnMoveInput -= HandleMove;
            InputManager.Instance.OnLookInput -= HandleLook;
            InputManager.Instance.OnInteractPressed -= HandleInteract;
            InputManager.Instance.OnPausePressed -= HandlePause;
            Debug.Log("PlayerController unsubscribed from InputManager events.");
        }
    }

    /// <summary>
    /// Event handler for the Jump action.
    /// </summary>
    private void HandleJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // Simplified ground check
            Debug.Log("Player Jumped!");
        }
    }

    /// <summary>
    /// Event handler for continuous movement input.
    /// </summary>
    /// <param name="moveInput">A normalized Vector2 representing horizontal and vertical movement.</param>
    private void HandleMove(Vector2 moveInput)
    {
        // Assuming character moves relative to its forward direction or world axes.
        // For a 3D game, you might want to consider camera's forward direction.
        Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
        movement.y = 0; // Keep movement purely horizontal for typical character controllers
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Event handler for continuous look input (e.g., mouse delta).
    /// </summary>
    /// <param name="lookInput">A Vector2 representing horizontal (X) and vertical (Y) look delta.</param>
    private void HandleLook(Vector2 lookInput)
    {
        // Rotate the player horizontally (yaw)
        transform.Rotate(Vector3.up * lookInput.x * rotationSpeed * Time.deltaTime);

        // For vertical look (pitch), you'd typically rotate the camera or a camera child object,
        // clamped to avoid over-rotation.
        // Example for a camera: Camera.main.transform.Rotate(Vector3.left * lookInput.y * rotationSpeed * Time.deltaTime);
        Debug.Log($"Player Look Input: X={lookInput.x}, Y={lookInput.y}");
    }

    /// <summary>
    /// Event handler for the Interact action.
    /// </summary>
    private void HandleInteract()
    {
        Debug.Log("Player interacted with something!");
        // Add interaction logic here (e.g., check for nearby interactable objects)
    }

    /// <summary>
    /// Event handler for the Pause action.
    /// </summary>
    private void HandlePause()
    {
        Debug.Log("Game Paused/Unpaused!");
        // Toggle pause state, show/hide pause menu, etc.
        // Example: Time.timeScale = Time.timeScale == 0 ? 1 : 0;
    }

    // --- Simplified Ground Check (for demonstration purposes) ---
    void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with a "Ground" tagged object
        // You would typically use a more robust ground detection system.
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}

*/
```