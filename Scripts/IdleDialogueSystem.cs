// Unity Design Pattern Example: IdleDialogueSystem
// This script demonstrates the IdleDialogueSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Idle Dialogue System** design pattern in Unity. This pattern is used to make game characters feel more alive by allowing them to speak pre-defined dialogue lines automatically when they are in an idle state for a certain period, with considerations for interruptions and cooldowns.

### `IdleDialogueSystem.cs`

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// The IdleDialogueSystem design pattern allows characters to automatically
/// speak pre-defined dialogue lines when they enter and remain in an "idle" state
/// for a specified duration.
///
/// This system is designed to make game worlds feel more alive by giving characters
/// periodic, self-initiated chatter, but it's also interruptible if the character
/// begins a new action, and includes cooldowns to prevent spamming.
///
/// Key Principles:
/// 1.  **Idle Detection**: Relies on external systems (e.g., player input, AI state machine)
///     to signal when the character is truly idle. This script provides `StartIdling()`
///     and `StopIdling()` methods for this purpose.
/// 2.  **Timed Activation**: A configurable delay is introduced before the *first* idle dialogue,
///     and separate delays for *subsequent* dialogues. This prevents dialogue from triggering
///     too quickly after becoming idle.
/// 3.  **Cooldown**: After a dialogue line is spoken, a cooldown period prevents
///     another idle dialogue from immediately following, even if the character remains idle.
///     This ensures natural pacing.
/// 4.  **Interruption**: If the character stops being idle (e.g., starts moving, performs an action),
///     any pending dialogue trigger or actively playing dialogue is immediately halted.
/// 5.  **Dialogue Cycling**: Dialogues are typically cycled through in order (as in this example),
///     or randomly selected from a pool, to provide variety.
/// 6.  **Simulated Display**: For demonstration, dialogue is logged to the console. In a real project,
///     this would interface with a UI text display, speech bubble system, or audio playback.
/// </summary>
public class IdleDialogueSystem : MonoBehaviour
{
    [Header("Idle Dialogue Settings")]
    [Tooltip("Time the character must be idle before the *first* idle dialogue can trigger.")]
    [SerializeField] private float timeUntilFirstIdleDialogue = 5f;

    [Tooltip("Time the character must be idle between *subsequent* idle dialogues (after the first has played).")]
    [SerializeField] private float timeBetweenIdleDialogues = 10f;

    [Tooltip("Time after an idle dialogue finishes before another one can be considered (even if idle duration is met).")]
    [SerializeField] private float idleDialogueCooldown = 3f;

    [Header("Dialogue Content")]
    [Tooltip("Array of dialogue lines this character can speak when idle.")]
    [SerializeField] private string[] dialogueLines = new string[] {
        "What a beautiful day!",
        "I wonder what's for dinner?",
        "Just enjoying the peace and quiet.",
        "Hmm, nothing much to do right now.",
        "A little rest never hurt anyone."
    };

    [Tooltip("How long each dialogue line is 'displayed' (e.g., in UI or console).")]
    [SerializeField] private float dialogueDisplayDuration = 2.5f;

    [Tooltip("Optional: Transform where dialogue text might visually appear in world space (e.g., above character's head).")]
    [SerializeField] private Transform speakPoint;

    // Internal State Variables
    private bool _isCurrentlyIdle = false;              // True if the character is currently signaled as idle.
    private float _currentIdleTime = 0f;                // Tracks how long the character has been idle in the current session.
    private float _lastDialoguePlayedTime = -Mathf.Infinity; // Time.time when the last dialogue finished. Used for cooldown.
    private bool _isDialogueActive = false;             // True if a dialogue is currently being "spoken" (displayed).
    private int _currentDialogueIndex = 0;              // Index of the next dialogue line to play from the array.
    private Coroutine _dialogueCoroutine = null;        // Reference to the active dialogue display coroutine.
    private bool _hasPlayedAnyDialogueOnce = false;     // Tracks if any idle dialogue has ever played, to use correct delay.

    /// <summary>
    /// Read-only property to check if an idle dialogue is currently active.
    /// Useful for other systems that might need to know if the character is speaking
    /// (e.g., to prevent other actions or UI elements).
    /// </summary>
    public bool IsDialogueActive => _isDialogueActive;

    void Update()
    {
        // Only process idle dialogue logic if the character is explicitly signaled as idle.
        if (_isCurrentlyIdle)
        {
            // If a dialogue is already active (being displayed), we don't count idle time
            // or try to start new ones. We wait for the current dialogue to finish.
            if (_isDialogueActive)
            {
                return;
            }

            // Check if the cooldown period after the *last* dialogue has elapsed.
            // This prevents rapid fire dialogue after one finishes.
            if (Time.time < _lastDialoguePlayedTime + idleDialogueCooldown)
            {
                return; // Still in cooldown, cannot play new dialogue yet.
            }

            // Increment the current idle time. This timer accumulates only when truly idle
            // and not actively speaking or on cooldown.
            _currentIdleTime += Time.deltaTime;

            // Determine which delay to use: the initial delay for the very first dialogue,
            // or the subsequent delay for all dialogues thereafter.
            float targetDelay = _hasPlayedAnyDialogueOnce ? timeBetweenIdleDialogues : timeUntilFirstIdleDialogue;

            // If the accumulated idle time meets or exceeds the target delay,
            // and all other conditions are met, try to play a dialogue.
            if (_currentIdleTime >= targetDelay)
            {
                TryPlayIdleDialogue();
            }
        }
        else
        {
            // If the character is no longer idle (e.g., moving, performing an action):
            // 1. Reset the idle timer.
            // 2. Immediately stop any active or pending dialogue.
            _currentIdleTime = 0f;
            StopDialogueDisplay(); // Interrupts any ongoing dialogue or pending display.
        }
    }

    /// <summary>
    /// Attempts to play an idle dialogue line.
    /// This method is called when all conditions for playing an idle dialogue are met
    /// (character is idle, idle duration met, cooldown passed, no dialogue active).
    /// </summary>
    private void TryPlayIdleDialogue()
    {
        // Basic validation: ensure there are dialogue lines and no dialogue is currently active.
        if (dialogueLines == null || dialogueLines.Length == 0 || _isDialogueActive)
        {
            return;
        }

        // Reset the current idle time. This ensures that the next idle dialogue
        // will wait for `timeBetweenIdleDialogues` starting from *this* moment.
        _currentIdleTime = 0f;

        // Start the coroutine that handles the actual "display" of the dialogue line.
        _dialogueCoroutine = StartCoroutine(PlayDialogueRoutine());
    }

    /// <summary>
    /// Coroutine responsible for simulating the display of a single dialogue line
    /// for its specified duration. In a real game, this would interface with UI.
    /// </summary>
    private IEnumerator PlayDialogueRoutine()
    {
        _isDialogueActive = true; // Set flag: a dialogue is now active.
        _hasPlayedAnyDialogueOnce = true; // Mark that at least one dialogue has played.

        // Get the current dialogue line to display.
        string currentLine = dialogueLines[_currentDialogueIndex];

        // --- Simulate Dialogue Display ---
        // In a production game, this section would interact with your UI manager,
        // speech bubble system, or audio engine to actually show/play the dialogue.
        string speakerName = gameObject.name; // Use the GameObject's name as the speaker.
        Vector3 displayPosition = speakPoint != null ? speakPoint.position : transform.position + Vector3.up * 1.5f; // Default above character.

        // Log to console for demonstration.
        Debug.Log($"[{speakerName} @ {displayPosition.x:F2}, {displayPosition.y:F2}, {displayPosition.z:F2}] says: \"{currentLine}\"", this);
        // Example for a real UI system: UIManager.Instance.ShowDialogue(currentLine, speakerName, displayPosition);

        // Wait for the specified duration to simulate the dialogue being visible/audible.
        yield return new WaitForSeconds(dialogueDisplayDuration);
        // Example for a real UI system: UIManager.Instance.HideDialogue();
        // --- End Simulate Dialogue Display ---

        // Move to the next dialogue line, wrapping around to the beginning if we reach the end of the array.
        _currentDialogueIndex = (_currentDialogueIndex + 1) % dialogueLines.Length;

        _isDialogueActive = false; // Dialogue display has finished, reset flag.
        _lastDialoguePlayedTime = Time.time; // Record the finish time for cooldown calculations.

        _dialogueCoroutine = null; // Clear the coroutine reference as it has completed.
    }

    /// <summary>
    /// Stops any currently active dialogue display immediately.
    /// This is called when the character stops being idle (interruption)
    /// or when the system needs to reset.
    /// </summary>
    private void StopDialogueDisplay()
    {
        // If there's an active coroutine for dialogue display, stop it.
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        // If the _isDialogueActive flag was true, it means a dialogue was interrupted.
        if (_isDialogueActive)
        {
            _isDialogueActive = false; // Reset the flag.
            // In a real game, you would also call your UI manager to immediately hide
            // any displayed dialogue text or stop any audio.
            // Example: UIManager.Instance.HideDialogue();
            Debug.Log($"Idle dialogue interrupted for {gameObject.name}.");
        }
    }

    // --- Public API for External Systems ---

    /// <summary>
    /// Signals to the IdleDialogueSystem that the character has entered an idle state.
    /// This will start the internal idle timer, eventually leading to idle dialogue
    /// if conditions are met. Should be called by character's movement/AI logic.
    /// </summary>
    public void StartIdling()
    {
        // Only update if the state is actually changing to avoid unnecessary resets.
        if (!_isCurrentlyIdle)
        {
            Debug.Log($"{gameObject.name} starts idling. Initializing timer for first dialogue consideration.");
            _isCurrentlyIdle = true;
            _currentIdleTime = 0f; // Reset timer to ensure the correct initial/between dialogue delay.
            // Note: _lastDialoguePlayedTime (for cooldown) is NOT reset here,
            // as cooldown should persist across brief non-idle periods.
        }
    }

    /// <summary>
    /// Signals to the IdleDialogueSystem that the character is no longer idle
    /// (e.g., started moving, performing an action).
    /// This will reset the idle timer and immediately interrupt any active idle dialogue.
    /// Should be called by character's movement/AI logic.
    /// </summary>
    public void StopIdling()
    {
        // Only update if the state is actually changing.
        if (_isCurrentlyIdle)
        {
            Debug.Log($"{gameObject.name} stops idling. Dialogue system paused.");
            _isCurrentlyIdle = false;
            _currentIdleTime = 0f; // Reset idle timer completely.
            StopDialogueDisplay(); // Immediately halt any ongoing dialogue.
        }
    }

    // --- Editor-only Visualization ---

    // Draws a gizmo in the editor to visualize the character's speak point.
    private void OnDrawGizmos()
    {
        if (speakPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(speakPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, speakPoint.position);
        }
        else
        {
            Gizmos.color = Color.yellow;
            // Draw a default speak point above the character if none is specified.
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
        }
    }
}
```

### Example Usage: How to Implement in Unity

To use the `IdleDialogueSystem` in your Unity project, follow these steps:

1.  **Create the Script**:
    *   In your Unity project, create a new C# script named `IdleDialogueSystem.cs` and paste the code above into it.

2.  **Create a Character GameObject**:
    *   In your scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty). Name it something like `NPC_Villager` or `PlayerCharacter`.

3.  **Attach the Script**:
    *   Drag and drop the `IdleDialogueSystem.cs` script onto your `NPC_Villager` GameObject in the Hierarchy or Inspector.

4.  **Configure in Inspector**:
    *   Select `NPC_Villager` in the Hierarchy.
    *   In the Inspector, you'll see the `Idle Dialogue System` component.
    *   **Idle Dialogue Settings**:
        *   `Time Until First Idle Dialogue`: Set the initial delay (e.g., `5` seconds).
        *   `Time Between Idle Dialogues`: Set the delay for subsequent dialogues (e.g., `10` seconds).
        *   `Idle Dialogue Cooldown`: Set the time after a dialogue finishes before a new one can start (e.g., `3` seconds).
    *   **Dialogue Content**:
        *   Expand the `Dialogue Lines` array. Add your desired dialogue phrases. The example provides five.
        *   `Dialogue Display Duration`: Set how long each line is "shown" (e.g., `2.5` seconds). This is how long the `Debug.Log` message will stay in the console before the system considers the dialogue finished.
        *   `Speak Point` (Optional): Create an empty child GameObject under `NPC_Villager` (e.g., named `SpeakPoint`). Position it slightly above the character's head. Drag this child GameObject into the `Speak Point` slot on the `Idle Dialogue System` component. This helps visualize where dialogue might appear.

5.  **Integrate with Character Logic (Example)**:
    The `IdleDialogueSystem` itself doesn't decide *when* a character is idle; it reacts to external signals. You need a separate script (e.g., your character's movement controller or AI state machine) to call `StartIdling()` and `StopIdling()`.

    Here's a simple example script (`CharacterIdleDetector.cs`) that you can attach to the same `NPC_Villager` GameObject to demonstrate this interaction:

    ```csharp
    using UnityEngine;

    /// <summary>
    /// Example script demonstrating how to integrate with the IdleDialogueSystem.
    /// This script simulates a character's "idleness" based on simple movement input.
    /// </summary>
    [RequireComponent(typeof(IdleDialogueSystem))] // Ensures IdleDialogueSystem is present
    public class CharacterIdleDetector : MonoBehaviour
    {
        private IdleDialogueSystem _idleDialogueSystem;
        private Rigidbody _rigidbody; // Used to detect if the character is moving
        
        [Tooltip("Velocity magnitude threshold below which the character is considered idle.")]
        [SerializeField] private float movementThreshold = 0.1f;

        void Awake()
        {
            _idleDialogueSystem = GetComponent<IdleDialogueSystem>();
            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
            {
                Debug.LogWarning("No Rigidbody found on " + gameObject.name + ". Adding one for movement detection.", this);
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.isKinematic = true; // Set to kinematic if you're not using physics for movement.
                                               // This prevents gravity from making it non-idle.
            }
        }

        void Update()
        {
            // Simulate player input or AI movement
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 inputMovement = new Vector3(horizontalInput, 0, verticalInput);

            // Determine if the character is currently moving based on input or Rigidbody velocity
            bool isMoving = inputMovement.magnitude > 0.1f; // Check for significant input
            if (_rigidbody != null && !_rigidbody.isKinematic) {
                 // If not kinematic, check actual physics velocity
                isMoving = isMoving || _rigidbody.velocity.magnitude > movementThreshold;
            } else if (_rigidbody != null && _rigidbody.isKinematic) {
                // If kinematic, rely only on input for this example.
                // In a real kinematic setup, you'd check transform changes or animation state.
            }
            
            // If currently speaking an idle dialogue, ignore input-based idle state
            // to allow the dialogue to complete without being immediately interrupted by "not idle" if input is released.
            // This is a design choice; you might want input to ALWAYS interrupt.
            if (_idleDialogueSystem.IsDialogueActive)
            {
                // If input is detected while speaking, *always* interrupt.
                if (isMoving) {
                    _idleDialogueSystem.StopIdling();
                }
                return; 
            }

            if (isMoving)
            {
                // If the character is moving, tell the dialogue system to stop idling.
                _idleDialogueSystem.StopIdling();
                // Example: Apply movement (e.g., transform.Translate(inputMovement * Time.deltaTime * 5f);)
            }
            else
            {
                // If the character is stationary, tell the dialogue system to start idling.
                _idleDialogueSystem.StartIdling();
            }

            // Optional: Manual trigger for demonstration
            if (Input.GetKeyDown(KeyCode.I))
            {
                _idleDialogueSystem.StartIdling();
                Debug.Log("Manually forced Start Idling.");
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                _idleDialogueSystem.StopIdling();
                Debug.Log("Manually forced Stop Idling.");
            }
        }
    }
    ```
    *   Attach `CharacterIdleDetector.cs` to your `NPC_Villager` GameObject.
    *   Ensure your `NPC_Villager` GameObject has a `Rigidbody` component (add one if it doesn't). For this simple example, setting `Is Kinematic` to `true` on the Rigidbody is often best, as we're not simulating full physics.

6.  **Run the Scene**:
    *   Play your Unity scene.
    *   Observe the Unity Console:
        *   After `timeUntilFirstIdleDialogue` (e.g., 5 seconds), the first dialogue line will appear.
        *   After `dialogueDisplayDuration` (e.g., 2.5 seconds), the dialogue will "disappear".
        *   After `idleDialogueCooldown` (e.g., 3 seconds) PLUS `timeBetweenIdleDialogues` (e.g., 10 seconds), the next dialogue line will appear.
    *   Use the **arrow keys** or **WASD** to move your character. You'll see "NPC_Villager stops idling." in the console, and any active dialogue will immediately cease.
    *   Stop moving. After a moment, "NPC_Villager starts idling." will appear, and the idle dialogue timer will restart.
    *   You can also press 'I' to manually force `StartIdling` and 'O' to force `StopIdling` (if using the example `CharacterIdleDetector`).

This setup provides a fully functional and understandable implementation of the Idle Dialogue System pattern, ready for integration into a larger game.