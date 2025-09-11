// Unity Design Pattern Example: CutsceneEventSystem
// This script demonstrates the CutsceneEventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **CutsceneEventSystem** design pattern in Unity. This pattern provides a flexible, decoupled way to orchestrate various actions (dialogue, camera movements, animations, audio, etc.) during a cutscene. It separates the cutscene's sequence definition from the execution of individual actions, making cutscenes easier to create, modify, and extend.

**Key Components of the Pattern:**

1.  **`CutsceneEventData` (Base Class):** An abstract base class that defines the common interface for all types of cutscene events.
2.  **`ConcreteCutsceneEventData` (Derived Classes):** Specific data classes (e.g., `DialogueEventData`, `CameraChangeEventData`) that inherit from `CutsceneEventData` and hold information relevant to their specific event type.
3.  **`CutsceneEvent` (Serializable Struct/Class):** A container that couples a `delayBeforeExecution` (how long to wait before this event) with its `CutsceneEventData`.
4.  **`CutsceneManager` (Publisher/Dispatcher):** A central `MonoBehaviour` that holds a sequence of `CutsceneEvent` objects. It iterates through them, waits for delays, and dispatches a generic `Action<CutsceneEventData>` event for each event in the sequence.
5.  **`CutsceneListener` (Abstract Base Listener):** An abstract `MonoBehaviour` that provides a common subscription/unsubscription mechanism for all concrete listeners.
6.  **`ConcreteCutsceneListeners` (Subscribers):** Specific `MonoBehaviour` classes (e.g., `DialogueDisplay`, `CameraDirector`, `AnimatorTrigger`) that inherit from `CutsceneListener`. They listen for `CutsceneManager` events, cast the received `CutsceneEventData` to their specific type, and perform their respective actions.

---

**Instructions for Use:**

1.  **Create C# Scripts:** Copy the code below into separate C# files in your Unity project, named exactly as the class names (e.g., `CutsceneManager.cs`, `DialogueEventData.cs`, `DialogueDisplay.cs`, etc.).
2.  **Setup Scene:**
    *   Create an empty GameObject in your scene named `_CutsceneManager_`. Attach the `CutsceneManager.cs` script to it.
    *   Create another empty GameObject named `_DialogueSystem_`. Attach the `DialogueDisplay.cs` script to it.
    *   Create another empty GameObject named `_CameraDirector_`. Attach the `CameraDirector.cs` script to it.
    *   Create another empty GameObject named `_AudioPlayer_`. Attach the `AudioPlayer.cs` script to it. Add an `AudioSource` component to it.
    *   Create a simple 3D object (e.g., a Cube) named `PlayerCharacter`. Attach an `AnimatorTrigger.cs` script to it. Add an `Animator` component to it and a simple Animator Controller (even an empty one will work for this example's trigger to register).
    *   Create two empty GameObjects named `CameraTarget1` and `CameraTarget2`. Position them differently in the scene.
    *   Create a UI Text element (UGUI) to display dialogue. Drag this `Text` component into the `DialogueDisplay` script's `dialogueText` field in the Inspector.
    *   You'll need a simple `AudioClip` for the audio event. Drag one into the `AudioPlayer` script's `audioSource` component.
3.  **Configure `CutsceneManager`:**
    *   Select the `_CutsceneManager_` GameObject.
    *   In the Inspector, expand the `Cutscene Events` list.
    *   Add new elements to define your cutscene sequence. For each element:
        *   Set `Delay Before Execution` (time in seconds before this event fires).
        *   Choose the `Event Data Type` (e.g., `DialogueEventData`).
        *   Fill in the specific details for that event type (e.g., `Speaker`, `DialogueText` for a Dialogue event; `Target Transform` for a Camera event).
    *   Add a button to your UI or use a simple `Input.GetKeyDown(KeyCode.Space)` in a new script (or the `CutsceneManager`) to call `CutsceneManager.Instance.StartCutscene()`.
4.  **Run:** Play the scene and trigger the cutscene. Observe how dialogue appears, the camera moves, animations trigger, and audio plays, all orchestrated by the `CutsceneManager` without any direct communication between the individual systems.

---

### File 1: `CutsceneEventData.cs`

```csharp
using UnityEngine;
using System;

/// <summary>
/// Base class for all cutscene event data.
/// This abstract class ensures that all specific event types
/// derive from a common base, allowing the CutsceneManager to dispatch
/// generic events and for listeners to process them polymorphically.
/// </summary>
[System.Serializable]
public abstract class CutsceneEventData
{
    // Common properties or methods can go here if needed for all events.
    // For now, it's just a marker base class.
}

/// <summary>
/// Concrete event data for displaying dialogue.
/// </summary>
[System.Serializable]
public class DialogueEventData : CutsceneEventData
{
    public string speaker;
    [TextArea(3, 5)]
    public string dialogueText;
    public float displayDuration = 3f; // How long this dialogue should be on screen
}

/// <summary>
/// Concrete event data for changing the camera's focus/position.
/// </summary>
[System.Serializable]
public class CameraChangeEventData : CutsceneEventData
{
    public Transform targetTransform;
    public float transitionSpeed = 1f; // Speed of camera movement/rotation
    public CameraTransitionType transitionType = CameraTransitionType.LerpPositionAndRotation;
}

public enum CameraTransitionType
{
    Instant,
    LerpPosition,
    LerpRotation,
    LerpPositionAndRotation
}

/// <summary>
/// Concrete event data for triggering an animator parameter.
/// </summary>
[System.Serializable]
public class AnimationTriggerEventData : CutsceneEventData
{
    // Instead of directly referencing an Animator (which can't be serialized in a ScriptableObject/Serializable class easily
    // if the target animator is not on the same GameObject as the CutsceneManager itself),
    // we use a tag or name to identify the target game object, or expect the listener to
    // find its own animator. For this example, the listener will just use its own animator.
    public string triggerName; // The name of the Animator Trigger parameter
}

/// <summary>
/// Concrete event data for playing a sound effect.
/// </summary>
[System.Serializable]
public class AudioEventData : CutsceneEventData
{
    public AudioClip audioClip;
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool loop = false;
    public bool stopPrevious = true; // Whether to stop any currently playing audio before starting this one
}

/// <summary>
/// Concrete event data for a generic action (e.g., enable/disable a GameObject, activate an effect).
/// This allows for more dynamic, custom actions without creating a new CutsceneEventData type for every tiny thing.
/// A listener would interpret the 'actionName' and 'parameters'.
/// </summary>
[System.Serializable]
public class GenericActionEventData : CutsceneEventData
{
    public string actionName; // e.g., "EnableObject", "DisableObject", "SpawnParticles"
    public string[] parameters; // Optional parameters, e.g., ["TargetObjectTag", "GameObjectToActivate"]
}
```

### File 2: `CutsceneManager.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // For Action delegate

/// <summary>
/// Represents a single event within the cutscene sequence.
/// It couples a delay with the specific event data.
/// </summary>
[System.Serializable]
public class CutsceneEvent
{
    [Tooltip("Delay in seconds before this event is triggered, AFTER the previous event is dispatched.")]
    public float delayBeforeExecution = 0f;

    [Tooltip("The actual data for this cutscene event.")]
    [SerializeReference] // Allows serialization of derived classes directly in the Inspector
    public CutsceneEventData eventData;
}

/// <summary>
/// The central orchestrator for cutscene events.
/// This MonoBehaviour manages the sequence of events, dispatches them,
/// and handles the timing between events.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    // Singleton pattern for easy access from anywhere
    public static CutsceneManager Instance { get; private set; }

    [Header("Cutscene Configuration")]
    [Tooltip("The list of events that make up this cutscene, in sequential order.")]
    [SerializeField]
    private List<CutsceneEvent> cutsceneEvents = new List<CutsceneEvent>();

    // This is the core event dispatcher.
    // Listeners subscribe to this event to receive notifications when a cutscene event occurs.
    public static event Action<CutsceneEventData> OnCutsceneEventTriggered;

    private Coroutine currentCutsceneRoutine;
    private bool isCutscenePlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally, Don'tDestroyOnLoad if this manager should persist across scenes
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Debug.LogWarning("Multiple CutsceneManagers found. Destroying duplicate: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initiates the playback of the cutscene.
    /// </summary>
    public void StartCutscene()
    {
        if (isCutscenePlaying)
        {
            Debug.LogWarning("Cutscene is already playing.");
            return;
        }

        Debug.Log("Starting cutscene...");
        isCutscenePlaying = true;
        currentCutsceneRoutine = StartCoroutine(PlayCutsceneRoutine());
    }

    /// <summary>
    /// Stops the currently playing cutscene, if any.
    /// </summary>
    public void StopCutscene()
    {
        if (currentCutsceneRoutine != null)
        {
            StopCoroutine(currentCutsceneRoutine);
            currentCutsceneRoutine = null;
            isCutscenePlaying = false;
            Debug.Log("Cutscene stopped.");
            // Optionally, dispatch a "CutsceneEnded" event here
        }
    }

    /// <summary>
    /// The coroutine that steps through each cutscene event,
    /// waits for the specified delays, and dispatches the events.
    /// </summary>
    private IEnumerator PlayCutsceneRoutine()
    {
        for (int i = 0; i < cutsceneEvents.Count; i++)
        {
            CutsceneEvent currentEvent = cutsceneEvents[i];

            // Wait for the specified delay before executing this event.
            if (currentEvent.delayBeforeExecution > 0)
            {
                yield return new WaitForSeconds(currentEvent.delayBeforeExecution);
            }

            // Dispatch the event. Any subscribed listeners will receive this data.
            OnCutsceneEventTriggered?.Invoke(currentEvent.eventData);
            Debug.Log($"Dispatching event: {currentEvent.eventData.GetType().Name} at index {i}");

            // If the event is a dialogue event, wait for its display duration
            // before proceeding to the next event. This makes dialogue "blocking".
            // Other event types might not need to block the sequence.
            if (currentEvent.eventData is DialogueEventData dialogueData)
            {
                yield return new WaitForSeconds(dialogueData.displayDuration);
            }
        }

        Debug.Log("Cutscene finished!");
        isCutscenePlaying = false;
        currentCutsceneRoutine = null;
        // Optionally, dispatch a "CutsceneEnded" event here
    }

    // Example Usage: How to trigger the cutscene (e.g., from a button click or game event)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isCutscenePlaying)
        {
            StartCutscene();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && isCutscenePlaying)
        {
            StopCutscene();
        }
    }
}
```

### File 3: `CutsceneListener.cs`

```csharp
using UnityEngine;

/// <summary>
/// Abstract base class for all cutscene event listeners.
/// It provides common functionality for subscribing and unsubscribing
/// to the CutsceneManager's event, ensuring proper lifecycle management.
/// </summary>
public abstract class CutsceneListener : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        // Subscribe to the global cutscene event dispatcher.
        // This ensures that when the CutsceneManager dispatches an event,
        // this listener's HandleCutsceneEvent method will be called.
        CutsceneManager.OnCutsceneEventTriggered += HandleCutsceneEvent;
    }

    protected virtual void OnDisable()
    {
        // Unsubscribe from the event when the GameObject is disabled or destroyed.
        // This prevents memory leaks and ensures that no events are sent to a non-existent listener.
        CutsceneManager.OnCutsceneEventTriggered -= HandleCutsceneEvent;
    }

    /// <summary>
    /// Abstract method that concrete listeners must implement.
    /// This is where the specific logic for handling different cutscene events resides.
    /// </summary>
    /// <param name="eventData">The generic CutsceneEventData dispatched by the CutsceneManager.</param>
    protected abstract void HandleCutsceneEvent(CutsceneEventData eventData);
}
```

### File 4: `DialogueDisplay.cs`

```csharp
using UnityEngine;
using TMPro; // Assuming TextMeshPro for UI text
using System.Collections;

/// <summary>
/// A concrete listener that handles DialogueEventData.
/// It displays dialogue text on a UI Text element.
/// </summary>
public class DialogueDisplay : CutsceneListener
{
    [Header("UI References")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel; // Optional: Panel to show/hide dialogue UI

    private Coroutine hideDialogueRoutine;

    protected override void OnEnable()
    {
        base.OnEnable(); // Call base class OnEnable to subscribe to events
        if (dialoguePanel != null) dialoguePanel.SetActive(false); // Start with dialogue hidden
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // Call base class OnDisable to unsubscribe
        if (hideDialogueRoutine != null) StopCoroutine(hideDialogueRoutine);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    protected override void HandleCutsceneEvent(CutsceneEventData eventData)
    {
        // Try to cast the generic event data to DialogueEventData.
        // If it's not a dialogue event, this listener ignores it.
        if (eventData is DialogueEventData dialogueEvent)
        {
            DisplayDialogue(dialogueEvent);
        }
        // else if (eventData is OtherEventType otherEvent) { // Could handle other events if needed }
    }

    /// <summary>
    /// Displays the provided dialogue data on the UI.
    /// </summary>
    /// <param name="dialogueEvent">The dialogue event data to display.</param>
    private void DisplayDialogue(DialogueEventData dialogueEvent)
    {
        if (speakerText == null || dialogueText == null)
        {
            Debug.LogError("Dialogue UI references not set in DialogueDisplay!", this);
            return;
        }

        if (hideDialogueRoutine != null) StopCoroutine(hideDialogueRoutine);

        speakerText.text = dialogueEvent.speaker;
        dialogueText.text = dialogueEvent.dialogueText;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        Debug.Log($"<color=cyan>Dialogue: {dialogueEvent.speaker}: {dialogueEvent.dialogueText}</color>");

        // Start a coroutine to hide the dialogue after its display duration.
        hideDialogueRoutine = StartCoroutine(HideDialogueAfterDelay(dialogueEvent.displayDuration));
    }

    private IEnumerator HideDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        speakerText.text = "";
        dialogueText.text = "";
        Debug.Log("Dialogue hidden.");
    }
}
```

### File 5: `CameraDirector.cs`

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// A concrete listener that handles CameraChangeEventData.
/// It moves and rotates the main camera to specified target transforms.
/// </summary>
public class CameraDirector : CutsceneListener
{
    [Header("Camera References")]
    public Camera mainCamera; // Reference to the main camera in the scene

    [Tooltip("If true, the camera will instantly snap to the target. Otherwise, it will smoothly transition.")]
    public bool instantSnapDefault = false;

    private Coroutine cameraTransitionRoutine;

    protected override void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Try to find the main camera if not set
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found or assigned to CameraDirector!", this);
                enabled = false; // Disable if no camera to control
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }
    }

    protected override void HandleCutsceneEvent(CutsceneEventData eventData)
    {
        if (eventData is CameraChangeEventData cameraEvent)
        {
            TransitionCamera(cameraEvent);
        }
    }

    /// <summary>
    /// Initiates a camera transition based on the provided event data.
    /// </summary>
    /// <param name="cameraEvent">The camera change event data.</param>
    private void TransitionCamera(CameraChangeEventData cameraEvent)
    {
        if (mainCamera == null || cameraEvent.targetTransform == null)
        {
            Debug.LogWarning("Camera or target transform is null for camera event.", this);
            return;
        }

        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
        }

        Debug.Log($"<color=green>Camera transitioning to: {cameraEvent.targetTransform.name}</color>");

        if (cameraEvent.transitionType == CameraTransitionType.Instant)
        {
            mainCamera.transform.position = cameraEvent.targetTransform.position;
            mainCamera.transform.rotation = cameraEvent.targetTransform.rotation;
        }
        else
        {
            cameraTransitionRoutine = StartCoroutine(LerpCamera(
                mainCamera.transform,
                cameraEvent.targetTransform,
                cameraEvent.transitionSpeed,
                cameraEvent.transitionType
            ));
        }
    }

    private IEnumerator LerpCamera(Transform cameraTransform, Transform targetTransform, float speed, CameraTransitionType type)
    {
        float t = 0;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            if (t > 1) t = 1; // Clamp to ensure we hit the target exactly

            if (type == CameraTransitionType.LerpPosition || type == CameraTransitionType.LerpPositionAndRotation)
            {
                cameraTransform.position = Vector3.Lerp(startPos, targetTransform.position, t);
            }
            if (type == CameraTransitionType.LerpRotation || type == CameraTransitionType.LerpPositionAndRotation)
            {
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, t);
            }
            yield return null;
        }
        cameraTransitionRoutine = null;
    }
}
```

### File 6: `AnimatorTrigger.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete listener that handles AnimationTriggerEventData.
/// It sets Animator triggers on its own Animator component.
/// </summary>
[RequireComponent(typeof(Animator))] // Ensures an Animator component is present
public class AnimatorTrigger : CutsceneListener
{
    private Animator _animator;

    protected override void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator component not found on AnimatorTrigger GameObject: " + gameObject.name, this);
            enabled = false; // Disable script if no animator to control
        }
    }

    protected override void HandleCutsceneEvent(CutsceneEventData eventData)
    {
        if (_animator == null || !_animator.enabled) return;

        // Try to cast to AnimationTriggerEventData
        if (eventData is AnimationTriggerEventData animEvent)
        {
            TriggerAnimation(animEvent);
        }
    }

    /// <summary>
    /// Sets an Animator trigger.
    /// </summary>
    /// <param name="animEvent">The animation trigger event data.</param>
    private void TriggerAnimation(AnimationTriggerEventData animEvent)
    {
        if (!string.IsNullOrEmpty(animEvent.triggerName))
        {
            _animator.SetTrigger(animEvent.triggerName);
            Debug.Log($"<color=orange>Animator '{gameObject.name}' triggered: '{animEvent.triggerName}'</color>");
        }
        else
        {
            Debug.LogWarning("AnimationTriggerEventData received with empty triggerName.", this);
        }
    }
}
```

### File 7: `AudioPlayer.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete listener that handles AudioEventData.
/// It plays audio clips through an AudioSource component.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : CutsceneListener
{
    private AudioSource _audioSource;

    protected override void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("AudioSource component not found on AudioPlayer GameObject: " + gameObject.name, this);
            enabled = false;
        }
    }

    protected override void HandleCutsceneEvent(CutsceneEventData eventData)
    {
        if (_audioSource == null || !_audioSource.enabled) return;

        if (eventData is AudioEventData audioEvent)
        {
            PlayAudio(audioEvent);
        }
    }

    /// <summary>
    /// Plays an audio clip.
    /// </summary>
    /// <param name="audioEvent">The audio event data.</param>
    private void PlayAudio(AudioEventData audioEvent)
    {
        if (audioEvent.audioClip == null)
        {
            Debug.LogWarning("AudioEventData received with no AudioClip.", this);
            return;
        }

        if (audioEvent.stopPrevious && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        _audioSource.clip = audioEvent.audioClip;
        _audioSource.volume = audioEvent.volume;
        _audioSource.loop = audioEvent.loop;
        _audioSource.Play();

        Debug.Log($"<color=purple>Playing audio: {audioEvent.audioClip.name} (Volume: {audioEvent.volume}, Loop: {audioEvent.loop})</color>");
    }
}
```