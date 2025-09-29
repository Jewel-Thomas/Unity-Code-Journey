// Unity Design Pattern Example: UINavMeshSystem
// This script demonstrates the UINavMeshSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'UINavMeshSystem' design pattern, as interpreted here, provides a flexible, graph-based approach to UI navigation in Unity. Unlike Unity's default `EventSystem` navigation (which is often grid-based or relies on explicit `selectOn...` links), this pattern allows you to define arbitrary, "mesh-like" connections between UI elements. This is particularly useful for complex UI layouts, radial menus, game board UIs, or scenarios where standard linear navigation is insufficient.

**Core Idea:**
1.  **Nodes:** Each navigatable UI element becomes a 'node' in our custom navigation graph.
2.  **Connections:** Each node explicitly defines which other nodes it can navigate to in specific directions (Up, Down, Left, Right).
3.  **Manager:** A central system handles input, tracks the current selected node, and uses the defined connections to move selection.

---

### UINavMeshSystem Example

This example provides three C# scripts:

1.  **`UINavMeshDirection.cs`**: An enum defining navigation directions.
2.  **`UINavMeshConnection.cs`**: A struct to define a single connection between nodes.
3.  **`UINavMeshNode.cs`**: A component attached to individual UI elements, defining its ID and connections.
4.  **`UINavMeshManager.cs`**: The central singleton manager that processes input and handles navigation.

---

#### 1. `UINavMeshDirection.cs`

This simple enum defines the cardinal directions for navigation.

```csharp
// UINavMeshDirection.cs
using UnityEngine; // Included for completeness, though not strictly needed for just an enum.

/// <summary>
/// Defines the standard navigation directions for the UINavMeshSystem.
/// </summary>
public enum UINavMeshDirection
{
    Up,
    Down,
    Left,
    Right,
    None // Can be used for custom connections or initialization where a direction isn't strictly defined.
}
```

---

#### 2. `UINavMeshConnection.cs`

This struct defines a single navigable link from one `UINavMeshNode` to another.

```csharp
// UINavMeshConnection.cs
using UnityEngine; // Included for completeness, though not strictly needed for just a struct.

/// <summary>
/// Represents a custom connection from one UINavMeshNode to another.
/// </summary>
[System.Serializable] // Makes this struct visible and editable in the Unity Inspector.
public struct UINavMeshConnection
{
    [Tooltip("The direction from the current node (e.g., Up, Down, Left, Right).")]
    public UINavMeshDirection direction;

    [Tooltip("The NodeID of the target UINavMeshNode in that direction.")]
    public string targetNodeID;
}
```

---

#### 3. `UINavMeshNode.cs`

Attach this script to any UI `Selectable` (Button, Slider, Toggle, InputField, etc.) that you want to include in your custom navigation graph.

```csharp
// UINavMeshNode.cs
using UnityEngine;
using UnityEngine.UI; // Required for Selectable
using System.Collections.Generic; // Not strictly needed for this class, but good practice.

/// <summary>
/// Represents a single navigatable UI element within the UINavMeshSystem.
/// Each node has a unique ID, references its associated Selectable component,
/// and defines explicit connections to other nodes based on direction.
/// </summary>
/// <remarks>
/// It's crucial that the default navigation of the attached Selectable
/// is set to 'None' to prevent conflicts with the UINavMeshManager.
/// </remarks>
[RequireComponent(typeof(Selectable))] // Ensures a Selectable component exists on the GameObject.
public class UINavMeshNode : MonoBehaviour
{
    [Tooltip("Unique identifier for this UI Nav Mesh Node. Must be unique within the system.")]
    public string NodeID;

    [Tooltip("The Selectable UI component that this node represents. If left null, it will try to find one on this GameObject.")]
    public Selectable selectable;

    [Tooltip("Define custom navigation connections from this node to others. " +
             "Specify a direction and the NodeID of the target node.")]
    public UINavMeshConnection[] connections;

    private void Awake()
    {
        // --- Input Validation ---
        if (string.IsNullOrEmpty(NodeID))
        {
            Debug.LogError($"UINavMeshNode on {gameObject.name} requires a unique NodeID! Disabling component.", this);
            enabled = false;
            return;
        }

        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
            if (selectable == null)
            {
                Debug.LogError($"UINavMeshNode on {gameObject.name} could not find a Selectable component! Disabling component.", this);
                enabled = false;
                return;
            }
        }
        
        // --- Crucial Step: Disable Unity's default navigation ---
        // This prevents the EventSystem from trying to navigate this selectable using its built-in
        // logic, which would conflict with our custom UINavMeshSystem navigation.
        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.None;
        selectable.navigation = nav;
    }

    private void OnEnable()
    {
        // Register this node with the central UINavMeshManager when it becomes active.
        if (UINavMeshManager.Instance != null)
        {
            UINavMeshManager.Instance.RegisterNode(this);
        }
        else
        {
            // This can happen if the manager hasn't initialized yet or is not in the scene.
            Debug.LogWarning("UINavMeshManager instance not found. Node will not be registered.", this);
        }
    }

    private void OnDisable()
    {
        // Unregister this node from the manager when it becomes inactive or destroyed.
        if (UINavMeshManager.Instance != null)
        {
            UINavMeshManager.Instance.UnregisterNode(this);
        }
    }

    /// <summary>
    /// Gets the target NodeID for a given navigation direction based on this node's defined connections.
    /// </summary>
    /// <param name="direction">The desired navigation direction (e.g., Up, Down, Left, Right).</param>
    /// <returns>The NodeID of the target node, or null if no connection exists for that direction from this node.</returns>
    public string GetTargetNodeID(UINavMeshDirection direction)
    {
        foreach (var connection in connections)
        {
            if (connection.direction == direction)
            {
                return connection.targetNodeID;
            }
        }
        return null; // No connection found for the given direction.
    }
}
```

---

#### 4. `UINavMeshManager.cs`

This is the central manager responsible for handling input, maintaining the current selection, and navigating between `UINavMeshNode`s. It's implemented as a Singleton.

```csharp
// UINavMeshManager.cs
using UnityEngine;
using UnityEngine.EventSystems; // Required for EventSystem.current
using UnityEngine.UI; // Required for Selectable (indirectly)
using System.Collections.Generic; // Required for Dictionary

/// <summary>
/// The UINavMeshSystem Manager. This Singleton class orchestrates a custom,
/// graph-based navigation system for UI elements. It processes user input
/// and moves selection between UINavMeshNodes based on their defined connections.
/// </summary>
/// <remarks>
/// This pattern shines when Unity's default EventSystem navigation (which often relies on
/// rectangular layouts or manual Selectable linking) is too restrictive for complex
/// UI designs, such as:
/// - Radial menus where navigation isn't strictly cardinal.
/// - Game board interfaces with custom adjacency rules.
/// - Highly thematic UI layouts where elements are positioned non-linearly.
/// - When you need fine-grained control over navigation paths.
/// </remarks>
public class UINavMeshManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // Provides global access to the single instance of the UINavMeshManager.
    public static UINavMeshManager Instance { get; private set; }

    [Tooltip("The ID of the UINavMeshNode that should be selected automatically when the system starts.")]
    [SerializeField] private string _initialNodeID;

    // Stores all registered UINavMeshNodes, allowing quick lookup by their unique NodeID.
    private Dictionary<string, UINavMeshNode> _nodes = new Dictionary<string, UINavMeshNode>();

    // The currently selected UINavMeshNode. This node determines the starting point
    // for the next navigation action.
    private UINavMeshNode _currentNode;

    // --- Input Debounce Settings ---
    // These help prevent rapid, unintended navigations if an input key is held down,
    // making the navigation feel more controlled and responsive to distinct presses.
    [Tooltip("Time in seconds to wait between allowed navigation inputs.")]
    [SerializeField] private float _inputDelay = 0.2f;
    private float _lastInputTime = 0f;

    private void Awake()
    {
        // --- Singleton Enforcement ---
        // Ensures only one instance of UINavMeshManager exists in the scene.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple UINavMeshManager instances found. Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, uncomment the line below if this manager should persist across scene changes.
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Attempt to set the initial node after a small delay.
        // This delay ensures that all UINavMeshNodes have had a chance to
        // register themselves with the manager via their OnEnable methods.
        if (!string.IsNullOrEmpty(_initialNodeID))
        {
            Invoke(nameof(SetInitialNode), 0.05f); 
        }
        else
        {
            Debug.LogWarning("No Initial Node ID specified for UINavMeshManager. UI navigation will not start automatically.", this);
        }
    }

    /// <summary>
    /// Attempts to set the node specified by `_initialNodeID` as the current selected node.
    /// Called after a short delay in Start().
    /// </summary>
    private void SetInitialNode()
    {
        if (_nodes.TryGetValue(_initialNodeID, out UINavMeshNode initialNode))
        {
            SetCurrentNode(initialNode);
        }
        else
        {
            Debug.LogWarning($"Initial Node ID '{_initialNodeID}' not found in registered UINavMeshNodes. " +
                             "Please ensure the node exists, is active, and has the correct ID.", this);
        }
    }

    private void Update()
    {
        // Ensure an EventSystem is present and enough time has passed since the last navigation input.
        if (EventSystem.current == null || Time.time < _lastInputTime + _inputDelay)
        {
            return;
        }

        // --- Input Handling ---
        // We capture raw input axis values for typical UI navigation.
        // Prioritize cardinal directions to avoid conflicting diagonal inputs.
        UINavMeshDirection? navigationInput = null;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal > 0.1f) navigationInput = UINavMeshDirection.Right;
        else if (horizontal < -0.1f) navigationInput = UINavMeshDirection.Left;
        else if (vertical > 0.1f) navigationInput = UINavMeshDirection.Up;
        else if (vertical < -0.1f) navigationInput = UINavMeshDirection.Down;

        // Example for specific key inputs (uncomment and modify as needed)
        // if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) navigationInput = UINavMeshDirection.Right;
        // else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) navigationInput = UINavMeshDirection.Left;
        // else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) navigationInput = UINavMeshDirection.Up;
        // else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) navigationInput = UINavMeshDirection.Down;


        if (navigationInput.HasValue)
        {
            Navigate(navigationInput.Value);
            _lastInputTime = Time.time; // Reset the debounce timer.
        }
    }

    /// <summary>
    /// Registers a UINavMeshNode with the system. Nodes call this themselves in OnEnable.
    /// </summary>
    /// <param name="node">The UINavMeshNode component to register.</param>
    public void RegisterNode(UINavMeshNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.NodeID))
        {
            Debug.LogError("Attempted to register a null or unnamed UINavMeshNode.", node);
            return;
        }

        if (_nodes.ContainsKey(node.NodeID))
        {
            Debug.LogWarning($"A UINavMeshNode with ID '{node.NodeID}' already exists. Overwriting with new instance.", node);
            _nodes[node.NodeID] = node;
        }
        else
        {
            _nodes.Add(node.NodeID, node);
            // Debug.Log($"Registered UINavMeshNode: {node.NodeID}", node);
        }
    }

    /// <summary>
    /// Unregisters a UINavMeshNode from the system. Nodes call this themselves in OnDisable.
    /// </summary>
    /// <param name="node">The UINavMeshNode component to unregister.</param>
    public void UnregisterNode(UINavMeshNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.NodeID)) return;

        if (_nodes.ContainsKey(node.NodeID))
        {
            _nodes.Remove(node.NodeID);
            // Debug.Log($"Unregistered UINavMeshNode: {node.NodeID}", node);

            // If the unregistered node was the currently selected node, clear the selection.
            if (_currentNode == node)
            {
                _currentNode = null;
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to navigate from the current node in the specified direction.
    /// This is the core logic for moving selection within the custom UI graph.
    /// </summary>
    /// <param name="direction">The desired navigation direction.</param>
    public void Navigate(UINavMeshDirection direction)
    {
        // If no node is currently selected, try to select the initial node.
        // This handles cases where the first node becomes inactive or was never set.
        if (_currentNode == null)
        {
            SetInitialNode(); 
            if (_currentNode == null) return; // Still no current node, cannot navigate.
        }

        // Query the current node for the target node ID in the given direction.
        string targetNodeID = _currentNode.GetTargetNodeID(direction);

        if (!string.IsNullOrEmpty(targetNodeID))
        {
            // If a target ID is found, attempt to retrieve the actual UINavMeshNode.
            if (_nodes.TryGetValue(targetNodeID, out UINavMeshNode targetNode))
            {
                SetCurrentNode(targetNode); // Move selection to the target node.
            }
            else
            {
                Debug.LogWarning($"Target Node ID '{targetNodeID}' not found for direction '{direction}' from node '{_currentNode.NodeID}'. " +
                                 "Check connections in the Inspector.", _currentNode);
            }
        }
        else
        {
            // Debug.Log($"No connection defined for direction '{direction}' from node '{_currentNode.NodeID}'.", _currentNode);
        }
    }

    /// <summary>
    /// Sets a specific UINavMeshNode as the currently selected node.
    /// This method updates the internal state and notifies Unity's EventSystem
    /// to visually select the corresponding GameObject.
    /// </summary>
    /// <param name="newNode">The UINavMeshNode to select.</param>
    public void SetCurrentNode(UINavMeshNode newNode)
    {
        if (newNode == null || newNode.selectable == null)
        {
            Debug.LogWarning("Attempted to set a null or invalid UINavMeshNode (or its selectable is null) as current. Clearing selection.", newNode);
            _currentNode = null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return;
        }
        if (!newNode.isActiveAndEnabled)
        {
            Debug.LogWarning($"Attempted to select inactive UINavMeshNode: {newNode.NodeID}. Clearing selection.", newNode);
            _currentNode = null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        _currentNode = newNode;
        
        // Notify Unity's EventSystem to highlight and make the new node active.
        // This leverages Unity's existing UI selection feedback mechanisms.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(newNode.selectable.gameObject);
        }
        else
        {
            Debug.LogError("No EventSystem found in the scene! UI navigation will not function correctly. Please add an EventSystem GameObject.", this);
        }

        // You could add custom visual feedback here, e.g., playing a sound,
        // triggering an animation, or changing a highlight state on the newNode.selectable.
    }

    /// <summary>
    /// Public method to programmatically select a node by its ID.
    /// Useful for externally controlling UI flow (e.g., from a game manager
    /// when a new menu panel opens).
    /// </summary>
    /// <param name="nodeID">The NodeID of the target node to select.</param>
    public void SelectNodeByID(string nodeID)
    {
        if (_nodes.TryGetValue(nodeID, out UINavMeshNode node))
        {
            SetCurrentNode(node);
        }
        else
        {
            Debug.LogWarning($"Attempted to select node with ID '{nodeID}', but it was not found in registered nodes.", this);
        }
    }

    /// <summary>
    /// For debugging: Get the currently selected node ID.
    /// </summary>
    public string GetCurrentNodeID()
    {
        return _currentNode != null ? _currentNode.NodeID : "None";
    }
}
```

---

### How to Implement and Use in Unity

Here’s a step-by-step guide to integrate and use the `UINavMeshSystem` in your Unity project:

1.  **Create the Scripts:**
    *   Create a new C# script named `UINavMeshDirection` and copy the code.
    *   Create a new C# script named `UINavMeshConnection` and copy the code.
    *   Create a new C# script named `UINavMeshNode` and copy the code.
    *   Create a new C# script named `UINavMeshManager` and copy the code.

2.  **Setup the `UINavMeshManager`:**
    *   In your Unity scene (typically on a Canvas or a persistent UI manager scene), create an empty GameObject.
    *   Rename it to `UINavMeshManager`.
    *   Attach the `UINavMeshManager.cs` script to this GameObject.
    *   In the Inspector, set the **"Initial Node ID"** to the `NodeID` of the UI element you want to be selected first when the UI appears. You can also adjust the `Input Delay` if needed.

3.  **Setup Your UI Elements:**
    *   Create your UI elements (e.g., `Button`, `Slider`, `Toggle`, `InputField`) on a Canvas as usual.
    *   For **each** UI element that you want to be part of the custom navigation:
        *   Select the GameObject of the UI element (e.g., "PlayButton").
        *   Add the `UINavMeshNode.cs` script to it.
        *   **Node ID:** Assign a unique string (e.g., "PlayButton", "OptionsSlider", "QuitButton"). This is how the manager identifies the node.
        *   **Selectable:** Ensure the `Selectable` field is populated. If you add `UINavMeshNode` to a GameObject that *has* a `Selectable` component (like a Button), it will automatically find it. Otherwise, drag the `Selectable` component from the same GameObject into this field.
        *   **Connections:** Expand the `Connections` array.
            *   For each direction (Up, Down, Left, Right) that you want to navigate *from* this node, add a new entry.
            *   Set the `Direction` (e.g., `Down`).
            *   Set the `Target Node ID` to the `NodeID` of the `UINavMeshNode` you want to move to in that direction.
            *   Leave directions blank if there's no navigation path in that direction.

4.  **Ensure an `EventSystem` Exists:**
    *   Make sure there's an `EventSystem` GameObject in your scene. Unity typically adds one automatically when you create a Canvas. Our system relies on `EventSystem.current.SetSelectedGameObject()` for visual highlighting.

5.  **Test It!**
    *   Run your scene. The `UINavMeshManager` will try to select the `UINavMeshNode` specified by your `Initial Node ID`.
    *   Use your keyboard arrow keys or gamepad D-pad (which map to Unity's "Horizontal" and "Vertical" input axes) to navigate between your UI elements. You should observe navigation following the custom connections you defined, rather than a rigid grid.

---

### Example Scenario: A Custom Main Menu Layout

Imagine a Main Menu with a "Play" button, an "Options" button, a "Quit" button, and an "Extras" button that is visually positioned off to the right of "Options", but you only want to reach it directly from "Options".

**UI Hierarchy:**
*   Canvas
    *   PlayButton (Button)
    *   OptionsButton (Button)
    *   QuitButton (Button)
    *   ExtrasButton (Button)

**`UINavMeshManager` Setup:**
*   **Initial Node ID:** "PlayButton"

**`UINavMeshNode` Setup on each Button:**

1.  **PlayButton (GameObject):**
    *   `UINavMeshNode` component:
        *   **Node ID:** "PlayButton"
        *   **Connections:**
            *   `Direction: Down`, `Target Node ID: OptionsButton`
            *   (No Up, Left, Right connections)

2.  **OptionsButton (GameObject):**
    *   `UINavMeshNode` component:
        *   **Node ID:** "OptionsButton"
        *   **Connections:**
            *   `Direction: Up`, `Target Node ID: PlayButton`
            *   `Direction: Down`, `Target Node ID: QuitButton`
            *   `Direction: Right`, `Target Node ID: ExtrasButton`
            *   (No Left connection)

3.  **QuitButton (GameObject):**
    *   `UINavMeshNode` component:
        *   **Node ID:** "QuitButton"
        *   **Connections:**
            *   `Direction: Up`, `Target Node ID: OptionsButton`
            *   (No Down, Left, Right connections)

4.  **ExtrasButton (GameObject):**
    *   `UINavMeshNode` component:
        *   **Node ID:** "ExtrasButton"
        *   **Connections:**
            *   `Direction: Left`, `Target Node ID: OptionsButton`
            *   (No Up, Down, Right connections)

**Result:**
*   Starting on "PlayButton", pressing `Down` goes to "OptionsButton".
*   From "OptionsButton", pressing `Up` goes to "PlayButton", `Down` goes to "QuitButton", and `Right` goes to "ExtrasButton".
*   From "ExtrasButton", only pressing `Left` will take you back to "OptionsButton". Pressing Up/Down/Right will do nothing because no connections are defined.
*   Crucially, you cannot navigate directly from "PlayButton" to "ExtrasButton" or from "QuitButton" to "ExtrasButton", even if they are visually close. Navigation strictly follows the defined "NavMesh" connections.

This example clearly demonstrates the power and flexibility of the `UINavMeshSystem` for creating custom UI navigation flows beyond standard grid-based or automatic systems.