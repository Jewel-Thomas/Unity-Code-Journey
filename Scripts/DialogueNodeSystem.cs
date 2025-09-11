// Unity Design Pattern Example: DialogueNodeSystem
// This script demonstrates the DialogueNodeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Dialogue Node System** design pattern, making it practical for building interactive dialogue in your games.

It includes:
*   **`DialogueNode`**: Represents a single piece of dialogue or a decision point.
*   **`DialogueChoice`**: Represents an option a player can select, leading to another node.
*   **`DialogueGraph`**: A `ScriptableObject` that holds a collection of connected `DialogueNode`s, defining the entire dialogue flow.
*   **`DialogueSystem`**: A `MonoBehaviour` that manages the traversal of the `DialogueGraph`, emits events for UI, and handles player input.

**How the DialogueNodeSystem Pattern Works:**

1.  **Nodes as Data Units:** Each distinct piece of dialogue or interaction is encapsulated in a "node." This node contains the speaker, the text, and information about what happens next.
2.  **Edges (Choices/Links):** Connections between nodes are "edges." In this system, these are represented by `DialogueChoice` objects or a direct `nextNodeID` within a `DialogueNode`. A node can have multiple choices (branching dialogue) or a single, linear progression.
3.  **Graph Structure:** The entire conversation is structured as a graph, where nodes are vertices and choices/links are edges. This allows for complex branching narratives, loops, and convergent storylines.
4.  **Traversal Management:** A central "system" or "manager" component traverses this graph. It keeps track of the current node, presents its content, processes player input (e.g., making a choice), and moves to the next appropriate node.
5.  **Decoupled UI:** The dialogue system typically operates independently of the UI. It emits events (e.g., "OnNodeChanged," "OnDialogueEnded") that a separate UI manager subscribes to, updating the visual elements without the core system needing to know about UI specifics.

---

### `DialogueSystem.cs`

Create a new C# script named `DialogueSystem` in your Unity project and paste the code below.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .FirstOrDefault()

/// <summary>
/// Represents a single choice a player can make, leading to another dialogue node.
/// </summary>
/// <remarks>
/// This struct is marked as [Serializable] so Unity can save its data within a ScriptableObject.
/// It defines the text displayed for the choice and the ID of the node it leads to.
/// </remarks>
[Serializable]
public struct DialogueChoice
{
    public string choiceText;      // The text displayed to the player for this choice.
    public string nextNodeID;      // The unique ID of the DialogueNode this choice will lead to.
}

/// <summary>
/// Represents a single node in the dialogue graph.
/// Each node holds dialogue text, speaker information, and defines how to proceed (linearly or via choices).
/// </summary>
/// <remarks>
/// This class is marked as [Serializable] so Unity can embed its instances within a ScriptableObject (DialogueGraph).
/// If you wanted a more complex node system (e.g., custom editor, different node types with unique logic),
/// you might make DialogueNode itself a ScriptableObject, but for a practical example demonstrating the pattern,
/// embedding them within DialogueGraph is simpler for initial setup.
/// </remarks>
[Serializable]
public class DialogueNode
{
    public string id;                      // A unique identifier for this node (e.g., "Intro1", "ChoicePathA").
    public string speakerName;             // The name of the character speaking this line.
    [TextArea(3, 10)]
    public string dialogueText;            // The actual dialogue text for this node.

    public List<DialogueChoice> choices;   // A list of choices the player can make from this node.
                                           // If this list is empty, the dialogue proceeds linearly.

    public string nextNodeID;              // The ID of the next node if no choices are present.
                                           // Used for linear progression.

    public bool isEndNode;                 // True if this node marks the end of a dialogue path.

    /// <summary>
    /// Checks if this node offers choices.
    /// </summary>
    public bool HasChoices => choices != null && choices.Count > 0;

    /// <summary>
    /// Validates if the node has a valid next step defined (either choices or a direct next node).
    /// </summary>
    public bool HasValidNextStep()
    {
        if (isEndNode) return true; // End nodes don't need a next step
        if (HasChoices) return true; // Choices define next steps
        return !string.IsNullOrEmpty(nextNodeID); // Linear progression
    }
}

/// <summary>
/// A ScriptableObject representing an entire dialogue conversation graph.
/// It contains a collection of DialogueNodes and manages their unique IDs.
/// </summary>
/// <remarks>
/// Using ScriptableObject allows you to create dialogue assets in the Unity Editor (Assets -> Create -> Dialogue -> Dialogue Graph).
/// This separates dialogue content from runtime logic, making it easy to create and manage many conversations.
/// </remarks>
[CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue/Dialogue Graph")]
public class DialogueGraph : ScriptableObject
{
    public string graphName;              // A descriptive name for this dialogue graph.
    public List<DialogueNode> nodes = new List<DialogueNode>(); // All nodes in this graph.

    [SerializeField]
    private string _startNodeID;          // The ID of the node where this dialogue graph begins.

    /// <summary>
    /// The ID of the starting node for this dialogue graph.
    /// Ensures it's always valid or points to the first node if none specified.
    /// </summary>
    public string StartNodeID
    {
        get
        {
            // If no start node is explicitly set, try to use the first node in the list.
            if (string.IsNullOrEmpty(_startNodeID) && nodes.Count > 0)
            {
                _startNodeID = nodes[0].id;
            }
            return _startNodeID;
        }
        set
        {
            _startNodeID = value;
        }
    }

    private Dictionary<string, DialogueNode> _nodeLookup; // Cache for quick node lookup by ID.

    /// <summary>
    /// Initializes the node lookup dictionary for efficient access.
    /// Should be called after loading or modifying the graph.
    /// </summary>
    public void InitializeNodeLookup()
    {
        _nodeLookup = new Dictionary<string, DialogueNode>();
        foreach (DialogueNode node in nodes)
        {
            if (_nodeLookup.ContainsKey(node.id))
            {
                Debug.LogWarning($"DialogueGraph '{graphName}': Duplicate node ID '{node.id}' found. Skipping duplicate.");
                continue;
            }
            _nodeLookup.Add(node.id, node);
        }
    }

    /// <summary>
    /// Retrieves a DialogueNode by its unique ID.
    /// </summary>
    /// <param name="nodeID">The ID of the node to retrieve.</param>
    /// <returns>The DialogueNode with the specified ID, or null if not found.</returns>
    public DialogueNode GetNode(string nodeID)
    {
        if (_nodeLookup == null || _nodeLookup.Count == 0)
        {
            InitializeNodeLookup();
        }

        if (string.IsNullOrEmpty(nodeID))
        {
            Debug.LogWarning($"Attempted to get node with null or empty ID in graph '{graphName}'.");
            return null;
        }

        if (_nodeLookup.TryGetValue(nodeID, out DialogueNode node))
        {
            return node;
        }

        Debug.LogError($"DialogueGraph '{graphName}': Node with ID '{nodeID}' not found!");
        return null;
    }

    /// <summary>
    /// Called when the ScriptableObject is loaded.
    /// </summary>
    private void OnEnable()
    {
        // When the graph is loaded (e.g., in editor or at runtime), ensure the lookup table is initialized.
        InitializeNodeLookup();
    }

    /// <summary>
    /// Validates the graph structure to ensure all referenced nodes exist.
    /// </summary>
    public void ValidateGraph()
    {
        if (nodes.Count == 0)
        {
            Debug.LogWarning($"DialogueGraph '{graphName}' is empty.");
            return;
        }

        // Validate start node
        if (GetNode(StartNodeID) == null)
        {
            Debug.LogError($"DialogueGraph '{graphName}': Start node ID '{StartNodeID}' is invalid or not found. Please set a valid start node.");
        }

        foreach (var node in nodes)
        {
            if (!node.HasValidNextStep() && !node.isEndNode)
            {
                 Debug.LogWarning($"DialogueGraph '{graphName}': Node '{node.id}' has no valid next step (no choices, no nextNodeID, and not marked as end node). It will lead to a dead end.");
            }

            if (node.HasChoices)
            {
                foreach (var choice in node.choices)
                {
                    if (GetNode(choice.nextNodeID) == null)
                    {
                        Debug.LogError($"DialogueGraph '{graphName}': Node '{node.id}' has a choice leading to an invalid node ID '{choice.nextNodeID}'.");
                    }
                }
            }
            else if (!node.isEndNode && !string.IsNullOrEmpty(node.nextNodeID))
            {
                if (GetNode(node.nextNodeID) == null)
                {
                    Debug.LogError($"DialogueGraph '{graphName}': Node '{node.id}' has a linear progression to an invalid node ID '{node.nextNodeID}'.");
                }
            }
        }
    }
}


/// <summary>
/// The central manager for playing dialogue.
/// This MonoBehaviour manages the state of the dialogue, traverses the graph,
/// and provides events for UI and other game systems to react to dialogue changes.
/// </summary>
/// <remarks>
/// This class acts as the "controller" in the MVC pattern (Model: DialogueGraph, View: UI, Controller: DialogueSystem).
/// It should be placed on a GameObject in your scene.
/// </remarks>
public class DialogueSystem : MonoBehaviour
{
    [Tooltip("The DialogueGraph asset currently being played.")]
    [SerializeField] private DialogueGraph _currentDialogueGraph;

    private DialogueNode _currentNode; // The currently active dialogue node.

    // --- Events for UI and Game Logic ---
    // These events allow other scripts (like a UI Manager) to subscribe and react to dialogue state changes.
    // Example: DialogueUI.OnDialogueNodeChanged += UpdateDisplay;

    /// <summary>
    /// Event fired when a new dialogue conversation begins. Provides the starting node.
    /// </summary>
    public event Action<DialogueNode> OnDialogueStarted;

    /// <summary>
    /// Event fired when the current dialogue node changes (new speaker, new text, new choices).
    /// </summary>
    public event Action<DialogueNode> OnDialogueNodeChanged;

    /// <summary>
    /// Event fired when the dialogue conversation ends.
    /// </summary>
    public event Action OnDialogueEnded;

    /// <summary>
    /// Get the currently active dialogue node.
    /// </summary>
    public DialogueNode CurrentNode => _currentNode;

    /// <summary>
    /// Get the currently active dialogue graph.
    /// </summary>
    public DialogueGraph CurrentDialogueGraph => _currentDialogueGraph;


    /// <summary>
    /// Initializes and starts a new dialogue conversation using the specified graph.
    /// </summary>
    /// <param name="graph">The DialogueGraph asset to start playing.</param>
    public void StartDialogue(DialogueGraph graph)
    {
        if (graph == null)
        {
            Debug.LogError("Cannot start dialogue: Provided DialogueGraph is null.");
            return;
        }

        _currentDialogueGraph = graph;
        _currentDialogueGraph.InitializeNodeLookup(); // Ensure lookup is ready

        _currentNode = _currentDialogueGraph.GetNode(_currentDialogueGraph.StartNodeID);

        if (_currentNode == null)
        {
            Debug.LogError($"DialogueGraph '{graph.graphName}' has no valid start node (ID: '{graph.StartNodeID}'). Dialogue cannot start.");
            OnDialogueEnded?.Invoke(); // Immediately end if cannot start
            return;
        }

        Debug.Log($"Dialogue started: '{graph.graphName}'. Current Node: '{_currentNode.id}' ({_currentNode.speakerName}: {_currentNode.dialogueText})");
        OnDialogueStarted?.Invoke(_currentNode);
        OnDialogueNodeChanged?.Invoke(_currentNode); // Immediately show the first node
    }

    /// <summary>
    /// Continues the dialogue to the next node when there are no choices.
    /// This is typically called by a "Continue" button in the UI.
    /// </summary>
    public void ContinueDialogue()
    {
        if (_currentNode == null || _currentDialogueGraph == null)
        {
            Debug.LogWarning("Cannot continue dialogue: No current dialogue active.");
            return;
        }

        // If the current node is an end node, the dialogue concludes.
        if (_currentNode.isEndNode)
        {
            EndDialogue();
            return;
        }

        // If the current node has choices, the player must make one, not just "continue".
        if (_currentNode.HasChoices)
        {
            Debug.LogWarning("Cannot use ContinueDialogue(). This node has choices. Please use MakeChoice().");
            return;
        }

        // Move to the next node based on nextNodeID.
        if (!string.IsNullOrEmpty(_currentNode.nextNodeID))
        {
            SetCurrentNode(_currentNode.nextNodeID);
        }
        else
        {
            // If no nextNodeID and not an end node, it's an unexpected dead end.
            Debug.LogWarning($"DialogueGraph '{_currentDialogueGraph.graphName}': Node '{_currentNode.id}' has no choices or next node defined, and is not marked as an end node. Ending dialogue.");
            EndDialogue();
        }
    }

    /// <summary>
    /// Makes a choice from the current dialogue node and proceeds to the next node.
    /// This is typically called by a choice button in the UI.
    /// </summary>
    /// <param name="choiceIndex">The index of the choice in the current node's choices list.</param>
    public void MakeChoice(int choiceIndex)
    {
        if (_currentNode == null || _currentDialogueGraph == null)
        {
            Debug.LogWarning("Cannot make choice: No current dialogue active.");
            return;
        }

        if (!_currentNode.HasChoices || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
        {
            Debug.LogError($"Invalid choice index {choiceIndex} for node '{_currentNode.id}'. It either has no choices or the index is out of bounds.");
            return;
        }

        string nextNodeID = _currentNode.choices[choiceIndex].nextNodeID;
        Debug.Log($"Choice made: '{_currentNode.choices[choiceIndex].choiceText}'. Moving to node: '{nextNodeID}'");
        SetCurrentNode(nextNodeID);
    }

    /// <summary>
    /// Sets the current dialogue node and triggers the OnDialogueNodeChanged event.
    /// </summary>
    /// <param name="nodeID">The ID of the next node to transition to.</param>
    private void SetCurrentNode(string nodeID)
    {
        _currentNode = _currentDialogueGraph.GetNode(nodeID);

        if (_currentNode == null)
        {
            Debug.LogError($"Could not find node with ID '{nodeID}'. Ending dialogue due to missing node.");
            EndDialogue();
            return;
        }

        if (_currentNode.isEndNode)
        {
            Debug.Log($"Reached end node: '{_currentNode.id}' ({_currentNode.speakerName}: {_currentNode.dialogueText})");
            OnDialogueNodeChanged?.Invoke(_currentNode); // Show the end node's text
            EndDialogue(); // The end node might still have text to display before ending.
            return;
        }

        Debug.Log($"Current Node: '{_currentNode.id}' ({_currentNode.speakerName}: {_currentNode.dialogueText})");
        OnDialogueNodeChanged?.Invoke(_currentNode);
    }

    /// <summary>
    /// Ends the current dialogue conversation.
    /// </summary>
    public void EndDialogue()
    {
        Debug.Log("Dialogue Ended.");
        _currentNode = null;
        _currentDialogueGraph = null;
        OnDialogueEnded?.Invoke();
    }
}
```

---

### **How to Use This in Unity:**

#### 1. Create the DialogueSystem GameObject:

*   In your Unity scene, create an empty GameObject (e.g., named `_DialogueManager`).
*   Attach the `DialogueSystem.cs` script to this GameObject.

#### 2. Create DialogueGraph Assets:

*   In your Project window, right-click -> `Create` -> `Dialogue` -> `Dialogue Graph`.
*   Name it (e.g., `MyFirstDialogue`).
*   Select the `MyFirstDialogue` asset in the Project window.
*   In the Inspector, you'll see a `Graph Name` field and a `Nodes` list.
*   **Populate Nodes:**
    *   Expand the `Nodes` list and increase its `Size`.
    *   For each node:
        *   Give it a unique `ID` (e.g., `start`, `greeting`, `choice1`, `pathA`, `end`).
        *   Fill in `Speaker Name` and `Dialogue Text`.
        *   **For linear progression:** Leave `Choices` empty, and set `Next Node ID` to the ID of the next node.
        *   **For choices/branching:** Fill the `Choices` list:
            *   Set `Choice Text` for each option.
            *   Set `Next Node ID` for each choice to the ID of the node it leads to.
        *   Set `Is End Node` to `true` for nodes that signify the end of a conversation path.
*   **Set the Start Node ID:** At the top of the `DialogueGraph` asset's inspector, make sure `Start Node ID` is set to the ID of your first node (e.g., `start`).

**Example DialogueGraph Setup:**

*   **Graph Name:** `TownsfolkGreeting`
*   **Start Node ID:** `intro`
*   **Nodes:**
    *   **Node 0:**
        *   `ID`: `intro`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `Hello, traveler! How can I help you today?`
        *   `Choices` (Size 2):
            *   **Choice 0:**
                *   `Choice Text`: `Tell me about this town.`
                *   `Next Node ID`: `town_info`
            *   **Choice 1:**
                *   `Choice Text`: `I'm just passing through.`
                *   `Next Node ID`: `farewell`
    *   **Node 1:**
        *   `ID`: `town_info`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `This is a peaceful village, known for its fresh produce. We welcome all who respect our customs.`
        *   `Next Node ID`: `ask_more` (No choices, linear)
    *   **Node 2:**
        *   `ID`: `ask_more`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `Is there anything else you'd like to know?`
        *   `Choices` (Size 2):
            *   **Choice 0:**
                *   `Choice Text`: `Where can I find the market?`
                *   `Next Node ID`: `market_direction`
            *   **Choice 1:**
                *   `Choice Text`: `No, I think I'm good.`
                *   `Next Node ID`: `farewell`
    *   **Node 3:**
        *   `ID`: `market_direction`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `The market is just past the town square, to your left.`
        *   `Next Node ID`: `farewell_market`
    *   **Node 4:**
        *   `ID`: `farewell`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `Safe travels, then!`
        *   `Is End Node`: `true`
    *   **Node 5:**
        *   `ID`: `farewell_market`
        *   `Speaker Name`: `Villager`
        *   `Dialogue Text`: `Enjoy your shopping!`
        *   `Is End Node`: `true`

#### 3. Integrate with Your Game Logic (e.g., an Interactable NPC):

Create a script for an interactive object (like an NPC) to trigger the dialogue.

```csharp
// Example: NPCInteractable.cs
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Tooltip("The DialogueGraph asset this NPC will use.")]
    [SerializeField] private DialogueGraph _npcDialogue;

    [Tooltip("Reference to the DialogueSystem in the scene. Assign in Inspector or find dynamically.")]
    [SerializeField] private DialogueSystem _dialogueSystem;

    private bool _dialogueActive = false; // To prevent starting dialogue multiple times

    void Start()
    {
        // Find the DialogueSystem if not explicitly assigned.
        // For production, consider a more robust service locator or dependency injection.
        if (_dialogueSystem == null)
        {
            _dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (_dialogueSystem == null)
            {
                Debug.LogError("NPCInteractable: DialogueSystem not found in scene!", this);
                enabled = false; // Disable script if no system to interact with
                return;
            }
        }

        // Subscribe to dialogue events to manage interaction state
        _dialogueSystem.OnDialogueStarted += OnDialogueStartedHandler;
        _dialogueSystem.OnDialogueEnded += OnDialogueEndedHandler;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks if the DialogueSystem outlives this NPC
        if (_dialogueSystem != null)
        {
            _dialogueSystem.OnDialogueStarted -= OnDialogueStartedHandler;
            _dialogueSystem.OnDialogueEnded -= OnDialogueEndedHandler;
        }
    }

    /// <summary>
    /// Call this method when the player interacts with the NPC.
    /// </summary>
    public void Interact()
    {
        if (_dialogueSystem == null || _npcDialogue == null)
        {
            Debug.LogWarning("NPCInteractable: DialogueSystem or NPC Dialogue Graph not assigned.", this);
            return;
        }

        if (!_dialogueActive)
        {
            Debug.Log($"NPC {name}: Starting dialogue with graph {_npcDialogue.graphName}");
            _dialogueSystem.StartDialogue(_npcDialogue);
        }
        else
        {
            Debug.Log($"NPC {name}: Dialogue already active. Use DialogueSystem.ContinueDialogue() or MakeChoice() instead.");
        }
    }

    private void OnDialogueStartedHandler(DialogueNode startingNode)
    {
        _dialogueActive = true;
        // Optionally, make NPC look at player, trigger animations, etc.
        Debug.Log($"NPC {name}: Dialogue has started with '{startingNode.speakerName}'.");
    }

    private void OnDialogueEndedHandler()
    {
        _dialogueActive = false;
        // Optionally, make NPC resume idle animations, allow player movement, etc.
        Debug.Log($"NPC {name}: Dialogue has ended.");
    }

    // You could also subscribe to OnDialogueNodeChanged to trigger specific NPC animations
    // based on the current node (e.g., a "pondering" animation for a choice node).
}
```

#### 4. Create a Basic UI for Dialogue Display:

For a real game, you'd use Unity's UI Canvas, Text, and Button components. Here's a conceptual UI script (requires `TextMeshPro` if you want to use `TMP_Text`):

```csharp
// Example: DialogueUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // If using TextMeshPro, otherwise use UnityEngine.UI.Text

public class DialogueUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TMP_Text _speakerNameText; // or public Text speakerNameText;
    [SerializeField] private TMP_Text _dialogueText;   // or public Text dialogueText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Transform _choicesParent; // Parent GameObject for choice buttons
    [SerializeField] private GameObject _choiceButtonPrefab; // Prefab for individual choice buttons

    [Tooltip("Reference to the DialogueSystem in the scene. Assign in Inspector or find dynamically.")]
    [SerializeField] private DialogueSystem _dialogueSystem;

    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    void Awake()
    {
        // Find the DialogueSystem if not explicitly assigned
        if (_dialogueSystem == null)
        {
            _dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (_dialogueSystem == null)
            {
                Debug.LogError("DialogueUI: DialogueSystem not found in scene!", this);
                enabled = false;
                return;
            }
        }

        // Subscribe to events
        _dialogueSystem.OnDialogueStarted += DisplayDialogueUI;
        _dialogueSystem.OnDialogueNodeChanged += UpdateDialogueUI;
        _dialogueSystem.OnDialogueEnded += HideDialogueUI;

        // Add listener to the continue button
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        // Initially hide the dialogue UI
        _dialoguePanel.SetActive(false);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (_dialogueSystem != null)
        {
            _dialogueSystem.OnDialogueStarted -= DisplayDialogueUI;
            _dialogueSystem.OnDialogueNodeChanged -= UpdateDialogueUI;
            _dialogueSystem.OnDialogueEnded -= HideDialogueUI;
        }
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveAllListeners();
        }
    }

    private void DisplayDialogueUI(DialogueNode startingNode)
    {
        _dialoguePanel.SetActive(true);
        // The first node's data will be set by UpdateDialogueUI immediately after this.
        Debug.Log("Dialogue UI: Displaying...");
    }

    private void UpdateDialogueUI(DialogueNode currentNode)
    {
        if (currentNode == null)
        {
            Debug.LogError("DialogueUI: Received null node to update UI with.");
            return;
        }

        // Update speaker name and dialogue text
        _speakerNameText.text = currentNode.speakerName;
        _dialogueText.text = currentNode.dialogueText;

        // Clear previous choices
        foreach (GameObject btn in _activeChoiceButtons)
        {
            Destroy(btn);
        }
        _activeChoiceButtons.Clear();

        // Show/hide continue button and generate choice buttons
        if (currentNode.HasChoices)
        {
            _continueButton.gameObject.SetActive(false);
            GenerateChoiceButtons(currentNode.choices);
        }
        else
        {
            _continueButton.gameObject.SetActive(true);
            // Hide choices panel completely if no choices
            if (_choicesParent != null) _choicesParent.gameObject.SetActive(false);
        }
        Debug.Log($"Dialogue UI: Updated for node '{currentNode.id}'");
    }

    private void GenerateChoiceButtons(List<DialogueChoice> choices)
    {
        if (_choicesParent != null) _choicesParent.gameObject.SetActive(true);
        if (_choiceButtonPrefab == null)
        {
            Debug.LogError("DialogueUI: Choice button prefab is not assigned!");
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            GameObject choiceBtnGO = Instantiate(_choiceButtonPrefab, _choicesParent);
            _activeChoiceButtons.Add(choiceBtnGO);

            // Get the Text and Button components (adjust based on your prefab's structure)
            TMP_Text buttonText = choiceBtnGO.GetComponentInChildren<TMP_Text>(); // or .GetComponent<Text>();
            Button buttonComponent = choiceBtnGO.GetComponent<Button>();

            if (buttonText != null)
            {
                buttonText.text = choices[i].choiceText;
            }
            if (buttonComponent != null)
            {
                int choiceIndex = i; // Closure for event listener
                buttonComponent.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
            }
            else
            {
                Debug.LogWarning("DialogueUI: Choice button prefab missing Button component!");
            }
        }
    }

    private void OnContinueButtonClicked()
    {
        _dialogueSystem.ContinueDialogue();
    }

    private void OnChoiceButtonClicked(int choiceIndex)
    {
        _dialogueSystem.MakeChoice(choiceIndex);
    }

    private void HideDialogueUI()
    {
        _dialoguePanel.SetActive(false);
        // Clear any remaining text
        _speakerNameText.text = "";
        _dialogueText.text = "";
        // Clear choice buttons
        foreach (GameObject btn in _activeChoiceButtons)
        {
            Destroy(btn);
        }
        _activeChoiceButtons.Clear();
        if (_choicesParent != null) _choicesParent.gameObject.SetActive(false); // Ensure choices panel is off
        Debug.Log("Dialogue UI: Hidden.");
    }
}
```

To set up the `DialogueUI`:

*   Create a Canvas in your scene (`GameObject` -> `UI` -> `Canvas`).
*   Inside the Canvas, create a Panel (`UI` -> `Panel`) and name it `DialoguePanel`.
*   Inside `DialoguePanel`, create two TextMeshPro Text objects (or regular UI Text): one for `SpeakerName` and one for `DialogueText`.
*   Create a Button (`UI` -> `Button`) and name it `ContinueButton`.
*   Create an empty GameObject inside `DialoguePanel` named `ChoicesParent`. This will hold your choice buttons.
*   Create another Button prefab for choices: `UI` -> `Button`. Drag it from your Hierarchy into your Project window to make it a prefab. You can delete it from the Hierarchy after creating the prefab.
*   Attach the `DialogueUI.cs` script to your `DialoguePanel` GameObject.
*   Drag and drop the created UI elements and the `DialogueSystem` GameObject into the corresponding fields in the `DialoguePanel`'s Inspector. Drag your `ChoiceButton` prefab into the `Choice Button Prefab` slot.

---

This comprehensive setup provides a robust and extensible foundation for a dialogue system in Unity using the Node System pattern. You can expand upon this by adding more node types (e.g., action nodes, quest nodes, item-giving nodes), visual editor tools, and more sophisticated UI animations.