// Unity Design Pattern Example: CrowdDialogueSystem
// This script demonstrates the CrowdDialogueSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'CrowdDialogueSystem' isn't a universally recognized GoF design pattern, but rather a practical pattern that combines several standard patterns (like **Manager/Mediator**, **Observer**, and **Scriptable Object** for data management) to solve a common game development challenge: managing dynamic dialogue from multiple, often non-player, entities in a scene.

This pattern is ideal for scenarios like:
*   A bustling town square where NPCs offer snippets of conversation.
*   Background chatter in a crowded market.
*   Environmental objects or events that trigger contextual dialogue.
*   Any situation where multiple entities might "want" to speak, and a central system needs to decide who gets to speak, when, and how.

The core idea is to decouple the "who wants to speak" from the "who is speaking" and "how it's displayed."

---

## Crowd Dialogue System: Design Principles

1.  **Centralized Management (Manager/Mediator):** A single `CrowdDialogueManager` handles all dialogue requests, prioritizes them, and controls the display and audio. This prevents individual NPCs from needing to know about each other or the UI.
2.  **Decoupled Data (ScriptableObject):** Dialogue lines are defined as `ScriptableObject` assets, allowing game designers to easily create, modify, and assign dialogue without touching code.
3.  **Event-Driven Communication (Observer):** The `CrowdDialogueManager` broadcasts events (`OnDialogueStarted`, `OnDialogueEnded`) that the `DialogueUI` (and any other interested systems) can subscribe to, keeping the UI logic separate from the core dialogue management.
4.  **Prioritization & Cooldowns:** The manager implements logic to decide which dialogue request to process when multiple entities are vying for attention, often using priority scores, proximity, and cooldown timers to simulate natural crowd behavior.
5.  **Dialogue Sources:** Individual entities (NPCs, interactive objects) act as "sources" that can request to speak, but they don't directly control the display.

---

## Complete Unity Example: Crowd Dialogue System

This example provides four C# scripts and explains their setup in Unity.

### 1. `DialogueLine.cs` (ScriptableObject)

This defines the content of a single line of dialogue. Using `ScriptableObject` allows artists and designers to create dialogue assets directly in the Unity editor without writing code.

```csharp
using UnityEngine;

/// <summary>
/// Represents a single line of dialogue, including who is speaking, what they say,
/// and an optional audio clip. This is a ScriptableObject, allowing designers
/// to create dialogue assets directly in the Unity editor.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "Crowd Dialogue/Dialogue Line")]
public class DialogueLine : ScriptableObject
{
    [Tooltip("The name of the character or entity speaking this line.")]
    public string speakerName = "Crowd";

    [TextArea(3, 6)]
    [Tooltip("The actual text of the dialogue line.")]
    public string dialogueText = "Hello there!";

    [Tooltip("Optional audio clip to play with this dialogue line.")]
    public AudioClip audioClip;
}
```

### 2. `CrowdDialogueManager.cs`

This is the central hub of the system. It's a Singleton that manages all dialogue requests, prioritizes them, handles cooldowns, and orchestrates the display of dialogue through the `DialogueUI`.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq; // For LINQ operations like OrderBy

/// <summary>
/// The central manager for the Crowd Dialogue System.
/// It acts as a Singleton, handling dialogue requests from multiple sources,
/// prioritizing them, managing speaker cooldowns, and orchestrating the
/// display of dialogue through the DialogueUI.
/// </summary>
public class CrowdDialogueManager : MonoBehaviour
{
    // Singleton pattern implementation
    public static CrowdDialogueManager Instance { get; private set; }

    [Header("UI & Audio References")]
    [Tooltip("Reference to the DialogueUI component responsible for displaying dialogue text.")]
    [SerializeField] private DialogueUI dialogueUI;
    [Tooltip("AudioSource used to play dialogue audio clips. It will be created if not assigned.")]
    [SerializeField] private AudioSource crowdAudioSource;

    [Header("Dialogue Behavior Settings")]
    [Tooltip("How long each dialogue line is displayed on screen by default if no audio clip is present, or if audio clip is shorter.")]
    [SerializeField] private float defaultDisplayDuration = 3f;
    [Tooltip("Minimum time (in seconds) before a specific DialogueSource can speak again after their last line finished.")]
    [SerializeField] private float speakerCooldown = 2f;
    [Tooltip("Maximum number of pending dialogue requests the manager will hold. If exceeded, older, lower priority requests will be dropped.")]
    [SerializeField] private int maxQueueSize = 10;

    /// <summary>
    /// Internal struct to hold details of a dialogue request.
    /// Used by the CrowdDialogueManager to track pending dialogues.
    /// </summary>
    private struct DialogueRequest
    {
        public DialogueLine line;      // The dialogue content
        public DialogueSource source;  // The entity that requested the dialogue
        public float requestTime;      // When the request was made
        public float priority;         // Higher value = higher priority (e.g., player proximity, importance)
    }

    // List of pending dialogue requests awaiting processing.
    private List<DialogueRequest> dialogueRequests = new List<DialogueRequest>();

    // Dictionary to track when each DialogueSource last successfully spoke, for cooldowns.
    private Dictionary<DialogueSource, float> lastSpeakerSpeakTime = new Dictionary<DialogueSource, float>();

    private Coroutine processingCoroutine; // Reference to the main processing coroutine
    private DialogueRequest? currentActiveRequest = null; // Stores the currently displayed dialogue

    // --- Events ---
    // These events allow other systems (like DialogueUI) to react to dialogue state changes.
    /// <summary>
    /// Event fired when a new dialogue line starts being displayed.
    /// Subscribers receive the DialogueLine object.
    /// </summary>
    public static event Action<DialogueLine> OnDialogueStarted;

    /// <summary>
    /// Event fired when the current dialogue line finishes being displayed.
    /// </summary>
    public static event Action OnDialogueEnded;

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
            // Optionally, make the manager persist across scene loads.
            // Remove if you want a new manager per scene.
            DontDestroyOnLoad(gameObject);
        }

        // Initialize AudioSource if not already assigned
        if (crowdAudioSource == null)
        {
            crowdAudioSource = GetComponent<AudioSource>();
            if (crowdAudioSource == null)
            {
                crowdAudioSource = gameObject.AddComponent<AudioSource>();
            }
            crowdAudioSource.playOnAwake = false;
            crowdAudioSource.loop = false;
        }

        // Warn if DialogueUI is missing
        if (dialogueUI == null)
        {
            Debug.LogWarning("CrowdDialogueManager: DialogueUI reference is missing. Dialogue text will not be displayed.", this);
        }
    }

    private void Start()
    {
        // Start the continuous coroutine that processes dialogue requests
        processingCoroutine = StartCoroutine(ProcessDialogueQueue());
    }

    /// <summary>
    /// Allows a DialogueSource (e.g., an NPC) to request a line of dialogue.
    /// The manager will add it to a queue and decide when/if it gets played
    /// based on its priority and speaker cooldowns.
    /// </summary>
    /// <param name="line">The DialogueLine ScriptableObject to be spoken.</param>
    /// <param name="source">The DialogueSource (e.g., NPC) making the request.</param>
    /// <param name="priority">A float value representing the urgency or importance of this request.
    /// Higher values mean higher priority. Default is 0.</param>
    public void RequestDialogue(DialogueLine line, DialogueSource source, float priority = 0f)
    {
        if (line == null || source == null)
        {
            Debug.LogWarning("CrowdDialogueManager: Invalid dialogue request (null line or source).", this);
            return;
        }

        // Check if this specific source is currently on a cooldown
        if (lastSpeakerSpeakTime.TryGetValue(source, out float lastTime) && Time.time < lastTime + speakerCooldown)
        {
            // Request is ignored because the speaker recently spoke.
            // Debug.Log($"CrowdDialogueManager: {source.name} is on cooldown. Request for '{line.dialogueText}' ignored.", source);
            return;
        }

        // Add the new request to the list of pending dialogues
        dialogueRequests.Add(new DialogueRequest
        {
            line = line,
            source = source,
            requestTime = Time.time,
            priority = priority
        });

        // If the queue exceeds its maximum size, remove the lowest priority/oldest request.
        if (dialogueRequests.Count > maxQueueSize)
        {
            // Sort to find the lowest priority, then oldest request.
            // This ensures we always keep the most relevant/recent high-priority requests.
            dialogueRequests = dialogueRequests
                .OrderBy(req => req.priority)     // Sort by priority ascending (lowest first)
                .ThenBy(req => req.requestTime)   // Then by time ascending (oldest first)
                .Skip(1)                          // Skip the first (lowest priority/oldest)
                .ToList();                        // Convert back to list
            
            // Debug.LogWarning($"CrowdDialogueManager: Queue exceeded max size {maxQueueSize}. Removed oldest/lowest priority request.");
        }
    }

    /// <summary>
    /// The main coroutine that continuously checks and processes dialogue requests.
    /// It runs indefinitely, picking the next available and highest-priority dialogue.
    /// </summary>
    private IEnumerator ProcessDialogueQueue()
    {
        while (true)
        {
            // Only try to process a new dialogue if no line is currently being displayed
            if (currentActiveRequest == null)
            {
                // Sort the pending requests to find the best candidate.
                // Priority: Highest priority first.
                // Tie-breaker: Oldest request first (if priorities are equal).
                dialogueRequests.Sort((a, b) => {
                    int priorityComparison = b.priority.CompareTo(a.priority); // Descending priority
                    if (priorityComparison != 0) return priorityComparison;
                    return a.requestTime.CompareTo(b.requestTime);             // Ascending time for same priority
                });

                // Find the first valid request that isn't on cooldown
                DialogueRequest? nextRequest = null;
                int indexToRemove = -1;

                for (int i = 0; i < dialogueRequests.Count; i++)
                {
                    DialogueRequest req = dialogueRequests[i];
                    // Check if the source is NOT on cooldown
                    if (!lastSpeakerSpeakTime.TryGetValue(req.source, out float lastTime) || Time.time >= lastTime + speakerCooldown)
                    {
                        nextRequest = req;
                        indexToRemove = i;
                        break; // Found an eligible request, stop searching
                    }
                }

                if (nextRequest.HasValue)
                {
                    currentActiveRequest = nextRequest.Value;
                    dialogueRequests.RemoveAt(indexToRemove); // Remove the selected request from the queue

                    // Start the coroutine to display and play this dialogue
                    yield return StartCoroutine(DisplayAndPlayDialogue(currentActiveRequest.Value));

                    // After the dialogue is done, record the time this speaker finished.
                    // This updates their cooldown timer.
                    lastSpeakerSpeakTime[currentActiveRequest.Value.source] = Time.time;
                    currentActiveRequest = null; // Clear the active request, making the system ready for the next one
                }
            }
            yield return null; // Wait for the next frame before checking again
        }
    }

    /// <summary>
    /// Coroutine responsible for displaying a dialogue line and playing its associated audio.
    /// It notifies subscribers when dialogue starts and ends.
    /// </summary>
    /// <param name="request">The DialogueRequest containing the line to display.</param>
    private IEnumerator DisplayAndPlayDialogue(DialogueRequest request)
    {
        // 1. Notify subscribers (e.g., DialogueUI) that dialogue has started
        OnDialogueStarted?.Invoke(request.line);

        // 2. Instruct the DialogueUI to display the text
        dialogueUI?.DisplayDialogue(request.line);

        // 3. Play audio if an AudioClip is provided and an AudioSource is available
        if (request.line.audioClip != null && crowdAudioSource != null)
        {
            crowdAudioSource.PlayOneShot(request.line.audioClip);
            // Wait for the audio clip to finish, or for the default duration, whichever is longer.
            // This ensures short lines aren't rushed and long lines are fully heard.
            yield return new WaitForSeconds(Mathf.Max(defaultDisplayDuration, request.line.audioClip.length));
        }
        else
        {
            // If no audio, wait for the default display duration
            yield return new WaitForSeconds(defaultDisplayDuration);
        }

        // 4. Instruct the DialogueUI to hide the text
        dialogueUI?.HideDialogue();

        // 5. Notify subscribers that dialogue has ended
        OnDialogueEnded?.Invoke();
    }

    private void OnDestroy()
    {
        // Stop the processing coroutine when the manager is destroyed
        if (processingCoroutine != null)
        {
            StopCoroutine(processingCoroutine);
        }
        // Clean up static event subscriptions to prevent memory leaks in editor
        OnDialogueStarted = null;
        OnDialogueEnded = null;

        // Clear singleton instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Example Usage (Programmatic Dialogue Request) ---
    // You can call RequestDialogue from any other script.
    // E.g., from a game event, a quest system, or another interaction:

    /*
    // Example: A quest giver might try to speak when player approaches
    public void OnPlayerProximity(DialogueSource questGiverSource, DialogueLine specificQuestLine)
    {
        if (CrowdDialogueManager.Instance != null)
        {
            // Request with a higher priority to ensure it gets attention
            CrowdDialogueManager.Instance.RequestDialogue(specificQuestLine, questGiverSource, 5f);
        }
    }

    // Example: An environmental object reacts to player interaction
    public void OnObjectInteracted(DialogueSource objectSource, DialogueLine objectResponseLine)
    {
        if (CrowdDialogueManager.Instance != null)
        {
            // Environmental cues might have a medium priority
            CrowdDialogueManager.Instance.RequestDialogue(objectResponseLine, objectSource, 2f);
        }
    }
    */
}
```

### 3. `DialogueUI.cs`

This script handles the visual display of dialogue on the UI canvas. It subscribes to the `CrowdDialogueManager`'s events to know when to show and hide dialogue. Requires TextMeshPro.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro UI components
using System.Collections;

/// <summary>
/// Manages the display of dialogue text on the UI.
/// It subscribes to CrowdDialogueManager events to know when to show and hide dialogue.
/// Requires TextMeshProUGUI for text fields and a CanvasGroup for fading.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("TextMeshProUGUI component for displaying the speaker's name.")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [Tooltip("TextMeshProUGUI component for displaying the dialogue line text.")]
    [SerializeField] private TextMeshProUGUI dialogueLineText;
    [Tooltip("CanvasGroup component to control the overall visibility and fading of the dialogue panel.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [Tooltip("Duration of the fade-in/fade-out animation for the dialogue UI.")]
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine fadeCoroutine; // Reference to the ongoing fade coroutine

    private void Awake()
    {
        // Ensure CanvasGroup exists and is set up
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        // Start hidden
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Basic validation for text components
        if (speakerNameText == null || dialogueLineText == null)
        {
            Debug.LogError("DialogueUI: Speaker Name Text or Dialogue Line Text is not assigned. Dialogue will not display correctly.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to dialogue events from the manager
        CrowdDialogueManager.OnDialogueStarted += DisplayDialogue;
        CrowdDialogueManager.OnDialogueEnded += HideDialogue;
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and unexpected behavior
        CrowdDialogueManager.OnDialogueStarted -= DisplayDialogue;
        CrowdDialogueManager.OnDialogueEnded -= HideDialogue;
    }

    /// <summary>
    /// Displays a new dialogue line on the UI, fading in the panel.
    /// This method is called by the CrowdDialogueManager via the OnDialogueStarted event.
    /// </summary>
    /// <param name="line">The DialogueLine object containing text and speaker information.</param>
    public void DisplayDialogue(DialogueLine line)
    {
        // Stop any ongoing fade animation
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Update UI text fields
        if (speakerNameText != null) speakerNameText.text = line.speakerName;
        if (dialogueLineText != null) dialogueLineText.text = line.dialogueText;

        // Start fading in the UI
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1, true));
    }

    /// <summary>
    /// Hides the dialogue UI panel, fading it out.
    /// This method is called by the CrowdDialogueManager via the OnDialogueEnded event.
    /// </summary>
    public void HideDialogue()
    {
        // Stop any ongoing fade animation
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        // Start fading out the UI
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0, false));
    }

    /// <summary>
    /// Coroutine to smoothly fade the CanvasGroup's alpha.
    /// </summary>
    /// <param name="targetAlpha">The alpha value to fade to (0 for hidden, 1 for visible).</param>
    /// <param name="interactable">Whether the canvas group should be interactable and block raycasts.</param>
    private IEnumerator FadeCanvasGroup(float targetAlpha, bool interactable)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha; // Ensure final alpha is exact

        // Set interactability and raycast blocking based on visibility
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }
}
```

### 4. `DialogueSource.cs`

This script represents an entity that can contribute dialogue, such as an NPC. It holds a collection of `DialogueLine` assets and requests the `CrowdDialogueManager` to speak when certain conditions are met (e.g., player proximity).

```csharp
using UnityEngine;
using System.Linq; // For potential advanced line selection (not used in simple random)

/// <summary>
/// Represents a source of dialogue, typically attached to an NPC or an interactive object.
/// It holds a collection of DialogueLine assets and, based on defined conditions (like proximity),
/// requests the CrowdDialogueManager to speak one of its lines.
/// </summary>
public class DialogueSource : MonoBehaviour
{
    [Header("Dialogue Content")]
    [Tooltip("The array of DialogueLine ScriptableObjects this source can speak.")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("Request Behavior Settings")]
    [Tooltip("The base priority this source's requests will have. Higher values (e.g., 5 for important NPCs, 1 for background chatter) get processed sooner.")]
    [SerializeField] private float requestPriority = 1f;
    [Tooltip("Minimum time (in seconds) between this source attempting to make a new dialogue request, regardless if the manager accepts it.")]
    [SerializeField] private float minRequestInterval = 5f;
    [Tooltip("The radius around the player within which this source will try to speak. Requires the player GameObject to have the 'Player' tag.")]
    [SerializeField] private float triggerRadius = 5f;

    private float lastRequestTime = -Mathf.Infinity; // Tracks last time a request was made, allows immediate first request.
    private Transform playerTransform;               // Cached reference to the player's transform for proximity checks.

    private void Start()
    {
        // Disable this component if no dialogue lines are assigned, as it has nothing to say.
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"DialogueSource on {gameObject.name}: No dialogue lines assigned. Disabling component.", this);
            enabled = false;
            return;
        }

        // Attempt to find the player by tag. Ensure your player GameObject has the "Player" tag.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("DialogueSource: No GameObject with tag 'Player' found. Proximity triggering will not work for " + gameObject.name, this);
        }
    }

    private void Update()
    {
        // Only attempt to make a new request if enough time has passed since the last attempt.
        if (Time.time < lastRequestTime + minRequestInterval)
        {
            return;
        }

        // Proximity-based triggering:
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= triggerRadius)
            {
                TrySpeakRandomLine();
            }
        }
        else // If no player is found, just try to speak based on interval
        {
            TrySpeakRandomLine();
        }
    }

    /// <summary>
    /// Attempts to make this source speak a random dialogue line from its assigned collection.
    /// This request is sent to the CrowdDialogueManager, which decides the actual timing and display.
    /// </summary>
    public void TrySpeakRandomLine()
    {
        // Ensure the manager exists in the scene
        if (CrowdDialogueManager.Instance == null)
        {
            Debug.LogError("CrowdDialogueManager.Instance is null. Is the manager GameObject present in the scene?", this);
            return;
        }

        // Defensive check, though should be caught in Start()
        if (dialogueLines.Length == 0) return;

        // Select a random dialogue line from the assigned array
        DialogueLine lineToSpeak = dialogueLines[Random.Range(0, dialogueLines.Length)];

        // Request dialogue from the manager. The manager handles prioritization and cooldowns.
        CrowdDialogueManager.Instance.RequestDialogue(lineToSpeak, this, requestPriority);

        // Update the last request time, even if the manager ultimately ignores the request due to cooldowns.
        // This ensures this specific source respects its own minRequestInterval.
        lastRequestTime = Time.time;
    }

    /// <summary>
    /// Example of how to trigger a specific line (e.g., from an animation event, or an external script).
    /// </summary>
    /// <param name="index">The index of the dialogue line in the 'dialogueLines' array.</param>
    public void SpeakSpecificLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Length)
        {
            Debug.LogWarning($"DialogueSource on {gameObject.name}: Invalid line index {index}.", this);
            return;
        }
        if (CrowdDialogueManager.Instance == null) return;
        CrowdDialogueManager.Instance.RequestDialogue(dialogueLines[index], this, requestPriority + 1); // Maybe slightly higher priority for directed lines
        lastRequestTime = Time.time;
    }


    // Visualise the trigger radius in the Unity editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }

    // --- Example Usage (Programmatic Triggering) ---
    // You can call TrySpeakRandomLine() or SpeakSpecificLine() from other scripts:

    /*
    // Example: A separate interaction script detects player click
    public class InteractableNPC : MonoBehaviour
    {
        public DialogueSource dialogueSource;
        public DialogueLine directGreeting; // A specific line for direct interaction

        void OnMouseDown() // Or using Unity's new Input System / Raycasting
        {
            if (dialogueSource != null && CrowdDialogueManager.Instance != null)
            {
                // Request a specific greeting with high priority
                CrowdDialogueManager.Instance.RequestDialogue(directGreeting, dialogueSource, 10f);
            }
        }
    }
    */
}
```

---

## Unity Project Setup Guide

Follow these steps to set up the example in a new or existing Unity project.

1.  **Create a New Unity Project:**
    *   Open Unity Hub, create a new 3D or 2D project.

2.  **Import TextMeshPro Essentials:**
    *   Go to `Window > TextMeshPro > Import TMP Essential Resources`. This is required for `TextMeshProUGUI`.

3.  **Create C# Scripts:**
    *   In your `Assets` folder, create a new folder named `Scripts`.
    *   Create four new C# scripts inside `Scripts`: `DialogueLine`, `CrowdDialogueManager`, `DialogueUI`, and `DialogueSource`.
    *   Copy and paste the code from above into their respective files.

4.  **Create Dialogue Lines (ScriptableObjects):**
    *   In your `Assets` folder, create a new folder named `DialogueLines`.
    *   Right-click within the `DialogueLines` folder in the Project window: `Create > Crowd Dialogue > Dialogue Line`.
    *   Create several `DialogueLine` assets. For each:
        *   Give it a distinct `Speaker Name` (e.g., "Villager 1", "Old Man", "Shopkeeper").
        *   Write a `Dialogue Text` (e.g., "Hello there!", "The weather's fine!", "Got any coin?").
        *   (Optional) If you have `.wav` or `.mp3` files, drag them into the `Audio Clip` slot.
    *   Example names: `Line_Greeting`, `Line_Weather`, `Line_Shop`, `Line_Rumor`.

5.  **Set Up the UI Canvas:**
    *   In your scene, right-click in the Hierarchy: `UI > Canvas`. Rename it `CrowdDialogueCanvas`.
    *   Set the `Render Mode` of the Canvas to `Screen Space - Overlay`.
    *   Inside `CrowdDialogueCanvas`, right-click: `UI > Panel`. Rename it `DialoguePanel`.
        *   Adjust its `Rect Transform`: Set Anchors to `min(0,1), max(1,1)` (top stretch). Set `Pos Y` to `-75`, `Height` to `150`, `Left` and `Right` to `0`.
        *   Add a **Canvas Group** component to `DialoguePanel` (Add Component -> Layout -> Canvas Group).
        *   Attach the `DialogueUI.cs` script to `DialoguePanel`.
    *   Inside `DialoguePanel`, right-click: `UI > Text - TextMeshPro`. Rename it `SpeakerNameText`.
        *   Position it at the top-left (e.g., `Pos X: 10, Pos Y: -20`). Set a clear `Font Size` (e.g., 24), color, and `Font Style` (e.g., Bold).
    *   Inside `DialoguePanel`, right-click: `UI > Text - TextMeshPro`. Rename it `DialogueLineText`.
        *   Position it below the speaker name, spanning most of the panel (e.g., `Pos X: 0, Pos Y: -50`, adjust `Width` and `Height` to fit). Set a `Font Size` (e.g., 18). Ensure `Word Wrap` is enabled.
    *   **Configure `DialogueUI` script:**
        *   On `DialoguePanel`, drag `SpeakerNameText` to the `Speaker Name Text` field.
        *   Drag `DialogueLineText` to the `Dialogue Line Text` field.
        *   Drag the `Canvas Group` component (on `DialoguePanel` itself) to the `Canvas Group` field.

6.  **Set Up `CrowdDialogueManager`:**
    *   Create an empty GameObject in your scene, name it `CrowdDialogueManager`.
    *   Attach the `CrowdDialogueManager.cs` script to it.
    *   Add an **Audio Source** component to `CrowdDialogueManager` (Add Component -> Audio -> Audio Source).
    *   **Configure `CrowdDialogueManager` script:**
        *   Drag your `DialoguePanel` (which has the `DialogueUI.cs` script) to the `Dialogue UI` field.
        *   Drag the **Audio Source** component (on `CrowdDialogueManager` itself) to the `Crowd Audio Source` field.
        *   Adjust `Default Display Duration`, `Speaker Cooldown`, `Max Queue Size` as desired.

7.  **Create a Player Object:**
    *   Create an empty GameObject, name it `Player`.
    *   Add a simple visual (e.g., `3D Object > Capsule`) to `Player`.
    *   Add a component that allows movement (e.g., a simple `PlayerMovement` script, or a `CharacterController` with basic input).
    *   **Crucially:** Select the `Player` GameObject. In the Inspector, click the `Tag` dropdown, then `Add Tag...`. Type "Player" and save. Re-select the `Player` GameObject and assign the new "Player" tag to it. This tag is used by `DialogueSource` for proximity detection.

8.  **Create NPCs (Dialogue Sources):**
    *   Create an empty GameObject, name it `NPC_Villager1`.
    *   Add a simple visual (e.g., `3D Object > Cube` or `Capsule`).
    *   Attach the `DialogueSource.cs` script to `NPC_Villager1`.
    *   **Configure `DialogueSource` script:**
        *   Drag your `DialogueLine` ScriptableObjects (e.g., `Line_Greeting`, `Line_Weather`) from the `DialogueLines` folder into the `Dialogue Lines` array in the inspector.
        *   Adjust `Request Priority` (e.g., 1 for general chatter, 5 for an important NPC).
        *   Adjust `Min Request Interval` (e.g., 5-10 seconds).
        *   Adjust `Trigger Radius` (e.g., 5 units).
    *   Duplicate `NPC_Villager1` (`Ctrl+D` or `Cmd+D`) several times. Place them at different locations in your scene. Assign different `DialogueLine` assets and perhaps varied `Request Priority` values to each. This creates your "crowd".

9.  **Run the Scene:**
    *   Start the game.
    *   Move your player object around. As you approach different NPCs, they will begin to request dialogue. The `CrowdDialogueManager` will prioritize, manage cooldowns, and display the chosen lines on your UI. You'll see how different NPCs get a chance to "speak" based on your proximity, their priority, and the system's internal cooldowns.

This setup provides a fully functional, practical, and educational example of the Crowd Dialogue System pattern in Unity.