// Unity Design Pattern Example: TreasureHuntSystem
// This script demonstrates the TreasureHuntSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements the 'TreasureHuntSystem' as a pattern for managing a sequential series of objectives (clues) that ultimately lead to a final reward. It leverages Unity's ScriptableObjects for defining the hunt's data and a MonoBehaviour singleton for managing its runtime state and progression.

### TreasureHuntSystem Design Pattern Breakdown:

1.  **`TreasureHuntStep` (Abstract ScriptableObject):**
    *   **Purpose:** Defines the common interface and properties for any single step (clue) in a treasure hunt.
    *   **Pattern:** Acts as the **Strategy** interface. Each concrete step type will implement its own logic for checking completion. By being a `ScriptableObject`, steps can be created as assets in the Unity Editor, making them highly reusable and configurable.
    *   **Key Methods:**
        *   `OnStepActivated()`: Called when this step becomes the current objective.
        *   `OnStepDeactivated()`: Called when this step is completed or the hunt changes.
        *   `CheckCompletion(string completionIdentifier)`: An abstract method that concrete steps must implement to determine if a given game event (identified by a string) fulfills the step's requirement.

2.  **Concrete `TreasureHuntStep` Implementations (ScriptableObjects):**
    *   **Purpose:** Provide specific types of objectives (e.g., "find an item," "reach a location," "interact with an NPC").
    *   **Pattern:** Concrete **Strategies**. Each class (e.g., `FindItemStep`, `ReachLocationStep`, `InteractNPCStep`) implements `CheckCompletion` based on its specific criteria.
    *   **Benefit:** Easily extensible. You can add new step types (e.g., `SolvePuzzleStep`, `DefeatEnemyStep`) without modifying the core `TreasureHuntManager`.

3.  **`TreasureHuntDefinition` (ScriptableObject):**
    *   **Purpose:** Defines an entire treasure hunt sequence, including all its steps and the final reward.
    *   **Pattern:** Represents the **Context** or configuration for a specific hunt. It holds a list of `TreasureHuntStep` assets, forming the ordered sequence.
    *   **Benefit:** Allows creating multiple, distinct treasure hunts as assets in the Editor, separating hunt data from the game logic.

4.  **`TreasureHuntManager` (MonoBehaviour Singleton):**
    *   **Purpose:** The central orchestrator that manages the active treasure hunt's state (which hunt is active, which step is current, etc.). It provides the API for game systems to interact with the hunt.
    *   **Pattern:** A **Singleton** (for global access) and the **Context** that uses the `TreasureHuntStep` strategies. It maintains the current state, advances the hunt, and broadcasts events.
    *   **Key Methods:**
        *   `StartTreasureHunt(TreasureHuntDefinition huntDefinition)`: Initiates a new hunt.
        *   `TryCompleteCurrentStep(string completionIdentifier)`: The primary method for other game systems to signal that a potential step completion event has occurred. The manager delegates the actual completion check to the `CurrentStep` (Strategy).
        *   `FailCurrentHunt()`: Aborts the current hunt.
    *   **Events:** Uses `UnityEvent`s (e.g., `OnStepActivated`, `OnHuntCompleted`) to notify other parts of the game (like UI, analytics, story systems) about hunt progress. This follows the **Observer** pattern.

5.  **`TestTreasureHunter` (MonoBehaviour for Demonstration):**
    *   **Purpose:** A utility component to simulate player input and demonstrate how external game systems would interact with the `TreasureHuntManager`. It subscribes to the manager's events to update a simple debug UI.

### Complete C# Unity Code:

To use this code:

1.  **Create a new C# script** in your Unity project, name it `TreasureHuntSystem.cs`, and paste all the code below into it.
2.  **Follow the "Unity Setup Instructions"** provided in the comments to set up your scene and create the necessary ScriptableObjects.

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events; // For editor-assignable events
using System; // For Guid and StringComparison
using TMPro; // For TextMeshPro UI elements (optional, but recommended for testing UI)

// --- 1. Define the TreasureHuntStep Base Pattern (Abstract ScriptableObject) ---
// This abstract class serves as the base for all types of individual steps in a treasure hunt.
// It uses the Strategy pattern principles, where each concrete step implements its own
// specific completion logic. By making it a ScriptableObject, we can easily create
// and manage different step types as assets in the Unity editor.

/// <summary>
/// Abstract base class for a single step in a treasure hunt.
/// Inherits from ScriptableObject, allowing creation as assets in Unity.
/// This acts as the 'Strategy' interface for different types of hunt steps.
/// </summary>
public abstract class TreasureHuntStep : ScriptableObject
{
    [Tooltip("A unique identifier for this step. Useful for debugging and tracking.")]
    public string stepID = Guid.NewGuid().ToString(); // Auto-generate a unique ID
    [Tooltip("The title or name of this clue/step.")]
    public string stepTitle = "New Clue";
    [Tooltip("A detailed description of what the player needs to do for this step.")]
    [TextArea(3, 5)]
    public string stepDescription = "Find the next clue...";
    [Tooltip("An optional hint for the player if they get stuck.")]
    [TextArea(1, 3)]
    public string hint = "";

    /// <summary>
    /// Called when this step becomes the currently active step in the hunt.
    /// Use this for any initialization specific to the step (e.g., showing UI elements, enabling objects).
    /// </summary>
    public virtual void OnStepActivated()
    {
        Debug.Log($"<color=cyan>Step Activated:</color> <b>{stepTitle}</b> - {stepDescription}");
        // Example: Maybe highlight an object, play a sound, or show a specific UI element.
    }

    /// <summary>
    /// Called when this step is successfully completed or the hunt changes/fails.
    /// Use this for any cleanup specific to the step (e.g., hiding UI, disabling objects).
    /// </summary>
    public virtual void OnStepDeactivated()
    {
        Debug.Log($"<color=green>Step Deactivated:</color> <b>{stepTitle}</b>");
        // Example: Clean up temporary UI, stop highlighting.
    }

    /// <summary>
    /// Checks if the given identifier matches the completion criteria for this step.
    /// This is where the core logic for completing a step resides.
    /// This is the abstract method that concrete strategies (step types) must implement.
    /// </summary>
    /// <param name="completionIdentifier">A string identifier representing a game event (e.g., "collected_sword", "reached_forest_shrine").</param>
    /// <returns>True if the step is considered complete, false otherwise.</returns>
    public abstract bool CheckCompletion(string completionIdentifier);
}

// --- 2. Concrete Implementations of TreasureHuntStep ---
// These classes define specific types of steps a player might encounter.
// Each one demonstrates a different completion strategy, inheriting from TreasureHuntStep.

/// <summary>
/// A treasure hunt step that requires the player to find and interact with a specific item.
/// Completion is checked by matching an item ID.
/// </summary>
[CreateAssetMenu(fileName = "NewFindItemStep", menuName = "TreasureHunt/Steps/Find Item Step", order = 1)]
public class FindItemStep : TreasureHuntStep
{
    [Tooltip("The ID of the item the player needs to find and interact with to complete this step.")]
    public string requiredItemID;

    public override void OnStepActivated()
    {
        base.OnStepActivated();
        Debug.Log($"Required Item: <color=yellow>{requiredItemID}</color>");
    }

    /// <summary>
    /// Checks if the provided identifier matches the required item ID.
    /// </summary>
    public override bool CheckCompletion(string completionIdentifier)
    {
        // For simplicity, we assume the completionIdentifier directly reports the item ID.
        // In a real game, this might involve checking if the player collected an item
        // and its ID matches 'requiredItemID' from an inventory system event.
        return completionIdentifier.Equals(requiredItemID, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A treasure hunt step that requires the player to reach a specific location or zone.
/// Completion is checked by matching a location/zone identifier.
/// </summary>
[CreateAssetMenu(fileName = "NewReachLocationStep", menuName = "TreasureHunt/Steps/Reach Location Step", order = 2)]
public class ReachLocationStep : TreasureHuntStep
{
    [Tooltip("The identifier of the location or zone the player needs to reach. " +
             "This could be a collider trigger's tag, a waypoint name, etc.")]
    public string targetLocationIdentifier;
    [Tooltip("An optional reference to a GameObject that marks the target location in the scene. " +
             "Useful for visualizing the target or instantiating a temporary marker.")]
    public GameObject locationMarkerPrefab;

    public override void OnStepActivated()
    {
        base.OnStepActivated();
        Debug.Log($"Target Location: <color=yellow>{targetLocationIdentifier}</color>");
        if (locationMarkerPrefab != null)
        {
            // In a real game, you might instantiate a temporary marker or highlight an existing one.
            Debug.Log($"A visual marker could be shown for {targetLocationIdentifier}");
        }
    }

    public override void OnStepDeactivated()
    {
        base.OnStepDeactivated();
        // In a real game, hide/destroy the temporary marker here.
    }

    /// <summary>
    /// Checks if the provided identifier matches the target location identifier.
    /// </summary>
    public override bool CheckCompletion(string completionIdentifier)
    {
        // This assumes some system (e.g., a collider trigger script) sends
        // 'targetLocationIdentifier' when the player enters the designated zone.
        return completionIdentifier.Equals(targetLocationIdentifier, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A treasure hunt step that requires the player to interact with a specific NPC.
/// Completion is checked by matching an NPC ID.
/// </summary>
[CreateAssetMenu(fileName = "NewInteractNPCStep", menuName = "TreasureHunt/Steps/Interact NPC Step", order = 3)]
public class InteractNPCStep : TreasureHuntStep
{
    [Tooltip("The ID of the NPC the player needs to interact with to complete this step.")]
    public string requiredNPCID;

    public override void OnStepActivated()
    {
        base.OnStepActivated();
        Debug.Log($"Interact with NPC: <color=yellow>{requiredNPCID}</color>");
    }

    /// <summary>
    /// Checks if the provided identifier matches the required NPC ID.
    /// </summary>
    public override bool CheckCompletion(string completionIdentifier)
    {
        // This assumes an NPC interaction system sends the NPC's ID upon interaction.
        return completionIdentifier.Equals(requiredNPCID, StringComparison.OrdinalIgnoreCase);
    }
}

// --- 3. Define the TreasureHuntDefinition (ScriptableObject) ---
// This ScriptableObject acts as a blueprint for an entire treasure hunt,
// holding the sequence of steps and final reward details.

/// <summary>
/// Defines a complete treasure hunt, including its sequence of steps and final reward.
/// Inherits from ScriptableObject, allowing creation as assets in Unity.
/// This acts as the 'Context' that holds an ordered list of 'Strategies' (TreasureHuntStep).
/// </summary>
[CreateAssetMenu(fileName = "NewTreasureHunt", menuName = "TreasureHunt/Treasure Hunt Definition", order = 0)]
public class TreasureHuntDefinition : ScriptableObject
{
    [Tooltip("The overall name of this treasure hunt.")]
    public string huntName = "The Legendary Quest";
    [Tooltip("The ordered list of steps that compose this treasure hunt.")]
    public List<TreasureHuntStep> steps = new List<TreasureHuntStep>();
    [Tooltip("The message displayed when the hunt is successfully completed.")]
    [TextArea(3, 5)]
    public string finalRewardMessage = "Congratulations, you found the treasure!";
    [Tooltip("An optional prefab to instantiate as the final reward.")]
    public GameObject finalRewardPrefab;

    /// <summary>
    /// Gets the total number of steps defined in this treasure hunt.
    /// </summary>
    public int NumberOfSteps => steps.Count;
}


// --- 4. The TreasureHuntManager (MonoBehaviour Singleton) ---
// This is the central orchestrator of the TreasureHuntSystem.
// It manages the active hunt's state, progression, and broadcasts events.

/// <summary>
/// Manages the state and progression of treasure hunts.
/// Implements a Singleton pattern for easy global access from other game systems.
/// Provides events for other systems to subscribe to hunt updates (Observer pattern).
/// This acts as the 'Client' that interacts with the 'Context' (TreasureHuntDefinition)
/// and delegates completion checks to the 'Strategy' (TreasureHuntStep).
/// </summary>
public class TreasureHuntManager : MonoBehaviour
{
    // Singleton instance for global access
    public static TreasureHuntManager Instance { get; private set; }

    // --- Events for external systems to subscribe to (Observer Pattern) ---
    // UnityEvents are used here to allow direct assignment of functions
    // from the Inspector, which is convenient for quick hookups (e.g., UI updates).
    // You could also use C# events (Action<T>) for more programmatic subscriptions.

    [Header("Treasure Hunt Events")]
    [Tooltip("Invoked when a new treasure hunt is started.")]
    public UnityEvent<TreasureHuntDefinition> OnHuntStarted = new UnityEvent<TreasureHuntDefinition>();
    [Tooltip("Invoked when a new step within the current hunt becomes active.")]
    public UnityEvent<TreasureHuntStep> OnStepActivated = new UnityEvent<TreasureHuntStep>();
    [Tooltip("Invoked when the current step is successfully completed.")]
    public UnityEvent<TreasureHuntStep> OnStepCompleted = new UnityEvent<TreasureHuntStep>();
    [Tooltip("Invoked when the entire treasure hunt is successfully completed.")]
    public UnityEvent<TreasureHuntDefinition> OnHuntCompleted = new UnityEvent<TreasureHuntDefinition>();
    [Tooltip("Invoked when the current treasure hunt is failed or aborted.")]
    public UnityEvent<TreasureHuntDefinition> OnHuntFailed = new UnityEvent<TreasureHuntDefinition>();

    // --- Internal State ---
    private TreasureHuntDefinition _currentHunt;
    private int _currentStepIndex = -1; // -1 indicates no hunt is active

    /// <summary>
    /// Gets the currently active treasure hunt definition.
    /// </summary>
    public TreasureHuntDefinition CurrentHunt => _currentHunt;

    /// <summary>
    /// Gets the currently active step in the treasure hunt.
    /// Returns null if no hunt is active or the index is out of bounds.
    /// </summary>
    public TreasureHuntStep CurrentStep
    {
        get
        {
            if (_currentHunt == null || _currentStepIndex < 0 || _currentStepIndex >= _currentHunt.steps.Count)
            {
                return null;
            }
            return _currentHunt.steps[_currentStepIndex];
        }
    }

    /// <summary>
    /// Gets the current step index (0-based). Returns -1 if no hunt is active.
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;

    /// <summary>
    /// Gets the total number of steps in the current hunt. Returns 0 if no hunt is active.
    /// </summary>
    public int TotalSteps => _currentHunt != null ? _currentHunt.NumberOfSteps : 0;

    // --- Monobehaviour Lifecycle ---
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the line below if the manager should persist across scene loads.
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Duplicate TreasureHuntManager instance found. Destroying new one.", this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public API for managing hunts ---

    /// <summary>
    /// Starts a new treasure hunt. If a hunt is already active, it will be aborted.
    /// </summary>
    /// <param name="huntDefinition">The ScriptableObject defining the hunt to start.</param>
    public void StartTreasureHunt(TreasureHuntDefinition huntDefinition)
    {
        if (huntDefinition == null)
        {
            Debug.LogError("Attempted to start a null treasure hunt definition.", this);
            return;
        }

        if (_currentHunt != null)
        {
            Debug.LogWarning($"Aborting current hunt '{_currentHunt.huntName}' to start new one '{huntDefinition.huntName}'.", this);
            FailCurrentHunt(); // Abort the previous hunt gracefully
        }

        _currentHunt = huntDefinition;
        _currentStepIndex = -1; // Reset index before starting the first step

        Debug.Log($"<color=blue>Starting Treasure Hunt: {_currentHunt.huntName}</color> with {_currentHunt.steps.Count} steps.");
        OnHuntStarted.Invoke(_currentHunt); // Notify subscribers (Observer pattern)

        AdvanceToNextStep(); // Activate the first step
    }

    /// <summary>
    /// Attempts to complete the current active step using a given completion identifier.
    /// This is the primary method for game systems to signal progress in the hunt.
    /// The manager delegates the completion check to the current step (Strategy).
    /// </summary>
    /// <param name="completionIdentifier">A string representing a game event (e.g., "collected_sword", "reached_forest_shrine").</param>
    /// <returns>True if the current step was completed by this identifier, false otherwise.</returns>
    public bool TryCompleteCurrentStep(string completionIdentifier)
    {
        if (_currentHunt == null || CurrentStep == null)
        {
            Debug.LogWarning($"No active hunt or current step to complete with identifier '{completionIdentifier}'.", this);
            return false;
        }

        // Delegate the completion check to the current step's specific strategy
        if (CurrentStep.CheckCompletion(completionIdentifier))
        {
            Debug.Log($"<color=green>Current step '{CurrentStep.stepTitle}' completed!</color> (Identifier: '{completionIdentifier}')");
            CurrentStep.OnStepDeactivated(); // Clean up current step
            OnStepCompleted.Invoke(CurrentStep); // Notify subscribers (Observer pattern)

            AdvanceToNextStep(); // Move to the next step or complete the hunt
            return true;
        }
        else
        {
            Debug.Log($"Identifier '{completionIdentifier}' did not complete step '{CurrentStep.stepTitle}'. Required: {CurrentStep.hint}");
            return false;
        }
    }

    /// <summary>
    /// Aborts the current treasure hunt, marking it as failed.
    /// </summary>
    public void FailCurrentHunt()
    {
        if (_currentHunt == null)
        {
            Debug.LogWarning("No active hunt to fail.", this);
            return;
        }

        Debug.Log($"<color=red>Treasure Hunt Failed: {_currentHunt.huntName}</color>");
        if (CurrentStep != null)
        {
            CurrentStep.OnStepDeactivated(); // Clean up current step
        }
        OnHuntFailed.Invoke(_currentHunt); // Notify subscribers (Observer pattern)

        _currentHunt = null; // Clear the hunt
        _currentStepIndex = -1;
    }

    // --- Internal Logic ---

    /// <summary>
    /// Advances the hunt to the next step, or completes the hunt if all steps are done.
    /// </summary>
    private void AdvanceToNextStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex < _currentHunt.steps.Count)
        {
            // There are more steps, activate the next one
            Debug.Log($"<color=blue>Advancing to Step {_currentStepIndex + 1}/{_currentHunt.steps.Count}</color>");
            CurrentStep.OnStepActivated(); // Initialize the new step
            OnStepActivated.Invoke(CurrentStep); // Notify subscribers (Observer pattern)
        }
        else
        {
            // All steps completed, finish the hunt!
            Debug.Log($"<color=magenta>Treasure Hunt Completed: {_currentHunt.huntName}</color>");
            Debug.Log($"<color=magenta>Final Reward:</color> {_currentHunt.finalRewardMessage}");

            if (_currentHunt.finalRewardPrefab != null)
            {
                // In a real game, you'd instantiate this at a specific location, add to player inventory, etc.
                // For demonstration, we'll just instantiate it at origin and log its name.
                GameObject reward = Instantiate(_currentHunt.finalRewardPrefab, Vector3.zero, Quaternion.identity);
                reward.name = _currentHunt.finalRewardPrefab.name + " (Treasure Reward)"; // Give it a distinct name
                Debug.Log($"Instantiated final reward: {reward.name}");
            }

            OnHuntCompleted.Invoke(_currentHunt); // Notify subscribers (Observer pattern)

            _currentHunt = null; // Clear the hunt
            _currentStepIndex = -1;
        }
    }
}


// --- 5. Example Usage / TestTreasureHunter ---
// This component simulates player input and interacts with the TreasureHuntManager
// to demonstrate how the system works. It's for testing and educational purposes.

/// <summary>
/// A simple component to simulate player actions and interact with the TreasureHuntManager.
/// Attach this to an empty GameObject in your scene. It also handles basic UI updates
/// by subscribing to the TreasureHuntManager's events.
/// </summary>
public class TestTreasureHunter : MonoBehaviour
{
    [Tooltip("Drag the TreasureHuntManager GameObject here from your scene (or it will find the singleton).")]
    public TreasureHuntManager huntManager; // Assign in Inspector
    [Tooltip("Drag a TreasureHuntDefinition ScriptableObject here to start with when 'S' is pressed.")]
    public TreasureHuntDefinition initialHunt; // Assign in Inspector

    [Header("UI References (Optional - Requires TextMeshPro)")]
    public TextMeshProUGUI huntTitleText;
    public TextMeshProUGUI stepDescriptionText;
    public TextMeshProUGUI stepProgressText;
    public GameObject completionMessagePanel;
    public TextMeshProUGUI completionMessageText;

    private void Start()
    {
        // Attempt to get the singleton instance if not assigned
        if (huntManager == null)
        {
            huntManager = TreasureHuntManager.Instance;
            if (huntManager == null)
            {
                Debug.LogError("TreasureHuntManager not found in scene. Please add it to an active GameObject.", this);
                enabled = false; // Disable this component if manager is missing
                return;
            }
        }

        // Subscribe to events for UI updates (Observer Pattern)
        huntManager.OnHuntStarted.AddListener(OnHuntStarted);
        huntManager.OnStepActivated.AddListener(OnStepActivated);
        huntManager.OnStepCompleted.AddListener(OnStepCompleted);
        huntManager.OnHuntCompleted.AddListener(OnHuntCompleted);
        huntManager.OnHuntFailed.AddListener(OnHuntFailed);

        UpdateUI(); // Initialize UI state

        if (initialHunt != null)
        {
            Debug.Log("Press 'S' to start the initial treasure hunt.");
        }
        else
        {
            Debug.LogWarning("No initial hunt assigned to TestTreasureHunter. Please assign one in the inspector to enable starting a hunt.", this);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks if manager still exists
        if (huntManager != null)
        {
            huntManager.OnHuntStarted.RemoveListener(OnHuntStarted);
            huntManager.OnStepActivated.RemoveListener(OnStepActivated);
            huntManager.OnStepCompleted.RemoveListener(OnStepCompleted);
            huntManager.OnHuntCompleted.RemoveListener(OnHuntCompleted);
            huntManager.OnHuntFailed.RemoveListener(OnHuntFailed);
        }
    }

    // --- UI Update Methods (Subscribed to TreasureHuntManager events) ---
    private void OnHuntStarted(TreasureHuntDefinition hunt)
    {
        if (completionMessagePanel != null) completionMessagePanel.SetActive(false);
        Debug.Log($"TestTreasureHunter: Hunt '{hunt.huntName}' has begun!");
        UpdateUI();
    }

    private void OnStepActivated(TreasureHuntStep step)
    {
        Debug.Log($"TestTreasureHunter: Current Step: '{step.stepTitle}' - '{step.stepDescription}'");
        UpdateUI();
    }

    private void OnStepCompleted(TreasureHuntStep step)
    {
        Debug.Log($"TestTreasureHunter: Completed Step: '{step.stepTitle}'");
        UpdateUI();
    }

    private void OnHuntCompleted(TreasureHuntDefinition hunt)
    {
        Debug.Log($"TestTreasureHunter: Hunt '{hunt.huntName}' is finished! Reward: {hunt.finalRewardMessage}");
        if (completionMessagePanel != null)
        {
            completionMessagePanel.SetActive(true);
            if (completionMessageText != null) completionMessageText.text = hunt.finalRewardMessage;
        }
        UpdateUI();
    }

    private void OnHuntFailed(TreasureHuntDefinition hunt)
    {
        Debug.Log($"TestTreasureHunter: Hunt '{hunt.huntName}' failed.");
        if (completionMessagePanel != null)
        {
            completionMessagePanel.SetActive(true);
            if (completionMessageText != null) completionMessageText.text = $"Hunt Failed: {hunt.huntName}";
        }
        UpdateUI();
    }

    /// <summary>
    /// Updates all connected UI elements to reflect the current state of the hunt.
    /// </summary>
    private void UpdateUI()
    {
        if (huntManager == null) return;

        // Update hunt title
        if (huntTitleText != null)
        {
            huntTitleText.text = huntManager.CurrentHunt != null ? huntManager.CurrentHunt.huntName : "No Active Hunt";
        }

        // Update step description
        if (stepDescriptionText != null)
        {
            stepDescriptionText.text = huntManager.CurrentStep != null ?
                                        $"<size=150%>{huntManager.CurrentStep.stepTitle}</size>\n{huntManager.CurrentStep.stepDescription}\n<i>Hint: {huntManager.CurrentStep.hint}</i>" :
                                        "No current step. Press 'S' to start a new hunt (if initialHunt is set).";
        }

        // Update progress text
        if (stepProgressText != null)
        {
            if (huntManager.CurrentHunt != null)
            {
                stepProgressText.text = $"Step {huntManager.CurrentStepIndex + 1} / {huntManager.TotalSteps}";
            }
            else
            {
                stepProgressText.text = "Hunt not active";
            }
        }
    }

    // --- Input Simulation (for testing the system) ---
    private void Update()
    {
        // Start hunt (if 'S' is pressed and an initial hunt is assigned)
        if (Input.GetKeyDown(KeyCode.S) && huntManager != null && initialHunt != null)
        {
            huntManager.StartTreasureHunt(initialHunt);
        }

        // Fail current hunt (if 'F' is pressed)
        if (Input.GetKeyDown(KeyCode.F) && huntManager != null)
        {
            huntManager.FailCurrentHunt();
        }

        // Simulate completing steps based on the current step type
        if (huntManager != null && huntManager.CurrentStep != null)
        {
            TreasureHuntStep currentStep = huntManager.CurrentStep;

            if (currentStep is FindItemStep findItemStep)
            {
                // Simulate finding an item by pressing '1', '2', or '3'
                // The key pressed should correspond to the `requiredItemID` of the current FindItemStep
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Debug.Log($"Simulating collecting item: AncientMap (Key: 1)");
                    huntManager.TryCompleteCurrentStep("AncientMap");
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log($"Simulating collecting item: GoldenKey (Key: 2)");
                    huntManager.TryCompleteCurrentStep("GoldenKey");
                }
                 if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log($"Simulating collecting item: MagicCompass (Key: 3)");
                    huntManager.TryCompleteCurrentStep("MagicCompass");
                }
            }
            else if (currentStep is ReachLocationStep reachLocationStep)
            {
                // Simulate reaching a location by pressing 'L' or 'K'
                // The key pressed should correspond to the `targetLocationIdentifier` of the current ReachLocationStep
                if (Input.GetKeyDown(KeyCode.L))
                {
                    Debug.Log($"Simulating reaching location: WhisperingCaves (Key: L)");
                    huntManager.TryCompleteCurrentStep("WhisperingCaves");
                }
                if (Input.GetKeyDown(KeyCode.K))
                {
                    Debug.Log($"Simulating reaching location: ForestShrine (Key: K)");
                    huntManager.TryCompleteCurrentStep("ForestShrine");
                }
            }
            else if (currentStep is InteractNPCStep interactNPCStep)
            {
                // Simulate interacting with an NPC by pressing 'I'
                // The key pressed should correspond to the `requiredNPCID` of the current InteractNPCStep
                if (Input.GetKeyDown(KeyCode.I))
                {
                    Debug.Log($"Simulating interacting with NPC: OldHermit (Key: I)");
                    huntManager.TryCompleteCurrentStep("OldHermit");
                }
            }
        }
    }
}
```

### Unity Setup Instructions:

To get this example working in your Unity project:

1.  **Create a new Unity Project** (or open an existing one).
2.  **Create a C# Script:** In your Project window, right-click -> Create -> C# Script. Name it `TreasureHuntSystem`.
3.  **Paste the Code:** Open the `TreasureHuntSystem.cs` script and replace all its content with the complete code provided above. Save the script.
4.  **Install TextMeshPro Essentials (if using UI):** If you plan to use the optional UI, go to `Window -> TextMeshPro -> Import TMP Essential Resources`.
5.  **Create Manager GameObject:**
    *   In the Hierarchy window, right-click -> Create Empty. Name it `_TreasureHuntManager`.
    *   Select `_TreasureHuntManager`. In the Inspector, click "Add Component" and search for `TreasureHuntManager` to add it.
6.  **Create Test Hunter GameObject:**
    *   In the Hierarchy window, right-click -> Create Empty. Name it `_TestTreasureHunter`.
    *   Select `_TestTreasureHunter`. In the Inspector, click "Add Component" and search for `TestTreasureHunter` to add it.
    *   **Link Manager:** Drag the `_TreasureHuntManager` GameObject from the Hierarchy into the `Hunt Manager` slot of the `TestTreasureHunter` component.
7.  **Create UI Elements (Optional but Recommended for Visual Feedback):**
    *   In the Hierarchy, right-click -> UI -> Canvas.
    *   On the Canvas, right-click -> UI -> Text - TextMeshPro. Rename it `HuntTitleText`.
    *   Repeat for: `StepDescriptionText`, `StepProgressText`.
    *   For the completion message: On Canvas, right-click -> UI -> Panel. Name it `CompletionMessagePanel`. Inside the panel, add a TextMeshPro object named `CompletionMessageText`. Initially, disable `CompletionMessagePanel` in the Inspector.
    *   **Link UI:** Drag these UI elements from the Hierarchy into their respective slots on the `_TestTreasureHunter` component in the Inspector.
8.  **Create Treasure Hunt Definitions (ScriptableObjects):**
    *   In the Project window, right-click -> Create -> TreasureHunt -> Treasure Hunt Definition. Name it `MyFirstTreasureHunt`.
    *   Select `MyFirstTreasureHunt` in the Project window. In the Inspector:
        *   Set `Hunt Name` to "The Ancient Artifact Quest".
        *   Increase the `Steps` list size to 4.
        *   Now, create individual `TreasureHuntStep` assets:
            *   Right-click in Project window -> Create -> TreasureHunt -> Steps -> **Find Item Step**. Name it `Clue1_FindMap`.
                *   Set `Step Title`: "The Old Map"
                *   Set `Step Description`: "Find the ancient map hidden in the dusty library."
                *   Set `Hint`: "Check the books on the second floor."
                *   Set `Required Item ID`: "AncientMap"
            *   Right-click in Project window -> Create -> TreasureHunt -> Steps -> **Reach Location Step**. Name it `Clue2_GoToCaves`.
                *   Set `Step Title`: "Whispering Caves Entrance"
                *   Set `Step Description`: "The map points to the entrance of the Whispering Caves."
                *   Set `Hint`: "Look for the twin rock formations."
                *   Set `Target Location Identifier`: "WhisperingCaves"
            *   Right-click in Project window -> Create -> TreasureHunt -> Steps -> **Interact NPC Step**. Name it `Clue3_TalkToHermit`.
                *   Set `Step Title`: "The Wise Hermit"
                *   Set `Step Description`: "A hermit knows the secret of the artifact. Find him deep within the caves."
                *   Set `Hint`: "He prefers solitude, seek the hidden grove."
                *   Set `Required NPC ID`: "OldHermit"
            *   Right-click in Project window -> Create -> TreasureHunt -> Steps -> **Find Item Step**. Name it `Clue4_GetArtifact`.
                *   Set `Step Title`: "The Golden Key"
                *   Set `Step Description`: "The hermit gave you a clue: 'The key lies where light meets shadow.'"
                *   Set `Hint`: "Look under the mossy stone near the cave exit."
                *   Set `Required Item ID`: "GoldenKey"
        *   **Assign Steps to Hunt:** Drag these four newly created step assets (e.g., `Clue1_FindMap`, `Clue2_GoToCaves`, etc.) into the `Steps` list of your `MyFirstTreasureHunt` definition in the correct order.
        *   Set `Final Reward Message`: "You have recovered the Legendary Golden Artifact!"
        *   (Optional) Create a simple 3D Cube (Right-click in Hierarchy -> 3D Object -> Cube), drag it from Hierarchy to Project window to make it a prefab, then drag this prefab to the `Final Reward Prefab` slot on `MyFirstTreasureHunt`.
9.  **Assign Initial Hunt to Test Hunter:** Drag `MyFirstTreasureHunt` from your Project window into the `Initial Hunt` slot of the `_TestTreasureHunter` component.

### How to Run and Test:

1.  **Press the Play button** in the Unity Editor.
2.  **Observe the Console:** All progression and debug messages will appear here. If you set up the UI, you'll see updates there too.
3.  **Start the Hunt:** Press the **'S'** key to start `MyFirstTreasureHunt`.
4.  **Follow the Clues:**
    *   The console and UI will show the first step's description.
    *   To complete `Clue1_FindMap` ("AncientMap"), press **'1'**.
    *   To complete `Clue2_GoToCaves` ("WhisperingCaves"), press **'L'**.
    *   To complete `Clue3_TalkToHermit` ("OldHermit"), press **'I'**.
    *   To complete `Clue4_GetArtifact` ("GoldenKey"), press **'2'**.
5.  **Hunt Completion:** Once the last step is completed, the hunt will finish, and the final reward message will appear. If you assigned a `finalRewardPrefab`, a GameObject will be instantiated in your scene at `Vector3.zero`.
6.  **Fail Hunt:** At any point, you can press **'F'** to simulate failing the current hunt.

This example provides a robust, extensible, and practical foundation for building complex quest or objective systems in your Unity games.