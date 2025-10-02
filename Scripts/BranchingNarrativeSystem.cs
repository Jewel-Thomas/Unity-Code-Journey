// Unity Design Pattern Example: BranchingNarrativeSystem
// This script demonstrates the BranchingNarrativeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete and practical implementation of the Branching Narrative System design pattern in Unity. It uses `ScriptableObject` for narrative nodes, making the content data-driven and easily manageable within the Unity editor, and a `MonoBehaviour` to control the flow.

---

### **Branching Narrative System - Unity Example**

This system allows you to create interactive, branching storylines for games, visual novels, or tutorial sequences.

**Core Components:**

1.  **`NarrativeOption` (Serializable Class):** Represents a single choice the player can make, leading to another narrative state.
2.  **`NarrativeNode` (ScriptableObject):** Represents a single state or point in the narrative. It contains the dialogue text and a list of `NarrativeOption`s that lead to subsequent nodes.
3.  **`BranchingNarrativeSystem` (MonoBehaviour):** The central controller that manages the current narrative node, displays its content, and handles player choices to transition between nodes.

---

### **How to Use:**

1.  **Create C# Scripts:**
    *   Create a C# script named `NarrativeOption.cs`.
    *   Create a C# script named `NarrativeNode.cs`.
    *   Create a C# script named `BranchingNarrativeSystem.cs`.
    *   Copy the respective code into each file.

2.  **Create Narrative Nodes (ScriptableObjects):**
    *   In the Unity editor, go to `Assets > Create > Narrative > Narrative Node`.
    *   Create multiple `NarrativeNode` assets (e.g., "StartNode", "PathANode", "PathBNode", "EndingNode").
    *   Select each node in the Project window and fill out its `Dialogue Text` in the Inspector.
    *   **Crucially, add `Narrative Options`:**
        *   For each option, provide `Option Text`.
        *   Drag and drop another `NarrativeNode` asset from your Project window into the `Next Node` slot of the option. This creates the branches.
        *   For an ending node, you might leave its `Options` list empty.

3.  **Set up the `BranchingNarrativeSystem`:**
    *   Create an empty GameObject in your scene (e.g., "NarrativeManager").
    *   Add the `BranchingNarrativeSystem` component to this GameObject.
    *   Drag your designated "StartNode" `NarrativeNode` asset from the Project window into the `Starting Node` slot in the Inspector of the `BranchingNarrativeSystem` component.

4.  **Run the Scene:**
    *   Press Play in Unity.
    *   The system will start with the `Starting Node`.
    *   Dialogue text and options will be printed to the Console (simulating UI output).
    *   You can call `BranchingNarrativeSystem.Instance.ChooseOption(index)` from other scripts (e.g., UI buttons) to navigate the narrative. For demonstration, I've added keyboard input.

---

### **1. `NarrativeOption.cs`**

This class defines a single choice within a narrative node.

```csharp
using UnityEngine;
using System; // Required for [System.Serializable]

/// <summary>
/// [Branching Narrative System]
/// Represents a single choice or option presented to the player within a NarrativeNode.
/// </summary>
/// <remarks>
/// This class is marked with [System.Serializable] so that Unity can save and load
/// instances of it when they are embedded within a ScriptableObject (NarrativeNode).
/// Each option has text to display to the player and a direct reference to the next
/// NarrativeNode it leads to if chosen. This direct reference is the core mechanism
/// for creating the "branches" in the narrative.
/// </remarks>
[System.Serializable]
public class NarrativeOption
{
    [Tooltip("The text displayed for this option (e.g., 'Go left', 'Talk to the guard').")]
    public string optionText;

    [Tooltip("The next NarrativeNode this option leads to.")]
    public NarrativeNode nextNode;

    // Optional: Add more fields here for advanced options, like:
    // - Requirements (e.g., player needs a key)
    // - Consequences (e.g., grants an item, changes a variable)
    // - A UnityEvent or method name to call when this option is chosen
}
```

---

### **2. `NarrativeNode.cs`**

This `ScriptableObject` represents a single point in your story, containing dialogue and potential choices.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [Branching Narrative System]
/// Represents a single state or point in the interactive narrative.
/// </summary>
/// <remarks>
/// This is a ScriptableObject, which means it's a data asset that can be created
/// and configured directly in the Unity editor. This makes it ideal for defining
/// narrative content (dialogue, choices) separate from scene objects.
/// Each NarrativeNode has:
/// - A unique ID (optional, but good practice for advanced features like saving state or debugging).
/// - The main dialogue text that will be displayed to the player.
/// - A list of 'NarrativeOption' objects, each representing a possible choice
///   the player can make, leading to a different NarrativeNode. If this list is empty,
///   it typically signifies an end to a particular branch of the narrative.
/// </remarks>
[CreateAssetMenu(fileName = "NewNarrativeNode", menuName = "Narrative/Narrative Node", order = 1)]
public class NarrativeNode : ScriptableObject
{
    [Tooltip("A unique identifier for this narrative node (useful for saving/loading, or specific jumps).")]
    public string nodeID;

    [TextArea(3, 10)] // Makes the string field a multi-line text area in the Inspector
    [Tooltip("The main dialogue or narrative text for this node.")]
    public string dialogueText;

    [Tooltip("The list of options available to the player from this node.")]
    public List<NarrativeOption> options;

    /// <summary>
    /// Called when the ScriptableObject is created or loaded.
    /// Ensures the nodeID is unique if not set.
    /// </summary>
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(nodeID))
        {
            // Use the asset's name as a default ID if none is provided.
            // For production, consider a more robust unique ID generation.
            nodeID = this.name;
        }
    }
}
```

---

### **3. `BranchingNarrativeSystem.cs`**

The central manager that orchestrates the narrative flow.

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.Collections.Generic; // Required for List

/// <summary>
/// [Branching Narrative System]
/// The central manager for controlling and displaying branching narratives.
/// </summary>
/// <remarks>
/// This MonoBehaviour acts as the primary interface for interacting with the
/// narrative system. It holds the current state (which NarrativeNode is active)
/// and provides methods to advance the story based on player choices.
///
/// It uses UnityEvents to decouple the narrative logic from the UI presentation.
/// UI elements (like text boxes, buttons) can subscribe to these events to
/// update themselves whenever the dialogue or options change.
///
/// **How the pattern works:**
/// 1.  **Nodes:** The story is broken down into discrete `NarrativeNode` objects. Each node
///     represents a piece of dialogue and a set of possible choices.
/// 2.  **Options:** Each choice (`NarrativeOption`) within a node points directly to the `NarrativeNode`
///     that should be displayed next if that choice is selected. This forms the "branches".
/// 3.  **Current State:** The `BranchingNarrativeSystem` keeps track of the `currentNode`
///     the player is currently viewing.
/// 4.  **Transitions:** When a player makes a choice, the system updates `currentNode` to
///     the `nextNode` associated with that choice and then displays the content of the new node.
/// 5.  **Termination:** A branch ends when a `NarrativeNode` has no `options` to present.
/// </remarks>
public class BranchingNarrativeSystem : MonoBehaviour
{
    // --- Singleton Pattern (Optional, but common for managers) ---
    public static BranchingNarrativeSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of BranchingNarrativeSystem found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make this object persist across scenes
            // DontDestroyOnLoad(gameObject); 
        }
    }
    // -----------------------------------------------------------

    [Header("Narrative Setup")]
    [SerializeField]
    [Tooltip("The starting point of this narrative branch.")]
    private NarrativeNode startingNode;

    // Private field to hold the current active node
    private NarrativeNode currentNode;

    [Header("Unity Events for UI Integration")]
    [Tooltip("Fired when the main dialogue text needs to be updated.")]
    public UnityEvent<string> OnDialogueTextUpdate;

    [Tooltip("Fired when the available options need to be updated. Provides a list of NarrativeOption objects.")]
    public UnityEvent<List<NarrativeOption>> OnOptionsUpdate;

    [Tooltip("Fired when the narrative reaches an end node (a node with no options).")]
    public UnityEvent OnNarrativeEnd;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the narrative with the starting node.
    /// </summary>
    void Start()
    {
        if (startingNode == null)
        {
            Debug.LogError("BranchingNarrativeSystem: Starting Node is not assigned!", this);
            return;
        }

        SetCurrentNode(startingNode);
    }

    /// <summary>
    /// Sets the current narrative node and triggers UI updates.
    /// </summary>
    /// <param name="node">The NarrativeNode to set as current.</param>
    private void SetCurrentNode(NarrativeNode node)
    {
        currentNode = node;
        DisplayCurrentNode();
    }

    /// <summary>
    /// Displays the content of the current narrative node by invoking Unity Events.
    /// </summary>
    private void DisplayCurrentNode()
    {
        if (currentNode == null)
        {
            Debug.LogError("BranchingNarrativeSystem: Attempted to display a null node.");
            return;
        }

        // Invoke events for UI elements to update
        OnDialogueTextUpdate.Invoke(currentNode.dialogueText);
        OnOptionsUpdate.Invoke(currentNode.options);

        // --- Console Output for Demonstration ---
        Debug.Log($"--- CURRENT NODE: {currentNode.name} ---");
        Debug.Log($"Dialogue: {currentNode.dialogueText}");

        if (currentNode.options != null && currentNode.options.Count > 0)
        {
            Debug.Log("Options:");
            for (int i = 0; i < currentNode.options.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {currentNode.options[i].optionText}");
            }
        }
        else
        {
            Debug.Log("--- END OF NARRATIVE BRANCH ---");
            OnNarrativeEnd.Invoke(); // Notify that the narrative has ended
        }
        // -----------------------------------------
    }

    /// <summary>
    /// Chooses an option from the current node and advances the narrative.
    /// This method would typically be called by UI button clicks, passing the index
    /// of the chosen option.
    /// </summary>
    /// <param name="optionIndex">The 0-based index of the chosen option.</param>
    public void ChooseOption(int optionIndex)
    {
        if (currentNode == null || currentNode.options == null || currentNode.options.Count == 0)
        {
            Debug.LogWarning("BranchingNarrativeSystem: No options available or narrative has ended.");
            return;
        }

        if (optionIndex < 0 || optionIndex >= currentNode.options.Count)
        {
            Debug.LogError($"BranchingNarrativeSystem: Invalid option index {optionIndex}. Must be between 0 and {currentNode.options.Count - 1}.");
            return;
        }

        NarrativeOption chosenOption = currentNode.options[optionIndex];

        if (chosenOption.nextNode != null)
        {
            Debug.Log($"Player chose: '{chosenOption.optionText}'");
            SetCurrentNode(chosenOption.nextNode); // Transition to the next node
        }
        else
        {
            Debug.LogWarning($"BranchingNarrativeSystem: Option '{chosenOption.optionText}' does not lead to a next node.");
            OnNarrativeEnd.Invoke(); // It's a dead end, so consider it the end of the branch.
        }
    }

    // --- Example of how to integrate with input (for demonstration) ---
    void Update()
    {
        if (currentNode != null && currentNode.options != null && currentNode.options.Count > 0)
        {
            for (int i = 0; i < currentNode.options.Count; i++)
            {
                // Use keyboard numbers 1, 2, 3... to select options
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ChooseOption(i);
                    break; // Only choose one option per frame
                }
            }
        }
    }
    // ------------------------------------------------------------------

    // --- Public methods for external control (e.g., reset, jump to node) ---

    /// <summary>
    /// Resets the narrative back to the starting node.
    /// </summary>
    public void ResetNarrative()
    {
        if (startingNode != null)
        {
            SetCurrentNode(startingNode);
            Debug.Log("Narrative Reset to Starting Node.");
        }
        else
        {
            Debug.LogWarning("Cannot reset narrative: Starting Node is not assigned.");
        }
    }

    // Optional: Add a method to jump to a specific node by ID
    // public void JumpToNode(string nodeID) { /* ... implement lookup ... */ }
}
```