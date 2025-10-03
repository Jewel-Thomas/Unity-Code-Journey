// Unity Design Pattern Example: CutsceneSkipSystem
// This script demonstrates the CutsceneSkipSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Cutscene Skip System** design pattern. It's designed to be educational, demonstrating how to build a flexible system for handling skippable in-game sequences.

The pattern centralizes the skip logic, allowing individual cutscene components to define *how* they respond to a skip request without needing to handle input themselves.

---

### `CutsceneSkipSystemExample.cs`

```csharp
using UnityEngine;
using System.Collections; // For IEnumerator
using System.Collections.Generic; // For List
// For UI Text, uncomment these and assign in inspector if you want on-screen UI:
// using UnityEngine.UI; 
// using TMPro; // If using TextMeshPro, requires importing TextMeshPro essentials

/// <summary>
/// Defines the contract for any object that represents a skippable cutscene.
/// Cutscene components (e.g., dialogue managers, timeline players, animation sequences)
/// that want to allow skipping should implement this interface.
/// </summary>
public interface ICutsceneSkipper
{
    /// <summary>
    /// Instructs the cutscene to immediately skip to its conclusion or
    /// fast-forward its current segment. The implementation should handle
    /// stopping ongoing processes (coroutines, animations, timelines) and
    /// transitioning to the cutscene's end state.
    /// </summary>
    void SkipCutscene();
}

/// <summary>
/// The central manager for the Cutscene Skip System pattern.
/// It detects user input for skipping and notifies all currently
/// registered and active skippable cutscenes to perform their skip logic.
/// This acts as the 'Invoker' in a Command pattern, sending a 'Skip' command
/// to all 'Receivers' (ICutsceneSkipper implementations).
/// </summary>
public class CutsceneSkipSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    // This allows any part of the game to access the skip system without direct references.
    public static CutsceneSkipSystem Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("The key that the player presses to request a cutscene skip.")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    
    [Tooltip("How frequently (in seconds) to log the skip prompt when a cutscene is skippable.")]
    [SerializeField] private float logPromptInterval = 5f;

    // Optional: Reference to a UI Text element to display the skip prompt on screen.
    // Uncomment and assign in Inspector if you wish to use UI.
    // [SerializeField] private Text skipPromptText; 
    // [SerializeField] private TMPro.TextMeshProUGUI skipPromptTextPro;

    // A list of all ICutsceneSkipper components that are currently active
    // and can be skipped. Cutscenes register themselves when they start
    // and unregister when they finish. This list represents the 'active commands'
    // that can respond to a skip request.
    private readonly List<ICutsceneSkipper> _activeSkippables = new List<ICutsceneSkipper>();

    private float _lastLogTime; // Tracks when the last skip prompt was logged to avoid spam.

    /// <summary>
    /// Returns true if there are any cutscenes currently active and registered
    /// with the system, indicating that a skip is possible.
    /// This can be used by UI elements to show/hide a "Press [KEY] to Skip" message.
    /// </summary>
    public bool IsSkipPossible => _activeSkippables.Count > 0;

    private void Awake()
    {
        // Singleton enforcement: Ensures only one instance of the manager exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CutsceneSkipSystem instances found! Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Makes the system persist across scene loads.
            // If cutscenes don't need to be skipped across scene loads, this can be removed.
            DontDestroyOnLoad(gameObject); 
        }
    }

    private void Update()
    {
        // Only check for skip input if a cutscene is currently active and skippable.
        if (IsSkipPossible)
        {
            // Log a prompt occasionally to inform the player that skipping is available.
            // This prevents console spam and provides feedback.
            if (Time.time >= _lastLogTime + logPromptInterval)
            {
                Debug.Log($"[CutsceneSkipSystem] A cutscene is playing. Press '{skipKey.ToString().ToUpper()}' to skip.", this);
                _lastLogTime = Time.time;
            }

            // Optional: UI update for skip prompt
            // if (skipPromptText != null) {
            //     if (!skipPromptText.gameObject.activeSelf) skipPromptText.gameObject.SetActive(true);
            //     skipPromptText.text = $"Press {skipKey.ToString().ToUpper()} to skip...";
            // }
            // if (skipPromptTextPro != null) {
            //     if (!skipPromptTextPro.gameObject.activeSelf) skipPromptTextPro.gameObject.SetActive(true);
            //     skipPromptTextPro.text = $"Press {skipKey.ToString().ToUpper()} to skip...";
            // }

            // Detect player input for skipping.
            if (Input.GetKeyDown(skipKey))
            {
                Debug.Log($"[CutsceneSkipSystem] Skip requested! Notifying {Instance._activeSkippables.Count} active skippers.", this);
                SkipAllActiveCutscenes(); // Trigger the skip action.
            }
        }
        // Optional: Hide UI skip prompt if no cutscene is skippable
        // else {
        //     if (skipPromptText != null && skipPromptText.gameObject.activeSelf) skipPromptText.gameObject.SetActive(false);
        //     if (skipPromptTextPro != null && skipPromptTextPro.gameObject.activeSelf) skipPromptTextPro.gameObject.SetActive(false);
        // }
    }

    /// <summary>
    /// Registers an ICutsceneSkipper with the system.
    /// This should be called by a cutscene component when it starts playing
    /// and becomes eligible for skipping. This makes the cutscene a 'Receiver'
    /// of the skip command.
    /// </summary>
    /// <param name="skipper">The ICutsceneSkipper to register.</param>
    public void RegisterSkipper(ICutsceneSkipper skipper)
    {
        if (!_activeSkippables.Contains(skipper))
        {
            _activeSkippables.Add(skipper);
            // Log which skipper was registered for debugging purposes.
            Debug.Log($"[CutsceneSkipSystem] Registered new skipper: {((MonoBehaviour)skipper).name}. Total active: {_activeSkippables.Count}", this);
            _lastLogTime = Time.time; // Reset log timer when a new cutscene starts, ensuring immediate prompt.
        }
    }

    /// <summary>
    /// Unregisters an ICutsceneSkipper from the system.
    /// This should be called by a cutscene component when it finishes playing
    /// (either naturally or by being skipped) and is no longer skippable.
    /// </summary>
    /// <param name="skipper">The ICutsceneSkipper to unregister.</param>
    public void UnregisterSkipper(ICutsceneSkipper skipper)
    {
        if (_activeSkippables.Remove(skipper))
        {
            // Log which skipper was unregistered.
            Debug.Log($"[CutsceneSkipSystem] Unregistered skipper: {((MonoBehaviour)skipper).name}. Total active: {_activeSkippables.Count}", this);
        }
    }

    /// <summary>
    /// Iterates through all currently registered skippable cutscenes and
    /// calls their SkipCutscene method. This is the core logic that dispatches
    /// the 'skip command' to all listening cutscenes.
    /// </summary>
    private void SkipAllActiveCutscenes()
    {
        // Create a copy of the list to avoid "Collection was modified" errors.
        // This is crucial because a skipper's SkipCutscene() method might call
        // UnregisterSkipper(), modifying the original _activeSkippables list
        // while it's being iterated over.
        List<ICutsceneSkipper> skippersToSkip = new List<ICutsceneSkipper>(_activeSkippables);

        foreach (var skipper in skippersToSkip)
        {
            // Call the SkipCutscene method on each active skipper.
            // Each skipper is responsible for implementing its own skip logic.
            skipper.SkipCutscene();
            // Note: The skipper itself is responsible for calling UnregisterSkipper(this)
            // when it completes its skip logic and is no longer active.
        }
    }
}

/// <summary>
/// An abstract base class for cutscene components that can be skipped.
/// It implements the ICutsceneSkipper interface and handles the common
/// lifecycle (registration/unregistration) with the CutsceneSkipSystem.
/// Derived classes will implement the specific cutscene behavior and
/// the concrete SkipCutscene logic.
/// </summary>
public abstract class BaseCutscene : MonoBehaviour, ICutsceneSkipper
{
    [Header("Base Cutscene Settings")]
    [Tooltip("Is this cutscene currently playing?")]
    [SerializeField] protected bool _isPlaying = false;
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Initiates the cutscene playback.
    /// Derived classes should call `base.BeginCutscene()` first, then implement
    /// their specific cutscene logic (e.g., start coroutines, play animations).
    /// Calls RegisterSkipper() with the CutsceneSkipSystem to make itself skippable.
    /// </summary>
    public virtual void BeginCutscene()
    {
        if (_isPlaying)
        {
            Debug.LogWarning($"Cutscene '{name}' is already playing, ignoring BeginCutscene() call.", this);
            return;
        }

        _isPlaying = true;
        // Register with the skip system so it knows this cutscene can be skipped.
        // This is a key part of the pattern, allowing the system to know which cutscenes are active.
        if (CutsceneSkipSystem.Instance != null)
        {
            CutsceneSkipSystem.Instance.RegisterSkipper(this);
        }
        Debug.Log($"[BaseCutscene] '{name}' started. Registered for skipping.", this);
    }

    /// <summary>
    /// Ends the cutscene playback and performs cleanup.
    /// Derived classes should call `base.EndCutscene()` at the end of their
    /// cutscene logic or when skipping is complete.
    /// Calls UnregisterSkipper() with the CutsceneSkipSystem.
    /// </summary>
    protected virtual void EndCutscene()
    {
        if (!_isPlaying)
        {
            // Already ended or wasn't playing.
            return;
        }

        _isPlaying = false;
        // Unregister from the skip system as this cutscene is no longer playing.
        // This is crucial for managing the list of active skippable cutscenes.
        if (CutsceneSkipSystem.Instance != null)
        {
            CutsceneSkipSystem.Instance.UnregisterSkipper(this);
        }
        Debug.Log($"[BaseCutscene] '{name}' finished. Unregistered from skipping.", this);
    }

    /// <summary>
    /// Abstract method to be implemented by derived classes.
    /// This method defines the specific behavior when the cutscene is skipped.
    /// Implementations should stop ongoing cutscene logic and call `EndCutscene()`.
    /// </summary>
    public abstract void SkipCutscene();

    /// <summary>
    /// Ensures unregistration if the GameObject is destroyed or disabled while playing,
    /// preventing stale references in the CutsceneSkipSystem.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (_isPlaying && CutsceneSkipSystem.Instance != null)
        {
            CutsceneSkipSystem.Instance.UnregisterSkipper(this);
            Debug.LogWarning($"[BaseCutscene] '{name}' was disabled/destroyed while playing. Unregistered from skipping to prevent issues.", this);
        }
    }
}

/// <summary>
/// A concrete example of a skippable cutscene.
/// This simulates a simple dialogue sequence displayed line by line in the console.
/// When skipped, it immediately finishes the dialogue and ends.
/// </summary>
public class ExampleDialogueCutscene : BaseCutscene
{
    [Header("Dialogue Settings")]
    [Tooltip("The lines of dialogue to display.")]
    [TextArea(3, 10)] // Makes the string array editable in a larger text area
    [SerializeField] private string[] dialogueLines = new string[] {
        "Greetings, adventurer! Welcome to the mystical land of Eldoria.",
        "A great peril awaits, but fear not, for courage is your greatest weapon.",
        "You must embark on a quest to retrieve the legendary Amulet of Light.",
        "It lies deep within the Shadowfen Crypt, guarded by ancient spirits.",
        "Are you ready to face your destiny?"
    };
    [Tooltip("Time delay between each dialogue line.")]
    [SerializeField] private float lineDisplayTime = 3.0f;
    
    // Optional: Reference to a UI Text component (e.g., UI Text, TextMeshPro) to display dialogue.
    // Uncomment and assign for actual UI display.
    // [SerializeField] private Text uiDialogueText; 
    // [SerializeField] private TMPro.TextMeshProUGUI uiDialogueTextPro; 

    private int _currentLineIndex = 0;
    private Coroutine _dialogueCoroutine; // Holds a reference to the running dialogue coroutine.

    /// <summary>
    /// Overrides BeginCutscene to start the dialogue sequence.
    /// </summary>
    public override void BeginCutscene()
    {
        base.BeginCutscene(); // Always call base method first for registration.

        // Optional: Activate UI elements
        // if (uiDialogueText != null) uiDialogueText.gameObject.SetActive(true);
        // if (uiDialogueTextPro != null) uiDialogueTextPro.gameObject.SetActive(true);

        _currentLineIndex = 0;
        // Start the coroutine that handles displaying dialogue lines.
        _dialogueCoroutine = StartCoroutine(PlayDialogueSequence());
        Debug.Log($"[DialogueCutscene] '{name}' dialogue sequence started.");
    }

    /// <summary>
    /// Coroutine to display dialogue lines one by one with a delay.
    /// This simulates a time-based cutscene.
    /// </summary>
    private IEnumerator PlayDialogueSequence()
    {
        while (_currentLineIndex < dialogueLines.Length && _isPlaying)
        {
            string currentLine = dialogueLines[_currentLineIndex];
            Debug.Log($"<color=cyan>[DialogueCutscene] Line {_currentLineIndex + 1}:</color> {currentLine}");

            // Optional: Update UI text
            // if (uiDialogueText != null) uiDialogueText.text = currentLine;
            // if (uiDialogueTextPro != null) uiDialogueTextPro.text = currentLine;

            yield return new WaitForSeconds(lineDisplayTime); // Wait for the specified time.
            _currentLineIndex++;
        }

        // If the loop finished naturally (all lines displayed), end the cutscene.
        if (_isPlaying)
        {
            EndCutscene();
        }
    }

    /// <summary>
    /// Implements the SkipCutscene logic for the dialogue sequence.
    /// It immediately ends the current coroutine and proceeds to the
    /// end of the cutscene, simulating a skip.
    /// </summary>
    public override void SkipCutscene()
    {
        if (!_isPlaying)
        {
            Debug.LogWarning($"[DialogueCutscene] Attempted to skip '{name}' but it's not playing.", this);
            return;
        }

        Debug.Log($"[DialogueCutscene] '{name}' skipped! Finishing sequence.");

        // Stop the ongoing dialogue coroutine.
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        // You might want to display the last line quickly, or a "Skipped" message
        // if using a UI. For this example, we just end.

        // Immediately end the cutscene after skipping. This will also unregister it.
        EndCutscene();
    }

    /// <summary>
    /// Overrides EndCutscene to handle specific cleanup for the dialogue.
    /// </summary>
    protected override void EndCutscene()
    {
        if (!_isPlaying) return; // Prevent double-ending if already processed by SkipCutscene or natural end.

        // Ensure the coroutine is stopped if it's still running.
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        // Optional: Deactivate UI elements
        // if (uiDialogueText != null) uiDialogueText.gameObject.SetActive(false);
        // if (uiDialogueTextPro != null) uiDialogueTextPro.gameObject.SetActive(false);

        Debug.Log($"[DialogueCutscene] '{name}' dialogue ended.");
        base.EndCutscene(); // Call base method last to handle unregistration and _isPlaying flag.
    }
}

/// <summary>
/// A simple component to trigger an example cutscene for demonstration purposes.
/// Attach this to an empty GameObject in your scene.
/// </summary>
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The key to press to trigger the cutscene.")]
    [SerializeField] private KeyCode triggerKey = KeyCode.E;
    [Tooltip("The ExampleDialogueCutscene instance to trigger.")]
    [SerializeField] private ExampleDialogueCutscene targetCutscene;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (targetCutscene != null)
            {
                // Only trigger if the cutscene isn't already playing.
                if (!targetCutscene.IsPlaying)
                {
                    Debug.Log($"[CutsceneTrigger] Press '{triggerKey.ToString().ToUpper()}' detected. Triggering cutscene: {targetCutscene.name}", this);
                    targetCutscene.BeginCutscene(); // Start the cutscene.
                }
                else
                {
                    Debug.LogWarning($"[CutsceneTrigger] Cutscene '{targetCutscene.name}' is already playing.", this);
                }
            }
            else
            {
                Debug.LogError("[CutsceneTrigger] No target cutscene assigned! Please assign an 'ExampleDialogueCutscene' in the inspector.", this);
            }
        }
    }
}

/*
--- HOW TO USE THIS EXAMPLE IN UNITY ---

This script demonstrates the Cutscene Skip System pattern using a simple dialogue sequence.
Follow these steps to set it up in your Unity project:

1.  **Create a C# Script:**
    *   In your Unity project, right-click in the Project window -> Create -> C# Script.
    *   Name it `CutsceneSkipSystemExample.cs`.
    *   Copy the entire content of this file into the newly created script, overwriting any default content.

2.  **Create the CutsceneSkipSystem GameObject:**
    *   In your Unity scene Hierarchy, right-click -> Create Empty.
    *   Rename this new GameObject to "CutsceneSkipSystem".
    *   Drag and drop the `CutsceneSkipSystemExample.cs` script onto this "CutsceneSkipSystem" GameObject in the Inspector.
    *   This GameObject will manage all skip requests. You can customize the `Skip Key` (default: Spacebar) in its Inspector.
    *   (Optional but recommended): For projects with multiple scenes, consider keeping this GameObject in a dedicated "Managers" scene or making it truly persistent using `DontDestroyOnLoad`.

3.  **Create an ExampleDialogueCutscene GameObject:**
    *   In your scene Hierarchy, right-click -> Create Empty.
    *   Rename this new GameObject to "DialogueCutscene_Intro".
    *   Drag and drop the `CutsceneSkipSystemExample.cs` script onto this "DialogueCutscene_Intro" GameObject.
    *   In the Inspector, locate the `Example Dialogue Cutscene` section.
    *   You can customize the `Dialogue Lines` (add/remove lines, change text) and `Line Display Time` (time each line stays on screen). This GameObject represents an actual cutscene sequence.

4.  **Create a CutsceneTrigger GameObject:**
    *   In your scene Hierarchy, right-click -> Create Empty.
    *   Rename this new GameObject to "CutsceneActivator".
    *   Drag and drop the `CutsceneSkipSystemExample.cs` script onto this "CutsceneActivator" GameObject.
    *   In the Inspector, locate the `Cutscene Trigger` section.
    *   Drag the "DialogueCutscene_Intro" GameObject from your Hierarchy into the `Target Cutscene` slot of the "CutsceneActivator".
    *   You can customize the `Trigger Key` (default: 'E') for starting the cutscene.

5.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Press the 'E' key (or your chosen `Trigger Key`) to start the `DialogueCutscene_Intro`.
    *   Observe the dialogue lines appearing sequentially in the Console window. You'll also see messages from the `CutsceneSkipSystem` indicating a cutscene is playing and can be skipped.
    *   While the dialogue is playing, press the 'Spacebar' key (or your chosen `Skip Key` on the "CutsceneSkipSystem" GameObject).
    *   You will see console messages confirming the skip request and the dialogue cutscene immediately ending, demonstrating the pattern in action.

---

### EXTENDING WITH UI (Optional)

To display the dialogue and skip prompt on the screen using Unity's UI system:

1.  **Add UI Canvas:**
    *   In your Hierarchy, right-click -> UI -> Canvas.
    *   Select the Canvas. In the Inspector, set its `Render Mode` to "Screen Space - Camera" and drag your `Main Camera` into the `Render Camera` slot.
    *   Set `UI Scale Mode` to "Scale With Screen Size" for better responsiveness across different resolutions.

2.  **Add Dialogue Text Element:**
    *   Right-click on your Canvas GameObject -> UI -> Text (Legacy) or UI -> Text - TextMeshPro (requires importing TextMeshPro essentials from Window -> TextMeshPro -> Import TMP Essential Resources).
    *   Rename it to "DialogueText".
    *   Adjust its `Rect Transform` (size, position) and `Font Size` for visibility. Set its text color to something readable. Initially, you might want to disable this GameObject.

3.  **Add Skip Prompt Text Element:**
    *   Repeat the above step to create another Text element. Rename it "SkipPromptText".
    *   Position it somewhere visible (e.g., bottom-right corner). Adjust its font and size. Initially, disable this GameObject.

4.  **Modify `CutsceneSkipSystemExample.cs` (Uncomment UI-related lines):**
    *   At the very top of the script, uncomment:
        ```csharp
        using UnityEngine.UI; 
        // using TMPro; // If you chose TextMeshPro
        ```
    *   **In the `CutsceneSkipSystem` class:**
        *   Uncomment one of these lines, depending on whether you're using Legacy Text or TextMeshPro:
            ```csharp
            [SerializeField] private Text skipPromptText; 
            // [SerializeField] private TMPro.TextMeshProUGUI skipPromptTextPro;
            ```
        *   Inside the `Update()` method, uncomment and ensure `skipPromptText` (or `skipPromptTextPro`) is updated:
            ```csharp
            // (Optional) Update UI for skip prompt
            if (skipPromptText != null) 
            {
                bool showPrompt = IsSkipPossible;
                if (skipPromptText.gameObject.activeSelf != showPrompt)
                {
                    skipPromptText.gameObject.SetActive(showPrompt);
                }
                if (showPrompt)
                {
                    skipPromptText.text = $"Press {skipKey.ToString().ToUpper()} to skip...";
                }
            }
            // if (skipPromptTextPro != null) 
            // {
            //     bool showPrompt = IsSkipPossible;
            //     if (skipPromptTextPro.gameObject.activeSelf != showPrompt)
            //     {
            //         skipPromptTextPro.gameObject.SetActive(showPrompt);
            //     }
            //     if (showPrompt)
            //     {
            //         skipPromptTextPro.text = $"Press {skipKey.ToString().ToUpper()} to skip...";
            //     }
            // }
            ```
    *   **In the `ExampleDialogueCutscene` class:**
        *   Uncomment one of these lines, depending on your UI choice:
            ```csharp
            [SerializeField] private Text uiDialogueText; 
            // [SerializeField] private TMPro.TextMeshProUGUI uiDialogueTextPro; 
            ```
        *   In `BeginCutscene()`: Uncomment `uiDialogueText.gameObject.SetActive(true);` (or `uiDialogueTextPro`).
        *   In `PlayDialogueSequence()`: Uncomment `uiDialogueText.text = currentLine;` (or `uiDialogueTextPro`).
        *   In `EndCutscene()`: Uncomment `uiDialogueText.gameObject.SetActive(false);` (or `uiDialogueTextPro`).

5.  **Assign UI Elements in Inspector:**
    *   Select your "CutsceneSkipSystem" GameObject. Drag your "SkipPromptText" (from the Canvas) to the `Skip Prompt Text` slot in its Inspector.
    *   Select your "DialogueCutscene_Intro" GameObject. Drag your "DialogueText" (from the Canvas) to the `UI Dialogue Text` slot in its Inspector.

Now, when you run the scene and trigger the cutscene, the dialogue will appear on your UI, and a "Press [KEY] to skip" prompt will dynamically appear/disappear!
*/
```