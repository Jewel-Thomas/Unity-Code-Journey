// Unity Design Pattern Example: AIFormationSystem
// This script demonstrates the AIFormationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AIFormationSystem' design pattern in Unity helps manage a group of AI agents (e.g., enemies, allies) to move and position themselves as a cohesive unit rather than individual entities. This pattern is particularly useful for creating tactical AI behaviors, military formations, or group movement for characters.

**Core Components of the AI Formation System Pattern:**

1.  **Formation Leader:** The central entity (often a `Transform` or a specific AI agent) that dictates the overall movement and orientation of the formation.
2.  **Formation Members:** The individual AI agents that belong to the formation and follow the leader's movement, maintaining specific relative positions.
3.  **Formation Strategy:** An algorithm or set of rules that determines the precise world positions for each member relative to the leader. This is where different formation types (line, circle, grid, V-shape, etc.) are implemented.
4.  **Formation Manager (or System):** The central component responsible for:
    *   Holding references to the leader and all members.
    *   Selecting and applying a specific `FormationStrategy`.
    *   Calculating target positions for each member using the chosen strategy.
    *   Notifying members of their new target positions.

---

### **C# Unity Example: AIFormationSystem**

This example provides a complete, practical Unity setup for an `AIFormationSystem`. It includes:

*   An `IFormationStrategy` interface for defining different formation types.
*   Concrete implementations for `Line`, `Circle`, and `Grid` formations.
*   A `FormationMemberAgent` script for individual AI units, using `NavMeshAgent` to move towards their assigned target positions.
*   A `FormationSystem` script that acts as the manager, dynamically calculating and assigning positions to its members based on the selected strategy and leader's movement.
*   Editor gizmos for visualizing the formation.

---

### **1. Setup in Unity (Pre-requisites)**

Before running the scripts, ensure you have a basic Unity scene set up:

1.  **Create a Plane or Terrain:** For the NavMesh to work.
2.  **Bake a NavMesh:**
    *   Go to `Window > AI > Navigation`.
    *   In the `Bake` tab, click the `Bake` button.
3.  **Create a Formation Member Prefab:**
    *   Create a 3D Object (e.g., `Capsule`). Rename it `FormationMember`.
    *   Add a `NavMeshAgent` component to it (`Add Component > AI > Nav Mesh Agent`).
    *   Adjust `NavMeshAgent` settings if needed (e.g., `Speed`, `Angular Speed`).
    *   Add the `FormationMemberAgent.cs` script (from below) to this `FormationMember` GameObject.
    *   Drag this `FormationMember` GameObject from the Hierarchy into your Project window to create a prefab. Delete the instance from the Hierarchy.

---

### **2. C# Scripts**

Create these C# scripts in your Unity project:

---

#### **Script 1: `IFormationStrategy.cs`**
This interface defines the contract for any formation strategy.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interface for defining different AI formation strategies.
/// Each strategy is responsible for calculating the world positions
/// for all members relative to a leader's position and rotation.
/// </summary>
public interface IFormationStrategy
{
    /// <summary>
    /// Calculates the world positions for formation members based on the leader's position and rotation.
    /// </summary>
    /// <param name="leaderPosition">The world position of the formation leader.</param>
    /// <param name="leaderRotation">The world rotation of the formation leader.</param>
    /// <param name="numMembers">The total number of members in the formation.</param>
    /// <param name="memberSpacing">The desired spacing between members.</param>
    /// <returns>A list of world positions for each member.</returns>
    List<Vector3> CalculateFormationPositions(Vector3 leaderPosition, Quaternion leaderRotation, int numMembers, float memberSpacing);
}
```

---

#### **Script 2: `FormationType.cs`**
An enum to easily switch between different formation strategies in the inspector.

```csharp
/// <summary>
/// Defines the types of formations available.
/// This enum is used by the FormationSystem to select the active strategy.
/// </summary>
public enum FormationType
{
    Line,
    Circle,
    Grid
}
```

---

#### **Script 3: `LineFormationStrategy.cs`**
An implementation for a simple line formation.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implements a simple line formation strategy.
/// Members are positioned in a straight line behind the leader,
/// oriented relative to the leader's forward direction.
/// </summary>
public class LineFormationStrategy : IFormationStrategy
{
    /// <summary>
    /// Calculates positions for members in a line formation.
    /// Members are placed behind the leader, spaced evenly.
    /// </summary>
    /// <param name="leaderPosition">The world position of the formation leader.</param>
    /// <param name="leaderRotation">The world rotation of the formation leader.</param>
    /// <param name="numMembers">The total number of members in the formation.</param>
    /// <param name="memberSpacing">The desired spacing between members.</param>
    /// <returns>A list of world positions for each member.</returns>
    public List<Vector3> CalculateFormationPositions(Vector3 leaderPosition, Quaternion leaderRotation, int numMembers, float memberSpacing)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 forward = leaderRotation * Vector3.forward; // Leader's forward direction
        Vector3 right = leaderRotation * Vector3.right;     // Leader's right direction

        // Calculate the starting offset for the first member to center the line
        float startOffsetZ = (numMembers - 1) * memberSpacing / 2f;

        for (int i = 0; i < numMembers; i++)
        {
            // Position behind the leader along the leader's forward axis
            Vector3 relativePos = -forward * (startOffsetZ - (i * memberSpacing));
            positions.Add(leaderPosition + relativePos);
        }

        return positions;
    }
}
```

---

#### **Script 4: `CircleFormationStrategy.cs`**
An implementation for a circular formation.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implements a circular formation strategy.
/// Members are positioned in a circle around the leader.
/// </summary>
public class CircleFormationStrategy : IFormationStrategy
{
    /// <summary>
    /// Calculates positions for members in a circular formation.
    /// Members are evenly distributed around a circle centered on the leader.
    /// </summary>
    /// <param name="leaderPosition">The world position of the formation leader.</param>
    /// <param name="leaderRotation">The world rotation of the formation leader (not directly used for position, but could be for orientation).</param>
    /// <param name="numMembers">The total number of members in the formation.</param>
    /// <param name="memberSpacing">The desired radius of the circle (effectively the spacing from the center).</param>
    /// <returns>A list of world positions for each member.</returns>
    public List<Vector3> CalculateFormationPositions(Vector3 leaderPosition, Quaternion leaderRotation, int numMembers, float memberSpacing)
    {
        List<Vector3> positions = new List<Vector3>();
        float radius = memberSpacing; // For circle, spacing directly maps to radius

        if (numMembers == 0) return positions;
        if (numMembers == 1)
        {
            positions.Add(leaderPosition); // Single member stays with leader
            return positions;
        }

        for (int i = 0; i < numMembers; i++)
        {
            float angle = i * Mathf.PI * 2f / numMembers; // Angle for each member
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            // Position relative to leader
            Vector3 relativePos = leaderRotation * new Vector3(x, 0, z); // Apply leader's rotation to the relative position
            positions.Add(leaderPosition + relativePos);
        }

        return positions;
    }
}
```

---

#### **Script 5: `GridFormationStrategy.cs`**
An implementation for a grid/square formation.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implements a grid/square formation strategy.
/// Members are positioned in a rectangular grid behind the leader.
/// </summary>
public class GridFormationStrategy : IFormationStrategy
{
    /// <summary>
    /// Calculates positions for members in a grid formation.
    /// Members are arranged in a square or rectangular grid behind the leader.
    /// </summary>
    /// <param name="leaderPosition">The world position of the formation leader.</param>
    /// <param name="leaderRotation">The world rotation of the formation leader.</param>
    /// <param name="numMembers">The total number of members in the formation.</param>
    /// <param name="memberSpacing">The desired spacing between members in the grid.</param>
    /// <returns>A list of world positions for each member.</returns>
    public List<Vector3> CalculateFormationPositions(Vector3 leaderPosition, Quaternion leaderRotation, int numMembers, float memberSpacing)
    {
        List<Vector3> positions = new List<Vector3>();

        if (numMembers == 0) return positions;
        if (numMembers == 1)
        {
            positions.Add(leaderPosition); // Single member stays with leader
            return positions;
        }

        // Determine grid dimensions
        int cols = Mathf.CeilToInt(Mathf.Sqrt(numMembers));
        int rows = Mathf.CeilToInt((float)numMembers / cols);

        // Calculate offset to center the grid behind the leader
        float startX = -((cols - 1) * memberSpacing / 2f);
        float startZ = -((rows - 1) * memberSpacing / 2f) - memberSpacing * 2f; // Offset back from leader

        int currentMemberIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (currentMemberIndex >= numMembers) break;

                // Position relative to leader's local space
                Vector3 relativePos = new Vector3(
                    startX + c * memberSpacing,
                    0,
                    startZ + r * memberSpacing
                );

                // Apply leader's rotation to get world position
                positions.Add(leaderPosition + (leaderRotation * relativePos));
                currentMemberIndex++;
            }
            if (currentMemberIndex >= numMembers) break;
        }

        return positions;
    }
}
```

---

#### **Script 6: `FormationMemberAgent.cs`**
This script is attached to each individual AI unit.

```csharp
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

/// <summary>
/// Represents an individual AI agent that is part of a formation.
/// It uses a NavMeshAgent to move towards a target position assigned by the FormationSystem.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))] // Ensures the GameObject has a NavMeshAgent
public class FormationMemberAgent : MonoBehaviour
{
    private NavMeshAgent _navMeshAgent;
    private Vector3 _targetPosition;

    // A unique ID for this member within its formation (optional, for advanced use)
    public int MemberID { get; private set; }

    /// <summary>
    /// Gets or sets the target world position for this member to move towards.
    /// The FormationSystem will set this value.
    /// </summary>
    public Vector3 TargetPosition
    {
        get => _targetPosition;
        set
        {
            _targetPosition = value;
            if (_navMeshAgent.isOnNavMesh) // Ensure agent is on a NavMesh before setting destination
            {
                _navMeshAgent.SetDestination(_targetPosition);
            }
            else
            {
                Debug.LogWarning($"FormationMemberAgent '{name}' is not on a NavMesh. Cannot set destination.");
            }
        }
    }

    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent not found on FormationMemberAgent! Please add one.", this);
            enabled = false; // Disable script if essential component is missing
        }
    }

    /// <summary>
    /// Initializes the member with an ID.
    /// </summary>
    /// <param name="id">The unique identifier for this member within its formation.</param>
    public void Initialize(int id)
    {
        MemberID = id;
    }

    // No need for Update loop here, as NavMeshAgent handles movement automatically
    // once TargetPosition is set. If custom movement or additional logic is needed
    // based on movement, this is where it would go.
}
```

---

#### **Script 7: `FormationSystem.cs`**
The main manager script that orchestrates the formation.

```csharp
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq; // For .ToList() if needed

/// <summary>
/// The central manager for an AI Formation System.
/// This script orchestrates a group of AI agents (members) to move
/// in a structured formation behind a designated leader.
/// It uses an IFormationStrategy to calculate member positions.
/// </summary>
public class FormationSystem : MonoBehaviour
{
    [Header("Formation Settings")]
    [Tooltip("The Transform that the formation will follow. Can be the player, another AI, or an empty GameObject.")]
    [SerializeField] private Transform _leaderTransform;
    [Tooltip("The type of formation to use (e.g., Line, Circle, Grid).")]
    [SerializeField] private FormationType _currentFormationType = FormationType.Line;
    [Tooltip("The desired spacing between formation members.")]
    [SerializeField] private float _memberSpacing = 2.0f;
    [Tooltip("The number of members to spawn initially.")]
    [SerializeField] private int _initialMemberCount = 5;

    [Header("Member Settings")]
    [Tooltip("The prefab for the AI agents that will form the formation.")]
    [SerializeField] private GameObject _formationMemberPrefab;

    private List<FormationMemberAgent> _formationMembers = new List<FormationMemberAgent>();
    private Dictionary<FormationType, IFormationStrategy> _formationStrategies;

    private IFormationStrategy ActiveFormationStrategy => _formationStrategies[_currentFormationType];

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the available formation strategies.
    /// </summary>
    void Awake()
    {
        // Initialize dictionary of all available formation strategies
        _formationStrategies = new Dictionary<FormationType, IFormationStrategy>
        {
            { FormationType.Line, new LineFormationStrategy() },
            { FormationType.Circle, new CircleFormationStrategy() },
            { FormationType.Grid, new GridFormationStrategy() }
            // Add new strategies here as they are created
        };

        // Basic validation
        if (_leaderTransform == null)
        {
            Debug.LogError("FormationSystem: Leader Transform is not assigned!", this);
            enabled = false; // Disable script if essential components are missing
            return;
        }
        if (_formationMemberPrefab == null)
        {
            Debug.LogError("FormationSystem: Formation Member Prefab is not assigned!", this);
            enabled = false;
            return;
        }
        if (_formationMemberPrefab.GetComponent<FormationMemberAgent>() == null)
        {
            Debug.LogError("FormationSystem: Member Prefab must have a FormationMemberAgent script attached!", this);
            enabled = false;
            return;
        }
        if (_formationMemberPrefab.GetComponent<NavMeshAgent>() == null)
        {
            Debug.LogError("FormationSystem: Member Prefab must have a NavMeshAgent component attached!", this);
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Spawns initial members.
    /// </summary>
    void Start()
    {
        SpawnInitialMembers(_initialMemberCount);
    }

    /// <summary>
    /// Spawns the initial formation members based on `_initialMemberCount`.
    /// </summary>
    /// <param name="count">The number of members to spawn.</param>
    private void SpawnInitialMembers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject memberGO = Instantiate(_formationMemberPrefab, transform.position, Quaternion.identity);
            FormationMemberAgent memberAgent = memberGO.GetComponent<FormationMemberAgent>();
            if (memberAgent != null)
            {
                memberAgent.Initialize(i); // Assign a simple ID
                AddMember(memberAgent);
            }
            else
            {
                Debug.LogError($"Spawned prefab '{_formationMemberPrefab.name}' does not have a FormationMemberAgent component!", memberGO);
                Destroy(memberGO); // Clean up if component is missing
            }
        }
    }

    /// <summary>
    /// Adds an existing FormationMemberAgent to the formation.
    /// </summary>
    /// <param name="member">The agent to add.</param>
    public void AddMember(FormationMemberAgent member)
    {
        if (!_formationMembers.Contains(member))
        {
            _formationMembers.Add(member);
        }
        else
        {
            Debug.LogWarning($"Attempted to add member '{member.name}' that is already in the formation.", member);
        }
    }

    /// <summary>
    /// Removes a FormationMemberAgent from the formation.
    /// </summary>
    /// <param name="member">The agent to remove.</param>
    public void RemoveMember(FormationMemberAgent member)
    {
        if (_formationMembers.Remove(member))
        {
            // Optionally, do something with the removed member, e.g., let it move freely or destroy it.
            Debug.Log($"Removed member '{member.name}' from formation.");
        }
        else
        {
            Debug.LogWarning($"Attempted to remove member '{member.name}' which was not found in the formation.", member);
        }
    }

    /// <summary>
    /// Called every frame.
    /// Recalculates and assigns new target positions for all members.
    /// </summary>
    void Update()
    {
        if (_leaderTransform == null || _formationMembers.Count == 0 || ActiveFormationStrategy == null)
        {
            return;
        }

        // 1. Get the leader's current position and rotation
        Vector3 leaderPos = _leaderTransform.position;
        Quaternion leaderRot = _leaderTransform.rotation;

        // 2. Use the active strategy to calculate target positions for all members
        List<Vector3> targetPositions = ActiveFormationStrategy.CalculateFormationPositions(
            leaderPos,
            leaderRot,
            _formationMembers.Count,
            _memberSpacing
        );

        // 3. Assign the calculated positions to each member
        for (int i = 0; i < _formationMembers.Count; i++)
        {
            if (i < targetPositions.Count) // Ensure we have a target position for this member
            {
                _formationMembers[i].TargetPosition = targetPositions[i];
            }
            else
            {
                Debug.LogWarning($"Not enough target positions calculated for all members! Member {i} will not move.", this);
            }
        }
    }

    /// <summary>
    /// Draws visual debugging aids in the scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        if (_leaderTransform == null || _formationStrategies == null || !enabled)
        {
            return;
        }

        // Draw leader position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_leaderTransform.position, 0.5f);
        Gizmos.DrawRay(_leaderTransform.position, _leaderTransform.forward * 2f);

        // Draw calculated member target positions
        if (Application.isPlaying && ActiveFormationStrategy != null && _formationMembers.Count > 0)
        {
            Gizmos.color = Color.cyan;
            List<Vector3> targetPositions = ActiveFormationStrategy.CalculateFormationPositions(
                _leaderTransform.position,
                _leaderTransform.rotation,
                _formationMembers.Count,
                _memberSpacing
            );

            foreach (Vector3 pos in targetPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.3f);
                Gizmos.DrawCube(pos + Vector3.up * 0.1f, Vector3.one * 0.2f);
            }
        }
        else if (!Application.isPlaying && _leaderTransform != null) // Draw a preview in edit mode
        {
             // Allow previewing formations in edit mode if a leader is assigned
            IFormationStrategy previewStrategy = new LineFormationStrategy(); // Default preview
            if (_currentFormationType == FormationType.Circle) previewStrategy = new CircleFormationStrategy();
            if (_currentFormationType == FormationType.Grid) previewStrategy = new GridFormationStrategy();

            Gizmos.color = Color.cyan * 0.7f; // Slightly faded for preview
            List<Vector3> previewPositions = previewStrategy.CalculateFormationPositions(
                _leaderTransform.position,
                _leaderTransform.rotation,
                _initialMemberCount,
                _memberSpacing
            );

            foreach (Vector3 pos in previewPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }
    }
}
```

---

### **3. How to Use in Unity**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., "FormationManager").
2.  **Attach `FormationSystem.cs`:** Drag and drop the `FormationSystem.cs` script onto this new "FormationManager" GameObject.
3.  **Assign Prefab:** In the Inspector for "FormationManager," drag your `FormationMember` prefab (the Capsule with NavMeshAgent and `FormationMemberAgent.cs`) into the `Formation Member Prefab` slot.
4.  **Create a Leader:**
    *   You can use your Player character's `Transform` as the leader.
    *   Or, create another empty GameObject (e.g., "AILeader") and position it in the scene. Add some simple movement script to it (e.g., `transform.position += transform.forward * Time.deltaTime * 5f;` in `Update()` and `transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * 100f * Time.deltaTime);` for basic keyboard control, or a simple AI pathfinding script).
5.  **Assign Leader Transform:** Drag the `Transform` of your chosen leader GameObject (e.g., "AILeader" or your Player) into the `Leader Transform` slot of the "FormationManager."
6.  **Configure Settings:** Adjust `Member Spacing`, `Initial Member Count`, and `Current Formation Type` in the Inspector to see different behaviors.
7.  **Run the Scene:** Play your scene. The `FormationMember` agents should spawn and move into the chosen formation relative to the `Leader Transform`. As the leader moves, the formation will follow.

---

### **Explanation of the AIFormationSystem Pattern in this Example:**

*   **`IFormationStrategy` (The Strategy Pattern):** This is the core of flexibility. Instead of hardcoding formation logic, we define an interface `IFormationStrategy`. Each specific formation (Line, Circle, Grid) then implements this interface. This allows us to easily add new formation types without modifying the `FormationSystem` itself, adhering to the Open/Closed Principle.
*   **`FormationType` Enum:** Provides an easy way to switch between different strategies via the Unity Inspector.
*   **`FormationSystem` (The Manager/Context):** This is the main orchestrator.
    *   It holds references to all `FormationMemberAgent`s.
    *   It has a reference to the `_leaderTransform` (the "Leader").
    *   It uses a `Dictionary` to store instances of each `IFormationStrategy` implementation. This prevents constant object creation and allows quick lookup.
    *   In its `Update` method, it gets the leader's current position and rotation, then calls `CalculateFormationPositions` on the currently active `IFormationStrategy`.
    *   It then iterates through its `_formationMembers` and assigns each one its calculated target position.
    *   `AddMember` and `RemoveMember` methods demonstrate how the formation can be dynamically modified at runtime.
    *   `OnDrawGizmos` provides essential visual feedback in the Unity editor, showing the leader and the target positions for all members.
*   **`FormationMemberAgent` (The Agent/Follower):**
    *   This script is attached to each individual AI unit that will be part of the formation.
    *   It has a `NavMeshAgent` component (ensured by `[RequireComponent]`) which handles the actual pathfinding and movement.
    *   It exposes a `TargetPosition` property. When this property is set by the `FormationSystem`, the `NavMeshAgent` is instructed to move to that new destination.
    *   The separation of concerns is clear: `FormationSystem` decides *where* the agent should go, and `FormationMemberAgent` decides *how* it gets there (using NavMeshAgent).

This example provides a robust and extensible foundation for implementing sophisticated AI formation behaviors in your Unity projects.