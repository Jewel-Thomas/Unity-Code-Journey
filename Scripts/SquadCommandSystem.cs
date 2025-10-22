// Unity Design Pattern Example: SquadCommandSystem
// This script demonstrates the SquadCommandSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Squad Command System** design pattern in Unity. This pattern leverages the **Command Pattern** to manage actions for a group of units (a squad). It provides a flexible and extensible way to issue commands, allowing for features like queuing, undo/redo (though not fully implemented here for brevity), and easy addition of new command types.

---

### Understanding the Squad Command System Pattern

The "Squad Command System" isn't a single GoF (Gang of Four) pattern, but rather a design that *combines* several patterns, primarily the **Command Pattern**, with elements of a **Manager/Mediator** for squad control.

**Core Idea:**
A player (or AI) issues high-level commands (e.g., "Move here," "Attack this enemy") to a *squad* or *selected units*. These commands are then encapsulated as objects and distributed to the individual units, which know how to execute them.

**Key Components and Their Roles:**

1.  **`ICommand` (Command Interface):**
    *   **Role:** Declares the interface for all command objects. It typically defines an `Execute()` method (and optionally `Undo()`).
    *   **Benefit:** Decouples the *invoker* (the part that issues commands, e.g., `SquadCommandSystem`) from the *receiver* (the part that performs the action, e.g., `Unit`). The invoker doesn't need to know *how* an action is performed, only that it can tell a command to `Execute()`.

2.  **`ConcreteCommand` Classes (e.g., `MoveCommand`, `AttackCommand`):**
    *   **Role:** Implement the `ICommand` interface. Each concrete command encapsulates a specific action and the parameters needed to perform it. It holds a reference to the `Receiver` (implicitly, via the `Execute` method's parameter, or explicitly in its constructor if the command is highly specific).
    *   **Benefit:** Each action becomes an object. This allows for commands to be stored, queued, passed around, and easily extended.

3.  **`Unit` (Receiver):**
    *   **Role:** The object that performs the actual action when a command is executed. It knows *how* to carry out specific operations (e.g., `MoveTo`, `Attack`).
    *   **Benefit:** Encapsulates the specific business logic for performing actions. The command objects tell the receiver *what* to do, and the receiver figures out *how* to do it.

4.  **`SquadCommandSystem` (Invoker/Manager/Client):**
    *   **Role:** This is the central hub. It handles player input, determines which command should be created, creates the appropriate `ConcreteCommand` object, and then `Execute()`s that command on the `Receiver`s (the selected `Unit`s). It also manages the collection of units (the "squad").
    *   **Benefit:** Orchestrates the entire system. It acts as the bridge between player input and unit actions, adhering to the Command pattern by not directly interacting with the unit's action methods.

---

### Unity Setup Instructions

1.  **Create C# Script:** In your Unity project, create a new C# script named `SquadCommandSystem` (File -> New C# Script).
2.  **Paste Code:** Copy the entire code block provided below and paste it into the `SquadCommandSystem.cs` file, replacing its contents.
3.  **Create Unit Prefab:**
    *   Create a 3D Object (e.g., `GameObject -> 3D Object -> Sphere`).
    *   Rename it to `UnitPrefab`.
    *   Ensure it has a `Collider` component (e.g., Sphere Collider) so raycasts can hit it.
    *   Drag this `UnitPrefab` from the Hierarchy into your Project window to create a prefab asset.
    *   You can then delete the instance from the Hierarchy if you wish; the script will spawn them.
4.  **Create Ground Object:**
    *   Create a 3D Object (e.g., `GameObject -> 3D Object -> Plane`).
    *   Rename it to `Ground`.
    *   Ensure it has a `Collider` component (e.g., Mesh Collider).
    *   **Important:** Create a new Layer named "Ground" (`Layers -> Add Layer...`) and assign this "Ground" layer to your `Ground` GameObject in the Inspector. Alternatively, ensure it has the Tag "Ground" if you prefer using tags. The script checks for both.
5.  **Create Materials:**
    *   Create two new Materials (e.g., `Assets -> Create -> Material`).
    *   Rename one `SelectedMaterial`. Set its Albedo color to something bright like **Yellow**.
    *   Rename the other `UnselectedMaterial`. Set its Albedo color to something neutral like **Gray**.
6.  **Create Manager GameObject:**
    *   Create an empty GameObject in your scene (`GameObject -> Create Empty`).
    *   Rename it to `SquadCommander`.
7.  **Attach Script and Assign References:**
    *   Select the `SquadCommander` GameObject in the Hierarchy.
    *   Drag and drop the `SquadCommandSystem` script from your Project window onto the `SquadCommander` in the Inspector.
    *   In the Inspector, you will see fields appear:
        *   Drag your `UnitPrefab` into the **Unit Prefab** slot.
        *   Drag your `SelectedMaterial` into the **Selected Material** slot.
        *   Drag your `UnselectedMaterial` into the **Unselected Material** slot.
        *   Adjust `Number Of Units`, `Unit Move Speed`, `Unit Attack Range` as desired.
8.  **Run the Scene:**
    *   Play the scene. You should see units spawn.
    *   **Left-click** on a unit to select it. Its color will change.
    *   **Shift + Left-click** on units to add/remove them from the current selection.
    *   With units selected, **Right-click** on the ground to issue a **Move Command**. The selected units will move to that position.
    *   With units selected, **Right-click** on an *unselected* unit to issue an **Attack Command**. The selected units will move towards and "attack" the target unit (simulated by debug logs and stopping near it).

---

### `SquadCommandSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Required for generic Lists and other collections

/// <summary>
/// ICommand Interface:
/// Defines the contract for all command objects.
/// In this system, commands are designed to operate on a single 'Unit' at a time.
/// This allows the SquadCommandSystem to easily issue the same command to multiple units
/// by iterating through them and calling 'Execute' for each.
/// </summary>
public interface ICommand
{
    void Execute(SquadCommandSystem.Unit unit);
    // Optional: void Undo(SquadCommandSystem.Unit unit);
    // Adding an Undo method would enable features like command history and reversal.
}

/// <summary>
/// MoveCommand:
/// A concrete implementation of ICommand for moving a unit to a specific position.
/// It encapsulates the target position needed for the move operation.
/// </summary>
public class MoveCommand : ICommand
{
    private Vector3 _targetPosition;

    public MoveCommand(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    /// <summary>
    /// Executes the Move command by instructing the given unit to move to the target position.
    /// </summary>
    /// <param name="unit">The Unit object that will perform the action.</param>
    public void Execute(SquadCommandSystem.Unit unit)
    {
        unit.MoveTo(_targetPosition);
    }
}

/// <summary>
/// AttackCommand:
/// A concrete implementation of ICommand for commanding a unit to attack another unit.
/// It encapsulates the target unit that needs to be attacked.
/// </summary>
public class AttackCommand : ICommand
{
    private SquadCommandSystem.Unit _targetUnit;

    public AttackCommand(SquadCommandSystem.Unit targetUnit)
    {
        _targetUnit = targetUnit;
    }

    /// <summary>
    /// Executes the Attack command by instructing the given unit to attack the target unit.
    /// </summary>
    /// <param name="unit">The Unit object that will perform the action.</param>
    public void Execute(SquadCommandSystem.Unit unit)
    {
        unit.Attack(_targetUnit);
    }
}

/// <summary>
/// SquadCommandSystem:
/// This is the main MonoBehaviour class that acts as the central orchestrator
/// for the entire Squad Command System.
///
/// It combines several roles:
/// 1.  **Manager:** Manages the collection of all units (the squad).
/// 2.  **Invoker:** Processes player input, creates appropriate command objects.
/// 3.  **Client:** Decides which commands to issue and to which units.
/// 4.  **Mediator (for squad-level commands):** Distributes commands to selected units.
///
/// This system demonstrates how to decouple the input/command issuance from
/// the actual execution logic of the units.
/// </summary>
public class SquadCommandSystem : MonoBehaviour
{
    [Header("Unit Configuration")]
    [Tooltip("Prefab for the units in the squad.")]
    [SerializeField] private GameObject _unitPrefab;
    [Tooltip("Number of units to spawn.")]
    [SerializeField] private int _numberOfUnits = 5;
    [Tooltip("Movement speed for units.")]
    [SerializeField] private float _unitMoveSpeed = 3f;
    [Tooltip("Attack range for units.")]
    [SerializeField] private float _unitAttackRange = 1.5f;

    [Header("Selection Visuals")]
    [Tooltip("Material to apply when a unit is selected.")]
    [SerializeField] private Material _selectedMaterial;
    [Tooltip("Material to apply when a unit is not selected.")]
    [SerializeField] private Material _unselectedMaterial;

    private List<Unit> _allUnits = new List<Unit>();
    private List<Unit> _selectedUnits = new List<Unit>();

    // Reference to the main camera for raycasting
    private Camera _mainCamera;

    /// <summary>
    /// Unit Nested Class:
    /// Represents an individual unit in the squad. This is the 'Receiver' of the commands.
    /// It encapsulates the actual logic for performing actions like moving or attacking.
    /// By nesting it, we keep the entire demo contained in one script file.
    /// In a larger project, 'Unit' would typically be its own MonoBehaviour script file.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        public float Speed { get; private set; }
        public float AttackRange { get; private set; }

        private Vector3 _currentMoveTarget;
        private Unit _currentAttackTarget;
        private Material _selectedMat;
        private Material _unselectedMat;
        private Renderer _renderer;

        /// <summary>
        /// Initializes the unit with its properties and materials.
        /// Called immediately after instantiation by the SquadCommandSystem.
        /// </summary>
        public void Initialize(float speed, float attackRange, Material unselectedMat, Material selectedMat)
        {
            Speed = speed;
            AttackRange = attackRange;
            _unselectedMat = unselectedMat;
            _selectedMat = selectedMat;
            _renderer = GetComponent<Renderer>();
            _renderer.material = _unselectedMat; // Start unselected
            _currentMoveTarget = transform.position; // Units start at their current position, not moving
        }

        void Update()
        {
            HandleMovement();
            HandleAttack();
        }

        /// <summary>
        /// Handles the unit's movement towards its current move target.
        /// If an attack target is set, movement might be towards the attack target.
        /// </summary>
        private void HandleMovement()
        {
            // If we have an attack target, the primary movement goal is to get into attack range.
            if (_currentAttackTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentAttackTarget.transform.position);
                if (distanceToTarget > AttackRange)
                {
                    // Move towards the attack target
                    transform.position = Vector3.MoveTowards(transform.position, _currentAttackTarget.transform.position, Speed * Time.deltaTime);
                }
                else
                {
                    // Within attack range, stop moving
                    _currentMoveTarget = transform.position;
                }
            }
            // If no attack target, or within range of attack target, move towards explicit move target
            else if (Vector3.Distance(transform.position, _currentMoveTarget) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, _currentMoveTarget, Speed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Handles the unit's attack behavior.
        /// If an attack target is set and the unit is within range, it "attacks".
        /// </summary>
        private void HandleAttack()
        {
            if (_currentAttackTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentAttackTarget.transform.position);
                if (distanceToTarget <= AttackRange + 0.1f) // Small buffer
                {
                    // Stop moving when within attack range
                    _currentMoveTarget = transform.position;
                    // Look at the target
                    Vector3 lookDir = _currentAttackTarget.transform.position - transform.position;
                    lookDir.y = 0; // Keep unit upright
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Speed * Time.deltaTime * 5f);
                    }

                    // Simulate attack action
                    // In a real game, this would trigger attack animations, damage calculation, sound effects, etc.
                    Debug.Log($"<color=red>{name}</color> is attacking <color=red>{_currentAttackTarget.name}</color>!");
                    // For demo purposes, we log; a real system would have attack cooldowns, health management, etc.
                }
            }
        }

        /// <summary>
        /// Public method for receiving and initiating a 'Move' action.
        /// This is called by the `MoveCommand`'s `Execute` method.
        /// </summary>
        /// <param name="position">The target world position to move to.</param>
        public void MoveTo(Vector3 position)
        {
            _currentMoveTarget = position;
            _currentAttackTarget = null; // Clear attack target when moving
            Debug.Log($"<color=cyan>{name}</color> received Move command to {position}");
        }

        /// <summary>
        /// Public method for receiving and initiating an 'Attack' action.
        /// This is called by the `AttackCommand`'s `Execute` method.
        /// </summary>
        /// <param name="target">The Unit object to attack.</param>
        public void Attack(Unit target)
        {
            if (target != null)
            {
                _currentAttackTarget = target;
                // Movement towards target will be handled in Update if out of range
                Debug.Log($"<color=red>{name}</color> received Attack command on <color=red>{target.name}</color>");
            }
            else
            {
                Debug.LogWarning($"{name} received Attack command with null target.");
            }
        }

        /// <summary>
        /// Updates the unit's visual appearance based on its selection state.
        /// </summary>
        /// <param name="isSelected">True if the unit is selected, false otherwise.</param>
        public void SetSelected(bool isSelected)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _renderer.material = isSelected ? _selectedMat : _unselectedMat;
        }
    } // End of Unit class

    // --- MONOBEHAVIOUR LIFECYCLE & INPUT HANDLING ---

    void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("Main Camera not found! Please ensure your camera is tagged 'MainCamera'.", this);
            enabled = false; // Disable script if no camera
            return;
        }
        InitializeUnits();
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Spawns units based on configuration and adds them to the _allUnits list.
    /// Each unit is initialized with necessary parameters.
    /// </summary>
    private void InitializeUnits()
    {
        if (_unitPrefab == null)
        {
            Debug.LogError("Unit Prefab is not assigned in the inspector! Please assign a prefab.", this);
            enabled = false;
            return;
        }
        if (_selectedMaterial == null || _unselectedMaterial == null)
        {
            Debug.LogError("Selection materials are not assigned! Please assign them.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < _numberOfUnits; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-8f, 8f), 0.5f, Random.Range(-8f, 8f));
            GameObject unitGO = Instantiate(_unitPrefab, spawnPos, Quaternion.identity, transform);
            unitGO.name = $"Unit_{i + 1}";
            Unit unit = unitGO.GetComponent<Unit>();
            if (unit == null)
            {
                // Add Unit component if it's missing from the prefab
                unit = unitGO.AddComponent<Unit>();
            }
            unit.Initialize(_unitMoveSpeed, _unitAttackRange, _unselectedMaterial, _selectedMaterial);
            _allUnits.Add(unit);
        }
        Debug.Log($"Spawned <color=green>{_numberOfUnits}</color> units.");
    }

    /// <summary>
    /// Handles player input (mouse clicks) for unit selection and command issuance.
    /// </summary>
    private void HandleInput()
    {
        // Left-click for selection
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelectionClick();
        }
        // Right-click for commands
        else if (Input.GetMouseButtonDown(1))
        {
            HandleCommandClick();
        }
    }

    /// <summary>
    /// Processes left-click input for unit selection logic.
    /// Supports single selection and multiple selection (Shift-click).
    /// </summary>
    private void HandleSelectionClick()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Unit clickedUnit = hit.collider.GetComponent<Unit>();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Shift-click: Add or remove unit from current selection
                if (clickedUnit != null)
                {
                    if (_selectedUnits.Contains(clickedUnit))
                    {
                        DeselectUnit(clickedUnit);
                    }
                    else
                    {
                        SelectUnit(clickedUnit);
                    }
                }
            }
            else
            {
                // Regular click: Clear existing selection and select only the clicked unit (or nothing)
                ClearSelection();
                if (clickedUnit != null)
                {
                    SelectUnit(clickedUnit);
                }
            }
        }
        else
        {
            // Clicked on empty space, clear selection
            ClearSelection();
        }
    }

    /// <summary>
    /// Processes right-click input for command issuance.
    /// Creates either a MoveCommand or an AttackCommand based on what was clicked.
    /// </summary>
    private void HandleCommandClick()
    {
        // Commands are only issued if there are units currently selected.
        if (_selectedUnits.Count == 0)
        {
            Debug.Log("<color=orange>No units selected to issue command.</color>", this);
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            ICommand command = null;
            Unit targetUnit = hit.collider.GetComponent<Unit>();

            // Determine command type based on the hit object
            if (targetUnit != null && !_selectedUnits.Contains(targetUnit))
            {
                // Clicked on an unselected unit: Issue an Attack Command
                command = new AttackCommand(targetUnit);
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || hit.collider.gameObject.CompareTag("Ground"))
            {
                // Clicked on an object on the "Ground" layer or with "Ground" tag: Issue a Move Command
                command = new MoveCommand(hit.point);
            }
            // Else (clicked on a selected unit, or other irrelevant object), no command is created.

            if (command != null)
            {
                IssueCommandToUnits(_selectedUnits, command);
            }
            else
            {
                Debug.Log("<color=yellow>Right-click: No valid command target found (e.g., clicked on empty space, self, or non-unit object).</color>", this);
            }
        }
        else
        {
            Debug.Log("<color=yellow>Right-click: No hit found for command target.</color>", this);
        }
    }

    /// <summary>
    /// Selects a single unit and updates its visual state.
    /// </summary>
    /// <param name="unit">The unit to select.</param>
    private void SelectUnit(Unit unit)
    {
        if (!_selectedUnits.Contains(unit))
        {
            _selectedUnits.Add(unit);
            unit.SetSelected(true);
            Debug.Log($"<color=lime>Selected</color> {unit.name}");
        }
    }

    /// <summary>
    /// Deselects a single unit and updates its visual state.
    /// </summary>
    /// <param name="unit">The unit to deselect.</param>
    private void DeselectUnit(Unit unit)
    {
        if (_selectedUnits.Remove(unit))
        {
            unit.SetSelected(false);
            Debug.Log($"<color=grey>Deselected</color> {unit.name}");
        }
    }

    /// <summary>
    /// Clears all currently selected units and resets their visual states.
    /// </summary>
    private void ClearSelection()
    {
        foreach (Unit unit in _selectedUnits)
        {
            unit.SetSelected(false);
        }
        _selectedUnits.Clear();
        Debug.Log("Selection cleared.");
    }

    /// <summary>
    /// The core method for distributing a command to a list of units.
    /// For each unit, it calls the command's Execute method.
    /// This demonstrates the decoupling: the SquadCommandSystem (Invoker)
    /// doesn't know the specifics of a 'Move' or 'Attack', it just tells
    /// the 'command' object to 'Execute' itself on each 'unit' (Receiver).
    /// </summary>
    /// <param name="unitsToCommand">The list of units to which the command should be issued.</param>
    /// <param name="command">The ICommand object to execute.</param>
    private void IssueCommandToUnits(List<Unit> unitsToCommand, ICommand command)
    {
        if (command == null || unitsToCommand == null || unitsToCommand.Count == 0) return;

        foreach (Unit unit in unitsToCommand)
        {
            command.Execute(unit); // Each unit executes the same command instance
        }
        Debug.Log($"<color=purple>Command issued to</color> <color=fuchsia>{unitsToCommand.Count}</color> units.");
    }
}
```