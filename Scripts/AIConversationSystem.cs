// Unity Design Pattern Example: AIConversationSystem
// This script demonstrates the AIConversationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **AI Conversation System** design pattern in Unity. This pattern focuses on creating a robust, data-driven, and extensible system for managing dialogues and interactions with AI characters (NPCs).

It leverages a state-machine-like approach where each piece of AI dialogue and its subsequent player choices represent a 'state' or 'node' in a conversation graph. Unity Events are used to decouple the core conversation logic from the UI and other game systems, adhering to the Observer pattern.

---

## AIConversationSystem.cs

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.Collections.Generic;
using System; // Required for [Serializable]

namespace AIConversationSystemPattern
{
    /// <summary>
    /// Represents a single option a player can choose during a conversation.
    /// This is a serializable class, allowing its properties to be set directly in the Unity Inspector.
    /// </summary>
    [System.Serializable]
    public class PlayerChoice
    {
        [Tooltip("The text displayed to the player for this choice.")]
        public string choiceText;

        [Tooltip("The ID of the next conversation node this choice leads to. Leave empty to end the conversation.")]
        public string nextNodeId;

        [Tooltip("Optional actions to trigger when this choice is selected. " +
                 "Drag GameObjects/scripts here and select public methods to execute.")]
        public UnityEvent onChoiceSelected;

        /// <summary>
        /// Constructor for easier programmatic creation if needed.
        /// </summary>
        public PlayerChoice(string text, string nextId = "", UnityAction action = null)
        {
            choiceText = text;
            nextNodeId = nextId;
            onChoiceSelected = new UnityEvent();
            if (action != null)
            {
                onChoiceSelected.AddListener(action);
            }
        }
    }

    /// <summary>
    /// Represents a single 'turn' or 'state' in the AI conversation.
    /// It contains the AI's dialogue and the potential player choices that follow.
    /// This is a serializable class, allowing its properties to be set directly in the Unity Inspector.
    /// </summary>
    [System.Serializable]
    public class ConversationNode
    {
        [Tooltip("Unique identifier for this conversation node. Used to link choices to specific nodes.")]
        public string id;

        [Tooltip("The dialogue text the AI character speaks at this node.")]
        [TextArea(3, 6)] // Makes the string field a multi-line text area in the Inspector.
        public string aiDialogue;

        [Tooltip("A list of choices the player can make in response to the AI's dialogue.")]
        public List<PlayerChoice> playerChoices;

        /// <summary>
        /// Constructor for easier programmatic creation if needed.
        /// </summary>
        public ConversationNode(string nodeId, string dialogue)
        {
            id = nodeId;
            aiDialogue = dialogue;
            playerChoices = new List<PlayerChoice>();
        }
    }

    /// <summary>
    /// The core AI Conversation System. This MonoBehaviour manages the flow, state,
    /// and logic of conversations with an AI character.
    ///
    /// This system embodies a state machine pattern where each ConversationNode is a state,
    /// and player choices are the transitions between states. It uses an Observer pattern
    /// (via UnityEvents) to notify UI and other systems about conversation progress,
    /// decoupling them from the core conversation logic.
    /// </summary>
    [DisallowMultipleComponent] // Ensures only one instance of this script can be on a GameObject.
    public class AIConversationSystem : MonoBehaviour
    {
        [Header("Conversation Setup")]
        [Tooltip("The ID of the conversation node to start with when StartConversation() is called.")]
        public string startNodeId = "start";

        [Tooltip("The initial list of conversation nodes that define the conversation tree. " +
                 "These nodes are loaded into memory when the system awakes.")]
        [SerializeField] // Makes a private field visible and editable in the Inspector.
        private List<ConversationNode> initialConversationData = new List<ConversationNode>();

        // Internal dictionary for quick lookup of conversation nodes by their ID at runtime.
        private Dictionary<string, ConversationNode> conversationMap;

        // The current node the conversation is actively at.
        private ConversationNode currentConversationNode;

        [Header("Events (for UI and Game Logic Integration)")]
        [Tooltip("Event invoked when the AI has new dialogue to display. Passes the AI's dialogue text.")]
        public UnityEvent<string> OnDialogueDisplayed;

        [Tooltip("Event invoked when new player choices are available. Passes a list of PlayerChoice objects.")]
        public UnityEvent<List<PlayerChoice>> OnPlayerChoicesGenerated;

        [Tooltip("Event invoked when the conversation ends (e.g., no more choices or a choice leads to no next node).")]
        public UnityEvent OnConversationEnded;

        private void Awake()
        {
            // Initialize the conversation map from the serialized list provided in the Inspector.
            // This allows designers to build entire conversations visually.
            conversationMap = new Dictionary<string, ConversationNode>();
            foreach (var node in initialConversationData)
            {
                if (string.IsNullOrEmpty(node.id))
                {
                    Debug.LogError($"AIConversationSystem: Conversation node found with empty ID. Please ensure all node IDs are unique and not empty. Node will be ignored.", this);
                    continue;
                }
                if (conversationMap.ContainsKey(node.id))
                {
                    Debug.LogWarning($"AIConversationSystem: Duplicate conversation node ID '{node.id}' found. Please ensure all node IDs are unique. First instance will be used.", this);
                    continue;
                }
                conversationMap.Add(node.id, node);
            }

            // Ensure UnityEvents are initialized to prevent NullReferenceExceptions if no listeners are assigned
            // in the Inspector before an event is invoked.
            if (OnDialogueDisplayed == null) OnDialogueDisplayed = new UnityEvent<string>();
            if (OnPlayerChoicesGenerated == null) OnPlayerChoicesGenerated = new UnityEvent<List<PlayerChoice>>();
            if (OnConversationEnded == null) OnConversationEnded = new UnityEvent();
        }

        private void Start()
        {
            // Automatically start the conversation with the designated start node when the scene loads.
            // In a real game, this might be triggered by player proximity, interaction, a quest event, etc.,
            // rather than automatically in Start().
            // For this example, it demonstrates the initial setup and flow.
            StartConversation(startNodeId);
        }

        /// <summary>
        /// Initiates or restarts a conversation from a specific node ID.
        /// This is the entry point for starting any conversation with the AI.
        /// </summary>
        /// <param name="nodeId">The ID of the conversation node to start from.</param>
        public void StartConversation(string nodeId)
        {
            if (conversationMap == null || conversationMap.Count == 0)
            {
                Debug.LogError("AIConversationSystem: Conversation data is not loaded or empty. Cannot start conversation.", this);
                return;
            }

            if (!conversationMap.ContainsKey(nodeId))
            {
                Debug.LogError($"AIConversationSystem: Start node with ID '{nodeId}' not found in conversation data. Ending conversation.", this);
                EndConversation(); // End immediately if the specified start node is invalid.
                return;
            }

            Debug.Log($"AIConversationSystem: Starting conversation from node '{nodeId}'.");
            SetCurrentNode(nodeId); // Transition to the initial node.
        }

        /// <summary>
        /// Processes the player's selected choice, advances the conversation to the next node,
        /// and triggers any associated actions defined on the chosen option.
        /// </summary>
        /// <param name="choiceIndex">The zero-based index of the choice selected by the player
        ///                             from the current list of available options.</param>
        public void ChoosePlayerOption(int choiceIndex)
        {
            // Validate the current conversation state and the chosen index.
            if (currentConversationNode == null || currentConversationNode.playerChoices == null ||
                choiceIndex < 0 || choiceIndex >= currentConversationNode.playerChoices.Count)
            {
                Debug.LogWarning($"AIConversationSystem: Invalid choice index {choiceIndex} or no active conversation node. Current node: {(currentConversationNode != null ? currentConversationNode.id : "None")}", this);
                return;
            }

            PlayerChoice chosen = currentConversationNode.playerChoices[choiceIndex];
            Debug.Log($"AIConversationSystem: Player chose: '{chosen.choiceText}'");

            // Invoke any UnityEvents (actions) associated with this specific player choice.
            // This is where game logic can be tied directly into the conversation flow.
            chosen.onChoiceSelected?.Invoke();

            // Determine the next step in the conversation.
            if (!string.IsNullOrEmpty(chosen.nextNodeId))
            {
                // If a next node ID is provided, transition to that node.
                SetCurrentNode(chosen.nextNodeId);
            }
            else
            {
                // If no next node ID, this choice implicitly ends the conversation.
                Debug.Log("AIConversationSystem: Conversation concluded by player choice.");
                EndConversation();
            }
        }

        /// <summary>
        /// Internal method to transition the conversation to a new node.
        /// This method updates the internal state and notifies external listeners (like UI)
        /// about the new dialogue and choices.
        /// </summary>
        /// <param name="nodeId">The ID of the conversation node to transition to.</param>
        private void SetCurrentNode(string nodeId)
        {
            // Attempt to retrieve the next node from the conversation map.
            if (!conversationMap.TryGetValue(nodeId, out ConversationNode nextNode))
            {
                Debug.LogError($"AIConversationSystem: Attempted to transition to non-existent node ID: '{nodeId}'. Ending conversation.", this);
                EndConversation();
                return;
            }

            currentConversationNode = nextNode; // Update the current state of the conversation.
            Debug.Log($"AIConversationSystem: Now at node '{currentConversationNode.id}'. AI says: '{currentConversationNode.aiDialogue}'");

            // Notify listeners (e.g., UI script) about the new AI dialogue.
            OnDialogueDisplayed?.Invoke(currentConversationNode.aiDialogue);

            // Check if there are player choices available at this new node.
            if (currentConversationNode.playerChoices != null && currentConversationNode.playerChoices.Count > 0)
            {
                // Notify listeners about the new player choices.
                OnPlayerChoicesGenerated?.Invoke(currentConversationNode.playerChoices);
            }
            else
            {
                // If there are no choices, the conversation implicitly ends after this AI dialogue.
                Debug.Log("AIConversationSystem: No player choices available at this node. Conversation concluding.");
                // Send an empty list to UI to clear any previously displayed choices.
                OnPlayerChoicesGenerated?.Invoke(new List<PlayerChoice>());
                EndConversation();
            }
        }

        /// <summary>
        /// Explicitly ends the current conversation. This resets the system's state
        /// and notifies any listeners that the conversation has finished.
        /// </summary>
        public void EndConversation()
        {
            currentConversationNode = null; // Clear the current state.
            OnConversationEnded?.Invoke(); // Notify listeners that the conversation is over.
            // Also notify UI to clear any remaining choice buttons or dialogue.
            OnPlayerChoicesGenerated?.Invoke(new List<PlayerChoice>());
            OnDialogueDisplayed?.Invoke(""); // Clear dialogue text
            Debug.Log("AIConversationSystem: Conversation officially ended.");
        }


        // --- Example Usage and Explanation in Unity ---
        /*
        The AI Conversation System pattern is designed to make conversation flows
        easy to define and manage, separate from complex coding logic.

        **1. Create the Conversation Manager:**
           - In your Unity scene, create an empty GameObject (e.g., "AIConversationManager").
           - Attach this `AIConversationSystem.cs` script to that GameObject.

        **2. Define Conversation Data in the Inspector:**
           - Select your "AIConversationManager" GameObject.
           - In the Inspector, you will see a `Initial Conversation Data` list.
           - Click the `+` button to add new `Conversation Node` elements.

           **For each Conversation Node:**
           - **ID**: Assign a unique string identifier (e.g., "start", "quest_intro", "thanks_for_help"). This ID is crucial for linking nodes.
           - **AI Dialogue**: Type the text that the AI character will say at this point in the conversation.
           - **Player Choices**: This is a list of options the player can select.
             - Click the `+` button to add new `Player Choice` elements for this node.

             **For each Player Choice:**
             - **Choice Text**: The text displayed on a UI button for the player to click.
             - **Next Node ID**: Enter the `ID` of the `Conversation Node` that this choice should lead to.
               - **Important**: If you leave `Next Node ID` empty, selecting this choice will end the conversation.
             - **On Choice Selected**: This is a UnityEvent. You can drag and drop any GameObject or script onto this slot and select a public method to be called when this specific player choice is made.
               - **Practical Use**: This is powerful for integrating game logic:
                 - `QuestManager.StartQuest()`
                 - `Inventory.AddItem("Potion")`
                 - `AudioManager.PlaySFX("QuestAccepted")`
                 - `NPC.UpdateNPCState("Happy")`

        **Example Conversation Structure (as you would configure it in the Inspector):**

        --- AIConversationManager GameObject (Inspector) ---

        AI Conversation System (Script):
            Start Node ID: "start"

            Initial Conversation Data (List):
                Element 0 (Conversation Node):
                    ID: "start"
                    AI Dialogue: "Greetings, adventurer! How may I assist you today?"
                    Player Choices (List):
                        Element 0 (Player Choice):
                            Choice Text: "Tell me about the quest."
                            Next Node ID: "quest_intro"
                            On Choice Selected: (empty, or add e.g., QuestManager.PreQuestDialogue)
                        Element 1 (Player Choice):
                            Choice Text: "I'm just passing by."
                            Next Node ID: "farewell"
                            On Choice Selected: (empty)

                Element 1 (Conversation Node):
                    ID: "quest_intro"
                    AI Dialogue: "A fearsome dragon has stolen our village's ancient relic. Will you help us?"
                    Player Choices (List):
                        Element 0 (Player Choice):
                            Choice Text: "Yes, I will help!"
                            Next Node ID: "quest_accepted"
                            On Choice Selected: (Add listener: yourQuestManager.AcceptQuest)
                        Element 1 (Player Choice):
                            Choice Text: "No, I am too busy."
                            Next Node ID: "quest_declined"
                            On Choice Selected: (Add listener: yourQuestManager.DeclineQuest)

                Element 2 (Conversation Node):
                    ID: "quest_accepted"
                    AI Dialogue: "Wonderful! The dragon's lair is in the Whispering Mountains. Be careful!"
                    Player Choices (List):
                        Element 0 (Player Choice):
                            Choice Text: "Thank you for the information."
                            Next Node ID: "" // Conversation ends here because Next Node ID is empty.
                            On Choice Selected: (empty)

                Element 3 (Conversation Node):
                    ID: "quest_declined"
                    AI Dialogue: "That is most unfortunate. Our hopes rest on another then."
                    Player Choices (List):
                        Element 0 (Player Choice):
                            Choice Text: "Goodbye."
                            Next Node ID: "" // Conversation ends.
                            On Choice Selected: (empty)

                Element 4 (Conversation Node):
                    ID: "farewell"
                    AI Dialogue: "Very well. Safe travels, stranger."
                    Player Choices (List):
                        Element 0 (Player Choice):
                            Choice Text: "You too."
                            Next Node ID: "" // Conversation ends.
                            On Choice Selected: (empty)

        **3. Create a UI Handler Script (e.g., `ConversationUIHandler.cs`):**
           This script will listen to the events from `AIConversationSystem` and update your UI.

           ```csharp
           // --- ConversationUIHandler.cs (Create this as a separate new C# script) ---
           using UnityEngine;
           using UnityEngine.UI;
           using System.Collections.Generic;
           using AIConversationSystemPattern; // To access PlayerChoice class

           public class ConversationUIHandler : MonoBehaviour
           {
               [Header("UI References")]
               public GameObject conversationPanel; // The parent panel for all conversation UI
               public Text aiDialogueText;         // Text component to display AI's dialogue
               public Transform playerChoiceButtonParent; // Parent transform where choice buttons will be instantiated
               public Button playerChoiceButtonPrefab; // A prefab of a UI Button to use for player choices

               private AIConversationSystem conversationSystem; // Reference to the conversation system
               private List<Button> activeChoiceButtons = new List<Button>(); // To keep track of instantiated buttons

               void Awake()
               {
                   // Find the AIConversationSystem in the scene.
                   // In a larger project, you might use a dependency injection system or a GameManager to provide this reference.
                   conversationSystem = FindObjectOfType<AIConversationSystem>();
                   if (conversationSystem == null)
                   {
                       Debug.LogError("ConversationUIHandler: AIConversationSystem not found in scene! " +
                                      "Please ensure it's present and this script can find it.", this);
                   }

                   // Ensure the conversation UI is hidden initially.
                   conversationPanel.SetActive(false);
               }

               /// <summary>
               /// Called by AIConversationSystem.OnDialogueDisplayed event. Updates the AI's dialogue text.
               /// </summary>
               /// <param name="dialogue">The text the AI is speaking.</param>
               public void UpdateAIDialogue(string dialogue)
               {
                   conversationPanel.SetActive(true); // Make sure UI is visible when dialogue starts
                   aiDialogueText.text = dialogue;
                   Debug.Log($"UI: AI Says: \"{dialogue}\"");
               }

               /// <summary>
               /// Called by AIConversationSystem.OnPlayerChoicesGenerated event. Creates and displays player choice buttons.
               /// </summary>
               /// <param name="choices">A list of PlayerChoice objects to display.</param>
               public void PopulatePlayerChoices(List<PlayerChoice> choices)
               {
                   ClearPlayerChoicesUI(); // First, remove any previous choices from the UI.

                   if (choices == null || choices.Count == 0)
                   {
                       Debug.Log("UI: No choices to display.");
                       // If no choices, it usually means the conversation is over or needs to end.
                       // The AIConversationSystem will handle ending it, but UI should reflect that.
                       return;
                   }

                   Debug.Log($"UI: Displaying {choices.Count} player choices.");
                   for (int i = 0; i < choices.Count; i++)
                   {
                       PlayerChoice choice = choices[i];
                       int choiceIndex = i; // Capture the index for the lambda expression.

                       // Instantiate a new button from the prefab.
                       Button newButton = Instantiate(playerChoiceButtonPrefab, playerChoiceButtonParent);
                       // Set the text of the button (assuming the prefab has a Text component as a child).
                       newButton.GetComponentInChildren<Text>().text = choice.choiceText;

                       // Add a listener to the button's onClick event.
                       // When clicked, it tells the conversation system which choice was made.
                       newButton.onClick.AddListener(() =>
                       {
                           conversationSystem.ChoosePlayerOption(choiceIndex);
                           ClearPlayerChoicesUI(); // Clear choices after one is picked to prevent re-selection.
                       });

                       activeChoiceButtons.Add(newButton);
                       newButton.gameObject.SetActive(true); // Ensure the button is visible.
                   }
               }

               /// <summary>
               /// Helper method to remove all currently displayed player choice buttons from the UI.
               /// </summary>
               private void ClearPlayerChoicesUI()
               {
                   foreach (Button btn in activeChoiceButtons)
                   {
                       Destroy(btn.gameObject); // Destroy the button GameObject.
                   }
                   activeChoiceButtons.Clear(); // Clear the list of references.
               }

               /// <summary>
               /// Called by AIConversationSystem.OnConversationEnded event. Hides the conversation UI.
               /// </summary>
               public void HideUI()
               {
                   conversationPanel.SetActive(false); // Hide the main conversation panel.
                   aiDialogueText.text = ""; // Clear any remaining dialogue text.
                   ClearPlayerChoicesUI(); // Ensure all choice buttons are also cleared.
                   Debug.Log("UI: Conversation UI hidden.");
               }
           }
           // --- End ConversationUIHandler.cs ---
           ```

        **4. Set up UI in Unity:**
           - Create a Canvas in your scene (`GameObject -> UI -> Canvas`).
           - Inside the Canvas, create a Panel (e.g., `GameObject -> UI -> Panel`) and name it "ConversationPanel". This will be your `conversationPanel` reference.
           - Inside "ConversationPanel", create a Text element (`GameObject -> UI -> Text - TextMeshPro`) and name it "AIDialogueText". Assign it to `aiDialogueText`.
           - Inside "ConversationPanel", create an empty GameObject (e.g., "PlayerChoicesParent"). This will be your `playerChoiceButtonParent` reference. Add a `Vertical Layout Group` component to it for automatic button arrangement.
           - Create a Button Prefab: Create a Button (`GameObject -> UI -> Button - TextMeshPro`). Configure its appearance. Drag this Button from your Hierarchy into your Project window to create a prefab. Delete the Button from the Hierarchy. This prefab will be your `playerChoiceButtonPrefab`.

           - Attach `ConversationUIHandler.cs` to a GameObject, for example, "ConversationPanel" itself, or an empty GameObject called "UI_Manager".
           - In the Inspector for your `ConversationUIHandler` script:
             - Drag "ConversationPanel" to the `Conversation Panel` slot.
             - Drag "AIDialogueText" to the `AI Dialogue Text` slot.
             - Drag "PlayerChoicesParent" to the `Player Choice Button Parent` slot.
             - Drag your Button Prefab from the Project window to the `Player Choice Button Prefab` slot.

        **5. Connect the `AIConversationSystem` Events to the `ConversationUIHandler`:**
           - Select your "AIConversationManager" GameObject.
           - In the Inspector, locate the `Events` section of the `AI Conversation System` component.
           - For `On Dialogue Displayed`: Click `+`, drag your "ConversationPanel" (or whatever GameObject `ConversationUIHandler` is on) into the object slot, and select `ConversationUIHandler -> UpdateAIDialogue(string)`.
           - For `On Player Choices Generated`: Click `+`, drag your "ConversationPanel", and select `ConversationUIHandler -> PopulatePlayerChoices(List<PlayerChoice>)`.
           - For `On Conversation Ended`: Click `+`, drag your "ConversationPanel", and select `ConversationUIHandler -> HideUI()`.

        **6. Triggering Conversations (beyond `Start()`):**
           - While the example `AIConversationSystem` starts a conversation in `Start()`, you'll usually trigger it via player interaction.
           - Create an NPC script (e.g., `NPCScript.cs`) that holds a reference to your `AIConversationSystem`:

           ```csharp
           // --- NPCScript.cs (Example NPC Interaction Script) ---
           using UnityEngine;
           using AIConversationSystemPattern;

           public class NPCScript : MonoBehaviour
           {
               public AIConversationSystem conversationSystem;
               [Tooltip("The ID of the conversation node to start when interacting with this NPC.")]
               public string npcConversationStartNodeId = "start"; // Can vary per NPC

               void Start()
               {
                   // Attempt to find the conversation system if not assigned in the Inspector.
                   if (conversationSystem == null)
                   {
                       conversationSystem = FindObjectOfType<AIConversationSystem>();
                       if (conversationSystem == null)
                       {
                           Debug.LogError("NPCScript: AIConversationSystem not found in scene for " + gameObject.name, this);
                       }
                   }
               }

               // This method would be called by player input (e.g., pressing 'E' while near the NPC).
               public void Interact()
               {
                   if (conversationSystem != null)
                   {
                       Debug.Log($"{gameObject.name}: Player interacting, starting conversation '{npcConversationStartNodeId}'.");
                       conversationSystem.StartConversation(npcConversationStartNodeId);
                   }
               }

               // Example of how to call Interact() (e.g., on mouse click, or a trigger enter)
               void OnMouseDown() // For 3D objects with Colliders
               {
                   Interact();
               }

               // Or if you have a PlayerController checking for interactables:
               // public void OnPlayerInteraction() { Interact(); }
           }
           // --- End NPCScript.cs ---
           ```
           - Attach `NPCScript.cs` to your NPC GameObject, assign the `AIConversationSystem` reference (if not found automatically), and set its `NPC Conversation Start Node Id`.

        This complete setup provides a highly educational and practical example of the AI Conversation System pattern in Unity. It separates data from logic, uses events for decoupling, and allows designers to create complex interactive narratives without deep coding knowledge.
        */
    }
}
```