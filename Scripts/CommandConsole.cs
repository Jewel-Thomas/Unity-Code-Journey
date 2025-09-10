// Unity Design Pattern Example: CommandConsole
// This script demonstrates the CommandConsole pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, self-contained Unity C# script demonstrating the CommandConsole design pattern. It's designed to be dropped into a Unity project, and with minimal setup (creating UI elements and a dummy player), it will function immediately.

## CommandConsole Design Pattern in Unity

The Command pattern encapsulates a request as an object, thereby letting you parameterize clients with different requests, queue or log requests, and support undoable operations.

The **CommandConsole** variant extends this by creating a central "console" or "invoker" that maps string commands (like those typed into a game console) to specific `ICommand` objects, which then execute the desired actions with parsed arguments.

**Key Components:**

1.  **`ICommand` Interface:** Defines the contract for all commands. Each command must have a name, a description, and an `Execute` method that takes an array of string arguments.
2.  **Concrete Commands:** Implement `ICommand`. These classes encapsulate specific actions (e.g., logging a message, setting player health, teleporting).
3.  **`CommandConsoleManager` (Invoker/Receiver):** This is the core of the pattern. It's a `MonoBehaviour` that:
    *   Maintains a dictionary of registered commands.
    *   Parses user input from a UI element.
    *   Looks up and executes the appropriate `ICommand` object, passing the parsed arguments.
    *   Manages displaying output to the console UI.
    *   Acts as a central point for registering and unregistering commands.
4.  **`Player` (Receiver/Context):** A simple dummy `MonoBehaviour` that the commands can interact with (e.g., changing health, position).
5.  **UI Elements:** `TMP_InputField` for input, `TextMeshProUGUI` for output, and a `Button` to trigger execution.

---

### Setup Instructions for Unity

1.  **Create a New C# Script:** Name it `CommandConsoleExample.cs`.
2.  **Replace Content:** Copy and paste the entire code below into `CommandConsoleExample.cs`.
3.  **Install TextMeshPro:** If you haven't already, go to `Window > TextMeshPro > Import TMP Essential Resources`.
4.  **Create UI Canvas:**
    *   Right-click in the Hierarchy -> `UI -> Canvas`.
5.  **Add Input Field:**
    *   Right-click on `Canvas` -> `UI -> Text - TextMeshPro Input Field`. Rename it to `ConsoleInputField`.
    *   Position it somewhere visible (e.g., bottom of the screen).
6.  **Add Output Text Area:**
    *   Right-click on `Canvas` -> `UI -> Text - TextMeshPro`. Rename it to `ConsoleOutputText`.
    *   Position it above the input field. Make it tall enough to show multiple lines.
    *   Set its `Horizontal Overflow` to `Wrap` and `Vertical Overflow` to `Overflow` in the Inspector for `Rect Transform`.
7.  **Add Execute Button:**
    *   Right-click on `Canvas` -> `UI -> Button - TextMeshPro`. Rename it to `ExecuteButton`.
    *   Position it next to the input field. Change its text to "Execute".
8.  **Create Player GameObject:**
    *   Right-click in Hierarchy -> `3D Object -> Cube`. Rename it to `Player`.
    *   Position it at `(0, 1, 0)`.
9.  **Create GameManager:**
    *   Right-click in Hierarchy -> `Create Empty`. Rename it to `GameManager`.
10. **Assign Scripts and References:**
    *   Drag `CommandConsoleExample.cs` onto the `GameManager` GameObject.
    *   Drag `CommandConsoleExample.cs` (or specifically, the `Player` class component from it) onto the `Player` GameObject.
    *   Select `GameManager` in the Hierarchy. In its Inspector, you'll see the `Command Console Manager` component.
        *   Drag `ConsoleInputField` from the Hierarchy to the `Input Field (TMP)` slot.
        *   Drag `ConsoleOutputText` from the Hierarchy to the `Output Text (TMP)` slot.
        *   Drag the `Player` GameObject (the Cube) from the Hierarchy to the `Player Ref` slot.
    *   Select `ExecuteButton` in the Hierarchy. In its Inspector, find the `Button` component.
        *   Under `On Click()`, click the `+` button.
        *   Drag the `GameManager` GameObject from the Hierarchy into the `Runtime Only` slot.
        *   From the `No Function` dropdown, select `CommandConsoleExample.CommandConsoleManager -> ProcessInputFieldCommand()`.
11. **Run the Scene:** Press Play. Type commands into the input field and press the Execute button or Enter.

---

### `CommandConsoleExample.cs`

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button, although TextMeshPro is used for text
using TMPro; // Required for TextMeshProUGUI and TMP_InputField
using System;
using System.Collections.Generic;
using System.Linq;

// This entire example is contained within a single file for ease of use
// and demonstration purposes. In a larger project, you would separate
// these classes into individual files.

namespace CommandConsoleExample
{
    /// <summary>
    /// #region 1. ICommand Interface
    /// Defines the contract for all executable commands.
    /// This is the core interface of the Command pattern.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The name of the command, used for lookup (e.g., "log", "sethealth").
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// A brief description of what the command does, useful for help commands.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the command with the given arguments.
        /// </summary>
        /// <param name="args">An array of string arguments passed to the command.</param>
        void Execute(string[] args);
    }

    /// <summary>
    /// #endregion
    ///
    /// #region 2. Concrete Command Implementations
    /// These classes implement the ICommand interface, encapsulating specific actions.
    /// Each command operates on a receiver (e.g., Player, Debug.Log, the console itself).
    /// </summary>

    /// <summary>
    /// Command to log a message to the Unity console and the CommandConsole output.
    /// Example usage: `log Hello World`, `log This is a test`
    /// </summary>
    public class LogMessageCommand : ICommand
    {
        public string CommandName => "log";
        public string Description => "Logs a message to the console. Usage: log <message>";

        private CommandConsoleManager _console;

        public LogMessageCommand(CommandConsoleManager console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                _console.LogConsoleOutput("Error: No message provided for 'log' command.");
                Debug.LogError("LogMessageCommand: No message provided.");
                return;
            }

            string message = string.Join(" ", args);
            _console.LogConsoleOutput($"[LOG]: {message}");
            Debug.Log($"Console Log: {message}"); // Also log to Unity's console
        }
    }

    /// <summary>
    /// Command to set the player's health. Requires a reference to a Player object.
    /// Example usage: `sethealth 50`, `sethealth 100`
    /// </summary>
    public class SetPlayerHealthCommand : ICommand
    {
        public string CommandName => "sethealth";
        public string Description => "Sets the player's health. Usage: sethealth <amount>";

        private Player _player;
        private CommandConsoleManager _console;

        public SetPlayerHealthCommand(Player player, CommandConsoleManager console)
        {
            _player = player;
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (_player == null)
            {
                _console.LogConsoleOutput("Error: Player object not assigned to console manager.");
                Debug.LogError("SetPlayerHealthCommand: Player reference is null.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                _console.LogConsoleOutput("Error: Missing health amount. Usage: sethealth <amount>");
                return;
            }

            if (int.TryParse(args[0], out int health))
            {
                _player.SetHealth(health);
                _console.LogConsoleOutput($"Player health set to: {health}");
            }
            else
            {
                _console.LogConsoleOutput($"Error: Invalid health amount '{args[0]}'. Must be a number.");
            }
        }
    }

    /// <summary>
    /// Command to teleport the player to a specified XYZ coordinate.
    /// Example usage: `teleport 10 0 5`, `teleport 0 100 0`
    /// </summary>
    public class TeleportPlayerCommand : ICommand
    {
        public string CommandName => "teleport";
        public string Description => "Teleports the player to X Y Z coordinates. Usage: teleport <x> <y> <z>";

        private Player _player;
        private CommandConsoleManager _console;

        public TeleportPlayerCommand(Player player, CommandConsoleManager console)
        {
            _player = player;
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (_player == null)
            {
                _console.LogConsoleOutput("Error: Player object not assigned to console manager.");
                Debug.LogError("TeleportPlayerCommand: Player reference is null.");
                return;
            }

            if (args == null || args.Length < 3)
            {
                _console.LogConsoleOutput("Error: Missing coordinates. Usage: teleport <x> <y> <z>");
                return;
            }

            if (float.TryParse(args[0], out float x) &&
                float.TryParse(args[1], out float y) &&
                float.TryParse(args[2], out float z))
            {
                _player.Teleport(new Vector3(x, y, z));
                _console.LogConsoleOutput($"Player teleported to: ({x}, {y}, {z})");
            }
            else
            {
                _console.LogConsoleOutput($"Error: Invalid coordinates. Ensure X, Y, Z are numbers.");
            }
        }
    }

    /// <summary>
    /// Command to display a list of all registered commands and their descriptions.
    /// Example usage: `help`
    /// </summary>
    public class HelpCommand : ICommand
    {
        public string CommandName => "help";
        public string Description => "Lists all available commands and their descriptions.";

        private CommandConsoleManager _console;

        public HelpCommand(CommandConsoleManager console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            _console.LogConsoleOutput("\n--- Available Commands ---");
            foreach (var cmd in _console.GetAllCommands())
            {
                _console.LogConsoleOutput($"- {cmd.CommandName}: {cmd.Description}");
            }
            _console.LogConsoleOutput("--------------------------\n");
        }
    }

    /// <summary>
    /// #endregion
    ///
    /// #region 3. Player (Dummy Receiver)
    /// A simple MonoBehaviour to act as a receiver for commands like SetPlayerHealth and TeleportPlayer.
    /// This would typically be a more complex player script in a real game.
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private int _health = 100;
        public int Health
        {
            get => _health;
            private set
            {
                _health = Mathf.Clamp(value, 0, 100); // Clamp health between 0 and 100
                Debug.Log($"Player Health: {_health}");
            }
        }

        void Start()
        {
            Debug.Log($"Player initialized with health: {Health}");
        }

        public void SetHealth(int amount)
        {
            Health = amount;
            Debug.Log($"Player health changed to {Health}.");
        }

        public void Teleport(Vector3 position)
        {
            transform.position = position;
            Debug.Log($"Player teleported to {position}. Current position: {transform.position}");
        }
    }

    /// <summary>
    /// #endregion
    ///
    /// #region 4. CommandConsoleManager (Invoker/Receiver/Controller)
    /// The central component that manages commands, parses input, and executes commands.
    /// This acts as the invoker in the Command pattern.
    /// </summary>
    public class CommandConsoleManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TextMeshProUGUI _outputText;
        [SerializeField] private ScrollRect _outputScrollRect; // Optional for auto-scrolling

        [Header("Game Object References")]
        [SerializeField] private Player _playerRef; // Reference to our dummy Player script

        // Dictionary to store registered commands, mapped by their name (lowercase for case-insensitivity)
        private Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        // StringBuilder for efficient text appending to the output log
        private System.Text.StringBuilder _outputStringBuilder = new System.Text.StringBuilder();

        private void Awake()
        {
            InitializeConsole();
            RegisterAllCommands();

            // Setup input field event for pressing Enter
            if (_inputField != null)
            {
                _inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
            }
            else
            {
                Debug.LogError("CommandConsoleManager: Input Field not assigned!");
            }

            // Initial welcome message
            LogConsoleOutput("Welcome to the Command Console!");
            LogConsoleOutput("Type 'help' for a list of commands.");
        }

        private void InitializeConsole()
        {
            if (_outputText == null)
            {
                Debug.LogError("CommandConsoleManager: Output Text not assigned!");
                return;
            }
            _outputStringBuilder.Clear();
            _outputText.text = "";
        }

        private void RegisterAllCommands()
        {
            // Register concrete command instances
            RegisterCommand(new LogMessageCommand(this));
            RegisterCommand(new SetPlayerHealthCommand(_playerRef, this));
            RegisterCommand(new TeleportPlayerCommand(_playerRef, this));
            // The HelpCommand needs access to the console itself to list other commands
            RegisterCommand(new HelpCommand(this));

            Debug.Log("CommandConsoleManager: All commands registered.");
        }

        /// <summary>
        /// Registers a command with the console.
        /// </summary>
        /// <param name="command">The ICommand instance to register.</param>
        public void RegisterCommand(ICommand command)
        {
            string commandNameLower = command.CommandName.ToLower();
            if (!_commands.ContainsKey(commandNameLower))
            {
                _commands.Add(commandNameLower, command);
                // LogConsoleOutput($"Registered command: {command.CommandName}"); // Too chatty for console, use Debug.Log
            }
            else
            {
                Debug.LogWarning($"Attempted to register duplicate command: {command.CommandName}");
            }
        }

        /// <summary>
        /// Unregisters a command from the console.
        /// </summary>
        /// <param name="commandName">The name of the command to unregister.</param>
        public void UnregisterCommand(string commandName)
        {
            string commandNameLower = commandName.ToLower();
            if (_commands.ContainsKey(commandNameLower))
            {
                _commands.Remove(commandNameLower);
                LogConsoleOutput($"Unregistered command: {commandName}");
            }
            else
            {
                Debug.LogWarning($"Attempted to unregister non-existent command: {commandName}");
            }
        }

        /// <summary>
        /// Retrieves all registered commands. Used by HelpCommand.
        /// </summary>
        /// <returns>An enumerable collection of all registered ICommand objects.</returns>
        public IEnumerable<ICommand> GetAllCommands()
        {
            return _commands.Values;
        }

        /// <summary>
        /// Handles the event when the input field finishes editing (e.g., pressing Enter).
        /// </summary>
        /// <param name="input">The string content of the input field.</param>
        private void OnInputFieldEndEdit(string input)
        {
            // Only process if the user pressed Enter (or lost focus, but Enter is primary)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ProcessInputFieldCommand();
            }
        }

        /// <summary>
        /// Processes the command entered in the input field.
        /// This method is typically called by a UI Button's OnClick event or by the input field's OnEndEdit.
        /// </summary>
        public void ProcessInputFieldCommand()
        {
            if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text))
            {
                return;
            }

            string inputCommand = _inputField.text;
            LogConsoleOutput($"> {inputCommand}"); // Echo the command typed by the user

            ExecuteCommand(inputCommand);

            // Clear the input field and refocus for the next command
            _inputField.text = "";
            _inputField.ActivateInputField(); // Keep input field active for continuous typing
        }

        /// <summary>
        /// Parses the input string, finds the corresponding command, and executes it.
        /// This is the core 'invoker' logic.
        /// </summary>
        /// <param name="commandLine">The full string entered by the user (e.g., "log hello world").</param>
        public void ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return;
            }

            // Split the input into command name and arguments
            string[] parts = commandLine.Split(' ');
            string commandName = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray(); // All parts after the command name are arguments

            if (_commands.TryGetValue(commandName, out ICommand command))
            {
                try
                {
                    command.Execute(args);
                }
                catch (Exception e)
                {
                    LogConsoleOutput($"Error executing '{commandName}': {e.Message}");
                    Debug.LogError($"Command execution failed for '{commandName}': {e}");
                }
            }
            else
            {
                LogConsoleOutput($"Unknown command: '{commandName}'. Type 'help' for a list of commands.");
                Debug.LogWarning($"CommandConsole: Unknown command '{commandName}' entered.");
            }
        }

        /// <summary>
        /// Appends a message to the console's output UI.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void LogConsoleOutput(string message)
        {
            if (_outputText == null)
            {
                Debug.LogError("Output Text is null, cannot log to console UI.");
                return;
            }

            _outputStringBuilder.AppendLine(message);
            _outputText.text = _outputStringBuilder.ToString();

            // Auto-scroll to the bottom if a ScrollRect is assigned
            if (_outputScrollRect != null)
            {
                Canvas.ForceUpdateCanvases(); // Ensure layout is updated before scrolling
                _outputScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_inputField != null)
            {
                _inputField.onEndEdit.RemoveListener(OnInputFieldEndEdit);
            }
        }
    }
    /// #endregion
}
```