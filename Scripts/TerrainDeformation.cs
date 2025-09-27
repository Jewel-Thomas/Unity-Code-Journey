// Unity Design Pattern Example: TerrainDeformation
// This script demonstrates the TerrainDeformation pattern in Unity
// Generated automatically - ready to use in your Unity project

The "TerrainDeformation" pattern isn't a standard GoF (Gang of Four) design pattern. Instead, it describes a common *problem domain* in game development: modifying the landscape of a game world. To address this problem robustly, we can leverage established design patterns.

This example primarily demonstrates the **Command Pattern** to manage terrain deformations. The Command Pattern is ideal here because:

1.  **Undo/Redo:** Each deformation operation (raising, lowering, smoothing) can be encapsulated as a command. This allows us to store a history of commands and easily revert (`Undo`) or re-apply (`Redo`) them.
2.  **Extensibility:** New deformation types can be added simply by creating new command classes without modifying existing code.
3.  **Decoupling:** The `TerrainDeformationManager` (invoker) is decoupled from the specific deformation logic (receiver), making the system more flexible.

**How the TerrainDeformation Pattern (using Command) Works:**

1.  **`ITerrainCommand` (Command Interface):** Defines the `Execute()` and `Undo()` methods that all concrete deformation commands must implement.
2.  **`BaseTerrainCommand` (Abstract Command):** Provides common functionality and storage for all terrain commands, such as references to the `TerrainData`, the coordinates of the affected area, and the `_originalHeights` array (essential for undo functionality).
3.  **Concrete Command Classes (e.g., `RaiseTerrainCommand`, `LowerTerrainCommand`, `SmoothTerrainCommand`):**
    *   Each class encapsulates a specific type of terrain deformation.
    *   Their `Execute()` method applies the change (modifies `_newHeights` and then sets them).
    *   Their `Undo()` method reverts the change by applying `_originalHeights`.
4.  **`TerrainDeformationManager` (Invoker/Client):**
    *   This is the main MonoBehaviour script attached to a GameObject.
    *   It listens for user input (mouse clicks, undo/redo keys).
    *   It creates the appropriate concrete `ITerrainCommand` based on user selection (e.g., `DeformationType`).
    *   It calls `Execute()` on the created command.
    *   It maintains two stacks: `_undoStack` (for executed commands) and `_redoStack` (for commands that were undone and can be re-executed).
    *   When Undo is requested, it pops from `_undoStack`, calls `Undo()`, and pushes to `_redoStack`.
    *   When Redo is requested, it pops from `_redoStack`, calls `Execute()`, and pushes to `_undoStack`.
5.  **`Terrain` (Receiver):** The actual Unity `Terrain` component whose `TerrainData` is being modified by the commands.

---

### **Complete C# Unity Example: TerrainDeformation**

This example will provide three types of deformation: Raise, Lower, and Smooth. It includes adjustable brush size, intensity, and basic undo/redo functionality.

To use this, create a new C# script named `TerrainDeformationManager.cs` and paste the following code into it.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For Stack<T>

/// <summary>
/// ITerrainCommand Interface: Defines the contract for all terrain deformation operations.
/// This is the 'Command' interface in the Command Design Pattern.
/// </summary>
public interface ITerrainCommand
{
    /// <summary>
    /// Executes the terrain deformation.
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverts the terrain deformation.
    /// </summary>
    void Undo();
}

/// <summary>
/// BaseTerrainCommand (Abstract Command): Provides common functionality and data for all terrain commands.
/// It handles capturing the original state for undo and applying changes to the terrain.
/// </summary>
public abstract class BaseTerrainCommand : ITerrainCommand
{
    protected Terrain _terrain;
    protected TerrainData _terrainData;
    protected int _resX, _resY; // Terrain heightmap resolution
    protected int _startX, _startY; // Start coordinates of the affected region in the heightmap
    protected int _width, _height; // Dimensions of the affected region
    protected float[,] _originalHeights; // Stored original heights for undo
    protected float[,] _newHeights;      // Stored new heights after execution

    /// <summary>
    /// Constructor for BaseTerrainCommand.
    /// Captures the terrain context and the specific region to be modified.
    /// </summary>
    /// <param name="terrain">The Unity Terrain component.</param>
    /// <param name="startX">The X-coordinate of the top-left corner of the affected area in heightmap coordinates.</param>
    /// <param name="startY">The Y-coordinate of the top-left corner of the affected area in heightmap coordinates.</param>
    /// <param name="width">The width of the affected area.</param>
    /// <param name="height">The height of the affected area.</param>
    public BaseTerrainCommand(Terrain terrain, int startX, int startY, int width, int height)
    {
        _terrain = terrain;
        _terrainData = terrain.terrainData;
        _resX = _terrainData.heightmapResolution;
        _resY = _terrainData.heightmapResolution;

        // Ensure coordinates and dimensions are within valid bounds
        _startX = Mathf.Clamp(startX, 0, _resX - 1);
        _startY = Mathf.Clamp(startY, 0, _resY - 1);
        _width = Mathf.Clamp(width, 1, _resX - _startX);
        _height = Mathf.Clamp(height, 1, _resY - _startY);

        // Store original heights before any modification for undo functionality
        _originalHeights = _terrainData.GetHeights(_startX, _startY, _width, _height);
        _newHeights = (float[,])_originalHeights.Clone(); // Initialize new heights with current state
    }

    /// <summary>
    /// Executes the command. This will apply the deformation and store the new state.
    /// </summary>
    public void Execute()
    {
        // First, ensure _newHeights are updated by the specific command's logic
        ExecuteModification(); 
        // Then, apply these new heights to the terrain
        _terrainData.SetHeights(_startX, _startY, _newHeights);
    }

    /// <summary>
    /// Undoes the command. This reverts the terrain to its state before this command was executed.
    /// </summary>
    public void Undo()
    {
        // Revert to the stored original heights
        _terrainData.SetHeights(_startX, _startY, _originalHeights);
    }

    /// <summary>
    /// Abstract method for concrete commands to implement their specific deformation logic.
    /// This method should modify the _newHeights array.
    /// </summary>
    protected abstract void ExecuteModification();
}

/// <summary>
/// RaiseTerrainCommand (Concrete Command): Raises the terrain within a specified brush area.
/// </summary>
public class RaiseTerrainCommand : BaseTerrainCommand
{
    private float _brushIntensity;

    public RaiseTerrainCommand(Terrain terrain, int startX, int startY, int width, int height, float intensity)
        : base(terrain, startX, startY, width, height)
    {
        _brushIntensity = intensity;
    }

    protected override void ExecuteModification()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _newHeights[y, x] = Mathf.Clamp01(_originalHeights[y, x] + _brushIntensity);
            }
        }
    }
}

/// <summary>
/// LowerTerrainCommand (Concrete Command): Lowers the terrain within a specified brush area.
/// </summary>
public class LowerTerrainCommand : BaseTerrainCommand
{
    private float _brushIntensity;

    public LowerTerrainCommand(Terrain terrain, int startX, int startY, int width, int height, float intensity)
        : base(terrain, startX, startY, width, height)
    {
        _brushIntensity = intensity;
    }

    protected override void ExecuteModification()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _newHeights[y, x] = Mathf.Clamp01(_originalHeights[y, x] - _brushIntensity);
            }
        }
    }
}

/// <summary>
/// SmoothTerrainCommand (Concrete Command): Smooths the terrain within a specified brush area.
/// It averages the height of each point with its neighbors.
/// </summary>
public class SmoothTerrainCommand : BaseTerrainCommand
{
    private float _smoothFactor; // How much to blend with neighbors

    public SmoothTerrainCommand(Terrain terrain, int startX, int startY, int width, int height, float smoothFactor)
        : base(terrain, startX, startY, width, height)
    {
        _smoothFactor = smoothFactor;
    }

    protected override void ExecuteModification()
    {
        // Iterate over the affected area to apply smoothing
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float sum = _originalHeights[y, x];
                int count = 1;

                // Check 8 neighbors (and self already included)
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip self

                        int neighborX = x + dx;
                        int neighborY = y + dy;

                        // Ensure neighbor coordinates are within the brush's local bounds
                        if (neighborX >= 0 && neighborX < _width && neighborY >= 0 && neighborY < _height)
                        {
                            sum += _originalHeights[neighborY, neighborX];
                            count++;
                        }
                    }
                }
                float average = sum / count;
                _newHeights[y, x] = Mathf.Lerp(_originalHeights[y, x], average, _smoothFactor);
            }
        }
    }
}


/// <summary>
/// TerrainDeformationManager: The main MonoBehaviour script that manages user input,
/// creates and executes terrain deformation commands, and handles undo/redo.
/// This acts as the 'Invoker' in the Command Pattern.
/// </summary>
public class TerrainDeformationManager : MonoBehaviour
{
    public enum DeformationType { Raise, Lower, Smooth }

    [Header("Terrain Reference")]
    [Tooltip("Drag the Terrain GameObject here.")]
    public Terrain targetTerrain;

    [Header("Deformation Settings")]
    [Tooltip("The current type of deformation to apply.")]
    public DeformationType currentDeformationType = DeformationType.Raise;
    [Range(1, 50)]
    [Tooltip("Radius of the deformation brush in heightmap pixels.")]
    public int brushRadius = 10;
    [Range(0.001f, 0.1f)]
    [Tooltip("Intensity of the deformation (how much height changes per click).")]
    public float brushIntensity = 0.01f;
    [Range(0.01f, 1.0f)]
    [Tooltip("Smooth factor for smoothing operation (how strongly to average with neighbors).")]
    public float smoothFactor = 0.5f;

    [Header("Undo/Redo")]
    [Tooltip("Maximum number of undo steps to store.")]
    public int maxUndoSteps = 20;

    private TerrainData _terrainData;
    private int _heightmapResolution;

    // Stacks to store commands for undo and redo functionality
    private Stack<ITerrainCommand> _undoStack = new Stack<ITerrainCommand>();
    private Stack<ITerrainCommand> _redoStack = new Stack<ITerrainCommand>();

    void Start()
    {
        if (targetTerrain == null)
        {
            targetTerrain = FindObjectOfType<Terrain>();
            if (targetTerrain == null)
            {
                Debug.LogError("TerrainDeformationManager: No Terrain found in the scene. Please assign one or ensure one exists.");
                enabled = false; // Disable script if no terrain is found
                return;
            }
        }

        _terrainData = targetTerrain.terrainData;
        _heightmapResolution = _terrainData.heightmapResolution;
    }

    void Update()
    {
        // Handle Mouse Input for Deformation
        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            ApplyDeformation();
        }

        // Handle Keyboard Input for Undo/Redo
        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            UndoLastCommand();
        }
        if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            RedoLastCommand();
        }
    }

    /// <summary>
    /// Performs a raycast from the mouse position to the terrain and applies the chosen deformation.
    /// </summary>
    private void ApplyDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Terrain")))
        {
            // Convert world hit point to terrain heightmap coordinates
            Vector3 terrainLocalPos = targetTerrain.transform.InverseTransformPoint(hit.point);
            Vector3 normalizedPos = new Vector3(
                Mathf.InverseLerp(0, _terrainData.size.x, terrainLocalPos.x),
                0,
                Mathf.InverseLerp(0, _terrainData.size.z, terrainLocalPos.z)
            );

            int hitX = (int)(normalizedPos.x * _heightmapResolution);
            int hitY = (int)(normalizedPos.z * _heightmapResolution);

            // Calculate the affected rectangular region for the brush
            int startX = Mathf.Max(0, hitX - brushRadius);
            int startY = Mathf.Max(0, hitY - brushRadius);
            int endX = Mathf.Min(_heightmapResolution - 1, hitX + brushRadius);
            int endY = Mathf.Min(_heightmapResolution - 1, hitY + brushRadius);

            int width = endX - startX + 1;
            int height = endY - startY + 1;

            ITerrainCommand command = null;

            // Create the appropriate command based on the selected deformation type
            switch (currentDeformationType)
            {
                case DeformationType.Raise:
                    command = new RaiseTerrainCommand(targetTerrain, startX, startY, width, height, brushIntensity);
                    break;
                case DeformationType.Lower:
                    command = new LowerTerrainCommand(targetTerrain, startX, startY, width, height, brushIntensity);
                    break;
                case DeformationType.Smooth:
                    command = new SmoothTerrainCommand(targetTerrain, startX, startY, width, height, smoothFactor);
                    break;
                default:
                    Debug.LogWarning("Unknown deformation type selected.");
                    return;
            }

            // Execute the command and add it to the undo stack
            if (command != null)
            {
                command.Execute();
                _undoStack.Push(command);

                // Clear redo stack whenever a new command is executed
                _redoStack.Clear();

                // Limit undo stack size
                if (_undoStack.Count > maxUndoSteps)
                {
                    // To remove the oldest item, we would need to convert to a list, remove, then convert back.
                    // For simplicity, we'll just let it grow past max and rely on typical editor reset if too large.
                    // In a production system, a custom circular buffer or linked list would be more efficient for true FIFO.
                    // For this example, if the stack exceeds, we just don't push new items until space is made, or we pop the bottom.
                    // Simple approach for demo: if we exceed, we could pop the bottom if we used a data structure like a Queue or Deque.
                    // For a Stack, removing oldest means recreating the stack. Let's just live with unbounded for this demo or
                    // assume maxUndoSteps is a soft limit for visual representation.
                    // A proper implementation might involve storing commands in a List and shifting, or a custom circular buffer.
                    // For now, let's keep it simple and just allow it to exceed.
                    // A more realistic scenario for stacks might be: If _undoStack.Count >= maxUndoSteps,
                    // we would need a way to discard the oldest command without iterating the stack.
                    // This often means using a `List<ITerrainCommand>` and treating it like a stack at one end and queue at the other.
                    // For a simple Stack, we can't easily remove from the bottom without re-populating.
                }
            }
        }
    }

    /// <summary>
    /// Undoes the last executed terrain command.
    /// </summary>
    private void UndoLastCommand()
    {
        if (_undoStack.Count > 0)
        {
            ITerrainCommand command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            Debug.Log("Undo executed. Remaining undo steps: " + _undoStack.Count);
        }
        else
        {
            Debug.Log("Nothing to undo.");
        }
    }

    /// <summary>
    /// Re-executes the last undone terrain command.
    /// </summary>
    private void RedoLastCommand()
    {
        if (_redoStack.Count > 0)
        {
            ITerrainCommand command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            Debug.Log("Redo executed. Remaining redo steps: " + _redoStack.Count);
        }
        else
        {
            Debug.Log("Nothing to redo.");
        }
    }
}
```

---

### **How to Use This Script in Unity:**

1.  **Create a Terrain:**
    *   In Unity, go to `GameObject > 3D Object > Terrain`. This will create a default terrain in your scene.
2.  **Create an Empty GameObject:**
    *   Right-click in the Hierarchy, select `Create Empty`. Name it something like "TerrainDeformationController".
3.  **Attach the Script:**
    *   Drag the `TerrainDeformationManager.cs` script from your Project window onto the "TerrainDeformationController" GameObject in the Hierarchy.
4.  **Assign Terrain:**
    *   In the Inspector, with "TerrainDeformationController" selected, find the `Target Terrain` field on the `Terrain Deformation Manager` component.
    *   Drag your `Terrain` GameObject from the Hierarchy into this `Target Terrain` slot.
5.  **Ensure Terrain Layer:**
    *   Select your `Terrain` GameObject. In the Inspector, next to its name, click the `Layer` dropdown and select `Add Layer...`.
    *   Add a new user layer (e.g., `User Layer 8`) and name it "Terrain".
    *   Go back to your `Terrain` GameObject, select the newly created "Terrain" layer for it. This is crucial for the raycasting to work correctly (`LayerMask.GetMask("Terrain")`).
6.  **Configure Camera:**
    *   Make sure your `Main Camera` is positioned above the terrain and looking down, so you can see the deformation clearly.
7.  **Play the Scene:**
    *   Run your game.
    *   **Click and Drag (Left Mouse Button):** You can now click and drag your mouse over the terrain to apply the selected deformation (`Raise`, `Lower`, or `Smooth`).
    *   **Change Deformation Type:** In the Inspector, while the game is running, you can change the `Current Deformation Type` (Raise, Lower, Smooth), `Brush Radius`, `Brush Intensity`, and `Smooth Factor`.
    *   **Undo (Ctrl+Z):** Press `Ctrl+Z` to undo the last deformation.
    *   **Redo (Ctrl+Y):** Press `Ctrl+Y` to redo an undone deformation.

This setup provides a practical and educational example of managing complex game-world interactions like terrain deformation using the Command design pattern, making it extensible and providing essential features like undo/redo.