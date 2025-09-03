// Unity Design Pattern Example: InputBufferSystem
// This script demonstrates the InputBufferSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a comprehensive and practical example of the **Input Buffer System** design pattern. It decouples input reading from command execution, enabling features like frame-independent input processing, replay systems, and predictable physics behavior.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and Queue

/// <summary>
/// This script demonstrates the Input Buffer System design pattern in Unity.
///
/// The Input Buffer System is used to:
/// 1. Decouple input reading (often in Update) from command execution (often in FixedUpdate).
/// 2. Ensure consistent command processing regardless of varying frame rates.
/// 3. Facilitate features like network prediction, rollback, replay systems, and input queuing.
///
/// The pattern consists of:
/// -   **IPlayerCommand**: An interface defining actions (commands).
/// -   **Concrete Commands (MoveCommand, JumpCommand, IdleCommand)**: Implementations of IPlayerCommand, encapsulating specific actions and their parameters.
/// -   **InputBuffer**: A data structure (like a Queue) that stores IPlayerCommand objects as they are generated.
/// -   **InputReader**: A component that polls raw input (e.g., keyboard, mouse) and translates it into IPlayerCommand objects, adding them to the InputBuffer. This typically runs in `Update`.
/// -   **PlayerController (Command Processor)**: A component that retrieves commands from the InputBuffer and executes them, often in `FixedUpdate` for physics-based games.
///
/// --- How to Use This Script in Unity ---
///
/// 1.  **Create a New C# Script:**
///     *   In your Unity project, go to `Assets -> Create -> C# Script`.
///     *   Name it `InputBufferSystemExample`.
///     *   Copy and paste the entire code below into this new script file, replacing its default content.
///
/// 2.  **Prepare the Scene:**
///     *   **Player Object:** Create a 3D object to represent your player (e.g., `GameObject -> 3D Object -> Cube`).
///         *   Rename it to `Player`.
///         *   Position it at `(0, 1, 0)` so it's slightly above the ground.
///         *   Add a `Rigidbody` component to it (`Add Component -> Physics -> Rigidbody`).
///             *   Ensure "Use Gravity" is checked.
///             *   Set "Drag" to `1` or `2` for more natural stopping.
///     *   **Ground Object:** Create another 3D object for the ground (e.g., `GameObject -> 3D Object -> Plane`).
///         *   Rename it to `Ground`.
///         *   Position it at `(0, 0, 0)`.
///         *   Scale it up (e.g., `Scale: X:10, Y:1, Z:10`) so the player has room to move.
///         *   Ensure its layer is "Default" or set it to a specific "Ground" layer (e.g., create a "Ground" layer in `Layers -> Edit Layers...`, assign it to the Plane, and then update `PlayerController._groundLayer` accordingly).
///
/// 3.  **Attach the Script:**
///     *   Select your `Player` GameObject in the Hierarchy.
///     *   Drag and drop the `InputBufferSystemExample` script from your Project window onto the `Player` GameObject in the Inspector.
///     *   Alternatively, click `Add Component` in the Inspector and search for `PlayerController` (the main Monobehaviour in the script).
///
/// 4.  **Configure in Inspector (Optional):**
///     *   On the `Player` GameObject, you'll see the `PlayerController` component.
///     *   The `InputReader` component will be automatically added if not present and linked.
///     *   You can adjust `Move Speed` and `Jump Force` directly on the `InputReader` component.
///
/// 5.  **Run the Scene:**
///     *   Press the `Play` button in the Unity Editor.
///     *   Use `W, A, S, D` keys to move the player cube.
///     *   Use the `Spacebar` to make the player cube jump.
/// </summary>

// --- 1. Command Interface ---
// Represents an abstract action that can be performed by the player.
// This interface defines the contract for all concrete commands.
public interface IPlayerCommand
{
    /// <summary>
    /// Executes the specific action encapsulated by this command.
    /// </summary>
    /// <param name="player">The PlayerController instance on which to execute the command.</param>
    void Execute(PlayerController player);
}

// --- 2. Concrete Command Implementations ---
// These classes implement IPlayerCommand and encapsulate the data
// and logic for specific player actions.

/// <summary>
/// A concrete command for moving the player character.
/// It holds the direction and speed for the movement.
/// </summary>
public class MoveCommand : IPlayerCommand
{
    private Vector3 _direction;
    private float _speed;

    /// <summary>
    /// Initializes a new instance of the MoveCommand.
    /// </summary>
    /// <param name="direction">The normalized direction vector for movement (e.g., Vector3.forward).</param>
    /// <param name="speed">The speed at which the player should move.</param>
    public MoveCommand(Vector3 direction, float speed)
    {
        _direction = direction;
        _speed = speed;
    }

    /// <summary>
    /// Executes the move action on the given PlayerController.
    /// </summary>
    /// <param name="player">The PlayerController target.</param>
    public void Execute(PlayerController player)
    {
        player.Move(_direction, _speed);
    }

    public override string ToString()
    {
        return $"MoveCommand(Dir:{_direction}, Speed:{_speed})";
    }
}

/// <summary>
/// A concrete command for making the player character jump.
/// It holds the force to be applied for the jump.
/// </summary>
public class JumpCommand : IPlayerCommand
{
    private float _jumpForce;

    /// <summary>
    /// Initializes a new instance of the JumpCommand.
    /// </summary>
    /// <param name="jumpForce">The force to apply upwards for the jump.</param>
    public JumpCommand(float jumpForce)
    {
        _jumpForce = jumpForce;
    }

    /// <summary>
    /// Executes the jump action on the given PlayerController.
    /// </summary>
    /// <param name="player">The PlayerController target.</param>
    public void Execute(PlayerController player)
    {
        player.Jump(_jumpForce);
    }

    public override string ToString()
    {
        return $"JumpCommand(Force:{_jumpForce})";
    }
}

/// <summary>
/// A "null" or no-operation command.
/// Useful in scenarios where every game tick/frame requires a command,
/// even if no active input is detected (e.g., for networked games, replay systems).
/// For simple local control, it might be omitted.
/// </summary>
public class IdleCommand : IPlayerCommand
{
    /// <summary>
    /// Executes no action. The player's existing physics (like drag) will handle natural deceleration.
    /// </summary>
    /// <param name="player">The PlayerController target.</param>
    public void Execute(PlayerController player)
    {
        // Do nothing. The player's existing physics will handle deceleration.
    }

    public override string ToString()
    {
        return "IdleCommand";
    }
}

// --- 3. Input Buffer ---
/// <summary>
/// The central buffer responsible for storing a sequence of IPlayerCommand objects.
/// This class acts as a queue, decoupling when input is read from when it is processed.
/// </summary>
public class InputBuffer
{
    // A Queue is ideal for FIFO (First-In, First-Out) processing,
    // which ensures commands are executed in the order they were generated.
    private Queue<IPlayerCommand> _commands = new Queue<IPlayerCommand>();

    /// <summary>
    /// Adds a new command to the end of the buffer.
    /// </summary>
    /// <param name="command">The IPlayerCommand to enqueue.</param>
    public void AddCommand(IPlayerCommand command)
    {
        if (command != null)
        {
            _commands.Enqueue(command);
        }
    }

    /// <summary>
    /// Retrieves and removes all commands currently in the buffer.
    /// This method is typically called by the command processor (e.g., PlayerController)
    /// at the beginning of its processing cycle (e.g., in FixedUpdate).
    /// </summary>
    /// <returns>A List of IPlayerCommand objects that were in the buffer.</returns>
    public List<IPlayerCommand> DequeueAllCommands()
    {
        List<IPlayerCommand> currentCommands = new List<IPlayerCommand>();
        while (_commands.Count > 0)
        {
            currentCommands.Add(_commands.Dequeue());
        }
        return currentCommands;
    }

    /// <summary>
    /// Clears all commands from the buffer.
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
    }

    /// <summary>
    /// Gets the current number of commands waiting in the buffer.
    /// </summary>
    public int Count => _commands.Count;
}

// --- 4. Input Reader ---
/// <summary>
/// This MonoBehaviour component is responsible for reading raw user input from Unity's Input system
/// and translating it into IPlayerCommand objects. It then adds these commands to the shared InputBuffer.
/// This typically runs in Unity's `Update` loop, which may vary in frequency.
/// </summary>
public class InputReader : MonoBehaviour
{
    [Tooltip("Movement speed for the player.")]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("Force applied when the player jumps.")]
    [SerializeField] private float _jumpForce = 7f;

    // A reference to the InputBuffer this reader will populate.
    // This is set by the PlayerController at Awake time.
    private InputBuffer _inputBuffer;

    /// <summary>
    /// Sets the InputBuffer instance that this reader will use.
    /// </summary>
    /// <param name="buffer">The InputBuffer instance.</param>
    public void SetInputBuffer(InputBuffer buffer)
    {
        _inputBuffer = buffer;
    }

    /// <summary>
    /// Called once per frame. Reads input and generates commands.
    /// </summary>
    void Update()
    {
        if (_inputBuffer == null)
        {
            Debug.LogError("InputBuffer is not assigned to InputReader.", this);
            return;
        }

        // --- Read Movement Input (W, A, S, D or Arrow Keys) ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // If there's significant movement input, create and buffer a MoveCommand.
        if (moveDirection.magnitude > 0.1f)
        {
            _inputBuffer.AddCommand(new MoveCommand(moveDirection, _moveSpeed));
        }
        // else
        // {
        //     // Optional: If no movement input, you might buffer an IdleCommand.
        //     // This is more common in network prediction/replay where every tick needs a command.
        //     // For simple local control with physics, it's often not needed as physics handles deceleration.
        //     // _inputBuffer.AddCommand(new IdleCommand());
        // }

        // --- Read Jump Input (Spacebar) ---
        if (Input.GetButtonDown("Jump"))
        {
            _inputBuffer.AddCommand(new JumpCommand(_jumpForce));
        }

        // --- Example: Additional commands (uncomment to test) ---
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     Debug.Log("Buffered Interact Command!");
        //     // _inputBuffer.AddCommand(new InteractCommand(some_target, some_param));
        // }
    }
}

// --- 5. Player Controller (Command Processor/Executor) ---
/// <summary>
/// This MonoBehaviour component acts as the command processor. It is the target
/// for the commands stored in the InputBuffer. It retrieves and executes these commands.
/// For physics-based actions, this processing typically happens in Unity's `FixedUpdate`
/// to ensure deterministic and consistent behavior regardless of frame rate.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // PlayerController needs a Rigidbody for physics
public class PlayerController : MonoBehaviour
{
    // The InputReader instance that provides input commands.
    // [SerializeField] allows it to be assigned in the Inspector, but also handled programmatically.
    [Tooltip("The InputReader component that provides input to this player.")]
    [SerializeField] private InputReader _inputReader;

    // Reference to the Rigidbody component for physics operations.
    private Rigidbody _rb;

    // The dedicated InputBuffer instance for this player.
    // It's private as only this controller should directly manage its own buffer.
    private InputBuffer _inputBuffer = new InputBuffer();

    // Player state variables for ground checking
    private bool _isGrounded;
    [Tooltip("Distance below the player to check for ground.")]
    [SerializeField] private float _groundCheckDistance = 0.6f;
    [Tooltip("LayerMask for objects considered 'ground'.")]
    [SerializeField] private LayerMask _groundLayer;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes components and sets up the InputReader.
    /// </summary>
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("Rigidbody component not found on PlayerController! Please add a Rigidbody.", this);
            enabled = false; // Disable this script if Rigidbody is missing
            return;
        }

        // If an InputReader isn't assigned, try to get one, or add one.
        if (_inputReader == null)
        {
            _inputReader = GetComponent<InputReader>();
            if (_inputReader == null)
            {
                _inputReader = gameObject.AddComponent<InputReader>();
                Debug.LogWarning("InputReader not found on Player, adding a new one. Consider assigning it manually for clarity.", this);
            }
        }

        // Important: Link the InputReader to *this* PlayerController's InputBuffer.
        _inputReader.SetInputBuffer(_inputBuffer);

        // Set default ground layer if not explicitly set in Inspector
        if (_groundLayer.value == 0) // LayerMask value 0 means "Nothing"
        {
            _groundLayer = LayerMask.GetMask("Default"); // Assume "Default" layer for ground
            Debug.LogWarning($"Ground Layer for {gameObject.name} not set. Defaulting to 'Default' layer.", this);
        }
    }

    /// <summary>
    /// Called once per frame. Used for visual updates and non-physics checks.
    /// </summary>
    void Update()
    {
        // Perform ground check in Update. This gives more immediate visual feedback.
        // Physics.Raycast returns true if it hits something.
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);
        // Optional: Draw a debug ray to visualize the ground check.
        Debug.DrawRay(transform.position, Vector3.down * _groundCheckDistance, _isGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// Called at a fixed time interval. This is where physics calculations and command execution
    /// for physics-based actions should occur to ensure consistency.
    /// </summary>
    void FixedUpdate()
    {
        // Dequeue and process all commands that have accumulated in the buffer
        // since the last FixedUpdate. This ensures all buffered input is processed
        // for the current physics tick.
        List<IPlayerCommand> commandsToProcess = _inputBuffer.DequeueAllCommands();

        // Iterate through all commands received for this physics tick and execute them.
        foreach (var command in commandsToProcess)
        {
            // Debug.Log($"Processing: {command.ToString()} in FixedUpdate", this); // Uncomment to see commands being processed
            command.Execute(this);
        }

        // Optional: If no commands were processed, you might apply an IdleCommand
        // to explicitly manage player state even with no input. For this example,
        // we rely on Rigidbody drag to naturally slow the player.
    }

    /// <summary>
    /// Moves the player character using Rigidbody.AddForce.
    /// This method is called by the MoveCommand's Execute method.
    /// </summary>
    /// <param name="direction">The normalized direction vector (e.g., Vector3.forward).</param>
    /// <param name="speed">The speed at which to move.</param>
    public void Move(Vector3 direction, float speed)
    {
        // Apply force for movement. Using ForceMode.Acceleration to directly affect acceleration.
        // Multiply by Time.fixedDeltaTime to make it framerate independent (though FixedUpdate already helps).
        // A multiplier (e.g., 100f) is often needed to make Rigidbody forces noticeable.
        _rb.AddForce(direction * speed * _rb.mass * Time.fixedDeltaTime * 100f, ForceMode.Acceleration);

        // Limit the horizontal velocity to prevent the player from accelerating indefinitely.
        Vector3 flatVelocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        if (flatVelocity.magnitude > speed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * speed;
            _rb.velocity = new Vector3(limitedVelocity.x, _rb.velocity.y, _rb.velocity.z);
        }
    }

    /// <summary>
    /// Makes the player character jump using Rigidbody.AddForce.
    /// This method is called by the JumpCommand's Execute method.
    /// Only allows jumping if the player is currently grounded.
    /// </summary>
    /// <param name="force">The upward force to apply for the jump.</param>
    public void Jump(float force)
    {
        if (_isGrounded)
        {
            // Reset vertical velocity to ensure consistent jump height regardless of falling speed.
            _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            _rb.AddForce(Vector3.up * force, ForceMode.Impulse); // Impulse for an immediate burst of force
            _isGrounded = false; // Temporarily set to false to prevent multiple jumps mid-air
        }
    }
}
```