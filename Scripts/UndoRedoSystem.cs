// Unity Design Pattern Example: UndoRedoSystem
// This script demonstrates the UndoRedoSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the Undo/Redo design pattern. It includes a flexible `UndoRedoSystem` and example `ICommand` implementations for moving a `GameObject` and changing its color. The demo manager allows easy interaction via the Unity Editor's Context Menu.

---

### How to Use This Script in Unity:

1.  **Create a New C# Script**: In your Unity project, create a new C# script (e.g., right-click in Project window > Create > C# Script) and name it `UndoRedoDemoManager`.
2.  **Copy and Paste**: Copy the entire code below and paste it into your new `UndoRedoDemoManager.cs` file, overwriting its default contents.
3.  **Create a Target Object**: In your Unity scene, create a 3D object (e.g., `GameObject > 3D Object > Cube`). You can name it `UndoRedoTarget`.
4.  **Create an Empty GameObject**: Create an empty GameObject in your scene (e.g., `GameObject > Create Empty`). Name it something like `UndoRedoSystemManager`.
5.  **Attach the Script**: Drag and drop the `UndoRedoDemoManager.cs` script from your Project window onto the `UndoRedoSystemManager` GameObject in your Hierarchy.
6.  **Assign Target Object**: Select the `UndoRedoSystemManager` GameObject in the Hierarchy. In the Inspector, you will see a field named `Target Object`. Drag your `UndoRedoTarget` Cube (or any other GameObject with a `Renderer`) from the Hierarchy into this `Target Object` slot.
7.  **Run the Demo**:
    *   You can enter Play Mode to see the console logs.
    *   Select the `UndoRedoSystemManager` in the Hierarchy.
    *   In its Inspector, click the small **gear icon (☰)** next to the `UndoRedoDemoManager` component title.
    *   A context menu will appear with actions like "Execute: Move Object Randomly", "Undo Last Action", etc.
    *   Click these menu items to observe the `UndoRedoTarget` Cube moving and changing color, and how the undo/redo operations affect it. Watch the Console for detailed logs.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

// --- 1. The ICommand Interface ---
/// <summary>
/// Represents an abstract command that can be executed and unexecuted.
/// This is the core interface of the Command design pattern, crucial for Undo/Redo functionality.
/// Any action that needs to be undoable/redoable must implement this interface.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command, performing the desired action.
    /// This method should modify the state of the target object.
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverses the command, undoing the action performed by Execute().
    /// This method should restore the target object to its state before Execute() was called.
    /// </summary>
    void Unexecute();
}

// --- 2. Concrete Command Implementations ---
// These classes define specific actions that can be performed and reverted.

/// <summary>
/// A concrete command to move a GameObject to a new position.
/// It stores the necessary information (target, old position, new position)
/// to perform and reverse the move operation.
/// </summary>
public class MoveObjectCommand : ICommand
{
    private Transform _targetTransform; // The Transform component of the GameObject to move.
    private Vector3 _oldPosition;       // The position of the GameObject before this command was executed.
    private Vector3 _newPosition;       // The target position to move the GameObject to.

    /// <summary>
    /// Initializes a new instance of the MoveObjectCommand.
    /// </summary>
    /// <param name="targetTransform">The Transform component of the GameObject to move.</param>
    /// <param name="oldPosition">The position of the GameObject before this command was executed.</param>
    /// <param name="newPosition">The target position to move the GameObject to.</param>
    public MoveObjectCommand(Transform targetTransform, Vector3 oldPosition, Vector3 newPosition)
    {
        _targetTransform = targetTransform;
        _oldPosition = oldPosition;
        _newPosition = newPosition;
    }

    /// <summary>
    /// Moves the target GameObject to the new position.
    /// </summary>
    public void Execute()
    {
        _targetTransform.position = _newPosition;
        Debug.Log($"<color=cyan>Executed:</color> Moved <color=yellow>{_targetTransform.name}</color> from {_oldPosition} to <color=green>{_newPosition}</color>");
    }

    /// <summary>
    /// Moves the target GameObject back to its old position.
    /// </summary>
    public void Unexecute()
    {
        _targetTransform.position = _oldPosition;
        Debug.Log($"<color=orange>Undone:</color> Restored <color=yellow>{_targetTransform.name}</color> to <color=red>{_oldPosition}</color>");
    }
}

/// <summary>
/// A concrete command to change the color of a GameObject's material.
/// It stores the necessary information (target renderer, old color, new color)
/// to perform and reverse the color change operation.
/// </summary>
public class ChangeColorCommand : ICommand
{
    private Renderer _targetRenderer; // The Renderer component of the GameObject whose color will be changed.
    private Color _oldColor;          // The color of the material before this command was executed.
    private Color _newColor;          // The target color to set the material to.

    /// <summary>
    /// Initializes a new instance of the ChangeColorCommand.
    /// </summary>
    /// <param name="targetRenderer">The Renderer component of the GameObject whose color will be changed.</param>
    /// <param name="oldColor">The color of the material before this command was executed.</param>
    /// <param name="newColor">The target color to set the material to.</param>
    public ChangeColorCommand(Renderer targetRenderer, Color oldColor, Color newColor)
    {
        _targetRenderer = targetRenderer;
        _oldColor = oldColor;
        _newColor = newColor;
    }

    /// <summary>
    /// Sets the target GameObject's material color to the new color.
    /// IMPORTANT: Accessing `renderer.material` in Unity creates an instance of the material
    /// if the renderer is currently using a shared material. This is generally desired for
    /// per-object color changes in a runtime undo/redo system to avoid modifying shared assets.
    /// If you wish to modify the *shared* material (e.g., for all objects using it), you'd use
    /// `renderer.sharedMaterial.color` but this is rarely desired for undo/redo on a single object.
    /// </summary>
    public void Execute()
    {
        _targetRenderer.material.color = _newColor;
        Debug.Log($"<color=cyan>Executed:</color> Changed <color=yellow>{_targetRenderer.name}</color> color from {_oldColor} to <color=green>{_newColor}</color>");
    }

    /// <summary>
    /// Sets the target GameObject's material color back to its old color.
    /// </summary>
    public void Unexecute()
    {
        _targetRenderer.material.color = _oldColor;
        Debug.Log($"<color=orange>Undone:</color> Restored <color=yellow>{_targetRenderer.name}</color> color to <color=red>{_oldColor}</color>");
    }
}


// --- 3. The UndoRedoSystem (Invoker/History Manager) ---
/// <summary>
/// Manages a history of executed commands, allowing for undo and redo operations.
/// This class acts as the invoker in the Command pattern, responsible for
/// storing commands, executing them, and orchestrating undo/redo requests.
/// </summary>
public class UndoRedoSystem
{
    private List<ICommand> _history = new List<ICommand>(); // Stores the sequence of executed commands.
    private int _currentIndex = -1; // Points to the last command that was executed/redone.
                                    // -1 means no commands have been executed yet.

    /// <summary>
    /// Checks if there are any commands available to be undone.
    /// True if _currentIndex is 0 or greater (meaning at least one command exists and is "active").
    /// </summary>
    public bool CanUndo => _currentIndex >= 0;

    /// <summary>
    /// Checks if there are any commands available to be redone.
    /// True if _currentIndex is less than the last index of the history list.
    /// </summary>
    public bool CanRedo => _currentIndex < _history.Count - 1;

    /// <summary>
    /// Executes a given command and adds it to the history.
    /// If new commands are executed after some 'undo' operations,
    /// all 'redoable' commands (future history) are discarded to maintain a linear timeline.
    /// </summary>
    /// <param name="command">The command to execute and add to history.</param>
    public void ExecuteCommand(ICommand command)
    {
        // If we are not at the end of the history (i.e., we've undone some commands),
        // executing a new command should truncate the "redoable" future history.
        // Example: History is [CmdA, CmdB, CmdC]. _currentIndex is 1 (CmdB).
        // If CmdD is executed, CmdC is removed, history becomes [CmdA, CmdB, CmdD].
        if (_currentIndex < _history.Count - 1)
        {
            // Remove all commands after the current index.
            _history.RemoveRange(_currentIndex + 1, _history.Count - (_currentIndex + 1));
        }

        command.Execute(); // Perform the action defined by the command.
        _history.Add(command); // Add the command to our history list.
        _currentIndex++; // Move the pointer to the newly added command.

        Debug.Log($"<color=lightblue>History:</color> Executed command '{command.GetType().Name}'. Current history size: {_history.Count}, Current index: {_currentIndex}");
    }

    /// <summary>
    /// Undoes the last executed command.
    /// Moves the current index back by one and calls Unexecute() on that command.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo)
        {
            Debug.Log("<color=red>Cannot Undo:</color> No commands in history to undo.");
            return;
        }

        ICommand command = _history[_currentIndex]; // Get the command at the current index.
        command.Unexecute(); // Revert the action performed by this command.
        _currentIndex--; // Move the pointer back one step.

        Debug.Log($"<color=lightblue>History:</color> Undone command '{command.GetType().Name}'. Current history size: {_history.Count}, Current index: {_currentIndex}");
    }

    /// <summary>
    /// Redoes the previously undone command.
    /// Moves the current index forward by one and calls Execute() on that command.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo)
        {
            Debug.Log("<color=red>Cannot Redo:</color> No future commands to redo.");
            return;
        }

        _currentIndex++; // Move the pointer forward one step.
        ICommand command = _history[_currentIndex]; // Get the command at the new current index.
        command.Execute(); // Re-apply the action performed by this command.

        Debug.Log($"<color=lightblue>History:</color> Redone command '{command.GetType().Name}'. Current history size: {_history.Count}, Current index: {_currentIndex}");
    }

    /// <summary>
    /// Clears the entire undo/redo history. This means all past actions are forgotten.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear(); // Empty the list of commands.
        _currentIndex = -1; // Reset the index, indicating no active commands.
        Debug.Log("<color=lightblue>History:</color> Undo/Redo history cleared.");
    }
}


// --- 4. Demo Manager (Monobehaviour) ---
/// <summary>
/// A Unity MonoBehaviour script to demonstrate the UndoRedoSystem.
/// This acts as a client that creates and executes commands based on user input
/// (or, in this case, Context Menu interactions in the Inspector).
/// It links concrete actions to the generic UndoRedoSystem.
/// </summary>
public class UndoRedoDemoManager : MonoBehaviour
{
    [Tooltip("The GameObject whose position and color will be manipulated.")]
    public GameObject targetObject;

    [Tooltip("The range for random movement along X and Z axes.")]
    public float movementRange = 5f;

    // The instance of our UndoRedoSystem that will manage command history.
    private UndoRedoSystem _undoRedoSystem = new UndoRedoSystem();
    private Renderer _targetRenderer; // Caching the Renderer for color changes.

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes necessary components and performs checks.
    /// </summary>
    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target Object not assigned! Please assign a GameObject in the inspector to 'UndoRedoDemoManager'.");
            enabled = false; // Disable the script if no target to prevent NullReferenceExceptions.
            return;
        }

        _targetRenderer = targetObject.GetComponent<Renderer>();
        if (_targetRenderer == null)
        {
            Debug.LogWarning("Target Object needs a Renderer component to change color! Please add one (e.g., MeshRenderer). Movement commands will still work.");
        }

        Debug.Log("UndoRedoDemoManager initialized. Use the Context Menu (☰ icon in Inspector) on this component to interact.");
    }

    /// <summary>
    /// Creates and executes a command to move the target object to a random new position.
    /// This method is exposed in the Inspector's Context Menu for easy testing.
    /// </summary>
    [ContextMenu("Execute: Move Object Randomly")]
    public void MoveObjectRandomly()
    {
        if (targetObject == null) return;

        Vector3 oldPosition = targetObject.transform.position;
        Vector3 newPosition = new Vector3(
            UnityEngine.Random.Range(-movementRange, movementRange),
            oldPosition.y, // Keep Y constant for simplicity in this demo.
            UnityEngine.Random.Range(-movementRange, movementRange)
        );

        // Create a new MoveObjectCommand with the current and target positions.
        ICommand moveCommand = new MoveObjectCommand(targetObject.transform, oldPosition, newPosition);
        // Execute the command via the UndoRedoSystem, which also adds it to history.
        _undoRedoSystem.ExecuteCommand(moveCommand);
    }

    /// <summary>
    /// Creates and executes a command to change the target object's color to a random color.
    /// This method is exposed in the Inspector's Context Menu for easy testing.
    /// </summary>
    [ContextMenu("Execute: Change Object Color")]
    public void ChangeObjectColor()
    {
        if (_targetRenderer == null)
        {
            Debug.LogWarning("No Renderer found on target object. Cannot change color.");
            return;
        }

        Color oldColor = _targetRenderer.material.color;
        Color newColor = new Color(
            UnityEngine.Random.value, // Random R
            UnityEngine.Random.value, // Random G
            UnityEngine.Random.value  // Random B
        );

        // Create a new ChangeColorCommand with the current and target colors.
        ICommand colorCommand = new ChangeColorCommand(_targetRenderer, oldColor, newColor);
        // Execute the command via the UndoRedoSystem.
        _undoRedoSystem.ExecuteCommand(colorCommand);
    }

    /// <summary>
    /// Triggers the Undo operation in the UndoRedoSystem.
    /// Exposed in the Inspector's Context Menu.
    /// </summary>
    [ContextMenu("Undo Last Action (Ctrl+Z)")]
    public void UndoLastAction()
    {
        _undoRedoSystem.Undo();
    }

    /// <summary>
    /// Triggers the Redo operation in the UndoRedoSystem.
    /// Exposed in the Inspector's Context Menu.
    /// </summary>
    [ContextMenu("Redo Last Action (Ctrl+Y)")]
    public void RedoLastAction()
    {
        _undoRedoSystem.Redo();
    }

    /// <summary>
    /// Clears the entire undo/redo history in the UndoRedoSystem.
    /// Exposed in the Inspector's Context Menu.
    /// </summary>
    [ContextMenu("Clear Undo/Redo History")]
    public void ClearHistory()
    {
        _undoRedoSystem.ClearHistory();
    }

    // --- Example Usage in Comments ---
    /*
    This section illustrates how you would integrate the UndoRedoSystem into your own Unity project.

    1.  **Define Your Actions as Commands**:
        For every discrete action you want to be undoable/redoable, create a class that implements `ICommand`.
        This class should encapsulate all the logic and state needed to `Execute()` and `Unexecute()` that specific action.

        ```csharp
        public class MyCustomActionCommand : ICommand
        {
            private MyCustomData _targetData; // Reference to the object/data model being manipulated
            private object _oldState;         // State of _targetData before Execute()
            private object _newState;         // State of _targetData after Execute()

            public MyCustomActionCommand(MyCustomData targetData, object oldState, object newState)
            {
                _targetData = targetData;
                _oldState = oldState;
                _newState = newState;
            }

            public void Execute()
            {
                // Apply the new state to your data model or scene objects
                _targetData.ApplyState(_newState);
                Debug.Log("Custom action executed.");
            }

            public void Unexecute()
            {
                // Revert to the old state
                _targetData.ApplyState(_oldState);
                Debug.Log("Custom action undone.");
            }
        }
        ```

    2.  **Instantiate and Use the UndoRedoSystem**:
        In your game manager, editor tool, or any relevant class, create an instance of `UndoRedoSystem`.
        This is where you'll trigger the creation and execution of commands.

        ```csharp
        public class MyGameManager : MonoBehaviour
        {
            private UndoRedoSystem _gameUndoRedoSystem = new UndoRedoSystem();
            public MyCustomData gameData; // Example: A ScriptableObject or custom class holding game data

            // Example: A method called when a user performs an action
            public void UserChangesGameSetting(object newSettingValue)
            {
                object oldSettingValue = gameData.GetSetting(); // Capture current state BEFORE modification
                // Perform the actual change (e.g., gameData.SetSetting(newSettingValue);)
                // Then, create and execute the command:
                ICommand changeCommand = new MyCustomActionCommand(gameData, oldSettingValue, newSettingValue);
                _gameUndoRedoSystem.ExecuteCommand(changeCommand);
            }

            // Bind Undo/Redo to UI buttons or keyboard shortcuts (e.g., Ctrl+Z, Ctrl+Y):
            void Update()
            {
                // Example for Ctrl+Z (Undo)
                if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    _gameUndoRedoSystem.Undo();
                }
                // Example for Ctrl+Y (Redo)
                if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    _gameUndoRedoSystem.Redo();
                }
            }
        }
        ```

    3.  **Ensure State Capture**:
        When creating a command, it is critically important to capture the `oldState` *before* the actual modification occurs and *before* `Execute()` is called. The `newState` should represent the desired state after the command's execution. The `Execute()` method of the command then applies the `newState`, and `Unexecute()` applies the `oldState`. This ensures a faithful restoration of state.
    */
}
```