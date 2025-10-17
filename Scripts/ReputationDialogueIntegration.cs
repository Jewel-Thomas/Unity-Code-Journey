// Unity Design Pattern Example: ReputationDialogueIntegration
// This script demonstrates the ReputationDialogueIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **ReputationDialogueIntegration** design pattern. The pattern focuses on how a player's reputation influences dialogue options and outcomes, and conversely, how dialogue choices can modify the player's reputation.

This script is designed to be self-contained and ready to drop into a Unity project. It includes:
*   A simplified `ReputationManager` to track reputation with different factions.
*   A simplified `DialogueManager` to handle dialogue flow and display (using `Debug.Log` for simplicity).
*   Data structures for dialogue nodes, choices, reputation effects, and requirements.
*   Detailed comments explaining each part of the pattern and its implementation.
*   An example dialogue sequence demonstrating the integration.

To use it:
1.  Create a new C# script named `ReputationDialogueSystemExample.cs` in your Unity project.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., "GameManager").
4.  Attach the `ReputationDialogueSystemExample.cs` script to the "GameManager" GameObject.
5.  Run the scene. Observe the dialogue flow and reputation changes in the Unity Console.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // Required for LINQ operations like All()

// This script demonstrates the 'ReputationDialogueIntegration' design pattern in Unity.
// It integrates a simplified Reputation System with a Dialogue System into a single,
// self-contained example for easy understanding and usage in a Unity project.

// The core idea of this pattern is:
// 1. Dialogue choices can have an impact on the player's reputation (e.g., with factions, NPCs).
// 2. The player's current reputation can influence which dialogue options are available,
//    or how NPCs react to the player.

public class ReputationDialogueSystemExample : MonoBehaviour
{
    // --- PART 1: Reputation System (ReputationManager) ---
    // This section manages the player's reputation values with various factions or entities.
    // It's designed as a simple singleton for easy global access.

    #region ReputationManager

    [Header("Reputation Settings")]
    [Tooltip("Initial reputation values for different factions. Can be set in the Inspector.")]
    [SerializeField]
    private Dictionary<string, int> _initialReputations = new Dictionary<string, int>()
    {
        { "Townsfolk", 0 },
        { "Merchants", 0 },
        { "Guards", 0 }
    };

    private Dictionary<string, int> _currentReputations;

    // Singleton instance: Provides a global point of access to the Reputation/Dialogue System.
    public static ReputationDialogueSystemExample Instance { get; private set; }

    // Event: Fired whenever a reputation value changes. Other systems (like UI) can subscribe to this.
    public static event Action<string, int> OnReputationChanged;

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            InitializeReputations();
        }
    }

    // Initializes the current reputation dictionary from the inspector-set initial values.
    private void InitializeReputations()
    {
        _currentReputations = new Dictionary<string, int>(_initialReputations);
        Debug.Log("--- Reputation Manager Initialized ---");
        DisplayAllReputations(); // Show initial state in console
    }

    /// <summary>
    /// Retrieves the current reputation value for a specified faction.
    /// </summary>
    /// <param name="factionName">The name of the faction (e.g., "Guards").</param>
    /// <returns>The current reputation score, or 0 if the faction doesn't exist.</returns>
    public int GetReputation(string factionName)
    {
        if (_currentReputations.TryGetValue(factionName, out int rep))
        {
            return rep;
        }
        Debug.LogWarning($"Reputation for faction '{factionName}' not found. Returning 0.");
        return 0;
    }

    /// <summary>
    /// Modifies the reputation for a specific faction by a given amount.
    /// This is a key integration point: dialogue choices will call this method.
    /// </summary>
    /// <param name="factionName">The name of the faction whose reputation to change.</param>
    /// <param name="amount">The value to add to the reputation (positive to increase, negative to decrease).</param>
    public void ChangeReputation(string factionName, int amount)
    {
        if (_currentReputations.ContainsKey(factionName))
        {
            _currentReputations[factionName] += amount;
            Debug.Log($"<color=cyan>Reputation for '{factionName}' changed by {amount}. New value: {_currentReputations[factionName]}</color>");
            OnReputationChanged?.Invoke(factionName, _currentReputations[factionName]); // Notify subscribers
            DisplayAllReputations(); // Update debug display
        }
        else
        {
            Debug.LogWarning($"Attempted to change reputation for unknown faction: '{factionName}'.");
        }
    }

    /// <summary>
    /// Checks if a given reputation requirement is met based on the player's current reputation.
    /// This is crucial for reputation-gated dialogue options, allowing choices to appear
    /// or be enabled only if certain reputation conditions are met.
    /// </summary>
    /// <param name="requirement">The <see cref="ReputationRequirement"/> to evaluate.</param>
    /// <returns>True if the requirement is met, false otherwise.</returns>
    public bool CheckReputationRequirement(ReputationRequirement requirement)
    {
        int currentRep = GetReputation(requirement.factionName);
        switch (requirement.comparison)
        {
            case ComparisonOperator.GreaterThan: return currentRep > requirement.requiredAmount;
            case ComparisonOperator.GreaterThanOrEqual: return currentRep >= requirement.requiredAmount;
            case ComparisonOperator.LessThan: return currentRep < requirement.requiredAmount;
            case ComparisonOperator.LessThanOrEqual: return currentRep <= requirement.requiredAmount;
            case ComparisonOperator.Equal: return currentRep == requirement.requiredAmount;
            case ComparisonOperator.NotEqual: return currentRep != requirement.requiredAmount;
            default:
                Debug.LogError($"Unknown comparison operator: {requirement.comparison}");
                return false;
        }
    }

    /// <summary>
    /// Debug method to display all current reputations in the console.
    /// </summary>
    public void DisplayAllReputations()
    {
        string repString = "--- Current Reputations ---\n";
        foreach (var kvp in _currentReputations)
        {
            repString += $"- {kvp.Key}: {kvp.Value}\n";
        }
        Debug.Log(repString);
    }

    #endregion ReputationManager


    // --- PART 2: Dialogue System (DialogueManager) ---
    // This section handles the flow and simulated display of dialogue.
    // It interacts directly with the ReputationManager for integration logic.

    #region DialogueManager

    private DialogueNode _currentDialogueNode; // The node currently being processed
    // (No _currentLineIndex needed for this simplified example, as DialogueLineNode is single-line)

    /// <summary>
    /// Starts a new dialogue sequence from a given starting node.
    /// </summary>
    /// <param name="startNode">The first node in the dialogue sequence.</param>
    public void StartDialogue(DialogueNode startNode)
    {
        if (startNode == null)
        {
            Debug.LogError("Attempted to start dialogue with a null node.");
            return;
        }
        Debug.Log("--- Dialogue Started ---");
        _currentDialogueNode = startNode;
        ProcessNode(_currentDialogueNode);
    }

    // Determines the type of the current node and calls the appropriate display method.
    private void ProcessNode(DialogueNode node)
    {
        if (node == null)
        {
            Debug.Log("--- Dialogue Ended ---");
            _currentDialogueNode = null; // Reset current node when dialogue finishes
            return;
        }

        _currentDialogueNode = node; // Update the current node reference

        if (node is DialogueLineNode lineNode)
        {
            DisplayLine(lineNode);
        }
        else if (node is DialogueChoiceNode choiceNode)
        {
            DisplayChoices(choiceNode);
        }
        else
        {
            Debug.LogError($"Unknown DialogueNode type encountered: {node.GetType()}");
            ProcessNode(null); // End dialogue if an unknown node type is found
        }
    }

    // Simulates displaying a single line of dialogue in the console.
    private void DisplayLine(DialogueLineNode lineNode)
    {
        // In a real game, this would update UI text elements (e.g., player/NPC name, dialogue text).
        Debug.Log($"<b><color=lime>[{lineNode.Speaker}]:</color></b> {lineNode.Text}");

        // For this example, we'll automatically proceed to the next node after a short delay
        // to simulate reading time. In a real game, this would often wait for player input (e.g., a "Next" button click).
        Invoke(nameof(ProceedToNextNode), 2.5f); // Automatically proceed after 2.5 seconds
    }

    // Helper method to proceed to the next node after a DialogueLineNode.
    private void ProceedToNextNode()
    {
        if (_currentDialogueNode is DialogueLineNode lineNode)
        {
            ProcessNode(lineNode.NextNode);
        }
    }

    // Simulates displaying multiple dialogue choices in the console.
    private void DisplayChoices(DialogueChoiceNode choiceNode)
    {
        Debug.Log($"<b><color=yellow>[{choiceNode.Speaker}]:</color></b> {choiceNode.IntroText}");
        Debug.Log("--- Player Choices ---");

        // Reputation-based filtering: Only display choices for which the player meets all requirements.
        List<DialogueChoice> availableChoices = choiceNode.Choices
            .Where(choice => choice.ReputationRequirements.All(req => Instance.CheckReputationRequirement(req)))
            .ToList();

        if (availableChoices.Count == 0)
        {
            Debug.LogWarning("<color=red>No choices available based on current reputation! Ending dialogue.</color>");
            ProcessNode(null); // End dialogue if no valid choices are present
            return;
        }

        for (int i = 0; i < availableChoices.Count; i++)
        {
            DialogueChoice choice = availableChoices[i];
            Debug.Log($"<b>{i + 1}.</b> <color=white>{choice.Text}</color>");
            // In a real UI, you would instantiate UI buttons here and attach MakeChoice() to their OnClick event.
        }

        // For demonstration, we automatically choose the first available option after a delay.
        // In a real game, this section would be replaced by actual player input (e.g., clicking a button).
        if (availableChoices.Count > 0)
        {
            Debug.Log("<color=grey><i>(Simulating player choosing the first available option after 3 seconds...)</i></color>");
            Invoke("SimulatePlayerChoice", 3f);
        }
    }

    // Simulates a player making a choice for demonstration purposes.
    private void SimulatePlayerChoice()
    {
        if (_currentDialogueNode is DialogueChoiceNode choiceNode)
        {
            // Re-filter choices to ensure we only pick from currently valid ones (in case reputation changed async)
            List<DialogueChoice> availableChoices = choiceNode.Choices
                .Where(choice => choice.ReputationRequirements.All(req => Instance.CheckReputationRequirement(req)))
                .ToList();

            if (availableChoices.Count > 0)
            {
                // Select the first available option for this example.
                DialogueChoice selectedChoice = availableChoices[0];
                Debug.Log($"<color=magenta>--- Player selected: '{selectedChoice.Text}' ---</color>");
                MakeChoice(selectedChoice); // Process the chosen option
            }
            else
            {
                Debug.LogWarning("<color=red>No available choices to simulate a selection! Dialogue ends.</color>");
                ProcessNode(null); // End dialogue
            }
        }
    }

    /// <summary>
    /// Processes a player's chosen dialogue option.
    /// This is the primary integration point where dialogue impacts the reputation system.
    /// </summary>
    /// <param name="choice">The <see cref="DialogueChoice"/> that was selected by the player.</param>
    public void MakeChoice(DialogueChoice choice)
    {
        if (choice == null)
        {
            Debug.LogError("Attempted to make a null choice.");
            return;
        }

        // 1. Apply Reputation Effects:
        // Iterate through all reputation changes associated with this choice and apply them.
        foreach (var effect in choice.ReputationEffects)
        {
            Instance.ChangeReputation(effect.factionName, effect.amount);
        }

        // 2. Proceed to the next dialogue node specified by the chosen option.
        ProcessNode(choice.NextNode);
    }

    #endregion DialogueManager


    // --- PART 3: Dialogue Data Structures ---
    // These classes and structs define how dialogue is structured, including how
    // reputation effects and requirements are embedded within dialogue elements.
    // They are nested within the main class for a self-contained example.

    #region DialogueDataStructures

    /// <summary>
    /// Defines a single reputation change (faction and amount).
    /// Used within a <see cref="DialogueChoice"/> to specify its impact.
    /// </summary>
    [System.Serializable]
    public struct ReputationEffect
    {
        public string factionName; // The faction affected (e.g., "Guards")
        public int amount;         // The amount of reputation change (positive or negative)
    }

    /// <summary>
    /// Defines a condition based on a player's reputation with a specific faction.
    /// Used to determine if a <see cref="DialogueChoice"/> should be available.
    /// </summary>
    [System.Serializable]
    public struct ReputationRequirement
    {
        public string factionName;         // The faction whose reputation is being checked
        public int requiredAmount;         // The reputation value to compare against
        public ComparisonOperator comparison; // The type of comparison (e.g., GreaterThanOrEqual)
    }

    /// <summary>
    /// Enumeration for different types of comparison operations used in reputation requirements.
    /// </summary>
    public enum ComparisonOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual
    }

    /// <summary>
    /// Represents a single selectable option presented to the player during dialogue.
    /// Crucially, it contains lists for <see cref="ReputationEffect"/> (what it changes)
    /// and <see cref="ReputationRequirement"/> (what's needed to see/select it).
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string Text;                                // The text displayed for this choice
        public List<ReputationEffect> ReputationEffects;    // Reputation changes this choice causes
        public List<ReputationRequirement> ReputationRequirements; // Conditions to show/enable this choice
        public DialogueNode NextNode;                      // The next node in the dialogue flow if this choice is made

        public DialogueChoice(string text, DialogueNode nextNode = null,
                              List<ReputationEffect> effects = null, List<ReputationRequirement> requirements = null)
        {
            Text = text;
            NextNode = nextNode;
            ReputationEffects = effects ?? new List<ReputationEffect>();
            ReputationRequirements = requirements ?? new List<ReputationRequirement>();
        }
    }

    /// <summary>
    /// Base abstract class for all dialogue nodes. Provides common properties like Speaker.
    /// </summary>
    public abstract class DialogueNode
    {
        public string Speaker; // The name of the character speaking
    }

    /// <summary>
    /// A concrete dialogue node representing a single line of spoken text.
    /// It automatically proceeds to the <see cref="NextNode"/> after display.
    /// </summary>
    public class DialogueLineNode : DialogueNode
    {
        public string Text;        // The actual dialogue text
        public DialogueNode NextNode; // The next node in the sequence

        public DialogueLineNode(string speaker, string text, DialogueNode nextNode = null)
        {
            Speaker = speaker;
            Text = text;
            NextNode = nextNode;
        }
    }

    /// <summary>
    /// A concrete dialogue node that presents multiple <see cref="DialogueChoice"/>s to the player.
    /// The player's selection determines the next path.
    /// </summary>
    public class DialogueChoiceNode : DialogueNode
    {
        public string IntroText;            // Optional introductory text before the choices are listed
        public List<DialogueChoice> Choices; // The list of choices available at this node

        public DialogueChoiceNode(string speaker, string introText, List<DialogueChoice> choices)
        {
            Speaker = speaker;
            IntroText = introText;
            Choices = choices;
        }
    }

    #endregion DialogueDataStructures


    // --- PART 4: Example Usage ---
    // This section demonstrates how to construct a dialogue sequence and trigger it,
    // showcasing the ReputationDialogueIntegration pattern in action.

    private void Start()
    {
        // Ensure the system is initialized. Awake() handles the singleton setup.

        // --- Example Dialogue Tree Setup ---
        // We'll construct a small, branched dialogue tree.
        // It's often easiest to define the end nodes first and work backward.

        // Define various ending dialogue lines
        var endNodeGoodGuard = new DialogueLineNode("Guard", "Thank you, citizen! Your honesty is appreciated.");
        var endNodeBadGuard = new DialogueLineNode("Guard", "I'll be keeping an eye on you. Move along.");
        var endNodeNeutralGuard = new DialogueLineNode("Guard", "Alright, I'll handle it from here. Thanks for the tip.");
        var endNodeGoodTownsfolk = new DialogueLineNode("Townsfolk", "Oh, thank you so much! I'll spread word of your kindness!");
        var endNodeBadTownsfolk = new DialogueLineNode("Townsfolk", "Hmph. Some hero you are.");
        var endNodeMerchantHelp = new DialogueLineNode("Townsfolk", "You drive a hard bargain, but a deal's a deal. Thank you!");

        // --- Dialogue for a Guard Encounter ---
        // This demonstrates reputation *requirements* for choices and reputation *effects* from choices.

        // Guard's Choices
        var guardChoices = new List<DialogueChoice>();

        // Option 1: Honest and helpful
        guardChoices.Add(new DialogueChoice(
            "Tell the truth: 'I saw a suspicious figure. I can help search.'",
            endNodeGoodGuard,
            new List<ReputationEffect> {
                new ReputationEffect { factionName = "Guards", amount = 10 },
                new ReputationEffect { factionName = "Townsfolk", amount = 5 } // Good deed might indirectly boost townsfolk rep
            }
        ));

        // Option 2: Evasive/Lie
        guardChoices.Add(new DialogueChoice(
            "Lie: 'I haven't seen anything. I'm in a hurry.'",
            endNodeBadGuard,
            new List<ReputationEffect> {
                new ReputationEffect { factionName = "Guards", amount = -10 }
            },
            // Requirement: Only allow this option if "Townsfolk" reputation is low
            new List<ReputationRequirement> {
                new ReputationRequirement { factionName = "Townsfolk", requiredAmount = 0, comparison = ComparisonOperator.LessThanOrEqual }
            }
        ));

        // Option 3: Use high reputation to defuse situation
        guardChoices.Add(new DialogueChoice(
            "Assert your authority (requires high Guards reputation): 'Officer, I'm known for assisting the watch. No need to detain me.'",
            endNodeNeutralGuard,
            null, // This choice doesn't change reputation, but *requires* it
            new List<ReputationRequirement> {
                new ReputationRequirement { factionName = "Guards", requiredAmount = 15, comparison = ComparisonOperator.GreaterThanOrEqual }
            }
        ));

        var guardChoiceNode = new DialogueChoiceNode("Guard", "Have you seen anything unusual around here?", guardChoices);

        // --- Dialogue for a Townsfolk Encounter ---
        // Another example with different faction impacts and requirements.

        // Townsfolk's Choices
        var townsfolkChoices = new List<DialogueChoice>();

        // Option A: Help purely out of kindness
        townsfolkChoices.Add(new DialogueChoice(
            "Offer to help: 'Of course, I'll keep an eye out for Mittens!'",
            endNodeGoodTownsfolk,
            new List<ReputationEffect> {
                new ReputationEffect { factionName = "Townsfolk", amount = 15 },
                new ReputationEffect { factionName = "Merchants", amount = 2 } // Merchants might see good deeds
            }
        ));

        // Option B: Refuse rudely
        townsfolkChoices.Add(new DialogueChoice(
            "Refuse rudely: 'A lost cat? Not my problem.'",
            endNodeBadTownsfolk,
            new List<ReputationEffect> {
                new ReputationEffect { factionName = "Townsfolk", amount = -10 }
            },
            // Requirement: Only show this option if "Guards" reputation is low, implying less fear of consequences
            new List<ReputationRequirement> {
                new ReputationRequirement { factionName = "Guards", requiredAmount = 5, comparison = ComparisonOperator.LessThan }
            }
        ));

        // Option C: Offer help for a price (requires high Merchants rep)
        townsfolkChoices.Add(new DialogueChoice(
            "Offer help for a fee: 'I can help, for a small finder's fee. It's good business.'",
            endNodeMerchantHelp,
            new List<ReputationEffect> {
                new ReputationEffect { factionName = "Townsfolk", amount = 5 }, // Still helpful, but transactional
                new ReputationEffect { factionName = "Merchants", amount = 10 }
            },
            new List<ReputationRequirement> {
                new ReputationRequirement { factionName = "Merchants", requiredAmount = 10, comparison = ComparisonOperator.GreaterThanOrEqual }
            }
        ));

        var townsfolkChoiceNode = new DialogueChoiceNode("Old Woman", "My cat, Mittens, is lost! Could you help me?", townsfolkChoices);


        // --- Define the full dialogue sequences (linking nodes) ---

        // Main dialogue branch 1: Guard encounter
        var dialogueSequenceGuard = new DialogueLineNode("Narrator", "You walk through the bustling market square.",
            new DialogueLineNode("Guard", "Halt! You there, adventurer!",
                guardChoiceNode
            )
        );

        // Main dialogue branch 2: Townsfolk encounter
        var dialogueSequenceTownsfolk = new DialogueLineNode("Narrator", "An old woman approaches you with a worried look.",
            townsfolkChoiceNode
        );

        // --- Triggering the dialogue ---
        // In a real game, this would be triggered by player interaction (e.g., clicking on an NPC, entering a trigger zone).

        Debug.Log("\n--- Starting Dialogue Scenario 1: Guard Encounter ---");
        Instance.StartDialogue(dialogueSequenceGuard);

        // --- Experiment: Uncomment these lines to test different scenarios ---

        // Scenario 2: Start with high Guard reputation to immediately see option 3
        // _currentReputations["Guards"] = 20;
        // Debug.Log("\n--- Starting Dialogue Scenario 2: Guard Encounter (High Guard Rep) ---");
        // Instance.StartDialogue(dialogueSequenceGuard);

        // Scenario 3: Start with low Townsfolk reputation to enable the rude lie option for the Guard
        // _currentReputations["Townsfolk"] = -5;
        // Debug.Log("\n--- Starting Dialogue Scenario 3: Guard Encounter (Low Townsfolk Rep) ---");
        // Instance.StartDialogue(dialogueSequenceGuard);

        // Scenario 4: Townsfolk Encounter (uncomment and comment out the guard one to test)
        // Debug.Log("\n--- Starting Dialogue Scenario 4: Townsfolk Encounter ---");
        // Instance.StartDialogue(dialogueSequenceTownsfolk);

        // Scenario 5: Townsfolk Encounter with high Merchant reputation
        // _currentReputations["Merchants"] = 12;
        // Debug.Log("\n--- Starting Dialogue Scenario 5: Townsfolk Encounter (High Merchant Rep) ---");
        // Instance.StartDialogue(dialogueSequenceTownsfolk);
    }

    #endregion ExampleUsage
}

/*
 * --- Practical Implementation Notes for a Real Unity Project ---
 *
 * 1.  UI Integration:
 *     -   **Dialogue Text:** Instead of `Debug.Log`, you would update a `UnityEngine.UI.Text` or TextMeshProUGUI` component.
 *     -   **Choices:** For `DisplayChoices`, you would typically have a prefab for a choice button. Instantiate these buttons
 *         or activate existing ones from a pool. Attach an `onClick` listener to each button that calls
 *         `ReputationDialogueSystemExample.Instance.MakeChoice(DialogueChoice chosenChoice);`, passing the
 *         corresponding `DialogueChoice` object.
 *     -   **Progression:** Replace `Invoke(nameof(ProceedToNextNode), 2.5f);` with logic that waits for player input
 *         (e.g., a "Next" button click or key press) to advance single dialogue lines.
 *     -   **Choice Simulation:** Remove `Invoke("SimulatePlayerChoice", 3f);` as real player input will drive choices.
 *
 * 2.  Dialogue Content Creation:
 *     -   **Hardcoding:** While useful for this example, hardcoding dialogue trees in `Start()` is not scalable.
 *     -   **ScriptableObjects:** A common Unity approach is to define `DialogueNode`s and `DialogueChoice`s as
 *         `ScriptableObject` assets. This allows you to build entire dialogue trees directly in the Inspector.
 *         You would then load these assets at runtime.
 *     -   **External Tools:** For complex dialogue, consider integrating with specialized dialogue authoring tools
 *         like Yarn Spinner, Ink, or Fungus. These tools typically provide their own runtime libraries
 *         that you would integrate with this `ReputationDialogueSystemExample`'s `StartDialogue` and `MakeChoice` methods.
 *         The reputation logic would still reside here, and the external tool would simply provide the text
 *         and the `ReputationEffect`s/`ReputationRequirement`s to apply.
 *
 * 3.  Extensibility:
 *     -   **More Reputation Types:** You could expand `ReputationEffect` and `ReputationRequirement` to include
 *         other game states (e.g., `PlayerInventoryRequirement`, `QuestStatusRequirement`).
 *     -   **Dialogue Actions:** Choices could trigger more than just reputation changes, such as giving items,
 *         starting quests, spawning enemies, or playing animations. You'd add an `Action` or `List<DialogueAction>`
 *         to `DialogueChoice` (e.g., `public Action OnChosenAction;`).
 *     -   **UI Feedback:** Subscribe to `OnReputationChanged` event to update reputation bars, icons, or text
 *         in your game's UI whenever reputation shifts.
 *
 * This example provides a solid foundation for integrating dynamic reputation mechanics into your game's narrative and choices.
 */
```