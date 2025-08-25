// Unity Design Pattern Example: Command
// This script demonstrates the Command pattern in Unity
// Generated automatically - ready to use in your Unity project

The Command design pattern is a behavioral pattern that turns a request into a stand-alone object. This object contains all information about the request. This transformation lets you parameterize methods with different requests, delay or queue a request's execution, and support undoable operations.

In Unity, this pattern is particularly useful for:
*   **Undo/Redo systems:** Each action is a command that can be stored and reversed.
*   **Macro recording:** A sequence of commands can be played back.
*   **Asynchronous operations/Queues:** Commands can be added to a queue and executed later.
*   **Networking:** Commands can be serialized and sent over a network to be executed on other clients or servers.
*   **AI behavior trees:** Different actions can be represented as commands.

---

### Command Design Pattern Example in Unity: Player Actions with Undo/Redo

This example demonstrates how to implement a Command pattern for a player character's movement and jumping actions, including the ability to undo and redo those actions.

**How to set up this example in Unity:**

1.  **Create a C# Script:** Create a new C# script named `CommandPatternExample` (or any other name, but ensure the filename matches the class containing `MonoBehaviour` classes if you copy the whole code into one file, or just name it `PlayerController` and ensure all the other classes are in it). Copy and paste the entire code below into this script.
2.  **Create an "Invoker" GameObject:**
    *   Create an empty GameObject in your scene (e.g., `GameObject -> Create Empty`).
    *   Rename it to `CommandManager`.
    *   Add the `CommandProcessor` component to it (`Add Component -> CommandProcessor`).
3.  **Create a "Receiver" GameObject (Player Character):**
    *   Create a 3D Cube or Sphere (e.g., `GameObject -> 3D Object -> Cube`).
    *   Rename it to `Player`.
    *   Add the `PlayerCharacter` component to it (`Add Component -> PlayerCharacter`).
    *   **Crucially, add a `Rigidbody` component to the `Player` GameObject** (`Add Component -> Rigidbody`). This is required for `PlayerCharacter` to jump. You might want to disable `Use Gravity` on the Rigidbody if you only want `JumpCommand` to apply a single upward force and not have the player fall, or set `Freeze Rotation` for X, Y, Z in Rigidbody's constraints.
4.  **Create a "Client" GameObject (Input Controller):**
    *   Create an empty GameObject (e.g., `GameObject -> Create Empty`).
    *   Rename it to `InputController`.
    *   Add the `PlayerController` component to it (`Add Component -> PlayerController`).
5.  **Assign References in Inspector:**
    *   Select the `InputController` GameObject.
    *   In its `PlayerController` component, drag the `Player` GameObject from the Hierarchy into the `Player Character` field.
    *   Drag the `CommandManager` GameObject from the Hierarchy into the `Command Processor` field.
6.  **Run the Scene:**
    *   Press **WASD** to move the player in discrete steps.
    *   Press **Space** to make the player jump.
    *   Press **Z** to undo the last action.
    *   Press **Y** to redo the last undone action.
    *   Observe the player's movement and the debug logs in the Console.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Stack<T>

namespace CommandPatternExample
{
    // -----------------------------------------------------------------------------------------------------------------
    // 1. The Command Interface (ICommand)
    //    Defines the contract for all concrete command classes.
    //    Every command must know how to Execute its action and how to Undo it.
    // -----------------------------------------------------------------------------------------------------------------
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    // -----------------------------------------------------------------------------------------------------------------
    // 2. The Receiver (PlayerCharacter)
    //    The object that performs the actual operations when a command's Execute() method is called.
    //    It doesn't know anything about commands; it just performs its own specific actions.
    // -----------------------------------------------------------------------------------------------------------------
    public class PlayerCharacter : MonoBehaviour
    {
        [Tooltip("The speed at which the character moves per discrete command.")]
        [SerializeField] private float moveSpeed = 1f; // Represents distance per command, not continuous speed.
        [Tooltip("The force applied when the character jumps.")]
        [SerializeField] private float jumpForce = 5f;

        private Rigidbody rb; // Reference to the Rigidbody component for physics actions like jumping.

        void Awake()
        {
            // Get the Rigidbody component, essential for physics-based actions.
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("PlayerCharacter requires a Rigidbody component for jumping and proper movement.");
                // Optionally disable this script if Rigidbody is missing to prevent NullReferenceExceptions.
                enabled = false;
            }
        }

        /// <summary>
        /// Moves the character by a given offset vector.
        /// This is the actual action performed by the PlayerCharacter.
        /// </summary>
        /// <param name="offset">The vector representing the change in position.</param>
        public void Move(Vector3 offset)
        {
            // Directly modify the transform's position.
            // In a more complex game, this might involve CharacterController.Move(), Rigidbody.MovePosition(), etc.
            transform.position += offset;
            Debug.Log($"Character moved by {offset} to: {transform.position}");
        }

        /// <summary>
        /// Makes the character jump by applying an upward force.
        /// This is another actual action performed by the PlayerCharacter.
        /// </summary>
        public void Jump()
        {
            if (rb != null)
            {
                // Clear any existing vertical velocity to ensure consistent jump height.
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                // Apply an instantaneous upward force.
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Debug.Log("Character jumped!");
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // 3. Concrete Commands
    //    Implement the ICommand interface, encapsulating a request to a Receiver object.
    //    Each command stores all the necessary information for its execution and undo operation.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Represents a command to move the PlayerCharacter.
    /// Stores the player, the direction of movement, and the player's position *before* the move for undo.
    /// </summary>
    public class MoveCommand : ICommand
    {
        private PlayerCharacter _player; // The Receiver instance
        private Vector3 _moveOffset;     // The specific offset for this move command
        private Vector3 _startPosition;  // Stored to facilitate undo: the position before execution

        /// <summary>
        /// Constructor for MoveCommand.
        /// </summary>
        /// <param name="player">The PlayerCharacter instance to move.</param>
        /// <param name="direction">The normalized direction of movement (e.g., Vector3.forward).</param>
        /// <param name="distance">The distance to move in the given direction.</param>
        public MoveCommand(PlayerCharacter player, Vector3 direction, float distance)
        {
            _player = player;
            // Calculate the actual offset the player will move.
            _moveOffset = direction.normalized * distance;
        }

        /// <summary>
        /// Executes the move command by telling the PlayerCharacter to move.
        /// Stores the player's current position *before* moving for future undo operations.
        /// </summary>
        public void Execute()
        {
            _startPosition = _player.transform.position; // Record current position for Undo
            _player.Move(_moveOffset);
        }

        /// <summary>
        /// Undoes the move command by returning the PlayerCharacter to its position before the command was executed.
        /// </summary>
        public void Undo()
        {
            _player.transform.position = _startPosition; // Restore to the recorded start position
            Debug.Log($"Undo: Character returned to previous position: {_startPosition}");
        }
    }

    /// <summary>
    /// Represents a command to make the PlayerCharacter jump.
    /// Stores the player and the player's Y-position *before* the jump for undo.
    /// </summary>
    public class JumpCommand : ICommand
    {
        private PlayerCharacter _player;    // The Receiver instance
        private Vector3 _startPosition;     // Stored to facilitate undo: the position before execution

        /// <summary>
        /// Constructor for JumpCommand.
        /// </summary>
        /// <param name="player">The PlayerCharacter instance to jump.</param>
        public JumpCommand(PlayerCharacter player)
        {
            _player = player;
        }

        /// <summary>
        /// Executes the jump command by telling the PlayerCharacter to jump.
        /// Stores the player's current position *before* jumping for future undo operations.
        /// </summary>
        public void Execute()
        {
            _startPosition = _player.transform.position; // Record current position for Undo
            _player.Jump();
        }

        /// <summary>
        /// Undoes the jump command by returning the PlayerCharacter to its Y-position
        /// before the jump and clearing its vertical velocity.
        /// </summary>
        public void Undo()
        {
            // For a jump, undo means returning to the Y position before the jump.
            // We also stop any current vertical velocity to prevent continued movement or falling after undo.
            Vector3 currentPos = _player.transform.position;
            _player.transform.position = new Vector3(currentPos.x, _startPosition.y, currentPos.z);

            // Ensure the Rigidbody is cleared of vertical forces
            Rigidbody rb = _player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Clear Y velocity
                rb.angularVelocity = Vector3.zero; // Clear any rotation too
            }
            Debug.Log($"Undo: Character returned to Y-position {_startPosition.y} and vertical velocity cleared.");
        }
    }


    // -----------------------------------------------------------------------------------------------------------------
    // 4. The Invoker (CommandProcessor)
    //    Responsible for taking a command, executing it, and managing its history (for undo/redo).
    //    It does not know the concrete type of command or the receiver; it only works with the ICommand interface.
    // -----------------------------------------------------------------------------------------------------------------
    public class CommandProcessor : MonoBehaviour
    {
        // A stack to store executed commands, allowing for undo functionality.
        private Stack<ICommand> _commandHistory = new Stack<ICommand>();
        // A stack to store undone commands, allowing for redo functionality.
        private Stack<ICommand> _redoHistory = new Stack<ICommand>();

        /// <summary>
        /// Executes a given command and adds it to the command history.
        /// Clears the redo history because a new action invalidates future redos.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute(); // Tell the command to perform its action.
            _commandHistory.Push(command); // Add the command to the history stack.
            _redoHistory.Clear();         // Clear redo history on a new command.
            Debug.Log($"Command executed: {command.GetType().Name}. History size: {_commandHistory.Count}");
        }

        /// <summary>
        /// Undoes the last executed command from the history.
        /// Moves the undone command to the redo history.
        /// </summary>
        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                ICommand lastCommand = _commandHistory.Pop(); // Get the last command from history.
                lastCommand.Undo();                           // Tell it to undo its action.
                _redoHistory.Push(lastCommand);               // Add it to the redo history.
                Debug.Log($"Undo executed: {lastCommand.GetType().Name}. History size: {_commandHistory.Count}, Redo size: {_redoHistory.Count}");
            }
            else
            {
                Debug.Log("No commands to undo.");
            }
        }

        /// <summary>
        /// Redoes the last undone command from the redo history.
        /// Moves the redone command back to the command history.
        /// </summary>
        public void RedoLastCommand()
        {
            if (_redoHistory.Count > 0)
            {
                ICommand redoCommand = _redoHistory.Pop(); // Get the last command from redo history.
                redoCommand.Execute();                     // Re-execute it.
                _commandHistory.Push(redoCommand);         // Put it back on the main command history.
                Debug.Log($"Redo executed: {redoCommand.GetType().Name}. History size: {_commandHistory.Count}, Redo size: {_redoHistory.Count}");
            }
            else
            {
                Debug.Log("No commands to redo.");
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // 5. The Client (PlayerController)
    //    Creates concrete command objects and assigns them to the Invoker when input is received.
    //    It holds references to both the Receiver (PlayerCharacter) and the Invoker (CommandProcessor).
    // -----------------------------------------------------------------------------------------------------------------
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The PlayerCharacter that will perform the actions.")]
        [SerializeField] private PlayerCharacter playerCharacter;
        [Tooltip("The CommandProcessor that will manage and execute commands.")]
        [SerializeField] private CommandProcessor commandProcessor;

        [Header("Movement Settings")]
        [Tooltip("The distance the player moves with each discrete MoveCommand.")]
        [SerializeField] private float moveMagnitude = 1f;

        void Awake()
        {
            // Basic validation to ensure required components are assigned.
            if (playerCharacter == null)
            {
                Debug.LogError("PlayerCharacter not assigned to PlayerController! Please assign it in the Inspector.");
                enabled = false; // Disable this script if references are missing.
            }
            if (commandProcessor == null)
            {
                Debug.LogError("CommandProcessor not assigned to PlayerController! Please assign it in the Inspector.");
                enabled = false; // Disable this script if references are missing.
            }
        }

        void Update()
        {
            // --- Handle Movement Commands ---
            // When a movement key is pressed, create a new MoveCommand with the appropriate direction
            // and magnitude, then pass it to the CommandProcessor for execution.
            if (Input.GetKeyDown(KeyCode.W))
            {
                commandProcessor.ExecuteCommand(new MoveCommand(playerCharacter, Vector3.forward, moveMagnitude));
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                commandProcessor.ExecuteCommand(new MoveCommand(playerCharacter, Vector3.back, moveMagnitude));
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                commandProcessor.ExecuteCommand(new MoveCommand(playerCharacter, Vector3.left, moveMagnitude));
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                commandProcessor.ExecuteCommand(new MoveCommand(playerCharacter, Vector3.right, moveMagnitude));
            }

            // --- Handle Jump Command ---
            // When the spacebar is pressed, create a new JumpCommand and execute it.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                commandProcessor.ExecuteCommand(new JumpCommand(playerCharacter));
            }

            // --- Handle Undo/Redo Commands ---
            // 'Z' key for Undo
            if (Input.GetKeyDown(KeyCode.Z))
            {
                commandProcessor.UndoLastCommand();
            }
            // 'Y' key for Redo
            if (Input.GetKeyDown(KeyCode.Y))
            {
                commandProcessor.RedoLastCommand();
            }
        }
    }
}
```