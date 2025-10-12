// Unity Design Pattern Example: NPCDialogueScheduling
// This script demonstrates the NPCDialogueScheduling pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'NPCDialogueScheduling' design pattern in Unity focuses on managing and orchestrating when and how Non-Player Characters (NPCs) deliver their dialogue. This pattern is crucial in games with multiple NPCs, branching storylines, or dynamic events, ensuring that dialogue is delivered logically, without overlaps, and according to game state or player interactions.

**Key Principles of NPCDialogueScheduling:**

1.  **Decoupling:** Separates the concerns of *what* dialogue an NPC has, *when* it should be said, and *how* it's displayed.
2.  **Centralized Management:** A central scheduler handles all dialogue requests, preventing multiple NPCs from speaking at once and managing a queue of upcoming dialogues.
3.  **Prioritization & Rules:** The scheduler can implement rules like priority (important quest dialogue over ambient chatter), cooldowns, or conditions (e.g., only speak if the player is nearby).
4.  **Flexibility:** Allows different NPCs to easily register their dialogue, and the display method can be swapped without affecting the core scheduling logic.

---

### Complete C# Unity Example: NPCDialogueScheduling

This example provides a robust and practical implementation ready to be dropped into a Unity project.

**Project Setup Requirements:**
1.  **TextMeshPro:** Ensure TextMeshPro is imported into your project (Window > TextMeshPro > Import TMP Essential Resources).
2.  **UI Canvas:** You'll need a Canvas with TextMeshProUGUI elements for dialogue display.
3.  **Player Tag:** Your player GameObject should have the tag "Player".
4.  **NPC Collider:** NPCs using `NPCDialogueSource` for proximity triggers need a `Collider` component with `Is Trigger` enabled and optionally a `Rigidbody` (set to `Is Kinematic`).

---

**1. Dialogue Data Structures (`DialogueLine.cs`, `DialogueSequenceSO.cs`)**
These define the content of the dialogue.

**`DialogueLine.cs`**
```csharp
using UnityEngine;

/// <summary>
/// Represents a single line of dialogue, including the speaker's name, text,
/// and how long it should be displayed.
/// </summary>
[System.Serializable]
public struct DialogueLine
{
    [Tooltip("The name of the character speaking this line.")]
    public string speakerName;

    [Tooltip("The actual text of the dialogue line.")]
    [TextArea(3, 5)] // Makes the string field a larger text area in the Inspector
    public string text;

    [Tooltip("How long this line should be displayed before moving to the next. " +
             "Set to 0 for player input to advance (e.g., mouse click or spacebar).")]
    public float displayDuration;
}
```

**`DialogueSequenceSO.cs`**
```csharp
using UnityEngine;

/// <summary>
/// A ScriptableObject representing a complete sequence of dialogue lines.
/// This allows designers to easily create and manage dialogue assets in the editor.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequenceSO : ScriptableObject
{
    [Tooltip("A unique identifier or descriptive name for this dialogue sequence.")]
    public string sequenceName;

    [Tooltip("The array of dialogue lines that make up this sequence.")]
    public DialogueLine[] lines;

    [Tooltip("If true, this dialogue sequence will only play once per game session.")]
    public bool playOnce;

    [HideInInspector] 
    // This flag is used at runtime to track if a 'playOnce' dialogue has already been played.
    // It is generally managed by the DialogueScheduler.
    public bool hasPlayed; 

    /// <summary>
    /// Resets the 'hasPlayed' flag. Useful for editor testing or starting a new game session.
    /// This method is typically called via a custom editor button or at specific game state events.
    /// </summary>
    public void ResetPlayedStatus()
    {
        hasPlayed = false;
        Debug.Log($"Dialogue Sequence '{sequenceName}' reset: hasPlayed = {hasPlayed}");
    }
}
```

---

**2. Dialogue Display Interface & Implementation (`IDialogueDisplay.cs`, `UnityUIDialogueDisplay.cs`)**
This defines *how* dialogue is shown. The `DialogueScheduler` uses the interface, remaining agnostic to the specific UI technology.

**`IDialogueDisplay.cs`**
```csharp
using System;

/// <summary>
/// Interface for any object responsible for displaying dialogue to the player.
/// This decouples the dialogue scheduling logic from the specific UI implementation.
/// </summary>
public interface IDialogueDisplay
{
    /// <summary>
    /// Called to show a single dialogue line to the player.
    /// </summary>
    /// <param name="line">The DialogueLine to be displayed.</param>
    void ShowDialogue(DialogueLine line);

    /// <summary>
    /// Called to hide the dialogue UI, typically when a sequence ends or is interrupted.
    /// </summary>
    void HideDialogue();

    /// <summary>
    /// Event that fires when the currently displayed dialogue line has finished
    /// its display duration or has been advanced by player input.
    /// </summary>
    event Action OnLineCompleted;

    /// <summary>
    /// Property indicating whether the dialogue display is currently active
    /// (i.e., displaying a line).
    /// </summary>
    bool IsDisplaying { get; }

    /// <summary>
    /// Forces the current dialogue line to complete immediately, skipping any
    /// typing animation or remaining display duration.
    /// </summary>
    void ForceCompleteCurrentLine();
}
```

**`UnityUIDialogueDisplay.cs`**
```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Collections;
using System;

/// <summary>
/// Concrete implementation of IDialogueDisplay using Unity's UI (TextMeshPro).
/// This component should be placed on a GameObject within your Canvas hierarchy.
/// </summary>
public class UnityUIDialogueDisplay : MonoBehaviour, IDialogueDisplay
{
    [Header("UI Elements")]
    [Tooltip("TextMeshProUGUI component for displaying the speaker's name.")]
    public TextMeshProUGUI speakerNameText;
    [Tooltip("TextMeshProUGUI component for displaying the dialogue text itself.")]
    public TextMeshProUGUI dialogueText;
    [Tooltip("The UI Panel or GameObject that contains all dialogue UI elements. " +
             "This will be activated/deactivated to show/hide the dialogue.")]
    public GameObject dialoguePanel;

    [Header("Settings")]
    [Tooltip("Time it takes for each character to appear, creating a typing effect. " +
             "Set to 0 for instant text display.")]
    public float typingSpeed = 0.03f; 

    private Coroutine _currentTypingRoutine;
    private DialogueLine _currentLine;
    private bool _isDisplaying; // Tracks if a line is currently being shown.

    /// <summary>
    /// True if the dialogue display is currently active and showing a line.
    /// </summary>
    public bool IsDisplaying => _isDisplaying;

    /// <summary>
    /// Event fired when the current dialogue line has completed its display (duration or player input).
    /// </summary>
    public event Action OnLineCompleted;

    void Awake()
    {
        // Ensure the dialogue panel is hidden at the start.
        HideDialogue();
    }

    /// <summary>
    /// Displays a single dialogue line with a typing effect and manages its duration.
    /// </summary>
    /// <param name="line">The dialogue line to display.</param>
    public void ShowDialogue(DialogueLine line)
    {
        _currentLine = line;
        _isDisplaying = true; // Mark as displaying
        dialoguePanel.SetActive(true); // Make the dialogue UI visible

        speakerNameText.text = line.speakerName;
        dialogueText.text = ""; // Clear for the typing effect

        // Stop any previous typing routine to prevent conflicts.
        if (_currentTypingRoutine != null)
        {
            StopCoroutine(_currentTypingRoutine);
        }
        _currentTypingRoutine = StartCoroutine(TypeSentence(line.text, line.displayDuration));
    }

    /// <summary>
    /// Hides the dialogue UI and stops any ongoing display routines.
    /// </summary>
    public void HideDialogue()
    {
        if (_currentTypingRoutine != null)
        {
            StopCoroutine(_currentTypingRoutine);
            _currentTypingRoutine = null;
        }
        _isDisplaying = false; // Mark as not displaying
        dialoguePanel.SetActive(false); // Hide the dialogue UI
        speakerNameText.text = "";
        dialogueText.text = "";
    }

    /// <summary>
    /// Coroutine for typing out a sentence character by character and managing its display duration.
    /// </summary>
    private IEnumerator TypeSentence(string sentence, float duration)
    {
        // Typing effect: reveal characters one by one.
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Wait for the specified duration or player input.
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
        }
        else // If duration is 0, wait indefinitely for player input to advance.
        {
            // In a real game, this would typically involve a specific input manager
            // event for advancing dialogue. For simplicity, we use mouse click or spacebar.
            while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space))
            {
                yield return null; // Wait until a relevant input is detected.
            }
        }

        // The line has finished displaying.
        _isDisplaying = false; // Mark as not displaying (for this specific line)
        OnLineCompleted?.Invoke(); // Notify the scheduler that this line is done.
    }

    /// <summary>
    /// Immediately completes the current dialogue line, skipping typing and duration.
    /// This is typically called by player input to speed up dialogue.
    /// </summary>
    public void ForceCompleteCurrentLine()
    {
        // Stop the typing coroutine if it's running.
        if (_currentTypingRoutine != null)
        {
            StopCoroutine(_currentTypingRoutine);
            _currentTypingRoutine = null;
        }

        // If a line was being displayed, mark it as completed and notify.
        if (_isDisplaying)
        {
            _isDisplaying = false;
            // Ensure the full text is shown if it wasn't already.
            dialogueText.text = _currentLine.text; 
            OnLineCompleted?.Invoke();
        }
    }

    // Example of how player input can force-complete a line.
    // In a real project, this would be tied to your custom input system.
    void Update()
    {
        if (_isDisplaying && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            // If the text is still typing, this will complete the typing animation.
            // If typing is done and duration > 0, this will complete the line immediately.
            // If typing is done and duration == 0, this will advance to the next line.
            ForceCompleteCurrentLine();
        }
    }
}
```

---

**3. Dialogue Request & Scheduler (`DialogueRequest.cs`, `DialogueScheduler.cs`)**
This is the core of the NPCDialogueScheduling pattern. The scheduler manages incoming requests and handles their execution.

**`DialogueRequest.cs`**
```csharp
using UnityEngine;
using System;

/// <summary>
/// A data class representing a request to play a dialogue sequence.
/// Used internally by the DialogueScheduler to manage its queue.
/// </summary>
public class DialogueRequest
{
    [Tooltip("The DialogueSequenceSO to be played.")]
    public DialogueSequenceSO sequence;

    [Tooltip("The GameObject that requested this dialogue (e.g., the NPC). " +
             "Useful for logging or context-aware callbacks.")]
    public GameObject requester;

    [Tooltip("Priority level of this dialogue request. Higher values mean higher priority. " +
             "Not fully implemented in this basic example's queue, but a placeholder for " +
             "more advanced scheduling.")]
    public int priority;

    [Tooltip("An optional callback action to be invoked when this entire dialogue sequence finishes.")]
    public Action onSequenceFinishedCallback;

    public DialogueRequest(DialogueSequenceSO seq, GameObject req, int prio, Action callback = null)
    {
        sequence = seq;
        requester = req;
        priority = prio;
        onSequenceFinishedCallback = callback;
    }
}
```

**`DialogueScheduler.cs`**
```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // Required for Action

/// <summary>
/// The central manager for the NPCDialogueScheduling pattern.
/// It acts as a singleton, handling all dialogue requests, queueing them,
/// and orchestrating their display through an IDialogueDisplay implementation.
/// </summary>
public class DialogueScheduler : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Assign an object in the scene that implements IDialogueDisplay " +
             "(e.g., UnityUIDialogueDisplay on your Canvas).")]
    [SerializeField]
    private MonoBehaviour _dialogueDisplayObject; // Allows assigning a GameObject in Inspector.
    private IDialogueDisplay _dialogueDisplay;

    // Singleton pattern for easy access from anywhere in the game.
    public static DialogueScheduler Instance { get; private set; }

    private Queue<DialogueRequest> _dialogueQueue = new Queue<DialogueRequest>();
    private Coroutine _currentDialogueSequenceRoutine;
    private bool _isDialogueActive; // True if a dialogue sequence is currently playing.

    /// <summary>
    /// Property indicating whether any dialogue sequence is currently active.
    /// </summary>
    public bool IsDialogueActive => _isDialogueActive;

    void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances.
        }
        else
        {
            Instance = this;
            // Optionally, uncomment to make the scheduler persist across scene loads.
            // DontDestroyOnLoad(gameObject); 
        }

        // Validate and assign the dialogue display interface.
        if (_dialogueDisplayObject == null)
        {
            Debug.LogError("DialogueScheduler: No IDialogueDisplay object assigned! Dialogue cannot be shown.");
            return;
        }
        _dialogueDisplay = _dialogueDisplayObject as IDialogueDisplay;
        if (_dialogueDisplay == null)
        {
            Debug.LogError("DialogueScheduler: Assigned object does not implement IDialogueDisplay! " +
                           "Please ensure it implements the IDialogueDisplay interface.");
            _dialogueDisplayObject = null; // Clear invalid assignment.
        }
        else
        {
            // Subscribe to the event that signals a single dialogue line has completed.
            _dialogueDisplay.OnLineCompleted += OnDialogueLineCompleted;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when the scheduler is destroyed.
        if (_dialogueDisplay != null)
        {
            _dialogueDisplay.OnLineCompleted -= OnDialogueLineCompleted;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Schedules a dialogue sequence to be played. The scheduler will manage
    /// when it's actually displayed based on queue, priority, and current active dialogue.
    /// </summary>
    /// <param name="sequence">The DialogueSequenceSO to play.</param>
    /// <param name="requester">The GameObject that requested the dialogue (e.g., the NPC).</param>
    /// <param name="priority">Higher priority dialogues are handled sooner. (Currently, FIFO queue is used, but this is a placeholder).</param>
    /// <param name="onFinishedCallback">An optional action to call when the entire sequence finishes.</param>
    /// <returns>True if the dialogue was successfully queued, false otherwise (e.g., if display is not set up).</returns>
    public bool ScheduleDialogue(DialogueSequenceSO sequence, GameObject requester, int priority = 0, Action onFinishedCallback = null)
    {
        if (_dialogueDisplay == null)
        {
            Debug.LogWarning($"DialogueScheduler: Cannot schedule dialogue '{sequence.sequenceName}' from '{requester.name}' " +
                             "because IDialogueDisplay is not set up or invalid.");
            return false;
        }

        // Handle 'playOnce' logic.
        if (sequence.playOnce && sequence.hasPlayed)
        {
            Debug.Log($"DialogueScheduler: Dialogue '{sequence.sequenceName}' is set to play once and has already played. Skipping request from '{requester.name}'.");
            return false;
        }

        // Enqueue the new dialogue request.
        _dialogueQueue.Enqueue(new DialogueRequest(sequence, requester, priority, onFinishedCallback));
        Debug.Log($"DialogueScheduler: Queued dialogue '{sequence.sequenceName}' from '{requester.name}'. Queue size: {_dialogueQueue.Count}");

        // If no dialogue is currently active, immediately try to start the next one.
        if (!_isDialogueActive)
        {
            StartNextDialogue();
        }
        return true;
    }

    /// <summary>
    /// Immediately stops any currently playing dialogue and clears the entire queue.
    /// Use with caution, as it will interrupt ongoing conversations.
    /// </summary>
    public void StopAllDialogue()
    {
        if (_currentDialogueSequenceRoutine != null)
        {
            StopCoroutine(_currentDialogueSequenceRoutine);
            _currentDialogueSequenceRoutine = null;
        }
        _dialogueQueue.Clear();
        _isDialogueActive = false;
        _dialogueDisplay?.HideDialogue(); // Hide the UI.
        Debug.Log("DialogueScheduler: All dialogue stopped and queue cleared.");
    }

    /// <summary>
    /// Tries to start the next dialogue sequence from the queue.
    /// This is called when the current dialogue finishes or when a new request comes in and nothing is playing.
    /// </summary>
    private void StartNextDialogue()
    {
        if (_isDialogueActive || _dialogueQueue.Count == 0)
        {
            return; // Already playing a dialogue or no dialogues in the queue.
        }

        DialogueRequest nextRequest = _dialogueQueue.Dequeue(); // Get the next request.
        
        // Mark the sequence as played if it's a 'playOnce' sequence.
        // This is done BEFORE playing in case the sequence itself causes a re-queue.
        if (nextRequest.sequence.playOnce)
        {
            nextRequest.sequence.hasPlayed = true; 
        }

        _currentDialogueSequenceRoutine = StartCoroutine(PlayDialogueSequence(nextRequest));
    }

    /// <summary>
    /// Coroutine to play an entire dialogue sequence, line by line.
    /// It communicates with the IDialogueDisplay to show each line.
    /// </summary>
    /// <param name="request">The DialogueRequest containing the sequence to play.</param>
    private IEnumerator PlayDialogueSequence(DialogueRequest request)
    {
        _isDialogueActive = true; // Mark scheduler as active.
        Debug.Log($"DialogueScheduler: Starting dialogue sequence '{request.sequence.sequenceName}' from '{request.requester.name}'.");

        // Iterate through each line in the sequence.
        for (int i = 0; i < request.sequence.lines.Length; i++)
        {
            DialogueLine currentLine = request.sequence.lines[i];
            _dialogueDisplay.ShowDialogue(currentLine); // Tell the UI to show the line.

            // Wait until the current line has finished displaying (as reported by the IDialogueDisplay).
            // The UnityUIDialogueDisplay sets IsDisplaying to false and invokes OnLineCompleted when a line finishes.
            yield return new WaitUntil(() => !_dialogueDisplay.IsDisplaying);
            // The OnDialogueLineCompleted event handler doesn't need to do anything specific here,
            // as the WaitUntil condition handles the flow.
        }

        // The entire sequence has finished.
        Debug.Log($"DialogueScheduler: Finished dialogue sequence '{request.sequence.sequenceName}'.");
        _dialogueDisplay.HideDialogue(); // Hide the dialogue UI.
        _isDialogueActive = false; // Mark scheduler as inactive.
        request.onSequenceFinishedCallback?.Invoke(); // Invoke any callback provided by the requester.

        // After a sequence finishes, check if there are more dialogues in the queue to play.
        StartNextDialogue();
    }

    /// <summary>
    /// Event handler for when a single dialogue line completes displaying.
    /// This method is subscribed to the IDialogueDisplay's OnLineCompleted event.
    /// It primarily serves to allow the PlayDialogueSequence coroutine to advance.
    /// </summary>
    private void OnDialogueLineCompleted()
    {
        // The PlayDialogueSequence coroutine is waiting for !_dialogueDisplay.IsDisplaying.
        // This event essentially signals that the coroutine can proceed to the next line or end.
        // No explicit logic needed here beyond allowing the `WaitUntil` to resolve.
    }
}
```

---

**4. NPC Dialogue Source (`NPCDialogueSource.cs`)**
This component is placed on NPCs and acts as their entry point for requesting dialogue from the `DialogueScheduler`.

**`NPCDialogueSource.cs`**
```csharp
using UnityEngine;
using System; // Required for Action

/// <summary>
/// Component placed on an NPC that allows it to trigger dialogue.
/// It demonstrates how an NPC would interact with the DialogueScheduler to request conversations.
/// Requires a Collider (set to Is Trigger) and Rigidbody (can be kinematic) for proximity checks.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure the NPC has a collider for triggers.
public class NPCDialogueSource : MonoBehaviour
{
    [Header("Dialogue Configuration")]
    [Tooltip("The default dialogue sequence this NPC will play when triggered (e.g., by proximity or 'E' key).")]
    public DialogueSequenceSO defaultDialogue;
    
    [Tooltip("Other dialogue sequences this NPC can play. These would typically be triggered " +
             "by specific game events, quests, or other game logic.")]
    public DialogueSequenceSO[] additionalDialogues;

    [Header("Trigger Settings")]
    [Tooltip("If true, the NPC will use OnTriggerEnter/Exit to detect the player.")]
    public bool triggerOnProximity = true;
    
    [Tooltip("The tag of the GameObject considered as the 'Player' for proximity checks.")]
    public string playerTag = "Player";
    
    [Tooltip("The cooldown duration (in seconds) after this NPC plays dialogue " +
             "before it can play another sequence (from this source).")]
    public float dialogueCooldown = 5f;

    private float _lastDialogueTime; // Tracks when this NPC last initiated dialogue.
    private bool _playerInRange; // True if the player is currently within the NPC's trigger collider.

    // Optional: Add a visual indicator for dialogue availability (e.g., an exclamation mark over the NPC)
    // [SerializeField] private GameObject dialogueIndicator; 

    void Start()
    {
        // Initialize cooldown to allow dialogue immediately at the start of the game.
        _lastDialogueTime = -dialogueCooldown; 

        // Ensure a Rigidbody is present for trigger events to work if not already there.
        // It can be kinematic if the NPC doesn't need physics simulation.
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // if (dialogueIndicator != null) dialogueIndicator.SetActive(false);
    }

    /// <summary>
    /// Public method to request a specific dialogue sequence to be scheduled.
    /// This is the primary way other systems (like quest managers or player input)
    /// would tell this NPC to speak.
    /// </summary>
    /// <param name="sequenceToPlay">The DialogueSequenceSO to be played.</param>
    public void TriggerDialogue(DialogueSequenceSO sequenceToPlay)
    {
        // Check if the scheduler exists.
        if (DialogueScheduler.Instance == null)
        {
            Debug.LogError("NPCDialogueSource: DialogueScheduler.Instance not found in scene! " +
                           "Please ensure a DialogueScheduler GameObject exists.");
            return;
        }

        // Apply cooldown logic.
        if (Time.time < _lastDialogueTime + dialogueCooldown)
        {
            Debug.Log($"NPCDialogueSource ({gameObject.name}): Dialogue on cooldown. " +
                      $"Remaining: {(_lastDialogueTime + dialogueCooldown - Time.time):F1}s.");
            return;
        }

        // Define an optional callback for when this specific dialogue sequence finishes.
        Action onFinished = () => Debug.Log($"NPCDialogueSource ({gameObject.name}): " +
                                            $"Dialogue sequence '{sequenceToPlay.sequenceName}' finished.");

        // Request the dialogue from the scheduler.
        if (DialogueScheduler.Instance.ScheduleDialogue(sequenceToPlay, gameObject, 0, onFinished))
        {
            _lastDialogueTime = Time.time; // Update last dialogue time only if successfully scheduled.
            // if (dialogueIndicator != null) dialogueIndicator.SetActive(false); // Hide indicator while talking.
        }
    }

    /// <summary>
    /// Convenience method to trigger the NPC's default dialogue sequence.
    /// </summary>
    public void TriggerDefaultDialogue()
    {
        if (defaultDialogue != null)
        {
            TriggerDialogue(defaultDialogue);
        }
        else
        {
            Debug.LogWarning($"NPCDialogueSource ({gameObject.name}): No default dialogue assigned.");
        }
    }

    // --- Proximity Trigger Logic ---
    // Requires a Collider component with 'Is Trigger' enabled on this GameObject.

    void OnTriggerEnter(Collider other)
    {
        if (!triggerOnProximity || defaultDialogue == null) return;

        if (other.CompareTag(playerTag))
        {
            _playerInRange = true;
            // if (dialogueIndicator != null) dialogueIndicator.SetActive(true); // Show indicator
            Debug.Log($"NPCDialogueSource ({gameObject.name}): Player entered range.");

            // Optionally, trigger dialogue immediately on entering range.
            // For a "talk to me" prompt, you might wait for an explicit input instead.
            TriggerDefaultDialogue(); 
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!triggerOnProximity) return;

        if (other.CompareTag(playerTag))
        {
            _playerInRange = false;
            // if (dialogueIndicator != null) dialogueIndicator.SetActive(false); // Hide indicator
            Debug.Log($"NPCDialogueSource ({gameObject.name}): Player left range.");
            
            // Optional: If the player leaves range during dialogue, you might want to stop it.
            // DialogueScheduler.Instance.StopAllDialogue(); // Might be too aggressive for some games.
        }
    }

    // Example of how to trigger dialogue via player input while in range.
    // In a real project, this would be tied to your custom input system.
    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E) && defaultDialogue != null)
        {
            Debug.Log($"NPCDialogueSource ({gameObject.name}): 'E' pressed by player.");
            TriggerDefaultDialogue();
        }
    }
}
```

---

### Example Unity Scene Setup and Usage:

1.  **Create Dialogue Sequences (ScriptableObjects):**
    *   In your Project window, right-click -> Create -> Dialogue -> Dialogue Sequence.
    *   Name it `NPC1_IntroDialogue`.
    *   Fill in its `Lines` array:
        *   **Line 0:** `Speaker: Villager`, `Text: Hello, adventurer! Welcome to our humble village.`, `Duration: 3`
        *   **Line 1:** `Speaker: Villager`, `Text: Press Space or click to continue...`, `Duration: 0` (Requires player input)
        *   **Line 2:** `Speaker: Villager`, `Text: Be careful, the forest beyond is full of dangers.`, `Duration: 4`
    *   Create another one, `NPC1_QuestDialogue`, and set `Play Once` to true:
        *   **Line 0:** `Speaker: Villager`, `Text: I need help with the lost artifact!`, `Duration: 3`
        *   **Line 1:** `Speaker: Villager`, `Text: It's somewhere in the ancient ruins. Will you go?`, `Duration: 0`
    *   Create a `NPC2_Greeting`:
        *   **Line 0:** `Speaker: Merchant`, `Text: Psst! Over here, traveler. Looking for goods?`, `Duration: 3`

2.  **Create the Dialogue UI:**
    *   Right-click in Hierarchy -> UI -> Canvas. Name it `DialogueCanvas`.
    *   Inside `DialogueCanvas`, right-click -> UI -> Panel. Name it `DialoguePanel`. This will be your `dialoguePanel`. Scale it and position it at the bottom of the screen.
    *   Inside `DialoguePanel`, right-click -> UI -> Text - TextMeshPro. Name it `SpeakerNameText`. Position it above the dialogue text. Set font size, color. This will be your `speakerNameText`.
    *   Inside `DialoguePanel`, right-click -> UI -> Text - TextMeshPro. Name it `DialogueText`. Position it centrally within the panel. Set font size, color, enable word wrapping. This will be your `dialogueText`.
    *   Make sure `DialoguePanel` is initially **inactive** in the Inspector.

3.  **Create the Dialogue Scheduler:**
    *   Create an empty GameObject in your scene. Name it `DialogueScheduler`.
    *   Add the `DialogueScheduler.cs` script to it.
    *   Drag your `DialoguePanel`'s `UnityUIDialogueDisplay` component (which you'll add next) to the `_dialogueDisplayObject` slot.

4.  **Add `UnityUIDialogueDisplay` to your UI Panel:**
    *   Select your `DialoguePanel` (or a dedicated `DialogueDisplay` GameObject within the Canvas).
    *   Add the `UnityUIDialogueDisplay.cs` script to it.
    *   Drag `SpeakerNameText` to the `Speaker Name Text` slot.
    *   Drag `DialogueText` to the `Dialogue Text` slot.
    *   Drag `DialoguePanel` itself to the `Dialogue Panel` slot.
    *   (Optional) Adjust `Typing Speed`.

5.  **Create NPCs:**
    *   Create a 3D object (e.g., a Cube) and name it `NPC_Villager`.
    *   Add a `Box Collider` to it. Set `Is Trigger` to true. Adjust its size to be a bit larger than the NPC model to represent interaction range.
    *   Add a `Rigidbody` component. Set `Is Kinematic` to true.
    *   Add the `NPCDialogueSource.cs` script to `NPC_Villager`.
    *   Drag your `NPC1_IntroDialogue` ScriptableObject to the `Default Dialogue` slot.
    *   Drag your `NPC1_QuestDialogue` to the `Additional Dialogues` array (e.g., element 0).
    *   Create another 3D object, name it `NPC_Merchant`.
    *   Add Collider and Rigidbody as above.
    *   Add `NPCDialogueSource.cs`.
    *   Drag your `NPC2_Greeting` to its `Default Dialogue` slot.

6.  **Create a Player Character:**
    *   Create a 3D object (e.g., a Sphere or Capsule) and name it `Player`.
    *   Set its Tag to **"Player"** (create the tag if it doesn't exist).
    *   Add a `Character Controller` or simple movement script so you can move it around the scene.

Now, when you run the scene:
*   Move your `Player` near `NPC_Villager`. It should automatically start the `NPC1_IntroDialogue`.
*   Click or press Space to advance the line that has `Duration: 0`.
*   Move away and then back to `NPC_Villager`. It will respect the cooldown.
*   Move near `NPC_Merchant`. It will play its greeting.
*   You can also press 'E' while in range of `NPC_Villager` to trigger its default dialogue.
*   To test `NPC1_QuestDialogue`, you would need another script (e.g., a simple Quest Manager) to call `NPC_Villager.GetComponent<NPCDialogueSource>().TriggerDialogue(npc1QuestDialogueSO);`. Once that dialogue plays, it won't play again due to `playOnce = true`.