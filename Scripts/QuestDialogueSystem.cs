// Unity Design Pattern Example: QuestDialogueSystem
// This script demonstrates the QuestDialogueSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'QuestDialogueSystem' pattern in Unity. While not a standard Gang of Four pattern, it represents a common architectural approach for managing the interplay between narrative (dialogue) and progression (quests) in games. It often combines elements of Data-Driven Design (ScriptableObjects), Manager/Service Locator, Observer/Event System, and implicit State Machines.

The system is designed to be:
1.  **Data-Driven**: Quest and Dialogue data are defined as ScriptableObjects, making them easy to create, edit, and manage in the Unity Editor without touching code.
2.  **Modular**: Components are decoupled. The Manager handles logic, ScriptableObjects store data, and UI/NPCs interact through events.
3.  **Event-Based**: Uses UnityEvents for communication between different parts of the system (e.g., Quest UI reacts to `OnQuestAccepted`).
4.  **Centralized Control**: A single `QuestDialogueManager` acts as the hub for all quest and dialogue-related operations.

---

**How to Use This Example in Unity:**

1.  **Create a New C# Script**: Name it `QuestDialogueSystem.cs` and paste the entire code below into it.
2.  **Create Folders**: In your Project window, create folders like `Assets/ScriptableObjects/Quests` and `Assets/ScriptableObjects/Dialogue`.
3.  **Create ScriptableObject Assets**:
    *   Right-click in `Assets/ScriptableObjects/Quests` -> Create -> Quest/New Quest.
    *   Right-click in `Assets/ScriptableObjects/Dialogue` -> Create -> Quest/Dialogue/New Dialogue Node.
    *   Fill out the details for quests (name, description, objectives) and dialogue nodes (speaker, text, choices). Link them together.
4.  **Create a Manager GameObject**:
    *   Create an empty GameObject in your scene named `QuestDialogueManager`.
    *   Attach the `QuestDialogueManager` component (from the `QuestDialogueSystem.cs` script) to it.
5.  **Create an Example NPC**:
    *   Create an empty GameObject named `NPC_A`.
    *   Attach the `NPCInteraction` component (from the `QuestDialogueSystem.cs` script) to it.
    *   Assign a `Quest` ScriptableObject to its `Quest To Offer` field and a `DialogueNode` ScriptableObject to its `Initial Dialogue` field in the Inspector.
6.  **Create an Example UI**:
    *   Create a Canvas (GameObject -> UI -> Canvas).
    *   Add a Text element (GameObject -> UI -> Text - TextMeshPro). Name it `DialogueText`.
    *   Add another Text element for the speaker (GameObject -> UI -> Text - TextMeshPro). Name it `SpeakerText`.
    *   Add a Button (GameObject -> UI -> Button - TextMeshPro) for choices. Duplicate it a few times. Name them `ChoiceButton_1`, `ChoiceButton_2`, etc. Disable them by default.
    *   Create an empty GameObject named `DialogueUI`.
    *   Attach the `DialogueUIController` component (from the `QuestDialogueSystem.cs` script) to it.
    *   Drag and drop the `DialogueText`, `SpeakerText`, and `ChoiceButton` GameObjects to their respective fields on the `DialogueUIController` in the Inspector.
7.  **Play**: Run the scene. You'll likely need to add some input (e.g., clicking on the NPC) to trigger the `NPCInteraction.Interact()` method. The `DialogueUIController` will automatically subscribe to the `QuestDialogueManager`'s events.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using TMPro; // Requires TextMeshPro for UI elements

// Ensure TextMeshPro is installed: Window > TextMeshPro > Import TMP Essential Resources

namespace QuestDialogueSystem
{
    // --- 1. Enums and Data Structures ---

    /// <summary>
    /// Represents the possible states of a quest.
    /// </summary>
    public enum QuestState
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }

    /// <summary>
    /// Represents a single choice in a dialogue, potentially leading to another node or triggering an action.
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public DialogueNode nextNode; // The next dialogue node if this choice is made.
        public UnityEvent onChoiceMade; // Custom actions to trigger when this choice is selected.
    }

    // --- 2. ScriptableObjects for Data ---

    /// <summary>
    /// Base class for all quest objectives. ScriptableObjects allow us to define objectives as assets.
    /// You could extend this for specific objective types (e.g., KillXEnemiesObjective, CollectYItemsObjective).
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestObjective", menuName = "Quest/Quest Objective")]
    public class QuestObjective : ScriptableObject
    {
        public string objectiveName = "New Objective";
        [TextArea(3, 5)]
        public string description = "Objective Description.";
        public bool isCompleted = false; // Runtime state
        public bool isOptional = false;

        // Reset state for when a quest is re-accepted or game loads.
        public virtual void ResetObjective()
        {
            isCompleted = false;
        }

        // Method to mark objective complete. Could be overridden for custom logic.
        public virtual void Complete()
        {
            if (!isCompleted)
            {
                isCompleted = true;
                Debug.Log($"Objective '{objectiveName}' completed.");
            }
        }
    }

    /// <summary>
    /// Defines a Quest. This is a ScriptableObject, allowing quest data to be created
    /// and managed as assets in the Unity editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/New Quest")]
    public class Quest : ScriptableObject
    {
        public string questName = "New Quest";
        [TextArea(3, 10)]
        public string description = "Quest Description.";
        public QuestObjective[] objectives;
        public List<Quest> prerequisiteQuests; // Quests that must be completed before this one is available.
        public List<Quest> rewardQuests;       // Quests unlocked upon completion of this quest.
        public UnityEvent onQuestCompleted;    // Actions triggered when the quest is finished.
        public UnityEvent onQuestAccepted;     // Actions triggered when the quest is started.

        // Runtime state variables
        [NonSerialized] private QuestState _currentState = QuestState.NotStarted;
        [NonSerialized] private bool _initialized = false;

        public QuestState CurrentState
        {
            get { return _currentState; }
            set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    // Additional logic can be added here, e.g., saving state.
                }
            }
        }

        /// <summary>
        /// Initializes the quest's runtime state. Call this when the game starts or loads.
        /// </summary>
        public void InitializeQuest()
        {
            if (_initialized) return;

            CurrentState = QuestState.NotStarted;
            foreach (var obj in objectives)
            {
                obj.ResetObjective(); // Reset all objectives to not completed.
            }
            _initialized = true;
        }

        /// <summary>
        /// Checks if all non-optional objectives are completed.
        /// </summary>
        public bool AreAllObjectivesCompleted()
        {
            foreach (var objective in objectives)
            {
                if (!objective.isOptional && !objective.isCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets the quest to its initial state (NotStarted, all objectives uncompleted).
        /// Useful for testing or repeatable quests.
        /// </summary>
        public void ResetQuest()
        {
            CurrentState = QuestState.NotStarted;
            foreach (var obj in objectives)
            {
                obj.ResetObjective();
            }
        }
    }

    /// <summary>
    /// Defines a single node in a dialogue tree. This is a ScriptableObject,
    /// allowing dialogue data to be created and managed as assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Quest/Dialogue/New Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        public string speakerName = "NPC Name";
        [TextArea(3, 10)]
        public string dialogueText = "Hello, traveler.";
        public List<DialogueChoice> choices; // List of choices the player can make from this node.

        // Optional: Can trigger an event when this node is displayed (e.g., play animation, update quest objective)
        public UnityEvent onNodeDisplayed;

        /// <summary>
        /// Checks if the dialogue node has choices.
        /// </summary>
        public bool HasChoices => choices != null && choices.Count > 0;
    }


    // --- 3. The Core Manager (QuestDialogueManager) ---

    /// <summary>
    /// The central manager for all quest and dialogue operations.
    /// This follows the Manager/Service Locator pattern, providing a single point of access
    /// for game systems to interact with quests and dialogue.
    /// It uses UnityEvents to notify other systems (like UI) about state changes.
    /// </summary>
    public class QuestDialogueManager : MonoBehaviour
    {
        public static QuestDialogueManager Instance { get; private set; }

        // --- Public Events for other systems to subscribe to ---
        public UnityEvent<Quest> OnQuestAccepted;
        public UnityEvent<Quest, QuestObjective> OnQuestObjectiveCompleted;
        public UnityEvent<Quest> OnQuestCompleted;
        public UnityEvent<Quest> OnQuestFailed;

        public UnityEvent<DialogueNode> OnDialogueStarted;
        public UnityEvent<DialogueNode> OnDialogueNodeChanged;
        public UnityEvent OnDialogueEnded;
        public UnityEvent<DialogueChoice> OnDialogueChoiceMade;


        // --- Internal State ---
        private List<Quest> activeQuests = new List<Quest>();
        private List<Quest> completedQuests = new List<Quest>();
        private List<Quest> failedQuests = new List<Quest>();
        private DialogueNode currentDialogueNode;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("QuestDialogueManager: Duplicate instance found, destroying this one.");
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Persist across scenes if needed.
            }
        }

        // --- Quest Management Methods ---

        /// <summary>
        /// Starts a new quest, adding it to the active quests list.
        /// </summary>
        /// <param name="quest">The Quest ScriptableObject to start.</param>
        public bool StartQuest(Quest quest)
        {
            if (quest == null || activeQuests.Contains(quest))
            {
                Debug.LogWarning($"Cannot start quest '{quest?.questName}'. It's null or already active.");
                return false;
            }

            // Check prerequisites
            if (quest.prerequisiteQuests != null)
            {
                foreach (var prereq in quest.prerequisiteQuests)
                {
                    if (!completedQuests.Contains(prereq))
                    {
                        Debug.LogWarning($"Cannot start quest '{quest.questName}'. Prerequisite '{prereq.questName}' not completed.");
                        return false;
                    }
                }
            }

            quest.InitializeQuest(); // Ensure quest state is reset/initialized
            quest.CurrentState = QuestState.Active;
            activeQuests.Add(quest);
            quest.onQuestAccepted?.Invoke(); // Invoke specific quest events
            OnQuestAccepted?.Invoke(quest); // Invoke general manager event
            Debug.Log($"Quest '{quest.questName}' accepted!");
            return true;
        }

        /// <summary>
        /// Marks a specific objective within an active quest as completed.
        /// </summary>
        /// <param name="quest">The quest containing the objective.</param>
        /// <param name="objectiveToComplete">The specific objective to mark as completed.</param>
        public void AdvanceQuestObjective(Quest quest, QuestObjective objectiveToComplete)
        {
            if (quest == null || !activeQuests.Contains(quest) || quest.CurrentState != QuestState.Active)
            {
                Debug.LogWarning($"Cannot advance objective for quest '{quest?.questName}'. Quest not active or null.");
                return;
            }

            foreach (var obj in quest.objectives)
            {
                if (obj == objectiveToComplete && !obj.isCompleted)
                {
                    obj.Complete();
                    OnQuestObjectiveCompleted?.Invoke(quest, obj);
                    Debug.Log($"Objective '{obj.objectiveName}' for quest '{quest.questName}' completed!");

                    // Check if quest can now be completed
                    if (quest.AreAllObjectivesCompleted())
                    {
                        CompleteQuest(quest);
                    }
                    return;
                }
            }
            Debug.LogWarning($"Objective '{objectiveToComplete?.objectiveName}' not found or already completed in quest '{quest.questName}'.");
        }

        /// <summary>
        /// Completes an active quest.
        /// </summary>
        /// <param name="quest">The quest to complete.</param>
        public void CompleteQuest(Quest quest)
        {
            if (quest == null || !activeQuests.Contains(quest) || quest.CurrentState != QuestState.Active)
            {
                Debug.LogWarning($"Cannot complete quest '{quest?.questName}'. Quest not active or null.");
                return;
            }

            if (!quest.AreAllObjectivesCompleted())
            {
                Debug.LogWarning($"Quest '{quest.questName}' cannot be completed yet. Not all objectives are finished.");
                return;
            }

            quest.CurrentState = QuestState.Completed;
            activeQuests.Remove(quest);
            completedQuests.Add(quest);

            quest.onQuestCompleted?.Invoke();
            OnQuestCompleted?.Invoke(quest);
            Debug.Log($"Quest '{quest.questName}' COMPLETED!");

            // Unlock reward quests
            if (quest.rewardQuests != null)
            {
                foreach (var rewardQuest in quest.rewardQuests)
                {
                    // You might just make them 'available' rather than immediately 'start' them
                    // For this example, let's just log that they are unlocked.
                    Debug.Log($"Quest '{rewardQuest.questName}' is now available/unlocked!");
                    // Further logic to add to an 'available quests' pool could go here.
                }
            }
        }

        /// <summary>
        /// Fails an active quest.
        /// </summary>
        /// <param name="quest">The quest to fail.</param>
        public void FailQuest(Quest quest)
        {
            if (quest == null || !activeQuests.Contains(quest) || quest.CurrentState != QuestState.Active)
            {
                Debug.LogWarning($"Cannot fail quest '{quest?.questName}'. Quest not active or null.");
                return;
            }

            quest.CurrentState = QuestState.Failed;
            activeQuests.Remove(quest);
            failedQuests.Add(quest);
            OnQuestFailed?.Invoke(quest);
            Debug.Log($"Quest '{quest.questName}' FAILED!");
        }

        /// <summary>
        /// Checks the current state of a quest.
        /// </summary>
        public QuestState GetQuestState(Quest quest)
        {
            if (quest == null) return QuestState.NotStarted; // Or throw an error.

            if (activeQuests.Contains(quest)) return QuestState.Active;
            if (completedQuests.Contains(quest)) return QuestState.Completed;
            if (failedQuests.Contains(quest)) return QuestState.Failed;

            return QuestState.NotStarted;
        }

        public bool IsQuestActive(Quest quest) => GetQuestState(quest) == QuestState.Active;
        public bool IsQuestCompleted(Quest quest) => GetQuestState(quest) == QuestState.Completed;


        // --- Dialogue Management Methods ---

        /// <summary>
        /// Initiates a dialogue sequence starting from a specific node.
        /// </summary>
        /// <param name="startNode">The DialogueNode to begin the conversation with.</param>
        public void StartDialogue(DialogueNode startNode)
        {
            if (startNode == null)
            {
                Debug.LogWarning("Cannot start dialogue, startNode is null.");
                return;
            }

            currentDialogueNode = startNode;
            OnDialogueStarted?.Invoke(currentDialogueNode);
            currentDialogueNode.onNodeDisplayed?.Invoke(); // Trigger node-specific event
            Debug.Log($"Dialogue started with: {currentDialogueNode.speakerName}");
        }

        /// <summary>
        /// Player makes a choice in the current dialogue.
        /// </summary>
        /// <param name="choiceIndex">The index of the choice made by the player.</param>
        public void MakeDialogueChoice(int choiceIndex)
        {
            if (currentDialogueNode == null || !currentDialogueNode.HasChoices || choiceIndex < 0 || choiceIndex >= currentDialogueNode.choices.Count)
            {
                Debug.LogWarning("Invalid dialogue choice made or no active dialogue.");
                return;
            }

            DialogueChoice chosen = currentDialogueNode.choices[choiceIndex];
            OnDialogueChoiceMade?.Invoke(chosen); // Notify listeners about the choice
            chosen.onChoiceMade?.Invoke(); // Trigger choice-specific actions

            if (chosen.nextNode != null)
            {
                currentDialogueNode = chosen.nextNode;
                OnDialogueNodeChanged?.Invoke(currentDialogueNode);
                currentDialogueNode.onNodeDisplayed?.Invoke(); // Trigger node-specific event
                Debug.Log($"Dialogue advanced to: {currentDialogueNode.speakerName}");
            }
            else
            {
                // No next node, so dialogue ends.
                EndDialogue();
            }
        }

        /// <summary>
        /// Ends the current dialogue session.
        /// </summary>
        public void EndDialogue()
        {
            if (currentDialogueNode == null) return; // No active dialogue.

            currentDialogueNode = null;
            OnDialogueEnded?.Invoke();
            Debug.Log("Dialogue ended.");
        }
    }


    // --- 4. Example Usage: NPC Interaction (MonoBehaviour) ---

    /// <summary>
    /// An example MonoBehaviour that can be attached to an NPC GameObject.
    /// It demonstrates how an NPC can initiate dialogue, offer quests, and react to quest states.
    /// </summary>
    public class NPCInteraction : MonoBehaviour
    {
        [Header("Dialogue Settings")]
        [SerializeField] private DialogueNode initialDialogue;
        [SerializeField] private DialogueNode dialogueAfterQuestAccepted;
        [SerializeField] private DialogueNode dialogueQuestComplete;
        [SerializeField] private DialogueNode dialogueQuestInProgress;

        [Header("Quest Settings")]
        [SerializeField] private Quest questToOffer;
        [SerializeField] private QuestObjective objectiveToAdvanceOnInteract; // Optional: an objective this NPC interaction completes.

        private QuestDialogueManager manager;

        void Start()
        {
            manager = QuestDialogueManager.Instance;
            if (manager == null)
            {
                Debug.LogError("NPCInteraction: QuestDialogueManager not found in scene!");
                enabled = false;
            }
        }

        /// <summary>
        /// This method would typically be called when the player interacts with the NPC (e.g., clicks on them).
        /// </summary>
        public void Interact()
        {
            if (manager == null) return;

            QuestState questState = manager.GetQuestState(questToOffer);

            // 1. If there's an objective to advance, try to advance it.
            if (objectiveToAdvanceOnInteract != null && questToOffer != null && questState == QuestState.Active)
            {
                manager.AdvanceQuestObjective(questToOffer, objectiveToAdvanceOnInteract);
                if (questToOffer.AreAllObjectivesCompleted() && dialogueQuestComplete != null)
                {
                    // If advancing this objective completed the quest, start completion dialogue.
                    manager.StartDialogue(dialogueQuestComplete);
                    return;
                }
            }

            // 2. Based on quest state, determine dialogue and actions.
            switch (questState)
            {
                case QuestState.NotStarted:
                    if (initialDialogue != null)
                    {
                        manager.StartDialogue(initialDialogue);
                        // A common pattern is to have a choice in initialDialogue to accept the quest.
                        // For simplicity, we can also auto-accept here.
                        // manager.StartQuest(questToOffer); // Or let dialogue choice handle it.
                    }
                    break;
                case QuestState.Active:
                    if (questToOffer.AreAllObjectivesCompleted())
                    {
                        if (dialogueQuestComplete != null)
                            manager.StartDialogue(dialogueQuestComplete);
                        else if (dialogueAfterQuestAccepted != null)
                            manager.StartDialogue(dialogueAfterQuestAccepted); // Fallback if no specific complete dialogue
                        manager.CompleteQuest(questToOffer);
                    }
                    else
                    {
                        if (dialogueQuestInProgress != null)
                            manager.StartDialogue(dialogueQuestInProgress);
                        else if (dialogueAfterQuestAccepted != null)
                            manager.StartDialogue(dialogueAfterQuestAccepted); // Fallback
                        else
                            manager.StartDialogue(initialDialogue); // Fallback
                    }
                    break;
                case QuestState.Completed:
                    if (dialogueQuestComplete != null)
                        manager.StartDialogue(dialogueQuestComplete); // Repeat completion dialogue
                    else
                        manager.StartDialogue(initialDialogue); // Generic "I've already helped you"
                    break;
                case QuestState.Failed:
                    Debug.Log("Quest failed, NPC might have different dialogue here.");
                    manager.StartDialogue(initialDialogue); // Or a specific 'failed quest' dialogue.
                    break;
            }
        }

        // Example of how to add an interaction trigger
        void OnMouseDown() // For 3D colliders
        {
            Interact();
        }

        // For 2D colliders:
        // void OnMouseDown2D() { Interact(); }
    }


    // --- 5. Example Usage: Dialogue UI Controller (MonoBehaviour) ---

    /// <summary>
    /// An example MonoBehaviour responsible for displaying dialogue and choices on the UI.
    /// It subscribes to events from the QuestDialogueManager.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI dialogueText;
        public GameObject choicesPanel;
        public Button[] choiceButtons; // Assign these in the Inspector

        private QuestDialogueManager manager;

        void Start()
        {
            manager = QuestDialogueManager.Instance;
            if (manager == null)
            {
                Debug.LogError("DialogueUIController: QuestDialogueManager not found in scene!");
                enabled = false;
                return;
            }

            // Subscribe to dialogue events
            manager.OnDialogueStarted.AddListener(DisplayDialogue);
            manager.OnDialogueNodeChanged.AddListener(DisplayDialogue);
            manager.OnDialogueEnded.AddListener(HideDialogue);

            HideDialogue(); // Start with UI hidden
        }

        void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnDialogueStarted.RemoveListener(DisplayDialogue);
                manager.OnDialogueNodeChanged.RemoveListener(DisplayDialogue);
                manager.OnDialogueEnded.RemoveListener(HideDialogue);
            }
        }

        /// <summary>
        /// Displays the current dialogue node's information.
        /// </summary>
        /// <param name="node">The DialogueNode to display.</param>
        private void DisplayDialogue(DialogueNode node)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerText != null) speakerText.text = node.speakerName;
            if (dialogueText != null) dialogueText.text = node.dialogueText;

            DisplayChoices(node);
        }

        /// <summary>
        /// Displays the choices for the current dialogue node.
        /// </summary>
        /// <param name="node">The DialogueNode containing choices.</param>
        private void DisplayChoices(DialogueNode node)
        {
            if (choicesPanel != null) choicesPanel.SetActive(true);

            // Hide all choice buttons first
            foreach (Button button in choiceButtons)
            {
                button.gameObject.SetActive(false);
            }

            if (node.HasChoices)
            {
                for (int i = 0; i < node.choices.Count; i++)
                {
                    if (i < choiceButtons.Length)
                    {
                        Button button = choiceButtons[i];
                        button.gameObject.SetActive(true);
                        button.GetComponentInChildren<TextMeshProUGUI>().text = node.choices[i].choiceText;

                        int choiceIndex = i; // Local copy for closure
                        button.onClick.RemoveAllListeners(); // Clear previous listeners
                        button.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
                    }
                    else
                    {
                        Debug.LogWarning($"Too many choices for dialogue node '{node.name}'. Not enough UI buttons.");
                        break;
                    }
                }
            }
            else
            {
                // If no choices, create a "Continue" or "End" button
                if (choiceButtons.Length > 0)
                {
                    Button continueButton = choiceButtons[0];
                    continueButton.gameObject.SetActive(true);
                    continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue / End";
                    continueButton.onClick.RemoveAllListeners();
                    continueButton.onClick.AddListener(() => manager.EndDialogue());
                }
                if (choicesPanel != null) choicesPanel.SetActive(false); // Can hide choice panel if it's just a "Continue" button
            }
        }

        /// <summary>
        /// Handler for when a choice button is clicked.
        /// </summary>
        /// <param name="choiceIndex">The index of the choice selected.</param>
        private void OnChoiceButtonClicked(int choiceIndex)
        {
            manager.MakeDialogueChoice(choiceIndex);
        }

        /// <summary>
        /// Hides the dialogue UI.
        /// </summary>
        private void HideDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicesPanel != null) choicesPanel.SetActive(false);
        }
    }
}
```