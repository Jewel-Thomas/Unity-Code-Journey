// Unity Design Pattern Example: TutorialSystem
// This script demonstrates the TutorialSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **TutorialSystem design pattern**, emphasizing modularity, extensibility, and ease of use in real-world Unity projects.

The core idea is to separate tutorial logic into:
1.  **Tutorial Manager (Singleton):** Orchestrates the overall tutorial flow, displays UI, and handles progression.
2.  **Tutorial Steps (Scriptable Objects):** Define individual tutorial instructions, their content, and how they are completed.

This approach allows designers to create and reorder tutorial sequences purely in the Unity Editor without touching code, while developers can easily extend the types of tutorial steps.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Unity's UI Button component
using TMPro;        // Required for TextMeshProUGUI, make sure TextMeshPro is imported into your project
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// BASE TUTORIAL STEP SCRIPTABLE OBJECT
///
/// This is the abstract base class for all tutorial steps.
/// Using ScriptableObjects allows us to define tutorial content and behavior
/// as data assets in the Unity editor. This promotes modularity, reusability,
/// and makes it easy for designers to create and manage tutorial sequences
/// without modifying code.
///
/// Derived classes will implement specific ways a step can be activated and completed.
/// </summary>
[CreateAssetMenu(fileName = "NewBaseTutorialStep", menuName = "Tutorial System/Base Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [Header("Step Info")]
    public string stepTitle = "New Step";
    [TextArea(3, 6)]
    public string stepDescription = "Describe what the player needs to do or learn for this step.";

    [Header("UI Feedback")]
    [Tooltip("Optional: Drag a RectTransform (UI element) here. The TutorialManager can use this to highlight the relevant UI during this step.")]
    public RectTransform uiElementToHighlight;

    [Tooltip("Time delay (in seconds) before the next step automatically starts. Set to 0 for steps that require an explicit action or 'Next' button click.")]
    public float autoProgressDelay = 0f;

    // A reference to the TutorialManager instance. This is set by the manager
    // when a step becomes active, allowing the step to interact with the manager
    // (e.g., to request progression).
    protected TutorialManager _manager;

    /// <summary>
    /// Called by the TutorialManager when this step becomes the active tutorial step.
    /// Derived classes should override this to set up their specific conditions (e.g., subscribe to events).
    /// </summary>
    /// <param name="manager">The TutorialManager instance orchestrating the tutorial.</param>
    public virtual void Activate(TutorialManager manager)
    {
        _manager = manager;
        Debug.Log($"<color=cyan>Tutorial Step Activated:</color> {stepTitle}");
        
        // Update the main tutorial UI panel with this step's content.
        _manager.UpdateTutorialUI(stepTitle, stepDescription, uiElementToHighlight);
    }

    /// <summary>
    /// Called by the TutorialManager when this step is completed and the tutorial moves to the next step.
    /// Derived classes should override this to clean up any specific conditions (e.g., unsubscribe from events).
    /// </summary>
    public virtual void Complete()
    {
        Debug.Log($"<color=cyan>Tutorial Step Completed:</color> {stepTitle}");
        // Default cleanup logic: In a real system, you might have a UIHighlightManager
        // that removes active highlights here. For this example, we just log.
        if (uiElementToHighlight != null)
        {
            Debug.Log($"Removed highlight request for {uiElementToHighlight.name}");
        }
    }

    /// <summary>
    /// Determines if this tutorial step requires the 'Next' button on the tutorial UI to be visible.
    /// </summary>
    /// <returns>True if the 'Next' button should be shown, false otherwise.</returns>
    public virtual bool RequiresNextButton()
    {
        // By default, a step doesn't require a 'Next' button if it has an auto-progress delay.
        // If autoProgressDelay is 0, it implies some other form of progression (like 'Next' button).
        return autoProgressDelay <= 0;
    }
}


/// <summary>
/// ACTION-BASED TUTORIAL STEP SCRIPTABLE OBJECT
///
/// This type of step waits for a specific in-game action to occur,
/// which then triggers its completion. Game code will invoke the
/// `OnStepActionCompleted` UnityEvent when the action is detected.
/// </summary>
[CreateAssetMenu(fileName = "NewActionBasedStep", menuName = "Tutorial System/Action-Based Step")]
public class ActionBasedTutorialStepSO : TutorialStepSO
{
    [Header("Action-Based Step Settings")]
    [Tooltip("This UnityEvent must be invoked by game code (e.g., PlayerController.OnJump) to complete this step.")]
    public UnityEvent OnStepActionCompleted;

    public override void Activate(TutorialManager manager)
    {
        base.Activate(manager);
        // Ensure the event is initialized to prevent NullReferenceException if not set in inspector.
        if (OnStepActionCompleted == null) OnStepActionCompleted = new UnityEvent();
        
        // Subscribe to our own completion event. When this event is invoked by external code,
        // it will call OnActionTriggered, which then tells the manager to progress.
        OnStepActionCompleted.AddListener(OnActionTriggered);
        Debug.Log($"<color=yellow>Action-Based Step '{stepTitle}' waiting for action completion.</color>");
        
        // Action-based steps typically hide the 'Next' button, as completion is external.
        _manager.SetNextButtonVisibility(false);
    }

    public override void Complete()
    {
        base.Complete();
        // Always unsubscribe to prevent memory leaks and unwanted triggers.
        OnStepActionCompleted.RemoveListener(OnActionTriggered);
    }

    /// <summary>
    /// Called when the `OnStepActionCompleted` UnityEvent is invoked.
    /// This signals that the required in-game action has occurred.
    /// </summary>
    private void OnActionTriggered()
    {
        Debug.Log($"<color=lime>Action for step '{stepTitle}' detected. Completing step...</color>");
        _manager.ProgressToNextStep(); // Tell the manager to move to the next step.
    }

    /// <summary>
    /// Action-based steps do not show a 'Next' button by default.
    /// </summary>
    public override bool RequiresNextButton()
    {
        return false;
    }
}


/// <summary>
/// INTERACTION-BASED TUTORIAL STEP SCRIPTABLE OBJECT
///
/// This type of step requires the player to explicitly click a "Next" button
/// on the tutorial UI to progress. Useful for informational steps or when
/// you want the player to acknowledge something before moving on.
/// </summary>
[CreateAssetMenu(fileName = "NewInteractionBasedStep", menuName = "Tutorial System/Interaction-Based Step")]
public class InteractionBasedTutorialStepSO : TutorialStepSO
{
    [Header("Interaction-Based Step Settings")]
    [Tooltip("If true, the 'Next' button will be visible for this step.")]
    public bool showNextButton = true;

    public override void Activate(TutorialManager manager)
    {
        base.Activate(manager);
        Debug.Log($"<color=yellow>Interaction-Based Step '{stepTitle}' waiting for 'Next' button click.</color>");
        
        // Control the 'Next' button visibility based on this step's setting.
        _manager.SetNextButtonVisibility(showNextButton);
    }

    /// <summary>
    /// Interaction-based steps show the 'Next' button based on their `showNextButton` setting.
    /// </summary>
    public override bool RequiresNextButton()
    {
        return showNextButton;
    }
}


/// <summary>
/// TUTORIAL MANAGER (SINGLETON MONOBEHAVIOUR)
///
/// This is the central controller for the entire tutorial system.
/// It orchestrates the flow of tutorial steps, manages the tutorial UI,
/// and handles progression based on step requirements.
///
/// It implements the Singleton pattern to ensure there is only one
/// instance managing the tutorial at any given time.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // Singleton pattern: Provides a global access point to the single instance of TutorialManager.
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Steps")]
    [Tooltip("The ordered list of tutorial steps to guide the player through. Drag your TutorialStepSO assets here.")]
    [SerializeField] private List<TutorialStepSO> tutorialSteps = new List<TutorialStepSO>();

    [Header("UI Elements (Drag & Drop from your Canvas)")]
    [Tooltip("The parent GameObject for all tutorial UI elements. Will be activated/deactivated.")]
    [SerializeField] private GameObject tutorialUIPanel;
    [Tooltip("TextMeshPro text component for displaying the current step's title.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("TextMeshPro text component for displaying the current step's description.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Button to progress manually to the next step for interaction-based steps.")]
    [SerializeField] private Button nextStepButton;

    [Header("Events")]
    [Tooltip("Invoked when the entire tutorial sequence has been completed.")]
    public UnityEvent OnTutorialCompleted;

    // Internal state variables
    private int _currentStepIndex = -1; // -1 indicates tutorial hasn't started or is inactive.
    private TutorialStepSO _currentStep; // Reference to the currently active step ScriptableObject.
    private Coroutine _autoProgressCoroutine; // Reference to the coroutine for auto-progression.

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Singleton enforcement logic.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TutorialManagers found! Destroying duplicate.");
            Destroy(gameObject); // Destroy any new instance if one already exists.
        }
        else
        {
            Instance = this; // Assign this instance as the singleton.
            // Uncomment the line below if you want the tutorial manager to persist across scene loads.
            // DontDestroyOnLoad(gameObject); 
        }

        // Initialize UI state: Tutorial panel should be hidden initially.
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("TutorialManager: 'Tutorial UI Panel' is not assigned. Tutorial UI will not display!");
        }

        // Add listener for the 'Next' button if it exists.
        if (nextStepButton != null)
        {
            nextStepButton.onClick.AddListener(OnNextButtonClicked);
            nextStepButton.gameObject.SetActive(false); // Hide the button by default.
        }
        else
        {
            Debug.LogWarning("TutorialManager: 'Next Step Button' is not assigned. Interaction-based steps will not work.");
        }

        // Initialize OnTutorialCompleted event if null to prevent NREs when invoking.
        if (OnTutorialCompleted == null) OnTutorialCompleted = new UnityEvent();
    }

    private void OnDestroy()
    {
        // Clear the singleton instance if this object is destroyed.
        if (Instance == this)
        {
            Instance = null;
        }

        // Remove listener to prevent memory leaks, especially important for singletons.
        if (nextStepButton != null)
        {
            nextStepButton.onClick.RemoveListener(OnNextButtonClicked);
        }

        // Stop any running auto-progress coroutine to prevent errors if the manager is destroyed mid-step.
        if (_autoProgressCoroutine != null)
        {
            StopCoroutine(_autoProgressCoroutine);
            _autoProgressCoroutine = null;
        }
    }

    // --- Public API for Tutorial Control ---

    /// <summary>
    /// Starts the tutorial sequence from the very beginning.
    /// This is the entry point for activating the tutorial.
    /// </summary>
    public void StartTutorial()
    {
        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            Debug.LogWarning("No tutorial steps assigned. Tutorial cannot start and will immediately complete.");
            OnTutorialCompleted?.Invoke(); // Immediately complete if no steps.
            return;
        }

        Debug.Log("<color=green>Starting Tutorial System...</color>");
        _currentStepIndex = -1; // Reset index to ensure it starts from the first step (index 0).
        ProgressToNextStep();   // Move to the first step.
    }

    /// <summary>
    /// Progresses the tutorial to the next step in the sequence.
    /// This method is called internally by the manager or by active tutorial steps,
    /// but can also be called externally to force progression (use with caution).
    /// </summary>
    public void ProgressToNextStep()
    {
        // First, complete the current step if one is active. This allows the step
        // to perform any necessary cleanup (e.g., unsubscribe from events).
        if (_currentStep != null)
        {
            _currentStep.Complete();
            // Stop any auto-progress coroutine from the previous step.
            if (_autoProgressCoroutine != null)
            {
                StopCoroutine(_autoProgressCoroutine);
                _autoProgressCoroutine = null;
            }
        }

        _currentStepIndex++; // Advance to the next step index.

        // Check if all steps are completed.
        if (_currentStepIndex >= tutorialSteps.Count)
        {
            EndTutorial(); // If no more steps, end the tutorial.
            return;
        }

        // Activate the next step.
        _currentStep = tutorialSteps[_currentStepIndex];
        if (_currentStep != null)
        {
            // Pass a reference to this manager to the step, so the step can interact with it.
            _currentStep.Activate(this); 
            tutorialUIPanel?.SetActive(true); // Ensure the tutorial UI is visible.

            // If the current step has an auto-progress delay and doesn't require a 'Next' button,
            // start a coroutine to automatically progress after the delay.
            if (_currentStep.autoProgressDelay > 0 && !_currentStep.RequiresNextButton())
            {
                _autoProgressCoroutine = StartCoroutine(AutoProgressAfterDelay(_currentStep.autoProgressDelay));
            }
        }
        else
        {
            Debug.LogError($"TutorialManager: Step at index {_currentStepIndex} is null. Skipping this step.");
            ProgressToNextStep(); // Try to progress to the next valid step if a null step is encountered.
        }
    }

    /// <summary>
    /// Ends the entire tutorial sequence, hiding the UI and invoking the OnTutorialCompleted event.
    /// </summary>
    public void EndTutorial()
    {
        if (_currentStep != null)
        {
            _currentStep.Complete(); // Ensure the last active step is completed.
        }
        _currentStep = null;        // Clear current step reference.
        _currentStepIndex = -1;     // Reset tutorial state.
        
        tutorialUIPanel?.SetActive(false); // Hide the tutorial UI panel.
        SetNextButtonVisibility(false);    // Ensure the 'Next' button is hidden.
        
        Debug.Log("<color=green>Tutorial System Completed!</color>");
        OnTutorialCompleted?.Invoke(); // Notify listeners that the tutorial has finished.
    }

    // --- Internal UI Management ---

    /// <summary>
    /// Updates the tutorial UI elements with the provided information for the current step.
    /// This method is called by the active TutorialStepSO.
    /// </summary>
    public void UpdateTutorialUI(string title, string description, RectTransform highlightTarget)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        // --- UI Highlight Logic (Conceptual) ---
        // In a full-fledged system, you would have a dedicated UIHighlightManager.
        // This manager would:
        // 1. Take the `highlightTarget` (RectTransform).
        // 2. Instantiate a highlight graphic (e.g., an Image with a frame or a pulsating effect).
        // 3. Position and size the highlight graphic to frame the `highlightTarget`.
        // 4. Potentially disable interaction on other UI elements (dimming background).
        // 5. When the step completes, the HighlightManager would remove the graphic.
        // For this example, we simply log which element *should* be highlighted.
        if (highlightTarget != null)
        {
            Debug.Log($"<color=yellow>TutorialManager: Requesting highlight for UI element:</color> {highlightTarget.name}");
            // Example: (DO NOT do this directly in a real project, use a dedicated manager!)
            // Image targetImage = highlightTarget.GetComponent<Image>();
            // if (targetImage != null) targetImage.color = Color.yellow; // This is destructive!
        }
        else
        {
            Debug.Log("TutorialManager: No specific UI element to highlight for this step.");
        }

        // Update 'Next' button visibility based on the current step's requirements.
        SetNextButtonVisibility(_currentStep.RequiresNextButton());
    }

    /// <summary>
    /// Sets the visibility of the 'Next' button on the tutorial UI.
    /// Called by TutorialStepSOs to control player interaction.
    /// </summary>
    public void SetNextButtonVisibility(bool isVisible)
    {
        if (nextStepButton != null && nextStepButton.gameObject.activeSelf != isVisible)
        {
            nextStepButton.gameObject.SetActive(isVisible);
        }
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Handler for when the 'Next' button on the tutorial UI is clicked.
    /// </summary>
    private void OnNextButtonClicked()
    {
        // Only progress if a step is active AND that step explicitly requires the 'Next' button.
        // This prevents accidental progression if, for example, an action-based step is active
        // but the 'Next' button somehow became visible.
        if (_currentStep != null && _currentStep.RequiresNextButton())
        {
            ProgressToNextStep();
        }
        else
        {
            Debug.LogWarning("Next button clicked but current step does not require it or is null. Ignoring.");
        }
    }

    /// <summary>
    /// Coroutine to automatically progress the tutorial after a specified delay.
    /// Used by steps with a `autoProgressDelay` greater than 0.
    /// </summary>
    private IEnumerator AutoProgressAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProgressToNextStep();
    }
}


/*
--- HOW TO SET UP AND USE THIS TUTORIAL SYSTEM IN UNITY ---

1.  **Import TextMeshPro:**
    *   In Unity, go to `Window > TextMeshPro > Import TMP Essential Resources`. This is required because the example uses `TMP_Text`.

2.  **Create a TutorialManager GameObject:**
    *   In your scene Hierarchy, create an empty GameObject (e.g., `_TutorialSystem`).
    *   Attach the `TutorialManager.cs` script to this GameObject.

3.  **Create UI Elements for the Tutorial:**
    *   **Canvas:** If you don't have one, create a UI Canvas: `GameObject > UI > Canvas`.
    *   **Tutorial Panel:** Inside your Canvas, create an empty GameObject (e.g., `TutorialPanel`).
        *   **Crucial:** Make sure `TutorialPanel` is initially **inactive** (uncheck its checkbox in the Inspector). The `TutorialManager` will activate it.
        *   You can add UI components like an `Image` for background, or `Vertical Layout Group` for organization.
    *   **Title Text:** Inside `TutorialPanel`, create a `TextMeshPro - Text (UI)` component (e.g., `TitleText`).
        *   Adjust its size, font, and color as desired.
    *   **Description Text:** Inside `TutorialPanel`, create another `TextMeshPro - Text (UI)` component (e.g., `DescriptionText`).
        *   Adjust its size, font, and color. Use `Overflow` or `Wrap` settings as needed.
    *   **Next Button:** Inside `TutorialPanel`, create a `Button (TextMeshPro)` component (e.g., `NextButton`).
        *   Change its text to "Next" or a relevant prompt.
        *   **Crucial:** Make sure `NextButton` is initially **inactive**. The `TutorialManager` will control its visibility.

4.  **Assign UI Elements in TutorialManager Inspector:**
    *   Select your `_TutorialSystem` GameObject in the Hierarchy.
    *   In the Inspector, drag the following UI GameObjects from your Canvas to the `Tutorial Manager` script's fields:
        *   `TutorialPanel` to `Tutorial UI Panel`.
        *   `TitleText` to `Title Text`.
        *   `DescriptionText` to `Description Text`.
        *   `NextButton` to `Next Step Button`.

5.  **Create Tutorial Step Scriptable Objects:**
    *   In your Project window, create a new folder (e.g., `Assets/TutorialSteps`).
    *   Right-click in this folder, then go to `Create > Tutorial System`.
    *   Choose the type of step you need:
        *   **`Base Tutorial Step`**: For simple informational steps that either:
            *   Auto-progress after a `autoProgressDelay` (if > 0).
            *   Require the "Next" button if `autoProgressDelay` is 0.
        *   **`Action-Based Step`**: For steps that complete when a specific in-game action happens (e.g., player jumps, collects item).
        *   **`Interaction-Based Step`**: For steps that require the player to click the "Next" button on the tutorial UI to proceed.

    *   **Example Step Creation:**
        *   **Step 1: Welcome Message (Interaction-Based)**
            *   Create `Interaction-Based Step` named "Welcome_Step".
            *   Title: "Welcome, Adventurer!"
            *   Description: "Embark on an epic journey! Click 'Next' to begin your training."
            *   Show Next Button: `true`
        *   **Step 2: Movement Instruction (Action-Based)**
            *   Create `Action-Based Step` named "MovePlayer_Step".
            *   Title: "Learn to Move"
            *   Description: "Use WASD or the Arrow Keys to move your character. Try moving around a bit!"
            *   (Note: `Show Next Button` is not available/relevant for Action-Based steps.)
            *   **Crucial for Action Steps:** The `On Step Action Completed` event on this ScriptableObject will need to be triggered by your game code.
        *   **Step 3: Collect Item (Action-Based)**
            *   Create `Action-Based Step` named "CollectItem_Step".
            *   Title: "Collect a Shard"
            *   Description: "Find the glowing blue shard nearby and walk over it to collect it."
        *   **Step 4: Congrats Message (Base Tutorial Step with Auto-Progress)**
            *   Create `Base Tutorial Step` named "Congrats_Step".
            *   Title: "Training Complete!"
            *   Description: "You've mastered the basics. Good luck on your quest!"
            *   Auto Progress Delay: `3` (This will show for 3 seconds then the tutorial ends.)

6.  **Assign Steps to TutorialManager:**
    *   Select your `_TutorialSystem` GameObject in the Hierarchy.
    *   In the Inspector, locate the `Tutorial Steps` list.
    *   Drag your created `TutorialStepSO` assets into this list in the desired order of your tutorial.

7.  **Trigger the Tutorial to Start:**
    *   From another script (e.g., your `GameManager`, `PlayerSpawner`, or a simple test script's `Start()` method), call:
        ```csharp
        // Example: GameManager.cs or a TestScript.cs
        public class GameInitializer : MonoBehaviour
        {
            void Start()
            {
                // It's often good practice to wait a frame or a short delay
                // to ensure all other systems (like UI) are fully initialized.
                StartCoroutine(DelayedTutorialStart(0.5f)); 
            }

            IEnumerator DelayedTutorialStart(float delay)
            {
                yield return new WaitForSeconds(delay);
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.StartTutorial();
                }
            }

            // You can also subscribe to the tutorial completion event to react when it's done.
            void OnEnable()
            {
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.OnTutorialCompleted.AddListener(HandleTutorialFinished);
                }
            }

            void OnDisable()
            {
                // Always remove listeners to prevent memory leaks, especially important for singletons.
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.OnTutorialCompleted.RemoveListener(HandleTutorialFinished);
                }
            }

            void HandleTutorialFinished()
            {
                Debug.Log("Tutorial completed! The player is now free to explore.");
                // e.g., enable main player input, show game menu, save tutorial progress.
            }
        }
        ```

8.  **Trigger Action-Based Steps from Game Logic:**
    *   For `ActionBasedTutorialStepSO`s, you need to invoke their `OnStepActionCompleted` event from the relevant game logic script.
    *   **Important:** Your game logic script needs a direct reference to the specific `ActionBasedTutorialStepSO` asset. Drag the ScriptableObject asset from your Project window into a `[SerializeField]` field on your script.

    ```csharp
    // Example: PlayerMovement.cs
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Tutorial Integration")]
        [Tooltip("Drag your 'MovePlayer_Step' (ActionBasedTutorialStepSO) asset here.")]
        [SerializeField] private ActionBasedTutorialStepSO movePlayerTutorialStep;
        
        private bool _hasMovedInStep = false; // Flag to ensure action only triggers once per step.

        void Update()
        {
            // Detect player input for movement
            if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
            {
                // If the movement tutorial step is currently active and the player hasn't moved yet for this step...
                if (movePlayerTutorialStep != null && !_hasMovedInStep)
                {
                    // Check if this specific step is currently active within the TutorialManager.
                    // (This check is not strictly necessary if you're sure about timing,
                    // but it prevents invoking events of inactive steps).
                    // The TutorialManager will only progress if the step is active and listening.
                    Debug.Log("Player moved, attempting to complete tutorial action!");
                    movePlayerTutorialStep.OnStepActionCompleted?.Invoke();
                    _hasMovedInStep = true; // Mark as moved for this step.
                }
            }
            else
            {
                // Reset flag if player stops moving, allowing the action to be triggered again
                // if the tutorial step is somehow reset or restarted.
                _hasMovedInStep = false;
            }
        }
    }
    
    // Example: ItemCollection.cs
    public class Item : MonoBehaviour
    {
        [Header("Tutorial Integration")]
        [Tooltip("Drag your 'CollectItem_Step' (ActionBasedTutorialStepSO) asset here.")]
        [SerializeField] private ActionBasedTutorialStepSO collectItemTutorialStep;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"Item {gameObject.name} collected by Player.");
                // Invoke the event for the collection tutorial step.
                // The TutorialManager will only respond if this is the active step.
                if (collectItemTutorialStep != null)
                {
                    collectItemTutorialStep.OnStepActionCompleted?.Invoke();
                }
                Destroy(gameObject); // Remove item from scene.
            }
        }
    }
    ```

This setup provides a flexible, data-driven tutorial system that is easy to extend and manage without writing complex state machine code directly in the manager.
*/
```