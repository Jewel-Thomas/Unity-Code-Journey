// Unity Design Pattern Example: AdvancedDialogueSystem
// This script demonstrates the AdvancedDialogueSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates an **Advanced Dialogue System** in Unity using C#. It leverages several design patterns:

1.  **ScriptableObject Pattern:** For defining dialogue data (nodes, conditions, actions) as assets, making them highly reusable, configurable, and easy to manage in the Unity editor without needing to create new MonoBehaviours for each dialogue piece.
2.  **Strategy Pattern:** For `IDialogueCondition` and `IDialogueAction` interfaces. This allows defining various types of conditions (e.g., "player has item X", "quest Y is complete") and actions (e.g., "give item", "start quest") independently and plugging them into dialogue nodes without changing the node's core logic.
3.  **Command Pattern:** `IDialogueAction`s can be seen as commands that get executed when a choice is made or a node is entered/exited.
4.  **Observer Pattern:** The `DialogueManager` uses C# events (`OnDialogueStarted`, `OnDialogueNodeChanged`, `OnDialogueChoicesAvailable`, `OnDialogueEnded`) to notify any interested UI components or other game systems about dialogue state changes.
5.  **Singleton Pattern:** The `DialogueManager` is implemented as a singleton for easy global access from any script.

---

### **Project Setup & How to Use:**

1.  **Create a new Unity project** or open an existing one.
2.  **Create a new C# script** named `DialogueSystem.cs`.
3.  **Copy and paste all the code below** into `DialogueSystem.cs`.
4.  **Create a GameObject** in your scene, name it `DialogueManager`, and **attach the `DialogueManager` component** to it.
5.  **Start creating Dialogue Assets:**
    *   In your Unity project window, right-click -> `Create` -> `Dialogue System`.
    *   You'll see options for `Dialogue Graph`, `Dialogue Nodes`, `Conditions`, and `Actions`.
6.  **Example Asset Creation Steps:**
    *   **Create a `DialogueGraph`** (e.g., `MyFirstConversation`).
    *   **Create various `Dialogue Nodes`**:
        *   `BasicDialogueNode` (e.g., `IntroNode`, `ResponseNode`).
        *   `ChoiceDialogueNode` (e.g., `QuestionNode`).
        *   `EventDialogueNode` (e.g., `GiveItemNode`).
        *   `EndDialogueNode` (e.g., `EndConvoNode`).
    *   **Create `Conditions`**:
        *   `HasItemCondition` (e.g., `HasKeyCondition`). Set an item name.
    *   **Create `Actions`**:
        *   `GiveItemAction` (e.g., `GiveSwordAction`). Set an item name.
        *   `StartQuestAction` (e.g., `StartDragonQuest`). Set a quest name.
        *   `SetBooleanVariableAction` (e.g., `SetMetNPCFlag`). Set a variable name and value.

7.  **Connect Your Assets:**
    *   **Populate the `DialogueGraph`** (`MyFirstConversation`) by dragging your created `Dialogue Nodes` into its `Nodes` list.
    *   **Set the `Start Node ID`** of the `DialogueGraph`.
    *   **In your `Dialogue Nodes`:**
        *   For `BasicDialogueNode` and `EventDialogueNode`, set the `Next Node ID`.
        *   For `ChoiceDialogueNode`, add `Dialogue Choices`. For each choice, set its `Text`, `Target Node ID`, and optionally drag `Conditions` and `Actions` into their respective lists.
    *   **On your `DialogueManager` component** in the scene, assign your `MyFirstConversation` to the `Test Dialogue Graph` field.

8.  **Run the Scene:** The example `DialogueManager` script will automatically start the `Test Dialogue Graph` in `Start()`. Watch the console for output showing the dialogue progression, choices, conditions, and actions.

---

### **DialogueSystem.cs**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// --- Core Dialogue System Interfaces & Base Classes ---

namespace AdvancedDialogueSystem
{
    /// <summary>
    /// Represents the runtime state of the dialogue, including player inventory, quest status,
    /// and any custom variables that conditions and actions might interact with.
    /// This keeps the dialogue logic loosely coupled from specific game systems.
    /// </summary>
    [System.Serializable]
    public class DialogueState
    {
        // Example: Simple inventory using a list of strings
        public List<string> playerInventory = new List<string>();
        // Example: Quest status using a dictionary
        public Dictionary<string, bool> questStatus = new Dictionary<string, bool>(); // Key: Quest Name, Value: IsCompleted
        // Example: Generic boolean flags
        public Dictionary<string, bool> booleanVariables = new Dictionary<string, bool>();

        // You would extend this with references to your actual game's InventoryManager, QuestManager, PlayerStats etc.
        // For this example, we'll use simple in-memory collections.

        public void AddItem(string itemName)
        {
            if (!playerInventory.Contains(itemName))
            {
                playerInventory.Add(itemName);
                Debug.Log($"[DialogueState] Added item: {itemName}");
            }
        }

        public bool HasItem(string itemName)
        {
            return playerInventory.Contains(itemName);
        }

        public void SetQuestStatus(string questName, bool completed)
        {
            questStatus[questName] = completed;
            Debug.Log($"[DialogueState] Quest '{questName}' status set to: {completed}");
        }

        public bool IsQuestCompleted(string questName)
        {
            return questStatus.ContainsKey(questName) && questStatus[questName];
        }

        public void SetBooleanVariable(string varName, bool value)
        {
            booleanVariables[varName] = value;
            Debug.Log($"[DialogueState] Variable '{varName}' set to: {value}");
        }

        public bool GetBooleanVariable(string varName)
        {
            return booleanVariables.ContainsKey(varName) ? booleanVariables[varName] : false;
        }
    }

    /// <summary>
    /// Interface for dialogue conditions.
    /// Conditions determine if a dialogue choice or node is available/enabled.
    /// This uses the Strategy Pattern.
    /// </summary>
    public interface IDialogueCondition
    {
        /// <summary>
        /// Evaluates the condition based on the current dialogue state.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        /// <returns>True if the condition is met, false otherwise.</returns>
        bool Evaluate(DialogueState state);
    }

    /// <summary>
    /// Base ScriptableObject for dialogue conditions, allowing them to be created as assets.
    /// Inherit from this to create specific conditions (e.g., HasItemCondition).
    /// </summary>
    public abstract class DialogueConditionSO : ScriptableObject, IDialogueCondition
    {
        [TextArea(1, 3)]
        [Tooltip("A description of what this condition checks.")]
        public string Description = "Checks a condition.";
        public abstract bool Evaluate(DialogueState state);
    }

    /// <summary>
    /// Interface for dialogue actions.
    /// Actions are executed when a dialogue choice is made or a node is entered/exited.
    /// This uses the Command Pattern.
    /// </summary>
    public interface IDialogueAction
    {
        /// <summary>
        /// Executes the action, potentially modifying the dialogue state or game world.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        void Execute(DialogueState state);
    }

    /// <summary>
    /// Base ScriptableObject for dialogue actions, allowing them to be created as assets.
    /// Inherit from this to create specific actions (e.g., GiveItemAction).
    /// </summary>
    public abstract class DialogueActionSO : ScriptableObject, IDialogueAction
    {
        [TextArea(1, 3)]
        [Tooltip("A description of what this action does.")]
        public string Description = "Performs an action.";
        public abstract void Execute(DialogueState state);
    }

    /// <summary>
    /// Represents a single choice presented to the player in a ChoiceDialogueNode.
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        [Tooltip("The text displayed for this choice.")]
        public string ChoiceText;
        [Tooltip("The ID of the next node to transition to if this choice is selected.")]
        public string TargetNodeID;

        [Tooltip("Conditions that must be met for this choice to be available.")]
        public List<DialogueConditionSO> Conditions = new List<DialogueConditionSO>();
        [Tooltip("Actions to perform when this choice is selected.")]
        public List<DialogueActionSO> Actions = new List<DialogueActionSO>();

        /// <summary>
        /// Checks if this choice is currently available based on its conditions.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        /// <returns>True if all conditions are met, false otherwise.</returns>
        public bool IsAvailable(DialogueState state)
        {
            foreach (var condition in Conditions)
            {
                if (condition == null || !condition.Evaluate(state))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Executes all actions associated with this choice.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        public void ExecuteActions(DialogueState state)
        {
            foreach (var action in Actions)
            {
                if (action != null)
                {
                    action.Execute(state);
                }
            }
        }
    }

    /// <summary>
    /// Base class for all dialogue nodes.
    /// Each node represents a piece of the conversation flow.
    /// Uses ScriptableObject for asset creation.
    /// </summary>
    public abstract class DialogueNodeSO : ScriptableObject
    {
        [Tooltip("A unique identifier for this node within a graph. Used for transitions.")]
        public string NodeID;

        [TextArea(3, 10)]
        [Tooltip("The main dialogue text for this node. (Not all node types use this)")]
        public string DialogueText;

        [Tooltip("Actions to perform when this node is first entered.")]
        public List<DialogueActionSO> OnEnterActions = new List<DialogueActionSO>();
        [Tooltip("Actions to perform when this node is exited (or a choice is made to leave it).")]
        public List<DialogueActionSO> OnExitActions = new List<DialogueActionSO>();

        /// <summary>
        /// Executes all 'OnEnter' actions for this node.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        public void ExecuteOnEnterActions(DialogueState state)
        {
            foreach (var action in OnEnterActions)
            {
                if (action != null)
                {
                    action.Execute(state);
                }
            }
        }

        /// <summary>
        /// Executes all 'OnExit' actions for this node.
        /// </summary>
        /// <param name="state">The current DialogueState.</param>
        public void ExecuteOnExitActions(DialogueState state)
        {
            foreach (var action in OnExitActions)
            {
                if (action != null)
                {
                    action.Execute(state);
                }
            }
        }

        /// <summary>
        /// Returns the next node ID for linear progression (if applicable for the node type).
        /// </summary>
        public virtual string GetNextNodeID() => null;

        /// <summary>
        /// Returns a list of choices available from this node (if applicable for the node type).
        /// </summary>
        public virtual List<DialogueChoice> GetChoices() => null;
    }

    /// <summary>
    /// Represents an entire dialogue conversation graph.
    /// Contains a collection of nodes and defines the starting point.
    /// Uses ScriptableObject for asset creation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraphSO : ScriptableObject
    {
        [Tooltip("The ID of the node where this conversation begins.")]
        public string StartNodeID;
        [Tooltip("All nodes belonging to this dialogue graph.")]
        public List<DialogueNodeSO> Nodes = new List<DialogueNodeSO>();

        private Dictionary<string, DialogueNodeSO> _nodeDictionary;

        /// <summary>
        /// Initializes the internal dictionary for fast node lookups.
        /// </summary>
        public void Init()
        {
            _nodeDictionary = new Dictionary<string, DialogueNodeSO>();
            foreach (var node in Nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.NodeID))
                {
                    if (_nodeDictionary.ContainsKey(node.NodeID))
                    {
                        Debug.LogWarning($"[DialogueGraphSO] Duplicate NodeID found: {node.NodeID} in graph {name}. Only the first will be used.");
                        continue;
                    }
                    _nodeDictionary.Add(node.NodeID, node);
                }
            }
        }

        /// <summary>
        /// Retrieves a node by its ID.
        /// </summary>
        /// <param name="nodeID">The ID of the node to retrieve.</param>
        /// <returns>The DialogueNodeSO if found, otherwise null.</returns>
        public DialogueNodeSO GetNode(string nodeID)
        {
            if (_nodeDictionary == null || _nodeDictionary.Count == 0)
            {
                Init(); // Initialize if not already
            }
            _nodeDictionary.TryGetValue(nodeID, out DialogueNodeSO node);
            return node;
        }
    }
}

// --- Specific Dialogue Node Implementations ---

namespace AdvancedDialogueSystem.Nodes
{
    /// <summary>
    /// A simple dialogue node with text and a direct transition to the next node.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBasicDialogueNode", menuName = "Dialogue System/Dialogue Nodes/Basic Dialogue Node")]
    public class BasicDialogueNodeSO : DialogueNodeSO
    {
        [Tooltip("The ID of the next node to transition to automatically.")]
        public string NextNodeID;

        public override string GetNextNodeID() => NextNodeID;
    }

    /// <summary>
    /// A dialogue node that presents multiple choices to the player.
    /// </summary>
    [CreateAssetMenu(fileName = "NewChoiceDialogueNode", menuName = "Dialogue System/Dialogue Nodes/Choice Dialogue Node")]
    public class ChoiceDialogueNodeSO : DialogueNodeSO
    {
        [Tooltip("The list of choices presented to the player from this node.")]
        public List<DialogueChoice> Choices = new List<DialogueChoice>();

        public override List<DialogueChoice> GetChoices() => Choices;
    }

    /// <summary>
    /// A dialogue node that executes actions and then automatically proceeds to the next node
    /// without displaying any text or requiring player input. Useful for intermediate game logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEventDialogueNode", menuName = "Dialogue System/Dialogue Nodes/Event Dialogue Node")]
    public class EventDialogueNodeSO : DialogueNodeSO
    {
        [Tooltip("The ID of the next node to transition to automatically after executing actions.")]
        public string NextNodeID;

        public override string GetNextNodeID() => NextNodeID;
    }

    /// <summary>
    /// A special node type that signals the end of a conversation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEndDialogueNode", menuName = "Dialogue System/Dialogue Nodes/End Dialogue Node")]
    public class EndDialogueNodeSO : DialogueNodeSO
    {
        // No additional fields needed, its type signifies its purpose.
        public override string GetNextNodeID() => null; // Explicitly signals no next node for simple advance
    }
}

// --- Specific Dialogue Condition Implementations ---

namespace AdvancedDialogueSystem.Conditions
{
    /// <summary>
    /// Condition: Checks if the player's inventory contains a specific item.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHasItemCondition", menuName = "Dialogue System/Conditions/Has Item Condition")]
    public class HasItemConditionSO : DialogueConditionSO
    {
        [Tooltip("The name of the item to check for in the player's inventory.")]
        public string ItemName;

        public override bool Evaluate(DialogueState state)
        {
            bool hasItem = state.HasItem(ItemName);
            Debug.Log($"[Condition] Checking for item '{ItemName}': {hasItem}");
            return hasItem;
        }
    }

    /// <summary>
    /// Condition: Checks if a specific quest is marked as completed in the dialogue state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestStatusCondition", menuName = "Dialogue System/Conditions/Quest Status Condition")]
    public class QuestStatusConditionSO : DialogueConditionSO
    {
        [Tooltip("The name of the quest to check.")]
        public string QuestName;
        [Tooltip("Whether the quest should be completed or not completed for this condition to pass.")]
        public bool ShouldBeCompleted = true;

        public override bool Evaluate(DialogueState state)
        {
            bool isCompleted = state.IsQuestCompleted(QuestName);
            bool result = (isCompleted == ShouldBeCompleted);
            Debug.Log($"[Condition] Checking quest '{QuestName}' (should be completed: {ShouldBeCompleted}): {result}");
            return result;
        }
    }

    /// <summary>
    /// Condition: Checks a generic boolean variable in the dialogue state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBooleanVariableCondition", menuName = "Dialogue System/Conditions/Boolean Variable Condition")]
    public class BooleanVariableConditionSO : DialogueConditionSO
    {
        [Tooltip("The name of the boolean variable to check.")]
        public string VariableName;
        [Tooltip("The expected value of the boolean variable for this condition to pass.")]
        public bool ExpectedValue = true;

        public override bool Evaluate(DialogueState state)
        {
            bool actualValue = state.GetBooleanVariable(VariableName);
            bool result = (actualValue == ExpectedValue);
            Debug.Log($"[Condition] Checking boolean variable '{VariableName}' (expected: {ExpectedValue}, actual: {actualValue}): {result}");
            return result;
        }
    }
}

// --- Specific Dialogue Action Implementations ---

namespace AdvancedDialogueSystem.Actions
{
    /// <summary>
    /// Action: Adds a specific item to the player's inventory in the dialogue state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGiveItemAction", menuName = "Dialogue System/Actions/Give Item Action")]
    public class GiveItemActionSO : DialogueActionSO
    {
        [Tooltip("The name of the item to give to the player.")]
        public string ItemName;

        public override void Execute(DialogueState state)
        {
            state.AddItem(ItemName);
            Debug.Log($"[Action] Player received item: {ItemName}");
        }
    }

    /// <summary>
    /// Action: Sets the completion status of a quest in the dialogue state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSetQuestStatus", menuName = "Dialogue System/Actions/Set Quest Status Action")]
    public class SetQuestStatusActionSO : DialogueActionSO
    {
        [Tooltip("The name of the quest to modify.")]
        public string QuestName;
        [Tooltip("The new completion status for the quest.")]
        public bool CompletedStatus = true;

        public override void Execute(DialogueState state)
        {
            state.SetQuestStatus(QuestName, CompletedStatus);
            Debug.Log($"[Action] Quest '{QuestName}' status set to: {CompletedStatus}");
        }
    }

    /// <summary>
    /// Action: Sets a generic boolean variable in the dialogue state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSetBooleanVariable", menuName = "Dialogue System/Actions/Set Boolean Variable Action")]
    public class SetBooleanVariableActionSO : DialogueActionSO
    {
        [Tooltip("The name of the boolean variable to set.")]
        public string VariableName;
        [Tooltip("The value to set the boolean variable to.")]
        public bool Value = true;

        public override void Execute(DialogueState state)
        {
            state.SetBooleanVariable(VariableName, Value);
            Debug.Log($"[Action] Boolean variable '{VariableName}' set to: {Value}");
        }
    }

    /// <summary>
    /// Action: Logs a custom message to the console. Useful for debugging or triggering generic events.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLogMessageAction", menuName = "Dialogue System/Actions/Log Message Action")]
    public class LogMessageActionSO : DialogueActionSO
    {
        [TextArea(1, 5)]
        [Tooltip("The message to log to the Unity console.")]
        public string Message;

        public override void Execute(DialogueState state)
        {
            Debug.Log($"[Action] Log Message: {Message}");
        }
    }
}

// --- Dialogue Manager (MonoBehaviour) ---

namespace AdvancedDialogueSystem
{
    using AdvancedDialogueSystem.Nodes;

    /// <summary>
    /// The central manager for the dialogue system.
    /// Manages the current conversation, handles node transitions, and notifies UI components.
    /// Implemented as a Singleton for easy access.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // --- Singleton Instance ---
        public static DialogueManager Instance { get; private set; }

        // --- Public Events (Observer Pattern) ---
        public static event Action OnDialogueStarted;
        public static event Action<string, string> OnDialogueNodeChanged; // NodeID, DialogueText
        public static event Action<List<DialogueChoice>> OnDialogueChoicesAvailable;
        public static event Action OnDialogueEnded;

        // --- Internal State ---
        [Tooltip("The current dialogue graph being played.")]
        [SerializeField] private DialogueGraphSO _currentDialogueGraph;
        private DialogueNodeSO _currentNode;
        private DialogueState _dialogueState = new DialogueState(); // Represents game state for conditions/actions

        [Header("Testing")]
        [Tooltip("Assign a Dialogue Graph here to automatically start it on Awake for testing.")]
        public DialogueGraphSO TestDialogueGraph;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep manager alive across scenes

            _dialogueState = new DialogueState(); // Initialize a fresh state for each manager instance.
        }

        private void Start()
        {
            if (TestDialogueGraph != null)
            {
                StartDialogue(TestDialogueGraph);
            }
        }

        /// <summary>
        /// Starts a new dialogue conversation using the specified graph.
        /// </summary>
        /// <param name="graph">The DialogueGraphSO to start.</param>
        public void StartDialogue(DialogueGraphSO graph)
        {
            if (graph == null)
            {
                Debug.LogError("[DialogueManager] Cannot start dialogue: Graph is null.");
                return;
            }

            _currentDialogueGraph = graph;
            _currentDialogueGraph.Init(); // Ensure graph is initialized
            _dialogueState = new DialogueState(); // Reset state for a new conversation

            OnDialogueStarted?.Invoke();
            Debug.Log($"[DialogueManager] Dialogue Started: {graph.name}");

            MoveToNode(_currentDialogueGraph.StartNodeID);
        }

        /// <summary>
        /// Moves the dialogue to a specific node by its ID.
        /// Handles executing OnExit actions of the previous node and OnEnter actions of the new node.
        /// </summary>
        /// <param name="nodeID">The ID of the target node.</param>
        private void MoveToNode(string nodeID)
        {
            if (_currentNode != null)
            {
                _currentNode.ExecuteOnExitActions(_dialogueState);
            }

            _currentNode = _currentDialogueGraph.GetNode(nodeID);

            if (_currentNode == null)
            {
                Debug.LogError($"[DialogueManager] Node with ID '{nodeID}' not found in graph '{_currentDialogueGraph.name}'. Ending dialogue.");
                EndDialogue();
                return;
            }

            _currentNode.ExecuteOnEnterActions(_dialogueState);
            Debug.Log($"[DialogueManager] Entering Node: {_currentNode.NodeID}");

            // Handle different node types
            if (_currentNode is EndDialogueNodeSO)
            {
                OnDialogueNodeChanged?.Invoke(_currentNode.NodeID, _currentNode.DialogueText); // Notify UI one last time
                EndDialogue();
            }
            else if (_currentNode is ChoiceDialogueNodeSO choiceNode)
            {
                // Filter choices based on conditions
                List<DialogueChoice> availableChoices = choiceNode.GetChoices()
                                                                 .Where(choice => choice != null && choice.IsAvailable(_dialogueState))
                                                                 .ToList();
                OnDialogueNodeChanged?.Invoke(choiceNode.NodeID, choiceNode.DialogueText);
                OnDialogueChoicesAvailable?.Invoke(availableChoices);
                Debug.Log($"[DialogueManager] Choice Node. Text: '{choiceNode.DialogueText}'. Available Choices: {availableChoices.Count}");
                foreach(var choice in availableChoices)
                {
                    Debug.Log($"- Choice: '{choice.ChoiceText}' (Target: {choice.TargetNodeID})");
                }
                if (availableChoices.Count == 0)
                {
                    Debug.LogWarning($"[DialogueManager] Choice node '{choiceNode.NodeID}' has no available choices. Ending dialogue.");
                    EndDialogue();
                }
            }
            else if (_currentNode is BasicDialogueNodeSO basicNode)
            {
                OnDialogueNodeChanged?.Invoke(basicNode.NodeID, basicNode.DialogueText);
                OnDialogueChoicesAvailable?.Invoke(null); // No choices
                Debug.Log($"[DialogueManager] Basic Node. Text: '{basicNode.DialogueText}'. Press 'Advance' to continue.");
            }
            else if (_currentNode is EventDialogueNodeSO eventNode)
            {
                // Event nodes execute actions and immediately advance
                OnDialogueNodeChanged?.Invoke(eventNode.NodeID, eventNode.DialogueText); // Still notify, even if text is empty
                OnDialogueChoicesAvailable?.Invoke(null); // No choices
                Debug.Log($"[DialogueManager] Event Node. Automatically advancing. Text: '{eventNode.DialogueText}'");
                // Auto-advance
                AdvanceDialogue();
            }
        }

        /// <summary>
        /// Advances the dialogue to the next node in a linear progression (for BasicDialogueNode, EventDialogueNode).
        /// This should only be called if the current node is not a ChoiceDialogueNode.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (_currentNode == null)
            {
                Debug.LogWarning("[DialogueManager] Cannot advance dialogue: No current node. Dialogue may have ended.");
                return;
            }

            if (_currentNode is ChoiceDialogueNodeSO)
            {
                Debug.LogWarning($"[DialogueManager] Cannot advance a ChoiceDialogueNode directly. Use MakeChoice() instead. Current Node: {_currentNode.NodeID}");
                return;
            }

            string nextNodeID = _currentNode.GetNextNodeID();

            if (string.IsNullOrEmpty(nextNodeID))
            {
                EndDialogue();
            }
            else
            {
                MoveToNode(nextNodeID);
            }
        }

        /// <summary>
        /// Makes a choice in a ChoiceDialogueNode and advances the dialogue.
        /// </summary>
        /// <param name="choiceIndex">The index of the chosen option from the currently available choices.</param>
        public void MakeChoice(int choiceIndex)
        {
            if (!(_currentNode is ChoiceDialogueNodeSO choiceNode))
            {
                Debug.LogWarning("[DialogueManager] Cannot make a choice: Current node is not a ChoiceDialogueNode.");
                return;
            }

            // Filter choices again to ensure we only consider currently available ones
            List<DialogueChoice> availableChoices = choiceNode.GetChoices()
                                                             .Where(choice => choice != null && choice.IsAvailable(_dialogueState))
                                                             .ToList();

            if (choiceIndex < 0 || choiceIndex >= availableChoices.Count)
            {
                Debug.LogError($"[DialogueManager] Invalid choice index {choiceIndex}. Only {availableChoices.Count} choices available.");
                return;
            }

            DialogueChoice selectedChoice = availableChoices[choiceIndex];

            // Execute actions associated with the chosen option
            selectedChoice.ExecuteActions(_dialogueState);
            Debug.Log($"[DialogueManager] Made choice: '{selectedChoice.ChoiceText}'. Executing actions and moving to '{selectedChoice.TargetNodeID}'");

            MoveToNode(selectedChoice.TargetNodeID);
        }

        /// <summary>
        /// Ends the current dialogue conversation.
        /// </summary>
        public void EndDialogue()
        {
            if (_currentNode != null)
            {
                _currentNode.ExecuteOnExitActions(_dialogueState);
            }
            _currentNode = null;
            _currentDialogueGraph = null;
            OnDialogueEnded?.Invoke();
            Debug.Log("[DialogueManager] Dialogue Ended.");
        }

        // --- Example UI Integration (Placeholder - you would build actual UI) ---

        // You would typically have a DialogueUI MonoBehaviour that subscribes to
        // DialogueManager's events and updates TextMeshPro fields and buttons.

        // Example: Player input to advance dialogue or make choices
        void Update()
        {
            if (_currentDialogueGraph == null) return; // No dialogue active

            if (_currentNode is BasicDialogueNodeSO || _currentNode is EventDialogueNodeSO)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    AdvanceDialogue();
                }
            }
            // For choice nodes, you'd bind UI buttons to MakeChoice(index)
            // Example for choices (simulated with keys 1, 2, 3):
            if (_currentNode is ChoiceDialogueNodeSO)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) MakeChoice(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) MakeChoice(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) MakeChoice(2);
            }
        }
    }
}
```