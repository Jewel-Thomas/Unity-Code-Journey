// Unity Design Pattern Example: SmartDialogueChoices
// This script demonstrates the SmartDialogueChoices pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **SmartDialogueChoices** design pattern in Unity. This pattern focuses on creating dialogue systems where the available choices are dynamic, context-aware, and lead to various game state changes or dialogue flows.

**Key Principles of Smart Dialogue Choices:**

1.  **Context-Aware Conditions:** Dialogue choices are not static. Their availability depends on the current game state (player level, inventory, quest status, character relationships, etc.).
2.  **Impactful Actions:** Selecting a choice triggers specific actions that modify the game state, influencing subsequent dialogue or gameplay.
3.  **Flexible Flow Control:** Choices can lead to different dialogue nodes, end the dialogue, or even trigger other game events.
4.  **Editor-Friendly Design:** Using ScriptableObjects and `[SerializeReference]` allows designers to create and link dialogue nodes, conditions, and actions directly in the Unity editor without writing code.

---

### **How to Use This Example in Unity:**

1.  **Create C# Script:** Create a new C# script named `SmartDialogueChoicesExample` and copy-paste the entire code below into it.
2.  **Create GameObjects:**
    *   Create an Empty GameObject in your scene named `DialogueManager`.
    *   Attach the `SmartDialogueChoicesExample` script to this `DialogueManager` GameObject.
3.  **Create ScriptableObjects (Assets):**
    *   In your Project window, right-click -> Create -> Dialogue -> Game Context. Name it `MyGameContext`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `StartNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `QuestAcceptNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `QuestDeclineNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `BargainSuccessNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `BargainFailNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `AlreadyAcceptedNode`.
    *   Right-click -> Create -> Dialogue -> Dialogue Node. Name it `EndDialogueNode`.
4.  **Configure `DialogueManager`:**
    *   Select the `DialogueManager` GameObject in your scene.
    *   Drag your `MyGameContext` asset into the `Game Context` field.
    *   Drag your `StartNode` asset into the `Initial Test Dialogue` field.
5.  **Configure `MyGameContext`:**
    *   Select your `MyGameContext` asset. You can adjust `Player Level`, `Has Bargaining Skill`, `Gold`, and `Quest Statuses` here to test different scenarios.
6.  **Configure Dialogue Nodes (Crucial for the Pattern!):**
    *   **`StartNode`:**
        *   `Speaker Name`: `Shopkeeper`
        *   `Dialogue Text`: `Greetings, adventurer! I have a task that might interest you.`
        *   **Choices (add 4 choices):**
            *   **Choice 0 (Accept Quest):**
                *   `Choice Text`: `Tell me about this task. (Accept Quest)`
                *   `Conditions`: (Leave empty - always available)
                *   `Actions`:
                    *   `Set Quest Status Action`: `Quest Id` = `MainQuest`, `New Status` = `Accepted`
                    *   `Load Next Node Action`
                *   `Next Node`: Drag `QuestAcceptNode` here.
            *   **Choice 1 (Decline Quest):**
                *   `Choice Text`: `No thanks, I'm busy. (Decline Quest)`
                *   `Conditions`: (Leave empty - always available)
                *   `Actions`:
                    *   `Set Quest Status Action`: `Quest Id` = `MainQuest`, `New Status` = `Failed`
                    *   `End Dialogue Action`
                *   `Next Node`: (Leave null, `End Dialogue Action` handles it)
            *   **Choice 2 (Bargain for More Gold - Smart Choice!):**
                *   `Choice Text`: `I'll do it, but only if you pay more! (Bargain)`
                *   `Conditions`:
                    *   `Has Bargaining Skill Condition`
                *   `Actions`:
                    *   `Add Gold Action`: `Amount` = `50`
                    *   `Load Next Node Action`
                *   `Next Node`: Drag `BargainSuccessNode` here.
            *   **Choice 3 (Already Accepted Quest - Smart Choice!):**
                *   `Choice Text`: `Wait, I think I already accepted this quest...`
                *   `Conditions`:
                    *   `Quest Status Condition`: `Quest Id` = `MainQuest`, `Required Status` = `Accepted`
                *   `Actions`:
                    *   `Load Next Node Action`
                *   `Next Node`: Drag `AlreadyAcceptedNode` here.
    *   **`QuestAcceptNode`:**
        *   `Speaker Name`: `Shopkeeper`
        *   `Dialogue Text`: `Excellent! The main quest is now yours. Report back when you're done.`
        *   `Default Next Node`: Drag `EndDialogueNode` here (or leave null if you want it to implicitly end).
        *   `Choices`: (Leave empty for now, or add a simple "Goodbye" choice)
    *   **`QuestDeclineNode`:**
        *   `Speaker Name`: `Shopkeeper`
        *   `Dialogue Text`: `Pity. Perhaps another time, then.`
        *   `Default Next Node`: Drag `EndDialogueNode` here.
        *   `Choices`: (Leave empty)
    *   **`BargainSuccessNode`:**
        *   `Speaker Name`: `Shopkeeper`
        *   `Dialogue Text`: `Hmph, fine. You drive a hard bargain. Here's an extra 50 gold.`
        *   `Default Next Node`: Drag `QuestAcceptNode` here (or a new node for successful bargain).
        *   `Choices`: (Leave empty)
    *   **`AlreadyAcceptedNode`:**
        *   `Speaker Name`: `Shopkeeper`
        *   `Dialogue Text`: `Ah, right! My apologies, I forgot. You're already on it. Carry on!`
        *   `Default Next Node`: Drag `EndDialogueNode` here.
        *   `Choices`: (Leave empty)
    *   **`EndDialogueNode`:**
        *   `Speaker Name`: `Narrator`
        *   `Dialogue Text`: `(Dialogue ends.)`
        *   `Choices`: (Leave empty)
        *   `Default Next Node`: (Leave null)
7.  **Run the Scene:**
    *   Press Play in Unity.
    *   Press `Space` to start the dialogue.
    *   Observe the console output (and imagine a UI showing the text and buttons).
    *   Press `0`, `1`, `2`, `3` to make choices.
    *   Experiment:
        *   Start dialogue.
        *   Reset `MyGameContext` (`R` key).
        *   Change `Has Bargaining Skill` to `true` on `MyGameContext` and restart dialogue (`Space`). Notice the Bargain choice appears!
        *   Accept the quest, then restart dialogue. Notice the "Already accepted" choice appears.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extension methods like .All() and .Where()

/// <summary>
/// GameContext ScriptableObject: Holds the global game state that dialogue conditions might check
/// or dialogue actions might modify. This is crucial for making choices "smart" and dynamic.
/// In a real game, this might be a more complex system with dedicated managers for inventory, quests, player stats.
/// </summary>
[CreateAssetMenu(fileName = "GameContext", menuName = "Dialogue/Game Context")]
public class GameContext : ScriptableObject
{
    [Header("Player State")]
    public int playerLevel = 1;
    public bool hasBargainingSkill = false;
    public int gold = 100;

    [Header("Quest States")]
    // Using an Enum for QuestId and storing states directly for simplicity.
    // In larger projects, a Dictionary<QuestId, QuestState> or a dedicated QuestManager would be better.
    public QuestState mainQuestStatus = QuestState.NotStarted;
    public QuestState sideQuest1Status = QuestState.NotStarted;

    public enum QuestId { MainQuest, SideQuest1 }
    public enum QuestState { NotStarted, Accepted, Completed, Failed }

    /// <summary>
    /// Retrieves the current state of a specified quest.
    /// </summary>
    public QuestState GetQuestState(QuestId id)
    {
        switch (id)
        {
            case QuestId.MainQuest: return mainQuestStatus;
            case QuestId.SideQuest1: return sideQuest1Status;
            default: return QuestState.NotStarted;
        }
    }

    /// <summary>
    /// Sets the state of a specified quest.
    /// </summary>
    public void SetQuestState(QuestId id, QuestState newState)
    {
        switch (id)
        {
            case QuestId.MainQuest: mainQuestStatus = newState; break;
            case QuestId.SideQuest1: sideQuest1Status = newState; break;
        }
        Debug.Log($"[GameContext] Quest '{id}' status changed to: {newState}");
    }

    /// <summary>
    /// Adds gold to the player's inventory.
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[GameContext] Added {amount} gold. Total gold: {gold}");
    }

    /// <summary>
    /// Removes gold from the player's inventory.
    /// </summary>
    public void RemoveGold(int amount)
    {
        gold -= amount;
        Debug.Log($"[GameContext] Removed {amount} gold. Total gold: {gold}");
    }

    /// <summary>
    /// Sets the player's bargaining skill status.
    /// </summary>
    public void SetBargainingSkill(bool value)
    {
        hasBargainingSkill = value;
        Debug.Log($"[GameContext] Bargaining skill set to: {value}");
    }

    /// <summary>
    /// Resets all game context values to their initial state. Useful for testing.
    /// </summary>
    public void ResetContext()
    {
        playerLevel = 1;
        hasBargainingSkill = false;
        gold = 100;
        mainQuestStatus = QuestState.NotStarted;
        sideQuest1Status = QuestState.NotStarted;
        Debug.Log("[GameContext] All states reset to initial values.");
    }
}

// --- Interfaces for Smart Dialogue Choices ---
// These interfaces define the contract for conditions (what makes a choice available)
// and actions (what happens when a choice is made).
// The [SerializeReference] attribute (Unity 2019.3+) is critical here. It allows Unity to serialize
// concrete classes that implement these interfaces directly in the inspector, making them editable.

/// <summary>
/// IDialogueCondition: Interface for defining conditions that determine if a dialogue choice is available.
/// All concrete condition classes must be [Serializable].
/// </summary>
public interface IDialogueCondition
{
    string Description { get; } // For displaying helpful text in the editor
    bool IsMet(GameContext context);
}

/// <summary>
/// IDialogueAction: Interface for defining actions to be executed when a dialogue choice is made.
/// All concrete action classes must be [Serializable].
/// </summary>
public interface IDialogueAction
{
    string Description { get; } // For displaying helpful text in the editor
    void Execute(GameContext context, DialogueManager manager);
}

// --- Concrete Condition Implementations ---
// These classes implement IDialogueCondition and provide specific checks against the GameContext.

[Serializable] // Required for [SerializeReference]
public class PlayerLevelCondition : IDialogueCondition
{
    public int requiredLevel;
    public string Description => $"Player Level >= {requiredLevel}";
    public bool IsMet(GameContext context) => context.playerLevel >= requiredLevel;
}

[Serializable]
public class HasBargainingSkillCondition : IDialogueCondition
{
    public string Description => $"Has Bargaining Skill";
    public bool IsMet(GameContext context) => context.hasBargainingSkill;
}

[Serializable]
public class QuestStatusCondition : IDialogueCondition
{
    public GameContext.QuestId questId;
    public GameContext.QuestState requiredStatus;
    public string Description => $"Quest '{questId}' is '{requiredStatus}'";
    public bool IsMet(GameContext context) => context.GetQuestState(questId) == requiredStatus;
}

[Serializable]
public class GoldAmountCondition : IDialogueCondition
{
    public int requiredGold;
    [Tooltip("If true, requires gold >= amount. If false, requires gold < amount.")]
    public bool greaterThanOrEqual;
    public string Description => $"Player Gold {(greaterThanOrEqual ? ">=" : "<")} {requiredGold}";
    public bool IsMet(GameContext context) => greaterThanOrEqual ? context.gold >= requiredGold : context.gold < requiredGold;
}

// --- Concrete Action Implementations ---
// These classes implement IDialogueAction and perform specific modifications to the GameContext or dialogue flow.

[Serializable] // Required for [SerializeReference]
public class SetQuestStatusAction : IDialogueAction
{
    public GameContext.QuestId questId;
    public GameContext.QuestState newStatus;
    public string Description => $"Set Quest '{questId}' to '{newStatus}'";
    public void Execute(GameContext context, DialogueManager manager) => context.SetQuestState(questId, newStatus);
}

[Serializable]
public class AddGoldAction : IDialogueAction
{
    public int amount;
    public string Description => $"Add {amount} Gold";
    public void Execute(GameContext context, DialogueManager manager) => context.AddGold(amount);
}

[Serializable]
public class RemoveGoldAction : IDialogueAction
{
    public int amount;
    public string Description => $"Remove {amount} Gold";
    public void Execute(GameContext context, DialogueManager manager) => context.RemoveGold(amount);
}

[Serializable]
public class SetBargainingSkillAction : IDialogueAction
{
    public bool value;
    public string Description => $"Set Bargaining Skill to {value}";
    public void Execute(GameContext context, DialogueManager manager) => context.SetBargainingSkill(value);
}

[Serializable]
public class EndDialogueAction : IDialogueAction
{
    public string Description => "End Dialogue";
    public void Execute(GameContext context, DialogueManager manager) => manager.EndDialogue();
}

[Serializable]
public class LoadNextNodeAction : IDialogueAction
{
    // This action typically doesn't need to do anything itself, as the DialogueChoice's 'nextNode' field
    // or the DialogueManager's 'defaultNextNode' handles the transition. It serves as a semantic indicator
    // that the dialogue continues to the next node.
    public string Description => "Continue to next node defined by choice/default";
    public void Execute(GameContext context, DialogueManager manager) { /* DialogueManager handles nextNode transition */ }
}


/// <summary>
/// DialogueChoice: Represents a single option a player can select in a dialogue.
/// It contains the display text, a list of conditions (all of which must be met for the choice to appear),
/// a list of actions (executed when the choice is picked), and a reference to the next DialogueNode.
/// </summary>
[Serializable]
public class DialogueChoice
{
    public string choiceText;

    [Tooltip("All conditions in this list must be met for this choice to be available.")]
    [SerializeReference] // Allows serializing interface implementations in the inspector
    public List<IDialogueCondition> conditions = new List<IDialogueCondition>();

    [Tooltip("All actions in this list will be executed when this choice is made.")]
    [SerializeReference] // Allows serializing interface implementations in the inspector
    public List<IDialogueAction> actions = new List<IDialogueAction>();

    [Tooltip("The next dialogue node to proceed to after this choice. Can be null if an action ends dialogue or dialogue flows linearly.")]
    public DialogueNode nextNode;

    /// <summary>
    /// Checks if this choice is available based on the current game context.
    /// All conditions in its list must evaluate to true. If no conditions, it's always available.
    /// </summary>
    public bool IsAvailable(GameContext context)
    {
        // If there are no conditions, the choice is always available.
        if (conditions == null || conditions.Count == 0)
        {
            return true;
        }

        // All conditions must be met for the choice to be available.
        return conditions.All(condition => condition.IsMet(context));
    }

    /// <summary>
    /// Executes all actions associated with this choice.
    /// </summary>
    public void ExecuteActions(GameContext context, DialogueManager manager)
    {
        if (actions != null)
        {
            foreach (var action in actions)
            {
                action.Execute(context, manager);
            }
        }
    }
}

/// <summary>
/// DialogueNode ScriptableObject: Represents a single point in a dialogue tree.
/// It defines the speaker, the dialogue text, and a list of possible choices.
/// Using ScriptableObjects allows designers to create and link dialogue nodes in the Unity editor,
/// building complex dialogue trees visually without code.
/// </summary>
[CreateAssetMenu(fileName = "DialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    public string speakerName = "NPC";
    [TextArea(3, 10)]
    public string dialogueText = "Hello, adventurer! What can I do for you?";

    [Tooltip("The list of choices presented to the player from this node.")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Tooltip("The node to automatically transition to if no specific choice leads to a node, or if there are no available choices.")]
    public DialogueNode defaultNextNode;
}


/// <summary>
/// DialogueManager MonoBehaviour: The central component that orchestrates the dialogue flow.
/// It's a singleton, manages the current dialogue node, filters choices based on game context,
/// and provides events for UI systems to subscribe to.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // Singleton pattern for easy access from other scripts.
    public static DialogueManager Instance { get; private set; }

    [Tooltip("Reference to the global GameContext ScriptableObject.")]
    public GameContext gameContext;

    // --- Events for UI and other game systems to subscribe to ---
    // These events provide data that a UI can use to display dialogue and choices.
    public event Action<DialogueNode> OnDialogueStarted; // Triggered when a dialogue begins.
    public event Action<DialogueNode, List<DialogueChoice>> OnDialogueNodeUpdated; // Triggered when the current node or available choices change.
    public event Action OnDialogueEnded; // Triggered when the dialogue concludes.

    private DialogueNode currentDialogueNode;
    private List<DialogueChoice> currentAvailableChoices; // Stores the choices currently available to the player.

    void Awake()
    {
        // Implement the singleton pattern.
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the line below if you want the DialogueManager to persist across scenes.
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances.
        }

        // Basic validation.
        if (gameContext == null)
        {
            Debug.LogError("GameContext is not assigned in DialogueManager! Please assign a GameContext ScriptableObject asset.", this);
        }
    }

    /// <summary>
    /// Initiates a dialogue sequence starting from a specified DialogueNode.
    /// </summary>
    /// <param name="initialNode">The DialogueNode where the conversation begins.</param>
    public void StartDialogue(DialogueNode initialNode)
    {
        if (initialNode == null)
        {
            Debug.LogError("Cannot start dialogue: initial node is null.", this);
            return;
        }

        currentDialogueNode = initialNode;
        OnDialogueStarted?.Invoke(currentDialogueNode); // Notify subscribers that dialogue has started.
        Debug.Log($"[DialogueManager] Dialogue Started. Initial Node: '{currentDialogueNode.name}'");
        DisplayCurrentNode(); // Process and display the initial node.
    }

    /// <summary>
    /// Terminates the current dialogue sequence.
    /// </summary>
    public void EndDialogue()
    {
        if (currentDialogueNode == null) return; // Dialogue already ended

        currentDialogueNode = null;
        currentAvailableChoices = null;
        OnDialogueEnded?.Invoke(); // Notify subscribers that dialogue has ended.
        Debug.Log("[DialogueManager] Dialogue Ended.");
    }

    /// <summary>
    /// Processes the player's selection of a dialogue choice.
    /// This method is typically called by a UI element (e.g., a button) when a choice is clicked.
    /// </summary>
    /// <param name="choiceIndex">The index of the chosen option within the currently available choices list.</param>
    public void MakeChoice(int choiceIndex)
    {
        if (currentDialogueNode == null || currentAvailableChoices == null || choiceIndex < 0 || choiceIndex >= currentAvailableChoices.Count)
        {
            Debug.LogWarning($"[DialogueManager] Invalid choice index {choiceIndex} or no dialogue active.");
            return;
        }

        DialogueChoice chosenOption = currentAvailableChoices[choiceIndex];
        Debug.Log($"[DialogueManager] Player chose: '{chosenOption.choiceText}'");

        // 1. Execute all actions associated with the chosen option.
        chosenOption.ExecuteActions(gameContext, this);

        // Check if an action (like EndDialogueAction) has already ended the dialogue.
        if (currentDialogueNode == null)
        {
            return; // Dialogue already ended by an action.
        }

        // 2. Determine the next dialogue node.
        DialogueNode nextNode = chosenOption.nextNode;

        // Fallback: If the choice doesn't explicitly define a next node, check the current node's default.
        if (nextNode == null && currentDialogueNode.defaultNextNode != null)
        {
            nextNode = currentDialogueNode.defaultNextNode;
        }

        // 3. Transition to the next node or end dialogue.
        if (nextNode != null)
        {
            currentDialogueNode = nextNode;
            DisplayCurrentNode(); // Display the new node and its choices.
        }
        else // No next node found (neither from choice nor default), so the dialogue implicitly ends.
        {
            Debug.Log("[DialogueManager] No next node specified. Ending dialogue implicitly.");
            EndDialogue();
        }
    }

    /// <summary>
    /// Calculates which choices are available based on current game context and updates the UI subscribers.
    /// This is where the 'Smart' filtering of choices happens.
    /// </summary>
    private void DisplayCurrentNode()
    {
        if (currentDialogueNode == null)
        {
            EndDialogue(); // Ensure dialogue is properly ended if current node somehow became null.
            return;
        }

        // Filter the choices: only include those whose conditions are met by the current game context.
        currentAvailableChoices = currentDialogueNode.choices
            .Where(choice => choice.IsAvailable(gameContext))
            .ToList();

        // Handle scenarios where no choices are available:
        if (currentAvailableChoices.Count == 0)
        {
            if (currentDialogueNode.defaultNextNode != null)
            {
                Debug.Log($"[DialogueManager] No choices available for '{currentDialogueNode.name}'. Proceeding to default next node: '{currentDialogueNode.defaultNextNode.name}'.");
                currentDialogueNode = currentDialogueNode.defaultNextNode;
                DisplayCurrentNode(); // Recursively call to display the default next node.
                return;
            }
            else
            {
                Debug.Log($"[DialogueManager] No choices available for '{currentDialogueNode.name}' and no default next node. Ending dialogue.");
                EndDialogue(); // End dialogue if no choices and no default next node.
                return;
            }
        }

        // Notify UI and other systems about the updated dialogue node and its available choices.
        OnDialogueNodeUpdated?.Invoke(currentDialogueNode, currentAvailableChoices);

        // --- Console Output for Debugging / Demonstration ---
        Debug.Log($"\n--- Dialogue Node Updated ---");
        Debug.Log($"Speaker: {currentDialogueNode.speakerName}");
        Debug.Log($"Text: {currentDialogueNode.dialogueText}");
        Debug.Log($"Available Choices ({currentAvailableChoices.Count}):");
        for (int i = 0; i < currentAvailableChoices.Count; i++)
        {
            // Also show the conditions for testing, though a real UI wouldn't expose this directly.
            string conditionDebug = currentAvailableChoices[i].conditions.Any() 
                                    ? " [Requires: " + string.Join(", ", currentAvailableChoices[i].conditions.Select(c => c.Description)) + "]"
                                    : "";
            Debug.Log($"  {i}. {currentAvailableChoices[i].choiceText}{conditionDebug}");
        }
        Debug.Log("-----------------------------");
    }

    // --- Example Usage / Simulation of UI Interaction ---
    [Header("Testing & Debugging")]
    [Tooltip("Drag the initial DialogueNode ScriptableObject here to start a test dialogue.")]
    public DialogueNode initialTestDialogue;
    public KeyCode startDialogueKey = KeyCode.Space;
    public KeyCode resetGameContextKey = KeyCode.R;

    void Update()
    {
        // Simulate starting a new dialogue.
        if (Input.GetKeyDown(startDialogueKey))
        {
            StartDialogue(initialTestDialogue);
        }

        // Simulate resetting game context (e.g., player stats changed, new game).
        if (Input.GetKeyDown(resetGameContextKey))
        {
            if (gameContext != null)
            {
                gameContext.ResetContext();
                // If dialogue is active, re-evaluate and display choices based on the new context.
                if (currentDialogueNode != null)
                {
                    Debug.Log("[DialogueManager] Game context reset. Re-evaluating current dialogue node choices.");
                    DisplayCurrentNode();
                }
            }
            else
            {
                Debug.LogWarning("Cannot reset GameContext: GameContext is not assigned.");
            }
        }

        // Simulate choice selection using number keys (0-9).
        if (currentDialogueNode != null && currentAvailableChoices != null)
        {
            for (int i = 0; i < currentAvailableChoices.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    MakeChoice(i);
                    break;
                }
            }
        }
    }

    // --- Example of a simple UI script that would subscribe to DialogueManager events ---
    /*
    // To use this, create a new C# script (e.g., DialogueUI.cs), attach it to a UI panel,
    // and set up TextMeshProUGUI components for speaker name, dialogue text, and a parent for choice buttons.
    // Ensure you have TextMeshPro installed (Window > TextMeshPro > Import TMP Essential Resources).

    using UnityEngine.UI;
    using TMPro; // For TextMeshProUGUI

    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        public GameObject choiceButtonPrefab; // A prefab containing a Button and a TextMeshProUGUI
        public Transform choiceButtonParent; // The parent transform where choice buttons will be instantiated

        void Start()
        {
            // Ensure the panel is hidden initially
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        void OnEnable()
        {
            // Subscribe to DialogueManager events when this UI script becomes active.
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted += ShowDialogueUI;
                DialogueManager.Instance.OnDialogueNodeUpdated += UpdateDialogueContent;
                DialogueManager.Instance.OnDialogueEnded += HideDialogueUI;
            }
            else
            {
                Debug.LogError("DialogueManager.Instance is null. Is it in the scene and initialized?");
            }
        }

        void OnDisable()
        {
            // Unsubscribe from DialogueManager events to prevent memory leaks or errors
            // if the DialogueManager is destroyed before this UI script.
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted -= ShowDialogueUI;
                DialogueManager.Instance.OnDialogueNodeUpdated -= UpdateDialogueContent;
                DialogueManager.Instance.OnDialogueEnded -= HideDialogueUI;
            }
        }

        // Event handler for OnDialogueStarted
        void ShowDialogueUI(DialogueNode node)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            // Optionally, clear content or show a loading state before UpdateDialogueContent fires.
        }

        // Event handler for OnDialogueNodeUpdated
        void UpdateDialogueContent(DialogueNode node, List<DialogueChoice> availableChoices)
        {
            if (speakerNameText != null) speakerNameText.text = node.speakerName;
            if (dialogueText != null) dialogueText.text = node.dialogueText;

            // Clear any previously created choice buttons
            foreach (Transform child in choiceButtonParent)
            {
                Destroy(child.gameObject);
            }

            // Create buttons for each available choice
            if (choiceButtonPrefab != null && choiceButtonParent != null)
            {
                for (int i = 0; i < availableChoices.Count; i++)
                {
                    DialogueChoice choice = availableChoices[i];
                    GameObject buttonGO = Instantiate(choiceButtonPrefab, choiceButtonParent);

                    // Find TextMeshProUGUI component in children (assuming button text is a child)
                    TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = choice.choiceText;
                    }

                    // Get the Button component and add a listener
                    Button buttonComponent = buttonGO.GetComponent<Button>();
                    if (buttonComponent != null)
                    {
                        int choiceIndex = i; // Local copy for closure
                        buttonComponent.onClick.AddListener(() => DialogueManager.Instance.MakeChoice(choiceIndex));
                    }
                }
            }
        }

        // Event handler for OnDialogueEnded
        void HideDialogueUI()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            // Clear all text and buttons when dialogue ends
            if (speakerNameText != null) speakerNameText.text = "";
            if (dialogueText != null) dialogueText.text = "";
            foreach (Transform child in choiceButtonParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
    */
}
```