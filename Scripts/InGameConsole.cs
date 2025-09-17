// Unity Design Pattern Example: InGameConsole
// This script demonstrates the InGameConsole pattern in Unity
// Generated automatically - ready to use in your Unity project

The InGameConsole design pattern provides a powerful way for developers and even advanced users to interact with a running game or application using text commands. It's invaluable for debugging, testing, tweaking game parameters on the fly, and even creating developer-specific cheats or tools.

This C# Unity example implements a practical InGameConsole. It's a singleton, allowing easy access from anywhere, supports registering custom commands with arguments, displays output, and has a basic `OnGUI` interface for input and display.

---

### InGameConsole.cs

This script should be attached to an empty GameObject in your scene (e.g., named "InGameConsole").

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
///     InGameConsole: A practical implementation of the InGameConsole design pattern for Unity.
///     This console allows for registering and executing commands via a simple text interface,
///     useful for debugging, testing, and developer tools.
/// </summary>
/// <remarks>
///     How to use:
///     1. Create an empty GameObject in your scene and name it "InGameConsole".
///     2. Attach this script (`InGameConsole.cs`) to that GameObject.
///     3. Run your game. Press the '`' (tilde) key to toggle the console visibility.
///     4. Type 'help' and press Enter to see available commands.
///
///     To add your own custom commands from other scripts:
///     Call `InGameConsole.Instance.RegisterCommand()` from an `Awake()` or `Start()` method
///     of your other script.
///
///     Example Custom Command Registration:
///     ------------------------------------
///     // In your GameManager.cs or PlayerController.cs:
///     public class MyGameScript : MonoBehaviour
///     {
///         void Awake()
///         {
///             // Register a simple command without arguments
///             InGameConsole.Instance.RegisterCommand(
///                 "hello",                                    // Command name
///                 "Prints a greeting message.",               // Description
///                 (args) => {                                 // Action delegate
///                     InGameConsole.Instance.Log("Hello from custom command!");
///                 }
///             );
///
///             // Register a command that takes arguments
///             InGameConsole.Instance.RegisterCommand(
///                 "set_speed",
///                 "Sets the player speed. Usage: set_speed <value>",
///                 (args) => {
///                     if (args.Length == 1 && float.TryParse(args[0], out float speed))
///                     {
///                         // Example: Find a player and set their speed
///                         // PlayerController player = FindObjectOfType<PlayerController>();
///                         // if (player != null) {
///                         //     player.MoveSpeed = speed;
///                         //     InGameConsole.Instance.Log($"Player speed set to {speed}.");
///                         // } else {
///                         //     InGameConsole.Instance.LogWarning("PlayerController not found.");
///                         // }
///                         InGameConsole.Instance.Log($"Attempting to set speed to {speed}. (PlayerController logic commented out for example)");
///                     }
///                     else
///                     {
///                         InGameConsole.Instance.LogWarning("Invalid usage: set_speed <value>");
///                     }
///                 }
///             );
///
///             // Register a command that logs to Unity's Debug.Log
///             InGameConsole.Instance.RegisterCommand(
///                 "unitylog",
///                 "Logs a message to Unity's Debug.Log. Usage: unitylog <message>",
///                 (args) => {
///                     if (args.Length > 0)
///                     {
///                         string message = string.Join(" ", args);
///                         Debug.Log($"[InGameConsole] Unity Log: {message}");
///                         InGameConsole.Instance.Log($"Logged to Unity: {message}");
///                     }
///                     else
///                     {
///                         InGameConsole.Instance.LogWarning("Usage: unitylog <message>");
///                     }
///                 }
///             );
///         }
///     }
/// </remarks>
public class InGameConsole : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // The InGameConsole is a singleton to ensure there's only one instance
    // managing all console commands and UI, making it globally accessible.
    public static InGameConsole Instance { get; private set; }

    // --- Public Inspector Settings ---
    // These fields allow tweaking console behavior and appearance directly from the Unity Editor.
    [Header("Console Settings")]
    [Tooltip("The key used to toggle the console visibility.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // Default to tilde (`) key

    [Tooltip("Maximum number of lines to keep in the console history/log.")]
    [SerializeField] private int maxLogLines = 50;

    [Tooltip("Height of the console window as a percentage of screen height.")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float consoleHeightPercentage = 0.5f;

    // --- Private Internal State ---
    private bool _isVisible = false; // Tracks if the console UI is currently visible.
    private string _currentInput = ""; // Stores the text currently being typed in the input field.
    private Vector2 _scrollPosition = Vector2.zero; // Controls the scroll position of the log display.

    // Stores the history of all messages logged to the console.
    private List<string> _logMessages = new List<string>();

    // The core data structure for storing registered commands.
    // Key: Command name (case-insensitive).
    // Value: ConsoleCommand struct containing details and the action to execute.
    private Dictionary<string, ConsoleCommand> _commands = new Dictionary<string, ConsoleCommand>(StringComparer.OrdinalIgnoreCase);

    // --- ConsoleCommand Struct ---
    // Defines the structure for each command that can be registered with the console.
    private struct ConsoleCommand
    {
        public string name;         // The unique name of the command (e.g., "help", "set_speed").
        public string description;  // A brief explanation of what the command does.
        public Action<string[]> action; // The delegate (method) to be executed when the command is called.
                                        // It takes a string array for command arguments.

        public ConsoleCommand(string name, string description, Action<string[]> action)
        {
            this.name = name;
            this.description = description;
            this.action = action;
        }
    }

    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Enforce the Singleton pattern.
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Otherwise, set this instance as the singleton.
        Instance = this;
        // Ensure the console GameObject persists across scene loads, useful for debugging persistent issues.
        DontDestroyOnLoad(gameObject);

        // Register default console commands when the console is initialized.
        RegisterDefaultCommands();
    }

    private void Update()
    {
        // Toggle console visibility when the specified key is pressed.
        if (Input.GetKeyDown(toggleKey))
        {
            _isVisible = !_isVisible;
            // Optionally clear input when hiding the console
            if (!_isVisible)
            {
                _currentInput = "";
            }
        }
    }

    // OnGUI is called multiple times per frame to draw and handle GUI events.
    private void OnGUI()
    {
        // Only draw the console UI if it's visible.
        if (!_isVisible)
        {
            return;
        }

        // Calculate console dimensions based on screen size and height percentage.
        float consoleHeight = Screen.height * consoleHeightPercentage;
        float inputHeight = 30f; // Fixed height for the input field.
        float logAreaHeight = consoleHeight - inputHeight - 5f; // Account for input field and some padding.

        // Draw the console background box.
        GUI.Box(new Rect(0, 0, Screen.width, consoleHeight), "");

        // --- Log Display Area ---
        // Create a scrollable view for the log messages.
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition,
            GUILayout.Width(Screen.width), GUILayout.Height(logAreaHeight));

        // Display all accumulated log messages.
        // Using GUILayout.ExpandHeight(true) ensures the content expands to fill the scroll view.
        // We use string.Join to display messages with newlines between them.
        GUILayout.TextArea(string.Join("\n", _logMessages), GUILayout.ExpandHeight(true));

        GUILayout.EndScrollView();

        // --- Input Field Area ---
        // Define the rectangle for the input field at the bottom of the console.
        Rect inputRect = new Rect(0, consoleHeight - inputHeight, Screen.width, inputHeight);

        // Make the input field the active control for keyboard input.
        // This ensures that when the console is visible, typing directly goes into the input field.
        GUI.SetNextControlName("ConsoleInput");
        _currentInput = GUI.TextField(inputRect, _currentInput);
        GUI.FocusControl("ConsoleInput"); // Ensure the input field always has focus.

        // Handle input events, specifically the Enter key to execute commands.
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrWhiteSpace(_currentInput))
            {
                Log($"> {_currentInput}"); // Log the command before executing.
                ExecuteCommand(_currentInput); // Process the entered command.
                _currentInput = ""; // Clear the input field after execution.
            }
            Event.current.Use(); // Consume the event to prevent it from affecting other UI elements.
        }
    }

    // --- Public API for Logging and Command Registration ---

    /// <summary>
    ///     Adds a message to the console's log history and displays it in the UI.
    ///     Automatically handles log line limits.
    /// </summary>
    /// <param name="message">The string message to log.</param>
    public void Log(string message)
    {
        _logMessages.Add(message);
        // Remove oldest messages if the log exceeds the maximum allowed lines.
        while (_logMessages.Count > maxLogLines)
        {
            _logMessages.RemoveAt(0);
        }
        // Auto-scroll to the bottom when a new message is added.
        _scrollPosition.y = Mathf.Infinity;
    }

    /// <summary>
    ///     Logs a warning message to the console (often displayed in yellow or specific styling if UI supported).
    /// </summary>
    /// <param name="message">The warning message.</param>
    public void LogWarning(string message)
    {
        // For simplicity, we just prepend "[WARNING]" here. A more advanced UI might color this.
        Log($"[WARNING] {message}");
    }

    /// <summary>
    ///     Logs an error message to the console.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void LogError(string message)
    {
        // For simplicity, we just prepend "[ERROR]" here. A more advanced UI might color this red.
        Log($"[ERROR] {message}");
    }

    /// <summary>
    ///     Registers a new command with the console.
    ///     Other scripts should use this to add their custom functionalities.
    /// </summary>
    /// <param name="commandName">The unique name for the command (e.g., "spawn", "godmode").</param>
    /// <param name="description">A short description of what the command does.</param>
    /// <param name="action">The method to call when this command is executed. It receives arguments as a string array.</param>
    public void RegisterCommand(string commandName, string description, Action<string[]> action)
    {
        // Ensure command name is not empty and is unique.
        if (string.IsNullOrWhiteSpace(commandName))
        {
            Debug.LogError("InGameConsole: Attempted to register a command with an empty or null name.");
            return;
        }

        if (_commands.ContainsKey(commandName))
        {
            Debug.LogWarning($"InGameConsole: Command '{commandName}' already registered. Overwriting.");
        }

        _commands[commandName.ToLower()] = new ConsoleCommand(commandName.ToLower(), description, action);
        // Log to Unity console to confirm registration, useful during development.
        Debug.Log($"InGameConsole: Registered command '{commandName}'");
    }

    // --- Core Command Processing Logic ---

    /// <summary>
    ///     Parses the input string, extracts the command name and its arguments,
    ///     then attempts to execute the corresponding command.
    /// </summary>
    /// <param name="inputLine">The full string entered by the user in the console.</param>
    private void ExecuteCommand(string inputLine)
    {
        // Split the input line into command and arguments.
        // Uses ' ' as a delimiter, then removes empty entries (e.g., multiple spaces).
        string[] parts = inputLine.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        if (parts.Length == 0)
        {
            return; // Nothing to execute.
        }

        string commandName = parts[0].ToLower(); // Command names are case-insensitive.
        string[] args = parts.Skip(1).ToArray(); // Remaining parts are arguments.

        if (_commands.TryGetValue(commandName, out ConsoleCommand command))
        {
            try
            {
                command.action.Invoke(args); // Execute the command's associated action.
            }
            catch (Exception ex)
            {
                LogError($"Error executing command '{commandName}': {ex.Message}");
                Debug.LogException(ex); // Also log to Unity's console for full stack trace.
            }
        }
        else
        {
            LogWarning($"Unknown command: '{commandName}'. Type 'help' for a list of commands.");
        }
    }

    // --- Default Console Commands ---

    /// <summary>
    ///     Registers a set of useful default commands for the console.
    /// </summary>
    private void RegisterDefaultCommands()
    {
        RegisterCommand("help", "Lists all available commands.", (args) =>
        {
            Log("--- Available Commands ---");
            foreach (var cmd in _commands.Values.OrderBy(c => c.name)) // Sort alphabetically for readability.
            {
                Log($"- {cmd.name}: {cmd.description}");
            }
            Log("--------------------------");
        });

        RegisterCommand("clear", "Clears the console log.", (args) =>
        {
            _logMessages.Clear();
            Log("Console cleared.");
        });

        RegisterCommand("echo", "Prints a message to the console. Usage: echo <message>", (args) =>
        {
            if (args.Length > 0)
            {
                Log(string.Join(" ", args)); // Join all arguments back into a single message.
            }
            else
            {
                LogWarning("Usage: echo <message>");
            }
        });

        RegisterCommand("quit", "Exits the application.", (args) =>
        {
#if UNITY_EDITOR
            // If in editor, stop play mode.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // If in a build, quit the application.
            Application.Quit();
#endif
            Log("Exiting application...");
        });

        RegisterCommand("toggle_ui", "Toggles the visibility of other game UI elements (example command).", (args) =>
        {
            // This is an example of a command that would interact with your game's UI Manager.
            // You would need to implement a UIManager singleton or similar pattern in your game.
            // Example: UIManager.Instance.ToggleGameUI();
            Log("Toggling game UI visibility... (Implementation needed in your UIManager)");
            // FindObjectOfType<Canvas>()?.gameObject.SetActive(!FindObjectOfType<Canvas>().gameObject.activeSelf);
        });

        RegisterCommand("load_scene", "Loads a scene by name. Usage: load_scene <sceneName>", (args) =>
        {
            if (args.Length == 1)
            {
                string sceneName = args[0];
                Log($"Attempting to load scene: {sceneName}...");
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                }
                catch (Exception e)
                {
                    LogError($"Failed to load scene '{sceneName}': {e.Message}");
                }
            }
            else
            {
                LogWarning("Usage: load_scene <sceneName>");
            }
        });
    }
}
```

---

### Example Usage (Optional: Create a `MyGameScript.cs` to test custom commands)

To demonstrate how other scripts would integrate with the console, create a new C# script named `MyGameScript.cs` and attach it to any GameObject in your scene (e.g., your GameManager).

```csharp
using UnityEngine;

/// <summary>
///     MyGameScript: An example class demonstrating how to register custom commands
///     with the InGameConsole from another script in your project.
/// </summary>
public class MyGameScript : MonoBehaviour
{
    // Example property that could be manipulated by console commands.
    public float PlayerMoveSpeed { get; private set; } = 5f;

    void Awake()
    {
        // Ensure the InGameConsole instance exists before trying to register commands.
        // It's good practice to check for null if this script might initialize before the console.
        if (InGameConsole.Instance == null)
        {
            Debug.LogError("MyGameScript: InGameConsole instance not found. Make sure it's in the scene.");
            return;
        }

        // --- Registering Custom Commands ---

        // 1. Simple command without arguments
        InGameConsole.Instance.RegisterCommand(
            "hello",
            "Prints a greeting message from MyGameScript.",
            (args) => {
                InGameConsole.Instance.Log("Hello from MyGameScript's 'hello' command!");
            }
        );

        // 2. Command that takes a single float argument to set PlayerMoveSpeed
        InGameConsole.Instance.RegisterCommand(
            "set_playerspeed",
            "Sets the player's movement speed. Usage: set_playerspeed <value>",
            (args) => {
                if (args.Length == 1 && float.TryParse(args[0], out float speed))
                {
                    PlayerMoveSpeed = speed;
                    InGameConsole.Instance.Log($"PlayerMoveSpeed set to {PlayerMoveSpeed}.");
                }
                else
                {
                    InGameConsole.Instance.LogWarning("Invalid usage: set_playerspeed <value>");
                }
            }
        );

        // 3. Command that takes multiple string arguments and logs them to Unity's Debug.Log
        InGameConsole.Instance.RegisterCommand(
            "unitylog_message",
            "Logs a custom message to Unity's Debug.Log console. Usage: unitylog_message <your message here>",
            (args) => {
                if (args.Length > 0)
                {
                    string message = string.Join(" ", args); // Join all arguments into one string.
                    Debug.Log($"[MyGameScript Console] Custom Unity Log: {message}");
                    InGameConsole.Instance.Log($"Message sent to Unity Debug.Log: '{message}'");
                }
                else
                {
                    InGameConsole.Instance.LogWarning("Usage: unitylog_message <your message here>");
                }
            }
        );

        // 4. Command to demonstrate interacting with a scene object (e.g., changing its color)
        InGameConsole.Instance.RegisterCommand(
            "change_cube_color",
            "Changes the color of a GameObject named 'TestCube'. Usage: change_cube_color <r> <g> <b>",
            (args) => {
                if (args.Length == 3 &&
                    float.TryParse(args[0], out float r) &&
                    float.TryParse(args[1], out float g) &&
                    float.TryParse(args[2], out float b))
                {
                    GameObject testCube = GameObject.Find("TestCube");
                    if (testCube != null && testCube.TryGetComponent<Renderer>(out Renderer renderer))
                    {
                        renderer.material.color = new Color(r, g, b);
                        InGameConsole.Instance.Log($"TestCube color set to R:{r} G:{g} B:{b}.");
                    }
                    else
                    {
                        InGameConsole.Instance.LogWarning("GameObject 'TestCube' with a Renderer not found.");
                    }
                }
                else
                {
                    InGameConsole.Instance.LogWarning("Invalid usage: change_cube_color <r> <g> <b> (values 0-1)");
                }
            }
        );

        InGameConsole.Instance.Log("MyGameScript has registered its custom commands.");
    }

    void Update()
    {
        // Example of something that might change based on console commands
        // Debug.Log($"Current Player Speed: {PlayerMoveSpeed}");
    }
}
```

---

### How to Set Up in Unity:

1.  **Create a C# Script:** Create a new C# script named `InGameConsole.cs` and copy the first code block into it.
2.  **Create Console GameObject:** In your Unity scene, right-click in the Hierarchy window -> Create Empty. Name it `InGameConsole`.
3.  **Attach Script:** Drag the `InGameConsole.cs` script onto the `InGameConsole` GameObject in the Hierarchy or Inspector.
4.  **Optional: Add `MyGameScript.cs`:**
    *   Create another C# script named `MyGameScript.cs` and copy the second code block into it.
    *   Create an empty GameObject (e.g., `GameManager`) and attach `MyGameScript.cs` to it.
    *   To test the `change_cube_color` command, create a 3D Cube (GameObject -> 3D Object -> Cube), and rename it to `TestCube`.
5.  **Run the Game:** Press Play.
6.  **Toggle Console:** Press the ` (tilde) key on your keyboard (usually to the left of '1').
7.  **Try Commands:**
    *   Type `help` and press Enter to see all commands, including the default ones and any custom ones you registered.
    *   Type `echo Hello World!`
    *   Type `set_playerspeed 10.5` (if you added `MyGameScript`)
    *   Type `change_cube_color 1 0 0` (if you added `MyGameScript` and `TestCube`)
    *   Type `quit` to exit Play mode (in Editor) or the application (in a build).

---

### Explanation of the Design Pattern and Code:

**1. Singleton Pattern (`Instance` property, `Awake()`):**
   *   The `InGameConsole` uses the Singleton pattern (`public static InGameConsole Instance`). This ensures there's only one active console instance throughout the application.
   *   `Awake()` handles the initialization: setting `Instance` and calling `DontDestroyOnLoad` so the console persists across scene changes, which is crucial for continuous debugging.

**2. Command Registration (`ConsoleCommand` struct, `_commands` dictionary, `RegisterCommand()`):**
   *   **`ConsoleCommand` struct:** This inner struct defines what a command is: a `name` (string), a `description` (string), and an `action` (an `Action<string[]>`).
     *   `Action<string[]>` is a delegate that represents a method that takes a `string[]` (for command arguments) and returns `void`. This is the core of how commands are executed – it's a flexible pointer to a method.
   *   **`_commands` dictionary:** A `Dictionary<string, ConsoleCommand>` stores all registered commands. The key is the command name (case-insensitive for user-friendliness), and the value is the `ConsoleCommand` struct.
   *   **`RegisterCommand()` method:** This public method is the API for other scripts to add their commands. They provide the command name, a description, and the `Action` to be invoked.

**3. Input Handling (`OnGUI()`, `_currentInput`, `Event.current.keyCode == KeyCode.Return`):**
   *   `OnGUI()` is Unity's immediate mode GUI system, used here for simplicity to draw the console. For a production game, you'd likely use Unity UI (UGUI) with a `Canvas`, `InputField`, and `TextMeshPro` for better control and appearance.
   *   `toggleKey` (tilde `) switches console visibility.
   *   A `GUI.TextField` captures user input into `_currentInput`.
   *   When the Enter key (`KeyCode.Return`) is pressed, the content of `_currentInput` is passed to `ExecuteCommand()`.

**4. Output Display (`_logMessages` list, `Log()`, `OnGUI()`):**
   *   `_logMessages` is a `List<string>` that stores every message logged to the console.
   *   `Log()`, `LogWarning()`, `LogError()` are public methods for other scripts to send output to the console. They add messages to `_logMessages` and handle line limits.
   *   `OnGUI()` draws a `GUILayout.TextArea` inside a `GUILayout.ScrollView` to display these messages, allowing the user to scroll through history.

**5. Command Execution (`ExecuteCommand()`):**
   *   This method takes the raw input string.
   *   It `Split`s the string by spaces to separate the command name from its arguments. `parts.Skip(1).ToArray()` isolates the arguments.
   *   It performs a dictionary lookup (`_commands.TryGetValue`) to find the `ConsoleCommand` corresponding to the entered name.
   *   If found, it invokes the command's `action` delegate, passing the parsed arguments.
   *   Includes basic error handling for unknown commands and exceptions during command execution.

**6. Default Commands (`RegisterDefaultCommands()`):**
   *   The console pre-registers essential commands like `help` (lists all commands), `clear` (clears the log), `echo` (repeats input), `quit` (exits the application), and example commands like `toggle_ui` and `load_scene`. These serve as good examples of how to define new commands.

**7. Unity Coding Conventions:**
   *   `[SerializeField]` for private fields exposed in the Inspector.
   *   CamelCase for private fields (`_currentInput`), PascalCase for public methods (`RegisterCommand`).
   *   Clear and descriptive names for classes, methods, and variables.
   *   `Debug.LogError` and `Debug.LogWarning` for Unity console output alongside in-game console messages.

This complete example provides a robust and extensible foundation for adding in-game console functionality to any Unity project, demonstrating the pattern in a practical, educational manner.