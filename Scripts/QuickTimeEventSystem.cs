// Unity Design Pattern Example: QuickTimeEventSystem
// This script demonstrates the QuickTimeEventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements a QuickTimeEventSystem in Unity, demonstrating how to design a reusable system for interactive prompts that require timely user input.

The core idea is:
1.  **QTE System (Publisher):** Manages the QTE state (active, cooldown), displays UI, handles timers, and checks input. It broadcasts events (QTE started, succeeded, failed).
2.  **QTE Event Data:** A struct containing all necessary information for a specific QTE (required key, duration, prompt text).
3.  **QTE Initiator (Caller):** A script that *starts* a QTE, passing the `QTEEventData`. It can optionally provide local success/failure callbacks.
4.  **QTE Listener (Subscriber):** Any script that needs to react to a QTE's outcome subscribes to the system's global events.

---

### **1. QuickTimeEventSystem.cs**

This is the main script that manages all Quick Time Events.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text and Image
using System;       // Required for Action (event delegate)
using System.Collections; // Required for Coroutines

/// <summary>
/// A struct to hold all necessary data for a single Quick Time Event.
/// This makes QTEs easily configurable and transferable.
/// </summary>
[System.Serializable] // Make it visible in the Inspector for easy setup
public struct QTEEventData
{
    public KeyCode requiredKey;  // The key the player must press
    public float duration;       // How long the player has to press the key
    [TextArea(1, 3)]
    public string promptText;    // The text displayed to the player
    public Color promptColor;    // Color of the prompt text
    public string successMessage; // Message to display on success (optional)
    public string failureMessage; // Message to display on failure (optional)

    /// <summary>
    /// Constructor for easy initialization.
    /// </summary>
    public QTEEventData(KeyCode key, float dur, string prompt, Color color, string success = "Success!", string failure = "Failed!")
    {
        requiredKey = key;
        duration = dur;
        promptText = prompt;
        promptColor = color;
        successMessage = success;
        failureMessage = failure;
    }

    /// <summary>
    /// Default event data for convenience.
    /// </summary>
    public static QTEEventData Default => new QTEEventData(KeyCode.Space, 2.0f, "Press Space!", Color.white);
}

/// <summary>
/// QuickTimeEventSystem
/// This MonoBehaviour acts as the central manager for all Quick Time Events (QTEs).
/// It follows an event-driven design, broadcasting events for QTE lifecycle stages.
/// Other game objects can subscribe to these events to react to QTE outcomes.
/// It also handles the visual presentation and timer for the QTE.
/// </summary>
public class QuickTimeEventSystem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject qtePanel;       // The main panel to show/hide the QTE UI
    [SerializeField] private Text promptText;           // Text component to display the required key/action
    [SerializeField] private Image timerFillImage;      // Image to visualize the countdown (e.g., radial fill)
    [SerializeField] private Text feedbackText;         // Text to display success/failure feedback

    [Header("QTE Settings")]
    [SerializeField] private float defaultQteDuration = 2.0f; // Default duration if not specified
    [SerializeField] private float cooldownDuration = 1.0f;    // Time before another QTE can be triggered after one ends

    // --- Public Static Events ---
    // These events allow any other script in the game to subscribe and react to QTE outcomes.
    // They are static so they can be accessed without a direct reference to the QTE System instance.
    // The QTEEventData provides context about the QTE that just occurred.

    /// <summary>
    /// Event fired when a new QTE starts.
    /// </summary>
    public static event Action<QTEEventData> OnQTEStarted;
    
    /// <summary>
    /// Event fired when a QTE is successfully completed by the player.
    /// </summary>
    public static event Action<QTEEventData> OnQTESucceeded;
    
    /// <summary>
    /// Event fired when a QTE fails (player didn't press in time, or wrong key).
    /// </summary>
    public static event Action<QTEEventData> OnQTEFailed;

    // --- Internal State Variables ---
    private bool _isQTEActive = false;             // Is a QTE currently running?
    private bool _isOnCooldown = false;            // Is the system on cooldown after a QTE?
    private Coroutine _qteCoroutine;               // Reference to the running QTE coroutine
    private QTEEventData _currentQTEData;          // Stores the data for the active QTE
    private Action _currentSuccessCallback;        // Local callback for the specific QTE initiator on success
    private Action _currentFailureCallback;        // Local callback for the specific QTE initiator on failure

    // --- Singleton Pattern (Optional but Recommended) ---
    // Ensures there's only one instance of the QTE system and provides easy global access.
    public static QuickTimeEventSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep QTE system across scenes if needed
        }

        // Initialize UI components
        if (qtePanel != null) qtePanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Initiates a Quick Time Event. This is the main public method to trigger a QTE.
    /// </summary>
    /// <param name="data">The QTEEventData containing details like key, duration, prompt.</param>
    /// <param name="onSuccess">Optional action to execute locally on success.</param>
    /// <param name="onFailure">Optional action to execute locally on failure.</param>
    public void StartQTE(QTEEventData data, Action onSuccess = null, Action onFailure = null)
    {
        // Prevent new QTEs if one is already active or on cooldown
        if (_isQTEActive || _isOnCooldown)
        {
            Debug.LogWarning("QTE System busy or on cooldown. Cannot start new QTE.");
            return;
        }

        _isQTEActive = true;
        _currentQTEData = data;
        _currentSuccessCallback = onSuccess;
        _currentFailureCallback = onFailure;

        // Ensure UI is set up correctly
        if (qtePanel == null || promptText == null || timerFillImage == null || feedbackText == null)
        {
            Debug.LogError("QTE UI components not assigned in Inspector!", this);
            StopQTE(false); // Fail the QTE gracefully due to missing UI
            return;
        }

        // Setup and show UI
        qtePanel.SetActive(true);
        promptText.text = data.promptText;
        promptText.color = data.promptColor;
        timerFillImage.fillAmount = 1f; // Start full
        feedbackText.gameObject.SetActive(false); // Hide any previous feedback

        // Notify global listeners that a QTE has started
        OnQTEStarted?.Invoke(data);

        // Start the QTE timer coroutine
        _qteCoroutine = StartCoroutine(QTE_Timer(data.requiredKey, data.duration > 0 ? data.duration : defaultQteDuration));
    }

    /// <summary>
    /// The coroutine that manages the QTE timer and input checking.
    /// </summary>
    private IEnumerator QTE_Timer(KeyCode requiredKey, float duration)
    {
        float timer = duration;

        while (timer > 0 && _isQTEActive) // Keep running while time is left and QTE is active
        {
            timer -= Time.deltaTime;
            timerFillImage.fillAmount = timer / duration; // Update UI fill amount

            // Check for input
            if (Input.GetKeyDown(requiredKey))
            {
                StopQTE(true); // Success!
                yield break; // Exit coroutine
            }

            yield return null; // Wait for next frame
        }

        // If loop finishes and QTE is still active, it means time ran out
        if (_isQTEActive)
        {
            StopQTE(false); // Failure!
        }
    }

    /// <summary>
    /// Stops the currently active QTE and handles outcomes.
    /// </summary>
    /// <param name="success">True if the QTE was successful, false otherwise.</param>
    private void StopQTE(bool success)
    {
        if (!_isQTEActive) return; // Only stop if a QTE is actually running

        _isQTEActive = false; // Mark QTE as no longer active

        // Stop the coroutine if it's still running
        if (_qteCoroutine != null)
        {
            StopCoroutine(_qteCoroutine);
            _qteCoroutine = null;
        }

        // Provide feedback and invoke events/callbacks
        feedbackText.gameObject.SetActive(true);
        if (success)
        {
            feedbackText.text = _currentQTEData.successMessage;
            feedbackText.color = Color.green;
            OnQTESucceeded?.Invoke(_currentQTEData);
            _currentSuccessCallback?.Invoke(); // Invoke local callback
        }
        else
        {
            feedbackText.text = _currentQTEData.failureMessage;
            feedbackText.color = Color.red;
            OnQTEFailed?.Invoke(_currentQTEData);
            _currentFailureCallback?.Invoke(); // Invoke local callback
        }

        // Start cooldown before hiding the UI entirely
        StartCoroutine(CooldownRoutine());
    }

    /// <summary>
    /// Handles the cooldown period after a QTE finishes, and hides the UI.
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration); // Wait for cooldown duration

        // Hide UI elements after cooldown
        if (qtePanel != null) qtePanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        _isOnCooldown = false;
        _currentSuccessCallback = null; // Clear callbacks
        _currentFailureCallback = null;
    }

    private void OnDestroy()
    {
        // Clear static event subscriptions to prevent memory leaks, especially important for DontDestroyOnLoad.
        OnQTEStarted = null;
        OnQTESucceeded = null;
        OnQTEFailed = null;
    }
}
```

---

### **2. QTEInitiator.cs (Example Usage - How to start a QTE)**

This script demonstrates how another part of your game would *trigger* a QTE.

```csharp
using UnityEngine;

/// <summary>
/// QTEInitiator
/// An example script that triggers a Quick Time Event.
/// This would typically be attached to a player character, an interactive object, or an enemy.
/// </summary>
public class QTEInitiator : MonoBehaviour
{
    [Header("QTE Configuration")]
    [SerializeField] private QTEEventData defaultAttackQTE = new QTEEventData(KeyCode.Space, 1.5f, "Quickly! Press [Space] to attack!", Color.yellow, "Enemy Stunned!", "Missed Attack!");
    [SerializeField] private QTEEventData defaultDefendQTE = new QTEEventData(KeyCode.D, 2.0f, "Defend! Press [D]!", Color.cyan, "Blocked!", "Took Damage!");
    
    // An optional reference to a QTE system, though the singleton pattern makes it accessible globally
    // [SerializeField] private QuickTimeEventSystem qteSystem; 

    private void Update()
    {
        // Example: Trigger an attack QTE when 'A' is pressed
        if (Input.GetKeyDown(KeyCode.A))
        {
            TriggerAttackQTE();
        }

        // Example: Trigger a defend QTE when 'S' is pressed
        if (Input.GetKeyDown(KeyCode.S))
        {
            TriggerDefendQTE();
        }
    }

    /// <summary>
    /// Triggers a pre-configured attack QTE.
    /// Demonstrates using local callbacks for specific actions related to this QTE.
    /// </summary>
    public void TriggerAttackQTE()
    {
        if (QuickTimeEventSystem.Instance == null)
        {
            Debug.LogError("QuickTimeEventSystem instance not found!");
            return;
        }

        Debug.Log("Attempting to trigger Attack QTE...");
        QuickTimeEventSystem.Instance.StartQTE(
            defaultAttackQTE,
            () => { // Local Success Callback
                Debug.Log("<color=green>Player successfully attacked!</color>");
                // Example: Deal damage to an enemy, grant a bonus, etc.
                GetComponent<Renderer>().material.color = Color.green; // Visual feedback
            },
            () => { // Local Failure Callback
                Debug.Log("<color=red>Player failed attack!</color>");
                // Example: Player takes damage, loses stamina, enemy retaliates
                GetComponent<Renderer>().material.color = Color.red; // Visual feedback
            }
        );
    }

    /// <summary>
    /// Triggers a pre-configured defend QTE.
    /// </summary>
    public void TriggerDefendQTE()
    {
        if (QuickTimeEventSystem.Instance == null)
        {
            Debug.LogError("QuickTimeEventSystem instance not found!");
            return;
        }

        Debug.Log("Attempting to trigger Defend QTE...");
        QuickTimeEventSystem.Instance.StartQTE(
            defaultDefendQTE,
            () => { // Local Success Callback
                Debug.Log("<color=green>Player successfully defended!</color>");
                GetComponent<Renderer>().material.color = Color.blue; // Visual feedback
            },
            () => { // Local Failure Callback
                Debug.Log("<color=red>Player failed to defend!</color>");
                GetComponent<Renderer>().material.color = Color.grey; // Visual feedback
            }
        );
    }
}
```

---

### **3. QTEListener.cs (Example Usage - How to listen to QTE outcomes)**

This script demonstrates how different game systems (like a UI manager, game state manager, or enemy AI) can *subscribe* to global QTE events to react to their outcomes, regardless of who initiated the QTE.

```csharp
using UnityEngine;

/// <summary>
/// QTEListener
/// An example script demonstrating how other parts of the game can react to global QTE events.
/// This would typically be attached to a GameManager, UI Manager, Enemy AI, etc.
/// </summary>
public class QTEListener : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to the global QTE events when this object becomes active.
        QuickTimeEventSystem.OnQTEStarted += HandleQTEStarted;
        QuickTimeEventSystem.OnQTESucceeded += HandleQTESucceeded;
        QuickTimeEventSystem.OnQTEFailed += HandleQTEFailed;
        Debug.Log("QTEListener: Subscribed to QTE events.");
    }

    private void OnDisable()
    {
        // Unsubscribe from the global QTE events when this object becomes inactive
        // This is crucial to prevent memory leaks and unexpected behavior (e.g., calling on a destroyed object).
        QuickTimeEventSystem.OnQTEStarted -= HandleQTEStarted;
        QuickTimeEventSystem.OnQTESucceeded -= HandleQTESucceeded;
        QuickTimeEventSystem.OnQTEFailed -= HandleQTEFailed;
        Debug.Log("QTEListener: Unsubscribed from QTE events.");
    }

    /// <summary>
    /// Handler for when any QTE starts.
    /// </summary>
    private void HandleQTEStarted(QTEEventData data)
    {
        Debug.Log($"QTEListener: QTE Started! Press '{data.requiredKey}' for '{data.duration}'s.");
        // Example: Pause game logic, show specific UI elements, play a sound.
    }

    /// <summary>
    /// Handler for when any QTE succeeds.
    /// </summary>
    private void HandleQTESucceeded(QTEEventData data)
    {
        Debug.Log($"QTEListener: <color=green>Global QTE Success!</color> Key: {data.requiredKey}, Message: {data.successMessage}");
        // Example: Update player score, play global success sound, enable next game state.
        if (data.requiredKey == KeyCode.Space)
        {
            // Specific logic for a 'Space' QTE success
            Debug.Log("QTEListener: Player successfully performed a Space-based QTE!");
        }
    }

    /// <summary>
    /// Handler for when any QTE fails.
    /// </summary>
    private void HandleQTEFailed(QTEEventData data)
    {
        Debug.Log($"QTEListener: <color=red>Global QTE Failed!</color> Key: {data.requiredKey}, Message: {data.failureMessage}");
        // Example: Penalize player, trigger enemy counter-attack, restart a segment.
        if (data.requiredKey == KeyCode.D)
        {
            // Specific logic for a 'D' QTE failure
            Debug.Log("QTEListener: Player failed to perform a Defend QTE!");
        }
    }
}
```

---

### **Unity Setup Guide:**

1.  **Create UI Canvas:**
    *   Right-click in Hierarchy -> UI -> Canvas.
    *   Set Canvas Scaler -> UI Scale Mode to "Scale With Screen Size", Reference Resolution (e.g., 1920x1080).

2.  **Create QTE Panel (GameObject for QTE UI):**
    *   Right-click on Canvas -> UI -> Panel.
    *   Rename it `QTE_Panel`.
    *   Adjust its Rect Transform to be, for example, centered and sized appropriately (e.g., Width: 400, Height: 200).
    *   Set its Image component's Color to something semi-transparent (e.g., black with alpha 150).

3.  **Create Prompt Text:**
    *   Right-click on `QTE_Panel` -> UI -> Text - TextMeshPro (if you've imported TMP Essentials, recommended). Otherwise, basic Text.
    *   Rename it `Prompt_Text`.
    *   Adjust Rect Transform to fill most of the panel.
    *   Set Font Size (e.g., 50), Alignment (Center, Middle).
    *   Initial Text can be `Press Key!`

4.  **Create Timer Fill Image:**
    *   Right-click on `QTE_Panel` -> UI -> Image.
    *   Rename it `Timer_FillImage`.
    *   Adjust Rect Transform to be a horizontal bar or a circle (e.g., anchor bottom, width 300, height 30).
    *   Set Image Type to `Filled`.
    *   Set Fill Method to `Radial 360` or `Horizontal`.
    *   Set Fill Origin to `Bottom` or `Left` (for horizontal).
    *   Set Color to (e.g., Yellow).
    *   Set Initial Fill Amount to `1`.

5.  **Create Feedback Text:**
    *   Right-click on `QTE_Panel` -> UI -> Text - TextMeshPro (or basic Text).
    *   Rename it `Feedback_Text`.
    *   Adjust Rect Transform (e.g., near the top of the panel, width 400, height 50).
    *   Set Font Size (e.g., 30), Alignment (Center, Middle).
    *   Initial Text can be `Result`.

6.  **Create QuickTimeEventSystem GameObject:**
    *   Create an empty GameObject in your scene (Right-click in Hierarchy -> Create Empty).
    *   Rename it `_QuickTimeEventSystem`.
    *   Attach the `QuickTimeEventSystem.cs` script to it.

7.  **Assign UI Elements in Inspector:**
    *   Select `_QuickTimeEventSystem` GameObject.
    *   Drag and drop the `QTE_Panel` to the `Qte Panel` field.
    *   Drag and drop `Prompt_Text` to the `Prompt Text` field.
    *   Drag and drop `Timer_FillImage` to the `Timer Fill Image` field.
    *   Drag and drop `Feedback_Text` to the `Feedback Text` field.

8.  **Create Initiator and Listener GameObjects:**
    *   Create an empty GameObject, rename it `QTE_Trigger`.
    *   Attach `QTEInitiator.cs` to it. (Optionally add a visible 3D object like a Cube for visual feedback from the initiator).
    *   Create an empty GameObject, rename it `QTE_GlobalListener`.
    *   Attach `QTEListener.cs` to it.

9.  **Run the Scene:**
    *   Press Play.
    *   Press 'A' to trigger the Attack QTE.
    *   Press 'S' to trigger the Defend QTE.
    *   Observe the UI, try to press the required key in time, or let the timer run out.
    *   Check the Console for messages from both the `QTEInitiator` (local callbacks) and the `QTEListener` (global event subscriptions).

This setup provides a complete, practical, and extensible QuickTimeEventSystem, following best practices for Unity development and design patterns.