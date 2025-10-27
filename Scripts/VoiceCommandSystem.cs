// Unity Design Pattern Example: VoiceCommandSystem
// This script demonstrates the VoiceCommandSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'VoiceCommandSystem' design pattern, as conceptualized here, is an application of the **Command Pattern** combined with a mechanism for mapping "voice commands" (simulated as strings in Unity) to executable actions. It provides a flexible and extensible way to define, register, and execute commands based on user input, making it ideal for systems that need to respond to specific phrases or keywords.

**Key Components of the VoiceCommandSystem Pattern:**

1.  **`VoiceCommand` (Abstract Command):** An abstract base class or interface that defines the contract for all commands. It typically includes a `CommandPhrase` (the string that triggers the command) and an `Execute()` method (the action to be performed).

2.  **`ConcreteCommand` (Concrete Commands):** Specific implementations of the `VoiceCommand`. Each concrete command encapsulates a request as an object, including all information needed to perform the action. Examples: `ChangeColorCommand`, `MoveObjectCommand`, `LogMessageCommand`. These commands often have a "Receiver" – an object that actually performs the work (e.g., a `Renderer` for color changes, a `Transform` for movement).

3.  **`VoiceCommandSystem` (Invoker/Processor):** This is the central hub. It maintains a collection (e.g., a dictionary) of registered `VoiceCommand` objects, mapped by their `CommandPhrase`. When it receives an "input phrase" (simulated voice input), it looks up the corresponding command and calls its `Execute()` method. It doesn't know *what* the command does, only that it has an `Execute()` method.

**Benefits:**

*   **Decoupling:** The `VoiceCommandSystem` (invoker) is decoupled from the `ConcreteCommand` (action) and the `Receiver` (object performing the action). New commands can be added without modifying the invoker.
*   **Extensibility:** Easily add new voice commands by creating new `ConcreteCommand` classes and registering them.
*   **Undo/Redo (Potential):** With slight modifications (e.g., adding an `Undo()` method to `VoiceCommand`), this pattern can support undo/redo functionality.
*   **Parameterization:** Commands can be parameterized with context-specific data at creation time.

---

Here's a complete C# Unity script demonstrating the VoiceCommandSystem pattern.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary
using System; // Required for Action delegate (optional, but useful)

namespace DesignPatterns.VoiceCommandSystem
{
    /// <summary>
    /// ABSTRACT COMMAND:
    /// Defines the interface for all voice commands.
    /// Each command must have a phrase that triggers it and an Execute method.
    /// </summary>
    public abstract class VoiceCommand
    {
        // The phrase (keyword or sentence) that will trigger this command.
        // It's set by concrete commands and normalized to lowercase for comparison.
        public string CommandPhrase { get; protected set; }

        /// <summary>
        /// The action to be performed when this command is executed.
        /// Concrete commands must implement this method.
        /// </summary>
        public abstract void Execute();
    }

    /// <summary>
    /// CONCRETE COMMAND 1: ChangeColorCommand
    /// Changes the color of a target Renderer component.
    /// This command acts upon a "Receiver" (the Renderer).
    /// </summary>
    public class ChangeColorCommand : VoiceCommand
    {
        private Renderer targetRenderer; // The receiver of this command
        private Color newColor;          // Parameter for the command

        /// <summary>
        /// Constructor for ChangeColorCommand.
        /// </summary>
        /// <param name="phrase">The voice phrase to trigger this command (e.g., "change to red").</param>
        /// <param name="renderer">The Renderer component whose color will be changed.</param>
        /// <param name="color">The new color to set.</param>
        public ChangeColorCommand(string phrase, Renderer renderer, Color color)
        {
            CommandPhrase = phrase.ToLower().Trim(); // Normalize phrase for case-insensitive matching
            targetRenderer = renderer;
            newColor = color;
        }

        /// <summary>
        /// Executes the command: sets the target renderer's material color.
        /// </summary>
        public override void Execute()
        {
            if (targetRenderer != null)
            {
                targetRenderer.material.color = newColor;
                Debug.Log($"<color=cyan>Voice Command Executed:</color> '{CommandPhrase}' - Changed <color=yellow>{targetRenderer.name}</color> to {newColor}.");
            }
            else
            {
                Debug.LogWarning($"<color=red>Voice Command Warning:</color> '{CommandPhrase}' - Target renderer is null. Command cannot execute.");
            }
        }
    }

    /// <summary>
    /// CONCRETE COMMAND 2: MoveObjectCommand
    /// Moves a target Transform component by a specified vector.
    /// This command acts upon a "Receiver" (the Transform).
    /// </summary>
    public class MoveObjectCommand : VoiceCommand
    {
        private Transform targetTransform; // The receiver of this command
        private Vector3 moveAmount;        // Parameter for the command

        /// <summary>
        /// Constructor for MoveObjectCommand.
        /// </summary>
        /// <param name="phrase">The voice phrase to trigger this command (e.g., "move forward").</param>
        /// <param name="transform">The Transform component to move.</param>
        /// <param name="amount">The vector by which to move the transform.</param>
        public MoveObjectCommand(string phrase, Transform transform, Vector3 amount)
        {
            CommandPhrase = phrase.ToLower().Trim(); // Normalize phrase
            targetTransform = transform;
            moveAmount = amount;
        }

        /// <summary>
        /// Executes the command: moves the target transform.
        /// </summary>
        public override void Execute()
        {
            if (targetTransform != null)
            {
                targetTransform.Translate(moveAmount, Space.World);
                Debug.Log($"<color=cyan>Voice Command Executed:</color> '{CommandPhrase}' - Moved <color=yellow>{targetTransform.name}</color> by {moveAmount}. New position: {targetTransform.position}");
            }
            else
            {
                Debug.LogWarning($"<color=red>Voice Command Warning:</color> '{CommandPhrase}' - Target transform is null. Command cannot execute.");
            }
        }
    }

    /// <summary>
    /// CONCRETE COMMAND 3: LogMessageCommand
    /// Simply logs a message to the console. This command doesn't have a direct "Receiver"
    /// in the traditional sense, but it still performs an action.
    /// </summary>
    public class LogMessageCommand : VoiceCommand
    {
        private string messageToLog; // Parameter for the command

        /// <summary>
        /// Constructor for LogMessageCommand.
        /// </summary>
        /// <param name="phrase">The voice phrase to trigger this command (e.g., "say hello").</param>
        /// <param name="message">The message string to log.</param>
        public LogMessageCommand(string phrase, string message)
        {
            CommandPhrase = phrase.ToLower().Trim(); // Normalize phrase
            messageToLog = message;
        }

        /// <summary>
        /// Executes the command: logs the stored message.
        /// </summary>
        public override void Execute()
        {
            Debug.Log($"<color=cyan>Voice Command Executed:</color> '{CommandPhrase}' - Message: <color=lime>{messageToLog}</color>");
        }
    }

    /// <summary>
    /// INVOKER / PROCESSOR: VoiceCommandSystem
    /// This MonoBehaviour acts as the central hub for registering and processing voice commands.
    /// It maps command phrases to VoiceCommand objects and executes them when triggered.
    /// </summary>
    public class VoiceCommandSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [Tooltip("Type a command phrase here and use the 'Process Simulated Input' button.")]
        [SerializeField] private string simulatedInputPhrase = "";

        // Dictionary to store all registered commands, mapping the command phrase to the VoiceCommand object.
        private Dictionary<string, VoiceCommand> registeredCommands = new Dictionary<string, VoiceCommand>();

        [Header("Command Receivers (Assign in Inspector)")]
        [Tooltip("Drag the GameObject whose Renderer you want to control (e.g., a Cube).")]
        [SerializeField] private Renderer cubeRenderer;
        [Tooltip("Drag the GameObject whose Transform you want to control (e.g., a Sphere).")]
        [SerializeField] private Transform sphereTransform;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// Use it to initialize the system and register all default commands.
        /// </summary>
        void Awake()
        {
            Debug.Log("<color=green>VoiceCommandSystem:</color> Initializing and registering commands...");
            RegisterDefaultCommands();
            Debug.Log($"<color=green>VoiceCommandSystem:</color> {registeredCommands.Count} commands registered.");
        }

        /// <summary>
        /// Registers a new VoiceCommand with the system.
        /// </summary>
        /// <param name="command">The VoiceCommand object to register.</param>
        public void RegisterCommand(VoiceCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.CommandPhrase))
            {
                Debug.LogError("<color=red>VoiceCommandSystem Error:</color> Attempted to register a null or empty-phrased command.");
                return;
            }

            string normalizedPhrase = command.CommandPhrase.ToLower().Trim();

            if (registeredCommands.ContainsKey(normalizedPhrase))
            {
                Debug.LogWarning($"<color=orange>VoiceCommandSystem Warning:</color> Command '{normalizedPhrase}' already registered. Overwriting with new command.");
                registeredCommands[normalizedPhrase] = command; // Overwrite existing command
            }
            else
            {
                registeredCommands.Add(normalizedPhrase, command);
                // Debug.Log($"<color=green>VoiceCommandSystem:</color> Registered command: '{normalizedPhrase}'");
            }
        }

        /// <summary>
        /// Unregisters a command by its phrase.
        /// </summary>
        /// <param name="phrase">The phrase of the command to unregister.</param>
        public void UnregisterCommand(string phrase)
        {
            string normalizedPhrase = phrase.ToLower().Trim();
            if (registeredCommands.Remove(normalizedPhrase))
            {
                Debug.Log($"<color=green>VoiceCommandSystem:</color> Unregistered command: '{normalizedPhrase}'");
            }
            else
            {
                Debug.LogWarning($"<color=red>VoiceCommandSystem Warning:</color> Attempted to unregister unknown command: '{normalizedPhrase}'");
            }
        }

        /// <summary>
        /// Processes a simulated voice input phrase.
        /// This method can be called by a UI Button's OnClick event or other input systems.
        /// </summary>
        public void ProcessSimulatedVoiceInput()
        {
            if (!string.IsNullOrWhiteSpace(simulatedInputPhrase))
            {
                Debug.Log($"<color=blue>VoiceCommandSystem:</color> Processing input: '{simulatedInputPhrase}'");
                ProcessVoiceInput(simulatedInputPhrase);
            }
            else
            {
                Debug.LogWarning("<color=red>VoiceCommandSystem Warning:</color> Simulated input phrase is empty. Please type a command.");
            }
        }

        /// <summary>
        /// The core method to process a given voice input string.
        /// It normalizes the input, looks up the command, and executes it.
        /// </summary>
        /// <param name="inputPhrase">The raw voice input string.</param>
        private void ProcessVoiceInput(string inputPhrase)
        {
            // Normalize the input phrase for case-insensitive matching
            string normalizedInput = inputPhrase.ToLower().Trim();

            // Try to retrieve the command from the dictionary
            if (registeredCommands.TryGetValue(normalizedInput, out VoiceCommand command))
            {
                // If a command is found, execute it
                command.Execute();
            }
            else
            {
                // If no command is found, log a warning
                Debug.LogWarning($"<color=red>VoiceCommandSystem Warning:</color> Unknown voice command: '{inputPhrase}'. No command registered for this phrase.");
            }
        }

        /// <summary>
        /// Helper method to register a set of common commands.
        /// This is where you define your application-specific commands.
        /// </summary>
        private void RegisterDefaultCommands()
        {
            // --- Register ChangeColorCommands ---
            if (cubeRenderer != null)
            {
                RegisterCommand(new ChangeColorCommand("change cube to red", cubeRenderer, Color.red));
                RegisterCommand(new ChangeColorCommand("change cube to blue", cubeRenderer, Color.blue));
                RegisterCommand(new ChangeColorCommand("change cube to green", cubeRenderer, Color.green));
                RegisterCommand(new ChangeColorCommand("reset cube color", cubeRenderer, Color.white));
            }
            else
            {
                Debug.LogWarning("<color=orange>VoiceCommandSystem Warning:</color> Cube Renderer not assigned. Color commands will not be registered.");
            }

            // --- Register MoveObjectCommands ---
            if (sphereTransform != null)
            {
                RegisterCommand(new MoveObjectCommand("move sphere forward", sphereTransform, Vector3.forward * 2f));
                RegisterCommand(new MoveObjectCommand("move sphere back", sphereTransform, Vector3.back * 2f));
                RegisterCommand(new MoveObjectCommand("move sphere right", sphereTransform, Vector3.right * 2f));
                RegisterCommand(new MoveObjectCommand("move sphere left", sphereTransform, Vector3.left * 2f));
            }
            else
            {
                Debug.LogWarning("<color=orange>VoiceCommandSystem Warning:</color> Sphere Transform not assigned. Movement commands will not be registered.");
            }

            // --- Register LogMessageCommands ---
            RegisterCommand(new LogMessageCommand("log hello", "Hello from the Log Message Command!"));
            RegisterCommand(new LogMessageCommand("show commands", 
                "Available commands:\n" +
                "- Change cube to red/blue/green/reset color\n" +
                "- Move sphere forward/back/right/left\n" +
                "- Log hello\n" +
                "- Show commands\n" +
                "Try typing these into the 'Simulated Input Phrase' field and clicking 'Process Simulated Input'."));
        }

        /// <summary>
        /// Example of how to add a custom command from another script at runtime.
        /// </summary>
        /// <param name="action">The Unity Action to execute.</param>
        public void AddCustomRuntimeCommand(string phrase, Action action)
        {
            // A simple command using an Action delegate for quick, inline command creation.
            // This is useful for commands defined at runtime without needing a separate class file.
            RegisterCommand(new DelegateCommand(phrase, action));
        }

        /// <summary>
        /// CONCRETE COMMAND (Delegate-based for runtime flexibility):
        /// A command that encapsulates a generic C# Action delegate.
        /// Useful for commands defined dynamically or inline.
        /// </summary>
        public class DelegateCommand : VoiceCommand
        {
            private Action actionToPerform;

            public DelegateCommand(string phrase, Action action)
            {
                CommandPhrase = phrase.ToLower().Trim();
                actionToPerform = action;
            }

            public override void Execute()
            {
                if (actionToPerform != null)
                {
                    Debug.Log($"<color=cyan>Voice Command Executed:</color> '{CommandPhrase}' - Invoking delegate action.");
                    actionToPerform.Invoke();
                }
                else
                {
                    Debug.LogWarning($"<color=red>Voice Command Warning:</color> '{CommandPhrase}' - Delegate action is null. Command cannot execute.");
                }
            }
        }
    }
}

/*
    =======================================================================================
    HOW TO USE THIS SCRIPT IN UNITY:
    =======================================================================================

    1.  Create a new C# script named "VoiceCommandSystem.cs" in your Unity project.
    2.  Copy and paste the entire code above into this new script.
    3.  Create an Empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty).
    4.  Rename this GameObject to "VoiceCommandSystemController".
    5.  Drag the "VoiceCommandSystem.cs" script onto the "VoiceCommandSystemController" GameObject in the Inspector.

    6.  Create a 3D Cube:
        - Right-click in Hierarchy -> 3D Object -> Cube.
        - Rename it to "MyCube".
        - In the Inspector of "VoiceCommandSystemController", drag "MyCube" from the Hierarchy
          into the "Cube Renderer" slot. The script will automatically find its Renderer component.

    7.  Create a 3D Sphere:
        - Right-click in Hierarchy -> 3D Object -> Sphere.
        - Rename it to "MySphere".
        - In the Inspector of "VoiceCommandSystemController", drag "MySphere" from the Hierarchy
          into the "Sphere Transform" slot. The script will automatically find its Transform component.

    8.  (Optional - For UI interaction):
        - Create a UI Input Field: Right-click in Hierarchy -> UI -> Input Field (TextMeshPro).
          (If you use TextMeshPro, import its essential resources when prompted).
        - Create a UI Button: Right-click in Hierarchy -> UI -> Button (TextMeshPro).
        - Select the "VoiceCommandSystemController" GameObject.
        - In its Inspector, you'll see a field "Simulated Input Phrase".
        - Select the Button GameObject in your scene.
        - In the Button's Inspector, find the "On Click ()" section.
        - Click the "+" button to add a new event.
        - Drag the "VoiceCommandSystemController" GameObject from the Hierarchy into the "None (Object)" slot.
        - From the dropdown menu that appears (next to "No Function"), navigate to:
          `DesignPatterns.VoiceCommandSystem` -> `VoiceCommandSystem` -> `ProcessSimulatedVoiceInput()`.

    9.  Run the scene:
        - If using the UI: Type a command (e.g., "change cube to red", "move sphere forward", "log hello", "show commands")
          into the Input Field and click the Button. Observe the console and the objects.
        - Without UI: You can directly type a command into the "Simulated Input Phrase" field
          in the Inspector of "VoiceCommandSystemController" while the game is running,
          and then click the "Process Simulated Input" button below it in the inspector
          (a custom editor would be needed to make a button appear, but simply changing the string
          and calling `ProcessSimulatedVoiceInput` from another script would work).

    EXAMPLE OF ADDING A CUSTOM COMMAND FROM ANOTHER SCRIPT AT RUNTIME:
    ------------------------------------------------------------------
    Let's say you have another script, `GameController.cs`:

    ```csharp
    using UnityEngine;
    using DesignPatterns.VoiceCommandSystem; // Don't forget this!

    public class GameController : MonoBehaviour
    {
        public VoiceCommandSystem voiceSystem;

        void Start()
        {
            if (voiceSystem != null)
            {
                // Register a new command at runtime
                voiceSystem.AddCustomRuntimeCommand("spawn effect", () => {
                    Debug.Log("Custom Command: Spawning a visual effect!");
                    // GameObject effect = Instantiate(myEffectPrefab, Vector3.zero, Quaternion.identity);
                    // Destroy(effect, 3f);
                });

                voiceSystem.AddCustomRuntimeCommand("quit game", () => {
                    Debug.Log("Custom Command: Quitting the application!");
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });

                Debug.Log("Added custom 'spawn effect' and 'quit game' commands dynamically.");
            }
            else
            {
                Debug.LogError("VoiceCommandSystem not assigned to GameController.");
            }
        }
    }
    ```
    - Attach `GameController.cs` to any GameObject.
    - Drag the "VoiceCommandSystemController" GameObject into the `voiceSystem` slot of `GameController`'s Inspector.
    - Now, when you run the game and input "spawn effect" or "quit game", these new commands will be recognized!

*/
```