// Unity Design Pattern Example: RuntimeDebugTools
// This script demonstrates the RuntimeDebugTools pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **RuntimeDebugTools** design pattern in Unity. This pattern provides a way to interact with your game's internal state and execute commands during runtime, without needing to stop the game, recompile, or use the Unity Editor's console. It's incredibly useful for:

*   **Faster Iteration:** Quickly test game mechanics, tweak values, or reproduce bugs.
*   **Quality Assurance (QA):** Testers can use pre-defined commands to set up specific test scenarios, grant items, or skip levels.
*   **Bug Diagnostics:** In builds (especially development builds), you can log more information or inspect variables that aren't exposed in the normal UI.
*   **Cheat Codes:** A natural extension for player cheats.

---

### **`RuntimeDebugManager.cs`**

This script creates a central manager for all runtime debug functionalities, including a simple in-game console.

**How to Use This Example:**

1.  **Create a New Unity Project** or open an existing one.
2.  **Create a C# Script** named `RuntimeDebugManager.cs` and paste the code below into it.
3.  **Create an Empty GameObject** in your scene (e.g., named "DebugManager").
4.  **Attach the `RuntimeDebugManager.cs` script** to this GameObject.
5.  **Run the Scene.**
6.  **Press the default toggle key** (backtick ` ` ` or `~`) to open/close the console.
7.  **Type `help`** and press Enter to see available commands.
8.  **Experiment** with the example commands (`timescale`, `clear`, `log`, `toggle_invincible`, `player_speed`).

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements the RuntimeDebugTools design pattern.
/// This manager provides an in-game console for executing commands and logging messages
/// during runtime, without needing to stop the game or rely on the Unity Editor.
/// </summary>
/// <remarks>
/// This pattern is highly useful for QA, development, and even as a foundation for cheat codes.
/// It allows for dynamic inspection and modification of game state on the fly.
/// </remarks>
public class RuntimeDebugManager : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Ensures there's only one instance of the DebugManager throughout the game.
    public static RuntimeDebugManager Instance { get; private set; }

    // --- Configuration Serialized Fields ---
    // These values can be adjusted directly in the Unity Inspector.
    [Header("Console Settings")]
    [Tooltip("Key to toggle the debug console visibility.")]
    [SerializeField]
    private KeyCode _toggleKey = KeyCode.BackQuote; // Default: `~` or backtick key

    [Tooltip("Maximum number of lines to display in the console log.")]
    [SerializeField]
    private int _maxConsoleLines = 20;

    [Tooltip("If true, the console will be visible immediately on game start.")]
    [SerializeField]
    private bool _enableOnStart = false;

    [Tooltip("If true, console input will block normal game input (e.g., player movement).")]
    [SerializeField]
    private bool _blockGameInputWhenActive = true;

    // --- Internal State ---
    private bool _isDebugConsoleActive; // True if the console is currently open
    private List<string> _consoleLog; // Stores all messages displayed in the console
    private Dictionary<string, DebugCommand> _commands; // Maps command names to their handlers and descriptions
    private string _inputCommand = ""; // The current text being typed into the command input field
    private Vector2 _scrollPosition; // Current scroll position for the console log
    private GUIStyle _consoleTextStyle; // Custom style for console text
    private GUIStyle _inputFieldStyle; // Custom style for input field
    private GUIStyle _buttonStyle; // Custom style for buttons
    private Texture2D _backgroundTexture; // Background for the console

    // --- DebugCommand Structure ---
    /// <summary>
    /// Represents a single debug command.
    /// Stores the action to be executed, its description, and expected argument count.
    /// </summary>
    private class DebugCommand
    {
        public Action<string[]> Action;
        public string Description;
        public int MinArgs;
        public int MaxArgs; // Use -1 for unlimited arguments

        public DebugCommand(Action<string[]> action, string description, int minArgs = 0, int maxArgs = 0)
        {
            Action = action;
            Description = description;
            MinArgs = minArgs;
            MaxArgs = maxArgs;
        }
    }

    // ====================================================================================
    // --- Unity Lifecycle Methods ---
    // ====================================================================================

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the debug manager alive across scenes

        // Initialize internal collections
        _consoleLog = new List<string>();
        _commands = new Dictionary<string, DebugCommand>();

        // Set initial console visibility
        _isDebugConsoleActive = _enableOnStart;

        // Initialize GUI styles (important for consistent look)
        InitializeGUIStyles();

        // Register default and example commands
        RegisterCoreCommands();
        RegisterExampleCommands();

        Log($"<color=lime>Runtime Debug Manager Initialized. Press '{_toggleKey}' to toggle console.</color>");
        Log("Type 'help' for a list of commands.");
    }

    private void Update()
    {
        // Toggle console visibility with the configured key
        if (Input.GetKeyDown(_toggleKey))
        {
            ToggleConsole();
        }

        // If console is active and blocking input, prevent other scripts from receiving input.
        if (_isDebugConsoleActive && _blockGameInputWhenActive)
        {
            // This is a simple way to block, actual game input systems might need more robust handling
            // e.g., setting a global game state flag.
            Input.ResetInputAxes(); 
        }
    }

    // OnGUI is called multiple times per frame for rendering and handling GUI events.
    private void OnGUI()
    {
        if (!_isDebugConsoleActive) return;

        // Draw the console background
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _backgroundTexture, ScaleMode.StretchToFill);

        // --- Console Log Area ---
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 70));
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Height(Screen.height - 80));

        foreach (string line in _consoleLog)
        {
            GUILayout.Label(line, _consoleTextStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // --- Command Input Field ---
        GUILayout.BeginArea(new Rect(10, Screen.height - 50, Screen.width - 20, 40));
        GUILayout.BeginHorizontal();

        // Input field for commands
        GUI.SetNextControlName("CommandInput"); // Allows us to focus this control
        _inputCommand = GUILayout.TextField(_inputCommand, _inputFieldStyle, GUILayout.ExpandWidth(true));

        // Command execution button
        if (GUILayout.Button("Execute", _buttonStyle, GUILayout.Width(100)))
        {
            ExecuteCommand(_inputCommand);
            _inputCommand = ""; // Clear input after execution
            GUI.FocusControl(""); // Unfocus the input field
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        // Automatically focus the input field when console is active
        if (_isDebugConsoleActive && Event.current.type == EventType.Layout)
        {
            GUI.FocusControl("CommandInput");
        }

        // Process Enter key for command execution
        if (_isDebugConsoleActive && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrWhiteSpace(_inputCommand))
            {
                ExecuteCommand(_inputCommand);
                _inputCommand = ""; // Clear input after execution
                GUI.FocusControl("CommandInput"); // Refocus to allow continuous typing
            }
            Event.current.Use(); // Consume the event to prevent it from affecting game input
        }
    }

    // ====================================================================================
    // --- Public API for Other Scripts ---
    // ====================================================================================

    /// <summary>
    /// Registers a new debug command with the manager.
    /// Other scripts can call this from their Awake/Start methods to add their own commands.
    /// </summary>
    /// <param name="commandName">The name of the command (e.g., "spawn_item").</param>
    /// <param name="commandAction">The delegate (method) to execute when the command is called.
    ///                               It receives an array of string arguments.</param>
    /// <param name="description">A brief description of what the command does.</param>
    /// <param name="minArgs">Minimum number of arguments expected by the command. (Default: 0)</param>
    /// <param name="maxArgs">Maximum number of arguments expected. Use -1 for unlimited. (Default: 0)</param>
    public void RegisterCommand(string commandName, Action<string[]> commandAction, string description, int minArgs = 0, int maxArgs = 0)
    {
        commandName = commandName.ToLower(); // Commands are case-insensitive
        if (_commands.ContainsKey(commandName))
        {
            Log($"<color=yellow>Warning: Command '{commandName}' already registered. Overwriting.</color>");
        }
        _commands[commandName] = new DebugCommand(commandAction, description, minArgs, maxArgs);
        Log($"<color=cyan>Command '{commandName}' registered.</color>");
    }

    /// <summary>
    /// Logs a message to the in-game debug console.
    /// </summary>
    /// <param name="message">The message string. Can use rich text tags (e.g., &lt;color=red&gt;).</param>
    public void Log(string message)
    {
        // Add timestamp for better debugging
        _consoleLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

        // Keep the log within the maximum line limit
        while (_consoleLog.Count > _maxConsoleLines)
        {
            _consoleLog.RemoveAt(0); // Remove the oldest message
        }

        // Auto-scroll to the bottom when a new message is added
        _scrollPosition.y = float.MaxValue; 
    }

    /// <summary>
    /// Toggles the visibility of the debug console.
    /// </summary>
    public void ToggleConsole()
    {
        _isDebugConsoleActive = !_isDebugConsoleActive;
        if (_isDebugConsoleActive)
        {
            Log("<color=orange>Debug Console Opened.</color>");
            // Ensure input field is focused when opened
            GUI.FocusControl("CommandInput");
        }
        else
        {
            Log("<color=orange>Debug Console Closed.</color>");
            _inputCommand = ""; // Clear input when closing
            GUI.FocusControl(""); // Unfocus
        }
    }

    // ====================================================================================
    // --- Internal Command Handling Logic ---
    // ====================================================================================

    /// <summary>
    /// Parses and executes a command string entered by the user.
    /// </summary>
    /// <param name="fullCommandString">The full string entered in the console.</param>
    private void ExecuteCommand(string fullCommandString)
    {
        if (string.IsNullOrWhiteSpace(fullCommandString)) return;

        Log($"> {fullCommandString}"); // Log the command that was entered

        // Split the command string into parts: command name and arguments
        string[] parts = fullCommandString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string commandName = parts[0].ToLower(); // Command names are case-insensitive
        string[] args = parts.Skip(1).ToArray(); // All subsequent parts are arguments

        if (_commands.TryGetValue(commandName, out DebugCommand debugCommand))
        {
            // Check argument count
            if (args.Length < debugCommand.MinArgs || (debugCommand.MaxArgs != -1 && args.Length > debugCommand.MaxArgs))
            {
                Log($"<color=red>Error: Command '{commandName}' expects {debugCommand.MinArgs}" +
                    (debugCommand.MaxArgs == -1 ? " or more" : $" to {debugCommand.MaxArgs}") +
                    $" arguments, but received {args.Length}.</color>");
                Log($"Usage: {commandName} {GetCommandArgsHint(debugCommand)}");
                return;
            }

            try
            {
                debugCommand.Action.Invoke(args); // Execute the command's associated action
            }
            catch (Exception e)
            {
                Log($"<color=red>Error executing command '{commandName}': {e.Message}</color>");
                Debug.LogError($"RuntimeDebugManager: Error executing command '{commandName}': {e}");
            }
        }
        else
        {
            Log($"<color=red>Error: Unknown command '{commandName}'. Type 'help' for a list of commands.</color>");
        }
    }

    /// <summary>
    /// Helper to generate argument hint string for help messages.
    /// </summary>
    private string GetCommandArgsHint(DebugCommand cmd)
    {
        string hint = "";
        if (cmd.MinArgs == 0 && cmd.MaxArgs == 0) return ""; // No args

        // Simple placeholder, can be made more sophisticated
        if (cmd.MaxArgs == -1)
        {
            hint = $"[arg1] [arg2] ... (min {cmd.MinArgs})";
        }
        else if (cmd.MinArgs == cmd.MaxArgs)
        {
            for (int i = 0; i < cmd.MinArgs; i++) hint += $"[arg{i + 1}] ";
        }
        else
        {
            for (int i = 0; i < cmd.MaxArgs; i++)
            {
                if (i < cmd.MinArgs) hint += $"<arg{i + 1}> "; // Required
                else hint += $"[arg{i + 1}] "; // Optional
            }
        }
        return hint.Trim();
    }


    // ====================================================================================
    // --- Command Registration (Core and Examples) ---
    // ====================================================================================

    /// <summary>
    /// Registers essential core commands for the console itself.
    /// </summary>
    private void RegisterCoreCommands()
    {
        RegisterCommand("help", HelpCommand, "Displays a list of all available commands.", 0, 0);
        RegisterCommand("clear", ClearCommand, "Clears the console log.", 0, 0);
        RegisterCommand("log", LogCommand, "Logs a custom message to the console.", 1, -1);
    }

    /// <summary>
    /// Registers example commands to demonstrate the pattern's use.
    /// In a real project, these might be registered by specific game systems.
    /// </summary>
    private void RegisterExampleCommands()
    {
        RegisterCommand("timescale", TimeScaleCommand, "Sets the game's time scale. Usage: timescale <value> (e.g., 0.5, 1, 2)", 1, 1);
        RegisterCommand("quit", QuitGameCommand, "Quits the application.", 0, 0);

        // Example commands that would interact with game-specific systems (PlayerController, ItemSpawner, etc.)
        // These are placeholders as we don't have actual Player or Item scripts in this example.
        RegisterCommand("toggle_invincible", ToggleInvincibilityCommand, "Toggles player invincibility. (Requires PlayerController)", 0, 0);
        RegisterCommand("player_speed", PlayerSpeedCommand, "Sets player movement speed. Usage: player_speed <speed> (Requires PlayerController)", 1, 1);
        RegisterCommand("spawn_item", SpawnItemCommand, "Spawns a specific item. Usage: spawn_item <item_id> [count]. (Requires ItemSpawner)", 1, 2);
    }

    // ====================================================================================
    // --- Command Implementations ---
    // These are the methods that are actually called when a command is executed.
    // ====================================================================================

    /// <summary>
    /// 'help' command: Displays all registered commands and their descriptions.
    /// </summary>
    private void HelpCommand(string[] args)
    {
        Log("<color=yellow>--- Available Commands ---</color>");
        foreach (var cmdEntry in _commands.OrderBy(e => e.Key))
        {
            string cmdName = cmdEntry.Key;
            DebugCommand cmd = cmdEntry.Value;
            string argHint = GetCommandArgsHint(cmd);
            Log($"  <color=lime>{cmdName}</color> {argHint}: {cmd.Description}");
        }
        Log("<color=yellow>-------------------------</color>");
    }

    /// <summary>
    /// 'clear' command: Clears the console log.
    /// </summary>
    private void ClearCommand(string[] args)
    {
        _consoleLog.Clear();
        Log("<color=lime>Console cleared.</color>");
    }

    /// <summary>
    /// 'log' command: Logs a custom message to the console.
    /// </summary>
    private void LogCommand(string[] args)
    {
        string message = string.Join(" ", args);
        Log($"<color=white>Custom Log: {message}</color>");
    }

    /// <summary>
    /// 'timescale' command: Sets the game's Time.timeScale.
    /// </summary>
    private void TimeScaleCommand(string[] args)
    {
        if (float.TryParse(args[0], out float scale))
        {
            Time.timeScale = scale;
            Log($"<color=lime>Time scale set to: {scale}</color>");
        }
        else
        {
            Log($"<color=red>Error: Invalid time scale value. Please provide a number (e.g., 0.5, 1.0).</color>");
        }
    }

    /// <summary>
    /// 'quit' command: Exits the application.
    /// </summary>
    private void QuitGameCommand(string[] args)
    {
        Log("<color=red>Quitting Application...</color>");
        // In editor: stops playing, In build: quits the app
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Placeholder Example Commands (Illustrative) ---
    // These would typically interact with actual game systems.

    private bool _isInvincible = false;
    private void ToggleInvincibilityCommand(string[] args)
    {
        _isInvincible = !_isInvincible;
        Log($"<color=purple>Player Invincibility: {(_isInvincible ? "ENABLED" : "DISABLED")}</color>");
        // In a real project: Call PlayerController.Instance.SetInvincible(_isInvincible);
    }

    private float _playerSpeed = 5.0f;
    private void PlayerSpeedCommand(string[] args)
    {
        if (float.TryParse(args[0], out float speed))
        {
            _playerSpeed = speed;
            Log($"<color=purple>Player Speed set to: {speed}</color>");
            // In a real project: Call PlayerController.Instance.SetSpeed(speed);
        }
        else
        {
            Log($"<color=red>Error: Invalid speed value. Please provide a number.</color>");
        }
    }

    private void SpawnItemCommand(string[] args)
    {
        string itemId = args[0];
        int count = 1;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
        {
            count = parsedCount;
        }

        Log($"<color=purple>Spawning {count} x '{itemId}'. (Mock)</color>");
        // In a real project: Call ItemSpawner.Instance.SpawnItem(itemId, count);
    }


    // ====================================================================================
    // --- GUI Styling and Setup ---
    // ====================================================================================

    /// <summary>
    /// Initializes custom GUI styles for the console elements.
    /// This provides a consistent and readable appearance.
    /// </summary>
    private void InitializeGUIStyles()
    {
        // Console Text Style
        _consoleTextStyle = new GUIStyle
        {
            normal = { textColor = Color.white },
            fontSize = 14,
            wordWrap = true,
            padding = new RectOffset(5, 5, 2, 2)
        };

        // Input Field Style
        _inputFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            normal = { textColor = Color.cyan },
            fontSize = 16,
            padding = new RectOffset(10, 10, 5, 5),
            fixedHeight = 35 // Ensure consistent height
        };

        // Button Style
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.white, background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f, 1f)) },
            hover = { background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 1f)) },
            active = { background = MakeTex(2, 2, new Color(0.1f, 0.3f, 0.5f, 1f)) },
            fontSize = 16,
            fixedHeight = 35 // Match input field height
        };

        // Console Background Texture
        _backgroundTexture = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f)); // Semi-transparent black
    }

    /// <summary>
    /// Helper to create a simple solid color texture for GUI backgrounds.
    /// </summary>
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}


/*
/// --- Example Usage from Another Script ---
/// You would typically register commands related to a specific game system
/// from within that system's script (e.g., PlayerController, GameManager, ItemSpawner).

public class PlayerController : MonoBehaviour
{
    // Assume these are actual properties in your PlayerController
    [SerializeField] private float _currentSpeed = 5f;
    [SerializeField] private bool _isInvincible = false;

    void Awake()
    {
        if (RuntimeDebugManager.Instance != null)
        {
            // Register command to set player speed
            RuntimeDebugManager.Instance.RegisterCommand(
                "player_speed",
                SetPlayerSpeed,
                "Sets the player's movement speed. Usage: player_speed <speed>",
                1, 1
            );

            // Register command to toggle invincibility
            RuntimeDebugManager.Instance.RegisterCommand(
                "toggle_invincible",
                ToggleInvincibility,
                "Toggles player invincibility.",
                0, 0
            );

            // You can also log messages from anywhere:
            RuntimeDebugManager.Instance.Log("<color=green>PlayerController initialized and registered debug commands.</color>");
        }
    }

    // Command handler for 'player_speed'
    private void SetPlayerSpeed(string[] args)
    {
        if (float.TryParse(args[0], out float newSpeed))
        {
            _currentSpeed = newSpeed;
            RuntimeDebugManager.Instance.Log($"<color=lime>Player speed set to: {newSpeed}</color>");
            // Apply speed to player movement logic here
        }
        else
        {
            RuntimeDebugManager.Instance.Log("<color=red>Error: Invalid speed value. Please enter a number.</color>");
        }
    }

    // Command handler for 'toggle_invincible'
    private void ToggleInvincibility(string[] args)
    {
        _isInvincible = !_isInvincible;
        RuntimeDebugManager.Instance.Log($"<color=lime>Player invincibility: {(_isInvincible ? "ENABLED" : "DISABLED")}</color>");
        // Apply invincibility state here
    }

    // Other player logic...
    void Update()
    {
        // Example: If debug console is active and blocking input, don't process player movement
        if (RuntimeDebugManager.Instance != null && RuntimeDebugManager.Instance._isDebugConsoleActive) // _isDebugConsoleActive is private, but for example purposes...
        {
            // Or better: Have RuntimeDebugManager expose a property like IsBlockingGameInput
            // if (RuntimeDebugManager.Instance.IsBlockingGameInput()) return;
        }
        // ... player movement logic here ...
    }
}

*/
```