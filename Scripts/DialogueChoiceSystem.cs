// Unity Design Pattern Example: DialogueChoiceSystem
// This script demonstrates the DialogueChoiceSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DialogueChoiceSystem' design pattern in Unity helps manage dynamic conversations where player choices influence the dialogue flow. This example demonstrates a robust, event-driven implementation using ScriptableObjects for data and a clear separation of concerns for the manager and UI.

**Key Components of this Design Pattern:**

1.  **`DialogueLine` (Struct):** Represents a single piece of dialogue text, including who says it.
2.  **`DialogueChoice` (Struct):** Defines a player option, its text, and which `DialogueNode` to transition to.
3.  **`DialogueNode` (ScriptableObject):** A modular unit of conversation. It contains a sequence of `DialogueLine`s and, optionally, a list of `DialogueChoice`s to present at its conclusion.
4.  **`DialogueGraph` (ScriptableObject):** An asset that holds the starting `DialogueNode` for an entire conversation, acting as the entry point.
5.  **`DialogueManager` (MonoBehaviour, Singleton):** The central controller. It orchestrates the dialogue flow, keeps track of the current node and line, and fires events for UI updates. It's a singleton to ensure easy global access.
6.  **`DialogueUIController` (MonoBehaviour):** A separate component responsible for subscribing to `DialogueManager` events and updating the actual UI elements (text, buttons) on the screen. This separates UI logic from game logic.
7.  **`ChoiceButton` (MonoBehaviour):** A simple helper script attached to individual choice buttons, responsible for displaying its text and notifying the `DialogueManager` when clicked.

---

### `DialogueChoiceSystem.cs`

This single file contains all the necessary classes and structs to implement the DialogueChoiceSystem.

To use this, create a new C# script named `DialogueChoiceSystem.cs` in your Unity project and paste the entire code below into it.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI; // Required for UI components like Text, Button
using TMPro;         // Recommended for TextMeshPro if you use it, otherwise use UnityEngine.UI.Text

namespace DialogueSystem
{
    /// <summary>
    /// Represents a single line of dialogue with a speaker and text.
    /// </summary>
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        [TextArea(3, 5)] // Makes the string field a larger text area in the Inspector
        public string dialogueText;
    }

    /// <summary>
    /// Represents a player choice, linking to the next dialogue node.
    /// </summary>
    [System.Serializable]
    public struct DialogueChoice
    {
        public string choiceText;
        public DialogueNode nextNode; // Reference to the next node in the conversation
        // You could extend this to include events, conditions, or item rewards.
    }

    /// <summary>
    /// A ScriptableObject representing a single logical block or "node" in a conversation.
    /// It contains a sequence of dialogue lines and potential choices to advance.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        public string nodeName; // For easy identification in the editor
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public bool isEndOfDialogue = false; // If true, this node ends the conversation
    }

    /// <summary>
    /// A ScriptableObject representing an entire conversation flow.
    /// It specifies the starting node for a dialogue sequence.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        public DialogueNode startNode; // The first node to begin this conversation
    }

    /// <summary>
    /// The core manager for the dialogue system. It handles dialogue state,
    /// progresses through lines, presents choices, and fires events for UI updates.
    /// Implemented as a Singleton for easy global access.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // Singleton pattern implementation
        public static DialogueManager Instance { get; private set; }

        // --- Dialogue State ---
        private DialogueGraph _currentGraph;
        private DialogueNode _currentNode;
        private int _currentLineIndex;
        private bool _isDialogueActive = false; // Tracks if dialogue is currently running

        // --- Events for UI and other game systems to subscribe to ---
        public static event Action OnDialogueStarted;
        public static event Action<string, string> OnDialogueLineDisplayed; // Speaker, Text
        public static event Action<List<DialogueChoice>> OnChoicesPresented; // List of choices
        public static event Action OnDialogueEnded;

        void Awake()
        {
            // Ensure only one instance of DialogueManager exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep manager across scenes if needed
            }
        }

        /// <summary>
        /// Starts a new dialogue conversation from a given DialogueGraph.
        /// </summary>
        /// <param name="graph">The DialogueGraph asset to start.</param>
        public void StartDialogue(DialogueGraph graph)
        {
            if (_isDialogueActive)
            {
                Debug.LogWarning("Dialogue already active. Cannot start new dialogue: " + graph.name);
                return;
            }

            if (graph == null || graph.startNode == null)
            {
                Debug.LogError("Cannot start dialogue: DialogueGraph or its start node is null.");
                return;
            }

            _currentGraph = graph;
            _currentNode = _currentGraph.startNode;
            _currentLineIndex = 0;
            _isDialogueActive = true;

            OnDialogueStarted?.Invoke();
            Debug.Log($"Dialogue Started: {_currentGraph.name}");

            DisplayNextLine(); // Display the first line of the starting node
        }

        /// <summary>
        /// Advances to the next line in the current dialogue node.
        /// If all lines in the node are displayed, it presents choices or ends the dialogue.
        /// </summary>
        public void DisplayNextLine()
        {
            if (!_isDialogueActive || _currentNode == null)
            {
                Debug.LogWarning("Cannot display next line: Dialogue not active or current node is null.");
                return;
            }

            // Display current line
            if (_currentLineIndex < _currentNode.dialogueLines.Count)
            {
                DialogueLine line = _currentNode.dialogueLines[_currentLineIndex];
                OnDialogueLineDisplayed?.Invoke(line.speakerName, line.dialogueText);
                Debug.Log($"[{line.speakerName}]: {line.dialogueText}");
                _currentLineIndex++;
            }
            // All lines in the current node have been displayed
            else
            {
                // If it's the end of the conversation
                if (_currentNode.isEndOfDialogue)
                {
                    EndDialogue();
                }
                // Otherwise, present choices to the player
                else if (_currentNode.choices != null && _currentNode.choices.Count > 0)
                {
                    OnChoicesPresented?.Invoke(_currentNode.choices);
                    Debug.Log("Choices presented.");
                }
                else
                {
                    // A node without choices and not marked as end of dialogue should probably not happen
                    // Or it implies automatic progression to a default next node (not implemented here for simplicity)
                    Debug.LogWarning($"Node '{_currentNode.name}' has no choices and is not marked as end of dialogue. Ending dialogue by default.");
                    EndDialogue();
                }
            }
        }

        /// <summary>
        /// Processes a player's choice, transitioning to the next specified dialogue node.
        /// </summary>
        /// <param name="choice">The DialogueChoice selected by the player.</param>
        public void MakeChoice(DialogueChoice choice)
        {
            if (!_isDialogueActive)
            {
                Debug.LogWarning("Cannot make choice: Dialogue not active.");
                return;
            }

            if (choice.nextNode == null)
            {
                Debug.LogWarning($"Choice '{choice.choiceText}' leads to a null next node. Ending dialogue.");
                EndDialogue();
                return;
            }

            _currentNode = choice.nextNode;
            _currentLineIndex = 0; // Reset line index for the new node
            Debug.Log($"Player chose: '{choice.choiceText}'. Moving to node: {_currentNode.name}");

            // Immediately display the first line of the new node
            DisplayNextLine();
        }

        /// <summary>
        /// Ends the current dialogue conversation.
        /// </summary>
        public void EndDialogue()
        {
            if (!_isDialogueActive) return;

            _isDialogueActive = false;
            _currentGraph = null;
            _currentNode = null;
            _currentLineIndex = 0;

            OnDialogueEnded?.Invoke();
            Debug.Log("Dialogue Ended.");
        }

        /// <summary>
        /// Public getter to check if dialogue is currently active.
        /// Useful for other systems (e.g., player movement, camera) to know dialogue state.
        /// </summary>
        public bool IsDialogueActive()
        {
            return _isDialogueActive;
        }
    }

    /// <summary>
    /// Manages the visual display of dialogue and choices on the UI canvas.
    /// Subscribes to events from DialogueManager to update UI elements.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject dialoguePanel; // The root panel for all dialogue UI
        public TextMeshProUGUI speakerNameText; // TextMeshPro required for this, or use UnityEngine.UI.Text
        public TextMeshProUGUI dialogueLineText; // TextMeshPro required for this, or use UnityEngine.UI.Text
        public Transform choiceButtonContainer; // Parent transform for choice buttons
        public GameObject choiceButtonPrefab; // Prefab for individual choice buttons

        private List<ChoiceButton> _currentChoiceButtons = new List<ChoiceButton>();

        void OnEnable()
        {
            // Subscribe to DialogueManager events when this script is enabled
            DialogueManager.OnDialogueStarted += HandleDialogueStarted;
            DialogueManager.OnDialogueLineDisplayed += HandleDialogueLineDisplayed;
            DialogueManager.OnChoicesPresented += HandleChoicesPresented;
            DialogueManager.OnDialogueEnded += HandleDialogueEnded;
        }

        void OnDisable()
        {
            // Unsubscribe from DialogueManager events when this script is disabled
            DialogueManager.OnDialogueStarted -= HandleDialogueStarted;
            DialogueManager.OnDialogueLineDisplayed -= HandleDialogueLineDisplayed;
            DialogueManager.OnChoicesPresented -= HandleChoicesPresented;
            DialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        void Start()
        {
            // Ensure dialogue panel is hidden at start
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            ClearChoices(); // Clear any pre-existing buttons
        }

        // --- Event Handlers ---

        private void HandleDialogueStarted()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
            // Clear previous dialogue and choices
            speakerNameText.text = "";
            dialogueLineText.text = "";
            ClearChoices();
        }

        private void HandleDialogueLineDisplayed(string speaker, string line)
        {
            speakerNameText.text = speaker;
            dialogueLineText.text = line;
            ClearChoices(); // Hide choices while dialogue lines are being displayed
        }

        private void HandleChoicesPresented(List<DialogueChoice> choices)
        {
            ClearChoices(); // Remove any old choices first

            foreach (DialogueChoice choice in choices)
            {
                GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                ChoiceButton choiceButton = buttonGO.GetComponent<ChoiceButton>();
                if (choiceButton != null)
                {
                    // Setup the button with its text and a callback to the DialogueManager
                    choiceButton.Setup(choice, DialogueManager.Instance.MakeChoice);
                    _currentChoiceButtons.Add(choiceButton);
                }
                else
                {
                    Debug.LogError("ChoiceButton prefab does not have a ChoiceButton component!");
                }
            }
        }

        private void HandleDialogueEnded()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
            speakerNameText.text = "";
            dialogueLineText.text = "";
            ClearChoices();
        }

        /// <summary>
        /// Clears all dynamically created choice buttons.
        /// </summary>
        private void ClearChoices()
        {
            foreach (ChoiceButton btn in _currentChoiceButtons)
            {
                Destroy(btn.gameObject);
            }
            _currentChoiceButtons.Clear();
        }

        // --- Input Handling (Example: for progressing dialogue lines) ---

        void Update()
        {
            // If dialogue is active and there are no choices presented,
            // allow pressing Space (or left mouse click) to advance the current dialogue line.
            // This assumes the UI controller is active when dialogue is active.
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive() && _currentChoiceButtons.Count == 0)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    DialogueManager.Instance.DisplayNextLine();
                }
            }
        }
    }

    /// <summary>
    /// A small helper script for individual choice buttons.
    /// It displays the choice text and calls back to the DialogueManager when clicked.
    /// </summary>
    public class ChoiceButton : MonoBehaviour
    {
        public TextMeshProUGUI choiceTextComponent; // TextMeshPro required for this, or use UnityEngine.UI.Text
        private Button _button;
        private DialogueChoice _choice;
        private Action<DialogueChoice> _onChoiceMade;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError("ChoiceButton requires a Button component on the same GameObject!");
            }
            _button.onClick.AddListener(OnButtonClick);
        }

        /// <summary>
        /// Sets up the choice button with its data and a callback function.
        /// </summary>
        /// <param name="choiceData">The DialogueChoice struct for this button.</param>
        /// <param name="callback">The action to invoke when this choice is selected.</param>
        public void Setup(DialogueChoice choiceData, Action<DialogueChoice> callback)
        {
            _choice = choiceData;
            _onChoiceMade = callback;

            if (choiceTextComponent != null)
            {
                choiceTextComponent.text = choiceData.choiceText;
            }
            else
            {
                Debug.LogWarning("ChoiceTextComponent not assigned on ChoiceButton!", this);
            }
        }

        private void OnButtonClick()
        {
            _onChoiceMade?.Invoke(_choice);
        }
    }

    /// <summary>
    /// Example script to trigger a dialogue from another GameObject (e.g., a character the player interacts with).
    /// </summary>
    public class DialogueTrigger : MonoBehaviour
    {
        public DialogueGraph dialogueToTrigger; // Assign your DialogueGraph asset here

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && dialogueToTrigger != null)
            {
                Debug.Log($"Player entered trigger. Attempting to start dialogue: {dialogueToTrigger.name}");
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(dialogueToTrigger);
                }
                else
                {
                    Debug.LogError("DialogueManager instance not found!");
                }
            }
        }

        // Example for manual trigger (e.g., pressing 'E' when near an NPC)
        // You would typically have a more sophisticated interaction system.
        public void ManualTriggerDialogue()
        {
            if (dialogueToTrigger != null)
            {
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(dialogueToTrigger);
                }
                else
                {
                    Debug.LogError("DialogueManager instance not found!");
                }
            }
            else
            {
                Debug.LogWarning("No DialogueGraph assigned to DialogueTrigger!");
            }
        }
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create the Script:**
    *   In your Unity project, create a new C# script named `DialogueChoiceSystem.cs` (or copy the code into an existing one).

2.  **Install TextMeshPro:**
    *   If you don't have it already, go to `Window > TextMeshPro > Import TMP Essential Resources`. This script uses `TextMeshProUGUI` for better text rendering, but you can swap to `UnityEngine.UI.Text` if preferred (remember to change `using TMPro;` to `using UnityEngine.UI;` in the `DialogueUIController` and `ChoiceButton` classes, and change component types in the Inspector).

3.  **Set up the UI Canvas:**
    *   Create a new UI Canvas (`GameObject > UI > Canvas`).
    *   Set its `Render Mode` to `Screen Space - Camera` and assign your main camera. Set `UI Scale Mode` to `Scale With Screen Size` (e.g., 1920x1080) for responsiveness.
    *   **Dialogue Panel:** Create an empty `Panel` GameObject under the Canvas (e.g., `GameObject > UI > Panel`). Name it "DialoguePanel". This will be the root for your dialogue UI. Make sure it covers a good portion of the screen.
    *   **Speaker Name:** Inside "DialoguePanel", create a `TextMeshPro - Text` (or UI Text). Name it "SpeakerNameText". Position it at the top of the dialogue panel.
    *   **Dialogue Line:** Inside "DialoguePanel", create another `TextMeshPro - Text`. Name it "DialogueLineText". Position it below the Speaker Name. Adjust its size to fit a few lines of dialogue.
    *   **Choice Button Container:** Inside "DialoguePanel", create an empty GameObject. Name it "ChoiceButtonContainer". Add a `Vertical Layout Group` component to it (`Add Component > Layout > Vertical Layout Group`) and adjust padding/spacing as desired. This will automatically arrange your choice buttons. Position it below the Dialogue Line Text.
    *   **Choice Button Prefab:**
        *   Create a `Button` (`GameObject > UI > Button - TextMeshPro`). Name it "ChoiceButtonPrefab".
        *   Make sure its child `TextMeshPro - Text` is named "Text".
        *   Add the `ChoiceButton` script (from `DialogueChoiceSystem.cs`) to this "ChoiceButtonPrefab" GameObject.
        *   Drag the child "Text" component from the button into the `Choice Text Component` slot of the `ChoiceButton` script.
        *   Adjust the button's size, text font, and colors to your liking.
        *   Drag this "ChoiceButtonPrefab" from the Hierarchy into your Project window (e.g., into a "Prefabs" folder) to make it a prefab. Delete it from the Hierarchy afterwards.

4.  **Create Dialogue Manager & UI Controller GameObjects:**
    *   Create an empty GameObject in your scene. Name it "DialogueManager".
    *   Add the `DialogueManager` script (from `DialogueChoiceSystem.cs`) to this GameObject.
    *   Create another empty GameObject in your scene. Name it "DialogueUIController".
    *   Add the `DialogueUIController` script (from `DialogueChoiceSystem.cs`) to this GameObject.
    *   **Link UI Elements:** On the "DialogueUIController" GameObject in the Inspector, drag the created UI elements:
        *   `DialoguePanel` -> your "DialoguePanel" GameObject.
        *   `Speaker Name Text` -> your "SpeakerNameText" TextMeshPro component.
        *   `Dialogue Line Text` -> your "DialogueLineText" TextMeshPro component.
        *   `Choice Button Container` -> your "ChoiceButtonContainer" GameObject.
        *   `Choice Button Prefab` -> your "ChoiceButtonPrefab" asset from the Project window.

5.  **Create Dialogue Data (ScriptableObjects):**
    *   In your Project window, right-click and go to `Create > Dialogue`. You will see two options: `Dialogue Graph` and `Dialogue Node`.
    *   **Create Dialogue Nodes first:**
        *   Create multiple `Dialogue Node` assets (e.g., "Node_Intro", "Node_ChoiceA", "Node_ChoiceB", "Node_Ending").
        *   Select each node and fill out its `Dialogue Lines` (speaker, text).
        *   If a node should offer choices, add items to its `Choices` list. For each choice, set its `Choice Text` and drag another `Dialogue Node` asset into the `Next Node` slot to define where that choice leads.
        *   Mark nodes that should end the conversation as `Is End Of Dialogue`.
    *   **Create a Dialogue Graph:**
        *   Create a `Dialogue Graph` asset (e.g., "MyFirstConversation").
        *   Drag your starting `Dialogue Node` (e.g., "Node_Intro") into its `Start Node` slot.

    **Example Dialogue Flow:**
    *   **Node_Intro:**
        *   Line 1: "Stranger", "Hello, traveler. Lost?"
        *   Line 2: "Player", "A little. Which way to town?"
        *   Choices:
            *   "Ask for directions (leads to Node_Directions)"
            *   "Refuse help (leads to Node_Refuse)"
    *   **Node_Directions:**
        *   Line 1: "Stranger", "Ah, Townsville is North. But beware the forest."
        *   Choices:
            *   "Thank him and leave (leads to Node_EndingThanks)"
    *   **Node_Refuse:**
        *   Line 1: "Player", "I don't need your help."
        *   Line 2: "Stranger", "As you wish. Good luck."
        *   `Is End Of Dialogue` = True
    *   **Node_EndingThanks:**
        *   Line 1: "Player", "Thanks!"
        *   Line 2: "Stranger", "May your journey be safe."
        *   `Is End Of Dialogue` = True
    *   **MyFirstConversation (Dialogue Graph):**
        *   `Start Node` = "Node_Intro"

6.  **Trigger Dialogue:**
    *   To start a dialogue, you need another script to call `DialogueManager.Instance.StartDialogue(yourDialogueGraph)`.
    *   **Example `DialogueTrigger`:**
        *   Create an empty GameObject in your scene (e.g., "NPC_InteractionPoint").
        *   Add a `Box Collider` component to it, set `Is Trigger` to true, and adjust its size.
        *   Add the `DialogueTrigger` script (from `DialogueChoiceSystem.cs`) to this GameObject.
        *   Drag your "MyFirstConversation" `Dialogue Graph` asset into the `Dialogue To Trigger` slot of the `DialogueTrigger` script.
        *   Make sure your player GameObject has a `Rigidbody` and its tag is set to "Player" so it can trigger the `OnTriggerEnter` event.

Now, run your game. When your player enters the `DialogueTrigger` collider, the dialogue should start, display lines, and present choices based on your ScriptableObject setup! You can click or press Space to advance dialogue lines.