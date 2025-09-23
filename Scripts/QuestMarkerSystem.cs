// Unity Design Pattern Example: QuestMarkerSystem
// This script demonstrates the QuestMarkerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'QuestMarkerSystem' design pattern in Unity provides a robust and flexible way to manage in-world markers that guide players through quests or objectives. It centralizes the logic for marker activation and deactivation, decoupling individual markers from the quest progression logic.

Here's how the pattern typically works:

1.  **QuestMarkerManager (Singleton):** A central manager that knows about all active quests and their current steps. It acts as the brain for the system. It exposes methods for a Quest System to update quest progress.
2.  **QuestMarker (Component):** Individual components attached to GameObjects in the scene that represent points of interest (e.g., a location to visit, an NPC to talk to, an item to collect). Each marker registers itself with the `QuestMarkerManager` and knows which quest and quest step it belongs to.
3.  **QuestSystem (or equivalent):** The game's main quest logic, which updates the `QuestMarkerManager` about quest progress (e.g., "Quest X is now on step Y").
4.  **Decoupling:** The individual `QuestMarker` components don't directly poll quest status. Instead, they react to updates from the `QuestMarkerManager`, which receives instructions from the `QuestSystem`. This makes markers reusable and easily configurable in the editor.

---

### Complete C# Unity Example: QuestMarkerSystem

This example includes three C# scripts and explains how to set them up in your Unity project.

**1. `QuestMarkerType.cs`**
An enum to categorize different types of quest markers.

```csharp
// File: Assets/Scripts/QuestMarkerType.cs
using UnityEngine; // Not strictly needed for an enum, but standard practice for Unity scripts.

/// <summary>
/// Defines different types of quest markers.
/// This enum can be used to categorize markers, allowing for
/// different visual representations, filtering in UI, or specific behaviors.
/// </summary>
public enum QuestMarkerType
{
    Destination,    // Marker for a specific location the player needs to reach.
    Talk,           // Marker for an NPC the player needs to talk to.
    Collect,        // Marker for an item or resource the player needs to collect.
    Interact,       // Marker for an object the player needs to interact with (e.g., lever, door).
    Return,         // Marker indicating where the player needs to return to complete a quest.
    Objective       // A generic objective marker.
}
```

**2. `QuestMarker.cs`**
The component attached to GameObjects in the scene that represent the actual markers.

```csharp
// File: Assets/Scripts/QuestMarker.cs
using UnityEngine;
using System.Collections; // Standard Unity namespace, often included.

/// <summary>
/// Represents an individual quest marker in the game world.
/// Attach this script to a GameObject that should act as a quest marker.
/// </summary>
/// <remarks>
/// This component registers itself with the global QuestMarkerManager on enable
/// and unregisters on disable. Its visual state (active/inactive) is controlled
/// by the QuestMarkerManager based on the current active quest step.
/// </remarks>
public class QuestMarker : MonoBehaviour
{
    [Header("Quest Marker Configuration")]
    [Tooltip("The unique ID of the quest this marker belongs to.")]
    public string questID;

    [Tooltip("The unique ID of the specific quest step this marker is relevant for. " +
             "This marker will only be active when this quest step is the current active step.")]
    public string questStepID;

    [Tooltip("The type of this quest marker (e.g., Destination, Talk, Collect). " +
             "Useful for distinguishing markers in UI or for different visual styles.")]
    public QuestMarkerType markerType;

    [Tooltip("The GameObject that represents the visual indicator of this marker in the world " +
             "(e.g., a child object with a mesh, sprite, or particle system).")]
    [SerializeField]
    private GameObject markerVisuals;

    private void Awake()
    {
        // If markerVisuals is not explicitly assigned in the Inspector,
        // try to find the first child GameObject as the visual representation.
        // This is a common convention but explicit assignment is safer.
        if (markerVisuals == null)
        {
            if (transform.childCount > 0)
            {
                markerVisuals = transform.GetChild(0).gameObject;
                Debug.LogWarning($"QuestMarker on '{gameObject.name}' had no 'markerVisuals' assigned. " +
                                 $"Using first child '{markerVisuals.name}' as visuals.", this);
            }
            else
            {
                Debug.LogWarning($"QuestMarker on '{gameObject.name}' has no 'markerVisuals' assigned " +
                                 "and no child GameObjects. This marker will not be able to display visuals.", this);
            }
        }

        // Initially ensure the marker visuals are off. The manager will activate them if needed.
        DeactivateVisuals();
    }

    private void OnEnable()
    {
        // When this GameObject becomes active in the hierarchy (or scene loads),
        // register this marker with the global QuestMarkerManager.
        if (QuestMarkerManager.Instance != null)
        {
            QuestMarkerManager.Instance.RegisterMarker(this);
        }
        else
        {
            Debug.LogError("QuestMarkerManager.Instance is null. Make sure QuestMarkerManager is present " +
                           "and initialized in your scene.", this);
            enabled = false; // Disable this component if manager is missing to prevent further errors.
        }
    }

    private void OnDisable()
    {
        // When this GameObject becomes inactive or is destroyed,
        // unregister this marker from the QuestMarkerManager.
        if (QuestMarkerManager.Instance != null)
        {
            QuestMarkerManager.Instance.UnregisterMarker(this);
        }
    }

    /// <summary>
    /// Activates the visual representation of this quest marker in the game world.
    /// This method is called by the QuestMarkerManager when this marker should be visible.
    /// </summary>
    public void ActivateVisuals()
    {
        if (markerVisuals != null && !markerVisuals.activeSelf)
        {
            markerVisuals.SetActive(true);
            // Add any additional visual effects here, e.g., particle systems, sounds, animations.
            // Debug.Log($"Activating visuals for marker: {questID} - {questStepID}");
        }
    }

    /// <summary>
    /// Deactivates the visual representation of this quest marker in the game world.
    /// This method is called by the QuestMarkerManager when this marker should be hidden.
    /// </summary>
    public void DeactivateVisuals()
    {
        if (markerVisuals != null && markerVisuals.activeSelf)
        {
            markerVisuals.SetActive(false);
            // Stop any visual effects here.
            // Debug.Log($"Deactivating visuals for marker: {questID} - {questStepID}");
        }
    }

    // You could extend this class with methods for player interaction
    // (e.g., OnPlayerEnterTrigger()), but the core pattern focuses on
    // activation/deactivation via the manager.
}
```

**3. `QuestMarkerManager.cs`**
The central singleton manager that handles the state of all quest markers.

```csharp
// File: Assets/Scripts/QuestMarkerManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList() when iterating collections to avoid modification issues.

/// <summary>
/// The central manager for all Quest Markers in the game.
/// Implements the Singleton pattern to provide a global access point.
/// </summary>
/// <remarks>
/// This is the core of the QuestMarkerSystem pattern. It acts as the
/// intermediary between the Quest System (which dictates quest progress)
/// and the individual QuestMarker components (which represent visual cues).
/// It keeps track of all registered markers and their active state based on
/// the current progress of quests.
/// </remarks>
public class QuestMarkerManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    private static QuestMarkerManager _instance;
    public static QuestMarkerManager Instance
    {
        get
        {
            // If the instance doesn't exist, try to find it in the scene.
            if (_instance == null)
            {
                _instance = FindObjectOfType<QuestMarkerManager>();

                // If still null, create a new GameObject and add the component.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(QuestMarkerManager).Name);
                    _instance = singletonObject.AddComponent<QuestMarkerManager>();
                    Debug.Log($"Created new QuestMarkerManager instance on GameObject '{singletonObject.name}'.");
                }
                // Ensure the singleton persists across scene loads.
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // Ensures only one instance exists and persists across scene loads.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If another instance already exists, destroy this one.
            Debug.LogWarning($"Duplicate QuestMarkerManager instance found on '{gameObject.name}'. Destroying it.");
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Make this GameObject persistent.
        }
    }
    // --- End Singleton Implementation ---


    // Stores all currently registered QuestMarker components in the scene.
    // Markers add/remove themselves from this list via OnEnable/OnDisable.
    private List<QuestMarker> _registeredMarkers = new List<QuestMarker>();

    // Stores the current active step ID for each active quest.
    // Key: QuestID (string), Value: Current Active QuestStepID (string).
    private Dictionary<string, string> _activeQuestStepIDs = new Dictionary<string, string>();

    /// <summary>
    /// Registers a QuestMarker with the manager.
    /// This method is typically called automatically by a QuestMarker's OnEnable() method.
    /// </summary>
    /// <param name="marker">The QuestMarker component to register.</param>
    public void RegisterMarker(QuestMarker marker)
    {
        if (!_registeredMarkers.Contains(marker))
        {
            _registeredMarkers.Add(marker);
            // Immediately update the marker's state upon registration
            // to reflect current quest progress (e.g., if quest is already active).
            UpdateMarkerState(marker);
            Debug.Log($"QuestMarker registered: Quest='{marker.questID}', Step='{marker.questStepID}' on '{marker.gameObject.name}'.");
        }
    }

    /// <summary>
    /// Unregisters a QuestMarker from the manager.
    /// This method is typically called automatically by a QuestMarker's OnDisable() method.
    /// </summary>
    /// <param name="marker">The QuestMarker component to unregister.</param>
    public void UnregisterMarker(QuestMarker marker)
    {
        if (_registeredMarkers.Remove(marker))
        {
            Debug.Log($"QuestMarker unregistered: Quest='{marker.questID}', Step='{marker.questStepID}' on '{marker.gameObject.name}'.");
        }
    }

    /// <summary>
    /// Initializes a quest by setting its initial active step.
    /// This should be called by your game's Quest System when a quest begins.
    /// All markers associated with this initial step will become visible.
    /// </summary>
    /// <param name="questID">The unique ID of the quest to initialize.</param>
    /// <param name="initialStepID">The unique ID of the first step for this quest.</param>
    public void InitializeQuest(string questID, string initialStepID)
    {
        Debug.Log($"<color=blue>QuestMarkerManager: Initializing Quest '{questID}' to step '{initialStepID}'.</color>");
        if (_activeQuestStepIDs.ContainsKey(questID))
        {
            Debug.LogWarning($"Quest '{questID}' is already active. Updating its step from '{_activeQuestStepIDs[questID]}' to '{initialStepID}'.");
        }
        SetQuestStep(questID, initialStepID);
    }

    /// <summary>
    /// Updates the active step for a given quest.
    /// This should be called by your game's Quest System whenever a quest progresses
    /// to a new step. It will trigger a re-evaluation of all registered markers' visibility.
    /// Markers for the old step will hide, and markers for the new step will show.
    /// </summary>
    /// <param name="questID">The unique ID of the quest whose step is changing.</param>
    /// <param name="newActiveStepID">The unique ID of the newly active quest step.</param>
    public void SetQuestStep(string questID, string newActiveStepID)
    {
        _activeQuestStepIDs[questID] = newActiveStepID;
        Debug.Log($"<color=blue>QuestMarkerManager: Quest '{questID}' current step set to: '{newActiveStepID}'.</color>");

        // Iterate through all registered markers and update their visibility
        // based on the new quest step. Using ToList() to avoid issues if a marker
        // would potentially unregister itself during the iteration (e.g., if it's destroyed).
        UpdateAllMarkerStates();
    }

    /// <summary>
    /// Marks a quest as completed. This will deactivate all markers associated with this quest.
    /// This should be called by your game's Quest System when a quest is finished.
    /// </summary>
    /// <param name="questID">The unique ID of the quest to complete.</param>
    public void CompleteQuest(string questID)
    {
        if (_activeQuestStepIDs.Remove(questID))
        {
            Debug.Log($"<color=blue>QuestMarkerManager: Quest '{questID}' completed. Deactivating all associated markers.</color>");
            // Re-evaluate all markers to ensure those belonging to the completed quest are hidden.
            UpdateAllMarkerStates();
        }
        else
        {
            Debug.LogWarning($"QuestMarkerManager: Attempted to complete quest '{questID}' which was not active.");
        }
    }

    /// <summary>
    /// Forces a re-evaluation of all registered markers' visibility.
    /// Use this if you have external factors that might change marker visibility
    /// without a direct quest step update (e.g., player enters a new area, time of day changes).
    /// </summary>
    public void UpdateAllMarkerStates()
    {
        // Debug.Log("QuestMarkerManager: Updating all marker states.");
        foreach (QuestMarker marker in _registeredMarkers.ToList())
        {
            // Check if the marker object is still valid before updating.
            // This handles cases where a GameObject might have been destroyed.
            if (marker != null)
            {
                UpdateMarkerState(marker);
            }
        }
    }

    /// <summary>
    /// Determines whether a specific marker should be active based on current quest progress
    /// and updates its visual state accordingly (Activates or Deactivates visuals).
    /// </summary>
    /// <param name="marker">The QuestMarker component to evaluate and update.</param>
    private void UpdateMarkerState(QuestMarker marker)
    {
        // Try to get the current active step ID for the quest this marker belongs to.
        if (_activeQuestStepIDs.TryGetValue(marker.questID, out string currentActiveStepID))
        {
            // If the marker's quest ID matches an active quest, and its step ID matches
            // the current active step for that quest, then activate its visuals.
            if (marker.questStepID == currentActiveStepID)
            {
                marker.ActivateVisuals();
            }
            else
            {
                // Otherwise, if the quest is active but this marker is not for the current step,
                // or if the step doesn't match, deactivate its visuals.
                marker.DeactivateVisuals();
            }
        }
        else
        {
            // If the quest associated with this marker is not active at all, deactivate its visuals.
            marker.DeactivateVisuals();
        }
    }

    // You can add methods here for UI integration, for example:
    // public List<QuestMarker> GetActiveMarkersForUI()
    // {
    //     // Return a list of markers that are currently visually active.
    //     // This could be used by a mini-map or an on-screen objective display.
    //     return _registeredMarkers.Where(m => m.gameObject.activeInHierarchy && m.markerVisuals != null && m.markerVisuals.activeSelf).ToList();
    // }
}
```

**4. `QuestSystemMock.cs`**
A simple mock quest system to demonstrate how a real quest system would interact with the `QuestMarkerManager`.

```csharp
// File: Assets/Scripts/QuestSystemMock.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Required for UI elements like Text and Button.

/// <summary>
/// A mock Quest System to demonstrate interaction with the QuestMarkerSystem pattern.
/// In a real game, this would be a more complex system managing quest data,
/// objectives, dialogue, triggers, rewards, etc.
/// </summary>
/// <remarks>
/// This script provides UI buttons to simulate starting, progressing, and completing a quest.
/// It interacts directly with the QuestMarkerManager to update quest progress,
/// which in turn controls the visibility of QuestMarkers in the scene.
/// </remarks>
public class QuestSystemMock : MonoBehaviour
{
    [Header("Mock Quest Definition")]
    [Tooltip("The unique ID for our demonstration quest.")]
    public string questID = "MyFirstQuest";

    [Tooltip("A list of step IDs that define the progression of this quest.")]
    public List<string> questSteps = new List<string> {
        "StartQuest_InitialMarker",
        "ReachLocationA",
        "TalkToNPC_AtLocationB",
        "CollectItem_NearC",
        "ReturnToStart_FinalLocation",
        "QuestComplete_FinalState" // A conceptual step indicating completion, might not have a marker.
    };

    private int _currentStepIndex = -1; // -1 means quest not started.
    private string _currentQuestStepID;
    private bool _isQuestActive = false;

    [Header("UI References (for demonstration)")]
    [Tooltip("Text element to display current quest status.")]
    public Text questStatusText; // Using legacy UI Text
    [Tooltip("Button to start the quest.")]
    public Button startQuestButton;
    [Tooltip("Button to progress to the next quest step.")]
    public Button progressQuestButton;
    [Tooltip("Button to complete the current quest.")]
    public Button completeQuestButton;

    void Start()
    {
        // Ensure the QuestMarkerManager exists in the scene.
        if (QuestMarkerManager.Instance == null)
        {
            Debug.LogError("QuestMarkerManager not found or not initialized in scene! " +
                           "Please ensure it's present or its Awake method has run.", this);
            enabled = false; // Disable this mock system if the manager is missing.
            return;
        }

        // Hook up UI button events to our methods.
        if (startQuestButton != null)
        {
            startQuestButton.onClick.AddListener(StartQuest);
        }
        if (progressQuestButton != null)
        {
            progressQuestButton.onClick.AddListener(ProgressQuest);
        }
        if (completeQuestButton != null)
        {
            completeQuestButton.onClick.AddListener(CompleteCurrentQuest);
        }

        // Initialize UI display.
        UpdateUI();
    }

    /// <summary>
    /// Starts the mock quest. This calls QuestMarkerManager.InitializeQuest()
    /// to set the initial active step and activate relevant markers.
    /// </summary>
    public void StartQuest()
    {
        if (_isQuestActive)
        {
            Debug.LogWarning($"Quest '{questID}' is already active.");
            return;
        }

        _isQuestActive = true;
        _currentStepIndex = 0;
        _currentQuestStepID = questSteps[_currentStepIndex];

        Debug.Log($"<color=cyan>--- Quest System: Quest Started: '{questID}' ---</color>");
        // Inform the QuestMarkerManager that this quest has started with its first step.
        QuestMarkerManager.Instance.InitializeQuest(questID, _currentQuestStepID);
        UpdateUI();
    }

    /// <summary>
    /// Progresses the mock quest to the next step in the defined sequence.
    /// This calls QuestMarkerManager.SetQuestStep() to update the active quest step,
    /// which in turn causes the manager to update marker visibility.
    /// </summary>
    public void ProgressQuest()
    {
        if (!_isQuestActive)
        {
            Debug.LogWarning($"Cannot progress quest '{questID}': Quest is not active.");
            return;
        }

        // Check if we're already at the last defined step.
        if (_currentStepIndex >= questSteps.Count - 1)
        {
            Debug.Log($"Quest '{questID}' is at its final defined step. Use 'Complete Quest' button to finish.");
            return;
        }

        _currentStepIndex++;
        _currentQuestStepID = questSteps[_currentStepIndex];

        Debug.Log($"<color=green>Quest System: Progressing Quest '{questID}' to step: '{_currentQuestStepID}'</color>");
        // Inform the QuestMarkerManager about the new active quest step.
        QuestMarkerManager.Instance.SetQuestStep(questID, _currentQuestStepID);
        UpdateUI();
    }

    /// <summary>
    /// Completes the mock quest. This calls QuestMarkerManager.CompleteQuest()
    /// to signal that the quest is finished and all its markers should be hidden.
    /// </summary>
    public void CompleteCurrentQuest()
    {
        if (!_isQuestActive)
        {
            Debug.LogWarning($"Cannot complete quest '{questID}': Quest is not active.");
            return;
        }

        Debug.Log($"<color=magenta>--- Quest System: Quest Completed: '{questID}' ---</color>");
        // Inform the QuestMarkerManager that this quest is now complete.
        QuestMarkerManager.Instance.CompleteQuest(questID);
        _isQuestActive = false;
        _currentStepIndex = -1;
        _currentQuestStepID = null; // Clear current step ID as quest is complete.
        UpdateUI();
    }

    /// <summary>
    /// Updates the text and button interactability based on the current quest state.
    /// </summary>
    private void UpdateUI()
    {
        if (questStatusText != null)
        {
            if (!_isQuestActive)
            {
                questStatusText.text = $"Quest Status: Not Started (Quest ID: {questID})";
            }
            else
            {
                questStatusText.text = $"Quest Status: Active\nCurrent Step: {_currentQuestStepID}";
            }
        }

        // Update button interactability.
        if (startQuestButton != null) startQuestButton.interactable = !_isQuestActive;
        // Progress button is interactable if quest is active AND not at the very last step.
        if (progressQuestButton != null) progressQuestButton.interactable = _isQuestActive && _currentStepIndex < questSteps.Count - 1;
        if (completeQuestButton != null) completeQuestButton.interactable = _isQuestActive;
    }
}
```

---

### Unity Scene Setup for Demonstration

To make this example work in Unity:

1.  **Create C# Scripts:**
    *   In your Unity Project window, create three new C# Scripts: `QuestMarkerType.cs`, `QuestMarker.cs`, `QuestMarkerManager.cs`, and `QuestSystemMock.cs`. Copy the respective code into each file.

2.  **Create Manager GameObject:**
    *   In your scene Hierarchy, create an empty GameObject and name it `_Managers`.
    *   Drag the `QuestMarkerManager.cs` script onto `_Managers` to add it as a component.
    *   Drag the `QuestSystemMock.cs` script onto `_Managers` to add it as a component.

3.  **Create UI Elements:**
    *   Right-click in the Hierarchy -> UI -> Canvas. Name it `QuestCanvas`.
    *   Inside `QuestCanvas`, create a Text element (Right-click on Canvas -> UI -> Text). Name it `QuestStatusText`. Adjust its position, font size, etc., to be visible.
    *   Inside `QuestCanvas`, create three Button elements (Right-click on Canvas -> UI -> Button). Name them `StartQuestButton`, `ProgressQuestButton`, `CompleteQuestButton`. Adjust their positions and text labels to be clear.

4.  **Connect UI to `QuestSystemMock`:**
    *   Select the `_Managers` GameObject.
    *   In the Inspector, locate the `QuestSystemMock` component.
    *   Drag `QuestStatusText` from the Hierarchy into the `Quest Status Text` field of `QuestSystemMock`.
    *   Drag `StartQuestButton` into the `Start Quest Button` field.
    *   Drag `ProgressQuestButton` into the `Progress Quest Button` field.
    *   Drag `CompleteQuestButton` into the `Complete Quest Button` field.

5.  **Create Quest Marker GameObjects:**
    *   Create several empty GameObjects in your scene (e.g., `MarkerA`, `MarkerB`, `MarkerC`, `MarkerD`). Position them at different locations.
    *   For each marker GameObject, add the `QuestMarker.cs` component.
    *   As a *child* of each marker GameObject, create a simple visual indicator. This could be:
        *   A 3D Cube or Sphere (Right-click on marker GO -> 3D Object -> Cube/Sphere). Resize and position it slightly above the parent.
        *   A Sprite (Right-click on marker GO -> 2D Object -> Sprite). Assign a sprite from your assets.
        *   A particle system, etc.
    *   **Crucially, assign this child visual GameObject to the `Marker Visuals` field on the `QuestMarker` component.**

6.  **Configure `QuestMarker` Components:**
    *   Select each of your `MarkerX` GameObjects and configure their `QuestMarker` component in the Inspector:
        *   **MarkerA:**
            *   `Quest ID`: `MyFirstQuest`
            *   `Quest Step ID`: `StartQuest_InitialMarker`
            *   `Marker Type`: `Destination`
        *   **MarkerB:**
            *   `Quest ID`: `MyFirstQuest`
            *   `Quest Step ID`: `ReachLocationA`
            *   `Marker Type`: `Destination`
        *   **MarkerC:**
            *   `Quest ID`: `MyFirstQuest`
            *   `Quest Step ID`: `TalkToNPC_AtLocationB`
            *   `Marker Type`: `Talk`
        *   **MarkerD:**
            *   `Quest ID`: `MyFirstQuest`
            *   `Quest Step ID`: `CollectItem_NearC`
            *   `Marker Type`: `Collect`
        *   **MarkerE (Optional):**
            *   `Quest ID`: `MyFirstQuest`
            *   `Quest Step ID`: `ReturnToStart_FinalLocation`
            *   `Marker Type`: `Return`

7.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Initially, all quest markers should be invisible. The UI will show "Quest Status: Not Started".
    *   Click the **"Start Quest"** button. `MarkerA` should become visible (and any other markers configured for `StartQuest_InitialMarker`). The UI will update to show "Current Step: StartQuest_InitialMarker".
    *   Click the **"Progress Quest"** button. `MarkerA` will disappear, and `MarkerB` will become visible (and any others for `ReachLocationA`). The UI will update.
    *   Continue clicking **"Progress Quest"**. You'll see the markers appear and disappear according to the `questSteps` defined in `QuestSystemMock`.
    *   Once you've reached the last defined step (`ReturnToStart_FinalLocation`), click **"Complete Quest"**. All visible markers for `MyFirstQuest` will disappear, and the UI will reset.

This setup demonstrates a complete, practical implementation of the QuestMarkerSystem pattern, ready to be extended and integrated into your game.