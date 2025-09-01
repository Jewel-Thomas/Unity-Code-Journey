// Unity Design Pattern Example: DialogueSystemPattern
// This script demonstrates the DialogueSystemPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a practical and extensible Dialogue System pattern in Unity. It combines several design patterns:

1.  **Scriptable Object Pattern:** For defining dialogue data (lines, choices, events) in a data-driven way, allowing designers to create dialogue flows without touching code.
2.  **State Pattern (Implicit):** The `DialogueSystemManager` manages the system's state (Idle, DisplayingLine, WaitingForChoice, ProcessingEvent) to control flow.
3.  **Observer/Event Pattern:** The `DialogueSystemManager` acts as a subject, raising events (`OnDialogueLineRequested`, `OnDialogueChoicesRequested`, etc.) that `DialogueUI` and other game systems (for `OnDialogueEventTriggered`) can subscribe to. This decouples the core dialogue logic from its presentation and side effects.
4.  **Chain of Responsibility/Graph Traversal:** Dialogue nodes (via their `OnEnter` method) define how they process themselves and what action the manager should take, effectively creating a graph structure.

This pattern makes the dialogue system modular, flexible, and easy to extend with new types of dialogue nodes or UI presentations.

---

To use this example:

1.  **Create Folders:** In your Unity project, create a `Scripts/DialogueSystem/` folder, and subfolders like `Core`, `UI`, `Nodes`, `Test`.
2.  **Create C# Scripts:** Create the C# files listed below in their respective folders.
3.  **Setup Scene:**
    *   Create an empty GameObject named `DialogueSystem` and attach the `DialogueSystemManager.cs` script to it.
    *   Create a Canvas (UI -> Canvas).
    *   Inside the Canvas, create a Panel (UI -> Panel) named `DialoguePanel`. Make sure it's initially inactive.
    *   Inside `DialoguePanel`, add:
        *   A Text object (TextMeshPro) named `CharacterNameText` for the character's name.
        *   A Text object (TextMeshPro) named `DialogueText` for the dialogue line.
        *   An Image or Button (UI -> Button) named `AdvanceButton` to advance dialogue.
        *   An empty GameObject named `ChoiceButtonsContainer`.
        *   A Button (UI -> Button) named `ChoiceButton_Template` inside `ChoiceButtonsContainer`. Make this template inactive. This will be cloned for choices.
    *   Attach the `DialogueUI.cs` script to the `DialoguePanel` and drag-and-drop the UI elements into its inspector fields.
    *   Create an empty GameObject named `TestDialogueTrigger` and attach the `TestDialogueTrigger.cs` script to it.
4.  **Create Dialogue Data:**
    *   Right-click in your Project window -> Create -> Dialogue System.
    *   Create a `Dialogue Graph` asset (e.g., "MyFirstDialogue").
    *   Create several `Dialogue Line Node` assets (e.g., "IntroLine1", "IntroLine2", "ExitLine").
    *   Create a `Dialogue Choice Node` asset (e.g., "ChoosePath").
    *   Create a `Dialogue Event Node` asset (e.g., "GiveItemEvent").
5.  **Link Dialogue Data:**
    *   **In "MyFirstDialogue" graph:** Set its `Start Node` to "IntroLine1".
    *   **In "IntroLine1":** Set its `Next Nodes` (size 1) to "IntroLine2".
    *   **In "IntroLine2":** Set its `Next Nodes` (size 1) to "ChoosePath".
    *   **In "ChoosePath":**
        *   Set its `Question Text` (e.g., "What do you do next?").
        *   Add two `Choices`:
            *   Choice 1: "Take the sword" -> `Next Node` to "GiveItemEvent".
            *   Choice 2: "Leave" -> `Next Node` to "ExitLine".
    *   **In "GiveItemEvent":**
        *   Set `Event Key` to "GIVE_ITEM_SWORD".
        *   Set its `Next Nodes` (size 1) to "ExitLine".
    *   **In "ExitLine":** No `Next Nodes` needed if it's the end.
6.  **Hook up Test Trigger:**
    *   Select the `TestDialogueTrigger` GameObject.
    *   Drag "MyFirstDialogue" (the Dialogue Graph asset) into its `Dialogue To Start` field.
7.  **Run:** Play the scene. Press spacebar to start the dialogue.

---

### 1. Core Dialogue System Manager

This is the central orchestrator of the dialogue flow. It manages state and dispatches events.

**File: `Scripts/DialogueSystem/Core/DialogueSystemManager.cs`**
```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events; // For UnityEvent<string>

namespace DialogueSystem
{
    /// <summary>
    /// The core Dialogue System Manager.
    /// This component orchestrates the entire dialogue flow, managing state and dispatching events.
    /// It acts as the 'Subject' in the Observer pattern for dialogue events.
    /// </summary>
    public class DialogueSystemManager : MonoBehaviour
    {
        // --- Singleton Pattern (for easy access in an example) ---
        // In a larger project, consider a more robust service locator or dependency injection.
        public static DialogueSystemManager Instance { get; private set; }

        // --- Dialogue State Machine ---
        // This enum defines the possible states of the dialogue system.
        // It helps manage the flow and prevent invalid operations.
        private enum DialogueState { Idle, DisplayingLine, WaitingForChoice, ProcessingEvent }
        private DialogueState _currentState = DialogueState.Idle;

        // --- Current Dialogue Data ---
        private DialogueNode _currentNode; // The dialogue node currently being processed.
        private DialogueNode _nextNodeAfterAdvance; // Stores the next node for linear progression or after an event.

        // --- Events for UI and Game Logic to Subscribe To (Observer Pattern) ---
        // These events are central to decoupling the dialogue logic from its presentation (UI)
        // and its effects on game state.

        /// <summary>
        /// Raised when a dialogue sequence officially begins.
        /// Subscribers (e.g., DialogueUI) can prepare to display dialogue.
        /// </summary>
        public event Action OnDialogueStarted;

        /// <summary>
        /// Raised when a dialogue sequence officially ends.
        /// Subscribers (e.g., DialogueUI) can hide dialogue elements.
        /// </summary>
        public event Action OnDialogueEnded;

        /// <summary>
        /// Raised when a new dialogue line needs to be displayed.
        /// Parameters: characterName, dialogueText.
        /// Subscribers (e.g., DialogueUI) will update their text fields.
        /// </summary>
        public event Action<string, string> OnDialogueLineRequested;

        /// <summary>
        /// Raised when dialogue choices need to be presented to the player.
        /// Parameters: questionText, list of ChoiceOption structs.
        /// Subscribers (e.g., DialogueUI) will create and display choice buttons.
        /// </summary>
        public event Action<string, List<ChoiceOption>> OnDialogueChoicesRequested;

        /// <summary>
        /// Raised when a special game event needs to be triggered by dialogue.
        /// Uses UnityEvent for easy inspector hookup by designers.
        /// Parameter: eventKey (a string identifying the event, e.g., "GIVE_ITEM_SWORD").
        /// Subscribers (e.g., a GameEventManager) will react to this key.
        /// </summary>
        public UnityEvent<string> OnDialogueEventTriggered;


        // --- MonoBehaviour Lifecycle Methods ---

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("DialogueSystemManager: Multiple instances found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep manager across scenes
        }

        // --- Public Methods for Starting and Interacting with Dialogue ---

        /// <summary>
        /// Initiates a dialogue sequence with the given DialogueGraph.
        /// </summary>
        /// <param name="graph">The starting DialogueGraph asset.</param>
        public void StartDialogue(DialogueGraph graph)
        {
            if (_currentState != DialogueState.Idle)
            {
                Debug.LogWarning("DialogueSystemManager: Cannot start new dialogue while one is already active.");
                return;
            }

            if (graph == null || graph.startNode == null)
            {
                Debug.LogError("DialogueSystemManager: Attempted to start dialogue with a null graph or start node.");
                EndDialogue();
                return;
            }

            _currentNode = graph.startNode;
            OnDialogueStarted?.Invoke(); // Notify subscribers that dialogue has started
            _currentNode.OnEnter(this); // Let the starting node define its initial behavior
        }

        /// <summary>
        /// Advances the dialogue to the next line or node.
        /// Typically called by the UI when the player presses an 'advance' button/key,
        /// or automatically after an event.
        /// </summary>
        public void AdvanceDialogue()
        {
            // Only allow advancement from 'DisplayingLine' or 'ProcessingEvent' states.
            if (_currentState != DialogueState.DisplayingLine && _currentState != DialogueState.ProcessingEvent)
            {
                Debug.LogWarning($"DialogueSystemManager: Cannot advance dialogue in current state: {_currentState}");
                return;
            }

            // If there's a designated next node, move to it.
            if (_nextNodeAfterAdvance != null)
            {
                _currentNode = _nextNodeAfterAdvance;
                _nextNodeAfterAdvance = null; // Clear the pending next node
                _currentNode.OnEnter(this); // Process the new current node
            }
            else
            {
                EndDialogue(); // No more nodes to advance to, end the dialogue.
            }
        }

        /// <summary>
        /// Called when the player makes a choice from a presented set of options.
        /// </summary>
        /// <param name="choiceIndex">The index of the chosen option.</param>
        public void MakeChoice(int choiceIndex)
        {
            // Only allow choices when in the 'WaitingForChoice' state.
            if (_currentState != DialogueState.WaitingForChoice)
            {
                Debug.LogWarning($"DialogueSystemManager: Cannot make choice in current state: {_currentState}");
                return;
            }

            // Ensure the current node is a DialogueChoiceNode and the index is valid.
            if (_currentNode is DialogueChoiceNode choiceNode && choiceIndex >= 0 && choiceIndex < choiceNode.Choices.Count)
            {
                // Set the current node to the node linked to the chosen option.
                _currentNode = choiceNode.Choices[choiceIndex].nextNode;
                if (_currentNode != null)
                {
                    _currentNode.OnEnter(this); // Process the newly chosen node.
                }
                else
                {
                    Debug.LogWarning($"DialogueSystemManager: Choice {choiceIndex} leads to a null node. Ending dialogue.");
                    EndDialogue();
                }
            }
            else
            {
                Debug.LogError($"DialogueSystemManager: Invalid choice index {choiceIndex} or current node is not a ChoiceNode. Ending dialogue.");
                EndDialogue();
            }
        }

        /// <summary>
        /// Ends the current dialogue sequence.
        /// Resets the state and notifies subscribers.
        /// </summary>
        public void EndDialogue()
        {
            _currentState = DialogueState.Idle;
            _currentNode = null;
            _nextNodeAfterAdvance = null;
            OnDialogueEnded?.Invoke(); // Notify subscribers that dialogue has ended
        }

        // --- Callbacks for Dialogue Nodes to Use (Node-to-Manager Communication) ---
        // These methods are called by the individual DialogueNode Scriptable Objects
        // to tell the manager how to proceed.

        /// <summary>
        /// Called by a DialogueLineNode to request the manager to display a line of dialogue.
        /// </summary>
        /// <param name="character">The name of the character speaking.</param>
        /// <param name="text">The dialogue text.</param>
        /// <param name="nextNode">The node to advance to when the player progresses this line.</param>
        public void DisplayLine(string character, string text, DialogueNode nextNode)
        {
            _currentState = DialogueState.DisplayingLine;
            OnDialogueLineRequested?.Invoke(character, text); // Notify UI
            _nextNodeAfterAdvance = nextNode; // Store the next node for advancement
        }

        /// <summary>
        /// Called by a DialogueChoiceNode to request the manager to display choices.
        /// </summary>
        /// <param name="question">The question prompt for the choices.</param>
        /// <param name="choices">A list of ChoiceOption structs.</param>
        public void DisplayChoices(string question, List<ChoiceOption> choices)
        {
            _currentState = DialogueState.WaitingForChoice;
            OnDialogueChoicesRequested?.Invoke(question, choices); // Notify UI
            // _nextNodeAfterAdvance is NOT set here, as the player's choice determines the next node.
        }

        /// <summary>
        /// Called by a DialogueEventNode to request the manager to trigger a game event.
        /// </summary>
        /// <param name="eventKey">A string identifier for the event.</param>
        /// <param name="nextNode">The node to advance to after the event is triggered.</param>
        public void TriggerEvent(string eventKey, DialogueNode nextNode)
        {
            _currentState = DialogueState.ProcessingEvent;
            OnDialogueEventTriggered?.Invoke(eventKey); // Notify game systems
            _nextNodeAfterAdvance = nextNode; // Store the next node for advancement

            // For simplicity in this example, we automatically advance immediately after the event.
            // In a real game, you might wait for an external system to signal event completion.
            AdvanceDialogue();
        }

        /// <summary>
        /// Gets the current state of the dialogue system.
        /// </summary>
        public DialogueState GetCurrentState()
        {
            return _currentState;
        }
    }
}
```

### 2. Dialogue User Interface

This component subscribes to manager events and updates the Unity UI.

**File: `Scripts/DialogueSystem/UI/DialogueUI.cs`**
```csharp
using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace DialogueSystem
{
    /// <summary>
    /// Handles the display of dialogue on the Unity UI.
    /// It subscribes to events from the DialogueSystemManager to update its elements.
    /// This component represents the 'Observer' in the Observer pattern.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private GameObject _choiceButtonsContainer;
        [SerializeField] private Button _choiceButtonTemplate; // Template to instantiate for choices

        [Header("Settings")]
        [SerializeField] private float _textDisplaySpeed = 0.05f; // Seconds per character for typewriter effect

        private Coroutine _typewriterCoroutine;
        private List<Button> _currentChoiceButtons = new List<Button>();

        private void Awake()
        {
            if (_dialoguePanel == null || _characterNameText == null || _dialogueText == null ||
                _advanceButton == null || _choiceButtonsContainer == null || _choiceButtonTemplate == null)
            {
                Debug.LogError("DialogueUI: One or more UI elements are not assigned. Please check inspector.");
                enabled = false; // Disable component if essential elements are missing
                return;
            }

            _dialoguePanel.SetActive(false); // Start with dialogue panel hidden
            _choiceButtonTemplate.gameObject.SetActive(false); // Hide template button
        }

        private void OnEnable()
        {
            // Subscribe to events from the DialogueSystemManager.
            if (DialogueSystemManager.Instance != null)
            {
                DialogueSystemManager.Instance.OnDialogueStarted += ShowDialoguePanel;
                DialogueSystemManager.Instance.OnDialogueEnded += HideDialoguePanel;
                DialogueSystemManager.Instance.OnDialogueLineRequested += DisplayDialogueLine;
                DialogueSystemManager.Instance.OnDialogueChoicesRequested += DisplayChoices;
            }

            // Hook up the advance button click event.
            _advanceButton.onClick.AddListener(OnAdvanceButtonClicked);
        }

        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks.
            if (DialogueSystemManager.Instance != null)
            {
                DialogueSystemManager.Instance.OnDialogueStarted -= ShowDialoguePanel;
                DialogueSystemManager.Instance.OnDialogueEnded -= HideDialoguePanel;
                DialogueSystemManager.Instance.OnDialogueLineRequested -= DisplayDialogueLine;
                DialogueSystemManager.Instance.OnDialogueChoicesRequested -= DisplayChoices;
            }

            _advanceButton.onClick.RemoveListener(OnAdvanceButtonClicked);
        }

        // --- Event Handlers from DialogueSystemManager ---

        private void ShowDialoguePanel()
        {
            _dialoguePanel.SetActive(true);
            _advanceButton.gameObject.SetActive(true);
            ClearChoices();
        }

        private void HideDialoguePanel()
        {
            _dialoguePanel.SetActive(false);
            _characterNameText.text = "";
            _dialogueText.text = "";
            ClearChoices();
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
        }

        private void DisplayDialogueLine(string characterName, string dialogueText)
        {
            ClearChoices(); // Ensure choices are hidden when a line is displayed
            _characterNameText.text = characterName;
            _dialogueText.text = ""; // Clear for typewriter effect
            _advanceButton.gameObject.SetActive(true);

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(dialogueText));
        }

        private void DisplayChoices(string question, List<ChoiceOption> choices)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _characterNameText.text = ""; // Clear character name for question
            _dialogueText.text = question; // Display question as dialogue text
            _advanceButton.gameObject.SetActive(false); // Disable advance button for choices

            ClearChoices(); // Clear any previous choices

            // Instantiate buttons for each choice
            for (int i = 0; i < choices.Count; i++)
            {
                ChoiceOption choice = choices[i];
                Button choiceButton = Instantiate(_choiceButtonTemplate, _choiceButtonsContainer.transform);
                choiceButton.gameObject.SetActive(true);
                choiceButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

                int choiceIndex = i; // Capture index for the lambda
                choiceButton.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
                _currentChoiceButtons.Add(choiceButton);
            }
        }

        // --- UI Interaction Handlers ---

        private void OnAdvanceButtonClicked()
        {
            // If typewriter effect is running, complete it instantly.
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
                _dialogueText.maxVisibleCharacters = int.MaxValue; // Show all text instantly
                return; // Don't advance immediately, let the player click again
            }

            DialogueSystemManager.Instance.AdvanceDialogue();
        }

        private void OnChoiceButtonClicked(int choiceIndex)
        {
            DialogueSystemManager.Instance.MakeChoice(choiceIndex);
            ClearChoices(); // Clear choice buttons after a choice is made
        }

        // --- Helper Methods ---

        private IEnumerator TypewriterEffect(string text)
        {
            _dialogueText.maxVisibleCharacters = 0; // Start with no characters visible
            int totalVisibleCharacters = text.Length;
            int currentVisibleCharacters = 0;

            while (currentVisibleCharacters < totalVisibleCharacters)
            {
                currentVisibleCharacters++;
                _dialogueText.maxVisibleCharacters = currentVisibleCharacters;
                yield return new WaitForSeconds(_textDisplaySpeed);
            }
            _typewriterCoroutine = null;
        }

        private void ClearChoices()
        {
            foreach (Button button in _currentChoiceButtons)
            {
                Destroy(button.gameObject);
            }
            _currentChoiceButtons.Clear();
        }
    }
}
```

### 3. Dialogue Node Base Class (Scriptable Object)

This abstract class defines the common interface for all dialogue nodes.

**File: `Scripts/DialogueSystem/Nodes/DialogueNode.cs`**
```csharp
using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// Base abstract class for all dialogue nodes.
    /// This is a ScriptableObject, allowing dialogue content and flow to be defined as data assets.
    /// It's a key part of the data-driven approach for the DialogueSystemPattern.
    /// </summary>
    public abstract class DialogueNode : ScriptableObject
    {
        [Tooltip("Internal name for editor organization.")]
        public string nodeName = "New Dialogue Node";

        [Tooltip("The list of next nodes. Used for linear progression or graph connections. " +
                 "Specific node types (e.g., ChoiceNode) will interpret this differently.")]
        public List<DialogueNode> nextNodes = new List<DialogueNode>();

        // Optional: for visual graph editors
        [HideInInspector] public Vector2 editorPosition;

        /// <summary>
        /// Abstract method that the DialogueSystemManager calls when this node becomes the current node.
        /// Each concrete DialogueNode type implements this to define its specific behavior
        /// (e.g., display a line, show choices, trigger an event).
        /// </summary>
        /// <param name="manager">Reference to the DialogueSystemManager to trigger its display/event methods.</param>
        public abstract void OnEnter(DialogueSystemManager manager);
    }
}
```

### 4. Dialogue Line Node (Scriptable Object)

Represents a single line of dialogue.

**File: `Scripts/DialogueSystem/Nodes/DialogueLineNode.cs`**
```csharp
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A concrete DialogueNode representing a single line of dialogue.
    /// When this node is entered, it tells the DialogueSystemManager to display a character's line.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Line Node", menuName = "Dialogue System/Dialogue Line Node")]
    public class DialogueLineNode : DialogueNode
    {
        [Tooltip("The name of the character speaking this line.")]
        public string characterName;
        [Tooltip("The actual text of the dialogue line.")]
        [TextArea(3, 10)]
        public string dialogueText;

        /// <summary>
        /// Called when the DialogueSystemManager enters this node.
        /// It requests the manager to display this dialogue line and specifies the next node in sequence.
        /// </summary>
        /// <param name="manager">The DialogueSystemManager instance.</param>
        public override void OnEnter(DialogueSystemManager manager)
        {
            DialogueNode next = (nextNodes != null && nextNodes.Count > 0) ? nextNodes[0] : null;
            manager.DisplayLine(characterName, dialogueText, next);
        }
    }
}
```

### 5. Dialogue Choice Node (Scriptable Object)

Represents a point where the player makes a choice.

**File: `Scripts/DialogueSystem/Nodes/DialogueChoiceNode.cs`**
```csharp
using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// A struct representing a single choice option in a DialogueChoiceNode.
    /// It links the choice text to the next DialogueNode in the graph.
    /// </summary>
    [System.Serializable]
    public struct ChoiceOption
    {
        [Tooltip("The text displayed for this choice button.")]
        public string choiceText;
        [Tooltip("The DialogueNode to transition to if this choice is selected.")]
        public DialogueNode nextNode;
    }

    /// <summary>
    /// A concrete DialogueNode that presents the player with multiple choices.
    /// When this node is entered, it tells the DialogueSystemManager to display choice buttons.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Choice Node", menuName = "Dialogue System/Dialogue Choice Node")]
    public class DialogueChoiceNode : DialogueNode
    {
        [Tooltip("The question or prompt displayed before the choices.")]
        [TextArea(1, 3)]
        public string questionText;
        [Tooltip("The list of available choices, each linking to a subsequent dialogue node.")]
        public List<ChoiceOption> Choices = new List<ChoiceOption>(); // Changed from 'choices' to 'Choices' for consistency

        /// <summary>
        /// Called when the DialogueSystemManager enters this node.
        /// It requests the manager to display the choices to the player.
        /// </summary>
        /// <param name="manager">The DialogueSystemManager instance.</param>
        public override void OnEnter(DialogueSystemManager manager)
        {
            // Clear any default nextNodes, as choice nodes handle their own connections explicitly.
            nextNodes.Clear(); 
            manager.DisplayChoices(questionText, Choices);
        }
    }
}
```

### 6. Dialogue Event Node (Scriptable Object)

Triggers a game event from within the dialogue.

**File: `Scripts/DialogueSystem/Nodes/DialogueEventNode.cs`**
```csharp
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A concrete DialogueNode that triggers a specific game event.
    /// When this node is entered, it tells the DialogueSystemManager to dispatch an event.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Event Node", menuName = "Dialogue System/Dialogue Event Node")]
    public class DialogueEventNode : DialogueNode
    {
        [Tooltip("A unique key or identifier for the event to be triggered (e.g., 'GIVE_ITEM_SWORD', 'START_QUEST_01').")]
        public string eventKey;

        /// <summary>
        /// Called when the DialogueSystemManager enters this node.
        /// It requests the manager to trigger a game event and specifies the next node in sequence.
        /// </summary>
        /// <param name="manager">The DialogueSystemManager instance.</param>
        public override void OnEnter(DialogueSystemManager manager)
        {
            DialogueNode next = (nextNodes != null && nextNodes.Count > 0) ? nextNodes[0] : null;
            manager.TriggerEvent(eventKey, next);
        }
    }
}
```

### 7. Dialogue Graph (Scriptable Object)

Acts as a container for a dialogue sequence, pointing to its starting node.

**File: `Scripts/DialogueSystem/Nodes/DialogueGraph.cs`**
```csharp
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// A ScriptableObject representing an entire dialogue sequence or "conversation graph."
    /// It holds a reference to the starting DialogueNode, which kicks off the dialogue flow.
    /// This allows different dialogue sequences to be created as distinct assets.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        [Tooltip("The starting node of this dialogue sequence.")]
        public DialogueNode startNode;

        // Optional: for visual graph editors
        [HideInInspector] public Vector2 editorPosition;
    }
}
```

### 8. Test Dialogue Trigger

A simple script to start dialogue with a key press.

**File: `Scripts/DialogueSystem/Test/TestDialogueTrigger.cs`**
```csharp
using UnityEngine;

namespace DialogueSystem.Test
{
    /// <summary>
    /// A simple test script to trigger a dialogue sequence when a key is pressed.
    /// Attach this to any GameObject in your scene.
    /// </summary>
    public class TestDialogueTrigger : MonoBehaviour
    {
        [Tooltip("The DialogueGraph asset to start when the trigger key is pressed.")]
        public DialogueGraph dialogueToStart;

        [Tooltip("The key to press to start the dialogue.")]
        public KeyCode triggerKey = KeyCode.Space;

        void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                if (dialogueToStart != null && DialogueSystem.DialogueSystemManager.Instance != null)
                {
                    Debug.Log($"TestDialogueTrigger: Starting dialogue '{dialogueToStart.name}'...");
                    DialogueSystem.DialogueSystemManager.Instance.StartDialogue(dialogueToStart);
                }
                else
                {
                    Debug.LogWarning("TestDialogueTrigger: DialogueGraph not assigned or DialogueSystemManager instance not found.");
                }
            }
        }
    }
}
```